using System.Text.Json;
using System.Text.Json.Nodes;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IrsaliyeVarligi = Kuyumcu.Api.Data.Irsaliye;

namespace Kuyumcu.Api.Modules.Satinalma;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-26 (Y-34) — SATINALMA SİPARİŞİ (PO) + MAL KABUL.
//
// ⚠ TEK YAZMA YOLU: mal kabul stoğa ve deftere KENDİ yazmaz. Mevcut
//   `SatisServisi.Alis` çağrılır — HareketFabrikasi.Ekle, maliyet partisi,
//   yerleşim (reyon), MASAK kapısı, tevsik ve cari borcu ORADADIR. İkinci bir
//   stok aritmetiği yazılsaydı aynı malın iki farklı maliyeti olurdu.
//   Bu dosyada `Hareketler.Add`, `Stoklar.Add`, kasa/cari bakiye dokunuşu YOKTUR.
//
// ⚠ ATOMİKLİK SINIRI (dürüst beyan, Anayasa md.1): `SatisServisi.Alis` KENDİ
//   transaction'ını açıp COMMIT eder. Bu yüzden mal kabul şöyle sıralanır:
//     (1) irsaliye başlığı + kalemleri ÖNCE yazılır (hareket_id NULL),
//     (2) her kalem için Alis çağrılır,
//     (3) doğan hareket kimliği kaleme yazılır ve kabul adedi artar.
//   Süreç ortada kesilirse ortaya "hareketi bağlanmamış kalem" çıkar ve bu
//   /satinalma/bagsiz-kalemler ucunda GÖRÜNÜR — sessiz kayıp olmaz.
//
// ⚠ ÖDEME BURADA YAPILMAZ: mal kabul tedarikçiye BORÇ yazar (kasa bacağı yok).
//   Ödeme mevcut cari/kasa uçlarından yürür; ikinci bir ödeme yolu açmak
//   tevsik ve MASAK kümülatifini ikizlerdi.
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class SatinalmaKalemDto
{
    public string? UrunId { get; set; }
    public string? Tanim { get; set; }
    public decimal? Adet { get; set; }
    public decimal? BirimFiyatTl { get; set; }
}

public sealed class SatinalmaOlusturDto
{
    public string? TedarikciCariId { get; set; }
    public string? SubeId { get; set; }
    public DateOnly? BeklenenTarih { get; set; }
    public string? Aciklama { get; set; }

    /// <summary>Muhasebe hesap kodu — BOŞ BIRAKILABİLİR, uydurulmaz (Y-37).</summary>
    public string? HesapKodu { get; set; }

    public List<SatinalmaKalemDto>? Kalemler { get; set; }
}

public sealed class MalKabulKalemDto
{
    public string? KalemId { get; set; }
    public decimal? Adet { get; set; }

    /// <summary>Fiili birim fiyat. Boşsa siparişteki anlaşılan fiyat kullanılır.</summary>
    public decimal? BirimFiyatTl { get; set; }
}

public sealed class MalKabulDto
{
    public string? KurSnapshotId { get; set; }
    public string? Aciklama { get; set; }
    public List<MalKabulKalemDto>? Kalemler { get; set; }

    /// <summary>
    /// PAKET EKSIK-B1-SATINALMA tur 3 (hasım KRİTİK): MASAK beyanı — eşik altında boş. Tedbirler Yön. md.5/1-b
    /// kimlik tespitini işlem tutarına (ya da bağlantılı işlemler toplamına) bağlar, ödeme şekline bağlamaz;
    /// md.5/2 "işlem yapılmadan önce tamamlanır". Bu yüzden toptan mal kabul de beyanı taşır ve her kalemin
    /// alışına AYNEN geçirilir (kural motoru SatisServisi.Alis içinde; burada kural YAZILMAZ).
    /// </summary>
    public Kuyumcu.Api.Modules.Masak.MasakBilgiDto? Masak { get; set; }
}

public sealed class TedarikGerekceDto
{
    public string? Gerekce { get; set; }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class SatinalmaServisi(
    KuyumcuDbContext db,
    DenetimServisi denetim,
    Satis.SatisServisi satis,
    ILogger<SatinalmaServisi> logger)
{
    private const string Varlik = nameof(SatinalmaSiparisi);

    /// <summary>Yazma/iptal yalnız bu roller (tezgâhtar tedarik siparişi açamaz).</summary>
    public static readonly string[] YazmaRolleri = ["SAHIP", "MUDUR"];

    // ─── Oluştur ─────────────────────────────────────────────────────────────

    public async Task<object> Olustur(SatinalmaOlusturDto dto, AktifKullanici islem)
    {
        if (string.IsNullOrWhiteSpace(dto.TedarikciCariId))
            throw new BadRequestException("Tedarikçi cari seçimi zorunlu");

        var kalemler = dto.Kalemler ?? [];
        if (kalemler.Count == 0)
            throw new BadRequestException("Satınalma siparişi en az bir kalem içermeli");
        if (kalemler.Count > SatinalmaSiparisi.AzamiKalem)
            throw new BadRequestException(
                $"Bir siparişte en fazla {SatinalmaSiparisi.AzamiKalem} kalem olabilir "
                + $"(gönderilen {kalemler.Count}). Kalanı için ikinci sipariş açın.");

        await IslemDogrulayici.CariDogrula(db, dto.TedarikciCariId, islem.TenantId);
        BeklenenTarihDenetle(dto.BeklenenTarih);

        var subeId = string.IsNullOrWhiteSpace(dto.SubeId) ? islem.SubeId : dto.SubeId;
        if (string.IsNullOrWhiteSpace(subeId))
            throw new BadRequestException(
                "Şube seçimi zorunlu: mal kabul stoğa ŞUBE bazında girer, şubesiz sipariş kabul edilemez");
        await IslemDogrulayici.SubeDogrula(db, subeId, islem.TenantId);

        var hazir = new List<SatinalmaKalemi>();
        for (var i = 0; i < kalemler.Count; i++)
            hazir.Add(await KalemKur(kalemler[i], i, islem.TenantId));

        var now = DateTime.UtcNow;
        var no = await TedarikNo.Uret(db, islem.TenantId, TedarikNo.SatinalmaOnek, Zaman.YerelBugun().Year);

        for (var deneme = 1; ; deneme++)
        {
            var id = Kimlik.Ulid();
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                db.SatinalmaSiparisleri.Add(new SatinalmaSiparisi
                {
                    Id = id,
                    TenantId = islem.TenantId,
                    SubeId = subeId!,
                    No = no,
                    TedarikciCariId = dto.TedarikciCariId!,
                    Durum = SatinalmaDurumu.Taslak,
                    BeklenenTarih = dto.BeklenenTarih,
                    Aciklama = Kirp(dto.Aciklama, Kuyumcu.Core.Domain.Girdi.AlanSinirlari.TedarikAciklama),
                    HesapKodu = Kirp(dto.HesapKodu, Kuyumcu.Core.Domain.Girdi.AlanSinirlari.HesapKodu, "hesapKodu"),
                    OlusturanKullaniciId = islem.Id,
                    Olusturuldu = now,
                    Guncellendi = now,
                });

                foreach (var k in hazir)
                    db.SatinalmaKalemleri.Add(new SatinalmaKalemi
                    {
                        Id = Kimlik.Ulid(),
                        TenantId = islem.TenantId,
                        SatinalmaSiparisId = id,
                        UrunId = k.UrunId,
                        Tanim = k.Tanim,
                        Adet = k.Adet,
                        BirimFiyatTl = k.BirimFiyatTl,
                        KabulEdilenAdet = 0m,
                        Sira = k.Sira,
                        Olusturuldu = now,
                    });

                denetim.YazmaKaydet(islem, Varlik, id,
                    $"satınalma siparişi {no} açıldı · {hazir.Count} kalem");

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                logger.LogInformation("[{TenantId}] SATINALMA açıldı: {No} ({Kalem} kalem)",
                    islem.TenantId, no, hazir.Count);
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
                throw new ConflictException(
                    $"Satınalma numarası üretilemedi ({TedarikNo.AzamiDeneme} deneme). Lütfen tekrar deneyin.");
            }
        }
    }

