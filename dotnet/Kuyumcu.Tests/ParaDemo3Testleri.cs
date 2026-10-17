using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO-3 — PARA-DEMO-2 kapanış tasarım hakemi kalanları
//
//   (K) KRİTİK: kasa kapısı mesajları ("bakiyesi yetersiz" toplam · "nakit yetersiz" nakit payı)
//       hiçbir ekranda "312547.91 TRY" kalıbıyla görünmez: sunucu tr-TR + işaret yazar, ret gövdesi
//       makine kodu taşır; istemci (KasaBakiyeMesaji) iki biçimi de okuyup sözlükten (TR+EN) yazar.
//       Kasalar › + Hareket › Gider › 2.000.000 senaryosu ekranda.
//   (T) YÜKSEK: değişim kilidi eksik alanların TAMAMINI tek cümlede sayar; BASİT'te "MASAK" yok.
//   (G) ORTA: gün sonu faturasız tablosu gerçek karmada bacak türlerini yazar ("Kart + Havale").
//   (N) ORTA: Gider formu önce çekmecedeki NAKDİ, sonra toplamı gösterir.
// Beklenen değerler SABİT ve elle hesaplanmıştır; Türkçe JSON gövdeleri ayrıştırılarak sınanır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class ParaDemo3Testleri(HttpFikstur f)
{
    private const string Nbsp = " ";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static async Task<string> KasaAc(HttpClient c, Kiraci k, string ad)
    {
        var r = await c.PostJson("/api/v1/kasalar", new { ad, subeId = k.SubeId, paraBirimi = "TRY" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Json()).GetProperty("id").GetString()!;
    }

    private static async Task Hareket(HttpClient c, string kasaId, string yon, string arac, decimal tutar, HttpStatusCode beklenen)
    {
        var r = await c.PostAsJsonAsync($"/api/v1/kasalar/{kasaId}/hareket", new
        {
            yon, tutar, odemeTuru = arac, aciklama = "PARA-DEMO-3 test hareketi",
        });
        Assert.True(r.StatusCode == beklenen, $"{r.StatusCode}: {await r.Content.ReadAsStringAsync()}");
    }

    private static readonly DilServisi Tr = new(null!);

    /// <summary>İngilizce dil servisi (localStorage/JS yok — boş JS çalışma zamanı).</summary>
    private static DilServisi Ingilizce()
    {
        var d = new DilServisi(new BosJs());
        d.DegistirAsync("en").GetAwaiter().GetResult();
        return d;
    }

    private sealed class BosJs : Microsoft.JSInterop.IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult(default(TValue)!);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => ValueTask.FromResult(default(TValue)!);
    }

    // ═══ (K) toplam kapısı: tr-TR + ₺ + kod; istemci biçimleyici sözlükten yazar ═══

    [Fact]
    public async Task PD3_K_elle_cikis_toplam_kapisi_mesaji_trTR_isaretli_TRY_yok_kod_KASA_BAKIYE_YETERSIZ()
    {
        var k = YeniKiraci("PD3-K toplam");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await KasaAc(c, k, "PD3 Ana Kasa");
        await Hareket(c, kasa, "GIRIS", "NAKIT", 312_547.91m, HttpStatusCode.OK);

        var r = await c.PostAsJsonAsync($"/api/v1/kasalar/{kasa}/hareket", new
        {
            yon = "CIKIS", tutar = 2_000_000m, odemeTuru = "NAKIT", aciklama = "PARA-DEMO-3 aşım",
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var g = await r.Json();
        var mesaj = g.GetProperty("message").GetString()!;
        Assert.Equal($"'PD3 Ana Kasa' kasasının bakiyesi yetersiz: 312.547,91{Nbsp}₺ var, 2.000.000,00{Nbsp}₺ isteniyor. Nakit kasa eksiye düşemez.", mesaj);
        Assert.DoesNotContain("TRY", mesaj);
        Assert.DoesNotContain("312547.91", mesaj);
        Assert.Equal(Kuyumcu.Api.Modules.Kasa.KasaBakiyeKapisi.YetersizKodu, g.GetProperty("kod").GetString());

        // İstemci sözleşmesi: GERÇEK sunucu cümlesi ayrıştırılır ve sözlükten yazılır (TR + EN).
        var tr = KasaBakiyeMesaji.Bicimle(mesaj, Tr);
        Assert.Equal($"'PD3 Ana Kasa' kasasında yeterli para yok: kasada 312.547,91{Nbsp}₺ var, 2.000.000{Nbsp}₺ isteniyor. Nakit kasa eksiye düşemez.", tr);
        var en = Ingilizce();
        var enMetin = KasaBakiyeMesaji.Bicimle(mesaj, en)!;
        Assert.StartsWith("Not enough cash in 'PD3 Ana Kasa'", enMetin);
        Assert.DoesNotContain("TRY", enMetin);
        Assert.DoesNotContain("kasasının", enMetin);
    }

    [Fact]
    public async Task PD3_K_virman_toplam_kapisi_projeksiyon_yolu_da_trTR()
    {
        var k = YeniKiraci("PD3-K virman");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await KasaAc(c, k, "PD3 Kaynak");
        var hedef = await KasaAc(c, k, "PD3 Hedef");
        await Hareket(c, kasa, "GIRIS", "NAKIT", 1_000m, HttpStatusCode.OK);
        var r = await c.PostAsJsonAsync("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kasa, hedefTur = "KASA", hedefId = hedef, tutar = 2_500.5m,
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var mesaj = (await r.Json()).GetProperty("message").GetString()!;
        Assert.Contains($"1.000,00{Nbsp}₺ var, 2.500,50{Nbsp}₺ isteniyor", mesaj);
        Assert.DoesNotContain("TRY", mesaj);
    }

    [Fact]
    public async Task PD3_K_nakit_kapisi_kod_ve_istemci_ayni_dil_ve_bicimle_yazar()
    {
        var k = YeniKiraci("PD3-K nakit");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await KasaAc(c, k, "PD3 Nakit Kasa");
        await Hareket(c, kasa, "GIRIS", "NAKIT", 1_000m, HttpStatusCode.OK);
        await Hareket(c, kasa, "GIRIS", "KART", 500m, HttpStatusCode.OK);
        var r = await c.PostAsJsonAsync($"/api/v1/kasalar/{kasa}/hareket", new
        {
            yon = "CIKIS", tutar = 1_200m, odemeTuru = "NAKIT", aciklama = "PARA-DEMO-3 nakit aşımı",
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var g = await r.Json();
        Assert.Equal(Kuyumcu.Api.Modules.Kasa.KasaBakiyeKapisi.NakitYetersizKodu, g.GetProperty("kod").GetString());
        var mesaj = g.GetProperty("message").GetString()!;
        var tr = KasaBakiyeMesaji.Bicimle(mesaj, Tr);
        Assert.Equal(
            $"'PD3 Nakit Kasa' kasasında nakit yetersiz: çekmecede 1.000{Nbsp}₺ nakit var, 1.200{Nbsp}₺ nakit isteniyor. Kasadaki kart (500{Nbsp}₺) ve havale (0{Nbsp}₺) tahsilatı nakit olarak verilemez.",
            tr);
        var en = Ingilizce();
        var enMetin = KasaBakiyeMesaji.Bicimle(mesaj, en)!;
        Assert.StartsWith("Not enough cash in 'PD3 Nakit Kasa'", enMetin);
        Assert.DoesNotContain("çekmecede", enMetin);
    }

    /// <summary>Tur 2 (doğrulayıcı DÜŞÜK): elle KART çıkışında araç payı reddi istemcide sözlükten (TR+EN) yazılır.</summary>
    [Fact]
    public async Task PD3T2_arac_payi_reddi_istemcide_TR_EN_cevrilir()
    {
        var k = YeniKiraci("PD3-T2 arac");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await KasaAc(c, k, "PD3 Kart Kasa");
        await Hareket(c, kasa, "GIRIS", "NAKIT", 1_000m, HttpStatusCode.OK);
        await Hareket(c, kasa, "GIRIS", "KART", 500m, HttpStatusCode.OK);
        var r = await c.PostAsJsonAsync($"/api/v1/kasalar/{kasa}/hareket", new
        {
            yon = "CIKIS", tutar = 800m, odemeTuru = "KART", aciklama = "PARA-DEMO-3 tur 2 kart payı aşımı",
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var g = await r.Json();
        Assert.Equal(Kuyumcu.Api.Modules.Kasa.KasaBakiyeKapisi.AracPayiYetersizKodu, g.GetProperty("kod").GetString());
        var mesaj = g.GetProperty("message").GetString()!;
        Assert.Equal($"'PD3 Kart Kasa' kasasındaki kart tahsilatı 500{Nbsp}₺; 800{Nbsp}₺ kart olarak çıkarılamaz.", KasaBakiyeMesaji.Bicimle(mesaj, Tr));
        var en = KasaBakiyeMesaji.Bicimle(mesaj, Ingilizce())!;
        Assert.Equal($"'PD3 Kart Kasa' holds 500{Nbsp}₺ of card takings; 800{Nbsp}₺ cannot be paid out by card.", en);
        Assert.DoesNotContain("kasasındaki", en);
        Assert.DoesNotContain("TRY", en);
    }

    /// <summary>Tur 2: NAKİT çıkışta toplam reddi, ekran kart/havale payını biliyorsa NAKİT cümlesiyle yazılır.</summary>
    [Fact]
    public void PD3T2_toplam_reddi_kart_payi_varsa_nakit_cumlesiyle_yalniz_nakitte_aynen()
    {
        var sunucu = $"'Ana Kasa' kasasının bakiyesi yetersiz: 221.726,43{Nbsp}₺ var, 2.000.000,00{Nbsp}₺ isteniyor. Nakit kasa eksiye düşemez.";
        var ayrim = new Kuyumcu.Contracts.Sayfalar.Kasalar.KasaNakitAyrimiOzeti("k1", "TRY", 221_726.43m, 193_830.90m, 25_895.53m, 2_000m);
        Assert.Equal($"'Ana Kasa' kasasında nakit yetersiz: çekmecede 193.830,90{Nbsp}₺ nakit var, 2.000.000{Nbsp}₺ nakit isteniyor. Kasadaki kart (25.895,53{Nbsp}₺) ve havale (2.000{Nbsp}₺) tahsilatı nakit olarak verilemez.",
            KasaBakiyeMesaji.Bicimle(sunucu, Tr, ayrim));
        Assert.DoesNotContain("221.726,43", KasaBakiyeMesaji.Bicimle(sunucu, Tr, ayrim));
        // Yalnız nakit tutan kasa: toplam cümlesi aynen (toplam = nakit).
        var yalnizNakit = ayrim with { Toplam = 221_726.43m, Nakit = 221_726.43m, Kart = 0m, Havale = 0m };
        Assert.Equal(KasaBakiyeMesaji.Bicimle(sunucu, Tr), KasaBakiyeMesaji.Bicimle(sunucu, Tr, yalnizNakit));
        Assert.Equal(KasaBakiyeMesaji.Bicimle(sunucu, Tr), KasaBakiyeMesaji.Bicimle(sunucu, Tr, null));
    }

    [Fact]
    public void PD3_K_istemci_iki_bicim_ve_kesme_isaretli_kasa_adi()
    {
        // Eski (invariant) biçim — geçiş uyumu.
        Assert.Equal($"'Ana Kasa' kasasında yeterli para yok: kasada 312.547,91{Nbsp}₺ var, 2.000.000{Nbsp}₺ isteniyor. Nakit kasa eksiye düşemez.",
            KasaBakiyeMesaji.Bicimle("'Ana Kasa' kasasının bakiyesi yetersiz: 312547.91 TRY var, 2000000.00 TRY isteniyor. Nakit kasa eksiye düşemez.", Tr));
        // Yeni biçim: eksi (U+2212) bakiye ve kesme işaretli ad.
        var m = KasaBakiyeMesaji.Bicimle($"'Merkez'deki Kasa' kasasının bakiyesi yetersiz: −500,25{Nbsp}₺ var, 1.000,00{Nbsp}₺ isteniyor. Nakit kasa eksiye düşemez.", Tr)!;
        Assert.StartsWith("'Merkez'deki Kasa' kasasında yeterli para yok", m);
        Assert.Matches($"kasada [−-]500,25{Nbsp}₺ var", m);
        // Dolar kasası: işaret → kod → yine işaret.
        Assert.Contains(Bicim.Para(1_250.50m, "USD"), KasaBakiyeMesaji.Bicimle($"'D' kasasının bakiyesi yetersiz: 1.250,50{Nbsp}$ var, 2.000,00{Nbsp}$ isteniyor.", Tr));
        // Kalıp dışı → null (uydurma yok).
        Assert.Null(KasaBakiyeMesaji.Bicimle("Kasa kapalı", Tr));
    }

    // ═══ (G) gün sonu faturasız tablosu: gerçek karmada bacak araçları ═══

    [Fact]
    public async Task PD3_G_gun_sonu_faturasiz_KARMA_satiri_odemeAraclari_KART_HAVALE_tek_bacak_null()
    {
        var k = YeniKiraci("PD3-G karma");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var karma = await c.PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 5_000m } },
            odemeler = new[] { new { arac = "KART", tutarTl = 3_000m, kasaId = k.KasaId }, new { arac = "HAVALE", tutarTl = 2_000m, kasaId = k.KasaId } },
        });
        Assert.True(karma.IsSuccessStatusCode, await karma.Content.ReadAsStringAsync());
        var tek = await c.PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 1_000m } },
            odemeler = new[] { new { arac = "HAVALE", tutarTl = 1_000m, kasaId = k.KasaId } },
        });
        Assert.True(tek.IsSuccessStatusCode, await tek.Content.ReadAsStringAsync());

        var gun = Zaman.YerelBugun().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var m = await (await c.GetAsync($"/api/v1/raporlar/pos-fatura-mutabakati?tarih={gun}")).Json();
        var satirlar = m.GetProperty("faturasizIslemler").EnumerateArray().ToList();
        var k5 = satirlar.Single(s => s.GetProperty("tlTutari").GetDecimal() == 5_000m);
        Assert.Equal("KARMA", k5.GetProperty("odemeTuru").GetString());
        Assert.Equal(new[] { "KART", "HAVALE" }, k5.GetProperty("odemeAraclari").EnumerateArray().Select(x => x.GetString()!).ToArray());
        var t1 = satirlar.Single(s => s.GetProperty("tlTutari").GetDecimal() == 1_000m);
        Assert.Equal("HAVALE", t1.GetProperty("odemeTuru").GetString());
        Assert.Equal(JsonValueKind.Null, t1.GetProperty("odemeAraclari").ValueKind);

        // Ekstreyle AYNI dil ve TEK sözlük (tur 2): adlar ekstrenin ParaDemo sözlüğünden.
        Assert.Equal("Kart + Havale", ParaDemo3Sozlugu.OdemeAdi("KARMA", ["KART", "HAVALE"], "tr"));
        Assert.Equal("Card + Transfer", ParaDemo3Sozlugu.OdemeAdi("KARMA", ["KART", "HAVALE"], "en"));
        Assert.Equal(ParaDemoSozlugu.Tr.OdemeKarma, ParaDemo3Sozlugu.OdemeAdi("KARMA", null, "tr"));   // eski kayıt: ekstreyle aynı ad
        Assert.Equal(KatalogSozlugu.OdemeAraci("BILINMEZ", "tr"), ParaDemo3Sozlugu.OdemeAdi("BILINMEZ", null, "tr"));   // bilinmeyen kod: uydurma yok
    }

    [Fact]
    public void PD3_sozluk_TR_ve_EN_dolu_ve_BASIT_metinlerde_MASAK_yok()
    {
        foreach (var p in typeof(ParaDemo3Metinleri).GetProperties())
        {
            var tr = (string)p.GetValue(ParaDemo3Sozlugu.Tr)!;
            var en = (string)p.GetValue(ParaDemo3Sozlugu.En)!;
            Assert.False(string.IsNullOrWhiteSpace(tr), p.Name);
            Assert.False(string.IsNullOrWhiteSpace(en), p.Name);
            if (p.Name is not ("GerekceBirlestir" or "TakasEksikBasit")) Assert.NotEqual(tr, en);
        }
        Assert.DoesNotContain("MASAK", ParaDemo3Sozlugu.Tr.TakasEksikBasit);
        Assert.DoesNotContain("MASAK", ParaDemo3Sozlugu.Tr.AlanIslemAmaci + ParaDemo3Sozlugu.Tr.AlanBeyanAciklama);
        // Tur 2 (doğrulayıcı DÜŞÜK): kilit metni ile alan yardımı (MasakFormu ipucu) AYNI terimi kullanır ("karakter").
        Assert.Contains(string.Format(Sozlukler.Tr.Islem.MasakEnAzKarakter, 20), string.Format(ParaDemo3Sozlugu.Tr.AlanBeyanAciklama, 20));
        Assert.Contains(string.Format(Sozlukler.En.Islem.MasakEnAzKarakter, 20), string.Format(ParaDemo3Sozlugu.En.AlanBeyanAciklama, 20));
        Assert.DoesNotContain("harf", ParaDemo3Sozlugu.Tr.AlanBeyanAciklama);
    }
}

