using System.Security.Cryptography;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// Alan şifreleme — SIRADAKI-ADIM-KARAR §2 mührü + Mehmet md.2.
// İki katman: saf şifreleyici (DB yok) ve DbContext üzerinden uçtan uca
// (diske gerçekten şifreli iniyor mu, ham SQL ile bakılır).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class AlanSifrelemeSafTestleri
{
    private static readonly IAlanSifreleyici S = TestVeritabani.TestSifreleyici;

    [Fact]
    public void SifreleCoz_GidipGelir()
    {
        Assert.Equal("12345678950", S.Coz(S.Sifrele("12345678950")));
    }

    [Fact]
    public void AyniMetin_HerSeferindeFarkliSifreliMetin()
    {
        // Rastgele nonce: deterministik DEĞİL. Aynı TCKN iki kayıtta aynı görünmez.
        var a = S.Sifrele("12345678950");
        var b = S.Sifrele("12345678950");
        Assert.NotEqual(a, b);
        Assert.Equal(S.Coz(a), S.Coz(b));
    }

    [Fact]
    public void SifreliMetin_SurumOnekiTasir()
    {
        Assert.StartsWith("v1:", S.Sifrele("x"));
    }

    [Fact]
    public void SifreliMetinDuzMetniIcermez()
    {
        var sifreli = S.Sifrele("12345678950");
        Assert.DoesNotContain("12345678950", sifreli);
    }

    [Fact]
    public void OynanmisVeri_Reddedilir()
    {
        var sifreli = S.Sifrele("12345678950");
        // base64 gövdesinin ortasından bir karakter değiştir
        var govde = sifreli[3..].ToCharArray();
        govde[govde.Length / 2] = govde[govde.Length / 2] == 'A' ? 'B' : 'A';
        var bozuk = "v1:" + new string(govde);

        Assert.ThrowsAny<Exception>(() => S.Coz(bozuk));   // GCM etiketi tutmaz ya da base64 bozulur
    }

    [Fact]
    public void FarkliAnahtar_Cozemez()
    {
        var baska = new AesGcmAlanSifreleyici(Enumerable.Repeat((byte)7, 32).ToArray());
        // GCM etiket uyuşmazlığı CryptographicException'ın ALT tipi (AuthenticationTagMismatchException)
        Assert.ThrowsAny<CryptographicException>(() => baska.Coz(S.Sifrele("12345678950")));
    }

    [Fact]
    public void FarkliSurum_Reddedilir()
    {
        var v2 = new AesGcmAlanSifreleyici(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray(), surum: 2);
        var ex = Assert.Throws<CryptographicException>(() => v2.Coz(S.Sifrele("x")));
        Assert.Contains("rotasyon", ex.Message);
    }

    // ── Hash: deterministik, normalize, tersine çevrilemez ───────────────────

    [Fact]
    public void Hash_Deterministik()
    {
        Assert.Equal(S.Hashle("12345678950"), S.Hashle("12345678950"));
    }

    [Theory]
    [InlineData("12345678950", "123 456 789 50")]
    [InlineData("AB123456", "ab123456")]
    [InlineData("U 12 34", "u1234")]
    public void Hash_BoslukVeBuyukKucukFarkiYok(string a, string b)
    {
        Assert.Equal(S.Hashle(a), S.Hashle(b));
    }

    [Fact]
    public void Hash_FarkliDegerFarkliHash()
    {
        Assert.NotEqual(S.Hashle("12345678950"), S.Hashle("10000000214"));
    }

    [Fact]
    public void Hash_64HexKarakter()
    {
        var h = S.Hashle("12345678950");
        Assert.Equal(64, h.Length);
        Assert.Matches("^[0-9A-F]{64}$", h);
    }

    [Fact]
    public void Hash_AnahtaraBagli()
    {
        var baska = new AesGcmAlanSifreleyici(Enumerable.Repeat((byte)7, 32).ToArray());
        Assert.NotEqual(S.Hashle("12345678950"), baska.Hashle("12345678950"));
    }

    // ── Anahtar yükleme: fail-closed ─────────────────────────────────────────

    private static IConfiguration Yapilandirma(params (string, string?)[] c) =>
        new ConfigurationBuilder().AddInMemoryCollection(c.ToDictionary(x => x.Item1, x => x.Item2)).Build();

    [Fact]
    public void AnahtarYoksa_UygulamaAcilmaz()
    {
        var eski = Environment.GetEnvironmentVariable(AlanSifreleme.OrtamDegiskeni);
        try
        {
            Environment.SetEnvironmentVariable(AlanSifreleme.OrtamDegiskeni, null);
            var ex = Assert.Throws<InvalidOperationException>(() => AlanSifreleme.Yukle(Yapilandirma()));
            Assert.Contains("fail-closed", ex.Message);
        }
        finally { Environment.SetEnvironmentVariable(AlanSifreleme.OrtamDegiskeni, eski); }
    }

    [Fact]
    public void Anahtar32BaytDegilse_Reddedilir()
    {
        var eski = Environment.GetEnvironmentVariable(AlanSifreleme.OrtamDegiskeni);
        try
        {
            Environment.SetEnvironmentVariable(AlanSifreleme.OrtamDegiskeni, null);
            var kisa = Convert.ToBase64String(new byte[16]);
            var ex = Assert.Throws<InvalidOperationException>(() =>
                AlanSifreleme.Yukle(Yapilandirma((AlanSifreleme.YapilandirmaAnahtari, kisa))));
            Assert.Contains("32 bayt", ex.Message);
        }
        finally { Environment.SetEnvironmentVariable(AlanSifreleme.OrtamDegiskeni, eski); }
    }

    [Fact]
    public void GecersizBase64_Reddedilir()
    {
        var eski = Environment.GetEnvironmentVariable(AlanSifreleme.OrtamDegiskeni);
        try
        {
            Environment.SetEnvironmentVariable(AlanSifreleme.OrtamDegiskeni, null);
            Assert.Throws<InvalidOperationException>(() =>
                AlanSifreleme.Yukle(Yapilandirma((AlanSifreleme.YapilandirmaAnahtari, "bu base64 degil!!"))));
        }
        finally { Environment.SetEnvironmentVariable(AlanSifreleme.OrtamDegiskeni, eski); }
    }

    [Fact]
    public void OrtamDegiskeni_YapilandirmayiEzer()
    {
        var eski = Environment.GetEnvironmentVariable(AlanSifreleme.OrtamDegiskeni);
        try
        {
            var ortam = AlanSifreleme.AnahtarUret();
            Environment.SetEnvironmentVariable(AlanSifreleme.OrtamDegiskeni, ortam);
            var cfg = Yapilandirma((AlanSifreleme.YapilandirmaAnahtari, AlanSifreleme.AnahtarUret()));

            var s1 = AlanSifreleme.Yukle(cfg);
            var s2 = new AesGcmAlanSifreleyici(Convert.FromBase64String(ortam));
            Assert.Equal(s1.Hashle("x"), s2.Hashle("x"));   // ortam anahtarıyla kurulmuş
        }
        finally { Environment.SetEnvironmentVariable(AlanSifreleme.OrtamDegiskeni, eski); }
    }

    [Fact]
    public void AnahtarUret_32BaytBase64()
    {
        Assert.Equal(32, Convert.FromBase64String(AlanSifreleme.AnahtarUret()).Length);
    }
}

