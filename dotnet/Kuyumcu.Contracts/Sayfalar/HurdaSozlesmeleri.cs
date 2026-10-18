// PAKET P-20 (Y-10) — hurda stok / ramat çıkışı ekranının istemci tipleri.
// Kural (PLAN § 1): yeni istemci record'u razor içine YAZILMAZ, buraya yazılır.
// Alan adları VE TİPLERİ sunucu yanıtıyla birebir doğrulandı:
//   Modules/Hurda/HurdaModulu.cs → HurdaStokServisi.Bakiye() / Defter()
//   Modules/Satis/SatisModulu.cs → SatisServisi.HurdaSatis()

namespace Kuyumcu.Contracts.Sayfalar.Hurda;

/// <summary>
/// Bir madenin hurda has bakiyesi ve bileşenleri (defterden TÜRETİLİR).
/// ⚠ Ad sunucudaki <c>HurdaBakiyeSatiri</c>'ndan BİLEREK farklıdır: aynı adın
/// iki tanımı denetim.sh CONTRACTS_DISI_MODEL sayacını kırar (P-03).
/// </summary>
public sealed record HurdaBakiyeVeri(
    string Maden, string MadenAdi, decimal HasGram, decimal GirenHas, decimal CikanHas,
    /// <summary>
    /// PAKET P-20b — bu madende yazılmış TAHLİL FARKI bacaklarının işaretli
    /// toplamı. Ayrı alandır ki kart dürüst olsun:
    /// <c>giren − çıkan + fark = has bakiye</c>. Farkı "çıkan"ın içine
    /// saklasaydık ekran "5 çıktı" derken defterde 4,8 satış + 0,2 fark
    /// olurdu ve iki ekran aynı soruya iki cevap verirdi (UX-S22).
    /// </summary>
    decimal TahlilFarkiHas);

public sealed record HurdaBakiyeYaniti(
    string SubeId, List<HurdaBakiyeVeri> Satirlar, bool FireKaydiAcik, string? FireNotu,
    /// <summary>
    /// PAKET ŞUBE-KAPSAM-A — "SUBE" (tek şube) ya da "TUM_SUBELER" (birleşik).
    /// Geriye uyumlu: eski istemci alanı okumaz.
    /// </summary>
    string? Kapsam = null,
    /// <summary>
    /// PAKET ŞUBE-KAPSAM-A — birleşik yanıtta şube kırılımı. Birleşik
    /// <c>Satirlar</c> bu kırılımın maden bazında TOPLAMIDIR (tek türetme,
    /// K-4: kırılım toplamı = birleşik toplam). Tek şube yanıtında null.
    /// </summary>
    List<HurdaSubeKirilimi>? Subeler = null);

/// <summary>PAKET ŞUBE-KAPSAM-A — bir şubenin maden bazında hurda has bakiyesi.</summary>
public sealed record HurdaSubeKirilimi(string SubeId, string Ad, List<HurdaBakiyeVeri> Satirlar);

public sealed record HurdaDefterSatiri(
    string Id, string Tur, DateTime Tarih, decimal TlTutari, decimal HasGrami,
    string Maden, string MadenAdi, string? CariId, string? Aciklama,
    string? ParaBirimi, bool StornoSatiri, bool Stornolandi,
    /// <summary>
    /// PAKET P-20b — bu satır bir TAHLİL FARKI bacağıysa, farkı doğuran
    /// hurda satışının kimliği; değilse null. Ekran bu alanla farkı satışının
    /// altına bağlar (ikinci bir türetme yapmaz — UX-S22).
    /// </summary>
    string? TahlilKaynakHareketId,
    /// <summary>
    /// PAKET P-20b — bu satır bir HURDA_SATIS ise ona yazılmış tahlil farkı
    /// bacaklarının HAS TOPLAMI (işaretli); fark yoksa 0. "Düzelt" düğmesi ve
    /// lehte/aleyhte rozeti bu değerden çizilir.
    /// </summary>
    decimal TahlilFarkiHasGram);

public sealed record HurdaDefterYaniti(
    List<HurdaDefterSatiri> Satirlar, int ToplamAdet, int Limit,
    bool DahaFazlasiVar, string? SinirNotu);

