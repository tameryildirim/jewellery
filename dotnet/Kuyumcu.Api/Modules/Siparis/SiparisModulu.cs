using System.Globalization;
using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Kuyumcu.Core.Domain.Girdi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SiparisVarligi = Kuyumcu.Api.Data.Siparis;

namespace Kuyumcu.Api.Modules.Siparis;

// ═════════════════════════════════════════════════════════════════════════════
// SİPARİŞ MODÜLÜ (paket 3 Faz A) — müşteri siparişi · dış atölye siparişi · tamir.
//
// Desen: Modules/Emanet/EmanetModulu.cs (DTO + servis + controller aynı dosyada,
// doğrulama servis içinde ELLE — DataAnnotations yalnız controller yolunda çalışır).
//
// ⚠ DEFTERE YAZMAZ. Bu dosyada HareketFabrikasi.Ekle YOKTUR, Hareketler.Add
//   YOKTUR, kasa/stok/kâr-zarar dokunuşu YOKTUR. Para ve teslim mevcut defter
//   uçlarından geçer; sipariş yalnız SÖZÜ kayda alır.
//
// ⚠ MEVZUAT: sipariş anında para el değiştirmediği için tevsik (459 VUK) ve
//   MASAK eşiği burada ÖLÇÜLMEZ — ödeme anında mevcut kapılar zaten ölçer.
//   Sipariş kimliği MASAK kümülatifine SOKULMAZ (ikinci bir gerçek doğururdu).
//
// ⚠ DENETİM İZİ: DenetimYazmaDinleyicisi SaveChanges'ı dinler; sipariş
//   yazma/güncelleme/geçiş kayıtları denetim izine KENDİLİĞİNDEN düşer.
//   Burada ikinci bir denetim yazımı YAPILMAZ (çift kayıt olurdu).
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class SiparisKalemDto
{
    public string? UrunId { get; set; }
    public string? Tanim { get; set; }
    public string? AyarId { get; set; }
    // PAKET GOC-C: K10 gram (> 0, 3 ondalık) · K14 adet (tam sayı ≥ 1) — ekran Gram/Adet bileşenleriyle AYNI Core kuralı.
    [Common.Dogrulama.Gram] public decimal? GramAgirlik { get; set; }
    [Common.Dogrulama.Adet] public decimal? Adet { get; set; }
}

public sealed class SiparisOlusturDto
{
    public string? Tur { get; set; }
    public string? CariId { get; set; }
    public string? SubeId { get; set; }
    public DateOnly? SozVerilenGun { get; set; }
    public string? Aciklama { get; set; }
    // PAKET GOC-C: K9a (≥ 0, 2 ondalık). Kolon decimal(14,2) tavanı servis içinde (KolonTavani).
    [Common.Dogrulama.Tutar(Kuyumcu.Core.Domain.Girdi.TutarTuru.SifirOlabilir)] public decimal? TahminiTutarTl { get; set; }
    public string? KurSnapshotId { get; set; }

    /// <summary>TAMIR türünde OPSİYONEL EMANET_GIRIS hareketi (bkz. Siparis.EmanetHareketId).</summary>
    public string? EmanetHareketId { get; set; }

    /// <summary>
    /// PAKET P-26 (Y-36) — ATOLYE_SIPARISI / TAMIR türünde işi yapan DIŞ usta carisi.
    /// ⚠ METAL ÇIKIŞI YAZMAZ (K-13: imalat kapsam DIŞI); yalnız "kim yapıyor".
    /// </summary>
    public string? UstaCariId { get; set; }

    public List<SiparisKalemDto>? Kalemler { get; set; }
}

/// <summary>
/// PAKET P-26 (Y-30) — tamir/servis kaporası. Alanlar cari tahsilat ucuyla
/// AYNI addadır: ikinci bir ödeme sözleşmesi doğmasın diye gövde birebir
/// <c>CariServisi.TahsilatGirHareket</c>'e devredilir.
/// </summary>
public sealed class SiparisKaporaDto
{
    [Common.Dogrulama.Tutar] public decimal? TlTutari { get; set; }   // PAKET GOC-C: K9 (> 0, 2 ondalık)
    public string? OdemeTuru { get; set; }
    public string? KasaId { get; set; }
    public string? KurSnapshotId { get; set; }
    public string? Aciklama { get; set; }
    // KAPORA-ESZAMAN tur 2 (tasarım hakemi KRİTİK): eşikten büyük kaporada çıkış yolu — cari tahsilatla
    // AYNI MASAK beyanı (kimlik + amaç + açıklama) tahsilat gövdesine aynen geçer; kapı TahsilatGirHareket'te.
    public Kuyumcu.Api.Modules.Masak.MasakBilgiDto? Masak { get; set; }
}

public sealed class SiparisGecisDto
{
    public string? YeniDurum { get; set; }
    public string? Gerekce { get; set; }

    /// <summary>İyimser kilit damgası — verilirse eskimişse 409.</summary>
    public DateTime? Guncellendi { get; set; }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class SiparisServisi(
    KuyumcuDbContext db,
    ILogger<SiparisServisi> logger,
    Cari.CariServisi? cari = null)
{
    /// <summary>İptal yalnız bu roller (Mehmet koşulu; tezgâhtar iptal edemez).</summary>
    public static readonly string[] IptalRolleri = ["SAHIP", "MUDUR"];

    /// <summary>
    /// GERİ YÖNLÜ geçiş (K-23 şart 1) yalnız bu roller. Tezgâhtar siparişi ileri
    /// götürür (ONAYLANDI → ATOLYEDE → HAZIR) ama "hazır"ı geri alamaz.
    /// <para>
    /// ⚠ NEDEN MODÜL ANAHTARI DEĞİL, ROL: modül anahtarları rota tablosundan
    /// TÜRETİLİR (<c>Kuyumcu.Core.Erisim.RotaModulleri.Harita</c>) — rotası
    /// olmayan bir "eylem anahtarı" bu mimaride yoktur ve uydurulması ikinci bir
    /// yetki gerçeği doğururdu. Eylem düzeyinde "herkes yapamaz" kapısının
    /// projedeki tek yerleşik deseni <see cref="IptalRolleri"/>'dir; K-23'ün
    /// yetki şartı aynı desenle karşılanır (P-22 raporu § Sapmalar).
    /// </para>
    /// </summary>
    public static readonly string[] GeriAlmaRolleri = ["SAHIP", "MUDUR"];

    /// <summary>
    /// PAKET P-51 — başlık (cari · tür · şube) düzenlemesinin geçiş dökümündeki
    /// önekidir. Durum listesi genişletilmez; iz `gerekce` alanında taşınır.
    /// </summary>
    public const string BaslikDuzenlendiNotu = "BASLIK_DUZENLENDI";

    // ─── Oluştur ──────────────────────────────────────────────────────────────

    public async Task<object> Olustur(SiparisOlusturDto dto, AktifKullanici islem)
    {
        var tur = (dto.Tur ?? "").Trim();
        if (Array.IndexOf(SiparisTuru.Tumu, tur) < 0)
            throw new BadRequestException(
                $"Geçersiz sipariş türü: '{dto.Tur}' ({string.Join(", ", SiparisTuru.Tumu)})");

        if (string.IsNullOrWhiteSpace(dto.CariId))
            throw new BadRequestException("Cari seçimi zorunlu");

        var kalemler = dto.Kalemler ?? [];
        if (kalemler.Count == 0)
            throw new BadRequestException("Sipariş en az bir kalem içermeli");

        // K1 — yabancı kimlik kapısı (transaction açılmadan ÖNCE).
        await IslemDogrulayici.CariDogrula(db, dto.CariId, islem.TenantId);

        var subeId = string.IsNullOrWhiteSpace(dto.SubeId) ? islem.SubeId : dto.SubeId;
        if (!string.IsNullOrWhiteSpace(subeId))
            await IslemDogrulayici.SubeDogrula(db, subeId, islem.TenantId);

        SozGunuDogrula(dto.SozVerilenGun);   // PAKET GOC-C: geçmiş/çok ileri söz günü yok
        var (tahmini, kurId) = await TahminDogrula(dto.TahminiTutarTl, dto.KurSnapshotId, islem.TenantId);
        var emanetId = await EmanetDogrula(dto.EmanetHareketId, tur, dto.CariId!, islem.TenantId);
        var ustaCariId = await UstaDogrula(dto.UstaCariId, tur, islem.TenantId);

        var hazir = new List<SiparisKalemi>();
        for (var i = 0; i < kalemler.Count; i++)
            hazir.Add(await KalemKur(kalemler[i], i, islem.TenantId));

        var now = DateTime.UtcNow;
        var yil = Zaman.YerelBugun().Year;
        var no = await SiparisNo.Uret(db, islem.TenantId, yil);

        // ⚠ SINIRLI DÖNGÜ (Bertan koşulu): tekilliği veritabanı verir; çakışmada
        //   numara artırılıp yeniden denenir, döngü tükenirse 409 döner.
        for (var deneme = 1; ; deneme++)
        {
            var siparisId = Kimlik.Ulid();
            var siparis = new SiparisVarligi
            {
                Id = siparisId,
                TenantId = islem.TenantId,
                No = no,
                Tur = tur,
                Durum = SiparisDurumu.Taslak,
                CariId = dto.CariId!,
                SubeId = string.IsNullOrWhiteSpace(subeId) ? null : subeId,
                SozVerilenGun = dto.SozVerilenGun,
                Aciklama = Kirp(dto.Aciklama, 500),
                TahminiTutarTl = tahmini,
                KurSnapshotId = kurId,
                EmanetHareketId = emanetId,
                UstaCariId = ustaCariId,
                OlusturanKullaniciId = islem.Id,
                Olusturuldu = now,
                Guncellendi = now,
            };

            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                db.Siparisler.Add(siparis);
                foreach (var k in hazir)
                    db.SiparisKalemleri.Add(new SiparisKalemi
                    {
                        Id = Kimlik.Ulid(),
                        TenantId = islem.TenantId,
                        SiparisId = siparisId,
                        UrunId = k.UrunId,
                        Tanim = k.Tanim,
                        AyarId = k.AyarId,
                        GramAgirlik = k.GramAgirlik,
                        Adet = k.Adet,
                        Sira = k.Sira,
                        Olusturuldu = now,
                    });

                // Doğuş satırı: onceki_durum NULL — "bu sipariş TASLAK olarak doğdu".
                db.SiparisGecisleri.Add(new SiparisGecisi
                {
                    Id = Kimlik.Ulid(),
                    TenantId = islem.TenantId,
                    SiparisId = siparisId,
                    OncekiDurum = null,
                    YeniDurum = SiparisDurumu.Taslak,
                    Gerekce = null,
                    KullaniciId = islem.Id,
                    Olusturuldu = now,
                });

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                logger.LogInformation("[{TenantId}] SIPARIS oluşturuldu: {No} ({Tur}, {Kalem} kalem)",
                    islem.TenantId, no, tur, hazir.Count);

                return await Detay(siparisId, islem);
            }
            catch (DbUpdateException) when (deneme < SiparisNo.AzamiDeneme)
            {
                await tx.RollbackAsync();
                db.ChangeTracker.Clear();
                no = SiparisNo.Artir(no);
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync();
                db.ChangeTracker.Clear();
                throw new ConflictException(
                    $"Sipariş numarası üretilemedi ({SiparisNo.AzamiDeneme} deneme). Lütfen tekrar deneyin.");
            }
        }
    }

