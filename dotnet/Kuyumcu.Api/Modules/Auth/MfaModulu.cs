using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Auth;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 45 Faz A — İKİ AŞAMALI GİRİŞ (TOTP / MFA), API bacağı.
//
// FAIL-CLOSED 1: durum=ETKIN kaydı olan kullanıcı için /auth/giris asla
//   accessToken/refreshToken döndürmez (AuthModulu.Giris). Atlama yolu yok.
// FAIL-CLOSED 2: giz şifresi çözülemezse doğrulama BAŞARISIZ sayılır; kullanıcı
//   içeri alınmaz (anahtar rotasyonu/bozuk veri sessizce kapı açamaz).
//
// ÜÇ KATMANLI KABA KUVVET SAVUNMASI:
//   1. uç oran sınırı (OranSiniri.Mfa / OranSiniri.MfaGiris) — süreç içi
//   2. DB'de yanlış deneme sayacı + kilit — süreç yeniden başlasa da durur
//   3. kullanılmış TOTP adımının tekrar kabul edilmemesi (omuz sörfü)
//
// SIR HİJYENİ: giz yalnız kurulum yanıtında, kurtarma kodları yalnız üretim
// yanıtında düz metin görünür. Loglanmaz, denetim izine YAZILMAZ, ikinci bir
// uçtan okunamaz.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Davranış ayarları — hepsi yapılandırmadan okunur (ADR-0005), sır içermez.</summary>
public sealed class MfaAyarlari
{
    public int Hane { get; set; } = 6;
    public int AdimSaniye { get; set; } = 30;
    /// <summary>Kabul penceresi ±1 adım (≈ ±30 sn). Genişletmek kaba kuvvet yüzeyini büyütür.</summary>
    public int DriftAdim { get; set; } = 1;
    public int KurtarmaKoduSayisi { get; set; } = 10;
    public int MaksYanlisDeneme { get; set; } = 5;
    public int KilitDakika { get; set; } = 15;
    /// <summary>Meydan okuma jetonunun ömrü (dakika) — kısa tutulur.</summary>
    public int MeydanOkumaDakika { get; set; } = 5;
    public string Yayinci { get; set; } = Marka.AdAscii;
}

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class MfaKodDto
{
    [Required(ErrorMessage = "Doğrulama kodu zorunludur")]
    [StringLength(25, MinimumLength = 6, ErrorMessage = "Doğrulama kodu 6 haneli olmalı ya da kurtarma kodu girilmelidir")]
    public string Kod { get; set; } = null!;
}

public sealed class MfaKapatDto
{
    [Required(ErrorMessage = "Şifre zorunludur")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "Şifre 8-72 karakter olmalıdır")]
    public string Sifre { get; set; } = null!;

    [Required(ErrorMessage = "Doğrulama kodu zorunludur")]
    [StringLength(25, MinimumLength = 6, ErrorMessage = "Doğrulama kodu 6 haneli olmalı ya da kurtarma kodu girilmelidir")]
    public string Kod { get; set; } = null!;
}

// ── Ortak kapılar (hem MfaServisi hem AuthServisi kullanır) ───────────────────

/// <summary>
/// Doğrulamada HANGİ kodun kullanıldığı. Kurtarma kodu tek kullanımlıktır ve
/// tükendiğinde kullanıcının elindeki yedek azalır — bu yüzden denetim izine
/// TOTP'ten AYRI bir olay olarak yazılır (45-API doğrulayıcı bulgusu (c)).
/// </summary>
public enum MfaKodTuru
{
    /// <summary>Kod yok / eşleşmedi.</summary>
    Yok = 0,
    Totp = 1,
    Kurtarma = 2,
}

/// <summary>
/// MFA kapıları. Girişin ikinci adımı KİMLİK ÖNCESİ yolda (AuthServisi, sistem
/// kapsamı), kurulum/kapatma ise istek bağlamında çalışır; kural TEK YERDE
/// dursun diye kapılar buradadır — iki yerde iki gerçek olmaz.
/// </summary>
public static class MfaKapilari
{
    /// <summary>Crockford base32 — karışabilen I/L/O/U yok.</summary>
    private const string KurtarmaAlfabesi = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    public const int KurtarmaKoduUzunlugu = 16;   // 16 × 5 bit = 80 bit entropi

