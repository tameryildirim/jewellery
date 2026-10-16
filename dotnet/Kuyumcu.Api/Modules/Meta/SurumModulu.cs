using System.Reflection;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Kuyumcu.Api.Modules.Meta;

// ═════════════════════════════════════════════════════════════════════════════
// SÜRÜM / DERLEME / MIGRATION KÜNYESİ — paket T-4 Faz A (T-4a).
//
// HEDEF: destek "hangi sürüm, hangi şema" sorusunu tahminle değil, ÜRÜNÜN
// KENDİ BEYANIYLA cevaplasın.
//
// ⚠ SIZDIRMAZ (Mehmet kapısı): bu uç hiçbir sır, bağlantı dizesi, dosya yolu,
//   makine adı, IP ya da ortam değişkeni DÖNDÜRMEZ. Ortam yalnız iki dizgeden
//   biridir: "Production" | "Development". Özel ortam adı (örn. "Testing",
//   "Staging-3") dışarı çıkmaz — dağıtım topolojisi de bir bilgidir.
//
// ⚠ MIGRATION SAYIMI DOSYADAN OKUNMAZ (Bertan koşulu): Data/Migrations altında
//   yarıda kesilmiş 0 baytlık dosyalar var (denetim.sh §12 beyaz listesi).
//   Sayım YALNIZ __EFMigrationsHistory (GetAppliedMigrationsAsync) ve
//   assembly'deki migration kümesinden (GetPendingMigrationsAsync) gelir.
//
// ⚠ FAIL-CLOSED (Anayasa md.1): sürüm ya da derleme damgası okunamıyorsa
//   "bilinmiyor" DÖNMEZ — 500 döner ve günlüğe yazar. Yanlış künye, künyesizden
//   daha tehlikelidir (destek yanlış sürüme bakar).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Sürüm künyesi — kiracıdan bağımsız, süreç genelinde aynıdır.</summary>
public sealed record SurumKunyesi(
    string Surum,
    string DerlemeZamani,
    string DerlemeZamaniKaynagi,
    string Ortam,
    int MigrationSayisi,
    string? SonMigration,
    int BekleyenMigrationSayisi);

