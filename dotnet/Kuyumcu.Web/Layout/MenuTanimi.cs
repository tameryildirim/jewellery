using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;

namespace Kuyumcu.Web.Layout;

// ═════════════════════════════════════════════════════════════════════════════
// KENAR ÇUBUĞU MENÜSÜ — TEK TANIM (paket P-14, kimlik Y-9)
//
// NEDEN VERİ, NEDEN RAZOR DEĞİL (PLAN § 1 md.1 "MEKANİZMA ÖNCE"):
//   KenarCubugu.razor'da 38 bağlantı düz bir liste hâlinde, her biri kendi
//   @if bloğuyla duruyordu. Grup başlığı yoktu; "aynı metin iki farklı
//   hedefe gidiyor mu" sorusu ancak gözle sorulabiliyordu. Menü VERİ olunca:
//     • grup başlıkları tek yerden gelir (boş grup başlığı çizilmez),
//     • MENU_AYNI_AD / MENU_KAPSAM / BASIT_MOD_TUTARSIZ sayaçları hem saf
//       testle hem denetim.sh'tan ölçülebilir,
//     • yeni sayfa ekleyen tek satır yazar (razor'a @if bloğu kopyalamaz).
//
// ⚠ BU BİR YETKİ DEĞİLDİR. Buradaki Modul/Roller alanları SUNUCU kapısının
//   (ModulYetkisi / RolGerekli) ekran yansımasıdır; menüyü YALNIZ GİZLER.
//   Adresi doğrudan yazan yetkili kullanıcı sayfaya girer, yetkisiz giremez —
//   son sözü sunucu söyler (Mehmet K8).
//
// ⚠ BasitMenu ile ilişki (S1-g): menü artık modla DARALMAZ — tuval basit
//   modda da yetkili her ekranı 6 grupta gösterir. Mod yetkiyi gevşetmez
//   (M-UX3-06). Adlar Kuyumcu.Core/I18n/Sozlukler.Menu.cs (MenuAdlari).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Kenar çubuğu grupları — çizim sırası <see cref="MenuTanimi.GrupSirasi"/>.</summary>
public enum MenuGrubu
{
    /// <summary>Tezgâhta her gün açılan ekranlar.</summary>
    Gunluk,
    /// <summary>Müşteri kartı ve stok yönetimi.</summary>
    MusteriStok,
    /// <summary>Para giriş/çıkışı ve banka/POS/evrak.</summary>
    ParaBanka,
    /// <summary>Resmî belge ve uyum ekranları.</summary>
    BelgeUyum,
    /// <summary>Okuma amaçlı dökümler.</summary>
    Rapor,
    /// <summary>İşletme kurulumu, kullanıcı, veri.</summary>
    Yonetim,
}

/// <summary>Kenar çubuğundaki tek bağlantı.</summary>
/// <param name="Grup">Hangi grup başlığının altında çizilir.</param>
/// <param name="Yol">Rota — <c>@page</c> şablonuyla birebir aynı olmalıdır.</param>
/// <param name="Modul">Yetki modülü anahtarı; <c>null</c> ise modül aranmaz.</param>
/// <param name="Roller">Rol kapısı; boş dizi ise rol aranmaz.</param>
/// <param name="Ikon"><see cref="Ikonlar"/> sabiti.</param>
/// <param name="Adi">
/// PAKET S1-g: menü adını <see cref="MenuAdlari"/> sözlüğünden seçen işlev
/// (tuvalle birebir; çıplak metin YASAK). Tam ad ve önizleme kısa adı birlikte gelir.
/// </param>
/// <param name="TamEslesme">NavLink eşleşmesi tam mı (yalnız Panel için).</param>
public sealed record MenuOgesi(
    MenuGrubu Grup,
    string Yol,
    string? Modul,
    string[] Roller,
    string Ikon,
    Func<MenuAdlariMetinleri, MenuAdi> Adi,
    bool TamEslesme = false)
{
    /// <summary>
    /// Menü satırı, karo ve sayfa başlığındaki TAM ad. İmza S1-g öncesiyle
    /// aynıdır (<c>oge.Etiket(Dil.T)</c>): tüketiciler değişmeden yeni adı okur.
    /// </summary>
    public Func<Sozluk, string> Etiket => t => Adi(MenuAdlari.Icin(t)).Ad;

    /// <summary>Kapalı grubun önizleme satırındaki KISA ad (tuval Panel.dc.html).</summary>
    public Func<Sozluk, string> KisaEtiket => t => Adi(MenuAdlari.Icin(t)).Kisa;

    /// <summary>
    /// Aramanın taradığı metin: tam ad + (görünmeyen) eş anlamlı eski ad.
    /// S1-g ad değişikliği aramayı KIRMAZ: "cari" yazan "Müşteriler"i bulur.
    /// </summary>
    public Func<Sozluk, string> AramaMetni => t =>
    {
        var a = Adi(MenuAdlari.Icin(t));
        return a.Esanlam.Length == 0 ? a.Ad : a.Ad + " " + a.Esanlam;
    };
}

