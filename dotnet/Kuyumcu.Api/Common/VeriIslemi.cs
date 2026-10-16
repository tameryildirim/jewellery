using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kuyumcu.Api.Common;

// ═════════════════════════════════════════════════════════════════════════════
// TRANSACTION BAĞLAMI DEVRALMA — paket 47 Faz A.
//
// SORUN (canlı ölçüldü, EF Core 8 / Pomelo): `db.Database.BeginTransactionAsync()`
// açık bir transaction varken InvalidOperationException fırlatır
// ("The connection is already in a transaction..."). Yani kendi transaction'ını
// açan bir servisi (CariServisi.Olustur) dış bir transaction içinden çağırmak
// MÜMKÜN DEĞİLDİ.
//
// Paket 47'nin değişmezi "bir oturumun TÜM satırları TEK transaction'da
// uygulanır; biri patlarsa hepsi geri alınır" olduğu için bu bir tıkanmaydı.
// Üç seçenek vardı:
//   (a) İçe aktarımın kendi Add'ini yazmak → FAIL-CLOSED 3'ün ihlali
//       (şifreleme, cari bakiye satırı, TasKapisi, barkod üretimi atlanırdı).
//   (b) Satır satır commit → KISMİ GÖÇ, "en kötü hâl".
//   (c) Servisin transaction'ı VARSA DEVRALMASI.
// (c) seçildi: davranış tek başına çağrıldığında AYNIDIR (transaction yoksa
// açılır ve commit edilir), dış transaction varsa ona katılır ve commit'i
// SAHİBİNE bırakır.
// ═════════════════════════════════════════════════════════════════════════════

public static class VeriIslemi
{
    /// <summary>
    /// Açık transaction yoksa yenisini başlatır; VARSA <c>null</c> döner —
    /// çağıran <c>null</c> ise commit ETMEZ, sahibine bırakır.
    /// <para>
    /// Kullanım: <c>await using var tx = await db.Database.IsBaslatVeyaDevral();
    /// … if (tx is not null) await tx.CommitAsync();</c>
    /// </para>
    /// </summary>
    public static async Task<IDbContextTransaction?> IsBaslatVeyaDevral(this DatabaseFacade db) =>
        db.CurrentTransaction is null ? await db.BeginTransactionAsync() : null;

    /// <summary>
    /// PAKET URUN-ACILIS-ATOMIK — reddedilen bir adımın İZLEYİCİDE bıraktığı yarım
    /// değişiklikleri geri alır: eklenen varlıklar ayrılır, değişen/silinen varlıklar
    /// özgün değerlerine döner. Reddi yakalayıp AYNI DbContext ile SaveChanges'e devam
    /// eden döngüler (ekstre toplu onayı) için zorunludur; aksi hâlde reddedilen
    /// adımın Add'i bir sonraki SaveChanges ile veritabanına sızar.
    /// <paramref name="haric"/> varlıkları (ör. HATALI işaretlenecek satır) dokunulmaz.
    /// </summary>
    public static void BekleyenleriGeriAl(this ChangeTracker izleyici, params object[] haric)
    {
        foreach (var e in izleyici.Entries().ToList())
        {
            if (haric.Any(h => ReferenceEquals(h, e.Entity))) continue;
            switch (e.State)
            {
                case EntityState.Added:
                    e.State = EntityState.Detached;
                    break;
                case EntityState.Modified:
                case EntityState.Deleted:
                    e.CurrentValues.SetValues(e.OriginalValues);
                    e.State = EntityState.Unchanged;
                    break;
            }
        }
    }
}
