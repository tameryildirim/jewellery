using Kuyumcu.Core.I18n;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET SATIS-2 — SATIŞ EKRANI = TUVAL + PUSULA SADELİĞİ (Islemler sayfasının parçası)
//
// Kurucu 25.09: "eski kayıtlar direkt açık gelmesin, piyano gibi açılır kapanır
// olsun" · "daha basit olamaz mı, Pusula'da böyle mi" (hayır).
// • Son satışlar Katlanir'dır: liste YALNIZ kart ilk açılınca yüklenir
//   (SonSatislariYukle sonSatisIstendi'yi kurar). Kayıttan sonra liste ancak
//   daha önce açılmışsa tazelenir — kapalı kart için istek ATILMAZ.
// • Diğer paneli (avans / değişim) aynı listeyi bağlamında kullanır: panel
//   açıldığı an liste bir kez yüklenir (özellik bozulmaz).
// • PRO ödeme özeti (FisProOdemeHapi) tıklanınca ödeme türü + kasa BAĞLI AÇILIR
//   içinde çizilir; karma ödemede ve yasal kapı açıkken satır içinde kalır.
// ⚠ Kaydet() / POST gövdeleri / yasal kapılar DEĞİŞMEDİ (yalnız görünüm).
// ⚠ Islemler.razor ≤ 1.400 satır (S21): yeni mantık bu dosyada.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    /// <summary>Son satışlar listesi en az bir kez istendi mi (Katlanir açıldı / Diğer paneli istedi).</summary>
    private bool sonSatisIstendi;
    private bool digerSonSatisYukleniyor;

    private SatisSadeMetinleri SS => SatisSadeSozlugu.Al(Dil.Dil);

    /// <summary>Kayıt/iade sonrası: liste daha önce açılmışsa tazelenir; kapalıysa istek atılmaz.</summary>
    private async Task SonSatislariTazele()
    {
        if (sonSatisIstendi) await SonSatislariYukle();
    }

    /// <summary>Diğer paneli (avans/değişim) listeyi bağlamında kullanır — açıldığında bir kez yükle.</summary>
    private async Task DigerIcinSonSatislar()
    {
        if (!DigerAcik || sonSatisIstendi || digerSonSatisYukleniyor) return;
        digerSonSatisYukleniyor = true;
        try { await SonSatislariYukle(); }
        finally { digerSonSatisYukleniyor = false; }
        StateHasChanged();
    }

    // ── PRO ödeme özeti → bağlı açılır (madde f) ─────────────────────────────
    private bool proOdemeAcik;

    /// <summary>
    /// PRO satışta ödeme türü + kasa satır içinde mi çizilir? Karma ödemede (bacak tablosu
    /// asıl iş) ve bir yasal kapı / alan hatası varken EVET — bilgi katmanın arkasına
    /// saklanmaz. Diğer durumda alt çubuktaki özetten açılan katmandadır.
    /// </summary>
    private bool ProOdemeSatirIci => karmaOdeme || YasalKapiAcik || !form.Gecerli;

    private void ProOdemeDegistir() => proOdemeAcik = !proOdemeAcik;

    /// <summary>(g+h) "+ Ayrıntı ekle" (reyon · açıklama · fiş iskontosu) dolu ya da reyon uyarısı varken açık gelir — girilmiş değer gizlenmez.</summary>
    private bool DigerAyrintiAcik => !string.IsNullOrEmpty(aciklama) || !string.IsNullOrEmpty(reyonId) || reyonHata is not null || fisIskontoYuzde is > 0;

    // ── KURUCU TEMEL KURALI (25.09 11:35) — PRO üst satır özeti ─────────────
    /// <summary>
    /// "Bugün · TL · Ertuğ U. · Perakende Varsayılan" — Tarih/Para/Satıcı/Fiyat listesi sistemin
    /// bildiği değerlerdir (UX-S21); tek satır özet olur, tıklanınca kutular açılır. Satıcı adı
    /// seçili personelden (yoksa oturumdan), liste adı sunucunun çözdüğü listeden gelir.
    /// </summary>
    private string ProUstOzet
    {
        get
        {
            var satici = saticilar?.Personeller?.FirstOrDefault(p => p.Id == (saticiId ?? saticilar.OturumId)) is { } ps
                ? KisaAd(ps.Ad, ps.Soyad)
                : KisaAd(Oturum.Kullanici?.Ad, Oturum.Kullanici?.Soyad);
            var (liste, _) = FiyatListesiCoz(fiyatListeleri, ProCari?.FiyatListesiId);
            return string.Join(" · ", new[] { SS.Bugun, ST.ParaTl, satici, liste ?? SP.FiyatListesiYok }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        }
    }

    /// <summary>"Ayşe Yılmaz" → "Ayşe Y." (tuvalin satıcı yazımı).</summary>
    public static string KisaAd(string? ad, string? soyad)
    {
        var a = (ad ?? "").Trim(); var s = (soyad ?? "").Trim();
        return s.Length == 0 ? a : $"{a} {char.ToUpper(s[0], System.Globalization.CultureInfo.GetCultureInfo("tr-TR"))}.".Trim();
    }

    /// <summary>Satıcı ya da fiyat listesi okunamadıysa özet açık gelir — hata gizlenmez (UX-S28).</summary>
    private bool ProUstOzetAcikZorunlu => saticiHata is not null || fiyatListesiHata is not null;
}
