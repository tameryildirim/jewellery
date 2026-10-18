namespace Kuyumcu.Contracts.Sayfalar.Cariler;

/// <summary>D2-12 — müşterinin dileği.</summary>
public sealed record CariDilekOzet(string Id, string Aciklama, string? UrunId, string? UrunAd, DateTime Olusturuldu, bool Kapali);

/// <summary>D2-12 — sadakat puanı kullanımı.</summary>
public sealed record PuanKullanimOzet(int Puan, string Aciklama, DateTime Tarih, string? KullaniciAd);

/// <summary>D2-12 — müşterinin puan durumu. Etkin değilse diğer alanlar 0.</summary>
public sealed record SadakatDurumu(bool Etkin, decimal? TlBasinaPuan, DateOnly? Baslangic, decimal NetAlisverisTl, int Kazanilan, int Kullanilan, int Bakiye, IReadOnlyList<PuanKullanimOzet> Kullanimlar);

/// <summary>D2-12 — işletmenin sadakat ayarı (her TlBasina TL net alışveriş = 1 puan; Baslangic'tan itibaren).</summary>
public sealed record SadakatAyari(decimal? TlBasina, DateOnly? Baslangic);

/// <summary>D2-12 — uykuda müşteri: son alışverişi N günden eski.</summary>
public sealed record UykudaMusteri(string CariId, string Ad, string? Telefon, DateTime SonAlisveris, int GecenGun, decimal ToplamAlisverisTl);
