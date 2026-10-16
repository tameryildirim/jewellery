using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Data.GeriDoldurma;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// BELGE SAHİBİ GERİ-DOLDURMA — paket 4
//
// ⚠ Bu testler migration'ın KOŞTUĞU SQL METNİNİN TA KENDİSİNİ koşar
// (BelgeSahipGeriDoldurma sabitleri). Test kendi SQL'ini yazsaydı, üretimde
// koşmayan bir metni doğrulamış olurdu.
//
// ⚠ Migration'ın kendisi burada SINANMAZ: TestOrtami her süreçte
// EnsureDeleted + Migrate yapar, yani şema HER ZAMAN BOŞ veritabanına kurulur
// ve geri-doldurma UPDATE'i hiçbir satır görmez. Fikstür ELLE kurulur —
// bu yüzden urun_id kolonu Faz A'da DÜŞÜRÜLMEDİ (Aşkın'ın koşulu).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class BelgeGeriDoldurmaTestleri(TestVeritabani vt)
{
    /// <summary>Sahibi HENÜZ YAZILMAMIŞ ham satır — migration öncesi kaydı taklit eder.</summary>
    private static string EskiBelge(KuyumcuDbContext db, string tenantId, string urunId,
        string sahipTuru = "URUN", string sahipId = "")
    {
        var id = Kimlik.Ulid();
        db.Belgeler.Add(new Belge
        {
            Id = id, TenantId = tenantId,
            SahipTuru = sahipTuru, SahipId = sahipId, UrunId = urunId,
            Tur = "SERTIFIKA", Ad = "eski.pdf", Mime = "application/pdf",
            Boyut = 10, Sha256 = new string('a', 64),
            Icerik = "%PDF-1.4 x"u8.ToArray(),
            YukleyenKullaniciId = "k", Aktif = true, Olusturuldu = DateTime.UtcNow,
        });
        db.SaveChanges();
        return id;
    }

    private static void GeriDoldur(KuyumcuDbContext db)
    {
        foreach (var sql in BelgeSahipGeriDoldurma.Sirayla)
            db.Database.ExecuteSqlRaw(sql);
    }

    private static Belge Oku(KuyumcuDbContext db, string id) =>
        db.Belgeler.AsNoTracking().Single(b => b.Id == id);

    [Fact]
    public void Her_satirda_sahip_id_eski_urun_id_olur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge geri doldurma");
        using var db = vt.OlusturSistem();
        var id = EskiBelge(db, k.TenantId, k.UrunId);

        GeriDoldur(db);

        var satir = Oku(db, id);
        Assert.Equal("URUN", satir.SahipTuru);
        Assert.Equal(k.UrunId, satir.SahipId);
        Assert.Equal(k.UrunId, satir.UrunId);   // eski kolon KORUNUR, veri kaybı yok
    }

    [Fact]
    public void Ayni_SQL_ikinci_kez_kosunca_HICBIR_SATIR_DEGISMEZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge geri doldurma idempotent");
        using var db = vt.OlusturSistem();
        var id = EskiBelge(db, k.TenantId, k.UrunId);

        GeriDoldur(db);
        var birinci = Oku(db, id);
        GeriDoldur(db);
        var ikinci = Oku(db, id);

        Assert.Equal(birinci.SahipTuru, ikinci.SahipTuru);
        Assert.Equal(birinci.SahipId, ikinci.SahipId);
    }

    /// <summary>
    /// ⚠ Aşkın'ın koşulu (M-B10 yerine): WHERE koruması SAHİBİ YAZILMIŞ satıra
    /// DOKUNMAMALIDIR. Koruma kaldırılırsa bu satırın sahibi ezilir ve sessiz
    /// veri bozulması olur.
    /// </summary>
    [Fact]
    public void Sahibi_ZATEN_YAZILMIS_satira_geri_doldurma_DOKUNMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge geri doldurma koruma");
        using var db = vt.OlusturSistem();
        var id = EskiBelge(db, k.TenantId, k.UrunId, sahipTuru: "SIPARIS", sahipId: "SIPARIS-42");

        GeriDoldur(db);

        var satir = Oku(db, id);
        Assert.Equal("SIPARIS", satir.SahipTuru);
        Assert.Equal("SIPARIS-42", satir.SahipId);
    }

    /// <summary>Kanıtı olmayan satır (urun_id boş) YAZILMAZ — tahmin edilmez.</summary>
    [Fact]
    public void Kaniti_olmayan_satir_BOS_KALIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge geri doldurma kanitsiz");
        using var db = vt.OlusturSistem();
        var id = EskiBelge(db, k.TenantId, urunId: "");

        GeriDoldur(db);

        Assert.Equal("", Oku(db, id).SahipId);
    }
}
