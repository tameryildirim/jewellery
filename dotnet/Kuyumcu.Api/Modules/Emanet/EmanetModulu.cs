using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kuyumcu.Api.Modules.Emanet;

// ═════════════════════════════════════════════════════════════════════════════
// EMANET MODÜLÜ — müşteri altınının teslim alınması (giriş) ve iadesi (çıkış).
// Paralel defterde yalnız has gram bacağı çalışır (tlTutari = 0).
//   • Giriş:  hasGramBakiye += hasGrami (bizde müşterinin altını var)
//   • Çıkış:  hasGramBakiye -= hasGrami (iade — mevcut bakiye yeterli olmalı)
// Kaynak: apps/api/src/modules/emanet/*
// Desen: Modules/Kasa/KasaModulu.cs (tek dosya: DTO'lar + Servis + Controller)
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class EmanetGirisDto
{
    /// <summary>Müşteri cari ID</summary>
    [Required] public string CariId { get; set; } = null!;

    /// <summary>Emanet altının ayarı</summary>
    [Required, Kuyumcu.Core.Domain.AyarId]
    public string AyarId { get; set; } = null!;

    /// <summary>Fiziksel gram</summary>
    // PAKET GOC-A (envanter C · e-gram): üst sınır double.MaxValue, ondalık sınırı yoktu — K10 Core kuralı.
    [Required, Gram]
    public decimal GramAgirlik { get; set; }

    /// <summary>İsteğe bağlı taş ağırlığı</summary>
    /// <remarks>GOC-A (K10): ≥ 0, 3 ondalık; "taş &lt; brüt" servis içinde (Core AgirlikKurali.Tas).</remarks>
    [Range(0, 999_999_999.999)] public decimal? TasAgirlik { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;
}

public sealed class EmanetCikisDto
{
    /// <summary>
    /// İade edilen has gram — girişte kayıtlı değerle aynı olmalı,
    /// fark varsa aciklama zorunlu.
    /// </summary>
    // PAKET GOC-A (K11): > 0, en çok 6 ondalık, decimal(15,6) tavanı — Core kuralı.
    [Required, Has]
    public decimal HasGrami { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    /// <summary>
    /// İadenin MADENİ (paket 20, K20-9). Boş bırakılırsa ALTIN varsayılır —
    /// eski istemciler bozulmasın diye; ama o madenin NET bakiyesi yetmiyorsa
    /// istek reddedilir. Gümüş yatıran müşteri altın çekemez.
    /// </summary>
    [MaxLength(10)] public string? Maden { get; set; }

    /// <summary>
    /// PAKET P-DIL-BORC-KASA (Y18) — iade BIRAKILDIĞI GİBİ girilir: ayar + gram (isteğe bağlı çift).
    /// Verilirse has SUNUCUDA girişle aynı has fonksiyonuyla (EmanetHas) hesaplanır ve gövdedeki
    /// <see cref="HasGrami"/> bununla en çok 0,001 g sapabilir (aşarsa 400). Deftere gövdedeki has yazılır.
    /// Maden ayardan gelir; gövdede ayrıca maden varsa aynı olmalıdır. Verilmezse eski yol (yalnız has).
    /// </summary>
    [MaxLength(10)] public string? AyarId { get; set; }

    /// <summary>PAKET P-DIL-BORC-KASA (Y18) — iade edilen net gram (AyarId ile birlikte).</summary>
    [Gram] public decimal? GramAgirlik { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;
}

/// <summary>
/// PAKET P-12 (B-43) — emanet hareketinin düzeltilebilir TEK alanı.
/// <para>
/// ⚠ Gram/ayar/maden/cari/tarih ALANI YOKTUR ve eklenmeyecektir: hepsi emanet
/// bakiyesini doğurur, düzeltme yolu STORNO + yeni kayıttır (PLAN § 1 md.8).
/// </para>
/// </summary>
public sealed class EmanetHareketDuzeltDto
{
    private string? aciklama;
    [System.Text.Json.Serialization.JsonIgnore] public bool AciklamaVerildi { get; private set; }
    [MaxLength(500)]
    public string? Aciklama { get => aciklama; set { aciklama = value; AciklamaVerildi = true; } }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET CIFT-BACAK (30.09) — EMANET KURALI: TEK KAYNAK.
//
// ÖLÇÜLEN KUSUR (senaryo 5.3/5.5/9.6): emanet giriş/çıkışı altın dışı madeni
// cari has kolonuna YAZMIYORDU (K20-2), ama storno motoru madene bakmadan
// tersliyordu ve kural aynası (CariModulu.CariHasEtkisi) "storno satırı
// madenden bağımsız" diyerek aynı hatayı DOĞRU sayıyordu: 138,75 g gümüş
// stornosu müşteriyi 138,75 g ALTIN borçlu gösterdi, ayna 0 uyuşmazlık dedi.
// Kök neden: kural üç yerde AYRI yazılmıştı. Artık yazan (bu modül), tersleyen
// (StornoServisi) ve ayna (CariModulu) AYNI fonksiyonu çağırır; biri değişirse
// üçü birlikte değişir.
//
// Elde kalan emanet (senaryo 5.3/9.3): iade kapısı ile storno kapısı aynı
// toplamı okur — iade edilmiş emanetin girişi stornolanıp defter eksiye
// düşürülemez.
// ═════════════════════════════════════════════════════════════════════════════
public static class EmanetKurali
{
    /// <summary>
    /// Emanet hareketinin (ya da onun stornosunun) cari has kolonuna yazdığı
    /// delta. Bakiye sözleşmesi K4: <c>has_gram_bakiye += −hasGrami</c>;
    /// K20-2: kolon ALTIN has gramıdır, altın dışı maden kolona YAZILMAZ.
    /// Storno satırı kaynağın madenini taşır (K20-1), dolayısıyla aynı kural
    /// storno satırı için de geçerlidir — "madenden bağımsız storno" YOKTUR.
    /// </summary>
    public static decimal CariHasEtkisi(string? maden, decimal hasGrami) =>
        CariHasKolonunaGirer(maden) ? -hasGrami : 0m;

    /// <summary>
    /// Emanet hareketi cari has kolonuna girer mi — yalnız ALTIN. Madeni boş
    /// (K20 öncesi) satır GİRMEZ: aynanın bugünkü tanımı (KESIF-P2) birebir
    /// korunur; yazan kod zaten her satıra ayarın madenini yazar.
    /// </summary>
    public static bool CariHasKolonunaGirer(string? maden) => maden == Maden.Altin;

    /// <summary>
    /// Elimizde fiilen duran emanet has gramı — <b>TEK MADEN İÇİN</b> (K20-9).
    /// Defterden okunur (giriş + çıkış toplamı, storno satırları ters işaretle
    /// doğal olarak sayılır); gruplama sunucuda yapılır (TB-19). İade kapısı
    /// (<see cref="EmanetServisi"/>) ve storno kapısı (StornoServisi) bunu okur.
    /// </summary>
    /// <summary>
    /// PAKET P-DIL-BORC-KASA (Y18) — carilerin TEK MADENDEKİ emanetinin "bırakıldığı gibi" kalanı.
    /// Tek gruplu sorgu (TB-19); kural <see cref="EmanetBirakildigi.Ozetle"/> (Core, tek kaynak).
    /// Sözlükte olmayan cari = emanet satırı yok. Değer null = bilinmiyor (ekran has gösterir).
    /// </summary>
    public static async Task<Dictionary<string, IReadOnlyList<EmanetAyarSatiri>?>> BirakildigiGibi(
        KuyumcuDbContext db, string tenantId, IReadOnlyCollection<string> cariIdler, string maden, string? sube = null)
    {
        if (cariIdler.Count == 0) return [];
        var gruplar = await db.Hareketler.AsNoTracking().Daralt(sube)
            .Where(h => h.TenantId == tenantId && h.CariId != null && cariIdler.Contains(h.CariId)
                     && (h.Tur == "EMANET_GIRIS" || h.Tur == "EMANET_CIKIS") && h.Maden == maden)
            .GroupBy(h => new { CariId = h.CariId!, h.EmanetAyarId })
            .Select(g => new
            {
                g.Key.CariId, g.Key.EmanetAyarId,
                Gram = g.Sum(h => h.EmanetGram ?? 0m),
                Tas  = g.Sum(h => h.EmanetTas ?? 0m),
                Has  = g.Sum(h => h.HasGrami),
            })
            .ToListAsync();
        return gruplar.GroupBy(g => g.CariId).ToDictionary(
            c => c.Key,
            c => EmanetBirakildigi.Ozetle(c.Select(g => new EmanetIz(g.EmanetAyarId, g.EmanetAyarId is null ? null : g.Gram, g.Tas, g.Has))));
    }

    public static async Task<decimal> EldeKalan(
        KuyumcuDbContext db, string cariId, string tenantId, string maden, string? sube = null) =>
        await db.Hareketler.AsNoTracking().Daralt(sube)
            .Where(h => h.TenantId == tenantId && h.CariId == cariId
                     && (h.Tur == "EMANET_GIRIS" || h.Tur == "EMANET_CIKIS")
                     && h.Maden == maden)
            .SumAsync(h => h.HasGrami);
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class EmanetServisi(
    KuyumcuDbContext db, ILogger<EmanetServisi> logger, DenetimServisi? denetim = null)
{
    // ─── Emanet girişi ────────────────────────────────────────────────────────
    /// <summary>
    /// Müşterinin altınını teslim alır.
    /// hasGramBakiye -= hasGrami — işaret sözleşmesi (K4, Bertan kararı 2026-09-10):
    /// cari_bakiyeler.has_gram_bakiye ALACAK perspektifidir; artı = müşteri bize
    /// borçlu. Emanette biz borçlanırız, bakiye eksiye gider (TL tarafıyla simetrik).
    /// hareketler: EMANET_GIRIS, tlTutari=0, hasGrami=+hasGrami (fiziki akış: bize girdi)
    /// </summary>
    public async Task<object> Giris(EmanetGirisDto dto, AktifKullanici islem)
    {
        if (islem.SubeId is null)
            throw new BadRequestException("Emanet için şube seçimi gerekli");

        // K1: cari bu kiracıya ait olmalı (doğrulama yoktu — yabancı cariye emanet yazılabiliyordu)
        await IslemDogrulayici.CariDogrula(db, dto.CariId, islem.TenantId);
        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);

        var ayar = Ayarlar.Bul(dto.AyarId)
            ?? throw new BadRequestException($"Geçersiz ayar: {dto.AyarId}");

        // PAKET GOC-A (envanter açık #5): brüt gram K10 + taş K10 ("taş < brüt"). Eskiden taş ≥ gram
        // has'ı ≤ 0 yapıp "bu madenin has gramı hesaplanmaz" diye YANLIŞ sebeple reddediliyordu.
        GirdiKapisi.Gram(dto.GramAgirlik, "gramAgirlik");
        GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.AgirlikKurali.Tas(dto.TasAgirlik, dto.GramAgirlik), "tasAgirlik");
        var tasAgirlik = dto.TasAgirlik ?? 0m;
        var netGram    = dto.GramAgirlik - tasAgirlik;
        var hasGrami   = EmanetHas(netGram, ayar);
        var maden      = ayar.Maden;
        var now        = DateTime.UtcNow;

        // K20-5: kıymetsiz madenin (çelik) has gramı yoktur; emanet has bacağı
        // üzerinden izlendiği için böyle bir emanet DEFTERE YAZILAMAZ.
        if (!ayar.Kiymetli || hasGrami <= 0m)
            throw new BadRequestException(
                $"{Maden.Ad(maden)} emaneti bu ekrandan alınamaz: bu madenin has gramı " +
                "hesaplanmaz ve emanet bakiyesi has gram üzerinden izlenir.");

        await using var tx = await db.Database.BeginTransactionAsync();
        HareketFabrikasi.Ekle(db,
            OdemeIzi.Yok,
            new Hareket
        {
            Id            = Kimlik.Ulid(),
            TenantId      = islem.TenantId,
            SubeId        = islem.SubeId,
            Tur           = "EMANET_GIRIS",
            Tarih         = now,
            Aciklama      = dto.Aciklama,
            TlTutari      = 0m,
            HasGrami      = hasGrami,          // pozitif: emanet aldık
            // P-DIL-BORC-KASA (Y18): bırakıldığı gibi — brüt gram, taş, ayar (gösterim; hesap has üzerinden).
            EmanetGram    = dto.GramAgirlik,
            EmanetTas     = tasAgirlik > 0m ? tasAgirlik : null,
            EmanetAyarId  = ayar.Id,
            // K20-1/K20-2: bu gram BU MADENİN gramıdır; altın toplamına karışmaz.
            Maden         = maden,
            KurSnapshotId = dto.KurSnapshotId,
            CariId        = dto.CariId,
            KullaniciId   = islem.Id,
            Olusturuldu   = now,
        });

        // ⚠ K20-2 — cari_bakiyeler.has_gram_bakiye "ALTIN has gramı" demektir.
        // Altın dışı emanet bu kolona DOKUNMAZ: 100 g gümüş emaneti bu kolona
        // yazılsaydı defterde 100 g altın borcu gibi görünürdü. Altın dışı
        // bakiye hareketlerden HESAPLANIR (kalıcı kolon paket 27'de açılacak).
        if (EmanetKurali.CariHasKolonunaGirer(maden))
            await BakiyeGuncelle(dto.CariId, islem.TenantId, 0m, EmanetKurali.CariHasEtkisi(maden, hasGrami), now);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] EMANET_GIRIS: cari={CariId} {Gram}g {AyarId} = {Has} has",
            islem.TenantId, dto.CariId, dto.GramAgirlik, dto.AyarId, hasGrami);

        return new
        {
            cariId      = dto.CariId,
            ayarId      = dto.AyarId,
            gramAgirlik = dto.GramAgirlik,
            hasGrami,
            maden,
            madenAdi    = Maden.Ad(maden),
        };
    }

    // ─── Emanet çıkışı ────────────────────────────────────────────────────────
    /// <summary>
    /// Müşteriye altınını iade eder.
    /// hasGramBakiye += hasGrami (borcumuz kapanır — K4 işaret sözleşmesi)
    /// hareketler: EMANET_CIKIS, tlTutari=0, hasGrami=-hasGrami (fiziki akış: bizden çıktı)
    ///
    /// Yeterlilik kontrolü CARİ BAKİYEDEN DEĞİL, DEFTERDEN yapılır: bakiye
    /// emanet ile ticari altın borcunu netler; altın borcu olan bir müşteri
    /// aksi halde kendi emanetini geri alamazdı (Bertan kararı, 2026-09-10).
    /// </summary>
    /// <summary>
    /// Emanetin has karşılığı — giriş ve iade AYNI çağrıdan geçer (denetim: HasHesap.ToHasGram beyaz listesi;
    /// emanet modülü tek çağrı yeri olarak kalır).
    /// </summary>
    private static decimal EmanetHas(decimal netGram, Ayar ayar) => HasHesap.ToHasGram(netGram, ayar);

    /// <summary>P-DIL-BORC-KASA (Y18): ayar·gramdan hesaplanan has ile gövdedeki has arasındaki en büyük fark.</summary>
    public const decimal IadeHasToleransi = 0.001m;

    public async Task<object> Cikis(string cariId, EmanetCikisDto dto, AktifKullanici islem)
    {
        if (islem.SubeId is null)
            throw new BadRequestException("Emanet iadesi için şube seçimi gerekli");

        await IslemDogrulayici.CariDogrula(db, cariId, islem.TenantId);
        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);

        // K20-9: çıkış TEK BİR MADENDEN yapılır ve o madenin NET bakiyesiyle
        // karşılaştırılır. Tek havuzdan bakılsaydı gümüş yatıran müşteri altın
        // çekebilirdi — defterdeki gram sayıları eşit, madenleri farklıydı.
        var maden = string.IsNullOrWhiteSpace(dto.Maden) ? Maden.Altin : dto.Maden!.Trim().ToUpperInvariant();
        if (Array.IndexOf(Maden.Hepsi, maden) < 0)
            throw new BadRequestException(
                $"Bilinmeyen maden: '{dto.Maden}'. Kabul edilenler: {string.Join(", ", Maden.Hepsi)}.");

        GirdiKapisi.Has(dto.HasGrami, "hasGrami");   // PAKET GOC-A — servis yolu da K11
        // P-DIL-BORC-KASA (Y18): ayar + gram verildiyse has sunucuda aynı Core fonksiyonuyla hesaplanır;
        // gövdedeki has ile birebir tutmazsa iade YAZILMAZ (ekran ile defter aynı sayıyı söyler).
        Ayar? iadeAyari = null;
        // Geriye uyum: gram GÖNDERİLMEDİYSE (eski istemci yalnız ayarId + has yollayabiliyordu; alan o gün
        // yoktu ve yok sayılıyordu) eski yol aynen sürer — ayar yok sayılır, iz yazılmaz.
        if (dto.GramAgirlik is not null)
        {
            if (dto.AyarId is null)
                throw new BadRequestException("İade için ayar ve gram birlikte gönderilmelidir.");
            iadeAyari = Ayarlar.Bul(dto.AyarId)
                ?? throw new BadRequestException($"Geçersiz ayar: {dto.AyarId}");
            GirdiKapisi.Gram(dto.GramAgirlik.Value, "gramAgirlik");
            if (iadeAyari.Maden != maden && !string.IsNullOrWhiteSpace(dto.Maden))
                throw new BadRequestException(
                    $"Ayar ({iadeAyari.Ad}) ile maden ({Maden.Ad(maden)}) uyuşmuyor.");
            maden = iadeAyari.Maden;
            var hesaplanan = EmanetHas(dto.GramAgirlik.Value, iadeAyari);
            // Tolerans 0,001 g has: "tümünü iade et" kalan has'ı birebir gönderir (satır satır yuvarlanmış
            // has toplamı, toplam gramdan hesaplanandan milyonda birkaç sapabilir); emanet tam kapanır.
            if (Math.Abs(hesaplanan - dto.HasGrami) > IadeHasToleransi)
                throw new BadRequestException(
                    $"Has gram ayar ve gramla uyuşmuyor. Beklenen: {hesaplanan.ToString(CultureInfo.InvariantCulture)}, " +
                    $"gönderilen: {dto.HasGrami.ToString(CultureInfo.InvariantCulture)}");
        }
        // Elimizde fiilen duran emanet = deftere yazılmış giriş/çıkışların toplamı
        var emanetNet = await EmanetNet(cariId, islem.TenantId, maden);
        if (emanetNet < dto.HasGrami)
        {
            throw new BadRequestException(
                $"Yetersiz {Maden.Ad(maden).ToLowerInvariant()} emaneti. " +
                $"Mevcut: {emanetNet.ToString("0.0000", CultureInfo.InvariantCulture)} has gram, " +
                $"Talep: {dto.HasGrami.ToString(CultureInfo.InvariantCulture)}");
        }

        var now = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();
        // PAKET KAPORA-ESZAMAN (sınıf taraması): elde kalan emanet yukarıda işlem DIŞINDA okunuyordu →
        // iki eşzamanlı teslim ikisi de geçip emaneti eksiye düşürebiliyordu (müşteriye iki kat altın).
        // Cari satırı kilitlenir, bakiye kilitten SONRA yeniden okunur.
        await db.Database.Kilitle(KilitTablosu.Cariler, cariId, islem.TenantId);
        var kilitliNet = await EmanetNet(cariId, islem.TenantId, maden);
        if (kilitliNet < dto.HasGrami)
            throw new BadRequestException(
                $"Yetersiz {Maden.Ad(maden).ToLowerInvariant()} emaneti. " +
                $"Mevcut: {kilitliNet.ToString("0.0000", CultureInfo.InvariantCulture)} has gram, " +
                $"Talep: {dto.HasGrami.ToString(CultureInfo.InvariantCulture)}");
        HareketFabrikasi.Ekle(db,
            OdemeIzi.Yok,
            new Hareket
        {
            Id            = Kimlik.Ulid(),
            TenantId      = islem.TenantId,
            SubeId        = islem.SubeId,
            Tur           = "EMANET_CIKIS",

            Tarih         = now,
            Aciklama      = dto.Aciklama,
            TlTutari      = 0m,
            HasGrami      = -dto.HasGrami,     // negatif: iade ettik
            // P-DIL-BORC-KASA (Y18): ayar + gramla girildiyse bırakıldığı gibi iz (negatif: bizden çıktı).
            EmanetGram    = iadeAyari is null ? null : -dto.GramAgirlik,
            EmanetAyarId  = iadeAyari?.Id,
            Maden         = maden,             // K20-1
            KurSnapshotId = dto.KurSnapshotId,
            CariId        = cariId,
            KullaniciId   = islem.Id,
            Olusturuldu   = now,
        });

        // Emanet borcumuz kapanır → alacak bakiyesi artı yönde düzeltilir.
        // K20-2: yalnız ALTIN bacağı cari_bakiyeler'e dokunur (girişle simetrik).
        if (EmanetKurali.CariHasKolonunaGirer(maden))
            await BakiyeGuncelle(cariId, islem.TenantId, 0m, EmanetKurali.CariHasEtkisi(maden, -dto.HasGrami), now);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] EMANET_CIKIS: cari={CariId} {Has} has iade",
            islem.TenantId, cariId, dto.HasGrami);

        return new { cariId, iadeEdilenHas = dto.HasGrami, maden, madenAdi = Maden.Ad(maden) };
    }

    // ─── Cari emanet listesi ──────────────────────────────────────────────────

    public async Task<object> CariEmanetListesi(string cariId, AktifKullanici islem, string? subeId = null)
    {
        // PAKET ŞUBE-KAPSAM-A: emanet satırının şubesi = hareketin şubesi.
        // Görünüm şubesi yalnız DARALTIR (null = tüm şubeler).
        var sube = await GorunumSubesi.Coz(db, islem, subeId);

        // Nest davranışı birebir: son 200 hareket çekilir,
        // EMANET_GIRIS/EMANET_CIKIS filtresi bellekte uygulanır.
        var liste = await db.Hareketler.AsNoTracking().Daralt(sube)
            .Where(h => h.TenantId == islem.TenantId && h.CariId == cariId)
            .OrderByDescending(h => h.Tarih)
            .Take(200)
            .ToListAsync();

        var emanetler = liste
            .Where(h => h.Tur == "EMANET_GIRIS" || h.Tur == "EMANET_CIKIS")
            .ToList();

        // ⚠ SÖZLEŞME DARALMASI (paket 20): `mevcutHasGramBakiye` artık YALNIZ
        // ALTIN emanetidir. Eskiden bütün madenlerin gramını topluyordu ve
        // 100 g gümüş ile 100 g altın aynı sayıya karışıyordu. Alan adı
        // korundu (eski istemci kırılmasın) ama anlamı daraldı; bayrak ve not
        // yanıtta AÇIKÇA taşınır, Web ile API aynı sürümde sevk edilir.
        var madenBakiyeleri = emanetler
            .GroupBy(h => string.IsNullOrWhiteSpace(h.Maden) ? Maden.Altin : h.Maden)
            .Select(g => new
            {
                maden    = g.Key,
                madenAdi = Maden.Ad(g.Key),
                gram     = Math.Round(g.Sum(h => h.HasGrami), 6),
            })
            .Where(x => x.gram != 0m)
            .OrderByDescending(x => x.gram)
            .ToList();

        var bgMap = await EmanetKurali.BirakildigiGibi(db, islem.TenantId, [cariId], Maden.Altin, sube);
        IReadOnlyList<EmanetAyarSatiri>? birakildigiGibi =
            bgMap.TryGetValue(cariId, out var bgSatir) ? bgSatir : Array.Empty<EmanetAyarSatiri>();

        return new
        {
            cariId,
            gorunumSubesi = sube,
            kapsam = GorunumSubesi.Kapsam(sube),
            mevcutHasGramBakiye = await EmanetNet(cariId, islem.TenantId, Maden.Altin, sube),
            mevcutHasGramBakiyeMadeni = Maden.Altin,
            mevcutHasGramBakiyeNotu =
                "Bu rakam YALNIZ altın emanetidir; diğer madenler madenBakiyeleri altındadır.",
            madenBakiyeleri,
            // P-DIL-BORC-KASA (Y18): ALTIN emanetinin bırakıldığı gibi kalanı ("22 ayar · 12,50 g").
            // null = bilinmiyor (eski satır / başka ayarla iade) → ekran has'a düşer; [] = emanet yok.
            birakildigiGibi,
            hareketler          = emanetler,
            // PAKET GOC-A (G1, hasım 08.10): son 200 hareket okunur; okunan satır sınıra değdiyse
            // emanet listesi TAM DEĞİLDİR → ekran sıralamayı kapatır ve sınırı yazar (UX-S29).
            sinirDegdi          = liste.Count >= 200,
            limit               = 200,
        };
    }

    // ─── Yardımcı ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Elimizde fiilen duran emanet has gramı — <b>TEK MADEN İÇİN</b> (K20-9).
    /// Defterden okunur (giriş + çıkış toplamı); gruplama sunucuda yapılır,
    /// satır listesi çekilmez (TB-19).
    /// </summary>
    private Task<decimal> EmanetNet(string cariId, string tenantId, string maden, string? sube = null) =>
        EmanetKurali.EldeKalan(db, cariId, tenantId, maden, sube);   // CIFT-BACAK: storno kapısıyla TEK toplam

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET P-12 (B-43) — EMANET HAREKETİ DÜZELTME
    //
    // Emanetin AYRI bir tablosu YOKTUR: satır doğrudan `hareketler` defterindedir
    // (Tur = EMANET_GIRIS / EMANET_CIKIS). Bu yüzden düzeltilebilir tek alan
    // AÇIKLAMAdır:
    //   • has gram / ayar / maden → BAKİYEYİ oluşturur; düzeltme yolu STORNO
    //     (48-A motoru bu iki türü zaten beyaz listesinde taşır).
    //   • tarih → defter satırının dönemidir; kaydırılırsa emanet bakiyesinin
    //     tarihsel dökümü ile cari ekstresi çelişir.
    //   • cari → emanetin SAHİBİdir; yanlış kişiye yazılmış emanet stornolanır
    //     ve doğru kişiye yeniden girilir.
    // Kilitli her alanın gerekçesi ekranda DegismezIpucu ile yazılıdır (UX-S25).
    //
    // ⚠ Üç durumlu PATCH: gövdede yoksa KORU, null ise SİL.
    // ═════════════════════════════════════════════════════════════════════════
    public async Task<object> HareketDuzelt(
        string hareketId, EmanetHareketDuzeltDto dto, AktifKullanici islem)
    {
        if (!dto.AciklamaVerildi)
            throw new BadRequestException(
                "Düzeltilecek alan gönderilmedi. Emanet hareketinde yalnız açıklama "
                + "düzeltilebilir; gram/ayar/cari düzeltmesi yalnız STORNO + yeni kayıt yoluyla yapılır.");

        var h = await db.Hareketler
            .FirstOrDefaultAsync(x => x.Id == hareketId && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Emanet hareketi bulunamadı");

        if (h.Tur is not ("EMANET_GIRIS" or "EMANET_CIKIS"))
            throw new ConflictException(
                $"Bu hareket bir emanet kaydı değil ({h.Tur}); bu uçtan düzeltilemez.");

        if (h.StornoKaynakId is not null)
            throw new ConflictException(
                "Bu satır bir STORNO kaydıdır; düzeltilemez. Doğru bilgiyi yeni bir kayıtla girin.");

        var stornolandi = await db.Hareketler.AsNoTracking()
            .AnyAsync(x => x.TenantId == islem.TenantId && x.StornoKaynakId == h.Id);
        if (stornolandi)
            throw new ConflictException(
                "Bu emanet hareketi stornolanmıştır; hükümsüz bir kaydın açıklaması değiştirilmez.");

        var yeni = string.IsNullOrWhiteSpace(dto.Aciklama) ? null : dto.Aciklama!.Trim();
        var degisti = yeni != h.Aciklama;

        if (degisti)
        {
            var eski = h.Aciklama;
            h.Aciklama = yeni;
            denetim?.YazmaKaydet(islem, "EmanetHareketiDuzeltme", h.Id,
                $"emanet hareketi düzeltildi · açıklama: '{eski ?? "—"}' → '{yeni ?? "—"}'");
            await db.SaveChangesAsync();
        }

        return new
        {
            id                = h.Id,
            tur               = h.Tur,
            aciklama          = h.Aciklama,
            hasGrami          = h.HasGrami,
            tarih             = h.Tarih,
            degisenAlanSayisi = degisti ? 1 : 0,
        };
    }

    /// <summary>Bakiye satırını delta ile günceller — çağıranın transaction'ı içinde çalışır.</summary>
    private async Task BakiyeGuncelle(
        string cariId, string tenantId, decimal tlDelta, decimal hasGramDelta, DateTime now)
    {
        // PAKET CARI-BAKIYE: TEK YAZICI (Common/CariBakiyeServisi.cs).
        await CariBakiyeYazici.Uygula(db, tenantId, cariId, tlDelta, hasGramDelta, now);
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/emanet")]
// P-09 (Y-39): yetki matrisi RotaModulleri haritasından türetilir.
[Yetki.ModulYetkisi("emanet")]
public sealed class EmanetController(EmanetServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/emanet/giris — Müşteriden altın teslim al</summary>
    [HttpPost("giris")]
    public Task<object> Giris([FromBody] EmanetGirisDto dto) => servis.Giris(dto, Islem);

    /// <summary>POST /api/v1/emanet/{cariId}/cikis — Müşteriye altını iade et</summary>
    [HttpPost("{cariId}/cikis")]
    public Task<object> Cikis(string cariId, [FromBody] EmanetCikisDto dto)
        => servis.Cikis(cariId, dto, Islem);

    /// <summary>
    /// PATCH /api/v1/emanet/hareketler/{hareketId} — PAKET P-12 (B-43).
    /// Emanet hareketinin AÇIKLAMASINI düzeltir; finansal alan değişmez.
    /// </summary>
    [HttpPatch("hareketler/{hareketId}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<object> HareketDuzelt(string hareketId, [FromBody] EmanetHareketDuzeltDto dto)
        => servis.HareketDuzelt(hareketId, dto, Islem);

    /// <summary>GET /api/v1/emanet/{cariId} — Cari bazlı emanet geçmişi + güncel bakiye</summary>
    [HttpGet("{cariId}")]
    public Task<object> CariEmanetListesi(string cariId, [FromQuery] string? subeId)
        => servis.CariEmanetListesi(cariId, Islem, subeId);
}
