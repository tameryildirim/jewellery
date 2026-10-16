using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Kuyumcu.Api.Modules.Masak;

namespace Kuyumcu.Api.Modules.Satis;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-21 — AVANS (Y-17) · DEĞİŞİM (Y-19) · KONSİNYE ÇIKIŞ/İADE (Y-20) ·
//              REZERV / BLOKE ADET (Y-23)
//
// ⚠ NEDEN AYRI DOSYA AMA AYNI SINIF: dördü de stok aritmetiğine, cari
// bakiyeye ve kasaya dokunur. Ayrı bir servis sınıfı açılsaydı
// `StokDegistir` / `CariBakiyeGuncelle` / `KasaGuncelle` / `Yuvarla*`
// KOPYALANIRDI; kopyalanan aritmetik bir gün ayrışır ve iki ekran aynı malın
// iki farklı adedini gösterir. `partial` ile aynı sınıfın parçasıyız:
// yardımcılar paylaşılır, 2500 satırlık SatisModulu.cs'e ikinci el değmez
// (plan § 7 TEK EL zinciri).
//
// ⚠ DEĞİŞMEZLER (plan § 1 md.8): deftere yazan TEK yol HareketFabrikasi.Ekle;
// geri alma yalnız StornoServisi; finansal varlıkta hard DELETE YOK.
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

/// <summary>Y-23 — rezervasyon açma.</summary>
public sealed class RezervOlusturDto
{
    [Required(ErrorMessage = "Ürün zorunludur"), MinLength(1)]
    public string UrunId { get; set; } = null!;

    [Required(ErrorMessage = "Müşteri zorunludur"), MinLength(1)]
    public string CariId { get; set; } = null!;

    /// <summary>PATCH-3 tur 2 (doğrulayıcı DÜŞÜK): rezerv adedinin TEK üst sınırı — oluşturma
    /// (bu Range) ve PATCH (servis içi <c>RezervAdetKapisi</c>) aynı sabiti okur.</summary>
    public const double AdetUst = 100_000;

    [Range(0.001, AdetUst)]
    public decimal Adet { get; set; }

    /// <summary>Sözün bittiği gün (opsiyonel). Geçmiş gün REDDEDİLİR.</summary>
    public DateOnly? BitisGunu { get; set; }

    [MaxLength(36)] public string? SiparisId { get; set; }
    [MaxLength(500)] public string? Aciklama { get; set; }
}

/// <summary>
/// Y-23 — rezervasyon iptali. Yıkıcı eylem: gerekçe ZORUNLU.
/// ⚠ Uzunluk servis içinde DE kontrol edilir (DataAnnotations yalnız
/// controller yolunda çalışır).
/// </summary>
public sealed class RezervIptalDto
{
    [Required, StringLength(500, MinimumLength = Rezervasyon.GerekceAsgari,
        ErrorMessage = "İptal gerekçesi en az 10 karakter olmalıdır")]
    public string Gerekce { get; set; } = null!;
}

/// <summary>Y-17 — avans (kapora) tahsili.</summary>
public sealed class AvansAlDto
{
    [Required(ErrorMessage = "Müşteri zorunludur"), MinLength(1)]
    public string CariId { get; set; } = null!;

    [Range(0.01, KolonTavani.UstTutar, ErrorMessage = "Avans tutarı 0,01 ile 9.999.999.999.999,99 TL arasında olmalıdır")]
    public decimal Tutar { get; set; }

    [Required, RegularExpression("NAKIT|KART|HAVALE",
        ErrorMessage = "Avans NAKIT, KART ya da HAVALE ile alınır (veresiye avans olmaz)")]
    public string OdemeTuru { get; set; } = null!;

    [Required(ErrorMessage = "Kasa zorunludur")] public string KasaId { get; set; } = null!;
    [Required(ErrorMessage = "Kur kaydı zorunludur")] public string KurSnapshotId { get; set; } = null!;

    [MaxLength(36)] public string? SiparisId { get; set; }
    [MaxLength(500)] public string? Aciklama { get; set; }
}

/// <summary>Y-17 — avansın müşteriye geri ödenmesi (satış olmadan).</summary>
public sealed class AvansIadeDto
{
    [Required, MinLength(1)] public string CariId { get; set; } = null!;
    [Range(0.01, KolonTavani.UstTutar)] public decimal Tutar { get; set; }
    [Required] public string KasaId { get; set; } = null!;
    [Required] public string KurSnapshotId { get; set; } = null!;

    [Required, StringLength(500, MinimumLength = 10,
        ErrorMessage = "İade gerekçesi en az 10 karakter olmalıdır")]
    public string Gerekce { get; set; } = null!;
}

/// <summary>Y-17 — açık avansın bir SATIŞA mahsup edilmesi.</summary>
public sealed class AvansMahsupDto
{
    [Required, MinLength(1)] public string SatisHareketId { get; set; } = null!;
    [Range(0.01, KolonTavani.UstTutar)] public decimal Tutar { get; set; }
    [Required] public string KurSnapshotId { get; set; } = null!;
}

/// <summary>Y-19 — ürünü ürünle değişim (tek fiş, tek transaction).</summary>
public sealed class DegisimDto
{
    /// <summary>Geri alınan malın ÇIKTIĞI satış hareketi.</summary>
    [Required, MinLength(1)] public string KaynakHareketId { get; set; } = null!;

    [Range(0.001, 100000)] public decimal GeriAlinanAdet { get; set; }

    [Required, MinLength(1)] public string YeniUrunId { get; set; } = null!;
    [Range(0.001, 100000)] public decimal YeniAdet { get; set; }
    [Range(0, KolonTavani.UstTutar)] public decimal YeniBirimFiyatTl { get; set; }
    [Range(0, KolonTavani.UstTutar)] public decimal? YeniIscilikTl { get; set; }

    /// <summary>
    /// FARKIN ödeme türü. KART bu sürümde KAPALIDIR (bkz. servis gerekçesi);
    /// fark 0 ise bu alan okunmaz.
    /// </summary>
    [RegularExpression("NAKIT|VERESIYE",
        ErrorMessage = "Değişim farkı bu sürümde NAKIT ya da VERESIYE ile kapanır")]
    public string? FarkOdemeTuru { get; set; }

    public string? KasaId { get; set; }
    [Range(0, 3650)] public int? VadeGun { get; set; }
    [Required] public string KurSnapshotId { get; set; } = null!;
    [MaxLength(500)] public string? Aciklama { get; set; }
    [MaxLength(36)] public string? ReyonId { get; set; }
}

/// <summary>Y-18 — sipariş → satış dönüşümü. Kalem fiyatları dönüşüm anında verilir.</summary>
public sealed class SiparisSatisaCevirKalemDto
{
    [Required, MinLength(1)] public string SiparisKalemId { get; set; } = null!;
    [Range(0, KolonTavani.UstTutar)] public decimal BirimFiyatTl { get; set; }
    [Range(0, KolonTavani.UstTutar)] public decimal? IscilikTl { get; set; }
}

/// <summary>Y-18 — sipariş → satış dönüşümü isteği.</summary>
public sealed class SiparisSatisaCevirDto
{
    [Required, MinLength(1)] public List<SiparisSatisaCevirKalemDto> Kalemler { get; set; } = [];

    /// <summary>Ödeme bacakları — çok bacaklı fiş sözleşmesiyle AYNI (SatisFisi).</summary>
    [Required, MinLength(1)] public List<OdemeBacagiDto> Odemeler { get; set; } = [];

    [Required] public string KurSnapshotId { get; set; } = null!;
    [Range(0, 3650)] public int? VadeGun { get; set; }

    /// <summary>
    /// Bu siparişe daha önce alınmış avansın satışa mahsup edilecek tutarı.
    /// Boş/0 ise mahsup YAPILMAZ (sessiz mahsup yok — UX-S27).
    /// </summary>
    [Range(0, KolonTavani.UstTutar)] public decimal? AvansMahsupTl { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }
    public MasakBilgiDto? Masak { get; set; }
}

/// <summary>Y-20 — BİZDEN giden konsinye mal.</summary>
public sealed class KonsinyeCikisDto
{
    [Required, MinLength(1)] public string CariId { get; set; } = null!;
    [Required, MinLength(1)] public string UrunId { get; set; } = null!;
    [Range(0.001, 100000)] public decimal Adet { get; set; }
    [Required] public string KurSnapshotId { get; set; } = null!;
    [MaxLength(500)] public string? Aciklama { get; set; }
    [MaxLength(36)] public string? ReyonId { get; set; }
}

