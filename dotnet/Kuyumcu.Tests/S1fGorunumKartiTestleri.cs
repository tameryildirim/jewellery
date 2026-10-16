using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-f — AYARLAR → GÖRÜNÜM KARTI · tuval Ayarlar.dc.html (artboard 7)
//
// Tuval notu n4: "her kullanıcı kendi temasını, aydınlık/karanlık tercihini,
// yazı boyutunu (16/18/20 px) ve basit/profesyonel modu seçer; sahip dükkân
// varsayılanını belirler. Kâğıt daima beyaz."
//
// Kilitlenen sözleşmeler:
//   • kart tuvaldeki dört bölümü taşır: 8 tema karosu · 3 aydınlık/karanlık ·
//     3 yazı boyutu · 2 ekran modu · alt satırda iki düğme
//   • her seçim GERÇEK PATCH atar; gövde yalnız DEĞİŞEN alanı taşır
//   • "Dükkân varsayılanına dön" DÖRT alanı TEK istekte null gönderir
//   • "Bunu dükkân varsayılanı yap" yalnız SAHİP; etkin dört değer TEK PATCH
//   • kilitli düğme sebebini yazar; hata bandı ayrı; ham kod ekrana basılmaz
//   • eski dört kart SİLİNMEDİ, katlandı (plan § 3 md.1), id'ler aynen
//   • KÂĞIT DAİMA BEYAZ: şema önizleme sınıfı kâğıt bağlamına giremez
//   • sunucu: arayuzModu kullanıcı ve dükkân uçlarında (koru / null / 400 / 403)
//
// ⚠ Test kiracısı adı "PAKETS1-f-TEST-" önekiyle açılır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>bUnit — kartın ekran sözleşmesi.</summary>
public sealed class S1fGorunumKartiEkranTestleri : BunitContext
{
    private readonly SahteApi sahte = new();

    public S1fGorunumKartiEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped(_ => new HttpClient(sahte) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<SurumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<KullaniciGorunumServisi>();
    }

