namespace Kuyumcu.Core.Sozlesmeler;

// ═════════════════════════════════════════════════════════════════════════════
// VEZNE İSTEMCİ SÖZLEŞMELERİ (paket 19 Faz A).
//
// ⚠ NEDEN PAGES İÇİNDE DEĞİL (S-2 / MÜHÜR 5 KOŞUL 3): Pages altına elle yazılan
//   yanıt modelleri dondurulmuştur — alan adı sunucudan saparsa hata ÇALIŞMA
//   ZAMANINDA çıkar (Stok modalı NRE, /uyum zarfı; QA turu 2). Yeni uçların
//   yanıt şekli bu ortak yerde, TEK kopya hâlinde durur. denetim.sh
//   "Pages içinde istemci record tanımı" sayacı bu kuralı zorlar.
//
// Alan adları sunucunun camelCase JSON'una birebir karşılık gelir
// (Modules/Vezne/VezneModulu.cs — Kuyruk / Detay / VezneAyarGetir).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>GET /vezne/kuyruk satırı.</summary>
public sealed record VezneKuyrukSatiri(
    string Id, string Durum, string? FisTuru, bool Kalemli,
    decimal ToplamTl, string? CariId, string? CariAd, string? Aciklama,
    string? HazirlayanKullaniciId, string? HazirlayanAd, int KalemSayisi, string? HareketFisId,
    bool Duzenlenebilir, bool MutabakatGerekli,
    DateTime Olusturuldu, DateTime? Veznelendi, DateTime Guncellendi,
    // P-46: ACIK fişte toplamTl kalemlerden HESAPLANIR (tahmini); damga yalnız
    // vezneleme anında yazılır ve damgaliToplamTl'de ayrı taşınır.
    decimal DamgaliToplamTl = 0m, bool ToplamTahmini = false);

/// <summary>GET /vezne/kuyruk zarfı (TB-19: limit + toplam + "daha fazlası var").</summary>
public sealed record VezneKuyrukYaniti(
    string Durum, int Limit, int AzamiLimit, int Toplam, bool DahaFazlasiVar,
    bool VezneZorunlu, int GerekceAsgari, string? SubeId, List<VezneKuyrukSatiri>? Fisler)
{
    public List<VezneKuyrukSatiri> Satirlar => Fisler ?? [];
}

/// <summary>GET /vezne/fisler/{id} kalem satırı.</summary>
public sealed record VezneDetayKalemi(
    string Id, string UrunId, string? UrunAd, decimal Adet, decimal BirimFiyatTl,
    decimal IscilikTl, int Sira, decimal TutarTl);

/// <summary>GET /vezne/fisler/{id} tam kart.</summary>
public sealed record VezneDetayYaniti(
    string Id, string Durum, string? FisTuru, bool Kalemli, decimal? TutarTl,
    string? CariId, string? CariAd, string? Aciklama,
    string KurSnapshotId, decimal ToplamTl, string? HareketFisId,
    string? HazirlayanKullaniciId, string? HazirlayanAd, string? TahsilEdenAd,
    bool VezneZorunlu, int GerekceAsgari, bool Duzenlenebilir, bool Veznelenebilir,
    bool TahsilEdilebilir, bool IptalEdilebilir, bool Terminal, bool MutabakatGerekli,
    DateTime Guncellendi, List<VezneDetayKalemi>? Kalemler,
    bool ToplamTahmini = false, DateTime? Veznelendi = null)
{
    public List<VezneDetayKalemi> KalemListesi => Kalemler ?? [];
}

/// <summary>GET / PATCH /admin/vezne — vezne ayrımı bayrağı.</summary>
public sealed record VezneAyarYaniti(bool Zorunlu);

/// <summary>Vezne ekranının beslendiği yardımcı listeler (cari / ürün / kasa / kur).</summary>
public sealed record VezneCariSatiri(string Id, string Ad);

/// <remarks>PAKET GOC-A: <c>Birim</c> (ADET · GRAM) — miktar alanı ADET üründe tam sayı (K14), GRAM üründe 3 ondalıklı gram (K10).
/// Sunucu /urunler yanıtı alanı zaten taşıyor; eski istemci için isteğe bağlı (null = ADET kabul edilir, sunucu yine denetler).</remarks>
public sealed record VezneUrunSatiri(string Id, string Ad, string? Birim = null);

public sealed record VezneKasaSatiri(string Id, string Ad, string? ParaBirimi);

public sealed record VezneKurSatiri(string? Id);
