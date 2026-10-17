using System.Text.Json;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// M7a (kurucu 17.10) — gelen ödeme: dekontta gönderenin IBAN'ı / TC'si yazılırsa sunucu müşteriyi
// bulur, yoksa açar; sonuç sade bir cümleyle söylenir. Müşteriye bağlanan ödeme "karar bekleyen"
// kutusuna düşer ("borcundan düşülsün mü?").
// ═════════════════════════════════════════════════════════════════════════════
public partial class Banka
{
    private GelenOdemeKarari? gelenKarar;
    private string dekontKarsiIban = "";
    private string dekontKimlikNo = "";

    private bool GelenMusteriAranir => dekontYon == "GELEN" && string.IsNullOrEmpty(dekontCariId);
    private string? DekontIbanGovde => GelenMusteriAranir && !string.IsNullOrWhiteSpace(dekontKarsiIban)
        ? dekontKarsiIban.ToUpperInvariant().Replace(" ", "") : null;
    private string? DekontKimlikGovde => GelenMusteriAranir && !string.IsNullOrWhiteSpace(dekontKimlikNo) ? dekontKimlikNo : null;

    /// <summary>Sunucunun müşteri çözümünü sade cümleye çevirir; bağ yoksa null (genel "eklendi").</summary>
    private string? DekontSonucMetni(JsonElement yanit)
    {
        if (!yanit.TryGetProperty("cari", out var c) || c.ValueKind != JsonValueKind.Object) return null;
        var yol = c.GetProperty("yol").GetString();
        var ad = c.TryGetProperty("cariAd", out var a) ? a.GetString() ?? "" : "";
        return yol switch
        {
            "KIMLIK" or "IBAN" => GelenOdemeSozlugu.Baglandi(ad, Dil.Dil),
            "YENI" => GelenOdemeSozlugu.YeniMusteri(ad, Dil.Dil),
            "ONERI" => GelenOdemeSozlugu.Benzer(string.Join(", ",
                c.GetProperty("oneriler").EnumerateArray().Select(o => o.GetProperty("ad").GetString())), Dil.Dil),
            _ => null,
        };
    }

    /// <summary>Yıldız listesinin bant yuvası: sayfa bantları + karar kutusu (başlığın altında, sayfa hizasında).</summary>
    private RenderFragment YildizBantlari => b =>
    {
        b.AddContent(0, Kuyumcu.Web.Bilesenler.Yildiz.YildizBant.Ciz(listeHata, basari, basariJeton, 3000));
        b.OpenComponent<GelenOdemeKarari>(1);
        b.AddAttribute(2, nameof(GelenOdemeKarari.Degisti), EventCallback.Factory.Create(this, GelenKararSonrasi));
        b.AddComponentReferenceCapture(3, r => gelenKarar = (GelenOdemeKarari)r);
        b.CloseComponent();
    };

    private async Task GelenKararSonrasi()
    {
        await HesaplariYukle();
        if (!string.IsNullOrEmpty(AktifHesapId)) await HareketleriYukle();
    }
}
