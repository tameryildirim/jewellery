using System.Net;
using Bunit;
using Kuyumcu.Api.Modules.Tercih;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-b — KATLANIR KENAR ÇUBUĞU (tuval Panel.dc.html <nav>)
//
// Kurucunun onayladığı taslakta menü 38 düz bağlantı değil, 6 KATLANIR gruptur.
// Katlamanın tek kabul ölçütü "göze az satır görünsün" DEĞİLDİR; tuval notu n2
// şunu söyler: «Menüde HİÇBİR ekran kaybolmadı». Bu dosya o cümlenin dört
// ölçülebilir karşılığını sınar:
//
//   (1) KAPALI GRUP KAYBETMEZ — kapalı grubun altındaki tek satır önizleme o
//       gruptaki EKRAN ADLARININ HEPSİNİ yazar; satırlar DOM'dan silinmez,
//       `hidden` ile katlanır (plan § 3 md.1 "özellik silinmez").
//   (2) VARSAYILAN DOĞRU     — kullanıcı hiç katlama yapmamışsa yalnız AKTİF
//       ROTANIN grubu açıktır; gerisi kapalıdır (plan § S-1 md. 1.1).
//   (3) MOD YETKİYİ GEVŞETMEZ — yetkisiz kullanıcının ekranı ne açık grupta,
//       ne kapalı grupta, NE DE ÖNİZLEME METNİNDE görünür (M-UX3-06).
//   (4) TERCİH KULLANICIYA ÖZEL — katlama sunucuya yazılır (/tercihler/benim),
//       yazma başarısızsa geri alınır ve bant çizilir (md.1).
//
// ⚠ DOM SÖZLEŞMESİ: mevcut id'ler DEĞİŞMEDİ (#kenar-grup-<slug>, #kenar-nav,
//   #kenar-satir-<slug>); yalnız #kenar-grup-dugme-<slug>, #kenar-onizleme-<slug>
//   ve #kenar-tum-ekranlar EKLENDİ.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class MenuKatlamaTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private string moduller = """["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","evrak","siparis","bildirim"]""";
    private string tercihler = "{}";
    private bool yazmaPatlasin;

    private sealed class SahteApi(
        List<(string, string, string)> kayit,
        Func<string> moduller, Func<string> tercihler, Func<bool> patlat) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            if (patlat() && istek.Method != HttpMethod.Get)
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"hata":"tercih yazılamadı"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            // ⚠ bUnit sözleşmesi: "{}" varsayılanı bırakılmaz — her uca gerçekçi yanıt.
            var cevap = yol switch
            {
                var y when y.Contains("/tercihler/benim", StringComparison.Ordinal) =>
                    $$"""{"tercihler":{{tercihler()}},"mesaj":"Tercihler kaydedildi"}""",
                var y when y.Contains("/yetkiler/benim", StringComparison.Ordinal) =>
                    $$"""{"moduller":{{moduller()}}}""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    public MenuKatlamaTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(
            new SahteApi(istekler, () => moduller, () => tercihler, () => yazmaPatlasin))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<MenuTercihServisi>();
    }

    private IRenderedComponent<KenarCubugu> Ciz(
        string rol = "SAHIP", string? mod = null, string? rota = null)
    {
        if (rota is not null)
            Services.GetRequiredService<NavigationManager>().NavigateTo(rota);
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", rol, "s1", null,
                                mod ?? ArayuzModuServisi.Profesyonel));
        var e = Render<KenarCubugu>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kenar-nav a")));
        return e;
    }

    private static bool Katli(IRenderedComponent<KenarCubugu> e, string slug) =>
        e.Find($"#kenar-grup-dugme-{slug}").GetAttribute("aria-expanded") == "false";

    // ── (2) VARSAYILAN: YALNIZ AKTİF ROTANIN GRUBU AÇIK ─────────────────────

    [Fact]
    public void Varsayilan_olarak_YALNIZ_aktif_rotanin_grubu_aciktir()
    {
        // bUnit'in başlangıç rotası "/" → Panel → "Günlük iş" grubu.
        var e = Ciz();

        Assert.False(Katli(e, "gunluk"));
        foreach (var slug in new[] { "musteri-stok", "para-banka", "belge-uyum", "rapor", "yonetim" })
            Assert.True(Katli(e, slug), $"{slug} varsayılan olarak KAPALI olmalıydı");
    }

    [Fact]
    public void Aktif_rota_degisince_ACIK_grup_da_degisir()
    {
        var e = Ciz(rota: "/kasalar");

        Assert.False(Katli(e, "para-banka"));
        Assert.True(Katli(e, "gunluk"));
    }

    [Fact]
    public void Alt_rota_dogru_grubu_acar()
    {
        // "/raporlar/kar-zarar" en UZUN eşleşmeyi seçmeli (/raporlar değil).
        var e = Ciz(rota: "/raporlar/kar-zarar");

        Assert.False(Katli(e, "rapor"));
        Assert.True(Katli(e, "yonetim"));
    }

    [Fact]
    public void Yetkisiz_rotada_o_rotanin_grubu_ACILMAZ()
    {
        // TEZGAHTAR /gider'i göremez (rol kısıtlı). Adres çubuğuna yazıp gelse
        // bile "aktif grup" o GÖRÜNMEYEN ekrandan türetilmez: menü, kullanıcının
        // göremediği bir ekranı işaret ederek grup açmaz (M-UX3-06 yansıması).
        var e = Ciz(rol: "TEZGAHTAR", rota: "/gider");

        Assert.NotNull(e.Find("#kenar-grup-dugme-para-banka"));   // grup var (kasalar görünür)
        Assert.True(Katli(e, "para-banka"));
        Assert.Empty(e.FindAll("a[href='/gider']"));
    }

    // ── (1) KAPALI GRUP HİÇBİR EKRANI KAYBETMEZ ─────────────────────────────

    [Fact]
    public void Kapali_grubun_onizlemesi_o_gruptaki_TUM_ekran_adlarini_yazar()
    {
        var e = Ciz();
        var tr = Sozlukler.Getir("tr");

        var onizleme = e.Find("#kenar-onizleme-musteri-stok").TextContent;
        foreach (var oge in MenuTanimi.Ogeler.Where(o => o.Grup == MenuGrubu.MusteriStok))
            Assert.Contains(oge.Etiket(tr), onizleme, StringComparison.Ordinal);

        // Tuvaldeki ayraç: "Müşteriler · Ürünler · …"
        Assert.Contains(MenuSozlugu.OnizlemeAyraci.Trim(), onizleme, StringComparison.Ordinal);
    }

    [Fact]
    public void Kapali_grubun_baglantilari_SILINMEZ_yalnizca_hidden_olur()
    {
        var e = Ciz();

        // Bağlantı DOM'da duruyor (menü kapsamı daralmadı)…
        var yollar = e.FindAll(".kenar-nav a").Select(a => a.GetAttribute("href")).ToList();
        Assert.Contains("/denetim-izi", yollar);
        Assert.Contains("/urunler/agac", yollar);

        // …ama satırı katlanmış.
        Assert.True(e.Find("#kenar-satir-denetim-izi").HasAttribute("hidden"));
        // Açık grubun satırı katlanmamış.
        Assert.False(e.Find("#kenar-satir-islemler").HasAttribute("hidden"));
    }

    [Fact]
    public void Acik_grupta_onizleme_satiri_CIZILMEZ()
    {
        var e = Ciz();

        Assert.Empty(e.FindAll("#kenar-onizleme-gunluk"));
        Assert.NotEmpty(e.FindAll("#kenar-onizleme-rapor"));
    }

    // ── KATLAMA ETKİLEŞİMİ + SUNUCUYA YAZMA ─────────────────────────────────

    [Fact]
    public void Grup_basligina_basinca_grup_acilir_ve_TERCIH_sunucuya_yazilir()
    {
        var e = Ciz();
        Assert.True(Katli(e, "rapor"));

        e.Find("#kenar-grup-dugme-rapor").Click();

        e.WaitForAssertion(() => Assert.False(Katli(e, "rapor")));
        e.WaitForAssertion(() => Assert.Contains(istekler, i =>
            i.Metot == "PATCH"
            && i.Yol.Contains("/tercihler/benim", StringComparison.Ordinal)
            && i.Govde.Contains(MenuTercihServisi.AnahtarAcikGruplar, StringComparison.Ordinal)
            && i.Govde.Contains("rapor", StringComparison.Ordinal)));
    }

    [Fact]
    public void Acik_gruba_basinca_KAPANIR_ve_diger_acik_gruplar_kapanmaz()
    {
        var e = Ciz();
        e.Find("#kenar-grup-dugme-rapor").Click();          // rapor açıldı
        e.WaitForAssertion(() => Assert.False(Katli(e, "rapor")));

        e.Find("#kenar-grup-dugme-rapor").Click();          // rapor kapandı

        e.WaitForAssertion(() => Assert.True(Katli(e, "rapor")));
        // İlk katlamada küme "o an açık olanlar" ile tohumlandığı için
        // aktif rotanın grubu AÇIK kalmalı (aksi hâlde tek tıkta menü kapanırdı).
        Assert.False(Katli(e, "gunluk"));
    }

    [Fact]
    public void Kayitli_tercih_OTEKI_gruplari_yonetir_AKTIF_grup_yine_ACIK()
    {
        // KATMAN-1b / KIYAS 28.09 bulgu 2a: eski kural "kayıtlı tercih aktif grubu
        // da ezer" idi → bulunulan ekranın grubu kapalı çiziliyordu. Yeni kural:
        // kayıtlı tercih yalnız ÖTEKİ grupları yönetir.
        tercihler = $$"""{"{{MenuTercihServisi.AnahtarAcikGruplar}}":"[\"yonetim\"]"}""";
        var e = Ciz();

        Assert.False(Katli(e, "yonetim"));
        Assert.False(Katli(e, "gunluk"));  // aktif rotanın grubu tercihte yok ama AÇIK
        Assert.True(Katli(e, "rapor"));
    }

    [Fact]
    public void KIYAS2a_tercih_para_banka_iken_faturalarda_BELGE_UYUM_acik_para_banka_tercihle_acik()
    {
        tercihler = $$"""{"{{MenuTercihServisi.AnahtarAcikGruplar}}":"[\"para-banka\"]"}""";
        var e = Ciz(rota: "/faturalar");

        Assert.False(Katli(e, "belge-uyum"));   // bulunulan ekranın grubu AÇIK
        Assert.False(Katli(e, "para-banka"));   // kullanıcının açık bıraktığı grup açık
        Assert.True(Katli(e, "gunluk"));
    }

    [Fact]
    public void KIYAS2a_aktif_grup_bu_ziyarette_katlanir_TERCIHE_YAZILMAZ_rota_degisince_yine_acik()
    {
        var e = Ciz(rota: "/kasalar");
        Assert.False(Katli(e, "para-banka"));

        e.Find("#kenar-grup-dugme-para-banka").Click();

        e.WaitForAssertion(() => Assert.True(Katli(e, "para-banka")));
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");   // kalıcı tercihe yazılmadı

        Services.GetRequiredService<NavigationManager>().NavigateTo("/banka");
        e.WaitForAssertion(() => Assert.False(Katli(e, "para-banka")));
    }

    [Fact]
    public void KIYAS2a_oteki_grubu_katlayinca_AKTIF_grup_TOHUMA_girmez()
    {
        var e = Ciz(rota: "/kasalar");

        e.Find("#kenar-grup-dugme-rapor").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH"
            && i.Govde.Contains("rapor", StringComparison.Ordinal)));
        var yazma = istekler.Last(i => i.Metot == "PATCH").Govde;
        Assert.DoesNotContain("para-banka", yazma, StringComparison.Ordinal);
    }

    [Fact]
    public void Yazma_basarisizsa_katlama_GERI_ALINIR_ve_bant_cizilir()
    {
        yazmaPatlasin = true;
        var e = Ciz();

        e.Find("#kenar-grup-dugme-rapor").Click();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kenar-sira-hatasi")));
        Assert.True(Katli(e, "rapor"));    // "oldu" denmedi — geri alındı
        // Bant NEYİN kaydedilemediğini söyler: katlama hatası "sıra" diye yazılmaz.
        Assert.Contains(MenuSozlugu.KatlamaKaydedilemedi("tr"),
            e.Find("#kenar-sira-hatasi").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Katlama_SIFIRLA_dugmesi_yalniz_kayitli_tercih_varken_cikar_ve_DELETE_atar()
    {
        var e = Ciz();
        Assert.Empty(e.FindAll("#kenar-katlama-sifirla"));   // silinecek tercih yok → düğme yok

        e.Find("#kenar-grup-dugme-rapor").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kenar-katlama-sifirla")));

        e.Find("#kenar-katlama-sifirla").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler, i =>
            i.Metot == "DELETE"
            && i.Yol.EndsWith("/tercihler/benim/" + MenuTercihServisi.AnahtarAcikGruplar, StringComparison.Ordinal)));
        // Varsayılana döndü: yalnız aktif rotanın grubu açık, rapor yine kapalı.
        e.WaitForAssertion(() => Assert.True(Katli(e, "rapor")));
        Assert.Empty(e.FindAll("#kenar-katlama-sifirla"));
    }

    // ── (3) MOD YETKİYİ GEVŞETMEZ (M-UX3-06) ────────────────────────────────

    [Fact]
    public void Yetkisiz_ekran_KAPALI_grupta_da_onizlemede_de_yoktur()
    {
        moduller = "[]";
        var e = Ciz(rol: "TEZGAHTAR");

        var yollar = e.FindAll(".kenar-nav a").Select(a => a.GetAttribute("href")).ToList();
        Assert.DoesNotContain("/urunler", yollar);
        // Grup hiç çizilmediği için önizlemesi de yok.
        Assert.Empty(e.FindAll("#kenar-onizleme-musteri-stok"));
        Assert.DoesNotContain("Ürünler", e.Find("#kenar-nav").TextContent, StringComparison.Ordinal);
    }

    // ── BASİT MOD: TUVAL — 6 KATLANIR GRUP (S1-g) ───────────────────────────
    //
    // S1-g YENİDEN YAZIM (kurucu 24.09: "menüyü kontrol et, olmamış"):
    //   ESKİ: Basit_modda_grup_basligi_CIZILMEZ_ve_satirlar_katlanmaz — basit
    //         modda başlık 0, satır katlanmaz, menü = BasitMenu (8 satır).
    //   YENİ: tuval Panel.dc.html satır 32-43 (basit yüz) ve Tezgah.dc.html
    //         satır 33-45 (tezgâhtar) basit modda da 6 katlanır grup çizer:
    //         aktif rotanın grubu açık, diğerleri kapalı + önizleme satırı;
    //         canvas.json notu n2 "Menüde HİÇBİR ekran kaybolmadı".

    [Fact]
    public void Basit_modda_da_ALTI_grup_basligi_cizilir_ve_kapali_gruplar_katlanir()
    {
        var e = Ciz(mod: ArayuzModuServisi.Basit, rota: "/");

        Assert.Equal(6, e.FindAll(".kenar-nav .kenar-grup-dugme").Count);
        Assert.False(Katli(e, "gunluk"));
        foreach (var slug in new[] { "musteri-stok", "para-banka", "belge-uyum", "rapor", "yonetim" })
        {
            Assert.True(Katli(e, slug), $"{slug} basit modda da KAPALI başlamalı");
            Assert.NotEmpty(e.FindAll($"#kenar-onizleme-{slug}"));
        }
        // Hiçbir ekran DOM'dan kaybolmaz: 38 bağlantının hepsi durur…
        Assert.Equal(MenuTanimi.Ogeler.Count, e.FindAll(".kenar-nav a").Count);
        // …ama yalnız AÇIK grubun satırları katlanmamış (Panel rotasında 5).
        var acikSatir = e.FindAll(".kenar-satir[data-sira-anahtar]").Count(s => !s.HasAttribute("hidden"));
        Assert.Equal(MenuTanimi.Ogeler.Count(o => o.Grup == MenuGrubu.Gunluk), acikSatir);
        Assert.Equal(5, acikSatir);
    }

    // ── "TÜM EKRANLAR" GİRİŞİ ───────────────────────────────────────────────

    [Fact]
    public void Tum_ekranlar_dugmesi_GORUNUR_EKRAN_SAYISINI_yazar()
    {
        var e = Ciz();
        var gorunur = e.FindAll(".kenar-nav a")
            .Select(a => a.GetAttribute("href")).Distinct().Count();

        Assert.Contains($"({gorunur})", e.Find("#kenar-tum-ekranlar").TextContent,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Tum_ekranlar_girisi_ARTIK_KARO_SAYFASINA_giden_BAGLANTIDIR()
    {
        // S1-b bu girişi hedef sayfa yokken KİLİTLİ düğme olarak koymuştu
        // (DoD EK md.1). S1-d /ekranlar'ı açtı: giriş gerçek bir bağlantıdır,
        // id'ler (#kenar-tum-ekranlar, #kenar-tum-ekranlar-sebep) AYNEN yaşar.
        var e = Ciz();
        var d = e.Find("#kenar-tum-ekranlar");

        Assert.Equal("a", d.TagName, ignoreCase: true);
        Assert.Equal("/ekranlar", d.GetAttribute("href"));
        Assert.False(d.HasAttribute("disabled"));
        Assert.Equal(MenuSozlugu.TumEkranlarAciklama("tr"), d.GetAttribute("title"));
        Assert.Equal(MenuSozlugu.TumEkranlarAciklama("tr"),
            e.Find("#kenar-tum-ekranlar-sebep").TextContent.Trim());
        // Metin kullanıcıya iç paket kodu (jargon) söylemez (plan § 5B).
        Assert.DoesNotContain("S1-", d.GetAttribute("title")!, StringComparison.Ordinal);
        // Giriş MENÜ bağlantısı sayılmamalı (mevcut testler .kenar-nav a sayıyor).
        Assert.Empty(e.FindAll("#kenar-nav #kenar-tum-ekranlar"));
    }

    [Fact]
    public void Tum_ekranlar_sayisi_BASIT_MODDA_da_YETKILI_ekran_sayisidir_mod_suzgeci_yok()
    {
        // Tuval Panel.dc.html (basit yüz) "Tüm ekranlar (38)" yazar: bağlantı
        // karo sayfasının karo sayısını söyler; mod yalnız MENÜYÜ daraltır.
        var e = Ciz(mod: ArayuzModuServisi.Basit);
        var beklenen = MenuTanimi.Yetkililer(_ => true, "SAHIP").Count;

        Assert.Equal(MenuTanimi.Ogeler.Count, beklenen);
        Assert.Contains($"({beklenen})", e.Find("#kenar-tum-ekranlar").TextContent,
            StringComparison.Ordinal);
        // S1-g YENİDEN YAZIM — ESKİ: "menünün kendisi YİNE daralmış"
        // (BasitMenu 8 satır). YENİ (tuval Panel.dc.html): basit modda da menü
        // yetkili HER ekranı taşır; sayı ile menü AYNI soruya aynı cevabı verir.
        Assert.Equal(beklenen, e.FindAll(".kenar-nav a").Count);
    }

    [Fact]
    public void Tum_ekranlar_sayisi_YETKISIZ_ekrani_SAYMAZ()
    {
        // M-UX3-06: TEZGÂHTAR'ın rol kapılı ekranları (gider, MASAK, işletme…)
        // ve yetkisi olmayan modüller sayıya girmez.
        moduller = """["cari","islem"]""";
        var e = Ciz(rol: "TEZGAHTAR");
        var beklenen = MenuTanimi.Yetkililer(m => m is "cari" or "islem", "TEZGAHTAR").Count;
        // Bağımsız sayım (süzgecin kendisine güvenmeden): modül VE rol kapısı.
        var bagimsiz = MenuTanimi.Ogeler.Count(o =>
            (o.Modul is null || o.Modul is "cari" or "islem")
            && (o.Roller.Length == 0 || Array.IndexOf(o.Roller, "TEZGAHTAR") >= 0));

        Assert.Equal(bagimsiz, beklenen);
        Assert.True(beklenen < MenuTanimi.Ogeler.Count);
        e.WaitForAssertion(() => Assert.Contains($"({beklenen})",
            e.Find("#kenar-tum-ekranlar").TextContent, StringComparison.Ordinal));
    }

    // ── KULLANICI KARTI + MARKA (tuval: "Mihenk" satırı, "Ramazan A. / Sahip") ─

    [Fact]
    public void Marka_satiri_TEK_KAYNAKTAN_gelir_eski_ad_yazilmaz()
    {
        var e = Ciz();
        var marka = e.Find("#kenar-marka").TextContent.Trim();

        // T-K2: tuval yazımı "Mihenk" — Marka.Ad'dan (tek kaynak) TÜRETİLİR.
        Assert.Equal(KabukTuvalSozlugu.MarkaYazimi, marka);
        Assert.Equal("Mihenk", marka);
        Assert.Equal(Marka.Ad, marka.ToUpper(new System.Globalization.CultureInfo("tr-TR")));
        Assert.DoesNotContain("Kuyumcu", marka, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("SAHIP")]
    [InlineData("MUDUR")]
    [InlineData("MUHASEBE")]
    [InlineData("TEZGAHTAR")]
    public void Kullanici_kartinda_rol_SOZLUKTEN_cevrilir_ham_kod_basilmaz(string rol)
    {
        var e = Ciz(rol);
        var d = Services.GetRequiredService<DilServisi>();
        var beklenen = rol switch
        {
            "SAHIP" => d.T.Kullanici.RolSahip,
            "MUDUR" => d.T.Kullanici.RolMudur,
            "MUHASEBE" => d.T.Kullanici.RolMuhasebe,
            _ => d.T.Kullanici.RolTezgahtar,
        };
        var yazilan = e.Find("#kenar-kullanici-rol").TextContent.Trim();

        Assert.Equal(beklenen, yazilan);
        Assert.NotEqual(rol, yazilan); // UX-S31: ham veritabanı kodu ekrana basılmaz
        Assert.Contains("RA", e.Find(".kenar-avatar").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Taninmayan_rol_TAHMIN_EDILMEZ_bos_kalir()
    {
        var e = Ciz("BILINMEYEN");
        Assert.Equal("", e.Find("#kenar-kullanici-rol").TextContent.Trim());
    }

    // ── ERİŞİLEBİLİRLİK + MENÜDE KALICI PARAGRAF YOK (P-37 gerilemesi) ──────

    [Fact]
    public void Grup_basligi_ARIA_EXPANDED_tasir_ve_menude_paragraf_yoktur()
    {
        var e = Ciz();

        foreach (var slug in MenuTanimi.GrupSirasi.Select(MenuTanimi.GrupSlug))
        {
            var d = e.Find($"#kenar-grup-dugme-{slug}");
            Assert.Contains(d.GetAttribute("aria-expanded"), new[] { "true", "false" });
            Assert.False(string.IsNullOrWhiteSpace(d.GetAttribute("aria-label")));
        }
        Assert.Empty(e.FindAll("#kenar-nav p"));
    }

    // ── SUNUCU SÖZLEŞMESİ ───────────────────────────────────────────────────

    [Fact]
    public void Katlama_anahtari_sunucunun_BEYAZ_LISTESINDEDIR()
    {
        // İstemci anahtarı ile sunucunun beyaz listesi AYNI dizgedir; biri
        // değişirse PATCH 400 döner ve katlama sessizce çalışmaz olurdu.
        Assert.Equal(TercihAnahtarlari.MenuAcikGruplar, MenuTercihServisi.AnahtarAcikGruplar);
        Assert.Contains(TercihAnahtarlari.MenuAcikGruplar, TercihAnahtarlari.Tumu);
    }

    [Fact]
    public void Menu_sozlugu_IKI_DILDE_doludur()
    {
        foreach (var dil in new[] { "tr", "en" })
        {
            Assert.False(string.IsNullOrWhiteSpace(MenuSozlugu.TumEkranlar(dil, 38)));
            Assert.False(string.IsNullOrWhiteSpace(MenuSozlugu.TumEkranlarAciklama(dil)));
            Assert.False(string.IsNullOrWhiteSpace(MenuSozlugu.GrubuAc(dil, "X")));
            Assert.False(string.IsNullOrWhiteSpace(MenuSozlugu.GrubuKapat(dil, "X")));
            Assert.False(string.IsNullOrWhiteSpace(MenuSozlugu.KatlamaIpucu(dil)));
            Assert.False(string.IsNullOrWhiteSpace(MenuSozlugu.OnizlemeEtiketi(dil)));
        }
        Assert.NotEqual(MenuSozlugu.KatlamaIpucu("tr"), MenuSozlugu.KatlamaIpucu("en"));
    }
}
