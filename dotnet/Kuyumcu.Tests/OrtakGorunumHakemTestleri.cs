using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ORTAK-1 — HAKEM TURU 29.09 (tasarım hakemi + son doğrulama bulguları)
//
//   HK1/HK2  /uyum?sekme=risk: ham yapılandırma anahtarı (MasakRisk:Etkin=false)
//            HİÇBİR modda yok; BASİT'te tek sade cümle, "durdurmaz" ayrıntısı PRO'da.
//   HK3–HK5  /isletme: BASİT'te ikinci başlık/açıklama yok; "+ Yeni Şube" ikincil
//            (ekranda tek birincil); son şube notu BASİT'te "kaldırılamaz".
//   HK6/HK7  /urunler/agac: HUB geliştirici notu yok; BASİT'te not gizli, PRO'da tek
//            cümle; "+ Yeni Reyon" ikincil.
//   HK8      /siparisler: BASİT'te "işlem uçları" yok.
//   HK9      /raporlar/vade-takibi: BASİT'te "ufuk" yok.
//   HK10     /vezne: BASİT'te ayar rozeti ("Kasa kuyruğu ayrımı zorunlu") yok.
//   HK11–13  ortak.css sözleşmeleri: Panel değer tabanı, sipariş notu iç boşluğu,
//            /kullanicilar ızgarasında ad hücresi içeriğinden dar olmaz.
// ═══════════════════════════════════════════════════════════════════════════
public sealed class OrtakGorunumHakemTestleri : BunitContext
{
    private bool tekSube;

    private const string RiskOzet = """
        {"dusuk":2,"orta":1,"yuksek":1,"pepSayisi":1,"tazelenecek":0,"profilsiz":3,
         "kuralEtkin":false,"listeSiniri":50,
         "yuksekRiskliler":[{"cariId":"c9","cariAd":"Anadolu Holding A.Ş.","puan":75,
           "seviye":"YUKSEK","kaynak":"HESAP","sebepler":["PEP_BEYANI"],
           "olusturuldu":"2026-09-17T08:00:00Z"}]}
        """;

    private const string IkiSube = """
        [{"id":"s1","ad":"Merkez","adres":"Kapalıçarşı","telefon":"02120000000","aktif":true},
         {"id":"s2","ad":"Nişantaşı","adres":null,"telefon":null,"aktif":true}]
        """;

    private const string TekSube = """
        [{"id":"s1","ad":"Merkez","adres":"Kapalıçarşı","telefon":"02120000000","aktif":true}]
        """;

    private const string Tenant = """
        {"id":"t1","ad":"Kuyumcu","plan":"PRO","aiEtkin":false,"aktif":true,"kurKaynagi":null,
         "maliyetYontemi":null,"varsayilanArayuzModu":"PRO","durum":"AKTIF"}
        """;

    private const string Reyonlar = """
        [{"id":"r1","subeId":"s1","ad":"A-VITRIN","tip":"VITRIN","aktif":true,"urunSayisi":2,"yerlesikAdet":4}]
        """;

    private const string Siparisler = """
        {"istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":0,"dahaFazlasiVar":false,
         "gecikenSayisi":0,"durumKirilimi":[],"siparisler":[]}
        """;

    private const string Vade = """
        {"ufukGun":7,"gecikenToplam":3000,"yaklasanToplam":2000,"vadesizToplam":1500,
         "ufukOtesiToplam":179999.94,"acikToplam":186499.94,"cariler":[]}
        """;

    private const string Kuyruk = """
        {"durum":"VEZNEDE","istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":0,
         "dahaFazlasiVar":false,"vezneZorunlu":false,"gerekceAsgari":10,"subeId":"s1",
         "durumKirilimi":[{"durum":"VEZNEDE","adet":1}],
         "fisler":[
           {"id":"v1","durum":"VEZNEDE","toplamTl":5000.00,"damgaliToplamTl":5000.00,
            "toplamTahmini":false,"cariId":"c1","cariAd":"Anadolu Holding A.Ş.",
            "aciklama":"bilezik","hazirlayanKullaniciId":"u2","hazirlayanAd":"Ayşe Yılmazoğlu",
            "kalemSayisi":1,"hareketFisId":null,"duzenlenebilir":false,"mutabakatGerekli":false,
            "olusturuldu":"2026-09-17T08:00:00Z","veznelendi":"2026-09-17T08:05:00Z",
            "guncellendi":"2026-09-17T08:05:00Z"}]}
        """;

    public OrtakGorunumHakemTestleri()
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

