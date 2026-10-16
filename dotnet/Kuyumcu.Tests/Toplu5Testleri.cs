using System.Net;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Contracts.Cikti;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Yazdirma;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET TOPLU-5 — para ekranları görünüm kuyruğu (Cari/ekstre/Çıktı, Gider/Kasalar/
// Virman, İşlemler, Döviz Veresiye, gün sonu biçimi). YALNIZ GÖRÜNÜM (hasımsız paket):
// her kabul maddesi burada bir testle sabitlenir ve mutasyonla kırmızıya döndüğü gösterilir.
//   bUnit : Çıktı değişmezlik ⓘ · belge telefonu · sıfır has satırı · Cariler üst bar tekrarı /
//           sınır notu / döviz sütunu / döviz çekmecesi (ⓘ, kalıp, etiket, boş tutar) · Gider
//           seçicide nakit / bant / boş listede değişmez kutusu yok / virman BASİT dili · gün sonu
//           kırılımı (sıfır kalem yok) / tekrar tutar yok / PRO durum ve formül ⓘ · İşlemler
//           BASİT kimlik başlığı.
//   kaynak: demo-tohumla işletme kimliği doğal · toplu5.css sırası, belirteç, kurallar.
// ═════════════════════════════════════════════════════════════════════════════

internal static class Toplu5Ortak
{
    public static string Dosya(params string[] parca) => Toplu3Ortak.Dosya(parca);

    /// <summary>toplu5.css (yorumsuz).</summary>
    public static string Css() => Regex.Replace(
        Dosya("Kuyumcu.Web", "wwwroot", "css", "toplu5.css"), @"/\*.*?\*/", "", RegexOptions.Singleline);

    public static string? Blok(string css, string secici) => Toplu3Ortak.Blok(css, secici);

    public static HttpResponseMessage Yanit(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
    };

    public static void Oturum(BunitContext c, string mod)
    {
        c.Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        // Kabukta (AnaYerlesim) olduğu gibi: sözlüğün BASİT yüzü moda bağlanır (Dil.BasitMi).
        c.Services.GetRequiredService<DilServisi>().BasitModuBagla(() => mod == ArayuzModuServisi.Basit);
    }
}

// ═══ (1) /cikti — değişmezlik notu ⓘ · telefon biçimi · sıfır has satırı ═══
public sealed class Toplu5CiktiTestleri : BunitContext
{
    public Toplu5CiktiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte()) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Sahte : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal) =>
            Task.FromResult(Toplu5Ortak.Yanit("""
            {"tur":"CARI_EKSTRE","belgeAdi":"CARİ HESAP EKSTRESİ","belgeNo":"01M3VV6RZST5TXEW9T1YJEPP4P","dayanak":null,
             "duzenlemeTarihi":"2026-10-02T08:00:00Z",
             "duzenleyen":{"unvan":"Test Kuyumcu","sube":"Merkez","adres":"Gümüşhane Sokak No: 12","vergiDairesi":"Merkez","vergiNo":"0000000000","telefon":"05320000100"},
             "karsi":{"baslik":"Müşteri","ad":"Kemal Ardıç","vergiNo":null,"adres":null,"telefon":"05320000104"},
             "bolumler":[{"baslik":"Hareketler","sutunBasliklari":["Tarih","İşlem","Has etkisi","Tutar"],"sutunBicimleri":["METIN","METIN","HAS","PARA"],
               "satirlar":[{"hucreler":["02.10.2026","Satış","","1000.00"],"vurgu":false,"paraBirimi":"TRY"},
                           {"hucreler":["02.10.2026","Tahsilat"," ","-500.00"],"vurgu":false,"paraBirimi":"TRY"}],"not":null}],
             "tutarlar":[{"etiket":"TL bakiye","deger":10404.74,"paraBirimi":"TRY","toplam":true},
                         {"etiket":"Has gram bakiye","deger":0.000000,"paraBirimi":"HAS_GRAM","toplam":false}],
             "imzaBasliklari":["Düzenleyen"],"not":null,"uyari":null,"belgeNoKayitKimligi":true}
            """));
    }

    private IRenderedComponent<Cikti> Ekran()
    {
        var e = Render<Cikti>(p => p.Add(x => x.Tur, BelgeTurleri.CariEkstre).Add(x => x.Id, "c1"));
        e.WaitForElement("#cikti-belge");
        return e;
    }

    [Fact]
    public void T5_Cikti_degismez_notu_TEK_satir_gerekce_ve_duzeltme_yolu_ipucunda()
    {
        var e = Ekran();
        var t5 = Toplu5Sozlugu.Tr;
        var satir = e.Find("#cikti-degismez-satir");
        Assert.Contains("t4-degismez-satir", satir.ClassName ?? "", StringComparison.Ordinal);
        Assert.Equal(t5.CiktiDegismezKisa, satir.QuerySelector("#degismez-ipucu-CiktiBelgesi .degismez-gerekce")!.TextContent.Trim());
        Assert.Null(satir.QuerySelector(".degismez-alternatif"));   // yol kutuda değil, ⓘ içinde
        var ipucu = e.Find("#cikti-degismez-satir > details#cikti-degismez-ipucu");
        Assert.Equal(t5.CiktiDegismezBilgi, ipucu.QuerySelector("summary")!.GetAttribute("aria-label"));
        var aciklama = e.Find("#cikti-degismez-aciklama").TextContent;
        Assert.Contains(Sozlukler.Tr.Cikti.TuretilmisGerekce, aciklama, StringComparison.Ordinal);
        Assert.Contains(Sozlukler.Tr.Cikti.KaynaktaDuzelt, aciklama, StringComparison.Ordinal);
    }

    [Fact]
    public void T5_Belgede_telefonlar_ekrandaki_TEK_bicimle()
    {
        var e = Ekran();
        Assert.Equal("0532 000 01 04", e.Find("#cikti-karsi-telefon").TextContent.Trim());
        Assert.Equal("0532 000 01 00", e.Find("#cikti-duzenleyen-telefon").TextContent.Trim());
        Assert.DoesNotContain("05320000104", e.Find("#cikti-belge").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void T5_Sifir_has_bakiye_satiri_basilmaz_TL_toplami_basilir()
    {
        var e = Ekran();
        var belge = e.Find("#cikti-belge").TextContent;
        Assert.DoesNotContain("Has gram bakiye", belge, StringComparison.Ordinal);
        Assert.DoesNotContain(Bicim.Has(0m), belge, StringComparison.Ordinal);
        Assert.Contains("TL bakiye", belge, StringComparison.Ordinal);
        Assert.Contains(Bicim.BelgePara(10404.74m, "TRY"), belge, StringComparison.Ordinal);
    }

    [Fact]
    public void T5_Hicbir_satirinda_degeri_olmayan_sutun_cizilmez_dolu_sutunlar_ayni_dizinle()
    {
        var e = Ekran();
        var basliklar = e.FindAll("#cikti-belge .belge-tablo thead th").Select(x => x.TextContent.Trim()).ToList();
        Assert.Equal(["Tarih", "İşlem", "Tutar"], basliklar);
        var td = e.FindAll("#cikti-belge .belge-tablo tbody tr")[0].QuerySelectorAll("td");
        Assert.Equal(3, td.Length);
        Assert.Equal("Tutar", td[2].GetAttribute("data-etiket"));
        Assert.Equal(Bicim.BelgePara(1000m, "TRY"), td[2].TextContent.Trim());   // PARA biçimi aynı dizinden
    }

    [Fact]
    public void T5_BosSutunlar_saf_kural()
    {
        var b = new CiktiBolum("H", ["A", "B", "C"], [new CiktiSatir(["1", "", "x"], false, "TRY"), new CiktiSatir(["2", " ", ""], false, "TRY")], null, null);
        Assert.Equal([1], A4Belge.BosSutunlar(b).ToArray());
        Assert.Empty(A4Belge.BosSutunlar(new CiktiBolum("H", ["A"], [], null, null)));
    }

    [Theory]
    [InlineData("HAS_GRAM", 0, false, false)]   // sıfır has bakiye → basılmaz
    [InlineData("HAS_GRAM", 2.5, false, true)]  // has bakiye var → basılır
    [InlineData("HAS_GRAM", 0, true, true)]     // toplam satırı her zaman basılır
    [InlineData("TRY", 0, false, true)]         // TL satırı sıfır da olsa basılır (davranış AYNEN)
    public void T5_Basilir_yalniz_sifir_has_ara_satirini_eler(string birim, double deger, bool toplam, bool beklenen) =>
        Assert.Equal(beklenen, A4Belge.Basilir(new CiktiTutar("x", (decimal)deger, birim, toplam)));
}

// ═══ (2) demo-tohumla — fiş işletme kimliği doğal (filigran yok, vergi no kurgusal olduğu belli) ═══
public sealed class Toplu5DemoTohumTestleri
{
    [Fact]
    public void T5_Isletme_kimligi_dogal_adres_kurgusal_vergi_no_yer_tutucu_yok()
    {
        var betik = Path.Combine(Directory.GetParent(Toplu3Ortak.Kok())!.FullName,
            "dokumanlar", "09-uygulama-kayitlari", "betikler", "demo-tohumla.js");
        var psi = new System.Diagnostics.ProcessStartInfo("node") { RedirectStandardOutput = true, RedirectStandardError = true };
        if (!File.Exists("/usr/bin/node") && File.Exists("/opt/homebrew/bin/node")) psi.FileName = "/opt/homebrew/bin/node";
        psi.ArgumentList.Add("-e");
        psi.ArgumentList.Add("process.stdout.write(JSON.stringify(require(process.argv[1]).SENARYO.isletme))");
        psi.ArgumentList.Add(betik);
        using var p = System.Diagnostics.Process.Start(psi)!;
        var cikti = p.StandardOutput.ReadToEnd(); var hata = p.StandardError.ReadToEnd();
        p.WaitForExit();
        Assert.True(p.ExitCode == 0, hata);
        var i = System.Text.Json.JsonDocument.Parse(cikti).RootElement;
        var adres = i.GetProperty("adres").GetString()!;
        var vd = i.GetProperty("vergiDairesi").GetString()!;
        foreach (var yasak in new[] { "örnek", "Örnek", "DEMO", "(", "test" })
        {
            Assert.DoesNotContain(yasak, adres, StringComparison.Ordinal);
            Assert.DoesNotContain(yasak, vd, StringComparison.Ordinal);
        }
        Assert.Matches(@"No: \d+", adres);                  // kapı numarası olan doğal adres
        Assert.Contains("/", adres, StringComparison.Ordinal); // ilçe / il
        Assert.Equal("0000000000", i.GetProperty("vergiNo").GetString());   // kurgusal olduğu BELLİ
    }
}

// ═══ (3) /cariler — üst bar tekrarı · sınır notu · döviz sütunu · döviz çekmecesi ═══
public sealed class Toplu5CariTestleri : BunitContext
{
    private bool dovizli;

    public Toplu5CariTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Sahte(Toplu5CariTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var y = istek.RequestUri!.AbsolutePath;
            var usd = t.dovizli ? "120.5" : "0";
            var g = y switch
            {
                _ when y.EndsWith("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"gbpTry":null}""",
                _ when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},{"id":"ks-usd","ad":"USD Kasa","paraBirimi":"USD","aktif":true}]""",
                _ when y.EndsWith("/cari") => $$"""
                    [{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567","email":null,
                      "tlBakiye":-1500.00,"hasGramBakiye":2.5,"usdBakiye":{{usd}},"eurBakiye":0,"gbpBakiye":0,
                      "riskSeviye":null,"pepBeyani":false,"aktif":true}]
                    """,
                _ when y.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","aktif":true}]""",
                _ => "{}",
            };
            return Task.FromResult(Toplu5Ortak.Yanit(g));
        }
    }

    private IRenderedComponent<Cariler> Ciz(string mod)
    {
        Toplu5Ortak.Oturum(this, mod);
        var e = Render<Cariler>();
        e.WaitForState(() => e.FindAll("#c-doviz-veresiye-c1").Count > 0);
        return e;
    }

    private static void Ac(IRenderedComponent<Cariler> e, string id) => e.FindAll($"#{id}")[0].Click();

    [Theory]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    [InlineData(ArayuzModuServisi.Basit)]
    public void T5_Serit_basligi_ust_bar_tekrari_olarak_gorsel_gizli_DOMda_kalir(string mod)
    {
        var e = Ciz(mod);
        var serit = e.Find("#sayfa-basi-cariler");
        Assert.Contains("sayfa-basi--baslik-ustte", serit.ClassName ?? "", StringComparison.Ordinal);
        Assert.Equal(Services.GetRequiredService<DilServisi>().T.Cari.Baslik, e.Find("h2#sayfa-basi-cariler-baslik").TextContent.Trim());
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    [InlineData(ArayuzModuServisi.Basit)]
    public void T5_Sinir_notu_sinirin_altinda_YOK(string mod)
    {
        var e = Ciz(mod);
        Assert.Empty(e.FindAll("#cari-sinir"));
        Assert.Empty(e.FindAll("#cari-sinir-asildi"));
    }

    [Fact]
    public void T5_PRO_durum_sutunu_yalniz_pasifler_gosterilirken()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        var durum = Sozlukler.Tr.Urun.Durum;
        Assert.DoesNotContain(e.FindAll("table thead th"), th => th.TextContent.Trim() == durum);
        Assert.Contains(e.FindAll("table thead th"), th => th.TextContent.Trim() == Sozlukler.Tr.Cari.Tur);   // Tür sütunu AYNEN
        e.Find("#cari-pasifleri-goster").Change(true);
        e.WaitForAssertion(() => Assert.Contains(e.FindAll("table thead th"), th => th.TextContent.Trim() == durum));
    }

    [Fact]
    public void T5_PRO_doviz_sutunu_yalniz_doviz_bakiyesi_varken()
    {
        var bos = Ciz(ArayuzModuServisi.Profesyonel);
        Assert.DoesNotContain(bos.FindAll("table thead th"), th => th.TextContent.Trim() == Sozlukler.Tr.Cari.DovizBakiye);
        bos.Dispose();
        dovizli = true;
        var dolu = Render<Cariler>();
        dolu.WaitForState(() => dolu.FindAll("#c-doviz-veresiye-c1").Count > 0);
        Assert.Contains(dolu.FindAll("table thead th"), th => th.TextContent.Trim() == Sozlukler.Tr.Cari.DovizBakiye);
    }

    [Fact]
    public void T5_PRO_doviz_veresiye_uc_aciklama_TEK_ipucunda_etiket_sade()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        Ac(e, "c-doviz-veresiye-c1");
        var t5 = Toplu5Sozlugu.Tr;
        var bilgi = e.Find("#cekmece-doviz-veresiye details#dv-bilgi");
        Assert.Equal(t5.DovizBilgi, bilgi.QuerySelector("summary")!.GetAttribute("aria-label"));
        Assert.NotNull(bilgi.QuerySelector("#dv-fis-bacagi-not"));
        Assert.NotNull(bilgi.QuerySelector("#dv-vadesiz-not"));
        Assert.Contains(Sozlukler.Tr.Cari.VeresiyeIpucu, bilgi.TextContent, StringComparison.Ordinal);
        // Formun gövdesinde açık 12 px paragraf kalmadı.
        Assert.Empty(e.FindAll("#cekmece-doviz-veresiye .form-dikey > p.metin-kucuk"));
        var etiket = e.Find("#dv-tl-karsilik-sar .alan-etiket-metin").TextContent.Trim();
        Assert.Equal(t5.DovizTlKarsiligi, etiket);
        Assert.DoesNotContain("sunucu", e.Find("#cekmece-doviz-veresiye").TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void T5_BASIT_doviz_veresiyede_ipucu_de_YOK_zorunlu_once()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        Ac(e, "c-doviz-veresiye-c1");
        Assert.Empty(e.FindAll("#dv-bilgi"));
        Assert.Empty(e.FindAll("#dv-fis-bacagi-not"));
    }

    [Theory]
    [InlineData("c-doviz-veresiye-c1", "#cekmece-doviz-veresiye")]
    [InlineData("c-doviz-tahsilat-c1", "#cekmece-doviz-tahsilat")]
    public void T5_Doviz_cekmeceleri_musteri_cekmecesiyle_AYNI_kalip_sarmalayicisinda(string acici, string cekmece)
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        Ac(e, acici);
        var c = e.Find(cekmece);
        var kap = c.Closest(".t5-cekmece-kap");
        Assert.NotNull(kap);
        Assert.Contains("musteri-panel-kap", kap!.ClassName ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void T5_Bos_tutar_alan_altinda_Turkce_mesaj_dokunmadan_once_YOK()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        Ac(e, "c-doviz-veresiye-c1");
        Assert.Empty(e.FindAll("#dv-tutar-bos"));
        e.Find("#dv-tutar").Input("50");
        Assert.Empty(e.FindAll("#dv-tutar-bos"));
        e.Find("#dv-tutar").Input("");
        e.WaitForAssertion(() => Assert.Equal(Toplu5Sozlugu.Tr.DovizTutarBos, e.Find("#dv-tutar-bos").TextContent.Trim()));
        Assert.Equal("alert", e.Find("#dv-tutar-bos").GetAttribute("role"));
    }
}

