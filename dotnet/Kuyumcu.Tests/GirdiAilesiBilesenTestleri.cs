using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// GİRDİ AİLESİ — Bilesenler/Form/* [bUnit] · paket P-36b (talimat 20/a)
//
// NEDEN: 21 Eylül ölçümünde ekranlarda 365 ham <input>/<textarea> vardı; her
// biri kendi etiketini, zorunluluk işaretini, hata yerini ve dokunma hedefini
// kendi bildiği gibi çiziyordu. Bu testler AİLENİN SÖZLEŞMESİNİ kilitler:
// "görünüyor" yetmez, davranış ölçülür.
//
// ⚠ Her test aynı id sözleşmesine bakar: <ElemanId> girdinin kendisi,
//   türetilenler <ElemanId>-sar / -ipucu / -durum / -hata / -onizleme.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GirdiAilesiBilesenTestleri : BunitContext
{
    public GirdiAilesiBilesenTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    // ── 1) METİN: etiket + zorunluluk + id sözleşmesi ────────────────────────
    [Fact]
    public void G1_Metin_etiket_zorunluluk_ve_id_sozlesmesi()
    {
        var c = Render<Metin>(p => p
            .Add(x => x.ElemanId, "t-ad")
            .Add(x => x.Etiket, "Ad")
            .Add(x => x.Zorunlu, true));

        var girdi = c.Find("#t-ad");
        Assert.Equal("text", girdi.GetAttribute("type"));
        Assert.Equal("true", girdi.GetAttribute("aria-required"));
        Assert.NotNull(c.Find("#t-ad-sar"));
        // Etiket GÖRÜNÜR ve girdiye BAĞLI — etiketsiz alan yasak (UX-S31).
        Assert.Equal("t-ad", c.Find("label.alan-etiket").GetAttribute("for"));
        Assert.Contains("*", c.Find(".zorunlu-isaret").TextContent);
    }

    // ── 2) METİN: isteğe bağlıysa "(isteğe bağlı)" yazar, yıldız YOK ─────────
    [Fact]
    public void G2_Metin_istege_bagli_alan_isaretlenir()
    {
        var c = Render<Metin>(p => p
            .Add(x => x.ElemanId, "t-not")
            .Add(x => x.Etiket, "Not"));

        Assert.Empty(c.FindAll(".zorunlu-isaret"));
        Assert.Contains("isteğe", c.Find(".alan-istege").TextContent);
    }

    // ── 3) Hata alanın ALTINDA + aria-invalid + role=alert (DoD EK md.3) ─────
    [Fact]
    public void G3_Hata_alan_altinda_ve_erisilebilir()
    {
        var c = Render<Metin>(p => p
            .Add(x => x.ElemanId, "t-ad")
            .Add(x => x.Etiket, "Ad")
            .Add(x => x.Hata, "Ad boş olamaz."));

        var hata = c.Find("#t-ad-hata");
        Assert.Equal("alert", hata.GetAttribute("role"));
        Assert.Equal("Ad boş olamaz.", hata.TextContent.Trim());
        Assert.Equal("true", c.Find("#t-ad").GetAttribute("aria-invalid"));
        Assert.Contains("t-ad-hata", c.Find("#t-ad").GetAttribute("aria-describedby")!);
    }

    // ── 4) Kilitli alan SEBEBİNİ söyler (UX-S30 · DoD EK md.1) ───────────────
    [Fact]
    public void G4_Kilitli_alan_sebebini_soyler()
    {
        var c = Render<Metin>(p => p
            .Add(x => x.ElemanId, "t-ad")
            .Add(x => x.Etiket, "Ad")
            .Add(x => x.Devredisi, true)
            .Add(x => x.DevredisiSebebi, "Önce kasa seçin."));

        Assert.True(c.Find("#t-ad").HasAttribute("disabled"));
        Assert.Equal("Önce kasa seçin.", c.Find("#t-ad-durum").TextContent.Trim());
    }

    // ── 5) UX-S21: türetilen değerin KAYNAĞI rozetle söylenir ────────────────
    [Fact]
    public void G5_Kaynak_rozeti_nereden_geldigini_yazar()
    {
        var c = Render<Para>(p => p
            .Add(x => x.ElemanId, "t-tutar")
            .Add(x => x.Etiket, "Tutar")
            .Add(x => x.KaynakRozeti, "kasadan"));

        Assert.Equal("kasadan", c.Find("#t-tutar-kaynak").TextContent.Trim());
    }

    // ── 6) SAYI: "12,5" de "12.5" de kabul edilir, TEK çözücüden geçer ───────
    [Theory]
    [InlineData("12,5", 12.5)]
    [InlineData("12.5", 12.5)]
    [InlineData(" 7 ", 7)]
    public void G6_Sayi_virgul_ve_nokta_ayirici_kabul_edilir(string ham, double beklenen)
    {
        decimal? gelen = null;
        var c = Render<Sayi>(p => p
            .Add(x => x.ElemanId, "t-adet")
            .Add(x => x.Etiket, "Adet")
            .Add(x => x.DegerChanged, (decimal? v) => gelen = v));

        c.Find("#t-adet").Input(ham);
        Assert.Equal((decimal)beklenen, gelen);
    }

    // ── 7) SAYI: anlaşılmayan metin SESSİZ GEÇMEZ, alan altı mesaj çıkar ─────
    [Fact]
    public void G7_Bozuk_sayi_sessiz_etkisiz_kalmaz()
    {
        decimal? gelen = 99m;
        var c = Render<Sayi>(p => p
            .Add(x => x.ElemanId, "t-adet")
            .Add(x => x.Etiket, "Adet")
            .Add(x => x.DegerChanged, (decimal? v) => gelen = v));

        c.Find("#t-adet").Input("abc");

        // Değer DEĞİŞMEDİ (0'a düşmedi — md.1: bilinmeyen tahmin edilmez)
        Assert.Equal(99m, gelen);
        Assert.NotNull(c.Find("#t-adet-hata"));
        Assert.Equal("true", c.Find("#t-adet").GetAttribute("aria-invalid"));
    }

    // ── 8) PARA: para birimi TUTARLA taşınır (UX-S32) ────────────────────────
    [Fact]
    public void G8_Para_birimi_tutarla_birlikte_gosterilir()
    {
        var c = Render<Para>(p => p
            .Add(x => x.ElemanId, "t-tutar")
            .Add(x => x.Etiket, "Tutar")
            .Add(x => x.Deger, 1234.5m)
            .Add(x => x.ParaBirimi, "USD"));

        // UX-S32 KORUNUR: para birimi tutarın YANINDA taşınır — ama artık
        // kutunun sağındaki `-birim` ekinde, girdinin ALTINDA İKİNCİ KEZ değil.
        Assert.Equal("USD", c.Find("#t-tutar-birim").TextContent.Trim());

        // ⚠ P-39 · K7 (TASARIM-DENETIMI § 5.1): "1234.5" yazan kutunun altına
        // "$1.234,50" yazmak MÜKERRER bilgidir (5 ekranda ölçüldü). Biçim
        // bilgi eklemiyorsa önizleme ÇİZİLMEZ ve aria-describedby de onu
        // göstermez (ekran okuyucuya da iki kez okutulmaz).
        Assert.Empty(c.FindAll("#t-tutar-onizleme"));
        Assert.DoesNotContain("t-tutar-onizleme",
            c.Find("#t-tutar").GetAttribute("aria-describedby") ?? "");
    }

    // ── 9) GRAM: has karşılığı VERİLDİYSE gösterilir, verilmediyse UYDURULMAZ ─
    [Fact]
    public void G9_Gram_has_karsiligi_uydurulmaz()
    {
        // P-39 · K7: has karşılığı YOKSA önizlemenin söyleyecek yeni bir şeyi
        // kalmaz ("10" kutusunun altında "10,000 g") → hiç çizilmez.
        var yok = Render<Gram>(p => p
            .Add(x => x.ElemanId, "t-gram")
            .Add(x => x.Etiket, "Gram")
            .Add(x => x.Deger, 10m));
        Assert.Empty(yok.FindAll("#t-gram-onizleme"));

        var var_ = Render<Gram>(p => p
            .Add(x => x.ElemanId, "t-gram2")
            .Add(x => x.Etiket, "Gram")
            .Add(x => x.Deger, 10m)
            .Add(x => x.HasKarsiligi, 9.16m));
        Assert.Contains("has", var_.Find("#t-gram2-onizleme").TextContent);
    }

    // ── 10) TARİH: ISO 8601 taşınır (makine biçimi) ──────────────────────────
    [Fact]
    public void G10_Tarih_ISO_8601_tasir()
    {
        DateOnly? gelen = null;
        var c = Render<Tarih>(p => p
            .Add(x => x.ElemanId, "t-tarih")
            .Add(x => x.Etiket, "Tarih")
            .Add(x => x.Deger, new DateOnly(2026, 9, 21))
            .Add(x => x.DegerChanged, (DateOnly? v) => gelen = v));

        // GIRDI-AILESI-2 (kurucu 08.10): kutu artık metin + gg.aa.yyyy maskesi; değer yine DateOnly,
        // makine biçimi (ISO) change ile hâlâ kabul edilir.
        Assert.Equal("21.09.2026", c.Find("#t-tarih").GetAttribute("value"));
        c.Find("#t-tarih").Change("2026-12-01");
        Assert.Equal(new DateOnly(2026, 12, 1), gelen);
    }

    // ── 11) TARİH ARALIĞI: ters aralık TEK yerden söylenir ───────────────────
    [Fact]
    public void G11_Ters_tarih_araligi_alan_altinda_soylenir()
    {
        var c = Render<TarihAralik>(p => p
            .Add(x => x.ElemanId, "t-aralik")
            .Add(x => x.Etiket, "Dönem")
            .Add(x => x.Bas, new DateOnly(2026, 9, 21))
            .Add(x => x.Bit, new DateOnly(2026, 9, 1)));

        Assert.NotNull(c.Find("#t-aralik-bas"));
        Assert.NotNull(c.Find("#t-aralik-bit"));
        Assert.NotEmpty(c.Find("#t-aralik-hata").TextContent.Trim());
    }

    // ── 12) TELEFON / E-POSTA: doğru klavye ve otomatik tamamlama ────────────
    [Fact]
    public void G12_Telefon_ve_eposta_dogru_klavyeyi_acar()
    {
        var t = Render<Telefon>(p => p
            .Add(x => x.ElemanId, "t-tel").Add(x => x.Etiket, "Telefon"));
        Assert.Equal("tel", t.Find("#t-tel").GetAttribute("inputmode"));
        Assert.Equal("tel", t.Find("#t-tel").GetAttribute("autocomplete"));
        // GIRDI-AILESI-2 tur 3 (tasarım hakemi): varsayılan ipucu satırı YOK — biçimi maske + yer tutucu anlatır.
        Assert.Empty(t.FindAll("#t-tel-ipucu"));
        Assert.False(string.IsNullOrWhiteSpace(t.Find("#t-tel").GetAttribute("placeholder")));

        var e = Render<Eposta>(p => p
            .Add(x => x.ElemanId, "t-eposta").Add(x => x.Etiket, "E-posta"));
        Assert.Equal("email", e.Find("#t-eposta").GetAttribute("type"));
        Assert.Equal("email", e.Find("#t-eposta").GetAttribute("inputmode"));
    }

    // ── 13) ÇOK SATIR: sunucu sınırı EKRANDA yazar (UX-S29) ──────────────────
    [Fact]
    public void G13_CokSatir_sinirini_ekranda_soyler()
    {
        var c = Render<CokSatir>(p => p
            .Add(x => x.ElemanId, "t-aciklama")
            .Add(x => x.Etiket, "Açıklama")
            .Add(x => x.EnFazla, 120)
            .Add(x => x.Deger, "abc"));

        Assert.Equal("120", c.Find("#t-aciklama").GetAttribute("maxlength"));
        Assert.Contains("3 / 120", c.Find("#t-aciklama-sayac").TextContent);
    }

    // ── 14) ONAY: değişiklik geri bildirilir, etiket tıklanabilir ────────────
    [Fact]
    public void G14_Onay_kutusu_degeri_bildirir()
    {
        bool? gelen = null;
        var c = Render<Onay>(p => p
            .Add(x => x.ElemanId, "t-onay")
            .Add(x => x.Etiket, "KDV dahil")
            .Add(x => x.DegerChanged, (bool v) => gelen = v));

        Assert.Equal("t-onay", c.Find("label.onay-etiket").GetAttribute("for"));
        c.Find("#t-onay").Change(true);
        Assert.True(gelen);
    }

    // ── 15) SEÇİM: pasif seçenek SEÇİLEMEZ ───────────────────────────────────
    [Fact]
    public void G15_Secim_pasif_secenek_secilemez()
    {
        string? gelen = null;
        var c = Render<Secim>(p => p
            .Add(x => x.ElemanId, "t-tur")
            .Add(x => x.Etiket, "Tür")
            .Add(x => x.Secenekler, new List<SecenekOgesi>
            {
                new("A", "Nakit"),
                new("B", "Kart", Pasif: true),
            })
            .Add(x => x.DegerChanged, (string? v) => gelen = v));

        Assert.True(c.Find("#t-tur-secenek-1").HasAttribute("disabled"));
        c.Find("#t-tur-secenek-0").Change(true);
        Assert.Equal("A", gelen);
    }

    // ── 16) SEÇİM: TEK seçenek varsa seçici GİZLENİR ve değer otomatik konur ─
    //      (UX-S22 — kullanıcıya sorulmayan soru gövdede boş kalmaz)
    [Fact]
    public void G16_Tek_secenekte_secici_gizlenir_ve_deger_otomatik_konur()
    {
        string? gelen = null;
        var c = Render<Secim>(p => p
            .Add(x => x.ElemanId, "t-kasa")
            .Add(x => x.Etiket, "Kasa")
            .Add(x => x.Secenekler, new List<SecenekOgesi> { new("K1", "Merkez kasa") })
            .Add(x => x.DegerChanged, (string? v) => gelen = v));

        Assert.Empty(c.FindAll(".secim-grubu"));
        Assert.Equal("Merkez kasa", c.Find("#t-kasa-tek").TextContent.Trim());
        Assert.Equal("K1", gelen);
    }

    // ── 17) ANAHTAR: role=switch, durumu METİNLE de söyler (UX-S7) ───────────
    [Fact]
    public void G17_Anahtar_durumu_metinle_de_soyler()
    {
        bool? gelen = null;
        var c = Render<Anahtar>(p => p
            .Add(x => x.ElemanId, "t-ai")
            .Add(x => x.Etiket, "Yapay zekâ")
            .Add(x => x.Deger, false)
            .Add(x => x.DegerChanged, (bool v) => gelen = v));

        var d = c.Find("#t-ai");
        Assert.Equal("switch", d.GetAttribute("role"));
        Assert.Equal("false", d.GetAttribute("aria-checked"));
        Assert.NotEmpty(c.Find(".anahtar-metin").TextContent.Trim());

        d.Click();
        Assert.True(gelen);
    }

    // ── 18) DoD EK md.1: kilitli anahtar tıklanınca HİÇBİR ŞEY OLMAZ ama
    //       sebebi yazılıdır — "tıklanınca hiçbir şey olmayan" düğme yok.
    [Fact]
    public void G18_Kilitli_anahtar_istek_atmaz_ve_sebebi_yazili()
    {
        var c = Render<Anahtar>(p => p
            .Add(x => x.ElemanId, "t-ai")
            .Add(x => x.Etiket, "Yapay zekâ")
            .Add(x => x.Devredisi, true)
            .Add(x => x.DevredisiSebebi, "Bu planda kapalı."));

        Assert.True(c.Find("#t-ai").HasAttribute("disabled"));
        Assert.Equal("Bu planda kapalı.", c.Find("#t-ai-durum").TextContent.Trim());
    }

    // ── 19) Yükleniyor: değer gelmeden alan KİLİTLİ, sebebi yazılı ───────────
    [Fact]
    public void G19_Yukleniyorken_alan_kilitli_kalir()
    {
        var c = Render<Metin>(p => p
            .Add(x => x.ElemanId, "t-ad")
            .Add(x => x.Etiket, "Ad")
            .Add(x => x.Yukleniyor, true)
            .Add(x => x.DevredisiSebebi, "Sunucudan okunuyor…"));

        Assert.True(c.Find("#t-ad").HasAttribute("disabled"));
        Assert.Equal("Sunucudan okunuyor…", c.Find("#t-ad-durum").TextContent.Trim());
    }

    // ── 20) TEK GÖRSEL KAYNAK: ailede satır içi renk/px YOKTUR ───────────────
    //      (kurucu: "beğenmediğimiz bir yer olursa TEK SEFERDE değiştiririz")
    [Fact]
    public void G20_Girdi_ailesinde_satir_ici_renk_veya_px_YOKTUR()
    {
        var dizin = Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "Bilesenler", "Form");
        var ihlaller = Directory.GetFiles(dizin, "*.razor")
            .Where(f => System.Text.RegularExpressions.Regex.IsMatch(
                File.ReadAllText(f), @"style=""[^""]*(#|px|rgb)"))
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(ihlaller.Count == 0,
            "Girdi ailesinde satır içi renk/px: " + string.Join(", ", ihlaller));
    }

    // ── 21) AİLE TAMLIĞI: talimat 20/a'daki 12 bileşenin HEPSİ vardır ────────
    [Fact]
    public void G21_Aile_on_iki_bilesenin_tamamini_tasir()
    {
        var dizin = Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "Bilesenler", "Form");
        var olmasiGereken = new[]
        {
            "Metin", "Sayi", "Para", "Gram", "Tarih", "TarihAralik",
            "Telefon", "Eposta", "CokSatir", "Onay", "Secim", "Anahtar",
        };

        var eksik = olmasiGereken
            .Where(a => !File.Exists(Path.Combine(dizin, a + ".razor")))
            .ToList();

        Assert.True(eksik.Count == 0, "Girdi ailesinde eksik bileşen: " + string.Join(", ", eksik));
    }
}
