using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET TOPLU-1 — toplu tasarım kuyruğu (30.09–01.10) testleri.
//   Saf/CSS: Secici P7 kuralı (KRİTİK), TARIH-TEK sayacı + Bicim işletme saati,
//            kasa bakiye mesajı biçimi, ortak.css kuralları.
//   bUnit:   Faturalar (kilitli toplam salt okunur · nötr satır silme notu · fişsiz satış
//            fişten fatura · işlem seçici işletme tarihi), Cariler (tek uyuşmazlık uyarısı,
//            iptal satırı), Vezne (BASİT katlı notlar, binlikli fiyat, toplam), Gider (tek kategori).
//   HTTP:    fişsiz satış faturalanabilir listede fisId null gelir ve hareketId ile faturalanır.
// ═══════════════════════════════════════════════════════════════════════════

internal static class Toplu1Ortak
{
    public static string WebKok()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "Kuyumcu.Web"))) d = d.Parent;
        return Path.Combine(d!.FullName, "Kuyumcu.Web");
    }

    public static string Css(string ad) => File.ReadAllText(Path.Combine(WebKok(), "wwwroot", "css", ad));

    /// <summary>Yorumları atılmış CSS'te seçicinin bildirim bloğu (yoksa null).</summary>
    public static string? Blok(string css, string secici)
    {
        var temiz = Regex.Replace(css, @"/\*.*?\*/", "", RegexOptions.Singleline);
        var i = temiz.IndexOf(secici + " {", StringComparison.Ordinal);
        if (i < 0) return null;
        var a = temiz.IndexOf('{', i); var k = temiz.IndexOf('}', a);
        return temiz[(a + 1)..k];
    }

    public static HttpResponseMessage Yanit(string json, HttpStatusCode kod = HttpStatusCode.OK) =>
        new(kod) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
}

public sealed class Toplu1SafTestleri
{
    // ── (1) KRİTİK: Secici P7 kuralı her Secici'de — sayfa sabit boy kuralını özgüllükle aşar ──
    [Fact]
    public void Secici_P7_kurali_sayfa_sabit_boyunu_asar_kutu_buyur_kirpmaz()
    {
        var css = Toplu1Ortak.Css("ortak.css");
        var blok = Toplu1Ortak.Blok(css, ".secici-kap > .secici-tetik:has(> .secici-metin--tam):not(#_):not(#__)");
        Assert.NotNull(blok);
        Assert.Matches(@"height:\s*auto", blok);
        Assert.Matches(@"overflow:\s*visible", blok);
        Assert.DoesNotContain("!important", blok);
        // Özgüllük: 2 kimlik (:not(#_):not(#__)) > PRO satış kuralı `html[data-mod] .tuval-pro #islem-ust-satir .secici-tetik` (1 kimlik).
        Assert.Equal(2, Regex.Matches(".secici-kap > .secici-tetik:has(> .secici-metin--tam):not(#_):not(#__)", @":not\(#").Count);
        var metin = Toplu1Ortak.Blok(css, ".secici-kap > .secici-tetik > .secici-metin--tam:not(#_):not(#__)");
        Assert.NotNull(metin);
        Assert.Matches(@"line-height:\s*1\.2", metin);
    }

    [Fact]
    public void Satis_PRO_dar_seritte_odeme_ozeti_icerik_tabani_alir()
    {
        var css = Toplu1Ortak.Css("tuval-satis.css");
        var i = css.IndexOf("@media (max-width: 1099px)", StringComparison.Ordinal);
        Assert.True(i >= 0);
        var medya = css[i..css.IndexOf("\n}", i, StringComparison.Ordinal)];
        Assert.Contains("html[data-mod] .islem-sayfa.tuval-pro #islem-alt-satir > .fis-pro-odeme-ozet-sar { flex: 1 1 auto; }", medya);
    }

    [Fact]
    public void Ortak_css_TOPLU1_kurallari_var()
    {
        var css = Toplu1Ortak.Css("ortak.css");
        Assert.Matches(@"font-family:\s*inherit", Toplu1Ortak.Blok(css, "textarea") ?? "");
        Assert.Contains(".form-toplam-satir", css);
        // GRID-1 tur 3: /cariler kartı ORTAK IZGARANIN tek kart kalıbına devredildi (css/izgara.css);
        // sayfaya özgü #cari-liste-sar kart kuralı kalmadı (kimlik özgüllüğü ortak kalıbı eziyordu).
        Assert.DoesNotContain("#cari-liste-sar > .veri-tablosu:not(.musteri-tablo) > tbody > tr", css);
        Assert.Contains("td[data-etiket]::before", Toplu1Ortak.Css("izgara.css"));
        Assert.Contains("#mut-ozet-tablo { min-width: 0; width: 100%; }", css);
        Assert.Contains(".musteri-sade .sayfa-basi:has(#cari-tur-filtre) { align-items: flex-end; }", css);
    }

    // ── (2) TARIH-TEK ──
    [Fact]
    public void TARIH_TEK_isletme_saati_UTC_gece_yarisi_ertesi_gun()
    {
        // 30.09 22:30 UTC = 01.10 01:30 TSİ (Europe/Istanbul, UTC+3). Kind Unspecified da UTC sayılır.
        var an = new DateTime(2026, 9, 30, 22, 30, 0, DateTimeKind.Unspecified);
        Assert.Equal("01.10.2026 01:30", Bicim.TarihSaat(an));
        Assert.Equal("01.10.2026", Bicim.TarihTam(an));
        Assert.Equal("01:30", Bicim.IsletmeSaati(an));
        Assert.Equal("01.10.2026 01:30:00", Bicim.TarihSaatSaniye(an));
        Assert.Equal("01.10", Bicim.IsletmeBicim(an, "dd.MM"));
        Assert.Equal(new DateOnly(2026, 10, 1), Bicim.IsletmeGunu(an));
        // Takvim günü ÇEVRİLMEZ.
        Assert.Equal("30.09.2026", Bicim.Gun(new DateTime(2026, 9, 30, 22, 30, 0)));
        Assert.Equal("30.09.2026", Bicim.Gun(new DateOnly(2026, 9, 30)));
        Assert.Equal("2026-09-30", Bicim.Makine((DateTime?)new DateTime(2026, 9, 30)));
        Assert.Null(Bicim.Makine((DateOnly?)null));
    }

    /// <summary>Tur 2 (tasarım hakemi KRİTİK, BASİT 1440): bakiye rozeti müşteri kutusunun İÇİNE bindirilmez —
    /// kendi satırında (grid-row 2), ad kutunun tamamını kullanır. Canlı ölçüm: 86 karakterlik adla K9=0.</summary>
    [Fact]
    public void T2_BASIT_bakiye_rozeti_kutuya_bindirilmez_kendi_satirinda()
    {
        var b = Toplu1Ortak.Blok(Toplu1Ortak.Css("tuval-satis.css"), ".tuval-tezgah .fis-ust-satir > .alan:first-child > .rozet-satir");
        Assert.NotNull(b);
        Assert.Contains("grid-row: 2", b);
        Assert.DoesNotContain("justify-self: end", b);
        Assert.DoesNotContain("z-index", b);
    }

