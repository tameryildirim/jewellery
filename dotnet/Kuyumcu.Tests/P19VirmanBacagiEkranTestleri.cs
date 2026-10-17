using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-19 — SAPMA S1 KAPANIŞI: VİRMAN BACAĞI KASA/BANKA EKRANINDA [bUnit]
//
// Hasım doğrulayıcı bulgusu (2026-09-21, ORTA): planın "Dosyalar" satırı
// `Pages/Kasalar.razor` ve `Banka.razor`u da SÖZLEŞME olarak sayıyordu, ama
// ilk turda hiç açılmadılar. Sonuç: StornoServisi'nin "bu bir VİRMAN bacağıdır,
// virmanın kendi stornosunu kullanın" yönlendirmesi YALNIZ API 409 gövdesinde
// yaşıyordu; kullanıcı kasa/banka ekranında ham hata bandı görüyor, doğru yola
// götüren hiçbir bağlantı bulamıyordu ("sunucuda uç var, ekranda düğme yok" —
// UX-S25'in tam olarak yasakladığı hâl).
//
// Bu dosya o yolu KORUR. Sınananlar:
//   1. karsiBacakId DOLU satırda storno düğmesi KİLİTLİ (sunucu kuralının aynısı)
//   2. Kilidin SEBEBİ satırın yanında Türkçe YAZILI (UX-S30 · DoD EK 1/1)
//   3. DOĞRU YOL bir bağlantıdır ve /gider?sekme=virman'a gider (UX-S25)
//   4. karsiBacakId BOŞ satırda düğme AÇIK ve virman metinleri YOK (negatif kontrol)
//   5. /gider?sekme=virman derin bağı Virman sekmesini GERÇEKTEN açar
//   6. Kasa ve banka ekranı AYNI sözlük anahtarını kullanır (UX-S31 tek kaynak)
//
// ⚠ Sahte API "{}" varsayılanıyla bırakılmadı: iki hareket ucu da gerçekçi,
//   camelCase ve karsiBacakId taşıyan yanıt döndürür — yoksa ekran boş modelle
//   yeşil kalır ve sözleşme kayması görünmez.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P19VirmanBacagiEkranTestleri : BunitContext
{
    /// <summary>Hareket listesi yanıtlarında karsiBacakId dolu mu? (senaryo anahtarı)</summary>
    private bool bacakDolu = true;

    public P19VirmanBacagiEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => bacakDolu))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(Func<bool> bacakDolu) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            // Sunucu kasa/banka hareketlerini VARLIK olarak döndürür; karsi_bacak_id
            // kolonu JSON'a camelCase "karsiBacakId" olarak çıkar (Entities.cs:920/1042).
            var bacak = bacakDolu() ? "\"kh-karsi\"" : "null";

            var y = yol switch
            {
                var v when v.Contains("/kasalar/") && v.Contains("/hareketler") => $$"""
                    {"hareketler":[{"id":"kh1","tutar":-2000,"odemeTuru":"NAKIT",
                      "tarih":"2026-09-20T10:00:00Z","hareketId":null,
                      "aciklama":"Virman: Merkez Kasa -> Ziraat","stornoKaynakId":null,
                      "karsiBacakId":{{bacak}}}]}
                    """,
                var v when v.Contains("/bankalar/") && v.Contains("/hareketler") => $$"""
                    {"hareketler":[{"id":"bh1","yon":"GELEN","tutar":2000,
                      "karsiTarafAd":"Merkez Kasa","aciklama":"Virman","durum":"BEKLIYOR",
                      "tarih":"2026-09-20T10:00:00Z","karsiIban":null,"stornoKaynakId":null,
                      "ekstreSatirId":null,"karsiBacakId":{{bacak}}}]}
                    """,
                var v when v.Contains("/kasalar") => """
                    [{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":10000,"aktif":true,"subeId":null}]
                    """,
                var v when v.Contains("/bankalar") => """
                    {"hesaplar":[{"id":"bh-h1","bankaAdi":"Ziraat","hesapAdi":"Vadesiz","hesapSahibi":null,
                      "iban":"TR200000000000000000000001","paraBirimi":"TRY","bakiye":5000,"aktif":true}]}
                    """,
                var v when v.Contains("/kur/guncel") => """{"id":"kur1","hasAlis":4100,"hasSatis":4150}""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                var v when v.Contains("/gider-kategorileri") => """{"kategoriler":[],"toplam":0}""",
                var v when v.Contains("/giderler") => """
                    {"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":"En çok 100 gider satırı gösterilir."}
                    """,
                var v when v.Contains("/virman") => """
                    {"satirlar":[],"toplam":0,"limit":50,"dahaFazlasiVar":false,"sinirNotu":"En son 50 virman gösterilir."}
                    """,
                _ => "{}",
            };

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private void OturumAc() =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));

    /// <summary>Kasalar ekranını açar ve ks1'in GEÇMİŞ modalını gösterir.</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Kasalar> KasaGecmisi()
    {
        OturumAc();
        var e = Render<Kuyumcu.Web.Pages.Kasalar>().AyrintiAc();
        e.WaitForAssertion(() => e.Find("#kasa-gecmis-ks1"));
        e.Find("#kasa-gecmis-ks1").Click();
        e.WaitForAssertion(() => e.Find("#kh-storno-kh1"));
        return e;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Banka> BankaHareketleri()
    {
        OturumAc();
        var e = Render<Kuyumcu.Web.Pages.Banka>().AyrintiAc().HareketleriAc();   // ZO-ISLEM-A
        e.WaitForAssertion(() => e.Find("#bh-storno-bh1"));
        return e;
    }

    // ── 1+2+3 · KASA: kilit + sebep + doğru yol ─────────────────────────────

    [Fact]
    public void Kasa_virman_bacaginda_storno_KILITLI_sebebi_yazili_ve_dogru_yol_bagli()
    {
        var e = KasaGecmisi();

        // 1) Sunucu kuralının EKRANDAKİ aynısı: tek bacak stornosu açılmaz.
        Assert.True(e.Find("#kh-storno-kh1").HasAttribute("disabled"),
            "Virman bacağının storno düğmesi AÇIK kalmış — kullanıcı 409'a koşturuluyor.");

        // 2) Kilidin sebebi görünür yerde ve Türkçe (UX-S30).
        var gerekce = e.Find("#kh-virman-gerekce-kh1").TextContent;
        Assert.Contains("VİRMANIN bacağıdır", gerekce, StringComparison.Ordinal);
        Assert.Contains("yalnız yarısı", gerekce, StringComparison.Ordinal);

        // 3) Doğru yol gerçek bir bağlantıdır ve virman sekmesine gider (UX-S25).
        var yol = e.Find("#kh-virman-yol-kh1");
        Assert.Equal("/gider?sekme=virman", yol.GetAttribute("href"));
        Assert.Contains("Virman stornosuna git", yol.TextContent, StringComparison.Ordinal);

        // Rozet: satır bir bakışta "virman bacağı" diye okunur.
        Assert.Contains("Virman bacağı", e.Markup, StringComparison.Ordinal);
    }

    // ── 4 · KASA negatif kontrol: sıradan satır etkilenmez ──────────────────

    [Fact]
    public void Kasa_virman_OLMAYAN_satirda_storno_ACIK_ve_virman_metni_YOK()
    {
        bacakDolu = false;
        var e = KasaGecmisi();

        Assert.False(e.Find("#kh-storno-kh1").HasAttribute("disabled"),
            "Sıradan elle hareketin stornosu kapanmış — P-19 kilidi fazla geniş.");
        Assert.Empty(e.FindAll("#kh-virman-gerekce-kh1"));
        Assert.Empty(e.FindAll("#kh-virman-yol-kh1"));
        Assert.DoesNotContain("Virman bacağı", e.Markup, StringComparison.Ordinal);
    }

    // ── 1+2+3 · BANKA: kasa ile BİREBİR aynı kalıp ──────────────────────────

    [Fact]
    public void Banka_virman_bacaginda_storno_KILITLI_sebebi_yazili_ve_dogru_yol_bagli()
    {
        var e = BankaHareketleri();

        Assert.True(e.Find("#bh-storno-bh1").HasAttribute("disabled"),
            "Virman bacağı dekontunun storno düğmesi AÇIK kalmış.");

        var gerekce = e.Find("#bh-virman-gerekce-bh1").TextContent;
        Assert.Contains("VİRMANIN bacağıdır", gerekce, StringComparison.Ordinal);

        var yol = e.Find("#bh-virman-yol-bh1");
        Assert.Equal("/gider?sekme=virman", yol.GetAttribute("href"));
        Assert.Contains("Virman stornosuna git", yol.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Banka_virman_OLMAYAN_dekontta_storno_ACIK_ve_virman_metni_YOK()
    {
        bacakDolu = false;
        var e = BankaHareketleri();

        Assert.False(e.Find("#bh-storno-bh1").HasAttribute("disabled"),
            "Sıradan dekontun stornosu kapanmış — P-19 kilidi fazla geniş.");
        Assert.Empty(e.FindAll("#bh-virman-yol-bh1"));
    }

    // ── 5 · Derin bağın HEDEFİ gerçekten açılır ─────────────────────────────

    [Fact]
    public void Gider_ekrani_sekme_virman_sorgusuyla_VIRMAN_sekmesini_acar()
    {
        OturumAc();
        // SupplyParameterFromQuery YALNIZ gerçek gezinmeyle dolar — kasa/banka
        // ekranındaki bağlantının yaptığı şeyin AYNISI yapılır (sahte parametre
        // enjeksiyonu bu yolu sınamazdı).
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
            .NavigateTo("http://localhost/gider?sekme=virman");
        var e = Render<Kuyumcu.Web.Pages.Gider>().AyrintiAc("#yildiz-gider-ekle");
        e.WaitForAssertion(() => e.Find("#gd-sekme-virman"));

        // PAKET S1-i: şerit ortak Sekmeler bileşeninde — aria-selected WAI-ARIA
        // kalıbıyla "true"/"false" DEĞERİ taşır (öznitelik her sekmede vardır).
        Assert.Equal("true", e.Find("#gd-sekme-virman").GetAttribute("aria-selected"));
        Assert.Contains("secili", e.Find("#gd-sekme-virman").ClassName ?? "", StringComparison.Ordinal);
        // Negatif kontrol: gider sekmesi seçili DEĞİL — bağlantı gerçekten işe yaradı.
        Assert.Equal("false", e.Find("#gd-sekme-giderler").GetAttribute("aria-selected"));
        Assert.DoesNotContain(" secili", e.Find("#gd-sekme-giderler").ClassName ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void S1i_Gider_sekmesine_tiklaninca_panel_ve_sayfa_durumu_birlikte_degisir()
    {
        // PAKET S1-i: şerit ortak Sekmeler bileşeninde; seçim URL'ye yazılır ve
        // sayfanın kendi durumu (sekme) SeciliDegisti ile İZLER — panel boş kalmaz.
        OturumAc();
        var e = Render<Kuyumcu.Web.Pages.Gider>().AyrintiAc("#yildiz-gider-ekle");
        e.WaitForAssertion(() => e.Find("#gd-sekme-kategoriler"));
        Assert.Empty(e.FindAll("#gd-kat-ekle"));
        e.Find("#gd-sekme-kategoriler").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#gd-panel-kategoriler #gd-kat-ekle")));
        Assert.Equal("true", e.Find("#gd-sekme-kategoriler").GetAttribute("aria-selected"));
        Assert.Contains("sekme=kategoriler",
            Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void Gider_ekrani_sorgu_YOKKEN_varsayilan_GIDER_sekmesinde_kalir()
    {
        OturumAc();
        var e = Render<Kuyumcu.Web.Pages.Gider>().AyrintiAc("#yildiz-gider-ekle");
        e.WaitForAssertion(() => e.Find("#gd-sekme-giderler"));
        Assert.Equal("true", e.Find("#gd-sekme-giderler").GetAttribute("aria-selected"));   // S1-i: ARIA değeri
        Assert.Equal("false", e.Find("#gd-sekme-virman").GetAttribute("aria-selected"));
    }

    // ── 6 · UX-S31 — tek kavram, tek kaynak ─────────────────────────────────

    [Fact]
    public void Kasa_ve_banka_ayni_sozluk_anahtarindan_beslenir_TR_ve_EN_dolu()
    {
        foreach (var kod in new[] { "tr", "en" })
        {
            var s = Kuyumcu.Core.I18n.Sozlukler.Getir(kod);
            Assert.False(string.IsNullOrWhiteSpace(s.Kabuk.VirmanBacagi), $"[{kod}] VirmanBacagi boş");
            Assert.False(string.IsNullOrWhiteSpace(s.Kabuk.VirmanBacagiGerekce), $"[{kod}] VirmanBacagiGerekce boş");
            Assert.False(string.IsNullOrWhiteSpace(s.Kabuk.VirmanStornosunaGit), $"[{kod}] VirmanStornosunaGit boş");
        }

        // TR ve EN GERÇEKTEN farklı (çıplak metin / kopyala-yapıştır yok).
        Assert.NotEqual(Kuyumcu.Core.I18n.Sozlukler.Getir("tr").Kabuk.VirmanBacagi,
                        Kuyumcu.Core.I18n.Sozlukler.Getir("en").Kabuk.VirmanBacagi);
        Assert.NotEqual(Kuyumcu.Core.I18n.Sozlukler.Getir("tr").Kabuk.VirmanBacagiGerekce,
                        Kuyumcu.Core.I18n.Sozlukler.Getir("en").Kabuk.VirmanBacagiGerekce);
    }
}
