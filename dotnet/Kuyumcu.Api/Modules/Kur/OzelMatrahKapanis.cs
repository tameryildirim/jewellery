using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain.Girdi;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Kur;

// ═════════════════════════════════════════════════════════════════════════════
// KUR KAYNAKLARI (kurucu 17.10, § 9 md.3) — HAS BEDELİ KAYNAĞI VE GÜN KAPANIŞLARI
//
// Kural (KDV GUT § III/A-4.2.1; araştırma § 5): has bedeli = has gram × Borsa İstanbul'da
// işlem yapılan SON RESMÎ İŞGÜNÜ külçe altın kapanışı. Kiracı kaynağı seçer:
//   BIST (varsayılan) · HAREM · ALTINKAYNAK · ELLE  → seçili kaynağın son işgünü kapanışı
//   ISLEM_KURU                                       → satıştaki kur kaydı (bugünkü davranış AYNEN)
// "Son işgünü": fatura tarihinin işletme gününden ÖNCEKİ en yakın kapanış (gün < fatura günü),
// en çok KapanisGeriGun (10) gün geriye. Bulunamazsa FAIL-CLOSED: fatura kesilmez, sebep +
// "kapanışı girin" yolu söylenir. Faturaya kaynak KODU + kapanış günü + fiyat yazılır.
//
// Mevcut "binde" yöntemi (M2b) KALIR — fiyat kullanmaz; o kalemde kapanış aranmaz.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Bir faturanın has bedeli fiyat kaynağı (tek belge için TEK çözüm — kalemler karışmaz).
/// <see cref="Fiyat"/> ilk kez çağrıldığında <see cref="Kullanildi"/> olur; kapanış yoksa o an
/// 400 fırlatır (has bacağı olmayan ya da binde yöntemli faturada kapanış ARANMAZ).
/// </summary>
public sealed class OzelMatrahFiyatCozumu
{
    public string Kod { get; }
    public DateOnly FaturaGunu { get; }
    public DateOnly? KapanisGunu { get; }
    public decimal? KapanisFiyati { get; }
    public bool Kullanildi { get; private set; }

    public OzelMatrahFiyatCozumu(string kod, DateOnly faturaGunu, DateOnly? kapanisGunu, decimal? kapanisFiyati)
    {
        Kod = kod; FaturaGunu = faturaGunu; KapanisGunu = kapanisGunu; KapanisFiyati = kapanisFiyati;
    }

    public bool IslemKuru => Kod == KurKaynakKodlari.IslemKuru;

    /// <summary>
    /// Bacağın has birim fiyatı. ISLEM_KURU: <paramref name="islemKuru"/> (yoksa
    /// <paramref name="kurYokIletisi"/> ile 400 — bugünkü kapı). Diğerleri: son işgünü kapanışı.
    /// </summary>
    public decimal Fiyat(decimal? islemKuru, string kurYokIletisi)
    {
        Kullanildi = true;
        if (IslemKuru)
            return islemKuru is > 0m ? islemKuru.Value : throw new BadRequestException(kurYokIletisi);
        return KapanisFiyati ?? throw new BadRequestException(
                $"Has bedeli için {Kod} kapanışı bulunamadı (fatura günü {FaturaGunu:dd.MM.yyyy}, son {KurKaynakKodlari.KapanisGeriGun} gün). İşletme → Ayarlar'dan son işgününün kapanışını girin.")
            .AlanIle("ozelMatrahKaynagi");
    }

    /// <summary>Faturaya yazılacak kod: fiyat kullanıldıysa kaynak; yalnız binde kalemi varsa BINDE; yoksa null.</summary>
    public string? BelgeKodu(bool bindeKullanildi) =>
        Kullanildi ? Kod : bindeKullanildi ? KurKaynakKodlari.Binde : null;

    public DateOnly? BelgeGunu => Kullanildi && !IslemKuru ? KapanisGunu : null;
    public decimal? BelgeFiyati => Kullanildi && !IslemKuru ? KapanisFiyati : null;
}

public static class OzelMatrahKaynakCozucu
{
    /// <summary>
    /// Kiracının has bedeli kaynağı. BOŞ sütun = bu paketten önce açılmış kiracı → ISLEM_KURU (bugünkü
    /// davranış; faturalama geçişle kesilmez). Yeni işletme kayıtta BIST ile doğar (AuthServisi, D1 kalıbı).
    /// </summary>
    public static async Task<string> KiraciKaynagi(KuyumcuDbContext db, string tenantId)
    {
        var kod = await db.Tenants.AsNoTracking().Where(t => t.Id == tenantId)
            .Select(t => t.OzelMatrahKaynagi).FirstOrDefaultAsync();
        return Coz(kod);
    }

