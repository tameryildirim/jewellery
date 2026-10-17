using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Fis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-TEZGAH-TUR-KIMLIK — ekran denetimi 10.10 (K4, Y4, Y2, O10) + YASAL-KAPI-3 tasarım kalanları.
//  (Y4) İşlem türü GÖRÜNÜR seçici: Yıldız'da Satış yüzünün üst satırında ve öteki türlerin (Alış, İade, Takas …)
//       başında AYNI sekmeler (FisTurSecici). Öteki türlerde eski "Ne yapıyorsun?" ızgarası / sekme şeridi /
//       "Diğer ▾" kutusu Yıldız'da yalnız "Diğer" açılınca görünür; form tuval dilinde (tema-yildiz.css TUR-KIMLIK).
//  (K4) Kilitli Kaydet EKSİK ALANLARIN ADINI söyler ("Kaydetmek için eksik: işlem amacı, …") — kimlik formu
//       açıkken de Kaydet'in yanında (eskiden genel cümle, Platin'de form açıkken gizli). Kimlik çekmecesinin "Tamam"ı
//       eksik formla basılınca eksik alanların ALTINA mesaj yazılır ve ilk eksiğe odaklanılır.
// Kural/gövde/eşik DEĞİŞMEZ: kilit koşulu sayfanın MasakTamam'ı; alan adları MasakEksikAlanlari'ndan (MasakFormu.Eksiksiz
// ile birebir aynı koşullar); hiçbir eşik sayısı istemciye gömülmez (beyan açıklamasının en az uzunluğu sunucudan).
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    /// <summary>Tur 2 (doğrulayıcı DÜŞÜK, UX-S31): Yıldız'da alanın etiketi "Müşteri" — kilit cümlesi de "Müşteri seçin" der
    /// (PRO'da "Cari seçin" jargonu Yıldız tezgâhına sızmaz). Platin PRO'da muhasebe dili (D10) AYNEN.</summary>
    private string CariSecinKilit => BasitDilSozlugu.Al(Dil.Dil).Islem_CariSecin;

    /// <summary>Kimlik çekmecesinde "Tamam" eksik formla basıldı → MasakFormu boş/kısa zorunlu alanların altına yazar.</summary>
    private bool masakEksikGoster;

    /// <summary>Yıldız şemasında Satış yüzü DIŞINDAKİ fiş (Alış, İade, Takas, Hurda alış …): türler sekmesi başta, form tuval dilinde.</summary>
    internal bool YildizTurYuzu => !YildizTezgahMi && !yukleniyor;

    /// <summary>Öteki türlerin sayfa sınıfı (CSS: eski ızgara/şerit yalnız "Diğer" açıkken; form tuval dilinde).</summary>
    private string YildizTurSinif => !YildizTurYuzu ? ""
        : " yz-islem-tur" + (yildizTurlerAcik ? " yz-turler-acik" : "") + (DigerAcik ? " yz-diger-acik" : "");

    /// <summary>Sekmeden tür seçimi: açık "Diğer" listesi kapanır, sayfanın MEVCUT tür seçimi (TurSecDiger) çağrılır.</summary>
    private async Task YildizTurSec(string kod)
    {
        yildizTurlerAcik = false;
        await TurSecDiger(kod);
    }

    private void TurSeciciCiz(RenderTreeBuilder b, string id, RenderFragment? diger)
    {
        b.OpenComponent<FisTurSecici>(0);
        b.AddAttribute(1, nameof(FisTurSecici.Turler), TurListesi);
        b.AddAttribute(2, nameof(FisTurSecici.Secili), DigerAcik ? "" : tur);
        b.AddAttribute(3, nameof(FisTurSecici.Basit), Modu.BasitMi);
        b.AddAttribute(4, nameof(FisTurSecici.TurSec), EventCallback.Factory.Create<string>(this, YildizTurSec));
        b.AddAttribute(5, nameof(FisTurSecici.CariId), cariId);
        b.AddAttribute(6, nameof(FisTurSecici.PesinCariId), pesinCariId);
        b.AddAttribute(7, nameof(FisTurSecici.Diger), diger);
        b.AddAttribute(8, nameof(FisTurSecici.Id), id);
        b.CloseComponent();
    }

    /// <summary>Satış yüzünün sekmeleri (YildizTezgah üst satırı; "Diğer" orada #yildiz-tezgah-tur).</summary>
    private RenderFragment YildizTurSeciciParcasi => b => TurSeciciCiz(b, "yildiz-tur-secici", null);

    /// <summary>Öteki türlerin başı: aynı sekmeler + "Diğer" (#yildiz-tur-diger — tüm türler ve PRO "Diğer ▾").</summary>
    private RenderFragment YildizTurUstParcasi => b =>
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "yz-tur-ust-sar");
        b.AddAttribute(2, "id", "yildiz-tur-ust");
        b.OpenElement(3, "p");
        b.AddAttribute(4, "class", "yz-ust yz-tur-ust");
        b.AddContent(5, (RenderFragment)(c => TurSeciciCiz(c, "yildiz-tur-secici-diger", d =>
        {
            d.OpenElement(0, "button");
            d.AddAttribute(1, "type", "button");
            d.AddAttribute(2, "class", "yz-tur-sekme yz-tur-diger");
            d.AddAttribute(3, "id", "yildiz-tur-diger");
            d.AddAttribute(4, "aria-expanded", yildizTurlerAcik || DigerAcik ? "true" : "false");
            d.AddAttribute(5, "onclick", EventCallback.Factory.Create(this, () => yildizTurlerAcik = !yildizTurlerAcik));
            d.AddContent(6, TurKimlikSozlugu.Al(Dil.Dil).Diger);
            d.CloseElement();
        })));
        b.CloseElement();
        b.CloseElement();
    };

    /// <summary>
    /// K4: kimlik formunun eksik alanları TEK cümlede ("Kaydetmek için eksik: işlem amacı, beyan açıklaması (en az 20 karakter).").
    /// Gereksinim (sunucu) bilinmiyorsa ya da adı çıkarılabilen eksik yoksa null → çağıran eski cümleye düşer (uydurma yok).
    /// </summary>
    internal string? KimlikEksikCumlesi =>
        MasakGerekli && !masakHata && masakGereksinim is { } g && MasakEksikAlanlari(g) is { Count: > 0 } l
            ? string.Format(TurKimlikSozlugu.Al(Dil.Dil).KimlikEksik, string.Join(", ", l))
            : null;

    /// <summary>K4: çekmecenin "Tamam"ı eksik formla basıldı — alan altı mesajlar açılır, ilk eksik alana odaklanılır.</summary>
    private async Task KimlikEksikGosterIste()
    {
        masakEksikGoster = true;
        StateHasChanged();
        try
        {
            await Task.Yield();
            await using var alan = await JS.InvokeAsync<IJSObjectReference>("document.getElementById", IlkEksikKimlikAlani());
            await alan.InvokeVoidAsync("focus");
        }
        catch { /* JS yok (bUnit) ya da alan çizilmedi: mesajlar yine alanların altında */ }
    }

    /// <summary>İlk eksik zorunlu kimlik alanı — MasakEksikAlanlari (Islemler.ParaDemo3) ile BİREBİR aynı koşullar ve sıra
    /// (tur 2, doğrulayıcı DÜŞÜK: belge türü ve uyruk atlanıyordu). Uyruk "Diğer bilgiler" katlanırındadır; MasakFormu
    /// EksikGoster açıkken uyruk geçersizse katlanırı kendisi açar (UyrukZorlaAcik).</summary>
    internal string IlkEksikKimlikAlani()
    {
        var g = masakGereksinim;
        if (string.IsNullOrWhiteSpace(masak.BelgeTuru)) return "masak-belge-turu";
        if (string.IsNullOrWhiteSpace(masak.BelgeNo) || MasakFormu.TcknKilitMetni(masak, Dil.Dil) is not null) return "masak-belge-no";
        if (string.IsNullOrWhiteSpace(masak.AdSoyad)) return "masak-ad-soyad";
        if (!UlkeListesi.KodGecerli(masak.Uyruk)) return "masak-uyruk";
        if (g is { AmacGerekli: true } && string.IsNullOrWhiteSpace(masak.IslemAmaci)) return "masak-amac";
        if (g is { AmacGerekli: true } && (masak.Aciklama?.Trim().Length ?? 0) < g.AsgariAciklamaUzunlugu) return "masak-aciklama";
        if (g is { BeyanFormuGerekli: true } && string.IsNullOrWhiteSpace(masak.FonKaynagi)) return "masak-fon";
        if (g is { EkBelgeGerekli: true } && string.IsNullOrWhiteSpace(masak.EkBelgeReferansi)) return "masak-ekbelge";
        return "masak-belge-no";
    }
}
