using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ORTAK SEÇİM BİLEŞENİ — <Secici> [bUnit] · paket P-36 (kurucu talimatı 19)
//
// NEDEN: kurucu 21 Eylül'de ekran görüntüsüyle geldi — yerleşik <select>'in
// açılır listesini işletim sistemi çiziyor, projenin renkleriyle ilgisi yok.
// Tek çözüm özel bileşen. Bu testler bileşenin SÖZLEŞMESİNİ (FRONTEND-KIMLIK
// § 6.4-6.9) kilitler; "görünüyor" yetmez, davranış ölçülür.
//
// ⚠ Her test AYNI id sözleşmesine bakar: mevcut <select> id'si BİREBİR korunur,
//   türetilenler <ElemanId>-ara / -liste / -secenek-<n> / -hata / -durum.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SeciciBilesenTestleri : BunitContext
{
    public SeciciBilesenTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private static IReadOnlyList<SecenekOgesi> Uc =>
    [
        new("A", "Ankara"),
        new("B", "Isparta", "merkez"),
        new("C", "İzmir"),
    ];

    private static IReadOnlyList<SecenekOgesi> On =>
        Enumerable.Range(1, 10).Select(i => new SecenekOgesi($"K{i}", $"Kasa {i}")).ToList();

    private IRenderedComponent<Secici> Kur(
        IReadOnlyList<SecenekOgesi> secenekler, string? deger = null, Action<string?>? secildi = null,
        bool devredisi = false, string? sebep = null, string? hata = null, bool zorunlu = false)
        => Render<Secici>(p => p
            .Add(c => c.ElemanId, "test-secim")
            .Add(c => c.Etiket, "Şube")
            .Add(c => c.Secenekler, secenekler)
            .Add(c => c.Deger, deger)
            .Add(c => c.Zorunlu, zorunlu)
            .Add(c => c.Devredisi, devredisi)
            .Add(c => c.DevredisiSebebi, sebep)
            .Add(c => c.Hata, hata)
            .Add(c => c.DegerChanged, (string? v) => secildi?.Invoke(v)));

    // ── 1) Kapalıyken liste ÇİZİLMEZ, combobox sözleşmesi yerinde ───────────
    [Fact]
    public void S36_1_Kapali_hal_combobox_sozlesmesi()
    {
        var c = Kur(Uc);
        var tetik = c.Find("#test-secim");
        Assert.Equal("combobox", tetik.GetAttribute("role"));
        Assert.Equal("false", tetik.GetAttribute("aria-expanded"));
        Assert.Equal("listbox", tetik.GetAttribute("aria-haspopup"));
        Assert.Equal("test-secim-liste", tetik.GetAttribute("aria-controls"));
        Assert.Empty(c.FindAll("#test-secim-liste"));
    }

    // ── 2) Tıklayınca açılır, listbox + option rolleri doğar ────────────────
    [Fact]
    public void S36_2_Tiklayinca_listbox_acilir()
    {
        var c = Kur(Uc);
        c.Find("#test-secim").Click();
        Assert.Equal("true", c.Find("#test-secim").GetAttribute("aria-expanded"));
        var liste = c.Find("#test-secim-liste");
        Assert.Equal("listbox", liste.GetAttribute("role"));
        Assert.Equal(3, c.FindAll("#test-secim-liste li[role=option]").Count);
    }

    // ── 3) Seçim gerçekten geri çağırır (DoD EK md.1: tıklanınca bir şey olur)
    [Fact]
    public void S36_3_Secenek_tiklaninca_deger_doner()
    {
        string? gelen = "yok";
        var c = Kur(Uc, secildi: v => gelen = v);
        c.Find("#test-secim").Click();
        c.Find("#test-secim-secenek-1").Click();
        Assert.Equal("B", gelen);
        Assert.Empty(c.FindAll("#test-secim-liste"));   // seçince kapanır
    }

    // ── 4) TÜRKÇE HARF DUYARLILIĞI — kurucunun "ısparta→Isparta" şartı ──────
    [Fact]
    public void S36_4_Turkce_arama_isparta_Isparta_bulur()
    {
        var c = Kur(On.Concat(Uc).ToList());          // 13 > 8 → arama kutusu çıkar
        c.Find("#test-secim").Click();
        c.Find("#test-secim-ara-girdi").Input("ısparta");
        var satirlar = c.FindAll("#test-secim-liste li[role=option]");
        Assert.Single(satirlar);
        Assert.Contains("Isparta", satirlar[0].TextContent);
    }

    // ── 5) Arama kutusu EŞİĞE göre çıkar (§ 6.5: > 8) ───────────────────────
    [Fact]
    // ⚠ P-37 (kurucu 21 Eylül 16:30: "dropdownun bazılarında var bazılarında
    //   yok arama") — ESKİ ADI: S36_5_Arama_kutusu_esige_gore. Eşik davranışı
    //   KALDIRILDI; testin ölçtüğü sözleşme TERSİNE döndü: arama kutusu az
    //   seçenekte de çıkar. Test SİLİNMEDİ, yeni sözleşmeye çevrildi.
    public void S36_5_Arama_kutusu_ESIKSIZ_her_zaman_gorunur()
    {
        var az = Kur(Uc);
        az.Find("#test-secim").Click();
        Assert.Single(az.FindAll("#test-secim-ara-girdi"));

        var cok = Kur(On.Concat(Uc).ToList());
        cok.Find("#test-secim").Click();
        Assert.Single(cok.FindAll("#test-secim-ara-girdi"));
    }

    // ── 6) Eşleşme yoksa BOŞ DURUM metni (UX-S28: hata değil, boş durum) ────
    [Fact]
    public void S36_6_Eslesme_yoksa_yonlendirici_metin()
    {
        var c = Kur(On.Concat(Uc).ToList());
        c.Find("#test-secim").Click();
        c.Find("#test-secim-ara-girdi").Input("zzzz");
        Assert.Empty(c.FindAll("#test-secim-liste li[role=option]"));
        Assert.NotNull(c.Find("#test-secim-eslesme-yok").TextContent);
        Assert.NotEmpty(c.Find("#test-secim-eslesme-yok").TextContent.Trim());
    }

    // ── 7) Klavye: ↓ açar, ↓ ilerletir, Enter seçer (§ 6.6) ─────────────────
    [Fact]
    public void S36_7_Klavye_ile_secim()
    {
        string? gelen = null;
        var c = Kur(Uc, secildi: v => gelen = v);
        c.Find("#test-secim").KeyDown("ArrowDown");
        Assert.Equal("true", c.Find("#test-secim").GetAttribute("aria-expanded"));
        c.Find("#test-secim-liste").KeyDown("ArrowDown");
        c.Find("#test-secim-liste").KeyDown("Enter");
        Assert.Equal("B", gelen);
    }

    // ── 8) Esc kapatır, DEĞERİ DEĞİŞTİRMEZ ──────────────────────────────────
    [Fact]
    public void S36_8_Esc_degeri_degistirmeden_kapatir()
    {
        var cagrildi = false;
        var c = Kur(Uc, deger: "A", secildi: _ => cagrildi = true);
        c.Find("#test-secim").Click();
        c.Find("#test-secim-liste").KeyDown("Escape");
        Assert.Empty(c.FindAll("#test-secim-liste"));
        Assert.False(cagrildi);
    }

    // ── 9) Etkin öğe aria-activedescendant ile duyurulur; ok BAŞTA DURUR ────
    [Fact]
    public void S36_9_Etkin_oge_duyurulur_ve_liste_sarmaz()
    {
        var c = Kur(Uc);
        c.Find("#test-secim").Click();
        Assert.Equal("test-secim-secenek-0", c.Find("#test-secim").GetAttribute("aria-activedescendant"));
        c.Find("#test-secim-liste").KeyDown("ArrowUp");     // başta: SARMAZ
        Assert.Equal("test-secim-secenek-0", c.Find("#test-secim").GetAttribute("aria-activedescendant"));
        c.Find("#test-secim-liste").KeyDown("End");
        Assert.Equal("test-secim-secenek-2", c.Find("#test-secim").GetAttribute("aria-activedescendant"));
        c.Find("#test-secim-liste").KeyDown("ArrowDown");   // sonda: SARMAZ
        Assert.Equal("test-secim-secenek-2", c.Find("#test-secim").GetAttribute("aria-activedescendant"));
    }

    // ── 10) DEVRE DIŞI ise SEBEBİ yazılır (DoD EK md.1 · UX-S30) ────────────
    [Fact]
    public void S36_10_Devredisi_sebebini_soyler()
    {
        var c = Kur(Uc, devredisi: true, sebep: "Önce kasa seçin.");
        var tetik = c.Find("#test-secim");
        Assert.True(tetik.HasAttribute("disabled"));
        Assert.Equal("Önce kasa seçin.", c.Find("#test-secim-durum").TextContent.Trim());
        tetik.Click();                                   // kilitli: açılmaz
        Assert.Empty(c.FindAll("#test-secim-liste"));
    }

    // ── 11) Hata alanın ALTINDA, aria-invalid ile (UX sözleşmesi) ───────────
    [Fact]
    public void S36_11_Hata_alan_altinda_ve_aria_invalid()
    {
        var c = Kur(Uc, hata: "Şube seçilmeden kaydedilemez.");
        Assert.Equal("true", c.Find("#test-secim").GetAttribute("aria-invalid"));
        var h = c.Find("#test-secim-hata");
        Assert.Equal("alert", h.GetAttribute("role"));
        Assert.Contains("Şube", h.TextContent);
    }

    // ── 12) Seçili öğe YALNIZ RENKLE anlatılmaz: ✓ işareti + aria-selected ──
    [Fact]
    public void S36_12_Secili_oge_isaretle_anlatilir()
    {
        var c = Kur(Uc, deger: "C");
        c.Find("#test-secim").Click();
        var secili = c.Find("#test-secim-secenek-2");
        Assert.Equal("true", secili.GetAttribute("aria-selected"));
        // Yalnız renkle anlatılmaz: işaret sınıfı + ortak Ikon (çıplak ✓ yok).
        Assert.NotNull(secili.QuerySelector(".secici-isaret.secili-isaret svg"));
        Assert.Null(c.Find("#test-secim-secenek-0").QuerySelector(".secici-isaret svg"));
        Assert.Equal("false", c.Find("#test-secim-secenek-0").GetAttribute("aria-selected"));
    }

    // ── 13) Pasif öğe SEÇİLEMEZ (sessizce seçilip kaydedilemez) ─────────────
    [Fact]
    public void S36_13_Pasif_oge_secilemez()
    {
        var cagrildi = false;
        var c = Kur([new("A", "Ankara"), new("X", "Kapalı şube", "Şube pasif", Pasif: true)],
                    secildi: _ => cagrildi = true);
        c.Find("#test-secim").Click();
        var pasif = c.Find("#test-secim-secenek-1");
        Assert.Equal("true", pasif.GetAttribute("aria-disabled"));
        pasif.Click();
        Assert.False(cagrildi);
    }

    // ── 14) Uzak kip SESSİZCE düşmez — md.1: bilinmeyen tahmin edilmez ──────
    [Fact]
    public void S36_14_Uzak_kip_sessiz_dusmez()
    {
        // P-36b: Uzak kip ARTIK UYGULANDI. Sessiz kip seçimi hâlâ yasaktır —
        // iki kaynak (UzakTur + Secenekler) birlikte verilirse kip belirsiz
        // kalır ve bileşen SUSMAZ, istisna atar (Anayasa md.1).
        var hata = Assert.Throws<InvalidOperationException>(() =>
            Render<Secici>(p => p
                .Add(c => c.ElemanId, "test-secim")
                .Add(c => c.Etiket, "Cari")
                .Add(c => c.Secenekler, Uc)
                .Add(c => c.UzakTur, "CARI")));
        Assert.Contains("BİRLİKTE", hata.Message);
    }

    // ── 15) Boş seçenek metni verilirse boş satır çıkar ve seçimi TEMİZLER ──
    [Fact]
    public void S36_15_Bos_secenek_secimi_temizler()
    {
        string? gelen = "dolu";
        var c = Render<Secici>(p => p
            .Add(x => x.ElemanId, "test-secim")
            .Add(x => x.Etiket, "Şube")
            .Add(x => x.Secenekler, Uc)
            .Add(x => x.Deger, "A")
            .Add(x => x.BosSecenekMetni, "Tümü")
            .Add(x => x.DegerChanged, (string? v) => gelen = v));
        c.Find("#test-secim").Click();
        c.Find("#test-secim-secenek-bos").Click();
        Assert.Null(gelen);
    }
}
