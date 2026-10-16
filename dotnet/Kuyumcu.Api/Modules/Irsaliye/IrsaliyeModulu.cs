using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satinalma;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IrsaliyeVarligi = Kuyumcu.Api.Data.Irsaliye;

namespace Kuyumcu.Api.Modules.Irsaliye;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-26 (Y-35) — SEVK / ALIŞ İRSALİYESİ.
//
// ⚠ İRSALİYE DEFTERE YAZMAZ. Bu dosyada HareketFabrikasi.Ekle, Hareketler.Add,
//   stok/kasa/cari dokunuşu YOKTUR. İrsaliye malın FİZİKSEL sevk belgesidir;
//   değer hareketi faturayla ya da satış/alış ucuyla doğar. GELEN irsaliye
//   mal kabulden doğar ve oradaki kalemler ALIŞ hareketine BAĞLIDIR
//   (irsaliye_kalemleri.hareket_id) — bağı kuran mal kabul ucudur, burası değil.
//
// ⚠ BAĞSIZ İRSALİYE GİZLENMEZ (DoD): faturası kesilmemiş irsaliyeler
//   /irsaliyeler/bagsiz ucunda ve liste özetinde AYRI sayılır. VUK'a göre
//   irsaliye ile fatura arasındaki bağ denetlenebilir olmalıdır; "ACIK"
//   irsaliyenin sessizce kaybolması vergisel risk doğurur.
//
// ⚠ HARD DELETE YOK: iptal = pasifleştirme, gerekçe zorunlu.
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class IrsaliyeKalemDto
{
    public string? UrunId { get; set; }
    public string? Tanim { get; set; }
    public decimal? Adet { get; set; }
}

public sealed class IrsaliyeOlusturDto
{
    /// <summary>Yalnız GIDEN irsaliye elle açılır; GELEN mal kabulden DOĞAR.</summary>
    public string? Yon { get; set; }

    public string? CariId { get; set; }
    public string? SubeId { get; set; }
    public DateTime? Tarih { get; set; }
    public string? Aciklama { get; set; }
    public string? HesapKodu { get; set; }
    public List<IrsaliyeKalemDto>? Kalemler { get; set; }
}

