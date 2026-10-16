using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Modules.Sistem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kuyumcu.Api.Modules.Abonelik;

// ═════════════════════════════════════════════════════════════════════════════
// ABONELİK UÇLARI — PAKET P-27 (F-1 + F-3 · R-1).
//
// İKİ KİMLİK HATTI, İKİ AYRI CONTROLLER (tek controller'a karıştırılamaz):
//   • /api/v1/abonelik        → JWT + [RolGerekli("SAHIP")] · kiracının kendi
//     görünümü ve YALNIZ plan İNDİRME.
//   • /api/v1/sistem/kiracilar/{id}/abonelik → SistemAnahtariKapisi (JWT ile
//     GEÇİLEMEZ) · tek YÜKSELTME yolu. Yol öneki bilerek '/api/v1/sistem'tir:
//     KiraciDurumKapisi muafiyeti oradadır, yoksa süresi dolmuş bir kiracının
//     aboneliği yenilenemez — kapı kendi kendini kilitlerdi.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>PATCH /abonelik/plan gövdesi — kiracı yolu (yalnız indirme).</summary>
public sealed class PlanDegistirDto
{
    /// <summary>
    /// Plan kodu. ⚠ Burada <c>RegularExpression</c> ile plan listesi SABİTLENMEZ:
    /// katalog tablodadır, koda ikinci kez yazılsaydı yeni plan eklendiğinde
    /// uç sessizce reddederdi. Doğrulama SERVİSTE, katalogdan yapılır.
    /// </summary>
    [Required(ErrorMessage = "Plan kodu zorunludur"), MaxLength(20)]
    public string Plan { get; set; } = null!;
}

/// <summary>POST /sistem/kiracilar/{id}/abonelik gövdesi — operatör yolu.</summary>
public sealed class SistemAbonelikDto
{
    [Required(ErrorMessage = "Plan kodu zorunludur"), MaxLength(20)]
    public string Plan { get; set; } = null!;

    /// <summary>Dönem bitişi (UTC). Gönderilirse İLERİ tarihli olmalı; null = süresiz.</summary>
    public DateTime? Bitis { get; set; }

    /// <summary>NEDEN — en az 20 karakter (MÜHÜR 4 / K3). Denetim izine bu yazılır.</summary>
    [Required(ErrorMessage = "Gerekçe zorunludur")]
    [MinLength(20, ErrorMessage = "Gerekçe en az 20 karakter olmalıdır")]
    [MaxLength(500)]
    public string Gerekce { get; set; } = null!;
}

// ── Kiracı ucu ───────────────────────────────────────────────────────────────

/// <summary>
/// <c>/api/v1/abonelik</c> — kiracının kendi abonelik görünümü.
/// <para>Rol kapısı SINIF düzeyindedir: [RolGerekli("SAHIP")] TAM eşleşme.</para>
/// </summary>
[ApiController]
[Authorize]
[RolGerekli("SAHIP")]
[Route("api/v1/abonelik")]
public sealed class AbonelikController(AbonelikServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/abonelik — geçerli dönem + katalog + tarihçe.</summary>
    [HttpGet]
    public Task<AbonelikDurumDto> Durum() => servis.DurumGetir(Islem);

    /// <summary>
    /// PATCH /api/v1/abonelik/plan — plan İNDİRME. Yükseltme 403 döner
    /// (F-3 plan kapısı) ve reddedilen girişim denetime yazılır.
    /// </summary>
    [HttpPatch("plan")]
    public Task<AbonelikDurumDto> PlanDegistir([FromBody] PlanDegistirDto dto) =>
        servis.KiraciPlanDegistir(dto.Plan, Islem);
}

// ── Sistem ucu ───────────────────────────────────────────────────────────────

/// <summary>
/// <c>/api/v1/sistem/kiracilar/{id}/abonelik</c> — TEK yükseltme yolu.
/// <para>
/// ⚠ [AllowAnonymous]: bu ucun kimliği <see cref="SistemAnahtariKapisi"/>'dır;
/// geçerli bir SAHIP token'ı bu ucu AÇMAZ (SistemController ile AYNI desen —
/// yeni kimlik deseni açılmadı).
/// </para>
/// </summary>
[ApiController]
[AllowAnonymous]
[TypeFilter(typeof(SistemAnahtariKapisi))]
[Route("api/v1/sistem")]
public sealed class AbonelikSistemController(AbonelikSistemServisi servis) : ControllerBase
{
    private string Operator =>
        Request.Headers[SistemAnahtariKapisi.OperatorBasligi].ToString().Trim();

    /// <summary>POST /api/v1/sistem/kiracilar/{id}/abonelik — plan ata / dönem uzat.</summary>
    [HttpPost("kiracilar/{id}/abonelik")]
    public Task<object> Ata(string id, [FromBody] SistemAbonelikDto dto) =>
        servis.SistemPlanDegistir(id, dto, Operator);
}
