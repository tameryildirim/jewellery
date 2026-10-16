// PAKET P-15 (kimlikler Y-29, Y-5, B-25, B-29) — KABUK ARAMA SÖZLEŞMESİ.
//
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya
// yazılır; ad VE tip sunucuyla aynıdır. Bu dosyayı HEM Kuyumcu.Api (yanıtı
// üretir) HEM Kuyumcu.Web (KayitSecici / GlobalArama tüketir) kullanır —
// yani sözleşme tek yerde yaşar, iki tarafta ayrışamaz.
//
// ⚠ ULID ekrana BASILMAZ: <see cref="AramaSonucu.Kimlik"/> yalnız gövdeye
//   konur, kullanıcı <see cref="AramaSonucu.Ad"/> + <see cref="AramaSonucu.Tur"/>
//   görür (B-25 — "26 haneli ULID yerine ad + tip").

namespace Kuyumcu.Contracts.Arama;

/// <summary>
/// Aranabilir kayıt türleri — TEK KAYNAK (UX-S31). Sunucu bu dizgeleri kabul
/// eder, istemci aynı sabitleri kullanır; üçüncü bir yerde tekrar yazılmaz.
/// </summary>
public static class AramaTurleri
{
    public const string Cari = "CARI";
    public const string Urun = "URUN";
    public const string Fis = "FIS";
    public const string Fatura = "FATURA";
    public const string BankaHareket = "BANKA_HAREKET";
    /// <summary>Defter satırı (hareketler tablosu) — düzeltme fişinin dayanağı.</summary>
    public const string Hareket = "HAREKET";

    /// <summary>Genel aramada (komut paleti) taranan türler, gösterim sırasıyla.</summary>
    public static readonly string[] Genel = [Cari, Urun, Fis, Fatura];

    /// <summary>Tümü — seçici bileşeni bu listeden doğrular.</summary>
    public static readonly string[] Tumu = [Cari, Urun, Fis, Fatura, BankaHareket, Hareket];

    /// <summary>
    /// P-38 · B-25 — GÖZATILABİLİR TÜRLER. QA turu 7 #159: "Fişten Fatura Kes"
    /// fiş araması 200 dönüyor ama HİÇBİR sonuç vermiyordu — çünkü fişin
    /// kullanıcının bildiği bir adı yok (ULID) ve arama en az 2 karakter
    /// istiyordu. Bu türlerde sorgu kısa/boşken sunucu EN SON kayıtları
    /// listeler: seçici boş kalmaz, kullanıcı ULID yazmak zorunda kalmaz.
    /// </summary>
    public static readonly string[] Gozatilabilir = [Fis];

    /// <summary>Tür gözatılabilir mi (sorgusuz son kayıtlar listelenir mi)?</summary>
    public static bool Gozatilir(string tur) => Array.IndexOf(Gozatilabilir, tur) >= 0;

    /// <summary>Tür → yetki modülü anahtarı. Bilinmeyen tür = yetki YOK (fail-closed).</summary>
    public static string? Modul(string tur) => tur switch
    {
        Cari => "cari",
        Urun => "urun",
        Fis => "islem",
        Fatura => "fatura",
        BankaHareket => "banka",
        Hareket => "islem",
        _ => null,
    };
}

/// <summary>
/// Tek arama sonucu.
/// </summary>
/// <param name="Kimlik">Kayıt kimliği (ULID). Gövdeye konur, EKRANA BASILMAZ.</param>
/// <param name="Ad">Kullanıcının tanıdığı ad (cari adı, ürün adı, fiş özeti).</param>
/// <param name="AltBilgi">Ayırt edici ikinci satır (telefon, kod, tarih+tutar).</param>
/// <param name="Tur"><see cref="AramaTurleri"/> değerlerinden biri.</param>
/// <param name="Rota">Kaydın kendi ekranına giden derin bağlantı; yoksa null.</param>
public sealed record AramaSonucu(
    string Kimlik,
    string Ad,
    string? AltBilgi,
    string Tur,
    string? Rota);

/// <summary>
/// Arama yanıtı — sunucuda arar ve SAYFALAR (Y-5).
/// </summary>
/// <param name="Sorgu">Sunucunun gerçekten uyguladığı (kırpılmış) sorgu.</param>
/// <param name="Tur">Tek tür arandıysa o tür; genel aramada "GENEL".</param>
/// <param name="Limit">Bu sayfada istenen en çok kayıt.</param>
/// <param name="Ofset">Atlanan kayıt sayısı.</param>
/// <param name="Sayi"><see cref="Sonuclar"/> uzunluğu.</param>
/// <param name="DahaVar">
/// Sunucuda bu sayfadan sonra kayıt kaldı mı (UX-S29 — sessiz kırpma yok).
/// </param>
/// <param name="Tavan">Sunucunun izin verdiği en büyük limit; ekran bunu yazar.</param>
/// <param name="Gozatma">
/// P-38 · B-25 — sonuçlar bir SORGUNUN değil, "en son kayıtlar" gözatmasının
/// sonucu mu? Ekran bunu yazar ki kullanıcı listenin neyi gösterdiğini bilsin.
/// </param>
public sealed record AramaYaniti(
    string Sorgu,
    string Tur,
    int Limit,
    int Ofset,
    int Sayi,
    bool DahaVar,
    int Tavan,
    IReadOnlyList<AramaSonucu> Sonuclar,
    bool Gozatma = false);
