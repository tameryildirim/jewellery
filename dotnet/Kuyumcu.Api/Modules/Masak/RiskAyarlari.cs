namespace Kuyumcu.Api.Modules.Masak;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 40-A — RİSK AYARLARI (ADR-0005: mevzuat/politika rakamı KODA GÖMÜLMEZ).
//
// ⚠ Etkin VARSAYILAN FALSE. Risk temelli yaklaşımın KUYUMCUYU BAĞLAYAN biçimi
//   (hangi skorda hangi tedbir zorunlu) DOĞRULANMADI. Kapalıyken skor
//   HESAPLANIR ve GÖSTERİLİR, hiçbir kapı kurmaz, hiçbir alanı zorunlu yapmaz.
//   Semantik TevsikEsikleri.Etkin ile BİREBİR aynıdır (TevsikKapisi.cs:55-60):
//   "karar yine hesaplanır ama işlem durdurulmaz" — ikinci bir 'kapalı'
//   anlamı doğmaz.
//
// ⚠ RİSK SKORU HİÇBİR KOŞULDA SATIŞ/MASAK/TEVSİK KAPISINA BAĞLANMAZ.
//   Mevzuatta doğrulanmış bir "yüksek riskliyle işlem yasaktır" hükmü YOKTUR;
//   koymak uydurma olurdu (Anayasa md.1). Skor uyarır ve ŞİB'e yönlendirir.
//
// ⚠ PUANLARIN KENDİSİ DE MEVZUAT DEĞİL POLİTİKADIR: varsayılanlar kuyumcunun
//   ayarlayacağı başlangıç değerleridir, "doğru" oldukları iddia EDİLMEZ.
//   Mali müşavir/uyum görevlisi teyidiyle kiracı bazında değiştirilir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class MasakRiskAyarlari
{
    /// <summary>
    /// Risk temelli tedbirlerin BAĞLAYICI hâle gelmesi. Varsayılan FALSE:
    /// kapalıyken skor hesaplanır ve gösterilir, akış DEĞİŞMEZ.
    /// </summary>
    public bool Etkin { get; init; }

    /// <summary>Yüksek riskte ek beyan zorunluluğu — DOĞRULANMADI, varsayılan kapalı.</summary>
    public bool YuksekRiskteBeyanZorunlu { get; init; }

    // ── Seviye bantları ──────────────────────────────────────────────────────
    /// <summary>Bu puan ve üstü ORTA.</summary>
    public int OrtaEsik { get; init; } = 40;
    /// <summary>Bu puan ve üstü YÜKSEK.</summary>
    public int YuksekEsik { get; init; } = 70;

    // ── Puan katsayıları ─────────────────────────────────────────────────────
    /// <summary>PEP beyanı varsa eklenen puan.</summary>
    public int PepPuan { get; init; } = 30;
    /// <summary>Yaptırım listesi eşleşmesi (FAZ B girdisi; Faz A'da her zaman 0 üretir).</summary>
    public int YaptirimPuan { get; init; } = 100;
    /// <summary>Nakit ağırlığı eşiği aşarsa eklenen puan.</summary>
    public int NakitAgirlikPuan { get; init; } = 15;
    /// <summary>Eşiğe yakın işlem (structuring sinyali) için eklenen puan.</summary>
    public int StructuringPuan { get; init; } = 25;
    /// <summary>Geçmişte şüpheli işlem bildirimi varsa eklenen puan.</summary>
    public int SupheliGecmisPuan { get; init; } = 20;

    /// <summary>
    /// Eşik üstü işlemi olup kimlik tespiti bulunmayan işlem sayısı için puan.
    /// ⚠ VARSAYILAN 0: bu kalemin ağırlığı mevzuatta doğrulanmadı; uydurulmuş
    ///   bir katsayı skoru sessizce bozardı. Kuyumcu açıkça değer verirse işler.
    /// </summary>
    public int KimlikEksikPuan { get; init; }

    /// <summary>
    /// <see cref="YuksekRiskliUlkeler"/> içindeki uyruk için eklenen puan.
    /// Liste BOŞ olduğu sürece hiç uygulanmaz.
    /// </summary>
    public int UlkeRiskPuan { get; init; } = 25;

    /// <summary>
    /// Yüksek riskli ülke uyrukları (ISO 3166-1 alpha-3). ⚠ VARSAYILAN BOŞ:
    /// listeyi biz uyduramayız; FATF/HMB listesini kuyumcunun uyum görevlisi
    /// girer. Boşken uyruk HİÇ puan üretmez.
    /// </summary>
    public string[] YuksekRiskliUlkeler { get; init; } = [];

    // ── Girdi toplama pencereleri ────────────────────────────────────────────
    /// <summary>Nakit oranı / structuring sayımının bakacağı gün sayısı.</summary>
    public int DegerlendirmePencereGunu { get; init; } = 90;

    /// <summary>Nakit oranı bu oranı AŞARSA nakit ağırlığı sinyali doğar (0–1).</summary>
    public decimal NakitAgirlikOrani { get; init; } = 0.70m;

    /// <summary>
    /// Structuring bandının ALT oranı: kimlik eşiğinin bu katı ile eşiğin
    /// kendisi arasında kalan işlemler "eşiğe yakın" sayılır.
    /// ⚠ EŞİĞİN KENDİSİ (MasakEsikleri.KimlikEsigi) BURADAN DEĞİŞTİRİLMEZ.
    /// </summary>
    public decimal BandAltOran { get; init; } = 0.80m;

    /// <summary>Bandın ÜST oranı (eşiğin kendisi = 1.00).</summary>
    public decimal BandUstOran { get; init; } = 1.00m;

    /// <summary>Kaç "eşiğe yakın" işlemden sonra structuring sinyali doğar.</summary>
    public int StructuringAsgariIslem { get; init; } = 2;

    // ── Profil ve gerekçe ────────────────────────────────────────────────────
    /// <summary>Bu kadar gündür hesaplanmamış profil "tazelenmeli" sayılır.</summary>
    public int ProfilTazelemeGunu { get; init; } = 30;

    /// <summary>Elle seviye ezmede gerekçenin asgari uzunluğu.</summary>
    public int ElleGerekceAsgari { get; init; } = 20;

    /// <summary>Yüksek riskli cari listesinin azami satır sayısı (UX-S29: ekranda YAZILIR).</summary>
    public int OzetListeSiniri { get; init; } = 50;
}
