// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Panel.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.
using System.Globalization;

namespace Kuyumcu.Contracts.Sayfalar.Panel;
// ── veri modelleri (sayfaya özel; JSON camelCase otomatik eşleşir,
//    Web ayarı sayesinde string gelen sayılar da decimal'e okunur) ──
// PAKET RAPOR-2 (03.10): sunucu /raporlar/gunluk alış ve hurdada `madenDagilimi`ni zaten döndürüyordu; istemci
// okumuyordu → "Bugün ne aldım?" gümüş alışı hiç göstermiyordu. İsteğe bağlı ve SONDA (eski yanıt/şekil bozulmaz).
public sealed record GunlukMadenPayi(string? Maden, string? MadenAdi, decimal ToplamHas);
public sealed record IslemOzet(int Adet, decimal ToplamTl, decimal ToplamHas, List<GunlukMadenPayi>? MadenDagilimi = null);
public sealed record HurdaOzet(decimal ToplamHas, decimal OdenenTl, List<GunlukMadenPayi>? MadenDagilimi = null);
public sealed record KasaOzet(string Ad, decimal Bakiye, string ParaBirimi);
public sealed record GunlukOzet(
    string? Tarih,
    IslemOzet? Satis,
    IslemOzet? Alis,
    HurdaOzet? Hurda,
    decimal NetHasGramHareketi,
    int HareketSayisi,
    List<KasaOzet>? Kasalar,
    // PAKET RAPOR-2 tur 2: Kâr-zarar hasılatıyla AYNI tanım (iade düşülmüş, storno dışlı).
    // İsteğe bağlı ve SONDA: alanı döndürmeyen eski yanıtta ekran brüt satışa düşer.
    NetSatisOzet? NetSatis = null);
/// <summary>PAKET RAPOR-2 tur 2 — /raporlar/gunluk `netSatis`: TL = brüt − iade; has YALNIZ ALTIN.</summary>
public sealed record NetSatisOzet(int SatisAdet, int IadeAdet, decimal BrutTl, decimal IadeTl,
    decimal ToplamTl, decimal BrutAltinHas, decimal IadeAltinHas, decimal AltinToplamHas);
// PAKET RAPOR-3: günün NET satışı (satış − iade) — Panel grafiği kartla aynı tanımı çizer; alan yoksa (eski yanıt) brüt.
public sealed record AylikGun(string? Tarih, decimal SatisTl, decimal? NetSatisTl = null);
public sealed record AylikYanit(List<AylikGun>? Gunler);
// ── kur kaynağı seçimi (tüm roller değiştirebilir) ──
public sealed record KaynakYaniti(string[]? Kaynaklar, string? Varsayilan, string? Secili,
    // KUR KAYNAKLARI (17.10) — İsteğe bağlı ve SONDA (eski yanıt/şekil bozulmaz): Ayarlar'ın üç seçimi.
    List<KaynakSecenegiVeri>? AltinSecenekleri = null,
    List<KaynakSecenegiVeri>? DovizSecenekleri = null, string? DovizSecili = null,
    List<KaynakSecenegiVeri>? OzelMatrahSecenekleri = null, string? OzelMatrahSecili = null);
/// <summary>KUR KAYNAKLARI (17.10) — bir kaynak seçeneği ve kısa durumu (Engel: ANAHTAR_YOK · SOZLESMESIZ_KAPALI).</summary>
public sealed record KaynakSecenegiVeri(string Kod, string? Engel, bool Sozlesmesiz, DateTime? SonCekim,
    DateOnly? SonKapanisGunu = null, decimal? SonKapanisFiyati = null);
public sealed record KaynakSonuc(string? Secili, string? Uyari);
// PAKET S3-1 — basit yüzün "Kasada ne var?" kartı: kasalar para birimine göre
// gruplanır; farklı para birimleri ASLA toplanmaz (UX-S32). Sira = ekrandaki
// sıra (TL önce) ve DOM id son eki.
public sealed record PanelKasaGrubu(int Sira, string ParaBirimi, decimal Toplam, List<KasaOzet> Kasalar);
// PAKET T-P1 — "Bugün ne sattım?" kartının kâr satırı: GET /raporlar/kar-zarar özetinden
// yalnız kâr ve maliyeti bilinmeyen adet okunur (kâr uydurulmaz; eksikse ekran söyler).
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.
// PAKET KAR-RAPOR: kâr maliyeti bilinmeyen satışta NULL gelir (hesaplanamadı) — decimal? zorunlu;
// karDurumu TAM/KISMI/HESAPLANAMADI/VERI_YOK, maliyeti eksik satış adedi + tutarı ayrı.
public sealed record PanelKarOzeti(decimal? KarTl, decimal MaliyetiBilinmeyenAdet,
    string? KarDurumu = null, int MaliyetiEksikSatisAdedi = 0, decimal MaliyetiEksikTutarTl = 0m,
    // MADEN-2: eksik yalnız iadeden geliyorsa da Panel kâr satırı bunu söyler.
    int MaliyetiEksikIadeAdedi = 0,
    // RAPOR-3: kâr satırının kapsamı ("yalnız N satış") — maliyeti bilinen satış = SatisAdedi − eksik.
    int SatisAdedi = 0);