    // ─── Liste ────────────────────────────────────────────────────────────────

    /// <summary>PAKET-03 Faz B (20.10): sipariş özeti — açık / geciken / bugün sözü verilen; kiracı geneli.</summary>
    public async Task<object> Ozet(AktifKullanici islem)
    {
        var bugun = Zaman.YerelBugun();
        var q = db.Siparisler.AsNoTracking().Where(s => s.TenantId == islem.TenantId);
        var kirilim = await q.GroupBy(s => s.Durum).Select(g => new { durum = g.Key, adet = g.Count() }).ToListAsync();
        var acik = await q.CountAsync(s => SiparisDurumu.Acik.Contains(s.Durum));
        var geciken = await q.CountAsync(s => s.SozVerilenGun != null && s.SozVerilenGun < bugun && SiparisDurumu.Acik.Contains(s.Durum));
        var bugunTeslim = await q.CountAsync(s => s.SozVerilenGun == bugun && SiparisDurumu.Acik.Contains(s.Durum));
        return new { acik, geciken, bugunTeslim, durumKirilimi = kirilim, hesaplanmaAni = DateTime.UtcNow };
    }

    public async Task<object> Listele(
        string? tur, string? durum, string? cariId, string? subeId, bool? geciken, int? limit,
        AktifKullanici islem)
    {
        if (tur is not null && Array.IndexOf(SiparisTuru.Tumu, tur) < 0)
            throw new BadRequestException($"Geçersiz sipariş türü: {tur}");
        if (durum is not null && Array.IndexOf(SiparisDurumu.Tumu, durum) < 0)
            throw new BadRequestException($"Geçersiz sipariş durumu: {durum}");

        var istenen = limit ?? SiparisVarligi.VarsayilanLimit;
        var etkinLimit = Math.Clamp(istenen, 1, SiparisVarligi.AzamiLimit);

        var bugun = Zaman.YerelBugun();

        var sorgu = db.Siparisler.AsNoTracking().Where(s => s.TenantId == islem.TenantId);
        if (tur is not null) sorgu = sorgu.Where(s => s.Tur == tur);
        if (durum is not null) sorgu = sorgu.Where(s => s.Durum == durum);
        if (!string.IsNullOrWhiteSpace(cariId)) sorgu = sorgu.Where(s => s.CariId == cariId);
        if (!string.IsNullOrWhiteSpace(subeId)) sorgu = sorgu.Where(s => s.SubeId == subeId);
        if (geciken == true)
            sorgu = sorgu.Where(s => s.SozVerilenGun != null && s.SozVerilenGun < bugun
                                  && SiparisDurumu.Acik.Contains(s.Durum));

        // TB-19: toplam ve kırılım SUNUCUDA; satır dönmez.
        var toplam = await sorgu.CountAsync();
        var kirilim = await sorgu.GroupBy(s => s.Durum)
            .Select(g => new { durum = g.Key, adet = g.Count() })
            .ToListAsync();
        var gecikenSayisi = await db.Siparisler.AsNoTracking()
            .Where(s => s.TenantId == islem.TenantId
                     && s.SozVerilenGun != null && s.SozVerilenGun < bugun
                     && SiparisDurumu.Acik.Contains(s.Durum))
            .CountAsync();

        var satirlar = await sorgu
            .OrderByDescending(s => s.Olusturuldu)
            .Take(etkinLimit)
            .Select(s => new
            {
                s.Id,
                s.No,
                s.Tur,
                s.Durum,
                s.CariId,
                s.SubeId,
                s.SozVerilenGun,
                s.TahminiTutarTl,
                s.Aciklama,
                s.Olusturuldu,
                s.Guncellendi,
                CariAd = db.Cariler.Where(c => c.Id == s.CariId).Select(c => c.Ad).FirstOrDefault(),
                KalemSayisi = db.SiparisKalemleri.Count(k => k.SiparisId == s.Id),
            })
            .ToListAsync();

        return new
        {
            istenenLimit = istenen,
            limit = etkinLimit,
            azamiLimit = SiparisVarligi.AzamiLimit,
            toplam,
            // UX-S29: sınıra değdiyse ekran "daha fazlası var" yazar — sessiz kırpma yok.
            dahaFazlasiVar = toplam > satirlar.Count,
            gecikenSayisi,
            durumKirilimi = kirilim,
            siparisler = satirlar.Select(s => new
            {
                id = s.Id,
                no = s.No,
                tur = s.Tur,
                durum = s.Durum,
                cariId = s.CariId,
                cariAd = s.CariAd,
                subeId = s.SubeId,
                sozVerilenGun = s.SozVerilenGun,
                tahminiTutarTl = s.TahminiTutarTl,
                aciklama = s.Aciklama,
                kalemSayisi = s.KalemSayisi,
                // Saat sunucudadır (K5): istemci kendi saatine bakmaz.
                geciken = SiparisGecikme.Gecikti(s.SozVerilenGun, s.Durum, bugun),
                izinliGecisler = SiparisDurumMakinesi.Hedefler(s.Tur, s.Durum),
                duzenlenebilir = s.Durum == SiparisDurumu.Taslak,
                olusturuldu = s.Olusturuldu,
                guncellendi = s.Guncellendi,
            }),
        };
    }

