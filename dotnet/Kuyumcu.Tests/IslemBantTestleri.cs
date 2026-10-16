using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ISLEM-BANT (09.10) — işlem içi elle yazılan has gram fiyatı makul bandı +
// TEZGAHTAR'ın üst çubuktan kendi BASİT/PRO modunu değiştirebilmesi (PATCH /kullanicilar/ben
// alan beyaz listesi). Kaynak: KUR-BANT § 0.6 (YÜKSEK) ve § 0.7/2 (ORTA).
// Bant (Core FiyatBandi.IslemDenetle, İŞ KURALI): mutlak has 500–50.000 TL (SERT); referans =
// son geçerli kur kaydının has fiyatı; alış yönlü (hurda alış, takas, eski altınla ödeme)
// %50–%110, hurda satışı ± %20. Bant dışı yalnız SAHİP/MÜDÜR onay + gerekçe (≥ 20) + denetim izi.
// Fikstür has 6.827,20 → alış bandı 3.413,60–7.509,92 · satış bandı 5.461,76–8.192,64.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Saf: Core kuralı, uçlar, maden, sözlük (TR + EN, iki ondalık).</summary>
public sealed class IslemBantSafTestleri
{
    private const decimal R = 6_827.20m;
    private const string Altin = Kuyumcu.Core.Domain.Maden.Altin;

    [Theory]
    [InlineData(IslemFiyatBaglami.HurdaAlis)]
    [InlineData(IslemFiyatBaglami.Takas)]
    [InlineData(IslemFiyatBaglami.EskiAltinOdeme)]
    public void IB01_Alis_yonlu_bant_yuzde_50_110_tam_sinir_dahil(IslemFiyatBaglami b)
    {
        Assert.Null(FiyatBandi.IslemDenetle(3_413.60m, b, R, Altin));
        Assert.Null(FiyatBandi.IslemDenetle(7_509.92m, b, R, Altin));
        Assert.Null(FiyatBandi.IslemDenetle(5_000m, b, R, Altin));
        var alt = FiyatBandi.IslemDenetle(3_413.59m, b, R, Altin);
        Assert.Equal(new GirdiHatasi(GirdiKodu.IslemFiyatBantDisi, 3_413.60m, 7_509.92m), alt);
        Assert.Equal(GirdiKodu.IslemFiyatBantDisi, FiyatBandi.IslemDenetle(7_509.93m, b, R, Altin)!.Value.Kod);
        Assert.Equal(GirdiKodu.IslemFiyatBantDisi, FiyatBandi.IslemDenetle(2_000m, b, R, Altin)!.Value.Kod);
    }

