using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MPos = Kuyumcu.Api.Modules.Pos;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

/// <summary>
/// PAKET 31 test yardımcıları — POS kurulumları TEK yerde durur ki kural
/// değişince onlarca test tek tek düzeltilmesin.
/// </summary>
public static class PosYardim
{
    public static MPos.PosServisi Servis(KuyumcuDbContext db) =>
        new(db, TestKurulum.Denetim(db));

    /// <summary>Kart bacağını POS defterine yazan satış servisi (ayarlar açıkça verilir).</summary>
    public static MSatis.SatisServisi Satis(KuyumcuDbContext db, MPos.PosAyarlari? ayarlar = null) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance, posAyarlari: ayarlar);

    /// <summary>Doğrudan cihaz satırı yazar (servis uçlarından bağımsız fikstür).</summary>
    public static string CihazYaz(
        TestVeritabani vt, Kiraci k, string ad = "Garanti POS",
        decimal oran = 0.0185m, int valorGun = 1, string? kasaId = null, bool aktif = true,
        string? subeId = null, string? bankaHesapId = null, bool hesapsiz = false)
    {
        using var db = vt.OlusturSistem();
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();
        // ZO-ISLEM-A 29.09: POS yatması yalnız cihazın hesabına gelen parayla eşleşir →
        // fikstür cihazı varsayılan olarak kiracının POS fikstür hesabına bağlıdır.
        var hesap = hesapsiz ? null : bankaHesapId ?? FiksturHesabi(db, k);
        db.PosCihazlari.Add(new PosCihaz
        {
            Id = id, TenantId = k.TenantId, SubeId = subeId ?? k.SubeId, Ad = ad, BankaAdi = "Garanti",
            KasaId = kasaId ?? k.KasaId, ParaBirimi = "TRY", BankaHesapId = hesap,
            PesinKomisyonOran = oran, ValorGun = valorGun, Aktif = aktif,
            Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    /// <summary>PosDefteri üzerinden tek satır yazar (TEK yazma yolu korunur).</summary>
    public static string TahsilatYaz(
        TestVeritabani vt, Kiraci k, string cihazId, decimal brut, int taksit = 1)
    {
        using var db = vt.OlusturSistem();
        var cihaz = db.PosCihazlari.First(c => c.Id == cihazId);
        var satir = MPos.PosDefteri.Ekle(db, new MPos.PosAyarlari(), cihaz, brut, taksit,
            null, null, k.KurId, k.CariId, DateTime.UtcNow);
        db.SaveChanges();
        return satir.Id;
    }

    /// <summary>Verilen kiracının POS tahsilat satırları (filtresiz bağlamla okunur).</summary>
    public static List<PosTahsilat> Tahsilatlar(TestVeritabani vt, Kiraci k)
    {
        using var db = vt.OlusturSistem();
        return db.PosTahsilatlari.AsNoTracking()
            .Where(t => t.TenantId == k.TenantId)
            .OrderBy(t => t.Olusturuldu).ToList();
    }

    /// <summary>Elle GELEN banka hareketi (Faz B eşleştirmesi için fikstür).</summary>
    public static string BankaHareketiYaz(
        TestVeritabani vt, Kiraci k, decimal tutar, string yon = "GELEN",
        string? hesapId = null, string? cariId = null)
    {
        using var db = vt.OlusturSistem();
        var now = DateTime.UtcNow;
        // ZO-ISLEM-A 29.09: varsayılan hesap = kiracının POS fikstür hesabı (CihazYaz ile aynı).
        hesapId ??= FiksturHesabi(db, k);
        var id = Kimlik.Ulid();
        db.BankaHareketleri.Add(new BankaHareket
        {
            Id = id, TenantId = k.TenantId, BankaHesapId = hesapId, Yon = yon,
            Tutar = tutar, ParaBirimi = "TRY", Durum = cariId is null ? "BEKLIYOR" : "ESLESTI",
            CariId = cariId, Tarih = now, Olusturuldu = now,
        });
        db.SaveChanges();
        return id;
    }

    /// <summary>Kiracının ayrı (POS'a bağlı olmayan) banka hesabını yazar.</summary>
    public static string HesapYaz(TestVeritabani vt, Kiraci k, string bankaAdi, string hesapAdi)
    {
        using var db = vt.OlusturSistem();
        var id = HesapEkle(db, k, bankaAdi, hesapAdi);
        db.SaveChanges();
        return id;
    }

    /// <summary>Kiracının POS fikstür hesabı ("Garanti · Ana") — yoksa yazılır, varsa aynısı döner.</summary>
    private static string FiksturHesabi(KuyumcuDbContext db, Kiraci k)
    {
        var var_ = db.BankaHesaplari.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.BankaAdi == "Garanti" && h.HesapAdi == "Ana")
            .Select(h => h.Id).FirstOrDefault();
        if (var_ is not null) return var_;
        var id = HesapEkle(db, k, "Garanti", "Ana");
        db.SaveChanges();
        return id;
    }

    private static string HesapEkle(KuyumcuDbContext db, Kiraci k, string bankaAdi, string hesapAdi)
    {
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();
        db.BankaHesaplari.Add(new BankaHesap
        {
            Id = id, TenantId = k.TenantId, BankaAdi = bankaAdi, HesapAdi = hesapAdi,
            Iban = TestIban.Uret(), // GIRDI-AILESI-2 (D2): kontrol hanesi doğru test IBAN'ı
            ParaBirimi = "TRY", Bakiye = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        return id;
    }

    /// <summary>
    /// Servis yanıtındaki cihaz kimliği. ⚠ Servis testinde nesne PascalCase
    /// serileşir, HTTP yolunda camelCase — ikisi de denenir (tip ikizlenmesi
    /// tuzağının test tarafındaki karşılığı).
    /// </summary>
    public static string CihazId(object cevap) =>
        TestKurulum.Metin(cevap, "cihaz", "Id")
        ?? TestKurulum.Metin(cevap, "cihaz", "id")
        ?? throw new InvalidOperationException("Cihaz kimliği yanıtta yok");

    public static MSatis.SatisOlusturDto KartSatis(
        Kiraci k, decimal tutar, string? cihazId = null, int? taksit = null) => new()
    {
        CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = tutar,
        OdemeTuru = "KART", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        PosCihazId = cihazId, PosTaksitAdedi = taksit,
    };
}

/// <summary>
/// Valör takvimi ölçümü için EF komut yakalayıcılı bağlam (Aşkın koşulu:
/// "sunucuda toplanıyor mu" iddiası SQL metninden doğrulanır).
/// </summary>
public static class PosSqlOrtami
{
    public static KuyumcuDbContext Kur(
        TestVeritabani vt, string tenantId, Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor yakalayici)
    {
        var ayar = new DbContextOptionsBuilder<KuyumcuDbContext>()
            .UseMySql(vt.Baglanti, ServerVersion.Parse("8.4.0-mysql"))
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory,
                Kuyumcu.Api.Data.KiraciModelOnbellegi>()
            .AddInterceptors(yakalayici)
            .Options;
        return new KuyumcuDbContext(ayar, new Kuyumcu.Api.Data.SabitKiraciBaglami(tenantId),
            TestVeritabani.TestSifreleyici);
    }
}
