namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// M4 (16.10) — Ürün kuralları çekmecesinin metinleri (TR + EN).
// Müşteri: "Ayarlar bölümünde bileziği ben tanımlarım… içinde bilezik geçiyorsa binde bir…
// bilezik ise şu kadar, kolyeyse bu kadar, tek tek yapma".
// ═════════════════════════════════════════════════════════════════════════════

public static class UrunKuraliSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Ac(string dil) => En(dil) ? "Pricing rules" : "Fiyat kuralları";
    public static string Baslik(string dil) => En(dil) ? "Pricing rules" : "Fiyat kuralları";
    public static string YeniKural(string dil) => En(dil) ? "New rule" : "Yeni kural";
    public static string KuralYok(string dil) => En(dil) ? "No rules yet." : "Henüz kural yok.";
    public static string Ad(string dil) => En(dil) ? "Rule name" : "Kural adı";
    public static string AdIcerir(string dil) => En(dil) ? "Name contains" : "Adında geçen";
    public static string AdIcerirIpucu(string dil) => En(dil) ? "E.g. bracelet — matches any item whose name contains it" : "Örn. bilezik — adında bu geçen ürünlere uyar";
    public static string Kategori(string dil) => En(dil) ? "Category" : "Kategori";
    public static string Sira(string dil) => En(dil) ? "Order" : "Sıra";
    public static string SiraIpucu(string dil) => En(dil) ? "Lower runs first when several rules match" : "Birden çok kural uyarsa küçük olan önce";
    public static string Aktif(string dil) => En(dil) ? "Active" : "Etkin";
    public static string Duzenle(string dil) => En(dil) ? "Edit" : "Düzenle";
    public static string Onizle(string dil) => En(dil) ? "Preview" : "Önizle";
    public static string Uygula(string dil) => En(dil) ? "Apply to items" : "Ürünlere uygula";
    public static string UstuneYaz(string dil) => En(dil) ? "Overwrite values already entered" : "Girilmiş değerlerin üzerine yaz";
    public static string Uyan(int uyan, int degisecek, string dil) => En(dil)
        ? $"{uyan} items match · {degisecek} will change"
        : $"{uyan} ürün uyuyor · {degisecek} ürün değişecek";
    public static string Uygulandi(int n, string dil) => En(dil) ? $"Applied to {n} items." : $"{n} ürüne uygulandı.";
    public static string Kaydedildi(string dil) => En(dil) ? "Rule saved." : "Kural kaydedildi.";
    public static string Ozet(string? adIcerir, string? kategori, string dil)
    {
        var parca = new List<string>();
        if (!string.IsNullOrWhiteSpace(kategori)) parca.Add(kategori);
        if (!string.IsNullOrWhiteSpace(adIcerir)) parca.Add(En(dil) ? $"name contains “{adIcerir}”" : $"adında “{adIcerir}”");
        return string.Join(" · ", parca);
    }
    public static string Degerler(decimal? alis, decimal? satis, decimal? binde, decimal? kdv, string dil)
    {
        var p = new List<string>();
        if (alis is { } a) p.Add((En(dil) ? "cost " : "alış ") + a.ToString("0.##"));
        if (satis is { } s) p.Add((En(dil) ? "sale " : "satış ") + s.ToString("0.##"));
        if (binde is { } b) p.Add((En(dil) ? "VAT share ‰" : "KDV payı ‰") + b.ToString("0.###"));
        if (kdv is { } k) p.Add((En(dil) ? "VAT %" : "KDV %") + k.ToString("0.##"));
        return string.Join(" · ", p);
    }
}
