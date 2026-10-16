using Bunit;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// HAM GİRDİ GÖÇÜ — paket P-36c/3 (talimat 20/a'nın ikinci ayağı)
//
// NEDEN BU DOSYA VAR: P-36c'nin ikinci ayağı (ham <input>/<textarea> → ortak
// girdi ailesi) önceki turda HİÇ yapılmamıştı; sayaç 357'de duruyordu. Göç
// yapılırken ailenin ÜÇ yeni üyesi doğdu (Sifre · Arama · SaltOkunur) ve
// ailenin tabanına bir KÖPRÜ eklendi (FormDurumu + eski hata kimliği).
// Bu testler o dört yeniliğin SÖZLEŞMESİNİ kilitler; kilitlenmezse göç
// bir sonraki pakette sessizce bozulur.
//
// ⚠ Göçün kabulü "görünüyor" değil ÖLÇÜLEBİLİR davranıştır:
//   • eski eleman kimliği BİREBİR doğar (testler ve QA turları onu kullanır),
//   • hata mesajı eski kimliğiyle (alan-hata-x / özel kimlik) çizilir,
//   • yazılamayan değer GİRDİ DEĞİLDİR (ekran okuyucuya "alan" demez).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GirdiGocuTestleri : BunitContext
{
    public GirdiGocuTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    // ── 1) ŞİFRE: parola kutusudur ve tarayıcı ipucunu taşır ────────────────
    [Fact]
    public void GG1_Sifre_parola_tipi_ve_otomatik_tamamlama_ipucu()
    {
        var c = Render<Sifre>(p => p
            .Add(x => x.ElemanId, "t-sifre")
            .Add(x => x.Etiket, "Şifre")
            .Add(x => x.Zorunlu, true)
            .Add(x => x.OtoTamamla, "new-password"));

        var g = c.Find("#t-sifre");
        Assert.Equal("password", g.GetAttribute("type"));
        Assert.Equal("new-password", g.GetAttribute("autocomplete"));
        Assert.Equal("true", g.GetAttribute("aria-required"));
        Assert.NotNull(c.Find("#t-sifre-sar"));
    }

    // ── 2) ŞİFRE: değer TAŞINIR ama DOM'a yazılmaz diye sessiz kalmaz ───────
    [Fact]
    public void GG2_Sifre_yazilan_deger_cagirana_gider()
    {
        string? gelen = null;
        var c = Render<Sifre>(p => p
            .Add(x => x.ElemanId, "t-sifre")
            .Add(x => x.Etiket, "Şifre")
            .Add(x => x.DegerChanged, (string? v) => gelen = v));

        c.Find("#t-sifre").Input("Abc12345");
        Assert.Equal("Abc12345", gelen);
    }

    // ── 3) ARAMA: yazarken süzer (oninput), etiket DOM'da durur ─────────────
    [Fact]
    public void GG3_Arama_yazarken_suzer_ve_etiketi_DOMda_durur()
    {
        var kayit = new List<string?>();
        var c = Render<Arama>(p => p
            .Add(x => x.ElemanId, "t-ara")
            .Add(x => x.Etiket, "Müşteri ara")
            .Add(x => x.DegerChanged, (string? v) => kayit.Add(v)));

        var g = c.Find("#t-ara");
        Assert.Equal("search", g.GetAttribute("type"));
        // Etiket GÖRSEL OLARAK gizli ama DOM'da: ekran okuyucu alanı okur.
        Assert.Equal("t-ara", c.Find("label.gorsel-gizli").GetAttribute("for"));
        Assert.Contains("Müşteri ara", c.Find("label.gorsel-gizli").TextContent);

        g.Input("ay");
        Assert.Equal(["ay"], kayit);
    }

    // ── 4) ARAMA: Enter çağıranın işini tetikler (barkod okuyucu) ───────────
    [Fact]
    public void GG4_Arama_Enter_cagirani_tetikler_diger_tuslar_tetiklemez()
    {
        var sayac = 0;
        var c = Render<Arama>(p => p
            .Add(x => x.ElemanId, "t-ara")
            .Add(x => x.Etiket, "Barkod")
            .Add(x => x.Enter, () => sayac++));

        c.Find("#t-ara").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "a" });
        Assert.Equal(0, sayac);

        c.Find("#t-ara").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(1, sayac);
    }

    // ── 5) SALT OKUNUR: değer GİRDİ DEĞİLDİR, makine değeri okunabilir ──────
    [Fact]
    public void GG5_SaltOkunur_girdi_degildir_ve_makine_degeri_tasir()
    {
        var c = Render<SaltOkunur>(p => p
            .Add(x => x.ElemanId, "t-tl")
            .Add(x => x.Etiket, "TL karşılığı")
            .Add(x => x.Deger, "4.000,00 ₺")
            .Add(x => x.MakineDeger, "4000.00")
            .Add(x => x.KaynakRozeti, "kurdan geldi"));

        var el = c.Find("#t-tl");
        Assert.NotEqual("INPUT", el.TagName);
        Assert.Empty(el.QuerySelectorAll("input, textarea"));
        Assert.Contains("4.000,00", el.TextContent);
        Assert.Equal("4000.00", el.GetAttribute("data-deger"));
        // UX-S21: değerin NEREDEN geldiği yazılı.
        Assert.Contains("kurdan geldi", c.Find("#t-tl-kaynak").TextContent);
        // Salt okunur alanda "(isteğe bağlı)" eki ANLAMSIZDIR — çizilmez.
        Assert.Empty(c.FindAll(".alan-istege"));
    }

    // ── 6) SALT OKUNUR: değer yoksa UYDURULMAZ, çağıranın metni yazılır ─────
    [Fact]
    public void GG6_SaltOkunur_deger_yoksa_uydurmaz()
    {
        var c = Render<SaltOkunur>(p => p
            .Add(x => x.ElemanId, "t-tl")
            .Add(x => x.Etiket, "TL karşılığı")
            .Add(x => x.Deger, null)
            .Add(x => x.BosMetin, "değer yok"));

        Assert.Equal("değer yok", c.Find("#t-tl").TextContent.Trim());
    }

    // ── 7) KÖPRÜ: hata FormDurumu'ndan okunur, ESKİ kimlikle çizilir ────────
    [Fact]
    public void GG7_FormDurumu_koprusu_eski_hata_kimligini_korur()
    {
        var durum = new FormDurumu();
        durum.Ekle("cari-ad", "Ad boş olamaz.");

        var c = Render<Metin>(p => p
            .Add(x => x.ElemanId, "cari-ad")
            .Add(x => x.Etiket, "Ad")
            .Add(x => x.Durum, durum)
            .Add(x => x.HataAlani, "cari-ad"));

        // AlanHatasi'nın ürettiği kimlik BİREBİR korunur.
        var hata = c.Find("#alan-hata-cari-ad");
        Assert.Equal("Ad boş olamaz.", hata.TextContent);
        Assert.Equal("alert", hata.GetAttribute("role"));
        Assert.Equal("true", c.Find("#cari-ad").GetAttribute("aria-invalid"));
        Assert.Contains("alan-hata-cari-ad", c.Find("#cari-ad").GetAttribute("aria-describedby")!);
    }

    // ── 8) KÖPRÜ: defter temizken hata ÇİZİLMEZ (boş span bile bırakılmaz) ──
    [Fact]
    public void GG8_FormDurumu_temizken_hata_cizilmez()
    {
        var durum = new FormDurumu();
        var c = Render<Metin>(p => p
            .Add(x => x.ElemanId, "cari-ad")
            .Add(x => x.Etiket, "Ad")
            .Add(x => x.Durum, durum)
            .Add(x => x.HataAlani, "cari-ad"));

        Assert.Empty(c.FindAll(".alan-hata"));
        Assert.Equal("false", c.Find("#cari-ad").GetAttribute("aria-invalid"));
    }

    // ── 9) KÖPRÜ: özel hata kimliği (eski #sifre-hata gibi) korunur ─────────
    [Fact]
    public void GG9_Ozel_hata_kimligi_birebir_korunur()
    {
        var c = Render<Sifre>(p => p
            .Add(x => x.ElemanId, "yeni-sifre-tekrar")
            .Add(x => x.Etiket, "Şifre tekrar")
            .Add(x => x.Hata, "Şifreler eşleşmiyor")
            .Add(x => x.HataKimligi, "sifre-hata"));

        Assert.Equal("Şifreler eşleşmiyor", c.Find("#sifre-hata").TextContent);
        // Türetilen kimlik AÇILMAZ — iki hata kutusu doğmaz.
        Assert.Empty(c.FindAll("#yeni-sifre-tekrar-hata"));
    }

    // ── 10) SAYISAL: çağıranın hatası defterdeki hatayı EZMEZ, ikisi de görünür
    [Fact]
    public void GG10_Sayisal_alanda_defter_hatasi_alan_altinda_cikar()
    {
        var durum = new FormDurumu();
        durum.Ekle("u-gram", "Gram sıfırdan büyük olmalı.");

        var c = Render<Gram>(p => p
            .Add(x => x.ElemanId, "u-gram")
            .Add(x => x.Etiket, "Gram")
            .Add(x => x.Durum, durum)
            .Add(x => x.HataAlani, "u-gram"));

        Assert.Equal("Gram sıfırdan büyük olmalı.", c.Find("#alan-hata-u-gram").TextContent);
        Assert.Contains("hatali", c.Find("#u-gram").GetAttribute("class")!);
    }

    // ── 11) ÇOK SATIR: sayaç KAPATILABİLİR — aynı kimlik iki kez doğmaz ─────
    [Fact]
    public void GG11_CokSatir_sayaci_kapatilabilir()
    {
        var acik = Render<CokSatir>(p => p
            .Add(x => x.ElemanId, "da-gerekce").Add(x => x.Etiket, "Gerekçe"));
        Assert.Single(acik.FindAll("#da-gerekce-sayac"));

        var kapali = Render<CokSatir>(p => p
            .Add(x => x.ElemanId, "da-gerekce").Add(x => x.Etiket, "Gerekçe")
            .Add(x => x.SayacGoster, false));
        Assert.Empty(kapali.FindAll("#da-gerekce-sayac"));
    }

    // ── 12) ONAY: erişilebilir ad hem etiketten hem aria-label'dan okunur ───
    [Fact]
    public void GG12_Onay_erisilebilir_ad_tasir()
    {
        var c = Render<Onay>(p => p
            .Add(x => x.ElemanId, "f-yalniz-hassas")
            .Add(x => x.Etiket, "Yalnız hassas kayıtlar"));

        Assert.Equal("Yalnız hassas kayıtlar", c.Find("#f-yalniz-hassas").GetAttribute("aria-label"));
        Assert.Equal("f-yalniz-hassas", c.Find("label.onay-etiket").GetAttribute("for"));
    }
}
