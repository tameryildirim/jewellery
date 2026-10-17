using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Satis;

// ═════════════════════════════════════════════════════════════════════════════
// M6b (kurucu 16.10: "TC'yi yazın, girdiğim anda fatura kesilsin"; 17.10: "müşteri listesinden otomatik bulsun,
// yoksa otomatik kaydetsin") — tezgâhta TC/VKN ile müşteri BUL, yoksa AÇ. Banka tarafındaki çözümle (GelenOdemeServisi.Coz)
// aynı sıra: önce kimlik no; bulunursa o müşteri. Yoksa ad + kimlik no ile MUSTERI açılır (CariServisi.OlusturId —
// benzersizlik, ad kuralı, denetim izi aynı kapıdan).
//   • Tezgâhtar da kullanır (POST /cari MUHASEBE+ olduğu için tezgâhta müşteri açılamıyordu); dar kapsam: yalnız
//     ad + kimlik no, tür MUSTERI. Diğer alanlar Müşteriler ekranından düzenlenir.
//   • Kimlik no kural dışıysa 400 (TCKN kontrol haneleri; 11111111111 geçmez). Peşin Müşteri hiç dönmez.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TezgahMusteriAcDto
{
    [System.ComponentModel.DataAnnotations.MaxLength(20)] public string? KimlikNo { get; set; }
    /// <summary>Cari kartıyla AYNI ad kuralı (en az 2 karakter, en az bir harf — K19).</summary>
    [System.ComponentModel.DataAnnotations.MaxLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.CariAd)]
    [Kuyumcu.Api.Common.Dogrulama.Ad(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.CariAd)]
    public string? Ad { get; set; }
}

[ApiController]
[Authorize]
[Route("api/v1/islemler/musteri-ac")]
public sealed class TezgahMusteriAcController(KuyumcuDbContext db, Cari.CariServisi cari) : ControllerBase
{
    /// <summary>POST /api/v1/islemler/musteri-ac — { id, ad, yeni }. Satış yetkisi olan her rol.</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public async Task<object> Ac([FromBody] TezgahMusteriAcDto dto)
    {
        var islem = AktifKullanici.FromClaims(User);
        var no = DogalAnahtar.Normalle(dto.KimlikNo);
        var tur = KimlikNo.TurTuret(no);
        if (no is null || tur is null || KimlikNo.Dogrula(no, tur) is not null)
            throw new BadRequestException("Geçerli bir TC kimlik ya da vergi numarası yazın").AlanIle("kimlikNo");

        var mevcut = await db.Cariler.AsNoTracking()
            .Where(c => c.TenantId == islem.TenantId && c.VergiNo == no && !c.SistemCarisi)
            .OrderBy(c => c.Id).Select(c => new { c.Id, c.Ad }).FirstOrDefaultAsync();
        if (mevcut is not null) return new { id = mevcut.Id, ad = mevcut.Ad, yeni = false };

        var ad = dto.Ad?.Trim();
        if (string.IsNullOrEmpty(ad))
            throw new BadRequestException("Bu numarayla kayıtlı müşteri yok; yeni müşteri için ad soyad yazın").AlanIle("ad");
        var id = await cari.OlusturId(new Cari.CariOlusturDto { Tur = Cari.SistemCarisi.CariTuru, Ad = ad, VergiNo = no, KimlikTuru = tur }, islem);
        return new { id, ad, yeni = true };
    }
}
