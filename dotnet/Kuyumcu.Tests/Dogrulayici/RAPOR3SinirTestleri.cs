using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests.Dogrulayici;

// ════════════════════════════════════════════════════════════════════════
// PAKET RAPOR-3 — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT (üretim kodundan
// hesaplanmaz). Kapsam: aylık "Tahsilat" açılış devri dışlama (kuruş, sıfır,
// büyük tutar, elle DİĞER girişi), para biçimi iki ondalık ve yuvarlama sınırı,
// kâr satırı kapsam metni, Kâr-zarar boş dönem kararı, Türkçe giriş gerilemesi.
// ════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class RAPOR3SinirHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static async Task Giris(HttpClient c, string kasaId, string arac, decimal tutar)
    {
        var r = await c.PostAsJsonAsync($"/api/v1/kasalar/{kasaId}/hareket", new
        {
            yon = "GIRIS", tutar, odemeTuru = arac, aciklama = "R3S girişi",
        });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
    }

    private static async Task Acilis(HttpClient c, Kiraci k, string kasaId, decimal tl)
    {
        var satirlar = new[] { new { hedefId = kasaId, tlTutar = tl, hasGram = 0m } };
        var p = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/prova", new { tur = "KASA", satirlar });
        Assert.True(p.IsSuccessStatusCode, await p.Content.ReadAsStringAsync());
        var jeton = (await p.Json()).GetProperty("provaJetonu").GetString()!;
        var u = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/uygula", new
        {
            tur = "KASA", satirlar, provaJetonu = jeton,
            gerekce = "Eski programdan devreden acilis bakiyesi", kurSnapshotId = k.KurId,
        });
        Assert.True(u.IsSuccessStatusCode, await u.Content.ReadAsStringAsync());
    }

    private static async Task<(JsonElement aylik, JsonElement gunSonu)> Oku(HttpClient c)
    {
        var bugun = Zaman.YerelBugun();
        var gun = bugun.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var gs = await c.GetAsync($"/api/v1/raporlar/pos-fatura-mutabakati?tarih={gun}");
        Assert.Equal(HttpStatusCode.OK, gs.StatusCode);
        var a = await c.GetAsync($"/api/v1/raporlar/aylik?yil={bugun.Year}&ay={bugun.Month}");
        Assert.Equal(HttpStatusCode.OK, a.StatusCode);
        return ((await a.Json()).GetProperty("tahsilat"), (await gs.Json()).GetProperty("tahsilat"));
    }

    // Kuruş sınırı: 0,01 ₺ tahsilat + 150.000 ₺ açılış → aylık tahsilat TAM 0,01 (açılış ayrı alanda).
    [Fact]
    public async Task R3S01_Kurus_tahsilat_acilis_devri_dislanir()
    {
        var k = YeniKiraci("PAKETRAPOR-3-TEST s01");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Giris(c, k.KasaId, "NAKIT", 0.01m);
        await Acilis(c, k, k.KasaId, 150_000m);
        var (a, gs) = await Oku(c);
        Assert.Equal(0.01m, a.GetProperty("toplamTl").GetDecimal());
        Assert.Equal(150_000m, a.GetProperty("acilisDevri").GetDecimal());
        Assert.Equal(0.01m, gs.GetProperty("toplam").GetDecimal());
    }

    // Sıfır: yalnız açılış devri varsa tahsilat 0 (eksiye düşmez), açılış 999.999.999,99 bile olsa.
    [Fact]
    public async Task R3S02_Yalniz_buyuk_acilis_tahsilat_sifir_eksi_degil()
    {
        var k = YeniKiraci("PAKETRAPOR-3-TEST s02");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Acilis(c, k, k.KasaId, 999_999_999.99m);
        var (a, gs) = await Oku(c);
        Assert.Equal(0m, a.GetProperty("toplamTl").GetDecimal());
        Assert.Equal(999_999_999.99m, a.GetProperty("acilisDevri").GetDecimal());
        Assert.Equal(0m, gs.GetProperty("toplam").GetDecimal());
    }

    // Elle girilen DİĞER tutarı açılış devri DEĞİLDİR: tahsilatta kalır (yalnız ACILIS hareketine bağlı bacak düşer).
    [Fact]
    public async Task R3S03_Elle_diger_giris_tahsilatta_kalir()
    {
        var k = YeniKiraci("PAKETRAPOR-3-TEST s03");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Giris(c, k.KasaId, "DIGER", 250.55m);
        await Acilis(c, k, k.KasaId, 1_000m);
        var (a, gs) = await Oku(c);
        Assert.Equal(250.55m, a.GetProperty("toplamTl").GetDecimal());
        Assert.Equal(1_000m, a.GetProperty("acilisDevri").GetDecimal());
        Assert.Equal(250.55m, gs.GetProperty("toplam").GetDecimal());
    }
}

