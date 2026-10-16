using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Kuyumcu.Api.Modules.Kasa;
using Kuyumcu.Api.Modules.Satis;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ═══════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET PARA-DEMO-2 finansal sınır testleri.
// Kapsam: nakit çıkış kapısı (KasaBakiyeKapisi.NakitDogrula), nakit/kart ayrımı,
// tek bacak ödeme etiketi (OdemeSepeti.FisEtiketi), hata metni para biçimi, kâğıt ekstre has hücresi.
// Beklenen değerler SABİT ve elle hesaplandı (üretim kodundan türetilmez).
// Yuvarlama kuralı: TL 2 hane, has 4 hane (ekran), MidpointRounding.AwayFromZero.
// ═══════════════════════════════════════════════════════════

[Collection("http")]
public sealed class PARADEMO2SinirTestleriTur6(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"PAKETPARA-DEMO-2-TEST dg {ad}");
    }

    private static async Task<string> KasaAc(HttpClient c, Kiraci k)
    {
        var r = await c.PostJson("/api/v1/kasalar", new { ad = "DG kasa", subeId = k.SubeId, paraBirimi = "TRY" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Json()).GetProperty("id").GetString()!;
    }

    private static Task<HttpResponseMessage> Hareket(HttpClient c, string kasa, string yon, decimal tutar, string? arac) =>
        arac is null
            ? c.PostAsJsonAsync($"/api/v1/kasalar/{kasa}/hareket", new { yon, tutar, aciklama = "dg" })
            : c.PostAsJsonAsync($"/api/v1/kasalar/{kasa}/hareket", new { yon, tutar, odemeTuru = arac, aciklama = "dg" });

    /// <summary>Nakit 1000 + kart 500 = toplam 1500.</summary>
    private static async Task<string> Kasa1000_500(HttpClient c, Kiraci k)
    {
        var kasa = await KasaAc(c, k);
        var g1 = await Hareket(c, kasa, "GIRIS", 1_000m, "NAKIT");
        Assert.True(g1.IsSuccessStatusCode, await g1.Content.ReadAsStringAsync());
        var g2 = await Hareket(c, kasa, "GIRIS", 500m, "KART");
        Assert.True(g2.IsSuccessStatusCode, await g2.Content.ReadAsStringAsync());
        return kasa;
    }

    private async Task<decimal> Bakiye(string kasa)
    {
        using var db = f.Vt.OlusturSistem();
        return (await db.Kasalar.AsNoTracking().SingleAsync(x => x.Id == kasa)).Bakiye;
    }

    private async Task<int> HareketSayisi(string kasa)
    {
        using var db = f.Vt.OlusturSistem();
        return await db.KasaHareketleri.AsNoTracking().CountAsync(x => x.KasaId == kasa);
    }

    // ── sıfır ──
    [Fact]
    public async Task DG_sifir_tutarli_nakit_cikis_400_hicbir_sey_yazilmaz()
    {
        var k = YeniKiraci("sifir");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Kasa1000_500(c, k);
        var r = await Hareket(c, kasa, "CIKIS", 0m, "NAKIT");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(1_500m, await Bakiye(kasa));
        Assert.Equal(2, await HareketSayisi(kasa));
    }

    // ── eksi ──
    [Fact]
    public async Task DG_eksi_tutarli_cikis_400_giris_gibi_islemez()
    {
        var k = YeniKiraci("eksi");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Kasa1000_500(c, k);
        var r = await Hareket(c, kasa, "CIKIS", -100m, "NAKIT");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(1_500m, await Bakiye(kasa));
    }

    // ── kuruş: PAKET GOC-A (K9) — kuruş altı hane YUVARLANMAZ, 400 (bakiye değişmez); nakit payı sınırı tam kuruşla ──
    [Fact]
    public async Task DG_kurus_1000_005_ve_1000_004_yuvarlanmaz_400_1000_01_payi_asar_1000_00_gecer()
    {
        var k = YeniKiraci("kurus");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Kasa1000_500(c, k);
        foreach (var kurusAlti in new[] { 1_000.005m, 1_000.004m })
        {
            var r = await Hareket(c, kasa, "CIKIS", kurusAlti, "NAKIT");
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.Contains("Virgülden sonra en çok 2 hane", await r.Content.ReadAsStringAsync());
            Assert.Equal(1_500m, await Bakiye(kasa));
        }
        var fazla = await Hareket(c, kasa, "CIKIS", 1_000.01m, "NAKIT");
        Assert.Equal(HttpStatusCode.BadRequest, fazla.StatusCode);
        Assert.Equal(1_500m, await Bakiye(kasa));
        var tam = await Hareket(c, kasa, "CIKIS", 1_000.00m, "NAKIT");
        Assert.True(tam.IsSuccessStatusCode, await tam.Content.ReadAsStringAsync());
        Assert.Equal(500.00m, await Bakiye(kasa));
        // Kalan 500 = kart payı; nakit 0 → 0,01 nakit çıkışı bile 400.
        var kurus = await Hareket(c, kasa, "CIKIS", 0.01m, "NAKIT");
        Assert.Equal(HttpStatusCode.BadRequest, kurus.StatusCode);
    }

    [Fact]
    public void DG_kurus_hata_metni_para_bicimi_0_005_yukari_eksi_U2212()
    {
        var m = typeof(KasaBakiyeKapisi).GetMethod("ParaMetni", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.Equal("0,01 ₺", m.Invoke(null, [0.005m, "TRY"]));
        Assert.Equal("0,00 ₺", m.Invoke(null, [0.004m, "TRY"]));
        Assert.Equal("−0,01 ₺", m.Invoke(null, [-0.005m, "TRY"]));
        Assert.Equal("1.234,57 $", m.Invoke(null, [1234.565m, "USD"]));
    }

    // ── has/gram 4 hane (kâğıt ekstre has hücresi) ──
    [Fact]
    public void DG_has_4_hane_0_0001_sinir_AwayFromZero_eksi_U2212()
    {
        var tip = typeof(Kuyumcu.Api.Modules.Cikti.CiktiServisi);
        var m = tip.GetMethod("EkstreHas", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.Equal("0,0001\u00a0has", m.Invoke(null, [0.0001m]));
        Assert.Equal("0,0001\u00a0has", m.Invoke(null, [0.00005m]));
        Assert.Equal("0,0000\u00a0has", m.Invoke(null, [0.00004m]));
        Assert.Equal("−1,2346\u00a0has", m.Invoke(null, [-1.23456m]));
    }

    // ── çok büyük tutar ──
    [Fact]
    public async Task DG_buyuk_999_999_999_99_nakit_cikis_400_mesaj_tr_TR()
    {
        var k = YeniKiraci("buyuk");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Kasa1000_500(c, k);
        var r = await Hareket(c, kasa, "CIKIS", 999_999_999.99m, "NAKIT");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var mesaj = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("message").GetString()!;
        Assert.Contains("bakiyesi yetersiz", mesaj);   // toplam kapısı önce (1500 < 999.999.999,99)
        Assert.Equal(1_500m, await Bakiye(kasa));
    }

    [Fact]
    public async Task DG_buyuk_kart_girisi_sonrasi_ayrim_kurusu_kurusuna_ve_nakit_kapisi_kart_payini_saymaz()
    {
        var k = YeniKiraci("buyuk-ayrim");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Kasa1000_500(c, k);
        var g = await Hareket(c, kasa, "GIRIS", 999_999_999.99m, "KART");
        Assert.True(g.IsSuccessStatusCode, await g.Content.ReadAsStringAsync());
        var ay = (await (await c.GetAsync("/api/v1/kasalar/nakit-ayrimi")).Json()).EnumerateArray()
            .Single(x => x.GetProperty("kasaId").GetString() == kasa);
        Assert.Equal(1_000m, ay.GetProperty("nakit").GetDecimal());
        Assert.Equal(1_000_000_499.99m, ay.GetProperty("kart").GetDecimal());
        Assert.Equal(0m, ay.GetProperty("havale").GetDecimal());
        // Toplam 1.000.001.499,99 yetse de nakit 1000 → 1000,01 nakit çıkışı 400.
        var r = await Hareket(c, kasa, "CIKIS", 1_000.01m, "NAKIT");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("nakit yetersiz", await r.Content.ReadAsStringAsync());
    }

    // ── boş / null ──
    [Fact]
    public async Task DG_bos_odeme_turu_400_DIGER_cikis_nakit_sayilir_fail_closed()
    {
        var k = YeniKiraci("bos");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Kasa1000_500(c, k);
        var bos = await Hareket(c, kasa, "CIKIS", 100m, null);
        Assert.Equal(HttpStatusCode.BadRequest, bos.StatusCode);
        // DIGER aracı kart/havale DEĞİL → nakit payına tabi: 1200 > nakit 1000 → 400.
        var diger = await Hareket(c, kasa, "CIKIS", 1_200m, "DIGER");
        Assert.Equal(HttpStatusCode.BadRequest, diger.StatusCode);
        Assert.Equal(1_500m, await Bakiye(kasa));
    }

    [Fact]
    public void DG_fis_etiketi_tek_arac_kendisi_iki_arac_KARMA_ayni_arac_tekrar_KARMA_degil()
    {
        static OdemeBacagiDto B(string a) => new() { Arac = a, TutarTl = 1m };
        Assert.Equal("HAVALE", OdemeSepeti.FisEtiketi([B("HAVALE")]));
        Assert.Equal("VERESIYE", OdemeSepeti.FisEtiketi([B("VERESIYE")]));
        Assert.Equal("KART", OdemeSepeti.FisEtiketi([B("KART"), B("KART")]));
        Assert.Equal("KARMA", OdemeSepeti.FisEtiketi([B("NAKIT"), B("KART")]));
    }

    // ── Türkçe giriş (FormAlani.SayiCoz) ──
    [Fact]
    public void DG_turkce_giris_45_000_1_5_ve_belirsiz_1_500_reddi()
    {
        Assert.Equal(45_000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        var b = FormAlani.SayiCoz("1.500");
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, b.Sonuc);
        Assert.Null(b.Deger);
        Assert.Equal(999_999_999.99m, FormAlani.SayiCoz("999.999.999,99").Deger);
    }
}