// ═══ (4) /gider — seçicide nakit · bant tekrarı yok · boş listede değişmez yok · virman BASİT dili ═══
public sealed class Toplu5GiderTestleri : BunitContext
{
    private bool kayitli;

    public Toplu5GiderTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Sahte(Toplu5GiderTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var y = istek.RequestUri!.AbsolutePath;
            var giderler = t.kayitli
                ? """[{"id":"g1","kasaId":"k1","kasaAdi":"Ana Kasa","kategoriId":"gk1","kategoriAdi":"Kira","tutar":500,"paraBirimi":"TRY","aciklama":null,"tarih":"2026-10-02T09:00:00","stornoSatiri":false,"stornolandi":false}]"""
                : "[]";
            var virmanlar = t.kayitli
                ? """[{"cikisBacakId":"v1","girisBacakId":"v2","kaynakTur":"KASA","kaynakAdi":"Ana Kasa","hedefTur":"BANKA","hedefAdi":"Ziraat","tutar":100,"paraBirimi":"TRY","aciklama":null,"tarih":"2026-10-02T09:00:00","stornoSatiri":false}]"""
                : "[]";
            return Task.FromResult(Toplu5Ortak.Yanit(y switch
            {
                _ when y.EndsWith("/kasalar/nakit-ayrimi") =>
                    """[{"kasaId":"k1","paraBirimi":"TRY","toplam":221726.43,"nakit":193830.90,"kart":25895.53,"havale":0},{"kasaId":"k2","paraBirimi":"TRY","toplam":5000,"nakit":5000,"kart":0,"havale":0}]""",
                _ when y.EndsWith("/kasalar") =>
                    """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":221726.43,"aktif":true},{"id":"k2","ad":"Vitrin Kasası","paraBirimi":"TRY","bakiye":5000,"aktif":true}]""",
                _ when y.EndsWith("/bankalar") =>
                    """{"hesaplar":[{"id":"bh1","bankaAdi":"Ziraat","hesapAdi":"Vadesiz","hesapSahibi":null,"iban":"TR11","paraBirimi":"TRY","bakiye":5000,"aktif":true}]}""",
                _ when y.EndsWith("/gider-kategorileri") => """{"kategoriler":[{"id":"gk1","ad":"Kira","aciklama":null,"aktif":true,"giderAdedi":0,"giderToplamTl":0}],"toplam":1}""",
                _ when y.EndsWith("/giderler") => $$"""{"satirlar":{{giderler}},"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}""",
                _ when y.EndsWith("/virman") => $$"""{"satirlar":{{virmanlar}},"toplam":0,"limit":50,"dahaFazlasiVar":false,"sinirNotu":null}""",
                _ when y.Contains("/kur/guncel") => """{"id":"kur1","hasAlis":4100,"hasSatis":4150}""",
                _ when y.Contains("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                _ => "{}",
            }));
        }
    }

