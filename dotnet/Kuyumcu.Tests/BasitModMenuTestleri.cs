using Bunit;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// BASİT MOD — KENAR ÇUBUĞU (paket UX-3 Faz A · Aşkın K8 · Mehmet K8)
//
// ⚠ KenarCubugu bu dosyadan ÖNCE hiçbir testte çizilmiyordu (Tests'te 0 dosya).
//   Kurulum: sahte /yetkiler/benim + OturumServisi + ArayuzModuServisi.
//
// Sınanan sözleşme:
//   • S1-g (2026-09-24): basit modda menü ARTIK DARALMAZ — kurucunun tuvali
//     (Panel.dc.html basit, Tezgah.dc.html) basit yüzde de yetkili her ekranı
//     6 katlanır grupta gösterir ("Menüde HİÇBİR ekran kaybolmadı", n2).
//     Eski "basit modda menü DARALIR (BasitMenu listesi)" sözleşmesi aşağıdaki
//     üç testte YENİDEN YAZILDI (eski/yeni hâli her testin başında).
//   • yetkisiz kullanıcı PROFESYONEL modda da o bağlantıyı GÖRMEZ
//   • yetkisiz kullanıcı BASİT modda da GÖRMEZ  → mod, yetkiyi GEVŞETMEZ (M-UX3-06)
//   • Kılavuz/Ayarlar alt bloğu her modda durur
// ═════════════════════════════════════════════════════════════════════════════

