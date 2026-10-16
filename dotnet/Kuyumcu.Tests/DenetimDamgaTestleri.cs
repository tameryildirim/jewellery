using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// BAĞLAM DAMGASI — paket 48 Faz A.
//
// Üç şubeli bir dükkânda denetim izi "kim, ne zaman, neyi" diyordu ama
// "NEREDE" demiyordu. Bu testler damganın düştüğünü ve — daha önemlisi —
// BİLİNMEDİĞİNDE UYDURULMADIĞINI bekçiler.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class DenetimDamgaTestleri(TestVeritabani vt)
{
    private static DenetimKaydi Iz(KuyumcuDbContext sistem, string tenantId, string varlikId) =>
        sistem.DenetimKayitlari.AsNoTracking()
            .Single(d => d.TenantId == tenantId && d.VarlikId == varlikId);

    [Fact]
    public async Task Hareket_yazilinca_sube_damgalanir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Damga-hareket");

        using var db = vt.Olustur(k);
        var id = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "EMANET_GIRIS",
            Tarih = DateTime.UtcNow, TlTutari = 0m, HasGrami = 1m,
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(k.SubeId, Iz(kontrol, k.TenantId, id).SubeId);
    }

    [Fact]
    public async Task Kasa_hareketi_yazilinca_kasa_damgalanir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Damga-kasa");

        using var db = vt.Olustur(k);
        var id = Kimlik.Ulid();
        db.KasaHareketleri.Add(new KasaHareket
        {
            Id = id, TenantId = k.TenantId, KasaId = k.KasaId, HareketId = null,
            Tutar = 100m, ParaBirimi = "TRY", OdemeTuru = "NAKIT",
            Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(k.KasaId, Iz(kontrol, k.TenantId, id).KasaId);
    }

    [Fact]
    public async Task Gun_kapanisi_yazilinca_hem_sube_hem_kasa_damgalanir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Damga-kapanis");

        using var db = vt.Olustur(k);
        var id = Kimlik.Ulid();
        db.GunKapanislari.Add(new GunKapanisi
        {
            Id = id, TenantId = k.TenantId, KasaId = k.KasaId, SubeId = k.SubeId,
            YerelGun = DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow)),
            DefterBakiye = 0m, SayilanNakit = 0m, Fark = 0m,
            KapatanKullaniciId = k.KullaniciId, KapanisZamani = DateTime.UtcNow,
            Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        using var kontrol = vt.OlusturSistem();
        var iz = Iz(kontrol, k.TenantId, id);
        Assert.Equal(k.SubeId, iz.SubeId);
        Assert.Equal(k.KasaId, iz.KasaId);
    }

    [Fact]
    public async Task Varlikta_sube_yoksa_oturumun_sube_claimi_kullanilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Damga-claim");

        // Cari'nin şubesi YOKTUR; damga oturumun "subeId" claim'inden gelmelidir.
        var erisim = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        erisim.HttpContext!.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity([
                new System.Security.Claims.Claim("sub", k.KullaniciId),
                new System.Security.Claims.Claim("subeId", k.SubeId),
            ], "test"));

        using var db = vt.Olustur(k, erisim);
        var id = Kimlik.Ulid();
        db.Cariler.Add(new Cari
        {
            Id = id, TenantId = k.TenantId, Tur = "MUSTERI", Ad = "Damga müşterisi",
            Aktif = true, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(k.SubeId, Iz(kontrol, k.TenantId, id).SubeId);
    }

    [Fact]
    public async Task Sube_belirlenemezse_NULL_yazilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Damga-null");

        // Şube claim'i YOK, varlıkta şube YOK → damga NULL kalmalıdır.
        // "İlk şubeyi yaz" mutasyonu bu testi kırar (Anayasa md.1).
        using var db = vt.Olustur(k);
        var id = Kimlik.Ulid();
        db.Cariler.Add(new Cari
        {
            Id = id, TenantId = k.TenantId, Tur = "MUSTERI", Ad = "Damgasız müşteri",
            Aktif = true, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        using var kontrol = vt.OlusturSistem();
        var iz = Iz(kontrol, k.TenantId, id);
        Assert.Null(iz.SubeId);
        Assert.Null(iz.KasaId);
    }

    [Fact]
    public async Task Damga_append_only_korumasini_BOZMAZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Damga-appendonly");

        using var db = vt.Olustur(k);
        db.Cariler.Add(new Cari
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, Tur = "MUSTERI", Ad = "İz doğuran",
            Aktif = true, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        // Yeni kolonlar append-only yasağını GEVŞETMEZ: damgayı sonradan
        // "düzeltmek" de yasaktır.
        using var db2 = vt.Olustur(k);
        var iz = db2.DenetimKayitlari.First(d => d.TenantId == k.TenantId);
        iz.SubeId = k.SubeId;
        Assert.Throws<DenetimIziDegistirilemez>(() => db2.SaveChanges());
    }

    [Fact]
    public async Task Okuma_izi_de_oturumun_subesini_damgalar()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Damga-okuma");

        using var db = vt.Olustur(k);
        await new DenetimServisi(db, new HttpContextAccessor())
            .OkumaKaydet(k.Islem, nameof(Cari), k.CariId, "damga testi");

        using var kontrol = vt.OlusturSistem();
        var iz = kontrol.DenetimKayitlari.AsNoTracking()
            .Single(d => d.TenantId == k.TenantId && d.Eylem == DenetimEylemi.Okuma);
        Assert.Equal(k.SubeId, iz.SubeId);
    }
}