    // ─── Detay ────────────────────────────────────────────────────────────────

    public async Task<object> Detay(string id, AktifKullanici islem)
    {
        var s = await Bul(id, islem.TenantId);
        var bugun = Zaman.YerelBugun();

        var kalemler = await db.SiparisKalemleri.AsNoTracking()
            .Where(k => k.SiparisId == id && k.TenantId == islem.TenantId)
            .OrderBy(k => k.Sira)
            .Select(k => new
            {
                k.Id, k.UrunId, k.Tanim, k.AyarId, k.GramAgirlik, k.Adet, k.Sira,
                UrunAd = db.Urunler.Where(u => u.Id == k.UrunId).Select(u => u.Ad).FirstOrDefault(),
            })
            .ToListAsync();

        var cariAd = await db.Cariler.AsNoTracking()
            .Where(c => c.Id == s.CariId).Select(c => c.Ad).FirstOrDefaultAsync();

        // PAKET EKSIK-B1-YAZDIR (B1-5): usta ADI ve kaporanın defterdeki satırı ekrana
        // gelir — kimlik (ULID) basılmaz; kapora tutarı DEFTERDEN okunur (ikinci gerçek yok).
        var ustaAd = string.IsNullOrWhiteSpace(s.UstaCariId) ? null
            : await db.Cariler.AsNoTracking().Where(c => c.Id == s.UstaCariId).Select(c => c.Ad).FirstOrDefaultAsync();
        var kapora = await KaporaOzeti(s.KaporaHareketId, islem.TenantId);
        var kaporaGecmisi = await KaporaGecmisi(s.Id, s.KaporaHareketId, islem.TenantId);

        return new
        {
            id = s.Id,
            no = s.No,
            tur = s.Tur,
            durum = s.Durum,
            cariId = s.CariId,
            cariAd,
            subeId = s.SubeId,
            sozVerilenGun = s.SozVerilenGun,
            aciklama = s.Aciklama,
            tahminiTutarTl = s.TahminiTutarTl,
            kurSnapshotId = s.KurSnapshotId,
            emanetHareketId = s.EmanetHareketId,
            // ── PAKET P-26 ──────────────────────────────────────────────────
            ustaCariId = s.UstaCariId,
            kaporaHareketId = s.KaporaHareketId,
            kaporaAlinabilir = KaporaKilidi(s.Durum, kapora) is null,
            // UX-S30: kilitli düğmenin sebebi sunucudan gelir.
            // Tur 2 (doğrulayıcı bulgusu): istek dilinde döner — EN modda İngilizce (null korunur).
            kaporaKilitSebebi = KaporaKilidi(s.Durum, kapora) is { } kilitTr
                ? Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(kilitTr) : null,
            // PAKET EKSIK-B1-YAZDIR (B1-5): kapora kartı için defter özeti.
            ustaAd,
            kaporaTutari = kapora?.Tutar,
            kaporaTarihi = kapora?.Tarih,
            kaporaKasaId = kapora?.KasaId,
            kaporaStornolu = kapora?.Stornolu ?? false,
            // PAKET KAPORA-ESZAMAN (DÜŞÜK): geri verilip yeniden alınan kaporalar dahil TÜM
            // kapora satırları (en yeni önce) — bağ s.KaporaHareketId'nin üzerine yazılınca
            // eskisi kaybolmaz.
            kaporaGecmisi,
            // Y-36 · K-13 mührü: ekran neden metal çıkışı yazmadığını SÖYLER.
            ustaMetalCikisiBeyani =
                "Ustaya metal çıkışı bu sürümde KAYDEDİLMEZ: imalat/atölye katmanı 1.0 kapsamı "
                + "dışındadır (karar K-13). Metal fiilen veriliyorsa EMANET ÇIKIŞI ucunu kullanın.",
            geciken = SiparisGecikme.Gecikti(s.SozVerilenGun, s.Durum, bugun),
            duzenlenebilir = s.Durum == SiparisDurumu.Taslak,
            terminal = SiparisDurumMakinesi.Terminal(s.Durum),
            izinliGecisler = SiparisDurumMakinesi.Hedefler(s.Tur, s.Durum),
            gerekceIsteyenGecisler = SiparisDurumMakinesi.Hedefler(s.Tur, s.Durum)
                .Where(h => SiparisDurumMakinesi.GerekceGerekli(s.Durum, h)).ToArray(),
            // K-23 / UX-S30: ekran kilitli düğmenin SEBEBİNİ yazabilsin diye
            // hangi hedeflerin geri yönlü olduğu ve kullanıcının yetkisi ayrı döner.
            geriYonluGecisler = SiparisDurumMakinesi.Hedefler(s.Tur, s.Durum)
                .Where(h => SiparisDurumMakinesi.GeriYonlu(s.Durum, h)).ToArray(),
            geriAlmaYetkisi = Array.IndexOf(GeriAlmaRolleri, islem.Rol) >= 0,
            geriAlmaRolleri = GeriAlmaRolleri,
            gerekceAsgari = SiparisVarligi.GerekceAsgari,
            olusturanKullaniciId = s.OlusturanKullaniciId,
            olusturuldu = s.Olusturuldu,
            guncellendi = s.Guncellendi,
            kalemler = kalemler.Select(k => new
            {
                id = k.Id, urunId = k.UrunId, urunAd = k.UrunAd, tanim = k.Tanim,
                ayarId = k.AyarId, gramAgirlik = k.GramAgirlik, adet = k.Adet, sira = k.Sira,
            }),
            gecisler = await GecisSatirlari(id, islem.TenantId),
        };
    }

    // ─── Geçiş dökümü ─────────────────────────────────────────────────────────

    public async Task<object> Gecisler(string id, AktifKullanici islem)
    {
        _ = await Bul(id, islem.TenantId);   // yabancı kiracı → 404 (varlık sızdırılmaz)
        return new
        {
            siparisId = id,
            appendOnly = true,
            gecisler = await GecisSatirlari(id, islem.TenantId),
        };
    }

    private async Task<List<object>> GecisSatirlari(string siparisId, string tenantId)
    {
        var satirlar = await db.SiparisGecisleri.AsNoTracking()
            .Where(g => g.SiparisId == siparisId && g.TenantId == tenantId)
            .OrderBy(g => g.Olusturuldu).ThenBy(g => g.Id)
            .Select(g => new
            {
                g.Id, g.OncekiDurum, g.YeniDurum, g.Gerekce, g.KullaniciId, g.Olusturuldu,
                KullaniciAd = db.Kullanicilar.Where(k => k.Id == g.KullaniciId)
                    .Select(k => k.Ad + " " + k.Soyad).FirstOrDefault(),
            })
            .ToListAsync();

        return [.. satirlar.Select(g => (object)new
        {
            id = g.Id,
            oncekiDurum = g.OncekiDurum,
            yeniDurum = g.YeniDurum,
            gerekce = g.Gerekce,
            kullaniciId = g.KullaniciId,
            kullaniciAd = g.KullaniciAd,
            olusturuldu = g.Olusturuldu,
        })];
    }

    // ─── Geçiş ────────────────────────────────────────────────────────────────

