using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json.Serialization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Pano;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MKur = Kuyumcu.Api.Modules.Kur;

namespace Kuyumcu.Api.Modules.Pano;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-17 (kimlik Y-7) — PANO FİYAT UCU + VİTRİN AYARI.
//
// ⚠ MIGRATION YOK ve bu bir eksiklik değil, PLAN'ın kendi hükmüdür:
//   P-17 DoD "Pano fiyatı marj/yuvarlama kaydı KİRACI AYARINDAN gelir" der.
//   `kiraci_ayarlari` MEVCUT bir tablodur (IKiraciVarlik → global kiracı
//   filtresi + yazma denetçisi + denetim izi kendiliğinden çalışır). Yeni bir
//   `pano_kalem_ayarlari` tablosu aynı üç mekanizmayı SIFIRDAN kurardı.
//   Plan § 2 P-17 "Migration: ŞARTLI" diyor; şart gerçekleşmedi.
//
// ⚠ KALEM BAŞINA BİR SATIR: `kiraci_ayarlari.deger` 200 karakterdir. 16 kalemlik
//   tek JSON bu sınıra sığmaz; her kalem kendi satırında, sıkı CSV ile durur:
//     anahtar = "pano_kalem.<PARITE_KODU>"   (60 karakter sınırı içinde)
//     deger   = "<sira>|<alisMarj>|<satisMarj>|<yuvarlama>|<aktif>|<etiket>"
//   Sayılar InvariantCulture ile yazılır/okunur (kültür kaymaz).
//
// ⚠ HESAP YENİDEN YAZILMAZ: marj/yuvarlama/ters makas/tavan kuralları
//   `PanoFiyatServisi` (saf) içinde kalır; burası yalnız KAYNAK (kur pariteleri)
//   ile AYAR'ı buluşturur. İkinci bir hesap yolu açılmaz.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>`kiraci_ayarlari` anahtar biçimi — tek kaynak.</summary>
public static class PanoAyarAnahtari
{
    public const string Onek = "pano_kalem.";

    public static string Anahtar(string pariteKodu) => Onek + pariteKodu;

    /// <summary>Anahtardan parite kodunu çıkarır; bu paketin anahtarı değilse null.</summary>
    public static string? Kod(string anahtar) =>
        anahtar.StartsWith(Onek, StringComparison.Ordinal) ? anahtar[Onek.Length..] : null;

    /// <summary>
    /// PAKET EKSIK-KUCUK (B2-10): işçilik ibaresi satırı. "pano_kalem." önekini
    /// TAŞIMAZ → kalem okuyucusu (Satirlar) bu satırı kalem sanmaz.
    /// </summary>
    public const string IscilikIbaresi = "pano.iscilik_ibaresi";
}

/// <summary>
/// PAKET EKSIK-KUCUK (B2-10) — PATCH /pano/iscilik-ibaresi gövdesi.
/// Gövdede alan YOKSA 400; <c>null</c> = ibare silinir (tam ekran pano kilitlenir).
/// </summary>
public sealed class IscilikIbaresiPatchDto
{
    private string? _iscilikIbaresi;
    public string? IscilikIbaresi { get => _iscilikIbaresi; set { _iscilikIbaresi = value; IscilikIbaresiVerildi = true; } }
    [JsonIgnore] public bool IscilikIbaresiVerildi { get; private set; }
}

// ── DTO ──────────────────────────────────────────────────────────────────────

/// <summary>
/// PATCH sözleşmesi: gövdede OLMAYAN alan DEĞİŞMEZ, açıkça <c>null</c> gelen
/// alan SİLİNİR (varsayılana döner). Üç durum ayrımı `...Verildi` bayraklarıyla
/// yapılır — tam gövdeyle üzerine yazılmaz (PLAN § 1 md.9).
/// </summary>
public sealed class PanoAyarPatchDto
{
    private string? _etiket;
    private int? _sira;
    private decimal? _alisMarjTl, _satisMarjTl, _yuvarlamaAdimi;
    private bool? _aktif;

