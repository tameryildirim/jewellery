using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Cari;

// ═════════════════════════════════════════════════════════════════════════════
// 18.10 (kurucu: "Faturalog kadar sade… para gelir gelmez fatura ekranına düşecek, müşteri hemen bulunacak,
// kullanıcı ürünü seçip satışını yapacak, faturaya direkt çevrilecek") — müşteri kartında EKSİK olan TC/VKN'yi
// tamamlar. Kuyumcu fatura haddi (36.000 TL) üstünde alıcının kimlik numarası zorunludur; kartta yoksa satış
// Kaydet'te durur. İki yol:
//   • Banka havalesi gönderenin TC'sini taşıyorsa ve bulunan müşterinin kartında TC yoksa SESSİZCE yazılır.
//   • Tezgâhta kilit çıkarsa tezgâhtar TC'yi yazar (POST /islemler/musteri-kimlik) ve satış devam eder.
// KURAL: yalnız BOŞ alan doldurulur — kayıtlı numara ASLA ezilmez; numara başka müşterideyse yazılmaz (bir kimlik
// tek müşteride, P-05). Yazıldıktan sonra yasal liste taraması (YaptirimServisi.CariTara) aynı kart açılışındaki gibi.
// ═════════════════════════════════════════════════════════════════════════════

public static class CariKimlikTamamlayici
{
    public const string Yazildi = "YAZILDI";
    public const string ZatenAyni = "ZATEN_AYNI";
    public const string KartindaBaskasi = "KARTINDA_BASKASI";
    public const string BaskaMusteride = "BASKA_MUSTERIDE";

    /// <summary>Kartı izlenen (tracked) olarak günceller; SaveChanges ÇAĞIRANDA. Döner: durum + (başka müşterideyse) sahibinin adı.</summary>
    public static async Task<(string Durum, string? Sahip)> Tamamla(KuyumcuDbContext db, string tenantId, string cariId, string no)
    {
        var cari = await db.Cariler.FirstOrDefaultAsync(c => c.Id == cariId && c.TenantId == tenantId)
            ?? throw new NotFoundException("Cari bulunamadı");
        if (cari.VergiNo == no) return (ZatenAyni, null);
        if (!string.IsNullOrEmpty(cari.VergiNo)) return (KartindaBaskasi, null);
        var sahip = await db.Cariler.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.VergiNo == no && c.Id != cariId)
            .Select(c => c.Ad).FirstOrDefaultAsync();
        if (sahip is not null) return (BaskaMusteride, sahip);
        cari.VergiNo = no;
        cari.KimlikTuru = KimlikNo.TurTuret(no);
        cari.Guncellendi = DateTime.UtcNow;
        return (Yazildi, null);
    }
}

public sealed class MusteriKimlikDto
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(36)]
    public string CariId { get; set; } = null!;
    [System.ComponentModel.DataAnnotations.MaxLength(20)] public string? KimlikNo { get; set; }
}

[ApiController]
[Authorize]
[Route("api/v1/islemler/musteri-kimlik")]
public sealed class MusteriKimlikController(KuyumcuDbContext db, DenetimServisi denetim) : ControllerBase
{
    /// <summary>POST /api/v1/islemler/musteri-kimlik — kartta EKSİK TC/VKN'yi yazar (satış yetkisi olan her rol).</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public async Task<object> Yaz([FromBody] MusteriKimlikDto dto)
    {
        var islem = AktifKullanici.FromClaims(User);
        var no = DogalAnahtar.Normalle(dto.KimlikNo);
        var tur = KimlikNo.TurTuret(no);
        if (no is null || tur is null || KimlikNo.Dogrula(no, tur) is not null)
            throw new BadRequestException("Geçerli bir TC kimlik ya da vergi numarası yazın").AlanIle("kimlikNo");
        if (await db.Cariler.AsNoTracking().AnyAsync(c => c.Id == dto.CariId && c.TenantId == islem.TenantId && c.SistemCarisi))
            throw new BadRequestException("Peşin Müşteri kartına kimlik yazılmaz; müşteriyi seçin ya da açın.").AlanIle("cariId");

        var (durum, sahip) = await CariKimlikTamamlayici.Tamamla(db, islem.TenantId, dto.CariId, no);
        if (durum == CariKimlikTamamlayici.KartindaBaskasi)
            throw new ConflictException("Bu müşterinin kartında başka bir kimlik numarası kayıtlı; kartı Müşteriler ekranından düzeltin.");
        if (durum == CariKimlikTamamlayici.BaskaMusteride)
            throw new ConflictException($"Bu kimlik numarası başka bir müşteride kayıtlı: {sahip}. O müşteriyi seçin.");
        var ad = await db.Cariler.AsNoTracking().Where(c => c.Id == dto.CariId && c.TenantId == islem.TenantId).Select(c => c.Ad).FirstAsync();
        if (durum == CariKimlikTamamlayici.Yazildi)
        {
            denetim.YazmaKaydet(islem, "Cari", dto.CariId, "tezgâhta eksik kimlik numarası yazıldı (fatura için)");
            await db.SaveChangesAsync();
            await Masak.YaptirimServisi.CariTara(db, islem.TenantId, dto.CariId, no, ad, islem);
        }
        return new { id = dto.CariId, ad, vergiNo = no };
    }
}
