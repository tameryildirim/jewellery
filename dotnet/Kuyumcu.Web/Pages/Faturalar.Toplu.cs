namespace Kuyumcu.Web.Pages;

// 18.10 — toplu fatura çekmecesinin sayfa tarafı: aç/kapa + kesim sonrası listeyi tazele, sonucu banda yaz.
public partial class Faturalar
{
    private bool topluAcik;

    private async Task TopluKesildi(string metin)
    {
        BasariGoster(metin);
        await Yukle();
    }
}
