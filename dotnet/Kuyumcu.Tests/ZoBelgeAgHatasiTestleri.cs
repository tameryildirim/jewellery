using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-BELGE — 29.09 devam 3 · doğrulayıcı bulguları
//
// (ORTA, KIYAS #3) Rapor merkezi "Bugün" yükleyicileri ve veri içe aktarım
//   yükleyicileri yalnız ApiHata/JsonException yakalıyordu: ağ hatası
//   (HttpRequestException) ya da zaman aşımı (TaskCanceledException) gösterge
//   bayrağını indirmeden istisnayı dışarı atıyordu → "Yükleniyor" donabilirdi.
//   Sözleşme: bayrak finally'de iner, hata bandı sözlükteki bağlantı metnini yazar.
// (DÜŞÜK) BASİT "Gün sonu" kartı sunucunun ham Türkçe formülünü açılışta basıyordu;
//   artık sade açıklama sözlükten, formül KAPALI katlanırda (TR+EN).
// ⚠ Sahte API her uca gerçekçi yanıt verir; ağ hatası senaryosu `ag` sözlüğüyle.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class ZoBelgeAgHatasiTestleri : BunitContext
{
    private readonly List<string> istekler = [];
    private readonly Dictionary<string, Func<Exception>> ag = [];

    private static ZoBelgeMetinleri ZB => ZoBelgeSozlugu.Tr;

    public ZoBelgeAgHatasiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Kuyumcu.Web.Bilesenler.KayitSecici.BeklemeMs = 0;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<RaporServisi>();
    }

    private void Oturum(string mod)
    {
        var m = Services.GetRequiredService<ArayuzModuServisi>();
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => m.BasitMi);
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
    }

    private int Say(string parca) { lock (istekler) return istekler.Count(i => i.Contains(parca, StringComparison.Ordinal)); }

    private sealed class SahteApi(ZoBelgeAgHatasiTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            lock (t.istekler) t.istekler.Add(istek.RequestUri!.PathAndQuery);
            foreach (var (parca, uret) in t.ag)
                if (yol.EndsWith(parca, StringComparison.Ordinal)) throw uret();

            var g = yol switch
            {
                var x when x.EndsWith("/raporlar/gunluk") => Gunluk,
                var x when x.EndsWith("/raporlar/pos-fatura-mutabakati") => MutabakatFarkli,
                var x when x.EndsWith("/bildirimler") =>
                    """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""",
                var x when x.EndsWith("/pozisyon") => Pozisyon,
                var x when x.EndsWith("/ice-aktarim/sablonlar") => IceSablon,
                var x when x.Contains("/ice-aktarim/gecmis") => """{"toplam":0,"limit":20,"kayitlar":[]}""",
                var x when x.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private const string Gunluk = """
        {"tarih":"2026-09-23","satis":{"adet":1,"toplamTl":1000.00,"toplamHas":1.0},
         "alis":{"adet":0,"toplamTl":0,"toplamHas":0},"hurda":{"toplamHas":0,"odenenTl":0},
         "netHasGramHareketi":-1.0,"hareketSayisi":1,
         "kasalar":[{"id":"k1","ad":"Nakit","bakiye":1000.00,"paraBirimi":"TRY"}]}
        """;

    private const string MutabakatFarkli = """
        {"tarih":"2026-09-23",
         "tahsilat":{"nakit":1000.00,"kart":0,"havale":0,"diger":0,"toplam":1000.00},
         "satis":{"sayisi":1,"toplamTl":1000.00,"faturaliSayisi":0,"faturaliTl":0,
                  "faturasizSayisi":1,"faturasizTl":1000.00},
         "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
         "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,
                  "stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
         "fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},
         "fark":1000.00,"mutabik":false,"uyarilar":[],
         "faturasizIslemler":[],
         "faturaEslesme":{"bagliSayisi":0,"bagliTl":0,"bagimsizSayisi":0,"bagimsizTl":0,
                          "faturasizTl":1000.00,"farkTanimi":"FARK = satış toplamı − fişe bağlı faturalar − fişe bağlanmamış faturalar",
                          "bagimsizFaturalar":[]}}
        """;

    private const string Pozisyon = """
        {"anaMaden":"ALTIN",
         "altin":{"maden":"ALTIN","vitrinGram":10,"hurdaGram":0,"cariAlacakGram":0,"cariBorcGram":0,
                  "netGram":10,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},
         "digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,
         "hesaplanmaAni":"2026-09-23T09:00:00Z","donemdenBagimsiz":true}
        """;

    private const string IceSablon = """
        {"onayMetni":"VERİLERİ İÇE AKTARMAYI ONAYLIYORUM","maksSatir":5000,
         "maksDosyaBayt":2097152,"gerekceAsgari":20,"politikalar":["HATA","ATLA"],
         "kaynaklar":[{"ad":"cari","kolonlar":[{"ad":"ad","zorunlu":true,"ornek":"Ayşe"}]}]}
        """;

    private IRenderedComponent<Kuyumcu.Web.Pages.Raporlar> Basit()
    {
        Oturum(ArayuzModuServisi.Basit);
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForElement("#raporlar-bugun");
        e.WaitForAssertion(() =>
        {
            Assert.True(e.Instance.BugunYuklemesiBitti);
            Assert.Empty(e.FindAll(".yukleniyor-kutu"));
        }, TimeSpan.FromSeconds(10));
        return e;
    }

    [Fact]
    public void ZB32_Rapor_bugun_AG_HATASI_ve_ZAMAN_ASIMI_yukleniyorda_DONMAZ_hata_bandi_yazar()
    {
        ag["/raporlar/gunluk"] = () => new HttpRequestException("ağ yok (test)");
        ag["/raporlar/pos-fatura-mutabakati"] = () => new TaskCanceledException("zaman aşımı (test)");
        ag["/bildirimler"] = () => new HttpRequestException("ağ yok (test)");

        var e = Basit();   // üç yükleyici de BİTTİ (bayrak finally'de iner)

        Assert.Contains(ZB.BaglantiHatasi, e.Find("#bugun-satis-hata").TextContent, StringComparison.Ordinal);
        Assert.Contains(ZB.BaglantiHatasi, e.Find("#bugun-gun-sonu-hata").TextContent, StringComparison.Ordinal);
        Assert.Contains(ZB.BaglantiHatasi, e.Find("#bugun-bekleyen-hata").TextContent, StringComparison.Ordinal);
        // UX-S28: hata "0 ₺"a / "fark yok"a çevrilmez; istisnanın ham İngilizce metni basılmaz.
        Assert.Empty(e.FindAll("#bugun-gun-sonu-tamam"));
        Assert.DoesNotContain("(test)", e.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ZB33_Ice_aktarim_AG_HATASINDA_donmaz_bant_yazar_yeniden_dene_calisir()
    {
        Oturum(ArayuzModuServisi.Profesyonel);
        ag["/ice-aktarim/sablonlar"] = () => new HttpRequestException("ağ yok (test)");
        var c = Render<Kuyumcu.Web.Pages.VeriIceAktarim>();
        c.WaitForElement("#ia-sablon-hata");
        Assert.Contains(ZB.BaglantiHatasi, c.Find("#ia-hata").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain(Sozlukler.Tr.Ortak.Yukleniyor, c.Find("#ia-adim1-baslik").ParentElement!.TextContent);
        Assert.Null(c.Find("#ia-sablon-yeniden").GetAttribute("disabled"));   // bayrak indi

        ag.Clear();
        c.Find("#ia-sablon-yeniden").Click();
        c.WaitForElement("#ia-sablon-indir");
        Assert.Equal(2, Say("/ice-aktarim/sablonlar"));

        // Geçmiş: zaman aşımı katlanırın içinde yazar, "Yükleniyor"da kalmaz.
        ag["/ice-aktarim/gecmis"] = () => new TaskCanceledException("zaman aşımı (test)");
        c.Find("#ia-gecmis-ac").Click();
        c.WaitForElement("#ia-gecmis-hata");
        Assert.Contains(ZB.BaglantiHatasi, c.Find("#ia-gecmis-hata").ParentElement!.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ZB34_Gun_sonu_SADE_aciklama_sozlukten_formul_KAPALI_katlanirda()
    {
        var e = Basit();
        // DEGISIM-GUNSONU: fark tamamen faturasız satıştan → açıklama bunu söyler ("kasaya giren para" DEMEZ).
        Assert.Equal(DegisimGunSonuSozlugu.Tr.AciklamaFaturasiz, e.Find("#bugun-fark-aciklama").TextContent.Trim());
        // Sunucunun ham Türkçe formülü açılışta da katlanırda da basılmaz (EN modda Türkçe kalıyordu).
        Assert.DoesNotContain("fişe bağlı", e.Markup, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#bugun-fark-formul-metin"));             // kapalı başlar
        e.Find("#bugun-fark-formul-ac").Click();
        Assert.Equal(ZB.GunSonuFormul, e.Find("#bugun-fark-formul-metin").TextContent.Trim());
        Assert.False(string.IsNullOrWhiteSpace(ZoBelgeSozlugu.En.GunSonuFormul));
        Assert.NotEqual(ZoBelgeSozlugu.Tr.GunSonuFormul, ZoBelgeSozlugu.En.GunSonuFormul);
        Assert.NotEqual(ZoBelgeSozlugu.Tr.BaglantiHatasi, ZoBelgeSozlugu.En.BaglantiHatasi);
    }
}
