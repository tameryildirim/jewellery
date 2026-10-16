using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Kurulum;

// ═════════════════════════════════════════════════════════════════════════════
// AÇILIŞ YAZICISI — paket GOC-47B (veri göçü Faz B: açılış bakiyesi + açılış stoku)
//
// NEDEN BURADA (Modules/Kurulum): içe aktarım klasörü (Modules/Iceaktarim) deftere
//   YAZAMAZ — denetim.sh "Iceaktarim/ altında dogrudan Add" sayacı oradaki her
//   defter çağrısını ihlal sayar (FAIL-CLOSED 3). Açılış ise zaten T-2'nin
//   konusudur: AcilisKalemi tablosu STOK türünü ve DEVIR partisi kolonunu
//   (parti_id) "Faz B'de açılır" notuyla BUGÜNDEN taşıyor. CSV yolu o şemayı
//   kullanır; ikinci bir açılış kaydı icat edilmez.
//
// KURALLAR (fail-closed):
//   • CARİ BACAĞI TEK YAZICIDAN GEÇER: CariServisi.AcilisYaz → DuzeltmeYaz →
//     CariBakiyeYazici (PAKET CARI-BAKIYE). Burada bakiye kolonuna dokunulmaz.
//   • EMANET BAKİYEYE GİRMEZ (karar 3): bu yazıcı emanet yazmaz; emanet açılışı
//     Emanet ekranından EMANET_GIRIS olarak girilir.
//   • STOK AÇILIŞI DEVIR PARTİSİ AÇAR; maliyet bilinmiyorsa NULL kalır, SIFIR
//     YAZILMAZ (migration 20260914084939 gerekçesi). MaliyetMotoru.PartiAc
//     sıfır/eksi maliyeti zaten NULL'a çevirir.
//   • GERİ ALMA = TERS KAYIT (ADR-0004 append-only): satır silinmez, ters işaretli
//     ikinci kayıt yazılır ve AcilisKalemi GERI_ALINDI damgası alır. Stokta ters
//     kayıt ancak DEVIR partisi HİÇ TÜKETİLMEMİŞSE yazılır (kısmi geri alma yok:
//     satılmış malı geri koymak kâr-zararı bozar — StornoServisi.ErtelenenTurler
//     ile aynı gerekçe).
//   • SaveChanges ÇAĞRILMAZ: çağıranın tek transaction'ı içinde biriktirilir.
//     (Cari bacağı DuzeltmeYaz'ın kendi SaveChanges'idir; dış transaction'a
//     katılır — VeriIslemi.IsBaslatVeyaDevral.)
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir cari açılış satırının döviz bacağı (para birimi + tutar, işaretli).</summary>
public sealed record AcilisDovizBacagi(string ParaBirimi, decimal Tutar);

public sealed class AcilisYazici(KuyumcuDbContext db, Cari.CariServisi cariServisi)
{
    private readonly Yerlesim.YerlesimServisi yerlesim = new(db);

