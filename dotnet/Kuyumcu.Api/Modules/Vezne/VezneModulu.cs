using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VezneFisVarligi = Kuyumcu.Api.Data.VezneFisi;

namespace Kuyumcu.Api.Modules.Vezne;

// ═════════════════════════════════════════════════════════════════════════════
// VEZNE MODÜLÜ (paket 19 Faz A) — tezgâhtar satar, vezne tahsil eder.
//
// Desen: Modules/Siparis/SiparisModulu.cs (DTO + servis + controller aynı
// dosyada; doğrulama servis içinde ELLE — DataAnnotations yalnız controller
// yolunda koşar).
//
// ⚠ R10 — TEK YAZMA YOLU: bu dosyada HareketFabrikasi.Ekle YOKTUR,
//   Hareketler.Add YOKTUR, KasaHareketleri.Add YOKTUR. Para el değiştirdiği an
//   Satis.SatisServisi.SatisFisi ÇAĞRILIR; tevsik (459 VUK), MASAK, gün kilidi,
//   K6 kasa para birimi, vade/limit, taş kökeni, konsinye ve stok kapıları
//   bugünkü TEK yoldan çalışır. denetim.sh § vezne kuralı bunu SAYAR.
//
// ⚠ ATOMLUK (Bertan + Mehmet koşulu): SatisFisi kendi transaction'ını açıp
//   commit eder; vezne fişinin durumu ondan SONRA yazılır. Çift yazmayı
//   engelleyen şey sıralamadır: fiş ÖNCE iyimser kilitle VEZNEDE → TAHSIL'e
//   çevrilir (SaveChanges WHERE guncellendi = @eski), ANCAK SONRA defter
//   yazılır. İkinci eşzamanlı istek durumu VEZNEDE bulamaz → 409. SatisFisi
//   hata verirse fiş VEZNEDE'ye geri alınır (R9).
//
// ⚠ FAZ SINIRI: bu dosya Faz A'dır. R1 (vezne_zorunlu iken doğrudan satışın
//   kapatılması) ve Islemler.razor'daki "Vezneye gönder" akışı Faz B'dedir;
//   SatisModulu.cs'ye BU PAKETTE DOKUNULMADI.
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class VezneKalemDto
{
    public string? UrunId { get; set; }
    // PAKET GOC-A (envanter açık #6): miktar > 0, en çok 3 ondalık (K10 — GRAM üründe miktar gramdır);
    // ADET birimli üründe ayrıca TAM SAYI ≥ 1 (K14) — birim üründen okunur, servis KalemleriKur'da.
    [Gram] public decimal? Adet { get; set; }
    [Tutar] public decimal? BirimFiyatTl { get; set; }
    [Tutar(TutarTuru.SifirOlabilir)] public decimal? IscilikTl { get; set; }
}

public sealed class VezneFisOlusturDto
{
    public string? CariId { get; set; }
    public string? KurSnapshotId { get; set; }
    public string? Aciklama { get; set; }
    public List<VezneKalemDto>? Kalemler { get; set; }

    /// <summary>P-23c — kapalı liste <see cref="VezneFisTuru"/>. Boşsa SATIS.</summary>
    public string? FisTuru { get; set; }

    /// <summary>P-23c — KALEMSİZ türlerde (TAHSILAT · ISCILIK) tutar. SATIS'ta yasak.</summary>
    [Tutar]   // PAKET GOC-A (K9): oluşturmada tavan/ondalık yoktu (PATCH'te vardı)
    public decimal? TutarTl { get; set; }
}

public sealed class VezneTahsilDto
{
    /// <summary>TAHSİL ANININ kuru — ZORUNLU (R8). Fişin kuru referanstır.</summary>
    public string? KurSnapshotId { get; set; }

    /// <summary>OdemeSepeti bacakları — NAKIT · KART · HAVALE · VERESIYE · DOVIZ.</summary>
    public List<Satis.OdemeBacagiDto>? Bacaklar { get; set; }

    public Masak.MasakBilgiDto? Masak { get; set; }

    public int? VadeGun { get; set; }

    /// <summary>İyimser kilit damgası — verilirse eskimişse 409.</summary>
    public DateTime? Guncellendi { get; set; }
}

public sealed class VezneIptalDto
{
    public string? Gerekce { get; set; }
    public DateTime? Guncellendi { get; set; }
}

public sealed class VezneAyarDto
{
    public bool? Zorunlu { get; set; }
}

// ── Kiracı ayarı okuma/yazma ─────────────────────────────────────────────────

