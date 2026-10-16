using System.Globalization;
using Kuyumcu.Contracts.Sayfalar.NakitSiniri;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-2 (TB-15) tur 3 — SATIŞ FİŞİ ONAYINDA "KALAN BORÇ YALNIZ KART/HAVALE"
//
// Orkestratör 30.09 22:20: veresiye (açık hesap) bacağı olan bir satış kaydedildiğinde,
// fiş toplamı nakit sınırını aşıyorsa başarı bandının altında TEK SATIR söylenir:
// "Bu fişin kalan borcu yalnız kartla ya da havaleyle alınabilir."
//
// ⚠ EŞİK SAYISI İSTEMCİDE YOK (MEVZUAT KURALI · ADR-0005): karar SUNUCUNUN
//   GET /tevsik/gunluk?cariId=&fisToplamTl= yanıtındaki `islemTutariKuraliDevrede`
//   (işlem tutarı kuralı açık VE fiş toplamı haddi aşıyor) ve `kuralEtkin` alanlarıdır.
//   Tahsilatta son sözü yine TevsikKapisi.TaksitDogrula söyler; bu satır yalnız bilgidir.
// ⚠ UX-S28: istek hatası "sınır yok"a çevrilmez — ayrı hata bandı çizilir.
// ⚠ Islemler.razor ≤ 1.400 satır (S21): mantık bu dosyada; razor'da yalnız çağrılar.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    /// <summary>Kaydedilen fişin kalan borcu için tek satır (sunucu söylediyse).</summary>
    private string? fisTaksitUyari;

    /// <summary>Tek satırın sorgusu düştüyse hata (UX-S28).</summary>
    private string? fisTaksitHata;

    /// <summary>
    /// Kayıt ANINDA (form sıfırlanmadan önce) sorgu yolu: satışta veresiye bacağı ve
    /// gerçek bir müşteri varsa <c>/tevsik/gunluk</c> sorgusu, yoksa null (istek atılmaz).
    /// </summary>
    private string? FisTaksitSorgusu()
    {
        if (tur != "SATIS" || string.IsNullOrEmpty(cariId)) return null;
        var veresiye = karmaOdeme
            ? bacaklar.Any(b => b.Arac == "VERESIYE" && b.TutarTl > 0m)
            : odemeTuru == "VERESIYE";
        if (!veresiye) return null;
        return $"/tevsik/gunluk?cariId={Uri.EscapeDataString(cariId)}"
             + $"&fisToplamTl={ToplamTl.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>Kayıttan SONRA sunucuya sorar; had aşılıyorsa tek satırı yazar.</summary>
    private async Task FisTaksitUyariYukle(string? sorgu)
    {
        if (sorgu is null) return;
        var m = TevsikTaksitSozlugu.Al(Dil.Dil);
        try
        {
            var y = await Api.GetAsync<TevsikGunlukVeri>(sorgu);
            if (y is { KuralEtkin: true, IslemTutariKuraliDevrede: true })
                fisTaksitUyari = m.FisKalanBorcBanka;
        }
        catch (ApiHata ex) { fisTaksitHata = string.Format(m.FisKalanBorcHata, ex.Message); }
    }
}