    private sealed class SahteApi(OrtakGorunumHakemTestleri sahip) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/masak/risk/ozet") => RiskOzet,
                var y when y.EndsWith("/masak/ozet") => """{"acikBildirim":0,"suresiGecmis":0,"yaklasan":0,"son30GunTespit":0}""",
                var y when y.Contains("/masak/risk/cariler") || y.EndsWith("/cari") =>
                    """[{"id":"c9","ad":"Anadolu Holding A.Ş.","riskSeviye":"YUKSEK","pepBeyani":true}]""",
                var y when y.Contains("/masak/") => "[]",
                var y when y.EndsWith("/subeler") => sahip.tekSube ? TekSube : IkiSube,
                var y when y.EndsWith("/admin/tenant") => Tenant,
                var y when y.EndsWith("/reyonlar") => Reyonlar,
                var y when y.EndsWith("/urun-kategorileri") => "[]",
                var y when y.EndsWith("/siparisler") => Siparisler,
                var y when y.Contains("/raporlar/vade-takibi") => Vade,
                var y when y.EndsWith("/vezne/kuyruk") => Kuyruk,
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private static OrtakGorunumMetinleri B => OrtakGorunumSozlugu.Tr;
    private static Sozluk Pro => Sozlukler.Getir("tr");

    // ── /uyum?sekme=risk ─────────────────────────────────────────────────────
    private IRenderedComponent<Kuyumcu.Web.Pages.Uyum> Risk()
    {
        var e = Render<Kuyumcu.Web.Pages.Uyum>();
        e.Find("#uy-sekme-risk").Click();
        e.WaitForElement("#uy-risk-kapali");
        return e;
    }

    [Fact]
    public void HK1_uyum_risk_BASIT_tek_sade_cumle_ham_anahtar_ve_kisaltma_yok()
    {
        Mod(basit: true);
        var e = Risk();
        Assert.Equal(B.UyumRiskAciklamaBasit, e.Find("#uy-risk-aciklama").TextContent.Trim());
        Assert.Equal(B.UyumRiskKapaliBasit, e.Find("#uy-risk-kapali").TextContent.Trim());
        Assert.Empty(e.FindAll("#uy-risk-durdurmaz"));
        Assert.DoesNotContain("MasakRisk", e.Markup);
        Assert.DoesNotContain("Etkin=", e.Markup);
        Assert.DoesNotMatch(@"(?<![\p{L}])PEP(?![\p{L}])", e.Find("#uy-risk-aciklama").ParentElement!.TextContent);
        Assert.Contains(B.UyumPepSayisiBasit, e.Markup);
    }

    [Fact]
    public void HK2_uyum_risk_PRO_ayrinti_korunur_ham_anahtar_yine_yok()
    {
        Mod(basit: false);
        var e = Risk();
        Assert.Equal(Pro.Uyum.RiskAciklama, e.Find("#uy-risk-aciklama").TextContent.Trim());
        Assert.Equal(Pro.Uyum.RiskSkorDurdurmaz, e.Find("#uy-risk-durdurmaz").TextContent.Trim());
        Assert.Equal(B.UyumRiskKapaliPro, e.Find("#uy-risk-kapali").TextContent.Trim());
        Assert.DoesNotContain("MasakRisk", e.Markup);
    }

    // ── /isletme ─────────────────────────────────────────────────────────────
    private IRenderedComponent<Kuyumcu.Web.Pages.Isletme> Subeler()
    {
        var e = Render<Kuyumcu.Web.Pages.Isletme>();
        e.Find("#isl-sekme-subeler").Click();
        e.WaitForElement("#s-pasife-s1");
        return e;
    }

    [Fact]
    public void HK3_isletme_BASIT_ikinci_baslik_yok_yeni_sube_ikincil_tek_birincil()
    {
        Mod(basit: true);
        var e = Subeler();
        Assert.Empty(e.FindAll(".sayfa-baslik h1"));
        Assert.DoesNotContain(Pro.Isletme.Aciklama, e.Markup);
        var yeni = e.Find("#s-yeni-sube-ac").GetAttribute("class") ?? "";
        Assert.Contains("btn-ikincil", yeni);
        Assert.DoesNotContain("btn-birincil", yeni);
        Assert.Equal(["i-kimlik-duzenle"], e.FindAll(".btn-birincil").Select(x => x.Id ?? "").ToArray());
    }

