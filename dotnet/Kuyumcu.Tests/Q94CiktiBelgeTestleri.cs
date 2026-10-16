using System.Globalization;
using System.Net;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Cikti;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Yazdirma;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MCikti = Kuyumcu.Api.Modules.Cikti;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q9-4 — ÇIKTI BELGESİ (QA turu 9, /cikti/{Tur}/{Id})
//
// Kusurlar (24.09, gerçek tarayıcı): kalem tutarı ham "45000.00"; müşteri
// fişinde has gram "-13.923" (kalem, nokta, 3 hane) ile "-13,9232 has" (toplam,
// virgül, 4 hane); sayfada iki <h1> ve belge adı bir kez de <h2> olarak.
//
// Sınananlar:
//   SAF  — sütun biçimi (TRY/USD/birimsiz/has/metin/eski sözleşme/İngilizce
//          kültür), toplam satırı yönü (tek yön · karışık yön), belge rotası
//   HTTP — fiş/alış kalemleri İŞARETSİZ + sütun biçimli, karışık fişte net yön
//          etikette, ekstrede işaret KORUNUR, belge no kayıt kimliği bayrağı
//   bUnit — ₺45.000,00, işaretsiz ve toplamla aynı has, TEK h1, belge no açıklaması
//          yalnız ekranda, üst barda belge rotasında h1 YOK / diğer rotada VAR
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Q94SafTestleri
{
    private static CiktiBolum Bolum(string[]? bicim) =>
        new("Kalemler", ["Tür", "Açıklama", "Has gram", "Tutar"], [], null, bicim);

    private static readonly string[] FisBicimi =
        [SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Has, SutunBicimi.Para];

    [Fact]
    public void Q94_para_sutunu_TL_bicimiyle_basilir_ham_sayi_kalmaz()
    {
        var s = new CiktiSatir(["Satış", "Bilezik", "13.9232", "45000.00"], ParaBirimi: "TRY");
        Assert.Equal(Bicim.BelgePara(45000m, "TRY"), A4Belge.Hucre(Bolum(FisBicimi), s, 3));
    }

    [Fact]
    public void Q94_has_sutunu_virgul_ve_4_hane_toplamla_AYNI_bicim()
    {
        var s = new CiktiSatir(["Satış", "Bilezik", "13.9232", "45000.00"], ParaBirimi: "TRY");
        var kalem = A4Belge.Hucre(Bolum(FisBicimi), s, 2);
        Assert.Equal("13,9232 has", kalem);
        // Tutarlar bölümündeki toplamın biçimleyicisiyle birebir aynı dize.
        Assert.Equal(A4Belge.Tutar(new CiktiTutar("Toplam has gram", 13.9232m, "HAS_GRAM")), kalem);
    }

    [Fact]
    public void Q94_para_birimi_satirla_tasinir_USD_TL_sayilmaz()
    {
        var s = new CiktiSatir(["Satış", "Bilezik", "1.0000", "1250.50"], ParaBirimi: "USD");
        Assert.Equal("1.250,50 $", A4Belge.Hucre(Bolum(FisBicimi), s, 3));
    }

    [Fact]
    public void Q94_birimsiz_tutar_TL_simgesi_ALMAZ()
    {
        var s = new CiktiSatir(["Satış", "Bilezik", "1.0000", "45000.00"]);
        var h = A4Belge.Hucre(Bolum(FisBicimi), s, 3);
        Assert.Equal("45.000,00", h);
        Assert.DoesNotContain("₺", h, StringComparison.Ordinal);
    }

    [Fact]
    public void Q94_metin_sutunu_ve_sayi_olmayan_hucre_oldugu_gibi_kalir()
    {
        var s = new CiktiSatir(["Satış", "22 Ayar 45000.00", "—", "45000.00"], ParaBirimi: "TRY");
        Assert.Equal("22 Ayar 45000.00", A4Belge.Hucre(Bolum(FisBicimi), s, 1));
        Assert.Equal("—", A4Belge.Hucre(Bolum(FisBicimi), s, 2));
    }

    [Fact]
    public void Q94_eski_sozlesme_bicimsiz_bolum_geriye_uyumlu()
    {
        var s = new CiktiSatir(["Satış", "Bilezik", "5.000", "1000.00"]);
        Assert.Equal("1000.00", A4Belge.Hucre(Bolum(null), s, 3));
        Assert.False(A4Belge.SayiSutunu(Bolum(null), 3));
        Assert.True(A4Belge.SayiSutunu(Bolum(FisBicimi), 3));
        Assert.False(A4Belge.SayiSutunu(Bolum(FisBicimi), 0));
    }

    [Fact]
    public void Q94_ingilizce_kulturde_de_TRY_kalibi_ve_nokta_ondalik_CIKMAZ()
    {
        var eski = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var s = new CiktiSatir(["Satış", "Bilezik", "13.9232", "45000.00"], ParaBirimi: "TRY");
            Assert.Equal(Bicim.BelgePara(45000m, "TRY"), A4Belge.Hucre(Bolum(FisBicimi), s, 3));
            Assert.Equal("13,9232 has", A4Belge.Hucre(Bolum(FisBicimi), s, 2));
        }
        finally { CultureInfo.CurrentCulture = eski; }
    }

    [Fact]
    public void Q94_toplam_tek_yonde_TOPLAM_etiketi_ve_isaretsiz()
    {
        var t = MCikti.CiktiServisi.ToplamSatiri("Toplam has gram", "Net has gram", [-13.9232m],
            "HAS_GRAM", "müşteriden alınan", "müşteriye verilen", toplam: false);
        Assert.Equal("Toplam has gram", t.Etiket);
        Assert.Equal(13.9232m, t.Deger);
    }

    [Fact]
    public void Q94_toplam_karisik_yonde_NET_ve_yon_etikette()
    {
        var tl = MCikti.CiktiServisi.ToplamSatiri("Toplam tutar", "Net tutar", [45_000m, -80_000m],
            "TRY", "müşteriden alınan", "müşteriye ödenen", toplam: true);
        Assert.Equal("Net tutar — müşteriye ödenen", tl.Etiket);
        Assert.Equal(35_000m, tl.Deger);
        Assert.True(tl.Toplam);

        var has = MCikti.CiktiServisi.ToplamSatiri("Toplam has gram", "Net has gram", [-13.9232m, 27.8464m],
            "HAS_GRAM", "müşteriden alınan", "müşteriye verilen", toplam: false);
        Assert.Equal("Net has gram — müşteriden alınan", has.Etiket);
        Assert.Equal(13.9232m, has.Deger);
    }

    [Theory]
    [InlineData("/cikti/SATIS_FISI/01ABC", true)]
    [InlineData("/cikti/CARI_EKSTRE/01ABC/", true)]
    [InlineData("/gider-pusulalari/01ABC/belge", true)]
    [InlineData("/gider-pusulalari", false)]
    [InlineData("/gider-pusulalari/01ABC", false)]
    [InlineData("/islemler", false)]
    [InlineData("/", false)]
    public void Q94_belge_rotasi_dogru_taninir(string yol, bool beklenen) =>
        Assert.Equal(beklenen, UstBar.BelgeSayfasi(yol));

    [Fact]
    public void Q94_sozluk_TR_ve_EN_dolu()
    {
        foreach (var m in new[] { CiktiBelgeSozlugu.Tr, CiktiBelgeSozlugu.En })
        {
            Assert.False(string.IsNullOrWhiteSpace(m.OnizlemeBasligi));
            Assert.False(string.IsNullOrWhiteSpace(m.BelgeNoKayitNumarasi));
        }
        Assert.NotEqual(CiktiBelgeSozlugu.Tr.OnizlemeBasligi, CiktiBelgeSozlugu.En.OnizlemeBasligi);
    }
}