    /// <summary>Sütun değeri → geçerli kod (boş/bilinmeyen → ISLEM_KURU, eski kiracı davranışı).</summary>
    public static string Coz(string? kod) =>
        KurKaynakKodlari.OzelMatrahTumu.Contains(kod ?? "") ? kod! : KurKaynakKodlari.IslemKuru;

    /// <summary>İşlem kuru çözümü (bugünkü davranış) — binde önizlemesinde bilgi amaçlı has bedeli için.</summary>
    public static OzelMatrahFiyatCozumu IslemKuruCozumu(DateTime tarih) =>
        new(KurKaynakKodlari.IslemKuru, FaturaGunu(tarih), null, null);

    /// <summary>Fatura tarihinin işletme günü (Unspecified → UTC kabul; BaslikGirdisiDenetle ile aynı).</summary>
    public static DateOnly FaturaGunu(DateTime tarih) =>
        Zaman.YerelBugun(tarih.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(tarih, DateTimeKind.Utc) : tarih.ToUniversalTime());

    /// <summary>
    /// Faturanın has bedeli fiyat çözümü. <paramref name="zorunluKod"/>/<paramref name="zorunluGun"/>:
    /// taslağa kalem eklenirken belgenin İLK kaynağı ve günü korunur (bir belgede iki kapanış olmaz).
    /// </summary>
    public static async Task<OzelMatrahFiyatCozumu> Coz(
        KuyumcuDbContext db, string tenantId, DateTime faturaTarihi,
        string? zorunluKod = null, DateOnly? zorunluGun = null)
    {
        var kod = zorunluKod is not null && KurKaynakKodlari.OzelMatrahTumu.Contains(zorunluKod)
            ? zorunluKod : await KiraciKaynagi(db, tenantId);
        var gun = FaturaGunu(faturaTarihi);
        if (kod == KurKaynakKodlari.IslemKuru) return new OzelMatrahFiyatCozumu(kod, gun, null, null);

        var q = db.OzelMatrahKapanislari.AsNoTracking().Where(k => k.TenantId == tenantId && k.Kaynak == kod);
        q = zorunluGun is { } zg
            ? q.Where(k => k.Gun == zg)
            : q.Where(k => k.Gun < gun && k.Gun >= gun.AddDays(-KurKaynakKodlari.KapanisGeriGun));
        var kapanis = await q.OrderByDescending(k => k.Gun).Select(k => new { k.Gun, k.Fiyat }).FirstOrDefaultAsync();
        return new OzelMatrahFiyatCozumu(kod, gun, kapanis?.Gun, kapanis?.Fiyat);
    }
}

// ── DTO ──────────────────────────────────────────────────────────────────────

public sealed class KapanisGirDto
{
    /// <summary>BIST (bültenden okunan) · ELLE (kuyumcunun kendi değeri).</summary>
    [Required, RegularExpression("BIST|ELLE", ErrorMessage = "Geçersiz kaynak")]
    public string Kaynak { get; set; } = null!;

    /// <summary>Kapanışın işletme günü (yyyy-MM-dd).</summary>
    [Required(ErrorMessage = "Gün zorunludur")]
    public DateOnly? Gun { get; set; }

    /// <summary>Girilen değer: TL/g ya da TL/kg (birime göre).</summary>
    [Required(ErrorMessage = "Kapanış değeri zorunludur")]
    [Range(0.0001, 999_999_999_999.0, ErrorMessage = "Kapanış değeri sıfırdan büyük olmalıdır")]
    public decimal? Deger { get; set; }

    /// <summary>TL_GRAM · TL_KG (BIST bülteni TL/kg yayımlar; 1000/1000 saflık).</summary>
    [Required, RegularExpression("TL_GRAM|TL_KG", ErrorMessage = "Geçersiz birim")]
    public string Birim { get; set; } = "TL_GRAM";

    [StringLength(AlanSinirlari.KapanisDayanakEnCok)]
    public string? Dayanak { get; set; }
}

