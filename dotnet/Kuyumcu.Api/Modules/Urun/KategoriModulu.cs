using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Urun;

// ═════════════════════════════════════════════════════════════════════════════
// ÜRÜN AĞACI VE REYON
//
// Rakiplerde "bölüm / stok cinsi / stok türü / stok grubu" diye 4 seviyeye
// çıkan bir ağaç var; bizde tek düz `Tip` alanı vardı ve kırılımlı stok raporu
// alınamıyordu.
//
// ⚠ DÖNGÜ KORUMASI: bir kategori kendi atası yapılamaz. Yapılabilseydi ağaç
// yürüyüşü sonsuz döngüye girer ve ürün listesi isteği sunucuyu kilitlerdi.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KategoriOlusturDto
{
    [Required(ErrorMessage = "Kategori adı zorunludur"), MaxLength(150)]
    public string Ad { get; set; } = null!;
    [MaxLength(36)] public string? UstId { get; set; }
    // UX-4d madde 8: "Sıra" hiç doğrulanmıyordu; negatif sıra listeyi sessizce
    // bozuyordu. Sıra bir GÖRÜNTÜ SIRASIDIR, negatif olamaz.
    [Range(0, int.MaxValue, ErrorMessage = "Sıra 0 veya daha büyük olmalıdır")]
    public int Sira { get; set; }
}

public sealed class KategoriGuncelleDto
{
    // PAKET PATCH-3 — Ad ZORUNLU alandır: null = KORU (silinemez), "" → 400 alan:ad.
    [MaxLength(150)] public string? Ad { get; set; }

    // PAKET PATCH-3 — üst kategori İSTEĞE BAĞLI, ÜÇ DURUMLU: gövdede yok → koru ·
    // null ya da ""(geriye uyum) → köke taşı · değer → doğrula+bağla. ESKİ: null = koru idi;
    // ekran "üst yok" seçilince null gönderiyordu → kategori köke TAŞINMIYORDU (sessiz etkisizlik).
    private string? _ustId;
    [MaxLength(36)]
    public string? UstId { get => _ustId; set { _ustId = value; UstIdVerildi = true; } }
    [System.Text.Json.Serialization.JsonIgnore] public bool UstIdVerildi { get; private set; }
    [Range(0, int.MaxValue, ErrorMessage = "Sıra 0 veya daha büyük olmalıdır")]
    public int? Sira { get; set; }
    public bool? Aktif { get; set; }
}

public sealed class ReyonOlusturDto
{
    [Required, MaxLength(36)] public string SubeId { get; set; } = null!;
    [Required, MaxLength(100)] public string Ad { get; set; } = null!;
    [RegularExpression(ReyonTipi.Desen)] public string Tip { get; set; } = ReyonTipi.Vitrin;
    [MaxLength(36)] public string? SorumluKullaniciId { get; set; }
}


/// <summary>
/// PATCH /reyonlar/{id} gövdesi — paket UX-1 Faz B.
/// <para>
/// ⚠ Mehmet K6: <b>SubeId alanı YOKTUR</b> ve eklenmez. Reyonun şubesi hiç
/// değiştirilemez (stok satırı reyona, reyon şubeye bağlıdır); gövdede subeId
/// gönderilse bile bağlanacak bir alan olmadığı için satır DEĞİŞMEZ.
/// </para>
/// <para>
/// ÜÇ DURUMLU PATCH (UX-S26): sorumlu kullanıcı alanı gövdede YOK → korunur ·
/// null → sorumlu SİLİNİR · dolu → atanır. Mevcut XGonderildi deseni kullanılır
/// (KullaniciModulu.SubeIdGonderildi); yeni bir üçlü-durum deseni AÇILMAZ.
/// </para>
/// </summary>
public sealed class ReyonGuncelleDto
{
    private string? _sorumluKullaniciId;

