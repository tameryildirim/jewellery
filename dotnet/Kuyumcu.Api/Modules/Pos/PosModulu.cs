using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kuyumcu.Api.Modules.Pos;

// ═════════════════════════════════════════════════════════════════════════════
// POS MODÜLÜ — cihaz tanımı, POS alacak defteri, valör takvimi, yatma eşleştirme.
// Modül deseni KasaModulu.cs:13-21 ile birebir: DTO + Servis + Controller tek dosya.
//
// KİRACI SERTLİĞİ (ADR-0010): global HasQueryFilter tek başına DAYANAK SAYILMAZ;
// aşağıdaki HER sorgu ayrıca elle `TenantId ==` taşır. IgnoreQueryFilters /
// Find / FindAsync KULLANILMAZ; PK erişimi FirstOrDefaultAsync + TenantId ile.
//
// DoD SÖZLEŞMESİ (bu dosyanın kapsamı):
//   pos_cihazlari         → PATCH (kısmi) + pasife-al + aktifleştirme · DÜZENLENEBİLİR
//   pos_taksit_oranlari   → PATCH + pasife alma · DÜZENLENEBİLİR
//   pos_tahsilatlari      → DEFTER SATIRI, düzenlenemez; tek yol durum=IPTAL + gerekçe
//   pos_yatma_eslesmeleri → append-only; tek yol geri_alindi=true + gerekçe
// Hiçbir uçta Remove/RemoveRange/ExecuteDelete/ExecuteUpdate YOKTUR.
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class PosCihazOlusturDto
{
    [Required, MinLength(2), MaxLength(100)] public string Ad { get; set; } = null!;
    [Required, MaxLength(100)] public string BankaAdi { get; set; } = null!;
    [Required, MaxLength(36)] public string SubeId { get; set; } = null!;
    [Required, MaxLength(36)] public string KasaId { get; set; } = null!;
    [MaxLength(36)] public string? BankaHesapId { get; set; }

    [MaxLength(50)] public string? UyeIsyeriNo { get; set; }
    [MaxLength(50)] public string? TerminalNo { get; set; }

    /// <summary>0..1 — %100'ü aşan giriş SESSİZCE KIRPILMAZ, reddedilir (decimal(5,4) tuzağı).</summary>
    [Range(0, 1)] public decimal PesinKomisyonOran { get; set; }

    [Range(0, 90)] public int ValorGun { get; set; }
}

/// <summary>
/// KISMİ güncelleme. ⚠ TAM GÖVDE ÜZERİNE YAZMAZ: gönderilmeyen alan
/// "değişmedi" demektir (kurul engel 4 — sipariş PATCH'i kalem verisini
/// sessizce imha ediyordu). Bu yüzden HER alan nullable'dır ve servis yalnız
/// null OLMAYAN alanları yazar. Banka hesabını GERÇEKTEN temizlemek için
/// ayrı açık bayrak vardır (<see cref="BankaHesabiniTemizle"/>).
/// </summary>
public sealed class PosCihazGuncelleDto
{
    [MinLength(2), MaxLength(100)] public string? Ad { get; set; }
    [MaxLength(100)] public string? BankaAdi { get; set; }
    [MaxLength(50)] public string? UyeIsyeriNo { get; set; }
    [MaxLength(50)] public string? TerminalNo { get; set; }
    [Range(0, 1)] public decimal? PesinKomisyonOran { get; set; }
    [Range(0, 90)] public int? ValorGun { get; set; }
    [MaxLength(36)] public string? BankaHesapId { get; set; }

    /// <summary>
    /// QA turu 5 (YÜKSEK): bağlı kasa oluşturmadan sonra HİÇ düzenlenemiyordu ve
    /// ekrandaki düzeltme yolu bu alanla ilgisizdi. Kasa bağı artık DEĞİŞTİRİLEBİLİR,
    /// ama YALNIZ cihazın henüz hiç POS tahsilatı yokken: yazılmış alacak satırları
    /// kasa/şube bağını taşır, sonradan taşınırsa defter ile ekstre ayrışır.
    /// Şube bağı kasadan TÜRETİLİR (UX-S21) — kullanıcıdan ayrıca istenmez.
    /// </summary>
    [MaxLength(36)] public string? KasaId { get; set; }

    /// <summary>true → banka hesabı bağı SİLİNİR. null gönderimi bunu YAPMAZ.</summary>
    public bool? BankaHesabiniTemizle { get; set; }

    /// <summary>Pasife alınan cihaz aynı uçla geri açılır (UX-S25).</summary>
    public bool? Aktif { get; set; }
}

/// <summary>PAKET P-22 / B-55 — POS cihazının bağlı kasasının taşınması.</summary>
public sealed class PosKasaGocDto
{
    [Required, MaxLength(36)] public string YeniKasaId { get; set; } = null!;

    /// <summary>ZORUNLU — boş/kısa gerekçe 400 ile reddedilir.</summary>
    [Required, MaxLength(500)] public string Gerekce { get; set; } = null!;
}

public sealed class PosTahsilatIptalDto
{
    [Required, MinLength(20), MaxLength(1000)] public string Gerekce { get; set; } = null!;
}

public sealed class PosEslestirSatirDto
{
    [Required, MaxLength(36)] public string PosTahsilatId { get; set; } = null!;
    [Range(0.01, double.MaxValue)] public decimal Tutar { get; set; }
}

public sealed class PosEslestirDto
{
    [Required, MaxLength(36)] public string BankaHareketId { get; set; } = null!;
    [Required] public List<PosEslestirSatirDto> Satirlar { get; set; } = [];
}

public sealed class PosOtoOneriDto
{
    [Required, MaxLength(36)] public string BankaHareketId { get; set; } = null!;
}

public sealed class PosEslesmeGeriAlDto
{
    [Required, MinLength(20), MaxLength(1000)] public string Gerekce { get; set; } = null!;
}

public sealed class PosTaksitOranOlusturDto
{
    [Range(2, 12)] public int TaksitAdedi { get; set; }
    [Range(0, 1)] public decimal KomisyonOran { get; set; }
    [Range(0, 90)] public int? ValorGun { get; set; }
}

public sealed class PosTaksitOranGuncelleDto
{
    [Range(0, 1)] public decimal? KomisyonOran { get; set; }
    [Range(0, 90)] public int? ValorGun { get; set; }
    public bool? ValorGunuTemizle { get; set; }
    public bool? Aktif { get; set; }
}

// ── Yanıt sözleşmesi ─────────────────────────────────────────────────────────
// ⚠ TİP İKİZLENMESİ TUZAĞI (HAZIRLIK-KURULU-3 engel 11): istemci bu alanları
// birebir aynı adla ve decimal olarak okur (taksitAdedi ve valorGun dışında
// int YOKTUR). Alan adı değiştirilirse Kuyumcu.Web/Servisler/PosServisi.cs de
// aynı anda değişir.

public sealed record PosCihazSatir(
    string Id, string Ad, string BankaAdi, string SubeId, string KasaId,
    string? BankaHesapId, string? UyeIsyeriNo, string? TerminalNo,
    decimal PesinKomisyonOran, int ValorGun, string ParaBirimi, bool Aktif,
    decimal BekleyenAlacak, decimal BugunYatacakNet,
    // QA turu 5 (YÜKSEK): ekran "bağlı kasa düzenlenebilir mi?" sorusunu
    // TAHMİN ETMEZ; sunucu söyler (UX-S21). true ise kasa bağı kilitlidir.
    bool TahsilatVar = false);

public sealed record PosTahsilatSatir(
    string Id, string PosCihazId, string CihazAd, string BankaAdi,
    decimal BrutTutar, decimal KomisyonOran, decimal KomisyonTutar, decimal NetTutar,
    decimal YatanTutar, decimal KalanTutar, int TaksitAdedi, string ParaBirimi,
    DateTime IslemTarihi, DateTime ValorTarihi, string Durum, string? IptalGerekcesi);

public sealed record PosValorSatir(
    DateTime ValorTarihi, string CihazId, string CihazAd, string BankaAdi,
    decimal Brut, decimal Komisyon, decimal Net, decimal Yatan, decimal Kalan);

public sealed record PosTaksitOranSatir(
    string Id, string PosCihazId, int TaksitAdedi, decimal KomisyonOran,
    int? ValorGun, DateTime GecerliBaslangic, bool Aktif);

