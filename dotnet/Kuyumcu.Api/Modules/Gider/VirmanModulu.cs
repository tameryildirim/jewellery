using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kuyumcu.Api.Modules.Gider;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-19 (Y-11) — VİRMAN (kasa ↔ kasa · kasa ↔ banka · banka ↔ banka)
//
// ⚠ TEK OLAY, İKİ SATIR. Para dükkânın DIŞINA çıkmaz; yalnız yer değiştirir.
// Bu yüzden virman:
//   • CİROYA GİRMEZ (hareketler defterine YAZILMAZ — orası mal/ciro defteri),
//   • GİDER DEĞİLDİR (gider_kategori_id NULL'dur),
//   • kasa/banka bakiyelerinde TOPLAMDA SIFIR etki yapar.
//
// ⚠ RİSK (plan § 2, P-19): "virman kasa/banka bakiyesini ÇİFT sayabilir."
// Çift sayma iki yoldan doğar ve ikisi de burada kapatılır:
//   1. Aynı parayı hem çıkış hem giriş bacağında CİRO sayma → defter yazımı YOK.
//   2. Bacaklardan yalnız birini yazma (yarım virman) → CiftBacakFabrikasi
//      ikisini TEK çağrıda, TEK SaveChangesAsync ile, TEK transaction içinde
//      yazar; tek bacak yazmak derleyici ve sayaç düzeyinde imkânsızdır.
//
// ⚠ ŞUBELER ARASI NAKİT TRANSFERİ: iki kasa farklı şubelerdeyse virman "iki
// şube birden" kapsamına girer; her bacak KENDİ kasasının şubesine aittir ve
// P-04 şube süzgeci her bacağı kendi şubesinde gösterir (tek satırlık bir
// "karsi_sube_id" alanı bu çift görünürlüğü sağlayamazdı — bkz. anlam ayrımı,
// Data/HareketFabrikasi.cs).
//
// ⚠ DÖVİZ ÇEVRİMİ KAPSAM DIŞI: para birimleri farklıysa 400 döner. Çevrim kuru
// seçimi (alış/satış/serbest) bir MALİ karardır, uydurulmaz.
// ═════════════════════════════════════════════════════════════════════════════

public static class VirmanTarafi
{
    public const string Kasa = "KASA";
    public const string Banka = "BANKA";
    public static readonly string[] Hepsi = [Kasa, Banka];
    public const string Desen = "KASA|BANKA";
}

public sealed class VirmanDto
{
    [Required(ErrorMessage = "Kaynak türü zorunludur")]
    [RegularExpression(VirmanTarafi.Desen, ErrorMessage = "Kaynak türü yalnız KASA veya BANKA olabilir")]
    public string KaynakTur { get; set; } = null!;

    [Required(ErrorMessage = "Kaynak seçilmelidir")]
    [MaxLength(36)] public string KaynakId { get; set; } = null!;

    [Required(ErrorMessage = "Hedef türü zorunludur")]
    [RegularExpression(VirmanTarafi.Desen, ErrorMessage = "Hedef türü yalnız KASA veya BANKA olabilir")]
    public string HedefTur { get; set; } = null!;

    [Required(ErrorMessage = "Hedef seçilmelidir")]
    [MaxLength(36)] public string HedefId { get; set; } = null!;

    // SON-KALANLAR tur 2 (doğrulayıcı DÜŞÜK): alt sınır TEK kapıda — servis KolonTavani.PozitifTutar
    // (önce 2 haneye yuvarla, sonra en az 0,01); DTO [Range(0.01)] kaldırıldı (uçlar arası tutarsızlık).
    [Tutar]   // PAKET GOC-A (K9) — Core kuralı
    public decimal Tutar { get; set; }

