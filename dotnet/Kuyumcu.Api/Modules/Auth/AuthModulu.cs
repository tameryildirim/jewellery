using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// Ad çakışması: `using Kuyumcu.Api.Data;` yüzünden çıplak `Kullanici` TİPE bağlanır;
// kardeş ad alanındaki çözümleyiciye takma adla erişilir (UX-3 Faz A).
using ArayuzModu = Kuyumcu.Api.Modules.Kullanici.ArayuzModu;

namespace Kuyumcu.Api.Modules.Auth;

// ═════════════════════════════════════════════════════════════════════════════
// AUTH MODÜLÜ — kayıt (tenant bootstrap), giriş, refresh rotasyonu, çıkış, ben.
// Kaynak: apps/api/src/modules/auth/*
//   • Refresh token: düz metin istemciye, SHA-256 hash'i veritabanına
//   • Rotasyon: her yenilemede eski token iptal, yenisi üretilir
//   • Çalıntı tespiti: iptal edilmiş token yeniden kullanılırsa kullanıcının
//     TÜM refresh tokenleri iptal edilir
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class KayitDto
{
    /// <summary>İşletme / mağaza adı</summary>
    [Required, MaxLength(200)]
    public string IsletmeAdi { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Ad { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Soyad { get; set; } = null!;

    // PAKET GOC-D (K3): [EmailAddress] yalnız "@" arıyordu ("a@b" geçiyordu) — Core EpostaDogrulayici
    // (ekrandaki <Eposta> ile AYNI kural ve sözlük metni). Uzunluk sınırı kuralın içinde (254).
    [Required, Kuyumcu.Api.Common.Dogrulama.Eposta]
    public string Email { get; set; } = null!;

    // PAKET GOC-D (K31, envanter "dükkân kaydı parolası 'aaaaaaaa' olabiliyor"): proje parola
    // politikası — 8–72 karakter, en az bir harf ve bir rakam (Core ParolaKurali; sıfırlama ve
    // kullanıcı uçlarıyla AYNI kural). Eski sınır 128'di: bcrypt 72 bayttan sonrasını yok sayar.
    [Required, Parola]
    public string Sifre { get; set; } = null!;

    /// <summary>
    /// Sistem yöneticisinin ürettiği TEK KULLANIMLIK davet jetonu — paket F-2-min.
    /// <para>
    /// Bu alan olmadan kayıt AÇILMAZ (fail-closed): "bedava sınırsız kiracı
    /// açma" bulgusu bu zorunlulukla kapanır. Jeton ham hâliyle saklanmaz,
    /// yalnız SHA-256 özeti ile eşleştirilir.
    /// </para>
    /// </summary>
    /// <remarks>
    /// ⚠ SAPMA (gerekçesi raporda): tasarım metni burada [Required] öngörüyordu,
    /// ama [Required] gövdeden EKSİK jetona 400 (alan doğrulama), GEÇERSİZ jetona
    /// 403 döndürürdü — yani uç "böyle bir alan göndermedin" ile "jetonun
    /// tutmadı" ayrımını sızdıran bir oracle olurdu. İş kuralı ("davetsiz istek
    /// 403, tek tip mesaj") üstündür: doğrulama SERVİSTE yapılır ve eksik · boş ·
    /// geçersiz · kullanılmış · süresi geçmiş HEPSİ aynı 403'ü alır.
    /// Ekranda alan yine zorunludur (Giris.razor · form.Zorunlu).
    /// </remarks>
    [MaxLength(200)]
    public string? DavetJetonu { get; set; }
}

/// <summary>
/// PAKET GOC-D — K31 parola politikası özniteliği (Core <see cref="Kuyumcu.Core.Domain.Girdi.ParolaKurali"/>).
/// Metin AuthSifirlaSozlugu.ParolaKurali — ekranın alan altı/kilit metniyle AYNI anahtar.
/// Boş değer geçer: zorunluluk [Required]'ın işidir.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class ParolaAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is not string p || p.Length == 0 || Kuyumcu.Core.Domain.Girdi.ParolaKurali.Gecerli(p))
            return ValidationResult.Success;
        return new ValidationResult(Kuyumcu.Core.I18n.AuthSifirlaSozlugu.Al(Kuyumcu.Core.I18n.IstekDili.Gecerli).ParolaKurali,
            ctx.MemberName is { Length: > 0 } m ? new[] { m } : null);
    }
}

