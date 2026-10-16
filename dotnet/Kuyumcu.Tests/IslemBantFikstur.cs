using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ISLEM-BANT (09.10) — FİKSTÜR ETKİSİ (KUR-BANT § 0.6: "~40 test fikstürü hurda/takas/
// eski altın fiyatı olarak 1.000–3.000 TL/has g kullanıyor; fikstür kur 6.827,20 → bant onları
// kırar; fikstür güncellemesi aynı pakette").
// Seçilen yol: testin TUTARLARINA dokunmadan (MASAK/tevsik/kasa eşikleri o tutarlara bağlı)
// kiracıya o testin fiyatlarıyla TUTARLI, DAHA YENİ ve GEÇERLİ bir kur kaydı eklenir. İşlem
// içi bandın referansı "o anki geçerli has fiyatı" = son iptal edilmemiş kur kaydıdır
// (IslemFiyatKapisi.Referans); testin işlem gövdesindeki kurSnapshotId (k.KurId) DEĞİŞMEZ.
// Bant kuralının KENDİSİ IslemBant*Testleri'nde 6.827,20 referansıyla sınanır.
// ═══════════════════════════════════════════════════════════════════════════
public static class IslemBantFikstur
{
    /// <summary>Hurda/takas fikstürlerinin (2.000–3.000 TL/has g) bant içinde kaldığı referans: %50–%110 → 2.546–5.600; ± %20 → 2.417–3.625.</summary>
    public const decimal VarsayilanReferans = 3_000m;

    /// <summary>
    /// Kiracıya, fikstür fiyatlarını bant içinde tutan daha yeni bir GEÇERLİ kur kaydı ekler (id döner).
    /// <paramref name="has"/> testin fiyatlarına göre seçilir (ör. 1.000 TL fiyat için 1.800).
    /// </summary>
    public static string ReferansKur(KuyumcuDbContext db, string tenantId, decimal has = VarsayilanReferans)
    {
        var id = Kimlik.Ulid();
        db.KurSnapshots.Add(new KurSnapshot
        {
            Id = id, TenantId = tenantId, AltinHasFiyati = has,
            Kaynak = "MANUAL", Olusturuldu = DateTime.UtcNow.AddSeconds(1),
        });
        db.SaveChanges();
        return id;
    }

    /// <summary><see cref="ReferansKur(KuyumcuDbContext,string,decimal)"/>'un kiracı kısayolu; kiracıyı aynen döndürür (zincirleme kurulum için).</summary>
    public static Kiraci ReferansKur(KuyumcuDbContext db, Kiraci k, decimal has = VarsayilanReferans)
    {
        ReferansKur(db, k.TenantId, has);
        return k;
    }
}
