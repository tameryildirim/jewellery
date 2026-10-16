using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MMasak = Kuyumcu.Api.Modules.Masak;
using MSatis = Kuyumcu.Api.Modules.Satis;
using MTevsik = Kuyumcu.Api.Modules.Tevsik;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-3 — MASAK kümülatifte brüt ölçü (§ 4.1), Peşin Müşteri art arda
// fiş sorusu (§ 4.2), YASAL-2 devri (günlük kapı red metni kodsuz).
// Şartname: dokumanlar/02-mimari/kalan-paketler/PAKET-YASAL-3-…-2026-09-30.md § 6.
// ⚠ Yaptırım listesi KİRACI-ÜSTÜ (paylaşılan) olduğu için testler BENZERSİZ TCKN ve
//   ad kullanır (Yasal3Yardim.Tckn) — başka testlerin kimlikleriyle çakışmaz.
// ═════════════════════════════════════════════════════════════════════════════

internal static class Yasal3Yardim
{
    private static readonly Random Zar = new();
    private static readonly object Kilit = new();

    /// <summary>Algoritmaya uygun, rastgele TCKN (ilk hane ≠ 0).</summary>
    public static string Tckn()
    {
        int[] d = new int[11];
        lock (Kilit)
        {
            d[0] = Zar.Next(1, 10);
            for (var i = 1; i < 9; i++) d[i] = Zar.Next(0, 10);
        }
        var tek = d[0] + d[2] + d[4] + d[6] + d[8];
        var cift = d[1] + d[3] + d[5] + d[7];
        d[9] = ((tek * 7 - cift) % 10 + 10) % 10;
        d[10] = d.Take(10).Sum() % 10;
        return string.Concat(d);
    }

    public static MMasak.MasakBilgiDto Kimlik(string tckn, string ad = "Ayşe Demir") => new()
    {
        BelgeTuru = KimlikBelgeTuru.TcKimlik, BelgeNo = tckn, AdSoyad = ad,
        DogumTarihi = new DateOnly(1985, 4, 12), Uyruk = "TUR",
        IslemAmaci = "Yatırım", Aciklama = "Birikim değerlendirmek için altın alımı",
        FonKaynagi = "Maaş birikimi", EkBelgeReferansi = "BELGE-2026-001",
    };

    public static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);
}

[Collection("veritabani")]
public sealed class MasakBrutOlcuTestleri(TestVeritabani vt)
{
    /// <summary>
    /// K-1 (servis): 900.000 eski ↔ 910.000 yeni takas (fark 10.000, kimlikli) → aynı gün
    /// 100.000 satış: kümülatif 1.010.000 ölçülür → kimlik bloğu yoksa 400; defterde
    /// masak_olcu_tl = 910.000 (Max), tl_tutari = 10.000 (fark).
    /// </summary>
    [Fact]
    public async Task K1_takas_kumulatife_el_degistiren_degerle_girer()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-K1"));
        using var db = vt.Olustur(k);
        var s = Yasal3Yardim.Satis(db);

        await s.Takas(new MSatis.TakasDto
        {
            Satici = ParaDemoTestVerisi.TakasSaticisi,   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
            CariId = k.CariId, EskiUrunAyarId = "K24", EskiUrunGram = 300m, EskiUrunBirimFiyatTlHasGram = 3_000m,
            YeniUrunId = k.UrunId, YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = 910_000m,
            KasaId = k.KasaId, OdemeTuru = "HAVALE", KurSnapshotId = k.KurId, Masak = Yasal3Yardim.Kimlik(Yasal3Yardim.Tckn()),
        }, k.Islem);

