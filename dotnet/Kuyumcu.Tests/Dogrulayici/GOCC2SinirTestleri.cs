using System.Diagnostics;
using Bunit;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests.Dogrulayici;

// olumsuz-fikstür: dosya — hasım doğrulayıcının SINIR testleri; eksi/aşırı/belirsiz değerler bilinçlidir.
// ═════════════════════════════════════════════════════════════════════════════
// PAKET GOC-C-2 — HASIM DOĞRULAYICI (Aşkın + Mehmet) FİNANSAL SINIR TESTLERİ (09.10.2026).
// Uygulayıcının testlerinden BAĞIMSIZ yazıldı. Beklenen değerler SABİT (üretim kodundan hesaplanmaz).
// Kapsam: karma ödeme satırları (FisOdemeBacaklari — VERI-1), tam sayı alanları (Adet / girdi-maske.js — VERI-2),
// açılış stoğu tavanı (AlanSinirlari.AcilisStokEnCok ↔ AcilisAdediAttribute), tr-TR ayrıştırma (FormAlani.SayiCoz).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GOCC2SinirOdemeTestleri : BunitContext
{
    public GOCC2SinirOdemeTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    /// <summary>Bicim.Tl bölünmez boşluk kullanır — metin karşılaştırması için tek boşluğa indirilir.</summary>
    private static string Bosluk(string s) => System.Text.RegularExpressions.Regex.Replace(s.Replace('\u00A0', ' ').Replace('\u202F', ' '), @"\s+", " ").Trim();

    private (IRenderedComponent<FisOdemeBacaklari> c, List<FisBacagi> b) Karma(decimal toplam)
    {
        var b = new List<FisBacagi>
        {
            new() { Arac = "KART", TutarTl = toplam, KasaId = "ks1" },
            new() { Arac = "NAKIT", TutarTl = 0m, KasaId = "ks1" },
        };
        var c = Render<FisOdemeBacaklari>(p => p
            .Add(x => x.Bacaklar, b)
            .Add(x => x.Kasalar, [new KasaVeri("ks1", "Ana Kasa", "TRY", true)])
            .Add(x => x.PosCihazlari, [])
            .Add(x => x.FisToplami, toplam)
            .Add(x => x.Basit, true));
        return (c, b);
    }

    [Fact]
    public void DG01_SIFIR_ikinci_satir_0_ilk_satir_toplamin_tamami_asim_yok_tek_satir_gider()
    {
        var (c, b) = Karma(10_000m);
        c.Find("#islem-bacak-1-tutar").Input("0");
        Assert.Equal(2, b.Count);
        Assert.Equal(10_000m, b[0].TutarTl);
        Assert.Equal(0m, b[1].TutarTl);
        Assert.Empty(c.FindAll("[id$=-asim]"));
        var giden = FisFark.Gonderilecek(b);
        Assert.Single(giden);
        Assert.Equal("KART", giden[0].Arac);
    }

    [Fact]
    public void DG02_EKSI_tutar_kabul_edilmez_satir_eksiye_inmez()
    {
        var (c, b) = Karma(10_000m);
        // Gerçek tarayıcıda "-" tuşu yazılamaz (EnAz=0 → data-eksi yok, girdi-maske.js); bUnit JS'i atlar. Bileşen
        // sözleşmesi: eksi değer alan altında HATA (aria-invalid) olur ve satır SİLİNMEZ; Kaydet kilidi sayfanın kapısında
        // (Islemler GecersizAlanKapisi). Hiçbir satır kendiliğinden eksiye İNMEZ.
        c.Find("#islem-bacak-1-tutar").Input("-5");
        Assert.Equal(2, b.Count);
        Assert.Equal("true", c.Find("#islem-bacak-1-tutar").GetAttribute("aria-invalid"));
        Assert.True(b[0].TutarTl >= 0m);
    }

    [Fact]
    public void DG03_KURUS_bir_kurus_asim_alan_altinda_TAM_kurusla_yazilir_kalani_ata_kurusu_kapatir()
    {
        var (c, b) = Karma(100.01m);
        c.Find("#islem-bacak-1-tutar").Input("100,02");
        Assert.Equal(100.02m, b[1].TutarTl);
        Assert.Equal(0m, b[0].TutarTl);
        Assert.Equal("Bu tutar fiş toplamını 0,01 ₺ aşıyor. Azaltın ya da “Kalanı ata”ya basın.",
            Bosluk(c.Find("#islem-bacak-1-asim").TextContent));
        c.Find("#islem-bacak-kalan-ata").Click();
        Assert.Equal(100.01m, b[1].TutarTl);
        Assert.Empty(c.FindAll("[id$=-asim]"));
    }

    [Fact]
    public void DG04_KURUS_YARIM_0_005_sessizce_yuvarlanip_odemeye_YAZILMAZ()
    {
        var (c, b) = Karma(10_000m);
        c.Find("#islem-bacak-1-tutar").Input("100,005");
        // Ya alan altı hata ile değer bildirilmez (0 kalır) ya da tam değer korunur; 100,01'e/100,00'a SESSİZ yuvarlama yok.
        Assert.True(b[1].TutarTl is 0m or 100.005m, $"bacak tutarı {b[1].TutarTl}");
        Assert.Equal(10_000m, b[0].TutarTl + b[1].TutarTl);
    }

    [Fact]
    public void DG05_COK_BUYUK_999_999_999_99_asim_dogru_tutarla_kalani_ata_fis_toplamina_indirir()
    {
        var (c, b) = Karma(10_000m);
        c.Find("#islem-bacak-1-tutar").Input("999.999.999,99");
        Assert.Equal(999_999_999.99m, b[1].TutarTl);
        Assert.Equal(2, b.Count);
        Assert.Equal("Bu tutar fiş toplamını 999.989.999,99 ₺ aşıyor. Azaltın ya da “Kalanı ata”ya basın.",
            Bosluk(c.Find("#islem-bacak-1-asim").TextContent));
        c.Find("#islem-bacak-kalan-ata").Click();
        Assert.Equal(10_000m, b[1].TutarTl);
        Assert.Equal(0m, b[0].TutarTl);
    }

    [Fact]
    public void DG06_BOS_metin_satir_0_olur_ilk_satir_toplami_geri_alir_satir_SILINMEZ()
    {
        var (c, b) = Karma(10_000m);
        c.Find("#islem-bacak-1-tutar").Input("4000");
        Assert.Equal(6_000m, b[0].TutarTl);
        c.Find("#islem-bacak-1-tutar").Input("");
        Assert.Equal(2, b.Count);
        Assert.Equal(0m, b[1].TutarTl);
        Assert.Equal(10_000m, b[0].TutarTl);
    }

    [Fact]
    public void DG07_TURKCE_45_000_odeme_satirinda_kirk_bes_bin()
    {
        var (c, b) = Karma(100_000m);
        c.Find("#islem-bacak-1-tutar").Input("45.000");
        Assert.Equal(45_000m, b[1].TutarTl);
        Assert.Equal(55_000m, b[0].TutarTl);
    }
}

