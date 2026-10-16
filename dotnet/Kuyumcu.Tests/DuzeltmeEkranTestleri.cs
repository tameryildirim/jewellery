using Bunit;
using Kuyumcu.Api.Modules.Meta;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// DÜZELTME EKRAN SÖZLEŞMESİ — paket UX-1 Faz A [bUnit]
//
// Faz A'da MEVCUT SAYFALARA SIFIR DOM DEĞİŞİKLİĞİ yapılır (Bertan koşul 2);
// bu yüzden burada sayfalar değil, iki YENİ BİLEŞEN sınanır.
//
// ⚠ AŞKIN K3 (M-UX1-05 izolasyonu): "gerekçe kısayken Kaydet kapalı" testi,
//   Kaydet'in DİĞER kapatma sebeplerini önce sağlar (kaydediliyor=false,
//   tutar girilmiş) ve 19→KAPALI / 20→AÇIK çiftini AYNI testte doğrular.
//   Aksi hâlde asgari uzunluk mutasyonu hiçbir şeyi kırmaz.
//
// ⚠ AŞKIN K4: negatif iddia tek başına test değildir — "Düzenle düğmesi YOK"
//   her zaman POZİTİF eşiyle (#degismez-ipucu-{varlik} VAR + gerekçe metni
//   sözlükten gelir) çiftlenir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class DuzeltmeEkranTestleri : BunitContext
{
    public DuzeltmeEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    // ── GerekceAlani ─────────────────────────────────────────────────────────

    /// <summary>
    /// Kaydet düğmesini gerekçenin geçerliliğine VE "kaydediliyor değil" +
    /// "tutar girilmiş" koşullarına bağlar — gerçek Cariler.razor deseni.
    /// </summary>
    private IRenderedComponent<GerekceAlani> Alan(string metin, int? asgari = null)
    {
        const bool kaydediliyor = false;
        const decimal tutar = 100m;

        RenderFragment<bool> kaydet = gecerli => yapi =>
        {
            yapi.OpenElement(0, "button");
            yapi.AddAttribute(1, "id", "dz-kaydet-test");
            yapi.AddAttribute(2, "disabled", !gecerli || kaydediliyor || tutar == 0m);
            yapi.AddContent(3, "Kaydet");
            yapi.CloseElement();
        };

        return Render<GerekceAlani>(p =>
        {
            p.Add(x => x.Id, "dz-gerekce-test");
            p.Add(x => x.Deger, metin);
            if (asgari is not null) p.Add(x => x.AsgariUzunluk, asgari.Value);
            p.Add(x => x.ChildContent, kaydet);
        });
    }

    private static bool Kapali(IRenderedComponent<GerekceAlani> e)
        => e.Find("#dz-kaydet-test").HasAttribute("disabled");

    [Fact]
    public void Gerekce_kisa_iken_kaydet_kapali()
    {
        // 19 karakter → KAPALI (varsayılan asgari 20; parametre VERİLMEZ ki
        // M-UX1-05 varsayılanı bozduğunda bu test kırmızıya dönsün).
        Assert.Equal(19, new string('a', 19).Length);
        Assert.True(Kapali(Alan(new string('a', 19))), "19 karakterde Kaydet AÇIK kalmış.");

        // 20 karakter → AÇIK. Diğer koşullar (kaydediliyor=false, tutar≠0)
        // zaten sağlanmış durumda; tek değişen gerekçe uzunluğudur.
        Assert.False(Kapali(Alan(new string('a', 20))), "20 karakterde Kaydet KAPALI kalmış.");
    }

    [Fact]
    public void Gerekce_bosluklari_SAYILMAZ()
        => Assert.True(Kapali(Alan("   " + new string('a', 17) + "   ")));

    [Fact]
    public void Asgari_uzunluk_PARAMETREDIR_300_de_uygulanir()
    {
        // Aşkın K2: Uyum ekranı 300 ister; bileşende 20 SABİT DEĞİLDİR.
        Assert.True(Kapali(Alan(new string('b', 299), asgari: 300)));
        Assert.False(Kapali(Alan(new string('b', 300), asgari: 300)));
    }

    [Fact]
    public void Gerekce_sayaci_ve_etiketi_ASGARIYI_gosterir()
    {
        var e = Alan(new string('c', 5), asgari: 300);
        Assert.Contains("5 / 300", e.Find("#dz-gerekce-test-sayac").TextContent);
        Assert.Contains("300", e.Find("label[for=dz-gerekce-test]").TextContent);
        Assert.Equal("1000", e.Find("#dz-gerekce-test").GetAttribute("maxlength"));
    }

    [Fact]
    public void Gerekce_yazilinca_DegerChanged_tetiklenir()
    {
        string? son = null;
        var e = Render<GerekceAlani>(p => p
            .Add(x => x.Id, "dz-gerekce-test")
            .Add(x => x.Deger, "")
            .Add(x => x.DegerChanged, EventCallback.Factory.Create<string>(this, v => son = v)));

        e.Find("#dz-gerekce-test").Input("Kasa sayımında eksik çıktı");
        Assert.Equal("Kasa sayımında eksik çıktı", son);
    }

    // ── DegismezIpucu ────────────────────────────────────────────────────────

    /// <summary>
    /// ⚠ Ekran sözleşmesi POLİTİKA HARİTASINDAN sürülür — testin içine sabit
    /// politika yazılmaz. Haritada bir satır yanlış damgalanırsa (M-UX1-01)
    /// bu ekran testi de kırmızıya döner.
    /// </summary>
    private IRenderedComponent<DegismezIpucu> HaritadanIpucu(string varlik, string? alternatif = null)
    {
        var satir = DuzeltmePolitikasi.Varliklar[varlik];
        Assert.Equal(Politikalar.SaltOkunur, satir.Politika);   // düğme değil, ipucu çizilir

        return Render<DegismezIpucu>(p =>
        {
            p.Add(x => x.Varlik, varlik);
            p.Add(x => x.Gerekce, Sozlukler.Tr.Duzeltme.GerekceMetni(satir.GerekceAnahtari));
            if (alternatif is not null) p.Add(x => x.AlternatifYol, alternatif);
        });
    }

    [Fact]
    public void Denetim_satirinda_duzenle_yoktur_ipucu_vardir()
    {
        var e = HaritadanIpucu("DenetimKaydi");

        // POZİTİF eş (Aşkın K4): ipucu VAR ve gerekçesi SÖZLÜKTEN gelir.
        var kutu = e.Find("#degismez-ipucu-DenetimKaydi");
        Assert.Equal("note", kutu.GetAttribute("role"));
        Assert.Contains(Sozlukler.Tr.Duzeltme.DuzenlenemezBaslik, kutu.TextContent);
        Assert.Contains(Sozlukler.Tr.Duzeltme.DenetimGerekce, kutu.TextContent);

        // NEGATİF eş: düğme HİÇ çizilmez — devre dışı düğme de çizilmez
        // (devre dışı düğme "bir yolu var ama sana kapalı" der; yanlış mesaj).
        Assert.Empty(e.FindAll("button"));
        Assert.Empty(e.FindAll("[disabled]"));
    }

    [Fact]
    public void Degismez_ipucu_ALTERNATIF_yolu_gosterir()
    {
        var e = HaritadanIpucu("CariBakiye", "Cari kartı → Düzeltme fişi");
        var kutu = e.Find("#degismez-ipucu-CariBakiye");
        Assert.Contains(Sozlukler.Tr.Duzeltme.AlternatifYol, kutu.TextContent);
        Assert.Contains("Cari kartı → Düzeltme fişi", kutu.TextContent);
        Assert.Empty(e.FindAll("button"));
    }

    [Fact]
    public void Degismez_ipucu_ALTERNATIF_yoksa_satiri_CIZMEZ()
    {
        var e = HaritadanIpucu("MasakBeyani");
        Assert.Empty(e.FindAll(".degismez-alternatif"));
        Assert.Contains(Sozlukler.Tr.Duzeltme.MasakGerekce, e.Find("#degismez-ipucu-MasakBeyani").TextContent);
    }
}