public sealed class GirisDto
{
    [Required, EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    public string Email { get; set; } = null!;

    [Required, StringLength(72, MinimumLength = 8, ErrorMessage = "Şifre 8-72 karakter olmalıdır")]
    public string Sifre { get; set; } = null!;

    /// <summary>Aynı e-posta birden fazla işletmede kayıtlıysa zorunlu</summary>
    [StringLength(36, MinimumLength = 26, ErrorMessage = "Geçersiz tenant ID")]
    public string? TenantId { get; set; }
}

public sealed class RefreshDto
{
    // Nest 43 karakter (32 byte) üretirdi; TokenServisi 64 karakter (48 byte)
    // üretir — geçiş dönemi için her iki uzunluk da kabul edilir.
    [Required, StringLength(64, MinimumLength = 43, ErrorMessage = "Geçersiz refresh token")]
    public string RefreshToken { get; set; } = null!;
}

/// <summary>
/// Girişin İKİNCİ adımı (paket 45). Meydan okuma jetonu GÖVDEDE DEĞİL,
/// <c>Authorization: Bearer</c> başlığında taşınır: oran sınırı bölütü jetonu
/// istek gövdesini okumadan, İMZA DOĞRULAYARAK çözebilsin (Mehmet'in şartı).
/// </summary>
public sealed class MfaGirisDto
{
    [Required(ErrorMessage = "Doğrulama kodu zorunludur")]
    [StringLength(25, MinimumLength = 6, ErrorMessage = "Doğrulama kodu 6 haneli olmalı ya da kurtarma kodu girilmelidir")]
    public string Kod { get; set; } = null!;
}

// ── Servis ───────────────────────────────────────────────────────────────────

/// <summary>
/// Kimlik doğrulama servisi.
///
/// KİRACI BAĞLAMI (ADIM1-KARAR §3/Ç1): Kayit/Giris/Yenile/Cikis/TumTokenlerIptalEt
/// kiracı ÖNCESİ çalışır — kullanıcı e-posta ile, token hash ile aranır, kiracı
/// o an henüz bilinmez. Bu yollar SİSTEM kapsamı açar (filtresiz).
/// `Ben` JWT sonrasıdır ve istek bağlamında kalır (Mehmet'in şartı) — bu yüzden
/// istek DbContext'i Lazy'dir: anonim uçlarda hiç kurulmaz, kurulsaydı
/// kiracısız bağlam yüzünden ctor'da patlardı.
/// </summary>
public sealed class AuthServisi(
    SistemKapsami kapsam, Lazy<KuyumcuDbContext> istekDb, TokenServisi tokenServisi,
    IAlanSifreleyici sifreleyici, MfaAyarlari mfaAyarlari,
    Microsoft.AspNetCore.Http.IHttpContextAccessor erisim)
{
    private const int BcryptRounds = 12;

    /// <summary>
    /// Davet reddinin TEK TİP mesajı (paket F-2-min): jetonun var olup
    /// olmadığını, süresinin mi dolduğunu yoksa kullanılmış mı olduğunu
    /// SIZDIRMAZ — aksi hâlde uç bir jeton doğrulama oracle'ı olurdu.
    /// </summary>
    private const string DavetRedMesaji =
        "Geçerli bir davet jetonu gerekiyor. Kayıt yalnız yöneticinin ürettiği "
        + "tek kullanımlık davetle açılır; jetonunuz geçersiz, kullanılmış ya da süresi dolmuş olabilir.";

    // ─── Tenant kayıt (bootstrap) ────────────────────────────────────────────
    /// <summary>
    /// Yeni tenant + varsayılan şube + SAHIP kullanıcı oluşturur.
    /// <para>
    /// ⚠ PAKET F-2-min: kayıt artık TEK KULLANIMLIK BİR DAVETLE açılır. Davet
    /// doğrulaması ve TÜKETİMİ kayıt transaction'ının İÇİNDEDİR: kayıt geri
    /// alınırsa davet de geri gelir, eşzamanlı iki istekten yalnız biri başarır.
    /// </para>
    /// <para>
    /// ⚠ Davet hatası TEK TİP mesaj döndürür: "jeton var ama süresi geçmiş" ile
    /// "böyle bir jeton yok" ayrımı sızdırılmaz.
    /// </para>
    /// </summary>
    public async Task<object> Kayit(KayitDto dto, string ipAdresi, string userAgent)
    {
        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();

        // DataAnnotations YALNIZ controller yolunda koşar → servis içi elle kontrol.
        if (string.IsNullOrWhiteSpace(dto.DavetJetonu))
            throw new ForbiddenException(DavetRedMesaji);
        // PAKET GOC-D: aynı kural servis yolunda da (parola K31, e-posta K3) — DTO özniteliğiyle AYNI metin.
        if (!Kuyumcu.Core.Domain.Girdi.ParolaKurali.Gecerli(dto.Sifre))
            throw new BadRequestException(Kuyumcu.Core.I18n.AuthSifirlaSozlugu.Al(Kuyumcu.Core.I18n.IstekDili.Gecerli).ParolaKurali).AlanIle("sifre");
        Kuyumcu.Api.Common.Dogrulama.GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.EpostaDogrulayici.Denetle(dto.Email), "email");

        // Kiracılar arası e-posta tekilliği: kasten filtresiz.
        var mevcutKullanici = await db.Kullanicilar.AsNoTracking()
            .AnyAsync(k => k.Email == dto.Email);
        if (mevcutKullanici) throw new ConflictException("Bu e-posta adresi zaten kayıtlı");

        var tenantId = Kimlik.Ulid();
        var subeId = Kimlik.Ulid();
        var userId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        var sifreHash = BCrypt.Net.BCrypt.HashPassword(dto.Sifre, BcryptRounds);

        await using var tx = await db.Database.BeginTransactionAsync();

        // ── DAVET KAPISI (fail-closed) ───────────────────────────────────────
        var jetonHash = Modules.Sistem.SistemServisi.Ozet(dto.DavetJetonu);
        var davet = await db.Davetler.AsNoTracking()
            .FirstOrDefaultAsync(d => d.JetonHash == jetonHash);
        if (davet is null || davet.Iptal || davet.KullanildiMi || davet.Gecerlilik < now)
            throw new ForbiddenException(DavetRedMesaji);

        // TÜKETİM — koşullu UPDATE. Eşzamanlı ikinci istek satır kilidini
        // bekler, sonra kullanildi_mi=1 görüp 0 satır günceller ve reddedilir.
        // (ExecuteUpdate bilinçli: davet IKiraciVarlik DEĞİLDİR, yazma denetçisi
        //  ve denetim izi zaten bu tabloya işlemez — denetim.sh beyaz listesi.)
        var tuketildi = await db.Davetler
            .Where(d => d.Id == davet.Id && !d.KullanildiMi)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.KullanildiMi, true)
                .SetProperty(d => d.KullananTenantId, tenantId)
                .SetProperty(d => d.Guncellendi, now));
        if (tuketildi != 1) throw new ForbiddenException(DavetRedMesaji);

        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Ad = dto.IsletmeAdi,
            Plan = "BASLANGIC",
            AiEtkin = false,
            Aktif = true,
            // Yeni kiracı DENEME doğar; süresi davetin belirlediği kadardır.
            Durum = KiraciDurumlari.Deneme,
            DenemeBitis = now.AddDays(davet.DenemeGun),
            DavetId = davet.Id,
            // PAKET EKSIK-KUCUK (B2-11 · oylanmış karar D1): YENİ işletme BASİT modda
            // açılır. Yalnız bu yeni kayıt yolunda yazılır; varlık başlangıç değeri ve
            // sütunun DB varsayılanı (PROFESYONEL) DEĞİŞMEDİ → mevcut kiracıların
            // satırına dokunulmaz, migration yoktur.
            VarsayilanArayuzModu = Modules.Kullanici.ArayuzModu.Basit,
            // KUR KAYNAKLARI (kurucu 17.10): YENİ işletmenin has bedeli (KDVK 23/e) kaynağı Borsa
            // İstanbul kapanışıdır. Aynı D1 kalıbı: yalnız bu kayıt yolunda yazılır; boş sütun
            // (mevcut kiracılar) bugünkü davranışla (ISLEM_KURU) sürer — faturalama geçişle kesilmez.
            OzelMatrahKaynagi = Modules.Kur.KurKaynakKodlari.OzelMatrahVarsayilan,
            Olusturuldu = now,
            Guncellendi = now,
        });
        db.Subeler.Add(new Data.Sube
        {
            Id = subeId,
            TenantId = tenantId,
            Ad = "Merkez Şube",
            Aktif = true,
            Olusturuldu = now,
            Guncellendi = now,
        });
        db.Kullanicilar.Add(new Data.Kullanici
        {
            Id = userId,
            TenantId = tenantId,
            SubeId = subeId,
            Ad = dto.Ad,
            Soyad = dto.Soyad,
            Email = dto.Email,
            SifreHash = sifreHash,
            Rol = "SAHIP",
            Aktif = true,
            Olusturuldu = now,
            Guncellendi = now,
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        var accessToken = tokenServisi.AccessUret(userId, tenantId, subeId, "SAHIP");
        var refreshToken = await RefreshUretVeKaydet(db, userId, tenantId, ipAdresi, userAgent);

        return new { tenantId, accessToken, refreshToken };
    }

    // ─── Giriş ───────────────────────────────────────────────────────────────

    public async Task<object> Giris(GirisDto dto, string ipAdresi, string userAgent)
    {
        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();

        // tenantId verilmediyse yalnızca e-posta ile ara; aynı e-posta birden
        // fazla tenant'ta kayıtlıysa (composite unique buna izin verir) tenantId iste
        var q = db.Kullanicilar.AsNoTracking()
            .Where(k => k.Email == dto.Email && k.Aktif);
        if (!string.IsNullOrEmpty(dto.TenantId))
            q = q.Where(k => k.TenantId == dto.TenantId);

        var adaylar = await q.OrderBy(k => k.TenantId).Take(AzamiAdayIsletme).ToListAsync();   // B-8: aynı e-posta en çok bu kadar işletmede aranır

        // ── B-35 · P-09: KULLANICI VARLIĞI İFŞA EDİLMEZ ──────────────────────
        // Eskiden burada 404 "Kullanıcı bulunamadı" dönüyordu: saldırgan hangi
        // e-postanın kayıtlı olduğunu tek istekle öğreniyordu. Artık YOK olan
        // kullanıcı da YANLIŞ şifre de AYNI generic 401'i alır.
        // ⚠ ZAMANLAMA da eşitlenir: kullanıcı yoksa BCrypt hiç çalışmıyordu ve
        //   yanıt gözle görülür biçimde hızlı dönüyordu (yan kanal). Sahte bir
        //   hash doğrulaması aynı maliyeti ödetir.
        if (adaylar.Count == 0)
        {
            BCrypt.Net.BCrypt.Verify(dto.Sifre ?? "", SahteHash);
            throw new UnauthorizedException(GirisHatasi);
        }

        // ── 02.11 (kurucu canlı sunucuda yakaladı): aynı e-posta birden fazla işletmede
        // kayıtlıysa eskiden "tenantId belirtiniz" diye TEKNİK bir 401 dönüyordu ve ekranda
        // işletme seçtirme yoktu → kullanıcı hiç giremiyordu. Şimdi parola HER adayda
        // denetlenir; parolası tutan tek işletme varsa doğrudan girilir, birden fazlaysa
        // yalnız o işletmelerin listesi döner ve istemci tenantId ile yeniden ister.
        // ⚠ Liste PAROLA KANITLANMADAN dönmez: yanlış parolayla işletme adı sızmaz
        //   (e-posta sayımı engeli korunur, yanıt yine generic 401).
        var eslesenler = adaylar.Where(k => BCrypt.Net.BCrypt.Verify(dto.Sifre, k.SifreHash)).ToList();
        if (eslesenler.Count == 0) throw new UnauthorizedException(GirisHatasi);
        if (eslesenler.Count > 1)
        {
            var kiraciIdleri = eslesenler.Select(k => k.TenantId).ToList();
            var kiraciAdlari = await db.Tenants.AsNoTracking()
                .Where(t => kiraciIdleri.Contains(t.Id)).OrderBy(t => t.Id).Take(AzamiAdayIsletme)
                .ToDictionaryAsync(t => t.Id, t => t.Ad);
            return new
            {
                isletmeSecimi = true,
                isletmeler = eslesenler
                    .Select(k => new Kuyumcu.Contracts.Giris.IsletmeSecenegi(k.TenantId, kiraciAdlari.GetValueOrDefault(k.TenantId, k.TenantId), k.Rol))
                    .OrderBy(x => x.Ad, StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), true))
                    .ToList(),
            };
        }
        var kullanici = eslesenler[0];

        // ── FAIL-CLOSED 1 (paket 45): MFA ETKİNSE BURADA TOKEN ÜRETİLMEZ ─────
        // Kullanıcı yalnız kısa ömürlü, AYRI AUDIENCE'lı bir meydan okuma jetonu
        // alır; access/refresh ancak ikinci adımdan (POST /auth/mfa/giris) çıkar.
        // MFA servisi patlarsa istisna yukarı gider ve giriş REDDEDİLİR — atlama yolu yok.
        var mfaEtkin = await db.KullaniciMfalari.AsNoTracking()
            .AnyAsync(m => m.KullaniciId == kullanici.Id && m.Durum == MfaDurumu.Etkin);
        if (mfaEtkin)
            return new
            {
                mfaGerekli = true,
                mfaToken = tokenServisi.MfaMeydanOkumaUret(
                    kullanici.Id, kullanici.TenantId, kullanici.SifreHash, mfaAyarlari.MeydanOkumaDakika),
                gecerlilikDakika = mfaAyarlari.MeydanOkumaDakika,
            };

        var accessToken = tokenServisi.AccessUret(
            kullanici.Id, kullanici.TenantId, kullanici.SubeId, kullanici.Rol, kullanici.SaltOkunur);
        var refreshToken = await RefreshUretVeKaydet(
            db, kullanici.Id, kullanici.TenantId, ipAdresi, userAgent);

        await SonGirisYaz(db, kullanici.Id);

        var etkinMod = ArayuzModu.Coz(kullanici.ArayuzModu, await KiraciVarsayilanModu(db, kullanici.TenantId));

        return new { accessToken, refreshToken, kullanici = Temizle(kullanici, etkinMod) };
    }

    // ─── Girişin ikinci adımı (paket 45) ─────────────────────────────────────

    /// <summary>
    /// Meydan okuma jetonu + TOTP/kurtarma kodu → access + refresh.
    /// <para>
    /// Kapılar sırayla: jeton (imza + audience + amaç + ömür) → kullanıcı aktif →
    /// <b>pv</b> (parola değişmemiş) → MFA kaydı ETKIN → DB kilidi → kod (TOTP
    /// tekrar kapısı ya da tek kullanımlık kurtarma kodu).
    /// </para>
    /// </summary>
    public async Task<object> GirisMfa(string? mfaToken, string? kod, string ipAdresi, string userAgent)
    {
        var cozum = tokenServisi.MfaMeydanOkumaCoz(mfaToken)
            ?? throw new UnauthorizedException("Doğrulama oturumu geçersiz ya da süresi doldu, yeniden giriş yapın");

        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();

        var kullanici = await db.Kullanicilar.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == cozum.KullaniciId && k.TenantId == cozum.TenantId && k.Aktif)
            ?? throw new UnauthorizedException("Kullanıcı aktif değil");

        // Parola değiştiyse elde kalan meydan okuma jetonu ÖLÜR.
        if (!string.Equals(TokenServisi.ParolaSurumu(kullanici.SifreHash), cozum.ParolaSurumu, StringComparison.Ordinal))
            throw new UnauthorizedException("Doğrulama oturumu geçersiz ya da süresi doldu, yeniden giriş yapın");

        var kayit = await db.KullaniciMfalari
            .FirstOrDefaultAsync(m => m.KullaniciId == kullanici.Id && m.Durum == MfaDurumu.Etkin)
            ?? throw new UnauthorizedException("İki aşamalı giriş etkin değil");

        MfaKapilari.KilitKontrol(kayit);

        var kodTuru = await MfaKapilari.KodCozumle(db, kayit, kod, sifreleyici, mfaAyarlari);
        if (kodTuru == MfaKodTuru.Yok)
        {
            var kilitlendi = MfaKapilari.BasarisizIsle(kayit, mfaAyarlari);
            MfaIzi(db, kullanici, kilitlendi ? "mfa-kilitlendi" : "mfa-giris-basarisiz", kayit.Id);
            await db.SaveChangesAsync();
            throw new UnauthorizedException(
                "Doğrulama kodu hatalı. Telefonunuzun saatinin otomatik ayarlı olduğundan emin olun.");
        }

        // 45-API doğrulayıcı bulgusu (c): kurtarma kodu kullanıldığında AYRI bir
        // Hassas olay yazılır. Detayda kodun kendisi DEĞİL, kalan adet vardır.
        if (kodTuru == MfaKodTuru.Kurtarma)
        {
            var kalanKod = (await db.MfaKurtarmaKodlari
                    .Where(k => k.KullaniciId == kayit.KullaniciId && !k.Kullanildi)
                    .ToListAsync())
                .Count(k => !k.Kullanildi);
            MfaIzi(db, kullanici, $"mfa-kurtarma-kullanildi · kalan kurtarma kodu: {kalanKod}", kayit.Id);
        }

        MfaKapilari.BasariliIsle(kayit);
        MfaIzi(db, kullanici, "mfa-giris-basarili", kayit.Id);
        await db.SaveChangesAsync();

        var accessToken = tokenServisi.AccessUret(
            kullanici.Id, kullanici.TenantId, kullanici.SubeId, kullanici.Rol, kullanici.SaltOkunur);
        var refreshToken = await RefreshUretVeKaydet(
            db, kullanici.Id, kullanici.TenantId, ipAdresi, userAgent);

        // ⚠ SonGiris yazımı birinci adımdan buraya TAŞINDI (kopyalanmadı):
        // toplu güncelleme sayısı artmaz (denetim.sh beyaz listesi 4'te kalır).
        await SonGirisYaz(db, kullanici.Id);

        var etkinMod = ArayuzModu.Coz(kullanici.ArayuzModu, await KiraciVarsayilanModu(db, kullanici.TenantId));
        return new { accessToken, refreshToken, kullanici = Temizle(kullanici, etkinMod) };
    }

    /// <summary>MFA olayının denetim izi (Hassas). Detayda YALNIZ olay adı — kod/giz yazılmaz.</summary>
    private void MfaIzi(KuyumcuDbContext db, Data.Kullanici kullanici, string olay, string kayitId)
    {
        var iz = DenetimServisi.Olustur(
            kullanici.TenantId, kullanici.Id, DenetimEylemi.Ekleme, nameof(KullaniciMfa), kayitId, olay,
            erisim, subeId: kullanici.SubeId);
        iz.Hassas = true;
        db.DenetimKayitlari.Add(iz);
    }

    /// <summary>
    /// Son giriş damgası — TEK toplu güncelleme noktası. Birinci adım (MFA'sız) ve
    /// ikinci adım (MFA'lı) aynı satırı kullanır; ikinci bir kopya AÇILMAZ.
    /// </summary>
    private static Task SonGirisYaz(KuyumcuDbContext db, string kullaniciId) =>
        db.Kullanicilar.Where(k => k.Id == kullaniciId)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.SonGiris, DateTime.UtcNow));

    // ─── Token yenileme (rotasyon) ───────────────────────────────────────────

    public async Task<object> Yenile(string rawRefreshToken, string ipAdresi, string userAgent)
    {
        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();

        var tokenHash = TokenServisi.Hashle(rawRefreshToken);
        var now = DateTime.UtcNow;

        var kayit = await db.RefreshTokenler
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        // Çalıntı tespiti: iptal edilmiş token yeniden kullanıldıysa
        // kullanıcının TÜM tokenlerini iptal et
        if (kayit is not null && kayit.IptalEdildi)
        {
            await TumTokenlerIptalEt(db, kayit.KullaniciId);
            throw new UnauthorizedException("Refresh token geçersiz veya süresi dolmuş");
        }

        if (kayit is null || kayit.Gecerlilik < now)
        {
            // Süresi dolmuşsa güvenlik önlemi: tüm tokenlerini iptal et
            if (kayit is not null) await TumTokenlerIptalEt(db, kayit.KullaniciId);
            throw new UnauthorizedException("Refresh token geçersiz veya süresi dolmuş");
        }

        // Token rotate: eskiyi iptal et
        kayit.IptalEdildi = true;
        await db.SaveChangesAsync();

        var kullanici = await db.Kullanicilar.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == kayit.KullaniciId);
        if (kullanici is null || !kullanici.Aktif)
            throw new UnauthorizedException("Kullanıcı aktif değil");

        var accessToken = tokenServisi.AccessUret(
            kullanici.Id, kullanici.TenantId, kullanici.SubeId, kullanici.Rol, kullanici.SaltOkunur);
        var refreshToken = await RefreshUretVeKaydet(
            db, kullanici.Id, kullanici.TenantId, ipAdresi, userAgent);

        return new { accessToken, refreshToken };
    }

    // ─── Çıkış ───────────────────────────────────────────────────────────────

    public async Task Cikis(string rawRefreshToken)
    {
        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();

        var tokenHash = TokenServisi.Hashle(rawRefreshToken);
        await db.RefreshTokenler.Where(t => t.TokenHash == tokenHash)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IptalEdildi, true));
    }

    // ─── Mevcut kullanıcı ────────────────────────────────────────────────────

    public async Task<object> Ben(AktifKullanici islem)
    {
        // JWT sonrası: istek bağlamı (kiracı filtresi AKTİF).
        // B-35 · P-09: geçerli jeton + silinmiş/başka kiracıya ait kimlik = 404
        // DEĞİL, generic 401. 404 "bu kimlik vardı ama artık yok" bilgisini
        // sızdırıyordu; istemci de 404'ü "sayfa yok" sanıp oturumu kapatmıyordu.
        var kullanici = await istekDb.Value.Kullanicilar.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == islem.Id)
            ?? throw new UnauthorizedException(GirisHatasi);

        // ETKİN MOD YALNIZ SUNUCUDA ÇÖZÜLÜR (Bertan KOŞUL 6 · Mehmet K6):
        // istemci ArayuzModu.Coz mantığını tekrarlamaz, değeri JWT claim.inde taşımaz.
        var etkinMod = ArayuzModu.Coz(
            kullanici.ArayuzModu, await KiraciVarsayilanModu(istekDb.Value, islem.TenantId));
        return Temizle(kullanici, etkinMod);
    }

    // ─── Yardımcı metodlar ───────────────────────────────────────────────────

    /// <summary>
    /// B-35 · P-09 — TEK generic kimlik hatası. "Kullanıcı bulunamadı" /
    /// "şifre hatalı" ayrımı YAPILMAZ: ikisi de bu metni alır.
    /// </summary>
    internal const string GirisHatasi = "E-posta veya şifre hatalı";
    /// <summary>02.11: aynı e-postanın aranacağı en çok işletme sayısı (sınırsız okuma yok — B-8).</summary>
    internal const int AzamiAdayIsletme = 20;

    /// <summary>
    /// Kullanıcı bulunamadığında doğrulanan SAHTE hash — zamanlama yan kanalını
    /// kapatır. Gerçek bir parolanın hash'i DEĞİLDİR; sabit, anlamsız bir tuzdur.
    /// </summary>
    private static readonly string SahteHash =
        BCrypt.Net.BCrypt.HashPassword("kuyumcu-p09-zamanlama-esitleyici");

    /// <summary>Rastgele refresh token üret, hash'ini kaydet, düz metni döndür.</summary>
    private async Task<string> RefreshUretVeKaydet(
        KuyumcuDbContext db, string kullaniciId, string tenantId, string ipAdresi, string userAgent)
    {
        var (duzMetin, hash) = tokenServisi.RefreshUret();
        var now = DateTime.UtcNow;

        db.RefreshTokenler.Add(new RefreshToken
        {
            Id = Kimlik.Ulid(),
            KullaniciId = kullaniciId,
            TenantId = tenantId,
            TokenHash = hash,
            IpAdresi = ipAdresi,
            UserAgent = userAgent,
            Gecerlilik = now.AddDays(tokenServisi.RefreshGun),
            IptalEdildi = false,
            Olusturuldu = now,
        });
        await db.SaveChangesAsync();

        return duzMetin;
    }

    /// <summary>Kullanıcının TÜM refresh jetonlarını iptal eder (çalıntı tespiti + AUTH-SIFIRLA madde 4).</summary>
    internal static async Task TumTokenlerIptalEt(KuyumcuDbContext db, string kullaniciId) =>
        await db.RefreshTokenler.Where(t => t.KullaniciId == kullaniciId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IptalEdildi, true));

    /// <summary>Kiracının varsayılan arayüz modu (satır yoksa null → çözümleyici PROFESYONEL der).</summary>
    private static async Task<string?> KiraciVarsayilanModu(KuyumcuDbContext db, string tenantId) =>
        await db.Tenants.AsNoTracking().Where(t => t.Id == tenantId)
            .Select(t => t.VarsayilanArayuzModu).FirstOrDefaultAsync();

    /// <summary>sifreHash hariç tüm kullanıcı alanları (Nest kullaniciTemizle).</summary>
    private static object Temizle(Data.Kullanici k, string etkinArayuzModu) => new
    {
        id = k.Id,
        tenantId = k.TenantId,
        subeId = k.SubeId,
        ad = k.Ad,
        soyad = k.Soyad,
        email = k.Email,
        rol = k.Rol,
        saltOkunur = k.SaltOkunur,   // D1-14
        arayuzModu = k.ArayuzModu,
        etkinArayuzModu,
        aktif = k.Aktif,
        sonGiris = k.SonGiris,
        olusturuldu = k.Olusturuldu,
        guncellendi = k.Guncellendi,
    };
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/auth")]
public sealed class AuthController(AuthServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);
    private string Ip => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
    private string Ua => Request.Headers.UserAgent.ToString();

    /// <summary>POST /api/v1/auth/kayit — Yeni tenant + SAHIP kullanıcı (bootstrap)</summary>
    [HttpPost("kayit")]
    [EnableRateLimiting(OranSiniri.Kayit)]
    [AllowAnonymous]
    public async Task<IActionResult> Kayit([FromBody] KayitDto dto) =>
        StatusCode(StatusCodes.Status201Created, await servis.Kayit(dto, Ip, Ua));

    /// <summary>POST /api/v1/auth/giris — E-posta + şifre ile oturum aç</summary>
    [HttpPost("giris")]
    [EnableRateLimiting(OranSiniri.Giris)]
    [AllowAnonymous]
    public Task<object> Giris([FromBody] GirisDto dto) => servis.Giris(dto, Ip, Ua);

    /// <summary>
    /// POST /api/v1/auth/mfa/giris — girişin ikinci adımı.
    /// Meydan okuma jetonu <c>Authorization: Bearer</c> başlığında gelir; access
    /// token buradan GEÇEMEZ (audience 'kuyumcu-mfa' değil).
    /// </summary>
    [HttpPost("mfa/giris")]
    [EnableRateLimiting(OranSiniri.MfaGiris)]
    [AllowAnonymous]
    public Task<object> GirisMfa([FromBody] MfaGirisDto dto)
    {
        var auth = Request.Headers.Authorization.ToString();
        var jeton = auth.StartsWith("Bearer ", StringComparison.Ordinal) ? auth["Bearer ".Length..] : null;
        return servis.GirisMfa(jeton, dto.Kod, Ip, Ua);
    }

    /// <summary>POST /api/v1/auth/yenile — Refresh token ile yeni access token (rotasyon)</summary>
    [HttpPost("yenile")]
    [EnableRateLimiting(OranSiniri.Yenile)]
    [AllowAnonymous]
    public Task<object> Yenile([FromBody] RefreshDto dto) => servis.Yenile(dto.RefreshToken, Ip, Ua);

    /// <summary>POST /api/v1/auth/cikis — Refresh token'ı geçersiz kıl (204)</summary>
    [HttpPost("cikis")]
    [AllowAnonymous]
    public async Task<IActionResult> Cikis([FromBody] RefreshDto dto)
    {
        await servis.Cikis(dto.RefreshToken);
        return NoContent();
    }

    /// <summary>GET /api/v1/auth/ben — Oturum açmış kullanıcının bilgileri</summary>
    [HttpGet("ben")]
    [HerkeseAcik("Kendi profilin — jetondaki kimlikle sınırlı, başka kullanıcı okunamaz")]
    public Task<object> Ben() => servis.Ben(Islem);
}
