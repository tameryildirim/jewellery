using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Vezne;

// ═════════════════════════════════════════════════════════════════════════════
// VEZNE KAPISI (paket 19 Faz A) — SAF sınıf, tek karar noktası.
//
// Kapılar ayrı sınıftadır ki mutasyonla TEK satır bozulduğunda hangi testin
// kırılacağı belirli olsun (TevsikKapisi / GunKapanisiKapisi deseni). Servisin
// içine gömülselerdi, çok katmanlı koruma mutasyonu yutar ve test açığı
// görünmez kalırdı (Aşkın'ın koşulu).
//
// ⚠ Veritabanı erişimi YOKTUR: her karar parametrelerden verilir.
// ═════════════════════════════════════════════════════════════════════════════

public static class VezneKapisi
{
    /// <summary>Fişin kendi kaydı üzerinde söz sahibi asgari rol seviyesi (MUDUR).</summary>
    public const string SahiplikAsgariRol = "MUDUR";

    /// <summary>
    /// R2 — DÖRT GÖZ: vezne ayrımı açıkken bir fişi HAZIRLAYAN, onu TAHSİL EDEMEZ.
    /// Ayar kapalıyken kural uygulanmaz (tek kişilik dükkân kilitlenmesin).
    /// </summary>
    public static void DortGozDogrula(string hazirlayanId, string tahsilEdenId, bool vezneZorunlu)
    {
        if (!vezneZorunlu) return;
        if (hazirlayanId == tahsilEdenId)
            throw new BadRequestException(
                "Kendi hazırladığınız fişi tahsil edemezsiniz (vezne ayrımı zorunlu). " +
                "Fişi başka bir kullanıcı tahsil etmelidir.");
    }

    /// <summary>
    /// R3 — TEK YÖNLÜ DURUM: tahsil YALNIZ VEZNEDE fişten yapılır. ACIK fiş
    /// tahsil edilemez (tutar henüz damgalanmadı), TAHSIL fiş ikinci kez
    /// tahsil edilemez (defterde ikinci satır doğardı), IPTAL fiş hiç.
    /// </summary>
    public static void TahsilDurumuDogrula(string durum)
    {
        if (durum != VezneDurum.Veznede)
            throw new ConflictException(
                $"Yalnız vezneye gönderilmiş fiş tahsil edilebilir (bu fişin durumu: {durum}).");
    }

    /// <summary>
    /// R4 — FİYAT KİLİDİ: kalem/fiyat yalnız ACIK fişte değişir. Değişebilseydi
    /// tezgâhtar, müşteri vezneye yürürken fiyat kırabilir; kuyrukta görünen
    /// tutar ile tahsil edilen tutar ayrışırdı.
    /// </summary>
    public static void DuzenlemeDurumuDogrula(string durum)
    {
        if (durum != VezneDurum.Acik)
            throw new ConflictException(
                $"Yalnız ACIK durumdaki fiş düzenlenebilir (bu fişin durumu: {durum}). " +
                "Vezneye gönderilmiş fişin tutarı damgalanmıştır.");
    }

    /// <summary>
    /// R5 — İPTAL: yalnız ACIK ve VEZNEDE fiş iptal edilir. TAHSİL edilmiş fiş
    /// iptal EDİLEMEZ; para el değiştirmiştir, doğru yol iadedir (paket 16).
    /// </summary>
    public static void IptalDurumuDogrula(string durum)
    {
        if (durum is VezneDurum.Acik or VezneDurum.Veznede) return;
        throw new ConflictException(durum == VezneDurum.Tahsil
            ? "Tahsil edilmiş fiş iptal edilemez. Para iade edilecekse satış iadesi yolu kullanılır."
            : "Bu fiş zaten iptal edilmiş.");
    }

    /// <summary>
    /// SAHİPLİK (Mehmet koşulu): fişi HAZIRLAYAN ya da MUDUR+ değiştirir.
    /// Ekranda düğmenin gizlenmesi kapı SAYILMAZ — karar sunucudadır.
    /// </summary>
    public static void SahiplikDogrula(string hazirlayanId, AktifKullanici islem)
    {
        if (islem.Id == hazirlayanId) return;
        if (AktifKullanici.Seviye(islem.Rol) >= AktifKullanici.Seviye(SahiplikAsgariRol)) return;
        throw new ForbiddenException(
            "Bu fişi yalnız hazırlayan kullanıcı ya da müdür/sahip değiştirebilir.");
    }
}
