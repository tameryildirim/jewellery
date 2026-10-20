// PAKET B-2 — ALTIN POZİSYONU (panelin üçüncü kartı · sunucuda hesaplanır)
//
// Kural (P-03 · PLAN § 1 md.9): istemci ile sunucunun paylaştığı yanıt tipi
// burada TEK tanımla durur; sunucu imzasında AYNI tip kullanılır (derleyici
// kilidi — SozlesmeKapsamTestleri ölçütü (a)).
//
// ⚠ MADEN KURALI (K20-2 · P-45): farklı madenlerin gramları TOPLANMAZ. Bu
//   yüzden yanıtta "toplam has" diye TEK bir sayı YOKTUR: ana kart ALTIN'ı
//   taşır, diğer madenler kendi satırlarında ayrı gelir.

namespace Kuyumcu.Contracts.Sayfalar.Pozisyon;

/// <summary>Tek bir madenin pozisyonu — gramlar YALNIZ bu madenin (has) gramıdır.</summary>
/// <param name="Maden">Kanonik maden kodu (ALTIN, GUMUS…). Ekrana ham basılmaz; sözlükten çevrilir.</param>
/// <param name="VitrinGram">Kendi stoğumuzun has karşılığı (konsinye HARİÇ).</param>
/// <param name="HurdaGram">Elde duran hurdanın has karşılığı (HURDA_ALIS + HURDA_SATIS + TAHLIL_FARKI).</param>
/// <param name="CariAlacakGram">Carilerin bize has borcu toplamı (≥ 0; emanet HARİÇ).</param>
/// <param name="CariBorcGram">Bizim carilere has borcumuz toplamı (≥ 0, mutlak; emanet HARİÇ).</param>
/// <param name="NetGram">Vitrin + Hurda + Alacak − Borç.</param>
/// <param name="AlacakliCariSayisi">Bize has borçlu cari adedi.</param>
/// <param name="BorcluOlduguCariSayisi">Bizim has borçlu olduğumuz cari adedi.</param>
/// <param name="KonsinyeGram">Vitrinde duran ama BİZE AİT OLMAYAN konsinye malın has karşılığı — pozisyona GİRMEZ.</param>
/// <param name="EmanetGram">Elimizde duran müşteri emaneti — sahibine borçlu olduğumuz için pozisyona GİRMEZ (nötr).</param>
public sealed record MadenPozisyonu(
    string Maden,
    decimal VitrinGram,
    decimal HurdaGram,
    decimal CariAlacakGram,
    decimal CariBorcGram,
    decimal NetGram,
    int AlacakliCariSayisi,
    int BorcluOlduguCariSayisi,
    decimal KonsinyeGram,
    decimal EmanetGram);

/// <summary>GET /api/v1/pozisyon yanıtı.</summary>
/// <param name="AnaMaden">Kartın büyük sayısının madeni — daima ALTIN.</param>
/// <param name="Altin">ALTIN pozisyonu (hiç hareket yoksa sıfırlarla döner, null DEĞİL).</param>
/// <param name="DigerMadenler">ALTIN dışındaki, en az bir bileşeni sıfırdan farklı madenler.</param>
/// <param name="MadenlerToplanmaz">DigerMadenler doluysa true — ekran "toplanmaz" uyarısını basar.</param>
/// <param name="DefterBakiyeFarki">
/// Carilerin ALTIN has bakiye tablosu (cari_bakiyeler) ile defterden türetilen
/// ALTIN cari bakiyesi (emanet dahil) arasındaki fark. 0 değilse iki kaynak
/// ayrışmıştır; ekran bunu GİZLEMEZ, uyarı basar (Anayasa md.1).
/// </param>
/// <param name="HesaplanmaAni">Sunucunun hesapladığı an (UTC).</param>
/// <param name="DonemdenBagimsiz">Daima true — kart dönem seçicisinden etkilenmez (UX-S33).</param>
/// <param name="Kapsam">ŞUBE-KAPSAM-B tur 3: fiziki taraf (vitrin/konsinye/hurda) kapsamı — "SUBE" | "TUM_SUBELER".</param>
/// <param name="CariKapsam">Cari alacak/borç + emanet kapsamı — daima "KIRACI_GENELI" (müşteri hesapları şubeye bölünmez).</param>
public sealed record AltinPozisyonuYaniti(
    string AnaMaden,
    MadenPozisyonu Altin,
    List<MadenPozisyonu> DigerMadenler,
    bool MadenlerToplanmaz,
    decimal DefterBakiyeFarki,
    DateTime HesaplanmaAni,
    bool DonemdenBagimsiz,
    string Kapsam = "TUM_SUBELER",
    string CariKapsam = "KIRACI_GENELI",
    ZekatOzeti? Zekat = null,
    NetVarlikOzeti? NetVarlik = null);

/// <summary>
/// 20.10 — zekât hesaplayıcı (BİLGİ AMAÇLI; dinî/mali danışmana teyit). Nisap: 80,18 g has altın (Diyanet İşleri
/// Başkanlığı). Oran 1/40. Matrah: altın NET pozisyonu (vitrin + hurda + alacak − borç; emanet ve konsinye dışarıda).
/// </summary>
/// <summary>PAKET-37 (20.10) — bir para birimindeki nakit varlık: aktif kasalar + aktif banka hesapları.</summary>
/// <param name="Kur">TL karşılığı için kullanılan kur (TRY için 1); kiracının güncel kur kaydında yoksa null.</param>
/// <param name="TlKarsiligi">Toplam × Kur; kur yoksa null (uydurulmaz).</param>
public sealed record ParaBirimiVarligi(string ParaBirimi, decimal Kasa, decimal Banka, decimal Toplam, decimal? Kur, decimal? TlKarsiligi);

/// <summary>
/// PAKET-37 — TL NET VARLIK (servet) özeti: nakit (kasa+banka, para birimi bazında) + altın net pozisyonunun has değeri +
/// cari TL/döviz bakiyeleri. Her kalem sunucuda; bir girdi (kur, has fiyatı) yoksa o kalem ve toplam null döner ve
/// <paramref name="Eksikler"/> sebebini kodla söyler (KUR_USD, KUR_EUR, KUR_GBP, KUR_xxx, HAS_FIYATI). Bilgi amaçlıdır:
/// stok maliyeti değil has değeri; vergi/muhasebe öz sermayesi değildir.
/// </summary>
public sealed record NetVarlikOzeti(
    IReadOnlyList<ParaBirimiVarligi> ParaBirimleri,
    decimal? NakitTl,
    decimal AltinNetHasGram,
    decimal? AltinTl,
    decimal? CariTl,
    decimal? NetVarlikTl,
    decimal? HasFiyati,
    IReadOnlyList<string> Eksikler);

public sealed record ZekatOzeti(decimal NisapGram, decimal MatrahHasGram, bool NisapUstu, decimal ZekatHasGram, decimal? ZekatTl, decimal? HasFiyati);

/// <summary>Kırılım satırı — bir carinin tek madendeki has bakiyesi (+ bize borçlu, − biz borçluyuz).</summary>
public sealed record PozisyonCariSatiri(string CariId, string CariAd, decimal HasGram);

/// <summary>GET /api/v1/pozisyon/cariler yanıtı — sınırlı liste (UX-S29).</summary>
/// <param name="Yon">ALACAK (bize borçlu) | BORC (biz borçluyuz).</param>
public sealed record PozisyonCariSayfasi(
    string Maden,
    string Yon,
    List<PozisyonCariSatiri> Satirlar,
    int ToplamSatir,
    int Ofset,
    int Limit,
    bool DahaFazlasiVar);
