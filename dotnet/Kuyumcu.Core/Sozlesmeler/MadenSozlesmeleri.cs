namespace Kuyumcu.Core.Sozlesmeler;

// ═════════════════════════════════════════════════════════════════════════════
// MADEN İSTEMCİ SÖZLEŞMELERİ (paket 20 Faz A).
//
// ⚠ NEDEN PAGES İÇİNDE DEĞİL (S-2 / MÜHÜR 5 KOŞUL 3): Pages altına elle yazılan
//   yanıt modelleri dondurulmuştur; alan adı sunucudan saparsa hata ÇALIŞMA
//   ZAMANINDA çıkar. Bu paketin üç ekranı (ürün · emanet · raporlar) aynı
//   maden kırılımını okur — tek kopya burada durur.
//
// Alan adları sunucunun camelCase JSON'una birebir karşılık gelir:
//   Modules/Rapor/RaporModulu.cs  → MadenDagilimi(...)
//   Modules/Emanet/EmanetModulu.cs → CariEmanetListesi().madenBakiyeleri
//   Modules/Urun/UrunModulu.cs     → GET /urunler/madenler
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Dönem/rapor maden kırılımı satırı.</summary>
public sealed record MadenDagilimSatiri(
    string Maden, string MadenAdi, decimal ToplamHas, int SatirSayisi);

/// <summary>Cari bazında maden emaneti (K20-9).</summary>
public sealed record MadenBakiyeSatiri(string Maden, string MadenAdi, decimal Gram);

/// <summary>
/// Etiket ibare politikası — rodyum ibaresinin ZORUNLU olduğu DOĞRULANMADI,
/// bu yüzden yapılandırmadan gelir ve varsayılanı KAPALIDIR (ADR-0005).
/// </summary>
public sealed record EtiketIbarePolitikasi(bool RodyumIbaresiGoster, string RodyumIbaresi);

/// <summary>GET /urunler/madenler zarfı.</summary>
public sealed record MadenTablosuYaniti(EtiketIbarePolitikasi? Etiket);
