using Microsoft.JSInterop;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-3 tur 2 (tasarım hakemi KRİTİK ×4, fiş):
//  (1) Nakit seçili ve nakit kuralı bu fişte tetikliyken (NakitBandiVar — kural sunucudan) kimlik formu AÇILMAZ:
//      tezgâhtar önce ödeme türünü düzeltir; form ödeme kart/havaleye dönünce açılır (sunucu da nakdi reddeder,
//      formu doldurtup Kaydet'te reddetmek yok — Cariler çekmecesiyle aynı kural, YASAL-KAPI-3 tur 1).
//  (2) Yasal engel ekranda TEK yerde: BASİT'te üst bant (#islem-yasal-kapi), PRO'da Kaydet'in yanı
//      (#islem-kaydet-kilit), kimlik formu açıksa formun başlığı. Aynı engeli tekrar eden satırlar (ödeme
//      sütunundaki kısa durum + "Neden?", "Kart olarak seçildi" notu, PRO paneldeki "Aşıldı") o anda çizilmez;
//      PRO paneli katlı gelir. Düğmenin title'ı sebebi taşımaya devam eder (UX-S30).
//  (3) Kimlik formu ilk kez açıldığında başlığı görünür bölgenin üstüne kaydırılır ve ilk boş zorunlu alana
//      odaklanılır — zorunlu alanlar ilk ekranda (Yıldız tezgâhında form kendi çekmecesinde; dokunulmaz).
// Eşik/had SAYISI istemciye gömülmez; kurallar AYNEN sunucuda (POST gövdeleri değişmedi).
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    /// <summary>YK3: ödemede nakit var ve nakit kuralı bu fişte tetikli → kimlik formu bekler (önce ödeme türü).</summary>
    internal bool NakitOnce => NakitBandiVar;

    /// <summary>YK3: nakit engelinin TEK cümlesi — had biliniyorsa sunucunun sayısıyla ("{had} üstündeki fişte nakit kullanılamaz —
    /// ödeme türünü kart ya da havale yapın"; YasalKilitSebebi ile aynı), yoksa ya da iade/takasta yönlü cümle.</summary>
    internal string NakitKilitCumlesi =>
        tevsikHaddi is { } h && h > 0m && TevsikOnerisiGerekli && !KarmaAcik && !(IadeKipi || tur == "TAKAS")
            ? string.Format(Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil).NakitHaddiKilit, Kuyumcu.Web.Servisler.Bicim.Tl(h))
            : NakitBantMetni;

    /// <summary>YK3: kimlik formu sayfada çiziliyor mu (MasakParcasi'nın koşuluyla aynı; Yıldız tezgâhında çekmecede).</summary>
    internal bool MasakFormuCizili => MasakGerekli && !PesinEsikUstu && !NakitOnce && masakGereksinim is not null;

    /// <summary>YK3: BASİT üst bant yasal bir cümle söylüyor mu (tek yer).</summary>
    private bool YasalCumleBantta => Modu.BasitMi && !YildizTezgahMi && BasitBantAcik && BasitBantMesaji is not null;

    /// <summary>YK3: sayfa şu an bir yasal engeli/kimlik formunu kendi TEK yerinde söylüyor mu — öteki tekrarlar çizilmez.</summary>
    internal bool YasalMesajVar =>
        YasalCumleBantta || YasalEngelMetni is not null || NakitBandiVar || (MasakFormuCizili && !YildizTezgahMi) || pesinKimlikGerekli;

    /// <summary>YK3: Kaydet'in yanındaki kilit cümlesi metinsiz mi — aynı engel BASİT üst bantta ya da kimlik formunun başlığında yazıyor
    /// (eskiden görünmez sınıfla DOM'da ikinci kez duruyordu; sebep düğmenin title'ında kalır).</summary>
    internal bool KilitMetniBantta(string kilit) =>
        KilitBantlaAyni(kilit) || (YasalCumleBantta && YasalKilitSebebi is { } y && kilit == y)
        ;   // P-TEZGAH-TUR-KIMLIK (denetim K4): kimlik formu açıkken de kilit cümlesi Kaydet'in YANINDA (artık eksik alanların adını söyler)

    /// <summary>YK3: "Tutar nakit sınırını aştığı için ödeme Kart olarak seçildi" notu — yalnız başka yasal mesaj
    /// yokken ve tek yerde: PRO'da panelde, öteki yüzlerde ödeme alanının altında.</summary>
    private bool OtomatikNotPanelde => Modu.ProfesyonelMi && !YildizTezgahMi && (tur == "SATIS" || MasakGerekli);   // panelin yasal kartı çiziliyorsa (FisProYanSutun YasalGoster ile aynı koşul)
    internal string? OdemeOtomatikAlanNotu => YasalMesajVar || OtomatikNotPanelde ? null : OdemeOtomatikNotu;
    internal string? OdemeOtomatikPanelNotu => YasalMesajVar ? null : OdemeOtomatikNotu;

    /// <summary>YK3: BASİT üst bant — kimlik formu açıkken ve bantın söyleyeceği başka engel yokken ("gereken alanlar
    /// açıldı") çizilmez: formun başlığı aynı şeyi söyler ve form görünür bölgeye kaydırılır.</summary>
    internal bool BasitBantKapiAcik => BasitBantAcik && !(MasakFormuCizili && BasitBantMesaji is null);

    private bool yk3FormGorulmustu;

    /// <summary>YK3 (3): kimlik formu bu çizimde İLK KEZ açıldıysa başlığını üste kaydır, ilk boş zorunlu alana odaklan.</summary>
    private async Task KimlikFormunaKaydir()
    {
        if (!MasakGerekli) masakEksikGoster = false;   // P-TEZGAH-TUR-KIMLIK: yeni fişte "Tamam"ın alan altı mesajları kendiliğinden açılmaz
        var acik = MasakFormuCizili && !YildizTezgahMi;
        if (!acik) { yk3FormGorulmustu = false; return; }
        if (yk3FormGorulmustu) return;
        yk3FormGorulmustu = true;
        try
        {
            await using var baslik = await JS.InvokeAsync<IJSObjectReference>("document.getElementById", "masak-baslik");
            await baslik.InvokeVoidAsync("scrollIntoView", new { block = "start", behavior = "instant" });
            // Barkod/adet/fiyat kutusunda yazarken (okuyucu tarıyor) odak ÇALINMAZ — form yalnız görünür olur.
            if (await JS.InvokeAsync<bool>("document.activeElement.matches", "input:not([type=checkbox]):not([type=radio]),textarea")) return;
            await using var alan = await JS.InvokeAsync<IJSObjectReference>("document.getElementById", IlkBosKimlikAlani());
            await alan.InvokeVoidAsync("focus", new { preventScroll = true });
        }
        catch { /* JS yok (bUnit) ya da alan çizilmedi: form yine yerinde, Kaydet sebebi yazılı */ }
    }

    /// <summary>İlk boş zorunlu kimlik alanı (formdaki sırayla).</summary>
    private string IlkBosKimlikAlani() =>
        string.IsNullOrWhiteSpace(masak.BelgeNo) ? "masak-belge-no"
        : string.IsNullOrWhiteSpace(masak.AdSoyad) ? "masak-ad-soyad"
        : string.IsNullOrWhiteSpace(masak.IslemAmaci) && masakGereksinim is { AmacGerekli: true } ? "masak-amac"
        : string.IsNullOrWhiteSpace(masak.Aciklama) && masakGereksinim is { AmacGerekli: true } ? "masak-aciklama"
        : "masak-belge-no";
}