public sealed class RAPOR3SinirSafTestleri
{
    // ── Para biçimi: iki ondalık her zaman; AwayFromZero; bölünmez boşluk; eksi işareti ──
    [Theory]
    [InlineData("0", "0,00 ₺")]
    [InlineData("0.005", "0,01 ₺")]
    [InlineData("0.004", "0,00 ₺")]
    [InlineData("-0.005", "−0,01 ₺")]
    [InlineData("-0.004", "0,00 ₺")]
    [InlineData("3840", "3.840,00 ₺")]
    [InlineData("999999999.99", "999.999.999,99 ₺")]
    [InlineData("999999999.995", "1.000.000.000,00 ₺")]
    public void R3S10_TlKurus_iki_ondalik_sinirlar(string girdi, string beklenen) =>
        Assert.Equal(beklenen, Bicim.TlKurus(decimal.Parse(girdi, CultureInfo.InvariantCulture)));

    // ── Has 4 hane ──
    [Fact]
    public void R3S11_Has_dort_hane()
    {
        Assert.Equal("0,0001 has", Bicim.Has(0.0001m));
        Assert.Equal("9,1600 has", Bicim.Has(9.16m));
        Assert.Equal("0,0000 has", Bicim.Has(0m));
    }

    // ── Kâr satırı kapsamı: neyin eksik olduğunu söyler (TR + EN), sayılar sabit ──
    [Fact]
    public void R3S12_Maliyet_yok_metni_sinirlar()
    {
        var tr = KarRaporSozlugu.Tr;
        Assert.Equal("12 satışın maliyeti yok", KarRaporSozlugu.MaliyetYok(tr, 12, 0));
        Assert.Equal("1 iadenin maliyeti yok", KarRaporSozlugu.MaliyetYok(tr, 0, 1));
        Assert.Equal("12 satış ve 1 iadenin maliyeti yok", KarRaporSozlugu.MaliyetYok(tr, 12, 1));
        Assert.Equal("Kâr 1.082,68 ₺ (yalnız 1 satış) · 12 satışın maliyeti yok",
            string.Format(tr.PanelKarKapsam, Bicim.TlKurus(1082.68m), 1, KarRaporSozlugu.MaliyetYok(tr, 12, 0)));
        Assert.NotEqual(tr.PanelKarKapsam, KarRaporSozlugu.En.PanelKarKapsam);
    }

