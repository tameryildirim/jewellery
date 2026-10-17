using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-2 tur 3 (orkestratör 30.09 22:20 madde 2) — SATIŞ FİŞİ ONAYINDA TEK SATIR:
// veresiye bacaklı satış kaydedildiğinde, SUNUCU fiş toplamının nakit sınırını aştığını
// söylüyorsa (GET /tevsik/gunluk?cariId=&fisToplamTl= → islemTutariKuraliDevrede)
// "Bu fişin kalan borcu yalnız kartla ya da havaleyle alınabilir." yazar.
// Eşik SAYISI istemcide yok: karar sahte sunucunun yanıtıyla değişir.
// ⚠ Görünürlük/sığma gerçek tarayıcıda ölçüldü (rapor § 1B).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class YASAL2FisTaksitEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool asiliyor;
    private int gunlukKodu = 200;

    public YASAL2FisTaksitEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(YASAL2FisTaksitEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            var kod = 200;
            // Kayıt SONRASI sorgu (fisToplamTl ile) sahte sunucunun kararını taşır.
            var kayitSonrasi = yol.Contains("/tevsik/gunluk") && yol.Contains("fisToplamTl=") && t.istekler.Any(i => i.Item1 == "POST");
            var y = yol switch
            {
                _ when kayitSonrasi && t.gunlukKodu != 200 => (kod = t.gunlukKodu) == 0 ? "" : """{"message":"sunucu yanıt vermedi"}""",
                _ when kayitSonrasi => $$"""
                    {"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":0,
                     "islemTutariKuraliDevrede":{{(t.asiliyor ? "true" : "false")}},"taksit":null,"aciklama":null}
                    """,
                var v when v.Contains("/tevsik/gunluk") => """
                    {"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}
                    """,
                var v when v.Contains("/islemler/satis") && istek.Method == HttpMethod.Post => """{"fisId":"01J0000000000000000000FIS1","hareketIdleri":["h1"]}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/esikler") => """
                    {"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,
                     "ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}
                    """,
                _ => "{}",
            };
            return new HttpResponseMessage((System.Net.HttpStatusCode)kod)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private List<(string Metot, string Yol, string Govde)> Istekler() { lock (istekler) return [.. istekler]; }

    private const string KalanBorcBanka = "Bu fişin kalan borcu yalnız kartla ya da havaleyle alınabilir.";

    /// <summary>
    /// Müşteri c1, tek kalem (10.000 TL), ödeme VERESİYE → Kaydet.
    /// TEMA TEMİZLİĞİ 17.10: satış hep Yıldız tezgâhı — müşteri "Değiştir" katmanında, ödeme türü ödeme satırına dokununca
    /// açılan düzende (PRO: #islem-odeme), Kaydet #yildiz-tezgah-kaydet. Kayıt sonrası satır Fark bloğunun uyarı satırlarındadır
    /// (#yildiz-tezgah-uyari-N — özgün #islem-taksit-uyari / #islem-taksit-hata Yıldız'da görünmez).
    /// </summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> VeresiyeSatisKaydet(string odeme = "VERESIYE")
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#yildiz-tezgah", TimeSpan.FromSeconds(10));
        e.GbYildizMusteriSec("c1");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        e.WaitForAssertion(() => Assert.Equal(1, e.YildizKalemSayisi()));
        e.YildizOdemeSec(odeme);
        if (e.FindAll("#islem-kasa").Count > 0 && odeme != "VERESIYE") SeciciYardim.Sec(e, "islem-kasa", "ks1");
        e.WaitForAssertion(() => Assert.False(e.GbYildizKaydet().HasAttribute("disabled"), e.GbYildizKaydet().GetAttribute("title")));
        e.GbYildizKaydet().Click();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "POST"));
        return e;
    }

    /// <summary>Fark bloğunun uyarı satırları (önce tezgâhın çizildiği doğrulanır — boş DOM'da yanlış yeşil yok).</summary>
    private static List<string> Uyarilar(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        Assert.Single(e.FindAll("#yildiz-tezgah-fark"));
        return [.. e.FindAll("#yildiz-tezgah-fark [id^='yildiz-tezgah-uyari-']").Select(x => x.TextContent.Trim())];
    }

    [Fact]
    public void Veresiye_satis_sunucu_HAD_ASILIYOR_derse_tek_satir_kart_havale_yazar()
    {
        asiliyor = true;
        var e = VeresiyeSatisKaydet();
        e.WaitForAssertion(() => Assert.Single(Uyarilar(e), KalanBorcBanka));
        var sorgu = Istekler().Last(i => i.Yol.Contains("/tevsik/gunluk") && i.Yol.Contains("fisToplamTl="));
        Assert.Contains("cariId=c1", sorgu.Yol, StringComparison.Ordinal);
        Assert.Contains("fisToplamTl=10000", sorgu.Yol, StringComparison.Ordinal);   // InvariantCulture
        Assert.DoesNotContain(Uyarilar(e), u => u.Contains("Kalan borcun nasıl alınacağı okunamadı", StringComparison.Ordinal));
    }

    [Fact]
    public void Veresiye_satis_sunucu_asilmiyor_derse_satir_YOK()
    {
        asiliyor = false;
        var e = VeresiyeSatisKaydet();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Yol.Contains("fisToplamTl=") && Istekler().Any(p => p.Metot == "POST")));
        Thread.Sleep(200);
        Assert.DoesNotContain(KalanBorcBanka, Uyarilar(e));
    }

    [Fact]
    public void Pesin_KART_satista_kayit_sonrasi_sorgu_ATILMAZ_satir_yok()
    {
        asiliyor = true;
        var e = VeresiyeSatisKaydet("KART");
        var postSirasi = Istekler().FindIndex(i => i.Metot == "POST");
        Assert.DoesNotContain(Istekler().Skip(postSirasi + 1), i => i.Yol.Contains("/tevsik/gunluk"));
        Assert.DoesNotContain(KalanBorcBanka, Uyarilar(e));
    }

    [Fact]
    public void Kayit_sonrasi_sorgu_duserse_HATA_bandi_sessiz_degil()
    {
        gunlukKodu = 500;
        var e = VeresiyeSatisKaydet();
        e.WaitForAssertion(() => Assert.Contains(Uyarilar(e), u => u.Contains("Kalan borcun nasıl alınacağı okunamadı", StringComparison.Ordinal)));
        Assert.DoesNotContain(KalanBorcBanka, Uyarilar(e));
    }
}
