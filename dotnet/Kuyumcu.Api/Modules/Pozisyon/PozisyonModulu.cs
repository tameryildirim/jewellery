using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Rapor;
using Kuyumcu.Contracts.Sayfalar.Pozisyon;
using Kuyumcu.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Pozisyon;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-2 — ALTIN POZİSYONU: PANELİN ÜÇÜNCÜ KARTI (SUNUCUDA HESAPLANIR)
//
// Pusula'nın ana ekranındaki 1. kutucuk "Açık Pozisyon"dur; patronun sabah
// baktığı tek sayı (PUSULA-REFERANS § 2). Bizde bu hesap YOKTU (ölçüm:
// Kuyumcu.Api'de "Pozisyon" = 0). Kurucunun onayladığı taslak (tuval
// Panel.dc.html, 3. kart): "Altın pozisyonum · +312,8 g has · Vitrin 298,4 g ·
// Alacak 14,4 g".
//
// TANIM (maden başına, gramlar YALNIZ o madenin has gramı):
//   NET = VİTRİN + HURDA + CARİ ALACAK − CARİ BORÇ
//
//   • VİTRİN  = stoklar.toplam_has_gram (ürünün ayarından maden) — KONSİNYE
//               HARİÇ: konsinye mal bize ait değildir (Entities.cs Urun.Konsinye);
//               vitrinde durur ama pozisyonumuz değildir. Ayrı satırda bilgi olarak döner.
//   • HURDA   = HurdaStokKapisi.KiraciKirilimi (hurda, stoklar
//               tablosunda DEĞİL, defterde yaşar — P-20). Tür listesi YALNIZ
//               kapı dosyasında durur; bu servis onu okumaz (P-20 sayacı b).
//   • CARİ    = cari has bakiyesini oluşturan defter satırları; işaret
//               sözleşmesi K4 (StornoServisi.CariEtkisi): bakiye += −HasGrami.
//               + → cari BİZE has borçlu (alacak), − → BİZ cariye borçluyuz.
//   • EMANET  = pozisyona GİRMEZ (nötr): müşterinin altını elimizdedir ama
//               sahibine borçluyuz. Elde +g, borç −g → net 0. Emanet cari
//               tarafına katılıp fiziki tarafa katılmasaydı pozisyon emanet
//               kadar EKSİK görünürdü. Bilgi olarak ayrı döner.
//
// ⚠ MADENLER TOPLANMAZ (K20-2 · P-45): ana sayı yalnız ALTIN'dır; gümüş/
//   platin/paladyum kendi satırında. Normalizasyon (boş/NULL maden = ALTIN)
//   P-45'in yardımcısından alınır: HareketToplami.MadenKodu / Coz — YENİDEN
//   YAZILMADI.
//
// ⚠ KİRACI: her sorgu açıkça tenant_id ile sınırlanır (global süzgeç ayrıca
//   çalışır — savunma derinliği). Şube: ISubeVarlik süzgeci (Hareket, Stok)
//   şubeye kilitli oturumda kendiliğinden daraltır.
//
// ⚠ SALT OKUNUR: yeni tablo/kolon YOK; hesap TÜRETİLİR. Düzeltme yolu kaynak
//   kayıtlardır (stok → sayım; cari has → düzeltme fişi; hurda → hurda ekranı).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class PozisyonServisi(KuyumcuDbContext db, DenetimServisi denetim)
{
    /// <summary>
    /// Cari has bakiyesini oluşturan defter türleri — K4 işaret sözleşmesi
    /// (bakiye += −HasGrami). Emanet türleri BİLEREK dışarıdadır (nötr).
    /// </summary>
    public static readonly string[] CariHasTurleri = ["VERESIYE", "TAHSILAT", "CARI_DUZELTME", "ACILIS"];

    /// <summary>Emanet türleri — pozisyona girmez, bilgi olarak döner.</summary>
    public static readonly string[] EmanetTurleri = ["EMANET_GIRIS", "EMANET_CIKIS"];

    public const string YonAlacak = "ALACAK";
    public const string YonBorc = "BORC";
    public const int EnFazlaLimit = 500;

    private static decimal Y6(decimal n) => Math.Round(n, 6, MidpointRounding.AwayFromZero);

    /// <param name="gorunumSubesi">
    /// PAKET ŞUBE-KAPSAM-B tur 3: GorunumSubesi.Coz ile ÇÖZÜLMÜŞ şube (null = tüm
    /// şubeler). Yalnız FİZİKİ taraf (vitrin, konsinye, hurda) daraltılır; cari
    /// alacak/borç ve emanet MÜŞTERİ HESABIDIR ve şubeye bölünmez (şartname § 1
    /// yasal değişmez: "cari bakiye … kiracı genelinde") — yanıt bunu
    /// <see cref="AltinPozisyonuYaniti.CariKapsam"/> ile söyler.
    /// </param>
    public async Task<AltinPozisyonuYaniti> Hesapla(AktifKullanici islem, string? gorunumSubesi = null)
    {
        var t = islem.TenantId;

        // ── 1) VİTRİN + KONSİNYE: stoklar ⋈ ürünler, (ayar, konsinye) başına ──
        // Maden ayardan TÜRETİLİR (Ayarlar.MadenBul bellek içi tablo); ayar sayısı
        // onlarcadır — sınırsız okuma değildir (StokDegeri ile aynı gerekçe).
        var stok = await (
            from s in db.Stoklar.AsNoTracking().Where(s => s.TenantId == t).Daralt(gorunumSubesi)
            join u in db.Urunler.AsNoTracking().Where(u => u.TenantId == t) on s.UrunId equals u.Id
            group s.ToplamHasGram by new { u.AyarId, u.Konsinye } into g
            select new { g.Key.AyarId, g.Key.Konsinye, Gram = g.Sum() })
            .ToListAsync();

        // ── 2) HURDA ve EMANET: P-45 yardımcısı (tür, maden) kırılımı ─────────
        var hurda = await Hurda.HurdaStokKapisi.KiraciKirilimi(db, t, gorunumSubesi);
        var emanet = await HareketToplami.Coz(db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == t && h.CariId != null && EmanetTurleri.Contains(h.Tur)));

        // ── 3) CARİ: (cari, maden) başına bakiye, SUNUCUDA toplanır ──────────
        // Yalnız bakiyesi SIFIR OLMAYAN satırlar döner (HAVING); satır başına
        // üç alan — alacak/borç ayrımı için cari düzeyi şarttır.
        var cari = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == t && h.CariId != null && CariHasTurleri.Contains(h.Tur))
            .GroupBy(h => new { h.CariId, h.Maden })
            .Select(g => new { g.Key.CariId, g.Key.Maden, Has = -g.Sum(x => x.HasGrami) })
            .Where(x => x.Has != 0m)
            .ToListAsync();
        var cariMaden = cari
            .GroupBy(x => new { x.CariId, Maden = HareketToplami.MadenKodu(x.Maden) })
            .Select(g => new { g.Key.Maden, Has = Y6(g.Sum(x => x.Has)) })
            .Where(x => x.Has != 0m)
            .ToList();

        // ── 4) Maden başına birleştir — TOPLAMA YOK, yan yana koyma VAR ──────
        string StokMaden(string? ayarId) => Ayarlar.MadenBul(ayarId ?? "") ?? Maden.Altin;
        var madenler = new SortedSet<string>(StringComparer.Ordinal) { Maden.Altin };
        foreach (var s in stok) madenler.Add(StokMaden(s.AyarId));
        foreach (var h in hurda) madenler.Add(HareketToplami.MadenKodu(h.Maden));
        foreach (var e in emanet) madenler.Add(HareketToplami.MadenKodu(e.Maden));
        foreach (var c in cariMaden) madenler.Add(c.Maden);

        MadenPozisyonu Kur(string m)
        {
            var vitrin = Y6(stok.Where(s => !s.Konsinye && StokMaden(s.AyarId) == m).Sum(s => s.Gram));
            var konsinye = Y6(stok.Where(s => s.Konsinye && StokMaden(s.AyarId) == m).Sum(s => s.Gram));
            var hurdaGram = Y6(hurda.Where(h => HareketToplami.MadenKodu(h.Maden) == m).Sum(h => h.HasToplam));
            var emanetGram = Y6(emanet.Where(e => HareketToplami.MadenKodu(e.Maden) == m).Sum(e => e.HasToplam));
            var alacaklar = cariMaden.Where(c => c.Maden == m && c.Has > 0m).ToList();
            var borclar = cariMaden.Where(c => c.Maden == m && c.Has < 0m).ToList();
            var alacak = Y6(alacaklar.Sum(c => c.Has));
            var borc = Y6(-borclar.Sum(c => c.Has));
            return new MadenPozisyonu(
                m, vitrin, hurdaGram, alacak, borc,
                Y6(vitrin + hurdaGram + alacak - borc),
                alacaklar.Count, borclar.Count, konsinye, emanetGram);
        }

        var altin = Kur(Maden.Altin);
        var digerleri = madenler.Where(m => m != Maden.Altin)
            .Select(Kur)
            .Where(p => p.VitrinGram != 0m || p.HurdaGram != 0m || p.CariAlacakGram != 0m
                     || p.CariBorcGram != 0m || p.KonsinyeGram != 0m || p.EmanetGram != 0m)
            .ToList();

        // ── 5) İki kaynağın tutarlılığı: cari_bakiyeler (ALTIN) ↔ defter ─────
        // cari_bakiyeler.has_gram_bakiye YALNIZ ALTIN'dır (K20-2) ve emaneti de
        // içerir. Defter karşılığı: ALTIN cari türleri + ALTIN emanet (−).
        var tabloAltin = await db.CariBakiyeler.AsNoTracking()
            .Where(b => b.TenantId == t)
            .SumAsync(b => (decimal?)b.HasGramBakiye) ?? 0m;
        var defterAltin = altin.CariAlacakGram - altin.CariBorcGram - altin.EmanetGram;
        var fark = Y6(tabloAltin - defterAltin);

        return new AltinPozisyonuYaniti(
            Maden.Altin, altin, digerleri, digerleri.Count > 0, fark, DateTime.UtcNow, true,
            GorunumSubesi.Kapsam(gorunumSubesi), GorunumSubesi.KiraciGeneli);
    }

    /// <summary>
    /// Kırılım: tek madende bize borçlu (ALACAK) ya da bizim borçlu olduğumuz
    /// (BORC) cariler — kartın "Alacak/Borç" sayısının kayıtlara giden yolu (UX-S23).
    /// Sınırlı ve sayfalı (UX-S29); en büyük bakiye önce.
    /// </summary>
    public async Task<PozisyonCariSayfasi> Cariler(
        AktifKullanici islem, string? maden, string? yon, int limit, int ofset)
    {
        var m = string.IsNullOrWhiteSpace(maden) ? Maden.Altin : maden.Trim().ToUpperInvariant();
        if (Array.IndexOf(Maden.Hepsi, m) < 0)
            throw new BadRequestException($"Bilinmeyen maden: '{maden}'.");
        var y = string.IsNullOrWhiteSpace(yon) ? YonAlacak : yon.Trim().ToUpperInvariant();
        if (y != YonAlacak && y != YonBorc)
            throw new BadRequestException($"Yön ALACAK ya da BORC olmalı: '{yon}'.");
        if (limit < 1 || limit > EnFazlaLimit)
            throw new BadRequestException($"Limit 1–{EnFazlaLimit} arasında olmalı.");
        if (ofset < 0)
            throw new BadRequestException("Ofset negatif olamaz.");

        var t = islem.TenantId;
        var altinMi = m == Maden.Altin;
        var gruplu = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == t && h.CariId != null && CariHasTurleri.Contains(h.Tur))
            // Boş/NULL maden ALTIN'dır (K20-8, HareketToplami.MadenKodu ile aynı kural).
            .Where(h => h.Maden == m || (altinMi && (h.Maden == null || h.Maden == "")))
            .GroupBy(h => h.CariId!)
            .Select(g => new { CariId = g.Key, Has = -g.Sum(x => x.HasGrami) });
        gruplu = y == YonAlacak ? gruplu.Where(x => x.Has > 0m) : gruplu.Where(x => x.Has < 0m);

        var toplam = await gruplu.CountAsync();
        var sayfa = y == YonAlacak
            ? await gruplu.OrderByDescending(x => x.Has).ThenBy(x => x.CariId).Skip(ofset).Take(limit).ToListAsync()
            : await gruplu.OrderBy(x => x.Has).ThenBy(x => x.CariId).Skip(ofset).Take(limit).ToListAsync();

        var idler = sayfa.Select(x => x.CariId).ToList();
        var adlar = idler.Count == 0
            ? new Dictionary<string, string>()
            : await db.Cariler.AsNoTracking()
                .Where(c => c.TenantId == t && idler.Contains(c.Id))
                .Take(limit)   // idler ≤ limit — sayaç B-8 için açık sınır
                .ToDictionaryAsync(c => c.Id, c => c.Ad);

        var satirlar = sayfa
            .Select(x => new PozisyonCariSatiri(x.CariId, adlar.GetValueOrDefault(x.CariId) ?? "", Y6(x.Has)))
            .ToList();

        // KVKK md.12 / M115 — cari adı dönen liste okuması iz bırakır.
        await denetim.ListeOkumaKaydet(islem, nameof(Data.Cari), satirlar.Count, $"pozisyon-cariler {m}/{y}");

        return new PozisyonCariSayfasi(m, y, satirlar, toplam, ofset, limit, ofset + satirlar.Count < toplam);
    }
}

