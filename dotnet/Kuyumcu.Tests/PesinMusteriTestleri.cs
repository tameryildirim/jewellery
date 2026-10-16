using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MMasak = Kuyumcu.Api.Modules.Masak;
using MSatis = Kuyumcu.Api.Modules.Satis;
using MTevsik = Kuyumcu.Api.Modules.Tevsik;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-3 — "PEŞİN MÜŞTERİ" SİSTEM CARİSİ (karar D2, K1–K5) · FAIL-CLOSED
//
// Mehmet'in veto alanı. Her test bir koşulu ADIYLA ölçer:
//   K1 — sistem carisinin satırları kümülatif havuzlara GİRMEZ;
//   K2 — tek işlem / fiş toplamı MASAK kimlik eşiğine ya da tevsik haddine
//        ulaşıyorsa SUNUCU reddeder (kimlik bloğuyla bile);
//   K3 — açık hesap yok (veresiye bacağı, bakiye kilidi);
//   K4 — silinemez / pasife alınamaz / bayrak değişmez / kiracı başına TEK.
// Eşik sayıları testte VARSAYILAN yapılandırmadır (MasakEsikleri/TevsikEsikleri);
// kodda sabit değildir.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class PesinMusteriTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, TestKurulum.Denetim(db));

    private static MMasak.MasakBilgiDto TamKimlik() => new()
    {
        BelgeTuru = KimlikBelgeTuru.TcKimlik, BelgeNo = "10000000146", AdSoyad = "Ayşe Demir",
        DogumTarihi = new DateOnly(1985, 4, 12), Uyruk = "TUR",
        IslemAmaci = "Yatırım", Aciklama = "Birikim değerlendirmek için altın alımı",
        FonKaynagi = "Maaş birikimi", EkBelgeReferansi = "BELGE-2026-001",
    };

    private async Task<(Kiraci K, string PesinId)> Kur(string ad)
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETB3-TEST-" + ad, stokAdet: 50m);
        using var db = vt.Olustur(k);
        var kart = await MCari.SistemCarisi.GetirVeyaOlustur(db, TestKurulum.Denetim(db), k.Islem);
        return (k, kart.Id);
    }

    private static MSatis.SatisOlusturDto Tek(Kiraci k, string cariId, decimal tutar, string odeme,
        MMasak.MasakBilgiDto? masak = null) => new()
    {
        CariId = cariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = tutar,
        OdemeTuru = odeme, KasaId = odeme == "VERESIYE" ? null : k.KasaId,
        KurSnapshotId = k.KurId, Masak = masak,
    };

    private static MSatis.SatisFisiDto Fis(Kiraci k, string cariId, decimal birimFiyat,
        List<MSatis.OdemeBacagiDto> odemeler) => new()
    {
        CariId = cariId,
        Kalemler = [new MSatis.SatisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = birimFiyat }],
        Odemeler = odemeler,
        KurSnapshotId = k.KurId,
    };

    private static MSatis.OdemeBacagiDto B(Kiraci k, string arac, decimal tutar) =>
        new() { Arac = arac, TutarTl = tutar, KasaId = arac == "VERESIYE" ? null : k.KasaId };

    private int SatisSayisi(Kiraci k, string cariId)
    {
        using var kontrol = vt.OlusturSistem();
        return kontrol.Hareketler.Count(h => h.TenantId == k.TenantId && h.CariId == cariId && h.Tur == "SATIS");
    }

    // ── K4 — tohumlama ve teklik ─────────────────────────────────────────────

    [Fact]
    public async Task B3_01_tohumlama_kiraci_basina_TEK_kart_acar_ve_tekrar_cagri_ayni_karti_doner()
    {
        var (k, pesinId) = await Kur("tohum");
        using var db = vt.Olustur(k);
        var ikinci = await MCari.SistemCarisi.GetirVeyaOlustur(db, TestKurulum.Denetim(db), k.Islem);
        Assert.Equal(pesinId, ikinci.Id);

        using var kontrol = vt.OlusturSistem();
        var kartlar = kontrol.Cariler.Where(c => c.TenantId == k.TenantId && c.SistemCarisi).ToList();
        var kart = Assert.Single(kartlar);
        Assert.True(kart.Aktif);
        Assert.Equal("Peşin Müşteri", kart.Ad);
        Assert.Equal("MUSTERI", kart.Tur);
        Assert.Null(kart.VergiNo);
        var bakiye = kontrol.CariBakiyeler.Single(b => b.CariId == pesinId);
        Assert.Equal(0m, bakiye.TlBakiye);
        // Denetim izi: tohumlama gerekçeli yazma kaydı düşer (tek kez).
        Assert.Equal(1, kontrol.DenetimKayitlari.Count(d =>
            d.TenantId == k.TenantId && d.VarlikTuru == MCari.SistemCarisi.DenetimVarligi && d.VarlikId == pesinId));
    }

    [Fact]
    public async Task B3_02_veritabani_kisiti_IKINCI_sistem_carisini_reddeder()
    {
        var (k, _) = await Kur("kisit");
        using var db = vt.OlusturSistem();
        var now = DateTime.UtcNow;
        db.Cariler.Add(new Cari
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, Tur = "MUSTERI", Ad = "İkinci Peşin",
            Aktif = true, SistemCarisi = true, Olusturuldu = now, Guncellendi = now,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    // ── Eşik altı şerit: peşin satış çalışır ─────────────────────────────────

    [Fact]
    public async Task B3_03_esik_alti_nakit_satis_GECER_ve_bakiye_sifir_kalir()
    {
        var (k, pesinId) = await Kur("esik-alti");
        using var db = vt.Olustur(k);
        await Satis(db).Satis(Tek(k, pesinId, 10_000m, "NAKIT"), k.Islem);

        Assert.Equal(1, SatisSayisi(k, pesinId));
        using var kontrol = vt.OlusturSistem();
        var b = kontrol.CariBakiyeler.Single(x => x.CariId == pesinId);
        Assert.Equal(0m, b.TlBakiye);
        Assert.Equal(0m, b.HasGramBakiye);
    }

    // ── K2 — eşik üstü FAIL-CLOSED ───────────────────────────────────────────

    [Fact]
    public async Task B3_04_MASAK_esigine_ulasan_satis_KIMLIK_BLOGUYLA_BILE_400()
    {
        var (k, pesinId) = await Kur("masak");
        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).Satis(Tek(k, pesinId, 200_000m, "HAVALE", TamKimlik()), k.Islem));
        Assert.Contains("Peşin Müşteri", hata.Message);
        Assert.Contains("gerçek müşteri", hata.Message);
        Assert.Contains("MASAK", hata.Message);
        Assert.Equal(0, SatisSayisi(k, pesinId));

        // Aynı tutar GERÇEK cariyle ve kimlikle geçer — kural kartı daraltır, satışı yasaklamaz.
        await Satis(db).Satis(Tek(k, k.CariId, 200_000m, "HAVALE", TamKimlik()), k.Islem);
        Assert.Equal(1, SatisSayisi(k, k.CariId));
    }

    [Fact]
    public async Task B3_05_tevsik_haddini_asan_BANKA_odemeli_satis_da_400()
    {
        // HAVALE tevsik kapısını hiç çağırmaz; sınır MASAK kapısındaki Peşin şeridinden gelir.
        var (k, pesinId) = await Kur("tevsik-havale");
        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).Satis(Tek(k, pesinId, 35_000m, "HAVALE"), k.Islem));
        Assert.Contains("Peşin Müşteri", hata.Message);
        Assert.Contains("tevsik", hata.Message);
        Assert.Equal(0, SatisSayisi(k, pesinId));
    }

    [Fact]
    public async Task B3_06_TevsikKapisi_Peşin_kartta_fis_toplamiyla_reddeder_KURAL_KAPALIYKEN_BILE()
    {
        var (k, pesinId) = await Kur("tevsik-dogrudan");
        using var db = vt.Olustur(k);

        // Nakit payı haddin altında, fiş toplamı üstünde → Peşin kartta RED.
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            MTevsik.TevsikKapisi.Varsayilan().Dogrula(db, k.TenantId, pesinId, 10_000m, DateTime.UtcNow,
                islemToplamTutari: 40_000m));
        Assert.Contains("Peşin Müşteri", hata.Message);

        // Kural kapatılmış (Etkin=false) olsa bile bu kart için kapı SIKILAŞIR.
        var kapali = new MTevsik.TevsikKapisi(new MTevsik.TevsikKurali(new MTevsik.TevsikEsikleri { Etkin = false }));
        await Assert.ThrowsAsync<BadRequestException>(() =>
            kapali.Dogrula(db, k.TenantId, pesinId, 35_000m, DateTime.UtcNow));

        // Aynı çağrı GERÇEK caride kural kapalıyken geçer (mevcut davranış korunur).
        var sonuc = await kapali.Dogrula(db, k.TenantId, k.CariId, 35_000m, DateTime.UtcNow);
        Assert.True(sonuc.Gerekli);
    }

    [Fact]
    public async Task B3_07_MasakKapisi_Peşin_kartta_esikte_kimlikle_bile_reddeder()
    {
        var (k, pesinId) = await Kur("masak-dogrudan");
        using var db = vt.Olustur(k);
        var kapi = MMasak.MasakKapisi.Varsayilan();
        var esik = new MMasak.MasakEsikleri().KimlikEsigi;

        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            kapi.DogrulaKumulatif(db, k.TenantId, pesinId, esik, TamKimlik(), DateTime.UtcNow));
        Assert.Contains("MASAK kimlik tespiti", hata.Message);

        // Eşik altı (tevsik haddinin de altı) geçer.
        var gerek = await kapi.DogrulaKumulatif(db, k.TenantId, pesinId, 1_000m, null, DateTime.UtcNow);
        Assert.Equal(BeyanKademesi.Yok, gerek.Kademe);
    }

    // ── K1 — havuz dışı ──────────────────────────────────────────────────────

    [Fact]
    public async Task B3_08_K1_Peşin_kartin_satirlari_kumulatif_havuzlara_GIRMEZ()
    {
        var (k, pesinId) = await Kur("havuz");
        using var db = vt.Olustur(k);

        // Günün iki ayrı kimliksiz müşterisi: 20.000 + 20.000 nakit. Tek kişi
        // sayılsaydı ikincisi tevsik haddinde (kümülatif 40.000) patlardı.
        await Satis(db).Satis(Tek(k, pesinId, 20_000m, "NAKIT"), k.Islem);
        // PAKET YASAL-3 (§ 4.2): art arda Peşin fişte program "aynı müşteri mi?" diye sorar;
        // bu senaryo İKİ AYRI kişidir → tezgâhtar "hayır, başka biri" der (beyan false).
        var ikinci = Tek(k, pesinId, 20_000m, "NAKIT");
        ikinci.Masak = new MMasak.MasakBilgiDto { AyniKisiBeyani = false };
        await Satis(db).Satis(ikinci, k.Islem);
        Assert.Equal(2, SatisSayisi(k, pesinId));

        Assert.Equal(0m, await MTevsik.TevsikKapisi.GunlukNakitToplam(db, k.TenantId, pesinId, DateTime.UtcNow));
        Assert.Equal(0m, await MMasak.MasakKapisi.GunlukIslemToplam(db, k.TenantId, pesinId, DateTime.UtcNow));

        // Karşılaştırma: GERÇEK caride aynı desen kümülatifte YAKALANIR (mevcut kural bozulmadı).
        await Satis(db).Satis(Tek(k, k.CariId, 20_000m, "NAKIT"), k.Islem);
        await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).Satis(Tek(k, k.CariId, 20_000m, "NAKIT"), k.Islem));
        Assert.Equal(20_000m, await MTevsik.TevsikKapisi.GunlukNakitToplam(db, k.TenantId, k.CariId, DateTime.UtcNow));
    }

    // ── K3 — açık hesap yok (fark ≠ 0 kaydedilemez) ──────────────────────────

    [Fact]
    public async Task B3_09_K3_karma_fiste_VERESIYE_bacagi_Peşin_kartta_400()
    {
        var (k, pesinId) = await Kur("karma-veresiye");
        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(db).SatisFisi(Fis(k, pesinId, 10_000m, [B(k, "NAKIT", 5_000m), B(k, "VERESIYE", 5_000m)]), k.Islem));
        Assert.Contains("veresiye", hata.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Peşin Müşteri", hata.Message);

        // Fark ≠ 0 (bacaklar fişi kapatmıyor) → 400 (mevcut TAM EŞİTLİK kuralı).
        var fark = await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(db).SatisFisi(Fis(k, pesinId, 10_000m, [B(k, "NAKIT", 6_000m)]), k.Islem));
        Assert.Contains("fark", fark.Message);

        // Fark = 0: nakit + havale üçlü olmadan ikili bacak geçer.
        await Satis(db).SatisFisi(Fis(k, pesinId, 10_000m, [B(k, "NAKIT", 6_000m), B(k, "HAVALE", 4_000m)]), k.Islem);
        Assert.Equal(1, SatisSayisi(k, pesinId));
    }

    [Fact]
    public void B3_10_OdemeSepeti_sistem_carisi_bayragi_VERESIYE_bacagini_reddeder()
    {
        List<MSatis.OdemeBacagiDto> bacaklar =
        [
            new() { Arac = "NAKIT", TutarTl = 100m, KasaId = "k1" },
            new() { Arac = "VERESIYE", TutarTl = 50m },
        ];
        var hata = Assert.Throws<BadRequestException>(() =>
            MSatis.OdemeSepeti.Coz(bacaklar, 150m, "c1", MSatis.DovizKurlari.Yok, sistemCarisi: true));
        Assert.Equal(MCari.SistemCarisi.VeresiyeYasakMesaji, hata.Message);

        // Bayrak yoksa (gerçek cari) aynı sepet geçer — mevcut davranış.
        var sepet = MSatis.OdemeSepeti.Coz(bacaklar, 150m, "c1", MSatis.DovizKurlari.Yok);
        Assert.Equal(50m, sepet.VeresiyeToplam);
    }

    [Fact]
    public async Task B3_11_K3_tek_etiketli_VERESIYE_satis_Peşin_kartta_400_ve_defter_bos()
    {
        var (k, pesinId) = await Kur("tek-veresiye");
        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAnyAsync<ApiException>(
            () => Satis(db).Satis(Tek(k, pesinId, 5_000m, "VERESIYE"), k.Islem));
        Assert.Equal(400, hata.StatusCode);
        Assert.Equal(MCari.SistemCarisi.BakiyeYasakMesaji, hata.Message);
        Assert.Equal(0, SatisSayisi(k, pesinId));
    }

    [Fact]
    public async Task B3_12_K3_cari_ekranindan_VERESIYE_girisi_Peşin_kartta_400()
    {
        var (k, pesinId) = await Kur("cari-veresiye");
        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAnyAsync<ApiException>(() => Cari(db).VeresiyeGir(pesinId,
            new MCari.VeresiyeGirisDto { TlTutari = 1_000m, KurSnapshotId = k.KurId }, k.Islem));
        Assert.Equal(400, hata.StatusCode);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0m, kontrol.CariBakiyeler.Single(b => b.CariId == pesinId).TlBakiye);
        Assert.Equal(0, kontrol.Hareketler.Count(h => h.CariId == pesinId));
    }

    // ── K4 — silinemez / pasife alınamaz ─────────────────────────────────────

    [Fact]
    public async Task B3_13_K4_servis_Peşin_karti_SILMEZ_ve_PASIFE_ALMAZ()
    {
        var (k, pesinId) = await Kur("silinemez");
        using var db = vt.Olustur(k);
        var sil = await Assert.ThrowsAsync<BadRequestException>(() => Cari(db).DeaktifEt(pesinId, k.Islem));
        Assert.Equal(MCari.SistemCarisi.SilinemezMesaji, sil.Message);

        var patch = new MCari.CariGuncelleDto { Aktif = false };
        await Assert.ThrowsAsync<BadRequestException>(() => Cari(db).Guncelle(pesinId, patch, k.Islem));

        using var kontrol = vt.OlusturSistem();
        var kart = kontrol.Cariler.Single(c => c.Id == pesinId);
        Assert.True(kart.Aktif);
        Assert.True(kart.SistemCarisi);
    }

    [Fact]
    public async Task B3_14_K4_PATCH_ad_duzenlenir_digerleri_korunur_kimlik_alani_400()
    {
        var (k, pesinId) = await Kur("patch");
        using var db = vt.Olustur(k);

        // Gövdede yalnız ad → ad değişir, gönderilmeyen alanlar KORUNUR.
        await Cari(db).Guncelle(pesinId, new MCari.CariGuncelleDto { Ad = "Peşin Satış", Notlar = "tezgâh" }, k.Islem);
        await Cari(db).Guncelle(pesinId, new MCari.CariGuncelleDto { Ad = "Peşin Satış 2" }, k.Islem);

        var kimlik = await Assert.ThrowsAsync<BadRequestException>(() =>
            Cari(db).Guncelle(pesinId, new MCari.CariGuncelleDto { VergiNo = "10000000146" }, k.Islem));
        Assert.Equal(MCari.SistemCarisi.KimlikAlaniMesaji, kimlik.Message);
        await Assert.ThrowsAsync<BadRequestException>(() =>
            Cari(db).PepGuncelle(pesinId, new MCari.CariPepDto { PepBeyani = true, PepGorev = "x" }, k.Islem));

        using var kontrol = vt.OlusturSistem();
        var kart = kontrol.Cariler.Single(c => c.Id == pesinId);
        Assert.Equal("Peşin Satış 2", kart.Ad);
        Assert.Equal("tezgâh", kart.Notlar);   // gönderilmedi → korundu
        Assert.Null(kart.VergiNo);
        Assert.False(kart.PepBeyani);
        Assert.True(kart.SistemCarisi);
    }

    [Fact]
    public async Task B3_15_K4_SaveChanges_servisi_atlayan_yolu_da_REDDEDER()
    {
        var (k, pesinId) = await Kur("savechanges");

        using (var db = vt.OlusturSistem())
        {
            db.Cariler.Single(c => c.Id == pesinId).Aktif = false;
            await Assert.ThrowsAsync<MCari.SistemCarisiDegistirilemez>(() => db.SaveChangesAsync());
        }
        using (var db = vt.OlusturSistem())
        {
            db.Cariler.Remove(db.Cariler.Single(c => c.Id == pesinId));
            Assert.Throws<MCari.SistemCarisiDegistirilemez>(() => db.SaveChanges());
        }
        using (var db = vt.OlusturSistem())
        {
            db.Cariler.Single(c => c.Id == pesinId).SistemCarisi = false;
            await Assert.ThrowsAsync<MCari.SistemCarisiDegistirilemez>(() => db.SaveChangesAsync());
        }
        using (var db = vt.OlusturSistem())
        {
            // Var olan sıradan cari sonradan sistem carisine ÇEVRİLEMEZ.
            db.Cariler.Single(c => c.Id == k.CariId).SistemCarisi = true;
            await Assert.ThrowsAsync<MCari.SistemCarisiDegistirilemez>(() => db.SaveChangesAsync());
        }
        using (var db = vt.OlusturSistem())
        {
            // Bakiye kilidi: servis dışı doğrudan bakiye yazımı da reddedilir.
            db.CariBakiyeler.Single(b => b.CariId == pesinId).TlBakiye = 100m;
            await Assert.ThrowsAsync<MCari.SistemCarisiDegistirilemez>(() => db.SaveChangesAsync());
        }

        using var kontrol = vt.OlusturSistem();
        var kart = kontrol.Cariler.Single(c => c.Id == pesinId);
        Assert.True(kart.Aktif && kart.SistemCarisi);
        Assert.False(kontrol.Cariler.Single(c => c.Id == k.CariId).SistemCarisi);
        Assert.Equal(0m, kontrol.CariBakiyeler.Single(b => b.CariId == pesinId).TlBakiye);
    }

    [Fact]
    public async Task B3_16_siradan_cari_pasife_alinir_MEVCUT_davranis_bozulmadi()
    {
        var (k, _) = await Kur("siradan");
        using var db = vt.Olustur(k);
        await Cari(db).DeaktifEt(k.CariId, k.Islem);
        using var kontrol = vt.OlusturSistem();
        Assert.False(kontrol.Cariler.Single(c => c.Id == k.CariId).Aktif);
    }
}