    /// <summary>Tur 2 (tasarım hakemi KRİTİK): /raporlar/kasa "Geçmiş kayıtlar" gün anahtarı tek kalıpla
    /// ("30.09.2026" altında "1.10.2026" çıkıyordu) ve takvim günü saat dilimine çevrilmeden basılır.</summary>
    [Fact]
    public void T2_Gun_sonu_gecmis_gun_anahtari_tek_kalip()
    {
        var sayfa = new Kuyumcu.Web.Pages.KasaRaporu();
        var m = typeof(Kuyumcu.Web.Pages.KasaRaporu).GetMethod("GunMetni",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)!;
        Assert.Equal("01.10.2026", m.Invoke(sayfa, ["2026-10-01"]));
        Assert.Equal("30.09.2026", m.Invoke(sayfa, ["2026-09-30"]));
        Assert.Equal("bozuk", m.Invoke(sayfa, ["bozuk"]));
    }

    /// <summary>Tur 2 (canlı ölçüm K3 1/9): seçenek alt bilgisi sarar, rozet tek satır.</summary>
    [Fact]
    public void T2_Secici_secenek_alt_bilgi_sarar_rozet_tek_satir()
    {
        var css = Toplu1Ortak.Css("ortak.css");
        Assert.Contains("white-space: normal", Toplu1Ortak.Blok(css, ".secici-secenek .secici-alt:not(#_)"));
        Assert.Contains("white-space: nowrap", Toplu1Ortak.Blok(css, ".secici-secenek .secici-rozet:not(#_)"));
    }

    /// <summary>Tur 3 (orkestratör 12:45): uzun ad kuralı BİLEŞEN düzeyinde TEK yerde — seçili değer en çok iki
    /// satır, fazlası "…" (line-clamp; tam ad Secici.razor title'ında). Sayfaya özgü uzun-ad düzeltmesi yok.</summary>
    [Fact]
    public void T3_Secici_en_cok_iki_satir_uc_nokta_bilesen_kurali()
    {
        var css = Toplu1Ortak.Css("ortak.css");
        var b = Toplu1Ortak.Blok(css, ".secici-kap > .secici-tetik > .secici-metin--tam:not(#_):not(#__)");
        Assert.NotNull(b);
        Assert.Matches(@"-webkit-line-clamp:\s*2;", b);
        Assert.Matches(@"display:\s*-webkit-box;", b);
        Assert.Matches(@"-webkit-box-orient:\s*vertical;", b);
        Assert.Matches(@"overflow:\s*hidden;", b);
        Assert.Matches(@"line-height:\s*1\.2;", b);
        // İki satırlı değer çerçeveye yapışmaz: tetiğin dikey iç boşluğu bileşen kuralında.
        Assert.Matches(@"padding-block:\s*3px", Toplu1Ortak.Blok(css, ".secici-kap > .secici-tetik:has(> .secici-metin--tam):not(#_):not(#__)"));
        Assert.DoesNotContain("!important", b);
        // Sayfaya özgü uzun-ad kuralı ortak.css'te yok (tek yer: bileşen kuralı).
        var temiz = Regex.Replace(css, @"/\*.*?\*/", "", RegexOptions.Singleline);
        Assert.DoesNotContain("#islem-cari.secici-tetik", temiz);
        // Secici.razor tam adı title'da taşır (kesilen değer kaybolmaz).
        var razor = File.ReadAllText(Path.Combine(Toplu1Ortak.WebKok(), "Bilesenler", "Secici.razor"));
        Assert.Contains("title=\"@(string.IsNullOrEmpty(Deger) ? null : GorunenMetin)\">@GorunenMetin</span>", razor);
    }

    /// <summary>Tur 3 (orkestratör 12:45 "rozet kutunun içine binmez"): PRO satışta bakiye rozeti müşteri kutusunun
    /// İÇİNE bindirilmez (grid-row 1 + sağa yaslı + kutuda %46 boşluk kaldırıldı); kutunun altında kendi satırında.</summary>
    [Fact]
    public void T3_PRO_bakiye_rozeti_kutuya_bindirilmez()
    {
        var css = Toplu1Ortak.Css("tuval-satis.css");
        var temiz = Regex.Replace(css, @"/\*.*?\*/", "", RegexOptions.Singleline);
        Assert.DoesNotContain("calc(46% + 36px)", temiz);
        var bloklar = Regex.Matches(temiz, @"\.tuval-pro #islem-ust-satir > \.alan:first-child > \.rozet-satir \{([^}]*)\}")
            .Select(m => m.Groups[1].Value).ToList();
        Assert.NotEmpty(bloklar);
        Assert.All(bloklar, x =>
        {
            Assert.DoesNotMatch(@"grid-row:\s*1\b", x);
            Assert.DoesNotContain("justify-self: end", x);
            Assert.DoesNotContain("z-index", x);
        });
        Assert.Contains(bloklar, x => x.Contains("grid-row: 2"));
    }

    /// <summary>Tur 2 (doğrulayıcı DÜŞÜK): Vezne toplam satırı düğmeye yapışmaz (üst boşluk belirteci) ve ₺ son eki
    /// gövde boyunda (yalnız vezne formu).</summary>
    [Fact]
    public void T2_Vezne_toplam_ust_bosluk_ve_para_son_eki_govde_boyu()
    {
        var css = Toplu1Ortak.Css("ortak.css");
        Assert.Contains("margin: var(--bosluk-3) 0 0", Toplu1Ortak.Blok(css, ".form-toplam-satir"));
        Assert.Contains("var(--yazi-govde", Toplu1Ortak.Blok(css, "#vz-form .alan-son-ek"));
    }

    /// <summary>Tur 2 (tasarım hakemi KRİTİK): aynı sütunda "30.09.2026" altında "1.10.2026" çıkıyordu
    /// (Bicim.Tarih kültürün kısa kalıbı "d"). Tek kalıp dd.MM.yyyy — dilden ve kısa kalıptan bağımsız.</summary>
    [Fact]
    public void TARIH_TEK_tek_kalip_sifirli_gun_dilden_bagimsiz()
    {
        var an = new DateTime(2026, 10, 1, 7, 3, 0, DateTimeKind.Utc);   // 10:03 TSİ
        Assert.Equal("01.10.2026", Bicim.Tarih(an, "tr-TR"));
        Assert.Equal("01.10.2026", Bicim.Tarih(an, "en-US"));
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        Assert.Equal("01.10.2026", Bicim.IsletmeBicim(an, "d", tr));
        Assert.Equal("01.10.2026 10:03", Bicim.IsletmeBicim(an, "g", tr));
        Assert.Equal("01.10.2026 10:03:00", Bicim.IsletmeBicim(an, "G", tr));
        Assert.Equal("01.10.2026", Bicim.GunBicim(new DateTime(2026, 10, 1), "d", tr));
        Assert.Equal("01.10.2026", Bicim.GunBicim(new DateOnly(2026, 10, 1), "d", CultureInfo.GetCultureInfo("en-GB")));
        // Uzun biçim (ay adı) tek kalıba ÇEVRİLMEZ — o başka bir gösterim.
        Assert.Equal("1 Ekim", Bicim.IsletmeBicim(an, "d MMMM", tr));
    }