    [MaxLength(40, ErrorMessage = "Etiket en fazla 40 karakter olabilir")]
    public string? Etiket { get => _etiket; set { _etiket = value; EtiketVerildi = true; } }
    [JsonIgnore] public bool EtiketVerildi { get; private set; }

    [Range(0, 99, ErrorMessage = "Sıra 0-99 arasında olmalıdır")]
    public int? Sira { get => _sira; set { _sira = value; SiraVerildi = true; } }
    [JsonIgnore] public bool SiraVerildi { get; private set; }

    public decimal? AlisMarjTl { get => _alisMarjTl; set { _alisMarjTl = value; AlisMarjTlVerildi = true; } }
    [JsonIgnore] public bool AlisMarjTlVerildi { get; private set; }

    public decimal? SatisMarjTl { get => _satisMarjTl; set { _satisMarjTl = value; SatisMarjTlVerildi = true; } }
    [JsonIgnore] public bool SatisMarjTlVerildi { get; private set; }

    public decimal? YuvarlamaAdimi { get => _yuvarlamaAdimi; set { _yuvarlamaAdimi = value; YuvarlamaAdimiVerildi = true; } }
    [JsonIgnore] public bool YuvarlamaAdimiVerildi { get; private set; }

    public bool? Aktif { get => _aktif; set { _aktif = value; AktifVerildi = true; } }
    [JsonIgnore] public bool AktifVerildi { get; private set; }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class PanoAyarServisi(
    KuyumcuDbContext db,
    MKur.KurServisi kur,
    MKur.KurKaynakDefteri kaynakDefteri,
    DenetimServisi denetim)
{
    private const string Varlik = "PanoKalemAyari";

    /// <summary>Ayarlanabilir kalemlerin TAM listesi — kanonik parite kodları.</summary>
    public static IReadOnlyList<string> Kalemler => [.. MKur.PariteKodlari.Adlar.Keys];

    /// <summary>
    /// Kiracının çözülmüş ayarları (varsayılan + ezme). Kayıtsız kalem de
    /// listeye GİRER: UX-S25 "oluşturulabilen her kayıt düzeltilebilir" —
    /// kuyumcu kapalı bir kalemi ekranda görüp açabilmelidir.
    /// </summary>
    public async Task<List<PanoKalemAyari>> Oku(string tenantId, CancellationToken iptal = default)
    {
        var satirlar = await Satirlar(tenantId, iptal);
        var liste = new List<PanoKalemAyari>();
        var sira = 0;

        foreach (var kod in Kalemler)
        {
            var varsayilan = Varsayilan(kod, sira++);
            liste.Add(satirlar.TryGetValue(kod, out var ham) ? Coz(kod, ham, varsayilan) : varsayilan);
        }
        return [.. liste.OrderBy(a => a.Sira).ThenBy(a => a.PariteKodu, StringComparer.Ordinal)];
    }

    /// <summary>Yönetim görünümü — seçenek listeleri de buradan gelir (UX-S26).</summary>
    public async Task<PanoAyarlariGorunum> AyarGorunumu(string tenantId, CancellationToken iptal = default)
    {
        var satirlar = await Satirlar(tenantId, iptal);
        var ayarlar = await Oku(tenantId, iptal);

        return new PanoAyarlariGorunum(
            [.. ayarlar.Select(a => new PanoAyarGorunum(
                a.PariteKodu,
                MKur.PariteKodlari.Ad(a.PariteKodu),
                a.Etiket,
                a.Sira,
                a.AlisMarjTl,
                a.SatisMarjTl,
                a.YuvarlamaAdimi,
                a.Aktif,
                Ozel: satirlar.ContainsKey(a.PariteKodu)))],
            [.. PanoFiyatServisi.YuvarlamaAdimlari],
            PanoFiyatServisi.TavanSatir,
            PanoFiyatServisi.OkunabilirlikUyariEsigi,
            ayarlar.Count(a => a.Aktif));
    }

    /// <summary>
    /// Vitrin görünümü — kaynak kotasyonları + ayarlar → satırlar.
    /// ⚠ Hesap `PanoFiyatServisi.Hesapla` içindedir; burada yeniden yazılmaz.
    /// </summary>
    public async Task<PanoGorunum> Gorunum(string tenantId, CancellationToken iptal = default)
    {
        var kaynakKod = await kur.TenantKaynagi(tenantId);
        var pariteler = kaynakDefteri.Pariteler(kaynakKod);
        var ayarlar = await Oku(tenantId, iptal);
        var sonuc = PanoFiyatServisi.Hesapla(pariteler, ayarlar);
        var ibare = await IscilikIbaresiOku(tenantId, iptal);

        return new PanoGorunum(
            kaynakKod,
            DateTime.UtcNow,
            [.. sonuc.Satirlar.Select(s => new PanoSatirGorunum(s.Kod, s.Etiket, s.Alis, s.Satis, s.Birim))],
            [.. sonuc.Atlananlar.Select(a => new PanoAtlananGorunum(
                a.Kod, MKur.PariteKodlari.Ad(a.Kod), a.Sebep))],
            PanoFiyatServisi.TavanSatir,
            PanoFiyatServisi.OkunabilirlikUyariEsigi,
            OkunabilirlikUyarisi: sonuc.Satirlar.Count > PanoFiyatServisi.OkunabilirlikUyariEsigi,
            IscilikIbaresi: ibare);
    }

    // ── PAKET EKSIK-KUCUK (B2-10) — İŞÇİLİK İBARESİ ─────────────────────────
    // Fiyat Etiketi Yönetmeliği (RG 28.06.2014/29044) md.6: kıymetli taş/maden
    // mallarında etiket zorunlu değildir ama cinslerine göre birim satış fiyatları
    // işyerinde görünür gösterilir ve "Bu satış fiyatlarına işçilik bedelinin dahil
    // olup olmadığı ayrıca belirtilir." HANGİSİNİN (dahil/hariç) doğru olduğu
    // DÜKKÂNIN fiyat politikasıdır → koda sabit yazılmaz, kiracı ayarıdır. Seçilmemişse
    // null döner ve ekran müşteriye dönük tam ekran panoyu AÇMAZ (fail-closed).

    /// <summary>Kayıtlı ibare ("DAHIL" | "HARIC") ya da null. Bozuk değer null sayılır.</summary>
    public async Task<string?> IscilikIbaresiOku(string tenantId, CancellationToken iptal = default)
    {
        var deger = await db.KiraciAyarlari.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.Anahtar == PanoAyarAnahtari.IscilikIbaresi)
            .Select(a => a.Deger).FirstOrDefaultAsync(iptal);
        return IscilikIbaresiDegerleri.Gecerli(deger) ? deger : null;
    }

