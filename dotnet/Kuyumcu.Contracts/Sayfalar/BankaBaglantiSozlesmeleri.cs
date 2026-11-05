namespace Kuyumcu.Contracts.Sayfalar;

// D2-05b (19.10) — banka bağlantısı (GET/PUT /bankalar/{id}/baglanti, GET /bankalar/baglayicilar).
public sealed record BankaAlaniOzet(string Ad, string Etiket, bool Gizli, bool Zorunlu, string? Ipucu);
public sealed record BankaBaglayiciOzet(string Kod, string BankaAdi, string BasvuruYolu, bool IpKisitli, IReadOnlyList<BankaAlaniOzet> Alanlar);
public sealed record BankaBaglayiciKatalogOzet(string? CikisIp, IReadOnlyList<BankaBaglayiciOzet> Bankalar);
/// <summary>Degerler: gizli OLMAYAN alanların kayıtlı değerleri (ör. onay numarası, uygulama kimliği) — "Bilgileri değiştir"
/// formu dolu açılır (kurucu 05.11). Gizli alan (şifre) hiçbir zaman dönmez; boş bırakılırsa aynı kalır.</summary>
public sealed record BankaBaglantiOzet(string? Kod, string? BankaAdi, string? Ozet, DateTime? SonDeneme, DateTime? SonBasari, string? SonHata,
    IReadOnlyDictionary<string, string?>? Degerler = null);
public sealed record BankaBaglantiDenemesi(bool Basarili, int HareketSayisi, string? Hata);
public sealed record BankaBaglantiKayitSonucu(BankaBaglantiOzet Baglanti, BankaBaglantiDenemesi Deneme);
public sealed record BankaBaglantiDeneSonucu(BankaBaglantiOzet Baglanti, int Yeni, int Hatali);
