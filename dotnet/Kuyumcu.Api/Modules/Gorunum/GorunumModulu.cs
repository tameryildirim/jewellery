using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Gorunum;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-36b — GÖRÜNÜM (renk şeması + yazı tipi ölçeği)
//
// Kurucu talimatı 16: "renk ve yazı tipi benim işim." Tercih İŞLETMENİNDİR,
// kullanıcının değil: aynı dükkânın iki tezgâhı farklı renkte olmaz. Bu yüzden
// KullaniciTercihi (P-37, kullanıcı bazlı) değil, kiraci_ayarlari (kiracı
// bazlı) kullanılır.
//
// ⚠ MIGRATION GEREKMEDİ — ve bu bir atlama değil, ölçülmüş bir karardır:
//   `kiraci_ayarlari` (KiraciAyari) tablosu kiracı düzeyinde anahtar/değer
//   saklamak için ZATEN vardır, IKiraciVarlik'tır (global kiracı filtresi +
//   KiraciYazmaDenetcisi + denetim izi kendiliğinden çalışır) ve
//   (tenant_id, anahtar) üzerinde BENZERSİZ indeksi vardır. Tenant tablosuna
//   kolon eklemek bu üç mekanizmanın DIŞINA çıkmak olurdu (KiraciAyari'nin
//   kendi XML belgesi bunu açıkça söylüyor). Görev metni "ŞARTLI kolon" dedi;
//   şart oluşmadı, gerekçesi budur.
//
// ⚠ TERCİHTİR, YETKİ DEĞİLDİR: sunucudaki hiçbir kapı bu değere bakmaz.
//   Hard DELETE serbesttir (finansal varlık değil) — "varsayılana dön" satırı
//   siler, defteri bozmaz.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Görünüm ayarının kapalı listeleri — sihirli dize yazılmaz.
///
/// ⚠ PAKET B-1 (dalga S-B): liste 3 şema → <b>8 şema</b>, 2 ölçek → <b>3 punto</b>
///   oldu ve <b>tema modu</b> (aydınlık/karanlık/otomatik) eklendi. Adlar ve
///   varsayılan <c>dokumanlar/02-mimari/sadelestirme/TEMA-SISTEMI-2026-09-23.md</c>
///   § 0/§ 4/§ 8'den BİREBİR alınmıştır (496 kontrast çifti orada ölçüldü).
/// ⚠ ESKİ DEĞER ÖLMEZ: bugün kayıtlı <c>altin</c>/<c>kurumsal</c>/<c>yogun</c>/
///   <c>okunakli</c> değerleri migration'da göç eder, AYRICA burada geriye
///   dönük TAKMA AD olarak kabul edilir (eski bir istemci 400 almaz).
/// </summary>
public static class GorunumSecenekleri
{
    /// <summary>
    /// TEK ŞEMA: <b>Yıldız</b> (kurucu kararı 17.10 — "tema sadece Yıldız, karanlık
    /// ve açık mod kalacak; diğer temalar gidecek"). Eski sekiz şema
    /// (platin/zumrut/grafit/bordo/okyanus/menekse/zeytin/fildisi) ve onların
    /// eski kodları (altin/kurumsal/platin-mavi) <see cref="SemaTakmaAdlari"/>
    /// ile "yildiz"e göç eder (migration TemaYildizTek + bu harita: eski istemci
    /// 400 almaz, unutulmuş satır varsayılana değil Yıldız'a çözülür).
    /// </summary>
    public const string SemaVarsayilan = "yildiz";

    /// <summary>Seçilebilir şemalar — yalnız Yıldız (açık/koyu ayrı boyuttur: <see cref="TemaModlari"/>).</summary>
    public static readonly string[] Semalar = [SemaVarsayilan];

    /// <summary>
    /// Varsayılan gövde puntosu: <b>18</b> px.
    ///
    /// ⚠ ÖLÇÜM KAZANIR: TEMA-SISTEMI § 8 "varsayılan 16" der, ama S-1 şeridi
    ///   <c>belirtecler.css</c>'te <c>:root { --yazi-govde: 18px }</c> yazdı
    ///   (216. satır) ve öznitelik yazılmayan hâli 18 px'tir. Sunucu 16 deseydi
    ///   varsayılan kullanıcı canlıda 18 görür, ayar ekranında "16" okurdu.
    /// </summary>
    public const string OlcekVarsayilan = "18";

