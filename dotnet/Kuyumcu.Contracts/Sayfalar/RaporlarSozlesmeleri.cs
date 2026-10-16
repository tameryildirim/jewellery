// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Raporlar.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.
using Kuyumcu.Core.Sozlesmeler;
using System.Globalization;

namespace Kuyumcu.Contracts.Sayfalar.Raporlar;
// ── veri modelleri (sayfaya özel; JSON camelCase otomatik eşleşir,
//    Web ayarı sayesinde string gelen sayılar da decimal'e okunur) ──
// Maden kırılımı sözleşmesi ORTAK: Kuyumcu.Core/Sozlesmeler/MadenSozlesmeleri.cs
// ⚠ PAKET 20 — ToplamHas artık YALNIZ ALTINDIR; diğer madenler MadenDagilimi
// altındadır. "600 g has" diye gösterilen 100 g altın + 500 g gümüş toplamı
// hiçbir şeyin karşılığı değildi (K20-3).
// ⚠ P-69 — ToplamHas ADI yanıltıcıdır (yalnız ALTIN); şekil sözleşmesi gereği
// kalmıştır. AltinToplamHas aynı değerin DOĞRU adıdır; varsayılanlıdır (null) ki
// alanı henüz döndürmeyen eski sunucuyla istemci KIRILMASIN.
public sealed record IslemToplam(int Sayisi, decimal ToplamTl, decimal ToplamHas,
                                  List<Kuyumcu.Core.Sozlesmeler.MadenDagilimSatiri>? MadenDagilimi = null,
                                  decimal? AltinToplamHas = null)
{
    /// <summary>Altın has gramı: yeni alan varsa o, yoksa (eski sunucu) ToplamHas.</summary>
    public decimal AltinHas => AltinToplamHas ?? ToplamHas;
}
// PAKET RAPOR-4 tur 2: günün NET satışı (iade düşülmüş — sunucu gunler[].netSatisTl); grafik çubuğu bunu çizer.
public sealed record AylikGun(string? Tarih, decimal SatisTl, decimal AlisTl, decimal? NetSatisTl = null);
// ⚠ S-2 / MÜHÜR 5: bu tip YENİ DEĞİL — sunucunun BUGÜNKÜ şekline
// (RaporModulu.Aylik) hizalandı. karTl artık null olabilir (hesaplanamadı).
public sealed record AylikYanit(
    int Yil, int Ay,
    IslemToplam? Satis, IslemToplam? Alis,
    IslemToplam? Tahsilat, IslemToplam? Veresiye,
    decimal? KarTl, string? KarKaynagi, decimal? SatisAlisFarkiTl,
    decimal? NetHasGram, decimal? NetHasGramHareketi,
    List<AylikGun>? Gunler,
    List<Kuyumcu.Core.Sozlesmeler.MadenDagilimSatiri>? MadenDagilimi = null, string? MadenUyarisi = null,
    // PAKET KAR-RAPOR: kâr durumu + maliyeti eksik satışlar (kâra katılmayan) — sunucudan.
    string? KarDurumu = null, int MaliyetiEksikSatisAdedi = 0, decimal MaliyetiEksikTutarTl = 0m,
    // MADEN-2: maliyeti eksik iade de bantta anılır ("2 satış ve 1 iade").
    int MaliyetiEksikIadeAdedi = 0,
    // PAKET RAPOR-2 tur 2: satış iadesi + net satış (Kâr-zarar "Bu ay" hasılatıyla aynı sayı).
    AylikIade? Iade = null, decimal? NetSatisTl = null,
    // PAKET RAPOR-4: alış-satış nakit farkının bileşeni (net satış − alış − hurda alışı).
    decimal? HurdaAlisTl = null);
public sealed record AylikIade(int Sayisi, decimal ToplamTl);
public sealed record YaslananCari(
    string CariId, string CariAd, string? CariTur,
    decimal TlBakiye, decimal HasGramBakiye, int? GunSayisi, string? Dilim,
    // Paket 27: döviz bakiyesi VADELENDİRİLMEZ ama SAKLANMAZ da (Anayasa md.1).
    bool DovizBakiyesiVar = false,
    // PAKET RAPOR-YASLANDIRMA: döviz bakiyeleri kod → tutar (sıfır olmayanlar; tek kaynak
    // sunucuda DovizBirimleri — yeni birim istemcide sabit liste olmadan görünür).
    Dictionary<string, decimal>? DovizBakiyeleri = null,
    // Dilim BELIRSIZ ise nedeni: "ACILIS" (son yazım açılış devri) · "TARIH_YOK".
    string? BelirsizNedeni = null,
    // Açılış devri hâlâ bakiyede (net ≠ 0) — sonradan işlem gördüyse tarihli dilimde rozetle.
    bool AcilisDahil = false);