public sealed class SurumServisi(
    KuyumcuDbContext db,
    IMemoryCache onbellek,
    IWebHostEnvironment ortam,
    DenetimServisi denetim)
{
    /// <summary>Künye önbellek ömrü — migration kümesi sık değişmez.</summary>
    public static readonly TimeSpan OnbellekOmru = TimeSpan.FromSeconds(60);

    // ⚠ ÖNBELLEK ANAHTARI KİRACIYA GÖRE AYRIŞMAZ ve AYRIŞMAMALIDIR (Mehmet
    //   sorusu): künyede kiracı verisi YOKTUR; şema tüm kiracılar için tektir.
    //   Kiracı başına ayrı anahtar tutmak aynı gerçeği N kez saklamak olurdu.
    private const string OnbellekAnahtari = "meta:surum";

    /// <summary>Sürüm/derleme/migration künyesi (60 sn önbellekli).</summary>
    public async Task<SurumKunyesi> Kunye()
    {
        if (onbellek.TryGetValue(OnbellekAnahtari, out SurumKunyesi? onbellekli) && onbellekli is not null)
            return onbellekli;

        var uygulanmis = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        var bekleyen = (await db.Database.GetPendingMigrationsAsync()).ToList();

        var derleme = DerlemeDamgasi();
        var kunye = new SurumKunyesi(
            Surum: SurumOku(),
            DerlemeZamani: derleme.Zaman,
            DerlemeZamaniKaynagi: derleme.Kaynak,
            Ortam: OrtamAdi(),
            MigrationSayisi: uygulanmis.Count,
            SonMigration: uygulanmis.LastOrDefault(),
            BekleyenMigrationSayisi: bekleyen.Count);

        onbellek.Set(OnbellekAnahtari, kunye, OnbellekOmru);
        return kunye;
    }

    /// <summary>SAHIP ayrıntısı: uygulanmış + bekleyen migration listeleri.</summary>
    public async Task<object> Ayrinti(AktifKullanici islem)
    {
        var kunye = await Kunye();
        var uygulanmis = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        var bekleyen = (await db.Database.GetPendingMigrationsAsync()).ToList();

        await denetim.OkumaKaydet(islem, "Surum", null, "sürüm ayrıntısı");

        return new
        {
            kunye.Surum,
            kunye.DerlemeZamani,
            kunye.DerlemeZamaniKaynagi,
            kunye.Ortam,
            migrationSayisi = uygulanmis.Count,
            sonMigration = uygulanmis.LastOrDefault(),
            uygulanmis,
            bekleyen,
            // Bekleyen varsa SESSİZ GEÇİLMEZ: ekran kırmızı rozet basar.
            uyari = bekleyen.Count == 0
                ? null
                : Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir($"{bekleyen.Count} migration henüz uygulanmadı — şema güncel değil."),
            // ⚠ YALNIZ BOOL KAPI DURUMLARI (mühür koşulu): ortam değişkeni adı
            //   ya da değeri, sır varlığı, yol bilgisi BURAYA GİRMEZ.
            kapilar = new
            {
                uretim = ortam.IsProduction(),
                httpsYonlendirme = !ortam.IsDevelopment(),
                hsts = !ortam.IsDevelopment(),
                swagger = ortam.IsDevelopment(),
            },
            onbellekSaniye = (int)OnbellekOmru.TotalSeconds,
        };
    }

    // ── Assembly okumaları ───────────────────────────────────────────────────

    // ⚠ GetEntryAssembly DEĞİL: test koşumunda giriş assembly'si test
    //   sürücüsüdür (testhost) — o zaman uç, ÜRÜNÜN sürümü yerine test
    //   altyapısının sürümünü beyan ederdi. Künyenin sahibi bu assembly'dir.
    private static Assembly Kaynak => typeof(SurumServisi).Assembly;

    private static string SurumOku()
    {
        var bilgi = Kaynak.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(bilgi))
            throw new ApiException(500, "Sürüm damgası okunamadı — yayın hattı sürümü yazmamış.");
        // "1.0.0+9a1b2c3" biçiminde kaynak damgası olabilir; kaynak karması
        // depo iç bilgisidir, künyeye girmez.
        var artı = bilgi.IndexOf('+');
        return artı > 0 ? bilgi[..artı] : bilgi;
    }

    /// <summary>
    /// Derleme zamanı. Birinci kaynak: yayın hattının gömdüğü
    /// <c>AssemblyMetadata("DerlemeZamani")</c> damgası. Damga yoksa (geliştirme
    /// derlemesi) ikinci kaynak assembly DOSYASININ yazılma anıdır — bu da
    /// GERÇEK bir ölçümdür, sabit değildir. Hangi kaynaktan geldiği yanıtta
    /// AÇIKÇA yazar; "bilinmiyor" ya da sessiz varsayım yoktur.
    /// </summary>
    private static (string Zaman, string Kaynak) DerlemeDamgasi()
    {
        var damga = Kaynak.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "DerlemeZamani")?.Value;

        if (!string.IsNullOrWhiteSpace(damga)
            && DateTime.TryParse(damga, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal
                | System.Globalization.DateTimeStyles.AssumeUniversal, out var damgaZaman))
            return (damgaZaman.ToString("O"), "damga");

        var yol = Kaynak.Location;
        if (!string.IsNullOrEmpty(yol) && File.Exists(yol))
            // YALNIZ zaman döner; dosya YOLU gövdeye YAZILMAZ.
            return (File.GetLastWriteTimeUtc(yol).ToString("O"), "dosya");

        throw new ApiException(500, "Derleme damgası okunamadı — künye eksik döndürülemez.");
    }

    /// <summary>
    /// Ortam: yalnız iki dizge. Üretim dışındaki her ortam "Development"
    /// olarak beyan edilir — özel ortam adları (dağıtım topolojisi) sızmasın.
    /// </summary>
    private string OrtamAdi() => ortam.IsProduction() ? "Production" : "Development";
}

// ── Controller ───────────────────────────────────────────────────────────────

/// <summary>
/// <c>/api/v1/meta/surum</c> — sürüm künyesi.
/// <para>
/// ⚠ ANONİM DEĞİLDİR (kurul kararı): yük dengeleyici/izleme için
/// <c>/saglik</c> ve <c>/saglik/hazir</c> vardır; künye kimliği doğrulanmış
/// kullanıcıya açılır.
/// </para>
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/meta")]
public sealed class SurumController(SurumServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/meta/surum — her rol okuyabilir (kiracı verisi yok).</summary>
    [HttpGet("surum")]
    [Kuyumcu.Api.Auth.HerkeseAcik("Sürüm künyesi — alt bilgi çubuğunda herkese gösterilir, kiracı verisi yok")]
    public async Task<object> Surum() => await servis.Kunye();

    /// <summary>GET /api/v1/meta/surum/ayrinti — migration listeleri (SAHIP).</summary>
    [HttpGet("surum/ayrinti")]
    [RolGerekli("SAHIP")]
    public Task<object> Ayrinti() => servis.Ayrinti(Islem);
}