    /// <summary>
    /// PATCH — gövdede alan YOKSA 400 (sessiz etkisizlik yasak); geçersiz değer 400;
    /// <c>null</c> = satır silinir (tercih satırıdır, finansal varlık değildir).
    /// Denetim izi mevcut servisle yazılır. Yanıt: güncel pano (UX-S23 etkisi aynı ekranda).
    /// </summary>
    public async Task<PanoGorunum> IscilikIbaresiGuncelle(
        IscilikIbaresiPatchDto dto, AktifKullanici islem, CancellationToken iptal = default)
    {
        if (!dto.IscilikIbaresiVerildi)
            throw new BadRequestException("Güncellenecek alan gönderilmedi").AlanIle("iscilikIbaresi");

        var yeni = dto.IscilikIbaresi?.Trim().ToUpperInvariant();
        if (yeni is not null && !IscilikIbaresiDegerleri.Gecerli(yeni))
            throw new BadRequestException("İşçilik ibaresi DAHIL ya da HARIC olmalıdır").AlanIle("iscilikIbaresi");

        var kayit = await db.KiraciAyarlari.FirstOrDefaultAsync(
            a => a.TenantId == islem.TenantId && a.Anahtar == PanoAyarAnahtari.IscilikIbaresi, iptal);

        if (yeni is null)
        {
            if (kayit is not null) db.KiraciAyarlari.Remove(kayit);
        }
        else if (kayit is null)
        {
            db.KiraciAyarlari.Add(new KiraciAyari
            {
                Id = Kimlik.Ulid(),
                TenantId = islem.TenantId,
                Anahtar = PanoAyarAnahtari.IscilikIbaresi,
                Deger = yeni,
                Guncellendi = DateTime.UtcNow,
            });
        }
        else
        {
            kayit.Deger = yeni;
            kayit.Guncellendi = DateTime.UtcNow;
        }

        denetim.YazmaKaydet(islem, Varlik, PanoAyarAnahtari.IscilikIbaresi,
            $"Vitrin işçilik ibaresi: {yeni ?? "kaldırıldı"}", hassas: false);
        await db.SaveChangesAsync(iptal);

        return await Gorunum(islem.TenantId, iptal);
    }

