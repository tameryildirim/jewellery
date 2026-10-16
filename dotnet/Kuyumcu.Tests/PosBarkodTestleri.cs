using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// SATIŞ TEZGÂHI — BARKOD OKUTMA (mühürlü liste paket 18)
//
// TESPİT: `/urunler/barkod/{kod}` ucu vardı, SATIŞ ekranı kullanmıyordu.
// Tezgâhtar her kalemi açılır listeden aramak zorundaydı — vitrindeki müşteri
// beklerken yapılacak en son şey.
//
// ⚠ GÜMÜŞ TUZAĞI: gümüş ürünün fiyatı BİLEREK otomatik hesaplanmaz (gümüşün
// gram kotasyonu modelde yok; has gramı ALTIN kuruyla çarpmak 12 g gümüş
// kolyeyi 76.972 TL yapıyordu). Okutunca sessizce hiçbir şey olmaması
// okutucuyu bozuk sandırır — ürün seçili kalır ve fiyat istenir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class PosBarkodTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string barkodYaniti = AltinUrun;
    private int barkodDurum = 200;

    private const string AltinUrun = """
        {"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,
         "iscilikTl":250,"satisFiyati":null,"barkod":"869001","aktif":true}
        """;
    private const string GumusUrun = """
        {"id":"u9","ad":"Gümüş Kolye","ayarId":"G925","hasGrami":11.1,
         "iscilikTl":0,"satisFiyati":null,"barkod":"869009","aktif":true}
        """;

    public PosBarkodTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(
            new SahteApi(istekler, () => (barkodDurum, barkodYaniti)))
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
        List<(string, string, string)> kayit, Func<(int, string)> barkod) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            if (yol.Contains("/urunler/barkod/"))
            {
                var (durum, g) = barkod();
                return new HttpResponseMessage((System.Net.HttpStatusCode)durum)
                {
                    Content = new StringContent(
                        durum == 200 ? g : """{"message":"'x' barkodlu ürün bulunamadı"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };
            }

            var y = yol switch
            {
                var v when v.Contains("/fiyat/teklif") && v.Contains("urunId=u9") => """{"teklifVar":false,"sebep":"FIYAT_YOK"}""",   // gümüş: sunucu da teklif vermez (FiyatMotoru kural 2)
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """
                    {"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}
                    """,
                var v when v.Contains("/urunler") => "[]",
                var v when v.Contains("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                // Gerçek şekil: alan adları tutmazsa eşikler 0 olur, MASAK formu
                // her tutarda açılır ve BelgeTurleri null'dan patlar.
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

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ciz()
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        istekler.Clear();
        return e;
    }

    /// <summary>
    /// Barkodu yazıp Enter'a basar.
    ///
    /// ⚠ Input sonrası bileşen yeniden çizilir ve elimizdeki DOM referansı
    /// BAYATLAR; aynı referansa KeyDown göndermek sessizce düşebilir (test
    /// tam koşuda kırılgan olur). Bu yüzden eleman her adımda yeniden bulunur.
    ///
    /// ⚠ KeyDown, EŞZAMANSIZ işleyicinin (ağ çağrısı içeren PosBarkodOkut)
    /// bitmesini GARANTİ ETMEZ. Yük altında sonuç henüz çizilmemişken doğrulama
    /// koşarsa test sebepsiz kırmızıya döner. Bu yüzden sonuca bakan her
    /// doğrulama WaitForAssertion ile beklenir — sabit uyku DEĞİL, koşul beklenir.
    /// </summary>
    private static void Okut(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string kod)
    {
        e.Find("#islem-barkod").Input(kod);
        e.Find("#islem-barkod").KeyDown(
            new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
    }

    [Fact]
    public void Satis_ekraninda_barkod_kutusu_vardir()
    {
        var e = Ciz();

        Assert.Single(e.FindAll("#islem-barkod"));
    }

    [Fact]
    public void Okutunca_BARKOD_ucuna_gidilir()
    {
        var e = Ciz();
        Okut(e, "869001");

        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/urunler/barkod/869001")));
    }

    [Fact]
    public void Okutulan_urun_KALEMLERE_dogrudan_eklenir()
    {
        // POS davranışı: okut → satır düşsün. Ayrıca "Ekle"ye basmak gerekmemeli.
        var e = Ciz();
        Okut(e, "869001");

        e.WaitForAssertion(() =>
            Assert.Contains(e.FindAll("tbody tr"), tr => tr.TextContent.Contains("Burma Bilezik")));
    }

    /// <summary>
    /// KURAL DEĞİŞTİ (paket 17, PAKET-17-FIYAT-LISTESI-KARAR §1): fiyat artık ekranda
    /// has × kur + işçilik ile HESAPLANMAZ; tek kaynak sunucunun /fiyat/teklif yanıtıdır
    /// (marj listede ve ticari sırdır — PANO-KARAR §88). Eski test "96.742 = 14,19 × 6.800
    /// + 250" istemci formülünü doğruluyordu; yeni test teklifin işlem anındaki kur
    /// kaydıyla istendiğini ve satıra sunucu fiyatının yazıldığını doğrular.
    /// </summary>
    [Fact]
    public void Fiyat_SUNUCU_teklifinden_gelir_kur_kaydiyla_istenir()
    {
        var e = Ciz();
        Okut(e, "869001");

        e.WaitForAssertion(() =>
        {
            Assert.Contains(istekler, i => i.Yol.Contains("/fiyat/teklif?urunId=u1") && i.Yol.Contains("kurSnapshotId=kur-1"));
            Assert.Contains(Bicim.Tl(10000m),
                e.FindAll("tbody tr").First(tr => tr.TextContent.Contains("Burma Bilezik")).TextContent);
        });
    }

    [Fact]
    public void Okuttuktan_sonra_kutu_TEMIZLENIR()
    {
        // Arka arkaya okutma: kutuda önceki barkod kalırsa ikinci okuma bozulur.
        var e = Ciz();
        Okut(e, "869001");

        e.WaitForAssertion(() => Assert.Equal("", e.Find("#islem-barkod").GetAttribute("value")));
    }

    [Fact]
    public void Bulunamayan_barkod_KULLANICIYA_soylenir()
    {
        barkodDurum = 404;
        var e = Ciz();
        Okut(e, "yok-boyle");

        e.WaitForAssertion(() => Assert.Contains("Bu barkodla aktif ürün bulunamadı", e.Markup));
        Assert.Empty(e.FindAll("tbody tr"));
    }

    [Fact]
    public void GUMUS_urunde_sessizce_gecilmez_fiyat_ISTENIR()
    {
        // Gümüşün fiyatı otomatik hesaplanamaz. Hiçbir şey olmaması, tezgâhtara
        // okutucu bozuk dedirtir. Ürün seçili kalır, fiyat elle istenir.
        barkodYaniti = GumusUrun;
        var e = Ciz();
        Okut(e, "869009");

        e.WaitForAssertion(() => Assert.Contains("Birim fiyatı elle girip", e.Markup));
        Assert.Empty(e.FindAll("tbody tr"));                       // kalem eklenmedi
        Assert.Equal("u9", SeciciYardim.SeciliDeger(e, "islem-urun"));  // ürün seçili
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// ALIŞ / HURDA ALIŞ — ÖDEME ARACI BEYANI
//
// TESPİT (2026-09-14, uç kapsamı ölçümü): `Islemler.razor` ALIŞ ve HURDA_ALIS
// POST gövdelerine `odemeTuru` alanını HİÇ koymuyordu; ödeme türü seçicisi de
// yalnız SATIŞ'ta çiziliyordu. Alan gitmeyince sunucu varsayılanı devreye
// giriyor (`AlisOlusturDto.OdemeTuru = "NAKIT"` — bilinçli fail-closed).
//
// ⚠ Sonuç CANLI bir engeldi: 30.000 TL üstü HAVALELİ alış tevsik kapısına
// takılıyor ve EKRANDAN HİÇ GİRİLEMİYORDU. Kuyumcu mal alamıyordu.
// Sunucu tarafındaki aynı hata 2026-09-14'te kapatılmıştı; ekran açık kalmıştı.
//
// Ayrıca: alışta ALTIN/VERESIYE ödeme aracı YOKTUR — sunucu yalnız
// NAKIT|KART|HAVALE kabul eder. Ekran reddedilecek seçeneği göstermemelidir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class AlisOdemeAraciEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public AlisOdemeAraciEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new AlisSahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();   // UX-3 Faz B: Islemler artik bu servisi enjekte ediyor
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class AlisSahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            var y = yol switch
            {
                var v when v.Contains("/fiyat/teklif") && v.Contains("urunId=u9") => """{"teklifVar":false,"sebep":"FIYAT_YOK"}""",   // gümüş: sunucu da teklif vermez (FiyatMotoru kural 2)
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """
                    [{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,
                      "iscilikTl":250,"satisFiyati":null}]
                    """,
                var v when v.Contains("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Kapalıçarşı Toptan"}]""",
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

    private static string? Alan(string govde, string ad)
    {
        var e = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ciz(string islemTuru)
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.FindAll("button").First(b => b.TextContent.Contains(islemTuru)).Click();
        istekler.Clear();
        return e;
    }

    [Fact]
    public void ALIS_ekraninda_odeme_araci_secicisi_VARDIR()
    {
        // Seçici yoksa kullanıcı HAVALE diyemez; gövdede alan gitmez.
        var e = Ciz("Alış");

        Assert.Single(e.FindAll("#islem-odeme"));
    }

    [Fact]
    public void HURDA_ALIS_ekraninda_da_odeme_araci_secicisi_VARDIR()
    {
        var e = Ciz("Hurda");

        Assert.Single(e.FindAll("#islem-odeme"));
    }

    [Fact]
    public void ALIS_secicisinde_ALTIN_ve_VERESIYE_YOKTUR()
    {
        // Sunucu (AlisOlusturDto) yalnız NAKIT|KART|HAVALE kabul eder.
        // Reddedilecek seçeneği göstermek kullanıcıyı 400'e sürükler.
        var e = Ciz("Alış");

        // TurSec senkron ama IadeAdaylariYukle'yi ateşle-unut başlatır; o görev
        // yük altında okuma sırasında yeniden çizim tetikler ve eleman referansı
        // eskir (tam süitte 1/4 kırmızı, izole 15/15 yeşil — 2026-09-15).
        // Bu yüzden eleman her denemede YENİDEN bulunur.
        e.WaitForAssertion(() =>
        {
            var degerler = SeciciYardim.Secenekler(e, "islem-odeme")
                .Select(o => o.Deger()).ToList();
            Assert.Equal(["NAKIT", "KART", "HAVALE"], degerler);
        });
    }

    [Fact]
    public void SATIS_secicisinde_VERESIYE_HALA_VAR_ALTIN_bacaga_tasindi()
    {
        // Daraltma satışı bozmamalı.
        // PAKET B-4 (G-1 + B4-3): tek etiketli "ALTIN" sunucuda artık 400; eski
        // altın karma ödemenin ALTIN bacağından alınır (B4AltinBacagiFisEkranTestleri).
        // Reddedilecek seçenek tek ödeme listesinde GÖSTERİLMEZ, yolu ipucu söyler.
        var e = Render<Kuyumcu.Web.Pages.Islemler>();

        var degerler = SeciciYardim.Secenekler(e, "islem-odeme")
            .Select(o => o.Deger()).ToList();
        Assert.DoesNotContain("ALTIN", degerler);
        Assert.Contains("VERESIYE", degerler);
        Assert.NotNull(e.Find("#islem-odeme-altin-ipucu"));
    }

    [Fact]
    public void ALIS_govdesinde_odemeTuru_GONDERILIR()
    {
        var e = Ciz("Alış");
        SeciciYardim.Sec(e, "islem-cari", "c1");
        if (e.FindAll("#islem-kasa").Count > 0) SeciciYardim.Sec(e, "islem-kasa", "ks1");   // SATIS-3: tek kasada seçici yok (otomatik)
        SeciciYardim.Sec(e, "islem-odeme", "HAVALE");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        e.FindAll("button").First(b => b.TextContent.Contains("Kaydet")).Click();

        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/islemler/alis")));
        var istek = istekler.First(i => i.Yol.EndsWith("/islemler/alis"));
        Assert.Equal("HAVALE", Alan(istek.Govde, "odemeTuru"));
    }

    [Fact]
    public void SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ()
    {
        // Taşınırsa gövdede sunucunun reddedeceği değer gider (400).
        //
        // KARARSIZLIK + TEST AÇIĞI DÜZELTMESİ (P-19, 2026-09-21):
        //  (1) Eski sürüm `#islem-odeme` elemanının DOM `value`sunu okuyordu.
        //      Alış kipinde VERESIYE seçeneği listeden kalktığı için AngleSharp
        //      seçicinin değerini KENDİLİĞİNDEN ilk seçeneğe ("NAKIT") düşürüyor;
        //      yani bileşen alanı VERESIYE kalsa bile iddia yeşil kalıyordu
        //      (mutasyon M-K1-1 hayatta kaldı = test açığı).
        //  (2) Aynı okuma, TurSec() içindeki `_ = IadeAdaylariYukle()` ateşle-unut
        //      görevinin tetiklediği ikinci render'dan önce düşerse eski değeri
        //      ("VERESIYE") görüyordu; tam süitte kararsız kırmızı buradan geliyordu.
        // Artık gerçek kanıt okunuyor: SUNUCUYA GİDEN GÖVDE.
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        SeciciYardim.Sec(e, "islem-odeme", "VERESIYE");
        e.FindAll("button").First(b => b.TextContent.Contains("Alış")).Click();
        istekler.Clear();

        SeciciYardim.Sec(e, "islem-cari", "c1");
        if (e.FindAll("#islem-kasa").Count > 0) SeciciYardim.Sec(e, "islem-kasa", "ks1");   // SATIS-3: tek kasada seçici yok (otomatik)
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        e.FindAll("button").First(b => b.TextContent.Contains("Kaydet")).Click();

        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/islemler/alis")));
        var istek = istekler.First(i => i.Yol.EndsWith("/islemler/alis"));
        Assert.Equal("NAKIT", Alan(istek.Govde, "odemeTuru"));
    }
}
