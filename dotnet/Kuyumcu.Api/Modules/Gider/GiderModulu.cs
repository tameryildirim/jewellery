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
// PAKET P-19 (Y-12) — GİDER / MASRAF
//
// NE VAR: kira, elektrik, personel gibi masrafların KATEGORİSİ (soyut kayıt) ve
// bu kategoriye bağlı NAKİT ÇIKIŞI (kasa hareketi).
//
// ⚠ NEDEN KATEGORİ ZORUNLU: kategorisiz bir çıkış kâr-zararda "gider" satırına
// giremez; giren ama sınıflanmayan bir tutar, dönem sonunda "bu para nereye
// gitti" sorusunu cevapsız bırakır. Kategori uydurulmaz — kullanıcı seçer.
//
// ⚠ NEDEN HARD DELETE YOK: gider bir FİNANSAL kayıttır (PLAN § 1 md.8). Yanlış
// girilen gider SİLİNMEZ, kasa hareketi STORNO edilir (tek storno motoru).
// Kategori de silinmez, PASİFE alınır: geçmiş giderlerin sınıfı yerinde kalır.
//
// ⚠ GİDERİN İŞARETİ: kasa_hareketleri.tutar NEGATİFTİR (çıkış). Kâr-zarar
// giderTl'yi SUM(-tutar) ile okur; storno satırı POZİTİFTİR ve AYNI kategoriyi
// taşır, böylece stornolanan gider toplamdan kendiliğinden düşer (ikinci bir
// süzgeç yazılmaz).
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class GiderKategoriOlusturDto
{
    [Required(ErrorMessage = "Kategori adı zorunludur")]
    [MinLength(2, ErrorMessage = "Kategori adı en az 2 karakter olmalıdır")]
    [MaxLength(100, ErrorMessage = "Kategori adı en fazla 100 karakter olabilir")]
    public string Ad { get; set; } = null!;

    [MaxLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
    public string? Aciklama { get; set; }
}

/// <summary>
/// ÜÇ DURUMLU PATCH (sözleşme: gövdede yoksa KORU · null ise SİL).
/// <c>...Verildi</c> bayrağı JSON anahtarının VARLIĞINDAN doğar.
/// </summary>
public sealed class GiderKategoriGuncelleDto
{
    private string? ad;
    [System.Text.Json.Serialization.JsonIgnore] public bool AdVerildi { get; private set; }
    [MinLength(2, ErrorMessage = "Kategori adı en az 2 karakter olmalıdır")]
    [MaxLength(100, ErrorMessage = "Kategori adı en fazla 100 karakter olabilir")]
    public string? Ad { get => ad; set { ad = value; AdVerildi = true; } }

    private string? aciklama;
    [System.Text.Json.Serialization.JsonIgnore] public bool AciklamaVerildi { get; private set; }
    [MaxLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
    public string? Aciklama { get => aciklama; set { aciklama = value; AciklamaVerildi = true; } }

    private bool? aktif;
    [System.Text.Json.Serialization.JsonIgnore] public bool AktifVerildi { get; private set; }
    public bool? Aktif { get => aktif; set { aktif = value; AktifVerildi = true; } }
}

public sealed class GiderKaydetDto
{
    [Required(ErrorMessage = "Kasa seçilmelidir")]
    [MaxLength(36)] public string KasaId { get; set; } = null!;

    [Required(ErrorMessage = "Gider kategorisi seçilmelidir")]
    [MaxLength(36)] public string GiderKategoriId { get; set; } = null!;

    // SON-KALANLAR tur 2 (doğrulayıcı DÜŞÜK): alt sınır TEK kapıda — servis KolonTavani.PozitifTutar
    // (önce 2 haneye yuvarla, sonra en az 0,01); DTO [Range(0.01)] kaldırıldı (uçlar arası tutarsızlık).
    [Tutar]   // PAKET GOC-A (K9) — Core kuralı
    public decimal Tutar { get; set; }

    [MaxLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
    public string? Aciklama { get; set; }
}


// ── SUNUCU ÇIKTI TİPLERİ ─────────────────────────────────────────────────────
// ⚠ NEDEN ANONİM NESNE DEĞİL (PLAN § 1 md.9 · SozlesmeKapsamTestleri cırcırı):
//   istemci tipiyle sunucu yanıtı arasındaki ad/TİP kayması DERLEME HATASI
//   VERMEZ — JSON ayrıştırıcısı eşleşmeyen alanı sessizce atlar ve ekran null/0
//   gösterir. Bu kayıtlar `Kuyumcu.Contracts.Sayfalar.Gider` tipleriyle
//   SozlesmeYansimaTestleri.Ciftler() tablosunda yansımayla KİLİTLENİR.

public sealed record GiderKategoriCiktisi(
    string Id, string Ad, string? Aciklama, bool Aktif,
    DateTime Olusturuldu, DateTime Guncellendi, int GiderAdedi, decimal GiderToplamTl);

public sealed record GiderKategoriListesi(List<GiderKategoriCiktisi> Kategoriler, int Toplam);

public sealed record GiderSatirCiktisi(
    string Id, string KasaId, string? KasaAdi, string? KategoriId, string? KategoriAdi,
    decimal Tutar, string ParaBirimi, string? Aciklama, DateTime Tarih,
    bool StornoSatiri, bool Stornolandi);

/// <summary>
/// PAKET Q10-1 — gider toplamının PARA BİRİMİ başına NET (işaretli) değeri.
/// Storno satırı ters işaretli toplandığı için "125,50 + storno" = 0,00 olur.
/// </summary>
public sealed record GiderParaToplami(string ParaBirimi, decimal Tutar, int Adet);

public sealed record GiderListesi(
    List<GiderSatirCiktisi> Satirlar, int ToplamAdet, int Limit, bool DahaFazlasiVar, string? SinirNotu,
    List<GiderParaToplami> Toplamlar);

public sealed record GiderKaydiCiktisi(
    string Id, string KasaId, string? KasaAdi, string KategoriId, string KategoriAdi,
    decimal Tutar, string ParaBirimi, string? Aciklama, DateTime Tarih,
    decimal KasaBakiyesi, string StornoYolu);

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class GiderServisi(
    KuyumcuDbContext db, ILogger<GiderServisi> logger, DenetimServisi? denetimServisi = null)
{
    private DenetimServisi? denetim => denetimServisi;

    // ── Kategori (soyut kayıt) ──────────────────────────────────────────────

    public async Task<GiderKategoriCiktisi> KategoriOlustur(GiderKategoriOlusturDto dto, AktifKullanici islem)
    {
        // DataAnnotations YALNIZ controller yolunda koşar; servis doğrudan
        // çağrıldığında da kapanmalıdır (birleşik koşul).
        var ad = (dto.Ad ?? "").Trim();
        if (ad.Length < 2)
            throw new BadRequestException("Kategori adı en az 2 karakter olmalıdır.");
        if (ad.Length > 100)
            throw new BadRequestException("Kategori adı en fazla 100 karakter olabilir.");

        if (await db.GiderKategorileri.AsNoTracking()
                .AnyAsync(g => g.TenantId == islem.TenantId && g.Ad == ad))
            throw new ConflictException(
                $"'{ad}' adlı gider kategorisi zaten var. Aynı ad iki kez açılırsa gider raporu "
                + "aynı masrafı iki satıra böler.");

        var now = DateTime.UtcNow;
        var k = new GiderKategorisi
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            Ad = ad,
            Aciklama = string.IsNullOrWhiteSpace(dto.Aciklama) ? null : dto.Aciklama.Trim(),
            Aktif = true,
            Olusturuldu = now,
            Guncellendi = now,
        };
        db.GiderKategorileri.Add(k);
        denetim?.YazmaKaydet(islem, nameof(GiderKategorisi), k.Id,
            $"gider kategorisi açıldı: {ad}", hassas: false);
        await db.SaveChangesAsync();

        return Kategoriye(k, 0, 0m);
    }

    public async Task<GiderKategoriListesi> KategoriListele(AktifKullanici islem, bool? aktif)
    {
        var q = db.GiderKategorileri.AsNoTracking().Where(g => g.TenantId == islem.TenantId);
        if (aktif is { } a) q = q.Where(g => g.Aktif == a);
        var liste = await q.OrderBy(g => g.Ad).ToListAsync();

        // UX-S23: soyut kaydın ETKİSİ aynı ekranda görünür — her kategorinin
        // kaç gider satırı ve ne kadar tutar taşıdığı listeyle birlikte döner.
        var kimlikler = liste.Select(g => g.Id).ToList();
        var etkiler = await db.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId
                     && h.GiderKategoriId != null && kimlikler.Contains(h.GiderKategoriId))
            .GroupBy(h => h.GiderKategoriId!)
            .Select(g => new { KategoriId = g.Key, Adet = g.Count(), Toplam = g.Sum(x => -x.Tutar) })
            .ToListAsync();
        var harita = etkiler.ToDictionary(e => e.KategoriId, e => (e.Adet, e.Toplam));

        return new GiderKategoriListesi(
            [.. liste.Select(g =>
            {
                harita.TryGetValue(g.Id, out var e);
                return Kategoriye(g, e.Item1, e.Item2);
            })],
            liste.Count);
    }

    public async Task<GiderKategoriCiktisi> KategoriGuncelle(string id, GiderKategoriGuncelleDto dto, AktifKullanici islem)
    {
        var k = await Kategori(id, islem.TenantId);

        if (!dto.AdVerildi && !dto.AciklamaVerildi && !dto.AktifVerildi)
            throw new BadRequestException(
                "Düzeltilecek alan gönderilmedi (ad · açıklama · aktif).");

        var degisiklikler = new List<string>();

        // PAKET PATCH-5 (PATCH-3 sözleşmesi): zorunlu alan null = KORU; "" / boşluk = 400 alan.
        if (dto.AdVerildi && dto.Ad is not null)
        {
            var yeni = dto.Ad.Trim();
            if (yeni.Length < 2)
                throw new BadRequestException(
                    "Kategori adı boşaltılamaz; en az 2 karakter olmalıdır.").AlanIle("ad");
            if (yeni.Length > 100)
                throw new BadRequestException("Kategori adı en fazla 100 karakter olabilir.").AlanIle("ad");
            if (yeni != k.Ad)
            {
                if (await db.GiderKategorileri.AsNoTracking()
                        .AnyAsync(g => g.TenantId == islem.TenantId && g.Ad == yeni && g.Id != k.Id))
                    throw new ConflictException($"'{yeni}' adlı gider kategorisi zaten var.");
                degisiklikler.Add($"ad: '{k.Ad}' → '{yeni}'");
                k.Ad = yeni;
            }
        }

        if (dto.AciklamaVerildi)
        {
            var yeni = string.IsNullOrWhiteSpace(dto.Aciklama) ? null : dto.Aciklama!.Trim();
            if (yeni != k.Aciklama)
            {
                degisiklikler.Add($"açıklama: '{k.Aciklama ?? "—"}' → '{yeni ?? "—"}'");
                k.Aciklama = yeni;
            }
        }

        if (dto.AktifVerildi && dto.Aktif is { } yeniAktif)   // PATCH-5: null = KORU (zorunlu alan)
        {
            if (yeniAktif != k.Aktif)
            {
                degisiklikler.Add($"aktif: {k.Aktif} → {yeniAktif}");
                k.Aktif = yeniAktif;
            }
        }

        if (degisiklikler.Count > 0)
        {
            k.Guncellendi = DateTime.UtcNow;
            denetim?.YazmaKaydet(islem, nameof(GiderKategorisi), k.Id,
                "gider kategorisi düzeltildi · " + string.Join(" · ", degisiklikler), hassas: false);
            await db.SaveChangesAsync();
        }

        var (adet, toplam) = await KategoriEtkisi(islem.TenantId, k.Id);
        return Kategoriye(k, adet, toplam);
    }

    /// <summary>
    /// PASİFE ALMA — hard DELETE yoktur (finansal sınıflandırma; geçmiş gider
    /// satırlarının kategorisi yerinde kalır). İdempotenttir.
    /// </summary>
    public async Task<GiderKategoriCiktisi> KategoriPasifeAl(string id, AktifKullanici islem)
    {
        var k = await Kategori(id, islem.TenantId);
        if (k.Aktif)
        {
            k.Aktif = false;
            k.Guncellendi = DateTime.UtcNow;
            denetim?.YazmaKaydet(islem, nameof(GiderKategorisi), k.Id,
                "gider kategorisi pasife alındı", hassas: false);
            await db.SaveChangesAsync();
        }
        var (adet, toplam) = await KategoriEtkisi(islem.TenantId, k.Id);
        return Kategoriye(k, adet, toplam);
    }

    // ── Gider kaydı (nakit çıkışı) ──────────────────────────────────────────

    /// <summary>
    /// PAKET YASAL-4 / Y4-B — SİSTEM kategorisi ("Vergi — stopaj"): kiracıda yoksa
    /// açılır, varsa (aynı ad) o kullanılır. Kategori kullanıcı ekranında normal
    /// kategori gibi görünür ve düzenlenebilir/pasife alınabilir (soyut kayıt).
    /// </summary>
    public async Task<GiderKategorisi> SistemKategorisi(string ad, AktifKullanici islem)
    {
        var k = await db.GiderKategorileri
            .FirstOrDefaultAsync(g => g.TenantId == islem.TenantId && g.Ad == ad);
        if (k is not null) return k;
        var now = DateTime.UtcNow;
        k = new GiderKategorisi
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, Ad = ad,
            Aciklama = "Program tarafından açıldı (stopaj ödemeleri).",
            Aktif = true, Olusturuldu = now, Guncellendi = now,
        };
        db.GiderKategorileri.Add(k);
        denetim?.YazmaKaydet(islem, nameof(GiderKategorisi), k.Id, $"sistem gider kategorisi açıldı: {ad}");
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("gider_kategorileri", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Eşzamanlı ilk ödeme (canlı doğrulama 01.10): iki istek aynı anda kategoriyi açtı,
            // benzersiz (tenant, ad) ikincisini reddetti. Yarım kayıtlar izlemeden çıkarılır ve
            // diğer isteğin açtığı kategori kullanılır — kullanıcıya "kayıt zaten var" gitmez.
            foreach (var e in db.ChangeTracker.Entries()
                         .Where(e => e.State == EntityState.Added
                                  && (ReferenceEquals(e.Entity, k)
                                      || (e.Entity is DenetimKaydi d && d.VarlikId == k.Id)))
                         .ToList())
                e.State = EntityState.Detached;
            return await db.GiderKategorileri
                .FirstOrDefaultAsync(g => g.TenantId == islem.TenantId && g.Ad == ad)
                ?? throw new ConflictException("Gider kategorisi aynı anda açılırken çakıştı; işlemi tekrarlayın.");
        }
        return k;
    }

    /// <param name="ekYazma">
    /// PAKET YASAL-4 / Y4-B — çağıranın AYNI işlemde (aynı SaveChanges) yazacağı ek
    /// kayıt (ör. stopaj_odemeleri satırı). Kasa yazma yolu TEKTİR: ikinci bir kasa
    /// çıkış yolu açılmaz; çağıran yalnız kendi belge satırını ekler.
    /// </param>
    public async Task<GiderKaydiCiktisi> Kaydet(GiderKaydetDto dto, AktifKullanici islem,
        Action<KasaHareket>? ekYazma = null)
    {
        // PAKET PATCH-5: önce yuvarla, sonra ≤ 0 (0,004 → 0,00 gider satırı doğmaz) + kolon tavanı;
        // açıklama varchar(500) servis içinde de (DTO MaxLength yalnız controller yolunda).
        // PAKET GOC-A (K9): 2'den fazla ondalık SESSİZCE YUVARLANMAZ — Core kuralı + sözlük metni (ekranla aynı).
        GirdiKapisi.Tutar(dto.Tutar, "tutar");
        var tutar = KolonTavani.PozitifTutar(dto.Tutar, "tutar", "Gider tutarı");
        KolonTavani.MetinSiniri(dto.Aciklama?.Trim(), 500, "aciklama", "Açıklama");

        var kategori = await Kategori(dto.GiderKategoriId, islem.TenantId);
        if (!kategori.Aktif)
            throw new BadRequestException(
                $"'{kategori.Ad}' kategorisi pasiftir; pasif kategoriye yeni gider yazılamaz. "
                + "Kategoriyi aktife alın ya da başka bir kategori seçin.");

        var kasa = await db.Kasalar
            .FirstOrDefaultAsync(k => k.Id == dto.KasaId && k.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Kasa bulunamadı");
        if (!kasa.Aktif)
            throw new BadRequestException("Pasif kasadan gider ödenemez.");

        // PAKET P-44: kopya kontrol yerine TEK MERKEZ (Kasa.KasaBakiyeKapisi).
        // PARA-DEMO-2 tur 4 (doğrulayıcı bulgusu S10): gider NAKİT ödenir (satır OdemeTuru="NAKIT");
        // toplam kapısına EK olarak nakit payı kapısı — kart/havale payı nakit gider olarak verilemez.
        await Kasa.KasaBakiyeKapisi.NakitDogrula(db, islem.TenantId, kasa, -tutar, OdemeAraci.Nakit);

        var now = DateTime.UtcNow;
        await Kasa.GunKapanisiKapisi.Dogrula(db, islem.TenantId, kasa.Id, now);

        await using var tx = await db.Database.BeginTransactionAsync();

        await BakiyeKilidi.Kilitle(db, kasa);   // PAKET KAPORA-ESZAMAN tur 2: kilitli taze okuma (kayıp güncelleme)
        kasa.Bakiye = Math.Round(kasa.Bakiye - tutar, 2, MidpointRounding.AwayFromZero);
        kasa.Guncellendi = now;

        var satirId = Kimlik.Ulid();
        var satir = new KasaHareket
        {
            Id = satirId,
            TenantId = islem.TenantId,
            KasaId = kasa.Id,
            HareketId = null,
            Tutar = -tutar,                 // ÇIKIŞ: işaret NEGATİF
            ParaBirimi = kasa.ParaBirimi,
            OdemeTuru = "NAKIT",
            KomisyonOran = 0m,
            GiderKategoriId = kategori.Id,
            Aciklama = string.IsNullOrWhiteSpace(dto.Aciklama) ? null : dto.Aciklama.Trim(),
            Tarih = now,
            Olusturuldu = now,
        };
        db.KasaHareketleri.Add(satir);
        ekYazma?.Invoke(satir);

        denetim?.YazmaKaydet(islem, "Gider", satirId,
            $"gider: {kategori.Ad} · {tutar.ToString("0.00", CultureInfo.InvariantCulture)} {kasa.ParaBirimi}"
            + (string.IsNullOrWhiteSpace(dto.Aciklama) ? "" : $" · {dto.Aciklama.Trim()}"),
            hassas: false, kasaId: kasa.Id);

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] GIDER: kasa={KasaId} kategori={KategoriId} tutar={Tutar}",
            islem.TenantId, kasa.Id, kategori.Id, tutar);

        return new GiderKaydiCiktisi(
            satirId, kasa.Id, kasa.Ad, kategori.Id, kategori.Ad,
            tutar, kasa.ParaBirimi,
            string.IsNullOrWhiteSpace(dto.Aciklama) ? null : dto.Aciklama.Trim(),
            now, kasa.Bakiye,
            $"/api/v1/kasalar/{kasa.Id}/hareket/{satirId}/storno");
    }

    /// <summary>
    /// Gider satırları. ⚠ SINIR EKRANDA SÖYLENİR (UX-S29): <paramref name="limit"/>
    /// kadar satır döner ve sınıra değildiyse <c>dahaFazlasiVar</c> true olur.
    /// </summary>
    public async Task<GiderListesi> Listele(
        AktifKullanici islem, DateTime? baslangic, DateTime? bitis, string? kategoriId, int limit = 100)
    {
        if (limit is < 1 or > 500) throw new BadRequestException("limit 1-500 arasında olmalıdır.");

        var q = db.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.GiderKategoriId != null);
        if (baslangic is { } b) q = q.Where(h => h.Tarih >= b);
        if (bitis is { } s) q = q.Where(h => h.Tarih < s);
        if (!string.IsNullOrWhiteSpace(kategoriId)) q = q.Where(h => h.GiderKategoriId == kategoriId);

        var toplamAdet = await q.CountAsync();

        // PAKET Q10-1 (QA turu 10, KRİTİK): "Toplam gider" SUNUCUDA, satır
        // sınırından (limit) BAĞIMSIZ ve AYNI süzgeçle hesaplanır (UX-S21). Gider
        // satırı defterde NEGATİF, storno satırı POZİTİFTİR; SUM(-tutar) işaretli
        // toplar → stornolanan gider toplamdan kendiliğinden düşer (GiderOlcumu ile
        // aynı kural). ⚠ Math.Abs YASAK: mutlak toplam 125,50 + storno = 251,00
        // verir (ölçülen kusur). Para birimleri KARIŞTIRILMAZ (UX-S32).
        var toplamlar = (await q
                .GroupBy(h => h.ParaBirimi)
                .Select(g => new { ParaBirimi = g.Key, Tutar = g.Sum(x => -x.Tutar), Adet = g.Count() })
                .ToListAsync())
            .OrderBy(t => t.ParaBirimi == "TRY" ? 0 : 1).ThenBy(t => t.ParaBirimi)
            .Select(t => new GiderParaToplami(t.ParaBirimi, Math.Round(t.Tutar, 2), t.Adet))
            .ToList();

        var satirlar = await q.OrderByDescending(h => h.Tarih).Take(limit)
            .Select(h => new
            {
                h.Id, h.KasaId, h.GiderKategoriId, h.Tutar, h.ParaBirimi,
                h.Aciklama, h.Tarih, h.StornoKaynakId,
            })
            .ToListAsync();

        var kategoriAdlari = await db.GiderKategorileri.AsNoTracking()
            .Where(g => g.TenantId == islem.TenantId)
            .ToDictionaryAsync(g => g.Id, g => g.Ad);
        var kasaAdlari = await db.Kasalar.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId)
            .ToDictionaryAsync(k => k.Id, k => k.Ad);

        var stornolananlar = await db.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.StornoKaynakId != null)
            .Select(h => h.StornoKaynakId!)
            .ToListAsync();
        var stornoKumesi = stornolananlar.ToHashSet();

        return new GiderListesi(
            [.. satirlar.Select(h => new GiderSatirCiktisi(
                h.Id,
                h.KasaId,
                kasaAdlari.TryGetValue(h.KasaId, out var ka) ? ka : null,
                h.GiderKategoriId,
                h.GiderKategoriId is { } gk && kategoriAdlari.TryGetValue(gk, out var ad) ? ad : null,
                -h.Tutar,                     // ekranda POZİTİF gider tutarı
                h.ParaBirimi,
                h.Aciklama,
                h.Tarih,
                h.StornoKaynakId is not null,
                stornoKumesi.Contains(h.Id)))],
            toplamAdet,
            limit,
            toplamAdet > satirlar.Count,
            $"En çok {limit} gider satırı gösterilir; daraltmak için tarih aralığı ya da kategori seçin.",
            toplamlar);
    }

    // ── Yardımcılar ─────────────────────────────────────────────────────────

    internal async Task<GiderKategorisi> Kategori(string id, string tenantId) =>
        await db.GiderKategorileri.FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId)
        ?? throw new NotFoundException("Gider kategorisi bulunamadı");

    private async Task<(int Adet, decimal Toplam)> KategoriEtkisi(string tenantId, string kategoriId)
    {
        var q = db.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.GiderKategoriId == kategoriId);
        return (await q.CountAsync(), await q.SumAsync(h => (decimal?)-h.Tutar) ?? 0m);
    }

    private static GiderKategoriCiktisi Kategoriye(GiderKategorisi g, int adet, decimal toplam)
        => new(g.Id, g.Ad, g.Aciklama, g.Aktif, g.Olusturuldu, g.Guncellendi,
               adet, Math.Round(toplam, 2));
}

