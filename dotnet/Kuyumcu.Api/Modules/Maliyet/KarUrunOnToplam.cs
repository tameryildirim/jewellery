using System.Collections.Concurrent;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Maliyet;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET RAPOR-4 (KAR-HIZ) — ÜRÜN BAZLI KÂRIN AYLIK ÖN-TOPLAMI (bellek içi, doğrulamalı)
//
// SORUN (OLCEK-YUK ölçümü, büyük kiracı 50.000 ürün / 180.000 satış, 3 yıl):
//   /raporlar/kar-zarar/urun her istekte dönemin TÜM satış ve parti satırlarını
//   belleğe alıp kârı ürün ürün hesaplıyordu → p95 1,4–4,4 sn. SQL'de iki katlı
//   toplama (hareket başına parti toplamı → ürün toplamı) aynı veride 2 sn sürdü
//   (ölçüldü), yani sorunu SQL'e taşımak çözmüyor.
//
// ÇÖZÜM: kapanmış (içinde bulunulan aydan ÖNCEKİ) her TAM ay için ürün başına
//   "kova" bir kez hesaplanır ve kiracı başına bellekte tutulur. İstek yalnız
//   (a) kapanmış ayların kovalarını toplar, (b) ay başı/sonu kısmi parçaları ve
//   içinde bulunulan ayı CANLI hesaplar, (c) sayfadaki ürünlerin ayrıntısını eski
//   bellek hesabından (UrunSatirlari) geçirir.
//
// KOVA = hareket başına "maliyeti bilinen" kuralının (MaliyetiBilinenler) KART'tan
//   BAĞIMSIZ hâli: K = tüm parti satırlarında maliyet var ve adet karşılanmış;
//   N = adet karşılanmış ama boş maliyetli satır var (kart maliyeti > 0 ise
//   bilinir, değilse eksik); U = parti satırı yok ya da adet karşılanmamış (her
//   zaman eksik). Kart maliyeti istek anında uygulanır → kart değişince önbellek
//   bozulmaz.
//
// EŞDEĞERLİK (eski yolla AYNI sonuç — RaporDortTestleri RD-KH*):
//   Eski hesap parti satırlarını İSTEK PENCERESİNDEN süzer; ay kovası ise ayın
//   hareketlerinin TÜM parti satırlarını kimlikle bağlar. İkisi ancak satırların
//   hepsi pencere içindeyse aynıdır → her ay kovası satırlarının en erken / en geç
//   tarihini, şube ve ürün uyumsuzluğunu taşır; pencere dışına taşan, şube süzgeçli
//   istekte şubesi uyumsuz ya da ürünü uyumsuz ay CANLI hesaplanır (fail-closed).
//
// GEÇERSİZLEŞTİRME: defter ve parti satırları yalnız EKLENİR (append-only;
//   HareketFabrikasi, MaliyetMotoru). Kapanmış ayı değiştirebilecek tek olaylar:
//   (1) tarihi kapanmış aya düşen yeni satış/iade satırı, (2) storno satırı
//   (kaynağı dışlanır). Her istek, son doğrulamadan bu yana OLUŞTURULAN bu tür
//   satırları (tenant_id, olusturuldu) indeksiyle arar; bulduğu ayları atar.
//   10 dakikalık güvenlik payı uzun işlemlerin geç commit'ini yakalar.
//   Kur kaydı harekete bağlıysa silinmez, fiyatı değişmez (KurModulu) → kursuz
//   sayısı kararlıdır. Ham SQL ile satır SİLMEK/DEĞİŞTİRMEK (bakım) önbelleği
//   görmez → süreç yeniden başlatılır ya da Temizle() çağrılır (rapor notu).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir (şube, ürün, dönem parçası) için kart maliyetinden bağımsız kâr kovası.</summary>
public sealed class UrunKovasi
{
    public int HareketSayisi;
    public int KAdet; public decimal KTl; public decimal KMal;
    public int NAdet; public decimal NTl; public decimal NMal; public decimal NBosAdet;
    public int UAdet;
    public decimal AbsBilinen; public decimal AbsBos;
    public int Kursuz;

