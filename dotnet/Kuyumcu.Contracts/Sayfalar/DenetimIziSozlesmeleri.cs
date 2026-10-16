// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/DenetimIzi.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.

namespace Kuyumcu.Contracts.Sayfalar.DenetimIzi;
// ── API sözleşmesi (camelCase JSON) ──────────────────────────────────────
public sealed record Kayit(
    string Id, DateTime Tarih, string Eylem, string VarlikTuru, string? VarlikId,
    bool Hassas, string? Detay, string? IpAdresi, string? KullaniciId,
    string Kullanici, string? Rol,
    string? SubeId = null, string? SubeAd = null,
    string? KasaId = null, string? KasaAd = null);
public sealed record SubeOzet(string Id, string Ad);
public sealed record SubeSayim(string? SubeId, string? SubeAd, int Adet);
public sealed record Sayfa(int Toplam, int Limit, int Offset, List<Kayit> Kayitlar);
public sealed record TurSayim(string VarlikTuru, int Adet);
public sealed record EylemSayim(string Eylem, int Adet);
public sealed record Ozet(
    int Toplam, int HassasAdet, DateTime? EnEskiKayit,
    List<EylemSayim> Eylemler, List<TurSayim> Turler,
    List<SubeSayim>? Subeler = null);
