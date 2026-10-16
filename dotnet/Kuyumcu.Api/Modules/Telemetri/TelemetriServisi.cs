using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Telemetri;

// ═════════════════════════════════════════════════════════════════════════════
// TELEMETRİ SERVİSLERİ — PAKET P-28 (F-4 · R-2).
//
// İKİ SINIF, İKİ KAPSAM (P-27 kalıbının AYNISI, yeni desen yok):
//   • TelemetriServisi        → istek kapsamı, kiracı KENDİ ölçümünü okur.
//   • TelemetriSistemServisi  → SistemKapsami; operatör tüm kiracıları okur
//     ve destek dakikası kaydını YAZAR/DÜZELTİR/PASİFE ALIR.
//
// ⚠ ÖLÇÜLMEYEN RAKAM ÜRETİLMEZ (FAIL-CLOSED 1): hiçbir alan tahminle
//   doldurulmaz. Ölçüm yoksa dönen değer 0 değil, "veri yok" anlamına gelen
//   <c>OlcumVarMi=false</c>'tur; F-8 belgesi bu bayrağa bakar.
//
// ⚠ TL YOK (K-16): burada saatlik ücret/tutar hesabı YAPILMAZ. Servis MİKTAR
//   döndürür (dakika, istek, bayt, satır); paraya çevirmek kurucunun kalemidir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir kiracının ölçülmüş kaynak tüketimi (belirli bir pencere için).</summary>
public sealed record TelemetriOzetDto(
    string TenantId,
    DateTime BaslangicUtc,
    DateTime BitisUtc,
    int GunSayisi,
    bool OlcumVarMi,
    int PencereSayisi,
    long IstekSayisi,
    long HataSayisi,
    long IslemMs,
    long YanitBayt,
    long GunlukOrtalamaIstek,
    long GunlukOrtalamaIslemMs);

/// <summary>Kiracının ölçülmüş satır yükü — depolama payının ölçülen vekili.</summary>
/// <param name="Tablolar">Ölçülen tablo → satır sayısı. Ölçülen tablolar YAZILIDIR; tahmin yok.</param>
public sealed record DepolamaOzetDto(long ToplamSatir, IReadOnlyDictionary<string, long> Tablolar);

/// <summary>Bir destek dakikası kaydı.</summary>
public sealed record DestekDakikasiDto(
    string Id, string TenantId, DateTime Tarih, int Dakika, string Kanal, string Konu,
    string? Aciklama, string Kaynak, bool Aktif, string OperatorAdi,
    DateTime Olusturuldu, DateTime Guncellendi);

/// <summary>Destek dakikası toplamı — F-8 destek satırının TEK meşru kaynağı.</summary>
public sealed record DestekOzetDto(
    bool OlcumVarMi, int KayitSayisi, long ToplamDakika, int AySayisi, double AylikOrtalamaDakika);

/// <summary>Kiracının kendi telemetri görünümü (salt okunur).</summary>
public sealed record KiraciTelemetriDto(
    TelemetriOzetDto Kullanim,
    DestekOzetDto Destek,
    string DegismezIpucu);

public sealed class TelemetriServisi(KuyumcuDbContext db)
{
    /// <summary>Özet penceresi tavanı (gün). UX-S29: sınır EKRANDA yazılır.</summary>
    public const int GunTavani = 365;

    /// <summary>Varsayılan pencere (gün).</summary>
    public const int VarsayilanGun = 30;

    /// <summary>
    /// Ölçüm satırları DEĞİŞTİRİLEMEZ: otomatik sayaçtır, elle düzeltilirse
    /// birim ekonomi uydurma olur. Düzeltilebilir olan DESTEK DAKİKASIdır.
    /// </summary>
    public const string OlcumDegismezIpucu =
        "Kullanım ölçümleri sunucu tarafından otomatik sayılır ve elle değiştirilemez; "
        + "yanlış olduğunu düşünüyorsanız destek kaydı açın. Düzeltilebilen kayıt "
        + "destek dakikasıdır (operatör ekranından düzenlenir veya pasife alınır).";

    /// <summary>Gün parametresini sözleşmeye sıkıştırır (0/negatif → varsayılan).</summary>
    public static int GunNormalle(int? gun) =>
        gun is null or <= 0 ? VarsayilanGun : Math.Min(gun.Value, GunTavani);

    public async Task<KiraciTelemetriDto> KendiOzetim(AktifKullanici islem, int? gun)
    {
        var g = GunNormalle(gun);
        var kullanim = await Ozet(db, islem.TenantId, g, DateTime.UtcNow);
        var destek = await DestekOzet(db, islem.TenantId, g, DateTime.UtcNow);
        return new KiraciTelemetriDto(kullanim, destek, OlcumDegismezIpucu);
    }

    // ── ORTAK OKUMA ÇEKİRDEĞİ (iki servis de bunu kullanır — iki kopya kural yok) ──

