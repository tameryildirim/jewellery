using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET RAPOR-2 (03.10, t1) — rapor tutarlılıkları + görünüm.
//   T1 "Bugün ne aldım?" gümüş alış/hurda AYRI satırda; yalnız gümüş günü "0,0000 has" YAZMAZ.
//   T2 Bekleyen iş: Panel ile Rapor merkezi AYNI tanım (bildirim + uyum sayaçları + yasal bantlar).
//   T3 Altın pozisyonu: sade kırılımda gösterilen kalemlerin toplamı = ana değer.
//   T4 Kâr-zarar has bölümü: "has gram" adı kalktı (defter gramıyla karışmasın), etiket tek yazım.
//   Görünüm: dönem ön ayarları tek segment, ürün adı sütunu etiketsiz + rozet altta, notlar ⓘ'de,
//   stok yaşı eşikleri katlanır, Panel kâr satırı kısa ve bölünmez, Kim borçlu has eşiği = görüntü hassasiyeti.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class RaporIkiDbTestleri(TestVeritabani vt)
{
    private static JsonElement J(object o) => JsonDocument.Parse(JsonSerializer.Serialize(o)).RootElement;

    private static Hareket Satir(Kiraci k, string tur, decimal tl, decimal has, string maden) => new()
    {
        Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = tur,
        Tarih = DateTime.UtcNow, TlTutari = tl, HasGrami = has, Maden = maden,
        ParaBirimi = "TRY", KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId, Olusturuldu = DateTime.UtcNow,
    };

    // ── R2-01 · T1 (sunucu): gunluk alis/hurda has YALNIZ ALTIN; gümüş madenDagilimi'nde AYRI (ekran bunu okur) ──
    [Fact]
    public async Task R2_01_Gunluk_alis_ve_hurda_gumus_maden_dagiliminda_has_yalniz_altin()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-2-TEST r201");
        sistem.Add(Satir(k, "ALIS", -1_200m, 37.46m, "GUMUS"));
        sistem.Add(Satir(k, "HURDA_ALIS", -1_100m, 37m, "GUMUS"));
        sistem.Add(Satir(k, "HURDA_ALIS", -8_286.02m, 0.916m, "ALTIN"));
        await sistem.SaveChangesAsync();

        using var db = vt.Olustur(k);
        var g = J(await TestKurulum.RaporServisi(db).GunlukOzet(Zaman.YerelBugun().ToString("yyyy-MM-dd"), k.Islem));
        var alis = g.GetProperty("alis");
        Assert.Equal(0m, alis.GetProperty("toplamHas").GetDecimal());               // altın alışı yok
        var ag = alis.GetProperty("madenDagilimi").EnumerateArray().Single();
        Assert.Equal("GUMUS", ag.GetProperty("maden").GetString());
        Assert.Equal(37.46m, ag.GetProperty("toplamHas").GetDecimal());
        var hurda = g.GetProperty("hurda");
        Assert.Equal(0.916m, hurda.GetProperty("toplamHas").GetDecimal());
        var hg = hurda.GetProperty("madenDagilimi").EnumerateArray().ToList();
        Assert.Equal(37m, hg.Single(x => x.GetProperty("maden").GetString() == "GUMUS").GetProperty("toplamHas").GetDecimal());
    }

    // ── R2-02 · TUR 2 (tasarım hakemi KRİTİK): /raporlar/gunluk netSatis = Kâr-zarar "Bugün" hasılatı ve defterdeki altın ──
    //   10 satış (259.152,14 ₺ · 37,234 has) + 1 iade (6.874,15 ₺ · 1 has) + storno çifti (dışlanır) + gümüş satış (has'a girmez).
    [Fact]
    public async Task R2_02_Gunluk_net_satis_kar_zarar_bugun_ile_ayni_sayi()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-2-TEST r202");
        sistem.Add(Satir(k, "SATIS", 250_000m, -37.234m, "ALTIN"));
        sistem.Add(Satir(k, "SATIS", 9_152.14m, -40m, "GUMUS"));
        sistem.Add(Satir(k, "SATIS_IADE", -6_874.15m, 1m, "ALTIN"));
        var kaynak = Satir(k, "SATIS", 5_000m, -0.7m, "ALTIN");
        var ters = Satir(k, "SATIS", -5_000m, 0.7m, "ALTIN");
        ters.StornoKaynakId = kaynak.Id;
        sistem.Add(kaynak);
        sistem.Add(ters);
        await sistem.SaveChangesAsync();

        using var db = vt.Olustur(k);
        var bugun = Zaman.YerelBugun();
        var g = J(await TestKurulum.RaporServisi(db).GunlukOzet(bugun.ToString("yyyy-MM-dd"), k.Islem));
        var n = g.GetProperty("netSatis");
        Assert.Equal(2, n.GetProperty("satisAdet").GetInt32());                         // storno çifti sayılmaz
        Assert.Equal(1, n.GetProperty("iadeAdet").GetInt32());
        Assert.Equal(259_152.14m, n.GetProperty("brutTl").GetDecimal());
        Assert.Equal(6_874.15m, n.GetProperty("iadeTl").GetDecimal());
        Assert.Equal(252_277.99m, n.GetProperty("toplamTl").GetDecimal());
        Assert.Equal(37.234m, n.GetProperty("brutAltinHas").GetDecimal());
        Assert.Equal(36.234m, n.GetProperty("altinToplamHas").GetDecimal());           // gümüş has'a girmez

        var kz = J(await new Kuyumcu.Api.Modules.Maliyet.KarZararServisi(db).Donem(k.Islem, bugun, bugun, null));
        Assert.Equal(kz.GetProperty("hasilatTl").GetDecimal(), n.GetProperty("toplamTl").GetDecimal());
        Assert.Equal(kz.GetProperty("altinHasilatHasDefterGram").GetDecimal(), n.GetProperty("altinToplamHas").GetDecimal());
    }

    // ── R2-03 · TUR 2: /raporlar/aylik iade + netSatisTl = Kâr-zarar "Bu ay" hasılatı ──
    [Fact]
    public async Task R2_03_Aylik_net_satis_kar_zarar_bu_ay_ile_ayni()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-2-TEST r203");
        sistem.Add(Satir(k, "SATIS", 259_152.14m, -37.234m, "ALTIN"));
        sistem.Add(Satir(k, "SATIS_IADE", -6_874.15m, 1m, "ALTIN"));
        await sistem.SaveChangesAsync();

        using var db = vt.Olustur(k);
        var bugun = Zaman.YerelBugun();
        var kzs = new Kuyumcu.Api.Modules.Maliyet.KarZararServisi(db);
        var a = J(await TestKurulum.RaporServisi(db, kzs).AylikOzet(bugun.Year, bugun.Month, k.Islem));
        Assert.Equal(1, a.GetProperty("iade").GetProperty("sayisi").GetInt32());
        Assert.Equal(6_874.15m, a.GetProperty("iade").GetProperty("toplamTl").GetDecimal());
        Assert.Equal(252_277.99m, a.GetProperty("netSatisTl").GetDecimal());
        var ilk = new DateOnly(bugun.Year, bugun.Month, 1);
        var kz = J(await kzs.Donem(k.Islem, ilk, ilk.AddMonths(1).AddDays(-1), null));
        Assert.Equal(kz.GetProperty("hasilatTl").GetDecimal(), a.GetProperty("netSatisTl").GetDecimal());
    }
}

