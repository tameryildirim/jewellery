using System.Net;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET UX-1-C4 — "her veri girilen yer düzeltilebilir", sayfa grubu 4
//   /veri-disa-aktarim · /kullanicilar · /yetkiler · /isletme · /ayarlar · /kilavuz
//   (+ 45-UI: Ayarlar'daki iki aşamalı giriş ve /giris ikinci adımı)
//
// ÖLÇÜLEN ŞEY "metin var mı" DEĞİL:
//   • SayfaBasi GERÇEKTEN çiziliyor mu, başlığı dolu mu (6 sayfa)
//   • "Düzenle → gövde PATCH'e gider": gönderilen GÖVDE sınanır, istek sayısı değil
//   • Düzenlenemeyen kayıtta DÜĞME YOK ama GEREKÇE VAR (UX-S25) — negatif iddia
//     pozitif eşiyle çiftlenir
//   • Kilitli düğme SEBEBİNİ söylüyor mu (UX-S30)
//   • Giz DOM'a MASKESİZ basılmıyor; kurtarma kodları onaylanmadan "Tamamla" kapalı
//
// Bu dosya MEVCUT testlerin hiçbirini değiştirmez; yalnız EKLER (K4).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UX1C4EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    /// <summary>Yol → gönderilen Authorization başlığı (45-UI jeton sözleşmesi).</summary>
    private readonly Dictionary<string, string?> basliklar = [];
    private string mfaDurum = """{"etkin":false,"durum":null,"yontem":null,"kalanKurtarmaKodu":0,"etkinlesti":null,"kilitli":false}""";
    private bool mfaGerekliGiris;

    public UX1C4EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, basliklar, () => mfaDurum, () => mfaGerekliGiris))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<SurumServisi>();   // paket T-4a: Ayarlar künye kartı
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<GorunumServisi>();      // P-36b: Ayarlar Görünüm bölümü
        Services.AddScoped<KullaniciGorunumServisi>();  // B-1: Ayarlar "Benim görünümüm" kartı
        // Oturum kullanıcısı: kendi satırı (#u1) ve rol hiyerarşisi bu kimliğe bakar.
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "QA", "Test", "qa@ornek.com", "SAHIP", "s1"));
    }

    private static void SayfaBasiVar(
        IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> c, string anahtar)
    {
        Assert.NotNull(c.Find($"#sayfa-basi-{anahtar}"));
        Assert.False(string.IsNullOrWhiteSpace(
            c.Find($"#sayfa-basi-{anahtar}-baslik").TextContent.Trim()));
    }

    /// <summary>Gerekçe metni kullanıcıya iç paket/dalga kodu SIZDIRMAZ.</summary>
    private static void GerekceDurust(string metin)
    {
        Assert.False(string.IsNullOrWhiteSpace(metin));
        Assert.DoesNotContain("paket", metin, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dalga", metin, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADR-", metin, StringComparison.Ordinal);
    }

    private (string Metot, string Yol, string Govde) Son(string metot, string parca) =>
        istekler.Last(i => i.Metot == metot && i.Yol.Contains(parca));

    /// <summary>Gövdedeki alanın metin değeri (yoksa null).</summary>
    private static string? Alan(string govde, string ad)
    {
        var e = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    // ── 1) Sayfa başı: altı sayfanın hepsinde ────────────────────────────────

    [Fact]
    public void C4_01_Veri_disa_aktarim_sayfa_basi_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.VeriDisaAktarim>();
        c.WaitForAssertion(() => SayfaBasiVar(c, "veri-disa-aktarim"));
    }

    [Fact]
    public void C4_02_Kullanicilar_sayfa_basi_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.Kullanicilar>();
        c.WaitForAssertion(() => SayfaBasiVar(c, "kullanicilar"));
    }

    [Fact]
    public void C4_03_Yetkiler_sayfa_basi_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.Yetkiler>();
        c.WaitForAssertion(() => SayfaBasiVar(c, "yetkiler"));
    }

    [Fact]
    public void C4_04_Isletme_sayfa_basi_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.Isletme>();
        c.WaitForAssertion(() => SayfaBasiVar(c, "isletme"));
    }

    // PAKET T-K3 (tuval Ayarlar.dc.html · kurucu 24.09 17:20): Ayarlar'ın sayfa başı
    // bloğu KALKTI — başlık üst barda (UstBar <h1>), sekmeler başlığın yanında.
    // Yazılı muafiyet dosyada (SAYFA_BASI_MUAFIYETI); "Profili düzenle" profil kartında.
    [Fact]
    public void C4_05_Ayarlar_sayfa_basi_TUVAL_geregi_yok_profil_duzenle_yasar()
    {
        var c = Render<Kuyumcu.Web.Pages.Ayarlar>();
        Assert.Empty(c.FindAll("#sayfa-basi-ayarlar"));
        Assert.NotNull(c.Find("#ayar-profil #ay-profil-duzenle"));
    }

    [Fact]
    public void C4_06_Kilavuz_sayfa_basi_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.Kilavuz>();
        c.WaitForAssertion(() => SayfaBasiVar(c, "kilavuz"));
        // Kılavuz iframe'e GERİ DÖNMEZ (CSP frame-ancestors 'none'); tek yol
        // yeni sekme bağlantısıdır ve o bağlantı ekranda DURUR.
        Assert.Empty(c.FindAll("iframe"));
        Assert.NotNull(c.Find("#kl-yeni-sekme"));
    }

    // ── 2) İşletme kimliği: Düzenle → gövde PATCH'e gider ────────────────────

    [Fact]
    public void C4_10_Isletme_kimligi_cekmeceden_PATCH_edilir()
    {
        var c = Render<Kuyumcu.Web.Pages.Isletme>();
        c.WaitForElement("#i-kimlik-duzenle").Click();

        c.WaitForElement("#cekmece-isletme-kimlik");           // K6 çekmece sözleşmesi
        c.Find("#ikimlik-ad").Input("QA-TEST Kuyumculuk A.Ş.");
        c.Find("#ikimlik-adres").Input("Kapalıçarşı No:1");
        c.Find("#ikimlik-vergino").Input("1234567890");
        c.Find("#cekmece-isletme-kimlik-kaydet").Click();

        var istek = Son("PATCH", "/admin/tenant/kimlik");
        Assert.Contains("\"ad\":\"QA-TEST Kuyumculuk A.\\u015E.\"", istek.Govde);
        Assert.Contains("Kapal", istek.Govde);
        Assert.Contains("\"vergiNo\":\"1234567890\"", istek.Govde);
    }

    [Fact]
    public void C4_11_Isletme_adi_bosken_kaydet_KILITLI_ve_sebebi_yazili()
    {
        var c = Render<Kuyumcu.Web.Pages.Isletme>();
        c.WaitForElement("#i-kimlik-duzenle").Click();
        c.WaitForElement("#ikimlik-ad").Input("   ");

        Assert.True(c.Find("#cekmece-isletme-kimlik-kaydet").HasAttribute("disabled"));
        // UX-S30: kilidin SEBEBİ ekranda yazar.
        Assert.False(string.IsNullOrWhiteSpace(
            c.Find("#cekmece-isletme-kimlik-kilit").TextContent.Trim()));
        Assert.NotNull(c.Find("#ikimlik-ad-hata"));
        // Sessiz etkisizlik yok ama YAZMA da yok.
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH" && i.Yol.Contains("tenant/kimlik"));
    }

    [Fact]
    public void C4_12_Vergi_no_hatali_bicimde_kaydet_KILITLI()
    {
        var c = Render<Kuyumcu.Web.Pages.Isletme>();
        c.WaitForElement("#i-kimlik-duzenle").Click();
        c.WaitForElement("#ikimlik-vergino").Input("12345");

        Assert.True(c.Find("#cekmece-isletme-kimlik-kaydet").HasAttribute("disabled"));
        Assert.NotNull(c.Find("#ikimlik-vergino-hata"));
    }

    [Fact]
    public void C4_13_Pasif_sube_ARTIK_AKTIFE_ALINIR_ve_PATCH_aktif_true_gonderir()
    {
        // ⚠ PAKET S-3b: eskiden "uç yok, düğme de olmasın" doğruydu. Şube PATCH'i
        // artık `aktif` alanını taşıyor; UX-S25 gereği düğme AÇILDI ve ölü
        // "değişmez" ipucu kaldırıldı (kurul tur 3 · /isletme, YÜKSEK).
        var c = Render<Kuyumcu.Web.Pages.Isletme>();
        c.WaitForState(() => c.FindAll("[id^=s-aktife-]").Count > 0);

        var dugme = c.FindAll("[id^=s-aktife-]").First();
        Assert.False(dugme.HasAttribute("disabled"));
        dugme.Click();

        var patch = istekler.Last(x => x.Metot == "PATCH");
        Assert.Contains("/subeler/", patch.Yol);
        Assert.Equal("True", Alan(patch.Govde, "aktif"), ignoreCase: true);
    }

    /// <summary>
    /// ⚠ SÖZLEŞME DEĞİŞTİ — PAKET P-13 (B-47). Bu test eskiden kur kaynağı ve
    /// maliyet yöntemi için bir DEĞİŞMEZLİK GEREKÇESİ arıyordu. Gerekçe DOĞRU
    /// DEĞİLDİ: kur kaynağının yazma ucu zaten vardı, maliyet yönteminin hiç
    /// yoktu — yani "türetilir" denen ayar aslında hiç sorulmuyordu. İkisi de
    /// artık seçilebilir; doğru sözleşme "gerekçe var" değil, "KİLİT YOK ve
    /// belirlenmemişse hangi değerin kullanıldığı yazılı"dır (UX-S21/UX-S25).
    /// </summary>
    [Fact]
    public void C4_14_Kur_kaynagi_ve_maliyet_yontemi_SECILEBILIR()
    {
        var c = Render<Kuyumcu.Web.Pages.Isletme>();
        var kur = c.WaitForElement("#i-kur-kaynagi");
        var maliyet = c.Find("#i-maliyet-yontemi");

        Assert.False(kur.HasAttribute("disabled"));
        Assert.False(maliyet.HasAttribute("disabled"));
        // Belirlenmemiş ayar sessiz kalmaz.
        Assert.NotNull(c.Find("#i-kur-kaynagi-not"));
        Assert.NotNull(c.Find("#i-maliyet-yontemi-not"));
    }

    [Fact]
    public void C4_15_Sube_cekmecesi_telefonu_DOGRULAR()
    {
        var c = Render<Kuyumcu.Web.Pages.Isletme>();
        c.WaitForElement("#s-yeni-sube-ac");
        c.Find("#s-yeni-sube-ac").Click();

        c.WaitForElement("#cekmece-sube");
        c.Find("#s-ad").Input("QA-TEST Şube");
        // GIRDI-AILESI-2 (kurucu 08.10): telefona harf YAZILAMAZ (maske); geçersiz numara rakamla denenir.
        c.Find("#s-telefon").Input("abc");
        Assert.Equal("", c.Find("#s-telefon").GetAttribute("value"));
        c.Find("#s-telefon").Input("123");
        // GIRDI-AILESI-2 tur 3: eksik numara alandan çıkınca hata + kilit (kural ortak Telefon bileşeninde,
        // sayfada ikinci kural yok; kilit metni = alan altı metni).
        c.Find("#s-telefon").Change("123");

        Assert.NotNull(c.Find("#s-telefon-hata"));
        Assert.True(c.Find("#cekmece-sube-kaydet").HasAttribute("disabled"));

        c.Find("#s-telefon").Input("0532 111 22 33");
        Assert.False(c.Find("#cekmece-sube-kaydet").HasAttribute("disabled"));
        c.Find("#cekmece-sube-kaydet").Click();

        var istek = Son("POST", "/subeler");
        Assert.Contains("QA-TEST", istek.Govde);
    }

    // ── 3) Kullanıcılar ──────────────────────────────────────────────────────

    [Fact]
    public void C4_20_Kullanici_listesinde_MFA_sutunu_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.Kullanicilar>();
        c.WaitForAssertion(() => Assert.Contains("İki aşamalı", c.Markup));
        // Giz/kurtarma kodu ekrana GELMEZ.
        Assert.DoesNotContain("gizBase32", c.Markup);
    }

    [Fact]
    public void C4_21_Yeni_kullanici_gecersiz_epostada_alan_alti_mesaj_verir()
    {
        var c = Render<Kuyumcu.Web.Pages.Kullanicilar>();
        c.WaitForElement("#k-yeni").Click();
        c.Find("#k-eposta").Input("gecersiz-eposta");

        Assert.NotNull(c.Find("#k-eposta-hata"));                 // sessiz etkisizlik YASAK
        Assert.True(c.Find("#k-kaydet").HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(c.Find("#k-kaydet-kilit").TextContent.Trim()));
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/kullanicilar"));
    }

    [Fact]
    public void C4_22_Kendi_satirinda_rol_alani_YOK_gerekce_VAR()
    {
        var c = Render<Kuyumcu.Web.Pages.Kullanicilar>();
        c.WaitForElement("button.btn-ikincil");
        // Kendi satırının Düzenle düğmesi (fikstürde oturum kullanıcısı = u1).
        c.FindAll("button").First(b => b.TextContent.Trim() == "Düzenle").Click();

        c.WaitForElement("#degismez-ipucu-KendiRolum");
        Assert.Empty(c.FindAll("#d-rol"));                        // rol seçici YOK
        GerekceDurust(c.Find("#degismez-ipucu-KendiRolum").TextContent);
    }

    // ── 4) Yetkiler ──────────────────────────────────────────────────────────

    [Fact]
    public void C4_30_Yetkiler_degisiklik_yokken_kaydet_KILITLI_sebebi_yazili()
    {
        var c = Render<Kuyumcu.Web.Pages.Yetkiler>();
        c.WaitForElement("#y-kaydet");

        Assert.True(c.Find("#y-kaydet").HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(c.Find("#y-kaydet-kilit").TextContent.Trim()));
        Assert.DoesNotContain(istekler, i => i.Metot == "PUT");
    }

    [Fact]
    public void C4_31_Yetki_degisince_kaydet_acilir_ve_matris_PUT_edilir()
    {
        var c = Render<Kuyumcu.Web.Pages.Yetkiler>();
        c.WaitForElement("#y-kaydet");

        // SAHİP satırı kilitli olduğu için ilk DEĞİŞTİRİLEBİLİR kutu seçilir.
        var kutu = c.FindAll("input[type=checkbox]").First(k => !k.HasAttribute("disabled"));
        kutu.Change(false);

        Assert.NotNull(c.Find("#y-kirli"));                       // kaydedilmemiş değişiklik uyarısı
        Assert.False(c.Find("#y-kaydet").HasAttribute("disabled"));
        c.Find("#y-kaydet").Click();

        var istek = Son("PUT", "/yetkiler");
        Assert.Contains("matris", istek.Govde);
        // Sunucu onayından sonra "kirli" damgası kalkar.
        c.WaitForAssertion(() => Assert.Empty(c.FindAll("#y-kirli")));
    }

    [Fact]
    public void C4_32_SAHIP_satiri_icin_gerekce_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.Yetkiler>();
        c.WaitForElement("#degismez-ipucu-SahipYetkisi");
        GerekceDurust(c.Find("#degismez-ipucu-SahipYetkisi").TextContent);
    }

    // ── 5) Veri dışa aktarım ────────────────────────────────────────────────

    [Fact]
    public void C4_40_Kisa_gerekcede_sayac_ve_alan_alti_mesaj_cikar()
    {
        var c = Render<Kuyumcu.Web.Pages.VeriDisaAktarim>();
        c.WaitForElement("#da-gerekce").Input("kısa gerekçe");

        Assert.Contains("/20", c.Find("#da-gerekce-sayac").TextContent);
        Assert.NotNull(c.Find("#da-gerekce-hata"));
        Assert.True(c.Find("#da-hazirla").HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(c.Find("#da-kilit-sebep").TextContent.Trim()));
    }

    [Fact]
    public void C4_41_Gecerli_gerekcede_dugme_acilir_ve_kilit_sebebi_kaybolur()
    {
        var c = Render<Kuyumcu.Web.Pages.VeriDisaAktarim>();
        c.WaitForElement("#da-gerekce").Input("Mali müşavire yıllık devir dosyası hazırlanıyor");

        Assert.Empty(c.FindAll("#da-gerekce-hata"));
        Assert.False(c.Find("#da-hazirla").HasAttribute("disabled"));
        Assert.Empty(c.FindAll("#da-kilit-sebep"));
    }

    [Fact]
    public void C4_42_Kisisel_veri_secilince_uyari_ve_gecmis_siniri_yazilidir()
    {
        var c = Render<Kuyumcu.Web.Pages.VeriDisaAktarim>();
        c.WaitForElement("#da-kisisel-not");                      // açılışta cari SEÇİLİ
        Assert.Contains("20", c.Find("#da-gecmis-siniri").TextContent);   // UX-S29
    }

    // ── 6) Ayarlar: profil + 45-UI ──────────────────────────────────────────

    [Fact]
    public void C4_50_Profil_cekmeceden_PATCH_edilir()
    {
        var c = Render<Kuyumcu.Web.Pages.Ayarlar>();
        c.WaitForElement("#ay-profil-duzenle").Click();

        c.WaitForElement("#cekmece-profil");
        c.Find("#pr-ad").Input("QA");
        c.Find("#pr-soyad").Input("Test");
        c.Find("#pr-eposta").Input("qa@ornek.com");
        c.Find("#cekmece-profil-kaydet").Click();

        var istek = Son("PATCH", "/kullanicilar/ben");
        Assert.Contains("\"ad\":\"QA\"", istek.Govde);
        Assert.Contains("\"soyad\":\"Test\"", istek.Govde);
        Assert.Contains("\"email\":\"qa@ornek.com\"", istek.Govde);
    }

    [Fact]
    public void C4_51_Profil_gecersiz_epostada_KILITLI()
    {
        var c = Render<Kuyumcu.Web.Pages.Ayarlar>();
        c.WaitForElement("#ay-profil-duzenle").Click();
        c.WaitForElement("#pr-eposta").Input("bozuk");

        Assert.NotNull(c.Find("#pr-eposta-hata"));
        Assert.True(c.Find("#cekmece-profil-kaydet").HasAttribute("disabled"));
    }

    [Fact]
    public void C4_52_Sifre_alanlari_bosken_guncelle_KILITLI_sebebi_yazili()
    {
        var c = Render<Kuyumcu.Web.Pages.Ayarlar>();
        c.WaitForElement("#ay-sifre-guncelle");
        Assert.True(c.Find("#ay-sifre-guncelle").HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(c.Find("#ay-sifre-kilit").TextContent.Trim()));
    }

    [Fact]
    public void C4_53_MFA_kurulumunda_giz_MASKELI_gosterilir()
    {
        var c = Render<Kuyumcu.Web.Pages.Ayarlar>();
        c.WaitForElement("#mfa-kur").Click();

        var anahtar = c.WaitForElement("#mfa-anahtar");
        // ⚠ Giz DOM'a MASKESİZ basılmaz.
        Assert.DoesNotContain("JBSWY3DPEHPK3PXP", anahtar.TextContent);
        Assert.Contains("•", anahtar.TextContent);

        // Göstermek KULLANICI isteğidir.
        c.Find("#mfa-anahtar-goster").Click();
        Assert.Contains("JBSWY3DPEHPK3PXP", c.Find("#mfa-anahtar").TextContent);
    }

    [Fact]
    public void C4_54_Kurtarma_kodlari_onaylanmadan_Tamamla_KAPALI()
    {
        var c = Render<Kuyumcu.Web.Pages.Ayarlar>();
        c.WaitForElement("#mfa-kur").Click();
        c.WaitForElement("#mfa-kod").Input("123456");
        c.Find("#mfa-dogrula").Click();

        c.WaitForElement("#mfa-kurtarma-liste");
        Assert.True(c.Find("#mfa-tamamla").HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(c.Find("#mfa-tamamla-kilit").TextContent.Trim()));

        c.Find("#mfa-kurtarma-onay").Change(true);
        Assert.False(c.Find("#mfa-tamamla").HasAttribute("disabled"));
    }

    [Fact]
    public void C4_55_MFA_kapatma_sifre_ve_kod_ister()
    {
        mfaDurum = """{"etkin":true,"durum":"ETKIN","yontem":"TOTP","kalanKurtarmaKodu":9,"etkinlesti":"2026-09-17T09:00:00Z","kilitli":false}""";
        var c = Render<Kuyumcu.Web.Pages.Ayarlar>();
        c.WaitForElement("#mfa-kapat-ac").Click();

        c.WaitForElement("#mfa-kapat-onayla");
        Assert.True(c.Find("#mfa-kapat-onayla").HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(c.Find("#mfa-kapat-kilit").TextContent.Trim()));

        c.Find("#mfa-kapat-sifre").Input("Parola12345");
        c.Find("#mfa-kapat-kod").Input("654321");
        c.Find("#mfa-kapat-onayla").Click();

        var istek = Son("POST", "/auth/mfa/kaldir");
        Assert.Contains("\"kod\":\"654321\"", istek.Govde);
    }

    [Fact]
    public void C4_56_MFA_etkinken_kalan_kurtarma_kodu_yazilidir()
    {
        mfaDurum = """{"etkin":true,"durum":"ETKIN","yontem":"TOTP","kalanKurtarmaKodu":9,"etkinlesti":null,"kilitli":false}""";
        var c = Render<Kuyumcu.Web.Pages.Ayarlar>();
        c.WaitForAssertion(() => Assert.Contains("9", c.Find("#mfa-kalan-kod").TextContent));
    }

    // ── 7) Giriş: ikinci adım ───────────────────────────────────────────────

    [Fact]
    public void C4_60_MFA_gerekliyse_giris_IKINCI_ADIMA_gecer()
    {
        mfaGerekliGiris = true;
        var c = Render<Kuyumcu.Web.Pages.Giris>();
        c.Find("#giris-eposta").Input("qa@ornek.com");
        c.Find("#giris-sifre").Input("Parola12345");
        c.Find("form").Submit();

        c.WaitForElement("#giris-mfa-kod");
        Assert.True(c.Find("#giris-mfa-dogrula").HasAttribute("disabled"));

        c.Find("#giris-mfa-kod").Input("123456");
        c.Find("#giris-mfa-dogrula").Click();

        var istek = Son("POST", "/auth/mfa/giris");
        Assert.Contains("\"kod\":\"123456\"", istek.Govde);
        // ⚠ SÖZLEŞME: meydan okuma jetonu GÖVDEDE DEĞİL, Authorization başlığında.
        // (Canlı doğrulamada yakalanan hata: gövdeye konunca sunucu 401 döner.)
        Assert.DoesNotContain("mfaToken", istek.Govde);
        Assert.Equal("Bearer meydan-okuma-jetonu",
            basliklar.Single(b => b.Key.EndsWith("/auth/mfa/giris")).Value);
    }

    // 08.10 (girdi envanteri bulgusu): kurtarma kodu 16 karakter (MfaKapilari.KurtarmaKoduUzunlugu),
    // alan 10'da kesiyordu → telefonunu kaybeden kullanıcı giremiyordu. Ayraçlı yazım (XXXX-XXXX-XXXX-XXXX = 19) da sığmalı.
    [Fact]
    public void C4_62_Giris_MFA_alani_KURTARMA_KODUNU_ayraciyla_birlikte_alir()
    {
        mfaGerekliGiris = true;
        var c = Render<Kuyumcu.Web.Pages.Giris>();
        c.Find("#giris-eposta").Input("qa@ornek.com");
        c.Find("#giris-sifre").Input("Parola12345");
        c.Find("form").Submit();
        c.WaitForElement("#giris-mfa-kod");
        var enFazla = int.Parse(c.Find("#giris-mfa-kod").GetAttribute("maxlength") ?? "0");
        Assert.True(enFazla >= Kuyumcu.Api.Modules.Auth.MfaKapilari.KurtarmaKoduUzunlugu + 3,
            $"giris-mfa-kod maxlength {enFazla} — 16 haneli kurtarma kodu (ayraçlı 19) sığmıyor");
    }

    [Fact]
    public void C4_63_Ayarlar_MFA_kapat_alani_KURTARMA_KODUNU_alir()
    {
        mfaDurum = """{"etkin":true,"durum":"ETKIN","yontem":"TOTP","kalanKurtarmaKodu":9,"etkinlesti":null,"kilitli":false}""";
        var c = Render<Kuyumcu.Web.Pages.Ayarlar>();
        c.WaitForElement("#mfa-kapat-ac").Click();
        c.WaitForElement("#mfa-kapat-kod");
        var enFazla = int.Parse(c.Find("#mfa-kapat-kod").GetAttribute("maxlength") ?? "0");
        Assert.True(enFazla >= Kuyumcu.Api.Modules.Auth.MfaKapilari.KurtarmaKoduUzunlugu + 3,
            $"mfa-kapat-kod maxlength {enFazla}");
    }

    // ── Sahte API ────────────────────────────────────────────────────────────

    private sealed class SahteApi(
        List<(string, string, string)> kayit,
        Dictionary<string, string?> basliklar,
        Func<string> mfaDurum,
        Func<bool> mfaGerekli) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var tam = istek.RequestUri!.PathAndQuery;
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, tam, govde));
            basliklar[yol] = istek.Headers.Authorization?.ToString();

            var g = yol switch
            {
                var y when y.EndsWith("/admin/tenant/kimlik") || y.EndsWith("/admin/tenant")
                        || y.EndsWith("/admin/plan") || y.EndsWith("/admin/ai") => TenantYaniti,
                var y when y.EndsWith("/subeler") => SubeListesi,
                var y when y.Contains("/subeler/") =>
                    """{"id":"s1","ad":"Merkez","adres":"Çarşı","telefon":"05321112233","aktif":true}""",
                var y when y.EndsWith("/kullanicilar") => KullaniciListesi,
                var y when y.Contains("/kullanicilar/") => """{"id":"u1"}""",
                var y when y.EndsWith("/yetkiler/benim") => """{"moduller":["cari","urun"]}""",
                var y when y.EndsWith("/yetkiler") => YetkiMatrisi,
                var y when y.EndsWith("/disa-aktarim/kaynaklar") => DisaAktarimKaynaklari,
                var y when y.Contains("/disa-aktarim/gecmis") => """{"toplam":0,"kayitlar":[]}""",
                var y when y.EndsWith("/auth/mfa/durum") => mfaDurum(),
                var y when y.EndsWith("/auth/mfa/kur") =>
                    """{"gizBase32":"JBSWY3DPEHPK3PXP","otpauthUri":"otpauth://totp/Kuyumcu:qa%40ornek.com?secret=JBSWY3DPEHPK3PXP&issuer=Kuyumcu&digits=6&period=30","hane":6,"adimSaniye":30}""",
                var y when y.EndsWith("/auth/mfa/dogrula") || y.EndsWith("/auth/mfa/kurtarma-yenile") =>
                    """{"etkin":true,"kurtarmaKodlari":["ABCD-EFGH-JKMN-PQRS","1234-5678-9ABC-DEFG"],"kalanKurtarmaKodu":2}""",
                var y when y.EndsWith("/auth/mfa/kaldir") => """{"etkin":false}""",
                var y when y.EndsWith("/auth/mfa/giris") => JetonYaniti,
                var y when y.EndsWith("/auth/giris") => mfaGerekli()
                    ? """{"mfaGerekli":true,"mfaToken":"meydan-okuma-jetonu","gecerlilikDakika":5}"""
                    : JetonYaniti,
                var y when y.EndsWith("/auth/ben") => KullaniciYaniti,
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"k1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-17T09:00:00Z","ayarFiyatlari":[]}""",
                // ⚠ B-1: "{}" varsayılanı bu uca YETMEZ (servis "yüklenmedi" sayar).
                var y when y.EndsWith("/kullanici/gorunum", StringComparison.Ordinal) =>
                    istek.Method == HttpMethod.Patch
                        ? B1SahteGorunumYaniti.Sarmal : B1SahteGorunumYaniti.Govde,
                var y when y.EndsWith("/gorunum", StringComparison.Ordinal) => B1SahteGorunumYaniti.Kiraci,
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }

        private const string TenantYaniti =
            """{"id":"t1","ad":"QA-TEST Kuyumculuk","plan":"BASLANGIC","aiEtkin":false,"varsayilanArayuzModu":"PROFESYONEL","adres":null,"telefon":null,"vergiDairesi":null,"vergiNo":null,"kurKaynagi":"ALTINKAYNAK","maliyetYontemi":"FIFO"}""";

        private const string SubeListesi =
            """[{"id":"s1","ad":"Merkez","adres":"Çarşı","telefon":"05321112233","aktif":true},{"id":"s2","ad":"Kadıköy","adres":null,"telefon":null,"aktif":true},{"id":"s3","ad":"Eski Şube","adres":null,"telefon":null,"aktif":false}]""";

        private const string KullaniciListesi =
            """[{"id":"u1","ad":"QA","soyad":"Test","email":"qa@ornek.com","rol":"SAHIP","aktif":true,"sonGiris":null,"arayuzModu":null,"subeId":"s1","mfaEtkin":false},{"id":"u2","ad":"Ayşe","soyad":"Yılmaz","email":"ayse@ornek.com","rol":"TEZGAHTAR","aktif":true,"sonGiris":null,"arayuzModu":null,"subeId":"s1","mfaEtkin":true}]""";

        private const string YetkiMatrisi =
            """{"moduller":["cari","urun","kasa"],"roller":["MUDUR","MUHASEBE","TEZGAHTAR"],"matris":{"MUDUR":["cari","urun","kasa"],"MUHASEBE":["cari"],"TEZGAHTAR":["urun"]}}""";

        private const string DisaAktarimKaynaklari =
            """[{"ad":"cari","kisiselVeri":true,"alanlar":["ad"]},{"ad":"urun","kisiselVeri":false,"alanlar":["ad"]}]""";

        private const string JetonYaniti =
            """{"accessToken":"a","refreshToken":"r","kullanici":{"id":"u1","tenantId":"t1","ad":"QA","soyad":"Test","email":"qa@ornek.com","rol":"SAHIP"}}""";

        private const string KullaniciYaniti =
            """{"id":"u1","tenantId":"t1","ad":"QA","soyad":"Test","email":"qa@ornek.com","rol":"SAHIP","arayuzModu":"PROFESYONEL"}""";
    }
}
