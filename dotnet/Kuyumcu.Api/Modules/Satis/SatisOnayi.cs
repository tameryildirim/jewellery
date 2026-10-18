using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Satis;

// ═════════════════════════════════════════════════════════════════════════════
// D1-05 (18.10, rakip eşitleme: "eşikli işlem onayı · personel iskonto sınırı · personel kural uyarıları").
//   • İşletme eşikleri (Ayarlar): indirim yüzdesi (liste fiyatına göre), fiş tutarı, zararına satış (has değeri altı).
//     Hepsi boşken KAPALI — davranış değişmez.
//   • Çalışanın kişisel iskonto sınırı (Kullanıcılar). Sahip ve müdür onay vericidir: onlara sorulmaz.
//   • Aşan satış 409 + kod ONAY_GEREKLI döner (hiçbir şey yazılmaz). Tezgâh aynı içerikle POST /onay-talepleri
//     açar; talep sunucuda ölçülür (ekranın söylediği tutara güvenilmez), patrona bildirim düşer. Onaylanınca
//     tezgâh satışı onayTalebiId ile yeniden gönderir: talep AYNI çalışanın, AYNI içerikli (parmak izi) satışında,
//     onaydan sonraki 30 dakikada, BİR KEZ geçer.
//   • Personel uyarıları (engel değil, bildirim): saatte 3+ iade · mesai dışı satış.
// ═════════════════════════════════════════════════════════════════════════════

public sealed record OnayKalemi(string UrunId, decimal Adet, decimal BirimFiyatTl, decimal? IscilikTl, decimal? SatirIskontoYuzde, bool Hediye);

public sealed record OnayOlcumu(decimal ToplamTl, decimal ListeTl, decimal IndirimYuzde, decimal HasDegeriTl, int KalemSayisi, string Parmak);

public sealed class SatisOnayAyarDto
{
    [Range(0.01, 100, ErrorMessage = "İndirim eşiği 0 ile 100 arasında olmalıdır")] public decimal? IskontoYuzde { get; set; }
    [Range(1, 1_000_000_000, ErrorMessage = "Tutar eşiği sıfırdan büyük olmalıdır")] public decimal? TutarTl { get; set; }
    public bool Zararina { get; set; }
    [RegularExpression("^([01][0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Saat SS:dd biçiminde olmalıdır")] public string? MesaiBaslangic { get; set; }
    [RegularExpression("^([01][0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Saat SS:dd biçiminde olmalıdır")] public string? MesaiBitis { get; set; }
}

public sealed class OnayTalebiKalemDto
{
    [Required, MaxLength(36)] public string UrunId { get; set; } = null!;
    [Range(0.001, KolonTavani.UstAdet)] public decimal Adet { get; set; }
    [Range(0, KolonTavani.UstTutar)] public decimal BirimFiyatTl { get; set; }
    [Range(0, KolonTavani.UstTutar)] public decimal? IscilikTl { get; set; }
    [Range(0, 100)] public decimal? SatirIskontoYuzde { get; set; }
    public bool Hediye { get; set; }
}

public sealed class OnayTalebiDto
{
    [MaxLength(36)] public string? CariId { get; set; }
    [Required, MaxLength(36)] public string KurSnapshotId { get; set; } = null!;
    [Range(0, 100)] public decimal? FisIskontoYuzde { get; set; }
    [Required, MinLength(1), MaxLength(200)] public List<OnayTalebiKalemDto> Kalemler { get; set; } = [];
}

public sealed class OnayKarariDto
{
    [MaxLength(200)] public string? Not { get; set; }
}

public static class SatisOnayMetni
{
    public static string Gerekli => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Bu satış patron onayı ister — tezgâhtan onay isteyin.");
    public static string Gecersiz => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Onay bu satışa ait değil ya da süresi geçti — yeniden onay isteyin.");
    public static string Gereksiz => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Bu satış onay istemiyor");
    public static string Bulunamadi => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Onay talebi bulunamadı");
    public static string Karara => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Bu talep artık karar beklemiyor");
}

public static class SatisOnayKapisi
{
    public const string Kod = "ONAY_GEREKLI";
    public const string SebepIskonto = "ISKONTO", SebepTutar = "TUTAR", SebepZararina = "ZARARINA", SebepKisisel = "KISISEL";

