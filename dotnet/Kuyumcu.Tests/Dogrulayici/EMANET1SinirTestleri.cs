using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET EMANET-1 — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT LİTERALDİR (üretim
// kodundan hesaplanmaz). Paket, seçilen müşterinin emanet ÖZETİNİ (#e-ozet-deger:
// has gram, 4 hane) ve iade kilidini (#e-cikis-ac + #e-kilit) yeniden yazdı; bu
// dosya o gösterim yolunun sınırlarını sınar: sıfır · eksi · 0,0001 · yuvarlama
// sınırı · çok büyük · null maden listesi · yalnız gümüş emanet · Türkçe giriş.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class EMANET1SinirTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string? girisYaniti;
    private string emanetGovdesi = """{"cariId":"c1","mevcutHasGramBakiye":0,"madenBakiyeleri":[],"hareketler":[]}""";

    private sealed class SahteApi(EMANET1SinirTestleri s) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (s.istekler) s.istekler.Add((istek.Method.Method, yol, govde));
            var g = yol switch
            {
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800}""",
                // Doğrulayıcı 28.09 (Q turu): giriş ucunun sunucu yanıtı yalnız test ayarlarsa değişir.
                var y when s.girisYaniti is not null && y.EndsWith("/emanet/giris", StringComparison.Ordinal) => s.girisYaniti,
                var y when y.Contains("/emanet/c1") => s.emanetGovdesi,
                // TEMA TEMİZLİĞİ 17.10: Yıldız'da özgün gövdeye geçiş listede bir emanet satırıdır → kapı müşterisi c0.
                var y when y.Contains("/emanet/c0") => TemaTemizligiGEYardimcisi.KapiEmanet,
                var y when y.Contains("/cari") => "[" + TemaTemizligiGEYardimcisi.KapiCari + """,{"id":"c1","ad":"Sınır Müşteri"}]""",
                _ => """{"hata":"tanimsiz uc"}""",
            };
            var kod = g.StartsWith("""{"hata""", StringComparison.Ordinal) ? HttpStatusCode.NotFound
                : g.StartsWith("""{"message""", StringComparison.Ordinal) ? HttpStatusCode.BadRequest : HttpStatusCode.OK;
            return new HttpResponseMessage(kod)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    public EMANET1SinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Emanet> Sec(string govde)
    {
        emanetGovdesi = govde;
        // TEMA TEMİZLİĞİ 17.10: Yıldız yolu — listede satıra bas (özgün gövde), sonra seçiciden sınanan müşteriyi seç.
        var e = Render<Kuyumcu.Web.Pages.Emanet>().EmanetSatiriAc();
        e.WaitForAssertion(() => Assert.DoesNotContain(OnayKilitSozlugu.Tr.EmanetKurYok, e.Markup, StringComparison.Ordinal));
        SeciciYardim.Sec(e, "e-cari", "c1");
        e.WaitForElement("#e-ozet-deger");
        e.WaitForElement("#e-gecmis");
        return e;
    }

    private static string Ozet(IRenderedComponent<Kuyumcu.Web.Pages.Emanet> e) =>
        e.Find("#e-ozet-deger").TextContent.Trim();

    // ── sıfır ──────────────────────────────────────────────────────────────
    [Fact]
    public void S1_Sifir_bakiye_Emaneti_yok_yazar_0_0000_yazmaz_iade_kilitli()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":0,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":0}],"hareketler":[]}""");
        Assert.Equal("Emaneti yok", Ozet(e));
        Assert.DoesNotContain("0,0000", e.Markup, StringComparison.Ordinal);
        Assert.True(e.Find("#e-cikis-ac").HasAttribute("disabled"));
        Assert.Equal("Bu müşterinin emaneti yok.", e.Find("#e-kilit").TextContent.Trim());
    }

    // ── eksi (sunucu üretmemeli; üretirse işaret korunur ve iade kilitli) ──
    [Fact]
    public void S2_Eksi_bakiye_isareti_korunur_ve_iade_kilitli()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":-1.5,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":-1.5}],"hareketler":[]}""");
        Assert.Equal("-1,5000 has", Ozet(e));
        Assert.True(e.Find("#e-cikis-ac").HasAttribute("disabled"));
    }

    // ── has 4 hane: en küçük birim ─────────────────────────────────────────
    [Fact]
    // 27.09 (doğrulayıcı ORTA bulgusu): sunucu iadede HasGrami ≥ 0,001 ister
    // (EmanetModulu.cs [Range(0.001, …)]) → 0,0001 YAZILIR ama iade KİLİTLİ,
    // sebep tek kısa cümle.
    public void S3_En_kucuk_birim_0_0001_dort_hane_yazilir_iade_alt_sinir_altinda_kilitli()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":0.0001,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":0.0001}],"hareketler":[]}""");
        Assert.Equal("0,0001 has", Ozet(e));
        Assert.True(e.Find("#e-cikis-ac").HasAttribute("disabled"));
        Assert.Equal("Kalan miktar iade için çok küçük.", e.Find("#e-kilit").TextContent.Trim());
    }

    // ── yuvarlama sınırı (F4: yarım uzağa) ─────────────────────────────────
    [Fact]
    public void S4_Has_bicimi_yuvarlama_siniri_0_00005_yukari_0_00004_asagi()
    {
        Assert.Equal("0,0001 has", Bicim.Has(0.00005m));
        Assert.Equal("0,0000 has", Bicim.Has(0.00004m));
        Assert.Equal("9,1600 has", Bicim.Has(9.16m));
    }

    // ── sıfır olmayan ama 4 haneye yuvarlanınca 0 olan bakiye ──────────────
    // Kapsam: "0,0000 has YAZMA". Sunucu has_grami 6 hane tutar (0.000040 olası).
    [Fact]
    public void S5_Dort_haneye_sifira_yuvarlanan_bakiye_0_0000_has_yazmaz()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":0.00004,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":0.00004}],"hareketler":[]}""");
        Assert.DoesNotContain("0,0000", Ozet(e), StringComparison.Ordinal);
    }

    // ── çok büyük ──────────────────────────────────────────────────────────
    // Ortak Bicim.Has sözleşmesi "F4" (binlik ayraç YOK; EMANET-1'in değil BICIM
    // şeridinin alanı). Burada sınanan: büyük değer KIRPILMADAN, 4 haneyle ve
    // tr-TR ondalık virgülle yazılır.
    [Fact]
    public void S6_Cok_buyuk_bakiye_kirpilmadan_dort_haneyle_yazilir()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":999999999.99,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":999999999.99}],"hareketler":[]}""");
        Assert.Equal("999.999.999,9900 has", Ozet(e));
    }

    // ── boş/null maden listesi: altın bakiyesine düşülür ───────────────────
    [Fact]
    public void S7_Maden_listesi_null_ise_altin_bakiyesi_yazilir()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":2.5,"madenBakiyeleri":null,"hareketler":[]}""");
        Assert.Equal("2,5000 has", Ozet(e));
        Assert.False(e.Find("#e-cikis-ac").HasAttribute("disabled"));
    }

    // ── YALNIZ GÜMÜŞ emanet (canlıda üretildi: G925 100 g → GUMUS 92,5) ────
    // Sunucu mevcutHasGramBakiye'yi YALNIZ ALTIN verir (0); madenBakiyeleri'nde
    // GUMUS 92,5 vardır. Ekran aynı anda "92,5000 has" (maden adı YOK — altın
    // has sanılır) ve "Bu müşterinin emaneti yok." yazıyor, iade kilitli.
    [Fact]
    public void S8_Yalniz_gumus_emanette_ozet_madeni_soyler_ve_emaneti_yok_DEMEZ()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":0,"madenBakiyeleri":[{"maden":"GUMUS","madenAdi":"Gümüş","gram":92.5}],"hareketler":[]}""");
        var ozet = Ozet(e);
        Assert.Contains("Gümüş", ozet, StringComparison.Ordinal);
        var kilit = e.FindAll("#e-kilit").Select(k => k.TextContent.Trim()).FirstOrDefault();
        Assert.NotEqual("Bu müşterinin emaneti yok.", kilit);
    }

    // ── Türkçe giriş (FormAlani.SayiCoz) ───────────────────────────────────
    [Theory]
    [InlineData("45.000", 45000)]
    [InlineData("1,5", 1.5)]
    [InlineData("0,0001", 0.0001)]
    [InlineData("999.999.999,99", 999999999.99)]
    public void S9_Turkce_sayi_girisi_cozulur(string ham, double beklenen)
    {
        var s = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, s.Sonuc);
        Assert.Equal((decimal)beklenen, s.Deger);
    }

    [Fact]
    public void S10_Belirsiz_1_500_reddedilir_bos_null_doner()
    {
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
    }

    // ── giriş çekmecesi: sıfır gram POST ETMEZ, alan altı mesaj verir ──────
    [Fact]
    public void S11_Giris_cekmecesi_sifir_gramda_istek_atmaz_mesaj_verir()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":0,"madenBakiyeleri":[],"hareketler":[]}""");
        e.Find("#e-giris-ac").Click();
        e.WaitForElement("#e-giris-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.Contains("Sıfırdan büyük", e.Find("#cekmece-emanet-giris").TextContent, StringComparison.Ordinal);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // 2. TUR HASIM DOĞRULAYICI (27.09) — ek sınırlar. Beklenenler SABİT literal.
    // ═════════════════════════════════════════════════════════════════════════

    // ── yuvarlama sınırı 0,00005: özet ile iade kilidi ÇELİŞMEZ ────────────
    // SifirMi Math.Round(…, 4) (ToEven) kullanır, Bicim.Has "F4" (yarım uzağa).
    // Sınırda ekran "0,0000 has" YAZMAMALI ve "Emaneti yok" ile AÇIK iade
    // düğmesi aynı anda görünmemeli.
    [Fact]
    public void S12_Yarim_sinir_0_00005_sifir_has_yazmaz_ozet_ve_kilit_tutarli()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":0.00005,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":0.00005}],"hareketler":[]}""");
        Assert.DoesNotContain("0,0000", e.Markup, StringComparison.Ordinal);
        // 27.09: tek yuvarlama kuralı (yarım UZAĞA, Bicim.Has ile aynı) → "0,0001 has";
        // iade alt sınırın (0,001) altında → kilitli ve sebebi YAZILI (çelişki yok).
        Assert.Equal("0,0001 has", Ozet(e));
        Assert.True(e.Find("#e-cikis-ac").HasAttribute("disabled"));
        Assert.Equal("Kalan miktar iade için çok küçük.", e.Find("#e-kilit").TextContent.Trim());
    }

    // ── çok büyük EKSİ: işaret korunur, kırpılmaz, iade kilitli ────────────
    [Fact]
    public void S13_Cok_buyuk_eksi_bakiye_isaretiyle_yazilir_iade_kilitli()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":-999999999.99,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":-999999999.99}],"hareketler":[]}""");
        Assert.Equal("-999.999.999,9900 has", Ozet(e));
        Assert.True(e.Find("#e-cikis-ac").HasAttribute("disabled"));
    }

    // ── çok madenli: sıfıra yuvarlanan üçüncü maden özete "0,0000" sızdırmaz ─
    [Fact]
    public void S14_Cok_maden_ozeti_sifir_madeni_atlar_digerlerini_ayri_yazar()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":1.25,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":1.25},{"maden":"GUMUS","madenAdi":"Gümüş","gram":2},{"maden":"PLATIN","madenAdi":"Platin","gram":0.00004}],"hareketler":[]}""");
        Assert.Equal("Altın 1,2500 has · Gümüş 2,0000 has", Ozet(e));
        Assert.DoesNotContain("0,0000", e.Markup, StringComparison.Ordinal);
        Assert.False(e.Find("#e-cikis-ac").HasAttribute("disabled"));
    }

    // ── iade gövdesi: 6 haneli bakiye KIRPILMADAN ve kültürsüz (nokta) gider ─
    [Fact]
    public void S15_Iade_varsayilani_alti_haneli_bakiyeyi_noktali_JSON_ile_gonderir()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":1.234567,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":1.234567}],"hareketler":[]}""");
        Assert.Equal("1,2346 has", Ozet(e));
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            lock (istekler)
            {
                var post = Assert.Single(istekler, i => i.Metot == "POST");
                Assert.EndsWith("/emanet/c1/cikis", post.Yol, StringComparison.Ordinal);
                using var j = System.Text.Json.JsonDocument.Parse(post.Govde);
                Assert.Equal(1.234567m, j.RootElement.GetProperty("hasGrami").GetDecimal());
                Assert.Equal("ALTIN", j.RootElement.GetProperty("maden").GetString());
                Assert.Equal("kur-1", j.RootElement.GetProperty("kurSnapshotId").GetString());
            }
        });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // 3. TUR HASIM DOĞRULAYICI (28.09) — çekmece girişlerinin sınırları.
    // Beklenenler SABİT literal; gövde JSON olarak AYRIŞTIRILIR.
    // ═════════════════════════════════════════════════════════════════════════

    private const string AltinIkiBucuk =
        """{"cariId":"c1","mevcutHasGramBakiye":2.5,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":2.5}],"hareketler":[]}""";

    // ── eksi gram girişi: istek YOK, alan altı Türkçe mesaj ───────────────
    [Fact]
    public void S16_Giris_eksi_gram_istek_atmaz_alan_alti_mesaj_verir()
    {
        var e = Sec(AltinIkiBucuk);
        e.Find("#e-giris-ac").Click();
        e.WaitForElement("#e-gram").Input("-5");
        e.Find("#e-giris-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.Equal("true", e.Find("#e-gram").GetAttribute("aria-invalid"));
    }

    // ── çok büyük giriş: 999.999.999,99 g KIRPILMADAN, noktalı JSON ────────
    [Fact]
    public void S17_Giris_cok_buyuk_gram_Turkce_binlikli_kirpilmadan_gider()
    {
        var e = Sec(AltinIkiBucuk);
        e.Find("#e-giris-ac").Click();
        e.WaitForElement("#e-gram").Input("999.999.999,99");
        e.Find("#e-giris-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            lock (istekler)
            {
                var post = Assert.Single(istekler, i => i.Metot == "POST");
                Assert.EndsWith("/emanet/giris", post.Yol, StringComparison.Ordinal);
                using var j = System.Text.Json.JsonDocument.Parse(post.Govde);
                Assert.Equal(999999999.99m, j.RootElement.GetProperty("gramAgirlik").GetDecimal());
                Assert.Equal("K22", j.RootElement.GetProperty("ayarId").GetString());
                Assert.Equal("c1", j.RootElement.GetProperty("cariId").GetString());
                Assert.Equal(System.Text.Json.JsonValueKind.Null, j.RootElement.GetProperty("tasAgirlik").ValueKind);
            }
        });
    }

    // ── iade: elimizdekinden fazlası kilitli, sebep yazılı, istek yok ──────
    [Fact]
    public void S18_Iade_bakiyeden_fazla_kaydet_kilitli_sebep_yazili()
    {
        var e = Sec(AltinIkiBucuk);
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-has").Input("2,5001");
        var kaydet = e.Find("#e-cikis-kaydet");
        Assert.True(kaydet.HasAttribute("disabled"));
        Assert.Equal("İade, elinizdeki emanetten fazla olamaz.", kaydet.GetAttribute("title"));
        Assert.Contains("İade, elinizdeki emanetten fazla olamaz.", e.Find("#cekmece-emanet-cikis").TextContent, StringComparison.Ordinal);
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    // ── iade: sıfır → istek yok, "Sıfırdan büyük" alan mesajı ──────────────
    [Fact]
    public void S19_Iade_sifir_istek_atmaz_mesaj_verir()
    {
        var e = Sec(AltinIkiBucuk);
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-has").Input("0");
        e.Find("#e-cikis-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.Contains("Sıfırdan büyük bir değer girin.", e.Find("#cekmece-emanet-cikis").TextContent, StringComparison.Ordinal);
    }

    // ── iade: Türkçe "1,5" → 1.5 has, maden ALTIN ─────────────────────────
    [Fact]
    public void S20_Iade_Turkce_virgullu_giris_1_5_dogru_gider()
    {
        var e = Sec(AltinIkiBucuk);
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-has").Input("1,5");
        e.Find("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            lock (istekler)
            {
                var post = Assert.Single(istekler, i => i.Metot == "POST");
                Assert.EndsWith("/emanet/c1/cikis", post.Yol, StringComparison.Ordinal);
                using var j = System.Text.Json.JsonDocument.Parse(post.Govde);
                Assert.Equal(1.5m, j.RootElement.GetProperty("hasGrami").GetDecimal());
                Assert.Equal("ALTIN", j.RootElement.GetProperty("maden").GetString());
            }
        });
    }

    // ── geçmiş tablosu: yarım sınır ±0,00005 yarım UZAĞA, eksi işaret korunur ─
    [Fact]
    public void S21_Gecmis_hucresi_yarim_sinir_uzaga_eksi_isaretli_birimsiz()
    {
        var e = Sec("""
            {"cariId":"c1","mevcutHasGramBakiye":1,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":1}],
             "hareketler":[
               {"id":"h1","tur":"EMANET_GIRIS","tarih":"2026-09-10T10:00:00Z","hasGrami":0.00005,"aciklama":null,"stornoKaynakId":null,"fisId":null,"maden":"ALTIN"},
               {"id":"h2","tur":"EMANET_GIRIS","tarih":"2026-09-11T10:00:00Z","hasGrami":-0.00005,"aciklama":null,"stornoKaynakId":"h1","fisId":null,"maden":"ALTIN"},
               {"id":"h3","tur":"EMANET_GIRIS","tarih":"2026-09-12T10:00:00Z","hasGrami":999999999.99,"aciklama":null,"stornoKaynakId":null,"fisId":null,"maden":"ALTIN"}]}
            """);
        e.Find("#e-gecmis-ac").Click();
        var hucreler = e.FindAll("#e-gecmis td.sayi").Select(h => h.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "0,0001", "-0,0001", "999.999.999,9900" }, hucreler);
    }

    // ═══ Doğrulayıcı 28.09 (t29 turu) — hasım sınır testleri ═══════════════

    // ── eksi yönde sıfıra yuvarlanan artık: "-0,0000 has" da yazılmaz ───────
    [Fact]
    public void S22_Eksi_yonde_sifira_yuvarlanan_artik_eksi_sifir_yazmaz()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":-0.00004,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":-0.00004}],"hareketler":[]}""");
        Assert.Equal("Emaneti yok", Ozet(e));
        Assert.DoesNotContain("0,0000", e.Markup, StringComparison.Ordinal);
        Assert.True(e.Find("#e-cikis-ac").HasAttribute("disabled"));
        Assert.Equal("Bu müşterinin emaneti yok.", e.Find("#e-kilit").TextContent.Trim());
    }

    // ── karma: altın artığı sıfıra yuvarlanır, gümüş dolu → özet yalnız gümüş ─
    [Fact]
    public void S23_Altin_artigi_sifir_gumus_dolu_ozet_yalniz_gumus_iade_gumusle_acilir()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":0.00004,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":0.00004},{"maden":"GUMUS","madenAdi":"Gümüş","gram":10}],"hareketler":[]}""");
        Assert.Equal("Gümüş 10,0000 has", Ozet(e));
        Assert.False(e.Find("#e-cikis-ac").HasAttribute("disabled"));
        e.Find("#e-cikis-ac").Click();
        var c = e.Find("#cekmece-emanet-cikis");
        Assert.DoesNotMatch(@"(?<![0-9])-?0,0000", c.TextContent);
        Assert.DoesNotContain("Altın:", c.TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#cekmece-emanet-cikis #c-maden"));      // tek iade madeni → seçici yok
        Assert.Equal("10", e.Find("#cekmece-emanet-cikis #c-has").GetAttribute("data-deger"));
    }

    // ── karma: altın dolu + gümüş toz artık → ikisi ayrı yazılır, seçici yalnız altın ─
    [Fact]
    public void S24_Altin_dolu_gumus_toz_ozet_ikisini_yazar_iade_secicisi_cikmaz()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":5,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":5},{"maden":"GUMUS","madenAdi":"Gümüş","gram":0.0005}],"hareketler":[]}""");
        Assert.Equal("Altın 5,0000 has · Gümüş 0,0005 has", Ozet(e));
        Assert.False(e.Find("#e-cikis-ac").HasAttribute("disabled"));
        e.Find("#e-cikis-ac").Click();
        Assert.Empty(e.FindAll("#cekmece-emanet-cikis #c-maden"));
        Assert.Equal("5", e.Find("#cekmece-emanet-cikis #c-has").GetAttribute("data-deger"));
    }

    // ── iade: 0,0009 (alt sınırın hemen altı) istek atmaz; 0,001 gider ─────
    [Fact]
    public void S25_Iade_0_0009_reddedilir_0_001_noktali_JSON_ile_gider()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":2,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":2}],"hareketler":[]}""");
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-has").Input("0,0009");
        e.Find("#e-cikis-kaydet").Click();
        Assert.Contains("İade edilecek miktar en az 0,0010 has olmalı.", e.Markup, StringComparison.Ordinal);
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        e.WaitForElement("#c-has").Input("0,001");
        e.Find("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            lock (istekler)
            {
                var post = Assert.Single(istekler, i => i.Metot == "POST");
                using var j = System.Text.Json.JsonDocument.Parse(post.Govde);
                Assert.Equal(0.001m, j.RootElement.GetProperty("hasGrami").GetDecimal());
            }
        });
    }

    // ═══ Doğrulayıcı 28.09 (t30 turu) — hasım sınır testleri ═══════════════

    // ── iade: belirsiz "1.500" istek ATMAZ, alan altı Türkçe mesaj ─────────
    [Fact]
    public void V1_Iade_belirsiz_1_500_istek_atmaz_alan_alti_mesaj()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":5000,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":5000}],"hareketler":[]}""");
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-has").Input("1.500");
        e.Find("#e-cikis-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST"); // 28.09 t30: varsayılan bakiye (5000) gidiyordu
        Assert.Contains("Ondalık için virgül kullanın: 1,5 ya da 1500", e.Markup, StringComparison.Ordinal);
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    // ── iade: Türkçe binlikli çok büyük has kırpılmadan noktalı JSON ile gider ─
    [Fact]
    public void V2_Iade_Turkce_binlikli_1_234_567_89_noktali_JSON_ile_gider()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":1234569.867,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":1234569.867}],"hareketler":[]}""");
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-has").Input("1.234.567,89");
        e.Find("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            lock (istekler)
            {
                var post = Assert.Single(istekler, i => i.Metot == "POST");
                using var j = System.Text.Json.JsonDocument.Parse(post.Govde);
                Assert.Equal(1234567.89m, j.RootElement.GetProperty("hasGrami").GetDecimal());
            }
        });
    }

    // ── geçmiş: çok büyük EKSİ ters kayıt birimsiz, işaretli, 4 haneli; açıklama sade ─
    [Fact]
    public void V3_Gecmis_cok_buyuk_eksi_ters_kayit_isaretli_ve_Geri_alma_metni()
    {
        var e = Sec("""
            {"cariId":"c1","mevcutHasGramBakiye":0,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":0}],
             "hareketler":[
               {"id":"h1","tur":"EMANET_GIRIS","tarih":"2026-09-10T10:00:00Z","hasGrami":999999999.9999,"aciklama":"Kulce","stornoKaynakId":null,"fisId":null,"maden":"ALTIN"},
               {"id":"h2","tur":"EMANET_GIRIS","tarih":"2026-09-11T10:00:00Z","hasGrami":-999999999.9999,"aciklama":"STORNO: Yanlis musteriye girildi duzeltiliyor","stornoKaynakId":"h1","fisId":null,"maden":"ALTIN"}]}
            """);
        e.Find("#e-gecmis-ac").Click();
        var hucreler = e.FindAll("#e-gecmis td.sayi").Select(h => h.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "999.999.999,9999", "-999.999.999,9999" }, hucreler);
        var metin = e.Find("#e-gecmis").TextContent;
        Assert.Contains("Geri alma: Yanlis musteriye girildi duzeltiliyor", metin, StringComparison.Ordinal);
        Assert.DoesNotContain("STORNO", metin, StringComparison.OrdinalIgnoreCase);
        // Net sıfır: özet "Emaneti yok", 0,0000 YOK, iade kilitli.
        Assert.Equal("Emaneti yok", Ozet(e));
        Assert.DoesNotContain("0,0000", e.Markup, StringComparison.Ordinal);
        Assert.True(e.Find("#e-cikis-ac").HasAttribute("disabled"));
    }

    // ── boş/null: bakiye alanları hiç gelmezse çökmez, "Emaneti yok" ─────────
    [Fact]
    public void V4_Bakiye_alanlari_null_ise_Emaneti_yok_ve_iade_kilitli()
    {
        var e = Sec("""{"cariId":"c1"}""");
        Assert.Equal("Emaneti yok", Ozet(e));
        Assert.True(e.Find("#e-cikis-ac").HasAttribute("disabled"));
        Assert.Equal("Bu müşterinin emaneti yok.", e.Find("#e-kilit").TextContent.Trim());
    }

    // ═══ Doğrulayıcı 28.09 (t31 turu) — hasım sınır testleri (beklenenler SABİT literal) ═══
    private const string Bakiye5000 = """{"cariId":"c1","mevcutHasGramBakiye":5000,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":5000}],"hareketler":[]}""";

    // ── giriş: belirsiz "1.500" gram istek ATMAZ, alan altı Türkçe mesaj ───
    [Fact]
    public void W1_Giris_belirsiz_1_500_gram_istek_atmaz_mesaj_verir()
    {
        var e = Sec(Bakiye5000);
        e.Find("#e-giris-ac").Click();
        e.WaitForElement("#e-gram").Input("1.500");
        e.Find("#e-giris-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.Contains("Ondalık için virgül kullanın: 1,5 ya da 1500", e.Find("#cekmece-emanet-giris").TextContent, StringComparison.Ordinal);
    }

    // ── giriş: gram geçerli, katlanır taş kutusunda belirsiz "1.500" → istek YOK ─
    [Fact]
    public void W2_Giris_tas_belirsiz_istek_atmaz_detay_acik_gelir()
    {
        var e = Sec(Bakiye5000);
        e.Find("#e-giris-ac").Click();
        e.WaitForElement("#e-gram").Input("10");
        e.Find("#e-tas").Input("1.500");
        e.Find("#e-giris-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.True(e.Find("#e-giris-detay").HasAttribute("open"));
        Assert.Contains("Ondalık için virgül kullanın: 1,5 ya da 1500", e.Find("#cekmece-emanet-giris").TextContent, StringComparison.Ordinal);
    }

    // ── iade: "abc" sonra alt sınır "0,001" → eski ham metin engellemez, 0.001 gider ─
    [Fact]
    public void W3_Iade_gecersiz_sonra_alt_sinir_0_001_noktali_JSON_ile_gider()
    {
        var e = Sec(Bakiye5000);
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-has").Input("abc");
        e.Find("#e-cikis-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        e.Find("#c-has").Input("0,001");
        e.Find("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            lock (istekler)
            {
                var post = Assert.Single(istekler, i => i.Metot == "POST");
                Assert.Contains("\"hasGrami\":0.001", post.Govde, StringComparison.Ordinal);
            }
        });
    }

    // ── iade: has kutusu belirsizken ayar+gram yardımcısı (K22, 2 g) has'ı 1,832 yapar ─
    [Fact]
    public void W4_Iade_belirsiz_has_sonra_yardimci_2g_K22_1_832_gider()
    {
        var e = Sec(Bakiye5000);
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-has").Input("1.500");
        e.Find("#c-gram").Input("2");
        e.Find("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            lock (istekler)
            {
                var post = Assert.Single(istekler, i => i.Metot == "POST");
                using var j = System.Text.Json.JsonDocument.Parse(post.Govde);
                Assert.Equal(1.832m, j.RootElement.GetProperty("hasGrami").GetDecimal());
            }
        });
    }

    // ── iade: bakiyeden 0,0001 fazla → istek YOK ───────────────────────────
    [Fact]
    public void W5_Iade_bakiyeden_0_0001_fazla_istek_atmaz()
    {
        var e = Sec(Bakiye5000);
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-has").Input("5000,0001");
        e.Find("#e-cikis-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    // ═══ Doğrulayıcı 28.09 (t33 turu) — karma / tek maden iade sınırları ══════
    private const string Karma = """{"cariId":"c1","mevcutHasGramBakiye":11.45,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":11.45},{"maden":"GUMUS","madenAdi":"Gümüş","gram":1387.5}],"hareketler":[]}""";

    // ── karma: gümüş seçilip netinden 0,0001 fazla → istek YOK; tam neti GUMUS ile gider ─
    [Fact]
    public void X1_Karma_gumus_netinden_fazla_istek_atmaz_tam_net_GUMUS_ile_gider()
    {
        var e = Sec(Karma);
        Assert.Equal("Altın 11,4500 has · Gümüş 1.387,5000 has", Ozet(e));
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#cekmece-emanet-cikis");
        Assert.NotNull(e.Find("#c-maden-notu"));
        SeciciYardim.Sec(e, "c-maden", "GUMUS");
        e.Find("#c-has").Input("1387,5001");
        e.Find("#e-cikis-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        e.Find("#c-has").Input("1387,5");
        e.Find("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            lock (istekler)
            {
                var post = Assert.Single(istekler, i => i.Metot == "POST");
                using var j = System.Text.Json.JsonDocument.Parse(post.Govde);
                Assert.Equal(1387.5m, j.RootElement.GetProperty("hasGrami").GetDecimal());
                Assert.Equal("GUMUS", j.RootElement.GetProperty("maden").GetString());
            }
        });
    }

    // ── karma: altın seçiliyken altın netinden fazla (gümüş bakiyesi karışmaz) → istek YOK ─
    [Fact]
    public void X2_Karma_altin_neti_gumusle_karismaz_11_4501_istek_atmaz()
    {
        var e = Sec(Karma);
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-has").Input("11,4501");
        e.Find("#e-cikis-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    // ── karma çok büyük altın + gümüş toz: kırpılmaz, seçici yok, not var ─────
    [Fact]
    public void X3_Karma_cok_buyuk_altin_gumus_toz_kirpilmaz_secici_yok()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":999999999.99,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":999999999.99},{"maden":"GUMUS","madenAdi":"Gümüş","gram":0.0005}],"hareketler":[]}""");
        Assert.Equal("Altın 999.999.999,9900 has · Gümüş 0,0005 has", Ozet(e));
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#cekmece-emanet-cikis");
        Assert.Empty(e.FindAll("#cekmece-emanet-cikis #c-maden"));
        Assert.Equal("999999999,99", e.Find("#c-has").GetAttribute("data-deger"));
    }

    // ── karma eksi altın + dolu gümüş: işaret korunur, iade gümüşle açılır ───
    [Fact]
    public void X4_Karma_eksi_altin_isaretli_iade_gumusle_acilir()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":-2,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":-2},{"maden":"GUMUS","madenAdi":"Gümüş","gram":5}],"hareketler":[]}""");
        Assert.Equal("Altın -2,0000 has · Gümüş 5,0000 has", Ozet(e));
        Assert.False(e.Find("#e-cikis-ac").HasAttribute("disabled"));
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#cekmece-emanet-cikis");
        Assert.Equal("5", e.Find("#c-has").GetAttribute("data-deger"));
    }

    // ── karma: altın varsayılanıyla açılıp GÜMÜŞE geçilince "tümünü iade et" varsayılanı ─
    // gümüşün neti olmalı (gözlem testi — altının 11,45'i gümüşe taşınmamalı).
    [Fact]
    public void X5_Karma_madene_gecince_varsayilan_o_madenin_neti()
    {
        var e = Sec(Karma);
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#cekmece-emanet-cikis");
        Assert.Equal("11,45", e.Find("#c-has").GetAttribute("data-deger"));
        SeciciYardim.Sec(e, "c-maden", "GUMUS");
        Assert.Equal("1387,5", e.Find("#c-has").GetAttribute("data-deger"));
    }

    // ═══ Doğrulayıcı 28.09 (t34 turu, bağımsız) — hasım sınır testleri ═════════

    // ── 4 haneye 0,0010'a yuvarlanan ama iade alt sınırının (0,001) ALTINDA kalan artık ──
    // Gözlem: özet "0,0010 has" yazar, iade yine KİLİTLİDİR (sunucu 0,00095'ten 0,001 iade
    // edemez — bakiyeden fazla olur). Kilit sebebi tek kısa cümle.
    [Fact]
    public void Z1_Artik_0_00095_ozet_0_0010_yazar_iade_kilitli_sebep_cok_kucuk()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":0.00095,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":0.00095}],"hareketler":[]}""");
        Assert.Equal("0,0010 has", Ozet(e));
        Assert.True(e.Find("#e-cikis-ac").HasAttribute("disabled"));
        Assert.Equal("Kalan miktar iade için çok küçük.", e.Find("#e-kilit").TextContent.Trim());
    }

    // ── Türkçe binlik "45.000" gram → 45000 (noktalı JSON) ─────────────────────
    [Fact]
    public void Z2_Giris_Turkce_binlik_45_000_gram_45000_gider()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":0,"madenBakiyeleri":[],"hareketler":[]}""");
        e.Find("#e-giris-ac").Click();
        e.WaitForElement("#e-gram").Input("45.000");
        e.Find("#e-giris-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            lock (istekler)
            {
                var post = Assert.Single(istekler, i => i.Metot == "POST");
                Assert.EndsWith("/emanet/giris", post.Yol, StringComparison.Ordinal);
                using var j = System.Text.Json.JsonDocument.Parse(post.Govde);
                Assert.Equal(45000m, j.RootElement.GetProperty("gramAgirlik").GetDecimal());
            }
        });
    }

    // ── taş = gram (1,5 / 1,5): istek YOK, taş alanı altında mesaj, detay AÇIK ──
    [Fact]
    public void Z3_Giris_tas_gramla_esit_istek_atmaz_detay_acik_mesaj()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":0,"madenBakiyeleri":[],"hareketler":[]}""");
        e.Find("#e-giris-ac").Click();
        e.WaitForElement("#e-gram").Input("1,5");
        e.Find("#e-tas").Input("1,5");
        e.Find("#e-giris-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.Contains("Taş ağırlığı, toplam gram ağırlığından küçük olmalıdır.", e.Markup, StringComparison.Ordinal);
        Assert.True(e.Find("#e-giris-detay").HasAttribute("open"));
    }

    // ── karma özet: çok büyük altın + eksi gümüş → iki ayrı parça, işaret korunur ─
    [Fact]
    public void Z4_Karma_ozet_parcalari_cok_buyuk_ve_eksi_ayri_blok()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":999999999.99,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":999999999.99},{"maden":"GUMUS","madenAdi":"Gümüş","gram":-0.5}],"hareketler":[]}""");
        var parcalar = e.FindAll("#e-ozet-deger .emanet-ozet-parca").Select(p => p.TextContent).ToList();
        Assert.Equal(new[] { "Altın 999.999.999,9900 has", "Gümüş -0,5000 has" }, parcalar);
        Assert.Equal("Altın 999.999.999,9900 has · Gümüş -0,5000 has", Ozet(e));
    }

    // ── "STORNO:" öneki YALNIZ ters kayıtta çevrilir; sıradan açıklamaya dokunulmaz ─
    [Fact]
    public void Z5_Storno_oneki_yalniz_ters_kayitta_Geri_alma_olur()
    {
        var e = Sec("""
            {"cariId":"c1","mevcutHasGramBakiye":1,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":1}],
             "hareketler":[
               {"id":"h1","tur":"EMANET_GIRIS","tarih":"2026-09-10T10:00:00Z","hasGrami":1,"aciklama":"STORNO: musterinin kendi notu","stornoKaynakId":null,"fisId":null,"maden":"ALTIN"},
               {"id":"h2","tur":"EMANET_GIRIS","tarih":"2026-09-11T10:00:00Z","hasGrami":0.00005,"aciklama":"Yarim sinir","stornoKaynakId":null,"fisId":null,"maden":"ALTIN"}]}
            """);
        e.Find("#e-gecmis-ac").Click();
        var hucreler = e.FindAll("#e-gecmis td.sayi").Select(h => h.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "1,0000", "0,0001" }, hucreler);
        Assert.DoesNotContain("Geri alma: musterinin kendi notu", e.Find("#e-gecmis").TextContent, StringComparison.Ordinal);
    }

    // ═══ Doğrulayıcı 28.09 (19:xx turu, bağımsız) — Y serisi hasım sınır testleri ═══
    // Beklenen değerler SABİT literal: K22 = 916 milyem, G800 = 800 milyem (Ayarlar.cs).

    private (string Govde, decimal Has, string Maden) TekPost()
    {
        lock (istekler)
        {
            var post = Assert.Single(istekler, i => i.Metot == "POST");
            using var j = System.Text.Json.JsonDocument.Parse(post.Govde);
            return (post.Govde, j.RootElement.GetProperty("hasGrami").GetDecimal(), j.RootElement.GetProperty("maden").GetString()!);
        }
    }

    // ── gram girilmişken maden değişince has YENİ madenin ilk ayarından hesaplanır ──
    [Fact]
    public void Y1_Karma_gram_girilmisken_gumuse_gecince_has_G800_den_8_olur()
    {
        var e = Sec(Karma);
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#cekmece-emanet-cikis");
        e.Find("#c-gram").Input("10");
        Assert.Equal("9,16", e.Find("#c-has").GetAttribute("data-deger"));
        SeciciYardim.Sec(e, "c-maden", "GUMUS");
        Assert.Equal("8", e.Find("#c-has").GetAttribute("data-deger"));
        e.Find("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() => { var p = TekPost(); Assert.Equal(8m, p.Has); Assert.Equal("GUMUS", p.Maden); });
    }

    // ── elle yazılan has sonra boşaltılırsa (null) sessizce eski değer GİTMEZ ──
    [Fact]
    public void Y2_Iade_elle_yazilip_bosaltilinca_istek_atmaz_alan_alti_mesaj()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":3,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":3}],"hareketler":[]}""");
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-has").Input("2");
        e.Find("#c-has").Input("");
        e.Find("#e-cikis-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.Contains(Dil().T.Kabuk.FormEksik, e.Markup, StringComparison.Ordinal);
    }

    // ── çok büyük bakiye: "tümünü iade et" varsayılanı kırpılmadan/yuvarlanmadan gider ──
    [Fact]
    public void Y3_Cok_buyuk_bakiye_999999999_99_varsayilan_iade_kirpilmadan_gider()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":999999999.99,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":999999999.99}],"hareketler":[]}""");
        Assert.Equal("999.999.999,9900 has", Ozet(e));
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#cekmece-emanet-cikis");
        e.Find("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() => { var p = TekPost(); Assert.Equal(999999999.99m, p.Has); Assert.Equal("ALTIN", p.Maden); });
    }

    // ── eksi iade has reddedilir (istek yok) ──
    [Fact]
    public void Y4_Iade_eksi_has_istek_atmaz()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":3,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":3}],"hareketler":[]}""");
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-has").Input("-1");
        e.Find("#e-cikis-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    // ── 4 hane sınırı: bakiye 0,0015 → iade 0,0015 gider (0,0020'ye yuvarlanıp bakiyeyi AŞMAZ) ──
    [Fact]
    public void Y5_Dort_hane_0_0015_bakiye_tam_iade_yuvarlanmadan_gider()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":0.0015,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":0.0015}],"hareketler":[]}""");
        Assert.Equal("0,0015 has", Ozet(e));
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#cekmece-emanet-cikis");
        e.Find("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(0.0015m, TekPost().Has));
    }

    // ═══ Doğrulayıcı 28.09 (Q turu, bağlam yalıtımlı hasım) — beklenenler SABİT literal ═══

    // ── giriş: sunucu alt sınırının (0,001 g) altı → sunucu 400 → alan altı mesaj, çekmece açık, başarı yok ──
    [Fact]
    public void Q1_Giris_0_0005_gram_sunucu_reddi_alan_altinda_gorunur_basari_yok()
    {
        girisYaniti = """{"message":"Doğrulama hatası","alanlar":{"gramAgirlik":["Gram en az 0,001 olmalı."]}}""";
        var e = Sec(AltinIkiBucuk);
        e.Find("#e-giris-ac").Click();
        e.WaitForElement("#e-gram").Input("0,0005");
        e.Find("#e-giris-kaydet").Click();
        e.WaitForAssertion(() =>
            Assert.Contains("Gram en az 0,001 olmalı.", e.Find("#cekmece-emanet-giris").TextContent, StringComparison.Ordinal));
        lock (istekler)
        {
            var post = Assert.Single(istekler, i => i.Metot == "POST");
            using var j = System.Text.Json.JsonDocument.Parse(post.Govde);
            Assert.Equal(0.0005m, j.RootElement.GetProperty("gramAgirlik").GetDecimal());
        }
        Assert.DoesNotContain(Dil().T.Emanet.GirisKaydedildi, e.Markup, StringComparison.Ordinal);
    }

    // ── iade: ekranda yuvarlanmış görünen değeri (1,0002) elle yazmak 1,00015 bakiyeyi AŞAR → istek yok ──
    [Fact]
    public void Q2_Iade_yuvarlanmis_gorunen_1_0002_bakiye_1_00015_i_asar_istek_yok_varsayilan_tam_gider()
    {
        const string b = """{"cariId":"c1","mevcutHasGramBakiye":1.00015,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":1.00015}],"hareketler":[]}""";
        var e = Sec(b);
        Assert.Equal("1,0002 has", Ozet(e));
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-has").Input("1,0002");
        e.Find("#e-cikis-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        e.Find("#c-has").Input("1,00015");
        e.Find("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(1.00015m, TekPost().Has));
    }

    // ── İngilizce arayüz: sıfır bakiye "No custody", "TRY" kalıbı ve 0,0000 yok; dolu bakiye tr-TR virgüllü ──
    [Fact]
    public async Task Q3_Ingilizce_arayuzde_sifir_No_custody_dolu_tr_TR_bicim_TRY_yok()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":0,"madenBakiyeleri":[],"hareketler":[]}""");
        await e.InvokeAsync(() => Dil().DegistirAsync("en"));
        e.WaitForAssertion(() => Assert.Equal("No custody", Ozet(e)));
        Assert.DoesNotContain("0,0000", e.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("TRY", e.Markup, StringComparison.Ordinal);
        Assert.Equal("This customer has no custody.", e.Find("#e-kilit").TextContent.Trim());
    }

    // ── tablo hücresi: çok büyük has + yarım sınır → binlik ayırıcısız, yarım uzağa, birimsiz ──
    [Fact]
    public void Q4_Gecmis_hucresi_cok_buyuk_yarim_sinir_1234567_00005_birimsiz()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":1234567.00005,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":1234567.00005}],"hareketler":[{"id":"h1","tur":"EMANET_GIRIS","tarih":"2026-09-28T10:00:00Z","hasGrami":1234567.00005,"aciklama":null,"stornoKaynakId":null,"fisId":null,"maden":"ALTIN"}]}""");
        Assert.Equal("1.234.567,0001 has", Ozet(e));
        e.Find("#e-gecmis-ac").Click();
        var hucre = e.FindAll("#e-gecmis td.sayi")[0].TextContent.Trim();
        Assert.Equal("1.234.567,0001", hucre);   // MADEN-AYRIMI tur 2: Bicim.Has binlik gruplu
    }

    // TEMA TEMİZLİĞİ 17.10: T1_Acilista_musteri_secilmeden_yalniz_zorunlu_kilit_tek_cumle_Eksik_olan_yok kaldırıldı — Yıldız'da açılış listedir; özgün gövde yalnız bir satırın müşterisi SEÇİLİ açılır (müşterisiz özgün durum ulaşılamaz). "Önce müşteri seçin" kapısı Yıldız "Emanet al" formunda alan altında: Yildiz5AFormTestleri.Y5A_F01.

    // Canlı 5198 verisinin aynısı: G925 30 g (27,75 has gümüş) + K24 5 g altın.
    [Fact]
    public void T2_Canli_karma_gumus_27_75_altin_5_ozet_iki_parca_iade_acik_kilit_yok()
    {
        var e = Sec("""{"cariId":"c1","mevcutHasGramBakiye":5,"madenBakiyeleri":[{"maden":"GUMUS","madenAdi":"Gümüş","gram":27.75},{"maden":"ALTIN","madenAdi":"Altın","gram":5}],"hareketler":[]}""");
        Assert.Equal("Gümüş 27,7500 has · Altın 5,0000 has", Ozet(e));
        Assert.False(e.Find("#e-cikis-ac").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#e-kilit"));
        Assert.Equal(2, e.FindAll("#e-ozet-deger .emanet-ozet-parca").Count);
    }

    // ═══ Doğrulayıcı 28.09 (t39 sonrası, 12. tur) — iade has kutusu 4 hane (EmanetHasGirdi) ═══
    // Beklenenler SABİT literal; üretim kodundan hesaplanmaz.

    private IRenderedComponent<Kuyumcu.Web.Pages.Emanet> IadeAc(string govde)
    {
        var e = Sec(govde);
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-has");
        return e;
    }

    private static decimal? PostHas(List<(string Metot, string Yol, string Govde)> l)
    {
        var post = Assert.Single(l, i => i.Metot == "POST" && i.Yol.Contains("/cikis", StringComparison.Ordinal));
        using var j = System.Text.Json.JsonDocument.Parse(post.Govde);
        return j.RootElement.GetProperty("hasGrami").GetDecimal();
    }

    // sıfır olmayan olağan değer: kutu 4 hane ("11,4500"), gram biçimi 3 hane DEĞİL
    [Fact]
    public void U1_Iade_has_kutusu_4_hane_11_4500()
    {
        var e = IadeAc("""{"cariId":"c1","mevcutHasGramBakiye":11.45,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":11.45}],"hareketler":[]}""");
        Assert.Equal("11,4500", e.Find("#c-has").GetAttribute("value"));
    }

    // 4 hane yarım sınır: 2,00005 özet AwayFromZero "2,0001 has"; kutu YUVARLAMAZ, tam net gider
    [Fact]
    public void U2_Yarim_sinir_2_00005_ozet_2_0001_kutu_tam_ve_POST_tam()
    {
        var e = IadeAc("""{"cariId":"c1","mevcutHasGramBakiye":2.00005,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":2.00005}],"hareketler":[]}""");
        Assert.Equal("2,0001 has", Ozet(e));
        Assert.Equal("2,00005", e.Find("#c-has").GetAttribute("value"));
        e.Find("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() => { lock (istekler) Assert.Equal(2.00005m, PostHas(istekler)); });
    }

    // çok büyük: 999.999.999,99 kırpılmadan 4 haneyle yazılır ve aynen gider
    [Fact]
    public void U3_Cok_buyuk_999999999_99_kutu_4_hane_POST_aynen()
    {
        var e = IadeAc("""{"cariId":"c1","mevcutHasGramBakiye":999999999.99,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":999999999.99}],"hareketler":[]}""");
        Assert.Equal("999999999,9900", e.Find("#c-has").GetAttribute("value"));
        e.Find("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() => { lock (istekler) Assert.Equal(999999999.99m, PostHas(istekler)); });
    }

    // en küçük birim 0,0001: alt sınırın (0,001) altında → istek YOK, alan altı mesaj
    [Fact]
    public void U4_Iade_0_0001_istek_atmaz_alt_sinir_mesaji()
    {
        var e = IadeAc("""{"cariId":"c1","mevcutHasGramBakiye":5,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":5}],"hareketler":[]}""");
        e.Find("#c-has").Input("0,0001");
        e.Find("#e-cikis-kaydet").Click();
        Assert.Contains("İade edilecek miktar en az 0,0010 has olmalı.", e.Markup, StringComparison.Ordinal);
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    // eksi: "-0,0001" reddedilir
    [Fact]
    public void U5_Iade_eksi_0_0001_istek_atmaz()
    {
        var e = IadeAc("""{"cariId":"c1","mevcutHasGramBakiye":5,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":5}],"hareketler":[]}""");
        e.Find("#c-has").Input("-0,0001");
        e.Find("#e-cikis-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.Equal("true", e.Find("#c-has").GetAttribute("aria-invalid"));
    }

    // boş: has kutusu boşaltılınca istek YOK, alan geçersiz işaretli
    [Fact]
    public void U6_Iade_has_bos_istek_atmaz()
    {
        var e = IadeAc("""{"cariId":"c1","mevcutHasGramBakiye":5,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":5}],"hareketler":[]}""");
        e.Find("#c-has").Input("");
        e.Find("#e-cikis-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.Equal("true", e.Find("#c-has").GetAttribute("aria-invalid"));
    }

    // yardımcı: 22 ayar 10 g → 9,1600 has (4 hane); gram silinince gram kutusu BOŞ, has varsayılana (5,0000) döner
    [Fact]
    public void U7_Yardimci_K22_10g_9_1600_gram_silinince_bos_has_5_0000()
    {
        var e = IadeAc("""{"cariId":"c1","mevcutHasGramBakiye":5,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":5}],"hareketler":[]}""");
        // #e-cikis-hesap-ac yerel <details> özetidir (olay işleyicisi yok); alan DOM'da durur.
        e.Find("#c-gram").Input("10");
        e.Find("#c-gram").Blur();
        e.WaitForAssertion(() => Assert.Equal("9,1600", e.Find("#c-has").GetAttribute("value")));
        e.Find("#c-gram").Input("");
        e.Find("#c-gram").Blur();
        e.WaitForAssertion(() => Assert.Equal("", e.Find("#c-gram").GetAttribute("value") ?? ""));
        Assert.Equal("5,0000", e.Find("#c-has").GetAttribute("value"));
    }

    // Türkçe giriş: "1,5" → 1.5 noktalı JSON
    [Fact]
    public void U8_Iade_Turkce_1_5_noktali_JSON()
    {
        var e = IadeAc("""{"cariId":"c1","mevcutHasGramBakiye":5,"madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":5}],"hareketler":[]}""");
        e.Find("#c-has").Input("1,5");
        e.Find("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() => { lock (istekler) Assert.Equal(1.5m, PostHas(istekler)); });
    }

    private DilServisi Dil() => Services.GetRequiredService<DilServisi>();
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET EMANET-1 — HASIM DOĞRULAYICI TUR 30.09 (Aşkın + Mehmet): ek sınırlar.
// Beklenen değerler SABİT literal (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class EMANET1SinirTestleriTur3009
{
    // eksi yönde yarım sınır: yarım UZAĞA → "-0,0001"; "-0,0000" asla
    [Fact]
    public void D1_Eksi_yarim_sinir_uzaga_yuvarlanir()
    {
        Assert.Equal("-0,0001 has", Bicim.Has(-0.00005m));
        Assert.Equal("0,0002 has", Bicim.Has(0.00015m));
        Assert.Equal("0,0000 has", Bicim.Has(0.000049m));
    }

    // çok büyük has: kırpılmaz, 4 hane, tr-TR virgül
    [Fact]
    public void D2_Cok_buyuk_eksi_has_kirpilmaz()
    {
        Assert.Equal("-999.999.999,9900 has", Bicim.Has(-999999999.99m));
    }

    // Türkçe giriş: 6 haneli has ondalığı korunur; boşluklu giriş boş değil
    [Theory]
    [InlineData("0,000050", 0.00005)]
    [InlineData("1.234,5678", 1234.5678)]
    public void D3_Turkce_has_girisi_ondalik_korunur(string ham, double beklenen)
    {
        var s = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, s.Sonuc);
        Assert.Equal((decimal)beklenen, s.Deger);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// Doğrulayıcı tur 30.09 (B006 turu) — bağımsız ek sınırlar. Beklenenler SABİT
// literal; üretim kodundan hesaplanmaz.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class EMANET1SinirTestleriTur3009b
{
    // yarım sınır 4. hanede: UZAĞA (0,00025 → 0,0003). ToEven olsaydı 0,0002 yazardı.
    [Fact]
    public void E1_Yarim_sinir_0_00025_uzaga_0_0003()
    {
        Assert.Equal("0,0003 has", Bicim.Has(0.00025m));
        Assert.Equal("-0,0003 has", Bicim.Has(-0.00025m));
    }

    // çok büyük has 4 hane kırpılmaz
    [Fact]
    public void E2_Cok_buyuk_has_4_hane_kirpilmaz()
    {
        Assert.Equal("999.999.999,9999 has", Bicim.Has(999999999.9999m));
    }

    // Türkçe eksi giriş işareti korunur; belirsiz eksi binlik reddedilir; tek eksi geçersiz
    [Fact]
    public void E3_Turkce_eksi_giris_isaret_ve_belirsizlik()
    {
        var a = FormAlani.SayiCoz("-1,5");
        Assert.Equal(FormAlani.SayiSonucu.Tamam, a.Sonuc);
        Assert.Equal(-1.5m, a.Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("-1.500").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Gecersiz, FormAlani.SayiCoz("-").Sonuc);
        var b = FormAlani.SayiCoz("999.999.999,99");
        Assert.Equal(999999999.99m, b.Deger);
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
    }
}
