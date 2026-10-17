using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 31 — /pos ve /pos/tahsilatlar EKRANLARI [bUnit]
//
// ⚠ Sahte API '{}' varsayılanıyla BIRAKILMADI (§6 tuzağı: 5 CariEkran testi
//   bu yüzden JsonException ile kırılmıştı): bu paketin çağırdığı her uca
//   GERÇEKÇİ yanıt verilir (/pos/cihazlar, /pos/tahsilatlar, /pos/valor-takvimi,
//   /kasalar).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class PosEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly SahteApi sahte = new();

    public PosEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        sahte.Kayit = istekler;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(sahte) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<PosServisi>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        public List<(string, string, string)> Kayit = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            Kayit.Add((istek.Method.Method, yol, govde));

            var y = yol switch
            {
                var v when v.Contains("/pos/valor-takvimi") =>
                    """[{"valorTarihi":"2026-09-19T00:00:00Z","cihazId":"p1","cihazAd":"Garanti POS","bankaAdi":"Garanti","brut":10000.00,"komisyon":185.00,"net":9815.00,"yatan":0.00,"kalan":9815.00}]""",
                var v when v.Contains("/pos/tahsilatlar") =>
                    """{"satirlar":[{"id":"t1","posCihazId":"p1","cihazAd":"Garanti POS","bankaAdi":"Garanti","brutTutar":10000.00,"komisyonOran":0.0185,"komisyonTutar":185.00,"netTutar":9815.00,"yatanTutar":0.00,"kalanTutar":9815.00,"taksitAdedi":1,"paraBirimi":"TRY","islemTarihi":"2026-09-18T09:00:00Z","valorTarihi":"2026-09-19T00:00:00Z","durum":"BEKLIYOR","iptalGerekcesi":null}],"toplamSatir":1,"limit":50,"offset":0,"azamiSatir":200,"sinirDegdi":false}""",
                var v when v.Contains("/pos/cihazlar") && istek.Method == HttpMethod.Get =>
                    """[{"id":"p1","ad":"Garanti POS","bankaAdi":"Garanti","subeId":"s1","kasaId":"k1","bankaHesapId":null,"uyeIsyeriNo":null,"terminalNo":null,"pesinKomisyonOran":0.0185,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":9815.00,"bugunYatacakNet":0.00}]""",
                // ZO-ISLEM-A 29.09: eşleştirme seçicisi açılışta POS yatma adaylarını listeler.
                var v when v.Contains("/pos/yatma-adaylari") =>
                    """{"sorgu":"","tur":"BANKA_HAREKET","limit":10,"ofset":0,"sayi":0,"dahaVar":false,"tavan":50,"sonuclar":[],"gozatma":true}""",
                var v when v.Contains("/kasalar") =>
                    """[{"id":"k1","ad":"Kasa","paraBirimi":"TRY","subeId":"s1","aktif":true}]""",
                _ => "{}",
            };

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    [Fact]
    public void PosEkrani_CihazListesiniGosterir()
    {
        var sayfa = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();

        var metin = sayfa.Markup;
        Assert.Contains("Garanti POS", metin);
        // UX-S31: ham kod değil sözlük metni; oran yüzde olarak okunur.
        // KESIF-P1 (#31): tr-TR yüzde — "%1,85" (eskiden InvariantCulture "1.85 %").
        Assert.Contains("%1,85", metin);
        Assert.DoesNotContain("1.85 %", metin);
        Assert.NotNull(sayfa.Find("#pos-duzenle-p1"));
    }

    [Fact]
    public void PosEkrani_YeniCihazModali_ZorunluAlanlar()
    {
        var sayfa = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        sayfa.Find("#pos-yeni-ac").Click();

        // Boş formda kaydet KİLİTLİ ve sebebi EKRANDA (UX-S30).
        var kaydet = sayfa.Find("#cekmece-pos-cihaz-kaydet");
        Assert.True(kaydet.HasAttribute("disabled"));
        Assert.NotNull(sayfa.Find("#cekmece-pos-cihaz-kilit"));
        // UX-S22: tek TL kasası varken seçici GİZLİ, değer salt okunur satır.
        Assert.NotNull(sayfa.Find("#pos-cihaz-kasa-sabit"));
    }

    [Fact]
    public void PosEkrani_AcikAlacakliCihazda_PasifeAlDugmesi_KILITLI_VeSebepYazar()
    {
        // DoD/1: tıklanınca hiçbir şey olmayan düğme YOK — kilitli düğme sebebini söyler.
        var sayfa = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        var dugme = sayfa.Find("#pos-pasif-p1");

        Assert.True(dugme.HasAttribute("disabled"));
        Assert.Equal("true", dugme.GetAttribute("aria-disabled"));
        Assert.False(string.IsNullOrWhiteSpace(dugme.GetAttribute("title")));
    }

    [Fact]
    public void TahsilatEkrani_ValorTakviminiGosterir()
    {
        var sayfa = Render<Kuyumcu.Web.Pages.PosTahsilatlar>().AyrintiAc();

        var metin = sayfa.Markup;
        Assert.Contains("19.09.2026", metin);        // InvariantCulture tarih
        Assert.Contains("Garanti POS", metin);
        // Defter satırı DÜZENLENEMEZ — gerekçe ve düzeltme yolu ekranda.
        Assert.NotNull(sayfa.Find("#degismez-ipucu-PosTahsilat"));
    }

    [Fact]
    public void TahsilatEkrani_EslestirmeDugmesi_OneriYokken_KAPALI()
    {
        var sayfa = Render<Kuyumcu.Web.Pages.PosTahsilatlar>().AyrintiAc();

        var uygula = sayfa.Find("#pos-eslestir-uygula");
        Assert.True(uygula.HasAttribute("disabled"));
        Assert.Equal("true", uygula.GetAttribute("aria-disabled"));

        var oneri = sayfa.Find("#pos-oto-oneri");
        Assert.True(oneri.HasAttribute("disabled"));   // banka hareketi girilmeden kapalı
    }

    [Fact]
    public void TahsilatEkrani_IptalModali_KisaGerekcede_ONAY_KAPALI()
    {
        var sayfa = Render<Kuyumcu.Web.Pages.PosTahsilatlar>().AyrintiAc();
        // ZO-ISLEM-A: defter kapalı akordeon — satır eylemi açınca görünür.
        sayfa.Find("#pos-t-defter-kart-ac").Click();
        sayfa.Find("#pos-t-iptal-t1").Click();

        var onayla = sayfa.Find("#pos-iptal-onayla");
        Assert.True(onayla.HasAttribute("disabled"));

        sayfa.Find("#pos-iptal-gerekce").Input("kısa");
        Assert.True(sayfa.Find("#pos-iptal-onayla").HasAttribute("disabled"));

        sayfa.Find("#pos-iptal-gerekce").Input("Kart hamili bankaya itiraz etti, chargeback kaydı.");
        Assert.False(sayfa.Find("#pos-iptal-onayla").HasAttribute("disabled"));
    }
}