[Collection("http")]
public sealed class Q94HttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private string HareketYaz(Kiraci k, string tur, decimal tl, decimal has, string? fisId = null)
    {
        using var db = f.Vt.OlusturSistem();
        var id = Kimlik.Ulid();
        db.Hareketler.Add(new Hareket
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Tur = tur,
            Tarih = DateTime.UtcNow, TlTutari = tl, HasGrami = has, ParaBirimi = "TRY",
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            FisId = fisId, Aciklama = "Q9-4 test kalemi", Olusturuldu = DateTime.UtcNow,
        });
        db.SaveChanges();
        return id;
    }

    private static string[] Hucreler(System.Text.Json.JsonElement j, int satir = 0) =>
        j.GetProperty("bolumler")[0].GetProperty("satirlar")[satir].GetProperty("hucreler")
            .EnumerateArray().Select(x => x.GetString()!).ToArray();

    [Fact]
    public async Task Q94_satis_fisi_kalemi_ISARETSIZ_ve_sutun_bicimli()
    {
        var k = YeniKiraci("Q94 satış");
        var id = HareketYaz(k, "SATIS", 45_000m, -13.9232m);

        var c = await f.Ortam.Istemci(k).GetAsync($"/api/v1/cikti/{BelgeTurleri.SatisFisi}/{id}");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var j = await c.Json();

        var h = Hucreler(j);
        Assert.Equal("13.9232", h[2]);           // 4 hane, işaretsiz (Invariant; biçim istemcide)
        Assert.Equal("45000.00", h[3]);
        var bolum = j.GetProperty("bolumler")[0];
        Assert.Equal(["METIN", "METIN", "HAS", "PARA"],
            bolum.GetProperty("sutunBicimleri").EnumerateArray().Select(x => x.GetString()!).ToArray());
        Assert.Equal("TRY", bolum.GetProperty("satirlar")[0].GetProperty("paraBirimi").GetString());

        var tutarlar = j.GetProperty("tutarlar").EnumerateArray().ToList();
        Assert.Equal("Toplam tutar", tutarlar[0].GetProperty("etiket").GetString());
        Assert.Equal(45_000m, tutarlar[0].GetProperty("deger").GetDecimal());
        Assert.Equal("Toplam has gram", tutarlar[1].GetProperty("etiket").GetString());
        Assert.Equal(13.9232m, tutarlar[1].GetProperty("deger").GetDecimal());
        Assert.True(j.GetProperty("belgeNoKayitKimligi").GetBoolean());
    }

    [Fact]
    public async Task Q94_alis_fisinde_eksi_TL_kagida_cikmaz()
    {
        var k = YeniKiraci("Q94 alış");
        var id = HareketYaz(k, "ALIS", -80_000m, 27.8464m);

        var j = await (await f.Ortam.Istemci(k).GetAsync($"/api/v1/cikti/{BelgeTurleri.SatisFisi}/{id}")).Json();
        var h = Hucreler(j);
        Assert.Equal("27.8464", h[2]);
        Assert.Equal("80000.00", h[3]);
        var tutarlar = j.GetProperty("tutarlar").EnumerateArray().ToList();
        Assert.Equal(80_000m, tutarlar[0].GetProperty("deger").GetDecimal());
        Assert.Equal("Toplam tutar", tutarlar[0].GetProperty("etiket").GetString());
    }

    [Fact]
    public async Task Q94_karisik_yonlu_fiste_NET_yon_etikette_isaret_yok()
    {
        var k = YeniKiraci("Q94 karışık");
        var fis = Kimlik.Ulid();
        var satis = HareketYaz(k, "SATIS", 45_000m, -13.9232m, fis);
        HareketYaz(k, "ALIS", -80_000m, 27.8464m, fis);

        var j = await (await f.Ortam.Istemci(k).GetAsync($"/api/v1/cikti/{BelgeTurleri.SatisFisi}/{satis}")).Json();
        var tutarlar = j.GetProperty("tutarlar").EnumerateArray().ToList();
        Assert.Equal("Net tutar — müşteriye ödenen", tutarlar[0].GetProperty("etiket").GetString());
        Assert.Equal(35_000m, tutarlar[0].GetProperty("deger").GetDecimal());
        Assert.Equal("Net has gram — müşteriden alınan", tutarlar[1].GetProperty("etiket").GetString());
        Assert.Equal(13.9232m, tutarlar[1].GetProperty("deger").GetDecimal());
        // Her kalem de işaretsiz.
        foreach (var i in new[] { 0, 1 })
            Assert.DoesNotContain(Hucreler(j, i), x => x.StartsWith('-'));
    }

    [Fact]
    public async Task Q94_cari_ekstresinde_isaret_KORUNUR_bicim_ayni()
    {
        var k = YeniKiraci("Q94 ekstre");
        HareketYaz(k, "SATIS", 1_000m, -5m);

        var j = await (await f.Ortam.Istemci(k).GetAsync($"/api/v1/cikti/{BelgeTurleri.CariEkstre}/{k.CariId}")).Json();
        var bolum = j.GetProperty("bolumler")[0];
        // PARA-DEMO (madde 5): ekstre sütunları Tarih · İşlem · Açıklama · Tutar · Has etkisi ·
        // Borç etkisi · Bakiye. Yön (işaret) artık BAKİYE ETKİSİ sütunlarında taşınır; satış
        // satırının ham has'ı (−5) bakiyeye etki değildir → has etkisi 0 (ekrandaki kuralla aynı).
        Assert.Equal(["METIN", "METIN", "METIN", "PARA", "HAS", "PARA", "PARA"],
            bolum.GetProperty("sutunBicimleri").EnumerateArray().Select(x => x.GetString()!).ToArray());
        var satir = bolum.GetProperty("satirlar").EnumerateArray()
            .Select(s => s.GetProperty("hucreler").EnumerateArray().Select(x => x.GetString()!).ToArray())
            .Single(s => s[2] == "Q9-4 test kalemi");
        Assert.Equal("1000.00", satir[3]);
        Assert.Equal("", satir[4]);   // PARA-DEMO-2 (madde 9): has etkisi olmayan satırda has hücresi BOŞ ("0,0000 has" basılmaz)
        // Veresiye payı bilinmeyen (alan öncesi) satış satırının borç etkisi UYDURULMAZ.
        Assert.Equal("—", satir[5]);
    }
}

