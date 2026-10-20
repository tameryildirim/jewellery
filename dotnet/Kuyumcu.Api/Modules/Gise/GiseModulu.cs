using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Sayfalar.Gise;
using Kuyumcu.Core.Domain.Girdi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Gise;

// ═══════════════════════════════════════════════════════════════════════════
// Y-03 (20.10, rakip taraması § 4.1) — GİŞE ZİMMETİ AKIŞI (Ariyen "Gişe Kasam" 7 adım):
//   1 yönetici ürün adedi + açılış nakdi zimmetler (TEKLIF)
//   2 tezgâhtar kabul eder (KABUL; açılış nakdi kaynak kasadan gişe kasasına VİRMAN)
//   3 gün içi takviye talebi; karar veren isteyenden FARKLI yönetici (kendi talebini onaylayamaz)
//   4 satış tezgâhtarın defter hareketinden okunur (zimmet − satılan = beklenen); nakit gişe kasasında birikir
//   5 akşam sayım: ürün ve nakit beklenen/sayılan, kalem bazlı fark; fark varsa gerekçe (K30)
//   6 nakit farkı gişe kasasına sayım farkı hareketi; sayılan nakit devir kasasına (ana kasa) VİRMAN (KAPANDI)
//   7 vardiya devri = kapat + yeni zimmet
// Para hareketleri mevcut kapılardan geçer (VirmanServisi, KasaServisi.HareketGir): gün kilidi, bakiye kapısı,
// nakit ayrımı ve denetim izi değişmez. Stok TAŞINMAZ (zimmet sorumluluk kaydıdır; sayım raporu bozulmaz).
// ═══════════════════════════════════════════════════════════════════════════

