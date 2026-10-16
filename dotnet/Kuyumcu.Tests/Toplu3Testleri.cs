using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET TOPLU-3 — demo videosu bulguları + RAPOR-YASLANDIRMA tasarım YÜKSEK'leri
// + TOPLU-2 YARIM kalanları. YALNIZ GÖRÜNÜM (hasımsız paket): her kabul maddesi
// burada bir testle sabitlenir ve mutasyonla kırmızıya döndüğü gösterilir.
//   bUnit: Panel yasal liste bandı (uyarı GİZLENMEZ, dil sade + ayrıntı bağlantısı) ·
//          Kur ücretsiz kaynak rozeti · Cari yaşlandırma (sıfır "—", (i) açıklama,
//          döviz tfoot satırı + yön, has sütunu dar ekran sınıfı).
//   CSS:   toplu3.css kuralları (yükleme sırası O1, kart düzeni, arama tam ekran,
//          kurulum ızgarası, hurda açıcı, vitrin ipucu boyu, uyum geniş kart, kimlik kutusu).
//   node:  demo-tohumla.js ilk kuru 150 sn bekler · işletme kartını doldurur.
// ═════════════════════════════════════════════════════════════════════════════

internal static class Toplu3Ortak
{
    public static string Kok()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "Kuyumcu.Web"))) d = d.Parent;
        return d!.FullName;
    }

    public static string Dosya(params string[] parca) => File.ReadAllText(Path.Combine([Kok(), .. parca]));

    /// <summary>toplu3.css (yorumsuz).</summary>
    public static string Css() => Regex.Replace(
        Dosya("Kuyumcu.Web", "wwwroot", "css", "toplu3.css"), @"/\*.*?\*/", "", RegexOptions.Singleline);

    /// <summary>Seçicinin bildirim bloğu (yoksa null). Aranan metin "seçici {" biçimindedir.</summary>
    public static string? Blok(string css, string secici)
    {
        var i = css.IndexOf(secici + " {", StringComparison.Ordinal);
        if (i < 0) return null;
        var a = css.IndexOf('{', i); var k = css.IndexOf('}', a);
        return css[(a + 1)..k];
    }

    /// <summary>"@media (max-width: N)" bloğunun içi (ilk eşleşme, iç içe süslü parantez sayılır).</summary>
    public static string Medya(string css, string sorgu, int baslangic = 0)
    {
        var i = css.IndexOf(sorgu, baslangic, StringComparison.Ordinal);
        Assert.True(i >= 0, sorgu + " yok");
        var a = css.IndexOf('{', i); var derinlik = 0;
        for (var k = a; k < css.Length; k++)
        {
            if (css[k] == '{') derinlik++;
            else if (css[k] == '}' && --derinlik == 0) return css[(a + 1)..k];
        }
        return css[(a + 1)..];
    }
}

public sealed class Toplu3CssTestleri
{
    [Fact]
    public void T3_toplu3_css_ortak_cssten_once_yuklenir()
    {
        var html = Toplu3Ortak.Dosya("Kuyumcu.Web", "wwwroot", "index.html");
        var t = html.IndexOf("css/toplu3.css", StringComparison.Ordinal);
        var o = html.IndexOf("css/ortak.css", StringComparison.Ordinal);
        Assert.True(t > 0 && o > t, "toplu3.css index.html'de ortak.css'ten ÖNCE olmalı");
    }