public sealed class IrsaliyeFaturaBagiDto
{
    public string? FaturaId { get; set; }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class IrsaliyeServisi(
    KuyumcuDbContext db, DenetimServisi denetim, ILogger<IrsaliyeServisi> logger)
{
    private const string Varlik = nameof(Data.Irsaliye);

    // ─── Oluştur (yalnız GIDEN) ──────────────────────────────────────────────

    public async Task<object> Olustur(IrsaliyeOlusturDto dto, AktifKullanici islem)
    {
        var yon = (dto.Yon ?? IrsaliyeYonu.Giden).Trim().ToUpperInvariant();
        if (!IrsaliyeYonu.Gecerli(yon))
            throw new BadRequestException(
                $"Geçersiz irsaliye yönü: '{dto.Yon}' ({string.Join(", ", IrsaliyeYonu.Hepsi)})");
        if (yon == IrsaliyeYonu.Gelen)
            throw new BadRequestException(
                "GELEN irsaliye elle açılmaz: satınalma siparişinin MAL KABUL ucundan doğar "
                + "(POST /api/v1/satinalma/{id}/mal-kabul). Böylece stok girişi ile irsaliye ayrışmaz.");

        if (string.IsNullOrWhiteSpace(dto.CariId))
            throw new BadRequestException("Cari seçimi zorunlu");
        await IslemDogrulayici.CariDogrula(db, dto.CariId, islem.TenantId);

        TarihDenetle(dto.Tarih);

        var subeId = string.IsNullOrWhiteSpace(dto.SubeId) ? islem.SubeId : dto.SubeId;
        if (string.IsNullOrWhiteSpace(subeId))
            throw new BadRequestException("Şube seçimi zorunlu: irsaliye malın çıktığı şubeye yazılır");
        await IslemDogrulayici.SubeDogrula(db, subeId, islem.TenantId);

        var kalemler = dto.Kalemler ?? [];
        if (kalemler.Count == 0)
            throw new BadRequestException("İrsaliye en az bir kalem içermeli");
        if (kalemler.Count > IrsaliyeVarligi.AzamiKalem)
            throw new BadRequestException(
                $"Bir irsaliyede en fazla {IrsaliyeVarligi.AzamiKalem} kalem olabilir "
                + $"(gönderilen {kalemler.Count}).");

        var hazir = new List<IrsaliyeKalemi>();
        for (var i = 0; i < kalemler.Count; i++)
        {
            var k = kalemler[i];
            // PAKET PATCH-2: önce kolon hanesine (decimal(12,3)) yuvarla, sonra sıfır kontrolü — 0,0004 kontrolden geçip 0,000 yazılmaz.
            var adet = Math.Round(k.Adet ?? 0m, 3, MidpointRounding.AwayFromZero);
            if (adet <= 0m) throw new BadRequestException($"Kalem {i + 1}: adet sıfırdan büyük olmalı").AlanIle("kalemler");
            var urunId = string.IsNullOrWhiteSpace(k.UrunId) ? null : k.UrunId;
            var tanim = SatinalmaServisi.Kirp(k.Tanim, Kuyumcu.Core.Domain.Girdi.AlanSinirlari.TedarikKalemTanim, "kalemler");
            if (urunId is null && string.IsNullOrWhiteSpace(tanim))
                throw new BadRequestException($"Kalem {i + 1}: ürün seçin ya da bir tanım yazın");
            if (urunId is not null && !await db.Urunler.AsNoTracking()
                    .AnyAsync(u => u.Id == urunId && u.TenantId == islem.TenantId))
                throw new BadRequestException($"Kalem {i + 1}: ürün bulunamadı");
            // PAKET EKSIK-B1-SATINALMA (hasım: ADET'e kesir): ürüne bağlı kalemde ADET birimi tam sayı
            // ister (UrunAdetKapisi — satış/alışla AYNI kural); ürünsüz tanım satırında birim bilinmez.
            if (urunId is not null)
            {
                var birim = await db.Urunler.AsNoTracking()
                    .Where(u => u.Id == urunId && u.TenantId == islem.TenantId).Select(u => u.Birim).FirstOrDefaultAsync();
                Urun.UrunAdetKapisi.Dogrula(birim, adet, "kalemler", $"Kalem {i + 1}");
            }
            hazir.Add(new IrsaliyeKalemi { UrunId = urunId, Tanim = tanim, Adet = adet, Sira = i });
        }

        var now = DateTime.UtcNow;
        var no = await TedarikNo.Uret(db, islem.TenantId, TedarikNo.IrsaliyeOnek, Zaman.YerelBugun().Year);

        for (var deneme = 1; ; deneme++)
        {
            var id = Kimlik.Ulid();
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                db.Irsaliyeler.Add(new IrsaliyeVarligi
                {
                    Id = id,
                    TenantId = islem.TenantId,
                    SubeId = subeId!,
                    No = no,
                    Yon = yon,
                    CariId = dto.CariId!,
                    Tarih = dto.Tarih ?? now,
                    SatinalmaSiparisId = null,
                    FaturaId = null,
                    Durum = IrsaliyeDurumu.Acik,
                    Aciklama = SatinalmaServisi.Kirp(dto.Aciklama, Kuyumcu.Core.Domain.Girdi.AlanSinirlari.TedarikAciklama),
                    HesapKodu = SatinalmaServisi.Kirp(dto.HesapKodu, Kuyumcu.Core.Domain.Girdi.AlanSinirlari.HesapKodu, "hesapKodu"),
                    OlusturanKullaniciId = islem.Id,
                    Olusturuldu = now,
                    Guncellendi = now,
                });

                foreach (var k in hazir)
                    db.IrsaliyeKalemleri.Add(new IrsaliyeKalemi
                    {
                        Id = Kimlik.Ulid(),
                        TenantId = islem.TenantId,
                        IrsaliyeId = id,
                        UrunId = k.UrunId, Tanim = k.Tanim, Adet = k.Adet,
                        HareketId = null, Sira = k.Sira, Olusturuldu = now,
                    });

                denetim.YazmaKaydet(islem, Varlik, id, $"sevk irsaliyesi {no} açıldı · {hazir.Count} kalem");
                await db.SaveChangesAsync();
                await tx.CommitAsync();

                logger.LogInformation("[{TenantId}] IRSALIYE açıldı: {No} ({Yon})", islem.TenantId, no, yon);
                return await Detay(id, islem);
            }
            catch (DbUpdateException) when (deneme < TedarikNo.AzamiDeneme)
            {
                await tx.RollbackAsync();
                db.ChangeTracker.Clear();
                no = TedarikNo.Artir(no);
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync();
                db.ChangeTracker.Clear();
                throw new ConflictException("İrsaliye numarası üretilemedi. Lütfen tekrar deneyin.");
            }
        }
    }

