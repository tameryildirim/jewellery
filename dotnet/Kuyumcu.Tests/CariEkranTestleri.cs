using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// CARİ EKRANI — HER İŞLEV TEK TEK
//
// TESPİT (2026-09-14, kurucu sordu): cari kartında DÜZENLEME YOKTU.
// Ölçüm bunun tek bir eksik olmadığını gösterdi: cari modülünün 9 ucundan
// yalnız 3'ü ekranda kullanılıyordu. Ekranı olmayanlar:
//   PATCH /cari/{id}            — düzenleme
//   DELETE /cari/{id}           — pasife alma
//   GET /cari/{id}              — tam kart (liste yalnız özet taşıyor)
//   GET /cari/{id}/bakiye       — bakiye
//   POST /cari/{id}/tahsilat    — ⚠ kuyumcunun GÜNLÜK işi
//   POST /cari/{id}/veresiye    — ⚠ kuyumcunun GÜNLÜK işi
//
// Bu dosya her birinin ekrandan gerçekten çağrıldığını sınar. "Düğme var"
// yetmez — hangi uca, hangi gövdeyle gittiği kontrol edilir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class CariEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string CariListesi = """
        [{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567",
          "email":"ayse@ornek.com","tlBakiye":-1500.00,"hasGramBakiye":2.5,
          "usdBakiye":1250.00,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false},
         {"id":"c2","ad":"Kapalıçarşı Toptan","tur":"TEDARIKCI","telefon":null,
          "email":null,"tlBakiye":0,"hasGramBakiye":0,
          "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":"YUKSEK","pepBeyani":true}]
        """;

    public CariEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        // PAKET S3-2: Cariler artık arayüz modunu okur (basit modda satır/form katlama).
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            var g = yol switch
            {
                // ⚠ Paket 27: döviz alanları GERÇEKÇİ dönmeli — eksikse döviz düğmeleri
                //   "kur yok" diye kilitli kalır ve testler sessizce boş geçerdi.
                var y when y.EndsWith("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"gbpTry":null}""",
                var y when y.EndsWith("/doviz-veresiye") || y.EndsWith("/doviz-tahsilat") => """
                    {"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI",
                     "bakiye":{"cariId":"c1","tlBakiye":-1500,"hasGramBakiye":2.5,"usdBakiye":1000,"eurBakiye":0,"gbpBakiye":0}}
                    """,
                var y when y.Contains("/ekstre") => """{"bakiye":{"tlBakiye":-1500,"hasGramBakiye":2.5},"hareketler":[]}""",
                // Sunucu düzeltmeden sonra cari kartını döndürür (servis: return Getir(...)).
                var y when y.EndsWith("/duzeltme") => """
                    {"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI",
                     "bakiye":{"cariId":"c1","tlBakiye":-250,"hasGramBakiye":2.0}}
                    """,
                var y when y.EndsWith("/cari/c1") => """
                    {"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567",
                     "email":"ayse@ornek.com","vergiNo":"1234567890","adres":"Şişli, İstanbul",
                     "pepBeyani":false,"pepGorev":null,"pepBeyanTarihi":null}
                    """,
                var y when y.EndsWith("/cari") => CariListesi,
                // paket UX-4b madde 9: tahsilat modalı kasa seçicisi bu uçtan beslenir.
                // '{}' varsayılanı bırakılamaz — gerçekçi yanıt verilir (tek TL kasası).
                // Paket 27: USD kasası da var — döviz tahsilat kasa seçicisi sınanabilsin.
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},{"id":"ks-usd","ad":"USD Kasa","paraBirimi":"USD","aktif":true},{"id":"ks-eur1","ad":"EUR Kasa 1","paraBirimi":"EUR","aktif":true},{"id":"ks-eur2","ad":"EUR Kasa 2","paraBirimi":"EUR","aktif":true}]""",
                var v when v.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","aktif":true},{"id":"fl2","ad":"Toptan","tur":"TOPTAN","aktif":true}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    /// <summary>
    /// ⚠ ROL ZORUNLU: düzeltme fişi düğmesi rolle gizlenir (ekip oyu Q3).
    /// Oturum kurulmazsa Kullanici null kalır, düğme hiç çizilmez ve düzeltme
    /// testleri "sessizce" boş geçerdi. Varsayılan SAHIP — ekranın tam hâli.
    /// </summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Cariler> Ciz(string rol = "SAHIP")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1"));
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        istekler.Clear();   // ilk yükleme çağrıları sayılmasın
        return e;
    }

    /// <summary>
    /// Gövdedeki bir alanın değeri. Ham metinde aramak yanıltıcıdır:
    /// System.Text.Json Türkçe harfleri `\u015F` gibi kaçırır, `Assert.Contains`
    /// tutmaz. Karşılaştırma anlam düzeyinde yapılır.
    /// </summary>
    private static string? Alan(string govde, string ad)
    {
        var e = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    private static AngleSharp.Dom.IElement Dugme(IRenderedComponent<Kuyumcu.Web.Pages.Cariler> e, string metin)
        => e.FindAll("button").First(b => b.TextContent.Contains(metin));

    // ── Düğmeler var mı ─────────────────────────────────────────────────────

    [Fact]
    public void Her_cari_satirinda_DUZENLE_dugmesi_vardir()
    {
        // Kurucunun sorduğu şey birebir bu.
        var e = Ciz();
        Assert.Equal(2, e.FindAll("button").Count(b => b.TextContent.Contains("Düzenle")));
    }

    [Fact]
    public void Her_cari_satirinda_TAHSILAT_ve_VERESIYE_dugmesi_vardir()
    {
        var e = Ciz();
        // ⚠ "Döviz Tahsilat" / "Döviz Veresiye" de bu metinleri İÇERİR (paket 27);
        //   sayım TL düğmelerine daraltılır, yoksa test yanlış yerden yeşil kalır.
        Assert.Equal(2, e.FindAll("button").Count(b => b.TextContent.Contains("Tahsilat") && !b.TextContent.Contains("Döviz")));
        Assert.Equal(2, e.FindAll("button").Count(b => b.TextContent.Contains("Veresiye") && !b.TextContent.Contains("Döviz")));
    }

    // ── Düzenleme ───────────────────────────────────────────────────────────

    [Fact]
    public void Duzenle_acilinca_TAM_KART_cekilir()
    {
        // ⚠ Liste yalnız özet taşır (vergi no ve adres YOK). Tam kart
        // çekilmezse, kaydet dendiğinde görünmeyen alanlar SİLİNİRDİ.
        var e = Ciz();
        Dugme(e, "Düzenle").Click();

        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/cari/c1"));
    }

    [Fact]
    public void Duzenle_formu_mevcut_degerlerle_DOLAR()
    {
        var e = Ciz();
        Dugme(e, "Düzenle").Click();

        Assert.Equal("Ayşe Demir", e.Find("#duz-ad").GetAttribute("value"));
        Assert.Equal("1234567890", e.Find("#duz-vergino").GetAttribute("value"));
        // PAKET GOC-B (envanter C satır 38): adres 500 karakterlik → çok satırlı kutu (textarea); değer içeriğinde.
        Assert.Equal("Şişli, İstanbul", e.Find("#duz-adres").TextContent.Trim());
    }

    [Fact]
    public void Kaydet_PATCH_ile_gonderir_ve_TUM_alanlari_tasir()
    {
        var e = Ciz();
        Dugme(e, "Düzenle").Click();
        e.Find("#duz-ad").Input("Ayşe Demir Kuyumculuk");
        Dugme(e, "Kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "PATCH");
        Assert.EndsWith("/cari/c1", istek.Yol);
        Assert.Equal("Ayşe Demir Kuyumculuk", Alan(istek.Govde, "ad"));
        Assert.Equal("1234567890", Alan(istek.Govde, "vergiNo"));      // vergi no KAYBOLMADI
        Assert.Equal("Şişli, İstanbul", Alan(istek.Govde, "adres"));   // adres KAYBOLMADI
    }

    /// <summary>Toptancıya "Toptan" listesi atanır; PATCH gövdesi fiyatListesiId taşır (paket 17).</summary>
    [Fact]
    public void Fiyat_listesi_secimi_PATCH_govdesinde_gider()
    {
        var e = Ciz();
        Dugme(e, "Düzenle").Click();
        e.WaitForAssertion(() => Assert.Contains(
            SeciciYardim.Secenekler(e, "duz-fiyat-listesi"), o => o.Deger() == "fl2"));
        SeciciYardim.Sec(e, "duz-fiyat-listesi", "fl2");
        Dugme(e, "Kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "PATCH");
            Assert.Equal("fl2", Alan(istek.Govde, "fiyatListesiId"));
        });
    }

    [Fact]
    public void Bos_ad_ile_kaydet_KAPALIDIR()
    {
        var e = Ciz();
        Dugme(e, "Düzenle").Click();
        e.Find("#duz-ad").Input("");
        Assert.True(Dugme(e, "Kaydet").HasAttribute("disabled"));
    }

    [Fact]
    public void Pasife_al_DELETE_gonderir()
    {
        // Sunucu SİLMEZ, pasife alır — hareket geçmişi korunur.
        // ⚠ UX-4d madde 13: yıkıcı eylem artık ONAY ister (DoD EK md.4).
        // İlk tıklama modali açar, isteği ikinci (onay) düğmesi atar.
        var e = Ciz();
        Dugme(e, "Düzenle").Click();
        e.Find("#c-pasife-al").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "DELETE");   // onaysız istek YOK
        e.Find("#c-pasif-onayla").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "DELETE");
        Assert.EndsWith("/cari/c1", istek.Yol);
    }

    // ── Tahsilat ────────────────────────────────────────────────────────────

    [Fact]
    public void Tahsilat_POST_ile_dogru_uca_gider()
    {
        var e = Ciz();
        Dugme(e, "Tahsilat").Click();
        e.Find("#th-tutar").Input("1500");
        Dugme(e, "Kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("tahsilat"));
        Assert.EndsWith("/cari/c1/tahsilat", istek.Yol);
        Assert.Contains("1500", istek.Govde);
    }

    [Fact]
    public void Tahsilatta_ODEME_TURU_gonderilir()
    {
        // 459 s. VUK Genel Tebliği tevsik kapısı bu alana bakıyor; ekran
        // göndermezse sunucu NAKİT varsayar ve havale tahsilatı hadde takılır.
        var e = Ciz();
        Dugme(e, "Tahsilat").Click();
        e.Find("#th-tutar").Input("50000");
        SeciciYardim.Sec(e, "th-odeme", "HAVALE");
        Dugme(e, "Kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("tahsilat"));
        Assert.Contains("HAVALE", istek.Govde);
    }

    [Fact]
    public void Tahsilat_KUR_fotografina_baglanir()
    {
        // Sunucu kurSnapshotId'yi ZORUNLU tutar: her hareket o anki kura bağlıdır.
        var e = Ciz();
        Dugme(e, "Tahsilat").Click();
        e.Find("#th-tutar").Input("1000");
        Dugme(e, "Kaydet").Click();

        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/kur/guncel"));
        var istek = Assert.Single(istekler, i => i.Yol.Contains("tahsilat"));
        Assert.Contains("kur-1", istek.Govde);
    }

    [Fact]
    public void Tutarsiz_tahsilat_kaydedilemez()
    {
        var e = Ciz();
        Dugme(e, "Tahsilat").Click();
        // PARA-DEMO-2 tur 5 (doğrulayıcı YÜKSEK): boş pencerede kırmızı yazı YOK, Kaydet açık; boş deneme İSTEK
        // ATMAZ ve tutar alanının altında tek cümle söyler (DoD EK md.3 — sessiz etkisizlik yok).
        istekler.Clear();
        Dugme(e, "Kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.Contains(Kuyumcu.Core.I18n.ParaDemo2Sozlugu.Tr.TutarYaDaAltin, e.Find("#th-tutar").Closest(".alan")!.TextContent);
    }

    [Fact]
    public void Tahsilat_YALNIZ_has_gram_ile_de_yapilabilir()
    {
        // Müşteri altınla ödeyebilir — TL sıfır olabilir.
        var e = Ciz();
        Dugme(e, "Tahsilat").Click();
        e.Find("#th-has-ekle").Click();   // PARA-DEMO-2 (madde 7): altınla ödeme "+ Altınla öde" arkasında
        e.Find("#th-has").Input("5");
        Assert.False(Dugme(e, "Kaydet").HasAttribute("disabled"));
    }

    // ── Veresiye ────────────────────────────────────────────────────────────

    [Fact]
    public void Veresiye_POST_ile_dogru_uca_gider()
    {
        var e = Ciz();
        Dugme(e, "Veresiye").Click();
        e.Find("#vr-tutar").Input("2500");
        Dugme(e, "Kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("veresiye"));
        Assert.EndsWith("/cari/c1/veresiye", istek.Yol);
        Assert.Contains("2500", istek.Govde);
        Assert.Contains("kur-1", istek.Govde);
    }

    [Fact]
    public void Sifir_tutarli_veresiye_kaydedilemez()
    {
        // Sunucu Range(0.01, ...) ile reddediyor; ekran da göndermemeli.
        var e = Ciz();
        Dugme(e, "Veresiye").Click();
        Assert.True(Dugme(e, "Kaydet").HasAttribute("disabled"));
    }

    // ── Detay (mevcut işlev — regresyon) ────────────────────────────────────

    [Fact]
    public void Detay_ekstreyi_ceker()
    {
        var e = Ciz();
        Dugme(e, "Detay").Click();
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("/ekstre"));
    }

    // ── Manuel düzeltme fişi (paket 33) ─────────────────────────────────────

    [Fact]
    public void Her_cari_satirinda_DUZELTME_FISI_dugmesi_vardir()
    {
        var e = Ciz();
        Assert.Equal(2, e.FindAll("button").Count(b => b.TextContent.Contains("Düzeltme Fişi")));
    }

    [Fact]
    public void Duzeltme_acilinca_TL_HAS_ve_GEREKCE_alanlari_cizilir()
    {
        var e = Ciz();
        Dugme(e, "Düzeltme Fişi").Click();

        Assert.NotNull(e.Find("#dz-tl"));
        Assert.NotNull(e.Find("#dz-has"));
        Assert.NotNull(e.Find("#dz-gerekce"));
        Assert.NotNull(e.Find("#dz-kaydet"));
    }

    [Fact]
    public void Gerekce_19_karakterse_KAYDET_KAPALI_20_karakterde_ACILIR()
    {
        // Sunucu da ≥ 20 istiyor (CariServisi.Duzeltme); ekran boşuna istek atmasın.
        var e = Ciz();
        Dugme(e, "Düzeltme Fişi").Click();
        e.Find("#dz-tl").Input("1250");

        e.Find("#dz-gerekce").Input(new string('a', 19));
        e.WaitForAssertion(() => Assert.True(e.Find("#dz-kaydet").HasAttribute("disabled")));

        e.Find("#dz-gerekce").Input(new string('a', 20));
        e.WaitForAssertion(() => Assert.False(e.Find("#dz-kaydet").HasAttribute("disabled")));
    }

    [Fact]
    public void Sifir_deltali_duzeltme_KAYDEDILEMEZ()
    {
        var e = Ciz();
        Dugme(e, "Düzeltme Fişi").Click();
        e.Find("#dz-gerekce").Input("Eski sistemden devir farkı — mutabakat");

        e.WaitForAssertion(() => Assert.True(e.Find("#dz-kaydet").HasAttribute("disabled")));
    }

    [Fact]
    public void Duzeltme_POST_govdesi_DELTA_GEREKCE_ve_KUR_tasir()
    {
        var e = Ciz();
        Dugme(e, "Düzeltme Fişi").Click();
        e.Find("#dz-tl").Input("1250");
        e.Find("#dz-has").Input("-0.5");
        e.Find("#dz-gerekce").Input("Eski sistemden devir farkı — mutabakat 2026-09");
        e.Find("#dz-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("duzeltme"));
            Assert.EndsWith("/cari/c1/duzeltme", istek.Yol);
            Assert.Equal(1250m, decimal.Parse(Alan(istek.Govde, "tlDelta")!, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal(-0.5m, decimal.Parse(Alan(istek.Govde, "hasDelta")!, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal("Eski sistemden devir farkı — mutabakat 2026-09", Alan(istek.Govde, "gerekce"));
            Assert.Equal("kur-1", Alan(istek.Govde, "kurSnapshotId"));
        });
    }

    [Fact]
    public void TEZGAHTAR_duzeltme_fisi_dugmesini_GORMEZ()
    {
        // Ekip oyu Q3: elle bakiye müdahalesi tezgâhtar ekranında DURMAZ.
        // ⚠ Bu bir kapı değil İKİNCİ kapıdır — sunucudaki [RolGerekli] ayrıca
        // HttpCariDuzeltmeTestleri'nde sınanır.
        var e = Ciz("TEZGAHTAR");
        Assert.DoesNotContain(e.FindAll("button"), b => b.TextContent.Contains("Düzeltme Fişi"));

        // Günlük işler (tahsilat/veresiye) TEZGÂHTAR'da DURUR — daraltma yok.
        Assert.Equal(2, e.FindAll("button").Count(b => b.TextContent.Contains("Tahsilat") && !b.TextContent.Contains("Döviz")));
        // Paket 27: döviz TAHSİLAT tezgâhtarda DURUR (para tahsil eder),
        // döviz VERESİYE ise durmaz (SAHIP/MUDUR/MUHASEBE — mevzuat sıkılaştırması).
        Assert.Equal(2, e.FindAll("button").Count(b => b.TextContent.Contains("Döviz Tahsilat")));
        Assert.DoesNotContain(e.FindAll("button"), b => b.TextContent.Contains("Döviz Veresiye"));
    }

    // ═══ PAKET 27 — DÖVİZ DEFTERİ EKRANI ════════════════════════════════════

    /// <summary>Sunucu döviz alanı dönünce liste satırında para birimli rozet çıkar.</summary>
    [Fact]
    public void Doviz_bakiyesi_olan_satirda_USD_rozeti_gorunur()
    {
        var e = Ciz();
        var satirlar = e.FindAll("tbody tr");
        // ⚠ Tutar ve para birimi BİRLİKTE taşınır (UX-S32) — çıplak sayı değil.
        //   Bicim.Para USD için "$" simgesini basar (tek biçimleyici, tek kaynak).
        Assert.Contains("1.250,00 $", satirlar[0].TextContent);
    }

    /// <summary>Sıfır döviz bakiyesinde rozet HİÇ çizilmez — gürültü yok.</summary>
    [Fact]
    public void Doviz_bakiyesi_sifir_olan_caride_rozet_CIKMAZ()
    {
        var e = Ciz();
        var ikinci = e.FindAll("tbody tr")[1];
        Assert.DoesNotContain("$", ikinci.TextContent);
        Assert.DoesNotContain("€", ikinci.TextContent);
        Assert.DoesNotContain("£", ikinci.TextContent);
    }

    /// <summary>
    /// Kuru OLMAYAN para birimi (GBP, sahte API null döner) seçilemez ve
    /// ekranda kur uyarısı vardır — "kur yok" sessiz kalmaz (UX-S30).
    /// </summary>
    [Fact]
    public void Kuru_olmayan_para_birimi_secilince_kur_yok_mesaji_cikar()
    {
        var e = Ciz();
        Dugme(e, "Döviz Veresiye").Click();
        SeciciYardim.Sec(e, "dv-para-birimi", "GBP");

        // TL karşılığı alanı sayı yerine "kur yok" der.
        // P-36c/3: türetilen değer artık <input readonly> değil, SALT OKUNUR METİN
        // (Bilesenler/Form/SaltOkunur). Eleman kimliği aynı; okuma yeri value → metin.
        Assert.Contains("kur", e.Find("#dv-tl-karsilik").TextContent,
            StringComparison.OrdinalIgnoreCase);
        // Kaydet kilitli ve sebebi yazılı (Cekmece kilit metni).
        Assert.Contains("kur", e.Find("#cekmece-doviz-veresiye-kilit").TextContent,
            StringComparison.OrdinalIgnoreCase);
        Assert.True(e.Find("#cekmece-doviz-veresiye-kaydet").HasAttribute("disabled"));
    }

    /// <summary>TL karşılığı SALT OKUNURDUR — kullanıcı yazamaz (A12 M73 gerekçesi).</summary>
    [Fact]
    public void TL_karsiligi_alani_salt_okunurdur()
    {
        var e = Ciz();
        Dugme(e, "Döviz Veresiye").Click();
        var alan = e.Find("#dv-tl-karsilik");
        // P-36c/3: "yazılamaz" artık disabled bir kutuyla değil, GİRDİ OLMAMASIYLA
        // sağlanır — devre dışı kutu ekran okuyucuya hâlâ "alan" diye görünüyordu.
        Assert.NotEqual("INPUT", alan.TagName);
        Assert.NotEqual("TEXTAREA", alan.TagName);
        Assert.Empty(alan.QuerySelectorAll("input, textarea"));
    }

    /// <summary>
    /// Döviz veresiye gövdesinde TL karşılığı YOKTUR: sunucu hesaplar.
    /// Gövde Türkçe JSON olarak AYRIŞTIRILARAK sınanır (kaçış tuzağı).
    /// </summary>
    [Fact]
    public void Doviz_veresiye_dogru_uca_TL_karsiligi_OLMADAN_gider()
    {
        var e = Ciz();
        Dugme(e, "Döviz Veresiye").Click();
        e.Find("#dv-tutar").Input("500");
        e.Find("#cekmece-doviz-veresiye-kaydet").Click();

        var istek = istekler.Single(i => i.Yol.EndsWith("/doviz-veresiye"));
        Assert.Equal("POST", istek.Metot);
        Assert.Equal("USD", Alan(istek.Govde, "paraBirimi"));
        Assert.Equal("500", Alan(istek.Govde, "tutar"));
        Assert.Equal("kur-1", Alan(istek.Govde, "kurSnapshotId"));
        // ⚠ İstemci TL değeri GÖNDERMEZ.
        Assert.Null(Alan(istek.Govde, "tlTutari"));
        Assert.Null(Alan(istek.Govde, "tlKarsiligi"));
    }

    /// <summary>
    /// Döviz tahsilat kasa seçicisi YALNIZ seçilen para birimindeki kasaları
    /// gösterir (K6). USD'de tek kasa var → seçici GİZLENİR ve otomatik atanır
    /// (UX-S22); EUR'da iki kasa var → seçici çıkar ve iki seçenek taşır.
    /// </summary>
    [Fact]
    public void Doviz_tahsilat_kasa_secicisi_yalniz_o_para_birimindeki_kasalari_gosterir()
    {
        var e = Ciz();
        Dugme(e, "Döviz Tahsilat").Click();

        // USD: tek kasa → seçici yok, salt okunur satır var.
        Assert.Empty(e.FindAll("#dt-kasa"));
        Assert.Equal("USD Kasa", e.Find("#dt-kasa-tek").TextContent.Trim());

        // EUR: iki kasa → seçici çıkar, TL kasası listede YOK.
        SeciciYardim.Sec(e, "dt-para-birimi", "EUR");
        var secenekler = SeciciYardim.Secenekler(e, "dt-kasa").Select(o => o.Ad()).ToList();
        Assert.Equal(2, secenekler.Count);
        Assert.Contains("EUR Kasa 1", secenekler);
        Assert.DoesNotContain("Merkez Kasa", secenekler);
    }

    /// <summary>Döviz tahsilat gövdesi kasa kimliğini ve efektifi taşır.</summary>
    [Fact]
    public void Doviz_tahsilat_dogru_uca_kasa_ve_efektif_ile_gider()
    {
        var e = Ciz();
        Dugme(e, "Döviz Tahsilat").Click();
        e.Find("#dt-tutar").Input("250");
        e.Find("#cekmece-doviz-tahsilat-kaydet").Click();

        var istek = istekler.Single(i => i.Yol.EndsWith("/doviz-tahsilat"));
        Assert.Equal("POST", istek.Metot);
        Assert.Equal("USD", Alan(istek.Govde, "paraBirimi"));
        Assert.Equal("250", Alan(istek.Govde, "tutar"));
        Assert.Equal("ks-usd", Alan(istek.Govde, "kasaId"));
    }

    /// <summary>
    /// SESSİZ ETKİSİZLİK YASAĞI: tutar 0 iken kaydet KİLİTLİDİR ve sebebi
    /// yazılıdır; tıklansa bile hiçbir istek ATILMAZ (DoD/1 + UX-S30).
    /// </summary>
    [Fact]
    public void Tutar_girilmeden_doviz_kaydet_KILITLI_ve_istek_ATILMAZ()
    {
        var e = Ciz();
        Dugme(e, "Döviz Veresiye").Click();
        var kaydet = e.Find("#cekmece-doviz-veresiye-kaydet");
        Assert.True(kaydet.HasAttribute("disabled"));
        Assert.NotEmpty(e.Find("#cekmece-doviz-veresiye-kilit").TextContent.Trim());

        kaydet.Click();
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/doviz-veresiye"));
    }

    /// <summary>
    /// K27-C ve vade yokluğu ekranda AÇIKÇA yazılır — "yapmıyor" şikâyeti
    /// bilgi saklandığı için doğmaz (Anayasa md.1).
    /// </summary>
    [Fact]
    public void Doviz_formunda_vade_yok_ve_fis_bacagi_uyarilari_YAZILIDIR()
    {
        var e = Ciz();
        Dugme(e, "Döviz Veresiye").Click();
        Assert.Contains("vade", e.Find("#dv-vadesiz-not").TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ÖDEME", e.Find("#dv-fis-bacagi-not").TextContent, StringComparison.OrdinalIgnoreCase);
    }
    // ── PAKET 40-A — PEP BEYANI (kurucunun düzenlenebilirlik şartı) ─────────

    [Fact]
    public void Duzenle_cekmecesinde_PEP_kutusu_vardir()
    {
        var ekran = Ciz();
        ekran.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();

        Assert.NotEmpty(ekran.FindAll("#duz-pep"));
        // Kutu işaretli DEĞİLKEN görev alanı istenmez (UX-S24).
        Assert.Empty(ekran.FindAll("#duz-pep-gorev"));
    }

    [Fact]
    public void PEP_isaretli_gorev_BOS_iken_KAYDET_KAPALI()
    {
        var ekran = Ciz();
        ekran.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();
        ekran.Find("#duz-pep").Change(true);

        // Görev alanı açıldı ama boş → Kaydet kilitli ve sebebi ekranda yazılı.
        Assert.NotEmpty(ekran.FindAll("#duz-pep-gorev"));
        Assert.True(ekran.Find("#cekmece-cari-kaydet").HasAttribute("disabled"));
        Assert.NotEmpty(ekran.FindAll("#duz-pep-hata"));
    }

    [Fact]
    public void PEP_gorevi_dolunca_KAYDET_ACILIR_ve_AYRI_pep_ucuna_gider()
    {
        var ekran = Ciz();
        ekran.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();
        ekran.Find("#duz-pep").Change(true);
        ekran.Find("#duz-pep-gorev").Input("Belediye meclis üyesi");

        Assert.False(ekran.Find("#cekmece-cari-kaydet").HasAttribute("disabled"));

        istekler.Clear();
        ekran.Find("#cekmece-cari-kaydet").Click();

        // ⚠ Kurul engel 4: PEP AYRI uca gider; tam gövdeli PATCH /cari/{id}
        //   cari alanlarını sıfırlayamaz.
        var pep = istekler.Single(i => i.Yol.EndsWith("/cari/c1/pep"));
        Assert.Equal("PATCH", pep.Metot);
        Assert.Contains("\"pepBeyani\":true", pep.Govde);
        Assert.Contains("Belediye meclis", pep.Govde);
    }

    [Fact]
    public void Listede_profili_olan_cari_ROZET_alir_olmayan_ALMAZ()
    {
        var ekran = Ciz();
        var satirlar = ekran.FindAll("tbody tr");

        // c1'in profili hiç hesaplanmadı → "Düşük" UYDURULMAZ.
        Assert.DoesNotContain("Düşük", satirlar[0].TextContent);
        // c2 yüksek riskli + PEP beyanlı → iki rozet.
        Assert.Contains("Yüksek", satirlar[1].TextContent);
        Assert.Contains("PEP", satirlar[1].TextContent);
    }


    // ── PAKET P-15 (B-29) — DERİN BAĞLANTI SÖZLEŞMESİ ───────────────────────
    //
    // ÖNCE: /cariler?cariId=… ile gelen kullanıcı (vade takibi bağlantısı,
    // kabuk araması) süzgecin uygulandığını sanıyor, tüm listeyi görüyordu.
    // Sorgu dizesi SESSİZCE düşüyordu. Bu üç test o sessizliği kapatır.

    /// <summary>Sorgudaki `q` süzgeci UYGULANIR (sunucuya gider) ve rozet çıkar.</summary>
    [Fact]
    public void Baglantidaki_q_suzgeci_sunucuya_gider_ve_rozetle_soylenir()
    {
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
            .NavigateTo("/cariler?q=Ay%C5%9Fe");
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));

        var e = Render<Kuyumcu.Web.Pages.Cariler>();

        e.WaitForAssertion(() =>
        {
            // ⚠ Sahte API yalnız AbsolutePath kaydeder (sorgu dizesi düşer), bu
            //   yüzden süzgecin uygulandığı KUTUDAN okunur: değer kutuya yazıldıysa
            //   Yukle() onu sorguya koyar (aynı alan, tek kaynak).
            Assert.Equal("Ayşe", e.Find("input[type=search]").GetAttribute("value"));
            // Süzgecin bağlantıdan geldiği GÖRÜNÜR (UX-S24) ve geri dönüş yolu var (UX-S25).
            Assert.NotNull(e.Find("#cari-suzgec-baglanti"));
            Assert.NotNull(e.Find("#cari-suzgec-temizle"));
        });
    }

    /// <summary>Süzgeç KUTUYA da yazılır — "uygulandı ama görünmüyor" olmaz.</summary>
    [Fact]
    public void Baglantidaki_tur_suzgeci_KUTUYA_yazilir()
    {
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
            .NavigateTo("/cariler?tur=TEDARIKCI");
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));

        var e = Render<Kuyumcu.Web.Pages.Cariler>();

        e.WaitForAssertion(() =>
            Assert.Equal("TEDARIKCI", e.Find("#cari-tur-filtre").Deger()));
    }

    /// <summary>Sorgu YOKSA rozet de YOKTUR: ekran olmayan bir süzgeci iddia etmez.</summary>
    [Fact]
    public void Sorgusuz_acilista_baglanti_rozeti_CIKMAZ()
    {
        var e = Ciz();
        Assert.Empty(e.FindAll("#cari-suzgec-baglanti"));
    }

    // ── PAKET P-61 — satır eylemleri taşma menüsü (Bilesenler/SatirEylemleri) ──

    /// <summary>
    /// P-61 kabul: satırda GÖRÜNEN eylem düğmesi en çok 3 (2 birincil + "⋯"); satırın
    /// geri kalan HER eylemi "⋯" penceresinde durur (özellik silinmez). Mutasyon
    /// M61-1 (BirincilSinir 2 → 8) bu testi KIRMIZIYA çevirir.
    /// </summary>
    [Fact]
    public void SatirdaGorunurEylem_EnFazlaUc()
    {
        var e = Ciz();
        var satirlar = e.FindAll("tr.cari-satir");
        Assert.Equal(2, satirlar.Count);
        foreach (var satir in satirlar)
        {
            var kap = satir.QuerySelector(".satir-eylem");
            Assert.NotNull(kap);
            var gorunen = kap!.QuerySelectorAll("button").ToList();
            Assert.InRange(gorunen.Count, 1, 3);
            Assert.Equal(2, gorunen.Count(b => !b.ClassList.Contains("satir-menu-ac")));
            // Son görünen düğme "⋯": aria-expanded başta "false", pencereyi adıyla gösterir.
            var ac = gorunen[^1];
            Assert.Contains("satir-menu-ac", ac.ClassList);
            Assert.Equal("false", ac.GetAttribute("aria-expanded"));
            var menuId = ac.GetAttribute("popovertarget");
            Assert.Equal(menuId, ac.GetAttribute("aria-controls"));
            // Taşan eylemler kaybolmadı: pencerede en az 4 gerçek eylem (+ Kapat).
            var menu = satir.QuerySelector($"#{menuId}");
            Assert.NotNull(menu);
            Assert.True(menu!.QuerySelectorAll("button").Length >= 5);
            // Satırda görünen / pencere dışında başıboş eylem düğmesi YOK.
            var disarida = satir.QuerySelectorAll("button")
                .Where(b => b.Closest(".satir-menu") is null && b.Closest(".satir-eylem") is null
                            && b.Closest(".satir-ici") is null)
                .ToList();
            Assert.Empty(disarida);
        }
    }

    // ═══ ORTAK-1 · KEŞİF #4 (30.09, KRİTİK) — "görünen ≠ giden": tuş tuş "1.500" ═══════════════
    // Kapısız formda kutu hatayı gösterip sonraki yeniden çizimde eski değere (0 / 1,5) dönüyordu ve Kaydet
    // o değeri POST ediyordu. Artık: kutu yazılanı ve hatayı korur, Kaydet PASİF + sebep kutunun ETİKETİYLE,
    // tıklansa bile (kod içi koruma) yazma isteği GİTMEZ.
    private static readonly string[] OK1TusTus1500 = ["1", "1.", "1.5", "1.50", "1.500"];

    private static string OK1Etiket(IRenderedComponent<Kuyumcu.Web.Pages.Cariler> e, string alan) =>
        e.Find($"label[for='{alan}']").TextContent.Replace("*", "").Split("(isteğe")[0].Trim();

    private void OK1BelirsizPostYok(IRenderedComponent<Kuyumcu.Web.Pages.Cariler> e, string alan,
                                    string kaydetSecici, string sebepSecici)
    {
        foreach (var a in OK1TusTus1500) e.Find($"#{alan}").Input(a);
        e.Find($"#{alan}").Blur();
        Assert.Equal("1.500", e.Find($"#{alan}").GetAttribute("value"));
        Assert.Equal("true", e.Find($"#{alan}").GetAttribute("aria-invalid"));
        Assert.True(e.Find(kaydetSecici).HasAttribute("disabled"));
        Assert.Contains(OK1Etiket(e, alan), e.Find(sebepSecici).TextContent);
        istekler.Clear();
        e.Find(kaydetSecici).Click();
        Assert.DoesNotContain(istekler, i => i.Metot is "POST" or "PATCH" or "PUT");
        Assert.Equal("1.500", e.Find($"#{alan}").GetAttribute("value"));
    }

    [Fact]
    public void OK1_K4_tahsilat_tutari_belirsiz_1_500_POST_YOK()
    {
        var e = Ciz();
        Dugme(e, "Tahsilat").Click();
        // Has ödemesi GEÇERLİ → düğmenin öbür kilitleri (tutar da has da 0) devre dışı; kilidi YALNIZ kapı tutar.
        e.Find("#th-has-ekle").Click();   // PARA-DEMO-2 (madde 7): altınla ödeme "+ Altınla öde" arkasında
        e.Find("#th-has").Input("5");
        OK1BelirsizPostYok(e, "th-tutar", "#th-kaydet", "#th-sayi-kilit");
        // Düzeltince aynı kutudan YAZILAN değer gider.
        e.Find("#th-tutar").Input("1500");
        Assert.Empty(e.FindAll("#th-sayi-kilit"));
        e.Find("#th-kaydet").Click();
        var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("tahsilat"));
        Assert.Contains("1500", istek.Govde);
    }

    [Fact]
    public void OK1_K4_veresiye_tutari_belirsiz_1_500_POST_YOK()
    {
        var e = Ciz();
        Dugme(e, "Veresiye").Click();
        OK1BelirsizPostYok(e, "vr-tutar", "#vr-kaydet", "#vr-sayi-kilit");
    }

    [Fact]
    public void OK1_K4_duzeltme_TL_belirsiz_1_500_POST_YOK()
    {
        var e = Ciz();
        Dugme(e, "Düzeltme Fişi").Click();
        e.Find("#dz-gerekce").Input(new string('a', 25));
        OK1BelirsizPostYok(e, "dz-tl", "#dz-kaydet", "#dz-sayi-kilit");
    }

    [Fact]
    public void OK1_K4_duzenle_cekmecesi_kredi_limiti_belirsiz_PATCH_YOK()
    {
        var e = Ciz();
        Dugme(e, "Düzenle").Click();
        e.WaitForElement("#duz-limit");
        e.WaitForAssertion(() => Assert.False(e.Find("#cekmece-cari-kaydet").HasAttribute("disabled")));
        OK1BelirsizPostYok(e, "duz-limit", "#cekmece-cari-kaydet", "#cekmece-cari-kilit");
    }

    [Fact]
    public void OK1_K4_yeni_cari_kredi_limiti_belirsiz_POST_YOK()
    {
        var e = Ciz();
        e.FindAll("button").First(b => b.TextContent.Contains("Yeni")).Click();
        e.WaitForElement("#cari-limit");
        e.Find("#cari-ad").Input("Anadolu Holding A.Ş.");
        OK1BelirsizPostYok(e, "cari-limit", "#cari-kaydet", "#cari-sayi-kilit");
    }

    // ── PAKET TCKN-DOGRULAMA (kurucu bulgusu 08.10) ─────────────────────────
    [Fact]
    public void TCKN_yeni_cari_11111111111_ANINDA_alan_alti_hata_Kaydet_kilitli_POST_YOK()
    {
        var e = Ciz();
        e.FindAll("button").First(b => b.TextContent.Contains("Yeni")).Click();
        e.WaitForElement("#cari-vergino");
        e.Find("#cari-ad").Input("Ali Veli");
        e.Find("#cari-vergino").Input("11111111111");

        e.WaitForAssertion(() => Assert.Equal("TC kimlik numarası geçerli değil.", e.Find("#alan-hata-cari-vergino").TextContent.Trim()));
        Assert.True(e.Find("#cari-kaydet").HasAttribute("disabled"));
        Assert.Equal("TC kimlik numarası geçerli değil.", e.Find("#cari-tckn-kilit").TextContent.Trim());
        istekler.Clear();
        e.Find("#cari-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");

        // Geçerli numara: hata kalkar, Kaydet açılır.
        e.Find("#cari-vergino").Input("10000000146");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#cari-tckn-kilit")));
        Assert.False(e.Find("#cari-kaydet").HasAttribute("disabled"));
    }

    [Fact]
    public void TCKN_duzenle_11111111111_ANINDA_hata_cekmece_Kaydet_kilitli_sebep_ayni()
    {
        var ekran = Ciz();
        ekran.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();
        ekran.WaitForElement("#duz-vergino");
        ekran.Find("#duz-vergino").Input("11111111111");

        ekran.WaitForAssertion(() => Assert.Equal("TC kimlik numarası geçerli değil.", ekran.Find("#alan-hata-duz-vergino").TextContent.Trim()));
        Assert.True(ekran.Find("#cekmece-cari-kaydet").HasAttribute("disabled"));
        Assert.Contains("TC kimlik numarası geçerli değil.", ekran.Find("#cekmece-cari-kilit").TextContent);
    }
}