    // ── Kâr-zarar boş dönem kararı: satışsız + giderli dönem BOŞ DEĞİL; tamamen boş dönem BOŞ ──
    private static bool DonemBos(string json)
    {
        var kz = typeof(Kuyumcu.Web.Pages.KarZarar);
        var tip = kz.GetNestedType("OzetVeri", BindingFlags.NonPublic)!;
        var o = JsonSerializer.Deserialize(json, tip, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        return (bool)kz.GetMethod("DonemBos", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [o])!;
    }

    private const string Bos = "\"maliyetiBilinenAdet\":0,\"maliyetiBilinmeyenAdet\":0,\"guvenilirlik\":\"VERI_YOK\",\"hasilatHas\":0";

    [Fact]
    public void R3S13_Bos_donem_karari()
    {
        Assert.True(DonemBos("{\"hasilatTl\":0,\"satisAdedi\":0,\"donemKariTl\":0," + Bos + "}"));
        Assert.True(DonemBos("{\"hasilatTl\":0,\"satisAdedi\":0,\"donemKariTl\":null," + Bos + "}"));
        // gider var, satış yok → dönem kârı −500 gösterilmeli (boş durumla GİZLENMEZ)
        Assert.False(DonemBos("{\"hasilatTl\":0,\"satisAdedi\":0,\"donemKariTl\":-500," + Bos + "}"));
        // yalnız iade → boş değil
        Assert.False(DonemBos("{\"hasilatTl\":-0.01,\"satisAdedi\":0,\"iadeAdedi\":1,\"donemKariTl\":0," + Bos + "}"));
        // 1 kuruşluk satış → boş değil
        Assert.False(DonemBos("{\"hasilatTl\":0.01,\"satisAdedi\":1,\"donemKariTl\":0.01," + Bos + "}"));
    }

    // ── Türkçe giriş gerilemesi (FormAlani.SayiCoz) ──
    [Fact]
    public void R3S14_Turkce_giris()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI TUR 2 (Aşkın + Mehmet) — PAKET RAPOR-3 EK FİNANSAL SINIR TESTLERİ (08.10, tur 1 sınıflarına EK).
// Kapsam: aylık "Tahsilat" (açılış devri / virman / döviz hariç) = Gün sonu toplam tahsilatı;
// gün sonu dovizGiris; ekran para/has biçimi (Bicim.TlKurus, Bicim.Has); Türkçe sayı girişi.
// Beklenen değerler SABİT yazılmıştır (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class RAPOR3SinirTur2HttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static async Task<string> KasaAc(HttpClient c, Kiraci k, string ad, string pb = "TRY")
    {
        var r = await c.PostJson("/api/v1/kasalar", new { ad, subeId = k.SubeId, paraBirimi = pb });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Json()).GetProperty("id").GetString()!;
    }

    private static async Task<HttpResponseMessage> Hareket(HttpClient c, string kasaId, string yon, decimal tutar, string arac = "NAKIT") =>
        await c.PostAsJsonAsync($"/api/v1/kasalar/{kasaId}/hareket", new { yon, tutar, odemeTuru = arac, aciklama = "RAPOR3 sinir" });

