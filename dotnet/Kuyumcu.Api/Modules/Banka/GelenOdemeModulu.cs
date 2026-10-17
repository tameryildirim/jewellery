using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Kuyumcu.Api.Modules.Satis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Banka;

// ═════════════════════════════════════════════════════════════════════════════
// M7a — GELEN ÖDEME: müşteriyi bul / yoksa aç + "borcundan düşülsün mü?" kararı.
//
// Kurucu 17.10:
//   • "hesabımıza para geldiğinde kişinin TC'si ve adı soyadı var… müşteri listesinden otomatik
//     bulsun, öyle bir müşteri yoksa otomatik kaydetsin… kullanıcı ekstra iş yapmaz"
//   • "adamın borcu vardır ama ekstra bir altın daha almıştır, gönderdiği para o altının borcudur
//     belki; borçtan düşme işini müşteriye sor, ekrana uyarı gelsin… müşteri karar versin"
//
// BULMA SIRASI (CariEslestirici K4-OK-5 korunur — ad benzerliği ASLA bağlamaz):
//   1) TCKN/VKN tam eşleşme  2) IBAN hash tam eşleşme (cari_ibanlari)
//   3) Adı benzeyen müşteri varsa → BAĞLANMAZ, öneri olarak döner (iki "Mehmet Yılmaz" olabilir)
//   4) Hiç benzeyen yoksa, ad geçerliyse VE TCKN/VKN ya da IBAN varsa → müşteri KENDİLİĞİNDEN açılır (CariServisi.OlusturId:
//      TCKN kuralı, benzersizlik, yaptırım taraması AYNI kapılardan), IBAN kartına yazılır.
//
// KARAR: BORC → TAHSILAT satırı (kasa bacağı YOK: para zaten banka hesabında; MasakOlcuTl = 0 —
//   para MASAK'a dekontun kendi gününde sayıldı, ikinci kez sayılmaz). SATIS → borca dokunulmaz.
//   Geri alma: TAHSILAT'ın stornosu (StornoServisi kararı boşaltır).
// ═════════════════════════════════════════════════════════════════════════════

public static class CariKarari
{
    public const string Borc = "BORC";
    public const string Satis = "SATIS";
}

public static class CariCozumYolu
{
    public const string Secildi = "SECILDI";
    public const string Kimlik = "KIMLIK";
    public const string Iban = "IBAN";
    public const string Yeni = "YENI";
    public const string Oneri = "ONERI";
    public const string Yok = "YOK";
}

public sealed record CariCozum(string Yol, string? CariId, IReadOnlyList<CariOneri> Oneriler);
public sealed record CariOneri(string Id, string Ad, decimal Skor);

public sealed class BankaKararDto
{
    [Required(ErrorMessage = "Karar zorunludur")]
    [RegularExpression("BORC|SATIS", ErrorMessage = "Karar BORC ya da SATIS olmalıdır")]
    public string Karar { get; set; } = null!;
}