// ── Controller ───────────────────────────────────────────────────────────────
// Yetki: raporlarla AYNI kapı — pozisyon işletmenin metal bilançosudur, rapor
// modülünün yetkisine tabidir. TEZGÂHTAR 403 alır; ekran bunu "kayıt yok"a
// ÇEVİRMEZ, yetki notu basar (UX-S28).

[ApiController]
[Authorize]
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
[Route("api/v1/pozisyon")]
[Yetki.ModulYetkisi("rapor")]
public sealed class PozisyonController(PozisyonServisi servis, KuyumcuDbContext db) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>
    /// GET /api/v1/pozisyon[?subeId=ULID|TUMU] — altın pozisyonu (+ diğer madenler ayrı).
    /// Dönemden bağımsız. ŞUBE-KAPSAM-B tur 3: görünüm şubesi TEK yoldan çözülür
    /// (GorunumSubesi.Coz — kilitli rol başka şube → 403, pasif/yabancı şube → 404).
    /// </summary>
    [HttpGet]
    public async Task<AltinPozisyonuYaniti> Getir([FromQuery] string? subeId = null)
        => await servis.Hesapla(Islem, await GorunumSubesi.Coz(db, Islem, subeId));

    /// <summary>GET /api/v1/pozisyon/cariler?maden=ALTIN&amp;yon=ALACAK&amp;limit=50&amp;ofset=0 — kırılım.</summary>
    [HttpGet("cariler")]
    public Task<PozisyonCariSayfasi> Cariler(
        [FromQuery] string? maden, [FromQuery] string? yon,
        [FromQuery] int limit = 50, [FromQuery] int ofset = 0)
        => servis.Cariler(Islem, maden, yon, limit, ofset);
}