    [StringLength(100, MinimumLength = 2, ErrorMessage = "Reyon adı 2-100 karakter olmalıdır")]
    public string? Ad { get; set; }

    [RegularExpression(ReyonTipi.Desen, ErrorMessage = "Geçersiz reyon tipi")]
    public string? Tip { get; set; }

    [JsonIgnore] public bool SorumluKullaniciIdGonderildi { get; private set; }

    [MaxLength(36)]
    public string? SorumluKullaniciId
    {
        get => _sorumluKullaniciId;
        set { _sorumluKullaniciId = value; SorumluKullaniciIdGonderildi = true; }
    }

    /// <summary>Pasife alınan reyon bu alanla geri açılır (UX-S25 — geri dönüş yolu).</summary>
    public bool? Aktif { get; set; }
}


public sealed class KategoriServisi(KuyumcuDbContext db, DenetimServisi? denetim = null)
{
    /// <summary>Düz liste — kampanya hedefi seçimi için (ağaç yerine).</summary>
    public async Task<object> Duz(AktifKullanici islem) =>
        await db.UrunKategorileri.AsNoTracking()
            .Where(c => c.TenantId == islem.TenantId && c.Aktif)
            .OrderBy(c => c.Ad)
            .Select(c => new { id = c.Id, ad = c.Ad, ustId = c.UstId })
            .ToListAsync();

    /// <summary>Ağacın tamamı, iç içe. Kök kategoriler sıraya göre.</summary>
    public async Task<object> Agac(AktifKullanici islem, bool sadeceAktif = true)
    {
        var hepsi = await Hepsi(islem.TenantId, sadeceAktif);
        var urunSayilari = await db.Urunler.AsNoTracking()
            .Where(u => u.TenantId == islem.TenantId && u.KategoriId != null)
            .GroupBy(u => u.KategoriId!)
            .Select(g => new { Id = g.Key, Adet = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Adet);

        return hepsi.Where(k => k.UstId is null)
            .OrderBy(k => k.Sira).ThenBy(k => k.Ad)
            .Select(k => Dugum(k, hepsi, urunSayilari))
            .ToList();
    }

    public async Task<object> Olustur(KategoriOlusturDto dto, AktifKullanici islem)
    {
        if (!string.IsNullOrEmpty(dto.UstId)) await UstVarMi(dto.UstId, islem.TenantId);

        var now = DateTime.UtcNow;
        var kategori = new UrunKategorisi
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            Ad = dto.Ad.Trim(),
            UstId = string.IsNullOrEmpty(dto.UstId) ? null : dto.UstId,
            Sira = dto.Sira,
            Aktif = true,
            Olusturuldu = now,
            Guncellendi = now,
        };
        db.Set<UrunKategorisi>().Add(kategori);
        await db.SaveChangesAsync();
        return kategori;
    }

    public async Task<object> Guncelle(string id, KategoriGuncelleDto dto, AktifKullanici islem)
    {
        var mevcut = await Bul(id, islem.TenantId);

        if (dto.UstIdVerildi)
        {
            var yeniUst = string.IsNullOrWhiteSpace(dto.UstId) ? null : dto.UstId.Trim();
            if (yeniUst is not null)
            {
                await UstVarMi(yeniUst, islem.TenantId);
                await DonguDenetle(id, yeniUst, islem.TenantId);
            }
            mevcut.UstId = yeniUst;
        }
        if (dto.Ad is not null)
        {
            // PAKET PATCH-SOZLESME (02.10): null = KORU; boş dize eskiden adı "" yapıyordu.
            var ad = dto.Ad.Trim();
            if (ad.Length == 0) throw new BadRequestException("Kategori adı boş bırakılamaz").AlanIle("ad");
            mevcut.Ad = ad;
        }
        if (dto.Sira is not null) mevcut.Sira = dto.Sira.Value;
        if (dto.Aktif is not null)
        {
            // ── PAKET P-13 (B-40) — TEK KAPI ──────────────────────────────────
            // Pasife alma iki yoldan yapılabiliyordu: PATCH {aktif:false} ve
            // (yeni) DELETE. Kapı yalnız DELETE'e konsaydı PATCH sessiz arka
            // kapı olurdu; altında ürün/alt kategori duran dal görünmez olur ve
            // o ürünler ağaçta kaybolurdu. Kural TEK yerde, ikisi de buradan geçer.
            if (!dto.Aktif.Value && mevcut.Aktif) await PasifeAlmaKapisi(mevcut, islem.TenantId);
            if (dto.Aktif.Value != mevcut.Aktif)
                denetim?.YazmaKaydet(islem, nameof(UrunKategorisi), mevcut.Id,
                    dto.Aktif.Value ? "kategori aktife alındı" : "kategori pasife alındı");
            mevcut.Aktif = dto.Aktif.Value;
        }

        mevcut.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return mevcut;
    }

