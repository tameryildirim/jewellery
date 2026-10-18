using System.Text;
using Kuyumcu.Contracts.Fiyat;
using Kuyumcu.Contracts.Sayfalar.UrunEtiketleri;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Core.Domain;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.JSInterop;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// D1-02 (18.10, rakip eşitleme: "etiket tasarımcısı · QR · anlık fiyat · Zebra ZPL").
//   • Şablon sunucuda (GET/PUT /urunler/etiket-sablonu): hazır boy + alanların sırası/görünürlüğü (sürükle-bırak
//     ya da ↑↓). Taş kökeni ibaresi çıkarılamaz. Ekranda basılan kâğıt ile ZPL AYNI veriden (EtiketVerisi).
//   • Fiyat alanı açıksa fiyat BASIM ANINDA güncel kurla sunucudan alınır (GET /fiyat/teklifler, 25'erli) ve
//     etikete basım saatiyle yazılır — kur değişince eskiyeceği ekranda söylenir.
// ═════════════════════════════════════════════════════════════════════════════
public partial class UrunEtiketleri
{
    private EtiketSablonu sablon = EtiketSablonu.Varsayilan;
    private bool sablonCekmeceAcik;
    private readonly Dictionary<string, decimal> fiyatlar = [];
    private DateTime? fiyatZamani;
    private string? fiyatHata, sablonHata;

    private D1Metinleri D1 => D1Sozlugu.Al(Dil.Dil);
    private bool SablonYazabilir => Oturum.Kullanici?.Rol is "SAHIP" or "MUDUR";
    private string EtiketBoyuSinifi => $"urun-etiket--{sablon.GenislikMm}x{sablon.YukseklikMm}";

    private async Task SablonYukle()
    {
        try { sablon = await Api.GetAsync<EtiketSablonu>("/urunler/etiket-sablonu") ?? EtiketSablonu.Varsayilan; }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException) { sablonHata = ex.Message; }   // şablon yardımcı veridir: varsayılanla basılır, sebep şablon satırında
    }

    private void SablonKaydedildi(EtiketSablonu s) { sablon = s; sablonCekmeceAcik = false; }

    /// <summary>Fiyat alanı açıksa seçili ürünlerin güncel fiyatı (basım anı). Okunamazsa basım DURUR (eski fiyat basılmaz).</summary>
    private async Task<bool> FiyatlariHazirla()
    {
        fiyatHata = null; fiyatlar.Clear();
        if (!sablon.Alanlar.Contains(EtiketSablonu.Fiyat)) return true;
        try
        {
            var kur = await Api.GetAsync<Kuyumcu.Contracts.Sayfalar.Kur.KurVeri>("/kur/guncel");
            var idler = secili.ToList();
            for (var i = 0; i < idler.Count; i += 25)
            {
                var parca = string.Join(',', idler.Skip(i).Take(25));
                var y = await Api.GetAsync<TopluTeklifYaniti>($"/fiyat/teklifler?urunIds={Uri.EscapeDataString(parca)}&kurSnapshotId={Uri.EscapeDataString(kur.Id)}");
                foreach (var s in y.Satirlar)
                    if (s.TeklifVar && s.SatisFiyati is { } f) fiyatlar[s.UrunId] = f;
            }
            fiyatZamani = DateTime.UtcNow;
            return true;
        }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException) { fiyatHata = ex.Message; return false; }
    }

    private string? FiyatMetni(UrunVeri u) =>
        fiyatlar.TryGetValue(u.Id, out var f) && fiyatZamani is { } z
            ? $"{Bicim.TlKurus(f)} · {Bicim.TarihSaat(z)}"
            : null;

    private EtiketVerisi Veri(UrunVeri u) => new(
        u.Ad,
        Ibare(u).Length > 0 ? TurkceMetin.Buyut(Ibare(u)) : null,
        MadenIbaresi(u),
        $"{KurKaynakAdi.AyarAdi(u.AyarId)} · {Bicim.Gram(u.GramAgirlik)} · {Bicim.Has(u.HasGrami)}",
        string.IsNullOrEmpty(u.Barkod) ? null : u.Barkod,
        u.Kod,
        FiyatMetni(u));

    private static string QrSvg(UrunVeri u) => QrKod.Svg(string.IsNullOrEmpty(u.Barkod) ? u.Kod : u.Barkod);

    /// <summary>Zebra yazıcı dosyası: seçili ürünler × kopya, şablon sırasıyla (ZPL II, 203 dpi).</summary>
    private async Task ZplIndir()
    {
        if (secili.Count == 0 || kopya < 1) return;
        if (!await FiyatlariHazirla()) return;
        var etiketler = UrunListesi.Where(x => secili.Contains(x.Id)).SelectMany(u => Enumerable.Repeat(Veri(u), kopya));
        var zpl = EtiketZpl.Uret(sablon, etiketler);
        await JS.InvokeVoidAsync("kuyumcuIndir", "etiketler.zpl", Convert.ToBase64String(Encoding.UTF8.GetBytes(zpl)), "text/plain");
    }

    private string SablonOzeti => string.Join(" · ", sablon.Alanlar.Select(a => Kuyumcu.Web.Bilesenler.EtiketSablonuCekmecesi.AlanAdi(a, D1)));
}