public static class MenuTanimi
{
    /// <summary>Grup başlıklarının çizim sırası.</summary>
    public static readonly MenuGrubu[] GrupSirasi =
    [
        MenuGrubu.Gunluk,
        MenuGrubu.MusteriStok,
        MenuGrubu.ParaBanka,
        MenuGrubu.BelgeUyum,
        MenuGrubu.Rapor,
        MenuGrubu.Yonetim,
    ];

    /// <summary>36 menü bağlantısı. Sıra = ekrandaki sıra.</summary>
    public static readonly IReadOnlyList<MenuOgesi> Ogeler =
    [
        // ── GÜNLÜK İŞ ───────────────────────────────────────────────────────
        new(MenuGrubu.Gunluk, "/", null, [], Ikonlar.Panel, m => m.Panel, TamEslesme: true),
        new(MenuGrubu.Gunluk, "/islemler", "islem", [], Ikonlar.Islemler, m => m.Satis),
        // Emanet paralel defterde yalnız has bacağında çalışır; işlem yetkisi
        // olan herkes müşteri altını teslim alabilmelidir.
        new(MenuGrubu.Gunluk, "/emanet", "islem", [], Ikonlar.Emanet, m => m.Emanet),
        new(MenuGrubu.Gunluk, "/siparisler", "siparis", [], Ikonlar.Siparisler, m => m.SiparisTamir),
        new(MenuGrubu.Gunluk, "/bildirimler", "bildirim", [], Ikonlar.Bildirimler, m => m.Bildirimler),

        // ── MÜŞTERİ & STOK ──────────────────────────────────────────────────
        new(MenuGrubu.MusteriStok, "/cariler", "cari", [], Ikonlar.Cariler, m => m.Musteriler),
        new(MenuGrubu.MusteriStok, "/urunler", "urun", [], Ikonlar.Urunler, m => m.Urunler),
        new(MenuGrubu.MusteriStok, "/urunler/agac", "urun", [], Ikonlar.UrunAgaci, m => m.UrunAgaci),
        new(MenuGrubu.MusteriStok, "/urunler/etiketler", "urun", [], Ikonlar.Etiketler, m => m.Etiketler),
        // UX-4a madde 14: fiyat belirleme ve sayım RAPOR değil ÜRÜN/STOK işidir;
        // sunucudaki FiyatModulu/SayimModulu kapısıyla aynı anahtara bağlıdır.
        new(MenuGrubu.MusteriStok, "/fiyatlandirma", "urun", [], Ikonlar.Fiyatlandirma, m => m.FiyatListeleri),
        new(MenuGrubu.MusteriStok, "/sayim", "urun", [], Ikonlar.Sayim, m => m.Sayim),
        // PAKET EKSIK-B1-SATINALMA (B1-1): tedarikçi siparişi + mal kabul. Rol kapısı sunucu
        // SatinalmaController [RolGerekli] ile AYNI (SAHİP/MÜDÜR/MUHASEBE okur; yazma SAHİP/MÜDÜR,
        // ekranda MUHASEBE'ye yazma düğmeleri kilit + sebep). TEZGÂHTAR görmez (sunucu 403).
        new(MenuGrubu.MusteriStok, "/satin-alma", null, ["SAHIP", "MUDUR", "MUHASEBE"], Ikonlar.Urunler, m => m.SatinAlma),

        // ── PARA & BANKA ────────────────────────────────────────────────────
        new(MenuGrubu.ParaBanka, "/kasalar", "kasa", [], Ikonlar.Kasalar, m => m.Kasalar),
        // Paket 19 Faz A — vezne kuyruğu: fiş uçlarında ModulYetkisi("islem"),
        // tahsil ucunda ModulYetkisi("kasa"); menüde dar olan kapı aranır.
        new(MenuGrubu.ParaBanka, "/vezne", "kasa", [], Ikonlar.Vezne, m => m.KasaKuyrugu),
        // Y-03 (20.10): gişe zimmeti — sabah teslim, akşam sayım, ana kasaya devir; tezgâhtar kendi zimmetini görür.
        new(MenuGrubu.ParaBanka, "/gise", "kasa", [], Ikonlar.Kasalar, m => m.GiseZimmeti),
        // Paket 31 — POS cihazları ve POS hakedişleri.
        new(MenuGrubu.ParaBanka, "/pos", "kasa", [], Ikonlar.Kasalar, m => m.PosCihazlari),
        new(MenuGrubu.ParaBanka, "/pos/tahsilatlar", "kasa", [], Ikonlar.Kasalar, m => m.PosAlacaklari),
        new(MenuGrubu.ParaBanka, "/banka", "banka", [], Ikonlar.Banka, m => m.Banka),
        // Paket 29 Faz A — çek/senet portföyü; uç [ModulYetkisi("evrak")].
        new(MenuGrubu.ParaBanka, "/evraklar", "evrak", [], Ikonlar.Faturalar, m => m.CekSenet),
        new(MenuGrubu.ParaBanka, "/evraklar/takvim", "evrak", [], Ikonlar.VadeTakibi, m => m.VadeTakvimi),
        new(MenuGrubu.ParaBanka, "/kur", "islem", [], Ikonlar.Kur, m => m.Kur),
        // PAKET P-19 (Y-11/Y-12): gider + virman. Modül anahtarı "kasa" (yeni anahtar
        // açılmadı); rol kapısı sunucuda MUHASEBE+ olduğu için menüde de öyledir —
        // TEZGÂHTAR'a tıklayınca 403 veren bir bağlantı gösterilmez (UX-S30).
        new(MenuGrubu.ParaBanka, "/gider", "kasa", ["SAHIP", "MUDUR", "MUHASEBE"], Ikonlar.GiderPusulalari, m => m.GiderVirman),
        // PAKET P-20 (Y-10): hurda stoğu + ramat çıkışı. Modül anahtarı "islem"
        // (okuma uçları orada), rol kapısı yazma ucundaki gibi MUHASEBE+.
        new(MenuGrubu.ParaBanka, "/hurda", "islem", ["SAHIP", "MUDUR", "MUHASEBE"], Ikonlar.Kur, m => m.HurdaRamat),

        // ── BELGE & UYUM ────────────────────────────────────────────────────
        new(MenuGrubu.BelgeUyum, "/faturalar", "fatura", [], Ikonlar.Faturalar, m => m.Faturalar),
        // PAKET EKSIK-B1-SATINALMA (B1-2): sevk / mal kabul irsaliyeleri + kâğıt dökümü.
        // Rol kapısı IrsaliyeController [RolGerekli] ile AYNI; TEZGÂHTAR görmez.
        new(MenuGrubu.BelgeUyum, "/irsaliyeler", null, ["SAHIP", "MUDUR", "MUHASEBE"], Ikonlar.Faturalar, m => m.Irsaliyeler),
        // MASAK ve gider pusulası kişisel veri taşır: TEZGÂHTAR bu iki ekranı
        // GÖRMEZ. API de RolGerekli ile kapalıdır.
        new(MenuGrubu.BelgeUyum, "/gider-pusulalari", null, ["SAHIP", "MUDUR", "MUHASEBE"], Ikonlar.GiderPusulalari, m => m.GiderPusulalari),
        new(MenuGrubu.BelgeUyum, "/uyum", null, ["SAHIP", "MUDUR", "MUHASEBE"], Ikonlar.Uyum, m => m.Uyum),
        // D1-19 (18.10): KVKK aydınlatma metni + müşteri izinleri özeti. Metni SAHİP/MÜDÜR girer (PUT rol kapısı).
        new(MenuGrubu.BelgeUyum, "/kvkk", "cari", ["SAHIP", "MUDUR"], Ikonlar.Uyum, m => m.Kvkk),
        // PAKET YASAL-1 tur 2 (tasarım hakemi KRİTİK): salt okunur yasal sınırlar —
        // sunucu kapısı [RolGerekli] SAHIP/MUDUR/MUHASEBE ile AYNI rol süzgeci.
        // "Tüm ekranlar" karosu bu satırdan üretilir (şartname § 4.5).
        new(MenuGrubu.BelgeUyum, "/yasal-sinirlar", null, ["SAHIP", "MUDUR", "MUHASEBE"], Ikonlar.Kilit, m => m.YasalSinirlar),

        // ── RAPORLAR ────────────────────────────────────────────────────────
        // Görsel denetim Y-7 (21.10): rapor uçlarının hepsi sunucuda [RolGerekli(SAHIP, MUDUR, MUHASEBE)] (RaporController,
        // RaporKatalogu.MuhasebeUstu). Menü satırında rol yoktu → tezgâhtar ekranı açıp denetimleri görüyor, altta kırmızı
        // "gerekli rol" bandı çıkıyordu. Rol kümesi sunucuyla AYNI; ekran artık tek tip yetki kapısını gösterir.
        // Kalan raporlar menüye girmez; /raporlar'daki Rapor Merkezi kart
        // ızgarasından açılır (paket 34 § riskler: "menü şişmesi" reddi).
        new(MenuGrubu.Rapor, "/raporlar", "rapor", ["SAHIP", "MUDUR", "MUHASEBE"], Ikonlar.Raporlar, m => m.RaporMerkezi),
        new(MenuGrubu.Rapor, "/raporlar/kar-zarar", "rapor", ["SAHIP", "MUDUR", "MUHASEBE"], Ikonlar.KarZarar, m => m.KarZarar),
        new(MenuGrubu.Rapor, "/raporlar/vade-takibi", "rapor", ["SAHIP", "MUDUR", "MUHASEBE"], Ikonlar.VadeTakibi, m => m.VadeTakibi),
        new(MenuGrubu.Rapor, "/raporlar/islem-defteri", "rapor", ["SAHIP", "MUDUR", "MUHASEBE"], Ikonlar.Islemler, m => m.IslemDefteri),
        new(MenuGrubu.Rapor, "/raporlar/satis-analizi", "rapor", ["SAHIP", "MUDUR", "MUHASEBE"], Ikonlar.Raporlar, m => m.SatisAnalizi),
        new(MenuGrubu.Rapor, "/raporlar/kasa", "rapor", ["SAHIP", "MUDUR", "MUHASEBE"], Ikonlar.Kasalar, m => m.KasaRaporu),

        // ── YÖNETİM ─────────────────────────────────────────────────────────
        new(MenuGrubu.Yonetim, "/kullanicilar", "kullanici", [], Ikonlar.Kullanicilar, m => m.Kullanicilar),
        new(MenuGrubu.Yonetim, "/isletme", null, ["SAHIP"], Ikonlar.Isletme, m => m.Isletme),
        new(MenuGrubu.Yonetim, "/yetkiler", null, ["SAHIP"], Ikonlar.Yetkiler, m => m.Yetkiler),
        new(MenuGrubu.Yonetim, "/denetim-izi", null, ["SAHIP"], Ikonlar.DenetimIzi, m => m.KimNeYapti),
        // Paket T-2: kuruluş sihirbazı. MÜDÜR durumu görür, açılış yazamaz —
        // yazma uçlarında ayrıca [RolGerekli("SAHIP")] vardır.
        new(MenuGrubu.Yonetim, "/kurulum", null, ["SAHIP", "MUDUR"], Ikonlar.Isletme, m => m.Kurulum),
        // Dışa aktarım YALNIZ SAHİP: toplu kişisel veri çıkarır (VETO-1 şartı 1).
        new(MenuGrubu.Yonetim, "/veri-disa-aktarim", null, ["SAHIP"], Ikonlar.VeriDisaAktarim, m => m.VeriDisaAktar),
        // Paket 47: içe aktarımın riski sızdırma değil ZEHİRLEME'dir.
        new(MenuGrubu.Yonetim, "/veri-ice-aktarim", null, ["SAHIP"], Ikonlar.VeriIceAktarim, m => m.VeriIceAktar),
        // Paket T-3 Faz A — yedek üstverisi tüm işletmeyi kapsar.
        new(MenuGrubu.Yonetim, "/yedekler", null, ["SAHIP"], Ikonlar.Isletme, m => m.Yedekler),
    ];