    /// <summary>
    /// DELETE /urun-kategorileri/{id} — SİLMEZ, pasife alır (PAKET P-13 · B-40).
    /// Hard DELETE yoktur: kategori ürün kartlarında referanslıdır, satır
    /// silinirse o ürünlerin kırılımı geçmişe dönük kaybolur.
    /// </summary>
    public async Task<object> PasifeAl(string id, AktifKullanici islem)
    {
        var kategori = await Bul(id, islem.TenantId);
        if (kategori.Aktif)
        {
            await PasifeAlmaKapisi(kategori, islem.TenantId);
            kategori.Aktif = false;
            kategori.Guncellendi = DateTime.UtcNow;
            denetim?.YazmaKaydet(islem, nameof(UrunKategorisi), kategori.Id, "kategori pasife alındı");
            await db.SaveChangesAsync();
        }
        return new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Kategori pasife alındı"), aktif = false };
    }

    /// <summary>
    /// PAKET P-13 (B-40) — bağlı kayıt sayıları. Ekran onay penceresinde bunu
    /// gösterir (UX-S23: etki kaydetmeden önce görünür).
    /// </summary>
    public async Task<object> BagliKayitlar(string id, AktifKullanici islem)
    {
        var kategori = await Bul(id, islem.TenantId);
        var (alt, urun) = await Sayilar(kategori.Id, islem.TenantId);
        return new Kuyumcu.Contracts.Sayfalar.UrunAgaci.KategoriBagliKayit(
            kategori.Id, kategori.Aktif, alt, urun);
    }

    private async Task<(int Alt, int Urun)> Sayilar(string kategoriId, string tenantId)
    {
        var alt = await db.Set<UrunKategorisi>().AsNoTracking()
            .CountAsync(k => k.TenantId == tenantId && k.UstId == kategoriId && k.Aktif);
        var urun = await db.Urunler.AsNoTracking()
            .CountAsync(u => u.TenantId == tenantId && u.KategoriId == kategoriId);
        return (alt, urun);
    }

    /// <summary>
    /// Bağlı kayıt kapısı — reyonunkiyle AYNI rejim (ConflictException + sayı).
    /// Ürün ya da aktif alt kategori varken pasife alınmaz: kullanıcı önce
    /// taşır. Sayı mesajda yazılıdır, "bir yerlerde var" denmez.
    /// </summary>
    private async Task PasifeAlmaKapisi(UrunKategorisi kategori, string tenantId)
    {
        var (alt, urun) = await Sayilar(kategori.Id, tenantId);
        if (urun > 0)
            throw new ConflictException(
                $"Bu kategoride {urun} ürün var; önce ürünler başka kategoriye taşınmalıdır");
        if (alt > 0)
            throw new ConflictException(
                $"Bu kategorinin {alt} aktif alt kategorisi var; önce alt kategoriler pasife alınmalıdır");
    }

