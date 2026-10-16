using System.Globalization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Satis;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ISLEM-BANT (09.10) — işlem içi elle yazılan HAS GRAM FİYATININ makul bandı.
// Kaynak: KUR-BANT § 0.6 devri (YÜKSEK, VERİ). Kural Core'da (FiyatBandi.IslemDenetle);
// bu kapı yalnız referansı (o anki GEÇERLİ has fiyatı) okur ve kur ekranıyla AYNI onay
// kalıbını uygular (KurServisi.ManuelKurKaydetIc):
//   · Mutlak sınır dışı (has 500–50.000 TL) → SERT 400 (onay/rol/gerekçe ne olursa olsun).
//   · Oran bandı dışı → onay yoksa 400 + alan + sözlük metni; onay var ama rol SAHİP/MÜDÜR
//     değil → 403; gerekçe K30 (≥ 20) → denetim izi (hassas) çağıranın SaveChanges'i ile.
// Çağrı yerleri (her biri TEK satır): SatisServisi.HurdaAlis, HurdaSatis, Takas ve satış
// fişindeki eski altın bacağı. Satış modülünün geri kalanına dokunulmaz (t5 şeridi).
// ═══════════════════════════════════════════════════════════════════════════
public static class IslemFiyatKapisi
{
    /// <summary>Denetim izinin varlık türü (SQL kanıtı ve testler bu adla süzer).</summary>
    public const string DenetimVarlikTuru = "IslemFiyatBantAsimi";

    /// <summary>
    /// O anki GEÇERLİ has fiyatı: kiracının iptal edilmemiş en son kur kaydı (has &gt; 0).
    /// Kaydı yoksa null (yalnız mutlak sınır uygulanır). Ekrandaki canlı kur da bu kayıttır.
    /// </summary>
    public static async Task<(string Id, decimal Has)?> Referans(KuyumcuDbContext db, string tenantId)
    {
        var son = await db.KurSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenantId && !s.IptalEdildi && s.AltinHasFiyati > 0m)
            .OrderByDescending(s => s.Olusturuldu)
            .Select(s => new { s.Id, s.AltinHasFiyati })
            .FirstOrDefaultAsync();
        return son is null ? null : (son.Id, son.AltinHasFiyati);
    }

    /// <summary>
    /// Bant kapısı. <paramref name="islem"/> null ise (kullanıcısız iç yol) bant aşımı ONAYLANAMAZ (fail-closed).
    /// </summary>
    public static async Task Denetle(
        KuyumcuDbContext db, AktifKullanici? islem, string tenantId, IslemFiyatBaglami baglam,
        string? maden, decimal? fiyat, string alan, bool? bantOnay, string? bantGerekce)
    {
        if (fiyat is not { } x || x <= 0m || maden != Kuyumcu.Core.Domain.Maden.Altin) return;
        var referans = await Referans(db, tenantId);
        if (FiyatBandi.IslemDenetle(x, baglam, referans?.Has, maden) is not { } h) return;

        // Mutlak sınır SERT kapıdır; oran bandı da onaysız geçmez — ikisi de 400 + alan + metin.
        if (!FiyatBandi.OnaylanabilirMi(h) || bantOnay != true)
            GirdiKapisi.Denetle(h, alan);
        if (islem is null || !Kur.KurServisi.BantOnayYetkili(islem.Rol))
            throw new ForbiddenException(KurBantSozlugu.Al(IstekDili.Gecerli).YetkiYok);   // 403: yalnız SAHİP/MÜDÜR
        var gerekce = GirdiKapisi.Gerekce(bantGerekce, "bantGerekce");                      // K30: ≥ 20 karakter

        var (a, u) = FiyatBandi.IslemBandi(baglam);
        var detay = string.Create(CultureInfo.InvariantCulture,
            $"{baglam} has gram fiyatı MAKUL BANT DIŞI onayla kaydedildi · {alan}={x} " +
            $"(güncel has {referans!.Value.Has}, bant %{a}–%{u}) · gerekçe: {gerekce}");
        // DenetimServisi çağrılır, yeniden yazılmaz; HttpContextAccessor AsyncLocal'dir — istek içindeyse IP/damga gelir.
        new DenetimServisi(db, new HttpContextAccessor())
            .YazmaKaydet(islem, DenetimVarlikTuru, referans.Value.Id, detay, hassas: true);
    }
}