// ── Controller ───────────────────────────────────────────────────────────────

/// <summary>
/// ⚠ MODÜL ANAHTARI "kasa" — YENİ yetki anahtarı AÇILMADI (kapsam kilidi).
/// Gider nakit kasasından çıkar; kasayı göremeyen gideri de göremez. Ayrı bir
/// "gider" anahtarı yetki matrisini, menüyü ve göç tablosunu bu paketin
/// kapsamı dışında genişletirdi.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/gider-kategorileri")]
[Yetki.ModulYetkisi("kasa")]
public sealed class GiderKategoriController(GiderServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<GiderKategoriCiktisi> Olustur([FromBody] GiderKategoriOlusturDto dto) => servis.KategoriOlustur(dto, Islem);

    [HttpGet]
    public Task<GiderKategoriListesi> Listele([FromQuery] bool? aktif) => servis.KategoriListele(Islem, aktif);

    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<GiderKategoriCiktisi> Guncelle(string id, [FromBody] GiderKategoriGuncelleDto dto)
        => servis.KategoriGuncelle(id, dto, Islem);

    /// <summary>DELETE = PASİFE ALMA (hard delete yoktur).</summary>
    [HttpDelete("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<GiderKategoriCiktisi> PasifeAl(string id) => servis.KategoriPasifeAl(id, Islem);
}

[ApiController]
[Authorize]
[Route("api/v1/giderler")]
[Yetki.ModulYetkisi("kasa")]
public sealed class GiderController(GiderServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<GiderKaydiCiktisi> Kaydet([FromBody] GiderKaydetDto dto) => servis.Kaydet(dto, Islem);

    [HttpGet]
    public Task<GiderListesi> Listele(
        [FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis,
        [FromQuery] string? kategoriId, [FromQuery, Range(1, 500)] int limit = 100)
        => servis.Listele(Islem, baslangic, bitis, kategoriId, limit);
}