    private static async Task Acilis(HttpClient c, Kiraci k, string kasaId, decimal tutar)
    {
        var satirlar = new[] { new { hedefId = kasaId, tlTutar = tutar, hasGram = 0m } };
        var p = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/prova", new { tur = "KASA", satirlar });
        Assert.True(p.IsSuccessStatusCode, await p.Content.ReadAsStringAsync());
        var jeton = (await p.Json()).GetProperty("provaJetonu").GetString()!;
        var u = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/uygula", new
        {
            tur = "KASA", satirlar, provaJetonu = jeton,
            gerekce = "Eski programdan devreden acilis bakiyesi", kurSnapshotId = k.KurId,
        });
        Assert.True(u.IsSuccessStatusCode, await u.Content.ReadAsStringAsync());
    }

    private static async Task<(JsonElement aylik, JsonElement gunSonu)> Oku(HttpClient c, DateOnly gun)
    {
        var g = gun.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var gr = await c.GetAsync($"/api/v1/raporlar/pos-fatura-mutabakati?tarih={g}");
        Assert.Equal(HttpStatusCode.OK, gr.StatusCode);
        var ar = await c.GetAsync($"/api/v1/raporlar/aylik?yil={gun.Year}&ay={gun.Month}");
        Assert.Equal(HttpStatusCode.OK, ar.StatusCode);
        return ((await ar.Json()).GetProperty("tahsilat"), (await gr.Json()).GetProperty("tahsilat"));
    }

    // SIFIR / BOŞ: hiç kasa hareketi olmayan geçmiş ay → tahsilat 0, açılış 0, virman 0, döviz girişi boş.
    [Fact]
    public async Task S01_Bos_donem_tahsilat_sifir_acilis_sifir_doviz_bos()
    {
        var k = YeniKiraci("PAKETRAPOR-3-TEST s01");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var (a, g) = await Oku(c, new DateOnly(2020, 1, 15));
        Assert.Equal(0m, a.GetProperty("toplamTl").GetDecimal());
        Assert.Equal(0m, a.GetProperty("acilisDevri").GetDecimal());
        Assert.Equal(0m, a.GetProperty("virmanGiris").GetDecimal());
        Assert.Equal(0m, g.GetProperty("toplam").GetDecimal());
        Assert.Empty(g.GetProperty("dovizGiris").EnumerateArray());
    }

    // YALNIZ AÇILIŞ: tahsilat EKSİYE düşmez (0), açılış ayrı alanda 150.000.
    [Fact]
    public async Task S02_Yalniz_acilis_devri_tahsilat_sifir_eksi_degil()
    {
        var k = YeniKiraci("PAKETRAPOR-3-TEST s02");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Acilis(c, k, k.KasaId, 150_000m);
        var (a, g) = await Oku(c, Zaman.YerelBugun());
        Assert.Equal(0m, a.GetProperty("toplamTl").GetDecimal());
        Assert.Equal(150_000m, a.GetProperty("acilisDevri").GetDecimal());
        Assert.Equal(0m, g.GetProperty("toplam").GetDecimal());
        Assert.Equal(0m, g.GetProperty("diger").GetDecimal());
        Assert.Equal(150_000m, g.GetProperty("acilisDevri").GetDecimal());
    }

    // EKSİ: kasa ÇIKIŞI tahsilat değildir — toplamı düşürmez de artırmaz da; elle DIGER girişi tahsilattır.
    [Fact]
    public async Task S03_Cikis_tahsilati_etkilemez_elle_diger_girisi_tahsilattir()
    {
        var k = YeniKiraci("PAKETRAPOR-3-TEST s03");
        var c = f.Ortam.Istemci(k, "SAHIP");
        Assert.True((await Hareket(c, k.KasaId, "GIRIS", 1_000m)).IsSuccessStatusCode);
        Assert.True((await Hareket(c, k.KasaId, "GIRIS", 700m, "DIGER")).IsSuccessStatusCode);
        Assert.True((await Hareket(c, k.KasaId, "CIKIS", 400m)).IsSuccessStatusCode);
        await Acilis(c, k, k.KasaId, 50m);
        var (a, g) = await Oku(c, Zaman.YerelBugun());
        Assert.Equal(1_700m, a.GetProperty("toplamTl").GetDecimal());
        Assert.Equal(1_700m, g.GetProperty("toplam").GetDecimal());
        Assert.Equal(700m, g.GetProperty("diger").GetDecimal());
        Assert.Equal(50m, a.GetProperty("acilisDevri").GetDecimal());
    }

    // KURUŞ: 0,01 tahsilat + 0,01 açılış → tahsilat 0,01 (kuruş kaybolmaz, açılış çıkarılırken yuvarlama sapması yok).
    [Fact]
    public async Task S04_Kurus_sinirinda_tahsilat_ve_acilis_ayrimi()
    {
        var k = YeniKiraci("PAKETRAPOR-3-TEST s04");
        var c = f.Ortam.Istemci(k, "SAHIP");
        Assert.True((await Hareket(c, k.KasaId, "GIRIS", 0.01m)).IsSuccessStatusCode);
        await Acilis(c, k, k.KasaId, 0.01m);
        var (a, g) = await Oku(c, Zaman.YerelBugun());
        Assert.Equal(0.01m, a.GetProperty("toplamTl").GetDecimal());
        Assert.Equal(0.01m, g.GetProperty("toplam").GetDecimal());
        Assert.Equal(0.01m, a.GetProperty("acilisDevri").GetDecimal());
    }

    // ÇOK BÜYÜK: 999.999.999,99 giriş → iki uç aynı, tam değer.
    [Fact]
    public async Task S05_Cok_buyuk_tutar_iki_uc_ayni()
    {
        var k = YeniKiraci("PAKETRAPOR-3-TEST s05");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var r = await Hareket(c, k.KasaId, "GIRIS", 999_999_999.99m);
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        var (a, g) = await Oku(c, Zaman.YerelBugun());
        Assert.Equal(999_999_999.99m, a.GetProperty("toplamTl").GetDecimal());
        Assert.Equal(999_999_999.99m, g.GetProperty("toplam").GetDecimal());
    }

    // DÖVİZ: USD 100 + EUR 10 giriş, USD 40 çıkış → TL toplamına GİRMEZ; dovizGiris para birimi başına, sıralı (EUR, USD),
    // çıkış girişi azaltmaz (giriş tanımı). Döviz kasası açılışı bu sürümde yok (DOVIZ_HESAP_FAZ_B) — prova reddi de sınanır.
    [Fact]
    public async Task S06_Doviz_girisi_TL_toplamina_girmez_para_birimi_basina()
    {
        var k = YeniKiraci("PAKETRAPOR-3-TEST s06");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var usd = await KasaAc(c, k, "S06 dolar", "USD");
        var eur = await KasaAc(c, k, "S06 avro", "EUR");
        Assert.True((await Hareket(c, usd, "GIRIS", 100m)).IsSuccessStatusCode);
        Assert.True((await Hareket(c, eur, "GIRIS", 10m)).IsSuccessStatusCode);
        Assert.True((await Hareket(c, usd, "CIKIS", 40m)).IsSuccessStatusCode);
        var p = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/prova", new { tur = "KASA", satirlar = new[] { new { hedefId = usd, tlTutar = 30m, hasGram = 0m } } });
        var pj = await p.Json();
        Assert.Equal(JsonValueKind.Null, pj.GetProperty("provaJetonu").ValueKind);
        var (a, g) = await Oku(c, Zaman.YerelBugun());
        Assert.Equal(0m, a.GetProperty("toplamTl").GetDecimal());
        Assert.Equal(0m, g.GetProperty("toplam").GetDecimal());
        Assert.Equal(0m, g.GetProperty("nakit").GetDecimal());
        var d = g.GetProperty("dovizGiris").EnumerateArray().ToList();
        Assert.Equal(2, d.Count);
        Assert.Equal("EUR", d[0].GetProperty("paraBirimi").GetString());
        Assert.Equal(10m, d[0].GetProperty("tutar").GetDecimal());
        Assert.Equal("USD", d[1].GetProperty("paraBirimi").GetString());
        Assert.Equal(100m, d[1].GetProperty("tutar").GetDecimal());
    }
}

