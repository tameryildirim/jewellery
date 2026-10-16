namespace Kuyumcu.Core.Domain;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ISLEM-BANT-2 (10.10) — işlem ekranlarının ROL KÜMELERİ, TEK KAYNAK.
// Sunucu [RolGerekli(...)] öznitelikleri derleme sabiti ister (dizi alanı veremez);
// bu yüzden öznitelik metni AYNEN kalır ve "tek kaynak" bir SÖZLEŞME TESTİYLE
// kilitlenir (IslemBant2Testleri: SatisController.HurdaAlis özniteliği == HurdaAlis).
// Ekran bu kümeyi okur: kümeye girmeyen rol formu kilitli + sebepli görür, sunucuya
// 403 alacağı istek hiç gitmez. Bu bir YETKİ GEVŞETMESİ değildir: son söz sunucunun.
// ═══════════════════════════════════════════════════════════════════════════
public static class IslemRolleri
{
    /// <summary>POST /islemler/hurda-alis — [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")] ile AYNI küme.</summary>
    public static readonly IReadOnlyList<string> HurdaAlis = ["SAHIP", "MUDUR", "MUHASEBE"];

    /// <summary>Rol bu kümede mi? Rol bilinmiyorsa (null/boş) HAYIR.</summary>
    public static bool Icinde(IReadOnlyList<string> kume, string? rol) =>
        !string.IsNullOrEmpty(rol) && kume.Contains(rol);
}
