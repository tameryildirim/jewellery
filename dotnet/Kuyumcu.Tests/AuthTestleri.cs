using System.Net;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// KİMLİK DOĞRULAMA TESTLERİ — PLAN-2 ölçüm boşluğu kapatması.
//
// NEDEN VAR: 2026-09-13 kapsam ölçümü (coverlet) `AuthServisi` için
// **%6,7 satır kapsamı** gösterdi. Güvenliğin en kritik parçası — giriş,
// kayıt, refresh rotasyonu, çalıntı token tespiti — neredeyse hiç test
// edilmiyordu. Uyum sınıfları %90-100 iken burası %6,7'ydi.
//
// NEDEN HTTP ÜZERİNDEN: `AuthServisi` `SistemKapsami` üzerinden DI'dan
// DbContext çözüyor. Doğrudan örnekleyerek test etmek, üretimdekinden FARKLI
// bir yol kurmak olurdu. HTTP yolu gerçek boru hattının aynısını kullanır —
// ve zaten 2026-09-11'de giriş ucunu 500'e düşüren hata tam da bu kapsam
// çözümlemesinde yaşanmıştı.
//
// ⚠ ORAN SINIRI KISITI: kayıt ucu saatte 3 istekle sınırlı (IP başına).
// Bu yüzden kayıt YALNIZ BİR kez sınanır; giriş/yenileme testleri kullanıcıyı
// doğrudan veritabanına yazar. Aksi hâlde testler kendi oran sınırımıza takılır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class AuthTestleri(HttpFikstur f)
{
    private const string Sifre = "GucluSifre123";

    /// <summary>Kayıt ucunu tüketmeden giriş yapılabilir bir kullanıcı kurar.</summary>
    private (Kiraci Kiraci, string Eposta) KullaniciKur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, ad);
        var eposta = $"auth-{Kimlik.Ulid()}@test.local";
        var kullanici = db.Kullanicilar.Single(x => x.Id == k.KullaniciId);
        kullanici.Email = eposta;
        kullanici.SifreHash = BCrypt.Net.BCrypt.HashPassword(Sifre, 11);
        db.SaveChanges();
        return (k, eposta);
    }

    // ── Kayıt ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Kayit_kiraci_sube_ve_sahip_kullanici_olusturur()
    {
        var eposta = $"kayit-{Kimlik.Ulid()}@test.local";

        // PAKET F-2-min: kayıt artık DAVETLE açılır (merkezî yardımcı).
        string jeton;
        using (var kur = f.Vt.OlusturSistem()) (jeton, _) = TestKurulum.DavetOlustur(kur);

        var cevap = await f.Ortam.AnonimIstemci().PostJson("/api/v1/auth/kayit", new
        {
            isletmeAdi = "Kayıt Testi Kuyumculuk",
            ad = "Ramazan", soyad = "Akkocan",
            email = eposta, sifre = Sifre, davetJetonu = jeton,
        });

        Assert.Equal(HttpStatusCode.Created, cevap.StatusCode);
        var json = await cevap.Json();

        var tenantId = json.GetProperty("tenantId").GetString()!;
        Assert.NotEmpty(json.GetProperty("accessToken").GetString()!);
        Assert.NotEmpty(json.GetProperty("refreshToken").GetString()!);

        using var db = f.Vt.OlusturSistem();

        // Kiracı + merkez şube + SAHİP kullanıcı: üçü birden kurulmalı.
        Assert.True(await db.Tenants.AnyAsync(t => t.Id == tenantId));
        Assert.True(await db.Subeler.AnyAsync(s => s.TenantId == tenantId));

        var kullanici = await db.Kullanicilar.SingleAsync(k => k.TenantId == tenantId);
        Assert.Equal("SAHIP", kullanici.Rol);
        Assert.Equal(eposta, kullanici.Email);

        // ŞİFRE DÜZ METİN OLARAK TUTULMAMALI.
        Assert.NotEqual(Sifre, kullanici.SifreHash);
        Assert.StartsWith("$2", kullanici.SifreHash);          // BCrypt imzası
        Assert.True(BCrypt.Net.BCrypt.Verify(Sifre, kullanici.SifreHash));
    }

    // ── Giriş ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Giris_dogru_sifreyle_token_cifti_doner()
    {
        var (_, eposta) = KullaniciKur("Auth giriş");

        var cevap = await f.Ortam.AnonimIstemci()
            .PostJson("/api/v1/auth/giris", new { email = eposta, sifre = Sifre });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var json = await cevap.Json();
        Assert.NotEmpty(json.GetProperty("accessToken").GetString()!);
        Assert.NotEmpty(json.GetProperty("refreshToken").GetString()!);

        // Yanıt kullanıcı bilgisini döndürür ama ŞİFRE HASH'İNİ ASLA.
        var govde = await cevap.Metin();
        Assert.DoesNotContain("sifreHash", govde, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("$2", govde);
    }

    [Fact]
    public async Task Giris_yanlis_sifreyle_401_doner()
    {
        var (_, eposta) = KullaniciKur("Auth yanlış şifre");

        var cevap = await f.Ortam.AnonimIstemci()
            .PostJson("/api/v1/auth/giris", new { email = eposta, sifre = "YanlisSifre999" });

        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
        // Mesaj hangi alanın yanlış olduğunu SIZDIRMAMALI (kullanıcı sayımı engeli).
        Assert.Equal("E-posta veya şifre hatalı",
            (await cevap.Json()).GetProperty("message").GetString());
    }

    [Fact]
    public async Task Giris_pasif_kullaniciyi_kabul_etmez()
    {
        var (k, eposta) = KullaniciKur("Auth pasif");
        using (var db = f.Vt.OlusturSistem())
        {
            db.Kullanicilar.Single(x => x.Id == k.KullaniciId).Aktif = false;
            db.SaveChanges();
        }

        var cevap = await f.Ortam.AnonimIstemci()
            .PostJson("/api/v1/auth/giris", new { email = eposta, sifre = Sifre });

        Assert.NotEqual(HttpStatusCode.OK, cevap.StatusCode);
    }

    [Fact]
    public async Task Giris_sonrasi_son_giris_tarihi_yazilir()
    {
        var (k, eposta) = KullaniciKur("Auth son giriş");

        await f.Ortam.AnonimIstemci()
            .PostJson("/api/v1/auth/giris", new { email = eposta, sifre = Sifre });

        using var db = f.Vt.OlusturSistem();
        Assert.NotNull(db.Kullanicilar.Single(x => x.Id == k.KullaniciId).SonGiris);
    }

    // ── Refresh token: saklama biçimi ────────────────────────────────────────

    [Fact]
    public async Task Refresh_token_veritabaninda_DUZ_METIN_tutulmaz()
    {
        var (k, eposta) = KullaniciKur("Auth refresh saklama");

        var json = await (await f.Ortam.AnonimIstemci()
            .PostJson("/api/v1/auth/giris", new { email = eposta, sifre = Sifre })).Json();
        var refresh = json.GetProperty("refreshToken").GetString()!;

        using var db = f.Vt.OlusturSistem();
        var kayitlar = await db.RefreshTokenler.AsNoTracking()
            .Where(t => t.TenantId == k.TenantId).ToListAsync();

        Assert.NotEmpty(kayitlar);
        // Düz metin hiçbir satırda bulunmamalı; yalnız SHA-256 hash'i tutulur.
        Assert.DoesNotContain(kayitlar, t => t.TokenHash == refresh);
        Assert.Contains(kayitlar, t => t.TokenHash == TokenServisi.Hashle(refresh));
    }

    // ── Rotasyon ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Yenileme_yeni_cift_uretir_ve_eskiyi_iptal_eder()
    {
        var (_, eposta) = KullaniciKur("Auth rotasyon");
        var istemci = f.Ortam.AnonimIstemci();

        var giris = await (await istemci
            .PostJson("/api/v1/auth/giris", new { email = eposta, sifre = Sifre })).Json();
        var eskiRefresh = giris.GetProperty("refreshToken").GetString()!;

        var yenile = await istemci.PostJson("/api/v1/auth/yenile", new { refreshToken = eskiRefresh });
        Assert.Equal(HttpStatusCode.OK, yenile.StatusCode);

        var yeni = await yenile.Json();
        var yeniRefresh = yeni.GetProperty("refreshToken").GetString()!;
        Assert.NotEqual(eskiRefresh, yeniRefresh);

        using var db = f.Vt.OlusturSistem();
        var eski = await db.RefreshTokenler.AsNoTracking()
            .SingleAsync(t => t.TokenHash == TokenServisi.Hashle(eskiRefresh));
        Assert.True(eski.IptalEdildi, "Rotasyondan sonra eski token İPTAL edilmeliydi");
    }

    [Fact]
    public async Task Gecersiz_refresh_token_401_doner()
    {
        // ⚠ RefreshDto 43-64 karakter şartı koyar; kısa bir dizge servise HİÇ
        // ulaşmadan 400 alır. 401 yolunu sınamak için BİÇİMİ GEÇERLİ ama
        // veritabanında KARŞILIĞI OLMAYAN bir token gerekir.
        var bicimGecerliAmaYok = new string('a', 48);

        var cevap = await f.Ortam.AnonimIstemci()
            .PostJson("/api/v1/auth/yenile", new { refreshToken = bicimGecerliAmaYok });

        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
    }

    [Fact]
    public async Task Bicimsiz_kisa_refresh_token_400_doner()
    {
        // Doğrulama servise varmadan devreye girer — savunma derinliği.
        var cevap = await f.Ortam.AnonimIstemci()
            .PostJson("/api/v1/auth/yenile", new { refreshToken = "kisa" });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Contains("Geçersiz refresh token",
            (await cevap.Json()).GetProperty("message").EnumerateArray().First().GetString());
    }

    // ── ÇALINTI TESPİTİ — güvenliğin kalbi ───────────────────────────────────

    [Fact]
    public async Task Iptal_edilmis_token_yeniden_kullanilirsa_TUM_oturumlar_kapanir()
    {
        // Senaryo: saldırgan refresh token'ı çalar. Gerçek kullanıcı yenileme
        // yapınca token rotate olur (eski iptal edilir). Saldırgan ÇALINMIŞ eski
        // token'ı kullanmaya çalıştığında, sistem bunu yeniden kullanım olarak
        // görüp kullanıcının TÜM tokenlerini iptal etmelidir.
        var (k, eposta) = KullaniciKur("Auth çalıntı tespiti");
        var istemci = f.Ortam.AnonimIstemci();

        var giris = await (await istemci
            .PostJson("/api/v1/auth/giris", new { email = eposta, sifre = Sifre })).Json();
        var calinan = giris.GetProperty("refreshToken").GetString()!;

        // Gerçek kullanıcı yeniler → `calinan` iptal olur, yerine yeni token gelir.
        var mesru = await (await istemci
            .PostJson("/api/v1/auth/yenile", new { refreshToken = calinan })).Json();
        var mesruRefresh = mesru.GetProperty("refreshToken").GetString()!;

        // Saldırgan çalınmış (artık iptal) token'ı kullanmaya çalışır.
        var saldiri = await istemci.PostJson("/api/v1/auth/yenile", new { refreshToken = calinan });
        Assert.Equal(HttpStatusCode.Unauthorized, saldiri.StatusCode);

        // KRİTİK: meşru kullanıcının token'ı da iptal edilmiş olmalı.
        using var db = f.Vt.OlusturSistem();
        var hepsi = await db.RefreshTokenler.AsNoTracking()
            .Where(t => t.KullaniciId == k.KullaniciId).ToListAsync();

        Assert.All(hepsi, t => Assert.True(t.IptalEdildi,
            "Çalıntı tespitinde kullanıcının TÜM tokenleri iptal edilmeliydi"));

        // Ve meşru token da artık çalışmamalı.
        var mesruDeneme = await istemci
            .PostJson("/api/v1/auth/yenile", new { refreshToken = mesruRefresh });
        Assert.Equal(HttpStatusCode.Unauthorized, mesruDeneme.StatusCode);
    }

    // ── Çıkış ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cikis_refresh_tokeni_gecersiz_kilar()
    {
        var (_, eposta) = KullaniciKur("Auth çıkış");
        var istemci = f.Ortam.AnonimIstemci();

        var giris = await (await istemci
            .PostJson("/api/v1/auth/giris", new { email = eposta, sifre = Sifre })).Json();
        var refresh = giris.GetProperty("refreshToken").GetString()!;

        var cikis = await istemci.PostJson("/api/v1/auth/cikis", new { refreshToken = refresh });
        Assert.Equal(HttpStatusCode.NoContent, cikis.StatusCode);

        var yenile = await istemci.PostJson("/api/v1/auth/yenile", new { refreshToken = refresh });
        Assert.Equal(HttpStatusCode.Unauthorized, yenile.StatusCode);
    }

    // ── /auth/ben ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Ben_ucu_oturum_sahibini_doner_ve_sifre_sizdirmaz()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "Auth ben");

        var cevap = await f.Ortam.Istemci(k).GetAsync("/api/v1/auth/ben");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        var govde = await cevap.Metin();
        Assert.Contains(k.TenantId, govde);
        Assert.DoesNotContain("sifreHash", govde, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("$2", govde);
    }

    [Fact]
    public async Task Ben_ucu_tokensiz_401_doner()
    {
        var cevap = await f.Ortam.AnonimIstemci().GetAsync("/api/v1/auth/ben");
        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
    }

    // ── Kiracılar arası e-posta tekilliği ────────────────────────────────────

    [Fact]
    public async Task Ayni_eposta_ikinci_kez_kayit_edilemez()
    {
        // Kayıt ucu saatte 3 istekle sınırlı; bu test 1 slot harcar ve
        // ilk kaydı doğrudan veritabanına yazarak ikinci slotu korur.
        var eposta = $"tekil-{Kimlik.Ulid()}@test.local";
        using (var db = f.Vt.OlusturSistem())
        {
            var k = TestKurulum.KiraciKur(db, "Auth tekillik");
            var kullanici = db.Kullanicilar.Single(x => x.Id == k.KullaniciId);
            kullanici.Email = eposta;
            db.SaveChanges();
        }

        string jeton;
        using (var kur = f.Vt.OlusturSistem()) (jeton, _) = TestKurulum.DavetOlustur(kur);

        var cevap = await f.Ortam.AnonimIstemci().PostJson("/api/v1/auth/kayit", new
        {
            isletmeAdi = "İkinci İşletme", ad = "A", soyad = "B",
            email = eposta, sifre = Sifre, davetJetonu = jeton,
        });

        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
    }
}
