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

    // TEMA TEMİZLİĞİ 17.10: SATIŞ = Yıldız tezgâhı (BASİT ve PRO). Eski BASİT "ALDIM" sütununun karşılığı tezgâhın sağ
    // sütunu (aside.yz-tezgah-sag: ödeme satırları · haplar · Fark bloğu). Engelin TEK yeri Fark bloğunun not satırı
    // (#yildiz-tezgah-not, Kaydet'in hemen üstü); özgün bantlar (#islem-yasal-kapi …) Yıldız'da CSS ile gizlidir
    // (tema-yildiz.css "TEZGÂH tur 2") — görünür metin sayılırken onlar düşülür (CSS kuralı da sınanır).
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        TemaTemizligiGDYardimcisi.MusteriBekle(e, "Peşin Müşteri");
        return e;
    }

    private static void KalemEkle(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string urunId, int beklenen)
    {
        SeciciYardim.Sec(e, "islem-urun", urunId);
        e.WaitForAssertion(() => Assert.Equal(urunId, SeciciYardim.SeciliDeger(e, "islem-urun")));
        e.WaitForAssertion(() => Assert.Equal("20000", SeciciYardim.SeciliDeger(e, "islem-birimfiyat")));
        e.Find("#islem-kalem-ekle").Click();
        TemaTemizligiGDYardimcisi.KalemSayisiBekle(e, beklenen);
    }

    /// <summary>2 × 20.000 = 40.000 ₺ NAKİT (sunucunun haddinin üstü) — yasal kapı tetiklenir.</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> TevsikliFis(string mod)
    {
        var e = Ekran(mod);
        KalemEkle(e, "u1", 1);
        KalemEkle(e, "u2", 2);
        TemaTemizligiGDYardimcisi.OdemeAc(e);   // Yıldız: ödeme satırına dokun → özgün ödeme seçicisi
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

    /// <summary>Yıldız tezgâhında CSS ile gizlenen özgün bantlar (tema-yildiz.css "TEZGÂH tur 2" kuralıyla AYNI liste).</summary>
    private const string YildizdaGizliBantlar = "#hata-bandi, #hata-ayrinti, #basari-bandi, #islem-yazdir-sar, #islem-yazdir-hata, "
        + "#islem-taksit-uyari, #islem-taksit-hata, #islem-yerlesim-uyari, #islem-yasal-kapi";

    private static int Say(string metin, string parca) => metin.Split(parca).Length - 1;

    /// <summary>Cümlenin GÖRÜNÜR metindeki sayısı: sayfa metni − Yıldız'da gizli bantların içindekiler (kural CSS'te sınanır).</summary>
    private static int GorunurSayi(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string cumle)
    {
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tema-yildiz.css");
        Assert.Contains(".yz-islem :is(#hata-bandi, #hata-ayrinti, #basari-bandi, #islem-yazdir-sar, #islem-yazdir-hata,", css);
        Assert.Contains("#islem-taksit-uyari, #islem-taksit-hata, #islem-yerlesim-uyari, #islem-yasal-kapi) { display: none; }", css);
        Assert.NotNull(e.Find(".islem-sayfa.yz-islem"));   // kural bu kapta geçerli
        var tum = Say(string.Concat(e.Nodes.Select(n => n.TextContent)), cumle);
        var gizli = e.FindAll(YildizdaGizliBantlar).Sum(x => Say(x.TextContent, cumle));
        return tum - gizli;
    }

    // ═══ BASİT ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CUMA_S_01_Basit_tevsik_tetiklenince_engel_TEK_bantta_ALDIM_kisa_durum_ve_uzun_metin_YOK()
    {
        var e = TevsikliFis(ArayuzModuServisi.Basit);
        // YASAL-KAPI-3 tur 2 (tasarım hakemi KRİTİK "aynı uyarı 2-5 kez"): engel TEK cümle — Yıldız'da Fark bloğunun not satırı
        // (kötü); ödeme sütununda ikinci kısa durum, "Neden?" ve ayrıntı ÇİZİLMEZ.
        e.WaitForAssertion(() => Assert.Contains("yz-kotu", e.Find("#yildiz-tezgah-not").ClassName), TimeSpan.FromSeconds(5));
        var not = e.Find("#yildiz-tezgah-not").TextContent.Trim();
        Assert.False(string.IsNullOrWhiteSpace(not));
        Assert.Equal(1, GorunurSayi(e, not));
        Assert.Empty(e.FindAll("#islem-tevsik-durum"));
        Assert.Empty(e.FindAll("#islem-tevsik-neden"));
        Assert.Empty(e.FindAll("#islem-tevsik-ipucu"));
        Assert.Empty(e.FindAll("#islem-odeme-otomatik"));

        // Uzun kırmızı metin ödeme sütununda HİÇ yok; not satırı dışındaki görünen kırmızı metinlerin hepsi kısa.
        var aldim = e.Find("aside.yz-tezgah-sag");
        foreach (var uzun in UzunMetinler()) Assert.DoesNotContain(uzun, aldim.TextContent);
        var gorunenKirmizi = aldim.QuerySelectorAll(".metin-kirmizi, .yz-kotu").Where(Gorunur)
            .Where(x => x.Id is not ("yildiz-tezgah-not" or "yildiz-tezgah-hata")).Select(x => x.TextContent.Trim()).ToList();
        Assert.All(gorunenKirmizi, t => Assert.True(t.Length <= 24, $"ödeme sütununda uzun kırmızı metin görünüyor: '{t}'"));

        // Eski altın ipucu görsel gizli (aynı metin düğmenin title'ında); para üstü nakit reddedilirken yok.
        Assert.Contains("gorsel-gizli", e.Find("#islem-odeme-altin-ipucu").ClassName);
        Assert.Empty(e.FindAll("#islem-odeme-altin"));   // k2 (tasarım hakemi K19): ayrı düğme yok, tek kapı "+ Başka ödeme"
        Assert.Empty(e.FindAll("#islem-para-ustu-sar"));
        Assert.Empty(e.FindAll("#yildiz-tezgah-para-ustu-alan"));
        Assert.True(e.Find("#yildiz-tezgah-para-ustu").HasAttribute("disabled"));

        // Ödeme türü seçici (kapı açık) yerinde — kullanıcı kart/havaleye geçebilir.
        Assert.NotEmpty(e.FindAll("#islem-odeme"));
    }

    [Fact]
    public void CUMA_S_02_Basit_gercek_musteride_nakit_engeli_TEK_bantta_Pesin_cumlesi_YOK()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        TemaTemizligiGDYardimcisi.MusteriSec(e, "c1");
        TemaTemizligiGDYardimcisi.MusteriBekle(e, "Ayşe Demir");
        KalemEkle(e, "u1", 1);
        KalemEkle(e, "u2", 2);
        TemaTemizligiGDYardimcisi.OdemeAc(e);
        Satis4Yardim.NakitElleSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        // YASAL-KAPI-3 tur 2: nakit engeli TEK yerde TEK cümle; ödeme sütununda kısa durum/"Neden?" yok, Peşin cümlesi yok.
        var kaydet = TemaTemizligiGDYardimcisi.Kaydet;
        e.WaitForAssertion(() => Assert.Contains("nakit", e.Find(kaydet).GetAttribute("title") ?? "", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(5));
        var cumle = e.Find(kaydet).GetAttribute("title")!;
        Assert.Equal(1, GorunurSayi(e, cumle));   // engel cümlesi TEK kez görünür (Yıldız: Kaydet'in üstündeki not satırı)
        Assert.Equal(cumle, e.Find("#yildiz-tezgah-not").TextContent.Trim());
        Assert.Empty(e.FindAll("#islem-tevsik-durum"));
        Assert.DoesNotContain(SatisAldimSozlugu.Tr.AyrintiPesin, e.Markup);
    }

    [Fact]
    public void CUMA_S_03_Basit_sinirin_altinda_durum_satiri_YOK_para_ustu_VAR()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        KalemEkle(e, "u1", 1);   // 20.000 < sunucu haddi
        // Yıldız: para üstü "+ Para üstü" hapının arkasında (özgün FisParaUstu) — sınırın altında KULLANILIR.
        e.WaitForAssertion(() => Assert.False(e.Find("#yildiz-tezgah-para-ustu").HasAttribute("disabled")), TimeSpan.FromSeconds(5));
        e.Find("#yildiz-tezgah-para-ustu").Click();
        e.WaitForElement("#yildiz-tezgah-para-ustu-alan", TimeSpan.FromSeconds(5));
        Assert.Empty(e.FindAll("#islem-tevsik-durum"));
        Assert.Empty(e.FindAll("#islem-tevsik-ipucu"));
        Assert.All(e.FindAll("#yildiz-tezgah-not"), n => Assert.DoesNotContain("yz-kotu", n.ClassName ?? ""));   // engel yok
    }

    // ═══ PRO — DEĞİŞMEDİ ═════════════════════════════════════════════════════

    [Fact]
    public void CUMA_S_04_Pro_satista_tevsik_engeli_TEK_yerde_Kaydet_yaninda()
    {
        // PAKET SATIS-2 (kurucu temel kuralı 25.09 11:35, CUMA-S bu pakete birleşti): PRO satışta da
        // uzun kırmızı metin ekranı doldurmaz — tek satır kısa durum + "Neden?" katlı ayrıntı (bilgi SİLİNMEDİ).
        var e = TevsikliFis(ArayuzModuServisi.Profesyonel);
        // YASAL-KAPI-3 tur 2: PRO'da engelin TEK yeri Kaydet'in yanı (Yıldız: Fark bloğunun not satırı, Kaydet'in hemen üstü);
        // ödeme sütununda kısa durum/"Neden?" ikinci kez çizilmez.
        e.WaitForAssertion(() => Assert.False(string.IsNullOrWhiteSpace(e.Find("#yildiz-tezgah-not").TextContent)), TimeSpan.FromSeconds(5));
        var not = e.Find("#yildiz-tezgah-not").TextContent.Trim();
        Assert.Equal(e.Find(TemaTemizligiGDYardimcisi.Kaydet).GetAttribute("title"), not);
        Assert.Equal(1, GorunurSayi(e, not));
        Assert.Empty(e.FindAll("#islem-tevsik-durum"));
        Assert.Empty(e.FindAll("#islem-tevsik-ipucu"));
        Assert.Contains("gorsel-gizli", e.Find("#islem-odeme-altin-ipucu").ClassName);
    }
}
