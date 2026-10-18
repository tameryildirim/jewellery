// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Kullanicilar.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.

namespace Kuyumcu.Contracts.Sayfalar.Kullanicilar;
// ⚠ Yeni alan SONA ve VARSAYILANLI: mevcut sahte API yanıtlarında alan yoktur.
public sealed record KullaniciVeri(
    string Id, string Ad, string Soyad, string Email, string Rol,
    bool Aktif, DateTime? SonGiris, string? ArayuzModu = null, string? SubeId = null,
    // 45-UI: liste ucu bu bayrağı döndürür; tekil uçlarda null (bilinmiyor).
    bool? MfaEtkin = null,
    // D1-14 — salt okunur (mali müşavir) hesap.
    bool SaltOkunur = false,
    // D1-05 — kişisel iskonto sınırı (yüzde); boş = sınır yok.
    decimal? IskontoSiniriYuzde = null);
public sealed record SubeOzet(string Id, string Ad);
