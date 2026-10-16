using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Meta;
using Kuyumcu.Api.Modules.Telemetri;
using Kuyumcu.Core.Sozlesmeler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Destek;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-31 (R-5 · T-4b) — MASKELİ DESTEK (TANILAMA) PAKETİ + İSTEMCİ HATA KODU.
//
// HEDEF: işletme sahibi bir sorun yaşadığında destek ekibine KİŞİSEL VERİ
//   İÇERMEYEN bir tanılama dosyası verebilsin; ekrandaki her beklenmeyen hata
//   kısa bir kod göstersin ve o kod sunucu günlüğünde bulunsun.
//
// ⚠ ALLOW-LIST (kara liste YOK): pakete giren her alan aşağıdaki
//   `TanilamaPaketi` ailesinde TEK TEK sayılıdır. Yeni bir tablo/alan eklemek
//   pakete kendiliğinden bir şey sokmaz. dotnet/denetim.sh
//   TANILAMA_PAKETINDE_SIR sayacı bu kayıtların alan adlarını ve içerik
//   üreten bölgeyi (>>> PAKET-ICERIGI) tarar.
//
// ⚠ FAIL-CLOSED DENETİM: paket, üretildikten SONRA ve kullanıcıya verilmeden
//   ÖNCE `TanilamaDenetcisi` ile taranır. TCKN, telefon, e-posta, IBAN, kart
//   numarası, JWT, bağlantı dizesi, özel anahtar ya da sır adlı alan
//   görülürse paket VERİLMEZ (500) ve reddedilen girişim denetim izine düşer.
//
// ⚠ GEREKÇE PAKETE KONMAZ: kullanıcının yazdığı serbest metin kişisel veri
//   taşıyabilir; yalnız işletmenin KENDİ denetim izine yazılır.
//
// ⚠ KANAL KAPISI (BK-17): TANILAMA_PAKETINDE_SIR=0 ve IC_KOD_SIZINTISI=0
//   Mehmet'in (güvenlik) kendi turunda doğrulanmadan /destek kanalı AÇILMAZ.
//   Bu yüzden `Destek:PaketKanaliAcik` yapılandırması VARSAYILAN KAPALIDIR;
//   kapalıyken paket ucu 503 döner, ekran düğmeyi kilitler ve sebebini yazar.
//
// ⚠ DESTEK DAKİKASI (P-28 bağı): her paket, P-28'in `destek_dakikalari`
//   tablosuna kaynak=TANILAMA ile bir satır açar. Harcanan dakika henüz
//   BİLİNMEZ; rakam uydurulmaz — satır PASİF ve 0 dakika doğar, ölçüme
//   SAYILMAZ. Operatör gerçek dakikayı P-28 sistem ucuyla (PATCH dakika +
//   aktif) girer. Böylece F-8'e ölçülmemiş rakam girmez (FAIL-CLOSED 1).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Yapılandırma anahtarları ve sabitler — dizge tek yerde.</summary>
public static class DestekAyarlari
{
    /// <summary>BK-17 kanal kapısı. Varsayılan KAPALI (fail-closed).</summary>
    public const string KanalAcikAnahtari = "Destek:PaketKanaliAcik";

    /// <summary>Gerekçe alt sınırı (UX-S10 / S-8 ile aynı eşik).</summary>
    public const int AsgariGerekce = 20;

    /// <summary>Gerekçe üst sınırı.</summary>
    public const int AzamiGerekce = 500;

    /// <summary>Kiracı başına saatlik paket tavanı (kötüye kullanım freni).</summary>
    public const int SaatlikTavan = 3;

    /// <summary>Geçmiş listesinin sınırı (UX-S29 — ekranda yazılır).</summary>
    public const int GecmisSiniri = 50;

    /// <summary>Paketteki denetim özeti satır tavanı (kırpılırsa not düşülür).</summary>
    public const int DenetimOzetiTavani = 200;

    /// <summary>Denetim izindeki varlık türü.</summary>
    public const string DenetimVarligi = "DestekPaketi";

    /// <summary>Destek dakikası satırının operatör alanı (otomatik kaynak).</summary>
    public const string KanalOperatoru = "tanılama kanalı (otomatik)";
}

// >>> PAKET-ICERIGI ── ALLOW-LIST: pakete giren alanların TAMAMI ─────────────

