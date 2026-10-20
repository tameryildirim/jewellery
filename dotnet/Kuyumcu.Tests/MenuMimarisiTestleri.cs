using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// MENÜ / BİLGİ MİMARİSİ — paket P-14, kimlik Y-9
//
// Kapatılan kusur (EKRAN-ALAN-DOGRULAMA-2026-09-18.md sıra 9):
//   • 38 bağlantı DÜZ bir liste, grup başlığı = 0 → kuyumcu ekranda kayboluyor
//   • 4 etiket çarpışması: İşlemler↔İşlem Defteri · Kasalar↔Kasa Defteri ·
//     Vade Takvimi↔Vade Takibi · POS Cihazları↔POS Alacakları
//
// Sayaçlar (denetim.sh `# >>> P-14` bloğunun test karşılığı):
//   MENU_AYNI_AD = 0 · MENU_KAPSAM = 0 · BASIT_MOD_TUTARSIZ = 0
//
// ⚠ Rota listesi MenuTanimi'nden DEĞİL, [Route] özniteliklerinden yansımayla
//   gelir (RotaBaslikTestleri deseni): aksi hâlde "menüden bir satır sil"
//   mutasyonu testi yeşil bırakırdı.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class MenuMimarisiTestleri : BunitContext
{
    private static Sozluk Tr => Sozlukler.Tr;
    private static Sozluk En => Sozlukler.En;

    private static IEnumerable<MenuOgesi> TumOgeler =>
        MenuTanimi.Ogeler.Concat(MenuTanimi.AltBlok);

    // ── 1) GRUPLAMA ─────────────────────────────────────────────────────────

    [Fact]
    // 36 → 37: PAKET P-19 (Y-11/Y-12) /gider bağlantısını ekledi
    // (Para & Banka grubu; modül anahtarı "kasa", rol kapısı MUHASEBE+).
    // 37 → 38: PAKET P-20 (Y-10) /hurda bağlantısını ekledi (Para & Banka;
    // modül anahtarı "islem", rol kapısı MUHASEBE+ — yazma ucuyla AYNI).
    public void Menu_36_baglanti_tasir_ve_alt_blokla_38_eder()
    {
        // 38 → 39: PAKET YASAL-1 tur 2 /yasal-sinirlar bağlantısını ekledi (Belge ve uyum;
        // rol kapısı SAHIP/MUDUR/MUHASEBE — sunucu [RolGerekli] ile AYNI).
        // 39 → 41: PAKET EKSIK-B1-SATINALMA /satin-alma (Müşteri ve stok) + /irsaliyeler (Belge ve uyum)
        // bağlantılarını ekledi; rol kapısı SAHIP/MUDUR/MUHASEBE — sunucu [RolGerekli] ile AYNI.
        // 41 → 42: D1-19 /kvkk (Belge ve uyum; SAHIP/MUDUR — PUT /kvkk/metin rol kapısıyla AYNI).
        // 42 → 43: Y-03 (20.10) /gise gişe zimmeti (Para ve banka; sunucu kapısı "kasa").
        Assert.Equal(43, MenuTanimi.Ogeler.Count);
        Assert.Equal(2, MenuTanimi.AltBlok.Count);
        Assert.Equal(45, TumOgeler.Count());
        Assert.Equal(45, TumOgeler.Select(o => o.Yol).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Grup_basligi_sayisi_SIFIRDAN_BUYUK_ve_her_grup_dolu()
    {
        Assert.True(MenuTanimi.GrupSirasi.Length > 0, "grup başlığı > 0 (P-14 DoD)");
        Assert.Equal(
            MenuTanimi.GrupSirasi.Length,
            MenuTanimi.GrupSirasi.Distinct().Count());

        foreach (var grup in MenuTanimi.GrupSirasi)
            Assert.True(
                MenuTanimi.Ogeler.Any(o => o.Grup == grup),
                $"'{grup}' grubu BOŞ — boş grup başlığı menüde yalan söyler");

        // Sıralamada yeri olmayan gruba öge konamaz.
        foreach (var oge in MenuTanimi.Ogeler)
            Assert.Contains(oge.Grup, MenuTanimi.GrupSirasi);
    }

    [Fact]
    public void Her_grup_basliginin_TR_ve_EN_karsiligi_vardir_ve_benzersizdir()
    {
        var tr = MenuTanimi.GrupSirasi.Select(g => MenuTanimi.GrupEtiketi(g)(Tr)).ToList();
        var en = MenuTanimi.GrupSirasi.Select(g => MenuTanimi.GrupEtiketi(g)(En)).ToList();

        Assert.All(tr, s => Assert.False(string.IsNullOrWhiteSpace(s)));
        Assert.All(en, s => Assert.False(string.IsNullOrWhiteSpace(s)));
        Assert.Equal(tr.Count, tr.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(en.Count, en.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            MenuTanimi.GrupSirasi.Length,
            MenuTanimi.GrupSirasi.Select(MenuTanimi.GrupSlug).Distinct(StringComparer.Ordinal).Count());
    }

    // ── 2) MENU_AYNI_AD = 0 ─────────────────────────────────────────────────

    [Fact]
    public void MENU_AYNI_AD_sifir_TR_sadelestirilmis_etiketler_carpismaz()
    {
        // S1-g YENİDEN YAZIM — ESKİ ölçü MenuTanimi.Sadelestir'di (genel ek
        // sözcükler "rapor/takvim/cihaz/alacak/defter" ATILIRDI). Kurucunun tuvali
        // (Ekranlar.dc.html) "Kasalar/Kasa raporu", "Vade takvimi/Vade takibi",
        // "POS cihazları/POS alacakları" adlarını birlikte kullanır: ayırt edici
        // sözcük tam da o eklerdir. YENİ ölçü MenuTanimi.AyirtEdiciAd: katlama +
        // çoğul eki kırpma (genel ekler KORUNUR). Kural hâlâ SIFIR.
        var carpisan = TumOgeler
            .GroupBy(o => MenuTanimi.AyirtEdiciAd(o.Etiket(Tr)), StringComparer.Ordinal)
            .Where(g => g.Select(o => o.Yol).Distinct(StringComparer.Ordinal).Count() > 1)
            .Select(g => $"{g.Key} → {string.Join(" | ", g.Select(o => $"{o.Etiket(Tr)} ({o.Yol})"))}")
            .ToList();

        Assert.True(carpisan.Count == 0,
            "MENU_AYNI_AD = 0 olmalı; çarpışanlar: " + string.Join(" ;; ", carpisan));
    }

    [Fact]
    public void MENU_AYNI_AD_sifir_EN_etiketler_birebir_benzersiz()
    {
        var etiketler = TumOgeler.Select(o => o.Etiket(En)).ToList();
        var tekrar = etiketler.GroupBy(x => x, StringComparer.Ordinal)
            .Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        Assert.True(tekrar.Count == 0, "EN etiketi tekrar ediyor: " + string.Join(", ", tekrar));
    }

    [Fact]
    public void Dort_etiket_carpismasi_COZULDU()
    {
        // Y-9'un adıyla saydığı dört çift. S1-g YENİDEN YAZIM: çiftler artık
        // menüde GÖRÜNEN adlardır (MenuAdlari, tuval) ve ölçü AyirtEdiciAd'dır.
        // "İşlemler ↔ İşlem Defteri" çarpışması tuvalde "Satış ↔ İşlem defteri"
        // olarak ÇÖZÜLMÜŞ durur; diğer üç çift ayırt edici sözcükle ayrışır.
        var m = MenuAdlari.Tr;
        (string a, string b)[] ciftler =
        [
            (m.Satis.Ad, m.IslemDefteri.Ad),
            (m.Kasalar.Ad, m.KasaRaporu.Ad),
            (m.VadeTakvimi.Ad, m.VadeTakibi.Ad),
            (m.PosCihazlari.Ad, m.PosAlacaklari.Ad),
        ];

        foreach (var (a, b) in ciftler)
            Assert.False(
                MenuTanimi.AyirtEdiciAd(a) == MenuTanimi.AyirtEdiciAd(b),
                $"'{a}' ile '{b}' hâlâ çarpışıyor ({MenuTanimi.AyirtEdiciAd(a)})");
        // Kural gevşemedi: gerçek çarpışmayı hâlâ yakalar.
        Assert.Equal(MenuTanimi.AyirtEdiciAd("Kasalar"), MenuTanimi.AyirtEdiciAd("Kasa"));
        Assert.Equal(MenuTanimi.AyirtEdiciAd("İşlemler"), MenuTanimi.AyirtEdiciAd("İşlem"));
    }

    [Fact]
    public void Eski_carpisan_adlar_GERI_GELMEZ()
    {
        // Cırcır: düzeltilen adlar kenar çubuğuna geri sızarsa test kırmızı.
        // S1-g YENİDEN YAZIM — ESKİ yasak liste "İşlem Defteri, Kasa Defteri,
        // Vade Takibi, POS Alacakları" idi; kurucunun tuvali üçünü (cümle
        // düzeninde, farklı AYIRT EDİCİ adla) geri getirdi. YENİ yasak liste:
        // tuvalin ONAYLAMADIĞI eski çarpışan ad + S1-g'de değiştirilen hub adları.
        string[] yasak = ["İşlemler", "Kasa Defteri", "Cariler", "Hareket Dökümü",
                          "Kasa Hareketleri", "POS Hakedişleri", "Gecikmiş Alacaklar", "Denetim İzi"];
        var suanki = TumOgeler.Select(o => o.Etiket(Tr)).ToList();

        foreach (var y in yasak)
            Assert.DoesNotContain(y, suanki);
    }

    [Fact]
    public void Ayni_kavram_TEK_ad_tasir_menu_ile_rapor_kataloğu_ayni()
    {
        // UX-S31: menüde "Hareket Dökümü", sayfa başlığında "İşlem Defteri"
        // yazamaz. İki sözlük anahtarı da AYNI dizgeyi döndürmelidir.
        Assert.Equal(Tr.RaporSeti.AdIslemDefteri, Tr.Nav.IslemDefteri);
        Assert.Equal(Tr.RaporSeti.AdKasaDefteri, Tr.Nav.KasaRaporu);
        Assert.Equal(Tr.RaporSeti.AdVadeTakibi, Tr.Nav.VadeTakibi);
        Assert.Equal(En.RaporSeti.AdIslemDefteri, En.Nav.IslemDefteri);
        Assert.Equal(En.RaporSeti.AdKasaDefteri, En.Nav.KasaRaporu);
        Assert.Equal(En.RaporSeti.AdVadeTakibi, En.Nav.VadeTakibi);
        // Sayfa gövdesindeki başlık da menüyle AYNI adı taşır (UX-S31):
        Assert.Equal(Tr.Vade.Baslik, Tr.Nav.VadeTakibi);
        Assert.Equal(En.Vade.Baslik, En.Nav.VadeTakibi);
        Assert.Equal(Tr.Pos.TahsilatBaslik, Tr.Nav.PosTahsilat);
        Assert.Equal(En.Pos.TahsilatBaslik, En.Nav.PosTahsilat);
        Assert.Equal(Tr.Pos.Baslik, Tr.Nav.Pos);
        Assert.Equal(Tr.Islem.Baslik, Tr.Nav.Islemler);
    }

    [Fact]
    public void Sadelestir_genel_ekleri_atar_ama_etiketi_BOSALTMAZ()
    {
        Assert.Equal("islem", MenuTanimi.Sadelestir("İşlemler"));
        Assert.Equal("islem", MenuTanimi.Sadelestir("İşlem Defteri"));
        Assert.Equal("kasa", MenuTanimi.Sadelestir("Kasa Defteri"));
        Assert.Equal("vade", MenuTanimi.Sadelestir("Vade Takibi"));
        Assert.Equal("vade", MenuTanimi.Sadelestir("Vade Takvimi"));
        Assert.Equal("pos", MenuTanimi.Sadelestir("POS Cihazları"));
        // Tüm sözcükler genel ek olsa bile etiket boşa inmez:
        Assert.Equal("rapor", MenuTanimi.Sadelestir("Raporlar"));
    }

    // ── 3) MENU_KAPSAM = 0 (erişilemeyen @page yok) ──────────────────────────

    [Fact]
    public void MENU_KAPSAM_sifir_her_rota_ya_menude_ya_gerekceli()
    {
        var rotalar = RotaBaslikTestleri.YansimayaGoreRotalar();
        var menude = TumOgeler.Select(o => RotaBasliklari.Duzgunlestir(o.Yol))
            .ToHashSet(StringComparer.Ordinal);

        var erisilemeyen = rotalar
            .Where(r => !menude.Contains(r) && !MenuTanimi.MenusuzRotalar.ContainsKey(r))
            .ToList();

        Assert.True(erisilemeyen.Count == 0,
            "MENU_KAPSAM = 0 olmalı; erişilemeyen @page: " + string.Join(", ", erisilemeyen));
    }

    [Fact]
    public void Menusuz_rota_listesi_BAYAT_DEGIL_ve_gerekcesi_dolu()
    {
        var rotalar = RotaBaslikTestleri.YansimayaGoreRotalar().ToHashSet(StringComparer.Ordinal);
        var menude = TumOgeler.Select(o => RotaBasliklari.Duzgunlestir(o.Yol))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var (rota, kaynak) in MenuTanimi.MenusuzRotalar)
        {
            Assert.Contains(rota, rotalar);                 // var olmayan rotaya muafiyet yazılamaz
            Assert.DoesNotContain(rota, menude);            // menüdeyse muafiyete gerek yok
            Assert.False(string.IsNullOrWhiteSpace(kaynak), $"{rota} gerekçesiz");
        }
    }

    [Fact]
    public void Menudeki_her_yol_GERCEK_bir_rotadir_ve_baslik_tasir()
    {
        var rotalar = RotaBaslikTestleri.YansimayaGoreRotalar().ToHashSet(StringComparer.Ordinal);

        foreach (var oge in TumOgeler)
        {
            var yol = RotaBasliklari.Duzgunlestir(oge.Yol);
            Assert.True(rotalar.Contains(yol), $"menüde ölü bağlantı: {yol}");
            Assert.NotNull(RotaBasliklari.Bul(yol));
        }
    }

    // ── 4) BASIT_MOD_TUTARSIZ = 0 ───────────────────────────────────────────

    [Fact]
    public void BASIT_MOD_TUTARSIZ_sifir_basit_menunun_her_yolu_menude_var()
    {
        var menuYollari = MenuTanimi.Ogeler.Select(o => o.Yol)
            .ToHashSet(StringComparer.Ordinal);

        var tutarsiz = BasitMenu.Yollar.Where(y => !menuYollari.Contains(y)).ToList();

        Assert.True(tutarsiz.Count == 0,
            "basit menüde menüde olmayan yol: " + string.Join(", ", tutarsiz));
    }

    [Fact]
    public void BASIT_MOD_TUTARSIZ_sifir_rozetin_hedefi_basit_modda_da_acilir()
    {
        // P-14 DoD: "rozeti olan her özelliğin sayfası basit mod menüsünde de
        // var." Bugün üst barda tek rozet vardır: bildirim rozeti → /bildirimler.
        // Rozet basit modda görünüp sayfaya gidilemezse kullanıcı hiçbir yere
        // varamaz (6-A doğrulayıcı devri, UX-1-C4).
        Assert.True(BasitMenu.Icerir("/bildirimler"));
    }

    // ── 5) ÇİZİM (bUnit) ────────────────────────────────────────────────────

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

    private string moduller =
        """["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","evrak","siparis","bildirim"]""";

    public MenuMimarisiTestleri()
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

    // TEMA TEMİZLİĞİ 17.10: kenar çubuğu kaldırıldı. Menü tanımının ÇİZİMİ artık "Tüm ekranlar"
    // sayfasıdır (Pages/Ekranlar → YildizEkranlar): her grup bir bölüm (section#ekranlar-grup-{slug},
    // başlık #ekranlar-grup-baslik-{slug}), her ekran bir kare (a.yz-kare). P-14 DoD'unun soruları
    // ("grup başlığı var mı, altında gidilecek yer var mı, yetkisizin grubu çiziliyor mu") orada sınanır.
    private IRenderedComponent<Ekranlar> Ciz(string rol, string etkinMod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1", null, etkinMod));
        return Render<Ekranlar>().KareleriBekle();
    }

    [Fact]
    public void Cizimde_grup_basligi_VARDIR_ve_altinda_en_az_bir_baglanti_bulunur()
    {
        var e = Ciz("SAHIP", ArayuzModuServisi.Profesyonel);
        var bolumler = e.FindAll("section.yz-tum-bolum");

        Assert.NotEmpty(bolumler);              // grup başlığı > 0 (P-14 DoD)
        Assert.Equal(MenuTanimi.GrupSirasi.Length, bolumler.Count);

        // Her bölümün başlığı vardır ve altında gerçekten gidilecek bir yer (bağlantı) bulunur.
        foreach (var b in bolumler)
        {
            var baslik = b.QuerySelector("h2");
            Assert.NotNull(baslik);
            Assert.False(string.IsNullOrWhiteSpace(baslik!.TextContent), $"{b.Id}: boş başlık");
            Assert.Equal(baslik.Id, b.GetAttribute("aria-labelledby"));
            Assert.NotEmpty(b.QuerySelectorAll("a.yz-kare[href]"));
        }
    }

    [Fact]
    public void Basit_modda_BOS_grup_basligi_cizilmez()
    {
        // "BOŞ grup başlığı çizilmez" — yetkisi olmayan grubun başlığı basit modda da yoktur.
        // ⚠ Yetki servisi kullanıcı başına önbellekli: süzgeç İLK çizimden önce kurulur.
        moduller = "[]";
        var t = Ciz("TEZGAHTAR", ArayuzModuServisi.Basit);
        var bolumler = t.BolumKimlikleri();
        Assert.Contains("ekranlar-grup-gunluk", bolumler);          // Panel yetki istemez
        Assert.DoesNotContain("ekranlar-grup-yonetim", bolumler);   // boş grup başlığı YOK
        Assert.DoesNotContain("ekranlar-grup-musteri-stok", bolumler);
        Assert.Empty(t.FindAll("#ekranlar-grup-baslik-yonetim"));
    }

    [Fact]
    public void Yetkisiz_kullanicinin_grubu_HIC_cizilmez()
    {
        moduller = "[]";                       // hiçbir modül yetkisi yok
        var e = Ciz("TEZGAHTAR", ArayuzModuServisi.Profesyonel);

        var bolumler = e.BolumKimlikleri();

        // Panel yetki istemez → "Günlük İş" durur; rapor/yönetim grubu gitmeli.
        Assert.Contains("ekranlar-grup-gunluk", bolumler);
        Assert.DoesNotContain("ekranlar-grup-rapor", bolumler);
        Assert.DoesNotContain("ekranlar-grup-yonetim", bolumler);
        Assert.DoesNotContain("ekranlar-grup-musteri-stok", bolumler);
    }

    [Fact]
    public void Cizilen_etiketler_menu_tanimiyla_AYNIDIR()
    {
        var e = Ciz("SAHIP", ArayuzModuServisi.Profesyonel);
        var metinler = e.FindAll("a.yz-kare .yz-kad").Select(a => a.TextContent.Trim()).ToList();

        // Menü adları TEK kaynaktan (MenuAdlari, tuval) gelir; hub sözlüğünün eski P-14 adları dönmez.
        var m = MenuAdlari.Tr;
        Assert.Contains(m.IslemDefteri.Ad, metinler);
        Assert.Contains(m.KasaRaporu.Ad, metinler);
        Assert.Contains(m.PosAlacaklari.Ad, metinler);
        Assert.Contains(m.VadeTakibi.Ad, metinler);
        foreach (var eski in new[] { "Hareket Dökümü", "Kasa Hareketleri", "POS Hakedişleri", "Gecikmiş Alacaklar" })
            Assert.DoesNotContain(eski, metinler);
    }
}
