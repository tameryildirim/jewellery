using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Kuyumcu.Api.Modules.Yetki;

// ═════════════════════════════════════════════════════════════════════════════
// Yetki modülü — rol bazlı modül (ekran) erişimi. Tenant başına yapılandırılır.
//   • Bir rol_yetkileri satırı = o rolün o modüle erişimi var.
//   • SAHIP her zaman tam yetkilidir (tabloda tutulmaz).
//   • İlk erişimde varsayılanlar tohumlanır (mevcut davranışı korur).
// ═════════════════════════════════════════════════════════════════════════════

public static class Moduller
{
    /// <summary>
    /// Yetkilendirilebilir ekranlar (menüdeki modüller).
    ///
    /// ⚠ PAKET P-09 (Y-39): BU LİSTE ARTIK ELLE YAZILMAZ. Tek kaynak
    /// <see cref="Kuyumcu.Core.Erisim.RotaModulleri.Harita"/>'dır; liste oradan
    /// TÜRETİLİR. Yeni bir ekran (@page) açan, haritaya bir satır yazar ve modül
    /// anahtarı matriste kendiliğinden belirir. Eskiden iki liste vardı ve
    /// Emanet, Kur, Vezne, POS, Sayım, Uyum, Yedekler, Veri aktarımı, Denetim İzi
    /// ve Fiyat Listeleri ekranlarının matriste karşılığı YOKTU.
    ///
    /// ⚠ Eşleşme <c>RotaYetkiMatrisiTestleri</c> ile İKİ YÖNLÜ kilitlenir.
    /// </summary>
    public static readonly string[] Tumu = Kuyumcu.Core.Erisim.RotaModulleri.Modulleri;

    /// <summary>
    /// MODÜL-BAŞI TOHUMLAMA'DAN ÖNCE var olan anahtarlar (paket 6 Faz A).
    ///
    /// ⚠ NEDEN SABİT LİSTE: eski kiracılarda tek bir "__yapilandirildi__"
    /// markeri vardı; hangi modüllerin tohumlandığı veride yazmıyor. Bu liste,
    /// o tek markerin O GÜN kapsadığı anahtar kümesidir. Eski kiracıda bu
    /// anahtarlar için satır YENİDEN EKLENMEZ — sahibin bilerek kaldırdığı bir
    /// yetki geri gelmemeli. Yalnız marker geriye doldurulur.
    /// ⚠ BU LİSTE BÜYÜMEZ. Yeni her modül anahtarı kendi markeriyle doğar.
    /// </summary>
    public static readonly string[] TohumlamaOncesi =
        ["cari", "urun", "islem", "kasa", "banka", "fatura", "rapor", "kullanici"];

