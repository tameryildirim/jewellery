namespace Kuyumcu.Core.Sozlesmeler;

// ═════════════════════════════════════════════════════════════════════════════
// REYON YERLEŞİMİ İSTEMCİ SÖZLEŞMELERİ (paket 25 Faz A).
//
// ⚠ NEDEN PAGES İÇİNDE DEĞİL (S-2 / MÜHÜR 5 KOŞUL 3): Pages altına elle yazılan
//   yanıt modelleri dondurulmuştur — alan adı sunucudan saparsa hata ÇALIŞMA
//   ZAMANINDA çıkar (Stok modalı NRE, /uyum zarfı; QA turu 2). Yeni uçların
//   yanıt şekli bu ortak yerde, TEK kopya hâlinde durur; denetim.sh
//   "Pages içinde istemci record tanımı" sayacı bu kuralı zorlar.
//
// Alan adları sunucunun camelCase JSON'una birebir karşılık gelir:
//   Modules/Yerlesim/YerlesimServisi.cs  → ReyonIcerigi · UrunYerlesimi
//   Modules/Urun/KategoriModulu.cs       → ReyonServisi.Listele
//   Modules/Rapor/RaporModulu.cs         → ReyonStok
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>GET /reyonlar satırı (paket 25 ile `yerlesikAdet` eklendi).</summary>
public sealed record ReyonSatiri(
    string Id, string SubeId, string Ad, string Tip,
    string? SorumluKullaniciId, bool Aktif, int UrunSayisi, decimal YerlesikAdet);

/// <summary>GET /reyonlar/{id}/stok satırı.</summary>
public sealed record ReyonIcerikSatiri(
    string UrunId, string Kod, string Ad, string? Birim, string? AyarId,
    string? Maden, decimal Adet, decimal ToplamHasGram);

/// <summary>GET /reyonlar/{id}/stok zarfı (TB-19: limit + toplam + sınır uyarısı).</summary>
public sealed record ReyonIcerikYaniti(
    string ReyonId, string ReyonAdi, string Tip, string SubeId, bool Aktif,
    int ToplamSatir, int Limit, int Ofset, bool SinirdaMi,
    List<ReyonIcerikSatiri>? Satirlar)
{
    public List<ReyonIcerikSatiri> Liste => Satirlar ?? [];
}

/// <summary>GET /urunler/{id}/yerlesim — bir şubedeki tek reyon satırı.</summary>
public sealed record UrunYerlesimReyonu(
    string ReyonId, string ReyonAdi, string Tip, bool Aktif,
    decimal Adet, decimal ToplamHasGram);

/// <summary>
/// GET /urunler/{id}/yerlesim — şube bloğu. <c>Atanmamis</c> SAKLANMAZ,
/// sunucuda hesaplanır: şube stoğu − yerleşik toplam.
/// </summary>
public sealed record UrunYerlesimSubesi(
    string SubeId, string SubeAdi, decimal StokAdedi, decimal YerlesikAdet,
    decimal Atanmamis, List<UrunYerlesimReyonu>? Reyonlar)
{
    public List<UrunYerlesimReyonu> Liste => Reyonlar ?? [];
}

/// <summary>GET /urunler/{id}/yerlesim zarfı.</summary>
public sealed record UrunYerlesimYaniti(
    string UrunId, string Kod, string Ad, string? Birim, decimal HasGrami,
    decimal ToplamStok, List<UrunYerlesimSubesi>? Subeler)
{
    public List<UrunYerlesimSubesi> Liste => Subeler ?? [];
}

/// <summary>Maden kırılımı — farklı madenlerin gramları TOPLANMAZ.</summary>
public sealed record YerlesimMadenSatiri(
    string Maden, string MadenAdi, decimal Adet, decimal ToplamGram,
    // MADEN-2: tartı (fiziksel) gramı — yalnız reyon-stok doldurur; ekran "150,00 g (saf 138,75 g)" yazar.
    decimal? FizikselGram = null);

/// <summary>GET /raporlar/reyon-stok — reyon satırı.</summary>
public sealed record ReyonStokSatiri(
    string ReyonId, string ReyonAdi, string Tip, bool Aktif, string? SorumluKullaniciId,
    string SubeId, string SubeAdi, decimal Adet, int UrunSayisi,
    List<YerlesimMadenSatiri>? Madenler, decimal AltinHasGram)
{
    public List<YerlesimMadenSatiri> MadenListesi => Madenler ?? [];
}

/// <summary>GET /raporlar/reyon-stok — atanmamış mal satırı (şube bazında).</summary>
public sealed record ReyonStokAtanmamisSatiri(
    string SubeId, string SubeAdi, decimal Adet, List<YerlesimMadenSatiri>? Madenler)
{
    public List<YerlesimMadenSatiri> MadenListesi => Madenler ?? [];
}

/// <summary>GET /raporlar/reyon-stok zarfı.</summary>
public sealed record ReyonStokYaniti(
    List<ReyonStokSatiri>? Reyonlar, List<ReyonStokAtanmamisSatiri>? Atanmamis,
    decimal ToplamAltinHasGram, string? Uyari)
{
    public List<ReyonStokSatiri> ReyonListesi => Reyonlar ?? [];
    public List<ReyonStokAtanmamisSatiri> AtanmamisListesi => Atanmamis ?? [];
}

/// <summary>POST /reyonlar/yerlesim · /tasima · /geri-al yanıt zarfı.</summary>
public sealed record YerlesimSonucu(
    string Mesaj, string SubeId, string UrunId,
    decimal StokAdedi, decimal YerlesikAdet, decimal Atanmamis);

/// <summary>GET /subeler — rapor süzgeçlerinin ihtiyaç duyduğu asgari şube özeti.</summary>
public sealed record SubeOzeti(string Id, string Ad, bool Aktif);