    /// <summary>
    /// PATCH — yalnız VERİLEN alanlar işlenir; <c>null</c> gelen alan
    /// varsayılana döner. Hiç alan gelmezse 400 (sessiz etkisizlik yasak).
    /// </summary>
    public async Task<PanoAyarlariGorunum> Guncelle(
        string kod, PanoAyarPatchDto dto, AktifKullanici islem, CancellationToken iptal = default)
    {
        if (!MKur.PariteKodlari.Gecerli(kod))
            throw new BadRequestException($"Geçersiz parite kodu: {kod}");

        var mevcut = (await Oku(islem.TenantId, iptal)).First(a => a.PariteKodu == kod);
        var varsayilan = Varsayilan(kod, Array.IndexOf([.. Kalemler], kod));

        var yeni = mevcut with
        {
            Etiket = dto.EtiketVerildi
                ? (string.IsNullOrWhiteSpace(dto.Etiket) ? null : dto.Etiket.Trim())
                : mevcut.Etiket,
            Sira = dto.SiraVerildi ? dto.Sira ?? varsayilan.Sira : mevcut.Sira,
            AlisMarjTl = dto.AlisMarjTlVerildi ? dto.AlisMarjTl ?? varsayilan.AlisMarjTl : mevcut.AlisMarjTl,
            SatisMarjTl = dto.SatisMarjTlVerildi ? dto.SatisMarjTl ?? varsayilan.SatisMarjTl : mevcut.SatisMarjTl,
            YuvarlamaAdimi = dto.YuvarlamaAdimiVerildi
                ? dto.YuvarlamaAdimi ?? varsayilan.YuvarlamaAdimi
                : mevcut.YuvarlamaAdimi,
            Aktif = dto.AktifVerildi ? dto.Aktif ?? varsayilan.Aktif : mevcut.Aktif,
        };

        var verildi = dto.EtiketVerildi || dto.SiraVerildi || dto.AlisMarjTlVerildi
                      || dto.SatisMarjTlVerildi || dto.YuvarlamaAdimiVerildi || dto.AktifVerildi;
        if (!verildi)
            throw new BadRequestException("Güncellenecek alan gönderilmedi");

        // Doğrulama YENİDEN YAZILMAZ — saf servisin kapısı çağrılır.
        // P-38 · B-18: kapıya kalemin CANLI KOTASYONU da verilir; marj artık
        // panoya ₺0,00 bastıramaz (QA turu 7 #112). Kotasyon yoksa null gider
        // ve yalnız mutlak tavan uygulanır — uydurma sınır konmaz (Madde 1).
        PanoFiyatServisi.Dogrula(yeni, await Kotasyon(islem.TenantId, kod));

        await Yaz(islem, kod, Diz(yeni), iptal);

        // Denetim izi YENİDEN YAZILMAZ — mevcut servis çağrılır (ADR-0021).
        denetim.YazmaKaydet(islem, Varlik, kod,
            $"Pano kalemi güncellendi ({kod})", hassas: false);
        await db.SaveChangesAsync(iptal);

        return await AyarGorunumu(islem.TenantId, iptal);
    }

