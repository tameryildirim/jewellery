using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-67 — VİTRİN PANO KALEM KATALOĞU: GEREKÇENİN EKRANDA GÖSTERİLMESİ [bUnit]
//
// ⚠ Sahte API '{}' varsayılanıyla BIRAKILMADI: /pano/guncel ve /pano/ayarlar
//   SUNUCU SÖZLEŞMESİYLE (PanoGorunum / PanoAyarlariGorunum) birebir alan adı
//   ve tipinde yanıt verir; varsayılana-don POST'u da gerçek ayar görünümü döner.
//
// Sınananlar (spesifikasyon test listesi + DoD):
//   1. PanoKalemKaynakAciklamasiGorunur — kalemlerin kur kaynağından geldiği,
//      elle eklenmediği ve yeni kalem için yönlendirme ekranda yazılı
//   2. SatirdanVarsayilanaDonCalisir — satırdaki düğme → satır içi onay → POST
//   3. AtlamaSebebiBosDurumMetniVar — atlanan yoksa "atlanan kalem yok" basar
//   + düzenlenebilir sütunlar işaretli · varsayılandaki kalemde düğme yok, durum
//     yazılı · Vazgeç istek atmaz · "yeni kalem ekle" düğmesi YOK · her düğme
//     ya istek atar ya disabled
// ═════════════════════════════════════════════════════════════════════════════

