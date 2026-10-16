using Kuyumcu.Web.Bilesenler.Form;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET SATIS-3 t17 — "EKRANDA GÖRÜNEN ≠ GÖNDERİLEN" HATA SINIFININ SAYFA KAPISI
// (orkestratör 29.09 12:10 + hasım doğrulayıcı KRİTİK ×2: takas yeni fiyat "1.500"
// iken teklif fiyatı POST ediliyordu; eski gram tuş tuş "1.500" iken ara değer 1,5 g
// POST ediliyordu).
// Sayfa TEK bir GecersizAlanKapisi'nı CascadingValue ile verir (Islemler.razor):
// sayfanın ve Bilesenler/Fis/* ile AltinBacagiPaneli'nin İÇİNDEKİ HER Sayi/Para/Gram
// kutusu çözülemeyen/belirsiz metinde kendini buraya yazar ve çağırana null bildirir.
//   • Gonderilebilir → kapıda kayıt varken FALSE (Kaydet pasif, F-kısayolu da).
//   • KaydetKilitSebebi → alanın ETİKETİYLE tam cümle (UX-S30).
//   • KalemEkle → giriş satırındaki adet/fiyat geçersizken çağırana null/0 gider, mevcut
//     "sıfırdan büyük" kuralı kalemi EKLEMEZ; kutu yine kendi uyarısını gösterir
//     (SayisalAlan.EtkinHata kapıda ayrıştırma hatasını öne alır).
//   • Diğer işlem formları: FisDigerEylem aynı kapıyı formun ön ekiyle okur.
// Kaydet() / POST gövdeleri DEĞİŞMEDİ (yalnız kapı). Islemler.razor ≤ 1.400 satır (S21).
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    /// <summary>Sayfanın geçersiz sayı kapısı — tüm Sayi/Para/Gram kutularına basamaklanır.</summary>
    private GecersizAlanKapisi? gecersizKapi;

    /// <remarks>Kayıt değişince sayfa yeniden çizilir: alanın çağıranı yalnız alt bileşen olsa da
    /// (FisParaUstu "Alınan") Kaydet kilidi ve sebebi aynı anda tazelenir.</remarks>
    private GecersizAlanKapisi gecersizAlanlar => gecersizKapi ??= new(Tazele);

    /// <summary>Sayfa kapanırken alanların Dispose bildirimi yeniden çizim istemez (yok edilmiş bileşen).</summary>
    private void Tazele()
    {
        try { _ = InvokeAsync(StateHasChanged); }
        catch (ObjectDisposedException) { }
    }

    /// <summary>
    /// Kapıda geçersiz alan varsa Kaydet'in kilit sebebi. Has gram fiyatı kutuları t16'daki cümleyi
    /// korur (H15/T15S/T16 testleri o metni sabitliyor); diğer her kutu kendi etiketiyle söylenir.
    /// </summary>
    private string? GecersizAlanSebebi
    {
        get
        {
            if (gecersizAlanlar.Ilk is not { } g) return null;
            var s3 = Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil);
            return g.Id is "takas-eski-fiyat" or "hurda-fiyat"
                ? s3.FiyatAlaniDuzeltin
                : string.Format(s3.AlanDuzeltin, g.Etiket);
        }
    }
}