    // ─── Liste ───────────────────────────────────────────────────────────────

    public async Task<object> Listele(
        string? yon, string? durum, string? cariId, bool? bagsiz, int? limit, AktifKullanici islem)
    {
        if (yon is not null && !IrsaliyeYonu.Gecerli(yon))
            throw new BadRequestException($"Geçersiz irsaliye yönü: {yon}");
        if (durum is not null && !IrsaliyeDurumu.Gecerli(durum))
            throw new BadRequestException($"Geçersiz irsaliye durumu: {durum}");

        var etkinLimit = Math.Clamp(limit ?? IrsaliyeVarligi.VarsayilanLimit, 1, IrsaliyeVarligi.AzamiLimit);

        var sorgu = db.Irsaliyeler.AsNoTracking().Where(i => i.TenantId == islem.TenantId);
        if (yon is not null) sorgu = sorgu.Where(i => i.Yon == yon);
        if (durum is not null) sorgu = sorgu.Where(i => i.Durum == durum);
        if (!string.IsNullOrWhiteSpace(cariId)) sorgu = sorgu.Where(i => i.CariId == cariId);
        if (bagsiz == true)
            sorgu = sorgu.Where(i => i.FaturaId == null && i.Durum == IrsaliyeDurumu.Acik);

        var toplam = await sorgu.CountAsync();
        var satirlar = await sorgu
            .OrderByDescending(i => i.Tarih).ThenByDescending(i => i.Id).Take(etkinLimit)
            .Select(i => new
            {
                i.Id, i.No, i.Yon, i.Durum, i.CariId, i.SubeId, i.Tarih,
                i.FaturaId, i.SatinalmaSiparisId, i.Aciklama, i.HesapKodu,
                bagsiz = i.FaturaId == null && i.Durum == IrsaliyeDurumu.Acik,
                kalemSayisi = db.IrsaliyeKalemleri.Count(k => k.IrsaliyeId == i.Id),
                // PAKET EKSIK-B1-SATINALMA (UX-S21): ad sunucudan — ekran kimlik basmaz, ikinci istek atmaz.
                cariAdi = db.Cariler.Where(c => c.Id == i.CariId && c.TenantId == i.TenantId).Select(c => c.Ad).FirstOrDefault(),
            })
            .ToListAsync();

        // Bağsız sayısı SÜZGEÇTEN BAĞIMSIZ ölçülür: "faturası kesilmemiş kaç
        // irsaliye var" sorusu, ekranda hangi süzgecin açık olduğuna bakmaz.
        var bagsizToplam = await db.Irsaliyeler.AsNoTracking()
            .CountAsync(i => i.TenantId == islem.TenantId
                          && i.FaturaId == null && i.Durum == IrsaliyeDurumu.Acik);

        await denetim.ListeOkumaKaydet(islem, Varlik, satirlar.Count, "irsaliye listesi");

        return new
        {
            satirlar, toplam, bagsizToplam,
            limit = etkinLimit,
            azamiLimit = IrsaliyeVarligi.AzamiLimit,
            sinirDegdi = satirlar.Count >= etkinLimit && toplam > satirlar.Count,
        };
    }