    public sealed record Ayar(decimal? IskontoYuzde, decimal? TutarTl, bool Zararina, string? MesaiBaslangic, string? MesaiBitis)
    {
        public bool Kapali => IskontoYuzde is null && TutarTl is null && !Zararina;
    }

    public static async Task<Ayar> AyarOku(KuyumcuDbContext db, string tenantId)
    {
        string[] anahtarlar = [KiraciAyarAnahtari.OnayIskontoYuzde, KiraciAyarAnahtari.OnayTutarTl, KiraciAyarAnahtari.OnayZararina,
            KiraciAyarAnahtari.MesaiBaslangic, KiraciAyarAnahtari.MesaiBitis];
        var d = await db.KiraciAyarlari.AsNoTracking()
            .Where(a => a.TenantId == tenantId && anahtarlar.Contains(a.Anahtar))
            .Take(anahtarlar.Length)
            .ToDictionaryAsync(a => a.Anahtar, a => a.Deger);
        static decimal? Sayi(string? s) => decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : null;
        static string? Bos(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
        return new Ayar(Sayi(d.GetValueOrDefault(KiraciAyarAnahtari.OnayIskontoYuzde)), Sayi(d.GetValueOrDefault(KiraciAyarAnahtari.OnayTutarTl)),
            d.GetValueOrDefault(KiraciAyarAnahtari.OnayZararina) == "true",
            Bos(d.GetValueOrDefault(KiraciAyarAnahtari.MesaiBaslangic)), Bos(d.GetValueOrDefault(KiraciAyarAnahtari.MesaiBitis)));
    }

    /// <summary>Satışı ölçer: uygulanan toplam (satır + fiş iskontosu sonrası), sunucu liste toplamı, has değeri, parmak izi.</summary>
    public static async Task<OnayOlcumu> Olc(KuyumcuDbContext db, Fiyat.FiyatServisi fiyat, string tenantId, string? cariId,
        string kurSnapshotId, decimal? fisIskontoYuzde, IReadOnlyList<OnayKalemi> kalemler)
    {
        var hasKuru = await db.KurSnapshots.AsNoTracking()
            .Where(s => s.Id == kurSnapshotId && s.TenantId == tenantId).Select(s => (decimal?)s.AltinHasFiyati).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Kur kaydı bulunamadı");
        var bugun = DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));
        decimal satir = 0m, liste = 0m, has = 0m;
        foreach (var k in kalemler)
        {
            if (k.Hediye) continue;   // hediyenin kendi kuralı var (yanında bedelli kalem); indirim sayılmaz
            var urun = await db.Urunler.AsNoTracking().FirstOrDefaultAsync(u => u.Id == k.UrunId && u.TenantId == tenantId && u.Aktif)
                ?? throw new NotFoundException("Ürün bulunamadı veya aktif değil");
            var brut = Math.Round(k.Adet * k.BirimFiyatTl + (k.IscilikTl ?? 0m), 2, MidpointRounding.AwayFromZero);
            satir += brut - IskontoHesabi.Tutar(brut, k.SatirIskontoYuzde);
            var teklif = await fiyat.Teklif(urun, cariId, hasKuru, bugun);
            liste += teklif is null ? brut : Math.Round(teklif.SatisFiyati * k.Adet + (k.IscilikTl ?? 0m), 2, MidpointRounding.AwayFromZero);
            has += urun.HasGrami * k.Adet * hasKuru;
        }
        if (fisIskontoYuzde is null && !string.IsNullOrEmpty(cariId))
        {
            var cariOran = await db.Cariler.AsNoTracking().Where(c => c.Id == cariId && c.TenantId == tenantId)
                .Select(c => c.IskontoYuzde).FirstOrDefaultAsync();
            if (cariOran is > 0m) fisIskontoYuzde = cariOran;
        }
        var toplam = satir - IskontoHesabi.Tutar(Math.Round(satir, 2, MidpointRounding.AwayFromZero), fisIskontoYuzde);
        toplam = Math.Round(toplam, 2, MidpointRounding.AwayFromZero);
        var indirim = liste > 0m ? Math.Max(0m, Math.Round((liste - toplam) / liste * 100m, 2, MidpointRounding.AwayFromZero)) : 0m;

