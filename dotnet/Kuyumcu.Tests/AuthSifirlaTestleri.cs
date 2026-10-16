using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET AUTH-SIFIRLA — "Parolamı unuttum" GERÇEK HTTP testleri (kurul 5 engeli).
//
// Ekip oyu maddeleri (1)-(7) ve kabul listesi:
//   var/yok e-posta AYNI yanıt + AYNI süre aralığı · jeton tek kullanım · 30 dk
//   sonra 400 · ikinci istek eskiyi geçersiz kılar · hız sınırı 4. istekte 429 ·
//   sıfırlama sonrası eski refresh jetonları 401 · DB'de jeton düz metin 0 ·
//   SMTP (kanal) yokken 503 · MFA atlanmaz · parola kuralı · denetim kaydı
//   (değer yazılmaz).
//
// Kanal: varsayılan fikstür (Testing, ParolaSifirlama:Kanal boş) = KAPALI → 503
// testleri onunla. Açık kanal testleri fikstürün TÜREVİ ile koşar: posta bellek
// içi TEST KUTUSUNA yazılır (madde 7 — gerçek e-posta YOK).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Madde (7): test kanalı — postayı belleğe yazar, dışarı istek atmaz.</summary>
public sealed class TestPostaKutusu : IParolaSifirlamaPostasi
{
    public ConcurrentQueue<(string Alici, string Konu, string Govde)> Postalar { get; } = new();

    public bool Yapilandirildi => true;

    public Task Gonder(string alici, string konu, string govde, CancellationToken iptal)
    {
        Postalar.Enqueue((alici, konu, govde));
        return Task.CompletedTask;
    }

    public IReadOnlyList<(string Alici, string Konu, string Govde)> Kime(string alici) =>
        Postalar.Where(p => string.Equals(p.Alici, alici, StringComparison.OrdinalIgnoreCase)).ToList();
}

[Collection("http")]
public sealed class AuthSifirlaTestleri(HttpFikstur fikstur)
{
    private const string EskiSifre = "EskiParola123";
    private const string YeniSifre = "YeniParola456";
    private const string Taban = "https://dukkan.test";
    private const int YanitTabaniMs = 400;

    private const string IstekYolu = "/api/v1/auth/parola-sifirlama/istek";
    private const string TamamlaYolu = "/api/v1/auth/parola-sifirlama/tamamla";

    // ── Açık kanallı türev fikstür (sınıf başına bir kez kurulur) ─────────────
    private static readonly object Kilit = new();
    private static WebApplicationFactory<Program>? acikOrtam;
    private static readonly TestPostaKutusu Kutu = new();

    private WebApplicationFactory<Program> AcikOrtam
    {
        get
        {
            lock (Kilit)
            {
                return acikOrtam ??= fikstur.Ortam.WithWebHostBuilder(b => b.ConfigureServices(s =>
                {
                    s.RemoveAll<IParolaSifirlamaPostasi>();
                    s.AddSingleton<IParolaSifirlamaPostasi>(Kutu);
                    s.RemoveAll<ParolaSifirlamaAyarlari>();
                    s.AddSingleton(new ParolaSifirlamaAyarlari
                    {
                        BaglantiTabani = Taban,
                        YanitTabaniMs = YanitTabaniMs,
                    });
                }));
            }
        }
    }

