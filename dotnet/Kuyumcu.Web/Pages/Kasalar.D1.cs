using Kuyumcu.Contracts.Sayfalar.SaticiPersonel;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// D1-11 (18.10, rakip eşitleme: "çalışan kasası") — kasa düzenlemede sorumlu çalışan seçimi.
// Liste /islemler/personeller'den (satış ekranının "Satan" seçicisiyle AYNI kaynak, aktif kullanıcılar).
// ═════════════════════════════════════════════════════════════════════════════
public partial class Kasalar
{
    private string? duzenleSorumlu, yeniSorumlu;
    private List<SaticiPersonelSatiri> personeller = [];
    private bool personelYuklendi;
    private string? personelHata;

    private IReadOnlyList<SecenekOgesi> SorumluSecenekleri =>
        [.. personeller.Select(p => new SecenekOgesi(p.Id, $"{p.Ad} {p.Soyad}".Trim()))];

    private async Task PersonelleriYukle()
    {
        if (personelYuklendi) return;
        try
        {
            personeller = (await Api.GetAsync<SaticiPersonelListesi>("/islemler/personeller")).Personeller ?? [];
            personelYuklendi = true; personelHata = null;
        }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException) { personelHata = ex.Message; }
    }
}