        var izler = kalemler
            .Select(k => string.Join(':', k.UrunId, Sayi(k.Adet), Sayi(k.BirimFiyatTl), Sayi(k.IscilikTl ?? 0m), Sayi(k.SatirIskontoYuzde ?? 0m), k.Hediye ? "H" : "-"))
            .Order(StringComparer.Ordinal);
        var kaynak = string.Join('|', [cariId ?? "-", kurSnapshotId, Sayi(fisIskontoYuzde ?? -1m), .. izler]);
        var parmak = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(kaynak))).ToLowerInvariant();
        return new OnayOlcumu(toplam, Math.Round(liste, 2, MidpointRounding.AwayFromZero), indirim,
            Math.Round(has, 2, MidpointRounding.AwayFromZero), kalemler.Count, parmak);
    }

    private static string Sayi(decimal d) => d.ToString("0.####", CultureInfo.InvariantCulture);

    public static List<string> Sebepler(OnayOlcumu o, Ayar a, decimal? kisisel)
    {
        var l = new List<string>();
        if (a.IskontoYuzde is { } iy && o.IndirimYuzde > iy) l.Add(SebepIskonto);
        if (kisisel is { } ks && o.IndirimYuzde > ks) l.Add(SebepKisisel);
        if (a.TutarTl is { } t && o.ToplamTl > t) l.Add(SebepTutar);
        if (a.Zararina && o.HasDegeriTl > 0m && o.ToplamTl < o.HasDegeriTl) l.Add(SebepZararina);
        return l;
    }

    public static bool OnayVerici(string? rol) => rol is "SAHIP" or "MUDUR";

    /// <summary>
    /// Satış kapısı: sahip/müdür ya da kural yok → geçer. Kural aşılıyorsa geçerli onay talebi şart; talep kullanıldı
    /// işaretlenir (satışla AYNI SaveChanges'te yazılır — satış düşerse onay da kullanılmamış kalır).
    /// </summary>
    public static async Task Denetle(KuyumcuDbContext db, Fiyat.FiyatServisi fiyat, AktifKullanici islem, string? cariId,
        string kurSnapshotId, decimal? fisIskontoYuzde, IReadOnlyList<OnayKalemi> kalemler, string? onayTalebiId)
    {
        if (OnayVerici(islem.Rol)) return;
        var ayar = await AyarOku(db, islem.TenantId);
        var kisisel = await db.Kullanicilar.AsNoTracking().Where(u => u.Id == islem.Id && u.TenantId == islem.TenantId)
            .Select(u => u.IskontoSiniriYuzde).FirstOrDefaultAsync();
        if (ayar.Kapali && kisisel is null) return;
        var olcum = await Olc(db, fiyat, islem.TenantId, cariId, kurSnapshotId, fisIskontoYuzde, kalemler);
        if (Sebepler(olcum, ayar, kisisel).Count == 0) return;
        if (string.IsNullOrEmpty(onayTalebiId))
            throw new ConflictException(SatisOnayMetni.Gerekli).KodIle(Kod).AlanIle("onayTalebiId");
        var simdi = DateTime.UtcNow;
        var t = await db.OnayTalepleri.FirstOrDefaultAsync(x => x.Id == onayTalebiId && x.TenantId == islem.TenantId);
        if (t is null || t.IsteyenId != islem.Id || t.Durum != OnayTalebi.Onaylandi || t.Parmak != olcum.Parmak
            || t.Kullanildi is not null || t.KararZamani is not { } kz || kz < simdi.AddMinutes(-OnayTalebi.GecerlilikDakika))
            throw new ConflictException(SatisOnayMetni.Gecersiz).KodIle(Kod).AlanIle("onayTalebiId");
        t.Kullanildi = simdi;
    }
}

public sealed class SatisOnayServisi(KuyumcuDbContext db, Fiyat.FiyatServisi fiyat, DenetimServisi denetim)
{
    public const string BildirimTuru = "SATIS_ONAYI";

    public async Task<SatisOnayAyarlari> AyarGetir(AktifKullanici islem)
    {
        var a = await SatisOnayKapisi.AyarOku(db, islem.TenantId);
        return new SatisOnayAyarlari(a.IskontoYuzde, a.TutarTl, a.Zararina, a.MesaiBaslangic, a.MesaiBitis);
    }

