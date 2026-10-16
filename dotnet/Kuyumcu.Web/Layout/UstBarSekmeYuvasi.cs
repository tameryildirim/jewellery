using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Layout;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-K2 — ÜST BAR SAYFA SEKMELERİ YUVASI (tuval Ayarlar.dc.html)
//
// Tuvalde Ayarlar'ın sekme hapı (Görünüm · Dükkân · Kur · Yazıcı · Güvenlik)
// üst barda BAŞLIĞIN HEMEN YANINDADIR. Sayfa içeriğini <UstBarSekmeleri> ile
// buraya koyar; UstBar çizer. Yuva AnaYerlesim'de doğar ve CascadingValue ile
// hem UstBar'a hem sayfaya iner (statik durum YOK: iki kabuk birbirine karışmaz,
// bUnit'te yuva yoksa hiçbir şey çizilmez).
// ⚠ Neden Blazor Sections değil: SectionOutlet aynı ad için İKİNCİ aboneyi
//   reddeder (InvalidOperationException) — aynı test bağlamında iki UstBar
//   çizen mevcut testler çöküyordu (ölçüldü, T-K2 raporu).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Üst barın sayfa sekmeleri yuvası — tek sahip, son yazan kazanır.</summary>
public sealed class UstBarSekmeYuvasi
{
    private object? sahip;

    /// <summary>Çizilecek içerik; yoksa üst barda yuva yer kaplamaz.</summary>
    public RenderFragment? Icerik { get; private set; }

    /// <summary>İçerik değişti — UstBar yeniden çizer.</summary>
    public event Action? Degisti;

    /// <summary>Sayfa içeriğini yuvaya koyar.</summary>
    public void Ata(object kimden, RenderFragment? icerik)
    {
        sahip = kimden;
        Icerik = icerik;
        Degisti?.Invoke();
    }

    /// <summary>Sayfa kapanınca içeriği kaldırır — yalnız hâlâ sahibiyse (yeni sayfa önce yazmış olabilir).</summary>
    public void Birak(object kimden)
    {
        if (!ReferenceEquals(sahip, kimden)) return;
        sahip = null;
        Icerik = null;
        Degisti?.Invoke();
    }

    // ── PAKET DEGISIM-GUNSONU — SAYFANIN ÜST BAR BAŞLIĞI ─────────────────────
    // Hakem ORTA: /islemler'de Değişim seçiliyken üst bar başlığı rotadan "Satış" kalıyordu.
    // Sayfa, seçili ekranın adını buraya yazar (UstBarBasligi bileşeni); üst bar yalnız
    // AYNI rotadayken okur (başka sayfaya geçince eski başlık taşınmaz). Sekme yuvasından
    // (Icerik) AYRIDIR: başlık yazmak kur şeridini / mod anahtarını gizlemez (TOPLU-5'te
    // sekme yuvası denemesinin yan etkisi).
    private object? baslikSahibi;
    private string? baslikYolu;

    /// <summary>Sayfanın yazdığı başlık (null = rota başlığı).</summary>
    public string? Baslik { get; private set; }

    /// <summary>Başlığı yazar; <paramref name="yol"/> sayfanın mutlak yolu (yalnız o rotada geçerli).</summary>
    public void BaslikAta(object kimden, string? baslik, string yol)
    {
        var y = RotaBasliklari.Duzgunlestir(yol);
        if (ReferenceEquals(baslikSahibi, kimden) && Baslik == baslik && baslikYolu == y) return;
        baslikSahibi = kimden;
        Baslik = string.IsNullOrWhiteSpace(baslik) ? null : baslik;
        baslikYolu = y;
        Degisti?.Invoke();
    }

    /// <summary>Sayfa kapanınca başlığı kaldırır — yalnız hâlâ sahibiyse.</summary>
    public void BaslikBirak(object kimden)
    {
        if (!ReferenceEquals(baslikSahibi, kimden)) return;
        baslikSahibi = null;
        Baslik = null;
        baslikYolu = null;
        Degisti?.Invoke();
    }

    /// <summary>Bu yolda sayfanın yazdığı başlık; başka rotadaysak null.</summary>
    public string? BaslikIcin(string yol) =>
        Baslik is not null && baslikYolu == RotaBasliklari.Duzgunlestir(yol) ? Baslik : null;
}
