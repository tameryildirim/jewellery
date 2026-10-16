// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/KarZarar.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.

namespace Kuyumcu.Contracts.Sayfalar.KarZarar;
public sealed record UrunSatiri(
    string UrunId, string? Kod, string? Ad, int SatisAdedi,
    // KAR-RAPOR-2: MaliyetTl kâr HESAPLANAMADI iken null (bilinmiyor — "0 ₺" basılmaz).
    decimal HasilatTl, decimal? MaliyetTl, decimal? KarTl,
    decimal? KarMarjiYuzde, decimal MaliyetiBilinmeyenAdet,
    decimal IadeTl = 0m, decimal? KarHas = 0m, string? Guvenilirlik = null,
    // PAKET KAR-RAPOR: kâr maliyeti bilinmeyen satışta NULL (hesaplanamadı); eksik tutar ayrı.
    string? KarDurumu = null, int MaliyetiEksikSatisAdedi = 0, decimal MaliyetiEksikTutarTl = 0m,
    // KAR-RAPOR-2: maliyeti bilinen satışların hasılatı (bilinenHasilatTl − maliyetTl = karTl).
    decimal? BilinenHasilatTl = null,
    // MADEN-2 tur 2: has bacağının satır durumu (TAM/KISMI/HESAPLANAMADI; altın dışı ürün null) ve
    // has maliyeti bilinmediği için has kârına katılmayan satış adedi — sunucu zaten döndürüyordu.
    string? HasKarDurumu = null, int HasMaliyetiEksikSatisAdedi = 0,
    // RAPOR-GORUNUM tur 2: satırın madeni (sunucu zaten `maden` döndürüyordu) — altın dışı satırın
    // "—" has kârı gerekçesiz kalmasın ("Has yalnız altında hesaplanır").
    string? Maden = null,
    // PAKET RAPOR-4 (VERİ 1): maliyeti partide olmayıp ürün kartındaki alış maliyetinden alınan adet.
    decimal KartMaliyetliAdet = 0m);
// PAKET OLCEK-YUK t2 (UX-S29): sunucu sınırı ekrana taşınır — toplam ürün sayısı ve "daha fazlası var" sunucudan.
public sealed record UrunYanit(List<UrunSatiri> Satirlar, bool DahaFazlasiVar = false, int ToplamUrunSayisi = 0, int Limit = 0, int Ofset = 0);
// MADEN-AYRIMI: ToplamHasGram YALNIZ ALTIN hası; diğer madenler MadenKirilimi'nde (saf + fiziksel gram).
//   StokMaliyetiTl/Has hiçbir adedin maliyeti bilinmiyorsa null ("0 ₺" basılmaz); kısmen biliniyorsa Kismi = true.
public sealed record StokVeri(
    decimal ToplamAdet, decimal ToplamHasGram, decimal MaliyetliAdet,
    decimal MaliyetsizAdet, decimal? StokMaliyetiTl, decimal? StokMaliyetiHas,
    int KonsinyeUrunSayisi, string? Uyari, string? Guvenilirlik = null,
    List<StokMadenSatiri>? MadenKirilimi = null, bool StokMaliyetiKismi = false,
    // PAKET RAPOR-4 tur 2: maliyetli adedin ürün kartı maliyetiyle bilinen kısmı (kâr hesabıyla aynı kural).
    decimal KartMaliyetliAdet = 0m);
/// <summary>MADEN-AYRIMI: stokta bir madenin saf gramı (altında = has) ve tartı gramı (bilinmiyorsa null).</summary>
public sealed record StokMadenSatiri(string Maden, string MadenAdi, decimal Adet, decimal ToplamGram, decimal? FizikselGram);