    [Fact]
    public void IB02_Hurda_satisi_kur_ekraniyla_ayni_arti_eksi_yuzde_20()
    {
        const IslemFiyatBaglami b = IslemFiyatBaglami.HurdaSatis;
        Assert.Null(FiyatBandi.IslemDenetle(5_461.76m, b, R, Altin));
        Assert.Null(FiyatBandi.IslemDenetle(8_192.64m, b, R, Altin));
        Assert.Equal(new GirdiHatasi(GirdiKodu.IslemFiyatBantDisi, 5_461.76m, 8_192.64m),
            FiyatBandi.IslemDenetle(5_461.75m, b, R, Altin));
        Assert.Equal(GirdiKodu.IslemFiyatBantDisi, FiyatBandi.IslemDenetle(8_192.65m, b, R, Altin)!.Value.Kod);
        Assert.Equal((80m, 120m), FiyatBandi.IslemBandi(b));
        Assert.Equal((50m, 110m), FiyatBandi.IslemBandi(IslemFiyatBaglami.HurdaAlis));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(499.99)]
    [InlineData(50_000.01)]
    [InlineData(99_999_999)]
    public void IB03_Mutlak_sinir_referans_olsa_da_olmasa_da_ve_onaylanamaz(double d)
    {
        var x = (decimal)d;
        foreach (var b in Enum.GetValues<IslemFiyatBaglami>())
        {
            var h1 = FiyatBandi.IslemDenetle(x, b, R, Altin);
            var h2 = FiyatBandi.IslemDenetle(x, b, null, Altin);
            Assert.Equal(new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, 500m, 50_000m), h1);
            Assert.Equal(h1, h2);
            Assert.False(FiyatBandi.OnaylanabilirMi(h1!.Value));
        }
    }

    [Fact]
    public void IB04_Bos_sifir_eksi_ve_altin_disi_maden_bu_kuralin_degil()
    {
        Assert.Null(FiyatBandi.IslemDenetle(null, IslemFiyatBaglami.HurdaAlis, R, Altin));
        Assert.Null(FiyatBandi.IslemDenetle(0m, IslemFiyatBaglami.HurdaAlis, R, Altin));
        Assert.Null(FiyatBandi.IslemDenetle(-5m, IslemFiyatBaglami.HurdaAlis, R, Altin));
        // Gümüşün has fiyatı ~80 TL — altın kur kaydı referans olamaz; bant uygulanmaz.
        Assert.Null(FiyatBandi.IslemDenetle(80m, IslemFiyatBaglami.HurdaAlis, R, Kuyumcu.Core.Domain.Maden.Gumus));
        Assert.Null(FiyatBandi.IslemDenetle(80m, IslemFiyatBaglami.HurdaAlis, R, null));
        // Referans yoksa yalnız mutlak sınır.
        Assert.Null(FiyatBandi.IslemDenetle(2_000m, IslemFiyatBaglami.HurdaAlis, null, Altin));
    }

    [Fact]
    public void IB05_Oran_bandi_onaylanabilir_mutlak_onaylanamaz()
    {
        Assert.True(FiyatBandi.OnaylanabilirMi(new GirdiHatasi(GirdiKodu.IslemFiyatBantDisi, 1m, 2m)));
        Assert.True(FiyatBandi.OnaylanabilirMi(new GirdiHatasi(GirdiKodu.FiyatBantDisi, 1m, 20m)));
        Assert.False(FiyatBandi.OnaylanabilirMi(new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, 500m, 50_000m)));
    }

    [Fact]
    public void IB06_Sozluk_TR_EN_iki_uc_iki_ondalik()
    {
        var h = new GirdiHatasi(GirdiKodu.IslemFiyatBantDisi, 3_413.6m, 7_509.92m);
        Assert.Equal("Bu has fiyatı güncel has fiyatına göre olağan aralığın dışında (3.413,60–7.509,92 TL). "
                     + "Rakamı yanlış yazmış olabilirsiniz; kontrol edin.", GirdiSozlugu.Mesaj(h, "tr"));
        var en = GirdiSozlugu.Mesaj(h, "en");
        Assert.Contains("(3,413.60–7,509.92 TL)", en);
        Assert.DoesNotContain("TRY", en);
        // Tur 2 (doğrulayıcı DÜŞÜK): aynı alandaki mutlak sınır iletisi de AYNI para biçimi (iki ondalık).
        var m = new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, 500m, 50_000m);
        Assert.Contains("(500,00–50.000,00 TL)", GirdiSozlugu.Mesaj(m, "tr"));
        Assert.Contains("(500.00–50,000.00 TL)", GirdiSozlugu.Mesaj(m, "en"));
    }

    [Fact]
    public void IB07_Uclar_kurus_yuvarlamasinda_yazilan_uc_kabul_edilir()
    {
        // 6.827,2345 × %50 = 3.413,61725 → alt uç YUKARI 3.413,62; × %110 = 7.509,95795 → üst AŞAĞI 7.509,95.
        Assert.Equal((3_413.62m, 7_509.95m), FiyatBandi.IslemBantUclari(IslemFiyatBaglami.HurdaAlis, 6_827.2345m));
        Assert.Null(FiyatBandi.IslemDenetle(3_413.62m, IslemFiyatBaglami.HurdaAlis, 6_827.2345m, Altin));
        Assert.Null(FiyatBandi.IslemDenetle(7_509.95m, IslemFiyatBaglami.HurdaAlis, 6_827.2345m, Altin));
    }

    [Fact]
    public void IB08_Ekran_gosterimi_sunucuyla_ayni_cumle()
    {
        var ekran = IslemBantGosterim.Metin(2_000m, IslemFiyatBaglami.HurdaAlis, R, "K22", "tr");
        Assert.Equal(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.IslemFiyatBantDisi, 3_413.60m, 7_509.92m), "tr"), ekran);
        Assert.Null(IslemBantGosterim.Metin(80m, IslemFiyatBaglami.HurdaAlis, R, "G925", "tr"));
        Assert.Null(IslemBantGosterim.Metin(6_000m, IslemFiyatBaglami.HurdaAlis, R, "K22", "tr"));
    }
}

/// <summary>Servis kapısı (kullanıcısız iç yol ve rol kapısı doğrudan çağrıda).</summary>
[Collection("veritabani")]
public sealed class IslemBantServisTestleri(TestVeritabani vt)
{
    private const string Gerekce = "Bozuk hurda, ayarı düşük çıktı; fiyatı ustayla teyit ettim.";

    [Theory]
    [InlineData(null)]
    [InlineData("TEZGAHTAR")]
    [InlineData("MUHASEBE")]
    public async Task IB09_Onayli_bant_disi_yalniz_SAHIP_MUDUR_dogrudan_cagrida_403(string? rol)
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETISLEM-BANT-TEST servis " + rol);
        var db = vt.Olustur(k);
        AktifKullanici? islem = rol is null ? null : new AktifKullanici(k.KullaniciId, k.TenantId, k.SubeId, rol);
        var hata = await Assert.ThrowsAsync<ForbiddenException>(() => IslemFiyatKapisi.Denetle(
            db, islem, k.TenantId, IslemFiyatBaglami.HurdaAlis, "ALTIN", 2_000m, "birimFiyatTlHasGram", true, Gerekce));
        Assert.Equal(KurBantSozlugu.Tr.YetkiYok, hata.Message);
        Assert.Empty(db.ChangeTracker.Entries<DenetimKaydi>());
    }

    [Theory]
    [InlineData("SAHIP")]
    [InlineData("MUDUR")]
    public async Task IB09b_Onayli_bant_disi_SAHIP_MUDUR_iz_birakir_onaysiz_400(string rol)
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETISLEM-BANT-TEST servis ok " + rol);
        var db = vt.Olustur(k);
        var islem = new AktifKullanici(k.KullaniciId, k.TenantId, k.SubeId, rol);
        var red = await Assert.ThrowsAsync<BadRequestException>(() => IslemFiyatKapisi.Denetle(
            db, islem, k.TenantId, IslemFiyatBaglami.Takas, "ALTIN", 2_000m, "eskiUrunBirimFiyatTlHasGram", null, Gerekce));
        Assert.StartsWith("Bu has fiyatı güncel has fiyatına göre", red.Message);
        await IslemFiyatKapisi.Denetle(
            db, islem, k.TenantId, IslemFiyatBaglami.Takas, "ALTIN", 2_000m, "eskiUrunBirimFiyatTlHasGram", true, Gerekce);
        var iz = Assert.Single(db.ChangeTracker.Entries<DenetimKaydi>()).Entity;
        Assert.Equal(IslemFiyatKapisi.DenetimVarlikTuru, iz.VarlikTuru);
        Assert.Equal(k.KurId, iz.VarlikId);
        Assert.True(iz.Hassas);
        Assert.Contains("Takas", iz.Detay);
        Assert.Contains("eskiUrunBirimFiyatTlHasGram=2000", iz.Detay);
        Assert.Contains("güncel has 6827.2", iz.Detay);
        Assert.Contains(Gerekce, iz.Detay);
    }

    [Fact]
    public async Task IB09c_Referans_iptal_edilmis_kaydi_atlar_son_gecerli_kaydi_okur()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETISLEM-BANT-TEST referans");
        var yeni = IslemBantFikstur.ReferansKur(sistem, k.TenantId, 3_000m);
        Assert.Equal((yeni, 3_000m), await IslemFiyatKapisi.Referans(vt.Olustur(k), k.TenantId));
        var s = sistem.KurSnapshots.Single(x => x.Id == yeni);
        s.IptalEdildi = true;
        sistem.SaveChanges();
        Assert.Equal((k.KurId, 6_827.20m), await IslemFiyatKapisi.Referans(vt.Olustur(k), k.TenantId));
    }
}