/// <summary>Y-20 — konsinye malın geri gelmesi.</summary>
public sealed class KonsinyeIadeDto
{
    [Required, MinLength(1)] public string CikisHareketId { get; set; } = null!;
    [Range(0.001, 100000)] public decimal Adet { get; set; }
    [Required] public string KurSnapshotId { get; set; } = null!;
    [MaxLength(500)] public string? Aciklama { get; set; }
    [MaxLength(36)] public string? ReyonId { get; set; }
}

// ── Servis (SatisServisi'nin P-21 parçası) ───────────────────────────────────

public sealed partial class SatisServisi
{
    // ═══ Y-23 · REZERVASYON / BLOKE ADET ═════════════════════════════════════

    /// <summary>
    /// İZLENEN (tracked) şube stok satırı — P-21'in TEK okuma noktası.
    ///
    /// <para>⚠ NEDEN TEK YARDIMCI: bu dosyada beş ayrı yer aynı satırı arıyor.
    /// Her biri kendi şube süzgecini ayrı yazsaydı P-04 sınıflandırma
    /// tablosu beş satır büyür ve birinde şube koşulu unutulduğunda BAŞKA
    /// ŞUBENİN stoğu düşerdi. Tek yerde durur; denetim.sh tek satır sayar.</para>
    /// <para>⚠ <c>StokBul</c> KULLANILAMAZ: o <c>AsNoTracking</c>'tir ve bloke
    /// yazımı için izlenen varlık gerekir.</para>
    /// </summary>
    private async Task<Data.Stok> StokSatiriIzlenen(string urunId, string subeId, string tenantId) =>
        await db.Stoklar.FirstOrDefaultAsync(
            s => s.UrunId == urunId && s.SubeId == subeId && s.TenantId == tenantId)
        ?? throw new BadRequestException("Bu şubede ürün stoğu bulunamadı");

    /// <summary>
    /// Rezervasyon açar ve <c>stoklar.bloke_adet</c>'i artırır.
    ///
    /// <para>⚠ SATILABİLİR STOKTAN düşer, ADETTEN DÜŞMEZ (Stok.BlokeAdet
    /// gerekçesi): mal hâlâ raftadır, sayım onu görmelidir.</para>
    /// </summary>
    /// <summary>
    /// PATCH-3 tur 2 — rezerv adedinin TEK kapısı (oluşturma + PATCH): yuvarlanmış adet
    /// 0 &lt; adet ≤ <see cref="RezervOlusturDto.AdetUst"/>; değilse 400 alan:adet.
    /// Eskiden oluşturma 100.000, PATCH ise kolon tavanı (999.999.999,999) ile sınırlıydı.
    /// </summary>
    public static decimal RezervAdetKapisi(decimal yuvarlanmisAdet)
    {
        if (yuvarlanmisAdet <= 0m)
            throw new BadRequestException("Rezerve adet sıfırdan büyük olmalıdır").AlanIle("adet");
        if (yuvarlanmisAdet > (decimal)RezervOlusturDto.AdetUst)
            throw new BadRequestException(
                $"Rezerve adet en çok {((decimal)RezervOlusturDto.AdetUst).ToString("#,##0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"))} olabilir").AlanIle("adet");
        return yuvarlanmisAdet;
    }

    public async Task<object> RezervOlustur(RezervOlusturDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem, "Rezervasyon için şube seçimi gerekli");

        // PAKET PATCH-3 (PATCH-2 kalıbı): ÖNCE kolon hanesine (rezervasyonlar.adet decimal(12,3))
        // yuvarla, SONRA ≤ 0 — 0,0004 eskiden kontrolü geçip blokeyi 0,000 oynatıyordu.
        // DTO Range(0.001) yalnız controller yolunda koşar → servis içi elle kontrol.
        dto.Adet = RezervAdetKapisi(Yuvarla3(dto.Adet));

        var bugun = Zaman.YerelBugun();
        if (dto.BitisGunu is { } bit && bit < bugun)
            throw new BadRequestException($"Rezervasyon bitiş günü geçmiş olamaz (bugün {bugun:yyyy-MM-dd})")
                .AlanIle("bitisGunu");   // PAKET PATCH-4: alan altı mesaj (PATCH yolu ile AYNI)

        await IslemDogrulayici.CariDogrula(db, dto.CariId, islem.TenantId);
        await IslemDogrulayici.CariAktifDogrula(db, dto.CariId, islem.TenantId);
        var urun = await UrunBul(dto.UrunId, islem.TenantId);
        Urun.UrunAdetKapisi.Dogrula(urun.Birim, dto.Adet, "adet");   // PAKET GOC-C (#6 · K14): ADET birimli üründe miktar tam sayı.

        if (!string.IsNullOrWhiteSpace(dto.SiparisId))
        {
            var siparis = await db.Siparisler.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == dto.SiparisId && s.TenantId == islem.TenantId)
                ?? throw new NotFoundException("Sipariş bulunamadı");
            // Kapanmış siparişe yeni rezervasyon bağlanamaz (TERMINAL_SONRASI_YAZAN_YOL).
            TerminalKapisi.Dogrula(siparis.Durum, SiparisDurumu.Terminal, "Sipariş", "rezervasyon bağlama");
        }

        var now = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();

        var stok = await StokSatiriIzlenen(dto.UrunId, subeId, islem.TenantId);

        // ⚠ Kapı, adedi DEĞİL blokeyi değiştirir ve 0 ≤ bloke ≤ adet değişmezini zorlar.
        BlokeKapisi.Degistir(stok, dto.Adet, now);

