using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S-3f — QA turu 5 YÜKSEK bulguları (2. dilim) · EKRAN (bUnit)
//
// ⚠ KARARSIZLIK KORUMASI: zamanlayıcı beklenmez, yoklama yoktur, ateşle-unut
//   yan kanal ölçülmez. /kullanicilar 429 testinde ÜSTEL GERİ ÇEKİLME BEKLENMEZ;
//   yalnız "Yeniden dene" düğmesinin VARLIĞI ve GERÇEK İSTEK atması ölçülür.
//
// ⚠ SAHTE API her uca GERÇEKÇİ yanıt döndürür; `_ => "{}"` varsayılanı
//   JsonException atar ve ilgisiz testleri kırardı.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S3fEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Sorgu, string Govde)> istekler = [];

    // Kaç kez 429 dönülecek (0 = hiç). /kullanicilar bulgusu için.
    private int kullaniciOranSiniriKalan;

    private string takvimYaniti = TakvimAralikBos;
    private string posCihazlari = PosTahsilatsiz;
    private string pusulaDetay = PusulaDolu;
    private string siparisDetay = TaslakDetay;

    // ── Sabit yanıtlar ───────────────────────────────────────────────────────

    /// <summary>Aralıkta gün YOK ama PORTFÖYDE 2 evrak VAR — bulgunun tam hâli.</summary>
    private const string TakvimAralikBos = """
        {"bas":"2026-09-18","bit":"2026-10-18","toplamAdet":0,"toplamTutar":0.00,"gunler":[],
         "portfoyAdet":2,"portfoyTutar":6250.50,
         "portfoyIlkVade":"2026-11-20","portfoySonVade":"2026-12-31","azamiGun":366}
        """;

    /// <summary>Portföy de gerçekten boş — "kayıt yok" doğru cümledir.</summary>
    private const string TakvimGercektenBos = """
        {"bas":"2026-09-18","bit":"2026-10-18","toplamAdet":0,"toplamTutar":0.00,"gunler":[],
         "portfoyAdet":0,"portfoyTutar":0.00,
         "portfoyIlkVade":null,"portfoySonVade":null,"azamiGun":366}
        """;

    private const string TakvimDolu = """
        {"bas":"2026-11-20","bit":"2026-12-31","toplamAdet":2,"toplamTutar":6250.50,
         "gunler":[{"gun":"2026-11-20","adet":1,"tutar":1250.50},
                   {"gun":"2026-12-31","adet":1,"tutar":5000.00}],
         "portfoyAdet":2,"portfoyTutar":6250.50,
         "portfoyIlkVade":"2026-11-20","portfoySonVade":"2026-12-31","azamiGun":366}
        """;

    private const string EvrakListesi = """
        {"toplamAdet":1,"toplamTutar":1250.50,"limit":200,"azamiLimit":200,"offset":0,
         "dahaFazlasiVar":false,"portfoydeTutar":1250.50,"tahsileVerilenTutar":0.00,
         "buHaftaVadeliTutar":0.00,"karsiliksizTutar":0.00,
         "satirlar":[
           {"id":"e1","evrakTuru":"CEK","yon":"ALINAN","durum":"PORTFOYDE","tutar":1250.50,
            "paraBirimi":"TRY","vadeGunu":"2026-11-20","evrakNo":"CEK-1","bankaAdi":"Ziraat",
            "bankaSube":null,"kesideYeri":null,"kesideTarihi":null,"borcluCariId":null,
            "borcluAd":null,"kaynakCariId":"c1","kaynakCariAd":"Ayşe Yılmaz","notlar":null,
            "gecikmeGun":0,"gecikmis":false,"yakindaVadeli":false,"duzenlenebilir":true,
            "izinliGecisler":["TAHSILE_VERILDI"]}]}
        """;

    private const string PusulaListesi = """
        [{"id":"gp1","pusulaNo":"GP-2026-000001","hareketId":null,"saticiTipi":"VATANDAS",
          "saticiAdSoyad":"Ali Veli","saticiTckn":"11111111110","ayarId":"K22","gramAgirlik":10.0,
          "hasGrami":9.16,"brutTutar":10000.00,"stopajOran":0.05,"stopajTutar":500.00,
          "netOdenen":9500.00,"teslimTarihi":"2026-09-01T00:00:00Z",
          "duzenlemeSonTarihi":"2026-09-08T00:00:00Z","gecikti":false,"kalanGun":3,
          "iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null}]
        """;

    private const string SureTakibi = """
        {"sure":{"gun":7,"dayanak":"VUK md.234"},"gecikmisSayisi":0,"yaklasanSayisi":0,
         "gecikmis":[],"yaklasan":[],"uyarilar":[]}
        """;

    private const string PusulaDolu = """
        {"id":"gp1","pusulaNo":"GP-2026-000001","saticiTipi":"VATANDAS","saticiAdSoyad":"Ali Veli",
         "saticiTckn":"11111111110","saticiAdres":"Merkez mah. 1","ayarId":"K22","gramAgirlik":10.0,
         "hasGrami":9.16,"malAciklamasi":"22 ayar hurda bilezik","brutTutar":10000.00,
         "stopajOran":0.05,"stopajTutar":500.00,"netOdenen":9500.00,
         "teslimTarihi":"2026-09-01T00:00:00Z","duzenlemeSonTarihi":"2026-09-08T00:00:00Z",
         "subeId":null,"cariId":null,"hareketId":null,"duzeltilenId":null,
         "olusturuldu":"2026-09-01T00:00:00Z",
         "iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null}
        """;

    private const string PosTahsilatsiz = """
        [{"id":"p1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"sb1","kasaId":"k1",
          "bankaHesapId":null,"uyeIsyeriNo":null,"terminalNo":null,"pesinKomisyonOran":0.0180,
          "valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0.00,
          "bugunYatacakNet":0.00,"tahsilatVar":false}]
        """;

    private const string PosTahsilatli = """
        [{"id":"p1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"sb1","kasaId":"k1",
          "bankaHesapId":null,"uyeIsyeriNo":null,"terminalNo":null,"pesinKomisyonOran":0.0180,
          "valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":1500.00,
          "bugunYatacakNet":0.00,"tahsilatVar":true}]
        """;

    private const string TaslakDetay = """
        {"id":"s1","no":"S-2026-000004","tur":"MUSTERI_SIPARISI","durum":"TASLAK","cariId":"c1",
         "cariAd":"Ayşe Yılmaz","subeId":"sb1","sozVerilenGun":"2026-10-10","aciklama":"bilezik",
         "tahminiTutarTl":25000.00,"kurSnapshotId":null,"emanetHareketId":null,
         "geciken":false,"duzenlenebilir":true,"terminal":false,
         "izinliGecisler":["ONAYLANDI","IPTAL"],"gerekceIsteyenGecisler":["IPTAL"],"gerekceAsgari":10,
         "olusturanKullaniciId":"u1","olusturuldu":"2026-09-16T08:00:00Z",
         "guncellendi":"2026-09-16T08:00:00Z",
         "kalemler":[{"id":"k1","urunId":null,"urunAd":null,"tanim":"bilezik","ayarId":"K22",
                      "gramAgirlik":10.0,"adet":1.0,"sira":0}],
         "gecisler":[{"id":"g1","oncekiDurum":null,"yeniDurum":"TASLAK","gerekce":null,
                      "kullaniciId":"u1","kullaniciAd":"Test","olusturuldu":"2026-09-16T08:00:00Z"}]}
        """;

    private const string OnayliDetay = """
        {"id":"s1","no":"S-2026-000004","tur":"MUSTERI_SIPARISI","durum":"ONAYLANDI","cariId":"c1",
         "cariAd":"Ayşe Yılmaz","subeId":"sb1","sozVerilenGun":"2026-10-10","aciklama":"bilezik",
         "tahminiTutarTl":25000.00,"kurSnapshotId":null,"emanetHareketId":null,
         "geciken":false,"duzenlenebilir":false,"terminal":false,
         "izinliGecisler":["ATOLYEDE","HAZIR","TASLAK","IPTAL"],
         "gerekceIsteyenGecisler":["TASLAK","IPTAL"],"gerekceAsgari":10,
         "olusturanKullaniciId":"u1","olusturuldu":"2026-09-16T08:00:00Z",
         "guncellendi":"2026-09-16T08:00:00Z",
         "kalemler":[{"id":"k1","urunId":null,"urunAd":null,"tanim":"bilezik","ayarId":"K22",
                      "gramAgirlik":10.0,"adet":1.0,"sira":0}],
         "gecisler":[{"id":"g1","oncekiDurum":"TASLAK","yeniDurum":"ONAYLANDI","gerekce":null,
                      "kullaniciId":"u1","kullaniciAd":"Test","olusturuldu":"2026-09-16T09:00:00Z"}]}
        """;

    private const string KasaListesi =
        // PAKET GOC-A (fikstür düzeltmesi): sunucu /kasalar yanıtı "aktif" taşır; ekran yalnız aktif TRY kasaları
        // listeler. Alan eksikken liste BOŞ kalıyor, eski <select>.Change("k2") seçenek olmadan da geçiyordu.
        """[{"id":"k1","ad":"Merkez kasa","paraBirimi":"TRY","subeId":"sb1","aktif":true},"""
        + """{"id":"k2","ad":"Tezgah kasa","paraBirimi":"TRY","subeId":"sb1","aktif":true}]""";

    private const string YetkiMatrisiYaniti =
        """{"moduller":["cari","urun","kasa"],"roller":["MUDUR","MUHASEBE","TEZGAHTAR"],"matris":{"MUDUR":["cari","urun","kasa"],"MUHASEBE":["cari"],"TEZGAHTAR":["urun"]}}""";

    private const string BenYaniti =
        """{"id":"u1","ad":"Test","soyad":"Kullanici","email":"t@t.local","rol":"SAHIP","subeId":"sb1"}""";

    private const string KullaniciListesi = """
        [{"id":"u1","ad":"Test","soyad":"Kullanıcı","email":"t@ornek.com","rol":"SAHIP",
          "subeId":"sb1","aktif":true,"sonGiris":null,"mfaEtkin":false,"arayuzModu":"BASIT"}]
        """;

    public S3fEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<PosServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));
    }

    private sealed class SahteApi(S3fEkranTestleri sahip) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var sorgu = istek.RequestUri!.Query;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            sahip.istekler.Add((istek.Method.Method, yol, sorgu, govde));

            if (yol.EndsWith("/kullanicilar") && istek.Method == HttpMethod.Get
                && sahip.kullaniciOranSiniriKalan > 0)
            {
                sahip.kullaniciOranSiniriKalan--;
                return new HttpResponseMessage((System.Net.HttpStatusCode)429)
                {
                    Content = new StringContent(
                        """{"hata":"Çok fazla istek gönderildi, lütfen biraz bekleyin"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };
            }

            var g = yol switch
            {
                var y when y.EndsWith("/evraklar/takvim") => sahip.takvimYaniti,
                var y when y.EndsWith("/evraklar") => EvrakListesi,
                var y when y.EndsWith("/gider-pusulalari/sure-takibi") => SureTakibi,
                var y when y.Contains("/gider-pusulalari/") => sahip.pusulaDetay,
                var y when y.EndsWith("/gider-pusulalari") => PusulaListesi,
                var y when y.Contains("/pos/cihazlar/") =>
                    """{"cihaz":null,"taksitOranlari":[]}""",
                var y when y.EndsWith("/pos/cihazlar") => sahip.posCihazlari,
                var y when y.Contains("/siparisler/") && y.EndsWith("/gecis") =>
                    """{"id":"s1","no":"S-2026-000004","durum":"TASLAK"}""",
                var y when y.Contains("/siparisler/") => sahip.siparisDetay,
                var y when y.EndsWith("/kullanicilar") => KullaniciListesi,
                var y when y.EndsWith("/yetkiler/benim") => """{"moduller":["cari","urun"]}""",
                var y when y.EndsWith("/yetkiler") => YetkiMatrisiYaniti,
                var y when y.EndsWith("/masak/ozet") => """{"acikBildirim":0,"kimlikTespiti":0}""",
                var y when y.EndsWith("/kasalar") => KasaListesi,
                var y when y.EndsWith("/bankalar") =>
                    """[{"id":"b1","hesapAdi":"Vadesiz TL","bankaAdi":"Ziraat","paraBirimi":"TRY"}]""",
                var y when y.EndsWith("/subeler") => """[{"id":"sb1","ad":"Merkez"}]""",
                var y when y.EndsWith("/cari") =>
                    """[{"id":"c1","ad":"Ayşe Yılmaz","tur":"MUSTERI"}]""",
                var y when y.EndsWith("/urunler") => "[]",
                var y when y.Contains("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6827.20,"gramAltinFiyati":6861.51}""",
                var y when y.Contains("/auth/ben") => BenYaniti,
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    // ═════ /evraklar/takvim — boş durum YANLIŞ bilgi veriyordu ═══════════════

    [Fact]
    public void Aralik_bossa_ama_PORTFOYDE_evrak_varsa_portfoyde_evrak_yok_DENMEZ()
    {
        takvimYaniti = TakvimAralikBos;
        var c = Render<Kuyumcu.Web.Pages.EvrakTakvimi>().AyrintiAc();

        // Doğru cümle + portföy toplamı ekranda.
        Assert.Contains("Seçili tarih aralığında", c.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Portföyde evrak yok", c.Markup, StringComparison.Ordinal);
        Assert.Contains("6.250,50", c.Find("#evrak-takvim-portfoy").TextContent, StringComparison.Ordinal);
        Assert.NotNull(c.Find("#evrak-takvim-genislet"));
    }

    [Fact]
    public void Portfoy_gercekten_bossa_ESKI_bos_durum_metni_cikar()
    {
        takvimYaniti = TakvimGercektenBos;
        var c = Render<Kuyumcu.Web.Pages.EvrakTakvimi>().AyrintiAc();

        Assert.Empty(c.FindAll("#evrak-takvim-portfoy"));
        Assert.Empty(c.FindAll("#evrak-takvim-genislet"));
        Assert.NotNull(c.Find("#evrak-takvim-bos"));
    }

    [Fact]
    public void Araligi_portfoye_genislet_GERCEK_istek_atar_ve_portfoy_vadelerini_kullanir()
    {
        takvimYaniti = TakvimAralikBos;
        var c = Render<Kuyumcu.Web.Pages.EvrakTakvimi>().AyrintiAc();
        takvimYaniti = TakvimDolu;

        c.Find("#evrak-takvim-genislet").Click();

        var son = istekler.Last(i => i.Yol.EndsWith("/evraklar/takvim"));
        Assert.Contains("bas=2026-11-20", son.Sorgu, StringComparison.Ordinal);
        Assert.Contains("bit=2026-12-31", son.Sorgu, StringComparison.Ordinal);
    }

    // ═════ /evraklar — derin bağlantıdaki vade süzgeci uygulanmıyordu ════════

    [Fact]
    public void Vade_sorgu_parametreleri_ILK_YUKLEMEDE_okunur_ve_listeye_uygulanir()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("http://localhost/evraklar?vadeBas=2026-11-20&vadeBit=2026-11-20");

        var c = Render<Kuyumcu.Web.Pages.Evraklar>().AyrintiAc();

        var get = istekler.Last(i => i.Metot == "GET" && i.Yol.EndsWith("/evraklar"));
        Assert.Contains("vadeBas=2026-11-20", get.Sorgu, StringComparison.Ordinal);
        Assert.Contains("vadeBit=2026-11-20", get.Sorgu, StringComparison.Ordinal);

        // Süzgeç kutuları DOLU ve etkin olduğu ekranda yazıyor (UX-S29).
        // GIRDI-AILESI-2 (kurucu 08.10): ortak <Tarih> metin + gg.aa.yyyy maskesi; yerli seçici takvim simgesinde (#<id>-takvim-yerli).
        Assert.Equal("20.11.2026", c.Find("#evrak-vade-bas").GetAttribute("value"));
        Assert.NotNull(c.Find("#evrak-suzgec-baglanti"));
    }

    [Fact]
    public void Sorgu_parametresi_YOKSA_vade_suzgeci_bos_kalir_ve_rozet_cikmaz()
    {
        var c = Render<Kuyumcu.Web.Pages.Evraklar>().AyrintiAc();

        var get = istekler.Last(i => i.Metot == "GET" && i.Yol.EndsWith("/evraklar"));
        Assert.DoesNotContain("vadeBas=", get.Sorgu, StringComparison.Ordinal);
        Assert.Empty(c.FindAll("#evrak-suzgec-baglanti"));
    }

    [Fact]
    public void Bozuk_tarih_sorgusu_SESSIZCE_uygulanmaz()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("http://localhost/evraklar?vadeBas=filanca");

        var c = Render<Kuyumcu.Web.Pages.Evraklar>().AyrintiAc();

        var get = istekler.Last(i => i.Metot == "GET" && i.Yol.EndsWith("/evraklar"));
        Assert.DoesNotContain("vadeBas=", get.Sorgu, StringComparison.Ordinal);
        Assert.Empty(c.FindAll("#evrak-suzgec-baglanti"));
    }

    // ═════ /gider-pusulalari — boş adresle kayıt (VUK md.234) ════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.GiderPusulalari> PusulaDetayAc()
    {
        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>().AyrintiAc();
        // Detay düğmesinin kendi kimliği yok (mevcut kimlikler DEĞİŞMEZ kuralı);
        // satırın ilk eylem düğmesi tıklanır.
        c.FindAll("tbody button")[0].Click();
        return c;
    }

    [Fact]
    public void Adres_bosaltilinca_alan_alti_TURKCE_mesaj_cikar_ve_KAYDET_kilitlenir()
    {
        var c = PusulaDetayAc();
        c.Find("#gp-duz-adres").Input("");

        Assert.Contains("VUK md.234", c.Find("#gp-duz-adres-hata").TextContent, StringComparison.Ordinal);
        Assert.True(c.Find("#cekmece-gider-pusulasi-kaydet").HasAttribute("disabled"));
    }

    [Fact]
    public void Mal_aciklamasi_bosaltilinca_da_KAYDET_kilitlenir()
    {
        var c = PusulaDetayAc();
        c.Find("#gp-duz-mal").Input("   ");

        Assert.NotNull(c.Find("#gp-duz-mal-hata"));
        Assert.True(c.Find("#cekmece-gider-pusulasi-kaydet").HasAttribute("disabled"));
    }

    [Fact]
    public void Bos_adresle_HICBIR_PATCH_istegi_GITMEZ()
    {
        var c = PusulaDetayAc();
        c.Find("#gp-duz-adres").Input("");
        var oncekiPatch = istekler.Count(i => i.Metot == "PATCH");

        c.Find("#cekmece-gider-pusulasi-kaydet").Click();

        Assert.Equal(oncekiPatch, istekler.Count(i => i.Metot == "PATCH"));
    }

    [Fact]
    public void Iki_alan_da_doluyken_KAYDET_acik_ve_GERCEK_PATCH_atar()
    {
        var c = PusulaDetayAc();
        c.Find("#gp-duz-adres").Input("Yeni mah. 5");

        Assert.False(c.Find("#cekmece-gider-pusulasi-kaydet").HasAttribute("disabled"));
        c.Find("#cekmece-gider-pusulasi-kaydet").Click();
        Assert.Contains(istekler, i => i.Metot == "PATCH" && i.Yol.Contains("/gider-pusulalari/"));
    }

    // ═════ /siparisler — durum geçişi onaysız ve geri dönüşsüzdü ═════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.SiparisDetay> Detay() =>
        Render<Kuyumcu.Web.Pages.SiparisDetay>(p => p.Add(x => x.Id, "s1"));

    [Fact]
    public void Durum_dugmesine_tek_tiklama_ISTEK_ATMAZ_once_onay_kipi_acilir()
    {
        siparisDetay = TaslakDetay;
        var c = Detay();

        c.Find("#sipd-gecis-ONAYLANDI").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/gecis"));
        Assert.NotNull(c.Find("#sipd-gecis-onay-metni"));
    }

    [Fact]
    public void Onay_kipinde_ONAYLA_basilinca_GERCEK_gecis_istegi_gider()
    {
        siparisDetay = TaslakDetay;
        var c = Detay();

        c.Find("#sipd-gecis-ONAYLANDI").Click();
        c.Find("#sipd-gecis-onayla").Click();

        var post = istekler.Last(i => i.Metot == "POST" && i.Yol.EndsWith("/gecis"));
        var jo = System.Text.Json.JsonDocument.Parse(post.Govde).RootElement;
        Assert.Equal("ONAYLANDI", jo.GetProperty("yeniDurum").GetString());
    }

    [Fact]
    public void Onay_kipinde_VAZGEC_istek_atmaz_ve_kipi_kapatir()
    {
        siparisDetay = TaslakDetay;
        var c = Detay();

        c.Find("#sipd-gecis-ONAYLANDI").Click();
        c.Find("#sipd-gecis-vazgec").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/gecis"));
        Assert.Empty(c.FindAll("#sipd-gecis-onay-metni"));
    }

    [Fact]
    public void Onaylandi_durumunda_TASLAGA_DONUS_dugmesi_ve_yol_metni_vardir()
    {
        siparisDetay = OnayliDetay;
        var c = Detay();

        Assert.NotNull(c.Find("#sipd-gecis-TASLAK"));
        Assert.NotNull(c.Find("#sipd-taslaga-don-notu"));
    }

    [Fact]
    public void Geri_gecis_GEREKCE_ister_kisa_gerekcede_ONAYLA_kilitlidir()
    {
        siparisDetay = OnayliDetay;
        var c = Detay();

        c.Find("#sipd-gecis-TASLAK").Click();
        Assert.True(c.Find("#sipd-gecis-onayla").HasAttribute("disabled"));

        c.Find("#sipd-gecis-gerekce").Input("Müşteri kalem sayısını değiştirdi");
        Assert.False(c.Find("#sipd-gecis-onayla").HasAttribute("disabled"));

        c.Find("#sipd-gecis-onayla").Click();
        var post = istekler.Last(i => i.Metot == "POST" && i.Yol.EndsWith("/gecis"));
        // ⚠ JSON gövdesi ASCII kaçışlıdır; metin ARAMA ile değil AYRIŞTIRILARAK sınanır.
        var j = System.Text.Json.JsonDocument.Parse(post.Govde).RootElement;
        Assert.Equal("TASLAK", j.GetProperty("yeniDurum").GetString());
        Assert.Equal("Müşteri kalem sayısını değiştirdi", j.GetProperty("gerekce").GetString());
    }

    // ═════ /kullanicilar — 429'da sayfa sessizce boş kalıyordu ═══════════════

    [Fact]
    public void Oran_sinirinda_YENIDEN_DENE_dugmesi_ve_yuklenemedi_durumu_cikar()
    {
        kullaniciOranSiniriKalan = 99;   // kalıcı 429
        var c = Render<Kuyumcu.Web.Pages.Kullanicilar>();

        // ⚠ PAKET P-07: 429 artık ÖNCE ApiIstemci bandında üstel geri çekilmeyle
        //   (200/400/800 ms + jitter) denenir; hata ekrana o denemeler bitince
        //   düşer. Beklenti DEĞİŞMEDİ (düğme + hata bandı çıkar, boş durum ÇIKMAZ),
        //   yalnız bekleme senkronizasyonu eklendi.
        c.WaitForElement("#k-yeniden-dene", TimeSpan.FromSeconds(10));
        Assert.NotNull(c.Find("#k-yeniden-dene"));
        Assert.NotNull(c.Find("#k-liste-hata"));
        // "Kullanıcı bulunamadı" boş durumu ÇIKMAZ (UX-S28: hata ≠ kayıt yok).
        Assert.DoesNotContain("Kullanıcı bulunamadı", c.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Yeniden_dene_GERCEK_istek_atar_ve_basarida_liste_gelir()
    {
        kullaniciOranSiniriKalan = 99;
        var c = Render<Kuyumcu.Web.Pages.Kullanicilar>();
        c.WaitForElement("#k-yeniden-dene", TimeSpan.FromSeconds(10));   // P-07 bandı geri çekilmesini bitirsin
        var once = istekler.Count(i => i.Metot == "GET" && i.Yol.EndsWith("/kullanicilar"));

        kullaniciOranSiniriKalan = 0;    // sınır kalktı
        c.Find("#k-yeniden-dene").Click();

        c.WaitForAssertion(() => Assert.Empty(c.FindAll("#k-yeniden-dene")), TimeSpan.FromSeconds(10));
        Assert.True(istekler.Count(i => i.Metot == "GET" && i.Yol.EndsWith("/kullanicilar")) > once);
        Assert.Contains("t@ornek.com", c.Markup, StringComparison.Ordinal);
    }

    // ═════ /pos — bağlı kasa oluşturmadan sonra düzenlenemiyordu ═════════════

    [Fact]
    public void Tahsilati_OLMAYAN_cihazda_bagli_kasa_secicisi_DUZENLEMEDE_de_cizilir()
    {
        posCihazlari = PosTahsilatsiz;
        var c = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        c.Find("#pos-duzenle-p1").Click();

        Assert.NotNull(c.Find("#pos-cihaz-kasa"));
        Assert.Empty(c.FindAll("#pos-cihaz-kasa-kilit"));
    }

    [Fact]
    // ⚠ SÖZLEŞME DEĞİŞTİ — PAKET P-22 / B-55: tahsilatlı cihazda kasa alanı
    //   hâlâ doğrudan düzenlenmez (sessiz değişiklik yok), AMA artık çıkmaz
    //   değildir: "yeni kasaya taşı" göç yolu ekranda vardır (UX-S25).
    public void Tahsilati_OLAN_cihazda_kasa_dogrudan_degismez_ama_GOC_YOLU_vardir()
    {
        posCihazlari = PosTahsilatli;
        var c = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        c.Find("#pos-duzenle-p1").Click();

        Assert.Empty(c.FindAll("#pos-cihaz-kasa"));
        Assert.NotNull(c.Find("#pos-cihaz-kasa-kilit"));
        // Ölü uyarı yerine GERÇEK BİR DÜĞME: tıklanınca göç formu açılır.
        Assert.NotNull(c.Find("#pos-kasa-goc-ac"));
        c.Find("#pos-kasa-goc-ac").Click();
        c.WaitForElement("#pos-goc-kasa");
        Assert.NotNull(c.Find("#pos-goc-uyari"));
    }

    [Fact]
    public void Kasa_degistirilince_PATCH_govdesinde_kasaId_gider()
    {
        posCihazlari = PosTahsilatsiz;
        var c = Render<Kuyumcu.Web.Pages.Pos>().AyrintiAc();
        c.Find("#pos-duzenle-p1").Click();
        // PAKET GOC-A: ham <select> → ortak Secici (aynı id); seçim liste öğesine tıklamayla.
        c.Find("#pos-cihaz-kasa").Click();
        c.Find("#pos-cihaz-kasa-liste li[data-deger='k2']").Click();
        c.Find("#cekmece-pos-cihaz-kaydet").Click();

        var patch = istekler.Last(i => i.Metot == "PATCH" && i.Yol.Contains("/pos/cihazlar/"));
        var jp = System.Text.Json.JsonDocument.Parse(patch.Govde).RootElement;
        Assert.Equal("k2", jp.GetProperty("kasaId").GetString());
    }

    // ═════ /yetkiler — ROL sütunu donuk değildi ══════════════════════════════

    [Fact]
    public void Yetki_matrisinde_ilk_sutun_YAPISKAN_sinifini_tasir()
    {
        var c = Render<Kuyumcu.Web.Pages.Yetkiler>();
        var tablo = c.Find("table.veri-tablosu");
        Assert.Contains("tablo-yapiskan-ilk", tablo.GetAttribute("class") ?? "", StringComparison.Ordinal);
    }
}