    private IRenderedComponent<Gider> Ciz(string mod, string? sekme = null)
    {
        Toplu5Ortak.Oturum(this, mod);
        if (sekme is not null)
            Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo($"/gider?sekme={sekme}");
        var e = Render<Gider>();
        e.WaitForState(() => e.FindAll(sekme is null ? "#gd-kasa" : "#gd-v-kaynak").Count > 0);
        GiderAkordeon.Ac(e); // SON-KALANLAR: geçmiş kayıtlar kapalı akordeon
        return e;
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T5_Kasa_seciciinde_CEKMECEDEKI_NAKIT_yazar_toplam_degil(string mod)
    {
        var e = Ciz(mod);
        e.WaitForAssertion(() => Assert.Contains(string.Format(Toplu5Sozlugu.Tr.GiderKasaNakitSecenek, "Ana Kasa (₺)", Bicim.Para(193_830.90m, "TRY")),
            SeciciYardim.ListeMetni(e, "gd-kasa"), StringComparison.Ordinal));
        var liste = SeciciYardim.ListeMetni(e, "gd-kasa");
        Assert.DoesNotContain(Bicim.Para(221_726.43m, "TRY"), liste, StringComparison.Ordinal);
    }

    [Fact]
    public void T5_Nakit_asiminda_bant_kilidi_TEKRARLAMAZ_sifir_kalem_yazilmaz()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        SeciciYardim.Sec(e, "gd-kasa", "k1");
        e.Find("#gd-tutar").Input("200000");
        var kilit = e.Find("#gd-kaydet-kilit").TextContent.Trim();
        var bant = e.Find("#gd-yetersiz-metin").TextContent.Trim();
        Assert.Contains("193.830,90", kilit, StringComparison.Ordinal);
        Assert.DoesNotContain("193.830,90", bant, StringComparison.Ordinal);   // aynı sayı iki kez yok
        Assert.Contains(Bicim.Para(25_895.53m, "TRY"), bant, StringComparison.Ordinal);
        Assert.DoesNotContain("havale", bant, StringComparison.OrdinalIgnoreCase);   // havale 0 → yazılmaz
        Assert.Equal(Gider.NakitBantMetni(Toplu5Sozlugu.Tr, 25_895.53m, 0m, "TRY"), bant);
    }

