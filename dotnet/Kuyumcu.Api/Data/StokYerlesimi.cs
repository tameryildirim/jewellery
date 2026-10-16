using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// STOK YERLEŞİMİ — paket 25 Faz A (reyon/vitrin bazlı stok bölümlemesi)
//
// ⚠ TEK GERÇEK KAYNAĞI DEĞİŞMEDİ: şubedeki stok adedinin tek gerçeği hâlâ
//   `stoklar` tablosudur (Entities.cs → Stok). Bu tablo o adedin ŞUBE İÇİ
//   KIRILIMIDIR — alt bölümlemedir, ikinci bir stok defteri DEĞİLDİR.
//
// DEĞİŞMEZ I1:  Σ stok_yerlesimleri.adet ≤ stoklar.adet
//   Aradaki fark SAKLANMAZ, HESAPLANIR ve adı "ATANMAMIŞ"tır: fiziksel yeri
//   henüz bildirilmemiş mal. Atanmamışı bir satır olarak tutmak iki gerçek
//   üretirdi (hem stoklar hem yerleşim tablosu "toplam" iddia ederdi); sapma
//   çıktığında hangisinin doğru olduğunun cevabı olmazdı.
//
// ADR-0016 (bu paketle yazıldı): reyon yerleşimi DEFTER KAYDI DEĞİLDİR.
//   Reyon içi taşımada para el değiştirmez, borç doğmaz, has miktarı ve toplam
//   adet değişmez — bu yüzden HareketFabrikasi ile deftere satır YAZILMAZ.
//   İz `denetim_kayitlari`na (append-only, ADR-0004) yazılır: kim, ne zaman,
//   hangi reyondan hangi reyona, kaç adet.
//
// EŞZAMANLILIK (Mehmet + Aşkın koşulu): `Guncellendi` alanı [ConcurrencyCheck]
//   taşır. İki satış aynı yerleşim satırını aynı anda düşürürse ikinci
//   SaveChanges DbUpdateConcurrencyException atar ve istek 409 ile döner —
//   sessiz aşırı-düşüm olamaz. ExecuteUpdate/ExecuteDelete KULLANILMAZ
//   (denetim.sh beyaz listesi 4'te kalır).
//
// ÖLÇEK: `adet` decimal(12,3) — `stoklar.adet` ile AYNI ölçek. A15 sözleşmesi
//   gereği GRAM birimli üründe 1 adet = 1 gramdır; ayrı bir has yolu açılmaz.
// ═════════════════════════════════════════════════════════════════════════════

[Table("stok_yerlesimleri")]
public class StokYerlesimi : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;
    [Column("urun_id"), MaxLength(36)] public string UrunId { get; set; } = null!;
    [Column("reyon_id"), MaxLength(36)] public string ReyonId { get; set; } = null!;

    /// <summary>Bu reyonda duran adet (GRAM üründe gram). Sıfıra düşen satır SİLİNİR.</summary>
    [Column("adet", TypeName = "decimal(12,3)")] public decimal Adet { get; set; }

    /// <summary>İyimser kilit damgası — eşzamanlı düşümde ikinci yazma reddedilir.</summary>
    [ConcurrencyCheck]
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}
