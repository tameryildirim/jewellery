using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// KABUK BİLEŞENLERİ — paket UX-2 Faz A2 (K6 · K7 · MÜHÜR 2 DoD · tur 2 bulgusu)
//
// A2 SIFIR sayfa DOM değişikliği paketidir: bileşenler sayfalara BAĞLANMAZ,
// ama davranışları burada SINANIR ki dalga 2c'nin B dilimi hazır bir sözleşme
// devralsın.
//
// ⚠ bUnit CSS UYGULAMAZ: "375px'te yatay kaydırma yok" burada ölçülemez
//   (o kanıt canlı turdadır). Burada ölçülen şey DOM SÖZLEŞMESİ ve OLAYLARDIR.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KabukBilesenTestleri : BunitContext
{
    private const string Ac = "kuyumcuCekmece.ac";
    private const string Kap = "kuyumcuCekmece.kapat";

    public KabukBilesenTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid(Ac, _ => true).SetVoidResult();
        JSInterop.SetupVoid(Kap, _ => true).SetVoidResult();
        Services.AddScoped<DilServisi>();
    }

    // ── SayfaBasi ────────────────────────────────────────────────────────────

    [Fact]
    public void SayfaBasi_baslik_aciklama_ve_TEK_birincil_eylem_yuvasini_cizer()
    {
        var e = Render<SayfaBasi>(p => p
            .Add(x => x.Anahtar, "cariler")
            .Add(x => x.Baslik, "Cariler")
            .Add(x => x.Aciklama, "Müşteri ve tedarikçi hesapları.")
            .Add(x => x.Eylem, (RenderFragment)(b =>
            {
                b.OpenElement(0, "button");
                b.AddAttribute(1, "id", "cari-yeni");
                b.AddContent(2, "Yeni cari");
                b.CloseElement();
            })));

        Assert.Equal("Cariler", e.Find("#sayfa-basi-cariler-baslik").TextContent);
        Assert.Equal("Müşteri ve tedarikçi hesapları.", e.Find("#sayfa-basi-cariler-aciklama").TextContent);
        Assert.NotNull(e.Find("#sayfa-basi-cariler-eylem #cari-yeni"));
        // aria-labelledby hedefi olacak başlık id'si sözleşmededir
        Assert.Equal("sayfa-basi-cariler", e.Find(".sayfa-basi").Id);
        // Satır içi style YASAK (CSP style-src 'unsafe-inline' kaldırma yolu)
        Assert.DoesNotContain("style=", e.Markup);
    }

    [Fact]
    public void SayfaBasi_aciklama_bos_ise_HIC_cizilmez()
    {
        var e = Render<SayfaBasi>(p => p
            .Add(x => x.Anahtar, "kur").Add(x => x.Baslik, "Kur"));

        Assert.Empty(e.FindAll("#sayfa-basi-kur-aciklama"));
        Assert.Empty(e.FindAll(".sayfa-basi-eylem"));
    }

    // ── Cekmece (K6) ─────────────────────────────────────────────────────────

    private IRenderedComponent<Cekmece> CekmeceCiz(
        bool acik = true, bool kaydediliyor = false, bool kaydetAktif = true,
        string? kilit = null, Action<string>? olay = null)
        => Render<Cekmece>(p =>
        {
            p.Add(x => x.Acik, acik)
             .Add(x => x.Varlik, "cari")
             .Add(x => x.Baslik, "Cari düzenle")
             .Add(x => x.Kaydediliyor, kaydediliyor)
             .Add(x => x.KaydetAktif, kaydetAktif)
             .Add(x => x.KilitSebebi, kilit)
             .Add(x => x.Kapat, EventCallback.Factory.Create(this, () => olay?.Invoke("kapat")))
             .Add(x => x.Kaydet, EventCallback.Factory.Create(this, () => olay?.Invoke("kaydet")));
        });

    [Fact]
    public void Cekmece_K6_id_ve_ARIA_sozlesmesini_karsilar()
    {
        var e = CekmeceCiz();

        var panel = e.Find("#cekmece-cari");
        Assert.Equal("dialog", panel.GetAttribute("role"));
        Assert.Equal("true", panel.GetAttribute("aria-modal"));
        Assert.Equal("cekmece-cari-baslik", panel.GetAttribute("aria-labelledby"));
        Assert.NotNull(e.Find("#cekmece-kapat"));
        Assert.NotNull(e.Find("#cekmece-cari-govde"));
        Assert.NotNull(e.Find("#cekmece-cari-kaydet"));
        Assert.NotNull(e.Find("#cekmece-cari-iptal"));
        Assert.DoesNotContain("style=", e.Markup);
    }

    [Fact]
    public void Cekmece_acilinca_ODAK_ICERI_kapaninca_TETIKLEYICIYE_doner()
    {
        // Tur 2 bulgusunun kökü: panel açılışta odağı almazsa Escape hiç gelmez.
        var e = CekmeceCiz();
        Assert.Single(JSInterop.Invocations[Ac]);

        e.Render(p => p.Add(x => x.Acik, false));
        Assert.Single(JSInterop.Invocations[Kap]);
    }

    [Fact]
    public void Cekmece_ESCAPE_ile_kapanir()
    {
        string? son = null;
        var e = CekmeceCiz(olay: x => son = x);

        e.Find("#cekmece-cari").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal("kapat", son);
    }

    [Fact]
    public void Cekmece_arka_plan_ve_IPTAL_kapatir_KAYDET_olayi_yukari_verir()
    {
        string? son = null;
        var e = CekmeceCiz(olay: x => son = x);

        e.Find("#cekmece-cari-kaydet").Click();
        Assert.Equal("kaydet", son);

        e.Find("#cekmece-cari-iptal").Click();
        Assert.Equal("kapat", son);

        e.Find("#cekmece-cari-arka").Click();
        Assert.Equal("kapat", son);
    }

    [Fact]
    public void Cekmece_KAYDEDILIYORKEN_kaydedildi_YAZMAZ_ve_dugmeyi_kilitler()
    {
        // Parasal kayıtta iyimser kaydetme YASAK (Anayasa md.1'in arayüz karşılığı).
        string? son = null;
        var e = CekmeceCiz(kaydediliyor: true, olay: x => son = x);

        var dugme = e.Find("#cekmece-cari-kaydet");
        Assert.True(dugme.HasAttribute("disabled"));
        Assert.Contains("Kaydediliyor", dugme.TextContent);
        Assert.DoesNotContain("Kaydedildi", e.Markup);

        dugme.Click();
        Assert.Null(son);   // kilitli düğme istek ATMAZ

        // DoD: kilitli düğme SEBEBİNİ söyler
        Assert.Contains("Sunucu onayı", e.Find("#cekmece-cari-kilit").TextContent);
    }

    [Fact]
    public void Cekmece_KILITLI_kaydet_sebebini_ekranda_yazar()
    {
        var e = CekmeceCiz(kaydetAktif: false, kilit: "Cari adı zorunlu");

        Assert.True(e.Find("#cekmece-cari-kaydet").HasAttribute("disabled"));
        Assert.Contains("Cari adı zorunlu", e.Find("#cekmece-cari-kilit").TextContent);
    }

    [Fact]
    public void Cekmece_kapali_iken_DOM_da_HIC_yoktur()
    {
        var e = CekmeceCiz(acik: false);
        Assert.Empty(e.FindAll("#cekmece-cari"));
        Assert.Empty(e.Markup.Trim());
    }

    // ── SatirIciDuzenle ──────────────────────────────────────────────────────

    private IRenderedComponent<SatirIciDuzenle> SatirCiz(
        string? deger = "Ahmet", bool bosOlabilir = false, Action<string>? kaydedildi = null)
        => Render<SatirIciDuzenle>(p => p
            .Add(x => x.Varlik, "cari")
            .Add(x => x.Alan, "aciklama")
            .Add(x => x.Id, "42")
            .Add(x => x.Etiket, "Açıklama")
            .Add(x => x.Deger, deger)
            .Add(x => x.BosOlabilir, bosOlabilir)
            .Add(x => x.Kaydedildi, EventCallback.Factory.Create<string>(this, v => kaydedildi?.Invoke(v))));

    [Fact]
    public void SatirIci_tiklayinca_GIRDI_acilir_ve_id_sozlesmesi_korunur()
    {
        var e = SatirCiz();

        var tetik = e.Find("#si-cari-aciklama-42-ac");
        Assert.Equal("true", tetik.GetAttribute("data-duzenlenebilir"));
        tetik.Click();

        var girdi = e.Find("#si-cari-aciklama-42");
        Assert.Equal("Ahmet", girdi.GetAttribute("value"));
        Assert.NotNull(e.Find("#si-cari-aciklama-42-kaydet"));
        Assert.NotNull(e.Find("#si-cari-aciklama-42-iptal"));
    }

    [Fact]
    public void SatirIci_ENTER_kaydeder_ESC_eski_degere_doner()
    {
        string? yazilan = null;
        var e = SatirCiz(kaydedildi: v => yazilan = v);

        e.Find("#si-cari-aciklama-42-ac").Click();
        e.Find("#si-cari-aciklama-42").Input("Mehmet");
        e.Find("#si-cari-aciklama-42").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal("Mehmet", yazilan);

        // Esc: taslak atılır, görünen değer ESKİ değerdir (ebeveyn güncellemedi)
        yazilan = null;
        e.Find("#si-cari-aciklama-42-ac").Click();
        e.Find("#si-cari-aciklama-42").Input("Silinecek");
        e.Find("#si-cari-aciklama-42").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(yazilan);
        Assert.Equal("Ahmet", e.Find("#si-cari-aciklama-42-ac").TextContent);
    }

    [Fact]
    public void SatirIci_BOS_gonderimde_SESSIZ_kalmaz_alan_alti_mesaj_verir()
    {
        string? yazilan = null;
        var e = SatirCiz(kaydedildi: v => yazilan = v);

        e.Find("#si-cari-aciklama-42-ac").Click();
        e.Find("#si-cari-aciklama-42").Input("   ");
        e.Find("#si-cari-aciklama-42-kaydet").Click();

        Assert.Null(yazilan);                        // istek atılmaz
        Assert.Contains("boş bırakılamaz", e.Find("#si-cari-aciklama-42-hata").TextContent);
    }

    [Fact]
    public void SatirIci_KAYDEDILIYORKEN_girdi_ve_dugmeler_kilitli_bekliyor_rozeti_vardir()
    {
        var e = SatirCiz();
        e.Find("#si-cari-aciklama-42-ac").Click();
        e.Render(p => p.Add(x => x.Kaydediliyor, true));

        Assert.True(e.Find("#si-cari-aciklama-42").HasAttribute("disabled"));
        Assert.True(e.Find("#si-cari-aciklama-42-kaydet").HasAttribute("disabled"));
        Assert.NotNull(e.Find("#si-cari-aciklama-42-bekliyor"));
    }

    // ── ModalPencere (QA turu 2 grup 1-2 bulgusu) ────────────────────────────

    [Fact]
    public void ModalPencere_acilinca_ODAGI_PANELE_tasir_ve_ESCAPE_kapatir()
    {
        var kapandi = false;
        var e = Render<ModalPencere>(p => p
            .Add(x => x.Acik, true)
            .Add(x => x.Baslik, "Geçmiş")
            .Add(x => x.Kapat, EventCallback.Factory.Create(this, () => kapandi = true)));

        // Odak panele taşınmazsa tuş olayı panele HİÇ ulaşmaz (tur 2 kök nedeni)
        Assert.Single(JSInterop.Invocations[Ac]);

        e.Find(".modal-panel").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.True(kapandi);
    }

    [Fact]
    public void ModalPencere_kapaninca_ODAK_TETIKLEYICIYE_geri_verilir()
    {
        var e = Render<ModalPencere>(p => p
            .Add(x => x.Acik, true).Add(x => x.Baslik, "Kalemler"));

        e.Render(p => p.Add(x => x.Acik, false));

        Assert.Single(JSInterop.Invocations[Kap]);
    }

    // ── Kabuk durumu (dar ekran çekmecesi) ───────────────────────────────────

    [Fact]
    public void KabukServisi_menuyu_acar_kapatir_ve_YALNIZ_degisince_haber_verir()
    {
        var k = new KabukServisi();
        var sayac = 0;
        k.Degisti += () => sayac++;

        Assert.False(k.MenuAcik);
        k.Kapat();                 // zaten kapalı → olay YOK
        Assert.Equal(0, sayac);

        k.Degistir();
        Assert.True(k.MenuAcik);
        Assert.Equal(1, sayac);

        k.Ac();                    // zaten açık → olay YOK
        Assert.Equal(1, sayac);

        k.Kapat();
        Assert.False(k.MenuAcik);
        Assert.Equal(2, sayac);
    }
}
