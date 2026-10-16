using System.Net;
using System.Text;
using System.Text.Json;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET KUR-BANT finansal sınır testleri (09.10.2026).
// Elle kur girişi: sıfır · eksi · 5. ondalık (yuvarlanmaz → 400) · 4 hane bant sınırı ·
// çok büyük tutar · boş/null · Türkçe giriş çözümü. Beklenen değerler SABİT; üretim kodundan
// hesaplanmaz. olumsuz-fikstür: dosya — geçersiz değerler bilinçlidir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Saf: Core FiyatBandi sınırları 4 hane çözünürlükte (sabit beklenen).</summary>
public sealed class KURBANTSinirSafTestleri
{
    [Theory]
    [InlineData("499.9999", false)]
    [InlineData("500.0000", true)]
    [InlineData("50000.0000", true)]
    [InlineData("50000.0001", false)]
    public void S01_Has_mutlak_sinir_4_hane(string ham, bool kabul)
    {
        var d = decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture);
        var h = FiyatBandi.Denetle(d, FiyatTuru.Has, null);
        if (kabul) Assert.Null(h); else Assert.Equal(GirdiKodu.FiyatMutlakDisi, h?.Kod);
    }

    [Theory]
    [InlineData("8192.6400", true)]    // 6.827,20 × 1,2 tam
    [InlineData("8192.6401", false)]   // 0,0001 fazlası
    [InlineData("5461.7600", true)]    // 6.827,20 × 0,8 tam
    [InlineData("5461.7599", false)]   // 0,0001 eksiği
    public void S02_Onceki_degere_gore_bant_4_hane(string ham, bool kabul)
    {
        var d = decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture);
        var h = FiyatBandi.Denetle(d, FiyatTuru.Has, 6_827.20m);
        if (kabul) Assert.Null(h); else Assert.Equal(GirdiKodu.FiyatBantDisi, h?.Kod);
    }

    [Fact]
    public void S03_Cok_buyuk_tutar_mutlak_disi_doviz_ve_has()
    {
        Assert.Equal(new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, 500m, 50_000m),
            FiyatBandi.Denetle(999_999_999.99m, FiyatTuru.Has, 6_800m));
        Assert.Equal(new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, 5m, 500m),
            FiyatBandi.Denetle(999_999_999.99m, FiyatTuru.Doviz, 41m));
    }

    [Fact]
    public void S04_Onceki_sifir_ya_da_eksi_ise_yalniz_mutlak_sinir()
    {
        Assert.Null(FiyatBandi.Denetle(40m, FiyatTuru.Doviz, 0m));
        Assert.Null(FiyatBandi.Denetle(40m, FiyatTuru.Doviz, -10m));
    }

    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("6.827,2", "6827.2")]
    [InlineData("1,5", "1.5")]
    public void S05_Turkce_giris_cozumu(string ham, string beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }

    [Fact]
    public void S06_Belirsiz_1_500_reddedilir()
    {
        var c = FormAlani.SayiCoz("1.500");
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, c.Sonuc);
        Assert.Null(c.Deger);
    }
}

