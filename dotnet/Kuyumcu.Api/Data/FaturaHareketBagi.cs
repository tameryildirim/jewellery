using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// FATURA ↔ HAREKET BAĞI — TEK SORGU YOLU (paket P-08 / Y-25, BK-04).
//
// ADR: dokumanlar/02-mimari/adr/0018-fatura-hareket-bagi-tek-yon.md
//
// İLİŞKİNİN YÖNÜ: 1 fatura ↔ N hareket. OTORİTE `fatura_kalemleri.hareket_id`
// kolonudur (belge satırı hangi defter satırına dayanıyor). Ters yönde
// `hareketler.fatura_id` diye bir kolon YOKTUR ve AÇILMAZ — iki paralel bağ
// olsaydı mutabakat aynı satışı iki kez saymak zorunda kalırdı.
//
// ⚠ `hareketler.fatura_no` EMEKLİYE AYRILDI: yeni okuma yolu yazılmaz. İki
//   ayrı nedenden dolayı yalnız GERİYE DÖNÜK olarak okunur:
//     1. Serbest METİNDİR — fatura silinse/iptal edilse de damga satırda kalır;
//        bağ değil, KOPYA'dır.
//     2. AŞIRI YÜKLÜDÜR — aynı kolon HURDA_ALIS satırlarında bizim fatura
//        numaramızı değil TEDARİKÇİNİN fatura numarasını taşır
//        (SatisModulu: `FaturaNo = dto.Satici.TedarikciFaturaNo`). Bu iki anlam
//        tek kolonda durduğu sürece "bu satış faturalı mı" sorusunun cevabı
//        kolondan OKUNAMAZ.
//   Bu yüzden damga yolu bu dosyanın İÇİNDE, Kaynak = "DAMGA" etiketiyle ve
//   YALNIZ tür süzgecinden geçmiş küme üzerinde çalışır; dışarıda kullanılmaz.
//
// ⚠ ÇİFTE SAYMA YOK: sonuç hareket kimliğine göre tekilleştirilmiştir; bir
//   hareket hem kalemle hem damgayla eşleşirse KALEM kazanır (otorite).
// ═════════════════════════════════════════════════════════════════════════════

/// <param name="HareketId">Defter satırı.</param>
/// <param name="FaturaId">Belge.</param>
/// <param name="FaturaNo">Belgenin numarası — GÖSTERİM içindir, bağ değildir.</param>
/// <param name="Kaynak">Bağ nereden okundu: KALEM (otorite) · DAMGA (emekli yol).</param>
public sealed record FaturaBagi(string HareketId, string FaturaId, string FaturaNo, string Kaynak);

public static class FaturaHareketBagi
{
    /// <summary>Bağ `fatura_kalemleri.hareket_id` kolonundan okundu — OTORİTE.</summary>
    public const string KaynakKalem = "KALEM";

    /// <summary>Bağ `hareketler.fatura_no` damgasından okundu — EMEKLİ yol, yalnız eski satırlar.</summary>
    public const string KaynakDamga = "DAMGA";

    /// <summary>
    /// Bağ TASLAK bir belgenin işaretinden okundu (PAKET P-22 / B-3).
    /// İşaret <c>TASLAK:{faturaId}</c> biçimindedir, seri numarası DEĞİLDİR.
    /// </summary>
    public const string KaynakTaslak = "TASLAK";

