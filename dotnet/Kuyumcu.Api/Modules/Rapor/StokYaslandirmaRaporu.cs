using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Rapor;

// ═════════════════════════════════════════════════════════════════════════════
// R15 — STOK YAŞLANDIRMA / DEVİR HIZI (PAKET P-25 · kimlik Y-16).
//
// NEDEN VAR: kuyumcunun sermayesi vitrinde durur. "Hangi mal ne kadar süredir
// rafta?" sorusunun cevabı bugüne dek hiçbir raporda YOKTU; Rapor Merkezi 15
// rapor gösteriyordu ve stok tarafı yalnız DEĞER soruyordu (R06/R09/R20),
// SÜRE sormuyordu.
//
// ── KAYNAK NEDEN `stok_partileri` ───────────────────────────────────────────
// `stoklar` tablosunda GİRİŞ TARİHİ YOKTUR (yalnız `guncellendi` vardır ve o,
// son satışta da değişir — yaş ölçüsü olarak kullanılsaydı her satış malı
// "yeni" gösterirdi). Yaşın tek dürüst kaynağı maliyet partisidir:
// `stok_partileri.giris_tarihi` malın dükkâna girdiği andır, `adet_kalan` da
// o partiden hâlâ rafta duran miktardır. Devir hızı da aynı defterden okunur:
// `stok_parti_tuketimleri` satılan/çıkan adedi partiye yazar.
//
// ── ÜÇ DÜRÜSTLÜK KURALI (Anayasa md.1) ──────────────────────────────────────
//  1. PARTİSİ OLMAYAN STOK GİZLENMEZ. Parti kaydı açılmadan önce girmiş ya da
//     göçle gelmiş mal `stoklar`'da vardır ama partisi yoktur; yaşı BİLİNMEZ.
//     Bu miktar `yasiBilinmeyen` alanında AYRI durur, "0-30 gün" kovasına
//     atılmaz ve toplamdan da düşülmez.
//  2. DEVİR HIZI PAYDA SIFIRKEN NULL DÖNER, sıfır değil. Stoğu olmayan ürünün
//     devir hızı "0" değil "hesaplanamaz"dır.
//  3. FARKLI MADENLER TOPLANMAZ (StokDegeri'nin 2026-09-14 kararı): has gram
//     yalnız ALTIN için toplanır, diğer madenler ayrı kırılımdadır.
//
// ── ÖLÇEK (P-25 / B-8) ──────────────────────────────────────────────────────
// Bu rapor SINIRSIZ DÖKÜM YAPMAZ ve bu bir tercih değil kurallıdır: kovalar
// SQL `GROUP BY` ile, devir toplamları `SUM` ile alınır; yalnız "ölü stok"
// listesi satır döndürür ve o da `Skip/Take` ile sayfalıdır, toplam satır
// `CountAsync` ile ölçülür (TB-19 sözleşmesi: toplam sayfadan türetilmez).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir yaş kovasının SUNUCUDA hesaplanmış toplamı.</summary>
/// <param name="Kod">Kova kodu — ekran sözlükten çevirir (UX-S31).</param>
/// <param name="AltSinirGun">Kovanın alt sınırı (dâhil).</param>
/// <param name="UstSinirGun">Üst sınır (hariç); son kovada <c>null</c>.</param>
public sealed record YasKovasi(string Kod, int AltSinirGun, int? UstSinirGun);

public sealed class StokYaslandirmaServisi(KuyumcuDbContext db, DenetimServisi denetim)
{
    /// <summary>
    /// Yaş kovaları — TEK KAYNAK. Ekran da, test de, sunucu da bu diziyi
    /// kullanır; ikinci bir eşik listesi açılırsa iki ekran iki farklı cevap
    /// verirdi (UX-S31).
    /// </summary>
    public static readonly YasKovasi[] Kovalar =
    [
        new("G0_30",    0,   30),
        new("G31_90",   31,  90),
        new("G91_180",  91,  180),
        new("G181_365", 181, 365),
        new("G365_USTU", 366, null),
    ];