/// <summary>
/// kiraci_ayarlari üzerinden vezne ayrımı bayrağı. Tenant tablosuna kolon
/// EKLENMEDİ (Bertan koşulu): Tenant IKiraciVarlik değildir.
/// </summary>
public static class VezneAyari
{
    public static async Task<bool> ZorunluMu(KuyumcuDbContext db, string tenantId) =>
        await db.KiraciAyarlari.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.Anahtar == KiraciAyarAnahtari.VezneZorunlu)
            .Select(a => a.Deger)
            .FirstOrDefaultAsync() == "true";
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class VezneServisi(
    KuyumcuDbContext db, ILogger<VezneServisi> logger, Satis.SatisServisi satis,
    Cari.CariServisi cari, DenetimServisi? denetim = null)
{
    /// <summary>PAKET GOC-A — iptal gerekçesi sınırları: K30 alt sınırı (20) ve kolon (varchar(500)).
    /// Ekran bu değeri yanıttaki <c>gerekceAsgari</c>'den okur (tek kaynak).</summary>
    public const int IptalGerekceEnAz = AlanSinirlari.GerekceEnAz;
    public const int IptalGerekceEnCok = AlanSinirlari.VezneIptalGerekce;

    // ─── P-23c — TÜR KAPISI (gövdeden gelen tür doğrulanır) ───────────────────

    /// <summary>
    /// Gövdeden gelen fiş türünü doğrular. Boş = SATIS (paket 19 davranışı
    /// birebir korunur). Kapalı liste dışı tür 400 döner — sessizce SATIS'a
    /// düşürmek, kullanıcının istediğinden BAŞKA bir fiş açmak olurdu.
    /// </summary>
    private static string TurDogrula(string? gelen)
    {
        if (string.IsNullOrWhiteSpace(gelen)) return VezneFisTuru.Satis;
        if (!VezneFisTuru.Gecerli(gelen))
            throw new BadRequestException(
                $"Geçersiz vezne fiş türü: '{gelen}'. Geçerli türler: {string.Join(", ", VezneFisTuru.Tumu)}. "
                + "Takas ve emanet vezne fişi taşımaz; takas doğrudan işlem ekranından girilir, "
                + "emanet giriş/çıkışında para el değiştirmez.");
        return gelen!;
    }

    // ─── Fiş aç ───────────────────────────────────────────────────────────────

    public async Task<object> Olustur(VezneFisOlusturDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem);

        if (string.IsNullOrWhiteSpace(dto.KurSnapshotId))
            throw new BadRequestException("Kur kaydı zorunlu");

        // K1 — yabancı kimlikler önce doğrulanır.
        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId!, islem.TenantId);
        await IslemDogrulayici.CariDogrula(db, dto.CariId, islem.TenantId);

        // ── P-23c — TÜRE GÖRE GÖVDE SÖZLEŞMESİ ──────────────────────────────
        // ⚠ Sessiz etkisizlik YASAK (DoD EK 3): kalemsiz türe kalem gönderen ya
        //   da kalemli türe tutar gönderen istek REDDEDİLİR. Yok sayılsaydı
        //   kullanıcı girdiği veriyi kaydedilmiş sanardı.
        var fisTuru = TurDogrula(dto.FisTuru);
        var kalemli = VezneFisTuru.Kalemli(fisTuru);

        if (!kalemli && dto.Kalemler is { Count: > 0 })
            throw new BadRequestException(
                $"{fisTuru} fişi ürün kalemi taşımaz; tutar 'tutarTl' alanına yazılır.");
        if (kalemli && dto.TutarTl is not null)
            throw new BadRequestException(
                "Satış fişinin tutarı kalemlerden hesaplanır; 'tutarTl' gönderilemez (UX-S21).");
        if (!kalemli && dto.TutarTl is null)
            throw new BadRequestException(
                $"{fisTuru} fişi için tutar zorunludur ve sıfırdan büyük olmalıdır.");
        // PAKET GOC-A (K9): eksi/sıfır/2'den fazla ondalık/decimal(15,2) tavanı — POST'ta da (PATCH'le AYNI kapı).
        if (!kalemli) GirdiKapisi.Tutar(dto.TutarTl, "tutarTl");
        if (VezneFisTuru.CariZorunlu(fisTuru) && string.IsNullOrWhiteSpace(dto.CariId))
            throw new BadRequestException(
                $"{fisTuru} fişi cariye yazılır; cari seçimi zorunludur.");

        var kalemler = kalemli
            ? await KalemleriKur(dto.Kalemler, islem.TenantId)
            : [];

        var now = DateTime.UtcNow;
        var fisId = Kimlik.Ulid();

        await using var tx = await db.Database.BeginTransactionAsync();
        db.VezneFisleri.Add(new VezneFisVarligi
        {
            Id = fisId,
            TenantId = islem.TenantId,
            SubeId = subeId,
            Durum = VezneDurum.Acik,
            FisTuru = fisTuru,
            TalepTutarTl = kalemli ? null : Yuvarla2(dto.TutarTl!.Value),
            HazirlayanKullaniciId = islem.Id,
            CariId = string.IsNullOrWhiteSpace(dto.CariId) ? null : dto.CariId,
            Aciklama = Kirp(dto.Aciklama, 500),
            KurSnapshotId = dto.KurSnapshotId!,
            ToplamTl = 0m,          // M-V10: tutar YALNIZ vezneleme anında damgalanır
            Olusturuldu = now,
            Guncellendi = now,
        });
        foreach (var k in kalemler)
            db.VezneFisKalemleri.Add(new VezneFisKalemi
            {
                Id = Kimlik.Ulid(),
                TenantId = islem.TenantId,
                FisId = fisId,
                UrunId = k.UrunId,
                Adet = k.Adet,
                BirimFiyatTl = k.BirimFiyatTl,
                IscilikTl = k.IscilikTl,
                Sira = k.Sira,
                Olusturuldu = now,
            });
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] VEZNE fiş açıldı: {FisId} ({Kalem} kalem)",
            islem.TenantId, fisId, kalemler.Count);

        return await Detay(fisId, islem);
    }

    // ─── PATCH (yalnız ACIK) ──────────────────────────────────────────────────

    /// <summary>
    /// PATCH sözleşmesi: <b>gövdede yoksa KORU, null ise SİL</b>. Gövde ham
    /// <see cref="JsonElement"/> okunur — nullable DTO ile "gönderilmedi" ile
    /// "null gönderildi" ayırt EDİLEMEZ.
    /// </summary>
    public async Task<object> Guncelle(string id, JsonElement govde, AktifKullanici islem)
    {
        if (govde.ValueKind != JsonValueKind.Object)
            throw new BadRequestException("Geçersiz istek gövdesi");

        var fis = await Bul(id, islem);
        VezneKapisi.SahiplikDogrula(fis.HazirlayanKullaniciId, islem);
        VezneKapisi.DuzenlemeDurumuDogrula(fis.Durum);       // R4
        IyimserKilitDogrula(fis, Damga(govde));

        if (Alan(govde, "cariId", out var cariOge))
        {
            var yeni = cariOge.ValueKind == JsonValueKind.Null ? null : cariOge.GetString();
            await IslemDogrulayici.CariDogrula(db, yeni, islem.TenantId);
            fis.CariId = string.IsNullOrWhiteSpace(yeni) ? null : yeni;
        }

        if (Alan(govde, "aciklama", out var acOge))
            fis.Aciklama = acOge.ValueKind == JsonValueKind.Null ? null : Kirp(acOge.GetString(), 500);

        if (Alan(govde, "kurSnapshotId", out var kurOge))
        {
            if (kurOge.ValueKind == JsonValueKind.Null || string.IsNullOrWhiteSpace(kurOge.GetString()))
                throw new BadRequestException("Kur kaydı silinemez");
            await IslemDogrulayici.KurDogrula(db, kurOge.GetString()!, islem.TenantId);
            fis.KurSnapshotId = kurOge.GetString()!;
        }

        // ── P-23c — TÜR DEĞİŞMEZ, TUTAR DÜZENLENEBİLİR ──────────────────────
        // Tür bir kere seçilir: TAHSILAT fişini SATIS'a çevirmek kalemsiz bir
        // fişi kalemli saymak olurdu ve damgalanmış tutar anlamını yitirirdi.
        // DegismezIpucu gerekçesi budur; doğru yol fişi iptal edip yenisini
        // açmaktır (iptal SİLMEZ, gerekçesiyle durur).
        if (Alan(govde, "fisTuru", out var turOge)
            && turOge.ValueKind != JsonValueKind.Null
            && turOge.GetString() != fis.FisTuru)
            throw new BadRequestException(
                "Vezne fişinin türü değiştirilemez. Yanlış türde açıldıysa fiş iptal edilir "
                + "(gerekçesiyle kayıtta kalır) ve doğru türde yeni fiş açılır.");

        var fisKalemli = VezneFisTuru.Kalemli(fis.FisTuru);

        if (Alan(govde, "tutarTl", out var tutarOge))
        {
            if (fisKalemli)
                throw new BadRequestException(
                    "Satış fişinin tutarı kalemlerden hesaplanır; 'tutarTl' düzenlenemez (UX-S21).");
            if (tutarOge.ValueKind == JsonValueKind.Null)
                throw new BadRequestException($"{fis.FisTuru} fişinin tutarı silinemez.");
            var yeniTutar = KolonTavani.JsonSayi(tutarOge, "tutarTl", "Tutar");   // tur 3: aralık dışı → 400
            // PAKET GOC-A (K9): Core kuralı + sözlük metni (POST ile aynı); fazla ondalık yuvarlanmaz.
            GirdiKapisi.Tutar(yeniTutar, "tutarTl");
            // PATCH-SOZLESME tur 3: vezne_fisleri.talep_tutar_tl decimal(15,2) kapasitesi.
            KolonTavani.Denetle(yeniTutar, 2, KolonTavani.D15_2, "tutarTl", "Tutar");
            fis.TalepTutarTl = Yuvarla2(yeniTutar);
        }

        List<HazirKalem>? yeniKalemler = null;
        if (Alan(govde, "kalemler", out var kalemOge))
        {
            if (!fisKalemli)
                throw new BadRequestException(
                    $"{fis.FisTuru} fişi ürün kalemi taşımaz; tutar 'tutarTl' alanıyla düzenlenir.");
            if (kalemOge.ValueKind != JsonValueKind.Array)
                throw new BadRequestException("Kalemler bir dizi olmalı");
            var gelen = KolonTavani.JsonListe<VezneKalemDto>(kalemOge, "kalemler", "Kalemler");   // tur 3
            yeniKalemler = await KalemleriKur(gelen, islem.TenantId);
        }

        var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync();
        if (yeniKalemler is not null)
        {
            // Kalem kimliği DIŞARIDAN gelmez: küme tümüyle değişir (sil + yaz).
            var mevcut = await db.VezneFisKalemleri
                .Where(k => k.FisId == fis.Id && k.TenantId == islem.TenantId).ToListAsync();
            db.VezneFisKalemleri.RemoveRange(mevcut);
            foreach (var k in yeniKalemler)
                db.VezneFisKalemleri.Add(new VezneFisKalemi
                {
                    Id = Kimlik.Ulid(),
                    TenantId = islem.TenantId,
                    FisId = fis.Id,
                    UrunId = k.UrunId,
                    Adet = k.Adet,
                    BirimFiyatTl = k.BirimFiyatTl,
                    IscilikTl = k.IscilikTl,
                    Sira = k.Sira,
                    Olusturuldu = now,
                });
        }
        fis.Guncellendi = now;
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return await Detay(fis.Id, islem);
    }

    // ─── Vezneye gönder (ACIK → VEZNEDE) ──────────────────────────────────────

    public async Task<object> Veznele(string id, AktifKullanici islem)
    {
        var fis = await Bul(id, islem);
        VezneKapisi.SahiplikDogrula(fis.HazirlayanKullaniciId, islem);
        VezneKapisi.DuzenlemeDurumuDogrula(fis.Durum);       // ACIK dışında 409

        // P-23c: kalemli fişte "boş fiş" kalem yokluğudur; kalemsiz fişte tutar
        // yokluğudur. İki hâl de vezneye BOŞ fiş göndermektir ve reddedilir.
        var kalemli = VezneFisTuru.Kalemli(fis.FisTuru);
        var kalemler = kalemli ? await Kalemler(fis.Id, islem.TenantId) : [];
        if (kalemli && kalemler.Count == 0)
            throw new BadRequestException("Boş fiş vezneye gönderilemez: en az bir kalem gerekli");
        if (!kalemli && !(fis.TalepTutarTl > 0m))
            throw new BadRequestException("Tutarsız fiş vezneye gönderilemez: tutar girilmelidir");

        var now = DateTime.UtcNow;
        fis.Durum = VezneDurum.Veznede;
        // M-V10: tutar TAM BURADA damgalanır — kuyrukta görünen tutar ile
        // tahsil edilen tutar ayrışamaz.
        fis.ToplamTl = kalemli ? Toplam(kalemler) : fis.TalepTutarTl!.Value;
        fis.Veznelendi = now;
        fis.Guncellendi = now;
        await db.SaveChangesAsync();

        logger.LogInformation("[{TenantId}] VEZNE fiş veznelendi: {FisId} toplam={Toplam}",
            islem.TenantId, fis.Id, fis.ToplamTl);

        return await Detay(fis.Id, islem);
    }

    // ─── Kuyruk ───────────────────────────────────────────────────────────────

    public async Task<object> Kuyruk(string? durum, int? limit, AktifKullanici islem)
    {
        // Yanıtta kullanıcıya hangi şubenin kuyruğu olduğu SÖYLENİR (UX-S21 rozeti);
        // süzgecin kendisi artık OturumSubesinde() mekanizmasındadır.
        var subeId = SubeBelirle(islem);
        var etkinDurum = string.IsNullOrWhiteSpace(durum) ? VezneDurum.Veznede : durum!;
        if (etkinDurum != "TUMU" && Array.IndexOf(VezneDurum.Tumu, etkinDurum) < 0)
            throw new BadRequestException($"Geçersiz durum: {durum}");

        var istenen = limit ?? VezneFisVarligi.VarsayilanLimit;
        var etkinLimit = Math.Clamp(istenen, 1, VezneFisVarligi.AzamiLimit);

        // R6: ŞUBE KAPSAMI SUNUCUDA. Başka şubenin fişi kuyrukta GÖRÜNMEZ.
        var sorgu = db.VezneFisleri.AsNoTracking()
            .Where(f => f.TenantId == islem.TenantId)
            .OturumSubesinde(islem, SubeGerekli);
        if (etkinDurum != "TUMU") sorgu = sorgu.Where(f => f.Durum == etkinDurum);

        var toplam = await sorgu.CountAsync();
        var kirilim = await db.VezneFisleri.AsNoTracking()
            .Where(f => f.TenantId == islem.TenantId)
            .OturumSubesinde(islem, SubeGerekli)
            .GroupBy(f => f.Durum)
            .Select(g => new { durum = g.Key, adet = g.Count() })
            .ToListAsync();

        var satirlar = await sorgu
            .OrderByDescending(f => f.Olusturuldu)
            .Take(etkinLimit)
            .Select(f => new
            {
                f.Id, f.Durum, f.FisTuru, f.TalepTutarTl,
                f.ToplamTl, f.CariId, f.Olusturuldu, f.Veznelendi, f.Guncellendi,
                f.HazirlayanKullaniciId, f.HareketFisId, f.Aciklama,
                // ⚠ KİŞİSEL VERİ SINIRI (Mehmet koşulu): yalnız AD döner;
                //   telefon/TCKN/adres kuyruk ucunda YOKTUR.
                CariAd = db.Cariler.Where(c => c.Id == f.CariId).Select(c => c.Ad).FirstOrDefault(),
                HazirlayanAd = db.Kullanicilar.Where(k => k.Id == f.HazirlayanKullaniciId)
                    .Select(k => k.Ad + " " + k.Soyad).FirstOrDefault(),
                KalemSayisi = db.VezneFisKalemleri.Count(k => k.FisId == f.Id),
            })
            .ToListAsync();

        // P-46 (kuyruk = detay): ACIK kalemli fişlerin kalemleri TEK sorguda
        // çekilir ve fiş kimliğine göre bellekte gruplanır — fiş başına sorgu
        // (N+1) açılmaz. Damgalı (VEZNEDE/TAHSIL/IPTAL) fişin kalemi OKUNMAZ;
        // onların tutarı M-V10 damgasıdır, yeniden hesaplanmaz.
        var acikKalemliIdler = satirlar
            .Where(f => f.Durum == VezneDurum.Acik && VezneFisTuru.Kalemli(f.FisTuru))
            .Select(f => f.Id)
            .ToList();
        var acikKalemler = await KalemHaritasi(acikKalemliIdler, islem.TenantId);

        var vezneZorunlu = await VezneAyari.ZorunluMu(db, islem.TenantId);

        return new
        {
            durum = etkinDurum,
            istenenLimit = istenen,
            limit = etkinLimit,
            azamiLimit = VezneFisVarligi.AzamiLimit,
            toplam,
            // UX-S29: sınıra değdiyse ekran "daha fazlası var" yazar.
            dahaFazlasiVar = toplam > satirlar.Count,
            vezneZorunlu,
            gerekceAsgari = IptalGerekceEnAz,
            subeId,
            durumKirilimi = kirilim,
            fisler = satirlar.Select(f => new
            {
                id = f.Id,
                durum = f.Durum,
                fisTuru = f.FisTuru,
                kalemli = VezneFisTuru.Kalemli(f.FisTuru),
                // P-46: gösterilecek tutar DETAY ile AYNI yardımcıdan gelir
                // (ACIK kalemli → kalem toplamı, ACIK kalemsiz → talep tutarı,
                // damgalı → damga). damgaliToplamTl yalnız vezneleme anının
                // damgasıdır; ACIK fişte 0'dır ve ekran onu "tahmini" rozetiyle ayırır.
                toplamTl = GosterilecekToplam(f.Durum, VezneFisTuru.Kalemli(f.FisTuru),
                    f.TalepTutarTl, f.ToplamTl, acikKalemler.GetValueOrDefault(f.Id)),
                damgaliToplamTl = f.ToplamTl,
                toplamTahmini = ToplamTahmini(f.Durum),
                cariId = f.CariId,
                cariAd = f.CariAd,
                aciklama = f.Aciklama,
                hazirlayanKullaniciId = f.HazirlayanKullaniciId,
                hazirlayanAd = f.HazirlayanAd,
                kalemSayisi = f.KalemSayisi,
                hareketFisId = f.HareketFisId,
                duzenlenebilir = f.Durum == VezneDurum.Acik,
                // Risk (c): defterde karşılığı olmayan TAHSIL fişi mutabakat ister.
                mutabakatGerekli = f.Durum == VezneDurum.Tahsil && f.HareketFisId == null,
                olusturuldu = f.Olusturuldu,
                veznelendi = f.Veznelendi,
                guncellendi = f.Guncellendi,
            }),
        };
    }

    // ─── Detay ────────────────────────────────────────────────────────────────

    public async Task<object> Detay(string id, AktifKullanici islem)
    {
        var fis = await Bul(id, islem);
        var fisKalemli = VezneFisTuru.Kalemli(fis.FisTuru);
        var kalemler = await db.VezneFisKalemleri.AsNoTracking()
            .Where(k => k.FisId == id && k.TenantId == islem.TenantId)
            .OrderBy(k => k.Sira)
            .Select(k => new
            {
                k.Id, k.UrunId, k.Adet, k.BirimFiyatTl, k.IscilikTl, k.Sira,
                UrunAd = db.Urunler.Where(u => u.Id == k.UrunId).Select(u => u.Ad).FirstOrDefault(),
            })
            .ToListAsync();

        var cariAd = await db.Cariler.AsNoTracking()
            .Where(c => c.Id == fis.CariId).Select(c => c.Ad).FirstOrDefaultAsync();
        var hazirlayanAd = await db.Kullanicilar.AsNoTracking()
            .Where(k => k.Id == fis.HazirlayanKullaniciId).Select(k => k.Ad + " " + k.Soyad)
            .FirstOrDefaultAsync();
        var tahsilEdenAd = await db.Kullanicilar.AsNoTracking()
            .Where(k => k.Id == fis.TahsilEdenKullaniciId).Select(k => k.Ad + " " + k.Soyad)
            .FirstOrDefaultAsync();

        var vezneZorunlu = await VezneAyari.ZorunluMu(db, islem.TenantId);

        return new
        {
            id = fis.Id,
            durum = fis.Durum,
            // P-23c: tür ve kalem taşıyıp taşımadığı EKRANA SÖYLENİR; ekran
            // hangi formu göstereceğini tahmin etmez (UX-S31 tek kaynak).
            fisTuru = fis.FisTuru,
            fisTurleri = VezneFisTuru.Tumu,
            kalemli = fisKalemli,
            tutarTl = fis.TalepTutarTl,
            subeId = fis.SubeId,
            cariId = fis.CariId,
            cariAd,
            aciklama = fis.Aciklama,
            kurSnapshotId = fis.KurSnapshotId,
            // P-46: kuyruk ile AYNI yardımcı — iki uçta kopya hesap YOK.
            toplamTl = GosterilecekToplam(fis.Durum, fisKalemli, fis.TalepTutarTl, fis.ToplamTl,
                kalemler
                    .Select(k => new HazirKalem(k.UrunId, k.Adet, k.BirimFiyatTl, k.IscilikTl, k.Sira))
                    .ToList()),
            damgaliToplamTl = fis.ToplamTl,
            toplamTahmini = ToplamTahmini(fis.Durum),
            hareketFisId = fis.HareketFisId,
            hazirlayanKullaniciId = fis.HazirlayanKullaniciId,
            hazirlayanAd,
            tahsilEdenKullaniciId = fis.TahsilEdenKullaniciId,
            tahsilEdenAd,
            iptalGerekce = fis.IptalGerekce,
            vezneZorunlu,
            gerekceAsgari = IptalGerekceEnAz,
            duzenlenebilir = fis.Durum == VezneDurum.Acik,
            veznelenebilir = fis.Durum == VezneDurum.Acik
                && (fisKalemli ? kalemler.Count > 0 : fis.TalepTutarTl > 0m),
            tahsilEdilebilir = fis.Durum == VezneDurum.Veznede,
            iptalEdilebilir = fis.Durum is VezneDurum.Acik or VezneDurum.Veznede,
            terminal = VezneDurum.Terminal(fis.Durum),
            mutabakatGerekli = fis.Durum == VezneDurum.Tahsil && fis.HareketFisId == null,
            olusturuldu = fis.Olusturuldu,
            guncellendi = fis.Guncellendi,
            veznelendi = fis.Veznelendi,
            tahsilEdildi = fis.TahsilEdildi,
            iptalEdildi = fis.IptalEdildi,
            kalemler = kalemler.Select(k => new
            {
                id = k.Id, urunId = k.UrunId, urunAd = k.UrunAd, adet = k.Adet,
                birimFiyatTl = k.BirimFiyatTl, iscilikTl = k.IscilikTl, sira = k.Sira,
                tutarTl = Yuvarla2(k.Adet * k.BirimFiyatTl + k.IscilikTl),
            }),
        };
    }

    // ─── Tahsil (VEZNEDE → TAHSIL) ────────────────────────────────────────────

    /// <summary>
    /// Para el değiştirir. Fişin kalemleri + gövdedeki bacaklar SatisFisiDto'ya
    /// çevrilir ve <c>SatisServisi.SatisFisi</c> ÇAĞRILIR — vezne kendi defterini
    /// YAZMAZ (R10).
    /// </summary>
    public async Task<object> Tahsil(string id, VezneTahsilDto dto, AktifKullanici islem)
    {
        if (string.IsNullOrWhiteSpace(dto.KurSnapshotId))
            throw new BadRequestException(
                "Tahsil için kur kaydı zorunlu: deftere TAHSİL ANININ kuru yazılır.");

        var fis = await Bul(id, islem);
        VezneKapisi.TahsilDurumuDogrula(fis.Durum);                       // R3
        IyimserKilitDogrula(fis, dto.Guncellendi);

        var vezneZorunlu = await VezneAyari.ZorunluMu(db, islem.TenantId);
        VezneKapisi.DortGozDogrula(fis.HazirlayanKullaniciId, islem.Id, vezneZorunlu);   // R2

        // Risk azaltma (b): fişin defterde karşılığı VAR MI — TOCTOU'dur, asıl
        // koruma aşağıdaki iyimser kilittir.
        if (fis.HareketFisId is not null)
            throw new ConflictException("Bu fiş zaten deftere yazılmış.");

        var kalemli = VezneFisTuru.Kalemli(fis.FisTuru);
        var kalemler = kalemli ? await Kalemler(fis.Id, islem.TenantId) : [];
        if (kalemli && kalemler.Count == 0)
            throw new BadRequestException("Boş fiş tahsil edilemez");
        if (!kalemli && !(fis.ToplamTl > 0m))
            throw new BadRequestException("Tutarsız fiş tahsil edilemez");

        var bacaklar = dto.Bacaklar ?? [];
        if (bacaklar.Count == 0)
            throw new BadRequestException("En az bir ödeme bacağı gerekli");

        // ⚠ P-23c KAPSAM SINIRI — KALEMSİZ FİŞ TEK BACAKLIDIR.
        // TahsilatGir ve Iscilik bugün TEK ödeme aracı kabul eder (dto.OdemeTuru);
        // çok bacaklı sepet YALNIZ SatisFisi'nde vardır. İki bacağı "toplayıp"
        // tek araç sanmak, ödeme kırılımını UYDURMAK olurdu (Anayasa md.1).
        // Fail-closed: ikinci bacak 400 ile reddedilir, sessizce yutulmaz.
        if (!kalemli && bacaklar.Count > 1)
            throw new BadRequestException(
                $"{fis.FisTuru} fişi tek ödeme aracıyla tahsil edilir; {bacaklar.Count} bacak gönderildi. "
                + "Karma ödeme bugün yalnız satış fişinde desteklenir.");
        if (!kalemli && Math.Abs(Yuvarla2(bacaklar[0].TutarTl) - fis.ToplamTl) > 0.005m)
            throw new BadRequestException(
                $"Ödeme bacağının tutarı fişin damgalanmış tutarına eşit olmalıdır "
                + $"(bacak {Yuvarla2(bacaklar[0].TutarTl):0.00} TL, fiş {fis.ToplamTl:0.00} TL).");

        var oncekiDamga = fis.Guncellendi;
        var now = DateTime.UtcNow;

        // ── ADIM 1: fişi ÖNCE kilitle. SaveChanges `WHERE guncellendi = @eski`
        //    üretir (IsConcurrencyToken); ikinci eşzamanlı istek satırı
        //    bulamaz ve 409 alır. Defter ANCAK bundan sonra yazılır, böylece
        //    aynı fiş için İKİ defter fişi doğması imkânsız hâle gelir.
        fis.Durum = VezneDurum.Tahsil;
        fis.TahsilEdenKullaniciId = islem.Id;
        fis.TahsilEdildi = now;
        fis.Guncellendi = now;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            throw new ConflictException(
                "Bu fiş siz görüntüledikten sonra değişti (başka bir vezne tahsil etmiş olabilir). " +
                "Kuyruğu yenileyip tekrar deneyin.");
        }

        // ── ADIM 2: defter. Kapıların HEPSİ burada çalışır.
        object sonuc;
        string? dagitilanHareketId = null;
        try
        {
            // ── P-23c — TAHSİL YOLU TÜRE GÖRE DAĞITILIR ─────────────────────
            // ⚠ R10 KORUNUR: her dalda MEVCUT servis çağrılır; vezne kendi
            //   defterini hâlâ YAZMAZ. Tevsik (459 VUK), MASAK, gün kilidi ve
            //   K6 kapıları o servislerin içinde, bugünkü TEK yoldan koşar.
            if (!kalemli)
            {
                var (dalSonuc, dalHareketId) = fis.FisTuru switch
                {
                    VezneFisTuru.Tahsilat => await TahsilatDali(fis, bacaklar[0], dto, islem),
                    VezneFisTuru.Iscilik  => await IscilikDali(fis, bacaklar[0], dto, islem),
                    _ => throw new BadRequestException(
                        $"Bu fiş türü tahsil edilemiyor: '{fis.FisTuru}'."),
                };
                sonuc = dalSonuc;
                dagitilanHareketId = dalHareketId;
            }
            else
            sonuc = await satis.SatisFisi(new Satis.SatisFisiDto
            {
                CariId = fis.CariId,
                KurSnapshotId = dto.KurSnapshotId!,      // R8: TAHSİL anının kuru
                Aciklama = fis.Aciklama,
                VadeGun = dto.VadeGun,
                Masak = dto.Masak,
                Kalemler = [.. kalemler.Select(k => new Satis.SatisFisKalemDto
                {
                    UrunId = k.UrunId,
                    Adet = k.Adet,
                    BirimFiyatTl = k.BirimFiyatTl,
                    IscilikTl = k.IscilikTl,
                })],
                Odemeler = bacaklar,
            },
            // R11: deftere yazılan hareketin sahibi TAHSİL EDEN'dir — kasa
            // sorumluluğu ondadır. Hazırlayan yalnız vezne fişinde kalır.
            islem);
        }
        catch
        {
            // R9: stok/tevsik/MASAK/gün kilidi reddettiyse fiş VEZNEDE KALIR.
            db.ChangeTracker.Clear();
            var geri = await db.VezneFisleri
                .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == islem.TenantId);
            if (geri is not null)
            {
                geri.Durum = VezneDurum.Veznede;
                geri.TahsilEdenKullaniciId = null;
                geri.TahsilEdildi = null;
                geri.Guncellendi = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
            throw;
        }

        // ── ADIM 3: defter fişini vezne fişine BAĞLA.
        // Kalemsiz yolda fiş = doğan HAREKETİN KENDİSİDİR (odeme_kalemleri.fis_id
        // de aynı değeri taşır — OdemeKalemFabrikasi gerekçesi); kalemli yolda
        // SatisFisi'nin döndürdüğü fiş kimliği okunur.
        var hareketFisId = dagitilanHareketId ?? FisIdOku(sonuc);
        db.ChangeTracker.Clear();
        var kayit = await db.VezneFisleri
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Vezne fişi bulunamadı");
        kayit.HareketFisId = hareketFisId;
        kayit.Guncellendi = DateTime.UtcNow;
        denetim?.YazmaKaydet(islem, "VezneFisi", kayit.Id,
            $"vezne tahsili · hazırlayan={kayit.HazirlayanKullaniciId} · tahsil eden={islem.Id} " +
            $"· toplam={kayit.ToplamTl} TL · defter fişi={hareketFisId}");
        await db.SaveChangesAsync();

        logger.LogInformation(
            "[{TenantId}] VEZNE tahsil: fis={FisId} defter={HareketFisId} tahsilEden={Kullanici}",
            islem.TenantId, kayit.Id, hareketFisId, islem.Id);

        return new
        {
            vezneFisId = kayit.Id,
            durum = kayit.Durum,
            hareketFisId,
            toplamTl = kayit.ToplamTl,
            tahsilEdenKullaniciId = islem.Id,
            satis = sonuc,
        };
    }

    // ─── P-23c — KALEMSİZ TAHSİL DALLARI ─────────────────────────────────────
    //
    // ⚠ İKİ DAL DA MEVCUT SERVİSİ ÇAĞIRIR. Vezne kendi tevsik/MASAK/kasa
    //   mantığını KOPYALAMAZ: kopya bir gün ayrışır ve iki yol farklı cevap
    //   verirdi (HareketFabrikasi'nın var oluş gerekçesiyle aynı).
    // ⚠ VERESIYE bacağı bu iki türde REDDEDİLİR ve bu bilinçlidir: borcu
    //   veresiyeyle "tahsil etmek" borcu kapatmaz, yerinde bırakır — vezneden
    //   para geçmediği hâlde fiş TAHSIL görünürdü.

    private async Task<(object Sonuc, string HareketId)> TahsilatDali(
        VezneFisVarligi fis, Satis.OdemeBacagiDto b, VezneTahsilDto dto, AktifKullanici islem)
    {
        if (b.Arac == Data.OdemeAraci.Veresiye)
            throw new BadRequestException(
                "Tahsilat fişi veresiye ile kapatılamaz: borç veresiyeyle ödenmiş olmaz.");
        if (b.Arac == Data.OdemeAraci.Doviz)
            throw new BadRequestException(
                "Döviz tahsilatı vezne fişi taşımaz; döviz tahsilatı cari ekranından girilir.");

        var hareketId = await cari.TahsilatGirHareket(fis.CariId!, new Cari.TahsilatGirisDto
        {
            TlTutari = b.TutarTl,
            OdemeTuru = b.Arac,
            KasaId = b.KasaId,
            KurSnapshotId = dto.KurSnapshotId!,   // R8: TAHSİL anının kuru
            Aciklama = fis.Aciklama,
            Masak = dto.Masak,
        }, islem);

        return (new { tahsilatTl = b.TutarTl, arac = b.Arac, cariId = fis.CariId, hareketId }, hareketId);
    }

    private async Task<(object Sonuc, string HareketId)> IscilikDali(
        VezneFisVarligi fis, Satis.OdemeBacagiDto b, VezneTahsilDto dto, AktifKullanici islem)
    {
        if (b.Arac == Data.OdemeAraci.Veresiye)
            throw new BadRequestException(
                "İşçilik vezne fişi veresiye ile kapatılamaz: vezneden para geçmeyen iş "
                + "vezne fişi değil, doğrudan işçilik kaydıdır.");
        if (b.Arac == Data.OdemeAraci.Doviz)
            throw new BadRequestException(
                "İşçilik bedeli döviz bacağıyla tahsil edilemiyor; TL kasası seçilmelidir.");

        var sonuc = await satis.Iscilik(new Satis.IscilikDto
        {
            CariId = fis.CariId!,
            IscilikTl = b.TutarTl,
            OdemeTuru = b.Arac,
            KasaId = b.KasaId,
            KurSnapshotId = dto.KurSnapshotId!,
            Aciklama = fis.Aciklama,
            Masak = dto.Masak,
        }, islem);

        return (sonuc, HareketIdOku(sonuc));
    }

    /// <summary>Anonim yanıttan <c>hareketId</c> okur — boşsa DURUR, tahmin etmez.</summary>
    private static string HareketIdOku(object sonuc)
    {
        var deger = sonuc.GetType().GetProperty("hareketId")?.GetValue(sonuc) as string;
        if (string.IsNullOrWhiteSpace(deger))
            throw new ConflictException(
                "Tahsil sonucunda doğan defter satırının kimliği okunamadı; vezne fişi bağlanamaz.");
        return deger!;
    }

    // ─── İptal ────────────────────────────────────────────────────────────────

    public async Task<object> Iptal(string id, VezneIptalDto dto, AktifKullanici islem)
    {
        // PAKET GOC-A (envanter C · vz-iptal-gerekce): proje kuralı K30 en az 20 (burada 10 idi) ve kolon
        // varchar(500) — 500'ü aşan gerekçe SESSİZCE KESİLMEZ (eskiden Kirp(…, 500)), 400 + sözlük metni.
        var gerekce = GirdiKapisi.Gerekce(dto.Gerekce, "gerekce", IptalGerekceEnCok);

        var fis = await Bul(id, islem);
        VezneKapisi.SahiplikDogrula(fis.HazirlayanKullaniciId, islem);
        VezneKapisi.IptalDurumuDogrula(fis.Durum);           // R5
        IyimserKilitDogrula(fis, dto.Guncellendi);

        var now = DateTime.UtcNow;
        fis.Durum = VezneDurum.Iptal;                        // SATIR SİLİNMEZ
        fis.IptalGerekce = gerekce;
        fis.IptalEdildi = now;
        fis.Guncellendi = now;
        denetim?.YazmaKaydet(islem, "VezneFisi", fis.Id,
            $"vezne fişi iptal · hazırlayan={fis.HazirlayanKullaniciId} · gerekçe: {gerekce}");
        await db.SaveChangesAsync();

        logger.LogInformation("[{TenantId}] VEZNE fiş iptal: {FisId}", islem.TenantId, fis.Id);
        return await Detay(fis.Id, islem);
    }

    // ─── Yardımcılar ──────────────────────────────────────────────────────────

    private sealed record HazirKalem(string UrunId, decimal Adet, decimal BirimFiyatTl,
        decimal IscilikTl, int Sira);

    /// <summary>Şubesiz oturumda vezne akışının tek hata metni (TR).</summary>
    // KESIF-P5 (TOPLU § ORTAK-1 ORTA): metin BASİT ekranda da aynen görünür — "Vezne" iç terimi yerine ekranın
    // adı (Kasa kuyruğu) ve yapılacak iş. Kural (şubesiz kullanıcı 400) DEĞİŞMEDİ.
    private const string SubeGerekli = "Kasa kuyruğu için kullanıcıya şube atanmalı. Yönetim > Kullanıcılar ekranından şube seçin.";

    private static string SubeBelirle(AktifKullanici islem) =>
        string.IsNullOrEmpty(islem.SubeId)
            ? throw new BadRequestException(SubeGerekli)
            : islem.SubeId!;

    /// <summary>R6: yabancı kiracı VE yabancı şube fişi 404 — varlık sızdırılmaz.</summary>
    private async Task<VezneFisVarligi> Bul(string id, AktifKullanici islem)
    {
        return await db.VezneFisleri
            .OturumSubesinde(islem, SubeGerekli)
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Vezne fişi bulunamadı");
    }

    private async Task<List<HazirKalem>> Kalemler(string fisId, string tenantId) =>
        [.. (await db.VezneFisKalemleri.AsNoTracking()
            .Where(k => k.FisId == fisId && k.TenantId == tenantId)
            .OrderBy(k => k.Sira)
            .ToListAsync())
            .Select(k => new HazirKalem(k.UrunId, k.Adet, k.BirimFiyatTl, k.IscilikTl, k.Sira))];

    /// <summary>R7 — biçim kapısı. DataAnnotations yalnız controller yolunda koşar.</summary>
    private async Task<List<HazirKalem>> KalemleriKur(List<VezneKalemDto>? gelen, string tenantId)
    {
        var liste = gelen ?? [];
        if (liste.Count == 0)
            throw new BadRequestException("Fiş en az bir kalem içermeli");
        if (liste.Count > VezneFisVarligi.AzamiKalem)
            throw new BadRequestException($"Bir fişte en çok {VezneFisVarligi.AzamiKalem} kalem olabilir");

        var hazir = new List<HazirKalem>();
        for (var i = 0; i < liste.Count; i++)
        {
            var k = liste[i];
            if (string.IsNullOrWhiteSpace(k.UrunId))
                throw new BadRequestException($"{i + 1}. kalemde ürün seçilmeli");

            // PAKET GOC-A: ürünün BİRİMİ okunur (ADET → tam sayı; GRAM → 3 ondalıklı gram).
            var birim = await db.Urunler.AsNoTracking()
                .Where(u => u.Id == k.UrunId && u.TenantId == tenantId)
                .Select(u => u.Birim).FirstOrDefaultAsync();
            if (birim is null) throw new NotFoundException($"Ürün bulunamadı: {k.UrunId}");

            // PAKET GOC-A (envanter açık #6 — "1,5 adet satılabiliyor", "0,0004 sessizce yuvarlanıyor"):
            // miktar SESSİZCE YUVARLANMAZ ve ADET birimli üründe kesir REDDEDİLİR. Kural Core'da tek
            // (AgirlikKurali.Gram · AdetKurali), metin sözlükten — ekranın alan altı metniyle AYNI.
            var onek = $"{i + 1}. kalem";
            if (k.Adet is not { } adet) throw new BadRequestException($"{onek}: adet zorunludur").AlanIle("kalemler");
            GirdiKapisi.Gram(adet, "kalemler", $"{onek} · adet");
            if (birim != "GRAM") GirdiKapisi.Adet(adet, "kalemler", $"{onek} · adet");
            // Sıfır fiyatlı satış hediye DEĞİL hatadır (hediye yolu kampanyadadır).
            if (k.BirimFiyatTl is not { } fiyat) throw new BadRequestException($"{onek}: birim fiyat zorunludur").AlanIle("kalemler");
            GirdiKapisi.Tutar(fiyat, "kalemler", TutarTuru.Pozitif, $"{onek} · birim fiyat");
            var iscilik = GirdiKapisi.Tutar(k.IscilikTl ?? 0m, "kalemler", TutarTuru.SifirOlabilir, $"{onek} · işçilik");
            // PATCH-SOZLESME tur 3: vezne_fis_kalemleri adet (12,3) · birim fiyat (15,2) — taşarsa 500 değil 400.
            // İşçilik (15,2) ve satır tutarı fiş toplamının parçasıdır; onları aşağıdaki toplam kapısı kapsar.
            KolonTavani.Denetle(adet, 3, KolonTavani.D12_3, "kalemler", $"{i + 1}. kalemde adet");
            KolonTavani.Denetle(fiyat, 2, KolonTavani.D15_2, "kalemler", $"{i + 1}. kalemde birim fiyat");

            hazir.Add(new HazirKalem(k.UrunId!, adet, fiyat, iscilik, i));
        }
        // PATCH-SOZLESME tur 3: vezneleme anında vezne_fisleri.toplam_tl decimal(15,2)'ye damgalanacak
        // toplam (satır = adet × fiyat + işçilik) ŞİMDİ denetlenir — POST ve PATCH aynı kapı.
        KolonTavani.Denetle(Toplam(hazir), 2, KolonTavani.D15_2, "kalemler", "Fiş toplamı");
        return hazir;
    }

    private static decimal Toplam(IReadOnlyCollection<HazirKalem> kalemler) =>
        Yuvarla2(kalemler.Sum(k => Yuvarla2(k.Adet * k.BirimFiyatTl + k.IscilikTl)));

    /// <summary>
    /// P-46 — GÖSTERİLECEK fiş toplamının TEK kaynağı. Kuyruk ve detay bu
    /// yardımcıyı çağırır (kuyruk = detay). ACIK fişte damga henüz yoktur:
    /// kalemliyse kalem toplamı, kalemsizse talep tutarı gösterilir. ACIK
    /// dışındaki her durumda M-V10 damgası döner; kalemler YENİDEN
    /// HESAPLANMAZ (damga, tahsil edilen tutarın kendisidir).
    /// </summary>
    private static decimal GosterilecekToplam(string durum, bool kalemli, decimal? talepTutarTl,
        decimal damgaliToplamTl, IReadOnlyCollection<HazirKalem>? kalemler) =>
        durum == VezneDurum.Acik
            ? (kalemli ? Toplam(kalemler ?? []) : talepTutarTl ?? 0m)
            : damgaliToplamTl;

    /// <summary>P-46: ACIK fişin tutarı damgalanmamıştır — ekran "tahmini" rozetini buna bağlar.</summary>
    private static bool ToplamTahmini(string durum) => durum == VezneDurum.Acik;

    /// <summary>
    /// P-46: verilen fişlerin kalemleri TEK sorguda çekilir, fiş kimliğine göre
    /// gruplanır. Boş listede veritabanına gidilmez.
    /// </summary>
    private async Task<Dictionary<string, List<HazirKalem>>> KalemHaritasi(
        List<string> fisIdler, string tenantId)
    {
        if (fisIdler.Count == 0) return [];
        var satirlar = await db.VezneFisKalemleri.AsNoTracking()
            .Where(k => k.TenantId == tenantId && fisIdler.Contains(k.FisId))
            .OrderBy(k => k.Sira)
            .Select(k => new { k.FisId, k.UrunId, k.Adet, k.BirimFiyatTl, k.IscilikTl, k.Sira })
            // Gerçek üst sınır: fiş başına kalem sayısı GİRİŞTE AzamiKalem ile
            // doğrulanır (KalemleriKur) — Take sessiz kırpma değil, B-8 sınırıdır.
            .Take(fisIdler.Count * VezneFisVarligi.AzamiKalem)
            .ToListAsync();
        return satirlar
            .GroupBy(k => k.FisId)
            .ToDictionary(g => g.Key,
                g => g.Select(k => new HazirKalem(k.UrunId, k.Adet, k.BirimFiyatTl, k.IscilikTl, k.Sira)).ToList());
    }

    private static decimal Yuvarla2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);

    /// <summary>SatisFisi anonim nesne döndürür; fisId JSON üzerinden okunur.</summary>
    private static string FisIdOku(object sonuc)
    {
        var oge = JsonSerializer.SerializeToElement(sonuc);
        if (oge.TryGetProperty("fisId", out var f) && f.ValueKind == JsonValueKind.String)
            return f.GetString()!;
        throw new ApiException(500, "Satış fişi kimliği okunamadı");
    }

    /// <summary>
    /// İYİMSER KİLİT: istemci gördüğü damgayı gönderirse ve kayıt o damgadan
    /// sonra değiştiyse 409. Damga gönderilmezse kural uygulanmaz (eski
    /// istemciler kırılmaz), ama ekran her zaman gönderir.
    /// ⚠ Kind=Local dışında çevirme YAPILMAZ — MySQL damgası Unspecified döner
    /// ve koşulsuz ToUniversalTime() onu +3 saat kaydırırdı (paket 3 tuzağı).
    /// </summary>
    private static void IyimserKilitDogrula(VezneFisVarligi fis, DateTime? damga)
    {
        if (damga is not { } d) return;
        var gelen = DateTime.SpecifyKind(
            d.Kind == DateTimeKind.Local ? d.ToUniversalTime() : d, DateTimeKind.Unspecified);
        var kayitli = DateTime.SpecifyKind(fis.Guncellendi, DateTimeKind.Unspecified);
        if (Math.Abs((kayitli - gelen).TotalMilliseconds) > 1)
            throw new ConflictException(
                "Bu fiş siz görüntüledikten sonra değişti. Sayfayı yenileyip tekrar deneyin.");
    }

    private static DateTime? Damga(JsonElement govde) =>
        Alan(govde, "guncellendi", out var o) && o.ValueKind == JsonValueKind.String
            ? o.GetDateTime()
            : null;

    private static bool Alan(JsonElement govde, string ad, out JsonElement oge)
    {
        foreach (var p in govde.EnumerateObject())
            if (string.Equals(p.Name, ad, StringComparison.OrdinalIgnoreCase))
            {
                oge = p.Value;
                return true;
            }
        oge = default;
        return false;
    }

    private static string? Kirp(string? metin, int azami)
    {
        var t = metin?.Trim();
        if (string.IsNullOrEmpty(t)) return null;
        return t.Length <= azami ? t : t[..azami];
    }
}

