using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// FATURA EKRANI — KALEMLER (paket 1 Faz A) [bUnit]
//
// Sunucudaki "TOLERANS YOK" kuralının ekran karşılığı: kalem toplamı fatura
// toplamıyla uyuşmuyorsa Kaydet KAPALIDIR.
// ⚠ Blazor'da disabled="false" bir DİZGEDİR ve düğmeyi yine kapatır; assert
//   niteliğin VARLIĞINA bakar.
// ⚠ Sahte API'ye /faturalar/{id}/kalemler yanıtı tanımlanmazsa "{}" döner ve
//   JsonException düşer; ekran onu ApiHata gibi yakalar (UX-S17).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class FaturaEkranTestleri : BunitContext
{

    // ZO-BELGE (tasarım hakemi t3, K22): fatura formlarındaki yerel <select>'ler ortak
    // Secici (uzun liste) ve Form/Secim (radyo) ailesine geçti; id'ler AYNI.
    private static void SeciciSec<T>(IRenderedComponent<T> e, string id, string deger)
        where T : Microsoft.AspNetCore.Components.IComponent
    {
        e.Find($"#{id}").Click();
        e.Find($"#{id}-liste li[data-deger='{deger}']").Click();
    }

    private static void RadyoSec<T>(IRenderedComponent<T> e, string id, string deger)
        where T : Microsoft.AspNetCore.Components.IComponent
        => e.Find($"#{id} input[value='{deger}']").Change(deger);

    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool kalemliFatura = true;
    private bool fisFaturali;
    private bool fisAlis;
    // ZO-BELGE 29.09 (orkestratör 18:05): pencere taraması için elle verilen fiş yanıtı.
    private string? fisYanitOzel;
    // Y-40: VARSAYILAN KAPALI — kurulum dosyasıyla aynı (PLAN:578/778, K-10 beklenir).
    private bool ozelMatrahAcik;

    public FaturaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        // P-15: seçici gecikmesi testte 0 — 250 ms beklemek davranış değil SÜRE ölçerdi.
        Kuyumcu.Web.Bilesenler.KayitSecici.BeklemeMs = 0;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => kalemliFatura, () => fisFaturali, () => ozelMatrahAcik, this))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        // ProAlan (UX-3) bileşeni bu servisi enjekte eder — paket 38 kırılımı ProAlan içinde.
        Services.AddScoped<Kuyumcu.Web.Servisler.ArayuzModuServisi>();
    }

    private sealed class SahteApi(
        List<(string, string, string)> kayit, Func<bool> kalemliMi, Func<bool> fisFaturaliMi,
        Func<bool> ozelMatrahAcikMi, FaturaEkranTestleri t)
        : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            // P-54 senaryoları: kalem okuma/düzeltme ucu GERÇEKÇİ yanıt verir;
            // PATCH sonrası liste yeni toplamla döner (sunucu başlığı kalemlerden toplar).
            if (t.p54PatchYanit is { } p54Patch && t.p53PatchHata is null
                && istek.Method == HttpMethod.Patch && yol.Contains("/kalemler/"))
            {
                if (t.p54ListeSonra is { } sonra) t.p53Fatura = sonra;
                return Tamam(p54Patch);
            }
            // PAKET YASAL-4 / Y4-C: kiracıda KUYUM fatura VAR senaryosu (rozet).
            if (t.y4KuyumFaturaVar && yol.Contains("/ozel-matrah-ayari"))
                return Tamam("""
                    {"kuyumFaturaVar":true,"kod":null,"acik":false,"aralikta":false,"aralik":"801-812",
                     "varsayilanOran":0.20,
                     "kaynakBeyani":"KDV 23/e özel matrah neden kodu bu kurulumda KAPALIDIR — Kdv:OzelMatrahKodu = 805 (K-10).",
                     "maliMusavirTeyidi":false}
                    """);
            if (t.p54Kalemler is { } p54Get && istek.Method == HttpMethod.Get && yol.Contains("/kalemler"))
                return Tamam(p54Get);

            // P-53 senaryoları: sunucu 400'ü (PATCH) ve cari listesi arızası (500).
            if (t.p53PatchHata is { } hataMetni && istek.Method == HttpMethod.Patch)
                return Hata(System.Net.HttpStatusCode.BadRequest, hataMetni);
            if (t.p53CariHata && yol.Contains("/cari"))
                return Hata(System.Net.HttpStatusCode.InternalServerError, "Sunucu hatası");
            if (t.p53Fatura is not null && yol.Contains("/cari"))
                return Tamam("""[{"id":"c1","ad":"Ayşe Demir"},{"id":"c2","ad":"Mehmet Kaya"}]""");
            if (t.p53Fatura is { } p53Liste && (yol == "/api/v1/faturalar" || yol.StartsWith("/api/v1/faturalar?", StringComparison.Ordinal))
                && istek.Method == HttpMethod.Get)
                return Tamam(p53Liste);

            var y = yol switch
            {
                // PAKET P-15 (B-25): fiş artık ULID yazılarak DEĞİL, kayıt seçiciyle
                // bulunur. Sahte API'nin "{}" varsayılanı JsonException doğururdu;
                // yeni uca GERÇEKÇİ yanıt verilir (bUnit sözleşmesi).
                var v when v.StartsWith("/api/v1/arama", StringComparison.Ordinal) => """
                    {"sorgu":"Ayşe","tur":"FIS","limit":10,"ofset":0,"sayi":1,
                     "dahaVar":false,"tavan":50,
                     "sonuclar":[{"kimlik":"FIS1","ad":"Ayşe Demir · 16.09.2026",
                       "altBilgi":"2 kalem · 3.600 ₺","tur":"FIS","rota":null}]}
                    """,
                // Fiş okuma ucu (paket 1 Faz A) — "fişten fatura kes" önizlemesinin kaynağı.
                var v when v.Contains("/islemler/fis/") && t.fisYanitOzel is { } ozel => ozel,
                var v when v.Contains("/islemler/fis/") => fisFaturaliMi()
                    ? """
                      {"fisId":"FIS1","cariId":"c1","cariAd":"Ayşe Demir",
                       "faturaNo":"FTR-2026-000001","kalemSayisi":2,"toplamTl":3600.00,
                       "toplamHas":-10.915,"kalemler":[
                         {"id":"h1","tur":"SATIS","tlTutari":1000.00,"hasGrami":-9.16,
                          "hediye":false,"urunAd":"Burma Bilezik","faturaNo":"FTR-2026-000001"},
                         {"id":"h2","tur":"SATIS","tlTutari":2600.00,"hasGrami":-1.755,
                          "hediye":false,"urunAd":"Tek Taş Yüzük","faturaNo":"FTR-2026-000001"}]}
                      """
                    : t.fisAlis
                    // Doğrulayıcı 29.09: ALIŞ fişi — defterde TL eksi, has artı (L-1b alış fişi biçimi).
                    ? """
                      {"fisId":"FIS1","cariId":"c1","cariAd":"Ayşe Demir","faturaNo":null,
                       "kalemSayisi":2,"toplamTl":-2100.50,"toplamHas":3.5,"kalemler":[
                         {"id":"h1","tur":"ALIS","tlTutari":-1500.50,"hasGrami":2.5,
                          "hediye":false,"urunAd":"Burma Bilezik","faturaNo":null},
                         {"id":"h2","tur":"HURDA_ALIS","tlTutari":-600.00,"hasGrami":1.0,
                          "hediye":false,"urunAd":null,"faturaNo":null}]}
                      """
                    : """
                      {"fisId":"FIS1","cariId":"c1","cariAd":"Ayşe Demir","faturaNo":null,
                       "kalemSayisi":2,"toplamTl":3600.00,"toplamHas":-10.915,"kalemler":[
                         {"id":"h1","tur":"SATIS","tlTutari":1000.00,"hasGrami":-9.16,
                          "hediye":false,"urunAd":"Burma Bilezik","faturaNo":null},
                         {"id":"h2","tur":"SATIS","tlTutari":2600.00,"hasGrami":-1.755,
                          "hediye":true,"urunAd":"Tek Taş Yüzük","faturaNo":null}]}
                      """,
                var v when v.Contains("/kalemler") => kalemliMi()
                    ? """
                      {"faturaId":"f1","faturaNo":"FTR-2026-000001","kalemKaynagi":"KALEM",
                       "hasDegerKaynagi":"KALEM","toplam":3600.00,
                       "kalemler":[
                         {"id":"k1","sira":1,"aciklama":"Burma Bilezik","miktar":1,"birim":"ADET",
                          "ayarId":"K22","milyem":916,"gramAgirlik":10,"hasGrami":9.16,
                          "tasIbaresi":null,"birimFiyat":1000,"iscilikTl":0,"indirimTutari":0,
                          "kdvTuru":"STANDART","kdvOran":0.2,"hasDeger":0,"kdvMatrah":833.33,
                          "kdvTutar":166.67,"satirToplam":1000.00},
                         {"id":"k2","sira":2,"aciklama":"Tek Taş Yüzük","miktar":2,"birim":"ADET",
                          "ayarId":"K14","milyem":585,"gramAgirlik":3,"hasGrami":1.755,
                          "tasIbaresi":"sentetik taşlıdır","birimFiyat":1300,"iscilikTl":0,
                          "indirimTutari":0,"kdvTuru":"STANDART","kdvOran":0.2,"hasDeger":0,
                          "kdvMatrah":2166.67,"kdvTutar":433.33,"satirToplam":2600.00}]}
                      """
                    : """
                      {"faturaId":"f1","faturaNo":"FTR-2026-000001","kalemKaynagi":"BASLIK",
                       "hasDegerKaynagi":"ELLE","toplam":12000.00,"kalemler":[]}
                      """,
                // Paket 38 — faturalandırılacak işlem seçicisi ve özel matrah önizlemesi.
                var v when v.Contains("/faturalanabilir-islemler") => """
                    {"gun":30,"limit":50,"dahaVar":false,"islemler":[
                      {"id":"h1","fisId":null,"tur":"SATIS","tarih":"2026-09-16T10:00:00Z",
                       "tlTutari":75000.00,"hasGrami":10.0,"cariId":"c1","urunAd":"Burma Bilezik"},
                      {"id":"h2","fisId":null,"tur":"HURDA_ALIS","tarih":"2026-09-16T11:00:00Z",
                       "tlTutari":-10000.00,"hasGrami":9.25,"cariId":"c1","urunAd":null}]}
                    """,
                // PAKET P-26 (Y-40) — özel matrah AYARI ucu. ⚠ Bu dal
                // "/api/v1/faturalar" dalından ÖNCE gelmek ZORUNDA: yol o önekle
                // başlıyor ve liste yanıtına düşerse ekran beyanı sessizce boş
                // gösterirdi (bUnit sözleşmesi: yeni uca GERÇEKÇİ yanıt).
                // Y-40 — KAPALI kurulum VARSAYILANDIR (K-10 teyidi beklenir);
                // açık yanıt yalnız teyit senaryosunu sınayan testte verilir.
                var v when v.Contains("/ozel-matrah-ayari") => ozelMatrahAcikMi() ? """
                    {"kod":"805","acik":true,"aralikta":true,"aralik":"801-812",
                     "varsayilanOran":0.20,
                     "kaynakBeyani":"Özel matrah neden kodu 805 — kaynak: KDVK md.23/e + GİB e-belge özel matrah kod listesi (801-812). Bu bir mali müşavir imzası DEĞİLDİR; ilk gerçek faturadan önce onaylatılması tavsiye edilir. Yorum farklıysa kod tek alandan değiştirilebilir.",
                     "maliMusavirTeyidi":false}
                    """ : """
                    {"kod":null,"acik":false,"aralikta":false,"aralik":"801-812",
                     "varsayilanOran":0.20,
                     "kaynakBeyani":"KDV 23/e özel matrah neden kodu bu kurulumda KAPALIDIR — faturaya kod BASILMAZ ve muhasebe fişinde alan BOŞ bırakılır (uydurulmaz). Ürün kapsamı: özel matrah neden kodu beyanı HARİÇ. Sebep: mali müşavir teyidi (kurucu kalemi K-10) beklenmektedir.",
                     "maliMusavirTeyidi":false}
                    """,
                // ZO-BELGE 30.09: ürünsüz gümüş hurda (h2) — has bacağı altın DIŞI → bayrak true.
                var v when v.Contains("/ozel-matrah-onizleme") && v.Contains("hareketId=h2") => """
                    {"hareketId":"h2","fisId":null,"kapsananHareketIdler":["h2"],
                     "kapsananHareketSayisi":1,"hasGram":0,"hasDeger":0,
                     "hasBirimFiyat":null,"kullanilanKurSayisi":0,"defterToplami":10000.00,
                     "kdvOran":0.20,"matrah":null,"kdvTutar":null,"araToplam":null,
                     "hasDegerKaynagi":"HAREKET","faturaNo":null,"uyari":"Has bacağı yok — özel matrah uygulanmadı",
                     "altinDisiMadenVar":true}
                    """,
                var v when v.Contains("/ozel-matrah-onizleme") => """
                    {"hareketId":"h1","fisId":null,"kapsananHareketIdler":["h1"],
                     "kapsananHareketSayisi":1,"hasGram":10.0,"hasDeger":68272.00,
                     "hasBirimFiyat":6827.20,"kullanilanKurSayisi":1,"defterToplami":75000.00,
                     "kdvOran":0.20,"matrah":5606.67,"kdvTutar":1121.33,"araToplam":73878.67,
                     "hasDegerKaynagi":"HAREKET","faturaNo":null,"uyari":null}
                    """,
                var v when v.StartsWith("/api/v1/faturalar", StringComparison.Ordinal)
                           && istek.Method == HttpMethod.Get => """
                    {"faturalar":[{"id":"f1","faturaNo":"FTR-2026-000001","cariAd":"Ayşe Demir",
                      "tur":"SATIS","araToplam":3000.00,"kdvOran":0.2,"kdvTutar":600.00,
                      "toplam":3600.00,"durum":"TASLAK","bankaHareketId":null,
                      "tarih":"2026-09-16T08:00:00Z","kalemKaynagi":"KALEM","hasDegerKaynagi":"KALEM"}]}
                    """,
                var v when v.Contains("/urunler") => """
                    [{"id":"u1","ad":"Burma Bilezik"},{"id":"u2","ad":"Tek Taş Yüzük"}]
                    """,
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }

        private static HttpResponseMessage Tamam(string json) => new(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        };

        /// <summary>Nest hata sözleşmesi: { message, error, statusCode }.</summary>
        private static HttpResponseMessage Hata(System.Net.HttpStatusCode kod, string mesaj) => new(kod)
        {
            Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new { message = mesaj, error = "Bad Request", statusCode = (int)kod }),
                System.Text.Encoding.UTF8, "application/json"),
        };
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> EkranAc()
    {
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-goster-0");
        return e;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> YeniModalAc()
    {
        var e = EkranAc();
        e.FindAll("button").First(b => b.TextContent.Contains("Yeni fatura")).Click();
        e.WaitForElement("#fk-ekle");
        return e;
    }

    private static AngleSharp.Dom.IElement Kaydet(IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> e)
        => e.Find("#fk-kaydet");

    // ── PAKET P-26 (Y-40) — ÖZEL MATRAH BEYANI ──────────────────────────────

    [Fact]
    public void Y40_ozel_matrah_kodu_EKRANDA_ve_KAYNAGIYLA_BIRLIKTE_gorunur()
    {
        ozelMatrahAcik = true;   // K-10 teyidi GELDİĞİ senaryo
        var e = EkranAc();
        // YASAL-4 / Y4-C: katlanır KALKTI; kod AÇIKKEN başlık altında kaynağıyla birlikte.

        // Kod tek başına değil; kaynağı, kararı ve mali müşavir sınırı AYNI kartta.
        e.WaitForAssertion(() => Assert.Equal("805", e.Find("#fk-ozel-matrah-kod").TextContent.Trim()));
        var kaynak = e.Find("#fk-ozel-matrah-kaynak").TextContent;
        Assert.Contains("23/e", kaynak);
        Assert.Contains("mali müşavir imzası DEĞİLDİR", kaynak);
        Assert.Empty(e.FindAll("#fk-ozel-matrah-haric"));

        // Kod GİB aralığında → aralık uyarısı BASILMAZ.
        Assert.Empty(e.FindAll("#fk-ozel-matrah-aralik"));
    }

    // PLAN:578 — teyit gelmezse ürün "HARİÇ" beyanını EKRANDA yazar; sessiz
    // boşluk (kodsuz, beyansız kart) OZEL_MATRAH_BEYANSIZ_EKRAN ihlalidir.
    /// <summary>
    /// PAKET YASAL-4 / Y4-C (K-11): teyit yokken sayfa sonundaki katlanır YOK, kod YOK;
    /// kiracıda KUYUM fatura yoksa rozet de YOK (sade ekran). Geliştirici dili ekranda 0.
    /// </summary>
    [Fact]
    public void Y40_teyit_yokken_kod_rozeti_YOK_HARIC_beyani_VAR()
    {
        var e = EkranAc();   // ozelMatrahAcik = false (varsayılan kurulum)
        Assert.Empty(e.FindAll("#fk-ozel-matrah-beyani"));
        Assert.Empty(e.FindAll("#fk-ozel-matrah-beyani-ac"));
        Assert.Empty(e.FindAll("#fk-ozel-matrah-kod"));
        Assert.Empty(e.FindAll("#fk-ozel-matrah-rozet"));
        Assert.DoesNotContain("K-10", e.Markup);
    }

    // ── PAKET YASAL-4 / Y4-C — K-10 · K-11 · K-12 ───────────────────────────

    [Fact]
    public void Y4C_K11_KUYUM_fatura_varken_tek_satir_rozet_1_katlanir_0_yokken_rozet_0()
    {
        y4KuyumFaturaVar = true;
        var e = EkranAc();
        e.WaitForElement("#fk-ozel-matrah-rozet");
        Assert.Single(e.FindAll("#fk-ozel-matrah-rozet"));
        Assert.Equal(Kuyumcu.Core.I18n.Yasal4Sozlugu.Tr.FaturaKodRozet, e.Find("#fk-ozel-matrah-rozet").TextContent.Trim());
        Assert.Equal("/yasal-sinirlar#ys-satir-kdv-ozel-matrah-kodu", e.Find("#fk-ozel-matrah-rozet").GetAttribute("href"));
        Assert.Empty(e.FindAll("#fk-ozel-matrah-beyani"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Y4C_K12_ekranda_gelistirici_dili_YOK_Basit_ve_Pro(bool basit)
    {
        y4KuyumFaturaVar = true;   // sunucu metni jargon taşısa bile ekrana basılmaz
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        var e = EkranAc();
        e.WaitForElement("#fk-ozel-matrah-rozet");
        // Görünen METİN taranır (işaretleme değil): Blazor'un rastgele GUID öznitelikleri "805" içerebiliyordu
        // (17.10 tam koşuda tek seferlik kırmızı — kimlik değeri ekranda görünen dil değildir).
        var gorunen = System.Text.RegularExpressions.Regex.Replace(e.Markup, "<[^>]*>", " ");
        foreach (var yasak in new[] { "Kdv:OzelMatrahKodu", "K-10", "805", "yapılandırma" })
            Assert.DoesNotContain(yasak, gorunen);
    }

    [Fact]
    public void Y4C_K10_KUYUM_faturada_has_bedeli_KDV_disi_satiri_motor_degeriyle_ayni()
    {
        p54Kalemler = """
            {"faturaId":"f1","faturaNo":"FTR-2026-000001","kalemKaynagi":"KALEM",
             "hasDegerKaynagi":"KALEM","toplam":80000.00,
             "kalemler":[
               {"id":"k1","sira":1,"aciklama":"Burma Bilezik","miktar":1,"birim":"ADET",
                "ayarId":"K22","milyem":916,"gramAgirlik":10,"hasGrami":9.16,
                "tasIbaresi":null,"birimFiyat":80000,"iscilikTl":0,"indirimTutari":0,
                "kdvTuru":"KUYUM","kdvOran":0.2,"hasDeger":62538.73,"kdvMatrah":9551.06,
                "kdvTutar":1910.21,"satirToplam":80000.00,"maden":"ALTIN"}]}
            """;
        var e = EkranAc();
        e.Find("#fk-goster-0").Click();
        e.WaitForElement("#fd-has-kdv-disi");
        Assert.Equal(Kuyumcu.Core.I18n.Yasal4Sozlugu.Tr.FaturaHasSatiri,
            e.Find("#fd-has-kdv-disi span").TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(62538.73m), e.Find("#fd-has-kdv-disi-tutar").TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(9551.06m), e.Find("#fd-kdv-matrahi-tutar").TextContent.Trim());
    }

    [Fact]
    public void Y4C_K10_gumus_kalem_HAS_ALTIN_satirina_girmez_ayri_kulce_gumus_satiri()
    {
        p54Kalemler = """
            {"faturaId":"f1","faturaNo":"FTR-2026-000001","kalemKaynagi":"KALEM",
             "hasDegerKaynagi":"KALEM","toplam":90000.00,
             "kalemler":[
               {"id":"k1","sira":1,"aciklama":"Burma Bilezik","miktar":1,"birim":"ADET",
                "ayarId":"K22","milyem":916,"gramAgirlik":10,"hasGrami":9.16,
                "tasIbaresi":null,"birimFiyat":80000,"iscilikTl":0,"indirimTutari":0,
                "kdvTuru":"KUYUM","kdvOran":0.2,"hasDeger":62538.73,"kdvMatrah":9551.06,
                "kdvTutar":1910.21,"satirToplam":80000.00,"maden":"ALTIN"},
               {"id":"k2","sira":2,"aciklama":"Gümüş Künye","miktar":1,"birim":"ADET",
                "ayarId":"G925","milyem":925,"gramAgirlik":100,"hasGrami":92.5,
                "tasIbaresi":null,"birimFiyat":10000,"iscilikTl":0,"indirimTutari":0,
                "kdvTuru":"KUYUM","kdvOran":0.2,"hasDeger":7400.00,"kdvMatrah":2166.67,
                "kdvTutar":433.33,"satirToplam":10000.00,"maden":"GUMUS"}]}
            """;
        var e = EkranAc();
        e.Find("#fk-goster-0").Click();
        e.WaitForElement("#fd-gumus-kdv-disi");
        Assert.Equal(Bicim.TlKurus(62538.73m), e.Find("#fd-has-kdv-disi-tutar").TextContent.Trim());   // gümüş EKLENMEZ
        Assert.Equal(Kuyumcu.Core.I18n.Yasal4Sozlugu.Tr.FaturaGumusSatiri, e.Find("#fd-gumus-kdv-disi span").TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(7400m), e.Find("#fd-gumus-kdv-disi-tutar").TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(9551.06m + 2166.67m), e.Find("#fd-kdv-matrahi-tutar").TextContent.Trim());
    }

    [Fact]
    public void Y4C_K10_gumus_has_bedeli_0_ise_gumus_satiri_YOK_altin_satiri_yalniz_altini_toplar()
    {
        p54Kalemler = """
            {"faturaId":"f1","faturaNo":"FTR-2026-000001","kalemKaynagi":"KALEM",
             "hasDegerKaynagi":"KALEM","toplam":90000.00,
             "kalemler":[
               {"id":"k1","sira":1,"aciklama":"Burma Bilezik","miktar":1,"birim":"ADET",
                "ayarId":"K22","milyem":916,"gramAgirlik":10,"hasGrami":9.16,
                "tasIbaresi":null,"birimFiyat":80000,"iscilikTl":0,"indirimTutari":0,
                "kdvTuru":"KUYUM","kdvOran":0.2,"hasDeger":62538.73,"kdvMatrah":9551.06,
                "kdvTutar":1910.21,"satirToplam":80000.00,"maden":"ALTIN"},
               {"id":"k2","sira":2,"aciklama":"Gümüş Künye","miktar":1,"birim":"ADET",
                "ayarId":"G925","milyem":925,"gramAgirlik":100,"hasGrami":92.5,
                "tasIbaresi":null,"birimFiyat":10000,"iscilikTl":0,"indirimTutari":0,
                "kdvTuru":"KUYUM","kdvOran":0.2,"hasDeger":0,"kdvMatrah":8333.33,
                "kdvTutar":1666.67,"satirToplam":10000.00,"maden":"GUMUS"}]}
            """;
        var e = EkranAc();
        e.Find("#fk-goster-0").Click();
        e.WaitForElement("#fd-has-kdv-disi");
        Assert.Empty(e.FindAll("#fd-gumus-kdv-disi"));
        Assert.Equal(Bicim.TlKurus(62538.73m), e.Find("#fd-has-kdv-disi-tutar").TextContent.Trim());
    }

    [Fact]
    public void Y4C_K10_yalniz_altin_faturada_gumus_satiri_YOK()
    {
        p54Kalemler = """
            {"faturaId":"f1","faturaNo":"FTR-2026-000001","kalemKaynagi":"KALEM",
             "hasDegerKaynagi":"KALEM","toplam":80000.00,
             "kalemler":[
               {"id":"k1","sira":1,"aciklama":"Burma Bilezik","miktar":1,"birim":"ADET",
                "ayarId":"K22","milyem":916,"gramAgirlik":10,"hasGrami":9.16,
                "tasIbaresi":null,"birimFiyat":80000,"iscilikTl":0,"indirimTutari":0,
                "kdvTuru":"KUYUM","kdvOran":0.2,"hasDeger":62538.73,"kdvMatrah":9551.06,
                "kdvTutar":1910.21,"satirToplam":80000.00,"maden":"ALTIN"}]}
            """;
        var e = EkranAc();
        e.Find("#fk-goster-0").Click();
        e.WaitForElement("#fd-has-kdv-disi");
        Assert.Empty(e.FindAll("#fd-gumus-kdv-disi"));
    }

    [Fact]
    public void Y4C_K10_STANDART_faturada_has_satiri_YOK()
    {
        var e = EkranAc();
        e.Find("#fk-goster-0").Click();
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll("tr.fd-kalem-satir").Count));
        Assert.Empty(e.FindAll("#fd-has-kdv-disi"));
    }

    [Fact]
    public void Y40_ayar_ucu_ekran_acilir_acilmaz_BIR_KEZ_cagrilir()
    {
        EkranAc();
        Assert.Single(istekler, i => i.Yol.Contains("/ozel-matrah-ayari"));
    }

    // ── Kalem tablosu ────────────────────────────────────────────────────────

    [Fact]
    public void Kalem_tablosu_satir_sayisi_yanittaki_kalem_sayisina_esittir()
    {
        var e = EkranAc();
        e.Find("#fk-goster-0").Click();

        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll("tr.fd-kalem-satir").Count));
    }

    [Fact]
    public void Kalemsiz_faturada_bos_tablo_yerine_BASLIK_DUZEYI_aciklamasi_gorunur()
    {
        kalemliFatura = false;
        var e = EkranAc();
        e.Find("#fk-goster-0").Click();

        e.WaitForAssertion(() =>
        {
            Assert.Empty(e.FindAll("tr.fd-kalem-satir"));
            Assert.Contains("başlık düzeyindedir", e.Markup);
        });
    }

    // ── Tolerans kapısının ekran karşılığı ───────────────────────────────────

    [Fact]
    public void Kalem_toplami_fatura_toplamiyla_UYUSMAYINCA_Kaydet_KAPALI()
    {
        var e = YeniModalAc();
        SeciciSec(e, "fatura-cari", "c1");
        e.Find("#fatura-toplam").Input("3600");
        // Kaydet önce AÇIK (izolasyon: kapalılığın tek sebebi toplam uyuşmazlığı olsun)
        e.WaitForAssertion(() => Assert.False(Kaydet(e).HasAttribute("disabled")));

        e.Find("#fk-ekle").Click();
        e.WaitForElement("#fk-miktar-0");
        e.Find("#fk-miktar-0").Input("1");
        e.Find("#fk-fiyat-0").Input("1000");           // kalem toplamı 1.000 ≠ 3.600

        e.WaitForAssertion(() => Assert.True(Kaydet(e).HasAttribute("disabled")));
    }

    [Fact]
    public void Kalem_toplami_fatura_toplamina_ESIT_olunca_Kaydet_ACIK()
    {
        var e = YeniModalAc();
        SeciciSec(e, "fatura-cari", "c1");
        e.Find("#fatura-toplam").Input("1000");
        e.Find("#fk-ekle").Click();
        e.WaitForElement("#fk-miktar-0");
        e.Find("#fk-miktar-0").Input("1");
        e.Find("#fk-fiyat-0").Input("1000");

        e.WaitForAssertion(() => Assert.False(Kaydet(e).HasAttribute("disabled")));
    }

    // ── POST gövdesi ─────────────────────────────────────────────────────────

    [Fact]
    public void Kalemli_kaydetmede_POST_govdesi_kalemleri_TASIR()
    {
        var e = YeniModalAc();
        SeciciSec(e, "fatura-cari", "c1");
        e.Find("#fatura-toplam").Input("1000");
        e.Find("#fk-ekle").Click();
        e.WaitForElement("#fk-urun-0");
        SeciciSec(e, "fk-urun-0", "u1");
        e.Find("#fk-miktar-0").Input("1");
        e.Find("#fk-fiyat-0").Input("1000");
        istekler.Clear();
        Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);

        e.WaitForAssertion(() =>
        {
            var post = istekler.First(i => i.Metot == "POST");
            var govde = System.Text.Json.JsonDocument.Parse(post.Govde).RootElement;
            var kalemler = govde.GetProperty("kalemler").EnumerateArray().ToList();
            Assert.Single(kalemler);
            Assert.Equal("u1", kalemler[0].GetProperty("urunId").GetString());
            Assert.Equal(1000m, kalemler[0].GetProperty("birimFiyat").GetDecimal());
        });
    }

    // ── Fişten fatura kes akışı (bağlayıcı çelişki çözümü) ───────────────────

    private IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> FistenModalAc()
    {
        var e = EkranAc();
        e.Find("#ff-ac").Click();
        e.WaitForElement("#ff-fis");
        return e;
    }

    /// <summary>
    /// PAKET P-15 (B-25): fiş kimliği ELLE YAZILMAZ. Kullanıcı arar ve
    /// sonuçtan seçer; ULID yalnız gövdeye gider. Girdi kimliği `#ff-fis`
    /// DEĞİŞMEDİ (mevcut id sözleşmesi) — değişen, ne yazıldığıdır.
    /// </summary>
    private static void FisSec(IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> e)
    {
        e.Find("#ff-fis").Input("Ayşe");
        e.WaitForElement("#ff-fis-sonuc-0");
        e.Find("#ff-fis-sonuc-0").Click();
        e.WaitForElement("#ff-fis-secili");
    }

    // ZO-BELGE 29.09 devam 9 (tasarım hakemi k3 KRİTİK): Fişten Fatura Kes penceresi — etiket "Fiş" (kimlik
    // değil), ipucu TEK kez ve "sunucuda" demez; seçilen fişin alt bilgisi tr-TR ve ₺ (sunucu EkranPara);
    // arama sonucu sarmalı alt bilgiyi her satırda adın altına alır (zo-belge.css .zo-fk-fis).
    [Fact]
    public void ZB65_Fisten_fatura_kes_etiketi_Fis_ipucu_tek_ve_sade_tutar_TR()
    {
        var e = FistenModalAc();
        var T = Kuyumcu.Core.I18n.Sozlukler.Getir("tr");
        var zb = Kuyumcu.Core.I18n.ZoBelgeSozlugu.Tr;
        Assert.StartsWith(zb.FistenFisEtiket, e.Find("label[for='ff-fis']").TextContent.Trim());
        Assert.DoesNotContain(T.Fatura.FisKimligi, e.Markup);
        Assert.DoesNotContain(T.Fatura.FisOnizlemeIpucu, e.Markup);
        Assert.DoesNotContain("sunucuda", zb.FistenIpucu);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(e.Markup, System.Text.RegularExpressions.Regex.Escape(zb.FistenIpucu)));
        Assert.NotNull(e.Find(".zo-fk-fis #ff-fis-sarmal"));
        FisSec(e);
        var alt = e.Find("#ff-fis-secili .metin-kucuk").TextContent.Trim();
        Assert.Equal("2 kalem · 3.600 ₺", alt);
        Assert.DoesNotContain("TRY", e.Find("#ff-fis-secili").TextContent);
        var css = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "wwwroot", "css", "zo-belge.css"));
        Assert.Contains(".zo-fk-fis .secici-secili > .secici-ad { flex: 1 1 100%;", css);
    }

    [Fact]
    public void Fisten_faturada_ONIZLEME_fisin_TUM_kalemlerini_gosterir()
    {
        var e = FistenModalAc();
        FisSec(e);
        e.Find("#ff-onizle").Click();

        e.WaitForAssertion(() =>
        {
            Assert.Equal(2, e.FindAll("tr.ff-kalem-satir").Count);
            Assert.Equal("2", e.Find("#ff-kalem-sayisi").TextContent.Trim());
            // ZO-BELGE 28.09 (P-57): kalem has gramı 4 hane ("-1,755" → "-1,7550 has").
            var hucreler = e.FindAll("#ff-kalem-tablosu tbody td").Select(h => h.TextContent.Trim()).ToList();
            // ZO-BELGE 29.09 (orkestratör 18:05): önizleme FATURAYI gösterir — işaretsiz (sunucu Math.Abs).
            Assert.Contains(Bicim.Has(9.16m), hucreler);
            Assert.Contains(Bicim.Has(1.755m), hucreler);
            Assert.False(e.Find("#ff-kaydet").HasAttribute("disabled"));
        });
    }

    [Fact]
    public void Onizleme_YAPILMADAN_fisten_Kaydet_KAPALI()
    {
        var e = FistenModalAc();
        FisSec(e);

        // disabled="false" bir DİZGEDİR: assert niteliğin VARLIĞINA bakar.
        e.WaitForAssertion(() => Assert.True(e.Find("#ff-kaydet").HasAttribute("disabled")));
    }

    [Fact]
    public void Zaten_faturalanmis_fiste_Kaydet_KAPALI()
    {
        fisFaturali = true;
        var e = FistenModalAc();
        FisSec(e);
        e.Find("#ff-onizle").Click();

        e.WaitForAssertion(() =>
        {
            Assert.Contains("FTR-2026-000001", e.Markup);
            Assert.True(e.Find("#ff-kaydet").HasAttribute("disabled"));
        });
    }

    [Fact]
    public void Fisten_kaydetme_POST_faturalar_fisten_ucuna_gider_ve_govdede_fisId_tasir()
    {
        var e = FistenModalAc();
        FisSec(e);
        e.Find("#ff-onizle").Click();
        e.WaitForAssertion(() => Assert.False(e.Find("#ff-kaydet").HasAttribute("disabled")));
        istekler.Clear();
        e.Find("#ff-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var post = istekler.First(i => i.Metot == "POST");
            Assert.EndsWith("/faturalar/fisten", post.Yol, StringComparison.Ordinal);
            var govde = System.Text.Json.JsonDocument.Parse(post.Govde).RootElement;
            Assert.Equal("FIS1", govde.GetProperty("fisId").GetString());
            Assert.Equal("STANDART", govde.GetProperty("kdvTuru").GetString());
            // Kalem listesi GÖNDERİLMEZ: kalemler sunucuda defterden kurulur.
            Assert.False(govde.TryGetProperty("kalemler", out _));
        });
    }

    [Fact]
    public void Kalemsiz_kaydetmede_POST_govdesinde_kalemler_NULL_kalir()
    {
        var e = YeniModalAc();
        SeciciSec(e, "fatura-cari", "c1");
        e.Find("#fatura-toplam").Input("1000");
        istekler.Clear();
        Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);

        e.WaitForAssertion(() =>
        {
            var post = istekler.First(i => i.Metot == "POST");
            var govde = System.Text.Json.JsonDocument.Parse(post.Govde).RootElement;
            Assert.Equal(System.Text.Json.JsonValueKind.Null,
                govde.GetProperty("kalemler").ValueKind);
        });
    }

    // ── Paket 38: KUYUM'da has bedeli KULLANICIDAN İSTENMEZ ─────────────────

    /// <summary>
    /// UX-S21: sunucunun bildiği alan giriş kutusu olarak konmaz. KUYUM
    /// seçilince elle "has bedeli" girdisi KAYBOLUR, yerine faturalandırılacak
    /// işlem seçicisi gelir.
    /// </summary>
    [Fact]
    public void KUYUM_secilince_has_bedeli_GIRDISI_kaybolur_ve_islem_secici_cikar()
    {
        var e = YeniModalAc();
        Assert.Empty(e.FindAll("#fatura-islem"));

        RadyoSec(e, "fatura-kdvturu", "KUYUM");

        e.WaitForAssertion(() =>
        {
            Assert.Empty(e.FindAll("#fatura-hasdeger"));     // elle giriş YOK
            Assert.Single(e.FindAll("#fatura-islem"));       // seçici VAR
        });
    }

    [Fact]
    public void KUYUM_da_islem_SECILMEDEN_Kaydet_KAPALI_ve_sebebi_YAZILI()
    {
        var e = YeniModalAc();
        SeciciSec(e, "fatura-cari", "c1");
        e.Find("#fatura-toplam").Input("75000");
        // İzolasyon: STANDART'ta Kaydet AÇIK — kapalılığın tek sebebi KUYUM olsun.
        e.WaitForAssertion(() => Assert.False(Kaydet(e).HasAttribute("disabled")));

        RadyoSec(e, "fatura-kdvturu", "KUYUM");

        e.WaitForAssertion(() =>
        {
            // disabled="false" bir DİZGEDİR: assert niteliğin VARLIĞINA bakar.
            Assert.True(Kaydet(e).HasAttribute("disabled"));
            // UX-S30: kilitli düğme sebebini söyler.
            Assert.NotNull(e.Find("#fom-kilit"));
            // DoD: "tıklanınca hiçbir şey olmayan" düğme yok — önizleme de kapalı.
            Assert.True(e.Find("#fom-yenile").HasAttribute("disabled"));
        });
    }

    [Fact]
    public void Islem_secilince_ONIZLEME_cagrilir_ve_turetilen_has_bedeli_gorunur()
    {
        var e = YeniModalAc();
        SeciciSec(e, "fatura-cari", "c1");
        e.Find("#fatura-toplam").Input("75000");
        RadyoSec(e, "fatura-kdvturu", "KUYUM");
        e.WaitForElement("#fatura-islem");
        istekler.Clear();
        SeciciSec(e, "fatura-islem", "h1");

        e.WaitForAssertion(() =>
        {
            Assert.Contains(istekler, i => i.Yol.Contains("/ozel-matrah-onizleme"));
            Assert.Contains("68.272", e.Find("#fom-hasdeger").TextContent);
            // ZO-BELGE 28.09 (P-57): has gramı 4 hane, birimi "has" (F3 "g" 0,0004 has'ı kaybediyordu).
            Assert.Equal("10,0000 has", e.Find("#fom-hasgram").TextContent.Trim());
            // Matrah kırılımı SUNUCUDAN gelir (ikiz formül silindi).
            Assert.Contains("5.606", e.Find("#fom-matrah").TextContent);
            Assert.False(Kaydet(e).HasAttribute("disabled"));
        });
    }

    // ZO-BELGE 30.09 (hasım DÜŞÜK): "altın dışı madende özel matrah uygulanmaz" notu
    // ZORUNLU-ÖNCE — KUYUM açılışında ve altın işlemde YOK; yalnız seçilen işlemde
    // altın dışı has bacağı varsa (sunucu bayrağı) görünür.
    [Fact]
    public void ZOB_gumus_notu_yalniz_ALTIN_DISI_bacakli_islemde_gorunur()
    {
        var e = YeniModalAc();
        SeciciSec(e, "fatura-cari", "c1");
        RadyoSec(e, "fatura-kdvturu", "KUYUM");
        e.WaitForElement("#fatura-islem");
        Assert.Empty(e.FindAll("#fom-gumus-not"));                 // açılışta yok

        SeciciSec(e, "fatura-islem", "h1");                          // altın işlem
        e.WaitForAssertion(() => Assert.Contains("68.272", e.Find("#fom-hasdeger").TextContent));
        Assert.Empty(e.FindAll("#fom-gumus-not"));

        SeciciSec(e, "fatura-islem", "h2");                          // ürünsüz gümüş hurda
        e.WaitForAssertion(() => Assert.Equal(
            Kuyumcu.Core.I18n.FaturaTaslakSozlugu.Tr.GumusOzelMatrahYok,
            e.Find("#fom-gumus-not").TextContent.Trim()));
    }

    [Fact]
    public void KUYUM_da_POST_govdesi_hareketId_ve_TURETILEN_hasDeger_tasir()
    {
        var e = YeniModalAc();
        SeciciSec(e, "fatura-cari", "c1");
        e.Find("#fatura-toplam").Input("75000");
        RadyoSec(e, "fatura-kdvturu", "KUYUM");
        e.WaitForElement("#fatura-islem");
        SeciciSec(e, "fatura-islem", "h1");
        e.WaitForAssertion(() => Assert.False(Kaydet(e).HasAttribute("disabled")));
        istekler.Clear();
        Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);

        e.WaitForAssertion(() =>
        {
            var post = istekler.First(i => i.Metot == "POST");
            var govde = System.Text.Json.JsonDocument.Parse(post.Govde).RootElement;
            Assert.Equal("h1", govde.GetProperty("hareketId").GetString());
            Assert.Equal("KUYUM", govde.GetProperty("kdvTuru").GetString());
            Assert.Equal(68272.00m, govde.GetProperty("hasDeger").GetDecimal());
        });
    }

    [Fact]
    public void STANDART_ta_govdede_hareketId_NULL_kalir_ve_eski_davranis_surer()
    {
        var e = YeniModalAc();
        SeciciSec(e, "fatura-cari", "c1");
        e.Find("#fatura-toplam").Input("1000");
        istekler.Clear();
        Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);

        e.WaitForAssertion(() =>
        {
            var post = istekler.First(i => i.Metot == "POST");
            var govde = System.Text.Json.JsonDocument.Parse(post.Govde).RootElement;
            Assert.Equal(System.Text.Json.JsonValueKind.Null, govde.GetProperty("hareketId").ValueKind);
            Assert.Equal(System.Text.Json.JsonValueKind.Null, govde.GetProperty("hasDeger").ValueKind);
            Assert.Equal("STANDART", govde.GetProperty("kdvTuru").GetString());
        });
    }

    // ═══ PAKET P-53 — TASLAK DÜZENLEME ÇEKMECESİ (QA tur 8, KRİTİK) ═════════
    //   (1) Çekmece açılınca cari listesi YÜKLENİR (eskiden yalnız yeni-fatura
    //       modalında yükleniyordu → seçici boş).
    //   (2) KdvTuru KUYUM taslakta toplam DAİMA kilitli — sunucu kuralının aynası
    //       (eski koşul HasDegerKaynagi != ELLE idi; ELLE kuyum taslağı 400 yiyordu).
    //   (3) Sunucu 400'ü çekmecede ALAN DÜZEYİNDE, çekmece kapanmaz.
    private string? p53Fatura;
    private string? p53PatchHata;
    private bool p53CariHata;

    private static string P53Liste(string kdvTuru, string hasKaynagi, string cariId) => $$"""
        {"faturalar":[{"id":"f53","faturaNo":null,"cariAd":"Mehmet Kaya","tur":"SATIS",
          "araToplam":1000.00,"kdvOran":0.2,"kdvTutar":200.00,"toplam":1200.00,
          "durum":"TASLAK","bankaHareketId":null,"tarih":"2026-09-23T08:00:00Z",
          "kalemKaynagi":"BASLIK","hasDegerKaynagi":"{{hasKaynagi}}","kdvTuru":"{{kdvTuru}}",
          "cariId":"{{cariId}}","aciklama":"ilk not"}]}
        """;

    private IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> P53CekmeceAc()
    {
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-duzenle-f53");
        e.Find("#fk-duzenle-f53").Click();
        e.WaitForElement("#fd-duzenle-cari");
        return e;
    }

    // ── PAKET CUMA-J (K3, demo taraması): "Numara kesimde verilir" 133 px hücrede iki satırdı ──
    [Fact]
    public void CJ_04_Taslak_numara_hucresi_kisa_ve_tek_satir_uzun_aciklama_title_da()
    {
        p53Fatura = P53Liste("STANDART", "ELLE", "c2");
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-no-yok-f53");
        var s = e.Find("#fk-no-yok-f53");
        // KESIF-P3 (TOPLU DUGME-2 → ZO-BELGE): numarası olmayan taslakta hücre "—"; açıklama title'da.
        Assert.Equal("—", s.TextContent.Trim());
        Assert.Equal(Kuyumcu.Core.I18n.Sozlukler.Tr.Fatura.NoKesimde, s.GetAttribute("title"));
        Assert.Contains("hucre-tek-satir", s.ClassList);
    }

    // ── PAKET CUMA-J (K7, 375 px gerçek tarayıcı): "Fişten fatura kes" filtre şeridinde
    //    durum seçicisiyle aynı esnek satırdaydı → dar ekranda alt alta sarıp kutuAlt±52.
    //    Eylemdir: eylem yuvasında durur, filtre şeridinde yalnız durum seçicisi kalır.
    [Fact]
    public void CJ_08_Fisten_kes_eylem_yuvasinda_filtre_seridinde_yalniz_durum_secicisi()
    {
        p53Fatura = P53Liste("STANDART", "ELLE", "c2");
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-no-yok-f53");
        // ZO-BELGE: filtre yuvası BOŞ (yerel <select> çıktı); durum süzgeci KAPALI
        // "Süzgeç" katlanırında ortak Form/Secim olarak durur.
        Assert.Empty(e.FindAll("#sayfa-basi-faturalar-filtre"));
        Assert.Empty(e.FindAll("#fk-durum-filtre"));
        e.Find("#fk-suzgec-ac").Click();
        Assert.Equal("radiogroup", e.Find("#fk-durum-filtre").GetAttribute("role"));
        var eylem = e.Find("#sayfa-basi-faturalar-eylem");
        Assert.Equal(["ff-ac", "fk-yeni"], eylem.QuerySelectorAll("button").Select(b => b.Id));
        // TEK birincil eylem korunur (UX-1-C2): fişten kes ikincil kalır.
        Assert.Single(eylem.QuerySelectorAll("button.btn-birincil"));
        Assert.Contains("btn-ikincil", e.Find("#ff-ac").ClassList);
    }

    // ── PAKET P-61 — TASLAK satırı 4 eylem taşıyordu (1280 px'te EYLEMLER kırılıyordu) ──
    [Fact]
    public void TaslakSatir_GorunurEylem_EnFazlaUc_KesIptalMenude()
    {
        p53Fatura = P53Liste("STANDART", "ELLE", "c2");
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-duzenle-f53");

        // DAR SÜTUN (10 sütunlu tablo): satırda durumun TEK birincil eylemi + "⋯".
        var gorunen = e.Find("#fk-eylem-f53").QuerySelectorAll("button").Select(b => b.Id).ToList();
        Assert.Equal(["fk-duzenle-f53", "fk-menu-ac-f53"], gorunen);
        var menu = e.Find("#fk-menu-f53");
        Assert.Equal(["fk-goster-0", "fk-kes-f53", "fk-iptal-f53", "fk-menu-kapat-f53"],
                     menu.QuerySelectorAll("button").Select(b => b.Id));
        // Taslakta numara YOK: açıklama sarabilir (sütunu şişirmez); tarih tek satır.
        Assert.DoesNotContain("hucre-tek-satir", e.Find("#fk-no-yok-f53").Closest("td")!.ClassList);
        Assert.Contains("tablo-sik", e.Find("table.veri-tablosu").ClassList);

        // Menüdeki "Kes" gerçek iş yapar: onay penceresi açılır (DoD EK 4 — önce onay).
        Assert.Empty(e.FindAll("#fk-durum-onay"));
        e.Find("#fk-kes-f53").Click();
        e.WaitForElement("#fk-durum-onay");
    }

    [Fact]
    public void DuzenleCekmecesiCarileriYukler()
    {
        p53Fatura = P53Liste("STANDART", "ELLE", "c2");
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-duzenle-f53");
        var oncekiCariIstegi = istekler.Count(i => i.Yol.Contains("/cari"));

        e.Find("#fk-duzenle-f53").Click();
        e.WaitForElement("#fd-duzenle-cari");

        // Çekmece açılışı cari ucunu ÇAĞIRIR (yeni-fatura modalı açılmadan).
        Assert.True(istekler.Count(i => i.Metot == "GET" && i.Yol.Contains("/cari")) > oncekiCariIstegi);
        // ZO-BELGE (K22): ortak Secici — seçenekler açılınca listede (id aynı).
        e.Find("#fd-duzenle-cari").Click();
        e.WaitForAssertion(() =>
        {
            var secenekler = e.FindAll("#fd-duzenle-cari-liste li[role='option']");
            Assert.Equal(2, secenekler.Count);
            Assert.Contains(secenekler, o => o.GetAttribute("data-deger") == "c1");
        });
        // Faturanın MEVCUT carisi ön seçili.
        Assert.Equal("c2", e.Find("#fd-duzenle-cari").GetAttribute("data-deger"));
        // Başarılı yüklemede Kaydet AÇIK.
        Assert.False(e.Find("#cekmece-fatura-kaydet").HasAttribute("disabled"));
    }

    [Fact]
    public void Cari_listesi_yuklenemezse_cekmecede_HATA_BANDI_ve_Kaydet_KILITLI()
    {
        p53Fatura = P53Liste("STANDART", "ELLE", "c2");
        p53CariHata = true;
        var e = P53CekmeceAc();

        e.WaitForAssertion(() =>
            Assert.Contains("Müşteri listesi yüklenemedi", e.Find("#cekmece-fatura").TextContent));
        Assert.True(e.Find("#cekmece-fatura-kaydet").HasAttribute("disabled"));
        // Mevcut cari yine GÖRÜNÜR (boş seçici değil, sessiz kayma değil).
        Assert.Equal("c2", e.Find("#fd-duzenle-cari").GetAttribute("data-deger"));
    }

    [Fact]
    public void KuyumTaslaginaToplamGonderilmez()
    {
        // ELLE kaynaklı KUYUM taslak: eski istemci kuralı (HasDegerKaynagi != ELLE)
        // bu belgede toplamı AÇIK bırakıyordu; sunucu ise KdvTuru KUYUM'da reddeder.
        p53Fatura = P53Liste("KUYUM", "ELLE", "c2");
        var e = P53CekmeceAc();

        // TOPLU-1 (md.1): kilitli toplam devre dışı GİRDİ değil, salt okunur satır (düzenlenebilir görünmez).
        Assert.Empty(e.FindAll("input#fd-duzenle-toplam"));
        Assert.NotNull(e.Find("#fd-duzenle-toplam"));
        // TOPLU-1 tur 2 (doğrulayıcı ORTA): düzenlenebilir taslakta kayıt düzeyindeki amber "değiştirilemez"
        // bandı yok — gerekçe + yol alan düzeyinde nötr not (satırsız kuyum taslağı).
        Assert.Empty(e.FindAll("#degismez-ipucu-FaturaTaslakToplam"));
        var ipucu = e.Find("#fd-duzenle-toplam-sebep").TextContent;
        Assert.Contains("iptal edip", ipucu);
        Assert.Contains("numarası harcamaz", ipucu);
        Assert.Empty(e.FindAll("#fd-duzenle-toplam-sebep button"));

        e.Find("#fd-duzenle-aciklama").Input("kuyum notu");   // PAKET GOC-A: ortak Metin (oninput)
        e.Find("#cekmece-fatura-kaydet").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "PATCH" && i.Yol == "/api/v1/faturalar/f53"));
        var govde = System.Text.Json.JsonDocument.Parse(istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
        Assert.False(govde.TryGetProperty("toplam", out _));
        Assert.Equal("kuyum notu", govde.GetProperty("aciklama").GetString());
        Assert.Equal("c2", govde.GetProperty("cariId").GetString());
    }

    [Fact]
    public void StandartTaslaktaToplamDuzenlenebilir_Ekranda()
    {
        p53Fatura = P53Liste("STANDART", "ELLE", "c2");
        var e = P53CekmeceAc();

        Assert.False(e.Find("#fd-duzenle-toplam").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#degismez-ipucu-FaturaTaslakToplam"));
        e.Find("#fd-duzenle-toplam").Input("1500");
        e.Find("#cekmece-fatura-kaydet").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH"));
        var govde = System.Text.Json.JsonDocument.Parse(istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
        Assert.Equal(1500m, govde.GetProperty("toplam").GetDecimal());
    }

    [Fact]
    public void SunucuHatasiAlanDuzeyindeBasilir()
    {
        p53Fatura = P53Liste("STANDART", "ELLE", "c2");
        p53PatchHata = "Toplam sıfırdan büyük olmalıdır";
        var e = P53CekmeceAc();

        e.Find("#fd-duzenle-toplam").Input("1");
        e.Find("#cekmece-fatura-kaydet").Click();

        // Mesaj ALANIN ALTINDA; bant yalnız "formda hata var" der; çekmece AÇIK.
        e.WaitForAssertion(() => Assert.Equal("Toplam sıfırdan büyük olmalıdır",
            e.Find("#alan-hata-fd-duzenle-toplam").TextContent.Trim()));
        Assert.Contains("hatalı", e.Find("#cekmece-fatura").TextContent);
        Assert.NotNull(e.Find("#fd-duzenle-toplam"));
        Assert.Contains("hatali", e.Find("#fd-duzenle-toplam").GetAttribute("class"));
    }

    [Fact]
    public void Alana_eslenemeyen_sunucu_hatasi_cekmece_BANDINA_yazilir()
    {
        p53Fatura = P53Liste("STANDART", "ELLE", "c2");
        p53PatchHata = "Yalnız TASLAK fatura düzenlenebilir; bu belge KESILDI durumunda.";
        var e = P53CekmeceAc();

        e.Find("#cekmece-fatura-kaydet").Click();

        e.WaitForAssertion(() => Assert.Contains("Yalnız TASLAK fatura düzenlenebilir",
            e.Find("#cekmece-fatura").TextContent));
        Assert.Empty(e.FindAll(".alan-hata"));
        Assert.NotNull(e.Find("#fd-duzenle-cari"));
    }

    // ═══ PAKET P-54 — TASLAK KALEMLERİ EKRANDAN DÜZELTİLİR (QA tur 8, YÜKSEK) ═══
    //   (1) Miktar/birim fiyat/işçilik/indirim/açıklama hücreleri TASLAKTA
    //       satır içi düzenlenir → PATCH /faturalar/{id}/kalemler/{kalemId}.
    //   (2) Toplam EKRANDA HESAPLANMAZ — PATCH yanıtından okunur.
    //   (3) Kesilmiş belgede hücre YOK, sebep yazılı.
    //   (4) Hata (ör. miktar 0) düzeltilen hücrenin ALTINDA.
    //   (5) Silme ucu YOK (ölçüldü) → düğme uydurulmaz, sebep + yol yazılı.
    private string? p54Kalemler;
    private bool y4KuyumFaturaVar;
    private string? p54PatchYanit;
    private string? p54ListeSonra;

    private static string P54Liste(string durum, decimal toplam) => $$"""
        {"faturalar":[{"id":"f54","faturaNo":{{(durum == "TASLAK" ? "null" : "\"FTR-2026-000054\"")}},
          "cariAd":"Mehmet Kaya","tur":"SATIS","araToplam":0,"kdvOran":0.2,"kdvTutar":0,
          "toplam":{{toplam.ToString(System.Globalization.CultureInfo.InvariantCulture)}},
          "durum":"{{durum}}","bankaHareketId":null,"tarih":"2026-09-23T08:00:00Z",
          "kalemKaynagi":"KALEM","hasDegerKaynagi":"KALEM","kdvTuru":"STANDART",
          "cariId":"c2","aciklama":null}]}
        """;

    /// <summary>İki kalem: a = miktar×1000 + işçilik 120, b = 500. Toplam SUNUCUNUN değeri.</summary>
    // ── ZO-BELGE K01 (30.09): kalem penceresi — binlikli para kutusu, değişmeden
    // kaydet istek atmaz, binlikli metin doğru çözülür; BASİT'te teknik sütun yok.
    [Fact]
    public void K01_Kalem_birim_fiyati_BINLIKLI_gosterilir_ve_binlikli_metin_dogru_kaydedilir()
    {
        var e = P54KalemleriAc("TASLAK");
        p54PatchYanit = P54Kalemler(1m, 1000m, 1500m);
        Assert.Equal("1.000,00", e.Find("#si-fatura-kalem-birimFiyat-k54a-ac").TextContent.Trim());
        Assert.Equal("120,00", e.Find("#si-fatura-kalem-iscilikTl-k54a-ac").TextContent.Trim());

        HucreyiKaydet(e, "birimFiyat", "k54a", "1.095.125,00");
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH"));
        var govde = System.Text.Json.JsonDocument.Parse(
            istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
        Assert.Equal(1_095_125m, govde.GetProperty("birimFiyat").GetDecimal());
    }

    [Fact]
    public void K01_Kalem_penceresi_BASIT_teknik_sutunlar_gizli_PRO_gorunur()
    {
        var dil = Services.GetRequiredService<DilServisi>();
        var basit = true;
        dil.BasitModuBagla(() => basit);
        var e = P54KalemleriAc("TASLAK");
        var basliklar = e.FindAll("#fd-kalem-tablosu thead th").Select(t => t.TextContent.Trim()).ToList();
        Assert.DoesNotContain(dil.T.Fatura.KdvMatrah, basliklar);
        Assert.DoesNotContain(dil.T.Fatura.HasDeger, basliklar);
        Assert.Contains(dil.T.Fatura.BirimFiyat, basliklar);
        Assert.Contains(dil.T.Fatura.SatirToplam, basliklar);
        Assert.Equal(basliklar.Count, e.FindAll("tr.fd-kalem-satir")[0].Children.Length);
        // 375 px: tablo kart yığınına döner (yatay kaydırmada miktar/fiyat görünmüyordu);
        // her hücre kendi etiketini taşır.
        Assert.Contains("q10-yigin", e.Find("#fd-kalem-tablosu").ClassList);
        Assert.Equal(basliklar, e.FindAll("tr.fd-kalem-satir")[0].Children
            .Select(td => td.GetAttribute("data-etiket") ?? "").ToList());

        basit = false;
        var p = Render<Kuyumcu.Web.Pages.Faturalar>();
        p.WaitForElement("#fk-goster-0");
        p.Find("#fk-goster-0").Click();
        p.WaitForAssertion(() => Assert.Equal(2, p.FindAll("tr.fd-kalem-satir").Count));
        var proBasliklar = p.FindAll("#fd-kalem-tablosu thead th").Select(t => t.TextContent.Trim()).ToList();
        Assert.Contains(dil.T.Fatura.KdvMatrah, proBasliklar);
        Assert.Contains(dil.T.Fatura.HasDeger, proBasliklar);
        Assert.Contains(dil.T.Fatura.KdvTutar, proBasliklar);
    }

    private static string P54Kalemler(decimal miktarA, decimal satirA, decimal toplam) => $$"""
        {"faturaId":"f54","faturaNo":null,"kalemKaynagi":"KALEM","hasDegerKaynagi":"KALEM",
         "toplam":{{toplam.ToString(System.Globalization.CultureInfo.InvariantCulture)}},
         "kalemler":[
           {"id":"k54a","sira":1,"aciklama":"Burma Bilezik","miktar":{{miktarA.ToString(System.Globalization.CultureInfo.InvariantCulture)}},"birim":"ADET",
            "ayarId":"K22","milyem":916,"gramAgirlik":10,"hasGrami":9.16,"tasIbaresi":null,
            "birimFiyat":1000,"iscilikTl":120,"indirimTutari":null,"kdvTuru":"STANDART",
            "kdvOran":0.2,"hasDeger":0,"kdvMatrah":0,"kdvTutar":0,
            "satirToplam":{{satirA.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},
           {"id":"k54b","sira":2,"aciklama":"Gümüş Kolye","miktar":1,"birim":"ADET",
            "ayarId":null,"milyem":null,"gramAgirlik":null,"hasGrami":null,"tasIbaresi":null,
            "birimFiyat":500,"iscilikTl":null,"indirimTutari":null,"kdvTuru":"STANDART",
            "kdvOran":0.2,"hasDeger":0,"kdvMatrah":416.67,"kdvTutar":83.33,"satirToplam":500}]}
        """;

    private IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> P54KalemleriAc(string durum)
    {
        p53Fatura = P54Liste(durum, 1620m);
        p54Kalemler = P54Kalemler(1m, 1120m, 1620m);
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-goster-0");
        e.Find("#fk-goster-0").Click();
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll("tr.fd-kalem-satir").Count));
        return e;
    }

    private static void HucreyiKaydet(IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> e,
        string alan, string kalemId, string deger)
    {
        e.Find($"#si-fatura-kalem-{alan}-{kalemId}-ac").Click();
        e.Find($"#si-fatura-kalem-{alan}-{kalemId}").Input(deger);
        e.Find($"#si-fatura-kalem-{alan}-{kalemId}-kaydet").Click();
    }

    [Fact]
    public void TaslakKalemiEkrandanDuzenlenir()
    {
        var e = P54KalemleriAc("TASLAK");
        p54PatchYanit = P54Kalemler(3m, 3120m, 3620m);
        p54ListeSonra = P54Liste("TASLAK", 3620m);
        var listeGetOnce = istekler.Count(i => i.Metot == "GET" && i.Yol.StartsWith("/api/v1/faturalar?", StringComparison.Ordinal)
                                               || i.Metot == "GET" && i.Yol == "/api/v1/faturalar");

        // Beş düzenlenebilir hücrenin beşi de gerçek düzenleme kipine bağlı.
        foreach (var alan in new[] { "aciklama", "miktar", "birimFiyat", "iscilikTl", "indirimTutari" })
            Assert.NotNull(e.Find($"#si-fatura-kalem-{alan}-k54a-ac"));

        HucreyiKaydet(e, "miktar", "k54a", "3");

        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "PATCH" && i.Yol == "/api/v1/faturalar/f54/kalemler/k54a"));
        var govde = System.Text.Json.JsonDocument.Parse(
            istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
        Assert.Equal(3m, govde.GetProperty("miktar").GetDecimal());
        // Tek alan gider: öbürleri gövdede YOK (PATCH: gönderilmeyen alan korunur).
        Assert.Single(govde.EnumerateObject());

        // Başlık toplamı sunucunun yanıtıyla güncellenir; liste YENİDEN okunur.
        e.WaitForAssertion(() => Assert.Equal(Kuyumcu.Web.Servisler.Bicim.TlKurus(3620m),
            e.Find("#fd-toplam").TextContent.Trim()));
        Assert.True(istekler.Count(i => i.Metot == "GET" && (i.Yol.StartsWith("/api/v1/faturalar?", StringComparison.Ordinal)
                                                          || i.Yol == "/api/v1/faturalar")) > listeGetOnce);
        Assert.Contains("Fatura kalemi güncellendi", e.Markup);
        Assert.Empty(e.FindAll(".alan-hata"));
    }

    [Fact]
    public void Iscilik_bos_birakilinca_govdede_NULL_gider_alan_silinir()
    {
        var e = P54KalemleriAc("TASLAK");
        p54PatchYanit = P54Kalemler(1m, 1000m, 1500m);

        HucreyiKaydet(e, "iscilikTl", "k54a", "");

        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH"));
        var govde = System.Text.Json.JsonDocument.Parse(
            istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
        Assert.Equal(System.Text.Json.JsonValueKind.Null, govde.GetProperty("iscilikTl").ValueKind);
        Assert.Single(govde.EnumerateObject());
    }

    [Fact]
    public void Indirim_hucresi_PATCH_govdesinde_indirimTutari_tasir()
    {
        var e = P54KalemleriAc("TASLAK");
        p54PatchYanit = P54Kalemler(1m, 1070m, 1570m);

        HucreyiKaydet(e, "indirimTutari", "k54a", "50,5");

        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH"));
        var govde = System.Text.Json.JsonDocument.Parse(
            istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
        Assert.Equal(50.5m, govde.GetProperty("indirimTutari").GetDecimal());
    }

    // ── PAKET P-78 — fatura kalemi satır içi sayı tr-TR ──
    // Eski yol virgülü noktaya çevirip Invariant okuyordu: "45.000" = 45,
    // "1.250,50" = FormatException. Artık tek merkez (FormAlani.SayiCoz).
    [Fact]
    public void P78_fatura_kalem_fiyati_45_000_TURKCE_okunur_ve_gosterim_TR()
    {
        var e = P54KalemleriAc("TASLAK");
        p54PatchYanit = P54Kalemler(1m, 1000m, 1500m);
        // K01 (30.09): gösterim tablo ile aynı dil — binlikli, iki kuruş haneli (eskiden
        // "1000,00" binliksizdi; 1.095.125 TL kutuda "1095125,0" diye kesiliyordu).
        Assert.Equal(Kuyumcu.Web.Servisler.Bicim.Girdi(1000m, "#,##0.00"),
            e.Find("#si-fatura-kalem-birimFiyat-k54a-ac").TextContent.Trim());
        Assert.Equal("1.000,00", e.Find("#si-fatura-kalem-birimFiyat-k54a-ac").TextContent.Trim());

        HucreyiKaydet(e, "birimFiyat", "k54a", "45.000");
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH"));
        var govde = System.Text.Json.JsonDocument.Parse(
            istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
        Assert.Equal(45000m, govde.GetProperty("birimFiyat").GetDecimal());
    }

    [Fact]
    public void P78_fatura_kalem_belirsiz_miktar_PATCH_ATMAZ_ve_alan_altinda_soyler()
    {
        var e = P54KalemleriAc("TASLAK");
        HucreyiKaydet(e, "birimFiyat", "k54a", "1.500");
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
        e.WaitForAssertion(() => Assert.Contains(Kuyumcu.Core.I18n.SayiGirisiSozlugu.Tr.Belirsiz, e.Markup));
    }

    [Fact]
    public void KalemToplamiSunucuYanitindanOkunur()
    {
        var e = P54KalemleriAc("TASLAK");
        Assert.Equal(Kuyumcu.Web.Servisler.Bicim.TlKurus(1620m), e.Find("#fd-toplam").TextContent.Trim());

        // Sunucu işçiliği ve öbür satırı da katarak 3.620 döner; ekranın kendi
        // çarpımı (3×1000 + 500 = 3.500) bu değeri ÜRETEMEZ — okunmak zorundadır.
        p54PatchYanit = P54Kalemler(3m, 3120m, 3620m);
        HucreyiKaydet(e, "miktar", "k54a", "3");

        e.WaitForAssertion(() => Assert.Equal(Kuyumcu.Web.Servisler.Bicim.TlKurus(3620m),
            e.Find("#fd-toplam").TextContent.Trim()));
        // Satır toplamı da yanıttan: 3.120 (3×1000 + 120 işçilik).
        Assert.Contains(Kuyumcu.Web.Servisler.Bicim.TlKurus(3120m), e.Find("tr.fd-kalem-satir").TextContent);
        Assert.DoesNotContain(Kuyumcu.Web.Servisler.Bicim.TlKurus(3500m), e.Find("#fd-toplam").TextContent);
    }

    [Fact]
    public void KesilmisFaturadaKalemKilitliVeSebepYazili()
    {
        var e = P54KalemleriAc("KESILDI");

        // Düzenleme kipi YOK: ne tetikleyici, ne işaretli hücre.
        Assert.Empty(e.FindAll("#fd-kalem-tablosu [id^='si-fatura-kalem']"));
        Assert.Empty(e.FindAll("#fd-kalem-tablosu [data-duzenlenebilir]"));
        // Değerler yine GÖRÜNÜR (salt okunur).
        Assert.Contains("Burma Bilezik", e.Find("#fd-kalem-tablosu").TextContent);
        // Sebep ve düzeltme yolu yazılı; silme ipucu (taslağa özgü) YOK.
        var ipucu = e.Find("#degismez-ipucu-FaturaKalemi").TextContent;
        Assert.Contains("belgenin parçasıdır", ipucu);
        Assert.Contains("iptal edin", ipucu);
        Assert.Empty(e.FindAll("#degismez-ipucu-FaturaKalemSilme"));
    }

    [Fact]
    public void GecersizMiktarAlanDuzeyindeHataVerir()
    {
        var e = P54KalemleriAc("TASLAK");

        // (a) İstemci kuralı: miktar 0 → HÜCRENİN ALTINDA mesaj, istek GİTMEZ.
        HucreyiKaydet(e, "miktar", "k54a", "0");
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#si-fatura-kalem-miktar-k54a-hata").TextContent));
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
        e.Find("#si-fatura-kalem-miktar-k54a-iptal").Click();

        // (b) Sunucu 400'ü: mesaj DÜZELTİLEN hücrenin altında; bant yalnız özet.
        p53PatchHata = "Miktar sıfırdan büyük olmalıdır";
        HucreyiKaydet(e, "miktar", "k54a", "2");

        e.WaitForAssertion(() => Assert.Equal("Miktar sıfırdan büyük olmalıdır",
            e.Find("#alan-hata-fd-kalem-miktar-k54a").TextContent.Trim()));
        Assert.Single(e.FindAll(".alan-hata"));
        Assert.Contains("işaretli alanın altındaki", e.Markup);
        // Toplam DEĞİŞMEDİ (başarısız yazma ekranda sonuç üretmez).
        Assert.Equal(Kuyumcu.Web.Servisler.Bicim.TlKurus(1620m), e.Find("#fd-toplam").TextContent.Trim());
    }

    [Fact]
    public void Taslak_kalemi_icin_SILME_dugmesi_UYDURULMAZ_sebep_ve_yol_yazili()
    {
        var e = P54KalemleriAc("TASLAK");

        var ipucu = e.Find("#degismez-ipucu-FaturaKalemSilme").TextContent;
        Assert.Contains("silinemez", ipucu);
        Assert.Contains("İptal Et", ipucu);
        Assert.Contains("numarası harcamaz", ipucu);
        Assert.Empty(e.FindAll("#degismez-ipucu-FaturaKalemSilme button"));
        // Kesilmiş belgeye özgü kilit ipucu taslakta çizilmez.
        Assert.Empty(e.FindAll("#degismez-ipucu-FaturaKalemi"));
    }

    // ── PAKET Q10-5 (QA turu 10 /faturalar: basit modda üst yarı KDV 23/e,
    //    K-10 ve yapılandırma anahtarıyla dolu) ─────────────────────────────

    [Fact]
    public void Q105_Basit_modda_ozel_matrah_beyani_katlanir_ozet_ve_ic_kod_yok()
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => true);
        var e = EkranAc();
        // YASAL-4 / Y4-C: sayfa sonundaki katlanır KALKTI (bilgi faturanın kendisinde).
        Assert.Empty(e.FindAll("#fk-ozel-matrah-beyani-ac"));
        Assert.Empty(e.FindAll("#fk-ozel-matrah-haric"));
        Assert.Empty(e.FindAll("#fk-ozel-matrah-kaynak"));
        Assert.DoesNotContain("OzelMatrahKodu", e.Markup);
        Assert.DoesNotContain("K-10", e.Markup);
    }

    [Fact]
    public void Q105_Profesyonel_modda_ozel_matrah_beyani_acik_ve_ayni()
    {
        var e = EkranAc();
        Assert.Empty(e.FindAll("#fk-ozel-matrah-ayrinti"));
        // YASAL-4 / Y4-C: PRO'da da katlanır YOK ve teknik kaynak beyanı basılmaz.
        Assert.Empty(e.FindAll("#fk-ozel-matrah-beyani-ac"));
        Assert.DoesNotContain("K-10", e.Markup);
    }

    [Fact]
    public void Q105_Fatura_listesi_basliklari_tek_satir_sinifi_tasir()
    {
        var e = EkranAc();
        Assert.Contains("q10-th-tek", e.Find("table.tablo-sik").ClassName);
    }

    // ── Doğrulayıcı 29.09 (ORTA): fişten kesilen faturanın türü FİŞTEN türetilir ve görünür ──
    private (string Tur, string Rozet) FistenKesVeGovde()
    {
        var e = FistenModalAc();
        FisSec(e);
        e.Find("#ff-onizle").Click();
        e.WaitForAssertion(() => Assert.False(e.Find("#ff-kaydet").HasAttribute("disabled")));
        var rozet = e.Find("#ff-tur").TextContent.Trim();
        istekler.Clear();
        e.Find("#ff-kaydet").Click();
        string tur = "";
        e.WaitForAssertion(() =>
        {
            var post = istekler.First(i => i.Metot == "POST" && i.Yol.EndsWith("/faturalar/fisten", StringComparison.Ordinal));
            tur = System.Text.Json.JsonDocument.Parse(post.Govde).RootElement.GetProperty("tur").GetString()!;
        });
        return (tur, rozet);
    }

    [Fact]
    public void ZB66_Alis_fisinden_kesilen_fatura_ALIS_turuyle_gider_ve_ekranda_Alis_yazar()
    {
        fisAlis = true;
        var T = Kuyumcu.Core.I18n.Sozlukler.Getir("tr");
        var (tur, rozet) = FistenKesVeGovde();
        Assert.Equal("ALIS", tur);
        Assert.Equal(T.Fatura.Alis, rozet);
    }

    [Fact]
    public void ZB66b_Satis_fisi_ve_karisik_fis_SATIS_turuyle_gider()
    {
        var T = Kuyumcu.Core.I18n.Sozlukler.Getir("tr");
        var (tur, rozet) = FistenKesVeGovde();
        Assert.Equal("SATIS", tur);
        Assert.Equal(T.Fatura.Satis, rozet);
        // Karışık fiş (alış + satış kalemi): eski varsayılan (SATIS) korunur.
        Assert.Equal("SATIS", Kuyumcu.Web.Pages.Faturalar.FistenTur(new Kuyumcu.Contracts.Sayfalar.Faturalar.FisYaniti(
            "F", null, null, null, 2, 0m, 0m,
            [new("a", "ALIS", -1m, 1m, false, null, null), new("b", "SATIS", 1m, -1m, false, null, null)])));
        Assert.Null(Kuyumcu.Web.Pages.Faturalar.FistenTur(null));
    }

    // ── ZO-BELGE 29.09 (orkestratör 18:05 + hasım doğrulayıcı KRİTİK): Fişten Fatura Kes
    //    penceresinin TAMAMI sözlükten geçer — ham kod, ham damga, defter işareti yok. ──

    /// <summary>Eski altınla satışın ürünsüz HURDA_ALIS kalemi + carisiz fiş + taslak faturaya bağlı fiş.</summary>
    private const string FisHurdaCarisizTaslak = """
        {"fisId":"FIS1","cariId":null,"cariAd":null,"faturaNo":"TASLAK:01M3PQ8AABNVX4Y5HCEDBR3HYK",
         "kalemSayisi":2,"toplamTl":-2100.50,"toplamHas":3.5,"kalemler":[
           {"id":"h1","tur":"ALIS","tlTutari":-1500.50,"hasGrami":2.5,
            "hediye":false,"urunAd":"Burma Bilezik","faturaNo":"TASLAK:01M3PQ8AABNVX4Y5HCEDBR3HYK"},
           {"id":"h2","tur":"HURDA_ALIS","tlTutari":-600.00,"hasGrami":1.0,
            "hediye":false,"urunAd":null,"faturaNo":"TASLAK:01M3PQ8AABNVX4Y5HCEDBR3HYK"},
           {"id":"h3","tur":"BILINMEYEN_YENI_TUR","tlTutari":0,"hasGrami":0,
            "hediye":false,"urunAd":null,"faturaNo":null}]}
        """;

    private static readonly System.Text.RegularExpressions.Regex BuyukHarfKod =
        new(@"(?<![\p{L}_-])[A-Z_]{3,}(?![\p{L}_\d-])");

    /// <summary>Pencerede büyük harfli kod kalıbındaki sözcükler (seri no öneki "FTR-2026-…" veri olduğu
    /// için tireyle bitişik kısım sayılmaz). "KDV" yasal kısaltmadır
    /// (vergi adı, veritabanı kodu değil) — tek istisna.</summary>
    private static List<string> PenceredekiKodlar(IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> e)
    {
        var pencere = e.Find("#ff-onizle").ParentElement!;
        var tumu = BuyukHarfKod.Matches(pencere.TextContent).Select(m => m.Value).ToList();
        // Yaprak düzeyi: kendi metni ^[A-Z_]{3,}$ olan eleman (orkestratörün kalıbı birebir).
        tumu.AddRange(pencere.QuerySelectorAll("*")
            .Select(x => x.TextContent.Trim())
            .Where(s => System.Text.RegularExpressions.Regex.IsMatch(s, "^[A-Z_]{3,}$")));
        return tumu.Where(k => k != "KDV").ToList();
    }

    [Fact]
    public void ZB70_Fisten_onizleme_urunsuz_HURDA_ALIS_kaleminde_sozlukteki_adi_yazar_ham_kod_yok()
    {
        fisAlis = true;   // ZB66'nın alış fişi: ikinci kalem ürünsüz HURDA_ALIS
        var e = FistenModalAc();
        FisSec(e);
        e.Find("#ff-onizle").Click();
        e.WaitForElement("#ff-kalem-tablosu");
        var tablo = e.Find("#ff-kalem-tablosu").TextContent;
        Assert.DoesNotContain("HURDA_ALIS", tablo);
        var ikinci = e.FindAll("tr.ff-kalem-satir")[1].QuerySelectorAll("td")[1].TextContent.Trim();
        Assert.Contains(ikinci, new[]
        {
            Kuyumcu.Core.I18n.KatalogSozlugu.HareketTuru("HURDA_ALIS", "tr", false),
            Kuyumcu.Core.I18n.KatalogSozlugu.HareketTuru("HURDA_ALIS", "tr", true),
        });
        Assert.Empty(PenceredekiKodlar(e));
    }

    [Fact]
    public void ZB70b_Fisten_penceresi_tarama_buyuk_harf_kod_kalibi_0_taslak_damga_ve_carisiz_fis()
    {
        fisYanitOzel = FisHurdaCarisizTaslak;
        var Z = Kuyumcu.Core.I18n.ZoBelgeSozlugu.Al("tr");
        var e = FistenModalAc();
        Assert.Empty(PenceredekiKodlar(e));            // önizleme öncesi
        FisSec(e);
        Assert.Empty(PenceredekiKodlar(e));            // seçim sonrası (seçici rozeti + alt bilgi)
        e.Find("#ff-onizle").Click();
        e.WaitForElement("#ff-kalem-tablosu");
        var pencere = e.Find("#ff-onizle").ParentElement!.TextContent;
        Assert.Empty(PenceredekiKodlar(e));
        Assert.DoesNotContain("TASLAK:", pencere);
        Assert.DoesNotContain("01M3PQ8AABNVX4Y5HCEDBR3HYK", pencere);
        Assert.Contains(Z.FistenTaslakFaturali, pencere);
        Assert.Equal(Z.FistenCariYok, e.Find("#ff-cari").TextContent.Trim());
        // Sözlükte olmayan kod da ham basılmaz (genel ad).
        Assert.Equal(Z.FistenKalemGenel,
            e.FindAll("tr.ff-kalem-satir")[2].QuerySelectorAll("td")[1].TextContent.Trim());
        Assert.True(e.Find("#ff-kaydet").HasAttribute("disabled"));
    }

    [Fact]
    public void ZB70c_Fisten_onizleme_fatura_yonunden_isaretsiz_toplam_ve_kalem()
    {
        fisAlis = true;
        var e = FistenModalAc();
        FisSec(e);
        e.Find("#ff-onizle").Click();
        e.WaitForElement("#ff-kalem-tablosu");
        Assert.Equal(Bicim.Tl(2100.50m), e.Find("#ff-toplam").TextContent.Trim());
        var hucreler = e.FindAll("#ff-kalem-tablosu tbody td").Select(h => h.TextContent.Trim()).ToList();
        Assert.Contains(Bicim.Tl(1500.50m), hucreler);
        Assert.Contains(Bicim.Has(2.5m), hucreler);
        Assert.DoesNotContain(hucreler, h => h.StartsWith('-') || h.StartsWith('−'));
    }

    [Fact]
    public void ZB70d_Faturali_fiste_bant_cumle_ve_numara_tasir()
    {
        fisFaturali = true;
        var e = FistenModalAc();
        FisSec(e);
        e.Find("#ff-onizle").Click();
        e.WaitForAssertion(() => Assert.Contains(
            string.Format(Kuyumcu.Core.I18n.ZoBelgeSozlugu.Al("tr").FistenZatenFaturali, "FTR-2026-000001"),
            e.Markup));
        Assert.Empty(PenceredekiKodlar(e));
    }

    // ── DEVAM 11 (375 ölçümü): önizleme tablosu dar kapta KART olur, tutar ve has etiketiyle görünür ──
    [Fact]
    public void ZB70e_Fisten_onizleme_dar_kapta_kart_tutar_ve_has_etiketli()
    {
        fisAlis = true;
        var e = FistenModalAc();
        FisSec(e);
        e.Find("#ff-onizle").Click();
        e.WaitForElement("#ff-kalem-tablosu");
        var satir = e.Find("#ff-kalem-tablosu tbody tr.ff-kalem-satir");
        var td = satir.QuerySelectorAll("td").ToList();
        Assert.Equal(4, td.Count);
        Assert.Contains("ff-sira", td[0].ClassName);
        Assert.Contains("ff-ad", td[1].ClassName);
        Assert.Equal(Kuyumcu.Core.I18n.Sozlukler.Tr.Ortak.Toplam, td[2].GetAttribute("data-etiket"));
        Assert.Equal(Kuyumcu.Core.I18n.Sozlukler.Tr.Urun.HasGram, td[3].GetAttribute("data-etiket"));
        var css = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "wwwroot", "css", "zo-belge.css"));
        Assert.Contains(".tablo-sar:has(> #ff-kalem-tablosu) { container: ffkalem / inline-size; }", css);
        var kart = System.Text.RegularExpressions.Regex.Match(css, @"@container ffkalem \(max-width: (\d+)px\) \{(.*?)\n\}",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.True(kart.Success, "dar kap kart kuralı yok");
        // 375 telefonda pencere içi kap ~319 px; eşik bunu kapsamalı.
        Assert.True(int.Parse(kart.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) >= 360);
        Assert.Contains("#ff-kalem-tablosu > thead { display: none; }", kart.Groups[2].Value);
        Assert.Contains("td.ff-sira { display: none; }", kart.Groups[2].Value);
        Assert.Contains("content: attr(data-etiket)", kart.Groups[2].Value);
    }

    // ── DEVAM 12 (tasarım hakemi YÜKSEK ×2): üç oluşturma formu TEK katman + TEK altlık düzeni ──
    // Yeni Fatura: Kaydet gövdenin ortasında değil, çekmecenin altlığında "İptal | Kaydet"
    // (Evrak gir ile aynı: cekmece-alt, dugme sınıfları, ikincil önce birincil sonda).
    [Fact]
    public void ZB71_Yeni_fatura_altlik_Iptal_Kaydet_yan_yana_govdenin_dibinde()
    {
        var e = YeniModalAc();
        var cek = e.Find("#cekmece-fatura-yeni");
        var eylem = e.Find("#fk-eylem");
        Assert.Contains("cekmece-alt", eylem.ClassList);
        // Altlık formun (zo-yeni-fatura) İÇİNDE değil, gövdenin son çocuğu.
        Assert.Equal("cekmece-fatura-yeni-govde", eylem.ParentElement!.Id);
        Assert.Null(eylem.Closest(".zo-yeni-fatura"));
        Assert.Null(e.Find(".zo-yeni-fatura").QuerySelector("#fk-kaydet"));
        var dugmeler = eylem.QuerySelectorAll("button").ToList();
        Assert.Equal(new[] { "fk-vazgec", "fk-kaydet" }, dugmeler.Select(d => d.Id).ToArray());
        Assert.Contains("dugme-ikincil", dugmeler[0].ClassList);
        Assert.Contains("dugme-birincil", dugmeler[1].ClassList);
        Assert.NotNull(cek.QuerySelector("#fk-kaydet"));
        // İptal kapatır (istek atmaz).
        e.Find("#fk-vazgec").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#cekmece-fatura-yeni")));
        var css = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "wwwroot", "css", "zo-belge.css"));
        Assert.Contains("#cekmece-fatura-yeni > .cekmece-alt,\n#cekmece-fatura-fisten > .cekmece-alt { display: none; }", css);
        Assert.Contains("#cekmece-fatura-fisten-govde { display: flex; flex-direction: column; padding-bottom: 0; }", css);
    }

    // Fişten Fatura Kes: ModalPencere DEĞİL Çekmece (DoD EK 2); id'ler AYNI, altlık aynı düzen.
    [Fact]
    public void ZB72_Fisten_fatura_kes_CEKMECE_modal_yok_altlik_ayni()
    {
        var e = FistenModalAc();
        var cek = e.Find("#cekmece-fatura-fisten");
        Assert.Equal("dialog", cek.GetAttribute("role"));
        Assert.Empty(e.FindAll(".modal-panel"));
        foreach (var id in new[] { "#ff-fis", "#ff-kdvturu", "#ff-onizle", "#ff-kaydet" })
            Assert.NotNull(cek.QuerySelector(id));
        var eylem = e.Find("#ff-eylem");
        Assert.Equal("cekmece-fatura-fisten-govde", eylem.ParentElement!.Id);
        Assert.Equal(new[] { "ff-vazgec", "ff-kaydet" },
            eylem.QuerySelectorAll("button").Select(d => d.Id).ToArray());
        Assert.Contains("cekmece-alt", eylem.ClassList);
        // Arama boş sonucu sayfa bağlamında konuşur (ortak "ekran yok" metni değil).
        var ks = e.FindComponent<Kuyumcu.Web.Bilesenler.KayitSecici>();
        Assert.Equal(Kuyumcu.Core.I18n.ZoBelgeSozlugu.Al("tr").FistenAramaYok, ks.Instance.BosMesaj);
        e.Find("#ff-vazgec").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#cekmece-fatura-fisten")));
    }

    // UX-S30: Fişten Kaydet kilitliyken SEBEBİ altlıkta yazar; önizleme sonrası kilit ve sebep kalkar.
    [Fact]
    public void ZB73_Fisten_kilitli_Kaydet_sebebi_altlikta()
    {
        var zb = Kuyumcu.Core.I18n.ZoBelgeSozlugu.Al("tr");
        var e = FistenModalAc();
        Assert.True(e.Find("#ff-kaydet").HasAttribute("disabled"));
        Assert.Equal(zb.FistenKilitOnizle, e.Find("#ff-eylem #ff-kaydet-kilit").TextContent.Trim());
        FisSec(e);
        e.Find("#ff-onizle").Click();
        e.WaitForAssertion(() => Assert.False(e.Find("#ff-kaydet").HasAttribute("disabled")));
        Assert.Empty(e.FindAll("#ff-kaydet-kilit"));
    }

    [Fact]
    public void ZB73b_Faturali_fiste_kilit_sebebi_faturali()
    {
        fisFaturali = true;
        var zb = Kuyumcu.Core.I18n.ZoBelgeSozlugu.Al("tr");
        var e = FistenModalAc();
        FisSec(e);
        e.Find("#ff-onizle").Click();
        e.WaitForAssertion(() => Assert.Equal(zb.FistenKilitFaturali, e.Find("#ff-kaydet-kilit").TextContent.Trim()));
        Assert.True(e.Find("#ff-kaydet").HasAttribute("disabled"));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET KESIF-P3 — fatura kalemi ham ayar kodu (#23/#59), önizleme kilit metni (#57),
    // defter bağlı kalemde adet salt okunur (TOPLU 30.09 08:50).
    // ═════════════════════════════════════════════════════════════════════════

    private static readonly System.Text.RegularExpressions.Regex HamAyarKodu =
        new(@"\b[KG]\d{2,3}\b", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    [Fact]
    public void Faturalar_kalem_ayar_adi()
    {
        var e = P54KalemleriAc("TASLAK");
        var hucre = e.Find("#fd-kalem-ayar-k54a");
        Assert.Equal("22 Ayar", hucre.TextContent.Trim());
        Assert.Equal("K22", hucre.GetAttribute("title"));          // ham kod yalnız ipucunda
        Assert.Equal("—", e.Find("#fd-kalem-ayar-k54b").TextContent.Trim());
        // Kalem tablosunun GÖRÜNÜR metninde ham ayar kodu 0.
        Assert.DoesNotMatch(HamAyarKodu, e.Find("#fd-kalem-tablosu").TextContent);
    }

    /// <summary>KESIF-P3 tasarım hakemi tur 2 (#23): PRO kalem tablosu (11 sütun) kendi, daha geniş
    /// kart eşiğini taşır (zo-kalem-genis → zo-belge.css @container zokalem 1140px); BASİT taşımaz.</summary>
    [Fact]
    public void Faturalar_kalem_PRO_genis_tablo_sinifi_BASIT_yok()
    {
        var dil = Services.GetRequiredService<DilServisi>();
        var basit = false;
        dil.BasitModuBagla(() => basit);
        var e = P54KalemleriAc("TASLAK");
        Assert.Equal(12, e.FindAll("#fd-kalem-tablosu thead th").Count);   // M5 (18.10): + Gram
        Assert.Contains("zo-kalem-genis", e.Find("#fd-kalem-tablosu").ClassList);

        basit = true;
        var b = Render<Kuyumcu.Web.Pages.Faturalar>();
        b.WaitForElement("#fk-goster-0");
        b.Find("#fk-goster-0").Click();
        b.WaitForAssertion(() => Assert.Equal(2, b.FindAll("tr.fd-kalem-satir").Count));
        Assert.DoesNotContain("zo-kalem-genis", b.Find("#fd-kalem-tablosu").ClassList);
    }

    /// <summary>KESIF-P3 tasarım hakemi tur 2 (#57): önizleme kutusu iç boşluklu kalıpta
    /// (zo-onizleme-kutu) ve kilit cümlesi küçük puntolu değil (en az 14 px — CSS testi).</summary>
    [Fact]
    public void Faturalar_onizleme_kutusu_ic_bosluklu_ve_kilit_buyuk_punto()
    {
        var e = YeniModalAc();
        SeciciSec(e, "fatura-cari", "c1");
        e.Find("#fatura-toplam").Input("75000");
        RadyoSec(e, "fatura-kdvturu", "KUYUM");
        e.WaitForElement("#fom-kilit");
        var kutu = e.Find("#fom-kutu");
        Assert.Contains("zo-onizleme-kutu", kutu.ClassList);
        Assert.Equal("fom-kutu", e.Find("#fom-kilit").ParentElement?.Id);
        Assert.DoesNotContain("metin-kucuk", e.Find("#fom-kilit").ClassList);
        Assert.Empty(kutu.QuerySelectorAll("[style]"));
    }

    [Fact]
    public void Faturalar_onizleme_kilit_metni_tek()
    {
        var e = YeniModalAc();
        SeciciSec(e, "fatura-cari", "c1");
        e.Find("#fatura-toplam").Input("75000");
        RadyoSec(e, "fatura-kdvturu", "KUYUM");
        e.WaitForAssertion(() => Assert.True(Kaydet(e).HasAttribute("disabled")));

        var T = Kuyumcu.Core.I18n.Sozlukler.Tr.Fatura;
        // Kilit sebebi düğmeden AYRI satırda: blok <p>, düğmenin kardeşi olarak ÖNCE gelir.
        var kilit = e.Find("#fom-kilit");
        Assert.Equal("P", kilit.TagName);
        Assert.Equal(T.IslemSecilmedi, kilit.TextContent.Trim());
        Assert.Equal("fom-yenile", kilit.NextElementSibling?.Id);
        // Aynı açıklama cümlesi ekranda BİR kez (alt çubuk kısa metin gösterir).
        var cumle = "defterden türetilir";
        var adet = System.Text.RegularExpressions.Regex.Matches(e.Markup, cumle).Count;
        Assert.Equal(1, adet);
        Assert.Equal(Kuyumcu.Core.I18n.KesifP3Sozlugu.Tr.KuyumKaydetKilitKisa, e.Find("#fk-kaydet-kilit").TextContent.Trim());
    }

    [Fact]
    public void Faturalar_defter_bagli_kalem_adedi_salt_okunur_fisten_degistir_baglantisi()
    {
        p53Fatura = P54Liste("TASLAK", 1620m);
        p54Kalemler = P54Kalemler(1m, 1120m, 1620m)
            .Replace("\"id\":\"k54a\",", "\"id\":\"k54a\",\"hareketId\":\"h1\",");
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-goster-0");
        e.Find("#fk-goster-0").Click();
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll("tr.fd-kalem-satir").Count));

        // Defter bağlı kalem: adet düzenleyicisi YOK, salt okunur değer + fişe bağlantı.
        Assert.Empty(e.FindAll("#si-fatura-kalem-miktar-k54a-ac"));
        Assert.Equal(Kuyumcu.Web.Servisler.Bicim.Miktar(1m, "ADET"), e.Find("#fd-kalem-adet-kilit-k54a").TextContent.Trim());
        var bag = e.Find("#fd-kalem-adet-fis-k54a");
        Assert.Equal("/islemler", bag.GetAttribute("href"));
        Assert.Equal(Kuyumcu.Core.I18n.KesifP3Sozlugu.Tr.AdetFistenDegistir, bag.TextContent.Trim());
        // Fiyat alanları serbest (sunucu kuralıyla aynı); bağsız kalemin adedi düzenlenebilir.
        Assert.NotNull(e.Find("#si-fatura-kalem-birimFiyat-k54a-ac"));
        Assert.NotNull(e.Find("#si-fatura-kalem-miktar-k54b-ac"));
    }
}