// ── (K) Kasalar › + Hareket › Gider › 2.000.000 — çekmecede tr-TR, "TRY" yok ──
public sealed class ParaDemo3KasaEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string redMesaji = "";
    private bool kartPayi;   // tur 2: kasada kart payı var mı (nakit-ayrimi yanıtı)

    public ParaDemo3KasaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => redMesaji, () => kartPayi)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<string> red, Func<bool> kartPayi) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            var get = istek.Method == HttpMethod.Get;
            if (!get && yol.Contains("/kasalar/k1/hareket"))
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { message = red(), kod = "KASA_BAKIYE_YETERSIZ", error = "Bad Request", statusCode = 400 }),
                        System.Text.Encoding.UTF8, "application/json"),
                };
            return ParaDemoSahte.Tamam(yol switch
            {
                var v when v.Contains("/kur/guncel") => """{"id":"kur1","altinHasFiyati":4100}""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez Şube"}]""",
                var v when get && v.Contains("/kasalar/nakit-ayrimi") => kartPayi()
                    ? """[{"kasaId":"k1","paraBirimi":"TRY","toplam":312547.91,"nakit":290000,"kart":22547.91,"havale":0}]"""
                    : """[{"kasaId":"k1","paraBirimi":"TRY","toplam":312547.91,"nakit":312547.91,"kart":0,"havale":0}]""",
                var v when get && v.Contains("/kasalar/") && v.Contains("/hareketler") => """{"hareketler":[]}""",
                var v when get && v.EndsWith("/kasalar") =>
                    """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":312547.91,"aktif":true,"subeId":"s1"}]""",
                var v when v.Contains("/kasalar/") => """{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":312547.91,"aktif":true,"subeId":"s1"}""",
                _ => "{}",
            });
        }
    }

    /// <summary>Sunucunun GERÇEK cümlesi (KasaBakiyeKapisi.Dogrula) — istemci sözleşmesi uydurma metinle sınanmaz.</summary>
    private static string SunucuCumlesi()
    {
        var kasa = new Kuyumcu.Api.Data.Kasa { Id = "k1", TenantId = "t", SubeId = "s1", Ad = "Ana Kasa", ParaBirimi = "TRY", Bakiye = 312_547.91m };
        var ex = Assert.Throws<BadRequestException>(() => Kuyumcu.Api.Modules.Kasa.KasaBakiyeKapisi.Dogrula(kasa, -2_000_000m));
        return ex.Message;
    }

    [Theory]
    [InlineData("BASIT", "tr")]
    [InlineData("PROFESYONEL", "tr")]
    [InlineData("BASIT", "en")]
    public async Task PD3E_K_elle_gider_2_000_000_reddi_cekmecede_trTR_TRY_yok(string mod, string dil)
    {
        redMesaji = SunucuCumlesi();
        Assert.DoesNotContain("TRY", redMesaji);   // sunucu cümlesinin kendisi de ham değil
        if (dil == "en") await Services.GetRequiredService<DilServisi>().DegistirAsync("en");
        Services.GetRequiredService<OturumServisi>().KullaniciAta(new OturumKullanici(
            "u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Kuyumcu.Web.Pages.Kasalar>().AyrintiAc();
        e.WaitForElement("#kasa-hareket-k1").Click();
        e.WaitForElement("#h-tutar");
        SeciciYardim.Sec(e, "h-tur", "CIKIS");
        e.Find("#h-tutar").Input("2000000");
        e.Find("#h-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/kasalar/k1/hareket")));
        var bant = e.WaitForElement("#cekmece-kasa-hareket .bant-hata").TextContent;
        Assert.DoesNotContain("TRY", bant);
        Assert.DoesNotContain("312547.91", bant);
        Assert.DoesNotContain("2000000.00", bant);
        Assert.Contains("312.547,91", bant);
        Assert.Contains("2.000.000", bant);
        var beklenen = string.Format(Services.GetRequiredService<DilServisi>().T.Gider.YetersizBakiye, "Ana Kasa",
            Bicim.Para(312_547.91m, "TRY"), Bicim.Para(2_000_000m, "TRY"));
        Assert.Equal(beklenen, bant.Trim());
        // Gövde: yön ÇIKIŞ ve tutar 2.000.000 (JSON ayrıştırılarak).
        var g = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST").Govde).RootElement;
        Assert.Equal("CIKIS", g.GetProperty("yon").GetString());
        Assert.Equal(2_000_000m, g.GetProperty("tutar").GetDecimal());
    }

    /// <summary>
    /// Tur 2 (doğrulayıcı DÜŞÜK): kasada kart payı varken elle NAKİT çıkışın toplam reddi "kasada 312.547,91 ₺ var"
    /// DEMEZ (kullanıcı o tutara güvenip ikinci bir nakit reddi alırdı); çekmecedeki nakit ve kart payı ayrı yazılır.
    /// </summary>
    [Theory]
    [InlineData("tr")]
    [InlineData("en")]
    public async Task PD3T2_elle_nakit_cikis_toplam_reddi_kart_payi_varsa_cekmecedeki_nakdi_soyler(string dil)
    {
        kartPayi = true;
        redMesaji = SunucuCumlesi();
        if (dil == "en") await Services.GetRequiredService<DilServisi>().DegistirAsync("en");
        Services.GetRequiredService<OturumServisi>().KullaniciAta(new OturumKullanici(
            "u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1", "BASIT", "BASIT"));
        var e = Render<Kuyumcu.Web.Pages.Kasalar>().AyrintiAc();
        e.WaitForElement("#kasa-hareket-k1").Click();
        e.WaitForElement("#h-tutar");
        SeciciYardim.Sec(e, "h-tur", "CIKIS");
        e.Find("#h-tutar").Input("2000000");
        e.Find("#h-kaydet").Click();
        var bant = e.WaitForElement("#cekmece-kasa-hareket .bant-hata").TextContent.Trim();
        var beklenen = string.Format(ParaDemo3Sozlugu.Al(dil).NakitYetersiz, "Ana Kasa", Bicim.Para(290_000m, "TRY"),
            Bicim.Para(2_000_000m, "TRY"), Bicim.Para(22_547.91m, "TRY"), Bicim.Para(0m, "TRY"));
        Assert.Equal(beklenen, bant);
        Assert.DoesNotContain("312.547,91", bant);
        Assert.DoesNotContain("TRY", bant);
    }
}

