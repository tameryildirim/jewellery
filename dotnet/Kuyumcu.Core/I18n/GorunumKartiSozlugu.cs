namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-f — AYARLAR → GÖRÜNÜM KARTI: TR + EN METİNLER
//
// Kaynak: kurucunun onayladığı tuval dokumanlar/02-mimari/sadelestirme/tuval/
// project/Ayarlar.dc.html (artboard 7) ve tuval notu n4. Metinler tuvaldeki
// cümlelerin birebir aynısıdır; tuvalde olmayan yalnız iki kilit sebebi ve
// kaynak notu vardır (UX-S21, UX-S30 — rapor § Sapmalar).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs 9200+ satırlık ORTAK HUB'dır ve aynı anda
//   başka şeritler ona dokunuyor (orkestratör yasağı). Şema/mod/punto ADLARI
//   burada DEĞİL, tek kaynakta durur: şema, tema modu ve yazı boyutu adları
//   GorunumSozlugu'nda; Basit/Profesyonel adları Sozlukler.Arayuz'da (UX-S31 —
//   aynı kavram tek ad, tek kaynak). Burada yalnız karta ÖZGÜ cümleler var.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Görünüm kartının bir dildeki metinleri.</summary>
public sealed class GorunumKartiMetinleri
{
    public required string TemaBaslik { get; init; }
    public required string TemaAltMetin { get; init; }
    public required string Secili { get; init; }
    public required string AydinlikKaranlikBaslik { get; init; }
    public required string KagitNotu { get; init; }
    public required string YaziBaslik { get; init; }
    public required string YaziNotu { get; init; }
    public required string ModBaslik { get; init; }
    public required string ModBasitAciklama { get; init; }
    public required string ModProfesyonelAciklama { get; init; }
    public required string ModNotu { get; init; }
    public required string AltNot { get; init; }
    public required string DukkanaDon { get; init; }
    public required string VarsayilanYap { get; init; }
    public required string DukkanaDonKilit { get; init; }
    public required string VarsayilanYapKilitSahip { get; init; }
    public required string VarsayilanYapildi { get; init; }
    public required string DukkanaDonuldu { get; init; }
    /// <summary>"{0} px" — piksel değeri InvariantCulture ile yazılır.</summary>
    public required string Piksel { get; init; }
    public required string AyrintiBaslik { get; init; }
    public required string Okunuyor { get; init; }
    /// <summary>"{0}" = sunucunun hata mesajı. Görünüm döndü, mod dönmedi.</summary>
    public required string DonModHatasi { get; init; }
    /// <summary>"{0}" = sunucunun hata mesajı. Görünüm yazıldı, mod yazılmadı.</summary>
    public required string YapModHatasi { get; init; }
}

/// <summary>Görünüm kartı sözlüğü — TR + EN (Sozlukler.cs'e dokunulmaz).</summary>
public static class GorunumKartiSozlugu
{
    public static readonly GorunumKartiMetinleri Tr = new()
    {
        TemaBaslik = "Renk teması",
        TemaAltMetin = "8 tema · herkes kendi seçer · işletme varsayılanını sahip belirler",
        Secili = "Seçili",
        AydinlikKaranlikBaslik = "Aydınlık / karanlık",
        KagitNotu = "Fiş ve etiket kâğıdı her zaman beyaz basılır.",
        YaziBaslik = "Yazı boyutu",
        YaziNotu = "Rakamlar her boyutta bir kademe büyük görünür.",
        ModBaslik = "Ekran modu",
        ModBasitAciklama = "Tezgâh için: tek fiş, büyük düğmeler",
        ModProfesyonelAciklama = "Tüm alanlar açık, kısayollar",
        ModNotu = "Hiçbir özellik kapanmaz, yalnız ilk bakışta görünen değişir.",
        AltNot = "Değişiklik hemen uygulanır, kaydetmek gerekmez. Bu tercih sadece senin hesabın içindir.",
        DukkanaDon = "İşletme varsayılanına dön",
        VarsayilanYap = "Bunu işletme varsayılanı yap",
        DukkanaDonKilit = "Zaten işletme varsayılanını kullanıyorsun; dönülecek kendi seçimin yok.",
        VarsayilanYapKilitSahip = "İşletme varsayılanını yalnız işletme sahibi belirler.",
        VarsayilanYapildi = "İşletme varsayılanı kaydedildi: kendi seçimini yapmamış herkes bu görünümü görür.",
        DukkanaDonuldu = "Kendi seçimlerin silindi; artık işletme varsayılanını kullanıyorsun.",
        Piksel = "{0} px",
        AyrintiBaslik = "Ayrıntılı görünüm ayarları (tek tek seçim, bu cihaz)",
        Okunuyor = "Görünüm ayarın okunuyor…",
        DonModHatasi = "Renk, aydınlık/karanlık ve yazı boyutu işletme varsayılanına döndü; ekran modu döndürülemedi: {0}",
        YapModHatasi = "Renk, aydınlık/karanlık ve yazı boyutu işletme varsayılanı oldu; ekran modu yazılamadı: {0}",
    };

    public static readonly GorunumKartiMetinleri En = new()
    {
        TemaBaslik = "Colour theme",
        TemaAltMetin = "8 themes · everyone picks their own · the owner sets the shop default",
        Secili = "Selected",
        AydinlikKaranlikBaslik = "Light / dark",
        KagitNotu = "Receipt and label paper is always printed white.",
        YaziBaslik = "Text size",
        YaziNotu = "Figures always look one step larger at every size.",
        ModBaslik = "Screen mode",
        ModBasitAciklama = "For the counter: one slip, big buttons",
        ModProfesyonelAciklama = "All fields open, shortcuts",
        ModNotu = "No feature is switched off; only what you see first changes.",
        AltNot = "Changes apply at once, no need to save. This preference is for your account only.",
        DukkanaDon = "Back to shop default",
        VarsayilanYap = "Make this the shop default",
        DukkanaDonKilit = "You already use the shop default; there is no own choice to clear.",
        VarsayilanYapKilitSahip = "Only the shop owner sets the shop default.",
        VarsayilanYapildi = "Shop default saved: everyone who has not chosen their own sees this appearance.",
        DukkanaDonuldu = "Your own choices were cleared; you now use the shop default.",
        Piksel = "{0} px",
        AyrintiBaslik = "Detailed appearance settings (one by one, this device)",
        Okunuyor = "Reading your appearance settings…",
        DonModHatasi = "Colour, light/dark and text size went back to the shop default; the screen mode could not be reset: {0}",
        YapModHatasi = "Colour, light/dark and text size are now the shop default; the screen mode could not be saved: {0}",
    };

    public static GorunumKartiMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