    [Fact]
    public void T3_Sabit_renk_yok_yalniz_belirtec()
    {
        var css = Toplu3Ortak.Css();
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b(?![\w-])(?=[^{]*;)", Regex.Replace(css, @"#[a-z][\w-]*", ""));
        Assert.DoesNotContain("rgb(", css, StringComparison.Ordinal);
    }

    [Fact]
    public void T3_Yaslandirma_sifir_hucre_tire_ve_sayi_dolgusu()
    {
        var css = Toplu3Ortak.Css();
        Assert.Contains("content: \"—\"", Toplu3Ortak.Blok(css, "#yas-tablo td.yas-sifir::before"));
        Assert.Contains("clip-path: inset(50%)", Toplu3Ortak.Blok(css, "#yas-tablo td.yas-sifir > .yas-deger"));
        Assert.Contains("padding-inline: 16px", Toplu3Ortak.Blok(css, "#yas-tablo :is(th, td).sayi"));
    }

    [Fact]
    public void T3_Yaslandirma_dar_masaustunde_bos_has_sutunu_gizlenir()
    {
        var m = Toplu3Ortak.Medya(Toplu3Ortak.Css(), "@media (max-width: 1365px)");
        Assert.Contains("#yas-tablo.yas-has-bos :is(#yas-has-baslik, td.yas-has, #yas-toplam-has)", m);
        Assert.Contains("display: none", m);
    }

    [Fact]
    public void T3_Yaslandirma_375_satir_karti()
    {
        var css = Toplu3Ortak.Css();
        var m = Toplu3Ortak.Medya(css, "@media (max-width: 640px)");
        Assert.Contains("display: grid", Toplu3Ortak.Blok(m, "#yas-tablo tr"));
        Assert.Contains("display: none", Toplu3Ortak.Blok(m, "#yas-tablo thead"));
        Assert.Contains("display: none", Toplu3Ortak.Blok(m, "#yas-tablo tr > td.yas-sifir"));
        Assert.Contains("content: attr(data-etiket)", Toplu3Ortak.Blok(m, "#yas-tablo tr > td.sayi::before"));
    }

    [Fact]
    public void T3_Kimlik_eksik_kutusunda_degismez_basligi_gizli()
    {
        var b = Toplu3Ortak.Blok(Toplu3Ortak.Css(), "#cikti-kimlik-uyari .degismez-kilit");
        Assert.NotNull(b);
        Assert.Contains("display: none", b);
        Assert.Contains("#cikti-kimlik-uyari .degismez-baslik", Toplu3Ortak.Css());
    }

    [Fact]
    public void T3_Kurulum_375_adim_izgarasi_eylem_ayni_satirda()
    {
        var css = Toplu3Ortak.Css();
        var i = css.IndexOf("#kur-adimlar", StringComparison.Ordinal);
        var m = Toplu3Ortak.Medya(css, "@media (max-width: 640px)", css.LastIndexOf("@media", i, StringComparison.Ordinal));
        var li = Toplu3Ortak.Blok(m, "#kur-adimlar .sade-adim-listesi > li");
        Assert.NotNull(li);
        Assert.Contains("\"ad ad eylem\" \"durum sayi eylem\"", li);
    }

    [Fact]
    public void T3_Arama_katmani_375_tam_ekran()
    {
        var m = Toplu3Ortak.Medya(Toplu3Ortak.Css(), "@media (max-width: 480px)");
        var b = Toplu3Ortak.Blok(m, "#kayit-arama .kayit-arama-palet");
        Assert.NotNull(b);
        Assert.Contains("width: 100vw", b);
        Assert.Contains("height: 100dvh", b);
        Assert.Contains("top: 0", b);
        Assert.Contains("border-radius: 0", b);
    }

    [Fact]
    public void T3_Hurda_daha_fazla_secim_kutusu_gibi_gorunmez()
    {
        var b = Toplu3Ortak.Blok(Toplu3Ortak.Css(), "#hrd-detay-ac, #hrd-detay-gizle");
        Assert.NotNull(b);
        Assert.Contains("border-style: dashed", b);
        Assert.Contains("color: var(--vurgu)", b);
    }

    [Fact]
    public void T3_Vitrin_cekmece_yardimci_yazi_en_az_14px()
    {
        var b = Toplu3Ortak.Blok(Toplu3Ortak.Css(), "#pano-varsayilana-don-kilit");
        Assert.NotNull(b);
        Assert.Contains("max(14px", b);
    }

    [Fact]
    public void T3_Uyum_pep_karti_iki_sutun_ve_razorda_isaretli()
    {
        Assert.Contains("grid-column: span 2", Toplu3Ortak.Blok(Toplu3Ortak.Css(), "#uy-risk-ozet > .stat-kart--genis"));
        var r = Toplu3Ortak.Dosya("Kuyumcu.Web", "Pages", "Uyum.razor");
        Assert.Contains("<StatKart Etiket=\"@RiskMetin.PepSayisi\" Deger=\"@ro.PepSayisi.ToString()\" Renk=\"stat-kart--genis\" />", r);
        Assert.Contains("<div class=\"stat-izgara\" id=\"uy-risk-ozet\">", r);
        // Yasal terim KISALTILMAZ.
        Assert.Equal("Siyasi nüfuz sahibi olduğunu beyan eden müşteri", OrtakGorunumSozlugu.Tr.UyumPepSayisiBasit);
    }

    [Fact]
    public void T3_Sozluk_sade_dil_TR_EN_dolu()
    {
        foreach (var m in new[] { Toplu3Sozlugu.Tr, Toplu3Sozlugu.En })
            foreach (var p in typeof(Toplu3Metinleri).GetProperties().Where(p => p.PropertyType == typeof(string)))
                Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(m)), p.Name);
        Assert.DoesNotContain("Deneme", Toplu3Sozlugu.Tr.KurUcretsizRozet, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("destek hattını arayın", Toplu3Sozlugu.Tr.PanelListeYok, StringComparison.Ordinal);
        // Fiş kimlik kutusu: eksik bilgiyi söyler, "uydurulmaz" teknik dili yok; düğme eylem söyler.
        Assert.DoesNotContain("uydurulmaz", Sozlukler.Tr.Cikti.KimlikEksikGerekce, StringComparison.Ordinal);
        Assert.Contains("adresi ya da vergi bilgisi eksik", Sozlukler.Tr.Cikti.KimlikEksikGerekce, StringComparison.Ordinal);
        Assert.Equal("İşletme bilgilerini girin", Sozlukler.Tr.SayfaDuzeltme.IsletmeKartinaGit);
    }

    [Theory]
    [InlineData(120, "GBP", true)]
    [InlineData(-50, "USD", false)]
    public void T3_Doviz_rozeti_yon_tasir(decimal tutar, string birim, bool bizeBorclu)
    {
        var m = Toplu3Sozlugu.Tr;
        var metin = Kuyumcu.Web.Pages.Raporlar.DovizYonMetni(new Dictionary<string, decimal> { [birim] = tutar }, m);
        var beklenen = string.Format(bizeBorclu ? m.YasDovizBizeBorclu : m.YasDovizBizBorcluyuz, Bicim.Para(Math.Abs(tutar), birim));
        Assert.Equal(beklenen, metin);
        Assert.DoesNotContain("-", metin, StringComparison.Ordinal);
    }
}

