namespace Kuyumcu.Web.Layout;

/// <summary>
/// PAKET YILDIZ-3 — Yıldız'da büyük rakamın "kuruş ve ₺ soluk, yarı boy" yazımı
/// (tuval Bugun4: <c>113.011</c><c>,33 ₺</c>). Yalnız GÖRÜNÜM bölmesidir: metin
/// aynen korunur (tam + kuruş = özgün dizge), para/biçim davranışı değişmez.
/// Ondalık ayracı virgül + tam iki hane aranır; bulunamazsa (ör. "—", "0 ₺")
/// <c>null</c> döner ve çağıran özgün dizgeyi basar.
/// </summary>
public static class YildizSayi
{
    /// <summary>"98.112,96 ₺" → ("98.112", ",96 ₺"); eşleşme yoksa null.</summary>
    public static (string Tam, string Kurus)? Bol(string? metin)
    {
        if (string.IsNullOrEmpty(metin)) return null;
        var i = metin.LastIndexOf(',');
        if (i <= 0 || i + 2 >= metin.Length) return null;
        if (!char.IsDigit(metin[i + 1]) || !char.IsDigit(metin[i + 2])) return null;
        if (!char.IsDigit(metin[i - 1])) return null;
        return (metin[..i], metin[i..]);
    }
}