    /// <summary>Kilit doluysa DOĞRU kod bile reddedilir; sayaç sıfırlanmaz.</summary>
    public static void KilitKontrol(KullaniciMfa kayit)
    {
        if (kayit.KilitBitis is { } bitis && bitis > DateTime.UtcNow)
            throw new ApiException(429,
                $"Çok fazla hatalı deneme: hesap {(int)Math.Ceiling((bitis - DateTime.UtcNow).TotalMinutes)} dakika kilitli",
                "TooManyRequests");
    }

    /// <summary>
    /// Kodu çözümler ve doğrular. 6 hane + tümü rakam ise TOTP, 16 karakterlik
    /// normalize edilmiş dizge ise KURTARMA KODU. Başarılıysa yan etkiler
    /// (kullanılan adım / kodun tüketilmesi) işaretlenir; SaveChanges ÇAĞIRAN'ındır.
    /// </summary>
    /// <remarks>
    /// ⚠ YAN ETKİLİDİR: kurtarma kodu eşleşirse satır <c>Kullanildi=true</c>
    /// işaretlenir. Çağıran, işlemi BAŞARIYLA bitirmeyeceği bir yolda bu metodu
    /// ÇAĞIRMAMALIDIR (45-API doğrulayıcı bulgusu (a): kapatma parolası yanlışken
    /// kurtarma kodu tüketiliyordu). Bkz. <see cref="MfaServisi.Kaldir"/>.
    /// </remarks>
    public static async Task<bool> KodDogrula(
        KuyumcuDbContext db, KullaniciMfa kayit, string? kod,
        IAlanSifreleyici sifreleyici, MfaAyarlari ayarlar)
        => await KodCozumle(db, kayit, kod, sifreleyici, ayarlar) != MfaKodTuru.Yok;

    /// <summary>
    /// <see cref="KodDogrula"/> ile AYNI kapı; farkı HANGİ kodun kullanıldığını
    /// söylemesidir. Kurtarma kodu kullanıldığında denetim izine ayrı bir olay
    /// yazılabilsin diye vardır (45-API doğrulayıcı bulgusu (c)).
    /// </summary>
    public static async Task<MfaKodTuru> KodCozumle(
        KuyumcuDbContext db, KullaniciMfa kayit, string? kod,
        IAlanSifreleyici sifreleyici, MfaAyarlari ayarlar)
    {
        if (string.IsNullOrWhiteSpace(kod)) return MfaKodTuru.Yok;
        var temiz = Normalize(kod);

        if (temiz.Length == ayarlar.Hane && temiz.All(char.IsAsciiDigit))
            return TotpDogrula(kayit, temiz, sifreleyici, ayarlar) ? MfaKodTuru.Totp : MfaKodTuru.Yok;

        if (temiz.Length == KurtarmaKoduUzunlugu)
            return await KurtarmaDogrula(db, kayit, temiz) ? MfaKodTuru.Kurtarma : MfaKodTuru.Yok;

        return MfaKodTuru.Yok;
    }

    private static bool TotpDogrula(
        KullaniciMfa kayit, string kod, IAlanSifreleyici sifreleyici, MfaAyarlari ayarlar)
    {
        byte[] giz;
        try
        {
            giz = Base32.Coz(sifreleyici.Coz(kayit.GizliSifreli));
        }
        catch (Exception)
        {
            // FAIL-CLOSED 2: çözülemeyen giz = doğrulama başarısız. İçeri alınmaz.
            return false;
        }

        if (!Totp.Dogrula(giz, kod, DateTime.UtcNow, ayarlar.AdimSaniye, ayarlar.DriftAdim, out var adim))
            return false;

        // TEKRAR KAPISI: omuz sörfüyle kapılan kod ikinci kez çalışmaz.
        if (kayit.SonKullanilanAdim is { } son && adim <= son) return false;

        kayit.SonKullanilanAdim = adim;
        return true;
    }