    [Fact]
    public void T5_Nakit_bant_metni_iki_pay_da_varsa_ikisini_sayar()
    {
        var t = Gider.NakitBantMetni(Toplu5Sozlugu.Tr, 10m, 20m, "TRY");
        Assert.Contains(string.Format(Toplu5Sozlugu.Tr.GiderKartPayi, Bicim.Para(10m, "TRY")), t, StringComparison.Ordinal);
        Assert.Contains(string.Format(Toplu5Sozlugu.Tr.GiderHavalePayi, Bicim.Para(20m, "TRY")), t, StringComparison.Ordinal);
        Assert.DoesNotContain(string.Format(Toplu5Sozlugu.Tr.GiderKartPayi, Bicim.Para(0m, "TRY")), Gider.NakitBantMetni(Toplu5Sozlugu.Tr, 0m, 20m, "TRY"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T5_Bos_gider_listesinde_degismez_kutusu_YOK_kayit_varken_VAR(string mod)
    {
        var bos = Ciz(mod);
        bos.WaitForState(() => bos.FindAll(".bos-durum").Count > 0);
        Assert.Empty(bos.FindAll("#degismez-ipucu-Gider"));
        bos.Dispose();
        kayitli = true;
        var dolu = Render<Gider>();
        GiderAkordeon.Ac(dolu); // SON-KALANLAR: geçmiş kayıtlar kapalı akordeon
        dolu.WaitForState(() => dolu.FindAll("#gd-storno-g1").Count > 0);
        Assert.NotNull(dolu.Find("#degismez-ipucu-Gider"));
    }

    [Fact]
    public void T5_BASIT_kasa_toplami_satiri_YOK_PRO_VAR()
    {
        var b = Ciz(ArayuzModuServisi.Basit);
        SeciciYardim.Sec(b, "gd-kasa", "k1");
        b.WaitForElement("#gd-kasa-nakit");
        Assert.Empty(b.FindAll("#gd-kasa-bakiye"));
        b.Dispose();
        Toplu5Ortak.Oturum(this, ArayuzModuServisi.Profesyonel);
        var pro = Render<Gider>();
        pro.WaitForState(() => pro.FindAll("#gd-kasa").Count > 0);
        SeciciYardim.Sec(pro, "gd-kasa", "k1");
        pro.WaitForElement("#gd-kasa-nakit");
        Assert.NotNull(pro.Find("#gd-kasa-bakiye"));
    }

    [Fact]
    public void T5_BASIT_virman_basligi_ve_aciklamasi_jargonsuz_PRO_aynen()
    {
        var b = Ciz(ArayuzModuServisi.Basit, "virman");
        var t5 = Toplu5Sozlugu.Tr;
        Assert.Equal(t5.VirmanBaslikBasit, b.Find(".kart-baslik h3").TextContent.Trim());
        Assert.Equal(t5.VirmanAciklamaBasit, b.Find("#gd-virman-aciklama").TextContent.Trim());
        foreach (var jargon in new[] { "bacak", "ciro", "storno" })
            Assert.DoesNotContain(jargon, b.Find("#gd-virman-aciklama").TextContent, StringComparison.OrdinalIgnoreCase);
        b.Dispose();
        Toplu5Ortak.Oturum(this, ArayuzModuServisi.Profesyonel);
        var p = Render<Gider>();
        p.WaitForState(() => p.FindAll("#gd-v-kaynak").Count > 0);
        Assert.Equal(Sozlukler.Tr.Gider.VirmanBaslik, p.Find(".kart-baslik h3").TextContent.Trim());
        Assert.Equal(Sozlukler.Tr.Gider.VirmanAciklama, p.Find("#gd-virman-aciklama").TextContent.Trim());
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T5_Virman_iki_taraf_notu_bos_listede_YOK_kayitta_hizali_sinifla(string mod)
    {
        var bos = Ciz(mod, "virman");
        bos.WaitForState(() => bos.FindAll(".bos-durum").Count > 0);
        Assert.Empty(bos.FindAll("#gd-v-iki-bacak"));
        bos.Dispose();
        kayitli = true;
        var dolu = Render<Gider>();
        GiderAkordeon.Ac(dolu); // SON-KALANLAR: geçmiş kayıtlar kapalı akordeon
        dolu.WaitForState(() => dolu.FindAll("#gd-v-iki-bacak").Count > 0);
        var not = dolu.Find("#gd-v-iki-bacak");
        Assert.Contains("t5-kart-not", not.ClassName ?? "", StringComparison.Ordinal);
        Assert.Equal(mod == ArayuzModuServisi.Basit ? Toplu5Sozlugu.Tr.VirmanIkiBacakBasit : Sozlukler.Tr.Gider.IkiBacakNotu, not.TextContent.Trim());
    }
}

// ═══ (5) Gün sonu — BASİT kırılım (sıfır kalem yok, parça bölünmez) · tekrar tutar yok · PRO durum + formül ⓘ ═══
public sealed class Toplu5GunSonuTestleri : BunitContext
{
    public Toplu5GunSonuTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class Sahte : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            return Task.FromResult(Toplu5Ortak.Yanit(yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/aylik") => """{"yil":2026,"ay":10,"gunler":[]}""",
                var y when y.EndsWith("/raporlar/gunluk") => """{"tarih":"2026-10-02","satis":{"adet":2,"toplamTl":64190.71,"toplamHas":1.2},"alis":{"adet":0,"toplamTl":0,"toplamHas":0},"hurda":{"toplamHas":0,"odenenTl":0},"netHasGramHareketi":-1.2,"hareketSayisi":2,"kasalar":[{"id":"k1","ad":"Ana Kasa","bakiye":64190.71,"paraBirimi":"TRY"}]}""",
                var y when y.EndsWith("/kasalar/nakit-ayrimi") => """[{"kasaId":"k1","paraBirimi":"TRY","toplam":64190.71,"nakit":0,"kart":40000,"havale":24190.71}]""",
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") => """
                    {"tarih":"2026-10-02",
                     "tahsilat":{"nakit":0,"kart":40000.00,"havale":24190.71,"diger":0,"toplam":64190.71,"virmanGiris":0,"acilisDevri":0},
                     "satis":{"sayisi":2,"toplamTl":64190.71,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":2,"faturasizTl":64190.71},
                     "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
                     "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
                     "fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},
                     "fark":64190.71,"mutabik":false,"uyarilar":[],"faturasizIslemler":[],"faturaEslesme":null}
                    """,
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                var y when y.EndsWith("/pozisyon") => """{"anaMaden":"ALTIN","altin":{"maden":"ALTIN","vitrinGram":10,"hurdaGram":0,"cariAlacakGram":0,"cariBorcGram":0,"netGram":10,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},"digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,"hesaplanmaAni":"2026-10-02T09:00:00Z","donemdenBagimsiz":true}""",
                var y when y.Contains("/bildirimler") => """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""",
                _ => "{}",
            }));
        }
    }

    private IRenderedComponent<Raporlar> Ciz(string mod)
    {
        Toplu5Ortak.Oturum(this, mod);
        return Render<Raporlar>();
    }

    [Fact]
    public void T5_BASIT_kirilimda_SIFIR_kalem_yok_her_kalem_bolunmez_parca()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.WaitForElement("#bugun-tahsilat-kirilim");
        var p = e.Find("#bugun-tahsilat-kirilim");
        var t5 = Toplu5Sozlugu.Tr;
        Assert.Equal($"{t5.GunSonuTahsilat} {Bicim.TlKurus(64190.71m)}: {string.Format(t5.KirilimKart, Bicim.TlKurus(40000m))} · {string.Format(t5.KirilimHavale, Bicim.TlKurus(24190.71m))}",
            p.TextContent.Trim());
        Assert.DoesNotContain(string.Format(t5.KirilimNakit, Bicim.TlKurus(0m)), p.TextContent, StringComparison.Ordinal);
        Assert.Equal(3, p.QuerySelectorAll("span.t5-kirilim-parca").Length);
        Assert.Contains("t5-kirilim", p.ClassName ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void T5_BASIT_faturasiz_satir_farkla_AYNI_tutari_ikinci_kez_basmaz()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.WaitForElement("#bugun-faturasiz");
        Assert.Equal(Bicim.Tl(64190.71m), e.Find("#bugun-fark-tutar").TextContent.Trim());
        var satir = e.Find("#bugun-faturasiz .panel-liste-ad").TextContent.Trim();
        Assert.Equal(string.Format(DegisimGunSonuSozlugu.Tr.FaturasizAdet, 2), satir);   // DEGISIM-GUNSONU: fark satırı adı söyler, burada adet
        Assert.DoesNotContain(Bicim.Tl(64190.71m), satir, StringComparison.Ordinal);
    }

    [Fact]
    public void T5_PRO_gun_sonu_durumu_cumle_duzeninde_formul_ipucunda()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        e.WaitForElement("#rp-sekme-gunsonu").Click();
        e.WaitForElement("#mut-fark-tanimi");
        Assert.DoesNotContain("FARK VAR", e.Markup, StringComparison.Ordinal);
        // PAKET RAPOR-4 (tasarım DÜŞÜK): faturasız satış varken durum "Fark var" değil, somut: "2 satışın faturası yok"
        // (cümle düzeni korunur; faturasız satış yoksa eski "Fark var" metni kalır — RaporDortEkranTestleri.RD4E03).
        Assert.Contains(string.Format(DegisimGunSonuSozlugu.Tr.FaturasizAdet, 2), e.Find("#mut-kartlar").TextContent, StringComparison.Ordinal);
        var bilgi = e.Find("#mut-fark-tanimi-satir > details#mut-fark-bilgi");
        Assert.NotNull(bilgi.QuerySelector("#mut-fark-tanimi"));
        Assert.Equal(ZoBelgeSozlugu.Tr.GunSonuHesapBaslik, bilgi.QuerySelector("summary")!.GetAttribute("aria-label"));
    }
}

// ═══ (6) /islemler — BASİT Değişim kimlik başlığı sade ═══
public sealed class Toplu5IslemTestleri : BunitContext
{
    public Toplu5IslemTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class Sahte : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            return Task.FromResult(Toplu5Ortak.Yanit(yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.EndsWith("/cari") => """
                    [{"id":"c1","ad":"Tuna Akarsu","tur":"MUSTERI","telefon":null,"email":null,"tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"riskSeviye":null,"pepBeyani":false,"sistemCarisi":false,"aktif":true},
                     {"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","telefon":null,"email":null,"tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"riskSeviye":null,"pepBeyani":false,"sistemCarisi":true,"aktif":true}]
                    """,
                var v when v.Contains("/tevsik/gunluk") => """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":3.206,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"AMAC","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/islemler/altin-bacagi/ayarlar") => """{"stopajOranlari":[{"saticiTipi":"NIHAI_TUKETICI","oran":0,"pusulaGerekir":true},{"saticiTipi":"ESNAF_MUAF","oran":0.02,"pusulaGerekir":true},{"saticiTipi":"MUKELLEF","oran":0,"pusulaGerekir":false}],"alisIskontoOran":null,"saticiAdresiZorunlu":true}""",
                var v when v.Contains("/reyonlar") || v.Contains("/islemler/son") || v.Contains("/pos/cihazlar") || v.Contains("/islemler/personeller") || v.Contains("/iade-adaylari") => "[]",
                _ => "{}",
            }));
        }
    }

    private IRenderedComponent<Islemler> Takas(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Sahip", "t@ornek.com", "SAHIP", "s1", null, mod));
        var e = Render<Islemler>();
        e.WaitForElement("#islem-tur-SATIS");
        e.Find("#islem-tur-TAKAS").Click();
        e.WaitForElement("#takas-satici");
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.Find("#takas-eski-gram").Input("30");
        SeciciYardim.Sec(e, "takas-yeni-urun", "u1");
        e.Find("#takas-yeni-fiyat").Input("10000");
        SeciciYardim.Sec(e, "takas-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#masak-amac");
        return e;
    }

    [Fact]
    public void T5_BASIT_degisim_kimlik_kutusu_basligi_SADE_MASAK_yazmaz_kapı_aynen()
    {
        var e = Takas(ArayuzModuServisi.Basit);
        var baslik = e.Find(".zo-masak-baslik").TextContent.Trim();
        Assert.Equal(string.Format(YasalParamSozlugu.Tr.BaslikKimlik, Bicim.Tl(185_000m)), baslik);   // YASAL-PARAM-EKRAN tur 2: tetikleyen kademe
        Assert.DoesNotContain("MASAK", baslik, StringComparison.Ordinal);
        // Kapı aynen: kimlik eksikken Kaydet kilitli.
        Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled"));
    }

    [Fact]
    public void T5_PRO_degisim_kimlik_basligi_yasal_atifla_AYNEN()
    {
        var e = Takas(ArayuzModuServisi.Profesyonel);
        // YASAL-PARAM-EKRAN tur 2 (tasarım hakemi KRİTİK): PRO başlığı BASİT ile AYNI; MASAK atfı #masak-kural cümlesinde.
        Assert.Equal(string.Format(YasalParamSozlugu.Tr.BaslikKimlik, Bicim.Tl(185_000m)), e.Find(".zo-masak-baslik").TextContent.Trim());
        // YASAL-KAPI-2 tur 2 (doğrulayıcı KRİTİK "aynı eşik 4 yerde"): PRO /islemler'de MASAK atfı ve kademe "Yasal kapılar"
        // panelinde (tek yer); kural satırı yalnız saklama notu.
        Assert.Contains("MASAK", e.Find("#islem-pro-kimlik-etiket").TextContent);
        Assert.Equal(YasalParamSozlugu.Tr.Saklama, e.Find("#masak-kural").TextContent.Trim());
    }
}

// ═══ (7) Kaynak + CSS + sözlük ═══
public sealed class Toplu5KaynakTestleri
{
    [Fact]
    public void T5_toplu5_css_toplu4ten_sonra_olcekten_ve_ortaktan_once()
    {
        var html = Toplu5Ortak.Dosya("Kuyumcu.Web", "wwwroot", "index.html");
        var t4 = html.IndexOf("css/toplu4.css", StringComparison.Ordinal);
        var t5 = html.IndexOf("css/toplu5.css", StringComparison.Ordinal);
        var ol = html.IndexOf("css/olcek.css", StringComparison.Ordinal);
        var o = html.IndexOf("css/ortak.css", StringComparison.Ordinal);
        Assert.True(t4 > 0 && t5 > t4 && ol > t5 && o > ol);
    }

    [Fact]
    public void T5_Sabit_renk_yok_yalniz_belirtec()
    {
        var css = Toplu5Ortak.Css();
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b(?![\w-])(?=[^{]*;)", Regex.Replace(css, @"#[a-z][\w-]*", ""));
        Assert.DoesNotContain("rgb(", css, StringComparison.Ordinal);
    }

    [Fact]
    public void T5_Kasalar_ayrim_satiri_14px_govde_rengi()
    {
        var b = Toplu5Ortak.Blok(Toplu5Ortak.Css(), ".kart p[id^=\"kasa-ayrim-\"]:not([id^=\"kasa-ayrim-hata-\"])") ?? "";
        Assert.Contains("font-size: max(14px, var(--yazi-sm))", b, StringComparison.Ordinal);
        Assert.Contains("color: var(--metin)", b, StringComparison.Ordinal);
    }

    [Fact]
    public void T5_Gider_sayilari_en_az_14px_nakit_rozeti_vurgulu()
    {
        var css = Toplu5Ortak.Css();
        Assert.Contains("font-size: max(14px, var(--yazi-sm))", Toplu5Ortak.Blok(css, "#gd-kasa-sabit, #gd-kasa-nakit, #gd-kasa-bakiye, #gd-toplam") ?? "", StringComparison.Ordinal);
        Assert.Contains("font-weight: var(--kalin)", Toplu5Ortak.Blok(css, "#gd-kasa-nakit > .rozet") ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void T5_Gider_sekme_seridi_dar_ekranda_TEK_satir()
    {
        var css = Toplu5Ortak.Css();
        var serit = Toplu5Ortak.Blok(css, "#gd-sekme-seridi") ?? "";
        Assert.Contains("display: flex", serit, StringComparison.Ordinal);   // app.css ≤ 600 px 2 sütunlu ızgarayı geçersiz kılar
        // PAKET DIL-EKRAN: İngilizce adlar ("Expense Categories") 375 px'te tek sıraya sığmıyordu (K3 taşan yazı 3).
        // Şerit artık SARAR: Türkçe adlar yine tek sıradadır (sığar), uzun adlar alt satıra iner, yazı taşmaz.
        Assert.Contains("flex-wrap: wrap", serit, StringComparison.Ordinal);
        Assert.DoesNotContain("flex-wrap: nowrap", serit, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 600px)", css, StringComparison.Ordinal);
        Assert.Contains("white-space: nowrap", Toplu5Ortak.Blok(css, "#gd-sekme-seridi > .sekmeler-sekme") ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void T5_Doviz_cekmecesi_musteri_cekmecesi_olcusunde()
    {
        var css = Toplu5Ortak.Css();
        const string k = "html[data-mod] .t5-cekmece-kap :is(#cekmece-doviz-veresiye, #cekmece-doviz-tahsilat)";
        Assert.Contains("width: min(440px, 100vw)", Toplu5Ortak.Blok(css, "html[data-mod] .t5-cekmece-kap .cekmece:is(#cekmece-doviz-veresiye, #cekmece-doviz-tahsilat)") ?? "", StringComparison.Ordinal);
        Assert.Contains("padding: 24px 26px 0", Toplu5Ortak.Blok(css, k + " .cekmece-bas") ?? "", StringComparison.Ordinal);
        Assert.Contains("font-size: var(--yazi-cekmece-baslik)", Toplu5Ortak.Blok(css, k + " .cekmece-bas h2") ?? "", StringComparison.Ordinal);
        Assert.Contains("padding: 18px 26px 0", Toplu5Ortak.Blok(css, k + " .cekmece-govde") ?? "", StringComparison.Ordinal);
        Assert.Contains("border-top: 0", Toplu5Ortak.Blok(css, k + " .cekmece-alt") ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void T5_Kirilim_parcasi_bolunmez_ve_tahsilat_baglantisi_belirgin()
    {
        var css = Toplu5Ortak.Css();
        Assert.Contains("white-space: nowrap", Toplu5Ortak.Blok(css, ".t5-kirilim-parca") ?? "", StringComparison.Ordinal);
        Assert.Contains("font-size: max(14px, var(--yazi-sm))", Toplu5Ortak.Blok(css, ".t5-kirilim") ?? "", StringComparison.Ordinal);
        Assert.Contains("text-decoration: underline", Toplu5Ortak.Blok(css, "#cari-panel-tahsilat") ?? "", StringComparison.Ordinal);
        Assert.Contains("padding: var(--bosluk-3) 1.5rem", Toplu5Ortak.Blok(css, ".kart > #gd-v-iki-bacak.t5-kart-not") ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void T5_Islemler_razor_1400_satir_tavaninda_MASAK_cagrisi_Sade_tasir()
    {
        var kod = Toplu5Ortak.Dosya("Kuyumcu.Web", "Pages", "Islemler.razor");
        Assert.True(kod.Split('\n').Length <= 1400);
        Assert.Contains("AdresZorunlu=\"@(KimlikBirlesik && BirlesikSatici.AdresZorunlu)\" Sade=\"@Modu.BasitMi\" EsikPanelde=\"@MasakEsikPanelde\" />", kod, StringComparison.Ordinal);
    }

    [Fact]
    public void T5_Sozluk_TR_EN_dolu_ve_farkli()
    {
        foreach (var p in typeof(Toplu5Metinleri).GetProperties())
        {
            var tr = (string)p.GetValue(Toplu5Sozlugu.Tr)!;
            var en = (string)p.GetValue(Toplu5Sozlugu.En)!;
            Assert.False(string.IsNullOrWhiteSpace(tr), p.Name);
            Assert.False(string.IsNullOrWhiteSpace(en), p.Name);
            Assert.DoesNotContain("sunucu", tr, StringComparison.OrdinalIgnoreCase);
        }
        Assert.Same(Toplu5Sozlugu.En, Toplu5Sozlugu.Al("en"));
        Assert.Same(Toplu5Sozlugu.Tr, Toplu5Sozlugu.Al("tr"));
    }
}

// ═══ (8) TOPLU-5 tur 2 — tasarım hakemi KRİTİK: fişte ham 26 haneli kayıt kimliği basılmaz ═══
public sealed class Toplu5FisKoduTestleri : BunitContext
{
    private const string Kimlik = "01M3Z355KJ1KMDCYYB8A6H51X2";
    private string tur = BelgeTurleri.SatisFisi;
    private bool kayitKimligi = true;

    public Toplu5FisKoduTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Sahte(Toplu5FisKoduTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal) =>
            Task.FromResult(Toplu5Ortak.Yanit(istek.RequestUri!.AbsolutePath.Contains("/cikti/", StringComparison.Ordinal)
                ? $$"""
                {"tur":"{{t.tur}}","belgeAdi":"SATIŞ FİŞİ","belgeNo":"{{(t.kayitKimligi ? Kimlik : "2026-000123")}}","dayanak":null,
                 "duzenlemeTarihi":"2026-10-02T08:00:00Z",
                 "duzenleyen":{"unvan":"Test Kuyumcu","sube":"Merkez","adres":"Gümüşhane Sokak No: 12","vergiDairesi":"Merkez","vergiNo":"0000000000","telefon":null},
                 "karsi":{"baslik":"Müşteri","ad":"Ayşe Yılmaz","vergiNo":null,"adres":null,"telefon":null},
                 "bolumler":[{"baslik":"Kalemler","sutunBasliklari":["Tür","Açıklama","Has gram","Tutar"],"sutunBicimleri":["METIN","METIN","HAS","PARA"],
                   "satirlar":[{"hucreler":["Satış","22 Ayar Bilezik","13.9232","45000.00"],"vurgu":false,"paraBirimi":"TRY"}],"not":null}],
                 "tutarlar":[{"etiket":"Toplam tutar","deger":45000.00,"paraBirimi":"TRY","toplam":true}],
                 "imzaBasliklari":["Satıcı","Alıcı"],"not":null,"uyari":null,"belgeNoKayitKimligi":{{(t.kayitKimligi ? "true" : "false")}}}
                """
                : "{}"));
    }

    private IRenderedComponent<Cikti> Ekran()
    {
        var e = Render<Cikti>(p => p.Add(x => x.Tur, tur).Add(x => x.Id, Kimlik));
        e.WaitForElement("#cikti-belge");
        return e;
    }

    [Fact]
    public void T5_Satis_fisinde_ham_kimlik_BASILMAZ_kisa_kayit_kodu_basilir()
    {
        var e = Ekran();
        Assert.Equal("8A6H-51X2", e.Find("#cikti-no").TextContent.Trim());
        Assert.Equal(Toplu5Sozlugu.Tr.CiktiKayitKodu, e.Find("#cikti-no-etiket").TextContent.Trim());
        // Kâğıda basılan metinde 26 haneli kimlik HİÇBİR yerde yok (yalnız ekranda title).
        Assert.DoesNotContain(Kimlik, e.Find("#cikti-belge").TextContent, StringComparison.Ordinal);
        Assert.Equal(string.Format(Toplu5Sozlugu.Tr.CiktiKayitKoduTam, Kimlik), e.Find("#cikti-no").GetAttribute("title"));
        Assert.Contains("belge-no-kisa", e.Find("#cikti-no").ClassName ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void T5_Okunur_belge_numarasi_aynen_basilir_etiket_Belge_no()
    {
        kayitKimligi = false;
        var e = Ekran();
        Assert.Equal("2026-000123", e.Find("#cikti-no").TextContent.Trim());
        Assert.Equal(Sozlukler.Tr.Cikti.BelgeNo, e.Find("#cikti-no-etiket").TextContent.Trim());
        Assert.Null(e.Find("#cikti-no").GetAttribute("title"));
    }

    [Theory]
    [InlineData("01M3Z355KJ1KMDCYYB8A6H51X2", "8A6H-51X2")]
    [InlineData("01m36t476xd2xvch13xm7h0evq", "XM7H-0EVQ")]
    [InlineData("2026-0001", "2026-0001")]   // 12 haneden kısa: zaten okunur, aynen
    [InlineData(null, "")]
    public void T5_KisaKod_saf_kural(string? girdi, string beklenen) =>
        Assert.Equal(beklenen, A4Belge.KisaKod(girdi));

    [Fact]
    public void T5_Kisa_kod_okunur_boyda_soluk_degil()
    {
        var b = Toplu5Ortak.Blok(Toplu5Ortak.Css(), ".belge-no .belge-no-kimlik.belge-no-kisa") ?? "";
        Assert.Contains("font-size: 11pt", b, StringComparison.Ordinal);
        Assert.Contains("opacity: 1", b, StringComparison.Ordinal);
    }
}