/// <summary>Maskeli tanılama paketi — allow-list kökü.</summary>
public sealed record TanilamaPaketi(
    int Bicim,
    string PaketKodu,
    DateTime OlusturmaUtc,
    string KiraciKisa,
    string IsteyenRol,
    string Konu,
    string? HataKodu,
    TanilamaSurumu Surum,
    TanilamaKapilari Kapilar,
    TanilamaSagligi Saglik,
    List<TanilamaDenetimSatiri> SonDenetim,
    bool DenetimKirpildi,
    List<string> Icermez);

/// <summary>Sürüm künyesi (SurumServisi ile aynı kaynak).</summary>
public sealed record TanilamaSurumu(
    string Surum, string DerlemeZamani, string Ortam,
    int MigrationSayisi, string? SonMigration, List<string> BekleyenMigrationlar);

/// <summary>Yalnız bool kapı durumları.</summary>
public sealed record TanilamaKapilari(bool Uretim, bool HttpsYonlendirme, bool Swagger);

/// <summary>Sağlık: yalnız sayılar ve bool.</summary>
public sealed record TanilamaSagligi(bool VeritabaniErisilebilir, int Son24SaatIstek, int Son24SaatSunucuHatasi);

/// <summary>Denetim özeti satırı — detay/IP/tarayıcı/tutar YOK.</summary>
public sealed record TanilamaDenetimSatiri(DateTime Tarih, string Eylem, string VarlikTuru, string? KullaniciKisa);

// <<< PAKET-ICERIGI ──────────────────────────────────────────────────────────