        var rezerv = new Rezervasyon
        {
            Id                   = Kimlik.Ulid(),
            TenantId             = islem.TenantId,
            SubeId               = subeId,
            UrunId               = dto.UrunId,
            CariId               = dto.CariId,
            SiparisId            = string.IsNullOrWhiteSpace(dto.SiparisId) ? null : dto.SiparisId,
            Adet                 = dto.Adet,
            Durum                = RezervasyonDurumu.Aktif,
            BitisGunu            = dto.BitisGunu,
            Aciklama             = dto.Aciklama,
            OlusturanKullaniciId = islem.Id,
            Olusturuldu          = now,
            Guncellendi          = now,
        };
        db.Rezervasyonlar.Add(rezerv);

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] REZERV: ürün={UrunId} adet={Adet} cari={CariId}",
            islem.TenantId, dto.UrunId, dto.Adet, dto.CariId);

        return new
        {
            id = rezerv.Id, urunId = rezerv.UrunId, urunAd = urun.Ad, cariId = rezerv.CariId,
            adet = rezerv.Adet, durum = rezerv.Durum, bitisGunu = rezerv.BitisGunu,
            stokAdet = stok.Adet, blokeAdet = stok.BlokeAdet,
            satilabilirAdet = BlokeKapisi.Satilabilir(stok),
        };
    }

    /// <summary>
    /// Rezervasyonu iptal eder ve blokeyi geri bırakır.
    /// ⚠ HARD DELETE YOKTUR: kayıt IPTAL durumuna geçer, gerekçesi kalır.
    /// </summary>
    public async Task<object> RezervIptal(string id, RezervIptalDto dto, AktifKullanici islem)
    {
        // ⚠ SERVİS İÇİ KONTROL: DataAnnotations yalnız controller yolunda çalışır.
        var gerekce = (dto.Gerekce ?? string.Empty).Trim();
        if (gerekce.Length < Rezervasyon.GerekceAsgari)
            throw new BadRequestException(
                $"İptal gerekçesi en az {Rezervasyon.GerekceAsgari} karakter olmalıdır " +
                $"({gerekce.Length}/{Rezervasyon.GerekceAsgari}).");

        var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync();

        var rezerv = await db.Rezervasyonlar.FirstOrDefaultAsync(
                r => r.Id == id && r.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Rezervasyon bulunamadı");

        TerminalKapisi.Dogrula(rezerv.Durum, RezervasyonDurumu.Terminal, "Rezervasyon", "iptal");

        var stok = await StokSatiriIzlenen(rezerv.UrunId, rezerv.SubeId, islem.TenantId);

        BlokeKapisi.Degistir(stok, -rezerv.Adet, now);

        rezerv.Durum            = RezervasyonDurumu.Iptal;
        rezerv.KapanisGerekcesi = gerekce;
        rezerv.Guncellendi      = now;

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return new
        {
            id = rezerv.Id, durum = rezerv.Durum,
            stokAdet = stok.Adet, blokeAdet = stok.BlokeAdet,
            satilabilirAdet = BlokeKapisi.Satilabilir(stok),
        };
    }

    /// <summary>
    /// PATCH — rezervasyonu düzeltir (SÖZLEŞME-DoD: her yeni kayıt türü PATCH ile doğar).
    ///
    /// <para>⚠ SÖZLEŞME: <b>gövdede olmayan alan KORUNUR, açıkça <c>null</c>
    /// yazılan alan SİLİNİR.</b> Tam gövdeyle üzerine yazılsaydı, ekran yalnız
    /// bitiş gününü gönderdiğinde açıklama SESSİZCE silinirdi (S-3a'da bire bir
    /// yaşanan hata).</para>
    /// <para>⚠ Adet değişirse bloke de AYNI transaction içinde düzeltilir;
    /// yoksa rezervasyon ile bloke ayrışır (BLOKE_TUTARSIZ).</para>
    /// <para>⚠ Yalnız AKTİF rezervasyon düzenlenir; kapanmış kayıt
    /// <see cref="TerminalKapisi"/>'ndan 409 alır.</para>
    /// </summary>
    public async Task<object> RezervGuncelle(string id, JsonElement govde, AktifKullanici islem)
    {
        if (govde.ValueKind != JsonValueKind.Object)
            throw new BadRequestException("Geçersiz istek gövdesi");

        var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync();

        var rezerv = await db.Rezervasyonlar.FirstOrDefaultAsync(
                r => r.Id == id && r.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Rezervasyon bulunamadı");

        TerminalKapisi.Dogrula(rezerv.Durum, RezervasyonDurumu.Terminal, "Rezervasyon", "düzenleme");

        // PAKET PATCH-3: tür denetimi — sayı/nesne gelirse GetString() InvalidOperationException → 500 idi.
        if (govde.TryGetProperty("aciklama", out var acOge))
            rezerv.Aciklama = (KolonTavani.JsonMetin(acOge, "aciklama", "Açıklama") ?? "").Trim()
                is { Length: > 0 } t ? t[..Math.Min(t.Length, 500)] : null;

        if (govde.TryGetProperty("bitisGunu", out var bgOge))
        {
            if (bgOge.ValueKind == JsonValueKind.Null) rezerv.BitisGunu = null;
            else if (DateOnly.TryParse(KolonTavani.JsonMetin(bgOge, "bitisGunu", "Bitiş günü"),
                         System.Globalization.CultureInfo.InvariantCulture, out var g))
            {
                var bugun = Zaman.YerelBugun();
                if (g < bugun)
                    throw new BadRequestException(
                        $"Rezervasyon bitiş günü geçmiş olamaz (bugün {bugun:yyyy-MM-dd})")
                        .AlanIle("bitisGunu");   // PAKET PATCH-4: aynı bloktaki biçim hatası gibi ALANLI
                rezerv.BitisGunu = g;
            }
            else throw new BadRequestException("Bitiş günü geçersiz (YYYY-AA-GG)").AlanIle("bitisGunu");
        }

        var stok = await StokSatiriIzlenen(rezerv.UrunId, rezerv.SubeId, islem.TenantId);

        // PAKET PATCH-3 — TUTARLILIK KURALI (zorunlu alan): gövdede YOK ya da null → KORU
        // (Şube adı, ürün adı/kodu, kategori adı ile AYNI; zorunlu alan PATCH ile silinemez,
        // null "değişiklik yok" demektir). Rezervasyonu bitirmenin yolu iptal ucudur.
        // Sayı dize ("1.500") ya da decimal'e sığmayan değer gelirse eskiden GetDecimal()
        // InvalidOperationException/FormatException → 500 idi; artık 400 alan:adet.
        if (govde.TryGetProperty("adet", out var adOge) && adOge.ValueKind != JsonValueKind.Null)
        {
            var yeniAdet = RezervAdetKapisi(Yuvarla3(KolonTavani.JsonSayi(adOge, "adet", "Rezerve adet")));
            // PAKET GOC-C (#6 · K14): düzenlemede de ADET birimli üründe tam sayı (oluşturma ile AYNI kural).
            var rezervBirim = await db.Urunler.AsNoTracking()
                .Where(u => u.Id == rezerv.UrunId && u.TenantId == islem.TenantId).Select(u => u.Birim).FirstOrDefaultAsync();
            Urun.UrunAdetKapisi.Dogrula(rezervBirim, yeniAdet, "adet");
            // Bloke FARK kadar oynar — kapı 0 ≤ bloke ≤ adet değişmezini zorlar.
            BlokeKapisi.Degistir(stok, yeniAdet - rezerv.Adet, now);
            rezerv.Adet = yeniAdet;
        }

        rezerv.Guncellendi = now;
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return new
        {
            id = rezerv.Id, rezerv.UrunId, rezerv.CariId, rezerv.Adet,
            rezerv.Durum, rezerv.BitisGunu, rezerv.Aciklama,
            stokAdet = stok.Adet, blokeAdet = stok.BlokeAdet,
            satilabilirAdet = BlokeKapisi.Satilabilir(stok),
        };
    }

    /// <summary>
    /// Rezervasyon listesi — sınır ekranda yazılır (UX-S29).
    /// </summary>
    public async Task<object> RezervListele(
        AktifKullanici islem, string? durum, string? urunId, string? cariId, int? limit)
    {
        var alinan = Math.Clamp(limit ?? Rezervasyon.VarsayilanLimit, 1, Rezervasyon.AzamiLimit);

        var q = db.Rezervasyonlar.AsNoTracking().Where(r => r.TenantId == islem.TenantId);
        if (!string.IsNullOrWhiteSpace(durum))
        {
            if (!RezervasyonDurumu.Tumu.Contains(durum))
                throw new BadRequestException(
                    $"Geçersiz durum: '{durum}'. Geçerli: {string.Join(", ", RezervasyonDurumu.Tumu)}");
            q = q.Where(r => r.Durum == durum);
        }
        if (!string.IsNullOrWhiteSpace(urunId)) q = q.Where(r => r.UrunId == urunId);
        if (!string.IsNullOrWhiteSpace(cariId)) q = q.Where(r => r.CariId == cariId);

        var toplam = await q.CountAsync();
        var satirlar = await q.OrderByDescending(r => r.Olusturuldu).Take(alinan)
            .Select(r => new
            {
                r.Id, r.UrunId, r.CariId, r.SiparisId, r.Adet, r.Durum,
                r.BitisGunu, r.Aciklama, r.KapanisGerekcesi, r.SatisHareketId, r.Olusturuldu,
            })
            .ToListAsync();

        var bugun = Zaman.YerelBugun();
        return new
        {
            satirlar = satirlar.Select(r => new
            {
                r.Id, r.UrunId, r.CariId, r.SiparisId, r.Adet, r.Durum, r.BitisGunu,
                r.Aciklama, r.KapanisGerekcesi, r.SatisHareketId, r.Olusturuldu,
                suresiDoldu = r.Durum == RezervasyonDurumu.Aktif
                              && r.BitisGunu is not null && r.BitisGunu < bugun,
                duzenlenebilir = r.Durum == RezervasyonDurumu.Aktif,
            }),
            toplam,
            limit = alinan,
            sinirAsildi = toplam > alinan,
        };
    }

    // ═══ Y-17 · AVANS (KAPORA) ═══════════════════════════════════════════════

    /// <summary>
    /// Açık avans havuzu = Σ AVANS_ALINDI − Σ AVANS_MAHSUP − Σ AVANS_IADE (TL, mutlak).
    ///
    /// <para>⚠ AYRI TABLO YOK (Ticaret.cs başlığı): havuz DEFTERDEN türetilir.
    /// İkinci bir kaynak olsaydı ikisi ayrışır ve hangisinin doğru olduğu bir
    /// daha bilinemezdi.</para>
    /// </summary>
    public async Task<decimal> AcikAvans(string cariId, string tenantId)
    {
        var satirlar = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.CariId == cariId
                        && (h.Tur == AvansKapisi.Alindi || h.Tur == AvansKapisi.Mahsup
                            || h.Tur == AvansKapisi.Iade))
            .Select(h => new { h.Tur, h.TlTutari })
            .ToListAsync();

        // AVANS_ALINDI TL bacağı +, MAHSUP ve IADE −; toplam doğrudan havuzdur.
        return Yuvarla2(satirlar.Sum(s => s.TlTutari));
    }

    /// <summary>
    /// AVANS TAHSİLİ — para kasaya girer, müşteri ALACAKLI olur.
    ///
    /// <para>Defter satırı: Tur=AVANS_ALINDI, TlTutari=+tutar,
    /// OdemeIzi = etiketten (NAKIT ise nakit payı — 459 s. VUK tevsik
    /// kümülatifi bu satırı GÖRMELİDİR: avans gerçek bir nakit tahsilattır).</para>
    /// <para>Cari bakiye −tutar: biz müşteriye borçlanırız.</para>
    /// </summary>
    public async Task<object> AvansAl(AvansAlDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem, "Avans için şube seçimi gerekli");
        if (dto.Tutar <= 0m) throw new BadRequestException("Avans tutarı sıfırdan büyük olmalıdır");

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);
        await IslemDogrulayici.CariDogrula(db, dto.CariId, islem.TenantId);
        await IslemDogrulayici.CariAktifDogrula(db, dto.CariId, islem.TenantId);

        var now  = DateTime.UtcNow;
        var kasa = await IslemDogrulayici.KasaDogrula(db, dto.KasaId, islem.TenantId, "TRY", now);

        if (!string.IsNullOrWhiteSpace(dto.SiparisId))
        {
            var siparis = await db.Siparisler.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == dto.SiparisId && s.TenantId == islem.TenantId)
                ?? throw new NotFoundException("Sipariş bulunamadı");
            TerminalKapisi.Dogrula(siparis.Durum, SiparisDurumu.Terminal, "Sipariş", "avans bağlama");
        }

        // 459 s. VUK — avans gerçek bir nakit tahsilattır; hadde tabidir.
        if (dto.OdemeTuru == "NAKIT")
            await tevsik.Dogrula(db, islem.TenantId, dto.CariId, dto.Tutar, now,
                islemToplamTutari: dto.Tutar);

        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, dto.CariId, now);   // PAKET MASAK-ESZAMAN: işlem + CARİ SATIRI KİLİDİ; tevsik kilit altında yinelenir
        if (dto.OdemeTuru == "NAKIT")
            await tevsik.Dogrula(db, islem.TenantId, dto.CariId, dto.Tutar, now,
                islemToplamTutari: dto.Tutar);

        var hareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            OdemeIzi.Etiketten(dto.OdemeTuru, dto.Tutar, vade: null),
            new Hareket
            {
                Id            = hareketId,
                TenantId      = islem.TenantId,
                SubeId        = subeId,
                Tur           = AvansKapisi.Alindi,
                Tarih         = now,
                Aciklama      = dto.Aciklama,
                TlTutari      = Yuvarla2(dto.Tutar),
                HasGrami      = 0m,
                OdemeTuru     = dto.OdemeTuru,
                KurSnapshotId = dto.KurSnapshotId,
                CariId        = dto.CariId,
                SiparisId     = string.IsNullOrWhiteSpace(dto.SiparisId) ? null : dto.SiparisId,
                KullaniciId   = islem.Id,
                Olusturuldu   = now,
            });

        await KasaGuncelle(kasa, islem.TenantId, dto.Tutar, now, hareketId, dto.OdemeTuru);

        // Müşteri ALACAKLI olur: bakiye negatif yöne gider.
        await CariBakiyeGuncelle(dto.CariId, islem.TenantId, -Yuvarla2(dto.Tutar), 0m, now);

        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        var havuz = await AcikAvans(dto.CariId, islem.TenantId);
        logger.LogInformation("[{TenantId}] AVANS_ALINDI: cari={CariId} tutar={Tutar}",
            islem.TenantId, dto.CariId, dto.Tutar);

        return new { hareketId, cariId = dto.CariId, tutar = Yuvarla2(dto.Tutar), acikAvans = havuz };
    }

    /// <summary>
    /// AVANS MAHSUBU — açık avans bir satışa sayılır.
    ///
    /// <para>⚠ ÇİFTE SAYMA KAPISI (plan DoD): bu satır CARİ BAKİYEYE DOKUNMAZ.
    /// Bakiye zaten AVANS_ALINDI anında azalmıştı; burada ikinci kez
    /// azaltılsaydı müşteri hiç ödemediği bir borcu ödemiş görünürdü. Satır
    /// yalnız avans HAVUZUNU eritir ve hangi satışa sayıldığını
    /// <see cref="Hareket.FisId"/> ile söyler.</para>
    /// <para>⚠ OdemeIzi.Yok: para EL DEĞİŞTİRMEZ (tevsik/MASAK kümülatifine
    /// girmez — nakit zaten AVANS_ALINDI'da sayıldı, ikinci kez sayılsaydı
    /// had yanlış yerden delinirdi).</para>
    /// </summary>
    public async Task<object> AvansMahsup(AvansMahsupDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem, "Avans mahsubu için şube seçimi gerekli");
        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);

        var satis = await db.Hareketler.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == dto.SatisHareketId && h.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Satış hareketi bulunamadı");

        if (satis.Tur != "SATIS")
            throw new BadRequestException($"Avans yalnız SATIS hareketine mahsup edilir (bulunan: {satis.Tur}).");
        if (string.IsNullOrEmpty(satis.CariId))
            throw new BadRequestException("Müşterisi olmayan satışa avans mahsup edilemez.");
        if (satis.StornoKaynakId is not null)
            throw new ConflictException("Storno satırına avans mahsup edilemez.");

        var havuz = await AcikAvans(satis.CariId, islem.TenantId);
        var tutar = AvansKapisi.MahsupEdilebilir(havuz, dto.Tutar);

        // Mahsup, satışın kendi tutarını da aşamaz: aşsaydı satılmamış bir mal
        // için avans eritilir ve havuz sessizce boşalırdı.
        var dahaOnce = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tur == AvansKapisi.Mahsup
                        && h.FisId == satis.Id)
            .SumAsync(h => (decimal?)h.TlTutari) ?? 0m;
        var kalanSatis = Yuvarla2(Math.Abs(satis.TlTutari) - Math.Abs(dahaOnce));
        if (tutar > kalanSatis + 0.005m)
            throw new BadRequestException(
                $"Mahsup tutarı satışın kalan tutarını aşıyor: satış {Math.Abs(satis.TlTutari):0.00} TL, " +
                $"daha önce mahsup {Math.Abs(dahaOnce):0.00} TL, kalan {kalanSatis:0.00} TL.");

        var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync();

        var hareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            OdemeIzi.Yok,   // ⚠ para el değiştirmez — çifte tevsik/MASAK sayımı olmaz
            new Hareket
            {
                Id            = hareketId,
                TenantId      = islem.TenantId,
                SubeId        = subeId,
                Tur           = AvansKapisi.Mahsup,
                Tarih         = now,
                Aciklama      = $"Avans mahsubu · satış {satis.Id}",
                TlTutari      = Yuvarla2(-tutar),
                HasGrami      = 0m,
                KurSnapshotId = dto.KurSnapshotId,
                CariId        = satis.CariId,
                FisId         = satis.Id,     // hangi satışa sayıldı
                SiparisId     = satis.SiparisId,
                KullaniciId   = islem.Id,
                Olusturuldu   = now,
            });

        // ⚠ CariBakiyeGuncelle ÇAĞRILMAZ — gerekçe yukarıda (M21-02 bunu ölçer).

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        var kalanHavuz = await AcikAvans(satis.CariId, islem.TenantId);
        return new
        {
            hareketId, satisHareketId = satis.Id, cariId = satis.CariId,
            mahsupTutar = tutar, acikAvans = kalanHavuz,
        };
    }

    /// <summary>
    /// AVANS İADESİ — satış olmadan avansın geri ödenmesi. Para kasadan ÇIKAR,
    /// cari bakiye GERİ ARTAR (borcumuz kapanır).
    /// </summary>
    public async Task<object> AvansIade(AvansIadeDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem, "Avans iadesi için şube seçimi gerekli");

        var gerekce = (dto.Gerekce ?? string.Empty).Trim();
        if (gerekce.Length < 10)
            throw new BadRequestException($"İade gerekçesi en az 10 karakter olmalıdır ({gerekce.Length}/10).");

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);
        await IslemDogrulayici.CariDogrula(db, dto.CariId, islem.TenantId);

        var havuz = await AcikAvans(dto.CariId, islem.TenantId);
        var tutar = AvansKapisi.MahsupEdilebilir(havuz, dto.Tutar);

        var now  = DateTime.UtcNow;
        var kasa = await IslemDogrulayici.KasaDogrula(db, dto.KasaId, islem.TenantId, "TRY", now);

        // 459 s. VUK — tediye de hadde tabidir (SatisIade ile aynı gerekçe).
        await tevsik.Dogrula(db, islem.TenantId, dto.CariId, tutar, now, islemToplamTutari: tutar);

        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, dto.CariId, now);   // PAKET MASAK-ESZAMAN: işlem + CARİ SATIRI KİLİDİ; tevsik kilit altında yinelenir
        await tevsik.Dogrula(db, islem.TenantId, dto.CariId, tutar, now, islemToplamTutari: tutar);

        var hareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            OdemeIzi.Nakitten(tutar),
            new Hareket
            {
                Id            = hareketId,
                TenantId      = islem.TenantId,
                SubeId        = subeId,
                Tur           = AvansKapisi.Iade,
                Tarih         = now,
                Aciklama      = gerekce,
                TlTutari      = Yuvarla2(-tutar),
                HasGrami      = 0m,
                OdemeTuru     = "NAKIT",
                KurSnapshotId = dto.KurSnapshotId,
                CariId        = dto.CariId,
                KullaniciId   = islem.Id,
                Olusturuldu   = now,
            });

        await KasaGuncelle(kasa, islem.TenantId, -tutar, now, hareketId, "NAKIT");
        // Borcumuz kapanır: bakiye pozitif yöne döner.
        await CariBakiyeGuncelle(dto.CariId, islem.TenantId, Yuvarla2(tutar), 0m, now);

        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        return new
        {
            hareketId, cariId = dto.CariId, tutar,
            acikAvans = await AcikAvans(dto.CariId, islem.TenantId),
        };
    }

    // ═══ Y-19 · DEĞİŞİM (ÜRÜNÜ ÜRÜNLE) ═══════════════════════════════════════

    /// <summary>
    /// DEĞİŞİM — müşteri aldığı malı geri getirir, yerine başka mal alır.
    /// İKİ BACAK, TEK TRANSACTION, TEK FİŞ (<see cref="Hareket.FisId"/>).
    ///
    /// <para>⚠ STORNO İLE KARIŞTIRILMAZ (plan risk notu: "çift iade riski").
    /// Değişim bir İADE + bir SATIŞ'tır; satışı HÜKÜMSÜZ KILMAZ. Geri alınan
    /// bacak <c>SATIS_IADE</c> türündedir ve <c>iade_kaynak_hareket_id</c> ile
    /// orijinal satışa bağlanır — böylece "bu maldan ne kadarı zaten geri
    /// geldi" sayacı (<c>IadeKaynagi</c>) değişimi de görür ve aynı mal iki
    /// kez geri alınamaz. Storno motoru (<c>StornoServisi</c>) bu satırlara
    /// HİÇ dokunmaz; ters iade TAKLİDİ yazılmaz (mühür 2).</para>
    ///
    /// <para>⚠ KART FARKI KAPALIDIR: POS yan defteri (pos_tahsilatlari) iki
    /// bacaklı bir fişte hangi bacağa yazılacağı kararını gerektirir ve bu
    /// karar P-21 kapsamında değildir. Kapı 400 döner ve SEBEBİNİ söyler
    /// (UX-S30); sessizce nakde çevirmez.</para>
    /// </summary>
    public async Task<object> Degisim(DegisimDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem, "Değişim için şube seçimi gerekli");
        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);

        var (orijinal, iadeEdilebilir) = await IadeKaynagi(dto.KaynakHareketId, "SATIS", islem.TenantId);

        if (dto.GeriAlinanAdet > iadeEdilebilir)
            throw new BadRequestException(
                $"Geri alınan adet kalan iade hakkını aşıyor. Kalan: {iadeEdilebilir:N3} adet.");

        var eskiUrun = await UrunBul(orijinal.UrunId!, islem.TenantId);
        var yeniUrun = await UrunBul(dto.YeniUrunId, islem.TenantId);
        Urun.UrunAdetKapisi.Dogrula(eskiUrun.Birim, dto.GeriAlinanAdet, "geriAlinanAdet");   // PAKET GOC-C (#6 · K14): ADET birimli üründe miktar tam sayı.
        Urun.UrunAdetKapisi.Dogrula(yeniUrun.Birim, dto.YeniAdet, "yeniAdet");
        Urun.TasKapisi.SatisaUygunMu(yeniUrun);
        Urun.KonsinyeKapisi.SatisaUygunMu(yeniUrun);

        var now = DateTime.UtcNow;

        // ── Tutarlar ────────────────────────────────────────────────────────
        var geriTutar  = Yuvarla2(Math.Abs(orijinal.TlTutari) * dto.GeriAlinanAdet / OrijinalAdet(orijinal));
        var yeniToplam = Yuvarla2(dto.YeniAdet * dto.YeniBirimFiyatTl + (dto.YeniIscilikTl ?? 0m));
        var fark       = Yuvarla2(yeniToplam - geriTutar);   // + müşteri öder, − biz iade ederiz

        var cariId = orijinal.CariId;

        Data.Kasa? kasa = null;
        var farkTuru = dto.FarkOdemeTuru;
        if (fark != 0m)
        {
            if (string.IsNullOrWhiteSpace(farkTuru))
                throw new BadRequestException(
                    $"Değişim farkı {fark:0.00} TL — farkın ödeme türü (NAKIT/VERESIYE) zorunludur.");
            if (farkTuru is not ("NAKIT" or "VERESIYE"))
                throw new BadRequestException(
                    "Değişim farkı bu sürümde yalnız NAKIT ya da VERESIYE ile kapanır; " +
                    "KART farkı POS yan defteri kararı beklediği için kapalıdır.");
            if (farkTuru == "NAKIT")
            {
                if (string.IsNullOrWhiteSpace(dto.KasaId))
                    throw new BadRequestException("Nakit fark için kasa seçimi zorunludur");
                kasa = await IslemDogrulayici.KasaDogrula(db, dto.KasaId!, islem.TenantId, "TRY", now);
                await tevsik.Dogrula(db, islem.TenantId, cariId, Math.Abs(fark), now,
                    islemToplamTutari: Math.Abs(fark));
            }
            else if (string.IsNullOrEmpty(cariId))
            {
                throw new BadRequestException("Veresiye fark için orijinal satışın müşterisi olmalıdır");
            }
        }

        // Veresiye fark borç DOĞURUYORSA vade + kredi limiti kapısından geçer.
        var vade = await Vade.VadeKapisi.Hazirla(
            db, islem.TenantId, cariId, dto.VadeGun,
            farkTuru == "VERESIYE" && fark > 0m ? fark : 0m,
            alacakYonu: true, now, dto.KurSnapshotId);

        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, cariId, now);   // PAKET MASAK-ESZAMAN: işlem + CARİ SATIRI KİLİDİ; tevsik kilit altında yinelenir
        if (farkTuru == "NAKIT" && fark != 0m)
            await tevsik.Dogrula(db, islem.TenantId, cariId, Math.Abs(fark), now,
                islemToplamTutari: Math.Abs(fark));

        var fisId = Kimlik.Ulid();   // ⚠ İKİ BACAK TEK FİŞ

        // ── 1. bacak: geri alınan mal (SATIS_IADE) ──────────────────────────
        var iadeHareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            OdemeIzi.Yok,   // para bacağı FARKTA tek sefer yazılır; burada yazılsaydı çift sayılırdı
            new Hareket
            {
                Id                  = iadeHareketId,
                TenantId            = islem.TenantId,
                SubeId              = subeId,
                Tur                 = "SATIS_IADE",
                Tarih               = now,
                Adet                = dto.GeriAlinanAdet,
                Aciklama            = dto.Aciklama ?? "Değişim — geri alınan mal",
                TlTutari            = Yuvarla2(-geriTutar),
                HasGrami            = Yuvarla6(dto.GeriAlinanAdet * eskiUrun.HasGrami),
                Maden               = Kuyumcu.Core.Domain.Ayarlar.MadenBul(eskiUrun.AyarId)
                                      ?? Kuyumcu.Core.Domain.Maden.Altin,
                KurSnapshotId       = dto.KurSnapshotId,
                CariId              = cariId,
                UrunId              = orijinal.UrunId,
                FisId               = fisId,
                IadeKaynakHareketId = orijinal.Id,
                KullaniciId         = islem.Id,
                Olusturuldu         = now,
            });

        await StokDegistir(orijinal.UrunId!, subeId, islem.TenantId,
            dto.GeriAlinanAdet, eskiUrun.HasGrami, now);
        await maliyet.PartiIade(islem.TenantId, orijinal.Id, dto.GeriAlinanAdet, now, iadeHareketId);

        // ── 2. bacak: verilen yeni mal (SATIS) ──────────────────────────────
        var stok = await StokSatiriIzlenen(dto.YeniUrunId, subeId, islem.TenantId);

        // Y-23 BAĞI: satılabilir = adet − bloke. Rezerve mal değişimde de satılamaz.
        var satilabilir = BlokeKapisi.Satilabilir(stok);
        if (satilabilir < dto.YeniAdet)
            throw new BadRequestException(
                $"Yetersiz satılabilir stok. Stok {stok.Adet} adet, rezerve {stok.BlokeAdet} adet, " +
                $"satılabilir {satilabilir} adet; talep {dto.YeniAdet} adet.");

        var satisHareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            fark > 0m
                ? (farkTuru == "VERESIYE"
                    ? OdemeIzi.Veresiyeden(fark, vade)
                    : OdemeIzi.Nakitten(fark))
                : OdemeIzi.Yok,
            new Hareket
            {
                Id            = satisHareketId,
                TenantId      = islem.TenantId,
                SubeId        = subeId,
                Tur           = "SATIS",
                Tarih         = now,
                Adet          = dto.YeniAdet,
                Aciklama      = dto.Aciklama ?? "Değişim — verilen mal",
                TlTutari      = yeniToplam,
                HasGrami      = Yuvarla6(-dto.YeniAdet * yeniUrun.HasGrami),
                Maden         = Kuyumcu.Core.Domain.Ayarlar.MadenBul(yeniUrun.AyarId)
                                ?? Kuyumcu.Core.Domain.Maden.Altin,
                OdemeTuru     = farkTuru,
                KurSnapshotId = dto.KurSnapshotId,
                CariId        = cariId,
                UrunId        = dto.YeniUrunId,
                FisId         = fisId,
                KullaniciId   = islem.Id,
                Olusturuldu   = now,
            });

        await StokDegistir(dto.YeniUrunId, subeId, islem.TenantId,
            -dto.YeniAdet, yeniUrun.HasGrami, now, dto.ReyonId);
        await maliyet.PartiTuket(islem.TenantId, subeId, dto.YeniUrunId, dto.YeniAdet, now,
            await maliyet.Yontem(islem.TenantId), satisHareketId);

        // ── Fark bacağı — TEK SEFER ─────────────────────────────────────────
        if (fark != 0m)
        {
            if (kasa is not null)
                await KasaGuncelle(kasa, islem.TenantId, fark, now, satisHareketId, "NAKIT");
            else
                await CariBakiyeGuncelle(cariId!, islem.TenantId, fark, 0m, now);
        }

        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] DEGISIM: fiş={FisId} geri={Eski} yeni={Yeni} fark={Fark} TL",
            islem.TenantId, fisId, orijinal.UrunId, dto.YeniUrunId, fark);

        return new
        {
            fisId, iadeHareketId, satisHareketId,
            geriAlinanTutarTl = geriTutar, yeniToplamTl = yeniToplam, farkTl = fark,
            farkYonu = fark > 0m ? "MUSTERI_ODER" : fark < 0m ? "BIZ_IADE_EDERIZ" : "YOK",
            kalanIadeHakki = Yuvarla3(iadeEdilebilir - dto.GeriAlinanAdet),
        };
    }


    // ═══ Y-18 · SİPARİŞ → SATIŞ DÖNÜŞÜMÜ ═════════════════════════════════════

    /// <summary>
    /// HAZIR bir siparişi TEK TRANSACTION içinde satışa çevirir.
    ///
    /// <para>Tek çağrıda olan her şey: satış fişi (<see cref="SatisFisi"/> —
    /// kapıları KOPYALANMADAN yeniden kullanılır) · hareketlere
    /// <c>siparis_id</c> damgası · varsa avans mahsubu · bağlı rezervasyonların
    /// kapanması ve blokenin bırakılması · siparişin TESLIM_EDILDI'ye geçmesi
    /// ve append-only geçiş kaydı.</para>
    ///
    /// <para>⚠ YARIM DÖNÜŞÜM İMKÂNSIZ (plan DoD): hepsi TEK
    /// <c>BeginTransactionAsync</c> içindedir ve tek <c>CommitAsync</c> ile
    /// biter. Herhangi bir adım patlarsa satış da, damga da, durum geçişi de
    /// geri alınır. İki ayrı istekle yapılsaydı ("önce satışı gir, sonra
    /// siparişi kapat") araya giren bir hata siparişi satılmış ama AÇIK
    /// bırakırdı — mal ikinci kez satılırdı.</para>
    ///
    /// <para>⚠ ÇİFT DÖNÜŞÜM KAPISI: <c>hareketler.siparis_id</c> üzerinde bu
    /// siparişe bağlı bir SATIS satırı varsa 409 döner.</para>
    /// </summary>

    /// <summary>Satışa çevirmede durum makinesi kapısı (HAZIR → TESLIM_EDILDI). KAPORA-ESZAMAN tur 2:
    /// kilitten ÖNCE ve kilitten SONRAKİ (yeniden okunan) durumla iki kez çağrılır.</summary>
    private static void SatisaCevirDurumKapisi(Data.Siparis siparis)
    {
        if (!Siparis.SiparisDurumMakinesi.Izinli(siparis.Tur, siparis.Durum, SiparisDurumu.TeslimEdildi))
            throw new BadRequestException(
                $"Sipariş '{siparis.Durum}' durumundan satışa çevrilemez; önce HAZIR durumuna getirin.");
    }
    public async Task<object> SiparisiSatisaCevir(
        string siparisId, SiparisSatisaCevirDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem, "Sipariş dönüşümü için şube seçimi gerekli");

        var siparis = await db.Siparisler
            .FirstOrDefaultAsync(sp => sp.Id == siparisId && sp.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Sipariş bulunamadı");

        // TERMINAL_SONRASI_YAZAN_YOL = 0 — kapanmış sipariş deftere yazdıramaz.
        TerminalKapisi.Dogrula(siparis.Durum, SiparisDurumu.Terminal, "Sipariş", "satışa çevirme");

        SatisaCevirDurumKapisi(siparis);

        var zatenVar = await db.Hareketler.AsNoTracking()
            .AnyAsync(h => h.TenantId == islem.TenantId && h.SiparisId == siparisId && h.Tur == "SATIS");
        if (zatenVar)
            throw new ConflictException("Bu sipariş zaten satışa çevrilmiş; ikinci kez çevrilemez.");

        var kalemler = await db.SiparisKalemleri.AsNoTracking()
            .Where(k => k.SiparisId == siparisId && k.TenantId == islem.TenantId)
            .ToListAsync();
        if (kalemler.Count == 0)
            throw new BadRequestException("Kalemsiz sipariş satışa çevrilemez.");

        // Serbest tanımlı (ürünsüz) kalem satılamaz: stok/maliyet karşılığı yoktur.
        var urunsuz = kalemler.Where(k => string.IsNullOrEmpty(k.UrunId)).ToList();
        if (urunsuz.Count > 0)
            throw new BadRequestException(
                $"{urunsuz.Count} kalem katalog ürününe bağlı değil (serbest tanım); " +
                "satışa çevirmeden önce her kaleme bir ürün seçin.");

        // İstek her kalemi TAM karşılamalı — eksik fiyat gönderilirse kalem
        // sessizce düşerdi ve müşteri aldığı malın parasını ödemezdi.
        var fiyatHarita = dto.Kalemler.ToDictionary(k => k.SiparisKalemId, k => k);
        var eksik = kalemler.Where(k => !fiyatHarita.ContainsKey(k.Id)).ToList();
        if (eksik.Count > 0)
            throw new BadRequestException(
                $"{eksik.Count} sipariş kalemi için fiyat gönderilmedi; dönüşüm kısmi yapılamaz.");

        var fisDto = new SatisFisiDto
        {
            CariId        = siparis.CariId,
            KurSnapshotId = dto.KurSnapshotId,
            VadeGun       = dto.VadeGun,
            Aciklama      = dto.Aciklama ?? $"Sipariş {siparis.No} teslimi",
            Masak         = dto.Masak,
            Odemeler      = dto.Odemeler,
            Kalemler      = [.. kalemler.Select(k => new SatisFisKalemDto
            {
                UrunId       = k.UrunId!,
                Adet         = k.Adet,
                BirimFiyatTl = fiyatHarita[k.Id].BirimFiyatTl,
                IscilikTl    = fiyatHarita[k.Id].IscilikTl,
            })],
        };

        var now = DateTime.UtcNow;

        // ⚠ TEK TRANSACTION — SatisFisi bu açık transaction'a KATILIR (commit etmez).
        // PAKET MASAK-ESZAMAN: READ COMMITTED — SatisFisi'nin cari kilidi sonrası kümülatif okuması,
        // burada sipariş Reload'unun kurduğu anlık görüntüye takılmaz (güncel toplamı görür).
        await using var tx = await db.Database.BaslatVeyaDevral();
        // PAKET KAPORA-ESZAMAN (sınıf taraması): "zaten satışa çevrilmiş" ve durum kontrolü yukarıda
        // işlem DIŞINDA okunuyordu → iki eşzamanlı istek ikisi de geçip İKİ satış fişi yazabiliyordu.
        // Sipariş satırı kilitlenir, kayıt kilitten SONRA yeniden okunur ve iki kapı yinelenir.
        await db.Database.Kilitle(KilitTablosu.Siparisler, siparisId, islem.TenantId);
        await db.Entry(siparis).ReloadAsync();
        TerminalKapisi.Dogrula(siparis.Durum, SiparisDurumu.Terminal, "Sipariş", "satışa çevirme");
        // KAPORA-ESZAMAN tur 2 (doğrulayıcı DÜŞÜK): durum makinesi kapısı da kilitten SONRAKİ durumla yinelenir
        // (kilit beklerken sipariş HAZIR → ONAYLANDI'ya geri alınmış olabilir).
        SatisaCevirDurumKapisi(siparis);
        if (await db.Hareketler.AsNoTracking()
                .AnyAsync(h => h.TenantId == islem.TenantId && h.SiparisId == siparisId && h.Tur == "SATIS"))
            throw new ConflictException("Bu sipariş zaten satışa çevrilmiş; ikinci kez çevrilemez.");


        // Rezerve mal satılıyorsa blokeyi ÖNCE bırakırız; aksi hâlde SatisFisi'nin
        // satılabilir-stok kapısı kendi ayırdığımız malı reddederdi.
        var rezervler = await db.Rezervasyonlar
            .Where(r => r.TenantId == islem.TenantId && r.SiparisId == siparisId
                        && r.Durum == RezervasyonDurumu.Aktif)
            .ToListAsync();
        foreach (var r in rezervler)
        {
            var rstok = await StokSatiriIzlenen(r.UrunId, r.SubeId, islem.TenantId);
            BlokeKapisi.RezerveSatildi(rstok, r.Adet, now);
        }

        var fisSonuc = await SatisFisi(fisDto, islem);
        var fisId = (string)fisSonuc.GetType().GetProperty("fisId")!.GetValue(fisSonuc)!;

        // ── siparis_id damgası: fişin TÜM satırlarına ───────────────────────
        // ⚠ ExecuteUpdate KULLANILMAZ (beyaz liste dolu + denetim izini atlar):
        //   satırlar izlenen varlık olarak güncellenir.
        var fisSatirlari = await db.Hareketler
            .Where(h => h.TenantId == islem.TenantId && h.FisId == fisId).ToListAsync();
        string? ilkSatisId = null;
        foreach (var h in fisSatirlari)
        {
            h.SiparisId = siparisId;
            if (h.Tur == "SATIS") ilkSatisId ??= h.Id;
        }

        // ── Avans mahsubu (Y-17 ∩ Y-18) ─────────────────────────────────────
        decimal mahsupEdilen = 0m;
        if (dto.AvansMahsupTl is > 0m)
        {
            if (string.IsNullOrEmpty(siparis.CariId))
                throw new BadRequestException("Müşterisiz siparişte avans mahsubu yapılamaz.");
            var havuz = await AcikAvans(siparis.CariId, islem.TenantId);
            mahsupEdilen = AvansKapisi.MahsupEdilebilir(havuz, dto.AvansMahsupTl.Value);

            HareketFabrikasi.Ekle(db,
                OdemeIzi.Yok,   // ⚠ para el değiştirmez; cari bakiyeye DOKUNULMAZ (çifte sayma kapısı)
                new Hareket
                {
                    Id            = Kimlik.Ulid(),
                    TenantId      = islem.TenantId,
                    SubeId        = subeId,
                    Tur           = AvansKapisi.Mahsup,
                    Tarih         = now,
                    Aciklama      = $"Sipariş {siparis.No} avans mahsubu",
                    TlTutari      = Yuvarla2(-mahsupEdilen),
                    HasGrami      = 0m,
                    KurSnapshotId = dto.KurSnapshotId,
                    CariId        = siparis.CariId,
                    FisId         = ilkSatisId ?? fisId,
                    SiparisId     = siparisId,
                    KullaniciId   = islem.Id,
                    Olusturuldu   = now,
                });
        }

        // ── Rezervasyonları kapat ───────────────────────────────────────────
        foreach (var r in rezervler)
        {
            r.Durum          = RezervasyonDurumu.Satildi;
            r.SatisHareketId = ilkSatisId;
            r.Guncellendi    = now;
        }

        // ── Sipariş durum geçişi + append-only iz ────────────────────────────
        var onceki = siparis.Durum;
        siparis.Durum       = SiparisDurumu.TeslimEdildi;
        siparis.Guncellendi = now;
        db.SiparisGecisleri.Add(new SiparisGecisi
        {
            Id          = Kimlik.Ulid(),
            TenantId    = islem.TenantId,
            SiparisId   = siparisId,
            OncekiDurum = onceki,
            YeniDurum   = SiparisDurumu.TeslimEdildi,
            Gerekce     = $"Satışa çevrildi (fiş {fisId})",
            KullaniciId = islem.Id,
            Olusturuldu = now,
        });

        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] SIPARIS_SATIS: sipariş={SiparisId} fiş={FisId} kalem={Kalem} avansMahsup={Mahsup}",
            islem.TenantId, siparisId, fisId, kalemler.Count, mahsupEdilen);

        return new
        {
            siparisId, siparisNo = siparis.No, yeniDurum = siparis.Durum,
            fisId, satisHareketId = ilkSatisId, kalemSayisi = kalemler.Count,
            avansMahsupTl = mahsupEdilen,
            kapananRezervSayisi = rezervler.Count,
            acikAvans = string.IsNullOrEmpty(siparis.CariId)
                ? 0m : await AcikAvans(siparis.CariId, islem.TenantId),
        };
    }

    // ═══ Y-20 · KONSİNYE ÇIKIŞ / İADE ════════════════════════════════════════

    /// <summary>
    /// KONSİNYE ÇIKIŞ — BİZİM malımız satılmak üzere başka bir cariye gider.
    ///
    /// <para>⚠ SATIŞ DEĞİLDİR: mülkiyet bizdedir, para el değiştirmez. Bu
    /// yüzden TL bacağı 0, cari bakiye değişmez ve kasa hiç açılmaz. Mal
    /// stoktan ÇIKAR (fiziken bizde değildir) — çıkmasaydı sayım fazla verir
    /// ve tezgâhtar olmayan malı satmaya kalkardı.</para>
    /// </summary>
    public async Task<object> KonsinyeCikis(KonsinyeCikisDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem, "Konsinye çıkış için şube seçimi gerekli");
        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);
        await IslemDogrulayici.CariDogrula(db, dto.CariId, islem.TenantId);
        await IslemDogrulayici.CariAktifDogrula(db, dto.CariId, islem.TenantId);

        var urun = await UrunBul(dto.UrunId, islem.TenantId);
        Urun.UrunAdetKapisi.Dogrula(urun.Birim, dto.Adet, "adet");   // PAKET GOC-C (#6 · K14): ADET birimli üründe miktar tam sayı.
        var now  = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();

        var stok = await StokSatiriIzlenen(dto.UrunId, subeId, islem.TenantId);

        var satilabilir = BlokeKapisi.Satilabilir(stok);
        if (satilabilir < dto.Adet)
            throw new BadRequestException(
                $"Yetersiz satılabilir stok. Stok {stok.Adet} adet, rezerve {stok.BlokeAdet} adet, " +
                $"satılabilir {satilabilir} adet; talep {dto.Adet} adet.");

        var hareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            OdemeIzi.Yok,   // para EL DEĞİŞTİRMEZ
            new Hareket
            {
                Id            = hareketId,
                TenantId      = islem.TenantId,
                SubeId        = subeId,
                Tur           = KonsinyeKapisi21.Cikis,
                Tarih         = now,
                Adet          = dto.Adet,
                Aciklama      = dto.Aciklama,
                TlTutari      = 0m,
                HasGrami      = Yuvarla6(-dto.Adet * urun.HasGrami),
                Maden         = Kuyumcu.Core.Domain.Ayarlar.MadenBul(urun.AyarId)
                                ?? Kuyumcu.Core.Domain.Maden.Altin,
                KurSnapshotId = dto.KurSnapshotId,
                CariId        = dto.CariId,
                UrunId        = dto.UrunId,
                KullaniciId   = islem.Id,
                Olusturuldu   = now,
            });

        await StokDegistir(dto.UrunId, subeId, islem.TenantId, -dto.Adet, urun.HasGrami, now, dto.ReyonId);

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return new
        {
            hareketId, urunId = dto.UrunId, cariId = dto.CariId, adet = dto.Adet,
            stokAdet = stok.Adet, blokeAdet = stok.BlokeAdet,
            satilabilirAdet = BlokeKapisi.Satilabilir(stok),
        };
    }

    /// <summary>
    /// KONSİNYE İADE — gönderilen mal geri gelir. Stok GERİ ARTAR.
    /// Açık adet = çıkan − daha önce iade edilen (<see cref="KonsinyeKapisi21"/>).
    /// </summary>
    public async Task<object> KonsinyeIade(KonsinyeIadeDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem, "Konsinye iade için şube seçimi gerekli");
        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);

        var cikis = await db.Hareketler.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == dto.CikisHareketId && h.TenantId == islem.TenantId
                                      && h.Tur == KonsinyeKapisi21.Cikis)
            ?? throw new NotFoundException("Konsinye çıkış hareketi bulunamadı");

        var iadeEdilen = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tur == KonsinyeKapisi21.Iade
                        && h.IadeKaynakHareketId == cikis.Id)
            .SumAsync(h => (decimal?)h.Adet) ?? 0m;

        var adet = KonsinyeKapisi21.IadeEdilebilir(cikis.Adet ?? 0m, iadeEdilen, dto.Adet);

        var urun = await UrunBul(cikis.UrunId!, islem.TenantId);
        Urun.UrunAdetKapisi.Dogrula(urun.Birim, adet, "adet");   // PAKET GOC-C (#6 · K14): ADET birimli üründe miktar tam sayı.
        var now  = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();

        var hareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            OdemeIzi.Yok,
            new Hareket
            {
                Id                  = hareketId,
                TenantId            = islem.TenantId,
                SubeId              = subeId,
                Tur                 = KonsinyeKapisi21.Iade,
                Tarih               = now,
                Adet                = adet,
                Aciklama            = dto.Aciklama,
                TlTutari            = 0m,
                HasGrami            = Yuvarla6(adet * urun.HasGrami),
                Maden               = Kuyumcu.Core.Domain.Ayarlar.MadenBul(urun.AyarId)
                                      ?? Kuyumcu.Core.Domain.Maden.Altin,
                KurSnapshotId       = dto.KurSnapshotId,
                CariId              = cikis.CariId,
                UrunId              = cikis.UrunId,
                IadeKaynakHareketId = cikis.Id,
                KullaniciId         = islem.Id,
                Olusturuldu         = now,
            });

        await StokDegistir(cikis.UrunId!, subeId, islem.TenantId, adet, urun.HasGrami, now, dto.ReyonId);

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return new
        {
            hareketId, cikisHareketId = cikis.Id, adet,
            kalanAcikAdet = Yuvarla3((cikis.Adet ?? 0m) - iadeEdilen - adet),
        };
    }

    /// <summary>Açık konsinye çıkışları — defterden türetilir, ayna tablo yoktur.</summary>
    public async Task<object> KonsinyeAcik(AktifKullanici islem, string? cariId, int? limit)
    {
        var alinan = Math.Clamp(limit ?? 50, 1, 200);

        var cikisQ = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tur == KonsinyeKapisi21.Cikis);
        if (!string.IsNullOrWhiteSpace(cariId)) cikisQ = cikisQ.Where(h => h.CariId == cariId);

        var toplam = await cikisQ.CountAsync();
        var cikislar = await cikisQ.OrderByDescending(h => h.Olusturuldu).Take(alinan)
            .Select(h => new { h.Id, h.CariId, h.UrunId, h.Adet, h.Tarih, h.Aciklama })
            .ToListAsync();

        var kimlikler = cikislar.Select(c => c.Id).ToList();
        var iadeler = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tur == KonsinyeKapisi21.Iade
                        && h.IadeKaynakHareketId != null && kimlikler.Contains(h.IadeKaynakHareketId))
            .GroupBy(h => h.IadeKaynakHareketId!)
            .Select(g => new { Kaynak = g.Key, Adet = g.Sum(x => x.Adet ?? 0m) })
            .ToListAsync();
        var iadeHarita = iadeler.ToDictionary(i => i.Kaynak, i => i.Adet);

        return new
        {
            satirlar = cikislar.Select(c => new
            {
                c.Id, c.CariId, c.UrunId, c.Tarih, c.Aciklama,
                cikanAdet = c.Adet ?? 0m,
                iadeEdilenAdet = iadeHarita.TryGetValue(c.Id, out var i) ? i : 0m,
                acikAdet = Yuvarla3((c.Adet ?? 0m) - (iadeHarita.TryGetValue(c.Id, out var j) ? j : 0m)),
            }),
            toplam,
            limit = alinan,
            sinirAsildi = toplam > alinan,
        };
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

