using Kuyumcu.Contracts.Kisit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kuyumcu.Api.Modules.Meta;

/// <summary>
/// <c>/api/v1/genel/kisitlar</c> — 1.0'da bulunmayan yeteneklerin beyanı (P-30 · R-4 · BK-47).
/// <para>
/// ⚠ [AllowAnonymous] GEREKÇESİ: bu uç GİRİŞ EKRANINDAN, yani oturum açılmadan
/// okunur (BK-47: "hariç listesi demo kiracısının giriş ekranında okunur").
/// Kiracıya özel HİÇBİR veri dönmez — yanıt <see cref="KisitBeyani.Liste"/>
/// sabitidir, veritabanına dokunmaz; bu yüzden kiracı izolasyonu açısından
/// anlamlı bir yüzey açmaz.
/// </para>
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/genel")]
public sealed class KisitController : ControllerBase
{
    /// <summary>GET /api/v1/genel/kisitlar — H-1…H-8 kısıt satırları.</summary>
    [HttpGet("kisitlar")]
    public KisitBeyaniYaniti Kisitlar() =>
        new(KisitBeyani.Kaynak, KisitBeyani.Liste.Count, KisitBeyani.Liste);
}