/// <summary>demo-tohumla.js — ilk kur beklemesi ve işletme kartı (gerçek node süreci, ağ yok).</summary>
public sealed class Toplu3DemoTohumTestleri
{
    private static (int Kod, string Cikti, string Hata) Node(string js)
    {
        var betik = Path.Combine(Directory.GetParent(Toplu3Ortak.Kok())!.FullName,
            "dokumanlar", "09-uygulama-kayitlari", "betikler", "demo-tohumla.js");
        var psi = new System.Diagnostics.ProcessStartInfo("node") { RedirectStandardOutput = true, RedirectStandardError = true };
        if (!File.Exists("/usr/bin/node") && File.Exists("/opt/homebrew/bin/node")) psi.FileName = "/opt/homebrew/bin/node";
        psi.ArgumentList.Add("-e"); psi.ArgumentList.Add(js); psi.ArgumentList.Add(betik);
        using var p = System.Diagnostics.Process.Start(psi)!;
        var c = p.StandardOutput.ReadToEnd(); var h = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, c, h);
    }

    [Fact]
    public void T3_Ilk_kur_en_az_150_sn_beklenir_404_ara_durumdur()
    {
        // Sahte uç: ilk 60 sn (30 yoklama × 2 sn) 404, sonra kur gelir — eski 40 sn'lik bekleme burada düşerdi.
        var js = "const m=require(process.argv[1]);let n=0;const getir=async()=>(++n<=30?{ok:false,status:404,text:async()=>''}:{ok:true,status:200,json:async()=>({id:'kur-1'})});"
               + "m.kurBekle(getir,{bekle:async()=>{}}).then(k=>process.stdout.write(JSON.stringify({id:k.id,n,azami:m.KUR_BEKLEME_AZAMI_SN})),e=>{process.stdout.write('HATA:'+e.message)});";
        var (kod, cikti, hata) = Node(js);
        Assert.True(kod == 0, hata);
        Assert.Equal("{\"id\":\"kur-1\",\"n\":31,\"azami\":150}", cikti);
    }

    [Fact]
    public void T3_Kur_404_disi_hata_yutulmaz_ve_sure_dolunca_durur()
    {
        var js = "const m=require(process.argv[1]);"
               + "m.kurBekle(async()=>({ok:false,status:500,text:async()=>'patladi'}),{bekle:async()=>{}}).then(()=>process.stdout.write('OK'),e=>process.stdout.write('A:'+e.message));"
               + "let n=0;m.kurBekle(async()=>{n++;return {ok:false,status:404,text:async()=>''}},{bekle:async()=>{}}).then(()=>process.stdout.write('OK'),e=>process.stdout.write('|B:'+n+':'+e.message.slice(0,20)));";
        var (kod, cikti, hata) = Node(js);
        Assert.True(kod == 0, hata);
        Assert.Contains("A:GET /kur/guncel → HTTP 500", cikti, StringComparison.Ordinal);
        Assert.Contains("|B:76:150 saniyede", cikti, StringComparison.Ordinal);
    }

    [Fact]
    public void T3_Isletme_karti_kurgusal_ve_plan_kapisi()
    {
        var js = "const m=require(process.argv[1]);const p=structuredClone(m.SENARYO);"
               + "const a=m.senaryoPlaniniDogrula(p);delete p.isletme;const b=m.senaryoPlaniniDogrula(p);"
               + "process.stdout.write(JSON.stringify({i:m.SENARYO.isletme,a:a.length,b:b.filter(x=>x.startsWith('ISLETME')).length}));";
        var (kod, cikti, hata) = Node(js);
        Assert.True(kod == 0, hata);
        var d = System.Text.Json.JsonDocument.Parse(cikti).RootElement;
        Assert.Equal(0, d.GetProperty("a").GetInt32());
        Assert.Equal(1, d.GetProperty("b").GetInt32());
        // Vergi no gerçek bir mükellefe ait OLAMAZ (kurgusal); adres ve vergi dairesi dolu.
        Assert.Equal("0000000000", d.GetProperty("i").GetProperty("vergiNo").GetString());
        Assert.False(string.IsNullOrWhiteSpace(d.GetProperty("i").GetProperty("adres").GetString()));
        var betik = File.ReadAllText(Path.Combine(Directory.GetParent(Toplu3Ortak.Kok())!.FullName,
            "dokumanlar", "09-uygulama-kayitlari", "betikler", "demo-tohumla.js"));
        Assert.Contains("istek('PATCH', '/admin/tenant/kimlik'", betik, StringComparison.Ordinal);
    }
}

/// <summary>Panel yasal liste bandı — uyarı GİZLENMEZ; dil sade, ayrıntı bağlantısı.</summary>
public sealed class Toplu3PanelBandiTestleri : BunitContext
{
    private string ozet = """{"saklamaYaklasan":0,"saklamaGun":30,"saklamaKuru":true,"listeTarihi":null,"listeEski":true,"listeYasiGun":null,"acikTam":0,"acikBenzerlik":0}""";