    /// <summary>Ölü stok eşiği varsayılanı (gün). Ekran değiştirebilir.</summary>
    public const int VarsayilanOluEsikGun = 180;

    /// <summary>Devir hızının ölçüldüğü varsayılan pencere (gün).</summary>
    public const int VarsayilanDevirPenceresiGun = 90;

    /// <summary>
    /// R15 — yaş kovaları + devir hızı + ölü stok sayfası.
    /// </summary>
    /// <param name="subeId">Şube süzgeci (P-04 sözleşmesi); boşsa tüm şubeler.</param>
    /// <param name="oluEsikGun">Bu günden eski ve hiç hareket görmeyen mal "ölü" sayılır.</param>
    /// <param name="devirPenceresiGun">Devir hızının ölçüldüğü geriye dönük pencere.</param>
    /// <param name="limit">Ölü stok sayfası boyu (TB-19 kapısı).</param>
    /// <param name="ofset">Ölü stok sayfası başlangıcı.</param>
    public async Task<object> Yaslandirma(
        AktifKullanici islem, string? subeId,
        int oluEsikGun = VarsayilanOluEsikGun,
        int devirPenceresiGun = VarsayilanDevirPenceresiGun,
        int limit = 100, int ofset = 0)
    {
        // ⚠ DataAnnotations SERVİS yolunda çalışmaz (Aşkın kuralı) — kapı burada.
        new RaporFiltresi(SubeId: subeId, Limit: limit, Ofset: ofset).Dogrula();
        if (oluEsikGun < 1 || oluEsikGun > 3650)
            throw new BadRequestException("Ölü stok eşiği 1 ile 3650 gün arasında olmalı");
        if (devirPenceresiGun < 1 || devirPenceresiGun > 3650)
            throw new BadRequestException("Devir penceresi 1 ile 3650 gün arasında olmalı");

        var simdi = DateTime.UtcNow;

        var partiler = db.Set<StokPartisi>().AsNoTracking()
            .Where(p => p.TenantId == islem.TenantId && p.AdetKalan > 0m);
        partiler = partiler.Daralt(subeId);   // PAKET ŞUBE-KAPSAM-A

        // ── 1) YAŞ KOVALARI — SUNUCUDA ──────────────────────────────────────
        // Eşikler tarih olarak ÖNCEDEN hesaplanır; SQL tarafında yalnız
        // karşılaştırma kalır (DATEDIFF'e bağımlılık yok, taşınabilir).
        var e30  = simdi.AddDays(-30);
        var e90  = simdi.AddDays(-90);
        var e180 = simdi.AddDays(-180);
        var e365 = simdi.AddDays(-365);

        // ⚠ PAKET KAR-RAPOR-2 (hasım bulgusu): kova `hasGram` altın hasıyla gümüş
        //   gramını TOPLUYORDU (242,5152 = altın 159,2652 + gümüş 83,25) — ekranın
        //   "has gramı yalnız altındır" notuyla çelişiyordu. Kova artık SQL'de
        //   (yaş, ayar) ile gruplanır; madene göre kırılır. `hasGram` YALNIZ ALTIN
        //   hasıdır (P-45 / K20-2 kuralı), diğer madenler `madenKirilimi`nde ayrı.
        //   Partisi ürüne bağlanamayan satır, madenDagilimi ile aynı varsayımla ALTIN.
        var kovaHamAyar = await (
            from p in partiler
            join u in db.Urunler.AsNoTracking().Where(u => u.TenantId == islem.TenantId)
                on p.UrunId equals u.Id into uj
            from u in uj.DefaultIfEmpty()
            select new
            {
                Sira = p.GirisTarihi < e365 ? 4
                     : p.GirisTarihi < e180 ? 3
                     : p.GirisTarihi < e90 ? 2
                     : p.GirisTarihi < e30 ? 1
                     : 0,
                AyarId = u == null ? null : u.AyarId,
                p.AdetKalan, p.HasGramBirim, p.BirimMaliyetTl,
                // MADEN-AYRIMI: tartı gramı (adet × ürün gramı); ürünsüz partide 0 ve grup null sayılır.
                GramAgirlik = u == null ? 0m : u.GramAgirlik,
            })
            .GroupBy(x => new { x.Sira, x.AyarId })
            .Select(g => new
            {
                g.Key.Sira,
                g.Key.AyarId,
                Adet = g.Sum(x => x.AdetKalan),
                Gram = g.Sum(x => x.AdetKalan * x.HasGramBirim),
                Fiziksel = g.Sum(x => x.AdetKalan * x.GramAgirlik),
                MaliyetliAdet = g.Sum(x => x.BirimMaliyetTl != null ? x.AdetKalan : 0m),
                MaliyetTl = g.Sum(x => x.BirimMaliyetTl != null
                    ? x.AdetKalan * x.BirimMaliyetTl!.Value : 0m),
            })
            .ToListAsync();

        var kovaHam = kovaHamAyar
            .Select(x => new
            {
                x.Sira, x.Adet, x.Gram, x.MaliyetliAdet, x.MaliyetTl,
                // Ürünsüz parti (AyarId null) için tartı gramı bilinmez → null (0 BASILMAZ).
                Fiziksel = x.AyarId == null ? (decimal?)null : x.Fiziksel,
                Maden = Kuyumcu.Core.Domain.Ayarlar.MadenBul(x.AyarId ?? "")
                        ?? Kuyumcu.Core.Domain.Maden.Altin,
            })
            .GroupBy(x => x.Sira)
            .Select(g => new
            {
                Sira = g.Key,
                Adet = g.Sum(x => x.Adet),
                AltinGram = g.Where(x => x.Maden == Kuyumcu.Core.Domain.Maden.Altin).Sum(x => x.Gram),
                MaliyetliAdet = g.Sum(x => x.MaliyetliAdet),
                MaliyetTl = g.Sum(x => x.MaliyetTl),
                Madenler = g.GroupBy(x => x.Maden)
                    .Select(m => new
                    {
                        maden = m.Key,
                        madenAdi = Kuyumcu.Core.Domain.Maden.Ad(m.Key),
                        adet = Math.Round(m.Sum(x => x.Adet), 3, MidpointRounding.AwayFromZero),
                        toplamGram = Math.Round(m.Sum(x => x.Gram), 6, MidpointRounding.AwayFromZero),
                        // MADEN-AYRIMI: "Gümüş 150 g (saf 138,75 g)" — tartı gramı ayrı alan.
                        fizikselGram = m.Any(x => x.Fiziksel is null) ? (decimal?)null
                            : Math.Round(m.Sum(x => x.Fiziksel!.Value), 4, MidpointRounding.AwayFromZero),
                    })
                    .OrderBy(m => m.maden == Kuyumcu.Core.Domain.Maden.Altin ? 0 : 1)
                    .ThenByDescending(m => m.toplamGram)
                    .ToList(),
            })
            .ToList();

        var kovalar = Kovalar.Select((k, i) =>
        {
            var h = kovaHam.FirstOrDefault(x => x.Sira == i);
            var adet = h?.Adet ?? 0m;
            var maliyetli = h?.MaliyetliAdet ?? 0m;
            return new
            {
                kod = k.Kod,
                altSinirGun = k.AltSinirGun,
                ustSinirGun = k.UstSinirGun,
                adet = Math.Round(adet, 3, MidpointRounding.AwayFromZero),
                // KAR-RAPOR-2: YALNIZ ALTIN hası (gümüş gramı karışmaz); aynı değer adıyla da.
                hasGram = Math.Round(h?.AltinGram ?? 0m, 6, MidpointRounding.AwayFromZero),
                altinHasGram = Math.Round(h?.AltinGram ?? 0m, 6, MidpointRounding.AwayFromZero),
                madenKirilimi = h?.Madenler ?? [],
                // Anayasa md.1: maliyeti bilinmeyen malın maliyeti SIFIR DEĞİL,
                // BİLİNMİYOR'dur. Tutar yalnız bilinen kısmı toplar; bilinmeyen
                // adet AYRI yazılır ve ekran oradan kayıtlara bağlanır (UX-S23).
                maliyetTl = Math.Round(h?.MaliyetTl ?? 0m, 2, MidpointRounding.AwayFromZero),
                maliyetiBilinmeyenAdet = Math.Round(adet - maliyetli, 3, MidpointRounding.AwayFromZero),
            };
        }).ToList();

        // ── 2) PARTİSİ OLMAYAN STOK — gizlenmez, ayrı durur ─────────────────
        var stoklar = db.Stoklar.AsNoTracking().Where(s => s.TenantId == islem.TenantId);
        stoklar = stoklar.Daralt(subeId);   // PAKET ŞUBE-KAPSAM-A

        var stokToplamAdet = await stoklar.SumAsync(s => (decimal?)s.Adet) ?? 0m;
        var partiToplamAdet = await partiler.SumAsync(p => (decimal?)p.AdetKalan) ?? 0m;
        var yasiBilinmeyenAdet = Math.Round(
            Math.Max(0m, stokToplamAdet - partiToplamAdet), 3, MidpointRounding.AwayFromZero);

        // ── 3) MADEN KIRILIMI — ayardan türetilir, SQL'de ayarId ile gruplanır
        // KAR-RAPOR-2 tur 2 (hasım bulgusu): kova sorgusu LEFT JOIN (ürünsüz parti ALTIN sayılır),
        //   bu sorgu INNER JOIN idi (ürünsüz parti düşüyordu) → iki rakam farklı kümeden gelebiliyordu.
        //   Artık ikisi de AYNI join kuralı: LEFT JOIN, ürünsüz parti → ayar yok → ALTIN varsayımı.
        var ayarHam = await (
            from p in partiler
            join u in db.Urunler.AsNoTracking().Where(u => u.TenantId == islem.TenantId)
                on p.UrunId equals u.Id into uj
            from u in uj.DefaultIfEmpty()
            select new { AyarId = u == null ? null : u.AyarId, p.AdetKalan, Gram = p.AdetKalan * p.HasGramBirim })
            .GroupBy(x => x.AyarId)
            .Select(g => new { AyarId = g.Key, Adet = g.Sum(x => x.AdetKalan), Gram = g.Sum(x => x.Gram) })
            .ToListAsync();

        var madenDagilimi = ayarHam
            .GroupBy(x => Kuyumcu.Core.Domain.Ayarlar.MadenBul(x.AyarId ?? "")
                          ?? Kuyumcu.Core.Domain.Maden.Altin)
            .Select(g => new
            {
                maden = g.Key,
                madenAdi = Kuyumcu.Core.Domain.Maden.Ad(g.Key),
                adet = Math.Round(g.Sum(x => x.Adet), 3, MidpointRounding.AwayFromZero),
                toplamGram = Math.Round(g.Sum(x => x.Gram), 6, MidpointRounding.AwayFromZero),
            })
            .OrderByDescending(x => x.toplamGram)
            .ToList();

        var altinHasGram = madenDagilimi
            .Where(x => x.maden == Kuyumcu.Core.Domain.Maden.Altin)
            .Sum(x => x.toplamGram);

        // ── 4) DEVİR HIZI — SUNUCU TOPLAMI ──────────────────────────────────
        // Tanım AÇIK YAZILIR, tahmin edilmez:
        //   devirHizi = pencerede tüketilen adet / bugün rafta kalan adet
        // Bu bir ORAN'dır; yıllık devir sayısı DEĞİLDİR (365/pencere ile
        // ölçeklenmiş hâli ayrı alanda `yillikTahmini` olarak ve TAHMİN
        // etiketiyle döner — "yıllık devir 4" diye kesin bir iddia üretmez).
        var devirBas = simdi.AddDays(-devirPenceresiGun);
        var tuketimler = db.Set<StokPartiTuketimi>().AsNoTracking()
            .Where(t => t.TenantId == islem.TenantId && t.Tarih >= devirBas);
        tuketimler = tuketimler.Daralt(subeId);   // PAKET ŞUBE-KAPSAM-A

        var tuketilenAdet = await tuketimler.SumAsync(t => (decimal?)t.Adet) ?? 0m;
        var kalanAdet = Math.Round(partiToplamAdet, 3, MidpointRounding.AwayFromZero);

        decimal? devirHizi = kalanAdet > 0m
            ? Math.Round(tuketilenAdet / kalanAdet, 4, MidpointRounding.AwayFromZero)
            : null;
        decimal? yillikTahmini = devirHizi is { } dh
            ? Math.Round(dh * 365m / devirPenceresiGun, 4, MidpointRounding.AwayFromZero)
            : null;
        // Ortalama raf ömrü: kalan / (tüketim hızı). Tüketim yoksa BİLİNMİYOR.
        decimal? ortalamaRafOmruGun = tuketilenAdet > 0m
            ? Math.Round(kalanAdet * devirPenceresiGun / tuketilenAdet, 1, MidpointRounding.AwayFromZero)
            : null;

        // ── PAKET RAPOR-GORUNUM (veri c): "Ortalama raf ömrü 310,9 gün" ile "tüm stok 0-30 gün"
        //   ÇELİŞKİ DEĞİL, AD HATASIYDI. `ortalamaRafOmruGun` = kalan ÷ (tüketilen ÷ pencere):
        //   bugünkü satış hızıyla raftaki malın KAÇ GÜNDE biteceğinin TAHMİNİDİR (ileriye bakar).
        //   Kovalar ise malın BUGÜNKÜ yaşıdır (geriye bakar). İki ayrı soru → iki ayrı alan:
        //     • tahminiSatisSuresiGun = ortalamaRafOmruGun (aynı değer, doğru adıyla; eski alan korunur)
        //     • ortalamaYasGun        = raftaki malın adet ağırlıklı GERÇEK yaşı (kovalarla aynı partiler)
        //   Yaş SQL'de toplanır (Σ adet × gün ÷ Σ adet); parti yoksa null (bilinmiyor, 0 değil).
        var yasToplami = await partiler
            .Select(p => p.AdetKalan * EF.Functions.DateDiffDay(p.GirisTarihi, simdi))
            .SumAsync(x => (decimal?)x) ?? 0m;
        decimal? ortalamaYasGun = partiToplamAdet > 0m
            ? Math.Round(yasToplami / partiToplamAdet, 1, MidpointRounding.AwayFromZero)
            : null;

        // ── 5) ÖLÜ STOK — SAYFALI, toplam CountAsync ile ────────────────────
        var oluEsik = simdi.AddDays(-oluEsikGun);
        var oluGruplu = partiler
            .Where(p => p.GirisTarihi < oluEsik)
            .GroupBy(p => p.UrunId)
            .Select(g => new
            {
                UrunId = g.Key,
                Adet = g.Sum(x => x.AdetKalan),
                Gram = g.Sum(x => x.AdetKalan * x.HasGramBirim),
                EnEskiGiris = g.Min(x => x.GirisTarihi),
            });

        var oluToplamSatir = await oluGruplu.CountAsync();
        var oluToplamAdet = await oluGruplu.SumAsync(x => (decimal?)x.Adet) ?? 0m;

        var oluSayfa = await oluGruplu
            .OrderBy(x => x.EnEskiGiris).ThenBy(x => x.UrunId)
            .Skip(ofset).Take(limit)
            .ToListAsync();

        var sayfaIdleri = oluSayfa.Select(x => x.UrunId).ToList();
        var urunMap = sayfaIdleri.Count == 0
            ? new Dictionary<string, Data.Urun>()
            : await db.Urunler.AsNoTracking()
                .Where(u => u.TenantId == islem.TenantId && sayfaIdleri.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

        var oluSatirlar = oluSayfa.Select(x =>
        {
            var u = urunMap.GetValueOrDefault(x.UrunId);
            var ayarId = u?.AyarId ?? "";
            return new
            {
                urunId = x.UrunId,
                kod = u?.Kod ?? "",
                ad = u?.Ad ?? "",
                ayarId,
                maden = Kuyumcu.Core.Domain.Ayarlar.MadenBul(ayarId)
                        ?? Kuyumcu.Core.Domain.Maden.Altin,
                adet = Math.Round(x.Adet, 3, MidpointRounding.AwayFromZero),
                hasGram = Math.Round(x.Gram, 6, MidpointRounding.AwayFromZero),
                enEskiGiris = x.EnEskiGiris,
                yasGun = (int)Math.Floor((simdi - x.EnEskiGiris).TotalDays),
            };
        }).ToList();

        // Denetim izi: rapor KİŞİSEL veri döndürmez (cari adı yoktur) ama stok
        // dökümü ticari bilgidir; okuma izi CariYaslandirma deseniyle aynı yazılır.
        await denetim.ListeOkumaKaydet(islem, nameof(Data.Urun), oluSatirlar.Count, "stok-yaslandirma");

        var uyarilar = new List<string>();
        if (yasiBilinmeyenAdet > 0m)
            uyarilar.Add(
                $"{yasiBilinmeyenAdet:0.###} adet malın maliyet partisi yok; yaşı BİLİNMİYOR "
                + "ve kovalara dağıtılmadı (göç öncesi ya da parti kaydı açılmadan girmiş mal).");
        if (madenDagilimi.Count > 1)
            uyarilar.Add(
                "Stokta birden çok maden var. `altinHasGram` yalnız ALTIN has gramıdır; "
                + "diğer madenler `madenDagilimi` altında ayrı verilir — gramları toplanmaz.");
        if (devirHizi is null)
            uyarilar.Add("Rafta mal olmadığı için devir hızı hesaplanamadı (sıfır DEĞİL, bilinmiyor).");

        return new
        {
            olculduAn = simdi,
            subeId,
            kovalar,
            toplamAdet = Math.Round(partiToplamAdet, 3, MidpointRounding.AwayFromZero),
            altinHasGram = Math.Round(altinHasGram, 6, MidpointRounding.AwayFromZero),
            madenDagilimi,
            yasiBilinmeyenAdet,
            devir = new
            {
                pencereGun = devirPenceresiGun,
                tuketilenAdet = Math.Round(tuketilenAdet, 3, MidpointRounding.AwayFromZero),
                kalanAdet,
                devirHizi,
                yillikTahmini,
                ortalamaRafOmruGun,
                // RAPOR-GORUNUM (veri c): aynı değer doğru adıyla + raftaki malın gerçek ortalama yaşı.
                tahminiSatisSuresiGun = ortalamaRafOmruGun,
                ortalamaYasGun,
                yontem = "PARTI_TUKETIMI",
                tanim = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("devirHizi = pencerede tüketilen adet ÷ bugün rafta kalan adet"),
            },
            oluStok = new
            {
                esikGun = oluEsikGun,
                toplamSatir = oluToplamSatir,
                toplamAdet = Math.Round(oluToplamAdet, 3, MidpointRounding.AwayFromZero),
                ofset,
                limit,
                dahaFazlasiVar = ofset + oluSatirlar.Count < oluToplamSatir,
                urunler = oluSatirlar,
            },
            guvenilirlik = RaporGuvenilirligi.Hesapla(partiToplamAdet, yasiBilinmeyenAdet),
            uyarilar = uyarilar.Select(u => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(u)).ToList(),   // PAKET DIL-SUNUCU
        };
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
[Route("api/v1/raporlar")]
[Yetki.ModulYetkisi("rapor")]
public sealed class StokYaslandirmaController(StokYaslandirmaServisi servis, KuyumcuDbContext db) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/raporlar/stok-yaslandirma — R15 yaş kovaları + devir hızı</summary>
    [HttpGet("stok-yaslandirma")]
    public async Task<object> StokYaslandirma(
        [FromQuery] string? subeId,
        [FromQuery] int? oluEsikGun,
        [FromQuery] int? devirPenceresiGun,
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, RaporFiltresi.EnFazlaLimit)] int limit = 100,
        [FromQuery, System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)] int ofset = 0)
        => await servis.Yaslandirma(
            Islem, await GorunumSubesi.Coz(db, Islem, subeId),
            oluEsikGun ?? StokYaslandirmaServisi.VarsayilanOluEsikGun,
            devirPenceresiGun ?? StokYaslandirmaServisi.VarsayilanDevirPenceresiGun,
            limit, ofset);
}
