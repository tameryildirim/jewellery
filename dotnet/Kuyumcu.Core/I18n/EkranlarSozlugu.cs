namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-d — "TÜM EKRANLAR" KARO SAYFASI METİNLERİ (TR + EN)
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs 9200+ satırlık ORTAK HUB'dır ve aynı dalgada
//   başka şeritler de ona dokunuyor (görev: "DÜZENLEME; yeni metin için AYRI
//   dosya aç"). MenuSozlugu / UstBarSozlugu ile aynı desen.
//
// ⚠ UX-S31 — çıplak metin YASAK: sayfanın tek bir Türkçe dizgesi bile Razor
//   içinde durmaz. Karo ADLARI burada DEĞİLDİR: onlar kenar çubuğuyla AYNI
//   kaynaktan (MenuTanimi → Sozluk.Nav) gelir — aynı ekran iki yerde iki ad
//   taşıyamaz.
//
// ⚠ Kaynak: kurucunun onayladığı tasarım tuvali
//   dokumanlar/02-mimari/sadelestirme/tuval/project/Ekranlar.dc.html.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Tüm ekranlar (karo menü) sayfası metinleri — dil kodu "tr" | "en".</summary>
public static class EkranlarSozlugu
{
    private static bool En(string dil) => dil == "en";

    /// <summary>Sayfa başlığı (tuval: "Tüm ekranlar").</summary>
    public static string Baslik(string dil) => En(dil) ? "All screens" : "Tüm ekranlar";

    /// <summary>Arama kutusunun yer tutucusu (tuval metni birebir).</summary>
    public static string AramaYerTutucu(string dil) =>
        En(dil) ? "Type a screen name: invoice, cash, count…" : "Ekran adı yaz: fatura, kasa, sayım…";

    /// <summary>Arama kutusunun erişilebilir adı.</summary>
    public static string AramaEtiketi(string dil) => En(dil) ? "Search screens" : "Ekran ara";

    /// <summary>
    /// Başlık satırının sağındaki ipucu. Sayı ELLE YAZILMAZ: kullanıcının
    /// yetkili olduğu karo sayısıdır (MenuTanimi'nden türer).
    /// </summary>
    public static string Ipucu(string dil, int adet) => En(dil)
        ? $"All {adet} screens are here · star one to add it to your favourites"
        : $"{adet} ekranın hepsi burada · yıldızla favorine ekle";

    /// <summary>Marka satırının (logo) erişilebilir adı — panele götürür.</summary>
    public static string PaneleDon(string dil) => En(dil) ? "Back to the dashboard" : "Panele dön";

    /// <summary>Aramanın ekran okuyucuya bildirilen sonuç sayısı.</summary>
    public static string SonucSayisi(string dil, int adet) =>
        En(dil) ? $"{adet} screens found" : $"{adet} ekran bulundu";

    /// <summary>Arama hiçbir karoyla eşleşmediğinde boş durum metni ("ne oldu + ne yap").</summary>
    public static string SonucYok(string dil, string sorgu) => En(dil)
        ? $"No screen is called “{sorgu}”. Try another word or clear the search."
        : $"“{sorgu}” adında bir ekran yok. Başka bir sözcük dene ya da aramayı temizle.";

    /// <summary>Boş durumdaki aramayı temizleme düğmesi.</summary>
    public static string AramayiTemizle(string dil) => En(dil) ? "Clear the search" : "Aramayı temizle";

    /// <summary>Karodaki yıldızın erişilebilir adı — hangi ekran olduğunu söyler.</summary>
    public static string FavoriyeEkle(string dil, string ekran) =>
        En(dil) ? $"Add {ekran} to favourites" : $"{ekran} — favorilere ekle";

    /// <summary>Dolu yıldızın erişilebilir adı.</summary>
    public static string FavoridenCikar(string dil, string ekran) =>
        En(dil) ? $"Remove {ekran} from favourites" : $"{ekran} — favorilerden çıkar";

    /// <summary>Favori yazılamadığında bant metni (değişiklik geri alındı).</summary>
    public static string FavoriKaydedilemedi(string dil, string sebep) => En(dil)
        ? $"The favourite could not be saved and was reverted: {sebep}"
        : $"Favori kaydedilemedi, geri alındı: {sebep}";

    /// <summary>Kullanıcı tercihleri okunamadığında bant metni.</summary>
    public static string TercihlerYuklenemedi(string dil, string sebep) => En(dil)
        ? $"Your favourites could not be loaded: {sebep}. Every screen still opens."
        : $"Favorilerin yüklenemedi: {sebep}. Ekranların hepsi yine de açılır.";

    /// <summary>Yetki listesi okunamadığında bant metni (karolar son bilinen hâlde).</summary>
    public static string YetkiYuklenemedi(string dil, string sebep) => En(dil)
        ? $"Screen permissions could not be loaded: {sebep}. The tiles show their last known state."
        : $"Ekran yetkileri yüklenemedi: {sebep}. Karolar son bilinen hâliyle gösteriliyor.";

    /// <summary>Sayfanın altındaki bağlantı şeridinin erişilebilir adı.</summary>
    public static string AltBaglantilar(string dil) => En(dil) ? "Other links" : "Diğer bağlantılar";
}