// ── (N) Gider formu: çekmecedeki nakit önce, toplam ikinci ───────────────────
public sealed class ParaDemo3GiderEkranTestleri : BunitContext
{
    private bool ayrimHatali;

    public ParaDemo3GiderEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => ayrimHatali)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(Func<bool> ayrimHatali) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            if (yol.EndsWith("/kasalar/nakit-ayrimi") && ayrimHatali())
                return Task.FromResult(ParaDemoSahte.Hata(500, "Sunucu hatası (test)"));
            return Task.FromResult(ParaDemoSahte.Tamam(yol switch
            {
                var y when y.EndsWith("/kasalar/nakit-ayrimi") =>
                    """[{"kasaId":"k1","paraBirimi":"TRY","toplam":377738.62,"nakit":195625.82,"kart":120000,"havale":62112.80}]""",
                var y when y.EndsWith("/kasalar") => """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":377738.62,"aktif":true}]""",
                var y when y.EndsWith("/bankalar") => """{"hesaplar":[]}""",
                var y when y.EndsWith("/gider-kategorileri") => """{"kategoriler":[{"id":"gk1","ad":"Kira","aciklama":null,"aktif":true,"giderAdedi":0,"giderToplamTl":0}],"toplam":1}""",
                var y when y.EndsWith("/giderler") => """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":"En çok 100 gider satırı gösterilir."}""",
                var y when y.EndsWith("/virman") => """{"satirlar":[],"toplam":0,"limit":50,"dahaFazlasiVar":false,"sinirNotu":"En son 50 virman gösterilir."}""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur1","hasAlis":4100,"hasSatis":4150}""",
                var y when y.Contains("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                _ => "{}",
            }));
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Gider> Ciz(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Kuyumcu.Web.Pages.Gider>().AyrintiAc("#yildiz-gider-ekle");
        e.WaitForElement("#gd-kaydet");
        return e;
    }

    [Theory]
    [InlineData("BASIT")]
    [InlineData("PROFESYONEL")]
    public void PD3E_N_gider_once_cekmecedeki_nakit_sonra_toplam(string mod)
    {
        var e = Ciz(mod);
        var nakit = e.WaitForElement("#gd-kasa-nakit").TextContent;
        Assert.Contains(ParaDemo3Sozlugu.Tr.GiderNakit, nakit);
        Assert.Contains(Bicim.Para(195_625.82m, "TRY"), nakit);
        Assert.DoesNotContain(Bicim.Para(377_738.62m, "TRY"), nakit);
        var toplam = e.Find("#gd-kasa-bakiye").TextContent;
        Assert.Contains(ParaDemo3Sozlugu.Tr.GiderToplam, toplam);
        Assert.Contains(Bicim.Para(377_738.62m, "TRY"), toplam);
        Assert.DoesNotContain(Sozlukler.Tr.Gider.KasaBakiyesi + ":", toplam);
        // Sıra: nakit satırı toplamdan ÖNCE.
        var html = e.Markup;
        Assert.True(html.IndexOf("gd-kasa-nakit", StringComparison.Ordinal) < html.IndexOf("gd-kasa-bakiye", StringComparison.Ordinal));
        Assert.Empty(e.FindAll("#gd-kasa-ayrim-hata"));
    }

    [Fact]
    public void PD3E_N_ayrim_okunamazsa_toplam_NAKIT_diye_basilmaz_kisa_hata()
    {
        ayrimHatali = true;
        var e = Ciz("BASIT");
        e.WaitForElement("#gd-kasa-ayrim-hata");
        Assert.Empty(e.FindAll("#gd-kasa-nakit"));
        var toplam = e.Find("#gd-kasa-bakiye").TextContent;
        Assert.Contains(Sozlukler.Tr.Gider.KasaBakiyesi, toplam);
        Assert.Contains(Bicim.Para(377_738.62m, "TRY"), toplam);
        Assert.DoesNotContain(ParaDemo3Sozlugu.Tr.GiderNakit, e.Markup);
    }
}