public sealed class GOCC2SinirGirdiTestleri : BunitContext
{
    public GOCC2SinirGirdiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private (IRenderedComponent<Adet> c, Func<decimal?> giden) AcilisAdet()
    {
        decimal? g = -1m;
        var c = Render<Adet>(p => p.Add(x => x.ElemanId, "u-acilis-stok").Add(x => x.Etiket, "Açılış stoğu")
            .Add(x => x.EnCok, AlanSinirlari.AcilisStokEnCok).Add(x => x.GecersizdeBosalt, true)
            .Add(x => x.DegerChanged, EventCallback.Factory.Create<decimal?>(this, v => g = v)));
        return (c, () => g);
    }

    [Theory]
    [InlineData("45.000", 45_000)]       // tr-TR binlik
    [InlineData("1.000", 1_000)]
    [InlineData("1000000", 1_000_000)]   // tavan dahil
    public void DG10_acilis_ADET_kabul_edilen_degerler(string metin, int beklenen)
    {
        var (c, giden) = AcilisAdet();
        c.Find("#u-acilis-stok").Input(metin);
        Assert.Equal((decimal)beklenen, giden());
    }

    [Theory]
    [InlineData("1,5")]          // kesir
    [InlineData("1.5")]
    [InlineData("1.500")]        // belirsiz (1,5 mi 1500 mü?) → reddedilir
    [InlineData("1000001")]      // tavan üstü
    [InlineData("999.999.999,99")]
    [InlineData("-1")]
    [InlineData("0")]            // sunucu AcilisAdedi EnAz 0,001 ile aynı: sıfır açılış girilmez
    public void DG11_acilis_ADET_reddedilen_degerler_bildirilmez_15_olmaz(string metin)
    {
        var (c, giden) = AcilisAdet();
        c.Find("#u-acilis-stok").Input(metin);
        Assert.Null(giden());
        Assert.Equal("true", c.Find("#u-acilis-stok").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void DG12_acilis_GRAM_3_hane_gecer_4_hane_0_0001_reddedilir_yuvarlanmaz()
    {
        decimal? giden = -1m;
        var c = Render<Sayi>(p => p.Add(x => x.ElemanId, "m").Add(x => x.Etiket, "Miktar").Add(x => x.EnAz, 0m).Add(x => x.EnCokOndalik, 3)
            .Add(x => x.EnCok, AlanSinirlari.AcilisStokEnCok).Add(x => x.GecersizdeBosalt, true)
            .Add(x => x.DegerChanged, EventCallback.Factory.Create<decimal?>(this, v => giden = v)));
        c.Find("#m").Input("1,001");
        Assert.Equal(1.001m, giden);
        c.Find("#m").Input("1,0001");
        Assert.Null(giden);
        c.Find("#m").Input("1000000,001");
        Assert.Null(giden);
    }

    [Fact]
    public void DG13_sunucu_acilis_kurali_sinirlar_sabit()
    {
        var k = new Kuyumcu.Api.Modules.Urun.AcilisAdediAttribute();
        bool Gecer(decimal d) => k.GetValidationResult(d, new System.ComponentModel.DataAnnotations.ValidationContext(new object()) { MemberName = "Adet" })
            == System.ComponentModel.DataAnnotations.ValidationResult.Success;
        Assert.Equal(1_000_000m, AlanSinirlari.AcilisStokEnCok);
        Assert.True(Gecer(1_000_000m));
        Assert.False(Gecer(1_000_000.001m));
        Assert.False(Gecer(0m));
        Assert.False(Gecer(-1m));
        Assert.True(Gecer(0.001m));
    }

    [Theory]
    [InlineData("45.000", FormAlani.SayiSonucu.Tamam, "45000")]
    [InlineData("1,5", FormAlani.SayiSonucu.Tamam, "1.5")]
    [InlineData("1.500", FormAlani.SayiSonucu.Belirsiz, null)]
    [InlineData("999.999.999,99", FormAlani.SayiSonucu.Tamam, "999999999.99")]
    [InlineData("0,0001", FormAlani.SayiSonucu.Tamam, "0.0001")]
    [InlineData("", FormAlani.SayiSonucu.Bos, null)]
    [InlineData(null, FormAlani.SayiSonucu.Bos, null)]
    public void DG14_SayiCoz_tr_TR(string? ham, FormAlani.SayiSonucu sonuc, string? beklenen)
    {
        var r = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, r.Sonuc);
        Assert.Equal(beklenen is null ? null : decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), r.Deger);
    }
}

