using Kuyumcu.Core.I18n;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-BELGE 28.09 (devir notu 2) — /raporlar "Stok değeri" sekmesi, REYON STOK
// kartı ve fatura/evrak tablolarında tek satır hücreler. Başsız Chrome turu
// (d28e, 375/960) üç kusur buldu:
//   ZB26  reyon has gramı F3 "g" (0,0004 has kayboluyordu) → Bicim.Has, 4 hane
//   ZB27  sunucunun geliştirici notu (`toplamAltinHasGram` ters tırnaklı) ekrana
//         basılıyordu → basılmaz; kullanıcı notu (madenler ayrı) görünür
//   ZB28  iki yerel <select> (aynı ekranda Yıl/Ay ortak Secici) → ortak Secici,
//         id'ler aynı; tek şubede şube seçicisi, reyon yokken reyon seçicisi yok (UX-S22)
//   ZB29  fatura KDV hücresi "150 ₺ (%20)" ve evrak "Veren" adı 960'ta iki satıra
//         kırılıyordu → hucre-tek-satir
// ═════════════════════════════════════════════════════════════════════════════
public sealed class ZoBelgeReyonTestleri : BunitContext
{
    private string subeler = """[{"id":"s1","ad":"Merkez","aktif":true},{"id":"s2","ad":"Kadıköy","aktif":true}]""";
    private string reyonlar = """[{"id":"r1","subeId":"s1","ad":"Vitrin 1","tip":"VITRIN","sorumluKullaniciId":null,"aktif":true,"urunSayisi":2,"yerlesikAdet":5}]""";

    private string? reyonStokYaniti;

    private const string ReyonStok = """
        {"reyonlar":[{"reyonId":"r1","reyonAdi":"Vitrin 1","tip":"VITRIN","aktif":true,"sorumluKullaniciId":null,
                      "subeId":"s1","subeAdi":"Merkez","adet":5,"urunSayisi":2,
                      "madenler":[{"maden":"ALTIN","madenAdi":"Altın","adet":2,"toplamGram":0.0004},
                                  {"maden":"GUMUS","madenAdi":"Gümüş","adet":3,"toplamGram":299.7}],
                      "altinHasGram":0.0004}],
         "atanmamis":[{"subeId":"s1","subeAdi":"Merkez","adet":1,
                       "madenler":[{"maden":"ALTIN","madenAdi":"Altın","adet":1,"toplamGram":1.2345}]}],
         "toplamAltinHasGram":0.0004,
         "uyari":"Raporda birden çok maden var. `toplamAltinHasGram` YALNIZ altın has gramıdır; diğer madenler her satırın `madenler` kırılımında ayrı verilir — farklı madenlerin gramları toplanmaz."}
        """;

    public ZoBelgeReyonTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi(ZoBelgeReyonTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/aylik") =>
                    """{"yil":2026,"ay":9,"satis":{"sayisi":0,"toplamTl":0,"toplamHas":0,"altinToplamHas":0,"madenDagilimi":[]},"alis":{"sayisi":0,"toplamTl":0,"toplamHas":0,"altinToplamHas":0,"madenDagilimi":[]},"tahsilat":{"toplamTl":0},"veresiye":{"toplamTl":0},"karTl":null,"karKaynagi":"HESAPLANAMADI","satisAlisFarkiTl":0,"netHasGramHareketi":0,"madenDagilimi":[],"madenUyarisi":null,"hareketSayisi":0,"gunler":[]}""",
                var y when y.EndsWith("/raporlar/cari-yaslandirma") =>
                    """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""",
                var y when y.EndsWith("/raporlar/stok-degeri") =>
                    """{"urunler":[],"toplamHas":0,"madenDagilimi":[],"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false,"uyari":null}""",
                var y when y.EndsWith("/raporlar/reyon-stok") => t.reyonStokYaniti ?? ReyonStok,
                var y when y.EndsWith("/raporlar/stok-yaslandirma") =>
                    """{"olculduAn":"2026-09-22T06:00:00Z","subeId":null,"kovalar":[],"toplamAdet":0,"altinHasGram":0,"madenDagilimi":[],"yasiBilinmeyenAdet":0,"devir":null,"oluStok":null,"guvenilirlik":"YUKSEK","uyarilar":[]}""",
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") =>
                    """{"tarih":"2026-09-17","tahsilat":{"nakit":0,"kart":0,"havale":0,"diger":0,"toplam":0},"satis":{"sayisi":0,"toplamTl":0,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":0,"faturasizTl":0},"alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},"hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},"fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},"fark":0,"mutabik":true,"uyarilar":[],"faturasizIslemler":[],"faturaEslesme":null}""",
                var y when y.EndsWith("/subeler") => t.subeler,
                var y when y.EndsWith("/reyonlar") => t.reyonlar,
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Raporlar> ReyonKarti()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#reyon-stok-has-baslik")));
        return e;
    }

    [Fact]
    public void ZB26_Reyon_stok_has_DORT_hane_0_0004_kaybolmaz()
    {
        var e = ReyonKarti();
        var kart = e.Find("#reyon-stok-kart").TextContent;
        Assert.Contains($"Altın: {Bicim.Has(0.0004m)}", kart, StringComparison.Ordinal);
        Assert.Contains("0,0004 has", kart, StringComparison.Ordinal);
        Assert.Contains($"Altın: {Bicim.Has(1.2345m)}", kart, StringComparison.Ordinal);   // atanmamış satır
        Assert.DoesNotContain("0,000 g", kart, StringComparison.Ordinal);
    }