public sealed class RAPOR3SinirTur2BicimTestleri
{
    private const string Nb = " ";

    [Theory]
    [InlineData("0", "0,00" + Nb + "₺")]
    [InlineData("0.005", "0,01" + Nb + "₺")]          // AwayFromZero
    [InlineData("0.004", "0,00" + Nb + "₺")]
    [InlineData("-0.004", "0,00" + Nb + "₺")]         // kuruşa yuvarlanınca sıfır olan eksi işaretsiz
    [InlineData("-1082.68", "−1.082,68" + Nb + "₺")]
    [InlineData("999999999.99", "999.999.999,99" + Nb + "₺")]
    [InlineData("1082.675", "1.082,68" + Nb + "₺")]
    public void B01_TlKurus_sinirlari(string ham, string beklenen) =>
        Assert.Equal(beklenen, Bicim.TlKurus(decimal.Parse(ham, CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData("0", "0,0000 has")]
    [InlineData("0.0001", "0,0001 has")]
    [InlineData("36.234", "36,2340 has")]
    [InlineData("9.16", "9,1600 has")]
    public void B02_Has_dort_hane(string ham, string beklenen) =>
        Assert.Equal(beklenen, Bicim.Has(decimal.Parse(ham, CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData("45.000", true, "45000")]
    [InlineData("1,5", true, "1.5")]
    [InlineData("999.999.999,99", true, "999999999.99")]
    [InlineData("1.5", true, "1.5")]       // projede nokta + 3'ten az hane = ondalık (P-78 kuralı)
    public void B03_SayiCoz_Turkce_giris(string ham, bool tamam, string deger)
    {
        var s = FormAlani.SayiCoz(ham);
        Assert.Equal(tamam, s.Tamam);
        Assert.Equal(decimal.Parse(deger, CultureInfo.InvariantCulture), s.Deger);
    }

    [Theory]
    [InlineData("1.500")]     // belirsiz (binlik mi ondalık mı) → REDDEDİLİR
    [InlineData("")]
    [InlineData(null)]
    public void B04_SayiCoz_belirsiz_ve_bos_tamam_degil(string? ham) =>
        Assert.False(FormAlani.SayiCoz(ham).Tamam);
}