[Collection("veritabani")]
public sealed class AlanSifrelemeVeritabaniTestleri(TestVeritabani vt)
{
    private const string Tckn = "10000000146";   // biçimsel örnek, gerçek kişi değil

    private static MasakKimlikTespiti Tespit(Kiraci k, string belgeNo) => new()
    {
        Id = Kimlik.Ulid(), TenantId = k.TenantId, CariId = k.CariId, IslemTutariTl = 250_000m,
        BelgeTuru = KimlikBelgeTuru.TcKimlik, BelgeNo = belgeNo, AdSoyad = "Test Kişi",
        TespitEdenKullaniciId = k.KullaniciId, Tarih = DateTime.UtcNow,
        SaklamaBitis = DateTime.UtcNow.AddYears(8), Olusturuldu = DateTime.UtcNow,
    };

    [Fact]
    public async Task BelgeNo_DiskeSifreliIner_HamSqlDuzMetinGormez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Sifre-disk");

        using var db = vt.Olustur(k);
        var t = Tespit(k, Tckn);
        db.MasakKimlikTespitleri.Add(t);
        await db.SaveChangesAsync();

        // HAM SQL — EF converter'ı ATLAYARAK diskteki gerçek değeri oku
        using var kontrol = vt.OlusturSistem();
        var ham = await kontrol.Database.SqlQuery<string>(
            $"SELECT belge_no AS Value FROM masak_kimlik_tespitleri WHERE id = {t.Id}").SingleAsync();

