using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-4 — ALTIN BACAĞI PANELİ (bUnit) · KARAR-HAS-DEFTERI § 4.5
//
// Kilitlenenler:
//   • B4-33: FARK 0,00 olmadan kayıt KİLİTLİ ve sebebi yazılı (T-26)
//   • B4-32: satıcı kimliği görünür; eksikse kilit (T-26)
//   • B4-35: iki belge kayıttan ÖNCE söylenir (T-26)
//   • B4-31 / B4-34: gram · ayar · fiyat değişince has ve TL YENİDEN türetilir;
//     TL/has için GİRİŞ ALANI YOK (T-27)
//   • K-KUR-4: oran yoksa fiyat ön-DOLDURULMAZ; varsa önerilir ve düzenlenir
// Bileşen istek ATMAZ (S-2 kuralı) — sahte API'ye gelen istek sayısı 0 olmalı.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class B4AltinBacagiEkranTestleri : BunitContext
{
    private readonly List<string> istekler = [];
    private AltinBacagiDurumu? son;

    private static AltinBacagiAyarlari AyarlarVarsayilan(decimal? iskonto = null) => new(
    [
        new("NIHAI_TUKETICI", 0m, true),
        new("ESNAF_MUAF", 0.02m, true),
        new("MUKELLEF", 0m, false),
    ], iskonto, SaticiAdresiZorunlu: true);

    public B4AltinBacagiEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new KaydedenIsleyici(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class KaydedenIsleyici(List<string> kayit) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            kayit.Add($"{r.Method} {r.RequestUri}");
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
        }
    }

    private IRenderedComponent<AltinBacagiPaneli> Panel(
        decimal fis = 20_000m, decimal diger = 10_840m, decimal? hasFiyati = 6_827.20m,
        AltinBacagiAyarlari? ayarlar = null) =>
        Render<AltinBacagiPaneli>(p => p
            .Add(x => x.Ayarlar, ayarlar ?? AyarlarVarsayilan())
            .Add(x => x.HasFiyati, hasFiyati)
            .Add(x => x.FisToplamTl, fis)
            .Add(x => x.DigerBacaklarTl, diger)
            .Add(x => x.Degisti, (AltinBacagiDurumu d) => son = d));

    private static void AyarSec(IRenderedComponent<AltinBacagiPaneli> c, string ayarId)
    {
        c.Find("#altin-ayar").Click();
        c.Find($"#altin-ayar-liste li[data-deger='{ayarId}']").Click();
    }

    private static void Doldur(IRenderedComponent<AltinBacagiPaneli> c, string ayar = "K22", string gram = "5", string fiyat = "2000")
    {
        AyarSec(c, ayar);
        c.Find("#altin-gram").Input(gram);
        c.Find("#altin-fiyat").Input(fiyat);
        c.Find("#altin-satici-ad").Input("Ali Veli");
        c.Find("#altin-satici-tckn").Input("10000000146");
        c.Find("#altin-satici-adres").Input("Ulus, Ankara");
    }

    [Fact]
    public void AltinBacagi_Ekran_Fark_Sifir_Olmadan_Kaydetmez()
    {
        // Fiş 20.000 · diğer bacaklar 10.000 · altın net 9.160 → fark −840 → KİLİT.
        var c = Panel(diger: 10_000m, hasFiyati: null);   // ISLEM-BANT-2: canlı kur yok → yalnız mutlak sınır; 2.000 oran bandı dışıydı ve Kaydet artık bant dışında kilitli (bu test FARK kilidini sınar)
        Doldur(c);

        Assert.NotNull(son);
        Assert.False(son!.Kaydedilebilir);
        Assert.Null(son.Bacak);
        Assert.Equal(-840m, son.Fark);
        Assert.Contains("Fark 0,00 TL olmadan fiş kaydedilemez", c.Find("#altin-eksikler").TextContent);
        Assert.Equal("-840", c.Find("#altin-fark").GetAttribute("data-deger"));

        // Diğer bacaklar 10.840'a çıkınca fark 0 → kayıt açılır, bacak gövdesi hazır.
        c.Render(p => p.Add(x => x.DigerBacaklarTl, 10_840m));
        Assert.True(son!.Kaydedilebilir);
        Assert.Equal(0m, son.Fark);
        Assert.Empty(c.FindAll("#altin-eksikler"));
        var govde = System.Text.Json.JsonSerializer.Serialize(son.Bacak);
        Assert.Contains("\"arac\":\"ALTIN\"", govde);
        Assert.Contains("\"tutarTl\":9160", govde);
        Assert.Contains("\"tckn\":\"10000000146\"", govde);

        // B4-32: satıcı adı silinirse fark 0 olsa da KİLİT (sunucuya gitmeden).
        c.Find("#altin-satici-ad").Input("");
        Assert.False(son!.Kaydedilebilir);
        Assert.Contains("ad soyad", c.Find("#altin-eksikler").TextContent, StringComparison.OrdinalIgnoreCase);

        // B4-35: iki belge kayıttan ÖNCE söylenir.
        Assert.Contains("gider pusulası", c.Find("#altin-belgeler").TextContent);   // SATIS-3 t3 (KAYNAK 2 md.7): büyük harf vurgusu kalktı, anlam aynı
        Assert.Contains("gider pusulası", c.Find("#altin-belgeler").TextContent);

        // Bileşen istek ATMAZ.
        Assert.Empty(istekler);
    }

    [Fact]
    public void AltinBacagi_Ekran_Alanlar_Duzenlenebilir()
    {
        var c = Panel();
        Doldur(c);                                   // K22 · 5 g · 2.000
        Assert.Equal("4.58", c.Find("#altin-has").GetAttribute("data-deger"));
        Assert.Equal("9160", c.Find("#altin-tl").GetAttribute("data-deger"));

        c.Find("#altin-gram").Input("10");           // gram değişti
        Assert.Equal("9.16", c.Find("#altin-has").GetAttribute("data-deger"));
        Assert.Equal("18320", c.Find("#altin-tl").GetAttribute("data-deger"));

        AyarSec(c, "K14");                           // ayar değişti: 10 × 0,585
        Assert.Equal("5.85", c.Find("#altin-has").GetAttribute("data-deger"));
        Assert.Equal("11700", c.Find("#altin-tl").GetAttribute("data-deger"));

        c.Find("#altin-fiyat").Input("1000");        // fiyat değişti
        Assert.Equal("5850", c.Find("#altin-tl").GetAttribute("data-deger"));

        // Esnaf muaf seçilince net = brüt − %2 (oran YAPILANDIRMADAN), brüt satırı görünür.
        c.Find("#altin-satici-tip").Click();
        c.Find("#altin-satici-tip-liste li[data-deger='ESNAF_MUAF']").Click();
        Assert.Equal("5733", c.Find("#altin-tl").GetAttribute("data-deger"));
        Assert.Contains(Bicim.Tl(5850m), c.Find("#altin-brut").TextContent);

        // B4-31: TL ve has YAZILAMAZ — giriş alanı yok, yalnız türetilmiş metin.
        Assert.Empty(c.FindAll("input#altin-tl, input#altin-has, textarea#altin-tl, textarea#altin-has"));
        Assert.Equal("P", c.Find("#altin-tl").TagName);
        Assert.Equal("P", c.Find("#altin-has").TagName);
    }

    [Fact]
    public void AltinBacagi_Ekran_Fiyat_Kurdan_Doldurulmaz_Oran_Varsa_Onerilir()
    {
        // K-KUR-4: oran yapılandırılmamış → alan BOŞ, has fiyatı yalnız bilgi.
        var bos = Panel();
        Assert.Equal("", bos.Find("#altin-fiyat").GetAttribute("value") ?? "");
        Assert.Contains("yalnız bilgi", bos.Find("#altin-fiyat-referans").TextContent);
        Assert.Contains("has gram fiyatını girin", bos.Find("#altin-eksikler").TextContent, StringComparison.OrdinalIgnoreCase);

        // Oran %2 yapılandırılmış → 6.827,20 × 0,98 = 6.690,66 önerilir, düzenlenebilir.
        var dolu = Panel(ayarlar: AyarlarVarsayilan(iskonto: 0.02m));
        // P-78: girdi kutusu değeri tr-TR gruplamasız basılır (virgül ondalık); eski
        // Invariant yazım ("1.234") tr-TR çözücüde BELİRSİZ sayılıp kutu kendi değerini
        // geri okuyamazdı. Değer aynı, yazım değişti.
        // BICIM-2: odak DIŞINDA para kutusu gruplu.
        Assert.Equal("6.690,66", dolu.Find("#altin-fiyat").GetAttribute("value"));
        Assert.Contains("önerildi", dolu.Find("#altin-fiyat-kaynak").TextContent);
        dolu.Find("#altin-fiyat").Input("6500");
        Assert.Contains("Elle", dolu.Find("#altin-fiyat-kaynak").TextContent);
    }

    [Fact]
    public void AltinBacagi_Ekran_Ayarlar_Yoksa_Kilitli_ve_Sebep_Yazili()
    {
        var c = Render<AltinBacagiPaneli>(p => p
            .Add(x => x.Ayarlar, (AltinBacagiAyarlari?)null)
            .Add(x => x.FisToplamTl, 100m)
            .Add(x => x.Degisti, (AltinBacagiDurumu d) => son = d));
        Assert.NotNull(c.Find("#altin-ayarlar-yok"));
        Assert.False(son!.Kaydedilebilir);
        Assert.Contains("yüklenemedi", c.Find("#altin-eksikler").TextContent);
    }
}
