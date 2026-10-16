using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Satis;

// ═════════════════════════════════════════════════════════════════════════════
// STORNO SÜZGECİ — paket 48 Faz A.
//
// NEDEN VAR: storno bir HATA DÜZELTMESİDİR; kaynak satır ile ters satırın
// birlikte net etkisi SIFIRDIR. Kümülatif eşik hesapları (459 s. VUK tevsik
// günlük nakit toplamı, MASAK bağlantılı işlem toplamı) bu çifti görmeye
// devam ederse, yanlış girilmiş ve düzeltilmiş bir işlem müşterinin haddini
// İKİNCİ KEZ doldurur — kimse ikinci işlemi neden yapamadığını anlayamaz.
//
// ⚠ ÇİFT DIŞLANIR, TEK TARAF DEĞİL: yalnız "storno_kaynak_id dolu olanı at"
// denseydi kümülatif ŞİŞMEZ ama AZALIR (kaynak sayılmaya devam ederken ters
// satır düşerdi → eksi yönde sapma yok; tersi 'yalnız kaynağı at' denseydi
// ters satır tek başına sayılırdı). İkisi birden dışlanır (Mehmet koşulu).
// ═════════════════════════════════════════════════════════════════════════════

public static class StornoSuzgeci
{
    /// <summary>
    /// Storno ÇİFTİNİ (hükümsüz kılınan kaynak + onu hükümsüz kılan ters satır)
    /// sorgudan dışlar. Süzgeç yalnız aynı kiracının satırlarına bakar.
    /// </summary>
    public static IQueryable<Hareket> StornoCiftiniDisla(
        this IQueryable<Hareket> sorgu, KuyumcuDbContext db, string tenantId)
    {
        var stornolananlar = db.Hareketler
            .Where(s => s.TenantId == tenantId && s.StornoKaynakId != null)
            .Select(s => s.StornoKaynakId!);

        return sorgu.Where(h => h.StornoKaynakId == null && !stornolananlar.Contains(h.Id));
    }

    /// <summary>
    /// PAKET P-12 (B-42) — BANKA tarafının simetriği. Dekont stornolanabilir
    /// hâle geldiği an bu süzgeç ZORUNLU oldu: MASAK kümülatifi
    /// (<c>MasakKapisi.GunlukIslemToplam</c>) banka_hareketleri'ni
    /// <c>hareket_id IS NULL</c> ile okur; storno satırının hareket_id'si de
    /// null olduğu için, süzgeç olmasaydı 100.000 TL'lik bir hatanın stornosu
    /// müşterinin günlük toplamını 200.000 TL göstererek eşiği SAHTE olarak
    /// doldururdu. Defter tarafında bu kural ilk günden vardı; banka tarafında
    /// bugüne dek storno olmadığı için gerekmiyordu.
    /// </summary>
    public static IQueryable<BankaHareket> StornoCiftiniDisla(
        this IQueryable<BankaHareket> sorgu, KuyumcuDbContext db, string tenantId)
    {
        var stornolananlar = db.BankaHareketleri
            .Where(s => s.TenantId == tenantId && s.StornoKaynakId != null)
            .Select(s => s.StornoKaynakId!);

        return sorgu.Where(h => h.StornoKaynakId == null && !stornolananlar.Contains(h.Id));
    }

    /// <summary>
    /// PAKET Q10-1 — KASA tarafının simetriği. Rapor özetlerinin "kasaya giren
    /// TL" toplamı (aylık özet, gün sonu mutabakatı) tek yönlü süzüyordu
    /// (<c>tutar &gt; 0</c>): gider stornosu (+) TAHSİLAT sayılıyor, tahsilat
    /// stornosu (−) ise düşülmüyordu. Çift birlikte dışlanır (Mehmet koşulu).
    /// </summary>
    public static IQueryable<KasaHareket> StornoCiftiniDisla(
        this IQueryable<KasaHareket> sorgu, KuyumcuDbContext db, string tenantId)
    {
        var stornolananlar = db.KasaHareketleri
            .Where(s => s.TenantId == tenantId && s.StornoKaynakId != null)
            .Select(s => s.StornoKaynakId!);

        return sorgu.Where(h => h.StornoKaynakId == null && !stornolananlar.Contains(h.Id));
    }
}
