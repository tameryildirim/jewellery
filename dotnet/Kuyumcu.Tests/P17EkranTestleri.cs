using Bunit;
using Kuyumcu.Contracts.Cikti;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-17 — EKRAN TESTLERİ [bUnit]
//
// ⚠ Sahte API '{}' varsayılanıyla BIRAKILMADI: bu paketin dokunduğu her uca
//   SUNUCU SÖZLEŞMESİYLE (OrtakBelge / PanoGorunum / PanoAyarlariGorunum)
//   birebir alan adı ve tipinde gerçekçi yanıt verildi.
//
// Sınananlar:
//   1. Dört belge türü de TEK şablondan (A4Belge) çizilir
//   2. Geçersiz tür → hata bandı; sessiz boş sayfa YOK (UX-S28)
//   3. Sunucu hatası boş duruma ÇEVRİLMEZ (UX-S28)
//   4. Yazdır düğmesi: belge yokken KAPALI ve sebebi YAZILI (UX-S30)
//   5. Her düğme ya istek atar ya disabled + sebep (DoD EK md.1)
//   6. Storno uyarısı kâğıda basılan gövdededir (yazdirma-gizle DEĞİL)
//   7. İşletme kimliği eksikse DegismezIpucu + işletme kartı yolu (UX-S25)
//   8. Vitrin: marj YÖNETİM tablosunda, panoda YOK
//   9. Vitrin: atlanan kalem sebebiyle görünür (UX-S28/md.1)
//  10. Vitrin: kaydet → PATCH doğru gövdeyle gider, başarı bandı + pano yenilenir
//  11. Vitrin: 403'te ayar tablosu yerine SEBEP yazar (UX-S30)
//  12. Vitrin: sıra aralık dışıysa ALAN ALTI mesaj, istek GİTMEZ
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P17CiktiEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly SahteApi sahte = new();

    public P17CiktiEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        sahte.Kayit = istekler;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(sahte) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        public List<(string, string, string)> Kayit = [];
        public bool Hata;
        public bool KimlikEksik;
        public bool Stornolu;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            Kayit.Add((istek.Method.Method, yol, govde));

            if (Hata)
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(
                        """{"message":"Sunucu hatası: belge okunamadı"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            var duzenleyen = KimlikEksik
                ? """{"unvan":"Test Kuyumcu","sube":"Merkez","adres":null,"vergiDairesi":null,"vergiNo":null,"telefon":null}"""
                : """{"unvan":"Test Kuyumcu","sube":"Merkez","adres":"Kuyumcular Çarşısı 1","vergiDairesi":"Beyazıt","vergiNo":"1234567890","telefon":"02120000000"}""";
            var uyari = Stornolu ? "\"BU KAYIT STORNO EDİLMİŞTİR — geçerli bir belge değildir.\"" : "null";

            var y = $$"""
            {"tur":"SATIS_FISI","belgeAdi":"SATIŞ FİŞİ","belgeNo":"01ABCDEF","dayanak":null,
             "duzenlemeTarihi":"2026-09-21T08:00:00Z",
             "duzenleyen":{{duzenleyen}},
             "karsi":{"baslik":"Müşteri","ad":"Ayşe Yılmaz","vergiNo":null,"adres":null,"telefon":null},
             "bolumler":[{"baslik":"Kalemler","sutunBasliklari":["Tür","Açıklama","Has gram","Tutar"],
               "satirlar":[{"hucreler":["Satış","Test bilezik","5.000","1000.00"],"vurgu":false}],
               "not":null}],
             "tutarlar":[{"etiket":"Toplam tutar","deger":1000.00,"paraBirimi":"TRY","toplam":true},
                         {"etiket":"Toplam has gram","deger":5.000,"paraBirimi":"HAS_GRAM","toplam":false}],
             "imzaBasliklari":["Satıcı","Alıcı"],"not":"P-17 test satırı","uyari":{{uyari}}}
            """;

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Cikti> Ekran(string tur = BelgeTurleri.SatisFisi) =>
        Render<Kuyumcu.Web.Pages.Cikti>(p => p.Add(x => x.Tur, tur).Add(x => x.Id, "01ABCDEF"));

    [Fact]
    public void Belge_ORTAK_sablondan_cizilir_ve_ucu_cagirir()
    {
        var e = Ekran();
        e.WaitForElement("#cikti-belge");

        Assert.Single(istekler);
        Assert.Contains("/cikti/SATIS_FISI/01ABCDEF", istekler[0].Yol, StringComparison.Ordinal);
        Assert.Equal("01ABCDEF", e.Find("#cikti-no").TextContent.Trim());
        Assert.Contains("Ayşe Yılmaz", e.Find("#cikti-karsi").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Has_gram_tutari_HAM_KOD_ile_BASILMAZ()
    {
        var e = Ekran();
        e.WaitForElement("#cikti-belge");
        // UX-S31: "HAS_GRAM" bir para birimi kodu değildir, ekrana basılmaz.
        Assert.DoesNotContain("HAS_GRAM", e.Find("#cikti-belge").TextContent, StringComparison.Ordinal);
        Assert.Contains("has", e.Find("#cikti-belge").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Gecersiz_tur_HATA_BANDI_gosterir_ve_sunucuya_GITMEZ()
    {
        var e = Ekran("SACMA_TUR");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#cikti-hata")));
        // Anayasa md.1: bilinmeyen tür sessizce boş sayfa göstermez.
        Assert.Contains("SACMA_TUR", e.Find("#cikti-hata").TextContent, StringComparison.Ordinal);
        Assert.Empty(istekler);
        Assert.Empty(e.FindAll("#cikti-belge"));
    }

    [Fact]
    public void Sunucu_hatasi_BOS_DURUMA_cevrilmez()
    {
        sahte.Hata = true;
        var e = Ekran();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#cikti-hata")));
        // UX-S28: hata bandı ayrıdır; belge gövdesi hiç çizilmez ama "kayıt yok" da denmez.
        Assert.Contains("belge okunamadı", e.Find("#cikti-hata").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Yazdir_dugmesi_belge_yokken_KAPALI_ve_sebebi_YAZILI()
    {
        sahte.Hata = true;
        var e = Ekran();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#cikti-yazdir")));

        Assert.True(e.Find("#cikti-yazdir").HasAttribute("disabled"));
        // UX-S30: kilitli düğmenin sebebi yazılıdır.
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#cikti-yazdir-kilit").TextContent));
    }

    [Fact]
    public void Belge_gelince_yazdir_dugmesi_ACIK_ve_kilit_metni_YOK()
    {
        var e = Ekran();
        e.WaitForElement("#cikti-belge");
        Assert.False(e.Find("#cikti-yazdir").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#cikti-yazdir-kilit"));
    }

    [Fact]
    public void Her_dugme_ya_istek_atar_ya_disabled_tasir()
    {
        // DoD EK (kurul tur 1) md.1 — "tıklanınca hiçbir şey olmayan" düğme yok.
        var e = Ekran();
        e.WaitForElement("#cikti-belge");
        foreach (var d in e.FindAll("button"))
            Assert.True(d.HasAttribute("disabled") || d.HasAttribute("blazor:onclick"),
                $"#{d.Id}: ne disabled ne @onclick taşıyor");
    }

    [Fact]
    public void Storno_uyarisi_KAGIDA_basilir_ekranda_gizlenmez()
    {
        sahte.Stornolu = true;
        var e = Ekran();
        e.WaitForElement("#cikti-uyari");
        // Uyarı belge GÖVDESİNDE olmalı; 'yazdirma-gizle' sınıfı taşırsa kâğıda çıkmaz.
        Assert.DoesNotContain("yazdirma-gizle", e.Find("#cikti-uyari").ClassName ?? "", StringComparison.Ordinal);
        Assert.Contains("STORNO", e.Find("#cikti-uyari").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Isletme_kimligi_eksikse_DUZELTME_YOLU_gosterilir()
    {
        sahte.KimlikEksik = true;
        var e = Ekran();
        e.WaitForElement("#cikti-belge");

        // UX-S25: boş alan uydurulmaz; işletme kartına giden yol gösterilir.
        Assert.NotNull(e.Find("#degismez-ipucu-IsletmeKimligi"));
        Assert.NotNull(e.Find("#cikti-isletme-karti"));
    }

    [Fact]
    public void Turetilmis_belgede_DUZENLEME_yerine_gerekce_vardir()
    {
        var e = Ekran();
        e.WaitForElement("#cikti-belge");
        // UX-S25: türetilmiş çıktı düzenlenmez; gerekçesi ve doğru yolu yazılır.
        Assert.NotNull(e.Find("#degismez-ipucu-CiktiBelgesi"));
    }
}

public sealed class P17VitrinEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly SahteApi sahte = new();

    public P17VitrinEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        sahte.Kayit = istekler;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(sahte) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        public List<(string, string, string)> Kayit = [];
        public bool AyarYasak;

        /// <summary>Sunucunun PanoGorunum'u ile BİREBİR — marj alanı YOK.</summary>
        private const string Pano = """
        {"kaynak":"ALTINKAYNAK","zaman":"2026-09-21T08:00:00Z",
         "satirlar":[
           {"kod":"GRAM","etiket":"Gram Altın","alis":6800.00,"satis":6900.00,"birim":"GRAM"},
           {"kod":"CEYREK_YENI","etiket":"Yeni Çeyrek","alis":11500.00,"satis":11800.00,"birim":"ADET"}],
         "atlananlar":[{"kod":"HAMIT","ad":"Hamit Altın","sebep":"TERS_MAKAS"}],
         "tavanSatir":16,"okunabilirlikEsigi":12,"okunabilirlikUyarisi":false}
        """;

        private const string Ayarlar = """
        {"ayarlar":[
           {"kod":"GRAM","ad":"Gram Altın","etiket":null,"sira":0,"alisMarjTl":-10.00,"satisMarjTl":20.00,
            "yuvarlamaAdimi":10,"aktif":true,"ozel":true},
           {"kod":"CEYREK_YENI","ad":"Yeni Çeyrek","etiket":null,"sira":1,"alisMarjTl":0,"satisMarjTl":0,
            "yuvarlamaAdimi":10,"aktif":true,"ozel":false}],
         "yuvarlamaAdimlari":[0,1,5,10,50,100],"tavanSatir":16,"okunabilirlikEsigi":12,"aktifSayisi":2}
        """;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            Kayit.Add((istek.Method.Method, yol, govde));

            if (yol.Contains("/pano/ayarlar", StringComparison.Ordinal) && AyarYasak)
                return new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)
                {
                    Content = new StringContent("""{"message":"yetki yok"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            var y = yol.Contains("/pano/guncel", StringComparison.Ordinal) ? Pano : Ayarlar;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Vitrin> Ekran()
    {
        var e = Render<Kuyumcu.Web.Pages.Vitrin>();
        e.WaitForElement("#vitrin-pano");
        GocC2Ortak.AyarlariAc(e);   // GOC-C-2: ayar tablosu PRO'da da kapalı katlanırla gelir (hakem K4 /vitrin 375) — test önce açar; beklenen GEVŞETİLMEDİ.
        return e;
    }

    [Fact]
    public void Pano_satirlari_cizilir_ve_MARJ_gorunmez()
    {
        var e = Ekran();
        var pano = e.Find("#vitrin-pano").TextContent;
        Assert.Contains("Gram Altın", pano, StringComparison.Ordinal);

        // Marj müşteriye dönük panoda YOK; yalnız yönetim tablosunda.
        var marj = Bicim.Tl(-10.00m);
        Assert.DoesNotContain(marj, pano, StringComparison.Ordinal);
        Assert.Contains(marj, e.Find("#vitrin-ayarlar").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Atlanan_kalem_SEBEBIYLE_gorunur_ve_HAM_KOD_basilmaz()
    {
        var e = Ekran();
        var metin = e.Find("#vitrin-atlanan-HAMIT").TextContent;
        Assert.Contains("Hamit", metin, StringComparison.Ordinal);
        // UX-S31: "TERS_MAKAS" ham kodu ekrana basılmaz, çevrilir.
        Assert.DoesNotContain("TERS_MAKAS", metin, StringComparison.Ordinal);
        Assert.Contains("satış", metin, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sunucu_satir_tavani_EKRANDA_yazar()
    {
        var e = Ekran();
        // UX-S29: sessiz kırpma yok.
        Assert.Contains("16", e.Find("#vitrin-tavan").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Duzenle_PATCH_gonderir_basari_bandi_yazar_ve_PANOYU_yeniler()
    {
        var e = Ekran();
        e.Find("#vitrin-duzenle-GRAM").Click();
        e.WaitForElement("#pano-satis-marj");

        e.Find("#pano-satis-marj").Input("45");
        istekler.Clear();
        e.Find("#cekmece-pano-kalem-kaydet").Click();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#vitrin-basari")));

        var patch = istekler.Single(i => i.Metot == "PATCH");
        Assert.Contains("/pano/ayarlar/GRAM", patch.Yol, StringComparison.Ordinal);
        Assert.Contains("\"satisMarjTl\":45", patch.Govde.Replace(" ", ""), StringComparison.Ordinal);
        // UX-S23: etkisi AYNI ekranda görünür — pano yeniden çekilir.
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("/pano/guncel", StringComparison.Ordinal));
    }

    // ── PAKET P-78 — Vitrin marj + yuvarlama tr-TR tek merkez ──
    [Fact]
    public void P78_vitrin_1_5_marj_ve_binlikli_marj_TURKCE_okunur_yuvarlama_gider()
    {
        var e = Ekran();
        e.Find("#vitrin-duzenle-GRAM").Click();
        e.WaitForElement("#pano-satis-marj");

        e.Find("#pano-satis-marj").Input("1,5");            // eskiden Invariant yolda 15
        e.Find("#pano-alis-marj").Input("-1.250,5");
        e.Find("#pano-yuvarlama").Click();
        e.Find("#pano-yuvarlama-liste [data-deger='5']").Click();
        istekler.Clear();
        e.Find("#cekmece-pano-kalem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH"));

        var patch = istekler.Single(i => i.Metot == "PATCH");
        using var j = System.Text.Json.JsonDocument.Parse(patch.Govde);
        Assert.Equal(1.5m, j.RootElement.GetProperty("satisMarjTl").GetDecimal());
        Assert.Equal(-1250.5m, j.RootElement.GetProperty("alisMarjTl").GetDecimal());
        Assert.Equal(5m, j.RootElement.GetProperty("yuvarlamaAdimi").GetDecimal());
    }

    [Fact]
    public void Gecersiz_sira_ALAN_ALTI_mesaj_verir_ve_istek_GITMEZ()
    {
        var e = Ekran();
        e.Find("#vitrin-duzenle-GRAM").Click();
        e.WaitForElement("#pano-sira");

        e.Find("#pano-sira").Input("500");
        istekler.Clear();
        e.Find("#cekmece-pano-kalem-kaydet").Click();

        // Sessiz etkisizlik YASAK: alan altı mesaj var, sunucuya gidilmedi.
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".alan-hata, [id^='alan-hata']")));
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
    }

    [Fact]
    public void Varsayilan_kalemde_varsayilana_don_KAPALI_ve_sebebi_YAZILI()
    {
        var e = Ekran();
        e.Find("#vitrin-duzenle-CEYREK_YENI").Click();
        e.WaitForElement("#pano-varsayilana-don");

        // ozel=false → geri dönecek bir ayar yok; UX-S30: sebep yazılı.
        Assert.True(e.Find("#pano-varsayilana-don").HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#pano-varsayilana-don-kilit").TextContent));
    }

    [Fact]
    public void Ozel_kalemde_varsayilana_don_ACIK_ve_istek_atar()
    {
        var e = Ekran();
        e.Find("#vitrin-duzenle-GRAM").Click();
        e.WaitForElement("#pano-varsayilana-don");
        Assert.False(e.Find("#pano-varsayilana-don").HasAttribute("disabled"));

        istekler.Clear();
        e.Find("#pano-varsayilana-don").Click();
        // PAKET P-65 (E49): yıkıcı eylem artık ORTAK ONAY KİPİNDEN geçer — önce istek YOK.
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        e.Find("#vitrin-varsayilana-don-evet-GRAM").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "POST" && i.Yol.Contains("/varsayilana-don", StringComparison.Ordinal)));
    }

    [Fact]
    public void Ayar_ucu_403_donerse_SEBEP_yazilir_tablo_cizilmez()
    {
        sahte.AyarYasak = true;
        var e = Render<Kuyumcu.Web.Pages.Vitrin>();
        e.WaitForElement("#vitrin-ayar-kapali");

        Assert.Empty(e.FindAll("#vitrin-ayarlar"));
        // UX-S30: görünmez kural olmaz — neden görünmediği yazılı.
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#vitrin-ayar-kapali").TextContent));
        // UX-S28: pano yine çizilir; 403 panoyu boşaltmaz.
        Assert.NotNull(e.Find("#vitrin-pano"));
    }

    [Fact]
    public void Her_dugme_ya_istek_atar_ya_disabled_tasir()
    {
        var e = Ekran();
        foreach (var d in e.FindAll("button"))
            Assert.True(d.HasAttribute("disabled") || d.HasAttribute("blazor:onclick"),
                $"#{d.Id}: ne disabled ne @onclick taşıyor");
    }

    // KATMAN-1b: pano çekmecesinde "Yuvarlama adımı" etiketi TEK kez görünür
    // (Secici etiketini kendisi çizer; sayfada ikinci <label> yazılmaz).
    [Fact]
    public void KATMAN1b_pano_cekmecesinde_yuvarlama_etiketi_TEK_kez()
    {
        var e = Ekran();
        e.Find("#vitrin-duzenle-GRAM").Click();
        e.WaitForElement("#pano-yuvarlama");

        var cekmece = e.Find("#cekmece-pano-kalem");
        var etiketler = cekmece.QuerySelectorAll("label[for=\"pano-yuvarlama\"]");
        Assert.Single(etiketler);
        var metin = Kuyumcu.Core.I18n.Sozlukler.Tr.Pano.Yuvarlama;
        Assert.Equal(1, cekmece.QuerySelectorAll("label").Count(l => l.TextContent.Trim().TrimEnd('*').Trim() == metin));
    }
}