public sealed class RaporIkiSafTestleri
{
    // Panel BASİT kâr satırı kısa öbek — TR + EN, tek kural.
    [Fact]
    public void R2_10_Eksik_sayi_kisa_kalibi_TR_EN()
    {
        Assert.Equal("9 satış, 1 iade", KarRaporSozlugu.EksikSayiKisa(KarRaporSozlugu.Tr, 9, 1));
        Assert.Equal("2 satış", KarRaporSozlugu.EksikSayiKisa(KarRaporSozlugu.Tr, 2, 0));
        Assert.Equal("1 iade", KarRaporSozlugu.EksikSayiKisa(KarRaporSozlugu.Tr, 0, 1));
        Assert.Contains(", ", KarRaporSozlugu.EksikSayiKisa(KarRaporSozlugu.En, 3, 2), StringComparison.Ordinal);
        Assert.Contains("eksik: {1}", KarRaporSozlugu.Tr.PanelKarKismiKisa, StringComparison.Ordinal);
        Assert.DoesNotContain("has gram", KarRaporSozlugu.Tr.AltinKariBaslik, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Has kâr", KarRaporSozlugu.Tr.HasKarEtiket);
    }

    // 375 kartında ürün adı iç span'ı sarar; masaüstünde ad sütunu azami genişlikli ve sarar (KRİTİK kırpılma).
    [Fact]
    public void R2_11_Rapor_css_ad_sutunu_sarar_kurallari()
    {
        var css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "Kuyumcu.Web", "wwwroot", "css", "rapor-gorunum.css"));
        Assert.Matches(@"td:first-child > span \{ white-space: normal; overflow-wrap: anywhere; \}", css);
        Assert.Matches(@"#kz-urun-tablo > tbody > tr > td\.kz-urun-ad \{\s*white-space: normal; overflow-wrap: anywhere; min-width: 12ch; max-width: 26ch;", css);
        // Grafikler: form ızgarasının align-self: end kuralı kartlara uymaz (iki kart aynı üst çizgide).
        Assert.Contains("#kz-grafikler > .kart { align-self: start; }", css, StringComparison.Ordinal);
        // Dönem segmenti filtre yuvasının 152 px taban/en az genişliğini almaz, satıra dizilir.
        Assert.Matches(@"#kz-donem-segment \{\s*display: inline-flex; flex-direction: row; flex-wrap: nowrap;[^}]*flex: 0 0 auto; width: auto; min-width: 0;", css);
        Assert.DoesNotMatch(@"#[a-z-]+ \{[^}]*#[0-9a-fA-F]{3,6}", css);   // sabit renk yok
    }
}

/// <summary>Panel ve Rapor merkezi AYNI bağlamda, AYNI sahte sunucuyla çizilir — iki ekran aynı sayıyı mı söylüyor?</summary>
[Collection("veritabani")]
public sealed class RaporIkiTutarlilikTestleri : BunitContext
{
    private readonly Dictionary<string, string> ozel = new(StringComparer.Ordinal);

    private const string Pozisyon = """
        {"anaMaden":"ALTIN","altin":{"maden":"ALTIN","vitrinGram":159.2652,"hurdaGram":0.916,"cariAlacakGram":0,"cariBorcGram":0,
         "netGram":160.1812,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},
         "digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,"hesaplanmaAni":"2026-10-03T09:00:00Z","donemdenBagimsiz":true}
        """;

    public RaporIkiTutarlilikTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(ozel)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
        Services.AddScoped<HavaServisi>();
        Services.BugunServisleri();   // TEMA TEMİZLİĞİ 17.10: Panel = Yıldız Bugün (YetkiServisi)
    }

    internal sealed class SahteApi(Dictionary<string, string> ozel) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            foreach (var (parca, govde) in ozel)
                if (yol.EndsWith(parca, StringComparison.Ordinal))
                    return Task.FromResult(Yanit(govde));
            var cevap = yol switch
            {
                var y when y.EndsWith("/raporlar/gunluk", StringComparison.Ordinal) => RaporGorunumRaporlarEkranTestleri.Gunluk,
                var y when y.EndsWith("/raporlar/cari-yaslandirma", StringComparison.Ordinal) =>
                    """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""",
                var y when y.EndsWith("/raporlar/vade-takibi", StringComparison.Ordinal) =>
                    """{"ufukGun":7,"gecikenToplam":0,"yaklasanToplam":0,"vadesizToplam":0,"ufukOtesiToplam":0,"acikToplam":0,"cariler":[]}""",
                var y when y.EndsWith("/raporlar/aylik", StringComparison.Ordinal) => """{"gunler":[]}""",
                var y when y.Contains("/raporlar/islem-defteri", StringComparison.Ordinal) => """
                    {"baslangic":"2026-10-03","bitis":"2026-10-03","toplamSatir":0,"ofset":0,"limit":5,
                     "dahaFazlasiVar":false,"ozet":{"tlGiris":0,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":0},
                     "storno":{"adet":0,"tutarTl":0},"satirlar":[]}
                    """,
                var y when y.EndsWith("/bildirimler", StringComparison.Ordinal) =>
                    """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""",
                var y when y.EndsWith("/raporlar/kar-zarar", StringComparison.Ordinal) => """
                    {"hasilatTl":1000,"maliyetTl":800,"karTl":200,"karDurumu":"TAM","satisAdedi":1,
                     "maliyetiEksikSatisAdedi":0,"maliyetiEksikIadeAdedi":0,"maliyetiEksikTutarTl":0,
                     "maliyetiBilinenAdet":1,"maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM"}
                    """,
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati", StringComparison.Ordinal) => """
                    {"tarih":"2026-10-03","tahsilat":{"nakit":0,"kart":0,"havale":0,"diger":0,"toplam":0},
                     "satis":{"sayisi":0,"toplamTl":0,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":0,"faturasizTl":0},
                     "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
                     "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"altinHasGrami":0,"madenDagilimi":[],"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
                     "fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},
                     "fark":0,"mutabik":true,"uyarilar":[],"faturasizIslemler":[],"faturaEslesme":null}
                    """,
                var y when y.EndsWith("/bankalar", StringComparison.Ordinal) => """{"hesaplar":[]}""",
                var y when y.EndsWith("/kasalar/nakit-ayrimi", StringComparison.Ordinal) => "[]",
                var y when y.EndsWith("/pozisyon", StringComparison.Ordinal) => Pozisyon,
                var y when y.EndsWith("/kur/kaynaklar", StringComparison.Ordinal) =>
                    """{"kaynaklar":["ALTINKAYNAK"],"secili":"ALTINKAYNAK","varsayilan":"ALTINKAYNAK"}""",
                var y when y.Contains("/tercihler/benim", StringComparison.Ordinal) => """{"tercihler":{}}""",
                var y when y.EndsWith("/masak/ozet", StringComparison.Ordinal) => """{"acikBildirim":0,"suresiGecmis":0,"yaklasan":0,"son30GunTespit":0}""",
                var y when y.EndsWith("/gider-pusulalari/sure-takibi", StringComparison.Ordinal) =>
                    """{"sure":{"gun":7,"dayanak":"VUK md.234"},"gecikmisSayisi":0,"yaklasanSayisi":0,"gecikmis":[],"yaklasan":[],"uyarilar":[]}""",
                var y when y.EndsWith("/masak/risk/ozet", StringComparison.Ordinal) =>
                    """{"dusuk":0,"orta":0,"yuksek":0,"pepSayisi":0,"tazelenecek":0,"profilsiz":0,"kuralEtkin":true,"listeSiniri":20,"yuksekRiskliler":[]}""",
                var y when y.EndsWith("/stopaj/ozet", StringComparison.Ordinal) =>
                    """{"buAy":"2026-10","buAyKesilenTl":0,"gecikmis":[],"yaklasan":[],"uyariGunu":5}""",
                var y when y.EndsWith("/masak/uyum-ozet", StringComparison.Ordinal) =>
                    """{"saklamaYaklasan":0,"saklamaGun":30,"saklamaKuru":false,"listeTarihi":"2026-10-01","listeEski":false,"listeYasiGun":2,"acikTam":0,"acikBenzerlik":0}""",
                var y when y.EndsWith("/raporlar/katalog", StringComparison.Ordinal) => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/subeler", StringComparison.Ordinal) => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/kasalar", StringComparison.Ordinal) => "[]",
                // TEMA TEMİZLİĞİ 17.10 (YILDIZ-4C): Bugün'ün gün sonu / emanet satırlarının uçları gerçekçi.
                var y when y.EndsWith("/raporlar/kasa-defteri", StringComparison.Ordinal) =>
                    """{"baslangic":"2026-10-02","bitis":"2026-10-02","saatDilimi":"Europe/Istanbul","utcOfsetSaat":3,"yazSaatiRiski":false,"toplamSatir":0,"kasalar":[]}""",
                var y when y.EndsWith("/cari", StringComparison.Ordinal) => "[]",
                _ => """{"sonuclar":[],"limit":10,"dahaFazlasiVar":false}""",
            };
            return Task.FromResult(Yanit(cevap));
        }

        private static HttpResponseMessage Yanit(string g) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    private void Oturum(string rol = "SAHIP") =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", rol, "s1",
                ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));

    // TEMA TEMİZLİĞİ 17.10: Panel = Yıldız Bugün (eski BASİT kartları ve PRO ızgarası kaldırıldı). "İki ekran aynı
    // sayıyı söyler" güvenceleri Bugün'ün rakamı, "Seni bekleyen" satırları ve alt şeridi üzerinde korunur.
    private IRenderedComponent<Kuyumcu.Web.Pages.Panel> Panel()
    {
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.BugunBekle(pozisyon: true);
        return e;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Raporlar> Raporlar()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.True(e.Instance.BugunYuklemesiBitti), TimeSpan.FromSeconds(10));
        return e;
    }

    private static readonly Regex SayiRx = new(@"[+−-]?\d{1,3}(?:\.\d{3})*,\d+", RegexOptions.CultureInvariant);

    private static decimal Sayi(string metin)
    {
        var m = SayiRx.Match(metin).Value.Replace("−", "-", StringComparison.Ordinal).Replace(".", "", StringComparison.Ordinal);
        return decimal.Parse(m, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"));
    }

    // ── R2-20 · T2: yasal bantta 1 iş, bildirim yok → İKİ ekran da AYNI cümle ("iş yok" demez) ──
    [Fact]
    public void R2_20_Bekleyen_is_yasal_bant_iki_ekranda_ayni()
    {
        ozel["/masak/uyum-ozet"] = """{"saklamaYaklasan":0,"saklamaGun":30,"saklamaKuru":false,"listeTarihi":null,"listeEski":true,"listeYasiGun":null,"acikTam":0,"acikBenzerlik":0}""";
        Oturum();
        // PAKET RAPOR-3 (hasım YÜKSEK): iki ekran AYNI satırı gösterir ("Yasal liste yüklenmedi"), yönlendirme cümlesi yok.
        var beklenen = KarRaporSozlugu.Tr.IsYasalListeYok;
        var p = Panel();
        p.WaitForAssertion(() => Assert.Equal(beklenen, p.Find("#yildiz-bekleyen-yasal-liste-eski .yildiz-bugun-sat-metin").TextContent.Trim()));
        Assert.Equal("/uyum?sekme=risk", p.Find("#yildiz-bekleyen-yasal-liste-eski-eylem").GetAttribute("href"));
        Assert.Empty(p.FindAll("#yildiz-bugun-bekleyen-bos"));
        var r = Raporlar();
        r.WaitForAssertion(() => Assert.Equal(beklenen, r.Find("#bugun-bekleyen-yasal-liste-eski-git").TextContent.Trim()));
        Assert.DoesNotContain(RaporBugunSozlugu.Tr.BekleyenYok, r.Find("#bugun-bekleyen").TextContent, StringComparison.Ordinal);
        Assert.Empty(r.FindAll("#bugun-bekleyen #uyb-liste"));     // bant kartın içinde ikinci kez çizilmez
    }

    // ── R2-21 · T2: uyum sayacında iş var (MASAK süresi geçmiş 2) → Rapor merkezi de "iş yok" DEMEZ, sayıyı yazar ──
    [Fact]
    public void R2_21_Bekleyen_is_uyum_sayaci_Rapor_merkezinde_de_sayilir()
    {
        ozel["/masak/ozet"] = """{"acikBildirim":0,"suresiGecmis":2,"yaklasan":0,"son30GunTespit":0}""";
        Oturum();
        var p = Panel();
        p.WaitForAssertion(() => Assert.StartsWith("2 ", p.Find("#yildiz-bekleyen-masak-gecti .yildiz-bugun-sat-metin").TextContent.Trim(), StringComparison.Ordinal));
        var r = Raporlar();
        r.WaitForAssertion(() => Assert.Equal(string.Format(KarRaporSozlugu.Tr.BekleyenUyumSatiri, 2),
            r.Find("#bugun-bekleyen-uyum-git").TextContent.Trim()));
        Assert.Equal("/uyum", r.Find("#bugun-bekleyen-uyum-git").GetAttribute("href"));
        Assert.Empty(r.FindAll("#bugun-bekleyen .bos-durum"));
    }

    // ── R2-22 · T2: tezgâhtar uyum sayaçlarını göremez → istek ATILMAZ, boş durum eskisi gibi ──
    [Fact]
    public void R2_22_Bekleyen_is_yetkisiz_rolde_uyum_istegi_yok()
    {
        ozel["/masak/ozet"] = """{"acikBildirim":0,"suresiGecmis":2,"yaklasan":0,"son30GunTespit":0}""";
        Oturum("TEZGAHTAR");
        var r = Raporlar();
        Assert.Empty(r.FindAll("#bugun-bekleyen-uyum"));
    }

    // ── R2-23 · T3: altın pozisyonu — Rapor merkezinde gösterilen kırılım toplamı = ana değer (hurda dahil);
    //   Panel (Bugün alt şeridi) AYNI ana değeri yazar. TEMA TEMİZLİĞİ 17.10: Panel'in pozisyon KARTI (kırılımlı) kaldırıldı;
    //   Bugün tuvali yalnız net pozisyonu (bir ondalık) ve vitrini yazar — iki ekranın aynı sayıyı söylemesi korunur. ──
    [Fact]
    public void R2_23_Altin_pozisyonu_kirilim_toplami_ana_degere_esit_iki_ekranda()
    {
        Oturum();
        var r = Raporlar();
        r.WaitForAssertion(() => Assert.NotNull(r.Find("#pozisyon-net")));
        var net = Sayi(r.Find("#pozisyon-net").TextContent);
        var kirilim = r.FindAll("#pozisyon-kirilim .pozisyon-kirilim-oge");
        var toplam = kirilim.Sum(o => o.Id == "pozisyon-borc" ? -Sayi(o.TextContent) : Sayi(o.TextContent));
        Assert.Equal(160.1812m, net);
        Assert.Equal(net, toplam);
        Assert.Contains("0,9160 has", r.Find("#pozisyon-hurda").TextContent, StringComparison.Ordinal);

        var p = Panel();
        Assert.Equal($"{YildizBugunSozlugu.AltinPozisyonu("tr")} {Kuyumcu.Web.Bilesenler.Yildiz.YildizBugunBicim.HasKisa(net, YildizBugunSozlugu.GramHas("tr"), true)}",
            p.Find("#yildiz-bugun-pozisyon").TextContent.Trim());
        Assert.Equal("+160,2 g has", p.Find("#yildiz-bugun-pozisyon b").TextContent.Trim());
    }

    // ── R2-24 · T1: yalnız gümüş alınan gün "0,0000 has" YAZILMAZ; gümüş alış ve gümüş hurda kendi satırında ──
    [Fact]
    public void R2_24_Bugun_ne_aldim_yalniz_gumus_has_satiri_yok_gumus_satiri_var()
    {
        ozel["/raporlar/gunluk"] = """
            {"tarih":"2026-10-03","satis":{"adet":0,"toplamTl":0,"toplamHas":0},
             "alis":{"adet":2,"toplamTl":2400,"toplamHas":0,"altinToplamHas":0,
                     "madenDagilimi":[{"maden":"GUMUS","madenAdi":"Gümüş","toplamHas":37.46,"satirSayisi":2}]},
             "hurda":{"toplamHas":0.916,"altinToplamHas":0.916,"odenenTl":9386.02,
                      "madenDagilimi":[{"maden":"GUMUS","madenAdi":"Gümüş","toplamHas":37,"satirSayisi":1},
                                       {"maden":"ALTIN","madenAdi":"Altın","toplamHas":0.916,"satirSayisi":1}]},
             "netHasGramHareketi":0,"hareketSayisi":4,"kasalar":[]}
            """;
        Oturum();
        var r = Raporlar();
        r.WaitForAssertion(() => Assert.NotNull(r.Find("#bugun-alis-adet")));
        Assert.Empty(r.FindAll("#bugun-alis-has"));                                     // "0,0000 has" yok
        Assert.Equal("Gümüş: saf 37,46 g", r.Find("#bugun-alis-maden-GUMUS").TextContent.Trim());
        Assert.Equal("0,9160 has", r.Find("#bugun-hurda-has").TextContent.Trim());      // altın hurda has'ı yerinde
        Assert.Equal("Gümüş: saf 37,00 g", r.Find("#bugun-hurda-maden-GUMUS").TextContent.Trim());
        Assert.Empty(r.FindAll("#bugun-hurda-maden-ALTIN"));                            // altın has satırında, iki kez değil
    }

    // ── R2-25 · T1: altın + gümüş alış → has satırı (yalnız altın) + gümüş ayrı; gramlar toplanmaz ──
    [Fact]
    public void R2_25_Bugun_ne_aldim_altin_ve_gumus_ayri_satir()
    {
        ozel["/raporlar/gunluk"] = """
            {"tarih":"2026-10-03","satis":{"adet":0,"toplamTl":0,"toplamHas":0},
             "alis":{"adet":5,"toplamTl":158120.75,"toplamHas":22.3046,"altinToplamHas":22.3046,
                     "madenDagilimi":[{"maden":"ALTIN","madenAdi":"Altın","toplamHas":22.3046,"satirSayisi":3},
                                      {"maden":"GUMUS","madenAdi":"Gümüş","toplamHas":37.46,"satirSayisi":2}]},
             "hurda":{"toplamHas":0,"odenenTl":0,"madenDagilimi":[]},
             "netHasGramHareketi":0,"hareketSayisi":5,"kasalar":[]}
            """;
        Oturum();
        var r = Raporlar();
        r.WaitForAssertion(() => Assert.NotNull(r.Find("#bugun-alis-has")));
        Assert.Equal("22,3046 has", r.Find("#bugun-alis-has").TextContent.Trim());
        Assert.Equal("Gümüş: saf 37,46 g", r.Find("#bugun-alis-maden-GUMUS").TextContent.Trim());
        Assert.Empty(r.FindAll("#bugun-hurda"));
    }

    // ── R2-26 · Panel "Kim borçlu" has eşiği = görüntü hassasiyeti (0,00005): "0,0000 has" satırı çıkmaz ──
    [Fact]
    public void R2_26_Kim_borclu_has_esigi_goruntu_hassasiyeti()
    {
        ozel["/raporlar/cari-yaslandirma"] = """
            {"yaslananCariler":[
              {"cariId":"c1","cariAd":"Küçük Has","cariTur":"MUSTERI","tlBakiye":0,"hasGramBakiye":0.00004,
               "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyesiVar":false,"gunSayisi":3,"dilim":"0-30"},
              {"cariId":"c2","cariAd":"Eşik Has","cariTur":"MUSTERI","tlBakiye":0,"hasGramBakiye":0.00005,
               "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyesiVar":false,"gunSayisi":3,"dilim":"0-30"}],
             "ozetTl":{},"toplamSatir":2,"ofset":0,"limit":500,"dahaFazlasiVar":false}
            """;
        Oturum();
        var p = Panel();
        p.WaitForAssertion(() => Assert.NotNull(p.Find("#yildiz-bekleyen-cari-c2")));
        Assert.Contains("0,0001 has", p.Find("#yildiz-bekleyen-cari-c2 .yildiz-bugun-sat-ek").TextContent, StringComparison.Ordinal);
        Assert.Empty(p.FindAll("#yildiz-bekleyen-cari-c1"));
        Assert.DoesNotContain("0,0000 has", p.Find("#yildiz-bugun").TextContent, StringComparison.Ordinal);
        Assert.Equal(0.00005m, Kuyumcu.Web.Pages.Panel.HasGosterimEsigi);
    }

    // ── R2-27 · Panel kâr satırı kısa ve tek öbek. TEMA TEMİZLİĞİ 17.10: Bugün alt şeridinde maliyeti eksik satış/iade
    //   varken kâr rakamı BASILMAZ (fail-closed) — yalnız "maliyet eksik" yazar; kısmi rakam ve uzun kapsam cümlesi yok. ──
    [Fact]
    public void R2_27_Panel_kar_satiri_kisa_ve_bolunmez()
    {
        ozel["/raporlar/kar-zarar"] = """
            {"hasilatTl":327878.00,"maliyetTl":145431,"karTl":20419.65,"karDurumu":"KISMI","satisAdedi":11,
             "maliyetiEksikSatisAdedi":9,"maliyetiEksikIadeAdedi":1,"maliyetiEksikTutarTl":95566.52,
             "maliyetiBilinenAdet":2,"maliyetiBilinmeyenAdet":9,"guvenilirlik":"DUSUK"}
            """;
        Oturum();
        var p = Panel();
        p.WaitForAssertion(() => Assert.NotNull(p.Find("#yildiz-bugun-kar")));
        var metin = p.Find("#yildiz-bugun-kar").TextContent.Trim();
        Assert.Equal($"{YildizBugunSozlugu.Kar("tr")} {YildizBugunSozlugu.MaliyetEksik("tr")}", metin);
        Assert.DoesNotContain(Bicim.Tl(20419.65m), p.Find("#yildiz-bugun").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain(Bicim.TlKurus(20419.65m), p.Find("#yildiz-bugun").TextContent, StringComparison.Ordinal);
        Assert.Equal("/raporlar/kar-zarar", p.Find("#yildiz-bugun-kar").GetAttribute("href"));   // ayrıntı Kâr-zarar'da
    }

    // ── R2-28 · Rapor merkezi BASİT: "Diğer raporlar" açıklaması ⓘ arkasında (düz satır değil), bağlantı korunur ──
    // ── R2-29 · TUR 2 (tasarım hakemi KRİTİK): iadeli günde Panel ve Rapor merkezi "Bugün ne sattım" AYNI NET sayı
    //   (Kâr-zarar "Bugün" ile aynı tanım) + iade kapsam satırı; brüt 259.152,14 GÖRÜNMEZ. ──
    internal const string GunlukIadeli = """
        {"tarih":"2026-10-03","satis":{"adet":10,"toplamTl":259152.14,"toplamHas":37.234,"altinToplamHas":37.234},
         "netSatis":{"satisAdet":10,"iadeAdet":1,"brutTl":259152.14,"iadeTl":6874.15,"toplamTl":252277.99,
                     "brutAltinHas":37.234,"iadeAltinHas":1,"altinToplamHas":36.234},
         "alis":{"adet":0,"toplamTl":0,"toplamHas":0},"hurda":{"toplamHas":0,"odenenTl":0},
         "netHasGramHareketi":36.234,"hareketSayisi":11,"kasalar":[]}
        """;

    [Fact]
    public void R2_29_Iadeli_gun_net_satis_iki_ekranda_ayni_iade_satiri_gorunur()
    {
        ozel["/raporlar/gunluk"] = GunlukIadeli;
        Oturum();
        var iade = string.Format(KarRaporSozlugu.Tr.BugunIadeSatiri, 1, Bicim.Tl(6874.15m));
        var p = Panel();
        // Bugün'ün dev rakamı NET satıştır (Rapor merkezi ve Kâr-zarar "Bugün" ile aynı); brüt görünmez.
        // Not: has ve iade alt satırı Bugün tuvalinde yazılmaz (bilerek) — Rapor merkezinde.
        p.WaitForAssertion(() => Assert.Equal(Bicim.TlKurus(252277.99m), p.Find("#yildiz-bugun-rakam").TextContent.Trim()));
        Assert.DoesNotContain(Bicim.Tl(259152.14m), p.Find("#yildiz-bugun").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain(Bicim.TlKurus(259152.14m), p.Find("#yildiz-bugun").TextContent, StringComparison.Ordinal);
        var r = Raporlar();
        r.WaitForAssertion(() => Assert.Equal(Bicim.Tl(252277.99m), r.Find("#bugun-satis-deger").TextContent.Trim()));
        Assert.Equal(36.234m, Sayi(r.Find("#bugun-satis-has").TextContent));
        Assert.Equal(iade, r.Find("#bugun-satis-iade").TextContent.Trim());
    }

    // ── R2-34 · TUR 2: Panel PRO "Günlük satış" NET. TEMA TEMİZLİĞİ 17.10: PRO stat kartları ("Günlük satış",
    //   "Günün nakit akışı") kaldırıldı — PRO'da da Bugün çizilir; dev rakam AYNI net satıştır, brüt görünmez. ──
    [Fact]
    public void R2_34_Panel_PRO_gunluk_satis_net_nakit_akisi_ayni_satistan()
    {
        ozel["/raporlar/gunluk"] = GunlukIadeli;
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1",
                ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.BugunBekle();
        Assert.Equal(Bicim.TlKurus(252277.99m), e.Find("#yildiz-bugun-rakam").TextContent.Trim());
        Assert.DoesNotContain(Bicim.TlKurus(259152.14m), e.Find("#yildiz-bugun").TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#panel-izgara"));
    }

    // ── R2-32 · TUR 2 (hasım DÜŞÜK): nakit sınırı aşımı Rapor merkezinde de Panel'le AYNI — şerit kartta,
    //   TevsikIhlali bildirimi listede ikinci kez yok, boş durum Panel'in cümlesi ──
    [Fact]
    public void R2_32_Nakit_siniri_asimi_iki_ekranda_ayni_tanim()
    {
        ozel["/bildirimler"] = """
            {"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[
              {"id":"tv1","tur":"TEVSIK_KURTARMA","onem":"KRITIK","baslik":"Nakit sınırı aşıldı — bildirim için son gün 08.10",
               "govde":"x","hedefRota":"/uyum?sekme=nakit","ilgiliVarlikTuru":"TevsikIhlali","ilgiliVarlikId":"i1",
               "olusturuldu":"2026-10-03T07:00:00Z","kapatildi":null,"kapatanKullaniciId":null,"kapatmaNotu":null}]}
            """;
        ozel["/tevsik/ihlaller"] = """
            {"ihlaller":[{"id":"i1","tur":"GUNLUK_KUMULATIF","yon":"ODEME","cariId":"c9","cariAd":"Hurda Satıcı","ihlalGunu":"2026-10-01","nakitToplamTl":36000,"hadTl":30000,"bildirimSonGunu":"2026-10-08","durum":"ACIK","kalanIsGunu":3,"kapatmaTarihi":null,"bildirimTarihi":null,"bildirimEvrakNo":null,"gec":false}],
             "kapanmamisSayisi":1,"sinir":200,"dahaVar":false,"bayramEksik":false}
            """;
        Oturum();
        // PAKET RAPOR-3 (hasım YÜKSEK): aşım iki ekranda AYNI satır bileşeniyle ("1 nakit sınırı aşımı" → Uyum › Nakit).
        var cumle = string.Format(KarRaporSozlugu.Tr.IsNakitSiniri, 1);
        var p = Panel();
        p.WaitForAssertion(() => Assert.Equal(cumle, p.Find("#yildiz-bekleyen-yasal-nakit .yildiz-bugun-sat-metin").TextContent.Trim()));
        Assert.Equal("/uyum?sekme=nakit", p.Find("#yildiz-bekleyen-yasal-nakit-eylem").GetAttribute("href"));
        Assert.Empty(p.FindAll("#yildiz-bekleyen-is-tv1"));
        var r = Raporlar();
        r.WaitForAssertion(() => Assert.Equal(cumle, r.Find("#bugun-bekleyen-yasal-nakit-git").TextContent.Trim()));
        Assert.Empty(r.FindAll("#bugun-bekleyen #nsp-odeme"));     // kırmızı kutu kartta ikinci kez çizilmez
        Assert.Empty(r.FindAll("#bugun-bekleyen-liste"));
        Assert.Empty(r.FindAll("#bugun-is-git-tv1"));
    }

    // ── R2-33 · TUR 2 (hasım DÜŞÜK): görüntü hassasiyetinden küçük gümüş (0,0049 g) "saf 0,00 g" satırı açmaz ──
    [Fact]
    public void R2_33_Cok_kucuk_gumus_saf_sifir_satiri_uretmez()
    {
        ozel["/raporlar/gunluk"] = """
            {"tarih":"2026-10-03","satis":{"adet":0,"toplamTl":0,"toplamHas":0},
             "alis":{"adet":1,"toplamTl":1,"toplamHas":0,"altinToplamHas":0,
                     "madenDagilimi":[{"maden":"GUMUS","madenAdi":"Gümüş","toplamHas":0.0049,"satirSayisi":1}]},
             "hurda":{"toplamHas":0,"odenenTl":0},"netHasGramHareketi":0,"hareketSayisi":1,"kasalar":[]}
            """;
        Oturum();
        var r = Raporlar();
        r.WaitForAssertion(() => Assert.NotNull(r.Find("#bugun-alis-adet")));
        Assert.Empty(r.FindAll("#bugun-alis-maden-GUMUS"));
        Assert.DoesNotContain("saf 0,00 g", r.Find("#bugun-alis").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void R2_28_Diger_raporlar_notu_ipucunda()
    {
        Oturum();
        var r = Raporlar();
        var satir = r.Find("#bugun-diger-raporlar");
        Assert.Equal("DIV", satir.TagName);
        Assert.NotNull(satir.QuerySelector("details.zo-ipucu #bugun-gorunum-git"));
        Assert.Equal(KarRaporSozlugu.Tr.DigerRaporlarAc, satir.QuerySelector(":scope > span")!.TextContent.Trim());
        // Açıklama metni YALNIZ ⓘ içinde: satırın doğrudan çocukları etiket + ipucudur, düz <p> yok.
        Assert.Contains("t4-ipucu-satir", satir.ClassList);
        Assert.Equal(["SPAN", "DETAILS"], satir.Children.Select(c => c.TagName).ToArray());
    }
}

[Collection("veritabani")]
public sealed class RaporIkiRaporlarProTestleri : BunitContext
{
    public RaporIkiRaporlarProTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => aylikGovde)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<Kuyumcu.Web.Servisler.RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private string aylikGovde = KarRaporRaporlarEkranTestleri.AylikHesaplanamadi;

    /// <summary>PAKET RAPOR-3: aynı sahte API başka sınıflarda da kullanılır (kopya yazılmaz).</summary>
    internal static HttpMessageHandler SahteApiOlustur(Func<string> aylik) => new SahteApi(aylik);

    private sealed class SahteApi(Func<string> aylik) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/aylik") => aylik(),
                var y when y.EndsWith("/raporlar/stok-yaslandirma") => """
                    {"olculduAn":"2026-10-03T06:00:00Z","subeId":null,
                     "kovalar":[{"kod":"G0_30","altSinirGun":0,"ustSinirGun":30,"adet":5,"hasGram":5,"altinHasGram":5,"maliyetTl":0,"maliyetiBilinmeyenAdet":0,"madenKirilimi":[]}],
                     "toplamAdet":5,"altinHasGram":5,
                     "madenDagilimi":[{"maden":"ALTIN","madenAdi":"Altın","adet":4,"toplamGram":5},{"maden":"GUMUS","madenAdi":"Gümüş","adet":1,"toplamGram":9}],
                     "yasiBilinmeyenAdet":0,
                     "devir":{"pencereGun":90,"tuketilenAdet":1,"kalanAdet":5,"devirHizi":null,"yillikTahmini":null,"ortalamaRafOmruGun":null,
                              "tahminiSatisSuresiGun":null,"ortalamaYasGun":12.4,"yontem":"PARTI_TUKETIMI","tanim":"t"},
                     "oluStok":{"esikGun":180,"toplamSatir":0,"toplamAdet":0,"ofset":0,"limit":100,"dahaFazlasiVar":false,"urunler":[]},
                     "guvenilirlik":"TAM","uyarilar":[]}
                    """,
                var y when y.EndsWith("/raporlar/gunluk") => RaporGorunumRaporlarEkranTestleri.Gunluk,
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") => """
                    {"tarih":"2026-10-03","tahsilat":{"nakit":0,"kart":0,"havale":0,"diger":0,"toplam":0},
                     "satis":{"sayisi":0,"toplamTl":0,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":0,"faturasizTl":0},
                     "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
                     "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"altinHasGrami":0,"madenDagilimi":[],"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
                     "fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},
                     "fark":0,"mutabik":true,"uyarilar":[],"faturasizIslemler":[],"faturaEslesme":null}
                    """,
                var y when y.EndsWith("/raporlar/cari-yaslandirma") =>
                    """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/kullanicilar") => "[]",
                var y when y.EndsWith("/kasalar") => "[]",
                var y when y.EndsWith("/reyonlar") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    // ── R2-31 · TUR 2 (tasarım hakemi KRİTİK, PRO "AYLIK SATIŞ"): iade varsa kart NET (Kâr-zarar "Bu ay" hasılatı) + alt satır brüt ──
    [Fact]
    public void R2_31_Aylik_satis_karti_iadeli_ayda_net_ve_brut_alt_satiri()
    {
        aylikGovde = KarRaporRaporlarEkranTestleri.AylikHesaplanamadi.Replace("\"yil\":2026,",
            "\"yil\":2026,\"iade\":{\"sayisi\":1,\"toplamTl\":6874.15},\"netSatisTl\":188195.64,", StringComparison.Ordinal);
        Assert.Contains("netSatisTl", aylikGovde, StringComparison.Ordinal);
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1",
                ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#rp-ozet-kartlar")), TimeSpan.FromSeconds(10));
        var kart = e.Find("#rp-ozet-kartlar .stat-kart");
        Assert.Equal(Bicim.Tl(188195.64m), kart.QuerySelector(".stat-deger")!.TextContent.Trim());
        Assert.Equal(string.Format(KarRaporSozlugu.Tr.AylikSatisNetAlt, Bicim.Tl(195069.79m), 1),
            kart.QuerySelector(".stat-alt")!.TextContent.Trim());
    }

    // ── R2-30 · Stok yaşı: açıklama ve madenler/devir notları ⓘ'de; isteğe bağlı eşikler KAPALI katlanırda; 3+3 ızgara kimliği ──
    [Fact]
    public void R2_30_Stok_yasi_notlar_ipucunda_esikler_katlanir()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1",
                ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sy-devir-izgara")), TimeSpan.FromSeconds(10));
        var ayar = e.Find("details#sy-ayarlar");
        Assert.False(ayar.HasAttribute("open"));
        Assert.NotNull(ayar.QuerySelector("#sy-esik"));
        Assert.NotNull(ayar.QuerySelector("#sy-pencere"));
        Assert.NotNull(e.Find("details#sy-nasil-ipucu #sy-aciklama"));
        Assert.NotNull(e.Find("details#sy-devir-ipucu #sy-uyari-maden"));
        Assert.NotNull(e.Find("details#sy-devir-ipucu #sy-uyari-devir"));
        Assert.Equal(6, e.Find("#sy-devir-izgara").Children.Length);
    }
}

