using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Gorunum;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-1 (dalga S-B) — KULLANICI GÖRÜNÜM TERCİHİ
//
// Kurucu, 23 Eylül: "o rapor ne diyorsa onu yapacaksın, backend de ona göre
// şekillensin, eğer mevcut hali bozulmuyorsa."
// Tuval notu n4: "her kullanıcı kendi temasını, aydınlık/karanlık tercihini,
// yazı boyutunu (16/18/20 px) ... seçer; SAHİP dükkân varsayılanını belirler."
//
// ⚠ ÜÇ KATMANLI ÇÖZÜM — TEK YERDE (ArayuzModu.Coz deseninin birebir aynısı):
//      kullanıcı satırı (null değilse)  →  kiracı varsayılanı  →  sistem
//   NULL = "bilinmiyor" DEĞİL, "devral" demektir. Bu yüzden hiçbir kullanıcı
//   satırı migration'da doldurulmaz: mevcut kiracının ekranı sessizce DEĞİŞMEZ.
//
// ⚠ TERCİHTİR, YETKİ DEĞİLDİR: sunucudaki hiçbir para/MASAK/gün kilidi kapısı
//   bu değerlere bakmaz. Bu yüzden uç HER kullanıcıya açıktır — ama YALNIZ
//   KENDİ satırına yazar (başkasının satırını yazmanın yolu YOKTUR).
//
// ⚠ KÂĞIT DAİMA BEYAZ: bu üçlü yalnız EKRAN görünümüdür. Yazdırma çıktısı
//   (Cikti.razor, belge şablonları, @media print) temadan ETKİLENMEZ; sunucu
//   bu değerleri hiçbir belge/PDF/CSV üretimine geçirmez.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir alanın hangi katmandan geldiği — ekran bunu rozetle yazar (UX-S21).</summary>
public static class GorunumKaynagi
{
    public const string Kullanici = "kullanici";
    public const string Kiraci = "kiraci";
    public const string Sistem = "sistem";
}

/// <summary>
/// Kullanıcının ÇÖZÜLMÜŞ görünümü + ham katmanlar + kaynak rozetleri.
/// Ekran hiçbir çözümü tekrarlamaz; sunucunun söylediğini çizer.
/// </summary>
/// <param name="TemaModu">Etkin tema modu (otomatik | aydinlik | karanlik).</param>
/// <param name="Sema">Etkin renk şeması (8 slug'dan biri).</param>
/// <param name="YaziBoyutu">Etkin gövde puntosu (16 | 18 | 20).</param>
/// <param name="TemaModuKaynagi">kullanici | kiraci | sistem.</param>
/// <param name="OzelTemaModu">Kullanıcının KENDİ satırı — null ise devralıyor.</param>
/// <param name="KiraciTemaModu">Dükkân varsayılanı (SAHİP belirler).</param>
public sealed record KullaniciGorunumu(
    string TemaModu,
    string Sema,
    int YaziBoyutu,
    string TemaModuKaynagi,
    string SemaKaynagi,
    string YaziBoyutuKaynagi,
    string? OzelTemaModu,
    string? OzelSema,
    int? OzelYaziBoyutu,
    string KiraciTemaModu,
    string KiraciSema,
    int KiraciYaziBoyutu,
    IReadOnlyList<string> Semalar,
    IReadOnlyList<string> TemaModlari,
    IReadOnlyList<int> YaziBoyutlari);

/// <summary>
/// Kullanıcı görünüm yaması. "Alan yok" ile "alan null" AYRI anlamlıdır
/// (PATCH sözleşmesi): gövdede YOKSA korunur, <c>null</c> ise DÜKKÂN
/// VARSAYILANINA devredilir (kullanıcı satırı temizlenir = silme yolu).
/// </summary>
public sealed class KullaniciGorunumYamaDto
{
    private string? temaModu;
    private string? sema;
    private int? yaziBoyutu;

    public string? TemaModu
    {
        get => temaModu;
        set { temaModu = value; TemaModuVerildi = true; }
    }

    public string? Sema
    {
        get => sema;
        set { sema = value; SemaVerildi = true; }
    }

