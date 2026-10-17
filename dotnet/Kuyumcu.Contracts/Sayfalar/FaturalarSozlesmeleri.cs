// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Faturalar.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.
using System.Globalization;
using System.Text.Json;

namespace Kuyumcu.Contracts.Sayfalar.Faturalar;
// ── veri modelleri (JSON camelCase otomatik eşleşir) ──
// ⚠ PAKET P-22 / B-3: FaturaNo TASLAK belgede NULL'dur (seri kesimde tüketilir).
//   CariId düzenleme çekmecesinin doldurulması için eklendi.
public sealed record FaturaVeri(
    string Id, string? FaturaNo, string CariAd, string Tur,
    decimal AraToplam, decimal KdvOran, decimal KdvTutar, decimal Toplam,
    string Durum, string? BankaHareketId, DateTime Tarih,
    string KalemKaynagi = "BASLIK", string HasDegerKaynagi = "ELLE",
    string? CariId = null, string? Aciklama = null,
    // P-53: kuyum (özel matrah) taslağında toplam başlıktan değişmez — sunucu
    // kuralı KdvTuru'na bakar (HasDegerKaynagi'na DEĞİL); ekran aynı alanı okur.
    string KdvTuru = "STANDART",
    // Q9-2 (UX-S32): taslağa eklenen satırın tutar alanları belgenin para
    // birimini taşır — sunucu yanıtında zaten vardı (CariliYanit.ParaBirimi).
    string ParaBirimi = "TRY",
    // TOPLU-1 tur 2: belge bir İŞLEMDEN (satış/alış hareketi) mi kesildi — sunucu kalemlerin defter
    // bağından türetir; doluysa listede "Satıştan/Alıştan geldi", boşsa "Manuel" (eski davranış).
    bool IslemdenGeldi = false);
public sealed record KalemVeri(
    string Id, int Sira, string Aciklama, decimal Miktar, string Birim,
    string? AyarId, decimal? Milyem, decimal? GramAgirlik, decimal? HasGrami,
    string? TasIbaresi, decimal BirimFiyat, decimal? IscilikTl, decimal? IndirimTutari,
    string KdvTuru, decimal KdvOran, decimal HasDeger, decimal KdvMatrah,
    decimal KdvTutar, decimal SatirToplam,
    // ZO-BELGE (30.09): kalemin madeni — TEK KAYNAK sunucudaki kalem.maden; NULL = bilinmiyor.
    string? Maden = null,
    // KESIF-P3 (TOPLU 08:50): defter bağı — sunucu yanıtında zaten vardı (hareketId);
    // doluysa adet fişten gelir ve ekranda salt okunur (sunucu 400 kuralıyla aynı).
    string? HareketId = null,
    // TOPLU-1 tur 2: bağlı hareketin FİŞİ yok (tek işlem) — adet kilidi metni "satış fişinden"
    // demez, "satış kaydından" der ve işlem defterine götürür. Sunucu türetir; varsayılan = fişli.
    bool HareketFissiz = false);
public sealed record KalemYaniti(
    string FaturaId, string? FaturaNo, string KalemKaynagi, string HasDegerKaynagi,
    decimal Toplam, List<KalemVeri> Kalemler);
/// <summary>GET /islemler/fis/{fisId} yanıtı (paket 1 Faz A okuma ucu).</summary>
public sealed record FisKalemVeri(
    string Id, string Tur, decimal TlTutari, decimal HasGrami, bool Hediye,
    string? UrunAd, string? FaturaNo);
public sealed record FisYaniti(
    string FisId, string? CariId, string? CariAd, string? FaturaNo,
    int KalemSayisi, decimal ToplamTl, decimal ToplamHas, List<FisKalemVeri> Kalemler);
public sealed record UrunVeri(string Id, string Ad);
public sealed record FaturaYaniti(List<FaturaVeri> Faturalar);
public sealed record CariVeri(string Id, string Ad);
public sealed record FaturalanabilirIslem(
    string Id, string? FisId, string Tur, DateTime Tarih,
    decimal TlTutari, decimal HasGrami, string? CariId, string? UrunAd);
public sealed record FaturalanabilirYanit(
    int Gun, int Limit, bool DahaVar, List<FaturalanabilirIslem>? Islemler);
public sealed record OzelMatrahOnizleme(
    string HareketId, string? FisId, List<string> KapsananHareketIdler,
    int KapsananHareketSayisi, decimal HasGram, decimal HasDeger,
    decimal? HasBirimFiyat, int KullanilanKurSayisi, decimal DefterToplami,
    decimal KdvOran, decimal? Matrah, decimal? KdvTutar, decimal? AraToplam,
    string HasDegerKaynagi, string? FaturaNo, string? Uyari,
    // ZO-BELGE (30.09): kümede altın dışı has bacağı var mı (not yalnız o zaman görünür).
    bool AltinDisiMadenVar = false);
// 18.10 — toplu fatura (GET/POST /faturalar/toplu; sunucu bu tipleri döndürür — derleyici kilidi).
public sealed record TopluAday(
    string Anahtar, string? FisId, string? HareketId, DateTime Tarih, string? CariId, string? CariAd,
    decimal TutarTl, int KalemSayisi, bool Kesilebilir, string? Sebep);
public sealed record TopluAdayListesi(int Gun, List<TopluAday> Adaylar);
public sealed record TopluSonucSatiri(string Anahtar, string? FaturaId, string? FaturaNo, string? Durum, string? Hata);
public sealed record TopluSonuc(int Kesilen, int Hatali, List<TopluSonucSatiri> Satirlar);