    // ─── Liste ───────────────────────────────────────────────────────────────

    public async Task<object> Listele(
        string? durum, string? cariId, string? subeId, int? limit, AktifKullanici islem)
    {
        if (durum is not null && !SatinalmaDurumu.Gecerli(durum))
            throw new BadRequestException($"Geçersiz satınalma durumu: {durum}");

        var istenen = limit ?? SatinalmaSiparisi.VarsayilanLimit;
        var etkinLimit = Math.Clamp(istenen, 1, SatinalmaSiparisi.AzamiLimit);

        var sorgu = db.SatinalmaSiparisleri.AsNoTracking().Where(s => s.TenantId == islem.TenantId);
        if (durum is not null) sorgu = sorgu.Where(s => s.Durum == durum);
        if (!string.IsNullOrWhiteSpace(cariId)) sorgu = sorgu.Where(s => s.TedarikciCariId == cariId);
        if (!string.IsNullOrWhiteSpace(subeId)) sorgu = sorgu.Where(s => s.SubeId == subeId);

        var toplam = await sorgu.CountAsync();
        var kirilim = await sorgu.GroupBy(s => s.Durum)
            .Select(g => new { durum = g.Key, adet = g.Count() }).ToListAsync();

        var satirlar = await sorgu
            .OrderByDescending(s => s.Olusturuldu).Take(etkinLimit)
            .Select(s => new
            {
                s.Id, s.No, s.Durum, s.SubeId, s.TedarikciCariId, s.BeklenenTarih,
                s.Aciklama, s.HesapKodu, s.Olusturuldu, s.Guncellendi,
                kalemSayisi = db.SatinalmaKalemleri.Count(k => k.SatinalmaSiparisId == s.Id),
                // PAKET EKSIK-B1-SATINALMA (UX-S21): ad sunucudan — ekran kimlik basmaz, ikinci istek atmaz.
                tedarikciAdi = db.Cariler.Where(c => c.Id == s.TedarikciCariId && c.TenantId == s.TenantId)
                    .Select(c => c.Ad).FirstOrDefault(),
            })
            .ToListAsync();

        await denetim.ListeOkumaKaydet(islem, Varlik, satirlar.Count, "satınalma listesi");

        // UX-S29: sunucu sınırı EKRANDA söylenir; sessiz kırpma yok.
        return new
        {
            satirlar, toplam, kirilim,
            limit = etkinLimit,
            azamiLimit = SatinalmaSiparisi.AzamiLimit,
            sinirDegdi = satirlar.Count >= etkinLimit && toplam > satirlar.Count,
        };
    }

    // ─── Detay ───────────────────────────────────────────────────────────────