public sealed class GiseKalemDto
{
    [Required, MaxLength(36)] public string UrunId { get; set; } = null!;
    [Range(typeof(decimal), "0", "999999999.999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Adet { get; set; }
}

public sealed class GiseAcDto
{
    [Required, MaxLength(36)] public string KasaId { get; set; } = null!;
    [Required, MaxLength(36)] public string PersonelId { get; set; } = null!;
    [MaxLength(36)] public string? KaynakKasaId { get; set; }
    [Tutar(TutarTuru.SifirOlabilir)] public decimal? AcilisNakit { get; set; }
    public List<GiseKalemDto> Kalemler { get; set; } = [];
}

public sealed class GiseTakviyeDto
{
    [MaxLength(36)] public string? UrunId { get; set; }
    [Range(typeof(decimal), "0", "999999999.999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal? Adet { get; set; }
    [Tutar] public decimal? NakitTutar { get; set; }
}

public sealed class GiseKararDto
{
    public bool Onay { get; set; }
}

public sealed class GiseSayimSatiriDto
{
    [Required, MaxLength(36)] public string UrunId { get; set; } = null!;
    [Range(typeof(decimal), "0", "999999999.999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal? SayilanAdet { get; set; }
}

public sealed class GiseKapatDto
{
    [Tutar(TutarTuru.SifirOlabilir)] public decimal? SayilanNakit { get; set; }
    public List<GiseSayimSatiriDto> Satirlar { get; set; } = [];
    [MaxLength(1000)] public string? Aciklama { get; set; }
    [MaxLength(36)] public string? DevirKasaId { get; set; }
}

public sealed class GiseServisi(
    KuyumcuDbContext db, Kasa.KasaServisi kasaServisi, Gider.VirmanServisi virman,
    ILogger<GiseServisi> logger, DenetimServisi? denetim = null)
{
    private const int GecmisSiniri = 20;
    /// <summary>B-8: her okuma sınırlı. Bir zimmette en çok bu kadar kalem/ürün/kişi; açık zimmet kasa+personel başına tektir.</summary>
    private const int SorguSiniri = 500;

    private static bool Yonetici(AktifKullanici islem) => islem.Rol is "SAHIP" or "MUDUR";

    // ── 1) Zimmetle (TEKLIF) ────────────────────────────────────────────────
    public async Task<GiseZimmetDetay> Olustur(GiseAcDto dto, AktifKullanici islem)
    {
        if (dto.PersonelId == islem.Id)
            throw new BadRequestException("Zimmet kendinize verilemez; tezgâhtar kabul eder").AlanIle("personelId");
        var personel = await db.Kullanicilar.AsNoTracking()
            .FirstOrDefaultAsync(k => k.TenantId == islem.TenantId && k.Id == dto.PersonelId && k.Aktif)
            ?? throw new BadRequestException("Zimmetlenecek personel bulunamadı ya da pasif").AlanIle("personelId");
        var kasa = await db.Kasalar.AsNoTracking()
            .FirstOrDefaultAsync(k => k.TenantId == islem.TenantId && k.Id == dto.KasaId && k.Aktif)
            ?? throw new BadRequestException("Gişe kasası bulunamadı ya da pasif").AlanIle("kasaId");
        if (await db.GiseZimmetleri.AnyAsync(z => z.TenantId == islem.TenantId && z.KasaId == kasa.Id && GiseDurum.Acik.Contains(z.Durum)))
            throw new ConflictException("Bu kasada açık bir zimmet var; önce onu kapatın");
        if (await db.GiseZimmetleri.AnyAsync(z => z.TenantId == islem.TenantId && z.PersonelId == personel.Id && GiseDurum.Acik.Contains(z.Durum)))
            throw new ConflictException("Bu personelin açık bir zimmeti var; önce onu kapatın");

        var acilis = 0m;
        if (dto.AcilisNakit is { } ham && ham != 0m)
        {
            GirdiKapisi.Tutar(ham, "acilisNakit");
            acilis = KolonTavani.PozitifTutar(ham, "acilisNakit", "Açılış nakdi");
        }
        Data.Kasa? kaynak = null;
        if (!string.IsNullOrWhiteSpace(dto.KaynakKasaId))
        {
            if (dto.KaynakKasaId == kasa.Id)
                throw new BadRequestException("Kaynak kasa gişe kasasıyla aynı olamaz").AlanIle("kaynakKasaId");
            kaynak = await db.Kasalar.AsNoTracking()
                .FirstOrDefaultAsync(k => k.TenantId == islem.TenantId && k.Id == dto.KaynakKasaId && k.Aktif)
                ?? throw new BadRequestException("Kaynak kasa bulunamadı ya da pasif").AlanIle("kaynakKasaId");
        }
        if (acilis > 0m && kaynak is null)
            throw new BadRequestException("Açılış nakdi için kaynak kasa seçin").AlanIle("kaynakKasaId");

        var kalemler = dto.Kalemler ?? [];
        if (kalemler.Count == 0 && acilis == 0m)
            throw new BadRequestException("Zimmete en az bir ürün ya da açılış nakdi yazın").AlanIle("kalemler");
        if (kalemler.Select(k => k.UrunId).Distinct().Count() != kalemler.Count)
            throw new BadRequestException("Aynı ürün zimmette iki kez yazılamaz").AlanIle("kalemler");
        var now = DateTime.UtcNow;
        var zimmetId = Kimlik.Ulid();
        var satirlar = new List<GiseZimmetKalemi>();
        foreach (var k in kalemler)
        {
            var adet = await AdetDogrula(k.UrunId, k.Adet, kasa.SubeId, islem.TenantId);
            satirlar.Add(new GiseZimmetKalemi
            {
                Id = Kimlik.Ulid(), TenantId = islem.TenantId, ZimmetId = zimmetId, Tur = GiseKalemTur.Zimmet,
                UrunId = k.UrunId, Adet = adet, Durum = GiseKalemDurum.Kabul, IsteyenId = islem.Id,
                OnaylayanId = islem.Id, KararZamani = now, Olusturuldu = now,
            });
        }
        var z = new GiseZimmeti
        {
            Id = zimmetId, TenantId = islem.TenantId, SubeId = kasa.SubeId, KasaId = kasa.Id, PersonelId = personel.Id,
            KaynakKasaId = kaynak?.Id, AcilisNakit = acilis, Durum = GiseDurum.Teklif, OlusturanId = islem.Id,
            Olusturuldu = now, Guncellendi = now,
        };
        db.GiseZimmetleri.Add(z);
        db.GiseZimmetKalemleri.AddRange(satirlar);
        denetim?.YazmaKaydet(islem, nameof(GiseZimmeti), z.Id,
            FormattableString.Invariant($"gişe zimmeti · personel {personel.Id} · kasa {kasa.Id} · {satirlar.Count} ürün · açılış {acilis:0.00}"),
            hassas: false, kasaId: kasa.Id);
        await db.SaveChangesAsync();
        logger.LogInformation("[{TenantId}] GISE_ZIMMET: {Id} personel={Personel} kasa={Kasa} urun={Urun} acilis={Acilis}",
            islem.TenantId, z.Id, personel.Id, kasa.Id, satirlar.Count, acilis);
        return await Detay(z.Id, islem);
    }

    /// <summary>Ürün aktif + adet > 0 (en çok 3 ondalık) + ŞUBE stoğu yeter. Stok düşülmez; zimmet sorumluluk kaydıdır.</summary>
    private async Task<decimal> AdetDogrula(string urunId, decimal adet, string subeId, string tenantId)
    {
        var urun = await db.Urunler.AsNoTracking().Where(u => u.TenantId == tenantId && u.Id == urunId && u.Aktif)
            .Select(u => new { u.Id, u.Kod }).FirstOrDefaultAsync()
            ?? throw new BadRequestException("Ürün bulunamadı ya da pasif").AlanIle("urunId");
        if (adet <= 0m) throw new BadRequestException("Adet sıfırdan büyük olmalıdır").AlanIle("adet");
        var temiz = KolonTavani.OndalikSiniri(adet, 3, "adet", "Adet") ?? 0m;
        var stok = await db.Stoklar.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.SubeId == subeId && s.UrunId == urunId)
            .Select(s => (decimal?)s.Adet).FirstOrDefaultAsync() ?? 0m;
        if (temiz > stok)
            throw new BadRequestException($"Şube stoğu yetersiz: {urun.Kod} için {stok:0.###} var, {temiz:0.###} zimmetlenemez").AlanIle("adet");
        return temiz;
    }

    // ── 2) Tezgâhtar kabul eder (açılış nakdi virmanla gişeye) ──────────────
    public async Task<GiseZimmetDetay> Kabul(string id, AktifKullanici islem)
    {
        var z = await Bul(id, islem.TenantId);
        if (z.PersonelId != islem.Id)
            throw new ForbiddenException("Zimmeti yalnız zimmetlenen tezgâhtar kabul eder");
        if (z.Durum != GiseDurum.Teklif)
            throw new ConflictException("Zimmet kabul edilecek durumda değil");
        if (z.AcilisNakit > 0m)
            await virman.Olustur(new Gider.VirmanDto
            {
                KaynakTur = Gider.VirmanTarafi.Kasa, KaynakId = z.KaynakKasaId!, HedefTur = Gider.VirmanTarafi.Kasa, HedefId = z.KasaId,
                Tutar = z.AcilisNakit, Aciklama = "Gişe açılış nakdi",
            }, islem);
        z.Durum = GiseDurum.Kabul;
        z.KabulZamani = DateTime.UtcNow;
        z.Guncellendi = z.KabulZamani.Value;
        denetim?.YazmaKaydet(islem, nameof(GiseZimmeti), z.Id, "gişe zimmeti kabul edildi", hassas: false, kasaId: z.KasaId);
        await db.SaveChangesAsync();
        return await Detay(z.Id, islem);
    }

    // ── 3) Takviye talebi + karar (isteyen ≠ karar veren) ───────────────────
    public async Task<GiseZimmetDetay> TakviyeIste(string id, GiseTakviyeDto dto, AktifKullanici islem)
    {
        var z = await Bul(id, islem.TenantId);
        if (z.Durum != GiseDurum.Kabul)
            throw new ConflictException("Takviye yalnız kabul edilmiş zimmette istenir");
        if (z.PersonelId != islem.Id && !Yonetici(islem))
            throw new ForbiddenException("Takviye talebini yalnız zimmetli personel ya da yönetici yazar");
        var urunlu = !string.IsNullOrWhiteSpace(dto.UrunId);
        var nakitli = dto.NakitTutar is { } n && n != 0m;
        if (urunlu == nakitli)
            throw new BadRequestException("Takviyede ürün ya da nakit tutarından biri girilmelidir").AlanIle("urunId");
        var now = DateTime.UtcNow;
        var kalem = new GiseZimmetKalemi
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, ZimmetId = z.Id, Tur = GiseKalemTur.Takviye,
            Durum = GiseKalemDurum.Talep, IsteyenId = islem.Id, Olusturuldu = now,
        };
        if (urunlu)
        {
            kalem.UrunId = dto.UrunId;
            kalem.Adet = await AdetDogrula(dto.UrunId!, dto.Adet ?? 0m, z.SubeId, islem.TenantId);
        }
        else
        {
            if (z.KaynakKasaId is null)
                throw new BadRequestException("Nakit takviye için zimmette kaynak kasa tanımlı değil").AlanIle("nakitTutar");
            GirdiKapisi.Tutar(dto.NakitTutar!.Value, "nakitTutar");
            kalem.NakitTutar = KolonTavani.PozitifTutar(dto.NakitTutar.Value, "nakitTutar", "Nakit tutarı");
        }
        db.GiseZimmetKalemleri.Add(kalem);
        z.Guncellendi = now;
        await db.SaveChangesAsync();
        return await Detay(z.Id, islem);
    }

    public async Task<GiseZimmetDetay> TakviyeKarar(string id, string kalemId, GiseKararDto dto, AktifKullanici islem)
    {
        var z = await Bul(id, islem.TenantId);
        var kalem = await db.GiseZimmetKalemleri.FirstOrDefaultAsync(k => k.TenantId == islem.TenantId && k.ZimmetId == z.Id && k.Id == kalemId && k.Tur == GiseKalemTur.Takviye)
            ?? throw new NotFoundException("Takviye talebi bulunamadı");
        if (kalem.Durum != GiseKalemDurum.Talep)
            throw new ConflictException("Talep zaten karara bağlanmış");
        // Ariyen kuralı: kendi talebini onaylayamaz — yöneticinin yazdığı talebi BAŞKA bir yönetici karara bağlar.
        if (kalem.IsteyenId == islem.Id)
            throw new ForbiddenException("Kendi talebinizi onaylayamazsınız; başka bir yönetici karar vermeli");
        if (z.Durum != GiseDurum.Kabul)
            throw new ConflictException("Takviye yalnız kabul edilmiş zimmette istenir");
        var now = DateTime.UtcNow;
        if (dto.Onay)
        {
            if (kalem.NakitTutar is { } nakit)
            {
                if (z.KaynakKasaId is null)
                    throw new BadRequestException("Nakit takviye için zimmette kaynak kasa tanımlı değil").AlanIle("nakitTutar");
                await virman.Olustur(new Gider.VirmanDto
                {
                    KaynakTur = Gider.VirmanTarafi.Kasa, KaynakId = z.KaynakKasaId, HedefTur = Gider.VirmanTarafi.Kasa, HedefId = z.KasaId,
                    Tutar = nakit, Aciklama = "Gişe nakit takviyesi",
                }, islem);
            }
            else await AdetDogrula(kalem.UrunId!, kalem.Adet, z.SubeId, islem.TenantId);
            kalem.Durum = GiseKalemDurum.Kabul;
        }
        else kalem.Durum = GiseKalemDurum.Red;
        kalem.OnaylayanId = islem.Id;
        kalem.KararZamani = now;
        z.Guncellendi = now;
        denetim?.YazmaKaydet(islem, nameof(GiseZimmetKalemi), kalem.Id, dto.Onay ? "takviye onaylandı" : "takviye reddedildi", hassas: false, kasaId: z.KasaId);
        await db.SaveChangesAsync();
        return await Detay(z.Id, islem);
    }

    // ── 5–6) Akşam sayımı + devir ───────────────────────────────────────────
    public async Task<GiseKapatSonucu> Kapat(string id, GiseKapatDto dto, AktifKullanici islem)
    {
        var z = await Bul(id, islem.TenantId);
        if (z.Durum != GiseDurum.Kabul)
            throw new ConflictException("Zimmet kapanacak durumda değil");
        if (z.PersonelId != islem.Id && !Yonetici(islem))
            throw new ForbiddenException("Zimmeti yalnız zimmetli personel ya da yönetici kapatır");
        if (dto.SayilanNakit is not { } sayilanHam)
            throw new BadRequestException("Sayılan nakit zorunludur; hiç para yoksa 0 yazın").AlanIle("sayilanNakit");
        GirdiKapisi.Tutar(sayilanHam, "sayilanNakit", TutarTuru.SifirOlabilir);
        var sayilan = KolonTavani.IsaretliTutar(sayilanHam, "sayilanNakit", "Sayılan nakit");
        var kasa = await db.Kasalar.AsNoTracking().FirstAsync(k => k.TenantId == islem.TenantId && k.Id == z.KasaId);
        var now = DateTime.UtcNow;

        // Ürünler: beklenen = zimmet + kabul edilen takviye − satılan (tezgâhtarın SATIS defteri, iade geri ekler).
        var urunler = await UrunDurumlari(z, now);
        var sayimlar = (dto.Satirlar ?? []).Where(s => !string.IsNullOrWhiteSpace(s.UrunId)).ToDictionary(s => s.UrunId, s => s.SayilanAdet);
        var sayimSatirlari = new List<GiseZimmetKalemi>();
        var farkliUrun = 0;
        foreach (var u in urunler)
        {
            if (!sayimlar.TryGetValue(u.UrunId, out var sy) || sy is not { } sayilanAdet)
                throw new BadRequestException($"{u.Kod} sayılmadı; her ürünün sayılan adedini yazın").AlanIle("satirlar");
            if (sayilanAdet < 0m) throw new BadRequestException("Sayılan adet eksi olamaz").AlanIle("satirlar");
            var temiz = KolonTavani.OndalikSiniri(sayilanAdet, 3, "satirlar", "Sayılan adet") ?? 0m;
            var fark = temiz - u.Beklenen;
            if (fark != 0m) farkliUrun++;
            sayimSatirlari.Add(new GiseZimmetKalemi
            {
                Id = Kimlik.Ulid(), TenantId = islem.TenantId, ZimmetId = z.Id, Tur = GiseKalemTur.Sayim, UrunId = u.UrunId,
                Adet = u.Beklenen, SayilanAdet = temiz, SatilanAdet = u.Satilan, Fark = fark, Durum = GiseKalemDurum.Kabul,
                IsteyenId = islem.Id, OnaylayanId = islem.Id, KararZamani = now, Olusturuldu = now,
            });
        }

        var ayrim = await Kasa.KasaNakitAyrimi.Hesapla(db, islem.TenantId, kasa.Id, kasa.Bakiye);
        var beklenenNakit = ayrim.Nakit;
        var nakitFark = Math.Round(sayilan - beklenenNakit, 2, MidpointRounding.AwayFromZero);
        if (nakitFark != 0m || farkliUrun > 0) dto.Aciklama = GirdiKapisi.Gerekce(dto.Aciklama, "aciklama");
        else dto.Aciklama = GirdiKapisi.Uzunluk(dto.Aciklama, AlanSinirlari.GerekceEnCok, "aciklama");

        var devirId = string.IsNullOrWhiteSpace(dto.DevirKasaId) ? z.KaynakKasaId : dto.DevirKasaId;
        Data.Kasa? devir = null;
        if (sayilan > 0m)
        {
            if (devirId is null || devirId == z.KasaId)
                throw new BadRequestException("Devir kasası seçin; sayılan nakit ana kasaya aktarılacak").AlanIle("devirKasaId");
            devir = await db.Kasalar.AsNoTracking().FirstOrDefaultAsync(k => k.TenantId == islem.TenantId && k.Id == devirId && k.Aktif)
                ?? throw new BadRequestException("Devir kasası bulunamadı ya da pasif").AlanIle("devirKasaId");
        }

        // Para kapıları sırayla: önce fark (defter = sayılan), sonra devir (gişe nakdi 0'a iner). Her adım kendi kapısından
        // geçer (gün kilidi, bakiye, nakit ayrımı); biri reddederse zimmet KABUL'de kalır ve hata olduğu gibi döner.
        var personelAd = await KullaniciAd(z.PersonelId, islem.TenantId);
        if (nakitFark != 0m)
            await kasaServisi.HareketGir(kasa.Id, new Kasa.KasaHareketDto
            {
                Yon = nakitFark > 0m ? Kuyumcu.Core.Domain.HareketYonu.KasaGiris : Kuyumcu.Core.Domain.HareketYonu.KasaCikis,
                Tutar = Math.Abs(nakitFark), OdemeTuru = "NAKIT",
                Aciklama = $"Gişe sayım farkı · {personelAd}",
            }, islem);
        if (devir is not null)
            await virman.Olustur(new Gider.VirmanDto
            {
                KaynakTur = Gider.VirmanTarafi.Kasa, KaynakId = kasa.Id, HedefTur = Gider.VirmanTarafi.Kasa, HedefId = devir.Id,
                Tutar = sayilan, Aciklama = $"Gişe devri · {personelAd}",
            }, islem);

        db.GiseZimmetKalemleri.AddRange(sayimSatirlari);
        z.Durum = GiseDurum.Kapandi;
        z.BeklenenNakit = beklenenNakit;
        z.SayilanNakit = sayilan;
        z.NakitFark = nakitFark;
        z.Aciklama = dto.Aciklama;
        z.DevirKasaId = devir?.Id;
        z.KapatanId = islem.Id;
        z.KapanisZamani = now;
        z.Guncellendi = now;
        denetim?.YazmaKaydet(islem, nameof(GiseZimmeti), z.Id,
            FormattableString.Invariant($"gişe kapanışı · sayılan {sayilan:0.00} · beklenen {beklenenNakit:0.00} · fark {nakitFark:0.00} · farklı ürün {farkliUrun}"),
            hassas: false, kasaId: kasa.Id);
        await db.SaveChangesAsync();
        logger.LogInformation("[{TenantId}] GISE_KAPANIS: {Id} sayilan={Sayilan} beklenen={Beklenen} fark={Fark} devir={Devir}",
            islem.TenantId, z.Id, sayilan, beklenenNakit, nakitFark, devir?.Id);
        return new GiseKapatSonucu(await Detay(z.Id, islem), devir is null ? 0m : sayilan, devir?.Ad, nakitFark, farkliUrun);
    }

    public async Task<GiseZimmetDetay> Iptal(string id, AktifKullanici islem)
    {
        var z = await Bul(id, islem.TenantId);
        if (z.Durum != GiseDurum.Teklif)
            throw new ConflictException("Yalnız teklif aşamasındaki zimmet iptal edilir; kabul edilmiş zimmet sayımla kapanır");
        if (z.PersonelId != islem.Id && !Yonetici(islem))
            throw new ForbiddenException("Bu zimmeti yalnız yönetici ya da zimmetli personel iptal eder");
        z.Durum = GiseDurum.Iptal;
        z.KapatanId = islem.Id;
        z.KapanisZamani = DateTime.UtcNow;
        z.Guncellendi = z.KapanisZamani.Value;
        denetim?.YazmaKaydet(islem, nameof(GiseZimmeti), z.Id, "gişe zimmeti iptal edildi", hassas: false, kasaId: z.KasaId);
        await db.SaveChangesAsync();
        return await Detay(z.Id, islem);
    }

    // ── Okuma ────────────────────────────────────────────────────────────────
    public async Task<GiseListesi> Liste(AktifKullanici islem)
    {
        var yonetim = Yonetici(islem);
        var q = db.GiseZimmetleri.AsNoTracking().Where(z => z.TenantId == islem.TenantId);
        if (!yonetim) q = q.Where(z => z.PersonelId == islem.Id);
        var acik = await q.Where(z => GiseDurum.Acik.Contains(z.Durum)).OrderByDescending(z => z.Olusturuldu).Take(SorguSiniri).ToListAsync();
        var gecmis = await q.Where(z => !GiseDurum.Acik.Contains(z.Durum)).OrderByDescending(z => z.Guncellendi).Take(GecmisSiniri).ToListAsync();
        var hepsi = acik.Concat(gecmis).ToList();
        var ozetler = await Ozetler(hepsi, islem.TenantId);
        return new GiseListesi(acik.Select(z => ozetler[z.Id]).ToList(), gecmis.Select(z => ozetler[z.Id]).ToList(), yonetim);
    }

    public async Task<GiseZimmetDetay> Detay(string id, AktifKullanici islem)
    {
        var z = await db.GiseZimmetleri.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == islem.TenantId && x.Id == id)
            ?? throw new NotFoundException("Zimmet bulunamadı");
        if (!Yonetici(islem) && z.PersonelId != islem.Id) throw new NotFoundException("Zimmet bulunamadı");
        var ozet = (await Ozetler([z], islem.TenantId))[z.Id];
        var kapali = z.Durum is "KAPANDI" or "IPTAL";
        var urunler = kapali ? await KapanisUrunleri(z) : await UrunDurumlari(z, DateTime.UtcNow);
        var takviyeler = await Takviyeler(z);
        var kasalar = await db.Kasalar.AsNoTracking().Where(k => k.TenantId == islem.TenantId).Select(k => new { k.Id, k.Ad, k.Bakiye }).Take(SorguSiniri).ToListAsync();
        var kasaAd = kasalar.ToDictionary(k => k.Id, k => k.Ad);
        decimal beklenenNakit = 0m; var gizli = false;
        if (z.Durum == GiseDurum.Kabul)
        {
            var kasa = kasalar.First(k => k.Id == z.KasaId);
            var ayrim = await Kasa.KasaNakitAyrimi.Hesapla(db, islem.TenantId, kasa.Id, kasa.Bakiye);
            // D1-11 kör sayım: tezgâhtar beklenen nakdi görmez (sayımı değere göre yazamasın); yönetici görür.
            gizli = !Yonetici(islem) && await kasaServisi.KorSayimEtkin(islem.TenantId);
            beklenenNakit = gizli ? 0m : ayrim.Nakit;
        }
        else if (z.BeklenenNakit is { } b) beklenenNakit = b;
        var yonetim = Yonetici(islem);
        return new GiseZimmetDetay(ozet, z.KaynakKasaId, z.KaynakKasaId is { } kk ? kasaAd.GetValueOrDefault(kk) : null,
            beklenenNakit, gizli, z.SayilanNakit, z.NakitFark, z.Aciklama,
            z.DevirKasaId is { } dk ? kasaAd.GetValueOrDefault(dk) : null, urunler, takviyeler,
            KabulEdebilir: z.Durum == GiseDurum.Teklif && z.PersonelId == islem.Id,
            KapatabilirMi: z.Durum == GiseDurum.Kabul && (z.PersonelId == islem.Id || yonetim),
            KararVerebilirMi: z.Durum == GiseDurum.Kabul && yonetim,
            IptalEdebilir: z.Durum == GiseDurum.Teklif && (z.PersonelId == islem.Id || yonetim));
    }

    private async Task<GiseZimmeti> Bul(string id, string tenantId) =>
        await db.GiseZimmetleri.FirstOrDefaultAsync(z => z.TenantId == tenantId && z.Id == id)
        ?? throw new NotFoundException("Zimmet bulunamadı");

    private async Task<string> KullaniciAd(string id, string tenantId)
    {
        var k = await db.Kullanicilar.AsNoTracking().Where(x => x.TenantId == tenantId && x.Id == id).Select(x => new { x.Ad, x.Soyad }).FirstOrDefaultAsync();
        return k is null ? "—" : $"{k.Ad} {k.Soyad}".Trim();
    }

    private async Task<Dictionary<string, GiseZimmetOzet>> Ozetler(List<GiseZimmeti> zler, string tenantId)
    {
        var idler = zler.Select(z => z.Id).ToList();
        var kasaIdler = zler.Select(z => z.KasaId).Distinct().ToList();
        var kasalar = await db.Kasalar.AsNoTracking().Where(k => k.TenantId == tenantId && kasaIdler.Contains(k.Id))
            .Select(k => new { k.Id, k.Ad, k.ParaBirimi }).Take(SorguSiniri).ToDictionaryAsync(k => k.Id);
        var personelIdler = zler.Select(z => z.PersonelId).Distinct().ToList();
        var adlar = await db.Kullanicilar.AsNoTracking().Where(k => k.TenantId == tenantId && personelIdler.Contains(k.Id))
            .Select(k => new { k.Id, k.Ad, k.Soyad }).Take(SorguSiniri).ToDictionaryAsync(k => k.Id, k => $"{k.Ad} {k.Soyad}".Trim());
        var kalemler = await db.GiseZimmetKalemleri.AsNoTracking().Where(k => k.TenantId == tenantId && idler.Contains(k.ZimmetId))
            .Select(k => new { k.ZimmetId, k.Tur, k.Durum, k.UrunId }).Take(SorguSiniri).ToListAsync();
        var sonuc = new Dictionary<string, GiseZimmetOzet>();
        foreach (var z in zler)
        {
            var kz = kalemler.Where(k => k.ZimmetId == z.Id).ToList();
            var urunSayisi = kz.Where(k => k.Tur == GiseKalemTur.Zimmet || (k.Tur == GiseKalemTur.Takviye && k.Durum == GiseKalemDurum.Kabul && k.UrunId != null))
                .Select(k => k.UrunId).Distinct().Count();
            var bekleyen = kz.Count(k => k.Tur == GiseKalemTur.Takviye && k.Durum == GiseKalemDurum.Talep);
            var kasa = kasalar.GetValueOrDefault(z.KasaId);
            sonuc[z.Id] = new GiseZimmetOzet(z.Id, z.KasaId, kasa?.Ad ?? "—", z.PersonelId, adlar.GetValueOrDefault(z.PersonelId, "—"),
                z.Durum, z.AcilisNakit, kasa?.ParaBirimi ?? "TRY", urunSayisi, bekleyen, z.Olusturuldu, z.KabulZamani, z.KapanisZamani, z.NakitFark);
        }
        return sonuc;
    }

    /// <summary>Açık zimmet: ürün başına zimmet (sabah + kabul edilen takviye), satılan (tezgâhtarın SATIS − SATIS_IADE), beklenen.</summary>
    private async Task<List<GiseUrunDurumu>> UrunDurumlari(GiseZimmeti z, DateTime bitis)
    {
        var kalemler = await db.GiseZimmetKalemleri.AsNoTracking()
            .Where(k => k.TenantId == z.TenantId && k.ZimmetId == z.Id && k.UrunId != null && k.Durum == GiseKalemDurum.Kabul
                        && (k.Tur == GiseKalemTur.Zimmet || k.Tur == GiseKalemTur.Takviye))
            .Select(k => new { k.UrunId, k.Adet }).Take(SorguSiniri).ToListAsync();
        var zimmetler = kalemler.GroupBy(k => k.UrunId!).ToDictionary(g => g.Key, g => g.Sum(x => x.Adet));
        var urunIdler = zimmetler.Keys.ToList();
        if (urunIdler.Count == 0) return [];
        var baslangic = z.KabulZamani ?? z.Olusturuldu;
        var satislar = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == z.TenantId && h.KullaniciId == z.PersonelId && h.UrunId != null && urunIdler.Contains(h.UrunId)
                        && h.Tarih >= baslangic && h.Tarih <= bitis && (h.Tur == "SATIS" || h.Tur == "SATIS_IADE"))
            .Select(h => new { h.UrunId, h.Tur, h.Adet }).Take(SorguSiniri).ToListAsync();
        var satilan = satislar.GroupBy(h => h.UrunId!)
            .ToDictionary(g => g.Key, g => g.Sum(h => (h.Tur == "SATIS" ? 1m : -1m) * (h.Adet ?? 0m)));
        var urunler = await db.Urunler.AsNoTracking().Where(u => u.TenantId == z.TenantId && urunIdler.Contains(u.Id))
            .Select(u => new { u.Id, u.Kod, u.Ad }).Take(SorguSiniri).ToDictionaryAsync(u => u.Id);
        return urunIdler.OrderBy(id => urunler.GetValueOrDefault(id)?.Kod).Select(id =>
        {
            var zm = zimmetler[id]; var st = satilan.GetValueOrDefault(id);
            var u = urunler.GetValueOrDefault(id);
            return new GiseUrunDurumu(id, u?.Kod ?? "—", u?.Ad ?? "—", zm, st, zm - st, null, null);
        }).ToList();
    }

    /// <summary>Kapanmış zimmet: SAYIM satırları (dondurulmuş beklenen/sayılan/fark).</summary>
    private async Task<List<GiseUrunDurumu>> KapanisUrunleri(GiseZimmeti z)
    {
        var sayim = await db.GiseZimmetKalemleri.AsNoTracking()
            .Where(k => k.TenantId == z.TenantId && k.ZimmetId == z.Id && k.Tur == GiseKalemTur.Sayim).Take(SorguSiniri).ToListAsync();
        if (sayim.Count == 0) return await UrunDurumlari(z, z.KapanisZamani ?? DateTime.UtcNow);
        var urunIdler = sayim.Select(k => k.UrunId!).ToList();
        var urunler = await db.Urunler.AsNoTracking().Where(u => u.TenantId == z.TenantId && urunIdler.Contains(u.Id))
            .Select(u => new { u.Id, u.Kod, u.Ad }).Take(SorguSiniri).ToDictionaryAsync(u => u.Id);
        return sayim.OrderBy(k => urunler.GetValueOrDefault(k.UrunId!)?.Kod).Select(k =>
        {
            var u = urunler.GetValueOrDefault(k.UrunId!);
            var satilan = k.SatilanAdet ?? 0m;
            return new GiseUrunDurumu(k.UrunId!, u?.Kod ?? "—", u?.Ad ?? "—", k.Adet + satilan, satilan, k.Adet, k.SayilanAdet, k.Fark);
        }).ToList();
    }

    private async Task<List<GiseTakviyeOzet>> Takviyeler(GiseZimmeti z)
    {
        var kalemler = await db.GiseZimmetKalemleri.AsNoTracking()
            .Where(k => k.TenantId == z.TenantId && k.ZimmetId == z.Id && k.Tur == GiseKalemTur.Takviye)
            .OrderByDescending(k => k.Olusturuldu).Take(SorguSiniri).ToListAsync();
        if (kalemler.Count == 0) return [];
        var urunIdler = kalemler.Where(k => k.UrunId != null).Select(k => k.UrunId!).Distinct().ToList();
        var urunler = await db.Urunler.AsNoTracking().Where(u => u.TenantId == z.TenantId && urunIdler.Contains(u.Id))
            .Select(u => new { u.Id, u.Kod, u.Ad }).Take(SorguSiniri).ToDictionaryAsync(u => u.Id);
        var kisiIdler = kalemler.SelectMany(k => new[] { k.IsteyenId, k.OnaylayanId }).Where(x => x != null).Distinct().ToList();
        var adlar = await db.Kullanicilar.AsNoTracking().Where(k => k.TenantId == z.TenantId && kisiIdler.Contains(k.Id))
            .Select(k => new { k.Id, k.Ad, k.Soyad }).Take(SorguSiniri).ToDictionaryAsync(k => k.Id, k => $"{k.Ad} {k.Soyad}".Trim());
        return kalemler.Select(k =>
        {
            var u = k.UrunId is { } uid ? urunler.GetValueOrDefault(uid) : null;
            return new GiseTakviyeOzet(k.Id, k.UrunId, u?.Kod, u?.Ad, k.Adet, k.NakitTutar, k.Durum, k.IsteyenId,
                adlar.GetValueOrDefault(k.IsteyenId, "—"), k.OnaylayanId is { } o ? adlar.GetValueOrDefault(o) : null, k.Olusturuldu);
        }).ToList();
    }
}

[ApiController]
[Authorize]
[Route("api/v1/gise")]
[Yetki.ModulYetkisi("kasa")]
public sealed class GiseController(GiseServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/gise — açık zimmetler + son 20 geçmiş (tezgâhtar yalnız kendininkini görür).</summary>
    [HttpGet] public Task<GiseListesi> Liste() => servis.Liste(Islem);

    /// <summary>POST /api/v1/gise — zimmetle (TEKLIF) — MUDUR+</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<GiseZimmetDetay> Olustur([FromBody] GiseAcDto dto) => servis.Olustur(dto, Islem);

    [HttpGet("{id}")] public Task<GiseZimmetDetay> Detay(string id) => servis.Detay(id, Islem);

    /// <summary>POST …/{id}/kabul — zimmetli tezgâhtar kabul eder (servis kimlik kapısı).</summary>
    [HttpPost("{id}/kabul")] public Task<GiseZimmetDetay> Kabul(string id) => servis.Kabul(id, Islem);

    [HttpPost("{id}/takviye")] public Task<GiseZimmetDetay> TakviyeIste(string id, [FromBody] GiseTakviyeDto dto) => servis.TakviyeIste(id, dto, Islem);

    /// <summary>POST …/{id}/takviye/{kalemId}/karar — MUDUR+; isteyen ≠ karar veren (servis kapısı).</summary>
    [HttpPost("{id}/takviye/{kalemId}/karar")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<GiseZimmetDetay> TakviyeKarar(string id, string kalemId, [FromBody] GiseKararDto dto) => servis.TakviyeKarar(id, kalemId, dto, Islem);

    [HttpPost("{id}/kapat")] public Task<GiseKapatSonucu> Kapat(string id, [FromBody] GiseKapatDto dto) => servis.Kapat(id, dto, Islem);

    [HttpPost("{id}/iptal")] public Task<GiseZimmetDetay> Iptal(string id) => servis.Iptal(id, Islem);
}
