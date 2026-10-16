using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Sayim;

// ═════════════════════════════════════════════════════════════════════════════
// SAYIM OTURUMU — paket 23. Tek ürün sayımı (UrunServisi.StokSayimGuncelle, TB-18)
// zaten deftere yazıyordu; oturum, vitrini/reyonu toptan sayıp FARK RAPORU alır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SayimAcDto
{
    [Required, MaxLength(AlanSinirlari.SayimAdi)] public string Ad { get; set; } = null!;
    [Required, MaxLength(36)] public string SubeId { get; set; } = null!;
    [RegularExpression("TUMU|REYON|KATEGORI")] public string KapsamTur { get; set; } = "TUMU";
    [MaxLength(36)] public string? KapsamId { get; set; }
    [MaxLength(1000)] public string? Notlar { get; set; }
}

public sealed class SayimSayDto
{
    /// <summary>Ürün kimliği ya da barkod — biri zorunlu.</summary>
    [MaxLength(36)] public string? UrunId { get; set; }
    [MaxLength(AlanSinirlari.Barkod)] public string? Barkod { get; set; }
    // PAKET GOC-C: gövdede adet YOKSA eskiden 0 bağlanıyordu (sessiz sıfır) → nullable + servis içi "girilmedi".
    // Üst sınır stok kolonu decimal(12,3); ADET birimli üründe tam sayı servis içinde (UrunAdetKapisi).
    [Range(typeof(decimal), "0", "999999999.999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal? SayilanAdet { get; set; }

    /// <summary>
    /// PAKET P-SAYIM (denetim K5): barkod okuyucu akışı — "okut + Enter = 1 adet". Verilirse satırın
    /// mevcut sayılanına EKLENİR (sayılmamışsa 0'dan başlar); aynı barkod tekrar okutulunca +1.
    /// <see cref="SayilanAdet"/> ile BİRLİKTE gönderilemez (biri: mutlak adet ya da artış).
    /// </summary>
    [Range(typeof(decimal), "0", "999999999.999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal? Artis { get; set; }
}

public sealed class SayimKapatDto
{
    /// <summary>
    /// Oturum açıkken HAREKET GÖREN (satış/alış vb.) satırların da uygulanmasına açık
    /// onay. Varsayılan false: o satırlarda "sayılan" hangi anın gerçeğini yansıttığı
    /// belirsizdir; sessizce üzerine yazılmaz, raporda işaretli kalır.
    /// </summary>
    public bool HareketliSatirlariUygula { get; set; }
    [MaxLength(1000)] public string? Notlar { get; set; }
}

public sealed class SayimServisi(KuyumcuDbContext db, Urun.UrunServisi urun)
{
    public Task<List<SayimOturumu>> Liste(AktifKullanici islem) =>
        db.SayimOturumlari.AsNoTracking().Where(s => s.TenantId == islem.TenantId)
            .OrderByDescending(s => s.Acildi).Take(100).ToListAsync();

    /// <summary>Oturumu açar ve kapsamdaki ürünlerin sistem adedini DONDURUR.</summary>
    /// <summary>PAKET GOC-C: oturum adı kırpılmış boş olamaz, kırpılmış hâli sınırı aşamaz (servis yolu; PATCH ile aynı).</summary>
    private static string AdKapisi(string? ad)
    {
        var t = (ad ?? "").Trim();
        if (t.Length == 0) throw new BadRequestException("Sayım oturumunun adı boş olamaz").AlanIle("ad");
        return GirdiKapisi.Uzunluk(t, AlanSinirlari.SayimAdi, "ad")!;
    }

    public async Task<object> Ac(SayimAcDto dto, AktifKullanici islem)
    {
        await IslemDogrulayici.SubeDogrula(db, dto.SubeId, islem.TenantId);
        // Aynı şubede ikinci açık oturum: iki sayım aynı stoğu iki farklı sonuca yazardı.
        if (await db.SayimOturumlari.AnyAsync(s => s.TenantId == islem.TenantId && s.SubeId == dto.SubeId && s.Durum == "ACIK"))
            throw new BadRequestException("Bu şubede açık bir sayım oturumu var; önce onu kapatın ya da iptal edin");

        var urunler = db.Urunler.AsNoTracking().Where(u => u.TenantId == islem.TenantId && u.Aktif);
        switch (dto.KapsamTur)
        {
            case "REYON":
                if (string.IsNullOrEmpty(dto.KapsamId) || !await db.Reyonlar.AnyAsync(r => r.Id == dto.KapsamId && r.TenantId == islem.TenantId))
                    throw new BadRequestException("Kapsam reyonu bulunamadı");
                urunler = urunler.Where(u => u.ReyonId == dto.KapsamId);
                break;
            case "KATEGORI":
                if (string.IsNullOrEmpty(dto.KapsamId) || !await db.UrunKategorileri.AnyAsync(c => c.Id == dto.KapsamId && c.TenantId == islem.TenantId))
                    throw new BadRequestException("Kapsam kategorisi bulunamadı");
                var altlar = await AltKategoriler(dto.KapsamId, islem.TenantId);
                urunler = urunler.Where(u => u.KategoriId != null && altlar.Contains(u.KategoriId));
                break;
        }
        var urunIdler = await urunler.Select(u => u.Id).ToListAsync();
        if (urunIdler.Count == 0) throw new BadRequestException("Kapsamda sayılacak ürün yok");

        var stoklar = await db.Stoklar.AsNoTracking()
            .Where(s => s.TenantId == islem.TenantId && s.SubeId == dto.SubeId && urunIdler.Contains(s.UrunId))
            .ToDictionaryAsync(s => s.UrunId, s => s.Adet);

        var now = DateTime.UtcNow;
        var oturum = new SayimOturumu
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, SubeId = dto.SubeId, Ad = AdKapisi(dto.Ad),
            KapsamTur = dto.KapsamTur, KapsamId = dto.KapsamTur == "TUMU" ? null : dto.KapsamId,
            Acildi = now, AcanKullaniciId = islem.Id, Notlar = dto.Notlar,
        };
        db.SayimOturumlari.Add(oturum);
        foreach (var uid in urunIdler)
            db.SayimSatirlari.Add(new SayimSatiri
            {
                Id = Kimlik.Ulid(), TenantId = islem.TenantId, OturumId = oturum.Id, UrunId = uid,
                SistemAdet = stoklar.GetValueOrDefault(uid, 0m),   // stok satırı yok = sistemde 0
            });
        await db.SaveChangesAsync();
        return await Detay(oturum.Id, islem);
    }

    /// <summary>Sayılan kolonunun tavanı (decimal(12,3)) — artışla aşılırsa 500 değil 400 alan.</summary>
    public const decimal SayilanTavani = 999_999_999.999m;

    /// <summary>Sayılan adedi yazar (aynı ürün tekrar sayılırsa ÜZERİNE yazar — son sayım geçerli).
    /// P-SAYIM: <c>artis</c> verilirse mevcut sayılana EKLER (okut + Enter = +1).</summary>
    public async Task<object> Say(string oturumId, SayimSayDto dto, AktifKullanici islem)
    {
        var oturum = await AcikOturum(oturumId, islem.TenantId);
        string urunId;
        if (!string.IsNullOrEmpty(dto.UrunId)) urunId = dto.UrunId;
        else if (!string.IsNullOrEmpty(dto.Barkod))
            urunId = await db.Urunler.AsNoTracking()
                .Where(u => u.TenantId == islem.TenantId && u.Barkod == dto.Barkod).Select(u => u.Id).FirstOrDefaultAsync()
                ?? throw new NotFoundException($"Barkod bulunamadı: {dto.Barkod}");
        else throw new BadRequestException("Ürün ya da barkod verilmelidir");

        var satir = await db.SayimSatirlari.FirstOrDefaultAsync(s => s.OturumId == oturum.Id && s.UrunId == urunId && s.TenantId == islem.TenantId)
            ?? throw new BadRequestException("Bu ürün oturumun kapsamında değil");
        var birim = await db.Urunler.AsNoTracking().Where(u => u.Id == urunId && u.TenantId == islem.TenantId)
            .Select(u => u.Birim).FirstOrDefaultAsync();
        decimal sayilan;
        if (dto.Artis is { } artisGirdi)
        {
            // PAKET P-SAYIM (K5): okutma artışı — mutlak adetle birlikte gelemez, sıfır/eksi olamaz,
            // ADET birimli üründe tam sayı (UrunAdetKapisi; kesir yok). Sonuç satırın yeni sayılanı.
            if (dto.SayilanAdet is not null)
                throw new BadRequestException("Sayılan adet ile artış birlikte gönderilemez").AlanIle("artis");
            var artis = Math.Round(artisGirdi, 3, MidpointRounding.AwayFromZero);
            if (artis <= 0m) GirdiKapisi.Denetle(new GirdiHatasi(artis < 0m ? GirdiKodu.SayiEksi : GirdiKodu.SayiSifir), "artis");
            Urun.UrunAdetKapisi.SifirOlabilirDogrula(birim, artis, "artis");
            sayilan = (satir.SayilanAdet ?? 0m) + artis;
            if (sayilan > SayilanTavani)
                throw new BadRequestException("Sayılan adet çok büyük — kaydedilebilecek en büyük değeri aşıyor").AlanIle("artis");
        }
        else
        {
            // PAKET GOC-C (#6 · K14): sayılan adet girilmeli, eksi olamaz, ADET birimli üründe tam sayı.
            if (dto.SayilanAdet is not { } mutlak)
                throw new BadRequestException("Sayılan adet girilmedi").AlanIle("sayilanAdet");
            sayilan = Math.Round(mutlak, 3, MidpointRounding.AwayFromZero);
            if (sayilan < 0m) GirdiKapisi.Denetle(new GirdiHatasi(GirdiKodu.SayiEksi), "sayilanAdet");
            Urun.UrunAdetKapisi.SifirOlabilirDogrula(birim, sayilan, "sayilanAdet");
        }
        satir.SayilanAdet = sayilan;
        satir.Sayildi = DateTime.UtcNow;
        satir.SayanKullaniciId = islem.Id;
        await db.SaveChangesAsync();
        return await Detay(oturum.Id, islem);
    }

    /// <summary>Fark raporu: dondurulmuş sistem adedi · sayılan · GÜNCEL stok · oturum sırasında hareket.</summary>
    public async Task<object> Detay(string oturumId, AktifKullanici islem)
    {
        var oturum = await db.SayimOturumlari.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == oturumId && s.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Sayım oturumu bulunamadı");
        var satirlar = await db.SayimSatirlari.AsNoTracking().Where(s => s.OturumId == oturum.Id).ToListAsync();
        var urunIdler = satirlar.Select(s => s.UrunId).ToList();
        var urunler = await db.Urunler.AsNoTracking().Where(u => u.TenantId == islem.TenantId && urunIdler.Contains(u.Id))
            // PAKET 20: sayım farkının has gramı MADENSİZ gösteriliyordu; iki
            // farklı madenin farkı "netFarkHas" altında tek sayıya toplanıyordu.
            .ToDictionaryAsync(u => u.Id, u => new { u.Kod, u.Ad, u.Barkod, u.HasGrami, u.Birim, u.AyarId });
        var guncel = await db.Stoklar.AsNoTracking()
            .Where(s => s.TenantId == islem.TenantId && s.SubeId == oturum.SubeId && urunIdler.Contains(s.UrunId))
            .ToDictionaryAsync(s => s.UrunId, s => s.Adet);
        // Oturum açıldıktan sonra bu şubede ürüne dokunan hareketler (sayım düzeltmesi hariç).
        var bitis = oturum.Kapandi ?? DateTime.UtcNow;
        var hareketli = (await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.SubeId == oturum.SubeId && h.UrunId != null
                     && urunIdler.Contains(h.UrunId) && h.Tarih >= oturum.Acildi && h.Tarih <= bitis && h.Tur != "SAYIM_DUZELTME")
            .Select(h => h.UrunId!).Distinct().ToListAsync()).ToHashSet();

        var rapor = satirlar.Select(s =>
        {
            var u = urunler.GetValueOrDefault(s.UrunId);
            var fark = s.SayilanAdet is { } sy ? sy - s.SistemAdet : (decimal?)null;
            return new
            {
                urunId = s.UrunId, kod = u?.Kod, ad = u?.Ad ?? "Bilinmiyor", barkod = u?.Barkod, birim = u?.Birim ?? "ADET",
                sistemAdet = s.SistemAdet, sayilanAdet = s.SayilanAdet, guncelAdet = guncel.GetValueOrDefault(s.UrunId, 0m),
                fark, farkHas = fark is { } f && u is not null ? Math.Round(f * u.HasGrami, 6) : (decimal?)null,
                maden = u is null ? Kuyumcu.Core.Domain.Maden.Altin
                        : Kuyumcu.Core.Domain.Ayarlar.MadenBul(u.AyarId) ?? Kuyumcu.Core.Domain.Maden.Altin,
                sayildi = s.Sayildi, oturumdaHareketVar = hareketli.Contains(s.UrunId),
                duzeltmeHareketId = s.DuzeltmeHareketId,
            };
        }).OrderBy(r => r.ad).ToList();

        return new
        {
            oturum.Id, oturum.Ad, oturum.SubeId, oturum.KapsamTur, oturum.KapsamId, oturum.Durum, oturum.Acildi, oturum.Kapandi, oturum.Notlar,
            toplamSatir = rapor.Count,
            sayilan = rapor.Count(r => r.sayilanAdet != null),
            sayilmayan = rapor.Count(r => r.sayilanAdet == null),
            farkli = rapor.Count(r => r.fark is not null && r.fark != 0m),
            eksikAdet = rapor.Where(r => r.fark < 0m).Sum(r => -r.fark ?? 0m),
            fazlaAdet = rapor.Where(r => r.fark > 0m).Sum(r => r.fark ?? 0m),
            // K20-2/K20-3: net fark YALNIZ ALTINDIR; diğer madenler ayrı satırda.
            netFarkHas = rapor.Where(r => r.maden == Kuyumcu.Core.Domain.Maden.Altin).Sum(r => r.farkHas ?? 0m),
            madenDagilimi = rapor.GroupBy(r => r.maden).Select(gr => new
            {
                maden = gr.Key,
                madenAdi = Kuyumcu.Core.Domain.Maden.Ad(gr.Key),
                farkHas = Math.Round(gr.Sum(r => r.farkHas ?? 0m), 6),
            }).Where(x => x.farkHas != 0m).ToList(),
            hareketli = rapor.Count(r => r.oturumdaHareketVar),
            satirlar = rapor,
        };
    }

    /// <summary>
    /// Kapanış: SAYILMIŞ ve fark ≠ 0 satırlar deftere yazılır (TB-18 yolu). Sayılmayan
    /// satır SIFIRLANMAZ — "sayılmadı" bilinmeyendir, sıfır değil. Oturum sırasında hareket
    /// gören satırlar yalnız açık onayla uygulanır.
    /// </summary>
    public async Task<object> Kapat(string oturumId, SayimKapatDto dto, AktifKullanici islem)
    {
        // URUN-ACILIS-ATOMIK tur 2 (doğrulayıcı ORTA): kapanış TEK işlemdir. Eskiden her
        // satırın StokSayimGuncelle'si kendi işlemini commit ediyordu; ortadaki bir satır
        // reddedince (ör. tavan, kur yok) önceki satırların defteri yazılmış, oturum AÇIK
        // kalıyordu. Şimdi herhangi bir satır reddederse HİÇBİR düzeltme yazılmaz, oturum
        // AÇIK kalır; StokSayimGuncelle bu işleme katılır (IsBaslatVeyaDevral).
        await using var tx = await db.Database.IsBaslatVeyaDevral();
        var oturum = await AcikOturum(oturumId, islem.TenantId);
        var detay = await Detay(oturum.Id, islem);
        // Sıra kimliğe göre SABİT (ULID = açılış sırası): uygulama sırası belirsiz kalmaz.
        var satirlar = await db.SayimSatirlari.Where(s => s.OturumId == oturum.Id && s.SayilanAdet != null)
            .OrderBy(s => s.Id).ToListAsync();
        var hareketli = ((IEnumerable<dynamic>)((dynamic)detay).satirlar)
            .Where(r => (bool)r.oturumdaHareketVar).Select(r => (string)r.urunId).ToHashSet();

        var uygulanan = 0; var atlanan = 0;
        foreach (var s in satirlar)
        {
            var fark = s.SayilanAdet!.Value - s.SistemAdet;
            if (fark == 0m) continue;
            if (hareketli.Contains(s.UrunId) && !dto.HareketliSatirlariUygula) { atlanan++; continue; }

            var oncesi = await db.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == islem.TenantId && h.Tur == "SAYIM_DUZELTME" && h.UrunId == s.UrunId && h.SubeId == oturum.SubeId)
                .Select(h => h.Id).ToListAsync();
            // Hareketli satırda "sayılan" fiziksel gerçek kabul edilir ve GÜNCEL stoğun üzerine yazılır.
            await urun.StokSayimGuncelle(s.UrunId, oturum.SubeId, s.SayilanAdet.Value, islem);
            var yeni = await db.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == islem.TenantId && h.Tur == "SAYIM_DUZELTME" && h.UrunId == s.UrunId && h.SubeId == oturum.SubeId && !oncesi.Contains(h.Id))
                .OrderByDescending(h => h.Olusturuldu).Select(h => h.Id).FirstOrDefaultAsync();
            s.DuzeltmeHareketId = yeni;
            uygulanan++;
        }

        oturum.Durum = "KAPANDI"; oturum.Kapandi = DateTime.UtcNow; oturum.KapatanKullaniciId = islem.Id;
        if (dto.Notlar is not null) oturum.Notlar = dto.Notlar;
        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();   // URUN-ACILIS-ATOMIK: tek commit
        return new { uygulanan, atlanan, detay = await Detay(oturum.Id, islem) };
    }

