namespace Kuyumcu.Web.Bilesenler;

// ═════════════════════════════════════════════════════════════════════════════
// İKON ADLARI — paket UX-5 (brif mührü S6-i · Ç7).
//
// TEK KAYNAK: wwwroot/img/ikon.svg içindeki <symbol id="ikon-{ad}"> kümesi.
// Buradaki her sabit o dosyada bir sembole karşılık gelir; IkonTestleri iki
// listeyi karşılaştırır (sembol silinirse test kırmızıya döner).
//
// ⚠ NEDEN SABİT, NEDEN ÇIPLAK DİZGE DEĞİL (UX-S31 "eksik anahtar derleme
//   hatası verir"): <Ikon Ad="paenl" /> yazılırsa derleyici susar, ekranda
//   boş kare çıkar. <Ikon Ad="@Ikonlar.Panel" /> yazılırsa yanlış ad DERLEME
//   HATASIDIR. Bu yüzden çağıranlar daima bu sınıftan geçer.
//
// ⚠ ADLANDIRMA: sabitin adı İŞ anlamıdır (Panel, Cariler, Emanet), değeri ise
//   Lucide dosya adıdır (layout-dashboard, users, handshake). Böylece sembolü
//   kaynakta bulmak kolay kalır, ekrandaki anlam ise koddan okunur.
// ═════════════════════════════════════════════════════════════════════════════
public static class Ikonlar
{
    // ── Kenar çubuğu: marka + menü ──────────────────────────────────────────
    public const string Marka = "gem";
    public const string Panel = "layout-dashboard";
    public const string Cariler = "users";
    public const string Urunler = "package";
    public const string UrunAgaci = "folder-tree";
    public const string Islemler = "coins";
    public const string Emanet = "handshake";
    public const string Kur = "chart-line";
    public const string VadeTakibi = "alarm-clock";
    public const string Kasalar = "wallet";
    /// <summary>Paket 19 — vezne. Kasalar ile AYNI sembolü paylaşır (sprite büyütülmedi).</summary>
    public const string Vezne = "wallet";
    public const string Banka = "landmark";
    public const string Faturalar = "receipt";
    public const string Raporlar = "chart-column";
    public const string KarZarar = "trending-up";
    public const string Fiyatlandirma = "clipboard-list";
    public const string Sayim = "calculator";
    public const string Uyum = "shield-check";
    public const string GiderPusulalari = "file-text";
    public const string Etiketler = "tag";
    public const string Bildirimler = "bell";
    public const string Siparisler = "square-pen";
    public const string Kullanicilar = "key";
    public const string Isletme = "store";
    public const string Yetkiler = "shield";
    public const string DenetimIzi = "search";
    public const string VeriDisaAktarim = "upload";
    /// <summary>Paket 47 — veri içe aktarımı (gelen kutusu: dışarıdan gelen veri).</summary>
    public const string VeriIceAktarim = "inbox";
    public const string Kilavuz = "book-open";
    public const string Ayarlar = "settings";
    public const string Cikis = "log-out";

    // ── Üst bar ─────────────────────────────────────────────────────────────
    public const string Menu = "menu";
    public const string Saat = "clock";
    /// <summary>GIRDI-AILESI-2 — ortak Tarih bileşeninin takvim düğmesi.</summary>
    public const string Takvim = "calendar";
    // PAKET P-15 — kabuk araması / komut paleti. DenetimIzi ile AYNI çizim
    // ("search") bilinçli paylaşımdır: ikisi de "bul" eylemidir.
    public const string Arama = "search";
    public const string BasitMod = "circle";
    // Profesyonel mod = Ayarlar ("settings") — bilinçli paylaşım: anahtarın
    // iki ucu "sade" ve "ayarlı" karşıtlığını anlatır.

    // ── Ortak bileşenler ────────────────────────────────────────────────────
    public const string BosKutu = "inbox";
    public const string Kilit = "lock";
    public const string Uyari = "triangle-alert";
    public const string Kalem = "pencil";
    public const string Onay = "check";
    public const string Carpi = "x";

    // ── Tema seçici ─────────────────────────────────────────────────────────
    public const string TemaSistem = "monitor";
    public const string TemaAydinlik = "sun";
    public const string TemaKaranlik = "moon";

    // ── Kenar çubuğu favorisi (P-36b: P-37'nin ★/☆ emojisinin yerine) ───────
    /// <summary>
    /// Favori yıldızı. DOLU/BOŞ ayrımı İKİNCİ BİR SEMBOLLE DEĞİL, CSS ile
    /// yapılır: <c>.kenar-yildiz.dolu .ikon</c> kuralı <c>fill: currentColor</c>
    /// verir. Sprite büyütülmedi — Vezne=Kasalar paylaşımıyla aynı gerekçe.
    /// </summary>
    public const string Favori = "star";

    // ── PAKET S1-b · KATLANIR KENAR ÇUBUĞU (tuval Panel.dc.html <nav>) ──────
    /// <summary>"Tüm ekranlar" karo menüsü — tuvalde 2×2 kare ızgara.</summary>
    public const string TumEkranlar = "layout-grid";
    /// <summary>Açık grup başlığının oku (tuvalde "▾").</summary>
    public const string KatlamaAcik = "chevron-down";
    /// <summary>Kapalı grup başlığının oku (tuvalde "▸").</summary>
    public const string KatlamaKapali = "chevron-right";
}
