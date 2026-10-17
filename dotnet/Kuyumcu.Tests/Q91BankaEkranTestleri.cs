using System.Globalization;
using System.Text.Json;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q9-1 — /banka EKRAN TARAFI [bUnit] (QA turu 9: 2 KRİTİK + 1 YÜKSEK)
//
//   K1 — dekont girişinde TARİH alanı yoktu; satırda tarih düzeltilemiyordu
//        (sayfa "tarih doğrudan düzeltilebilir" diyordu).
//   K2 — dekont "Bekliyor" durumundan çıkarılamıyordu (eylem yoktu).
//   Y1 — storno sonrası TOPLAM BAKİYE ve hesap sekmesi sayfa yenilenene dek
//        eski tutarı gösteriyordu.
//
// ⚠ Sahte API '{}' varsayılanıyla BIRAKILMADI: bu paketin dokunduğu her uca
//   (PATCH tarih, POST eslestir, POST storno, GET /bankalar) gerçekçi yanıt.
// ⚠ DoD EK md.1: her yeni düğme ya gerçek istek atar ya da gönderimi alan altı
//   mesajla durdurur — "tıklanınca hiçbir şey olmayan düğme" yok.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Q91BankaEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly SahteApi sahte;

    private static DateOnly Bugun => DateOnly.FromDateTime(DateTime.Now);
    private static string Iso(DateOnly g) => g.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public Q91BankaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        sahte = new SahteApi(istekler);
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(sahte) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
    }

    /// <summary>
    /// Durumlu sahte API: storno POST'undan SONRA /bankalar yeni bakiyeyi döner
    /// (sunucudaki gerçek davranış: 1550 → 1250).
    /// </summary>
    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        public bool Stornolandi;

        private const string Hareketler = """
            {"hareketler":[
              {"id":"bh1","yon":"GELEN","tutar":300,"karsiTarafAd":"Ali Veli","aciklama":"Elle dekont",
               "durum":"BEKLIYOR","tarih":"2026-09-16T10:00:00Z","karsiIban":null,"stornoKaynakId":null,
               "ekstreSatirId":null,"karsiBacakId":null,"cariId":null},
              {"id":"bh2","yon":"GELEN","tutar":1000,"karsiTarafAd":"BANKA KAYDI","aciklama":"Ekstreden",
               "durum":"BEKLIYOR","tarih":"2026-09-15T10:00:00Z","karsiIban":null,"stornoKaynakId":null,
               "ekstreSatirId":"es1","karsiBacakId":null,"cariId":null},
              {"id":"bh3","yon":"GELEN","tutar":250,"karsiTarafAd":"Ayşe","aciklama":"Eşleşmiş",
               "durum":"ESLESTI","tarih":"2026-09-14T10:00:00Z","karsiIban":null,"stornoKaynakId":null,
               "ekstreSatirId":null,"karsiBacakId":null,"cariId":"c1"}]}
            """;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            var bakiye = Stornolandi ? "1250" : "1550";
            var y = yol switch
            {
                var v when v.Contains("/masak/esikler") =>
                    """{"kimlikEsigi":185000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000,"ekBelgeEsigi":20000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var v when v.EndsWith("/storno") =>
                    MarkStorno(),
                var v when v.EndsWith("/eslestir") =>
                    """{"id":"bh1","bankaHesapId":"b1","durum":"ESLESTI","cariId":"c1","oncekiDurum":"BEKLIYOR"}""",
                var v when v.Contains("/hareketler/") && istek.Method == HttpMethod.Patch =>
                    """{"id":"bh1","bankaHesapId":"b1","aciklama":"Elle dekont","karsiTarafAd":"Ali Veli","karsiIban":null,"tarih":"2026-09-12T12:00:00Z","tutar":300,"yon":"GELEN","degisenAlanSayisi":1}""",
                var v when v.EndsWith("/hareket") && istek.Method == HttpMethod.Post =>
                    """{"hareketId":"bh9","hesap":{"id":"b1","bakiye":1850}}""",
                var v when v.Contains("/bankalar/") && v.Contains("/hareketler") => Hareketler,
                var v when v.Contains("/bankalar") && istek.Method == HttpMethod.Get =>
                    $$"""{"hesaplar":[{"id":"b1","bankaAdi":"Ziraat","hesapAdi":"Ana Hesap","hesapSahibi":"X Ltd.","iban":"TR200000000000000000000001","paraBirimi":"TRY","bakiye":{{bakiye}},"aktif":true}]}""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz"}]""",
                _ => "{}",
            };

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }

        private string MarkStorno()
        {
            Stornolandi = true;
            return """{"stornoBankaHareketId":"bh8","kaynakBankaHareketId":"bh1","yon":"GIDEN","tutar":300,"paraBirimi":"TRY","hesapBakiyesi":1250}""";
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Banka> Sayfa()
    {
        var e = Render<Kuyumcu.Web.Pages.Banka>().AyrintiAc();
        e.WaitForElement("#bh-dekont-gir");
        e.HareketleriAc();   // ZO-ISLEM-A: hareketler kapalı akordeonda
        return e;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Banka> DekontModali()
    {
        var e = Sayfa();
        e.Find("#bh-dekont-gir").Click();
        e.WaitForElement("#dekont-tutar");
        e.DekontDetayAc();   // ZO-ISLEM-A: tarih "+ … ekle" bölümünde
        e.WaitForElement("#dekont-tarih");
        istekler.Clear();
        e.Find("#dekont-tutar").Input("500");
        return e;
    }

    private JsonElement SonGovde(string metot, string yolParcasi)
    {
        var i = istekler.Last(x => x.Metot == metot && x.Yol.Contains(yolParcasi));
        return JsonDocument.Parse(i.Govde).RootElement;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // K1 — DEKONT GİRİŞİNDE TARİH
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void K1_dekont_formunda_TARIH_alani_VAR_ve_varsayilan_BUGUN()
    {
        var e = DekontModali();
        var girdi = e.Find("#dekont-tarih");
        // GIRDI-AILESI-2 (kurucu 08.10): ortak <Tarih> metin + gg.aa.yyyy maskesi; yerli seçici takvim simgesinde (#<id>-takvim-yerli).
        Assert.Equal("text", girdi.GetAttribute("type"));
        Assert.Equal("tarih", girdi.GetAttribute("data-maske"));
        Assert.Equal(Kuyumcu.Core.Domain.Girdi.TarihPenceresi.Yaz(Bugun), girdi.GetAttribute("value"));
        Assert.Equal(Bugun.Year.ToString(), girdi.GetAttribute("data-yil-en-cok"));   // ileri yıl yazılamaz
        Assert.Equal(Iso(Bugun), e.Find("#dekont-tarih-takvim-yerli").GetAttribute("max"));   // ileri tarih seçiciden de kapalı
    }

    [Fact]
    public void K1_gecmis_tarih_POST_govdesine_OGLE_UTC_olarak_gider()
    {
        var e = DekontModali();
        var gecmis = Bugun.AddDays(-4);
        e.Find("#dekont-tarih").Change(Iso(gecmis));
        e.Find("#dekont-kaydet").Click();

        e.WaitForAssertion(() =>
            Assert.Equal(Iso(gecmis) + "T12:00:00Z",
                SonGovde("POST", "/bankalar/b1/hareket").GetProperty("tarih").GetString()));
    }

    [Fact]
    public void K1_bugun_secili_kalirsa_tarih_GONDERILMEZ_sunucu_saati_kullanilir()
    {
        var e = DekontModali();
        e.Find("#dekont-kaydet").Click();

        e.WaitForAssertion(() =>
            Assert.Equal(JsonValueKind.Null,
                SonGovde("POST", "/bankalar/b1/hareket").GetProperty("tarih").ValueKind));
    }

    [Fact]
    public void K1_ileri_tarih_ALAN_ALTINDA_soylenir_ve_POST_GITMEZ()
    {
        var e = DekontModali();
        e.Find("#dekont-tarih").Change(Iso(Bugun.AddDays(3)));
        e.Find("#dekont-kaydet").Click();

        Assert.Contains("ileri", e.Find("#alan-hata-dekont-tarih").TextContent);
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/hareket"));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // K1 — SATIRDA TARİH DÜZELTME
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void K1_satirdaki_tarih_DUZENLENEBILIR_ve_PATCH_YALNIZ_tarih_tasir()
    {
        var e = Sayfa();
        e.WaitForElement("#bh-tarih-bh1").Click();
        e.WaitForElement("#bh-tarih-girdi");
        istekler.Clear();
        e.Find("#bh-tarih-girdi").Change("2026-09-12");
        e.Find("#cekmece-banka-dekont-tarih-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var g = SonGovde("PATCH", "/bankalar/b1/hareketler/bh1");
            Assert.Equal("2026-09-12T12:00:00Z", g.GetProperty("tarih").GetString());
            // Üç durumlu PATCH: gönderilmeyen alan KORUNUR → gövdede yalnız tarih.
            Assert.Single(g.EnumerateObject());
            Assert.Contains("12.09.2026", e.Find("#basari-bandi").TextContent);
        });
    }

    [Fact]
    public void K1_ekstreden_gelen_satirda_tarih_tetigi_YOK_ve_sebebi_YAZILI()
    {
        var e = Sayfa();
        e.WaitForElement("#bh-tarih-bh1");
        Assert.Empty(e.FindAll("#bh-tarih-bh2"));
        Assert.Contains("bankanın kendi kaydıdır", e.Markup);
    }

    [Fact]
    public void K1_bos_tarihle_kaydet_ALAN_ALTI_mesaj_verir_PATCH_GITMEZ()
    {
        var e = Sayfa();
        e.WaitForElement("#bh-tarih-bh1").Click();
        e.WaitForElement("#bh-tarih-girdi");
        istekler.Clear();
        e.Find("#bh-tarih-girdi").Change("");
        e.Find("#cekmece-banka-dekont-tarih-kaydet").Click();

        Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-bh-tarih-girdi").TextContent));
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // K2 — BEKLİYOR → EŞLEŞTİ
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void K2_bekleyen_dekontta_ESLESTIR_dugmesi_VAR_eslesmiste_YOK()
    {
        var e = Sayfa();
        e.WaitForElement("#bh-eslestir-bh1");
        Assert.NotNull(e.Find("#bh-eslestir-bh2"));        // ekstre satırı da müşteriye bağlanabilir
        Assert.Empty(e.FindAll("#bh-eslestir-bh3"));      // ESLESTI → düğme yok
        // UX-S25: eşleşmiş dekontun müşterisinin neden değişmediği ve doğru yol YAZILI.
        Assert.Contains("yeni dekont girin", e.Markup);
        Assert.NotNull(e.Find("#bh-bekliyor-not"));
    }

    [Fact]
    public void K2_musteri_secmeden_kaydet_ALAN_ALTI_mesaj_verir_istek_GITMEZ()
    {
        var e = Sayfa();
        e.WaitForElement("#bh-eslestir-bh1").Click();
        e.WaitForElement("#bh-eslestir-cari-sar");
        istekler.Clear();
        e.Find("#cekmece-banka-dekont-eslestir-kaydet").Click();

        Assert.Contains("müşteri seçin", e.Find("#bh-eslestir-cari-sar").TextContent);
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/eslestir"));
    }

    [Fact]
    public void K2_musteri_secilince_ESLESTIR_ucu_cagrilir_ve_sonuc_BANDA_yazilir()
    {
        var e = Sayfa();
        e.WaitForElement("#bh-eslestir-bh1").Click();
        e.WaitForElement("#bh-eslestir-cari-sar");
        SeciciYardim.Sec(e, "bh-eslestir-cari", "c1");
        istekler.Clear();
        e.Find("#cekmece-banka-dekont-eslestir-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            Assert.Equal("c1", SonGovde("POST", "/bankalar/b1/hareketler/bh1/eslestir")
                .GetProperty("cariId").GetString());
            var bant = e.Find("#basari-bandi").TextContent;
            Assert.Contains("Ayşe Yılmaz", bant);
            Assert.Contains("Eşleşti", bant);
        });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Y1 — STORNO SONRASI BAKİYE AYNI EKRANDA TAZELENİR
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Y1_storno_sonrasi_TOPLAM_BAKIYE_ve_hesap_sekmesi_TAZELENIR()
    {
        var e = Sayfa();
        e.WaitForAssertion(() => Assert.Contains(Bicim.Tl(1550m), e.Find(".stat-izgara").TextContent));

        e.Find("#bh-storno-bh1").Click();
        e.Find("#bh-storno-gerekce").Input("Yanlış hesaba girildi, düzeltiliyor.");
        e.Find("#bh-storno-onayla").Click();

        e.WaitForAssertion(() =>
        {
            Assert.Contains(Bicim.Tl(1250m), e.Find(".stat-izgara").TextContent);
            Assert.Contains(Bicim.Tl(1250m), e.Find(".sekme-grubu").TextContent);
            Assert.DoesNotContain(Bicim.Tl(1550m), e.Find(".stat-izgara").TextContent);
        });
    }
}