    public async Task<object> Detay(string id, AktifKullanici islem)
    {
        var s = await Bul(id, islem.TenantId);
        var kalemler = await db.SatinalmaKalemleri.AsNoTracking()
            .Where(k => k.SatinalmaSiparisId == id && k.TenantId == islem.TenantId)
            .OrderBy(k => k.Sira)
            .Select(k => new
            {
                k.Id, k.UrunId, k.Tanim, k.Adet, k.BirimFiyatTl, k.KabulEdilenAdet,
                kalanAdet = k.Adet - k.KabulEdilenAdet, k.Sira,
                // PAKET EKSIK-B1-SATINALMA (UX-S21): ad sunucudan — ekran kimlik basmaz, ikinci istek atmaz.
                urunAdi = db.Urunler.Where(u => u.Id == k.UrunId && u.TenantId == k.TenantId).Select(u => u.Ad).FirstOrDefault(),
                birim = db.Urunler.Where(u => u.Id == k.UrunId && u.TenantId == k.TenantId).Select(u => u.Birim).FirstOrDefault(),
            })
            .Take(SatinalmaSiparisi.AzamiKalem).ToListAsync();

        var irsaliyeler = await db.Irsaliyeler.AsNoTracking()
            .Where(i => i.SatinalmaSiparisId == id && i.TenantId == islem.TenantId)
            .OrderBy(i => i.Tarih)
            .Select(i => new { i.Id, i.No, i.Tarih, i.Durum, i.FaturaId })
            .Take(Data.Irsaliye.AzamiLimit).ToListAsync();

        var tedarikciAdi = await db.Cariler.AsNoTracking()   // PAKET EKSIK-B1-SATINALMA (UX-S21): ad sunucudan — ekran kimlik basmaz, ikinci istek atmaz.
            .Where(c => c.Id == s.TedarikciCariId && c.TenantId == islem.TenantId).Select(c => c.Ad).FirstOrDefaultAsync();
        return new
        {
            s.Id, s.No, s.Durum, s.SubeId, s.TedarikciCariId, tedarikciAdi, s.BeklenenTarih,
            s.Aciklama, s.HesapKodu, s.IptalGerekce, s.Olusturuldu, s.Guncellendi,
            kalemler, irsaliyeler,
            // UX-S30: kilitli düğmenin sebebi SUNUCUDAN gelir; ekran kural uydurmaz.
            duzenlenebilir = s.Durum == SatinalmaDurumu.Taslak,
            malKabulEdilebilir = SatinalmaDurumu.KabulEdilebilir(s.Durum),
            kilitSebebi = s.Durum switch
            {
                SatinalmaDurumu.Taslak => null,
                SatinalmaDurumu.Iptal => "Sipariş iptal edildi; düzenlenemez. «Siparişi aktife al» ile taslağa döndürün ya da yeni sipariş açın.",
                SatinalmaDurumu.Kapandi => "Sipariş kapandı (tüm kalemler kabul edildi).",
                _ => "Onaylanmış siparişin kalemleri değiştirilemez; kalan kalem mal kabulüyle kapanır.",
            },
        };
    }

    // ─── PATCH ───────────────────────────────────────────────────────────────

