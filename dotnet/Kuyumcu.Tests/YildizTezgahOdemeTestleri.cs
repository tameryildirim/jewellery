using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-TEZGAH-ODEME — kurucu (10.10): "satış ekranında özellikle sağ tarafta ek ödeme
// indirim gibi kısımları kullanırken neyin ne olduğunu anlamadım … scroll vs çıkmadığından
// aşağıda kaldığından nasıl haberi olur".
//   • Ödeme satırları + haplar + açılan alanlar TEK kaydırma kabında; Fark + Kaydet dışarıda,
//     sütunun dibinde (geniş ekran) — kap kayınca ince çubuk + dipte çizgi işareti.
//   • Karma satırı [araç · kasa] [tutar alanı — çizgili] [Çıkar]; kasa adı kesilmez.
//   • Karmada "Çıkar" ile tek satır kalınca sayfanın MEVCUT "− Tek ödeme" yolu (FARK 0).
//   • "Açık hesap olarak kaydet" .b2 (birincil değil) ve yalnız fark > 0 + GERÇEK müşteri.
// Görünürlük/sığma kanıtı başsız Chrome ölçümüdür (rapor § 3); bu sınıf sözleşmeyi kilitler.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class YildizTezgahOdemeTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private sealed class SahteApi(YildizTezgahOdemeTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            var y = yol switch
            {
                var v when v.Contains("/gorunum") => """{"sema":"yildiz","yaziOlcegi":"18","temaModu":"karanlik"}""",
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Gram altın","ayarId":"K24","ayar":"24","hasGrami":0.995,"iscilikTl":0,"satisFiyati":10000,"birim":"ADET","milyem":995,"gramAgirlik":1}]""",
                var v when v.Contains("/kasalar/nakit-ayrimi") => "[]",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/yetkiler/benim") => """{"moduller":["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","kur","emanet"]}""",
                var v when v.Contains("/cari/sistem-carisi") => """{"id":"p1","ad":"Peşin Müşteri"}""",
                var v when v.Contains("/cari") => """[{"id":"p1","ad":"Peşin Müşteri","sistemCarisi":true},{"id":"c1","ad":"Selin Ergüven"}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var v when v.Contains("/pos/cihazlar") => "[]",
                var v when v.Contains("/islemler/satis") && istek.Method == HttpMethod.Post => """{"id":"h1","hareketId":"h1","fisNo":"F-0001","fisId":"f1"}""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    public YildizTezgahOdemeTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<HavaServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
    }

    // ── yardımcılar ──────────────────────────────────────────────────────────

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Satis(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Hakan", "Tunalı", "h@ornek.com", "SAHIP", "s1", mod, mod));
        Services.GetRequiredService<GorunumServisi>().YukleAsync().GetAwaiter().GetResult();
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#yildiz-tezgah", TimeSpan.FromSeconds(10));
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll(".yz-tezgah-kalem")), TimeSpan.FromSeconds(5));
        return e;
    }

    /// <summary>Yolu içeren son POST'un gövdesi; istemci üretimi kimlikler (ULID/GUID) çıkarılır.</summary>
    private string Govde(string yolParcasi)
    {
        string? g = null;
        Assert.True(SpinWait.SpinUntil(() =>
        {
            lock (istekler) g = istekler.LastOrDefault(i => i.Metot == "POST" && i.Yol.EndsWith(yolParcasi)).Govde;
            return g is not null;
        }, TimeSpan.FromSeconds(10)), yolParcasi + " POST'u gelmedi");
        var n = JsonNode.Parse(g!)!;
        void Temizle(JsonNode? d)
        {
            if (d is JsonObject o)
                foreach (var k in o.Select(p => p.Key).ToList())
                {
                    if (o[k] is JsonValue v && v.TryGetValue<string>(out var s)
                        && Regex.IsMatch(s, "^([0-9a-f]{8}-[0-9a-f]{4}-|[0-9A-HJKMNP-TV-Z]{26}$)", RegexOptions.IgnoreCase)) o.Remove(k);
                    else Temizle(o[k]);
                }
            else if (d is JsonArray a) foreach (var x in a) Temizle(x);
        }
        Temizle(n);
        return n.ToJsonString();
    }

    private (YildizTezgahBaglami B, List<string> Cagri) Baglam(bool karma, int odemeSayisi, bool teke = false,
        bool acikHesap = false, bool indirimGorunur = true, bool paraUstuKullanilir = false)
    {
        var cagri = new List<string>();
        EventCallback C(string ad) => EventCallback.Factory.Create(this, () => cagri.Add(ad));
        return (new YildizTezgahBaglami
        {
            TurAdi = "Satış", MusteriAdi = "Selin Ergüven",
            Kalemler = [new(0, "24", "Gram altın", "2 adet · 1,00 g · 1,9900 g has", "13.844,00 ₺")],
            ToplamMetni = "13.844,00 ₺",
            Odemeler = [.. Enumerable.Range(0, odemeSayisi).Select(i => new YildizTezgahOdeme(i, i == 0 ? "Nakit" : "Kart", "Ana Kasa", "0,00 ₺"))],
            Karma = karma, TekeIndirilebilir = teke, FarkMetni = acikHesap ? "3.844 ₺" : "0 ₺", FarkTamam = !acikHesap,
            AcikHesapGorunur = acikHesap, AcikHesapEtiketi = "Açık hesap olarak kaydet",
            KaydetEtiketi = "Kaydet ve yazdır", FisDolu = true, KalemVar = true,
            IndirimGorunur = indirimGorunur, ParaUstuKullanilir = paraUstuKullanilir, ParaUstuNakit = paraUstuKullanilir ? 13844m : 0m, IndirimAlani = b => b.AddMarkupContent(0, "<div id=\"sahte-indirim\"></div>"),
            OdemeDuzen = b => b.AddMarkupContent(0, "<div id=\"sahte-bacaklar\"></div>"),
            Kaydet = C("kaydet"), Vazgec = C("vazgec"), BaskaOdeme = C("baska"), TekOdeme = C("tek"), BacakEkle = C("bacak"),
            Indirim = C("indirim"), AcikHesap = C("acik"), MusteriDegistir = C("musteri"), TurDegistir = C("tur"),
        }, cagri);
    }

    private static string Css() =>
        Regex.Replace(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tema-yildiz.css"), @"/\*.*?\*/", "", RegexOptions.Singleline);

    /// <summary>tema-yildiz.css'in TEZGAH-ODEME bloğu (yorumsuz).</summary>
    private static string Blok()
    {
        var c = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tema-yildiz.css");
        var i = c.IndexOf("/* TEZGAH-ODEME */", StringComparison.Ordinal);
        Assert.True(i > 0, "tema-yildiz.css'te TEZGAH-ODEME bloğu yok");
        var son = c.IndexOf("/* TEZGAH-ODEME sonu */", i, StringComparison.Ordinal);
        Assert.True(son > i, "TEZGAH-ODEME bloğunun sonu yok");
        return Regex.Replace(c[i..son], @"/\*.*?\*/", "", RegexOptions.Singleline);
    }

    private static string Kural(string css, string secici)
    {
        var i = css.IndexOf(secici + " {", StringComparison.Ordinal);
        Assert.True(i >= 0, "kural yok: " + secici);
        var a = css.IndexOf('{', i);
        return css[(a + 1)..css.IndexOf('}', a)];
    }

    // ── bUnit: bileşen ───────────────────────────────────────────────────────

    [Fact]
    public void YTO_01_odeme_satirlari_haplar_ve_acilan_alanlar_tek_kaydirma_kabinda_fark_kaydet_disarida()
    {
        var (b, _) = Baglam(karma: true, odemeSayisi: 2);
        var e = Render<YildizTezgah>(p => p.Add(x => x.B, b));
        var sag = e.Find("aside.yz-tezgah-sag");
        var cocuklar = sag.Children.Select(c => c.Id is { Length: > 0 } id ? "#" + id : "." + c.ClassList.First()).ToList();
        // tur 2 (hakem KRİTİK "haplar kabın dibinde yarıdan kesik"): hap sırası kabın DIŞINDA, hemen ardında (tuval sırası).
        Assert.Equal(["#yildiz-tezgah-odeme-baslik", "#yildiz-tezgah-odeme-govde", "#yildiz-tezgah-ekler", ".yz-tezgah-bosluk", "#yildiz-tezgah-alt"], cocuklar);
        var govde = e.Find("#yildiz-tezgah-odeme-govde");
        Assert.Contains("yz-tezgah-odeme-govde", govde.ClassList);
        foreach (var s in new[] { "#yildiz-tezgah-odeme-duzen", "#sahte-bacaklar", "#yildiz-tezgah-indirim-alan" })
            Assert.NotNull(govde.QuerySelector(s));
        Assert.Null(govde.QuerySelector("#yildiz-tezgah-ekler, #yildiz-tezgah-baska-odeme, #yildiz-tezgah-indirim, #yildiz-tezgah-tek-odeme"));
        Assert.Null(govde.QuerySelector("#yildiz-tezgah-fark, #yildiz-tezgah-kaydet, #yildiz-tezgah-vazgec"));
        // tek ödemede de satır kabın içinde
        var (t, _) = Baglam(karma: false, odemeSayisi: 1);
        var e2 = Render<YildizTezgah>(p => p.Add(x => x.B, t));
        Assert.NotNull(e2.Find("#yildiz-tezgah-odeme-govde").QuerySelector("#yildiz-odeme-0"));
    }

    [Fact]
    public void YTO_02_acik_hesap_birincil_degil_b2_ekranda_tek_birincil_kaydet()
    {
        var (b, cagri) = Baglam(karma: true, odemeSayisi: 1, acikHesap: true);
        var e = Render<YildizTezgah>(p => p.Add(x => x.B, b));
        var ah = e.Find("#yildiz-tezgah-acik-hesap");
        Assert.Contains("yz-b2", ah.ClassList);
        Assert.DoesNotContain("yz-b3", ah.ClassList);
        Assert.DoesNotContain("yz-b1", ah.ClassList);
        Assert.Equal(["yildiz-tezgah-kaydet"], e.FindAll(".yz-b1").Select(x => x.Id).ToList());
        ah.Click();
        Assert.Equal(["acik"], cagri);
    }

    [Fact]
    public void YTO_03_karmada_cikarla_tek_satir_kalinca_tek_odeme_yolu_bir_kez_cagrilir()
    {
        var (iki, cagri) = Baglam(karma: true, odemeSayisi: 2);
        var e = Render<YildizTezgah>(p => p.Add(x => x.B, iki));
        Assert.Empty(cagri);
        var (bir, _) = Baglam(karma: true, odemeSayisi: 1, teke: true);
        var birC = new YildizTezgahBaglami { Karma = true, TekeIndirilebilir = true, Odemeler = bir.Odemeler, KalemVar = true, TekOdeme = iki.TekOdeme, KaydetEtiketi = "K" };
        e.Render(p => p.Add(x => x.B, birC));
        Assert.Equal(["tek"], cagri);
        // sayfa tek ödemeye döndü → yeniden çağrılmaz
        e.Render(p => p.Add(x => x.B, new YildizTezgahBaglami { Karma = false, Odemeler = bir.Odemeler, TekOdeme = iki.TekOdeme, KaydetEtiketi = "K" }));
        Assert.Equal(["tek"], cagri);
    }

    [Fact]
    public void YTO_04_tek_satir_tek_odemeye_uygun_degilse_ya_da_bastan_tek_satirsa_dokunulmaz()
    {
        // eski altın/veresiye/döviz kalan satır (TekeIndirilebilir=false) → karmada kalır
        var (iki, cagri) = Baglam(karma: true, odemeSayisi: 2);
        var e = Render<YildizTezgah>(p => p.Add(x => x.B, iki));
        e.Render(p => p.Add(x => x.B, new YildizTezgahBaglami { Karma = true, TekeIndirilebilir = false, Odemeler = [new(0, "Eski altın", null, "0,00 ₺")], TekOdeme = iki.TekOdeme }));
        Assert.Empty(cagri);
        // karmaya TEK satırla girilen yol (eski altın OdemeyiBolIlk) — 0 → 1: çağrılmaz
        var (bas, cagri2) = Baglam(karma: false, odemeSayisi: 1);
        var e2 = Render<YildizTezgah>(p => p.Add(x => x.B, bas));
        e2.Render(p => p.Add(x => x.B, new YildizTezgahBaglami { Karma = true, TekeIndirilebilir = true, Odemeler = bas.Odemeler, TekOdeme = bas.TekOdeme }));
        Assert.Empty(cagri2);
    }

    // ── bUnit: sayfa (gövde) ─────────────────────────────────────────────────

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void YTO_05_cikar_sonrasi_tek_odeme_govdesi_elle_secilen_tek_odemeyle_ayni_fark_sifir(string mod)
    {
        // Otomatik: + Başka ödeme (Nakit tam + Kart 0) → Nakit satırını Çıkar → kalan Kart tek ödemeye iner
        var e = Satis(mod);
        e.Find("#yildiz-tezgah-baska-odeme").Click();
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll(".bacak-satiri").Count));
        e.Find("#islem-bacak-sil-0").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll(".bacak-satiri")), TimeSpan.FromSeconds(5));
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-odeme-0")));
        Assert.StartsWith("Kart", e.Find("#yildiz-odeme-0 .yz-tezgah-arac").TextContent.Trim());
        Assert.Equal("0 ₺", e.Find("#yildiz-tezgah-fark-tutar").TextContent.Replace('\u00a0', ' ').Trim());
        Assert.Empty(e.FindAll("#yildiz-tezgah-tek-odeme"));
        e.Find("#yildiz-tezgah-kaydet").Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        var oto = Govde("/islemler/satis");
        Assert.Contains("\"odemeTuru\":\"KART\"", oto);

        // Elle (mevcut yol, iki modda da var): yeni fiş, + Başka ödeme → ilk satırı Kart yap → "− Tek ödeme" → Kaydet
        e.Dispose();
        lock (istekler) istekler.Clear();
        var m = Satis(mod);
        m.Find("#yildiz-tezgah-baska-odeme").Click();
        m.WaitForAssertion(() => Assert.Equal(2, m.FindAll(".bacak-satiri").Count));
        SeciciYardim.Sec(m, "islem-bacak-0-arac", "KART");
        m.Find("#yildiz-tezgah-tek-odeme").Click();
        m.WaitForAssertion(() => Assert.Empty(m.FindAll(".bacak-satiri")), TimeSpan.FromSeconds(5));
        m.Find("#yildiz-tezgah-kaydet").Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(m);
        Assert.Equal(Govde("/islemler/satis"), oto);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void YTO_06_acik_hesap_yalniz_fark_varken_gercek_musteride_gorunur_govde_veresiye_bacagi(string mod)
    {
        var e = Satis(mod);
        e.Find("#yildiz-tezgah-baska-odeme").Click();
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll(".bacak-satiri").Count));
        e.Find("#islem-bacak-0-tutar").Input("4000");
        e.Find("#islem-bacak-0-tutar").Blur();
        e.Find("#islem-bacak-1-tutar").Input("0");
        e.Find("#islem-bacak-1-tutar").Blur();
        e.WaitForAssertion(() => Assert.Equal("6.000 ₺", e.Find("#yildiz-tezgah-fark-tutar").TextContent.Replace('\u00a0', ' ').Trim()));
        // Peşin Müşteri: fark var ama düğme YOK (kilitli düğme gösterilmez)
        Assert.Empty(e.FindAll("#yildiz-tezgah-acik-hesap"));
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-musteri-sec-c1")));
        e.Find("#yildiz-musteri-sec-c1").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-tezgah-acik-hesap")), TimeSpan.FromSeconds(5));
        Assert.Contains("yz-b2", e.Find("#yildiz-tezgah-acik-hesap").ClassList);
        e.Find("#yildiz-tezgah-acik-hesap").Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        var g = JsonNode.Parse(Govde("/islemler/satis-fisi"))!;
        var bacak = g["odemeler"]!.AsArray().Select(x => $"{x!["arac"]}={(decimal)x["tutarTl"]!}").ToList();
        Assert.Contains("VERESIYE=6000", bacak);
        Assert.Contains("NAKIT=4000", bacak);
        // fark 0 iken (tek ödeme) gerçek müşteride de düğme yok
        e.Dispose();
        var t = Satis(mod);
        Assert.Empty(t.FindAll("#yildiz-tezgah-acik-hesap"));
    }

    // ── CSS sözleşmesi ───────────────────────────────────────────────────────

    [Fact]
    public void YTO_07_css_genis_ekranda_govde_kayar_fark_kaydet_sabit_kaydirma_isareti()
    {
        var b = Blok();
        var kok = "html:root[data-sema=\"yildiz\"] .yz-tezgah";
        var genis = b[b.IndexOf("@media (min-width: 1001px)", StringComparison.Ordinal)..];
        var govde = Kural(genis, $"  {kok} .yz-tezgah-sag > .yz-tezgah-odeme-govde");
        Assert.Contains("overflow-y: auto;", govde);
        Assert.Contains("min-height: 0;", govde);
        Assert.Contains("flex: 0 1 auto;", govde);
        Assert.Contains("linear-gradient(var(--kenarlik), var(--kenarlik)) bottom / 100% 1px no-repeat scroll", govde);   // "aşağıda daha var" çizgisi
        Assert.Contains("no-repeat local", govde);                                                                       // sona gelince örtülür
        // tur 2 (hakem KRİTİK "işaret yalnız 1 px çizgi"): kap kayıyorsa altta/üstte belirteç renkli gölge (yerinde örtü kalkar).
        Assert.Contains("--yz-kaydir-golge: color-mix(in srgb, var(--metin) 16%, transparent);", govde);   // renk belirteçten (Y1_10)
        Assert.Contains("linear-gradient(transparent, var(--yz-kaydir-golge)) bottom / 100% 14px no-repeat scroll", govde);
        Assert.Contains("linear-gradient(var(--yz-kaydir-golge), transparent) top / 100% 14px no-repeat scroll", govde);
        Assert.Contains("linear-gradient(var(--yuzey-govde), var(--yuzey-govde)) bottom / 100% 16px no-repeat local", govde);
        Assert.Contains("linear-gradient(var(--yuzey-govde), var(--yuzey-govde)) top / 100% 16px no-repeat local", govde);
        Assert.Contains("flex-shrink: 0;", Kural(genis, $"  {kok} .yz-tezgah-sag > .yz-tezgah-odeme-govde > *"));
        Assert.Contains("overflow: visible;", Kural(genis, $"  {kok} .yz-tezgah-odeme-govde > .yz-tezgah-duzen"));
        Assert.Contains("background: var(--kenarlik);", Kural(genis, $"  {kok} :is(.yz-tezgah-odeme-govde, .yz-tezgah-kalemler)::-webkit-scrollbar-thumb"));
        Assert.Contains("gap: 28px;", Kural(b, $"{kok} .yz-tezgah-odeme-govde"));   // tuval aside aralığı
        var dar = b[b.IndexOf("@media (max-width: 1000px)", StringComparison.Ordinal)..];
        Assert.Contains("display: contents;", Kural(dar, $"  {kok} .yz-tezgah-odeme-govde"));
        // tur 2: dar ekranda açılan alan sayfanın dibine yapışık Fark/Kaydet şeridinin ARKASINA kaydırılmaz.
        var pay = Kural(dar, $"  {kok} .yz-tezgah-sag :is(.yz-tezgah-duzen, .bacak-satiri input)");
        Assert.Contains("scroll-margin-bottom: 40vh;", pay);
        Assert.Contains("scroll-margin-top: 96px;", pay);
        // tur 2: kısa ekranda aralıklar daralır — tek ödeme satırı kaptan kırpılmaz. Görsel denetim 2 (23.10): eşik 767 → 840 px
        // (1280×800 ve 1024×768'de 303 px yapışkan blok "+ İndirim" düğmesini 7 px örtüyordu; aralık 16 ile blok 267 px).
        var kisa = b[b.IndexOf("@media (min-width: 1001px) and (max-height: 840px)", StringComparison.Ordinal)..];
        Assert.Contains("gap: 16px;", Kural(kisa, $"  {kok} :is(.yz-tezgah-sag, .yz-tezgah-alt, .yz-tezgah-sag > .yz-tezgah-odeme-govde)"));
    }

    [Fact]
    public void YTO_08_css_karma_satiri_tutar_alani_cizgili_cikar_sonda_kasa_kesilmez()
    {
        var b = Blok();
        var kok = "html:root[data-sema=\"yildiz\"] .yz .yz-tezgah-karma .bacak-satiri";
        Assert.Contains("grid-template-columns: minmax(0, 1fr) 150px auto;", Kural(b, kok));
        Assert.Contains("grid-column: 2;", Kural(b, $"{kok} > .alan[id$=\"-tutar-sar\"]"));
        Assert.Contains("grid-column: 3;", Kural(b, $"{kok} > .yz-bacak-sil"));
        Assert.Contains("border-bottom: 1px solid var(--kenarlik);", Kural(b, $"{kok} > .alan[id$=\"-tutar-sar\"] .alan-girdi-satir:not(#_)"));
        var kasa = Kural(b, $"{kok} .yz-bacak-kasa");
        Assert.Contains("overflow: visible;", kasa);
        Assert.Contains("text-overflow: clip;", kasa);
        Assert.Contains("flex-wrap: wrap;", Kural(b, $"{kok} > .yz-bacak-ad"));
        var dar = b[b.LastIndexOf("@media (max-width: 1000px)", StringComparison.Ordinal)..];
        Assert.Contains("grid-template-columns: minmax(0, 1fr) 130px auto;", Kural(dar, $"  {kok}"));
        // tur 2: uzun müşteri satırında (375 px, "· BORCU 4.734,30 ₺") "Değiştir" daralıp taşmaz (ölçer K3 = 0); satır yazısı sarar.
        Assert.Contains("flex-shrink: 0;", Kural(b, "html:root[data-sema=\"yildiz\"] .yz-tezgah .yz-tezgah-ust-satir > .yz-tezgah-degistir"));
        var ah = Kural(b, "html:root[data-sema=\"yildiz\"] .yz-tezgah .yz-tezgah-fark > .yz-b2.yz-tezgah-acik-hesap:not(#_)");
        Assert.Contains("height: 32px;", ah);   // tuval "Değiştir" küçük .b2
        Assert.Contains("font-size: 14px;", ah);
    }

    [Fact]
    public void YTO_09_css_blok_yalniz_yildiz_kapsaminda_sabit_renksiz()
    {
        var b = Blok();
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b(?![-\w])", Regex.Replace(b, @"#[\w-]+[\s,{:.>\[]", " "));
        Assert.DoesNotContain("rgb(", b);
        foreach (Match m in Regex.Matches(b, @"(?m)^\s*([^@{}\s][^{}]*)\{"))
            foreach (var s in Regex.Split(m.Groups[1].Value, @",(?![^(]*\))").Select(x => x.Trim()).Where(x => x.Length > 0))
                Assert.StartsWith("html:root[data-sema=\"yildiz\"]", s);
        Assert.DoesNotContain("!important", b);
    }

    // ── tur 2 (tasarım hakemi KRİTİK): açılan alan görünür bölgeye kaydırılır ─────────────────

    private List<string> Kaydirilan() => JSInterop.Invocations
        .Where(i => i.Identifier == "document.querySelector").Select(i => (string)i.Arguments[0]!).ToList();

    [Fact]
    public void YTO_10_indirim_acilinca_alan_kap_icinde_gorunur_bolgeye_kaydirilir_ilk_cizimde_kaydirilmaz()
    {
        var (kapali, _) = Baglam(karma: true, odemeSayisi: 2, indirimGorunur: false);
        var e = Render<YildizTezgah>(p => p.Add(x => x.B, kapali));
        Assert.Empty(Kaydirilan());
        var (acik, _) = Baglam(karma: true, odemeSayisi: 2, indirimGorunur: true);
        e.Render(p => p.Add(x => x.B, acik));
        Assert.Equal(["#yildiz-tezgah-indirim-alan"], Kaydirilan());
        e.Render(p => p.Add(x => x.B, acik));                // açık kalan alan yeniden kaydırılmaz
        Assert.Single(Kaydirilan());
        // baştan açık çizilen alan (sayfa yenilendi, iskonto kayıtlı) kaydırılmaz — kullanıcı açmadı
        var (bastan, _) = Baglam(karma: false, odemeSayisi: 1, indirimGorunur: true);
        Render<YildizTezgah>(p => p.Add(x => x.B, bastan));
        Assert.Single(Kaydirilan());
    }

    [Fact]
    public void YTO_11_yeni_odeme_satiri_ve_para_ustu_acilinca_gorunur_bolgeye_kaydirilir()
    {
        var (tek, _) = Baglam(karma: false, odemeSayisi: 1, indirimGorunur: false, paraUstuKullanilir: true);
        var e = Render<YildizTezgah>(p => p.Add(x => x.B, tek));
        e.Find("#yildiz-tezgah-para-ustu").Click();
        Assert.NotNull(e.Find("#yildiz-tezgah-para-ustu-alan"));
        Assert.Equal(["#yildiz-tezgah-para-ustu-alan"], Kaydirilan());
        var (iki, _) = Baglam(karma: true, odemeSayisi: 2, indirimGorunur: false);
        e.Render(p => p.Add(x => x.B, iki));                 // + Başka ödeme: tek → karma iki satır
        Assert.Equal("#islem-bacak-1-tutar", Kaydirilan()[^1]);
        var (uc, _) = Baglam(karma: true, odemeSayisi: 3, indirimGorunur: false);
        e.Render(p => p.Add(x => x.B, uc));
        Assert.Equal("#islem-bacak-2-tutar", Kaydirilan()[^1]);
        var n = Kaydirilan().Count;
        e.Render(p => p.Add(x => x.B, iki));                 // satır çıkarıldı → kaydırma yok
        Assert.Equal(n, Kaydirilan().Count);
        var cagri = JSInterop.Invocations.Where(i => i.Identifier == "document.querySelector").ToList();
        Assert.All(cagri, i => Assert.Single(i.Arguments));
    }
}