    public void Ekle(UrunKovasi d)
    {
        HareketSayisi += d.HareketSayisi;
        KAdet += d.KAdet; KTl += d.KTl; KMal += d.KMal;
        NAdet += d.NAdet; NTl += d.NTl; NMal += d.NMal; NBosAdet += d.NBosAdet;
        UAdet += d.UAdet;
        AbsBilinen += d.AbsBilinen; AbsBos += d.AbsBos;
        Kursuz += d.Kursuz;
    }

    /// <summary>Kart maliyeti uygulanmış TL kârı — eski UrunSatirlari ile AYNI kural (R2 bileşen, null = hesaplanamadı).</summary>
    public decimal? Kar(decimal? kart)
    {
        var kartVar = kart is > 0m;
        var bilinenSayisi = KAdet + (kartVar ? NAdet : 0);
        if (bilinenSayisi == 0) return null;
        var bilinenHasilat = KTl + (kartVar ? NTl : 0m);
        var maliyet = KMal + (kartVar ? NMal + kart!.Value * NBosAdet : 0m);
        return KarZararServisi.R2(bilinenHasilat) - KarZararServisi.R2(maliyet);
    }
}

public sealed record OnToplamHareket(string Id, string Tur, string UrunId, string SubeId, decimal TlTutari,
    decimal HasGrami, string KurSnapshotId, decimal? Adet, string? Maden);
public sealed record OnToplamTuketim(string Id, string? CikisHareketId, string UrunId, string SubeId, decimal Adet,
    decimal? BirimMaliyetTl, decimal? BirimMaliyetHas, DateTime Tarih);

public sealed class AyOzeti
{
    public required Dictionary<(string Sube, string Urun), UrunKovasi> Kovalar { get; init; }
    public DateTime? TuketimEnErken { get; init; }
    public DateTime? TuketimEnGec { get; init; }
    public bool SubeUyumsuz { get; init; }
    public bool UrunUyumsuz { get; init; }
    public int SatirSayisi { get; init; }
}

/// <summary>
/// KAR-HIZ: dönemin ürün kovaları = paylaşılan (değiştirilmez) kapanmış kısım + canlı parça; okuma anında
/// birleştirilir (50.000 ürünlük sözlük her istekte kopyalanmaz). Aynı ürün iki tarafta varsa birleşik
/// YENİ kova üretilir — paylaşılan nesneye yazılmaz.
/// </summary>
public sealed class DonemKovaGorunumu : IEnumerable<KeyValuePair<string, UrunKovasi>>
{
    private readonly IReadOnlyDictionary<string, UrunKovasi>? paylasilan;
    private readonly Dictionary<string, UrunKovasi> canli;
    public int Count { get; }

    public DonemKovaGorunumu(IReadOnlyDictionary<string, UrunKovasi>? paylasilan, Dictionary<string, UrunKovasi> canli)
    {
        this.paylasilan = paylasilan;
        this.canli = canli;
        Count = (paylasilan?.Count ?? 0) + canli.Keys.Count(k => paylasilan is null || !paylasilan.ContainsKey(k));
    }

