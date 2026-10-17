using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET SADE-1 — göz incelemesi artıkları (ham/GOZ-INCELEME-2026-09-25.json)
//   #11/#13 çift başlık · #14 Kurulum sonsuz "Yükleniyor" · #15 adım satırı ·
//   #23/#24 Hurda boş kart + kilitsiz form · #25/#31 Kur hukuki paragraf + ayar
//   hapı · #26 POS tahsilat kopya boş durum · #38 Yetkiler tekrar eden bloklar ·
//   #41 Ayarlar gri hata · #63/#64 POS jargon + kırmızı düğme · #73 Sayım durum ·
//   #18 Kılavuz uzun liste.
//
// ÖLÇÜLEN ŞEY "metin var mı" DEĞİL: her test bulgunun KÖK NEDENİNİ sınar ve
// negatif iddia pozitif eşiyle çiftlenir (ör. "yükleniyor yok" + "bant var").
// Varsayılan sahte yanıt 404'tür (ApiHata): tanımlanmamış uç "{}" ile sessizce
// başarılı sayılmaz.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SADE1EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol)> istekler = [];

    /// <summary>Yol parçası → (durum kodu, gövde). İlk eşleşen kazanır.</summary>
    private readonly List<(string Parca, HttpStatusCode Kod, string Govde)> yollar = [];

    /// <summary>Bu parçayı içeren isteğe ağ hatası (HttpRequestException) fırlatılır.</summary>
    private string? agHatasiParcasi;

    /// <summary>Yolu bu sonekle BİTEN istek, bu görev tamamlanana dek bekler (istek sürüyor anı).</summary>
    private string? bekletSonek;
    private readonly TaskCompletionSource<HttpResponseMessage> bekleyen =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public SADE1EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<SurumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<KullaniciGorunumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<PosServisi>();
        Services.AddScoped<MenuTercihServisi>();
    }

    private sealed class SahteApi(SADE1EkranTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            t.istekler.Add((istek.Method.Method, yol));
            if (t.agHatasiParcasi is { } a && yol.Contains(a))
                throw new HttpRequestException("ağ bağlantısı koptu");
            if (t.bekletSonek is { } bs && istek.RequestUri.AbsolutePath.EndsWith(bs))
                return t.bekleyen.Task;
            foreach (var (p, kod, govde) in t.yollar)
                if (yol.Contains(p))
                    return Task.FromResult(Yanit(kod, govde));
            return Task.FromResult(Yanit(HttpStatusCode.NotFound, """{"message":"tanımsız uç"}"""));
        }

        private static HttpResponseMessage Yanit(HttpStatusCode kod, string govde) => new(kod)
        {
            Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    internal static HttpResponseMessage HataYaniti(HttpStatusCode kod, string govde) => new(kod)
    {
        Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
    };

    private void Yol(string parca, string govde, HttpStatusCode kod = HttpStatusCode.OK) =>
        yollar.Add((parca, kod, govde));

    private void Oturum(string rol = "SAHIP", string? sube = "s1", string mod = "PROFESYONEL") =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, sube, null, mod));

    private void Basit() => Services.GetRequiredService<DilServisi>().BasitModuBagla(() => true);

    // ── #11 / #13 — çift başlık ──────────────────────────────────────────────

    [Fact]
    public void Veri_disa_aktarim_basligi_TEK_kez_yazilir_sayfada_h1_yok()
    {
        Oturum();
        var c = Render<Kuyumcu.Web.Pages.VeriDisaAktarim>();
        var baslik = Sozlukler.Tr.DisaAktarim.Baslik;
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#sayfa-basi-veri-disa-aktarim-baslik")));
        // Pozitif eş: şerit başlığı gerçekten bu metni taşır.
        Assert.Equal(baslik, c.Find("#sayfa-basi-veri-disa-aktarim-baslik").TextContent.Trim());
        // Negatif: sayfanın KENDİ <h1>'i yok (tek <h1> üst barda), başlık metni bir kez.
        Assert.Empty(c.FindAll("h1"));
        Assert.Single(c.FindAll("h1, h2, h3, .sayfa-basi-baslik"), e => e.TextContent.Trim() == baslik);
        Assert.Empty(c.FindAll(".sayfa-baslik"));
    }

    [Fact]
    public void Yedekler_basligi_TEK_kez_yazilir_sayfada_h1_yok()
    {
        Oturum();
        var c = Render<Kuyumcu.Web.Pages.Yedekler>();
        var baslik = Sozlukler.Tr.Yedek.Baslik;
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#sayfa-basi-yedekler-baslik")));
        Assert.Equal(baslik, c.Find("#sayfa-basi-yedekler-baslik").TextContent.Trim());
        Assert.Empty(c.FindAll("h1"));
        Assert.Single(c.FindAll("h1, h2, h3, .sayfa-basi-baslik"), e => e.TextContent.Trim() == baslik);
    }

    // ── #14 — Kurulum sonsuz "Yükleniyor" ────────────────────────────────────

    private const string KurulumDurum = """
        {"muhurlendi":false,"muhurTarihi":null,"muhurleyenKullaniciId":null,"muhurGerekcesi":null,
         "acilisPenceresiBitis":"2026-10-17T00:00:00Z","acilisPenceresiAcik":true,
         "muhurlenebilir":true,"eksikAdimlar":[],"atlananAdimlar":[],
         "adimlar":[
           {"anahtar":"ISLETME","bitti":true,"zorunlu":true,"atlandi":false},
           {"anahtar":"KUR","bitti":true,"zorunlu":true,"atlandi":false},
           {"anahtar":"CARI_ACILIS","bitti":false,"zorunlu":false,"atlandi":false}],
         "sayilar":{"sube":1,"kullanici":1,"kur":15910,"kasa":1,"banka":0,
                    "acilisCari":0,"acilisKasa":0,"acilisBanka":0},
         "maksSatir":500,"gerekceAsgari":20,"isletmeAdi":"Altın Işık Kuyumcu"}
        """;

    private void KurulumYollari()
    {
        Yol("/kasalar", "[]");
        Yol("/bankalar", """{"hesaplar":[]}""");
    }

    [Fact]
    public void Kurulum_sunucu_hatasinda_SONSUZ_yukleniyor_yok_bant_ve_yeniden_dene_var()
    {
        Oturum();
        Yol("/kurulum/durum", """{"message":"Sunucu hatasi","statusCode":500}""", HttpStatusCode.InternalServerError);
        var c = Render<Kuyumcu.Web.Pages.Kurulum>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-hata")));
        Assert.Empty(c.FindAll("#kur-yukleniyor"));
        Assert.Contains("Sunucu hatasi", c.Find("#kur-hata").TextContent);
        // "Yeniden dene" GERÇEK istek atar ve başarılı yanıtta adımlar yüklenir.
        yollar.Clear();
        Yol("/kurulum/durum", KurulumDurum);
        KurulumYollari();
        var once = istekler.Count(i => i.Yol.Contains("/kurulum/durum"));
        c.Find("#kur-yeniden").Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-adimlar")));
        Assert.True(istekler.Count(i => i.Yol.Contains("/kurulum/durum")) > once);
        Assert.Empty(c.FindAll("#kur-yeniden"));
    }

    [Fact]
    public void Kurulum_AG_hatasinda_da_bant_cikar_yukleniyor_kalmaz()
    {
        Oturum();
        agHatasiParcasi = "/kurulum/durum";
        var c = Render<Kuyumcu.Web.Pages.Kurulum>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-hata")));
        Assert.Equal(SadeSozlugu.Tr.KurulumOkunamadi, c.Find("#kur-hata").TextContent.Trim());
        Assert.Empty(c.FindAll("#kur-yukleniyor"));
        Assert.NotNull(c.Find("#kur-yeniden"));
    }

    [Fact]
    public void Kurulum_adimlari_yuklenir_kart_basligi_sayfa_basligini_TEKRARLAMAZ()
    {
        Oturum();
        Yol("/kurulum/durum", KurulumDurum);
        KurulumYollari();
        var c = Render<Kuyumcu.Web.Pages.Kurulum>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-adim-ISLETME")));
        Assert.Empty(c.FindAll("#kur-yukleniyor"));
        var sayfaBasligi = c.Find("#sayfa-basi-kurulum-baslik").TextContent.Trim();
        Assert.Equal(SadeSozlugu.Tr.KurulumAdimlarBaslik, c.Find("#kur-adimlar-baslik").TextContent.Trim());
        Assert.Single(c.FindAll("h1, h2, h3, .sayfa-basi-baslik"), e => e.TextContent.Trim() == sayfaBasligi);
        // #15: satır ızgara sınıfını taşır (375 px'te yığılır; ölçüm tarayıcıda).
        Assert.Contains("sade-adim-listesi", c.Find("#kur-adimlar ul").ClassName);
    }

    // ── #23 / #24 — Hurda ────────────────────────────────────────────────────

    [Fact]
    public void Hurda_sube_baglami_yoksa_form_KILITLI_sebep_yazili_bakiye_karti_bos_degil()
    {
        Oturum("MUDUR", sube: null);
        Yol("/hurda/bakiye", """{"message":"Hurda işlemleri için şube bağlamı gerekli."}""", HttpStatusCode.BadRequest);
        var c = Render<Kuyumcu.Web.Pages.Hurda>().AyrintiAc();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#hrd-hata")));
        // #24: form kilitli (fieldset disabled → içindeki kaydet düğmesi de) ve NEDEN yazıyor.
        Assert.True(c.Find("#hrd-form-alan").HasAttribute("disabled"));
        Assert.True(c.Find("#hrd-kaydet").HasAttribute("disabled"));
        // ZO-ISLEM-A (orkestratör görüntüsü: aynı sebep 3 yerde + iç terim "şube bağlamı"):
        // sebep ve ne yapılacağı bantta TEK kez; form notu / bakiye kartı kopyalamaz.
        Assert.Contains(SadeSozlugu.Tr.HurdaSubeYok, c.Find("#hrd-hata").TextContent);
        Assert.Empty(c.FindAll("#hrd-form-kilit-notu"));
        Assert.Empty(c.FindAll("#hrd-bakiye-okunamadi"));
        Assert.DoesNotContain("şube bağlamı", c.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Hurda_veri_gelince_form_KILITSIZ_kilit_notu_yok()
    {
        Oturum();
        Yol("/hurda/bakiye", """{"subeId":"s1","satirlar":[],"fireKaydiAcik":false,"fireNotu":null}""");
        Yol("/hurda/defter", """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}""");
        Yol("/kasalar", """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":0,"aktif":true}]""");
        var c = Render<Kuyumcu.Web.Pages.Hurda>().AyrintiAc();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#hrd-form-alan")));
        c.WaitForAssertion(() => Assert.False(c.Find("#hrd-form-alan").HasAttribute("disabled")));
        Assert.Empty(c.FindAll("#hrd-form-kilit-notu"));
        Assert.Empty(c.FindAll("#hrd-bakiye-okunamadi"));
        // Veri yoksa boş durum metni (kart boş kalmaz).
        Assert.Contains(Sozlukler.Tr.Hurda.BakiyeYok, c.Markup);
    }

    // ── #25 / #31 — Kur ──────────────────────────────────────────────────────

    private void KurYollari()
    {
        Yol("/kur/kaynaklar", """{"kaynaklar":["ALTINKAYNAK"],"varsayilan":"ALTINKAYNAK","secili":"ALTINKAYNAK","sozlesmesizKaynaklarAcik":true,"seciliSozlesmesiz":true,"manuelKod":"MANUAL"}""");
        Yol("/kur/gecmis", "[]");
        Yol("/kur/pariteler", """{"kaynak":"ALTINKAYNAK","pariteler":[]}""");
        Yol("/kur/guncel", """
            {"id":"kur-9","altinHasFiyati":6800,"gramAltinFiyati":7100,"usdTry":34.2,
             "kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-14T09:00:00Z",
             "ayarFiyatlari":[{"ayarId":"K22","milyem":916,"gramFiyati":6228.8}]}
            """);
    }

    [Fact]
    public void Kur_sozlesmesiz_kaynak_gunluk_ekranda_TEK_satir_rozet_hukuki_paragraf_yok()
    {
        Oturum(mod: "BASIT");
        KurYollari();
        var c = Render<Kuyumcu.Web.Pages.Kur>().AyrintiAc();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-sozlesmesiz")));
        var satir = c.Find("#kur-sozlesmesiz");
        // TOPLU-3 (demo bulgusu): rozet dili "Deneme" yerine sade "Ücretsiz kur kaynağı" (Toplu3Sozlugu); koşul aynı.
        Assert.Equal(Kuyumcu.Core.I18n.Toplu3Sozlugu.Tr.KurUcretsizRozet, satir.TextContent.Trim());
        Assert.NotNull(satir.QuerySelector(".rozet"));
        Assert.DoesNotContain("Ticari", satir.TextContent, StringComparison.Ordinal);
        // Metin SİLİNMEDİ: rozetin ipucunda tam hukuki metin durur.
        Assert.Equal(Sozlukler.Tr.Ortak.SozlesmesizKaynakUyari, satir.GetAttribute("title"));
    }

    [Fact]
    public void Kur_ayar_hucresi_HAP_degil_duz_metin()
    {
        Oturum();
        KurYollari();
        var c = Render<Kuyumcu.Web.Pages.Kur>().AyrintiAc();
        c.WaitForAssertion(() => Assert.Contains("22 Ayar", c.Markup));
        var hucre = c.FindAll("td").First(td => td.TextContent.Trim() == "22 Ayar");
        Assert.Null(hucre.QuerySelector(".rozet"));
    }

    // ── #63 / #64 — POS ──────────────────────────────────────────────────────

    private void PosYollari()
    {
        Yol("/pos/cihazlar", """[{"id":"p1","ad":"Garanti POS","bankaAdi":"Garanti","subeId":"s1","kasaId":"k1","bankaHesapId":null,"uyeIsyeriNo":null,"terminalNo":null,"pesinKomisyonOran":0.0185,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0.00}]""");
        Yol("/kasalar", """[{"id":"k1","ad":"Kasa","paraBirimi":"TRY","subeId":"s1","aktif":true}]""");
    }

    [Fact]
    public void Pos_BASIT_aciklama_jargonsuz_PRO_ayrinti_korunur()
    {
        Oturum(mod: "BASIT");
        Basit();
        PosYollari();
        var c = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#sayfa-basi-pos-aciklama")));
        var metin = c.Find("#sayfa-basi-pos-aciklama").TextContent.Trim();
        Assert.Equal(SadeSozlugu.Tr.PosAciklamaBasit, metin);
        Assert.DoesNotContain("YOKTUR", metin, StringComparison.Ordinal);
        Assert.DoesNotContain("valör", metin, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pos_PRO_aciklama_tatil_ayrintisini_tasir()
    {
        Oturum();
        PosYollari();
        var c = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#sayfa-basi-pos-aciklama")));
        Assert.Equal(Sozlukler.Tr.Pos.TatilUyarisi, c.Find("#sayfa-basi-pos-aciklama").TextContent.Trim());
    }

    [Fact]
    public void Pos_kullanimdan_kaldir_GERI_ALINABILIR_tehlike_rengi_tasimaz_onay_birincil()
    {
        Oturum();
        PosYollari();
        var c = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#pos-pasif-p1")));
        var d = c.Find("#pos-pasif-p1");
        Assert.DoesNotContain("dugme-tehlike", d.ClassName);
        Assert.Contains("dugme-ikincil", d.ClassName);
        d.Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#pos-pasif-onayla")));
        var onay = c.Find("#pos-pasif-onayla");
        Assert.Contains("dugme-birincil", onay.ClassName);
        Assert.DoesNotContain("dugme-tehlike", onay.ClassName);
    }

    // ── #26 — POS tahsilatları: iki kopya boş durum ──────────────────────────

    [Fact]
    public void Pos_tahsilat_takvim_ve_defter_bos_durumlari_FARKLI_ve_basliklidir()
    {
        Oturum();
        Yol("/pos/valor-takvimi", "[]");
        Yol("/pos/tahsilatlar", """{"satirlar":[],"toplamSatir":0,"limit":50,"offset":0,"azamiSatir":200,"sinirDegdi":false}""");
        PosYollari();
        var c = Render<Kuyumcu.Web.Pages.PosTahsilatlar>().AyrintiAc();
        // ZO-ISLEM-A: defter kapalı akordeon (ortak Katlanir) — önce açılır.
        c.WaitForElement("#pos-t-defter-kart-ac").Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#pos-t-defter-bos")));
        var takvim = c.Find("#pos-t-takvim-bos").TextContent.Trim();
        var defter = c.Find("#pos-t-defter-bos").TextContent.Trim();
        Assert.NotEqual(takvim, defter);
        Assert.Equal(SadeSozlugu.Tr.PosTakvimBos, takvim);
        // ZO-ISLEM-A: başlık akordeonun başlık düğmesindedir (Katlanir sözleşmesi {Id}-ac).
        Assert.Equal(SadeSozlugu.Tr.PosDefterBaslik, c.Find("#pos-t-defter-kart-ac .katlanir-ad").TextContent.Trim());
    }

    [Fact]
    public void Pos_tahsilat_defteri_istek_DUSERSE_bos_durum_cizilmez_bant_cikar()
    {
        Oturum();
        Yol("/pos/valor-takvimi", "[]");
        Yol("/pos/tahsilatlar", """{"message":"defter okunamadı"}""", HttpStatusCode.InternalServerError);
        PosYollari();
        var c = Render<Kuyumcu.Web.Pages.PosTahsilatlar>().AyrintiAc();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#pos-t-hata")));
        Assert.Empty(c.FindAll("#pos-t-defter-bos"));
    }

    // ── #38 — Yetkiler ───────────────────────────────────────────────────────

    [Fact]
    public void Yetkiler_okunamazsa_YALNIZ_bant_bos_tablo_ve_tekrar_kutusu_yok()
    {
        Oturum("MUDUR");
        Yol("/yetkiler/benim", """{"moduller":[]}""");
        Yol("/yetkiler", """{"message":"Bu işlem için gerekli rol: SAHİP"}""", HttpStatusCode.Forbidden);
        var c = Render<Kuyumcu.Web.Pages.Yetkiler>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#hata-bandi")));
        Assert.Contains("SAHİP", c.Find("#hata-bandi").TextContent);
        Assert.Empty(c.FindAll("#yetki-tablo"));
        Assert.Empty(c.FindAll("#degismez-ipucu-SahipYetkisi"));
    }

    [Fact]
    public void Yetkiler_okununca_tablo_ve_sahip_gerekcesi_durur()
    {
        Oturum();
        Yol("/yetkiler/benim", """{"moduller":["cari"]}""");
        Yol("/yetkiler", """{"moduller":["cari","urun"],"roller":["MUDUR"],"matris":{"MUDUR":["cari"]}}""");
        var c = Render<Kuyumcu.Web.Pages.Yetkiler>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#yetki-tablo")));
        Assert.NotNull(c.Find("#degismez-ipucu-SahipYetkisi"));
        Assert.Empty(c.FindAll("#hata-bandi"));
    }

    // ── #41 — Ayarlar görünüm okuma hatası ───────────────────────────────────

    [Fact]
    public async Task Ayarlar_gorunum_okunamadiysa_HATA_BANDI_ve_yeniden_dene_gercek_istek()
    {
        Oturum();
        Yol("/gorunum", """{"message":"okunamadı"}""", HttpStatusCode.InternalServerError);
        // Uygulama açılışındaki okuma (App → GorunumServisi.YukleAsync) başarısız oldu.
        await Services.GetRequiredService<GorunumServisi>().YukleAsync();
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/ayarlar?sekme=dukkan");
        var c = Render<Kuyumcu.Web.Pages.Ayarlar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#gorunum-okuma-hata")));
        var bant = c.Find("#gorunum-okuma-hata");
        Assert.Equal("alert", bant.GetAttribute("role"));
        Assert.Contains("bant-hata", bant.ClassName);
        Assert.Contains(SadeSozlugu.Tr.GorunumOkunamadi, bant.TextContent);
        var once = istekler.Count(i => i.Yol.EndsWith("/gorunum"));
        c.Find("#gorunum-yeniden").Click();
        c.WaitForAssertion(() => Assert.True(istekler.Count(i => i.Yol.EndsWith("/gorunum")) > once));
    }

    // ── #73 — Sayım durum sütunu ─────────────────────────────────────────────

    [Fact]
    public void Sayim_IPTAL_durumu_EYLEM_metniyle_basilmaz()
    {
        Oturum();
        // TEMA TEMİZLİĞİ 17.10: Yıldız açık sayımı (sy2) kendiliğinden açar — özgün listeye "Tüm ayrıntılar → ← Sayım" ile
        // varılır; ayrıntı yanıtı gerekir (ilk eşleşen kazanır: "/sayim/" önce).
        Yol("/sayim/", """{"id":"sy2","ad":"Ekim","subeId":"s1","kapsamTur":"TUMU","durum":"ACIK","toplamSatir":0,"sayilan":0,"sayilmayan":0,"farkli":0,"eksikAdet":0,"fazlaAdet":0,"netFarkHas":0,"hareketli":0,"satirlar":[]}""");
        Yol("/sayim", """[{"id":"sy1","ad":"Eylül sayımı","kapsamTur":"TUMU","durum":"IPTAL"},{"id":"sy2","ad":"Ekim","kapsamTur":"TUMU","durum":"ACIK"}]""");
        var c = Render<Kuyumcu.Web.Pages.Sayim>().SayimOzgunListe();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#sy-durum-sy1")));
        var iptal = c.Find("#sy-durum-sy1").TextContent.Trim();
        Assert.Equal(SadeSozlugu.Tr.SayimIptalEdildi, iptal);
        Assert.NotEqual(Sozlukler.Tr.Sayim.Iptal, iptal);
        Assert.NotNull(c.Find("#sy-durum-sy1 .rozet"));
        Assert.Equal(Sozlukler.Tr.Sayim.Acik, c.Find("#sy-durum-sy2").TextContent.Trim());
    }

    // ── #18 — Kılavuz ────────────────────────────────────────────────────────

    [Fact]
    public void Kilavuz_BASIT_uzun_ekran_listesi_KATLI_bolumler_kart_izgarasi()
    {
        Oturum(mod: "BASIT");
        Basit();
        var c = Render<Kuyumcu.Web.Pages.Kilavuz>();
        var ayrinti = c.Find("#kl-ekranlar-ayrinti");
        Assert.Equal("details", ayrinti.TagName, StringComparer.OrdinalIgnoreCase);
        Assert.False(ayrinti.HasAttribute("open"));
        Assert.NotNull(ayrinti.QuerySelector("#kl-ekranlar"));
        Assert.NotEmpty(c.FindAll("#kl-ekranlar a"));   // liste SİLİNMEDİ
        Assert.All(c.FindAll("#kl-bolumler > li"), li => Assert.Contains("sade-kart-oge", li.ClassName));
    }

    [Fact]
    public void Kilavuz_PRO_ekran_listesi_ACIK()
    {
        Oturum();
        var c = Render<Kuyumcu.Web.Pages.Kilavuz>();
        Assert.True(c.Find("#kl-ekranlar-ayrinti").HasAttribute("open"));
    }

    // ── Doğrulama bulguları (tur 2) ──────────────────────────────────────────

    [Fact]
    public void Kur_hukuki_tam_metin_ROLE_bagli_PRO_modda_TEZGAHTAR_gormez_sebep_yazili()
    {
        Oturum(rol: "TEZGAHTAR");   // PROFESYONEL mod: "Kaynak" sekmesi çizilir
        KurYollari();
        var c = Render<Kuyumcu.Web.Pages.Kur>().AyrintiAc();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-sozlesmesiz-sahipte")));
        Assert.NotNull(c.Find("#kur-panel-kaynak"));   // mod PRO → sekme gerçekten var
        Assert.Empty(c.FindAll("#kur-sozlesmesiz-ayrinti"));
        // TOPLU-3: ipucu metni Toplu3Sozlugu'ndan (rol kapısı aynı).
        Assert.Equal(Kuyumcu.Core.I18n.Toplu3Sozlugu.Tr.KurUcretsizIpucu, c.Find("#kur-sozlesmesiz-sahipte").TextContent.Trim());
        Assert.Equal(Kuyumcu.Core.I18n.Toplu3Sozlugu.Tr.KurUcretsizIpucu, c.Find("#kur-sozlesmesiz").GetAttribute("title"));
        Assert.DoesNotContain(Sozlukler.Tr.Ortak.SozlesmesizKaynakUyari, c.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Kur_hukuki_tam_metin_SAHIP_Kaynak_sekmesinde_okur()
    {
        Oturum();   // SAHİP, PROFESYONEL
        KurYollari();
        var c = Render<Kuyumcu.Web.Pages.Kur>().AyrintiAc();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-sozlesmesiz-ayrinti")));
        Assert.Contains(Sozlukler.Tr.Ortak.SozlesmesizKaynakUyari, c.Find("#kur-sozlesmesiz-ayrinti").TextContent);
        Assert.Empty(c.FindAll("#kur-sozlesmesiz-sahipte"));
    }

    [Fact]
    public async Task Ayarlar_gorunum_OKUNURKEN_hata_bandi_YANIP_SONMEZ_bitince_basari_yoksa_cikar()
    {
        Oturum();
        bekletSonek = "/v1/gorunum";
        Yol("/kullanici/gorunum", """{"message":"yok"}""", HttpStatusCode.NotFound);
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/ayarlar?sekme=dukkan");
        // Uygulama açılışındaki okuma BAŞLADI ama yanıt gelmedi (istek sürüyor).
        var okuma = Services.GetRequiredService<GorunumServisi>().YukleAsync();
        var c = Render<Kuyumcu.Web.Pages.Ayarlar>();
        // TEMA TEMİZLİĞİ 17.10: kartın renk şeması seçicisi (#gorunum-sema) bilerek kaldırıldı — kart çizildi mi, kalan
        // seçiciyle (yazı ölçeği #gorunum-olcek, aynı kart #ayar-gorunum) ölçülür.
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#ayar-gorunum #gorunum-olcek")));
        Assert.Contains(istekler, i => i.Yol.EndsWith("/v1/gorunum"));
        Assert.False(okuma.IsCompleted);
        // Okunmadı ama DENENİP BİTMEDİ → hata bandı YOK (yanıp sönme yok); kilit sebebi gri satırda.
        Assert.Empty(c.FindAll("#gorunum-okuma-hata"));
        // Yanıt hata olarak döner → bant ŞİMDİ çıkar (pozitif eş).
        bekleyen.SetResult(HataYaniti(HttpStatusCode.InternalServerError, """{"message":"okunamadı"}"""));
        await okuma;
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#gorunum-okuma-hata")));
    }
}