    // ─── Detay ───────────────────────────────────────────────────────────────

    public async Task<object> Detay(string id, AktifKullanici islem)
    {
        var i = await Bul(id, islem.TenantId);
        var kalemler = await db.IrsaliyeKalemleri.AsNoTracking()
            .Where(k => k.IrsaliyeId == id && k.TenantId == islem.TenantId)
            .OrderBy(k => k.Sira)
            .Select(k => new
            {
                k.Id, k.UrunId, k.Tanim, k.Adet, k.HareketId, k.Sira,
                // PAKET EKSIK-B1-SATINALMA (UX-S21): ad sunucudan — ekran kimlik basmaz, ikinci istek atmaz.
                urunAdi = db.Urunler.Where(u => u.Id == k.UrunId && u.TenantId == k.TenantId).Select(u => u.Ad).FirstOrDefault(),
                birim = db.Urunler.Where(u => u.Id == k.UrunId && u.TenantId == k.TenantId).Select(u => u.Birim).FirstOrDefault(),
            })
            .Take(IrsaliyeVarligi.AzamiKalem).ToListAsync();

        var cariAdi = await db.Cariler.AsNoTracking()   // PAKET EKSIK-B1-SATINALMA (UX-S21): ad sunucudan — ekran kimlik basmaz, ikinci istek atmaz.
            .Where(c => c.Id == i.CariId && c.TenantId == islem.TenantId).Select(c => c.Ad).FirstOrDefaultAsync();
        return new
        {
            i.Id, i.No, i.Yon, i.Durum, i.CariId, cariAdi, i.SubeId, i.Tarih,
            i.SatinalmaSiparisId, i.FaturaId, i.Aciklama, i.HesapKodu,
            i.IptalGerekce, i.Olusturuldu, i.Guncellendi,
            kalemler,
            bagsiz = i.FaturaId is null && i.Durum == IrsaliyeDurumu.Acik,
            duzenlenebilir = i.Durum == IrsaliyeDurumu.Acik,
            kilitSebebi = i.Durum switch
            {
                IrsaliyeDurumu.Acik => null,
                IrsaliyeDurumu.Faturalandi =>
                    "İrsaliye bir faturaya bağlı; açıklama/hesap kodu dışında değişiklik için önce bağı kaldırın.",
                _ => "İrsaliye iptal edildi; düzenlenemez. «İrsaliyeyi aktife al» ile yeniden açın ya da yeni irsaliye açın.",
            },
        };
    }

    // ─── PATCH ───────────────────────────────────────────────────────────────

