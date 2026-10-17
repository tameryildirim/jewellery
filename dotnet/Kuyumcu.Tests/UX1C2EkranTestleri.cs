using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET UX-1-C2 — "her veri girilen yer düzeltilebilir", sayfa grubu 2
// (Fiyatlandırma · Kasalar · Banka · Kur · Emanet · Sayım · Faturalar)
//
// Her sayfa için sınananlar:
//   1. <SayfaBasi> VAR ve başlığı BOŞ DEĞİL (#sayfa-basi-{anahtar}-baslik)
//   2. Düzenlenebilir alanın DÜZENLE yolu gerçekten isteğe dönüşür (PATCH/POST)
//   3. Düzenlenemeyen kayıtta DÜĞME YOK ama GEREKÇE VAR (DegismezIpucu)
//   4. Boş/geçersiz gönderimde SESSİZ ETKİSİZLİK yok — alan altı Türkçe mesaj
//   5. Storno düğmesi: kapsam içinde POST atar, kapsam dışında KİLİTLİ + gerekçe
//
// ⚠ Sahte API '{}' varsayılanıyla BIRAKILMADI: bu paketin dokunduğu her uca
//   gerçek yanıt şekli verildi (S-2'nin NRE'si tam da '{}'dan doğmuştu).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UX1C2EkranTestleri : BunitContext
{

    // ZO-BELGE (tasarım hakemi t3, K22): fatura formlarındaki yerel <select>'ler ortak
    // Secici (uzun liste) ve Form/Secim (radyo) ailesine geçti; id'ler AYNI.
    private static void RadyoSec<T>(IRenderedComponent<T> e, string id, string deger)
        where T : Microsoft.AspNetCore.Components.IComponent
        => e.Find($"#{id} input[value='{deger}']").Change(deger);

    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly SahteApi sahte = new();

    public UX1C2EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        sahte.Kayit = istekler;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(sahte) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();   // paket 41: /banka MASAK kapısı
    }

    // ── Sahte API ────────────────────────────────────────────────────────────

    private sealed class SahteApi : HttpMessageHandler
    {
        public List<(string, string, string)> Kayit = [];
        /// <summary>Kasa geçmişinde defter bağı olan (stornolanabilir) satır da olsun.</summary>
        public bool DefterBagliKasaHareketi;
        /// <summary>Güncel kur bulunamasın (storno kilidi senaryosu).</summary>
        public bool KurYok;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            Kayit.Add((istek.Method.Method, yol, govde));

            // PAKET S-3b: sahte yanıt YENİ UÇLARIN GERÇEKÇİ gövdesini taşır
            // (aciklama + stornoKaynakId) — "{}" varsayılanına düşürülmez.
            var kasaHareketleri = DefterBagliKasaHareketi
                ? """[{"id":"kh1","tutar":150,"odemeTuru":"NAKIT","tarih":"2026-09-16T10:00:00Z","hareketId":"h1","aciklama":null,"stornoKaynakId":null}, {"id":"kh2","tutar":-50,"odemeTuru":"NAKIT","tarih":"2026-09-16T11:00:00Z","hareketId":null,"aciklama":"Dükkân kirası — Eylül","stornoKaynakId":null}]"""
                : """[{"id":"kh2","tutar":-50,"odemeTuru":"NAKIT","tarih":"2026-09-16T11:00:00Z","hareketId":null,"aciklama":"Dükkân kirası — Eylül","stornoKaynakId":null}]""";

            var y = yol switch
            {
                // ── ortak ────────────────────────────────────────────────────
                var v when v.Contains("/kur/guncel") =>
                    KurYok ? "{}" : """{"id":"kur1","altinHasFiyati":4100,"gramAltinFiyati":4300,"usdTry":34,"eurTry":37,"gbpTry":43,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-16T09:00:00Z","ayarFiyatlari":[]}""",
                var v when v.Contains("/kur/manuel/") && v.Contains("/iptal") =>
                    """{"id":"kur1","altinHasFiyati":4100,"kaynak":"MANUAL","olusturuldu":"2026-09-16T09:00:00Z","ayarFiyatlari":null,"iptalEdildi":true,"iptalGerekce":"gerekçe","iptalTarihi":"2026-09-17T09:00:00Z"}""",
                var v when v.Contains("/kur/gecmis") =>
                    """[{"id":"kur1","altinHasFiyati":4100,"kaynak":"MANUAL","olusturuldu":"2026-09-16T09:00:00Z","ayarFiyatlari":null,"iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null}]""",
                var v when v.Contains("/kur/pariteler") => """{"kaynak":"ALTINKAYNAK","pariteler":[]}""",
                var v when v.Contains("/kur/kaynaklar") =>
                    """{"kaynaklar":["ALTINKAYNAK"],"varsayilan":"ALTINKAYNAK","secili":"ALTINKAYNAK","sozlesmesizKaynaklarAcik":false,"seciliSozlesmesiz":false,"manuelKod":"MANUAL"}""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez Şube"}]""",
                var v when v.Contains("/reyonlar") => """[{"id":"r1","ad":"Vitrin"}]""",
                var v when v.Contains("/urun-kategorileri/duz") => """[{"id":"kat1","ad":"Bilezik"}]""",

                // ── fiyatlandırma ────────────────────────────────────────────
                var v when v.Contains("/fiyat/listeler") && istek.Method == HttpMethod.Get =>
                    """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","hasMarjYuzde":5,"iscilikCarpanYuzde":100,"varsayilan":true,"aktif":true}, {"id":"fl2","ad":"Toptan","tur":"TOPTAN","hasMarjYuzde":2,"iscilikCarpanYuzde":50,"varsayilan":false,"aktif":true}]""",
                var v when v.Contains("/fiyat/kampanyalar") && istek.Method == HttpMethod.Get =>
                    """[{"id":"km1","ad":"Eylül","baslangic":"2026-09-01","bitis":"2026-09-20","hedefTur":"TUMU","hedefId":null,"indirimTuru":"ISCILIK_YUZDE","deger":10,"aktif":true}]""",

                // ── kasalar ──────────────────────────────────────────────────
                var v when v.Contains("/kasalar/") && v.Contains("/hareketler") =>
                    $$"""{"kasa":{"id":"k1","ad":"Ana Kasa","paraBirimi":"EUR","bakiye":150,"aktif":true,"subeId":"s1"},"hareketler":{{kasaHareketleri}}}""",
                var v when v.Contains("/kasalar") && istek.Method == HttpMethod.Get =>
                    """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"EUR","bakiye":150,"aktif":true,"subeId":"s1"}, {"id":"k2","ad":"Kapalı Kasa","paraBirimi":"TRY","bakiye":0,"aktif":false,"subeId":"s1"}]""",

                // ── banka ────────────────────────────────────────────────────
                // PAKET 41: /banka dekont formu MASAK kapısına bağlandı; eşikler
                // bu uçtan gelir ("{}" varsayılanına düşürülmez).
                var v when v.Contains("/masak/esikler") =>
                    """{"kimlikEsigi":185000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000,"ekBelgeEsigi":20000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var v when v.Contains("/bankalar/") && v.Contains("/hareketler") =>
                    """{"hareketler":[{"id":"bh1","yon":"GELEN","tutar":1250,"karsiTarafAd":"Ali Veli","aciklama":"Dekont","durum":"BEKLIYOR","tarih":"2026-09-16T10:00:00Z"}]}""",
                var v when v.Contains("/bankalar") && istek.Method == HttpMethod.Get =>
                    """{"hesaplar":[{"id":"b1","bankaAdi":"Ziraat","hesapAdi":"Ana Hesap","hesapSahibi":"X Ltd.","iban":"TR200000000000000000000001","paraBirimi":"TRY","bakiye":0,"aktif":true}]}""",

                // ── emanet ───────────────────────────────────────────────────
                var v when v.Contains("/emanet/") =>
                    """{"cariId":"c1","mevcutHasGramBakiye":11.1752, "hareketler":[{"id":"eh1","tur":"EMANET_GIRIS","tarih":"2026-09-16T10:00:00Z","hasGrami":11.1752,"aciklama":"Bilezik","stornoKaynakId":null,"fisId":null}, {"id":"eh2","tur":"EMANET_CIKIS","tarih":"2026-09-16T12:00:00Z","hasGrami":-4.58,"aciklama":"İade","stornoKaynakId":null,"fisId":"fis1"}]}""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz"}]""",

                // ── sayım ────────────────────────────────────────────────────
                var v when v.Contains("/sayim/") =>
                    """{"id":"o1","ad":"Eylül sayımı","subeId":"s1","kapsamTur":"TUMU","durum":"ACIK", "toplamSatir":2,"sayilan":1,"sayilmayan":1,"farkli":1,"eksikAdet":2,"fazlaAdet":0,"netFarkHas":-11.65,"hareketli":0, "satirlar":[{"urunId":"u1","kod":"GMS-925-001","ad":"Gümüş Kolye","barkod":"QA2-GMS-925-001","birim":"ADET","sistemAdet":8,"sayilanAdet":6,"guncelAdet":8,"fark":-2,"oturumdaHareketVar":false}, {"urunId":"u2","kod":"KL-1","ad":"Kolye KL1","barkod":null,"birim":"ADET","sistemAdet":1,"sayilanAdet":null,"guncelAdet":1,"fark":null,"oturumdaHareketVar":false}]}""",
                var v when v.Contains("/sayim") && istek.Method == HttpMethod.Get =>
                    """[{"id":"o1","ad":"Eylül sayımı","kapsamTur":"TUMU","durum":"ACIK"}]""",

                // ── faturalar ────────────────────────────────────────────────
                var v when v.Contains("/faturalar/faturalanabilir-islemler") => "[]",
                var v when v.Contains("/faturalar/") && v.Contains("/kalemler") =>
                    """{"faturaId":"f1","faturaNo":"FTR-2026-000002","kalemKaynagi":"KALEM","hasDegerKaynagi":"ELLE","toplam":2500, "kalemler":[{"id":"fk1","sira":1,"aciklama":"Gümüş kolye","miktar":1,"birim":"ADET","ayarId":null,"milyem":null,"gramAgirlik":null,"hasGrami":null,"tasIbaresi":null,"birimFiyat":2500,"iscilikTl":null,"indirimTutari":null,"kdvTuru":"STANDART","kdvOran":0.2,"hasDeger":0,"kdvMatrah":2083.33,"kdvTutar":416.67,"satirToplam":2500}]}""",
                var v when v.Contains("/faturalar") && istek.Method == HttpMethod.Get =>
                    """{"faturalar":[{"id":"f1","faturaNo":"FTR-2026-000002","cariAd":"Ayşe Yılmaz","tur":"SATIS","araToplam":2083.33,"kdvOran":0.2,"kdvTutar":416.67,"toplam":2500,"durum":"TASLAK","bankaHareketId":null,"tarih":"2026-09-16T10:00:00Z","kalemKaynagi":"KALEM","hasDegerKaynagi":"ELLE"}, {"id":"f2","faturaNo":"FTR-2026-000003","cariAd":"Ayşe Yılmaz","tur":"SATIS","araToplam":100,"kdvOran":0.2,"kdvTutar":20,"toplam":120,"durum":"ODENDI","bankaHareketId":null,"tarih":"2026-09-16T11:00:00Z","kalemKaynagi":"BASLIK","hasDegerKaynagi":"ELLE"}]}""",

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

    /// <summary>Gerekçe kutusu: DÜĞME İÇERMEZ, metni BOŞ DEĞİLDİR.</summary>
    private static void GerekceKutusu(IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> _) { }

    private void DegismezDogrula(string id, string markup, string metin)
    {
        Assert.Contains(id, markup);
        Assert.False(string.IsNullOrWhiteSpace(metin));
        // İç paket/dalga numarası KULLANICIYA gösterilmez (48-A doğrulayıcı devri).
        Assert.DoesNotContain("paket", metin, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dalga", metin, StringComparison.OrdinalIgnoreCase);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 1) FİYATLANDIRMA
    // ════════════════════════════════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Fiyatlandirma> Fiyat()
    {
        var e = Render<Kuyumcu.Web.Pages.Fiyatlandirma>().AyrintiAc();
        e.WaitForElement("#fl-tablo tbody tr");
        istekler.Clear();
        return e;
    }

    [Fact]
    public void Fiyatlandirma_SAYFA_BASI_vardir_ve_basligi_bos_degildir()
    {
        var e = Fiyat();
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#sayfa-basi-fiyatlandirma-baslik").TextContent));
        Assert.NotNull(e.Find("#sayfa-basi-fiyatlandirma-eylem #fl-yeni"));
    }

    [Fact]
    public void Fiyatlandirma_bos_adla_kaydet_ISTEK_ATMAZ_ve_ALAN_ALTI_mesaj_verir()
    {
        var e = Fiyat();
        e.Find("#fl-yeni").Click();
        e.WaitForElement("#fl-form");
        e.Find("#fl-kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-fl-ad").TextContent));
    }

    [Fact]
    public void Fiyatlandirma_kampanya_bitis_baslangictan_ONCEYSE_alan_alti_mesaj_cikar()
    {
        var e = Fiyat();
        e.Find("#km-yeni").Click();
        e.WaitForElement("#km-form");
        e.Find("#km-ad").Input("Ters aralık");
        e.Find("#km-deger").Input("10");
        e.Find("#km-bas").Change("2026-10-01");
        e.Find("#km-bit").Change("2026-09-01");
        e.Find("#km-kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        // PAKET GOC-C: bitiş kutusu ortak Tarih (EnErken = başlangıç) — ters aralık YAZILIRKEN alan altında ortak sözlük
        // metniyle söylenir ("Tarih 01.10.2026 tarihinden önce olamaz."); sayfanın eski cümlesi yerine (K8e, tek kaynak).
        Assert.Contains(Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(
            Kuyumcu.Core.Domain.Girdi.GirdiKodu.TarihErken, Gun: new DateOnly(2026, 10, 1)), "tr"), e.Find("#alan-hata-km-bit").TextContent);
    }

    [Fact]
    public void Fiyatlandirma_tabloda_HAM_ENUM_basilmaz()
    {
        var e = Fiyat();
        var tablo = e.Find("#fl-tablo").TextContent + e.Find("#km-tablo").TextContent;
        Assert.DoesNotContain("PERAKENDE", tablo);
        Assert.DoesNotContain("TOPTAN", tablo);
        Assert.DoesNotContain("ISCILIK_YUZDE", tablo);
        Assert.DoesNotContain("TUMU", tablo);
    }

    [Fact]
    public void Fiyatlandirma_pasife_alinca_KENDI_mesajini_yazar()
    {
        var e = Fiyat();
        e.Find("#fl-durum-fl2").Click();
        // PAKET P-65 (E49): pasife alma ORTAK ONAY KİPİNDEN geçer.
        e.Find("#fl-pasif-onayla").Click();
        e.WaitForAssertion(() =>
        {
            Assert.Single(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/fiyat/listeler/fl2"));
            Assert.Contains("pasife alındı", e.Find("#fl-basari").TextContent, StringComparison.OrdinalIgnoreCase);
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // 2) KASALAR
    // ════════════════════════════════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Kasalar> Kasa()
    {
        var e = Render<Kuyumcu.Web.Pages.Kasalar>().AyrintiAc();
        e.WaitForElement("#kasa-gecmis-k1");
        istekler.Clear();
        return e;
    }

    [Fact]
    public void Kasalar_SAYFA_BASI_vardir_ve_basligi_bos_degildir()
    {
        var e = Kasa();
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#sayfa-basi-kasalar-baslik").TextContent));
        Assert.NotNull(e.Find("#sayfa-basi-kasalar-eylem #kasa-yeni"));
    }

    [Fact]
    public void Kasalar_duzenleme_CEKMECEDE_acilir_ve_PATCH_govdesi_PARA_BIRIMI_tasir()
    {
        var e = Kasa();
        e.FindAll(".kasa-duzenle-btn")[0].Click();
        e.WaitForElement("#cekmece-kasa");

        Assert.NotNull(e.Find("#d-ad"));
        Assert.NotNull(e.Find("#d-doviz"));
        Assert.NotNull(e.Find("#d-sube"));

        SeciciYardim.Sec(e, "d-doviz", "USD");
        e.Find("#cekmece-kasa-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/kasalar/k1"));
            Assert.Equal("USD", Alan(istek.Govde, "paraBirimi"));
            Assert.Null(Alan(istek.Govde, "ad"));       // DEĞİŞMEYEN alan gövdeye KONMAZ
        });
    }

    [Fact]
    public void Kasalar_pasif_kasada_AKTIFE_AL_dugmesi_PATCH_aktif_true_gonderir()
    {
        var e = Kasa();
        e.Find("#kasa-aktife-al-k2").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/kasalar/k2"));
            Assert.Equal("True", Alan(istek.Govde, "aktif"));
        });
    }

    [Fact]
    public void Kasalar_pasif_kasada_HAREKET_dugmesi_KILITLI_ve_sebebi_yazar()
    {
        var e = Kasa();
        Assert.True(e.Find("#kasa-hareket-k2").HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#kasa-pasif-not-k2").TextContent));
    }

    [Fact]
    public void Kasalar_tutar_SIFIRKEN_hareket_ISTEK_ATMAZ_ve_alan_alti_mesaj_verir()
    {
        var e = Kasa();
        e.Find("#kasa-hareket-k1").Click();
        e.WaitForElement("#h-tutar");
        e.Find("#h-kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-h-tutar").TextContent));
    }

    [Fact]
    public void Kasalar_ELLE_girilen_hareket_ARTIK_STORNOLANABILIR_ve_KENDI_ucuna_POST_atar()
    {
        // ⚠ PAKET S-3b: bu test eskiden düğmenin KİLİTLİ olmasını istiyordu.
        // Kurucunun "her veri girişi düzeltilebilir" şartı ve kurul tur 3
        // engel 1 gereği kilit KALKTI; sözleşme tersine döndü.
        var e = Kasa();
        e.Find("#kasa-gecmis-k1").Click();
        e.WaitForElement("#kh-storno-kh2");

        Assert.False(e.Find("#kh-storno-kh2").HasAttribute("disabled"));
        e.Find("#kh-storno-kh2").Click();
        e.Find("#kh-storno-gerekce").Input("Yanlış yönde girildi, düzeltiliyor");
        e.Find("#kh-storno-onayla").Click();

        var post = istekler.Last(x => x.Metot == "POST");
        Assert.Contains("/kasalar/k1/hareket/kh2/storno", post.Yol);
        // Elle hareketin kur bacağı YOKTUR: gövde kur kimliği TAŞIMAZ.
        Assert.Null(Alan(post.Govde, "kurSnapshotId"));
        Assert.Equal("Yanlış yönde girildi, düzeltiliyor", Alan(post.Govde, "gerekce"));
    }

    [Fact]
    public void Kasalar_hareket_modali_her_acilista_YONU_SIFIRLAR()
    {
        // QA turu 3 (YÜKSEK): modal önceki kasadaki "Gider" seçimini hatırlıyor,
        // yeni kasada yalnız tutar girilince kayıt YANLIŞ YÖNDE oluşuyordu.
        var e = Kasa();
        e.Find("#kasa-hareket-k1").Click();
        e.WaitForElement("#h-tur");
        SeciciYardim.Sec(e, "h-tur", "CIKIS");
        e.Find("#h-tutar").Input("100");

        // Modalı kapat, yeniden aç: yön VARSAYILANA döner.
        // TEMA TEMİZLİĞİ 17.10: Yıldız katmanında çekmecenin ikincil düğmesi "Vazgeç"tir (YildizKatmanSozlugu) — kimliğiyle bulunur.
        e.Find("#cekmece-kasa-hareket-iptal").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#h-tur")));
        e.Find("#kasa-hareket-k1").Click();
        e.WaitForElement("#h-tur");

        Assert.Equal("GIRIS", e.Find("#h-tur").Deger());
    }

    [Fact]
    public void Kasalar_gecmisinde_ELLE_hareketin_ACIKLAMASI_gorunur()
    {
        // QA turu 3: girilen açıklama hiçbir ekranda geri okunamıyordu.
        var e = Kasa();
        e.Find("#kasa-gecmis-k1").Click();
        e.WaitForElement("#kh-aciklama-kh2");
        Assert.Contains("Dükkân kirası", e.Find("#kh-aciklama-kh2").TextContent);
    }

    [Fact]
    public void Kasalar_DEFTER_BAGLI_hareket_satirinda_STORNO_ACIK_ve_POST_atar()
    {
        sahte.DefterBagliKasaHareketi = true;
        var e = Kasa();
        e.Find("#kasa-gecmis-k1").Click();
        e.WaitForElement("#kh-storno-kh1");

        Assert.False(e.Find("#kh-storno-kh1").HasAttribute("disabled"));
        e.Find("#kh-storno-kh1").Click();
        e.WaitForElement("#kh-storno-gerekce");

        // Gerekçe 20 karakterden kısayken onay KİLİTLİ ve sebebi yazar.
        e.Find("#kh-storno-gerekce").Input("kısa");
        Assert.True(e.Find("#kh-storno-onayla").HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#kh-storno-kilit").TextContent));

        e.Find("#kh-storno-gerekce").Input("Kasaya yanlış tutar girildi, ters kayıtla düzeltiliyor.");
        e.Find("#kh-storno-onayla").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/h1/storno"));
            Assert.Equal("kur1", Alan(istek.Govde, "kurSnapshotId"));
            Assert.Equal("k1", Alan(istek.Govde, "kasaId"));
            Assert.True(Alan(istek.Govde, "gerekce")!.Length >= 20);
        });
    }

    [Fact]
    public void Kasalar_KUR_YOKKEN_storno_KILITLI_kalir_ve_sebebi_KUR_YOKLUGUDUR()
    {
        sahte.DefterBagliKasaHareketi = true;
        sahte.KurYok = true;
        var e = Kasa();
        e.Find("#kasa-gecmis-k1").Click();
        e.WaitForElement("#kh-storno-kh1");

        Assert.True(e.Find("#kh-storno-kh1").HasAttribute("disabled"));
        Assert.Contains("kur", e.Find("#degismez-ipucu-KasaHareketi").TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Kasalar_DOVIZ_kasasinin_bakiyesi_TL_simgesiyle_BASILMAZ()
    {
        var e = Kasa();
        e.Find("#kasa-gecmis-k1").Click();
        e.WaitForElement("#kh-storno-kh2");
        // EUR kasası: "₺" görünmemeli (UX-S32).
        Assert.DoesNotContain(Bicim.Tl(150m), e.Markup);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 3) BANKA
    // ════════════════════════════════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Banka> BankaSayfasi()
    {
        var e = Render<Kuyumcu.Web.Pages.Banka>().AyrintiAc();
        e.WaitForElement("#bh-dekont-gir");
        e.HareketleriAc();   // ZO-ISLEM-A: hareketler kapalı akordeonda
        istekler.Clear();
        return e;
    }

    [Fact]
    public void Banka_SAYFA_BASI_vardir_ve_basligi_bos_degildir()
    {
        var e = BankaSayfasi();
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#sayfa-basi-banka-baslik").TextContent));
        Assert.NotNull(e.Find("#sayfa-basi-banka-eylem #banka-yeni"));
    }

    [Fact]
    public void Banka_hesap_duzenleme_CEKMECEDE_acilir_ve_PATCH_atar()
    {
        var e = BankaSayfasi();
        e.Find("#bh-duzenle-b1").Click();
        e.WaitForElement("#cekmece-banka-hesap");

        e.Find("#d-hesapad").Input("Vadesiz Hesap");
        e.Find("#cekmece-banka-hesap-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/bankalar/b1"));
            Assert.Equal("Vadesiz Hesap", Alan(istek.Govde, "hesapAdi"));
        });
    }

    [Fact]
    public void Banka_hesap_pasife_alma_ONAY_ister_ve_DELETE_gonderir()
    {
        var e = BankaSayfasi();
        e.Find("#bh-pasife-al-b1").Click();
        e.WaitForElement("#bh-pasif-onayla");

        Assert.DoesNotContain(istekler, i => i.Metot == "DELETE");   // onaysız istek YOK
        e.Find("#bh-pasif-onayla").Click();

        e.WaitForAssertion(() =>
            Assert.Single(istekler, i => i.Metot == "DELETE" && i.Yol.EndsWith("/bankalar/b1")));
    }

    [Fact]
    public void Banka_dekont_satirinda_STORNO_dugmesi_ACIK_ve_finansal_kilit_YAZILI()
    {
        // ⚠ SÖZLEŞME DEĞİŞTİ — PAKET P-12 (B-42).
        // UX-1-D'de bu test "storno düğmesi HİÇ ÇİZİLMEZ"i koruyordu ve o gün
        // DOĞRUYDU: sunucuda banka dekontu için ters kayıt yolu YOKTU, kalıcı
        // disabled bir düğme "bir yolu var ama sana kapalı" derdi.
        // P-12 o yolu AÇTI (StornoServisi.BankaHareketiStorno, tek motor), bu
        // yüzden testin koruduğu kural da yer değiştirdi: artık GERÇEK düğme
        // beklenir ve kilitli kalan ŞEY (tutar/yön gibi finansal alanlar)
        // gerekçesiyle ekranda yazar.
        var e = BankaSayfasi();

        var dugme = e.Find("#bh-storno-bh1");
        Assert.False(dugme.HasAttribute("disabled"));   // yol gerçekten AÇIK

        // UX-S25/S30: finansal alanın neden düzeltilemediği ve doğru yolu yazılı.
        var ipucu = e.Find("#degismez-ipucu-BankaHareketiFinansalAlan");
        Assert.Empty(ipucu.QuerySelectorAll("button"));
        DegismezDogrula("degismez-ipucu-BankaHareketiFinansalAlan", e.Markup, ipucu.TextContent);
    }

    [Fact]
    public void Banka_negatif_dekont_ISTEK_ATMAZ_ve_alan_alti_mesaj_verir()
    {
        var e = BankaSayfasi();
        e.Find("#bh-dekont-gir").Click();
        e.WaitForElement("#dekont-tutar");
        e.Find("#dekont-tutar").Input("-50");
        e.Find("#dekont-kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-dekont-tutar").TextContent));
    }

    // ════════════════════════════════════════════════════════════════════════
    // 4) KUR
    // ════════════════════════════════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Kur> KurSayfasi()
    {
        var e = Render<Kuyumcu.Web.Pages.Kur>().AyrintiAc();
        e.WaitForElement("#sayfa-basi-kur-baslik");
        istekler.Clear();
        return e;
    }

    [Fact]
    public void Kur_SAYFA_BASI_vardir_ve_TEK_birincil_eylem_tasir()
    {
        var e = KurSayfasi();
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#sayfa-basi-kur-baslik").TextContent));
        Assert.NotNull(e.Find("#sayfa-basi-kur-eylem #kur-simdi-cek"));
    }

    [Fact]
    public void Kur_MANUEL_kaydi_ARTIK_gerekceyle_IPTAL_edilebilir()
    {
        // ⚠ PAKET S-3b: eskiden "hiç düzeltilemez" doğruydu; artık MANUAL kayıt
        // gerekçeli olarak geçersiz kılınabiliyor (kurul tur 3 / engel 1).
        var e = KurSayfasi();
        e.WaitForElement("#kur-iptal-kur1");

        Assert.False(e.Find("#kur-iptal-kur1").HasAttribute("disabled"));
        e.Find("#kur-iptal-kur1").Click();
        e.Find("#kur-iptal-gerekce").Input("Virgül hatası: 6.800 yerine 68.000 girildi");
        e.Find("#kur-iptal-onayla").Click();

        var post = istekler.Last(x => x.Metot == "POST");
        Assert.Contains("/kur/manuel/kur1/iptal", post.Yol);
        Assert.Equal("Virgül hatası: 6.800 yerine 68.000 girildi", Alan(post.Govde, "gerekce"));
    }

    [Fact]
    public void Kur_manuel_HAS_SIFIRKEN_istek_ATILMAZ_ve_alan_alti_mesaj_cikar()
    {
        var e = KurSayfasi();
        e.Find("#kur-manuel-ac").Click();
        e.WaitForElement("#m-has");
        e.Find("#m-has").Input("0");
        e.Find("#m-kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-m-has").TextContent));
    }

    [Fact]
    public void Kur_manuel_kayit_POST_sonrasi_KENDI_basari_mesajini_yazar()
    {
        var e = KurSayfasi();
        e.Find("#kur-manuel-ac").Click();
        e.WaitForElement("#m-has");
        // PAKET KUR-BANT (09.10): sahte güncel has 4.100 → 6.800 (+%66) artık makul bant dışı (POST yok);
        // bu testin konusu başarı bandı olduğundan bant içi bir değer yazılır.
        e.Find("#m-has").Input("4500");
        e.Find("#m-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/kur/manuel"));
            Assert.Contains("Manuel kur", e.Find("#basari-bandi").TextContent);
        });
    }

    // ═══ ORTAK-1 · KEŞİF #4 (30.09, KRİTİK) — manuel has fiyatı tuş tuş "1.500" ═════════════════
    // Kapısız: "1.5" ara değeri kalıyor; kutu hata yazarken Kaydet has fiyatını 1,5 TL diye yazabilirdi.
    [Fact]
    public void OK1_K4_kur_manuel_has_belirsiz_1_500_POST_YOK_sebep_yazar()
    {
        var e = KurSayfasi();
        e.Find("#kur-manuel-ac").Click();
        e.WaitForElement("#m-has");
        foreach (var a in new[] { "1", "1.", "1.5", "1.50", "1.500" }) e.Find("#m-has").Input(a);
        e.Find("#m-has").Blur();
        Assert.Equal("1.500", e.Find("#m-has").GetAttribute("value"));
        Assert.Equal("true", e.Find("#m-has").GetAttribute("aria-invalid"));
        Assert.True(e.Find("#m-kaydet").HasAttribute("disabled"));
        Assert.Contains(e.Find("label[for='m-has']").TextContent.Replace("*", "").Trim(), e.Find("#m-sayi-kilit").TextContent);
        istekler.Clear();
        e.Find("#m-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        e.Find("#m-has").Input("6800");
        Assert.False(e.Find("#m-kaydet").HasAttribute("disabled"));
    }

    // ════════════════════════════════════════════════════════════════════════
    // 5) EMANET
    // ════════════════════════════════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Emanet> EmanetSayfasi()
    {
        var e = Render<Kuyumcu.Web.Pages.Emanet>().EmanetOzgunAc();
        e.WaitForElement("#e-cari");
        SeciciYardim.Sec(e, "e-cari", "c1");
        e.WaitForElement("#e-gecmis-ac").Click();   // EMANET-1: geçmiş katlanırda KAPALI
        e.WaitForElement("#eh-storno-eh1");
        istekler.Clear();
        return e;
    }

    [Fact]
    public void Emanet_SAYFA_BASI_vardir_ve_basligi_bos_degildir()
    {
        var e = EmanetSayfasi();
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#sayfa-basi-emanet-baslik").TextContent));
    }

    [Fact]
    public void Emanet_giris_hareketinde_STORNO_ACIK_ve_POST_atar()
    {
        var e = EmanetSayfasi();
        Assert.False(e.Find("#eh-storno-eh1").HasAttribute("disabled"));

        e.Find("#eh-storno-eh1").Click();
        e.WaitForElement("#eh-storno-gerekce");
        e.Find("#eh-storno-gerekce").Input("Emanet gramı yanlış girildi, ters kayıtla düzeltiliyor.");
        e.Find("#eh-storno-onayla").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/eh1/storno"));
            Assert.Equal("kur1", Alan(istek.Govde, "kurSnapshotId"));
            Assert.True(Alan(istek.Govde, "gerekce")!.Length >= 20);
        });
    }

    [Fact]
    public void Emanet_COK_BACAKLI_fis_satirinda_STORNO_KILITLI_ve_gerekce_VAR()
    {
        var e = EmanetSayfasi();
        Assert.True(e.Find("#eh-storno-eh2").HasAttribute("disabled"));
        // EMANET-1 tasarım hakemi 28.09 (KRİTİK): tablo altındaki kalıcı kutu kaldırıldı;
        // gerekçe (UX-S30) kilitli düğmenin kendi title'ında, satıra özgü tek cümle.
        Assert.Empty(e.FindAll("#degismez-ipucu-EmanetHareketi"));
        var gerekce = e.Find("#eh-storno-eh2").GetAttribute("title");
        Assert.Equal(Kuyumcu.Core.I18n.EmanetSadeSozlugu.Tr.NedenFis, gerekce);
        Assert.DoesNotContain("paket", gerekce!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Emanet_gram_SIFIRKEN_istek_ATILMAZ_ve_alan_alti_mesaj_cikar()
    {
        var e = EmanetSayfasi();
        e.Find("#e-giris-ac").Click();
        e.WaitForElement("#e-gram");
        e.Find("#e-giris-kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-e-gram").TextContent));
    }

    // ════════════════════════════════════════════════════════════════════════
    // 6) SAYIM
    // ════════════════════════════════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Sayim> SayimListesi()
    {
        var e = Render<Kuyumcu.Web.Pages.Sayim>().SayimOzgunListe();
        e.WaitForElement("#sy-tablo tbody tr");
        istekler.Clear();
        return e;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Sayim> SayimOturumu()
    {
        var e = SayimListesi();
        e.Find("#sy-ac-o1").Click();
        e.WaitForElement("#sy-satirlar tbody tr");
        istekler.Clear();
        return e;
    }

    [Fact]
    public void Sayim_SAYFA_BASI_vardir_ve_basligi_bos_degildir()
    {
        var e = SayimListesi();
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#sayfa-basi-sayim-baslik").TextContent));
        Assert.NotNull(e.Find("#sayfa-basi-sayim-eylem #sy-yeni"));
    }

    [Fact]
    public void Sayim_oturumu_DUZENLENEMEZ_dugme_YOK_gerekce_VAR()
    {
        var e = SayimListesi();
        var ipucu = e.Find("#degismez-ipucu-SayimOturumu");
        Assert.Empty(ipucu.QuerySelectorAll("button"));
        DegismezDogrula("degismez-ipucu-SayimOturumu", e.Markup, ipucu.TextContent);
    }

    [Fact]
    public void Sayim_bos_adla_oturum_acma_ISTEK_ATMAZ_ve_alan_alti_mesaj_verir()
    {
        var e = SayimListesi();
        e.Find("#sy-yeni").Click();
        e.WaitForElement("#sy-form");
        // PAKET P-SAYIM (Y10): ad artık tarih-saatle otomatik dolu; boş ad kapısı kullanıcı adı silince ölçülür.
        e.Find("#sy-ad").Input("");
        e.Find("#sy-ac").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-sy-ad").TextContent));
    }

    [Fact]
    public void Sayim_URUN_KODUYLA_arama_govdede_urunId_gonderir_barkod_UYDURMAZ()
    {
        var e = SayimOturumu();
        e.Find("#sy-barkod").Input("GMS-925-001");   // PAKET GOC-C: ortak Arama     // barkod DEĞİL, ürün KODU
        e.Find("#sy-adet").Input("6");   // PAKET GOC-C: ortak Sayi
        e.Find("#sy-say").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("/sayim/o1/say"));
            Assert.Equal("u1", Alan(istek.Govde, "urunId"));
            Assert.Null(Alan(istek.Govde, "barkod"));
        });
    }

    [Fact]
    public void Sayim_satir_ici_SAYILAN_duzenlemesi_ayni_uca_urunId_ile_gider()
    {
        var e = SayimOturumu();
        e.Find("#si-sayim-sayilan-u2-ac").Click();
        e.WaitForElement("#si-sayim-sayilan-u2");
        e.Find("#si-sayim-sayilan-u2").Input("3");
        e.Find("#si-sayim-sayilan-u2-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("/sayim/o1/say"));
            Assert.Equal("u2", Alan(istek.Govde, "urunId"));
            Assert.Equal("3", Alan(istek.Govde, "sayilanAdet"));
        });
    }

    // ── PAKET P-78 — sayım satır içi SAYILAN tr-TR ("1,5" eskiden 15 okunuyordu) ──
    [Fact]
    public void P78_Sayim_satir_ici_1_5_TURKCE_okunur_belirsiz_ISTEK_ATMAZ()
    {
        var e = SayimOturumu();
        e.Find("#si-sayim-sayilan-u2-ac").Click();
        e.WaitForElement("#si-sayim-sayilan-u2");
        e.Find("#si-sayim-sayilan-u2").Input("2.500");
        e.Find("#si-sayim-sayilan-u2-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("/sayim/o1/say"));
        e.WaitForAssertion(() => Assert.Contains(Kuyumcu.Core.I18n.SayiGirisiSozlugu.Tr.Belirsiz, e.Markup));

        e.Find("#si-sayim-sayilan-u2-ac").Click();
        e.WaitForElement("#si-sayim-sayilan-u2");
        e.Find("#si-sayim-sayilan-u2").Input("1,5");
        e.Find("#si-sayim-sayilan-u2-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("/sayim/o1/say"));
            Assert.Equal("1.5", Alan(istek.Govde, "sayilanAdet"));
        });
    }

    [Fact]
    public void Sayim_satiri_SAYILMADIYA_geri_alinamaz_gerekce_VAR()
    {
        var e = SayimOturumu();
        var ipucu = e.Find("#degismez-ipucu-SayimSatiri");
        Assert.Empty(ipucu.QuerySelectorAll("button"));
        DegismezDogrula("degismez-ipucu-SayimSatiri", e.Markup, ipucu.TextContent);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 7) FATURALAR
    // ════════════════════════════════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> Fatura()
    {
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-kes-f1");
        istekler.Clear();
        return e;
    }

    [Fact]
    public void Faturalar_SAYFA_BASI_vardir_ve_filtre_yuvasi_dolu()
    {
        var e = Fatura();
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#sayfa-basi-faturalar-baslik").TextContent));
        // ZO-BELGE: durum süzgeci filtre yuvasından KAPALI "Süzgeç" katlanırına taşındı
        // (kurucu 25.09 "zorunlu önce"); açılınca ortak Form/Secim olarak durur.
        Assert.Empty(e.FindAll("#sayfa-basi-faturalar-filtre #fk-durum-filtre"));
        e.Find("#fk-suzgec-ac").Click();
        Assert.NotNull(e.Find("#fk-suzgec #fk-durum-filtre"));
        Assert.NotNull(e.Find("#sayfa-basi-faturalar-eylem #fk-yeni"));
    }

    [Fact]
    public void Faturalar_KES_dugmesi_ONAYSIZ_istek_ATMAZ()
    {
        var e = Fatura();
        e.Find("#fk-kes-f1").Click();
        e.WaitForElement("#fk-durum-onayla");

        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
        e.Find("#fk-durum-onayla").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "PATCH" && i.Yol.Contains("/faturalar/f1/durum"));
            Assert.Equal("KESILDI", Alan(istek.Govde, "durum"));
        });
    }

    [Fact]
    public void Faturalar_ODENDI_faturada_IPTAL_ET_yolu_ACIKTIR()
    {
        var e = Fatura();
        Assert.NotNull(e.Find("#fk-iptal-f2"));      // ODENDI → IPTAL sunucuda izinli
        e.Find("#fk-iptal-f2").Click();
        e.WaitForElement("#fk-durum-onayla");
        e.Find("#fk-durum-onayla").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "PATCH" && i.Yol.Contains("/faturalar/f2/durum"));
            Assert.Equal("IPTAL", Alan(istek.Govde, "durum"));
        });
    }

    [Fact]
    public void Faturalar_TASLAK_duzenlenemez_dugme_YOK_gerekce_VAR()
    {
        var e = Fatura();
        // ZO-BELGE: "değiştirilemez" gerekçesi liste açılışında değil, KESİLMİŞ
        // fatura açılınca (kayıt açılınca) görünür.
        Assert.Empty(e.FindAll("#degismez-ipucu-Fatura"));
        e.Find("#fk-goster-1").Click();   // f2 = ODENDI (kesilmiş) — sahte listede 2. satır
        e.WaitForElement("#degismez-ipucu-Fatura");
        var ipucu = e.Find("#degismez-ipucu-Fatura");
        Assert.Empty(ipucu.QuerySelectorAll("button"));
        DegismezDogrula("degismez-ipucu-Fatura", e.Markup, ipucu.TextContent);
    }

    [Fact]
    public void Faturalar_cari_SECILMEDEN_kaydet_ISTEK_ATMAZ_ve_alan_alti_mesaj_verir()
    {
        var e = Fatura();
        e.Find("#fk-yeni").Click();
        e.WaitForElement("#fatura-cari");
        e.Find("#fk-kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-fatura-cari").TextContent));
    }

    [Fact]
    public void Faturalar_KUYUM_secilince_kilidin_SEBEBI_ekranda_yazar()
    {
        var e = Fatura();
        e.Find("#fk-yeni").Click();
        e.WaitForElement("#fatura-kdvturu");
        RadyoSec(e, "fatura-kdvturu", "KUYUM");

        e.WaitForAssertion(() =>
        {
            Assert.True(e.Find("#fk-kaydet").HasAttribute("disabled"));
            Assert.Contains("has bedeli", e.Find("#fk-kaydet-kilit").TextContent, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    // ⚠ SÖZLEŞME DEĞİŞTİ — PAKET P-22 / B-3: TASLAK faturanın KALEMİ artık
    //   düzenlenebilir (satır içi). "Değişmez" ipucu YALNIZ kesilmiş belgede
    //   çizilir. Test, yeni gerçeği ölçer: taslakta DÜZENLEME YOLU VARDIR ve
    //   ipucu YOKTUR (ikisi aynı anda bulunamaz — UX-S25).
    public void Faturalar_kalem_modalinda_TASLAKTA_duzenleme_VAR_ipucu_YOK()
    {
        var e = Fatura();
        e.Find("#fk-goster-0").Click();
        e.WaitForElement("#fd-kalem-tablosu");

        Assert.Empty(e.FindAll("#degismez-ipucu-FaturaKalemi"));
        Assert.NotEmpty(e.FindAll("[data-duzenlenebilir='true']"));
    }
}