public sealed class BasitModMenuTestleri : BunitContext
{
    /// <summary>Sahte /yetkiler/benim — modül listesi test başına ayarlanır.</summary>
    private sealed class SahteYetkiApi(Func<string> modulJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"moduller":{{modulJson()}}}""",
                    System.Text.Encoding.UTF8, "application/json"),
            });
    }

    private string moduller = """["cari","urun","islem","kasa","banka","fatura","rapor","kullanici"]""";

    public BasitModMenuTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteYetkiApi(() => moduller))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        // P-36b kapanışı (P-37 artığı): KenarCubugu favori/sıra tercihini bu
        // servisten okur; Program.cs'te kayıtlıydı, fikstürlerde DEĞİLDİ →
        // kabuk çizen her test DI hatasıyla kırmızıydı.
        Services.AddScoped<MenuTercihServisi>();
    }

    private IRenderedComponent<KenarCubugu> Ciz(string rol, string etkinMod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1", null, etkinMod));
        var e = Render<KenarCubugu>();
        // Yetkiler eşzamansız yükleniyor (HazirlaAsync) — çizim tamamlansın.
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kenar-nav a")));
        return e;
    }

    private static List<string> Yollar(IRenderedComponent<KenarCubugu> e) =>
        e.FindAll(".kenar-nav a").Select(a => a.GetAttribute("href")!).ToList();

    [Fact]
    public void Profesyonel_modda_SAHIP_tum_menuyu_gorur()
    {
        var yollar = Yollar(Ciz("SAHIP", ArayuzModuServisi.Profesyonel));

        Assert.Contains("/denetim-izi", yollar);
        Assert.Contains("/isletme", yollar);
        Assert.Contains("/emanet", yollar);
        Assert.Contains("/kur", yollar);
        Assert.Contains("/fiyatlandirma", yollar);
        Assert.True(yollar.Count > BasitMenu.Yollar.Length,
            "profesyonel menü basit menüden geniş olmalı");
    }

    [Fact]
    public void Basit_modda_menu_YETKILI_HER_ekrani_tasir_BasitMenu_ile_daralmaz()
    {
        // S1-g — ESKİ: Basit_modda_menu_YALNIZ_BasitMenu_listesidir (8 yol).
        // YENİ: tuval Panel.dc.html satır 30-43 — "Tüm ekranlar (38)" ve 6 grubun
        // önizlemesi basit yüzde 38 ekranı taşır. Menü = yetkili ekranlar.
        var yollar = Yollar(Ciz("SAHIP", ArayuzModuServisi.Basit));

        Assert.Equal(MenuTanimi.Ogeler.Select(o => o.Yol).OrderBy(x => x), yollar.OrderBy(x => x));
        Assert.True(yollar.Count > BasitMenu.Yollar.Length);
    }

    [Fact]
    public void Basit_modda_profesyonel_ekranlar_menude_KAYBOLMAZ_kapali_grupta_durur()
    {
        // S1-g — ESKİ: Basit_modda_profesyonel_ekranlar_menude_YOKTUR (16 yol
        // basit menüde olmamalıydı). YENİ: tuval Panel.dc.html satır 38-43 —
        // bu ekranlar KAPALI grupların önizlemesinde yazar ve grup açılınca
        // gidilir; menüden kaybolmaz, yalnız katlanır.
        var e = Ciz("SAHIP", ArayuzModuServisi.Basit);
        var yollar = Yollar(e);

        foreach (var yol in new[]
        {
            "/denetim-izi", "/uyum", "/fiyatlandirma", "/kur", "/emanet",
            "/isletme", "/yetkiler", "/banka", "/faturalar", "/sayim",
            "/veri-disa-aktarim", "/gider-pusulalari", "/kullanicilar",
            "/urunler/agac", "/raporlar/kar-zarar", "/raporlar/vade-takibi",
        })
            Assert.Contains(yol, yollar);
        // Panel rotasında Yönetim grubu kapalı: satırı katlı, adı önizlemede.
        Assert.True(e.Find("#kenar-satir-denetim-izi").HasAttribute("hidden"));
        Assert.Contains(Kuyumcu.Core.I18n.MenuAdlari.Tr.KimNeYapti.Kisa,
            e.Find("#kenar-onizleme-yonetim").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Yetkisiz_kullanici_baglantiyi_gormez_PROFESYONEL_modda()
    {
        moduller = "[]";   // hiçbir modüle yetkisi yok
        var yollar = Yollar(Ciz("TEZGAHTAR", ArayuzModuServisi.Profesyonel));

        Assert.DoesNotContain("/cariler", yollar);
        Assert.DoesNotContain("/urunler", yollar);
        Assert.DoesNotContain("/islemler", yollar);
        Assert.DoesNotContain("/kasalar", yollar);
        Assert.DoesNotContain("/raporlar", yollar);
        Assert.Contains("/", yollar);   // Panel yetki istemez
    }

    [Fact]
    public void Yetkisiz_kullanici_baglantiyi_BASIT_modda_da_gormez()
    {
        // M-UX3-06 hedefi: menü daraltması Yetki.Erisebilir'in YERİNE geçerse
        // (VEYA / yalnız mod kontrolü) bu test kırmızıya döner.
        moduller = "[]";
        var yollar = Yollar(Ciz("TEZGAHTAR", ArayuzModuServisi.Basit));

        Assert.DoesNotContain("/cariler", yollar);
        Assert.DoesNotContain("/urunler", yollar);
        Assert.DoesNotContain("/urunler/etiketler", yollar);
        Assert.DoesNotContain("/islemler", yollar);
        Assert.DoesNotContain("/kasalar", yollar);
        Assert.DoesNotContain("/raporlar", yollar);
        Assert.Contains("/", yollar);
    }

    [Fact]
    public void Yetkili_TEZGAHTAR_basit_modda_calisma_ekranlarini_gorur()
    {
        moduller = """["cari","urun","islem","kasa","rapor"]""";
        var yollar = Yollar(Ciz("TEZGAHTAR", ArayuzModuServisi.Basit));

        Assert.Contains("/islemler", yollar);
        Assert.Contains("/cariler", yollar);
        Assert.Contains("/urunler", yollar);
        Assert.Contains("/kasalar", yollar);
        Assert.Contains("/raporlar", yollar);
        // S1-g — ESKİ: "yetkisi olsa bile basit modda kur/emanet menüde yok".
        // YENİ (tuval Tezgah.dc.html satır 33-39, tezgâhtar basit yüzü): Emanet
        // Günlük iş grubunda, Kur Para ve banka grubunda DURUR — yetki varsa.
        Assert.Contains("/kur", yollar);
        Assert.Contains("/emanet", yollar);
        // Yetki yine son sözü söyler: modülü verilmeyen ekran yoktur (M-UX3-06).
        Assert.DoesNotContain("/banka", yollar);
        Assert.DoesNotContain("/faturalar", yollar);
    }

    [Fact]
    public void Kilavuz_ve_Ayarlar_her_modda_alt_blokta_kalir()
    {
        foreach (var mod in new[] { ArayuzModuServisi.Basit, ArayuzModuServisi.Profesyonel })
        {
            var e = Ciz("SAHIP", mod);
            var alt = e.FindAll(".kenar-alt a").Select(a => a.GetAttribute("href")).ToList();
            Assert.Contains("/kilavuz", alt);
            Assert.Contains("/ayarlar", alt);
        }
    }

    [Fact]
    public void BasitMenu_listesi_TEK_yerdedir_ve_yedi_yol_icerir()
    {
        // 6-A doğrulayıcı devri (UX-1-C4): /bildirimler EKLENDİ — bildirim rozeti
        // basit modda görünüyor ama sayfaya gidilemiyordu. Sayı 7 → 8.
        Assert.Equal(8, BasitMenu.Yollar.Length);
        Assert.Equal(BasitMenu.Yollar.Length, BasitMenu.Yollar.Distinct().Count());
        Assert.True(BasitMenu.Icerir("/islemler"));
        Assert.True(BasitMenu.Icerir("/bildirimler"));
        Assert.False(BasitMenu.Icerir("/denetim-izi"));
    }
}
