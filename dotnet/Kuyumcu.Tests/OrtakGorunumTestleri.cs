using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ORTAK-1 — ORTAK GÖRÜNÜM ARTIKLARI [saf + bUnit]
// Kaynak: tam tur v3 (ham/TAM-TUR-v3-2026-09-25.txt) + paketin kendi ÖNCE/SONRA
// turu (başsız Chrome). Görünürlük/sığma kanıtı TARAYICI ölçümüdür (rapor);
// bu testler o ölçümün dayandığı SÖZLEŞMEYİ kilitler:
//   (A) tablo başlığı tek satır (ortak.css) · (B) para kartı değeri kap
//   sorgusuyla küçülür, uzun değer sınıfı StatKart'tan · (C) 375'te filtre
//   yuvası tek sütun · (D) çekmece kaydırma konumunu korur (cekmece.js) ·
//   (E) Kim borçlu dar kartta iki kat · (F)/(H) tek satır/blok not sınıfları ·
//   (G) BASİT mod jargonu (uyum risk "cari", şubeler "Pasife Al").
// ⚠ Sahte API '{}' varsayılanına dokunulan uçlar BIRAKILMADI: her birine
//   Contracts kayıtlarıyla BİREBİR alan adlı gerçekçi yanıt verildi.
// ═══════════════════════════════════════════════════════════════════════════