    [MaxLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
    public string? Aciklama { get; set; }

    /// <summary>
    /// PAKET PARA-DEMO tur 2 — kasadan aktarılan paranın ARACI: NAKIT (varsayılan,
    /// bugünkü davranış) · KART · HAVALE. POS cihazı / banka hesabı tanımsız dükkânda
    /// kart ve havale tahsilatı kasaya yazılır; bu para bankaya virmanla aktarılırken
    /// NAKİT damgası alırsa gün sonu nakit defteri (bakiye − Σkart − Σhavale) düşer ve
    /// sayımda sahte FAZLA çıkar. Boş = NAKIT. Yalnız kasa bacaklarına damga olur.
    /// </summary>
    [RegularExpression("NAKIT|KART|HAVALE", ErrorMessage = "Aktarılan para yalnız NAKIT, KART veya HAVALE olabilir")]
    public string? KasaAraci { get; set; }
}

/// <summary>Bir virman bacağının çözülmüş hâli: hesap/kasa + para birimi + şube.</summary>
internal sealed record VirmanTaraf(
    string Tur, string Id, string Ad, string ParaBirimi, decimal Bakiye, bool Aktif, string? SubeId);


// ── SUNUCU ÇIKTI TİPLERİ (sözleşme kilidi — bkz. GiderModulu.cs başlığı) ─────
public sealed record VirmanCiktisi(
    string CikisBacakId, string GirisBacakId,
    string KaynakTur, string KaynakId, string? KaynakAdi,
    string HedefTur, string HedefId, string? HedefAdi,
    decimal Tutar, string ParaBirimi, string? Aciklama, DateTime Tarih,
    bool SubelerArasi, string StornoYolu);

public sealed record VirmanSatirCiktisi(
    string CikisBacakId, string? GirisBacakId, string KaynakTur, string? KaynakAdi,
    string HedefTur, string? HedefAdi, decimal Tutar, string ParaBirimi,
    string? Aciklama, DateTime Tarih, bool StornoSatiri);

public sealed record VirmanListesi(
    List<VirmanSatirCiktisi> Satirlar, int Toplam, int Limit, bool DahaFazlasiVar, string? SinirNotu);

public sealed record VirmanStornoCiktisi(
    string[] KaynakBacakIdleri, string[] StornoBacakIdleri, int BacakSayisi,
    string Gerekce, DateTime Tarih);

public sealed class VirmanServisi(
    KuyumcuDbContext db, ILogger<VirmanServisi> logger, DenetimServisi? denetimServisi = null)
{
    private DenetimServisi? denetim => denetimServisi;

    public async Task<VirmanCiktisi> Olustur(VirmanDto dto, AktifKullanici islem)
    {
        // ── Servis içi ELLE doğrulama: DataAnnotations yalnız controller yolunda.
        var kaynakTur = (dto.KaynakTur ?? "").Trim().ToUpperInvariant();
        var hedefTur  = (dto.HedefTur ?? "").Trim().ToUpperInvariant();
        if (!VirmanTarafi.Hepsi.Contains(kaynakTur) || !VirmanTarafi.Hepsi.Contains(hedefTur))
            throw new BadRequestException("Virman tarafı yalnız KASA veya BANKA olabilir.");
        // PAKET PATCH-5: önce yuvarla, sonra ≤ 0 (0,004 → 0,00 bacak doğmaz) + kolon tavanı decimal(15,2).
        // PAKET GOC-A (K9): 2'den fazla ondalık SESSİZCE YUVARLANMAZ — Core kuralı + sözlük metni (ekranla aynı).
        GirdiKapisi.Tutar(dto.Tutar, "tutar");
        var tutar = KolonTavani.PozitifTutar(dto.Tutar, "tutar", "Virman tutarı");
        if (kaynakTur == hedefTur && dto.KaynakId == dto.HedefId)
            throw new BadRequestException(
                "Virmanın kaynağı ve hedefi aynı olamaz; para kendi içine aktarılamaz.");

        var kaynak = await Taraf(kaynakTur, dto.KaynakId, islem.TenantId, "Kaynak");
        var hedef  = await Taraf(hedefTur, dto.HedefId, islem.TenantId, "Hedef");

        if (!kaynak.Aktif) throw new BadRequestException($"Kaynak ({kaynak.Ad}) pasiftir; virman yapılamaz.");
        if (!hedef.Aktif)  throw new BadRequestException($"Hedef ({hedef.Ad}) pasiftir; virman yapılamaz.");

        if (kaynak.ParaBirimi != hedef.ParaBirimi)
            throw new BadRequestException(
                $"Para birimleri farklı: kaynak {kaynak.ParaBirimi}, hedef {hedef.ParaBirimi}. "
                + "Virman çevrim YAPMAZ; hangi kurdan çevrileceği mali bir karardır. "
                + "Önce döviz işlemini kaydedin, sonra aynı para biriminde virman yapın.");

        // PAKET PARA-DEMO tur 2 — aktarılan aracın kapısı (servis içi ELLE doğrulama).
        var arac = string.IsNullOrWhiteSpace(dto.KasaAraci)
            ? OdemeAraci.Nakit : dto.KasaAraci.Trim().ToUpperInvariant();
        if (arac != OdemeAraci.Nakit && !Kasa.KasaNakitAyrimi.NakitDisiAraclar.Contains(arac))
            throw new BadRequestException("Aktarılan para yalnız nakit, kart ya da havale olabilir.");
        if (arac != OdemeAraci.Nakit)
        {
            if (kaynak.Tur != VirmanTarafi.Kasa)
                throw new BadRequestException(
                    "Kart / havale payı yalnız KASADAN çıkan virmanda seçilir; banka hesabının böyle bir ayrımı yoktur.");
            var ayrim = await Kasa.KasaNakitAyrimi.Hesapla(db, islem.TenantId, kaynak.Id, kaynak.Bakiye);
            var pay = arac == OdemeAraci.Kart ? ayrim.Kart : ayrim.Havale;
            if (tutar > pay)
                throw new BadRequestException(
                    $"'{kaynak.Ad}' kasasındaki {(arac == OdemeAraci.Kart ? "kart" : "havale")} tahsilatı "
                    + $"{Kasa.KasaBakiyeKapisi.ParaMetni(pay, kaynak.ParaBirimi)}; "
                    + $"{Kasa.KasaBakiyeKapisi.ParaMetni(tutar, kaynak.ParaBirimi)} aktarılamaz.");
        }

        // Nakit kasa eksiye düşemez. Banka hesabında bu kapı YOKTUR ve bilinçlidir:
        // KMH (kredili mevduat) negatif bakiyeye izin verir; kuralı kurucu/mali
        // müşavir belirler (plan § 4 açık kalem) — uydurulmaz.
        // PAKET P-44: kopya kontrol yerine TEK MERKEZ (Kasa.KasaBakiyeKapisi).
        if (kaynak.Tur == VirmanTarafi.Kasa)
        {
            Kasa.KasaBakiyeKapisi.Dogrula(kaynak.Ad, kaynak.Bakiye, kaynak.ParaBirimi, -tutar);
            // PARA-DEMO-2 (madde 3): NAKİT virmanın üst sınırı kasanın NAKİT payıdır (kart/havale payı
            // "Ne aktarılıyor?" seçicisiyle kendi aracıyla aktarılır).
            await Kasa.KasaBakiyeKapisi.NakitDogrula(db, islem.TenantId, kaynak.Id, kaynak.Ad,
                kaynak.Bakiye, kaynak.ParaBirimi, -tutar, arac);
        }

        var now = DateTime.UtcNow;
        if (kaynak.Tur == VirmanTarafi.Kasa) await Kasa.GunKapanisiKapisi.Dogrula(db, islem.TenantId, kaynak.Id, now);
        if (hedef.Tur  == VirmanTarafi.Kasa) await Kasa.GunKapanisiKapisi.Dogrula(db, islem.TenantId, hedef.Id, now);

        var aciklama = string.IsNullOrWhiteSpace(dto.Aciklama) ? null : dto.Aciklama.Trim();
        var etiket = $"VİRMAN: {kaynak.Ad} → {hedef.Ad}" + (aciklama is null ? "" : $" · {aciklama}");
        // PAKET PATCH-5: kasa bacağının açıklaması kasa_hareketleri.aciklama varchar(500). Etiket
        // kasa/hesap adlarını da taşıdığı için 500 karakterlik açıklama + adlar 500'ü aşıp MySQL 1406
        // → 500 veriyordu. Metin KIRPILMAZ (sessiz kayıp); kalan yer söylenir, 400 alan:aciklama.
        if ((kaynak.Tur == VirmanTarafi.Kasa || hedef.Tur == VirmanTarafi.Kasa) && etiket.Length > 500)
            throw new BadRequestException(
                $"Açıklama çok uzun: kasa/hesap adlarıyla birlikte en fazla 500 karakter yazılabilir "
                + $"(bu virmanda açıklamaya en fazla {Math.Max(0, 500 - (etiket.Length - aciklama!.Length))} karakter kalıyor).")
                .AlanIle("aciklama");
        // Bakiye kolonları decimal(15,2): sonuç sığmazsa 400 alan:tutar (MySQL 1264 ağına bırakılmaz).
        KolonTavani.BakiyeSigar(kaynak.Bakiye - tutar, "tutar", "Virman tutarı");
        KolonTavani.BakiyeSigar(hedef.Bakiye + tutar, "tutar", "Virman tutarı");

        await using var tx = await db.Database.BeginTransactionAsync();

        // Kasa→kasa aktarımında kart/havale payı hedefte de AYNI araçla durur (çekmeceye
        // nakit girmez); banka bacağında araç kolonu yoktur.
        var cikis = BacakYap(kaynak, islem.TenantId, -tutar, etiket, now, arac);
        var giris = BacakYap(hedef,  islem.TenantId, +tutar, etiket, now, arac);

        // ⚠ TEK NOKTA: iki bacak burada karşılıklı bağlanır ve BİRLİKTE eklenir.
        CiftBacakFabrikasi.BacaklariEsle(db, cikis, giris);

        await BakiyeIsle(kaynak, -tutar, now);
        await BakiyeIsle(hedef,  +tutar, now);

        denetim?.YazmaKaydet(islem, "Virman", cikis.Id,
            $"virman {kaynak.Tur}:{kaynak.Ad} → {hedef.Tur}:{hedef.Ad} · "
            + $"{tutar.ToString("0.00", CultureInfo.InvariantCulture)} {kaynak.ParaBirimi} · "
            + $"bacaklar {cikis.Id} / {giris.Id}",
            hassas: false,
            kasaId: kaynak.Tur == VirmanTarafi.Kasa ? kaynak.Id : null);

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] VIRMAN: {KaynakTur}/{KaynakId} → {HedefTur}/{HedefId} tutar={Tutar} bacaklar={A}/{B}",
            islem.TenantId, kaynak.Tur, kaynak.Id, hedef.Tur, hedef.Id, tutar, cikis.Id, giris.Id);

