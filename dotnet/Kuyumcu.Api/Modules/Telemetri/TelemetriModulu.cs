using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Modules.Sistem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kuyumcu.Api.Modules.Telemetri;

// ═════════════════════════════════════════════════════════════════════════════
// TELEMETRİ UÇLARI — PAKET P-28 (F-4 · R-2).
//
// İKİ KİMLİK HATTI (P-27 ile AYNI desen, yeni desen açılmadı):
//   • /api/v1/telemetri              → JWT + [RolGerekli("SAHIP")] · kiracı
//     KENDİ kullanımını okur. SALT OKUNUR: ölçüm otomatik sayaçtır.
//   • /api/v1/sistem/... /telemetri  → SistemAnahtariKapisi (JWT ile geçilmez)
//     · operatör tüm kiracıları okur, destek dakikasını yazar/düzeltir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>POST destek dakikası gövdesi.</summary>
public sealed class DestekDakikasiEkleDto
{
    /// <summary>Desteğin verildiği gün (UTC). Boşsa BUGÜN.</summary>
    public DateTime? Tarih { get; set; }

    [Range(1, TelemetriSistemServisi.DakikaTavani,
        ErrorMessage = "Dakika 1 ile 600 arasında olmalıdır")]
    public int Dakika { get; set; }

    [Required(ErrorMessage = "Kanal zorunludur"), MaxLength(16)]
    public string Kanal { get; set; } = null!;

    [Required(ErrorMessage = "Konu zorunludur"), MaxLength(24)]
    public string Konu { get; set; } = null!;

    [MaxLength(300, ErrorMessage = "Açıklama en fazla 300 karakter olabilir")]
    public string? Aciklama { get; set; }

    /// <summary>MANUEL (varsayılan) · TANILAMA.</summary>
    [MaxLength(16)] public string? Kaynak { get; set; }
}

/// <summary>
/// ÜÇ DURUMLU PATCH (sözleşme: gövdede yoksa KORU · null ise SİL).
/// <c>...Verildi</c> bayrağı JSON anahtarının VARLIĞINDAN doğar.
/// </summary>
public sealed class DestekDakikasiGuncelleDto
{
    private DateTime? tarih;
    [System.Text.Json.Serialization.JsonIgnore] public bool TarihVerildi { get; private set; }
    public DateTime? Tarih { get => tarih; set { tarih = value; TarihVerildi = true; } }

    private int? dakika;
    [System.Text.Json.Serialization.JsonIgnore] public bool DakikaVerildi { get; private set; }
    [Range(1, TelemetriSistemServisi.DakikaTavani,
        ErrorMessage = "Dakika 1 ile 600 arasında olmalıdır")]
    public int? Dakika { get => dakika; set { dakika = value; DakikaVerildi = true; } }

    private string? kanal;
    [System.Text.Json.Serialization.JsonIgnore] public bool KanalVerildi { get; private set; }
    [MaxLength(16)] public string? Kanal { get => kanal; set { kanal = value; KanalVerildi = true; } }

    private string? konu;
    [System.Text.Json.Serialization.JsonIgnore] public bool KonuVerildi { get; private set; }
    [MaxLength(24)] public string? Konu { get => konu; set { konu = value; KonuVerildi = true; } }

    private string? aciklama;
    [System.Text.Json.Serialization.JsonIgnore] public bool AciklamaVerildi { get; private set; }
    [MaxLength(300, ErrorMessage = "Açıklama en fazla 300 karakter olabilir")]
    public string? Aciklama { get => aciklama; set { aciklama = value; AciklamaVerildi = true; } }

    private bool? aktif;
    [System.Text.Json.Serialization.JsonIgnore] public bool AktifVerildi { get; private set; }
    public bool? Aktif { get => aktif; set { aktif = value; AktifVerildi = true; } }
}

// ── Kiracı ucu ───────────────────────────────────────────────────────────────

/// <summary>
/// <c>/api/v1/telemetri</c> — kiracının KENDİ kullanım ölçümü (salt okunur).
/// <para>Rol kapısı SINIF düzeyindedir: [RolGerekli("SAHIP")] TAM eşleşme.</para>
/// </summary>
[ApiController]
[Authorize]
[RolGerekli("SAHIP")]
[Route("api/v1/telemetri")]
public sealed class TelemetriController(TelemetriServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/telemetri?gun=30 — kendi kullanımım + destek özeti.</summary>
    [HttpGet]
    public Task<KiraciTelemetriDto> Ozet([FromQuery] int? gun) => servis.KendiOzetim(Islem, gun);
}

// ── Sistem ucu ───────────────────────────────────────────────────────────────

/// <summary>
/// <c>/api/v1/sistem</c> — operatör telemetri ve destek dakikası yönetimi.
/// <para>⚠ [AllowAnonymous]: bu uçların kimliği <see cref="SistemAnahtariKapisi"/>'dır;
/// geçerli bir SAHIP token'ı bu uçları AÇMAZ (P-27 ile aynı desen).</para>
/// </summary>
[ApiController]
[AllowAnonymous]
[TypeFilter(typeof(SistemAnahtariKapisi))]
[Route("api/v1/sistem")]
public sealed class TelemetriSistemController(TelemetriSistemServisi servis) : ControllerBase
{
    private string Operator =>
        Request.Headers[SistemAnahtariKapisi.OperatorBasligi].ToString().Trim();

    /// <summary>GET /api/v1/sistem/telemetri — F-8 kapasite paydası ölçümü.</summary>
    [HttpGet("telemetri")]
    public Task<KapasiteOzetiDto> Kapasite([FromQuery] int? gun) => servis.KapasiteOzeti(gun);

    /// <summary>GET /api/v1/sistem/kiracilar/{id}/telemetri — tek kiracının yükü.</summary>
    [HttpGet("kiracilar/{id}/telemetri")]
    public Task<KapasiteSatiriDto> KiraciTelemetri(string id, [FromQuery] int? gun) =>
        servis.KiraciOzet(id, gun);

    /// <summary>GET /api/v1/sistem/kiracilar/{id}/destek-dakikasi — kayıt listesi (pasifler dâhil).</summary>
    [HttpGet("kiracilar/{id}/destek-dakikasi")]
    public Task<object> DestekListe(string id) => servis.DestekListe(id);

    /// <summary>POST /api/v1/sistem/kiracilar/{id}/destek-dakikasi — yeni kayıt.</summary>
    [HttpPost("kiracilar/{id}/destek-dakikasi")]
    public Task<DestekDakikasiDto> DestekEkle(string id, [FromBody] DestekDakikasiEkleDto dto) =>
        servis.DestekEkle(id, dto, Operator);

    /// <summary>PATCH .../destek-dakikasi/{kayitId} — düzeltme (gönderilmeyen alan KORUNUR).</summary>
    [HttpPatch("kiracilar/{id}/destek-dakikasi/{kayitId}")]
    public Task<DestekDakikasiDto> DestekDuzenle(
        string id, string kayitId, [FromBody] DestekDakikasiGuncelleDto dto) =>
        servis.DestekDuzenle(id, kayitId, dto, Operator);

    /// <summary>
    /// DELETE .../destek-dakikasi/{kayitId} — PASİFE ALIR (hard delete YOK).
    /// Geri dönüş yolu: aynı kaydı PATCH ile <c>aktif=true</c> yapmak.
    /// </summary>
    [HttpDelete("kiracilar/{id}/destek-dakikasi/{kayitId}")]
    public Task<DestekDakikasiDto> DestekPasifeAl(string id, string kayitId) =>
        servis.DestekAktiflik(id, kayitId, aktif: false, Operator);
}
