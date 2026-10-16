using AngleSharp.Dom;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET CUMA-S — SATIŞ BASİT: tevsik/kimlik tetiklenince ALDIM (ödeme) sütunu SADE
//
// Kaynak: orkestratör görsel kontrolü 25.09 03:05 (a,b). Basit modda Peşin Müşteri +
// nakit toplam sunucunun haddine ulaşınca ALDIM sütununda ödeme türü seçici + uzun
// kırmızı metin + eski altın ipucu + alınan/para üstü + düğmeler üst üste yığılıyor,
// alt eylem satırı 1440×900'ün altına taşıyordu (tarayıcıda ölçüldü: 949 > 900).
//
//   • BASİT tezgâh fişi: tek satır kısa durum ("Kimlik gerekli" / "Nakit alınmaz") +
//     katlanır "Neden?" ayrıntısı (kapalı). Uzun kırmızı metin YOK; ipucu VAR.
//   • BASİT: nakit reddedilirken para üstü çizilmez; eski altın ipucu görsel gizli.
//   • Sınırın altında: durum satırı yok, para üstü geri gelir.
//   • PRO DEĞİŞMEDİ: uzun kırmızı öneri metni aynen, sade satır yok.
//
// ⚠ Eşik istemcide YOK: had sahte API'nin /tevsik/gunluk yanıtıdır (sunucu).
// ⚠ Para metni sabit yazılmaz: Bicim.Tl(..) ile üretilir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class CumaSAldimSadeTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public CumaSAldimSadeTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    /// <summary>Sunucunun bildirdiği had (sahte /tevsik/gunluk) — istemci bu sayıyı GÖMMEZ, ekrandan okur.</summary>
    private const decimal SunucuHaddi = 30000m;

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, yol, govde));
            if (yol.Contains("/urunler/barkod/"))
                return Yanit(System.Net.HttpStatusCode.NotFound, """{"message":"bulunamadı"}""");
            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") =>
                    """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":20000,"hasDegeri":19000,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":20000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":3.94,"iscilikTl":0,"satisFiyati":20000,"gramAgirlik":4.3},{"id":"u2","ad":"Tek Tas Yuzuk","ayarId":"K14","hasGrami":1.2,"iscilikTl":0,"satisFiyati":20000,"gramAgirlik":2}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/iade-adaylari") => "[]",
                var v when v.Contains("/reyon") => "[]",
                var v when v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return Yanit(System.Net.HttpStatusCode.OK, y);
        }

        private static HttpResponseMessage Yanit(System.Net.HttpStatusCode kod, string govde) =>
            new(kod) { Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json") };
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-tur-SATIS");
        e.WaitForAssertion(() => Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        return e;
    }

    private static void KalemEkle(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string urunId, int beklenen)
    {
        SeciciYardim.Sec(e, "islem-urun", urunId);
        e.WaitForAssertion(() => Assert.Equal(urunId, SeciciYardim.SeciliDeger(e, "islem-urun")));
        e.WaitForAssertion(() => Assert.Equal("20000", SeciciYardim.SeciliDeger(e, "islem-birimfiyat")));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.Equal(beklenen, e.FindAll("#islem-kalem-tablo tbody tr, #islem-kalem-pro tr.fis-pro-satir").Count), TimeSpan.FromSeconds(5));
    }

    /// <summary>2 × 20.000 = 40.000 ₺ NAKİT (sunucunun haddinin üstü) — yasal kapı tetiklenir.</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> TevsikliFis(string mod)
    {
        var e = Ekran(mod);
        KalemEkle(e, "u1", 1);
        KalemEkle(e, "u2", 2);
        Satis4Yardim.NakitElleSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        return e;
    }

    /// <summary>PRO/basit sözlüklerdeki UZUN öneri metni (had sunucudan) — basit ALDIM'da görünmemeli.</summary>
    private static string[] UzunMetinler() =>
    [
        string.Format(Sozlukler.Getir("tr").Islem.TevsikNakitOnerisi, Bicim.Tl(SunucuHaddi)),
        string.Format(BasitSozluk.Getir("tr").Islem.TevsikNakitOnerisi, Bicim.Tl(SunucuHaddi)),
    ];

    /// <summary>Kapalı &lt;details&gt; ya da .gorsel-gizli içinde OLMAYAN (yani ekranda görünen) öğe mi?</summary>
    private static bool Gorunur(IElement x) =>
        x.Closest(".gorsel-gizli") is null
        && (x.Closest("details") is not { } d || d.HasAttribute("open") || x.Closest("summary") is not null);

    // ═══ BASİT ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CUMA_S_01_Basit_tevsik_tetiklenince_engel_TEK_bantta_ALDIM_kisa_durum_ve_uzun_metin_YOK()
    {
        var e = TevsikliFis(ArayuzModuServisi.Basit);
        // YASAL-KAPI-3 tur 2 (tasarım hakemi KRİTİK "aynı uyarı 2-5 kez"): engel üst bantta TEK cümle — ödeme sütununda
        // ikinci kısa durum, "Neden?" ve ayrıntı ÇİZİLMEZ (hakemin sayacı görünmez/katlı kopyayı da sayıyordu).
        e.WaitForElement("#islem-yasal-kapi", TimeSpan.FromSeconds(5));
        var aldim = e.Find("#islem-aldim");
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#islem-yasal-kapi").TextContent));
        Assert.Empty(e.FindAll("#islem-tevsik-durum"));
        Assert.Empty(e.FindAll("#islem-tevsik-neden"));
        Assert.Empty(e.FindAll("#islem-tevsik-ipucu"));
        Assert.Empty(e.FindAll("#islem-odeme-otomatik"));

        // Uzun kırmızı metin ALDIM'da HİÇ yok; görünen kırmızı metinlerin hepsi kısa.
        foreach (var uzun in UzunMetinler()) Assert.DoesNotContain(uzun, aldim.TextContent);
        var gorunenKirmizi = aldim.QuerySelectorAll(".metin-kirmizi").Where(Gorunur).Select(x => x.TextContent.Trim()).ToList();
        Assert.All(gorunenKirmizi, t => Assert.True(t.Length <= 24, $"ALDIM'da uzun kırmızı metin görünüyor: '{t}'"));

        // Eski altın ipucu görsel gizli (aynı metin düğmenin title'ında); para üstü nakit reddedilirken yok.
        Assert.Contains("gorsel-gizli", e.Find("#islem-odeme-altin-ipucu").ClassName);
        Assert.Empty(e.FindAll("#islem-odeme-altin"));   // k2 (tasarım hakemi K19): ayrı düğme yok, tek kapı "+ Başka ödeme ekle"
        Assert.Empty(e.FindAll("#islem-para-ustu-sar"));

        // Ödeme türü seçici (kapı açık) yerinde — kullanıcı kart/havaleye geçebilir.
        Assert.NotEmpty(e.FindAll("#islem-odeme"));
    }

    [Fact]
    public void CUMA_S_02_Basit_gercek_musteride_nakit_engeli_TEK_bantta_Pesin_cumlesi_YOK()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.WaitForAssertion(() => Assert.Equal("c1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        KalemEkle(e, "u1", 1);
        KalemEkle(e, "u2", 2);
        Satis4Yardim.NakitElleSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        // YASAL-KAPI-3 tur 2: nakit engeli TEK yerde TEK cümle; ödeme sütununda kısa durum/"Neden?" yok, Peşin cümlesi yok.
        e.WaitForAssertion(() => Assert.Contains("nakit", e.Find("#islem-kaydet").GetAttribute("title") ?? "", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(5));
        var cumle = e.Find("#islem-kaydet").GetAttribute("title")!;
        var tum = string.Concat(e.Nodes.Select(n => n.TextContent));
        Assert.Equal(1, tum.Split(cumle).Length - 1);   // engel cümlesi TEK kez (tezgâh fişinde Kaydet'in yanında; görünmez kopya yok)
        Assert.Empty(e.FindAll("#islem-tevsik-durum"));
        Assert.DoesNotContain(SatisAldimSozlugu.Tr.AyrintiPesin, e.Markup);
    }

    [Fact]
    public void CUMA_S_03_Basit_sinirin_altinda_durum_satiri_YOK_para_ustu_VAR()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        KalemEkle(e, "u1", 1);   // 20.000 < sunucu haddi
        e.WaitForElement("#islem-para-ustu-ac", TimeSpan.FromSeconds(5));   // SATIS-3 t22: para üstü kapalı "+ Para üstü hesapla" arkasında
        Assert.Empty(e.FindAll("#islem-tevsik-durum"));
        Assert.Empty(e.FindAll("#islem-tevsik-ipucu"));
    }

    // ═══ PRO — DEĞİŞMEDİ ═════════════════════════════════════════════════════

    [Fact]
    public void CUMA_S_04_Pro_satista_tevsik_engeli_TEK_yerde_Kaydet_yaninda()
    {
        // PAKET SATIS-2 (kurucu temel kuralı 25.09 11:35, CUMA-S bu pakete birleşti): PRO satışta da
        // uzun kırmızı metin ekranı doldurmaz — tek satır kısa durum + "Neden?" katlı ayrıntı (bilgi SİLİNMEDİ).
        var e = TevsikliFis(ArayuzModuServisi.Profesyonel);
        // YASAL-KAPI-3 tur 2: PRO'da engelin TEK yeri Kaydet'in yanı; ödeme sütununda kısa durum/"Neden?" ikinci kez çizilmez.
        e.WaitForAssertion(() => Assert.False(string.IsNullOrWhiteSpace(e.Find("#islem-kaydet-kilit").TextContent)), TimeSpan.FromSeconds(5));
        Assert.Empty(e.FindAll("#islem-tevsik-durum"));
        Assert.Empty(e.FindAll("#islem-tevsik-ipucu"));
        Assert.Contains("gorsel-gizli", e.Find("#islem-odeme-altin-ipucu").ClassName);
    }
}