    [Fact]
    public void HK4_isletme_PRO_ikinci_baslik_korunur_yeni_sube_yine_ikincil()
    {
        Mod(basit: false);
        var e = Subeler();
        Assert.Equal(Pro.Isletme.Baslik, e.Find(".sayfa-baslik h1").TextContent.Trim());
        Assert.Contains("btn-ikincil", e.Find("#s-yeni-sube-ac").GetAttribute("class") ?? "");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HK5_isletme_son_sube_notu_BASIT_kaldirilamaz_PRO_pasife_alinamaz(bool basit)
    {
        tekSube = true;
        Mod(basit);
        var e = Subeler();
        var beklenen = basit ? B.IsletmeSonSubeBasit : Pro.Isletme.SonSubeUyarisi;
        Assert.Equal(beklenen, e.Find("#s-son-sube-notu").TextContent.Trim());
        Assert.Equal(beklenen, e.Find("#s-pasife-s1").GetAttribute("title"));
        if (basit) Assert.DoesNotContain(Pro.Isletme.SonSubeUyarisi, e.Markup);
    }

    // ── /urunler/agac ────────────────────────────────────────────────────────
    private IRenderedComponent<Kuyumcu.Web.Pages.UrunAgaci> Agac()
    {
        var e = Render<Kuyumcu.Web.Pages.UrunAgaci>().AyrintiAc();
        e.WaitForElement("#agac-yeni-reyon");
        return e;
    }

    [Fact]
    public void HK6_urun_agaci_BASIT_gelistirici_notu_yok_tek_birincil()
    {
        Mod(basit: true);
        var e = Agac();
        Assert.Empty(e.FindAll("#agac-reyon-sayim-notu"));
        Assert.DoesNotContain("SAYIM KAPSAMI", e.Markup);
        Assert.DoesNotContain(B.UrunAgaciReyonSayimNotu, e.Markup);
        var reyon = e.Find("#agac-yeni-reyon").GetAttribute("class") ?? "";
        Assert.Contains("btn-ikincil", reyon);
        Assert.Equal(["agac-yeni-kategori"], e.FindAll(".btn-birincil").Select(x => x.Id ?? "").ToArray());
    }

    [Fact]
    public void HK7_urun_agaci_PRO_tek_sade_cumle()
    {
        Mod(basit: false);
        var e = Agac();
        Assert.Equal(B.UrunAgaciReyonSayimNotu, e.Find("#agac-reyon-sayim-notu").TextContent.Trim());
        Assert.DoesNotContain("SAYIM KAPSAMI", e.Markup);
        Assert.DoesNotContain("⚠", e.Markup);
    }

    // ── /siparisler ──────────────────────────────────────────────────────────
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HK8_siparis_notu_BASIT_ekran_adi_PRO_aynen(bool basit)
    {
        // KESIF-P6 tur 2 (hakem 01.10, K17 → P6): "deftere yazmaz / işlem uçları" açıklama satırı
        // kaldırıldı (tuvalde yok). Ölçüt: not satırı her iki modda 0, BASİT'te "uçlar" jargonu 0.
        Mod(basit);
        var e = Render<Kuyumcu.Web.Pages.Siparisler>().AyrintiAc();
        e.WaitForElement("#sip-yeni-ac");
        Assert.Empty(e.FindAll("#sip-defter-notu"));
        Assert.DoesNotContain(Pro.Siparis.DefterEtkisiYok, e.Markup);
        Assert.DoesNotContain(B.SiparisDefterNotuBasit, e.Markup);
        Assert.DoesNotContain("uçlar", e.Markup);
    }

    // ── /raporlar/vade-takibi ────────────────────────────────────────────────
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HK9_vade_BASIT_ufuk_demez_PRO_aynen(bool basit)
    {
        Mod(basit);
        var e = Render<Kuyumcu.Web.Pages.VadeTakibi>();
        e.WaitForAssertion(() => Assert.Contains(Pro.Vade.AcikToplam, e.Markup));
        if (basit)
        {
            Assert.Contains(B.VadeUfukOtesiBasit, e.Markup);
            Assert.Contains(B.VadeUfukOtesiAciklamaBasit, e.Markup);
            Assert.Contains(B.VadeDonemdenBagimsizBasit, e.Markup);
            Assert.DoesNotContain(Pro.Vade.UfukOtesiToplam, e.Markup);
            Assert.DoesNotContain(Pro.Vade.DonemdenBagimsiz, e.Markup);
        }
        else
        {
            Assert.Contains(Pro.Vade.UfukOtesiToplam, e.Markup);
            Assert.Contains(Pro.Vade.DonemdenBagimsiz, e.Markup);
            Assert.DoesNotContain(B.VadeUfukOtesiBasit, e.Markup);
        }
    }

    // ── /vezne ───────────────────────────────────────────────────────────────
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HK10_vezne_ayar_rozeti_BASIT_yok_PRO_var(bool basit)
    {
        Mod(basit);
        var e = Render<Kuyumcu.Web.Pages.Vezne>().AyrintiAc();
        e.WaitForElement("#vz-defter-notu");
        e.WaitForAssertion(() => Assert.Equal(basit ? 0 : 1, e.FindAll("#vz-ayrim-rozet").Count));
    }

    [Fact]
    public void HK15_vezne_hazirlayan_adi_tek_satir_hucrede()
    {
        // 30.09 tur ölçümü: "Ayşe Yılmazoğlu" 960–1440'ta iki satıra kırılıyordu (K3, 16 durum).
        Mod(basit: true);
        var e = Render<Kuyumcu.Web.Pages.Vezne>().AyrintiAc();
        var satir = e.WaitForElement("#vz-satir-v1");
        var td = satir.QuerySelectorAll("td").Single(t => t.TextContent.Trim() == "Ayşe Yılmazoğlu");
        Assert.Contains("hucre-tek-satir", td.GetAttribute("class") ?? "");
    }

    [Fact]
    public void HK16_vezne_toplam_rozeti_tutarin_altina_iner_ve_satir_kirar()
    {
        // Hakem turu 30.09 (DÜŞÜK): 960'ta "Kesin — kuyruğa gidince sabitlendi" rozeti tutarın yanında
        // tek satır kalıp Toplam sütununu genişletiyor, eylem sütunu kabın kenarında kesiliyordu.
        Mod(basit: true);
        var e = Render<Kuyumcu.Web.Pages.Vezne>().AyrintiAc();
        var rozet = e.WaitForElement("#vz-toplam-rozet-v1");
        Assert.Contains("vz-toplam-rozet", rozet.GetAttribute("class") ?? "");
        Assert.Matches(@"#vz-liste \.vz-toplam-rozet\s*\{\s*display:\s*block;", Css);
        Assert.Matches(@"#vz-liste \.vz-toplam-rozet > \.rozet\s*\{\s*white-space:\s*normal;\s*max-width:\s*12rem;", Css);
        // Yapışık eylem sütunu denendi, K9 binme ürettiği için geri alındı — bir daha açılmasın:
        Assert.DoesNotContain("#vz-liste > tbody > tr > td:last-child", Css);
    }

    // ── ortak.css sözleşmeleri ──────────────────────────────────────────────
    private static string Css => CssKaynak.YorumsuzCss(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "ortak.css"));