    /// <summary>
    /// Gövde yazı boyutu kademeleri — 16 / 18 / 20 px (§ 8).
    /// ⚠ İlk eleman varsayılan DEĞİLDİR (kademeler artan sırada gösterilir).
    /// </summary>
    public static readonly string[] Olcekler = ["16", OlcekVarsayilan, "20"];

    /// <summary>
    /// Varsayılan tema modu: işletim sisteminin tercihini izle.
    /// ⚠ ÖLÇÜM KAZANIR: belge "otomatik" der, canlı <c>TemaServisi</c> ve
    ///   <c>js/tema.js</c> bugün <c>"sistem"</c> yazıyor ve localStorage'daki
    ///   kayıtlı değer odur. Sunucu "otomatik" deseydi istemcinin beyaz listesi
    ///   onu reddederdi. Belgedeki ad takma ad olarak kabul edilir.
    /// </summary>
    public const string TemaModuVarsayilan = "sistem";

    /// <summary>Aydınlık/karanlık tercihi (§ 5) — kullanıcı satırında kalıcıdır.</summary>
    public static readonly string[] TemaModlari = [TemaModuVarsayilan, "aydinlik", "karanlik"];

    /// <summary>GÖÇ HARİTASI — belgedeki "otomatik" → canlıdaki "sistem".</summary>
    public static readonly IReadOnlyDictionary<string, string> TemaModuTakmaAdlari =
        new Dictionary<string, string>(StringComparer.Ordinal) { ["otomatik"] = TemaModuVarsayilan };

    /// <summary>
    /// GÖÇ HARİTASI — eski şema kodu → "yildiz". Hem migration
    /// (<c>TemaYildizTek</c>) hem de <see cref="SemaNormalle"/> AYNI listeyi
    /// kullanır ki veritabanında unutulmuş bir satır 400'e düşmesin.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> SemaTakmaAdlari =
        EskiSemalar.ToDictionary(k => k, _ => SemaVarsayilan, StringComparer.Ordinal);

    /// <summary>Kaldırılan şema kodlarının tamamı (17.10 öncesi 8 şema + daha eski 3 kod).</summary>
    public static IReadOnlyList<string> EskiSemalar =>
    [
        "platin", "zumrut", "grafit", "bordo", "okyanus", "menekse", "zeytin", "fildisi",
        "altin", "kurumsal", "platin-mavi",
    ];

    /// <summary>GÖÇ HARİTASI — eski ölçek kodu → punto. yogun 14→16, okunakli 16→18.</summary>
    public static readonly IReadOnlyDictionary<string, string> OlcekTakmaAdlari =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // ⚠ ÖLÇÜM KAZANIR: belgeye göre okunakli→18 olurdu; canlı CSS
            //   (1954. satır) `[data-yazi="20"], [data-olcek="okunakli"]`
            //   yazdığı için "okunakli" bugün 20 px'tir. Göç, kullanıcının
            //   BUGÜN GÖRDÜĞÜ boyutu korur.
            ["yogun"] = "16",
            ["okunakli"] = "20",
        };

    /// <summary>Kodu kanonik slug'a çevirir; tanınmıyorsa <c>null</c>.</summary>
    public static string? SemaNormalle(string? s) =>
        s is null ? null
        : Array.IndexOf(Semalar, s) >= 0 ? s
        : SemaTakmaAdlari.TryGetValue(s, out var y) ? y : null;

    /// <summary>Kodu kanonik puntoya çevirir; tanınmıyorsa <c>null</c>.</summary>
    public static string? OlcekNormalle(string? o) =>
        o is null ? null
        : Array.IndexOf(Olcekler, o) >= 0 ? o
        : OlcekTakmaAdlari.TryGetValue(o, out var y) ? y : null;

    /// <summary>Tema modunu kanonik koda çevirir; tanınmıyorsa <c>null</c>.</summary>
    public static string? TemaModuNormalle(string? t) =>
        t is null ? null
        : Array.IndexOf(TemaModlari, t) >= 0 ? t
        : TemaModuTakmaAdlari.TryGetValue(t, out var y) ? y : null;

    public static bool SemaGecerli(string? s) => SemaNormalle(s) is not null;
    public static bool OlcekGecerli(string? o) => OlcekNormalle(o) is not null;
    public static bool TemaModuGecerli(string? t) => TemaModuNormalle(t) is not null;

    /// <summary>Punto kodunu sayıya çevirir ("18" → 18). Tanınmazsa varsayılan.</summary>
    public static int Punto(string? olcek) =>
        int.TryParse(OlcekNormalle(olcek) ?? OlcekVarsayilan, out var p) ? p : 18;

    /// <summary>Sayıyı punto koduna çevirir (18 → "18"). Tanınmazsa <c>null</c>.</summary>
    public static string? PuntoKodu(int? punto) =>
        punto is null ? null : OlcekNormalle(punto.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
}