        Assert.NotEqual(Tckn, ham);
        Assert.DoesNotContain(Tckn, ham);
        Assert.StartsWith("v1:", ham);
    }

    [Fact]
    public async Task BelgeNo_EfUzerindenOkununcaCozulur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Sifre-oku");

        using var db = vt.Olustur(k);
        var t = Tespit(k, Tckn);
        db.MasakKimlikTespitleri.Add(t);
        await db.SaveChangesAsync();

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(Tckn, kontrol.MasakKimlikTespitleri.AsNoTracking().Single(x => x.Id == t.Id).BelgeNo);
    }

    [Fact]
    public async Task HashVeSurum_OtomatikDolar()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Sifre-hash");

        using var db = vt.Olustur(k);
        var t = Tespit(k, Tckn);   // BelgeNoHash ve SifreSurumu bilinçli olarak BOŞ
        db.MasakKimlikTespitleri.Add(t);
        await db.SaveChangesAsync();

        using var kontrol = vt.OlusturSistem();
        var okunan = kontrol.MasakKimlikTespitleri.AsNoTracking().Single(x => x.Id == t.Id);
        Assert.Equal(TestVeritabani.TestSifreleyici.Hashle(Tckn), okunan.BelgeNoHash);
        Assert.Equal(1, okunan.SifreSurumu);
    }

    [Fact]
    public async Task AyniTckn_HashIleBulunur_SifreliSutunFarkli()
    {
        // MASAK'ın şüpheli işlem tespiti için: "bu kişi daha önce işlem yaptı mı?"
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Sifre-ara");

        using var db = vt.Olustur(k);
        db.MasakKimlikTespitleri.Add(Tespit(k, Tckn));
        db.MasakKimlikTespitleri.Add(Tespit(k, "100 000 001 46"));   // aynı TCKN, boşluklu yazılmış
        db.MasakKimlikTespitleri.Add(Tespit(k, "99999999990"));
        await db.SaveChangesAsync();

        var hash = TestVeritabani.TestSifreleyici.Hashle(Tckn);
        var ayniKisi = db.MasakKimlikTespitleri.Where(x => x.BelgeNoHash == hash).ToList();

        Assert.Equal(2, ayniKisi.Count);

        // Şifreli sütunlar birbirinden farklı (rastgele nonce) — ham SQL ile doğrula
        using var kontrol = vt.OlusturSistem();
        // Paylaşılan test DB'sinde başka testler de aynı örnek TCKN'yi yazar → kiracıya sınırla
        var hamlar = await kontrol.Database.SqlQuery<string>(
            $"SELECT belge_no AS Value FROM masak_kimlik_tespitleri WHERE belge_no_hash = {hash} AND tenant_id = {k.TenantId}").ToListAsync();
        Assert.Equal(2, hamlar.Count);
        Assert.Equal(hamlar.Count, hamlar.Distinct().Count());   // rastgele nonce: hiçbiri aynı değil
    }

    [Fact]
    public async Task BosBelgeNo_Reddedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Sifre-bos");

        using var db = vt.Olustur(k);
        db.MasakKimlikTespitleri.Add(Tespit(k, "   "));
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task SifreliKayit_KiraciFiltresineTabi()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "Sifre-A");
        var b = TestKurulum.KiraciKur(kurulum, "Sifre-B");

        using var aDb = vt.Olustur(a);
        var t = Tespit(a, Tckn);
        aDb.MasakKimlikTespitleri.Add(t);
        await aDb.SaveChangesAsync();

        using var bDb = vt.Olustur(b);
        Assert.Null(bDb.MasakKimlikTespitleri.FirstOrDefault(x => x.Id == t.Id));
        Assert.Empty(bDb.MasakKimlikTespitleri.Where(x => x.BelgeNoHash == TestVeritabani.TestSifreleyici.Hashle(Tckn)));
    }
}