    [Fact]
    public void HK11_panel_buyuk_deger_uzun_kademede_1rem_tabanini_asmaz()
    {
        var css = Css;
        Assert.Matches(@"\.panel-soru > \.panel-soru-sayi:is\(\[data-deger-boy=""cok-uzun""\], \[data-deger-boy=""en-uzun""\]\),\s*\.panel-sade-kartlar > \.kart > \.kart-govde > \.stat-deger:is\(\[data-deger-boy=""cok-uzun""\], \[data-deger-boy=""en-uzun""\]\)\s*\{\s*font-size:\s*max\(1rem,", css);
        Assert.Matches(@"\.panel-sade \.panel-soru > \.panel-soru-sayi:is\([^)]*\)[^{]*\{\s*font-size:\s*max\(1rem,", css);
    }

    [Fact]
    public void HK12_siparis_notu_kart_ic_boslugunu_alir()
    {
        Assert.Matches(@"\.kart > #sip-defter-notu,\s*\.kart > #vz-defter-notu,\s*\.kart > #vz-sinir-uyarisi\s*\{\s*margin:\s*0;\s*padding:\s*0 1\.5rem 1rem;", Css);
    }

    [Fact]
    public void HK13_kullanicilar_tek_satir_ad_hucresi_olcek_9em_kuralini_icerikle_ezer()
    {
        // olcek.css: "html[data-mod] .ana-icerik .veri-tablosu > tbody > tr > td.hucre-vurgu:not(.sayi) { min-width: 9em }"
        // (özgüllük 0,5,4). Ezen kural daha özgül (0,6,4) ve içerik genişliğini verir.
        Assert.Matches(@"html\[data-mod\] \.ana-icerik \.veri-tablosu > tbody > tr > td\.hucre-vurgu\.hucre-tek-satir:not\(\.sayi\)\s*\{\s*min-width:\s*max-content;\s*\}", Css);
        var olcek = CssKaynak.YorumsuzCss(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "olcek.css"));
        Assert.Contains("td.hucre-vurgu:not(.sayi)", olcek);
    }

    [Fact]
    public void HK14_kullanicilar_ad_hucresi_tek_satir_sinifi_ve_siz_rozeti_tasir()
    {
        var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Pages", "Kullanicilar.razor");
        // PAKET GOC-D: liste ortak ızgarada — ad hücresinin sınıfı kolon tanımında; data-etiket'i ızgara her hücreye yazar.
        Assert.Contains("HucreSinif = _ => \"hucre-vurgu hucre-tek-satir\"", kaynak); // KESIF-P6 tur 2: q10-yigin etiketi
        Assert.Contains("id=\"k-siz\"", kaynak);
    }
}
