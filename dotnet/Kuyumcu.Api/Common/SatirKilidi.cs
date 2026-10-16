using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Common;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KAPORA-ESZAMAN — SATIR KİLİDİ (SELECT … FOR UPDATE).
//
// SORUN (doğrulayıcı EKSIK-B1-YAZDIR tur 3, canlı 5166'da kanıtlandı): para uçları
// "kontrol et → ayrı transaction'da yaz" kalıbındaydı. İki eşzamanlı istek AYNI
// kontrolü (kapora yok mu? storno yok mu? iade hakkı kaldı mı?) birlikte geçiyor,
// ikisi de yazıyordu → kasada iki kat para, biri siparişe bağsız.
//
// KURAL (ekip oyu, fail-closed): kilit + kontrol + yazma TEK işlemde. İşlem açılır,
// İLK deyim olarak korunan satır kilitlenir (InnoDB X kilidi; ikinci istek burada
// BEKLER), kontrol kilitten SONRA okunur (REPEATABLE READ anlık görüntüsü ilk tutarlı
// okumada kurulur → kilit alındıktan sonra kurulur ve öncekinin commit'ini görür).
//
// Tablo adı serbest metin DEĞİLDİR (SQL enjeksiyonu kapısı): yalnız aşağıdaki kapalı
// listeden; kimlik ve kiracı PARAMETRE olarak gider.
// ═════════════════════════════════════════════════════════════════════════════

public enum KilitTablosu
{
    Siparisler,
    Hareketler,
    KasaHareketleri,
    BankaHareketleri,
    Cariler,
    // PAKET EKSIK-B1-SATINALMA: mal kabul — çift kabul (eşzamanlı iki istek) aynı kalanı iki kez stoğa yazmasın.
    SatinalmaSiparisleri,
}

public static class SatirKilidi
{
    public static string TabloAdi(KilitTablosu t) => t switch
    {
        KilitTablosu.Siparisler      => "siparisler",
        KilitTablosu.Hareketler      => "hareketler",
        KilitTablosu.KasaHareketleri => "kasa_hareketleri",
        KilitTablosu.BankaHareketleri => "banka_hareketleri",
        KilitTablosu.Cariler         => "cariler",
        KilitTablosu.SatinalmaSiparisleri => "satinalma_siparisleri",
        _ => throw new ArgumentOutOfRangeException(nameof(t)),
    };

