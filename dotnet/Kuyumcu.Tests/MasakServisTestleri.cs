using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MMasak = Kuyumcu.Api.Modules.Masak;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// MasakServisi — uçların arkasındaki servis. Mehmet md.2 (maskeleme), md.4 (okuma
// izi), şüpheli işlem yaşam döngüsü (silinmez, kapatılır), 10 iş günü son tarih.
// Rol kısıtı [RolGerekli] controller filtresidir; canlı HTTP ile ayrıca kanıtlandı.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MasakServisTestleri(TestVeritabani vt)
{
    private const string Tckn = "10000000146";

    private static MMasak.MasakServisi Servis(KuyumcuDbContext db) =>
        new(db, new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri()), new DenetimServisi(db, new HttpContextAccessor()));

    private static MasakKimlikTespiti Tespit(Kiraci k, string belgeNo, decimal tutar = 250_000m) => new()
    {
        Id = Kimlik.Ulid(), TenantId = k.TenantId, CariId = k.CariId, IslemTutariTl = tutar,
        BelgeTuru = KimlikBelgeTuru.TcKimlik, BelgeNo = belgeNo, AdSoyad = "Test Kişi",
        TespitEdenKullaniciId = k.KullaniciId, Tarih = DateTime.UtcNow,
        SaklamaBitis = DateTime.UtcNow.AddYears(8), Olusturuldu = DateTime.UtcNow,
    };

    /// <summary>MASAK asgari 300 karakter (Rehber 2.0 §4.2.7) — testler bunu taklit eder.</summary>
    private static string Uzun(string cekirdek) =>
        (cekirdek + " ").PadRight(320, '.') + " Bu metin test amaçlıdır ve asgari uzunluğu sağlar.";

    private static string Json(object o) => System.Text.Json.JsonSerializer.Serialize(o);
    private static System.Text.Json.JsonElement Kok(object o) => System.Text.Json.JsonDocument.Parse(Json(o)).RootElement;

    // ── Maskeleme (Mehmet md.2) ──────────────────────────────────────────────

    [Theory]
    [InlineData("10000000146", "*******0146")]
    [InlineData("AB123456", "****3456")]
    [InlineData("1234", "****")]
    [InlineData("12", "**")]
    public void Maskele_Son4HaneHaricGizler(string girdi, string beklenen)
    {
        Assert.Equal(beklenen, MMasak.MasakServisi.Maskele(girdi));
    }

    [Fact]
    public async Task Liste_BelgeNoMaskeli_TamDegerYok()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MSrv-liste");
        using var db = vt.Olustur(k);
        db.MasakKimlikTespitleri.Add(Tespit(k, Tckn));
        await db.SaveChangesAsync();

        var json = Json(await Servis(db).TespitleriListele(k.Islem, null, null, 100));

        Assert.Contains("*******0146", json);
        Assert.DoesNotContain(Tckn, json);
    }

    [Fact]
    public async Task Tekil_TamBelgeNoDoner_VeOkumaIziDuser()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MSrv-tekil");
        using var db = vt.Olustur(k);
        var t = Tespit(k, Tckn);
        db.MasakKimlikTespitleri.Add(t);
        await db.SaveChangesAsync();

        var sonuc = Kok(await Servis(db).TespitGetir(t.Id, k.Islem));
        Assert.Equal(Tckn, sonuc.GetProperty("BelgeNo").GetString());

        using var kontrol = vt.OlusturSistem();
        var iz = kontrol.DenetimKayitlari.AsNoTracking()
            .Single(d => d.TenantId == k.TenantId && d.Eylem == DenetimEylemi.Okuma && d.VarlikId == t.Id);
        Assert.Equal(nameof(MasakKimlikTespiti), iz.VarlikTuru);
        Assert.True(iz.Hassas);
        Assert.Equal(k.KullaniciId, iz.KullaniciId);
        Assert.DoesNotContain(Tckn, iz.Detay ?? "");   // izde TCKN yok
    }

    [Fact]
    public async Task Liste_OkumaIziAdetleDuser()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MSrv-listeiz");
        using var db = vt.Olustur(k);
        db.MasakKimlikTespitleri.Add(Tespit(k, Tckn));
        db.MasakKimlikTespitleri.Add(Tespit(k, "99999999990"));
        await db.SaveChangesAsync();

        await Servis(db).TespitleriListele(k.Islem, null, null, 100);

        using var kontrol = vt.OlusturSistem();
        var iz = kontrol.DenetimKayitlari.AsNoTracking()
            .Single(d => d.TenantId == k.TenantId && d.Eylem == DenetimEylemi.Okuma);
        Assert.Contains("liste: 2 kayıt", iz.Detay);
    }

    [Fact]
    public async Task Tekil_BaskaKiracininKaydi_Bulunamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "MSrv-A");
        var b = TestKurulum.KiraciKur(kurulum, "MSrv-B");
        using var aDb = vt.Olustur(a);
        var t = Tespit(a, Tckn);
        aDb.MasakKimlikTespitleri.Add(t);
        await aDb.SaveChangesAsync();

        using var bDb = vt.Olustur(b);
        await Assert.ThrowsAsync<NotFoundException>(() => Servis(bDb).TespitGetir(t.Id, b.Islem));
    }

    // ── Hash ile arama ───────────────────────────────────────────────────────

    [Fact]
    public async Task Ara_BosluklarFarkEtmez_ToplamVeAdetDogru()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MSrv-ara");
        using var db = vt.Olustur(k);
        db.MasakKimlikTespitleri.Add(Tespit(k, Tckn, 200_000m));
        db.MasakKimlikTespitleri.Add(Tespit(k, "100 000 001 46", 300_000m));
        db.MasakKimlikTespitleri.Add(Tespit(k, "99999999990", 999_999m));
        await db.SaveChangesAsync();

        var s = Kok(await Servis(db).BelgeNoIleAra("10000000146", k.Islem));
        Assert.Equal(2, s.GetProperty("adet").GetInt32());
        Assert.Equal(500_000m, s.GetProperty("toplamTutarTl").GetDecimal());
    }

    [Fact]
    public async Task Ara_BosBelgeNo_Reddedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MSrv-arabos");
        using var db = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).BelgeNoIleAra("  ", k.Islem));
    }

    // ── Şüpheli işlem yaşam döngüsü ──────────────────────────────────────────

    [Fact]
    public async Task SupheliOlustur_SonTarih10IsGunu_DurumAcik()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MSrv-supheli");
        using var db = vt.Olustur(k);

        var s = Kok(await Servis(db).SupheliOlustur(new MMasak.SupheliBildirimDto
        {
            CariId = k.CariId, IslemTutariTl = 50_000m,   // eşik ALTI — şüphe tutardan bağımsız
            Gerekce = Uzun("Müşteri işlemin kaynağını açıklamaktan kaçındı, tutarsız beyan"),
        }, k.Islem));

        Assert.Equal(SupheliDurum.Acik, s.GetProperty("Durum").GetString());
        var tespit = s.GetProperty("TespitTarihi").GetDateTime();
        var son = s.GetProperty("SonBildirimTarihi").GetDateTime();
        var isGunu = 0; for (var g = tespit.Date.AddDays(1); g <= son.Date; g = g.AddDays(1))
            if (g.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) isGunu++;
        Assert.InRange(isGunu, 10, 11);   // yerel/UTC gün sınırına göre 10 ya da 11 görünebilir; 10 iş günü hedefi
        Assert.True(s.GetProperty("kalanSaat").GetDouble() > 0);
    }

    [Fact]
    public async Task Supheli_GonderildiIsaretlenir_SonraTekrarGonderilemez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MSrv-gonder");
        using var db = vt.Olustur(k);
        var servis = Servis(db);
        var id = Kok(await servis.SupheliOlustur(new MMasak.SupheliBildirimDto
            { Gerekce = Uzun("Parçalı ödemelerle eşiğin altında kalma çabası gözlendi") }, k.Islem)).GetProperty("Id").GetString()!;

        var g = Kok(await servis.SupheliGonderildi(id, new MMasak.GonderildiDto { GonderimReferansi = "MASAK-2026-000123" }, k.Islem));
        Assert.Equal(SupheliDurum.Gonderildi, g.GetProperty("Durum").GetString());
        Assert.Equal("MASAK-2026-000123", g.GetProperty("GonderimReferansi").GetString());

        await Assert.ThrowsAsync<BadRequestException>(() =>
            servis.SupheliGonderildi(id, new MMasak.GonderildiDto { GonderimReferansi = "X" }, k.Islem));
    }

    [Fact]
    public async Task Supheli_Kapat_SilmezGerekceyleKapatir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MSrv-kapat");
        using var db = vt.Olustur(k);
        var servis = Servis(db);
        var id = Kok(await servis.SupheliOlustur(new MMasak.SupheliBildirimDto
            { Gerekce = Uzun("Kimlik belgesindeki fotoğraf kişiyle uyuşmuyor gibi göründü") }, k.Islem)).GetProperty("Id").GetString()!;

        await servis.SupheliKapat(id, new MMasak.KapatDto { Gerekce = "Müşteri ikinci belgeyle kimliğini teyit etti" }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kayit = kontrol.MasakSupheliBildirimler.AsNoTracking().Single(s => s.Id == id);   // SİLİNMEDİ
        Assert.Equal(SupheliDurum.Kapatildi, kayit.Durum);
        Assert.Contains("ikinci belgeyle", kayit.KapatmaGerekcesi);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            servis.SupheliKapat(id, new MMasak.KapatDto { Gerekce = "İkinci kez kapatma denemesi yapılıyor" }, k.Islem));
    }

    [Fact]
    public async Task Supheli_GecersizDurumFiltresi_Reddedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MSrv-durum");
        using var db = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).SupheliListele(k.Islem, "YOK_BOYLE"));
    }

    [Fact]
    public async Task Supheli_YabanciHareket_Reddedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "MSrv-yabA");
        var b = TestKurulum.KiraciKur(kurulum, "MSrv-yabB");
        // A'da bir hareket yok; B rastgele bir id ile deniyor → NotFound
        using var bDb = vt.Olustur(b);
        await Assert.ThrowsAsync<NotFoundException>(() => Servis(bDb).SupheliOlustur(new MMasak.SupheliBildirimDto
            { HareketId = Kimlik.Ulid(), Gerekce = Uzun("Var olmayan bir hareket için deneme kaydı açılıyor") }, b.Islem));
    }

    // ── Özet ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Ozet_AcikVeSon30GunSayilari()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MSrv-ozet");
        using var db = vt.Olustur(k);
        var servis = Servis(db);
        db.MasakKimlikTespitleri.Add(Tespit(k, Tckn));
        await db.SaveChangesAsync();
        await servis.SupheliOlustur(new MMasak.SupheliBildirimDto { Gerekce = Uzun("Özet testi için açık şüpheli bildirim kaydı") }, k.Islem);

        var o = Kok(await servis.Ozet(k.Islem));
        Assert.Equal(1, o.GetProperty("acikBildirim").GetInt32());
        Assert.Equal(0, o.GetProperty("suresiGecmis").GetInt32());
        Assert.Equal(1, o.GetProperty("son30GunTespit").GetInt32());
    }

    [Fact]
    public void Esikler_KisiselVeriIcermez_BelgeTurleriniVerir()
    {
        var e = Kok(new MMasak.MasakServisi(null!, new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri()), null!).Esikler());
        Assert.Equal(370_000m, e.GetProperty("kimlikEsigi").GetDecimal());   // MASAK-370: varsayılan RG 07.10.2026/33393
        Assert.Equal(5, e.GetProperty("belgeTurleri").GetArrayLength());
    }

    [Fact]
    public async Task Supheli_300KarakterAltiAciklama_Reddedilir()
    {
        // Rehber 2.0 §4.2.7: asgari 300 karakter. İlk sürümde 20 yazılmıştı — birincil kaynakla düzeltildi.
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MSrv-300");
        using var db = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).SupheliOlustur(
            new MMasak.SupheliBildirimDto { Gerekce = new string('a', 299) }, k.Islem));
        Assert.Contains("300", ex.Message);

        // Boşlukla şişirme işe yaramaz
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).SupheliOlustur(
            new MMasak.SupheliBildirimDto { Gerekce = new string('a', 100) + new string(' ', 250) }, k.Islem));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// ÇAPRAZ KİRACI REFERANS ENJEKSİYONU — 2026-09-13 güvenlik bulgusu.