    private static async Task<bool> KurtarmaDogrula(KuyumcuDbContext db, KullaniciMfa kayit, string kod)
    {
        var hash = TokenServisi.Hashle(kod);
        var satir = await db.MfaKurtarmaKodlari
            .FirstOrDefaultAsync(k => k.KullaniciId == kayit.KullaniciId && k.KodHash == hash && !k.Kullanildi);
        if (satir is null) return false;

        // TEK KULLANIMLIK: tüketilen kod işaretlenir, ASLA silinmez (denetim izi).
        satir.Kullanildi = true;
        satir.KullanildiTarih = DateTime.UtcNow;
        return true;
    }

    /// <summary>Yanlış deneme sayacını artırır; eşiğe gelince kilitler. Kilitlendiyse true döner.</summary>
    public static bool BasarisizIsle(KullaniciMfa kayit, MfaAyarlari ayarlar)
    {
        kayit.YanlisDeneme++;
        kayit.Guncellendi = DateTime.UtcNow;
        if (kayit.YanlisDeneme < ayarlar.MaksYanlisDeneme) return false;
        kayit.KilitBitis = DateTime.UtcNow.AddMinutes(ayarlar.KilitDakika);
        return true;
    }

    public static void BasariliIsle(KullaniciMfa kayit)
    {
        kayit.YanlisDeneme = 0;
        kayit.KilitBitis = null;
        kayit.Guncellendi = DateTime.UtcNow;
    }

    /// <summary>Girilen kodu normalize eder: ayraçlar atılır, Crockford karışmaları düzeltilir.</summary>
    public static string Normalize(string kod)
    {
        var sb = new StringBuilder(kod.Length);
        foreach (var c in kod)
        {
            if (c is '-' or ' ' or '\t') continue;
            var b = char.ToUpperInvariant(c);
            sb.Append(b switch { 'O' => '0', 'I' => '1', 'L' => '1', _ => b });
        }
        return sb.ToString();
    }

    /// <summary>10 adet kurtarma kodu: düz metin (gruplu) + SHA-256 hash'i.</summary>
    public static List<(string Duz, string Hash)> KurtarmaKodlariUret(int adet)
    {
        var liste = new List<(string, string)>(adet);
        for (var i = 0; i < adet; i++)
        {
            var ham = new char[KurtarmaKoduUzunlugu];
            for (var j = 0; j < ham.Length; j++)
                ham[j] = KurtarmaAlfabesi[RandomNumberGenerator.GetInt32(KurtarmaAlfabesi.Length)];
            var duz = new string(ham);
            // Gösterimde 4'erli gruplama; hash NORMALİZE (ayraçsız) dizgenin.
            var gosterim = string.Join('-', Enumerable.Range(0, 4).Select(g => duz.Substring(g * 4, 4)));
            liste.Add((gosterim, TokenServisi.Hashle(duz)));
        }
        return liste;
    }
}

// ── Servis ───────────────────────────────────────────────────────────────────

