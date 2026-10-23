namespace Kuyumcu.Web.Servisler;

/// <summary>
/// Görsel denetim 2 (23.10, O-19 devamı): rol bilinirken sunucunun 403 döndüreceği uçlara istek HİÇ atılmaz
/// (tezgâhtar her ekranda /bildirimler/sayi, panelde kasa-defteri, müşterilerde vade-takibi, kurda geçmiş, bankada
/// karar-bekleyen, faturada özel matrah, vitrinde pano ayarları → konsol 403 kirliliği, boşa ağ). Rol listeleri
/// sunucudaki [RolGerekli] ile AYNI; son söz yine sunucunun.
/// </summary>
public static class RolKapisi
{
    /// <summary>Rapor uçları: SAHIP · MUDUR · MUHASEBE ([RolGerekli] Rapor/Banka karar/Fatura özel matrah/Kur geçmiş).</summary>
    public static bool RaporRolu(this OturumServisi oturum) =>
        oturum.Kullanici?.Rol is "SAHIP" or "MUDUR" or "MUHASEBE";

    /// <summary>Yönetim uçları: SAHIP · MUDUR (pano ayarları, kur kaynağı).</summary>
    public static bool YonetimRolu(this OturumServisi oturum) =>
        oturum.Kullanici?.Rol is "SAHIP" or "MUDUR";

    /// <summary>Rol henüz bilinmiyorsa (profil yüklenmedi) istek ATILIR — tahmin yok, karar sunucuda.</summary>
    public static bool RolBilinmiyor(this OturumServisi oturum) => oturum.Kullanici is null;

    /// <summary>
    /// Sayfalar profil (/auth/ben) gelmeden açılır; rol o anda bilinmez ve "bilinmiyor → istek at" kuralı 403'ü yine
    /// üretirdi (23.10 ölçümü: tezgâhtarda 15 ekranda 24 istek). Profil en çok 3 sn beklenir; gelmezse karar sunucuda.
    /// </summary>
    public static async Task<bool> ProfilBekle(this OturumServisi oturum, int enCokMs = 3000)
    {
        // Yalnız oturum varken (jeton var, profil yolda) beklenir; oturumsuz çizimde (bileşen testleri) bekleme yok.
        for (var g = 0; oturum.Kullanici is null && oturum.GirisYapildi && g < enCokMs; g += 100) await Task.Delay(100);
        return oturum.Kullanici is not null;
    }

    /// <summary>Rapor rolü var mı (profil beklenir)? Bilinemezse true — istek atılır, karar sunucuda.</summary>
    public static async Task<bool> RaporRoluBekle(this OturumServisi oturum) => !await oturum.ProfilBekle() || oturum.RaporRolu();

    /// <summary>Yönetim rolü var mı (profil beklenir)? Bilinemezse true.</summary>
    public static async Task<bool> YonetimRoluBekle(this OturumServisi oturum) => !await oturum.ProfilBekle() || oturum.YonetimRolu();
}