// ── Controller: fiş hazırlama (tezgâhtar yolu) ───────────────────────────────

/// <summary>
/// Vezne fişi uçları. Sunucu kapısı: <c>[ModulYetkisi("islem")]</c> — tezgâhtar
/// fiş açar, düzenler, vezneye gönderir ve (kendi fişini) iptal eder.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/vezne")]
// P-09: Vezne KENDİ anahtarını taşır; eskiden yarısı "islem" yarısı "kasa" idi.
[Yetki.ModulYetkisi("vezne")]
public sealed class VezneController(VezneServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/vezne/fisler — Fiş aç (ACIK doğar)</summary>
    [HttpPost("fisler")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<object> Olustur([FromBody] VezneFisOlusturDto dto) => servis.Olustur(dto, Islem);

    /// <summary>GET /api/v1/vezne/kuyruk — Bekleyen fiş kuyruğu (yalnız kendi şubesi)</summary>
    [HttpGet("kuyruk")]
    public Task<object> Kuyruk([FromQuery] string? durum, [FromQuery] int? limit)
        => servis.Kuyruk(durum, limit, Islem);

    /// <summary>GET /api/v1/vezne/fisler/{id} — Fiş başlığı + kalemleri</summary>
    [HttpGet("fisler/{id}")]
    public Task<object> Detay(string id) => servis.Detay(id, Islem);

    /// <summary>PATCH /api/v1/vezne/fisler/{id} — Yalnız ACIK fiş düzenlenir</summary>
    [HttpPatch("fisler/{id}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<object> Guncelle(string id, [FromBody] JsonElement govde)
        => servis.Guncelle(id, govde, Islem);

    /// <summary>POST /api/v1/vezne/fisler/{id}/veznele — ACIK → VEZNEDE (tutar damgalanır)</summary>
    [HttpPost("fisler/{id}/veznele")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<object> Veznele(string id) => servis.Veznele(id, Islem);

    /// <summary>POST /api/v1/vezne/fisler/{id}/iptal — Gerekçeli iptal (kayıt SİLİNMEZ)</summary>
    [HttpPost("fisler/{id}/iptal")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<object> Iptal(string id, [FromBody] VezneIptalDto dto) => servis.Iptal(id, dto, Islem);
}

// ── Controller: tahsil (vezne yolu) ──────────────────────────────────────────

/// <summary>
/// TAHSİL UCU AYRI CONTROLLER'DADIR (Mehmet koşulu): ModulYetkisiAttribute'un
/// metot düzeyinde sınıf düzeyini geçersiz kıldığı DOĞRULANMADI. Para ucu bu
/// yüzden kendi sınıfında ve kendi kapısındadır: <c>[ModulYetkisi("kasa")]</c>.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/vezne")]
// ⚠ P-09 DÜZELTMESİ: bu sınıf "vezne" anahtarına çevrilmişti; o bir GENİŞLEME
//   idi — "vezne" yetkisi olup "kasa" yetkisi OLMAYAN bir kullanıcı parayı
//   tahsil edebiliyordu. Sınıfın kendi doc-comment'i ve Mehmet'in koşulu
//   "kasa" diyor; anahtar geri alındı. Fiş uçları "vezne", PARA ucu "kasa".
[Yetki.ModulYetkisi("kasa")]
public sealed class VezneTahsilController(VezneServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/vezne/fisler/{id}/tahsil — Para el değiştirir</summary>
    [HttpPost("fisler/{id}/tahsil")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<object> Tahsil(string id, [FromBody] VezneTahsilDto dto)
        => servis.Tahsil(id, dto, Islem);
}