/// <summary>
/// PAKET P-21 uçları. Sunucu kapısı <c>[ModulYetkisi("islem")]</c> —
/// SatisController ile AYNI modül yetkisi (hepsi defter işlemidir).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/islemler")]
[Yetki.ModulYetkisi("islem")]
public sealed class TicaretController(SatisServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    // ── Rezervasyon (Y-23) ───────────────────────────────────────────────────

    /// <summary>POST /api/v1/islemler/rezervler — Rezervasyon aç (bloke artar).</summary>
    [HttpPost("rezervler")]
    [RolGerekli("SAHIP", "MUDUR", "TEZGAHTAR")]
    public async Task<IActionResult> RezervOlustur([FromBody] RezervOlusturDto dto)
        => StatusCode(201, await servis.RezervOlustur(dto, Islem));

    /// <summary>GET /api/v1/islemler/rezervler — Rezervasyon listesi.</summary>
    [HttpGet("rezervler")]
    public Task<object> RezervListele(
        [FromQuery] string? durum, [FromQuery] string? urunId,
        [FromQuery] string? cariId, [FromQuery] int? limit)
        => servis.RezervListele(Islem, durum, urunId, cariId, limit);

    /// <summary>PATCH /api/v1/islemler/rezervler/{id} — Gönderilmeyen alan KORUNUR.</summary>
    [HttpPatch("rezervler/{id}")]
    [RolGerekli("SAHIP", "MUDUR", "TEZGAHTAR")]
    public Task<object> RezervGuncelle(string id, [FromBody] System.Text.Json.JsonElement govde)
        => servis.RezervGuncelle(id, govde, Islem);

    /// <summary>POST /api/v1/islemler/rezervler/{id}/iptal — Blokeyi bırak (gerekçe zorunlu).</summary>
    [HttpPost("rezervler/{id}/iptal")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> RezervIptal(string id, [FromBody] RezervIptalDto dto)
        => servis.RezervIptal(id, dto, Islem);

    // ── Avans (Y-17) ─────────────────────────────────────────────────────────

    /// <summary>POST /api/v1/islemler/avans — Avans (kapora) tahsili.</summary>
    [HttpPost("avans")]
    [RolGerekli("SAHIP", "MUDUR", "TEZGAHTAR", "MUHASEBE")]
    public async Task<IActionResult> AvansAl([FromBody] AvansAlDto dto)
        => StatusCode(201, await servis.AvansAl(dto, Islem));

    /// <summary>POST /api/v1/islemler/avans/mahsup — Açık avansı bir satışa say.</summary>
    [HttpPost("avans/mahsup")]
    [RolGerekli("SAHIP", "MUDUR", "TEZGAHTAR", "MUHASEBE")]
    public Task<object> AvansMahsup([FromBody] AvansMahsupDto dto) => servis.AvansMahsup(dto, Islem);

    /// <summary>POST /api/v1/islemler/avans/iade — Avansı müşteriye geri öde.</summary>
    [HttpPost("avans/iade")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> AvansIade([FromBody] AvansIadeDto dto) => servis.AvansIade(dto, Islem);

    /// <summary>GET /api/v1/islemler/avans/{cariId} — Carinin açık avansı.</summary>
    [HttpGet("avans/{cariId}")]
    public async Task<object> AcikAvans(string cariId)
        => new { cariId, acikAvans = await servis.AcikAvans(cariId, Islem.TenantId) };

    // ── Değişim (Y-19) ───────────────────────────────────────────────────────

    /// <summary>POST /api/v1/islemler/degisim — Ürünü ürünle değiş (tek fiş).</summary>
    [HttpPost("degisim")]
    [RolGerekli("SAHIP", "MUDUR", "TEZGAHTAR")]
    public async Task<IActionResult> Degisim([FromBody] DegisimDto dto)
        => StatusCode(201, await servis.Degisim(dto, Islem));

    // ── Konsinye çıkış / iade (Y-20) ─────────────────────────────────────────

    /// <summary>POST /api/v1/islemler/konsinye-cikis — Bizden giden konsinye mal.</summary>
    [HttpPost("konsinye-cikis")]
    [RolGerekli("SAHIP", "MUDUR")]
    public async Task<IActionResult> KonsinyeCikis([FromBody] KonsinyeCikisDto dto)
        => StatusCode(201, await servis.KonsinyeCikis(dto, Islem));

    /// <summary>POST /api/v1/islemler/konsinye-iade — Konsinye mal geri geldi.</summary>
    [HttpPost("konsinye-iade")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> KonsinyeIade([FromBody] KonsinyeIadeDto dto) => servis.KonsinyeIade(dto, Islem);

    /// <summary>GET /api/v1/islemler/konsinye-acik — Açık konsinye çıkışları.</summary>
    [HttpGet("konsinye-acik")]
    public Task<object> KonsinyeAcik([FromQuery] string? cariId, [FromQuery] int? limit)
        => servis.KonsinyeAcik(Islem, cariId, limit);
}
