namespace Kuyumcu.Contracts.Sayfalar.Kvkk;

// ═════════════════════════════════════════════════════════════════════════════
// D1-19 (18.10) — KVKK aydınlatma metni ve müşteri beyanları.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>İşletmenin geçerli aydınlatma metni; hiç girilmediyse Surum null.</summary>
public sealed record KvkkMetinDurumu(int? Surum, string? Metin, DateTime? Tarih, int AydinlatilanMusteri, int PazarlamaIzniOlan);

/// <summary>Müşteri beyan geçmişinin satırı.</summary>
public sealed record KvkkKayitSatiri(string Tur, bool Deger, int? MetinSurumu, string Kanal, string? KullaniciAd, DateTime Tarih);

/// <summary>Müşterinin KVKK durumu (son beyanlar + geçmiş).</summary>
public sealed record KvkkCariDurumu(
    DateTime? AydinlatmaTarihi, int? AydinlatmaSurumu, int? GuncelSurum,
    bool PazarlamaIzni, DateTime? PazarlamaTarihi, List<KvkkKayitSatiri> Gecmis);
