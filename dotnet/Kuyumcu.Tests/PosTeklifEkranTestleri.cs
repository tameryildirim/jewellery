using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// POS — SUNUCU FİYAT TEKLİFİ ve HEDİYE (paket 17)
//
// Ekran kendi formülüyle fiyat UYDURMAZ: tek kaynak GET /fiyat/teklif. Liste
// yoksa alan boş kalır ve sebep görünür. Hediye kalemi fiyatı 0'a çeker ve
// satış FİŞ ucundan gider (tek başına hediye reddi orada işler).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class PosTeklifEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string teklifYaniti = """
        {"teklifVar":true,"listeFiyati":63500,"hasDegeri":60000,"marjTl":3000,"iscilikTl":500,
         "indirimTutari":250,"satisFiyati":63250,"kampanyaId":"k1","sabitFiyat":false,"indirimOncesiFiyat":63500}
        """;

    public PosTeklifEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => teklifYaniti)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();   // UX-3 Faz B: Islemler artik bu servisi enjekte ediyor
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<string> teklif) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            var y = yol switch
            {
                var v when v.Contains("/fiyat/teklif") => teklif(),
                var v when v.Contains("/tevsik/durum") => """
                    {"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}
                    """,
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6000,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """
                    [{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":10,"iscilikTl":500,"satisFiyati":null}]
                    """,
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/esikler") => """
                    {"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,
                     "asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}
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

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> UrunSec()
    {
        // TEMA TEMİZLİĞİ 17.10: SATIŞ = Yıldız tezgâhı — müşteri "Değiştir" katmanından (aynı cariId → aynı teklif isteği).
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        TemaTemizligiGDYardimcisi.MusteriSec(e, "c1");
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/kur/guncel")));
        SeciciYardim.Sec(e, "islem-urun", "u1");
        return e;
    }

    private static AngleSharp.Dom.IElement Kaydet(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
        => e.Find(TemaTemizligiGDYardimcisi.Kaydet);

    [Fact]
    public void Urun_secilince_fiyat_SUNUCU_teklifinden_dolar_ve_kirilim_gorunur()
    {
        var e = UrunSec();

        e.WaitForAssertion(() =>
        {
            Assert.Contains(istekler, i => i.Yol.Contains("/fiyat/teklif?urunId=u1") && i.Yol.Contains("cariId=c1") && i.Yol.Contains("kurSnapshotId=kur-1"));
            Assert.Equal("63.250", e.Find("#islem-birimfiyat").GetAttribute("value"));   // BICIM-2: odak dışı kutu gruplu
            var kirilim = e.Find("#islem-teklif").TextContent;
            Assert.Contains("63.500", kirilim);   // liste
            Assert.Contains("250", kirilim);      // indirim
        });
    }

    /// <summary>Liste tanımlı değilse rakam UYDURULMAZ; alan boş, sebep açık.</summary>
    [Fact]
    public void Liste_yoksa_fiyat_BOS_kalir_ve_sebep_gosterilir()
    {
        teklifYaniti = """{"teklifVar":false,"sebep":"LISTE_YOK"}""";
        var e = UrunSec();

        e.WaitForAssertion(() =>
        {
            Assert.True(string.IsNullOrEmpty(e.Find("#islem-birimfiyat").GetAttribute("value")));
            Assert.Contains("Fiyat listesi tanımlı değil", e.Find("#islem-teklif-yok").TextContent);
        });
    }

    /// <summary>Eski/boş sunucu yanıtı ({}) çökme değil "teklif yok" demektir.</summary>
    [Fact]
    public void Bos_yanit_ekrani_DUSURMEZ()
    {
        teklifYaniti = "{}";
        var e = UrunSec();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#islem-teklif-yok")));
    }

    [Fact]
    public void Hediye_isaretlenince_kalem_fiyati_SIFIR_ve_fis_ucundan_gider()
    {
        var e = UrunSec();
        e.WaitForAssertion(() => Assert.Equal("63.250", e.Find("#islem-birimfiyat").GetAttribute("value")));   // BICIM-2: odak dışı kutu gruplu
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.Equal("63.250", e.Find("#islem-birimfiyat").GetAttribute("value")));   // BICIM-2: odak dışı kutu gruplu
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        TemaTemizligiGDYardimcisi.KalemSayisiBekle(e, 1);
        // Yıldız: satıra dokun → satır içi düzenleme (PRO: adet · fiyat · iskonto · hediye — FisKalemTablosu kuralının aynası)
        e.Find("#yildiz-kalem-0").Click();
        e.WaitForElement("#yildiz-kalem-0-hediye");
        // aynı ürün ikinci eklemede adet artırır → tek kalem; ayrı kalem için hediye kutusu tek kalemde
        e.Find("#yildiz-kalem-0-hediye").Change(true);
        TemaTemizligiGDYardimcisi.OdemeSec(e, "KART");
        if (e.FindAll("#islem-kasa").Count > 0) SeciciYardim.Sec(e, "islem-kasa", "ks1");   // SATIS-3: tek kasada seçici yok (otomatik)
        istekler.Clear();

        e.WaitForAssertion(() => Assert.False(Kaydet(e).HasAttribute("disabled")));
        Kaydet(e).Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis-fisi"));
            var kok = System.Text.Json.JsonDocument.Parse(istek.Govde).RootElement;
            var kalem = kok.GetProperty("kalemler")[0];
            Assert.True(kalem.GetProperty("hediye").GetBoolean());
            Assert.Equal(0m, kalem.GetProperty("birimFiyatTl").GetDecimal());
            Assert.Equal("KART", kok.GetProperty("odemeler")[0].GetProperty("arac").GetString());
            Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/islemler/satis"));
        });
    }
}
