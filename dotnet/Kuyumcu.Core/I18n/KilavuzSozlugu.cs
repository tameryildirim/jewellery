namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-33 (B-11 · B-5) — KILAVUZ EKRANI + YEDEK TATBİKAT KARTI METİNLERİ
// (TR + EN)
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs 9300 satırlık ORTAK HUB'dır ve aynı anda
//   başka şeritler de ona dokunuyor. EkranlarSozlugu / UstBarSozlugu deseni:
//   yeni metin ayrı, dil kodu ("tr" | "en") ile seçilir.
// ⚠ UX-S31: ekran ADLARI burada DEĞİLDİR — kılavuz ekranı adları
//   RotaBasliklari → Sozluk.Nav'dan okur (aynı ekran iki yerde iki ad taşımaz).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>/kilavuz ekranının P-33 ekleri.</summary>
public static class KilavuzSozlugu
{
    private static bool En(string dil) => dil == "en";

    /// <summary>Ekran ekran bağlantı listesinin başlığı.</summary>
    public static string EkranListesiBasligi(string dil) =>
        En(dil) ? "Screen by screen" : "Ekran ekran";

    /// <summary>
    /// Liste açıklaması. Sayı ELLE YAZILMAZ: RotaBasliklari'ndaki rota
    /// sayısıdır (yeni ekran eklenince kendiliğinden artar).
    /// </summary>
    public static string EkranListesiAciklama(string dil, int adet) => En(dil)
        ? $"All {adet} screens of the program have their own section in the guide. Each link opens the guide in a new tab at that screen."
        : $"Programdaki {adet} ekranın her birinin kılavuzda kendi bölümü vardır. Her bağlantı kılavuzu yeni sekmede o ekranın bölümünde açar.";

    /// <summary>Parametreli (kayıt ayrıntısı) rotaların adına eklenen not.</summary>
    public static string KayitAyrintisi(string dil) =>
        En(dil) ? "record detail" : "kayıt ayrıntısı";
}

/// <summary>/yedekler ekranındaki "son geri yükleme tatbikatı" kartı (BK-30 · Köprü K-5).</summary>
public static class YedekTatbikatSozlugu
{
    private static bool En(string dil) => dil == "en";

    /// <summary>Kart etiketi.</summary>
    public static string KartEtiketi(string dil) =>
        En(dil) ? "Last restore drill" : "Son geri yükleme tatbikatı";

    /// <summary>Hiç başarılı tatbikat yoksa kartın değeri.</summary>
    public static string HicYok(string dil) =>
        En(dil) ? "Never done" : "Hiç yapılmadı";

    /// <summary>Kart alt metni: kaç gün önce + azami süre (sayılar sunucudan gelir).</summary>
    public static string KartAlt(string dil, int? yasGun, int azamiGun) => yasGun is { } g
        ? (En(dil)
            ? $"{g} day(s) ago · must be repeated at least every {azamiGun} days"
            : $"{g} gün önce · en geç {azamiGun} günde bir tekrarlanmalı")
        : (En(dil)
            ? $"must be repeated at least every {azamiGun} days"
            : $"en geç {azamiGun} günde bir tekrarlanmalı");

    /// <summary>
    /// Bayat uyarısı ("ne oldu + ne yap"). Komut adı kılavuzla ve
    /// docs/YEDEK-GERI-YUKLEME.md ile aynıdır (tek yedek yolu).
    /// </summary>
    public static string BayatUyari(string dil, int azamiGun) => En(dil)
        ? $"The last successful restore drill is older than {azamiGun} days (or has never been done). A backup is only proven when it can be restored: ask your installer to run \"yedek-al-ve-dogrula.sh tatbikat\"."
        : $"Son başarılı geri yükleme tatbikatı {azamiGun} günden eski (ya da hiç yapılmadı). Yedek ancak geri yüklenebildiğinde kanıtlanır: kurulumu yapan kişiden \"yedek-al-ve-dogrula.sh tatbikat\" komutunu çalıştırmasını isteyin.";
}