public sealed class Q94EkranTestleri : BunitContext
{
    private readonly List<string> istekler = [];

    public Q94EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    /// <summary>Sunucu sözleşmesiyle birebir (CiktiModulu.HareketBelgesi, Q9-4 sonrası).</summary>
    private const string Belge = """
        {"tur":"SATIS_FISI","belgeAdi":"SATIŞ FİŞİ","belgeNo":"01M36T476XD2XVCH13XM7H0EVQ","dayanak":null,
         "duzenlemeTarihi":"2026-09-24T08:00:00Z",
         "duzenleyen":{"unvan":"Test Kuyumcu","sube":"Merkez","adres":"Kuyumcular Çarşısı 1","vergiDairesi":"Beyazıt","vergiNo":"1234567890","telefon":null},
         "karsi":{"baslik":"Müşteri","ad":"Ayşe Yılmaz","vergiNo":null,"adres":null,"telefon":null},
         "bolumler":[{"baslik":"Kalemler","sutunBasliklari":["Tür","Açıklama","Has gram","Tutar"],
           "satirlar":[{"hucreler":["Satış","22 Ayar Burma Bilezik","13.9232","45000.00"],"vurgu":false,"paraBirimi":"TRY"}],
           "not":null,"sutunBicimleri":["METIN","METIN","HAS","PARA"]}],
         "tutarlar":[{"etiket":"Toplam tutar","deger":45000.00,"paraBirimi":"TRY","toplam":true},
                     {"etiket":"Toplam has gram","deger":13.9232,"paraBirimi":"HAS_GRAM","toplam":false}],
         "imzaBasliklari":["Satıcı","Alıcı"],"not":null,"uyari":null,"belgeNoKayitKimligi":true}
        """;

