using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q10-1 — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİTTİR (üretim kodundan
// hesaplanmaz). Kapsam: GET /giderler → toplamlar[] (işaretli net toplam) ve
// gider kaydı tutar kapısı (sıfır/eksi/yuvarlama/çok büyük/boş), ekran biçimi
// (Bicim.Para) ve Türkçe sayı girişi (FormAlani.SayiCoz).
// Projedeki para yuvarlama kuralı: MidpointRounding.AwayFromZero (GiderModulu.Kaydet).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class Q101SinirTestleri(HttpFikstur f)
{
    private const string Gerekce = "Dogrulayici sinir testi: gider geri aliniyor.";

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

    private async Task<string> KategoriAc(Kiraci k)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/gider-kategorileri", new { ad = "Sinir" });
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

    private async Task Storno(Kiraci k, string kasa, string satir)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/kasalar/{kasa}/hareket/{satir}/storno", new { gerekce = Gerekce });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
    }

    private async Task<JsonElement> ToplamlarDizisi(Kiraci k)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/giderler");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        return (await c.Json()).GetProperty("toplamlar");
    }

    private static decimal TryToplam(JsonElement dizi) =>
        dizi.EnumerateArray().Single(t => t.GetProperty("paraBirimi").GetString() == "TRY")
            .GetProperty("tutar").GetDecimal();

    // ── sıfır ──
    [Fact]
    public async Task DQ101_sifir_tutarli_gider_reddedilir_toplam_bos_kalir()
    {
        var k = YeniKiraci("PAKETQ10-1-TEST sinir sifir");
        var kasa = await KasaAc(k, 1_000m);
        var kat = await KategoriAc(k);
        var c = await GiderPost(k, new { kasaId = kasa, giderKategoriId = kat, tutar = 0m });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Empty((await ToplamlarDizisi(k)).EnumerateArray());
    }

    // ── eksi ──
    [Fact]
    public async Task DQ101_eksi_tutarli_gider_reddedilir()
    {
        var k = YeniKiraci("PAKETQ10-1-TEST sinir eksi");
        var kasa = await KasaAc(k, 1_000m);
        var kat = await KategoriAc(k);
        var c = await GiderPost(k, new { kasaId = kasa, giderKategoriId = kat, tutar = -125.50m });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Empty((await ToplamlarDizisi(k)).EnumerateArray());
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(1_000m, (await db.Kasalar.AsNoTracking().SingleAsync(x => x.Id == kasa)).Bakiye);
    }

    // ── kuruş yuvarlaması (0,005 sınırı, AwayFromZero) ──
    [Fact]
    public async Task DQ101_kurus_alti_yuvarlanmaz_400_ve_storno_net_sifir()
    {
        var k = YeniKiraci("PAKETQ10-1-TEST sinir yuvarlama");
        var kasa = await KasaAc(k, 1_000m);
        var kat = await KategoriAc(k);

        // Yuvarlanınca 0,00'a düşen tutar reddedilir (sıfır tutarlı gider doğmaz).
        // SON-KALANLAR tur 2: alt sınır TEK kapıda (KolonTavani.PozitifTutar: önce yuvarla, sonra en az 0,01);
        // DTO [Range(0.01)] kaldırıldığı için 0,005 artık 0,01 olarak kabul edilir (HTTP = servis yolu).
        // Sınırın hemen altı 0,0049 → 0,00 → 400.
        var c = await GiderPost(k, new { kasaId = kasa, giderKategoriId = kat, tutar = 0.0049m });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);

        // PAKET GOC-A (K9): kuruş altı hane artık YUVARLANMAZ (eski: 10,005 → 10,01 · 20,004 → 20,00) — 400.
        Assert.Equal(HttpStatusCode.BadRequest, (await GiderPost(k, new { kasaId = kasa, giderKategoriId = kat, tutar = 10.005m })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await GiderPost(k, new { kasaId = kasa, giderKategoriId = kat, tutar = 20.004m })).StatusCode);
        var a = await Gider(k, kasa, kat, 10.01m);
        await Gider(k, kasa, kat, 20.00m);
        Assert.Equal(30.01m, TryToplam(await ToplamlarDizisi(k)));

        await Storno(k, kasa, a);
        Assert.Equal(20.00m, TryToplam(await ToplamlarDizisi(k)));
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(980.00m, (await db.Kasalar.AsNoTracking().SingleAsync(x => x.Id == kasa)).Bakiye);
    }

    // ── çok büyük tutar ──
    [Fact]
    public async Task DQ101_cok_buyuk_tutar_999_999_999_99_ve_stornosu()
    {
        var k = YeniKiraci("PAKETQ10-1-TEST sinir buyuk");
        var kasa = await KasaAc(k, 2_000_000_000m);
        var kat = await KategoriAc(k);
        var a = await Gider(k, kasa, kat, 999_999_999.99m);
        await Gider(k, kasa, kat, 999_999_999.99m);
        Assert.Equal(1_999_999_999.98m, TryToplam(await ToplamlarDizisi(k)));
        await Storno(k, kasa, a);
        Assert.Equal(999_999_999.99m, TryToplam(await ToplamlarDizisi(k)));
        Assert.Equal("999.999.999,99₺", Bicim.Para(999_999_999.99m, "TRY").Replace(" ", "").Replace(" ", ""));
    }

    // ── boş / null ──
    [Fact]
    public async Task DQ101_tutarsiz_govde_reddedilir_ve_storno_sonrasi_toplam_sifir()
    {
        var k = YeniKiraci("PAKETQ10-1-TEST sinir bos");
        var kasa = await KasaAc(k, 500m);
        var kat = await KategoriAc(k);
        var c = await GiderPost(k, new { kasaId = kasa, giderKategoriId = kat });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        var c2 = await GiderPost(k, new { kasaId = kasa, giderKategoriId = kat, tutar = (decimal?)null });
        Assert.Equal(HttpStatusCode.BadRequest, c2.StatusCode);

        var a = await Gider(k, kasa, kat, 125.50m);
        await Storno(k, kasa, a);
        var dizi = await ToplamlarDizisi(k);
        Assert.Equal(0.00m, TryToplam(dizi));
        // eksi sıfır ekrana "-₺0,00" diye düşmemeli
        Assert.DoesNotContain("-", Bicim.Para(TryToplam(dizi), "TRY"));
        Assert.DoesNotContain("-", Bicim.Para(-0.00m, "TRY"));
    }

    // ── kiracı yalıtımı: başka kiracının gideri toplama girmez ──
    [Fact]
    public async Task DQ101_baska_kiracinin_gideri_toplama_girmez()
    {
        var a = YeniKiraci("PAKETQ10-1-TEST sinir A");
        var b = YeniKiraci("PAKETQ10-1-TEST sinir B");
        var kasaA = await KasaAc(a, 1_000m);
        var katA = await KategoriAc(a);
        var kasaB = await KasaAc(b, 1_000m);
        var katB = await KategoriAc(b);
        await Gider(a, kasaA, katA, 11.11m);
        var sb = await Gider(b, kasaB, katB, 99.99m);
        await Storno(b, kasaB, sb);
        Assert.Equal(11.11m, TryToplam(await ToplamlarDizisi(a)));
        Assert.Equal(0.00m, TryToplam(await ToplamlarDizisi(b)));
    }

    // ── Türkçe sayı girişi (FormAlani.SayiCoz) ──
    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("999.999.999,99", "999999999.99")]
    [InlineData("0,005", "0.005")]
    public void DQ101_turkce_sayi_girisi_cozulur(string ham, string beklenen)
    {
        var s = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, s.Sonuc);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), s.Deger);
    }

    [Fact]
    public void DQ101_belirsiz_1_500_reddedilir_bos_bos_doner()
    {
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
    }

    // ═══ DOĞRULAYICI TUR 2 (2026-09-24 19:50) — ek sınır durumları ═══════════

    private async Task<JsonElement> Liste(Kiraci k, string sorgu)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/giderler" + sorgu);
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        return await c.Json();
    }

    // ── kısmi: 3 gider + 1 storno, satır sınırı 1 → toplam sınırdan bağımsız ──
    [Fact]
    public async Task DQ101b_uc_gider_bir_storno_limit_1_iken_toplam_125_25()
    {
        var k = YeniKiraci("PAKETQ10-1-TEST sinir kismi");
        var kasa = await KasaAc(k, 1_000m);
        var kat = await KategoriAc(k);
        await Gider(k, kasa, kat, 100.00m);
        var b = await Gider(k, kasa, kat, 50.00m);
        await Gider(k, kasa, kat, 25.25m);
        await Storno(k, kasa, b);

        var l = await Liste(k, "?limit=1");
        Assert.Single(l.GetProperty("satirlar").EnumerateArray());
        Assert.True(l.GetProperty("dahaFazlasiVar").GetBoolean());
        var t = l.GetProperty("toplamlar").EnumerateArray().Single();
        Assert.Equal("TRY", t.GetProperty("paraBirimi").GetString());
        Assert.Equal(125.25m, t.GetProperty("tutar").GetDecimal());
        Assert.Equal(4, t.GetProperty("adet").GetInt32());
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(874.75m, (await db.Kasalar.AsNoTracking().SingleAsync(x => x.Id == kasa)).Bakiye);
    }

    // ── en küçük geçerli tutar 0,01 ve stornosu → 0,00 (eksi sıfır yok) ──
    [Fact]
    public async Task DQ101b_en_kucuk_tutar_0_01_ve_stornosu_sifir()
    {
        var k = YeniKiraci("PAKETQ10-1-TEST sinir kurus");
        var kasa = await KasaAc(k, 1m);
        var kat = await KategoriAc(k);
        var a = await Gider(k, kasa, kat, 0.01m);
        Assert.Equal(0.01m, TryToplam(await ToplamlarDizisi(k)));
        await Storno(k, kasa, a);
        var dizi = await ToplamlarDizisi(k);
        Assert.Equal(0.00m, TryToplam(dizi));
        Assert.Equal(2, dizi.EnumerateArray().Single().GetProperty("adet").GetInt32());
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(1.00m, (await db.Kasalar.AsNoTracking().SingleAsync(x => x.Id == kasa)).Bakiye);
    }

    // ── dönem süzgeci: gider ile stornosu farklı aralıklara düşerse her aralık
    //    KENDİ işaretli netini verir (gider aralığı +50, storno aralığı −50),
    //    süzgeçsiz toplam 0. Mutlak toplam olsaydı storno aralığı +50 çıkardı. ──
    [Fact]
    public async Task DQ101b_gider_ve_stornosu_farkli_aralikta_isaretli_net()
    {
        var k = YeniKiraci("PAKETQ10-1-TEST sinir donem");
        var kasa = await KasaAc(k, 1_000m);
        var kat = await KategoriAc(k);
        var a = await Gider(k, kasa, kat, 50.00m);
        await Task.Delay(1_200);
        await Storno(k, kasa, a);

        DateTime tGider, tStorno;
        using (var db = f.Vt.OlusturSistem())
        {
            tGider = (await db.KasaHareketleri.AsNoTracking().SingleAsync(h => h.Id == a)).Tarih;
            tStorno = (await db.KasaHareketleri.AsNoTracking().SingleAsync(h => h.StornoKaynakId == a)).Tarih;
        }
        Assert.True(tStorno > tGider);
        var orta = DateTime.SpecifyKind(tGider + (tStorno - tGider) / 2, DateTimeKind.Utc)
            .ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", System.Globalization.CultureInfo.InvariantCulture);

        static decimal Try(JsonElement l) =>
            l.GetProperty("toplamlar").EnumerateArray().Single().GetProperty("tutar").GetDecimal();
        Assert.Equal(50.00m, Try(await Liste(k, "?bitis=" + Uri.EscapeDataString(orta))));
        Assert.Equal(-50.00m, Try(await Liste(k, "?baslangic=" + Uri.EscapeDataString(orta))));
        Assert.Equal(0.00m, Try(await Liste(k, "")));
    }

    // ═══ DOĞRULAYICI TUR 3 (2026-09-24 20:15) — çift storno, storno'nun stornosu, USD yuvarlama ═══

    private async Task<string> KasaAcPb(Kiraci k, string pb, decimal acilis)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson("/api/v1/kasalar", new { ad = "Doviz " + pb, subeId = k.SubeId, paraBirimi = pb });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var id = (await c.Json()).GetProperty("id").GetString()!;
        using var db = f.Vt.OlusturSistem();
        var kasa = await db.Kasalar.SingleAsync(x => x.Id == id);
        kasa.Bakiye = acilis;
        await db.SaveChangesAsync();
        return id;
    }

    // ── aynı gider İKİNCİ kez stornolanamaz; toplam 0,00 ve adet 2 kalır (çift sayım yok) ──
    [Fact]
    public async Task DQ101c_ikinci_storno_reddedilir_toplam_sifir_kalir()
    {
        var k = YeniKiraci("PAKETQ10-1-TEST sinir cift storno");
        var kasa = await KasaAc(k, 1_000m);
        var kat = await KategoriAc(k);
        var a = await Gider(k, kasa, kat, 125.50m);
        await Storno(k, kasa, a);
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/kasalar/{kasa}/hareket/{a}/storno", new { gerekce = Gerekce });
        Assert.NotEqual(HttpStatusCode.OK, c.StatusCode);
        var dizi = await ToplamlarDizisi(k);
        Assert.Equal(0.00m, TryToplam(dizi));
        Assert.Equal(2, dizi.EnumerateArray().Single().GetProperty("adet").GetInt32());
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(1_000.00m, (await db.Kasalar.AsNoTracking().SingleAsync(x => x.Id == kasa)).Bakiye);
    }

    // ── storno SATIRININ stornosu reddedilir; toplam 0,00 kalır (−125,50'ye kaymaz) ──
    [Fact]
    public async Task DQ101c_storno_satiri_stornolanamaz_toplam_bozulmaz()
    {
        var k = YeniKiraci("PAKETQ10-1-TEST sinir storno storno");
        var kasa = await KasaAc(k, 1_000m);
        var kat = await KategoriAc(k);
        var a = await Gider(k, kasa, kat, 125.50m);
        await Storno(k, kasa, a);
        string ters;
        using (var db = f.Vt.OlusturSistem())
            ters = (await db.KasaHareketleri.AsNoTracking().SingleAsync(h => h.StornoKaynakId == a)).Id;
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/kasalar/{kasa}/hareket/{ters}/storno", new { gerekce = Gerekce });
        Assert.NotEqual(HttpStatusCode.OK, c.StatusCode);
        Assert.Equal(0.00m, TryToplam(await ToplamlarDizisi(k)));
    }

    // ── USD kasada 15,755 → 15,76 (AwayFromZero); stornosu USD 0,00; TRY satırı DOĞMAZ ──
    [Fact]
    public async Task DQ101c_usd_kurus_alti_400_ve_storno_para_birimi_karismaz()
    {
        var k = YeniKiraci("PAKETQ10-1-TEST sinir usd");
        var kasa = await KasaAcPb(k, "USD", 100m);
        var kat = await KategoriAc(k);
        // PAKET GOC-A (K9): 15,755 YUVARLANMAZ (eski: 15,76) — 400; 15,76 aynen yazılır.
        Assert.Equal(HttpStatusCode.BadRequest, (await GiderPost(k, new { kasaId = kasa, giderKategoriId = kat, tutar = 15.755m })).StatusCode);
        var a = await Gider(k, kasa, kat, 15.76m);
        var d1 = (await ToplamlarDizisi(k)).EnumerateArray().Single();
        Assert.Equal("USD", d1.GetProperty("paraBirimi").GetString());
        Assert.Equal(15.76m, d1.GetProperty("tutar").GetDecimal());
        await Storno(k, kasa, a);
        var d2 = (await ToplamlarDizisi(k)).EnumerateArray().Single();
        Assert.Equal("USD", d2.GetProperty("paraBirimi").GetString());
        Assert.Equal(0.00m, d2.GetProperty("tutar").GetDecimal());
        Assert.Equal(2, d2.GetProperty("adet").GetInt32());
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(100.00m, (await db.Kasalar.AsNoTracking().SingleAsync(x => x.Id == kasa)).Bakiye);
    }
}
