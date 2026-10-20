namespace Kuyumcu.Contracts.Sayfalar;

// E-belge (20.10) — entegratör bağlantısı ve giden kutusu (GET/PUT /ebelge/ayar, GET /ebelge/gonderimler…).
public sealed record EBelgeSaglayiciOzet(string Kod, string Ad);
public sealed record EBelgeAyarOzet(string? Saglayici, string? Ortam, string? SeriOnEki, bool Etkin, bool Kayitli,
    DateTime? Baslangic, DateTime? SonDeneme, string? SonHata, IReadOnlyList<EBelgeSaglayiciOzet> Saglayicilar);
public sealed record EBelgeAyarKayitSonucu(EBelgeAyarOzet Ayar, bool Basarili, string? Hata);
public sealed record EBelgeOlayOzet(DateTime Zaman, string? EskiDurum, string YeniDurum, string Kaynak, string? Mesaj);
public sealed record EBelgeKutuSatiri(EBelgeGonderimOzet Gonderim, string? FaturaNo, string? AliciUnvan, decimal Toplam, DateTime Tarih);
public sealed record EBelgeKutuYaniti(IReadOnlyList<EBelgeKutuSatiri> Satirlar, IReadOnlyDictionary<string, int> Sayaclar, int Toplam, bool DahaFazlasiVar);
public sealed record EBelgeGonderimOzet(string FaturaId, string Durum, string? Senaryo, string? BelgeNo, string? SonHata,
    int DenemeSayisi, DateTime? Gonderildi, DateTime? Sonuclandi, bool IptalEdilebilir);
