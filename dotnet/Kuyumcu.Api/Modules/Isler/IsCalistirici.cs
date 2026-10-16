using System.Diagnostics;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Isler;

/// <summary>Bir koşumun sonucu — uç ve zamanlayıcı aynı nesneyi okur.</summary>
/// <param name="Atlandi">Bu (iş, anahtar) çifti ZATEN koşmuş; ikinci koşum yapılmadı.</param>
public readonly record struct IsKosumSonucu(
    string IsKodu,
    string CalismaAnahtari,
    string Durum,
    bool Atlandi,
    int UretilenBildirim,
    int AtlananBildirim,
    int IncelenenKayit,
    int SureMs,
    string? HataOzeti,
    string? CalismaId);

/// <summary>
/// ZAMANLANMIŞ İŞ ÇALIŞTIRICISI — işin tek yazma kapısı.
///
/// Akış:
///   (a) HAK TALEBİ: is_calismalari'na BASLADI satırı eklenir ve SaveChanges
///       çağrılır. (tenant_id, is_kodu, calisma_anahtari) UNIQUE olduğu için
///       ikinci koşum VERİTABANI tarafından reddedilir. "Önce SELECT et"
///       yapılmaz — iki örnek aynı anda uyansa bile yalnız biri kazanır.
///   (b) İş koşar, taslak döndürür; taslaklar bildirimler'e tek tek eklenir.
///       (tenant_id, tekillik_anahtari) çakışması ATLANAN sayılır.
///   (c) Çalışma satırı BASARILI/HATALI olarak BİR KEZ tamamlanır.
///
/// ⚠ HATA GİZLEME YASAK (Faz A kabul ölçütü, Anayasa md.1):
///   DbUpdateException YALNIZ MySQL 1062 (duplicate key) ise "zaten çalıştı"
///   sayılır. Başka her istisna — başka bir DbUpdateException dahil — HATALI
///   olarak kayda geçer. Her istisnayı sessizce atlamak gerçek hatayı gizlerdi.
///
/// ⚠ FAIL-SOFT TUR / FAIL-CLOSED KAPI: bir işin hatası turu düşürmez; ama bir
///   kapı (yetki/rol/kiracı) ihlali ASLA yumuşatılmaz.
/// </summary>
public sealed class IsCalistirici(
    KuyumcuDbContext db,
    IEnumerable<IZamanlanmisIs> isler,
    IConfiguration yapilandirma,
    IServiceProvider kapsam,
    IBildirimKanali kanal,
    IsAyarServisi ayarServisi,
    ILogger<IsCalistirici> gunluk)
{
    /// <summary>Tanımlı tüm işler (kapalılar dahil) — /isler ucu bunu listeler.</summary>
    public IReadOnlyList<IZamanlanmisIs> Tumu => [.. isler.OrderBy(i => i.Kod, StringComparer.Ordinal)];

    public IZamanlanmisIs? Bul(string kod) =>
        isler.FirstOrDefault(i => string.Equals(i.Kod, kod, StringComparison.Ordinal));

    /// <summary>YAPILANDIRMA kapısı (appsettings). Kiracı ayarı bunu GEVŞETEMEZ.</summary>
    public bool Etkin(IZamanlanmisIs i) => i.Etkin(yapilandirma);

    /// <summary>Bu turda koşacak işler — yapılandırmada KAPALI olanlar hiç dokunulmaz.</summary>
    public IReadOnlyList<IZamanlanmisIs> EtkinIsler => [.. Tumu.Where(Etkin)];

    // ── PAKET P-11 (B-24) — KİRACI AYARI KAPISI ─────────────────────────────
    /// <summary>
    /// Kiracı için gerçekten koşacak işler: yapılandırma AÇIK **ve** kiracı
    /// kapatmamış. ⚠ Ayar yalnız DARALTIR — yapılandırmada kapalı bir iş
    /// kiracı ayarıyla açılamaz (IsAyarAnahtari notu, fail-closed).
    /// </summary>
    public async Task<IReadOnlyList<IZamanlanmisIs>> KiraciEtkinIsleri(
        string tenantId, CancellationToken iptal = default)
    {
        var ayar = await ayarServisi.Oku(tenantId, iptal);
        return [.. EtkinIsler.Where(i => !ayar.KapaliIsler.Contains(i.Kod, StringComparer.Ordinal))];
    }

    /// <summary>Tek iş için kiracı kapısı — elle tetikleme ucu bunu sorar.</summary>
    public async Task<bool> KiraciEtkinMi(IZamanlanmisIs i, string tenantId, CancellationToken iptal = default)
    {
        if (!Etkin(i)) return false;
        var ayar = await ayarServisi.Oku(tenantId, iptal);
        return !ayar.KapaliIsler.Contains(i.Kod, StringComparer.Ordinal);
    }

    /// <summary>Bir kiracı için TÜM etkin işleri sırayla koşturur (zamanlayıcı turu).</summary>
    public async Task<List<IsKosumSonucu>> TumEtkinIsleriCalistir(
        string tenantId, DateTime anUtc, CancellationToken iptal)
    {
        var sonuclar = new List<IsKosumSonucu>();
        var ayar = await ayarServisi.Oku(tenantId, iptal);

        // B-24 "saat": kiracı işletme saatinden ÖNCE tur koşmaz. Gece yarısı
        // üretilen bir hatırlatma sabah kimsenin görmediği bir bildirimdir;
        // kuyumcu dükkânı açıldıktan sonra koşmasını isteyebilir.
        // ⚠ ELLE tetikleme bu kapıdan GEÇMEZ (kullanıcı bilerek istemiştir).
        var yerelSaat = Zaman.UtcdenYerele(anUtc).Hour;
        if (yerelSaat < ayar.BaslangicSaat)
        {
            gunluk.LogDebug("Tur saat kapısında bekledi: {Saat} < {Esik}", yerelSaat, ayar.BaslangicSaat);
            return sonuclar;
        }

        foreach (var i in EtkinIsler.Where(x => !ayar.KapaliIsler.Contains(x.Kod, StringComparer.Ordinal)))
        {
            iptal.ThrowIfCancellationRequested();
            // Bir işin hatası turu düşürmez: Calistir zaten istisnayı HATALI'ya
            // çevirip kayda geçirir. Buraya yalnız hak talebi yazılamazsa düşer.
            try { sonuclar.Add(await Calistir(i, tenantId, anUtc, null, iptal)); }
            catch (Exception ex)
            {
                gunluk.LogError(ex, "İş hak talebi yazılamadı: {Kod}", i.Kod);
            }
        }
        return sonuclar;
    }

    /// <summary>Elle tetikleme — çalışma anahtarı "-Ehhmm" ekiyle günlük anahtardan ayrışır.</summary>
    public Task<IsKosumSonucu> ElleCalistir(
        IZamanlanmisIs i, string tenantId, DateTime anUtc, string tetikleyenKullaniciId,
        CancellationToken iptal) =>
        Calistir(i, tenantId, anUtc, tetikleyenKullaniciId, iptal);

    public async Task<IsKosumSonucu> Calistir(
        IZamanlanmisIs i, string tenantId, DateTime anUtc,
        string? tetikleyenKullaniciId, CancellationToken iptal)
    {
        var yerelGun = DateOnly.FromDateTime(Zaman.UtcdenYerele(anUtc));
        var anahtar = tetikleyenKullaniciId is null
            ? i.CalismaAnahtari(yerelGun)
            : IsAnahtarlari.Elle(anUtc);

        // (a) HAK TALEBİ — kazananı veritabanı seçer.
        var calisma = new IsCalismasi
        {
            Id = Kimlik.Ulid(),
            TenantId = tenantId,
            IsKodu = i.Kod,
            CalismaAnahtari = anahtar,
            Baslangic = anUtc,
            Durum = IsDurum.Basladi,
            TetikleyenKullaniciId = tetikleyenKullaniciId,
        };
        db.IsCalismalari.Add(calisma);
        try
        {
            await db.SaveChangesAsync(iptal);
        }
        catch (DbUpdateException ex) when (BenzersizlikIhlali(ex))
        {
            db.Entry(calisma).State = EntityState.Detached;
            gunluk.LogDebug("İş bu anahtarda zaten çalışmış: {Kod}/{Anahtar}", i.Kod, anahtar);
            return new IsKosumSonucu(i.Kod, anahtar, IsDurum.Basarili, true, 0, 0, 0, 0, null, null);
        }

        // (b) İŞ KOŞAR.
        var kronometre = Stopwatch.StartNew();
        int uretilen = 0, atlanan = 0, incelenen = 0;
        string? hataOzeti = null;

        try
        {
            var cikti = await i.Calistir(new IsBaglami(kapsam, db, tenantId, anUtc), iptal);
            incelenen = cikti.IncelenenKayit;

            foreach (var taslak in cikti.Taslaklar)
            {
                var bildirim = new Bildirim
                {
                    Id = Kimlik.Ulid(),
                    TenantId = tenantId,
                    Tur = taslak.Tur,
                    Onem = taslak.Onem,
                    Baslik = Kirp(taslak.Baslik, 200),
                    Govde = Kirp(taslak.Govde, 1000),
                    HedefRota = taslak.HedefRota,
                    GorunurRoller = taslak.GorunurRoller ?? "",
                    IlgiliVarlikTuru = taslak.IlgiliVarlikTuru,
                    IlgiliVarlikId = taslak.IlgiliVarlikId,
                    TekillikAnahtari = taslak.TekillikAnahtari,
                    IsCalismasiId = calisma.Id,
                    Olusturuldu = anUtc,
                };
                db.Bildirimler.Add(bildirim);
                try
                {
                    await db.SaveChangesAsync(iptal);
                    uretilen++;
                    await kanal.Gonder(bildirim, iptal);
                }
                catch (DbUpdateException ex) when (BenzersizlikIhlali(ex))
                {
                    // Aynı tekillik anahtarı zaten var → kullanıcı TEK satır görür.
                    // ⚠ ChangeTracker.Clear() ÇAĞRILMAZ: hak talebi satırının
                    //   izlenmesi sürmeli, yoksa (c) adımı satırı bulamaz.
                    db.Entry(bildirim).State = EntityState.Detached;
                    atlanan++;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            hataOzeti = Siniflandir(ex);
            gunluk.LogError(ex, "Zamanlanmış iş hatası: {Kod}", i.Kod);
            // Başarısız SaveChanges izlenen değişiklikleri geride bırakabilir;
            // hak talebi satırı dışındaki her şey temizlenir.
            TemizleHakTalebiDisinda(calisma);
        }

        // (c) TAMAMLA — bir kez.
        kronometre.Stop();
        calisma.Bitis = DateTime.UtcNow;
        calisma.Durum = hataOzeti is null ? IsDurum.Basarili : IsDurum.Hatali;
        calisma.UretilenBildirim = uretilen;
        calisma.IncelenenKayit = incelenen;
        calisma.SureMs = (int)Math.Min(kronometre.ElapsedMilliseconds, int.MaxValue);
        calisma.HataOzeti = hataOzeti;
        await db.SaveChangesAsync(iptal);

        return new IsKosumSonucu(
            i.Kod, anahtar, calisma.Durum, false, uretilen, atlanan, incelenen,
            calisma.SureMs ?? 0, hataOzeti, calisma.Id);
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    /// <summary>
    /// YALNIZ MySQL 1062 (ER_DUP_ENTRY) "zaten var" demektir. Başka hiçbir
    /// DbUpdateException buraya girmez — girseydi kolon taşması, yabancı anahtar
    /// ihlali ve bağlantı kopması "zaten çalıştı" diye yutulurdu (md.1).
    /// </summary>
    internal static bool BenzersizlikIhlali(DbUpdateException ex) =>
        ex.InnerException is MySqlConnector.MySqlException { Number: 1062 };

    /// <summary>
    /// SINIFLANDIRILMIŞ özet — ham istisna mesajı ASLA yazılmaz (bağlantı dizesi,
    /// sır ve kişisel veri oradan sızardı). Yalnız tür adı + varsa MySQL hata no.
    /// </summary>
    internal static string Siniflandir(Exception ex)
    {
        var mysql = ex as MySqlConnector.MySqlException
            ?? ex.InnerException as MySqlConnector.MySqlException;
        var ozet = mysql is not null
            ? FormattableString.Invariant($"VERITABANI_HATASI/{ex.GetType().Name}/MySqlNo={mysql.Number}")
            : $"IS_HATASI/{ex.GetType().Name}";
        return Kirp(ozet, 500);
    }

    private static string Kirp(string metin, int azami) =>
        metin.Length <= azami ? metin : metin[..azami];

    private void TemizleHakTalebiDisinda(IsCalismasi calisma)
    {
        foreach (var giris in db.ChangeTracker.Entries().ToList())
        {
            if (ReferenceEquals(giris.Entity, calisma)) continue;
            if (giris.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                giris.State = EntityState.Detached;
        }
    }
}