public sealed class OrtakGorunumSaflik
{
    private static string OrtakCss() => CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "ortak.css");
    private static string CekmeceJs() => CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "js", "cekmece.js");

    [Fact]
    public void O1_index_ortak_cssi_EN_SONDA_yukler()
    {
        var html = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "index.html"));
        var ortak = html.IndexOf("href=\"css/ortak.css\"", StringComparison.Ordinal);
        Assert.True(ortak > 0, "index.html css/ortak.css'i yüklemiyor");
        var sonrakiler = Regex.Matches(html, "<link rel=\"stylesheet\" href=\"([^\"]+)\"")
            .Select(m => (m.Index, Href: m.Groups[1].Value)).ToList();
        Assert.Equal("css/ortak.css", sonrakiler[^1].Href);   // ezme sırası: en son o
        var katman = html.IndexOf("css/katman.css", StringComparison.Ordinal);
        Assert.True(katman > 0 && ortak > katman, $"ortak.css katman.css'ten sonra gelmeli ({katman} / {ortak})");
    }

    [Fact]
    public void O2_tablo_basligi_tek_satir_ve_ad_hucresi_kirilmaz()
    {
        var css = CssKaynak.YorumsuzCss(OrtakCss());
        Assert.Matches(@"\.tablo-sar > table > thead > tr > th,\s*\.tablo-sar > table > tbody > tr > th,\s*\.tablo-sar > table > tfoot > tr > th,[^{]*\{\s*white-space:\s*nowrap;", css);
        Assert.Matches(@"\.tablo-sar > \.veri-tablosu > tbody > tr > td:first-child > :is\(span, a\):first-child\s*\{\s*white-space:\s*nowrap;", css);
        Assert.Matches(@"\.tablo-sar \.hucre-not-tek-satir\s*\{\s*white-space:\s*nowrap;", css);
    }

    [Fact]
    public void O3_para_karti_kap_sorgusuyla_kuculur_ozgun_boyu_asmaz()
    {
        var css = CssKaynak.YorumsuzCss(OrtakCss());
        Assert.Matches(@"\.kart-govde:has\(> \.stat-deger\)[^{]*\{\s*container-type:\s*inline-size;", css);
        // Özgün boy (StatKart 1.5rem · kart 1em · panel 3xl · sade panel gövde×2) AŞILMAZ.
        Assert.Contains("font-size: min(1.5rem, calc(var(--deger-oran) * 1cqi));", css);
        Assert.Matches(@"\.kart-govde > \.stat-deger\s*\{\s*font-size:\s*min\(1em, calc\(var\(--deger-oran\) \* 1cqi\)\);\s*white-space:\s*nowrap;", css);
        Assert.Matches(@"\.panel-soru > \.panel-soru-sayi\s*\{\s*font-size:\s*min\(var\(--yazi-3xl\), calc\(var\(--deger-oran\) \* 1cqi\)\);", css);
        Assert.Contains("font-size: min(calc(var(--yazi-govde) * 2), calc(var(--deger-oran) * 1cqi));", css);
        // Kademe → oran: kısa değer büyük kalır, uzun değer küçülür (azalan sıra).
        var oran = new Dictionary<string, double>();
        foreach (Match m in Regex.Matches(css, @"\[data-deger-boy=""(?<k>[a-z-]+)""\]\s*\{\s*--deger-oran:\s*(?<o>[0-9.]+);"))
            oran[m.Groups["k"].Value] = double.Parse(m.Groups["o"].Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(new[] { "kisa", "orta", "uzun", "cok-uzun", "en-uzun" }, oran.Keys.ToArray());
        Assert.True(oran["kisa"] > oran["orta"] && oran["orta"] > oran["uzun"]
                    && oran["uzun"] > oran["cok-uzun"] && oran["cok-uzun"] > oran["en-uzun"], string.Join(",", oran));
        // Kademe sınırının ÜST ucundaki değer (ör. 15 karakter) ~0.6em/karakterle kaba sığar.
        Assert.True(15 * 0.6 * oran["uzun"] <= 100 && 20 * 0.6 * oran["cok-uzun"] <= 100
                    && 12 * 0.6 * oran["orta"] <= 100 && 9 * 0.6 * oran["kisa"] <= 100);
        Assert.Matches(@"\.stat-deger,\s*\.panel-soru-sayi\s*\{\s*--deger-oran:\s*11;", css);
    }

    [Fact]
    public void O4_dar_ekranda_filtre_yuvasi_tek_sutun()
    {
        var css = CssKaynak.YorumsuzCss(OrtakCss());
        var m = Regex.Match(css, @"@media \(max-width: 640px\)\s*\{(?<g>.*?\})\s*\}", RegexOptions.Singleline);
        Assert.True(m.Success, "640 px medya bloğu yok");
        Assert.Matches(@"\.sayfa-basi \.sayfa-basi-filtre\s*\{[^}]*flex-direction:\s*column;", m.Groups["g"].Value);
        Assert.Matches(@"flex:\s*0 0 auto;\s*width:\s*100%;", m.Groups["g"].Value);
        // Sarılan etiketsiz düğme kendi satırının kontrol çizgisine oturur (/urunler 960).
        Assert.Matches(@"\.sayfa-basi \.sayfa-basi-filtre > :is\(button, \.btn, \.dugme\)\s*\{\s*margin-top:\s*var\(--arac-etiket-bosluk\);", css);
    }

    [Fact]
    public void O4b_filtre_seridindeki_dugme_yazisi_ortalanir_dibe_yapismaz()
    {
        // 27.09 ölçümü: app.css ".sayfa-basi-filtre > *" (sütun + flex-end) düğmeye
        // de uyuyordu → "Süzgeç ▾" yazısı 44 px kutunun dibindeydi (/urunler 375).
        var css = CssKaynak.YorumsuzCss(OrtakCss());
        Assert.Matches(@"\.sayfa-basi \.sayfa-basi-filtre > :is\(button, \.btn, \.dugme\)\s*\{\s*flex-direction:\s*row;\s*justify-content:\s*center;\s*align-items:\s*center;\s*\}", css);
    }

    [Fact]
    public void O5_cekmece_kaydirma_konumunu_korur()
    {
        var js = CssKaynak.YorumsuzKod(CekmeceJs());
        // Açılışta belge boyu sabitlenir, konum saklanır; odak sayfayı kaydırmaz.
        Assert.Contains("document.body.style.setProperty('--pencere-sabit-boy', kok.scrollHeight + 'px');", js);
        Assert.Contains("document.body.classList.add('pencere-sabit');", js);
        Assert.Contains("hedef.focus({ preventScroll: true });", js);
        Assert.Contains("onceki.focus({ preventScroll: true });", js);
        Assert.Contains("window.scrollTo(k.x, k.y);", js);
        // Konum yalnız İLK pencerede geri verilir (açık çekmecede ikinci açılış sarmaz).
        Assert.Contains("if (ilkPencere) konumuKoru();", js);
        // Sabitleme yalnız İLK pencerede kurulur (sayaçlı kilit bozulmaz).
        Assert.Contains("if (ilk && !document.__kuyumcuOnSabit) sabitle();", js);
        // Çizimden ÖNCE sabitleme ucu dışa açıktır (Cekmece.OnParametersSet çağırır).
        Assert.Contains("window.kuyumcuCekmece = { ac: ac, kapat: kapat, sabitle: onSabitle, birak: birak };", js);
        // Bayat sayaç (sayfa değişti, kapat çağrılmadı) açık pencere yoksa sıfırlanır.
        Assert.Matches(@"function onSabitle\(\) \{\s*sayaciDogrula\(\);", js);
        Assert.Contains("!document.querySelector('[aria-modal=\"true\"]')", js);
        // Sabit KALICI olamaz: bayat sayaç temizlenir, pencere açılmazsa 1 sn sonra bırakılır.
        Assert.Matches(@"function sabitiBirak\(\) \{\s*sayaciDogrula\(\);", js);
        Assert.Contains("if (document.__kuyumcuOnSabit && !document.querySelector('[aria-modal=\"true\"]')) {", js);
        var css = CssKaynak.YorumsuzCss(OrtakCss());
        Assert.Matches(@"body\.pencere-sabit\s*\{\s*min-height:\s*var\(--pencere-sabit-boy, auto\);", css);
    }

    [Fact]
    public void O6_kim_borclu_dar_kartta_iki_kat_ve_ad_kesilmez()
    {
        var css = CssKaynak.YorumsuzCss(OrtakCss());
        Assert.Contains("@container (max-width: 559px)", css);
        Assert.Contains("grid-template-areas: \"ad eylem\" \"durum tutar\";", css);
        Assert.Matches(@"#panel-soru-borclu \.panel-liste-ad\s*\{\s*white-space:\s*normal;\s*overflow:\s*visible;\s*text-overflow:\s*clip;", css);
    }

    [Fact]
    public void O7_ortak_css_sabit_renk_ve_gizleyen_tasma_tasimaz()
    {
        var css = CssKaynak.YorumsuzCss(OrtakCss());
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b", css);
        Assert.DoesNotMatch(@"rgba?\(|hsla?\(", css);
        Assert.DoesNotContain("text-overflow: ellipsis", css);
        // TOPLU-1 tur 3 (orkestratör 12:45, BAĞLAYICI): TEK ve DAR istisna — Secici'nin seçili değeri en çok İKİ
        // satır, fazlası "…" (-webkit-line-clamp: 2, overflow: hidden şart); tam ad Secici.razor title'ında kalır
        // (Toplu1SafTestleri.T3_Secici_en_cok_iki_satir_uc_nokta_bilesen_kurali + KesifP7 title testi). İstisna
        // yalnız line-clamp: 2 taşıyan o bloğa uygulanır; ortak.css'in geri kalanında gizleyen taşma hâlâ yasak.
        var istisnasiz = Regex.Replace(css,
            @"\.secici-kap > \.secici-tetik > \.secici-metin--tam:not\(#_\):not\(#__\)\s*\{[^}]*-webkit-line-clamp:\s*2;[^}]*\}", "");
        Assert.NotEqual(css, istisnasiz);
        Assert.Equal(1, Regex.Matches(css, "overflow: hidden").Count - Regex.Matches(istisnasiz, "overflow: hidden").Count);
        Assert.DoesNotContain("overflow: hidden", istisnasiz);
    }

    [Fact]
    public void O8_basit_metinler_jargon_tasimaz_iki_dil_dolu()
    {
        // Turun K8 düzenli ifadesiyle AYNI: bağımsız "cari/cariler/pasife al".
        var jargon = new Regex(@"(?<![\p{L}])(cari|cariler|cariden|cariye|carinin|storno|vezne|mutabakat|pasife al|denetim izi)(?![\p{L}])",
            RegexOptions.IgnoreCase);
        var hakemJargonu = new Regex(@"(?<![\p{L}])(PEP|ufuk|ufk\p{L}*|işlem uç\p{L}*|pasif\p{L}*|bacağı|bacak|ayrımı)(?![\p{L}])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        foreach (var m in new[] { OrtakGorunumSozlugu.Tr, OrtakGorunumSozlugu.En })
        {
            var degerler = typeof(OrtakGorunumMetinleri).GetProperties()
                .Where(p => p.PropertyType == typeof(string))
                .Select(p => (p.Name, Deger: (string)p.GetValue(m)!)).ToList();
            // 15 + VezneKilitNeden (28.09) + PanelBaglantiHatasi (kıyas devri 29.09)
            // + 12 hakem turu 29.09 (risk ×3, son şube, reyon notu, kâr-zarar ×3, sipariş, vade ×3).
            Assert.Equal(29, degerler.Count);
            Assert.All(degerler, d => Assert.False(string.IsNullOrWhiteSpace(d.Deger), d.Name));
            // PRO metinleri (UyumRiskKapaliPro, UrunAgaciReyonSayimNotu) muhasebe dilini taşıyabilir;
            // K8 jargonu yalnız iki modda da görünen ve BASİT metinlerde yasak.
            Assert.All(degerler.Where(d => !d.Name.EndsWith("Pro") && d.Name != "UrunAgaciReyonSayimNotu"),
                d => Assert.False(jargon.IsMatch(d.Deger), $"{d.Name}: {d.Deger}"));
            // Hakem turu 29.09: tur ölçerinin tanımadığı artıklar da BASİT metinde yok —
            // kısaltma (PEP), "ufuk", "işlem ucu", "pasif", "bacak", "kuyruk ayrımı".
            Assert.All(degerler.Where(d => d.Name.EndsWith("Basit")),
                d => Assert.False(hakemJargonu.IsMatch(d.Deger), $"{d.Name}: {d.Deger}"));
            // Ham yapılandırma anahtarı HİÇBİR metinde yok (MasakRisk:Etkin=false gibi).
            Assert.All(degerler, d => Assert.DoesNotMatch(@"[A-Za-z]+:[A-Za-z]+=", d.Deger));
        }
        Assert.Same(OrtakGorunumSozlugu.En, OrtakGorunumSozlugu.Al("en"));
        Assert.Same(OrtakGorunumSozlugu.Tr, OrtakGorunumSozlugu.Al("tr"));
        Assert.Contains("{0}", OrtakGorunumSozlugu.Tr.UyumRiskListeSiniriBasit);
        Assert.Contains("{0}", OrtakGorunumSozlugu.En.UyumRiskListeSiniriBasit);
    }
}

public sealed class OrtakGorunumStatKartTestleri : BunitContext
{
    private string Kademe(string deger) =>
        Render<StatKart>(p => p.Add(x => x.Etiket, "Toplam").Add(x => x.Deger, deger))
            .Find("p.stat-deger").GetAttribute("data-deger-boy") ?? "";

    [Fact]
    public void S1_deger_aynen_basilir_kademe_ozniteligi_tasir()
    {
        var deger = Bicim.Tl(-728188.36m);
        var e = Render<StatKart>(p => p.Add(x => x.Etiket, "Kasa").Add(x => x.Deger, deger));
        var p = e.Find("p.stat-deger");
        Assert.Equal(deger, p.TextContent);                    // değer/biçim DEĞİŞMEZ
        Assert.Equal("stat-deger", p.GetAttribute("class"));   // sınıf aynı
        Assert.Equal(DegerBoyu.Kademe(deger), p.GetAttribute("data-deger-boy"));
    }

    [Fact]
    public void S2_kademeler_gercek_tutarlarla()
    {
        Assert.Equal("kisa", Kademe("3"));
        Assert.Equal("orta", Kademe(Bicim.Tl(203149.08m)));               // "203.149,08 ₺"
        Assert.Equal("uzun", Kademe(Bicim.Tl(-7681959.97m)));             // 7 haneli eksi
        Assert.Equal("cok-uzun", Kademe("Giriş: " + Bicim.Tl(959572.58m)));
        Assert.Equal("en-uzun", Kademe("Giriş: " + Bicim.Tl(8959572.58m)));
    }

    [Fact]
    public void S3_esikler_sinirda_dogru()
    {
        Assert.Equal("kisa", Kademe(""));
        Assert.Equal("kisa", Kademe(new string('9', 9)));
        Assert.Equal("orta", Kademe(new string('9', 10)));
        Assert.Equal("orta", Kademe(new string('9', 12)));
        Assert.Equal("uzun", Kademe(new string('9', 13)));
        Assert.Equal("uzun", Kademe(new string('9', 15)));
        Assert.Equal("cok-uzun", Kademe(new string('9', 16)));
        Assert.Equal("cok-uzun", Kademe(new string('9', 20)));
        Assert.Equal("en-uzun", Kademe(new string('9', 21)));
        Assert.Equal("kisa", DegerBoyu.Kademe(null));
    }
}

public sealed class OrtakGorunumEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol)> istekler = [];

    private const string RiskOzet = """
        {"dusuk":2,"orta":1,"yuksek":1,"pepSayisi":1,"tazelenecek":0,"profilsiz":3,
         "kuralEtkin":false,"listeSiniri":50,
         "yuksekRiskliler":[{"cariId":"c9","cariAd":"Anadolu Holding A.Ş.","puan":75,
           "seviye":"YUKSEK","kaynak":"HESAP","sebepler":["PEP_BEYANI"],
           "olusturuldu":"2026-09-17T08:00:00Z"}]}
        """;

    private const string RiskCariler = """
        [{"id":"c9","ad":"Anadolu Holding A.Ş.","riskSeviye":"YUKSEK","pepBeyani":true}]
        """;

    private const string SubeListesi = """
        [{"id":"s1","ad":"Merkez","adres":"Kapalıçarşı","telefon":"02120000000","aktif":true},
         {"id":"s2","ad":"Nişantaşı","adres":null,"telefon":null,"aktif":true}]
        """;

    private const string Tenant = """
        {"id":"t1","ad":"Kuyumcu","plan":"PRO","aiEtkin":false,"aktif":true,"kurKaynagi":null,
         "maliyetYontemi":null,"varsayilanArayuzModu":"PRO"}
        """;

    private const string Reyonlar = """
        [{"id":"r1","subeId":"s1","ad":"A-VITRIN","tip":"VITRIN","aktif":true,"urunSayisi":2,"yerlesikAdet":4},
         {"id":"r2","subeId":"s1","ad":"B-KASA","tip":"KASA","aktif":true,"urunSayisi":0,"yerlesikAdet":0}]
        """;

    private const string Pusulalar = """
        [{"id":"gp1","pusulaNo":"GP-2026-000001","hareketId":null,"saticiTipi":"ESNAF_MUAF",
          "saticiAdSoyad":"Hüseyin Arslan","saticiTckn":"100******46","ayarId":"K22","gramAgirlik":6.5,
          "hasGrami":5.96,"brutTutar":13785.80,"stopajOran":0.02,"stopajTutar":275.72,
          "netOdenen":13510.08,"teslimTarihi":"2099-09-01T00:00:00Z",
          "duzenlemeSonTarihi":"2099-09-08T00:00:00Z","gecikti":false,"kalanGun":7,
          "iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null}]
        """;

    private const string SureTakibi = """
        {"sure":{"gun":7,"dayanak":"VUK md.234"},"gecikmisSayisi":0,"yaklasanSayisi":0,
         "gecikmis":[],"yaklasan":[],"uyarilar":[]}
        """;

    private const string Siparisler = """
        {"istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":1,"dahaFazlasiVar":false,
         "gecikenSayisi":0,"durumKirilimi":[{"durum":"TASLAK","adet":1}],
         "siparisler":[
           {"id":"s1","no":"S-2026-000002","tur":"MUSTERI_SIPARISI","durum":"TASLAK","cariId":"c1",
            "cariAd":"Karadeniz Kuyumculuk ve Mücevherat Ltd. Şti.","subeId":"s1","sozVerilenGun":null,
            "tahminiTutarTl":null,"aciklama":null,"kalemSayisi":1,"geciken":false,
            "izinliGecisler":["ONAYLANDI","IPTAL"],"duzenlenebilir":true,
            "olusturuldu":"2026-09-27T08:00:00Z","guncellendi":"2026-09-27T08:00:00Z"}]}
        """;

    public OrtakGorunumEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<PosServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private void Mod(bool basit) => Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);

    private sealed class SahteApi(OrtakGorunumEkranTestleri sahip) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            lock (sahip.istekler) sahip.istekler.Add((istek.Method.Method, yol));
            if (yol.EndsWith("/kullanicilar") && istek.Method == HttpMethod.Get)
            {
                // Tam tur v3'teki bant: YALNIZ 429 (oran sınırı) otomatik denemeyi açar.
                return Task.FromResult(new HttpResponseMessage((System.Net.HttpStatusCode)429)
                {
                    Content = new StringContent("""{"hata":"Çok fazla istek gönderildi, lütfen biraz bekleyin"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                });
            }
            var g = yol switch
            {
                var y when y.EndsWith("/masak/risk/ozet") => RiskOzet,
                var y when y.EndsWith("/masak/ozet") => """{"acikBildirim":0,"suresiGecmis":0,"yaklasan":0,"son30GunTespit":0}""",
                var y when y.Contains("/masak/risk/cariler") || y.EndsWith("/cari") => RiskCariler,
                var y when y.EndsWith("/subeler") => SubeListesi,
                var y when y.EndsWith("/admin/tenant") => Tenant,
                var y when y.EndsWith("/reyonlar") => Reyonlar,
                var y when y.EndsWith("/urun-kategorileri") => "[]",
                var y when y.EndsWith("/gider-pusulalari/sure-takibi") => SureTakibi,
                var y when y.EndsWith("/gider-pusulalari") => Pusulalar,
                var y when y.EndsWith("/siparisler") => Siparisler,
                var y when y.Contains("/masak/") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Uyum> RiskSekmesi()
    {
        var e = Render<Kuyumcu.Web.Pages.Uyum>();
        e.Find("#uy-sekme-risk").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/masak/risk/ozet")));
        e.WaitForElement("#uy-risk-cari-sinir");
        return e;
    }

    // ── (G) /uyum?sekme=risk — BASİT modda "cari/cariler" yok ────────────────
    [Fact]
    public void G1_uyum_risk_BASIT_musteri_der_PRO_cari_der()
    {
        var pro = Sozlukler.Getir("tr").Uyum;
        var b = OrtakGorunumSozlugu.Tr;

        Mod(basit: true);
        var e = RiskSekmesi();
        e.WaitForAssertion(() => Assert.Contains(b.UyumYuksekRisklilerBasit, e.Markup));
        var metin = e.Markup;
        Assert.Contains(b.UyumPepSayisiBasit, metin);
        Assert.Contains(b.UyumProfilsizBasit, metin);
        Assert.Contains(string.Format(b.UyumRiskListeSiniriBasit, 200), e.Find("#uy-risk-cari-sinir").TextContent);
        Assert.DoesNotContain(pro.YuksekRiskliler, metin);
        Assert.DoesNotContain(pro.PepSayisi, metin);
        Assert.DoesNotContain(pro.Profilsiz, metin);
    }

    [Fact]
    public void G2_uyum_risk_PRO_muhasebe_dilini_korur()
    {
        var pro = Sozlukler.Getir("tr").Uyum;
        Mod(basit: false);
        var e = RiskSekmesi();
        e.WaitForAssertion(() => Assert.Contains(pro.YuksekRiskliler, e.Markup));
        Assert.Contains(pro.PepSayisi, e.Markup);
        Assert.Contains(string.Format(pro.RiskListeSiniri, 200), e.Find("#uy-risk-cari-sinir").TextContent);
        Assert.DoesNotContain(OrtakGorunumSozlugu.Tr.UyumYuksekRisklilerBasit, e.Markup);
    }

    // ── (G) /isletme?sekme=subeler — BASİT "Kullanımdan kaldır", PRO "Pasife Al"
    private IRenderedComponent<Kuyumcu.Web.Pages.Isletme> Subeler()
    {
        var e = Render<Kuyumcu.Web.Pages.Isletme>();
        e.Find("#isl-sekme-subeler").Click();
        e.WaitForElement("#s-pasife-s1");
        return e;
    }

    [Fact]
    public void G3_isletme_subeler_BASIT_kullanimdan_kaldir_ve_onay_ayni_ad()
    {
        Mod(basit: true);
        var e = Subeler();
        Assert.Equal(OrtakGorunumSozlugu.Tr.IsletmeSubePasifeAlBasit, e.Find("#s-pasife-s1").TextContent.Trim());
        e.Find("#s-pasife-s1").Click();
        var onay = e.WaitForElement("#s-pasife-onayla");
        Assert.Equal(OrtakGorunumSozlugu.Tr.IsletmeSubePasifeAlBasit, onay.TextContent.Trim());
        Assert.DoesNotContain(Sozlukler.Getir("tr").Isletme.PasifeAl, e.Markup);
    }

    [Fact]
    public void G4_isletme_subeler_PRO_pasife_al_korunur()
    {
        Mod(basit: false);
        var e = Subeler();
        Assert.Equal(Sozlukler.Getir("tr").Isletme.PasifeAl, e.Find("#s-pasife-s1").TextContent.Trim());
    }

    // ── (H) /kullanicilar — otomatik deneme notu kendi satırında ─────────────
    [Fact]
    public void H1_oran_sinirinda_otomatik_deneme_notu_blok_satirdir()
    {
        var e = Render<Kuyumcu.Web.Pages.Kullanicilar>();
        var not = e.WaitForElement("#k-otomatik-deneme", TimeSpan.FromSeconds(10));
        Assert.Contains("durum-notu-blok", not.GetAttribute("class") ?? "");
        Assert.NotNull(e.Find("#k-yeniden-dene"));
        var css = CssKaynak.YorumsuzCss(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "ortak.css"));
        Assert.Matches(@"\.durum-notu-blok\s*\{\s*display:\s*block;", css);
    }

    // ── (F) /urunler/agac — boş reyon notu tek satır ─────────────────────────
    [Fact]
    public void F1_bos_reyon_notu_tek_satir_sinifi_tasir_id_ayni()
    {
        var e = Render<Kuyumcu.Web.Pages.UrunAgaci>().AyrintiAc();
        var not = e.WaitForElement("#r-tasi-kilit-r2");
        Assert.Contains("hucre-not-tek-satir", not.GetAttribute("class") ?? "");
        Assert.Contains("mal yok", not.TextContent);
        Assert.Empty(e.FindAll("#r-tasi-kilit-r1"));   // dolu reyonda not yok (davranış aynı)
    }

    // ── /gider-pusulalari — stopaj tutarı ve oranı tek satır (K9 binen) ───────
    [Fact]
    public void P1_stopaj_hucresi_tek_satir()
    {
        var e = Render<Kuyumcu.Web.Pages.GiderPusulalari>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Contains("GP-2026-000001", e.Markup));
        var hucre = Assert.Single(e.FindAll("td.hucre-tek-satir"), td => td.TextContent.Contains('%'));
        Assert.Contains("%2", hucre.TextContent.Replace(" ", ""));
        // Hakem turu 28.09: sağa hiza ortak "sayi" sınıfıyla; satır içi style YOK.
        Assert.True(hucre.ClassList.Contains("sayi"), hucre.OuterHtml);
        Assert.Null(hucre.GetAttribute("style"));
    }

    // ── /siparisler — (A) 27.09 devir ölçümü: sipariş no 2 satıra, uzun müşteri
    //    adı 960 px'te 4 satıra bölünüyordu. No tek satır; ad hücresi makul en az
    //    genişlik sınıfını taşır (css/ortak.css .tablo-sar .hucre-ad). id AYNI.
    [Fact]
    public void A1_siparis_no_tek_satir_musteri_adi_hucresi_ad_sinifi_tasir()
    {
        var e = Render<Kuyumcu.Web.Pages.Siparisler>().AyrintiAc();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sip-satir-s1")));
        var satir = e.Find("#sip-satir-s1");
        var no = satir.QuerySelector("td.hucre-tek-satir");
        Assert.NotNull(no);
        Assert.Equal("S-2026-000002", no!.TextContent.Trim());
        var ad = satir.QuerySelector("td.hucre-ad");
        Assert.NotNull(ad);
        Assert.Equal("Karadeniz Kuyumculuk ve Mücevherat Ltd. Şti.", ad!.TextContent.Trim());
    }

    [Fact]
    public void A2_ad_hucresi_en_az_genislik_kurali_ortak_cssde()
    {
        var css = CssKaynak.YorumsuzCss(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "ortak.css"));
        Assert.Matches(@"\.tablo-sar \.hucre-ad\s*\{\s*min-width:\s*14em;\s*\}", css);
    }

    [Fact]
    public void B4_ad_tutar_liste_satirinda_tutar_kendi_genisligini_alir()
    {
        // 27.09 son tur: /raporlar kasa listesinde uzun ad tutarı sıkıştırıp taşırıyordu.
        var css = CssKaynak.YorumsuzCss(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "ortak.css"));
        Assert.Matches(@"\.panel-liste-satir--is:has\(> \.panel-liste-ad \+ \.panel-liste-tutar\)\s*\{\s*grid-template-columns:\s*minmax\(0, 1fr\) auto;\s*\}", css);
        Assert.Matches(@"\.panel-liste-satir--is > \.panel-liste-ad \+ \.panel-liste-tutar\s*\{\s*white-space:\s*nowrap;\s*\}", css);
    }

    [Fact]
    public void B5_pozisyon_basligi_bolunmez_ayrinti_alt_satira_iner()
    {
        var css = CssKaynak.YorumsuzCss(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "ortak.css"));
        Assert.Matches(@"\.panel-sade \.pozisyon-kart--sade > \.kart-baslik\s*\{\s*flex-wrap:\s*wrap;\s*\}", css);
        Assert.Matches(@"\.panel-sade \.pozisyon-kart--sade #pozisyon-baslik\s*\{\s*white-space:\s*nowrap;\s*\}", css);
    }

    [Fact]
    public void B6_panel_kart_alt_satiri_dar_kartta_tasmaz()
    {
        var css = CssKaynak.YorumsuzCss(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "ortak.css"));
        Assert.Matches(@"\.panel-sade \.panel-soru \.panel-soru-alt > \*\s*\{\s*white-space:\s*normal;\s*min-width:\s*0;\s*\}", css);
    }

    [Fact]
    public void B7_uzun_kademe_stat_degeri_okunaklilik_tabanini_asmaz()
    {
        var css = CssKaynak.YorumsuzCss(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "ortak.css"));
        Assert.Matches(@"\.stat-kart > \.kart-govde > \.stat-deger:is\(\[data-deger-boy=""cok-uzun""\], \[data-deger-boy=""en-uzun""\]\)\s*\{\s*font-size:\s*clamp\(1rem, calc\(var\(--deger-oran\) \* 1cqi\), 1\.5rem\);\s*white-space:\s*normal;\s*\}", css);
    }
}

// TEMA TEMİZLİĞİ 17.10: OrtakGorunumPanelTestleri sınıfı (fikstürüyle) kaldırıldı — iki testi de Panel'in eski BASİT gövdesini (#panel-sade "sabahın beş sorusu" kartları) sınıyordu; o gövde bilerek kaldırıldı, Panel yalnız YildizBugun çizer:
// TEMA TEMİZLİĞİ 17.10: PN1_buyuk_degerler_uzunluk_kademesi_tasir_deger_aynen kaldırıldı — #panel-soru-satis-deger / #panel-soru-kasa-deger / #pozisyon-net (data-deger-boy kademesi) Yıldız Bugün tuvalinde yok; Yıldız'ın büyük rakamı ve pozisyon satırı Yildiz4ATestleri'nde sınanır.
// TEMA TEMİZLİĞİ 17.10: PN2_kim_borclu_adi_tam_metin_ve_title kaldırıldı — "Kim borçlu?" kartı (#panel-borclu-*) Yıldız Bugün tuvalinde yok (bekleyen iş satırları Yildiz4A/4C testlerinde).

/// <summary>ORTAK-1 (D) — Çekmece sayfa konumunu ÇİZİMDEN ÖNCE sabitler.</summary>
public sealed class OrtakGorunumCekmeceTestleri : BunitContext
{
    public OrtakGorunumCekmeceTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    [Fact]
    public void C1_acilista_sabitle_ac_tan_ONCE_bir_kez_cagrilir()
    {
        var e = Render<Cekmece>(p => p.Add(x => x.Varlik, "cari").Add(x => x.Baslik, "Düzenle").Add(x => x.Acik, false));
        Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "kuyumcuCekmece.sabitle");

        e.Render(p => p.Add(x => x.Acik, true));
        var adlar = JSInterop.Invocations.Select(i => i.Identifier).ToList();
        var sabitle = adlar.IndexOf("kuyumcuCekmece.sabitle");
        var ac = adlar.IndexOf("kuyumcuCekmece.ac");
        Assert.True(sabitle >= 0, string.Join(",", adlar));
        Assert.True(ac > sabitle, $"sabitle ({sabitle}) ac'tan ({ac}) önce olmalı");

        // Açıkken yeniden çizim ikinci sabitleme YAPMAZ (konum ilk ölçümde kalır).
        e.Render(p => p.Add(x => x.Baslik, "Düzenle 2"));
        Assert.Single(JSInterop.Invocations, i => i.Identifier == "kuyumcuCekmece.sabitle");
    }

    [Fact]
    public async Task C2_acikken_yok_edilen_cekmece_kilidi_birakir()
    {
        Render<Cekmece>(p => p.Add(x => x.Varlik, "cari").Add(x => x.Acik, true));
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "kuyumcuCekmece.ac");
        await DisposeComponentsAsync();
        Assert.Single(JSInterop.Invocations, i => i.Identifier == "kuyumcuCekmece.birak");
    }

    [Fact]
    public async Task C3_kapali_cekmece_yok_edilince_kilide_dokunmaz()
    {
        Render<Cekmece>(p => p.Add(x => x.Varlik, "cari").Add(x => x.Acik, false));
        await DisposeComponentsAsync();
        Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "kuyumcuCekmece.birak");
    }
}

// ── (G) /vezne — hakem turu 28.09 ────────────────────────────────────────────
// BASİT modda "vezne" kökü ve ÇEKİMLERİ (Veznede, Vezneye gönder, Vezneye
// gönderildi, vezneye gönderilince …) ekrana çıkmaz. Tur K8 ifadesi
// "(?![\p{L}])" ile çekimli biçimleri kaçırıyordu; burada KÖK aranır.
public sealed class OrtakGorunumVezneTestleri : BunitContext
{
    private const string Kuyruk = """
        {"durum":"VEZNEDE","istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":2,
         "dahaFazlasiVar":false,"vezneZorunlu":false,"gerekceAsgari":10,"subeId":"sb1",
         "durumKirilimi":[{"durum":"VEZNEDE","adet":1}],
         "fisler":[
           {"id":"v1","durum":"VEZNEDE","toplamTl":5000.00,"damgaliToplamTl":5000.00,
            "toplamTahmini":false,"cariId":"c1","cariAd":"Anadolu Holding A.Ş.",
            "aciklama":"bilezik","hazirlayanKullaniciId":"u2","hazirlayanAd":"Ali Satıcı",
            "kalemSayisi":1,"hareketFisId":null,"duzenlenebilir":false,"mutabakatGerekli":false,
            "olusturuldu":"2026-09-17T08:00:00Z","veznelendi":"2026-09-17T08:05:00Z",
            "guncellendi":"2026-09-17T08:05:00Z"},
           {"id":"v2","durum":"ACIK","toplamTl":2150.00,"damgaliToplamTl":0.00,
            "toplamTahmini":true,"cariId":null,"cariAd":null,
            "aciklama":null,"hazirlayanKullaniciId":"u1","hazirlayanAd":"Test Kullanıcı",
            "kalemSayisi":2,"hareketFisId":null,"duzenlenebilir":true,"mutabakatGerekli":false,
            "olusturuldu":"2026-09-17T09:00:00Z","veznelendi":null,
            "guncellendi":"2026-09-17T09:00:00Z"}]}
        """;

    private const string Detay = """
        {"id":"v2","durum":"VEZNEDE","cariId":null,"cariAd":null,"aciklama":null,
         "kurSnapshotId":"kur-1","toplamTl":2150.00,"damgaliToplamTl":2150.00,"hareketFisId":null,
         "hazirlayanKullaniciId":"u1","hazirlayanAd":"Test Kullanıcı","tahsilEdenKullaniciId":null,
         "tahsilEdenAd":null,"iptalGerekce":null,"vezneZorunlu":false,"gerekceAsgari":10,
         "duzenlenebilir":false,"veznelenebilir":false,"tahsilEdilebilir":true,
         "iptalEdilebilir":true,"terminal":false,"mutabakatGerekli":false,"toplamTahmini":false,
         "olusturuldu":"2026-09-17T09:00:00Z","guncellendi":"2026-09-17T09:05:00Z",
         "veznelendi":"2026-09-17T09:05:00Z","tahsilEdildi":null,"iptalEdildi":null,
         "kalemler":[{"id":"k1","urunId":"u-1","urunAd":"Bilezik","adet":1.0,
                      "birimFiyatTl":2150.00,"iscilikTl":0.00,"sira":0,"tutarTl":2150.00}]}
        """;

    private static readonly Regex VezneKoku = new("vezne", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public OrtakGorunumVezneTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi())
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/vezne/kuyruk") => Kuyruk,
                var y when y.Contains("/vezne/fisler") => Detay,
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez kasa","paraBirimi":"TRY"}]""",
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Anadolu Holding A.Ş."}]""",
                var y when y.EndsWith("/urunler") => """[{"id":"u-1","ad":"Bilezik"}]""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800}""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private void Mod(bool basit) => Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);

    /// <summary>Görünen metin + title/aria-label (öznitelik kimlikleri — id, Varlik — hariç).</summary>
    private static string Gorunen(IRenderedComponent<Kuyumcu.Web.Pages.Vezne> e) =>
        string.Concat(e.Nodes.Select(n => n.TextContent)) + " " +
        string.Join(" ", e.FindAll("[title],[aria-label],[placeholder]")
            .Select(x => $"{x.GetAttribute("title")} {x.GetAttribute("aria-label")} {x.GetAttribute("placeholder")}"));

    private IRenderedComponent<Kuyumcu.Web.Pages.Vezne> TumYuzeyleriAc()
    {
        var e = Render<Kuyumcu.Web.Pages.Vezne>().AyrintiAc();
        e.WaitForElement("#vz-satir-v1");
        e.Find("#vz-veznele-v2").Click();          // başarı bandı
        e.WaitForElement("#vz-basari-bandi");
        e.Find("#vz-yeni-ac").Click();              // yeni fiş formu (takas/emanet notu)
        e.WaitForElement("#vz-f-tur-kapsam");
        return e;
    }

    [Fact]
    public void G5_vezne_BASIT_modda_vezne_koku_ve_cekimleri_gorunmez()
    {
        var b = OrtakGorunumSozlugu.Tr;
        Mod(basit: true);
        var e = TumYuzeyleriAc();
        var metin = Gorunen(e);
        Assert.Contains(b.VezneDurumVeznedeBasit, e.Find("#vz-satir-v1").TextContent);
        Assert.Contains(b.VeznelendiBasit, e.Find("#vz-basari-bandi").TextContent);
        Assert.Contains(b.VezneTakasEmanetYokBasit, e.Find("#vz-f-tur-kapsam").TextContent);
        Assert.Contains(b.VezneToplamDamgaliBasit, e.Find("#vz-toplam-v1").TextContent);
        var kok = VezneKoku.Matches(metin).Select(m => metin.Substring(Math.Max(0, m.Index - 20), Math.Min(50, metin.Length - Math.Max(0, m.Index - 20)))).ToList();
        Assert.True(kok.Count == 0, "BASİT modda 'vezne' kökü görünüyor: " + string.Join(" | ", kok));
    }

    [Fact]
    public void G6_vezne_PRO_modda_muhasebe_dili_korunur()
    {
        var pro = Sozlukler.Getir("tr").Vezne;
        Mod(basit: false);
        var e = Render<Kuyumcu.Web.Pages.Vezne>().AyrintiAc();
        e.WaitForElement("#vz-satir-v1");
        Assert.Contains(pro.DurumVeznede, e.Find("#vz-satir-v1").TextContent);
        Assert.Equal(pro.VezneyeGonder, e.Find("#vz-veznele-v2").TextContent.Trim());
    }

    [Fact]
    public void G8_vezne_eylem_dugmeleri_ciplak_degil_birincil_ve_ikincil_sinif_tasir()
    {
        // 28.09 canlı görüntü: #vz-yeni-ac sınıfsız çıplak <button> idi (gri kutu, yazı tipi/yarıçap yok).
        Mod(basit: true);
        var e = Render<Kuyumcu.Web.Pages.Vezne>().AyrintiAc();
        e.WaitForElement("#vz-satir-v1");
        Assert.Equal("btn btn-birincil", e.Find("#vz-yeni-ac").GetAttribute("class"));
        e.Find("#vz-yeni-ac").Click();
        e.WaitForElement("#vz-f-kaydet");
        Assert.Equal("btn btn-ikincil", e.Find("#vz-yeni-ac").GetAttribute("class"));
        Assert.Equal("btn btn-birincil", e.Find("#vz-f-kaydet").GetAttribute("class"));
    }

    [Fact]
    public void G10_vezne_tablosu_veri_tablosu_kilit_gerekcesi_katli_satir_dugmesi_sinifli()
    {
        // 28.09 canlı görüntü: tablo sınıfsız (hücre boşluğu yok), kilit kutusu satırı ~300 px uzatıyordu.
        Mod(basit: true);
        var e = Render<Kuyumcu.Web.Pages.Vezne>().AyrintiAc();
        e.WaitForElement("#vz-satir-v1");
        Assert.Contains("veri-tablosu", e.Find("#vz-liste").ClassList);
        var kilit = e.Find("#vz-kilit-v1");
        Assert.Equal("DETAILS", kilit.TagName);
        Assert.False(kilit.HasAttribute("open"));
        Assert.Equal(OrtakGorunumSozlugu.Tr.VezneKilitNeden, kilit.QuerySelector("summary")!.TextContent.Trim());
        Assert.NotNull(kilit.QuerySelector("#degismez-ipucu-VezneFisi-v1"));
        Assert.Equal("btn btn-birincil", e.Find("#vz-veznele-v2").GetAttribute("class"));
    }

    [Fact]
    public void G9_vezne_kart_notlari_kart_ic_boslugunu_alir()
    {
        var css = CssKaynak.YorumsuzCss(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "ortak.css"));
        Assert.Matches(@"\.kart > #vz-defter-notu,\s*\.kart > #vz-sinir-uyarisi\s*\{\s*margin:\s*0;\s*padding:\s*0 1\.5rem 1rem;\s*\}", css);
    }

    [Fact]
    public void G7_vezne_BASIT_metinleri_kokten_arinmis_iki_dil()
    {
        foreach (var m in new[] { OrtakGorunumSozlugu.Tr, OrtakGorunumSozlugu.En })
            foreach (var p in typeof(OrtakGorunumMetinleri).GetProperties().Where(p => p.Name.StartsWith("Vezne")))
                Assert.False(VezneKoku.IsMatch(((string)p.GetValue(m)!).Replace(p.Name, "")), $"{p.Name}");
    }
}
