using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q9-2 — TASLAK FATURA "DÜZENLE" ÇEKMECESİNDE SATIRLAR [bUnit]
//
// QA turu 9 (KRİTİK): Düzenle çekmecesinde yalnız cari/tarih/açıklama/toplam
// vardı; satır ekleme/düzeltme yoktu. Bu testler:
//   • çekmecede satırlar SatirIciDuzenle ile düzeltilir (P-54 ucu) ve
//     yeni satır eklenir (POST /faturalar/{id}/kalemler);
//   • toplam EKRANDA hesaplanmaz — sunucunun KalemYaniti'ndan okunur;
//   • boş/geçersiz gönderim alan altında söylenir, istek GİTMEZ;
//   • sunucu 400'ü ilgili alanın altına yazılır;
//   • satırsız taslakta ekleme yok, gerekçe + gerçek yol yazılı;
//   • okuma hatası "satır yok"a çevrilmez (UX-S28);
//   • Ekle düğmesi kaydederken kilitli ve sebebi yazılı (UX-S30).
// Sahte API DURUMLUDUR: POST gelen satırı listeye ekler, toplamı SUNUCU gibi
// kasıtlı FARKLI bir değerle döner (ekranın yerel hesap yapmadığı kanıtlanır).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Q92FaturaKalemEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string kalemKaynagi = "KALEM";
    private string durum = "TASLAK";
    private string kdvTuru = "STANDART";
    private string? postHata;
    private bool kalemOkumaHatasi;
    private TaskCompletionSource? postBekle;
    private decimal sunucuToplami = 1620m;
    private readonly List<string> satirlar =
    [
        Satir("k92a", 1, "Burma Bilezik", 1m, 1000m, 120m, 1120m),
        Satir("k92b", 2, "Gümüş Kolye", 1m, 500m, null, 500m),
    ];

    public Q92FaturaKalemEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Kuyumcu.Web.Bilesenler.KayitSecici.BeklemeMs = 0;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private static string S(decimal d) => d.ToString(CultureInfo.InvariantCulture);

    private static string Satir(string id, int sira, string aciklama, decimal miktar, decimal fiyat,
        decimal? iscilik, decimal satirToplam) => $$"""
        {"id":"{{id}}","sira":{{sira}},"aciklama":"{{aciklama}}","miktar":{{S(miktar)}},"birim":"ADET",
         "ayarId":null,"milyem":null,"gramAgirlik":null,"hasGrami":null,"tasIbaresi":null,
         "birimFiyat":{{S(fiyat)}},"iscilikTl":{{(iscilik is { } i ? S(i) : "null")}},"indirimTutari":null,
         "kdvTuru":"STANDART","kdvOran":0.2,"hasDeger":0,"kdvMatrah":0,"kdvTutar":0,
         "satirToplam":{{S(satirToplam)}}}
        """;

    private string KalemYaniti() => $$"""
        {"faturaId":"f92","faturaNo":null,"kalemKaynagi":"{{kalemKaynagi}}","hasDegerKaynagi":"KALEM",
         "toplam":{{S(sunucuToplami)}},"kalemler":[{{string.Join(",", satirlar)}}]}
        """;

    private string Liste() => $$"""
        {"faturalar":[{"id":"f92","faturaNo":{{(durum == "TASLAK" ? "null" : "\"FTR-2026-000092\"")}},
          "cariAd":"Mehmet Kaya","tur":"SATIS","araToplam":0,"kdvOran":0.2,"kdvTutar":0,
          "toplam":{{S(sunucuToplami)}},"durum":"{{durum}}","bankaHareketId":null,
          "tarih":"2026-09-24T08:00:00Z","kalemKaynagi":"{{kalemKaynagi}}","hasDegerKaynagi":"KALEM",
          "kdvTuru":"{{kdvTuru}}","cariId":"c2","aciklama":null}]}
        """;

    private sealed class SahteApi(Q92FaturaKalemEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add((istek.Method.Method, yol, govde));

            if (istek.Method == HttpMethod.Post && yol == "/api/v1/faturalar/f92/kalemler")
            {
                if (t.postBekle is { } bekle) await bekle.Task;
                if (t.postHata is { } h) return Hata(HttpStatusCode.BadRequest, h);
                var g = JsonDocument.Parse(govde).RootElement;
                var miktar = g.GetProperty("miktar").GetDecimal();
                var fiyat = g.GetProperty("birimFiyat").GetDecimal();
                var aciklama = g.GetProperty("aciklama").GetString() ?? "Test bilezik";
                t.satirlar.Add(Satir("k92c", t.satirlar.Count + 1, aciklama, miktar, fiyat, null, miktar * fiyat));
                // Kasıtlı olarak yerel toplamdan FARKLI: ekran bunu yazmalı.
                t.sunucuToplami = 9999m;
                return Tamam(t.KalemYaniti());
            }
            if (istek.Method == HttpMethod.Patch && yol.StartsWith("/api/v1/faturalar/f92/kalemler/", StringComparison.Ordinal))
            {
                t.sunucuToplami = 3620m;
                return Tamam(t.KalemYaniti());
            }
            if (istek.Method == HttpMethod.Get && yol == "/api/v1/faturalar/f92/kalemler")
                return t.kalemOkumaHatasi
                    ? Hata(HttpStatusCode.InternalServerError, "Satırlar okunamadı (sunucu)")
                    : Tamam(t.KalemYaniti());
            if (yol.Contains("/ozel-matrah-ayari"))
                return Tamam("""{"kod":null,"acik":false,"aralikta":false,"aralik":"801-812","varsayilanOran":0.20,"kaynakBeyani":"kapalı","maliMusavirTeyidi":false}""");
            if (yol.StartsWith("/api/v1/urunler", StringComparison.Ordinal))
                return Tamam("""[{"id":"u1","ad":"Test bilezik"},{"id":"u2","ad":"Tek Taş Yüzük"}]""");
            if (yol.StartsWith("/api/v1/cari", StringComparison.Ordinal))
                return Tamam("""[{"id":"c1","ad":"Ayşe Demir"},{"id":"c2","ad":"Mehmet Kaya"}]""");
            if (istek.Method == HttpMethod.Get
                && (yol == "/api/v1/faturalar" || yol.StartsWith("/api/v1/faturalar?", StringComparison.Ordinal)))
                return Tamam(t.Liste());
            return Tamam("{}");
        }

        private static HttpResponseMessage Tamam(string json) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        private static HttpResponseMessage Hata(HttpStatusCode kod, string mesaj) => new(kod)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { message = mesaj, error = "Bad Request", statusCode = (int)kod }),
                Encoding.UTF8, "application/json"),
        };
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> CekmeceAc()
    {
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-duzenle-f92");
        e.Find("#fk-duzenle-f92").Click();
        e.WaitForElement("#fd-kalem-bolumu");
        return e;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> SatirlarlaCekmeceAc()
    {
        var e = CekmeceAc();
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll(".fd-cekmece-kalem").Count));
        return e;
    }

    private int PostSayisi => istekler.Count(i => i.Metot == "POST" && i.Yol == "/api/v1/faturalar/f92/kalemler");

    [Fact]
    public void Q92_Duzenle_cekmecesinde_SATIRLAR_duzenlenebilir_ve_YENI_SATIR_formu_var()
    {
        var e = SatirlarlaCekmeceAc();
        var cekmece = e.Find("#cekmece-fatura");

        // Her satırın beş alanı gerçek düzenleme kipine bağlı (P-54 ucu).
        foreach (var kalem in new[] { "k92a", "k92b" })
            foreach (var alan in new[] { "aciklama", "miktar", "birimFiyat", "iscilikTl", "indirimTutari" })
                Assert.NotNull(cekmece.QuerySelector($"#si-fatura-kalem-{alan}-{kalem}-ac"));

        // Yeni satır formu oluşturma formuyla AYNI alan kümesini taşır (UX-S26).
        foreach (var id in new[] { "urun", "aciklama", "miktar", "fiyat", "iscilik", "indirim" })
            Assert.NotNull(cekmece.QuerySelector($"#fd-kalem-yeni-{id}"));
        Assert.NotNull(cekmece.QuerySelector("#fd-kalem-yeni-ekle"));
        Assert.Equal(Bicim.Tl(1620m), e.Find("#fd-cekmece-kalem-genel-toplam").TextContent.Trim());
        // Tek satır silinmez: sebep + gerçek yol yazılı, silme düğmesi UYDURULMAZ.
        Assert.NotNull(cekmece.QuerySelector("#degismez-ipucu-FaturaKalemSilme"));
        Assert.Empty(cekmece.QuerySelectorAll("[id^=fd-kalem-sil]"));
    }

    [Fact]
    public void Q92_yeni_satir_POST_edilir_ve_TOPLAM_sunucu_yanitindan_okunur()
    {
        var e = SatirlarlaCekmeceAc();
        var listeOnce = istekler.Count(i => i.Metot == "GET" && i.Yol == "/api/v1/faturalar");

        e.Find("#fd-kalem-yeni-aciklama").Input("Q92 yüzük");
        e.Find("#fd-kalem-yeni-miktar").Input("2");
        e.Find("#fd-kalem-yeni-fiyat").Input("1.250,50");
        // Önizleme (UX-S23): 2 × 1.250,50 = 2.501,00 — yalnız bilgi.
        Assert.Equal(Bicim.Tl(2501m), e.Find("#fd-kalem-yeni-satir").TextContent.Trim());

        e.Find("#fd-kalem-yeni-ekle").Click();

        e.WaitForAssertion(() => Assert.Equal(1, PostSayisi));
        var govde = JsonDocument.Parse(istekler.Single(i => i.Metot == "POST").Govde).RootElement;
        Assert.Equal(2m, govde.GetProperty("miktar").GetDecimal());
        Assert.Equal(1250.50m, govde.GetProperty("birimFiyat").GetDecimal());
        Assert.Equal("Q92 yüzük", govde.GetProperty("aciklama").GetString());
        Assert.Equal(JsonValueKind.Null, govde.GetProperty("urunId").ValueKind);

        // Toplam SUNUCUNUN değeri (9.999), yerel toplam (1.620 + 2.501) DEĞİL.
        e.WaitForAssertion(() => Assert.Equal(Bicim.Tl(9999m),
            e.Find("#fd-cekmece-kalem-genel-toplam").TextContent.Trim()));
        Assert.Equal(3, e.FindAll(".fd-cekmece-kalem").Count);
        Assert.Contains("Satır eklendi: 3. satır", e.Find("#fd-kalem-basari").TextContent);
        Assert.Contains(Bicim.Tl(9999m), e.Find("#fd-kalem-basari").TextContent);
        // Liste yeniden okunur (satırdaki toplam da tazelenir); form sıfırlanır.
        Assert.True(istekler.Count(i => i.Metot == "GET" && i.Yol == "/api/v1/faturalar") > listeOnce);
        Assert.Equal("", e.Find("#fd-kalem-yeni-aciklama").GetAttribute("value") ?? "");
        Assert.Empty(e.FindAll(".alan-hata"));
    }

    [Fact]
    public void Q92_bos_gonderimde_ALAN_ALTINDA_mesaj_ve_POST_GITMEZ()
    {
        var e = SatirlarlaCekmeceAc();
        e.Find("#fd-kalem-yeni-miktar").Input("");
        e.Find("#fd-kalem-yeni-ekle").Click();

        Assert.Equal(0, PostSayisi);
        Assert.NotNull(e.Find("#alan-hata-fd-kalem-yeni-miktar"));
        Assert.NotNull(e.Find("#alan-hata-fd-kalem-yeni-fiyat"));
        Assert.Contains("Açıklama yazın ya da ürün seçin", e.Find("#alan-hata-fd-kalem-yeni-aciklama").TextContent);

        // Sıfır miktar ve satır toplamı sıfır da istek göndermeden söylenir.
        e.Find("#fd-kalem-yeni-aciklama").Input("Q92 x");
        e.Find("#fd-kalem-yeni-miktar").Input("0");
        e.Find("#fd-kalem-yeni-fiyat").Input("100");
        e.Find("#fd-kalem-yeni-ekle").Click();
        Assert.Equal(0, PostSayisi);
        Assert.NotNull(e.Find("#alan-hata-fd-kalem-yeni-miktar"));

        e.Find("#fd-kalem-yeni-miktar").Input("1");
        e.Find("#fd-kalem-yeni-indirim").Input("100");
        e.Find("#fd-kalem-yeni-ekle").Click();
        Assert.Equal(0, PostSayisi);
        Assert.Contains("Satır toplamı sıfırdan büyük", e.Find("#alan-hata-fd-kalem-yeni-fiyat").TextContent);
    }

    [Fact]
    public void Q92_sunucu_400_ILGILI_ALANIN_ALTINA_yazilir_cekmece_acik_kalir()
    {
        var e = SatirlarlaCekmeceAc();
        postHata = "Miktar sıfırdan büyük olmalıdır";
        e.Find("#fd-kalem-yeni-aciklama").Input("Q92 x");
        e.Find("#fd-kalem-yeni-fiyat").Input("100");
        e.Find("#fd-kalem-yeni-ekle").Click();

        e.WaitForAssertion(() => Assert.Contains("Miktar sıfırdan büyük olmalıdır",
            e.Find("#alan-hata-fd-kalem-yeni-miktar").TextContent));
        Assert.NotNull(e.Find("#cekmece-fatura"));
        Assert.Equal(2, e.FindAll(".fd-cekmece-kalem").Count);
    }

    [Fact]
    public void Q92_cekmecede_satir_DUZELTILIR_PATCH_ve_toplam_cekmecede_yenilenir()
    {
        var e = SatirlarlaCekmeceAc();
        var cekmece = e.Find("#cekmece-fatura");
        cekmece.QuerySelector("#si-fatura-kalem-miktar-k92a-ac")!.Click();
        e.Find("#si-fatura-kalem-miktar-k92a").Input("3");
        e.Find("#si-fatura-kalem-miktar-k92a-kaydet").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "PATCH" && i.Yol == "/api/v1/faturalar/f92/kalemler/k92a"));
        var govde = JsonDocument.Parse(istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
        Assert.Equal(3m, govde.GetProperty("miktar").GetDecimal());
        Assert.Single(govde.EnumerateObject());
        e.WaitForAssertion(() => Assert.Equal(Bicim.Tl(3620m),
            e.Find("#fd-cekmece-kalem-genel-toplam").TextContent.Trim()));
        Assert.Contains("Fatura kalemi güncellendi", e.Find("#fd-kalem-basari").TextContent);
    }

    [Fact]
    public void Q92_satirsiz_taslakta_EKLEME_YOK_gerekce_ve_gercek_yol_yazili()
    {
        kalemKaynagi = "BASLIK";
        var e = CekmeceAc();

        var ipucu = e.Find("#degismez-ipucu-FaturaSatirsizTaslak");
        Assert.Contains("satır eklenemez", ipucu.TextContent);
        Assert.Contains("İptal Et", ipucu.TextContent);
        Assert.Empty(e.FindAll("#fd-kalem-yeni-ekle"));
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/kalemler", StringComparison.Ordinal));
    }

    [Fact]
    public void Q92_satir_okuma_hatasi_BOS_DURUMA_cevrilmez_hata_bandi_gorunur()
    {
        kalemOkumaHatasi = true;
        var e = CekmeceAc();

        e.WaitForAssertion(() => Assert.Contains("Satırlar okunamadı",
            e.Find("#fd-kalem-bolumu .bant-hata").TextContent));
        Assert.Empty(e.FindAll(".fd-cekmece-kalem"));
        Assert.Empty(e.FindAll("#fd-kalem-yeni-ekle"));
    }

    [Fact]
    public void Q92_kuyum_taslakta_urunsuz_satir_ALAN_ALTINDA_reddedilir_POST_GITMEZ()
    {
        kdvTuru = "KUYUM";
        var e = SatirlarlaCekmeceAc();
        e.Find("#fd-kalem-yeni-aciklama").Input("Q92 elle");
        e.Find("#fd-kalem-yeni-fiyat").Input("1000");
        e.Find("#fd-kalem-yeni-ekle").Click();

        Assert.Equal(0, PostSayisi);
        Assert.Contains("ürün seçin", e.Find("#alan-hata-fd-kalem-yeni-urun").TextContent);

        SeciciYardim.Sec(e, "fd-kalem-yeni-urun", "u1");
        e.Find("#fd-kalem-yeni-ekle").Click();
        e.WaitForAssertion(() => Assert.Equal(1, PostSayisi));
        var govde = JsonDocument.Parse(istekler.Single(i => i.Metot == "POST").Govde).RootElement;
        Assert.Equal("u1", govde.GetProperty("urunId").GetString());
    }

    [Fact]
    public void Q92_Ekle_dugmesi_kaydederken_KILITLI_ve_sebebi_yazili()
    {
        var e = SatirlarlaCekmeceAc();
        postBekle = new TaskCompletionSource();
        e.Find("#fd-kalem-yeni-aciklama").Input("Q92 x");
        e.Find("#fd-kalem-yeni-fiyat").Input("100");
        e.Find("#fd-kalem-yeni-ekle").Click();

        e.WaitForAssertion(() => Assert.True(e.Find("#fd-kalem-yeni-ekle").HasAttribute("disabled")));
        Assert.NotNull(e.Find("#fd-kalem-yeni-ekle-kilit"));

        postBekle.SetResult();
        e.WaitForAssertion(() => Assert.False(e.Find("#fd-kalem-yeni-ekle").HasAttribute("disabled")));
        Assert.Empty(e.FindAll("#fd-kalem-yeni-ekle-kilit"));
        Assert.Equal(1, PostSayisi);
    }

    [Fact]
    public void Q92_KESILMIS_faturada_Duzenle_YOK_ve_satirlar_KILITLI_sebep_yazili()
    {
        durum = "KESILDI";
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-goster-0");
        Assert.Empty(e.FindAll("#fk-duzenle-f92"));

        e.Find("#fk-goster-0").Click();
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll("tr.fd-kalem-satir").Count));
        Assert.Empty(e.FindAll("[id^=si-fatura-kalem-]"));
        Assert.Empty(e.FindAll("#fd-kalem-yeni-ekle"));
        Assert.NotNull(e.Find("#degismez-ipucu-FaturaKalemi"));
    }

    // ZO-BELGE Y4-D (hasım DÜŞÜK bulguları, 30.09): altın dışı kuyum satırında has bedeli
    // düşülmez — ekran bunu satırın üzerinde kısa cümleyle söyler. TEK KAYNAK sunucunun
    // kaleme yazdığı "maden" (ayardan türetilmez): ürünsüz/ayarsız hurda gümüş satırında
    // ve platin satırında da not VAR; altın satırında YOK; madeni bilinmeyen satırda
    // "madeni bilinmiyor — özel matrah uygulanmadı" sebebi yazılır.
    private static string Kuyum(string satir, string? ayar, string? maden) => satir
        .Replace("\"ayarId\":null", ayar is null ? "\"ayarId\":null" : $"\"ayarId\":\"{ayar}\"")
        .Replace("\"kdvTuru\":\"STANDART\"", "\"kdvTuru\":\"KUYUM\"")
        .Replace("\"satirToplam\"", maden is null ? "\"satirToplam\"" : $"\"maden\":\"{maden}\",\"satirToplam\"");

    [Fact]
    public void ZOBELGE_kuyum_taslakta_GUMUS_satirinda_ozel_matrah_yok_notu_altinda_yok()
    {
        kdvTuru = "KUYUM";
        satirlar.Clear();
        satirlar.Add(Kuyum(Satir("k92a", 1, "Burma Bilezik", 1m, 1000m, 120m, 1120m), "K22", "ALTIN"));
        satirlar.Add(Kuyum(Satir("k92b", 2, "Gümüş Kolye", 1m, 500m, null, 500m), "G925", "GUMUS"));
        // Ürünsüz/ayarsız hurda gümüş (HURDA_ALIS fişinden): ayar YOK, maden defterden.
        satirlar.Add(Kuyum(Satir("k92c", 3, "Hurda gümüş", 1m, 800m, null, 800m), null, "GUMUS"));
        satirlar.Add(Kuyum(Satir("k92d", 4, "Platin Yüzük", 1m, 900m, null, 900m), "PT950", "PLATIN"));
        // Ayarı ALTIN (K22) ama madeni BİLİNMİYOR: ayardan türetilmez → sebep notu.
        satirlar.Add(Kuyum(Satir("k92e", 5, "Eski satır", 1m, 700m, null, 700m), "K22", null));
        var e = CekmeceAc();
        e.WaitForAssertion(() => Assert.Equal(5, e.FindAll(".fd-cekmece-kalem").Count));

        var not = e.Find("#fd-cekmece-kalem-gumus-k92b").TextContent.Trim();
        Assert.Equal(Kuyumcu.Core.I18n.FaturaTaslakSozlugu.Tr.GumusOzelMatrahYok, not);
        Assert.Contains("Gümüş", not);
        Assert.Equal(not, e.Find("#fd-cekmece-kalem-gumus-k92c").TextContent.Trim());
        Assert.Equal(not, e.Find("#fd-cekmece-kalem-gumus-k92d").TextContent.Trim());
        Assert.Empty(e.FindAll("#fd-cekmece-kalem-gumus-k92a"));
        Assert.Empty(e.FindAll("#fd-cekmece-kalem-madenyok-k92a"));
        Assert.Empty(e.FindAll("#fd-cekmece-kalem-gumus-k92e"));
        Assert.Equal(Kuyumcu.Core.I18n.FaturaTaslakSozlugu.Tr.MadenBilinmiyorOzelMatrahYok,
            e.Find("#fd-cekmece-kalem-madenyok-k92e").TextContent.Trim());
        Assert.Empty(e.FindAll("#fd-cekmece-kalem-madenyok-k92c"));
    }
}