    [Fact]
    public void ZB27_Sunucunun_gelistirici_notu_ekrana_basilmaz_kullanici_notu_gorunur()
    {
        var e = ReyonKarti();
        var kart = e.Find("#reyon-stok-kart");
        Assert.DoesNotContain("toplamAltinHasGram", kart.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("`", kart.TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#reyon-stok-uyari"));
        var dil = Services.GetRequiredService<DilServisi>();
        Assert.Equal(dil.T.Yerlesim.MadenAyriToplanir, e.Find("#reyon-stok-maden-notu").TextContent.Trim());
    }

    [Fact]
    public void ZB28_Reyon_suzgeci_ortak_Secici_yerel_select_YOK_idler_ayni()
    {
        var e = ReyonKarti();
        Assert.Empty(e.FindAll("#reyon-stok-kart select"));
        Assert.Contains("secici-tetik", e.Find("#rs-sube").ClassName, StringComparison.Ordinal);
        Assert.Contains("secici-tetik", e.Find("#rs-reyon").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void ZB28b_Tek_subede_sube_secicisi_reyon_yokken_reyon_secicisi_CIZILMEZ()
    {
        subeler = """[{"id":"s1","ad":"Merkez","aktif":true}]""";
        reyonlar = "[]";
        var e = ReyonKarti();
        Assert.Empty(e.FindAll("#rs-sube"));
        Assert.Empty(e.FindAll("#rs-reyon"));
        // Rapor yine çizilir (süzgeç gizlendi, veri gizlenmedi).
        Assert.Contains("Vitrin 1", e.Find("#reyon-stok-kart").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ZB28c_Tek_subede_reyon_varken_YALNIZ_reyon_secicisi_cizilir()
    {
        // M5-3 dersi: ZB28b dış koşul yüzünden şube kuralını sınamıyordu.
        subeler = """[{"id":"s1","ad":"Merkez","aktif":true}]""";
        var e = ReyonKarti();
        Assert.Empty(e.FindAll("#rs-sube"));
        Assert.NotNull(e.Find("#rs-reyon"));
    }

    [Fact]
    public void ZB30_Reyon_stok_bos_durumu_RAPOR_metni_tasir_urun_metni_DEGIL()
    {
        // Doğrulayıcı 28.09 (DÜŞÜK): mağaza çapındaki raporda "Bu üründen…" yazıyordu.
        reyonStokYaniti = """{"reyonlar":[],"atanmamis":[]}""";
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#reyon-stok-bos")));
        var dil = Services.GetRequiredService<DilServisi>();
        var bos = e.Find("#reyon-stok-bos").TextContent;
        Assert.Contains(ZoBelgeSozlugu.Tr.ReyonStokBos, bos, StringComparison.Ordinal);
        Assert.DoesNotContain(dil.T.Yerlesim.YerlesimYok, bos, StringComparison.Ordinal);
        Assert.DoesNotContain("Bu üründen", e.Find("#reyon-stok-kart").TextContent, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(ZoBelgeSozlugu.En.ReyonStokBos));
        Assert.NotEqual(ZoBelgeSozlugu.Tr.ReyonStokBos, ZoBelgeSozlugu.En.ReyonStokBos);
    }

    [Fact]
    public void ZB29_Fatura_KDV_ve_evrak_veren_hucreleri_tek_satir()
    {
        var kok = CssKaynak.Kok();
        var fat = File.ReadAllText(Path.Combine(kok, "Kuyumcu.Web", "Pages", "Faturalar.razor"));
        var evr = File.ReadAllText(Path.Combine(kok, "Kuyumcu.Web", "Pages", "Evraklar.razor"));
        // PAKET GOC-A: liste ortak ızgarada — KDV kolonunun hücre sınıfı tek satır (data-etiket ızgaradan, başlıkla AYNI).
        Assert.Matches(@"Anahtar = ""kdv"", Baslik = Dil\.T\.Fatura\.KdvTutar,[^\n]*\n\s*HucreSinif = _ => ""[^""]*\bhucre-tek-satir\b", fat);
        // 29.09 devam 6 (tasarım hakemi YÜKSEK): veren adı artık tek satıra ZORLANMAZ (PRO 1440'ta tabloyu
        // taşırıp eylem sütununu kesiyordu) — en çok İKİ satır sarar, tam ad title'da, sütunun en az genişliği var.
        // PAKET GOC-B: evrak listesi ortak ızgarada — veren hücresi sınıfı kolon tanımında AYNI (zo-metin-sutun), en çok
        // iki satır (zo-iki-satir), tam ad title'da; data-etiket ızgaradan (başlıkla AYNI).
        Assert.Matches(@"Anahtar = ""veren"", Baslik = EkranM\.EvrakSutunVeren,[^\n]*\n[^\n]*HucreSinif = _ => ""zo-metin-sutun""[^\n]*\n\s*Hucre = e => @<span class=""zo-iki-satir"" title=""@e\.KaynakCariAd"">", evr);
    }
}
