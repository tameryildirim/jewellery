using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// Kiracı filtresini aşmanın TEK meşru kapısı — ADIM1-KARAR §3/Ç1.
//
// `IgnoreQueryFilters` BİLİNÇLİ OLARAK KULLANILMAZ. Gerekçe (Bertan, mühürlendi):
//   • çağrı başına hatırlanması gerekir; unutulan tek sorgu sessizce yanlış
//     kapsamda çalışır,
//   • bir sorguda kullanıldığında TÜM sorgu ağacına yayılır,
//   • denetlenemez: "bu 40 sorgudan hangisi bilerek filtresiz?" sorusunun kod
//     üzerinde cevabı olmaz.
// Bunun yerine filtresiz erişim tek sınıfa hapsedilmiştir; kod tabanında
// `IgnoreQueryFilters` sayısı 0 kalır ve CI bunu denetler.
//
// İZİN VERİLEN KULLANIM — tam liste (başka yok):
//   1. KurZamanlayici → aktif kiracı listesi                      [Sistem]
//   2. KurZamanlayici → kiracı başına kur yazımı                  [Kiraci(id)]
//   3. KurZamanlayici.EskiKurlariTemizle                          [Sistem]
//   4. AuthServisi kimlik-öncesi yollar: Kayit/Giris/Yenile/
//      Cikis/TumTokenlerIptalEt                                   [Sistem]
//   5. IsZamanlayici → aktif kiracı listesi                       [Sistem]
//   6. IsZamanlayici → kiracı başına iş koşumu                    [Kiraci(id)]
//   7. SistemServisi → kiracı listesi ve süre/durum yönetimi      [Sistem]
//   8. SistemServisi → kiracı-ÖNCESİ davet tablosu (kiraci_davetleri)
//                                                                 [Sistem]
//   9. YasalParametreServisi → kiracı-ÜSTÜ yasal_parametreler tablosu
//      (önbellek yükleme + sistem anahtarlı yazma; PAKET YASAL-1) [Sistem]
//      Ek (PAKET YASAL-PARAM-EKRAN): sistem anahtarlı ÖNİZLEME ucu, değişikliğin
//      eski–yeni değer bandına düşen AÇIK sipariş SAYISINI (tüm kiracılar, yalnız
//      COUNT — satır/kişisel veri okunmaz, dönmez) onay penceresine yazar.
//  10. YASAL KÜMÜLATİF OKUYUCU (tevsik/MASAK) — kiracıya SABİT, şube süzgeçsiz,
//      SALT OKUMA (PAKET ŞUBE-KAPSAM-A § 2.6; ADR-0002 ek not 2026-09-30)
//      Yalnız <see cref="YasalKumulatifOku"/> üzerinden; yalnız çağıranın KENDİ
//      kiracısı için (başka kiracı → istisna). Mevzuat: tevsik "aynı gün aynı
//      kişi" mükellef geneli, MASAK "bağlantılı işlemler toplamı" yükümlü geneli;
//      şube yükümlünün birimidir (MASAK GT 34 md.3/1-c). Şubeye kilitli rolde
//      P-04 süzgeci kümülatifi şubeye bölüyordu → kapı delinebiliyordu.     [Kiraci(id)]
//   `AuthServisi.Ben` dahil JWT-SONRASI her şey istek bağlamında kalır.
//
// ⚠ 7-8 (paket F-2-min, mühürlü koşul): bu iki kullanım YAPILANDIRMA SIRRI ile
//   korunan /api/v1/sistem/* uçlarındadır (SistemAnahtariKapisi). JWT ile
//   erişilemezler; kiracı bağlamı bilinmediği için değil, kiracı-ÜSTÜ oldukları
//   için filtresizdirler.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Kiracı filtresini aşan ya da belirli bir kiracıya sabitlenen DI kapsamı üretir.</summary>
public sealed class SistemKapsami(IServiceScopeFactory fabrika)
{
    /// <summary>Filtresiz kapsam — yalnız yukarıdaki listedeki [Sistem] maddeleri için.</summary>
    public AsyncServiceScope Sistem() => Ac(new SistemKiraciBaglami());

    /// <summary>Belirli bir kiracıya sabitlenmiş kapsam (arka plan işleri).</summary>
    public AsyncServiceScope Kiraci(string kiraciId) => Ac(new SabitKiraciBaglami(kiraciId));

    /// <summary>
    /// Madde 10 — YASAL KÜMÜLATİF OKUYUCU. Şube süzgeci KAPALI bağlamda (tüm-şube
    /// rolü, arka plan işi, test) sorgu verilen <paramref name="db"/> ile aynen koşar.
    /// Şube süzgeci AÇIK bağlamda (şubeye kilitli rol) aynı kiracıya sabit, şube
    /// süzgeçsiz bir kapsam açılır ve sorgu orada SALT OKUMA koşar.
    /// <para>
    /// FAIL-CLOSED: (1) istenen kiracı oturumun kiracısı değilse, (2) kapsam
    /// çözülemezse, (3) açılan kapsamda şube süzgeci yine açıksa, (4) okuyucu
    /// bir değişiklik izlediyse istisna atılır — kümülatif hiçbir koşulda
    /// sessizce şube toplamına düşmez.
    /// </para>
    /// </summary>
    public static async Task<T> YasalKumulatifOku<T>(
        KuyumcuDbContext db, string tenantId, Func<KuyumcuDbContext, Task<T>> oku)
    {
        if (!db.SubeFiltresiAcik) return await oku(db);

        if (string.IsNullOrEmpty(tenantId) || !string.Equals(db.AktifKiraciId, tenantId, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Yasal kümülatif okuyucu yalnız oturumun kendi kiracısı için açılır.");

        var kapsamci = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<SistemKapsami>(db)
            ?? throw new InvalidOperationException("Yasal kümülatif okuyucu: SistemKapsami çözülemedi.");

        await using var kapsam = kapsamci.Kiraci(tenantId);
        var kdb = kapsam.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
        if (kdb.SubeFiltresiAcik || kdb.SistemModu)
            throw new InvalidOperationException("Yasal kümülatif okuyucu: kiracı geneli kapsam kurulamadı.");
        kdb.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking;

        // ORTAM TRANSACTION'I (doğrulayıcı bulgusu, tur 2): çağıran bir transaction
        // içindeyse (ör. SiparisiSatisaCevir → SatisFisi) okuyucu AYNI bağlantı ve
        // AYNI transaction üzerinde koşar. Ayrı bağlantı, ortam transaction'ında
        // kapıdan önce yazılmış ama henüz commit edilmemiş nakit satırlarını
        // GÖRMEZDİ → kümülatif eksik sayılırdı (fail-open). Bağlantı paylaşılır,
        // sahipliği devredilmez (kdb kapanınca bağlantı açık kalır).
        var ortamTx = db.Database.CurrentTransaction;
        if (ortamTx is not null)
        {
            kdb.Database.SetDbConnection(db.Database.GetDbConnection());
            await kdb.Database.UseTransactionAsync(ortamTx.GetDbTransaction());
        }

        var sonuc = await oku(kdb);
        if (kdb.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Yasal kümülatif okuyucu SALT OKUMADIR; değişiklik izlendi.");
        return sonuc;
    }

    private AsyncServiceScope Ac(IKiraciBaglami baglam)
    {
        var kapsam = fabrika.CreateAsyncScope();
        // Bağlam, DbContext çözülmeden ÖNCE yerleşir.
        kapsam.ServiceProvider.GetRequiredService<KapsamBaglamAyari>().Ayarla(baglam);
        return kapsam;
    }
}