    /// <summary>denetim.sh TARIH_TEK sayacının aynısı: Razor/sayfa/bileşen/yerleşim kodunda ham tarih biçimi 0.</summary>
    [Fact]
    public void TARIH_TEK_razor_ve_sayfa_kodunda_ham_tarih_bicimi_yok()
    {
        var des = new[]
        {
            new Regex(@"\.ToLocalTime\s*\("),
            new Regex("\\.ToString\\(\\s*\"[^\"]*(dd|yyyy|yy|HH|hh|MMMM|dddd)[^\"]*\""),
            new Regex("\\.ToString\\(\\s*\"[gdtfGDTF]\""),
            new Regex("\\{[^{}\"]*:(dd|yyyy|HH|hh)[^{}]*\\}"),
            // Tur 2 (tasarım hakemi KRİTİK): sıfırsız gün kalıbı ve kültürün kısa kalıbı.
            new Regex("(IsletmeBicim|GunBicim)\\([^;]*\"d(\\.|'\\.')M"),
            new Regex("\\{[^{}\"]*:[dgG]\\}"),
        };
        var kok = Toplu1Ortak.WebKok();
        var ihlal = new List<string>();
        foreach (var alt in new[] { "Pages", "Bilesenler", "Layout" })
            foreach (var f in Directory.EnumerateFiles(Path.Combine(kok, alt), "*.*", SearchOption.AllDirectories)
                         .Where(f => (f.EndsWith(".razor") || f.EndsWith(".cs")) && !f.Contains("/obj/") && !f.Contains("/bin/")))
            {
                var no = 0;
                foreach (var s in File.ReadLines(f))
                {
                    no++;
                    var t = s.TrimStart();
                    if (t.StartsWith("//") || t.StartsWith("@*") || t.StartsWith("*")) continue;
                    if (des.Any(d => d.IsMatch(s))) ihlal.Add($"{Path.GetRelativePath(kok, f)}:{no}");
                }
            }
        Assert.True(ihlal.Count == 0, "TARIH-TEK ihlali: " + string.Join(", ", ihlal.Take(15)));
    }