/// <summary>
/// Paketin kişisel veri / sır içermediğini METİN ÜZERİNDE denetler. Paket
/// allow-list'le üretilse de denetim ikinci, bağımsız kapıdır (fail-closed).
/// </summary>
public static class TanilamaDenetcisi
{
    private static readonly (string Tur, Regex Desen)[] Desenler =
    [
        ("TCKN", new Regex(@"(?<![0-9A-Za-z])[1-9][0-9]{10}(?![0-9A-Za-z])", RegexOptions.Compiled)),
        ("TELEFON", new Regex(@"(?<![0-9A-Za-z])(\+?90[ -]?)?0?5[0-9]{2}[ -]?[0-9]{3}[ -]?[0-9]{2}[ -]?[0-9]{2}(?![0-9A-Za-z])", RegexOptions.Compiled)),
        ("EPOSTA", new Regex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.Compiled)),
        ("IBAN", new Regex(@"TR[0-9]{2}[ ]?([0-9]{4}[ ]?){5}[0-9]{2}", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("KART", new Regex(@"(?<![0-9A-Za-z])[0-9]{15,19}(?![0-9A-Za-z])", RegexOptions.Compiled)),
        ("JWT", new Regex(@"eyJ[A-Za-z0-9_-]{5,}\.[A-Za-z0-9_-]{5,}", RegexOptions.Compiled)),
        ("BAGLANTI_DIZESI", new Regex(@"(password|pwd|user id|uid|server|data source)\s*=", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("OZEL_ANAHTAR", new Regex(@"-----BEGIN", RegexOptions.Compiled)),
        ("BEARER", new Regex(@"bearer\s+[A-Za-z0-9._-]{10,}", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("SIR_ALANI", new Regex(@"""(parola|sifre|şifre|password|secret|sir|sır|anahtar|token|jeton|apikey|api_key)""\s*:", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
    ];

    /// <summary>Bulunan ihlal TÜRLERİ (değer DEĞİL — değer günlüğe de yazılmaz).</summary>
    public static IReadOnlyList<string> Tara(string metin)
    {
        var bulgu = new List<string>();
        foreach (var (tur, desen) in Desenler)
            if (desen.IsMatch(metin)) bulgu.Add(tur);
        return bulgu;
    }
}

public sealed class DestekPaketiServisi(
    KuyumcuDbContext db,
    DenetimServisi denetim,
    SurumServisi surum,
    IConfiguration yapilandirma,
    IWebHostEnvironment ortam,
    IHttpContextAccessor erisim,
    ILogger<DestekPaketiServisi> logger)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    /// <summary>BK-17 kapısı: yapılandırma yoksa ya da okunamıyorsa KAPALI.</summary>
    public bool KanalAcik => yapilandirma.GetValue(DestekAyarlari.KanalAcikAnahtari, false);

    private string IstekKimligi =>
        erisim.HttpContext?.IstekKimligi() ?? throw new ApiException(500, "İstek bağlamı yok — paket kodu üretilemez.");

    /// <summary>Kimliğin son N karakteri — tam kimlik pakete girmez.</summary>
    private static string Kisalt(string? kimlik, int n) =>
        string.IsNullOrEmpty(kimlik) ? "-" : kimlik.Length <= n ? kimlik : kimlik[^n..];

    // ── PAKET ÜRET ───────────────────────────────────────────────────────────

    public async Task<(byte[] Veri, string DosyaAdi, string PaketKodu)> Olustur(
        AktifKullanici islem, DestekPaketiIstegi? dto)
    {
        if (!KanalAcik)
            throw new ApiException(503,
                "Destek paketi kanalı güvenlik doğrulaması tamamlanana dek kapalıdır. "
                + "Hata ekranındaki destek kodunu iletmeniz yeterlidir.", "Service Unavailable");

        // Servis içi ELLE doğrulama (DataAnnotations yalnız controller yolunda).
        var gerekce = (dto?.Gerekce ?? "").Trim();
        if (gerekce.Length < DestekAyarlari.AsgariGerekce)
            throw new BadRequestException(
                $"Gerekçe en az {DestekAyarlari.AsgariGerekce} karakter olmalı ({gerekce.Length}/{DestekAyarlari.AsgariGerekce}).");
        if (gerekce.Length > DestekAyarlari.AzamiGerekce)
            throw new BadRequestException($"Gerekçe en çok {DestekAyarlari.AzamiGerekce} karakter olabilir.");

        var konu = (dto?.Konu ?? "").Trim().ToUpperInvariant();
        if (!DestekKonulari.Gecerli(konu))
            throw new BadRequestException($"Geçersiz konu. Geçerli konular: {string.Join(", ", DestekKonulari.Hepsi)}");

        string? hataKodu = null;
        if (!string.IsNullOrWhiteSpace(dto?.HataKodu))
        {
            if (!HataKodu.GecerliMi(dto.HataKodu))
                throw new BadRequestException(
                    $"Destek kodu {HataKodu.Uzunluk} karakterli olmalı ve yalnız harf/rakam içermeli (hata ekranındaki kod).");
            hataKodu = HataKodu.Kisa(dto.HataKodu);
        }

        var now = DateTime.UtcNow;
        var birSaatOnce = now.AddHours(-1);
        var sonSaat = await db.DenetimKayitlari.AsNoTracking()
            .CountAsync(d => d.TenantId == islem.TenantId
                && d.VarlikTuru == DestekAyarlari.DenetimVarligi
                && d.Eylem == DenetimEylemi.Ekleme
                && d.Tarih >= birSaatOnce);
        if (sonSaat >= DestekAyarlari.SaatlikTavan)
            throw new ApiException(429,
                $"Bir saatte en çok {DestekAyarlari.SaatlikTavan} destek paketi oluşturulabilir. Biraz sonra yeniden deneyin.",
                "Too Many Requests");

        var paketKodu = IstekKimligi;
        var paket = await Derle(islem, paketKodu, konu, hataKodu, now);
        var metin = JsonSerializer.Serialize(paket, Json);

        // ── FAIL-CLOSED DENETİM ──────────────────────────────────────────────
        var bulgu = TanilamaDenetcisi.Tara(metin);
        if (bulgu.Count > 0)
        {
            // Değer DEĞİL yalnız tür yazılır — günlüğe de sızmasın.
            logger.LogWarning("Tanılama paketi denetimden geçmedi · hata kodu: {HataKodu} · tür: {Turler}",
                HataKodu.Kisa(paketKodu), string.Join(",", bulgu));
            await denetim.RedKaydet(islem, DestekAyarlari.DenetimVarligi, paketKodu,
                $"DESTEK_PAKETI: REDDEDILDI · denetim bulgusu: {string.Join(",", bulgu)}");
            throw new ApiException(500,
                "Tanılama paketi güvenlik denetiminden geçmedi; paket üretilmedi. Destek kodunu iletin.");
        }

        var veri = Encoding.UTF8.GetBytes(metin);

        // ── DESTEK DAKİKASI (P-28): pasif + 0 dk — rakam uydurulmaz ──────────
        db.DestekDakikalari.Add(new DestekDakikasi
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = islem.TenantId,
            Tarih = now.Date,
            Dakika = 0,
            Kanal = DestekKanallari.Yazili,
            Konu = konu,
            Aciklama = $"Tanılama paketi {HataKodu.Kisa(paketKodu)} — dakika operatör tarafından girilecek",
            Kaynak = DestekKaynaklari.Tanilama,
            Aktif = false,
            OperatorAdi = DestekAyarlari.KanalOperatoru,
            Olusturuldu = now,
            Guncellendi = now,
        });

        // Denetim yazılmadan paket VERİLMEZ: ikisi aynı SaveChanges'te.
        denetim.YazmaKaydet(islem, DestekAyarlari.DenetimVarligi, paketKodu,
            $"DESTEK_PAKETI: OLUSTURULDU · konu: {konu} · boyut: {veri.Length} bayt"
            + (hataKodu is null ? "" : $" · hata kodu: {hataKodu}")
            + $" · gerekçe: {gerekce}", hassas: false);
        await db.SaveChangesAsync();

        logger.LogInformation("Tanılama paketi üretildi · paket kodu: {HataKodu} · boyut: {Bayt}",
            HataKodu.Kisa(paketKodu), veri.Length);

        return (veri, $"destek-paketi-{HataKodu.Kisa(paketKodu)}.json", paketKodu);
    }

    // >>> PAKET-ICERIGI ── içerik üreten TEK bölge (denetim.sh bunu tarar) ──
    private async Task<TanilamaPaketi> Derle(
        AktifKullanici islem, string paketKodu, string konu, string? hataKodu, DateTime now)
    {
        var kunye = await surum.Kunye();
        var bekleyen = (await db.Database.GetPendingMigrationsAsync()).ToList();

        var gunOnce = now.AddHours(-24);
        var olcum = await db.TelemetriOlcumleri.AsNoTracking()
            .Where(t => t.TenantId == islem.TenantId && t.OlcumSaati >= gunOnce)
            .GroupBy(t => 1)
            .Select(g => new { Istek = g.Sum(x => x.IstekSayisi), Hata = g.Sum(x => x.HataSayisi) })
            .FirstOrDefaultAsync();

        var son = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == islem.TenantId)
            .OrderByDescending(d => d.Tarih)
            .Take(DestekAyarlari.DenetimOzetiTavani + 1)
            .Select(d => new { d.Tarih, d.Eylem, d.VarlikTuru, d.KullaniciId })
            .ToListAsync();
        var kirpildi = son.Count > DestekAyarlari.DenetimOzetiTavani;

        return new TanilamaPaketi(
            Bicim: 1,
            PaketKodu: paketKodu,
            OlusturmaUtc: now,
            KiraciKisa: Kisalt(islem.TenantId, 6),
            IsteyenRol: islem.Rol,
            Konu: konu,
            HataKodu: hataKodu,
            Surum: new TanilamaSurumu(
                kunye.Surum, kunye.DerlemeZamani, kunye.Ortam,
                kunye.MigrationSayisi, kunye.SonMigration, bekleyen),
            Kapilar: new TanilamaKapilari(
                Uretim: ortam.IsProduction(),
                HttpsYonlendirme: !ortam.IsDevelopment(),
                Swagger: ortam.IsDevelopment()),
            Saglik: new TanilamaSagligi(true, olcum?.Istek ?? 0, olcum?.Hata ?? 0),
            SonDenetim: son.Take(DestekAyarlari.DenetimOzetiTavani)
                .Select(d => new TanilamaDenetimSatiri(d.Tarih, d.Eylem, d.VarlikTuru, Kisalt(d.KullaniciId, 6)))
                .ToList(),
            DenetimKirpildi: kirpildi,
            Icermez:
            [
                "müşteri/cari adı, TCKN, telefon, adres, e-posta",
                "tutar, bakiye, has gram, fiyat",
                "kullanıcı adı, parola, oturum jetonu, bağlantı dizesi",
                "denetim kaydı ayrıntısı, IP adresi, tarayıcı bilgisi",
                "kullanıcının yazdığı gerekçe metni",
            ]);
    }
    // <<< PAKET-ICERIGI

    // ── GEÇMİŞ ───────────────────────────────────────────────────────────────

    public async Task<DestekPaketiGecmisi> Gecmis(AktifKullanici islem)
    {
        var satirlar = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == islem.TenantId
                && d.VarlikTuru == DestekAyarlari.DenetimVarligi
                && d.Eylem == DenetimEylemi.Ekleme)
            .OrderByDescending(d => d.Tarih)
            .Take(DestekAyarlari.GecmisSiniri + 1)
            .Select(d => new { d.Tarih, d.VarlikId, d.Detay })
            .ToListAsync();

        var fazla = satirlar.Count > DestekAyarlari.GecmisSiniri;
        var kayitlar = satirlar.Take(DestekAyarlari.GecmisSiniri)
            // Denetim tarihi UTC yazılır ama sürücü Kind=Unspecified okur; istemci
            // ToLocalTime() çağırdığında saat kaymasın diye UTC damgası konur
            // (canlı doğrulamada "…T18:17:48" Z'siz döndüğü görüldü).
            .Select(s => new DestekPaketiGecmisSatiri(
                DateTime.SpecifyKind(s.Tarih, DateTimeKind.Utc),
                HataKodu.Kisa(s.VarlikId) ?? "-", KonuAyikla(s.Detay)))
            .ToList();

        await denetim.ListeOkumaKaydet(islem, DestekAyarlari.DenetimVarligi, kayitlar.Count);

        return new DestekPaketiGecmisi(
            KanalAcik, DestekAyarlari.GecmisSiniri, fazla, DestekAyarlari.AsgariGerekce,
            [.. DestekKonulari.Hepsi], kayitlar);
    }

    /// <summary>Denetim detayından konu kodu (yalnız beyaz listedeki değer döner).</summary>
    private static string KonuAyikla(string? detay)
    {
        if (string.IsNullOrEmpty(detay)) return DestekKonulari.Diger;
        var m = Regex.Match(detay, @"konu: ([A-Z]+)");
        return m.Success && DestekKonulari.Gecerli(m.Groups[1].Value) ? m.Groups[1].Value : DestekKonulari.Diger;
    }

    // ── İSTEMCİ HATASI ───────────────────────────────────────────────────────

    /// <summary>
    /// Ekranda (tarayıcıda) doğan beklenmeyen hatayı sunucu günlüğüne bağlar.
    /// YALNIZ hata türü adı kabul edilir ve beyaz karakter kümesine süzülür;
    /// hata mesajı, sayfa verisi, kullanıcı girdisi ALINMAZ.
    /// </summary>
    public IstemciHatasiYaniti IstemciHatasi(IstemciHatasiBildirimi? dto)
    {
        var tur = TurSuz(dto?.Tur);
        var kimlik = IstekKimligi;
        var kod = HataKodu.Kisa(kimlik)!;
        logger.LogError("İstemci ekran hatası · hata kodu: {HataKodu} · tür: {Tur}", kod, tur);
        return new IstemciHatasiYaniti(kimlik, kod);
    }

    /// <summary>Tür adı: yalnız [A-Za-z0-9._], en çok 80 karakter; değilse "Bilinmeyen".</summary>
    public static string TurSuz(string? tur)
    {
        if (string.IsNullOrWhiteSpace(tur)) return "Bilinmeyen";
        var t = tur.Trim();
        return t.Length <= 80 && Regex.IsMatch(t, @"^[A-Za-z0-9_.]+$") ? t : "Bilinmeyen";
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

/// <summary>
/// <c>/api/v1/destek</c> — maskeli tanılama paketi (SAHIP) ve istemci hata
/// kodu bildirimi (her rol). Paket kaydı DEĞİŞMEZDİR: PATCH/DELETE ucu yoktur
/// (DuzeltmePolitikasi: SALT_OKUNUR · DENETIM).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/destek")]
public sealed class DestekController(DestekPaketiServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/destek/paket — maskeli paketi üretir, dosya döner.</summary>
    [HttpPost("paket")]
    [RolGerekli("SAHIP")]
    public async Task<IActionResult> Paket([FromBody] DestekPaketiIstegi? dto)
    {
        var (veri, ad, _) = await servis.Olustur(Islem, dto);
        return File(veri, "application/json", ad);
    }

    /// <summary>GET /api/v1/destek/paket/gecmis — son 50 paket.</summary>
    [HttpGet("paket/gecmis")]
    [RolGerekli("SAHIP")]
    public Task<DestekPaketiGecmisi> Gecmis() => servis.Gecmis(Islem);

    /// <summary>POST /api/v1/destek/istemci-hatasi — ekran hatasına sunucu kodu.</summary>
    [HttpPost("istemci-hatasi")]
    // Oturumu olan HER rol bildirebilir (ekran hatası her rolde doğar);
    // beyan açık yazılır ki uç "unutulmuş" sayılmasın (P-09 OZNITELIKSIZ_UC).
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public IstemciHatasiYaniti IstemciHatasi([FromBody] IstemciHatasiBildirimi? dto) =>
        servis.IstemciHatasi(dto);
}