/// <summary>POST /islemler/hurda-satis yanıtı — banda YAZILAN sonuç (UX-S27).</summary>
public sealed record HurdaSatisSonucu(
    string Id, string Maden, string MadenAdi, decimal HasGrami,
    decimal BirimFiyatTlHasGram, decimal ToplamTl, decimal TahsilEdilenTl, decimal KalanTl,
    decimal KalanHurdaHasGram, decimal? KasaBakiyesi, string ParaBirimi, DateTime Tarih,
    bool FireKaydiAcik, string? FireNotu,
    /// <summary>PAKET P-20b — bizim kaydımız has (stoktan çıkan TOPLAM).</summary>
    decimal BizimKaydimizHasGram,
    /// <summary>PAKET P-20b — alıcının kabul ettiği has (TL bu hasla ödenir).</summary>
    decimal AliciKabulHasGram,
    /// <summary>
    /// PAKET P-20b — İŞARETLİ tahlil farkı (kabul − bizim kaydımız). 0 = fark
    /// yok, bacak YAZILMADI. Negatif = ALEYHTE, pozitif = LEHTE.
    /// </summary>
    decimal TahlilFarkiHasGram,
    /// <summary>Yazıldıysa tahlil farkı bacağının kimliği; yoksa null.</summary>
    string? TahlilFarkiHareketId);

/// <summary>
/// PAKET P-20b — POST /islemler/hurda-satis/{id}/tahlil-duzeltme yanıtı.
/// Düzeltme, satırın ÜZERİNE YAZMAZ: aynı satışa ikinci bir fark bacağı ekler.
/// </summary>
public sealed record TahlilDuzeltmeSonucu(
    string HurdaSatisId, string DuzeltmeHareketId, string Maden, string MadenAdi,
    decimal EkFarkHasGram, decimal ToplamTahlilFarkiHasGram,
    decimal KalanHurdaHasGram, DateTime Tarih);

/// <summary>
/// PAKET Q10-2 v2 (doğrulayıcı KRİTİK) — ramat çıkışı onayında DONDURULAN gövde.
/// "Ramat çıkışını kaydet" anında kurulur; onay özeti bu kayıttan üretilir ve
/// POST YALNIZ bu kaydı taşır. Pencere açıkken canlı kur değişse de kullanıcının
/// onaylamadığı fiyat/tutar/kur kimliği sunucuya gitmez. Alan adları sunucu
/// <c>HurdaSatisDto</c>'suyla birebir (camelCase; ad BİLEREK farklı — P-03).
/// </summary>
public sealed record HurdaSatisIstegi(
    string Maden, decimal HasGrami, decimal BirimFiyatTlHasGram,
    decimal? AliciKabulHasGrami, string? TahlilFarkiGerekcesi, string? CariId,
    // ZO-ISLEM-A 29.09 (tasarım hakemi KRİTİK): Masak — sunucu HurdaSatisDto.Masak
    // (MasakBilgiDto) ile aynı ad; eşik altında null (sunucu bugünkü gibi davranır). Tip object:
    // istemcinin MasakFormu.MasakBilgi sınıfı Web projesindedir; çalışma anı tipiyle serileşir (Banka dekontu anonim gövdeyle aynı alanları gönderir).
    string KasaId, string OdemeTuru, decimal TahsilEdilenTl, string KurSnapshotId,
    object? Masak = null,
    // PAKET ISLEM-BANT: bant dışı has fiyatının SAHİP/MÜDÜR onayı (sunucu HurdaSatisDto.BantOnay/BantGerekce).
    bool? BantOnay = null, string? BantGerekce = null);

/// <summary>
/// D1-15 (18.10, rakip eşitleme) — hurda analizi: son N günde alınan / rafineriye gönderilen has ve TL,
/// ortalama has fiyatları, has başına fark ve tahlil farkı. Maden başına bir satır (defterde ayar tutulmaz).
/// Ortalama, işlem yoksa null (sıfır UYDURULMAZ).
/// </summary>
public sealed record HurdaAnalizMaden(
    string Maden, int AlisAdet, decimal AlisHas, decimal AlisTl, decimal? AlisOrtHasFiyati,
    int SatisAdet, decimal SatisHas, decimal SatisTl, decimal? SatisOrtHasFiyati,
    decimal? HasBasinaFark, decimal TahlilFarkiHas);

public sealed record HurdaAnalizi(int Gun, List<HurdaAnalizMaden> Madenler);
