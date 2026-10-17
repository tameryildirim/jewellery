using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET UX-4b — EKRAN TARAFI (madde 6 · 7 · 8 · 9)
//
//   6 — Ürün düzenleme modalında konsinye alanları (sessiz silinme durdu)
//   7 — POS: has gram fiyatı kurdan türetilir + rozet; tek kasa otomatik+kilitli;
//       ana kasa seçicisi TRY ile süzülür
//   8 — Kur: manuel modalda GBP; "Şimdi çek" yükleniyor + başarı bandı
//   9 — Cari: tahsilat modalında kasa seçici; yeni cari modalı düzenleme
//       modalıyla ALAN KÜMESİ olarak eşit (UX-S26)
//
// Ölçülen şey "düğme var mı" değil: hangi uca HANGİ GÖVDEYLE gidildiği.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UX4bEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    /// <summary>Kasa listesi testten teste değişir (tek TL / TL+USD).</summary>
    private string kasaYaniti = """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""";

    public UX4bEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();   // UX-3 Faz B: Islemler artik bu servisi enjekte ediyor
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => kasaYaniti))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<string> kasalar) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            var g = yol switch
            {
                var y when y.EndsWith("/kasalar") => kasalar(),
                var y when y.EndsWith("/kur/guncelle") || y.EndsWith("/kur/manuel") =>
                    """{"id":"kur-2","altinHasFiyati":6900,"usdTry":34.2,"eurTry":37.1,"gbpTry":43.8,"kaynak":"MANUAL","olusturuldu":"2026-09-16T10:00:00Z","ayarFiyatlari":[]}""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-16T09:00:00Z","ayarFiyatlari":[]}""",
                var y when y.EndsWith("/kur/gecmis") => "[]",
                var y when y.Contains("/kur/pariteler") => """{"pariteler":[]}""",
                var y when y.EndsWith("/kur/kaynaklar") => "[]",
                var y when y.Contains("/fiyat/teklif") => """{"teklifVar":false,"sebep":"LISTE_YOK"}""",
                var y when y.Contains("/fiyat/listeler") =>
                    """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","aktif":true}]""",
                var y when y.Contains("/tevsik/durum") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var y when y.EndsWith("/urunler/u1") =>
                    """{"id":"u1","tip":"ZIYNET","kod":"K-1","ad":"Burma Bilezik","ayarId":"K22","gramAgirlik":10,"tasAgirlik":0,"iscilikTl":500,"barkod":"B1","konsinye":true,"konsinyeCariId":"c1","konsinyeBirimBedel":9500}""",
                var y when y.Contains("/urunler") =>
                    """[{"id":"u1","ad":"Burma Bilezik","kod":"K-1","tip":"ZIYNET","ayarId":"K22","gramAgirlik":10,"tasAgirlik":0,"hasGrami":9.16,"iscilikTl":500,"satisFiyati":null,"aktif":true,"konsinye":true}]""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                var y when y.EndsWith("/urun-kategorileri") => "[]",
                var y when y.Contains("/masak/esikler") =>
                    """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                // TEMA TEMİZLİĞİ 17.10: müşteri KARTI (Yıldız'da tahsilatın yolu) IBAN listesini okur — gerçek şekil: boş liste.
                var y when y.EndsWith("/ibanlar") => "[]",
                var y when y.EndsWith("/cari/c1") =>
                    """{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":null,"email":null,"vergiNo":null,"adres":null}""",
                var y when y.Contains("/cari") =>
                    """[{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","tlBakiye":-1500,"hasGramBakiye":0}]""",
                var y when y.Contains("/belgeler") => "[]",
                // P-10: Urunler ekrani parite kodlarini TEK KAYNAKTAN okur (UX-S31).
                var p10p when p10p.Contains("/kur/pariteler") =>
                    """{"kaynak":"TEST","pariteler":[{"kod":"BILEZIK_22","ad":"22 Ayar Bilezik","alis":6800,"satis":6900,"birim":"GRAM"}]}""",
                // P-10: KART bacagi POS cihazi listesini okur.
                var p10c when p10c.Contains("/pos/cihazlar") =>
                    """[{"id":"pc1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"s1","kasaId":"ks1","pesinKomisyonOran":1.5,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private void Oturum(string rol = "SAHIP") =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1"));

    private static string? Alan(string govde, string ad)
    {
        var e = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    // ═════════ Madde 7 — POS ═════════════════════════════════════════════════

    // TEMA TEMİZLİĞİ 17.10: satış fişi HER ZAMAN Yıldız tezgâhıdır (#yildiz-tezgah); müşteri seçimi "Değiştir" katmanında,
    // ödeme/kasa ödeme satırına dokununca (#yildiz-odeme-0) açılan düzende, karma "+ Başka ödeme"dedir. Öteki türler Yıldız
    // tür sekmelerinden (#yildiz-tur-HURDA_ALIS) ya da "Diğer"den (#yildiz-tezgah-tur → #islem-tur-{TÜR}) seçilir ve eski
    // formla çizilir. Testler bu gerçek yolları sürer; iddialar (kur kaynağı, kasa süzgeci, gövde) AYNI.
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Pos()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#yildiz-tezgah");
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/kur/guncel")));
        return e;
    }

    /// <summary>İşlem türü: ana türler Yıldız sekmesinde (#yildiz-tur-{kod}); ötekiler "Diğer" listesinde (#islem-tur-{kod}).</summary>
    private static void TurSec(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string kod)
    {
        if (e.FindAll($"#yildiz-tur-{kod}").Count > 0) { e.Find($"#yildiz-tur-{kod}").Click(); return; }
        e.Find("#yildiz-tezgah-tur").Click();
        e.WaitForElement($"#islem-tur-{kod}").Click();
    }

    /// <summary>Tezgâha bir kalem ekler (teklif yok — birim fiyat elle) ve kalem satırının çizilmesini bekler.</summary>
    private void KalemEkle(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/fiyat/teklif")));
        e.Find("#islem-birimfiyat").Input("10000");
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll(".yz-tezgah-kalem")));
    }

    [Fact]
    public void POS_hurda_has_gram_fiyati_KURDAN_gelir_ve_rozeti_soyler()
    {
        // UX-S21: sistemin bildiği değer boş giriş alanı olarak konmaz.
        var e = Pos();
        TurSec(e, "HURDA_ALIS");
        // Canlı kur ASENKRON yansır: değerin gelmesini bekle, yoksa iddia
        // "henüz gelmedi" hâlini ölçer (kararsız test).
        e.WaitForAssertion(() => Assert.Equal("6.800", e.Find("#hurda-fiyat").GetAttribute("value")));   // BICIM-2: odak dışı kutu gruplu
        Assert.Contains("Kurdan geldi", e.Find("#hurda-fiyat-kaynak").TextContent);
    }

    [Fact]
    public void POS_elle_girilen_hurda_fiyati_ELLE_GIRILDI_rozeti_alir()
    {
        var e = Pos();
        TurSec(e, "HURDA_ALIS");
        // ÖNCE kur yansısın: aksi hâlde elle giriş, arkadan gelen kur
        // yansımasıyla aynı render turuna düşüp ölçüm kararsızlaşıyor.
        e.WaitForAssertion(() => Assert.Equal("6.800", e.Find("#hurda-fiyat").GetAttribute("value")));   // BICIM-2: odak dışı kutu gruplu
        e.Find("#hurda-fiyat").Input("7100");
        e.WaitForAssertion(() =>
            Assert.Contains("Elle girildi", e.Find("#hurda-fiyat-kaynak").TextContent));
        Assert.Equal("7100", e.Find("#hurda-fiyat").GetAttribute("value"));
    }

    [Fact]
    public void POS_takas_eski_urun_fiyati_da_KURDAN_gelir()
    {
        var e = Pos();
        TurSec(e, "TAKAS");
        e.WaitForAssertion(() => Assert.Equal("6.800", e.Find("#takas-eski-fiyat").GetAttribute("value")));   // BICIM-2: odak dışı kutu gruplu
        Assert.Contains("Kurdan geldi", e.Find("#takas-eski-fiyat-kaynak").TextContent);
    }

    [Fact]
    public void POS_TEK_kasada_ana_kasa_otomatik_secilir_ve_KILITLENIR()
    {
        // UX-S22 — aynı ekranda aynı sorunun iki farklı cevabı olamaz.
        // SATIS-3 (kurucu 25.09 "kasa otomatik"): tek kasada kilitli seçici de çizilmez —
        // Yıldız tezgâhında tek kasa ödeme satırında TEK kez yazar ("Nakit · Merkez Kasa"); kasa seçicisi ve
        // "tek kasa — otomatik seçildi" notu yok — ödeme düzeni açıldığında da (UX-S22).
        var e = Pos();
        e.WaitForAssertion(() => Assert.Contains("Merkez Kasa", e.Find("#yildiz-odeme-0").TextContent));
        Assert.Empty(e.FindAll("#islem-kasa"));
        Assert.Empty(e.FindAll("#islem-kasa-tek"));
        e.Find("#yildiz-odeme-0").Click();
        e.WaitForElement("#yildiz-tezgah-odeme-duzen");
        Assert.Empty(e.FindAll("#islem-kasa"));
        Assert.DoesNotContain(Kuyumcu.Core.I18n.Sozlukler.Tr.Islem.TekKasaOtomatik, e.Markup);
    }

    [Fact]
    public void POS_ana_kasa_secicisinde_DOVIZ_kasasi_YOKTUR()
    {
        // TL satış USD kasasına yazılamaz (sunucu kapısı K6). Ekran imkânsız
        // seçeneği hiç göstermez; iki kasa olduğu için seçici de kilitli değildir.
        kasaYaniti = """
            [{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},
             {"id":"ks2","ad":"Dolar Kasa","paraBirimi":"USD","aktif":true},
             {"id":"ks3","ad":"Yedek TL","paraBirimi":"TRY","aktif":true}]
            """;
        var e = Pos();
        // Yıldız: kasa seçicisi ödeme satırına dokununca açılan ödeme düzenindedir.
        e.Find("#yildiz-odeme-0").Click();
        e.WaitForElement("#yildiz-tezgah-odeme-duzen #islem-kasa");
        // ⚠ KARARSIZLIK ÖNLEMİ (P-36c/3): kasa listesi SUNUCUDAN asenkron gelir;
        //   veri gelmeden açılan seçici BOŞ okunuyordu (tam süitte aralıklı kırmızı).
        var degerler = new List<string?>();
        e.WaitForAssertion(() =>
        {
            degerler = SeciciYardim.Secenekler(e, "islem-kasa").Select(o => (string?)o.Deger()).ToList();
            Assert.Contains("ks1", degerler);
        }, TimeSpan.FromSeconds(5));
        Assert.Contains("ks1", degerler);
        Assert.Contains("ks3", degerler);
        Assert.DoesNotContain("ks2", degerler);
        Assert.False(e.Find("#islem-kasa").HasAttribute("disabled"));
    }

    // ═════════ Madde 8 — Kur ═════════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Kur> KurEkrani()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Kur>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/kur/guncel")));
        istekler.Clear();
        return e;
    }

    [Fact]
    public void Manuel_kur_modali_MEVCUT_GBP_ile_acilir()
    {
        var e = KurEkrani();
        e.FindAll("button").First(b => b.TextContent.Contains("Manuel")).Click();
        // P-78: girdi kutusu değeri tr-TR gruplamasız basılır (virgül ondalık); eski
        // Invariant yazım ("1.234") tr-TR çözücüde BELİRSİZ sayılıp kutu kendi değerini
        // geri okuyamazdı. Değer aynı, yazım değişti.
        Assert.Equal("43,5", e.Find("#m-gbp").GetAttribute("value"));
    }

    [Fact]
    public void Manuel_kur_govdesinde_GBP_TASINIR()
    {
        // Sunucu (ManuelKurDto.GbpTry) bunu başından beri kabul ediyordu;
        // ekran göndermediği için POS'ta sterlin bacağı 0 TL kalıyordu.
        var e = KurEkrani();
        e.FindAll("button").First(b => b.TextContent.Contains("Manuel")).Click();
        e.Find("#m-gbp").Input("44.75");
        e.FindAll("button").First(b => b.TextContent.Contains("Kaydet")).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);

        var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/kur/manuel"));
        Assert.Equal("44.75", Alan(istek.Govde, "gbpTry"));
    }

    [Fact]
    public void Simdi_cek_sonrasi_BASARI_bandi_gorunur()
    {
        // UX-S27: yazma işleminin sonucu ekranda görünür.
        var e = KurEkrani();
        e.Find("#kur-simdi-cek").Click();
        e.WaitForAssertion(() => Assert.Contains("güncellendi", e.Markup));
    }

    // ═════════ Madde 9 — Cari ════════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Cariler> CariEkrani()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("#yildiz-musteri-c1");
        istekler.Clear();
        return e;
    }

    /// <summary>
    /// TEMA TEMİZLİĞİ 17.10: Yıldız müşteri listesinde satır eylemi yok — satıra dokununca açılan kartın eylem satırındaki
    /// "Tahsilat" (#c-tahsilat-c1, özgün satır eylemiyle AYNI geri çağrı TahsilatAc) tahsilat çekmecesini açar.
    /// </summary>
    private static void TahsilatAc(IRenderedComponent<Kuyumcu.Web.Pages.Cariler> e)
    {
        e.Find("#yildiz-musteri-c1").Click();
        e.WaitForElement("#c-tahsilat-c1").Click();
        e.WaitForElement("#th-tutar");
    }

    [Fact]
    public void Yeni_cari_modali_duzenleme_modaliyla_AYNI_alan_kumesini_tasir()
    {
        // UX-S26 — sunucunun oluşturmada kabul ettiği alan "sonra düzenlemeden
        // gir" diye ertelenmez.
        var e = CariEkrani();
        e.FindAll("button").First(b => b.TextContent.Contains("Yeni")).Click();
        e.WaitForElement("#cari-vergino");
        Assert.NotNull(e.Find("#cari-limit"));
        Assert.NotNull(e.Find("#cari-vade"));
        Assert.NotNull(e.Find("#cari-fiyat-listesi"));
        // Tür seçenekleri TEK diziden: iki modalda da beş seçenek.
        Assert.Equal(5, SeciciYardim.Secenekler(e, "cari-tur").Count);
    }

    [Fact]
    public void Yeni_cari_govdesi_vergiNo_ve_fiyatListesiId_TASIR()
    {
        var e = CariEkrani();
        e.FindAll("button").First(b => b.TextContent.Contains("Yeni")).Click();
        e.WaitForElement("#cari-vergino");
        e.Find("#cari-ad").Input("Toptancı A");
        e.Find("#cari-vergino").Input("1234567890");
        SeciciYardim.Sec(e, "cari-fiyat-listesi", "fl1");
        e.FindAll("button").First(b => b.TextContent.Contains("Kaydet")).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);

        var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/cari"));
        Assert.Equal("1234567890", Alan(istek.Govde, "vergiNo"));
        Assert.Equal("fl1", Alan(istek.Govde, "fiyatListesiId"));
    }

    [Fact]
    public void Tahsilat_govdesinde_KASA_ID_gider_tek_kasada_otomatik()
    {
        var e = CariEkrani();
        TahsilatAc(e);
        e.Find("#th-tutar").Input("1500");
        e.WaitForElement("#th-kasa");
        Assert.True(e.Find("#th-kasa").HasAttribute("disabled"));   // UX-S22
        e.Find("#th-kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("tahsilat"));
        Assert.Equal("ks1", Alan(istek.Govde, "kasaId"));
    }

    [Fact]
    public void Tahsilat_kasa_secicisinde_DOVIZ_kasasi_YOKTUR()
    {
        kasaYaniti = """
            [{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},
             {"id":"ks2","ad":"Dolar Kasa","paraBirimi":"USD","aktif":true}]
            """;
        var e = CariEkrani();
        TahsilatAc(e);
        e.Find("#th-tutar").Input("1500");
        e.WaitForElement("#th-kasa");
        // P-36c: TL tahsilatta uygun TEK kasa kaldığı için seçici KİLİTLİ ve
        // o kasa seçili (UX-S22). Döviz kasasının listelenmediğinin ölçülebilir
        // karşılığı: seçili değer ks1, ekranda ks2 adı GEÇMEZ.
        Assert.Equal("ks1", SeciciYardim.SeciliDeger(e, "th-kasa"));
        Assert.DoesNotContain("Dolar Kasa", e.Find("#th-kasa-sar").TextContent);
    }

    [Fact]
    public void Kasasiz_tahsilat_KAYDEDILEMEZ_ve_sebebi_yazar()
    {
        // UX-S30 — kilitli düğme sebebini söyler. Sunucudaki kapı asıl kapıdır.
        kasaYaniti = """
            [{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},
             {"id":"ks3","ad":"Yedek TL","paraBirimi":"TRY","aktif":true}]
            """;
        var e = CariEkrani();
        TahsilatAc(e);
        e.Find("#th-tutar").Input("1500");
        e.WaitForElement("#th-kasa");
        Assert.True(e.Find("#th-kaydet").HasAttribute("disabled"));
        // SON-KALANLAR (hasım DÜŞÜK, B1440__21): "Kasa seçin" üç kez yazılıyordu — sebep TEK yerde, altlıkta.
        // SON-KALANLAR tur 2: altlık sebebi yer tutucunun ("Kasa seçin...") cümlesini tekrar etmez.
        Assert.Contains(Kuyumcu.Core.I18n.SonKalanlarSozlugu.Tr.TahsilatKasaKilit, e.Find("#th-kaydet-kilit").TextContent);
        Assert.Empty(e.FindAll("#th-kasa-gerekli"));
    }

    [Fact]
    public void Sadece_has_gram_tahsilatinda_kasa_secicisi_CIKMAZ()
    {
        var e = CariEkrani();
        TahsilatAc(e);
        e.Find("#th-has-ekle").Click();   // PARA-DEMO-2 (madde 7): altınla ödeme "+ Altınla öde" arkasında
        e.Find("#th-has").Input("5");
        Assert.Empty(e.FindAll("#th-kasa"));
        Assert.False(e.Find("#th-kaydet").HasAttribute("disabled"));
    }

    // ═════════ Madde 6 — Ürün düzenleme ══════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Urunler> UrunEkrani()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Urunler>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/urunler")));
        istekler.Clear();
        return e;
    }

    [Fact]
    public void Urun_duzenleme_modalinda_KONSINYE_alanlari_vardir()
    {
        var e = UrunEkrani();
        e.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();
        e.WaitForElement("#d-konsinye");
        Assert.NotNull(e.Find("#d-kons-cari"));
        // BICIM-2: odak DIŞINDA para kutusu gruplu.
        Assert.Equal("9.500", e.Find("#d-kons-bedel").GetAttribute("value"));
    }

    [Fact]
    public void Urun_PATCH_govdesi_KONSINYE_ucluyu_tasir()
    {
        // Eskiden gövdede hiç yoktu: sunucu false alıyor, sahibi+bedeli
        // SESSİZCE siliniyordu (başkasının malı işletme stoğuna dönüyordu).
        var e = UrunEkrani();
        e.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();
        e.WaitForElement("#d-konsinye");
        e.FindAll("button").First(b => b.TextContent.Contains("Kaydet")).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);

        var istek = Assert.Single(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/urunler/u1"));
        Assert.Equal("True", Alan(istek.Govde, "konsinye"));
        Assert.Equal("c1", Alan(istek.Govde, "konsinyeCariId"));
        Assert.Equal("9500", Alan(istek.Govde, "konsinyeBirimBedel"));
    }

    // ═════════ Madde 7(d) — karma ödemede TEK kasa sorusu ════════════════════
    // Kurucu ek bulgusu (spesifikasyon § "Kurucu ek bulgusu — madde 7(d)"):
    // karma açıkken üstte bir "Kasa" seçici, altta her bacak için ayrı kasa
    // soruluyordu (aynı ekranda aynı soru iki kez — UX-S22). Üstteki alan
    // POST /islemler/satis-fisi gövdesine HİÇ girmiyordu: ölü alan.

    /// <summary>
    /// TEMA TEMİZLİĞİ 17.10: Yıldız tezgâhında karma "+ Başka ödeme"dir (#yildiz-tezgah-baska-odeme → OdemeyiBol; özgün
    /// #islem-karma anahtarı satışta ulaşılamaz). Kalem gerekir (düğme kalemsiz kilitli); iki ödeme satırı açılır.
    /// </summary>
    private void KarmaAc(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        if (e.FindAll(".yz-tezgah-kalem").Count == 0) KalemEkle(e);
        e.Find("#yildiz-tezgah-baska-odeme").Click();
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll(".bacak-satiri").Count));
    }

    [Fact]
    public void Karma_acikken_ust_kasa_secicisi_CIZILMEZ()
    {
        var e = Pos();
        e.WaitForAssertion(() => Assert.Contains("Merkez Kasa", e.Find("#yildiz-odeme-0").TextContent));  // tek kasa ödeme satırında
        KarmaAc(e);
        Assert.Empty(e.FindAll("#islem-kasa"));       // karma açıkken DOM'da YOK
        Assert.Empty(e.FindAll("#islem-kasa-tek"));
    }

    [Fact]
    public void Karma_kapaninca_ust_secici_GERI_gelir_ve_son_bacak_kasasi_on_degerdir()
    {
        // İki TL kasası: üst seçici tek kasa kuralıyla değil, bacaktan dolmalı.
        kasaYaniti = """
            [{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},
             {"id":"ks3","ad":"Yedek TL","paraBirimi":"TRY","aktif":true}]
            """;
        var e = Pos();
        KarmaAc(e);
        e.WaitForElement("#islem-bacak-0-kasa");
        SeciciYardim.Sec(e, "islem-bacak-0-kasa", "ks3");

        // Yıldız'da karma, öteki satır "Çıkar"ılınca tek ödemeye iner (YildizTezgah → TekOdemeyeDonAracla → KarmaDegisti):
        // kalan (son) satırın kasası tek ödemenin kasası olur.
        e.Find("#islem-bacak-sil-1").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll(".bacak-satiri")));
        e.WaitForAssertion(() => Assert.Contains("Yedek TL", e.Find("#yildiz-odeme-0").TextContent));
        e.Find("#yildiz-odeme-0").Click();
        e.WaitForElement("#islem-kasa");
        Assert.Equal("ks3", SeciciYardim.SeciliDeger(e, "islem-kasa"));
    }

    [Fact]
    public void Karma_bacak_kasasi_TEK_TL_kasasinda_otomatik_ve_KILITLIDIR()
    {
        // UX-S22: tek seçenek sorulmaz. kasaYaniti varsayılanı tek TRY kasasıdır.
        var e = Pos();
        KarmaAc(e);
        e.WaitForElement("#islem-bacak-0-arac");
        // SATIS-3 (KAYNAK 5): tek TL kasasında bacak başına "Kasa seçin" HİÇ yok; kısa satır.
        Assert.Empty(e.FindAll("#islem-bacak-0-kasa"));
        Assert.Contains("Merkez Kasa", e.Find("#islem-bacak-0-kasa-tek").TextContent);
        Assert.DoesNotContain("otomatik", e.Find("#islem-bacak-0-kasa-tek").TextContent);   // YASAL-KAPI-3: teknik not yok
    }

    [Fact]
    public void Karma_fis_govdesinde_YALNIZ_BACAK_kasasi_gider()
    {
        var e = Pos();
        KalemEkle(e);
        // Müşteri Yıldız'da "Değiştir" katmanından seçilir (aynı MusteriDegisti yolu).
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForElement("#yildiz-musteri-sec-c1").Click();
        e.WaitForAssertion(() => Assert.Contains("Ayşe Demir", e.Find("#yildiz-tezgah-ust").TextContent));

        KarmaAc(e);
        // "+ Başka ödeme": 1. satır Nakit (fiş toplamı), 2. satır 0 — 2. satırı Çıkar'mak tek ödemeye indirir; bu yüzden
        // ödeme iki satıra BÖLÜNÜR (Nakit 6.000 + 4.000) ve fiş TEK POST (satis-fisi) ile gider.
        e.Find("#islem-bacak-0-tutar").Input("6000");
        e.Find("#islem-bacak-0-tutar").Blur();
        e.Find("#islem-bacak-1-tutar").Input("4000");
        e.Find("#islem-bacak-1-tutar").Blur();
        e.WaitForAssertion(() => Assert.False(e.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled")));
        istekler.Clear();

        e.Find("#yildiz-tezgah-kaydet").Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Yol.EndsWith("/islemler/satis-fisi")));

        var govde = istekler.Last(i => i.Yol.EndsWith("/islemler/satis-fisi")).Govde;
        var kok = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        // Üst kasaId gövdede HİÇ yok; kasa yalnız bacakta.
        Assert.False(kok.TryGetProperty("kasaId", out _));
        var bacak = kok.GetProperty("odemeler")[0];
        Assert.Equal("ks1", bacak.GetProperty("kasaId").GetString());
    }

    // ═════════ PAKET P-70 — Kur yoklaması: push bağlıyken duraklat + tek okuma noktası ═════
    //
    // QA turu 8: 6 rotalık turda 154 /kur/guncel isteği. Ölçülen şey SAYI:
    // sahte saatle 60 sn "yaşanır", /kur/guncel'e giden GET'ler sayılır.

    /// <summary>
    /// Elle ilerletilen saat. PeriodicTimer(TimeSpan, TimeProvider) bu sağlayıcının
    /// zamanlayıcısını kullanır; Ilerle() vadesi gelen tikleri SIRAYLA ateşler.
    /// </summary>
    private sealed class SahteSaat : TimeProvider
    {
        private readonly object kilit = new();
        private readonly List<SahteZamanlayici> zamanlayicilar = [];
        private DateTimeOffset simdi = new(2026, 9, 24, 9, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() { lock (kilit) return simdi; }

        public override ITimer CreateTimer(TimerCallback geriCagri, object? durum, TimeSpan vade, TimeSpan periyot)
        {
            var z = new SahteZamanlayici(this, geriCagri, durum);
            z.Change(vade, periyot);
            lock (kilit) zamanlayicilar.Add(z);
            return z;
        }

        public void Ilerle(TimeSpan sure)
        {
            DateTimeOffset hedef;
            lock (kilit) hedef = simdi + sure;
            while (true)
            {
                SahteZamanlayici? z;
                lock (kilit)
                {
                    z = zamanlayicilar.Where(x => x.Sonraki is { } s && s <= hedef)
                                      .OrderBy(x => x.Sonraki).FirstOrDefault();
                    if (z is not null) simdi = z.Sonraki!.Value;
                }
                if (z is null) break;
                z.Tetikle();
            }
            lock (kilit) simdi = hedef;
        }

        internal void Birak(SahteZamanlayici z) { lock (kilit) zamanlayicilar.Remove(z); }

        internal sealed class SahteZamanlayici(SahteSaat saat, TimerCallback geriCagri, object? durum) : ITimer
        {
            private TimeSpan periyot = Timeout.InfiniteTimeSpan;
            public DateTimeOffset? Sonraki { get; private set; }

            public bool Change(TimeSpan vade, TimeSpan yeniPeriyot)
            {
                periyot = yeniPeriyot;
                Sonraki = vade == Timeout.InfiniteTimeSpan ? null : saat.GetUtcNow() + vade;
                return true;
            }

            public void Tetikle()
            {
                Sonraki = periyot == Timeout.InfiniteTimeSpan || periyot == TimeSpan.Zero
                    ? null : Sonraki + periyot;
                geriCagri(durum);
            }

            public void Dispose() { Sonraki = null; saat.Birak(this); }
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }

    private SahteSaat SaatKur()
    {
        var saat = new SahteSaat();
        Services.AddSingleton<TimeProvider>(saat);
        return saat;
    }

    private async Task<CanliKurServisi> KurServisiAsync(bool baslat)
    {
        await Services.GetRequiredService<OturumServisi>().TokenKaydetAsync("test-erisim", "test-yenile");
        var kur = Services.GetRequiredService<CanliKurServisi>();
        if (baslat) await kur.BaslatAsync();
        return kur;
    }

    private int KurGuncelIstekSayisi() =>
        istekler.ToArray().Count(i => i.Metot == "GET" && i.Yol.EndsWith("/kur/guncel"));

    /// <summary>Ateşlenen tikin (varsa) isteğinin sahte API'ye ulaşması için kısa gerçek bekleme.</summary>
    private static Task Sakin() => Task.Delay(150);

    private async Task SayiyiBekle(int beklenen)
    {
        for (var i = 0; i < 100 && KurGuncelIstekSayisi() < beklenen; i++) await Task.Delay(20);
        await Sakin();   // fazladan istek de yakalansın diye
        Assert.Equal(beklenen, KurGuncelIstekSayisi());
    }

    [Fact]
    public async Task CanliKurServisi_PushBagliykenYoklamaIstekAtmaz()
    {
        var saat = SaatKur();
        var kur = await KurServisiAsync(baslat: true);
        await SayiyiBekle(1);                        // yalnız ilk REST okuma

        kur.BaglantiDurumunuIsle(true);              // push bağlandı
        Assert.True(kur.Bagli);
        var ilkTeyit = kur.SonSorgu;

        for (var s = 0; s < 6; s++)                  // 60 sn simüle
        {
            saat.Ilerle(kur.Aralik);
            await Sakin();
        }

        Assert.Equal(1, KurGuncelIstekSayisi());
        // Çubuk donmaz: canlı kanal teyidiyle "son sorgu" saati yine ilerler.
        Assert.NotNull(kur.SonSorgu);
        Assert.True(kur.SonSorgu >= ilkTeyit);
    }

    [Fact]
    public async Task CanliKurServisi_PushKopuncaYoklamaDevreyeGirer()
    {
        var saat = SaatKur();
        var kur = await KurServisiAsync(baslat: true);
        await SayiyiBekle(1);

        kur.BaglantiDurumunuIsle(true);
        for (var s = 0; s < 3; s++) { saat.Ilerle(kur.Aralik); await Sakin(); }
        Assert.Equal(1, KurGuncelIstekSayisi());

        kur.BaglantiDurumunuIsle(false);             // push koptu
        Assert.False(kur.Bagli);
        await SayiyiBekle(2);                        // kopuş anında kaçırılanı telafi

        saat.Ilerle(TimeSpan.FromSeconds(10));       // 10 sn içinde yoklama yeniden istek atar
        await SayiyiBekle(3);
        saat.Ilerle(TimeSpan.FromSeconds(10));
        await SayiyiBekle(4);
    }

    [Fact]
    public async Task CanliKurServisi_AlAsync_ArtArdaCagrilardaTekIstek()
    {
        var saat = SaatKur();
        var kur = await KurServisiAsync(baslat: false);

        var sonuclar = new List<CanliKurServisi.KurAnlik>();
        for (var i = 0; i < 5; i++) sonuclar.Add(await kur.AlAsync());

        Assert.Equal(1, KurGuncelIstekSayisi());
        Assert.All(sonuclar, s => Assert.Equal("kur-1", s.Id));
        Assert.Equal(6800m, sonuclar[4].AltinHasFiyati);

        // Tazelik penceresi aşılınca bir sonraki okuma GERÇEKTEN sunucuya gider.
        saat.Ilerle(kur.Aralik + TimeSpan.FromSeconds(1));
        await kur.AlAsync();
        Assert.Equal(2, KurGuncelIstekSayisi());
    }

    [Fact]
    public async Task CanliKurServisi_AlAsyncZorla_HerCagridaIstekAtar()
    {
        SaatKur();
        var kur = await KurServisiAsync(baslat: false);

        await kur.AlAsync();                          // önbellek dolu ve taze
        Assert.Equal(1, KurGuncelIstekSayisi());

        for (var i = 0; i < 4; i++)
        {
            var k = await kur.AlAsync(zorla: true);   // "Yenile" yalancı olmaz
            Assert.Equal("kur-1", k.Id);
            Assert.Equal(2 + i, KurGuncelIstekSayisi());
        }
    }

    [Fact]
    public async Task CanliKurServisi_SekmeGizliykenYoklamaDuraklar()
    {
        var saat = SaatKur();
        var kur = await KurServisiAsync(baslat: true);
        await SayiyiBekle(1);

        kur.GorunurlukDegisti(false);                 // sekme arka plana gitti
        for (var s = 0; s < 6; s++) { saat.Ilerle(kur.Aralik); await Sakin(); }
        Assert.Equal(1, KurGuncelIstekSayisi());

        kur.GorunurlukDegisti(true);                  // geri geldi: kur bayat → hemen tazele
        await SayiyiBekle(2);
    }

    [Fact]
    public void CanliKurServisi_YoklamaAraligiYapilandirmadanOkunur()
    {
        static Microsoft.Extensions.Configuration.IConfiguration Yap(string? deger) =>
            Microsoft.Extensions.Configuration.MemoryConfigurationBuilderExtensions.AddInMemoryCollection(
                new Microsoft.Extensions.Configuration.ConfigurationBuilder(),
                new Dictionary<string, string?> { [CanliKurServisi.AralikAnahtari] = deger }).Build();

        Assert.Equal(TimeSpan.FromSeconds(10), CanliKurServisi.YoklamaAraligi);   // varsayılan korunur
        Assert.Equal(TimeSpan.FromSeconds(10), CanliKurServisi.AralikOku(null));
        Assert.Equal(TimeSpan.FromSeconds(30), CanliKurServisi.AralikOku(Yap("30")));
        Assert.Equal(TimeSpan.FromSeconds(10), CanliKurServisi.AralikOku(Yap("1")));     // sunucuyu döver → ret
        Assert.Equal(TimeSpan.FromSeconds(10), CanliKurServisi.AralikOku(Yap("301")));   // çok bayat → ret
        Assert.Equal(TimeSpan.FromSeconds(10), CanliKurServisi.AralikOku(Yap("on")));    // sayı değil → ret
    }
}