    /// <summary>
    /// Durumlu sahte sunucu: kullanıcı satırı + dükkân varsayılanı + arayüz modu.
    /// Yanıtlar sunucunun gerçek şeklini taşır ("{}" varsayılanı YANLIŞ YEŞİL
    /// üretirdi). Mod: kendi modu /kullanicilar/ben/arayuz-modu (dar uç),
    /// dükkân modu /admin/tenant, etkin mod /auth/ben.
    /// </summary>
    private sealed class SahteApi : HttpMessageHandler
    {
        public List<(string Yontem, string Yol, string Govde)> Istekler { get; } = [];
        public string? OzelSema { get; set; }
        public string? OzelTema { get; set; }
        public int? OzelYazi { get; set; }
        public string? OzelMod { get; set; }
        public string KiraciSema { get; set; } = "grafit";
        public string KiraciTema { get; set; } = "sistem";
        public int KiraciYazi { get; set; } = 18;
        public string KiraciMod { get; set; } = "PROFESYONEL";
        public bool Bozuk { get; set; }
        public bool PatchReddet { get; set; }
        public bool ModReddet { get; set; }
        public bool AdminReddet { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            Istekler.Add((istek.Method.Method, yol, govde));

            if (yol.EndsWith("/kullanici/gorunum", StringComparison.Ordinal))
            {
                if (Bozuk) return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                if (istek.Method == HttpMethod.Patch)
                {
                    if (PatchReddet)
                        return Json("""{"message":"Bilinmeyen renk şeması: pembe"}""", HttpStatusCode.BadRequest);
                    using var j = JsonDocument.Parse(govde);
                    foreach (var p in j.RootElement.EnumerateObject())
                    {
                        var bos = p.Value.ValueKind == JsonValueKind.Null;
                        switch (p.Name)
                        {
                            case "sema": OzelSema = bos ? null : p.Value.GetString(); break;
                            case "temaModu": OzelTema = bos ? null : p.Value.GetString(); break;
                            case "yaziBoyutu": OzelYazi = bos ? null : p.Value.GetInt32(); break;
                        }
                    }
                    return Json($$"""{"gorunum":{{Govde()}},"mesaj":"Görünüm kaydedildi"}""");
                }
                return Json(Govde());
            }

            if (yol.EndsWith("/kullanicilar/ben/arayuz-modu", StringComparison.Ordinal) && istek.Method == HttpMethod.Patch)
            {
                if (ModReddet) return Json("""{"message":"Mod yazılamadı"}""", HttpStatusCode.InternalServerError);
                using var j = JsonDocument.Parse(govde);
                var m = j.RootElement.GetProperty("arayuzModu");
                OzelMod = m.ValueKind == JsonValueKind.Null ? null : m.GetString();
                return Json($$"""{"arayuzModu":{{S(OzelMod)}},"etkinArayuzModu":"{{OzelMod ?? KiraciMod}}"}""");
            }

            if (yol.EndsWith("/auth/ben", StringComparison.Ordinal))
                return Json($$"""{"id":"ben","arayuzModu":{{S(OzelMod)}},"etkinArayuzModu":"{{OzelMod ?? KiraciMod}}"}""");

            if (yol.EndsWith("/admin/tenant", StringComparison.Ordinal) && istek.Method == HttpMethod.Patch)
            {
                if (AdminReddet) return Json("""{"message":"Bu işlem için yetkiniz yok"}""", HttpStatusCode.Forbidden);
                using var j = JsonDocument.Parse(govde);
                KiraciMod = j.RootElement.GetProperty("varsayilanArayuzModu").GetString()!;
                return Json($$"""{"id":"t1","ad":"Dükkân","varsayilanArayuzModu":"{{KiraciMod}}"}""");
            }

            if (yol.EndsWith("/gorunum", StringComparison.Ordinal))
            {
                if (istek.Method == HttpMethod.Patch)
                {
                    using var j = JsonDocument.Parse(govde);
                    var r = j.RootElement;
                    KiraciSema = r.GetProperty("sema").GetString()!;
                    KiraciYazi = int.Parse(r.GetProperty("yaziOlcegi").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
                    KiraciTema = r.GetProperty("temaModu").GetString()!;
                    return Json($$"""{"gorunum":{{Kiraci()}},"mesaj":"Görünüm kaydedildi"}""");
                }
                return Json(Kiraci());
            }

            return Json("{}");
        }

        private string Kiraci() =>
            $$"""{"sema":"{{KiraciSema}}","yaziOlcegi":"{{KiraciYazi}}","temaModu":"{{KiraciTema}}"}""";

        private static string S(string? v) => v is null ? "null" : $"\"{v}\"";

        private static string Kaynak(bool ozel, bool kiraciFarkli) => ozel ? "kullanici" : kiraciFarkli ? "kiraci" : "sistem";

        private string Govde() =>
            $$"""
            {"temaModu":"{{OzelTema ?? KiraciTema}}","sema":"{{OzelSema ?? KiraciSema}}","yaziBoyutu":{{OzelYazi ?? KiraciYazi}},
             "temaModuKaynagi":"{{Kaynak(OzelTema is not null, KiraciTema != "sistem")}}",
             "semaKaynagi":"{{Kaynak(OzelSema is not null, KiraciSema != "platin")}}",
             "yaziBoyutuKaynagi":"{{Kaynak(OzelYazi is not null, KiraciYazi != 18)}}",
             "ozelTemaModu":{{S(OzelTema)}},"ozelSema":{{S(OzelSema)}},"ozelYaziBoyutu":{{(OzelYazi is null ? "null" : OzelYazi.ToString())}},
             "kiraciTemaModu":"{{KiraciTema}}","kiraciSema":"{{KiraciSema}}","kiraciYaziBoyutu":{{KiraciYazi}},
             "semalar":["platin","zumrut","grafit","bordo","okyanus","menekse","zeytin","fildisi"],
             "temaModlari":["sistem","aydinlik","karanlik"],"yaziBoyutlari":[16,18,20]}
            """;

        private static HttpResponseMessage Json(string g, HttpStatusCode kod = HttpStatusCode.OK) => new(kod)
        {
            Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    private void Oturum(string rol, string? ozelMod = null) =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(new OturumKullanici(
            "ben", "Ramazan", "A.", "ben@ornek.com", rol, null, ozelMod, ozelMod ?? "PROFESYONEL"));

    private async Task<IRenderedComponent<Kuyumcu.Web.Bilesenler.GorunumKarti>> Ciz(string rol = "SAHIP")
    {
        Oturum(rol, sahte.OzelMod);
        await Services.GetRequiredService<KullaniciGorunumServisi>().YukleAsync();
        return Render<Kuyumcu.Web.Bilesenler.GorunumKarti>();
    }

    private List<JsonElement> Patchler(string sonEk) =>
        [.. sahte.Istekler.Where(i => i.Yontem == "PATCH" && i.Yol.EndsWith(sonEk, StringComparison.Ordinal))
            .Select(i => JsonDocument.Parse(i.Govde).RootElement.Clone())];

    // ── S1f-E1) Tuvalin dört bölümü: 8 + 3 + 3 + 2 düğme + alt satır ────────
    [Fact]
    public async Task S1f_E1_Kart_tuvalin_dort_bolumunu_ve_alt_satiri_tasir()
    {
        var e = await Ciz();

        Assert.Equal(9, e.FindAll(".gk-sema-izgara button.gk-sema").Count);   // YILDIZ-1: 8 + "yildiz"
        foreach (var kod in new[] { "platin", "zumrut", "grafit", "bordo", "okyanus", "menekse", "zeytin", "fildisi" })
            Assert.NotNull(e.Find($"#gk-sema-{kod}"));
        foreach (var kod in new[] { "aydinlik", "karanlik", "sistem" })
            Assert.NotNull(e.Find($"#gk-tema-{kod}"));
        foreach (var kod in new[] { "16", "18", "20" })
            Assert.NotNull(e.Find($"#gk-yazi-{kod}"));
        Assert.NotNull(e.Find("#gk-mod-basit"));
        Assert.NotNull(e.Find("#gk-mod-profesyonel"));
        Assert.NotNull(e.Find("#gk-dukkana-don"));
        Assert.NotNull(e.Find("#gk-varsayilan-yap"));

        // Tuval sırası: Aydınlık · Karanlık · Bilgisayara uy.
        var sira = e.FindAll(".gk-uclu section:first-child button").Select(b => b.Id).ToList();
        Assert.Equal(["gk-tema-aydinlik", "gk-tema-karanlik", "gk-tema-sistem"], sira);

        // Tuval metinleri (TR) — sözlükten, bağımsız beklenenle.
        var metin = e.Find("#ayar-gorunum-karti").TextContent;
        Assert.Contains("Renk teması", metin);
        Assert.Contains("8 tema · herkes kendi seçer · işletme varsayılanını sahip belirler", metin);
        Assert.Contains("Fiş ve etiket kâğıdı her zaman beyaz basılır.", metin);
        Assert.Contains("Rakamlar her boyutta bir kademe büyük görünür.", metin);
        Assert.Contains("Hiçbir özellik kapanmaz, yalnız ilk bakışta görünen değişir.", metin);
        Assert.Contains("Değişiklik hemen uygulanır, kaydetmek gerekmez. Bu tercih sadece senin hesabın içindir.", metin);
        Assert.Contains("Bilgisayara uy", metin);
        // KESIF-P1 (#44): kartta ad ("Fildişi") ve niteleyici ("(açık menü)") ayrı satırdadır —
        // 960 px'te ad kelime ortasından kırılmasın diye; sözlükteki tam ad AYNI (bkz. :697).
        Assert.Equal("Fildişi", e.Find("#gk-sema-fildisi .gk-sema-ad").TextContent.Trim());
        Assert.Equal("(açık menü)", e.Find("#gk-sema-fildisi-nitel").TextContent.Trim());
        Assert.Contains("Çok büyük", metin);
        Assert.Contains("20 px", metin);
        Assert.Contains("Tezgâh için: tek fiş, büyük düğmeler", metin);
    }

    // ── S1f-E2) Ham kod EKRANA BASILMAZ (UX-S31) ────────────────────────────
    [Fact]
    public async Task S1f_E2_Ham_kod_ekrana_basilmaz()
    {
        var e = await Ciz();
        var metin = e.Find("#ayar-gorunum-karti").TextContent;
        foreach (var ham in new[] { "platin", "zumrut", "fildisi", "karanlik", "aydinlik", "sistem", "BASIT", "PROFESYONEL", "kiraci", "kullanici" })
            Assert.DoesNotContain(ham, metin, StringComparison.Ordinal);
    }

    // ── S1f-E3) Seçili durum SUNUCUNUN etkin değeridir; tek "Seçili" rozeti ─
    [Fact]
    public async Task S1f_E3_Secili_durum_sunucunun_etkin_degeridir()
    {
        sahte.KiraciSema = "bordo";
        sahte.OzelYazi = 20;
        var e = await Ciz();

        Assert.Equal("true", e.Find("#gk-sema-bordo").GetAttribute("aria-pressed"));
        Assert.Equal("false", e.Find("#gk-sema-platin").GetAttribute("aria-pressed"));
        Assert.Single(e.FindAll(".gk-secili-rozet"));
        Assert.Contains("secili", e.Find("#gk-yazi-20").ClassName ?? "");
        Assert.Equal("true", e.Find("#gk-tema-sistem").GetAttribute("aria-pressed"));

        // Kaynak rozeti (UX-S21): şema dükkândan, punto kullanıcıdan.
        Assert.Equal("İşletme varsayılanı", e.Find("#gk-sema-kaynak").TextContent);
        Assert.Equal("Sizin seçiminiz", e.Find("#gk-yazi-kaynak").TextContent);
    }

    // ── S1f-E4) Tema karosu GERÇEK PATCH atar, gövdede YALNIZ şema ──────────
    [Fact]
    public async Task S1f_E4_Tema_karosu_yalniz_sema_PATCH_atar_ve_bant_yazar()
    {
        var e = await Ciz();
        e.Find("#gk-sema-zumrut").Click();

        e.WaitForAssertion(() => Assert.Single(Patchler("/kullanici/gorunum")));
        var g = Patchler("/kullanici/gorunum")[0];
        Assert.Equal("zumrut", g.GetProperty("sema").GetString());
        Assert.Single(g.EnumerateObject());   // ⚠ öteki alanlar gövdede YOK → sunucuda KORUNUR

        e.WaitForAssertion(() => Assert.Equal("true", e.Find("#gk-sema-zumrut").GetAttribute("aria-pressed")));
        Assert.Contains(Sozlukler.Tr.Gorunum.Kaydedildi, e.Find("#gk-basari").TextContent);
    }

    // ── S1f-E5) Aydınlık/karanlık — kendi alanını gönderir ──────────────────
    [Theory]
    [InlineData("#gk-tema-karanlik", "temaModu", "karanlik")]
    [InlineData("#gk-tema-sistem", "temaModu", "sistem")]
    [InlineData("#gk-tema-aydinlik", "temaModu", "aydinlik")]
    public async Task S1f_E5_Secenek_dugmesi_kendi_alanini_PATCH_eder(string secici, string alan, string deger)
    {
        var e = await Ciz();
        e.Find(secici).Click();

        e.WaitForAssertion(() => Assert.Single(Patchler("/kullanici/gorunum")));
        var g = Patchler("/kullanici/gorunum")[0];
        Assert.Equal(deger, g.GetProperty(alan).GetString());
        Assert.Single(g.EnumerateObject());
    }

    [Fact]
    public async Task S1f_E5b_Yazi_boyutu_SAYI_olarak_gider()
    {
        var e = await Ciz();
        e.Find("#gk-yazi-16").Click();

        e.WaitForAssertion(() => Assert.Single(Patchler("/kullanici/gorunum")));
        var g = Patchler("/kullanici/gorunum")[0];
        Assert.Equal(JsonValueKind.Number, g.GetProperty("yaziBoyutu").ValueKind);
        Assert.Equal(16, g.GetProperty("yaziBoyutu").GetInt32());
    }

    // ── S1f-E6) Mod: DAR "kendi modum" ucu; oturum SUNUCU yanıtından tazelenir
    [Theory]
    [InlineData("#gk-mod-basit", "BASIT")]
    [InlineData("#gk-mod-profesyonel", "PROFESYONEL")]
    public async Task S1f_E6_Mod_secimi_kendi_mod_ucunu_PATCH_eder_ve_oturumu_tazeler(string secici, string mod)
    {
        var e = await Ciz();
        var modu = Services.GetRequiredService<ArayuzModuServisi>();

        e.Find(secici).Click();

        e.WaitForAssertion(() => Assert.Single(Patchler("/kullanicilar/ben/arayuz-modu")));
        var g = Patchler("/kullanicilar/ben/arayuz-modu")[0];
        Assert.Equal(mod, g.GetProperty("arayuzModu").GetString());
        Assert.Single(g.EnumerateObject());
        Assert.Empty(Patchler("/kullanici/gorunum"));   // görünüm satırına dokunulmadı

        e.WaitForAssertion(() => Assert.Equal(mod, modu.KullaniciModu));
        Assert.Equal(mod, modu.EtkinMod);
        e.WaitForAssertion(() => Assert.Equal("true", e.Find(secici).GetAttribute("aria-pressed")));
        Assert.Equal("Sizin seçiminiz", e.Find("#gk-mod-kaynak").TextContent);
    }

    // ── S1f-E7) "Dükkân varsayılanına dön": ÜÇ alan TEK istekte null + mod null
    [Fact]
    public async Task S1f_E7_Dukkana_don_uc_alani_tek_istekte_null_ve_modu_devreder()
    {
        sahte.OzelSema = "zeytin";
        sahte.OzelMod = "BASIT";
        var e = await Ciz();

        Assert.False(e.Find("#gk-dukkana-don").HasAttribute("disabled"));
        e.Find("#gk-dukkana-don").Click();

        e.WaitForAssertion(() => Assert.Single(Patchler("/kullanicilar/ben/arayuz-modu")));
        var g = Patchler("/kullanici/gorunum").Single();
        var alanlar = g.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.ValueKind);
        Assert.Equal(3, alanlar.Count);
        foreach (var ad in new[] { "sema", "temaModu", "yaziBoyutu" })
            Assert.Equal(JsonValueKind.Null, alanlar[ad]);
        Assert.Equal(JsonValueKind.Null, Patchler("/kullanicilar/ben/arayuz-modu")[0].GetProperty("arayuzModu").ValueKind);

        e.WaitForAssertion(() => Assert.Contains(
            "Kendi seçimlerin silindi; artık işletme varsayılanını kullanıyorsun.", e.Find("#gk-basari").TextContent));
        var modu = Services.GetRequiredService<ArayuzModuServisi>();
        Assert.Null(modu.KullaniciModu);
        Assert.False(modu.BasitMi);                 // dükkânınki (PROFESYONEL) /auth/ben'den
        e.WaitForAssertion(() => Assert.True(e.Find("#gk-dukkana-don").HasAttribute("disabled")));
    }

    // ── S1f-E7b) Yalnız mod kendiyse görünüm isteği ATILMAZ ─────────────────
    [Fact]
    public async Task S1f_E7b_Yalniz_mod_kendiyse_gorunum_istegi_atilmaz()
    {
        sahte.OzelMod = "BASIT";
        var e = await Ciz();
        e.Find("#gk-dukkana-don").Click();

        e.WaitForAssertion(() => Assert.Single(Patchler("/kullanicilar/ben/arayuz-modu")));
        Assert.Empty(Patchler("/kullanici/gorunum"));
    }

    // ── S1f-E7c) Mod adımı düşerse "hepsi döndü" DENMEZ; ne olduğu yazılır ──
    [Fact]
    public async Task S1f_E7c_Mod_adimi_duserse_kismi_sonuc_hata_bandinda_yazilir()
    {
        sahte.OzelSema = "zeytin";
        sahte.OzelMod = "BASIT";
        sahte.ModReddet = true;
        var e = await Ciz();
        e.Find("#gk-dukkana-don").Click();

        e.WaitForAssertion(() => Assert.Contains(
            "Renk, aydınlık/karanlık ve yazı boyutu işletme varsayılanına döndü; ekran modu döndürülemedi: Mod yazılamadı",
            e.Find("#gk-hata").TextContent));
        Assert.Empty(e.FindAll("#gk-basari"));
    }

    // ── S1f-E8) Kendi seçimi yokken "dön" KİLİTLİ ve SEBEBİ yazılı (UX-S30) ─
    [Fact]
    public async Task S1f_E8_Kendi_secimi_yokken_don_kilitli_ve_sebepli()
    {
        var e = await Ciz();
        var d = e.Find("#gk-dukkana-don");
        Assert.True(d.HasAttribute("disabled"));
        Assert.Equal("gk-dukkana-don-kilit", d.GetAttribute("aria-describedby"));
        Assert.Equal("Zaten işletme varsayılanını kullanıyorsun; dönülecek kendi seçimin yok.",
            e.Find("#gk-dukkana-don-kilit").TextContent);
    }

    // ── S1f-E9) Varsayılan yap: SAHİP DEĞİLSE kilitli + sebep, istek YOK ────
    [Theory]
    [InlineData("TEZGAHTAR")]
    [InlineData("MUDUR")]
    public async Task S1f_E9_Sahip_degilse_varsayilan_yap_kilitli_ve_sebepli(string rol)
    {
        var e = await Ciz(rol);
        var d = e.Find("#gk-varsayilan-yap");
        Assert.True(d.HasAttribute("disabled"));
        Assert.Equal("İşletme varsayılanını yalnız işletme sahibi belirler.", e.Find("#gk-varsayilan-yap-kilit").TextContent);

        // Kişisel seçim tezgâhtara AÇIK (tuval n4: "her kullanıcı").
        Assert.False(e.Find("#gk-sema-bordo").HasAttribute("disabled"));
        Assert.False(e.Find("#gk-mod-basit").HasAttribute("disabled"));
        Assert.Empty(Patchler("/api/v1/gorunum"));
        Assert.Empty(Patchler("/admin/tenant"));
    }

    // ── S1f-E10) SAHİP: üç görünüm TEK PATCH /gorunum + mod /admin/tenant ──
    [Fact]
    public async Task S1f_E10_Sahip_etkin_degerleri_dukkan_varsayilani_yapar()
    {
        sahte.OzelSema = "okyanus";
        sahte.OzelYazi = 20;
        sahte.OzelTema = "karanlik";
        sahte.OzelMod = "BASIT";
        var e = await Ciz();

        Assert.Null(e.Find("#gk-varsayilan-yap").GetAttribute("disabled"));
        e.Find("#gk-varsayilan-yap").Click();

        e.WaitForAssertion(() => Assert.Single(Patchler("/admin/tenant")));
        var g = Patchler("/api/v1/gorunum").Single();
        Assert.Equal("okyanus", g.GetProperty("sema").GetString());
        Assert.Equal("20", g.GetProperty("yaziOlcegi").GetString());
        Assert.Equal("karanlik", g.GetProperty("temaModu").GetString());
        Assert.Equal(3, g.EnumerateObject().Count());
        var t = Patchler("/admin/tenant")[0];
        Assert.Equal("BASIT", t.GetProperty("varsayilanArayuzModu").GetString());
        Assert.Single(t.EnumerateObject());

        e.WaitForAssertion(() => Assert.Contains(
            "İşletme varsayılanı kaydedildi: kendi seçimini yapmamış herkes bu görünümü görür. (Okyanus · Çok büyük · Karanlık · Basit)",
            e.Find("#gk-basari").TextContent));
        // Kullanıcının KENDİ satırına dokunulmadı.
        Assert.Empty(Patchler("/kullanici/gorunum"));
        Assert.Empty(Patchler("/kullanicilar/ben/arayuz-modu"));
    }

    // ── S1f-E10b) Mod adımı reddedilirse kısmi sonuç HATA bandında ─────────
    [Fact]
    public async Task S1f_E10b_Mod_adimi_reddedilirse_kismi_sonuc_yazilir()
    {
        sahte.AdminReddet = true;
        var e = await Ciz();
        e.Find("#gk-varsayilan-yap").Click();

        e.WaitForAssertion(() => Assert.Contains(
            "Renk, aydınlık/karanlık ve yazı boyutu işletme varsayılanı oldu; ekran modu yazılamadı: Bu işlem için yetkiniz yok",
            e.Find("#gk-hata").TextContent));
        Assert.Empty(e.FindAll("#gk-basari"));
        Assert.Single(Patchler("/api/v1/gorunum"));
    }

    // ── S1f-E11) Okunamadıysa HER seçim kilitli + sebep; tıklama istek atmaz ─
    [Fact]
    public async Task S1f_E11_Okunamadiysa_tum_secimler_kilitli_ve_sebepli()
    {
        sahte.Bozuk = true;
        var e = await Ciz();

        Assert.Equal(Sozlukler.Tr.Gorunum.Yuklenmedi, e.Find("#gk-kilit").TextContent);
        foreach (var b in e.FindAll("#ayar-gorunum-karti button"))
        {
            Assert.True(b.HasAttribute("disabled"), $"{b.Id} kilitli değil");
            Assert.Equal("gk-kilit", b.GetAttribute("aria-describedby"));
        }
        Assert.DoesNotContain(sahte.Istekler, i => i.Yontem == "PATCH");
        // Hata "kayıt yok"a çevrilmedi: seçili gösterilen tahmini bir tema YOK.
        Assert.Empty(e.FindAll(".gk-secili-rozet"));
    }

    // ── S1f-E12) Okuma sürerken "okunamadı" yazılmaz ────────────────────────
    [Fact]
    public void S1f_E12_Okuma_surerken_okunamadi_yazmaz()
    {
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Bilesenler.GorunumKarti>();   // YukleAsync henüz çağrılmadı
        Assert.Equal("Görünüm ayarın okunuyor…", e.Find("#gk-kilit").TextContent);
    }

    // ── S1f-E13) PATCH reddedilirse HATA bandı; başarı bandı YOK (UX-S28) ───
    [Fact]
    public async Task S1f_E13_Patch_reddedilirse_hata_bandi_yazilir()
    {
        var e = await Ciz();
        sahte.PatchReddet = true;
        e.Find("#gk-sema-menekse").Click();

        e.WaitForAssertion(() => Assert.Contains("Bilinmeyen renk şeması: pembe", e.Find("#gk-hata").TextContent));
        Assert.Empty(e.FindAll("#gk-basari"));
        Assert.Equal("false", e.Find("#gk-sema-menekse").GetAttribute("aria-pressed"));
    }

    // ── S1f-E14) DoD EK md.1: HER düğme ya istek atar ya kilitli + sebepli ──
    [Fact]
    public async Task S1f_E14_Her_dugme_ya_istek_atar_ya_kilitli_ve_sebeplidir()
    {
        sahte.OzelSema = "bordo";
        var e = await Ciz();
        var idler = e.FindAll("#ayar-gorunum-karti button").Select(b => b.Id).ToList();
        Assert.Equal(9 + 3 + 3 + 2 + 2, idler.Count);   // YILDIZ-1: 9 şema düğmesi

        foreach (var id in idler)
        {
            var b = e.Find($"#{id}");
            if (b.HasAttribute("disabled"))
            {
                var sebep = b.GetAttribute("aria-describedby");
                Assert.False(string.IsNullOrEmpty(sebep), $"{id} kilitli ama sebebi yok");
                Assert.False(string.IsNullOrWhiteSpace(e.Find($"#{sebep}").TextContent));
                continue;
            }
            var once = sahte.Istekler.Count(i => i.Yontem == "PATCH");
            b.Click();
            e.WaitForAssertion(() => Assert.True(sahte.Istekler.Count(i => i.Yontem == "PATCH") > once, $"{id} istek atmadı"));
        }
    }

    // ── S1f-E15) /ayarlar: yeni kart VAR, eski dört kart KATLANDI, id'ler aynı
    [Fact]
    public async Task S1f_E15_Ayarlar_yeni_karti_tasir_eski_kartlar_katlanmis_yasiyor()
    {
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Pages.Ayarlar>();
        await Task.CompletedTask;

        Assert.NotNull(e.Find("#ayar-gorunum-karti"));
        var ayrinti = e.Find("details#ayar-gorunum-ayrinti");
        Assert.False(ayrinti.HasAttribute("open"));   // katlı başlar
        // KESIF-P1 · TEMA-TEK: "ayar-tema" ve "benim-tema" (ikinci/üçüncü tema seçici) kaldırıldı.
        foreach (var id in new[] { "ayar-benim-gorunum", "ayar-arayuz-modu",
                                   "benim-sema", "benim-tema-notu", "benim-yazi",
                                   "arayuz-devral", "arayuz-basit", "arayuz-profesyonel" })
            Assert.NotNull(ayrinti.QuerySelector($"#{id}"));
        // PAKET T-K3: dükkânın görünüm VARSAYILANI kartı (#ayar-gorunum) tuvaldeki
        // "Dükkân" sekmesine taşındı — silinmedi, seçicileri aynı id'lerle yaşıyor.
        foreach (var id in new[] { "ayar-gorunum", "gorunum-sema", "gorunum-olcek" })
            Assert.NotNull(e.Find($"#ayar-panel-dukkan #{id}"));

        // Yeni kart katlanmış bölümün İÇİNDE değil, önünde.
        Assert.Null(ayrinti.QuerySelector("#ayar-gorunum-karti"));
        // PAKET T-K3 düzeltmesi: dükkân varsayılanı artık bu katlanmış bölümde
        // DEĞİL (Dükkân sekmesinde) — başlık onu vaat etmez.
        // KESIF-P1 · TEMA-TEK: cihaz tema kartı kalktı → başlık "bu cihaz" VAAT ETMEZ.
        Assert.Equal("Ayrıntılı görünüm ayarları (tek tek seçim)",
            e.Find("#ayar-gorunum-ayrinti-baslik").TextContent);
        Assert.DoesNotContain("cihaz", e.Find("#ayar-gorunum-ayrinti-baslik").TextContent);
        Assert.DoesNotContain("işletme", e.Find("#ayar-gorunum-ayrinti-baslik").TextContent);
    }

    // ── S1f-E16) İngilizce: metin sözlükten, Türkçe sızmaz ──────────────────
    [Fact]
    public async Task S1f_E16_Ingilizce_metinler_sozlukten_gelir()
    {
        var e = await Ciz();
        await Services.GetRequiredService<DilServisi>().DegistirAsync("en");
        e.WaitForAssertion(() => Assert.Contains("Colour theme", e.Find("#gk-tema-baslik").TextContent));
        Assert.Equal("Back to shop default", e.Find("#gk-dukkana-don").TextContent);
        Assert.Contains("Follow this device", e.Find("#gk-tema-sistem").TextContent);
    }
}

/// <summary>Saf — kaynak, CSS, belirteç ve kâğıt sözleşmesi.</summary>
public sealed class S1fGorunumKaynakTestleri
{
    private static string Kok()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        Assert.True(d is not null, "Kuyumcu.sln bulunamadı.");
        return d!.FullName;
    }