    // ── CARİ ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Cari açılışını TEK YAZICI üzerinden yazar ve <see cref="AcilisKalemi"/> açar.
    /// TL + has tek ACILIS hareketidir; her döviz bacağı ayrı ACILIS hareketidir.
    /// </summary>
    public async Task<AcilisKalemi> CariYaz(
        string cariId, decimal tl, decimal has, IReadOnlyList<AcilisDovizBacagi> dovizler,
        string gerekce, string kurSnapshotId, AktifKullanici islem, string oturumId, DateTime now)
    {
        var hareketId = await CariBacaklari(cariId, tl, has, dovizler, gerekce, kurSnapshotId, islem);

        var kalem = new AcilisKalemi
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            OturumId = oturumId,
            Tur = AcilisTuru.Cari,
            HedefId = cariId,
            SubeId = null,
            BenzersizAnahtar = AcilisKalemi.AnahtarUret(AcilisTuru.Cari, cariId, null),
            HareketId = hareketId,
            TlTutar = tl,
            HasGram = has,
            Durum = AcilisKalemDurumu.Uygulandi,
            Olusturuldu = now,
        };
        db.AcilisKalemleri.Add(kalem);
        return kalem;
    }

    /// <summary>Cari açılışını TERS ACILIS kaydıyla geri alır (T-2 GeriAl deseni).</summary>
    public async Task CariTersYaz(
        AcilisKalemi kalem, IReadOnlyList<AcilisDovizBacagi> dovizler,
        string gerekce, string kurSnapshotId, AktifKullanici islem)
    {
        if (kalem.Tur != AcilisTuru.Cari || kalem.Durum != AcilisKalemDurumu.Uygulandi)
            throw new ConflictException("Bu açılış kalemi geri alınabilir durumda değil.");

        await CariBacaklari(kalem.HedefId, -kalem.TlTutar, -kalem.HasGram,
            dovizler.Select(d => d with { Tutar = -d.Tutar }).ToList(),
            gerekce, kurSnapshotId, islem);
        kalem.Durum = AcilisKalemDurumu.GeriAlindi;
        AnahtariSerbestBirak(kalem);
    }

    private async Task<string> CariBacaklari(
        string cariId, decimal tl, decimal has, IReadOnlyList<AcilisDovizBacagi> dovizler,
        string gerekce, string kurSnapshotId, AktifKullanici islem)
    {
        // TL + has bir ACILIS hareketidir (ParaBirimi TRY); her döviz bacağı AYRI hareket
        // olur (TL=0). Tek satırda TRY dışı para birimiyle TL tutarı taşımak, TL'yi
        // ParaBirimi'ne göre süzen raporlarda bakiyeyi görünmez kılardı.
        string? ilkHareket = null;
        if (tl != 0m || has != 0m)
            ilkHareket = await cariServisi.AcilisYaz(cariId, new Cari.CariDuzeltmeDto
            {
                TlDelta = tl,
                HasDelta = has,
                Gerekce = gerekce,
                KurSnapshotId = kurSnapshotId,
            }, islem);

        foreach (var d in dovizler.Where(d => d.Tutar != 0m))
        {
            var id = await cariServisi.AcilisYaz(cariId, new Cari.CariDuzeltmeDto
            {
                TlDelta = 0m,
                HasDelta = 0m,
                DovizParaBirimi = d.ParaBirimi,
                DovizDelta = d.Tutar,
                Gerekce = gerekce,
                KurSnapshotId = kurSnapshotId,
            }, islem);
            ilkHareket ??= id;
        }

        return ilkHareket
            ?? throw new BadRequestException("Açılış satırında en az bir tutar sıfırdan farklı olmalıdır");
    }

    // ── STOK ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Açılış stoğunu yazar: SAYIM_DUZELTME hareketi (para hareketi değildir) +
    /// stok artışı + DEVIR partisi + yerleşim girişi + <see cref="AcilisKalemi"/>.
    /// </summary>
    public async Task<AcilisKalemi> StokYaz(
        Data.Urun urun, string subeId, decimal adet, decimal? birimMaliyetTl,
        KurSnapshot kur, string gerekce, AktifKullanici islem, string oturumId, DateTime now)
    {
        if (adet <= 0m) throw new BadRequestException("Açılış adedi sıfırdan büyük olmalıdır");
        // PAKET GOC-C (#6 · K14) son savunma: ADET birimli ürüne kesirli açılış adedi yazılmaz (yuvarlamadan ÖNCE).
        Urun.UrunAdetKapisi.Dogrula(urun.Birim, adet, "adet");
        adet = Math.Round(adet, 3);

        var hareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
        {
            Id = hareketId,
            TenantId = islem.TenantId,
            SubeId = subeId,
            Tur = "SAYIM_DUZELTME",
            Tarih = now,
            Aciklama = $"Açılış stoku (devir): {gerekce}",
            TlTutari = 0m,                                    // para el değiştirmez
            HasGrami = Math.Round(adet * urun.HasGrami, 6),   // + stoğa giriş
            Adet = adet,
            Maden = urun.Maden,
            ParaBirimi = "TRY",
            KurSnapshotId = kur.Id,
            UrunId = urun.Id,
            KullaniciId = islem.Id,
            Olusturuldu = now,
        });

        await StokDegistir(urun, subeId, islem.TenantId, adet, now);

        // FAIL-CLOSED 4: maliyet bilinmiyorsa NULL — SIFIR YAZILMAZ.
        var parti = new Maliyet.MaliyetMotoru(db).PartiAc(
            islem.TenantId, subeId, urun.Id, PartiKaynagi.Devir, adet, urun.HasGrami, now,
            birimMaliyetTl: birimMaliyetTl, altinHasFiyati: kur.AltinHasFiyati,
            girisHareketId: hareketId, kurSnapshotId: birimMaliyetTl is null ? null : kur.Id);

        await yerlesim.GirisEkle(islem.TenantId, subeId, urun.Id, adet, null, urun.ReyonId, now);

        var kalem = new AcilisKalemi
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            OturumId = oturumId,
            Tur = AcilisTuru.Stok,
            HedefId = urun.Id,
            SubeId = subeId,
            BenzersizAnahtar = AcilisKalemi.AnahtarUret(AcilisTuru.Stok, urun.Id, subeId),
            HareketId = hareketId,
            PartiId = parti.Id,
            TlTutar = birimMaliyetTl is { } m ? Math.Round(m * adet, 2, MidpointRounding.AwayFromZero) : 0m,
            HasGram = Math.Round(adet * urun.HasGrami, 6),
            Durum = AcilisKalemDurumu.Uygulandi,
            Olusturuldu = now,
        };
        db.AcilisKalemleri.Add(kalem);
        return kalem;
    }

    /// <summary>
    /// Stok açılışını TERS SAYIM_DUZELTME kaydıyla geri alır. DEVIR partisi
    /// tüketilmişse (mal satılmış/sayımda düşmüş) 409 — kısmi geri alma yoktur.
    /// </summary>
    public async Task StokTersYaz(
        AcilisKalemi kalem, KurSnapshot kur, string gerekce, AktifKullanici islem, DateTime now)
    {
        if (kalem.Tur != AcilisTuru.Stok || kalem.Durum != AcilisKalemDurumu.Uygulandi
            || kalem.PartiId is null || kalem.SubeId is null)
            throw new ConflictException("Bu açılış kalemi geri alınabilir durumda değil.");

        var parti = await db.Set<StokPartisi>()
            .FirstOrDefaultAsync(p => p.Id == kalem.PartiId && p.TenantId == islem.TenantId)
            ?? throw new ConflictException("Açılış partisi bulunamadı; geri alma yapılamaz.");

        var urun = await db.Urunler
            .FirstOrDefaultAsync(u => u.Id == kalem.HedefId && u.TenantId == islem.TenantId)
            ?? throw new ConflictException("Açılış ürünü bulunamadı; geri alma yapılamaz.");

        if (parti.AdetKalan != parti.AdetGiren)
            throw new ConflictException(
                $"'{urun.Kod}' ürününün açılış stoğundan {parti.AdetGiren - parti.AdetKalan:0.###} adet " +
                "satılmış ya da düşülmüş. Kısmi geri alma yapılmaz: önce o işlemleri iade/storno edin " +
                "ya da farkı sayımla düzeltin.");

        var adet = parti.AdetGiren;
        var hareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
        {
            Id = hareketId,
            TenantId = islem.TenantId,
            SubeId = kalem.SubeId,
            Tur = "SAYIM_DUZELTME",
            Tarih = now,
            Aciklama = $"Açılış stoku GERİ ALINDI (ters kayıt): {gerekce}",
            TlTutari = 0m,
            HasGrami = -Math.Round(adet * parti.HasGramBirim, 6),
            Adet = adet,
            Maden = urun.Maden,
            ParaBirimi = "TRY",
            KurSnapshotId = kur.Id,
            UrunId = urun.Id,
            KullaniciId = islem.Id,
            Olusturuldu = now,
        });

        var yeniAdet = await StokDegistir(urun, kalem.SubeId, islem.TenantId, -adet, now, parti.HasGramBirim);

        // Partiyi bu ters kayda TÜKETTİR (iz kalır, satır silinmez).
        parti.AdetKalan = 0m;
        db.Set<StokPartiTuketimi>().Add(new StokPartiTuketimi
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            PartiId = parti.Id,
            CikisHareketId = hareketId,
            UrunId = urun.Id,
            SubeId = kalem.SubeId,
            Adet = adet,
            BirimMaliyetTl = parti.BirimMaliyetTl,
            BirimMaliyetHas = parti.BirimMaliyetHas,
            Tarih = now,
        });

        await yerlesim.CikisDus(islem.TenantId, kalem.SubeId, urun.Id, adet, null, yeniAdet, now);
        kalem.Durum = AcilisKalemDurumu.GeriAlindi;
        AnahtariSerbestBirak(kalem);
    }

    /// <summary>
    /// Geri alınan kalemin benzersizlik anahtarı SERBEST bırakılır: UNIQUE(tenant, tür, anahtar)
    /// kısıtı GERI_ALINDI satırı da sayar; anahtar kalsaydı geri alınan açılış (düzeltilmiş
    /// dosyayla) bir daha HİÇ girilemezdi. Kalem SİLİNMEZ, iz kalır (ADR-0004).
    /// </summary>
    private static void AnahtariSerbestBirak(AcilisKalemi kalem) =>
        kalem.BenzersizAnahtar = $"GERI:{kalem.Id}";

    /// <summary>Stok satırına delta uygular (aynı istekteki kaydedilmemiş satır Local'den bulunur).</summary>
    private async Task<decimal> StokDegistir(
        Data.Urun urun, string subeId, string tenantId, decimal delta, DateTime now, decimal? hasBirim = null)
    {
        var birim = hasBirim ?? urun.HasGrami;
        var mevcut = db.Stoklar.Local
                .FirstOrDefault(s => s.UrunId == urun.Id && s.SubeId == subeId && s.TenantId == tenantId)
            ?? await db.Stoklar
                .FirstOrDefaultAsync(s => s.UrunId == urun.Id && s.SubeId == subeId && s.TenantId == tenantId);

        var yeniAdet = Math.Round((mevcut?.Adet ?? 0m) + delta, 3);
        if (yeniAdet < 0m)
            throw new ConflictException(
                $"'{urun.Kod}' ürününün stoğu açılış adedinin altına inmiş; geri alma stoğu eksiye düşürür.");
        var yeniHas = Math.Round((mevcut?.ToplamHasGram ?? 0m) + delta * birim, 6);

        if (mevcut is not null)
        {
            mevcut.Adet = yeniAdet;
            mevcut.ToplamHasGram = yeniHas;
            mevcut.Guncellendi = now;
        }
        else
        {
            db.Stoklar.Add(new Stok
            {
                Id = Kimlik.Ulid(),
                TenantId = tenantId,
                SubeId = subeId,
                UrunId = urun.Id,
                Adet = yeniAdet,
                ToplamHasGram = yeniHas,
                Guncellendi = now,
            });
        }
        return yeniAdet;
    }
}
