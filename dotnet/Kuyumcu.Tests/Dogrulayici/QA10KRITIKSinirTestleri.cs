using System.Globalization;
using System.Net;
using System.Text.Json;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET QA10-KRITIK — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİTTİR (üretim kodundan
// hesaplanmaz). QA turu 10'un /gider KRİTİK'i (storno sonrası toplam mutlak
// değerle 251,00) için GET /giderler → toplamlar[] işaretli net toplam sınırları.
// Projedeki gider yuvarlama kuralı: MidpointRounding.AwayFromZero
// (GiderModulu.Kaydet). 0,025 ToEven'da 0,02, AwayFromZero'da 0,03 verir → ayırt edici.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class QA10KRITIKSinirTestleri(HttpFikstur f)
{
    private const string Gerekce = "Dogrulayici QA10 sinir testi: gider geri aliniyor.";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private async Task<string> KasaAc(Kiraci k, decimal acilis)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson("/api/v1/kasalar", new { ad = "Nakit", subeId = k.SubeId, paraBirimi = "TRY" });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var id = (await c.Json()).GetProperty("id").GetString()!;
        using var db = f.Vt.OlusturSistem();
        var kasa = await db.Kasalar.SingleAsync(x => x.Id == id);
        kasa.Bakiye = acilis;
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<string> KategoriAc(Kiraci k, string ad = "QA10")
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/gider-kategorileri", new { ad });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        return (await c.Json()).GetProperty("id").GetString()!;
    }

    private Task<HttpResponseMessage> GiderPost(Kiraci k, object govde) =>
        f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/giderler", govde);

    private async Task<string> Gider(Kiraci k, string kasa, string kat, decimal tutar)
    {
        var c = await GiderPost(k, new { kasaId = kasa, giderKategoriId = kat, tutar });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        return (await c.Json()).GetProperty("id").GetString()!;
    }

    private async Task<HttpResponseMessage> Storno(Kiraci k, string kasa, string satir) =>
        await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/kasalar/{kasa}/hareket/{satir}/storno", new { gerekce = Gerekce });

    private async Task<JsonElement> Liste(Kiraci k, string sorgu = "")
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/giderler" + sorgu);
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        return await c.Json();
    }

    private static decimal TryToplam(JsonElement liste) =>
        liste.GetProperty("toplamlar").EnumerateArray()
            .Single(t => t.GetProperty("paraBirimi").GetString() == "TRY")
            .GetProperty("tutar").GetDecimal();

    private async Task<decimal> Bakiye(string kasa)
    {
        using var db = f.Vt.OlusturSistem();
        return (await db.Kasalar.AsNoTracking().SingleAsync(x => x.Id == kasa)).Bakiye;
    }

    // ── QA senaryosu birebir: 125,50 + storno → 0,00 (251,00 DEĞİL), bakiye döner ──
    [Fact]
    public async Task DQA10_qa_senaryosu_125_50_storno_toplam_0_bakiye_5508_51()
    {
        var k = YeniKiraci("PAKETQA10-KRITIK-TEST senaryo");
        var kasa = await KasaAc(k, 5_508.51m);
        var kat = await KategoriAc(k);
        var a = await Gider(k, kasa, kat, 125.50m);
        Assert.Equal(5_383.01m, await Bakiye(kasa));
        Assert.Equal(125.50m, TryToplam(await Liste(k)));

        Assert.Equal(HttpStatusCode.OK, (await Storno(k, kasa, a)).StatusCode);
        var l = await Liste(k);
        Assert.Equal(0.00m, TryToplam(l));
        Assert.NotEqual(251.00m, TryToplam(l));
        Assert.Equal(2, l.GetProperty("satirlar").GetArrayLength());
        Assert.Equal(5_508.51m, await Bakiye(kasa));
        Assert.DoesNotContain("251", Bicim.Para(TryToplam(l), "TRY"));
        // Ekran biçimi (Bicim.Tl) sıfır kuruşu atar: "0 ₺" — eksi işaret yok, 251 yok.
        Assert.Equal("0 ₺", Bicim.Para(TryToplam(l), "TRY").Replace('\u00A0', ' ').Replace('\u202F', ' '));
    }

    // ── sıfır: 0 tutar reddedilir, toplam dizisi boş kalır, bakiye değişmez ──
    [Fact]
    public async Task DQA10_sifir_tutar_reddedilir_bakiye_degismez()
    {
        var k = YeniKiraci("PAKETQA10-KRITIK-TEST sifir");
        var kasa = await KasaAc(k, 100m);
        var kat = await KategoriAc(k);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await GiderPost(k, new { kasaId = kasa, giderKategoriId = kat, tutar = 0m })).StatusCode);
        Assert.Empty((await Liste(k)).GetProperty("toplamlar").EnumerateArray());
        Assert.Equal(100m, await Bakiye(kasa));
    }

    // ── eksi: eksi gider reddedilir; işaret: storno satırı listede EKSİ, toplam azalır ──
    [Fact]
    public async Task DQA10_eksi_gider_reddedilir_storno_satiri_eksi_isaretli()
    {
        var k = YeniKiraci("PAKETQA10-KRITIK-TEST eksi");
        var kasa = await KasaAc(k, 1_000m);
        var kat = await KategoriAc(k);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await GiderPost(k, new { kasaId = kasa, giderKategoriId = kat, tutar = -0.01m })).StatusCode);

        await Gider(k, kasa, kat, 100.00m);
        var b = await Gider(k, kasa, kat, 0.01m);
        await Gider(k, kasa, kat, 50.00m);
        Assert.Equal(150.01m, TryToplam(await Liste(k)));
        Assert.Equal(HttpStatusCode.OK, (await Storno(k, kasa, b)).StatusCode);

        var l = await Liste(k);
        Assert.Equal(150.00m, TryToplam(l));
        var tutarlar = l.GetProperty("satirlar").EnumerateArray()
            .Select(s => s.GetProperty("tutar").GetDecimal()).OrderBy(x => x).ToList();
        Assert.Equal([-0.01m, 0.01m, 50.00m, 100.00m], tutarlar);
        Assert.Equal(850.00m, await Bakiye(kasa));
    }

    // ── kuruş: PAKET GOC-A (K9) — 0,025 / 0,014 YUVARLANMAZ (eski: 0,03 / 0,01) — 400; tam kuruşla storno net 0 ──
    [Fact]
    public async Task DQA10_kurus_alti_400_tam_kurus_0_03_storno_net_0()
    {
        var k = YeniKiraci("PAKETQA10-KRITIK-TEST kurus");
        var kasa = await KasaAc(k, 10m);
        var kat = await KategoriAc(k);
        foreach (var kurusAlti in new[] { 0.025m, 0.014m })
        {
            var r = await GiderPost(k, new { kasaId = kasa, giderKategoriId = kat, tutar = kurusAlti });
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.Contains("Virgülden sonra en çok 2 hane", await r.Content.ReadAsStringAsync());
        }
        Assert.Equal(10m, await Bakiye(kasa));
        var a = await Gider(k, kasa, kat, 0.03m);
        await Gider(k, kasa, kat, 0.01m);
        Assert.Equal(0.04m, TryToplam(await Liste(k)));
        Assert.Equal(9.96m, await Bakiye(kasa));
        Assert.Equal(HttpStatusCode.OK, (await Storno(k, kasa, a)).StatusCode);
        Assert.Equal(0.01m, TryToplam(await Liste(k)));
        Assert.Equal(9.99m, await Bakiye(kasa));
    }

    // ── çok büyük tutar: 999.999.999,99 + storno → tam 0,00 (taşma/kayma yok) ──
    [Fact]
    public async Task DQA10_cok_buyuk_999_999_999_99_storno_net_0()
    {
        var k = YeniKiraci("PAKETQA10-KRITIK-TEST buyuk");
        var kasa = await KasaAc(k, 999_999_999.99m);
        var kat = await KategoriAc(k);
        var a = await Gider(k, kasa, kat, 999_999_999.99m);
        Assert.Equal(999_999_999.99m, TryToplam(await Liste(k)));
        Assert.Equal(0m, await Bakiye(kasa));
        Assert.Equal(HttpStatusCode.OK, (await Storno(k, kasa, a)).StatusCode);
        Assert.Equal(0.00m, TryToplam(await Liste(k)));
        Assert.Equal(999_999_999.99m, await Bakiye(kasa));
        Assert.Equal("999.999.999,99", Bicim.Para(999_999_999.99m, "TRY").Replace("₺", "").Trim());
    }

    // ── boş/null: süzgeç eşleşmesiz → toplamlar BOŞ dizi (uydurma 0 yok); null tutar 400 ──
    [Fact]
    public async Task DQA10_bos_suzgec_toplamlar_bos_null_tutar_400()
    {
        var k = YeniKiraci("PAKETQA10-KRITIK-TEST bos");
        var kasa = await KasaAc(k, 100m);
        var kat = await KategoriAc(k, "Dolu");
        var bos = await KategoriAc(k, "Bos");
        await Gider(k, kasa, kat, 10m);
        var l = await Liste(k, $"?kategoriId={bos}");
        Assert.Equal(JsonValueKind.Array, l.GetProperty("toplamlar").ValueKind);
        Assert.Empty(l.GetProperty("toplamlar").EnumerateArray());
        Assert.Equal(HttpStatusCode.BadRequest,
            (await GiderPost(k, new { kasaId = kasa, giderKategoriId = kat, tutar = (decimal?)null })).StatusCode);
        Assert.Equal(10m, TryToplam(await Liste(k)));
    }

    // ── has/gram 4 hane (0,0001) — hurda onay özeti/girdi yolu: çözülür ve biçim kaybetmez ──
    [Fact]
    public void DQA10_has_gram_4_hane_0_0001_cozulur_ve_bicimlenir()
    {
        var s = FormAlani.SayiCoz("0,0001");
        Assert.Equal(FormAlani.SayiSonucu.Tamam, s.Sonuc);
        Assert.Equal(0.0001m, s.Deger);
        Assert.Equal("0,0001 has", Bicim.Has(0.0001m));
        Assert.Equal("0,0001", Bicim.HasGirdi(0.0001m));
    }

    // ── Türkçe giriş: '45.000' = 45000, '1,5' = 1.5, '125,50' = 125.5, '1.500' BELİRSİZ ──
    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("125,50", "125.50")]
    [InlineData("1.500,00", "1500.00")]
    public void DQA10_turkce_giris_cozulur(string ham, string beklenen)
    {
        var s = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, s.Sonuc);
        Assert.Equal(decimal.Parse(beklenen, CultureInfo.InvariantCulture), s.Deger);
    }

    [Fact]
    public void DQA10_belirsiz_1_500_reddedilir()
    {
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
    }
}
