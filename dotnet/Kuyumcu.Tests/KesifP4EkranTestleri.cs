using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P4 · ÜRÜN — keşif turu 30.09 bulguları:
//   #14 Şube Stokları > reyon yerleşimi: Adet kutusu Reyon kutusundan 23 px aşağıda
//   #15/#16/#38 (TOPLU KATMAN-1b) Düzenle çekmecesinde yarım sütun ezilmesi
//   #37 parite seçeneğinde ham kod · #40 açıklama etiketi = sayfa açıklaması
//   #79/#84/#85 ham ayar kodu (K22) · #84 maden kilidi ipucu iki kez
//   KIYAS URUN-SADE: satır eylemlerinde emoji, her satırda "Görsel yok" metni
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KesifP4UrunlerTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string Liste = """
        [{"id":"u1","tip":"ZIYNET","kod":"BLZ-01","ad":"Burma Bilezik","ayarId":"K22",
          "gramAgirlik":15.5,"tasAgirlik":0,"hasGrami":14.19,"barkod":"869001","aktif":true},
         {"id":"u2","tip":"GUMUS","kod":"GMS-02","ad":"Gümüş Kolye","ayarId":"G925",
          "gramAgirlik":8,"tasAgirlik":0,"hasGrami":7.4,"barkod":null,"aktif":true}]
        """;

    private const string TamKart = """
        {"id":"u1","tip":"ZIYNET","kod":"BLZ-01","ad":"Burma Bilezik","ayarId":"K22",
         "gramAgirlik":15.5,"tasAgirlik":0,"iscilikTl":0,"barkod":"869001",
         "kategoriId":null,"reyonId":null,"satisFiyati":null,"aciklama":null,
         "pariteKodu":"CEYREK_YENI","tasKokeni":"TAS_YOK"}
        """;

    public KesifP4UrunlerTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, yol, govde));
            var g = yol switch
            {
                var y when y.Contains("/urunler/stok-adetleri") => """
                    {"urunler":[{"urunId":"u1","toplamAdet":3,"toplamHasGram":40},
                                {"urunId":"u2","toplamAdet":0,"toplamHasGram":0}]}
                    """,
                var y when y.EndsWith("/urunler/u1/stok") => """
                    {"urunId":"u1","toplamAdet":3,"toplamHasGram":40,
                     "subeStoklar":[{"subeId":"s1","adet":3.000,"toplamHasGram":40,"guncellendi":"2026-09-30T00:00:00"}]}
                    """,
                // GERÇEK şekil (UrunYerlesimYaniti): s1'de 3 adet, 1'i Ön Vitrin'de, 2 atanmamış.
                var y when y.EndsWith("/urunler/u1/yerlesim") => """
                    {"urunId":"u1","kod":"BLZ-01","ad":"Burma Bilezik","birim":"ADET","hasGrami":14.19,"toplamStok":3,
                     "subeler":[{"subeId":"s1","subeAdi":"Merkez","stokAdedi":3,"yerlesikAdet":1,"atanmamis":2,
                                 "reyonlar":[{"reyonId":"r1","reyonAdi":"Ön Vitrin","tip":"VITRIN","aktif":true,"adet":1,"toplamHasGram":13.4}]}]}
                    """,
                var y when y.EndsWith("/etiket") => """
                    {"urunId":"u1","kod":"BLZ-01","ad":"Burma Bilezik","ibare":"","ibareZorunlu":false,
                     "etiketMetni":"Burma Bilezik","barkod":"869001","ayarId":"K22","milyem":916,
                     "gramAgirlik":15.5,"hasGrami":14.19,"dayanak":"Kuyum Ticareti Yönetmeliği md.11/(e)"}
                    """,
                var y when y.EndsWith("/urunler/madenler") => """
                    {"madenler":[{"maden":"ALTIN","madenAdi":"Altın","rodyumKaplanabilir":true,
                      "ayarlar":[{"id":"K22","ad":"22 Ayar","milyem":916,"kiymetli":true,"hasHesaplanir":true}]}],
                     "etiket":{"rodyumIbaresiGoster":false,"rodyumIbaresi":"rodyum kaplama"}}
                    """,
                var y when y.Contains("/urun-kategorileri") => "[]",
                var y when y.Contains("/reyonlar") => """
                    [{"id":"r1","subeId":"s1","ad":"Ön Vitrin","tip":"VITRIN","sorumluKullaniciId":null,"aktif":true,"urunSayisi":3,"yerlesikAdet":1}]
                    """,
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                var y when y.Contains("/cari") => """
                    [{"id":"c1","ad":"Ayşe Demir"},
                     {"id":"c2","ad":"Anadolu Kapalıçarşı Kuyumculuk Sanayi ve Ticaret Anonim Şirketi"}]
                    """,
                // KESIF-P4 tur 2: barkod ucu — yalnız 869001 bulunur, gerisi 404.
                var y when y.EndsWith("/urunler/barkod/869001") => TamKart,
                var y when y.Contains("/urunler/barkod/") => "__404__",
                var y when y.Contains("/urunler?") && y.Contains("q=zzz") => "[]",
                var y when y.EndsWith("/urunler/u1") => TamKart,
                var y when y.Contains("/belgeler") => "[]",
                // GERÇEK şekil (KurModulu.Pariteler): kod + AD. Ad ekranda, kod gövdede.
                var y when y.Contains("/kur/pariteler") => """
                    {"kaynak":"TEST","pariteler":[
                      {"kod":"CEYREK_YENI","ad":"Çeyrek (yeni)","alis":10000,"satis":10300,"birim":"ADET"},
                      {"kod":"BILEZIK_22","ad":"22 Ayar Bilezik","alis":6800,"satis":6900,"birim":"GRAM"}]}
                    """,
                var y when y.Contains("/urunler") => istek.Method == HttpMethod.Post ? """{"id":"u9"}""" : Liste,
                _ => "{}",
            };
            if (g == "__404__")
                return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
                {
                    Content = new StringContent("""{"message":"bulunamadı"}""", System.Text.Encoding.UTF8, "application/json"),
                };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Urunler> Ciz(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("k1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Kuyumcu.Web.Pages.Urunler>().AyrintiAc();
        e.WaitForAssertion(() => Assert.DoesNotContain("…", e.Find("#u-stok-adet-u1").TextContent));
        lock (istekler) istekler.Clear();
        return e;
    }

    private List<(string Metot, string Yol, string Govde)> Istekler()
    {
        lock (istekler) return istekler.ToList();
    }

    private static string? Alan(string govde, string ad)
    {
        var k = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return k.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    private static void DuzenleAc(IRenderedComponent<Kuyumcu.Web.Pages.Urunler> e)
    {
        e.Find("#u-duzenle-u1").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#d-ad")));
    }

    // ── #79 / #84 — ham ayar kodu 0; milyem yalnız PRO; gövdede KOD ─────────

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Urunler_liste_ve_secici_ayar_adi(string mod)
    {
        var e = Ciz(mod);
        Assert.Equal("22 Ayar", e.Find("#u-ayar-u1").TextContent.Trim());
        Assert.Equal("925 Gümüş", e.Find("#u-ayar-u2").TextContent.Trim());
        foreach (var hucre in e.FindAll("[id^=u-ayar-]"))
            Assert.DoesNotMatch(@"\b(K\d{2}|G\d{3})\b", hucre.TextContent);

        e.Find("#urun-yeni-ac").Click();
        var secenekler = SeciciYardim.Secenekler(e, "u-ayar");
        Assert.NotEmpty(secenekler);
        // P-URUN (K6): ilk seçenek boş "Ayar seçin" (değeri "") — varsayılan yok; kod/ad kuralı dolu seçeneklerde.
        foreach (var s in secenekler.Where(s => s.Deger() != ""))
        {
            // Seçenek DEĞERİ kod (sözleşme), GÖRÜNEN metin ad.
            Assert.DoesNotContain(s.Deger(), s.Ad());
            Assert.DoesNotContain(" — ", s.Ad());
        }
        var k22 = secenekler.Single(s => s.Deger() == "K22").Ad();
        if (mod == ArayuzModuServisi.Profesyonel)
            Assert.Equal("22 Ayar (916‰)", k22);
        else
            Assert.Equal("22 Ayar", k22);

        e.Find("#u-ad").Input("P4 Ürün");
        e.Find("#u-kod").Input("P4-1");
        e.Find("#u-gram").Input("5");
        SeciciYardim.Sec(e, "u-ayar", "K18");
        e.Find("#u-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "POST" && i.Yol.EndsWith("/urunler")));
        var post = Istekler().Single(i => i.Metot == "POST" && i.Yol.EndsWith("/urunler"));
        Assert.Equal("K18", Alan(post.Govde, "ayarId"));
    }

    [Fact]
    public void Urunler_ayar_suzgeci_ad_gosterir_sorguda_kod_gider()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        var s = SeciciYardim.Secenekler(e, "urun-ayar-filtre").Single(o => o.Deger() == "K22");
        Assert.Equal("22 Ayar", s.Ad());
        SeciciYardim.Sec(e, "urun-ayar-filtre", "K22");
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "GET" && i.Yol.Contains("ayarId=K22")));
    }

    [Fact]
    public void Urunler_etiket_penceresi_ayar_adi()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        e.Find("#u-etiket-u1").Click();
        e.WaitForAssertion(() => Assert.Equal("22 Ayar", e.Find("#u-etiket-ayar").TextContent.Trim()));
    }

    // ── #37 — parite adı görünür, gövdede kod ────────────────────────────────

    [Fact]
    public void Urunler_parite_adi_gorunur_govdede_kod()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        e.Find("#urun-yeni-ac").Click();
        e.Find("#u-daha-fazla").Click();   // PAKET GOC-C (devir "zorunlu önce"): yeni ürün formu PRO'da da yalnız zorunlularla açılır — isteğe bağlılar "Daha fazla"da
        e.WaitForAssertion(() => Assert.Contains("Çeyrek (yeni)", SeciciYardim.ListeMetni(e, "u-parite")));
        var liste = SeciciYardim.ListeMetni(e, "u-parite");
        Assert.DoesNotContain("CEYREK_YENI", liste);
        Assert.DoesNotContain("BILEZIK_22", liste);

        e.Find("#u-ad").Input("P4 Çeyrek");
        e.Find("#u-kod").Input("P4-2");
        e.Find("#u-gram").Input("1,75");
        SeciciYardim.Sec(e, "u-ayar", "K18");   // P-URUN (K6): ayarın varsayılanı yok — kullanıcı seçer
        SeciciYardim.Sec(e, "u-parite", "CEYREK_YENI");
        e.Find("#u-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "POST" && i.Yol.EndsWith("/urunler")));
        var post = Istekler().Single(i => i.Metot == "POST" && i.Yol.EndsWith("/urunler"));
        Assert.Equal("CEYREK_YENI", Alan(post.Govde, "pariteKodu"));
    }

    [Fact]
    public void Urunler_duzenle_parite_mevcut_kod_adla_secili()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        DuzenleAc(e);
        e.WaitForAssertion(() => Assert.Equal("CEYREK_YENI", SeciciYardim.SeciliDeger(e, "d-parite")));
        Assert.Contains("Çeyrek (yeni)", e.Find("#d-parite").TextContent);
        Assert.DoesNotContain("CEYREK_YENI", e.Find("#d-parite").TextContent);
    }

    // ── #40 — açıklama alanının etiketi alanın adı ───────────────────────────

    [Fact]
    public void Urunler_aciklama_etiketi_sayfa_aciklamasi_degil()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        var sayfaAciklamasi = Sozlukler.Tr.Urun.Aciklama;

        e.Find("#urun-yeni-ac").Click();
        e.Find("#u-daha-fazla").Click();   // PAKET GOC-C (devir "zorunlu önce"): yeni ürün formu PRO'da da yalnız zorunlularla açılır — isteğe bağlılar "Daha fazla"da
        var yeni = e.Find("label[for=u-aciklama]").TextContent;
        Assert.StartsWith("Açıklama", yeni.Trim());
        Assert.DoesNotContain(sayfaAciklamasi, yeni);

        DuzenleAc(e);
        var duz = e.Find("label[for=d-aciklama]").TextContent;
        Assert.StartsWith("Açıklama", duz.Trim());
        Assert.DoesNotContain(sayfaAciklamasi, duz);
    }

    // ── #84 — maden kilidi ipucu ekranda TEK kez ─────────────────────────────

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Urunler_maden_kilidi_ipucu_tek(string mod)
    {
        var e = Ciz(mod);
        e.Find("#urun-yeni-ac").Click();
        SeciciYardim.Sec(e, "u-tip", "ZIYNET");
        var sebep = e.Find("#u-maden-kilit").TextContent.Trim();
        Assert.False(string.IsNullOrEmpty(sebep));
        Assert.True(e.Find("#u-maden").HasAttribute("disabled"));
        var diyalog = e.Find("#u-maden-kilit").Closest("[role=dialog]")!.TextContent;
        var adet = System.Text.RegularExpressions.Regex.Matches(diyalog, System.Text.RegularExpressions.Regex.Escape(sebep)).Count;
        Assert.Equal(1, adet);
        Assert.Empty(e.FindAll("#u-maden-durum"));
    }

    // ── #15/#16/#38 — düzenle çekmecesi tam genişlik; belge türü tam satır ────

    [Fact]
    public void Urunler_duzenle_cekmece_belge_turu_tam_satir()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        DuzenleAc(e);
        if (e.FindAll("#d-belge-tur").Count == 0) e.Find("#d-daha-fazla").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#d-belge-tur")));

        var cekmece = e.Find(".cekmece");
        // Çekmecede YARIM SÜTUN yok: 480 px + 18 px yazıda yarım sütun kırılıyordu.
        Assert.Empty(cekmece.QuerySelectorAll(".form-grid-2, .form-grid-3"));
        // Belge türü ve Dosya seç AYNI SATIRDA değil: ikisi de alt alta tam genişlik.
        var tur = e.Find("#d-belge-tur-sar");
        var dosya = e.Find("#belge-sec");
        Assert.Null(tur.Closest(".form-grid-2"));
        Assert.Null(dosya.Closest(".form-grid-2"));
        Assert.NotNull(tur.Closest(".form-dikey"));
        // Tedarikçi ve Ayar seçicileri de tam satır.
        Assert.Null(e.Find("#d-tedarikci-sar").Closest(".form-grid-2"));
        Assert.Null(e.Find("#d-ayar-sar").Closest(".form-grid-2"));
        Assert.Contains("form-dikey", e.Find("#d-p16").ClassName);
    }

    // ── #14 — reyon yerleşiminde Reyon ve Adet aynı ızgaranın doğrudan çocuğu ─

    [Fact]
    public void Urunler_yerlesim_reyon_ve_adet_ayni_izgara_satirinda()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        e.Find("#u-stok-adet-u1").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yer-reyon-s1-sar")));
        var reyon = e.Find("#yer-reyon-s1-sar");
        var adet = e.Find("#yer-adet-s1-sar");
        Assert.NotNull(reyon.ParentElement);
        Assert.Same(reyon.ParentElement, adet.ParentElement);
        Assert.Contains("form-grid-2", reyon.ParentElement!.ClassName);
        // Atanmamış açıklaması tablo hücresinde DEĞİL (hücreyi genişletip pencere kenarında kesiliyordu).
        Assert.Null(e.Find("#yerlesim-atanmamis-ipucu-s1").Closest("table"));
        Assert.DoesNotContain(Sozlukler.Tr.Yerlesim.AtanmamisIpucu, e.Find("#yerlesim-atanmamis-s1").TextContent);

        // Aşım uyarısı ızgaranın DIŞINDA (üçüncü raya binip kutuyu itmesin) ve çalışıyor.
        e.Find("#yer-adet-s1").Input("5");
        e.Find("#yer-adet-s1").Blur();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yer-adet-asim-s1")));
        Assert.Null(e.Find("#yer-adet-asim-s1").Closest(".form-grid-2"));
    }

    // ── KIYAS URUN-SADE — emoji 0, "Görsel yok" satır metni 0 ────────────────

    [Fact]
    public void Urunler_liste_emoji_ve_gorsel_yok_tekrari_yok()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        var tablo = e.Find("table").TextContent;
        Assert.DoesNotContain(Sozlukler.Tr.Urun.GorselYok, tablo);
        // Erişilebilirlik korunur: ikon etiketi ekran okuyucuya söyler.
        Assert.Equal(Sozlukler.Tr.Urun.GorselYok,
            e.Find("#u-gorsel-yok-u1 svg").GetAttribute("aria-label"));
        foreach (var d in e.FindAll("button"))
            Assert.DoesNotMatch(@"[☀-➿]|[\uD83C-\uD83E][\uDC00-\uDFFF]", d.TextContent);
    }

    // ── KESIF-P4 tur 2 · #37 — etiket sade ad, ipucunda ham kod 0 ──────────────

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Urunler_parite_etiket_ve_ipucu_ham_kodsuz(string mod)
    {
        var e = Ciz(mod);
        e.Find("#urun-yeni-ac").Click();
        e.Find("#u-daha-fazla").Click();   // PAKET GOC-C (devir "zorunlu önce"): yeni ürün formu PRO'da da yalnız zorunlularla açılır — isteğe bağlılar "Daha fazla"da (iki modda aynı)
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#u-parite")));
        Assert.StartsWith(KesifP4Sozlugu.Tr.PariteEtiketi, e.Find("label[for=u-parite]").TextContent.Trim());
        Assert.DoesNotContain("Parite", e.Find("label[for=u-parite]").TextContent);
        Assert.Equal(KesifP4Sozlugu.Tr.PariteIpucu, e.Find("#u-parite-ipucu").TextContent.Trim());
        // Ham kotasyon kodu (CEYREK_YENI, REŞAT…) ipucunda yok.
        Assert.DoesNotMatch(@"[A-ZÇĞİÖŞÜ]{3,}_[A-ZÇĞİÖŞÜ0-9]+|REŞAT|RESAT", e.Find("#u-parite-ipucu").TextContent);

        DuzenleAc(e);
        if (mod == ArayuzModuServisi.Basit) e.Find("#d-daha-fazla").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#d-parite-ipucu")));
        Assert.Equal(KesifP4Sozlugu.Tr.PariteIpucu, e.Find("#d-parite-ipucu").TextContent.Trim());
        Assert.StartsWith(KesifP4Sozlugu.Tr.PariteEtiketi, e.Find("label[for=d-parite]").TextContent.Trim());
    }

    // ── KESIF-P4 tur 2 · #15 — uzun cari unvanı kısa ad, tam unvan alt bilgide ──

    [Fact]
    public void Urunler_tedarikci_secici_kisa_unvan_govdede_kimlik()
    {
        Assert.Equal("Anadolu Kapalıçarşı…",
            Kuyumcu.Web.Pages.Urunler.KisaUnvan("Anadolu Kapalıçarşı Kuyumculuk Sanayi ve Ticaret Anonim Şirketi"));
        Assert.Equal("Ege Kuyum Tic. A.Ş.",
            Kuyumcu.Web.Pages.Urunler.KisaUnvan("Ege Kuyum Ticaret Anonim Şirketi"));
        Assert.Equal("Ege San. ve Tic. Ltd. Şti.",
            Kuyumcu.Web.Pages.Urunler.KisaUnvan("Ege Sanayi ve Ticaret Limited Şirketi"));
        Assert.Equal("Ayşe Demir", Kuyumcu.Web.Pages.Urunler.KisaUnvan("Ayşe Demir"));

        var e = Ciz(ArayuzModuServisi.Profesyonel);
        DuzenleAc(e);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#d-tedarikci")));
        var c2 = SeciciYardim.Secenekler(e, "d-tedarikci").Single(o => o.Deger() == "c2");
        // Seçenek ADI kısa unvan (tavan içinde), alt bilgi TAM unvan.
        var kisa = c2.Ad().Replace(c2.QuerySelector(".secici-alt")!.TextContent, "").Trim();
        Assert.Equal("Anadolu Kapalıçarşı…", kisa);
        Assert.True(kisa.Length <= Kuyumcu.Web.Pages.Urunler.KisaUnvanTavan, kisa);
        // Tam unvan listede alt bilgi olarak görünür (sessiz kırpma yok).
        Assert.Contains("Anadolu Kapalıçarşı Kuyumculuk Sanayi ve Ticaret Anonim Şirketi",
            SeciciYardim.ListeMetni(e, "d-tedarikci"));
        SeciciYardim.Sec(e, "d-tedarikci", "c2");
        Assert.Equal("Anadolu Kapalıçarşı…", e.Find("#d-tedarikci").TextContent.Trim());
        e.Find("#cekmece-urun-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "PATCH" && i.Yol.EndsWith("/urunler/u1")));
        var patch = Istekler().Last(i => i.Metot == "PATCH" && i.Yol.EndsWith("/urunler/u1"));
        Assert.Equal("c2", Alan(patch.Govde, "tedarikciId"));
    }

    // ── KESIF-P4 tur 2 · #16 — ham dosya kontrolü gizli, düğme + dosya adı ─────

    [Fact]
    public void Urunler_belge_dosya_secici_gizli_girdi_etiketli_dugme()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        DuzenleAc(e);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#belge-sec")));
        var girdi = e.Find("#belge-sec");
        Assert.Equal("file", girdi.GetAttribute("type"));
        Assert.Contains("gorunmez", girdi.ClassName);
        Assert.DoesNotContain("girdi", girdi.ClassList);
        var dugme = e.Find("#belge-sec-dugme");
        Assert.Equal("LABEL", dugme.TagName);
        Assert.Equal("belge-sec", dugme.GetAttribute("for"));
        Assert.Contains("btn-ikincil", dugme.ClassName);
        Assert.Equal(Sozlukler.Tr.Belge.DosyaSec, dugme.TextContent.Trim());
        Assert.Equal(KesifP4Sozlugu.Tr.DosyaSecilmedi, e.Find("#belge-sec-ad").TextContent.Trim());
    }

    // ── KESIF-P4 tur 2 · #14 — reyon listesi tablo değil, ad sarar, adet sağda ─

    [Fact]
    public void Urunler_yerlesim_reyon_listesi_tablosuz_ad_ve_adet()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.Find("#u-stok-adet-u1").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#urun-tas-liste")));
        var liste = e.Find("#urun-tas-liste");
        Assert.Equal("UL", liste.TagName);
        Assert.Null(liste.Closest("table"));
        Assert.Empty(e.Find("#urun-yerlesim").QuerySelectorAll("table"));
        var satirlar = liste.QuerySelectorAll("li.panel-liste-satir--is");
        Assert.Equal(3, satirlar.Length);   // başlık + Ön Vitrin + Atanmamış
        var vitrin = satirlar[1];
        Assert.Equal("Ön Vitrin", vitrin.QuerySelector(".panel-liste-ad")!.TextContent.Trim());
        Assert.Equal("1", vitrin.QuerySelector(".panel-liste-tutar")!.TextContent.Trim());
        var atanmamis = e.Find("#yerlesim-atanmamis-s1");
        Assert.Equal("2", atanmamis.QuerySelector(".panel-liste-tutar")!.TextContent.Trim());
        // Yatay kaydırma ipucu (KartListe) bu bölümde yok.
        Assert.Empty(e.Find("#urun-yerlesim").QuerySelectorAll(".kart-liste-ipucu"));
    }

    // ── KESIF-P4 tur 2 — ad aramasında Enter "barkod bulunamadı" bandı basmaz ──

    [Fact]
    public void Urunler_ad_aramasinda_enter_barkod_hatasi_basmaz()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        // Boşluklu AD: barkod ucuna HİÇ gidilmez.
        e.Find("#urun-arama").Input("burma bilezik");
        e.Find("#urun-arama").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Yol.Contains("q=burma")));
        Assert.DoesNotContain(Istekler(), i => i.Yol.Contains("/urunler/barkod/"));
        Assert.DoesNotContain(Sozlukler.Tr.Urun.BarkodBulunamadi, e.Markup);

        // Tek sözcüklü AD: barkod denenir, 404 olur, liste doluysa bant YOK.
        e.Find("#urun-arama").Input("burma");
        e.Find("#urun-arama").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Yol.Contains("/urunler/barkod/burma")));
        Assert.NotEmpty(e.FindAll("[id^=u-ayar-]"));
        Assert.DoesNotContain(Sozlukler.Tr.Urun.BarkodBulunamadi, e.Markup);

        // Hiçbir şey eşleşmiyorsa bant görünür (sessiz etkisizlik yok).
        e.Find("#urun-arama").Input("zzz");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("[id^=u-ayar-]")));
        e.Find("#urun-arama").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        e.WaitForAssertion(() => Assert.Contains(Sozlukler.Tr.Urun.BarkodBulunamadi, e.Markup));

        // Gerçek barkod: kart açılır.
        e.Find("#urun-arama").Input("869001");
        e.Find("#urun-arama").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#d-ad")));
    }
}

