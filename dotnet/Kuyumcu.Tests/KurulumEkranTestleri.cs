using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// KURULUŞ SİHİRBAZI EKRANI — paket T-2 Faz A [bUnit]
//
// "Düğme var" yetmez: hangi uca hangi gövdeyle gidildiği, KİLİTLİ düğmenin
// sebebini SÖYLEYİP söylemediği (UX-S30 · DoD/1), hatanın boş duruma
// ÇEVRİLMEDİĞİ (UX-S28) ve mühürden sonra ekranın SALT OKUNUR olduğu ölçülür.
//
// ⚠ Her olay sonrası bekleme WaitForAssertion ile (sabit uyku YASAK).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KurulumEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string durumJson = DurumAcik;
    private string provaJson = ProvaHatali;
    private bool durumHatasi;
    // P-68: kasa listesi yanıtı ve PATCH /kurulum/durum bekletici (kaydediliyor anı).
    private string kasalarJson = """[{"id":"k1","ad":"Merkez Kasa","aktif":true}]""";
    private TaskCompletionSource<bool>? patchBekletici;

    private const string DurumAcik = """
        {"muhurlendi":false,"muhurTarihi":null,"muhurleyenKullaniciId":null,"muhurGerekcesi":null,
         "acilisPenceresiBitis":"2026-10-17T00:00:00Z","acilisPenceresiAcik":true,
         "muhurlenebilir":true,"eksikAdimlar":[],"atlananAdimlar":[],
         "adimlar":[
           {"anahtar":"ISLETME","bitti":true,"zorunlu":true,"atlandi":false},
           {"anahtar":"KULLANICILAR","bitti":true,"zorunlu":true,"atlandi":false},
           {"anahtar":"KUR","bitti":true,"zorunlu":true,"atlandi":false},
           {"anahtar":"KASA_BANKA","bitti":true,"zorunlu":true,"atlandi":false},
           {"anahtar":"CARI_ACILIS","bitti":false,"zorunlu":false,"atlandi":false},
           {"anahtar":"STOK_ACILIS","bitti":false,"zorunlu":false,"atlandi":false}],
         "sayilar":{"sube":1,"kullanici":1,"kur":1,"kasa":1,"banka":0,
                    "acilisCari":0,"acilisKasa":0,"acilisBanka":0},
         "maksSatir":500,"gerekceAsgari":20,"isletmeAdi":"Altın Işık Kuyumcu"}
        """;

    /// <summary>P-49: DurumAcik'taki kayıtlı işletme adı (type-to-confirm hedefi).</summary>
    private const string IsletmeAdi = "Altın Işık Kuyumcu";

    private const string DurumMuhurlu = """
        {"muhurlendi":true,"muhurTarihi":"2026-09-17T10:00:00Z","muhurleyenKullaniciId":"u1",
         "muhurGerekcesi":"Kurulum tamamlandi, acilis kapatiliyor",
         "acilisPenceresiBitis":"2026-10-17T00:00:00Z","acilisPenceresiAcik":false,
         "muhurlenebilir":false,"eksikAdimlar":[],"atlananAdimlar":[],
         "adimlar":[{"anahtar":"ISLETME","bitti":true,"zorunlu":true,"atlandi":false}],
         "sayilar":{"sube":1,"kullanici":1,"kur":1,"kasa":1,"banka":0,
                    "acilisCari":2,"acilisKasa":1,"acilisBanka":0},
         "maksSatir":500,"gerekceAsgari":20}
        """;

    private const string ProvaHatali = """
        {"tur":"KASA","temiz":false,"provaJetonu":null,"toplamSatir":1,"hataliSatir":1,
         "uygulanacakSatir":0,"uyariliSatir":0,"limitAsimiOnayiGerekli":false,
         "tlToplam":0,"hasToplam":0,"kisiselVeriIzi":false,
         "hatalar":[{"satirNo":1,"hataKodu":"SIFIR_SATIR",
                     "hataMesaji":"Açılış satırında en az bir tutar sıfırdan farklı olmalıdır"}],
         "uyarilar":[]}
        """;

    private const string ProvaTemiz = """
        {"tur":"KASA","temiz":true,"provaJetonu":"jeton-1","toplamSatir":1,"hataliSatir":0,
         "uygulanacakSatir":1,"uyariliSatir":0,"limitAsimiOnayiGerekli":false,
         "tlToplam":1000,"hasToplam":0,"kisiselVeriIzi":false,"hatalar":[],"uyarilar":[]}
        """;

    public KurulumEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(KurulumEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add((istek.Method.Method, yol, govde));

            if (istek.Method == HttpMethod.Patch && yol.EndsWith("/kurulum/durum")
                && t.patchBekletici is { } bek)
                await bek.Task;

            if (yol.EndsWith("/kurulum/durum") && t.durumHatasi)
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(
                        """{"message":"Sunucu hatasi","statusCode":500}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            // ⚠ UX-S17: bUnit sahte API'nin "{}" varsayılanı yeni uçlarda
            //   JsonException'a döner; her uca GERÇEKÇİ yanıt verilir.
            var g = yol switch
            {
                var y when y.EndsWith("/kurulum/durum") => t.durumJson,
                var y when y.EndsWith("/kurulum/muhurle") => DurumMuhurlu,
                var y when y.EndsWith("/kurulum/acilis/prova") => t.provaJson,
                var y when y.EndsWith("/kurulum/acilis/uygula") =>
                    """{"oturumId":"o1","tur":"KASA","uygulananSatir":1,"tlToplam":1000,"hasToplam":0,"geriAlinabilir":true}""",
                var y when y.EndsWith("/kurulum/acilis/geri-al") =>
                    """{"oturumId":"o1","geriAlinan":1}""",
                var y when y.EndsWith("/kasalar") => t.kasalarJson,
                var y when y.EndsWith("/bankalar") =>
                    """{"hesaplar":[{"id":"b1","bankaAdi":"Ziraat","hesapAdi":"Vadesiz","aktif":true}]}""",
                var y when y.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz","aktif":true}]""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6827.2}""",
                var y when y.Contains("/yetkiler/benim") =>
                    """{"moduller":["cari","urun","islem","kasa","rapor","kurulum"]}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Kurulum> Ciz(bool profesyonel = true)
    {
        Kullanici(profesyonel ? ArayuzModuServisi.Profesyonel : ArayuzModuServisi.Basit);
        var e = Render<Kuyumcu.Web.Pages.Kurulum>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kur-adimlar")));
        istekler.Clear();
        return e;
    }

    private void Kullanici(string mod) =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", null, mod));

    private static void SatirKur(IRenderedComponent<Kuyumcu.Web.Pages.Kurulum> e)
    {
        e.Find("#kur-satir-ekle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kur-acilis-tablo")));
    }

    private static void GerekceYaz(IRenderedComponent<Kuyumcu.Web.Pages.Kurulum> e, string metin) =>
        e.Find("#kur-gerekce").Input(metin);

    /// <summary>P-49: mühür kartının AYRI gerekçe alanı.</summary>
    private static void MuhurGerekceYaz(IRenderedComponent<Kuyumcu.Web.Pages.Kurulum> e, string metin) =>
        e.Find("#kur-muhur-gerekce").Input(metin);

    // ── UX-S30 / DoD/1 — kilitli düğme SEBEBİNİ söyler ──────────────────────

    [Fact]
    public void Satir_yokken_PROVA_kilitli_ve_sebep_YAZAR()
    {
        var e = Ciz();
        Assert.True(e.Find("#kur-prova").HasAttribute("disabled"));
        Assert.NotNull(e.Find("#kur-prova-kilit"));
    }

    [Fact]
    public void Prova_temiz_degilken_Uygula_dugmesi_KAPALI_ve_sebebi_yazili()
    {
        provaJson = ProvaHatali;
        var e = Ciz();
        SatirKur(e);
        GerekceYaz(e, "Eski programdan devreden acilis bakiyesi");

        e.Find("#kur-prova").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kur-prova-sonuc")));

        Assert.True(e.Find("#kur-uygula").HasAttribute("disabled"));
        var sebep = e.Find("#kur-uygula-kilit").TextContent;
        // TOPLU-4 (2): sebep düğmenin ekrandaki adını söyler; "kuru prova" jargonu sızmaz.
        Assert.Contains("Kontrol et", sebep, StringComparison.Ordinal);
        Assert.DoesNotContain("prova", sebep, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Gerekce_20_karakterden_kisayken_Uygula_KAPALI()
    {
        provaJson = ProvaTemiz;
        var e = Ciz();
        SatirKur(e);
        GerekceYaz(e, "kisa");

        e.Find("#kur-prova").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kur-prova-sonuc")));

        Assert.True(e.Find("#kur-uygula").HasAttribute("disabled"));
        Assert.NotNull(e.Find("#kur-uygula-kilit"));

        // Gerekçe yeterince uzayınca AÇILIR ve gerçek istek atar.
        GerekceYaz(e, "Eski programdan devreden acilis bakiyesi");
        e.WaitForAssertion(() => Assert.False(e.Find("#kur-uygula").HasAttribute("disabled")));
        e.Find("#kur-uygula").Click();
        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Yol.EndsWith("/kurulum/acilis/uygula")));
    }

    // ── Hata satırları ──────────────────────────────────────────────────────

    [Fact]
    public void Hata_satirlari_tabloda_satir_no_ile_gosterilir()
    {
        provaJson = ProvaHatali;
        var e = Ciz();
        SatirKur(e);
        GerekceYaz(e, "Eski programdan devreden acilis bakiyesi");
        e.Find("#kur-prova").Click();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kur-prova-hatalar")));
        var metin = e.Find("#kur-prova-hatalar").TextContent;
        // ⚠ PAKET P-13 (UX-S31): ham iç kod ARTIK BASILMIYOR. Kullanıcı Türkçe
        //   mesajı okur, desteğe kısa hata kimliğini söyler. Sözleşme değişti:
        //   ham kodun GÖRÜNMEMESİ de sınanır, yoksa sızıntı sessizce geri gelir.
        Assert.DoesNotContain("SIFIR_SATIR", metin);
        Assert.Contains("AC-03", metin);
        Assert.Contains("sıfırdan farklı", metin);
    }

    // ── Mühür sonrası salt okunur ───────────────────────────────────────────

    [Fact]
    public void Muhurlendikten_sonra_adim_formlari_SALT_OKUNUR()
    {
        durumJson = DurumMuhurlu;
        var e = Ciz();

        Assert.NotNull(e.Find("#kur-muhur-rozeti"));
        // Mühürlüyse "mühürle" kartı hiç çizilmez.
        Assert.Empty(e.FindAll("#kur-muhurle"));
        // Açılış düğmeleri kilitli.
        Assert.True(e.Find("#kur-satir-ekle").HasAttribute("disabled"));
        Assert.True(e.Find("#kur-prova").HasAttribute("disabled"));
        // UX-S25: değişmezliğin gerekçesi ve doğru düzeltme yolu yazılı.
        Assert.NotNull(e.Find("#degismez-ipucu-AcilisKalemi"));
    }

    // ── UX-3 basit mod ──────────────────────────────────────────────────────

    [Fact]
    public void Basit_modda_ileri_alanlar_ProAlan_icinde_gizli()
    {
        var e = Ciz(profesyonel: false);

        // Basit modda ileri bilgiler çizilmez…
        Assert.Empty(e.FindAll("#kur-acilis-sinir"));
        Assert.Empty(e.FindAll("#kur-pencere"));

        // …profesyonel modda AYNI ekran onları GÖSTERİR (mod bir TERCİHTİR,
        // yetki değil: sunucu kapısı ikisinde de aynıdır — K-UX-6).
        var pro = Ciz();
        Assert.NotNull(pro.Find("#kur-acilis-sinir"));
        Assert.NotNull(pro.Find("#kur-pencere"));
    }

    // ── UX-S28 — hata "kayıt yok"a çevrilmez ────────────────────────────────

    [Fact]
    public void Sunucu_hatasi_bos_duruma_cevrilmez_HataBandi_cikar()
    {
        durumHatasi = true;
        Kullanici(ArayuzModuServisi.Profesyonel);

        var e = Render<Kuyumcu.Web.Pages.Kurulum>();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kur-hata")));
        // Hata bandı ayrıdır; sihirbaz "her şey tamam" görüntüsü VERMEZ.
        Assert.Empty(e.FindAll("#kur-adimlar"));
    }

    // ── KVKK / UX-S20 — toplu kişisel veri uyarısı (Mehmet koşulu 5) ────────

    [Fact]
    public void Cari_acilisinda_KISISEL_VERI_uyarisi_ekranda_YAZAR()
    {
        var e = Ciz();
        Assert.Empty(e.FindAll("#kur-kvkk"));

        SeciciYardim.Sec(e, "kur-acilis-tur", "CARI");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kur-kvkk")));

        var metin = e.Find("#kur-kvkk").TextContent;
        Assert.Contains("denetim izine", metin, StringComparison.OrdinalIgnoreCase);
    }

    // ── DoD/1 — tıklanınca hiçbir şey olmayan düğme YOK ─────────────────────

    [Fact]
    public void Her_dugme_ya_ISTEK_atar_ya_GORUNUR_degisiklik_yapar_ya_KILITLIDIR()
    {
        provaJson = ProvaTemiz;
        var e = Ciz();

        foreach (var d in e.FindAll("button"))
        {
            if (d.HasAttribute("disabled")) continue;
            Assert.True(d.HasAttribute("id"), "Kilitli olmayan düğmenin id'si yok.");
        }

        // "Satır ekle" istek atmaz ama GÖRÜNÜR bir değişiklik yapar (yeni satır).
        Assert.Empty(e.FindAll("#kur-acilis-tablo"));
        e.Find("#kur-satir-ekle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kur-satir-0")));

        // "Prova" gerçek istek atar.
        GerekceYaz(e, "Eski programdan devreden acilis bakiyesi");
        e.Find("#kur-prova").Click();
        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/kurulum/acilis/prova")));

        // "Satır çıkar" da görünür değişiklik yapar.
        e.Find("#kur-satir-0-sil").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#kur-satir-0")));
    }

    // ── Yıkıcı eylem ORTAK ONAY MODALI ister (DoD EK madde 4) ───────────────

    [Fact]
    public void Muhurleme_ONAY_MODALI_ister_ve_onaylanmadan_istek_ATILMAZ()
    {
        var e = Ciz();
        // P-49: mühür kendi gerekçe alanıyla açılır (açılış gerekçesi DEĞİL).
        MuhurGerekceYaz(e, "Kurulum tamamlandi, acilis kapatiliyor");
        e.WaitForAssertion(() => Assert.False(e.Find("#kur-muhurle").HasAttribute("disabled")));

        e.Find("#kur-muhurle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kur-onay-metni")));
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/kurulum/muhurle"));

        // Geri dönüş yolu var: "Hayır" istek atmadan kapatır.
        e.Find("#kur-onay-hayir").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#kur-onay-metni")));
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/kurulum/muhurle"));

        e.Find("#kur-muhurle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kur-onay-evet")));
        e.Find("#kur-onay-isletme-adi").Input(IsletmeAdi);
        e.WaitForAssertion(() => Assert.False(e.Find("#kur-onay-evet").HasAttribute("disabled")));
        e.Find("#kur-onay-evet").Click();
        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/kurulum/muhurle")));
    }

    // ── P-49 — mühür AYRI gerekçe + işletme adı yazdıran ikinci kapı ────────

    [Fact]
    public void Acilis_gerekcesi_muhur_dugmesini_etkinlestirmez()
    {
        var e = Ciz();
        const string acilisGerekcesi = "Eski programdan devreden bakı";
        Assert.Equal(29, acilisGerekcesi.Length);

        GerekceYaz(e, acilisGerekcesi);
        // Açılış gerekçesi geçerli (açılış tarafı bunu kabul eder)…
        e.WaitForAssertion(() => Assert.Contains("29", e.Find("#kur-gerekce-sayac").TextContent));
        // …ama mühür düğmesi KİLİTLİ kalır ve sebebini yazar (UX-S30).
        Assert.True(e.Find("#kur-muhurle").HasAttribute("disabled"));
        Assert.NotNull(e.Find("#kur-muhur-kilit"));
        Assert.Equal("", e.Find("#kur-muhur-gerekce").GetAttribute("value") ?? "");
    }

    [Fact]
    public void Ayri_muhur_gerekcesi_dolunca_dugme_etkinlesir()
    {
        var e = Ciz();
        Assert.True(e.Find("#kur-muhurle").HasAttribute("disabled"));

        MuhurGerekceYaz(e, new string('m', 19));
        e.WaitForAssertion(() => Assert.True(e.Find("#kur-muhurle").HasAttribute("disabled")));
        Assert.NotNull(e.Find("#kur-muhur-kilit"));

        MuhurGerekceYaz(e, new string('m', 20));
        e.WaitForAssertion(() => Assert.False(e.Find("#kur-muhurle").HasAttribute("disabled")));
        Assert.Empty(e.FindAll("#kur-muhur-kilit"));

        // Mühür gerekçesi açılış gerekçesini DOLDURMAZ (bağımsız değişken).
        Assert.Equal("", e.Find("#kur-gerekce").GetAttribute("value") ?? "");
    }

    [Fact]
    public void Onay_kipinde_isletme_adi_yazilmadan_evet_disabled()
    {
        var e = Ciz();
        const string muhurGerekcesi = "Kurulum tamamlandi, acilis kapatiliyor";
        MuhurGerekceYaz(e, muhurGerekcesi);
        e.WaitForAssertion(() => Assert.False(e.Find("#kur-muhurle").HasAttribute("disabled")));
        e.Find("#kur-muhurle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kur-onay-isletme-adi")));

        // Hiç yazılmadan: kilitli + sebep yazılı (işletme adı gösterilir).
        Assert.True(e.Find("#kur-onay-evet").HasAttribute("disabled"));
        Assert.Contains(IsletmeAdi, e.Find("#kur-onay-kilit").TextContent);

        // Birebir olmayan her yazım kilitli kalır ve alan altında Türkçe mesaj çıkar.
        foreach (var yanlis in new[] { "altın ışık kuyumcu", "Altın Işık", "Altin Isik Kuyumcu", "Başka Kuyumcu" })
        {
            e.Find("#kur-onay-isletme-adi").Input(yanlis);
            e.WaitForAssertion(() => Assert.True(e.Find("#kur-onay-evet").HasAttribute("disabled")));
            Assert.NotNull(e.Find("#kur-onay-isletme-adi-hata"));
        }
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/kurulum/muhurle"));

        // Birebir yazılınca açılır; gövde sabit "onay=true" değil, adı ve
        // AYRI mühür gerekçesini taşır.
        e.Find("#kur-onay-isletme-adi").Input(IsletmeAdi);
        e.WaitForAssertion(() => Assert.False(e.Find("#kur-onay-evet").HasAttribute("disabled")));
        e.Find("#kur-onay-evet").Click();
        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/kurulum/muhurle")));

        var govde = System.Text.Json.JsonDocument.Parse(
            istekler.Single(i => i.Yol.EndsWith("/kurulum/muhurle")).Govde).RootElement;
        Assert.Equal(IsletmeAdi, govde.GetProperty("isletmeAdiOnayi").GetString());
        Assert.Equal(muhurGerekcesi, govde.GetProperty("gerekce").GetString());
        Assert.True(govde.GetProperty("onay").GetBoolean());
    }

    [Fact]
    public void Onay_kipi_metni_geri_donulemezligi_ve_storno_yolunu_yazar()
    {
        var e = Ciz();
        MuhurGerekceYaz(e, "Kurulum tamamlandi, acilis kapatiliyor");
        e.WaitForAssertion(() => Assert.False(e.Find("#kur-muhurle").HasAttribute("disabled")));
        e.Find("#kur-muhurle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kur-onay-metni")));

        var metin = e.Find("#kur-onay-metni").TextContent;
        Assert.Contains("geri alınamaz", metin, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tek düzeltme yolu storno", metin, StringComparison.OrdinalIgnoreCase);
    }

    // ── UX-S27 — kaydet sonrası SONUÇ görünür ───────────────────────────────

    [Fact]
    public void Uygulama_sonrasi_BASARI_BANDI_sunucunun_dondurdugu_sayiyi_yazar()
    {
        provaJson = ProvaTemiz;
        var e = Ciz();
        SatirKur(e);
        GerekceYaz(e, "Eski programdan devreden acilis bakiyesi");
        e.Find("#kur-prova").Click();
        e.WaitForAssertion(() => Assert.False(e.Find("#kur-uygula").HasAttribute("disabled")));

        e.Find("#kur-uygula").Click();
        e.WaitForAssertion(() => Assert.Contains("1", e.Find("#kur-basari").TextContent));
    }

    // ── P-68 — mühür kilidi gerekçesi doğruluğu + bağlantılar + döviz süzgeci ─

    /// <summary>
    /// Dört zorunlu adımın DÖRDÜ de eksik; sunucu kabul edilen para birimi
    /// listesini bildiriyor (ekran sabit liste yazmaz).
    /// </summary>
    private const string DurumEksik = """
        {"muhurlendi":false,"muhurTarihi":null,"muhurleyenKullaniciId":null,"muhurGerekcesi":null,
         "acilisPenceresiBitis":"2026-10-17T00:00:00Z","acilisPenceresiAcik":true,
         "muhurlenebilir":false,"eksikAdimlar":["ISLETME","KULLANICILAR","KUR","KASA_BANKA"],
         "atlananAdimlar":[],
         "adimlar":[
           {"anahtar":"ISLETME","bitti":false,"zorunlu":true,"atlandi":false},
           {"anahtar":"KULLANICILAR","bitti":false,"zorunlu":true,"atlandi":false},
           {"anahtar":"KUR","bitti":false,"zorunlu":true,"atlandi":false},
           {"anahtar":"KASA_BANKA","bitti":false,"zorunlu":true,"atlandi":false},
           {"anahtar":"CARI_ACILIS","bitti":false,"zorunlu":false,"atlandi":false},
           {"anahtar":"STOK_ACILIS","bitti":false,"zorunlu":false,"atlandi":false}],
         "sayilar":{"sube":0,"kullanici":0,"kur":1,"kasa":0,"banka":0,
                    "acilisCari":0,"acilisKasa":0,"acilisBanka":0},
         "maksSatir":500,"gerekceAsgari":20,"isletmeAdi":"Altın Işık Kuyumcu",
         "acilisParaBirimleri":["TRY"]}
        """;

    /// <summary>Sunucu yanıtındaki eksikAdimlar dizisi — beklenen sayı JSON'dan okunur (elle sayı yazılmaz).</summary>
    private static List<string> SunucununEksikleri(string json)
    {
        using var d = System.Text.Json.JsonDocument.Parse(json);
        return [.. d.RootElement.GetProperty("eksikAdimlar").EnumerateArray().Select(x => x.GetString()!)];
    }

    private static List<AngleSharp.Dom.IElement> KilitMaddeleri(IRenderedComponent<Kuyumcu.Web.Pages.Kurulum> e) =>
        [.. e.Find("#kur-muhur-kilit").QuerySelectorAll("li")];

    [Fact]
    public void Kilit_gerekcesi_sunucunun_eksik_adimlariyla_birebir_esler()
    {
        durumJson = DurumEksik;
        var eksikler = SunucununEksikleri(DurumEksik);
        var e = Ciz();

        // Gerekçe boş → kısa: eksikAdimlar + 1.
        var maddeler = KilitMaddeleri(e);
        Assert.Equal(eksikler.Count + 1, maddeler.Count);
        // Adım maddeleri SUNUCUNUN dizisiyle aynı küme ve aynı sırada.
        Assert.Equal(eksikler.Select(a => $"kur-muhur-eksik-{a}"),
            maddeler.Where(m => m.Id != "kur-muhur-eksik-gerekce").Select(m => m.Id));
        Assert.Single(maddeler, m => m.Id == "kur-muhur-eksik-gerekce");

        // Gerekçe yeterli → yalnız eksikAdimlar kalır (birebir).
        MuhurGerekceYaz(e, new string('m', 20));
        e.WaitForAssertion(() => Assert.Equal(eksikler.Count, KilitMaddeleri(e).Count));
        Assert.Empty(e.FindAll("#kur-muhur-eksik-gerekce"));
        // Ham anahtar ekrana basılmaz (UX-S31): madde metni çevrilmiş addır.
        Assert.DoesNotContain("KASA_BANKA", e.Find("#kur-muhur-kilit").TextContent);
    }

    [Fact]
    public void Eksik_adim_maddeleri_ilgili_rotaya_baglanir()
    {
        durumJson = DurumEksik;
        var e = Ciz();
        var beklenen = new Dictionary<string, string>
        {
            ["ISLETME"] = "/isletme",
            ["KULLANICILAR"] = "/kullanicilar",
            ["KUR"] = "/kur",
            ["KASA_BANKA"] = "/kasalar",
        };

        foreach (var adim in SunucununEksikleri(DurumEksik))
        {
            var li = e.Find($"#kur-muhur-eksik-{adim}");
            var a = li.QuerySelector("a");
            Assert.NotNull(a);
            var href = a!.GetAttribute("href");
            Assert.False(string.IsNullOrWhiteSpace(href), $"{adim} bağlantısı boş");
            Assert.Equal(beklenen[adim], href);
        }
    }

    [Fact]
    public void GOCC_T2_eksik_kasa_adiminda_git_ve_atla_AYNI_eylem_sarmalayicisinda()
    {
        // PAKET GOC-C tur 2 (hasım ORTA, K9): "Bu adıma git" ile "Bu adımı atla" aynı grid-area'ya iki ayrı
        // öğe olarak konup üst üste biniyordu. İkisi de TEK sarmalayıcıda (eylem alanını o alır), sıra: git → atla.
        durumJson = DurumEksik;
        var e = Ciz();
        var li = e.Find("#kur-adim-KASA_BANKA");
        var sarma = li.QuerySelector(":scope > .kur-adim-eylemler");
        Assert.NotNull(sarma);
        var cocuklar = sarma!.Children.Select(c => c.Id).ToList();
        Assert.Equal(["kur-adim-KASA_BANKA-git", "kur-adim-KASA_BANKA-atla"], cocuklar);
        Assert.Empty(li.QuerySelectorAll(":scope > a, :scope > button"));   // eylem doğrudan li çocuğu değil
        var css = File.ReadAllText(Path.Combine(Toplu1Ortak.WebKok(), "wwwroot", "css", "toplu4.css"));
        var i = css.IndexOf("#kur-adimlar .sade-adim-listesi > li > .kur-adim-eylemler {", StringComparison.Ordinal);
        Assert.True(i > 0);
        var blok = css[i..css.IndexOf('}', i)];
        Assert.Contains("grid-area: eylem", blok);
        Assert.Contains("flex-wrap: wrap", blok);
    }

    [Fact]
    public void Kaydediliyor_durumu_eksik_adim_gibi_listelenmez()
    {
        durumJson = DurumEksik;
        var eksikler = SunucununEksikleri(DurumEksik);
        var e = Ciz();
        MuhurGerekceYaz(e, new string('m', 20));
        e.WaitForAssertion(() => Assert.Equal(eksikler.Count, KilitMaddeleri(e).Count));
        Assert.Empty(e.FindAll("#kur-muhur-kaydediliyor"));

        // Kaydetme sürerken (PATCH yanıtı bekletiliyor) …
        patchBekletici = new TaskCompletionSource<bool>();
        e.Find("#kur-adim-KUR-atla").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kur-muhur-kaydediliyor")));

        // … "Kaydediliyor" AYRI satırdır; madde sayısı DEĞİŞMEZ ve hiçbir madde onu taşımaz.
        var maddeler = KilitMaddeleri(e);
        Assert.Equal(eksikler.Count, maddeler.Count);
        var kaydediliyorMetni = e.Find("#kur-muhur-kaydediliyor").TextContent.Trim();
        Assert.DoesNotContain(maddeler, m => m.TextContent.Contains(kaydediliyorMetni));
        Assert.Null(e.Find("#kur-muhur-kaydediliyor").Closest("li"));

        patchBekletici.SetResult(true);
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#kur-muhur-kaydediliyor")));
        Assert.Equal(eksikler.Count, KilitMaddeleri(e).Count);
    }

    [Fact]
    public void Kabul_edilmeyen_para_birimli_kasa_hedef_seciciye_girmez()
    {
        durumJson = DurumEksik;
        kasalarJson = """
            [{"id":"k1","ad":"Merkez Kasa","aktif":true,"paraBirimi":"TRY"},
             {"id":"k2","ad":"Döviz Kasası","aktif":true,"paraBirimi":"USD"}]
            """;
        var e = Ciz();
        SatirKur(e);

        // Döviz kasası seçiciye GİRMEDİ: tek seçenek kaldı → seçici gizli, TL kasa yazılı (UX-S22).
        Assert.Equal("Merkez Kasa", e.Find("#kur-satir-0-hedef-tek").TextContent.Trim());
        Assert.Empty(e.FindAll("#kur-satir-0-hedef"));
        Assert.DoesNotContain("Döviz Kasası", e.Find("#kur-acilis-tablo").TextContent);

        // Sessiz kırpma yok (UX-S29): dışarıda kalan kasa gerekçesiyle yazılı,
        // kabul edilen birim SUNUCUDAN gelen listeden okunur.
        var not = e.Find("#kur-acilis-doviz-k2");
        Assert.Contains("Döviz Kasası", not.TextContent);
        Assert.Contains("USD", not.TextContent);
        // KAT-1 (K5): kabul edilen TRY ekranda ₺ işaretiyle yazılır; USD kodu aynen.
        Assert.Contains("\u20ba", not.TextContent);
        Assert.DoesNotContain("TRY", not.TextContent);
        Assert.Empty(e.FindAll("#kur-acilis-doviz-k1"));
    }

    [Fact]
    public void Kilit_kutusu_role_status_tasir()
    {
        durumJson = DurumEksik;
        var e = Ciz();
        Assert.Equal("status", e.Find("#kur-muhur-kilit").GetAttribute("role"));
    }

    // ── PAKET Q10-5 (QA turu 10 /kurulum: basit modda K-24, KMH, storno, cari) ──

    [Fact]
    public void Q105_Basit_modda_acilis_kurali_ic_kod_ve_jargon_tasimaz()
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => true);
        var e = Ciz(profesyonel: false);
        var kural = e.Find("#kur-tl-kural").TextContent.Trim();
        Assert.Equal(Kuyumcu.Core.I18n.Q105Sozlugu.Tr.KurulumNegatifAcilisBasit, kural);
        Assert.DoesNotContain("K-24", kural);
        Assert.DoesNotContain("KMH", kural);
        Assert.DoesNotContain("cari", kural, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Q105_Basit_modda_gerekce_kilit_sebebi_denetim_izi_demez()
    {
        // Doğrulama turu (24.09, tr-TR küçük harfle K8): /kurulum BASİT'te
        // "…dışa aktarım denetim izine bu gerekçeyle yazılır" görünüyordu.
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => true);
        var e = Ciz(profesyonel: false);
        var metin = string.Concat(e.Nodes.Select(n => n.TextContent));
        Assert.Contains(string.Format(Kuyumcu.Core.I18n.Q105Sozlugu.Tr.GerekceKisaBasit, 20), metin);
        Assert.DoesNotContain("denetim izi", metin.ToLower(new System.Globalization.CultureInfo("tr-TR")));
    }

    [Fact]
    public void Q105_Profesyonel_modda_acilis_kurali_ayni_kalir()
    {
        // TOPLU-2 (KESIF-P8 açık kalanı, karar 2): kural metni AYNI, yalnız iç kalem kodu "(K-24)" ekrandan
        // çıktı — PRO metni Toplu2Sozlugu'ndan okunur (HUB değişmedi).
        var e = Ciz();
        Assert.Equal(Kuyumcu.Core.I18n.Toplu2Sozlugu.Tr.KurulumNegatifAcilisPro,
            e.Find("#kur-tl-kural").TextContent.Trim());
        Assert.DoesNotContain("K-24", e.Find("#kur-tl-kural").TextContent);
    }

    // ═══ ORTAK-1 · KEŞİF #4 (30.09, KRİTİK) — açılış tutarı "1.500" ═══════════════════════════════
    // Kapısız: "1.5" ara değeri satırda kalıyor; kutu hata yazarken prova/uygula 1,5 TL açılış yazabilirdi.
    [Fact]
    public void OK1_K4_acilis_tutari_belirsiz_1_500_prova_KILITLI_sebep_yazar()
    {
        var e = Ciz();
        SatirKur(e);
        GerekceYaz(e, "Eski programdan devreden acilis bakiyesi");
        foreach (var a in new[] { "1", "1.", "1.5", "1.50", "1.500" }) e.Find("#kur-satir-0-tl").Input(a);
        e.Find("#kur-satir-0-tl").Blur();
        Assert.Equal("1.500", e.Find("#kur-satir-0-tl").GetAttribute("value"));
        Assert.True(e.Find("#kur-prova").HasAttribute("disabled"));
        Assert.Contains(e.Find("label[for='kur-satir-0-tl']").TextContent.Replace("*", "").Split("(isteğe")[0].Trim(),
                        e.Find("#kur-prova-kilit").TextContent);
        var once = istekler.Count;
        e.Find("#kur-prova").Click();
        Assert.Equal(once, istekler.Count);
        e.Find("#kur-satir-0-tl").Input("1500");
        Assert.False(e.Find("#kur-prova").HasAttribute("disabled"));
    }
}
