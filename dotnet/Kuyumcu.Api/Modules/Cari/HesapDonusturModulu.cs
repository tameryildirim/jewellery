using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Cari;

// ═════════════════════════════════════════════════════════════════════════════
// D1-09 (18.10, rakip eşitleme: "hesap dönüştürme döviz ↔ has ↔ TL"). Müşterinin bir hesabındaki
// bakiyeyi başka birime çevirir (ör. 5 has borcu → 32.000 TL borcu). Yeni hareket türü YOK: tek kaynak
// cari düzeltme yoludur (CARI_DUZELTME; para el değiştirmez, MASAK/tevsik çalışmaz) — tek işlemde iki bacak.
//   • YÖN kaynak bakiyenin yönüdür (borç borç olarak, alacak alacak olarak taşınır); bakiyeden fazlası çevrilmez.
//   • ORAN VARSAYILMAZ: ekran güncel kurla hedef tutarı ÖNERİR, kuyumcu anlaştığı tutarı yazar; sunucu iki
//     tutarı ve çıkan oranı gerekçeye yazar.
//   • Kredi limiti uygulanmaz: dönüştürme yeni borç doğurmaz, mevcut borcu birim değiştirerek taşır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class HesapDonusturDto
{
    [Required] public string Kaynak { get; set; } = null!;
    [Required] public string Hedef { get; set; } = null!;
    [Range(typeof(decimal), "0", "9999999999999.99", ErrorMessage = "Değer çok büyük.", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal KaynakMiktar { get; set; }
    [Range(typeof(decimal), "0", "9999999999999.99", ErrorMessage = "Değer çok büyük.", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal HedefMiktar { get; set; }
    [Required] public string KurSnapshotId { get; set; } = null!;
}

public sealed class HesapDonusturServisi(KuyumcuDbContext db, CariServisi cari)
{
    public static readonly string[] Birimler = ["TRY", "HAS", "USD", "EUR", "GBP"];

    private static decimal Bakiye(CariBakiye? b, string birim, decimal emanetEtkisi) => b is null ? 0m : birim switch
    {
        "TRY" => b.TlBakiye,
        "HAS" => CariBakiyeKurali.EmanetsizHas(b.HasGramBakiye, emanetEtkisi),
        _ => DovizBakiye.Oku(b, birim),
    };

    private static int Hane(string birim) => birim == "HAS" ? 6 : 2;

    public async Task<object> Donustur(string cariId, HesapDonusturDto dto, AktifKullanici islem)
    {
        var kaynak = (dto.Kaynak ?? "").Trim().ToUpperInvariant();
        var hedef = (dto.Hedef ?? "").Trim().ToUpperInvariant();
        if (!Birimler.Contains(kaynak)) throw new BadRequestException("Kaynak hesap TRY, HAS, USD, EUR ya da GBP olmalıdır").AlanIle("kaynak");
        if (!Birimler.Contains(hedef)) throw new BadRequestException("Hedef hesap TRY, HAS, USD, EUR ya da GBP olmalıdır").AlanIle("hedef");
        if (kaynak == hedef) throw new BadRequestException("Kaynak ve hedef hesap aynı olamaz").AlanIle("hedef");
        var km = Math.Round(dto.KaynakMiktar, Hane(kaynak), MidpointRounding.AwayFromZero);
        var hm = Math.Round(dto.HedefMiktar, Hane(hedef), MidpointRounding.AwayFromZero);
        if (km <= 0m) throw new BadRequestException("Çevrilecek miktar sıfırdan büyük olmalıdır").AlanIle("kaynakMiktar");
        if (hm <= 0m) throw new BadRequestException("Karşılık miktarı sıfırdan büyük olmalıdır").AlanIle("hedefMiktar");

        var c = await db.Cariler.AsNoTracking().Where(x => x.Id == cariId && x.TenantId == islem.TenantId)
            .Select(x => new { x.SistemCarisi }).FirstOrDefaultAsync() ?? throw new NotFoundException("Cari bulunamadı");
        if (c.SistemCarisi) throw new BadRequestException("Peşin Müşteri'nin hesabı dönüştürülmez");
        var b = await db.CariBakiyeler.AsNoTracking().FirstOrDefaultAsync(x => x.CariId == cariId && x.TenantId == islem.TenantId);
        var emanet = b is null ? 0m : await CariBakiyeKurali.EmanetEtkisi(db, islem.TenantId, cariId);
        var mevcut = Bakiye(b, kaynak, emanet);
        if (mevcut == 0m) throw new BadRequestException("Kaynak hesapta bakiye yok").AlanIle("kaynak");
        if (km > Math.Abs(mevcut)) throw new BadRequestException("Çevrilecek miktar bakiyeden büyük olamaz").AlanIle("kaynakMiktar");

        var yon = Math.Sign(mevcut);   // + bize borçlu · − biz borçluyuz — taşınan yön AYNI
        var oran = (hm / km).ToString("0.######", CultureInfo.InvariantCulture);
        var gerekce = string.Create(CultureInfo.InvariantCulture,
            $"Hesap dönüştürme: {km} {kaynak} → {hm} {hedef} (oran {oran} {hedef}/{kaynak})");

        // Bacaklar: TL ve HAS tek fişte; döviz ayrı fiş (döviz bacağı TL tutarı taşımaz).
        decimal tl = 0m, has = 0m; var dovizler = new List<(string Pb, decimal Delta)>();
        void Ekle(string birim, decimal delta)
        {
            if (birim == "TRY") tl += delta;
            else if (birim == "HAS") has += delta;
            else dovizler.Add((birim, delta));
        }
        Ekle(kaynak, -yon * km);
        Ekle(hedef, yon * hm);

        await using var tx = await db.Database.IsBaslatVeyaDevral();
        if (tl != 0m || has != 0m)
            await cari.Duzeltme(cariId, new CariDuzeltmeDto { TlDelta = tl, HasDelta = has, Gerekce = gerekce, KurSnapshotId = dto.KurSnapshotId },
                islem, krediLimitiUygula: false);
        foreach (var (pb, delta) in dovizler)
            await cari.Duzeltme(cariId, new CariDuzeltmeDto { DovizParaBirimi = pb, DovizDelta = delta, Gerekce = gerekce, KurSnapshotId = dto.KurSnapshotId },
                islem, krediLimitiUygula: false);
        if (tx is not null) await tx.CommitAsync();
        return await cari.Getir(cariId, islem);
    }
}

[ApiController]
[Authorize]
[Route("api/v1/cari")]
[Yetki.ModulYetkisi("cari")]
public sealed class HesapDonusturController(HesapDonusturServisi servis) : ControllerBase
{
    /// <summary>POST /api/v1/cari/{id}/hesap-donustur — bakiyeyi başka birime çevirir (düzeltmeyle aynı rol kapısı).</summary>
    [HttpPost("{id}/hesap-donustur")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Donustur(string id, [FromBody] HesapDonusturDto dto) => servis.Donustur(id, dto, AktifKullanici.FromClaims(User));
}
