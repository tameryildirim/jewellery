using System.Collections.Concurrent;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Sistem;

// ═════════════════════════════════════════════════════════════════════════════
// KİRACI DURUM KAPISI — paket F-2-min Faz A.
//
// ⚠ YENİ MEKANİZMA (Bertan'ın koşulu, ADR notu): kod tabanında bundan önce
//   GLOBAL bir MVC yetkilendirme filtresi YOKTU. Mevcut iki "kapı" deseni
//   farklıydı:
//     • GunKapanisiKapisi   → public static class, servisten çağrılan yardımcı
//     • TevsikKapisi        → AddScoped servis kapısı (Program.cs)
//   Bu kapı üçüncü bir desendir ve BİLİNÇLİDİR: kural uca özgü değil, kiracıya
//   özgüdür; tek tek çağrı noktalarına serpiştirilirse biri unutulur.
//
// ⚠ KAPSAM SINIRI (belgelenmesi koşuldu): filtre YALNIZ MVC controller
//   uçlarında (app.MapControllers) çalışır. Kapsam DIŞINDA kalan yollar:
//     • /saglik, /saglik/hazir      → minimal API (GET, yazma yok)
//     • /hubs/kur                   → SignalR hub (kur push, yazma yok)
//     • UseStaticFiles + MapFallbackToFile("index.html") → statik WASM varlıkları
//     • MapFallback("/api/{**kalan}") → eşleşmeyen API yolu (404 üreticisi)
//   Hiçbiri kiracı verisine YAZMAZ; bu yüzden kapsam dışı kalmaları kuralı
//   delmez. Yeni bir minimal API YAZMA ucu açılırsa bu kapı onu GÖRMEZ.
//
// MUAFİYET (veri rehin alınmaz — KVKK md.11-12 · VUK 253/256):
//   • /api/v1/auth        → salt-okunur kiracı giriş yapabilsin
//   • /api/v1/sistem      → kapıyı açan yönetici ucu kendini kilitlemesin
//   • /api/v1/disa-aktarim→ veri her koşulda indirilebilir
//   Eşleşme SEGMENT SINIRIYLA yapılır: '/api/v1' gibi geniş bir önek
//   yazılamaz (test: Muafiyet_listesi_GENIS_onek_ICERMEZ).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Kiracı durumunun kısa ömürlü bellek-içi önbelleği (singleton).
/// <para>
/// Her mutasyon isteğinde tenants okunması sıcak yolda ölçülebilir gecikme
/// getirirdi. Önbellek 30 saniyeliktir ve süre uzatma/durum değişiminde
/// <see cref="Bosalt"/> ile AÇIKÇA geçersiz kılınır — uzatma gecikmeli
/// etki etmesin (Aşkın koşulu).
/// </para>
/// </summary>
public sealed class KiraciDurumOnbellegi
{
    public static readonly TimeSpan Omur = TimeSpan.FromSeconds(30);

    private readonly ConcurrentDictionary<string, (KiraciDurumu Durum, DateTime Yazildi)> kayitlar = new();

    public KiraciDurumu? Oku(string tenantId, DateTime now) =>
        kayitlar.TryGetValue(tenantId, out var k) && now - k.Yazildi < Omur ? k.Durum : null;

    public void Yaz(string tenantId, KiraciDurumu durum, DateTime now) =>
        kayitlar[tenantId] = (durum, now);

    /// <summary>Tek kiracının kaydını düşürür (süre uzatma / durum değişimi).</summary>
    public void Bosalt(string tenantId) => kayitlar.TryRemove(tenantId, out _);
}

/// <param name="Durum">tenants.durum ham değeri (bilinmeyen de olabilir).</param>
/// <param name="DenemeBitis">Deneme bitiş anı (UTC) — NULL = süre kısıtı yok.</param>
/// <param name="AbonelikBitis">
/// PAKET P-27 (F-1): aktif abonelik döneminin bitişi (UTC) — NULL = süresiz ya
/// da abonelik satırı yok. İKİNCİ bir kapı deseni AÇILMADI: süresi dolan
/// abonelik de bu kapıdan, aynı 403 ile durur.
/// <para>⚠ Parametre SONA ve VARSAYILANLI eklendi: mevcut iki argümanlı
/// çağrılar (AdminModulu, SistemModulu) DEĞİŞMEDEN derlenir.</para>
/// </param>
public readonly record struct KiraciDurumu(string Durum, DateTime? DenemeBitis, DateTime? AbonelikBitis = null)
{
    /// <summary>
    /// Bu kiracı YAZABİLİR mi? BEYAZ LİSTE — bilinmeyen değer KAPATIR.
    /// <list type="bullet">
    /// <item>AKTIF → yazar (deneme_bitis'e BAKILMAZ; süre kısıtı yalnız denemededir)</item>
    /// <item>DENEME → deneme_bitis geçmemişse yazar</item>
    /// <item>SALT_OKUNUR → yazamaz</item>
    /// <item>başka her şey (boş, 'ASKIDA', yazım hatası) → yazamaz</item>
    /// </list>
    /// <para>
    /// P-27 · FAIL-CLOSED: yukarıdaki kapı AÇIK olsa bile abonelik dönemi
    /// GEÇMİŞSE yazma durur. Okuma ve dışa aktarım etkilenmez — veri rehin
    /// alınmaz (KVKK md.11-12 · VUK 253/256).
    /// </para>
    /// </summary>
    public bool YazabilirMi(DateTime now) => TemelYazabilirMi(now) && !AbonelikSuresiDoldu(now);

    /// <summary>Abonelikten BAĞIMSIZ durum kapısı (F-2-min davranışı, değişmedi).</summary>
    private bool TemelYazabilirMi(DateTime now) => Durum switch
    {
        KiraciDurumlari.Aktif => true,
        KiraciDurumlari.Deneme => DenemeBitis is null || DenemeBitis.Value >= now,
        _ => false,
    };

    /// <summary>Abonelik dönemi geçti mi? NULL bitiş = süresiz (kısıt yok).</summary>
    public bool AbonelikSuresiDoldu(DateTime now) =>
        AbonelikBitis is not null && AbonelikBitis.Value < now;

    public string RedSebebi(DateTime now)
    {
        // P-27: abonelik bitişi kendi gerekçesini söyler. "Deneme süreniz doldu"
        // demek ÜCRETLİ bir kiracı için yanlış olurdu (UX-S31 · Anayasa md.1).
        if (TemelYazabilirMi(now) && AbonelikSuresiDoldu(now))
            return "Abonelik döneminiz sona erdi: kayıtlarınız duruyor, görüntüleyebilir "
                + "ve dışa aktarabilirsiniz; yeni kayıt yapabilmek için aboneliğin "
                + "yenilenmesi gerekir. Yöneticinize başvurun.";
        return Durum switch
        {
        KiraciDurumlari.SaltOkunur =>
            "İşletmeniz SALT OKUNUR durumdadır: kayıtlarınızı görüntüleyebilir ve dışa "
            + "aktarabilirsiniz, yeni kayıt yapamazsınız. Süre uzatımı için yöneticinize başvurun.",
        KiraciDurumlari.Deneme =>
            "Deneme süreniz doldu: kayıtlarınız duruyor, görüntüleyebilir ve dışa "
            + "aktarabilirsiniz; yeni kayıt yapabilmek için süre uzatımı gerekir.",
            _ =>
                "İşletmenizin durumu tanınmadığı için yazma işlemleri kapalıdır "
                + "(fail-closed). Yöneticinize başvurun.",
        };
    }
}

