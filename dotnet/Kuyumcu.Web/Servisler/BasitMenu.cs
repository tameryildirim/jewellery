namespace Kuyumcu.Web.Servisler;

/// <summary>
/// Basit modun ÇEKİRDEK ekranlarının TEK listesi (paket UX-3 Faz A).
///
/// <para>
/// ⚠ PAKET S1-g (2026-09-24, kurucu: "menüyü kontrol et, olmamış"): bu liste
/// artık kenar çubuğunu DARALTMAZ. Kurucunun tuvali (Panel.dc.html basit yüz,
/// Tezgah.dc.html) basit modda da 6 katlanır grup ve yetkili HER ekranı
/// gösterir — "menüde HİÇBİR ekran kaybolmadı" (canvas.json notu n2). Eskiden
/// basit modda menü bu 8 satıra iniyor, 30 ekran menüden kayboluyordu.
/// Liste SİLİNMEDİ (dosya silme yasak, özellik silinmez): basit modun
/// çekirdeğini belgeler ve denetim.sh BASIT_MOD_TUTARSIZ sayacı üst bar
/// rozetlerinin hedefinin bu çekirdekte olduğunu ölçmeye devam eder.
/// </para>
///
/// <para>
/// ⚠ Bu liste bir YETKİ DEĞİLDİR. Menü daraltması <c>YetkiServisi.Erisebilir</c>
/// koşulunun ÜSTÜNE eklenir, onun YERİNE geçmez (Bertan KOŞUL 7 · Mehmet K8):
/// yetkisi olmayan kullanıcı basit modda da o bağlantıyı görmez, basit moddaki
/// yetkili ise adresi doğrudan yazarak sayfaya girebilir — gizleme kapı değildir.
/// </para>
/// <para>
/// Karar tek yerdedir: razor dosyalarına dağılmış <c>@if (basit)</c> koşulları
/// yazılmaz. Bir sayfanın basit modda görünürlüğü değişecekse YALNIZ bu dizi
/// değişir (mimari belgesindeki sayfa envanteri "basit modda" sütunu).
/// </para>
/// </summary>
public static class BasitMenu
{
    /// <summary>
    /// Basit modda görünen rotalar. Kaynak: MIMARI-KALAN-PAKETLER-2026-09-15.md
    /// sayfa envanteri — "Görünür" işaretli ve menüde yer alan sayfalar.
    /// Kılavuz ve Ayarlar kenar çubuğunun ALT bloğundadır; her modda görünür,
    /// bu yüzden listede yer almazlar.
    /// </summary>
    public static readonly string[] Yollar =
    [
        "/",                    // Panel
        "/islemler",            // Satış (POS)
        "/cariler",             // Müşteriler
        "/urunler",             // Ürünler
        "/urunler/etiketler",   // Barkod/etiket basma — perakendede günlük iş
        "/kasalar",             // Kasa
        "/raporlar",            // Günlük özet
        // 6-A doğrulayıcı devri (UX-1-C4): bildirim ROZETİ basit modda üst
        // çubukta görünüyordu ama sayfaya gidilecek bağlantı yoktu — rozete
        // tıklayan kullanıcı hiçbir yere varamıyordu (UX-3 tutarsızlığı).
        "/bildirimler",
    ];

    /// <summary>Verilen rota basit modda görünür mü?</summary>
    public static bool Icerir(string yol) => Array.IndexOf(Yollar, yol) >= 0;
}