        return new VirmanCiktisi(
            cikis.Id, giris.Id,
            kaynak.Tur, kaynak.Id, kaynak.Ad,
            hedef.Tur, hedef.Id, hedef.Ad,
            tutar, kaynak.ParaBirimi, aciklama, now,
            kaynak.SubeId is not null && hedef.SubeId is not null && kaynak.SubeId != hedef.SubeId,
            $"/api/v1/virman/{cikis.Id}/storno");
    }

    /// <summary>
    /// Virman listesi — ÇİFTLER hâlinde. Tek bacak gösterilmez: kullanıcı
    /// "para nereden nereye gitti" sorusunu tek satırda görmelidir.
    /// </summary>
    public async Task<VirmanListesi> Listele(AktifKullanici islem, int limit = 50)
    {
        if (limit is < 1 or > 200) throw new BadRequestException("limit 1-200 arasında olmalıdır.");

        var kasaBacaklari = await db.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.KarsiBacakId != null)
            .OrderByDescending(h => h.Tarih).Take(limit * 2)
            .Select(h => new { h.Id, h.KarsiBacakId, h.KasaId, h.Tutar, h.ParaBirimi, h.Aciklama, h.Tarih, h.StornoKaynakId })
            .ToListAsync();

        var bankaBacaklari = await db.BankaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.KarsiBacakId != null)
            .OrderByDescending(h => h.Tarih).Take(limit * 2)
            .Select(h => new { h.Id, h.KarsiBacakId, h.BankaHesapId, h.Yon, h.Tutar, h.ParaBirimi, h.Aciklama, h.Tarih, h.StornoKaynakId })
            .ToListAsync();

        var kasaAdlari = await db.Kasalar.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId).ToDictionaryAsync(k => k.Id, k => k.Ad);
        var hesapAdlari = await db.BankaHesaplari.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId).ToDictionaryAsync(h => h.Id, h => h.HesapAdi);

        var kasaKimlikleri = kasaBacaklari.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var bankaKimlikleri = bankaBacaklari.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);

        string KarsiTur(string id) =>
            kasaKimlikleri.Contains(id) ? VirmanTarafi.Kasa
          : bankaKimlikleri.Contains(id) ? VirmanTarafi.Banka
          // ⚠ "BILINMIYOR" UYDURMA DEĞİL: karşı bacak listenin penceresi dışında
          //   kalmış olabilir. Sessizce "KASA" demek yanlış bilgi üretirdi.
          : "BILINMIYOR";

        string? KarsiAd(string id)
        {
            var k = kasaBacaklari.FirstOrDefault(x => x.Id == id);
            if (k is not null) return kasaAdlari.TryGetValue(k.KasaId, out var a) ? a : null;
            var b = bankaBacaklari.FirstOrDefault(x => x.Id == id);
            if (b is not null) return hesapAdlari.TryGetValue(b.BankaHesapId, out var a) ? a : null;
            return null;
        }

        // ÇİFTLER hâlinde gösterilir: kullanıcı "para nereden nereye gitti"
        // sorusunu TEK satırda görmelidir. Çıkış bacağı satırı temsil eder.
        var satirlar = new List<VirmanSatirCiktisi>();

        foreach (var k in kasaBacaklari.Where(x => x.Tutar < 0m))
            satirlar.Add(new VirmanSatirCiktisi(
                k.Id, k.KarsiBacakId, VirmanTarafi.Kasa,
                kasaAdlari.TryGetValue(k.KasaId, out var ka) ? ka : null,
                KarsiTur(k.KarsiBacakId!), KarsiAd(k.KarsiBacakId!),
                -k.Tutar, k.ParaBirimi, k.Aciklama, k.Tarih, k.StornoKaynakId is not null));

        foreach (var b in bankaBacaklari.Where(x => x.Yon == Kuyumcu.Core.Domain.HareketYonu.BankaGiden))
            satirlar.Add(new VirmanSatirCiktisi(
                b.Id, b.KarsiBacakId, VirmanTarafi.Banka,
                hesapAdlari.TryGetValue(b.BankaHesapId, out var ha) ? ha : null,
                KarsiTur(b.KarsiBacakId!), KarsiAd(b.KarsiBacakId!),
                b.Tutar, b.ParaBirimi, b.Aciklama, b.Tarih, b.StornoKaynakId is not null));

        var sirali = satirlar.OrderByDescending(x => x.Tarih).Take(limit).ToList();

        return new VirmanListesi(
            sirali, sirali.Count, limit, satirlar.Count > sirali.Count,
            $"En son {limit} virman gösterilir.");
    }

    // ── Yardımcılar ─────────────────────────────────────────────────────────

    private async Task<VirmanTaraf> Taraf(string tur, string id, string tenantId, string etiket)
    {
        if (tur == VirmanTarafi.Kasa)
        {
            var k = await db.Kasalar.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
                ?? throw new NotFoundException($"{etiket} kasa bulunamadı");
            return new VirmanTaraf(VirmanTarafi.Kasa, k.Id, k.Ad, k.ParaBirimi, k.Bakiye, k.Aktif, k.SubeId);
        }

        var h = await db.BankaHesaplari.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException($"{etiket} banka hesabı bulunamadı");
        return new VirmanTaraf(VirmanTarafi.Banka, h.Id, h.HesapAdi, h.ParaBirimi, h.Bakiye, h.Aktif, null);
    }

    /// <summary>
    /// Bacak taslağı — <paramref name="isaretliTutar"/> NEGATİF ise çıkış,
    /// POZİTİF ise giriştir. <c>KarsiBacakId</c> BURADA DOLDURULMAZ (fabrika işi).
    /// </summary>
    internal static IBacakliHareket BacakYap(
        VirmanTaraf taraf, string tenantId, decimal isaretliTutar, string aciklama, DateTime now,
        string kasaAraci = OdemeAraci.Nakit)
        => taraf.Tur == VirmanTarafi.Kasa
            ? new KasaHareket
            {
                Id = Kimlik.Ulid(),
                TenantId = tenantId,
                KasaId = taraf.Id,
                HareketId = null,
                Tutar = isaretliTutar,
                ParaBirimi = taraf.ParaBirimi,
                OdemeTuru = kasaAraci,
                KomisyonOran = 0m,
                GiderKategoriId = null,      // virman GİDER DEĞİLDİR
                Aciklama = aciklama,
                Tarih = now,
                Olusturuldu = now,
            }
            : new BankaHareket
            {
                Id = Kimlik.Ulid(),
                TenantId = tenantId,
                BankaHesapId = taraf.Id,
                Yon = isaretliTutar < 0m
                    ? Kuyumcu.Core.Domain.HareketYonu.BankaGiden
                    : Kuyumcu.Core.Domain.HareketYonu.BankaGelen,
                Tutar = Math.Abs(isaretliTutar),
                ParaBirimi = taraf.ParaBirimi,
                Aciklama = aciklama,
                CariId = null,
                FaturaId = null,
                HareketId = null,
                EkstreSatirId = null,
                BankaReferans = null,
                DedupeAnahtar = null,
                // ⚠ "ESLESTI": virman bacağının eşleşecek CARİSİ YOKTUR; "BEKLIYOR"
                // bırakılsaydı eşleşme bekleyen dekont listesine düşer ve faturaya
                // dönüştürülebilirdi (para dükkân içinde kaldığı hâlde fatura doğardı).
                Durum = "ESLESTI",
                Tarih = now,
                Olusturuldu = now,
            };

    private async Task BakiyeIsle(VirmanTaraf taraf, decimal isaretliTutar, DateTime now)
    {
        if (taraf.Tur == VirmanTarafi.Kasa)
        {
            var k = await db.Kasalar.FirstAsync(x => x.Id == taraf.Id);
            await BakiyeKilidi.Kilitle(db, k);   // PAKET KAPORA-ESZAMAN tur 2: kilitli taze okuma (kayıp güncelleme)
            k.Bakiye = Math.Round(k.Bakiye + isaretliTutar, 2, MidpointRounding.AwayFromZero);
            k.Guncellendi = now;
        }
        else
        {
            var h = await db.BankaHesaplari.FirstAsync(x => x.Id == taraf.Id);
            await BakiyeKilidi.Kilitle(db, h);   // PAKET KAPORA-ESZAMAN tur 2: kilitli taze okuma (kayıp güncelleme)
            h.Bakiye = Math.Round(h.Bakiye + isaretliTutar, 2, MidpointRounding.AwayFromZero);
            h.Guncellendi = now;
        }
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/virman")]
[Yetki.ModulYetkisi("kasa")]
public sealed class VirmanController(VirmanServisi servis, Satis.StornoServisi storno) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<VirmanCiktisi> Olustur([FromBody] VirmanDto dto) => servis.Olustur(dto, Islem);

    [HttpGet]
    public Task<VirmanListesi> Listele([FromQuery, Range(1, 200)] int limit = 50) => servis.Listele(Islem, limit);

    /// <summary>
    /// POST /api/v1/virman/{bacakId}/storno — İKİ BACAK BİRLİKTE döner.
    /// İş mantığı burada DEĞİL: tek storno motoru (StornoServisi).
    /// </summary>
    [HttpPost("{bacakId}/storno")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<VirmanStornoCiktisi> Storno(string bacakId, [FromBody] GerekceDto dto)
        => storno.VirmanStorno(bacakId, dto.Gerekce, Islem);
}