        using (var kontrol = vt.OlusturSistem())
        {
            var h = kontrol.Hareketler.Single(x => x.TenantId == k.TenantId && x.Tur == "TAKAS");
            Assert.Equal(10_000m, h.TlTutari);
            Assert.Equal(910_000m, h.MasakOlcuTl);
        }

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => s.Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 100_000m,
            OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        Assert.Contains("MASAK", ex.Message);
        Assert.Contains("1.010.000,00", ex.Message);
        Assert.Equal(1_010_000m, await MMasak.MasakKapisi.GunlukIslemToplam(db, k.TenantId, k.CariId, DateTime.UtcNow) + 100_000m);
    }

    /// <summary>
    /// K-2: ESNAF_MUAF hurda alış brüt 180.000 (stopaj %2 → net 176.400) + aynı gün 20.000 satış →
    /// kümülatif 200.000 ≥ 200.000 → 400 (net okunsaydı 196.400 ile geçerdi). MASAK-370: bugün kimliği
    /// fiilen tetikleyen en düşük tutar 200.000 (amaç beyanı, TEYİTSİZ; md.5/1-b 07.10.2026'dan 370.000).
    /// Eski hâli: 6.000 satış → 186.000 ≥ 185.000 (185.000 dönemi).
    /// </summary>
    [Fact]
    public async Task K2_hurdada_kumulatif_brut_bedelle_olculur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-K2"));
        using var db = vt.Olustur(k);
        var s = Yasal3Yardim.Satis(db);

        await s.HurdaAlis(new MSatis.HurdaAlisDto
        {
            CariId = k.CariId, AyarId = "K24", GramAgirlik = 60m, BirimFiyatTlHasGram = 3_000m,
            OdemeTuru = "VERESIYE", KasaId = null, KurSnapshotId = k.KurId,
            Satici = TestKurulum.HurdaSatici(SaticiTipi.EsnafMuaf),
        }, k.Islem);

        using (var kontrol = vt.OlusturSistem())
        {
            var h = kontrol.Hareketler.Single(x => x.TenantId == k.TenantId && x.Tur == "HURDA_ALIS");
            Assert.Equal(-176_400m, h.TlTutari);   // defter: stopaj sonrası net
            Assert.Equal(180_000m, h.MasakOlcuTl); // MASAK ölçüsü: brüt
        }
        Assert.Equal(180_000m, await MMasak.MasakKapisi.GunlukIslemToplam(db, k.TenantId, k.CariId, DateTime.UtcNow));

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => s.Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 20_000m,
            OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        Assert.Contains("200.000,00", ex.Message);
    }

    /// <summary>Kolonsuz (eski) satır TL bacağıyla sayılır — iki ayrık sorgu birlikte toplanır.</summary>
    [Fact]
    public async Task Olcusuz_eski_takas_satiri_fark_ile_toplanir_olculu_ile_birlikte()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-ESKI"));
        using var db = vt.Olustur(k);
        var s = Yasal3Yardim.Satis(db);
        await s.Takas(new MSatis.TakasDto
        {
            Satici = ParaDemoTestVerisi.TakasSaticisi,   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
            CariId = k.CariId, EskiUrunAyarId = "K24", EskiUrunGram = 10m, EskiUrunBirimFiyatTlHasGram = 3_000m,
            YeniUrunId = k.UrunId, YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = 32_000m,
            KasaId = k.KasaId, OdemeTuru = "HAVALE", KurSnapshotId = k.KurId,
        }, k.Islem);
        using (var ham = vt.OlusturSistem())
            await ham.Database.ExecuteSqlRawAsync(
                "UPDATE hareketler SET masak_olcu_tl = NULL WHERE tenant_id = {0} AND tur = 'TAKAS'", k.TenantId);
        await s.Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 5_000m,
            OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
        // eski takas: fark 2.000 (ölçüsüz) + satış 5.000 (ölçüsüz) = 7.000
        Assert.Equal(7_000m, await MMasak.MasakKapisi.GunlukIslemToplam(db, k.TenantId, k.CariId, DateTime.UtcNow));
    }
}