    /// <summary>
    /// Kalemi VARSAYILANA döndürür (ayar satırı silinir). Finansal varlık
    /// DEĞİLDİR — bir tercih satırıdır; hard DELETE yasağı bunu kapsamaz.
    /// </summary>
    public async Task<PanoAyarlariGorunum> VarsayilanaDon(
        string kod, AktifKullanici islem, CancellationToken iptal = default)
    {
        if (!MKur.PariteKodlari.Gecerli(kod))
            throw new BadRequestException($"Geçersiz parite kodu: {kod}");

        var kayit = await db.KiraciAyarlari.FirstOrDefaultAsync(
            a => a.TenantId == islem.TenantId && a.Anahtar == PanoAyarAnahtari.Anahtar(kod), iptal);

        if (kayit is null)
            throw new NotFoundException($"Bu kalemin kiracı ayarı yok: {kod}");

        db.KiraciAyarlari.Remove(kayit);
        denetim.YazmaKaydet(islem, Varlik, kod,
            $"Pano kalemi varsayılana döndürüldü ({kod})", hassas: false);
        await db.SaveChangesAsync(iptal);

        return await AyarGorunumu(islem.TenantId, iptal);
    }

    // ── iç ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// P-38 · B-18 — kalemin canlı kotasyonu (yoksa null). Kaynak defteri
    /// bellekte tutar; yeni kiracıda ilk saniyelerde boş dönebilir.
    /// </summary>
    private async Task<MKur.Parite?> Kotasyon(string tenantId, string kod)
    {
        var kaynakKod = await kur.TenantKaynagi(tenantId);
        return kaynakDefteri.Pariteler(kaynakKod)
            .FirstOrDefault(p => string.Equals(p.Kod, kod, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Dictionary<string, string>> Satirlar(string tenantId, CancellationToken iptal)
    {
        var ham = await db.KiraciAyarlari.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.Anahtar.StartsWith(PanoAyarAnahtari.Onek))
            .Select(a => new { a.Anahtar, a.Deger })
            .ToListAsync(iptal);

        var sonuc = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var s in ham)
            if (PanoAyarAnahtari.Kod(s.Anahtar) is { } kod && MKur.PariteKodlari.Gecerli(kod))
                sonuc[kod] = s.Deger;
        return sonuc;
    }

    /// <summary>Varsayılan kalem ayarı — dükkân setindeki 7 kalem AÇIK, kalanlar kapalı.</summary>
    private static PanoKalemAyari Varsayilan(string kod, int sira)
    {
        var varsayilanSira = PanoFiyatServisi.VarsayilanKalemler.ToList().IndexOf(kod);
        return new PanoKalemAyari
        {
            PariteKodu = kod,
            Sira = varsayilanSira >= 0 ? varsayilanSira : 50 + sira,
            Etiket = null,
            AlisMarjTl = 0m,
            SatisMarjTl = 0m,
            YuvarlamaAdimi = PanoFiyatServisi.VarsayilanYuvarlama,
            Aktif = varsayilanSira >= 0,
        };
    }

    /// <summary>Ayar → "sira|alis|satis|yuvarlama|aktif|etiket" (InvariantCulture).</summary>
    public static string Diz(PanoKalemAyari a) => string.Join('|',
        a.Sira.ToString(CultureInfo.InvariantCulture),
        a.AlisMarjTl.ToString(CultureInfo.InvariantCulture),
        a.SatisMarjTl.ToString(CultureInfo.InvariantCulture),
        a.YuvarlamaAdimi.ToString(CultureInfo.InvariantCulture),
        a.Aktif ? "1" : "0",
        (a.Etiket ?? string.Empty).Replace('|', ' '));

    /// <summary>
    /// Ham CSV → ayar. Bozuk/eksik alan VARSAYILANA düşer ve sessizce yutulmaz:
    /// kuyumcu ekranda varsayılan değeri görür ve düzeltebilir (UX-S25).
    /// </summary>
    public static PanoKalemAyari Coz(string kod, string ham, PanoKalemAyari varsayilan)
    {
        var p = ham.Split('|');
        return new PanoKalemAyari
        {
            PariteKodu = kod,
            Sira = Tam(p, 0, varsayilan.Sira),
            AlisMarjTl = Ondalik(p, 1, varsayilan.AlisMarjTl),
            SatisMarjTl = Ondalik(p, 2, varsayilan.SatisMarjTl),
            YuvarlamaAdimi = Ondalik(p, 3, varsayilan.YuvarlamaAdimi),
            Aktif = p.Length > 4 ? p[4] == "1" : varsayilan.Aktif,
            Etiket = p.Length > 5 && !string.IsNullOrWhiteSpace(p[5]) ? p[5] : null,
        };
    }

    private static int Tam(string[] p, int i, int varsayilan) =>
        p.Length > i && int.TryParse(p[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var d)
            ? d : varsayilan;

    private static decimal Ondalik(string[] p, int i, decimal varsayilan) =>
        p.Length > i && decimal.TryParse(p[i], NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
            ? d : varsayilan;

    private async Task Yaz(AktifKullanici islem, string kod, string deger, CancellationToken iptal)
    {
        var anahtar = PanoAyarAnahtari.Anahtar(kod);
        var kayit = await db.KiraciAyarlari
            .FirstOrDefaultAsync(a => a.TenantId == islem.TenantId && a.Anahtar == anahtar, iptal);

        if (kayit is null)
        {
            db.KiraciAyarlari.Add(new KiraciAyari
            {
                Id = Kimlik.Ulid(),
                TenantId = islem.TenantId,
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

// ── Controller ───────────────────────────────────────────────────────────────

/// <summary>
/// Vitrin panosu uçları.
/// <para>
/// ⚠ MODÜL KAPISI "kur"dur, yeni bir yetki anahtarı AÇILMADI: pano kur
/// kotasyonunun türevidir, kuru göremeyen kullanıcı panoyu da göremez.
/// Yeni anahtar yetki matrisini genişletir ve her kiracıda elle açılmayı
/// bekleyen ölü bir satır bırakırdı (P-09 mirası).
/// </para>
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/pano")]
[Yetki.ModulYetkisi("kur")]
public sealed class PanoController(PanoAyarServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/pano/guncel — vitrine basılacak satırlar (marj İÇERMEZ).</summary>
    [HttpGet("guncel")]
    public Task<PanoGorunum> Guncel(CancellationToken iptal) => servis.Gorunum(Islem.TenantId, iptal);

    /// <summary>GET /api/v1/pano/ayarlar — marj/yuvarlama ayarları — MUDUR+ (marj ticari sırdır).</summary>
    [HttpGet("ayarlar")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<PanoAyarlariGorunum> Ayarlar(CancellationToken iptal) =>
        servis.AyarGorunumu(Islem.TenantId, iptal);

    /// <summary>PATCH /api/v1/pano/ayarlar/{kod} — tek kalemi güncelle — MUDUR+.</summary>
    [HttpPatch("ayarlar/{kod}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<PanoAyarlariGorunum> Guncelle(string kod, [FromBody] PanoAyarPatchDto dto, CancellationToken iptal) =>
        servis.Guncelle(kod, dto, Islem, iptal);

    /// <summary>POST /api/v1/pano/ayarlar/{kod}/varsayilana-don — ayarı kaldır — MUDUR+.</summary>
    [HttpPost("ayarlar/{kod}/varsayilana-don")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<PanoAyarlariGorunum> VarsayilanaDon(string kod, CancellationToken iptal) =>
        servis.VarsayilanaDon(kod, Islem, iptal);

    /// <summary>
    /// PATCH /api/v1/pano/iscilik-ibaresi — vitrin "işçilik dahildir/hariçtir" ibaresi — MUDUR+.
    /// PAKET EKSIK-KUCUK (B2-10) · Fiyat Etiketi Yönetmeliği md.6.
    /// </summary>
    [HttpPatch("iscilik-ibaresi")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<PanoGorunum> IscilikIbaresi([FromBody] IscilikIbaresiPatchDto dto, CancellationToken iptal) =>
        servis.IscilikIbaresiGuncelle(dto, Islem, iptal);
}