    /// <summary>PATCH: gövdede YOKSA korunur, null ise SİLİNİR.</summary>
    public async Task<object> Guncelle(string id, JsonElement govde, AktifKullanici islem)
    {
        var i = await Bul(id, islem.TenantId);
        if (i.Durum == IrsaliyeDurumu.Iptal)
            throw new ConflictException("İptal edilmiş irsaliye düzenlenemez. Yeni irsaliye açın.");

        var degisenler = new List<string>();

        if (SatinalmaServisi.Alan(govde, "aciklama", out var a))
        {
            var yeni = a.ValueKind == JsonValueKind.Null ? null : SatinalmaServisi.Kirp(a.GetString(), Kuyumcu.Core.Domain.Girdi.AlanSinirlari.TedarikAciklama);
            if (yeni != i.Aciklama) degisenler.Add("açıklama");
            i.Aciklama = yeni;
        }

        if (SatinalmaServisi.Alan(govde, "hesapKodu", out var h))
        {
            var yeni = h.ValueKind == JsonValueKind.Null ? null : SatinalmaServisi.Kirp(h.GetString(), Kuyumcu.Core.Domain.Girdi.AlanSinirlari.HesapKodu, "hesapKodu");
            if (yeni != i.HesapKodu) degisenler.Add("hesap kodu");
            i.HesapKodu = yeni;
        }

        if (SatinalmaServisi.Alan(govde, "tarih", out var t))
        {
            if (t.ValueKind == JsonValueKind.Null)
                throw new BadRequestException(
                    "İrsaliye tarihi boş bırakılamaz: sevkin ne zaman yapıldığı belgenin zorunlu unsurudur.");
            var yeni = t.GetDateTime();
            if (yeni != i.Tarih) TarihDenetle(yeni);
            if (yeni != i.Tarih) degisenler.Add("tarih");
            i.Tarih = yeni;
        }

        if (degisenler.Count == 0)
            throw new BadRequestException(
                "Düzenlenebilir alan gönderilmedi (açıklama, hesap kodu, tarih). Kalem değişikliği için "
                + "irsaliyeyi iptal edip yenisini düzenleyin — sevk edilen mal geriye dönük değiştirilemez.");

        i.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, Varlik, i.Id,
            $"irsaliye {i.No} düzenlendi · değişen: {string.Join(", ", degisenler)}");
        await db.SaveChangesAsync();
        return await Detay(id, islem);
    }

    // ─── Faturaya bağla / bağı kaldır ────────────────────────────────────────

    /// <summary>
    /// İrsaliyeyi faturaya bağlar (DoD: "irsaliye faturaya bağlanır").
    /// Fatura aynı kiracı ve aynı cariye ait olmalıdır — aksi hâlde belge
    /// zinciri yanlış kurulur ve mal başka müşterinin faturasına yazılır.
    /// </summary>
    public async Task<object> FaturayaBagla(string id, string? faturaId, AktifKullanici islem)
    {
        var i = await Bul(id, islem.TenantId);
        if (i.Durum == IrsaliyeDurumu.Iptal)
            throw new ConflictException("İptal edilmiş irsaliye faturaya bağlanamaz.");
        if (string.IsNullOrWhiteSpace(faturaId))
            throw new BadRequestException("Fatura seçimi zorunlu");

        var fatura = await db.Faturalar.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == faturaId && f.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Fatura bulunamadı");

        if (!string.IsNullOrWhiteSpace(fatura.CariId) && fatura.CariId != i.CariId)
            throw new BadRequestException(
                "Fatura başka bir cariye ait. İrsaliye yalnız aynı carinin faturasına bağlanır.");

        i.FaturaId = faturaId;
        i.Durum = IrsaliyeDurumu.Faturalandi;
        i.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, Varlik, i.Id, $"irsaliye {i.No} faturaya bağlandı · {faturaId}");
        await db.SaveChangesAsync();
        return await Detay(id, islem);
    }

    /// <summary>Bağı kaldırır (geri dönüş yolu — UX-S25). İrsaliye ACIK'a döner.</summary>
    public async Task<object> BagiKaldir(string id, AktifKullanici islem)
    {
        var i = await Bul(id, islem.TenantId);
        if (i.FaturaId is null)
            throw new ConflictException("İrsaliye zaten bir faturaya bağlı değil.");

        i.FaturaId = null;
        i.Durum = IrsaliyeDurumu.Acik;
        i.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, Varlik, i.Id, $"irsaliye {i.No} fatura bağı kaldırıldı");
        await db.SaveChangesAsync();
        return await Detay(id, islem);
    }

    // ─── İptal ───────────────────────────────────────────────────────────────

    public async Task<object> Iptal(string id, string? gerekce, AktifKullanici islem)
    {
        var i = await Bul(id, islem.TenantId);
        if (i.Durum == IrsaliyeDurumu.Iptal)
            throw new ConflictException("İrsaliye zaten iptal edilmiş.");

        var temiz = (gerekce ?? "").Trim();
        if (temiz.Length < IrsaliyeVarligi.GerekceAsgari)
            throw new BadRequestException(
                $"İptal gerekçesi en az {IrsaliyeVarligi.GerekceAsgari} karakter olmalı (şu an {temiz.Length}).");

        // Deftere bağlı kalem varsa irsaliye iptali ALIŞI GERİ ALMAZ — bu
        // sessizce yapılırsa stok ile belge ayrışır (Anayasa md.1).
        var defterBagli = await db.IrsaliyeKalemleri.AsNoTracking()
            .AnyAsync(k => k.IrsaliyeId == id && k.TenantId == islem.TenantId && k.HareketId != null);
        if (defterBagli)
            throw new ConflictException(
                "Bu irsaliyenin kalemleri deftere yazılmış (mal kabul). İrsaliyeyi iptal etmek stoğu "
                + "geri almaz. Önce ALIŞ İADESİ ya da storno ile defteri düzeltin.");

        i.Durum = IrsaliyeDurumu.Iptal;
        i.IptalGerekce = SatinalmaServisi.Kirp(temiz, Kuyumcu.Core.Domain.Girdi.AlanSinirlari.TedarikAciklama, "gerekce");
        i.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, Varlik, i.Id, $"irsaliye {i.No} iptal edildi · {temiz}");
        await db.SaveChangesAsync();
        return await Detay(id, islem);
    }

    /// <summary>
    /// PAKET SATINALMA-EKRAN — İPTALDEN GERİ DÖNÜŞ: iptal irsaliye yeniden AÇIK olur (fatura bağı iptalde
    /// zaten yoktu). Defter bağlı irsaliye iptal edilemediği için buraya hiç gelmez; yine de kalem
    /// deftere bağlıysa fail-closed reddedilir.
    /// </summary>
    public async Task<object> AktifeAl(string id, AktifKullanici islem)
    {
        var i = await Bul(id, islem.TenantId);
        if (i.Durum != IrsaliyeDurumu.Iptal)
            throw new ConflictException($"Yalnız iptal edilmiş irsaliye aktife alınabilir (şu an: {i.Durum}).");
        var defterBagli = await db.IrsaliyeKalemleri.AsNoTracking()
            .AnyAsync(k => k.IrsaliyeId == id && k.TenantId == islem.TenantId && k.HareketId != null);
        if (defterBagli)
            throw new ConflictException("Bu irsaliyenin kalemleri deftere bağlı (mal kabul); aktife alınamaz.");

        i.Durum = i.FaturaId is null ? IrsaliyeDurumu.Acik : IrsaliyeDurumu.Faturalandi;
        i.IptalGerekce = null;
        i.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, Varlik, i.Id, $"irsaliye {i.No} iptalden aktife alındı");
        await db.SaveChangesAsync();
        return await Detay(id, islem);
    }

    // ─── Bağsız irsaliye raporu (DoD) ────────────────────────────────────────

    public async Task<object> Bagsiz(int? gunEsigi, AktifKullanici islem)
    {
        var esik = Math.Clamp(gunEsigi ?? 0, 0, 3650);
        var sinir = DateTime.UtcNow.AddDays(-esik);

        var satirlar = await db.Irsaliyeler.AsNoTracking()
            .Where(i => i.TenantId == islem.TenantId
                     && i.FaturaId == null && i.Durum == IrsaliyeDurumu.Acik
                     && i.Tarih <= sinir)
            .OrderBy(i => i.Tarih)
            .Take(IrsaliyeVarligi.AzamiLimit)
            .Select(i => new
            {
                i.Id, i.No, i.Yon, i.CariId, i.SubeId, i.Tarih, i.SatinalmaSiparisId, i.Aciklama,
                gecenGun = EF.Functions.DateDiffDay(i.Tarih, DateTime.UtcNow),
            })
            .ToListAsync();

        return new
        {
            satirlar,
            toplam = satirlar.Count,
            gunEsigi = esik,
            azamiLimit = IrsaliyeVarligi.AzamiLimit,
            sinirDegdi = satirlar.Count >= IrsaliyeVarligi.AzamiLimit,
            aciklama = satirlar.Count == 0
                ? "Faturası kesilmemiş irsaliye yok."
                : "Bu irsaliyelerin faturası kesilmemiş; her biri için fatura düzenleyip bağlayın.",
        };
    }

    /// <summary>
    /// PAKET EKSIK-B1-SATINALMA (hasım: geçmiş/gelecek tarih) — irsaliye (belge) tarihi Core
    /// TarihPenceresi.Islem penceresinde: bugün dâhil, ileri tarih yok, en çok 1 yıl geri (İŞ KURALI
    /// K8a). ⚠ Mevzuat hükmü DEĞİLDİR: VUK md.230/5 "fiili sevk tarihi"nin ileri tarihli
    /// düzenlenip düzenlenemeyeceğini bu paket birincil metinden OKUMADI — mali müşavir sorusu
    /// raporda. PATCH'te yalnız DEĞİŞEN tarih sınanır.
    /// </summary>
    internal static void TarihDenetle(DateTime? tarih) =>
        Common.Dogrulama.GirdiKapisi.IslemTarihi(tarih is { } t
            ? (t.Kind == DateTimeKind.Utc ? Zaman.YerelBugun(t) : DateOnly.FromDateTime(t))
            : null, "tarih");

    private async Task<IrsaliyeVarligi> Bul(string id, string tenantId) =>
        await db.Irsaliyeler.FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId)
        ?? throw new NotFoundException("İrsaliye bulunamadı");
}