[Collection("http")]
public sealed class MasakBrutOlcuHttpTestleri(HttpFikstur f)
{
    /// <summary>K-1 (HTTP boru hattı): gövde AYRIŞTIRILIR; SQL: masak_olcu_tl = 910.000.</summary>
    [Fact]
    public async Task K1_HTTP_takas_sonrasi_100000_satis_kimliksiz_400()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = IslemBantFikstur.ReferansKur(db, TestKurulum.KiraciKur(db, "PAKETYASAL3-TEST-K1H"));
        var c = f.Ortam.Istemci(k, "SAHIP");
        var takas = await c.PostAsJsonAsync("/api/v1/islemler/takas", new
        {
            cariId = k.CariId, eskiUrunAyarId = "K24", eskiUrunGram = 300m, eskiUrunBirimFiyatTlHasGram = 3_000m,
            yeniUrunId = k.UrunId, yeniUrunAdet = 1m, yeniUrunBirimFiyatTl = 910_000m,
            kasaId = k.KasaId, odemeTuru = "HAVALE", kurSnapshotId = k.KurId,
            masak = Yasal3Yardim.Kimlik(Yasal3Yardim.Tckn()),
            satici = ParaDemoTestVerisi.TakasSaticisiJson,   // PARA-DEMO md.3
        });
        Assert.True(takas.IsSuccessStatusCode, $"{(int)takas.StatusCode}: {await takas.Content.ReadAsStringAsync()}");

        var satis = await c.PostAsJsonAsync("/api/v1/islemler/satis", new
        {
            cariId = k.CariId, urunId = k.UrunId, adet = 1m, birimFiyatTl = 100_000m,
            odemeTuru = "HAVALE", kasaId = k.KasaId, kurSnapshotId = k.KurId,
        });
        Assert.Equal(HttpStatusCode.BadRequest, satis.StatusCode);
        var mesaj = (await satis.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;
        Assert.Contains("1.010.000,00", mesaj);

        using var kontrol = f.Vt.OlusturSistem();
        Assert.Equal(910_000m, kontrol.Hareketler.Single(h => h.TenantId == k.TenantId && h.Tur == "TAKAS").MasakOlcuTl);
        Assert.Equal(0, kontrol.Hareketler.Count(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
    }
}

[Collection("veritabani")]
public sealed class PesinArtArdaTestleri(TestVeritabani vt)
{
    private static async Task<string> Pesin(KuyumcuDbContext db, Kiraci k) =>
        (await MCari.SistemCarisi.GetirVeyaOlustur(db, TestKurulum.Denetim(db), k.Islem)).Id;

    private static MSatis.SatisOlusturDto Satis(Kiraci k, string pesin, decimal tutar, string odeme = "NAKIT", bool? beyan = null) => new()
    {
        CariId = pesin, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = tutar, OdemeTuru = odeme,
        KasaId = k.KasaId, KurSnapshotId = k.KurId,
        Masak = beyan is null ? null : new MMasak.MasakBilgiDto { AyniKisiBeyani = beyan },
    };

    private async Task Geriye(Kiraci k, int dakika)
    {
        using var ham = vt.OlusturSistem();
        await ham.Database.ExecuteSqlRawAsync(
            "UPDATE hareketler SET tarih = DATE_SUB(tarih, INTERVAL {0} MINUTE) WHERE tenant_id = {1}", dakika, k.TenantId);
    }

    private int DenetimSayisi(Kiraci k)
    {
        using var kontrol = vt.OlusturSistem();
        return kontrol.DenetimKayitlari.Count(d => d.TenantId == k.TenantId && d.VarlikTuru == MMasak.PesinPencereKapisi.DenetimTuru);
    }

    /// <summary>
    /// K-4 (uyarlama — § 4.2 tevsik bileşeni): Peşin kartıyla tek işlem 30.000'i aşamaz (B-3 K2),
    /// bu yüzden art arda örnek NAKİT 20.000 + 15.000'dir (40 dk arayla). Beyan yok → 400
    /// PESIN_ART_ARDA_SORU; false → geçer + denetim 1; true → 400 "müşteriyi seçin".
    /// </summary>
    [Fact]
    public async Task K4_40dk_arayla_art_arda_nakit_soru_false_gecer_true_red()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-K4");
        using var db = vt.Olustur(k);
        var pesin = await Pesin(db, k);
        var s = Yasal3Yardim.Satis(db);

        await s.Satis(Satis(k, pesin, 20_000m), k.Islem);
        await Geriye(k, 40);

        var soru = await Assert.ThrowsAsync<BadRequestException>(() => s.Satis(Satis(k, pesin, 15_000m), k.Islem));
        Assert.Equal(MMasak.PesinPencereKapisi.SoruKodu, AlanIsareti.Kod(soru));
        Assert.Equal(MMasak.PesinPencereKapisi.BeyanAlani, AlanIsareti.Alan(soru));

        var ayni = await Assert.ThrowsAsync<BadRequestException>(() => s.Satis(Satis(k, pesin, 15_000m, beyan: true), k.Islem));
        Assert.Equal("cariId", AlanIsareti.Alan(ayni));
        Assert.Contains("Müşteriyi seçin", ayni.Message);
        Assert.Equal(0, DenetimSayisi(k));

        await s.Satis(Satis(k, pesin, 15_000m, beyan: false), k.Islem);
        Assert.Equal(1, DenetimSayisi(k));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(2, kontrol.Hareketler.Count(h => h.TenantId == k.TenantId && h.CariId == pesin && h.Tur == "SATIS"));
    }

    /// <summary>K-5: aynı senaryo 61 dk arayla → soru yok (pencere dışı).</summary>
    [Fact]
    public async Task K5_61dk_arayla_soru_yok()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-K5");
        using var db = vt.Olustur(k);
        var pesin = await Pesin(db, k);
        var s = Yasal3Yardim.Satis(db);
        await s.Satis(Satis(k, pesin, 20_000m), k.Islem);
        await Geriye(k, 61);
        await s.Satis(Satis(k, pesin, 15_000m), k.Islem);   // soru YOK
        Assert.Equal(0, DenetimSayisi(k));
    }

