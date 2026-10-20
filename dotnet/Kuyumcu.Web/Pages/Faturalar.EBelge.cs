using Kuyumcu.Contracts.Sayfalar;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.JSInterop;

namespace Kuyumcu.Web.Pages;

// 20.10 — fatura listesinde e-belge durumu (giden kutusu): sırada / gönderildi / GİB onayladı / gönderilemedi (+ sebep,
// "Yeniden dene") / alıcı reddetti / iptal; entegratöre ulaşmış belgede resmi PDF. Kayıt yoksa satıra bir şey yazılmaz.
public partial class Faturalar
{
    private Dictionary<string, EBelgeGonderimOzet> ebelgeler = [];
    private string? ebelgeIslemde;

    private async Task EBelgeYukle()
    {
        try { ebelgeler = (await Api.GetAsync<List<EBelgeGonderimOzet>>("/ebelge/gonderimler")).ToDictionary(g => g.FaturaId); }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException) { listeHata ??= ex.Message; }
    }

    private static string EBelgeRenk(string durum) => durum switch
    {
        "KABUL" => "metin-yesil", "HATA" or "RET" => "metin-kirmizi", "IPTAL" => "metin-soluk", _ => "metin-mavi",
    };

    private async Task EBelgeYenidenDene(string faturaId)
    {
        ebelgeIslemde = faturaId; listeHata = null;
        try { ebelgeler[faturaId] = await Api.PostAsync<EBelgeGonderimOzet>($"/ebelge/faturalar/{Uri.EscapeDataString(faturaId)}/yeniden-dene"); }
        catch (ApiHata ex) { listeHata = ex.Message; }
        finally { ebelgeIslemde = null; }
    }

    private async Task EBelgePdf(string faturaId)
    {
        ebelgeIslemde = faturaId; listeHata = null;
        try
        {
            var (veri, ad) = await Api.GetBaytAsync($"/ebelge/faturalar/{Uri.EscapeDataString(faturaId)}/pdf");
            await JS.InvokeVoidAsync("kuyumcuIndir", ad ?? "e-fatura.pdf", Convert.ToBase64String(veri), "application/pdf");
        }
        catch (ApiHata ex) { listeHata = ex.Message; }
        catch (JSException ex) { listeHata = ex.Message; }
        finally { ebelgeIslemde = null; }
    }
}