    /// <summary>
    /// PATCH sözleşmesi: <b>gövdede YOKSA korunur, null ise SİLİNİR</b>.
    /// Yalnız TASLAK sipariş düzenlenir — onaylanmış siparişin kalemi değişirse
    /// tedarikçiye verilen söz ile kayıt ayrışırdı.
    /// </summary>
    public async Task<object> Guncelle(string id, JsonElement govde, AktifKullanici islem)
    {
        var s = await Bul(id, islem.TenantId);
        if (s.Durum != SatinalmaDurumu.Taslak)
            throw new ConflictException(
                "Yalnız TASLAK satınalma siparişi düzenlenebilir. Onaylanmış sipariş için "
                + "kalan kalemi mal kabulüyle kapatın ya da siparişi iptal edip yenisini açın.");

        var degisenler = new List<string>();

        if (Alan(govde, "guncellendi", out var damga) && damga.ValueKind != JsonValueKind.Null
            && damga.TryGetDateTime(out var gonderilen)
            && Math.Abs((gonderilen - s.Guncellendi).TotalSeconds) > 1)
            throw new ConflictException(
                "Bu sipariş siz bakarken değişti. Sayfayı yenileyip tekrar deneyin.");

        if (Alan(govde, "aciklama", out var acikOge))
        {
            var yeni = acikOge.ValueKind == JsonValueKind.Null ? null : Kirp(acikOge.GetString(), Kuyumcu.Core.Domain.Girdi.AlanSinirlari.TedarikAciklama);
            if (yeni != s.Aciklama) degisenler.Add("açıklama");
            s.Aciklama = yeni;
        }

        if (Alan(govde, "hesapKodu", out var hkOge))
        {
            var yeni = hkOge.ValueKind == JsonValueKind.Null ? null : Kirp(hkOge.GetString(), Kuyumcu.Core.Domain.Girdi.AlanSinirlari.HesapKodu, "hesapKodu");
            if (yeni != s.HesapKodu) degisenler.Add("hesap kodu");
            s.HesapKodu = yeni;
        }

        if (Alan(govde, "beklenenTarih", out var btOge))
        {
            DateOnly? yeni = btOge.ValueKind == JsonValueKind.Null
                ? null
                : DateOnly.Parse(btOge.GetString() ?? "", System.Globalization.CultureInfo.InvariantCulture);
            if (yeni != s.BeklenenTarih) BeklenenTarihDenetle(yeni);
            if (yeni != s.BeklenenTarih) degisenler.Add("beklenen tarih");
            s.BeklenenTarih = yeni;
        }

        if (Alan(govde, "tedarikciCariId", out var cariOge))
        {
            if (cariOge.ValueKind == JsonValueKind.Null || string.IsNullOrWhiteSpace(cariOge.GetString()))
                throw new BadRequestException(
                    "Tedarikçi boş bırakılamaz: siparişin kime verildiği belgenin zorunlu unsurudur. "
                    + "Yanlış tedarikçi seçildiyse doğrusunu yazın.");
            var yeniCari = cariOge.GetString()!;
            await IslemDogrulayici.CariDogrula(db, yeniCari, islem.TenantId);
            if (yeniCari != s.TedarikciCariId) degisenler.Add("tedarikçi");
            s.TedarikciCariId = yeniCari;
        }

        if (Alan(govde, "kalemler", out var kalemOge) && kalemOge.ValueKind == JsonValueKind.Array)
        {
            var yeniler = KolonTavani.JsonListe<SatinalmaKalemDto>(kalemOge, "kalemler", "Kalemler");   // tur 3
            if (yeniler.Count == 0)
                throw new BadRequestException("Sipariş en az bir kalem içermeli");
            if (yeniler.Count > SatinalmaSiparisi.AzamiKalem)
                throw new BadRequestException(
                    $"Bir siparişte en fazla {SatinalmaSiparisi.AzamiKalem} kalem olabilir.");

            var hazir = new List<SatinalmaKalemi>();
            for (var i = 0; i < yeniler.Count; i++)
                hazir.Add(await KalemKur(yeniler[i], i, islem.TenantId));

            var eskiler = await db.SatinalmaKalemleri
                .Where(k => k.SatinalmaSiparisId == id && k.TenantId == islem.TenantId)
                .Take(SatinalmaSiparisi.AzamiKalem).ToListAsync();
            // TASLAK'ta hiçbir kalem kabul edilmiş olamaz (kabul ONAYLANDI ister),
            // bu yüzden sil+yaz veri imha etmez — yine de fail-closed doğrulanır.
            if (eskiler.Any(k => k.KabulEdilenAdet > 0m))
                throw new ConflictException("Mal kabulü başlamış kalemler değiştirilemez.");
            db.SatinalmaKalemleri.RemoveRange(eskiler);

            var now2 = DateTime.UtcNow;
            foreach (var k in hazir)
                db.SatinalmaKalemleri.Add(new SatinalmaKalemi
                {
                    Id = Kimlik.Ulid(),
                    TenantId = islem.TenantId,
                    SatinalmaSiparisId = id,
                    UrunId = k.UrunId, Tanim = k.Tanim, Adet = k.Adet,
                    BirimFiyatTl = k.BirimFiyatTl, KabulEdilenAdet = 0m,
                    Sira = k.Sira, Olusturuldu = now2,
                });
            degisenler.Add("kalemler");
        }

        if (degisenler.Count == 0)
            throw new BadRequestException(
                "Düzenlenebilir alan gönderilmedi (açıklama, hesap kodu, beklenen tarih, tedarikçi, kalemler).");

        s.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, Varlik, s.Id,
            $"satınalma siparişi {s.No} düzenlendi · değişen: {string.Join(", ", degisenler)}");
        await db.SaveChangesAsync();
        return await Detay(id, islem);
    }

    // ─── Onayla / İptal ──────────────────────────────────────────────────────

    public async Task<object> Onayla(string id, AktifKullanici islem)
    {
        var s = await Bul(id, islem.TenantId);
        if (s.Durum != SatinalmaDurumu.Taslak)
            throw new ConflictException($"Yalnız TASLAK sipariş onaylanabilir (şu an: {s.Durum}).");

        s.Durum = SatinalmaDurumu.Onaylandi;
        s.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, Varlik, s.Id, $"satınalma siparişi {s.No} onaylandı");
        await db.SaveChangesAsync();
        return await Detay(id, islem);
    }

    /// <summary>
    /// İPTAL = pasifleştirme. Hard DELETE ucu YOKTUR (finansal belge zinciri).
    /// Geri dönüş yolu: yeni sipariş açmak (iptal edilen numara korunur).
    /// </summary>
    public async Task<object> Iptal(string id, string? gerekce, AktifKullanici islem)
    {
        var s = await Bul(id, islem.TenantId);
        if (s.Durum == SatinalmaDurumu.Iptal)
            throw new ConflictException("Sipariş zaten iptal edilmiş.");

        var temiz = (gerekce ?? "").Trim();
        if (temiz.Length < SatinalmaSiparisi.GerekceAsgari)
            throw new BadRequestException(
                $"İptal gerekçesi en az {SatinalmaSiparisi.GerekceAsgari} karakter olmalı "
                + $"(şu an {temiz.Length}).");

        var kabulEdilen = await db.SatinalmaKalemleri.AsNoTracking()
            .Where(k => k.SatinalmaSiparisId == id && k.TenantId == islem.TenantId)
            .SumAsync(k => (decimal?)k.KabulEdilenAdet) ?? 0m;
        if (kabulEdilen > 0m)
            throw new ConflictException(
                "Mal kabulü yapılmış sipariş iptal edilemez: deftere alış yazıldı. "
                + "Yanlış giren mal için ALIŞ İADESİ ya da storno yolunu kullanın.");

        s.Durum = SatinalmaDurumu.Iptal;
        s.IptalGerekce = Kirp(temiz, Kuyumcu.Core.Domain.Girdi.AlanSinirlari.TedarikAciklama, "gerekce");
        s.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, Varlik, s.Id, $"satınalma siparişi {s.No} iptal edildi · {temiz}");
        await db.SaveChangesAsync();
        return await Detay(id, islem);
    }

    /// <summary>
    /// PAKET SATINALMA-EKRAN — İPTALDEN GERİ DÖNÜŞ. Yalnız İPTAL sipariş TASLAK'a döner (önceki durum
    /// kayıtlı olmadığı için tutucu: kullanıcı yeniden onaylar). İptal yalnız kabulü olmayan siparişte
    /// yapılabildiğinden defter/stok etkisi YOKTUR. Gerekçe alanı temizlenir; geçmiş denetim kaydındadır.
    /// </summary>
    public async Task<object> AktifeAl(string id, AktifKullanici islem)
    {
        var s = await Bul(id, islem.TenantId);
        if (s.Durum != SatinalmaDurumu.Iptal)
            throw new ConflictException($"Yalnız iptal edilmiş sipariş aktife alınabilir (şu an: {s.Durum}).");

        s.Durum = SatinalmaDurumu.Taslak;
        s.IptalGerekce = null;
        s.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, Varlik, s.Id, $"satınalma siparişi {s.No} iptalden aktife alındı (taslak)");
        await db.SaveChangesAsync();
        return await Detay(id, islem);
    }

    // ─── Mal kabul ───────────────────────────────────────────────────────────

    /// <summary>
    /// Y-34 — MAL KABUL. Stoğa <b>SatisServisi.Alis</b> üzerinden yazar
    /// (HareketFabrikasi tek yazma yolu) ve GELEN irsaliye üretir.
    /// </summary>
    public async Task<object> MalKabul(string id, MalKabulDto dto, AktifKullanici islem)
    {
        var s = await Bul(id, islem.TenantId);
        if (!SatinalmaDurumu.KabulEdilebilir(s.Durum))
            throw new ConflictException(
                $"Mal kabul yalnız ONAYLANDI/KISMI_KABUL siparişte yapılır (şu an: {s.Durum}).");

        if (string.IsNullOrWhiteSpace(dto.KurSnapshotId))
            throw new BadRequestException("Kur kaydı zorunlu: alış o günün has fiyatına bağlanır");

        // Alış, işlemi yapanın AKTİF şubesine yazar (SatisServisi.SubeBelirle).
        // Siparişin şubesi farklıysa mal yanlış şubeye girerdi — fail-closed.
        if (!string.IsNullOrWhiteSpace(islem.SubeId) && islem.SubeId != s.SubeId)
            throw new BadRequestException(
                "Mal kabul, siparişin şubesinde yapılır. Aktif şubeniz siparişin şubesinden farklı — "
                + "şube değiştirip tekrar deneyin.");

        var istekler = dto.Kalemler ?? [];
        if (istekler.Count == 0)
            throw new BadRequestException("Kabul edilecek en az bir kalem gönderilmeli");

        // PAKET EKSIK-B1-SATINALMA: AsNoTracking — izlenen (tracked) kopya kilitten SONRA
        // yeniden okunduğunda kimlik haritası ESKİ kabul adedini döndürür, artış eski değere
        // yazılırdı (kayıp güncelleme → çift kabul). Yazılacak satır aşağıda kilit sonrası okunur.
        var kalemler = await db.SatinalmaKalemleri.AsNoTracking()
            .Where(k => k.SatinalmaSiparisId == id && k.TenantId == islem.TenantId)
            .Take(SatinalmaSiparisi.AzamiKalem).ToListAsync();

        // PAKET EKSIK-B1-SATINALMA (hasım: çift kabul): aynı kalem tek istekte iki kez
        // gönderilirse her satır tek başına sınırı geçer ama toplamı aşardı → 400.
        if (istekler.Select(x => x.KalemId).Distinct(StringComparer.Ordinal).Count() != istekler.Count)
            throw new BadRequestException("Aynı sipariş kalemi tek mal kabulde bir kez yazılır.").AlanIle("kalemler");

        // ── Doğrulama tümüyle ÖNCE: yarım kabul yazmamak için ────────────────
        var plan = new List<(SatinalmaKalemi Kalem, decimal Adet, decimal Fiyat)>();
        foreach (var istek in istekler)
        {
            var k = kalemler.FirstOrDefault(x => x.Id == istek.KalemId)
                ?? throw new NotFoundException($"Sipariş kalemi bulunamadı: {istek.KalemId}");
            // PAKET PATCH-2: önce kolon hanesine (decimal(12,3)) yuvarla, sonra sıfır kontrolü — 0,0004 kontrolden geçip 0,000 yazılmaz.
            var adet = Math.Round(istek.Adet ?? 0m, 3, MidpointRounding.AwayFromZero);
            if (adet <= 0m)
                throw new BadRequestException($"Kabul adedi sıfırdan büyük olmalı (kalem {k.Sira + 1}).").AlanIle("kalemler");
            if (k.KabulEdilenAdet + adet > k.Adet + 0.0005m)
                throw new BadRequestException(
                    $"Kabul adedi sipariş adedini aşıyor (kalem {k.Sira + 1}: sipariş {k.Adet}, "
                    + $"kabul edilen {k.KabulEdilenAdet}, istenen {adet}). Fazla mal için yeni sipariş açın.");
            if (string.IsNullOrWhiteSpace(k.UrunId))
                throw new BadRequestException(
                    $"Kalem {k.Sira + 1} bir stok ürününe bağlı değil ({k.Tanim}). Mal kabul için önce "
                    + "ürünü tanımlayıp kalemi ürüne bağlayın — stok girişi ürünsüz yapılamaz.");
            // PAKET EKSIK-B1-SATINALMA (hasım: ADET'e kesir): ADET birimli ürüne 1,5 adet girmez.
            // Alis de reddederdi; burada ÖNCE ve kalem sırasıyla söylenir (irsaliye hiç açılmaz).
            var birim = await db.Urunler.AsNoTracking()
                .Where(u => u.Id == k.UrunId && u.TenantId == islem.TenantId)
                .Select(u => u.Birim).FirstOrDefaultAsync();
            Urun.UrunAdetKapisi.Dogrula(birim, adet, "kalemler", $"Kalem {k.Sira + 1}");

            var fiyat = istek.BirimFiyatTl ?? k.BirimFiyatTl
                ?? throw new BadRequestException(
                    $"Kalem {k.Sira + 1} için birim fiyat yok. Maliyeti bilinmeyen mal stoğa giremez "
                    + "(kâr-zarar yanlış çıkar); fiyatı girin.");
            if (fiyat < 0m) throw new BadRequestException("Birim fiyat negatif olamaz");
            plan.Add((k, adet, fiyat));
        }

        var now = DateTime.UtcNow;

        // ── (1) İrsaliye başlığı + kalemleri ÖNCE (hareket_id NULL) ──────────
        var irsaliyeId = Kimlik.Ulid();
        var irsNo = await TedarikNo.Uret(db, islem.TenantId, TedarikNo.IrsaliyeOnek, Zaman.YerelBugun().Year);
        var irsKalemler = new List<IrsaliyeKalemi>();
        // ── PAKET URUN-ACILIS-ATOMIK — mal kabul TEK DB İŞLEMİ ────────────────
        // KÖK NEDEN: irsaliye başlığı+kalemleri burada commit ediliyor, kalem
        // alışları (SatisServisi.Alis) ardından ayrı yazılıyordu. Bir alış
        // reddedince (gün kilidi, MASAK, kur…) irsaliye ve önceki kalemlerin
        // stok/hareketleri KALIYOR, kalem 'bağsız' düşüyordu. Şimdi irsaliye
        // denemesinin işlemi AÇIK tutulur; Alis bu işleme katılır (CurrentTransaction
        // devralma) ve en sonda TEK commit yapılır. Red = hiçbir satır kalmaz.
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? anaTx = null;
        for (var deneme = 1; ; deneme++)
        {
            // PAKET MASAK-ESZAMAN-2: READ COMMITTED — Alis bu işlemi devralır ve MASAK/tevsik kümülatifini
            // cari kilidinden SONRA yeniden okur; REPEATABLE READ'de anlık görüntü kilitten önceki Faz 1
            // okumasında kurulur, eşzamanlı aynı tedarikçi alışında tazelik denetimi işlemi 500 ile
            // keserdi (MASAK-ESZAMAN § 9 md.1). RC'de kilit sonrası okuma son commit'i görür.
            var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
            try
            {
                // PAKET EKSIK-B1-SATINALMA (hasım: çift kabul / eşzamanlı iki istek): sipariş satırı
                // İLK deyimde kilitlenir (ikinci istek burada BEKLER); durum ve kabul adetleri
                // kilitten SONRA yeniden okunur → ikinci istek kalanı aşıyorsa 409, 0 satır.
                await db.Database.Kilitle(KilitTablosu.SatinalmaSiparisleri, id, islem.TenantId);
                await KilitSonrasiDenetle(id, plan.Select(p => (p.Kalem.Id, p.Adet)).ToList(), islem.TenantId);

                db.Irsaliyeler.Add(new IrsaliyeVarligi
                {
                    Id = irsaliyeId,
                    TenantId = islem.TenantId,
                    SubeId = s.SubeId,
                    No = irsNo,
                    Yon = IrsaliyeYonu.Gelen,
                    CariId = s.TedarikciCariId,
                    Tarih = now,
                    SatinalmaSiparisId = s.Id,
                    FaturaId = null,
                    Durum = IrsaliyeDurumu.Acik,
                    Aciklama = Kirp(dto.Aciklama, Kuyumcu.Core.Domain.Girdi.AlanSinirlari.TedarikAciklama),
                    HesapKodu = s.HesapKodu,
                    OlusturanKullaniciId = islem.Id,
                    Olusturuldu = now,
                    Guncellendi = now,
                });

                irsKalemler.Clear();
                for (var i = 0; i < plan.Count; i++)
                {
                    var satir = new IrsaliyeKalemi
                    {
                        Id = Kimlik.Ulid(),
                        TenantId = islem.TenantId,
                        IrsaliyeId = irsaliyeId,
                        UrunId = plan[i].Kalem.UrunId,
                        Tanim = plan[i].Kalem.Tanim,
                        Adet = plan[i].Adet,
                        HareketId = null,
                        Sira = i,
                        Olusturuldu = now,
                    };
                    irsKalemler.Add(satir);
                    db.IrsaliyeKalemleri.Add(satir);
                }

                denetim.YazmaKaydet(islem, nameof(Data.Irsaliye), irsaliyeId,
                    $"mal kabul irsaliyesi {irsNo} açıldı · sipariş {s.No}");
                await db.SaveChangesAsync();
                anaTx = tx;            // commit EN SONDA (URUN-ACILIS-ATOMIK)
                break;
            }
            catch (DbUpdateException) when (deneme < TedarikNo.AzamiDeneme)
            {
                await tx.RollbackAsync();
                await tx.DisposeAsync();
                db.ChangeTracker.Clear();
                irsNo = TedarikNo.Artir(irsNo);
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync();
                await tx.DisposeAsync();
                db.ChangeTracker.Clear();
                throw new ConflictException("İrsaliye numarası üretilemedi. Lütfen tekrar deneyin.");
            }
            catch
            {
                await tx.DisposeAsync();   // geri alınır
                throw;
            }
        }
        // `await using`: aşağıdaki herhangi bir adım reddederse işlem geri alınır.
        await using var _anaTx = anaTx;

        // ── (2) Her kalem için TEK YAZMA YOLU: SatisServisi.Alis ─────────────
        // DEFTER-DELEGE: bu modül deftere KENDİ yazmaz, yazan servise devreder.
        // Sayaç: denetim.sh P-26 DEFTERE_YAZMAYAN_PARA_MODULU (bu işaret
        // silinirse sayaç KIRILIR — yani "para alıp deftere yazmayan modül" geri
        // gelirse kapı kapanır).
        for (var i = 0; i < plan.Count; i++)
        {
            var (kalem, adet, fiyat) = plan[i];
            var sonuc = await satis.Alis(new Satis.AlisOlusturDto
            {
                CariId = s.TedarikciCariId,
                UrunId = kalem.UrunId!,
                Adet = adet,
                BirimFiyatTl = fiyat,
                KasaId = null,            // ödeme YOK: tedarikçiye borç
                // Ödeme YOK (KasaId null → ödenen 0): TEVSİK kapısı yalnız kasadan çıkan nakde bakar, tetiklenmez.
                // ⚠ MASAK kapısı ödeme şekline BAKMAZ (Tedbirler Yön. md.5/1-b: tutar ya da bağlantılı işlemler
                // toplamı) — bu yüzden beyan aşağıda Masak ile geçirilir. "NAKIT" yalnız alanın fail-closed
                // varsayılanıdır (AlisOlusturDto); veresiye değeri sözleşmede yoktur, ödenen 0 olduğu için etkisizdir.
                OdemeTuru = "NAKIT",
                Masak = dto.Masak,        // PAKET EKSIK-B1-SATINALMA tur 3: eşik üstü mal kabul çıkmaz sokak değil
                OdenenTl = null,
                Aciklama = $"Mal kabul · {s.No} / {irsNo}",
                KurSnapshotId = dto.KurSnapshotId!,
            }, islem);

            var hareketId = HareketKimligi(sonuc);

            // ── (3) Bağ + kabul adedi ────────────────────────────────────────
            var irsKalem = await db.IrsaliyeKalemleri
                .FirstAsync(k => k.Id == irsKalemler[i].Id && k.TenantId == islem.TenantId);
            irsKalem.HareketId = hareketId;

            var izlenen = await db.SatinalmaKalemleri
                .FirstAsync(k => k.Id == kalem.Id && k.TenantId == islem.TenantId);
            izlenen.KabulEdilenAdet += adet;
            await db.SaveChangesAsync();
        }

        // ── Durum: hepsi kabul edildiyse KAPANDI, değilse KISMI_KABUL ────────
        var guncel = await db.SatinalmaKalemleri
            .Where(k => k.SatinalmaSiparisId == id && k.TenantId == islem.TenantId)
            .Take(SatinalmaSiparisi.AzamiKalem).ToListAsync();
        var izlenenSiparis = await db.SatinalmaSiparisleri
            .FirstAsync(x => x.Id == id && x.TenantId == islem.TenantId);
        izlenenSiparis.Durum = guncel.All(k => k.KabulEdilenAdet >= k.Adet - 0.0005m)
            ? SatinalmaDurumu.Kapandi
            : SatinalmaDurumu.KismiKabul;
        izlenenSiparis.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, Varlik, id,
            $"mal kabul · {plan.Count} kalem · irsaliye {irsNo} · yeni durum {izlenenSiparis.Durum}");
        await db.SaveChangesAsync();
        await anaTx!.CommitAsync();   // URUN-ACILIS-ATOMIK: irsaliye + tüm alışlar + durum tek commit

        logger.LogInformation("[{TenantId}] MAL KABUL: sipariş={No} irsaliye={Irs} kalem={Adet}",
            islem.TenantId, s.No, irsNo, plan.Count);

        // UX-S27: sonuç BANDA yazılacak veriyle döner (irsaliye no + durum).
        return new
        {
            siparis = await Detay(id, islem),
            irsaliyeId,
            irsaliyeNo = irsNo,
            kabulEdilenKalem = plan.Count,
            yeniDurum = izlenenSiparis.Durum,
        };
    }

    /// <summary>
    /// PAKET EKSIK-B1-SATINALMA — kilit ALINDIKTAN sonra, son commit'i görerek: sipariş hâlâ
    /// kabul edilebilir mi, her kalemin kalanı istenen adede yetiyor mu. Yetmiyorsa 409
    /// (iş kuralı çatışması: başka bir mal kabul az önce aynı kalanı yazdı).
    /// </summary>
    private async Task KilitSonrasiDenetle(string id, List<(string KalemId, decimal Adet)> istenen, string tenantId)
    {
        var durum = await db.SatinalmaSiparisleri.AsNoTracking()
            .Where(x => x.Id == id && x.TenantId == tenantId).Select(x => x.Durum).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Satınalma siparişi bulunamadı");
        if (!SatinalmaDurumu.KabulEdilebilir(durum))
            throw new ConflictException(
                $"Mal kabul yalnız ONAYLANDI/KISMI_KABUL siparişte yapılır (şu an: {durum}).");
        var guncel = await db.SatinalmaKalemleri.AsNoTracking()
            .Where(k => k.SatinalmaSiparisId == id && k.TenantId == tenantId)
            .Select(k => new { k.Id, k.Adet, k.KabulEdilenAdet, k.Sira })
            .Take(SatinalmaSiparisi.AzamiKalem).ToListAsync();
        foreach (var (kalemId, adet) in istenen)
        {
            var k = guncel.First(x => x.Id == kalemId);
            if (k.KabulEdilenAdet + adet > k.Adet + 0.0005m)
                throw new ConflictException(
                    $"Kalem {k.Sira + 1} bu arada kabul edildi (sipariş {k.Adet}, kabul edilen {k.KabulEdilenAdet}, "
                    + $"istenen {adet}). Sayfayı yenileyip kalan adedi girin.");
        }
    }

    /// <summary>
    /// <c>SatisServisi.Alis</c> yanıtından doğan hareketin kimliğini okur.
    /// Yanıt ya <see cref="Hareket"/> varlığıdır ya da yerleşim uyarısı
    /// eklenmiş JSON düğümüdür (HareketGetir sözleşmesi) — iki hâl de okunur.
    /// Kimlik çıkmazsa <c>null</c> kalır ve kalem "bağsız" görünür; UYDURULMAZ.
    /// </summary>
    private static string? HareketKimligi(object? sonuc) => sonuc switch
    {
        Hareket h => h.Id,
        JsonObject j => j["id"]?.GetValue<string>(),
        _ => null,
    };

    // ─── Bağsız kalem raporu ─────────────────────────────────────────────────

    /// <summary>
    /// Deftere bağlanamamış mal kabul kalemleri (atomiklik sınırının görünür
    /// yüzü). Boş dönmesi beklenir; dolu dönerse elle düzeltme gerekir.
    /// </summary>
    public async Task<object> BagsizKalemler(AktifKullanici islem)
    {
        var satirlar = await (
            from k in db.IrsaliyeKalemleri.AsNoTracking()
            join i in db.Irsaliyeler.AsNoTracking() on k.IrsaliyeId equals i.Id
            where k.TenantId == islem.TenantId && i.Yon == IrsaliyeYonu.Gelen
                  && i.Durum != IrsaliyeDurumu.Iptal && k.HareketId == null
            orderby i.Tarih descending
            select new { irsaliyeId = i.Id, i.No, i.Tarih, kalemId = k.Id, k.UrunId, k.Tanim, k.Adet })
            .Take(IrsaliyeVarligi.AzamiLimit).ToListAsync();

        return new
        {
            satirlar,
            toplam = satirlar.Count,
            aciklama = satirlar.Count == 0
                ? "Deftere bağlanmamış mal kabul kalemi yok."
                : "Bu kalemler için stok/alış hareketi oluşmadı — mal kabulü tekrarlayın ya da alışı elle girin.",
        };
    }

    // ─── Yardımcılar ─────────────────────────────────────────────────────────

    private async Task<SatinalmaKalemi> KalemKur(SatinalmaKalemDto dto, int sira, string tenantId)
    {
        // PAKET PATCH-2: önce kolon hanesine (decimal(12,3)) yuvarla, sonra sıfır kontrolü — 0,0004 kontrolden geçip 0,000 yazılmaz.
        var adet = Math.Round(dto.Adet ?? 0m, 3, MidpointRounding.AwayFromZero);
        if (adet <= 0m)
            throw new BadRequestException($"Kalem {sira + 1}: adet sıfırdan büyük olmalı").AlanIle("kalemler");
        if (dto.BirimFiyatTl is < 0m)
            throw new BadRequestException($"Kalem {sira + 1}: birim fiyat negatif olamaz");
        // PATCH-SOZLESME tur 3: satinalma_kalemleri adet decimal(12,3) · birim_fiyat_tl decimal(15,2).
        KolonTavani.Denetle(adet, 3, KolonTavani.D12_3, "kalemler", $"Kalem {sira + 1}: adet");
        KolonTavani.Denetle(dto.BirimFiyatTl, 2, KolonTavani.D15_2, "kalemler", $"Kalem {sira + 1}: birim fiyat");

        var urunId = string.IsNullOrWhiteSpace(dto.UrunId) ? null : dto.UrunId;
        var tanim = Kirp(dto.Tanim, Kuyumcu.Core.Domain.Girdi.AlanSinirlari.TedarikKalemTanim, "kalemler");
        if (urunId is null && string.IsNullOrWhiteSpace(tanim))
            throw new BadRequestException(
                $"Kalem {sira + 1}: ürün seçin ya da bir tanım yazın (ikisi de boş olamaz)");

        if (urunId is not null && !await db.Urunler.AsNoTracking()
                .AnyAsync(u => u.Id == urunId && u.TenantId == tenantId))
            throw new BadRequestException($"Kalem {sira + 1}: ürün bulunamadı");

        // PAKET EKSIK-B1-SATINALMA (hasım: ADET'e kesir): ürüne bağlı kalemde birim ADET ise
        // adet tam sayı olmalı (UrunAdetKapisi — satış/alışla AYNI kural). Ürünsüz tanım
        // satırında birim bilinmez → kural uygulanmaz (mal kabul zaten ürün ister).
        if (urunId is not null)
        {
            var birim = await db.Urunler.AsNoTracking()
                .Where(u => u.Id == urunId && u.TenantId == tenantId).Select(u => u.Birim).FirstOrDefaultAsync();
            Urun.UrunAdetKapisi.Dogrula(birim, adet, "kalemler", $"Kalem {sira + 1}");
        }

        return new SatinalmaKalemi
        {
            UrunId = urunId, Tanim = tanim, Adet = adet,
            BirimFiyatTl = dto.BirimFiyatTl, Sira = sira,
        };
    }

    /// <summary>
    /// PAKET EKSIK-B1-SATINALMA (hasım: geçmiş/gelecek tarih) — beklenen teslim tarihi bugün ya da
    /// sonrası, en çok 10 yıl (Core TarihPenceresi.Vade — İŞ KURALI, mevzuat hükmü değildir).
    /// PATCH'te yalnız DEĞİŞEN tarih sınanır: dünkü beklenen tarihi taşıyan taslağın açıklaması düzenlenebilir.
    /// </summary>
    internal static void BeklenenTarihDenetle(DateOnly? tarih) =>
        Common.Dogrulama.GirdiKapisi.Denetle(
            Kuyumcu.Core.Domain.Girdi.TarihPenceresi.Denetle(
                tarih, Kuyumcu.Core.Domain.Girdi.TarihTuru.Vade, Zaman.YerelBugun(DateTime.UtcNow)),
            "beklenenTarih");

    private async Task<SatinalmaSiparisi> Bul(string id, string tenantId) =>
        await db.SatinalmaSiparisleri.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId)
        ?? throw new NotFoundException("Satınalma siparişi bulunamadı");

    /// <summary>
    /// Boşluğu kırpar; boşsa null. PAKET EKSIK-B1-SATINALMA (hasım: aşırı uzun metin): kolon sınırını
    /// aşan metin artık SESSİZCE KESİLMEZ — 400 + alan (UX-S29; GirdiKapisi.Uzunluk ile aynı sözlük iletisi).
    /// Ekran kutuları aynı sınırı (EnFazla) taşır.
    /// </summary>
    internal static string? Kirp(string? metin, int azami, string alan = "aciklama")
    {
        if (string.IsNullOrWhiteSpace(metin)) return null;
        return Common.Dogrulama.GirdiKapisi.Uzunluk(metin, azami, alan);
    }

    internal static bool Alan(JsonElement govde, string ad, out JsonElement oge)
    {
        if (govde.ValueKind == JsonValueKind.Object)
            foreach (var p in govde.EnumerateObject())
                if (string.Equals(p.Name, ad, StringComparison.OrdinalIgnoreCase))
                {
                    oge = p.Value;
                    return true;
                }
        oge = default;
        return false;
    }
}