    public async Task<object> Iptal(string oturumId, AktifKullanici islem)
    {
        var oturum = await AcikOturum(oturumId, islem.TenantId);
        oturum.Durum = "IPTAL"; oturum.Kapandi = DateTime.UtcNow; oturum.KapatanKullaniciId = islem.Id;
        await db.SaveChangesAsync();
        return await Detay(oturum.Id, islem);
    }

    // ─── PATCH — oturumun ADI ve NOTU (PAKET P-22 / B-44) ────────────────────

    /// <summary>
    /// PATCH sözleşmesi: <b>gövdede yoksa KORU, null ise SİL</b>.
    /// <para>
    /// ⚠ Gövde ham <see cref="JsonElement"/>'tir: nullable DTO ile "gönderilmedi"
    /// ile "null gönderildi" ayırt EDİLEMEZ.
    /// </para>
    /// <para>
    /// ⚠ KAPSAM (B-44): yalnız <b>etiket</b> alanları — ad ve not. Sayılan adet,
    /// kapsam, şube ve durum BURADAN DEĞİŞMEZ; onlar sayımın kendisidir.
    /// Ad, sayım <b>kapandıktan sonra da</b> düzeltilebilir (yanlış yazılmış bir
    /// başlık, kapanmış bir tutanağı yalancı yapmaz); İPTAL edilmiş oturum ise
    /// artık hiçbir alanıyla değişmez.
    /// </para>
    /// </summary>
    public async Task<object> Guncelle(string oturumId, JsonElement govde, AktifKullanici islem)
    {
        if (govde.ValueKind != JsonValueKind.Object)
            throw new BadRequestException("Geçersiz istek gövdesi");

        var o = await db.SayimOturumlari
            .FirstOrDefaultAsync(s => s.Id == oturumId && s.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Sayım oturumu bulunamadı");

        if (o.Durum == "IPTAL")
            throw new BadRequestException(
                "İptal edilmiş sayım oturumu düzenlenemez; yeni bir oturum açın.");

        var degisti = false;

        if (govde.TryGetProperty("ad", out var adOge))
        {
            if (adOge.ValueKind == JsonValueKind.Null)
                throw new BadRequestException("Sayım oturumunun adı silinemez");
            if (adOge.ValueKind != JsonValueKind.String)
                throw new BadRequestException("Sayım oturumunun adı metin olmalı");
            var ad = (adOge.GetString() ?? "").Trim();
            if (ad.Length == 0) throw new BadRequestException("Sayım oturumunun adı boş olamaz");
            if (ad.Length > 150) throw new BadRequestException("Sayım oturumunun adı en çok 150 karakter olabilir");
            if (ad != o.Ad) { o.Ad = ad; degisti = true; }
        }

        if (govde.TryGetProperty("notlar", out var notOge))
        {
            if (notOge.ValueKind == JsonValueKind.Null) { o.Notlar = null; degisti = true; }
            else if (notOge.ValueKind != JsonValueKind.String)
                throw new BadRequestException("Not metin olmalı");
            else
            {
                var not = notOge.GetString()?.Trim();
                if (not is { Length: > 1000 })
                    throw new BadRequestException("Not en çok 1000 karakter olabilir");
                o.Notlar = string.IsNullOrEmpty(not) ? null : not;
                degisti = true;
            }
        }

        // ⚠ Denetim izi: DenetimYazmaDinleyicisi SaveChanges'ı dinler, güncelleme
        //   kendiliğinden düşer. İkinci bir yazım ÇİFT KAYIT olurdu.
        if (degisti) await db.SaveChangesAsync();
        return await Detay(o.Id, islem);
    }

    private async Task<SayimOturumu> AcikOturum(string id, string tenantId)
    {
        var o = await db.SayimOturumlari.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId)
            ?? throw new NotFoundException("Sayım oturumu bulunamadı");
        if (o.Durum != "ACIK") throw new BadRequestException($"Oturum {o.Durum} durumunda; değiştirilemez");
        return o;
    }

