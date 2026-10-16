using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-37c — AÇILIR PANEL SARMA ÖRTÜŞMESİ + ARAÇ ŞERİDİ KAPSAMI + ARTIKLAR
//
// P-37 doğrulayıcısının AÇIK bıraktığı üç kalem burada KAPANIR:
//   (1) PANEL_KESISIM = 0 — şerit ikinci satıra sardığında açık dropdown alt
//       satırın üstüne biniyordu (1280px'te ölçülen örtüşme 5056 px²).
//   (2) SAYFA_BASI_KAPSAM — 41/46 ekran ortak şeridi kullanıyordu.
//   (3) ARTIK_DOLU_BAYT = 0 — ağaçta dolu .mutorj/.bak dosyaları duruyordu.
//   (4) Secici iç arama kimliği sapması → YAZILI MUAFİYET, teste bağlandı.
//
// ⚠ NE ÖLÇÜLÜR, NE ÖLÇÜLMEZ (Anayasa md.1): birim testi TARAYICI DEĞİLDİR,
//   piksel hesaplamaz. Burada panelin örtüşmemesini SAĞLAYAN GEOMETRİK KURAL
//   (akışta kalma + kendi sütun genişliği) kaynak seviyesinde sınanır; gerçek
//   piksel kesişimi tarayıcıda koşan betikle ölçülür ve rapora sayıyla yazılır:
//   dokumanlar/09-uygulama-kayitlari/betikler/p37c-panel-olcum.js
//
// ⚠ Sayaç karşılıkları (dotnet/denetim.sh `# >>> P-37c` bloğu):
//   PANEL_AKIS_DISI = 0 · SAYFA_BASI_KAPSAM = 45/45 · ARTIK_DOLU_BAYT = 0
//
// ⚠ TOPLU-4 (TOPLU-2 kapanışı, 02.10) — BUGÜNKÜ DAVRANIŞ İKİ KATMANDIR:
//   (a) app.css TABAN kuralı (bu dosyanın (1) testleri): `.sayfa-basi-filtre
//       .secici-liste-kap` akışta, kendi sütun genişliğinde, z 41. `.sabit`
//       yerleşimde ve şerit dışı kullanımda geçerlidir.
//   (b) ortak.css T2-16: SayfaBasi şeridinde `…secici-liste-kap:not(.sabit)`
//       tetiğe bağlı AKIŞ DIŞI katmandır (absolute) — açık panel tetiği 320 px'e
//       genişletip sayfayı zıplatıyordu. Alt satıra binme kaygısını açıkken tüm
//       sayfayı kapatan `.secici-perde` (z 40 < panel 41) karşılar. (b) katmanı
//       Toplu4Testleri.T4_P37c_* ve denetim sayacı (t216/perde) ile sınanır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P37cPanelVeKapsamTestleri : BunitContext
{
    public P37cPanelVeKapsamTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    // ── Kaynak ağacı yardımcıları ──────────────────────────────────────────

    private static string DotnetKoku()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }

    private static string DepoKoku() => Directory.GetParent(DotnetKoku())!.FullName;

    private static string AppCss() =>
        File.ReadAllText(Path.Combine(DotnetKoku(), "Kuyumcu.Web", "wwwroot", "css", "app.css"));

    private static string SayfalarDizini() =>
        Path.Combine(DotnetKoku(), "Kuyumcu.Web", "Pages");

    /// <summary>app.css içindeki tek bir kural gövdesini ("seçici { … }") döndürür.</summary>
    private static string KuralGovdesi(string css, string secici)
    {
        // ⚠ Satır başına DEMİRLENİR: aynı seçici adı bir YORUM içinde de geçebilir
        //   (nitekim geçiyor) ve yorumdan sonraki '{' yanlış gövdeyi döndürürdü.
        var i = css.IndexOf("\n" + secici + " ", StringComparison.Ordinal);
        if (i < 0) i = css.IndexOf("\n" + secici + "{", StringComparison.Ordinal);
        if (i < 0) i = css.IndexOf("\n" + secici + ",", StringComparison.Ordinal);
        Assert.True(i >= 0, $"app.css'te '{secici}' kuralı YOK.");
        var acilis = css.IndexOf('{', i);
        var kapanis = css.IndexOf('}', acilis);
        Assert.True(acilis > 0 && kapanis > acilis, $"'{secici}' kuralının gövdesi okunamadı.");
        return css[(acilis + 1)..kapanis];
    }

    // ═══ (1) PANEL_KESISIM = 0 — GEOMETRİK KURAL ═══════════════════════════

    [Fact]
    public void Arac_seridindeki_acilir_panel_AKISTA_kalir_mutlak_konumlu_DEGILDIR()
    {
        // TABAN KURAL (app.css): akışta kalan panel kendi sütununu uzatır, esnek
        // satır büyür, alt satır İTİLİR. ⚠ TOPLU-4: SayfaBasi şeridinde bu taban
        // kuralın ÜSTÜNE ortak.css T2-16 yazar (tetiğe bağlı mutlak katman + perde);
        // bu test yalnız TABANI sınar — şerit davranışı Toplu4Testleri.T4_P37c_*'tadır.
        var govde = KuralGovdesi(AppCss(), ".sayfa-basi-filtre .secici-liste-kap");

        Assert.Contains("position: relative", govde, StringComparison.Ordinal);
        Assert.DoesNotContain("position: absolute", govde, StringComparison.Ordinal);
        Assert.DoesNotContain("position: fixed", govde, StringComparison.Ordinal);
        // Mutlak konumun kalıntısı da kalmamalı: top/left sıfırlanır.
        Assert.Contains("top: auto", govde, StringComparison.Ordinal);
        Assert.Contains("left: auto", govde, StringComparison.Ordinal);
    }

    [Fact]
    public void Acilir_panel_KENDI_sutununu_asmaz_yatay_kesisim_dogamaz()
    {
        // Panel akışta olsa bile kendi sütunundan TAŞARSA yandaki kontrolle
        // kesişirdi. Genişlik tetikle aynı tutulur: min-width 100% / max 100%.
        var govde = KuralGovdesi(AppCss(), ".sayfa-basi-filtre .secici-liste-kap");

        Assert.Contains("min-width: 100%", govde, StringComparison.Ordinal);
        Assert.Contains("max-width: 100%", govde, StringComparison.Ordinal);
        // Bileşenin genel kuralındaki 150% tavanı şeritte GEÇERSİZ olmalı.
        Assert.DoesNotContain("150%", govde, StringComparison.Ordinal);
    }

    [Fact]
    public void Panel_perdenin_ustunde_kalir_secenekler_tiklanabilir()
    {
        // `.secici-perde` z-index 40. Panel akışa alınırken `static` seçilseydi
        // z-index uygulanamaz, perde panelin üstünü kapar ve seçenek tıklanamazdı.
        var css = AppCss();
        var panel = KuralGovdesi(css, ".sayfa-basi-filtre .secici-liste-kap");
        var perde = KuralGovdesi(css, ".secici-perde");

        Assert.Contains("z-index: 41", panel, StringComparison.Ordinal);
        Assert.Contains("z-index: 40", perde, StringComparison.Ordinal);
    }

    [Fact]
    public void Panel_acikken_hizalama_USTTEN_yapilir_komsular_zıplamaz()
    {
        // Varsayılan `align-items: flex-end` ile satır uzayınca komşular AŞAĞI
        // kayardı. Panel açıkken hiza üstten yapılır → komşular YERİNDE kalır.
        var css = AppCss();

        Assert.Contains(".sayfa-basi:has(.sayfa-basi-filtre .secici-liste-kap)", css, StringComparison.Ordinal);
        Assert.Contains(".sayfa-basi-filtre:has(.secici-liste-kap)", css, StringComparison.Ordinal);

        var govde = KuralGovdesi(css, ".sayfa-basi-filtre:has(.secici-liste-kap)");
        Assert.Contains("align-items: flex-start", govde, StringComparison.Ordinal);
    }

    [Fact]
    public void Olculmemis_kesisim_iddiasi_yorumda_kalmaz_olcum_betigine_atif_var()
    {
        // md.1: "kesişim 0" beyanı ÖLÇÜMSÜZ yazılamaz. Yorum, ölçümün nerede
        // koştuğunu göstermek zorundadır.
        var css = AppCss();
        Assert.Contains("p37c-panel-olcum.js", css, StringComparison.Ordinal);
    }

    // ═══ (2) SAYFA_BASI_KAPSAM ═════════════════════════════════════════════

    /// <summary>Yazılı muafiyet taşıyan ekran(lar). Sessiz istisna YOKTUR.</summary>
    /// <remarks>
    /// PAKET S1-d: <c>Ekranlar.razor</c> TAM EKRAN karo menüdür — kurucunun tuvali
    /// (Ekranlar.dc.html) kendi koyu başlık şeridini çizer, ortak şerit (SayfaBasi)
    /// o şeridin yerine geçemez. Gerekçe dosyanın kendisinde yazılıdır.
    /// </remarks>
    // PAKET T-K3: Ayarlar — tuval Ayarlar.dc.html başlığı üst barda, sekmeler yanında (gerekçe dosyada).
    private static readonly string[] Muaflar = ["Panel.razor", "Ekranlar.razor", "Ayarlar.razor"];

    [Fact]
    public void Her_ekran_ortak_arac_seridini_kullanir_muafiyet_YAZILIDIR()
    {
        var sayfalar = Directory.GetFiles(SayfalarDizini(), "*.razor").OrderBy(x => x).ToArray();
        // 46 → 47: PAKET S1-d /ekranlar (muaf, gerekçe dosyasında).
        // 47 → 48: PAKET YASAL-1 YasalSinirlar.razor (SayfaBasi kullanır, muaf DEĞİL).
        // 48 → 49: PAKET AUTH-SIFIRLA SifreSifirla.razor (SayfaBasi kullanır, muaf DEĞİL).
        // 49 → 51: PAKET EKSIK-B1-SATINALMA SatinAlma.razor + Irsaliyeler.razor (ikisi de SayfaBasi kullanır, muaf DEĞİL).
        // 51 → 52: PAKET EKSIK-KUCUK VitrinEkrani.razor (/vitrin/ekran; SayfaBasi kullanır, muaf DEĞİL).
        Assert.Equal(52, sayfalar.Length);

        var eksik = sayfalar
            .Where(y => !Muaflar.Contains(Path.GetFileName(y)))
            .Where(y => !File.ReadAllText(y).Contains("<SayfaBasi", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToArray();

        Assert.Empty(eksik);
        Assert.Equal(49, sayfalar.Length - Muaflar.Length);   // AUTH-SIFIRLA: 45 → 46 · EKSIK-B1-SATINALMA: 46 → 48 · EKSIK-KUCUK: 48 → 49
    }

    [Fact]
    public void Muaf_ekran_gerekcesini_KENDI_dosyasinda_yazili_tasir()
    {
        foreach (var ad in Muaflar)
        {
            var metin = File.ReadAllText(Path.Combine(SayfalarDizini(), ad));
            Assert.Contains("SAYFA_BASI_MUAFIYETI", metin, StringComparison.Ordinal);
            // Gerekçe: neden geçemediği ve neyin bozulacağı yazılı olmalı.
            // Panel: karşılama başlığı; Ekranlar (S1-d): tuvalin tam ekran şeridi.
            Assert.Contains(ad switch
            {
                "Panel.razor" => "karsilama-bas",
                "Ekranlar.razor" => "ekranlar-ust",
                _ => "Ayarlar.dc.html",     // T-K3: tuval başlığı üst barda
            }, metin, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Bu_dalgada_sende_gecen_dort_ekran_serit_yuvalarini_kullanir()
    {
        // TEK DOKUNUŞ kaydı: bu dalgada açılan dört sayfa.
        (string Dosya, string Anahtar)[] beklenen =
        [
            ("Bildirimler.razor", "bildirimler"),
            ("Siparisler.razor", "siparisler"),
            ("SiparisDetay.razor", "siparis-detay"),
            ("Vezne.razor", "vezne"),
        ];

        foreach (var (dosya, anahtar) in beklenen)
        {
            var metin = File.ReadAllText(Path.Combine(SayfalarDizini(), dosya));
            Assert.Contains($"Anahtar=\"{anahtar}\"", metin, StringComparison.Ordinal);
            // Ekran başına hizalama CSS'i YAZILMAZ (SAYFA_BASI_DISI_DUZEN = 0).
            Assert.DoesNotContain("class=\"sayfa-basi", metin, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Serit_yuvalarina_tasinan_dugmeler_kaybolmadi()
    {
        // DoD EK (1): düğme ya istek atar ya disabled. Taşıma sırasında düğme
        // DÜŞÜRÜLMEDİĞİ ve kimliği DEĞİŞMEDİĞİ burada sabitlenir.
        (string Dosya, string[] Kimlikler)[] beklenen =
        [
            ("Bildirimler.razor", ["bildirim-yenile"]),
            ("Siparisler.razor", ["sip-durum-suzgec", "sip-geciken", "sip-yenile", "sip-yeni-ac"]),
            ("Vezne.razor", ["vz-durum-suzgec", "vz-yenile", "vz-yeni-ac"]),
        ];

        foreach (var (dosya, kimlikler) in beklenen)
        {
            var metin = File.ReadAllText(Path.Combine(SayfalarDizini(), dosya));
            foreach (var k in kimlikler)
            {
                // Her kimlik ekranda TAM OLARAK BİR kez DOĞAR (mükerrer DOM yok).
                // Sayılan şey `id="…"`dir; `for="…"`/`aria-controls="…"` atıflarıdır.
                // `ElemanId="…"` de sayılır: bileşen o kimliği kendi kökünde doğurur.
                // (İki kalıp AYRIKTIR: büyük I'lı `ElemanId=`, küçük i'li `id=` ile eşleşmez.)
                var adet = (metin.Split($"id=\"{k}\"").Length - 1)
                         + (metin.Split($"ElemanId=\"{k}\"").Length - 1);
                Assert.True(adet == 1, $"{dosya}: '{k}' {adet} kez geçiyor (beklenen 1).");
            }
        }
    }

    // ═══ (3) ARTIK_DOLU_BAYT = 0 ═══════════════════════════════════════════

    [Fact]
    public void Agacta_dolu_mutasyon_yedegi_veya_bak_dosyasi_KALMAZ()
    {
        // Kabuk kancası `rm`i engeller → artıklar SİLİNEMEZ, BOŞALTILIR.
        // Bundan sonrası için kural: mutasyon yedeği scratchpad'e alınır.
        var kok = DepoKoku();
        var dolu = Directory
            .EnumerateFiles(kok, "*", SearchOption.AllDirectories)
            .Where(y => y.EndsWith(".mutorj", StringComparison.Ordinal)
                     || y.EndsWith(".bak", StringComparison.Ordinal)
                     || y.EndsWith(".mutbak", StringComparison.Ordinal)
                     || y.EndsWith(".orig", StringComparison.Ordinal))
            .Where(y => !y.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(y => !y.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(y => !y.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(y => new FileInfo(y).Length > 0)
            .Select(y => Path.GetRelativePath(kok, y))
            .OrderBy(y => y)
            .ToArray();

        Assert.Empty(dolu);
    }

    // ═══ (4) KİMLİK SAPMASI — YAZILI MUAFİYET, TESTE BAĞLI ═════════════════

    [Fact]
    public void Secici_ic_arama_kutusu_ara_girdi_kalibindadir_MUAFIYET()
    {
        // MUAFİYET GEREKÇESİ (rapor § kimlik sapması): Emanet ekranında sayfanın
        // KENDİ sunucu araması `<Arama ElemanId="e-cari-ara">` ile `#e-cari-ara`
        // kimliğini doğurur. Secici eski `{ElemanId}-ara` kalıbına dönseydi
        // `<Secici ElemanId="e-cari">` AYNI kimliği ikinci kez doğururdu →
        // mükerrer DOM kimliği (geçersiz HTML, aria-controls/label bağı bozulur).
        // Bu yüzden kalıp `-ara-girdi` OLARAK KALIR ve burada sabitlenir.
        var c = Render<Secici>(p => p
            .Add(x => x.ElemanId, "e-cari")
            .Add(x => x.Etiket, "Müşteri")
            .Add(x => x.Secenekler, new List<SecenekOgesi> { new("1", "Ayşe"), new("2", "Mehmet") }));

        c.Find("#e-cari").Click();

        Assert.NotNull(c.Find("#e-cari-ara-girdi"));
        // Eski kalıp ASLA doğmaz: sayfanın kendi `#e-cari-ara` kutusuyla çakışırdı.
        Assert.Empty(c.FindAll("#e-cari-ara"));
    }

    [Fact]
    public void Emanet_ekraninda_iki_arama_yuzeyi_AYRI_kimlik_tasir()
    {
        var metin = File.ReadAllText(Path.Combine(SayfalarDizini(), "Emanet.razor"));

        // Sayfanın SUNUCU araması ve Secici'nin YERİNDE süzmesi iki farklı iştir.
        Assert.Contains("<Arama ElemanId=\"e-cari-ara\"", metin, StringComparison.Ordinal);
        Assert.Contains("<Secici ElemanId=\"e-cari\"", metin, StringComparison.Ordinal);
        // Sapma sessiz değil: gerekçe sayfanın kendi yorumunda yazılıdır.
        Assert.Contains("mükerrer DOM kimliği yok", metin, StringComparison.Ordinal);
    }
}
