using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO — ekran testleri (bUnit). Sahte API'ler "{}" varsayılanına
// BIRAKILMADI: dokunulan her uca gerçekçi yanıt; gövdeler JSON ayrıştırılarak sınanır.
//   (1) Günü Kapat çekmecesi: kart/havale ayrı satır, sayılan nakit 1000 → fark 0
//   (1) Gün sonu mutabakatı: alt kalemler Nakit · Kart · Havale
//   (2) Tahsilat: "Borcu azaltır" yok; TL borçlu müşteride altın uyarısı
//   (3) Takas: satıcı kimliği yokken Kaydet kilitli; gövdede satıcı bloğu
//   (4) Peşin Müşteri + Veresiye: Kaydet önceden kilitli, sebep yazılı
//   (5) Cari detay: ödeme türü, borç etkisi, yürüyen bakiye
// ═════════════════════════════════════════════════════════════════════════════

internal static class ParaDemoSahte
{
    public static HttpResponseMessage Tamam(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
    };

    public static HttpResponseMessage Hata(int kod, string mesaj) => new((HttpStatusCode)kod)
    {
        Content = new StringContent(JsonSerializer.Serialize(new { message = mesaj }),
            System.Text.Encoding.UTF8, "application/json"),
    };
}

// ── (1) Kasalar → Günü Kapat ─────────────────────────────────────────────────
public sealed class ParaDemoKasaEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool onizlemeHata;

    public ParaDemoKasaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => onizlemeHata)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<bool> hata) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            var get = istek.Method == HttpMethod.Get;
            if (get && yol.Contains("/gun-kapanisi/onizleme") && hata())
                return ParaDemoSahte.Hata(500, "Önizleme okunamadı");
            return ParaDemoSahte.Tamam(yol switch
            {
                var v when v.Contains("/kur/guncel") => """{"id":"kur1","altinHasFiyati":4100}""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez Şube"}]""",
                // Demo kabul senaryosu: açılış 0 + nakit 1000 + kart 500 + havale 300 = bakiye 1800.
                var v when get && v.Contains("/gun-kapanisi/onizleme") =>
                    """{"kasaId":"k1","paraBirimi":"TRY","toplamBakiye":1800,"kartToplam":500,"havaleToplam":300,"nakitDefter":1000}""",
                var v when get && v.Contains("/kasalar/") && v.Contains("/hareketler") => """{"hareketler":[]}""",
                var v when get && v.EndsWith("/kasalar") =>
                    """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":1800,"aktif":true,"subeId":"s1"}]""",
                var v when v.EndsWith("/gun-kapanisi") =>
                    """{"kasaId":"k1","yerelGun":"2026-10-01","sayilanNakit":1000,"defterBakiye":1000,"toplamBakiye":1800,"kartToplam":500,"havaleToplam":300,"fark":0,"aciklama":null,"kilitli":true}""",
                var v when v.Contains("/kasalar/") => """{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":1800,"aktif":true,"subeId":"s1"}""",
                _ => "{}",
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Kasalar> Ciz()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(new OturumKullanici(
            "u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1", ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Pages.Kasalar>().AyrintiAc();
        e.WaitForElement("#kasa-gun-kapat-k1");
        return e;
    }

    [Fact]
    public void PDE1_kart_ve_havale_AYRI_satir_sayilacak_nakit_1000_sayilan_1000_fark_SIFIR()
    {
        var e = Ciz();
        e.Find("#kasa-gun-kapat-k1").Click();
        e.WaitForElement("#gun-nakit-defter");

        Assert.Contains("500", e.Find("#gun-kart").TextContent);
        Assert.Contains("300", e.Find("#gun-havale").TextContent);
        Assert.Contains("1.800", e.Find("#gun-toplam").TextContent);
        Assert.Contains("1.000", e.Find("#gun-nakit-defter").TextContent);
        Assert.Equal(ParaDemoSozlugu.Tr.NakitAyrimNotu, e.Find("#gun-nakit-not").TextContent.Trim());

        e.Find("#gun-sayilan").Input("1000");
        // Fark 0 → gerekçe alanı yok, düğme açık, kilit metni yok.
        Assert.Empty(e.FindAll("#gun-aciklama"));
        Assert.Empty(e.FindAll("#gun-kapat-kilit"));
        Assert.False(e.Find("#gun-kapat-onayla").HasAttribute("disabled"));

        e.Find("#gun-kapat-onayla").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/kasalar/k1/gun-kapanisi")));
        var g = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST").Govde).RootElement;
        Assert.Equal(1000m, g.GetProperty("sayilanNakit").GetDecimal());
    }

    [Fact]
    public void PDE1_toplam_bakiye_yazilirsa_FARK_var_gerekce_ister()
    {
        var e = Ciz();
        e.Find("#kasa-gun-kapat-k1").Click();
        e.WaitForElement("#gun-nakit-defter");
        e.Find("#gun-sayilan").Input("1800");   // kart + havale de sayılmış → +800 fazla
        Assert.True(e.Find("#gun-kapat-onayla").HasAttribute("disabled"));
        Assert.NotNull(e.Find("#gun-aciklama"));
    }

    [Fact]
    public void PDE1_onizleme_okunamazsa_HATA_bandi_ve_kapanis_KILITLI_sebebi_yazili()
    {
        onizlemeHata = true;
        var e = Ciz();
        e.Find("#kasa-gun-kapat-k1").Click();
        e.WaitForElement("#gun-onizleme-hata");
        Assert.Contains("Önizleme okunamadı", e.Find("#gun-onizleme-hata").TextContent);
        e.Find("#gun-sayilan").Input("1000");
        Assert.True(e.Find("#gun-kapat-onayla").HasAttribute("disabled"));
        Assert.Equal(ParaDemoSozlugu.Tr.OnizlemeKilit, e.Find("#gun-kapat-kilit").TextContent.Trim());
        Assert.Empty(e.FindAll("#gun-nakit-defter"));   // "0 TL nakit" uydurulmaz (UX-S28)
    }
}

// ── (1) Kasa raporu → gün sonu sayım çekmecesi ───────────────────────────────
public sealed class ParaDemoKasaRaporuEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public ParaDemoKasaRaporuEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Kuyumcu.Web.Bilesenler.KayitSecici.BeklemeMs = 0;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            var bugun = DateTime.Today.ToString("yyyy-MM-dd");
            return ParaDemoSahte.Tamam(yol switch
            {
                var v when v.Contains("/gun-kapanisi/onizleme") =>
                    """{"kasaId":"ks1","paraBirimi":"TRY","toplamBakiye":1800,"kartToplam":500,"havaleToplam":300,"nakitDefter":1000}""",
                var v when v.StartsWith("/api/v1/kasalar", StringComparison.Ordinal) && istek.Method == HttpMethod.Post =>
                    """{"kasaId":"ks1","yerelGun":"2026-10-01","sayilanNakit":1000,"defterBakiye":1000,"fark":0,"aciklama":null,"kilitli":true}""",
                var v when v.StartsWith("/api/v1/kasalar", StringComparison.Ordinal) => """[{"id":"ks1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/raporlar/kasa-defteri") => $$"""
                    {"baslangic":"{{bugun}}","bitis":"{{bugun}}","saatDilimi":"Europe/Istanbul","utcOfsetSaat":3,"yazSaatiRiski":false,
                     "toplamSatir":1,"kasalar":[{"kasaId":"ks1","kasaAd":"Ana Kasa","paraBirimi":"TRY","acilisDeviri":0,
                      "kapanisBakiye":1800,"guncelBakiye":1800,"gunler":[{"tarih":"{{bugun}}","devir":0,"giris":1800,"cikis":0,
                      "hareketSayisi":3,"kapanis":1800,"durum":"ACIK","sayilanNakit":null,"kapanisDefterBakiye":null,"fark":null}]}]}
                    """,
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                var v when v.Contains("/kullanicilar") => "[]",
                _ => "{}",
            });
        }
    }

    private bool YildizVar(IRenderedComponent<Kuyumcu.Web.Pages.KasaRaporu> c) =>
        c.FindAll("label[for='kr-sayim-aciklama'] .zorunlu-isaret").Count == 1;

    [Fact]
    public void PDE1_kasa_raporu_sayimi_NAKIT_defteriyle_karsilastirir_kart_havale_ayri_yazar()
    {
        var c = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        c.WaitForElement("#kr-sayim-ks1");
        c.Find("#kr-sayim-ks1").Click();
        c.WaitForElement("#kr-sayim-nakit-disi");
        Assert.Contains("500", c.Find("#kr-sayim-kart").TextContent);
        Assert.Contains("300", c.Find("#kr-sayim-havale").TextContent);
        Assert.Contains(ParaDemoSozlugu.Tr.SayilacakNakit, c.Find("#kr-sayim-defter").TextContent);

        c.Find("#kr-sayim-tutar").Input("1000");           // nakit = 1000 → fark yok, gerekçe zorunlu DEĞİL
        c.WaitForAssertion(() => Assert.False(YildizVar(c)));
        c.Find("#kr-sayim-tutar").Input("1800");           // toplam bakiye yazıldı → +800 fark, gerekçe zorunlu
        c.WaitForAssertion(() => Assert.True(YildizVar(c)));
    }
}

// ── (1) Raporlar → Gün sonu mutabakatı: alt kalemler ─────────────────────────
public sealed class ParaDemoRaporEkranTestleri : BunitContext
{
    public ParaDemoRaporEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            return Task.FromResult(ParaDemoSahte.Tamam(yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/aylik") => """{"yil":2026,"ay":10,"gunler":[]}""",
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") => """
                    {"tarih":"2026-10-01",
                     "tahsilat":{"nakit":1000.00,"kart":500.00,"havale":300.00,"diger":0,"toplam":1800.00},
                     "satis":{"sayisi":3,"toplamTl":1800.00,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":3,"faturasizTl":1800.00},
                     "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
                     "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
                     "fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},
                     "fark":1800.00,"mutabik":false,"uyarilar":[],"faturasizIslemler":[],"faturaEslesme":null}
                    """,
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                _ => "{}",
            }));
        }
    }

    [Fact]
    public void PDE1_mutabakat_alt_kalemleri_HAVALE_dahil_toplamla_tutar()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForElement("#rp-sekme-gunsonu");
        e.Find("#rp-sekme-gunsonu").Click();
        e.WaitForAssertion(() => Assert.Contains("Havale", e.Find("#rp-panel-gunsonu").TextContent));
        var metin = e.Find("#rp-panel-gunsonu").TextContent;
        // DEGISIM-GUNSONU: aynı kalemler, her kalem bölünmez parça (iç boşluklar U+00A0) — kalem sınırında kırılır.
        var t5 = Toplu5Sozlugu.Tr;
        // PAKET RAPOR-4 (tasarım DÜŞÜK): PRO kartında kalemler satır sonunda "·" ile sarkıyordu → her kalem AYRI satır.
        Assert.Contains(string.Join("\n", new[] { string.Format(t5.KirilimNakit, Bicim.TlKurus(1000m)), string.Format(t5.KirilimKart, Bicim.TlKurus(500m)), string.Format(t5.KirilimHavale, Bicim.TlKurus(300m)) }
            .Select(k => k.Replace(' ', '\u00A0'))), metin);
        Assert.DoesNotContain("Diğer", metin);   // sıfır "Diğer" yazılmaz (zorunlu önce)
    }
}

// ── (2) Cariler → Tahsilat · (5) Cari detay ekstresi ─────────────────────────
public sealed class ParaDemoCariEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string CariListesi = """
        [{"id":"c1","ad":"Kemal Ardıç","tur":"MUSTERI","telefon":null,"email":null,
          "tlBakiye":16415.01,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false,"aktif":true},
         {"id":"c2","ad":"Derya Yıldızlı","tur":"MUSTERI","telefon":null,"email":null,
          "tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false,"aktif":true}]
        """;

    // En yeniden eskiye: kart tahsilat −500 · veresiye satış +2.000 · nakit satış (borca yazılmaz).
    private const string Ekstre = """
        {"bakiye":{"cariId":"c1","tlBakiye":1500,"hasGramBakiye":0},
         "hareketler":[
           {"id":"h3","tur":"TAHSILAT","tlTutari":-500,"hasGrami":0,"aciklama":null,"olusturuldu":"2026-10-01T10:03:00Z","odemeTuru":"KART","cariTlEtkisi":-500,"cariHasEtkisi":0,"bakiyeHasEtkisi":0,"hasBakiyesineGirer":true},
           {"id":"h2","tur":"SATIS","tlTutari":2000,"hasGrami":-1.5,"aciklama":null,"olusturuldu":"2026-10-01T10:02:00Z","odemeTuru":"VERESIYE","cariTlEtkisi":2000,"cariHasEtkisi":0,"bakiyeHasEtkisi":0},
           {"id":"h1","tur":"SATIS","tlTutari":1000,"hasGrami":-0.7,"aciklama":null,"olusturuldu":"2026-10-01T10:01:00Z","odemeTuru":"NAKIT","cariTlEtkisi":0,"cariHasEtkisi":0,"bakiyeHasEtkisi":0}],
         "sayfaDisiHasEtkisi":0,"dahaFazlaVar":false,"limit":100,"bakiyeUyusmazligiHas":0,
         "bakiyeEmanetsizHas":0,"emanetteHas":0,"bakiyeUyusmazligiTl":0}
        """;

    public ParaDemoCariEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            return ParaDemoSahte.Tamam(yol switch
            {
                var y when y.EndsWith("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"gbpTry":null}""",
                var y when y.Contains("/ekstre") => Ekstre,
                var y when y.EndsWith("/tahsilat") => """{"id":"c1","ad":"Kemal Ardıç","tur":"MUSTERI","bakiye":{"cariId":"c1","tlBakiye":16415.01,"hasGramBakiye":-1}}""",
                var y when y.EndsWith("/cari/c1") => """{"id":"c1","ad":"Kemal Ardıç","tur":"MUSTERI","telefon":null,"email":null,"vergiNo":null,"adres":null,"pepBeyani":false}""",
                var y when y.EndsWith("/cari") => CariListesi,
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true}]""",
                var y when y.Contains("/tevsik/gunluk") => """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/listeler") => "[]",
                _ => "{}",
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Cariler> Ciz()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        // TEMA TEMİZLİĞİ 17.10: Yıldız Müşteriler listesi — eylemler müşterinin kartında (CariKartEylemi).
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("#yildiz-musteri-c1");
        istekler.Clear();
        return e;
    }

    [Fact]
    public void PDE2_tahsilat_ipucu_BORCU_AZALTIR_demez_altin_ayri_hesaba()
    {
        var e = Ciz();
        e.CariKartEylemi("c1", "c-tahsilat");
        e.WaitForElement("#th-ipucu");
        Assert.Equal(ParaDemoSozlugu.Tr.TahsilatIpucu, e.Find("#th-ipucu").TextContent.Trim());
        Assert.DoesNotContain("Borcu azaltır", e.Markup);
        Assert.Empty(e.FindAll("#th-has-uyari"));   // altın girilmeden uyarı yok (zorunlu önce)
    }

    [Fact]
    public void PDE2_TL_borclu_musteride_altin_girilince_UYARI_ve_dogru_yol_yazili()
    {
        var e = Ciz();
        e.CariKartEylemi("c1", "c-tahsilat");
        // PARA-DEMO-2 (madde 7): altınla ödeme isteğe bağlı — "+ Altınla öde" arkasında.
        e.WaitForElement("#th-has-ekle").Click();
        e.WaitForElement("#th-has");
        e.Find("#th-has").Input("1");
        var uyari = e.WaitForElement("#th-has-uyari").TextContent;
        // PARA-DEMO-2 (madde 6): büyük harfle bağırma yok; ekrandaki gerçek tür adı + oraya giden bağlantı.
        Assert.DoesNotContain("DÜŞÜRMEZ", uyari);
        Assert.Contains("düşürmez", uyari);
        Assert.Contains("Hurda Alış", uyari);
        Assert.Equal("/islemler?tur=HURDA_ALIS", e.Find("#th-has-yol").GetAttribute("href"));
        Assert.Contains(Bicim.Para(16415.01m, "TRY"), uyari);

        // Kaydet yine çalışır (davranış aynı: altın hesabına yazılır) ve gövdede TL YOK.
        e.Find("#th-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/cari/c1/tahsilat")));
        var g = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST").Govde).RootElement;
        Assert.Equal(1m, g.GetProperty("hasGrami").GetDecimal());
        Assert.Equal(JsonValueKind.Null, g.GetProperty("tlTutari").ValueKind);
    }

    [Fact]
    public void PDE2_borcsuz_musteride_altin_uyarisi_CIKMAZ()
    {
        var e = Ciz();
        e.CariKartEylemi("c2", "c-tahsilat");
        e.WaitForElement("#th-has-ekle").Click();   // PARA-DEMO-2 (madde 7)
        e.WaitForElement("#th-has");
        e.Find("#th-has").Input("1");
        Assert.Empty(e.FindAll("#th-has-uyari"));
    }

    [Fact]
    public void PDE5_detayda_odeme_turu_borc_etkisi_ve_YURUYEN_bakiye()
    {
        var e = Ciz();
        e.CariKartEylemi("c1", "c-detay");
        e.WaitForElement("#cari-detay-hareketler");

        Assert.Equal("· Kart", e.Find("#cari-detay-odeme-h3").TextContent.Trim());
        Assert.Equal("· Veresiye", e.Find("#cari-detay-odeme-h2").TextContent.Trim());
        Assert.Equal("· Nakit", e.Find("#cari-detay-odeme-h1").TextContent.Trim());

        // Satır tutarı = borç etkisi; peşin satış "ödendi, borca yazılmadı".
        Assert.Equal(Bicim.Tl(-500m), e.Find("#cari-detay-tl-h3").TextContent.Trim());
        Assert.Equal(Bicim.Tl(2000m), e.Find("#cari-detay-tl-h2").TextContent.Trim());
        Assert.Equal(string.Format(ParaDemoSozlugu.Tr.EkstrePesinOdendi, Bicim.Tl(1000m)),
            e.Find("#cari-detay-tl-h1").TextContent.Trim());

        // Yürüyen bakiye: en yeni = kayıtlı bakiye; önceki = sonraki − satır.
        Assert.Equal(string.Format(ParaDemoSozlugu.Tr.EkstreBakiyeSonra, Bicim.Tl(1500m)), e.Find("#cari-detay-bakiye-h3").TextContent.Trim());
        Assert.Equal(string.Format(ParaDemoSozlugu.Tr.EkstreBakiyeSonra, Bicim.Tl(2000m)), e.Find("#cari-detay-bakiye-h2").TextContent.Trim());
        Assert.Equal(string.Format(ParaDemoSozlugu.Tr.EkstreBakiyeSonra, Bicim.Tl(0m)), e.Find("#cari-detay-bakiye-h1").TextContent.Trim());
    }
}

// ── (3) Takas satıcı kimliği · (4) Peşin Müşteri + Veresiye ──────────────────
public sealed class ParaDemoIslemEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string CariListesi = """
        [{"id":"c1","ad":"Tuna Akarsu","tur":"MUSTERI","telefon":null,"email":null,
          "tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false,"sistemCarisi":false,"aktif":true},
         {"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","telefon":null,"email":null,
          "tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false,"sistemCarisi":true,"aktif":true}]
        """;

    public ParaDemoIslemEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            return ParaDemoSahte.Tamam(yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.EndsWith("/cari") => CariListesi,
                var v when v.Contains("/tevsik/gunluk") => """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Yarım Altın","ayarId":"K22","hasGrami":3.206,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/islemler/altin-bacagi/ayarlar") => """{"stopajOranlari":[{"saticiTipi":"NIHAI_TUKETICI","oran":0,"pusulaGerekir":true},{"saticiTipi":"ESNAF_MUAF","oran":0.02,"pusulaGerekir":true},{"saticiTipi":"MUKELLEF","oran":0,"pusulaGerekir":false}],"alisIskontoOran":null,"saticiAdresiZorunlu":true}""",
                var v when v.Contains("/islemler/takas") => """{"eskiHasGrami":2.748,"yeniHasGrami":3.206,"eskiDegerTl":8244,"yeniDegerTl":10000,"farkTl":1756,"saticiTipi":"NIHAI_TUKETICI","giderPusulasi":{"id":"gp1","pusulaNo":"GP-2026-000001","brutTutar":8244,"netOdenen":8244,"duzenlemeSonTarihi":"2026-10-08T00:00:00Z"}}""",
                var v when v.Contains("/islemler/satis") => """{"id":"h1","fisId":"f1"}""",
                var v when v.Contains("/reyonlar") || v.Contains("/islemler/son") || v.Contains("/pos/cihazlar") || v.Contains("/islemler/personeller") => "[]",
                _ => "{}",
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Pos()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Sahip", "t@ornek.com", "SAHIP", "s1", null, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#islem-tur-SATIS");
        return e;
    }

    // TEMA TEMİZLİĞİ 17.10: SATIŞ fişi HER ZAMAN Yıldız tezgâhıdır — ödeme türü tek ödeme satırının açtığı düzende
    // (#islem-odeme, FisOdemeKasa — aynı id), Kaydet #yildiz-tezgah-kaydet, kilit sebebi düğmenin title'ı + not satırı
    // (#yildiz-tezgah-not), müşteri "Değiştir" katmanında (#yildiz-tezgah-degistir → #yildiz-musteri-sec-{id}).
    private static void TezgahKalemOdeme(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string odeme)
    {
        e.WaitForElement("#yildiz-tezgah", TimeSpan.FromSeconds(10));
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll(".yz-tezgah-kalem")), TimeSpan.FromSeconds(5));
        if (e.FindAll("#yildiz-tezgah-odeme-duzen").Count == 0) e.Find("#yildiz-odeme-0").Click();
        e.WaitForElement("#islem-odeme");
        SeciciYardim.Sec(e, "islem-odeme", odeme);
    }

    [Fact]
    public void PDE4_Pesin_Musteri_VERESIYE_secilince_Kaydet_ONCEDEN_kilitli_sebebi_yazili()
    {
        var e = Pos();
        TezgahKalemOdeme(e, "VERESIYE");

        e.WaitForAssertion(() => Assert.True(e.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled")));
        Assert.Equal(ParaDemoSozlugu.Tr.PesinVeresiyeKilit, e.Find("#yildiz-tezgah-kaydet").GetAttribute("title"));
        Assert.Equal(ParaDemoSozlugu.Tr.PesinVeresiyeKilit, e.Find("#yildiz-tezgah-not").TextContent.Trim());
        e.Find("#yildiz-tezgah-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis"));

        // Geri dönüş yolu: gerçek müşteri seçilince kilit kalkar.
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForElement("#yildiz-musteri-sec-c1").Click();
        e.WaitForAssertion(() => Assert.False(e.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled")));
    }

    [Fact]
    public void PDE4_Pesin_Musteri_NAKITte_kilit_YOK()
    {
        var e = Pos();
        TezgahKalemOdeme(e, "NAKIT");
        e.WaitForAssertion(() => Assert.False(e.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled")));
        Assert.DoesNotContain(ParaDemoSozlugu.Tr.PesinVeresiyeKilit, e.Find("#yildiz-tezgah").TextContent, StringComparison.Ordinal);
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> TakasDolu()
    {
        var e = Pos();
        e.Find("#islem-tur-TAKAS").Click();
        e.WaitForElement("#takas-satici");
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.Find("#takas-eski-gram").Input("3");
        SeciciYardim.Sec(e, "takas-yeni-urun", "u1");
        e.Find("#takas-yeni-fiyat").Input("10000");
        return e;
    }

    [Fact]
    public void PDE3_takas_satici_kimligi_yokken_Kaydet_KILITLI_sebebi_yazili()
    {
        var e = TakasDolu();
        e.WaitForAssertion(() => Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled")));
        // PARA-DEMO-2 (madde 8): sebep EKSİK ALANI adıyla söyler (PRO: sade dil + VUK md.234).
        Assert.Equal(string.Format(ParaDemo2Sozlugu.Tr.TakasEksikPro, ParaDemo2Sozlugu.Tr.AlanSaticiTuru),
            e.Find("#islem-kaydet-kilit").TextContent.Trim());
        Assert.Equal(ParaDemoSozlugu.Tr.TakasKimlikNotu, e.Find("#takas-satici-not").TextContent.Trim());
    }

    [Fact]
    public void PDE3_takas_esnaf_muaf_KILITLI_ayri_islem_yolu_yazili()
    {
        var e = TakasDolu();
        SeciciYardim.Sec(e, "takas-satici-tip", "ESNAF_MUAF");
        e.WaitForElement("#takas-satici-esnaf-kilit");
        Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled"));
        // PARA-DEMO-2 (madde 6): esnaf kilidi ekrandaki GERÇEK tür adlarıyla.
        Assert.Equal(string.Format(ParaDemo2Sozlugu.Tr.TakasEsnafKilit, Kuyumcu.Core.I18n.Sozlukler.Tr.Islem.HurdaAlis,
            Kuyumcu.Core.I18n.Sozlukler.Tr.Islem.Satis), e.Find("#islem-kaydet-kilit").TextContent.Trim());
    }

    [Fact]
    public void PDE3_takas_vatandas_kimligi_GOVDEDE_ve_pusula_no_basari_bandinda()
    {
        var e = TakasDolu();
        SeciciYardim.Sec(e, "takas-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#takas-satici-ad");
        e.Find("#takas-satici-ad").Input("Tuna Akarsu");
        e.Find("#takas-satici-tckn").Input("10000000146");
        e.Find("#takas-satici-adres").Input("Çarşı Mah. 1");
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled")));

        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/takas")));
        var g = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST" && i.Yol.Contains("/islemler/takas")).Govde).RootElement;
        var s = g.GetProperty("satici");
        Assert.Equal("NIHAI_TUKETICI", s.GetProperty("saticiTipi").GetString());
        Assert.Equal("10000000146", s.GetProperty("tckn").GetString());
        Assert.Equal("Çarşı Mah. 1", s.GetProperty("adres").GetString());
        e.WaitForAssertion(() => Assert.Contains("GP-2026-000001", e.Markup));
    }
}

/// <summary>
/// PAKET PARA-DEMO (madde 3) — takas ekranı artık satıcı kimliği olmadan Kaydet'i açmaz.
/// Takası başka bir kural için dolduran mevcut ekran testleri bu yardımcıyla MÜKELLEF satıcı
/// (pusula yok, yalnız fatura no) seçer; sınadıkları kural (fiyat, ödeme türü, POS) değişmez.
/// </summary>
public static class ParaDemoEkranYardim
{
    public static void TakasSaticiDoldur<T>(IRenderedComponent<T> e) where T : Microsoft.AspNetCore.Components.IComponent
    {
        SeciciYardim.Sec(e, "takas-satici-tip", "MUKELLEF");
        e.WaitForElement("#takas-satici-fatura");
        e.Find("#takas-satici-fatura").Input("TEST-TAKAS-1");
    }
}