/// <summary>HTTP (gerçek MySQL): her işlem türü için bant içi/dışı/sert sınır, onaylı aşım + iz, yetkisiz 403.</summary>
[Collection("http")]
public sealed class IslemBantHttpTestleri(HttpFikstur f)
{
    private const string Gerekce = "Bozuk hurda, ayarı düşük çıktı; fiyatı ustayla teyit ettim.";
    private static readonly object Satici = new { saticiTipi = "NIHAI_TUKETICI", adSoyad = "Ali Veli", tckn = "10000000146", adres = "Ulus" };

    private static string M(GirdiKodu kod, decimal? s1 = null, decimal? s2 = null) =>
        GirdiSozlugu.Mesaj(new GirdiHatasi(kod, s1, s2), "tr");
    private static readonly string AlisBandi = M(GirdiKodu.IslemFiyatBantDisi, 3_413.60m, 7_509.92m);
    private static readonly string SatisBandi = M(GirdiKodu.IslemFiyatBantDisi, 5_461.76m, 8_192.64m);
    private static readonly string Mutlak = M(GirdiKodu.FiyatMutlakDisi, 500m, 50_000m);

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETISLEM-BANT-TEST " + ad);   // has 6.827,20
    }

    private int Hareket(Kiraci k, string tur)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Hareketler.AsNoTracking().Count(h => h.TenantId == k.TenantId && h.Tur == tur);
    }

    private List<DenetimKaydi> Izler(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikTuru == IslemFiyatKapisi.DenetimVarlikTuru).ToList();
    }

    private static async Task<string> Red(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), govde);
        var p = alanlar.EnumerateObject().FirstOrDefault(x => x.Name.Equals(alan, StringComparison.OrdinalIgnoreCase));
        Assert.True(p.Value.ValueKind == JsonValueKind.Array, $"'{alan}' işaretli değil: {govde}");
        return p.Value[0].GetString() ?? "";
    }

    private static StringContent Ham(string json) => new(json, Encoding.UTF8, "application/json");

    private static Dictionary<string, object?> HurdaAlis(Kiraci k, object fiyat, string ayar = "K22") => new()
    {
        ["cariId"] = k.CariId, ["ayarId"] = ayar, ["gramAgirlik"] = 1m, ["birimFiyatTlHasGram"] = fiyat,
        ["kasaId"] = k.KasaId, ["kurSnapshotId"] = k.KurId, ["odemeTuru"] = "NAKIT", ["satici"] = Satici,
    };

    private static Dictionary<string, object?> Takas(Kiraci k, decimal fiyat) => new()
    {
        ["cariId"] = k.CariId, ["eskiUrunAyarId"] = "K22", ["eskiUrunGram"] = 3m, ["eskiUrunBirimFiyatTlHasGram"] = fiyat,
        ["yeniUrunId"] = k.UrunId, ["yeniUrunAdet"] = 1m, ["yeniUrunBirimFiyatTl"] = 20_000m,
        ["odemeTuru"] = "NAKIT", ["kasaId"] = k.KasaId, ["kurSnapshotId"] = k.KurId, ["satici"] = Satici,
    };

    private static Dictionary<string, object?> HurdaSatis(Kiraci k, decimal fiyat) => new()
    {
        ["maden"] = "ALTIN", ["hasGrami"] = 2m, ["birimFiyatTlHasGram"] = fiyat,
        ["kasaId"] = k.KasaId, ["kurSnapshotId"] = k.KurId, ["odemeTuru"] = "HAVALE",
    };

    /// <summary>Satış fişi: 30.000 TL kalem; eski altın 5 g 22 ayar (4,58 has) × fiyat, kalanı nakit.</summary>
    private static object Fis(Kiraci k, decimal fiyat, bool? onay = null, string? gerekce = null)
    {
        var altinTl = Math.Round(4.58m * fiyat, 2, MidpointRounding.AwayFromZero);
        return new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1, birimFiyatTl = 30_000m } },
            odemeler = new object[]
            {
                new { arac = "NAKIT", tutarTl = 30_000m - altinTl, kasaId = k.KasaId },
                new
                {
                    arac = "ALTIN", tutarTl = altinTl, ayarId = "K22", gramAgirlik = 5m, birimFiyatTlHasGram = fiyat,
                    satici = Satici,
                },
            },
            bantOnay = onay, bantGerekce = gerekce,   // fiş düzeyinde (SatisFisiDto)
        };
    }

    private static Dictionary<string, object?> Onayli(Dictionary<string, object?> g, string gerekce = Gerekce)
    {
        g["bantOnay"] = true; g["bantGerekce"] = gerekce;
        return g;
    }

    // ── Hurda alış ──────────────────────────────────────────────────────────

    [Fact]
    public async Task IB10_Hurda_alis_hasim_degerleri_400_alanli_hareket_yazilmaz()
    {
        var k = YeniKiraci("hurda hasim");
        var c = f.Ortam.Istemci(k);
        Assert.Equal(Mutlak, await Red(await c.PostJson("/api/v1/islemler/hurda-alis", HurdaAlis(k, 5m)), "birimFiyatTlHasGram"));
        Assert.Equal(Mutlak, await Red(await c.PostJson("/api/v1/islemler/hurda-alis", HurdaAlis(k, 99_999_999m)), "birimFiyatTlHasGram"));
        Assert.Equal(AlisBandi, await Red(await c.PostJson("/api/v1/islemler/hurda-alis", HurdaAlis(k, 2_000m)), "birimFiyatTlHasGram"));
        Assert.Equal(AlisBandi, await Red(await c.PostJson("/api/v1/islemler/hurda-alis", HurdaAlis(k, 3_413.59m)), "birimFiyatTlHasGram"));
        Assert.Equal(AlisBandi, await Red(await c.PostJson("/api/v1/islemler/hurda-alis", HurdaAlis(k, 7_509.93m)), "birimFiyatTlHasGram"));
        // 0 ve eksi: mevcut kurallar (sıfır olamaz / Range) — bant bunlara karışmaz ama yine 400.
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostJson("/api/v1/islemler/hurda-alis", HurdaAlis(k, 0m))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostJson("/api/v1/islemler/hurda-alis", HurdaAlis(k, -5m))).StatusCode);
        // Harf: JSON çözümlenemez → 400.
        var harf = await c.PostAsync("/api/v1/islemler/hurda-alis", Ham(
            $$"""{"cariId":"{{k.CariId}}","ayarId":"K22","gramAgirlik":1,"birimFiyatTlHasGram":"altibin","kasaId":"{{k.KasaId}}","kurSnapshotId":"{{k.KurId}}"}"""));
        Assert.Equal(HttpStatusCode.BadRequest, harf.StatusCode);
        Assert.Equal(0, Hareket(k, "HURDA_ALIS"));
        Assert.Empty(Izler(k));
    }

    [Fact]
    public async Task IB11_Hurda_alis_bant_ici_ve_tam_sinir_200_iz_yok()
    {
        var k = YeniKiraci("hurda ici");
        var c = f.Ortam.Istemci(k);
        foreach (var fiyat in new[] { 3_413.60m, 6_000m, 7_509.92m })
        {
            var r = await c.PostJson("/api/v1/islemler/hurda-alis", HurdaAlis(k, fiyat));
            Assert.True(r.IsSuccessStatusCode, $"{fiyat}: {await r.Content.ReadAsStringAsync()}");
        }
        Assert.Equal(3, Hareket(k, "HURDA_ALIS"));
        Assert.Empty(Izler(k));
    }

    [Theory]
    [InlineData("SAHIP")]
    [InlineData("MUDUR")]
    public async Task IB12_Hurda_alis_SAHIP_MUDUR_onayli_asim_200_ve_denetim_izi(string rol)
    {
        var k = YeniKiraci("hurda onay " + rol);
        var c = f.Ortam.Istemci(k, rol);
        Assert.Equal(M(GirdiKodu.GerekceKisa, 20m, 5m),
            await Red(await c.PostJson("/api/v1/islemler/hurda-alis", Onayli(HurdaAlis(k, 2_000m), "doğru")), "bantGerekce"));
        // Onay mutlak sınırı AŞAMAZ.
        Assert.Equal(Mutlak, await Red(await c.PostJson("/api/v1/islemler/hurda-alis", Onayli(HurdaAlis(k, 5m))), "birimFiyatTlHasGram"));
        Assert.Equal(0, Hareket(k, "HURDA_ALIS"));

        var r = await c.PostJson("/api/v1/islemler/hurda-alis", Onayli(HurdaAlis(k, 2_000m)));
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        Assert.Equal(1, Hareket(k, "HURDA_ALIS"));
        var iz = Assert.Single(Izler(k));
        Assert.Equal(k.KullaniciId, iz.KullaniciId);
        Assert.Equal(k.KurId, iz.VarlikId);
        Assert.True(iz.Hassas);
        Assert.Contains("HurdaAlis", iz.Detay);
        Assert.Contains("birimFiyatTlHasGram=2000", iz.Detay);
        Assert.Contains(Gerekce, iz.Detay);
    }

    [Fact]
    public async Task IB13_Hurda_alis_MUHASEBE_onayli_asim_403_hareket_yok()
    {
        var k = YeniKiraci("hurda muhasebe");
        var r = await f.Ortam.Istemci(k, "MUHASEBE").PostJson("/api/v1/islemler/hurda-alis", Onayli(HurdaAlis(k, 2_000m)));
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal(KurBantSozlugu.Tr.YetkiYok, (await r.Json()).GetProperty("message").GetString());
        Assert.Equal(0, Hareket(k, "HURDA_ALIS"));
        Assert.Empty(Izler(k));
        // Bant içi fiyat MUHASEBE'ye açık kalır (yetki daralmadı).
        var ok = await f.Ortam.Istemci(k, "MUHASEBE").PostJson("/api/v1/islemler/hurda-alis", HurdaAlis(k, 6_000m));
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task IB14_Gumus_hurda_alisi_bantsiz_80_TL_kabul()
    {
        var k = YeniKiraci("gumus");
        var r = await f.Ortam.Istemci(k).PostJson("/api/v1/islemler/hurda-alis", HurdaAlis(k, 80m, "G925"));
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task IB15_Referans_son_gecerli_kur_kayit_eklenince_bant_kayar()
    {
        var k = YeniKiraci("referans kayar");
        var c = f.Ortam.Istemci(k);
        Assert.Equal(AlisBandi, await Red(await c.PostJson("/api/v1/islemler/hurda-alis", HurdaAlis(k, 3_000m)), "birimFiyatTlHasGram"));
        using (var db = f.Vt.OlusturSistem()) IslemBantFikstur.ReferansKur(db, k.TenantId, 4_000m);   // bant 2.000–4.400
        var r = await c.PostJson("/api/v1/islemler/hurda-alis", HurdaAlis(k, 3_000m));
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
    }

    // ── Hurda satışı (ramatçıya) ± %20 ─────────────────────────────────────

    [Fact]
    public async Task IB16_Hurda_satisi_bant_disi_400_onayli_200_iz_MUHASEBE_403()
    {
        var k = YeniKiraci("hurda satis");
        var sahip = f.Ortam.Istemci(k);
        var alis = await sahip.PostJson("/api/v1/islemler/hurda-alis", new Dictionary<string, object?>(HurdaAlis(k, 6_000m)) { ["gramAgirlik"] = 10m, ["odemeTuru"] = "HAVALE" });
        Assert.True(alis.IsSuccessStatusCode, await alis.Content.ReadAsStringAsync());

        Assert.Equal(SatisBandi, await Red(await sahip.PostJson("/api/v1/islemler/hurda-satis", HurdaSatis(k, 9_000m)), "birimFiyatTlHasGram"));
        Assert.Equal(SatisBandi, await Red(await sahip.PostJson("/api/v1/islemler/hurda-satis", HurdaSatis(k, 5_461.75m)), "birimFiyatTlHasGram"));
        Assert.Equal(Mutlak, await Red(await sahip.PostJson("/api/v1/islemler/hurda-satis", Onayli(HurdaSatis(k, 99_999_999m))), "birimFiyatTlHasGram"));
        var muh = await f.Ortam.Istemci(k, "MUHASEBE").PostJson("/api/v1/islemler/hurda-satis", Onayli(HurdaSatis(k, 9_000m)));
        Assert.Equal(HttpStatusCode.Forbidden, muh.StatusCode);
        Assert.Equal(0, Hareket(k, "HURDA_SATIS"));

        var ici = await sahip.PostJson("/api/v1/islemler/hurda-satis", HurdaSatis(k, 6_900m));
        Assert.True(ici.IsSuccessStatusCode, await ici.Content.ReadAsStringAsync());
        var onayli = await sahip.PostJson("/api/v1/islemler/hurda-satis", Onayli(HurdaSatis(k, 9_000m)));
        Assert.True(onayli.IsSuccessStatusCode, await onayli.Content.ReadAsStringAsync());
        Assert.Equal(2, Hareket(k, "HURDA_SATIS"));
        var iz = Assert.Single(Izler(k));
        Assert.Contains("HurdaSatis", iz.Detay);
        Assert.Contains("bant %80–%120", iz.Detay);
    }

    // ── Takas ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task IB17_Takas_bant_disi_400_ici_200_TEZGAHTAR_onay_403_SAHIP_onayli_200_iz()
    {
        var k = YeniKiraci("takas");
        var sahip = f.Ortam.Istemci(k);
        Assert.Equal(AlisBandi, await Red(await sahip.PostJson("/api/v1/islemler/takas", Takas(k, 3_000m)), "eskiUrunBirimFiyatTlHasGram"));
        Assert.Equal(Mutlak, await Red(await sahip.PostJson("/api/v1/islemler/takas", Takas(k, 99_999_999m)), "eskiUrunBirimFiyatTlHasGram"));
        var tez = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson("/api/v1/islemler/takas", Onayli(Takas(k, 3_000m)));
        Assert.Equal(HttpStatusCode.Forbidden, tez.StatusCode);
        Assert.Equal(0, Hareket(k, "TAKAS"));

        var ici = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson("/api/v1/islemler/takas", Takas(k, 6_000m));
        Assert.True(ici.IsSuccessStatusCode, await ici.Content.ReadAsStringAsync());
        var onayli = await sahip.PostJson("/api/v1/islemler/takas", Onayli(Takas(k, 3_000m)));
        Assert.True(onayli.IsSuccessStatusCode, await onayli.Content.ReadAsStringAsync());
        Assert.Equal(2, Hareket(k, "TAKAS"));
        var iz = Assert.Single(Izler(k));
        Assert.Contains("Takas", iz.Detay);
        Assert.Contains("eskiUrunBirimFiyatTlHasGram=3000", iz.Detay);
    }

    // ── Eski altınla ödeme (satış fişi ALTIN bacağı) ───────────────────────

    [Fact]
    public async Task IB18_Eski_altinla_odeme_bant_disi_400_ici_200_TEZGAHTAR_403_SAHIP_onayli_200()
    {
        var k = YeniKiraci("eski altin");
        var sahip = f.Ortam.Istemci(k);
        Assert.Equal(AlisBandi, await Red(await sahip.PostJson("/api/v1/islemler/satis-fisi", Fis(k, 2_000m)), "birimFiyatTlHasGram"));
        Assert.Equal(Mutlak, await Red(await sahip.PostJson("/api/v1/islemler/satis-fisi", Fis(k, 100m, true, Gerekce)), "birimFiyatTlHasGram"));
        var tez = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson("/api/v1/islemler/satis-fisi", Fis(k, 2_000m, true, Gerekce));
        Assert.Equal(HttpStatusCode.Forbidden, tez.StatusCode);
        Assert.Equal(0, Hareket(k, "SATIS"));

        var ici = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson("/api/v1/islemler/satis-fisi", Fis(k, 6_000m));
        Assert.True(ici.IsSuccessStatusCode, await ici.Content.ReadAsStringAsync());
        var onayli = await sahip.PostJson("/api/v1/islemler/satis-fisi", Fis(k, 2_000m, true, Gerekce));
        Assert.True(onayli.IsSuccessStatusCode, await onayli.Content.ReadAsStringAsync());
        var iz = Assert.Single(Izler(k));
        Assert.Contains("EskiAltinOdeme", iz.Detay);
    }
}

/// <summary>PATCH /kullanicilar/ben — alan beyaz listesi: modülü olmayan rol yalnız kendi modunu yazar.</summary>
[Collection("http")]
public sealed class IslemBantKendiModHttpTestleri(HttpFikstur f)
{
    private const string Ben = "/api/v1/kullanicilar/ben";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETISLEM-BANT-TEST " + ad);
    }

    private Kullanici Satir(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Kullanicilar.AsNoTracking().Single(x => x.Id == k.KullaniciId);
    }

    private static StringContent Ham(string json) => new(json, Encoding.UTF8, "application/json");

    [Theory]
    [InlineData("TEZGAHTAR")]
    [InlineData("MUHASEBE")]
    public async Task IB20_Modulsuz_rol_kendi_modunu_200_ile_yazar_iz_dusulur(string rol)
    {
        var k = YeniKiraci("mod " + rol);
        var c = f.Ortam.Istemci(k, rol);
        var r = await c.PatchAsync(Ben, Ham("""{"arayuzModu":"BASIT"}"""));
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        Assert.Equal("BASIT", (await r.Json()).GetProperty("arayuzModu").GetString());
        Assert.Equal("BASIT", Satir(k).ArayuzModu);
        using var db = f.Vt.OlusturSistem();
        Assert.Contains(db.DenetimKayitlari.AsNoTracking().Where(d => d.TenantId == k.TenantId && d.VarlikId == k.KullaniciId).ToList(),
            d => d.Detay == "arayuz-modu: BASIT");
    }

    [Theory]
    [InlineData("""{"ad":"Hacker"}""")]
    [InlineData("""{"arayuzModu":"BASIT","soyad":"Yeni"}""")]
    [InlineData("""{"email":"baska@ornek.com"}""")]
    [InlineData("""{"arayuzModu":"BASIT","subeId":null}""")]
    [InlineData("""{"aktif":false}""")]
    public async Task IB21_TEZGAHTAR_mod_disi_alan_403_hicbir_sey_yazilmaz(string govde)
    {
        var k = YeniKiraci("mod disi");
        var once = Satir(k);
        var r = await f.Ortam.Istemci(k, "TEZGAHTAR").PatchAsync(Ben, Ham(govde));
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal(IslemBantSozlugu.Tr.YalnizModDegisir, (await r.Json()).GetProperty("message").GetString());
        var sonra = Satir(k);
        Assert.Equal((once.Ad, once.Soyad, once.Email, once.SubeId, once.Aktif, once.ArayuzModu),
            (sonra.Ad, sonra.Soyad, sonra.Email, sonra.SubeId, sonra.Aktif, sonra.ArayuzModu));
    }

    [Fact]
    public async Task IB22_PATCH_sozlesmesi_yoksa_koru_null_ise_devral_gecersiz_400()
    {
        var k = YeniKiraci("mod patch");
        var c = f.Ortam.Istemci(k, "TEZGAHTAR");
        Assert.Equal(HttpStatusCode.OK, (await c.PatchAsync(Ben, Ham("""{"arayuzModu":"PROFESYONEL"}"""))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PatchAsync(Ben, Ham("{}"))).StatusCode);
        Assert.Equal("PROFESYONEL", Satir(k).ArayuzModu);                       // gövdede yok → korunur
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PatchAsync(Ben, Ham("""{"arayuzModu":"KARMA"}"""))).StatusCode);
        Assert.Equal("PROFESYONEL", Satir(k).ArayuzModu);
        Assert.Equal(HttpStatusCode.OK, (await c.PatchAsync(Ben, Ham("""{"arayuzModu":null}"""))).StatusCode);
        Assert.Null(Satir(k).ArayuzModu);                                       // null → kiracı varsayılanını devral
    }

    [Fact]
    public async Task IB23_SAHIP_ve_MUDUR_eskisi_gibi_ad_soyad_yazar()
    {
        var k = YeniKiraci("mod sahip");
        var r = await f.Ortam.Istemci(k, "SAHIP").PatchAsync(Ben, Ham("""{"ad":"Yeni Ad","arayuzModu":"BASIT"}"""));
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        Assert.Equal(("Yeni Ad", "BASIT"), (Satir(k).Ad, Satir(k).ArayuzModu));
        var m = await f.Ortam.Istemci(k, "MUDUR").PatchAsync(Ben, Ham("""{"soyad":"Müdür"}"""));
        Assert.True(m.IsSuccessStatusCode, await m.Content.ReadAsStringAsync());
        Assert.Equal("Müdür", Satir(k).Soyad);
    }

    [Fact]
    public async Task IB24_Anonim_401()
    {
        var r = await f.Ortam.AnonimIstemci().PatchAsync(Ben, Ham("""{"arayuzModu":"BASIT"}"""));
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }
}