    // ── Hurda: kasa bakiye reddi biçimli cümle (TRY/makine sayısı yok) ──
    [Fact]
    public void Kasa_bakiye_reddi_tr_TR_bicimli_TRY_yok()
    {
        var dil = new DilServisi(null!);
        var m = KasaBakiyeMesaji.Bicimle(
            "'Ana Kasa' kasasının bakiyesi yetersiz: 188407.25 TRY var, 219840.00 TRY isteniyor. Nakit kasa eksiye düşemez.", dil);
        Assert.NotNull(m);
        Assert.Contains(Bicim.Para(188407.25m, "TRY"), m);
        Assert.Contains(Bicim.Para(219840m, "TRY"), m);
        Assert.DoesNotContain("TRY", m);
        Assert.DoesNotContain("188407.25", m);
        Assert.Null(KasaBakiyeMesaji.Bicimle("MASAK: kimlik tespiti zorunludur", dil));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// Faturalar (bUnit)
// ═════════════════════════════════════════════════════════════════════════════
public sealed class Toplu1FaturaTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string liste = "";
    private string? kalemler;
    private string faturalanabilir = Faturalanabilir;
    private string? fistenRet;

    private const string Faturalanabilir = """
        {"gun":30,"limit":50,"dahaVar":false,"islemler":[
          {"id":"h-fissiz","fisId":null,"tur":"SATIS","tarih":"2026-09-30T22:30:00","tlTutari":2500.00,"hasGrami":1.5,"cariId":"c1","urunAd":"Çeyrek altın"},
          {"id":"h-f1a","fisId":"FIS9","tur":"SATIS","tarih":"2026-09-30T10:00:00","tlTutari":1000.00,"hasGrami":0.5,"cariId":"c1","urunAd":"Yüzük"},
          {"id":"h-f1b","fisId":"FIS9","tur":"SATIS","tarih":"2026-09-30T10:00:00","tlTutari":3000.00,"hasGrami":1.2,"cariId":"c1","urunAd":"Kolye"}]}
        """;

    private static string Liste(string kalemKaynagi, decimal toplam, string kdvTuru = "STANDART",
                                bool islemden = false, string tur = "SATIS", string tarih = "2026-09-23T08:00:00Z") => $$"""
        {"faturalar":[{"id":"f1","faturaNo":null,"cariAd":"Ayşe Demir","tur":"{{tur}}",
          "araToplam":1000.00,"kdvOran":0.2,"kdvTutar":200.00,"toplam":{{toplam.ToString(CultureInfo.InvariantCulture)}},
          "durum":"TASLAK","bankaHareketId":null,"tarih":"{{tarih}}",
          "kalemKaynagi":"{{kalemKaynagi}}","hasDegerKaynagi":"ELLE","kdvTuru":"{{kdvTuru}}",
          "cariId":"c1","aciklama":"not","islemdenGeldi":{{(islemden ? "true" : "false")}}}]}
        """;

    private const string KalemYanit = """
        {"faturaId":"f1","faturaNo":null,"kalemKaynagi":"KALEM","hasDegerKaynagi":"KALEM","toplam":20000,
         "kalemler":[{"id":"k1","sira":1,"aciklama":"Bilezik","miktar":1,"birim":"ADET","ayarId":"K22","milyem":916,
          "gramAgirlik":10,"hasGrami":9.16,"tasIbaresi":null,"birimFiyat":20000,"iscilikTl":null,"indirimTutari":null,
          "kdvTuru":"STANDART","kdvOran":0.2,"hasDeger":0,"kdvMatrah":16666.67,"kdvTutar":3333.33,"satirToplam":20000}]}
        """;

    public Toplu1FaturaTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Kuyumcu.Web.Bilesenler.KayitSecici.BeklemeMs = 0;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Sahte(Toplu1FaturaTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add((istek.Method.Method, yol, govde));
            var get = istek.Method == HttpMethod.Get;
            if (yol.EndsWith("/faturalar/fisten") && t.fistenRet is not null)
                return Toplu1Ortak.Yanit(JsonSerializer.Serialize(new { statusCode = 400, message = t.fistenRet, error = "Bad Request" }),
                    HttpStatusCode.BadRequest);
            return Toplu1Ortak.Yanit(yol switch
            {
                var y when y.Contains("/ozel-matrah-ayari") => """{"kod":null,"acik":false,"aralikta":false,"kuyumFaturaVar":false}""",
                var y when y.Contains("/faturalanabilir-islemler") => t.faturalanabilir,
                var y when get && y.Contains("/kalemler") => t.kalemler ?? "{}",
                var y when y.Contains("/islemler/fis/") => """
                    {"fisId":"FIS9","cariId":"c1","cariAd":"Ayşe Demir","faturaNo":null,"kalemSayisi":2,"toplamTl":4000,"toplamHas":-1.7,
                     "kalemler":[{"id":"h-f1a","tur":"SATIS","tlTutari":1000,"hasGrami":-0.5,"hediye":false,"urunAd":"Yüzük","faturaNo":null},
                                 {"id":"h-f1b","tur":"SATIS","tlTutari":3000,"hasGrami":-1.2,"hediye":false,"urunAd":"Kolye","faturaNo":null}]}
                    """,
                var y when y.EndsWith("/faturalar/fisten") => """{"id":"fyeni","toplam":2500.00,"durum":"TASLAK"}""",
                var y when get && (y == "/api/v1/faturalar" || y.StartsWith("/api/v1/faturalar?", StringComparison.Ordinal)) => t.liste,
                var y when y.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"},{"id":"c2","ad":"Mehmet Kaya"}]""",
                var y when y.Contains("/urunler") => "[]",
                var y when y.Contains("/arama") => """{"sorgu":"","tur":"FIS","limit":10,"ofset":0,"sayi":0,"dahaVar":false,"tavan":50,"sonuclar":[]}""",
                _ => "{}",
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> Ciz(string kalemKaynagi = "BASLIK", decimal toplam = 1200m,
        string? hazirListe = null, string? mod = null)
    {
        liste = hazirListe ?? Liste(kalemKaynagi, toplam);
        if (mod is not null)
        {
            var m = Services.GetRequiredService<ArayuzModuServisi>();
            Services.GetRequiredService<DilServisi>().BasitModuBagla(() => m.BasitMi);
        }
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod ?? "PROFESYONEL"));
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-duzenle-f1");
        return e;
    }

    /// <summary>(3) md.1: kalemli taslakta toplam KİLİTLİ → girdi değil salt okunur satır; Kaydet toplam göndermez.</summary>
    [Fact]
    public void Kilitli_toplam_salt_okunur_satir_duzenlenebilir_gorunmez()
    {
        kalemler = KalemYanit;
        var e = Ciz("KALEM", 20000m);
        e.Find("#fk-duzenle-f1").Click();
        e.WaitForElement("#fd-duzenle-cari");
        var toplam = e.Find("#fd-duzenle-toplam");
        Assert.NotEqual("INPUT", toplam.TagName);
        Assert.Contains("20.000", toplam.TextContent);
        Assert.Empty(e.FindAll("input#fd-duzenle-toplam"));
        e.Find("#cekmece-fatura-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH"));
        var govde = JsonDocument.Parse(istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
        Assert.False(govde.TryGetProperty("toplam", out _));
    }

    /// <summary>Düzenlenebilir (kalemsiz) taslakta toplam GERÇEK, ZORUNLU girdi ve PATCH'e gider.</summary>
    [Fact]
    public void Duzenlenebilir_toplam_zorunlu_girdi_ve_gercekten_gonderilir()
    {
        var e = Ciz("BASLIK", 1200m);
        e.Find("#fk-duzenle-f1").Click();
        e.WaitForElement("#fd-duzenle-cari");
        var g = e.Find("input#fd-duzenle-toplam");
        Assert.Equal("true", g.GetAttribute("aria-required"));
        g.Input("1500");
        e.Find("#cekmece-fatura-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH"));
        var govde = JsonDocument.Parse(istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
        Assert.Equal(1500m, govde.GetProperty("toplam").GetDecimal());
    }

    /// <summary>Düzenlenebilir taslağın Kalemler penceresinde amber "değiştirilemez" bandı YOK (nötr not).</summary>
    [Fact]
    public void Taslak_kalemler_penceresinde_degistirilemez_bandi_yok()
    {
        kalemler = KalemYanit;
        var e = Ciz("KALEM", 20000m);
        e.Find("#fk-goster-0").Click();
        e.WaitForElement("#degismez-ipucu-FaturaKalemSilme");
        var not = e.Find("#degismez-ipucu-FaturaKalemSilme");
        Assert.DoesNotContain("degismez-ipucu", not.ClassName ?? "");
        Assert.DoesNotContain(Sozlukler.Tr.Duzeltme.DuzenlenemezBaslik, not.TextContent);
        Assert.Contains(FaturaTaslakSozlugu.Tr.KalemSilmeGerekce, not.TextContent);
    }

    /// <summary>(4) Fişten Fatura Kes fişsiz satışı BULUR ve hareketId ile keser; fişli kayıt FİŞ olarak.</summary>
    [Fact]
    public void Fisten_fatura_kes_fissiz_satisi_bulur_ve_hareketId_gonderir()
    {
        var e = Ciz();
        e.Find("#ff-ac").Click();
        e.WaitForElement("#ff-islem");
        var secenekler = SeciciYardim.Secenekler(e, "ff-islem");
        Assert.Equal(2, secenekler.Count);   // fişsiz 1 + FIS9 (2 kalem tek seçenek)
        Assert.Contains(secenekler, o => o.Deger() == "har:h-fissiz");
        Assert.Contains(secenekler, o => o.Deger() == "fis:FIS9");
        // İşletme saati: 30.09 22:30 UTC → 01.10.2026 01:30
        Assert.Contains("01.10.2026 01:30", secenekler.First(o => o.Deger() == "har:h-fissiz").TextContent);

        SeciciYardim.Sec(e, "ff-islem", "har:h-fissiz");
        e.WaitForElement("#ff-toplam");
        Assert.Contains("2.500", e.Find("#ff-toplam").TextContent);
        // Tur 2: fişsiz seçimde eski fiş araması ve Önizle düğmesi gizli (önizleme hazır; sebepsiz kilit yok).
        Assert.Empty(e.FindAll("#ff-fis"));
        Assert.Empty(e.FindAll("#ff-onizle"));
        Assert.False(e.Find("#ff-kaydet").HasAttribute("disabled"));
        e.Find("#ff-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/faturalar/fisten")));
        var govde = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST").Govde).RootElement;
        Assert.Equal("h-fissiz", govde.GetProperty("hareketId").GetString());
        Assert.Equal(JsonValueKind.Null, govde.GetProperty("fisId").ValueKind);
        // UX-S27: sonuç sayfa bandında, sunucunun toplamıyla.
        e.WaitForAssertion(() => Assert.Equal(string.Format(Toplu1Sozlugu.Tr.FistenOlustu, "Ayşe Demir", Bicim.Tl(2500m)),
            e.Find("#basari-bandi").TextContent.Trim()));
    }