    private async Task<HashSet<string>> AltKategoriler(string kokId, string tenantId)
    {
        var tumu = await db.UrunKategorileri.AsNoTracking().Where(c => c.TenantId == tenantId).Select(c => new { c.Id, c.UstId }).ToListAsync();
        var sonuc = new HashSet<string> { kokId }; var degisti = true;
        while (degisti) { degisti = false; foreach (var c in tumu) if (c.UstId is not null && sonuc.Contains(c.UstId) && sonuc.Add(c.Id)) degisti = true; }
        return sonuc;
    }
}

// Sayım bir STOK işidir (paket UX-4a madde 14): menüde de uçta da "urun" modül
// yetkisi geçerlidir. Önceden yalnız [Authorize] vardı; menü ise "rapor" bloğunda
// duruyordu — ekran ile sunucu farklı kapı kullanıyordu.
[ApiController, Authorize]
// P-09: Sayım KENDİ anahtarını taşır ("urun" değil).
[Yetki.ModulYetkisi("sayim")]
[Route("api/v1/sayim")]
public sealed class SayimModulu(SayimServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);
    [HttpGet] public Task<List<SayimOturumu>> Liste() => servis.Liste(Islem);
    [HttpPost] public Task<object> Ac([FromBody] SayimAcDto dto) => servis.Ac(dto, Islem);
    [HttpGet("{id}")] public Task<object> Detay(string id) => servis.Detay(id, Islem);
    [HttpPost("{id}/say")] public Task<object> Say(string id, [FromBody] SayimSayDto dto) => servis.Say(id, dto, Islem);
    [HttpPost("{id}/kapat")] public Task<object> Kapat(string id, [FromBody] SayimKapatDto dto) => servis.Kapat(id, dto, Islem);
    [HttpPost("{id}/iptal")] public Task<object> Iptal(string id) => servis.Iptal(id, Islem);

    /// <summary>PATCH /api/v1/sayim/{id} — oturumun ADI ve NOTU (P-22 / B-44).
    /// Sözleşme: gövdede yoksa KORU, null ise SİL.</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<object> Guncelle(string id, [FromBody] JsonElement govde) => servis.Guncelle(id, govde, Islem);
}
