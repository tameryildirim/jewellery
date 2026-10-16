using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-2 tur 2 — doğrulayıcı KRİTİK: PRO /islemler 220.000 ₺ satışta aynı MASAK eşik cümlesi 4 GÖRÜNÜR
// blokta çıkıyordu (#masak-baslik, #masak-kural, #islem-kaydet-kilit, #islem-pro-amac-etiket). KABUL "uyarı tekrar
// sayacı 1" mod ayırmaz. Hedef: eşik form başlığında + PRO "Yasal kapılar" panelinde; kilit sebebinde 0; kural satırı
// yalnız saklama notu (panel kademeyi zaten MASAK atfıyla söylüyor). YK2_01 yalnız /cariler'i ölçüyordu.
// ═════════════════════════════════════════════════════════════════════════════
public sealed partial class SATIS3EkranTestleri
{
    private static string Yk2Gereksinim(decimal olcu) =>
        $$"""{"kademe":"AMAC_BEYANI","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20,"olcuTutarTl":{{olcu.ToString(CultureInfo.InvariantCulture)}},"islemTutarTl":{{olcu.ToString(CultureInfo.InvariantCulture)}}}""";

    /// <summary>Görünür (gizli atası olmayan) yaprak elemanlarda eşik cümlesi kaç blokta geçiyor.</summary>
    private static List<string> EsikBloklari(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, decimal esik)
    {
        var desen = Regex.Escape(Bicim.Tl(esik)) + @"\s*ve üstünde";
        return e.FindAll("*")
            .Where(x => x.Children.Length == 0 && Regex.IsMatch(x.TextContent, desen) && Gorunur(x))
            .Select(x => $"#{x.Id} :: {x.TextContent.Trim()}").ToList();
    }

    [Fact]
    public void YK2_11_PRO_islemler_220000_esik_baslik_ve_panelde_kilit_sebebinde_ESIK_YOK()
    {
        masakEsikJson = Tur3Esik();
        gereksinimOzel = v => v.Contains("tutar=220000") ? Yk2Gereksinim(220_000m) : null;
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        SeciciYardim.Sec(e, "islem-cari", "c1");
        FiyatliKalem(e, "u1", "220000", 1);
        SeciciYardim.Sec(e, "islem-odeme", "KART");   // nakit (tevsik) kilidi bu testin konusu değil
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kart--masak")), Bekle);
        Satis4Yardim.MasakBelgeNoSil(e);
        var y = YasalKapi2Sozlugu.Tr;

        e.WaitForAssertion(() => PTezgahTurKimlikYardim.EksikKilit(e.Find("#islem-kaydet").GetAttribute("title"), ParaDemo3Sozlugu.Tr.AlanIslemAmaci), Bekle);   // P-TEZGAH-TUR-KIMLIK K4: eksik alanın adı
        Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled"));
        foreach (var k in e.FindAll("#islem-kaydet-kilit"))
        {
            Assert.DoesNotContain(Bicim.Tl(200_000m), k.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain(Bicim.Tl(370_000m), k.TextContent, StringComparison.Ordinal);
        }

        // 200.000: form başlığı + PRO panel amaç satırı = 2; 370.000: yalnız PRO panel kimlik satırı = 1.
        var b200 = EsikBloklari(e, 200_000m);
        Assert.True(b200.Count == 2, string.Join(" ‖ ", b200));
        Assert.Contains(b200, s => s.StartsWith("#masak-baslik", StringComparison.Ordinal));
        Assert.Contains(b200, s => s.StartsWith("#islem-pro-amac-etiket", StringComparison.Ordinal));
        var b370 = EsikBloklari(e, 370_000m);
        Assert.True(b370.Count == 1 && b370[0].StartsWith("#islem-pro-kimlik-etiket", StringComparison.Ordinal), string.Join(" ‖ ", b370));

        // Kural satırı yalnız saklama notu; MASAK atfı panelde.
        Assert.Equal(YasalParamSozlugu.Tr.Saklama, e.Find("#masak-kural").TextContent.Trim());
        Assert.Contains("MASAK", e.Find("#islem-pro-kimlik-etiket").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void YK2_12_BASIT_islemler_220000_esik_tek_yerde_kilit_sade()
    {
        masakEsikJson = Tur3Esik();
        gereksinimOzel = v => v.Contains("tutar=220000") ? Yk2Gereksinim(220_000m) : null;
        var e = Ekran(ArayuzModuServisi.Basit);
        SeciciYardim.Sec(e, "islem-cari", "c1");
        FiyatliKalem(e, "u1", "220000", 1);
        SeciciYardim.Sec(e, "islem-odeme", "KART");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kart--masak")), Bekle);
        Satis4Yardim.MasakBelgeNoSil(e);
        e.WaitForAssertion(() => PTezgahTurKimlikYardim.EksikKilit(e.Find("#islem-kaydet").GetAttribute("title")), Bekle);   // P-TEZGAH-TUR-KIMLIK K4: eksik alanın adı
        var b200 = EsikBloklari(e, 200_000m);
        Assert.True(b200.Count == 1 && b200[0].StartsWith("#masak-baslik", StringComparison.Ordinal), string.Join(" ‖ ", b200));
        Assert.Empty(EsikBloklari(e, 370_000m));
    }
}
