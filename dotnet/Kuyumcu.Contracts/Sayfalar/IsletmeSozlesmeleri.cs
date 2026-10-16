// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Isletme.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.

namespace Kuyumcu.Contracts.Sayfalar.Isletme;
// ⚠ Yeni alan SONA ve VARSAYILANLI: eski sahte API yanıtları (alan yok)
// derlenmeye ve ayrıştırılmaya devam eder.
public sealed record TenantVeri(string Id, string Ad, string Plan, bool AiEtkin,
    string VarsayilanArayuzModu = "PROFESYONEL",
    // UX-1-C4: sunucu (GET /admin/tenant → Tenant varlığı) bu alanları ZATEN
    // döndürüyordu; ekran okumuyordu. Yeni istemci kayıt tipi AÇILMADI (MÜHÜR 5 KOŞUL 3),
    // mevcut tip sunucunun gerçek yanıt şekline hizalandı.
    string? Adres = null, string? Telefon = null,
    string? VergiDairesi = null, string? VergiNo = null,
    string? KurKaynagi = null, string? MaliyetYontemi = null,
    // paket F-2-min: kiracı durumu SUNUCUDAN gelir, ekran hesaplamaz (UX-S21).
    string Durum = "AKTIF", int? KalanGun = null, bool SaltOkunurMu = false);
public sealed record SubeVeri(string Id, string Ad, string? Adres, string? Telefon, bool Aktif);

// ── PAKET P-27 (F-1 abonelik + F-3 plan kapısı · R-1) ────────────────────────
// GET /api/v1/abonelik yanıtının istemci karşılığı. Alan adları VE tipleri
// sunucudaki AbonelikDurumDto ile birebir aynıdır (PLAN § 1 md.9 — ad ve tip
// yansımayla testte kilitlenir). Tümü VARSAYILANLI: eksik alanlı eski sahte
// yanıtlar ayrıştırılmaya devam eder.

public sealed record AbonelikPlanVeri(
    string Kod = "", string Ad = "", string? Aciklama = null, int Seviye = 0,
    int? KullaniciTavan = null, int? SubeTavan = null, bool Aktif = true);

public sealed record AbonelikDonemVeri(
    string Id = "", string PlanKodu = "", DateTime Baslangic = default, DateTime? Bitis = null,
    string Durum = "AKTIF", string Kaynak = "SISTEM",
    string? Gerekce = null, string? OperatorAdi = null);

public sealed record AbonelikVeri(
    string PlanKodu = "", string PlanAdi = "", int? Seviye = null,
    AbonelikDonemVeri? Donem = null, int? KalanGun = null,
    bool SuresiDolduMu = false, bool YazabilirMi = true,
    bool YukseltmeKiraciyaKapali = true, string YukseltmeKapaliSebebi = "",
    IReadOnlyList<AbonelikPlanVeri>? Planlar = null,
    IReadOnlyList<string>? IndirilebilirPlanlar = null,
    IReadOnlyList<AbonelikDonemVeri>? Gecmis = null,
    int GecmisSiniri = 50, bool GecmisSinirinaDegdi = false);