    [Fact]
    public void Fisten_fatura_kes_fisli_kayit_fisId_ile_gider()
    {
        var e = Ciz();
        e.Find("#ff-ac").Click();
        e.WaitForElement("#ff-islem");
        SeciciYardim.Sec(e, "ff-islem", "fis:FIS9");
        e.WaitForElement("#ff-toplam");
        Assert.NotEmpty(e.FindAll("#ff-fis"));
        Assert.NotEmpty(e.FindAll("#ff-onizle"));
        e.Find("#ff-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/faturalar/fisten")));
        var govde = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST").Govde).RootElement;
        Assert.Equal("FIS9", govde.GetProperty("fisId").GetString());
        Assert.Equal(JsonValueKind.Null, govde.GetProperty("hareketId").ValueKind);
    }

    // ═══ Tur 2 (doğrulayıcı + tasarım hakemi) ═══════════════════════════════════════════

    private static string KalemYanitBagli(bool fissiz) => $$"""
        {"faturaId":"f1","faturaNo":null,"kalemKaynagi":"KALEM","hasDegerKaynagi":"KALEM","toplam":20000,
         "kalemler":[{"id":"k1","sira":1,"aciklama":"Bilezik","miktar":1,"birim":"ADET","ayarId":"K22","milyem":916,
          "gramAgirlik":10,"hasGrami":9.16,"tasIbaresi":null,"birimFiyat":20000,"iscilikTl":null,"indirimTutari":null,
          "kdvTuru":"KUYUM","kdvOran":0.2,"hasDeger":0,"kdvMatrah":16666.67,"kdvTutar":3333.33,"satirToplam":20000,
          "hareketId":"h1","hareketFissiz":{{(fissiz ? "true" : "false")}}}]}
        """;

    /// <summary>[ORTA] KUYUM taslağın düzenleme çekmecesinde kayıt düzeyindeki amber "Bu kayıt değiştirilemez"
    /// bandı YOK; alan düzeyinde nötr not "Toplam satırlardan hesaplanır".</summary>
    [Fact]
    public void T2_Kuyum_taslak_cekmecesinde_degistirilemez_bandi_yok_notr_not()
    {
        kalemler = KalemYanitBagli(fissiz: false);
        var e = Ciz(hazirListe: Liste("KALEM", 20000m, kdvTuru: "KUYUM", islemden: true));
        e.Find("#fk-duzenle-f1").Click();
        e.WaitForElement("#fd-cekmece-kalem-k1");
        Assert.Empty(e.FindAll("#degismez-ipucu-FaturaTaslakToplam"));
        Assert.DoesNotContain(Sozlukler.Tr.Duzeltme.DuzenlenemezBaslik, e.Find("#cekmece-fatura").TextContent);
        Assert.Equal(Toplu1Sozlugu.Tr.ToplamSatirlardan, e.Find("#fd-duzenle-toplam-sebep").TextContent.Trim());
        Assert.Empty(e.FindAll("input#fd-duzenle-toplam"));
    }

    /// <summary>Satırsız KUYUM taslakta da bant yok; gerekçe + yol nötr metin olarak.</summary>
    [Fact]
    public void T2_Satirsiz_kuyum_taslakta_gerekce_notr_metin()
    {
        var e = Ciz(hazirListe: Liste("BASLIK", 1200m, kdvTuru: "KUYUM"));
        e.Find("#fk-duzenle-f1").Click();
        e.WaitForElement("#fd-duzenle-cari");
        Assert.Empty(e.FindAll("#degismez-ipucu-FaturaTaslakToplam"));
        var not = e.Find("#fd-duzenle-toplam-sebep").TextContent;
        Assert.Contains(FaturaTaslakSozlugu.Tr.KuyumToplamGerekce, not);
        Assert.Contains(FaturaTaslakSozlugu.Tr.KuyumToplamYol, not);
    }

    /// <summary>Tur 3 (orkestratör 12:45): SATIRSIZ taslak (standart ve kuyum) düzenlenebilir bir kayıttır →
    /// çekmecede amber "Bu kayıt değiştirilemez" bandı (DegismezIpucu) YOK; aynı id ile nötr not (gerekçe + yol)
    /// var; Toplam gerçekten düzenlenebilir (standartta) — bant yalnız KESİLDİ/İPTAL belgede çıkar.</summary>
    [Theory]
    [InlineData("STANDART")]
    [InlineData("KUYUM")]
    public void T3_Satirsiz_taslakta_degistirilemez_bandi_yok_notr_not(string kdvTuru)
    {
        var e = Ciz(hazirListe: Liste("BASLIK", 1200m, kdvTuru: kdvTuru));
        e.Find("#fk-duzenle-f1").Click();
        e.WaitForElement("#fd-duzenle-cari");
        var cekmece = e.Find("#cekmece-fatura");
        Assert.DoesNotContain(Sozlukler.Tr.Duzeltme.DuzenlenemezBaslik, cekmece.TextContent);
        Assert.Empty(cekmece.QuerySelectorAll(".degismez-ipucu"));
        var not = e.Find("#degismez-ipucu-FaturaSatirsizTaslak");
        Assert.Equal("P", not.TagName);
        Assert.Contains(FaturaTaslakSozlugu.Tr.SatirsizGerekce, not.TextContent);
        Assert.Contains(FaturaTaslakSozlugu.Tr.SatirsizYol, not.TextContent);
        if (kdvTuru == "STANDART") Assert.NotEmpty(e.FindAll("input#fd-duzenle-toplam"));
    }

    /// <summary>[KRİTİK] çekmece satır ipucunda "sunucu" jargonu yok (TR ve EN).</summary>
    [Fact]
    public void T2_Taslak_satir_ipucunda_sunucu_jargonu_yok()
    {
        kalemler = KalemYanit;
        var e = Ciz("KALEM", 20000m);
        e.Find("#fk-duzenle-f1").Click();
        e.WaitForElement("#fd-kalem-duzelt-ipucu");
        var ip = e.Find("#fd-kalem-duzelt-ipucu").TextContent;
        Assert.Equal(Toplu1Sozlugu.Tr.SatirDuzeltIpucu, ip.Trim());
        Assert.DoesNotContain("sunucu", e.Find("#fd-kalem-bolumu").TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("server", Toplu1Sozlugu.En.SatirDuzeltIpucu, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>[KRİTİK] fişsiz satıştan gelen kalem: adet kilidi "satış kaydından", bağlantı işlem defterine;
    /// fiş metni ve /islemler bağlantısı YOK. Fişli kalemde eski metin + /islemler korunur.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void T2_Adet_kilidi_gercek_kaynagi_soyler(bool fissiz)
    {
        kalemler = KalemYanitBagli(fissiz);
        var e = Ciz(hazirListe: Liste("KALEM", 20000m, islemden: true));
        e.Find("#fk-duzenle-f1").Click();
        e.WaitForElement("#fd-cekmece-kalem-adet-sebep-k1");
        var sebep = e.Find("#fd-cekmece-kalem-adet-sebep-k1").TextContent;
        var bag = e.Find("#fd-cekmece-kalem-adet-fis-k1");
        if (fissiz)
        {
            Assert.Contains(Toplu1Sozlugu.Tr.AdetSatistanGelir, sebep);
            Assert.DoesNotContain("fiş", sebep, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("/raporlar/islem-defteri", bag.GetAttribute("href"));
            Assert.Equal(Toplu1Sozlugu.Tr.IslemKaydiniGor, bag.TextContent.Trim());
        }
        else
        {
            Assert.Contains(KesifP3Sozlugu.Tr.AdetFistenGelir, sebep);
            Assert.Equal("/islemler", bag.GetAttribute("href"));
        }
    }

    /// <summary>[KRİTİK] Fatura listesinde işlemden kesilen belge "Manuel" değil, "Satıştan geldi".</summary>
    [Fact]
    public void T2_Liste_islemden_gelen_belge_kaynak_rozeti()
    {
        var e = Ciz(hazirListe: Liste("KALEM", 20000m, islemden: true), mod: ArayuzModuServisi.Profesyonel);
        e.WaitForElement("#fk-kaynak-f1");
        var k = e.Find("#fk-kaynak-f1").TextContent;
        Assert.Contains(Toplu1Sozlugu.Tr.KaynakSatistan, k);
        Assert.DoesNotContain(Sozlukler.Tr.Fatura.Manuel, k);
    }

    /// <summary>[KRİTİK] fişsiz ALIŞ işlemi "Tek satış" değil "Tek alış" rozetiyle; satış "Tek satış".</summary>
    [Fact]
    public void T2_Fisten_secici_alis_islemi_tek_alis_rozeti()
    {
        faturalanabilir = """
            {"gun":30,"limit":50,"dahaVar":false,"islemler":[
              {"id":"h-alis","fisId":null,"tur":"ALIS","tarih":"2026-10-01T07:03:00","tlTutari":-3840.00,"hasGrami":3.5,"cariId":"c2","urunAd":"925 Gümüş Bileklik"},
              {"id":"h-satis","fisId":null,"tur":"SATIS","tarih":"2026-10-01T06:00:00","tlTutari":2500.00,"hasGrami":-1.5,"cariId":"c1","urunAd":"Çeyrek altın"}]}
            """;
        var e = Ciz();
        e.Find("#ff-ac").Click();
        e.WaitForElement("#ff-islem");
        var s = SeciciYardim.Secenekler(e, "ff-islem");
        var alis = s.First(o => o.Deger() == "har:h-alis").TextContent;
        var satis = s.First(o => o.Deger() == "har:h-satis").TextContent;
        Assert.Contains(Toplu1Sozlugu.Tr.FistenTekAlisRozet, alis);
        Assert.DoesNotContain(Toplu1Sozlugu.Tr.FistenFissizRozet, alis);
        Assert.Contains(Toplu1Sozlugu.Tr.FistenFissizRozet, satis);
        Assert.Contains("01.10.2026 10:03", alis);   // tek kalıp, işletme saati
    }

    /// <summary>[DÜŞÜK] yarışta ikinci POST reddi ham basılmaz ("zaten  ile" çift boşluk) — taslak cümlesi.</summary>
    [Theory]
    [InlineData("Bu hareket zaten  ile faturalandırılmış", true)]
    [InlineData("Bu hareket zaten taslak bir faturayla faturalandırılmış (numara kesimde verilir)", true)]
    [InlineData("Bu hareket zaten SAT-2026-000012 ile faturalandırılmış", false)]
    public void T2_Fisten_yaris_reddi_sade_cumle(string sunucu, bool taslak)
    {
        fistenRet = sunucu;
        var e = Ciz();
        e.Find("#ff-ac").Click();
        e.WaitForElement("#ff-islem");
        SeciciYardim.Sec(e, "ff-islem", "har:h-fissiz");
        e.WaitForElement("#ff-toplam");
        e.Find("#ff-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/faturalar/fisten")));
        var beklenen = taslak ? Toplu1Sozlugu.Tr.FistenIslemTaslakBagli
            : string.Format(Toplu1Sozlugu.Tr.FistenIslemFaturali, "SAT-2026-000012");
        e.WaitForAssertion(() => Assert.Contains(beklenen, e.Find("#cekmece-fatura-fisten").TextContent));
        Assert.DoesNotContain("zaten  ile", e.Find("#cekmece-fatura-fisten").TextContent);
    }

    /// <summary>[KRİTİK] TARIH-TEK: listede ve çekmecede aynı kalıp (01.10.2026, "1.10.2026" değil).</summary>
    [Fact]
    public void T2_Liste_tarihi_tek_kalip()
    {
        var e = Ciz(hazirListe: Liste("BASLIK", 1200m, tarih: "2026-10-01T07:03:00Z"));
        Assert.Contains("01.10.2026", e.Find("#fk-duzenle-f1").Closest("tr")!.TextContent);
        Assert.DoesNotContain(" 1.10.2026", " " + e.Find("#fk-duzenle-f1").Closest("tr")!.TextContent.Replace("01.10.2026", ""));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// Cariler detay (bUnit)
// ═════════════════════════════════════════════════════════════════════════════
public sealed class Toplu1CariTestleri : BunitContext
{
    private string ekstre = "";

    private static string Ekstre(string has, string tl) => $$"""
        {"bakiye":{"tlBakiye":800,"hasGramBakiye":2,"emanetteHas":0,"kayitliHasGramBakiye":2,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0},
         "hareketler":[
          {"id":"h2","tur":"VERESIYE","tlTutari":-2500,"hasGrami":0,"aciklama":"STORNO: Yanlış müşteriye yazıldı","olusturuldu":"2026-09-30T09:30:00Z","cariHasEtkisi":0,"bakiyeHasEtkisi":0,"emanetHas":0,"hasBakiyesineGirer":false,"emanet":false,"maden":null,"cariTlEtkisi":-2500,"stornoKaynakId":"h1"},
          {"id":"h1","tur":"VERESIYE","tlTutari":2500,"hasGrami":0,"aciklama":"bilezik","olusturuldu":"2026-09-30T09:00:00Z","cariHasEtkisi":0,"bakiyeHasEtkisi":0,"emanetHas":0,"hasBakiyesineGirer":false,"emanet":false,"maden":null,"cariTlEtkisi":2500,"stornoKaynakId":null}],
         "sayfaDisiHasEtkisi":0,"dahaFazlaVar":false,"limit":100,
         "bakiyeUyusmazligiHas":{{has}},"bakiyeEmanetsizHas":2,"emanetteHas":0,
         "bakiyeUyusmazligiTl":{{tl}},"tlAynaOlculemez":null}
        """;

    public Toplu1CariTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Sahte(Toplu1CariTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            return Task.FromResult(Toplu1Ortak.Yanit(yol switch
            {
                var y when y.EndsWith("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"gbpTry":null}""",
                var y when y.Contains("/ekstre") => t.ekstre,
                var y when y.EndsWith("/cari/c1") => """{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":null,"email":null,"vergiNo":null,"adres":null,"krediLimitiTl":null,"varsayilanVadeGun":30,"fiyatListesiId":null,"pepBeyani":false,"pepGorev":null,"pepBeyanTarihi":null,"uyruk":null}""",
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567","email":null,"tlBakiye":800,"hasGramBakiye":2,"emanetteHas":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"riskSeviye":null,"pepBeyani":false}]""",
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var y when y.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","aktif":true}]""",
                _ => "{}",
            }));
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Cariler> Detay(bool basit)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1",
                EtkinArayuzModu: basit ? "BASIT" : "PROFESYONEL"));
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("#c-detay-c1");
        e.Find("#c-detay-c1").Click();
        e.WaitForElement("#cari-detay-hareketler");
        return e;
    }

    /// <summary>Uyuşmazlık TEK bant, bakiye kutularının hemen altında (hareketlerden ÖNCE); PRO iki farkı tek cümlede.</summary>
    [Fact]
    public void Uyusmazlik_tek_bant_bakiyenin_yaninda_PRO_iki_fark()
    {
        ekstre = Ekstre("1.25", "-150.5");
        var e = Detay(basit: false);
        var bantlar = e.FindAll(".bant-uyari").Where(b => b.Id?.StartsWith("cari-detay-uyusmazlik") == true).ToList();
        Assert.Single(bantlar);
        var bant = e.Find("#cari-detay-uyusmazlik");
        Assert.Contains(Bicim.Tl(-150.5m), e.Find("#cari-detay-uyusmazlik-tl").TextContent);
        Assert.Contains(Kuyumcu.Web.Pages.Cariler.HasBicim(1.25m), e.Find("#cari-detay-uyusmazlik-has").TextContent);
        // Konum: bant, hareket listesinden ÖNCE ve bakiye kutularından SONRA gelir.
        var markup = e.Find("#cari-detay").InnerHtml;
        Assert.True(markup.IndexOf("cari-detay-bakiyeler", StringComparison.Ordinal) < markup.IndexOf("cari-detay-uyusmazlik", StringComparison.Ordinal));
        Assert.True(markup.IndexOf("cari-detay-uyusmazlik", StringComparison.Ordinal) < markup.IndexOf("cari-detay-hareketler", StringComparison.Ordinal));
        // Teknik cümle bir kez.
        Assert.Single(Regex.Matches(e.Find("#cari-detay").TextContent, "düzeltme fişi bu farkı kapatmaz"));
    }

    [Fact]
    public void Uyusmazlik_BASIT_teknik_degil_tek_sade_cumle()
    {
        ekstre = Ekstre("1.25", "-150.5");
        var e = Detay(basit: true);
        var bant = e.Find("#cari-detay-uyusmazlik");
        Assert.Equal(Toplu1Sozlugu.Tr.UyusmazlikBasit, bant.TextContent.Trim());
        Assert.DoesNotContain("düzeltme fişi", e.Find("#cari-detay").TextContent);
    }

    [Fact]
    public void Uyusmazlik_yoksa_bant_yok()
    {
        ekstre = Ekstre("0", "0");
        var e = Detay(basit: false);
        Assert.Empty(e.FindAll("#cari-detay-uyusmazlik"));
    }

    /// <summary>İptal satırı "Veresiye iptali" (BASİT) — "STORNO:" ham ön eki ekrana çıkmaz.</summary>
    [Fact]
    public void Iptal_satiri_BASIT_iptal_adiyla_STORNO_jargonu_yok()
    {
        ekstre = Ekstre("0", "0");
        var e = Detay(basit: true);
        var tur = e.Find("#cari-detay-tur-h2").TextContent.Trim();
        Assert.Equal(string.Format(Toplu1Sozlugu.Tr.IptalSatiriBasit, KatalogSozlugu.HareketTuru("VERESIYE", "tr", true)), tur);
        Assert.Equal(string.Format(Toplu1Sozlugu.Tr.IptalSebebi, "Yanlış müşteriye yazıldı"), e.Find("#cari-detay-aciklama-h2").TextContent.Trim());
        Assert.DoesNotContain("STORNO", e.Find("#cari-detay").TextContent, StringComparison.OrdinalIgnoreCase);
        // Kaynak satır kendi adını taşır.
        Assert.Equal(KatalogSozlugu.HareketTuru("VERESIYE", "tr", true), e.Find("#cari-detay-tur-h1").TextContent.Trim());
    }

    [Fact]
    public void Iptal_satiri_PRO_storno_etiketli()
    {
        ekstre = Ekstre("0", "0");
        var e = Detay(basit: false);
        Assert.Equal(string.Format(Toplu1Sozlugu.Tr.IptalSatiriPro, KatalogSozlugu.HareketTuru("VERESIYE", "tr", false)),
            e.Find("#cari-detay-tur-h2").TextContent.Trim());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// Vezne + Gider (bUnit)
// ═════════════════════════════════════════════════════════════════════════════
public sealed class Toplu1VezneGiderTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string kategoriler = """{"kategoriler":[{"id":"gk1","ad":"Kira","aciklama":null,"aktif":true,"giderAdedi":0,"giderToplamTl":0}],"toplam":1}""";

    public Toplu1VezneGiderTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Sahte(Toplu1VezneGiderTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var metot = istek.Method.Method;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add((metot, yol, govde));
            return Toplu1Ortak.Yanit(yol switch
            {
                var y when y.EndsWith("/vezne/kuyruk") => """
                    {"durum":"VEZNEDE","istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":0,"dahaFazlasiVar":false,
                     "vezneZorunlu":false,"gerekceAsgari":10,"subeId":"sb1","durumKirilimi":[],"fisler":[]}
                    """,
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":10000,"aktif":true}]""",
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz"}]""",
                var y when y.EndsWith("/urunler") => """[{"id":"u-1","ad":"Bilezik"}]""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800}""",
                var y when y.EndsWith("/gider-kategorileri") => t.kategoriler,
                var y when y.EndsWith("/giderler") && metot == "POST" =>
                    """{"id":"kh9","kasaId":"ks1","kasaAdi":"Merkez Kasa","kategoriId":"gk1","kategoriAdi":"Kira","tutar":1500,"paraBirimi":"TRY","aciklama":null,"tarih":"2026-09-23T06:00:00Z","kasaBakiyesi":8500,"stornoYolu":""}""",
                var y when y.EndsWith("/giderler") => """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}""",
                var y when y.EndsWith("/virman") => """{"satirlar":[],"toplam":0,"limit":50,"dahaFazlasiVar":false,"sinirNotu":null}""",
                var y when y.EndsWith("/bankalar") => """{"hesaplar":[]}""",
                _ => "{}",
            });
        }
    }

    private void Oturum(bool basit)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1",
                EtkinArayuzModu: basit ? "BASIT" : "PROFESYONEL"));
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
    }

    /// <summary>BASİT: tür notları (i) ipucunda, açıklama "+ Açıklama ekle" arkasında KAPALI; PRO: açık.</summary>
    [Fact]
    public void Vezne_BASIT_notlar_ve_aciklama_katli_PRO_acik()
    {
        Oturum(basit: true);
        var e = Render<Kuyumcu.Web.Pages.Vezne>().AyrintiAc();
        e.Find("#vz-yeni-ac").Click();
        e.WaitForElement("#vz-form");
        Assert.False(e.Find("#vz-f-tur-ipucu").HasAttribute("open"));
        Assert.NotNull(e.Find("#vz-f-tur-ipucu #vz-f-tur-kapsam"));
        Assert.False(e.Find("#vz-f-aciklama-detay").HasAttribute("open"));
        Assert.Equal(Toplu1Sozlugu.Tr.VezneAciklamaEkle, e.Find("#vz-f-aciklama-ac").TextContent.Trim());
    }

    [Fact]
    public void Vezne_PRO_notlar_ve_aciklama_acik()
    {
        Oturum(basit: false);
        var e = Render<Kuyumcu.Web.Pages.Vezne>().AyrintiAc();
        e.Find("#vz-yeni-ac").Click();
        e.WaitForElement("#vz-form");
        Assert.Empty(e.FindAll("#vz-f-tur-ipucu"));
        Assert.Empty(e.FindAll("#vz-f-aciklama-detay"));
        Assert.NotNull(e.Find("#vz-f-aciklama"));
    }

    /// <summary>Birim fiyat binlikli Para girdisi (odak dışı "125.000"), toplam formun sonucu olarak (12 px dipnot değil).</summary>
    [Fact]
    public void Vezne_birim_fiyat_binlikli_toplam_belirgin_ve_govde_dogru()
    {
        Oturum(basit: true);
        var e = Render<Kuyumcu.Web.Pages.Vezne>().AyrintiAc();
        e.Find("#vz-yeni-ac").Click();
        e.WaitForElement("#vz-form");
        SeciciYardim.Sec(e, "vz-f-urun-0", "u-1");
        var f = e.Find("#vz-f-fiyat-0");
        f.Focus();
        f.Input("125000");
        e.Find("#vz-f-fiyat-0").Blur();
        Assert.Equal("125.000", e.Find("#vz-f-fiyat-0").GetAttribute("value"));
        var t = e.Find("#vz-f-toplam");
        Assert.Contains("form-toplam-satir", t.ClassName);
        Assert.DoesNotContain("metin-kucuk", t.ClassName);
        Assert.Contains("125.000", t.TextContent);
        e.Find("#vz-f-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/vezne/fisler")));
        var govde = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST").Govde).RootElement;
        Assert.Equal(125000m, govde.GetProperty("kalemler")[0].GetProperty("birimFiyatTl").GetDecimal());
    }

    /// <summary>Gider: tek aktif kategori → seçici YOK, salt okunur satır; gövdeye kendiliğinden gider (UX-S22).</summary>
    [Fact]
    public void Gider_tek_kategori_secici_gizli_otomatik_gonderilir()
    {
        Oturum(basit: false);
        var e = Render<Kuyumcu.Web.Pages.Gider>().AyrintiAc("#yildiz-gider-ekle");
        e.WaitForElement("#gd-kategori-sabit");
        Assert.Empty(e.FindAll("#gd-kategori"));
        Assert.Equal("gk1", e.Find("#gd-kategori-sabit").GetAttribute("data-deger"));
        Assert.Contains("Kira", e.Find("#gd-kategori-sabit").TextContent);
        e.Find("#gd-tutar").Input("1500");
        Assert.Empty(e.FindAll("#gd-kaydet-kilit"));
        e.Find("#gd-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/giderler")));
        var govde = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST").Govde).RootElement;
        Assert.Equal("gk1", govde.GetProperty("giderKategoriId").GetString());
    }

    [Fact]
    public void Gider_iki_kategoride_secici_gorunur()
    {
        kategoriler = """{"kategoriler":[{"id":"gk1","ad":"Kira","aciklama":null,"aktif":true,"giderAdedi":0,"giderToplamTl":0},{"id":"gk2","ad":"Elektrik","aciklama":null,"aktif":true,"giderAdedi":0,"giderToplamTl":0}],"toplam":2}""";
        Oturum(basit: false);
        var e = Render<Kuyumcu.Web.Pages.Gider>().AyrintiAc("#yildiz-gider-ekle");
        e.WaitForElement("#gd-kategori");
        Assert.Empty(e.FindAll("#gd-kategori-sabit"));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HTTP: fişsiz satış faturalanabilir listede fisId NULL ve hareketId ile faturalanır
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class Toplu1HttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Fissiz_satis_faturalanabilir_listede_ve_hareketId_ile_faturalanir()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem())
            k = TestKurulum.KiraciKur(db, $"PAKETTOPLU1-TEST http {Guid.NewGuid():N}"[..40]);
        var c = f.Ortam.Istemci(k, "SAHIP");

        var s = await c.PostJson("/api/v1/islemler/satis", new
        {
            urunId = k.UrunId, adet = 1m, birimFiyatTl = 5_000m, odemeTuru = "NAKIT", kasaId = k.KasaId,
            cariId = k.CariId, kurSnapshotId = k.KurId,
        });
        Assert.True(s.IsSuccessStatusCode, await s.Metin());

        var liste = (await (await c.GetAsync("/api/v1/faturalar/faturalanabilir-islemler?gun=30&limit=50")).Json())
            .GetProperty("islemler").EnumerateArray().ToList();
        var satir = Assert.Single(liste, x => x.GetProperty("tur").GetString() == "SATIS");
        Assert.Equal(JsonValueKind.Null, satir.GetProperty("fisId").ValueKind);   // FİŞSİZ — fiş aramasında görünmez
        var hareketId = satir.GetProperty("id").GetString();

        var r = await c.PostJson("/api/v1/faturalar/fisten", new { hareketId, tur = "SATIS", kdvTuru = "STANDART", kdvOran = 0.20m });
        Assert.True(r.IsSuccessStatusCode, await r.Metin());

        var sonra = (await (await c.GetAsync("/api/v1/faturalar/faturalanabilir-islemler?gun=30&limit=50")).Json())
            .GetProperty("islemler").EnumerateArray().ToList();
        Assert.DoesNotContain(sonra, x => x.GetProperty("id").GetString() == hareketId);   // damgalandı

        // ── Tur 2 (doğrulayıcı + tasarım hakemi) ──
        var faturaId = (await r.Json()).GetProperty("id").GetString();
        // (a) İşlemden kesilen belge listede ve tekilde "islemdenGeldi" taşır ("Manuel" değil).
        var fl = (await (await c.GetAsync("/api/v1/faturalar?limit=50")).Json()).GetProperty("faturalar").EnumerateArray()
            .Single(x => x.GetProperty("id").GetString() == faturaId);
        Assert.True(fl.GetProperty("islemdenGeldi").GetBoolean());
        Assert.True((await (await c.GetAsync($"/api/v1/faturalar/{faturaId}")).Json()).GetProperty("islemdenGeldi").GetBoolean());
        // (b) Fişsiz hareketten gelen kalem "hareketFissiz" = true (adet kilidi metni fiş demesin).
        var kalem = (await (await c.GetAsync($"/api/v1/faturalar/{faturaId}/kalemler")).Json()).GetProperty("kalemler")[0];
        Assert.Equal(hareketId, kalem.GetProperty("hareketId").GetString());
        Assert.True(kalem.GetProperty("hareketFissiz").GetBoolean());
        // (c) Yarışta ikinci POST: TASLAK numarasızdır — "zaten  ile" (boş numara) değil, sade cümle.
        var ikinci = await c.PostJson("/api/v1/faturalar/fisten", new { hareketId, tur = "SATIS", kdvTuru = "STANDART", kdvOran = 0.20m });
        Assert.Equal(HttpStatusCode.BadRequest, ikinci.StatusCode);
        var mesaj = (await ikinci.Json()).GetProperty("message").GetString()!;
        Assert.Equal("Bu hareket zaten taslak bir faturayla faturalandırılmış (numara kesimde verilir)", mesaj);
        Assert.DoesNotContain("  ", mesaj);
        // (d) Elle (kalemsiz) kesilen belge işlemden gelmiş sayılmaz.
        var elle = await c.PostJson("/api/v1/faturalar", new { cariId = k.CariId, tur = "SATIS", toplam = 1200m, kdvOran = 0.20m });
        if (elle.IsSuccessStatusCode)
            Assert.False((await elle.Json()).GetProperty("islemdenGeldi").GetBoolean());
    }
}