// P-47: toplamSatir + dahaFazlasiVar sunucuda ZATEN dönüyordu (RaporModulu.CariYaslandirma);
// ekran alt toplamın yalnız listelenenleri kapsadığını söyleyebilsin diye okunur (UX-S29).
// PAKET RAPOR-YASLANDIRMA: ozetDoviz (kod → TÜM carilerin toplamı) sunucuda hesaplanır; ekran
// türetmez. (ozetTl istemciye BAĞLANMADI: eski sahte yanıtlar onu sayı olarak taşıyor —
// ekranın okumadığı alan sayfayı düşürmesin.)
public sealed record YaslandirmaYaniti(List<YaslananCari>? YaslananCariler,
    int ToplamSatir = 0, bool DahaFazlasiVar = false,
    Dictionary<string, decimal>? OzetDoviz = null);
// MADEN-AYRIMI: Maden (ayardan) + FizikselGram (adet × ürün gramı) — gümüş satırı "has" yazmaz, kendi biriminde.
public sealed record StokUrun(string UrunId, string? Kod, string Ad, string? AyarId, decimal Adet, decimal ToplamHas,
    string? Maden = null, decimal? FizikselGram = null);
// RAPOR-GORUNUM: madenDagilimi (sunucu toplamı) — altın dışı madenin toplam satırı istemcide TOPLANMAZ, buradan okunur.
public sealed record StokMadenToplami(string Maden, string? MadenAdi, decimal Adet, decimal ToplamGram, decimal? FizikselGram = null);
public sealed record StokYaniti(List<StokUrun>? Urunler, decimal ToplamHas, List<StokMadenToplami>? MadenDagilimi = null);

// ── G-5 (B-4 devri) — SATIŞ ÖDEME KIRILIMI ─────────────────────────────────
// Sunucu: GET /raporlar/odeme-araci → satisKirilimi (SatisOdemeKirilimi.Olustur).
// Bütün toplamlar SUNUCUDA hesaplanır; ekran yalnız çizer (UX-S21 — istemci
// toplam uydurmaz). Arac ham koddur; ekran sözlükten çevirir (UX-S31).
public sealed record SatisKirilimiSatiri(string Arac, decimal TutarTl, int FisAdet, bool KasayaGirer);
public sealed record SatisKirilimi(
    string? Kaynak, List<SatisKirilimiSatiri>? Satirlar,
    decimal BrutSatisTl, decimal KirilimToplamTl, decimal FarkTl,
    decimal KasayaGirenTl, decimal MahsupAltinTl, decimal VeresiyeTl,
    int FisAdet, int KalemsizFisAdet, decimal KalemsizTl);
public sealed record SatisOdemeKirilimiYaniti(string? Baslangic, string? Bitis, SatisKirilimi? SatisKirilimi);

// ── PAKET GOC-D — ortak ızgara satırları (razor içine record yazılmaz — PLAN § 1 md.9) ──

/// <summary>Reyon stok ızgarası: reyon satırı + atanmamış mal satırı TEK satır türü (atanmamış gizlenmez).</summary>
public sealed record ReyonStokIzgaraSatiri(string SubeId, string SubeAdi, string ReyonAdi, decimal Adet,
                                           List<YerlesimMadenSatiri> Madenler, bool Atanmamis);

/// <summary>Gün sonu özetinin bir kalemi (kod · ad · değer · satır sınıfı) — sıra sunucunun fark tanımının sırası.</summary>
public sealed record MutOzetSatiri(string Kod, string Ad, string Deger, string? Sinif);

/// <summary>Satış analizi ödeme aracı ızgarası: araç × para birimi satırı (UX-S32 — tutar para birimiyle taşınır).</summary>
public sealed record SatisAnaliziOdemeSatiri(string Kimlik, string Arac, string? ParaBirimi, decimal Giris, decimal Cikis, int Adet);