    /// <summary>
    /// Bir kategorinin kendisi ve BÜTÜN alt kategorileri. Ürün listesi
    /// filtresinde kullanılır: "Ziynet" seçilince altındaki her şey gelir.
    /// </summary>
    public async Task<List<string>> AltAgacIdleri(string kategoriId, string tenantId)
    {
        var hepsi = await Hepsi(tenantId, sadeceAktif: false);
        var sonuc = new List<string>();
        var kuyruk = new Queue<string>([kategoriId]);
        var gorulen = new HashSet<string> { kategoriId };

        while (kuyruk.Count > 0)
        {
            var mevcut = kuyruk.Dequeue();
            sonuc.Add(mevcut);
            foreach (var c in hepsi.Where(k => k.UstId == mevcut))
                if (gorulen.Add(c.Id)) kuyruk.Enqueue(c.Id);   // döngüye karşı ikinci kalkan
        }
        return sonuc;
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private async Task<List<UrunKategorisi>> Hepsi(string tenantId, bool sadeceAktif)
    {
        var q = db.Set<UrunKategorisi>().AsNoTracking().Where(k => k.TenantId == tenantId);
        if (sadeceAktif) q = q.Where(k => k.Aktif);
        return await q.ToListAsync();
    }

    private static object Dugum(UrunKategorisi k, List<UrunKategorisi> hepsi,
                                Dictionary<string, int> urunSayilari) => new
    {
        id = k.Id,
        ad = k.Ad,
        sira = k.Sira,
        aktif = k.Aktif,
        urunSayisi = urunSayilari.GetValueOrDefault(k.Id),
        altlar = hepsi.Where(c => c.UstId == k.Id)
                      .OrderBy(c => c.Sira).ThenBy(c => c.Ad)
                      .Select(c => Dugum(c, hepsi, urunSayilari))
                      .ToList(),
    };

    private async Task<UrunKategorisi> Bul(string id, string tenantId) =>
        await db.Set<UrunKategorisi>()
            .FirstOrDefaultAsync(k => k.Id == id && k.TenantId == tenantId)
        ?? throw new NotFoundException("Ürün kategorisi bulunamadı");

    private async Task UstVarMi(string ustId, string tenantId)
    {
        var var_ = await db.Set<UrunKategorisi>().AsNoTracking()
            .AnyAsync(k => k.Id == ustId && k.TenantId == tenantId);
        if (!var_) throw new NotFoundException("Üst kategori bulunamadı");
    }

    /// <summary>
    /// Yeni üst, bu kategorinin kendisi ya da alt ağacında mı? Öyleyse döngü olur.
    /// </summary>
    private async Task DonguDenetle(string kategoriId, string yeniUstId, string tenantId)
    {
        if (kategoriId == yeniUstId)
            throw new BadRequestException("Bir kategori kendi üst kategorisi olamaz");

        var altAgac = await AltAgacIdleri(kategoriId, tenantId);
        if (altAgac.Contains(yeniUstId))
            throw new BadRequestException(
                "Bir kategori kendi alt kategorisinin altına taşınamaz — ağaçta döngü oluşur.");
    }
}

public sealed class ReyonServisi(KuyumcuDbContext db, DenetimServisi? denetim = null)
{
    public async Task<object> Listele(AktifKullanici islem, string? subeId)
    {
        var q = db.Set<Reyon>().AsNoTracking().Where(r => r.TenantId == islem.TenantId);
        if (!string.IsNullOrEmpty(subeId)) q = q.Where(r => r.SubeId == subeId);

        var reyonlar = await q.OrderBy(r => r.Ad).ToListAsync();
        var urunSayilari = await db.Urunler.AsNoTracking()
            .Where(u => u.TenantId == islem.TenantId && u.ReyonId != null)
            .GroupBy(u => u.ReyonId!)
            .Select(g => new { Id = g.Key, Adet = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Adet);

        // ⚠ PAKET 25 — İKİ SAYI FARKLI ŞEYDİR, İKİSİ DE GÖSTERİLİR:
        //   urunSayisi   = bu reyonu VARSAYILAN reyon olarak taşıyan ürün KARTI
        //                  sayısı (urunler.reyon_id). Adet bilgisi taşımaz.
        //   yerlesikAdet = bu reyonda FİİLEN duran mal adedi
        //                  (stok_yerlesimleri; GRAM üründe gram).
        // Birini diğerinin yerine koymak "3 kart var ama 17 adet mal var"
        // gerçeğini gizlerdi.
        var yerlesikler = await db.StokYerlesimleri.AsNoTracking()
            .Where(y => y.TenantId == islem.TenantId)
            .GroupBy(y => y.ReyonId)
            .Select(g => new { Id = g.Key, Adet = g.Sum(x => x.Adet) })
            .ToDictionaryAsync(x => x.Id, x => x.Adet);

        return reyonlar.Select(r => new
        {
            id = r.Id, subeId = r.SubeId, ad = r.Ad, tip = r.Tip,
            sorumluKullaniciId = r.SorumluKullaniciId, aktif = r.Aktif,
            urunSayisi = urunSayilari.GetValueOrDefault(r.Id),
            yerlesikAdet = yerlesikler.GetValueOrDefault(r.Id),
        }).ToList();
    }

    public async Task<object> Olustur(ReyonOlusturDto dto, AktifKullanici islem)
    {
        await IslemDogrulayici.SubeDogrula(db, dto.SubeId, islem.TenantId);

        if (!string.IsNullOrEmpty(dto.SorumluKullaniciId))
        {
            var gecerli = await db.Kullanicilar.AsNoTracking()
                .AnyAsync(k => k.Id == dto.SorumluKullaniciId && k.TenantId == islem.TenantId);
            if (!gecerli) throw new NotFoundException("Sorumlu kullanıcı bulunamadı");
        }

        var cakisan = await db.Set<Reyon>()
            .AnyAsync(r => r.TenantId == islem.TenantId && r.SubeId == dto.SubeId && r.Ad == dto.Ad.Trim());
        if (cakisan) throw new ConflictException($"'{dto.Ad}' adlı reyon bu şubede zaten var");

        var now = DateTime.UtcNow;
        var reyon = new Reyon
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            SubeId = dto.SubeId,
            Ad = dto.Ad.Trim(),
            Tip = dto.Tip,
            SorumluKullaniciId = dto.SorumluKullaniciId,
            Aktif = true,
            Olusturuldu = now,
            Guncellendi = now,
        };
        db.Set<Reyon>().Add(reyon);
        await db.SaveChangesAsync();
        return reyon;
    }