//
// BULGU (Mehmet, ADR-0009 oylaması sırasında): `SupheliOlustur` içinde
// `HareketId` ve `CariId` kiracıya karşı doğrulanıyordu, ama `KimlikTespitiId`
// kullanıcı gövdesinden gelip DOĞRULANMADAN yazılıyordu.
//
// Etkisi: A kiracısı, B kiracısının kimlik tespiti kaydının kimliğini kendi
// APPEND-ONLY şüpheli işlem bildirimine bağlayabiliyordu. Kayıt append-only
// olduğu için sonradan düzeltilemezdi.
//
// Kiracı sorgu filtresi bunu ENGELLEMEZ: filtre okumada çalışır; INSERT'te
// yazılan ham kimlik değerini denetlemez. Bu, ADR-0009'daki "basit FK kiracı
// sınırını korumaz, kompozit FK gerekir" tespitinin uygulama katmanındaki
// karşılığıdır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MasakCaprazKiraciTestleri(TestVeritabani vt)
{
    private static MMasak.MasakServisi Servis(KuyumcuDbContext db) =>
        new(db, new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri()),
            new DenetimServisi(db, new HttpContextAccessor()));

    private static MMasak.SupheliBildirimDto Bildirim(string? kimlikTespitiId) => new()
    {
        IslemTutariTl = 250_000m,
        Gerekce = new string('a', 320),
        KimlikTespitiId = kimlikTespitiId,
    };

    /// <summary>A kiracısının kimlik tespiti kendi bildirimine bağlanabilmeli.</summary>
    [Fact]
    public async Task Kendi_kimlik_tespiti_bildirime_baglanabilir()
    {
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "Çapraz A meşru");

        string tespitId;
        using (var db = vt.Olustur(a.TenantId))
        {
            var tespit = new MasakKimlikTespiti
            {
                Id = Kimlik.Ulid(), TenantId = a.TenantId,
                IslemTutariTl = 250_000m, BelgeTuru = KimlikBelgeTuru.TcKimlik,
                BelgeNo = "10000000146", AdSoyad = "Ayşe Demir",
                TespitEdenKullaniciId = a.KullaniciId,
                Tarih = DateTime.UtcNow, SaklamaBitis = DateTime.UtcNow.AddYears(8),
                Olusturuldu = DateTime.UtcNow,
            };
            db.MasakKimlikTespitleri.Add(tespit);
            await db.SaveChangesAsync();
            tespitId = tespit.Id;
        }

        using var db2 = vt.Olustur(a.TenantId);
        var sonuc = await Servis(db2).SupheliOlustur(Bildirim(tespitId), a.Islem);
        Assert.NotNull(sonuc);
    }

    /// <summary>BAŞKA kiracının kimlik tespiti REDDEDİLMELİ.</summary>
    [Fact]
    public async Task Baska_kiracinin_kimlik_tespiti_bildirime_BAGLANAMAZ()
    {
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "Çapraz A saldıran");
        var b = TestKurulum.KiraciKur(vt.OlusturSistem(), "Çapraz B kurban");

        // B kiracısının kimlik tespiti — A bunu asla görememeli ve bağlayamamalı.
        string bTespitId;
        using (var db = vt.Olustur(b.TenantId))
        {
            var tespit = new MasakKimlikTespiti
            {
                Id = Kimlik.Ulid(), TenantId = b.TenantId,
                IslemTutariTl = 190_000m, BelgeTuru = KimlikBelgeTuru.TcKimlik,
                BelgeNo = "10000000146", AdSoyad = "Kurban Kiracı Müşterisi",
                TespitEdenKullaniciId = b.KullaniciId,
                Tarih = DateTime.UtcNow, SaklamaBitis = DateTime.UtcNow.AddYears(8),
                Olusturuldu = DateTime.UtcNow,
            };
            db.MasakKimlikTespitleri.Add(tespit);
            await db.SaveChangesAsync();
            bTespitId = tespit.Id;
        }

        using var dbA = vt.Olustur(a.TenantId);
        await Assert.ThrowsAsync<NotFoundException>(
            () => Servis(dbA).SupheliOlustur(Bildirim(bTespitId), a.Islem));

        // Ve hiçbir bildirim yazılmamış olmalı.
        Assert.Empty(dbA.MasakSupheliBildirimler.Where(s => s.TenantId == a.TenantId));
    }

    /// <summary>Var olmayan kimlik tespiti de reddedilir (yetim referans).</summary>
    [Fact]
    public async Task Var_olmayan_kimlik_tespiti_reddedilir()
    {
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "Çapraz yetim");

        using var db = vt.Olustur(a.TenantId);
        await Assert.ThrowsAsync<NotFoundException>(
            () => Servis(db).SupheliOlustur(Bildirim(Kimlik.Ulid()), a.Islem));
    }

    /// <summary>Kimlik tespiti VERİLMEZSE bildirim yine de oluşur (opsiyonel alan).</summary>
    [Fact]
    public async Task Kimlik_tespiti_olmadan_bildirim_olusabilir()
    {
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "Çapraz opsiyonel");

        using var db = vt.Olustur(a.TenantId);
        Assert.NotNull(await Servis(db).SupheliOlustur(Bildirim(null), a.Islem));
    }
}
