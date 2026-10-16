using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-15 — KABUK EKRAN TESTLERİ [bUnit]
// (kimlikler Y-29 · Y-5 · B-25)
//
// Ölçülen davranışlar:
//   • B-25  — kullanıcı ULID GÖRMEZ; seçtiğinde ULID yalnız gövdeye gider.
//   • UX-S29 — sunucu sınırı ekranda YAZILI; sınıra değince uyarı çıkar.
//   • UX-S30 — kilitli "Ara" düğmesinin SEBEBİ yazılı.
//   • UX-S28 — sunucu hatası "kayıt yok"a ÇEVRİLMEZ.
//   • UX-S22 — tek şube varsa seçici GÖSTERİLMEZ, salt okunur satır çıkar.
//   • DoD EK md.1 — her düğme ya istek atar ya disabled'dır.
//
// ⚠ Sahte API'nin "{}" varsayılanı JsonException doğurur; yeni uca GERÇEKÇİ
//   yanıt tanımlanır (bUnit sözleşmesi).
// ⚠ KayitSecici.BeklemeMs testte 0'a çekilir: 250 ms'lik gerçek bekleme
//   testleri KARARSIZ yapardı, davranış değil süre ölçülürdü.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KabukAramaEkranTestleri : BunitContext
{
    private readonly List<string> istekler = [];
    private int sonucSayisi = 2;
    private bool dahaVar;
    private HttpStatusCode aramaKodu = HttpStatusCode.OK;
    private int subeSayisi = 1;
    /// <summary>Sunucu rotası OLMAYAN bir sonuç da döndürsün mü? (DoD EK md.1 testi)</summary>
    private bool rotasizEkle;
    /// <summary>
    /// ŞUBE-KAPSAM-B: "tüm şubeler" yetkisi istemcide rol listesinden DEĞİL,
    /// sunucunun tercih yanıtından (gorunumSubesi.tumSubeler) okunur — sahte
    /// sunucu da bunu oturum rolüne göre söyler (SubeYetkisi ile aynı liste).
    /// </summary>
    private string oturumRolu = "SAHIP";

    public KabukAramaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        KayitSecici.BeklemeMs = 0;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(
            new SahteApi(istekler, () => sonucSayisi, () => dahaVar, () => aramaKodu,
                         () => subeSayisi, () => rotasizEkle, () => oturumRolu))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        // P-36b kapanışı (P-37 artığı): KenarCubugu favori/sıra tercihini bu
        // servisten okur; Program.cs'te kayıtlıydı, fikstürlerde DEĞİLDİ →
        // kabuk çizen her test DI hatasıyla kırmızıydı.
        Services.AddScoped<MenuTercihServisi>();
        KabukServisi.Ortak.GorunumSubesiYerelAta(null);
    }

    /// <summary>Seçilen kaydın gerçek ULID'i — ekranda GÖRÜNMEMESİ gerekir.</summary>
    private const string Ulid1 = "01J9ZK8QW7A0B1C2D3E4F5G6H7";

    private sealed class SahteApi(
        List<string> kayit, Func<int> sayi, Func<bool> daha,
        Func<HttpStatusCode> kod, Func<int> sube, Func<bool> rotasiz, Func<string> rol) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            kayit.Add(yol);

            if (yol.Contains("/arama", StringComparison.Ordinal) && kod() != HttpStatusCode.OK)
                return Task.FromResult(new HttpResponseMessage(kod())
                {
                    Content = new StringContent(
                        """{"hata":"Arama ucu cevap vermedi"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                });

            string govde;
            if (yol.Contains("/arama", StringComparison.Ordinal))
            {
                var n = sayi();
                var ogeler = string.Join(",", Enumerable.Range(0, n).Select(i =>
                    "{\"kimlik\":\"" + (i == 0 ? Ulid1 : "01J9ZK8QW7A0B1C2D3E4F5G6H" + i) + "\","
                    + "\"ad\":\"Ayşe Demir " + i + "\","
                    + "\"altBilgi\":\"0555111223" + i + "\","
                    + "\"tur\":\"CARI\",\"rota\":\"/cariler?cariId=x" + i + "\"}"));
                // Rotası OLMAYAN sonuç (fiş/fatura/dekont gerçeği): tıklanabilir
                // listeye GİRMEMELİ, yoksa "tıklanınca hiçbir şey olmayan düğme"
                // doğar (DoD EK md.1).
                if (rotasiz())
                    ogeler += (ogeler.Length > 0 ? "," : "")
                        + "{\"kimlik\":\"FISX\",\"ad\":\"Ayşe fişi\",\"altBilgi\":\"2 kalem\","
                        + "\"tur\":\"FIS\",\"rota\":null}";
                govde = "{\"sorgu\":\"ay\",\"tur\":\"CARI\",\"limit\":10,\"ofset\":0,"
                      + "\"sayi\":" + n + ",\"dahaVar\":" + (daha() ? "true" : "false")
                      + ",\"tavan\":50,\"sonuclar\":[" + ogeler + "]}";
            }
            else if (yol.Contains("/subeler", StringComparison.Ordinal))
            {
                govde = "[" + string.Join(",", Enumerable.Range(0, sube()).Select(i =>
                    "{\"id\":\"s" + i + "\",\"ad\":\"Şube " + i + "\",\"aktif\":true}")) + "]";
            }
            else if (yol.Contains("/yetkiler/benim", StringComparison.Ordinal))
            {
                govde = """{"moduller":["cari","urun","islem","kasa","banka","fatura"]}""";
            }
            else if (yol.Contains("/tercihler/benim", StringComparison.Ordinal))
            {
                var tum = rol() is "SAHIP" or "MUDUR" or "MUHASEBE" ? "true" : "false";
                // PATCH: sunucu yazılan değeri çözülmüş hâliyle geri döner (TUMU → null).
                string? sube = null;
                if (istek.Method == HttpMethod.Patch && istek.Content is not null)
                {
                    using var b = System.Text.Json.JsonDocument.Parse(istek.Content.ReadAsStringAsync().Result);
                    var v = b.RootElement.GetProperty("tercihler").GetProperty("gorunum-subesi").GetString();
                    sube = v is null or "TUMU" ? null : v;
                }
                var subeJson = sube is null ? "null" : "\"" + sube + "\"";
                govde = "{\"tercihler\":{},\"gorunumSubesi\":{\"kayitli\":" + subeJson + ",\"subeId\":" + subeJson + ","
                      + "\"tumSubeler\":" + tum + ",\"gecersizTercih\":false,\"deger\":\"TUMU\"}}";
            }
            else
            {
                govde = "{}";
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private void OturumAc(string rol = "SAHIP")
    {
        oturumRolu = rol;
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s0"));
    }

    // ── KayitSecici ──────────────────────────────────────────────────────────

    private IRenderedComponent<KayitSecici> SeciciCiz(string? deger = null)
    {
        var secilen = deger;
        return Render<KayitSecici>(p => p
            .Add(c => c.Tur, Kuyumcu.Contracts.Arama.AramaTurleri.Cari)
            .Add(c => c.ElemanId, "kt-cari")
            .Add(c => c.Etiket, "Cari")
            .Add(c => c.Deger, secilen)
            .Add(c => c.DegerChanged, (string? v) => secilen = v));
    }

    [Fact]
    public void Kisa_sorguda_Ara_dugmesi_KAPALI_ve_sebebi_YAZILI()
    {
        var e = SeciciCiz();

        // disabled="false" bir DİZGEDİR: assert niteliğin VARLIĞINA bakar.
        Assert.True(e.Find("#kt-cari-ara").HasAttribute("disabled"));
        Assert.Contains("2", e.Find("#kt-cari-durum").TextContent);
        // Kilitliyken SUNUCUYA GİDİLMEZ.
        Assert.DoesNotContain(istekler, y => y.Contains("/arama"));
    }

    [Fact]
    public void Yazinca_sunucuda_ARANIR_ve_sonuclar_cizilir()
    {
        var e = SeciciCiz();
        e.Find("#kt-cari").Input("ayş");

        e.WaitForAssertion(() =>
        {
            Assert.Contains(istekler, y => y.Contains("/arama") && y.Contains("tur=CARI"));
            Assert.Equal(2, e.FindAll("#kt-cari-sonuclar button").Count);
        });
    }

    /// <summary>B-25: sonuç listesinde 26 haneli kimlik EKRANA BASILMAZ.</summary>
    [Fact]
    public void Sonuc_listesinde_ULID_gorunmez()
    {
        var e = SeciciCiz();
        e.Find("#kt-cari").Input("ayş");

        e.WaitForAssertion(() =>
        {
            Assert.NotEmpty(e.FindAll("#kt-cari-sonuclar button"));
            Assert.DoesNotContain(Ulid1, e.Find("#kt-cari-sonuclar").TextContent, StringComparison.Ordinal);
        });
    }

    /// <summary>Seçim ULID'i GÖVDEYE taşır; ekranda yine ad + tür görünür.</summary>
    [Fact]
    public void Secim_ULIDi_govdeye_tasir_ekranda_AD_gosterir()
    {
        string? secilen = null;
        var e = Render<KayitSecici>(p => p
            .Add(c => c.Tur, Kuyumcu.Contracts.Arama.AramaTurleri.Cari)
            .Add(c => c.ElemanId, "kt-cari")
            .Add(c => c.Etiket, "Cari")
            .Add(c => c.DegerChanged, (string? v) => secilen = v));

        e.Find("#kt-cari").Input("ayş");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#kt-cari-sonuc-0")));
        e.Find("#kt-cari-sonuc-0").Click();

        e.WaitForAssertion(() =>
        {
            Assert.Equal(Ulid1, secilen);
            var secili = e.Find("#kt-cari-secili");
            Assert.Contains("Ayşe Demir 0", secili.TextContent);
            Assert.DoesNotContain(Ulid1, secili.TextContent, StringComparison.Ordinal);
            // Geri dönüş yolu VARDIR (UX-S25).
            Assert.NotNull(e.Find("#kt-cari-temizle"));
        });
    }

    [Fact]
    public void Temizle_secimi_kaldirir_ve_null_bildirir()
    {
        string? secilen = "dolu";
        var e = Render<KayitSecici>(p => p
            .Add(c => c.Tur, Kuyumcu.Contracts.Arama.AramaTurleri.Cari)
            .Add(c => c.ElemanId, "kt-cari")
            .Add(c => c.Etiket, "Cari")
            .Add(c => c.DegerChanged, (string? v) => secilen = v));

        e.Find("#kt-cari").Input("ayş");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#kt-cari-sonuc-0")));
        e.Find("#kt-cari-sonuc-0").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#kt-cari-temizle")));
        e.Find("#kt-cari-temizle").Click();

        e.WaitForAssertion(() =>
        {
            Assert.Null(secilen);
            Assert.NotNull(e.Find("#kt-cari"));   // arama kutusu geri geldi
        });
    }

    /// <summary>UX-S29: sınıra değince "daha fazlası var" uyarısı BASILIR.</summary>
    [Fact]
    public void Sinira_deginde_daha_var_uyarisi_cikar()
    {
        dahaVar = true;
        var e = SeciciCiz();
        e.Find("#kt-cari").Input("ayş");

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kt-cari-daha-var")));
    }

    /// <summary>UX-S28: sunucu hatası BOŞ DURUMA çevrilmez; bant ayrıdır.</summary>
    [Fact]
    public void Sunucu_hatasi_bos_duruma_CEVRILMEZ()
    {
        aramaKodu = HttpStatusCode.InternalServerError;
        var e = SeciciCiz();
        e.Find("#kt-cari").Input("ayş");

        e.WaitForAssertion(() =>
        {
            Assert.NotNull(e.Find("#kt-cari-hata"));
            Assert.Empty(e.FindAll(".bos-durum"));
        });
    }

    [Fact]
    public void Gercekten_bos_sonucta_BOS_DURUM_cikar_hata_bandi_cikmaz()
    {
        sonucSayisi = 0;
        var e = SeciciCiz();
        e.Find("#kt-cari").Input("ayş");

        e.WaitForAssertion(() =>
        {
            Assert.Empty(e.FindAll("#kt-cari-hata"));
            Assert.Contains("Eşleşen", e.Markup, StringComparison.Ordinal);
        });
    }

    // ── SubeSecici ───────────────────────────────────────────────────────────

    /// <summary>
    /// UX-S22: tek şube varsa SEÇİCİ YOKTUR, salt okunur satır vardır.
    /// S1-g YENİDEN YAZIM — ESKİ: salt okunur satır (#ust-sube-sabit) + "Şube
    /// kapsamı" etiketi + rozet ÜST BARDA çizilirdi. YENİ (tuval
    /// Profesyonel.dc.html satır 52 "Sahip · Merkez şube"; üst barda şube yok):
    /// üst bar yüzeyi HİÇBİR ŞEY çizmez; satır kullanıcı kartındadır ve
    /// şubenin ADINI yazar ("Tüm şubeler" tahmini değil).
    /// </summary>
    [Fact]
    public void Tek_subede_secici_CIZILMEZ_salt_okunur_satir_cikar()
    {
        subeSayisi = 1;
        OturumAc("SAHIP");
        var ust = Render<SubeSecici>();
        var kart = Render<SubeSecici>(p => p.Add(x => x.KartIci, true));

        kart.WaitForAssertion(() =>
            Assert.Contains("Şube 0", kart.Find("#ust-sube-sabit").TextContent, StringComparison.Ordinal));
        Assert.Empty(ust.FindAll("#ust-sube-secim"));
        Assert.Empty(ust.FindAll("#ust-sube-sabit"));
        Assert.Empty(ust.FindAll("#ust-sube-secici"));
        Assert.DoesNotContain(Sozlukler.Tr.Kabuk.SubeKapsami, ust.Markup, StringComparison.Ordinal);
        Assert.Empty(kart.FindAll("#ust-sube-secim"));
    }

    [Fact]
    public void Coklu_subede_TUM_SUBELER_rolu_secici_gorur()
    {
        subeSayisi = 3;
        OturumAc("SAHIP");
        var e = Render<SubeSecici>();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ust-sube-secim")));
        // S1-g (kurucu bulgusu): etiket üst barda TEK kez yazar —
        // eskiden küçük etiket + seçicinin kendi etiketi = iki kez.
        // ŞUBE-KAPSAM-B tur 3: etiket sade soru "Hangi şube?" (eski "Şube kapsamı" teknikti).
        var metin = e.Find("#ust-sube-secici").TextContent;
        var kez = System.Text.RegularExpressions.Regex.Matches(
            metin, System.Text.RegularExpressions.Regex.Escape(SubeKapsamBSozlugu.Tr.HangiSube)).Count;
        Assert.Equal(1, kez);
        Assert.DoesNotContain(Sozlukler.Tr.Kabuk.SubeKapsami, metin, StringComparison.Ordinal);
    }

    /// <summary>
    /// Şubeye kilitli rol (TEZGAHTAR) çok şube olsa bile seçici GÖRMEZ:
    /// ekran, sunucunun vermeyeceği bir yetkiyi VAAT ETMEZ.
    /// </summary>
    [Fact]
    public void Sube_kilitli_rol_coklu_subede_bile_secici_GORMEZ()
    {
        subeSayisi = 3;
        OturumAc("TEZGAHTAR");
        var e = Render<SubeSecici>();
        // S1-g YENİDEN YAZIM: salt okunur satır üst bardan kullanıcı kartına
        // taşındı (tuval Profesyonel.dc.html satır 52); kilitli rol kendi
        // şubesinin ADINI görür.
        var kart = Render<SubeSecici>(p => p.Add(x => x.KartIci, true));

        kart.WaitForAssertion(() =>
            Assert.Contains("Şube 0", kart.Find("#ust-sube-sabit").TextContent, StringComparison.Ordinal));
        Assert.Empty(e.FindAll("#ust-sube-secim"));
        Assert.Empty(e.FindAll("#ust-sube-sabit"));
    }

    /// <summary>Seçilen şube kabuk durumuna yazılır; arama ucuna o gider.</summary>
    [Fact]
    public void Secilen_sube_kabuk_durumuna_yazilir_ve_arama_ucuna_gecer()
    {
        subeSayisi = 3;
        OturumAc("SAHIP");
        var sube = Render<SubeSecici>();
        sube.WaitForAssertion(() => Assert.NotNull(sube.Find("#ust-sube-secim")));
        // P-36: ham <select> ortak <Secici> bileşenine göç etti. id BİREBİR
        // aynı (ust-sube-secim); seçim artık Change ile değil, listeyi AÇIP
        // öğeye tıklayarak yapılır. SÖZLEŞME DEĞİŞMEDİ: seçim kabuk durumuna
        // yazılır ve arama ucuna sube= olarak geçer — test onu ölçmeye devam eder.
        sube.Find("#ust-sube-secim").Click();
        sube.WaitForAssertion(() => Assert.NotNull(sube.Find("#ust-sube-secim-liste")));
        var s1 = sube.FindAll("#ust-sube-secim-liste li[role=option]")
                     .First(o => o.TextContent.Contains("Şube 1"));
        s1.Click();

        // ŞUBE-KAPSAM-B: seçim sunucuya yazılır (asenkron) ve OTURUMA bağlıdır —
        // paralel test sınıflarının seçimi bu testin değerini ezemez.
        var oturum = Services.GetRequiredService<OturumServisi>();
        sube.WaitForAssertion(() => Assert.Equal("s1", KabukServisi.Ortak.GorunumSubesiIcin(oturum)));

        istekler.Clear();
        var secici = SeciciCiz();
        secici.Find("#kt-cari").Input("ayş");
        secici.WaitForAssertion(() =>
            Assert.Contains(istekler, y => y.Contains("/arama") && y.Contains("sube=s1")));

        KabukServisi.Ortak.GorunumSubesiYerelAta(null);
    }

    // ── GlobalArama (S1-j: Ctrl+K KAYIT PALETİ) ──────────────────────────────
    //
    // ⚠ SÖZLEŞME DEĞİŞTİ (PAKET S1-j, kurucu 24.09 14:40): menü kutusu artık
    //   YALNIZ menüyü süzer (KenarCubugu, #menu-arama-girdi). Kayıt araması
    //   SİLİNMEDİ; üst bardaki YAZILI "Müşteri, ürün ara" düğmesi (#ust-kayit-ara)
    //   ve Ctrl/Cmd+K bu paleti açar. Kimlikler (#kabuk-arama-*) korunur; testler
    //   önce paleti açar, sonra eski davranışı aynen sınar (kapsam daralmadı).

    /// <summary>Paleti üst bar düğmesiyle açar (gerçek kullanıcı yolu).</summary>
    private IRenderedComponent<GlobalArama> Palet()
    {
        var e = Render<GlobalArama>();
        e.Find("#ust-kayit-ara").Click();
        return e;
    }

    // PAKET KATMAN-1b: palet açıcısına BAĞLANMAZ — perdeli KOMUT PALETİdir. Masaüstünde açıcı
    // (#ust-kayit-ara) gizli (0×0) olduğundan bağlı palet sol üst köşeye yapışıyordu (tur 3).
    // Perde + aria-modal: tur ölçümü paleti "modal" sayar; konum css'ten (satır içi style yok).
    [Fact]
    public void K1_Palet_BAGLI_DEGIL_perdeli_komut_paleti_aria_modal()
    {
        OturumAc();
        var e = Palet();
        var palet = e.Find("#kabuk-arama-palet");
        Assert.Null(palet.GetAttribute("data-katman-acici"));
        Assert.Null(palet.GetAttribute("data-katman"));
        Assert.Equal("dialog", palet.GetAttribute("role"));
        Assert.Equal("true", palet.GetAttribute("aria-modal"));
        Assert.NotNull(e.Find("#kabuk-arama-perde"));
        Assert.Null(palet.GetAttribute("style"));   // konum css/katman.css + app.css'ten, satır içi düzen yok
    }

    [Fact]
    public void Kayit_aramasi_YAZILI_dugmeyle_acilir_panel_yazilmadan_ACILMAZ()
    {
        OturumAc();
        var e = Render<GlobalArama>();

        // Düğme GÖRÜNÜR YAZILIDIR (ikonla yetinilmez — P-37 itirazı) ve kısayolu söyler.
        var dugme = e.Find("#ust-kayit-ara");
        Assert.Contains("Müşteri, ürün ara", dugme.TextContent, StringComparison.Ordinal);
        Assert.Contains("Ctrl+K", dugme.GetAttribute("title")!, StringComparison.Ordinal);
        Assert.Equal("false", dugme.GetAttribute("aria-expanded"));
        // Kapalıyken palet ve kutu yok.
        Assert.Empty(e.FindAll("#kabuk-arama-palet"));
        Assert.Empty(e.FindAll("#kabuk-arama-girdi"));

        dugme.Click();

        Assert.NotNull(e.Find("#kabuk-arama-palet"));
        Assert.NotNull(e.Find("#kabuk-arama-girdi"));
        Assert.Equal("true", e.Find("#ust-kayit-ara").GetAttribute("aria-expanded"));
        // Sonuç paneli yazılana kadar yoktur.
        Assert.Empty(e.FindAll("#kabuk-arama-paneli"));
        // Kaldırılan eski düğme kimliği geri sızmadı (P-37 kararının cırcırı).
        Assert.Empty(e.FindAll("#ust-arama-ac"));
    }

    [Fact]
    public async Task CtrlK_kisayolu_kapaliyken_paleti_ACAR()
    {
        OturumAc();
        var e = Render<GlobalArama>();
        Assert.Empty(e.FindAll("#kabuk-arama-palet"));

        // JS kısayolu kutu DOM'da yokken .NET'e haber verir (siralistesi.js).
        await e.InvokeAsync(() => e.Instance.KisayolAc());

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kabuk-arama-girdi")));
    }

    [Fact]
    public void Esc_paleti_KAPATIR()
    {
        OturumAc();
        var e = Palet();
        e.Find("#kabuk-arama-girdi").Input("ayş");
        e.Find("#kabuk-arama-girdi").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(e.FindAll("#kabuk-arama-palet"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("s")]
    [InlineData("ş")]
    [InlineData("İ")]
    [InlineData("ı")]
    [InlineData("*?%&<>\\\"'")]
    public void Palet_klavyesi_bos_ve_sinir_metinlerde_COKMEZ(string metin)
    {
        // K11: keydown bağlama güncellenmeden ÖNCE gelir; boş metinde Sadelestir
        // çöküyordu. Her tuş her metinde istisnasız çalışmalı.
        OturumAc();
        var e = Palet();
        var girdi = e.Find("#kabuk-arama-girdi");
        foreach (var tus in new[] { "ArrowDown", "ArrowUp", "Enter", "a" })
            girdi.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = tus });
        girdi.Input(metin);
        foreach (var tus in new[] { "ArrowDown", "ArrowUp", "a" })
            e.Find("#kabuk-arama-girdi").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = tus });

        Assert.NotNull(e.Find("#kabuk-arama-palet"));
    }

    [Fact]
    public void Yazilinca_panel_acilir_ve_kapatilabilir()
    {
        OturumAc();
        var e = Palet();
        e.Find("#kabuk-arama-girdi").Input("cari");

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kabuk-arama-paneli")));
        e.Find("#kabuk-arama-kapat").Click();
        Assert.Empty(e.FindAll("#kabuk-arama-paneli"));
    }

    [Fact]
    public void Palette_ekran_arama_menu_kaynagindan_gelir()
    {
        OturumAc();
        var e = Palet();
        e.Find("#kabuk-arama-girdi").Input("cari");

        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#kabuk-arama-ekranlar button")));
    }

    [Fact]
    public void Palette_kayit_sonucu_derin_baglantiya_gider()
    {
        OturumAc();
        var e = Palet();
        e.Find("#kabuk-arama-girdi").Input("ayş");

        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#kabuk-arama-kayit-0")));
        e.Find("#kabuk-arama-kayit-0").Click();

        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        Assert.Contains("/cariler?cariId=x0", nav.Uri, StringComparison.Ordinal);
    }

    /// <summary>
    /// DoD EK md.1: "tıklanınca hiçbir şey olmayan düğme" YASAK. Rotası
    /// olmayan sonuç tıklanabilir listeye HİÇ girmez.
    /// </summary>
    [Fact]
    public void Rotasiz_sonuc_tiklanabilir_listeye_GIRMEZ()
    {
        // Sunucu 3 sonuç döner: 2'si rotalı, 1'i (fiş) ROTASIZ.
        rotasizEkle = true;
        OturumAc();
        var e = Palet();
        e.Find("#kabuk-arama-girdi").Input("ayş");

        e.WaitForAssertion(() =>
        {
            var dugmeler = e.FindAll("#kabuk-arama-kayitlar button");
            // Rotasız olan ÇİZİLMEZ: yalnız 2 rotalı sonuç listelenir.
            Assert.Equal(sonucSayisi, dugmeler.Count);
            Assert.DoesNotContain("Ayşe fişi", e.Find("#kabuk-arama-kayitlar").TextContent,
                StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Palette_sunucu_hatasi_bos_duruma_CEVRILMEZ()
    {
        aramaKodu = HttpStatusCode.InternalServerError;
        OturumAc();
        var e = Palet();
        e.Find("#kabuk-arama-girdi").Input("ayş");

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kabuk-arama-hata")));
    }

    // ── KATMAN-1b (tasarım hakemi 29.09 KRİTİK): boş bölüm çizilmez, "yok" TEK ve simgesiz;
    // sınır satırı YALNIZ "daha var" iken ve "süzgeç" sözcüğü olmadan. ─────────────────────

    private static int BaslikSayisi(IRenderedComponent<GlobalArama> e, string metin) =>
        e.FindAll("#kabuk-arama-paneli h3").Count(h => h.TextContent.Trim() == metin);

    [Fact]
    public void KATMAN1b_1_kayit_0_ekran_BOS_DURUM_YOK_ekran_basligi_CIZILMEZ()
    {
        sonucSayisi = 1;
        OturumAc();
        var e = Palet();
        e.Find("#kabuk-arama-girdi").Input("zzqqxx");

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kabuk-arama-kayit-0")));
        Assert.Empty(e.FindAll("#kabuk-arama-paneli .bos-durum"));
        Assert.Empty(e.FindAll("#kabuk-arama-ekranlar"));
        Assert.Equal(0, BaslikSayisi(e, Sozlukler.Tr.Kabuk.AramaEkranlar));
        Assert.Equal(1, BaslikSayisi(e, Sozlukler.Tr.Kabuk.AramaKayitlar));
        // Gerçek sonuç panelin İLK içeriğidir (üstünde "yok" kutusu yok).
        Assert.DoesNotContain(Sozlukler.Tr.Kabuk.AramaSonucYok, e.Find("#kabuk-arama-paneli").TextContent);
    }

    [Fact]
    public void KATMAN1b_0_kayit_0_ekran_TEK_bos_durum_SIMGESIZ_ve_KISA()
    {
        sonucSayisi = 0;
        OturumAc();
        var e = Palet();
        e.Find("#kabuk-arama-girdi").Input("zzqqxx");

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kabuk-arama-yok")));
        var bos = Assert.Single(e.FindAll("#kabuk-arama-paneli .bos-durum"));
        Assert.Equal(MenuAramaSozlugu.PaletSonucYok("tr"), bos.TextContent.Trim());
        Assert.Empty(bos.QuerySelectorAll("svg, .ikon"));
        Assert.Empty(e.FindAll("#kabuk-arama-paneli h3"));
    }

    [Fact]
    public void KATMAN1b_ekran_ve_kayit_varken_iki_baslik_bos_durum_YOK()
    {
        sonucSayisi = 2;
        OturumAc();
        var e = Palet();
        e.Find("#kabuk-arama-girdi").Input("cari");

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kabuk-arama-kayit-0")));
        Assert.NotEmpty(e.FindAll("#kabuk-arama-ekranlar button"));
        Assert.Equal(1, BaslikSayisi(e, Sozlukler.Tr.Kabuk.AramaEkranlar));
        Assert.Equal(1, BaslikSayisi(e, Sozlukler.Tr.Kabuk.AramaKayitlar));
        Assert.Empty(e.FindAll("#kabuk-arama-paneli .bos-durum"));
    }

    [Fact]
    public void KATMAN1b_ekran_var_kayit_0_YOK_mesaji_ve_KAYITLAR_basligi_CIKMAZ()
    {
        sonucSayisi = 0;
        OturumAc();
        var e = Palet();
        e.Find("#kabuk-arama-girdi").Input("cari");

        e.WaitForAssertion(() =>
        {
            Assert.Contains(istekler, y => y.Contains("/arama", StringComparison.Ordinal));
            Assert.NotEmpty(e.FindAll("#kabuk-arama-ekranlar button"));
            Assert.Empty(e.FindAll("#kabuk-arama-paneli .yukleniyor-kutu"));
        });
        Assert.Empty(e.FindAll("#kabuk-arama-paneli .bos-durum"));
        Assert.Empty(e.FindAll("#kabuk-arama-yok"));
        Assert.Equal(0, BaslikSayisi(e, Sozlukler.Tr.Kabuk.AramaKayitlar));
        Assert.Equal(1, BaslikSayisi(e, Sozlukler.Tr.Kabuk.AramaEkranlar));
    }

    [Fact]
    public void KATMAN1b_sinir_satiri_daha_var_YOKKEN_cikmaz()
    {
        sonucSayisi = 2;
        dahaVar = false;
        OturumAc();
        var e = Palet();
        e.Find("#kabuk-arama-girdi").Input("ayş");

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kabuk-arama-kayit-0")));
        Assert.Empty(e.FindAll("#kabuk-arama-daha-var"));
        Assert.Equal("", e.Find("#kabuk-arama-durum").TextContent.Trim());
        Assert.DoesNotContain("süzgeç", e.Find("#kabuk-arama-palet").TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KATMAN1b_sinir_satiri_daha_var_iken_TEK_kez_sade_metinle()
    {
        sonucSayisi = 2;
        dahaVar = true;
        OturumAc();
        var e = Palet();
        e.Find("#kabuk-arama-girdi").Input("ayş");

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kabuk-arama-kayit-0")));
        var satir = Assert.Single(e.FindAll("#kabuk-arama-daha-var"));
        Assert.Equal(MenuAramaSozlugu.PaletDahaVar("tr"), satir.TextContent.Trim());
        Assert.NotNull(e.Find("#kabuk-arama-durum #kabuk-arama-daha-var"));
        Assert.DoesNotContain("süzgeç", e.Find("#kabuk-arama-palet").TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Sozlukler.Tr.Kabuk.SinirAsildi, e.Find("#kabuk-arama-palet").TextContent);
    }
}