    public async Task<object> Gecis(string id, SiparisGecisDto dto, AktifKullanici islem)
    {
        var yeni = (dto.YeniDurum ?? "").Trim();
        if (Array.IndexOf(SiparisDurumu.Tumu, yeni) < 0)
            throw new BadRequestException(
                $"Geçersiz durum: '{dto.YeniDurum}' ({string.Join(", ", SiparisDurumu.Tumu)})");

        // İPTAL yalnız SAHIP/MUDUR — tezgâhtar sipariş alır ama iptal edemez.
        if (yeni == SiparisDurumu.Iptal && Array.IndexOf(IptalRolleri, islem.Rol) < 0)
            throw new ForbiddenException(
                $"Bu işlem için gerekli rol: {string.Join(" veya ", IptalRolleri)}");

        var s = await Bul(id, islem.TenantId);
        IyimserKilitDogrula(s, dto.Guncellendi);

        // K-23 şart 1 — GERİ DÖNÜŞ SERBEST AMA DENETİMLİ: yetki kapısı.
        // ⚠ Kapı geçiş İZİNLİ Mİ sorusundan ÖNCE sorulur ki yetkisiz kullanıcı
        //   "izinli mi" bilgisini deneme yanılmayla sızdıramasın.
        if (SiparisDurumMakinesi.GeriYonlu(s.Durum, yeni)
            && Array.IndexOf(GeriAlmaRolleri, islem.Rol) < 0)
            throw new ForbiddenException(
                $"Durumu geri almak için gerekli rol: {string.Join(" veya ", GeriAlmaRolleri)}");

        if (!SiparisDurumMakinesi.Izinli(s.Tur, s.Durum, yeni))
            throw new BadRequestException(
                $"'{s.Durum}' durumundan '{yeni}' durumuna geçilemez ({s.Tur})");

        var gerekce = dto.Gerekce?.Trim();
        if (SiparisDurumMakinesi.GerekceGerekli(s.Durum, yeni))
        {
            if (string.IsNullOrEmpty(gerekce) || gerekce.Length < SiparisVarligi.GerekceAsgari)
                throw new BadRequestException(
                    $"Bu geçiş için gerekçe zorunlu (en az {SiparisVarligi.GerekceAsgari} karakter)");
        }

        var onceki = s.Durum;
        var now = DateTime.UtcNow;

        // ⚠ AYNI TRANSACTION: durum güncellemesi ile geçiş satırı birlikte düşer.
        //   Biri yazılıp diğeri yazılmazsa iz yalan söyler.
        await using var tx = await db.Database.BeginTransactionAsync();
        s.Durum = yeni;
        s.Guncellendi = now;
        db.SiparisGecisleri.Add(new SiparisGecisi
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            SiparisId = s.Id,
            OncekiDurum = onceki,
            YeniDurum = yeni,
            Gerekce = Kirp(gerekce, 500),
            KullaniciId = islem.Id,
            Olusturuldu = now,
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] SIPARIS geçiş: {No} {Onceki} → {Yeni}",
            islem.TenantId, s.No, onceki, yeni);

