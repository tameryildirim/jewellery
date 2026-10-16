using System.Net;
using Bunit;
using Kuyumcu.Contracts.Bilesenler.MasakFormu;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET MASAK-370 tur 3 — EKRANIN FORM AÇMA TUTARI (doğrulayıcı KRİTİK: /banka dekont çıkmazı).
//
// 07.10.2026'dan itibaren md.5/1-b kimlik eşiği 370.000 TL (RG 33393), ama TEYİTSİZ 200.000 TL
// "işlem amacı beyanı" kademesi (kurucu md.15 (a): olduğu gibi) kimliği 200.000'de istemeye devam
// ediyor. Sunucu kimliği MasakEsikleri.KimlikIstenenEnDusukTutar = min(370.000, 200.000) = 200.000'de
// istiyor; ekran formu kimlikEsigi'ne (370.000) göre açıyordu → müşterisiz 250.000 dekontta form hiç
// açılmıyor, sunucu her kayıtta 400 dönüyordu (çıkmaz).
// Düzeltme: /masak/esikler yanıtı kimlikIstenenEnDusukTutar taşır; Banka/Hurda/Islemler/FisProYanSutun
// formu bu tutarla açar (Esikler.KimlikIstenenTutar()). Sunucu alanı yoksa (eski sunucu) güvenli yön:
// iki eşiğin küçüğü. Eşik SAYISI istemciye gömülmez — sahte API'ler gerçek sunucu yanıtını taklit eder.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Masak370EkranTestleri : BunitContext
{
    // 07.10.2026 sonrası gerçek sunucu yanıtı (MasakModulu.EsikYaniti; canlı 5162'de okunan değerler).
    private const string EsikYeni =
        """{"kimlikEsigi":370000,"kimlikIstenenEnDusukTutar":200000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000.01,"ekBelgeEsigi":20000000.01,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""";
    // Aynı dönem, alanı göndermeyen ESKİ sunucu (yayın sırası farkı) — istemci güvenli yöne düşer.
    private const string EsikYeniAlansiz =
        """{"kimlikEsigi":370000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000.01,"ekBelgeEsigi":20000000.01,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""";
    // 06.10.2026 ve öncesi (dönem eşiği 185.000 < 200.000).
    private const string EsikEski =
        """{"kimlikEsigi":185000,"kimlikIstenenEnDusukTutar":185000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000,"ekBelgeEsigi":20000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""";
    private const string GereksinimAmac =
        """{"kademe":"AMAC_BEYANI","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""";
    private const string GereksinimYalnizKimlik =
        """{"kademe":"YALNIZ_KIMLIK","kimlikGerekli":true,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""";

    private readonly List<(string Metot, string Yol)> istekler = [];
    private readonly List<(string Parca, string Govde)> yollar = [];

    public Masak370EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<PosServisi>();
    }

    private sealed class SahteApi(Masak370EkranTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol));
            // /masak/gereksinim: gerçek sunucu kuralı (07.10 sonrası) — < 200.000 YOK, ≥ 200.000 kimlik + amaç.
            if (yol.Contains("/masak/gereksinim") && t.yollar.All(y => !y.Parca.StartsWith("/masak/gereksinim")))
            {
                var m = System.Text.RegularExpressions.Regex.Match(yol, @"tutar=([0-9.]+)");
                var tutar = m.Success ? decimal.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0m;
                return Task.FromResult(Yanit(tutar < 200_000m
                    ? """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":0}"""
                    : GereksinimAmac));
            }
            foreach (var (p, g) in t.yollar)
                if (yol.Contains(p)) return Task.FromResult(Yanit(g));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            { Content = new StringContent("""{"message":"tanımsız uç"}""", System.Text.Encoding.UTF8, "application/json") });
        }

        private static HttpResponseMessage Yanit(string govde) => new(HttpStatusCode.OK)
        { Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json") };
    }

    private void Yol(string parca, string govde) => yollar.Add((parca, govde));

    private void Oturum(string mod = ArayuzModuServisi.Basit) =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", null, mod));

    private bool GereksinimSoruldu(string tutar)
    {
        lock (istekler) return istekler.Any(i => i.Yol.Contains("/masak/gereksinim?tutar=" + tutar));
    }

    // ══ SAF: istemci sözleşmesi ═════════════════════════════════════════════

    [Fact]
    public void Sozlesme_sunucu_alani_varsa_O_kullanilir_yoksa_iki_esigin_KUCUGU_fail_closed()
    {
        var js = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
        var yeni = System.Text.Json.JsonSerializer.Deserialize<Esikler>(EsikYeni, js)!;
        Assert.Equal(370_000m, yeni.KimlikEsigi);
        Assert.Equal(200_000m, yeni.KimlikIstenenEnDusukTutar);
        Assert.Equal(200_000m, yeni.KimlikIstenenTutar());

        var alansiz = System.Text.Json.JsonSerializer.Deserialize<Esikler>(EsikYeniAlansiz, js)!;
        Assert.Equal(0m, alansiz.KimlikIstenenEnDusukTutar);
        Assert.Equal(200_000m, alansiz.KimlikIstenenTutar());            // eski sunucu: küçük olan

        var eski = System.Text.Json.JsonSerializer.Deserialize<Esikler>(EsikEski, js)!;
        Assert.Equal(185_000m, eski.KimlikIstenenTutar());               // 06.10 dönemi davranışı birebir

        // Amaç beyanı eşiği bilinmiyorsa (0) kimlik eşiği — sayı uydurulmaz.
        Assert.Equal(370_000m, new Esikler(370_000m, 0m, 0m, 0m, 20, []).KimlikIstenenTutar());
    }

    // ══ /banka dekont ═══════════════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Banka> BankaDekont(string esik, string mod = ArayuzModuServisi.Basit)
    {
        Oturum(mod);
        Yol("/masak/esikler", esik);
        Yol("/hareketler", """{"hareketler":[]}""");
        Yol("/bankalar", """{"hesaplar":[{"id":"b1","bankaAdi":"Ziraat","hesapAdi":"Ana Hesap","hesapSahibi":"X Ltd.","iban":"TR200000000000000000000001","paraBirimi":"TRY","bakiye":0,"aktif":true}]}""");
        Yol("/cari", """[{"id":"c1","ad":"Ayşe Yılmaz"}]""");
        var e = Render<Kuyumcu.Web.Pages.Banka>();
        e.WaitForElement("#bh-dekont-gir").Click();
        e.WaitForElement("#dekont-tutar");
        return e;
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Banka_musterisiz_250000_dekontta_MASAK_formu_ONCEDEN_acilir_kaydet_gerekceli_kilitli(string mod)
    {
        var e = BankaDekont(EsikYeni, mod);
        e.Find("#dekont-tutar").Input("250000");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#masak-belge-no")), TimeSpan.FromSeconds(3));
        Assert.True(GereksinimSoruldu("250000"));
        e.WaitForAssertion(() => Assert.True(e.Find("#dekont-kaydet").HasAttribute("disabled")));
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");   // çıkmaz yok: önce form, sonra kayıt
    }

    [Fact]
    public void Banka_musterisiz_199999_99_dekontta_MASAK_sorulmaz_form_yok()
    {
        var e = BankaDekont(EsikYeni);
        e.Find("#dekont-tutar").Input("199999,99");
        e.WaitForAssertion(() => Assert.False(e.Find("#dekont-kaydet").HasAttribute("disabled")));
        Assert.Empty(e.FindAll("#masak-belge-no"));
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Yol.Contains("/masak/gereksinim"));
    }

    [Fact]
    public void Banka_alani_gondermeyen_eski_sunucuda_da_250000_form_acilir()
    {
        var e = BankaDekont(EsikYeniAlansiz);
        e.Find("#dekont-tutar").Input("250000");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#masak-belge-no")), TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Banka_eski_donem_185000_esigi_birebir_190000_form_acilir()
    {
        Yol("/masak/gereksinim", GereksinimYalnizKimlik);
        var e = BankaDekont(EsikEski);
        e.Find("#dekont-tutar").Input("190000");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#masak-belge-no")), TimeSpan.FromSeconds(3));
        Assert.True(GereksinimSoruldu("190000"));
    }

    // ══ /hurda (müşterisiz alış) ═════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Hurda> HurdaEkrani()
    {
        Oturum();
        Yol("/masak/esikler", EsikYeni);
        Yol("/islemler/hurda-satis", """{"id":"h9","maden":"ALTIN","madenAdi":"Altın","hasGrami":1,"birimFiyatTlHasGram":6800,"toplamTl":6800,"tahsilEdilenTl":6800,"kalanTl":0,"kalanHurdaHasGram":8.16,"kasaBakiyesi":256800,"paraBirimi":"TRY","tarih":"2026-09-25T09:00:00Z","fireKaydiAcik":false,"fireNotu":null,"aliciKabulHasGrami":1,"tahlilFarkiHasGram":0}""");
        Yol("/hurda/bakiye", """{"subeId":"s1","satirlar":[{"maden":"ALTIN","madenAdi":"Altın","hasGram":60,"girenHas":60,"cikanHas":0,"tahlilFarkiHas":0}],"fireKaydiAcik":false,"fireNotu":null}""");
        Yol("/hurda/defter", """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}""");
        Yol("/kasalar", """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":250000,"aktif":true}]""");
        Yol("/kur/guncel", """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-25T09:00:00Z","ayarFiyatlari":[]}""");
        Yol("/arama", """{"sonuclar":[],"toplam":0}""");
        var e = Render<Kuyumcu.Web.Pages.Hurda>();
        e.WaitForAssertion(() => Assert.False(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        return e;
    }

    private static void Gir(IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e, string secici, string deger) =>
        e.WaitForAssertion(() =>
        {
            if (e.Find(secici).GetAttribute("value") != deger) e.Find(secici).Input(deger);
            Assert.Equal(deger, e.Find(secici).GetAttribute("value"));
        });

    [Fact]
    public void Hurda_musterisiz_204000_MASAK_alanlari_ONCEDEN_acilir_197200_acilmaz()
    {
        var e = HurdaEkrani();
        Gir(e, "#hrd-has", "29");                                        // 29 × 6.800 = 197.200 < 200.000
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(197_200m, "TRY"), e.Find("#hrd-tutar").TextContent));
        Assert.Empty(e.FindAll("#hrd-masak"));

        Gir(e, "#hrd-has", "30");                                        // 30 × 6.800 = 204.000 (kimlik eşiği 370.000'in altı)
        e.WaitForElement("#hrd-masak");
        Assert.True(GereksinimSoruldu("204000"));
        Assert.True(e.Find("#hrd-kaydet").HasAttribute("disabled"));
        Assert.Equal(Sozlukler.Tr.Banka.MasakKilitSebep, e.Find("#hrd-kaydet-kilit").TextContent.Trim());
    }

    // ══ /islemler PRO yan sütun ══════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> IslemlerPro(string esik)
    {
        Oturum(ArayuzModuServisi.Profesyonel);
        Yol("/cari/sistem-carisi", """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""");
        Yol("/tevsik/gunluk", """{"had":50000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":50000,"islemTutariKuraliDevrede":false}""");
        Yol("/fiyat/listeler", """[{"id":"fl1","ad":"Vitrin","varsayilan":true,"aktif":true}]""");
        Yol("/kur/guncel", """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""");
        Yol("/urunler", "[]");
        Yol("/kasalar", """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""");
        Yol("/subeler", """[{"id":"s1","ad":"Merkez","aktif":true}]""");
        Yol("/cari", """[{"id":"pm1","ad":"Peşin Müşteri"}]""");
        Yol("/masak/esikler", esik);
        Yol("/islemler/personeller", """{"secimSerbest":false,"oturumId":"u1","personeller":[{"id":"u1","ad":"Test","soyad":"Tezgâhtar"}],"dahaFazlasiVar":false}""");
        Yol("/reyon", "[]");
        Yol("/pos/cihazlar", "[]");
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-tur-SATIS");
        return e;
    }

    [Theory]
    [InlineData(EsikYeni)]
    [InlineData(EsikYeniAlansiz)]
    public void Islemler_PRO_yan_sutun_kimligin_ISTENDIGI_tutari_gosterir_370000_degil(string esik)
    {
        // YASAL-PARAM-EKRAN tur 2 (tasarım hakemi KRİTİK): kimliği isteyen 200.000 kademesi panelde
        // KAYBOLMAZ, ama "kimlik eşiği" adıyla değil AYRI satırda ("işlem amacıyla kimlik — teyit bekleniyor");
        // kimlik satırı md.5/1-b eşiğini (370.000, MASAK) söyler — kilit metniyle aynı kural.
        var e = IslemlerPro(esik);
        e.WaitForAssertion(() => Assert.Equal(
            string.Format(YasalParamSozlugu.Tr.PanelAmac, Bicim.Tl(200_000m)),
            e.Find("#islem-pro-amac-etiket").TextContent.Trim()));
        Assert.Equal(string.Format(SatisProSozlugu.Tr.KimlikEsigi, Bicim.Tl(370_000m)),
            e.Find("#islem-pro-kimlik-etiket").TextContent.Trim());
        Assert.DoesNotContain(Bicim.Tl(200_000m), e.Find("#islem-pro-kimlik-etiket").TextContent);
    }
}