public sealed class KesifP4EtiketTestleri : BunitContext
{
    public KesifP4EtiketTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi())
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var g = yol.EndsWith("/urunler/madenler")
                ? """{"madenler":[],"etiket":{"rodyumIbaresiGoster":false,"rodyumIbaresi":"rodyum kaplama"}}"""
                : """
                  [{"id":"u3","tip":"ZIYNET","kod":"B-3","ad":"Burma bilezik","ayarId":"K22",
                    "gramAgirlik":15.0,"tasAgirlik":0.0,"hasGrami":13.7,"barkod":"869003","aktif":true,
                    "tasKokeni":"TAS_YOK","tasIbaresi":null,"milyem":916,"maden":"ALTIN","madenAdi":"Altın","rodyumKaplama":false},
                   {"id":"u4","tip":"GUMUS","kod":"G-4","ad":"Gümüş halhal","ayarId":"G925",
                    "gramAgirlik":6.0,"tasAgirlik":0.0,"hasGrami":5.55,"barkod":null,"aktif":true,
                    "tasKokeni":"TAS_YOK","tasIbaresi":null,"milyem":925,"maden":"GUMUS","madenAdi":"Gümüş","rodyumKaplama":false}]
                  """;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    [Fact]
    public void UrunEtiketleri_ayar_adi()
    {
        var e = Render<Kuyumcu.Web.Pages.UrunEtiketleri>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Equal("22 Ayar", e.Find("#et-ayar-u3").TextContent.Trim()));
        Assert.Equal("925 Gümüş", e.Find("#et-ayar-u4").TextContent.Trim());

        e.FindAll("input[type=checkbox]").First(i => i.GetAttribute("aria-label") == "Burma bilezik").Change(true);
        var satir = e.Find(".urun-etiket-satir").TextContent;
        Assert.Contains("22 Ayar", satir);
        Assert.DoesNotContain("K22", satir);
    }
}
