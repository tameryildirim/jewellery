using System.Text.Json;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S-3a — EKRAN TARAFI (bUnit).
//
//   A) SiparisDetay düzenleme formu kalemin TAM alan kümesini taşır ve geri
//      gönderir (UX-S26). Eskiden yalnız tanim+adet gidiyor, sunucu kalan
//      alanları yeniden kuramadığı için ürün/ayar/gram siliniyordu.
//   B) KabukHatasi sayfa kökü hata sınırı: sayfa ağacı çökse bile kullanıcı
//      BOŞ EKRAN değil, GÖRÜNÜR hata bandı ve yeniden deneme yolu görür
//      (UX-S28 · Anayasa md.1).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S3aSiparisDuzenlemeEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    // Ürün bağlı + ayarlı + gramlı TASLAK kalem — QA turu 3'teki S-2026-000001.
    private const string UrunluTaslak = """
        {"id":"s1","no":"S-2026-000001","tur":"MUSTERI_SIPARISI","durum":"TASLAK","cariId":"c1",
         "cariAd":"Ayşe Yılmaz","subeId":"sb1","sozVerilenGun":"2026-09-10","aciklama":"bilezik",
         "tahminiTutarTl":null,"kurSnapshotId":null,"emanetHareketId":null,
         "geciken":false,"duzenlenebilir":true,"terminal":false,
         "izinliGecisler":["ONAYLANDI","IPTAL"],"gerekceIsteyenGecisler":["IPTAL"],"gerekceAsgari":10,
         "olusturanKullaniciId":"u1","olusturuldu":"2026-09-16T08:00:00Z","guncellendi":"2026-09-16T08:00:00Z",
         "kalemler":[{"id":"k1","urunId":"u-1","urunAd":"QA-TEST 22 Ayar Bilezik","tanim":null,
                      "ayarId":"K22","gramAgirlik":12.5,"adet":1.0,"sira":0}],
         "gecisler":[{"id":"g1","oncekiDurum":null,"yeniDurum":"TASLAK","gerekce":null,
                      "kullaniciId":"u1","kullaniciAd":"Test","olusturuldu":"2026-09-16T08:00:00Z"}]}
        """;

    public S3aSiparisDuzenlemeEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(S3aSiparisDuzenlemeEkranTestleri sahip) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            sahip.istekler.Add((istek.Method.Method, yol, govde));

            var g = yol switch
            {
                var y when y.Contains("/siparisler/") => UrunluTaslak,
                var y when y.Contains("/auth/ben") =>
                    """{"id":"u1","ad":"Test","soyad":"Kullanici","email":"t@t.local","rol":"SAHIP","subeId":"sb1"}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.SiparisDetay> Detay()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));
        return Render<Kuyumcu.Web.Pages.SiparisDetay>(p => p.Add(x => x.Id, "s1"));
    }

    [Fact]
    public void Duzenleme_formu_urun_ayar_ve_grami_GOSTERIR()
    {
        var e = Detay();
        e.Find("#sipd-duzenle").Click();

        // PAKET P-52: ürün ve ayar artık SEÇİCİ (Secici) taşır — eleman kimlikleri
        // aynı kaldı, okunacak öznitelik `value` yerine `data-deger` oldu. Ürün adı
        // hâlâ GÖRÜNÜR (UX-S21: sistemin bildiği alan gizlenmez), artık DEĞİŞTİRİLEBİLİR de.
        Assert.Contains("QA-TEST 22 Ayar Bilezik", e.Find("#sipd-d-kalem-urun-0").TextContent);
        Assert.Equal("u-1", e.Find("#sipd-d-kalem-urun-0").GetAttribute("data-deger"));
        Assert.Equal("K22", e.Find("#sipd-d-kalem-ayar-0").GetAttribute("data-deger"));
        // P-78: girdi kutusu değeri tr-TR ("12,5"); tr-TR çözücü noktalı 3 haneli
        // makine yazımını ("1.234") belirsiz sayacağı için kutu artık virgül basar.
        // BICIM-2: odak DIŞINDA gram kutusu Bicim.Gram kuralıyla (3 hane) basar.
        Assert.Equal("12,500", e.Find("#sipd-d-kalem-gram-0").GetAttribute("value"));
        // Ürün adı SERBEST TANIMA çevrilmez — tanım gerçekten boştur.
        Assert.True(string.IsNullOrEmpty(e.Find("#sipd-d-kalem-tanim-0").GetAttribute("value")));
    }

    [Fact]
    public void Kaydet_kalemi_kimligi_urunu_ayari_ve_gramiyla_GERI_GONDERIR()
    {
        var e = Detay();
        e.Find("#sipd-duzenle").Click();
        e.Find("#sipd-d-aciklama").Input("yalnız açıklama değişti");
        e.Find("#sipd-d-kaydet").Click();

        var patch = istekler.Last(i => i.Metot == "PATCH");
        using var j = JsonDocument.Parse(patch.Govde);
        var kalem = j.RootElement.GetProperty("kalemler").EnumerateArray().Single();

        Assert.Equal("k1", kalem.GetProperty("id").GetString());
        Assert.Equal("u-1", kalem.GetProperty("urunId").GetString());
        Assert.Equal("K22", kalem.GetProperty("ayarId").GetString());
        Assert.Equal(12.5m, kalem.GetProperty("gramAgirlik").GetDecimal());
        Assert.Equal(1m, kalem.GetProperty("adet").GetDecimal());
    }

    // ── PAKET P-78 — SiparisDetay gram/adet tr-TR (eskiden Invariant: "1,5" = 15) ──
    [Fact]
    public void P78_siparis_detay_1_5_gram_ve_2_5_adet_TURKCE_okunur()
    {
        var e = Detay();
        e.Find("#sipd-duzenle").Click();
        e.Find("#sipd-d-kalem-gram-0").Input("1,5");
        e.Find("#sipd-d-kalem-adet-0").Input("2");   // PAKET GOC-C (K14): adet tam sayı — 2,5 artık alan altı hata
        e.Find("#sipd-d-kaydet").Click();

        var patch = istekler.Last(i => i.Metot == "PATCH");
        using var j = JsonDocument.Parse(patch.Govde);
        var kalem = j.RootElement.GetProperty("kalemler").EnumerateArray().Single();
        Assert.Equal(1.5m, kalem.GetProperty("gramAgirlik").GetDecimal());
        Assert.Equal(2m, kalem.GetProperty("adet").GetDecimal());
    }

    [Fact]
    public void P78_siparis_detay_belirsiz_gram_ALAN_ALTINDA_soylenir()
    {
        var e = Detay();
        e.Find("#sipd-duzenle").Click();
        e.Find("#sipd-d-kalem-gram-0").Input("1.500");
        Assert.Equal(Kuyumcu.Core.I18n.SayiGirisiSozlugu.Tr.Belirsiz,
            e.Find("#sipd-d-kalem-gram-0-hata").TextContent.Trim());
    }

    [Fact]
    public void Urun_bagli_kalem_tanim_bos_olsa_bile_KAYDEDILIR()
    {
        // Eski kural "tanım boşsa kalem geçersiz" idi: ürün bağlı kalemde tanım
        // boş olduğu için kaydet SESSİZCE etkisiz kalıyordu (UX-S27 ihlali).
        var e = Detay();
        e.Find("#sipd-duzenle").Click();
        e.Find("#sipd-d-kaydet").Click();

        Assert.Contains(istekler, i => i.Metot == "PATCH");
        Assert.Empty(e.FindAll("#sipd-d-kalem-hata"));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// KABUK HATA SINIRI — sessiz boş ekran yerine görünür bant.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S3aKabukHatasiTestleri : BunitContext
{
    public S3aKabukHatasiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    /// <summary>Render sırasında patlayan sahte sayfa (deserialize hatası taklidi).</summary>
    private sealed class PatlayanSayfa : ComponentBase
    {
        [Parameter] public Exception Hata { get; set; } = new JsonException("kalemler alanı beklenen tipte değil");

        protected override void OnParametersSet() => throw Hata;
    }

    [Fact]
    public void Sayfa_cokunce_BOS_EKRAN_degil_GORUNUR_bant_cizilir()
    {
        var e = Render<Kuyumcu.Web.Bilesenler.KabukHatasi>(p =>
            p.Add(x => x.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<PatlayanSayfa>(0);
                b.CloseComponent();
            })));

        Assert.Single(e.FindAll("#kabuk-hata-bandi"));
        Assert.Contains("beklenmeyen bir hata", e.Find("#kabuk-hata-bandi").TextContent);
        // P-31 (R-5 · IC_KOD_SIZINTISI): ekran hatasının HAM mesajı artık
        // kullanıcıya basılmaz (iç kod/tip adı sızıntısı); yerine Türkçe genel
        // açıklama çıkar ve destek kodu sunucudan alınır (P31DestekEkranTestleri).
        Assert.DoesNotContain("kalemler alanı beklenen tipte değil",
            e.Find("#kabuk-hata-ayrinti").TextContent);
        Assert.Contains("beklenmeyen bir sorun", e.Find("#kabuk-hata-ayrinti").TextContent);
        Assert.Single(e.FindAll("#kabuk-hata-yeniden"));
    }

    [Fact]
    public void Sunucu_korelasyon_kimligi_varsa_destek_kodu_basilir_yoksa_UYDURULMAZ()
    {
        var kimlikli = Render<Kuyumcu.Web.Bilesenler.KabukHatasi>(p =>
            p.Add(x => x.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<PatlayanSayfa>(0);
                b.AddAttribute(1, nameof(PatlayanSayfa.Hata),
                    new ApiHata(500, "sunucu hatası") { IstekKimligi = "REQ-42" });
                b.CloseComponent();
            })));
        // P-36c/3: destek kodu artık yazılamaz bir kutu değil, SALT OKUNUR
        // metindir; MAKİNE değeri Secici ile aynı sözleşmeden okunur (data-deger).
        Assert.Equal("REQ-42", kimlikli.Find("#hata-kimlik-deger").GetAttribute("data-deger"));

        var kimliksiz = Render<Kuyumcu.Web.Bilesenler.KabukHatasi>(p =>
            p.Add(x => x.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<PatlayanSayfa>(0);
                b.CloseComponent();
            })));
        Assert.Empty(kimliksiz.FindAll("#hata-kimlik-rozet"));
    }

    /// <summary>Kurtarmadan sonra sağlam içerik çizilir; bant kaybolur.</summary>
    private sealed class Kapak { public bool Patla = true; }

    private sealed class KosulluSayfa : ComponentBase
    {
        [Parameter] public Kapak Durum { get; set; } = new();

        protected override void OnParametersSet()
        {
            if (Durum.Patla) throw new JsonException("ilk render patladı");
        }

        protected override void BuildRenderTree(RenderTreeBuilder b) =>
            b.AddMarkupContent(0, "<p id=\"saglam\">tamam</p>");
    }

    [Fact]
    public void Yeniden_dene_siniri_kurtarir_ve_saglam_icerik_cizilir()
    {
        var durum = new Kapak();
        var e = Render<Kuyumcu.Web.Bilesenler.KabukHatasi>(p =>
            p.Add(x => x.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<KosulluSayfa>(0);
                b.AddAttribute(1, nameof(KosulluSayfa.Durum), durum);
                b.CloseComponent();
            })));
        Assert.Single(e.FindAll("#kabuk-hata-bandi"));

        durum.Patla = false;
        e.Find("#kabuk-hata-yeniden").Click();

        Assert.Empty(e.FindAll("#kabuk-hata-bandi"));
        Assert.Single(e.FindAll("#saglam"));
    }
}