// ── Uçlar ────────────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/satinalma")]
// Tedarikçi fiyatı ve borç bilgisi taşır → tezgâhtar görmez.
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
public sealed class SatinalmaController(SatinalmaServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/satinalma — liste + durum kırılımı</summary>
    [HttpGet]
    public Task<object> Listele(
        [FromQuery] string? durum, [FromQuery] string? cariId,
        [FromQuery] string? subeId, [FromQuery] int? limit)
        => servis.Listele(durum, cariId, subeId, limit, Islem);

    /// <summary>GET /api/v1/satinalma/bagsiz-kalemler — deftere bağlanmamış mal kabul kalemleri</summary>
    [HttpGet("bagsiz-kalemler")]
    public Task<object> BagsizKalemler() => servis.BagsizKalemler(Islem);

    /// <summary>GET /api/v1/satinalma/{id} — detay (kalemler + irsaliyeler)</summary>
    [HttpGet("{id}")]
    public Task<object> Detay(string id) => servis.Detay(id, Islem);

    /// <summary>POST /api/v1/satinalma — yeni TASLAK sipariş</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Olustur([FromBody] SatinalmaOlusturDto dto) => servis.Olustur(dto, Islem);

    /// <summary>PATCH /api/v1/satinalma/{id} — gövdede yoksa KORU, null ise SİL</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Guncelle(string id, [FromBody] JsonElement govde) =>
        servis.Guncelle(id, govde, Islem);

    /// <summary>POST /api/v1/satinalma/{id}/onayla — mal kabule açar</summary>
    [HttpPost("{id}/onayla")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Onayla(string id) => servis.Onayla(id, Islem);

    /// <summary>POST /api/v1/satinalma/{id}/iptal — PASİFLEŞTİRME (hard DELETE yok)</summary>
    [HttpPost("{id}/iptal")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Iptal(string id, [FromBody] TedarikGerekceDto dto) =>
        servis.Iptal(id, dto.Gerekce, Islem);

    /// <summary>POST /api/v1/satinalma/{id}/aktife-al — iptalden geri dönüş (taslak)</summary>
    [HttpPost("{id}/aktife-al")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> AktifeAl(string id) => servis.AktifeAl(id, Islem);

    /// <summary>POST /api/v1/satinalma/{id}/mal-kabul — stoğa SatisServisi.Alis ile yazar</summary>
    [HttpPost("{id}/mal-kabul")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> MalKabul(string id, [FromBody] MalKabulDto dto) =>
        servis.MalKabul(id, dto, Islem);
}
