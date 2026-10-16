using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Data.GeriDoldurma;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// GERİ-DOLDURMA — nakit payı alanları eklenmeden önce yazılmış satırlar
//
// ⚠ Bu testler migration'ın KOŞTUĞU SQL METNİNİN TA KENDİSİNİ koşar
// (NakitPayiGeriDoldurma sabitleri). Test kendi SQL'ini yazsaydı, üretimde
// koşmayan bir metni doğrulamış olurdu.
//
// ⚠ Migration'ın kendisi bu testle SINANMAZ: TestOrtami her süreçte
// EnsureDeleted + Migrate yapar, yani şema HER ZAMAN BOŞ veritabanına kurulur
// ve geri-doldurma UPDATE'i hiçbir satır görmez. Burada fikstür ELLE kurulur.
//
// İLKE: tahmin edilen pay YAZILMAZ. Kanıtı olmayan satır NULL kalır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class GeriDoldurmaTestleri(TestVeritabani vt)
{
    /// <summary>Ödeme payı DOLDURULMAMIŞ ham satır — migration öncesi kaydı taklit eder.</summary>
    private static string EskiHareket(
        KuyumcuDbContext db, Kiraci k, string tur, decimal tl, string? odemeTuru)
    {
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        // Fabrika KULLANILMAZ: fabrika payı doldurur, oysa burada tam olarak
        // "payı boş" bir satır gerekiyor.
        db.Hareketler.Add(new Hareket
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = tur, Tarih = now, TlTutari = tl, HasGrami = 0m,
            ParaBirimi = "TRY", OdemeTuru = odemeTuru,
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            Olusturuldu = now,
        });
        db.SaveChanges();
        return id;
    }

    private static void KasaSatiri(
        KuyumcuDbContext db, Kiraci k, string hareketId, decimal tutar, string odemeTuru)
    {
        var now = DateTime.UtcNow;
        db.KasaHareketleri.Add(new KasaHareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, KasaId = k.KasaId,
            HareketId = hareketId, Tutar = tutar, ParaBirimi = "TRY",
            OdemeTuru = odemeTuru, Tarih = now, Olusturuldu = now,
        });
        db.SaveChanges();
    }

    private static void GeriDoldur(KuyumcuDbContext db)
    {
        foreach (var sql in NakitPayiGeriDoldurma.Sirayla)
            db.Database.ExecuteSqlRaw(sql);
    }

    private static Hareket Oku(KuyumcuDbContext db, string id) =>
        db.Hareketler.AsNoTracking().Single(h => h.Id == id);

    [Fact]
    public void TL_bacagi_sifir_olan_hareketler_SIFIRLA_doldurulur()
    {
        // Emanet ve transferde para hiç el değiştirmez — bu bir çıkarım değil,
        // kaydın kendi TL bacağının söylediği şeydir.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Geri doldurma parasız");
        using var db = vt.OlusturSistem();
        var emanet = EskiHareket(db, k, "EMANET_GIRIS", 0m, null);
        var transfer = EskiHareket(db, k, "TRANSFER", 0m, null);

        GeriDoldur(db);

        foreach (var id in new[] { emanet, transfer })
        {
            var h = Oku(db, id);
            Assert.Equal(0m, h.NakitTutar);
            Assert.Equal(0m, h.VeresiyeTutar);
        }
    }

    [Fact]
    public void Kasa_defteri_NAKIT_diyorsa_nakit_payi_KANITTAN_gelir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Geri doldurma kasa nakit");
        using var db = vt.OlusturSistem();
        var h = EskiHareket(db, k, "SATIS", 12_000m, null);
        KasaSatiri(db, k, h, 12_000m, "NAKIT");

        GeriDoldur(db);

        var okunan = Oku(db, h);
        Assert.Equal(12_000m, okunan.NakitTutar);
        Assert.Equal(0m, okunan.VeresiyeTutar);   // kasa tutarı TL bacağının tamamını karşılıyor
    }

    [Fact]
    public void Kasa_defteri_KART_diyorsa_nakit_payi_SIFIRDIR()
    {
        // Kanıt "nakit değil" de diyebilir; bu da kesin bilgidir.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Geri doldurma kasa kart");
        using var db = vt.OlusturSistem();
        var h = EskiHareket(db, k, "SATIS", 12_000m, null);
        KasaSatiri(db, k, h, 12_000m, "KART");

        GeriDoldur(db);

        Assert.Equal(0m, Oku(db, h).NakitTutar);
    }

    [Fact]
    public void Kismi_odemede_veresiye_payi_BILINMEZ_kalir()
    {
        // 50.000'lik alışın 10.000'i kasadan geçmiş: nakit payı KANITLI (10.000),
        // ama kalan 40.000'in veresiye mi başka bir şey mi olduğu bu sorgudan
        // çıkarılamaz — uydurulmaz, NULL bırakılır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Geri doldurma kısmi");
        using var db = vt.OlusturSistem();
        var h = EskiHareket(db, k, "ALIS", -50_000m, null);
        KasaSatiri(db, k, h, -10_000m, "NAKIT");

        GeriDoldur(db);

        var okunan = Oku(db, h);
        Assert.Equal(10_000m, okunan.NakitTutar);   // işaret değil, mutlak değer
        Assert.Null(okunan.VeresiyeTutar);          // BİLİNMİYOR
    }

    [Fact]
    public void Kasa_karsiligi_OLMAYAN_parali_hareket_NULL_kalir()
    {
        // Kanıt yoksa tahmin YOK. NULL "bilinmiyor" demektir ve tevsik kapısı
        // bu satırlar için eski kurala düşer — durum kötüleşmez.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Geri doldurma kanıtsız");
        using var db = vt.OlusturSistem();
        var h = EskiHareket(db, k, "ALIS", -75_000m, null);

        GeriDoldur(db);

        var okunan = Oku(db, h);
        Assert.Null(okunan.NakitTutar);
        Assert.Null(okunan.VeresiyeTutar);
    }

    [Fact]
    public void Birden_fazla_kasa_satiri_olan_hareket_DOKUNULMADAN_birakilir()
    {
        // Çok bacaklı ödemede hangi payın nakit olduğu tek satırlık bu sorgudan
        // çıkarılamaz. HAVING COUNT(*) = 1 bu satırları dışarıda tutar.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Geri doldurma çok bacak");
        using var db = vt.OlusturSistem();
        var h = EskiHareket(db, k, "SATIS", 30_000m, null);
        KasaSatiri(db, k, h, 20_000m, "NAKIT");
        KasaSatiri(db, k, h, 10_000m, "KART");

        GeriDoldur(db);

        Assert.Null(Oku(db, h).NakitTutar);
    }

    [Fact]
    public void Zaten_dolu_satirlar_EZILMEZ()
    {
        // Geri-doldurma yalnız boşları doldurur; yeni kodun yazdığı kesin payı
        // kanıt tahminiyle ezmek veri kaybıdır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Geri doldurma ezme yok");
        using var db = vt.OlusturSistem();
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(500m), new Hareket
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = "SATIS", Tarih = now, TlTutari = 9_000m, HasGrami = 0m,
            ParaBirimi = "TRY", OdemeTuru = "KARMA",
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            Olusturuldu = now,
        });
        db.SaveChanges();
        KasaSatiri(db, k, id, 9_000m, "NAKIT");   // kanıt "tamamı nakit" der ama YANILIR

        GeriDoldur(db);

        Assert.Equal(500m, Oku(db, id).NakitTutar);   // fabrikanın yazdığı pay korunur
    }
}