    /// <summary>
    /// TEK SORGU YOLU — verilen hareketlerin, verilen FATURA KÜMESİ içindeki
    /// karşılıklarını çözer.
    ///
    /// <para>
    /// ⚠ Fatura kümesi çağırandan gelir ve GENİŞLETİLMEZ: gün sonu mutabakatı
    /// "o günün faturaları" sorusunu sorar; burada tüm faturalara bakılsaydı
    /// ertesi gün kesilen fatura o günün raporunu sessizce değiştirirdi.
    /// </para>
    /// </summary>
    /// <returns>hareket kimliği → bağ. Bağsız hareket sözlükte YOKTUR.</returns>
    public static async Task<Dictionary<string, FaturaBagi>> Coz(
        KuyumcuDbContext db,
        string tenantId,
        IReadOnlyCollection<Hareket> hareketler,
        IReadOnlyCollection<Fatura> faturaKumesi)
    {
        var sonuc = new Dictionary<string, FaturaBagi>(StringComparer.Ordinal);
        if (hareketler.Count == 0 || faturaKumesi.Count == 0) return sonuc;

        var hareketIdler = hareketler.Select(h => h.Id).Distinct().ToList();
        var faturaIdler = faturaKumesi.Select(f => f.Id).ToHashSet(StringComparer.Ordinal);
        // P-22: numarası olmayan (TASLAK) belge damga sözlüğüne GİRMEZ.
        var faturaNolar = faturaKumesi.Where(f => f.FaturaNo is not null)
            .ToDictionary(f => f.FaturaNo!, f => f, StringComparer.Ordinal);

        // ── 1) OTORİTE: fatura_kalemleri.hareket_id ─────────────────────────
        var kalemler = await db.FaturaKalemleri.AsNoTracking()
            .Where(k => k.TenantId == tenantId
                     && k.HareketId != null
                     && hareketIdler.Contains(k.HareketId))
            .Select(k => new { k.HareketId, k.FaturaId })
            .ToListAsync();

        foreach (var k in kalemler)
        {
            if (k.HareketId is null || !faturaIdler.Contains(k.FaturaId)) continue;
            var fatura = faturaKumesi.First(f => f.Id == k.FaturaId);
            sonuc[k.HareketId] = new FaturaBagi(k.HareketId, fatura.Id, fatura.FaturaNo ?? "", KaynakKalem);
        }

        // ── 2) EMEKLİ YOL: hareketler.fatura_no damgası ─────────────────────
        // Bu blok, kalemli fatura yolundan ÖNCE kesilmiş belgeler içindir.
        // Kod tabanında `hareketler.fatura_no` üzerinden bağ kuran BAŞKA bir
        // yer kalmamıştır (denetim.sh P-08: FATURA_NO_SERBEST_METIN).
        foreach (var h in hareketler)
        {
            if (sonuc.ContainsKey(h.Id)) continue;                 // KALEM kazandı
            if (string.IsNullOrEmpty(h.FaturaNo)) continue;

            // ── 2b) TASLAK İŞARETİ (P-22 / B-3) ────────────────────────────
            // Taslağın numarası YOKTUR; bağ belge KİMLİĞİ üzerinden kurulur.
            // Bu blok olmasaydı, taslağı olan bir satış mutabakatta "faturasız"
            // görünürdü — oysa tek değişen numaranın ne zaman verildiğidir.
            if (Modules.Fatura.FaturaSeriKapisi.TaslakIsareti(h.FaturaNo))
            {
                var taslakId = h.FaturaNo![Modules.Fatura.FaturaSeriKapisi.TaslakOnEki.Length..];
                var taslak = faturaKumesi.FirstOrDefault(f => f.Id == taslakId);
                if (taslak is not null)
                    sonuc[h.Id] = new FaturaBagi(h.Id, taslak.Id, taslak.FaturaNo ?? h.FaturaNo, KaynakTaslak);
                continue;
            }

            if (!faturaNolar.TryGetValue(h.FaturaNo, out var fatura)) continue;
            sonuc[h.Id] = new FaturaBagi(h.Id, fatura.Id, fatura.FaturaNo!, KaynakDamga);
        }

        return sonuc;
    }

    /// <summary>
    /// "Bu hareket/fiş ZATEN faturalanmış mı" sorusunun TEK cevabı
    /// (FaturaModulu damga kontrolü).
    ///
    /// <para>
    /// ⚠ FAIL-CLOSED ve eski kuraldan KESİNLİKLE DAHA GENİŞTİR — bu bilinçlidir.
    /// İki şarttan biri yeterlidir:
    ///   (a) İPTAL EDİLMEMİŞ bir faturanın kalemi bu harekete bağlıdır (YENİ —
    ///       eski hâlde kalemi bağlı ama damgası düşmüş satır İKİNCİ KEZ
    ///       faturalanabiliyordu), ya da
    ///   (b) hareket bir <c>fatura_no</c> damgası taşır (ESKİ kural, KORUNDU).
    /// </para>
    /// <para>
    /// ⚠ (b)'de faturanın VARLIĞI aranmaz. Damga duruyor ama karşılığında
    /// <c>faturalar</c> satırı bulunamıyorsa ortada bir tutarsızlık vardır ve
    /// doğru davranış "yeniden faturala" DEĞİL, DURUP sebebi sormaktır. Bu
    /// kontrolü "fatura kaydı yoksa serbest" diye gevşetmek, iptal edilmiş ya
    /// da silinmiş bir belgenin satışını sessizce ikinci kez faturalatırdı.
    /// </para>
    /// </summary>
    /// <returns>Bulunan ilk bağ; yoksa <c>null</c>.</returns>
    public static async Task<FaturaBagi?> IlkBag(
        KuyumcuDbContext db, string tenantId, IReadOnlyCollection<Hareket> kume)
    {
        if (kume.Count == 0) return null;

        var faturalar = await GecerliFaturalar(db, tenantId, kume);
        var bag = await Coz(db, tenantId, kume, faturalar);

        foreach (var h in kume)
        {
            if (bag.TryGetValue(h.Id, out var b)) return b;

            if (string.IsNullOrEmpty(h.FaturaNo)) continue;

            // (c) TASLAK İŞARETİ (P-22 / B-3) — numara YOK, belge var. Bu bir
            //     tutarsızlık DEĞİLDİR; hareket açık bir taslağa bağlıdır.
            //     Mesaj "fatura kaydı bulunamadı" demek yerine doğruyu söyler.
            if (Modules.Fatura.FaturaSeriKapisi.TaslakIsareti(h.FaturaNo))
            {
                var taslakId = h.FaturaNo![Modules.Fatura.FaturaSeriKapisi.TaslakOnEki.Length..];
                var taslakVar = await db.Faturalar.AsNoTracking()
                    .AnyAsync(f => f.Id == taslakId && f.TenantId == tenantId && f.Durum != "IPTAL");
                return taslakVar
                    ? new FaturaBagi(h.Id, taslakId, h.FaturaNo, KaynakTaslak)
                    // İşaret duruyor ama taslak yok/iptal: karşılıksız damga kuralı.
                    : new FaturaBagi(h.Id, FaturaBulunamadi, h.FaturaNo, KaynakDamga);
            }

            // (b) KARŞILIKSIZ DAMGA — faturası bulunamadı ama damga duruyor.
            return new FaturaBagi(h.Id, FaturaBulunamadi, h.FaturaNo, KaynakDamga);
        }

        return null;
    }