[Collection("veritabani")]
public sealed class RaporIkiKarZararEkranTestleri : BunitContext
{
    private const string Urun = """
        {"satirlar":[
          {"urunId":"u1","kod":"K-1","ad":"22 Ayar El İşçiliği Kalemli Hasır Bilezik Takımı (Trabzon Örgü, Çift Sıra)","satisAdedi":1,"iadeAdedi":0,"maden":"ALTIN",
           "hasilatTl":36391.32,"maliyetTl":null,"bilinenHasilatTl":null,"karTl":null,"karDurumu":"HESAPLANAMADI",
           "maliyetiEksikSatisAdedi":1,"maliyetiEksikTutarTl":36391.32,"hasilatHas":5.33,"maliyetHas":null,"karHas":null,
           "karMarjiYuzde":null,"maliyetiBilinmeyenAdet":1,"guvenilirlik":"DUSUK","hasKarDurumu":"HESAPLANAMADI","hasMaliyetiEksikSatisAdedi":1}],
         "urunSayisi":1,"limit":50,"dahaFazlasiVar":false,"guvenilirlik":"DUSUK"}
        """;

    private readonly List<string> istekler = [];

    public RaporIkiKarZararEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.RaporServisi>();
        Services.AddSingleton<TimeProvider>(new SabitSaat(new DateTimeOffset(2026, 10, 15, 9, 0, 0, TimeSpan.Zero)));
    }

    /// <summary>Sabit saat (ayın 15'i — "bu ay" ≠ "bugün"); yerel saat dilimi Europe/Istanbul.</summary>
    private sealed class SabitSaat(DateTimeOffset utc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utc;
        public override TimeZoneInfo LocalTimeZone { get; } = Kuyumcu.Api.Common.Zaman.Isletme;
    }

    private sealed class SahteApi(List<string> istekler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            lock (istekler) istekler.Add(yol);
            var g = yol switch
            {
                var y when y.Contains("kar-zarar/urun") => Urun,
                var y when y.Contains("kar-zarar") => MadenAyrimiKarZararEkranTestleri.OzetTam,
                var y when y.Contains("stok-degerlemesi") => MadenAyrimiKarZararEkranTestleri.StokBilinmiyor,
                var y when y.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.KarZarar> Ciz(bool basit)
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-panel-stok .stat-izgara")));
        return c;
    }

    // ── R2-40 · T4: has bölümü başlığı "has gram" DEMEZ (defter gramıyla karışmasın); iki modda aynı ──
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void R2_40_Altin_kari_basligi_satis_tutarinin_altin_karsiligi(bool basit)
    {
        var c = Ciz(basit);
        var b = c.Find("#kz-altin-kari-baslik").TextContent.Trim();
        Assert.Equal(KarRaporSozlugu.Tr.AltinKariBaslik, b);
        Assert.DoesNotContain("has gram", b, StringComparison.OrdinalIgnoreCase);
    }

    // ── R2-41 · Ürünler: ad hücresi etiketsiz, ad ve rozet ayrı blok; "Has kâr" tek yazım (th + kart etiketi) ──
    [Fact]
    public void R2_41_Urunler_ad_hucresi_rozet_altta_has_kar_etiketi()
    {
        var c = Ciz(basit: false);
        var tablo = c.Find("#kz-urun-tablo");
        var ad = tablo.QuerySelector("tbody td.kz-urun-ad")!;
        Assert.Null(ad.GetAttribute("data-etiket"));
        Assert.StartsWith("22 Ayar El İşçiliği", ad.QuerySelector(".kz-urun-ad-metin")!.TextContent, StringComparison.Ordinal);
        Assert.NotNull(ad.QuerySelector(".kz-urun-ad-rozet"));
        var basliklar = tablo.QuerySelectorAll("thead th").Select(t => t.TextContent.Trim()).ToList();
        Assert.Contains(KarRaporSozlugu.Tr.HasKarEtiket, basliklar);
        Assert.DoesNotContain("HAS kâr", basliklar);
        Assert.Equal(KarRaporSozlugu.Tr.HasKarEtiket, tablo.QuerySelector("tbody td.kz-urun-has")!.GetAttribute("data-etiket"));
        Assert.NotNull(c.Find("#kz-urun-sar"));
    }

    // ── R2-42 · Dönem ön ayarları tek segment grubunda; birincil düğme yok; seçili olan aria-pressed ──
    [Fact]
    public void R2_42_Donem_on_ayarlari_tek_segment_secili_vurgulu()
    {
        var c = Ciz(basit: false);
        var seg = c.Find("#kz-donem-segment");
        Assert.Equal("group", seg.GetAttribute("role"));
        foreach (var k in new[] { "kz-gecen-ay", "kz-bugun", "kz-bu-ay" })
        {
            var d = seg.QuerySelector($"#{k}")!;
            Assert.Contains("musteri-segment-oge", d.ClassList);
            Assert.DoesNotContain("btn-birincil", d.ClassList);
        }
        Assert.Equal("true", c.Find("#kz-bu-ay").GetAttribute("aria-pressed"));        // açılış: bu ay
        Assert.Equal("false", c.Find("#kz-bugun").GetAttribute("aria-pressed"));
        c.Find("#kz-bugun").Click();
        c.WaitForAssertion(() => Assert.Equal("true", c.Find("#kz-bugun").GetAttribute("aria-pressed")));
        Assert.Equal("false", c.Find("#kz-bu-ay").GetAttribute("aria-pressed"));
        c.Find("#kz-gecen-ay").Click();
        c.WaitForAssertion(() => Assert.Equal("true", c.Find("#kz-gecen-ay").GetAttribute("aria-pressed")));
        Assert.Contains("musteri-segment-oge--secili", c.Find("#kz-gecen-ay").ClassList);
        Assert.Empty(c.FindAll("#kz-panel-ozet ~ * .btn-birincil#kz-bu-ay"));
    }
}