    public IEnumerator<KeyValuePair<string, UrunKovasi>> GetEnumerator()
    {
        if (paylasilan is not null)
            foreach (var kv in paylasilan)
            {
                if (canli.TryGetValue(kv.Key, out var c))
                {
                    var b = new UrunKovasi(); b.Ekle(kv.Value); b.Ekle(c);
                    yield return new(kv.Key, b);
                }
                else yield return kv;
            }
        foreach (var kv in canli)
            if (paylasilan is null || !paylasilan.ContainsKey(kv.Key)) yield return kv;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public static class KarUrunOnToplam
{
    // ── Ayarlar (testler için iç erişim) ────────────────────────────────────
    /// <summary>Geç commit güvenlik payı: bu kadar eski oluşturulmuş satırlar yeniden denetlenir.</summary>
    public static TimeSpan GuvenlikPayi { get; set; } = TimeSpan.FromMinutes(10);
    /// <summary>Kiracıya özel güvenlik payı (yalnız testler: önbellek isabet yolunu sınamak için).</summary>
    public static readonly ConcurrentDictionary<string, TimeSpan> KiraciGuvenlikPayi = new();
    private static TimeSpan Pay(string tenantId) =>
        KiraciGuvenlikPayi.TryGetValue(tenantId, out var p) ? p : GuvenlikPayi;
    /// <summary>Bellek tavanı (kova sayısı, tüm kiracılar). Aşılınca en eski kullanılan kiracı atılır.</summary>
    public static int KovaTavani { get; set; } = 600_000;
    /// <summary>Önbellek kapalı mı (eşdeğerlik testinde eski yolu zorlamak için).</summary>
    public static bool Kapali { get; set; }
    /// <summary>Sayfalı okuma boyu (TB-19: hiçbir okuma sınırsız değildir).</summary>
    public const int OkumaSayfasi = 20_000;

    private sealed class KiraciOnbellegi
    {
        public readonly ConcurrentDictionary<DateOnly, AyOzeti> Aylar = new();
        public DateTime DenetimDamgasi;   // UTC: bu andan sonra oluşturulan satırlar denetlenir
        public DateTime SonKullanim;
        public readonly SemaphoreSlim Kilit = new(1, 1);
        public int KovaSayisi => Aylar.Values.Sum(a => a.Kovalar.Count);
        /// <summary>Aylardan biri atılınca/eklenince artar — aralık toplamı önbelleği bununla doğrulanır.</summary>
        public long Surum;
        /// <summary>Son aralık toplamı (aynı gün aynı pencere + şube tekrarında 165.000 kovayı yeniden toplamamak için).</summary>
        public readonly Dictionary<string, (long Surum, Dictionary<string, UrunKovasi> Toplam)> AralikToplamlari = new();
        /// <summary>Kiracı başına en çok bu kadar aralık (şube × pencere) toplamı tutulur; aşılınca hepsi atılır.</summary>
        public const int AralikTavani = 8;
    }

    private static readonly ConcurrentDictionary<string, KiraciOnbellegi> Kiracilar = new();

    /// <summary>Bakım sonrası (ham SQL ile satır silme/değiştirme) önbelleği boşaltır.</summary>
    public static void Temizle(string? tenantId = null)
    {
        if (tenantId is null) Kiracilar.Clear();
        else Kiracilar.TryRemove(tenantId, out _);
    }

    /// <summary>Önbellekteki ay sayısı (test ve tanılama).</summary>
    public static int OnbellektekiAySayisi(string tenantId) =>
        Kiracilar.TryGetValue(tenantId, out var k) ? k.Aylar.Count : 0;

    // ── Kovalama: eski MaliyetiBilinenler kuralının kart-bağımsız hâli ──────
    public static Dictionary<(string Sube, string Urun), UrunKovasi> Kovala(
        IReadOnlyList<OnToplamHareket> hareketler, IReadOnlyList<OnToplamTuketim> tuketimler,
        IReadOnlyDictionary<string, decimal> kurlar)
    {
        var gruplar = tuketimler.Where(t => t.CikisHareketId != null)
            .GroupBy(t => t.CikisHareketId!)
            .ToDictionary(g => g.Key, g => g.ToList());
        var sonuc = new Dictionary<(string, string), UrunKovasi>();
        foreach (var h in hareketler)
        {
            var anahtar = (h.SubeId, h.UrunId);
            if (!sonuc.TryGetValue(anahtar, out var k)) sonuc[anahtar] = k = new UrunKovasi();
            k.HareketSayisi++;
            if (!kurlar.TryGetValue(h.KurSnapshotId ?? "", out var fiyat) || fiyat <= 0m) k.Kursuz++;
            var isaretli = h.Tur == "SATIS" ? h.TlTutari : -Math.Abs(h.TlTutari);

            if (!gruplar.TryGetValue(h.Id, out var satirlar) || satirlar.Count == 0) { k.UAdet++; continue; }
            decimal karsilanan = 0m, bilinenMal = 0m, bosAdet = 0m;
            var bosVar = false;
            foreach (var t in satirlar)
            {
                var abs = Math.Abs(t.Adet);
                karsilanan += abs;
                if (t.BirimMaliyetTl is { } b) { k.AbsBilinen += abs; bilinenMal += b * t.Adet; }
                else { k.AbsBos += abs; bosAdet += t.Adet; bosVar = true; }
            }
            if (karsilanan + KarZararServisi.KarsilamaToleransi < Math.Abs(h.Adet ?? 0m)) { k.UAdet++; continue; }
            if (!bosVar) { k.KAdet++; k.KTl += isaretli; k.KMal += bilinenMal; }
            else { k.NAdet++; k.NTl += isaretli; k.NMal += bilinenMal; k.NBosAdet += bosAdet; }
        }
        return sonuc;
    }

    // ── Sayfalı okuyucular (keyset; TB-19 sayfalı okuma) ─────────────────────
    public static async Task<List<OnToplamHareket>> HareketleriOku(IQueryable<Hareket> sorgu)
    {
        var sonuc = new List<OnToplamHareket>();
        string? son = null;
        while (true)
        {
            var q = son is null ? sorgu : sorgu.Where(h => string.Compare(h.Id, son) > 0);
            var sayfa = await q.OrderBy(h => h.Id)
                .Select(h => new OnToplamHareket(h.Id, h.Tur, h.UrunId!, h.SubeId, h.TlTutari, h.HasGrami,
                    h.KurSnapshotId, h.Adet, h.Maden))
                .Take(OkumaSayfasi)
                .ToListAsync();
            sonuc.AddRange(sayfa);
            if (sayfa.Count < OkumaSayfasi) return sonuc;
            son = sayfa[^1].Id;
        }
    }

    public static async Task<List<OnToplamTuketim>> TuketimleriOku(IQueryable<StokPartiTuketimi> sorgu)
    {
        var sonuc = new List<OnToplamTuketim>();
        string? son = null;
        while (true)
        {
            var q = son is null ? sorgu : sorgu.Where(t => string.Compare(t.Id, son) > 0);
            var sayfa = await q.OrderBy(t => t.Id)
                .Select(t => new OnToplamTuketim(t.Id, t.CikisHareketId, t.UrunId, t.SubeId, t.Adet,
                    t.BirimMaliyetTl, t.BirimMaliyetHas, t.Tarih))
                .Take(OkumaSayfasi)
                .ToListAsync();
            sonuc.AddRange(sayfa);
            if (sayfa.Count < OkumaSayfasi) return sonuc;
            son = sayfa[^1].Id;
        }
    }

    /// <summary>Net (storno çifti dışlanmış) satış/iade hareketleri, [basUtc, bitUtc).</summary>
    public static IQueryable<Hareket> NetSatislar(KuyumcuDbContext db, string tenantId, DateTime basUtc, DateTime bitUtc, string? subeId)
    {
        var temel = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId
                     && h.Tarih >= basUtc && h.Tarih < bitUtc
                     && (h.Tur == "SATIS" || h.Tur == "SATIS_IADE")
                     && h.UrunId != null);
        if (!string.IsNullOrEmpty(subeId)) temel = temel.Where(h => h.SubeId == subeId);
        return temel.StornoCiftiniDisla(db, tenantId);
    }

    private static async Task<Dictionary<string, decimal>> Kurlar(KuyumcuDbContext db, string tenantId, IEnumerable<string> idler)
    {
        var kume = idler.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        if (kume.Count == 0) return [];
        return await db.KurSnapshots.AsNoTracking()
            .Where(k => k.TenantId == tenantId && kume.Contains(k.Id))
            .Take(kume.Count)
            .ToDictionaryAsync(k => k.Id, k => k.AltinHasFiyati);
    }

    /// <summary>
    /// CANLI parça: [parcaBas, parcaBit) hareketleri, parti satırları ESKİ yolla AYNI pencereden
    /// ([pencereBas, pencereBit) + şube) süzülerek kovalanır.
    /// </summary>
    public static async Task<Dictionary<(string Sube, string Urun), UrunKovasi>> CanliKova(
        KuyumcuDbContext db, string tenantId, DateTime parcaBas, DateTime parcaBit,
        DateTime pencereBas, DateTime pencereBit, string? subeId)
    {
        var net = NetSatislar(db, tenantId, parcaBas, parcaBit, subeId);
        var hareketler = await HareketleriOku(net);
        if (hareketler.Count == 0) return [];
        var ids = net.Select(h => h.Id);
        var tsorgu = db.Set<StokPartiTuketimi>().AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.Tarih >= pencereBas && t.Tarih < pencereBit
                     && t.CikisHareketId != null && ids.Contains(t.CikisHareketId));
        if (!string.IsNullOrEmpty(subeId)) tsorgu = tsorgu.Where(t => t.SubeId == subeId);
        var tuketimler = await TuketimleriOku(tsorgu);
        var kurlar = await Kurlar(db, tenantId, hareketler.Select(h => h.KurSnapshotId));
        return Kovala(hareketler, tuketimler, kurlar);
    }

