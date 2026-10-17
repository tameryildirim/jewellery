using System.Text.Json;
using Bunit;
using Kuyumcu.Contracts.Bilesenler.MasakFormu;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ════════════════════════════════════════════════════════════════════════════
// PAKET P-TEZGAH-TUR-KIMLIK — ekran denetimi 10.10 (K4, Y4, Y2, O10) + YASAL-KAPI-3 tasarım kalanları.
//   K4 : kimlik çekmecesinde birincil "Tamam" GERÇEK (eksikse alan altına yazar, tamamsa kapanır); zorunlu işaretli;
//        kilitli Kaydet eksik ALANLARIN ADINI söyler (Platin'de form açıkken de Kaydet'in yanında).
//   Y4 : işlem türü GÖRÜNÜR sekmeler (Satış · Alış · İade · Tahsilat · Diğer) — Yıldız Satış yüzünde ve öteki türlerde.
//   Y2 : Yıldız müşteri katmanı seçimle kapanır.
//   O10: "yasal bir kapıya takıldı" iç dili yok.
//   Tasarım: Ad Soyad tam satır (fiş), BASİT seçili tür karosu birincil görünmez (tek birincil Kaydet), Yıldız TUR-KIMLIK bloğu.
// Görsel ölçümün ASIL kanıtı gerçek Chrome (rapor ölçüm tablosu); bu sınıf sözleşmeyi kilitler.
// ════════════════════════════════════════════════════════════════════════════
public sealed class PTezgahTurKimlikTestleri : BunitContext
{
    private static readonly TimeSpan Bekle = TimeSpan.FromSeconds(6);
    private string sema = "yildiz";
    private decimal kimlikEsigi = 185000m;
    private bool amac;
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private sealed class SahteApi(PTezgahTurKimlikTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            var y = yol switch
            {
                var v when v.Contains("/gorunum") => $$"""{"sema":"{{t.sema}}","yaziOlcegi":"18","temaModu":"karanlik"}""",
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Gram altın","ayarId":"K24","ayar":"24","hasGrami":0.995,"iscilikTl":0,"satisFiyati":10000,"birim":"ADET","milyem":995,"gramAgirlik":1}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari/sistem-carisi") => """{"id":"p1","ad":"Peşin Müşteri"}""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Selin Ergüven"},{"id":"c2","ad":"Tuna Akarsu"}]""",
                var v when v.Contains("/masak/gereksinim") && t.kimlikEsigi < 185000m && t.amac =>
                    """{"kademe":"AMAC_BEYANI","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/gereksinim") && t.kimlikEsigi < 185000m =>
                    """{"kademe":"YALNIZ_KIMLIK","kimlikGerekli":true,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => $$"""{"kimlikEsigi":{{t.kimlikEsigi}},"amacBeyaniEsigi":{{t.kimlikEsigi}},"beyanFormuEsigi":{{t.kimlikEsigi}},"ekBelgeEsigi":{{t.kimlikEsigi}},"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var v when v.Contains("/pos/cihazlar") => "[]",
                var v when v.Contains("/islemler/satis") && istek.Method == HttpMethod.Post => """{"id":"h1","hareketId":"h1","fisNo":"F-0001","fisId":"f1"}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    public PTezgahTurKimlikTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod, string s = "yildiz")
    {
        sema = s;
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Hakan", "Tunalı", "h@ornek.com", "SAHIP", "s1", mod, mod));
        Services.GetRequiredService<GorunumServisi>().YukleAsync().GetAwaiter().GetResult();
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement(s == "yildiz" ? "#yildiz-tezgah" : "#islem-kaydet", TimeSpan.FromSeconds(10));
        return e;
    }

    private static void KalemEkle(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))), Bekle);
        e.Find("#islem-kalem-ekle").Click();
    }

    /// <summary>Yıldız BASİT satış: kalem + gerçek müşteri (eşik aşılır) → "+ Kimlik bilgisi" çekmecesi açık.</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> KimlikCekmecesi(string mod)
    {
        kimlikEsigi = 5000m; amac = true;
        var e = Ekran(mod);
        KalemEkle(e);
        e.WaitForAssertion(() => Assert.Single(e.FindAll(".yz-tezgah-kalem")), Bekle);
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-musteri-sec-c1")), Bekle);
        e.Find("#yildiz-musteri-sec-c1").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-tezgah-kimlik")), Bekle);
        e.Find("#yildiz-tezgah-kimlik").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#cekmece-yildiz-kimlik")), Bekle);
        return e;
    }

    private static string Onek => TurKimlikSozlugu.Tr.KimlikEksik[..TurKimlikSozlugu.Tr.KimlikEksik.IndexOf('{')];

    // ── K4 · kimlik çekmecesi ───────────────────────────────────────────────────

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void TK_01_kimlik_cekmecesi_birincil_Tamam_eksikse_alan_altina_yazar_tamamsa_kapanir(string mod)
    {
        var e = KimlikCekmecesi(mod);
        // birincil "Tamam" GERÇEK düğme (iptal değil); ikincil Kapat — id'ler sözleşmeli
        Assert.Equal("Tamam", e.Find("#cekmece-yildiz-kimlik-tamam").TextContent.Trim());
        Assert.Contains("dugme-birincil", e.Find("#cekmece-yildiz-kimlik-tamam").ClassName);
        Assert.Equal("Kapat", e.Find("#cekmece-yildiz-kimlik-iptal").TextContent.Trim());
        Assert.Empty(e.FindAll("#cekmece-yildiz-kimlik .alan-hata"));          // açılışta kırmızı yazı yok
        // eksik formla Tamam → çekmece AÇIK, boş zorunluların altında mesaj
        e.Find("#cekmece-yildiz-kimlik-tamam").Click();
        e.WaitForAssertion(() => Assert.Equal(TurKimlikSozlugu.Tr.AlanZorunlu, e.Find("#masak-amac-hata").TextContent.Trim()), Bekle);
        Assert.Equal(TurKimlikSozlugu.Tr.AlanZorunlu, e.Find("#masak-aciklama-hata").TextContent.Trim());
        Assert.Equal(TurKimlikSozlugu.Tr.AlanZorunlu, e.Find("#masak-belge-no-hata").TextContent.Trim());
        Assert.Single(e.FindAll("#cekmece-yildiz-kimlik"));
        Assert.True(e.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled"));
        // kısa beyan → tam cümle ("En az 20 karakter yazın (4 / 20).")
        e.Find("#masak-belge-no").Input("10000000146");
        e.Find("#masak-amac").Input("Birikim");
        e.Find("#masak-aciklama").Input("kısa");
        e.Find("#cekmece-yildiz-kimlik-tamam").Click();
        e.WaitForAssertion(() => Assert.Equal(string.Format(TurKimlikSozlugu.Tr.AciklamaKisa, 4, 20), e.Find("#masak-aciklama-hata").TextContent.Trim()), Bekle);
        Assert.Single(e.FindAll("#cekmece-yildiz-kimlik"));
        // tamamlanınca Tamam çekmeceyi KAPATIR, Kaydet açılır
        e.Find("#masak-aciklama").Input("Müşteri birikim için altın alıyor, kartla ödüyor.");
        e.WaitForAssertion(() => Assert.False(e.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled")), Bekle);
        e.Find("#cekmece-yildiz-kimlik-tamam").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#cekmece-yildiz-kimlik")), Bekle);
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis"));   // Tamam istek ATMAZ
    }

    [Fact]
    public void TK_02_kilitli_Kaydet_eksik_alanlarin_ADINI_soyler_ve_alanlar_isaretli()
    {
        var e = KimlikCekmecesi(ArayuzModuServisi.Basit);
        var not = e.Find("#yildiz-tezgah-not").TextContent.Trim();
        Assert.StartsWith(Onek, not);
        Assert.Contains(ParaDemo3Sozlugu.Tr.AlanIslemAmaci, not);
        Assert.Contains(string.Format(ParaDemo3Sozlugu.Tr.AlanBeyanAciklama, 20), not);
        Assert.Equal(not, e.Find("#yildiz-tezgah-kaydet").GetAttribute("title"));
        // zorunlu işaret DOM'da (CSS'te Yıldız çekmecesinde görünür — TK_08)
        foreach (var id in new[] { "masak-amac", "masak-aciklama", "masak-ad-soyad" })
            Assert.NotNull(e.Find($"#{id}-sar .zorunlu-isaret"));
        // tamamlanınca cümle eksik alanı bırakır
        e.Find("#masak-amac").Input("Birikim");
        e.WaitForAssertion(() => Assert.DoesNotContain(ParaDemo3Sozlugu.Tr.AlanIslemAmaci + ",", e.Find("#yildiz-tezgah-not").TextContent), Bekle);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void TK_03_platin_form_acikken_kilit_cumlesi_Kaydetin_YANINDA_ve_alan_adli(string mod)
    {
        // TEMA TEMİZLİĞİ 17.10: Platin şeması kaldırıldı — satış hep Yıldız tezgâhı. Güvence AYNI, iki modda: kimlik formu
        // açıkken kilitli Kaydet'in sebebi eksik ALANIN ADINI söyler ve Kaydet'in YANINDA (Fark bloğunun not satırı) görünür.
        var e = KimlikCekmecesi(mod);
        e.WaitForAssertion(() => Assert.StartsWith(Onek, e.Find("#yildiz-tezgah-kaydet").GetAttribute("title") ?? ""), Bekle);
        Assert.True(e.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled"));
        var kilit = e.Find("#yildiz-tezgah-not");
        Assert.Equal(e.Find("#yildiz-tezgah-kaydet").GetAttribute("title"), kilit.TextContent.Trim());
        Assert.DoesNotContain("gorunmez", kilit.ClassName);
        Assert.Contains(ParaDemo3Sozlugu.Tr.AlanIslemAmaci, kilit.TextContent);
        Assert.NotNull(e.Find("#yildiz-tezgah-alt #yildiz-tezgah-not"));   // Kaydet ile aynı blokta (Fark + eylemler)
    }

    [Fact]
    public void TK_04_MasakFormu_EksikGoster_kapaliyken_kirmizi_yok_acikken_bos_ve_kisa_alan_altinda()
    {
        var model = new MasakFormu.MasakBilgi { BelgeTuru = "TC_KIMLIK", Aciklama = "kısa" };
        var esik = JsonSerializer.Deserialize<Esikler>("""{"kimlikEsigi":5000,"amacBeyaniEsigi":5000,"beyanFormuEsigi":5000,"ekBelgeEsigi":5000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var ger = JsonSerializer.Deserialize<MasakGereksinim>("""{"kademe":"AMAC_BEYANI","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var c = Render<MasakFormu>(p => p.Add(x => x.Model, model).Add(x => x.Tutar, 10000m).Add(x => x.Esik, esik).Add(x => x.Gereksinim, ger));
        Assert.Empty(c.FindAll("#masak-amac-hata"));
        Assert.Equal("4 / 20", c.Find("#masak-aciklama-hata").TextContent.Trim());   // eskisi AYNEN
        c.Render(p => p.Add(x => x.EksikGoster, true));
        Assert.Equal(TurKimlikSozlugu.Tr.AlanZorunlu, c.Find("#masak-amac-hata").TextContent.Trim());
        Assert.Equal(TurKimlikSozlugu.Tr.AlanZorunlu, c.Find("#masak-ad-soyad-hata").TextContent.Trim());
        Assert.Equal(string.Format(TurKimlikSozlugu.Tr.AciklamaKisa, 4, 20), c.Find("#masak-aciklama-hata").TextContent.Trim());
        Assert.False(MasakFormu.Eksiksiz(model, ger));   // kural AYNEN (yalnız görünüm)
    }

    // ── Y4 · görünür tür seçici ─────────────────────────────────────────────────

    [Theory]
    [InlineData(ArayuzModuServisi.Basit, "HURDA_ALIS")]
    [InlineData(ArayuzModuServisi.Profesyonel, "HURDA_ALIS")]   // tur 2: Yıldız'da "Alış" iki modda da aynı (müşteriden alış)
    public void TK_05_yildiz_tur_sekmeleri_gorunur_tek_tikla_ve_geri_donus(string mod, string alis)
    {
        var e = Ekran(mod);
        foreach (var id in new[] { "#yildiz-tur-SATIS", $"#yildiz-tur-{alis}", "#yildiz-tur-SATIS_IADE", "#yildiz-tur-tahsilat" })
            Assert.NotNull(e.Find(id));
        Assert.Equal("true", e.Find("#yildiz-tur-SATIS").GetAttribute("aria-pressed"));
        Assert.Empty(e.FindAll("#yildiz-tur-ALIS"));   // tur 2: toptancıdan ürün alışı sekme DEĞİL — "Diğer" listesinde
        Assert.Equal("Alış", e.Find($"#yildiz-tur-{alis}").TextContent.Trim());
        Assert.Equal("Diğer", e.Find("#yildiz-tezgah-tur").TextContent.Trim());            // id AYNEN, artık "Diğer"
        Assert.Equal("/cariler", e.Find("#yildiz-tur-tahsilat").GetAttribute("href"));
        // tek tık: Alış
        e.Find($"#yildiz-tur-{alis}").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#yildiz-tezgah")), Bekle);
        Assert.Contains("yz-islem-tur", e.Find(".islem-sayfa").ClassList);
        Assert.Equal("true", e.Find($"#yildiz-tur-{alis}").GetAttribute("aria-pressed"));
        Assert.Single(e.FindAll("#yildiz-tur-ust"));
        Assert.Single(e.FindAll("#islem-kaydet"));
        Assert.DoesNotContain("yz-turler-acik", e.Find(".islem-sayfa").ClassList);
        // Diğer: tüm türler açılır; listeden seçilince kapanır
        e.Find("#yildiz-tur-diger").Click();
        e.WaitForAssertion(() => Assert.Contains("yz-turler-acik", e.Find(".islem-sayfa").ClassList), Bekle);
        e.Find("#islem-tur-SATIS_IADE").Click();
        e.WaitForAssertion(() => Assert.DoesNotContain("yz-turler-acik", e.Find(".islem-sayfa").ClassList), Bekle);
        Assert.Equal("true", e.Find("#yildiz-tur-SATIS_IADE").GetAttribute("aria-pressed"));
        // tek tık geri: Satış
        e.Find("#yildiz-tur-SATIS").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-tezgah")), Bekle);
        Assert.Empty(e.FindAll("#yildiz-tur-ust"));
    }

    // TEMA TEMİZLİĞİ 17.10: TK_06_platinde_tur_sekmesi_yok_ozgun_izgara_AYNEN kaldırıldı — Platin şeması (ve sekmesiz özgün ızgara
    // yüzü) bilerek kaldırıldı; Yıldız tür sekmeleri ve "Diğer"le açılan özgün ızgara TK_05'te sınanır.

    // ── Y2 · müşteri katmanı seçimle kapanır ───────────────────────────────────

    [Fact]
    public void TK_07_yildiz_musteri_secince_katman_kapanir()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-musteri-sec-c2")), Bekle);
        e.Find("#yildiz-musteri-sec-c2").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#yildiz-tezgah-musteri")), Bekle);
        Assert.Contains("Tuna Akarsu", e.Find("#yildiz-tezgah-ust").TextContent);
        Assert.Equal("false", e.Find("#yildiz-tezgah-degistir").GetAttribute("aria-expanded"));
    }

    // ── O10 · iç dil yok ─────────────────────────────────────────────────────────

    [Fact]
    public void TK_08_yasal_kapi_ic_dili_yok()
    {
        Assert.DoesNotContain("kapı", SozlukTr().Arayuz.YasalKapiAcildi, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("legal check", SozlukEn().Arayuz.YasalKapiAcildi, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(TurKimlikSozlugu.Tr.GetType().GetProperties().Length, TurKimlikSozlugu.En.GetType().GetProperties().Length);
        Assert.All(TurKimlikSozlugu.En.GetType().GetProperties().Where(p => p.PropertyType == typeof(string)),
            p => Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(TurKimlikSozlugu.En)), p.Name));
    }

    private static Sozluk SozlukTr() => Sozlukler.Getir("tr");
    private static Sozluk SozlukEn() => Sozlukler.Getir("en");

    // ── CSS sözleşmesi ───────────────────────────────────────────────────────────

    [Fact]
    public void TK_09_css_yildiz_tur_kimlik_ve_yasal_tasarim_kurallari()
    {
        var y = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tema-yildiz.css");
        var i = y.IndexOf("PAKET P-TEZGAH-TUR-KIMLIK (TUR-KIMLIK)", StringComparison.Ordinal);
        Assert.True(i > 0, "TUR-KIMLIK bloğu yok");
        var blok = y[i..y.IndexOf("P-TEZGAH-TUR-KIMLIK sonu", i, StringComparison.Ordinal)];
        Assert.Contains("#yildiz-kimlik-govde .zorunlu-isaret { display: inline;", blok);           // K4: işaret görünür
        Assert.Contains(".yz-islem-tur:not(.yz-turler-acik) > :is(.sekme-grubu, #islem-izgara) { display: none; }", blok);
        Assert.Contains("font-size: 14px;", blok);                                                  // tuval .sek ölçüsü
        Assert.Contains("letter-spacing: .1em;", blok);
        Assert.DoesNotContain("#", System.Text.RegularExpressions.Regex.Replace(blok, @"(#[a-z][\w-]*|/\*.*?\*/)", "", System.Text.RegularExpressions.RegexOptions.Singleline));   // sabit renk (#hex) yok
        var s = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "yasal.css");
        Assert.Contains(".islem-sayfa .kart--masak > .kart-govde > .form-grid-3 > :nth-child(3) { grid-column: 1 / -1; }", s);   // Ad Soyad tam satır
        // TEMA TEMİZLİĞİ 17.10: Yıldız DIŞI şemaların BASİT tür karosu kuralı (html:root:not([data-sema="yildiz"]) … .ekran-karo-birincil)
        // kaldırıldı — o şemalar yok; Yıldız'da tek birincil düğme Kaydet'tir (YTO_02).
        Assert.DoesNotContain(":not([data-sema=\"yildiz\"])",
            System.Text.RegularExpressions.Regex.Replace(s, @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline));
    }

    // ── TUR 2 (doğrulayıcı + tasarım hakemi bulguları) ─────────────────────────────

    /// <summary>Son "document.getElementById" çağrısının hedefi (Tamam'ın odak verdiği alan).</summary>
    private string? SonOdak() => JSInterop.Invocations.Where(i => i.Identifier == "document.getElementById")
        .Select(i => i.Arguments.FirstOrDefault()?.ToString()).LastOrDefault();

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void TK_10_kimlik_cekmecesinde_MASAK_teknik_paragrafi_ve_musavir_notu_YOK(string mod)
    {
        // Hakem KRİTİK: PRO çekmecede "Kimlik … zorunlu (MASAK) … (mali müşavir teyidi bekleniyor). Bilgiler şifreli saklanır."
        var e = KimlikCekmecesi(mod);
        Assert.Empty(e.FindAll("#masak-kural"));
        var metin = e.Find("#cekmece-yildiz-kimlik").TextContent;
        Assert.DoesNotContain("müşavir", metin, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("teyid", metin, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("(MASAK)", metin, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void TK_11_yildiz_Alis_bos_acilista_Kaydet_kilit_sebebi_GORUNUR_ve_Cari_jargonu_yok(string mod)
    {
        var e = Ekran(mod);
        e.Find("#yildiz-tur-HURDA_ALIS").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#islem-kaydet")), Bekle);
        Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled"));
        var kilit = e.Find("#islem-kaydet-kilit");
        Assert.DoesNotContain("gorunmez", kilit.ClassName ?? "");                                 // 1 px gizli değil
        var cumle = kilit.TextContent.Trim();
        Assert.False(string.IsNullOrWhiteSpace(cumle));
        Assert.Equal(cumle, e.Find("#islem-kaydet").GetAttribute("title"));                      // tek cümle, düğmeyle aynı
        var sayfa = e.Find(".islem-sayfa").TextContent;
        Assert.DoesNotContain("Cari seçin", sayfa, StringComparison.Ordinal);                     // etiket Müşteri → yer tutucu/kilit de Müşteri
    }

    [Fact]
    public void TK_12_Tamam_odagi_MasakEksikAlanlari_sirasinda_uyruk_eksikse_katlanir_acilir()
    {
        var e = KimlikCekmecesi(ArayuzModuServisi.Basit);
        e.Find("#masak-belge-no").Input("10000000146");
        e.Find("#masak-ad-soyad").Input("Selin Ergüven");
        e.Find("#masak-amac").Input("Birikim");
        e.Find("#masak-aciklama").Input("Müşteri birikim için altın alıyor, kartla ödüyor.");
        e.WaitForAssertion(() => Assert.False(e.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled")), Bekle);
        Assert.Empty(e.FindAll("#masak-uyruk"));                                                   // katlanır kapalı (zorunlu önce)
        // yalnız uyruk geçersiz → Tamam odağı uyruğa verir ve "Diğer bilgiler" açılır
        var form = e.FindComponent<MasakFormu>();
        form.Instance.Model.Uyruk = "Q";
        e.Render();
        e.WaitForAssertion(() => Assert.True(e.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled")), Bekle);
        e.Find("#cekmece-yildiz-kimlik-tamam").Click();
        e.WaitForAssertion(() => Assert.Equal("masak-uyruk", SonOdak()), Bekle);
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#masak-uyruk")), Bekle);
        Assert.Single(e.FindAll("#cekmece-yildiz-kimlik"));
        // belge türü boş → odak belge türüne (listenin İLK maddesi)
        form.Instance.Model.Uyruk = "TUR";
        form.Instance.Model.BelgeTuru = "";
        e.Render();
        e.Find("#cekmece-yildiz-kimlik-tamam").Click();
        e.WaitForAssertion(() => Assert.Equal("masak-belge-turu", SonOdak()), Bekle);
    }

    [Fact]
    public void TK_13_css_tur2_Tamam_dolu_kilit_Kaydet_yaninda_musteri_katmani_eski_satirlar()
    {
        var y = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tema-yildiz.css");
        var i = y.IndexOf("PAKET P-TEZGAH-TUR-KIMLIK (TUR-KIMLIK)", StringComparison.Ordinal);
        var blok = y[i..y.IndexOf("P-TEZGAH-TUR-KIMLIK sonu", i, StringComparison.Ordinal)];
        var tamam = blok[blok.IndexOf(".yz .cekmece-alt .dugme.dugme-birincil:not(#yz-tk) {", StringComparison.Ordinal)..];
        tamam = tamam[..tamam.IndexOf('}')];
        Assert.Contains("background: var(--metin);", tamam);                                      // .b1 dolu
        Assert.Contains("color: var(--yuzey-govde);", tamam);
        Assert.Contains("border: 0;", tamam);                                                     // yarım yay (yalnız alt kenar) yok
        var kilit = blok[blok.IndexOf(".yz-islem-tur #islem-kaydet-kilit:not(.gorunmez) {", StringComparison.Ordinal)..];
        kilit = kilit[..kilit.IndexOf('}')];
        Assert.Contains("order: 0;", kilit);                                                      // Kaydet'in yanında (alta itilmez)
        Assert.Contains("#yildiz-tezgah-musteri.yz-tezgah-musteri { flex: 0 1 auto; min-height: 0; max-height: none;", blok);   // 50vh iç kutu yok
        Assert.Contains(":has(> #yildiz-tezgah-musteri) > .yz-tezgah-giris", blok);              // katman barkod alanının yerinde
        Assert.Contains(":is(#islem-pro-kisayollar, #islem-cari-pasif-notu) { display: none; }", blok);
    }

    [Fact]
    public void TK_14_PRO_urun_alisi_Digerden_secilir_sekmede_iki_Alis_olmaz()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        e.Find("#yildiz-tezgah-tur").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#islem-tur-ALIS")), Bekle);
        e.Find("#islem-tur-ALIS").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-tur-ALIS")), Bekle);
        Assert.Equal(TurKimlikSozlugu.Tr.UrunAlisi, e.Find("#yildiz-tur-ALIS").TextContent.Trim());
        Assert.Equal("Alış", e.Find("#yildiz-tur-HURDA_ALIS").TextContent.Trim());
        Assert.Equal("Product purchase", TurKimlikSozlugu.En.UrunAlisi);
    }
}

/// <summary>
/// P-TEZGAH-TUR-KIMLIK (denetim K4): kilitli Kaydet'in kimlik cümlesi artık eksik ALANLARIN ADINI söyler
/// ("Kaydetmek için eksik: …"). Eski genel cümleyi (MasakKilitSebepBasit / MasakKilitProAmac / MasakKilitProKimlik)
/// bekleyen YASAL-KAPI-2/3 testleri bu yardımcıyla yeni sözleşmeye çekildi (eşik SAYISI yine yazılmaz).
/// </summary>
internal static class PTezgahTurKimlikYardim
{
    public static string Onek => TurKimlikSozlugu.Tr.KimlikEksik[..TurKimlikSozlugu.Tr.KimlikEksik.IndexOf('{')];

    public static void EksikKilit(string? metin, params string[] alanlar)
    {
        Assert.NotNull(metin);
        Assert.StartsWith(Onek, metin);
        foreach (var a in alanlar) Assert.Contains(a, metin);
    }
}
