using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-19 — /gider EKRANI [bUnit]
//
// DoD EK 1/1: "tıklanınca hiçbir şey olmayan düğme" YOKTUR — her <button> ya
// gerçek istek atar ya disabled'dır ve SEBEBİ yanında yazar (UX-S30).
// DoD EK 1/3: boş/geçersiz gönderim alan altı Türkçe mesaj verir, sessiz
// etkisizlik YASAK.
// UX-S28: sunucu hatası "kayıt yok"a ÇEVRİLMEZ — hata bandı çıkar.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P19EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string? hataliYolParcasi;
    private string kasaYaniti = """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":10000,"aktif":true}]""";
    private string kategoriYaniti = """
        {"kategoriler":[{"id":"gk1","ad":"Kira","aciklama":"işletme kirası","aktif":true,"giderAdedi":2,"giderToplamTl":6000}],"toplam":1}
        """;

    public P19EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(
            new SahteApi(istekler, () => hataliYolParcasi, () => kasaYaniti, () => kategoriYaniti))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(
        List<(string, string, string)> kayit,
        Func<string?> hataliParca,
        Func<string> kasalar,
        Func<string> kategoriler) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var tam = istek.RequestUri!.PathAndQuery;
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, tam, govde));

            if (hataliParca() is { } p && yol.Contains(p))
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"message":"Sunucu hatası: gider listesi okunamadı"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            var metot = istek.Method.Method;
            // ⚠ Sahte API'nin "{}" varsayılanı YETMEZ: yeni uçlara GERÇEKÇİ yanıt verilir,
            //   yoksa ekran boş modelle yeşil kalır ve sözleşme kayması görünmez.
            var g = yol switch
            {
                var y when y.EndsWith("/gider-kategorileri") && metot == "POST" =>
                    """{"id":"gk2","ad":"Elektrik","aciklama":null,"aktif":true,"giderAdedi":0,"giderToplamTl":0}""",
                var y when y.EndsWith("/gider-kategorileri") => kategoriler(),
                var y when y.Contains("/gider-kategorileri/") =>
                    """{"id":"gk1","ad":"Kira","aciklama":"dükkân kirası","aktif":true,"giderAdedi":2,"giderToplamTl":6000}""",
                var y when y.EndsWith("/giderler") && metot == "POST" =>
                    """{"id":"kh9","kasaId":"ks1","kasaAdi":"Merkez Kasa","kategoriId":"gk1","kategoriAdi":"Kira","tutar":1500,"paraBirimi":"TRY","aciklama":null,"tarih":"2026-09-21T06:00:00Z","kasaBakiyesi":8500}""",
                var y when y.EndsWith("/giderler") => """
                    {"satirlar":[{"id":"kh1","kasaId":"ks1","kasaAdi":"Merkez Kasa","kategoriId":"gk1","kategoriAdi":"Kira","tutar":3000,"paraBirimi":"TRY","aciklama":"Ekim","tarih":"2026-09-20T06:00:00Z","stornoSatiri":false,"stornolandi":false}],
                     "toplamAdet":1,"limit":100,"dahaFazlasiVar":false,"sinirNotu":"En çok 100 gider satırı gösterilir."}
                    """,
                var y when y.EndsWith("/virman") && metot == "POST" =>
                    """{"cikisBacakId":"a1","girisBacakId":"b1","kaynakTur":"KASA","kaynakId":"ks1","kaynakAdi":"Merkez Kasa","hedefTur":"BANKA","hedefId":"bh1","hedefAdi":"Ziraat","tutar":2000,"paraBirimi":"TRY","aciklama":null,"tarih":"2026-09-21T06:00:00Z","subelerArasi":false}""",
                var y when y.EndsWith("/virman") => """
                    {"satirlar":[{"cikisBacakId":"a1","girisBacakId":"b1","kaynakTur":"KASA","kaynakAdi":"Merkez Kasa","hedefTur":"BANKA","hedefAdi":"Ziraat","tutar":2000,"paraBirimi":"TRY","aciklama":null,"tarih":"2026-09-20T06:00:00Z","stornoSatiri":false}],
                     "toplam":1,"limit":50,"dahaFazlasiVar":false,"sinirNotu":"En son 50 virman gösterilir."}
                    """,
                var y when y.Contains("/virman/") =>
                    """{"kaynakBacakIdleri":["a1","b1"],"stornoBacakIdleri":["t1","t2"],"bacakSayisi":2,"gerekce":"test"}""",
                var y when y.EndsWith("/kasalar") => kasalar(),
                var y when y.EndsWith("/bankalar") =>
                    """{"hesaplar":[{"id":"bh1","bankaAdi":"Ziraat","hesapAdi":"Vadesiz","hesapSahibi":null,"iban":"TR11","paraBirimi":"TRY","bakiye":5000,"aktif":true}]}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private void OturumAc(string rol = "SAHIP") =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1"));

    private IRenderedComponent<Kuyumcu.Web.Pages.Gider> Ekran()
    {
        OturumAc();
        var e = Render<Kuyumcu.Web.Pages.Gider>().AyrintiAc("#yildiz-gider-ekle");
        GiderAkordeon.Ac(e); // SON-KALANLAR: geçmiş kayıtlar kapalı akordeon
        return e;
    }

    // ── DoD EK 1/1 — ölü düğme yok ──────────────────────────────────────────

    [Fact]
    public void Her_dugme_ya_istek_atar_ya_DISABLED_ve_sebebi_yazar()
    {
        var e = Ekran();
        foreach (var sekme in new[] { "gd-sekme-giderler", "gd-sekme-kategoriler", "gd-sekme-virman" })
        {
            e.Find("#" + sekme).Click();
            foreach (var d in e.FindAll("button"))
            {
                var kilitli = d.HasAttribute("disabled");
                var tiklanabilir = d.Attributes.Any(a => a.Name.Contains("onclick", StringComparison.OrdinalIgnoreCase));
                Assert.True(kilitli || tiklanabilir,
                    $"Ölü düğme: #{d.Id} — ne @onclick ne disabled taşıyor.");
            }
        }
    }

    [Fact]
    public void Gider_kaydet_dugmesi_tutar_yokken_KILITLI_ve_sebebi_yazili()
    {
        var e = Ekran();
        // TOPLU-1 (UX-S22): TEK aktif kategori kendiliğinden seçili (seçici yok) → ilk kilit sebebi
        // doğrudan TUTAR'dır; "kategori seçin" sebebi yalnız birden çok kategoride çıkar.
        Assert.Equal("gk1", e.Find("#gd-kategori-sabit").GetAttribute("data-deger"));
        Assert.DoesNotContain("kategorisi", e.Find("#gd-kaydet-kilit").TextContent);
        Assert.True(e.Find("#gd-kaydet").HasAttribute("disabled"));
        Assert.Contains("Tutar", e.Find("#gd-kaydet-kilit").TextContent);
    }

    /// <summary>UX-S22: tek kasa varsa seçici GİZLENİR, değer salt okunur satırdır.</summary>
    [Fact]
    public void Tek_kasa_varsa_kasa_secici_GIZLENIR()
    {
        var e = Ekran();
        Assert.Empty(e.FindAll("#gd-kasa"));
        Assert.Contains("Merkez Kasa", e.Find("#gd-kasa-sabit").TextContent);
    }

    [Fact]
    public void Iki_kasa_varsa_kasa_secici_GORUNUR()
    {
        kasaYaniti = """
            [{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":10000,"aktif":true},
             {"id":"ks2","ad":"Şube Kasa","paraBirimi":"TRY","bakiye":500,"aktif":true}]
            """;
        var e = Ekran();
        Assert.Single(e.FindAll("#gd-kasa"));
        Assert.Empty(e.FindAll("#gd-kasa-sabit"));
    }

    /// <summary>UX-S27: POST yanıtı OKUNUR ve banda yazılır (yeni bakiye dâhil).</summary>
    [Fact]
    public void Gider_kaydedilince_sunucu_sonucu_BANDA_yazilir()
    {
        var e = Ekran();
        Assert.Equal("gk1", e.Find("#gd-kategori-sabit").GetAttribute("data-deger"));   // TOPLU-1 UX-S22: tek kategori seçicisiz, kendiliğinden
        e.Find("#gd-tutar").Input("1500");
        e.Find("#gd-kaydet").Click();

        var post = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/giderler"));
        Assert.Contains("\"kasaId\":\"ks1\"", post.Govde);
        Assert.Contains("\"giderKategoriId\":\"gk1\"", post.Govde);

        var bant = e.Find("#gd-basari").TextContent;
        Assert.Contains("Kira", bant);
        Assert.Contains("8.500", bant.Replace(',', '.'));   // sunucunun döndürdüğü YENİ bakiye
    }

    /// <summary>Virman: gövde TÜR + KİMLİK olarak AYRIŞTIRILARAK gönderilir.</summary>
    [Fact]
    public void Virman_govdesi_tur_ve_kimligi_AYRI_gonderir()
    {
        var e = Ekran();
        e.Find("#gd-sekme-virman").Click();
        GiderAkordeon.Ac(e); // SON-KALANLAR: geçmiş kayıtlar kapalı akordeon
        SeciciYardim.Sec(e, "gd-v-kaynak", "KASA:ks1");
        SeciciYardim.Sec(e, "gd-v-hedef", "BANKA:bh1");
        e.Find("#gd-v-tutar").Input("2000");
        e.Find("#gd-v-kaydet").Click();

        var post = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/virman"));
        Assert.Contains("\"kaynakTur\":\"KASA\"", post.Govde);
        Assert.Contains("\"kaynakId\":\"ks1\"", post.Govde);
        Assert.Contains("\"hedefTur\":\"BANKA\"", post.Govde);
        Assert.Contains("\"hedefId\":\"bh1\"", post.Govde);
    }

    [Fact]
    public void Virman_kaynak_ve_hedef_ayniyken_dugme_KILITLI()
    {
        var e = Ekran();
        e.Find("#gd-sekme-virman").Click();
        GiderAkordeon.Ac(e); // SON-KALANLAR: geçmiş kayıtlar kapalı akordeon
        SeciciYardim.Sec(e, "gd-v-kaynak", "KASA:ks1");
        SeciciYardim.Sec(e, "gd-v-hedef", "KASA:ks1");
        e.Find("#gd-v-tutar").Input("100");
        Assert.True(e.Find("#gd-v-kaydet").HasAttribute("disabled"));
        Assert.Contains("aynı", e.Find("#gd-v-kaydet-kilit").TextContent);
    }

    /// <summary>Yıkıcı eylem: onay + gerekçe; gerekçe kısa iken onay düğmesi KİLİTLİ.</summary>
    [Fact]
    public void Storno_onayi_gerekce_istemeden_ACILMAZ()
    {
        var e = Ekran();
        e.Find("#gd-storno-kh1").Click();
        Assert.True(e.Find("#gd-storno-onayla").HasAttribute("disabled"));
        Assert.Contains("20", e.Find("#gd-storno-kilit").TextContent);

        e.Find("#gd-storno-gerekce").Input("Yanlış kategoriye yazılmış gider düzeltiliyor.");
        Assert.False(e.Find("#gd-storno-onayla").HasAttribute("disabled"));
        e.Find("#gd-storno-onayla").Click();

        var post = istekler.Single(i => i.Metot == "POST" && i.Yol.Contains("/hareket/kh1/storno"));
        Assert.Contains("gerekce", post.Govde);
    }

    /// <summary>Virman stornosu VİRMAN ucuna gider (kasa ucuna DEĞİL).</summary>
    [Fact]
    public void Virman_stornosu_virman_ucuna_gider_ve_IKI_BACAK_bildirilir()
    {
        var e = Ekran();
        e.Find("#gd-sekme-virman").Click();
        GiderAkordeon.Ac(e); // SON-KALANLAR: geçmiş kayıtlar kapalı akordeon
        e.Find("#gd-v-storno-a1").Click();
        e.Find("#gd-storno-gerekce").Input("Yanlış hesaba yapılan aktarım geri alınıyor.");
        e.Find("#gd-storno-onayla").Click();

        Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/virman/a1/storno"));
        Assert.Contains("2 bacak", e.Find("#gd-basari").TextContent);
    }

    /// <summary>UX-S28: sunucu hatası "kayıt yok"a çevrilmez.</summary>
    [Fact]
    public void Sunucu_hatasi_BOS_DURUM_gibi_gosterilmez()
    {
        hataliYolParcasi = "/giderler";
        var e = Ekran();
        Assert.NotEmpty(e.FindAll("#gd-hata"));
        Assert.Empty(e.FindAll(".bos-durum"));
    }

    /// <summary>Kategori PATCH'i gövdesinde yalnız DÜZENLENEN alanları taşır.</summary>
    [Fact]
    public void Kategori_duzenlemesi_CEKMECE_ile_yapilir_ve_PATCH_atar()
    {
        var e = Ekran();
        e.Find("#gd-sekme-kategoriler").Click();
        e.Find("#gd-kat-duzenle-gk1").Click();
        Assert.NotEmpty(e.FindAll("#cekmece-gider-kategori"));

        e.Find("#gd-kd-ad").Input("İşletme kirası");
        e.Find("#cekmece-gider-kategori-kaydet").Click();

        var patch = istekler.Single(i => i.Metot == "PATCH" && i.Yol.Contains("/gider-kategorileri/gk1"));
        // ⚠ Gövde JSON'dur ve Türkçe harfler \uXXXX kaçışlıdır: metin araması
        //   yanlış negatif verirdi (Anayasa md.1 — sahte yeşil yerine AYRIŞTIR).
        using var govde = System.Text.Json.JsonDocument.Parse(patch.Govde);
        Assert.Equal("İşletme kirası", govde.RootElement.GetProperty("ad").GetString());
        // Gönderilmeyen alan KORUNUR: "aktif" anahtarı gövdede YOKTUR.
        Assert.False(govde.RootElement.TryGetProperty("aktif", out _));
    }

    /// <summary>DELETE = pasife alma; ekranda "Sil" DEĞİL "Pasife al" yazar.</summary>
    [Fact]
    public void Kategori_pasife_alma_DELETE_atar_ve_silme_dili_kullanilmaz()
    {
        var e = Ekran();
        e.Find("#gd-sekme-kategoriler").Click();
        var dugme = e.Find("#gd-kat-durum-gk1");
        Assert.Equal("Pasife al", dugme.TextContent.Trim());
        dugme.Click();
        Assert.Contains(istekler, i => i.Metot == "DELETE" && i.Yol.Contains("/gider-kategorileri/gk1"));
        Assert.Contains("SİLİNMEZ", e.Find("#gd-kat-silinmez").TextContent);
    }

    // ═══ ORTAK-1 · KEŞİF #4 (30.09, KRİTİK) — gider tutarı / virman tutarı tuş tuş "1.500" ═════════
    private static readonly string[] OK1TusTus1500 = ["1", "1.", "1.5", "1.50", "1.500"];

    [Fact]
    public void OK1_K4_gider_tutari_belirsiz_1_500_POST_YOK_sebep_yazar()
    {
        var e = Ekran();
        Assert.Equal("gk1", e.Find("#gd-kategori-sabit").GetAttribute("data-deger"));   // TOPLU-1 UX-S22: tek kategori seçicisiz, kendiliğinden
        foreach (var a in OK1TusTus1500) e.Find("#gd-tutar").Input(a);
        e.Find("#gd-tutar").Blur();
        Assert.Equal("1.500", e.Find("#gd-tutar").GetAttribute("value"));
        Assert.True(e.Find("#gd-kaydet").HasAttribute("disabled"));
        // Sebep "tutar girin" gibi genel kural değil, KAPININ cümlesi: kutunun etiketiyle "sayıyı düzeltin".
        Assert.Equal(string.Format(Kuyumcu.Core.I18n.Satis3Sozlugu.Al("tr").AlanDuzeltin,
                                   e.Find("label[for='gd-tutar']").TextContent.Replace("*", "").Trim()),
                     e.Find("#gd-kaydet-kilit").TextContent.Trim());
        istekler.Clear();
        e.Find("#gd-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    [Fact]
    public void OK1_K4_virman_tutari_belirsiz_1_500_POST_YOK_sebep_yazar()
    {
        var e = Ekran();
        e.Find("#gd-sekme-virman").Click();
        GiderAkordeon.Ac(e); // SON-KALANLAR: geçmiş kayıtlar kapalı akordeon
        SeciciYardim.Sec(e, "gd-v-kaynak", "KASA:ks1");
        SeciciYardim.Sec(e, "gd-v-hedef", "BANKA:bh1");
        foreach (var a in OK1TusTus1500) e.Find("#gd-v-tutar").Input(a);
        e.Find("#gd-v-tutar").Blur();
        Assert.Equal("1.500", e.Find("#gd-v-tutar").GetAttribute("value"));
        Assert.True(e.Find("#gd-v-kaydet").HasAttribute("disabled"));
        Assert.NotEmpty(e.Find("#gd-v-kaydet-kilit").TextContent);
        istekler.Clear();
        e.Find("#gd-v-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }
}
