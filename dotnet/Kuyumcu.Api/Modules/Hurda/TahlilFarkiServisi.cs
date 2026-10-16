using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Sayfalar.Hurda;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Hurda;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-20b (Y-10 kalan dilim · K-13/2) — TAHLİL FARKI DÜZELTME YOLU
//
// ⚠ NEDEN VAR (UX-1 / UX-S25): tahlil raporu çoğu zaman sevkiyattan SONRA
//   gelir ve bir kez daha düzelebilir. "Fark yanlış girildi" hâlinde tek çare
//   belgeyi iptal etmek olsaydı, hurda satışının fatura/mutabakat bacağı da
//   yanardı — defterde yalan, seride boşluk. Bu yüzden defter APPEND-ONLY
//   kalır: satırın üzerine yazılmaz, AYNI hurda satışına İKİNCİ bir fark
//   bacağı eklenir ve toplam fark bacakların toplamı olur.
//
// ⚠ NEDEN SatisServisi'nde DEĞİL: düzeltme bacağının TL, kasa, cari, MASAK ve
//   tevsik bacağı YOKTUR (TlTutari = 0). SatisServisi'ne konsaydı, oradaki
//   ödeme/kur/MASAK iskelesi para hareketi olmayan bir olay için taşınırdı.
//   Deftere yazma yine TEK KAPIDAN (HareketFabrikasi → TahlilFarkiFabrikasi)
//   geçer; ikinci bir yazma motoru açılmaz.
//
// ⚠ NEDEN TL DÜZELTİLMEZ: alıcının ödediği tutar KABUL EDİLEN hasla bağlıdır
//   ve o bacak fatura/tahsilat/MASAK kümülatifine girmiştir. Tahsilat tarafı
//   sonradan değişiyorsa doğru yol cari düzeltme fişidir
//   (api/v1/cari/{id}/duzeltme) — bu uç has defterini düzeltir, parayı DEĞİL.
//   Bu sınır raporda "devredildi" olarak yazılıdır (kapsam kilidi).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TahlilDuzeltmeDto
{
    /// <summary>
    /// EK fark (İŞARETLİ has gram): negatif = ALEYHTE (elde kalan hurda azalır),
    /// pozitif = LEHTE. 0 kabul edilmez — fabrika reddeder.
    /// </summary>
    // PAKET GOC-A (envanter C · hrd-ek-fark): üst sınır ve 6 ondalık yoktu (7+ ondalık sessizce yuvarlanıyordu).
    // İşaretli has: |değer| ≤ decimal(15,6) tavanı; ondalık ve sıfır kuralı servis içinde (GirdiKapisi.IsaretliHas).
    [Range(-999_999_999.999999, 999_999_999.999999)]
    public decimal EkFarkHasGrami { get; set; }

    /// <summary>
    /// Düzeltmenin gerekçesi — ZORUNLU (K-13 şartı). Evin standardı 20 karakter.
    /// ⚠ DataAnnotations YALNIZ controller yolunda çalışır; servis ayrıca
    ///   fabrikada elle doğrular.
    /// </summary>
    [Required, MinLength(20), MaxLength(1000)] public string Gerekce { get; set; } = null!;
}

public sealed class TahlilFarkiServisi(KuyumcuDbContext db, DenetimServisi denetim)
{
    /// <summary>Denetim izinde görünecek varlık adı.</summary>
    public const string Varlik = "TahlilFarki";

    public async Task<TahlilDuzeltmeSonucu> Duzelt(
        string hurdaSatisId, TahlilDuzeltmeDto dto, AktifKullanici islem)
    {
        if (string.IsNullOrWhiteSpace(hurdaSatisId))
            throw new BadRequestException("Düzeltilecek hurda satışının kimliği zorunludur.");

        islem = await HurdaStokKapisi.OturumSubesiCoz(db, islem);   // ZO-ISLEM-A

        // ⚠ Kaynak satır TEK sorgudan okunur (HurdaStokKapisi.Sorgu): kiracı
        //   global filtreden, şube P-04 süzgecinden gelir. Elle yazılsaydı bir
        //   şubenin farkı diğerinin satışına bağlanabilirdi.
        var kaynak = await HurdaStokKapisi.Sorgu(db, islem)
            .FirstOrDefaultAsync(h => h.Id == hurdaSatisId
                                   && h.Tur == TahlilFarkiFabrikasi.KaynakHareketTuru)
            ?? throw new NotFoundException(
                $"Hurda satışı bulunamadı: {hurdaSatisId}. Tahlil farkı yalnız bu şubedeki bir "
                + "hurda satışına yazılabilir.");

        // PAKET GOC-A: 7+ ondalık SESSİZCE YUVARLANMAZ — K11 + K9b (işaretli has) Core kuralı, sözlük metni.
        var ek = GirdiKapisi.IsaretliHas(dto.EkFarkHasGrami, "ekFarkHasGrami");

        // ALEYHTE düzeltme elde kalan hurdayı AZALTIR: bakiyeyi eksiye düşüren
        // bir düzeltme, olmayan hurdanın satılmasıyla aynı sonucu verirdi.
        // Aynı fail-closed kapı kullanılır — ikinci bir kapı açılmaz.
        if (ek < 0m)
            await HurdaStokKapisi.DogrulaCikis(db, islem, kaynak.Maden, -ek);

        var now = DateTime.UtcNow;
        var duzeltme = TahlilFarkiFabrikasi.Ekle(db, kaynak, ek, dto.Gerekce, islem.Id, now);

        // ⚠ Denetim kaydı YENİDEN YAZILMAZ, mevcut servis ÇAĞRILIR; SaveChanges
        //   çağırmaz; aşağıdaki TEK kayıt çağrısıyla aynı işlemde yazılır.
        denetim.YazmaKaydet(islem, Varlik, duzeltme.Id,
            $"tahlil farkı düzeltmesi · hurda satışı {hurdaSatisId} · ek fark {ek:0.######} has gram "
            + $"({kaynak.Maden}) · gerekçe: {dto.Gerekce.Trim()}",
            hassas: false);

        await db.SaveChangesAsync();

        var toplamFark = await db.Hareketler.AsNoTracking()
            .Where(h => h.TahlilKaynakHareketId == hurdaSatisId)
            .SumAsync(h => (decimal?)h.HasGrami) ?? 0m;

        return new TahlilDuzeltmeSonucu(
            hurdaSatisId,
            duzeltme.Id,
            kaynak.Maden,
            Kuyumcu.Core.Domain.Maden.Ad(kaynak.Maden),
            ek,
            Math.Round(toplamFark, 6, MidpointRounding.AwayFromZero),
            await HurdaStokKapisi.Bakiye(db, islem, kaynak.Maden),
            now);
    }
}

/// <summary>
/// PAKET P-20b — tahlil farkı düzeltme ucu. Yetki hurda satışıyla AYNIDIR
/// (aynı defter hattı, aynı sorumluluk): MUHASEBE ve üstü.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/islemler")]
[Yetki.ModulYetkisi("islem")]
public sealed class TahlilFarkiController(TahlilFarkiServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>
    /// POST /api/v1/islemler/hurda-satis/{hareketId}/tahlil-duzeltme —
    /// aynı hurda satışına EK tahlil farkı bacağı yazar (üzerine yazmaz).
    /// </summary>
    [HttpPost("hurda-satis/{hareketId}/tahlil-duzeltme")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<TahlilDuzeltmeSonucu> TahlilDuzeltme(
        string hareketId, [FromBody] TahlilDuzeltmeDto dto) => servis.Duzelt(hareketId, dto, Islem);
}
