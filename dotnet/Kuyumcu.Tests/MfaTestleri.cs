using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 45 — MFA servis/veritabanı testleri (gerçek MySQL).
// Sınananlar: şifreli saklama, tekrar kapısı, kilit, kurtarma kodu tek
// kullanımlık, kapatmada parola+kod şartı, kiracı izolasyonu.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MfaTestleri(TestVeritabani vt)
{
    private static readonly MfaAyarlari Ayarlar = new();

    private static MfaServisi Servis(KuyumcuDbContext db) =>
        new(new Lazy<KuyumcuDbContext>(() => db), TestVeritabani.TestSifreleyici, Ayarlar,
            new DenetimServisi(db, new HttpContextAccessor()));

    /// <summary>Kurulum yanıtındaki gizden o an geçerli kodu üretir (doğrulayıcı uygulamanın yaptığı).</summary>
    private static string KodUret(string gizBase32, int adimKaydirma = 0) =>
        Totp.Uret(Base32.Coz(gizBase32), Totp.Adim(DateTime.UtcNow, 30) + adimKaydirma);

    private static string GizAl(object kurCevabi) =>
        (string)kurCevabi.GetType().GetProperty("gizBase32")!.GetValue(kurCevabi)!;

    private static List<string> KurtarmaAl(object cevap) =>
        (List<string>)cevap.GetType().GetProperty("kurtarmaKodlari")!.GetValue(cevap)!;

    // ── Kurulum ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Gizli_veritabaninda_duz_metin_degildir()
    {
        var kiraci = KiraciKur("MFA-giz");
        await using var db = vt.Olustur(kiraci);

        var cevap = await Servis(db).Kur(kiraci.Islem);
        var giz = GizAl(cevap);

        await using var oku = vt.Olustur(kiraci);
        var kayit = await oku.KullaniciMfalari.AsNoTracking()
            .SingleAsync(m => m.KullaniciId == kiraci.KullaniciId);

        Assert.StartsWith("v1:", kayit.GizliSifreli, StringComparison.Ordinal);
        Assert.DoesNotContain(giz, kayit.GizliSifreli, StringComparison.Ordinal);
        Assert.Equal(MfaDurumu.Bekliyor, kayit.Durum);
        // Şifreli metin gerçekten o gizi taşıyor (çözülünce aynısı çıkıyor).
        Assert.Equal(giz, TestVeritabani.TestSifreleyici.Coz(kayit.GizliSifreli));
    }

    [Fact]
    public async Task Dogrulama_etkinlestirir_ve_on_kurtarma_kodu_uretir()
    {
        var kiraci = KiraciKur("MFA-etkin");
        await using var db = vt.Olustur(kiraci);
        var servis = Servis(db);

        var giz = GizAl(await servis.Kur(kiraci.Islem));
        var sonuc = await servis.Dogrula(kiraci.Islem, new MfaKodDto { Kod = KodUret(giz) });
        var kodlar = KurtarmaAl(sonuc);

        await using var oku = vt.Olustur(kiraci);
        var kayit = await oku.KullaniciMfalari.AsNoTracking()
            .SingleAsync(m => m.KullaniciId == kiraci.KullaniciId);
        var satirlar = await oku.MfaKurtarmaKodlari.AsNoTracking()
            .Where(k => k.KullaniciId == kiraci.KullaniciId).ToListAsync();

        Assert.Equal(MfaDurumu.Etkin, kayit.Durum);
        Assert.NotNull(kayit.Etkinlesti);
        Assert.Equal(10, kodlar.Count);
        Assert.Equal(10, satirlar.Count);
        // Hiçbiri düz metin değil: yalnız 64 hex karakterlik SHA-256 durur.
        Assert.All(satirlar, s => Assert.Equal(64, s.KodHash.Length));
        foreach (var kod in kodlar)
            Assert.DoesNotContain(satirlar, s => s.KodHash.Contains(MfaKapilari.Normalize(kod), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Ayni_TOTP_kodu_ikinci_kez_reddedilir()
    {
        var kiraci = KiraciKur("MFA-tekrar");
        await using var db = vt.Olustur(kiraci);
        var servis = Servis(db);

        var giz = GizAl(await servis.Kur(kiraci.Islem));
        var kod = KodUret(giz);
        await servis.Dogrula(kiraci.Islem, new MfaKodDto { Kod = kod });

        // Aynı kod (aynı adım) ikinci kez: kurtarma kodu yenilemede denenir.
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => servis.KurtarmaYenile(kiraci.Islem, new MfaKodDto { Kod = kod }));
    }

    [Fact]
    public async Task Bes_yanlis_deneme_hesabi_kilitler()
    {
        var kiraci = KiraciKur("MFA-kilit");
        await using var db = vt.Olustur(kiraci);
        var servis = Servis(db);

        GizAl(await servis.Kur(kiraci.Islem));
        for (var i = 0; i < 5; i++)
            await Assert.ThrowsAsync<UnauthorizedException>(
                () => servis.Dogrula(kiraci.Islem, new MfaKodDto { Kod = "000000" }));

        await using var oku = vt.Olustur(kiraci);
        var kayit = await oku.KullaniciMfalari.AsNoTracking()
            .SingleAsync(m => m.KullaniciId == kiraci.KullaniciId);
        Assert.Equal(5, kayit.YanlisDeneme);
        Assert.NotNull(kayit.KilitBitis);
        Assert.True(kayit.KilitBitis > DateTime.UtcNow);
    }

    [Fact]
    public async Task Kilitliyken_dogru_kod_bile_reddedilir()
    {
        var kiraci = KiraciKur("MFA-kilit-dogru");
        await using var db = vt.Olustur(kiraci);
        var servis = Servis(db);

        var giz = GizAl(await servis.Kur(kiraci.Islem));

        // Kilit doğrudan kurulur: sahte saat gerekmez, "gelecekteki bitiş" yeter.
        var kayit = await db.KullaniciMfalari.SingleAsync(m => m.KullaniciId == kiraci.KullaniciId);
        kayit.KilitBitis = DateTime.UtcNow.AddMinutes(15);
        await db.SaveChangesAsync();

        var hata = await Assert.ThrowsAsync<ApiException>(
            () => servis.Dogrula(kiraci.Islem, new MfaKodDto { Kod = KodUret(giz) }));
        Assert.Equal(429, hata.StatusCode);

        await using var oku = vt.Olustur(kiraci);
        var sonrasi = await oku.KullaniciMfalari.AsNoTracking()
            .SingleAsync(m => m.KullaniciId == kiraci.KullaniciId);
        Assert.Equal(MfaDurumu.Bekliyor, sonrasi.Durum);   // içeri alınmadı
    }

    [Fact]
    public async Task Kurtarma_kodu_tek_kullanimliktir()
    {
        var kiraci = KiraciKur("MFA-kurtarma");
        await using var db = vt.Olustur(kiraci);
        var servis = Servis(db);

        var giz = GizAl(await servis.Kur(kiraci.Islem));
        var kodlar = KurtarmaAl(await servis.Dogrula(kiraci.Islem, new MfaKodDto { Kod = KodUret(giz) }));
        var kurtarma = kodlar[0];

        await using var kapi = vt.Olustur(kiraci);
        var kayit = await kapi.KullaniciMfalari.SingleAsync(m => m.KullaniciId == kiraci.KullaniciId);

        Assert.True(await MfaKapilari.KodDogrula(kapi, kayit, kurtarma, TestVeritabani.TestSifreleyici, Ayarlar));
        await kapi.SaveChangesAsync();

        await using var ikinci = vt.Olustur(kiraci);
        var kayit2 = await ikinci.KullaniciMfalari.SingleAsync(m => m.KullaniciId == kiraci.KullaniciId);
        Assert.False(await MfaKapilari.KodDogrula(ikinci, kayit2, kurtarma, TestVeritabani.TestSifreleyici, Ayarlar));

        await using var oku = vt.Olustur(kiraci);
        var satir = await oku.MfaKurtarmaKodlari.AsNoTracking()
            .SingleAsync(k => k.KodHash == TokenServisi.Hashle(MfaKapilari.Normalize(kurtarma)));
        Assert.True(satir.Kullanildi);
        Assert.NotNull(satir.KullanildiTarih);
        Assert.Equal(9, await oku.MfaKurtarmaKodlari.CountAsync(
            k => k.KullaniciId == kiraci.KullaniciId && !k.Kullanildi));
    }

    // ── Kapatma ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Kapatma_yanlis_parolayla_reddedilir()
    {
        var kiraci = KiraciKur("MFA-kapat-red", sifre: "Parola12345");
        await using var db = vt.Olustur(kiraci);
        var servis = Servis(db);

        var giz = GizAl(await servis.Kur(kiraci.Islem));
        await servis.Dogrula(kiraci.Islem, new MfaKodDto { Kod = KodUret(giz) });

        await Assert.ThrowsAsync<UnauthorizedException>(() => servis.Kaldir(
            kiraci.Islem, new MfaKapatDto { Sifre = "YanlisParola1", Kod = KodUret(giz, 1) }));

        await using var oku = vt.Olustur(kiraci);
        Assert.True(await oku.KullaniciMfalari.AnyAsync(m => m.KullaniciId == kiraci.KullaniciId));
    }

    [Fact]
    public async Task Kapatma_parola_ve_kod_dogruysa_kayitlari_siler()
    {
        var kiraci = KiraciKur("MFA-kapat", sifre: "Parola12345");
        await using var db = vt.Olustur(kiraci);
        var servis = Servis(db);

        var giz = GizAl(await servis.Kur(kiraci.Islem));
        await servis.Dogrula(kiraci.Islem, new MfaKodDto { Kod = KodUret(giz) });

        await servis.Kaldir(kiraci.Islem, new MfaKapatDto { Sifre = "Parola12345", Kod = KodUret(giz, 1) });

        await using var oku = vt.Olustur(kiraci);
        Assert.False(await oku.KullaniciMfalari.AnyAsync(m => m.KullaniciId == kiraci.KullaniciId));
        Assert.Equal(0, await oku.MfaKurtarmaKodlari.CountAsync(k => k.KullaniciId == kiraci.KullaniciId));
        // Denetim izi KALIR (append-only).
        Assert.True(await oku.DenetimKayitlari.AnyAsync(d => d.Detay == "mfa-kapatildi"));
    }

    // ── Kiracı izolasyonu ────────────────────────────────────────────────────

    [Fact]
    public async Task Baska_kiracinin_mfa_kaydi_gorunmez()
    {
        var a = KiraciKur("MFA-kiraci-A");
        var b = KiraciKur("MFA-kiraci-B");

        await using var dbA = vt.Olustur(a);
        await Servis(dbA).Kur(a.Islem);

        await using var dbB = vt.Olustur(b);
        Assert.Equal(0, await dbB.KullaniciMfalari.CountAsync());
        // B'nin oturumu A'nın kullanıcı kimliğini yazsa bile filtre kapatır.
        Assert.False(await dbB.KullaniciMfalari.AnyAsync(m => m.KullaniciId == a.KullaniciId));
    }

    // ── Yardımcı ─────────────────────────────────────────────────────────────

    private Kiraci KiraciKur(string ad, string? sifre = null)
    {
        using var db = vt.OlusturSistem();
        var kiraci = TestKurulum.KiraciKur(db, $"{ad}-{Guid.NewGuid():N}"[..20]);
        if (sifre is not null)
        {
            var k = db.Kullanicilar.Single(x => x.Id == kiraci.KullaniciId);
            k.SifreHash = BCrypt.Net.BCrypt.HashPassword(sifre, 12);
            db.SaveChanges();
        }
        return kiraci;
    }
}