/// <summary>Ekran: alan altı sunucuyla AYNI cümle (hurda alış, takas, eski altın paneli).</summary>
public sealed class IslemBantEkranTestleri : BunitContext
{
    public IslemBantEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Bos()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class Bos : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") });
    }

    private static readonly string AlisBandi =
        GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.IslemFiyatBantDisi, 3_413.60m, 7_509.92m), "tr");

    private void Kur(decimal has) =>
        Services.GetRequiredService<CanliKurServisi>().Uygula(new CanliKurServisi.KurAnlik("kur-1", has, null, null, null, null));

    [Fact]
    public void IB30_Hurda_alis_alan_alti_bant_metni_ici_yok()
    {
        Kur(6_827.20m);
        var hurda = new HurdaGirisi { Ayar = "K22" };
        hurda.FiyatGirildi(2_000m);
        var e = Render<FisHurdaAlis>(p => p.Add(x => x.Hurda, hurda).Add(x => x.Ayarlar, new[] { "K22", "G925" }));
        Assert.Contains(AlisBandi, e.Markup);

        hurda.FiyatGirildi(6_000m);
        e.Render(p => p.Add(x => x.Hurda, hurda));
        Assert.DoesNotContain("olağan aralığın dışında", e.Markup);
    }

    [Fact]
    public void IB31_Takas_alan_alti_bant_metni_ve_mutlak_sinir()
    {
        Kur(6_827.20m);
        var takas = new TakasGirisi { EskiAyar = "K22" };
        takas.FiyatGirildi(3_000m);
        var e = Render<FisTakas>(p => p.Add(x => x.Takas, takas).Add(x => x.Ayarlar, new[] { "K22" })
            .Add(x => x.UrunSecenekleri, Array.Empty<Kuyumcu.Web.Bilesenler.SecenekOgesi>()));
        Assert.Contains(AlisBandi, e.Markup);
        takas.FiyatGirildi(99_999_999m);
        e.Render(p => p.Add(x => x.Takas, takas));
        Assert.Contains(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, 500m, 50_000m), "tr"), e.Markup);
    }

    [Fact]
    public void IB32_Eski_altin_paneli_alan_alti_bant_metni()
    {
        var giris = new AltinBacagiGirisi { AyarId = "K22", Gram = 5m, Fiyat = 2_000m, FiyatElle = true };
        var e = Render<Kuyumcu.Web.Bilesenler.AltinBacagiPaneli>(p => p.Add(x => x.Giris, giris).Add(x => x.HasFiyati, 6_827.20m));
        Assert.Contains(AlisBandi, e.Markup);
    }

    private void Rol(string rol) => Services.GetRequiredService<OturumServisi>().KullaniciAta(
        new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1"));

    [Theory]
    [InlineData("SAHIP")]
    [InlineData("MUDUR")]
    public void IB34_Yetkili_rolde_onay_kutusu_isaretlenince_gerekce_kisa_gerekce_uyarisi(string rol)
    {
        Rol(rol);
        Kur(6_827.20m);
        var hurda = new HurdaGirisi { Ayar = "K22" };
        hurda.FiyatGirildi(2_000m);
        var e = Render<FisHurdaAlis>(p => p.Add(x => x.Hurda, hurda).Add(x => x.Ayarlar, new[] { "K22" }));
        Assert.Empty(e.FindAll("#hurda-bant-yetki"));
        Assert.Empty(e.FindAll("#hurda-bant-gerekce"));
        e.Find("#hurda-bant-onay").Change(true);
        Assert.True(hurda.Bant.Onay);
        Assert.Equal(true, hurda.Bant.GovdeOnay);
        e.Find("#hurda-bant-gerekce").Input("kısa");
        Assert.Contains(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.GerekceKisa, 20m, 4m), "tr"), e.Markup);
        e.Find("#hurda-bant-gerekce").Input("Bozuk hurda, ayarı düşük çıktı; teyit ettim.");
        Assert.Equal("Bozuk hurda, ayarı düşük çıktı; teyit ettim.", hurda.Bant.GovdeGerekce);
        Assert.DoesNotContain("Gerekçe en az", e.Markup);
    }

    [Theory]
    [InlineData("TEZGAHTAR")]
    [InlineData("MUHASEBE")]
    public void IB35_Yetkisiz_rolde_onay_yolu_yok_tek_kucuk_yetki_cumlesi(string rol)
    {
        Rol(rol);
        Kur(6_827.20m);
        var takas = new TakasGirisi { EskiAyar = "K22" };
        takas.FiyatGirildi(3_000m);
        var e = Render<FisTakas>(p => p.Add(x => x.Takas, takas).Add(x => x.Ayarlar, new[] { "K22" })
            .Add(x => x.UrunSecenekleri, Array.Empty<Kuyumcu.Web.Bilesenler.SecenekOgesi>()));
        Assert.Empty(e.FindAll("#takas-bant-onay"));
        var yetki = e.Find("#takas-bant-yetki");
        Assert.Equal(KurBantSozlugu.Tr.YetkiYok, yetki.TextContent.Trim());
        Assert.Contains("metin-kucuk", yetki.ClassName);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(e.Markup, System.Text.RegularExpressions.Regex.Escape(KurBantSozlugu.Tr.YetkiYok)));
    }

    [Fact]
    public void IB36_Mutlak_sinir_ve_bant_ici_onay_yolu_cizilmez()
    {
        Rol("SAHIP");
        Kur(6_827.20m);
        var hurda = new HurdaGirisi { Ayar = "K22" };
        hurda.FiyatGirildi(5m);
        var e = Render<FisHurdaAlis>(p => p.Add(x => x.Hurda, hurda).Add(x => x.Ayarlar, new[] { "K22" }));
        Assert.Empty(e.FindAll("#hurda-bant-onay, #hurda-bant-yetki"));
        hurda.FiyatGirildi(6_000m);
        e.Render(p => p.Add(x => x.Hurda, hurda));
        Assert.Empty(e.FindAll("#hurda-bant-onay, #hurda-bant-yetki"));
    }

    [Fact]
    public void IB37_Eski_altin_paneli_SAHIP_onay_fis_govdesine_bacaktan_tasinir()
    {
        Rol("SAHIP");
        var giris = new AltinBacagiGirisi { AyarId = "K22", Gram = 5m, Fiyat = 2_000m, FiyatElle = true };
        var e = Render<Kuyumcu.Web.Bilesenler.AltinBacagiPaneli>(p => p.Add(x => x.Giris, giris).Add(x => x.HasFiyati, 6_827.20m));
        e.Find("#altin-bant-onay").Change(true);
        e.Find("#altin-bant-gerekce").Input("Müşterinin altını eski ve ayarı düşük, teyitli.");
        var bacaklar = new List<FisBacagi> { new() { Arac = "NAKIT" }, new() { Arac = "ALTIN", Altin = giris } };
        var bant = FisAltinBacagi.Bant(bacaklar);
        Assert.Equal(true, bant?.GovdeOnay);
        Assert.Equal("Müşterinin altını eski ve ayarı düşük, teyitli.", bant?.GovdeGerekce);
        Assert.Null(FisAltinBacagi.Bant(new List<FisBacagi> { new() { Arac = "NAKIT" } }));
    }

    [Fact]
    public void IB38_Onay_isaretsizken_govdeye_alan_gitmez()
    {
        var d = new IslemBantOnayDurumu { Gerekce = "yazıldı ama onay yok" };
        Assert.Null(d.GovdeOnay);
        Assert.Null(d.GovdeGerekce);
    }

    [Fact]
    public void IB33_Canli_kur_yoksa_yalniz_mutlak_sinir_ekran_cokmez()
    {
        var hurda = new HurdaGirisi { Ayar = "K22" };
        hurda.FiyatGirildi(2_000m);
        var e = Render<FisHurdaAlis>(p => p.Add(x => x.Hurda, hurda).Add(x => x.Ayarlar, new[] { "K22" }));
        Assert.DoesNotContain("olağan aralığın dışında", e.Markup);
        hurda.FiyatGirildi(5m);
        e.Render(p => p.Add(x => x.Hurda, hurda));
        Assert.Contains(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, 500m, 50_000m), "tr"), e.Markup);
    }
}

