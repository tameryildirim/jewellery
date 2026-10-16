using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 45 — GERÇEK HTTP testleri (Mehmet'in VETO koşulu burada kapanır).
//
// Sınananlar: MFA etkinken girişin token DÖNDÜRMEMESİ, meydan okuma jetonunun
// bearer olarak KABUL EDİLMEMESİ (audience), access token'ın ikinci adımdan
// GEÇEMEMESİ, ömür/parola sürümü kapıları, kurtarma kodu tek kullanımlık,
// oran sınırı (429 + Retry-After).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class MfaHttpTestleri(HttpFikstur fikstur)
{
    private const string Sifre = "Parola12345";
    private static readonly Kuyumcu.Api.Modules.Auth.MfaAyarlari Ayarlar = new();

    private static TokenServisi Jetonlar => new(new JwtAyarlar
    {
        Secret = HttpOrtami.TestSirri, AccessDakika = 15, RefreshGun = 7,
    });

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    /// <summary>MFA'sı ETKİN bir kullanıcı kurar; e-postasını ve TOTP gizini döner.</summary>
    private (Kiraci Kiraci, string Eposta, string Giz) MfaliKullanici(string ad, string? gizB32 = null)
    {
        using var db = fikstur.Vt.OlusturSistem();
        var kiraci = TestKurulum.KiraciKur(db, $"P45-{ad}-{Guid.NewGuid():N}"[..24]);
        var kullanici = db.Kullanicilar.Single(k => k.Id == kiraci.KullaniciId);
        kullanici.SifreHash = BCrypt.Net.BCrypt.HashPassword(Sifre, 12);

        var giz = gizB32 ?? Base32.Kodla(System.Security.Cryptography.RandomNumberGenerator.GetBytes(20));
        var now = DateTime.UtcNow;
        db.KullaniciMfalari.Add(new KullaniciMfa
        {
            Id = Kimlik.Ulid(),
            TenantId = kiraci.TenantId,
            KullaniciId = kiraci.KullaniciId,
            Yontem = MfaYontemi.Totp,
            GizliSifreli = TestVeritabani.TestSifreleyici.Sifrele(giz),
            Durum = MfaDurumu.Etkin,
            Etkinlesti = now,
            Olusturuldu = now,
            Guncellendi = now,
        });
        db.SaveChanges();
        return (kiraci, kullanici.Email, giz);
    }

    private static string Kod(string giz, int kaydirma = 0) =>
        Totp.Uret(Base32.Coz(giz), Totp.Adim(DateTime.UtcNow, 30) + kaydirma);

    private string MeydanOkuma(Kiraci kiraci, int dakika = 5)
    {
        using var db = fikstur.Vt.OlusturSistem();
        var k = db.Kullanicilar.AsNoTracking().Single(x => x.Id == kiraci.KullaniciId);
        return Jetonlar.MfaMeydanOkumaUret(k.Id, k.TenantId, k.SifreHash, dakika);
    }

    private HttpClient JetonluIstemci(string jeton)
    {
        var c = fikstur.Ortam.AnonimIstemci();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jeton);
        return c;
    }

    private static bool AlanVar(JsonElement govde, string ad) =>
        govde.EnumerateObject().Any(p => string.Equals(p.Name, ad, StringComparison.OrdinalIgnoreCase));

    // ── Giriş birinci adım ───────────────────────────────────────────────────

    [Fact]
    public async Task Mfa_etkinken_giris_token_dondurmez()
    {
        var (kiraci, eposta, _) = MfaliKullanici("giris");
        var istemci = fikstur.Ortam.AnonimIstemci();

        var cevap = await istemci.PostJson("/api/v1/auth/giris",
            new { email = eposta, sifre = Sifre, tenantId = kiraci.TenantId });
        var govde = await cevap.Json();

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.True(govde.GetProperty("mfaGerekli").GetBoolean());
        Assert.False(AlanVar(govde, "accessToken"));
        Assert.False(AlanVar(govde, "refreshToken"));
        Assert.False(string.IsNullOrWhiteSpace(govde.GetProperty("mfaToken").GetString()));
    }

    [Fact]
    public async Task Mfasiz_kullanicida_giris_eskisi_gibi_token_dondurur()
    {
        using var db = fikstur.Vt.OlusturSistem();
        var kiraci = TestKurulum.KiraciKur(db, $"P45-mfasiz-{Guid.NewGuid():N}"[..24]);
        var kullanici = db.Kullanicilar.Single(k => k.Id == kiraci.KullaniciId);
        kullanici.SifreHash = BCrypt.Net.BCrypt.HashPassword(Sifre, 12);
        db.SaveChanges();

        var cevap = await fikstur.Ortam.AnonimIstemci().PostJson("/api/v1/auth/giris",
            new { email = kullanici.Email, sifre = Sifre, tenantId = kiraci.TenantId });
        var govde = await cevap.Json();

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.True(AlanVar(govde, "accessToken"));
        Assert.False(AlanVar(govde, "mfaGerekli"));
    }

    // ── VETO: jeton ayrımı ───────────────────────────────────────────────────

    [Fact]
    public async Task Meydan_okuma_jetonu_korumali_uca_giremez()
    {
        var (kiraci, _, _) = MfaliKullanici("veto");
        var istemci = JetonluIstemci(MeydanOkuma(kiraci));

        var cevap = await istemci.GetAsync("/api/v1/cari");

        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
    }

    [Fact]
    public async Task Access_token_ikinci_adimda_kabul_edilmez()
    {
        var (kiraci, _, giz) = MfaliKullanici("access");
        var access = Jetonlar.AccessUret(kiraci.KullaniciId, kiraci.TenantId, kiraci.SubeId, "SAHIP");
        var istemci = JetonluIstemci(access);

        var cevap = await istemci.PostJson("/api/v1/auth/mfa/giris", new { kod = Kod(giz) });

        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
    }

    // ── Giriş ikinci adım ────────────────────────────────────────────────────

    [Fact]
    public async Task Ikinci_adim_dogru_kodla_token_verir()
    {
        var (kiraci, _, giz) = MfaliKullanici("ikinci");
        var istemci = JetonluIstemci(MeydanOkuma(kiraci));

        var cevap = await istemci.PostJson("/api/v1/auth/mfa/giris", new { kod = Kod(giz) });
        var govde = await cevap.Json();

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(govde.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(govde.GetProperty("refreshToken").GetString()));

        // Dönen access token GERÇEKTEN korumalı uca giriyor.
        var oturum = fikstur.Ortam.AnonimIstemci();
        oturum.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", govde.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await oturum.GetAsync("/api/v1/auth/ben")).StatusCode);
    }

    [Fact]
    public async Task Ikinci_adim_yanlis_kodu_reddeder()
    {
        var (kiraci, _, _) = MfaliKullanici("yanlis");
        var istemci = JetonluIstemci(MeydanOkuma(kiraci));

        var cevap = await istemci.PostJson("/api/v1/auth/mfa/giris", new { kod = "000000" });

        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
    }

    [Fact]
    public async Task Suresi_dolmus_meydan_okuma_jetonu_reddedilir()
    {
        var (kiraci, _, giz) = MfaliKullanici("omur");
        var istemci = JetonluIstemci(MeydanOkuma(kiraci, dakika: -1));

        var cevap = await istemci.PostJson("/api/v1/auth/mfa/giris", new { kod = Kod(giz) });

        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
    }

    [Fact]
    public async Task Parola_degisince_bekleyen_meydan_okuma_gecersiz()
    {
        var (kiraci, _, giz) = MfaliKullanici("pv");
        var jeton = MeydanOkuma(kiraci);

        await using (var db = fikstur.Vt.OlusturSistem())
        {
            var k = await db.Kullanicilar.SingleAsync(x => x.Id == kiraci.KullaniciId);
            k.SifreHash = BCrypt.Net.BCrypt.HashPassword("YeniParola123", 12);
            await db.SaveChangesAsync();
        }

        var cevap = await JetonluIstemci(jeton).PostJson("/api/v1/auth/mfa/giris", new { kod = Kod(giz) });

        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
    }

    [Fact]
    public async Task Kurtarma_kodu_girisde_tek_kullanimliktir()
    {
        var (kiraci, _, _) = MfaliKullanici("kurtarma");
        var duz = "ABCD1234EFGH5678";
        await using (var db = fikstur.Vt.OlusturSistem())
        {
            db.MfaKurtarmaKodlari.Add(new MfaKurtarmaKodu
            {
                Id = Kimlik.Ulid(),
                TenantId = kiraci.TenantId,
                KullaniciId = kiraci.KullaniciId,
                KodHash = TokenServisi.Hashle(duz),
                Kullanildi = false,
                Olusturuldu = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var ilk = await JetonluIstemci(MeydanOkuma(kiraci))
            .PostJson("/api/v1/auth/mfa/giris", new { kod = "ABCD-1234-EFGH-5678" });
        Assert.Equal(HttpStatusCode.OK, ilk.StatusCode);

        var ikinci = await JetonluIstemci(MeydanOkuma(kiraci))
            .PostJson("/api/v1/auth/mfa/giris", new { kod = "ABCD-1234-EFGH-5678" });
        Assert.Equal(HttpStatusCode.Unauthorized, ikinci.StatusCode);

        await using var oku = fikstur.Vt.OlusturSistem();
        var satir = await oku.MfaKurtarmaKodlari.AsNoTracking()
            .SingleAsync(k => k.KullaniciId == kiraci.KullaniciId);
        Assert.True(satir.Kullanildi);
    }

    [Fact]
    public async Task Ikinci_adim_oran_siniri_429_ve_retry_after_dondurur()
    {
        var (kiraci, _, _) = MfaliKullanici("oran");
        var jeton = MeydanOkuma(kiraci);
        var istemci = JetonluIstemci(jeton);

        HttpResponseMessage? son = null;
        for (var i = 0; i < 6; i++)
            son = await istemci.PostJson("/api/v1/auth/mfa/giris", new { kod = "000000" });

        Assert.NotNull(son);
        Assert.Equal(HttpStatusCode.TooManyRequests, son!.StatusCode);
        Assert.True(son.Headers.Contains("Retry-After"));
    }

    // ── Kurulum uçları ───────────────────────────────────────────────────────

    [Fact]
    public async Task Mfa_kur_jetonsuz_401_doner()
    {
        var cevap = await fikstur.Ortam.AnonimIstemci().PostJson("/api/v1/auth/mfa/kur", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
    }

    [Fact]
    public async Task Mfa_durumu_gizi_dondurmez()
    {
        var (kiraci, _, giz) = MfaliKullanici("durum");
        var istemci = fikstur.Ortam.Istemci(kiraci);

        var cevap = await istemci.GetAsync("/api/v1/auth/mfa/durum");
        var metin = await cevap.Metin();

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.Contains("\"etkin\":true", metin, StringComparison.Ordinal);
        Assert.DoesNotContain(giz, metin, StringComparison.Ordinal);
        Assert.DoesNotContain("giz", metin, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Mfa_etkinken_ikinci_kurulum_409_doner()
    {
        var (kiraci, _, _) = MfaliKullanici("cakisma");

        var cevap = await fikstur.Ortam.Istemci(kiraci).PostJson("/api/v1/auth/mfa/kur", new { });

        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
    }

    [Fact]
    public async Task Denetim_izinde_mfa_olayi_hassas_isaretlenir()
    {
        var (kiraci, _, giz) = MfaliKullanici("iz");
        var cevap = await JetonluIstemci(MeydanOkuma(kiraci))
            .PostJson("/api/v1/auth/mfa/giris", new { kod = Kod(giz) });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        await using var oku = fikstur.Vt.OlusturSistem();
        var iz = await oku.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == kiraci.TenantId && d.Detay == "mfa-giris-basarili")
            .ToListAsync();

        Assert.Single(iz);
        Assert.True(iz[0].Hassas);
        Assert.DoesNotContain(giz, iz[0].Detay!, StringComparison.Ordinal);
    }
}