    private HttpClient Anonim(string? ip = null)
    {
        var c = AcikOrtam.CreateClient();
        c.DefaultRequestHeaders.Add("X-Forwarded-For", ip ?? HttpOrtami.BenzersizIp());
        return c;
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    /// <summary>Bilinen eski parolalı bir kullanıcı kurar; (kiracı, e-posta) döner.</summary>
    private (Kiraci Kiraci, string Eposta) Kullanici(string ad, bool aktif = true)
    {
        using var db = fikstur.Vt.OlusturSistem();
        var kiraci = TestKurulum.KiraciKur(db, $"PAKETAUTH-{ad}-{Guid.NewGuid():N}"[..24]);
        var k = db.Kullanicilar.Single(x => x.Id == kiraci.KullaniciId);
        k.SifreHash = BCrypt.Net.BCrypt.HashPassword(EskiSifre, 4);
        k.Aktif = aktif;
        db.SaveChanges();
        return (kiraci, k.Email);
    }

    /// <summary>Kutudaki SON postadan bağlantı jetonunu çıkarır.</summary>
    private static string JetonCek(string eposta)
    {
        var son = Kutu.Kime(eposta).Last();
        var m = Regex.Match(son.Govde, Regex.Escape(Taban) + @"/sifre-sifirla#jeton=([A-Za-z0-9_\-]+)");
        Assert.True(m.Success, "postada sıfırlama bağlantısı yok");
        return m.Groups[1].Value;
    }

    private async Task<string> JetonIste(string eposta, string? ip = null)
    {
        var once = Kutu.Kime(eposta).Count;
        var r = await Anonim(ip).PostJson(IstekYolu, new { email = eposta });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(once + 1, Kutu.Kime(eposta).Count);
        return JetonCek(eposta);
    }

    private Task<HttpResponseMessage> Tamamla(string jeton, string sifre = YeniSifre, string? tekrar = null) =>
        Anonim().PostJson(TamamlaYolu, new { jeton, yeniSifre = sifre, yeniSifreTekrar = tekrar ?? sifre });

    private List<ParolaSifirlamaJetonu> Jetonlar(string kullaniciId)
    {
        using var db = fikstur.Vt.OlusturSistem();
        return db.ParolaSifirlamaJetonlari.AsNoTracking().Where(j => j.KullaniciId == kullaniciId)
            .OrderBy(j => j.Olusturuldu).ThenBy(j => j.Id).ToList();
    }

    private async Task<HttpResponseMessage> Giris(string eposta, string sifre, Kiraci kiraci) =>
        await Anonim().PostJson("/api/v1/auth/giris", new { email = eposta, sifre, tenantId = kiraci.TenantId });

    private static bool AlanVar(JsonElement govde, string ad) =>
        govde.EnumerateObject().Any(p => string.Equals(p.Name, ad, StringComparison.OrdinalIgnoreCase));

    // ═════════════════════════════════════════════════════════════════════════
    // Madde (6) — KANAL YOKKEN FAIL-CLOSED
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Kanal_yokken_istek_503_ve_GONDERILDI_demez_jeton_uretmez()
    {
        var (kiraci, eposta) = Kullanici("kapali");
        var c = fikstur.Ortam.AnonimIstemci();

        var durum = await (await c.GetAsync("/api/v1/auth/parola-sifirlama/durum")).Json();
        Assert.False(durum.GetProperty("acik").GetBoolean());
        Assert.Contains("başvurun", durum.GetProperty("mesaj").GetString());

        var r = await c.PostJson(IstekYolu, new { email = eposta });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, r.StatusCode);
        var metin = (await r.Json()).GetProperty("message").GetString()!;   // Türkçe gövde AYRIŞTIRILIR
        Assert.Contains("işletme sahibine ya da destek hattına başvurun", metin);
        Assert.DoesNotContain("gönderildi", metin);
        Assert.Empty(Jetonlar(kiraci.KullaniciId));
    }