/// <summary>girdi-maske.js — TUŞLA yazım (gerçek dosya, node, sahte DOM): tam sayı alanında tr-TR binlik.</summary>
public sealed class GOCC2SinirMaskeTestleri
{
    private const string Sahne = """
        const fs = require('fs');
        const h = {};
        const ipucu = { attrs: { hidden: '' }, removeAttribute(a) { delete this.attrs[a]; }, setAttribute(a, v) { this.attrs[a] = v; } };
        global.window = { addEventListener: (t, f) => { (h[t] = h[t] || []).push(f); } };
        global.document = { getElementById: id => id === 'k-kesir' ? ipucu : null };
        global.Event = function (t) { this.type = t; };
        eval(fs.readFileSync(process.argv[2], 'utf8'));
        function kutu() {
          const a = { 'data-maske': 'sayi', 'data-tam': '1' };
          return { tagName: 'INPUT', id: 'k', value: '', selectionStart: 0, selectionEnd: 0, attrs: a,
            hasAttribute(n) { return n in this.attrs; }, getAttribute(n) { return n in this.attrs ? this.attrs[n] : null; },
            setAttribute(n, v) { this.attrs[n] = String(v); }, removeAttribute(n) { delete this.attrs[n]; },
            setSelectionRange(s, e) { this.selectionStart = s; this.selectionEnd = e; } };
        }
        function yaz(el, metin) {
          for (const ch of metin) {
            let engel = false;
            const ev = { target: el, inputType: 'insertText', data: ch, preventDefault() { engel = true; } };
            (h.beforeinput || []).forEach(f => f(ev));
            if (engel) continue;
            el.value = el.value.slice(0, el.selectionStart) + ch + el.value.slice(el.selectionEnd);
            el.selectionStart = el.selectionEnd = el.selectionStart + 1;
            (h.input || []).forEach(f => f({ target: el }));
          }
          return el.value;
        }
        process.stdout.write(JSON.stringify({ kirkBesBin: yaz(kutu(), '45.000'), bin: yaz(kutu(), '1.000'), onBes: yaz(kutu(), '1,5') }));
        """;

