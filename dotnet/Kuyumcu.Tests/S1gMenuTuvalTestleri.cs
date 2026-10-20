using System.Net;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-g — MENÜ VE ÜST BAR TUVALE UYUM (kurucu 24.09 06:55: "menüyü felan
// kontrol et, olmamış")
//
// TEK DOĞRULUK KAYNAĞI: dokumanlar/02-mimari/sadelestirme/tuval/project/
//   Panel.dc.html (basit) · Profesyonel.dc.html · Tezgah.dc.html · Ekranlar.dc.html
//
// Orkestratörün gerçek tarayıcıda ölçtüğü sapmaların kilitleri:
//   (1) KRİTİK — kapalı grubun satırları `hidden` taşıyor ama GÖRÜNÜYORDU:
//       `.kenar-satir{display:flex}` UA `[hidden]{display:none}` kuralını
//       eziyordu. bUnit CSS uygulamaz → bu dosya CSS DOSYASINI okur ve genel
//       `[hidden]{display:none !important}` kuralını kilitler. Gerçek görünürlük
//       ölçümü tarayıcıdadır (rapor § canlı ölçüm).
//   (2) basit modda 6 grup (MenuKatlamaTestleri / BasitModMenuTestleri'nde).
//   (3) ekran adları tuvalle BİREBİR (ad tablosu aşağıda).
//   (4) menüde yıldız 0, tutamaç 0; favori ekleme "Tüm ekranlar"da.
//   (5) kullanıcı kartı: baş harf + ad + rol (+ PRO'da " · <şube>").
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S1gMenuTuvalTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol)> istekler = [];
    private string tercihler = "{}";
    private string moduller = """["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","evrak","siparis","bildirim"]""";

    private sealed class SahteApi(
        List<(string, string)> kayit, Func<string> tercihler, Func<string> moduller) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            kayit.Add((istek.Method.Method, yol));
            // ⚠ bUnit sözleşmesi: "{}" varsayılanı bırakılmaz — her uca gerçekçi yanıt.
            var cevap = yol switch
            {
                var y when y.Contains("/tercihler/benim", StringComparison.Ordinal) =>
                    $$"""{"tercihler":{{tercihler()}},"mesaj":"Tercihler kaydedildi"}""",
                var y when y.Contains("/yetkiler/benim", StringComparison.Ordinal) =>
                    $$"""{"moduller":{{moduller()}}}""",
                var y when y.EndsWith("/subeler", StringComparison.Ordinal) =>
                    """[{"id":"s1","ad":"Merkez şube","aktif":true}]""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    public S1gMenuTuvalTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => tercihler, () => moduller))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<MenuTercihServisi>();
    }

    // TEMA TEMİZLİĞİ 17.10: kenar çubuğu kaldırıldı; menünün bütününe "Tüm ekranlar" sayfasından ulaşılır.
    private IRenderedComponent<Ekranlar> TumEkranlar()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1", null, ArayuzModuServisi.Profesyonel));
        return Render<Ekranlar>().KareleriBekle();
    }

    private static string CssYolu(string ad) =>
        Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web", "wwwroot", "css", ad);

    // ── (1) GENEL [hidden] KURALI — KRİTİK ──────────────────────────────────

    [Fact]
    public void Genel_hidden_kurali_app_css_BASINDA_ve_IMPORTANT()
    {
        var css = File.ReadAllText(CssYolu("app.css"));
        var kural = Regex.Match(css, @"(?m)^\[hidden\]\s*\{\s*display:\s*none\s*!important;\s*\}");

        Assert.True(kural.Success, "app.css'te genel `[hidden] { display: none !important; }` kuralı YOK — " +
            "`.kenar-satir{display:flex}` gibi her sınıf kuralı `hidden` özniteliğini ezer (S1-g KRİTİK)");
        // Kural sınıf kurallarından ÖNCE, dosyanın başındadır (tek yerde, bulunur).
        var ilkSinifKurali = Regex.Match(css, @"(?m)^\.[a-z]").Index;
        Assert.True(kural.Index < ilkSinifKurali, "genel [hidden] kuralı app.css'in BAŞINDA olmalı");
        // !important olmadan kural .kenar-satir'ı YENEMEZ (özgüllük eşit/düşük).
        Assert.DoesNotMatch(@"(?m)^\[hidden\]\s*\{\s*display:\s*none\s*;\s*\}", css);
    }

    // TEMA TEMİZLİĞİ 17.10: Katli_satirlar_hidden_OZNITELIGI_tasir_ve_display_ezen_sinif_kurali_var kaldırıldı — katlanan kenar satırı (.kenar-satir) ve CSS'i kaldırıldı; genel [hidden] kuralı yukarıdaki testte, Yıldız'da hidden ile katlanan hesap menüsünün kuralı Yildiz2Testleri.Y2_40 ve Y2_13'te.

    // ── (3) ADLAR TUVALLE BİREBİR ───────────────────────────────────────────

    /// <summary>
    /// Rota → tuvaldeki ad (Ekranlar.dc.html "Tüm ekranlar" + Panel.dc.html
    /// menü). Bir ad eskiye dönerse (ör. "Cariler") bu tablo KIRMIZIDIR.
    /// </summary>
    public static readonly (string Yol, string Ad)[] TuvalAdlari =
    [
        ("/", "Panel"), ("/islemler", "Satış"), ("/emanet", "Emanet"),
        ("/siparisler", "Sipariş ve tamir"), ("/bildirimler", "Bildirimler"),
        ("/cariler", "Müşteriler"), ("/urunler", "Ürünler"), ("/urunler/agac", "Ürün ağacı"),
        ("/urunler/etiketler", "Etiketler"), ("/fiyatlandirma", "Fiyat listeleri"), ("/sayim", "Sayım"),
        ("/satin-alma", "Satın alma"), ("/irsaliyeler", "İrsaliyeler"),   // PAKET EKSIK-B1-SATINALMA
        ("/kasalar", "Kasalar"), ("/vezne", "Kasa kuyruğu"), ("/gise", "Gişe zimmeti"), ("/pos", "POS cihazları"),
        ("/pos/tahsilatlar", "POS alacakları"), ("/banka", "Banka"), ("/evraklar", "Çek ve senet"),
        ("/evraklar/takvim", "Vade takvimi"), ("/kur", "Kur"), ("/gider", "Gider ve para aktarımı"),
        ("/hurda", "Hurda ve ramat"),
        ("/faturalar", "Faturalar"), ("/gider-pusulalari", "Gider pusulaları"), ("/uyum", "Uyum (MASAK)"), ("/kvkk", "KVKK"),   // D1-19
        // PAKET YASAL-1 tur 2 (tasarım hakemi KRİTİK): tuvalde olmayan YENİ ekran — şartname § 4.5 adı.
        ("/yasal-sinirlar", "Yasal sınırlar"),
        ("/raporlar", "Rapor merkezi"), ("/raporlar/kar-zarar", "Kâr-zarar"),
        ("/raporlar/vade-takibi", "Vade takibi"), ("/raporlar/islem-defteri", "İşlem defteri"),
        ("/raporlar/satis-analizi", "Satış analizi"), ("/raporlar/kasa", "Kasa raporu"),
        ("/kullanicilar", "Kullanıcılar"), ("/isletme", "İşletme"), ("/yetkiler", "Yetkiler"),
        ("/denetim-izi", "Kim ne yaptı"), ("/kurulum", "Kurulum"),
        ("/veri-disa-aktarim", "Veri dışa aktar"), ("/veri-ice-aktarim", "Veri içe aktar"),
        ("/yedekler", "Yedekler"),
        ("/kilavuz", "Kılavuz"), ("/ayarlar", "Ayarlar"),
    ];

    [Fact]
    public void Menu_adlari_TUVALLE_BIREBIR_ve_sayfa_basligi_menu_adiyla_AYNI()
    {
        var tumu = MenuTanimi.Ogeler.Concat(MenuTanimi.AltBlok).ToList();
        Assert.Equal(tumu.Count, TuvalAdlari.Length);
        foreach (var (yol, ad) in TuvalAdlari)
        {
            var oge = tumu.Single(o => o.Yol == yol);
            Assert.Equal(ad, oge.Etiket(Sozlukler.Tr));
            // Üst bar başlığı (RotaBasliklari) menü adıyla AYNI (UX-S31).
            Assert.Equal(ad, RotaBasliklari.Baslik(yol, Sozlukler.Tr));
            // İngilizce karşılık boş değil ve TR ile aynı dizge değil (Panel/Kur/Banka hariç ortak).
            Assert.False(string.IsNullOrWhiteSpace(oge.Etiket(Sozlukler.En)));
        }
    }

    [Fact]
    public void Grup_basliklari_TUVALDEKI_gibi_CUMLE_DUZENINDE()
    {
        string[] tuval = ["Günlük iş", "Müşteri ve stok", "Para ve banka", "Belge ve uyum", "Raporlar", "Yönetim"];
        Assert.Equal(tuval, MenuTanimi.GrupSirasi.Select(g => MenuTanimi.GrupEtiketi(g)(Sozlukler.Tr)).ToArray());

        // TEMA TEMİZLİĞİ 17.10: S1-g bloğunun .kenar-grup-dugme text-transform kuralı kenar çubuğuyla
        // birlikte kaldırıldı. Yıldız "Tüm ekranlar" bölüm başlığı metni sözlükteki cümle düzenindeki
        // adla BAŞLAR (DOM'da büyük harfe çevrilmiş kopya yok).
        var e = TumEkranlar();
        foreach (var g in MenuTanimi.GrupSirasi)
            Assert.StartsWith(MenuTanimi.GrupEtiketi(g)(Sozlukler.Tr) + " ",
                e.Find($"#ekranlar-grup-baslik-{MenuTanimi.GrupSlug(g)}").TextContent.Trim(), StringComparison.Ordinal);
    }

    // TEMA TEMİZLİĞİ 17.10: Kapali_grup_onizlemesi_tuvalin_KISA_adlarini_yazar_tam_adlar_ipucunda kaldırıldı — kapalı grup önizleme satırı Yıldız'da yok (kısa adlar MenuOgesi.KisaEtiket'te durur, iki dilde dolu oldukları Menu_sozlugu_IKI_DILDE_dolu_ve_EN_adlar_benzersiz'de).

    // ── (4) YILDIZ VE TUTAMAÇ MENÜDE YOK; FAVORİLER ────────────────────────

    // TEMA TEMİZLİĞİ 17.10: Menude_yildiz_ve_tutamac_SIFIR kaldırıldı — kenar menüsü yok; Yıldız üst menüsü yalnız beş bağlantıdır (Yildiz2Testleri.Y2_10: tam beş a.yildiz-menu-oge), "Tüm ekranlar" sayfasında düğme yok (S1dEkranlarTestleri.Her_dugme_ya_istek_atar_ya_da_gorunur_is_yapar).
    // TEMA TEMİZLİĞİ 17.10: PRO_modda_Favoriler_satiri_favori_varsa_EN_USTTE_basitte_yok kaldırıldı — Favoriler satırı Yıldız kabuğunda yok (tuval Bugun4/Tum4).
    // TEMA TEMİZLİĞİ 17.10: PRO_modda_favori_yoksa_Favoriler_satiri_CIZILMEZ kaldırıldı — Favoriler satırı yok.
    // TEMA TEMİZLİĞİ 17.10: Sira_sifirla_kayitli_sira_YOKKEN_cizilmez kaldırıldı — menü sırası ve #kenar-sira-sifirla Yıldız'da yok.
    // TEMA TEMİZLİĞİ 17.10: Sira_sifirla_YALNIZ_kayitli_sira_varken_cikar_ipucu_tasir_ve_DELETE_atar kaldırıldı — #kenar-sira-sifirla yok.

    // ── (5) KULLANICI KARTI ─────────────────────────────────────────────────

    // TEMA TEMİZLİĞİ 17.10: Kullanici_karti_bas_harf_ad_rol_ve_PRO_da_SUBE_adi kaldırıldı — kenar kullanıcı kartı yok; Yıldız kullanıcı düğmesi "AD S." + baş harf (YildizAmblem2Testleri.YA2_18), şube seçici hesap menüsünde (YildizUstMenu <SubeSecici/>).

    // ── KAYNAK KİLİTLERİ ────────────────────────────────────────────────────

    [Fact]
    public void Ust_bar_ve_menu_kaynaginda_METIN_IKON_yok()
    {
        var kok = Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web", "Layout");
        var ust = File.ReadAllText(Path.Combine(kok, "UstBar.razor"));
        // TEMA TEMİZLİĞİ 17.10: menü kaynağı artık üst menüdür (KenarCubugu.razor boşaltıldı, silinecek).
        var kenar = File.ReadAllText(Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web", "Bilesenler", "Kabuk", "YildizUstMenu.razor"));
        Assert.Contains("yildiz-menu-oge", kenar, StringComparison.Ordinal);   // doğru dosya okundu (boş metinde yanlış yeşil yok)

        Assert.DoesNotContain(">⟳<", ust, StringComparison.Ordinal);
        Assert.DoesNotContain(">⠿<", kenar, StringComparison.Ordinal);
        Assert.DoesNotContain("YildizDugmesi(", kenar, StringComparison.Ordinal);
        Assert.DoesNotContain("MenudeGorunur", kenar, StringComparison.Ordinal);
    }

    [Fact]
    public void Menu_sozlugu_IKI_DILDE_dolu_ve_EN_adlar_benzersiz()
    {
        var tumu = MenuTanimi.Ogeler.Concat(MenuTanimi.AltBlok).ToList();
        foreach (var dil in new[] { Sozlukler.Tr, Sozlukler.En })
        {
            var adlar = tumu.Select(o => o.Etiket(dil)).ToList();
            Assert.All(adlar, a => Assert.False(string.IsNullOrWhiteSpace(a)));
            Assert.All(tumu.Select(o => o.KisaEtiket(dil)), a => Assert.False(string.IsNullOrWhiteSpace(a)));
            Assert.Equal(adlar.Count, adlar.Distinct(StringComparer.Ordinal).Count());
        }
        Assert.NotEqual(MenuAdlari.Tr.Musteriler.Ad, MenuAdlari.En.Musteriler.Ad);
    }

    [Fact]
    public void Eski_ad_ile_arama_EKRANI_BULMAYA_devam_eder()
    {
        // Ad değişikliği aramayı kırmaz: "cari" / "denetim" yazan yeni adı bulur.
        var cariler = MenuTanimi.Ogeler.Single(o => o.Yol == "/cariler");
        var denetim = MenuTanimi.Ogeler.Single(o => o.Yol == "/denetim-izi");
        Assert.Contains("cari", AramaMetni.Katla(cariler.AramaMetni(Sozlukler.Tr)), StringComparison.Ordinal);
        Assert.Contains("denetim", AramaMetni.Katla(denetim.AramaMetni(Sozlukler.Tr)), StringComparison.Ordinal);
        // Eş anlamlı EKRANDA görünmez: görünen ad yalnız tuval adıdır.
        Assert.Equal("Müşteriler", cariler.Etiket(Sozlukler.Tr));
    }
}