    [Fact]
    public async Task Kanal_acikken_durum_acik_doner_hesap_bilgisi_icermez()
    {
        var d = await (await Anonim().GetAsync("/api/v1/auth/parola-sifirlama/durum")).Json();
        Assert.True(d.GetProperty("acik").GetBoolean());
        Assert.Equal(30, d.GetProperty("gecerlilikDakika").GetInt32());
        Assert.Equal(3, d.EnumerateObject().Count());
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Madde (1) — VAR/YOK AYNI YANIT + AYNI SÜRE ARALIĞI
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Var_ve_yok_eposta_AYNI_yanit_AYNI_sure_araligi()
    {
        var (_, eposta) = Kullanici("varyok");
        var yok = $"yok-{Guid.NewGuid():N}@test.local";
        // Isınma (ilk istek JIT/bağlantı havuzu maliyetini ölçüme katmasın).
        await Anonim().PostJson(IstekYolu, new { email = $"isinma-{Guid.NewGuid():N}@test.local" });

        var s1 = Stopwatch.StartNew();
        var rVar = await Anonim().PostJson(IstekYolu, new { email = eposta });
        s1.Stop();
        var s2 = Stopwatch.StartNew();
        var rYok = await Anonim().PostJson(IstekYolu, new { email = yok });
        s2.Stop();

        Assert.Equal(HttpStatusCode.OK, rVar.StatusCode);
        Assert.Equal(HttpStatusCode.OK, rYok.StatusCode);
        Assert.Equal(await rVar.Metin(), await rYok.Metin());     // gövde BAYT BAYT aynı
        Assert.Equal(ParolaSifirlamaServisi.IstekYanitMesaji,
            (await rYok.Json()).GetProperty("mesaj").GetString());

        // Süre: ikisi de tabandan kısa dönmez ve aralarındaki fark tabanın altında kalır.
        Assert.True(s1.ElapsedMilliseconds >= YanitTabaniMs - 30, $"var: {s1.ElapsedMilliseconds} ms");
        Assert.True(s2.ElapsedMilliseconds >= YanitTabaniMs - 30, $"yok: {s2.ElapsedMilliseconds} ms");
        Assert.True(Math.Abs(s1.ElapsedMilliseconds - s2.ElapsedMilliseconds) < YanitTabaniMs,
            $"fark {s1.ElapsedMilliseconds} / {s2.ElapsedMilliseconds}");

        Assert.Single(Kutu.Kime(eposta));
        Assert.Empty(Kutu.Kime(yok));
    }

    [Fact]
    public async Task Pasif_kullaniciya_posta_gitmez_yanit_yine_ayni()
    {
        var (kiraci, eposta) = Kullanici("pasif", aktif: false);
        var r = await Anonim().PostJson(IstekYolu, new { email = eposta });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(ParolaSifirlamaServisi.IstekYanitMesaji, (await r.Json()).GetProperty("mesaj").GetString());
        Assert.Empty(Kutu.Kime(eposta));
        Assert.Empty(Jetonlar(kiraci.KullaniciId));
    }

    [Fact]
    public async Task Gecersiz_eposta_bicimi_400_alan_hatasi()
    {
        var r = await Anonim().PostJson(IstekYolu, new { email = "eposta-degil" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Madde (2) — JETON: ≥32 bayt, yalnız SHA-256, tek kullanım, 30 dk, iptal
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DB_de_jeton_DUZ_METIN_yok_yalniz_SHA256_ve_30_dk()
    {
        var (kiraci, eposta) = Kullanici("hash");
        var jeton = await JetonIste(eposta);

        Assert.True(jeton.Length >= 43, "32 bayt base64url en az 43 karakter");
        var satir = Assert.Single(Jetonlar(kiraci.KullaniciId));
        Assert.Equal(TokenServisi.Hashle(jeton), satir.JetonHash);
        Assert.NotEqual(jeton, satir.JetonHash);
        Assert.Matches("^[0-9a-f]{64}$", satir.JetonHash);
        // 30 dk ömür (± 5 sn).
        var omur = satir.Gecerlilik - satir.Olusturuldu;
        Assert.InRange(omur.TotalSeconds, 30 * 60 - 5, 30 * 60 + 5);

        // Ham SQL: tablonun HİÇBİR kolonunda düz jeton yok.
        using var db = fikstur.Vt.OlusturSistem();
        var baglanti = db.Database.GetDbConnection();
        await baglanti.OpenAsync();
        await using var komut = baglanti.CreateCommand();
        komut.CommandText =
            "SELECT COUNT(*) FROM parola_sifirlama_jetonlari WHERE CONCAT_WS('|', id, tenant_id, kullanici_id, " +
            "jeton_hash, IFNULL(ip_adresi,'')) LIKE CONCAT('%', @j, '%')";
        var p = komut.CreateParameter(); p.ParameterName = "@j"; p.Value = jeton; komut.Parameters.Add(p);
        Assert.Equal(0L, Convert.ToInt64(await komut.ExecuteScalarAsync()));
        // Denetim izinde de jeton yok.
        Assert.False(db.DenetimKayitlari.AsNoTracking()
            .Any(d => d.TenantId == kiraci.TenantId && d.Detay != null && d.Detay.Contains(jeton)));
    }

    [Fact]
    public async Task Jeton_TEK_KULLANIMLIK_ikinci_kullanim_400()
    {
        var (kiraci, eposta) = Kullanici("tek");
        var jeton = await JetonIste(eposta);

        var r1 = await Tamamla(jeton);
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        var r2 = await Tamamla(jeton, "BaskaParola789");
        Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
        Assert.Equal(ParolaSifirlamaServisi.JetonRedMesaji, (await r2.Json()).GetProperty("message").GetString());

        // İkinci deneme parolayı DEĞİŞTİRMEDİ: yeni parola geçer, "BaskaParola789" geçmez.
        Assert.Equal(HttpStatusCode.OK, (await Giris(eposta, YeniSifre, kiraci)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Giris(eposta, "BaskaParola789", kiraci)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Giris(eposta, EskiSifre, kiraci)).StatusCode);
        Assert.NotNull(Assert.Single(Jetonlar(kiraci.KullaniciId)).Kullanildi);
    }

    [Fact]
    public async Task Suresi_dolan_jeton_400_parola_degismez()
    {
        var (kiraci, eposta) = Kullanici("sure");
        var jeton = await JetonIste(eposta);
        using (var db = fikstur.Vt.OlusturSistem())
        {
            var satir = db.ParolaSifirlamaJetonlari.Single(j => j.KullaniciId == kiraci.KullaniciId);
            // 30 dk + 1 sn önce üretilmiş gibi.
            satir.Olusturuldu = DateTime.UtcNow.AddMinutes(-31);
            satir.Gecerlilik = DateTime.UtcNow.AddSeconds(-1);
            db.SaveChanges();
        }

        var r = await Tamamla(jeton);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Giris(eposta, EskiSifre, kiraci)).StatusCode);
    }

    [Fact]
    public async Task Ikinci_istek_ESKI_jetonu_gecersiz_kilar()
    {
        var (kiraci, eposta) = Kullanici("iki");
        var ip = HttpOrtami.BenzersizIp();
        var eski = await JetonIste(eposta, ip);
        var yeni = await JetonIste(eposta, ip);
        Assert.NotEqual(eski, yeni);

        var satirlar = Jetonlar(kiraci.KullaniciId);
        Assert.Equal(2, satirlar.Count);
        Assert.True(satirlar.Single(j => j.JetonHash == TokenServisi.Hashle(eski)).IptalEdildi);

        Assert.Equal(HttpStatusCode.BadRequest, (await Tamamla(eski)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Tamamla(yeni)).StatusCode);
    }

    [Fact]
    public async Task Kullanim_DIGER_acik_jetonlari_da_oldurur()
    {
        var (kiraci, eposta) = Kullanici("diger");
        var j1 = await JetonIste(eposta);
        // Aynı kullanıcıya ikinci açık jeton (yarış/eski kayıt taklidi): istek yolu
        // onu zaten iptal ederdi, burada DOĞRUDAN açık bırakılır.
        var (duz2, hash2) = ParolaSifirlamaServisi.JetonUret();
        using (var db = fikstur.Vt.OlusturSistem())
        {
            db.ParolaSifirlamaJetonlari.Add(new ParolaSifirlamaJetonu
            {
                Id = Kimlik.Ulid(), TenantId = kiraci.TenantId, KullaniciId = kiraci.KullaniciId,
                JetonHash = hash2, Gecerlilik = DateTime.UtcNow.AddMinutes(30), Olusturuldu = DateTime.UtcNow,
            });
            db.SaveChanges();
        }

        Assert.Equal(HttpStatusCode.OK, (await Tamamla(j1)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Tamamla(duz2, "UcuncuParola1")).StatusCode);
    }

    [Fact]
    public async Task Uydurma_jeton_400_ayni_mesaj()
    {
        var (duz, _) = ParolaSifirlamaServisi.JetonUret();
        var r = await Tamamla(duz);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(ParolaSifirlamaServisi.JetonRedMesaji, (await r.Json()).GetProperty("message").GetString());
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Madde (3) — HIZ SINIRI: e-posta + IP başına 15 dk'da 3
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Hiz_siniri_ayni_eposta_ayni_IP_DORDUNCU_istekte_429()
    {
        var (_, eposta) = Kullanici("hiz");
        var ip = HttpOrtami.BenzersizIp();
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.OK, (await Anonim(ip).PostJson(IstekYolu, new { email = eposta })).StatusCode);

        var r4 = await Anonim(ip).PostJson(IstekYolu, new { email = eposta });
        Assert.Equal(HttpStatusCode.TooManyRequests, r4.StatusCode);
        Assert.True(r4.Headers.RetryAfter?.Delta?.TotalSeconds > 0, "Retry-After yok");
        Assert.Equal(429, (await r4.Json()).GetProperty("statusCode").GetInt32());
        Assert.Equal(3, Kutu.Kime(eposta).Count);   // 4. istek posta üretmedi

        // Başka IP aynı e-posta: ayrı bölüt (NAT arkasındaki başka dükkân kilitlenmez).
        Assert.Equal(HttpStatusCode.OK, (await Anonim().PostJson(IstekYolu, new { email = eposta })).StatusCode);
    }

    [Fact]
    public async Task Hiz_siniri_KAYITSIZ_epostada_da_ayni_429_varlik_sizmaz()
    {
        var yok = $"yok-hiz-{Guid.NewGuid():N}@test.local";
        var ip = HttpOrtami.BenzersizIp();
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.OK, (await Anonim(ip).PostJson(IstekYolu, new { email = yok })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await Anonim(ip).PostJson(IstekYolu, new { email = yok.ToUpperInvariant() })).StatusCode);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Madde (4) — PAROLA KURALI + TÜM REFRESH JETONLARI İPTAL + DENETİM
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Sifirlama_sonrasi_ESKI_refresh_jetonlari_401()
    {
        var (kiraci, eposta) = Kullanici("refresh");
        var giris = await (await Giris(eposta, EskiSifre, kiraci)).Json();
        var eskiRefresh = giris.GetProperty("refreshToken").GetString()!;

        Assert.Equal(HttpStatusCode.OK, (await Tamamla(await JetonIste(eposta))).StatusCode);

        var yenile = await Anonim().PostJson("/api/v1/auth/yenile", new { refreshToken = eskiRefresh });
        Assert.Equal(HttpStatusCode.Unauthorized, yenile.StatusCode);
        using var db = fikstur.Vt.OlusturSistem();
        Assert.True(db.RefreshTokenler.AsNoTracking().Where(t => t.KullaniciId == kiraci.KullaniciId)
            .All(t => t.IptalEdildi));
    }

    [Theory]
    [InlineData("kisa1", "yeniSifre")]          // 8'den kısa
    [InlineData("sadeceharfler", "yeniSifre")]  // rakam yok
    [InlineData("1234567890", "yeniSifre")]     // harf yok
    public async Task Parola_kurali_ihlali_400_alan_hatasi_jeton_YANMAZ(string sifre, string alan)
    {
        var (kiraci, eposta) = Kullanici("kural");
        var jeton = await JetonIste(eposta);

        var r = await Tamamla(jeton, sifre);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains(alan, await r.Metin(), StringComparison.OrdinalIgnoreCase);
        Assert.Null(Assert.Single(Jetonlar(kiraci.KullaniciId)).Kullanildi);

        // Kurala uyan parola aynı jetonla geçer.
        Assert.Equal(HttpStatusCode.OK, (await Tamamla(jeton)).StatusCode);
    }

    [Fact]
    public async Task Tekrar_uyusmazsa_400_alan_yeniSifreTekrar()
    {
        var (_, eposta) = Kullanici("tekrar");
        var jeton = await JetonIste(eposta);
        var r = await Tamamla(jeton, YeniSifre, "FarkliParola999");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("yeniSifreTekrar", await r.Metin(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Basarili_sifirlama_DENETIM_kaydi_yazar_DEGER_yazmaz()
    {
        var (kiraci, eposta) = Kullanici("denetim");
        var jeton = await JetonIste(eposta);
        Assert.Equal(HttpStatusCode.OK, (await Tamamla(jeton)).StatusCode);

        using var db = fikstur.Vt.OlusturSistem();
        var izler = db.DenetimKayitlari.AsNoTracking().Where(d => d.TenantId == kiraci.TenantId).ToList();
        var iz = Assert.Single(izler, d => d.VarlikTuru == "Kullanici" && d.VarlikId == kiraci.KullaniciId
                                           && (d.Detay ?? "").Contains("parola e-posta bağlantısıyla sıfırlandı"));
        Assert.True(iz.Hassas);
        Assert.Equal(kiraci.KullaniciId, iz.KullaniciId);
        // Mevcut kullanıcının parolası DEĞİŞTİ → eylem GÜNCELLEME (EKLEME değil; doğrulayıcı bulgusu tur 2).
        Assert.Equal(DenetimEylemi.Guncelleme, iz.Eylem);
        var hash = TokenServisi.Hashle(jeton);
        Assert.DoesNotContain(izler, d => (d.Detay ?? "").Contains(jeton) || (d.Detay ?? "").Contains(hash)
                                          || (d.Detay ?? "").Contains(YeniSifre));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Madde (5) — MFA ATLANMAZ
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MFA_acik_kullanicida_sifirlama_oturum_ACMAZ_giris_MFA_ister()
    {
        var (kiraci, eposta) = Kullanici("mfa");
        using (var db = fikstur.Vt.OlusturSistem())
        {
            var now = DateTime.UtcNow;
            db.KullaniciMfalari.Add(new KullaniciMfa
            {
                Id = Kimlik.Ulid(), TenantId = kiraci.TenantId, KullaniciId = kiraci.KullaniciId,
                Yontem = MfaYontemi.Totp,
                GizliSifreli = TestVeritabani.TestSifreleyici.Sifrele(
                    Base32.Kodla(System.Security.Cryptography.RandomNumberGenerator.GetBytes(20))),
                Durum = MfaDurumu.Etkin, Etkinlesti = now, Olusturuldu = now, Guncellendi = now,
            });
            db.SaveChanges();
        }

        var r = await Tamamla(await JetonIste(eposta));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var govde = await r.Json();
        Assert.False(AlanVar(govde, "accessToken"));
        Assert.False(AlanVar(govde, "refreshToken"));
        Assert.False(AlanVar(govde, "mfaToken"));

        var giris = await (await Giris(eposta, YeniSifre, kiraci)).Json();
        Assert.True(giris.GetProperty("mfaGerekli").GetBoolean());
        Assert.False(AlanVar(giris, "accessToken"));
        Assert.False(AlanVar(giris, "refreshToken"));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// SAF testler — kanal fabrikası, kural aynası, jeton üretimi
// ═════════════════════════════════════════════════════════════════════════════

public sealed class AuthSifirlaSafTestleri
{
    private sealed class Ortam(string ad) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = ad;
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    [Fact]
    public void Parola_kalibi_KullaniciModulu_kurali_ile_AYNI()
    {
        var p = typeof(Kuyumcu.Api.Modules.Kullanici.SifreDegistirDto).GetProperty("YeniSifre")!;
        var kalip = p.GetCustomAttribute<RegularExpressionAttribute>()!.Pattern;
        var uzunluk = p.GetCustomAttribute<StringLengthAttribute>()!;
        Assert.Equal(ParolaSifirlamaServisi.ParolaKalibi, kalip);
        Assert.Equal(ParolaSifirlamaServisi.ParolaEnAz, uzunluk.MinimumLength);
        Assert.Equal(ParolaSifirlamaServisi.ParolaEnFazla, uzunluk.MaximumLength);
    }

    [Fact]
    public void Jeton_en_az_32_bayt_rastgele_ve_hash_SHA256()
    {
        var (a, ha) = ParolaSifirlamaServisi.JetonUret();
        var (b, _) = ParolaSifirlamaServisi.JetonUret();
        Assert.NotEqual(a, b);
        Assert.True(ParolaSifirlamaServisi.JetonBayt >= 32);
        Assert.Equal(43, a.Length);   // 32 bayt base64url, dolgusuz
        Assert.Equal(TokenServisi.Hashle(a), ha);
    }

    [Theory]
    [InlineData(null, "Production", false)]
    [InlineData("", "Development", false)]
    [InlineData("DOSYA", "Production", false)]   // üretimde jeton diske düşmez
    [InlineData("SMTP", "Development", false)]   // gerçek SMTP YOK (ADR-0016)
    [InlineData("DOSYA", "Development", true)]
    [InlineData("dosya", "Testing", true)]
    public void Kanal_fabrikasi_fail_closed(string? kanal, string ortam, bool acik)
    {
        var p = ParolaSifirlamaPostasiFabrikasi.Kur(
            new ParolaSifirlamaAyarlari { Kanal = kanal, DosyaDizini = Path.GetTempPath() },
            new Ortam(ortam), NullLogger.Instance);
        Assert.Equal(acik, p.Yapilandirildi);
        if (!acik) Assert.IsType<KapaliParolaSifirlamaPostasi>(p);
    }

    [Fact]
    public async Task Kapali_kanal_gonderirse_PATLAR_sessiz_gonderildi_yok()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new KapaliParolaSifirlamaPostasi().Gonder("a@b.c", "k", "g", CancellationToken.None));
    }

    [Fact]
    public async Task Dosya_kanali_yerel_dosyaya_yazar_yalniz_sahibi_okur()
    {
        var dizin = Path.Combine(Path.GetTempPath(), "kuyumcu-posta-test-" + Guid.NewGuid().ToString("N"));
        var kanal = new DosyaParolaSifirlamaPostasi(dizin);
        await kanal.Gonder("a@b.c", "Konu", "Gövde bağlantı", CancellationToken.None);
        var dosya = Assert.Single(Directory.GetFiles(dizin, "*.eml"));
        var icerik = await File.ReadAllTextAsync(dosya);
        Assert.Contains("To: a@b.c", icerik);
        Assert.Contains("Gövde bağlantı", icerik);
        if (!OperatingSystem.IsWindows())
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(dosya));
    }

    [Fact]
    public void Baglanti_jetonu_PARCADA_tasir_Host_basligindan_turetilmez()
    {
        var ayar = new ParolaSifirlamaAyarlari { BaglantiTabani = "https://dukkan.ornek/" };
        var s = new ParolaSifirlamaServisi(null!, new TestPostaKutusu(), ayar,
            new ParolaSifirlamaHizSiniri(), NullLogger<ParolaSifirlamaServisi>.Instance);
        Assert.Equal("https://dukkan.ornek/sifre-sifirla#jeton=abc", s.Baglanti("abc"));
        Assert.True(s.Acik);
        // Taban yoksa kanal açık olsa da özellik KAPALI.
        var s2 = new ParolaSifirlamaServisi(null!, new TestPostaKutusu(), new ParolaSifirlamaAyarlari(),
            new ParolaSifirlamaHizSiniri(), NullLogger<ParolaSifirlamaServisi>.Instance);
        Assert.False(s2.Acik);
    }

    [Fact]
    public void Hiz_siniri_ayni_anahtarda_UC_izin_dorduncu_red()
    {
        using var h = new ParolaSifirlamaHizSiniri();
        for (var i = 0; i < 3; i++) Assert.True(h.Al("1.1.1.1", "a@b.c", out _));
        Assert.False(h.Al("1.1.1.1", "a@b.c", out var bekle));
        Assert.True(bekle > 0);
        Assert.True(h.Al("2.2.2.2", "a@b.c", out _));
    }
}
