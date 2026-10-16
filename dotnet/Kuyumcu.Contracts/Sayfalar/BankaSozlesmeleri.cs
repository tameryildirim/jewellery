// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Banka.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.
using System.Globalization;

namespace Kuyumcu.Contracts.Sayfalar.Banka;
// ── veri modelleri (JSON camelCase otomatik eşleşir) ──
public sealed record HesapVeri(
    string Id, string BankaAdi, string HesapAdi, string? HesapSahibi, string Iban,
    string ParaBirimi, decimal Bakiye, bool Aktif);
public sealed record HesapYaniti(List<HesapVeri> Hesaplar);
// PAKET P-12 (B-42): dekontun storno ve köken bilgisi EKRANA da gelir —
// "bu satır geri alındı mı" ve "bu satır bankadan mı geldi" sorularının
// cevabı sunucudadır, ekran tahmin etmez (UX-S30: kilidin sebebi yazılır).
// Alan adı VE tipi sunucuyla doğrulandı (Data/Entities.cs BankaHareket:
// StornoKaynakId string? · EkstreSatirId string? · KarsiIban string?).
public sealed record HareketVeri(
    string Id, string Yon, decimal Tutar, string? KarsiTarafAd,
    string? Aciklama, string Durum, DateTime Tarih,
    string? KarsiIban = null, string? StornoKaynakId = null, string? EkstreSatirId = null,
    // PAKET P-19 (Y-11) — sapma S1 kapanışı: dekont bir VİRMANIN bacağı mı?
    //   Doluysa tek başına stornolanamaz (StornoServisi.BankaHareketiStorno kapısı);
    //   ekran storno düğmesini kilitler ve /gider → Virman sekmesine yönlendirir.
    //   Alan adı VE tipi sunucuyla doğrulandı (Data/Entities.cs:1042 KarsiBacakId string?).
    string? KarsiBacakId = null,
    // PAKET Q9-1 — dekontun eşleştiği müşteri (BankaHareket.CariId string?,
    //   Data/Entities.cs). "Eşleşti" rozetinin müşteri bağından mı (değiştirilemez,
    //   MASAK toplamı) yoksa POS/virman bağından mı geldiğini ekran TAHMİN ETMEZ.
    string? CariId = null);
public sealed record HareketYaniti(List<HareketVeri> Hareketler);
public sealed record CariVeri(string Id, string Ad);