    public Toplu3PanelBandiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(() => ozet)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class Sahte(Func<string> o) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(istek.RequestUri!.AbsolutePath.EndsWith("/masak/uyum-ozet") ? o() : "{}",
                    System.Text.Encoding.UTF8, "application/json"),
            });
    }

    [Fact]
    public void T3_Liste_yuklenmemisse_uyari_SADE_dille_cikar_ve_ayrintiya_baglar()
    {
        var e = Render<Kuyumcu.Web.Bilesenler.Uyum.UyumYasalBantlari>();
        e.WaitForElement("#uyb-liste");
        var bant = e.Find("#uyb-liste");
        Assert.Contains("bant-uyari", bant.GetAttribute("class"), StringComparison.Ordinal);   // uyarı gizlenmedi
        // TOPLU-6 (YASAL-LISTE-DEMO hakemi ORTA "çıkmaz sokak"): numarası olmayan "destek hattı" yazılmaz.
        Assert.Equal(Toplu6Sozlugu.Tr.PanelListeYok, bant.QuerySelector("span")!.TextContent.Trim());
        Assert.DoesNotContain("destek hattı", bant.TextContent, StringComparison.Ordinal);
        Assert.Equal("/uyum?sekme=risk", e.Find("#uyb-liste-git").GetAttribute("href"));
        Assert.DoesNotContain("destekle görüşün", bant.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void T3_Liste_guncelse_bant_yok_kosul_degismedi()
    {
        ozet = ozet.Replace("\"listeEski\":true", "\"listeEski\":false");
        var e = Render<Kuyumcu.Web.Bilesenler.Uyum.UyumYasalBantlari>();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#uyb-liste")));
    }
}

/// <summary>Cari yaşlandırma tablosu görünümü (rapor hesabı aynı).</summary>
public sealed class Toplu3YaslandirmaEkranTestleri : BunitContext
{
    // a1: açılış 1.000 ₺ (tarihsiz) · b1: 0-30 100 ₺ · g1: yalnız GBP 100 · h1: has 2,5 (yalnız has testi için).
    private const string Yanit = """
        {"yaslananCariler":[
           {"cariId":"a1","cariAd":"Açılışlı Müşteri","cariTur":"MUSTERI","tlBakiye":1000.00,"hasGramBakiye":0,
            "dovizBakiyeleri":{},"dovizBakiyesiVar":false,"gunSayisi":null,"dilim":"BELIRSIZ","belirsizNedeni":"ACILIS","acilisDahil":true},
           {"cariId":"b1","cariAd":"Yeni Borçlu","cariTur":"MUSTERI","tlBakiye":100.00,"hasGramBakiye":0,
            "dovizBakiyeleri":{},"dovizBakiyesiVar":false,"gunSayisi":10,"dilim":"0-30","belirsizNedeni":null,"acilisDahil":false},
           {"cariId":"g1","cariAd":"Sterlin Müşterisi","cariTur":"MUSTERI","tlBakiye":0,"hasGramBakiye":0,
            "dovizBakiyeleri":{"GBP":100.00},"dovizBakiyesiVar":true,"gunSayisi":null,"dilim":"BELIRSIZ","belirsizNedeni":"ACILIS","acilisDahil":true}],
         "ozetTl":{"0-30":100.00,"31-60":0,"61-90":0,"90+":0,"BELIRSIZ":1000.00},
         "ozetDoviz":{"USD":0,"EUR":0,"GBP":100.00},
         "toplamSatir":3,"ofset":0,"limit":500,"dahaFazlasiVar":false}
        """;
    private string yanit = Yanit;

    public Toplu3YaslandirmaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(() => yanit)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class Sahte(Func<string> yaslandirma) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/odeme-araci") =>
                    """{"baslangic":"2026-09-01","bitis":"2026-09-30","kaynak":"KASA_HAREKETLERI","araclar":[],"veresiye":{"doganTl":0,"kapananTl":0},"bilinmeyen":{"nakitPayiBilinmeyenSatir":0,"nakitPayiBilinenSatir":0},"storno":{"adet":0,"tutarTl":0},"guvenilirlik":"VERI_YOK","satisKirilimi":{"kaynak":"ODEME_KALEMLERI","satirlar":[],"brutSatisTl":0,"kirilimToplamTl":0,"farkTl":0,"kasayaGirenTl":0,"mahsupAltinTl":0,"veresiyeTl":0,"fisAdet":0,"kalemsizFisAdet":0,"kalemsizTl":0}}""",
                var y when y.EndsWith("/raporlar/aylik") => """{"yil":2026,"ay":9,"gunler":[]}""",
                var y when y.EndsWith("/raporlar/cari-yaslandirma") => yaslandirma(),
                var y when y.EndsWith("/raporlar/stok-degeri") =>
                    """{"urunler":[],"toplamHas":0,"madenDagilimi":[],"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false,"uyari":null}""",
                var y when y.EndsWith("/raporlar/reyon-stok") => """{"reyonlar":[],"subeler":[]}""",
                var y when y.EndsWith("/raporlar/stok-yaslandirma") =>
                    """{"olculduAn":"2026-09-22T06:00:00Z","subeId":null,"kovalar":[],"madenDagilimi":[],"yasiBilinmeyenAdet":0,"toplamAdet":0,"devir":null,"oluStok":{"urunler":[],"dahaFazlasiVar":false},"guvenilirlik":"VERI_YOK","uyarilar":[]}""",
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") =>
                    """{"tarih":"2026-09-17","tahsilat":{"nakit":0,"kart":0,"havale":0,"diger":0,"toplam":0},"satis":{"sayisi":0,"toplamTl":0,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":0,"faturasizTl":0},"alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},"hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},"fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},"fark":0,"mutabik":true,"uyarilar":[],"faturasizIslemler":[],"faturaEslesme":null}""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Raporlar> Ac()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yas-tablo")));
        return e;
    }

    [Fact]
    public void T3_Sifir_hucre_yas_sifir_sinifi_dolu_hucre_degil_metin_korunur()
    {
        var e = Ac();
        var sifir = e.Find("#yas-satir-b1 td.yas-31-60");
        Assert.Contains("yas-sifir", sifir.ClassName, StringComparison.Ordinal);
        Assert.Equal(Bicim.TlKurus(0m), sifir.QuerySelector(".yas-deger")!.TextContent.Trim());   // ekran okuyucu "0,00 ₺"
        Assert.Equal(Kuyumcu.Core.I18n.Sozlukler.Tr.Rapor.Gun_31_60, sifir.GetAttribute("data-etiket"));
        var dolu = e.Find("#yas-satir-b1 td.yas-0-30");
        Assert.DoesNotContain("yas-sifir", dolu.ClassName, StringComparison.Ordinal);
        Assert.Contains("yas-sifir", e.Find("#yas-satir-g1 td.yas-toplam-tl").ClassName, StringComparison.Ordinal);
        Assert.Contains("yas-sifir", e.Find("#yas-satir-b1 td.yas-has").ClassName, StringComparison.Ordinal);
        Assert.True(Kuyumcu.Web.Pages.Raporlar.YasSifirMi(0m));
        Assert.False(Kuyumcu.Web.Pages.Raporlar.YasSifirMi(0.01m));
    }

    [Fact]
    public void T3_Tarihsiz_aciklamasi_baslikta_dokunmatik_i_tablo_altinda_paragraf_yok()
    {
        var e = Ac();
        var ipucu = e.Find("#yas-belirsiz-ipucu");
        Assert.Equal("DETAILS", ipucu.TagName);
        Assert.NotNull(ipucu.Closest("th"));
        Assert.NotNull(ipucu.QuerySelector("summary"));
        Assert.Equal(RaporYaslandirmaSozlugu.Tr.BelirsizNotu, ipucu.QuerySelector("#yas-belirsiz-notu")!.TextContent.Trim());
        // Kartın doğrudan çocuğu paragraf olarak not kalmadı.
        Assert.Empty(e.FindAll("#yas-kart > p#yas-belirsiz-notu"));
        Assert.Empty(e.FindAll("#yas-kart > p#yas-doviz-ozet"));
    }

    [Fact]
    public void T3_Doviz_ozeti_tfoot_satiri_yonlu_rozet_ve_kolon_kapsami()
    {
        var e = Ac();
        var satir = e.Find("tfoot #yas-doviz-ozet");
        Assert.NotNull(satir.QuerySelector("details#yas-doviz-ipucu summary"));
        var hucre = satir.QuerySelector("td.yas-doviz-ozet-hucre")!;
        Assert.Equal("7", hucre.GetAttribute("colspan"));    // 8 kolon (tarihsiz var) − 1
        Assert.Equal(string.Format(Toplu3Sozlugu.Tr.YasDovizBizeBorclu, Bicim.Para(100m, "GBP")), hucre.TextContent.Trim());
    }

    [Fact]
    public void T3_Has_bakiyesi_yoksa_tablo_has_bos_sinifi_varsa_yok()
    {
        var e = Ac();
        Assert.Contains("yas-has-bos", e.Find("#yas-tablo").ClassName, StringComparison.Ordinal);
        yanit = Yanit.Replace("\"cariId\":\"b1\",\"cariAd\":\"Yeni Borçlu\",\"cariTur\":\"MUSTERI\",\"tlBakiye\":100.00,\"hasGramBakiye\":0",
                              "\"cariId\":\"b1\",\"cariAd\":\"Yeni Borçlu\",\"cariTur\":\"MUSTERI\",\"tlBakiye\":100.00,\"hasGramBakiye\":2.5");
        Assert.NotEqual(Yanit, yanit);
        var e2 = Ac();
        Assert.DoesNotContain("yas-has-bos", e2.Find("#yas-tablo").ClassName ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void T3b_Hakem_fikesturu_has_dolu_8_kolon_has_degerleri_cizilir()
    {
        // Tasarım hakemi (tur 1 KRİTİK) fikstürü: açılış devri −125,5 g has ve 12,25 g has.
        yanit = Yanit
            .Replace("\"cariId\":\"a1\",\"cariAd\":\"Açılışlı Müşteri\",\"cariTur\":\"MUSTERI\",\"tlBakiye\":1000.00,\"hasGramBakiye\":0",
                     "\"cariId\":\"a1\",\"cariAd\":\"Açılışlı Müşteri\",\"cariTur\":\"MUSTERI\",\"tlBakiye\":1000.00,\"hasGramBakiye\":12.25")
            .Replace("\"cariId\":\"g1\",\"cariAd\":\"Sterlin Müşterisi\",\"cariTur\":\"MUSTERI\",\"tlBakiye\":0,\"hasGramBakiye\":0",
                     "\"cariId\":\"g1\",\"cariAd\":\"Sterlin Müşterisi\",\"cariTur\":\"MUSTERI\",\"tlBakiye\":0,\"hasGramBakiye\":-125.5");
        Assert.Equal(2, yanit.Split("hasGramBakiye\":", StringSplitOptions.None).Count(p => p.StartsWith("12.25") || p.StartsWith("-125.5")));
        var e = Ac();
        var tablo = e.Find("#yas-tablo");
        Assert.DoesNotContain("yas-has-bos", tablo.ClassName ?? "", StringComparison.Ordinal);
        Assert.Equal(8, e.FindAll("#yas-tablo thead th").Count);            // ad + 4 dilim + tarihsiz + toplam + has
        Assert.Equal(Kuyumcu.Core.I18n.Sozlukler.Tr.Rapor.YasHasBakiye, e.Find("#yas-has-baslik").TextContent.Trim());
        Assert.Equal(Bicim.Has(12.25m), e.Find("#yas-satir-a1 td.yas-has .yas-deger").TextContent.Trim());
        Assert.Equal(Bicim.Has(-125.5m), e.Find("#yas-satir-g1 td.yas-has .yas-deger").TextContent.Trim());
        Assert.Contains("metin-kirmizi", e.Find("#yas-satir-g1 td.yas-has").ClassName, StringComparison.Ordinal);
        Assert.Equal(Bicim.Has(-113.25m), e.Find("#yas-toplam-has .yas-deger").TextContent.Trim());
        // Has notu (sütunu anlatan) dolu tabloda gizlenmez: kartta durur.
        // TOPLU-4: not ⓘ içine taşındı (kuyruk TOPLU-3 t2 ORTA); satır kartın doğrudan çocuğu.
        Assert.NotNull(e.Find("#yas-kart > #yas-has-not-satir #yas-has-notu"));
    }
}

/// <summary>Kur ekranı rozeti — koşul aynı, dil sade.</summary>
public sealed class Toplu3KurRozetTestleri
{
    [Fact]
    public void T3_Kur_razor_rozet_ve_ipucu_Toplu3ten()
    {
        var r = Toplu3Ortak.Dosya("Kuyumcu.Web", "Pages", "Kur.razor");
        Assert.Contains("<Rozet Renk=\"amber\">@Toplu3Sozlugu.Al(Dil.Dil).KurUcretsizRozet</Rozet>", r);
        Assert.DoesNotContain("KurDenemeRozet", r, StringComparison.Ordinal);
        // Koşul DEĞİŞMEDİ: rozet yalnız seçili kaynak sözleşmesizken.
        Assert.Contains("else if (kd.SeciliSozlesmesiz)", r);
        Assert.Equal("Ücretsiz kur kaynağı", Toplu3Sozlugu.Tr.KurUcretsizRozet);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// TUR 2 — tasarım hakemi KRİTİK'leri: (1) yaşlandırma 1280'de has DOLUYKEN son sütun
// kırpılıyordu; (2) /kurulum açılışta isteğe bağlı formlar açık, iki birincil düğme, jargon.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Tur 2 CSS kuralları (yaşlandırma dar masaüstü sığdırma · kurulum akordeon).</summary>
public sealed class Toplu3Tur2CssTestleri
{
    [Fact]
    public void T3b_Yaslandirma_641_1439_arasi_dar_dolgu_basliklar_tek_satir_has_doluyken_de()
    {
        var css = Toplu3Ortak.Css();
        var m = Toplu3Ortak.Medya(css, "@media (min-width: 641px) and (max-width: 1439px)");
        Assert.Contains("padding-inline: 10px", Toplu3Ortak.Blok(m, "#yas-tablo :is(th, td).sayi") ?? "");
        // Başlıklar sözcük arasından da SARMAZ (ölçer K3 "kırık kısa etiket" 0): taban kural nowrap,
        // dar masaüstü bloğu bunu "normal"e çevirmez.
        Assert.Contains("white-space: nowrap", Toplu3Ortak.Blok(css, "#yas-tablo :is(th, td).sayi") ?? "");
        Assert.DoesNotContain("white-space: normal", m, StringComparison.Ordinal);
        Assert.Contains("min-width: 8rem", Toplu3Ortak.Blok(m, "#yas-tablo td.yas-ad") ?? "");
        // Kural has-bos sınıfına BAĞLI DEĞİL (has dolu tabloda da çalışır).
        Assert.DoesNotContain("yas-has-bos", m, StringComparison.Ordinal);
    }

    [Fact]
    public void T3b_Kurulum_bolum_akordeon_ve_kart_araligi()
    {
        var css = Toplu3Ortak.Css();
        Assert.Contains("margin-top: var(--bosluk-4)", Toplu3Ortak.Blok(css, ".sayfa > #kur-adimlar ~ .kart") ?? "");
        var baslik = Toplu3Ortak.Blok(css, ".kur-bolum > summary.kur-bolum-baslik") ?? "";
        Assert.Contains("list-style: none", baslik);
        Assert.Contains("min-height: 48px", baslik);
        var govde = Toplu3Ortak.Blok(css, ".kur-bolum-govde") ?? "";
        Assert.Contains("flex-direction: column", govde);
        Assert.Contains("gap: var(--bosluk-3)", govde);   // "Kayıt"/"Gerekçe" etiketi üstteki öğeye yapışmaz
        Assert.Contains("rotate(-135deg)", Toplu3Ortak.Blok(css, ".kur-bolum[open] > summary.kur-bolum-baslik::after") ?? "");
    }
}

/// <summary>Yaşlandırma — has bakiyesi DOLU fikstür: sütun her genişlikte çizilir.</summary>
public sealed class Toplu3Tur2YaslandirmaTestleri
{
    [Fact]
    public void T3b_Has_dolu_tabloda_has_sutunu_ve_degeri_cizilir_gizleme_sinifi_yok()
    {
        var r = Toplu3Ortak.Dosya("Kuyumcu.Web", "Pages", "Raporlar.razor");
        // Has sütunu başlığı ve hücresi KOŞULSUZ çizilir; gizleme yalnız YasHasVar=false iken sınıfla.
        Assert.Contains("<table class=\"veri-tablosu @(YasHasVar ? null : \"yas-has-bos\")\" id=\"yas-tablo\">", r);
        Assert.Contains("id=\"yas-has-baslik\"", r);
        Assert.Contains("private bool YasHasVar => GorunenYaslananlar.Any(y => y.HasGramBakiye != 0m);", r);
    }
}

/// <summary>/kurulum açılış görünümü (tasarım hakemi KRİTİK — TOPLU-2 YARIM).</summary>
public sealed class Toplu3KurulumEkranTestleri : BunitContext
{
    private string durumJson;
    private const string ProvaTemiz = """
        {"tur":"KASA","temiz":true,"provaJetonu":"jeton-1","toplamSatir":1,"hataliSatir":0,
         "uygulanacakSatir":1,"uyariliSatir":0,"limitAsimiOnayiGerekli":false,
         "tlToplam":1000,"hasToplam":0,"kisiselVeriIzi":false,"hatalar":[],"uyarilar":[]}
        """;

    private static string Durum(bool muhurlenebilir) => """
        {"muhurlendi":false,"muhurTarihi":null,"muhurleyenKullaniciId":null,"muhurGerekcesi":null,
         "acilisPenceresiBitis":"2026-10-17T00:00:00Z","acilisPenceresiAcik":true,
         "muhurlenebilir":MUHUR,"eksikAdimlar":[],"atlananAdimlar":[],
         "adimlar":[
           {"anahtar":"ISLETME","bitti":true,"zorunlu":true,"atlandi":false},
           {"anahtar":"KUR","bitti":true,"zorunlu":true,"atlandi":false},
           {"anahtar":"CARI_ACILIS","bitti":false,"zorunlu":false,"atlandi":false},
           {"anahtar":"STOK_ACILIS","bitti":false,"zorunlu":false,"atlandi":false}],
         "sayilar":{"sube":1,"kullanici":1,"kur":1,"kasa":1,"banka":0,
                    "acilisCari":0,"acilisKasa":0,"acilisBanka":0},
         "maksSatir":500,"gerekceAsgari":20,"isletmeAdi":"Altın Işık Kuyumcu"}
        """.Replace("MUHUR", muhurlenebilir ? "true" : "false");

    public Toplu3KurulumEkranTestleri()
    {
        durumJson = Durum(false);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class Sahte(Toplu3KurulumEkranTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/kurulum/durum") => t.durumJson,
                var y when y.EndsWith("/kurulum/acilis/prova") => ProvaTemiz,
                var y when y.EndsWith("/kasalar") => """[{"id":"k1","ad":"Merkez Kasa","aktif":true}]""",
                var y when y.EndsWith("/bankalar") => """{"hesaplar":[]}""",
                var y when y.Contains("/cari") => "[]",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6827.2}""",
                var y when y.Contains("/yetkiler/benim") => """{"moduller":["kurulum"]}""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Kurulum> Ciz(string mod = ArayuzModuServisi.Basit)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Kurulum>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kur-adimlar")));
        return e;
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T3b_Acilista_acilis_ve_muhur_bolumu_KAPALI_ozet_satiri_gorunur(string mod)
    {
        var e = Ciz(mod);
        var acilis = e.Find("#kur-acilis > details#kur-acilis-bolum");
        Assert.False(acilis.HasAttribute("open"));
        Assert.Equal(Toplu3Sozlugu.Tr.KurulumAcilisOzet, e.Find("#kur-acilis-bolum > summary #kur-acilis-ozet").TextContent.Trim());
        var muhur = e.Find("#kur-muhur > details#kur-muhur-bolum");
        Assert.False(muhur.HasAttribute("open"));
        Assert.Equal(Toplu3Sozlugu.Tr.KurulumMuhurOzet, e.Find("#kur-muhur-bolum > summary #kur-muhur-ozet").TextContent.Trim());
        // Mevcut id'ler ve form DOM'da (davranış aynı): yalnız görünüm kapalı.
        Assert.NotNull(e.Find("#kur-acilis-bolum #kur-prova"));
        Assert.NotNull(e.Find("#kur-muhur-bolum #kur-muhurle"));
        // Mühürlenemezken mühür düğmesi birincil DEĞİL → ekranda tek birincil.
        Assert.Contains("btn-ikincil", e.Find("#kur-muhurle").ClassName, StringComparison.Ordinal);
        Assert.Contains("btn-birincil", e.Find("#kur-prova").ClassName, StringComparison.Ordinal);
        Assert.Single(e.FindAll(".btn-birincil"));   // kapalı bölüm içindekiler dahil DOM'da tek birincil
    }

    [Fact]
    public void T3b_Muhurlenebilirken_muhur_KAPALI_ama_birincil_satir_eklenince_acilis_acik_muhur_ikincil()
    {
        durumJson = Durum(true);
        var e = Ciz();
        Assert.False(e.Find("#kur-muhur-bolum").HasAttribute("open"));
        Assert.Contains("btn-birincil", e.Find("#kur-muhurle").ClassName, StringComparison.Ordinal);
        Assert.False(e.Find("#kur-acilis-bolum").HasAttribute("open"));
        Assert.Contains("btn-ikincil", e.Find("#kur-prova").ClassName, StringComparison.Ordinal);
        Assert.Single(e.FindAll(".btn-birincil"));

        e.Find("#kur-satir-ekle").Click();
        e.WaitForAssertion(() => Assert.True(e.Find("#kur-acilis-bolum").HasAttribute("open")));
        Assert.Contains("btn-ikincil", e.Find("#kur-muhurle").ClassName, StringComparison.Ordinal);
        Assert.Single(e.FindAll(".btn-birincil"));   // kur-prova
    }

    [Fact]
    public void T3b_Kontrol_sonrasi_tek_birincil_Uygula_jargon_yok()
    {
        var e = Ciz();
        e.Find("#kur-satir-ekle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kur-acilis-tablo")));
        e.Find("#kur-gerekce").Input("Eski programdan devreden acilis bakiyesi");
        Assert.Equal(Toplu3Sozlugu.Tr.KurulumKontrolEt, e.Find("#kur-prova").TextContent.Trim());
        e.Find("#kur-prova").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kur-prova-sonuc")));
        Assert.Contains("btn-ikincil", e.Find("#kur-prova").ClassName, StringComparison.Ordinal);
        Assert.Contains("btn-birincil", e.Find("#kur-uygula").ClassName, StringComparison.Ordinal);
        Assert.Single(e.FindAll(".btn-birincil"));
        Assert.Equal(Toplu3Sozlugu.Tr.KurulumKontrolSonucu, e.Find("#kur-prova-sonuc > .kart-baslik").TextContent.Trim());
        Assert.DoesNotContain("KURU PROVA", e.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Kuru prova", e.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void T3b_Stok_adimi_Yakinda_sayisiz_ve_aciklamalar_i_icinde()
    {
        var e = Ciz();
        var stok = e.Find("#kur-adim-STOK_ACILIS");
        Assert.Contains(Toplu3Sozlugu.Tr.KurulumYakinda, stok.QuerySelector(".rozet")!.TextContent);
        Assert.Contains(Toplu3Sozlugu.Tr.KurulumStokAdi, stok.TextContent);
        Assert.DoesNotContain("bu sürümde yok", e.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Null(stok.QuerySelector(".metin-kucuk"));
        // Bitmemiş zorunsuz başka adım "Eksik" yazmaya devam eder (durum uydurulmaz).
        Assert.Contains(Sozlukler.Tr.Kurulum.AdimEksik, e.Find("#kur-adim-CARI_ACILIS .rozet").TextContent);
        // Açıklamalar dokunmatikte açılan (i) içinde, kartın düz paragrafı değil.
        Assert.Equal("DETAILS", e.Find("#kur-acilis-ipucu").TagName);
        Assert.Equal(Toplu3Sozlugu.Tr.KurulumAcilisIpucu, e.Find("#kur-acilis-ipucu > #kur-acilis-aciklama").TextContent.Trim());
        Assert.Equal("DETAILS", e.Find("#kur-muhur-ipucu").TagName);
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#kur-muhur-ipucu > #kur-muhur-aciklama").TextContent));
    }

    [Fact]
    public void T3b_Sozluk_kurulum_TR_EN_dolu_jargonsuz()
    {
        foreach (var m in new[] { Toplu3Sozlugu.Tr, Toplu3Sozlugu.En })
            foreach (var d in new[] { m.KurulumStokAdi, m.KurulumYakinda, m.KurulumAcilisOzet, m.KurulumAcilisIpucu,
                                      m.KurulumKontrolEt, m.KurulumKontrolSonucu, m.KurulumMuhurOzet })
                Assert.False(string.IsNullOrWhiteSpace(d));
        Assert.DoesNotContain("prova", Toplu3Sozlugu.Tr.KurulumAcilisIpucu, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Kontrol et", Toplu3Sozlugu.Tr.KurulumKontrolEt);
    }
}
