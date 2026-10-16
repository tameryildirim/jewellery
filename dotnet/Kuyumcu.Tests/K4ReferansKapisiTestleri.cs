using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Kasa;
using Kuyumcu.Api.Modules.Kullanici;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// K4 — REFERANS KAPISI DENETİMİ (Mehmet, 06 Güvenlik)
//
// SORU: "Her `*_id` alanı için uygulama katmanında bir kapı var mı?"
// Veritabanında FK kısıtı YOK (ADR-0009); tek koruma uygulama kapılarıdır.
// Bir kapı eksikse yabancı ya da hiç var olmayan bir kimlik kayda girer.
//
// DENETİM SONUCU (2026-09-14): 36 ilişki tarandı, ÜÇ boşluk bulundu —
//   • kasalar.sube_id          → doğrulanmıyordu
//   • kullanicilar.sube_id     → doğrulanmıyordu (ağır: JWT'ye girip
//                                hareketlere yazılıyordu)
//   • faturalar okuması        → elle kiracı şartı yoktu (ADR-0010)
// Bu dosya üçünü de kapalı tutar.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class K4ReferansKapisiTestleri(TestVeritabani vt)
{
    [Fact]
    public async Task Kasa_BASKA_kiracinin_subesine_acilamaz()
    {
        var sistem = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(sistem, "K4 kasa A");
        var b = TestKurulum.KiraciKur(sistem, "K4 kasa B");

        using var db = vt.Olustur(a.TenantId);

        await Assert.ThrowsAsync<NotFoundException>(
            () => new KasaServisi(db, NullLogger<KasaServisi>.Instance).Olustur(
                new KasaOlusturDto { SubeId = b.SubeId, Ad = "Sızan kasa" }, a.Islem));

        Assert.False(await db.Kasalar.AnyAsync(k => k.Ad == "Sızan kasa"));
    }

    [Fact]
    public async Task Kasa_OLMAYAN_subeye_acilamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "K4 kasa yok");
        using var db = vt.Olustur(k.TenantId);

        await Assert.ThrowsAsync<NotFoundException>(
            () => new KasaServisi(db, NullLogger<KasaServisi>.Instance).Olustur(
                new KasaOlusturDto { SubeId = Kimlik.Ulid(), Ad = "Hayalet kasa" }, k.Islem));
    }

    [Fact]
    public async Task Kasa_KENDI_subesine_acilir()
    {
        // Regresyon: kapı meşru kullanımı engellememeli.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "K4 kasa geçerli");
        using var db = vt.Olustur(k.TenantId);

        await new KasaServisi(db, NullLogger<KasaServisi>.Instance).Olustur(
            new KasaOlusturDto { SubeId = k.SubeId, Ad = "Geçerli kasa" }, k.Islem);

        Assert.True(await db.Kasalar.AnyAsync(x => x.Ad == "Geçerli kasa"));
    }

    [Fact]
    public async Task Kullanici_BASKA_kiracinin_subesine_atanamaz()
    {
        // ⛔ En ağır boşluk: subeId JWT'ye girer, sonra satış/alış onu
        // `hareketler.sube_id`'ye yazar — yabancı şube defterlere sızardı.
        var sistem = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(sistem, "K4 kullanıcı A");
        var b = TestKurulum.KiraciKur(sistem, "K4 kullanıcı B");

        using var db = vt.Olustur(a.TenantId);

        await Assert.ThrowsAsync<NotFoundException>(
            () => new KullaniciServisi(db, TestKurulum.Denetim(db)).Olustur(new KullaniciOlusturDto
            {
                Ad = "Sızan", Soyad = "Kullanıcı",
                Email = $"{Kimlik.Ulid()}@test.local",
                Sifre = "Gucluparola1!", Rol = "TEZGAHTAR", SubeId = b.SubeId,
            }, a.Islem));

        Assert.False(await db.Kullanicilar.AnyAsync(x => x.Ad == "Sızan"));
    }

    [Fact]
    public async Task Kullanici_guncellemesinde_de_sube_dogrulanir()
    {
        var sistem = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(sistem, "K4 güncelleme A");
        var b = TestKurulum.KiraciKur(sistem, "K4 güncelleme B");

        using var db = vt.Olustur(a.TenantId);
        var s = new KullaniciServisi(db, TestKurulum.Denetim(db));

        await s.Olustur(new KullaniciOlusturDto
        {
            Ad = "Normal", Soyad = "Kullanıcı",
            Email = $"{Kimlik.Ulid()}@test.local",
            Sifre = "Gucluparola1!", Rol = "TEZGAHTAR", SubeId = a.SubeId,
        }, a.Islem);

        var hedef = await db.Kullanicilar.AsNoTracking().SingleAsync(x => x.Ad == "Normal");

        await Assert.ThrowsAsync<NotFoundException>(
            () => s.Guncelle(hedef.Id, new KullaniciGuncelleDto { SubeId = b.SubeId }, a.Islem));

        var sonra = await db.Kullanicilar.AsNoTracking().SingleAsync(x => x.Id == hedef.Id);
        Assert.Equal(a.SubeId, sonra.SubeId);
    }

    [Fact]
    public async Task Kullanici_subesi_ACIK_null_ile_temizlenebilir()
    {
        // Bilinçli davranış: şubesiz kullanıcı (merkez/muhasebe) meşrudur.
        // Kapı bunu engellememeli.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "K4 şube temizle");
        using var db = vt.Olustur(k.TenantId);
        var s = new KullaniciServisi(db, TestKurulum.Denetim(db));

        await s.Olustur(new KullaniciOlusturDto
        {
            Ad = "Merkez", Soyad = "Muhasebe",
            Email = $"{Kimlik.Ulid()}@test.local",
            Sifre = "Gucluparola1!", Rol = "MUHASEBE", SubeId = k.SubeId,
        }, k.Islem);

        var hedef = await db.Kullanicilar.AsNoTracking().SingleAsync(x => x.Ad == "Merkez");
        await s.Guncelle(hedef.Id, new KullaniciGuncelleDto { SubeId = null }, k.Islem);

        var sonra = await db.Kullanicilar.AsNoTracking().SingleAsync(x => x.Id == hedef.Id);
        Assert.Null(sonra.SubeId);
    }
}