/// <summary>PAKET P-22 / B-55 — kasa göç tutanağı satırı (append-only).</summary>
public sealed record PosKasaGocSatir(
    string Id, string PosCihazId,
    string EskiKasaId, string EskiKasaAd,
    string YeniKasaId, string YeniKasaAd,
    int TahsilatSayisi, string Gerekce,
    string KullaniciId, string? KullaniciAd, DateTime Olusturuldu);

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class PosServisi(
    KuyumcuDbContext db, DenetimServisi denetim, IOptions<PosAyarlari>? ayarlarSecenek = null)
{
    private readonly PosAyarlari ayarlar = ayarlarSecenek?.Value ?? new PosAyarlari();

    private static decimal Yuvarla2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);

    /// <summary>Sunucu sınırı — ekranda AÇIKÇA yazılır (UX-S29), sessiz kırpma yok.</summary>
    public const int AzamiSatir = 200;

    // ── Cihaz ────────────────────────────────────────────────────────────────

    public async Task<object> CihazOlustur(PosCihazOlusturDto dto, AktifKullanici islem)
    {
        var ad = (dto.Ad ?? "").Trim();
        // DataAnnotations YALNIZ controller yolunda koşar → servis içi elle kontrol.
        if (ad.Length < 2) throw new BadRequestException("Cihaz adı en az 2 karakter olmalıdır");
        if (string.IsNullOrWhiteSpace(dto.BankaAdi)) throw new BadRequestException("Banka adı zorunludur");
        OranDogrula(dto.PesinKomisyonOran);
        ValorDogrula(dto.ValorGun);

        await IslemDogrulayici.SubeDogrula(db, dto.SubeId, islem.TenantId);

        var kasa = await db.Kasalar
            .FirstOrDefaultAsync(k => k.Id == dto.KasaId && k.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Kasa bulunamadı");
        if (kasa.SubeId != dto.SubeId)
            throw new BadRequestException("Kasa bu şubeye ait değil");
        // K6 kasa para birimi kapısının POS karşılığı. PAKET-15: POS yalnız TRY.
        if (kasa.ParaBirimi != "TRY")
            throw new BadRequestException("POS cihazı yalnız TRY kasasına bağlanabilir (PAKET-15 kararı)");

        if (!string.IsNullOrEmpty(dto.BankaHesapId))
        {
            var hesap = await db.BankaHesaplari
                .FirstOrDefaultAsync(h => h.Id == dto.BankaHesapId && h.TenantId == islem.TenantId)
                ?? throw new NotFoundException("Banka hesabı bulunamadı");
            if (hesap.ParaBirimi != "TRY")
                throw new BadRequestException("POS cihazı yalnız TRY banka hesabına bağlanabilir");
        }

        if (await db.PosCihazlari.AnyAsync(c =>
                c.TenantId == islem.TenantId && c.Ad == ad))
            throw new ConflictException($"'{ad}' adlı POS cihazı zaten mevcut");

        var now = DateTime.UtcNow;
        var cihaz = new PosCihaz
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            SubeId = dto.SubeId,
            Ad = ad,
            BankaAdi = dto.BankaAdi.Trim(),
            UyeIsyeriNo = dto.UyeIsyeriNo,
            TerminalNo = dto.TerminalNo,
            KasaId = dto.KasaId,
            BankaHesapId = string.IsNullOrEmpty(dto.BankaHesapId) ? null : dto.BankaHesapId,
            ParaBirimi = "TRY",
            PesinKomisyonOran = dto.PesinKomisyonOran,
            ValorGun = dto.ValorGun,
            Aktif = true,
            Olusturuldu = now,
            Guncellendi = now,
        };
        db.PosCihazlari.Add(cihaz);
        denetim.YazmaKaydet(islem, nameof(PosCihaz), cihaz.Id,
            $"POS cihazı tanımlandı: {cihaz.Ad}", hassas: false);
        await db.SaveChangesAsync();
        return await CihazGetir(cihaz.Id, islem);
    }

    public async Task<List<PosCihazSatir>> CihazListele(AktifKullanici islem, string? subeId, bool? aktif)
    {
        // PAKET ŞUBE-KAPSAM-A: görünüm şubesi tek yoldan çözülür ve daraltılır.
        var sube = await GorunumSubesi.Coz(db, islem, subeId);
        var q = db.PosCihazlari.AsNoTracking().Daralt(sube).Where(c => c.TenantId == islem.TenantId);
        if (aktif is { } a) q = q.Where(c => c.Aktif == a);
        var cihazlar = await q.OrderBy(c => c.Ad).ToListAsync();

        // Bekleyen alacak SUNUCUDA toplanır (TB-19): satır listesi çekilip
        // bellekte toplanmaz. Bugün yatacak net de aynı sorgudan gelir.
        var bugunSonu = Zaman.YerelGunBasiUtc(Zaman.YerelBugun().AddDays(1));
        var ozet = await db.PosTahsilatlari.AsNoTracking()
            .Where(t => t.TenantId == islem.TenantId && t.Durum != PosTahsilatDurumu.Iptal)
            .GroupBy(t => t.PosCihazId)
            .Select(g => new
            {
                CihazId = g.Key,
                Bekleyen = g.Sum(x => x.NetTutar - x.YatanTutar),
                Bugun = g.Sum(x => x.ValorTarihi < bugunSonu ? x.NetTutar - x.YatanTutar : 0m),
            })
            .ToListAsync();

        // Kasa bağının kilitli olup olmadığı İPTAL EDİLMİŞ tahsilatı da sayar:
        // iptal satırı da o kasaya yazılmış bir defter izidir.
        var tahsilatliCihazlar = await db.PosTahsilatlari.AsNoTracking()
            .Where(t => t.TenantId == islem.TenantId)
            .Select(t => t.PosCihazId)
            .Distinct()
            .ToListAsync();

        return cihazlar.Select(c =>
        {
            var o = ozet.FirstOrDefault(x => x.CihazId == c.Id);
            return new PosCihazSatir(c.Id, c.Ad, c.BankaAdi, c.SubeId, c.KasaId,
                c.BankaHesapId, c.UyeIsyeriNo, c.TerminalNo, c.PesinKomisyonOran,
                c.ValorGun, c.ParaBirimi, c.Aktif,
                Yuvarla2(o?.Bekleyen ?? 0m), Yuvarla2(o?.Bugun ?? 0m),
                tahsilatliCihazlar.Contains(c.Id));
        }).ToList();
    }

    public async Task<object> CihazGetir(string id, AktifKullanici islem)
    {
        var cihaz = await CihazBul(id, islem.TenantId);
        var bekleyen = await db.PosTahsilatlari.AsNoTracking()
            .Where(t => t.TenantId == islem.TenantId && t.PosCihazId == id
                        && t.Durum != PosTahsilatDurumu.Iptal)
            .SumAsync(t => (decimal?)(t.NetTutar - t.YatanTutar)) ?? 0m;

        var oranlar = await TaksitOranlariGetir(id, islem.TenantId);

        return new
        {
            cihaz = new PosCihazSatir(cihaz.Id, cihaz.Ad, cihaz.BankaAdi, cihaz.SubeId,
                cihaz.KasaId, cihaz.BankaHesapId, cihaz.UyeIsyeriNo, cihaz.TerminalNo,
                cihaz.PesinKomisyonOran, cihaz.ValorGun, cihaz.ParaBirimi, cihaz.Aktif,
                Yuvarla2(bekleyen), 0m),
            taksitOranlari = oranlar,
        };
    }

    public async Task<object> CihazGuncelle(string id, PosCihazGuncelleDto dto, AktifKullanici islem)
    {
        var cihaz = await CihazBul(id, islem.TenantId);
        var degisen = new List<string>();

        // ⚠ PATCH SÖZLEŞMESİ: yalnız null OLMAYAN alanlar yazılır. Gönderilmeyen
        // pesin_komisyon_oran / valor_gun / kasa_id / banka_hesap_id AYNEN KALIR.
        if (dto.Ad is not null)
        {
            var ad = dto.Ad.Trim();
            if (ad.Length < 2) throw new BadRequestException("Cihaz adı en az 2 karakter olmalıdır").AlanIle("ad");
            if (ad != cihaz.Ad)
            {
                if (await db.PosCihazlari.AnyAsync(c =>
                        c.TenantId == islem.TenantId && c.Ad == ad && c.Id != id))
                    throw new ConflictException($"'{ad}' adlı POS cihazı zaten mevcut");
                cihaz.Ad = ad; degisen.Add("ad");
            }
        }
        // PAKET PATCH-5 (PATCH-3 sözleşmesi): zorunlu banka adı null = KORU, "" / boşluk = 400 alan (eskiden
        // sessizce yok sayılıyordu); isteğe bağlı üye işyeri / terminal no "" = SİL (kolona "" değil NULL).
        if (dto.BankaAdi is not null)
        {
            var bankaAdi = dto.BankaAdi.Trim();
            if (bankaAdi.Length == 0) throw new BadRequestException("Banka adı boşaltılamaz").AlanIle("bankaAdi");
            cihaz.BankaAdi = bankaAdi; degisen.Add("banka_adi");
        }
        if (dto.UyeIsyeriNo is not null) { cihaz.UyeIsyeriNo = string.IsNullOrWhiteSpace(dto.UyeIsyeriNo) ? null : dto.UyeIsyeriNo.Trim(); degisen.Add("uye_isyeri_no"); }
        if (dto.TerminalNo is not null) { cihaz.TerminalNo = string.IsNullOrWhiteSpace(dto.TerminalNo) ? null : dto.TerminalNo.Trim(); degisen.Add("terminal_no"); }

        // ⚠ ORAN/VALÖR DEĞİŞİKLİĞİ GEÇMİŞE İŞLEMEZ: yazılmış pos_tahsilatlari
        // satırları kendi snapshot oranını taşır ve DEĞİŞMEZ.
        if (dto.PesinKomisyonOran is { } oran)
        {
            OranDogrula(oran);
            cihaz.PesinKomisyonOran = oran; degisen.Add("pesin_komisyon_oran");
        }
        if (dto.ValorGun is { } valor)
        {
            ValorDogrula(valor);
            cihaz.ValorGun = valor; degisen.Add("valor_gun");
        }

        // QA turu 5 (YÜKSEK): bağlı kasa düzenlenebilir — yalnız tahsilatı yokken.
        if (!string.IsNullOrWhiteSpace(dto.KasaId) && dto.KasaId != cihaz.KasaId)
        {
            if (await db.PosTahsilatlari.AnyAsync(t =>
                    t.TenantId == islem.TenantId && t.PosCihazId == cihaz.Id))
                // ⚠ B-56: bu 409 KORUNUR — sessiz alan güncellemesiyle kasa
                //   taşınmaz. Ama artık ÇIKMAZ DEĞİLDİR: B-55 göç ucu gösterilir.
                throw new ConflictException(
                    "Bu cihazda POS tahsilatı var; bağlı kasa sessizce değiştirilemez. "
                    + "Doğru yol: POST /api/v1/pos/cihazlar/{id}/kasa-goc ile gerekçeli "
                    + "kasa göçü yapın (geçmiş tahsilatlar eski kasada kalır).");

            var yeniKasa = await db.Kasalar.AsNoTracking()
                .FirstOrDefaultAsync(k => k.Id == dto.KasaId && k.TenantId == islem.TenantId)
                ?? throw new NotFoundException("Kasa bulunamadı");
            if (!yeniKasa.Aktif) throw new BadRequestException("Pasif kasaya POS cihazı bağlanamaz");
            // Oluşturma kapısının BİREBİR aynısı (PAKET-15: POS yalnız TRY).
            if (yeniKasa.ParaBirimi != "TRY")
                throw new BadRequestException(
                    "POS cihazı yalnız TRY kasasına bağlanabilir (PAKET-15 kararı)");

            cihaz.KasaId = yeniKasa.Id; degisen.Add("kasa_id");
            // Şube kasadan TÜRETİLİR; iki ayrı gerçek olmaz (UX-S21).
            if (cihaz.SubeId != yeniKasa.SubeId)
            {
                cihaz.SubeId = yeniKasa.SubeId; degisen.Add("sube_id");
            }
        }

        if (dto.BankaHesabiniTemizle == true)
        {
            cihaz.BankaHesapId = null; degisen.Add("banka_hesap_id(silindi)");
        }
        else if (!string.IsNullOrEmpty(dto.BankaHesapId))
        {
            var hesap = await db.BankaHesaplari
                .FirstOrDefaultAsync(h => h.Id == dto.BankaHesapId && h.TenantId == islem.TenantId)
                ?? throw new NotFoundException("Banka hesabı bulunamadı");
            if (hesap.ParaBirimi != "TRY")
                throw new BadRequestException("POS cihazı yalnız TRY banka hesabına bağlanabilir");
            cihaz.BankaHesapId = hesap.Id; degisen.Add("banka_hesap_id");
        }

        if (dto.Aktif is { } yeniAktif && yeniAktif != cihaz.Aktif)
        {
            if (!yeniAktif) await AcikAlacakYokDogrula(id, islem.TenantId);
            cihaz.Aktif = yeniAktif;
            degisen.Add(yeniAktif ? "aktif(açıldı)" : "aktif(pasife alındı)");
        }

        if (degisen.Count > 0)
        {
            cihaz.Guncellendi = DateTime.UtcNow;
            denetim.YazmaKaydet(islem, nameof(PosCihaz), cihaz.Id,
                $"POS cihazı güncellendi · değişen alanlar: {string.Join(", ", degisen)}", hassas: false);
            await db.SaveChangesAsync();
        }
        return await CihazGetir(id, islem);
    }

    /// <summary>
    /// Cihazı kapatır. ⚠ FİNANSAL VARLIKTA HARD DELETE YASAK: silme ucu YOKTUR.
    /// Açık (BEKLIYOR/KISMI) tahsilatı olan cihaz kapatılamaz — fail-closed.
    /// </summary>
    public async Task<object> CihazPasifeAl(string id, AktifKullanici islem)
    {
        var cihaz = await CihazBul(id, islem.TenantId);
        if (cihaz.Aktif)
        {
            await AcikAlacakYokDogrula(id, islem.TenantId);
            cihaz.Aktif = false;
            cihaz.Guncellendi = DateTime.UtcNow;
            denetim.YazmaKaydet(islem, nameof(PosCihaz), cihaz.Id, "POS cihazı pasife alındı", hassas: false);
            await db.SaveChangesAsync();
        }
        return new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("POS cihazı pasife alındı"), aktif = false };
    }

    // ── B-55: KASA GÖÇÜ — "tahsilat var, kasa kilitli" çıkmazının çıkışı ─────

    /// <summary>
    /// Cihazın bağlı kasasını TAŞIR. Tahsilat varken de çalışır — bu yüzden
    /// PATCH'ten AYRI bir uçtur: PATCH sessiz bir alan güncellemesidir, göç ise
    /// tutanaklı bir olaydır.
    ///
    /// <para>DÖRT ŞART BİRLİKTE (K-23 ile aynı doktrin):</para>
    /// <list type="number">
    ///   <item>Yetki — uçta <c>[RolGerekli("SAHIP","MUDUR")]</c>.</item>
    ///   <item>Gerekçe ZORUNLU — <see cref="PosKasaGocu.GerekceAsgari"/> karakter.</item>
    ///   <item>İz — <c>pos_kasa_gocleri</c> satırı + <c>DenetimServisi.YazmaKaydet</c>.</item>
    ///   <item>Görünürlük — tutanak silinmez, cihaz kartında listelenir.</item>
    /// </list>
    ///
    /// <para>
    /// ⚠ GEÇMİŞE İŞLEMEZ: yazılmış <c>pos_tahsilatlari</c> satırları kendi
    /// <c>kasa_hareket_id</c>'sine bağlıdır ve DOKUNULMAZ. Göç yalnız bundan
    /// sonraki tahsilatların düşeceği kasayı değiştirir; bu, ekranda da yazılır.
    /// </para>
    /// </summary>
    public async Task<object> KasaGoc(string cihazId, PosKasaGocDto dto, AktifKullanici islem)
    {
        var cihaz = await CihazBul(cihazId, islem.TenantId);

        var gerekce = (dto.Gerekce ?? "").Trim();
        if (gerekce.Length < PosKasaGocu.GerekceAsgari)
            throw new BadRequestException(
                $"Kasa göçü için gerekçe zorunlu (en az {PosKasaGocu.GerekceAsgari} karakter).");

        var yeniKasaId = (dto.YeniKasaId ?? "").Trim();
        if (yeniKasaId.Length == 0)
            throw new BadRequestException("Yeni kasa seçimi zorunlu");
        if (yeniKasaId == cihaz.KasaId)
            throw new BadRequestException("Cihaz zaten bu kasaya bağlı; göç edecek bir şey yok.");

        var yeniKasa = await db.Kasalar.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == yeniKasaId && k.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Kasa bulunamadı");
        if (!yeniKasa.Aktif) throw new BadRequestException("Pasif kasaya POS cihazı bağlanamaz");
        // Oluşturma ve PATCH kapılarının BİREBİR aynısı (PAKET-15: POS yalnız TRY).
        if (yeniKasa.ParaBirimi != "TRY")
            throw new BadRequestException(
                "POS cihazı yalnız TRY kasasına bağlanabilir (PAKET-15 kararı)");

        var tahsilatSayisi = await db.PosTahsilatlari
            .CountAsync(t => t.TenantId == islem.TenantId && t.PosCihazId == cihaz.Id);

        var eskiKasaId = cihaz.KasaId;
        var now = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();
        db.PosKasaGocleri.Add(new PosKasaGocu
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            PosCihazId = cihaz.Id,
            EskiKasaId = eskiKasaId,
            YeniKasaId = yeniKasa.Id,
            TahsilatSayisi = tahsilatSayisi,
            Gerekce = gerekce.Length <= 500 ? gerekce : gerekce[..500],
            KullaniciId = islem.Id,
            Olusturuldu = now,
        });

        cihaz.KasaId = yeniKasa.Id;
        // Şube kasadan TÜRETİLİR; iki ayrı gerçek olmaz (UX-S21).
        cihaz.SubeId = yeniKasa.SubeId;
        cihaz.Guncellendi = now;

        denetim.YazmaKaydet(islem, nameof(PosCihaz), cihaz.Id,
            $"POS cihazının bağlı kasası taşındı: {eskiKasaId} → {yeniKasa.Id} "
            + $"(mevcut tahsilat {tahsilatSayisi}) · gerekçe: {gerekce}", hassas: false);

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return new
        {
            mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("POS cihazının bağlı kasası taşındı"),
            posCihazId = cihaz.Id,
            eskiKasaId,
            yeniKasaId = yeniKasa.Id,
            tahsilatSayisi,
            gecmisEtkilenmedi = true,
            gocler = await KasaGocleri(cihaz.Id, islem.TenantId),
        };
    }

    /// <summary>Cihazın kasa göç tutanakları — append-only, eskiden yeniye.</summary>
    public async Task<List<PosKasaGocSatir>> KasaGocleri(string cihazId, string tenantId)
    {
        var satirlar = await db.PosKasaGocleri.AsNoTracking()
            .Where(g => g.TenantId == tenantId && g.PosCihazId == cihazId)
            .OrderBy(g => g.Olusturuldu).ThenBy(g => g.Id)
            .ToListAsync();
        if (satirlar.Count == 0) return [];

        var kasaIdler = satirlar.Select(g => g.EskiKasaId)
            .Concat(satirlar.Select(g => g.YeniKasaId)).Distinct().ToList();
        var kasaAdlari = await db.Kasalar.AsNoTracking()
            .Where(k => k.TenantId == tenantId && kasaIdler.Contains(k.Id))
            .ToDictionaryAsync(k => k.Id, k => k.Ad);
        var kullaniciIdler = satirlar.Select(g => g.KullaniciId).Distinct().ToList();
        var kullanicilar = await db.Kullanicilar.AsNoTracking()
            .Where(k => k.TenantId == tenantId && kullaniciIdler.Contains(k.Id))
            .ToDictionaryAsync(k => k.Id, k => k.Ad + " " + k.Soyad);

        return [.. satirlar.Select(g => new PosKasaGocSatir(
            g.Id, g.PosCihazId,
            g.EskiKasaId, kasaAdlari.GetValueOrDefault(g.EskiKasaId, "—"),
            g.YeniKasaId, kasaAdlari.GetValueOrDefault(g.YeniKasaId, "—"),
            g.TahsilatSayisi, g.Gerekce, g.KullaniciId,
            kullanicilar.GetValueOrDefault(g.KullaniciId), g.Olusturuldu))];
    }

    private async Task AcikAlacakYokDogrula(string cihazId, string tenantId)
    {
        var acik = await db.PosTahsilatlari
            .CountAsync(t => t.TenantId == tenantId && t.PosCihazId == cihazId
                             && (t.Durum == PosTahsilatDurumu.Bekliyor || t.Durum == PosTahsilatDurumu.Kismi));
        if (acik > 0)
            throw new BadRequestException(
                $"Açık POS alacağı olan cihaz pasife alınamaz ({acik} tahsilat bekliyor). " +
                "Önce banka yatmalarını eşleştirin ya da tahsilatları iptal edin.");
    }

    // ── Taksit oranları (Faz B) ──────────────────────────────────────────────

    private async Task<List<PosTaksitOranSatir>> TaksitOranlariGetir(string cihazId, string tenantId) =>
        (await db.PosTaksitOranlari.AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.PosCihazId == cihazId)
            .OrderBy(o => o.TaksitAdedi).ThenByDescending(o => o.GecerliBaslangic)
            .ToListAsync())
        .Select(o => new PosTaksitOranSatir(o.Id, o.PosCihazId, o.TaksitAdedi,
            o.KomisyonOran, o.ValorGun, o.GecerliBaslangic, o.Aktif))
        .ToList();

    public async Task<object> TaksitOranEkle(string cihazId, PosTaksitOranOlusturDto dto, AktifKullanici islem)
    {
        var cihaz = await CihazBul(cihazId, islem.TenantId);
        if (dto.TaksitAdedi is < 2 or > 12)
            throw new BadRequestException("Taksit adedi 2 ile 12 arasında olmalıdır");
        OranDogrula(dto.KomisyonOran);
        if (dto.ValorGun is { } v) ValorDogrula(v);

        var now = DateTime.UtcNow;
        // Aynı taksit adedinde önceki AKTİF oran pasife alınır (tarihçe SİLİNMEZ).
        var oncekiler = await db.PosTaksitOranlari
            .Where(o => o.TenantId == islem.TenantId && o.PosCihazId == cihazId
                        && o.TaksitAdedi == dto.TaksitAdedi && o.Aktif)
            .ToListAsync();
        foreach (var o in oncekiler) o.Aktif = false;

        var satir = new PosTaksitOran
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            PosCihazId = cihaz.Id,
            TaksitAdedi = dto.TaksitAdedi,
            KomisyonOran = dto.KomisyonOran,
            ValorGun = dto.ValorGun,
            GecerliBaslangic = now,
            Aktif = true,
            Olusturuldu = now,
        };
        db.PosTaksitOranlari.Add(satir);
        denetim.YazmaKaydet(islem, nameof(PosTaksitOran), satir.Id,
            $"taksit oranı tanımlandı: {dto.TaksitAdedi} taksit", hassas: false);
        await db.SaveChangesAsync();
        return await CihazGetir(cihazId, islem);
    }

    public async Task<object> TaksitOranGuncelle(
        string cihazId, string oranId, PosTaksitOranGuncelleDto dto, AktifKullanici islem)
    {
        await CihazBul(cihazId, islem.TenantId);
        var o = await db.PosTaksitOranlari
            .FirstOrDefaultAsync(x => x.Id == oranId && x.TenantId == islem.TenantId
                                      && x.PosCihazId == cihazId)
            ?? throw new NotFoundException("Taksit oranı bulunamadı");

        var degisen = new List<string>();
        if (dto.KomisyonOran is { } oran) { OranDogrula(oran); o.KomisyonOran = oran; degisen.Add("komisyon_oran"); }
        if (dto.ValorGunuTemizle == true) { o.ValorGun = null; degisen.Add("valor_gun(silindi)"); }
        else if (dto.ValorGun is { } v) { ValorDogrula(v); o.ValorGun = v; degisen.Add("valor_gun"); }
        if (dto.Aktif is { } a && a != o.Aktif) { o.Aktif = a; degisen.Add(a ? "aktif(açıldı)" : "aktif(kapatıldı)"); }

        if (degisen.Count > 0)
        {
            denetim.YazmaKaydet(islem, nameof(PosTaksitOran), o.Id,
                $"taksit oranı güncellendi · değişen alanlar: {string.Join(", ", degisen)}", hassas: false);
            await db.SaveChangesAsync();
        }
        return await CihazGetir(cihazId, islem);
    }

    // ── Tahsilat defteri ─────────────────────────────────────────────────────

    public async Task<object> TahsilatListele(
        AktifKullanici islem, string? durum, string? bas, string? bit,
        string? cihazId, int limit, int offset, string? subeId = null)
    {
        if (limit is < 1 or > AzamiSatir) limit = AzamiSatir;
        if (offset < 0) offset = 0;

        // PAKET ŞUBE-KAPSAM-A: görünüm şubesi (null = tüm şubeler).
        var sube = await GorunumSubesi.Coz(db, islem, subeId);
        var q = db.PosTahsilatlari.AsNoTracking().Daralt(sube).Where(t => t.TenantId == islem.TenantId);
        // PAKET GOC-A (envanter C · pos-t-durum/bas/bit — "sessiz süzgeçsiz sonuç"): bilinmeyen durum,
        // okunamayan tarih ve ters aralık 400'dür (K32 beyaz liste · K8e) — boş liste ile gizlenmez.
        if (!string.IsNullOrEmpty(durum) && Array.IndexOf(TahsilatDurumlari, durum) < 0)
        {
            // GOC-A tur 2 (hasım 08.10): iç kodlar (BEKLIYOR, KISMI …) kullanıcıya gitmez — sözlük etiketleri.
            var p = Kuyumcu.Core.I18n.Sozlukler.Getir(Kuyumcu.Core.I18n.IstekDili.Gecerli).Pos;
            throw new BadRequestException(string.Format(Kuyumcu.Core.I18n.GocASozlugu.Al(Kuyumcu.Core.I18n.IstekDili.Gecerli).PosDurumBilinmiyor,
                string.Join(", ", p.DurumBekliyor, p.DurumKismi, p.DurumYatti, p.DurumIptal))).AlanIle("durum");
        }
        var basGun = SorguGunu(bas, "bas");
        var bitGun = SorguGunu(bit, "bit");
        GirdiKapisi.Aralik(basGun, bitGun, "bit");
        if (!string.IsNullOrEmpty(durum)) q = q.Where(t => t.Durum == durum);
        if (!string.IsNullOrEmpty(cihazId)) q = q.Where(t => t.PosCihazId == cihazId);

        // K5: bas/bit YEREL gündür, UTC aralığına Zaman ile çevrilir.
        if (basGun is { } b1) q = q.Where(t => t.IslemTarihi >= Zaman.YerelGunBasiUtc(b1));
        if (bitGun is { } b2) q = q.Where(t => t.IslemTarihi < Zaman.YerelGunBasiUtc(b2.AddDays(1)));

        var toplamSatir = await q.CountAsync();
        var sayfa = await q.OrderByDescending(t => t.IslemTarihi)
            .Skip(offset).Take(limit).ToListAsync();

        var cihazlar = await db.PosCihazlari.AsNoTracking()
            .Where(c => c.TenantId == islem.TenantId)
            .Select(c => new { c.Id, c.Ad, c.BankaAdi }).ToListAsync();

        var satirlar = sayfa.Select(t =>
        {
            var c = cihazlar.FirstOrDefault(x => x.Id == t.PosCihazId);
            return new PosTahsilatSatir(t.Id, t.PosCihazId, c?.Ad ?? "—", c?.BankaAdi ?? "—",
                t.BrutTutar, t.KomisyonOran, t.KomisyonTutar, t.NetTutar, t.YatanTutar,
                Yuvarla2(t.NetTutar - t.YatanTutar), t.TaksitAdedi, t.ParaBirimi,
                t.IslemTarihi, t.ValorTarihi, t.Durum, t.IptalGerekcesi);
        }).ToList();

        return new
        {
            gorunumSubesi = sube,
            kapsam = GorunumSubesi.Kapsam(sube),
            satirlar,
            toplamSatir,
            limit,
            offset,
            // UX-S29: sunucu sınırı ekranda SÖYLENİR, sessiz kırpma yok.
            azamiSatir = AzamiSatir,
            sinirDegdi = offset + satirlar.Count < toplamSatir,
        };
    }

    /// <summary>
    /// Valör takvimi — ⚠ SATIR LİSTESİ DÖNMEZ (TB-19). GroupBy + Sum
    /// VERİTABANINDA koşar; SQL'de GROUP BY görünür. ToListAsync + bellekte
    /// GroupBy'a dönmek ValorTakvimi_SunucudaToplanir_SatirDonmez testini kırar.
    /// </summary>
    public async Task<List<PosValorSatir>> ValorTakvimi(
        AktifKullanici islem, string? bas, string? bit, string? subeId = null)
    {
        var sube = await GorunumSubesi.Coz(db, islem, subeId);   // PAKET ŞUBE-KAPSAM-A
        var q = db.PosTahsilatlari.AsNoTracking().Daralt(sube)
            .Where(t => t.TenantId == islem.TenantId && t.Durum != PosTahsilatDurumu.Iptal);
        if (GunCoz(bas) is { } b1) q = q.Where(t => t.ValorTarihi >= Zaman.YerelGunBasiUtc(b1));
        if (GunCoz(bit) is { } b2) q = q.Where(t => t.ValorTarihi < Zaman.YerelGunBasiUtc(b2.AddDays(1)));

        var gruplar = await q
            .GroupBy(t => new { t.ValorTarihi, t.PosCihazId })
            .Select(g => new
            {
                g.Key.ValorTarihi,
                g.Key.PosCihazId,
                Brut = g.Sum(x => x.BrutTutar),
                Komisyon = g.Sum(x => x.KomisyonTutar),
                Net = g.Sum(x => x.NetTutar),
                Yatan = g.Sum(x => x.YatanTutar),
            })
            .OrderBy(x => x.ValorTarihi)
            .ToListAsync();

        var cihazlar = await db.PosCihazlari.AsNoTracking()
            .Where(c => c.TenantId == islem.TenantId)
            .Select(c => new { c.Id, c.Ad, c.BankaAdi }).ToListAsync();

        return gruplar.Select(g =>
        {
            var c = cihazlar.FirstOrDefault(x => x.Id == g.PosCihazId);
            return new PosValorSatir(g.ValorTarihi, g.PosCihazId, c?.Ad ?? "—", c?.BankaAdi ?? "—",
                Yuvarla2(g.Brut), Yuvarla2(g.Komisyon), Yuvarla2(g.Net), Yuvarla2(g.Yatan),
                Yuvarla2(g.Net - g.Yatan));
        }).ToList();
    }

    /// <summary>
    /// Chargeback / iptal. ⚠ SATIR SİLİNMEZ: durum=IPTAL + gerekçe yazılır.
    /// Eşleşmiş (yatan &gt; 0) tahsilat iptal EDİLEMEZ — önce eşleşme geri alınır.
    /// </summary>
    public async Task<object> TahsilatIptal(string id, PosTahsilatIptalDto dto, AktifKullanici islem)
    {
        // DataAnnotations yalnız controller yolunda koşar → elle tekrar.
        var gerekce = (dto.Gerekce ?? "").Trim();
        if (gerekce.Length < 20)
            throw new BadRequestException("İptal gerekçesi en az 20 karakter olmalıdır");

        var t = await db.PosTahsilatlari
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("POS tahsilatı bulunamadı");
        if (t.Durum == PosTahsilatDurumu.Iptal)
            throw new BadRequestException("Tahsilat zaten iptal edilmiş");

        var eslesme = await db.PosYatmaEslesmeleri
            .AnyAsync(e => e.TenantId == islem.TenantId && e.PosTahsilatId == id && !e.GeriAlindi);
        if (eslesme)
            throw new BadRequestException(
                "Banka yatmasıyla eşleşmiş tahsilat iptal edilemez; önce eşleşmeyi geri alın");

        t.Durum = PosTahsilatDurumu.Iptal;
        t.IptalGerekcesi = gerekce;
        denetim.YazmaKaydet(islem, nameof(PosTahsilat), t.Id,
            "POS tahsilatı iptal edildi (chargeback)", hassas: false);
        await db.SaveChangesAsync();
        return new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("POS tahsilatı iptal edildi"), durum = t.Durum };
    }

    // ── Faz B: yatma eşleştirme ──────────────────────────────────────────────

    public async Task<object> Eslestir(PosEslestirDto dto, AktifKullanici islem)
    {
        if (dto.Satirlar is null || dto.Satirlar.Count == 0)
            throw new BadRequestException("Eşleştirilecek en az bir tahsilat satırı gerekir");
        // ZO-ISLEM-A (doğrulayıcı 28.09): aynı tahsilat tek istekte iki satırla gelirse
        // döngüdeki "mevcut" toplamı kaydedilmemiş satırları görmez; aşımı yalnız DB'nin
        // tekil indeksi durdururdu (MySQL 1062 → HTTP 409 "Bu kayıt zaten var"). Kural serviste, anlaşılır 400 ile.
        // Kimlik karşılaştırması harf DUYARSIZ: MySQL harmanlaması "ABC" ile "abc"yi aynı
        // satır sayar; ön kontrol harf duyarlı olsaydı ikisi buradan geçip indekse (409) düşerdi.
        if (dto.Satirlar.GroupBy(s => s.PosTahsilatId, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
            throw new BadRequestException(
                "Aynı tahsilat bir eşleştirmede yalnız bir kez yer alabilir; tutarı tek satırda toplayın");
        // PAKET PATCH-5: ÖNCE yuvarla (eslesen_tutar decimal(15,2)), SONRA ≤ 0 ve aşım kapıları. Eskiden
        // 0,004 "sıfırdan büyük" kapısından geçip 0,00 eşleşme satırı yazıyordu; kapılar ham, satır yuvarlanmış
        // değerle çalışıyordu. Kolon tavanı da burada (toplam decimal taşması 500 olmasın).
        for (var i = 0; i < dto.Satirlar.Count; i++)
            dto.Satirlar[i].Tutar = KolonTavani.PozitifTutar(dto.Satirlar[i].Tutar, $"satirlar[{i}].tutar", "Eşleşen tutar");

        var hareket = await db.BankaHareketleri
            .FirstOrDefaultAsync(h => h.Id == dto.BankaHareketId && h.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Banka hareketi bulunamadı");
        await PosYatmasiKapisi(hareket, islem.TenantId);

        await using var tx = await db.Database.BeginTransactionAsync();

        // Bu harekete DAHA ÖNCE eşleşen tutar (geri alınmamışlar).
        var oncekiHareket = await db.PosYatmaEslesmeleri
            .Where(e => e.TenantId == islem.TenantId && e.BankaHareketId == hareket.Id && !e.GeriAlindi)
            .SumAsync(e => (decimal?)e.EslesenTutar) ?? 0m;

        var istenenToplam = dto.Satirlar.Sum(s => s.Tutar);
        if (istenenToplam <= 0m) throw new BadRequestException("Eşleşen tutar sıfırdan büyük olmalıdır");
        if (Yuvarla2(oncekiHareket + istenenToplam) > hareket.Tutar)
            throw new BadRequestException(
                $"Eşleşen toplam banka hareketi tutarını aşamaz (hareket {hareket.Tutar}, " +
                $"önceki eşleşme {oncekiHareket}, istenen {istenenToplam})");

        var now = DateTime.UtcNow;
        var etkilenen = new List<string>();
        foreach (var s in dto.Satirlar)
        {
            if (s.Tutar <= 0m) throw new BadRequestException("Eşleşen tutar sıfırdan büyük olmalıdır");

            var t = await db.PosTahsilatlari
                .FirstOrDefaultAsync(x => x.Id == s.PosTahsilatId && x.TenantId == islem.TenantId)
                ?? throw new NotFoundException("POS tahsilatı bulunamadı");
            if (t.Durum == PosTahsilatDurumu.Iptal)
                throw new BadRequestException("İptal edilmiş tahsilat eşleştirilemez");
            await CihazHesapKapisi(t, hareket, islem.TenantId);

            var mevcut = await db.PosYatmaEslesmeleri
                .Where(e => e.TenantId == islem.TenantId && e.PosTahsilatId == t.Id && !e.GeriAlindi)
                .SumAsync(e => (decimal?)e.EslesenTutar) ?? 0m;
            if (Yuvarla2(mevcut + s.Tutar) > t.NetTutar)
                throw new BadRequestException(
                    $"Eşleşen tutar tahsilatın net tutarını aşamaz (net {t.NetTutar}, yatan {mevcut})");

            // ZO-ISLEM-A (28.09, doğrulayıcı KRİTİK — elle eşleştirme çıkmazı): aynı çiftin
            // kaçıncı eşleşme olayı olduğu; geri alınmışlar da sayılır (iz silinmez).
            var ciftSira = await db.PosYatmaEslesmeleri
                .CountAsync(e => e.TenantId == islem.TenantId && e.PosTahsilatId == t.Id
                                 && e.BankaHareketId == hareket.Id) + 1;


            db.PosYatmaEslesmeleri.Add(new PosYatmaEslesme
            {
                Id = Kimlik.Ulid(),
                TenantId = islem.TenantId,
                PosTahsilatId = t.Id,
                BankaHareketId = hareket.Id,
                EslesenTutar = Yuvarla2(s.Tutar),
                Otomatik = false,
                EslestirenKullaniciId = islem.Id,
                GeriAlindi = false,
                CiftSira = ciftSira,
                Olusturuldu = now,
            });
            etkilenen.Add(t.Id);
        }

        await db.SaveChangesAsync();
        foreach (var id in etkilenen) await PosDefteri.Ozetle(db, islem.TenantId, id);

        if (hareket.Durum == "BEKLIYOR") hareket.Durum = "ESLESTI";
        denetim.YazmaKaydet(islem, nameof(PosYatmaEslesme), hareket.Id,
            $"POS yatması eşleştirildi · {etkilenen.Count} tahsilat · toplam {istenenToplam}", hassas: false);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return new
        {
            mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Eşleştirme tamamlandı"),
            bankaHareketId = hareket.Id,
            eslesenToplam = Yuvarla2(istenenToplam),
            satirSayisi = etkilenen.Count,
        };
    }

    // ── ZO-ISLEM-A 29.09 (tasarım hakemi KRİTİK) — POS YATMASI KAPISI ─────────
    // Olay: Garanti POS cihazının tahsilatı, ZİRAAT hesabındaki bir MÜŞTERİ havalesiyle
    // eşleştirilebildi; tahsilat "Yattı" oldu, Garanti'ye o para hiç yatmamıştı.
    // Kural (TEK KURAL — aday listesi, öneri ve eşleştirme aynı iki kapıdan geçer):
    //  1) banka hareketi POS yatması OLABİLECEK bir satır olmalı: GELEN, müşteriye/faturaya
    //     bağlanmamış, ters kayıt / ters kaydı yapılmış / virman bacağı DEĞİL;
    //  2) tahsilatın cihazının parasının yattığı hesap = hareketin hesabı (cihazın hesabı
    //     tanımlı değilse eşleştirme yapılmaz — "hangi hesaba yatar" bilinmeden kapanış yok).

    /// <summary>Hareket POS yatması olabilir mi? Olamazsa kullanıcıya sebebiyle 400.</summary>
    private async Task PosYatmasiKapisi(BankaHareket hareket, string tenantId)
    {
        if (hareket.Yon != "GELEN")
            throw new BadRequestException("Yalnızca GELEN banka hareketi POS tahsilatıyla eşleştirilebilir");
        if (!string.IsNullOrEmpty(hareket.CariId))
            throw new BadRequestException(
                "Bu banka hareketi bir müşteriyle eşleşmiş; POS yatması olarak kullanılamaz");
        if (!string.IsNullOrEmpty(hareket.FaturaId) || hareket.Durum is not ("BEKLIYOR" or "ESLESTI"))
            throw new BadRequestException(
                "Bu banka hareketi faturaya bağlanmış; POS yatması olarak kullanılamaz");
        if (!string.IsNullOrEmpty(hareket.KarsiBacakId))
            throw new BadRequestException(
                "Bu banka hareketi hesaplar arası aktarım; POS yatması olarak kullanılamaz");
        if (!string.IsNullOrEmpty(hareket.StornoKaynakId)
            || await db.BankaHareketleri.AnyAsync(h => h.TenantId == tenantId && h.StornoKaynakId == hareket.Id))
            throw new BadRequestException(
                "Bu banka hareketi ters kayıtla kapatılmış; POS yatması olarak kullanılamaz");
        var posHesabi = await db.PosCihazlari.AnyAsync(c => c.TenantId == tenantId
                                                            && c.BankaHesapId == hareket.BankaHesapId);
        if (!posHesabi)
            throw new BadRequestException(
                "Bu banka hesabına bağlı POS cihazı yok; POS yatması yalnız cihazın hesabına gelen parayla eşleşir");
    }

    /// <summary>Tahsilatın cihazının parası bu hareketin hesabına mı yatar?</summary>
    private async Task CihazHesapKapisi(PosTahsilat t, BankaHareket hareket, string tenantId)
    {
        var cihaz = await db.PosCihazlari.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == t.PosCihazId && c.TenantId == tenantId)
            ?? throw new NotFoundException("POS cihazı bulunamadı");
        if (string.IsNullOrEmpty(cihaz.BankaHesapId))
            throw new BadRequestException(
                $"{cihaz.Ad} cihazının parasının yattığı banka hesabı tanımlı değil; POS Cihazları ekranından hesabı seçin");
        if (!string.Equals(cihaz.BankaHesapId, hareket.BankaHesapId, StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException(
                $"{cihaz.Ad} cihazının parası başka bir banka hesabına yatar; seçilen banka hareketi bu tahsilatı kapatamaz");
        if (!string.Equals(t.ParaBirimi, hareket.ParaBirimi, StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("Tahsilat ile banka hareketinin para birimi aynı olmalıdır");
    }

    /// <summary>
    /// GET /pos/yatma-adaylari — eşleştirme seçicisinin kaynağı. YALNIZ POS cihazlarının
    /// hesaplarına GELEN, müşteriye/faturaya bağlanmamış, ters kayıt/virman olmayan ve
    /// eşleşmemiş KALANI olan hareketler. Her sonuç hesap adını ve kalan tutarı taşır.
    /// Sorgu boşsa en yeni adaylar listelenir (gözatma); sınır yanıtta yazılır (UX-S29).
    /// </summary>
    public async Task<Kuyumcu.Contracts.Arama.AramaYaniti> YatmaAdaylari(
        string? q, int limit, string? dil, string tenantId)
    {
        var sorgu = (q ?? "").Trim();
        if (sorgu.Length > 100) sorgu = sorgu[..100];
        var al = Math.Clamp(limit <= 0 ? 10 : limit, 1, 50);
        var metinler = Kuyumcu.Core.I18n.ZoIslemSozlugu.Al(dil == "en" ? "en" : "tr");

        var posHesaplari = await db.PosCihazlari.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.BankaHesapId != null)
            .Select(c => c.BankaHesapId!).Distinct().Take(AzamiSatir).ToListAsync();

        var onekler = sorgu.Length > 0 ? Arama.AramaServisi.TutarOnekleri(sorgu) : [];
        var d1 = onekler.Count > 0 ? onekler[0] + "%" : null;
        var d2 = onekler.Count > 1 ? onekler[1] + "%" : null;

        var q0 = db.BankaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == tenantId && posHesaplari.Contains(h.BankaHesapId)
                        && h.Yon == "GELEN" && h.CariId == null && h.FaturaId == null
                        && (h.Durum == "BEKLIYOR" || h.Durum == "ESLESTI")
                        && h.KarsiBacakId == null && h.StornoKaynakId == null
                        && !db.BankaHareketleri.Any(s => s.TenantId == tenantId && s.StornoKaynakId == h.Id)
                        && h.Tutar > (db.PosYatmaEslesmeleri
                            .Where(e => e.TenantId == tenantId && e.BankaHareketId == h.Id && !e.GeriAlindi)
                            .Sum(e => (decimal?)e.EslesenTutar) ?? 0m));
        if (sorgu.Length > 0)
        {
            var hesapEslesen = await db.BankaHesaplari.AsNoTracking()
                .Where(b => b.TenantId == tenantId && posHesaplari.Contains(b.Id)
                            && (b.BankaAdi.Contains(sorgu) || b.HesapAdi.Contains(sorgu)))
                .Select(b => b.Id).Take(AzamiSatir).ToListAsync();
            q0 = q0.Where(h => hesapEslesen.Contains(h.BankaHesapId)
                               || (h.KarsiTarafAd != null && h.KarsiTarafAd.Contains(sorgu))
                               || (h.BankaReferans != null && h.BankaReferans.Contains(sorgu))
                               || (h.Aciklama != null && h.Aciklama.Contains(sorgu))
                               || (d1 != null && EF.Functions.Like(h.Tutar.ToString(), d1))
                               || (d2 != null && EF.Functions.Like(h.Tutar.ToString(), d2)));
        }

        var satirlar = await q0.OrderByDescending(h => h.Tarih).ThenBy(h => h.Id)
            .Take(al + 1)
            .Select(h => new
            {
                h.Id, h.BankaHesapId, h.Tutar, h.ParaBirimi, h.Tarih, h.KarsiTarafAd,
                Eslesen = db.PosYatmaEslesmeleri
                    .Where(e => e.TenantId == tenantId && e.BankaHareketId == h.Id && !e.GeriAlindi)
                    .Sum(e => (decimal?)e.EslesenTutar) ?? 0m,
            })
            .ToListAsync();
        var dahaVar = satirlar.Count > al;
        if (dahaVar) satirlar = satirlar.Take(al).ToList();

        var hesapIdleri = satirlar.Select(s => s.BankaHesapId).Distinct().ToList();
        var hesaplar = await db.BankaHesaplari.AsNoTracking()
            .Where(b => b.TenantId == tenantId && hesapIdleri.Contains(b.Id))
            .Take(al)
            .ToDictionaryAsync(b => b.Id, b => $"{b.BankaAdi} · {b.HesapAdi}");

        // 29.09 (tasarım hakemi YÜKSEK): kullanıcı "bankaya yatan TUTARI" seçer → sonucun
        // adı (seçicide büyük, kalın) TUTARDIR. Hesap · tarih · karşı taraf ikincil satırdadır;
        // kısmen eşleşmiş harekette ikincil satır "kalan …" ile biter. Yanıt şekli DEĞİŞMEDİ.
        var sonuclar = satirlar.Select(s =>
        {
            var kalan = Yuvarla2(s.Tutar - s.Eslesen);
            var parcalar = new List<string>
            {
                hesaplar.TryGetValue(s.BankaHesapId, out var hesapAd) ? hesapAd : "—",
                s.Tarih.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture),
            };
            if (!string.IsNullOrWhiteSpace(s.KarsiTarafAd)) parcalar.Add(s.KarsiTarafAd!);
            if (kalan < s.Tutar)
                parcalar.Add(string.Format(metinler.PosAdayKalan, Arama.AramaServisi.EkranPara(kalan, s.ParaBirimi)));
            return new Kuyumcu.Contracts.Arama.AramaSonucu(
                s.Id, "+" + Arama.AramaServisi.EkranPara(s.Tutar, s.ParaBirimi), string.Join(" · ", parcalar),
                Kuyumcu.Contracts.Arama.AramaTurleri.BankaHareket, null);
        }).ToList();

        return new Kuyumcu.Contracts.Arama.AramaYaniti(
            sorgu, Kuyumcu.Contracts.Arama.AramaTurleri.BankaHareket, al, 0, sonuclar.Count, dahaVar,
            50, sonuclar, Gozatma: sorgu.Length == 0);
    }

    /// <summary>
    /// Oto eşleştirme ÖNERİSİ — HİÇBİR ŞEY YAZMAZ. Aynı cihazın aynı valör
    /// gününe düşen açık tahsilatlarından toplamı hareket tutarına EŞİTLENEN
    /// kümeyi önerir. ⚠ TEK ADAY YOKSA BOŞ DÖNER: "yakın olanı seçelim"
    /// varsayımı yapılmaz (Anayasa md.1).
    /// </summary>
    public async Task<object> OtoEslestirOnerisi(PosOtoOneriDto dto, AktifKullanici islem)
    {
        var hareket = await db.BankaHareketleri.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == dto.BankaHareketId && h.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Banka hareketi bulunamadı");
        if (hareket.Yon != "GELEN")
            throw new BadRequestException("Yalnızca GELEN banka hareketi için öneri üretilir");
        // ZO-ISLEM-A 29.09 (tasarım hakemi KRİTİK): öneri de eşleştirmeyle AYNI kapıdan geçer.
        await PosYatmasiKapisi(hareket, islem.TenantId);
        // Yalnız parası BU hesaba yatan cihazların tahsilatları aday olur (tek kural: CihazHesapKapisi).
        var uygunCihazlar = await db.PosCihazlari.AsNoTracking()
            .Where(c => c.TenantId == islem.TenantId && c.BankaHesapId == hareket.BankaHesapId)
            .Select(c => c.Id).Take(AzamiSatir).ToListAsync();

        var tolerans = ayarlar.OtoEslestirToleransKurus / 100m;

        // ZO-ISLEM-A 28.09 (tasarım hakemi KRİTİK — elle eşleştirme yolu): hareketin
        // HENÜZ eşleşmemiş tutarı yanıtta döner; ekran elle seçimde toplamı buna göre
        // kilitler (sunucu Eslestir kapısı aynı kuralla yine reddeder — tek kural).
        var oncekiEslesen = await db.PosYatmaEslesmeleri.AsNoTracking()
            .Where(e => e.TenantId == islem.TenantId && e.BankaHareketId == hareket.Id && !e.GeriAlindi)
            .SumAsync(e => (decimal?)e.EslesenTutar) ?? 0m;
        var hareketKalan = Yuvarla2(Math.Max(0m, hareket.Tutar - oncekiEslesen));

        var acik = await db.PosTahsilatlari.AsNoTracking()
            .Where(t => t.TenantId == islem.TenantId
                        && (t.Durum == PosTahsilatDurumu.Bekliyor || t.Durum == PosTahsilatDurumu.Kismi)
                        && t.NetTutar > t.YatanTutar
                        && t.ParaBirimi == hareket.ParaBirimi
                        && uygunCihazlar.Contains(t.PosCihazId))
            .OrderBy(t => t.ValorTarihi)
            .Take(AzamiSatir)
            .ToListAsync();

        foreach (var kume in acik.GroupBy(t => new { t.PosCihazId, t.ValorTarihi }))
        {
            var toplam = Yuvarla2(kume.Sum(t => t.NetTutar - t.YatanTutar));
            // ZO-ISLEM-A (28.09): kısmen eşleşmiş harekette küme HENÜZ eşleşmemiş tutara
            // (hareketKalan) eşitlenir; hareketin tam tutarına değil.
            if (hareketKalan > 0m && Math.Abs(toplam - hareketKalan) <= tolerans)
            {
                return new
                {
                    bulundu = true,
                    bankaHareketId = hareket.Id,
                    toplam,
                    hareketKalan,
                    uygunCihazlar,
                    satirlar = kume.Select(t => new
                    {
                        posTahsilatId = t.Id,
                        tutar = Yuvarla2(t.NetTutar - t.YatanTutar),
                        valorTarihi = t.ValorTarihi,
                        posCihazId = t.PosCihazId,
                    }).ToList(),
                };
            }
        }

        // ZO-ISLEM-A 29.09 (tasarım hakemi YÜKSEK): gün kümesi tutmuyorsa kalanı hareketin
        // eşleşmemiş tutarına BİREBİR eşit TEK tahsilat aranır (26.950 ₺ yatış ↔ 26.950 ₺ net
        // tahsilat, aynı gün başka tahsilatlar olsa da). Eskiden yalnız günün TAMAMI denendiği
        // için birebir tutan satır önerilmiyor, ekran "tutmuyor" diyordu. ⚠ Birden çok eşit aday
        // varsa hangisinin yattığı bilinemez → öneri YOK (varsayım yapılmaz, Anayasa md.1).
        if (hareketKalan > 0m)
        {
            var birebir = acik
                .Where(t => Math.Abs(Yuvarla2(t.NetTutar - t.YatanTutar) - hareketKalan) <= tolerans)
                .Take(2).ToList();
            if (birebir.Count == 1)
            {
                var t = birebir[0];
                var kalan = Yuvarla2(t.NetTutar - t.YatanTutar);
                return new
                {
                    bulundu = true,
                    bankaHareketId = hareket.Id,
                    toplam = kalan,
                    hareketKalan,
                    uygunCihazlar,
                    satirlar = new[]
                    {
                        new { posTahsilatId = t.Id, tutar = kalan, valorTarihi = t.ValorTarihi, posCihazId = t.PosCihazId },
                    }.ToList(),
                };
            }
        }

        return new
        {
            bulundu = false,
            bankaHareketId = hareket.Id,
            toplam = 0m,
            hareketKalan,
            uygunCihazlar,
            satirlar = new List<object>(),
            mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Tutarı bu harekete eşitlenen tek bir tahsilat kümesi bulunamadı; elle eşleştirin."),
        };
    }

    /// <summary>
    /// Eşleşmeyi geri alır. ⚠ SATIR SİLİNMEZ (ADR-0004 append-only):
    /// geri_alindi=true + gerekçe yazılır, özet yeniden hesaplanır.
    /// </summary>
    public async Task<object> EslesmeGeriAl(string id, PosEslesmeGeriAlDto dto, AktifKullanici islem)
    {
        var gerekce = (dto.Gerekce ?? "").Trim();
        if (gerekce.Length < 20)
            throw new BadRequestException("Geri alma gerekçesi en az 20 karakter olmalıdır");

        var e = await db.PosYatmaEslesmeleri
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Eşleşme bulunamadı");
        if (e.GeriAlindi) throw new BadRequestException("Eşleşme zaten geri alınmış");

        e.GeriAlindi = true;
        e.GeriAlmaGerekcesi = gerekce;
        await db.SaveChangesAsync();
        await PosDefteri.Ozetle(db, islem.TenantId, e.PosTahsilatId);

        // Bankaya eşleşen başka satır kalmadıysa hareket BEKLIYOR'a döner.
        var kalan = await db.PosYatmaEslesmeleri
            .AnyAsync(x => x.TenantId == islem.TenantId && x.BankaHareketId == e.BankaHareketId && !x.GeriAlindi);
        if (!kalan)
        {
            var h = await db.BankaHareketleri
                .FirstOrDefaultAsync(x => x.Id == e.BankaHareketId && x.TenantId == islem.TenantId);
            if (h is not null && h.Durum == "ESLESTI") h.Durum = "BEKLIYOR";
        }

        denetim.YazmaKaydet(islem, nameof(PosYatmaEslesme), e.Id,
            "POS yatma eşleşmesi geri alındı", hassas: false);
        await db.SaveChangesAsync();
        return new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Eşleşme geri alındı"), geriAlindi = true, posTahsilatId = e.PosTahsilatId };
    }

    public async Task<object> EslesmeListele(string tahsilatId, AktifKullanici islem)
    {
        var satirlar = await db.PosYatmaEslesmeleri.AsNoTracking()
            .Where(e => e.TenantId == islem.TenantId && e.PosTahsilatId == tahsilatId)
            .OrderByDescending(e => e.Olusturuldu)
            .Select(e => new
            {
                e.Id, e.BankaHareketId, e.EslesenTutar, e.Otomatik,
                e.GeriAlindi, e.GeriAlmaGerekcesi, e.Olusturuldu,
            })
            .ToListAsync();
        return new { satirlar };
    }

    // ── Kart bacağının cihaz çözümü (satış yolundan çağrılır) ────────────────

    /// <summary>
    /// Kart bacağı için cihazı çözer. FAIL-CLOSED:
    ///   • gövdede cihaz varsa kiracı + şube + kasa doğrulanır (yabancı cihaz 404)
    ///   • yoksa o ŞUBEDE tam bir aktif cihaz varsa o seçilir
    ///   • birden çok varsa BadRequest ("POS cihazı belirtilmeli") — ilk cihaz SEÇİLMEZ
    ///   • hiç yoksa CihazZorunlu=true ise BadRequest, false ise null (POS satırı yazılmaz)
    /// </summary>
    public static async Task<PosCihaz?> CihazCoz(
        KuyumcuDbContext db, PosAyarlari ayarlar, string tenantId, string subeId,
        string? istenenCihazId, string kasaId)
    {
        if (!string.IsNullOrEmpty(istenenCihazId))
        {
            var c = await db.PosCihazlari
                .FirstOrDefaultAsync(x => x.Id == istenenCihazId && x.TenantId == tenantId)
                ?? throw new NotFoundException("POS cihazı bulunamadı");
            if (c.SubeId != subeId) throw new NotFoundException("POS cihazı bulunamadı");
            if (!c.Aktif) throw new BadRequestException("Pasif POS cihazıyla tahsilat yapılamaz");
            if (c.KasaId != kasaId)
                throw new BadRequestException("Kart bacağının kasası POS cihazının kasasıyla aynı olmalıdır");
            return c;
        }

        var adaylar = await db.PosCihazlari
            .Where(x => x.TenantId == tenantId && x.SubeId == subeId && x.Aktif)
            .Take(3).ToListAsync();

        if (adaylar.Count == 1)
        {
            var tek = adaylar[0];
            if (tek.KasaId != kasaId)
            {
                if (ayarlar.CihazZorunlu)
                    throw new BadRequestException("Kart bacağının kasası POS cihazının kasasıyla aynı olmalıdır");
                return null;
            }
            return tek;
        }
        if (adaylar.Count > 1)
            throw new BadRequestException("POS cihazı belirtilmeli: bu şubede birden çok aktif cihaz var");

        if (ayarlar.CihazZorunlu)
            throw new BadRequestException("POS cihazı tanımlı değil: kart tahsilatı için önce cihaz tanımlayın");
        return null;
    }

    /// <summary>Taksitli satışta geçerli oran/valör — yoksa cihazın peşin değerleri kullanılır.</summary>
    public static async Task<(decimal? Oran, int? ValorGun)> TaksitOraniCoz(
        KuyumcuDbContext db, string tenantId, string cihazId, int taksit)
    {
        if (taksit < 2) return (null, null);
        var o = await db.PosTaksitOranlari.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PosCihazId == cihazId
                        && x.TaksitAdedi == taksit && x.Aktif)
            .OrderByDescending(x => x.GecerliBaslangic)
            .FirstOrDefaultAsync();
        return o is null ? (null, null) : (o.KomisyonOran, o.ValorGun);
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private async Task<PosCihaz> CihazBul(string id, string tenantId) =>
        await db.PosCihazlari.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId)
        ?? throw new NotFoundException("POS cihazı bulunamadı");

    private void ValorDogrula(int gun)
    {
        if (gun < 0 || gun > ayarlar.AzamiValorGun)
            throw new BadRequestException($"Valör günü 0 ile {ayarlar.AzamiValorGun} arasında olmalıdır");
    }

    /// <summary>Komisyon oranı sütunlarının ondalık ölçeği: decimal(5,4) (Entities.cs).</summary>
    internal const int OranOlcegi = 4;

    /// <summary>decimal(5,4) tuzağı: %100'ü aşan oran SESSİZCE KIRPILMAZ, reddedilir.</summary>
    private static void OranDogrula(decimal oran)
    {
        if (oran < 0m || oran > 1m)
            throw new BadRequestException("Komisyon oranı 0 ile 1 arasında olmalıdır (örn. %1,85 → 0,0185)");
        // KESIF-P6 (30.09 18:10, KESIF-P1'den devir): sütun decimal(5,4). 0,01855 gibi 4 haneden
        // ince oran veritabanında SESSİZCE yuvarlanıyordu (istemcinin 2 hane yüzde kapısı vardı,
        // API'yi doğrudan çağıran yoktu). Fazla hane REDDEDİLİR, yuvarlanmaz (Anayasa md.1).
        if (decimal.Round(oran, OranOlcegi) != oran)
            throw new BadRequestException("Komisyon oranı en çok 4 ondalık haneli olabilir (örn. %1,85 → 0,0185)");
    }

    private static DateOnly? GunCoz(string? deger) =>
        DateOnly.TryParse(deger, System.Globalization.CultureInfo.InvariantCulture, out var g) ? g : null;

    /// <summary>PAKET GOC-A — POS tahsilat durumları (TEK kaynak: PosTahsilatDurumu).</summary>
    public static readonly string[] TahsilatDurumlari =
        [PosTahsilatDurumu.Bekliyor, PosTahsilatDurumu.Kismi, PosTahsilatDurumu.Yatti, PosTahsilatDurumu.Iptal];

    /// <summary>PAKET GOC-A — sorgu günü: boş → null; okunamazsa 400 (sessizce süzgeçsiz sonuç YOK).</summary>
    private static DateOnly? SorguGunu(string? deger, string alan)
    {
        if (string.IsNullOrWhiteSpace(deger)) return null;
        return GunCoz(deger) ?? throw new BadRequestException("Tarih okunamadı — YYYY-AA-GG biçiminde gönderin").AlanIle(alan);
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/pos")]
// P-09: POS artık KENDİ yetki anahtarını taşır ("kasa" değil) — varsayılanı
// kasa ile aynı rollere açıktır, davranış değişmez.
[Yetki.ModulYetkisi("pos")]
public sealed class PosController(PosServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/pos/cihazlar — POS cihazı tanımla — MUDUR+</summary>
    [HttpPost("cihazlar")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> CihazOlustur([FromBody] PosCihazOlusturDto dto) => servis.CihazOlustur(dto, Islem);

    /// <summary>GET /api/v1/pos/cihazlar?subeId=&amp;aktif=</summary>
    [HttpGet("cihazlar")]
    public Task<List<PosCihazSatir>> CihazListele([FromQuery] string? subeId, [FromQuery] bool? aktif)
        => servis.CihazListele(Islem, subeId, aktif);

    /// <summary>GET /api/v1/pos/cihazlar/{id} — cihaz + taksit oran tablosu</summary>
    [HttpGet("cihazlar/{id}")]
    public Task<object> CihazGetir(string id) => servis.CihazGetir(id, Islem);

    /// <summary>PATCH /api/v1/pos/cihazlar/{id} — KISMİ güncelleme — MUDUR+</summary>
    [HttpPatch("cihazlar/{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> CihazGuncelle(string id, [FromBody] PosCihazGuncelleDto dto)
        => servis.CihazGuncelle(id, dto, Islem);

    /// <summary>POST /api/v1/pos/cihazlar/{id}/pasife-al — MUDUR+ (hard DELETE YOKTUR)</summary>
    /// <summary>
    /// POST /api/v1/pos/cihazlar/{id}/kasa-goc — PAKET P-22 / B-55.
    /// Tahsilat VARKEN de bağlı kasayı taşır; gerekçe zorunlu, tutanak kalıcı.
    /// </summary>
    [HttpPost("cihazlar/{id}/kasa-goc")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> KasaGoc(string id, [FromBody] PosKasaGocDto dto) => servis.KasaGoc(id, dto, Islem);

    /// <summary>GET /api/v1/pos/cihazlar/{id}/kasa-gocleri — göç tutanakları (B-55)</summary>
    [HttpGet("cihazlar/{id}/kasa-gocleri")]
    public Task<List<PosKasaGocSatir>> KasaGocleri(string id)
        => servis.KasaGocleri(id, Islem.TenantId);

    [HttpPost("cihazlar/{id}/pasife-al")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> CihazPasifeAl(string id) => servis.CihazPasifeAl(id, Islem);

    /// <summary>POST /api/v1/pos/cihazlar/{id}/taksit-oranlari — MUDUR+</summary>
    [HttpPost("cihazlar/{id}/taksit-oranlari")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> TaksitOranEkle(string id, [FromBody] PosTaksitOranOlusturDto dto)
        => servis.TaksitOranEkle(id, dto, Islem);

    /// <summary>PATCH /api/v1/pos/cihazlar/{id}/taksit-oranlari/{oranId} — MUDUR+</summary>
    [HttpPatch("cihazlar/{id}/taksit-oranlari/{oranId}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> TaksitOranGuncelle(string id, string oranId, [FromBody] PosTaksitOranGuncelleDto dto)
        => servis.TaksitOranGuncelle(id, oranId, dto, Islem);

    /// <summary>GET /api/v1/pos/tahsilatlar — POS alacak defteri (sayfalı)</summary>
    [HttpGet("tahsilatlar")]
    public Task<object> TahsilatListele(
        [FromQuery] string? durum, [FromQuery] string? bas, [FromQuery] string? bit,
        [FromQuery] string? cihazId, [FromQuery] int limit = 50, [FromQuery] int offset = 0,
        [FromQuery] string? subeId = null)
        => servis.TahsilatListele(Islem, durum, bas, bit, cihazId, limit, offset, subeId);

    /// <summary>GET /api/v1/pos/valor-takvimi — sunucuda toplanmış (satır listesi DÖNMEZ)</summary>
    [HttpGet("valor-takvimi")]
    public Task<List<PosValorSatir>> ValorTakvimi(
        [FromQuery] string? bas, [FromQuery] string? bit, [FromQuery] string? subeId)
        => servis.ValorTakvimi(Islem, bas, bit, subeId);

    /// <summary>GET /api/v1/pos/tahsilatlar/{id}/eslesmeler</summary>
    [HttpGet("tahsilatlar/{id}/eslesmeler")]
    public Task<object> EslesmeListele(string id) => servis.EslesmeListele(id, Islem);

    /// <summary>POST /api/v1/pos/tahsilatlar/{id}/iptal — chargeback — MUDUR+</summary>
    [HttpPost("tahsilatlar/{id}/iptal")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> TahsilatIptal(string id, [FromBody] PosTahsilatIptalDto dto)
        => servis.TahsilatIptal(id, dto, Islem);

    /// <summary>POST /api/v1/pos/eslestir — Faz B — MUHASEBE+</summary>
    [HttpPost("eslestir")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Eslestir([FromBody] PosEslestirDto dto) => servis.Eslestir(dto, Islem);

    /// <summary>
    /// GET /api/v1/pos/yatma-adaylari — ZO-ISLEM-A 29.09: eşleştirme seçicisinin kaynağı;
    /// yalnız POS cihazlarının hesaplarına gelen, kalanı olan hareketler — MUHASEBE+
    /// </summary>
    [HttpGet("yatma-adaylari")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<Kuyumcu.Contracts.Arama.AramaYaniti> YatmaAdaylari(
        [FromQuery] string? q, [FromQuery] int limit = 10, [FromQuery] string? dil = null)
        => servis.YatmaAdaylari(q, limit, dil, Islem.TenantId);

    /// <summary>POST /api/v1/pos/oto-eslestir-onerisi — HİÇBİR ŞEY YAZMAZ — MUHASEBE+</summary>
    [HttpPost("oto-eslestir-onerisi")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> OtoEslestirOnerisi([FromBody] PosOtoOneriDto dto)
        => servis.OtoEslestirOnerisi(dto, Islem);

    /// <summary>POST /api/v1/pos/eslesmeler/{id}/geri-al — SİLMEZ — MUDUR+</summary>
    [HttpPost("eslesmeler/{id}/geri-al")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> EslesmeGeriAl(string id, [FromBody] PosEslesmeGeriAlDto dto)
        => servis.EslesmeGeriAl(id, dto, Islem);
}