/// <summary>
/// MFA kurulum / doğrulama / kapatma / kurtarma kodları. JWT SONRASI yoldur:
/// istek bağlamı (kiracı filtresi AKTİF) kullanılır — bir kiracının kullanıcısı
/// başka kiracının MFA kaydını göremez.
/// </summary>
public sealed class MfaServisi(
    Lazy<KuyumcuDbContext> istekDb,
    IAlanSifreleyici sifreleyici,
    MfaAyarlari ayarlar,
    DenetimServisi denetim)
{
    private KuyumcuDbContext Db => istekDb.Value;

    // ─── Durum ───────────────────────────────────────────────────────────────

    public async Task<object> Durum(AktifKullanici islem)
    {
        var kayit = await Db.KullaniciMfalari.AsNoTracking()
            .FirstOrDefaultAsync(m => m.KullaniciId == islem.Id);
        var kalan = kayit is null ? 0 : await Db.MfaKurtarmaKodlari
            .CountAsync(k => k.KullaniciId == islem.Id && !k.Kullanildi);

        // GİZ ASLA DÖNMEZ — yalnız kurulum yanıtında bir kez görünür.
        return new
        {
            etkin = kayit?.Durum == MfaDurumu.Etkin,
            durum = kayit?.Durum,
            yontem = kayit?.Yontem,
            kalanKurtarmaKodu = kalan,
            etkinlesti = kayit?.Etkinlesti,
            kilitli = kayit?.KilitBitis is { } b && b > DateTime.UtcNow,
        };
    }

    // ─── Kurulum ─────────────────────────────────────────────────────────────

    /// <summary>Giz üretir, ŞİFRELİ kaydeder, otpauth URI döner. Zaten ETKIN ise 409.</summary>
    public async Task<object> Kur(AktifKullanici islem)
    {
        var kullanici = await Db.Kullanicilar.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == islem.Id)
            ?? throw new NotFoundException("Kullanıcı bulunamadı");

        var kayit = await Db.KullaniciMfalari.FirstOrDefaultAsync(m => m.KullaniciId == islem.Id);
        if (kayit?.Durum == MfaDurumu.Etkin)
            throw new ConflictException("İki aşamalı giriş zaten etkin. Önce kapatıp yeniden kurun.");

        var giz = RandomNumberGenerator.GetBytes(20);   // 160 bit — RFC 4226 önerisi
        var gizBase32 = Base32.Kodla(giz);
        var now = DateTime.UtcNow;

        if (kayit is null)
        {
            kayit = new KullaniciMfa
            {
                Id = Kimlik.Ulid(),
                TenantId = islem.TenantId,
                KullaniciId = islem.Id,
                Yontem = MfaYontemi.Totp,
                GizliSifreli = sifreleyici.Sifrele(gizBase32),
                Durum = MfaDurumu.Bekliyor,
                Olusturuldu = now,
                Guncellendi = now,
            };
            Db.KullaniciMfalari.Add(kayit);
        }
        else
        {
            // İkinci bir kurulum çağrısı BEKLIYOR kaydının üstüne yazar.
            kayit.GizliSifreli = sifreleyici.Sifrele(gizBase32);
            kayit.Durum = MfaDurumu.Bekliyor;
            kayit.Etkinlesti = null;
            kayit.SonKullanilanAdim = null;
            kayit.YanlisDeneme = 0;
            kayit.KilitBitis = null;
            kayit.Guncellendi = now;
        }

        // Detayda YALNIZ olay adı — giz denetim izine YAZILMAZ.
        denetim.YazmaKaydet(islem, nameof(KullaniciMfa), kayit.Id, "mfa-kuruldu");
        await Db.SaveChangesAsync();

        return new
        {
            gizBase32,
            otpauthUri = Totp.OtpauthUri(
                ayarlar.Yayinci, kullanici.Email, gizBase32, ayarlar.Hane, ayarlar.AdimSaniye),
            hane = ayarlar.Hane,
            adimSaniye = ayarlar.AdimSaniye,
        };
    }

    // ─── Kurulumu doğrula (ETKIN'e geçiş) ────────────────────────────────────

    public async Task<object> Dogrula(AktifKullanici islem, MfaKodDto dto)
    {
        // Servis içi ELLE doğrulama: DataAnnotations yalnız controller yolunda çalışır.
        if (string.IsNullOrWhiteSpace(dto.Kod)) throw new BadRequestException("Doğrulama kodu zorunludur");

        var kayit = await Db.KullaniciMfalari.FirstOrDefaultAsync(m => m.KullaniciId == islem.Id)
            ?? throw new BadRequestException("Önce iki aşamalı girişi kurmanız gerekir");
        if (kayit.Durum == MfaDurumu.Etkin)
            throw new ConflictException("İki aşamalı giriş zaten etkin");

        MfaKapilari.KilitKontrol(kayit);

        var tur = await MfaKapilari.KodCozumle(Db, kayit, dto.Kod, sifreleyici, ayarlar);
        if (tur == MfaKodTuru.Yok)
        {
            var kilitlendi = MfaKapilari.BasarisizIsle(kayit, ayarlar);
            denetim.YazmaKaydet(islem, nameof(KullaniciMfa), kayit.Id,
                kilitlendi ? "mfa-kilitlendi" : "mfa-dogrulama-basarisiz");
            await Db.SaveChangesAsync();
            throw new UnauthorizedException(
                "Doğrulama kodu hatalı. Telefonunuzun saatinin otomatik ayarlı olduğundan emin olun.");
        }
        await KurtarmaKullanimiIzle(islem, kayit, tur);

        var now = DateTime.UtcNow;
        kayit.Durum = MfaDurumu.Etkin;
        kayit.Etkinlesti = now;
        kayit.KurtarmaUretildi = now;
        MfaKapilari.BasariliIsle(kayit);

        var kodlar = await KurtarmaYaz(islem, kayit);
        denetim.YazmaKaydet(islem, nameof(KullaniciMfa), kayit.Id, "mfa-etkinlesti");
        await Db.SaveChangesAsync();

        return new { etkin = true, kurtarmaKodlari = kodlar, kalanKurtarmaKodu = kodlar.Count };
    }

    // ─── Kurtarma kodlarını yenile ───────────────────────────────────────────

    public async Task<object> KurtarmaYenile(AktifKullanici islem, MfaKodDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Kod)) throw new BadRequestException("Doğrulama kodu zorunludur");

        var kayit = await EtkinKayit(islem);
        MfaKapilari.KilitKontrol(kayit);

        var tur = await MfaKapilari.KodCozumle(Db, kayit, dto.Kod, sifreleyici, ayarlar);
        if (tur == MfaKodTuru.Yok)
        {
            var kilitlendi = MfaKapilari.BasarisizIsle(kayit, ayarlar);
            denetim.YazmaKaydet(islem, nameof(KullaniciMfa), kayit.Id,
                kilitlendi ? "mfa-kilitlendi" : "mfa-dogrulama-basarisiz");
            await Db.SaveChangesAsync();
            throw new UnauthorizedException("Doğrulama kodu hatalı");
        }
        await KurtarmaKullanimiIzle(islem, kayit, tur);

        MfaKapilari.BasariliIsle(kayit);
        kayit.KurtarmaUretildi = DateTime.UtcNow;

        // Eski kodlar geçersizleşir: yenileme talebinin anlamı budur.
        var eski = await Db.MfaKurtarmaKodlari.Where(k => k.KullaniciId == islem.Id).ToListAsync();
        Db.MfaKurtarmaKodlari.RemoveRange(eski);   // toplu silme DEĞİL: denetim izi görsün (beyaz liste 4'te kalır)

        var kodlar = await KurtarmaYaz(islem, kayit);
        denetim.YazmaKaydet(islem, nameof(KullaniciMfa), kayit.Id, "mfa-kurtarma-uretildi");
        await Db.SaveChangesAsync();

        return new { kurtarmaKodlari = kodlar, kalanKurtarmaKodu = kodlar.Count };
    }

    // ─── Kapatma (parola + kod) ──────────────────────────────────────────────

    /// <summary>
    /// MFA'yı kapatır. PAROLA <b>ve</b> geçerli kod birlikte istenir: yalnız açık
    /// oturum yetmez — çalınan bir oturum MFA'yı kapatamasın.
    /// </summary>
    public async Task<object> Kaldir(AktifKullanici islem, MfaKapatDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Sifre) || string.IsNullOrWhiteSpace(dto.Kod))
            throw new BadRequestException("Şifre ve doğrulama kodu zorunludur");

        var kullanici = await Db.Kullanicilar.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == islem.Id)
            ?? throw new NotFoundException("Kullanıcı bulunamadı");

        var kayit = await EtkinKayit(islem);
        MfaKapilari.KilitKontrol(kayit);

        // ⚠ SIRA ÖNEMLİ (45-API doğrulayıcı bulgusu (a)): kod doğrulaması YAN
        // ETKİLİDİR — kurtarma kodunu TÜKETİR. Parola yanlışken kodu hiç
        // çözümlemiyoruz; aksi hâlde başarısız bir kapatma denemesi kullanıcının
        // elindeki tek kullanımlık kodu yakıyordu. Tüketim ile kapatma artık
        // AYNI SaveChanges'tedir: biri olmazsa ikisi de olmaz.
        var sifreEslesti = BCrypt.Net.BCrypt.Verify(dto.Sifre, kullanici.SifreHash);
        var tur = sifreEslesti
            ? await MfaKapilari.KodCozumle(Db, kayit, dto.Kod, sifreleyici, ayarlar)
            : MfaKodTuru.Yok;

        if (!sifreEslesti || tur == MfaKodTuru.Yok)
        {
            var kilitlendi = MfaKapilari.BasarisizIsle(kayit, ayarlar);
            denetim.YazmaKaydet(islem, nameof(KullaniciMfa), kayit.Id,
                kilitlendi ? "mfa-kilitlendi" : "mfa-kapatma-reddedildi");
            await Db.SaveChangesAsync();
            throw new UnauthorizedException("Şifre veya doğrulama kodu hatalı");
        }
        await KurtarmaKullanimiIzle(islem, kayit, tur);

        var kodlar = await Db.MfaKurtarmaKodlari.Where(k => k.KullaniciId == islem.Id).ToListAsync();
        Db.MfaKurtarmaKodlari.RemoveRange(kodlar);
        Db.KullaniciMfalari.Remove(kayit);
        denetim.YazmaKaydet(islem, nameof(KullaniciMfa), kayit.Id, "mfa-kapatildi");
        await Db.SaveChangesAsync();

        return new { etkin = false };
    }

    // ─── Yarım kalan kurulumu sıfırla (P-09 · B-6) ───────────────────────────

    /// <summary>
    /// BEKLIYOR durumundaki bir kurulumu siler.
    /// <para>
    /// ⚠ NEDEN GEREKTİ (P-09 DoD): kullanıcı QR'ı okutup doğrulamadan vazgeçtiğinde
    /// <c>/ayarlar</c> ekranı "kurulum yarım" hâlinde kalıyor, ne kapatma
    /// (<see cref="Kaldir"/> ETKİN ister) ne de temizleme yolu görünüyordu.
    /// Kurulum yarım olduğu için giriş zaten MFA sormaz — bu bir KİLİT değil,
    /// bir ARTIK'tır; ama ekranda çıkışı olmayan bir durum bırakmak yasaktır.
    /// </para>
    /// <para>⚠ ETKİN kaydı SİLMEZ: orada parola + kod isteyen <see cref="Kaldir"/> vardır.</para>
    /// </summary>
    public async Task<object> Sifirla(AktifKullanici islem)
    {
        var kayit = await Db.KullaniciMfalari.FirstOrDefaultAsync(m => m.KullaniciId == islem.Id)
            ?? throw new BadRequestException("Sıfırlanacak yarım kurulum yok");
        if (kayit.Durum == MfaDurumu.Etkin)
            throw new ConflictException(
                "İki aşamalı giriş ETKİN. Kapatmak için şifrenizi ve doğrulama kodunuzu girin.");

        var kodlar = await Db.MfaKurtarmaKodlari.Where(k => k.KullaniciId == islem.Id).ToListAsync();
        Db.MfaKurtarmaKodlari.RemoveRange(kodlar);
        Db.KullaniciMfalari.Remove(kayit);
        denetim.YazmaKaydet(islem, nameof(KullaniciMfa), kayit.Id, "mfa-kurulum-sifirlandi");
        await Db.SaveChangesAsync();

        return new { etkin = false, durum = (string?)null, mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Yarım kalan kurulum sıfırlandı") };
    }

    // ─── Yardımcılar ─────────────────────────────────────────────────────────

    private async Task<KullaniciMfa> EtkinKayit(AktifKullanici islem)
    {
        var kayit = await Db.KullaniciMfalari.FirstOrDefaultAsync(m => m.KullaniciId == islem.Id)
            ?? throw new BadRequestException("İki aşamalı giriş kurulu değil");
        if (kayit.Durum != MfaDurumu.Etkin)
            throw new BadRequestException("İki aşamalı giriş henüz etkinleştirilmedi");
        return kayit;
    }

    /// <summary>
    /// Kurtarma kodu KULLANILDIYSA denetim izine ayrı bir Hassas olay yazar
    /// (45-API doğrulayıcı bulgusu (c)). ⚠ Detaya KODUN KENDİSİ yazılmaz;
    /// yalnız olay adı ve kalan kod sayısı girer. SaveChanges çağırmaz —
    /// çağıranın transaction'ında yazılır.
    /// </summary>
    private async Task KurtarmaKullanimiIzle(AktifKullanici islem, KullaniciMfa kayit, MfaKodTuru tur)
    {
        if (tur != MfaKodTuru.Kurtarma) return;
        // ⚠ CountAsync KULLANILMAZ: tüketim henüz SaveChanges edilmedi, sunucu
        // tarafı sayım eski hâli görürdü (kalan 1 fazla yazılırdı). Satırlar
        // TAKİPLİ çekilir — kimlik çözümlemesi sayesinde az önce işaretlenen
        // satır aynı nesne olarak döner — ve bellekte sayılır.
        var kalan = (await Db.MfaKurtarmaKodlari
                .Where(k => k.KullaniciId == kayit.KullaniciId && !k.Kullanildi)
                .ToListAsync())
            .Count(k => !k.Kullanildi);
        denetim.YazmaKaydet(islem, nameof(KullaniciMfa), kayit.Id,
            $"mfa-kurtarma-kullanildi · kalan kurtarma kodu: {kalan}");
    }

    /// <summary>Kurtarma kodlarını yazar ve DÜZ METİNLERİNİ döner (yalnız bu yanıtta).</summary>
    private async Task<List<string>> KurtarmaYaz(AktifKullanici islem, KullaniciMfa kayit)
    {
        var now = DateTime.UtcNow;
        var uretilen = MfaKapilari.KurtarmaKodlariUret(ayarlar.KurtarmaKoduSayisi);
        foreach (var (_, hash) in uretilen)
            Db.MfaKurtarmaKodlari.Add(new MfaKurtarmaKodu
            {
                Id = Kimlik.Ulid(),
                TenantId = islem.TenantId,
                KullaniciId = kayit.KullaniciId,
                KodHash = hash,
                Kullanildi = false,
                Olusturuldu = now,
            });
        await Task.CompletedTask;
        return [.. uretilen.Select(u => u.Duz)];
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

/// <summary>
/// Kendi hesabının iki aşamalı giriş ayarları. TÜM ROLLER kendi kaydına erişir;
/// başka kullanıcının kaydına erişim yolu YOKTUR (kiracı filtresi + kullanıcı
/// kimliği jetondan gelir, gövdeden değil).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/auth/mfa")]
[EnableRateLimiting(OranSiniri.Mfa)]
[HerkeseAcik("Kendi hesabının iki aşamalı giriş ayarları — kullanıcı kimliği jetondan gelir, başka kaydına yol yok")]
public sealed class MfaController(MfaServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/auth/mfa/durum — kendi MFA durumum (giz DÖNMEZ)</summary>
    [HttpGet("durum")]
    public Task<object> Durum() => servis.Durum(Islem);

    /// <summary>POST /api/v1/auth/mfa/kur — giz üretir, otpauth URI döner</summary>
    [HttpPost("kur")]
    public Task<object> Kur() => servis.Kur(Islem);

    /// <summary>POST /api/v1/auth/mfa/dogrula — kurulumu onaylar, kurtarma kodlarını bir kez döner</summary>
    [HttpPost("dogrula")]
    public Task<object> Dogrula([FromBody] MfaKodDto dto) => servis.Dogrula(Islem, dto);

    /// <summary>POST /api/v1/auth/mfa/kurtarma-yenile — eski kodları geçersiz kılar, 10 yeni kod üretir</summary>
    [HttpPost("kurtarma-yenile")]
    public Task<object> KurtarmaYenile([FromBody] MfaKodDto dto) => servis.KurtarmaYenile(Islem, dto);

    /// <summary>
    /// POST /api/v1/auth/mfa/sifirla — YARIM kalan (BEKLIYOR) kurulumu temizler.
    /// Etkin kurulumu kapatmaz (409) — onun yolu /kaldir'dır.
    /// </summary>
    [HttpPost("sifirla")]
    public Task<object> Sifirla() => servis.Sifirla(Islem);

    /// <summary>POST /api/v1/auth/mfa/kaldir — parola + kod ile kapatır</summary>
    [HttpPost("kaldir")]
    public Task<object> Kaldir([FromBody] MfaKapatDto dto) => servis.Kaldir(Islem, dto);
}