    internal static async Task<TelemetriOzetDto> Ozet(
        KuyumcuDbContext hedef, string tenantId, int gun, DateTime now)
    {
        var bitis = TelemetriToplayici.Pencere(now).AddHours(1);
        var baslangic = bitis.AddDays(-gun);

        // ⚠ B-8 (sınırsız ToListAsync): pencere satırları İSTEMCİYE ÇEKİLMEZ.
        //   Toplama SUNUCUDA yapılır (P-25 dersi) — 100 kiracı × 30 gün × 24
        //   saat = 72.000 satırı belleğe almak kapasite ölçen aracın kendisini
        //   kapasite sorununa çevirirdi. `.Take(...)` ile kırpmak ise ölçümü
        //   SESSİZCE eksiltirdi (Anayasa md.1): toplam yanlış çıkardı.
        var toplam = await hedef.Set<TelemetriOlcumu>().AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.OlcumSaati >= baslangic && o.OlcumSaati < bitis)
            .GroupBy(o => 1)
            .Select(g => new
            {
                Pencere = g.Count(),
                Istek = (long)g.Sum(o => (long)o.IstekSayisi),
                Hata = (long)g.Sum(o => (long)o.HataSayisi),
                Ms = g.Sum(o => o.IslemMs),
                Bayt = g.Sum(o => o.YanitBayt),
            })
            .FirstOrDefaultAsync();

        int pencere = toplam?.Pencere ?? 0;
        long istek = toplam?.Istek ?? 0;
        long hata = toplam?.Hata ?? 0;
        long ms = toplam?.Ms ?? 0;
        long bayt = toplam?.Bayt ?? 0;

        return new TelemetriOzetDto(
            tenantId, baslangic, bitis, gun,
            OlcumVarMi: pencere > 0,
            PencereSayisi: pencere,
            IstekSayisi: istek, HataSayisi: hata, IslemMs: ms, YanitBayt: bayt,
            GunlukOrtalamaIstek: gun > 0 ? istek / gun : 0,
            GunlukOrtalamaIslemMs: gun > 0 ? ms / gun : 0);
    }

    internal static async Task<DestekOzetDto> DestekOzet(
        KuyumcuDbContext hedef, string tenantId, int gun, DateTime now)
    {
        // ⚠ B-62 (bitiş günü off-by-one): üst sınır TEK KAYNAKTAN gelir —
        //   `Mutabakat.UstSinirKapsayici` gün başlangıcını ertesi güne taşır,
        //   böylece BUGÜN girilen destek kaydı pencereye DÂHİLDİR. Sınır adı
        //   `ustSinirHaric`tir: karşılaştırma "<" ile yapılır ve bu bilinçlidir.
        var ustSinirHaric = Rapor.Mutabakat.UstSinirKapsayici(now.Date)!.Value;
        var baslangic = ustSinirHaric.AddDays(-gun);

        // ⚠ B-8: satırlar çekilmez, SUNUCUDA toplanır. `.Take(...)` ile
        //   kırpmak destek dakikasını EKSİK gösterirdi ve F-8'in tek meşru
        //   destek kaynağı yanlış rakam üretirdi.
        var toplama = await hedef.Set<DestekDakikasi>().AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.Aktif
                        && d.Tarih >= baslangic && d.Tarih < ustSinirHaric)
            .GroupBy(d => 1)
            .Select(g => new { Adet = g.Count(), Dakika = (long)g.Sum(d => (long)d.Dakika) })
            .FirstOrDefaultAsync();

        int kayit = toplama?.Adet ?? 0;
        long toplam = toplama?.Dakika ?? 0;
        // Ay sayısı ÖLÇÜLEN pencereden gelir; 30 güne yuvarlanır ve en az 1'dir.
        var ay = Math.Max(1, (int)Math.Round(gun / 30.0, MidpointRounding.AwayFromZero));
        return new DestekOzetDto(
            OlcumVarMi: kayit > 0,
            KayitSayisi: kayit,
            ToplamDakika: toplam,
            AySayisi: ay,
            AylikOrtalamaDakika: kayit == 0 ? 0 : Math.Round((double)toplam / ay, 2));
    }

    /// <summary>
    /// DEPOLAMA VEKİLİ — ölçülen tablolar YAZILIDIR.
    /// <para>⚠ Bayt DEĞİL SATIR ölçülür: bayt ancak <c>information_schema</c>
    /// ile okunabilir ve bu depoda ham SQL deseni YOKTUR (yeni desen açmamak
    /// için açılmadı). Satır başına bayt F-8'de canlı ölçümle çarpılır ve o
    /// ölçümün komutu belgede yazılıdır — kod uydurma bayt üretmez.</para>
    /// </summary>
    internal static async Task<DepolamaOzetDto> Depolama(KuyumcuDbContext hedef, string tenantId)
    {
        var tablolar = new Dictionary<string, long>
        {
            ["hareketler"] = await hedef.Hareketler.AsNoTracking().LongCountAsync(x => x.TenantId == tenantId),
            ["urunler"] = await hedef.Urunler.AsNoTracking().LongCountAsync(x => x.TenantId == tenantId),
            ["stoklar"] = await hedef.Stoklar.AsNoTracking().LongCountAsync(x => x.TenantId == tenantId),
            ["cariler"] = await hedef.Cariler.AsNoTracking().LongCountAsync(x => x.TenantId == tenantId),
            ["kasa_hareketleri"] = await hedef.KasaHareketleri.AsNoTracking().LongCountAsync(x => x.TenantId == tenantId),
            ["faturalar"] = await hedef.Faturalar.AsNoTracking().LongCountAsync(x => x.TenantId == tenantId),
            ["fatura_kalemleri"] = await hedef.FaturaKalemleri.AsNoTracking().LongCountAsync(x => x.TenantId == tenantId),
            ["denetim_kayitlari"] = await hedef.DenetimKayitlari.AsNoTracking().LongCountAsync(x => x.TenantId == tenantId),
        };
        return new DepolamaOzetDto(tablolar.Values.Sum(), tablolar);
    }
}