    /// <summary>
    /// K-4 varyantı (mutasyon 4): pencere KULLANICI başınadır — başka tezgâhtarın az önceki
    /// Peşin satışı bu kullanıcıya soru açmaz.
    /// </summary>
    [Fact]
    public async Task K4b_baska_kullanicinin_pesin_satisi_soru_acmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-K4B");
        var ikinciId = Kimlik.Ulid();
        kurulum.Kullanicilar.Add(new Kullanici
        {
            Id = ikinciId, TenantId = k.TenantId, SubeId = k.SubeId, Ad = "İkinci", Soyad = "Tezgâhtar",
            Email = $"{ikinciId}@test.local", SifreHash = "x", Rol = "TEZGAHTAR", Aktif = true,
            Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        kurulum.SaveChanges();
        using var db = vt.Olustur(k);
        var pesin = await Pesin(db, k);
        var s = Yasal3Yardim.Satis(db);
        await s.Satis(Satis(k, pesin, 20_000m), k.Islem);
        await Geriye(k, 10);
        var ikinci = new Kuyumcu.Api.Auth.AktifKullanici(ikinciId, k.TenantId, k.SubeId, "TEZGAHTAR");
        await s.Satis(Satis(k, pesin, 15_000m), ikinci);    // başka kullanıcı → soru yok
        // aynı kullanıcı ise soru VAR (kural gerçekten kullanıcı başına çalışıyor)
        var soru = await Assert.ThrowsAsync<BadRequestException>(() => s.Satis(Satis(k, pesin, 15_000m), k.Islem));
        Assert.Equal(MMasak.PesinPencereKapisi.SoruKodu, AlanIsareti.Kod(soru));
    }

    /// <summary>
    /// MASAK bileşeni: kartsız (HAVALE) 6 × 29.000 = 174.000 + 29.000 = 203.000 ≥ 200.000 (MASAK-370: bugünkü en düşük kimlik tetikleyicisi) →
    /// soru (nakit bileşeni tetiklenmez: önceki nakit 0 + 29.000 ≤ 30.000).
    /// </summary>
    [Fact]
    public async Task Pencere_MASAK_kimlik_esigine_ulasinca_soru()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-PM");
        using var db = vt.Olustur(k);
        var pesin = await Pesin(db, k);
        var s = Yasal3Yardim.Satis(db);
        for (var i = 0; i < 6; i++) await s.Satis(Satis(k, pesin, 29_000m, "HAVALE"), k.Islem);
        var soru = await Assert.ThrowsAsync<BadRequestException>(() => s.Satis(Satis(k, pesin, 29_000m, "HAVALE"), k.Islem));
        Assert.Equal(MMasak.PesinPencereKapisi.SoruKodu, AlanIsareti.Kod(soru));
    }
}

