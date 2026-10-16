// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/SiparisDetay.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.
using System.Globalization;

namespace Kuyumcu.Contracts.Sayfalar.SiparisDetay;
public sealed record KalemSatiri(string Id, string? UrunId, string? UrunAd, string? Tanim,
    string? AyarId, decimal? GramAgirlik, decimal Adet, int Sira);
public sealed record GecisSatiri(string Id, string? OncekiDurum, string YeniDurum,
    string? Gerekce, string? KullaniciId, string? KullaniciAd, DateTime Olusturuldu);
public sealed record Kart(string Id, string No, string Tur, string Durum, string? CariId,
    string? CariAd, string? SubeId, DateOnly? SozVerilenGun, string? Aciklama,
    decimal? TahminiTutarTl, string? KurSnapshotId, string? EmanetHareketId,
    bool Geciken, bool Duzenlenebilir, bool Terminal, List<string>? IzinliGecisler,
    int GerekceAsgari, DateTime Olusturuldu, DateTime Guncellendi,
    List<KalemSatiri>? Kalemler, List<GecisSatiri>? Gecisler,
    // QA turu 5 (YÜKSEK): geri geçiş (ONAYLANDI → TASLAK) açıldı; hangi
    // hedefin gerekçe istediğini SUNUCU söyler, ekran tahmin etmez (UX-S21).
    List<string>? GerekceIsteyenGecisler = null,
    // PAKET P-22 / KARAR K-23: ATOLYEDE ve HAZIR'dan geri dönüş açıldı. Hangi
    // hedefin GERİ YÖNLÜ olduğunu ve kullanıcının yetkisi olup olmadığını
    // SUNUCU söyler (UX-S21); ekran kilitli düğmenin sebebini yazar (UX-S30).
    List<string>? GeriYonluGecisler = null,
    bool GeriAlmaYetkisi = false,
    List<string>? GeriAlmaRolleri = null,
    // PAKET EKSIK-B1-YAZDIR (B1-5): usta ve kapora kartı — sunucu Detay'dan (defterden okunur).
    string? UstaCariId = null,
    string? UstaAd = null,
    string? KaporaHareketId = null,
    bool KaporaAlinabilir = false,
    string? KaporaKilitSebebi = null,
    decimal? KaporaTutari = null,
    DateTime? KaporaTarihi = null,
    string? KaporaKasaId = null,
    bool KaporaStornolu = false,
    string? UstaMetalCikisiBeyani = null,
    // PAKET KAPORA-ESZAMAN (DÜŞÜK): geri verilip yeniden alınanlar dahil kapora geçmişi (defterden).
    List<KaporaGecmisSatiri>? KaporaGecmisi = null)
{
    /// <summary>Canlı (stornolanmamış) kapora var mı?</summary>
    public bool KaporaCanli => !string.IsNullOrWhiteSpace(KaporaHareketId) && !KaporaStornolu;

    public List<string> IzinliGecislerL => IzinliGecisler ?? [];
    public List<string> GerekceIsteyenGecislerL => GerekceIsteyenGecisler ?? [];
    public List<string> GeriYonluGecislerL => GeriYonluGecisler ?? [];
    public List<string> GeriAlmaRolleriL => GeriAlmaRolleri ?? [];
    public List<KalemSatiri> KalemlerL => Kalemler ?? [];
    public List<GecisSatiri> GecislerL => Gecisler ?? [];
    public List<KaporaGecmisSatiri> KaporaGecmisiL => KaporaGecmisi ?? [];
}

/// <summary>PAKET KAPORA-ESZAMAN: kapora geçmişi satırı (sunucu Detay.kaporaGecmisi).</summary>
public sealed record KaporaGecmisSatiri(string HareketId, DateTime Tarih, decimal Tutar, string? OdemeTuru,
    bool Stornolu, DateTime? StornoTarihi, bool Guncel);

// ── PAKET P-51 — başlık kimliği (cari · tür · şube) TASLAKTA düzenlenir ──────
// Düzenleme formunun seçici listeleri. Kural (PLAN § 1 md.9): istemci record'u
// razor içine yazılmaz. Alan kümesi Siparisler ekranındaki eşleniğiyle AYNIDIR
// (UX-S26 — oluşturma ve düzenleme formu tek dizi ailesinden beslenir).
public sealed record CariSecenegi(string Id, string Ad, string Tur);
public sealed record SubeSecenegi(string Id, string Ad);

/// <summary>PAKET EKSIK-B1-YAZDIR: kapora formunun kasa seçeneği (GET /kasalar).</summary>
public sealed record KaporaKasasi(string Id, string Ad, string ParaBirimi, bool Aktif, string? SubeId = null);