    /// <summary>
    /// Reyonu yerinde günceller (UX-1 Faz B — harita damgası UC_YOK →
    /// DUZENLENEBILIR). Gövdede olmayan alan KORUNUR.
    /// </summary>
    public async Task<object> Guncelle(string id, ReyonGuncelleDto dto, AktifKullanici islem)
    {
        // Mehmet K6: AÇIK kiracı koşulu (Find/FindAsync yasak, denetim.sh:49).
        var reyon = await db.Set<Reyon>()
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Reyon bulunamadı");

        var degisen = new List<string>();

        if (dto.Ad is not null)
        {
            // DataAnnotations yalnız controller yolunda koşar → elle kontrol.
            var ad = dto.Ad.Trim();
            if (ad.Length < 2) throw new BadRequestException("Reyon adı en az 2 karakter olmalıdır");
            if (ad != reyon.Ad)
            {
                var cakisan = await db.Set<Reyon>().AnyAsync(r =>
                    r.TenantId == islem.TenantId && r.SubeId == reyon.SubeId
                    && r.Ad == ad && r.Id != id);
                if (cakisan) throw new ConflictException($"'{ad}' adlı reyon bu şubede zaten var");
                reyon.Ad = ad;
                degisen.Add("ad");
            }
        }

        if (dto.Tip is not null)
        {
            if (!ReyonTipi.Hepsi.Contains(dto.Tip))
                throw new BadRequestException("Geçersiz reyon tipi: VITRIN, KASA, DEPO ya da CANTA");
            if (dto.Tip != reyon.Tip) { reyon.Tip = dto.Tip; degisen.Add("tip"); }
        }

        if (dto.SorumluKullaniciIdGonderildi)
        {
            var yeni = string.IsNullOrWhiteSpace(dto.SorumluKullaniciId)
                ? null : dto.SorumluKullaniciId.Trim();
            if (yeni is not null)
            {
                var gecerli = await db.Kullanicilar.AsNoTracking()
                    .AnyAsync(k => k.Id == yeni && k.TenantId == islem.TenantId);
                if (!gecerli) throw new NotFoundException("Sorumlu kullanıcı bulunamadı");
            }
            if (yeni != reyon.SorumluKullaniciId)
            {
                reyon.SorumluKullaniciId = yeni;
                degisen.Add("sorumlu_kullanici_id");
            }
        }

        if (dto.Aktif is { } yeniAktif && yeniAktif != reyon.Aktif)
        {
            // Pasife alma kapısı DELETE ile AYNI kuraldır — ikinci kural yazılmaz.
            if (!yeniAktif) await UrunKapisi(reyon, islem);
            reyon.Aktif = yeniAktif;
            degisen.Add(yeniAktif ? "aktif(açıldı)" : "aktif(pasife alındı)");
        }

        if (degisen.Count > 0)
        {
            reyon.Guncellendi = DateTime.UtcNow;
            denetim?.YazmaKaydet(islem, nameof(Reyon), reyon.Id,
                $"reyon güncellendi · değişen alanlar: {string.Join(", ", degisen)}", hassas: false);
            await db.SaveChangesAsync();
        }

        return Sunum(reyon, await BagliUrunSayisi(reyon.Id, islem.TenantId),
                     await YerlesikAdet(reyon.Id, islem.TenantId));
    }

