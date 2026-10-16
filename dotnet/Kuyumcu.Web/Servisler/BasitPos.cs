using System.Globalization;

namespace Kuyumcu.Web.Servisler;

/// <summary>
/// Basit modda POS ekranının (Pages/Islemler.razor) DARALTILMIŞ yüzünün TEK
/// karar kaynağı (paket UX-3 Faz B).
///
/// <para>
/// ⚠ Bu liste bir YETKİ DEĞİLDİR (K-UX-6). Sunucudaki hiçbir kapı arayüz moduna
/// bakmaz; burada gizlenen tür, adresi/ucu bilen yetkili kullanıcı için hâlâ
/// açıktır. Gizleme yalnız görünürlüktür.
/// </para>
/// <para>
/// Karar TEK YERDEDİR: razor dosyalarına dağılmış <c>@if (basit)</c> koşulları
/// yazılmaz (Bertan KOŞUL 7). Basit modda hangi işlem türünün görüneceği
/// değişecekse YALNIZ bu dizi değişir.
/// </para>
/// </summary>
public static class BasitPos
{
    /// <summary>
    /// Basit modda görünen işlem türleri — tezgâh fişinin DÜĞME IZGARASI.
    /// Kaynak: PLAN-SADELESTIRME § S-2 madde 2.2 (PAKET S2-2): "BasitPos.Turler
    /// 3 → 6 görünür işlem (SATIS, ALIS/HURDA_ALIS, TAHSILAT, ODEME, SATIS_IADE,
    /// TAKAS=değişim)". Bu sayfanın işlem türü olanlar burada; TAHSILAT / ODEME /
    /// EMANET bu sayfanın türü DEĞİLDİR → <see cref="Kisayollar"/>.
    /// ISCILIK / TRANSFER / ALIS_IADE profesyonel moddadır (özellik silinmedi).
    /// (Eski UX-3 listesi: SATIS · ALIS · SATIS_IADE.)
    /// PAKET T-S1 (tuval Tezgah.dc.html): ALIS (ürün kartlı alış) tuvalde KAROSU OLMADIĞI
    /// için PROFESYONEL moda taşındı (özellik silinmedi; kurucu koşulu "tuvalde olmayan
    /// özellik PRO'ya taşınır"). Tuvalin tek "Alış · eski altın" karosu HURDA_ALIS'tir.
    /// </summary>
    public static readonly string[] Turler = ["SATIS", "HURDA_ALIS", "TAKAS", "SATIS_IADE"];

    /// <summary>
    /// Izgaradaki sayfa DIŞI düğmeler (PAKET S2-2). Kod → hedef adres. Adres
    /// boşsa düğme KİLİTLİDİR ve sebebini yazar (DoD EK md.1: tıklanınca hiçbir
    /// şey olmayan düğme yok). ODEME: tedarikçiye ödeme ucu cari modülünde YOK
    /// (yalnız /cari/{id}/tahsilat var) → kilitli; ödeme alış fişinde seçilir.
    /// TAHSILAT: müşterinin borcu Müşteriler ekranında alınır; seçili gerçek
    /// müşteri varsa /cariler?cariId=… derin bağlantısı (DerinBaglanti, P-15).
    /// </summary>
    public static readonly (string Kod, string? Adres)[] Kisayollar =
    [
        ("TAHSILAT", "/cariler"),
        ("EMANET", "/emanet"),   // SATIS-3 t11: ("ODEME", null) kaldırıldı — ucu olmayan, hep pasif karo (hakem YÜKSEK); bkz. FisTurleri.ProSekmeler
    ];

    /// <summary>
    /// PAKET T-S1 — tuvalin (Tezgah.dc.html) "Ne yapıyorsun?" 2×4 düğme ızgarası,
    /// TUVAL SIRASIYLA: Satış · Alış·eski altın · Tahsilat · Ödeme · İade · Değişim ·
    /// Emanet · İndirim. Kod ya bu sayfanın işlem türüdür (<see cref="Turler"/>), ya
    /// sayfa dışı kısayoldur (<see cref="Kisayollar"/>), ya da <see cref="Indirim"/>
    /// (fiş iskontosu satırını açar — yeni tür DEĞİL, P-16 fiş iskontosu).
    /// </summary>
    public static readonly string[] Karolar =
        ["SATIS", "HURDA_ALIS", "TAHSILAT", "SATIS_IADE", "TAKAS", "EMANET", Indirim];   // SATIS-3 t11: ODEME karosu kaldırıldı (ucu yok, hep pasifti)

    /// <summary>İndirim karosunun kodu (işlem türü değildir, gövdeye gitmez).</summary>
    public const string Indirim = "INDIRIM";

    /// <summary>Verilen işlem türü basit modda görünür mü?</summary>
    public static bool TurGorunur(string tur) => Array.IndexOf(Turler, tur) >= 0;

    /// <summary>
    /// Basit modda POST gövdesinden ÇIKARILAN alanlar — ekranda çizilmeyen alan
    /// gövdeye HİÇ KONMAZ (null gönderilip veri ezilmez; Mehmet K7 · Aşkın K3).
    /// Satış/alış gövdesinde basit modda gizlenen tek alan vadedir: vade girdisi
    /// yalnız borç doğuran işlemde çizilir ve basit modda ödeme NAKİT'tir.
    /// </summary>
    public static readonly string[] GovdedenCikan = ["vadeGun"];

    /// <summary>
    /// PAKET T-S1 — tuvalin barkod kutusu "Barkodu okut ya da ürün adı yaz" der. Barkod
    /// sunucuda bulunamazsa (404) ürün, ekranda ZATEN yüklü aktif ürün listesinde ADIYLA
    /// aranır — yeni uç yok, istek yok. Adı birebir tutan tek ürün ya da adında metni
    /// geçen TEK ürün → o ürün. Birden çok eşleşme UYDURULMAZ: kimlik null döner ve kaç
    /// ürünün tuttuğu söylenir (ekran "adı biraz daha yazın" der). Karşılaştırma tr-TR,
    /// büyük/küçük harf duyarsız ("ALYANS" = "alyans"). 2 karakterden kısa metin aranmaz.
    /// </summary>
    public static (string? Id, int Sayi) AdlaBul(IEnumerable<(string Id, string Ad)> urunler, string metin)
    {
        var m = (metin ?? "").Trim();
        if (m.Length < 2) return (null, 0);
        var ci = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;
        var tutan = urunler.Where(u => ci.IndexOf(u.Ad ?? "", m, CompareOptions.IgnoreCase) >= 0).ToList();
        var birebir = tutan.Where(u => ci.Compare((u.Ad ?? "").Trim(), m, CompareOptions.IgnoreCase) == 0).ToList();
        if (birebir.Count == 1) return (birebir[0].Id, 1);
        return tutan.Count == 1 ? (tutan[0].Id, 1) : (null, tutan.Count);
    }
}
