using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages.EmanetParcalari;

/// <summary>
/// PAKET EMANET-1 — doğrulayıcı 28.09 (DÜŞÜK): iade çekmecesinde has alanı ortak
/// <see cref="Gram"/> bileşeninin gram biçimiyle 3 hane ("5,000") görünüyordu, aynı
/// ekrandaki bakiye 4 hane ("5,0000 has"). Has girdisi odak dışında tek merkezin
/// has girdi biçimiyle (Bicim.HasGirdi, 4 hane) yazılır. Ortak Gram bileşenine
/// DOKUNULMADI (ortak alan); işaretleme, doğrulama ve id'ler Gram'ınkiyle aynıdır.
/// </summary>
public sealed class EmanetHasGirdi : Gram
{
    protected override string OdakDisiBicim(decimal d) => Bicim.HasGirdi(d);
}
