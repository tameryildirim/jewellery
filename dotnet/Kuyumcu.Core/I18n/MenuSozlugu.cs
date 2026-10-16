namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-b — KATLANIR KENAR ÇUBUĞU METİNLERİ (TR + EN)
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs 9200+ satırlık ORTAK HUB'dır, aynı dalgada
//   başka şeritler de ona dokunuyor ve SozlukKabuk'un alanları `required`
//   olduğu için oraya tek alan eklemek İKİ sözlük gövdesini birden düzenlemeyi
//   gerektirirdi. Yeni metin bu yüzden buraya, kapalı bir sözlüğe yazıldı
//   (GorunumSozlugu / HavaSozlugu / BenzersizlikSozlugu ile aynı desen).
//
// ⚠ UX-S31 — çıplak metin YASAK: kenar çubuğunun tek bir Türkçe dizgesi bile
//   Razor içinde durmaz; ekran her zaman buradan çevirir. İki dil YAN YANA
//   durur, biri unutulamaz.
//
// ⚠ Kaynak: kurucunun onayladığı tasarım tuvali
//   dokumanlar/02-mimari/sadelestirme/tuval/project/Panel.dc.html <nav> +
//   canvas.json notu n2 ("menüde HİÇBİR ekran kaybolmadı").
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Kenar çubuğu katlama metinleri — dil kodu "tr" | "en".</summary>
public static class MenuSozlugu
{
    private static bool En(string dil) => dil == "en";

    /// <summary>"Tüm ekranlar (38)" — sayı MenuTanimi'ndan gelir, elle yazılmaz.</summary>
    public static string TumEkranlar(string dil, int adet) =>
        En(dil) ? $"All screens ({adet})" : $"Tüm ekranlar ({adet})";

    /// <summary>
    /// "Tüm ekranlar" bağlantısının ne açtığını söyleyen metin (title +
    /// aria-describedby). S1-b'de burada KİLİT sebebi yazıyordu; S1-d karo
    /// sayfasını açınca kilit kalktı.
    /// ⚠ Paket kodu KULLANICI METNİNE YAZILMAZ (plan § 5B "jargon sıfır").
    /// </summary>
    public static string TumEkranlarAciklama(string dil) => En(dil)
        ? "Opens every screen you are allowed to use as one page of tiles, grouped like this menu."
        : "Yetkili olduğun her ekranı tek sayfada karo olarak açar; gruplar bu menüdekiyle aynı.";

    /// <summary>Kapalı grubu açan düğmenin erişilebilir adı.</summary>
    public static string GrubuAc(string dil, string grup) =>
        En(dil) ? $"Open the {grup} group" : $"{grup} grubunu aç";

    /// <summary>Açık grubu kapatan düğmenin erişilebilir adı.</summary>
    public static string GrubuKapat(string dil, string grup) =>
        En(dil) ? $"Close the {grup} group" : $"{grup} grubunu kapat";

    /// <summary>Grup başlığındaki ekran sayısının erişilebilir karşılığı.</summary>
    public static string EkranSayisi(string dil, int adet) =>
        En(dil) ? $"{adet} screens" : $"{adet} ekran";

    /// <summary>
    /// Katlamanın bir kez anlatıldığı ipucu (menüde KALICI PARAGRAF YOKTUR —
    /// P-37 kuralı; açıklama düğmenin kendi title'ında durur).
    /// </summary>
    public static string KatlamaIpucu(string dil) => En(dil)
        ? "Press a group heading to open it; press again to close. A closed group still lists its screen names on the line below — nothing disappears from the menu."
        : "Grup başlığına bas: ekranlar açılır, tekrar basınca kapanır. Kapalı grubun altındaki satır o gruptaki ekranların adlarını yazar — menüden hiçbir ekran kaybolmaz.";

    /// <summary>Önizleme satırının ekran okuyucuya söylenen hâli.</summary>
    public static string OnizlemeEtiketi(string dil) =>
        En(dil) ? "Screens in this group" : "Bu gruptaki ekranlar";

    /// <summary>Önizleme satırındaki ekran adlarını ayıran işaret.</summary>
    public const string OnizlemeAyraci = " · ";

    /// <summary>
    /// Kayıtlı katlama tercihini silip varsayılana (yalnız açık ekranın grubu
    /// açık) dönen düğme. Yalnız kullanıcı en az bir kez katladıysa görünür.
    /// </summary>
    public static string KatlamayiSifirla(string dil) =>
        En(dil) ? "Reset groups" : "Grupları sıfırla";

    /// <summary>Katlamayı sıfırla düğmesinin ne yaptığını anlatan ipucu.</summary>
    public static string KatlamayiSifirlaIpucu(string dil) => En(dil)
        ? "Forget which groups you opened or closed; only the group of the open screen stays open."
        : "Hangi grupları açıp kapattığını unutur; yalnız açık ekranın grubu açık kalır.";

    /// <summary>Katlama tercihi sunucuya yazılamadığında görünen bant metni.</summary>
    public static string KatlamaKaydedilemedi(string dil) => En(dil)
        ? "Menu layout could not be saved."
        : "Menü düzeni kaydedilemedi.";
}