    /// <summary>Kenar çubuğunun ALT bloğu — her modda, her rolde görünür.</summary>
    public static readonly IReadOnlyList<MenuOgesi> AltBlok =
    [
        new(MenuGrubu.Yonetim, "/kilavuz", null, [], Ikonlar.Kilavuz, m => m.Kilavuz),
        new(MenuGrubu.Yonetim, "/ayarlar", null, [], Ikonlar.Ayarlar, m => m.Ayarlar),
    ];

    // ── MENU_KAPSAM: menüsüz rotaların GEREKÇELİ listesi ─────────────────────
    /// <summary>
    /// Kenar çubuğunda YER ALMAYAN ama erişilebilir olan rotalar: her satır
    /// "hangi dosya oraya götürüyor" sorusuna cevap verir. Gerekçesiz bir
    /// <c>@page</c> = erişilemeyen sayfa = <c>MENU_KAPSAM</c> ihlali
    /// (UX-S25 "sunucuda uç var, ekranda düğme yok" yasağının menü karşılığı).
    /// Değer = o rotaya götüren kaynak dosyanın <c>Pages/</c> altındaki adı,
    /// ya da girişin kendisi için <c>"(kimlik)"</c>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> MenusuzRotalar =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["/giris"] = "(kimlik)",
            // PAKET AUTH-SIFIRLA: giriş ekranındaki "Parolamı unuttum" bağlantısı + e-postadaki bağlantı.
            ["/sifre-sifirla"] = "Giris.razor",
            ["/banka/ekstre/{IthalatId}"] = "Banka.razor",
            ["/gider-pusulalari/{Id}/belge"] = "GiderPusulalari.razor",
            ["/siparisler/{Id}"] = "Siparisler.razor",
            // PAKET P-17 (Y-6): ortak A4 çıktı — işlem defterindeki her
            // satırın "Belge" bağlantısı buraya götürür.
            ["/cikti/{Tur}/{Id}"] = "IslemDefteri.razor",
            // PAKET P-17 (Y-7): vitrin panosu — kur ekranındaki düğme götürür.
            // Menüye KONMADI: 36 bağlantılık kenar çubuğu P-14'ün mühürlü
            // bilgi mimarisidir; pano kurun türevidir, kur ekranından açılır.
            ["/vitrin"] = "Kur.razor",
            // PAKET EKSIK-KUCUK (B2-10): vitrin dükkân ekranı (tam ekran) — Vitrin
            // panosundaki "Dükkân ekranını aç" düğmesi götürür.
            ["/vitrin/ekran"] = "Vitrin.razor",
            // Y-14 (20.10): oturumsuz canlı fiyat bağlantısı — İşletme › Dış API çekmecesi üretir; menüye konmaz (müşteriye verilir).
            ["/fiyat/{Anahtar}"] = "Bilesenler/DisApiCekmecesi.razor",
            // PAKET-E-BELGE (20.10): e-belge giden kutusu — Faturalar ekranındaki "E-belgeler" düğmesi götürür;
            // menüye konmadı (faturanın türevi; kenar çubuğu şişmez).
            ["/e-belgeler"] = "Faturalar.razor",
            // PAKET S1-d: "Tüm ekranlar" karo sayfası. Üst menünün (Yıldız kabuğu)
            // "Tüm ekranlar" sözcüğü götürür (YildizKabuk.UstMenu son öğesi; grup öğesi
            // DEĞİLDİR: ekranların kendisini listeleyen sayfa kendini listelemez).
            ["/ekranlar"] = "Bilesenler/Kabuk/YildizUstMenu.razor",
        };

    /// <summary>
    /// PAKET S1-d: "Tüm ekranlar" karo sayfasının rotası. Kenar çubuğu bu
    /// sabiti kullanır (MENU_ELLE_BAGLANTI: KenarCubugu'nda elle href yok).
    /// </summary>
    public const string TumEkranlarYolu = "/ekranlar";

    // ── PAKET S1-d — YETKİ SÜZGECİ TEK YERDE ───────────────────────────────
    /// <summary>
    /// Öğe bu kullanıcıya YETKİ bakımından açık mı? Sunucu kapısının
    /// (ModulYetkisi / RolGerekli) ekran yansımasıdır; MOD bilmez.
    /// Kenar çubuğu buna modu VE ile ekler (M-UX3-06), karo sayfası modu
    /// eklemez: "Tüm ekranlar" basit modda da yetkili olunan HER ekranı
    /// gösterir (tuval notu n2) ama yetkisiz ekranın karosunu ÇİZMEZ.
    /// </summary>
    public static bool YetkiliMi(MenuOgesi oge, Func<string, bool> modulErisimi, string? rol) =>
        (oge.Modul is null || modulErisimi(oge.Modul))
        && (oge.Roller.Length == 0 || oge.Roller.Contains(rol ?? ""));

    /// <summary>Kullanıcının yetkili olduğu menü öğeleri, tanım sırasıyla.</summary>
    public static List<MenuOgesi> Yetkililer(Func<string, bool> modulErisimi, string? rol) =>
        [.. Ogeler.Where(o => YetkiliMi(o, modulErisimi, rol))];

    /// <summary>Rotadan id eki: "/" → "panel", "/urunler/agac" → "urunler-agac".</summary>
    public static string Slug(string yol) =>
        yol == "/" ? "panel" : yol.Trim('/').Replace('/', '-');

    /// <summary>Grup başlığının CSS/id eki.</summary>
    public static string GrupSlug(MenuGrubu grup) => grup switch
    {
        MenuGrubu.Gunluk => "gunluk",
        MenuGrubu.MusteriStok => "musteri-stok",
        MenuGrubu.ParaBanka => "para-banka",
        MenuGrubu.BelgeUyum => "belge-uyum",
        MenuGrubu.Rapor => "rapor",
        MenuGrubu.Yonetim => "yonetim",
        _ => throw new ArgumentOutOfRangeException(nameof(grup), grup, "tanımsız menü grubu"),
    };

    /// <summary>Grup başlığını sözlükten seçer (çıplak metin YASAK — UX-S31).</summary>
    public static Func<Sozluk, string> GrupEtiketi(MenuGrubu grup) => grup switch
    {
        MenuGrubu.Gunluk => t => MenuAdlari.Icin(t).GrupGunluk,
        MenuGrubu.MusteriStok => t => MenuAdlari.Icin(t).GrupMusteriStok,
        MenuGrubu.ParaBanka => t => MenuAdlari.Icin(t).GrupParaBanka,
        MenuGrubu.BelgeUyum => t => MenuAdlari.Icin(t).GrupBelgeUyum,
        MenuGrubu.Rapor => t => MenuAdlari.Icin(t).GrupRapor,
        MenuGrubu.Yonetim => t => MenuAdlari.Icin(t).GrupYonetim,
        _ => throw new ArgumentOutOfRangeException(nameof(grup), grup, "tanımsız menü grubu"),
    };

    // ── MENU_AYNI_AD: etiket çarpışması ölçüsü ──────────────────────────────
    // Genel ek sözcükler: bir etiketi diğerinden AYIRMAYAN, yalnız "bu bir
    // rapor/defter" bilgisi taşıyan kelimeler. Çarpışma tanımı: iki farklı
    // hedefin etiketi bu ekler atıldıktan ve çoğul eki kırpıldıktan sonra
    // AYNI dizgeye iniyorsa. P-14 öncesi 4 çarpışma ölçülmüştü:
    //   İşlemler↔İşlem Defteri · Kasalar↔Kasa Defteri ·
    //   Vade Takvimi↔Vade Takibi · POS Cihazları↔POS Alacakları
    private static readonly string[] GenelEkler =
        ["defter", "rapor", "takib", "takip", "takvim", "cihaz", "alacak", "liste"];

    /// <summary>
    /// Etiketi çarpışma karşılaştırması için sadeleştirir: Türkçe harfler
    /// katlanır, çoğul eki kırpılır, genel ek sözcükler atılır. Tüm sözcükler
    /// atılacak olursa ilk sözcüğün gövdesi korunur (yoksa "Raporlar" boşa
    /// inip her şeyle çarpışırdı).
    /// </summary>
    public static string Sadelestir(string etiket)
    {
        var sozcukler = Katla(etiket)
            .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(Govde)
            .ToArray();
        // PAKET S1-j — BOŞ METİN KORUMASI (kurucu 24.09 14:40, K11): menü
        // aramasının ilk tuşunda keydown, bağlama güncellenmeden ÖNCE çalışır ve
        // buraya "" gelirdi → sozcukler[0] IndexOutOfRangeException → Blazor
        // "Beklenmeyen bir hata oluştu" bandı. Boş/boşluklu metnin sadeleşmişi
        // BOŞTUR; hiçbir şey tahmin edilmez.
        if (sozcukler.Length == 0) return "";
        var kalan = sozcukler.Where(s => !GenelEkler.Any(s.StartsWith)).ToArray();
        if (kalan.Length == 0) kalan = [sozcukler[0]];
        return string.Join(' ', kalan);
    }

    // ── PAKET S1-j — MENÜ ARAMASI: TEK EŞLEŞME KURALI ───────────────────────
    // Kenar çubuğunun kendi içinde süzülmesi (KenarCubugu) ve Ctrl+K kayıt
    // paletinin EKRANLAR bölümü (GlobalArama) AYNI kuralı kullanır (UX-S31:
    // iki normalleştirme yasak). Kural: sorgunun HER sözcüğü, hedefin
    // (ad + görünmeyen eski ad, S1-g eş anlamlıları) bir sözcüğünün BAŞI olur.
    // "sat" → Satış, Satış analizi · "cari" → Müşteriler (eski ad Cariler) ·
    // "masak" → Uyum (MASAK) · "kâr" → Kâr-zarar. Türkçe harfler katlanır
    // (ş/İ/ı → s/i/i — AramaMetni.Katla, tek kaynak).
    // ⚠ Sadelestir BURADA KULLANILMAZ: genel ekleri (rapor, defter…) attığı
    //   için "rapor" yazan "Kasa raporu"nu bulamazdı.

    /// <summary>Metni arama sözcüklerine böler: katlanır, harf/rakam dışı her şey ayraçtır.</summary>
    public static string[] AramaSozcukleri(string? metin)
    {
        var k = Katla(metin ?? "");
        if (k.Length == 0) return [];
        var sb = new System.Text.StringBuilder(k.Length);
        foreach (var h in k) sb.Append(char.IsLetterOrDigit(h) ? h : ' ');
        return sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Kullanıcı bir şey yazdı mı? (boş ve yalnız boşluk = yazmadı; menü
    /// süzülmez). Özel karakterlerden ibaret sorgu YAZILMIŞ sayılır ama hiçbir
    /// sözcük taşımadığı için hiçbir ekranla eşleşmez ("Eşleşen ekran yok").
    /// </summary>
    public static bool AramaVar(string? sorgu) => !string.IsNullOrWhiteSpace(sorgu);

    /// <summary>Sorgunun her sözcüğü hedefin bir sözcüğünün başı mı?</summary>
    public static bool AramaEslesir(string? hedefMetni, string? sorgu)
    {
        var q = AramaSozcukleri(sorgu);
        if (q.Length == 0) return false;
        var hedef = AramaSozcukleri(hedefMetni);
        return q.All(s => hedef.Any(h => h.StartsWith(s, StringComparison.Ordinal)));
    }

    /// <summary>Menü öğesi (ad + eski ad) bu sorguyla eşleşiyor mu?</summary>
    public static bool AramaEslesir(MenuOgesi oge, Sozluk t, string? sorgu) =>
        AramaEslesir(oge.AramaMetni(t), sorgu);

    /// <summary>
    /// PAKET S1-g — MENU_AYNI_AD ölçüsünün YENİ tanımı: Türkçe harfler katlanır,
    /// her sözcüğün çoğul eki kırpılır, ama genel ek sözcükler ATILMAZ.
    /// <para>
    /// NEDEN DEĞİŞTİ: kurucunun tuvali (Ekranlar.dc.html, Panel.dc.html)
    /// "Kasalar / Kasa raporu", "Vade takvimi / Vade takibi", "POS cihazları /
    /// POS alacakları" adlarını YAN YANA kullanır; bu çiftlerde ayırt edici
    /// olan tam da P-14'ün "genel ek" saydığı sözcüktür (rapor, takvim, cihaz,
    /// alacak). Plan ile tuval çelişirse TUVAL geçerlidir (kurucu kararı).
    /// Kural hâlâ sıfırdır: iki farklı hedef aynı AYIRT EDİCİ ada inemez
    /// ("Kasalar" ↔ "Kasa" ya da "İşlemler" ↔ "İşlem" hâlâ çarpışmadır).
    /// </para>
    /// </summary>
    public static string AyirtEdiciAd(string etiket) =>
        string.Join(' ', Katla(etiket)
            .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(Govde));

    /// <summary>Çoğul ekini kırpar (gövde en az 3 harf kalacak şekilde).</summary>
    private static string Govde(string s)
    {
        foreach (var ek in new[] { "leri", "lari", "ler", "lar" })
            if (s.Length - ek.Length >= 3 && s.EndsWith(ek, StringComparison.Ordinal))
                return s[..^ek.Length];
        return s;
    }

    /// <summary>Türkçe katlama TEK KAYNAK (P-36): <c>Servisler.AramaMetni.Katla</c>.
    /// İki normalleştirme yasak — Secici bileşeni ile aynı harf haritası kullanılır.</summary>
    private static string Katla(string s) => Kuyumcu.Web.Servisler.AramaMetni.Katla(s);
}