    public int? YaziBoyutu
    {
        get => yaziBoyutu;
        set { yaziBoyutu = value; YaziBoyutuVerildi = true; }
    }

    [System.Text.Json.Serialization.JsonIgnore] public bool TemaModuVerildi { get; private set; }
    [System.Text.Json.Serialization.JsonIgnore] public bool SemaVerildi { get; private set; }
    [System.Text.Json.Serialization.JsonIgnore] public bool YaziBoyutuVerildi { get; private set; }
}

public sealed class KullaniciGorunumServisi(
    KuyumcuDbContext db, GorunumServisi kiraciGorunum, DenetimServisi denetim)
{
    public const string Varlik = "KullaniciGorunum";

    public async Task<KullaniciGorunumu> Getir(AktifKullanici islem, CancellationToken iptal = default)
    {
        var k = await db.Kullanicilar.AsNoTracking()
            .Where(x => x.Id == islem.Id)
            .Select(x => new { x.TemaModu, x.Sema, x.YaziBoyutu })
            .FirstOrDefaultAsync(iptal);

        var kiraci = await kiraciGorunum.Getir(islem.TenantId, iptal);

        // ⚠ Bozuk/eski kullanıcı değeri SESSİZCE uygulanmaz; takma adla çevrilir,
        //   çevrilemiyorsa katman DEVRE DIŞI kalır (kiracıya düşer) — md.1:
        //   "geçerli sandım" diye yanlış renk çizilmez.
        var ozelTema = GorunumSecenekleri.TemaModuNormalle(k?.TemaModu);
        var ozelSema = GorunumSecenekleri.SemaNormalle(k?.Sema);
        var ozelPuntoKodu = GorunumSecenekleri.PuntoKodu(k?.YaziBoyutu);

        return new KullaniciGorunumu(
            TemaModu: ozelTema ?? kiraci.TemaModu,
            Sema: ozelSema ?? kiraci.Sema,
            YaziBoyutu: GorunumSecenekleri.Punto(ozelPuntoKodu ?? kiraci.YaziOlcegi),
            TemaModuKaynagi: Kaynak(ozelTema, kiraci.TemaModu, GorunumSecenekleri.TemaModuVarsayilan),
            SemaKaynagi: Kaynak(ozelSema, kiraci.Sema, GorunumSecenekleri.SemaVarsayilan),
            YaziBoyutuKaynagi: Kaynak(ozelPuntoKodu, kiraci.YaziOlcegi, GorunumSecenekleri.OlcekVarsayilan),
            OzelTemaModu: ozelTema,
            OzelSema: ozelSema,
            OzelYaziBoyutu: ozelPuntoKodu is null ? null : GorunumSecenekleri.Punto(ozelPuntoKodu),
            KiraciTemaModu: kiraci.TemaModu,
            KiraciSema: kiraci.Sema,
            KiraciYaziBoyutu: GorunumSecenekleri.Punto(kiraci.YaziOlcegi),
            Semalar: GorunumSecenekleri.Semalar,
            TemaModlari: GorunumSecenekleri.TemaModlari,
            YaziBoyutlari: [.. GorunumSecenekleri.Olcekler.Select(GorunumSecenekleri.Punto)]);
    }

    /// <summary>
    /// Kullanıcı satırı varsa "kullanici"; dükkân varsayılandan FARKLI bir şey
    /// söylüyorsa "kiraci"; ikisi de yoksa "sistem".
    /// </summary>
    private static string Kaynak(string? ozel, string kiraciDegeri, string sistemVarsayilani) =>
        ozel is not null ? GorunumKaynagi.Kullanici
        : kiraciDegeri != sistemVarsayilani ? GorunumKaynagi.Kiraci
        : GorunumKaynagi.Sistem;

    /// <summary>
    /// PATCH sözleşmesi: gövdede OLMAYAN alan KORUNUR, <c>null</c> gönderilen
    /// alan DÜKKÂN VARSAYILANINA devredilir (kullanıcı satırı temizlenir).
    /// </summary>
    public async Task<KullaniciGorunumu> Yama(
        AktifKullanici islem, KullaniciGorunumYamaDto dto, CancellationToken iptal = default)
    {
        // ⚠ DTO DataAnnotations yalnız controller yolunda çalışır → beyaz liste
        //   BURADA, servis içinde ELLE denetlenir.
        if (dto.TemaModuVerildi && dto.TemaModu is not null && !GorunumSecenekleri.TemaModuGecerli(dto.TemaModu))
            throw new BadRequestException($"Bilinmeyen tema modu: {dto.TemaModu}");
        if (dto.SemaVerildi && dto.Sema is not null && !GorunumSecenekleri.SemaGecerli(dto.Sema))
            throw new BadRequestException($"Bilinmeyen renk şeması: {dto.Sema}");
        if (dto.YaziBoyutuVerildi && dto.YaziBoyutu is not null
            && GorunumSecenekleri.PuntoKodu(dto.YaziBoyutu) is null)
            throw new BadRequestException($"Bilinmeyen yazı boyutu: {dto.YaziBoyutu}");

        var degisen = new List<string>(3);
        if (!dto.TemaModuVerildi && !dto.SemaVerildi && !dto.YaziBoyutuVerildi)
            throw new BadRequestException("Hiçbir alan gönderilmedi — değiştirilecek bir şey yok.");

        // ⚠ Find/FindAsync YASAK — global kiracı filtresini atlar.
        var kullanici = await db.Kullanicilar.FirstOrDefaultAsync(x => x.Id == islem.Id, iptal)
            ?? throw new NotFoundException("Kullanıcı bulunamadı.");

        if (dto.TemaModuVerildi)
        {
            kullanici.TemaModu = GorunumSecenekleri.TemaModuNormalle(dto.TemaModu);
            degisen.Add("tema modu");
        }
        if (dto.SemaVerildi)
        {
            kullanici.Sema = GorunumSecenekleri.SemaNormalle(dto.Sema);
            degisen.Add("renk şeması");
        }
        if (dto.YaziBoyutuVerildi)
        {
            var kod = GorunumSecenekleri.PuntoKodu(dto.YaziBoyutu);
            kullanici.YaziBoyutu = kod is null ? null : GorunumSecenekleri.Punto(kod);
            degisen.Add("yazı boyutu");
        }

        kullanici.Guncellendi = DateTime.UtcNow;

        // Denetim kaydı ÇAĞRILIR (yeniden yazılmaz) — DoD sözleşmesi.
        denetim.YazmaKaydet(islem, Varlik, islem.Id,
            $"Kullanıcı görünümü güncellendi: {string.Join(", ", degisen)}", hassas: false);

        await db.SaveChangesAsync(iptal);
        return await Getir(islem, iptal);
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/kullanici/gorunum")]
public sealed class KullaniciGorunumController(KullaniciGorunumServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/kullanici/gorunum — ÇÖZÜLMÜŞ görünüm + katmanlar.</summary>
    [HttpGet]
    [HerkeseAcik("Her kullanıcı KENDİ ekranının görünümünü okur; modül kapısı olsaydı tezgâhtarın ekranı varsayılana düşerdi")]
    public async Task<KullaniciGorunumu> Getir(CancellationToken iptal) =>
        await servis.Getir(Islem, iptal);

    /// <summary>
    /// PATCH /api/v1/kullanici/gorunum — kısmi güncelleme
    /// (gövdede yoksa koru, null ise dükkân varsayılanına devret).
    /// ⚠ HER kullanıcı yazar — ama YALNIZ kendi satırına: hedef kimlik
    ///   gövdeden DEĞİL, jetondan gelir.
    /// </summary>
    [HttpPatch]
    [HerkeseAcik("Kişisel EKRAN tercihi; hiçbir para/MASAK/gün kilidi kapısı bu değere bakmaz — dükkân varsayılanı ayrı uçta SAHİP kilidindedir")]
    public async Task<object> Yama([FromBody] KullaniciGorunumYamaDto dto, CancellationToken iptal)
    {
        var g = await servis.Yama(Islem, dto, iptal);
        return new { gorunum = g, mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Görünüm kaydedildi") };
    }
}