/// <summary>HTTP: controller + DTO + servis kapısı birlikte (gerçek MySQL).</summary>
[Collection("http")]
public sealed class KURBANTSinirHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETKUR-BANT-TEST dg " + ad);   // fikstür has 6.827,20
    }

    private int Sayi(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.KurSnapshots.AsNoTracking().Count(s => s.TenantId == k.TenantId);
    }

    private static StringContent Ham(string json) => new(json, Encoding.UTF8, "application/json");

    private static async Task<string> RedAlan(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), govde);
        var p = alanlar.EnumerateObject().FirstOrDefault(x => x.Name.Equals(alan, StringComparison.OrdinalIgnoreCase));
        Assert.True(p.Value.ValueKind == JsonValueKind.Array, $"'{alan}' işaretli değil: {govde}");
        return p.Value[0].GetString() ?? "";
    }

    [Theory]
    [InlineData("0", "0'dan büyük")]
    [InlineData("-5", "eksi")]
    public async Task H01_Sifir_ve_eksi_400_K16_mesaji_kayit_yok(string deger, string parca)
    {
        var k = YeniKiraci("sifir" + deger);
        var c = f.Ortam.Istemci(k);
        var once = Sayi(k);
        var m = await RedAlan(await c.PostAsync("/api/v1/kur/manuel", Ham($$"""{"altinHasFiyati":{{deger}}}""")), "altinHasFiyati");
        Assert.Contains(parca, m, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(once, Sayi(k));
    }

    [Fact]
    public async Task H02_Bes_ondalik_yuvarlanmaz_400_dort_ondalik_birebir()
    {
        var k = YeniKiraci("ondalik");
        var c = f.Ortam.Istemci(k);
        await RedAlan(await c.PostAsync("/api/v1/kur/manuel", Ham("""{"altinHasFiyati":6827.00005}""")), "altinHasFiyati");
        var ok = await c.PostAsync("/api/v1/kur/manuel", Ham("""{"altinHasFiyati":8192.6400}"""));
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(8_192.64m, db.KurSnapshots.AsNoTracking().Where(s => s.TenantId == k.TenantId)
            .OrderByDescending(s => s.Olusturuldu).First().AltinHasFiyati);
    }

    [Fact]
    public async Task H03_Bant_ust_sinir_0_0001_fazlasi_400()
    {
        var k = YeniKiraci("bant4");
        var c = f.Ortam.Istemci(k);
        var once = Sayi(k);
        var m = await RedAlan(await c.PostAsync("/api/v1/kur/manuel", Ham("""{"altinHasFiyati":8192.6401}""")), "altinHasFiyati");
        Assert.Contains("6.827,2", m);
        Assert.Equal(once, Sayi(k));
    }

    [Fact]
    public async Task H04_Cok_buyuk_tutar_onaysiz_400_kayit_yok()
    {
        var k = YeniKiraci("buyuk");
        var c = f.Ortam.Istemci(k);
        var once = Sayi(k);
        var m = await RedAlan(await c.PostAsync("/api/v1/kur/manuel", Ham("""{"altinHasFiyati":999999999.99}""")), "altinHasFiyati");
        Assert.Contains("500", m);
        Assert.Equal(once, Sayi(k));
    }

    [Fact]
    public async Task H05_Bos_null_doviz_alanlari_kabul_has_null_400()
    {
        var k = YeniKiraci("bos");
        var c = f.Ortam.Istemci(k);
        var ok = await c.PostAsync("/api/v1/kur/manuel", Ham("""{"altinHasFiyati":6900,"usdTry":null,"eurTry":null}"""));
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        var red = await c.PostAsync("/api/v1/kur/manuel", Ham("""{"altinHasFiyati":null}"""));
        Assert.Equal(HttpStatusCode.BadRequest, red.StatusCode);
    }

    /// <summary>
    /// KUR-BANT tur 2 (bulgu kapandı): onay yolu MUTLAK sınırı AŞAMAZ — SAHİP gerekçe yazsa da
    /// has 5 TL 400 alanlı reddedilir, kayıt doğmaz. (Tur 1'de bu test 201'i belgeliyordu.)
    /// </summary>
    [Fact]
    public async Task H06_Onayli_asim_mutlak_siniri_gecemez_400()
    {
        var k = YeniKiraci("mutlak onay");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var once = Sayi(k);
        var m = await RedAlan(await c.PostAsync("/api/v1/kur/manuel",
            Ham("""{"altinHasFiyati":5,"bantOnay":true,"bantGerekce":"Bu fiyat doğru, teyit ettim gerçekten."}""")), "altinHasFiyati");
        Assert.Contains("500", m);
        Assert.Equal(once, Sayi(k));
    }
}