/// <summary>
/// Kiracının (dükkânın) görünüm VARSAYILANI — okunan ve yazılan tek gövde.
/// ⚠ B-1: <paramref name="TemaModu"/> EKLENDİ (alan eklemek geriye dönük
/// uyumludur; eski istemci fazla alanı yok sayar). Alan KALDIRILMADI.
/// </summary>
public sealed record GorunumGorunum(string Sema, string YaziOlcegi, string TemaModu);

public sealed class GorunumServisi(KuyumcuDbContext db, DenetimServisi denetim)
{
    public const string Varlik = "Gorunum";

    public async Task<GorunumGorunum> Getir(string tenantId, CancellationToken iptal = default)
    {
        var satirlar = await db.KiraciAyarlari.AsNoTracking()
            .Where(a => a.TenantId == tenantId
                && (a.Anahtar == KiraciAyarAnahtari.GorunumSemasi
                 || a.Anahtar == KiraciAyarAnahtari.GorunumYaziOlcegi
                 || a.Anahtar == KiraciAyarAnahtari.GorunumTemaModu))
            .Select(a => new { a.Anahtar, a.Deger })
            .ToListAsync(iptal);

        var sema = satirlar.FirstOrDefault(a => a.Anahtar == KiraciAyarAnahtari.GorunumSemasi)?.Deger;
        var olcek = satirlar.FirstOrDefault(a => a.Anahtar == KiraciAyarAnahtari.GorunumYaziOlcegi)?.Deger;
        var temaModu = satirlar.FirstOrDefault(a => a.Anahtar == KiraciAyarAnahtari.GorunumTemaModu)?.Deger;

        // ⚠ Bozuk değer SESSİZCE uygulanmaz; varsayılana düşer. ESKİ değer
        //   (altin/kurumsal/yogun/okunakli) ise ÖLMEZ — takma ad haritasıyla
        //   yeni slug'a çevrilir (B-1). Tarayıcı tarafı da AYNI beyaz listeyi
        //   yazar (iki taraf da güvenmez — Mehmet K1).
        return new GorunumGorunum(
            GorunumSecenekleri.SemaNormalle(sema) ?? GorunumSecenekleri.SemaVarsayilan,
            GorunumSecenekleri.OlcekNormalle(olcek) ?? GorunumSecenekleri.OlcekVarsayilan,
            GorunumSecenekleri.TemaModuNormalle(temaModu) ?? GorunumSecenekleri.TemaModuVarsayilan);
    }

    /// <summary>
    /// PATCH sözleşmesi: gövdede OLMAYAN alan KORUNUR, <c>null</c> gönderilen
    /// alan VARSAYILANA döner (ayar satırı silinir). Tam gövdeyle üzerine
    /// yazıp diğer alanı imha etmek YASAK.
    /// </summary>
    public async Task<GorunumGorunum> Yama(
        AktifKullanici islem, GorunumYamaDto dto, CancellationToken iptal = default)
    {
        // ⚠ DTO DataAnnotations yalnız controller yolunda çalışır → beyaz liste
        //   BURADA, servis içinde ELLE denetlenir.
        if (dto.SemaVerildi && dto.Sema is not null && !GorunumSecenekleri.SemaGecerli(dto.Sema))
            throw new BadRequestException($"Bilinmeyen renk şeması: {dto.Sema}");
        if (dto.OlcekVerildi && dto.YaziOlcegi is not null && !GorunumSecenekleri.OlcekGecerli(dto.YaziOlcegi))
            throw new BadRequestException($"Bilinmeyen yazı boyutu: {dto.YaziOlcegi}");
        if (dto.TemaModuVerildi && dto.TemaModu is not null && !GorunumSecenekleri.TemaModuGecerli(dto.TemaModu))
            throw new BadRequestException($"Bilinmeyen tema modu: {dto.TemaModu}");

        var degisen = new List<string>(3);
        if (dto.SemaVerildi)
        {
            // ⚠ DEPOYA KANONİK SLUG yazılır: takma adla gelen istek (eski
            //   istemci) veritabanında eski adı YAŞATMAZ.
            await Yaz(islem.TenantId, KiraciAyarAnahtari.GorunumSemasi,
                GorunumSecenekleri.SemaNormalle(dto.Sema), iptal);
            degisen.Add("renk şeması");
        }
        if (dto.OlcekVerildi)
        {
            await Yaz(islem.TenantId, KiraciAyarAnahtari.GorunumYaziOlcegi,
                GorunumSecenekleri.OlcekNormalle(dto.YaziOlcegi), iptal);
            degisen.Add("yazı boyutu");
        }
        if (dto.TemaModuVerildi)
        {
            await Yaz(islem.TenantId, KiraciAyarAnahtari.GorunumTemaModu, dto.TemaModu, iptal);
            degisen.Add("tema modu");
        }

        if (degisen.Count == 0)
            throw new BadRequestException("Hiçbir alan gönderilmedi — değiştirilecek bir şey yok.");

        // Denetim kaydı ÇAĞRILIR (yeniden yazılmaz) — DoD sözleşmesi.
        denetim.YazmaKaydet(islem, Varlik, islem.TenantId,
            $"Görünüm ayarı güncellendi: {string.Join(", ", degisen)}", hassas: false);

        await db.SaveChangesAsync(iptal);
        return await Getir(islem.TenantId, iptal);
    }