        return await Detay(s.Id, islem);
    }

    // ─── PATCH (yalnız TASLAK) ────────────────────────────────────────────────

    /// <summary>
    /// PATCH sözleşmesi: <b>gövdede yoksa KORU, null ise SİL</b>.
    /// Bu yüzden gövde ham <see cref="JsonElement"/> olarak okunur — nullable DTO
    /// ile "gönderilmedi" ile "null gönderildi" ayırt EDİLEMEZ.
    /// </summary>
    public async Task<object> Guncelle(string id, JsonElement govde, AktifKullanici islem)
    {
        if (govde.ValueKind != JsonValueKind.Object)
            throw new BadRequestException("Geçersiz istek gövdesi");

        var s = await Bul(id, islem.TenantId);
        if (s.Durum != SiparisDurumu.Taslak)
            throw new BadRequestException(
                $"Yalnız TASLAK durumundaki sipariş düzenlenebilir (bu sipariş: {s.Durum})");

        IyimserKilitDogrula(s, Damga(govde));

        var now = DateTime.UtcNow;

        // ═══ PAKET P-51 — BAŞLIK KİMLİĞİ (cari · tür · şube) TASLAKTA DÜZENLENİR ═══
        // QA turu 8 / KRİTİK (UX-1 ihlali): sipariş yanlış cariye, yanlış türe ya da
        // yanlış şubeye açıldığında düzeltme yolu YOKTU — kullanıcı siparişi iptal edip
        // yenisini açmak zorundaydı ve geçiş dökümü "iptal" diye YANLIŞ hikâye anlatıyordu.
        //
        // ⚠ KAPI DEĞİŞMEDİ: yalnız TASLAK (yukarıdaki Durum != TASLAK kapısı korunur).
        //   ONAYLANDI'dan sonra başlık DONAR — kayıt karşı tarafa söz verilmiştir.
        // ⚠ SESSİZ NULL'LAMA YASAK: tür değişimi mevcut bağları (usta carisi, emanet)
        //   geçersiz kılıyorsa AÇIK 400 döner; hiçbir alan sessizce boşaltılmaz.
        var yeniTur = s.Tur;
        if (Alan(govde, "tur", out var turOge))
        {
            if (turOge.ValueKind == JsonValueKind.Null)
                throw new BadRequestException(
                    "Sipariş türü silinemez; geçerli bir tür seçin "
                    + $"({string.Join(", ", SiparisTuru.Tumu)})");
            var t = (turOge.GetString() ?? "").Trim();
            if (Array.IndexOf(SiparisTuru.Tumu, t) < 0)
                throw new BadRequestException(
                    $"Geçersiz sipariş türü: '{turOge.GetString()}' ({string.Join(", ", SiparisTuru.Tumu)})");
            yeniTur = t;
        }

        var yeniCariId = s.CariId;
        if (Alan(govde, "cariId", out var cariOge))
        {
            var c = cariOge.ValueKind == JsonValueKind.Null ? null : cariOge.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(c))
                throw new BadRequestException("Cari seçimi zorunlu — sipariş carisiz bırakılamaz");
            // K1 — yabancı kimlik kapısı: cari AYNI kiracıda olmalı.
            await IslemDogrulayici.CariDogrula(db, c, islem.TenantId);
            yeniCariId = c;
        }

        var yeniSubeId = s.SubeId;
        if (Alan(govde, "subeId", out var subeOge))
        {
            var sb = subeOge.ValueKind == JsonValueKind.Null ? null : subeOge.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(sb))
            {
                yeniSubeId = null;   // PATCH sözleşmesi: null = SİL (şube opsiyoneldir)
            }
            else
            {
                await IslemDogrulayici.SubeDogrula(db, sb, islem.TenantId);
                yeniSubeId = sb;
            }
        }

        if (Alan(govde, "sozVerilenGun", out var gunOge))
            s.SozVerilenGun = gunOge.ValueKind == JsonValueKind.Null
                ? null
                : DateOnly.TryParse(gunOge.GetString(), CultureInfo.InvariantCulture, out var g)
                    ? (g == s.SozVerilenGun ? g : SozGunuDogrula(g)!.Value)   // PAKET GOC-C: yalnız DEĞİŞEN gün sınanır (eski siparişin kayıtlı günü düzenlemeyi kilitlemez)
                    : throw new BadRequestException("Söz verilen gün geçersiz (YYYY-AA-GG)");

        if (Alan(govde, "aciklama", out var acOge))
            s.Aciklama = acOge.ValueKind == JsonValueKind.Null ? null : Kirp(acOge.GetString(), 500);

        // ⚠ P-51: bağ doğrulamaları AYNI istekte seçilen YENİ tür/cariye göre yapılır —
        //   "önce türü değiştir, sonra ustayı bağla" iki adımlık tuzağı doğmasın.
        if (Alan(govde, "ustaCariId", out var ustaOge))
            s.UstaCariId = ustaOge.ValueKind == JsonValueKind.Null
                ? null
                : await UstaDogrula(ustaOge.GetString(), yeniTur, islem.TenantId);

        if (Alan(govde, "emanetHareketId", out var emOge))
            s.EmanetHareketId = emOge.ValueKind == JsonValueKind.Null
                ? null
                : await EmanetDogrula(emOge.GetString(), yeniTur, yeniCariId, islem.TenantId);

        // ── P-51 · BAŞLIK DEĞİŞİMİNİN MEVCUT BAĞLARA ETKİSİ ─────────────────────
        // Bağ "taşınamıyorsa" sessizce null'lanmaz: kullanıcı ne olduğunu ve ne
        // yapması gerektiğini okur (UX-S30 · Anayasa md.1).
        if (yeniTur != s.Tur)
        {
            if (!string.IsNullOrWhiteSpace(s.UstaCariId)
                && yeniTur is not (SiparisTuru.AtolyeSiparisi or SiparisTuru.Tamir))
                throw new BadRequestException(
                    $"Tür '{yeniTur}' olarak değiştirilemez: bu siparişte usta carisi bağlı ve usta "
                    + "carisi yalnız ATOLYE_SIPARISI ile TAMIR türünde tutulur. Aynı istekte "
                    + "ustaCariId alanını null gönderip bağı kaldırın ya da türü değiştirmeyin.");

            if (!string.IsNullOrWhiteSpace(s.EmanetHareketId) && yeniTur != SiparisTuru.Tamir)
                throw new BadRequestException(
                    $"Tür '{yeniTur}' olarak değiştirilemez: bu siparişe müşteri eşyasının EMANET "
                    + "girişi bağlı ve emanet bağı yalnız TAMIR türünde tutulur. Aynı istekte "
                    + "emanetHareketId alanını null gönderip bağı kaldırın ya da türü değiştirmeyin.");

            // Dış atölyeye ısmarlama "kimin yaptığı" bilinmeden kurulamaz: mevcut bir
            // kayıt ATOLYE_SIPARISI'na ÇEVRİLİYORSA usta carisi aynı istekte gelmelidir.
            if (yeniTur == SiparisTuru.AtolyeSiparisi && string.IsNullOrWhiteSpace(s.UstaCariId))
                throw new BadRequestException(
                    "Tür ATOLYE_SIPARISI olarak değiştirilemez: dış atölye siparişinde işi kimin "
                    + "yaptığı (usta carisi) boş bırakılamaz. Aynı istekte ustaCariId gönderin.");
        }

        // Cari değişince mevcut emanet bağı ARTIK BAŞKASININ eşyasını gösterir olabilir.
        if (yeniCariId != s.CariId && !string.IsNullOrWhiteSpace(s.EmanetHareketId))
        {
            var emanetUygun = await db.Hareketler.AsNoTracking().AnyAsync(h =>
                h.Id == s.EmanetHareketId && h.TenantId == islem.TenantId
                && h.Tur == "EMANET_GIRIS" && h.CariId == yeniCariId);
            if (!emanetUygun)
                throw new BadRequestException(
                    "Cari değiştirilemez: bu siparişe bağlı emanet girişi eski cariye aittir. "
                    + "Aynı istekte emanetHareketId alanını null gönderip bağı kaldırın ya da "
                    + "yeni carinin emanet girişini bağlayın.");
        }

        // Tahmini tutar ve kur BİRLİKTE değerlendirilir: tutar varsa kur ZORUNLU.
        var tahmini = s.TahminiTutarTl;
        var kurId = s.KurSnapshotId;
        if (Alan(govde, "tahminiTutarTl", out var ttOge))
            tahmini = ttOge.ValueKind == JsonValueKind.Null ? null
                : KolonTavani.JsonSayi(ttOge, "tahminiTutarTl", "Tahmini tutar");   // tur 3: aralık dışı → 400
        if (Alan(govde, "kurSnapshotId", out var kurOge))
            kurId = kurOge.ValueKind == JsonValueKind.Null ? null : kurOge.GetString();
        (s.TahminiTutarTl, s.KurSnapshotId) = await TahminDogrula(tahmini, kurId, islem.TenantId);

        // ── PAKET S-3a · KRİTİK: kalem PATCH'i tam-gövde DEĞİŞTİRME YAPMAZ ────
        // Eski davranış: gelen kalem dizisi ham DTO'ya deserialize edilir, mevcut
        // küme silinip yeniden yazılırdı. İstemci kalemi yalnız `tanim`+`adet` ile
        // geri gönderdiğinde urunId/ayarId/gramAgirlik SESSİZCE NULL'lanıyordu
        // (QA turu 3, S-2026-000001: ürün bağı + K22 + 12,5 g yok oldu).
        //
        // Yeni sözleşme: kalem de gövde düzeyiyle AYNI kuralı izler —
        //   • `id` verilirse o kalem GÜNCELLENİR; gövdede BULUNMAYAN alan KORUNUR,
        //     açıkça `null` yazılan alan SİLİNİR.
        //   • `id` verilmezse yeni kalem doğar.
        //   • Dizide hiç anılmayan mevcut kalem SİLİNİR (küme anlamı korunur).
        // Böylece kalem kimliği de kararlıdır (ekran satır id'si değişmez).
        List<(string? Id, SiparisKalemi Deger)>? yeniKalemler = null;
        List<SiparisKalemi> mevcutKalemler = [];
        if (Alan(govde, "kalemler", out var kalemOge))
        {
            if (kalemOge.ValueKind != JsonValueKind.Array)
                throw new BadRequestException("Kalemler bir dizi olmalı");
            var ogeler = kalemOge.EnumerateArray().ToList();
            if (ogeler.Count == 0)
                throw new BadRequestException("Sipariş en az bir kalem içermeli");

            mevcutKalemler = await db.SiparisKalemleri
                .Where(k => k.SiparisId == s.Id && k.TenantId == islem.TenantId).ToListAsync();

            yeniKalemler = [];
            for (var i = 0; i < ogeler.Count; i++)
            {
                var oge = ogeler[i];
                if (oge.ValueKind != JsonValueKind.Object)
                    throw new BadRequestException($"Kalem {i + 1}: nesne olmalı");

                string? kalemId = null;
                if (Alan(oge, "id", out var kidOge) && kidOge.ValueKind == JsonValueKind.String)
                    kalemId = kidOge.GetString();

                SiparisKalemi? taban = null;
                if (!string.IsNullOrWhiteSpace(kalemId))
                {
                    taban = mevcutKalemler.FirstOrDefault(m => m.Id == kalemId)
                        ?? throw new NotFoundException(
                            $"Kalem {i + 1}: bu siparişte {kalemId} kimlikli kalem yok");
                }

                var dto = KalemBirlestir(oge, taban);
                yeniKalemler.Add((taban?.Id, await KalemKur(dto, i, islem.TenantId)));
            }
        }

        await using var tx = await db.Database.BeginTransactionAsync();
        if (yeniKalemler is not null)
        {
            var korunan = yeniKalemler.Where(y => y.Id is not null).Select(y => y.Id!).ToHashSet();
            foreach (var eski in mevcutKalemler.Where(m => !korunan.Contains(m.Id)))
                db.SiparisKalemleri.Remove(eski);

            foreach (var (kid, k) in yeniKalemler)
            {
                if (kid is not null)
                {
                    var hedef = mevcutKalemler.First(m => m.Id == kid);
                    hedef.UrunId = k.UrunId;
                    hedef.Tanim = k.Tanim;
                    hedef.AyarId = k.AyarId;
                    hedef.GramAgirlik = k.GramAgirlik;
                    hedef.Adet = k.Adet;
                    hedef.Sira = k.Sira;
                    continue;
                }
                db.SiparisKalemleri.Add(new SiparisKalemi
                {
                    Id = Kimlik.Ulid(),
                    TenantId = islem.TenantId,
                    SiparisId = s.Id,
                    UrunId = k.UrunId,
                    Tanim = k.Tanim,
                    AyarId = k.AyarId,
                    GramAgirlik = k.GramAgirlik,
                    Adet = k.Adet,
                    Sira = k.Sira,
                    Olusturuldu = now,
                });
            }
        }

        // ── P-51: başlık değişimi GEÇİŞ DÖKÜMÜNE yazılır (append-only iz) ───────
        // Durum DEĞİŞMEZ (TASLAK → TASLAK); durum listesi ENFLASYONA UĞRAMAZ.
        // Değişiklik `gerekce` alanına BASLIK_DUZENLENDI önekiyle düşer — "bu
        // sipariş kimin, ne türü, hangi şubesi" sorusunun cevabı sonradan sorulur.
        var baslikDegisimi = new List<string>();
        if (yeniCariId != s.CariId) baslikDegisimi.Add($"cari {s.CariId} → {yeniCariId}");
        if (yeniTur != s.Tur) baslikDegisimi.Add($"tür {s.Tur} → {yeniTur}");
        if (yeniSubeId != s.SubeId) baslikDegisimi.Add($"şube {s.SubeId ?? "—"} → {yeniSubeId ?? "—"}");

        if (baslikDegisimi.Count > 0)
        {
            db.SiparisGecisleri.Add(new SiparisGecisi
            {
                Id = Kimlik.Ulid(),
                TenantId = islem.TenantId,
                SiparisId = s.Id,
                OncekiDurum = s.Durum,
                YeniDurum = s.Durum,
                Gerekce = Kirp($"{BaslikDuzenlendiNotu}: {string.Join(" · ", baslikDegisimi)}", 500),
                KullaniciId = islem.Id,
                Olusturuldu = now,
            });

            s.CariId = yeniCariId;
            s.Tur = yeniTur;
            s.SubeId = yeniSubeId;

            logger.LogInformation("[{TenantId}] SIPARIS başlık düzenlendi: {No} ({Degisim})",
                islem.TenantId, s.No, string.Join(" · ", baslikDegisimi));
        }

        s.Guncellendi = now;
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return await Detay(s.Id, islem);
    }

    // ─── Yardımcılar ──────────────────────────────────────────────────────────

    private async Task<SiparisVarligi> Bul(string id, string tenantId) =>
        await db.Siparisler.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId)
            ?? throw new NotFoundException("Sipariş bulunamadı");

    /// <summary>
    /// İYİMSER KİLİT (Bertan koşulu): istemci gördüğü damgayı gönderirse ve kayıt
    /// o damgadan sonra değiştiyse 409. Damga gönderilmezse kural uygulanmaz
    /// (eski istemciler kırılmaz), ama ekran her zaman gönderir.
    /// </summary>
    private static void IyimserKilitDogrula(SiparisVarligi s, DateTime? damga)
    {
        if (damga is not { } d) return;

        // ⚠ CANLI DOĞRULAMADA YAKALANDI (2026-09-16): burada koşulsuz
        //   ToUniversalTime() vardı. MySQL'den okunan damga Kind=Unspecified
        //   döner ve JSON'a "…554933" (Z'siz) yazılır; istemci onu aynen geri
        //   gönderince ToUniversalTime() değeri YEREL sanıp +3 saat kaydırıyor,
        //   GÜNCEL damga bile 409 alıyordu. Kilit "her geçişi reddeden" bir
        //   kilide dönüşmüştü. Artık yalnız AÇIK ofsetli (Kind=Local) damga
        //   çevrilir; Utc ve Unspecified zaten UTC'dir.
        var gelen = d.Kind == DateTimeKind.Local ? d.ToUniversalTime() : d;
        var kayitli = DateTime.SpecifyKind(s.Guncellendi, DateTimeKind.Unspecified);
        gelen = DateTime.SpecifyKind(gelen, DateTimeKind.Unspecified);

        // MySQL datetime(6) yuvarlaması: saniyenin altında 1 ms tolerans.
        if (Math.Abs((kayitli - gelen).TotalMilliseconds) > 1)
            throw new ConflictException(
                "Bu sipariş siz görüntüledikten sonra değişti. Sayfayı yenileyip tekrar deneyin.");
    }

    private static DateTime? Damga(JsonElement govde) =>
        Alan(govde, "guncellendi", out var o) && o.ValueKind == JsonValueKind.String
            ? o.GetDateTime()
            : null;

    private static bool Alan(JsonElement govde, string ad, out JsonElement oge)
    {
        foreach (var p in govde.EnumerateObject())
            if (string.Equals(p.Name, ad, StringComparison.OrdinalIgnoreCase))
            {
                oge = p.Value;
                return true;
            }
        oge = default;
        return false;
    }

    /// <summary>
    /// PAKET GOC-C (envanter #9 · K8c): söz verilen gün bugünden ÖNCE olamaz (sipariş anında "gecikmiş"
    /// doğardı) ve en çok <see cref="AlanSinirlari.SiparisSozEnCokYil"/> yıl ileri olabilir (İŞ KURALI).
    /// Ekran <c>Tarih Tur=Vade EnGec=…</c> ile AYNI Core sınıfı (TarihPenceresi); ileti GirdiSozlugu'ndan.
    /// </summary>
    public static DateOnly? SozGunuDogrula(DateOnly? gun)
    {
        if (gun is not { } g) return gun;
        var bugun = Zaman.YerelBugun();
        Common.Dogrulama.GirdiKapisi.Denetle(
            TarihPenceresi.Denetle(g, TarihTuru.Vade, bugun, enGec: bugun.AddYears(AlanSinirlari.SiparisSozEnCokYil)),
            "sozVerilenGun");
        return g;
    }

    /// <summary>Tahmini tutar varsa kur ZORUNLU ve aynı kiracıda olmalı.</summary>
    private async Task<(decimal?, string?)> TahminDogrula(decimal? tutar, string? kurId, string tenantId)
    {
        if (tutar is null) return (null, string.IsNullOrWhiteSpace(kurId) ? null : kurId);
        if (tutar < 0) throw new BadRequestException("Tahmini tutar negatif olamaz");
        // PATCH-SOZLESME tur 3: siparisler.tahmini_tutar_tl decimal(14,2) — taşan değer 500 değil 400.
        KolonTavani.Denetle(tutar, 2, KolonTavani.D14_2, "tahminiTutarTl", "Tahmini tutar");
        if (string.IsNullOrWhiteSpace(kurId))
            throw new BadRequestException(
                "Tahmini tutar verildiyse kur kaydı (kurSnapshotId) zorunludur — hangi kurdan söz verildiği sonradan sorulur");
        await IslemDogrulayici.KurDogrula(db, kurId, tenantId);
        return (tutar, kurId);
    }

    /// <summary>
    /// TAMIR'de müşteri eşyasının EMANET_GIRIS hareketi — OPSİYONEL.
    /// Verilirse: aynı kiracıda, EMANET_GIRIS türünde ve AYNI cariye ait olmalı.
    /// </summary>
    // ═══ PAKET P-26 (Y-36) — DIŞ USTA CARİSİ ════════════════════════════════
    /// <summary>
    /// Usta carisini doğrular. <b>Metal çıkışı YAZMAZ</b> — K-13 mührü gereği
    /// imalat/atölye katmanı 1.0 kapsamı dışındadır; burada tutulan tek şey
    /// işi kimin yaptığıdır.
    /// </summary>
    private async Task<string?> UstaDogrula(string? ustaCariId, string tur, string tenantId)
    {
        if (string.IsNullOrWhiteSpace(ustaCariId)) return null;
        if (tur is not (SiparisTuru.AtolyeSiparisi or SiparisTuru.Tamir))
            throw new BadRequestException(
                "Usta carisi yalnız ATOLYE_SIPARISI ve TAMIR türündeki siparişe bağlanabilir");
        await IslemDogrulayici.CariDogrula(db, ustaCariId, tenantId);
        return ustaCariId;
    }

    // ═══ PAKET P-26 (Y-30) — TAMİR/SERVİS KAPORASI DEFTERE BAĞLANIR ═════════
    /// <summary>
    /// Kaporayı GERÇEK tahsilat hareketine çevirir.
    ///
    /// <para>
    /// ⚠ İKİNCİ PARA YOLU AÇILMAZ: gövde olduğu gibi
    /// <c>CariServisi.TahsilatGirHareket</c>'e devredilir — kasa bacağı, MASAK
    /// kümülatifi, tevsik kapısı, denetim izi ve HareketFabrikasi ORADADIR.
    /// Sipariş modülü yalnız doğan hareketin kimliğini saklar. Kapora "sipariş
    /// tablosunda bir tutar alanı" olsaydı para kasaya girer ama deftere
    /// girmezdi (P-26 sayacı DEFTERE_YAZMAYAN_PARA_MODULU'nun sebebi).
    /// </para>
    /// </summary>
    public async Task<object> Kapora(string id, SiparisKaporaDto dto, AktifKullanici islem)
    {
        var servis = cari ?? throw new InvalidOperationException(
            "CariServisi bağlanmadan kapora alınamaz (tek tahsilat yolu).");

        // PAKET KAPORA-ESZAMAN (doğrulayıcı KRİTİK): kilit + kontrol + tahsilat + sipariş bağı
        // TEK işlemde. Eskiden kontrol ayrı okunuyor, TahsilatGirHareket KENDİ işlemini
        // commit ediyor, bağ ayrı SaveChanges ile yazılıyordu → iki eşzamanlı istek ikisi de
        // "kapora yok" görüp İKİ tahsilat yazıyordu (kasa iki kat, biri siparişe bağsız).
        // Şimdi: işlem açılır → sipariş satırı FOR UPDATE ile kilitlenir (ikinci istek BEKLER)
        // → sipariş ve kapora durumu kilitten SONRA okunur → ikinci istek 409 alır.
        // PAKET MASAK-ESZAMAN: READ COMMITTED — TahsilatGirHareket'in cari kilidi sonrası MASAK/tevsik
        // toplamı, burada sipariş okumasının kurduğu anlık görüntüye takılmaz (güncel toplamı görür).
        await using var tx = await db.Database.BaslatVeyaDevral();
        await db.Database.Kilitle(KilitTablosu.Siparisler, id, islem.TenantId);
        var s = await Bul(id, islem.TenantId);
        // PAKET EKSIK-B1-YAZDIR (B1-5): kilit TEK yerde (KaporaKilidi) — ekranın okuduğu
        // kaporaKilitSebebi ile sunucunun reddi AYNI cümledir. Stornolanmış kapora
        // "alınmış" sayılmaz: yanlış tutar geri alınınca doğrusu yeniden alınabilir.
        var mevcut = await KaporaOzeti(s.KaporaHareketId, islem.TenantId);
        if (KaporaKilidi(s.Durum, mevcut) is { } kilit)
            throw new ConflictException(kilit);
        if ((dto.TlTutari ?? 0m) <= 0m)
            throw new BadRequestException("Kapora tutarı sıfırdan büyük olmalı").AlanIle("tlTutari");
        // PAKET EKSIK-B1-YAZDIR (hasım): kapora GERÇEK paradır — açık hesaba (VERESIYE)
        // ya da tanımsız bir araçla "kapora" yazılmaz (avans ucu Y-17 ile aynı kapalı liste).
        var odeme = string.IsNullOrWhiteSpace(dto.OdemeTuru) ? "NAKIT" : dto.OdemeTuru!.Trim();
        if (odeme is not ("NAKIT" or "KART" or "HAVALE"))
            throw new BadRequestException("Kapora nakit, kart ya da havale ile alınır (açık hesaba kapora yazılmaz).")
                .AlanIle("odemeTuru");

        // DEFTER-DELEGE: kapora defteri sipariş modülünden DEĞİL, cari tahsilat
        // gövdesinden yazılır. Sayaç: denetim.sh P-26 DEFTERE_YAZMAYAN_PARA_MODULU.
        var hareketId = await servis.TahsilatGirHareket(s.CariId, new Cari.TahsilatGirisDto
        {
            TlTutari = dto.TlTutari,
            OdemeTuru = odeme,
            KasaId = dto.KasaId,
            KurSnapshotId = dto.KurSnapshotId!,
            Aciklama = Kirp(dto.Aciklama, 500) ?? $"Sipariş kaporası · {s.No}",
            Masak = dto.Masak,   // KAPORA-ESZAMAN tur 2: MASAK beyanı tahsilat kapısına
        }, islem);

        // PAKET KAPORA-ESZAMAN (DÜŞÜK): kapora geçmişi kaybolmasın — tahsilat satırı siparişe
        // DEFTERDE de bağlanır (hareketler.siparis_id). s.KaporaHareketId yalnız GÜNCEL kaporayı
        // gösterir; geri verilip yeniden alınan eski kaporalar Detay'da kaporaGecmisi olarak
        // defterden listelenir. Satır izlenen varlık olarak güncellenir (ExecuteUpdate yok;
        // SiparisiSatisaCevir'deki siparis_id damgasıyla aynı yol).
        var kaporaSatiri = await db.Hareketler
            .FirstOrDefaultAsync(h => h.Id == hareketId && h.TenantId == islem.TenantId)
            ?? throw new InvalidOperationException("Kapora tahsilat satırı işlem içinde bulunamadı.");
        kaporaSatiri.SiparisId = s.Id;

        s.KaporaHareketId = hareketId;
        s.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();   // kilit burada bırakılır

        logger.LogInformation("[{TenantId}] SIPARIS KAPORA: {No} → hareket {HareketId}",
            islem.TenantId, s.No, hareketId);

        // UX-S27: sonuç banda yazılacak veriyle döner.
        return new { siparis = await Detay(id, islem), kaporaHareketId = hareketId };
    }

    // ═══ PAKET EKSIK-B1-YAZDIR (B1-5) — KAPORA KARTI ════════════════════════
    /// <summary>Kaporanın defter satırından okunan özeti (tutar · tarih · kasa · storno).</summary>
    public sealed record KaporaBilgisi(decimal Tutar, DateTime Tarih, string? KasaId, bool Stornolu);

    private async Task<KaporaBilgisi?> KaporaOzeti(string? hareketId, string tenantId)
    {
        if (string.IsNullOrWhiteSpace(hareketId)) return null;
        var h = await db.Hareketler.AsNoTracking()
            .Where(x => x.Id == hareketId && x.TenantId == tenantId)
            .Select(x => new { x.TlTutari, x.Tarih })
            .FirstOrDefaultAsync();
        if (h is null) return null;
        var kasaId = await db.KasaHareketleri.AsNoTracking()
            .Where(k => k.HareketId == hareketId && k.TenantId == tenantId)
            .Select(k => k.KasaId).FirstOrDefaultAsync();
        var stornolu = await db.Hareketler.AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.StornoKaynakId == hareketId);
        return new KaporaBilgisi(Math.Abs(h.TlTutari), h.Tarih, kasaId, stornolu);
    }

    /// <summary>
    /// PAKET KAPORA-ESZAMAN — kapora geçmişi DEFTERDEN: siparişe bağlı (hareketler.siparis_id)
    /// TAHSILAT satırları + güncel bağ (eski veride siparis_id boş olabilir). Storno satırları
    /// listelenmez; her kaporanın geri verilip verilmediği ve ne zaman yazılır.
    /// </summary>
    public const int KaporaGecmisSiniri = 100;

    public sealed record KaporaGecmisKaydi(
        string HareketId, DateTime Tarih, decimal Tutar, string? OdemeTuru,
        bool Stornolu, DateTime? StornoTarihi, bool Guncel);

    private async Task<List<KaporaGecmisKaydi>> KaporaGecmisi(string siparisId, string? guncelId, string tenantId)
    {
        var satirlar = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.Tur == "TAHSILAT" && h.StornoKaynakId == null
                        && (h.SiparisId == siparisId || h.Id == guncelId))
            .OrderByDescending(h => h.Tarih)
            .Select(h => new { h.Id, h.Tarih, h.TlTutari, h.OdemeTuru })
            .Take(KaporaGecmisSiniri)   // B-8: aynı anda tek canlı kapora olur, 100 satır pratikte erişilmez
            .ToListAsync();
        if (satirlar.Count == 0) return [];
        var kimlikler = satirlar.Select(x => x.Id).ToList();
        var stornolar = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.StornoKaynakId != null && kimlikler.Contains(h.StornoKaynakId))
            .Select(h => new { Kaynak = h.StornoKaynakId!, h.Tarih })
            .Take(KaporaGecmisSiniri)
            .ToListAsync();
        return [.. satirlar
            .Select(x =>
            {
                var st = stornolar.FirstOrDefault(z => z.Kaynak == x.Id);
                return new KaporaGecmisKaydi(x.Id, x.Tarih, Math.Abs(x.TlTutari), x.OdemeTuru,
                    st is not null, st?.Tarih, x.Id == guncelId);
            })
            .OrderByDescending(x => x.Tarih).ThenByDescending(x => x.HareketId)];
    }

    /// <summary>
    /// Kapora neden alınamaz? null = alınabilir. Terminal sipariş (iptal · teslim)
    /// para kabul etmez; canlı (stornolanmamış) kapora varken ikincisi yazılmaz.
    /// </summary>
    public static string? KaporaKilidi(string durum, KaporaBilgisi? kapora) =>
        durum == SiparisDurumu.Iptal ? "İptal edilmiş siparişe kapora alınamaz."
        : durum == SiparisDurumu.TeslimEdildi ? "Teslim edilmiş siparişe kapora alınamaz."
        : kapora is { Stornolu: false }
            ? "Bu siparişte kapora zaten alınmış. Değiştirmek için önce kaporayı geri verin."
            : null;

    private async Task<string?> EmanetDogrula(string? hareketId, string tur, string cariId, string tenantId)
    {
        if (string.IsNullOrWhiteSpace(hareketId)) return null;
        if (tur != SiparisTuru.Tamir)
            throw new BadRequestException("Emanet hareketi yalnız TAMIR türündeki siparişe bağlanabilir");

        var uygun = await db.Hareketler.AsNoTracking().AnyAsync(h =>
            h.Id == hareketId && h.TenantId == tenantId
            && h.Tur == "EMANET_GIRIS" && h.CariId == cariId);
        if (!uygun)
            throw new NotFoundException("Emanet hareketi bulunamadı (aynı kiracı ve aynı cari olmalı)");
        return hareketId;
    }

    /// <summary>
    /// PAKET S-3a — kalem düzeyinde PATCH birleştirmesi.
    /// <para>
    /// <paramref name="taban"/> varsa (id ile eşleşen mevcut kalem) gövdede
    /// BULUNMAYAN her alan tabandan devralınır; gövdede açıkça <c>null</c>
    /// yazılan alan SİLİNİR. Taban yoksa (yeni kalem) alanlar gövdeden okunur.
    /// </para>
    /// </summary>
    private static SiparisKalemDto KalemBirlestir(JsonElement oge, SiparisKalemi? taban)
    {
        var dto = new SiparisKalemDto
        {
            UrunId = taban?.UrunId,
            Tanim = taban?.Tanim,
            AyarId = taban?.AyarId,
            GramAgirlik = taban?.GramAgirlik,
            Adet = taban?.Adet,
        };

        if (Alan(oge, "urunId", out var u))
            dto.UrunId = u.ValueKind == JsonValueKind.Null ? null : u.GetString();
        if (Alan(oge, "tanim", out var t))
            dto.Tanim = t.ValueKind == JsonValueKind.Null ? null : t.GetString();
        if (Alan(oge, "ayarId", out var a))
            dto.AyarId = a.ValueKind == JsonValueKind.Null ? null : a.GetString();
        if (Alan(oge, "gramAgirlik", out var g))
            dto.GramAgirlik = g.ValueKind == JsonValueKind.Null ? null : KolonTavani.JsonSayi(g, "gramAgirlik", "Gram ağırlık");
        if (Alan(oge, "adet", out var ad))
            dto.Adet = ad.ValueKind == JsonValueKind.Null ? null : KolonTavani.JsonSayi(ad, "adet", "Adet");

        return dto;
    }

    private async Task<SiparisKalemi> KalemKur(SiparisKalemDto dto, int sira, string tenantId)
    {
        var urunId = string.IsNullOrWhiteSpace(dto.UrunId) ? null : dto.UrunId.Trim();
        var tanim = Kirp(dto.Tanim, 200);
        if (urunId is null && string.IsNullOrWhiteSpace(tanim))
            throw new BadRequestException($"Kalem {sira + 1}: ürün seçilmeli ya da tanım yazılmalı");

        if (urunId is not null)
        {
            var var_ = await db.Urunler.AsNoTracking()
                .AnyAsync(u => u.Id == urunId && u.TenantId == tenantId);
            if (!var_) throw new NotFoundException($"Kalem {sira + 1}: ürün bulunamadı");
        }

        string? ayarId = null;
        if (!string.IsNullOrWhiteSpace(dto.AyarId))
        {
            ayarId = dto.AyarId.Trim();
            if (Ayarlar.Bul(ayarId) is null)
                throw new BadRequestException($"Kalem {sira + 1}: geçersiz ayar: {ayarId}");
        }

        if (dto.GramAgirlik is < 0)
            throw new BadRequestException($"Kalem {sira + 1}: gram ağırlık negatif olamaz");
        // PAKET GOC-C: K10 gram > 0 (0 gramlık kalem anlamsız; 3 ondalık) · K14 adet tam sayı ≥ 1 — servis yolunda da.
        if (dto.GramAgirlik is { } gk)
            Common.Dogrulama.GirdiKapisi.Gram(gk, "gramAgirlik", $"Kalem {sira + 1}");
        if (dto.Adet is { } ak)
            Common.Dogrulama.GirdiKapisi.Adet(ak, "adet", $"Kalem {sira + 1}");

        // PAKET PATCH-2: önce kolon hanesine (decimal(12,3)) yuvarla, sonra sıfır kontrolü — 0,0004 kontrolden geçip 0,000 yazılmaz.
        var adet = Math.Round(dto.Adet ?? 1m, 3, MidpointRounding.AwayFromZero);
        if (adet <= 0) throw new BadRequestException($"Kalem {sira + 1}: adet sıfırdan büyük olmalı").AlanIle("adet");
        // PATCH-SOZLESME tur 3: siparis_kalemleri.adet / gram_agirlik decimal(12,3) kapasitesi.
        KolonTavani.Denetle(adet, 3, KolonTavani.D12_3, "adet", $"Kalem {sira + 1}: adet");
        KolonTavani.Denetle(dto.GramAgirlik, 3, KolonTavani.D12_3, "gramAgirlik", $"Kalem {sira + 1}: gram ağırlık");

        return new SiparisKalemi
        {
            UrunId = urunId,
            Tanim = string.IsNullOrWhiteSpace(tanim) ? null : tanim,
            AyarId = ayarId,
            GramAgirlik = dto.GramAgirlik,
            Adet = adet,
            Sira = sira,
        };
    }

    private static string? Kirp(string? metin, int azami)
    {
        if (metin is null) return null;
        var t = metin.Trim();
        if (t.Length == 0) return null;
        return t.Length <= azami ? t : t[..azami];
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

/// <summary>
/// Sipariş uçları. Sunucu kapısı: <c>[ModulYetkisi("siparis")]</c> — ekranda
/// bağlantının gizlenmesi TEK KAPI SAYILMAZ.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/siparisler")]
[Yetki.ModulYetkisi("siparis")]
// ⚠ PAKET P-21 (Y-18): SatisServisi BURAYA enjekte edildi. Dönüşüm mantığı
// SatisModulu.P21.cs'tedir (stok/kasa/cari yardımcıları orada); uç ise siparişin
// kendi rotasında durur — kullanıcı için bu bir SİPARİŞ eylemidir.
public sealed class SiparisController(SiparisServisi servis, Satis.SatisServisi satis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/siparisler — Sipariş oluştur (TASLAK doğar)</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<object> Olustur([FromBody] SiparisOlusturDto dto) => servis.Olustur(dto, Islem);

    /// <summary>GET /api/v1/siparisler — Liste (azami 200 satır)</summary>
    [HttpGet]
    public Task<object> Listele(
        [FromQuery] string? tur, [FromQuery] string? durum, [FromQuery] string? cariId,
        [FromQuery] string? subeId, [FromQuery] bool? geciken, [FromQuery] int? limit)
        => servis.Listele(tur, durum, cariId, subeId, geciken, limit, Islem);

    /// <summary>GET /api/v1/siparisler/{id} — Tam kart</summary>
    /// <summary>GET /api/v1/siparisler/ozet — PAKET-03 Faz B: açık/geciken/bugün teslim sayıları ve durum kırılımı.</summary>
    [HttpGet("ozet")]
    public Task<object> Ozet() => servis.Ozet(Islem);

    [HttpGet("{id}")]
    public Task<object> Detay(string id) => servis.Detay(id, Islem);

    /// <summary>PATCH /api/v1/siparisler/{id} — Yalnız TASLAK düzenlenir</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<object> Guncelle(string id, [FromBody] JsonElement govde)
        => servis.Guncelle(id, govde, Islem);

    /// <summary>
    /// POST /api/v1/siparisler/{id}/kapora — PAKET P-26 (Y-30).
    /// Kaporayı GERÇEK tahsilat hareketine çevirir (CariServisi tek yolu).
    /// </summary>
    [HttpPost("{id}/kapora")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<object> Kapora(string id, [FromBody] SiparisKaporaDto dto) => servis.Kapora(id, dto, Islem);

    /// <summary>POST /api/v1/siparisler/{id}/gecis — Durum geçişi (IPTAL: SAHIP/MUDUR)</summary>
    [HttpPost("{id}/gecis")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<object> Gecis(string id, [FromBody] SiparisGecisDto dto) => servis.Gecis(id, dto, Islem);

    /// <summary>
    /// POST /api/v1/siparisler/{id}/satisa-cevir — PAKET P-21 (Y-18).
    /// HAZIR siparişi TEK transaction içinde satışa çevirir; yarım dönüşüm imkânsızdır.
    /// </summary>
    [HttpPost("{id}/satisa-cevir")]
    [RolGerekli("SAHIP", "MUDUR", "TEZGAHTAR")]
    public async Task<IActionResult> SatisaCevir(
        string id, [FromBody] Satis.SiparisSatisaCevirDto dto)
        => StatusCode(201, await satis.SiparisiSatisaCevir(id, dto, Islem));

    /// <summary>GET /api/v1/siparisler/{id}/gecisler — Append-only geçiş dökümü</summary>
    [HttpGet("{id}/gecisler")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Gecisler(string id) => servis.Gecisler(id, Islem);
}