    public async Task<SatisOnayAyarlari> AyarYaz(SatisOnayAyarDto dto, AktifKullanici islem)
    {
        if (dto.IskontoYuzde is <= 0m or > 100m) throw new BadRequestException("İndirim eşiği 0 ile 100 arasında olmalıdır").AlanIle("iskontoYuzde");
        if (dto.TutarTl is <= 0m) throw new BadRequestException("Tutar eşiği sıfırdan büyük olmalıdır").AlanIle("tutarTl");
        if (string.IsNullOrEmpty(dto.MesaiBaslangic) != string.IsNullOrEmpty(dto.MesaiBitis))
            throw new BadRequestException("Mesai için başlangıç ve bitiş birlikte girilir").AlanIle("mesaiBitis");
        foreach (var s in new[] { dto.MesaiBaslangic, dto.MesaiBitis })
            if (!string.IsNullOrEmpty(s) && !TimeOnly.TryParseExact(s, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                throw new BadRequestException("Saat SS:dd biçiminde olmalıdır").AlanIle("mesaiBaslangic");
        var degerler = new Dictionary<string, string?>
        {
            [KiraciAyarAnahtari.OnayIskontoYuzde] = dto.IskontoYuzde?.ToString(CultureInfo.InvariantCulture),
            [KiraciAyarAnahtari.OnayTutarTl] = dto.TutarTl?.ToString(CultureInfo.InvariantCulture),
            [KiraciAyarAnahtari.OnayZararina] = dto.Zararina ? "true" : "false",
            [KiraciAyarAnahtari.MesaiBaslangic] = string.IsNullOrEmpty(dto.MesaiBaslangic) ? null : dto.MesaiBaslangic,
            [KiraciAyarAnahtari.MesaiBitis] = string.IsNullOrEmpty(dto.MesaiBitis) ? null : dto.MesaiBitis,
        };
        var anahtarlar = degerler.Keys.ToList();
        var mevcut = await db.KiraciAyarlari.Where(a => a.TenantId == islem.TenantId && anahtarlar.Contains(a.Anahtar))
            .Take(anahtarlar.Count).ToListAsync();
        foreach (var (anahtar, deger) in degerler)
        {
            var a = mevcut.FirstOrDefault(x => x.Anahtar == anahtar);
            if (a is null)
            {
                a = new KiraciAyari { Id = Kimlik.Ulid(), TenantId = islem.TenantId, Anahtar = anahtar, Deger = "" };
                db.KiraciAyarlari.Add(a);
            }
            a.Deger = deger ?? "";
            a.Guncellendi = DateTime.UtcNow;
        }
        denetim.YazmaKaydet(islem, nameof(KiraciAyari), islem.TenantId,
            $"satış onayı kuralları: iskonto {dto.IskontoYuzde?.ToString(CultureInfo.InvariantCulture) ?? "-"} · tutar {dto.TutarTl?.ToString(CultureInfo.InvariantCulture) ?? "-"} · zararına {dto.Zararina} · mesai {dto.MesaiBaslangic ?? "-"}–{dto.MesaiBitis ?? "-"}", hassas: false);
        await db.SaveChangesAsync();
        return await AyarGetir(islem);
    }

    /// <summary>Talep açar (aynı çalışanın aynı içerikli bekleyen talebi varsa onu döndürür). Ölçüm SUNUCUDA.</summary>
    public async Task<OnayTalebiOzet> Ac(OnayTalebiDto dto, AktifKullanici islem)
    {
        if (dto.Kalemler is not { Count: > 0 }) throw new BadRequestException("Fişte kalem yok").AlanIle("kalemler");
        var kalemler = dto.Kalemler.Select(k => new OnayKalemi(k.UrunId, k.Adet, k.BirimFiyatTl, k.IscilikTl, k.SatirIskontoYuzde, k.Hediye)).ToList();
        var cariId = string.IsNullOrEmpty(dto.CariId) ? null : dto.CariId;
        var olcum = await SatisOnayKapisi.Olc(db, fiyat, islem.TenantId, cariId, dto.KurSnapshotId, dto.FisIskontoYuzde, kalemler);
        var ayar = await SatisOnayKapisi.AyarOku(db, islem.TenantId);
        var ben = await db.Kullanicilar.AsNoTracking().Where(u => u.Id == islem.Id && u.TenantId == islem.TenantId)
            .Select(u => new { u.Ad, u.Soyad, u.IskontoSiniriYuzde }).FirstOrDefaultAsync();
        var sebepler = SatisOnayKapisi.OnayVerici(islem.Rol) ? [] : SatisOnayKapisi.Sebepler(olcum, ayar, ben?.IskontoSiniriYuzde);
        if (sebepler.Count == 0) throw new BadRequestException(SatisOnayMetni.Gereksiz);

        var simdi = DateTime.UtcNow;
        var esik = simdi.AddMinutes(-OnayTalebi.GecerlilikDakika);
        var var = await db.OnayTalepleri.AsNoTracking()
            .Where(t => t.TenantId == islem.TenantId && t.IsteyenId == islem.Id && t.Parmak == olcum.Parmak
                     && t.Durum == OnayTalebi.Bekliyor && t.Olusturuldu > esik)
            .OrderByDescending(t => t.Olusturuldu).FirstOrDefaultAsync();
        if (var is not null) return (await Ozetle([var], islem.TenantId))[0];

        var t = new OnayTalebi
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, IsteyenId = islem.Id, CariId = cariId, Parmak = olcum.Parmak,
            Sebepler = string.Join(',', sebepler), ToplamTl = olcum.ToplamTl, ListeTl = olcum.ListeTl,
            IndirimYuzde = olcum.IndirimYuzde, HasDegeriTl = olcum.HasDegeriTl, KalemSayisi = olcum.KalemSayisi, Olusturuldu = simdi,
        };
        db.OnayTalepleri.Add(t);
        var ad = ben is null ? "" : $"{ben.Ad} {ben.Soyad}".Trim();
        db.Bildirimler.Add(new Bildirim
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, Tur = BildirimTuru, Onem = BildirimOnem.Kritik,
            Baslik = "Satış onayı bekleniyor",
            Govde = $"{ad} · {olcum.ToplamTl.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))} TL · indirim %{olcum.IndirimYuzde.ToString("0.##", CultureInfo.GetCultureInfo("tr-TR"))}",
            HedefRota = "/bildirimler", GorunurRoller = "SAHIP,MUDUR",
            IlgiliVarlikTuru = nameof(OnayTalebi), IlgiliVarlikId = t.Id,
            TekillikAnahtari = $"{BildirimTuru}:{t.Id}", Olusturuldu = simdi,
        });
        denetim.YazmaKaydet(islem, nameof(OnayTalebi), t.Id, $"satış onayı istendi · {t.Sebepler}", hassas: false);
        await db.SaveChangesAsync();
        return (await Ozetle([t], islem.TenantId))[0];
    }