    /// <summary>
    /// ANAHTAR GÖÇÜ — yeni anahtar → devraldığı ESKİ anahtar(lar).
    ///
    /// ⚠ NEDEN VAR (P-09 hasım turu, KRİTİK bulgu): P-09 bazı ekranların
    /// sunucu kapısını YENİDEN ADLANDIRDI (ödünç anahtar → kendi anahtarı):
    ///   • <c>pos</c>            ← <c>kasa</c>  (<c>PosController</c>)
    ///   • <c>vezne</c>          ← <c>islem</c> (<c>VezneController</c> fiş uçları;
    ///                             PARA ucu <c>VezneTahsilController</c> "kasa"da KALDI)
    ///   • <c>sayim</c>          ← <c>urun</c>  (<c>SayimController</c>)
    ///   • <c>fiyat-listesi</c>  ← <c>urun</c>  (<c>FiyatListesiController</c>)
    /// Bu anahtarlar eski kiracıda marker taşımaz; koşulsuz tohumlansalardı
    /// <see cref="Varsayilan"/> satırları KOŞULSUZ yazılır ve SAHİBİN BİLEREK
    /// KALDIRDIĞI erişim (ör. TEZGÂHTAR'dan alınan "kasa") yeni ad altında
    /// SESSİZCE GERİ AÇILIRDI — üstelik para el değiştiren uçlarda (POS, vezne).
    /// Bu yüzden eski kiracıda göçmen bir anahtar, ilgili rol devraldığı ESKİ
    /// anahtarın TAMAMINA halen sahipse yazılır; değilse SATIR YAZILMAZ.
    ///
    /// ⚠ BURAYA YALNIZ "YENİDEN ADLANDIRMA" YAZILIR. Gerçekten YENİ kapılar
    /// (<c>emanet</c>, <c>kur</c>: P-09 öncesi controller'da HİÇ
    /// <c>ModulYetkisi</c> yoktu — uç tüm kimlikli kullanıcılara açıktı) bu
    /// haritaya GİRMEZ: onları koşullu yapmak erişimi GENİŞLETMEZ, tam tersine
    /// var olan erişimi kaldırırdı. Menü anahtarları (<c>uyum</c>, <c>yedek</c>,
    /// <c>veri-aktarim</c>, <c>denetim-izi</c>) de girmez; uçları
    /// <c>[RolGerekli]</c> ile ayrıca kapalıdır.
    /// ⚠ Harita <c>P09AnahtarGocuTestleri</c> ile kilitlidir.
    /// </summary>
    public static readonly Dictionary<string, string[]> AnahtarGocu = new()
    {
        ["pos"]           = ["kasa"],
        ["vezne"]         = ["islem"],
        ["sayim"]         = ["urun"],
        ["fiyat-listesi"] = ["urun"],
    };

    /// <summary>Yapılandırılabilir roller — SAHIP her zaman tam yetkili olduğundan listede yok.</summary>
    public static readonly string[] Roller = ["MUDUR", "MUHASEBE", "TEZGAHTAR"];