/// <summary>
/// PAKET ISLEM-BANT tur 2 (tasarım hakemi KRİTİK): BASİT Değişim tuvalinde VERDİM sütunundaki elle satırı geniş ekran
/// kuralıyla tek satıra diziliyor, sütunu aşıp ALDIM'daki bant hatasının üstüne biniyordu. Kural dosyada durmalı:
/// arama ızgarası minmax(0,1fr), elle satırı her genişlikte dar düzen (Ürün tam satır, Adet · Fiyat · Ekle altında).
/// Görsel kanıt gerçek Chrome'da (rapor § 9); bu test kuralın silinmesini yakalar.
/// </summary>
public sealed class IslemBantDegisimCssTestleri
{
    private static string Govde()
    {
        var css = File.ReadAllText(Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web", "wwwroot", "css", "degisim-gunsonu.css"));
        return System.Text.RegularExpressions.Regex.Replace(css, @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
    }

    [Fact]
    public void IB39_BASIT_degisim_verdim_elle_satiri_sutunu_asmaz()
    {
        var g = Govde();
        Assert.Contains(".islem-sayfa #takas-verdim .dg-takas-arama { grid-template-columns: minmax(0, 1fr); }", g);
        Assert.Matches(@"\.islem-sayfa #takas-verdim \.satir\.fis-kalem-satiri \{\s*display: grid;\s*grid-template-columns: minmax\(0, 1fr\) minmax\(0, 1fr\) auto;", g);
        Assert.Contains(".islem-sayfa #takas-verdim .fis-kalem-satiri > .alan.esnek { grid-column: 1 / -1;", g);
        Assert.Contains(".islem-sayfa #takas-verdim .fis-kalem-satiri > #islem-kalem-ekle { grid-column: 3; grid-row: 5;", g);
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgb\(|hsl\(", g);   // sabit renk yok
    }
}
