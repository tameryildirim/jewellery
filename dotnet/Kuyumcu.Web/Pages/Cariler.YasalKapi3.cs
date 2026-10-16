using Kuyumcu.Contracts.Sayfalar.NakitSiniri;
using Microsoft.JSInterop;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-3 — NAKİT SINIRI ÖNCE (tasarım hakemi KRİTİK, 10.10, YASAL-KAPI-2 kapanış devri).
//
// Kusur: /cariler Tahsilat çekmecesinde 250.000 ₺ NAKİT → ekran önce MASAK kimlik formunu (5 zorunlu alan)
// doldurtuyor, Kaydet'te sunucu nakit sınırıyla (459 s. VUK GT, kod NAKIT_SINIRI) reddediyordu; ödeme türü
// Kart'a çevrilince eski hata yerinde kalıyordu (silinmiyordu).
//
// Kural (sunucuyla AYNI, ekran tahmin etmez): sunucu /tevsik/gunluk?cariId yanıtında haddi, bugünkü nakit
// toplamını ve KALAN NAKİT HAKKINI söyler (TevsikController.Gunluk). Sunucunun reddi
// TevsikKurali.Degerlendir: bugünkü + bu işlem > had  ⇔  bu işlem > kalan hak (tam had serbest).
//   • Nakit seçili ve tutar > kalan hak → tutar alanının ALTINDA tek cümle, Kaydet kilitli, kimlik formu
//     ÇİZİLMEZ (önce ödeme türü — form doldurtulmaz).
//   • Kalan hak 0 → "Nakit" seçicide görünür ama seçilemez, gerekçesi alt satırında.
//   • Sunucu yine NAKIT_SINIRI derse (ör. pencere açıkken başka kasada nakit alındı) ret YasalKapiIzi
//     deseniyle tutulur: ödeme türü değişince ya da tutar reddin ölçüsünün altına inince kalkar, Nakit +
//     aynı tutara dönülünce geri gelir. Bantta ham metin yok, alan altında tek cümle.
//   • Kural kapalıysa (KuralEtkin=false) ya da müşteri bilinmiyorsa ön kontrol YOK — son söz sunucuda.
// Had SAYISI istemciye gömülmez: cümledeki sayı sunucunun kalan hakkıdır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir para formunun nakit sınırı durumu (form başına bir örnek; formu açan <see cref="Ac"/> çağırır).</summary>
public sealed class CariNakitSiniri
{
    /// <summary>Sunucunun son /tevsik/gunluk yanıtı (null = sorulmadı ya da alınamadı → ön kontrol yok).</summary>
    public TevsikGunlukVeri? Gunluk { get; private set; }

    private YasalKapiIzi? redIzi;
    private string? redMetni;

    /// <summary>Form açılırken: önceki formun izi kalmaz.</summary>
    public void Ac() { Gunluk = null; redIzi = null; redMetni = null; }

    /// <summary>Sunucu yanıtını yerleştirir.</summary>
    public void Yukle(TevsikGunlukVeri? gunluk) => Gunluk = gunluk;

    /// <summary>Bugünkü KALAN nakit hakkı — yalnız kural açık ve kümülatif hesaplandıysa (aksi hâlde null: ön kontrol yok).</summary>
    public decimal? Kalan => Gunluk is { KuralEtkin: true, KumulatifHesaplandi: true } g ? Math.Max(0m, g.KalanNakitHakki) : null;

    /// <summary>Bugün bu müşteriden hiç nakit alınamaz (kalan hak 0).</summary>
    public bool NakitKapali => Kalan is <= 0m;

    /// <summary>Ölçü (TL) nakitle alınırsa sunucunun reddedeceği biliniyor mu (ön kontrol ya da geçerli ret izi).</summary>
    public bool Asildi(bool nakit, decimal olcuTl, string? cariId) =>
        nakit && olcuTl > 0m && ((Kalan is { } k && olcuTl > k) || YasalKapiIzi.Gecerli(redIzi, olcuTl, cariId));

    /// <summary>
    /// Alan altındaki tek cümle (yoksa null). <paramref name="sinirCumlesi"/> kalan hakla biçimlenir
    /// (TL tahsilat: "Nakit en çok {0} olabilir…", döviz: "… {0} karşılığı …").
    /// </summary>
    public string? Hata(bool nakit, decimal olcuTl, string? cariId, string dil, string sinirCumlesi)
    {
        if (!nakit || olcuTl <= 0m) return null;
        if (Kalan is { } k && olcuTl > k)
            return k <= 0m ? YasalKapi3Sozlugu.Al(dil).NakitBugunYok : string.Format(sinirCumlesi, Bicim.Para(k, "TRY"));
        return YasalKapiIzi.Gecerli(redIzi, olcuTl, cariId) ? redMetni : null;
    }