    /// <summary>Varsayılan yetkiler — mevcut davranışı korur (rapor: MUHASEBE+, kullanıcı: MUDUR+).</summary>
    /// <remarks>
    /// ⚠ "bildirim" (paket 6) TEZGÂHTAR'a VARSAYILAN OLARAK AÇILMAZ: bildirim
    /// listesi MASAK ve saklama süresi kaynaklı satırlar taşır; bunlar zaten
    /// gorunur_roller ile süzülür, ama ekranın kendisi de kişisel veri ekranları
    /// (uyum, gider pusulası) ile aynı hizada tutulur. Sahibi isterse yetki
    /// ekranından açabilir — uçtaki [RolGerekli] tezgâhtarı kabul eder.
    /// </remarks>
    public static readonly Dictionary<string, string[]> Varsayilan = new()
    {
        // "kurulum" MÜDÜR'e açıktır ama yalnız GÖRÜNTÜLEME anlamına gelir:
        // açılış yazma uçları metot düzeyinde SAHİP ister (403).
        // ⚠ P-09 (Y-39) yeni anahtarlar: emanet · kur · vezne · pos · sayim ·
        //   fiyat-listesi · uyum. Varsayılanlar BUGÜNKÜ erişimi birebir korur —
        //   POS "kasa", Vezne "islem"/"kasa", Sayım ve Fiyat Listesi "urun"
        //   anahtarını kullanıyordu; o anahtarlara sahip her rol yenisini de alır.
        //   GENİŞLEME YOK: "uyum" TEZGÂHTAR'a KAPALI (kişisel veri rejimi),
        //   "yedek" · "veri-aktarim" · "denetim-izi" hiçbir varsayılanda YOK
        //   (uçları zaten [RolGerekli("SAHIP")]; SAHİP matristen bağımsız geçer).
        ["MUDUR"]     = ["cari", "urun", "islem", "kasa", "banka", "fatura", "rapor", "kullanici", "bildirim", "siparis", "kurulum", "evrak",
                         "emanet", "kur", "vezne", "pos", "sayim", "fiyat-listesi", "uyum"],
        ["MUHASEBE"]  = ["cari", "urun", "islem", "kasa", "banka", "fatura", "rapor", "bildirim", "siparis", "evrak",
                         "emanet", "kur", "vezne", "pos", "sayim", "fiyat-listesi", "uyum"],
        ["TEZGAHTAR"] = ["cari", "urun", "islem", "kasa", "banka", "fatura", "siparis",
                         "emanet", "kur", "vezne", "pos", "sayim", "fiyat-listesi"],
    };
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class YetkiServisi(KuyumcuDbContext db, IMemoryCache cache)
{
    private const string MarkerRol = "__SISTEM__", MarkerModul = "__yapilandirildi__";

    /// <summary>
    /// MODÜL-BAŞI TOHUMLAMA MARKERİ (paket 6 Faz A · ADR-0027 mekanizması).
    ///
    /// ⚠ NEDEN GEREKTİ: tek "__yapilandirildi__" markeri, kiracı bir kez
    /// yapılandırıldıktan SONRA eklenen her yeni modül anahtarını sessizce
    /// kapalı bırakıyordu — kullanıcı menüde bağlantıyı görmüyor, uç 403
    /// veriyordu ve hiçbir yerde "yetki yok" demiyordu. Artık her modül kendi
    /// markeriyle BİR KEZ tohumlanır; mekanizma IDEMPOTENT'tir.
    ///
    /// ⚠ SAHİPLİK: mimari belgesi bu mekanizmayı paket 29'a yazmıştı (ADR-0027).
    /// Paket 29 henüz yazılmadığı ve paket 6 yeni bir anahtar ("bildirim")
    /// getirdiği için mekanizma BURADA kuruldu. 29/31/32/45/46 yalnız kendi
    /// anahtarlarını Moduller.Tumu + Varsayilan'a ekler; buraya dokunmaz.
    /// </summary>
    private static string ModulMarker(string modul) => $"__tohum:{modul}__";

    private static string CacheKey(string tenantId) => $"yetki:{tenantId}";

    /// <summary>Tenant'ın rol→modül matrisi (SAHIP hariç). İlk erişimde varsayılanları tohumlar.</summary>
    public async Task<Dictionary<string, HashSet<string>>> Matris(string tenantId)
    {
        if (cache.TryGetValue(CacheKey(tenantId), out Dictionary<string, HashSet<string>>? onbellek) && onbellek is not null)
            return onbellek;

        await TohumlaGerekiyorsa(tenantId);

        var satirlar = await db.RolYetkileri.AsNoTracking()
            .Where(y => y.TenantId == tenantId && y.Rol != MarkerRol)
            .ToListAsync();

        var matris = Moduller.Roller.ToDictionary(r => r, _ => new HashSet<string>());
        foreach (var s in satirlar)
            if (matris.TryGetValue(s.Rol, out var set)) set.Add(s.Modul);

        cache.Set(CacheKey(tenantId), matris, TimeSpan.FromMinutes(30));
        return matris;
    }

    /// <summary>Belirli rolün erişebildiği modüller (SAHIP → tümü).</summary>
    public async Task<HashSet<string>> RolModulleri(string tenantId, string rol)
    {
        if (rol == "SAHIP") return [.. Moduller.Tumu];
        var matris = await Matris(tenantId);
        return matris.TryGetValue(rol, out var set) ? set : [];
    }

    public async Task<bool> ErisebilirMi(string tenantId, string rol, string modul)
    {
        if (rol == "SAHIP") return true;
        return (await RolModulleri(tenantId, rol)).Contains(modul);
    }

    /// <summary>Matrisi kaydet (SAHIP) — tenant satırları silinip yeniden yazılır (+ marker).</summary>
    public async Task Kaydet(string tenantId, Dictionary<string, string[]> matris)
    {
        var mevcut = await db.RolYetkileri.Where(y => y.TenantId == tenantId).ToListAsync();
        db.RolYetkileri.RemoveRange(mevcut);

        var eklenecek = new List<RolYetki>
        {
            new() { Id = Kimlik.Ulid(), TenantId = tenantId, Rol = MarkerRol, Modul = MarkerModul },
        };
        // ⚠ Kaydet TÜM satırları siler; modül markerleri de silinirdi ve bir
        //   sonraki okumada tohumlama yeniden koşup sahibin KALDIRDIĞI yetkiyi
        //   geri getirirdi. Markerler burada yeniden yazılır.
        eklenecek.AddRange(Moduller.Tumu.Select(m => new RolYetki
        {
            Id = Kimlik.Ulid(), TenantId = tenantId, Rol = MarkerRol, Modul = ModulMarker(m),
        }));
        foreach (var rol in Moduller.Roller)
        {
            if (!matris.TryGetValue(rol, out var modeller) || modeller is null) continue;
            foreach (var m in modeller.Where(Moduller.Tumu.Contains).Distinct())
                eklenecek.Add(new RolYetki { Id = Kimlik.Ulid(), TenantId = tenantId, Rol = rol, Modul = m });
        }
        db.RolYetkileri.AddRange(eklenecek);
        await db.SaveChangesAsync();
        cache.Remove(CacheKey(tenantId));
    }

    /// <summary>
    /// MODÜL-BAŞI, IDEMPOTENT TOHUMLAMA (paket 6 Faz A).
    ///
    /// Üç durum:
    ///   • YENİ KİRACI (hiç marker yok)  → genel marker + her modülün markeri +
    ///     Varsayilan matrisinin tamamı yazılır.
    ///   • ESKİ KİRACI (yalnız genel marker var) → TohumlamaOncesi anahtarları
    ///     için SATIR EKLENMEZ, yalnız markerleri geriye doldurulur; bundan
    ///     SONRA eklenen anahtarlar (örn. "bildirim") varsayılanıyla açılır —
    ///     ANCAK yeniden ADLANDIRILMIŞ anahtarlar (<see cref="Moduller.AnahtarGocu"/>)
    ///     yalnız rol eski anahtara halen sahipse yazılır. Marker her hâlükârda
    ///     yazılır: tohumlama TEK SEFERLİKTİR, sahibin sonradan geri verdiği
    ///     eski yetki yeni anahtarı kendiliğinden açmaz (yetki ekranından açılır).
    ///   • GÜNCEL KİRACI (tüm markerler var) → hiçbir şey yazılmaz.
    /// Her çağrıda en fazla bir kez satır ekler; tekrar çağrılması zararsızdır.
    /// </summary>
    private async Task TohumlaGerekiyorsa(string tenantId)
    {
        var markerlar = await db.RolYetkileri.AsNoTracking()
            .Where(y => y.TenantId == tenantId && y.Rol == MarkerRol)
            .Select(y => y.Modul)
            .ToListAsync();

        var ilkKurulum = !markerlar.Contains(MarkerModul);
        var eklenecek = new List<RolYetki>();

        // ⚠ ANAHTAR GÖÇÜ (bkz. Moduller.AnahtarGocu): eski kiracıda yeniden
        //   ADLANDIRILMIŞ bir anahtarın varsayılan satırı, rol devraldığı ESKİ
        //   anahtara halen sahipse yazılır. Mevcut (marker olmayan) satırları
        //   bir kez okuyup (rol, modul) kümesine alıyoruz.
        HashSet<(string Rol, string Modul)> mevcutYetkiler = [];
        if (!ilkKurulum)
        {
            var satirlar = await db.RolYetkileri.AsNoTracking()
                .Where(y => y.TenantId == tenantId && y.Rol != MarkerRol)
                .Select(y => new { y.Rol, y.Modul })
                .ToListAsync();
            foreach (var s in satirlar) mevcutYetkiler.Add((s.Rol, s.Modul));
        }

        // Göçmen anahtar + rol → satır yazılabilir mi? (yeni kiracıda daima evet)
        bool GocKapisi(string rol, string modul)
        {
            if (ilkKurulum) return true;
            if (!Moduller.AnahtarGocu.TryGetValue(modul, out var eskiler)) return true;
            // TAMAMINA sahip olmalı: eskiden iki ayrı kapının arkasında olan
            // uçlar tek anahtarda birleşiyorsa, yarısına sahip olmak yetmez.
            return eskiler.All(eski => mevcutYetkiler.Contains((rol, eski)));
        }

        if (ilkKurulum)
            eklenecek.Add(new RolYetki { Id = Kimlik.Ulid(), TenantId = tenantId, Rol = MarkerRol, Modul = MarkerModul });

        foreach (var modul in Moduller.Tumu)
        {
            if (markerlar.Contains(ModulMarker(modul))) continue;
            eklenecek.Add(new RolYetki { Id = Kimlik.Ulid(), TenantId = tenantId, Rol = MarkerRol, Modul = ModulMarker(modul) });

            // Eski kiracıda ESKİ anahtarlar yeniden tohumlanmaz: sahibin
            // kaldırdığı bir yetkiyi geri getirmek sessiz bir yetki genişlemesi olur.
            if (!ilkKurulum && Moduller.TohumlamaOncesi.Contains(modul)) continue;

            foreach (var (rol, modeller) in Moduller.Varsayilan)
                if (modeller.Contains(modul) && GocKapisi(rol, modul))
                    eklenecek.Add(new RolYetki { Id = Kimlik.Ulid(), TenantId = tenantId, Rol = rol, Modul = modul });
        }

        if (eklenecek.Count == 0) return;

        db.RolYetkileri.AddRange(eklenecek);
        // Eşzamanlı ilk isteklerde çift tohumlama olabilir — benzersizlik ihlalini yut.
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); }
    }
}