    /// <summary>
    /// DELETE /reyonlar/{id} — SİLMEZ, pasife alır (UX-S25 · Mehmet: kayıt
    /// kaybolmaz, geri dönüş yolu PATCH aktif=true'dur). Üzerinde ürün varsa 409.
    /// </summary>
    public async Task<object> PasifeAl(string id, AktifKullanici islem)
    {
        var reyon = await db.Set<Reyon>()
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Reyon bulunamadı");

        if (reyon.Aktif)
        {
            await UrunKapisi(reyon, islem);
            reyon.Aktif = false;
            reyon.Guncellendi = DateTime.UtcNow;
            denetim?.YazmaKaydet(islem, nameof(Reyon), reyon.Id, "reyon pasife alındı", hassas: false);
            await db.SaveChangesAsync();
        }
        return new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Reyon pasife alındı"), aktif = false };
    }

    private async Task UrunKapisi(Reyon reyon, AktifKullanici islem)
    {
        var adet = await BagliUrunSayisi(reyon.Id, islem.TenantId);
        if (adet > 0)
            throw new ConflictException(
                $"Bu reyonda {adet} ürün var; önce ürünler başka reyona taşınmalıdır");

        // ── PAKET 25 — YERLEŞİM KAPISI ──────────────────────────────────────
        // Kart bağı olmasa bile reyonda FİİLEN mal duruyor olabilir. Pasife
        // alınırsa o mal görünmez bir reyonda kalır ve sayımda "kayıp" sayılır.
        var yerlesik = await db.StokYerlesimleri.AsNoTracking()
            .Where(y => y.TenantId == islem.TenantId && y.ReyonId == reyon.Id)
            .SumAsync(y => (decimal?)y.Adet) ?? 0m;
        if (yerlesik > 0m)
            throw new BadRequestException(
                $"Bu reyonda {yerlesik:0.###} adet mal duruyor; önce malı boşaltın "
                + "(taşıma ya da geri alma ucuyla).");
    }

