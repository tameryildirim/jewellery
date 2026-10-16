using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kuyumcu.Api.Modules.Tevsik;

/// <summary>
/// Tevsik haddinin SALT OKUNUR önizlemesi.
///
/// ⚠ NEDEN VAR: bugüne kadar tezgâhtar 30.000 TL haddini ancak Kaydet'e
/// bastıktan sonra, bir hata mesajı olarak öğreniyordu — müşteri karşısında.
/// Bu uç, fiş kapanmadan "bu müşteriye bugün ne kadar nakit kaldı"yı söyler.
///
/// Kapı DEĞİLDİR ve kapının yerine geçmez: son sözü POST yolundaki
/// <see cref="TevsikKapisi"/> söyler. <c>GET /masak/esikler</c> ucunun birebir
/// eşdeğeridir ve aynı gerekçeyle vardır (ADR-0005: eşik sunucudan gelir,
/// istemciye gömülmez).
/// </summary>
[ApiController]
[Authorize]
// P-09: tevsik haddi SATIŞ akışında sorulur → "islem" anahtarı (uyum değil);
// yanıtta kişisel veri yoktur, yalnız had ve kalan nakit hakkı döner.
[Yetki.ModulYetkisi("islem")]
[Route("api/v1/tevsik")]
public sealed class TevsikController(TevsikKapisi kapi, KuyumcuDbContext db) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>
    /// GET /api/v1/tevsik/gunluk?cariId=...&amp;fisToplamTl=...
    /// Haddi, bugünkü nakit toplamını ve kalan nakit hakkını döner.
    /// </summary>
    /// <param name="cariId">
    /// Müşteri. VERİLMEZSE kümülatif hesaplanamaz (kişi bilinmiyor) ve
    /// <c>kumulatifHesaplandi=false</c> döner — sessizce 0 denmez.
    /// </param>
    /// <param name="fisToplamTl">
    /// Hazırlanmakta olan fişin toplamı. Verilirse §4.1.2 işlem tutarı kuralı
    /// da değerlendirilir: had aşılıyorsa nakit hakkı SIFIRDIR.
    /// </param>
    [HttpGet("gunluk")]
    /// <param name="tarih">
    /// PAKET YASAL-1: haddin OKUNACAĞI yerel gün (YYYY-MM-DD). Verilmezse bugün.
    /// Kümülatif nakit toplamı her zaman BUGÜNÜN hareketlerinden hesaplanır.
    /// </param>
    public async Task<Kuyumcu.Contracts.Sayfalar.NakitSiniri.TevsikGunlukVeri> Gunluk(
        [FromQuery] string? cariId, [FromQuery] decimal? fisToplamTl, [FromQuery] DateOnly? tarih)
    {
        var an = DateTime.UtcNow;
        var esik = kapi.EsiklerGunu(tarih ?? Common.YasalParametreServisi.IslemGunu(an));
        var had = esik.Had;

        var kumulatifHesaplandi = !string.IsNullOrEmpty(cariId);
        var bugunkuNakit = kumulatifHesaplandi
            ? await TevsikKapisi.GunlukNakitToplam(db, Islem.TenantId, cariId!, an)
            : 0m;

        // §4.1.2: fiş toplamı haddi aşıyorsa hiçbir kısım nakit olamaz.
        var islemTutariKuraliDevrede =
            esik.IslemTutariKuraliEtkin && fisToplamTl is { } t && Math.Abs(t) > had;

        var kalanNakitHakki = islemTutariKuraliDevrede
            ? 0m
            : Math.Max(0m, had - bugunkuNakit);

        // PAKET YASAL-2 (TB-15): tahsilat ekranı "Nakit"i bu alana göre kapatır —
        // ekran tahmin etmez, sunucu söyler. Kişisel veri yok.
        Kuyumcu.Contracts.Sayfalar.NakitSiniri.TaksitOnizlemeVeri? taksit = null;
        if (kumulatifHesaplandi)
        {
            var (alinabilir, buyuk, buyukHad) = await kapi.TaksitOnizle(db, Islem.TenantId, cariId!, alacakYonu: true);
            taksit = new Kuyumcu.Contracts.Sayfalar.NakitSiniri.TaksitOnizlemeVeri(
                KuralDevrede: alinabilir is not null && esik.Etkin && esik.IslemTutariKuraliEtkin,
                NakitAlinabilir: alinabilir,
                BuyukIslemTutari: buyuk?.KaynakTutar,
                BuyukIslemGunu: buyuk?.KaynakGun,
                BuyukIslemHaddi: buyukHad,
                Sebep: buyuk is { } b
                    ? string.Format(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"),
                        "{0:N2} TL'yi aşan {1:dd.MM.yyyy} tarihli işlemden kalan borç", buyukHad, b.KaynakGun.ToDateTime(TimeOnly.MinValue))
                    : null);
        }

        return new Kuyumcu.Contracts.Sayfalar.NakitSiniri.TevsikGunlukVeri(
            Taksit: taksit,
            Had: had,
            KuralEtkin: esik.Etkin,
            KumulatifHesaplandi: kumulatifHesaplandi,
            BugunkuNakitToplam: bugunkuNakit,
            KalanNakitHakki: kalanNakitHakki,
            IslemTutariKuraliDevrede: islemTutariKuraliDevrede,
            Aciklama: islemTutariKuraliDevrede
                ? $"Fiş toplamı {had:N2} TL haddini aşıyor — ödemenin tamamı banka, "
                  + "yetkili ödeme kuruluşu veya PTT A.Ş. aracılığıyla yapılmalıdır "
                  + "(459 s. VUK Genel Tebliği §4.1.2)."
                : kumulatifHesaplandi
                    ? $"Bu müşteriye bugün {bugunkuNakit:N2} TL nakit işlem yapıldı; "
                      + $"kalan nakit hakkı {kalanNakitHakki:N2} TL."
                    : "Müşteri seçilmediği için aynı gün kümülatifi hesaplanamadı; "
                      + "yalnız bu işlemin kendi tutarı değerlendirilecek.");
    }
}
