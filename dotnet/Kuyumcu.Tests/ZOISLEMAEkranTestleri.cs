using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-ISLEM-A — Hurda · POS tahsilat · Banka "zorunlu önce" [bUnit]
//
// Kurucu 25.09: "zorunlu olanlar hariç ilk etapta kullanıcı görmesin … isterse
// ekler, eklemese kayıp yoksa açılır kapanır bir seçenek olarak kalsın".
//
// Her test ÇİFT yönlü sınar:
//   • açılışta gizlenen alan DOM'da YOKTUR (yalnız CSS ile saklanmış değil),
//   • "+ … ekle" ile GÖRÜNÜR ve girilen değer POST gövdesine GİRER,
//   • kapalı bölüm gövdeye varsayılanından başka değer TAŞIMAZ,
//   • değer girilmiş / hatalı bölüm KAPATILAMAZ (gizli değer, sessiz etkisizlik yok).
// Geçmiş listeleri (hurda defteri, banka hareketleri, POS defteri) ortak Katlanir
// ile KAPALI gelir; açılınca satır kimlikleri aynen durur.
//
// Varsayılan sahte yanıt 404'tür: tanımlanmamış uç "{}" ile sessizce başarılı sayılmaz.
// ═════════════════════════════════════════════════════════════════════════════