// ── (G) Gün sonu faturasız tablosu: "Kart + Havale" (ekstreyle aynı dil) ─────
public sealed class ParaDemo3RaporEkranTestleri : BunitContext
{
    public ParaDemo3RaporEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            return Task.FromResult(ParaDemoSahte.Tamam(yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/aylik") => """{"yil":2026,"ay":10,"gunler":[]}""",
                var y when y.EndsWith("/raporlar/gunluk") => """{"tarih":"2026-10-02","satis":{"adet":2,"toplamTl":64190.71,"toplamHas":1.2},"alis":{"adet":0,"toplamTl":0,"toplamHas":0},"hurda":{"toplamHas":0,"odenenTl":0},"netHasGramHareketi":-1.2,"hareketSayisi":2,"kasalar":[{"id":"k1","ad":"Ana Kasa","bakiye":64190.71,"paraBirimi":"TRY"}]}""",
                var y when y.EndsWith("/kasalar/nakit-ayrimi") =>
                    """[{"kasaId":"k1","paraBirimi":"TRY","toplam":64190.71,"nakit":0,"kart":40000,"havale":24190.71}]""",
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") => """
                    {"tarih":"2026-10-02",
                     "tahsilat":{"nakit":0,"kart":40000.00,"havale":24190.71,"diger":0,"toplam":64190.71,"virmanGiris":0,"acilisDevri":0},
                     "satis":{"sayisi":2,"toplamTl":64190.71,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":2,"faturasizTl":64190.71},
                     "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
                     "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
                     "fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},
                     "fark":64190.71,"mutabik":false,"uyarilar":[],
                     "faturasizIslemler":[
                        {"id":"h1","tarih":"2026-10-02T09:00:00Z","tlTutari":63190.71,"odemeTuru":"KARMA","cariId":"c1","odemeAraclari":["KART","HAVALE"]},
                        {"id":"h2","tarih":"2026-10-02T09:10:00Z","tlTutari":1000.00,"odemeTuru":"HAVALE","cariId":"c1","odemeAraclari":null}],
                     "faturaEslesme":null}
                    """,
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                var y when y.EndsWith("/pozisyon") => """{"anaMaden":"ALTIN","altin":{"maden":"ALTIN","vitrinGram":10,"hurdaGram":0,"cariAlacakGram":0,"cariBorcGram":0,"netGram":10,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},"digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,"hesaplanmaAni":"2026-10-02T09:00:00Z","donemdenBagimsiz":true}""",
                var y when y.Contains("/bildirimler") => """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""",
                _ => "{}",
            }));
        }
    }

    [Fact]
    public void PD3E_G_PRO_gun_sonu_faturasiz_karma_satiri_Kart_arti_Havale_Karisik_YOK()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForElement("#rp-sekme-gunsonu").Click();
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll(".gs-faturasiz-odeme").Count));
        var hucreler = e.FindAll(".gs-faturasiz-odeme").Select(h => h.TextContent.Trim()).ToList();
        Assert.Equal(["Kart + Havale", "Havale"], hucreler);
        Assert.DoesNotContain(KatalogSozlugu.OdemeAraci("KARMA", "tr"), hucreler);
    }
}