    public async Task<OnayTalebiOzet> Getir(string id, AktifKullanici islem)
    {
        var t = await db.OnayTalepleri.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException(SatisOnayMetni.Bulunamadi);
        if (t.IsteyenId != islem.Id && !SatisOnayKapisi.OnayVerici(islem.Rol)) throw new NotFoundException(SatisOnayMetni.Bulunamadi);
        return (await Ozetle([t], islem.TenantId))[0];
    }

    /// <summary>Sahip/müdür: karar bekleyen (süresi geçmemiş) talepler.</summary>
    public async Task<List<OnayTalebiOzet>> Bekleyenler(AktifKullanici islem)
    {
        var esik = DateTime.UtcNow.AddMinutes(-OnayTalebi.GecerlilikDakika);
        var l = await db.OnayTalepleri.AsNoTracking()
            .Where(t => t.TenantId == islem.TenantId && t.Durum == OnayTalebi.Bekliyor && t.Olusturuldu > esik)
            .OrderBy(t => t.Olusturuldu).ThenBy(t => t.Id).Take(50).ToListAsync();
        return await Ozetle(l, islem.TenantId);
    }

    public async Task<OnayTalebiOzet> Karar(string id, bool onay, OnayKarariDto dto, AktifKullanici islem)
    {
        var t = await db.OnayTalepleri.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException(SatisOnayMetni.Bulunamadi);
        var simdi = DateTime.UtcNow;
        if (t.Durum != OnayTalebi.Bekliyor || t.Olusturuldu < simdi.AddMinutes(-OnayTalebi.GecerlilikDakika))
            throw new ConflictException(SatisOnayMetni.Karara);
        t.Durum = onay ? OnayTalebi.Onaylandi : OnayTalebi.Reddedildi;
        t.KararVerenId = islem.Id; t.KararZamani = simdi;
        t.KararNotu = string.IsNullOrWhiteSpace(dto.Not) ? null : dto.Not.Trim();
        var b = await db.Bildirimler.FirstOrDefaultAsync(x => x.TenantId == islem.TenantId && x.TekillikAnahtari == $"{BildirimTuru}:{t.Id}");
        if (b is not null && b.Kapatildi is null) { b.Kapatildi = simdi; b.KapatanKullaniciId = islem.Id; b.KapatmaNotu = onay ? "onaylandı" : "reddedildi"; }
        denetim.YazmaKaydet(islem, nameof(OnayTalebi), t.Id, $"satış onayı {(onay ? "verildi" : "reddedildi")}", hassas: false);
        await db.SaveChangesAsync();
        return (await Ozetle([t], islem.TenantId))[0];
    }

