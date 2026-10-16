namespace Kuyumcu.Web.Bilesenler;

// PAKET S1-h — ortak Sekmeler bileşeninin sekme tanımı. Razor içinde record
// yazılmaz (denetim.sh "Pages+Bilesenler+Layout record" tavanı); SecenekOgesi
// ile aynı desen: bileşen klasöründe ayrı .cs dosyası.

/// <summary>Bir sekmenin kodu (URL'deki <c>?sekme=</c> değeri) ve görünen adı.</summary>
/// <remarks>
/// PAKET S1-i: isteğe bağlı <paramref name="Rozet"/> (ör. açık bildirim sayısı) ve
/// <paramref name="RozetRenk"/> (rozet belirteç sınıfı: amber, kirmizi, gri…). Boşsa rozet çizilmez.
/// </remarks>
public sealed record SekmeTanimi(string Kod, string Etiket, string? Rozet = null, string? RozetRenk = null);