/// <summary>
/// Global MVC yetkilendirme filtresi: kimliği doğrulanmış MUTASYON isteklerinde
/// kiracı durumunu okur; yazamıyorsa 403 döner. Okuma (GET/HEAD/OPTIONS) ve
/// muaf yollar ETKİLENMEZ.
/// </summary>
public sealed class KiraciDurumKapisi(KiraciDurumOnbellegi onbellek) : IAsyncAuthorizationFilter
{
    /// <summary>
    /// Kapıdan MUAF yol önekleri. Eşleşme SEGMENT SINIRIYLA yapılır
    /// (tam eşit ya da ardından '/'), substring ile DEĞİL.
    /// </summary>
    public static readonly string[] MuafYollar =
    [
        "/api/v1/auth",
        "/api/v1/sistem",
        "/api/v1/disa-aktarim",
    ];

    public static bool MuafMi(string? yol)
    {
        if (string.IsNullOrEmpty(yol)) return false;
        var n = yol.TrimEnd('/').ToLowerInvariant();
        foreach (var m in MuafYollar)
            if (n == m || n.StartsWith(m + "/", StringComparison.Ordinal)) return true;
        return false;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext baglam)
    {
        var istek = baglam.HttpContext.Request;
        if (!SistemAnahtariKapisi.MutasyonMu(istek.Method)) return;
        if (MuafMi(istek.Path.Value)) return;

        // Kimliği doğrulanmamış istek: kiracı bilinmez, zaten 401'e gider.
        var kullanici = baglam.HttpContext.User;
        if (kullanici?.Identity?.IsAuthenticated != true) return;
        var tenantId = kullanici.FindFirst("tenantId")?.Value;
        if (string.IsNullOrEmpty(tenantId)) return;

        var now = DateTime.UtcNow;
        var durum = onbellek.Oku(tenantId, now);
        if (durum is null)
        {
            // İSTEK KAPSAMI — SistemKapsami'ne gerek YOK: Tenant'ın kendi
            // sorgu filtresi (t.Id == kiraciId) zaten bu kiracıyı seçer.
            var db = baglam.HttpContext.RequestServices.GetRequiredService<KuyumcuDbContext>();
            var satir = await db.Tenants.AsNoTracking()
                .Where(t => t.Id == tenantId)
                .Select(t => new { t.Durum, t.DenemeBitis })
                .FirstOrDefaultAsync();

            // PAKET P-27 (F-1): aktif abonelik döneminin bitişi. Ayrı bir kapı
            // değil, AYNI kapının ikinci zaman kaynağıdır. Sorgu yalnız önbellek
            // ISKALADIĞINDA koşar (30 sn) ve (tenant_id, durum, baslangic)
            // indeksinden karşılanır.
            DateTime? abonelikBitis = null;
            if (satir is not null)
                abonelikBitis = await db.Set<Modules.Abonelik.KiraciAbonelik>().AsNoTracking()
                    .Where(a => a.TenantId == tenantId
                             && a.Durum == Modules.Abonelik.AbonelikDurumlari.Aktif)
                    .OrderByDescending(a => a.Baslangic)
                    .Select(a => a.Bitis)
                    .FirstOrDefaultAsync();

            // Kiracı satırı yoksa (silinmiş/bozuk jeton) fail-closed: boş durum yazamaz.
            durum = satir is null
                ? new KiraciDurumu("", null)
                : new KiraciDurumu(satir.Durum ?? "", satir.DenemeBitis, abonelikBitis);
            onbellek.Yaz(tenantId, durum.Value, now);
        }

        if (durum.Value.YazabilirMi(now)) return;

        baglam.Result = new ObjectResult(new
        {
            message = durum.Value.RedSebebi(now),
            error = "Forbidden",
            statusCode = StatusCodes.Status403Forbidden,
            kiraciDurumu = durum.Value.Durum,
        })
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };
    }
}
