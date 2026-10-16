using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET UX-1-C4 — 45-API HASIM DOĞRULAYICI DEVİR MADDELERİ (a) ve (c)
//
// (a) ORTA — YAN ETKİLİ BAŞARISIZLIK: MFA kapatma parolası YANLIŞKEN de
//     kurtarma kodu TÜKETİLİYORDU. Kullanıcı tek kullanımlık kodunu, hiçbir şey
//     başaramadan yakıyordu. Kod artık YALNIZ işlem başarılı olduğunda tüketilir
//     ve tüketim kapatmayla AYNI SaveChanges'tedir.
//
// (c) DÜŞÜK — 'mfa-kurtarma-kullanildi' denetim olayı hiç yazılmıyordu: bir
//     kurtarma kodunun kullanıldığı denetim izinde görünmüyordu. Artık Hassas
//     bir kayıt yazılır; detayda KODUN KENDİSİ değil, kalan adet vardır.
//
// Bu dosya MEVCUT testlerin hiçbirini değiştirmez; yalnız EKLER (K4).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class UX1C4MfaDevirTestleri(TestVeritabani vt)
{
    private static readonly MfaAyarlari Ayarlar = new();

    private static MfaServisi Servis(KuyumcuDbContext db) =>
        new(new Lazy<KuyumcuDbContext>(() => db), TestVeritabani.TestSifreleyici, Ayarlar,
            new DenetimServisi(db, new HttpContextAccessor()));

    private static string KodUret(string gizBase32, int adimKaydirma = 0) =>
        Totp.Uret(Base32.Coz(gizBase32), Totp.Adim(DateTime.UtcNow, 30) + adimKaydirma);

    private static string GizAl(object kurCevabi) =>
        (string)kurCevabi.GetType().GetProperty("gizBase32")!.GetValue(kurCevabi)!;

    private static List<string> KurtarmaAl(object cevap) =>
        (List<string>)cevap.GetType().GetProperty("kurtarmaKodlari")!.GetValue(cevap)!;

    // ── (a) Başarısız kapatma kurtarma kodunu TÜKETMEZ ───────────────────────

    [Fact]
    public async Task Kapatma_basarisizsa_kurtarma_kodu_HALA_gecerlidir()
    {
        var kiraci = KiraciKur("C4-yanetki", sifre: "Parola12345");
        await using var db = vt.Olustur(kiraci);
        var servis = Servis(db);

        var giz = GizAl(await servis.Kur(kiraci.Islem));
        var kodlar = KurtarmaAl(await servis.Dogrula(kiraci.Islem, new MfaKodDto { Kod = KodUret(giz) }));
        var kurtarma = kodlar[0];

        // Parola YANLIŞ, kurtarma kodu DOĞRU → kapatma reddedilir.
        await Assert.ThrowsAsync<UnauthorizedException>(() => servis.Kaldir(
            kiraci.Islem, new MfaKapatDto { Sifre = "YanlisParola1", Kod = kurtarma }));

        // Kod TÜKETİLMEMİŞ olmalı: hem satır işaretsiz hem sayaç 10.
        await using var oku = vt.Olustur(kiraci);
        var satir = await oku.MfaKurtarmaKodlari.AsNoTracking()
            .SingleAsync(k => k.KodHash == TokenServisi.Hashle(MfaKapilari.Normalize(kurtarma)));
        Assert.False(satir.Kullanildi);
        Assert.Null(satir.KullanildiTarih);
        Assert.Equal(10, await oku.MfaKurtarmaKodlari.CountAsync(
            k => k.KullaniciId == kiraci.KullaniciId && !k.Kullanildi));

        // Ve gerçekten HÂLÂ ÇALIŞIYOR: doğru parolayla kapatma aynı kodla geçer.
        await using var db2 = vt.Olustur(kiraci);
        await Servis(db2).Kaldir(kiraci.Islem, new MfaKapatDto { Sifre = "Parola12345", Kod = kurtarma });

        await using var oku2 = vt.Olustur(kiraci);
        Assert.False(await oku2.KullaniciMfalari.AnyAsync(m => m.KullaniciId == kiraci.KullaniciId));
    }

    // ── (c) Kurtarma kodu kullanımı denetim izine yazılır ────────────────────

    [Fact]
    public async Task Kurtarma_kodu_kullanilinca_denetim_kaydi_yazilir_kod_YAZILMAZ()
    {
        var kiraci = KiraciKur("C4-iz", sifre: "Parola12345");
        await using var db = vt.Olustur(kiraci);
        var servis = Servis(db);

        var giz = GizAl(await servis.Kur(kiraci.Islem));
        var kodlar = KurtarmaAl(await servis.Dogrula(kiraci.Islem, new MfaKodDto { Kod = KodUret(giz) }));
        var kurtarma = kodlar[0];

        // Kurtarma kodu ile kapatma (başarılı yol).
        await servis.Kaldir(kiraci.Islem, new MfaKapatDto { Sifre = "Parola12345", Kod = kurtarma });

        await using var oku = vt.Olustur(kiraci);
        var iz = await oku.DenetimKayitlari.AsNoTracking()
            .Where(d => d.Detay != null && d.Detay.StartsWith("mfa-kurtarma-kullanildi"))
            .ToListAsync();

        var kayit = Assert.Single(iz);
        Assert.True(kayit.Hassas);
        // Kalan adet yazılır: 10 koddan biri tüketildi → 9.
        Assert.Contains("9", kayit.Detay!, StringComparison.Ordinal);
        // ⚠ KODUN KENDİSİ hiçbir denetim kaydında geçmez.
        var normalize = MfaKapilari.Normalize(kurtarma);
        Assert.DoesNotContain(normalize, kayit.Detay!, StringComparison.OrdinalIgnoreCase);
        Assert.False(await oku.DenetimKayitlari.AsNoTracking()
            .AnyAsync(d => d.Detay != null && d.Detay.Contains(normalize)));
    }

    [Fact]
    public async Task TOTP_ile_kapatmada_kurtarma_olayi_YAZILMAZ()
    {
        var kiraci = KiraciKur("C4-totp", sifre: "Parola12345");
        await using var db = vt.Olustur(kiraci);
        var servis = Servis(db);

        var giz = GizAl(await servis.Kur(kiraci.Islem));
        await servis.Dogrula(kiraci.Islem, new MfaKodDto { Kod = KodUret(giz) });
        await servis.Kaldir(kiraci.Islem, new MfaKapatDto { Sifre = "Parola12345", Kod = KodUret(giz, 1) });

        await using var oku = vt.Olustur(kiraci);
        Assert.False(await oku.DenetimKayitlari.AsNoTracking()
            .AnyAsync(d => d.Detay != null && d.Detay.StartsWith("mfa-kurtarma-kullanildi")));
        Assert.True(await oku.DenetimKayitlari.AsNoTracking()
            .AnyAsync(d => d.Detay == "mfa-kapatildi"));
    }

    // ── KodCozumle: hangi kodun kullanıldığını DOĞRU söyler ──────────────────

    [Fact]
    public async Task KodCozumle_TOTP_ve_kurtarmayi_AYIRT_eder()
    {
        var kiraci = KiraciKur("C4-cozum");
        await using var db = vt.Olustur(kiraci);
        var servis = Servis(db);

        var giz = GizAl(await servis.Kur(kiraci.Islem));
        var kodlar = KurtarmaAl(await servis.Dogrula(kiraci.Islem, new MfaKodDto { Kod = KodUret(giz) }));

        await using var a = vt.Olustur(kiraci);
        var kayitA = await a.KullaniciMfalari.SingleAsync(m => m.KullaniciId == kiraci.KullaniciId);
        Assert.Equal(MfaKodTuru.Totp, await MfaKapilari.KodCozumle(
            a, kayitA, KodUret(giz, 1), TestVeritabani.TestSifreleyici, Ayarlar));

        await using var b = vt.Olustur(kiraci);
        var kayitB = await b.KullaniciMfalari.SingleAsync(m => m.KullaniciId == kiraci.KullaniciId);
        Assert.Equal(MfaKodTuru.Kurtarma, await MfaKapilari.KodCozumle(
            b, kayitB, kodlar[1], TestVeritabani.TestSifreleyici, Ayarlar));

        await using var c = vt.Olustur(kiraci);
        var kayitC = await c.KullaniciMfalari.SingleAsync(m => m.KullaniciId == kiraci.KullaniciId);
        Assert.Equal(MfaKodTuru.Yok, await MfaKapilari.KodCozumle(
            c, kayitC, "000000", TestVeritabani.TestSifreleyici, Ayarlar));
    }

    // ── 45-UI: kullanıcı listesi MFA bayrağını taşır (giz taşımaz) ───────────

    [Fact]
    public async Task Kullanici_listesi_mfaEtkin_bayragini_dondurur()
    {
        var kiraci = KiraciKur("C4-liste");
        await using var db = vt.Olustur(kiraci);

        var liste = await new Kuyumcu.Api.Modules.Kullanici.KullaniciServisi(db, TestKurulum.Denetim(db)).Listele(kiraci.Islem);
        var satir = Assert.Single(liste, o => Alan(o, "id") as string == kiraci.KullaniciId);
        Assert.Equal(false, Alan(satir, "mfaEtkin"));

        // MFA kurulup ETKİN olunca bayrak döner.
        var giz = GizAl(await Servis(db).Kur(kiraci.Islem));
        await Servis(db).Dogrula(kiraci.Islem, new MfaKodDto { Kod = KodUret(giz) });

        await using var oku = vt.Olustur(kiraci);
        var liste2 = await new Kuyumcu.Api.Modules.Kullanici.KullaniciServisi(oku, TestKurulum.Denetim(oku)).Listele(kiraci.Islem);
        var satir2 = Assert.Single(liste2, o => Alan(o, "id") as string == kiraci.KullaniciId);
        Assert.Equal(true, Alan(satir2, "mfaEtkin"));

        // ⚠ Liste GİZ taşımaz.
        Assert.DoesNotContain("giz", System.Text.Json.JsonSerializer.Serialize(liste2),
            StringComparison.OrdinalIgnoreCase);
    }

    private static object? Alan(object o, string ad) =>
        o.GetType().GetProperty(ad)?.GetValue(o);

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
