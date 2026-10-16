using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// DÖVİZ BACAĞI EKRANI — paket 15 (yasal biçim: dövizle TAHSİLAT)
//
// Ekrandaki tek kritik kural: DOVIZ bacağının TL'si KUR KAYDINDAN türetilir,
// kasiyer yazamaz. Elle yazılabilseydi her fişte kendi kurunu uygular, dükkân
// fiilen döviz alım-satımı yapmış olurdu — yetki belgesiz kuyumcu için yasak
// (PAKET-15-DOVIZ-KARAR-2026-09-15.md). Sunucu da aynı hesabı yapıp eşitlik
// arar; ekran ve sunucu AYNI yuvarlamayı kullanmalı.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class DovizBacagiEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool kurYok;

    public DovizBacagiEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => kurYok))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();   // UX-3 Faz B: Islemler artik bu servisi enjekte ediyor
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(
        List<(string, string, string)> kayit, Func<bool> kurYokMu) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            var y = yol switch
            {
                var v when v.Contains("/tevsik/durum") => """
                    {"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,
                     "bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}
                    """,
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => kurYokMu()
                    ? """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}"""
                    : """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """
                    [{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,
                      "iscilikTl":0,"satisFiyati":10000}]
                    """,
                var v when v.Contains("/kasalar") => """
                    [{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},
                     {"id":"kusd","ad":"Dolar Kasa","paraBirimi":"USD","aktif":true}]
                    """,
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/esikler") => """
                    {"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,
                     "ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,
                     "belgeTurleri":["TC_KIMLIK","PASAPORT"]}
                    """,
                // P-10: KART bacagi POS cihazi listesini okur.
                var p10c when p10c.Contains("/pos/cihazlar") =>
                    """[{"id":"pc1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"s1","kasaId":"ks1","pesinKomisyonOran":1.5,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    /// <summary>Karma kipte, bir kalemli (10.000 TL) fiş ve ilk bacağı DOVIZ seçilmiş ekran.</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> DovizBacagiAc()
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-cari");
        SeciciYardim.Sec(e, "islem-cari", "c1");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        e.Find("#islem-karma").Change(true);
        e.WaitForElement("#islem-bacak-ekle");
        e.Find("#islem-bacak-ekle").Click();
        e.WaitForElement("#islem-bacak-0-arac");
        SeciciYardim.Sec(e, "islem-bacak-0-arac", "DOVIZ");
        e.WaitForElement("#islem-bacak-0-birim");
        istekler.Clear();
        return e;
    }

    private static AngleSharp.Dom.IElement Kaydet(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
        => e.FindAll("button").First(b => b.TextContent.Contains("Kaydet"));

    [Fact]
    public void DOVIZ_secilince_birim_ve_miktar_alanlari_cizilir_TL_SALT_OKUNUR()
    {
        var e = DovizBacagiAc();

        Assert.NotNull(e.Find("#islem-bacak-0-doviz"));
        // P-36c/3: "elle yazılamaz" artık devre dışı bir kutuyla değil,
        // GİRDİ OLMAMASIYLA sağlanır (Bilesenler/Form/SaltOkunur).
        Assert.NotEqual("INPUT", e.Find("#islem-bacak-0-tutar").TagName);
        Assert.Empty(e.Find("#islem-bacak-0-tutar").QuerySelectorAll("input, textarea"));
    }

    /// <summary>100 USD × 40 = 4.000,00 — ekran sunucuyla AYNI hesabı yapar.</summary>
    [Fact]
    public void Doviz_miktari_girilince_TL_kur_kaydindan_hesaplanir()
    {
        var e = DovizBacagiAc();
        SeciciYardim.Sec(e, "islem-bacak-0-doviz", "100");

        e.WaitForAssertion(() =>
            Assert.Equal("4000.00", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")));
    }

    /// <summary>Kur kaydında USD yoksa TL 0 kalır; bacak fişi kapatamaz, Kaydet açılmaz.</summary>
    [Fact]
    public void Kur_yoksa_TL_sifir_kalir_ve_fis_KAPANMAZ()
    {
        kurYok = true;
        var e = DovizBacagiAc();
        SeciciYardim.Sec(e, "islem-bacak-0-doviz", "250");

        e.WaitForAssertion(() =>
            Assert.Equal("0.00", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")));
        Assert.True(Kaydet(e).HasAttribute("disabled"));
    }

    /// <summary>Dolar bacağına yalnız USD kasası; TL bacağına yalnız TRY kasası (K6).</summary>
    [Fact]
    public void Kasa_listesi_bacagin_para_birimine_gore_FILTRELENIR()
    {
        var e = DovizBacagiAc();

        // P-36c: ham <select> → ortak <Secici>. Bacağın para birimine UYAN
        // TEK kasa varsa seçici artık AÇILMAZ; UX-S22 gereği kilitlenir ve o
        // kasa kendiliğinden seçilir. "Yalnız o kasa listelenir" şartının
        // ölçülebilir karşılığı budur: seçili değer O kasadır ve kilit sebebi
        // ekranda yazılıdır (UX-S30) — sessiz otomatik seçim yok.
        // SATIS-3 (KAYNAK 5, kurucu 25.09): tek uyan kasada seçici HİÇ çizilmez — PRO'da
        // kısa salt okunur satır o kasanın adını yazar; gövdedeki kasaId aşağıdaki testte.
        Assert.Empty(e.FindAll("#islem-bacak-0-kasa"));
        Assert.Contains("Dolar Kasa", e.Find("#islem-bacak-0-kasa-tek").TextContent);

        e.Find("#islem-bacak-ekle").Click();
        e.WaitForElement("#islem-bacak-1-arac");
        Assert.Empty(e.FindAll("#islem-bacak-1-kasa"));
        Assert.Contains("Merkez", e.Find("#islem-bacak-1-kasa-tek").TextContent);
    }

    [Fact]
    public void Govdede_paraBirimi_ve_dovizTutar_GONDERILIR()
    {
        var e = DovizBacagiAc();
        SeciciYardim.Sec(e, "islem-bacak-0-doviz", "250");          // 250 × 40 = 10.000 = fiş
        e.WaitForAssertion(() =>
            Assert.Equal("10000.00", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")));
        // SATIS-3: tek USD kasası otomatik — seçici yok, gövdede kasaId = kusd (aşağıda).

        e.WaitForAssertion(() => Assert.False(Kaydet(e).HasAttribute("disabled")));
        Kaydet(e).Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Yol.EndsWith("/islemler/satis-fisi"));
            var kok = System.Text.Json.JsonDocument.Parse(istek.Govde).RootElement;
            var bacak = kok.GetProperty("odemeler")[0];
            Assert.Equal("DOVIZ", bacak.GetProperty("arac").GetString());
            Assert.Equal("USD", bacak.GetProperty("paraBirimi").GetString());
            Assert.Equal(250m, bacak.GetProperty("dovizTutar").GetDecimal());
            Assert.Equal(10_000m, bacak.GetProperty("tutarTl").GetDecimal());
            Assert.Equal("kusd", bacak.GetProperty("kasaId").GetString());
        });
    }
}