    private static System.Text.Json.JsonElement Kos()
    {
        var maske = CssKaynak.Yol("Kuyumcu.Web", "wwwroot", "js", "girdi-maske.js");
        var psi = new ProcessStartInfo("node") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        if (!File.Exists("/usr/bin/node") && File.Exists("/opt/homebrew/bin/node")) psi.FileName = "/opt/homebrew/bin/node";
        psi.ArgumentList.Add("-");
        psi.ArgumentList.Add(maske);
        using var p = Process.Start(psi)!;
        p.StandardInput.Write(Sahne);
        p.StandardInput.Close();
        var cikti = p.StandardOutput.ReadToEnd();
        var hata = p.StandardError.ReadToEnd();
        p.WaitForExit();
        Assert.True(p.ExitCode == 0, hata);
        return System.Text.Json.JsonDocument.Parse(cikti).RootElement.Clone();
    }

    [Fact]
    public void DG20_tam_sayi_kutusunda_1_virgul_5_TUSLA_ASLA_15_olmaz()
    {
        Assert.NotEqual("15", Kos().GetProperty("onBes").GetString());
    }

    [Fact]   // tur 2: hasımın Skip'i kaldırıldı — girdi-maske.js '.' binlik ayırıcıyı artık engellemez
    public void DG21_tam_sayi_kutusunda_TUSLA_45_000_kirk_bes_bin_kalir_45e_DUSMEZ()
    {
        var r = Kos();
        // Ya "45.000" kalır (C# SayiCoz → 45.000) ya da "45000" yazılır; "45" (1000 kat küçültme) YANLIŞ.
        Assert.Contains(r.GetProperty("kirkBesBin").GetString(), new[] { "45.000", "45000" });
        Assert.Contains(r.GetProperty("bin").GetString(), new[] { "1.000", "1000" });
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// GOC-C-2 — HASIM DOĞRULAYICI TUR 3 (Aşkın + Mehmet, 09.10.2026) — bağımsız ek sınır testleri.
// Beklenen değerler SABİT yazıldı (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class GOCC2SinirTur3Testleri : BunitContext
{
    public GOCC2SinirTur3Testleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private static string Bosluk(string s) => System.Text.RegularExpressions.Regex.Replace(s.Replace(' ', ' ').Replace(' ', ' '), @"\s+", " ").Trim();

    private (IRenderedComponent<FisOdemeBacaklari> c, List<FisBacagi> b) Karma(decimal toplam)
    {
        var b = new List<FisBacagi>
        {
            new() { Arac = "KART", TutarTl = toplam, KasaId = "ks1" },
            new() { Arac = "NAKIT", TutarTl = 0m, KasaId = "ks1" },
        };
        var c = Render<FisOdemeBacaklari>(p => p
            .Add(x => x.Bacaklar, b)
            .Add(x => x.Kasalar, [new KasaVeri("ks1", "Ana Kasa", "TRY", true)])
            .Add(x => x.PosCihazlari, [])
            .Add(x => x.FisToplami, toplam)
            .Add(x => x.Basit, true));
        return (c, b);
    }

    [Fact]
    public void DG30_SIFIR_fis_toplami_0_iken_5_TL_asim_satir_altinda_kalani_ata_0a_indirir_satir_silinmez()
    {
        var (c, b) = Karma(0m);
        c.Find("#islem-bacak-1-tutar").Input("5");
        Assert.Equal(2, b.Count);
        Assert.Equal("Bu tutar fiş toplamını 5 ₺ aşıyor. Azaltın ya da “Kalanı ata”ya basın.", Bosluk(c.Find("#islem-bacak-1-asim").TextContent));
        c.Find("#islem-bacak-kalan-ata").Click();
        Assert.Equal(0m, b[1].TutarTl);
        Assert.Equal(2, b.Count);
        Assert.Empty(c.FindAll("[id$=-asim]"));
    }

    [Fact]
    public void DG31_uc_satirda_kalani_ata_asimi_TEK_tikla_kapatir_hicbir_satir_silinmez_eksiye_inmez()
    {
        // TOPLU-7A: eskiden (GOC-C-2 tur 2) hedef satır tek başına yetmezse aşım yazılı kalıyordu (15.000 fazla). Artık
        // fazla öteki TL satırlardan sondan başa düşülür: aşım TEK tıkla kapanır, satır silinmez, tutar eksiye inmez.
        var (c, b) = Karma(10_000m);
        c.Find("#islem-bacak-ekle").Click();
        c.Find("#islem-bacak-2-tutar").Input("25000");
        c.Find("#islem-bacak-1-tutar").Input("3000");
        c.Find("#islem-bacak-kalan-ata").Click();
        Assert.Equal(3, b.Count);
        Assert.True(b.All(x => x.TutarTl >= 0m));
        Assert.Equal(10_000m, b.Sum(x => x.TutarTl));                    // fiş toplamına TAM iner
        Assert.Empty(c.FindAll("[id$=-asim]"));
        Assert.Empty(c.FindAll("#islem-bacak-kalan-ata"));
        Assert.Equal(0m, b[1].TutarTl);                                  // hedef (son yazılan) önce sıfırlanır
        Assert.Equal(10_000m, b[2].TutarTl);                             // kalan fazla sondaki satırdan düşer
    }

    [Fact]
    public void DG32_TURKCE_belirsiz_1_500_odeme_tutarinda_sessizce_1_5_ya_da_1500_OLMAZ()
    {
        var (c, b) = Karma(10_000m);
        c.Find("#islem-bacak-1-tutar").Input("1.500");
        Assert.Equal("true", c.Find("#islem-bacak-1-tutar").GetAttribute("aria-invalid"));
        Assert.DoesNotContain(b[1].TutarTl, new[] { 1.5m, 1_500m });
        Assert.Equal(2, b.Count);
    }

    [Fact]
    public void DG33_COK_BUYUK_fis_999_999_999_99_ikinci_satir_esit_ilk_satir_0_asim_yok()
    {
        var (c, b) = Karma(999_999_999.99m);
        c.Find("#islem-bacak-1-tutar").Input("999.999.999,99");
        Assert.Equal(999_999_999.99m, b[1].TutarTl);
        Assert.Equal(0m, b[0].TutarTl);
        Assert.Equal(2, b.Count);
        Assert.Empty(c.FindAll("[id$=-asim]"));
        Assert.Single(FisFark.Gonderilecek(b));
    }

    [Fact]
    public void DG34_KURUS_fis_0_01_esit_kurus_asim_yok_bir_kurus_fazla_asim_0_01()
    {
        var (c, b) = Karma(0.01m);
        c.Find("#islem-bacak-1-tutar").Input("0,01");
        Assert.Equal(0m, b[0].TutarTl);
        Assert.Empty(c.FindAll("[id$=-asim]"));
        c.Find("#islem-bacak-1-tutar").Input("0,02");
        Assert.Equal("Bu tutar fiş toplamını 0,01 ₺ aşıyor. Azaltın ya da “Kalanı ata”ya basın.", Bosluk(c.Find("#islem-bacak-1-asim").TextContent));
    }

    [Theory]
    [InlineData("1.000.000", 1_000_000)]   // tavan, tr-TR binlikli
    [InlineData("1", 1)]                   // alt sınır (tam sayı)
    public void DG35_acilis_ADET_turkce_tavan_ve_alt_sinir_kabul(string metin, int beklenen)
    {
        decimal? g = -1m;
        var c = Render<Adet>(p => p.Add(x => x.ElemanId, "u-acilis-stok").Add(x => x.Etiket, "Açılış stoğu")
            .Add(x => x.EnCok, AlanSinirlari.AcilisStokEnCok).Add(x => x.GecersizdeBosalt, true)
            .Add(x => x.DegerChanged, EventCallback.Factory.Create<decimal?>(this, v => g = v)));
        c.Find("#u-acilis-stok").Input(metin);
        Assert.Equal((decimal)beklenen, g);
    }

    [Theory]
    [InlineData("1.000.001")]   // tavan +1, tr-TR binlikli
    [InlineData("1.000,5")]     // binlik + kesir
    [InlineData("0,5")]
    public void DG36_acilis_ADET_turkce_tavan_ustu_ve_kesir_red(string metin)
    {
        decimal? g = -1m;
        var c = Render<Adet>(p => p.Add(x => x.ElemanId, "u-acilis-stok").Add(x => x.Etiket, "Açılış stoğu")
            .Add(x => x.EnCok, AlanSinirlari.AcilisStokEnCok).Add(x => x.GecersizdeBosalt, true)
            .Add(x => x.DegerChanged, EventCallback.Factory.Create<decimal?>(this, v => g = v)));
        c.Find("#u-acilis-stok").Input(metin);
        Assert.Null(g);
        Assert.Equal("true", c.Find("#u-acilis-stok").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void DG37_acilis_GRAM_has_4_hane_sinir_1_2345_red_1_234_kabul_kurus_yuvarlamasi_yok()
    {
        decimal? g = -1m;
        var c = Render<Sayi>(p => p.Add(x => x.ElemanId, "m").Add(x => x.Etiket, "Miktar").Add(x => x.EnAz, 0m).Add(x => x.EnCokOndalik, 3)
            .Add(x => x.EnCok, AlanSinirlari.AcilisStokEnCok).Add(x => x.GecersizdeBosalt, true)
            .Add(x => x.DegerChanged, EventCallback.Factory.Create<decimal?>(this, v => g = v)));
        c.Find("#m").Input("1,2345");
        Assert.Null(g);                       // 1,235'e/1,234'e sessizce yuvarlanmaz
        c.Find("#m").Input("1,234");
        Assert.Equal(1.234m, g);
        c.Find("#m").Input("1.000.000");
        Assert.Equal(1_000_000m, g);
    }
}