// ── Uçlar ────────────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/irsaliyeler")]
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
public sealed class IrsaliyeController(IrsaliyeServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/irsaliyeler — liste (bağsız sayısı süzgeçten bağımsız)</summary>
    [HttpGet]
    public Task<object> Listele(
        [FromQuery] string? yon, [FromQuery] string? durum, [FromQuery] string? cariId,
        [FromQuery] bool? bagsiz, [FromQuery] int? limit)
        => servis.Listele(yon, durum, cariId, bagsiz, limit, Islem);

    /// <summary>GET /api/v1/irsaliyeler/bagsiz — faturası kesilmemiş irsaliyeler (DoD)</summary>
    [HttpGet("bagsiz")]
    public Task<object> Bagsiz([FromQuery] int? gunEsigi) => servis.Bagsiz(gunEsigi, Islem);

    /// <summary>GET /api/v1/irsaliyeler/{id} — detay</summary>
    [HttpGet("{id}")]
    public Task<object> Detay(string id) => servis.Detay(id, Islem);

    /// <summary>POST /api/v1/irsaliyeler — yeni SEVK (GIDEN) irsaliyesi</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Olustur([FromBody] IrsaliyeOlusturDto dto) => servis.Olustur(dto, Islem);

    /// <summary>PATCH /api/v1/irsaliyeler/{id} — gövdede yoksa KORU, null ise SİL</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Guncelle(string id, [FromBody] JsonElement govde) =>
        servis.Guncelle(id, govde, Islem);

    /// <summary>POST /api/v1/irsaliyeler/{id}/fatura — faturaya bağla</summary>
    [HttpPost("{id}/fatura")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> FaturayaBagla(string id, [FromBody] IrsaliyeFaturaBagiDto dto) =>
        servis.FaturayaBagla(id, dto.FaturaId, Islem);

    /// <summary>
    /// POST /api/v1/irsaliyeler/{id}/fatura-bagini-kaldir — fatura bağını kaldır (geri dönüş yolu).
    /// ⚠ DELETE DEĞİL: belge modüllerinde [HttpDelete] yasaktır (denetim.sh
    /// "Finansal varlıkta [HttpDelete] = 0"); bu uç zaten hiçbir satır silmez,
    /// yalnız bağı çözüp irsaliyeyi ACIK durumuna geri alır.
    /// </summary>
    [HttpPost("{id}/fatura-bagini-kaldir")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> BagiKaldir(string id) => servis.BagiKaldir(id, Islem);

    /// <summary>POST /api/v1/irsaliyeler/{id}/aktife-al — iptalden geri dönüş</summary>
    [HttpPost("{id}/aktife-al")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> AktifeAl(string id) => servis.AktifeAl(id, Islem);

    /// <summary>POST /api/v1/irsaliyeler/{id}/iptal — PASİFLEŞTİRME (hard DELETE yok)</summary>
    [HttpPost("{id}/iptal")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Iptal(string id, [FromBody] TedarikGerekceDto dto) =>
        servis.Iptal(id, dto.Gerekce, Islem);
}