[Collection("http")]
public sealed class PesinArtArdaHttpTestleri(HttpFikstur f)
{
    /// <summary>K-4 (HTTP): gövdede `kod` = PESIN_ART_ARDA_SORU ve `alanlar.ayniKisiBeyani`.</summary>
    [Fact]
    public async Task K4_HTTP_govde_kod_ve_alan_tasir()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETYASAL3-TEST-K4H");
        var c = f.Ortam.Istemci(k, "TEZGAHTAR");
        var pesin = (await (await c.GetAsync("/api/v1/cari/sistem-carisi")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        object Govde(decimal t, bool? beyan) => new
        {
            cariId = pesin, urunId = k.UrunId, adet = 1m, birimFiyatTl = t, odemeTuru = "NAKIT",
            kasaId = k.KasaId, kurSnapshotId = k.KurId, masak = beyan is null ? null : new { ayniKisiBeyani = beyan },
        };
        var ilk = await c.PostAsJsonAsync("/api/v1/islemler/satis", Govde(20_000m, null));
        Assert.True(ilk.IsSuccessStatusCode, await ilk.Content.ReadAsStringAsync());
        var ikinci = await c.PostAsJsonAsync("/api/v1/islemler/satis", Govde(15_000m, null));
        Assert.Equal(HttpStatusCode.BadRequest, ikinci.StatusCode);
        var j = await ikinci.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PESIN_ART_ARDA_SORU", j.GetProperty("kod").GetString());
        Assert.True(j.GetProperty("alanlar").TryGetProperty("ayniKisiBeyani", out _));
        var ucuncu = await c.PostAsJsonAsync("/api/v1/islemler/satis", Govde(15_000m, false));
        Assert.True(ucuncu.IsSuccessStatusCode, await ucuncu.Content.ReadAsStringAsync());
    }
}

/// <summary>YASAL-2 devri: günlük kapının red metni — mevzuat kodu yok, tek cümle, ne yapmalı.</summary>
public sealed class TevsikGunlukRedMetniTestleri
{
    private static readonly MTevsik.TevsikEsikleri Es = new() { Had = 30_000m, Etkin = true, IslemTutariKuraliEtkin = true };

    [Theory]
    [InlineData(20_000, 15_000, null)]        // kümülatif
    [InlineData(0, 45_000, null)]             // tek işlem
    [InlineData(0, 10_000, 50_000)]           // işlem tutarı kuralı
    public void Red_metni_kodsuz_tek_cumle_ve_ne_yapmali_soyler(int onceki, int bu, int? toplam)
    {
        var s = MTevsik.TevsikKurali.Degerlendir(onceki, bu, toplam, Es);
        Assert.True(s.Gerekli);
        TaksitMesajDenetimi.KodsuzTekCumle(s.Mesaj!);
        Assert.Contains("kartla ya da havaleyle", s.Mesaj!, StringComparison.Ordinal);
        Assert.Contains("30.000,00 TL", s.Mesaj!, StringComparison.Ordinal);
    }

    [Fact]
    public void Kumulatif_metin_onceki_ve_bu_islemi_yazar()
    {
        var s = MTevsik.TevsikKurali.Degerlendir(20_000m, 15_000m, null, Es);
        Assert.Equal("Bu kişiyle bugün nakit toplamı 35.000,00 TL oluyor (önceki 20.000,00 TL, bu işlem 15.000,00 TL) ve 30.000,00 TL nakit sınırını 5.000,00 TL aşıyor; kalanı kartla ya da havaleyle ödenmeli.", s.Mesaj);
    }
}