/// <summary>Kapanış satırı yanıtı.</summary>
public sealed record KapanisCevap(
    string Id, string Kaynak, DateOnly Gun, decimal Fiyat, decimal GirilenDeger, string GirilenBirim,
    string Dayanak, bool Otomatik, DateTime Guncellendi);

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class OzelMatrahKapanisServisi(
    KuyumcuDbContext db, DenetimServisi denetim, ILogger<OzelMatrahKapanisServisi> logger)
{
    public const string BirimGram = "TL_GRAM";
    public const string BirimKg = "TL_KG";

    /// <summary>TL/kg → TL/has gram (1000/1000 saflık; BIST veri sayfası: "altın için 1000/1000 saflık").</summary>
    public static decimal GramaCevir(decimal deger, string birim) =>
        Math.Round(birim == BirimKg ? deger / 1000m : deger, 4, MidpointRounding.AwayFromZero);

    /// <summary>Son kapanışlar (yeniden eskiye).</summary>
    public async Task<List<KapanisCevap>> Listele(string tenantId, string? kaynak, int limit)
    {
        var q = db.OzelMatrahKapanislari.AsNoTracking().Where(k => k.TenantId == tenantId);
        if (!string.IsNullOrEmpty(kaynak)) q = q.Where(k => k.Kaynak == kaynak);
        var liste = await q.OrderByDescending(k => k.Gun).ThenBy(k => k.Kaynak)
            .Take(Math.Clamp(limit, 1, 200)).ToListAsync();
        return liste.Select(Cevap).ToList();
    }

    /// <summary>
    /// Kuyumcunun girdiği kapanış (BIST bülteni ya da ELLE). Aynı (kaynak, gün) varsa DÜZELTİLİR —
    /// ancak bir faturada kullanılmışsa 409 (belgenin dayanağı değişmez).
    /// </summary>
    public async Task<KapanisCevap> Gir(KapanisGirDto dto, AktifKullanici islem)
    {
        if (!KurKaynakKodlari.ElleKapanisli.Contains(dto.Kaynak))
            throw new BadRequestException("Bu kaynağın kapanışı elle girilmez").AlanIle("kaynak");
        if (dto.Birim is not (BirimGram or BirimKg))
            throw new BadRequestException("Geçersiz birim").AlanIle("birim");
        var gun = dto.Gun ?? throw new BadRequestException("Gün zorunludur").AlanIle("gun");
        GirdiKapisi.IslemTarihi(gun, "gun");   // ileri gün yok, en çok 1 yıl geri
        if (dto.Kaynak == KurKaynakKodlari.Bist && gun.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            throw new BadRequestException("Borsa İstanbul hafta sonu kapalıdır; son işgününün kapanışını girin").AlanIle("gun");
        var deger = dto.Deger is > 0m ? dto.Deger.Value
            : throw new BadRequestException("Kapanış değeri sıfırdan büyük olmalıdır").AlanIle("deger");
        if (dto.Birim == BirimGram) GirdiKapisi.Kur(deger, "deger", KurServisi.KurKolonOndalik);
        var fiyat = GramaCevir(deger, dto.Birim);

        // Yazım hatası kapısı (Kur ekranıyla AYNI kural): mutlak sınır + son geçerli has kuruna göre ± %20.
        // Kapanış ONAYLA geçilmez — TL/kg yerine TL/g yazmak gibi hatalar burada durur.
        var sonHas = await db.KurSnapshots.AsNoTracking()
            .Where(s => s.TenantId == islem.TenantId && !s.IptalEdildi)
            .OrderByDescending(s => s.Olusturuldu).Select(s => (decimal?)s.AltinHasFiyati).FirstOrDefaultAsync();
        GirdiKapisi.Denetle(FiyatBandi.Denetle(fiyat, FiyatTuru.Has, sonHas), "deger");

        var dayanak = string.IsNullOrWhiteSpace(dto.Dayanak)
            ? (dto.Kaynak == KurKaynakKodlari.Bist ? "BIST günlük bülten" : "Elle girildi")
            : dto.Dayanak.Trim();
        dayanak += string.Create(CultureInfo.InvariantCulture,
            $" · {deger} {(dto.Birim == BirimKg ? "TL/kg ÷ 1000" : "TL/g")}");
        if (dayanak.Length > AlanSinirlari.KapanisDayanakEnCok) dayanak = dayanak[..AlanSinirlari.KapanisDayanakEnCok];

        var mevcut = await db.OzelMatrahKapanislari
            .FirstOrDefaultAsync(k => k.TenantId == islem.TenantId && k.Kaynak == dto.Kaynak && k.Gun == gun);
        var simdi = DateTime.UtcNow;
        if (mevcut is not null)
        {
            var kullanan = await KullananFaturaSayisi(islem.TenantId, dto.Kaynak, gun);
            if (kullanan > 0)
                throw new ConflictException(
                    $"Bu kapanış {kullanan} faturada kullanıldı; değiştirilemez.");
            var eski = mevcut.Fiyat;
            mevcut.Fiyat = fiyat; mevcut.GirilenDeger = deger; mevcut.GirilenBirim = dto.Birim;
            mevcut.Dayanak = dayanak; mevcut.KaydedenKullaniciId = islem.Id; mevcut.Guncellendi = simdi;
            denetim.YazmaKaydet(islem, "OzelMatrahKapanisi", mevcut.Id,
                string.Create(CultureInfo.InvariantCulture, $"has bedeli kapanışı düzeltildi · {dto.Kaynak} {gun:yyyy-MM-dd} · {eski} → {fiyat} TL/has g"),
                hassas: true, eylem: DenetimEylemi.Guncelleme);
        }
        else
        {
            mevcut = new OzelMatrahKapanisi
            {
                Id = Kimlik.Ulid(), TenantId = islem.TenantId, Kaynak = dto.Kaynak, Gun = gun,
                Fiyat = fiyat, GirilenDeger = deger, GirilenBirim = dto.Birim, Dayanak = dayanak,
                KaydedenKullaniciId = islem.Id, Olusturuldu = simdi, Guncellendi = simdi,
            };
            db.OzelMatrahKapanislari.Add(mevcut);
            denetim.YazmaKaydet(islem, "OzelMatrahKapanisi", mevcut.Id,
                string.Create(CultureInfo.InvariantCulture, $"has bedeli kapanışı girildi · {dto.Kaynak} {gun:yyyy-MM-dd} · {fiyat} TL/has g"),
                hassas: true);
        }
        await db.SaveChangesAsync();
        return Cevap(mevcut);
    }

    /// <summary>Bu (kaynak, gün) kapanışını dayanak almış fatura sayısı.</summary>
    public Task<int> KullananFaturaSayisi(string tenantId, string kaynak, DateOnly gun) =>
        db.Faturalar.AsNoTracking().CountAsync(f => f.TenantId == tenantId
            && f.OzelMatrahKaynakKodu == kaynak && f.OzelMatrahKapanisGunu == gun);

    /// <summary>
    /// Zamanlayıcının kaynaktan aldığı kapanışı yazar (HAREM · ALTINKAYNAK). Değişmediyse yazmaz;
    /// faturada kullanılmış (geçmiş) bir günün kapanışını DEĞİŞTİRMEZ.
    /// </summary>
    public async Task OtomatikKaydet(string tenantId, string kaynak, KaynakKapanisi k)
    {
        if (!KurKaynakKodlari.OtomatikKapanisli.Contains(kaynak) || k.Fiyat <= 0m) return;
        var fiyat = Math.Round(k.Fiyat, 4, MidpointRounding.AwayFromZero);
        var dayanak = k.Dayanak.Length > AlanSinirlari.KapanisDayanakEnCok ? k.Dayanak[..AlanSinirlari.KapanisDayanakEnCok] : k.Dayanak;
        var mevcut = await db.OzelMatrahKapanislari
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Kaynak == kaynak && x.Gun == k.Gun);
        var simdi = DateTime.UtcNow;
        if (mevcut is null)
        {
            db.OzelMatrahKapanislari.Add(new OzelMatrahKapanisi
            {
                Id = Kimlik.Ulid(), TenantId = tenantId, Kaynak = kaynak, Gun = k.Gun, Fiyat = fiyat,
                GirilenDeger = fiyat, GirilenBirim = BirimGram, Dayanak = dayanak,
                KaydedenKullaniciId = null, Olusturuldu = simdi, Guncellendi = simdi,
            });
        }
        else
        {
            if (mevcut.Fiyat == fiyat) return;
            if (await KullananFaturaSayisi(tenantId, kaynak, k.Gun) > 0) return;
            mevcut.Fiyat = fiyat; mevcut.GirilenDeger = fiyat; mevcut.Dayanak = dayanak; mevcut.Guncellendi = simdi;
        }
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex)
        {   // eşzamanlı ikinci tur aynı satırı yazdı — sonraki tur günceller
            db.ChangeTracker.Clear();
            logger.LogDebug("[{TenantId}] {Kaynak} kapanışı yazılamadı (eşzamanlı): {Mesaj}", tenantId, kaynak, ex.Message);
        }
    }

    /// <summary>Kaynağın en son kapanışı (ayar ekranındaki durum satırı için).</summary>
    public async Task<KapanisCevap?> SonKapanis(string tenantId, string kaynak)
    {
        var k = await db.OzelMatrahKapanislari.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Kaynak == kaynak)
            .OrderByDescending(x => x.Gun).FirstOrDefaultAsync();
        return k is null ? null : Cevap(k);
    }

    private static KapanisCevap Cevap(OzelMatrahKapanisi k) => new(
        k.Id, k.Kaynak, k.Gun, k.Fiyat, k.GirilenDeger, k.GirilenBirim, k.Dayanak,
        k.KaydedenKullaniciId is null, k.Guncellendi);
}