public sealed class GelenOdemeServisi(
    KuyumcuDbContext db, BankaServisi banka, Cari.CariServisi cari, Satis.StornoServisi storno,
    DenetimServisi denetim)
{
    /// <summary>Ad benzerliği bu skordan itibaren "aynı kişi olabilir" sayılır (yalnız öneri).</summary>
    public const decimal OneriEsigi = 0.5m;

    // ── 1) Müşteriyi çöz ─────────────────────────────────────────────────────
    public async Task<CariCozum> Coz(string? kimlikNo, string? iban, string? ad, string tenantId)
    {
        var no = DogalAnahtar.Normalle(kimlikNo);
        if (no is not null && KimlikNo.Dogrula(no, KimlikNo.TurTuret(no)) is null)
        {
            var id = await db.Cariler.AsNoTracking()
                .Where(c => c.TenantId == tenantId && c.VergiNo == no && !c.SistemCarisi)
                .Select(c => c.Id).FirstOrDefaultAsync();
            if (id is not null) return new(CariCozumYolu.Kimlik, id, []);
        }

        if (!string.IsNullOrWhiteSpace(iban))
        {
            var hash = db.Sifreleyici.Hashle(iban);
            var id = await db.CariIbanlari.AsNoTracking()
                .Where(c => c.TenantId == tenantId && c.IbanHash == hash)
                .Select(c => c.CariId).FirstOrDefaultAsync();
            if (id is not null) return new(CariCozumYolu.Iban, id, []);
        }

        if (string.IsNullOrWhiteSpace(ad) || Kuyumcu.Core.Domain.Girdi.AdKurali.Unvan(ad) is not null)
            return new(CariCozumYolu.Yok, null, []);

        var oneriler = await Benzeyenler(ad, tenantId);
        if (oneriler.Count > 0) return new(CariCozumYolu.Oneri, null, oneriler);
        // Yalnız ad yetmez: POS yatması, faiz, banka masrafı da "karşı taraf adı" taşır ve müşteri
        // DEĞİLDİR. Kart ancak bir KİŞİYİ gösteren kanıtla açılır: geçerli TCKN/VKN ya da gönderen IBAN'ı.
        var kanit = (no is not null && KimlikNo.Dogrula(no, KimlikNo.TurTuret(no)) is null) || !string.IsNullOrWhiteSpace(iban);
        return kanit ? new(CariCozumYolu.Yeni, null, []) : new(CariCozumYolu.Yok, null, []);
    }

    /// <summary>Adın herhangi bir kelimesini taşıyan müşteriler arasından skoru eşiği geçenler.</summary>
    private async Task<List<CariOneri>> Benzeyenler(string ad, string tenantId)
    {
        var kelimeler = CariEslestirici.UnvanNormalle(ad)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(k => k.Length >= 2).Take(4).ToList();
        if (kelimeler.Count == 0) return [];
        var adaylar = new Dictionary<string, string>();
        foreach (var k in kelimeler)
        {
            var desen = $"%{k}%";
            foreach (var c in await db.Cariler.AsNoTracking()
                         .Where(c => c.TenantId == tenantId && !c.SistemCarisi && EF.Functions.Like(c.Ad, desen))
                         .OrderBy(c => c.Ad).Select(c => new { c.Id, c.Ad }).Take(50).ToListAsync())
                adaylar[c.Id] = c.Ad;
        }
        return [.. adaylar
            .Select(a => new CariOneri(a.Key, a.Value, CariEslestirici.Skor(ad, a.Value)))
            .Where(o => o.Skor >= OneriEsigi)
            .OrderByDescending(o => o.Skor).Take(5)];
    }

    // ── 2) Dekont gir: müşteriyi bul / aç, IBAN'ı öğret ──────────────────────
    public async Task<object> HareketEkle(string hesapId, HareketEkleDto dto, AktifKullanici islem)
    {
        var kimlikNo = DogalAnahtar.Normalle(dto.KarsiKimlikNo);
        if (kimlikNo is not null && KimlikNo.Dogrula(kimlikNo, KimlikNo.TurTuret(kimlikNo)) is { } kh)
            throw new BadRequestException(kh).AlanIle("karsiKimlikNo");

        var gelen = dto.Yon == HareketYonu.BankaGelen;
        CariCozum cozum = !string.IsNullOrEmpty(dto.CariId)
            ? new(CariCozumYolu.Secildi, dto.CariId, [])
            : gelen
                ? await Coz(kimlikNo, dto.KarsiIban, dto.KarsiTarafAd, islem.TenantId)
                : new(CariCozumYolu.Yok, null, []);

        if (cozum.Yol is CariCozumYolu.Kimlik or CariCozumYolu.Iban or CariCozumYolu.Yeni)
        {
            await using var tx = await db.Database.IsBaslatVeyaDevral();
            try
            {
                var cariId = cozum.CariId ?? await cari.OlusturId(new Cari.CariOlusturDto
                {
                    Tur = Cari.SistemCarisi.CariTuru,   // "MUSTERI" (tek tanım)
                    Ad = dto.KarsiTarafAd!.Trim(),
                    VergiNo = kimlikNo,
                    Notlar = "Bankadan gelen ödemeyle kendiliğinden açıldı",
                }, islem);
                if (!string.IsNullOrWhiteSpace(dto.KarsiIban))
                    await IbanOgretSessiz(db, cariId, dto.KarsiIban, dto.KarsiTarafAd, islem);
                dto.CariId = cariId;
                var sonuc = await banka.HareketEkle(hesapId, dto, islem);
                if (tx is not null) await tx.CommitAsync();
                return await Yanit(sonuc, cozum with { CariId = cariId }, islem);
            }
            catch (BadRequestException) when (tx is not null)
            {
                // Otomatik bağ yasal bir kapıya takıldı (ör. MASAK kimlik bloğu): para YİNE kaydedilir,
                // müşterisiz ("bekliyor"); kuyumcu listeden bağlar. Açılmış kart geri alınır.
                await tx.RollbackAsync();
                db.ChangeTracker.Clear();
                dto.CariId = null;
                cozum = new(CariCozumYolu.Yok, null, cozum.Oneriler);
            }
        }

        if (cozum.Yol == CariCozumYolu.Secildi && gelen && !string.IsNullOrWhiteSpace(dto.KarsiIban))
            await IbanOgretSessiz(db, dto.CariId!, dto.KarsiIban, dto.KarsiTarafAd, islem);
        var duz = await banka.HareketEkle(hesapId, dto, islem);
        return await Yanit(duz, cozum, islem);
    }

    private async Task<object> Yanit(object sonuc, CariCozum cozum, AktifKullanici islem)
    {
        var t = sonuc.GetType();
        var hareketId = (string)t.GetProperty("hareketId")!.GetValue(sonuc)!;
        var hesap = t.GetProperty("hesap")!.GetValue(sonuc);
        string? cariAd = cozum.CariId is null ? null : await db.Cariler.AsNoTracking()
            .Where(c => c.Id == cozum.CariId && c.TenantId == islem.TenantId).Select(c => c.Ad).FirstOrDefaultAsync();
        return new
        {
            hareketId, hesap,
            cari = new { yol = cozum.Yol, cariId = cozum.CariId, cariAd, oneriler = cozum.Oneriler },
        };
    }

    /// <summary>IBAN'ı cariye öğretir; başka cariye bağlıysa DOKUNMAZ (bir IBAN tek cariye).</summary>
    internal static async Task IbanOgretSessiz(KuyumcuDbContext db, string cariId, string iban, string? unvan, AktifKullanici islem)
    {
        var hash = db.Sifreleyici.Hashle(iban);
        if (await db.CariIbanlari.AnyAsync(c => c.TenantId == islem.TenantId && c.IbanHash == hash)) return;
        if (db.CariIbanlari.Local.Any(c => c.TenantId == islem.TenantId && c.IbanHash == hash)) return;
        db.CariIbanlari.Add(new CariIban
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, CariId = cariId, Iban = iban,
            IbanHash = hash,
            Unvan = unvan is { Length: > 150 } u ? u[..150] : unvan,
            Kaynak = IbanBagKaynagi.Ogrenildi, Olusturuldu = DateTime.UtcNow,
        });
    }

    // ── 3) Karar bekleyenler ("önüne düşen liste") ──────────────────────────
    public async Task<object> KararBekleyenler(AktifKullanici islem)
    {
        var satirlar = await db.BankaHareketleri.AsNoTracking()
            .Where(b => b.TenantId == islem.TenantId
                     && b.Yon == HareketYonu.BankaGelen
                     && b.CariId != null && b.CariKarari == null
                     && b.Durum == "ESLESTI" && b.ParaBirimi == "TRY"
                     && b.StornoKaynakId == null && b.KarsiBacakId == null)
            .StornoCiftiniDisla(db, islem.TenantId)
            .OrderByDescending(b => b.Olusturuldu)
            .Take(100)
            .ToListAsync();

        var cariIdler = satirlar.Select(s => s.CariId!).Distinct().ToList();
        var adlar = await db.Cariler.AsNoTracking()
            .Where(c => c.TenantId == islem.TenantId && cariIdler.Contains(c.Id))
            .OrderBy(c => c.Id).Take(cariIdler.Count)
            .ToDictionaryAsync(c => c.Id, c => c.Ad);
        var bakiyeler = await db.CariBakiyeler.AsNoTracking()
            .Where(b => b.TenantId == islem.TenantId && cariIdler.Contains(b.CariId))
            .OrderBy(b => b.CariId).Take(cariIdler.Count)
            .ToDictionaryAsync(b => b.CariId, b => b.TlBakiye);

        return satirlar.Select(s => new
        {
            id = s.Id,
            bankaHesapId = s.BankaHesapId,
            tarih = s.Tarih,
            tutar = s.Tutar,
            karsiTarafAd = s.KarsiTarafAd,
            aciklama = s.Aciklama,
            cariId = s.CariId,
            cariAd = adlar.GetValueOrDefault(s.CariId!),
            borcTl = Math.Max(0m, bakiyeler.GetValueOrDefault(s.CariId!)),
        }).ToList();
    }

    // ── 4) Karar ────────────────────────────────────────────────────────────
    public async Task<object> Karar(string bankaHareketId, BankaKararDto dto, AktifKullanici islem)
    {
        if (dto.Karar is not (CariKarari.Borc or CariKarari.Satis))
            throw new BadRequestException("Karar BORC ya da SATIS olmalıdır").AlanIle("karar");

        var h = await db.BankaHareketleri
            .FirstOrDefaultAsync(x => x.Id == bankaHareketId && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Banka hareketi bulunamadı");
        KararaUygunMu(h);
        if (await db.BankaHareketleri.AnyAsync(x => x.TenantId == islem.TenantId && x.StornoKaynakId == h.Id))
            throw new ConflictException("Bu dekont stornolanmıştır; karar verilemez.");

        var now = DateTime.UtcNow;
        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, h.CariId, now);
        await db.Database.Kilitle(KilitTablosu.BankaHareketleri, h.Id, islem.TenantId);
        var guncel = await db.BankaHareketleri.AsNoTracking()
            .Where(x => x.Id == h.Id && x.TenantId == islem.TenantId)
            .Select(x => new { x.CariKarari }).FirstAsync();
        if (guncel.CariKarari == CariKarari.Borc)
            throw new ConflictException("Bu ödeme zaten borcundan düşüldü.");

        if (dto.Karar == CariKarari.Satis)
        {
            h.CariKarari = CariKarari.Satis;
            denetim.YazmaKaydet(islem, "BankaOdemeKarari", h.Id,
                $"gelen ödeme yeni alışverişin ödemesi sayıldı · borca dokunulmadı · cari: {h.CariId}", hassas: false);
        }
        else
        {
            if (islem.SubeId is null)
                throw new BadRequestException("Hareket kaydı için şube seçimi gerekli");
            var kurId = await db.KurSnapshots.AsNoTracking()
                .Where(s => s.TenantId == islem.TenantId && !s.IptalEdildi)
                .OrderByDescending(s => s.Olusturuldu).Select(s => s.Id).FirstOrDefaultAsync()
                ?? throw new BadRequestException("Önce güncel kur alınmalı (kur ekranı).");

            var tahsilatId = Kimlik.Ulid();
            HareketFabrikasi.Ekle(db, OdemeIzi.Etiketten("HAVALE", h.Tutar, vade: null), new Hareket
            {
                Id = tahsilatId,
                TenantId = islem.TenantId,
                SubeId = islem.SubeId,
                Tur = "TAHSILAT",
                Tarih = now,
                Aciklama = $"Banka havalesi · {h.KarsiTarafAd}".TrimEnd(' ', '·'),
                TlTutari = -h.Tutar,
                HasGrami = 0m,
                // Para MASAK'a dekontun girildiği gün banka satırıyla sayıldı; burada 0 → çift sayım yok.
                MasakOlcuTl = 0m,
                OdemeTuru = Satis.OdemeTutarKapisi.Etiket(h.Tutar, "HAVALE"),
                KurSnapshotId = kurId,
                CariId = h.CariId,
                KullaniciId = islem.Id,
                Olusturuldu = now,
            });
            await CariBakiyeYazici.Uygula(db, islem.TenantId, h.CariId!, -h.Tutar, 0m, now);
            h.CariKarari = CariKarari.Borc;
            h.KararHareketId = tahsilatId;
            denetim.YazmaKaydet(islem, "BankaOdemeKarari", h.Id,
                $"gelen ödeme borcundan düşüldü · tahsilat: {tahsilatId} · cari: {h.CariId}", hassas: false);
        }

        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();
        return new { id = h.Id, cariKarari = h.CariKarari, kararHareketId = h.KararHareketId };
    }

    /// <summary>Borçtan düşmeyi geri alır: TAHSILAT stornolanır (tek storno motoru), karar boşalır.</summary>
    public async Task<object> KararGeriAl(string bankaHareketId, AktifKullanici islem)
    {
        var h = await db.BankaHareketleri.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == bankaHareketId && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Banka hareketi bulunamadı");
        if (h.CariKarari == CariKarari.Satis)
        {
            var t = await db.BankaHareketleri.FirstAsync(x => x.Id == h.Id && x.TenantId == islem.TenantId);
            t.CariKarari = null;
            denetim.YazmaKaydet(islem, "BankaOdemeKarari", h.Id, "karar geri alındı (SATIS → bekliyor)", hassas: false);
            await db.SaveChangesAsync();
            return new { id = h.Id, cariKarari = (string?)null };
        }
        if (h.CariKarari != CariKarari.Borc || h.KararHareketId is null)
            throw new ConflictException("Bu ödeme için verilmiş bir karar yok.");
        var kurId = await db.Hareketler.AsNoTracking()
            .Where(x => x.Id == h.KararHareketId && x.TenantId == islem.TenantId)
            .Select(x => x.KurSnapshotId).FirstAsync();
        await storno.Storno(h.KararHareketId, new Satis.StornoDto
        { Gerekce = "Banka ödemesinin borçtan düşülmesi geri alındı", KurSnapshotId = kurId }, islem);
        return new { id = h.Id, cariKarari = (string?)null };
    }

    private static void KararaUygunMu(BankaHareket h)
    {
        if (h.Yon != HareketYonu.BankaGelen)
            throw new ConflictException("Yalnız gelen ödeme için karar verilir.");
        if (h.CariId is null)
            throw new ConflictException("Önce ödemeyi bir müşteriyle eşleştirin.");
        if (h.StornoKaynakId is not null || h.KarsiBacakId is not null)
            throw new ConflictException("Bu satır için karar verilemez (storno ya da virman bacağı).");
        if (h.ParaBirimi != "TRY")
            throw new ConflictException("Döviz hesabına gelen ödeme müşterinin döviz tahsilatından girilir.");
        if (h.Durum != "ESLESTI")
            throw new ConflictException($"Bu dekont için karar verilemez (durum: {h.Durum}).");
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/bankalar")]
[Yetki.ModulYetkisi("banka")]
public sealed class GelenOdemeController(GelenOdemeServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/bankalar/karar-bekleyen — müşteriye bağlı, kararı verilmemiş gelen ödemeler.</summary>
    [HttpGet("karar-bekleyen")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> KararBekleyenler() => servis.KararBekleyenler(Islem);

    /// <summary>POST /api/v1/bankalar/hareketler/{hareketId}/karar — BORC (borcundan düş) | SATIS.</summary>
    [HttpPost("hareketler/{hareketId}/karar")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Karar(string hareketId, [FromBody] BankaKararDto dto) => servis.Karar(hareketId, dto, Islem);

    /// <summary>POST /api/v1/bankalar/hareketler/{hareketId}/karar/geri-al — kararı geri al (TAHSILAT stornolanır; silme YOK).</summary>
    [HttpPost("hareketler/{hareketId}/karar/geri-al")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> KararGeriAl(string hareketId) => servis.KararGeriAl(hareketId, Islem);
}