/// <summary>
/// HASIM DOĞRULAYICI tur 3 (09.10.2026, Aşkın + Mehmet) — ek sınırlar: döviz mutlak sınır 4 hane,
/// gram altın kendi referansıyla, karma (sert + yumuşak) onaylı istek, boşluk gerekçe, yarım kuruş
/// (5. hane değil, 4. hane 0,005) birebir saklama, Türkçe binlik + ondalık çözümü. Beklenenler SABİT.
/// </summary>
[Collection("http")]
public sealed class KURBANTSinirDogrulayiciTur3Testleri(HttpFikstur f)
{
    private const string Gerekce = "Piyasa sert oynadı, fiyatı odadan teyit ettim.";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETKUR-BANT-TEST dg3 " + ad);   // has 6.827,20 · gram 6.861,51 · USD yok
    }

    private int Sayi(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.KurSnapshots.AsNoTracking().Count(s => s.TenantId == k.TenantId);
    }

    private int IzSayisi(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.DenetimKayitlari.AsNoTracking().Count(d => d.TenantId == k.TenantId && d.VarlikTuru == "KurBantAsimi");
    }

    private static StringContent Ham(string json) => new(json, Encoding.UTF8, "application/json");

    private static async Task<string> RedAlan(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), govde);
        var p = alanlar.EnumerateObject().FirstOrDefault(x => x.Name.Equals(alan, StringComparison.OrdinalIgnoreCase));
        Assert.True(p.Value.ValueKind == JsonValueKind.Array, $"'{alan}' işaretli değil: {govde}");
        return p.Value[0].GetString() ?? "";
    }

    [Fact]
    public async Task D01_Doviz_mutlak_sinir_4_hane_alt_ve_ust()
    {
        var k = YeniKiraci("doviz");
        var c = f.Ortam.Istemci(k);
        var once = Sayi(k);
        Assert.Equal("Bu fiyat olağan aralığın dışında (5,00–500,00 TL). Rakamı yanlış yazmış olabilirsiniz; kontrol edin.",
            await RedAlan(await c.PostAsync("/api/v1/kur/manuel", Ham("""{"altinHasFiyati":6827.2,"usdTry":4.9999}""")), "usdTry"));
        await RedAlan(await c.PostAsync("/api/v1/kur/manuel", Ham("""{"altinHasFiyati":6827.2,"eurTry":500.0001}""")), "eurTry");
        Assert.Equal(once, Sayi(k));
        var ok = await c.PostAsync("/api/v1/kur/manuel", Ham("""{"altinHasFiyati":6827.2,"usdTry":5.0000,"eurTry":500.0000}"""));
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
    }

    [Fact]
    public async Task D02_Gram_altin_kendi_referansiyla_bantlanir()
    {
        var k = YeniKiraci("gram");
        var c = f.Ortam.Istemci(k);
        // 6.861,51 × 1,2 = 8.233,812 → 8.233,8121 red, 8.233,8120 kabul (has bant içi 6.900)
        var m = await RedAlan(await c.PostAsync("/api/v1/kur/manuel",
            Ham("""{"altinHasFiyati":6900,"gramAltinFiyati":8233.8121}""")), "gramAltinFiyati");
        Assert.Contains("6.861,51", m);
        var ok = await c.PostAsync("/api/v1/kur/manuel", Ham("""{"altinHasFiyati":6900,"gramAltinFiyati":8233.8120}"""));
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
    }

    [Fact]
    public async Task D03_Karma_yumusak_onayli_ama_sert_alan_varsa_400_kayit_ve_iz_yok()
    {
        var k = YeniKiraci("karma");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var once = Sayi(k);
        var govde = $$"""{"altinHasFiyati":9000,"usdTry":4,"bantOnay":true,"bantGerekce":"{{Gerekce}}"}""";
        await RedAlan(await c.PostAsync("/api/v1/kur/manuel", Ham(govde)), "usdTry");
        Assert.Equal(once, Sayi(k));
        Assert.Equal(0, IzSayisi(k));
    }

    [Fact]
    public async Task D04_Bosluk_gerekce_yetmez_400_bantGerekce()
    {
        var k = YeniKiraci("bosluk");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var once = Sayi(k);
        await RedAlan(await c.PostAsync("/api/v1/kur/manuel",
            Ham("""{"altinHasFiyati":9000,"bantOnay":true,"bantGerekce":"                         "}""")), "bantGerekce");
        // onay yok + gerekçe dolu → yine 400 (gerekçe tek başına onay sayılmaz)
        await RedAlan(await c.PostAsync("/api/v1/kur/manuel",
            Ham($$"""{"altinHasFiyati":9000,"bantGerekce":"{{Gerekce}}"}""")), "altinHasFiyati");
        Assert.Equal(once, Sayi(k));
        Assert.Equal(0, IzSayisi(k));
    }

    [Fact]
    public async Task D05_Yarim_kurus_4_hane_birebir_saklanir_yuvarlanmaz()
    {
        var k = YeniKiraci("yarim");
        var c = f.Ortam.Istemci(k);
        var r = await c.PostAsync("/api/v1/kur/manuel", Ham("""{"altinHasFiyati":6900.0050,"usdTry":41.0049}"""));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        using var db = f.Vt.OlusturSistem();
        var s = db.KurSnapshots.AsNoTracking().Where(x => x.TenantId == k.TenantId)
            .OrderByDescending(x => x.Olusturuldu).First();
        Assert.Equal(6_900.0050m, s.AltinHasFiyati);
        Assert.Equal(41.0049m, s.UsdTry);
    }

    [Fact]
    public async Task D06_Cok_buyuk_doviz_ve_gram_400()
    {
        var k = YeniKiraci("buyuk");
        var c = f.Ortam.Istemci(k);
        var once = Sayi(k);
        await RedAlan(await c.PostAsync("/api/v1/kur/manuel", Ham("""{"altinHasFiyati":6900,"usdTry":999999999.99}""")), "usdTry");
        await RedAlan(await c.PostAsync("/api/v1/kur/manuel", Ham("""{"altinHasFiyati":6900,"gramAltinFiyati":99999999}""")), "gramAltinFiyati");
        Assert.Equal(once, Sayi(k));
    }

    [Theory]
    [InlineData("6.827,20", "6827.20")]
    [InlineData("8.192,6401", "8192.6401")]
    public void D07_Turkce_binlik_ve_ondalik_cozumu_sonra_bant(string ham, string beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }
}