    private static string Oku(params string[] yol) => File.ReadAllText(Path.Combine([Kok(), .. yol]));

    // ── S1f-K1) Razor: satır içi style YOK, script YOK, ApiIstemci YOK ─────
    [Fact]
    public void S1f_K1_Kart_razor_satir_ici_stil_script_ve_ApiIstemci_icermez()
    {
        var razor = Oku("Kuyumcu.Web", "Bilesenler", "GorunumKarti.razor");
        Assert.DoesNotContain("style=\"", razor);
        Assert.DoesNotContain("<script", razor, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@inject ApiIstemci", razor);
        var kod = Regex.Replace(razor, @"@\*.*?\*@", "", RegexOptions.Singleline);
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{6}\b", kod);
    }

    // ── S1f-K2) CSS bloğu: tuval ölçüleri + YALNIZ belirteç ─────────────────
    [Fact]
    public void S1f_K2_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir()
    {
        var css = Oku("Kuyumcu.Web", "wwwroot", "css", "app.css");
        var i = css.IndexOf("S1-f — AYARLAR → GÖRÜNÜM KARTI", StringComparison.Ordinal);
        Assert.True(i > 0, "app.css'te S1-f bloğu yok.");
        // Blok başlığı bir yorumun ORTASINDA başlar: yorumun kapanışından sonrası ölçülür.
        var bas = css.IndexOf("*/", i, StringComparison.Ordinal) + 2;
        // Blok, bir SONRAKİ blok başlığında ("/* ═══") biter — sonra eklenen
        // başka paketlerin CSS'i bu paketin kurallarına tabi tutulmaz.
        var son = css.IndexOf("\n/* ═══", bas, StringComparison.Ordinal);
        var kod = Regex.Replace(son < 0 ? css[bas..] : css[bas..son], @"/\*.*?\*/", "", RegexOptions.Singleline);

        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgba?\(", kod);
        Assert.DoesNotContain("overflow", kod);
        Assert.DoesNotContain("@media print", kod);
        Assert.Contains("padding: 22px 24px; border: 1px solid var(--ayrac); border-radius: 12px;", kod);
        Assert.Contains(".gk-bolum-baslik { margin: 0; font-size: 20px; font-weight: 600;", kod);
        Assert.Contains(".gk-sema-izgara { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; }", kod);
        Assert.Contains(".gk-sema.secili { border: 3px solid var(--vurgu); }", kod);
        Assert.Contains(".gk-onizleme-menu { width: 34%; background: var(--kenar-yuzey); }", kod);
        Assert.Contains(".gk-uclu { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 20px; }", kod);
        Assert.Contains("height: 52px; padding: 0 16px;", kod);
        Assert.Contains(".gk-secenek.secili { border: 2px solid var(--vurgu); }", kod);
        Assert.Contains(".gk-yazi-20 { font-size: 20px; }", kod);
        Assert.Contains(".gk-alt-dugme { height: 50px; min-height: 50px; padding: 0 20px; border-radius: 9px; font-size: 16px; }", kod);
        Assert.Contains("background: var(--vurgu-yuzey); color: var(--vurgu); font-size: 13px; font-weight: 500;", kod);
    }

    // ── S1f-K3) 8 AYDINLIK şema bloğu önizleme seçicisini taşır; karanlık TAŞIMAZ
    [Fact]
    public void S1f_K3_Sekiz_aydinlik_sema_blogu_onizleme_secicisini_tasir()
    {
        var css = Oku("Kuyumcu.Web", "wwwroot", "css", "belirtecler.css");
        // YILDIZ-1: "yildiz" AYRI tema dosyasında yaşar (tema-yildiz.css); önizleme
        // seçicisini orada taşıması Yildiz1Testleri'nde kilitlidir.
        foreach (var sema in GorunumServisi.Semalar.Where(x => x != "yildiz"))
        {
            var desen = new Regex(
                $@"(?m)^:root\[data-sema=""{sema}""\][^{{\n]*\.gorunum-sema-onizleme\[data-sema=""{sema}""\] \{{");
            Assert.True(desen.IsMatch(css), $"'{sema}' aydınlık bloğu önizleme seçicisini taşımıyor.");
        }
        foreach (var b in CssKaynak.Bildirimler(css).Where(b => b.Secici.Contains("gorunum-sema-onizleme", StringComparison.Ordinal)))
        {
            Assert.DoesNotContain("karanlik", b.Secici);
            Assert.DoesNotContain("prefers-color-scheme", b.AtKural);
        }
        // Önizlemenin kullandığı dört belirteç 8 şemanın HER birinde tanımlı.
        var t = BelirtecTablosu.Yukle();
        foreach (var sema in GorunumServisi.Semalar.Where(x => x != "yildiz"))
            foreach (var belirtec in new[] { "--kenar-yuzey", "--yuzey-govde", "--yuzey", "--vurgu", "--ayrac" })
                Assert.True(t.Semalar[sema].ContainsKey(belirtec), $"{sema} {belirtec} tanımlamıyor");
    }

    // ── S1f-K4) KÂĞIT DAİMA BEYAZ ──────────────────────────────────────────
    //   (a) yazdırmada gövde zemini sabit beyaz; (b) hiçbir kâğıt bildirimi
    //   şema bloğunun tanımladığı bir belirtece başvurmaz; (c) önizleme sınıfı
    //   ve kartın sınıfları kâğıt bağlamına (print/.belge/.urun-etiket) girmez.
    [Fact]
    public void S1f_K4_Kagit_daima_beyaz_sema_kagida_sizamaz()
    {
        var app = Oku("Kuyumcu.Web", "wwwroot", "css", "app.css");
        var bil = CssKaynak.Bildirimler(app).ToList();

        Assert.Contains(bil, b => b.AtKural.Contains("print", StringComparison.Ordinal)
                                  && b.Secici.Contains("body", StringComparison.Ordinal)
                                  && b.Ozellik == "background" && b.Deger.Trim() == "#fff");

        var semaBelirtecleri = BelirtecTablosu.Yukle().Semalar.Values
            .SelectMany(s => s.Keys).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("--kenar-yuzey", semaBelirtecleri);

        static bool Kagit(CssKaynak.Bildirim b) =>
            b.AtKural.Contains("print", StringComparison.OrdinalIgnoreCase)
            || b.Secici.Contains(".belge", StringComparison.Ordinal)
            || b.Secici.Contains(".urun-etiket", StringComparison.Ordinal);

        var sizinti = new List<string>();
        foreach (var b in bil.Where(Kagit))
        {
            foreach (Match m in Regex.Matches(b.Deger, @"var\(\s*(--[a-z0-9-]+)"))
                if (semaBelirtecleri.Contains(m.Groups[1].Value)) sizinti.Add($"{b.Secici} {b.Ozellik}: {b.Deger}");
            if (b.Secici.Contains("gk-", StringComparison.Ordinal) || b.Secici.Contains("gorunum-sema-onizleme", StringComparison.Ordinal))
                sizinti.Add($"{b.Secici} kâğıt bağlamında");
        }
        Assert.True(sizinti.Count == 0, "Şema kâğıda sızıyor:\n  " + string.Join("\n  ", sizinti));

        var bel = Oku("Kuyumcu.Web", "wwwroot", "css", "belirtecler.css");
        Assert.DoesNotContain(CssKaynak.Bildirimler(bel), b =>
            b.AtKural.Contains("print", StringComparison.OrdinalIgnoreCase) && b.Secici.Contains("data-sema", StringComparison.Ordinal));
    }

    // ── S1f-K5) Sözlük: TR/EN her alan dolu, EN TR'nin kopyası DEĞİL ────────
    [Fact]
    public void S1f_K5_Kart_sozlugu_TR_EN_dolu_ve_farkli()
    {
        var ozellikler = typeof(GorunumKartiMetinleri).GetProperties();
        Assert.True(ozellikler.Length >= 20);
        var ayni = 0;
        foreach (var p in ozellikler)
        {
            var tr = (string)p.GetValue(GorunumKartiSozlugu.Tr)!;
            var en = (string)p.GetValue(GorunumKartiSozlugu.En)!;
            Assert.False(string.IsNullOrWhiteSpace(tr), p.Name);
            Assert.False(string.IsNullOrWhiteSpace(en), p.Name);
            if (tr == en) ayni++;
        }
        Assert.True(ayni <= 1, $"{ayni} alan TR=EN");   // yalnız "{0} px" ortak
        Assert.Same(GorunumKartiSozlugu.En, GorunumKartiSozlugu.Al("en"));
        Assert.Same(GorunumKartiSozlugu.Tr, GorunumKartiSozlugu.Al("tr"));
    }

    // ── S1f-K6) Adlar tuvaldeki gibi ve TEK kaynakta (UX-S31) ───────────────
    [Fact]
    public void S1f_K6_Tema_modu_ve_punto_adlari_tuvaldeki_gibidir()
    {
        Assert.Equal("Bilgisayara uy", GorunumSozlugu.Ad(GorunumSozlugu.TemaModlari, "sistem", "tr"));
        Assert.Equal("Normal", GorunumSozlugu.Ad(GorunumSozlugu.YaziBoyutlari, "16", "tr"));
        Assert.Equal("Büyük", GorunumSozlugu.Ad(GorunumSozlugu.YaziBoyutlari, "18", "tr"));
        Assert.Equal("Çok büyük", GorunumSozlugu.Ad(GorunumSozlugu.YaziBoyutlari, "20", "tr"));
        Assert.Equal("Fildişi (açık menü)", GorunumSozlugu.Ad(GorunumSozlugu.Semalar, "fildisi", "tr"));
        Assert.Empty(GorunumSozlugu.AnahtarEksigi(GorunumSozlugu.Semalar, GorunumServisi.Semalar));
    }
}

/// <summary>
/// Gerçek HTTP — kartın kullandığı uçların sözleşmesi. Görünüm uçları B-1'in;
/// tek yeni uç DAR "kendi modum" ucudur (KullaniciModulu.cs — denetim.sh
/// "ArayuzModu yalnız 6 dosya" kuralı korunur).
/// </summary>
[Collection("http")]
public sealed class S1fGorunumHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static async Task<JsonElement> Oku(HttpResponseMessage c)
    {
        var j = (await c.Content.ReadFromJsonAsync<JsonElement>())!;
        return j.TryGetProperty("gorunum", out var ic) ? ic : j;
    }

    // ── S1f-H1) "Dön" gövdesi: üç null TEK istekte üçünü temizler; mod AYRI ─
    [Fact]
    public async Task S1f_H1_Uc_null_tek_istekte_uc_alani_temizler_mod_ayri_kalir()
    {
        var k = YeniKiraci("PAKETS1-f-TEST-don");
        var i = f.Ortam.Istemci(k, "TEZGAHTAR");
        await i.PatchAsJsonAsync("/api/v1/kullanici/gorunum", new { sema = "bordo", temaModu = "karanlik", yaziBoyutu = 20 });
        Assert.Equal(HttpStatusCode.OK, (await i.PatchAsJsonAsync("/api/v1/kullanicilar/ben/arayuz-modu", new { arayuzModu = "BASIT" })).StatusCode);

        var c = await i.PatchAsJsonAsync("/api/v1/kullanici/gorunum",
            new { temaModu = (string?)null, sema = (string?)null, yaziBoyutu = (int?)null });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var g = await Oku(c);
        Assert.Equal(JsonValueKind.Null, g.GetProperty("ozelSema").ValueKind);
        Assert.Equal(JsonValueKind.Null, g.GetProperty("ozelTemaModu").ValueKind);
        Assert.Equal(JsonValueKind.Null, g.GetProperty("ozelYaziBoyutu").ValueKind);

        using (var db = f.Vt.Olustur(k.TenantId))
        {
            var s = await db.Kullanicilar.AsNoTracking().SingleAsync(x => x.Id == k.KullaniciId);
            Assert.Null(s.Sema);
            Assert.Null(s.TemaModu);
            Assert.Null(s.YaziBoyutu);
            Assert.Equal("BASIT", s.ArayuzModu);     // ⚠ görünüm ucu modu SİLMEZ → kart ikinci isteği atar
        }

        // İkinci adım: mod devredilir → etkin mod dükkânınki.
        Assert.Equal(HttpStatusCode.OK, (await i.PatchAsJsonAsync("/api/v1/kullanicilar/ben/arayuz-modu", new { arayuzModu = (string?)null })).StatusCode);
        var ben = await (await i.GetAsync("/api/v1/auth/ben")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PROFESYONEL", ben.GetProperty("etkinArayuzModu").GetString());
        Assert.Equal(JsonValueKind.Null, ben.GetProperty("arayuzModu").ValueKind);
    }

    // ── S1f-H2) "Bunu dükkân varsayılanı yap": SAHİP → /gorunum + /admin/tenant
    [Fact]
    public async Task S1f_H2_Sahip_uc_gorunumu_ve_modu_dukkan_varsayilani_yapar()
    {
        var k = YeniKiraci("PAKETS1-f-TEST-yap");
        var sahip = f.Ortam.Istemci(k, "SAHIP");

        var c = await sahip.PatchAsJsonAsync("/api/v1/gorunum",
            new { sema = "bordo", yaziOlcegi = "20", temaModu = "karanlik" });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var g = await Oku(c);
        Assert.Equal("bordo", g.GetProperty("sema").GetString());
        Assert.Equal("20", g.GetProperty("yaziOlcegi").GetString());
        Assert.Equal("karanlik", g.GetProperty("temaModu").GetString());

        var t = await sahip.PatchAsJsonAsync("/api/v1/admin/tenant", new { varsayilanArayuzModu = "BASIT" });
        Assert.Equal(HttpStatusCode.OK, t.StatusCode);
        var tj = await t.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("BASIT", tj.GetProperty("varsayilanArayuzModu").GetString());   // kart bu alanı okur

        // Kendi seçimi olmayan kullanıcı DEVRALIR (görünüm + mod).
        var ben = await (await sahip.GetAsync("/api/v1/auth/ben")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("BASIT", ben.GetProperty("etkinArayuzModu").GetString());
        var kg = await Oku(await sahip.GetAsync("/api/v1/kullanici/gorunum"));
        Assert.Equal("bordo", kg.GetProperty("sema").GetString());
        Assert.Equal("kiraci", kg.GetProperty("semaKaynagi").GetString());

        using var db = f.Vt.Olustur(k.TenantId);
        var d = await db.DenetimKayitlari.AsNoTracking()
            .Where(x => x.VarlikTuru == Kuyumcu.Api.Modules.Gorunum.GorunumServisi.Varlik && x.VarlikId == k.TenantId)
            .Select(x => x.Detay).ToListAsync();
        Assert.Contains(d, x => x != null && x.Contains("renk şeması") && x.Contains("tema modu"));
    }

    // ── S1f-H3) YETKİ: tezgâhtar dükkân varsayılanını YAZAMAZ (iki uç 403) ──
    [Fact]
    public async Task S1f_H3_Tezgahtar_dukkan_varsayilanini_yazamaz()
    {
        var k = YeniKiraci("PAKETS1-f-TEST-403");
        var i = f.Ortam.Istemci(k, "TEZGAHTAR");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await i.PatchAsJsonAsync("/api/v1/gorunum", new { sema = "bordo", yaziOlcegi = "20", temaModu = "karanlik" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await i.PatchAsJsonAsync("/api/v1/admin/tenant", new { varsayilanArayuzModu = "BASIT" })).StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.Equal("PROFESYONEL", await db.Tenants.AsNoTracking()
            .Where(t => t.Id == k.TenantId).Select(t => t.VarsayilanArayuzModu).SingleAsync());
        Assert.False(await db.KiraciAyarlari.AsNoTracking().AnyAsync(a => a.TenantId == k.TenantId));
    }

    // ── S1f-H4) Tezgâhtar KENDİ görünümünü ve modunu yazar (tuval n4) ───────
    [Fact]
    public async Task S1f_H4_Tezgahtar_kendi_gorunumunu_ve_modunu_yazar()
    {
        var k = YeniKiraci("PAKETS1-f-TEST-kendi");
        var i = f.Ortam.Istemci(k, "TEZGAHTAR");
        Assert.Equal(HttpStatusCode.OK, (await i.PatchAsJsonAsync("/api/v1/kullanici/gorunum", new { sema = "menekse" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await i.PatchAsJsonAsync("/api/v1/kullanicilar/ben/arayuz-modu", new { arayuzModu = "BASIT" })).StatusCode);

        using var db = f.Vt.Olustur(k.TenantId);
        var s = await db.Kullanicilar.AsNoTracking().SingleAsync(x => x.Id == k.KullaniciId);
        Assert.Equal("menekse", s.Sema);
        Assert.Equal("BASIT", s.ArayuzModu);
    }

    // ── S1f-H5) ESKİ yol tezgâhtara 403 — DAR uç 200 (sapmanın gerekçesi) ───
    [Fact]
    public async Task S1f_H5_Tezgahtar_eski_yolda_yalniz_mod_200_dar_ucta_200_alir()
    {
        var k = YeniKiraci("PAKETS1-f-TEST-dar");
        var i = f.Ortam.Istemci(k, "TEZGAHTAR");

        // PAKET ISLEM-BANT (KUR-BANT § 0.7/2): üst çubuk bu yolu çağırıyordu ve 403 alıyordu. Artık
        // "kullanici" modülü olmayan rol bu yolda YALNIZ kendi modunu yazar (başka alan 403 — IB21).
        var eski = await i.PatchAsJsonAsync("/api/v1/kullanicilar/ben", new { arayuzModu = "BASIT" });
        Assert.Equal(HttpStatusCode.OK, eski.StatusCode);
        var ad = await i.PatchAsJsonAsync("/api/v1/kullanicilar/ben", new { ad = "Baska" });
        Assert.Equal(HttpStatusCode.Forbidden, ad.StatusCode);

        var c = await i.PatchAsJsonAsync("/api/v1/kullanicilar/ben/arayuz-modu", new { arayuzModu = "BASIT" });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var j = await c.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("BASIT", j.GetProperty("arayuzModu").GetString());
        Assert.Equal("BASIT", j.GetProperty("etkinArayuzModu").GetString());

        var ben = await (await i.GetAsync("/api/v1/auth/ben")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("BASIT", ben.GetProperty("etkinArayuzModu").GetString());
    }

    // ── S1f-H6) null = DEVRAL: etkin mod dükkânınki ─────────────────────────
    [Fact]
    public async Task S1f_H6_Null_gonderilince_dukkan_modu_devralinir()
    {
        var k = YeniKiraci("PAKETS1-f-TEST-mod-null");
        await f.Ortam.Istemci(k, "SAHIP").PatchAsJsonAsync("/api/v1/admin/tenant", new { varsayilanArayuzModu = "BASIT" });
        var i = f.Ortam.Istemci(k, "SAHIP");
        await i.PatchAsJsonAsync("/api/v1/kullanicilar/ben/arayuz-modu", new { arayuzModu = "PROFESYONEL" });

        var c = await i.PatchAsJsonAsync("/api/v1/kullanicilar/ben/arayuz-modu", new { arayuzModu = (string?)null });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var j = await c.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, j.GetProperty("arayuzModu").ValueKind);
        Assert.Equal("BASIT", j.GetProperty("etkinArayuzModu").GetString());

        using var db = f.Vt.Olustur(k.TenantId);
        Assert.Null((await db.Kullanicilar.AsNoTracking().SingleAsync(x => x.Id == k.KullaniciId)).ArayuzModu);
    }

    // ── S1f-H7) Beyaz liste + boş gövde 400 ─────────────────────────────────
    [Theory]
    [InlineData("""{"arayuzModu":"basit"}""")]
    [InlineData("""{"arayuzModu":"UZMAN"}""")]
    [InlineData("""{}""")]
    public async Task S1f_H7_Gecersiz_ya_da_bos_govde_400_doner(string govde)
    {
        var k = YeniKiraci("PAKETS1-f-TEST-400");
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").PatchAsync("/api/v1/kullanicilar/ben/arayuz-modu",
            new StringContent(govde, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
    }

    // ── S1f-H8) YALNIZ kendi satırı + YALNIZ mod; denetim kaydı yazılır ─────
    [Fact]
    public async Task S1f_H8_Yalniz_kendi_satiri_ve_yalniz_mod_yazilir_denetim_kaydi_var()
    {
        var k = YeniKiraci("PAKETS1-f-TEST-dar-kapsam");
        var i = f.Ortam.Istemci(k, "TEZGAHTAR");
        string? subeOnce; string emailOnce, rolOnce;
        using (var db = f.Vt.Olustur(k.TenantId))
        {
            var s0 = await db.Kullanicilar.AsNoTracking().SingleAsync(x => x.Id == k.KullaniciId);
            subeOnce = s0.SubeId; emailOnce = s0.Email; rolOnce = s0.Rol;
        }

        // Gövdeye başka alanlar da konsa bile YALNIZ arayuz_modu yazılır.
        var c = await i.PatchAsync("/api/v1/kullanicilar/ben/arayuz-modu", new StringContent(
            """{"arayuzModu":"BASIT","subeId":"BASKA-SUBE","email":"x@y.z","id":"BASKASI","rol":"SAHIP"}""",
            System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);

        using var db2 = f.Vt.Olustur(k.TenantId);
        var s = await db2.Kullanicilar.AsNoTracking().SingleAsync(x => x.Id == k.KullaniciId);
        Assert.Equal("BASIT", s.ArayuzModu);
        Assert.Equal(subeOnce, s.SubeId);
        Assert.Equal(emailOnce, s.Email);
        Assert.Equal(rolOnce, s.Rol);
        Assert.True(await db2.DenetimKayitlari.AsNoTracking().AnyAsync(d =>
            d.VarlikTuru == nameof(Kullanici) && d.VarlikId == k.KullaniciId && d.Detay == "arayuz-modu: BASIT"));
    }

    // ── S1f-H9) Jetonsuz 401 ────────────────────────────────────────────────
    [Fact]
    public async Task S1f_H9_Jetonsuz_istek_401_doner()
    {
        var c = await f.Ortam.AnonimIstemci().PatchAsJsonAsync("/api/v1/kullanicilar/ben/arayuz-modu", new { arayuzModu = "BASIT" });
        Assert.Equal(HttpStatusCode.Unauthorized, c.StatusCode);
    }
}
