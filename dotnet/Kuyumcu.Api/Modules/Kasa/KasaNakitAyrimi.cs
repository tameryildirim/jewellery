using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Kasa;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO (madde 1, YÜKSEK) — GÜN SONU SAYIMINDA NAKİT DEFTERİ
//
// SORUN (uzun demo kaydı 01.10, BULGU-1): POS cihazı / banka hesabı tanımlı
// olmayan dükkânda KART ve HAVALE tahsilatı da kasaya yazılıyor (satış ve
// tahsilat sözleşmesi: "HAVALE de kasaya yazılır — banka bacağı bu pakette
// AÇILMAZ"). Günü Kapat ise çekmeceden SAYILAN NAKDİ kasanın TOPLAM bakiyesiyle
// (nakit + kart + havale) karşılaştırıyordu → tezgâhtar kuruşu kuruşuna saysa
// bile kart + havale kadar SAHTE AÇIK görüyordu.
//
// KARAR (en küçük doğru değişiklik — yeni muhasebe kavramı İCAT EDİLMEDİ):
//   • Defter, kasa bakiyesi ve hareketler DEĞİŞMEZ; geri dönük veri düzeltmesi YOK.
//   • Sayım, kasanın NAKİT payıyla karşılaştırılır:
//       nakit defteri = kasa.bakiye − Σ(kart hareketleri) − Σ(havale hareketleri)
//     Ayrım OKUMA TARAFINDA, kasa_hareketleri.odeme_turu kolonundan yapılır
//     (eski kayıtlar da bu kolonu taşıdığı için geriye dönük doğru okunur).
//   • Nakit dışı sayılan araçlar YALNIZ kart ve havaledir. Açılış devri (DIGER),
//     gider/virman/evrak (NAKIT) ve döviz (DOVIZ) satırları bugünkü gibi nakit
//     sayılır — bilinmeyen bir aracı "nakit değil" diye çıkarmak sayımı
//     sessizce gevşetirdi (fail-closed: şüphede bugünkü davranış).
//   • Kart ve havale ekranda AYRI satır olarak gösterilir (sayıma girmez).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Kasanın gün sonu sayımı için ayrımı: toplam = nakit + kart + havale.</summary>
public sealed record KasaNakitAyrimiSonuc(decimal ToplamBakiye, decimal Kart, decimal Havale, decimal Nakit);

public static class KasaNakitAyrimi
{
    /// <summary>Çekmecede SAYILAMAYAN araçlar — kapalı liste (sihirli dize yazılmaz).</summary>
    public static readonly string[] NakitDisiAraclar = [OdemeAraci.Kart, OdemeAraci.Havale];

    /// <summary>
    /// Kasanın nakit/kart/havale ayrımını hareketlerden okur. Kiracı şartı global
    /// süzgece EK olarak elle de yazılır.
    /// </summary>
    public static Task<KasaNakitAyrimiSonuc> Hesapla(KuyumcuDbContext db, string tenantId, Data.Kasa kasa)
        => Hesapla(db, tenantId, kasa.Id, kasa.Bakiye);

    /// <summary>Aynı ayrım, kasa kimliği + güncel bakiyeyle (virman kapısı).</summary>
    public static async Task<KasaNakitAyrimiSonuc> Hesapla(KuyumcuDbContext db, string tenantId, string kasaId, decimal bakiye)
    {
        var toplamlar = await db.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.KasaId == kasaId
                     && (h.OdemeTuru == OdemeAraci.Kart || h.OdemeTuru == OdemeAraci.Havale))
            .GroupBy(h => h.OdemeTuru)
            .Select(g => new { Tur = g.Key, Toplam = g.Sum(x => x.Tutar) })
            .ToListAsync();

        // PARA-DEMO-2 (madde 3): aynı istekte henüz kaydedilmemiş (Added) kart/havale satırları da
        // sayılır — kasa.Bakiye bellekte onları zaten içerir; DB toplamı içermez. Yalnız Added:
        // Unchanged satır DB toplamındadır, ikinci kez sayılmaz.
        var bekleyen = db.ChangeTracker.Entries<KasaHareket>()
            .Where(e => e.State == EntityState.Added && e.Entity.TenantId == tenantId && e.Entity.KasaId == kasaId
                     && (e.Entity.OdemeTuru == OdemeAraci.Kart || e.Entity.OdemeTuru == OdemeAraci.Havale))
            .Select(e => e.Entity).ToList();
        var kart = Yuvarla(toplamlar.Where(x => x.Tur == OdemeAraci.Kart).Sum(x => x.Toplam)
                           + bekleyen.Where(x => x.OdemeTuru == OdemeAraci.Kart).Sum(x => x.Tutar));
        var havale = Yuvarla(toplamlar.Where(x => x.Tur == OdemeAraci.Havale).Sum(x => x.Toplam)
                             + bekleyen.Where(x => x.OdemeTuru == OdemeAraci.Havale).Sum(x => x.Tutar));
        return new KasaNakitAyrimiSonuc(bakiye, kart, havale, Yuvarla(bakiye - kart - havale));
    }

    private static decimal Yuvarla(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);
}
