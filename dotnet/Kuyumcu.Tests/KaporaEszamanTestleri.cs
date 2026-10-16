using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MEmanet = Kuyumcu.Api.Modules.Emanet;
using MKasa = Kuyumcu.Api.Modules.Kasa;
using MBanka = Kuyumcu.Api.Modules.Banka;
using MGider = Kuyumcu.Api.Modules.Gider;
using MSatis = Kuyumcu.Api.Modules.Satis;
using MSiparis = Kuyumcu.Api.Modules.Siparis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KAPORA-ESZAMAN — "kontrol et → ayrı işlemde yaz" sınıfı.
// Her eşzamanlı test N ayrı DbContext (= ayrı MySQL bağlantısı) ile AYNI anda başlar
// (başlangıç kapısı); beklenen değerler SABİT ve elle hesaplandı.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class KaporaEszamanTestleri(TestVeritabani vt)
{
    private const int N = 10;

    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));

    private static MSiparis.SiparisServisi Siparis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSiparis.SiparisServisi>.Instance, Cari(db));

    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MSatis.StornoServisi Storno(KuyumcuDbContext db) =>
        new(db, new DenetimServisi(db, new HttpContextAccessor()), NullLogger<MSatis.StornoServisi>.Instance);

    private static MKasa.KasaServisi Kasa(KuyumcuDbContext db) =>
        new(db, NullLogger<MKasa.KasaServisi>.Instance);

    private static MEmanet.EmanetServisi Emanet(KuyumcuDbContext db) =>
        new(db, NullLogger<MEmanet.EmanetServisi>.Instance);

    private static JsonElement J(object yanit) =>
        JsonSerializer.SerializeToElement(yanit, yanit.GetType(), new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private Kiraci Kur(string ad)
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETKAPORA-ESZAMAN-TEST " + ad, stokAdet: 5m);
        sistem.SaveChanges();
        return k;
    }

    private static async Task<string> SiparisAc(KuyumcuDbContext db, Kiraci k, bool urunlu = false)
    {
        var y = J(await Siparis(db).Olustur(new MSiparis.SiparisOlusturDto
        {
            Tur = SiparisTuru.MusteriSiparisi, CariId = k.CariId, SubeId = k.SubeId, Aciklama = "eşzamanlı",
            Kalemler = [urunlu
                ? new MSiparis.SiparisKalemDto { UrunId = k.UrunId, Adet = 1m }
                : new MSiparis.SiparisKalemDto { Tanim = "bilezik", Adet = 1m }],
        }, k.Islem));
        return y.GetProperty("id").GetString()!;
    }

    private static MSiparis.SiparisKaporaDto Govde(Kiraci k, decimal tutar = 1_000m) => new()
    {
        TlTutari = tutar, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
    };

    /// <summary>
    /// N işi N ayrı bağlamda AYNI anda başlatır; her işin sonucu: null = başarılı, yoksa istisna.
    /// </summary>
    private async Task<List<Exception?>> Paralel(int n, Func<KuyumcuDbContext, Task> is_)
    {
        var baglamlar = Enumerable.Range(0, n).Select(_ => vt.OlusturSistem()).ToList();
        try
        {
            var kapi = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var gorevler = baglamlar.Select(db => Task.Run(async () =>
            {
                await kapi.Task;
                try { await is_(db); return (Exception?)null; }
                catch (Exception e) { return e; }
            })).ToList();
            kapi.SetResult();
            return [.. await Task.WhenAll(gorevler)];
        }
        finally
        {
            foreach (var b in baglamlar) b.Dispose();
        }
    }

    private static void TekBasariDigerleri<TRed>(List<Exception?> sonuc) where TRed : Exception
    {
        var beklenmeyen = sonuc.Where(e => e is not null && e is not TRed).ToList();
        Assert.True(beklenmeyen.Count == 0,
            "Beklenmeyen istisna: " + string.Join(" | ", beklenmeyen.Select(e => e!.GetType().Name + ": " + e.Message)));
        Assert.Equal(1, sonuc.Count(e => e is null));
        Assert.Equal(sonuc.Count - 1, sonuc.Count(e => e is TRed));
    }

    // ── KRİTİK: 10 eşzamanlı kapora → kasada TEK hareket, 9 istek 409, bağ o harekete ─────
    [Fact]
    public async Task KE01_on_paralel_kapora_tek_tahsilat_digerleri_409()
    {
        var k = Kur("KE01");
        string id;
        decimal kasaOnce;
        using (var db0 = vt.OlusturSistem())
        {
            id = await SiparisAc(db0, k);
            kasaOnce = TestKurulum.KasaBakiye(db0, k.KasaId);
        }

        var sonuc = await Paralel(N, db => Siparis(db).Kapora(id, Govde(k), k.Islem));
        TekBasariDigerleri<ConflictException>(sonuc);
        Assert.All(sonuc.OfType<ConflictException>(), e => Assert.Contains("kapora zaten alınmış", e.Message));

        using var kontrol = vt.OlusturSistem();
        var tahsilatlar = await kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT").ToListAsync();
        var tek = Assert.Single(tahsilatlar);
        Assert.Equal(-1_000m, tek.TlTutari);
        Assert.Equal(id, tek.SiparisId);                                               // defterde de bağlı
        Assert.Equal(1, await kontrol.KasaHareketleri.CountAsync(x => x.TenantId == k.TenantId && x.KasaId == k.KasaId));
        Assert.Equal(kasaOnce + 1_000m, TestKurulum.KasaBakiye(kontrol, k.KasaId));    // iki kat DEĞİL
        Assert.Equal(-1_000m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
        Assert.Equal(tek.Id, (await kontrol.Siparisler.AsNoTracking().FirstAsync(x => x.Id == id)).KaporaHareketId);
    }

    // ── Geriye uyum 1: TahsilatGirHareket tek başına çağrılınca KENDİ işlemini açar ve commit eder ─
    [Fact]
    public async Task KE02_tahsilat_tek_basina_kendi_islemini_commit_eder()
    {
        var k = Kur("KE02");
        string hareketId;
        using (var db = vt.OlusturSistem())
        {
            hareketId = await Cari(db).TahsilatGirHareket(k.CariId, new MCari.TahsilatGirisDto
            {
                TlTutari = 250m, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);
            Assert.Null(db.Database.CurrentTransaction);   // açık işlem bırakmadı
        }
        using var kontrol = vt.OlusturSistem();
        Assert.True(await kontrol.Hareketler.AnyAsync(h => h.Id == hareketId && h.TenantId == k.TenantId));
        Assert.Equal(-250m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
    }

    // ── Geriye uyum 2: dış işlem varsa ona KATILIR — dış işlem geri alınırsa iz kalmaz ───────
    [Fact]
    public async Task KE03_tahsilat_dis_isleme_katilir_geri_alininca_iz_kalmaz()
    {
        var k = Kur("KE03");
        decimal kasaOnce;
        using (var db = vt.OlusturSistem())
        {
            kasaOnce = TestKurulum.KasaBakiye(db, k.KasaId);
            await using var tx = await db.Database.BeginTransactionAsync();
            await Cari(db).TahsilatGirHareket(k.CariId, new MCari.TahsilatGirisDto
            {
                TlTutari = 250m, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);
            Assert.Same(tx, db.Database.CurrentTransaction);   // commit SAHİBİNDE kaldı
            await tx.RollbackAsync();
        }
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT"));
        Assert.Equal(kasaOnce, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
    }

    // ── Atomik: tahsilat reddedilirse sipariş bağı da yazılmaz ──────────────────────────────
    [Fact]
    public async Task KE04_tahsilat_reddedilirse_bag_ve_defter_bos()
    {
        var k = Kur("KE04");
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k);
        var g = Govde(k);
        g.KasaId = "01KASAYOKKASAYOKKASAYOK000";
        await Assert.ThrowsAnyAsync<ApiException>(() => Siparis(db).Kapora(id, g, k.Islem));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT"));
        Assert.Null((await kontrol.Siparisler.AsNoTracking().FirstAsync(x => x.Id == id)).KaporaHareketId);
    }

    // ── DÜŞÜK: geri verilip yeniden alınan kapora — eski bağ kaybolmaz (geçmiş defterden) ─────
    [Fact]
    public async Task KE05_kapora_gecmisi_geri_verilen_kaybolmaz()
    {
        var k = Kur("KE05");
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k);
        var ilk = J(await Siparis(db).Kapora(id, Govde(k, 1_000m), k.Islem)).GetProperty("kaporaHareketId").GetString()!;
        using (var dbS = vt.OlusturSistem())
            await Storno(dbS).Storno(ilk, new MSatis.StornoDto
            {
                Gerekce = "Yanlış tutar alındı, kapora müşteriye geri verildi", KurSnapshotId = k.KurId, KasaId = k.KasaId,
            }, k.Islem);
        string ikinci;
        using (var db2 = vt.OlusturSistem())
            ikinci = J(await Siparis(db2).Kapora(id, Govde(k, 1_500m), k.Islem)).GetProperty("kaporaHareketId").GetString()!;

        using var kontrol = vt.OlusturSistem();
        var d = J(await Siparis(kontrol).Detay(id, k.Islem));
        var gecmis = d.GetProperty("kaporaGecmisi").EnumerateArray().ToList();
        Assert.Equal(2, gecmis.Count);
        var yeni = gecmis.Single(x => x.GetProperty("hareketId").GetString() == ikinci);
        var eski = gecmis.Single(x => x.GetProperty("hareketId").GetString() == ilk);
        Assert.Equal(1_500m, yeni.GetProperty("tutar").GetDecimal());
        Assert.True(yeni.GetProperty("guncel").GetBoolean());
        Assert.False(yeni.GetProperty("stornolu").GetBoolean());
        Assert.Equal(1_000m, eski.GetProperty("tutar").GetDecimal());
        Assert.True(eski.GetProperty("stornolu").GetBoolean());
        Assert.False(eski.GetProperty("guncel").GetBoolean());
        Assert.Equal(JsonValueKind.String, eski.GetProperty("stornoTarihi").ValueKind);
        Assert.Equal("HAVALE", eski.GetProperty("odemeTuru").GetString());
        // Sıra: en yeni önce.
        Assert.Equal(ikinci, gecmis[0].GetProperty("hareketId").GetString());
    }

    // ── Sınıf: 10 eşzamanlı storno (kapora geri verme) → TEK ters kayıt ──────────────────────
    [Fact]
    public async Task KE06_on_paralel_storno_tek_ters_kayit()
    {
        var k = Kur("KE06");
        string hareketId;
        decimal kasaOnce;
        using (var db0 = vt.OlusturSistem())
        {
            kasaOnce = TestKurulum.KasaBakiye(db0, k.KasaId);
            var id = await SiparisAc(db0, k);
            hareketId = J(await Siparis(db0).Kapora(id, Govde(k), k.Islem)).GetProperty("kaporaHareketId").GetString()!;
        }
        var sonuc = await Paralel(N, db => Storno(db).Storno(hareketId, new MSatis.StornoDto
        {
            Gerekce = "Eşzamanlı storno sınaması: kapora geri verildi", KurSnapshotId = k.KurId, KasaId = k.KasaId,
        }, k.Islem));
        TekBasariDigerleri<ConflictException>(sonuc);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.StornoKaynakId == hareketId));
        Assert.Equal(kasaOnce, TestKurulum.KasaBakiye(kontrol, k.KasaId));             // +1000 −1000, iki kez DEĞİL
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
    }

    // ── Sınıf: 10 eşzamanlı elle kasa hareketi stornosu → TEK ters satır ─────────────────────
    [Fact]
    public async Task KE07_on_paralel_kasa_hareketi_storno_tek_ters_satir()
    {
        var k = Kur("KE07");
        string kasaHareketId;
        decimal kasaOnce;
        using (var db0 = vt.OlusturSistem())
        {
            kasaOnce = TestKurulum.KasaBakiye(db0, k.KasaId);
            await Kasa(db0).HareketGir(k.KasaId, new MKasa.KasaHareketDto
            {
                Yon = HareketYonu.KasaGiris, Tutar = 700m, OdemeTuru = "NAKIT", Aciklama = "eşzamanlı storno",
            }, k.Islem);
            kasaHareketId = db0.KasaHareketleri.AsNoTracking()
                .Single(h => h.TenantId == k.TenantId && h.KasaId == k.KasaId).Id;
        }
        var sonuc = await Paralel(N, db => Storno(db).KasaHareketiStorno(
            k.KasaId, kasaHareketId, "Eşzamanlı kasa stornosu sınaması: yanlış giriş", k.Islem));
        TekBasariDigerleri<ConflictException>(sonuc);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, await kontrol.KasaHareketleri.CountAsync(h => h.TenantId == k.TenantId && h.StornoKaynakId == kasaHareketId));
        Assert.Equal(kasaOnce, TestKurulum.KasaBakiye(kontrol, k.KasaId));
    }

    // ── Sınıf: 10 eşzamanlı "satışa çevir" → TEK satış, sipariş TESLIM_EDILDI ─────────────────
    [Fact]
    public async Task KE08_on_paralel_satisa_cevir_tek_satis()
    {
        var k = Kur("KE08");
        string id, kalemId;
        using (var db0 = vt.OlusturSistem())
        {
            id = await SiparisAc(db0, k, urunlu: true);
            await Siparis(db0).Gecis(id, new MSiparis.SiparisGecisDto { YeniDurum = SiparisDurumu.Onaylandi }, k.Islem);
            await Siparis(db0).Gecis(id, new MSiparis.SiparisGecisDto { YeniDurum = SiparisDurumu.Hazir }, k.Islem);
            kalemId = await db0.SiparisKalemleri.AsNoTracking().Where(x => x.SiparisId == id).Select(x => x.Id).FirstAsync();
        }
        var sonuc = await Paralel(N, db => Satis(db).SiparisiSatisaCevir(id, new MSatis.SiparisSatisaCevirDto
        {
            KurSnapshotId = k.KurId, VadeGun = 0,
            Kalemler = [new MSatis.SiparisSatisaCevirKalemDto { SiparisKalemId = kalemId, BirimFiyatTl = 3_000m }],
            Odemeler = [new MSatis.OdemeBacagiDto { Arac = "VERESIYE", TutarTl = 3_000m }],
        }, k.Islem));
        TekBasariDigerleri<ConflictException>(sonuc);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
        Assert.Equal(SiparisDurumu.TeslimEdildi, (await kontrol.Siparisler.AsNoTracking().FirstAsync(x => x.Id == id)).Durum);
        Assert.Equal(3_000m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);           // borç bir kez
    }

    // ── Sınıf: 10 eşzamanlı tam iade → TEK iade ─────────────────────────────────────────────
    [Fact]
    public async Task KE09_on_paralel_satis_iadesi_tek_iade()
    {
        var k = Kur("KE09");
        string satisId;
        using (var db0 = vt.OlusturSistem())
        {
            await Satis(db0).Satis(new MSatis.SatisOlusturDto
            {
                UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 2_000m,
                OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId,
            }, k.Islem);
            satisId = await db0.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS").Select(h => h.Id).SingleAsync();
        }
        var sonuc = await Paralel(N, db => Satis(db).SatisIade(new MSatis.IadeDto
        {
            KaynakHareketId = satisId, Adet = 1m, OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId,
        }, k.Islem));
        TekBasariDigerleri<BadRequestException>(sonuc);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "SATIS_IADE"));
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);               // borç bir kez kapandı
    }

    // ── Sınıf: 10 eşzamanlı emanet teslimi (tamamı) → TEK çıkış, emanet eksiye inmez ────────
    [Fact]
    public async Task KE10_on_paralel_emanet_teslimi_tek_cikis()
    {
        var k = Kur("KE10");
        decimal net;
        using (var db0 = vt.OlusturSistem())
        {
            await Emanet(db0).Giris(new MEmanet.EmanetGirisDto
            {
                CariId = k.CariId, AyarId = "K22", GramAgirlik = 10m, KurSnapshotId = k.KurId,
            }, k.Islem);
            net = await db0.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == k.TenantId && h.CariId == k.CariId && h.Tur == "EMANET_GIRIS")
                .SumAsync(h => h.HasGrami);
        }
        Assert.True(net > 0m);
        var sonuc = await Paralel(N, db => Emanet(db).Cikis(k.CariId, new MEmanet.EmanetCikisDto
        {
            HasGrami = net, KurSnapshotId = k.KurId,
        }, k.Islem));
        TekBasariDigerleri<BadRequestException>(sonuc);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "EMANET_CIKIS"));
        var kalan = await kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.CariId == k.CariId && (h.Tur == "EMANET_GIRIS" || h.Tur == "EMANET_CIKIS"))
            .SumAsync(h => h.HasGrami);
        Assert.Equal(0m, kalan);
    }

    // ── Kilit yardımcısı: işlem dışında çağrılırsa sessizce geçmez ─────────────────────────
    [Fact]
    public async Task KE11_satir_kilidi_islem_disinda_firlatir()
    {
        using var db = vt.OlusturSistem();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            db.Database.Kilitle(KilitTablosu.Siparisler, "x", "y"));
    }

    // ═══ TUR 2 — doğrulayıcı bulguları ═══════════════════════════════════════════════════════

    // ── ORTA→KRİTİK: aynı kasa + aynı cariye 10 eşzamanlı tahsilat (her biri meşru) → kasa ve cari
    // bakiyesi KAYIPSIZ: kasa = önce + 10×100, cari TL = önce − 10×100, SUM(kasa_hareketleri) = delta ──
    [Fact]
    public async Task KE12_on_paralel_tahsilat_kasa_ve_cari_bakiyesi_kaybolmaz()
    {
        var k = Kur("KE12");
        decimal kasaOnce, cariOnce;
        using (var db0 = vt.OlusturSistem())
        {
            kasaOnce = TestKurulum.KasaBakiye(db0, k.KasaId);
            cariOnce = TestKurulum.CariBakiye(db0, k.CariId).Tl;
        }
        var sonuc = await Paralel(N, db => Cari(db).TahsilatGirHareket(k.CariId, new MCari.TahsilatGirisDto
        {
            TlTutari = 100m, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        Assert.True(sonuc.All(e => e is null),
            "Beklenmeyen istisna: " + string.Join(" | ", sonuc.Where(e => e is not null).Select(e => e!.GetType().Name + ": " + e.Message)));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(10, await kontrol.KasaHareketleri.CountAsync(h => h.TenantId == k.TenantId && h.KasaId == k.KasaId));
        Assert.Equal(1_000m, await kontrol.KasaHareketleri.Where(h => h.TenantId == k.TenantId && h.KasaId == k.KasaId).SumAsync(h => h.Tutar));
        Assert.Equal(kasaOnce + 1_000m, TestKurulum.KasaBakiye(kontrol, k.KasaId));      // kayıp güncelleme YOK
        Assert.Equal(cariOnce - 1_000m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);   // cari özeti de kayıpsız
    }

    private static string BankaHesabiAc(KuyumcuDbContext db, Kiraci k)
    {
        var id = Kimlik.Ulid();
        db.BankaHesaplari.Add(new BankaHesap
        {
            Id = id, TenantId = k.TenantId, BankaAdi = "Test Bankası", HesapAdi = "Vadesiz",
            Iban = "TR200000000000000000000001", ParaBirimi = "TRY", Bakiye = 0m, Aktif = true,
            Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        db.SaveChanges();
        return id;
    }

    // ── DÜŞÜK: 10 eşzamanlı banka hareketi stornosu → TEK ters satır, hesap bakiyesi bir kez döner ──
    [Fact]
    public async Task KE13_on_paralel_banka_hareketi_storno_tek_ters_satir()
    {
        var k = Kur("KE13");
        string hesapId, bhId;
        using (var db0 = vt.OlusturSistem())
        {
            hesapId = BankaHesabiAc(db0, k);
            await new MBanka.BankaServisi(db0).HareketEkleIc(hesapId, new MBanka.HareketEkleDto
            {
                Yon = HareketYonu.BankaGelen, Tutar = 500m, Aciklama = "eşzamanlı banka stornosu",
            }, k.Islem);
            bhId = db0.BankaHareketleri.AsNoTracking().Single(h => h.TenantId == k.TenantId && h.BankaHesapId == hesapId).Id;
        }
        var sonuc = await Paralel(N, db => Storno(db).BankaHareketiStorno(
            hesapId, bhId, "Eşzamanlı banka stornosu sınaması: yanlış dekont", k.Islem));
        TekBasariDigerleri<ConflictException>(sonuc);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, await kontrol.BankaHareketleri.CountAsync(h => h.TenantId == k.TenantId && h.StornoKaynakId == bhId));
        Assert.Equal(0m, kontrol.BankaHesaplari.AsNoTracking().Single(x => x.Id == hesapId).Bakiye);   // +500 −500, iki kez DEĞİL
    }

    // ── DÜŞÜK: 10 eşzamanlı virman stornosu (kasa → banka) → TEK ters çift, iki bakiye bir kez döner ──
    [Fact]
    public async Task KE14_on_paralel_virman_storno_tek_ters_cift()
    {
        var k = Kur("KE14");
        string hesapId, bacakId;
        decimal kasaOnce;
        using (var db0 = vt.OlusturSistem())
        {
            kasaOnce = TestKurulum.KasaBakiye(db0, k.KasaId);
            hesapId = BankaHesabiAc(db0, k);
            var v = await new MGider.VirmanServisi(db0, NullLogger<MGider.VirmanServisi>.Instance).Olustur(new MGider.VirmanDto
            {
                KaynakTur = "KASA", KaynakId = k.KasaId, HedefTur = "BANKA", HedefId = hesapId,
                Tutar = 300m, Aciklama = "eşzamanlı virman stornosu",
            }, k.Islem);
            bacakId = v.CikisBacakId;
        }
        var sonuc = await Paralel(N, db => Storno(db).VirmanStorno(
            bacakId, "Eşzamanlı virman stornosu sınaması: yanlış aktarım", k.Islem));
        TekBasariDigerleri<ConflictException>(sonuc);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, await kontrol.KasaHareketleri.CountAsync(h => h.TenantId == k.TenantId && h.StornoKaynakId != null));
        Assert.Equal(1, await kontrol.BankaHareketleri.CountAsync(h => h.TenantId == k.TenantId && h.StornoKaynakId != null));
        Assert.Equal(kasaOnce, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Equal(0m, kontrol.BankaHesaplari.AsNoTracking().Single(x => x.Id == hesapId).Bakiye);
    }

    // ── DÜŞÜK: 10 eşzamanlı TAM alış iadesi → TEK iade, kasa bir kez geri alır ──
    [Fact]
    public async Task KE15_on_paralel_alis_iadesi_tek_iade()
    {
        var k = Kur("KE15");
        string alisId;
        decimal kasaOnce;
        using (var db0 = vt.OlusturSistem())
        {
            kasaOnce = TestKurulum.KasaBakiye(db0, k.KasaId);
            await Satis(db0).Alis(new MSatis.AlisOlusturDto
            {
                CariId = k.CariId, UrunId = k.UrunId, Adet = 2m, BirimFiyatTl = 1_000m,
                OdemeTuru = "HAVALE", OdenenTl = 2_000m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);
            alisId = await db0.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == k.TenantId && h.Tur == "ALIS").Select(h => h.Id).SingleAsync();
        }
        var sonuc = await Paralel(N, db => Satis(db).AlisIade(new MSatis.IadeDto
        {
            KaynakHareketId = alisId, Adet = 2m, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        var beklenmeyen = sonuc.Where(e => e is not null && e is not BadRequestException and not ConflictException).ToList();
        Assert.True(beklenmeyen.Count == 0,
            "Beklenmeyen istisna: " + string.Join(" | ", beklenmeyen.Select(e => e!.GetType().Name + ": " + e.Message)));
        Assert.Equal(1, sonuc.Count(e => e is null));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "ALIS_IADE"));
        Assert.Equal(kasaOnce, TestKurulum.KasaBakiye(kontrol, k.KasaId));             // −2000 +2000, bir kez
    }

    // ── DÜŞÜK: satışa çevir kilitte beklerken sipariş HAZIR → ONAYLANDI'ya geri alınırsa durum makinesi
    // kapısı kilitten SONRA yeniden işler → 400, satış YOK ──
    [Fact]
    public async Task KE16_satisa_cevir_kilit_beklerken_durum_geri_alinirsa_400()
    {
        var k = Kur("KE16");
        string id, kalemId;
        using (var db0 = vt.OlusturSistem())
        {
            id = await SiparisAc(db0, k, urunlu: true);
            await Siparis(db0).Gecis(id, new MSiparis.SiparisGecisDto { YeniDurum = SiparisDurumu.Onaylandi }, k.Islem);
            await Siparis(db0).Gecis(id, new MSiparis.SiparisGecisDto { YeniDurum = SiparisDurumu.Hazir }, k.Islem);
            kalemId = await db0.SiparisKalemleri.AsNoTracking().Where(x => x.SiparisId == id).Select(x => x.Id).FirstAsync();
        }
        using var tutucu = vt.OlusturSistem();
        await using var tx = await tutucu.Database.BeginTransactionAsync();
        await tutucu.Database.Kilitle(KilitTablosu.Siparisler, id, k.TenantId);

        using var db1 = vt.OlusturSistem();
        var gorev = Task.Run(() => Satis(db1).SiparisiSatisaCevir(id, new MSatis.SiparisSatisaCevirDto
        {
            KurSnapshotId = k.KurId, VadeGun = 0,
            Kalemler = [new MSatis.SiparisSatisaCevirKalemDto { SiparisKalemId = kalemId, BirimFiyatTl = 3_000m }],
            Odemeler = [new MSatis.OdemeBacagiDto { Arac = "VERESIYE", TutarTl = 3_000m }],
        }, k.Islem));
        await Task.Delay(1_500);
        Assert.False(gorev.IsCompleted, "Satışa çevir sipariş kilidini beklemeliydi.");
        var sip = await tutucu.Siparisler.FirstAsync(x => x.Id == id);
        sip.Durum = SiparisDurumu.Onaylandi;       // kilit sahibi durumu geri aldı
        await tutucu.SaveChangesAsync();
        await tx.CommitAsync();

        await Assert.ThrowsAsync<BadRequestException>(() => gorev);
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
        Assert.Equal(SiparisDurumu.Onaylandi, (await kontrol.Siparisler.AsNoTracking().FirstAsync(x => x.Id == id)).Durum);
    }

    // ── TUR 2 (tasarım hakemi KRİTİK): eşiği aşan kaporanın ÇIKIŞ YOLU — beyansız 400 "MASAK…",
    // aynı tutar tam beyanla 200; beyan tahsilat kapısına aynen geçer (kapora gövdesi Masak taşır) ──
    [Fact]
    public async Task KE17_esik_ustu_kapora_beyansiz_400_beyanla_200()
    {
        var k = Kur("KE17");
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k);
        var red = await Assert.ThrowsAsync<BadRequestException>(() => Siparis(db).Kapora(id, Govde(k, 400_000m), k.Islem));
        Assert.StartsWith("MASAK", red.Message);
        db.ChangeTracker.Clear();
        var govde = Govde(k, 400_000m);
        govde.Masak = new Kuyumcu.Api.Modules.Masak.MasakBilgiDto
        {
            BelgeTuru = KimlikBelgeTuru.TcKimlik, BelgeNo = "10000000146", AdSoyad = "Ayşe Demir",
            DogumTarihi = new DateOnly(1985, 4, 12), Uyruk = "TUR",
            IslemAmaci = "Yatırım", Aciklama = "Düğün için bilezik siparişi kaporası",
            FonKaynagi = "Maaş birikimi", EkBelgeReferansi = "BELGE-2026-001",
        };
        var y = J(await Siparis(db).Kapora(id, govde, k.Islem));
        var hk = y.GetProperty("kaporaHareketId").GetString()!;
        using var kontrol = vt.OlusturSistem();
        var h = await kontrol.Hareketler.AsNoTracking().SingleAsync(x => x.Id == hk);
        Assert.Equal(-400_000m, h.TlTutari);
        Assert.Equal(id, h.SiparisId);
    }
    // ── TUR 2: cari bakiye yazıcısı (CariBakiyeYazici.Uygula) da kilitli okur — kasa kilidinin
    // serileştirmediği yol: 10 eşzamanlı YALNIZ ALTIN tahsilatı (TL 0 → kasa bacağı yok) aynı cariye →
    // has bakiyesi KAYIPSIZ önce − 10 × 1 g ──
    [Fact]
    public async Task KE18_on_paralel_altin_tahsilati_cari_has_bakiyesi_kaybolmaz()
    {
        var k = Kur("KE18");
        decimal hasOnce;
        using (var db0 = vt.OlusturSistem()) hasOnce = TestKurulum.CariBakiye(db0, k.CariId).Has;
        var sonuc = await Paralel(N, db => Cari(db).TahsilatGirHareket(k.CariId, new MCari.TahsilatGirisDto
        {
            HasGrami = 1m, KurSnapshotId = k.KurId,
        }, k.Islem));
        Assert.True(sonuc.All(e => e is null),
            "Beklenmeyen istisna: " + string.Join(" | ", sonuc.Where(e => e is not null).Select(e => e!.GetType().Name + ": " + e.Message)));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(10, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT"));
        Assert.Equal(hasOnce - 10m, TestKurulum.CariBakiye(kontrol, k.CariId).Has);
    }
}

// ── SAF: güvenli dönüş yolu (açık yönlendirme) ───────────────────────────────────────────
public sealed class KaporaEszamanSafTestleri
{
    [Theory]
    [InlineData("/\t/kotu.example.com")]
    [InlineData("/\n/kotu.example.com")]
    [InlineData("/\r/kotu.example.com")]
    [InlineData("/\u0000/kotu.example.com")]
    [InlineData("/\u007f/kotu.example.com")]
    [InlineData("/%09/kotu.example.com")]
    [InlineData("/%0A/kotu.example.com")]
    [InlineData("/%0D/kotu.example.com")]
    [InlineData("/%5C/kotu.example.com")]
    [InlineData("/%5Ckotu.example.com")]
    [InlineData("/%2Fkotu.example.com")]
    [InlineData("/%2fkotu.example.com")]
    [InlineData("/%252Fkotu.example.com")]
    [InlineData("/%25252Fkotu.example.com")]
    [InlineData("/%2525252Fkotu.example.com")]
    [InlineData("//kotu.example.com")]
    [InlineData("/\\kotu.example.com")]
    [InlineData("/a\\b")]
    [InlineData("/x%3Ay")]
    [InlineData("/a:b?x=1")]              // tur 2: yol kısmında ":" hâlâ yasak
    [InlineData("/x?a=1%0A:2")]           // sorguda ":" serbest ama kontrol karakteri yine yasak
    [InlineData("https://kotu.example.com")]
    [InlineData("/")]
    [InlineData("")]
    [InlineData(null)]
    public void KS01_kotu_donus_reddedilir_koke_duser(string? girdi)
    {
        Assert.Null(GuvenliYol.UygulamaIci(girdi));
        Assert.Equal("/", GuvenliYol.Donus(girdi));
        Assert.Equal("/emanet", GuvenliYol.Donus(girdi, "/emanet"));
        Assert.Null(Kuyumcu.Web.Pages.Cikti.GuvenliDonus(girdi));   // ekran aynı yardımcıyı kullanır
    }

    [Theory]
    [InlineData("/emanet")]
    [InlineData("/siparisler/01ABC")]
    [InlineData("/siparisler/01ABC?sekme=kapora")]
    [InlineData("/cariler/%C3%A7")]
    [InlineData("/x?saat=10:00")]         // tur 2: sorgu kısmında ":" meşru
    [InlineData("/x?saat=10%3A00")]
    public void KS02_uygulama_ici_yol_korunur(string girdi)
    {
        Assert.Equal(girdi, GuvenliYol.UygulamaIci(girdi));
        Assert.Equal(girdi, GuvenliYol.Donus(girdi, "/x"));
        Assert.Equal(girdi, Kuyumcu.Web.Pages.Cikti.GuvenliDonus(girdi));
    }

    [Fact]
    public void KS03_varsayilan_da_guvensizse_kok()
    {
        Assert.Equal("/", GuvenliYol.Donus("//a", "//b"));
    }
}