    /// <summary>
    /// Açık işlemde (tenant_id, id) satırını işlem sonuna dek kilitler. İşlem yoksa
    /// kilit anlamsızdır (otomatik commit hemen bırakır) → programcı hatası, fırlatılır.
    /// Satır yoksa (yabancı kiracı / silinmiş) hiçbir şey kilitlenmez; çağıranın
    /// sonraki okuması 404'ü üretir.
    /// </summary>
    public static async Task Kilitle(this DatabaseFacade db, KilitTablosu tablo, string id, string tenantId)
    {
        if (db.CurrentTransaction is null)
            throw new InvalidOperationException(
                "Satır kilidi yalnız açık bir veritabanı işlemi içinde alınır (IsBaslatVeyaDevral).");
        var sql = "SELECT id FROM " + TabloAdi(tablo) + " WHERE id = {0} AND tenant_id = {1} FOR UPDATE";
        await db.ExecuteSqlRawAsync(sql, id, tenantId);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KAPORA-ESZAMAN tur 2 — BAKİYE KİLİDİ (kayıp güncelleme).
//
// SORUN (doğrulayıcı tur 1, canlı 5198): kasalar.bakiye / banka_hesaplari.bakiye /
// cari_bakiyeler.* uygulamada OKU-DEĞİŞTİR-YAZ ile güncelleniyordu. Aynı kasaya 10
// eşzamanlı 100 TL tahsilat → 10×200, kasa +100 (beklenen +1000); SUM(kasa_hareketleri)
// ≠ kasalar.bakiye. Defter satırları doğru, ÖZET kolon bozuk.
//
// KURAL: bakiyeye delta yazmadan ÖNCE, açık işlemde, satır KİLİTLİ OKUMA ile yeniden
// okunur (SELECT … FOR UPDATE). InnoDB'de kilitli okuma işlemin anlık görüntüsünü DEĞİL
// son commit'lenmiş sürümü döndürür → REPEATABLE READ altında, kilit işlemin ortasında
// alınsa bile doğru değer gelir (düz SELECT ya da EF Reload burada YANLIŞ olurdu: anlık
// görüntüden okur). Okunan değer izlenen varlığın hem özgün hem güncel değerine yazılır;
// çağıranın ardından yaptığı "Bakiye = Bakiye + d" artık taze değer üzerinden hesaplanır.
// Aynı istekte ikinci delta: özellik zaten "değişti" ise yeniden okunmaz (satır zaten bu
// işlemde kilitli, bellekteki değer ilk deltayı içerir).
// İşlem yoksa kilit anlamsızdır (otomatik commit hemen bırakır) → hiçbir şey yapılmaz;
// işlem açmayan çağıranlar rapordaki listede (davranış değişmez).
// ═════════════════════════════════════════════════════════════════════════════
public static class BakiyeKilidi
{
    /// <summary>Kasa bakiyesini kilitli okur ve izlenen varlığa yazar (delta yazmadan hemen önce çağrılır).</summary>
    public static Task Kilitle(KuyumcuDbContext db, Kasa kasa) =>
        TekKolon(db, kasa, "SELECT bakiye FROM kasalar WHERE id = @id AND tenant_id = @t FOR UPDATE",
            kasa.Id, kasa.TenantId, e => e.Property(x => x.Bakiye));

    /// <summary>Banka hesabı bakiyesini kilitli okur ve izlenen varlığa yazar.</summary>
    public static Task Kilitle(KuyumcuDbContext db, BankaHesap hesap) =>
        TekKolon(db, hesap, "SELECT bakiye FROM banka_hesaplari WHERE id = @id AND tenant_id = @t FOR UPDATE",
            hesap.Id, hesap.TenantId, e => e.Property(x => x.Bakiye));

    /// <summary>Cari bakiye satırının TÜM tutar kolonlarını kilitli okur (CariBakiyeYazici.Uygula çağırır).</summary>
    public static async Task Kilitle(KuyumcuDbContext db, CariBakiye b)
    {
        var e = db.Entry(b);
        if (!IslemVar(db) || e.State != EntityState.Unchanged) return;   // Added: yeni satır · Modified: bu işlemde zaten tazelendi
        var d = await Oku(db,
            "SELECT tl_bakiye, has_gram_bakiye, usd_bakiye, eur_bakiye, gbp_bakiye FROM cari_bakiyeler " +
            "WHERE cari_id = @id AND tenant_id = @t FOR UPDATE", b.CariId, b.TenantId, 5);
        if (d is null) return;
        Yaz(e.Property(x => x.TlBakiye), d[0]);
        Yaz(e.Property(x => x.HasGramBakiye), d[1]);
        Yaz(e.Property(x => x.UsdBakiye), d[2]);
        Yaz(e.Property(x => x.EurBakiye), d[3]);
        Yaz(e.Property(x => x.GbpBakiye), d[4]);
    }

    private static bool IslemVar(KuyumcuDbContext db) =>
        db.Database.IsRelational() && db.Database.CurrentTransaction is not null;

    private static async Task TekKolon<T>(KuyumcuDbContext db, T varlik, string sql, string id, string tenantId,
        Func<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T>,
             Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry<T, decimal>> ozellik) where T : class
    {
        if (!IslemVar(db)) return;
        var p = ozellik(db.Entry(varlik));
        if (p.IsModified) return;   // bu işlemde zaten tazelendi ve değişti
        var d = await Oku(db, sql, id, tenantId, 1);
        if (d is not null) Yaz(p, d[0]);
    }

    private static void Yaz<T>(Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry<T, decimal> p, decimal v) where T : class
    {
        p.OriginalValue = v;
        p.CurrentValue = v;
        p.IsModified = false;
    }

    private static async Task<decimal[]?> Oku(KuyumcuDbContext db, string sql, string id, string tenantId, int kolon)
    {
        var baglanti = db.Database.GetDbConnection();
        await using var komut = baglanti.CreateCommand();
        komut.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        komut.CommandText = sql;
        var p1 = komut.CreateParameter(); p1.ParameterName = "@id"; p1.Value = id; komut.Parameters.Add(p1);
        var p2 = komut.CreateParameter(); p2.ParameterName = "@t"; p2.Value = tenantId; komut.Parameters.Add(p2);
        await using var okuyucu = await komut.ExecuteReaderAsync();
        if (!await okuyucu.ReadAsync()) return null;
        var d = new decimal[kolon];
        for (var i = 0; i < kolon; i++) d[i] = okuyucu.GetDecimal(i);
        return d;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET MASAK-ESZAMAN — YASAL KÜMÜLATİF KİLİDİ (MASAK bağlantılı işlem · tevsik).
//
// SORUN (KAPORA-ESZAMAN hasım tur 2, KRİTİK; canlı kanıtlı): MasakKapisi.DogrulaKumulatif
// ve TevsikKapisi.Dogrula "bugünkü toplam + bu işlem ≥ eşik mi?" sorusunu işlem AÇILMADAN,
// kilitsiz okuyordu. Aynı cariye eşzamanlı 10 × 40.000 TL HAVALE: her istek "önceki 0"
// gördü, hepsi beyansız geçti → 400.000 TL kimlik tespitsiz (eşik bölme / structuring).
//
// KURAL (fail-closed, eşik DEĞİŞMEZ — YasalParametreServisi tek kaynak):
//   (1) İşlem READ COMMITTED açılır (yoksa) ve İLK iş olarak CARİ SATIRI kilitlenir
//       (SELECT … FOR UPDATE). Aynı cariye ikinci istek BURADA bekler.
//   (2) Kümülatif kapılar kilitten SONRA, hareket yazılmadan ÖNCE YENİDEN koşar
//       (MasakKapisi.KilitAltindaYinele · tevsik.Dogrula aynı argümanlarla). İlk istek
//       commit edince kilit bırakılır; bekleyen istek güncel toplamı görür.
//   (3) READ COMMITTED: her tutarlı okuma kendi anlık görüntüsünü alır → kilit alındıktan
//       sonra yapılan toplam, kilidi bırakan işlemin commit'ini GÖRÜR. (REPEATABLE READ'de
//       anlık görüntü işlemin ilk okumasında kurulur; kilitten önce okuma yapılmışsa toplam
//       eski kalırdı.)
//   (4) Dış işlem (devralınan) READ COMMITTED değilse: kilitten sonra TAZELİK DENETİMİ —
//       carinin bugünkü satır sayısı tutarlı okumayla ve kilitli okumayla (FOR SHARE) sayılır;
//       farklıysa anlık görüntü eskidir → İŞLEM YAZILMAZ (InvalidOperationException, sessiz
//       geçiş yok). Bilinen tek yol: Satınalma mal kabulü (dış işlem; rapor § açık kalanlar).
//
// Kilit sırası (deadlock yok): sipariş/kaynak hareket (varsa) → CARİ → kasa/banka → cari_bakiyeler.
// ═════════════════════════════════════════════════════════════════════════════
public static class YasalKilit
{
    /// <summary>İşlem yoksa READ COMMITTED açar (döndürür), varsa devralır (null).</summary>
    public static async Task<IDbContextTransaction?> BaslatVeyaDevral(this DatabaseFacade db) =>
        db.CurrentTransaction is null
            ? await db.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted)
            : null;

    /// <summary>
    /// İşlemi başlatır/devralır ve <paramref name="cariId"/> satırını işlem sonuna dek kilitler.
    /// Cari yoksa (kimliksiz tek işlem) yalnız işlem açılır — kümülatif kurulamaz, tek işlem kuralı işler.
    /// </summary>
    public static async Task<IDbContextTransaction?> CariIcinBaslat(
        this DatabaseFacade db, string tenantId, string? cariId, DateTime an)
    {
        var tx = await db.BaslatVeyaDevral();
        try
        {
            await CariKilitle(db, tenantId, cariId, an);
        }
        catch
        {
            if (tx is not null) await tx.DisposeAsync();
            throw;
        }
        return tx;
    }

    /// <summary>Açık işlemde cari satırını kilitler; dış işlem READ COMMITTED değilse tazelik denetler.</summary>
    public static async Task CariKilitle(DatabaseFacade db, string tenantId, string? cariId, DateTime an)
    {
        if (string.IsNullOrEmpty(cariId)) return;
        await db.Kilitle(KilitTablosu.Cariler, cariId, tenantId);   // işlem yoksa fırlatır
        var isl = db.CurrentTransaction!.GetDbTransaction();
        if (isl.IsolationLevel == System.Data.IsolationLevel.ReadCommitted) return;
        await TazelikDenetle(db, isl, tenantId, cariId, an);
    }

    private static async Task TazelikDenetle(
        DatabaseFacade db, System.Data.Common.DbTransaction isl, string tenantId, string cariId, DateTime an)
    {
        var (bas, bit) = Zaman.YerelGunAraligiUtc(DateOnly.FromDateTime(Zaman.UtcdenYerele(an)));
        const string h = "SELECT COUNT(*) FROM hareketler WHERE tenant_id = @t AND cari_id = @c AND tarih >= @b AND tarih < @e";
        const string b = "SELECT COUNT(*) FROM banka_hareketleri WHERE tenant_id = @t AND cari_id = @c AND olusturuldu >= @b AND olusturuldu < @e";
        foreach (var sql in new[] { h, b })
        {
            var tutarli = await Say(db, isl, sql, tenantId, cariId, bas, bit);
            var kilitli = await Say(db, isl, sql + " FOR SHARE", tenantId, cariId, bas, bit);
            if (tutarli != kilitli)
                throw new InvalidOperationException(
                    "MASAK-ESZAMAN: dış işlemin anlık görüntüsü eski (REPEATABLE READ) — yasal kümülatif güvenle okunamaz, işlem yazılmadı.");
        }
    }

    private static async Task<long> Say(DatabaseFacade db, System.Data.Common.DbTransaction isl, string sql,
        string tenantId, string cariId, DateTime bas, DateTime bit)
    {
        await using var komut = db.GetDbConnection().CreateCommand();
        komut.Transaction = isl;
        komut.CommandText = sql;
        foreach (var (ad, deger) in new (string, object)[] { ("@t", tenantId), ("@c", cariId), ("@b", bas), ("@e", bit) })
        {
            var p = komut.CreateParameter(); p.ParameterName = ad; p.Value = deger; komut.Parameters.Add(p);
        }
        return Convert.ToInt64(await komut.ExecuteScalarAsync());
    }
}
