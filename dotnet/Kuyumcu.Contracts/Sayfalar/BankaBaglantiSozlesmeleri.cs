namespace Kuyumcu.Contracts.Sayfalar;

// D2-05b (19.10) — banka bağlantısı (GET/PUT /bankalar/{id}/baglanti, GET /bankalar/baglayicilar).
public sealed record BankaAlaniOzet(string Ad, string Etiket, bool Gizli, bool Zorunlu, string? Ipucu);
public sealed record BankaBaglayiciOzet(string Kod, string BankaAdi, string BasvuruYolu, bool IpKisitli, IReadOnlyList<BankaAlaniOzet> Alanlar);
public sealed record BankaBaglayiciKatalogOzet(string? CikisIp, IReadOnlyList<BankaBaglayiciOzet> Bankalar);
public sealed record BankaBaglantiOzet(string? Kod, string? BankaAdi, string? Ozet, DateTime? SonDeneme, DateTime? SonBasari, string? SonHata);
public sealed record BankaBaglantiDenemesi(bool Basarili, int HareketSayisi, string? Hata);
public sealed record BankaBaglantiKayitSonucu(BankaBaglantiOzet Baglanti, BankaBaglantiDenemesi Deneme);
public sealed record BankaBaglantiDeneSonucu(BankaBaglantiOzet Baglanti, int Yeni, int Hatali);