public sealed class VitrinEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly SahteApi sahte = new();

    public VitrinEkranTestleri()
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
        public bool AtlananVar;
        public bool DonusHatasi;

        private string Pano => $$"""
        {"kaynak":"ALTINKAYNAK","zaman":"2026-09-24T08:00:00Z",
         "satirlar":[
           {"kod":"GRAM","etiket":"Gram Altın","alis":6800.00,"satis":6900.00,"birim":"GRAM"},
           {"kod":"CEYREK_YENI","etiket":"Yeni Çeyrek","alis":11500.00,"satis":11800.00,"birim":"ADET"}],
         "atlananlar":{{(AtlananVar ? """[{"kod":"HAMIT","ad":"Hamit Altın","sebep":"TERS_MAKAS"}]""" : "[]")}},
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

        private const string DonulmusAyarlar = """
        {"ayarlar":[
           {"kod":"GRAM","ad":"Gram Altın","etiket":null,"sira":0,"alisMarjTl":0,"satisMarjTl":0,
            "yuvarlamaAdimi":10,"aktif":true,"ozel":false},
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

            if (yol.Contains("/varsayilana-don", StringComparison.Ordinal) && DonusHatasi)
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"message":"Sunucu hatası: ayar silinemedi"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            var y = yol.Contains("/pano/guncel", StringComparison.Ordinal) ? Pano
                : yol.Contains("/varsayilana-don", StringComparison.Ordinal) ? DonulmusAyarlar
                : Ayarlar;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private static VitrinKalemMetinleri K => VitrinKalemSozlugu.Tr;

    private IRenderedComponent<Kuyumcu.Web.Pages.Vitrin> Ekran()
    {
        var e = Render<Kuyumcu.Web.Pages.Vitrin>();
        GocC2Ortak.AyarlariAc(e);   // GOC-C-2: ayar tablosu PRO'da da kapalı katlanırla gelir (hakem K4 /vitrin 375) — test önce açar; beklenen GEVŞETİLMEDİ.
        return e;
    }

    [Fact]
    public void PanoKalemKaynakAciklamasiGorunur()
    {
        var e = Ekran();
        var bant = e.Find("#vitrin-kalem-kaynak");

        // Kalemler kur kaynağından gelir, elle eklenmez/silinmez — metin ekranda.
        Assert.Contains(K.KaynakAciklama, bant.TextContent, StringComparison.Ordinal);
        Assert.Contains("kur kaynağı", bant.TextContent, StringComparison.Ordinal);
        Assert.Contains("elle eklenmez", bant.TextContent, StringComparison.Ordinal);

        // Yeni kalem talebi için yönlendirme: kur kaynağı ayarlarına bağlantı.
        Assert.Contains(K.YeniKalemYolu, e.Find("#vitrin-yeni-kalem-yolu").TextContent, StringComparison.Ordinal);
        Assert.Equal("/kur#kur-kaynak-kart", e.Find("#vitrin-kur-kaynagi").GetAttribute("href"));

        // Bant, yönetim tablosunun ÜSTÜNDEdir (listeyi okumadan önce görülür).
        var html = e.Markup;
        Assert.True(html.IndexOf("vitrin-kalem-kaynak", StringComparison.Ordinal)
                    < html.IndexOf("id=\"vitrin-ayarlar\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Duzenlenebilir_sutunlar_ACIKCA_isaretli()
    {
        var e = Ekran();
        // GOC-C-2: ayar tablosu ortak ızgarada; "değiştirilebilir" rozeti sütun başlığında değil, ızgaranın üstündeki TEK
        // satırda ve düzenlenebilir sütunların ADLARIYLA (P-67 "açıkça işaretlenir" aynen; başlık kimlikleri korunur).
        var lejant = e.Find("#vitrin-duzenlenebilir-sutunlar").TextContent;
        Assert.Contains(K.DuzenlenebilirRozet, lejant, StringComparison.Ordinal);
        foreach (var id in new[] { "#vitrin-baslik-alis-marj", "#vitrin-baslik-satis-marj", "#vitrin-baslik-yuvarlama" })
            Assert.Contains(e.Find(id).TextContent.Trim(), lejant, StringComparison.Ordinal);
        Assert.Contains(K.DuzenlenebilirNot, e.Find("#vitrin-duzenlenebilir").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void SatirdanVarsayilanaDonCalisir()
    {
        var e = Ekran();
        istekler.Clear();

        // 1. adım: satırdaki düğme ÇEKMECEYİ AÇMADAN satır içi onay ister; istek henüz GİTMEZ.
        e.Find("#vitrin-varsayilana-don-GRAM").Click();
        e.WaitForElement("#vitrin-varsayilana-don-evet-GRAM");
        Assert.Contains("Gram Altın", e.Find("#vitrin-varsayilana-don-soru-GRAM").TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#pano-varsayilana-don"));
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");

        // 2. adım: onay → doğru uca POST, başarı bandı, pano yeniden çekilir (UX-S23/S27).
        e.Find("#vitrin-varsayilana-don-evet-GRAM").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#vitrin-basari")));

        var post = istekler.Single(i => i.Metot == "POST");
        Assert.EndsWith("/pano/ayarlar/GRAM/varsayilana-don", post.Yol, StringComparison.Ordinal);
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("/pano/guncel", StringComparison.Ordinal));

        // Sunucu yanıtı OKUNDU: GRAM artık varsayılanda → satırda düğme yerine durum yazar.
        e.WaitForElement("#vitrin-varsayilan-GRAM");
        Assert.Empty(e.FindAll("#vitrin-varsayilana-don-GRAM"));
    }

    [Fact]
    public void Satir_onayinda_Vazgec_istek_ATMAZ_ve_dugmeyi_geri_getirir()
    {
        var e = Ekran();
        istekler.Clear();
        e.Find("#vitrin-varsayilana-don-GRAM").Click();
        e.WaitForElement("#vitrin-varsayilana-don-vazgec-GRAM").Click();

        e.WaitForElement("#vitrin-varsayilana-don-GRAM");
        Assert.Empty(e.FindAll("#vitrin-varsayilana-don-evet-GRAM"));
        Assert.Empty(istekler);
    }

    [Fact]
    public void Satirdan_donus_hatasi_SAYFA_bandinda_gorunur()
    {
        sahte.DonusHatasi = true;
        var e = Ekran();
        e.Find("#vitrin-varsayilana-don-GRAM").Click();
        e.WaitForElement("#vitrin-varsayilana-don-evet-GRAM").Click();

        // UX-S28: hata yutulmaz; çekmece kapalıyken sayfa bandına düşer.
        e.WaitForAssertion(() =>
            Assert.Contains("ayar silinemedi", e.Find("#vitrin-hata").TextContent, StringComparison.Ordinal));
    }

    [Fact]
    public void Varsayilandaki_kalemde_satirda_DUGME_yok_durum_YAZILI()
    {
        var e = Ekran();
        Assert.Empty(e.FindAll("#vitrin-varsayilana-don-CEYREK_YENI"));
        Assert.Equal(K.Varsayilan, e.Find("#vitrin-varsayilan-CEYREK_YENI").TextContent.Trim());
    }

    [Fact]
    public void AtlamaSebebiBosDurumMetniVar()
    {
        var e = Ekran();
        e.WaitForElement("#vitrin-pano");

        // Atlanan yok → boş tablo ya da kaybolan bölüm DEĞİL, anlamlı metin.
        Assert.Empty(e.FindAll("#vitrin-atlananlar"));
        Assert.Equal(K.AtlananYok, e.Find("#vitrin-atlanan-yok").TextContent.Trim());
    }

    [Fact]
    public void Atlanan_varsa_tablo_cizilir_bos_durum_metni_CIKMAZ()
    {
        sahte.AtlananVar = true;
        var e = Ekran();
        e.WaitForElement("#vitrin-atlanan-HAMIT");
        Assert.Empty(e.FindAll("#vitrin-atlanan-yok"));
    }

    [Fact]
    public void Yeni_kalem_ekle_izlenimi_veren_DUGME_yok_ve_her_dugme_istek_atar_ya_disabled()
    {
        var e = Ekran();
        foreach (var d in e.FindAll("button"))
        {
            var metin = d.TextContent.ToLowerInvariant();
            Assert.False(metin.Contains("ekle", StringComparison.Ordinal) || metin.Contains("yeni kalem", StringComparison.Ordinal) || metin.TrimStart().StartsWith('+'),
                $"#{d.Id}: 'yeni kalem ekle' izlenimi veren düğme: {d.TextContent.Trim()}");
            Assert.True(d.HasAttribute("disabled") || d.HasAttribute("blazor:onclick"),
                $"#{d.Id}: ne disabled ne @onclick taşıyor");
        }
    }

    [Fact]
    public void Ingilizce_metinler_de_DOLU()
    {
        foreach (var p in typeof(VitrinKalemMetinleri).GetProperties())
        {
            Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(VitrinKalemSozlugu.En)), $"En.{p.Name} boş");
            Assert.NotEqual(p.GetValue(VitrinKalemSozlugu.Tr), p.GetValue(VitrinKalemSozlugu.En));
        }
    }
}