    private sealed class SahteApi(Q94EkranTestleri sahip) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            lock (sahip.istekler) sahip.istekler.Add(istek.RequestUri!.PathAndQuery);
            var g = istek.RequestUri!.AbsolutePath.Contains("/cikti/", StringComparison.Ordinal) ? Belge : "{}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Cikti> Ekran()
    {
        var e = Render<Kuyumcu.Web.Pages.Cikti>(p => p
            .Add(x => x.Tur, BelgeTurleri.SatisFisi).Add(x => x.Id, "01M36T476XD2XVCH13XM7H0EVQ"));
        e.WaitForElement("#cikti-belge");
        return e;
    }

    [Fact]
    public void Q94_kalem_tutari_TL_bicimiyle_ham_45000_00_YOK()
    {
        var e = Ekran();
        var hucreler = e.FindAll("#cikti-belge tbody tr")[0].QuerySelectorAll("td").Select(x => x.TextContent.Trim()).ToList();
        Assert.Equal(Bicim.BelgePara(45000m, "TRY"), hucreler[3]);
        var metin = e.Find("#cikti-belge").TextContent;
        Assert.DoesNotContain("45000.00", metin, StringComparison.Ordinal);
        Assert.DoesNotContain("TRY", metin, StringComparison.Ordinal);
    }