    private async Task<List<OnayTalebiOzet>> Ozetle(List<OnayTalebi> l, string tenantId)
    {
        var kisiIdler = l.Select(t => t.IsteyenId).Concat(l.Where(t => t.KararVerenId != null).Select(t => t.KararVerenId!)).Distinct().ToList();
        var kisiler = await db.Kullanicilar.AsNoTracking().Where(u => u.TenantId == tenantId && kisiIdler.Contains(u.Id))
            .Take(kisiIdler.Count + 1).ToDictionaryAsync(u => u.Id, u => (u.Ad + " " + u.Soyad).Trim());
        var cariIdler = l.Where(t => t.CariId != null).Select(t => t.CariId!).Distinct().ToList();
        var cariler = await db.Cariler.AsNoTracking().Where(c => c.TenantId == tenantId && cariIdler.Contains(c.Id))
            .Take(cariIdler.Count + 1).ToDictionaryAsync(c => c.Id, c => new { c.Ad, c.SistemCarisi });
        var esik = DateTime.UtcNow.AddMinutes(-OnayTalebi.GecerlilikDakika);
        return [.. l.Select(t =>
        {
            var c = t.CariId is null ? null : cariler.GetValueOrDefault(t.CariId);
            var durum = t.Durum == OnayTalebi.Bekliyor && t.Olusturuldu < esik ? "SURESI_DOLDU" : t.Kullanildi is not null ? "KULLANILDI" : t.Durum;
            return new OnayTalebiOzet(t.Id, durum, t.Sebepler.Split(',', StringSplitOptions.RemoveEmptyEntries), t.ToplamTl, t.ListeTl,
                t.IndirimYuzde, t.HasDegeriTl, t.KalemSayisi, kisiler.GetValueOrDefault(t.IsteyenId), c is null || c.SistemCarisi ? null : c.Ad,
                t.KararVerenId is null ? null : kisiler.GetValueOrDefault(t.KararVerenId), t.KararNotu, t.Olusturuldu, t.KararZamani);
        })];
    }
}

/// <summary>D1-05 — personel kural uyarıları (engel değil; patrona bildirim). Satış/iade YAZILDIKTAN sonra çağrılır,
/// kendi SaveChanges'ı vardır ve hatası satışı etkilemez.</summary>
public sealed class PersonelUyariServisi(KuyumcuDbContext db, ILogger<PersonelUyariServisi> gunluk)
{
    public const string IadeTuru = "PERSONEL_IADE", MesaiTuru = "PERSONEL_MESAI";
    public const int IadeEsigi = 3;

    public async Task SatisSonrasi(AktifKullanici islem)
    {
        if (islem.Rol == "SAHIP") return;
        var ayar = await SatisOnayKapisi.AyarOku(db, islem.TenantId);
        if (ayar.MesaiBaslangic is not { } bs || ayar.MesaiBitis is not { } bt) return;
        var bas = TimeOnly.ParseExact(bs, "HH:mm", CultureInfo.InvariantCulture);
        var bit = TimeOnly.ParseExact(bt, "HH:mm", CultureInfo.InvariantCulture);
        var yerel = Zaman.UtcdenYerele(DateTime.UtcNow);
        var saat = TimeOnly.FromDateTime(yerel);
        var mesaide = bas <= bit ? saat >= bas && saat <= bit : saat >= bas || saat <= bit;   // gece yarısını aşan mesai
        if (mesaide) return;
        await Bildir(islem, MesaiTuru, $"{MesaiTuru}:{islem.Id}:{yerel:yyyy-MM-dd}", "Mesai dışında satış",
            $"{await Ad(islem)} · {yerel:HH:mm} · mesai {bs}–{bt}", "/raporlar");
    }