public sealed partial class ZOISLEMAEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly List<(string Parca, HttpStatusCode Kod, string Govde)> yollar = [];

    /// <summary>
    /// Bu parçayı içeren istek gerçek ağ gibi GECİKİR (await Task.Delay): sayfa
    /// "yükleniyor" durumunu gerçekten çizer. Anında dönen sahte yanıtta Blazor
    /// ara çizim yapmaz ve yeniden yükleme sırasında kaybolan durum sınanamaz
    /// (mutasyon M9 bu açığı gösterdi).
    /// </summary>
    private string? gecikmeParca;

    public ZOISLEMAEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<PosServisi>();
    }

    private sealed class SahteApi(ZOISLEMAEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            if (t.gecikmeParca is { } gp && yol.Contains(gp)) await Task.Delay(60, iptal);
            foreach (var (p, kod, g) in t.yollar)
                if (yol.Contains(p) && (p.StartsWith("POST ") ? istek.Method == HttpMethod.Post : true))
                    return Yanit(kod, g);
            return Yanit(HttpStatusCode.NotFound, """{"message":"tanımsız uç"}""");
        }

        private static HttpResponseMessage Yanit(HttpStatusCode kod, string govde) => new(kod)
        {
            Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    private void Yol(string parca, string govde, HttpStatusCode kod = HttpStatusCode.OK) =>
        yollar.Add((parca, kod, govde));

    private void Oturum(string rol = "SAHIP", string? sube = "s1") =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, sube));

    private List<(string Metot, string Yol, string Govde)> Postlar(string parca)
    {
        lock (istekler) return istekler.Where(i => i.Metot == "POST" && i.Yol.Contains(parca)).ToList();
    }

    private static System.Text.Json.JsonElement Json(string ham) =>
        System.Text.Json.JsonDocument.Parse(ham).RootElement;

    // ══ HURDA ═══════════════════════════════════════════════════════════════

    private void HurdaYollari(string defter = """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}""")
    {
        Yol("/islemler/hurda-satis", """
            {"id":"h9","maden":"ALTIN","madenAdi":"Altın","hasGrami":1,
             "birimFiyatTlHasGram":6800,"toplamTl":6800,"tahsilEdilenTl":6800,"kalanTl":0,
             "kalanHurdaHasGram":8.16,"kasaBakiyesi":256800,"paraBirimi":"TRY",
             "tarih":"2026-09-25T09:00:00Z","fireKaydiAcik":false,"fireNotu":null,
             "aliciKabulHasGrami":1,"tahlilFarkiHasGram":0}
            """);
        Yol("/hurda/bakiye", """{"subeId":"s1","satirlar":[{"maden":"ALTIN","madenAdi":"Altın","hasGram":9.16,"girenHas":9.16,"cikanHas":0,"tahlilFarkiHas":0}],"fireKaydiAcik":false,"fireNotu":null}""");
        Yol("/hurda/defter", defter);
        Yol("/kasalar", """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":250000,"aktif":true}]""");
        Yol("/kur/guncel", """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-25T09:00:00Z","ayarFiyatlari":[]}""");
        Yol("/arama", """{"sonuclar":[],"toplam":0}""");
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Hurda> HurdaEkrani()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Hurda>().AyrintiAc();
        e.WaitForAssertion(() => Assert.False(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        return e;
    }

    private static void Gir(IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e, string secici, string deger) =>
        e.WaitForAssertion(() =>
        {
            if (e.Find(secici).GetAttribute("value") != deger) e.Find(secici).Input(deger);
            Assert.Equal(deger, e.Find(secici).GetAttribute("value"));
        });

    [Fact]
    public void ZO_hurda_acilista_YALNIZ_zorunlu_alan_istege_baglilar_DOMda_yok()
    {
        HurdaYollari();
        var e = HurdaEkrani();

        // Zorunlu alan ve sistemin bildiği değerler görünür.
        Assert.Single(e.FindAll("#hrd-has"));
        Assert.Contains("Merkez Kasa", e.Find("#hrd-kasa-sabit").TextContent);
        Assert.Contains(Bicim.Para(6800m, "TRY"), e.Find("#hrd-fiyat-ozet").TextContent);
        Assert.Contains(Sozlukler.Tr.Hurda.FiyatKurdan, e.Find("#hrd-fiyat-ozet").TextContent);

        // İsteğe bağlılar DOM'da YOK.
        foreach (var id in new[] { "#hrd-kabul-has", "#hrd-tahlil-fark", "#hrd-fiyat", "#hrd-cari", "#hrd-odeme", "#hrd-tahsil", "#hrd-detay" })
            Assert.Empty(e.FindAll(id));
        Assert.Equal(ZoIslemSozlugu.Tr.DetayEkle, e.Find("#hrd-detay-ac").TextContent.Trim());

        // Geçmiş (defter) KAPALI akordeon.
        Assert.Equal("false", e.Find("#hrd-gecmis-ac").GetAttribute("aria-expanded"));
        Assert.Empty(e.FindAll("#hrd-gecmis-govde"));
    }

    [Fact]
    public void ZO_hurda_detay_ekle_ile_alanlar_GORUNUR_ve_POSTa_girer()
    {
        HurdaYollari();
        var e = HurdaEkrani();
        e.Find("#hrd-detay-ac").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#hrd-detay")));
        foreach (var id in new[] { "#hrd-kabul-has", "#hrd-fiyat", "#hrd-cari", "#hrd-odeme", "#hrd-tahsil" })
            Assert.Single(e.FindAll(id));
        // Açıkken özet satırı ikinci kez fiyat yazmaz (kutu + kaynak notu yeterli).
        Assert.Empty(e.FindAll("#hrd-fiyat-ozet"));

        Gir(e, "#hrd-has", "1");
        // 29.09 (14. doğrulama): "6.800" BELİRSİZDİR (P-78) — eski hâli yeşil geçiyordu çünkü belirsiz
        // tahsil null kalıp "tamamı" (= 6.800) sayılıyordu; bu tam da kapatılan hataydı. Açık yazılır.
        Gir(e, "#hrd-tahsil", "6800");
        // 28.09: tahsil aracı ortak Form/Secim (radyo) — seçenek değerle bulunur.
        e.Find("#hrd-odeme input[value='HAVALE']").Change(true);
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/islemler/hurda-satis")));

        var g = Json(Postlar("/islemler/hurda-satis")[0].Govde);
        Assert.Equal("HAVALE", g.GetProperty("odemeTuru").GetString());
        Assert.Equal(6800m, g.GetProperty("tahsilEdilenTl").GetDecimal());
    }

    [Fact]
    public void ZO_hurda_detay_KAPALIYKEN_govde_yalniz_varsayilanlari_tasir()
    {
        HurdaYollari();
        var e = HurdaEkrani();
        Gir(e, "#hrd-has", "0,5");
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/islemler/hurda-satis")));

        var g = Json(Postlar("/islemler/hurda-satis")[0].Govde);
        Assert.Equal(0.5m, g.GetProperty("hasGrami").GetDecimal());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, g.GetProperty("aliciKabulHasGrami").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, g.GetProperty("cariId").ValueKind);
        Assert.Equal("NAKIT", g.GetProperty("odemeTuru").GetString());
        // Tahsil edilen = tutarın tamamı (0,5 × 6.800); kurdan gelen fiyat aynen.
        Assert.Equal(3400m, g.GetProperty("tahsilEdilenTl").GetDecimal());
        Assert.Equal(6800m, g.GetProperty("birimFiyatTlHasGram").GetDecimal());
        Assert.Equal("kur-1", g.GetProperty("kurSnapshotId").GetString());
    }

    [Fact]
    public void ZO_hurda_detayda_deger_varken_bolum_KAPATILAMAZ_bos_iken_kapanir()
    {
        HurdaYollari();
        var e = HurdaEkrani();
        e.Find("#hrd-detay-ac").Click();
        // Boşken "Detayları gizle" var ve çalışır.
        e.WaitForElement("#hrd-detay-gizle").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hrd-detay")));

        e.Find("#hrd-detay-ac").Click();
        Gir(e, "#hrd-kabul-has", "0,4");
        // Değer girildi → gizle düğmesi YOK (gizli değer POST'a gidemez).
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hrd-detay-gizle")));
        Assert.Single(e.FindAll("#hrd-detay"));
    }

    [Fact]
    public void ZO_hurda_detay_alani_HATALIYSA_bolum_acik_kalir_ve_mesaj_gorunur()
    {
        HurdaYollari();
        var e = HurdaEkrani();
        e.Find("#hrd-detay-ac").Click();
        Gir(e, "#hrd-has", "1");
        Gir(e, "#hrd-tahsil", "100");           // tutar 6.800 > tahsil, ramatçı yok
        e.Find("#hrd-kaydet").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#alan-hata-hrd-cari")));
        Assert.Single(e.FindAll("#hrd-detay"));
        Assert.Empty(e.FindAll("#hrd-detay-gizle"));
        Assert.Empty(Postlar("/islemler/hurda-satis"));

        // Tahsil hâlâ eksikken ramatçı hatası duruyor (tutarı güncel) → bölüm HÂLÂ kapatılamaz
        // (hatalı alan gizlenemez).
        Gir(e, "#hrd-tahsil", "200");
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(6600m, "TRY"), e.Find("#alan-hata-hrd-cari").TextContent));
        Assert.Single(e.FindAll("#hrd-detay"));
        Assert.Empty(e.FindAll("#hrd-detay-gizle"));

        // 29.09 (13. doğrulama): tahsil boşaltılınca (= tamamı) koşul ORTADAN KALKAR; mesaj bayat
        // kalmaz, bölüm yeniden kapatılabilir.
        Gir(e, "#hrd-tahsil", "");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#alan-hata-hrd-cari")));
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#hrd-detay-gizle")));
    }

    [Fact]
    public void ZO_hurda_defter_akordeonu_acilinca_satirlar_ve_duzelt_dugmesi_gorunur()
    {
        HurdaYollari("""
            {"satirlar":[{"id":"h8","tur":"HURDA_SATIS","tarih":"2026-09-24T09:00:00Z","tlTutari":3400,"hasGrami":-0.5,
              "maden":"ALTIN","madenAdi":"Altın","cariId":null,"aciklama":null,"paraBirimi":"TRY",
              "stornoSatiri":false,"stornolandi":false,"tahlilKaynakHareketId":null,"tahlilFarkiHasGram":0}],
             "toplamAdet":1,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}
            """);
        var e = HurdaEkrani();
        Assert.Equal("1", e.Find("#hrd-gecmis-sayi").TextContent.Trim());
        Assert.Empty(e.FindAll("#hrd-defter"));
        e.Find("#hrd-gecmis-ac").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#hrd-defter")));
        Assert.Single(e.FindAll("#hrd-duzelt-h8"));
        Assert.Equal("true", e.Find("#hrd-gecmis-ac").GetAttribute("aria-expanded"));
    }

    // 29.09 (tasarım hakemi k3 KRİTİK, K13): Hurda defteri 1100 px altında KART — başlık gizli,
    // has gram ve tutar etiketiyle, düzelt düğmesi en altta tam satır ve sarabilir; tahlil farkı
    // etiketi yalnız hurda satışında (boş hücre etiket basmaz). Kimlikler değişmedi.
    [Fact]
    public void ZO29p_hurda_defteri_dar_ekranda_KART_kalibi()
    {
        HurdaYollari("""
            {"satirlar":[{"id":"h8","tur":"HURDA_SATIS","tarih":"2026-09-24T09:00:00Z","tlTutari":3400,"hasGrami":-0.5,
              "maden":"ALTIN","madenAdi":"Altın","cariId":null,"aciklama":null,"paraBirimi":"TRY",
              "stornoSatiri":false,"stornolandi":false,"tahlilKaynakHareketId":null,"tahlilFarkiHasGram":0},
             {"id":"h9","tur":"HURDA_ALIS","tarih":"2026-09-23T09:00:00Z","tlTutari":1700,"hasGrami":0.25,
              "maden":"ALTIN","madenAdi":"Altın","cariId":null,"aciklama":null,"paraBirimi":"TRY",
              "stornoSatiri":false,"stornolandi":false,"tahlilKaynakHareketId":null,"tahlilFarkiHasGram":0}],
             "toplamAdet":2,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}
            """);
        var e = HurdaEkrani();
        e.Find("#hrd-gecmis-ac").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#hrd-defter")));
        Assert.Contains("zo-kart-tablo", e.Find("#hrd-defter").ClassList);
        var h = e.Find("#hrd-satir-h8").QuerySelectorAll("td");
        string[] beklenen = ["zo-k-bas", "zo-k-durum", "zo-k-ad", "zo-k-gram", "zo-k-tutar", "zo-k-fark", "zo-k-eylem"];
        Assert.Equal(beklenen.Length, h.Length);
        for (var i = 0; i < beklenen.Length; i++) Assert.Contains(beklenen[i], h[i].ClassList);
        Assert.Equal(Sozlukler.Tr.Hurda.HasGram, h[3].GetAttribute("data-etiket"));
        Assert.Equal(Sozlukler.Tr.Ortak.Tutar, h[4].GetAttribute("data-etiket"));
        Assert.False(string.IsNullOrWhiteSpace(h[5].GetAttribute("data-etiket")));
        Assert.NotNull(h[6].QuerySelector("#hrd-duzelt-h8"));
        // Alış satırında tahlil farkı hücresi boş → etiket de YOK (kartta yalnız başlık basılmaz).
        Assert.Null(e.Find("#hrd-fark-h9").GetAttribute("data-etiket"));

        var css = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "zo-islem.css"));
        var blok = System.Text.RegularExpressions.Regex.Match(css, @"@media \(max-width: 1100px\) \{\s*#hrd-defter > thead \{ display: none; \}[\s\S]*?\n\}").Value;
        Assert.False(string.IsNullOrEmpty(blok));
        Assert.Matches(@"#hrd-defter > tbody > tr \{\s*display: grid;", blok);
        Assert.Matches(@"#hrd-defter > tbody > tr > td \{[^}]*white-space: normal;", blok);
        Assert.Matches(@"td\.zo-k-eylem \{ grid-column: 1 / -1;", blok);
        Assert.Matches(@"td\.zo-k-eylem > \.dugme \{ white-space: normal;", blok);
        Assert.Matches(@"td\[data-etiket\]::before \{[^}]*content: attr\(data-etiket\);", blok);
    }

    // 28.09 gerçek tarayıcı (PRO 1440): defter notu PRO modda "fark bacağı / bacakların"
    // jargonu taşıyordu. bUnit varsayılanı PRO (BasitMi=false) — iki modda AYNI sade cümle.
    [Fact]
    public void ZO28_hurda_defter_notu_PRO_modda_da_jargonsuz()
    {
        HurdaYollari("""
            {"satirlar":[{"id":"h8","tur":"HURDA_SATIS","tarih":"2026-09-24T09:00:00Z","tlTutari":3400,"hasGrami":-0.5,
              "maden":"ALTIN","madenAdi":"Altın","cariId":null,"aciklama":null,"paraBirimi":"TRY",
              "stornoSatiri":false,"stornolandi":false,"tahlilKaynakHareketId":null,"tahlilFarkiHasGram":0}],
             "toplamAdet":1,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}
            """);
        var e = HurdaEkrani();
        e.Find("#hrd-gecmis-ac").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#hrd-defter")));
        // 29.09 (tasarım hakemi KRİTİK): PRO'da da geliştirici notu YOK — kutu iki modda aynı tek cümle.
        var kutu = e.Find("#degismez-ipucu-HurdaSatisi");
        Assert.Contains(string.Format(ZoIslemSozlugu.Tr.HurdaDefterKilitBasit, Sozlukler.Tr.Hurda.Duzelt), kutu.TextContent);
        Assert.Empty(kutu.QuerySelectorAll(".degismez-alternatif"));
        foreach (var j in new[] { "bu sürümde", "ters kayıt", "stok satırı", "motor", "stornolanmaz", "fark satırı", "Düzeltme şöyle" })
            Assert.DoesNotContain(j, e.Markup);
        Assert.DoesNotContain(Sozlukler.Tr.Hurda.DegismezNotu, e.Markup);
        Assert.DoesNotContain(HurdaOnaySozlugu.Tr.DuzeltmeNotuBasit, e.Markup);
        Assert.DoesNotContain(Sozlukler.Tr.Hurda.DuzeltmeNotu, e.Markup);
        // Düzeltme çekmecesinin notu da kullanıcı dilinde.
        e.Find("#hrd-duzelt-h8").Click();
        e.WaitForAssertion(() => Assert.Equal(ZoIslemSozlugu.Tr.HurdaDuzeltCekmeceNot, e.Find("#cekmece-tahlilfarki-not").TextContent.Trim()));
        Assert.DoesNotContain("fark satırı", e.Markup);
        Assert.DoesNotContain("bacağı", e.Markup);
        Assert.DoesNotContain("bacak", e.Markup);
    }

    // 28.09 gerçek tarayıcı (BASİT 1440): defter kilit kutusu "Düzeltme şöyle yapılır:" başlığı
    // ile iki paragraf ve "stok satırı"/"fark satırı" iç terimleri taşıyordu (POS kutusuyla aynı
    // kusur). BASİT'te TEK sade cümle, doğru yol satırdaki düğmenin ADIYLA; alternatif bölüm yok.
    [Fact]
    public void ZO28_hurda_defter_kilidi_BASIT_modda_TEK_cumle_ve_dugme_adi()
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => true);
        HurdaYollari("""
            {"satirlar":[{"id":"h8","tur":"HURDA_SATIS","tarih":"2026-09-24T09:00:00Z","tlTutari":3400,"hasGrami":-0.5,
              "maden":"ALTIN","madenAdi":"Altın","cariId":null,"aciklama":null,"paraBirimi":"TRY",
              "stornoSatiri":false,"stornolandi":false,"tahlilKaynakHareketId":null,"tahlilFarkiHasGram":0}],
             "toplamAdet":1,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}
            """);
        var e = HurdaEkrani();
        e.Find("#hrd-gecmis-ac").Click();
        var kutu = e.WaitForElement("#degismez-ipucu-HurdaSatisi");
        Assert.Contains(string.Format(ZoIslemSozlugu.Tr.HurdaDefterKilitBasit, Sozlukler.Tr.Hurda.Duzelt), kutu.TextContent);
        Assert.Empty(kutu.QuerySelectorAll(".degismez-alternatif"));
        Assert.DoesNotContain(HurdaOnaySozlugu.Tr.DuzeltmeNotuBasit, kutu.TextContent);
        Assert.DoesNotContain("stok satırı", kutu.TextContent);
        Assert.Empty(kutu.QuerySelectorAll("button"));
    }

    // 28.09 gerçek tarayıcı: seçili hesap sekmesi birincil düğmeyle aynı dolu mavi değil
    // (tek birincil düğme) — yumuşak vurgu belirteçleri; sekme seçili durumunu aria ile taşır.
    [Fact]
    public void ZO28_banka_secili_hesap_sekmesi_DOLU_vurgu_degil_yumusak_belirtec()
    {
        var e = BankaEkrani();
        var sekme = e.Find(".zo-banka-sekmeler > .sekme.secili");
        Assert.Equal("true", sekme.GetAttribute("aria-pressed"));
        var css = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "zo-islem.css"));
        Assert.Matches(@"\.zo-banka-sekmeler > \.sekme\.secili\s*\{[^}]*background:\s*var\(--vurgu-yuzey\);\s*border-color:\s*var\(--vurgu\);\s*color:\s*var\(--vurgu\);", css);
    }

    // ── Şube bağlamı: tek kısa sebep + ne yapılacağı (UX-S30) ────────────────

    // Sunucu yanıtları CANLIDA ölçüldü (25.09, 5170): tüm-şube rolü + çok şube →
    // 400 "Hurda işlemleri için şube bağlamı gerekli."; şubeye kilitli TEZGAHTAR →
    // 403 "Şube bağlamı çözülemedi: …" (küresel çözümleyici).
    [Theory]
    [InlineData("MUDUR", 400, "Hurda işlemleri için şube bağlamı gerekli.")]
    [InlineData("MUHASEBE", 400, "Hurda işlemleri için şube bağlamı gerekli.")]
    [InlineData("SAHIP", 400, "Hurda işlemleri için şube bağlamı gerekli.")]
    [InlineData("TEZGAHTAR", 403, "Şube bağlamı çözülemedi: bu rol tüm şubeleri göremez ve oturumda şube yok.")]
    public void ZO_hurda_subesiz_oturumda_sebep_TEK_kez_ve_ic_terim_yok(string rol, int kod, string sunucuMetni)
    {
        Oturum(rol, sube: null);
        Yol("/hurda/bakiye", "{\"message\":\"" + sunucuMetni + "\"}", (HttpStatusCode)kod);
        var e = Render<Kuyumcu.Web.Pages.Hurda>().AyrintiAc();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#hrd-hata")));

        var beklenen = rol == "SAHIP" ? ZoIslemSozlugu.Tr.HurdaSubeYokSahip : SadeSozlugu.Tr.HurdaSubeYok;
        Assert.Contains(beklenen, e.Find("#hrd-hata").TextContent);
        // TEK kez: bant yazar; form notu, düğme yanı ve bakiye kartı kopyalamaz.
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(e.Markup, System.Text.RegularExpressions.Regex.Escape(beklenen)));
        Assert.DoesNotContain("şube bağlamı", e.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(e.FindAll("#hrd-bakiye-okunamadi"));
        Assert.Empty(e.FindAll("#hrd-kaydet-kilit"));
        // Form yine KİLİTLİ (SADE-1 #24 korunur).
        Assert.True(e.Find("#hrd-form-alan").HasAttribute("disabled"));
        Assert.True(e.Find("#hrd-kaydet").HasAttribute("disabled"));
    }

    [Fact]
    public void ZO_hurda_subeli_oturumda_BASKA_bir_hata_sunucu_metniyle_kalir()
    {
        Oturum("MUDUR", sube: "s1");
        Yol("/hurda/bakiye", """{"message":"Sunucu yoğun, tekrar deneyin."}""", HttpStatusCode.BadRequest);
        var e = Render<Kuyumcu.Web.Pages.Hurda>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Contains("Sunucu yoğun", e.Find("#hrd-hata").TextContent));
        Assert.DoesNotContain(SadeSozlugu.Tr.HurdaSubeYok, e.Markup);
        Assert.Single(e.FindAll("#hrd-bakiye-okunamadi"));
    }

    // ══ BANKA ═══════════════════════════════════════════════════════════════

    private const string HesapSahipli =
        """{"id":"b1","bankaAdi":"Ziraat","hesapAdi":"Ana Hesap","hesapSahibi":"X Ltd.","iban":"TR200000000000000000000001","paraBirimi":"TRY","bakiye":0,"aktif":true}""";
    private const string HesapSahipsiz =
        """{"id":"b2","bankaAdi":"Garanti","hesapAdi":"POS Hesabı","hesapSahibi":null,"iban":"TR900000000000000000000002","paraBirimi":"TRY","bakiye":0,"aktif":true}""";

    private void BankaYollari(string hesaplar)
    {
        Yol("/masak/esikler", """{"kimlikEsigi":185000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000,"ekBelgeEsigi":20000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""");
        Yol("/masak/gereksinim", """{"kademe":"YALNIZ_KIMLIK","kimlikGerekli":true,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""");
        Yol("/hareketler", """{"hareketler":[{"id":"bh1","yon":"GELEN","tutar":1250,"karsiTarafAd":"Ali Veli","aciklama":"Dekont","durum":"BEKLIYOR","tarih":"2026-09-16T10:00:00Z"}]}""");
        Yol("/hareket", """{"id":"bh2","yon":"GELEN","tutar":500,"durum":"BEKLIYOR"}""");
        Yol("/bankalar", "{\"hesaplar\":[" + hesaplar + "]}");
        Yol("/cari", """[{"id":"c1","ad":"Ayşe Yılmaz"}]""");
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Banka> BankaEkrani(string hesaplar = HesapSahipli)
    {
        Oturum();
        BankaYollari(hesaplar);
        var e = Render<Kuyumcu.Web.Pages.Banka>().AyrintiAc();
        e.WaitForElement("#bh-dekont-gir");
        return e;
    }

    [Fact]
    public void ZO_banka_hareketler_KAPALI_akordeon_acilinca_satirlar_gorunur()
    {
        var e = BankaEkrani();
        e.WaitForAssertion(() => Assert.Equal("1", e.Find("#bh-gecmis-sayi").TextContent.Trim()));
        Assert.Equal("false", e.Find("#bh-gecmis-ac").GetAttribute("aria-expanded"));
        Assert.DoesNotContain("Ali Veli", e.Markup);
        e.Find("#bh-gecmis-ac").Click();
        e.WaitForAssertion(() => Assert.Contains("Ali Veli", e.Find("#bh-gecmis-govde").TextContent));
        // Emoji yok — ince çizgi ikon bileşeni.
        Assert.DoesNotContain("🏛", e.Markup);
        Assert.DoesNotContain("✏", e.Markup);
    }

    [Fact]
    public void ZO_banka_yeni_hesap_sahibi_KAPALI_ekle_ile_acilir_ve_POSTa_girer()
    {
        var e = BankaEkrani();
        e.Find("#banka-yeni").Click();
        e.WaitForElement("#hesap-banka");
        Assert.Empty(e.FindAll("#hesap-sahibi"));
        Assert.Equal(ZoIslemSozlugu.Tr.BankaDigerBilgiler, e.Find("#hesap-sahibi-ac").TextContent.Trim());

        e.Find("#hesap-sahibi-ac").Click();
        e.WaitForElement("#hesap-sahibi").Input("Yılmaz Kuyumculuk Ltd.");
        e.Find("#hesap-banka").Input("Ziraat Bankası");
        e.Find("#hesap-ad").Input("Vadesiz");
        e.Find("#hesap-iban").Input("TR330006100519786457841326");
        e.Find("#hesap-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/bankalar"), p => !p.Yol.Contains("/hareket")));
        var g = Json(Postlar("/bankalar").Single(p => !p.Yol.Contains("/hareket")).Govde);
        Assert.Equal("Yılmaz Kuyumculuk Ltd.", g.GetProperty("hesapSahibi").GetString());
    }

    [Fact]
    public void ZO_banka_yeni_hesap_sahibi_EKLENMEZSE_govde_null_tasir()
    {
        var e = BankaEkrani();
        e.Find("#banka-yeni").Click();
        e.WaitForElement("#hesap-banka").Input("Ziraat Bankası");
        e.Find("#hesap-ad").Input("Vadesiz");
        e.Find("#hesap-iban").Input("TR330006100519786457841326");
        e.Find("#hesap-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/bankalar"), p => !p.Yol.Contains("/hareket")));
        var g = Json(Postlar("/bankalar").Single(p => !p.Yol.Contains("/hareket")).Govde);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, g.GetProperty("hesapSahibi").ValueKind);
    }

    [Fact]
    public void ZO_banka_duzenle_KAYITLI_sahip_varsa_alan_ACIK_gelir()
    {
        var e = BankaEkrani(HesapSahipli);
        e.Find("#bh-duzenle-b1").Click();
        e.WaitForAssertion(() => Assert.Equal("X Ltd.", e.Find("#d-sahibi").GetAttribute("value")));
        Assert.Empty(e.FindAll("#d-sahibi-ac"));
    }

    [Fact]
    public void ZO_banka_duzenle_sahipsiz_hesapta_alan_KAPALI_ekle_ile_acilir()
    {
        var e2 = BankaEkrani(HesapSahipsiz);
        e2.Find("#bh-duzenle-b2").Click();
        e2.WaitForElement("#d-banka");
        Assert.Empty(e2.FindAll("#d-sahibi"));
        e2.Find("#d-sahibi-ac").Click();
        e2.WaitForAssertion(() => Assert.Single(e2.FindAll("#d-sahibi")));
    }

    [Fact]
    public void ZO_banka_dekont_acilista_yalniz_yon_ve_tutar_detay_ile_digerleri()
    {
        var e = BankaEkrani();
        e.Find("#bh-dekont-gir").Click();
        e.WaitForElement("#dekont-tutar");
        Assert.Single(e.FindAll("#dekont-yon"));
        foreach (var id in new[] { "#dekont-tarih", "#dekont-cari", "#dekont-karsi", "#dekont-aciklama" })
            Assert.Empty(e.FindAll(id));

        e.Find("#dekont-detay-ac").Click();
        e.WaitForElement("#dekont-karsi").Input("Ahmet Demir");
        e.Find("#dekont-tutar").Input("500");
        e.WaitForAssertion(() => Assert.False(e.Find("#dekont-kaydet").HasAttribute("disabled")));
        e.Find("#dekont-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/hareket")));
        var g = Json(Postlar("/hareket")[0].Govde);
        Assert.Equal("Ahmet Demir", g.GetProperty("karsiTarafAd").GetString());
    }

    [Fact]
    public void ZO_banka_dekont_MASAK_esiginde_detay_KENDILIGINDEN_acilir()
    {
        var e = BankaEkrani();
        e.Find("#bh-dekont-gir").Click();
        e.WaitForElement("#dekont-tutar").Input("185000");
        // Müşteri seçimi yasal kapının parçası — gizli kalamaz.
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#dekont-cari")));
        Assert.Single(e.FindAll("#dekont-masak-cari-uyari"));
        Assert.Empty(e.FindAll("#dekont-detay-ac"));
    }

    // ══ POS TAHSİLAT ════════════════════════════════════════════════════════

    private const string PosSatir =
        """{"id":"t1","posCihazId":"p1","cihazAd":"Garanti POS","bankaAdi":"Garanti","brutTutar":1000,"komisyonOran":0.0185,"komisyonTutar":18.5,"netTutar":981.5,"yatanTutar":0,"kalanTutar":981.5,"taksitAdedi":1,"paraBirimi":"TRY","islemTarihi":"2026-09-24T10:00:00Z","valorTarihi":"2026-09-25T00:00:00Z","durum":"BEKLIYOR","iptalGerekcesi":null}""";

    private IRenderedComponent<Kuyumcu.Web.Pages.PosTahsilatlar> PosEkrani()
    {
        Oturum();
        Yol("/pos/valor-takvimi", "[]");
        Yol("/pos/tahsilatlar", "{\"satirlar\":[" + PosSatir + "],\"toplamSatir\":1,\"limit\":50,\"offset\":0,\"azamiSatir\":200,\"sinirDegdi\":false}");
        Yol("/pos/cihazlar", """[{"id":"p1","ad":"Garanti POS","bankaAdi":"Garanti","subeId":"s1","kasaId":"k1","bankaHesapId":null,"uyeIsyeriNo":null,"terminalNo":null,"pesinKomisyonOran":0.0185,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0.00}]""");
        Yol("/bankalar", """{"hesaplar":[]}""");
        var e = Render<Kuyumcu.Web.Pages.PosTahsilatlar>().AyrintiAc();
        e.WaitForElement("#pos-t-defter-kart-ac");
        return e;
    }

    [Fact]
    public void ZO_pos_defteri_KAPALI_acilinca_iptal_dugmesi_gorunur_filtrede_acik_kalir()
    {
        var e = PosEkrani();
        Assert.Equal("false", e.Find("#pos-t-defter-kart-ac").GetAttribute("aria-expanded"));
        Assert.Equal("1", e.Find("#pos-t-defter-kart-sayi").TextContent.Trim());
        Assert.Empty(e.FindAll("#pos-t-iptal-t1"));
        Assert.Equal(SadeSozlugu.Tr.PosDefterBaslik, e.Find("#pos-t-defter-kart-ac .katlanir-ad").TextContent.Trim());

        e.Find("#pos-t-defter-kart-ac").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#pos-t-iptal-t1")));

        // Filtre değişince liste yeniden yüklenir (gerçek ağ gecikmesiyle: arada
        // "yükleniyor" çizilir ve akordeon yeniden kurulur); akordeon AÇIK kalır.
        gecikmeParca = "/pos/tahsilatlar";
        var once = istekler.Count(i => i.Yol.Contains("/pos/tahsilatlar"));
        e.Find("#pos-t-durum input[value='BEKLIYOR']").Change(true);
        e.WaitForAssertion(() => Assert.True(istekler.Count(i => i.Yol.Contains("/pos/tahsilatlar")) > once));
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#pos-t-iptal-t1")));
        Assert.Equal("true", e.Find("#pos-t-defter-kart-ac").GetAttribute("aria-expanded"));
    }

    // 27.09 tur ölçümü (960, dolu veri): "VALÖR TARİHİ" / "CİHAZ ADI" başlıkları iki satıra
    // kırılıyordu (K3). Her iki POS tablosu tek-satır başlık sınıfını taşır ve kural
    // şerit dosyasında (css/zo-islem.css) nowrap der; tablo kendi kabında kayar.
    [Fact]
    public void ZO_pos_tablolari_baslik_TEK_SATIR_sinifini_ve_nowrap_kuralini_tasir()
    {
        Oturum();
        Yol("/pos/valor-takvimi", """[{"valorTarihi":"2026-09-25T00:00:00Z","posCihazId":"p1","cihazAd":"Garanti POS","bankaAdi":"Garanti","brut":1000,"komisyon":18.5,"net":981.5,"yatan":0,"kalan":981.5,"adet":1,"paraBirimi":"TRY"}]""");
        Yol("/pos/tahsilatlar", "{\"satirlar\":[" + PosSatir + "],\"toplamSatir\":1,\"limit\":50,\"offset\":0,\"azamiSatir\":200,\"sinirDegdi\":false}");
        Yol("/pos/cihazlar", "[]");
        Yol("/bankalar", """{"hesaplar":[]}""");
        var e = Render<Kuyumcu.Web.Pages.PosTahsilatlar>().AyrintiAc();
        e.WaitForElement("#pos-t-defter-kart-ac").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#pos-t-iptal-t1")));
        var sarmallar = e.FindAll(".tablo-sar");
        Assert.Equal(2, sarmallar.Count);
        // Doğrulayıcı 27.09 (P-39 K5): sarmalayıcı sınıfı TAM "tablo-sar" kalır; tek satır
        // kuralı tabloya verilir (ortak kaydırma kabı sözleşmesi bozulmaz).
        Assert.All(sarmallar, s => Assert.Equal("tablo-sar", s.GetAttribute("class")));
        var tablolar = e.FindAll(".tablo-sar > table.veri-tablosu");
        Assert.Equal(2, tablolar.Count);
        Assert.All(tablolar, t => Assert.Contains("zo-baslik-tek-satir", t.ClassList));

        var css = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "zo-islem.css"));
        Assert.Matches(@"\.veri-tablosu\.zo-baslik-tek-satir th\s*\{\s*white-space:\s*nowrap;", css);
        // 28.09 gerçek tarayıcı: 5 seçenekli durum süzgeci dar alanda alt alta diziliyordu
        // (açılış 1,07 ekran) — alan içerik kadar genişler, seçenek yazısı kırılmaz.
        Assert.Matches(@"#pos-t-durum-sar\s*\{\s*flex:\s*0 1 auto;", css);
        Assert.Matches(@"#pos-t-durum \.secim-secenek\s*\{\s*white-space:\s*nowrap;", css);
        // 28.09 gerçek tarayıcı 960: kısa iki kelimelik hücre kırılmaz (K3).
        Assert.Matches(@"\.veri-tablosu\.zo-baslik-tek-satir td,\s*#hrd-defter td\s*\{\s*white-space:\s*nowrap;", css);
    }

    // Doğrulayıcı 27.09 (UX-S29, sessiz kırpma yok): sunucu sınırına değince akordeon
    // KAPALIYKEN rozet dönen sayfa adedini değil sunucunun toplamını gösterir.
    [Fact]
    public void ZO_pos_defter_rozeti_SINIRA_degince_sunucu_TOPLAMINI_gosterir()
    {
        Oturum();
        Yol("/pos/valor-takvimi", "[]");
        Yol("/pos/tahsilatlar", "{\"satirlar\":[" + PosSatir + "],\"toplamSatir\":250,\"limit\":200,\"offset\":0,\"azamiSatir\":200,\"sinirDegdi\":true}");
        Yol("/pos/cihazlar", "[]");
        Yol("/bankalar", """{"hesaplar":[]}""");
        var e = Render<Kuyumcu.Web.Pages.PosTahsilatlar>().AyrintiAc();
        e.WaitForElement("#pos-t-defter-kart-ac");
        e.WaitForAssertion(() => Assert.Equal("250", e.Find("#pos-t-defter-kart-sayi").TextContent.Trim()));
        Assert.Equal("false", e.Find("#pos-t-defter-kart-ac").GetAttribute("aria-expanded"));
        e.Find("#pos-t-defter-kart-ac").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#pos-t-sinir")));
    }
    // ══ 28.09 — DOĞRULAYICI + TASARIM HAKEMİ BULGULARI ══════════════════════

    /// <summary>İki elemanın belge sırası: a, b'den ÖNCE mi? (belge sırasındaki konum)</summary>
    private static bool Once<T>(IRenderedComponent<T> e, string a, string b) where T : Microsoft.AspNetCore.Components.IComponent
    {
        var idler = e.FindAll("[id]").Select(x => x.Id).ToList();
        int ia = idler.IndexOf(a), ib = idler.IndexOf(b);
        Assert.True(ia >= 0 && ib >= 0, $"{a}={ia} {b}={ib} bulunamadı");
        return ia < ib;
    }

    // [ORTA] isteğe bağlı açıcı zorunlu alanların ARASINA girmez (yeni hesap + düzenleme).
    [Fact]
    public void ZO28_banka_hesap_sahibi_acicisi_IBAN_ve_DOVIZden_SONRA_gelir()
    {
        var e = BankaEkrani(HesapSahipsiz);
        e.Find("#banka-yeni").Click();
        e.WaitForElement("#hesap-banka");
        Assert.Single(e.FindAll("#hesap-sahibi-ac"));
        Assert.True(Once(e, "hesap-iban", "hesap-sahibi-ac"), "yeni: IBAN açıcıdan önce olmalı");
        Assert.True(Once(e, "hesap-doviz", "hesap-sahibi-ac"), "yeni: Döviz açıcıdan önce olmalı");
        Assert.True(Once(e, "hesap-sahibi-ac", "hesap-kaydet"), "yeni: açıcı kaydet düğmesinden önce");

        e.Find("#bh-duzenle-b2").Click();
        e.WaitForElement("#d-banka");
        Assert.Single(e.FindAll("#d-sahibi-ac"));
        Assert.True(Once(e, "d-iban", "d-sahibi-ac"), "düzenle: IBAN açıcıdan önce olmalı");
        Assert.True(Once(e, "d-doviz", "d-sahibi-ac"), "düzenle: Döviz açıcıdan önce olmalı");
    }

    // [KRİTİK] K22: az seçenekli seçimler ortak Form/Secim (radyo); gövdeye değer girer.
    [Fact]
    public void ZO28_banka_doviz_ve_dekont_yonu_RADYO_secimdir_ve_POSTa_girer()
    {
        var e = BankaEkrani();
        e.Find("#banka-yeni").Click();
        e.WaitForElement("#hesap-banka");
        Assert.Equal("radiogroup", e.Find("#hesap-doviz").GetAttribute("role"));
        Assert.Equal(4, e.FindAll("#hesap-doviz input.secim-radyo").Count);
        e.Find("#hesap-doviz input[value='USD']").Change(true);
        e.Find("#hesap-banka").Input("Ziraat Bankası");
        e.Find("#hesap-ad").Input("Döviz");
        e.Find("#hesap-iban").Input("TR330006100519786457841326");
        e.Find("#hesap-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/bankalar"), p => !p.Yol.Contains("/hareket")));
        Assert.Equal("USD", Json(Postlar("/bankalar").Single(p => !p.Yol.Contains("/hareket")).Govde).GetProperty("paraBirimi").GetString());

        e.Find("#bh-dekont-gir").Click();
        e.WaitForElement("#dekont-tutar");
        Assert.Equal("radiogroup", e.Find("#dekont-yon").GetAttribute("role"));
        e.Find("#dekont-yon input[value='GIDEN']").Change(true);
        e.Find("#dekont-tutar").Input("500");
        e.WaitForAssertion(() => Assert.False(e.Find("#dekont-kaydet").HasAttribute("disabled")));
        e.Find("#dekont-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/hareket")));
        Assert.Equal("GIDEN", Json(Postlar("/hareket")[0].Govde).GetProperty("yon").GetString());
    }

    // [KRİTİK] K17: üç sayfada başlık altı açıklama satırı yok; hurda başlığı menü adıyla aynı.
    [Fact]
    public void ZO28_uc_sayfada_SAYFA_ACIKLAMASI_yok_hurda_basligi_menu_adi()
    {
        var b = BankaEkrani();
        Assert.Empty(b.FindAll("#sayfa-basi-banka-aciklama"));
        Assert.DoesNotContain(Sozlukler.Tr.Banka.SayfaAciklama, b.Markup);

        var p = PosEkrani();
        Assert.Empty(p.FindAll("#sayfa-basi-pos-tahsilatlar-aciklama"));

        HurdaYollari();
        var h = HurdaEkrani();
        Assert.Empty(h.FindAll("#sayfa-basi-hurda-aciklama"));
        Assert.Empty(h.FindAll("#hrd-bakiye-kaynak"));
        Assert.Equal(MenuAdlari.Icin(Sozlukler.Tr).HurdaRamat.Ad, h.Find("#sayfa-basi-hurda-baslik").TextContent.Trim());
        Assert.DoesNotContain(Sozlukler.Tr.Hurda.SayfaAciklama, h.Markup);
        Assert.DoesNotContain(Sozlukler.Tr.Hurda.BakiyeKaynagi, h.Markup);
    }

    // [KRİTİK] Hurda: geliştirici notu yok; alıcı kabulünde TEK sade cümle; tahsil aracı radyo.
    [Fact]
    public void ZO28_hurda_detayda_TEK_sade_ipucu_jargon_ve_fark_aciklamasi_yok()
    {
        HurdaYollari();
        var e = HurdaEkrani();
        e.Find("#hrd-detay-ac").Click();
        e.WaitForElement("#hrd-kabul-has");
        Gir(e, "#hrd-has", "1");
        Gir(e, "#hrd-kabul-has", "0,9");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#hrd-tahlil-fark")));
        Assert.Equal(ZoIslemSozlugu.Tr.HurdaKabulIpucu, e.Find("#hrd-kabul-notu").TextContent.Trim());
        Assert.DoesNotContain(Sozlukler.Tr.Hurda.AliciKabulNotu, e.Markup);
        Assert.DoesNotContain(Sozlukler.Tr.Hurda.TahlilFarkiNotu, e.Markup);
        Assert.DoesNotContain("bacağı", e.Markup);
        Assert.DoesNotContain("DEĞİLDİR", e.Markup);
        Assert.Equal("radiogroup", e.Find("#hrd-odeme").GetAttribute("role"));
        Assert.Equal(3, e.FindAll("#hrd-odeme input.secim-radyo").Count);
    }

    // [DÜŞÜK] İki şubeli kiracıda şubesiz müdür: bakiye kartı HİÇ çizilmez (gövdesiz başlık yok).
    [Fact]
    public void ZO28_hurda_sube_yokken_BAKIYE_KARTI_cizilmez_baska_hatada_cizilir()
    {
        Oturum("MUDUR", sube: null);
        Yol("/hurda/bakiye", """{"message":"Hurda işlemleri için şube bağlamı gerekli."}""", HttpStatusCode.BadRequest);
        var e = Render<Kuyumcu.Web.Pages.Hurda>().AyrintiAc();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#hrd-hata")));
        Assert.Empty(e.FindAll("#hrd-bakiye-kart"));
        Assert.DoesNotContain(Sozlukler.Tr.Hurda.BakiyeBasligi, e.Markup);
    }

    // [KRİTİK] POS kilit kutusu tek cümle; eşleştirme etiketi sade; kilitli düğmenin sebebi görünür.
    [Fact]
    public void ZO28_pos_kilit_TEK_cumle_etiket_sade_eslestir_kilidinin_SEBEBI_gorunur()
    {
        var e = PosEkrani();
        var kilit = e.Find("#degismez-ipucu-PosTahsilat");
        Assert.Contains(ZoIslemSozlugu.Tr.PosKilitNotu, kilit.TextContent);
        Assert.Empty(kilit.QuerySelectorAll(".degismez-alternatif"));
        Assert.DoesNotContain(Sozlukler.Tr.Pos.DefterAlternatif, e.Markup);
        Assert.DoesNotContain(Sozlukler.Tr.Pos.DefterGerekce, e.Markup);
        Assert.Contains(ZoIslemSozlugu.Tr.PosBankaYatan, e.Find("#pos-eslestir-hareket-sarmal").TextContent);
        Assert.DoesNotContain(Sozlukler.Tr.Pos.BankaHareketId, e.Markup);
        // Boşken iki düğme de kilitli ve SEBEP ekranda yazılı (sessiz değil).
        Assert.True(e.Find("#pos-oto-oneri").HasAttribute("disabled"));
        Assert.True(e.Find("#pos-eslestir-uygula").HasAttribute("disabled"));
        Assert.Equal(ZoIslemSozlugu.Tr.PosOnceHareketSec, e.Find("#pos-eslestir-kilit").TextContent.Trim());
    }

    // [KRİTİK] Yıkıcı eylem kırmızı DOLU değil; ortak "⋯" menüsünde; kilitliyse sebebi yazılı.
    [Fact]
    public void ZO28_pos_iptal_TEHLIKE_dolu_degil_satir_menusunde_kilitliyse_sebep_yazili()
    {
        Oturum();
        Yol("/pos/valor-takvimi", "[]");
        var yatti = PosSatir.Replace("\"id\":\"t1\"", "\"id\":\"t2\"").Replace("\"durum\":\"BEKLIYOR\"", "\"durum\":\"YATTI\"");
        Yol("/pos/tahsilatlar", "{\"satirlar\":[" + PosSatir + "," + yatti + "],\"toplamSatir\":2,\"limit\":50,\"offset\":0,\"azamiSatir\":200,\"sinirDegdi\":false}");
        Yol("/pos/cihazlar", "[]");
        Yol("/bankalar", """{"hesaplar":[]}""");
        var e = Render<Kuyumcu.Web.Pages.PosTahsilatlar>().AyrintiAc();
        e.WaitForElement("#pos-t-defter-kart-ac").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#pos-t-iptal-t1")));
        foreach (var id in new[] { "t1", "t2" })
        {
            var d = e.Find($"#pos-t-iptal-{id}");
            Assert.DoesNotContain("tehlike", d.ClassName ?? "");
            Assert.NotNull(d.Closest($"#pos-t-menu-{id}"));
        }
        Assert.False(e.Find("#pos-t-iptal-t1").HasAttribute("disabled"));
        Assert.True(e.Find("#pos-t-iptal-t2").HasAttribute("disabled"));
        Assert.Equal(ZoIslemSozlugu.Tr.PosIptalKilit, e.Find("#pos-t-iptal-kilit-t2").TextContent.Trim());
        Assert.Empty(e.FindAll("#pos-t-iptal-kilit-t1"));
        // Menüdeki iptal gerçek iş yapar: onay penceresi açılır.
        e.Find("#pos-t-iptal-t1").Click();
        e.WaitForElement("#pos-iptal-gerekce");
        Assert.Equal("radiogroup", e.Find("#pos-t-durum").GetAttribute("role"));
    }

    [Fact]
    public void ZO28_banka_KALDIR_tehlike_dolu_degil_menude_bakiyeliyse_SEBEP_yazili()
    {
        const string bakiyeli = """{"id":"b3","bankaAdi":"Ziraat","hesapAdi":"Ana","hesapSahibi":null,"iban":"TR630000000000000000000003","paraBirimi":"TRY","bakiye":1500,"aktif":true}""";
        var e = BankaEkrani(bakiyeli);
        var d = e.Find("#bh-pasife-al-b3");
        Assert.DoesNotContain("tehlike", d.ClassName ?? "");
        Assert.NotNull(d.Closest("#bh-menu-b3"));
        Assert.True(d.HasAttribute("disabled"));
        Assert.Equal(ZoIslemSozlugu.Tr.BankaKaldirKilit, e.Find("#bh-pasife-al-kilit-b3").TextContent.Trim());
        Assert.DoesNotContain("btn-tehlike", e.Find("#bh-dekont-gir").ParentElement!.InnerHtml);
    }

    [Fact]
    public void ZO28_banka_KALDIR_bakiyesiz_hesapta_ACIK_sebep_yok_onay_acilir()
    {
        var e = BankaEkrani(HesapSahipli);   // bakiye 0
        Assert.False(e.Find("#bh-pasife-al-b1").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#bh-pasife-al-kilit-b1"));
        e.Find("#bh-pasife-al-b1").Click();
        e.WaitForElement("#bh-pasif-onayla");
    }

    // [KRİTİK] Hareket tablosu: dar sütun sınıfları + tutar tek satır + eylemler ortak menüde.
    [Fact]
    public void ZO28_banka_hareket_tablosu_DAR_sutun_ve_eylemler_ortak_menude()
    {
        var e = BankaEkrani();
        e.WaitForElement("#bh-gecmis-ac").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("table.veri-tablosu.zo-banka-hareket")));
        var satir = e.Find("table.zo-banka-hareket tbody tr");
        var hucreler = satir.QuerySelectorAll("td");
        // 28.09 (3. doğrulama — K3 tek ölçü): karşı taraf + açıklama TEK hücrede alt alta.
        Assert.Equal(6, e.FindAll("table.zo-banka-hareket thead th").Count);
        Assert.Contains("zo-hucre-dar", hucreler[2].ClassList);
        Assert.Contains("zo-hucre-karsi", hucreler[2].ClassList);
        Assert.Single(hucreler[2].QuerySelectorAll(".zo-karsi-ad"));
        Assert.Single(hucreler[2].QuerySelectorAll(".zo-karsi-aciklama"));
        Assert.Contains("zo-hucre-tutar", hucreler[3].ClassList);
        // İlk eylem satırda, diğerleri "⋯" menüsünde; kimlikler korunur.
        Assert.Single(e.FindAll("#bh-h-eylem-bh1 #bh-eslestir-bh1"));
        Assert.NotNull(e.Find("#bh-fatura-bh1").Closest("#bh-h-menu-bh1"));
        Assert.NotNull(e.Find("#bh-storno-bh1").Closest("#bh-h-menu-bh1"));

        var css = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "zo-islem.css"));
        Assert.Matches(@"\.veri-tablosu\.zo-banka-hareket td\.zo-hucre-dar\s*\{[^}]*max-width:\s*14rem;[^}]*white-space:\s*normal;", css);
        Assert.Matches(@"td\.zo-hucre-tutar,[^{]*\{\s*white-space:\s*nowrap;", css);
        // Kısa ad (≤ 24 karakter) tek satır: birleşik hücrenin asgari genişliği.
        Assert.Matches(@"td\.zo-hucre-dar\.zo-hucre-karsi\s*\{\s*min-width:\s*15rem;", css);
        // 28.09 gerçek tarayıcı: uzun karşı taraf metni satır içi düzenleme düğmesinde (nowrap)
        // komşu hücreye biniyordu (K9) — dar hücrede düğme SARAR.
        Assert.Matches(@"td\.zo-hucre-dar \.si-tetik\s*\{[^}]*white-space:\s*normal;", css);
    }

    // ══ 28.09 (2. hakem turu) — başlık · Vazgeç · valör kartı · eşleştirme arama · MASAK ══

    // [DÜŞÜK] UX-S31: POS sayfa başlığı menü / üst bar adıyla AYNI; BASİT'te "hakediş" yok.
    [Fact]
    public void ZO28b_pos_basligi_MENU_adiyla_ayni_BASITte_hakedis_yok()
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => true);
        var p = PosEkrani();
        var baslik = p.Find("#sayfa-basi-pos-tahsilatlar-baslik").TextContent.Trim();
        Assert.Equal(MenuAdlari.Icin(Sozlukler.Tr).PosAlacaklari.Ad, baslik);
        Assert.DoesNotContain("akediş", baslik);
    }

    // [KRİTİK] Yıkıcı onay: solda "Vazgeç", sağda kırmızı eylem; vazgeç "İptal" adını taşımaz.
    [Fact]
    public void ZO28b_pos_iptal_onayi_VAZGEC_solda_kirmizi_eylem_sagda()
    {
        var e = PosEkrani();
        e.Find("#pos-t-defter-kart-ac").Click();
        e.WaitForElement("#pos-t-iptal-t1").Click();
        e.WaitForElement("#pos-iptal-gerekce");
        var vazgec = e.Find("#pos-iptal-vazgec");
        var onay = e.Find("#pos-iptal-onayla");
        Assert.Equal(ZoIslemSozlugu.Tr.Vazgec, vazgec.TextContent.Trim());
        var kardesler = onay.ParentElement!.Children;
        Assert.Equal(2, kardesler.Length);
        Assert.Equal("pos-iptal-vazgec", kardesler[0].Id);   // solda Vazgeç
        Assert.Equal("pos-iptal-onayla", kardesler[1].Id);   // sağda kırmızı eylem
        Assert.Contains("btn-tehlike", onay.ClassList);
        Assert.Contains("btn-ikincil", vazgec.ClassList);
        Assert.DoesNotContain(e.FindAll("button"), b => b.TextContent.Trim() == Sozlukler.Tr.Ortak.Iptal);
        vazgec.Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#pos-iptal-gerekce")));
        Assert.Empty(Postlar("/iptal"));
    }

    // [KRİTİK] Valör takvimi dar ekranda KART: KALAN her genişlikte görünür, hücreler etiketli.
    [Fact]
    public void ZO28b_pos_valor_DAR_ekranda_KART_KALAN_etiketli_ve_css_kurali()
    {
        Oturum();
        Yol("/pos/valor-takvimi", """[{"valorTarihi":"2026-09-25T00:00:00Z","posCihazId":"p1","cihazAd":"Garanti POS","bankaAdi":"Garanti","brut":1000,"komisyon":18.5,"net":981.5,"yatan":0,"kalan":981.5,"adet":1,"paraBirimi":"TRY"}]""");
        Yol("/pos/tahsilatlar", "{\"satirlar\":[],\"toplamSatir\":0,\"limit\":50,\"offset\":0,\"azamiSatir\":200,\"sinirDegdi\":false}");
        Yol("/pos/cihazlar", "[]");
        Yol("/bankalar", """{"hesaplar":[]}""");
        var e = Render<Kuyumcu.Web.Pages.PosTahsilatlar>().AyrintiAc();
        var tablo = e.WaitForElement("#pos-t-valor");
        Assert.Contains("zo-valor-tablo", tablo.ClassList);
        Assert.Equal("tablo-sar", tablo.ParentElement!.GetAttribute("class"));
        var kalan = e.Find("#pos-t-valor tbody td.zo-valor-kalan");
        Assert.Equal(Sozlukler.Tr.Pos.Kalan, kalan.GetAttribute("data-etiket"));
        Assert.Equal(Bicim.Para(981.5m, "TRY"), kalan.TextContent.Trim());
        // Tarih ve cihaz dışındaki HER tutar hücresi etiketini taşır (kart düzeninde başlık yok).
        var sayilar = e.FindAll("#pos-t-valor tbody td.sayi");
        Assert.Equal(5, sayilar.Count);
        Assert.All(sayilar, td => Assert.False(string.IsNullOrWhiteSpace(td.GetAttribute("data-etiket"))));

        var css = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "zo-islem.css"));
        var blok = System.Text.RegularExpressions.Regex.Match(css, @"@media \(max-width: 1100px\) \{(.*?)\n\}",
            System.Text.RegularExpressions.RegexOptions.Singleline).Groups[1].Value;
        // Kimlik seçici: olcek.css'in html[data-mod] .ana-icerik ... td kuralları (yazı boyu,
        // satır boyu) sınıf seçicisini eziyordu — gerçek tarayıcıda KALAN büyümüyordu.
        Assert.Matches(@"#pos-t-valor > thead\s*\{\s*display:\s*none;", blok);
        Assert.Matches(@"#pos-t-valor > tbody > tr\s*\{[^}]*display:\s*grid;", blok);
        Assert.Matches(@"#pos-t-valor > tbody > tr > td\s*\{[^}]*height:\s*auto;", blok);
        Assert.Matches(@"td\.zo-valor-kalan\s*\{[^}]*grid-column:\s*1 / -1;[^}]*font-size:\s*24px;", blok);
        Assert.Matches(@"td\[data-etiket\]::before\s*\{[^}]*content:\s*attr\(data-etiket\);", blok);
    }

    // [KRİTİK] Eşleştirme seçicisi: ayrı "Ara" düğmesi yok (yazdıkça arar); boş sonuçta
    // tutara özgü mesaj; seçici sonuç alt bilgisini sunucudan aynen basar.
    [Fact]
    public void ZO28b_pos_eslestir_AYRI_ara_dugmesi_yok_yazinca_arar_bos_sonucta_tutar_mesaji()
    {
        Yol("/pos/yatma-adaylari", """{"sorgu":"999","tur":"BANKA_HAREKET","limit":10,"ofset":0,"sayi":0,"dahaVar":false,"tavan":50,"sonuclar":[],"gozatma":false}""");
        var e = PosEkrani();
        Assert.Empty(e.FindAll("#pos-eslestir-hareket-ara"));
        e.Find("#pos-eslestir-hareket").Input("999");
        // KESIF-P1 · TOPLU ZO-ISLEM-A: boş sonuç metni sorgudan bağımsız ("Bu tutarda…" kutu boşken yanlıştı).
        e.WaitForAssertion(() => Assert.Contains(KesifP1Sozlugu.Tr.PosBankaYatmasiYok, e.Find("#pos-eslestir-hareket-sarmal").TextContent),
            TimeSpan.FromSeconds(3));
        lock (istekler) Assert.Contains(istekler, i => i.Yol.Contains("/pos/yatma-adaylari") && i.Yol.Contains("q=999"));
        Assert.DoesNotContain(Sozlukler.Tr.Kabuk.AramaSonucYok, e.Find("#pos-eslestir-hareket-sarmal").TextContent);
    }

    // [KRİTİK] Banka pencerelerinde vazgeç düğmesi "Vazgeç" (İptal değil).
    [Fact]
    public void ZO28b_banka_pencerelerinde_vazgec_dugmesi_VAZGEC()
    {
        var e = BankaEkrani();
        e.Find("#bh-dekont-gir").Click();
        e.WaitForElement("#dekont-kaydet");
        var vazgecler = e.FindAll("button").Where(b => b.TextContent.Trim() == ZoIslemSozlugu.Tr.Vazgec).ToList();
        Assert.NotEmpty(vazgecler);
        Assert.DoesNotContain(e.FindAll("button"), b => b.TextContent.Trim() == Sozlukler.Tr.Ortak.Iptal);
        vazgecler[0].Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#dekont-kaydet")));

        e.Find("#bh-pasife-al-b1").Click();
        e.WaitForElement("#bh-pasif-onayla");
        Assert.Equal(ZoIslemSozlugu.Tr.Vazgec, e.Find("#bh-pasif-vazgec").TextContent.Trim());
    }

    // 28.09 (doğrulayıcı DÜŞÜK): dekont "Yön" sunucuda [Required] — ekranda "(isteğe bağlı)"
    // yazmaz, zorunlu işareti (*) taşır ve aria-required="true".
    [Fact]
    public void ZO28c_dekont_YON_zorunlu_isaretli_istege_bagli_yazmaz()
    {
        var e = BankaEkrani();
        e.Find("#bh-dekont-gir").Click();
        e.WaitForElement("#dekont-yon");
        var sar = e.Find("#dekont-yon-sar");
        Assert.Empty(sar.QuerySelectorAll(".alan-istege"));
        Assert.Single(sar.QuerySelectorAll(".zorunlu-isaret"));
        Assert.Equal("true", e.Find("#dekont-yon").GetAttribute("aria-required"));
    }

    // [KRİTİK] MASAK dekontu: kilit kutusu TEK cümle ("Düzeltme şöyle yapılır" yok), müşteri
    // ipucu uyarı kutusuyla İKİNCİ KEZ yazılmaz, BASİT'te jargonsuz başlık ve kilit sebebi.
    [Fact]
    public void ZO28b_banka_MASAK_kilit_TEK_cumle_uyari_TEK_yerde_BASITte_jargonsuz()
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => true);
        var e = BankaEkrani();
        e.Find("#bh-dekont-gir").Click();
        e.WaitForElement("#dekont-detay-ac").Click();
        e.WaitForElement("#dekont-cari");
        Assert.Equal(ZoIslemSozlugu.Tr.BankaMusteriIpucuBasit, e.Find("#dekont-cari-ipucu").TextContent.Trim());

        e.Find("#dekont-tutar").Input("185000");
        var kutu = e.WaitForElement("#degismez-ipucu-MasakKimlikTespiti");
        Assert.Contains(ZoIslemSozlugu.Tr.BankaMasakKilit, kutu.TextContent);
        Assert.Empty(kutu.QuerySelectorAll(".degismez-alternatif"));
        Assert.DoesNotContain("8 yıl", kutu.TextContent);
        // Uyarı tek yerde: kutu var, alan altı ipucu YOK.
        Assert.Single(e.FindAll("#dekont-masak-cari-uyari"));
        Assert.Empty(e.FindAll("#dekont-cari-ipucu"));
        Assert.Equal(string.Format(YasalParamSozlugu.Tr.BaslikKimlik, Bicim.Tl(185_000m)), e.Find(".kart--masak h3").TextContent.Trim());   // YASAL-PARAM-EKRAN tur 2: tetikleyen kademe, iki modda aynı
        Assert.Equal(ZoIslemSozlugu.Tr.MasakKilitSebepBasit, e.Find("#dekont-kaydet-sebep").TextContent.Trim());
        Assert.DoesNotContain("eşiğ", e.Find(".modal-panel").TextContent);
        // 3. tur gerçek tarayıcı (K7): pencere içinde MASAK alanları TEK sütun.
        Assert.Single(e.FindAll(".zo-masak-dikey > .kart--masak"));
        var css = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "zo-islem.css"));
        Assert.Matches(@"\.zo-masak-dikey \.kart--masak \.form-grid-3\s*\{\s*grid-template-columns:\s*minmax\(0, 1fr\);", css);
    }

    // ══ 28.09 (4. doğrulama — tasarım hakemi KRİTİK) ════════════════════════

    private const string BankaSonuc =
        """{"sorgu":"103","tur":"BANKA_HAREKET","limit":10,"ofset":0,"sayi":1,"dahaVar":false,"tavan":50,"sonuclar":[{"kimlik":"bh9","ad":"+981,50 ₺","altBilgi":"Garanti · Ana · 28.09.2026","tur":"BANKA_HAREKET","rota":null}],"gozatma":false}""";

    private IRenderedComponent<Kuyumcu.Web.Pages.PosTahsilatlar> PosHareketSecili(string oneriGovde)
    {
        Yol("/pos/yatma-adaylari", BankaSonuc);
        Yol("/pos/oto-eslestir-onerisi", oneriGovde);
        Yol("/pos/eslestir", """{"mesaj":"Eşleştirme tamamlandı","bankaHareketId":"bh9","eslesenToplam":981.5,"satirSayisi":1}""");
        var e = PosEkrani();
        e.Find("#pos-eslestir-hareket").Input("103");
        e.WaitForElement("#pos-eslestir-hareket-sonuc-0", TimeSpan.FromSeconds(3)).Click();
        e.WaitForAssertion(() => Assert.False(e.Find("#pos-oto-oneri").HasAttribute("disabled")));
        e.Find("#pos-oto-oneri").Click();
        e.WaitForElement("#pos-elle");
        return e;
    }

    // ══ 29.09 (tasarım hakemi t3 KRİTİK): POS tahsilatı ilgisiz hesaptaki havaleyle eşleşiyordu ══
    // Seçici genel /arama'yı DEĞİL, yalnız POS hesaplarındaki açık hareketleri döndüren
    // /pos/yatma-adaylari ucunu sorgular; açılışta yazmadan listeler; sonuç hesap adını ve
    // kalanı taşır. Elle seçim listesi yalnız hareketin hesabına yatan cihazların satırları.
    [Fact]
    public void ZO29l_pos_secici_YALNIZ_yatma_adaylari_hesap_adi_kalan_elle_liste_hesabin_cihazlari()
    {
        const string ikinci =
            """{"id":"t2","posCihazId":"p2","cihazAd":"Ziraat POS","bankaAdi":"Ziraat","brutTutar":2000,"komisyonOran":0.0185,"komisyonTutar":37,"netTutar":1963,"yatanTutar":0,"kalanTutar":1963,"taksitAdedi":1,"paraBirimi":"TRY","islemTarihi":"2026-09-24T10:00:00Z","valorTarihi":"2026-09-25T00:00:00Z","durum":"BEKLIYOR","iptalGerekcesi":null}""";
        Yol("/pos/tahsilatlar", "{\"satirlar\":[" + PosSatir + "," + ikinci + "],\"toplamSatir\":2,\"limit\":50,\"offset\":0,\"azamiSatir\":200,\"sinirDegdi\":false}");
        Yol("/pos/yatma-adaylari",
            """{"sorgu":"","tur":"BANKA_HAREKET","limit":10,"ofset":0,"sayi":1,"dahaVar":false,"tavan":50,"sonuclar":[{"kimlik":"bh9","ad":"+26.950 ₺","altBilgi":"Garanti · Ana · 28.09.2026 · kalan 11.950 ₺","tur":"BANKA_HAREKET","rota":null}],"gozatma":true}""");
        Yol("/pos/oto-eslestir-onerisi",
            """{"bulundu":false,"bankaHareketId":"bh9","toplam":0,"satirlar":[],"mesaj":"x","hareketKalan":11950,"uygunCihazlar":["p1"]}""");
        var e = PosEkrani();

        // Yazmadan listelenir; genel aramaya banka hareketi sorusu gitmez.
        var sonuc = e.WaitForElement("#pos-eslestir-hareket-sonuc-0", TimeSpan.FromSeconds(3));
        Assert.Contains("Garanti · Ana", sonuc.TextContent);
        Assert.Contains("kalan 11.950 ₺", sonuc.TextContent);
        lock (istekler)
        {
            Assert.Contains(istekler, i => i.Yol.Contains("/pos/yatma-adaylari?dil=tr&q=&limit=10"));
            Assert.DoesNotContain(istekler, i => i.Yol.Contains("/arama") && i.Yol.Contains("BANKA_HAREKET"));
        }

        sonuc.Click();
        e.WaitForAssertion(() => Assert.False(e.Find("#pos-oto-oneri").HasAttribute("disabled")));
        e.Find("#pos-oto-oneri").Click();
        e.WaitForElement("#pos-elle");
        Assert.Single(e.FindAll("#pos-elle-t1"));
        Assert.Empty(e.FindAll("#pos-elle-t2"));   // başka hesaba yatan cihazın satırı seçilemez
    }

    // [KRİTİK] Öneri bulunamayınca mesaj TEK kez ve gerçek yolu söyler; bekleyen tahsilat
    // işaretlenerek ELLE eşleştirilir ve POST gövdesine işaretli satır + kalan tutarı girer.
    [Fact]
    public void ZO28e_pos_oneri_YOKSA_tek_mesaj_ELLE_isaretle_eslestir_POSTa_girer()
    {
        var e = PosHareketSecili("""{"bulundu":false,"bankaHareketId":"bh9","toplam":0,"satirlar":[],"mesaj":"x","hareketKalan":981.5}""");
        Assert.Equal(ZoIslemSozlugu.Tr.PosOneriYokElle, e.Find("#pos-oneri-yok").TextContent.Trim());
        Assert.DoesNotContain(Sozlukler.Tr.Pos.OneriYok, e.Markup);
        Assert.Empty(e.FindAll("#pos-eslestir-kilit"));      // aynı yol ikinci kez yazılmaz
        Assert.True(e.Find("#pos-eslestir-uygula").HasAttribute("disabled"));
        Assert.Single(e.FindAll("#pos-elle-t1"));             // BEKLIYOR + KISMI yanıtında aynı satır → tek kutu
        lock (istekler)
        {
            Assert.Contains(istekler, i => i.Yol.Contains("/pos/tahsilatlar") && i.Yol.Contains("durum=BEKLIYOR"));
            Assert.Contains(istekler, i => i.Yol.Contains("/pos/tahsilatlar") && i.Yol.Contains("durum=KISMI"));
        }

        e.Find("#pos-elle-t1").Change(true);
        e.WaitForAssertion(() => Assert.False(e.Find("#pos-eslestir-uygula").HasAttribute("disabled")));
        Assert.Contains(string.Format(ZoIslemSozlugu.Tr.PosElleSecilen, Bicim.Para(981.5m, "TRY"), Bicim.Para(981.5m, "TRY")),
            e.Find("#pos-elle-toplam").TextContent);
        e.Find("#pos-eslestir-uygula").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/pos/eslestir")));
        var g = Json(Postlar("/pos/eslestir")[0].Govde);
        Assert.Equal("bh9", g.GetProperty("bankaHareketId").GetString());
        var satirlar = g.GetProperty("satirlar");
        Assert.Equal(1, satirlar.GetArrayLength());
        Assert.Equal("t1", satirlar[0].GetProperty("posTahsilatId").GetString());
        Assert.Equal(981.5m, satirlar[0].GetProperty("tutar").GetDecimal());
        e.WaitForAssertion(() => Assert.Contains("Eşleştirme tamamlandı", e.Find("#pos-t-basari").TextContent));
    }

    // [KRİTİK] Seçilen toplam bankaya yatanın kalanını aşarsa düğme kilitli ve SEBEBİ yazılı (UX-S30).
    // 28.09 (5. doğrulama): hareketin kalanı 0 < satır → kısmi eşleşme mümkün değil, satır
    // tam kalanıyla seçilir ve fazla yazılır (kalan > 0 iken kısmi seçim: ZO28m).
    [Fact]
    public void ZO28e_pos_elle_secim_FAZLAYSA_kilitli_sebep_yazili()
    {
        var e = PosHareketSecili("""{"bulundu":false,"bankaHareketId":"bh9","toplam":0,"satirlar":[],"mesaj":"x","hareketKalan":0}""");
        e.Find("#pos-elle-t1").Change(true);
        e.WaitForAssertion(() => Assert.Equal(
            string.Format(ZoIslemSozlugu.Tr.PosElleFazla, Bicim.Para(981.5m, "TRY")),
            e.Find("#pos-eslestir-kilit").TextContent.Trim()));
        Assert.True(e.Find("#pos-eslestir-uygula").HasAttribute("disabled"));
        Assert.Empty(Postlar("/pos/eslestir"));
    }

    // [DÜŞÜK, 5. doğrulama] Hareketin kalanı satırın kalanından KÜÇÜKSE satır KISMEN eşleşir:
    // kısmi tutar satırın altında yazılır, düğme açılır, POST gövdesine kalan kadar tutar girer.
    [Fact]
    public void ZO28m_pos_elle_hareket_kalani_KUCUKSE_kismi_tutarla_eslestirir()
    {
        var e = PosHareketSecili("""{"bulundu":false,"bankaHareketId":"bh9","toplam":0,"satirlar":[],"mesaj":"x","hareketKalan":500}""");
        Assert.Empty(e.FindAll("#pos-elle-kismi-t1"));
        e.Find("#pos-elle-t1").Change(true);
        e.WaitForAssertion(() => Assert.False(e.Find("#pos-eslestir-uygula").HasAttribute("disabled")));
        Assert.Equal(string.Format(ZoIslemSozlugu.Tr.PosElleKismi, Bicim.Para(500m, "TRY")),
            e.Find("#pos-elle-kismi-t1").TextContent.Trim());
        Assert.Empty(e.FindAll("#pos-eslestir-kilit"));
        e.Find("#pos-eslestir-uygula").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/pos/eslestir")));
        var s = Json(Postlar("/pos/eslestir")[0].Govde).GetProperty("satirlar");
        Assert.Equal(1, s.GetArrayLength());
        Assert.Equal("t1", s[0].GetProperty("posTahsilatId").GetString());
        Assert.Equal(500m, s[0].GetProperty("tutar").GetDecimal());
    }

    // Öneri bulunduysa önerilen satırlar İŞARETLİ gelir; gövde önerinin tutarını taşır.
    [Fact]
    public void ZO28e_pos_oneri_VARSA_satirlar_isaretli_gelir()
    {
        var e = PosHareketSecili("""{"bulundu":true,"bankaHareketId":"bh9","toplam":981.5,"hareketKalan":981.5,"satirlar":[{"posTahsilatId":"t1","tutar":981.5,"valorTarihi":"2026-09-25T00:00:00Z","posCihazId":"p1"}]}""");
        Assert.True(e.Find("#pos-elle-t1").HasAttribute("checked"));
        Assert.Empty(e.FindAll("#pos-oneri-yok"));
        Assert.False(e.Find("#pos-eslestir-uygula").HasAttribute("disabled"));
    }

    private const string TevsikAsildi =
        """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":0,"islemTutariKuraliDevrede":true}""";

    // [KRİTİK] Tevsik duvarı: tutar nakit sınırını aşınca tahsil aracı AÇILIŞTA görünür,
    // Nakit seçiliyken altında tek sade cümle; Havale seçilince cümle kalkar ve gövdeye girer.
    // Eşik SAYISI istemcide yok: sunucunun önizlemesi (fisToplamTl) sorulur.
    [Fact]
    public void ZO28e_hurda_nakit_SINIRI_asinca_tahsil_araci_acilista_tek_sade_cumle()
    {
        Yol("/tevsik/gunluk", TevsikAsildi);
        HurdaYollari();
        var e = HurdaEkrani();
        Assert.Empty(e.FindAll("#hrd-odeme"));               // tutar yokken sorulmaz
        Gir(e, "#hrd-has", "9");                  // bakiye 9,16 g: sınır içinde
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#hrd-odeme")));
        Assert.Empty(e.FindAll("#hrd-detay"));                // "Daha fazla" açılmadan
        Assert.Equal(ZoIslemSozlugu.Tr.HurdaNakitAlinamaz, e.Find("#hrd-nakit-uyari").TextContent.Trim());
        lock (istekler) Assert.Contains(istekler, i => i.Yol.Contains("/tevsik/gunluk?fisToplamTl=61200"));

        e.Find("#hrd-odeme input[value='HAVALE']").Change(true);
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hrd-nakit-uyari")));
        Assert.Empty(e.FindAll("#hrd-detay"));                // tercih dışarıda; detay zorla açılmaz
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/islemler/hurda-satis")));
        Assert.Equal("HAVALE", Json(Postlar("/islemler/hurda-satis")[0].Govde).GetProperty("odemeTuru").GetString());
    }

    // Sınırın altında tahsil aracı açılışta sorulmaz (zorunlu önce) — "Daha fazla" içinde kalır.
    [Fact]
    public void ZO28e_hurda_nakit_SINIR_ALTINDA_tahsil_araci_gizli()
    {
        Yol("/tevsik/gunluk", """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""");
        HurdaYollari();
        var e = HurdaEkrani();
        Gir(e, "#hrd-has", "1");
        e.WaitForAssertion(() => { lock (istekler) Assert.Contains(istekler, i => i.Yol.Contains("/tevsik/gunluk")); });
        Assert.Empty(e.FindAll("#hrd-odeme"));
        Assert.Empty(e.FindAll("#hrd-nakit-uyari"));
    }

    // 28.09 7. tur: ret metni sayfada TEK kez. 29.09 (K18): önizleme nakit sınırını gösterirken Kaydet
    // artık gerekçeli kilitli (ZO29b); burada önizleme sınırı GÖSTERMEZKEN (bağlantılı toplamı yalnız
    // sunucu bilir) gelen ret: pencerede sade cümle; pencere kapanınca tahsil aracı açılışta ZORUNLU
    // görünür ve cümle alanın altında TEK kez durur (banda ikinci kez yazılmaz), Kaydet gerekçeli kilitli.
    [Fact]
    public void ZO28f_hurda_ret_ALAN_UYARISIYLA_ayniysa_banda_tekrar_yazilmaz()
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => true);
        Yol("/tevsik/gunluk", """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":900000,"islemTutariKuraliDevrede":false}""");
        Yol("/islemler/hurda-satis", """{"message":"Tevsik zorunluluğu: işlemin toplam tutarı 61.200,00 TL, 30.000,00 TL haddini aşıyor (459 s. VUK Genel Tebliği §4.1.2)"}""",
            HttpStatusCode.BadRequest);
        HurdaYollari();
        var e = HurdaEkrani();
        Gir(e, "#hrd-has", "9");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hrd-nakit-uyari")));
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Contains(ZoIslemSozlugu.Tr.HurdaNakitAlinamaz, e.Find("#hrd-onay-hata").TextContent));
        e.Find("#hrd-onay-vazgec").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hrd-onay-hata")));
        Assert.Empty(e.FindAll("#hrd-hata"));
        Assert.Equal(ZoIslemSozlugu.Tr.HurdaNakitAlinamaz, e.Find("#hrd-nakit-uyari").TextContent.Trim());
        Assert.True(e.Find("#hrd-kaydet").HasAttribute("disabled"));
        Assert.Equal(ZoIslemSozlugu.Tr.HurdaTahsilSecinKilit, e.Find("#hrd-kaydet-kilit").TextContent.Trim());
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(e.Markup, System.Text.RegularExpressions.Regex.Escape(ZoIslemSozlugu.Tr.HurdaNakitAlinamaz)));
        // 28.09 (r10): "459" tek başına rastgele modal kimliğinde (GUID onaltılık) çıkabiliyordu → kararsız.
        Assert.DoesNotContain("459 s.", e.Markup);
        Assert.DoesNotContain("Tebliğ", e.Markup);
    }

    // [KRİTİK] Ret metni TEK yerde (önce pencerede, kapatınca bantta); BASİT'te mevzuat atfı yok.
    [Fact]
    public void ZO28e_hurda_ret_metni_TEK_yerde_BASITte_mevzuat_atfi_yok()
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => true);
        Yol("/islemler/hurda-satis", """{"message":"Tevsik zorunluluğu: işlemin toplam tutarı 6.800,00 TL, 5.000,00 TL haddini aşıyor (459 s. VUK Genel Tebliği §4.1.2; dayanak: VUK mükerrer md.257)"}""",
            HttpStatusCode.BadRequest);
        HurdaYollari();
        var e = HurdaEkrani();
        Gir(e, "#hrd-has", "1");
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Contains(ZoIslemSozlugu.Tr.HurdaNakitAlinamaz, e.Find("#hrd-onay-hata").TextContent));
        // 28.09 (r10): "459" tek başına rastgele modal kimliğinde (GUID onaltılık) çıkabiliyordu → kararsız.
        Assert.DoesNotContain("459 s.", e.Markup);
        Assert.DoesNotContain("Tebliğ", e.Markup);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(e.Markup, System.Text.RegularExpressions.Regex.Escape(ZoIslemSozlugu.Tr.HurdaNakitAlinamaz)));

        e.Find("#hrd-onay-vazgec").Click();
        // 29.09 (K18): pencere kapanınca cümle yapılacak işin yanında — tahsil aracının altında (bant boş).
        e.WaitForAssertion(() => Assert.Equal(ZoIslemSozlugu.Tr.HurdaNakitAlinamaz, e.Find("#hrd-nakit-uyari").TextContent.Trim()));
        Assert.Empty(e.FindAll("#hrd-hata"));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(e.Markup, System.Text.RegularExpressions.Regex.Escape(ZoIslemSozlugu.Tr.HurdaNakitAlinamaz)));
    }

    // [KRİTİK + ORTA] Banka hareketleri ve POS defteri 1100 px altında KART: başlık gizli,
    // tutar büyük, eylem satırın altında; hücreler kart sınıflarını taşır.
    [Fact]
    public void ZO28e_banka_ve_pos_defteri_dar_ekranda_KART_kalibi()
    {
        var css = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "zo-islem.css"));
        Assert.Matches(@"#bh-hareket-tablo > thead, #pos-t-defter-tablo > thead \{ display: none; \}", css);
        Assert.Matches(@"#bh-hareket-tablo > tbody > tr, #pos-t-defter-tablo > tbody > tr \{\s*display: grid;", css);
        Assert.Matches(@"td\.zo-k-eylem \{\s*grid-column: 1 / -1;", css);

        var p = PosEkrani();
        p.Find("#pos-t-defter-kart-ac").Click();
        p.WaitForElement("#pos-t-defter-tablo");
        var h = p.FindAll("#pos-t-defter-tablo > tbody > tr > td");
        Assert.Contains(h, td => td.ClassList.Contains("zo-k-tutar") && td.GetAttribute("data-etiket") == Sozlukler.Tr.Pos.Kalan);
        Assert.Contains(h, td => td.ClassList.Contains("zo-k-eylem"));
        Assert.Contains(h, td => td.ClassList.Contains("zo-k-durum"));
    }

    [Fact]
    public void ZO28e_banka_hareket_hucreleri_KART_siniflari_ve_375_sekme_adi()
    {
        var e = BankaEkrani();
        e.WaitForElement("#bh-gecmis-ac").Click();
        e.WaitForElement("#bh-hareket-tablo");
        var hucreler = e.Find("#bh-hareket-tablo tbody tr").QuerySelectorAll("td");
        string[] beklenen = ["zo-k-bas", "zo-k-yon", "zo-k-ad", "zo-k-tutar", "zo-k-durum", "zo-k-eylem"];
        for (var i = 0; i < beklenen.Length; i++) Assert.Contains(beklenen[i], hucreler[i].ClassList);
        Assert.Contains("zo-kart-liste", e.Find("#banka-hesap-liste-sar").ParentElement!.ClassList);
        Assert.NotEmpty(e.FindAll(".zo-banka-sekmeler > .sekme > .zo-sekme-ad"));
    }

    // ══ 28.09 (7. doğrulama) — eşleşmeyi EKRANDAN geri alma · düğme yeri · etiketli sonuç ══

    private const string EslesmisSatir =
        """{"id":"t3","posCihazId":"p1","cihazAd":"Garanti POS","bankaAdi":"Garanti","brutTutar":1000,"komisyonOran":0.0185,"komisyonTutar":18.5,"netTutar":981.5,"yatanTutar":981.5,"kalanTutar":0,"taksitAdedi":1,"paraBirimi":"TRY","islemTarihi":"2026-09-24T10:00:00Z","valorTarihi":"2026-09-25T00:00:00Z","durum":"YATTI","iptalGerekcesi":null}""";

    private const string GeriAlGerekcesi = "Yanlış banka hareketiyle eşleştirildi, düzeltiliyor";

    /// <summary>Eşleşmiş (YATTI) tahsilatlı POS ekranı; eşleşme listesi yolu defter yolundan ÖNCE kaydedilir
    /// (sahte API "içerir" ile eşler, ilk eşleşen kazanır).</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.PosTahsilatlar> PosEslesmisEkran(
        string rol, string eslesmeler, HttpStatusCode eslesmeKod = HttpStatusCode.OK)
    {
        Oturum(rol);
        Yol("/pos/tahsilatlar/t3/eslesmeler", eslesmeler, eslesmeKod);
        Yol("/geri-al", """{"mesaj":"Eşleşme geri alındı","geriAlindi":true,"posTahsilatId":"t3"}""");
        Yol("/pos/valor-takvimi", "[]");
        Yol("/pos/tahsilatlar", "{\"satirlar\":[" + EslesmisSatir + "],\"toplamSatir\":1,\"limit\":50,\"offset\":0,\"azamiSatir\":200,\"sinirDegdi\":false}");
        Yol("/pos/cihazlar", "[]");
        Yol("/bankalar", """{"hesaplar":[]}""");
        var e = Render<Kuyumcu.Web.Pages.PosTahsilatlar>().AyrintiAc();
        e.WaitForElement("#pos-t-defter-kart-ac").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#pos-t-iptal-t3")));
        return e;
    }

    private const string TekEtkin =
        """{"satirlar":[{"id":"e2","bankaHareketId":"bh9","eslesenTutar":981.5,"otomatik":false,"geriAlindi":false,"geriAlmaGerekcesi":null,"olusturuldu":"2026-09-28T10:00:00Z"},{"id":"e1","bankaHareketId":"bh8","eslesenTutar":981.5,"otomatik":true,"geriAlindi":true,"geriAlmaGerekcesi":"eski","olusturuldu":"2026-09-27T10:00:00Z"}]}""";

    // [KRİTİK] Eşleşmiş satırın "⋯" menüsünde "Eşleşmeyi geri al" var; iptal kilidi doğru yolu söyler;
    // onay penceresi etkin eşleşmeyi (geri alınmışı DEĞİL) gösterir, gerekçe ≥ 20 ister, POST'a girer,
    // sonuç banda yazılır ve liste yeniden yüklenir.
    [Fact]
    public void ZO28n_pos_eslesmis_satirda_GERI_AL_menude_gerekceli_onay_POSTa_girer()
    {
        var e = PosEslesmisEkran("MUDUR", TekEtkin);
        var d = e.Find("#pos-t-geri-al-t3");
        Assert.NotNull(d.Closest("#pos-t-menu-t3"));
        Assert.False(d.HasAttribute("disabled"));
        Assert.Equal(ZoIslemSozlugu.Tr.PosGeriAl, d.TextContent.Trim());
        Assert.Empty(e.FindAll("#pos-t-geri-al-kilit-t3"));
        Assert.True(e.Find("#pos-t-iptal-t3").HasAttribute("disabled"));
        Assert.Equal(ZoIslemSozlugu.Tr.PosIptalKilitEslesmis, e.Find("#pos-t-iptal-kilit-t3").TextContent.Trim());
        Assert.Contains("Eşleşmeyi geri al", ZoIslemSozlugu.Tr.PosKilitNotu);
        Assert.Contains(ZoIslemSozlugu.Tr.PosKilitNotu, e.Find("#degismez-ipucu-PosTahsilat").TextContent);

        d.Click();
        e.WaitForElement("#pos-geri-al-gerekce");
        lock (istekler) Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("/pos/tahsilatlar/t3/eslesmeler"));
        // Tek etkin eşleşme → seçici çizilmez, değer salt okunur satır (UX-S22); geri alınmış e1 listede YOK.
        Assert.Contains(Bicim.Para(981.5m, "TRY"), e.Find("#pos-geri-al-secim-tek").TextContent);
        Assert.Empty(e.FindAll("#pos-geri-al-secim-secenek-0"));
        var onay = e.Find("#pos-geri-al-onayla");
        Assert.True(onay.HasAttribute("disabled"));
        Assert.Contains("btn-tehlike", onay.ClassList);
        Assert.Equal("pos-geri-al-vazgec", onay.ParentElement!.Children[0].Id);
        Assert.Contains("zo-onay-dugmeler", onay.ParentElement!.ClassList);
        Assert.Equal(string.Format(ZoIslemSozlugu.Tr.PosGeriAlGerekceKisa, 0), e.Find("#pos-geri-al-kilit").TextContent.Trim());

        // Kısa gerekçe: alan altı Türkçe mesaj + kilit (sessiz etkisizlik yok).
        e.Find("#pos-geri-al-gerekce").Input("kısa");
        e.WaitForAssertion(() => Assert.Equal(string.Format(ZoIslemSozlugu.Tr.PosGeriAlGerekceKisa, 4),
            e.Find("#pos-geri-al-gerekce-hata").TextContent.Trim()));
        Assert.True(e.Find("#pos-geri-al-onayla").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#pos-geri-al-kilit"));   // aynı cümle iki kez yazılmaz
        Assert.Empty(Postlar("/geri-al"));

        e.Find("#pos-geri-al-gerekce").Input(GeriAlGerekcesi);
        e.WaitForAssertion(() => Assert.False(e.Find("#pos-geri-al-onayla").HasAttribute("disabled")));
        Assert.Empty(e.FindAll("#pos-geri-al-kilit"));
        int once;
        lock (istekler) once = istekler.Count(i => i.Metot == "GET" && i.Yol.Contains("/pos/tahsilatlar") && !i.Yol.Contains("/eslesmeler"));
        Assert.True(once > 0);
        e.Find("#pos-geri-al-onayla").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/geri-al")));
        var post = Postlar("/geri-al")[0];
        Assert.Contains("/pos/eslesmeler/e2/geri-al", post.Yol);
        Assert.Equal(GeriAlGerekcesi, Json(post.Govde).GetProperty("gerekce").GetString());
        e.WaitForAssertion(() => Assert.Equal(
            string.Format(ZoIslemSozlugu.Tr.PosGeriAlSonuc, "Eşleşme geri alındı", Bicim.Para(981.5m, "TRY")),
            e.Find("#pos-t-basari").TextContent.Trim()));
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#pos-geri-al-gerekce")));
        e.WaitForAssertion(() =>
        {
            lock (istekler) Assert.True(istekler.Count(i => i.Metot == "GET" && i.Yol.Contains("/pos/tahsilatlar") && !i.Yol.Contains("/eslesmeler")) > once);
        });
    }

    // KESIF-P1 · TOPLU ZO-ISLEM-A [YÜKSEK]: "Eşleşmeyi geri al" başarılı olunca eşleştirme
    // seçicisinin aday listesi YENİDEN çekilir (serbest kalan yatma görünsün); eskiden seçici
    // önbellekteki listeyi gösteriyor, F5 gerekiyordu.
    [Fact]
    public void KesifP1_pos_geri_al_sonrasi_aday_listesi_yeniden_cekilir()
    {
        var e = PosEslesmisEkran("MUDUR", TekEtkin);
        int Aday() { lock (istekler) return istekler.Count(i => i.Metot == "GET" && i.Yol.Contains("/pos/yatma-adaylari")); }
        e.WaitForAssertion(() => Assert.True(Aday() >= 1));
        var once = Aday();
        e.Find("#pos-t-geri-al-t3").Click();
        e.WaitForElement("#pos-geri-al-gerekce").Input(GeriAlGerekcesi);
        e.WaitForAssertion(() => Assert.False(e.Find("#pos-geri-al-onayla").HasAttribute("disabled")));
        e.Find("#pos-geri-al-onayla").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/geri-al")));
        e.WaitForAssertion(() => Assert.True(Aday() > once), TimeSpan.FromSeconds(3));
    }

    // [KRİTİK] Birden çok etkin eşleşme: seçim ZORUNLU; seçilmeden düğme kilitli ve sebep yazılı;
    // seçilen eşleşmenin kimliği POST yoluna girer. Vazgeç istek atmaz.
    [Fact]
    public void ZO28n_pos_iki_etkin_eslesmede_SECIM_zorunlu_secilen_POSTa_girer()
    {
        const string iki = """{"satirlar":[{"id":"e2","bankaHareketId":"bh9","eslesenTutar":500,"otomatik":false,"geriAlindi":false,"geriAlmaGerekcesi":null,"olusturuldu":"2026-09-28T10:00:00Z"},{"id":"e3","bankaHareketId":"bh7","eslesenTutar":481.5,"otomatik":true,"geriAlindi":false,"geriAlmaGerekcesi":null,"olusturuldu":"2026-09-27T10:00:00Z"}]}""";
        var e = PosEslesmisEkran("SAHIP", iki);
        e.Find("#pos-t-geri-al-t3").Click();
        e.WaitForElement("#pos-geri-al-secim-secenek-1");
        Assert.Equal("radiogroup", e.Find("#pos-geri-al-secim").GetAttribute("role"));
        Assert.Contains(ZoIslemSozlugu.Tr.PosEslesmeOneriyle, e.Find("#pos-geri-al-secim").TextContent);
        Assert.Contains(ZoIslemSozlugu.Tr.PosEslesmeElle, e.Find("#pos-geri-al-secim").TextContent);
        e.Find("#pos-geri-al-gerekce").Input(GeriAlGerekcesi);
        e.WaitForAssertion(() => Assert.Equal(ZoIslemSozlugu.Tr.PosGeriAlSecinKilit, e.Find("#pos-geri-al-kilit").TextContent.Trim()));
        Assert.True(e.Find("#pos-geri-al-onayla").HasAttribute("disabled"));

        // Vazgeç: pencere kapanır, istek yok; yeniden açılınca seçim ve gerekçe sıfırdır.
        e.Find("#pos-geri-al-vazgec").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#pos-geri-al-gerekce")));
        Assert.Empty(Postlar("/geri-al"));
        e.Find("#pos-t-geri-al-t3").Click();
        e.WaitForElement("#pos-geri-al-secim-secenek-1");
        Assert.Equal("", e.Find("#pos-geri-al-gerekce").TextContent);

        e.Find("#pos-geri-al-secim-secenek-1").Change("e3");
        e.Find("#pos-geri-al-gerekce").Input(GeriAlGerekcesi);
        e.WaitForAssertion(() => Assert.False(e.Find("#pos-geri-al-onayla").HasAttribute("disabled")));
        e.Find("#pos-geri-al-onayla").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/geri-al")));
        Assert.Contains("/pos/eslesmeler/e3/geri-al", Postlar("/geri-al")[0].Yol);
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(481.5m, "TRY"), e.Find("#pos-t-basari").TextContent));
    }

    // [KRİTİK] Sunucu geri almayı yalnız SAHİP/MÜDÜR'e açar: MUHASEBE'de eylem kilitli, sebep menüde YAZILI.
    [Fact]
    public void ZO28n_pos_MUHASEBE_geri_al_KILITLI_sebep_yazili()
    {
        var e = PosEslesmisEkran("MUHASEBE", TekEtkin);
        Assert.True(e.Find("#pos-t-geri-al-t3").HasAttribute("disabled"));
        Assert.Equal(ZoIslemSozlugu.Tr.PosGeriAlYetki, e.Find("#pos-t-geri-al-kilit-t3").TextContent.Trim());
        Assert.Empty(e.FindAll("#pos-geri-al-gerekce"));
    }

    // UX-S28: eşleşme listesi okunamazsa hata bandı çıkar, "geri alınacak eşleşme yok" DENMEZ.
    [Fact]
    public void ZO28n_pos_eslesme_listesi_HATASI_bos_durum_degil()
    {
        var e = PosEslesmisEkran("MUDUR", """{"message":"Sunucu yoğun"}""", HttpStatusCode.InternalServerError);
        e.Find("#pos-t-geri-al-t3").Click();
        e.WaitForAssertion(() => Assert.Contains("Sunucu yoğun", e.Find("#pos-geri-al-hata").TextContent));
        Assert.Empty(e.FindAll("#pos-geri-al-bos"));
        Assert.True(e.Find("#pos-geri-al-onayla").HasAttribute("disabled"));
    }

    // [DÜŞÜK] Elle seçim açıkken birincil eşleştir düğmesi listenin ve özetin ALTINDA (tek kopya);
    // başarı bandındaki sayı etiketli ("· 1 tahsilat").
    [Fact]
    public void ZO28o_pos_eslestir_dugmesi_LISTENIN_ALTINDA_sonuc_bandi_ETIKETLI()
    {
        var once = PosEkrani();
        Assert.Single(once.FindAll("#pos-eslestir-uygula"));
        Assert.Null(once.Find("#pos-eslestir-uygula").Closest(".zo-pos-elle"));
        // Bekleyen (eşleşmemiş) satırda geri alma eylemi YOK.
        once.Find("#pos-t-defter-kart-ac").Click();
        once.WaitForAssertion(() => Assert.Single(once.FindAll("#pos-t-iptal-t1")));
        Assert.Empty(once.FindAll("#pos-t-geri-al-t1"));

        var e = PosHareketSecili("""{"bulundu":false,"bankaHareketId":"bh9","toplam":0,"satirlar":[],"mesaj":"x","hareketKalan":981.5}""");
        Assert.Single(e.FindAll("#pos-eslestir-uygula"));
        var dugme = e.Find("#pos-eslestir-uygula");
        Assert.NotNull(dugme.Closest(".zo-pos-elle-eylem"));
        var sira = e.Find(".zo-pos-elle").QuerySelectorAll("#pos-elle, #pos-elle-toplam, #pos-eslestir-uygula").Select(x => x.Id).ToList();
        Assert.Equal(new[] { "pos-elle", "pos-elle-toplam", "pos-eslestir-uygula" }, sira);
        e.Find("#pos-elle-t1").Change(true);
        e.WaitForAssertion(() => Assert.False(e.Find("#pos-eslestir-uygula").HasAttribute("disabled")));
        e.Find("#pos-eslestir-uygula").Click();
        e.WaitForAssertion(() => Assert.Equal(
            string.Format(ZoIslemSozlugu.Tr.PosEslestiSonuc, "Eşleştirme tamamlandı", Bicim.Para(981.5m, "TRY"), 1)
                + " · " + ZoIslemSozlugu.Tr.PosTamamiEslesti,   // 29.09: yatmanın yeni kalanı banda yazılır
            e.Find("#pos-t-basari").TextContent.Trim()));
        Assert.Contains("· 1 tahsilat ·", e.Find("#pos-t-basari").TextContent.Trim());
        var css = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "zo-islem.css"));
        Assert.Matches(@"\.zo-pos-elle-eylem\s*\{[^}]*justify-content:\s*flex-end;", css);
        // 375'te geri alma penceresinin düğmeleri alt alta (yazı kırılmaz).
        Assert.Matches(@"\.satir\.zo-onay-dugmeler > \.btn\s*\{\s*white-space:\s*nowrap;", css);
        Assert.Matches(@"@media \(max-width: 480px\)\s*\{\s*\.satir\.zo-onay-dugmeler\s*\{\s*flex-direction:\s*column-reverse;", css);
    }

    // ══ 29.09 — doğrulayıcı ORTA + tasarım hakemi 4 KRİTİK ═══════════════════════════════════

    private const string MasakEsik =
        """{"kimlikEsigi":185000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000,"ekBelgeEsigi":20000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""";
    private const string MasakYalnizKimlik =
        """{"kademe":"YALNIZ_KIMLIK","kimlikGerekli":true,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""";
    private const string BuyukBakiye =
        """{"subeId":"s1","satirlar":[{"maden":"ALTIN","madenAdi":"Altın","hasGram":60,"girenHas":60,"cikanHas":0,"tahlilFarkiHas":0}],"fireKaydiAcik":false,"fireNotu":null}""";

    // [ORTA] İptal penceresi sunucu reddini PENCERENİN İÇİNDE gösterir; pencere açık kalır; sayfa
    // bandına (perdenin arkası) yazılmaz. Vazgeç ile ret metni de kapanır.
    [Fact]
    public void ZO29a_pos_iptal_sunucu_reddi_PENCEREDE_gorunur_pencere_acik_kalir()
    {
        Yol("/iptal", """{"message":"Banka yatmasıyla eşleşmiş tahsilat iptal edilemez; önce eşleşmeyi geri alın","statusCode":400}""", HttpStatusCode.BadRequest);
        var e = PosEkrani();
        e.Find("#pos-t-defter-kart-ac").Click();
        e.WaitForElement("#pos-t-iptal-t1").Click();
        e.WaitForElement("#pos-iptal-gerekce").Input("Banka itirazi geldi, musteri karti iptal etti");
        e.WaitForAssertion(() => Assert.False(e.Find("#pos-iptal-onayla").HasAttribute("disabled")));
        e.Find("#pos-iptal-onayla").Click();
        e.WaitForAssertion(() => Assert.Contains("önce eşleşmeyi geri alın", e.Find("#pos-iptal-hata").TextContent));
        Assert.Single(Postlar("/iptal"));
        Assert.Single(e.FindAll("#pos-iptal-gerekce"));                 // pencere AÇIK
        Assert.Empty(e.FindAll("#pos-t-hata"));                         // perdenin arkasına yazılmadı
        // Hata bandı pencerenin İÇİNDE (onay metniyle aynı kapta).
        Assert.NotNull(e.Find("#pos-iptal-hata").Closest(".modal, [role=dialog]"));
        e.Find("#pos-iptal-vazgec").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#pos-iptal-gerekce")));
        Assert.Empty(e.FindAll("#pos-iptal-hata"));
        // Yeniden açılınca eski ret metni görünmez.
        e.Find("#pos-t-iptal-t1").Click();
        e.WaitForElement("#pos-iptal-gerekce");
        Assert.Empty(e.FindAll("#pos-iptal-hata"));
    }

    // [KRİTİK, K18] Nakit kapalıyken tahsil aracı ZORUNLU ("isteğe bağlı" yok), Nakit seçilemez ve
    // seçili gelmez, cümle alanın altında TEK hata; Kaydet gerekçeli kilitli; Havale seçilince açılır.
    [Fact]
    public void ZO29b_hurda_nakit_kapali_TAHSIL_ARACI_zorunlu_nakit_secili_degil_kaydet_gerekceli_kilitli()
    {
        Yol("/tevsik/gunluk", TevsikAsildi);
        HurdaYollari();
        var e = HurdaEkrani();
        Gir(e, "#hrd-has", "9");
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#hrd-odeme")));
        var sar = e.Find("#hrd-odeme-sar");
        Assert.Single(sar.QuerySelectorAll(".zorunlu-isaret"));
        Assert.Empty(sar.QuerySelectorAll(".alan-istege"));
        var nakit = e.Find("#hrd-odeme input[value='NAKIT']");
        Assert.True(nakit.HasAttribute("disabled"));
        Assert.False(nakit.HasAttribute("checked"));
        Assert.Empty(e.FindAll("#hrd-odeme input.secim-radyo[checked]"));
        var uyari = e.Find("#hrd-nakit-uyari");
        Assert.Contains("alan-hata", uyari.ClassList);
        Assert.NotNull(uyari.Closest("#hrd-odeme-sar") ?? uyari.ParentElement!.QuerySelector("#hrd-odeme-sar"));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(e.Markup, System.Text.RegularExpressions.Regex.Escape(ZoIslemSozlugu.Tr.HurdaNakitAlinamaz)));
        Assert.True(e.Find("#hrd-kaydet").HasAttribute("disabled"));
        Assert.Equal(ZoIslemSozlugu.Tr.HurdaTahsilSecinKilit, e.Find("#hrd-kaydet-kilit").TextContent.Trim());

        e.Find("#hrd-odeme input[value='KART']").Change(true);
        e.WaitForAssertion(() => Assert.False(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        Assert.Empty(e.FindAll("#hrd-nakit-uyari"));
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/islemler/hurda-satis")));
        Assert.Equal("KART", Json(Postlar("/islemler/hurda-satis")[0].Govde).GetProperty("odemeTuru").GetString());
        // Kayıt bitince form açılış hâline döner: seçilen araç "Daha fazla"yı zorla açık tutmaz.
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hrd-detay")));
        Assert.Empty(e.FindAll("#hrd-odeme"));
    }

    // [KRİTİK] Eşik (SUNUCUDAN, /masak/esikler) üstünde MASAK alanları açılışta görünür; Kaydet eksik
    // alanda gerekçeli kilitli; doldurulunca açılır ve kimlik bilgisi gövdeye (masak) girer. Eşik altı: yok.
    [Fact]
    public void ZO29c_hurda_MASAK_esik_ustunde_ALANLAR_acilista_kaydet_kilitli_govdeye_masak_girer()
    {
        Yol("/hurda/bakiye", BuyukBakiye);
        Yol("/masak/esikler", MasakEsik);
        Yol("/masak/gereksinim", MasakYalnizKimlik);
        HurdaYollari();
        var e = HurdaEkrani();
        Gir(e, "#hrd-has", "1");
        e.WaitForAssertion(() => Assert.Contains(Bicim.Para(6800m, "TRY"), e.Find("#hrd-tutar").TextContent));
        Assert.Empty(e.FindAll("#hrd-masak"));                          // eşik altı: MASAK sorulmaz

        Gir(e, "#hrd-has", "30");                                       // 30 × 6.800 = 204.000 ≥ eşik
        e.WaitForElement("#hrd-masak");
        lock (istekler) Assert.Contains(istekler, i => i.Yol.Contains("/masak/gereksinim?tutar=204000"));
        Assert.True(e.Find("#hrd-kaydet").HasAttribute("disabled"));
        Assert.Equal(Sozlukler.Tr.Banka.MasakKilitSebep, e.Find("#hrd-kaydet-kilit").TextContent.Trim());
        // YASAL-PARAM-EKRAN: eşik sayısı İSTEMCİYE GÖMÜLMEZ — formda yalnız SUNUCUNUN eşiği, tek cümle kalıbıyla basılır.
        Assert.DoesNotContain("370", e.Find("#hrd-masak").TextContent.Replace(".", ""));
        if (e.FindAll("#masak-kural").Count > 0)   // PRO açıklaması: sunucunun eşiği (sahte 185.000) tek cümle kalıbıyla
            Assert.Equal(Kuyumcu.Core.I18n.YasalParamSozlugu.KimlikKuraliMetni("tr", 185_000m, 200_000m, Bicim.Tl) + " "
                + Kuyumcu.Core.I18n.YasalParamSozlugu.Tr.Saklama, e.Find("#masak-kural").TextContent);

        e.Find("#masak-belge-no").Input("10000000146");
        e.Find("#masak-ad-soyad").Input("Rafineri Yetkilisi");
        e.WaitForAssertion(() => Assert.False(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/islemler/hurda-satis")));
        var m = Json(Postlar("/islemler/hurda-satis")[0].Govde).GetProperty("masak");
        Assert.Equal("10000000146", m.GetProperty("belgeNo").GetString());
        Assert.Equal("Rafineri Yetkilisi", m.GetProperty("adSoyad").GetString());
        Assert.Equal("TC_KIMLIK", m.GetProperty("belgeTuru").GetString());
        // Başarıdan sonra kişisel veri formda kalmaz.
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hrd-masak")));
    }

    // [KRİTİK] Sunucu MASAK nedeniyle reddederse (eşik altında bağlantılı işlem toplamı) çıkmaz yok:
    // pencere kapanır, alanlar formda açılır, tek sade cümle; ham alan yolu ("masak.aciklama") basılmaz.
    [Fact]
    public void ZO29d_hurda_MASAK_reddi_ALANLARI_acar_ham_alan_yolu_basilmaz()
    {
        Yol("/masak/esikler", MasakEsik);
        Yol("/masak/gereksinim", MasakYalnizKimlik);
        Yol("/islemler/hurda-satis",
            """{"message":"MASAK: 185.000 TL ve üstü işlemde kimlik tespiti zorunludur · İşlem amacı belirtilmelidir · MASAK beyan açıklaması (masak.aciklama) en az 20 karakter olmalıdır (şu an 0)","alanlar":{"masak.aciklama":["en az 20 karakter"]},"statusCode":400}""",
            HttpStatusCode.BadRequest);
        HurdaYollari();
        var e = HurdaEkrani();
        Gir(e, "#hrd-has", "9");                                        // 61.200 — eşik altı
        Assert.Empty(e.FindAll("#hrd-masak"));
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForElement("#hrd-masak");
        Assert.Empty(e.FindAll("#hrd-onay-onayla"));                   // pencere kapandı
        Assert.Equal(ZoIslemSozlugu.Tr.HurdaMasakGerekli, e.Find("#hrd-masak-not").TextContent.Trim());
        Assert.DoesNotContain("masak.aciklama", e.Markup);
        Assert.Empty(e.FindAll("#hrd-hata"));
        lock (istekler) Assert.Contains(istekler, i => i.Yol.Contains("/masak/gereksinim?tutar=185000"));
        Assert.True(e.Find("#hrd-kaydet").HasAttribute("disabled"));
    }

    // [KRİTİK] Tezgâhtarın baktığı tutarlar 12 px değil: tutar 20 px kalın, özet/kalan/fark gövde boyu.
    [Fact]
    public void ZO29e_tutar_ve_ozet_yazilari_GOVDE_boyu_toplam_KALIN()
    {
        var css = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "zo-islem.css"));
        Assert.Matches(@"#hrd-ozet, #hrd-ozet > p,\s*#hrd-has-sinir, #hrd-tahlil-fark,\s*#hrd-bakiye-listesi \.hurda-bakiye-kart > span \{ font-size: var\(--yazi-govde\); \}", css);
        Assert.Matches(@"#hrd-tutar > strong \{ font-size: var\(--yazi-rakam\); font-weight: 700;", css);
        Assert.Matches(@"#pos-elle-toplam \{ font-size: var\(--yazi-rakam\); font-weight: 700;", css);
        // Kimlikler aynı: ekran hâlâ bu öğeleri çiziyor.
        HurdaYollari();
        var e = HurdaEkrani();
        Assert.Single(e.FindAll("#hrd-tutar > strong"));
        Assert.Single(e.FindAll("#hrd-ozet"));
    }

    // 29.09 (gerçek tarayıcı): kayıt bitince form AÇILIŞ hâline döner — elle açılan "Daha fazla" ve
    // seçilen tahsil aracı bir sonraki kayda taşınmaz (zorunlu önce).
    [Fact]
    public void ZO29f_hurda_kayit_sonrasi_form_ACILIS_halinde_detay_kapali_nakit()
    {
        HurdaYollari();
        var e = HurdaEkrani();
        Gir(e, "#hrd-has", "1");
        e.Find("#hrd-detay-ac").Click();
        e.WaitForElement("#hrd-odeme input[value='HAVALE']").Change(true);
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/islemler/hurda-satis")));
        Assert.Equal("HAVALE", Json(Postlar("/islemler/hurda-satis")[0].Govde).GetProperty("odemeTuru").GetString());
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hrd-detay")));
        Assert.Empty(e.FindAll("#hrd-odeme"));
        e.Find("#hrd-detay-ac").Click();
        e.WaitForAssertion(() => Assert.Equal("true", e.Find("#hrd-odeme input[value='NAKIT']").HasAttribute("checked") ? "true" : "false"));
    }

    // ══ 29.09 (tasarım hakemi t1 — 3 YÜKSEK) ═══════════════════════════════════════════════
    // [YÜKSEK] POS: hareket seçilince öneri OTOMATİK gelir (ara düğme basılmaz); başarılı
    // eşleştirmeden sonra seçili yatma BAYAT "kalan"ıyla ekranda KALMAZ, yeni kalan banda yazılır.
    [Fact]
    public void ZO29m_pos_hareket_secilince_ONERI_otomatik_eslestirince_secim_TEMIZLENIR_kalan_banda()
    {
        Yol("/pos/yatma-adaylari", BankaSonuc);
        Yol("/pos/oto-eslestir-onerisi",
            """{"bulundu":true,"bankaHareketId":"bh9","toplam":981.5,"satirlar":[{"posTahsilatId":"t1","tutar":981.5,"valorTarihi":"2026-09-25T00:00:00Z","posCihazId":"p1"}],"mesaj":null,"hareketKalan":2000,"uygunCihazlar":["p1"]}""");
        Yol("/pos/eslestir", """{"mesaj":"Eşleştirme tamamlandı","bankaHareketId":"bh9","eslesenToplam":981.5,"satirSayisi":1}""");
        var e = PosEkrani();
        // Yazmadan gözatma listesinden seçilir (tezgâhtarın gerçek yolu; canlıda yakalanan çıkmaz:
        // seçim temizlenince liste yeniden gelmiyordu).
        e.WaitForElement("#pos-eslestir-hareket-sonuc-0", TimeSpan.FromSeconds(3)).Click();
        // "Öneri getir"e BASILMADAN öneri gelir ve önerilen satır işaretlidir.
        e.WaitForElement("#pos-elle");
        e.WaitForAssertion(() => Assert.False(e.Find("#pos-eslestir-uygula").HasAttribute("disabled")));
        lock (istekler) Assert.Single(istekler, i => i.Yol.Contains("/pos/oto-eslestir-onerisi"));
        Assert.Contains("bh9", Postlar("/pos/oto-eslestir-onerisi")[0].Govde);

        e.Find("#pos-eslestir-uygula").Click();
        e.WaitForAssertion(() => Assert.Contains(
            string.Format(ZoIslemSozlugu.Tr.PosKalanVar, Bicim.Para(1018.5m, "TRY")),
            e.Find("#pos-t-basari").TextContent));
        var govde = Json(Postlar("/pos/eslestir").Single().Govde);
        Assert.Equal("bh9", govde.GetProperty("bankaHareketId").GetString());
        Assert.Equal(981.5m, govde.GetProperty("satirlar")[0].GetProperty("tutar").GetDecimal());
        // Seçim temizlendi: bayat kart, bayat "kalan" ve elle liste YOK; düğmeler gerekçeli kilitli.
        Assert.Empty(e.FindAll("#pos-eslestir-hareket-secili"));
        // Seçici listesi bayat kalmaz: eşleştirmeden SONRA aday listesi sunucudan YENİDEN okunur.
        e.WaitForAssertion(() =>
        {
            lock (istekler)
            {
                var eslestirSira = istekler.FindIndex(i => i.Metot == "POST" && i.Yol.Contains("/pos/eslestir"));
                Assert.True(eslestirSira >= 0);
                Assert.Contains(istekler.Skip(eslestirSira + 1), i => i.Metot == "GET" && i.Yol.Contains("/pos/yatma-adaylari"));
            }
        });
        // Yazmadan yeniden seçilebilir: gözatma listesi seçim temizlenince TAZE gelir.
        e.WaitForElement("#pos-eslestir-hareket-sonuc-0", TimeSpan.FromSeconds(3));
        Assert.Empty(e.FindAll("#pos-elle"));
        Assert.True(e.Find("#pos-oto-oneri").HasAttribute("disabled"));
        Assert.True(e.Find("#pos-eslestir-uygula").HasAttribute("disabled"));
        Assert.Equal(ZoIslemSozlugu.Tr.PosOnceHareketSec, e.Find("#pos-eslestir-kilit").TextContent.Trim());
    }

    // [YÜKSEK] Banka: "Bekliyor" talimat satırı tablonun altında GÖRÜNMEZ (Ayrıntı katlanında);
    // POS hesabına gelen, müşteriye bağlanmamış satırın birincil eylemi POS eşleştirmesidir.
    [Fact]
    public void ZO29n_banka_POS_hesabi_satiri_POS_ile_eslestir_birincil_bekliyor_notu_katlanda()
    {
        Yol("/pos/cihazlar", """[{"id":"p1","ad":"Garanti POS","bankaAdi":"Garanti","subeId":"s1","kasaId":"k1","bankaHesapId":"b2","uyeIsyeriNo":null,"terminalNo":null,"pesinKomisyonOran":0.0185,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0}]""");
        var e = BankaEkrani(HesapSahipsiz);   // b2 = POS cihazının hesabı
        e.WaitForElement("#bh-gecmis-ac").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#bh-h-eylem-bh1 #bh-pos-bh1")));
        Assert.Equal(ZoIslemSozlugu.Tr.BankaPosEslestir, e.Find("#bh-pos-bh1").TextContent.Trim());
        // Müşteriyle eşleştirme kaybolmadı: menüde.
        Assert.NotNull(e.Find("#bh-eslestir-bh1").Closest("#bh-h-menu-bh1"));
        // Talimat satırı görünür akışta değil, kapalı "Ayrıntı" katlanında.
        Assert.NotNull(e.Find("#bh-bekliyor-not").Closest("details#bh-degismez-ayrinti"));
        // Katlanın DIŞINDA ikinci bir kopya da yok (metin sayfada tek kez, o da katlanda).
        Assert.Single(e.FindAll("#bh-bekliyor-not"));
        Assert.All(e.FindAll("p").Where(p => p.TextContent.Trim() == BankaDekontSozlugu.Tr.BekliyorAnlami),
            p => Assert.NotNull(p.Closest("details#bh-degismez-ayrinti")));
        Assert.False(e.Find("#bh-degismez-ayrinti").HasAttribute("open"));
        e.Find("#bh-pos-bh1").Click();
        Assert.EndsWith("/pos/tahsilatlar", Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().Uri);
    }

    [Fact]
    public void ZO29n_banka_POS_hesabi_OLMAYAN_satirda_POS_eylemi_yok_musteriyle_eslestir_birincil()
    {
        Yol("/pos/cihazlar", """[{"id":"p1","ad":"Garanti POS","bankaAdi":"Garanti","subeId":"s1","kasaId":"k1","bankaHesapId":"b2","uyeIsyeriNo":null,"terminalNo":null,"pesinKomisyonOran":0.0185,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0}]""");
        var e = BankaEkrani(HesapSahipli);    // b1 — cihaz bağlı değil
        e.WaitForElement("#bh-gecmis-ac").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#bh-h-eylem-bh1 #bh-eslestir-bh1")));
        Assert.Empty(e.FindAll("#bh-pos-bh1"));
    }

    // [YÜKSEK] Dekont: MASAK alanları açılınca Kaydet/Vazgeç ve kilit sebebi pencerenin SABİT
    // altlığındadır (gövdeyle kaymaz); tutar kutusu odak dışında tr-TR gruplanır.
    [Fact]
    public void ZO29o_dekont_kaydet_ve_sebep_SABIT_altlikta_tutar_odak_disinda_gruplanir()
    {
        var e = BankaEkrani();
        e.Find("#bh-dekont-gir").Click();
        var kutu = e.WaitForElement("#dekont-tutar");
        kutu.Focus();
        kutu.Input("250000");
        e.Find("#dekont-tutar").Blur();
        e.WaitForAssertion(() => Assert.Equal(Bicim.ParaGirdi(250000m), e.Find("#dekont-tutar").GetAttribute("value")));
        Assert.Equal("250.000", e.Find("#dekont-tutar").GetAttribute("value"));
        e.WaitForElement("#degismez-ipucu-MasakKimlikTespiti");
        var kaydet = e.Find("#dekont-kaydet");
        Assert.NotNull(kaydet.Closest(".modal-alt"));
        Assert.Null(kaydet.Closest(".modal-govde"));
        Assert.True(kaydet.HasAttribute("disabled"));
        Assert.NotNull(e.Find("#dekont-kaydet-sebep").Closest(".modal-alt"));
        Assert.Null(e.Find("#dekont-kaydet-sebep").Closest(".modal-govde"));
        // MASAK alanları gövdede (kayar), altlıkta değil.
        Assert.NotNull(e.Find(".zo-masak-dikey").Closest(".modal-govde"));
        var css = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "zo-islem.css"));
        Assert.Matches(@"\.modal-alt \.zo-alt-sebep\s*\{\s*flex:\s*1 1 100%;", css);
    }

    // ══ 29.09 (tasarım hakemi YÜKSEK): şubesiz oturumda POS ham sunucu metni 3 kez + "ödeme yok" ══
    // Küresel şube çözümleyicisi 403 "Şube bağlamı çözülemedi: …" döner (canlıda ölçüldü). POS
    // sayfası bunu Hurda ile AYNI sade cümleye çevirir, TEK kez yazar; takvim "ödeme yok" demez,
    // eşleştirme kartı/arama kutusu ve defter çizilmez (UX-S28, UX-S30).
    [Theory]
    [InlineData("TEZGAHTAR")]
    [InlineData("MUDUR")]
    [InlineData("SAHIP")]
    public void ZO29q_pos_subesiz_403_sade_cumle_TEK_kez_kartlar_cizilmez(string rol)
    {
        const string ham = "Şube bağlamı çözülemedi: bu rol tüm şubeleri göremez ve oturumda şube yok.";
        Oturum(rol, sube: null);
        Yol("/pos/valor-takvimi", "{\"message\":\"" + ham + "\"}", HttpStatusCode.Forbidden);
        Yol("/pos/tahsilatlar", "{\"message\":\"" + ham + "\"}", HttpStatusCode.Forbidden);
        Yol("/pos/yatma-adaylari", "{\"message\":\"" + ham + "\"}", HttpStatusCode.Forbidden);
        var e = Render<Kuyumcu.Web.Pages.PosTahsilatlar>().AyrintiAc();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#pos-t-hata")));

        var beklenen = rol == "SAHIP" ? ZoIslemSozlugu.Tr.HurdaSubeYokSahip : SadeSozlugu.Tr.HurdaSubeYok;
        Assert.Contains(beklenen, e.Find("#pos-t-hata").TextContent);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(e.Markup, System.Text.RegularExpressions.Regex.Escape(beklenen)));
        Assert.DoesNotContain("şube bağlamı", e.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(e.FindAll("#pos-t-takvim-bos"));
        Assert.Empty(e.FindAll("#pos-t-valor"));
        Assert.Empty(e.FindAll("#pos-eslestir-hareket"));
        Assert.Empty(e.FindAll("#pos-oto-oneri"));
        Assert.Empty(e.FindAll("#pos-t-defter-kart-ac"));
        Assert.DoesNotContain(SadeSozlugu.Tr.PosTakvimBos, e.Markup);
    }

    [Fact]
    public void ZO29q_pos_subeli_oturumda_BASKA_hata_sunucu_metniyle_kalir_bos_durum_yalan_soylemez()
    {
        Oturum("MUDUR", sube: "s1");
        Yol("/pos/valor-takvimi", """{"message":"Sunucu yoğun, tekrar deneyin."}""", HttpStatusCode.Forbidden);
        Yol("/pos/tahsilatlar", "{\"satirlar\":[],\"toplamSatir\":0,\"limit\":50,\"offset\":0,\"azamiSatir\":200,\"sinirDegdi\":false}");
        var e = Render<Kuyumcu.Web.Pages.PosTahsilatlar>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Contains("Sunucu yoğun", e.Find("#pos-t-hata").TextContent));
        Assert.DoesNotContain(SadeSozlugu.Tr.HurdaSubeYok, e.Markup);
        Assert.Empty(e.FindAll("#pos-t-takvim-bos"));
        Assert.Empty(e.FindAll("#pos-eslestir-hareket"));
    }

    // ══ 29.09 (tasarım hakemi YÜKSEK): banka hareketi seçenekleri silik, hiyerarşi ters ══
    // Her seçenek kenarlı + zeminli satır-düğme; TUTAR (sonucun adı) büyük, hesap · tarih ikincil;
    // "Banka hareketi" çipi bu seçicide gizli. Kural yalnız bu ekranın sarmalında (ortak bileşen değil).
    [Fact]
    public void ZO29r_pos_hareket_secenekleri_satir_dugme_TUTAR_birincil_cip_yok()
    {
        Yol("/pos/yatma-adaylari",
            """{"sorgu":"","tur":"BANKA_HAREKET","limit":10,"ofset":0,"sayi":1,"dahaVar":false,"tavan":50,"sonuclar":[{"kimlik":"bh9","ad":"+1.470 ₺","altBilgi":"Garanti · Ana · 29.09.2026","tur":"BANKA_HAREKET","rota":null}],"gozatma":true}""");
        var e = PosEkrani();
        var sonuc = e.WaitForElement("#pos-eslestir-hareket-sonuc-0", TimeSpan.FromSeconds(3));
        Assert.Equal("+1.470 ₺", sonuc.QuerySelector(".secici-ad")!.TextContent.Trim());
        Assert.NotNull(e.Find("#pos-eslestir-hareket-sarmal"));

        var css = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "zo-islem.css"));
        Assert.Matches(@"#pos-eslestir-hareket-sarmal \.secici-sonuc \{[^}]*border: 1px solid var\(--kenarlik\);[^}]*background: var\(--yuzey\);", css);
        Assert.Matches(@"#pos-eslestir-hareket-sarmal \.secici-sonuc > \.rozet,\s*#pos-eslestir-hareket-sarmal \.secici-secili > \.rozet \{ display: none; \}", css);
        Assert.Matches(@"#pos-eslestir-hareket-sarmal \.secici-secili > \.secici-ad \{\s*font-size: 20px; font-weight: 700;", css);
        Assert.Matches(@"#pos-eslestir-hareket-sarmal \.secici-secili > \.metin-kucuk \{ font-size: 15px; \}", css);
        // Açılış ≤ 1 ekran: geniş ekranda seçici iki sütun, eylemler sağda alt alta.
        Assert.Contains("zo-pos-eslestir-izgara", e.Find("#pos-eslestir-hareket-sarmal").ParentElement!.ClassName);
        Assert.Matches(@"@media \(min-width: 1101px\) \{\s*\.kart-govde\.zo-pos-eslestir-izgara \{\s*grid-template-columns: minmax\(0, 2fr\) minmax\(0, 1fr\);", css);
    }

    // 29.09 (aynı sınıf, canlıda ölçüldü): şubeli TEZGAHTAR /pos/tahsilatlar 200 alır ama eşleştirme
    // uçları 403 "gerekli rol" döner → kart hiç çizilmez, aday ucuna istek gitmez; SAHİP/MÜDÜR/MUHASEBE görür.
    [Theory]
    [InlineData("TEZGAHTAR", false)]
    [InlineData("MUHASEBE", true)]
    [InlineData("MUDUR", true)]
    public void ZO29s_pos_eslestirme_karti_yalniz_yetkili_rolde(string rol, bool gorur)
    {
        Oturum(rol, sube: "s1");
        Yol("/pos/valor-takvimi", "[]");
        Yol("/pos/tahsilatlar", "{\"satirlar\":[" + PosSatir + "],\"toplamSatir\":1,\"limit\":50,\"offset\":0,\"azamiSatir\":200,\"sinirDegdi\":false}");
        Yol("/pos/yatma-adaylari", BankaSonuc);
        var e = Render<Kuyumcu.Web.Pages.PosTahsilatlar>().AyrintiAc();
        e.WaitForElement("#pos-t-defter-kart-ac");
        if (gorur)
        {
            e.WaitForElement("#pos-eslestir-hareket-sonuc-0", TimeSpan.FromSeconds(3));
            Assert.NotNull(e.Find("#pos-oto-oneri"));
        }
        else
        {
            Assert.Empty(e.FindAll("#pos-eslestir-hareket"));
            Assert.Empty(e.FindAll("#pos-oto-oneri"));
            lock (istekler) Assert.DoesNotContain(istekler, i => i.Yol.Contains("/pos/yatma-adaylari"));
        }
    }

    // ══ 29.09 (16. tur) — tasarım hakemi KRİTİK + 2 YÜKSEK ═══════════════════════════════════

    private const string MasakBeyanFormu =
        """{"kademe":"BEYAN_FORMU","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":true,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""";
    private const string RamatciUlid = "01J9ZK8QW7A0B1C2D3E4F5G6H7";
    private const string RamatciArama =
        "{\"sorgu\":\"ab\",\"tur\":\"CARI\",\"limit\":10,\"ofset\":0,\"sayi\":1,\"dahaVar\":false,\"tavan\":50,"
        + "\"sonuclar\":[{\"kimlik\":\"" + RamatciUlid + "\",\"ad\":\"Abdurrahman Ramat\",\"altBilgi\":\"05551112233\",\"tur\":\"CARI\",\"rota\":\"/cariler?cariId=x\"}]}";

    private static void RamatciSec(IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e)
    {
        if (e.FindAll("#hrd-cari").Count == 0) e.Find("#hrd-detay-ac").Click();
        e.WaitForElement("#hrd-cari").Input("abd");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#hrd-cari-sonuc-0")));
        e.Find("#hrd-cari-sonuc-0").Click();
    }

    // [KRİTİK] Hurda MASAK çıkmazı: aynı gün aynı ramatçıyla yapılmış işlemler toplamı kademeyi
    // yükseltir (sunucu kümülatifle doğrular). Ekran gereksinimi MÜŞTERİYLE sorar → amaç, açıklama,
    // fon kaynağı alanları kayıttan ÖNCE çizilir, doldurulmadan Kaydet açılmaz, gövdeye girer.
    [Fact]
    public void ZO29z_hurda_MASAK_gereksinimi_RAMATCIYLA_sorulur_kumulatif_kademe_alanlari_cizilir()
    {
        Yol("/hurda/bakiye", BuyukBakiye);
        Yol("/masak/esikler", MasakEsik);
        Yol("/masak/gereksinim?tutar=204000&cariId=" + RamatciUlid, MasakBeyanFormu);
        Yol("/masak/gereksinim", MasakYalnizKimlik);
        Yol("/arama", RamatciArama);
        HurdaYollari();
        var e = HurdaEkrani();
        Gir(e, "#hrd-has", "30");                                       // 204.000 ≥ eşik
        e.WaitForElement("#hrd-masak");
        Assert.Empty(e.FindAll("#masak-amac"));                         // müşterisiz: yalnız kimlik

        RamatciSec(e);
        e.WaitForElement("#masak-amac");
        lock (istekler) Assert.Contains(istekler, i => i.Yol.Contains("/masak/gereksinim?tutar=204000&cariId=" + RamatciUlid));
        Assert.Single(e.FindAll("#masak-aciklama"));
        Assert.Single(e.FindAll("#masak-fon"));

        e.Find("#masak-belge-no").Input("10000000146");
        e.Find("#masak-ad-soyad").Input("Abdurrahman Ramat");
        e.WaitForAssertion(() => Assert.True(e.Find("#hrd-kaydet").HasAttribute("disabled")));   // amaç/fon eksik
        e.Find("#masak-amac").Input("Hurda altın satışı");
        e.Find("#masak-aciklama").Input("Rafineriye aylık hurda çıkışı, tahlil sonrası ödeme");
        e.Find("#masak-fon").Input("Rafineri ticari hesabı");
        e.WaitForAssertion(() => Assert.False(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/islemler/hurda-satis")));
        var g = Json(Postlar("/islemler/hurda-satis")[0].Govde);
        Assert.Equal(RamatciUlid, g.GetProperty("cariId").GetString());
        var m = g.GetProperty("masak");
        Assert.Equal("Hurda altın satışı", m.GetProperty("islemAmaci").GetString());
        Assert.Equal("Rafineri ticari hesabı", m.GetProperty("fonKaynagi").GetString());
    }

    // [KRİTİK] Eşik ALTINDA ama ramatçının günlük toplamı kimlik istiyorsa alanlar kayıttan ÖNCE açılır
    // (sunucuya gidip 400 almadan); Kaydet eksik alanda gerekçeli kilitli.
    [Fact]
    public void ZO29z_hurda_esik_alti_RAMATCI_toplami_kimlik_istiyorsa_alanlar_KAYITTAN_ONCE_acilir()
    {
        Yol("/masak/esikler", MasakEsik);
        Yol("/masak/gereksinim?tutar=61200&cariId=" + RamatciUlid, MasakBeyanFormu);
        Yol("/masak/gereksinim", """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":0}""");
        Yol("/arama", RamatciArama);
        HurdaYollari();
        var e = HurdaEkrani();
        Gir(e, "#hrd-has", "9");                                        // 61.200 — eşik altı
        Assert.Empty(e.FindAll("#hrd-masak"));
        RamatciSec(e);
        e.WaitForElement("#masak-fon");
        Assert.True(e.Find("#hrd-kaydet").HasAttribute("disabled"));
        Assert.Equal(Sozlukler.Tr.Banka.MasakKilitSebep, e.Find("#hrd-kaydet-kilit").TextContent.Trim());
        Assert.Empty(Postlar("/islemler/hurda-satis"));
    }

    // [KRİTİK — sessiz döngü] Ekrandaki MASAK alanlarının HEPSİ doluyken sunucu yine MASAK'la reddederse
    // (ör. uyruk kodu) sunucunun cümlesi sayfa bandında görünür; kullanıcı sebepsiz döngüde kalmaz.
    [Fact]
    public void ZO29z_hurda_MASAK_alanlari_doluyken_ret_SEBEBI_bantta_gorunur()
    {
        Yol("/hurda/bakiye", BuyukBakiye);
        Yol("/masak/esikler", MasakEsik);
        Yol("/masak/gereksinim", MasakYalnizKimlik);
        Yol("/islemler/hurda-satis",
            """{"message":"MASAK: uyruk kodu geçersiz ('XXX'); pasaporttaki 3 harfli ülke kodunu yazın (örnek: FRA).","statusCode":400}""",
            HttpStatusCode.BadRequest);
        HurdaYollari();
        var e = HurdaEkrani();
        Gir(e, "#hrd-has", "30");
        e.WaitForElement("#hrd-masak");
        e.Find("#masak-belge-no").Input("10000000146");
        e.Find("#masak-ad-soyad").Input("Rafineri Yetkilisi");
        e.WaitForAssertion(() => Assert.False(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Contains("uyruk kodu", e.Find("#hrd-hata").TextContent));
        Assert.Empty(e.FindAll("#hrd-onay-onayla"));
    }

    // [YÜKSEK, K18/K20] Dekont Tutar kutusu BOŞ açılır; yazılan "250000" "0250000" olmaz.
    [Fact]
    public void ZO29z_banka_dekont_tutar_BOS_acilir_yazilan_sifirla_birlesmez()
    {
        var e = BankaEkrani();
        e.Find("#bh-dekont-gir").Click();
        e.WaitForElement("#dekont-tutar");
        Assert.Equal("", e.Find("#dekont-tutar").GetAttribute("value") ?? "");
        e.Find("#dekont-tutar").Input("250000");
        e.WaitForAssertion(() => Assert.Equal("250000", e.Find("#dekont-tutar").GetAttribute("value")));
        // Kaydet boş tutarla POST atmaz; alan altı mesajı.
        e.Find("#dekont-tutar").Input("");
        e.Find("#dekont-kaydet").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#alan-hata-dekont-tutar")));
        Assert.Empty(Postlar("/hareket"));
        // Yeniden açılışta yine boş.
        e.Find("#dekont-tutar").Input("500");
        e.WaitForAssertion(() => Assert.False(e.Find("#dekont-kaydet").HasAttribute("disabled")));
        e.Find("#dekont-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/hareket")));
        Assert.Equal(500m, Json(Postlar("/hareket")[0].Govde).GetProperty("tutar").GetDecimal());
        e.Find("#bh-dekont-gir").Click();
        e.WaitForElement("#dekont-tutar");
        Assert.Equal("", e.Find("#dekont-tutar").GetAttribute("value") ?? "");
    }

    // [KRİTİK sınıfı, Banka] Dekontta müşteri seçilince gereksinim MÜŞTERİYLE sorulur; müşterinin günlük
    // toplamı kimlik istiyorsa eşik altında da alanlar açılır.
    [Fact]
    public void ZO29z_banka_dekont_MUSTERI_secilince_gereksinim_musteriyle_sorulur()
    {
        yollar.Add(("/masak/gereksinim?tutar=50000&cariId=c1", HttpStatusCode.OK, MasakBeyanFormu));
        var e = BankaEkrani();
        e.Find("#bh-dekont-gir").Click();
        e.WaitForElement("#dekont-tutar").Input("50000");
        Assert.Empty(e.FindAll("#masak-belge-no"));
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Yol.Contains("/masak/gereksinim"));   // müşterisiz eşik altı: sorgu yok
        e.Find("#dekont-detay-ac").Click();
        e.WaitForElement("#dekont-cari").Click();
        e.WaitForElement("#dekont-cari-secenek-0").Click();
        e.WaitForElement("#masak-fon");
        lock (istekler) Assert.Contains(istekler, i => i.Yol.Contains("/masak/gereksinim?tutar=50000&cariId=c1"));
        Assert.True(e.Find("#dekont-kaydet").HasAttribute("disabled"));
    }
}