    /// <summary>Sunucunun nakit sınırı reddi mi (kodla tanınır — YASAL-3).</summary>
    public static bool NakitReddi(ApiHata ex) => ex.StatusCode == 400 && ex.Kod == "NAKIT_SINIRI";

    /// <summary>Sunucu reddini iz olarak tutar (sunucunun cümlesi zaten sade — YASAL-3: mevzuat kodu yok).</summary>
    public void Reddet(ApiHata ex, decimal olcuTl, string? cariId)
    {
        redIzi = YasalKapiIzi.Al(olcuTl, cariId);
        redMetni = ex.Message;
    }
}

public partial class Cariler
{
    /// <summary>TL tahsilat ve döviz tahsilat çekmecelerinin nakit sınırı (aynı anda yalnız biri açık; her açılış Ac).</summary>
    private readonly CariNakitSiniri NS = new();

    private YasalKapi3Metinleri Y3 => YasalKapi3Sozlugu.Al(Dil.Dil);

    /// <summary>Sunucunun günlük nakit durumunu sorar (hata yutulmaz — UX-S28: önizleme hata bandına yazılır).</summary>
    private async Task<(TevsikGunlukVeri? Veri, string? Hata)> TevsikGunlukSor(string cariId)
    {
        try { return (await Api.GetAsync<TevsikGunlukVeri>($"/tevsik/gunluk?cariId={Uri.EscapeDataString(cariId)}"), null); }
        catch (ApiHata ex) { return (null, string.Format(TT.OnizlemeHata, ex.Message)); }
    }

    // ── TL tahsilat ──
    /// <summary>Ödeme türü değişti: sunucunun eski alan reddi ve bant temizlenir; nakit sınırı yeniden değerlendirilir (ifade).</summary>
    private void ThOdemeDegisti(string? v)
    {
        thOdeme = v ?? "NAKIT";
        thTutarSunucuHata = null;
        islemHata = null;
    }

    /// <summary>Günlük nakit hakkı aşıldı (ön kontrol ya da sunucu reddi izi).</summary>
    private bool ThGunlukNakitAsildi => NS.Asildi(thOdeme == "NAKIT", thTutar, tahsilatCari?.Id);

    /// <summary>Kimlik formu çizilir mi: nakit sınırı aşılmışken form doldurtulmaz (önce ödeme türü).</summary>
    private bool ThMasakCizilir => PM.FormCizilir && !ThNakitSinirAsildi;

    // ── YASAL-KAPI-3 tur 2 (doğrulayıcı ORTA "375'te beyan açıklaması ilk ekranda değil") ─────────────────────
    // Çekmecede kimlik formu İLK KEZ açıldığında son zorunlu alan (beyan açıklaması) görünür bölgeye EN AZ kaydırmayla
    // alınır (block: nearest) — tutar/ödeme yukarıda görünür kalır, beş zorunlu alan birlikte ekrandadır. Odak çalınmaz.
    private bool cekmeceFormGorulmustu;
    private bool CekmeceKimlikFormuAcik => PM.FormCizilir && !ThNakitSinirAsildi && !DtNakitAsildi;

    private async Task CekmeceKimlikKaydir()
    {
        if (!CekmeceKimlikFormuAcik) { cekmeceFormGorulmustu = false; return; }
        if (cekmeceFormGorulmustu) return;
        cekmeceFormGorulmustu = true;
        try
        {
            await using var alan = await JS.InvokeAsync<IJSObjectReference>("document.getElementById", "masak-aciklama");
            await alan.InvokeVoidAsync("scrollIntoView", new { block = "nearest", behavior = "instant" });
        }
        catch { /* JS yok (bUnit) ya da alan çizilmedi: form yine yerinde */ }
    }

    // ── Döviz tahsilat (efektif nakit; ölçü TL karşılığı — sunucu tevsik.Dogrula(tlKarsiligi)) ──
    private decimal DtOlcuTl => DovizTl(dtParaBirimi, dtTutar);
    private bool DtNakitAsildi => dovizTahsilatCari is not null && NS.Asildi(true, DtOlcuTl, dovizTahsilatCari.Id);
    private string? DtNakitHatasi => dovizTahsilatCari is null ? null : NS.Hata(true, DtOlcuTl, dovizTahsilatCari.Id, Dil.Dil, Y3.DovizNakitSinirAsildi);
    private bool DtMasakCizilir => PM.FormCizilir && !DtNakitAsildi;
}