    private async Task Yaz(string tenantId, string anahtar, string? deger, CancellationToken iptal)
    {
        var kayit = await db.KiraciAyarlari
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Anahtar == anahtar, iptal);

        if (deger is null)
        {
            // null = VARSAYILANA DÖN. Bir tercih satırıdır; finansal değildir.
            if (kayit is not null) db.KiraciAyarlari.Remove(kayit);
            return;
        }

        if (kayit is null)
        {
            db.KiraciAyarlari.Add(new KiraciAyari
            {
                Id = Kimlik.Ulid(),
                TenantId = tenantId,
                Anahtar = anahtar,
                Deger = deger,
                Guncellendi = DateTime.UtcNow,
            });
            return;
        }

        kayit.Deger = deger;
        kayit.Guncellendi = DateTime.UtcNow;
    }
}

// ── DTO ──────────────────────────────────────────────────────────────────────

/// <summary>
/// Görünüm yaması. "Alan yok" ile "alan null" AYRI anlamlıdır, bu yüzden
/// setter'lar gönderilme bayrağı tutar (PATCH sözleşmesi).
/// </summary>
public sealed class GorunumYamaDto
{
    private string? sema;
    private string? olcek;
    private string? temaModu;

    public string? Sema
    {
        get => sema;
        set { sema = value; SemaVerildi = true; }
    }

    public string? YaziOlcegi
    {
        get => olcek;
        set { olcek = value; OlcekVerildi = true; }
    }

    /// <summary>PAKET B-1 — dükkânın aydınlık/karanlık varsayılanı.</summary>
    public string? TemaModu
    {
        get => temaModu;
        set { temaModu = value; TemaModuVerildi = true; }
    }

    [System.Text.Json.Serialization.JsonIgnore] public bool SemaVerildi { get; private set; }
    [System.Text.Json.Serialization.JsonIgnore] public bool OlcekVerildi { get; private set; }
    [System.Text.Json.Serialization.JsonIgnore] public bool TemaModuVerildi { get; private set; }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/gorunum")]
public sealed class GorunumController(GorunumServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/gorunum — işletmenin renk şeması + yazı ölçeği.</summary>
    [HttpGet]
    [HerkeseAcik("Ekranın kendi görünümünü çizebilmesi için HER kullanıcı okur; modül kapısı olsaydı tezgâhtarın ekranı varsayılana düşerdi")]
    public async Task<GorunumGorunum> Getir(CancellationToken iptal) =>
        await servis.Getir(Islem.TenantId, iptal);

    /// <summary>
    /// PATCH /api/v1/gorunum — kısmi güncelleme (yoksa koru, null ise varsayılana dön).
    /// ⚠ Yalnız SAHİP: bu uç DÜKKÂN VARSAYILANIDIR. B-1'den sonra her kullanıcı
    ///   KENDİ görünümünü <c>PATCH /api/v1/kullanici/gorunum</c> ile seçer
    ///   (tuval notu n4); SAHİP kilidi yalnız dükkân varsayılanında kalır.
    /// </summary>
    [HttpPatch]
    [RolGerekli("SAHIP")]
    public async Task<object> Yama([FromBody] GorunumYamaDto dto, CancellationToken iptal)
    {
        var g = await servis.Yama(Islem, dto, iptal);
        return new { gorunum = g, mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Görünüm kaydedildi") };
    }
}