// ── Yetki filtresi (controller/action seviyesinde modül erişimi) ─────────────

/// <summary>
/// Modül erişim kontrolü. SAHIP daima geçer; diğer roller için tenant yapılandırmasına bakılır.
/// RolGerekli ile aynı desende ForbiddenException fırlatır (HataYakalayici 403'e çevirir).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ModulYetkisiAttribute(string modul) : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext ctx)
    {
        var kullanici = AktifKullanici.FromClaims(ctx.HttpContext.User);
        if (kullanici.Rol == "SAHIP") return;

        var servis = ctx.HttpContext.RequestServices.GetRequiredService<YetkiServisi>();
        if (!await servis.ErisebilirMi(kullanici.TenantId, kullanici.Rol, modul))
            throw new ForbiddenException($"Bu ekran için yetkiniz yok ({modul})");
    }
}

// ── DTO ──────────────────────────────────────────────────────────────────────

public sealed class YetkiKaydetDto
{
    /// <summary>rol → erişebileceği modüller. Yalnız yapılandırılabilir roller dikkate alınır.</summary>
    public Dictionary<string, string[]>? Matris { get; set; }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/yetkiler")]
public sealed class YetkiController(YetkiServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/yetkiler/benim — Giriş yapanın erişebildiği modüller (menü için) — herkes</summary>
    [HttpGet("benim")]
    [HerkeseAcik("Menüyü çizmek için kendi yetkilerin — başka rolün matrisi dönmez")]
    public async Task<object> Benim()
    {
        var moduller = await servis.RolModulleri(Islem.TenantId, Islem.Rol);
        return new { moduller = moduller.OrderBy(x => x).ToArray() };
    }

    /// <summary>GET /api/v1/yetkiler — Tüm rol/modül matrisi (yapılandırma ekranı) — SAHIP</summary>
    [HttpGet]
    [RolGerekli("SAHIP")]
    public async Task<object> Getir()
    {
        var matris = await servis.Matris(Islem.TenantId);
        return new
        {
            moduller = Moduller.Tumu,
            roller = Moduller.Roller,
            matris = Moduller.Roller.ToDictionary(r => r, r => matris[r].OrderBy(x => x).ToArray()),
        };
    }

    /// <summary>PUT /api/v1/yetkiler — Matrisi kaydet — SAHIP</summary>
    [HttpPut]
    [RolGerekli("SAHIP")]
    public async Task<object> Kaydet([FromBody] YetkiKaydetDto dto)
    {
        await servis.Kaydet(Islem.TenantId, dto.Matris ?? new());
        return new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Yetkiler güncellendi") };
    }
}