    /// <summary>Kapanmış bir ayın kovası — tüm şubeler, parti satırları KİMLİKLE (tarih süzgeci yok).</summary>
    private static async Task<AyOzeti> AyiHesapla(KuyumcuDbContext db, string tenantId, DateOnly ay)
    {
        var (bas, _) = Zaman.YerelGunAraligiUtc(ay);
        var (sonBas, _) = Zaman.YerelGunAraligiUtc(ay.AddMonths(1));
        var net = NetSatislar(db, tenantId, bas, sonBas, null);
        var hareketler = await HareketleriOku(net);
        var ids = net.Select(h => h.Id);
        var tuketimler = hareketler.Count == 0 ? [] : await TuketimleriOku(db.Set<StokPartiTuketimi>().AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.CikisHareketId != null && ids.Contains(t.CikisHareketId)));
        var kurlar = await Kurlar(db, tenantId, hareketler.Select(h => h.KurSnapshotId));
        var hIndeks = hareketler.ToDictionary(h => h.Id);
        var subeUyumsuz = false; var urunUyumsuz = false;
        foreach (var t in tuketimler)
        {
            if (t.CikisHareketId is null || !hIndeks.TryGetValue(t.CikisHareketId, out var h)) continue;
            if (t.SubeId != h.SubeId) subeUyumsuz = true;
            if (t.UrunId != h.UrunId) urunUyumsuz = true;
        }
        return new AyOzeti
        {
            Kovalar = Kovala(hareketler, tuketimler, kurlar),
            TuketimEnErken = tuketimler.Count == 0 ? null : tuketimler.Min(t => t.Tarih),
            TuketimEnGec = tuketimler.Count == 0 ? null : tuketimler.Max(t => t.Tarih),
            SubeUyumsuz = subeUyumsuz,
            UrunUyumsuz = urunUyumsuz,
            SatirSayisi = hareketler.Count,
        };
    }

    /// <summary>
    /// Dönemin ürün kovaları (şube süzgeci uygulanmış, ürün başına toplanmış). Kapanmış tam aylar
    /// önbellekten (doğrulanarak), kalan parçalar canlı.
    /// </summary>
    public static async Task<DonemKovaGorunumu> DonemKovalari(
        KuyumcuDbContext db, string tenantId, DateOnly baslangic, DateOnly bitis, string? subeId)
    {
        var (pencereBas, _) = Zaman.YerelGunAraligiUtc(baslangic);
        var (_, pencereBit) = Zaman.YerelGunAraligiUtc(bitis);
        var buAy = new DateOnly(Zaman.YerelBugun().Year, Zaman.YerelBugun().Month, 1);

        // Tam ve kapanmış aylar: [ilkTam, sonTamHaric)
        var ilkTam = baslangic.Day == 1 ? baslangic : new DateOnly(baslangic.Year, baslangic.Month, 1).AddMonths(1);
        var bitisSonrasi = bitis.AddDays(1);
        var sonTamHaric = new DateOnly(bitisSonrasi.Year, bitisSonrasi.Month, 1);
        if (sonTamHaric > buAy) sonTamHaric = buAy;
        var aylar = new List<DateOnly>();
        for (var a = ilkTam; a < sonTamHaric; a = a.AddMonths(1)) aylar.Add(a);

        // KAR-HIZ: kapanmış kısmın toplamı aynı kiracının istekleri arasında PAYLAŞILIR ve DEĞİŞTİRİLMEZ (kopyalanmaz); canlı parça
        // ayrı sözlükte toplanır, ikisi okuma anında birleştirilir (DonemKovaGorunumu). Kilit yalnız önbellek
        // durumunu korur; birleştirme ve canlı parça kilidin DIŞINDA.
        void Topla(Dictionary<string, UrunKovasi> hedef, Dictionary<(string Sube, string Urun), UrunKovasi> kovalar)
        {
            foreach (var ((sube, urun), k) in kovalar)
            {
                if (!string.IsNullOrEmpty(subeId) && sube != subeId) continue;
                if (!hedef.TryGetValue(urun, out var t)) hedef[urun] = t = new UrunKovasi();
                t.Ekle(k);
            }
        }
        var canli = new Dictionary<string, UrunKovasi>();

        if (Kapali || aylar.Count < 2)
        {
            Topla(canli, await CanliKova(db, tenantId, pencereBas, pencereBit, pencereBas, pencereBit, subeId));
            return new DonemKovaGorunumu(null, canli);
        }

        var (tamBasUtc, _) = Zaman.YerelGunAraligiUtc(aylar[0]);
        var (tamBitUtc, _) = Zaman.YerelGunAraligiUtc(aylar[^1].AddMonths(1));
        var (buAyUtc, _) = Zaman.YerelGunAraligiUtc(buAy);

        var kiraci = Kiracilar.GetOrAdd(tenantId, _ => new KiraciOnbellegi { DenetimDamgasi = DateTime.MinValue });
        Dictionary<string, UrunKovasi> paylasilan;
        await kiraci.Kilit.WaitAsync();
        try
        {
            kiraci.SonKullanim = DateTime.UtcNow;
            await Dogrula(db, tenantId, kiraci, buAy);
            // Aralık toplamı = kapanmış tam aylar + KAPANMIŞ kısmi parçalar (baş kısmi, bitiş geçmişteyse son
            // kısmi) + eşdeğerlik dışı aylar. İçinde bulunulan aya düşen parça HER istekte canlıdır.
            var anahtar = $"{aylar[0]:yyyy-MM}|{aylar[^1]:yyyy-MM}|{subeId}|{pencereBas.Ticks}|{pencereBit.Ticks}";
            if (kiraci.AralikToplamlari.TryGetValue(anahtar, out var at) && at.Surum == kiraci.Surum
                && aylar.All(kiraci.Aylar.ContainsKey))
            {
                // Aynı aralık, hiçbir şey değişmedi: toplam PAYLAŞILIR.
                paylasilan = at.Toplam;
            }
            else
            {
                var toplam = new Dictionary<string, UrunKovasi>();
                var canliAylar = new List<DateOnly>();
                foreach (var ay in aylar)
                {
                    if (!kiraci.Aylar.TryGetValue(ay, out var ozet))
                    {
                        var hesapAni = DateTime.UtcNow;
                        ozet = await AyiHesapla(db, tenantId, ay);
                        kiraci.Aylar[ay] = ozet;
                        kiraci.Surum++;
                        var damga = hesapAni - Pay(tenantId);
                        if (kiraci.DenetimDamgasi == DateTime.MinValue || damga < kiraci.DenetimDamgasi)
                            kiraci.DenetimDamgasi = damga;
                    }
                    // Eski yolla eşdeğerlik koşulları (fail-closed): değilse ay CANLI hesaplanır.
                    var pencereIcinde = ozet.TuketimEnErken is null
                        || (ozet.TuketimEnErken >= pencereBas && ozet.TuketimEnGec < pencereBit);
                    var subeUygun = string.IsNullOrEmpty(subeId) || !ozet.SubeUyumsuz;
                    if (pencereIcinde && subeUygun && !ozet.UrunUyumsuz) Topla(toplam, ozet.Kovalar);
                    else canliAylar.Add(ay);
                }
                if (pencereBas < tamBasUtc)
                    Topla(toplam, await CanliKova(db, tenantId, pencereBas, tamBasUtc, pencereBas, pencereBit, subeId));
                foreach (var ay in canliAylar)
                {
                    var (b, _) = Zaman.YerelGunAraligiUtc(ay);
                    var (e, _) = Zaman.YerelGunAraligiUtc(ay.AddMonths(1));
                    Topla(toplam, await CanliKova(db, tenantId, b, e, pencereBas, pencereBit, subeId));
                }
                // Son parça içinde bulunulan aydan ÖNCE bitiyorsa o da kapanmıştır → toplama girer.
                if (tamBitUtc < pencereBit && pencereBit <= buAyUtc)
                    Topla(toplam, await CanliKova(db, tenantId, tamBitUtc, pencereBit, pencereBas, pencereBit, subeId));
                if (kiraci.AralikToplamlari.Count >= KiraciOnbellegi.AralikTavani) kiraci.AralikToplamlari.Clear();
                kiraci.AralikToplamlari[anahtar] = (kiraci.Surum, toplam);
                paylasilan = toplam;
            }
        }
        finally { kiraci.Kilit.Release(); }
        Tavan();

        // CANLI parça: içinde bulunulan aya düşen kısım (ya da sınırı aşan son kısmi parça).
        if (tamBitUtc < pencereBit && pencereBit > buAyUtc)
            Topla(canli, await CanliKova(db, tenantId, tamBitUtc, pencereBit, pencereBas, pencereBit, subeId));
        return new DonemKovaGorunumu(paylasilan, canli);
    }

    /// <summary>
    /// Son denetimden bu yana oluşturulan (a) tarihi kapanmış aya düşen satış/iade, (b) storno
    /// satırlarını arar; etkilenen ayları atar. Çok fazla iz varsa kiracının tüm önbelleği atılır.
    /// </summary>
    private static async Task Dogrula(KuyumcuDbContext db, string tenantId, KiraciOnbellegi kiraci, DateOnly buAy)
    {
        if (kiraci.Aylar.IsEmpty) return;
        var simdi = DateTime.UtcNow;
        var (buAyUtc, _) = Zaman.YerelGunAraligiUtc(buAy);
        var damga = kiraci.DenetimDamgasi;
        const int izTavani = 500;
        var izler = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.Olusturuldu > damga
                     && (h.StornoKaynakId != null
                         || (h.Tarih < buAyUtc && (h.Tur == "SATIS" || h.Tur == "SATIS_IADE"))))
            .Select(h => new { h.Tarih, h.StornoKaynakId })
            .Take(izTavani + 1)
            .ToListAsync();
        if (izler.Count > izTavani) { kiraci.Aylar.Clear(); kiraci.Surum++; kiraci.DenetimDamgasi = simdi - Pay(tenantId); return; }

        var tarihler = izler.Where(i => i.StornoKaynakId is null).Select(i => i.Tarih).ToList();
        var kaynaklar = izler.Where(i => i.StornoKaynakId is not null).Select(i => i.StornoKaynakId!).Distinct().ToList();
        if (kaynaklar.Count > 0)
        {
            tarihler.AddRange(await db.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == tenantId && kaynaklar.Contains(h.Id))
                .Select(h => h.Tarih)
                .Take(kaynaklar.Count)
                .ToListAsync());
            // Storno satırının kendisi de dışlanır: kendi tarihi kapanmış aydaysa o ay da atılır.
            tarihler.AddRange(izler.Where(i => i.StornoKaynakId is not null).Select(i => i.Tarih));
        }
        // Kapanmış aralığa düşen HER iz aralık toplamını bayatlatır (kısmi baş parçası Aylar'da tutulmaz).
        if (tarihler.Count > 0) kiraci.Surum++;
        foreach (var t in tarihler)
        {
            var yerel = Zaman.YerelBugun(t);
            if (kiraci.Aylar.TryRemove(new DateOnly(yerel.Year, yerel.Month, 1), out _)) kiraci.Surum++;
        }
        var yeni = simdi - Pay(tenantId);
        if (yeni > kiraci.DenetimDamgasi) kiraci.DenetimDamgasi = yeni;
    }

    private static void Tavan()
    {
        var toplam = Kiracilar.Values.Sum(k => k.KovaSayisi);
        while (toplam > KovaTavani && Kiracilar.Count > 1)
        {
            var eski = Kiracilar.OrderBy(k => k.Value.SonKullanim).First();
            Kiracilar.TryRemove(eski.Key, out _);
            toplam = Kiracilar.Values.Sum(k => k.KovaSayisi);
        }
    }
}