    public async Task IadeSonrasi(AktifKullanici islem)
    {
        if (islem.Rol == "SAHIP") return;
        var esik = DateTime.UtcNow.AddHours(-1);
        var sayi = await db.Hareketler.AsNoTracking()
            .CountAsync(h => h.TenantId == islem.TenantId && h.KullaniciId == islem.Id && h.Tur == "SATIS_IADE" && h.Tarih >= esik);
        if (sayi < IadeEsigi) return;
        var yerel = Zaman.UtcdenYerele(DateTime.UtcNow);
        await Bildir(islem, IadeTuru, $"{IadeTuru}:{islem.Id}:{yerel:yyyy-MM-dd-HH}", "Peş peşe iade",
            $"{await Ad(islem)} · son bir saatte {sayi} iade", "/raporlar");
    }

    private async Task<string> Ad(AktifKullanici islem) =>
        await db.Kullanicilar.AsNoTracking().Where(u => u.Id == islem.Id && u.TenantId == islem.TenantId)
            .Select(u => (u.Ad + " " + u.Soyad).Trim()).FirstOrDefaultAsync() ?? "";

    private async Task Bildir(AktifKullanici islem, string tur, string tekillik, string baslik, string govde, string rota)
    {
        if (await db.Bildirimler.AsNoTracking().AnyAsync(b => b.TenantId == islem.TenantId && b.TekillikAnahtari == tekillik)) return;
        db.Bildirimler.Add(new Bildirim
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, Tur = tur, Onem = BildirimOnem.Uyari, Baslik = baslik, Govde = govde,
            HedefRota = rota, GorunurRoller = "SAHIP,MUDUR", IlgiliVarlikTuru = nameof(Kullanici), IlgiliVarlikId = islem.Id,
            TekillikAnahtari = tekillik, Olusturuldu = DateTime.UtcNow,
        });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex)
        {
            // Eşzamanlı ikinci istek aynı bildirimi yazdı (tekillik) — uyarı zaten var; satış etkilenmez.
            db.ChangeTracker.Clear();
            gunluk.LogInformation(ex, "Personel uyarısı zaten yazılmış: {Tekillik}", tekillik);
        }
    }
}

[ApiController]
[Authorize]
[Route("api/v1/satis-onayi")]
[Yetki.ModulYetkisi("islem")]
public sealed class SatisOnayController(SatisOnayServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/satis-onayi/ayarlar — işletmenin onay eşikleri ve mesai saatleri.</summary>
    [HttpGet("ayarlar")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<SatisOnayAyarlari> Ayar() => servis.AyarGetir(Islem);

    /// <summary>PUT /api/v1/satis-onayi/ayarlar — eşikleri yaz (boş = kapalı).</summary>
    [HttpPut("ayarlar")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<SatisOnayAyarlari> AyarYaz([FromBody] SatisOnayAyarDto dto) => servis.AyarYaz(dto, Islem);

    /// <summary>POST /api/v1/satis-onayi/talepler — tezgâhtaki satış için onay iste (ölçüm sunucuda).</summary>
    [HttpPost("talepler")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<OnayTalebiOzet> Ac([FromBody] OnayTalebiDto dto) => servis.Ac(dto, Islem);

    /// <summary>GET /api/v1/satis-onayi/talepler — karar bekleyenler (sahip/müdür).</summary>
    [HttpGet("talepler")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<List<OnayTalebiOzet>> Bekleyenler() => servis.Bekleyenler(Islem);

    /// <summary>GET /api/v1/satis-onayi/talepler/{id} — talebin durumu (isteyen ya da onay veren).</summary>
    [HttpGet("talepler/{id}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<OnayTalebiOzet> Getir(string id) => servis.Getir(id, Islem);

    /// <summary>POST /api/v1/satis-onayi/talepler/{id}/onayla</summary>
    [HttpPost("talepler/{id}/onayla")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<OnayTalebiOzet> Onayla(string id, [FromBody] OnayKarariDto? dto) => servis.Karar(id, true, dto ?? new(), Islem);

    /// <summary>POST /api/v1/satis-onayi/talepler/{id}/reddet</summary>
    [HttpPost("talepler/{id}/reddet")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<OnayTalebiOzet> Reddet(string id, [FromBody] OnayKarariDto? dto) => servis.Karar(id, false, dto ?? new(), Islem);
}
