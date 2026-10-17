using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kuyumcu.Api.Modules.Satinalma;

// ═════════════════════════════════════════════════════════════════════════════
// 17.10 kurucu: stok girişi tek adımlı "Mal geldi" — tedarikçi · ürün · adet · fiyat yazılır, tek düğmeyle
// stoğa girer. Sipariş → onay → teslim alma mevcut SatinalmaServisi yollarıyla SIRAYLA yapılır (kural
// tekrarlanmaz: MASAK, adet birimi, şube, kur, irsaliye, tedarikçi borcu aynı kapılardan geçer).
//   • Önce bu uç kendi doğrulamasını yapar (boş/eksik satır, adet, fiyat, kur) — reddedilecek istek sipariş açmaz.
//   • Teslim alma reddedilirse açılan sipariş AYNI istekte gerekçeyle iptal edilir (kabul yazılmadığı için
//     iptal edilebilir); kullanıcı yarım kalmış sipariş görmez, denetim izinde ikisi de durur.
//   • Üç adımlı yol (Satın alma ekranındaki sipariş) olduğu gibi durur.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class MalGeldiDto
{
    public string? TedarikciCariId { get; set; }
    public string? KurSnapshotId { get; set; }
    public string? Aciklama { get; set; }
    public List<SatinalmaKalemDto>? Kalemler { get; set; }
    public Kuyumcu.Api.Modules.Masak.MasakBilgiDto? Masak { get; set; }
}

[ApiController]
[Authorize]
[Route("api/v1/satinalma/mal-geldi")]
[RolGerekli("SAHIP", "MUDUR")]   // mal kabulle AYNI yetki
public sealed class SatinalmaMalGeldiController(SatinalmaServisi servis, KuyumcuDbContext db) : ControllerBase
{
    /// <summary>POST /api/v1/satinalma/mal-geldi — sipariş + onay + teslim alma tek istekte.</summary>
    [HttpPost]
    public async Task<object> Kaydet([FromBody] MalGeldiDto dto)
    {
        var islem = AktifKullanici.FromClaims(User);
        if (string.IsNullOrWhiteSpace(dto.TedarikciCariId))
            throw new BadRequestException("Tedarikçi seçin").AlanIle("tedarikciCariId");
        if (string.IsNullOrWhiteSpace(dto.KurSnapshotId))
            throw new BadRequestException("Kur kaydı zorunlu: alış o günün has fiyatına bağlanır").AlanIle("kurSnapshotId");
        var kalemler = dto.Kalemler ?? [];
        if (kalemler.Count == 0)
            throw new BadRequestException("En az bir ürün satırı girin").AlanIle("kalemler");
        for (var i = 0; i < kalemler.Count; i++)
        {
            var k = kalemler[i];
            if (string.IsNullOrWhiteSpace(k.UrunId))
                throw new BadRequestException($"Satır {i + 1}: ürün seçin — stok girişi ürünsüz yapılamaz").AlanIle("kalemler");
            if ((k.Adet ?? 0m) <= 0m)
                throw new BadRequestException($"Satır {i + 1}: adet sıfırdan büyük olmalı").AlanIle("kalemler");
            if ((k.BirimFiyatTl ?? -1m) < 0m)
                throw new BadRequestException($"Satır {i + 1}: birim fiyat sıfır ya da daha büyük olmalı").AlanIle("kalemler");
        }

        var siparis = JsonSerializer.SerializeToElement(await servis.Olustur(new SatinalmaOlusturDto
        {
            TedarikciCariId = dto.TedarikciCariId, Aciklama = dto.Aciklama, Kalemler = kalemler,
        }, islem));
        var id = siparis.GetProperty("Id").GetString()!;
        try
        {
            var onayli = JsonSerializer.SerializeToElement(await servis.Onayla(id, islem));
            var kabul = onayli.GetProperty("kalemler").EnumerateArray()
                .Select(k => new MalKabulKalemDto { KalemId = k.GetProperty("Id").GetString(), Adet = k.GetProperty("Adet").GetDecimal() })
                .ToList();
            return await servis.MalKabul(id, new MalKabulDto
            {
                KurSnapshotId = dto.KurSnapshotId, Aciklama = dto.Aciklama, Kalemler = kabul, Masak = dto.Masak,
            }, islem);
        }
        catch (ApiException ex)
        {
            db.ChangeTracker.Clear();
            await servis.Iptal(id, $"Mal geldi tamamlanamadı: {ex.Message}", islem);
            throw;
        }
    }
}