    /// <summary>
    /// Damgası duran ama <c>faturalar</c> karşılığı bulunamayan bağın fatura
    /// kimliği. Boş dizge DEĞİL, ADI OLAN bir işaret: "bilinmiyor"u sessiz
    /// geçmemek için (Anayasa md.1).
    /// </summary>
    public const string FaturaBulunamadi = "FATURA-KAYDI-BULUNAMADI";

    /// <summary>
    /// PAKET P-22 / B-3 — TASLAK İŞARETİ TAŞIYAN HAREKETLERİ getirir.
    ///
    /// ⚠ NEDEN BURADA: <c>hareketler.fatura_no</c> metnini sorgulayan TEK YOL bu
    /// dosyadır (ADR-0018, denetim.sh P-08: FATURA_NO_SERBEST_METIN). Seri
    /// kapısı damgayı yazar ama SORGUYU buradan ister; iki ayrı sorgu yolu
    /// açılsaydı "bu hareket hangi belgeye bağlı" sorusunun iki cevabı olurdu.
    /// <para>⚠ <c>SaveChangesAsync</c> ÇAĞIRMAZ.</para>
    /// </summary>
    public static Task<List<Hareket>> IsaretliHareketler(
        KuyumcuDbContext db, string tenantId, string isaret) =>
        db.Hareketler.Where(h => h.TenantId == tenantId && h.FaturaNo == isaret).ToListAsync();

    /// <summary>
    /// HENÜZ FATURALANMAMIŞ hareketler süzgeci — TEK SORGU YOLU'nun sunucu
    /// tarafı (fatura kesme ekranının aday listesi).
    ///
    /// <para>
    /// ⚠ Eski hâl yalnız <c>h.FaturaNo == null</c> diyordu; kalemi bir faturaya
    /// bağlı ama damgası düşmüş satır aday listesinde YENİDEN görünüyordu.
    /// Otorite burada da <c>fatura_kalemleri.hareket_id</c>'dir; damga koşulu
    /// eski satırlar için İKİNCİ şart olarak durur ve bu dosyanın DIŞINA çıkmaz.
    /// </para>
    /// </summary>
    public static IQueryable<Hareket> Bagsiz(
        KuyumcuDbContext db, string tenantId, IQueryable<Hareket> kaynak) =>
        kaynak.Where(h => h.FaturaNo == null
                       && !db.FaturaKalemleri.Any(k => k.TenantId == tenantId
                                                    && k.HareketId == h.Id
                                                    && db.Faturalar.Any(f => f.Id == k.FaturaId
                                                                          && f.Durum != "IPTAL")));

    /// <summary>
    /// Kümeyi ilgilendiren, İPTAL EDİLMEMİŞ faturalar. Damga yolu için numaraya,
    /// kalem yolu için kimliğe bakılır; ikisi de tek sorguda toplanır.
    /// </summary>
    private static async Task<List<Fatura>> GecerliFaturalar(
        KuyumcuDbContext db, string tenantId, IReadOnlyCollection<Hareket> kume)
    {
        var hareketIdler = kume.Select(h => h.Id).Distinct().ToList();
        var damgalar = kume.Where(h => !string.IsNullOrEmpty(h.FaturaNo))
                           .Select(h => h.FaturaNo!).Distinct().ToList();
        // P-22 / B-3: taslak işaretleri belge KİMLİĞİ taşır, numara değil.
        var taslakIdler = damgalar
            .Where(Modules.Fatura.FaturaSeriKapisi.TaslakIsareti)
            .Select(d => d[Modules.Fatura.FaturaSeriKapisi.TaslakOnEki.Length..])
            .ToList();

        var kalemFaturaIdler = await db.FaturaKalemleri.AsNoTracking()
            .Where(k => k.TenantId == tenantId && k.HareketId != null && hareketIdler.Contains(k.HareketId))
            .Select(k => k.FaturaId)
            .Distinct()
            .ToListAsync();

        return await db.Faturalar.AsNoTracking()
            .Where(f => f.TenantId == tenantId
                     && f.Durum != "IPTAL"
                     && (kalemFaturaIdler.Contains(f.Id)
                     || taslakIdler.Contains(f.Id)
                     || (f.FaturaNo != null && damgalar.Contains(f.FaturaNo))))
            .ToListAsync();
    }
}