    [Fact]
    public void Q94_has_gram_ISARETSIZ_ve_kalem_toplam_AYNI_bicim()
    {
        var e = Ekran();
        var kalem = e.FindAll("#cikti-belge tbody tr")[0].QuerySelectorAll("td")[2].TextContent.Trim();
        Assert.Equal("13,9232 has", kalem);
        var toplamSatiri = e.FindAll("#cikti-belge tr").Single(t => t.TextContent.Contains("Toplam has gram", StringComparison.Ordinal));
        Assert.Contains(kalem, toplamSatiri.TextContent, StringComparison.Ordinal);
        var metin = e.Find("#cikti-belge").TextContent;
        Assert.DoesNotContain("-13", metin, StringComparison.Ordinal);
        Assert.DoesNotContain("13.923", metin, StringComparison.Ordinal);
    }

    [Fact]
    public void Q94_sayi_sutunlari_saga_yaslanir_metin_sutunu_yaslanmaz()
    {
        var e = Ekran();
        var td = e.FindAll("#cikti-belge tbody tr")[0].QuerySelectorAll("td");
        Assert.Contains("belge-sag", td[3].ClassName ?? "", StringComparison.Ordinal);
        Assert.Contains("belge-sag", td[2].ClassName ?? "", StringComparison.Ordinal);
        Assert.DoesNotContain("belge-sag", td[1].ClassName ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void Q94_sayfada_TEK_h1_belge_basligi_ve_tekrar_eden_h2_YOK()
    {
        var e = Ekran();
        var h1 = e.FindAll("h1");
        Assert.Single(h1);
        Assert.Equal("SATIŞ FİŞİ", h1[0].TextContent.Trim());
        Assert.DoesNotContain(e.FindAll("h2"), h => h.TextContent.Trim() == "SATIŞ FİŞİ");
        // Araç şeridi mevcut id'sini korur ama başlık öğesi DEĞİLDİR.
        var serit = e.Find("#sayfa-basi-cikti-belge-baslik");
        Assert.Equal("P", serit.TagName);
        Assert.Equal(CiktiBelgeSozlugu.Tr.OnizlemeBasligi, serit.TextContent.Trim());
        Assert.NotNull(e.Find("#sayfa-basi-cikti-belge-eylem #cikti-yazdir"));
        Assert.NotNull(e.Find("#sayfa-basi-cikti-belge-filtre #cikti-geri"));
    }

    /// <summary>
    /// TOPLU-2 tur 2 (tasarım hakemi KRİTİK): "Bu numara sistemdeki kayıt numarasıdır…" teknik
    /// açıklama satırı ekrandan KALDIRILDI; numara uydurulmaz, kayıt kimliği TEK SATIR, küçük ve
    /// soluk basılır (sınıf belge-no-kimlik). Eski beklenti ("açıklama ekranda yazar") bu kararla değişti.
    /// </summary>
    [Fact]
    public void Q94_belge_no_kayit_kimligi_tek_satir_soluk_teknik_aciklama_YOK()
    {
        var e = Ekran();
        Assert.Empty(e.FindAll("#cikti-no-aciklama"));
        Assert.DoesNotContain("sistemdeki kayıt numarası", e.Markup, StringComparison.Ordinal);
        var no = e.Find("#cikti-no");
        // TOPLU-5 tur 2: ham 26 haneli kimlik yerine kısa kayıt kodu (son 8 hane), tamamı title'da.
        Assert.Equal("XM7H-0EVQ", no.TextContent.Trim());
        Assert.Contains("01M36T476XD2XVCH13XM7H0EVQ", no.GetAttribute("title") ?? "", StringComparison.Ordinal);
        Assert.Contains("belge-no-kimlik", no.ClassName ?? "", StringComparison.Ordinal);
    }
}

public sealed class Q94UstBarTestleri : BunitContext
{
    public Q94UstBarTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/kur/guncel", StringComparison.Ordinal) =>
                    """{"id":"kur-1","tenantId":"t1","altinHasFiyati":6877,"gramAltinFiyati":6912.40,"usdTry":41.95,"eurTry":49.10,"gbpTry":56.30,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-24T09:02:00Z","ayarFiyatlari":[]}""",
                var y when y.EndsWith("/bildirimler/sayi", StringComparison.Ordinal) => """{"sayi":0,"enYuksekOnem":null}""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<UstBar> Ciz(string rota)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", "BASIT", "BASIT"));
        Services.GetRequiredService<NavigationManager>().NavigateTo(rota);
        return Render<UstBar>();
    }

    [Fact]
    public void Q94_belge_rotasinda_ust_bar_h1_CIZMEZ_rota_adi_yine_gorunur()
    {
        var e = Ciz("/cikti/SATIS_FISI/01ABC");
        Assert.Empty(e.FindAll("h1"));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#ust-bar-rota-adi").TextContent));
    }

    [Fact]
    public void Q94_pusula_belgesinde_de_ust_bar_h1_CIZMEZ()
    {
        var e = Ciz("/gider-pusulalari/01ABC/belge");
        Assert.Empty(e.FindAll("h1"));
    }

    [Fact]
    public void Q94_diger_rotalarda_ust_bar_h1_AYNEN_durur()
    {
        var e = Ciz("/islemler");
        Assert.Single(e.FindAll("h1"));
        Assert.Empty(e.FindAll("#ust-bar-rota-adi"));
    }
}

public sealed class Q94SayfaBasiTestleri : BunitContext
{
    [Fact]
    public void Q94_SayfaBasi_varsayilanda_h2_cizer_davranis_DEGISMEZ()
    {
        var e = Render<Kuyumcu.Web.Bilesenler.SayfaBasi>(p => p.Add(x => x.Anahtar, "deneme").Add(x => x.Baslik, "Başlık"));
        Assert.Equal("H2", e.Find("#sayfa-basi-deneme-baslik").TagName);
    }

    [Fact]
    public void Q94_SayfaBasi_baslik_ogesi_yoksa_AYNI_id_ve_sinifla_p_cizer()
    {
        var e = Render<Kuyumcu.Web.Bilesenler.SayfaBasi>(p => p
            .Add(x => x.Anahtar, "deneme").Add(x => x.Baslik, "Başlık").Add(x => x.BaslikOgesiYok, true));
        var b = e.Find("#sayfa-basi-deneme-baslik");
        Assert.Equal("P", b.TagName);
        Assert.Contains("sayfa-basi-baslik", b.ClassName ?? "", StringComparison.Ordinal);
        Assert.Empty(e.FindAll("h1, h2, h3"));
    }
}

/// <summary>
/// Q9-4 — 375 px ölçümünde bulunan sığma kusurlarının DOM kilidi (mevcut sınıflar:
/// .hucre-tek-satir = tek satır, .kir-uzun = gerekirse kır). Görünürlüğün kendisi
/// tarayıcıda ölçülür (rapor tablosu); bu testler sınıfın sökülmesini yakalar.
/// </summary>
public sealed class Q94SigmaKilidiTestleri : UyumEkranTemeli
{
    [Fact]
    public void Q94_pusula_basligi_tek_satir_tutar_yaziyla_kendi_satirinda_kirilabilir()
    {
        var e = Render<Kuyumcu.Web.Pages.PusulaBelgesi>(p => p.Add(c => c.Id, "p1"));
        e.WaitForElement("article.belge h1");
        Assert.Contains("hucre-tek-satir", e.Find("article.belge h1").ClassName ?? "", StringComparison.Ordinal);
        var yazi = e.Find(".belge-yazi strong");
        Assert.Contains("kir-uzun", yazi.ClassName ?? "", StringComparison.Ordinal);
        Assert.NotNull(e.Find(".belge-yazi br + strong"));
        var hasTh = e.FindAll("article.belge th").Single(t => t.TextContent.Contains("Has", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("hucre-tek-satir", hasTh.ClassName ?? "", StringComparison.Ordinal);
    }
}

public sealed class Q94CiktiSigmaTestleri : BunitContext
{
    private const string Belge = """
        {"tur":"SATIS_FISI","belgeAdi":"SATIŞ FİŞİ","belgeNo":"01M36T476XD2XVCH13XM7H0EVQ","dayanak":null,
         "duzenlemeTarihi":"2026-09-24T08:00:00Z",
         "duzenleyen":{"unvan":"Test Kuyumcu","sube":"Merkez","adres":"Kuyumcular Çarşısı 1","vergiDairesi":"Beyazıt","vergiNo":"1234567890","telefon":null},
         "karsi":null,
         "bolumler":[{"baslik":"Kalemler","sutunBasliklari":["Tür","Açıklama","Has gram","Tutar"],
           "satirlar":[{"hucreler":["Satış","22 Ayar Burma Bilezik","13.9232","45000.00"],"vurgu":false,"paraBirimi":"TRY"}],
           "not":null,"sutunBicimleri":["METIN","METIN","HAS","PARA"]}],
         "tutarlar":[{"etiket":"Toplam tutar","deger":45000.00,"paraBirimi":"TRY","toplam":true}],
         "imzaBasliklari":["Satıcı","Alıcı"],"not":null,"uyari":null,"belgeNoKayitKimligi":true}
        """;

    [Fact]
    public void Q94_belge_basligi_tek_satir_numara_kirilabilir_sayi_hucresi_tek_satir()
    {
        var belge = System.Text.Json.JsonSerializer.Deserialize<OrtakBelge>(Belge,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!;
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        var e = Render<A4Belge>(p => p.Add(x => x.Belge, belge).Add(x => x.Onek, "cikti"));
        Assert.Contains("hucre-tek-satir", e.Find("#cikti-belge h1").ClassName ?? "", StringComparison.Ordinal);
        // TOPLU-2 tur 2: okunur numarası olmayan (kayıt kimliği) belgede numara tek satır/küçük/soluk.
        Assert.Contains("belge-no-kimlik", e.Find("#cikti-no").ClassName ?? "", StringComparison.Ordinal);
        var td = e.FindAll("#cikti-belge tbody tr")[0].QuerySelectorAll("td");
        Assert.Contains("hucre-tek-satir", td[2].ClassName ?? "", StringComparison.Ordinal);
        Assert.Contains("hucre-tek-satir", td[3].ClassName ?? "", StringComparison.Ordinal);
        Assert.DoesNotContain("hucre-tek-satir", td[1].ClassName ?? "", StringComparison.Ordinal);
    }
}