    private Task<int> BagliUrunSayisi(string reyonId, string tenantId) =>
        db.Urunler.AsNoTracking().CountAsync(u => u.TenantId == tenantId && u.ReyonId == reyonId);

    private Task<decimal> YerlesikAdet(string reyonId, string tenantId) =>
        db.StokYerlesimleri.AsNoTracking()
            .Where(y => y.TenantId == tenantId && y.ReyonId == reyonId)
            .SumAsync(y => y.Adet);

    private static object Sunum(Reyon r, int urunSayisi, decimal yerlesikAdet) => new
    {
        id = r.Id, subeId = r.SubeId, ad = r.Ad, tip = r.Tip,
        sorumluKullaniciId = r.SorumluKullaniciId, aktif = r.Aktif, urunSayisi, yerlesikAdet,
    };
}

// ═════════════════════════════════════════════════════════════════════════════

[ApiController]
[Route("api/v1/urun-kategorileri")]
[Authorize]
[Yetki.ModulYetkisi("urun")]
public sealed class KategoriController(KategoriServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/urun-kategorileri/duz — Düz liste (kampanya hedefi seçimi için)</summary>
    [HttpGet("duz")]
    public Task<object> Duz() => servis.Duz(Islem);

    /// <summary>GET /api/v1/urun-kategorileri — Kategori ağacı (iç içe)</summary>
    [HttpGet]
    public Task<object> Agac([FromQuery] bool sadeceAktif = true) => servis.Agac(Islem, sadeceAktif);

    /// <summary>POST /api/v1/urun-kategorileri — Kategori ekle — MUHASEBE+</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public async Task<IActionResult> Olustur([FromBody] KategoriOlusturDto dto) =>
        StatusCode(StatusCodes.Status201Created, await servis.Olustur(dto, Islem));

    /// <summary>PATCH /api/v1/urun-kategorileri/{id} — Kategori güncelle — MUHASEBE+</summary>
    /// <summary>GET /api/v1/urun-kategorileri/{id}/bagli-kayitlar — P-13 · B-40</summary>
    [HttpGet("{id}/bagli-kayitlar")]
    public Task<object> BagliKayitlar(string id) => servis.BagliKayitlar(id, Islem);

    /// <summary>DELETE /api/v1/urun-kategorileri/{id} — SİLMEZ, pasife alır — MUDUR+</summary>
    [HttpDelete("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> PasifeAl(string id) => servis.PasifeAl(id, Islem);

    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Guncelle(string id, [FromBody] KategoriGuncelleDto dto) =>
        servis.Guncelle(id, dto, Islem);
}

[ApiController]
[Route("api/v1/reyonlar")]
[Authorize]
[Yetki.ModulYetkisi("urun")]
public sealed class ReyonController(ReyonServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/reyonlar — Reyon listesi</summary>
    [HttpGet]
    public Task<object> Listele([FromQuery] string? subeId) => servis.Listele(Islem, subeId);

    /// <summary>POST /api/v1/reyonlar — Reyon ekle — MUDUR+</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR")]
    public async Task<IActionResult> Olustur([FromBody] ReyonOlusturDto dto) =>
        StatusCode(StatusCodes.Status201Created, await servis.Olustur(dto, Islem));

    /// <summary>PATCH /api/v1/reyonlar/{id} — Reyon güncelle — MUDUR+</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Guncelle(string id, [FromBody] ReyonGuncelleDto dto) =>
        servis.Guncelle(id, dto, Islem);

    /// <summary>DELETE /api/v1/reyonlar/{id} — Reyonu PASİFE ALIR (silmez) — MUDUR+</summary>
    [HttpDelete("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> PasifeAl(string id) => servis.PasifeAl(id, Islem);
}