// ── (T) Değişim kilidi: eksik alanların TAMAMI tek cümlede; BASİT'te "MASAK" yok ──
public sealed class ParaDemo3TakasKilitTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public ParaDemo3TakasKilitTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            return ParaDemoSahte.Tamam(yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.EndsWith("/cari") => """
                    [{"id":"c1","ad":"Tuna Akarsu","tur":"MUSTERI","telefon":null,"email":null,"tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"riskSeviye":null,"pepBeyani":false,"sistemCarisi":false,"aktif":true},
                     {"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","telefon":null,"email":null,"tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"riskSeviye":null,"pepBeyani":false,"sistemCarisi":true,"aktif":true}]
                    """,
                var v when v.Contains("/tevsik/gunluk") => """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":3.206,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                // Hakem senaryosu: kimlik + amaç beyanı kademesi (işlem amacı + beyan açıklaması zorunlu).
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"AMAC","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/islemler/altin-bacagi/ayarlar") => """{"stopajOranlari":[{"saticiTipi":"NIHAI_TUKETICI","oran":0,"pusulaGerekir":true},{"saticiTipi":"ESNAF_MUAF","oran":0.02,"pusulaGerekir":true},{"saticiTipi":"MUKELLEF","oran":0,"pusulaGerekir":false}],"alisIskontoOran":null,"saticiAdresiZorunlu":true}""",
                var v when v.Contains("/islemler/takas") => """{"id":"h9","fisId":"f9"}""",
                var v when v.Contains("/reyonlar") || v.Contains("/islemler/son") || v.Contains("/pos/cihazlar") || v.Contains("/islemler/personeller") || v.Contains("/iade-adaylari") => "[]",
                _ => "{}",
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Takas(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Sahip", "t@ornek.com", "SAHIP", "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#islem-tur-SATIS");
        e.Find("#islem-tur-TAKAS").Click();
        e.WaitForElement("#takas-satici");
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.Find("#takas-eski-gram").Input("30");
        SeciciYardim.Sec(e, "takas-yeni-urun", "u1");
        e.Find("#takas-yeni-fiyat").Input("10000");
        SeciciYardim.Sec(e, "takas-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#masak-amac");
        return e;
    }

    private static string Kilit(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) => e.Find("#islem-kaydet-kilit").TextContent.Trim();

    [Fact]
    public void PD3E_T_BASIT_degisim_kilidi_tum_eksik_alanlari_sayar_MASAK_kelimesi_yok()
    {
        var e = Takas(ArayuzModuServisi.Basit);
        var p = ParaDemo3Sozlugu.Tr;
        var beyan = string.Format(p.AlanBeyanAciklama, 20);
        // TOPLU-5 (hakem DÜŞÜK "kilit 'TCKN' ↔ alan 'Belge No'"): kilit, kimlik kutusundaki alanın ETİKETİYLE yazar.
        var tc = ZoIslemSozlugu.Tr.MasakBelgeNoTc;
        Assert.Equal(tc, e.Find("#masak-belge-no-sar .alan-etiket-metin").TextContent.Trim());
        e.WaitForAssertion(() => Assert.Equal(string.Format(p.TakasEksikBasit, $"{tc}, adres, {p.AlanIslemAmaci}, {beyan}"), Kilit(e)));
        Assert.DoesNotContain("MASAK", Kilit(e));
        Assert.DoesNotContain("gorunmez", e.Find("#islem-kaydet-kilit").ClassName ?? "", StringComparison.Ordinal);
        Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled"));

        // TCKN + adres girilince genel jargon cümlesine DÖNMEZ: kalan alanlar adıyla.
        e.Find("#masak-belge-no").Input("10000000146");
        e.Find("#masak-adres").Input("Çarşı Mah. 1");
        e.WaitForAssertion(() => Assert.Equal(string.Format(p.TakasEksikBasit, $"{p.AlanIslemAmaci}, {beyan}"), Kilit(e)));
        Assert.NotEqual(Sozlukler.Tr.Islem.MasakKimlikEksik, Kilit(e));
        Assert.DoesNotContain("MASAK", Kilit(e));

        e.Find("#masak-amac").Input("Takı yenileme");
        e.WaitForAssertion(() => Assert.Equal(string.Format(p.TakasEksikBasit, beyan), Kilit(e)));
        e.Find("#masak-aciklama").Input("Eski bileziği yenisiyle değiştiriyor");
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled")));
        e.Find("#islem-kaydet").Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/takas")));
        var g = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/takas")).Govde).RootElement;
        Assert.Equal("Takı yenileme", g.GetProperty("masak").GetProperty("islemAmaci").GetString());
        Assert.Equal("10000000146", g.GetProperty("satici").GetProperty("tckn").GetString());
    }

    [Fact]
    public void PD3E_T_PRO_degisim_kilidi_tek_cumle_iki_gerekce()
    {
        var e = Takas(ArayuzModuServisi.Profesyonel);
        var p = ParaDemo3Sozlugu.Tr;
        var beyan = string.Format(p.AlanBeyanAciklama, 20);
        var ikiGerekce = string.Format(p.GerekceBirlestir, p.GerekceGiderPusulasi, p.GerekceKimlikTespiti);
        var belgeNo = ZoIslemSozlugu.Tr.MasakBelgeNoTc;   // YASAL-PARAM-EKRAN tur 2: PRO alan etiketi BASİT ile aynı (TCKN kısaltmasız)
        e.WaitForAssertion(() => Assert.Equal(string.Format(p.TakasEksikPro, $"{belgeNo}, adres, {p.AlanIslemAmaci}, {beyan}", ikiGerekce), Kilit(e)));
        e.Find("#masak-belge-no").Input("10000000146");
        e.Find("#masak-adres").Input("Çarşı Mah. 1");
        e.WaitForAssertion(() => Assert.Equal(string.Format(p.TakasEksikPro, $"{p.AlanIslemAmaci}, {beyan}", p.GerekceKimlikTespiti), Kilit(e)));
    }
}

// ── Tur 2 (doğrulayıcı + tasarım hakemi): Gider kilidi nakde bağlı · bant eylemleri ayrı satır · virman reddi formda ──
public sealed class ParaDemo3Tur2GiderTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public ParaDemo3Tur2GiderTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    /// <summary>Sunucunun GERÇEK virman (projeksiyon) toplam kapısı cümlesi.</summary>
    private static string VirmanRedCumlesi()
    {
        var ex = Assert.Throws<BadRequestException>(() => Kuyumcu.Api.Modules.Kasa.KasaBakiyeKapisi.Dogrula("Ana Kasa", 221_726.43m, "TRY", -2_000_000m));
        return ex.Message;
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var metot = istek.Method.Method;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((metot, yol, govde));
            if (metot == "POST" && yol.EndsWith("/virman"))
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { message = VirmanRedCumlesi(), kod = "KASA_BAKIYE_YETERSIZ", statusCode = 400 }),
                        System.Text.Encoding.UTF8, "application/json"),
                };
            return ParaDemoSahte.Tamam(yol switch
            {
                var y when y.EndsWith("/kasalar/nakit-ayrimi") =>
                    """[{"kasaId":"k1","paraBirimi":"TRY","toplam":221726.43,"nakit":193830.90,"kart":25895.53,"havale":2000}]""",
                var y when y.EndsWith("/kasalar") => """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":221726.43,"aktif":true}]""",
                var y when y.EndsWith("/bankalar") =>
                    """{"hesaplar":[{"id":"bh1","bankaAdi":"Ziraat","hesapAdi":"Vadesiz","hesapSahibi":null,"iban":"TR11","paraBirimi":"TRY","bakiye":5000,"aktif":true}]}""",
                var y when y.EndsWith("/gider-kategorileri") => """{"kategoriler":[{"id":"gk1","ad":"Kira","aciklama":null,"aktif":true,"giderAdedi":0,"giderToplamTl":0}],"toplam":1}""",
                var y when y.EndsWith("/giderler") => """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":"En çok 100 gider satırı gösterilir."}""",
                var y when y.EndsWith("/virman") => """{"satirlar":[],"toplam":0,"limit":50,"dahaFazlasiVar":false,"sinirNotu":"En son 50 virman gösterilir."}""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur1","hasAlis":4100,"hasSatis":4150}""",
                var y when y.Contains("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                _ => "{}",
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Gider> Ciz(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Kuyumcu.Web.Pages.Gider>().AyrintiAc("#yildiz-gider-ekle");
        e.WaitForElement("#gd-kasa-nakit");
        return e;
    }

    /// <summary>Doğrulayıcı DÜŞÜK (UX-S30): tutar nakdi aşıp toplamın altında kalınca Kaydet KİLİTLİ, sebep nakdi söyler.</summary>
    [Theory]
    [InlineData("BASIT")]
    [InlineData("PROFESYONEL")]
    public void PD3T2_gider_kilidi_cekmecedeki_nakde_bagli(string mod)
    {
        var e = Ciz(mod);
        e.Find("#gd-tutar").Input("150000");
        Assert.False(e.Find("#gd-kaydet").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#gd-yetersiz"));

        e.Find("#gd-tutar").Input("200000");   // 193.830,90 < 200.000 < 221.726,43
        Assert.True(e.Find("#gd-kaydet").HasAttribute("disabled"));
        // TOPLU-6 (TOPLU-5 hakemi ORTA): kilit tek cümle — çekmecedeki nakit + gider tutarı (kural AYNI).
        Assert.Equal(string.Format(Toplu6Sozlugu.Tr.GiderNakitYetmiyor, Bicim.Para(193_830.90m, "TRY"), Bicim.Para(200_000m, "TRY")), e.Find("#gd-kaydet-kilit").TextContent.Trim());
        var bant = e.Find("#gd-yetersiz-metin").TextContent.Trim();
        // TOPLU-5 (hakem ORTA "aynı bilgi iki kez"): nakit sayısı kilitte; bant yalnız çekmecede OLMAYAN payı söyler.
        Assert.Equal(Kuyumcu.Web.Pages.Gider.NakitBantMetni(Toplu5Sozlugu.Tr, 25_895.53m, 2_000m, "TRY"), bant);
        Assert.DoesNotContain("193.830,90", bant);
        Assert.DoesNotContain("221.726,43", bant);   // toplam "kasada var" denmez
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    /// <summary>Tasarım hakemi KRİTİK: bant eylemleri metnin altında ayrı satırda, ikincil küçük düğme görünümünde.</summary>
    [Fact]
    public void PD3T2_yetersiz_bandi_eylemleri_ayri_satirda_ikincil_kucuk_dugme()
    {
        var e = Ciz("BASIT");
        e.Find("#gd-tutar").Input("400000");
        // Toplamı da aşan tutarda kilit yine ÇEKMECEDEKİ NAKDİ söyler ("kasada 221.726,43 ₺ var" değil).
        // TOPLU-6: kilit tek cümle (nakit + tutar); tutar kasa TOPLAMINI da aşıyor → kart payı yardım etmez,
        // bant yalnız çıkış yolunu söyler (kart payı cümlesi YOK).
        Assert.Equal(string.Format(Toplu6Sozlugu.Tr.GiderNakitYetmiyor, Bicim.Para(193_830.90m, "TRY"), Bicim.Para(400_000m, "TRY")), e.Find("#gd-kaydet-kilit").TextContent.Trim());
        var metin = e.Find("#gd-yetersiz-metin");
        Assert.Equal(Toplu6Sozlugu.Tr.GiderCikisYolu, metin.TextContent.Trim());
        Assert.DoesNotContain(Sozlukler.Tr.Gider.KasayaGirisYap, metin.TextContent);
        Assert.DoesNotContain(Sozlukler.Tr.Gider.VirmanYapBaglanti, metin.TextContent);
        var eylemler = e.Find("#gd-yetersiz #gd-yetersiz-eylemler");
        var baglantilar = eylemler.QuerySelectorAll("a").ToList();
        Assert.Equal(new string?[] { "gd-yetersiz-giris", "gd-yetersiz-virman" }, baglantilar.Select(a => a.Id).ToArray());
        foreach (var a in baglantilar)
        {
            Assert.Contains("dugme", a.ClassList);
            Assert.Contains("dugme-ikincil", a.ClassList);
            Assert.Contains("dugme-kucuk", a.ClassList);
        }
        Assert.Equal("/kasalar?kasa=k1&yon=giris", baglantilar[0].GetAttribute("href"));
        Assert.Equal("/gider?sekme=virman&hedef=k1", baglantilar[1].GetAttribute("href"));
        Assert.Equal(Sozlukler.Tr.Gider.KasayaGirisYap, baglantilar[0].TextContent.Trim());
    }

    /// <summary>
    /// Tasarım hakemi KRİTİK: virman reddi sayfanın en üstüne (#gd-hata) DEĞİL, "Virmanı kaydet"in hemen altına yazılır;
    /// nakit kaynağında kart/havale payı varsa nakit cümlesiyle. Tr ve En.
    /// </summary>
    [Theory]
    [InlineData("tr")]
    [InlineData("en")]
    public async Task PD3T2_virman_reddi_dugmenin_altinda_formun_icinde(string dil)
    {
        if (dil == "en") await Services.GetRequiredService<DilServisi>().DegistirAsync("en");
        var e = Ciz("BASIT");
        e.Find("#gd-sekme-virman").Click();
        SeciciYardim.Sec(e, "gd-v-kaynak", "KASA:k1");
        SeciciYardim.Sec(e, "gd-v-hedef", "BANKA:bh1");
        e.Find("#gd-v-tutar").Input("2000000");
        e.Find("#gd-v-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/virman")));
        var bant = e.WaitForElement("#gd-v-hata").TextContent.Trim();
        Assert.Empty(e.FindAll("#gd-hata"));
        var html = e.Markup;
        Assert.True(html.IndexOf("id=\"gd-v-kaydet\"", StringComparison.Ordinal) < html.IndexOf("id=\"gd-v-hata\"", StringComparison.Ordinal));
        Assert.Equal(string.Format(ParaDemo3Sozlugu.Al(dil).NakitYetersiz, "Ana Kasa", Bicim.Para(193_830.90m, "TRY"),
            Bicim.Para(2_000_000m, "TRY"), Bicim.Para(25_895.53m, "TRY"), Bicim.Para(2_000m, "TRY")), bant);
        Assert.DoesNotContain("TRY", bant);
        Assert.DoesNotContain("221.726,43", bant);
    }
}

// ── Tur 2 (doğrulayıcı ORTA): gün sonu ödeme adı = ekstre ödeme adı, TR ve EN, birebir (ekrandan ekrana) ──
public sealed class ParaDemo3OdemeAdiTutarlilikTestleri : BunitContext
{
    public ParaDemo3OdemeAdiTutarlilikTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    // Aynı beş ödeme biçimi iki ekranda: gerçek karma (bacaklı) · ESKİ karma (bacaksız) · havale · nakit · kart.
    private static readonly (string Id, string Tur, string? Araclar)[] Satirlar =
    [
        ("h1", "KARMA", "[\"KART\",\"HAVALE\"]"), ("h2", "KARMA", null), ("h3", "HAVALE", null), ("h4", "NAKIT", null), ("h5", "KART", null),
    ];

    private static string Araclar((string Id, string Tur, string? Araclar) s) => s.Araclar ?? "null";

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var ekstre = string.Join(",", Satirlar.Select((s, i) =>
                $$"""{"id":"{{s.Id}}","tur":"SATIS","tlTutari":{{1000 + i}},"hasGrami":0,"aciklama":null,"olusturuldu":"2026-10-02T10:0{{i}}:00Z","odemeTuru":"{{s.Tur}}","odemeAraclari":{{Araclar(s)}},"cariTlEtkisi":0,"cariHasEtkisi":0,"bakiyeHasEtkisi":0}"""));
            var faturasiz = string.Join(",", Satirlar.Select((s, i) =>
                $$"""{"id":"{{s.Id}}","tarih":"2026-10-02T09:0{{i}}:00Z","tlTutari":{{1000 + i}},"odemeTuru":"{{s.Tur}}","cariId":"c1","odemeAraclari":{{Araclar(s)}}}"""));
            return Task.FromResult(ParaDemoSahte.Tamam(yol switch
            {
                var y when y.EndsWith("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"gbpTry":null}""",
                var y when y.Contains("/ekstre") => $$"""
                    {"bakiye":{"cariId":"c1","tlBakiye":0,"hasGramBakiye":0},"hareketler":[{{ekstre}}],
                     "sayfaDisiHasEtkisi":0,"dahaFazlaVar":false,"limit":100,"bakiyeUyusmazligiHas":0,
                     "bakiyeEmanetsizHas":0,"emanetteHas":0,"bakiyeUyusmazligiTl":0}
                    """,
                var y when y.EndsWith("/cari/c1") => """{"id":"c1","ad":"Kemal Ardıç","tur":"MUSTERI","telefon":null,"email":null,"vergiNo":null,"adres":null,"pepBeyani":false}""",
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Kemal Ardıç","tur":"MUSTERI","telefon":null,"email":null,"tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"riskSeviye":null,"pepBeyani":false,"aktif":true}]""",
                var y when y.EndsWith("/kasalar") => """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":0,"aktif":true}]""",
                var y when y.Contains("/tevsik/gunluk") => """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var y when y.Contains("/fiyat/listeler") => "[]",
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/aylik") => """{"yil":2026,"ay":10,"gunler":[]}""",
                var y when y.EndsWith("/raporlar/gunluk") => """{"tarih":"2026-10-02","satis":{"adet":5,"toplamTl":5010,"toplamHas":0},"alis":{"adet":0,"toplamTl":0,"toplamHas":0},"hurda":{"toplamHas":0,"odenenTl":0},"netHasGramHareketi":0,"hareketSayisi":5,"kasalar":[{"id":"k1","ad":"Ana Kasa","bakiye":5010,"paraBirimi":"TRY"}]}""",
                var y when y.EndsWith("/kasalar/nakit-ayrimi") => """[{"kasaId":"k1","paraBirimi":"TRY","toplam":5010,"nakit":1003,"kart":1004,"havale":1002}]""",
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") => $$"""
                    {"tarih":"2026-10-02",
                     "tahsilat":{"nakit":1003,"kart":1004,"havale":1002,"diger":0,"toplam":5010,"virmanGiris":0,"acilisDevri":0},
                     "satis":{"sayisi":5,"toplamTl":5010,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":5,"faturasizTl":5010},
                     "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
                     "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
                     "fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},
                     "fark":5010,"mutabik":false,"uyarilar":[],
                     "faturasizIslemler":[{{faturasiz}}],
                     "faturaEslesme":null}
                    """,
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                var y when y.EndsWith("/pozisyon") => """{"anaMaden":"ALTIN","altin":{"maden":"ALTIN","vitrinGram":10,"hurdaGram":0,"cariAlacakGram":0,"cariBorcGram":0,"netGram":10,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},"digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,"hesaplanmaAni":"2026-10-02T09:00:00Z","donemdenBagimsiz":true}""",
                var y when y.Contains("/bildirimler") => """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""",
                _ => "{}",
            }));
        }
    }

    [Theory]
    [InlineData("tr")]
    [InlineData("en")]
    public async Task PD3T2_gun_sonu_ve_ekstre_odeme_adi_birebir_ayni(string dil)
    {
        if (dil == "en") await Services.GetRequiredService<DilServisi>().DegistirAsync("en");
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel));

        // Ekstre (Cariler › detay): her satırın ödeme adı ("· " önekiyle basılır).
        // TEMA TEMİZLİĞİ 17.10: Yıldız Müşteriler listesi — eylemler müşterinin kartında (CariKartEylemi).
        var cari = Render<Kuyumcu.Web.Pages.Cariler>();
        cari.CariKartEylemi("c1", "c-detay");
        cari.WaitForElement("#cari-detay-odeme-h5");
        var ekstreAdlari = Satirlar.Select(s => cari.Find($"#cari-detay-odeme-{s.Id}").TextContent.Trim().TrimStart('·').Trim()).ToArray();

        // Gün sonu (Raporlar › Gün sonu › faturasız işlemler).
        var rapor = Render<Kuyumcu.Web.Pages.Raporlar>();
        rapor.WaitForElement("#rp-sekme-gunsonu").Click();
        rapor.WaitForAssertion(() => Assert.Equal(Satirlar.Length, rapor.FindAll(".gs-faturasiz-odeme").Count));
        var gunSonuAdlari = rapor.FindAll(".gs-faturasiz-odeme").Select(h => h.TextContent.Trim()).ToArray();

        Assert.Equal(ekstreAdlari, gunSonuAdlari);   // BİREBİR, beş biçimin beşi
        // Sabit, elle yazılmış beklenen (sözlükten hesaplanmaz — iki sözlük ayrışırsa test kırmızı olur).
        string[] beklenen = dil == "en"
            ? ["Card + Transfer", "Mixed payment", "Transfer", "Cash", "Card"]
            : ["Kart + Havale", "Karma ödeme", "Havale", "Nakit", "Kart"];
        Assert.Equal(beklenen, gunSonuAdlari);
        Assert.DoesNotContain("Karışık", gunSonuAdlari);
        Assert.DoesNotContain("Bank transfer", string.Join("|", gunSonuAdlari));
    }
}
