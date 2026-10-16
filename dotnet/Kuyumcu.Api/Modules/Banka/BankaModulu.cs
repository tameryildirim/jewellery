using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Banka;

// ═════════════════════════════════════════════════════════════════════════════
// Banka modülü — KasaModulu.cs desenini birebir izler.
// Kaynak: apps/api/src/modules/banka/*
//   • Manuel dekont girişi (canlı banka entegrasyonu gelene kadar)
//   • GELEN hareketleri satış faturasına dönüştürme (FTR-{yıl}-{sıra})
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class HesapOlusturDto
{
    [Required, StringLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.BankaAdi, MinimumLength = 2, ErrorMessage = "Banka adı 2-100 karakter olmalıdır")]
    public string BankaAdi { get; set; } = null!;

    [Required, StringLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.BankaAdi, MinimumLength = 2, ErrorMessage = "Hesap adı 2-100 karakter olmalıdır")]
    public string HesapAdi { get; set; } = null!;

    /// <summary>Hesap sahibinin adı/ünvanı (kişi ya da firma) — opsiyonel</summary>
    [StringLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.HesapSahibi, MinimumLength = 2, ErrorMessage = "Hesap sahibi 2-150 karakter olmalıdır")]
    [Unvan]   // PAKET GOC-A (K20): en az bir harf — ekran Unvan bileşeniyle AYNI kural
    public string? HesapSahibi { get; set; }

    // PAKET GIRDI-AILESI-2: IBAN kuralı Core'daki TEK kaynaktan (TCMB 2008/6 md.4 + MOD 97-10);
    // eski [RegularExpression] kontrol hanesine bakmıyordu (envanter açık #1).
    [Required, Iban]
    public string Iban { get; set; } = null!;

    [RegularExpression("TRY|USD|EUR|GBP")]
    public string? ParaBirimi { get; set; }
}

public sealed class HesapGuncelleDto
{
    [StringLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.BankaAdi, MinimumLength = 2)] public string? BankaAdi { get; set; }
    [StringLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.BankaAdi, MinimumLength = 2)] public string? HesapAdi { get; set; }
    /// <summary>Boş string gönderilirse sahip temizlenir</summary>
    [StringLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.HesapSahibi), Unvan] public string? HesapSahibi { get; set; }

    [Iban]
    public string? Iban { get; set; }

    [RegularExpression("TRY|USD|EUR|GBP")] public string? ParaBirimi { get; set; }
    public bool? Aktif { get; set; }
}

public sealed class HareketEkleDto
{
    /// <summary>
    /// Banka hareketi yönü. UX-4d madde 6: kabul edilen değerler TEK KAYNAKtan
    /// (Kuyumcu.Core HareketYonu) gelir. "GIRIS" gönderilirse sessizce
    /// eşlenmez — 400 döner ve mesaj doğru değerleri listeler.
    /// </summary>
    [Required(ErrorMessage = "Yön zorunludur")]
    [RegularExpression(Kuyumcu.Core.Domain.HareketYonu.BankaDesen,
        ErrorMessage = Kuyumcu.Core.Domain.HareketYonu.BankaMesaj)]
    public string Yon { get; set; } = null!;

    // SON-KALANLAR tur 2 (doğrulayıcı DÜŞÜK): alt sınır TEK kapıda — servis KolonTavani.PozitifTutar
    // (önce 2 haneye yuvarla, sonra en az 0,01). DTO [Range(0.01)] HTTP yolunda 0,005'i servisten önce
    // reddediyor, servis yolu ve tahsilat 0,01 olarak kabul ediyordu (uçlar arası tutarsızlık).
    [Required]
    public decimal Tutar { get; set; }

    [StringLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.HesapSahibi, MinimumLength = 1)]
    [Unvan]   // PAKET GOC-A (K20): "en az bir harf / yalnız rakam olamaz" iki tarafta da yoktu
    public string? KarsiTarafAd { get; set; }

    // PAKET GOC-A (GIRDI-AILESI-2 kapanış DÜŞÜK devri): karşı IBAN yalnız uzunluk taşıyordu —
    // Core IBAN kuralı (TCMB 2008/6 md.4 + MOD 97-10) ile aynı öznitelik.
    [Iban]
    public string? KarsiIban { get; set; }

    // GOC-A: POST'ta sınır yoktu (envanter C · dekont-aciklama) — ekran ile AYNI sabit.
    [StringLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.DekontAciklama)]
    public string? Aciklama { get; set; }

    /// <summary>Bilinen bir cariye doğrudan eşleştir</summary>
    [StringLength(36, MinimumLength = 26)]
    public string? CariId { get; set; }

    /// <summary>ISO tarih; verilmezse şimdi</summary>
    public string? Tarih { get; set; }

    /// <summary>
    /// MASAK kimlik/beyan bloğu (paket 41) — satış DTO'larındakiyle AYNI tip.
    /// Eşik altında boş kalır ve mevcut çağrılar kırılmaz; eşik üstünde
    /// (kümülatif dâhil) eksikse istek 400 döner ve HİÇBİR satır yazılmaz.
    /// </summary>
    public Modules.Masak.MasakBilgiDto? Masak { get; set; }
}

/// <summary>
/// PAKET P-12 (B-42) — banka dekontunun FİNANSAL OLMAYAN alanlarının düzeltmesi.
/// <para>
/// ⚠ Burada tutar/yön/para birimi/hesap ALANI YOKTUR ve eklenmeyecektir:
/// finansal düzeltmenin tek yolu STORNO + yeni dekonttur (PLAN § 1 md.8,
/// denetim.sh <c>STORNO_DISI_FINANSAL_PATCH</c>).
/// </para>
/// <para>⚠ Üç durumlu PATCH: gönderilmeyen alan "değişmedi", null "sil" demektir.</para>
/// </summary>
public sealed class HareketDuzeltDto
{
    private string? aciklama;
    [System.Text.Json.Serialization.JsonIgnore] public bool AciklamaVerildi { get; private set; }
    [StringLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.DekontAciklamaDuzelt)]
    public string? Aciklama { get => aciklama; set { aciklama = value; AciklamaVerildi = true; } }

    private string? karsiTarafAd;
    [System.Text.Json.Serialization.JsonIgnore] public bool KarsiTarafAdVerildi { get; private set; }
    [StringLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.HesapSahibi), Unvan]   // GOC-A K20
    public string? KarsiTarafAd { get => karsiTarafAd; set { karsiTarafAd = value; KarsiTarafAdVerildi = true; } }

    private string? karsiIban;
    [System.Text.Json.Serialization.JsonIgnore] public bool KarsiIbanVerildi { get; private set; }
    [Iban]   // GOC-A: Core IBAN kuralı (yalnız uzunluk taşıyordu)
    public string? KarsiIban { get => karsiIban; set { karsiIban = value; KarsiIbanVerildi = true; } }

    private DateTime? tarih;
    [System.Text.Json.Serialization.JsonIgnore] public bool TarihVerildi { get; private set; }
    public DateTime? Tarih { get => tarih; set { tarih = value; TarihVerildi = true; } }
}

/// <summary>
/// PAKET Q9-1 (QA turu 9 KRİTİK: "dekont Bekliyor durumundan çıkarılamıyor") —
/// bekleyen dekontu bir CARİYLE eşleştirir (durum BEKLIYOR → ESLESTI).
/// <para>
/// ⚠ BİLİNÇLİ OLARAK <see cref="HareketDuzeltDto"/>'ya EKLENMEDİ: CariId
/// denetim.sh <c>STORNO_DISI_FINANSAL_PATCH</c> listesindedir (cari bağı MASAK
/// bağlantılı işlem toplamını değiştirir). Ayrı uç, ayrı durum makinesi,
/// ayrı MASAK kapısı.
/// </para>
/// </summary>
public sealed class DekontEslestirDto
{
    [Required(ErrorMessage = "Eşleştirilecek müşteri seçilmelidir")]
    [StringLength(36, MinimumLength = 26)]
    public string CariId { get; set; } = null!;
}

public sealed class FaturayaDonusturDto
{
    /// <summary>Fatura kesilecek cari — hareket zaten eşleşmişse opsiyonel</summary>
    [StringLength(36, MinimumLength = 26)]
    public string? CariId { get; set; }

    /// <summary>KDV oranı (0, 0.01, 0.10, 0.20) — tutar KDV dahil kabul edilir</summary>
    [Range(0, 1)]
    public decimal? KdvOran { get; set; }

    [RegularExpression("SATIS|ALIS")]
    public string? Tur { get; set; }

    public string? Aciklama { get; set; }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class BankaServisi(
    KuyumcuDbContext db,
    DenetimServisi? denetim = null,
    Modules.Fatura.KdvAyarlari? kdvAyar = null,
    Modules.Masak.MasakKuralMotoru? masakMotoru = null)
{
    /// <summary>
    /// MASAK kapısı (paket 41). DI gerçek motoru verir; testlerdeki
    /// <c>new BankaServisi(db)</c> VARSAYILAN eşiklerle kurulu motoru alır.
    /// "Motor yoksa kontrol yok" YAZILMADI — o bir güvenlik deliği olurdu
    /// (SatisServisi ile birebir aynı desen).
    /// </summary>
    private readonly Modules.Masak.MasakKapisi masak =
        masakMotoru is null ? Modules.Masak.MasakKapisi.Varsayilan() : new(masakMotoru);

    /// <summary>
    /// KDV oranı FaturaServisi ile AYNI kaynaktan okunur (ADR-0005). Opsiyoneldir
    /// ve null'da varsayılan verir — mevcut <c>new BankaServisi(db)</c> çağrıları
    /// (MutabakatTestleri vb.) KIRILMAZ, davranış bugünküyle aynıdır.
    /// </summary>
    private readonly Modules.Fatura.KdvAyarlari kdvAyarlari =
        kdvAyar ?? new Modules.Fatura.KdvAyarlari();

    // ─── Hesap oluştur ────────────────────────────────────────────────────────

    public async Task<object> HesapOlustur(HesapOlusturDto dto, AktifKullanici islem)
    {
        // PAKET PATCH-5: para birimi servis içinde de listeden ("" RegularExpression'dan geçip hesaba yazılıyordu).
        dto.ParaBirimi = KolonTavani.ParaBirimi(string.IsNullOrEmpty(dto.ParaBirimi) ? null : dto.ParaBirimi, "paraBirimi");
        // GIRDI-AILESI-2: servis yolu da AYNI Core kuralını çağırır (öznitelik yalnız controller yolunda çalışır).
        dto.Iban = IbanDenetle(dto.Iban) ?? throw new BadRequestException("IBAN zorunludur").AlanIle("iban");
        var mevcut = await db.BankaHesaplari.AnyAsync(h =>
            h.TenantId == islem.TenantId && h.Iban == dto.Iban);
        if (mevcut) throw new ConflictException("Bu IBAN zaten kayıtlı");

        var now = DateTime.UtcNow;
        var hesap = new BankaHesap
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            BankaAdi = dto.BankaAdi,
            HesapAdi = dto.HesapAdi,
            HesapSahibi = string.IsNullOrWhiteSpace(dto.HesapSahibi) ? null : dto.HesapSahibi.Trim(),
            Iban = dto.Iban,
            ParaBirimi = dto.ParaBirimi ?? "TRY",
            Bakiye = 0m,
            Aktif = true,
            Olusturuldu = now,
            Guncellendi = now,
        };
        db.BankaHesaplari.Add(hesap);
        await db.SaveChangesAsync();

        return await HesapGetir(hesap.Id, islem);
    }

    /// <summary>
    /// GIRDI-AILESI-2 — IBAN'ı kayıt biçimine getirir (boşluksuz, büyük harf) ve Core kuralıyla
    /// denetler; bozuksa sözlük metniyle 400 (alan "iban"). Boş → null.
    /// </summary>
    private static string? IbanDenetle(string? ham, string alan = "iban")
    {
        if (string.IsNullOrWhiteSpace(ham)) return null;
        if (Kuyumcu.Core.Domain.Girdi.IbanDogrulayici.Denetle(ham) is { } h)
            throw new BadRequestException(Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(h, Kuyumcu.Core.I18n.IstekDili.Gecerli)).AlanIle(alan);
        return Kuyumcu.Core.Domain.Girdi.IbanDogrulayici.Normallestir(ham);
    }

    // ─── Hesap güncelle ───────────────────────────────────────────────────────
    /// <summary>Yalnız gönderilen alanlar güncellenir. IBAN değişirse benzersizlik doğrulanır.</summary>
    public async Task<object> HesapGuncelle(string id, HesapGuncelleDto dto, AktifKullanici islem)
    {
        // PAKET PATCH-5 (PATCH-3 sözleşmesi): zorunlu alan null = KORU, "" / boşluk = 400 alan (eskiden
        // para birimi "" hesaba yazılıyor, IBAN "" sessizce yok sayılıyor, "  " banka adı yazılıyordu).
        var hesap = await TenantHesabiBul(id, islem.TenantId);
        KolonTavani.ParaBirimi(dto.ParaBirimi, "paraBirimi");
        if (dto.Iban is not null && string.IsNullOrWhiteSpace(dto.Iban))   // GIRDI-AILESI-2 tur 2: "   " de 400 (eskiden 200 + sessiz yok sayma)
            throw new BadRequestException("IBAN boşaltılamaz").AlanIle("iban");
        if (!string.IsNullOrEmpty(dto.Iban)) dto.Iban = IbanDenetle(dto.Iban);
        if (dto.BankaAdi is not null)
        {
            dto.BankaAdi = dto.BankaAdi.Trim();
            if (dto.BankaAdi.Length < 2) throw new BadRequestException("Banka adı 2-100 karakter olmalıdır").AlanIle("bankaAdi");
            KolonTavani.MetinSiniri(dto.BankaAdi, 100, "bankaAdi", "Banka adı");
        }
        if (dto.HesapAdi is not null)
        {
            dto.HesapAdi = dto.HesapAdi.Trim();
            if (dto.HesapAdi.Length < 2) throw new BadRequestException("Hesap adı 2-100 karakter olmalıdır").AlanIle("hesapAdi");
            KolonTavani.MetinSiniri(dto.HesapAdi, 100, "hesapAdi", "Hesap adı");
        }
        KolonTavani.MetinSiniri(dto.HesapSahibi?.Trim(), 150, "hesapSahibi", "Hesap sahibi");
        if (!string.IsNullOrEmpty(dto.Iban) && dto.Iban != hesap.Iban)
        {
            var cakisan = await db.BankaHesaplari.AnyAsync(h =>
                h.TenantId == islem.TenantId && h.Iban == dto.Iban && h.Id != id);
            if (cakisan) throw new ConflictException("Bu IBAN zaten kayıtlı");
            hesap.Iban = dto.Iban;
        }

        if (dto.BankaAdi is not null) hesap.BankaAdi = dto.BankaAdi;
        if (dto.HesapAdi is not null) hesap.HesapAdi = dto.HesapAdi;
        // Boş string → sahip temizlenir; null → dokunulmaz
        if (dto.HesapSahibi is not null)
            hesap.HesapSahibi = string.IsNullOrWhiteSpace(dto.HesapSahibi) ? null : dto.HesapSahibi.Trim();
        if (dto.ParaBirimi is not null && dto.ParaBirimi != hesap.ParaBirimi)
        {
            // PAKET SON-KALANLAR (hasım D-P5-1, ORTA): hareketi/bakiyesi olan hesabın para birimi
            // DEĞİŞMEZ — Kasa GecmisiBosMu (Mehmet K4) kuralının eşi. Eskiden USD 100 bakiyeli hesap
            // PATCH {"paraBirimi":"TRY"} ile sessizce TRY 100 oluyordu. Kapı hiçbir yazmadan ÖNCE.
            await ParaBirimiKilidi(hesap, islem);
            hesap.ParaBirimi = dto.ParaBirimi;
        }
        if (dto.Aktif is { } yeniAktif && yeniAktif != hesap.Aktif)
        {
            // Pasife alma kapısı DELETE ile AYNI kuraldır (Mehmet K5) —
            // ikinci bir kural yazılmaz, aynı metot çağrılır.
            if (!yeniAktif) await KapatmaKapisi(hesap, islem);
            hesap.Aktif = yeniAktif;
        }
        hesap.Guncellendi = DateTime.UtcNow;
        denetim?.YazmaKaydet(islem, nameof(BankaHesap), hesap.Id,
            "banka hesabı güncellendi", hassas: false);
        await db.SaveChangesAsync();

        return await HesapGetir(id, islem);
    }

    // ─── Hesap pasife alma ────────────────────────────────────────────────────

    /// <summary>
    /// DELETE /bankalar/{id} — hesabı SİLMEZ, pasife alır.
    /// <para>
    /// ⚠ Mehmet K5 / MÜHÜR 2: banka hesabı finansal bir varlıktır; satır
    /// silinmez (dekontlar hesaba bağlıdır, hesap kaybolursa mutabakat kırılır).
    /// Bakiyesi ≠ 0 ya da hareketi olan hesap KAPATILAMAZ → 409.
    /// Geri dönüş yolu: PATCH { aktif: true } (UX-S25).
    /// </para>
    /// </summary>
    public async Task<object> HesapPasifeAl(string id, AktifKullanici islem)
    {
        var hesap = await TenantHesabiBul(id, islem.TenantId);

        if (hesap.Aktif)
        {
            await KapatmaKapisi(hesap, islem);
            hesap.Aktif = false;
            hesap.Guncellendi = DateTime.UtcNow;
            denetim?.YazmaKaydet(islem, nameof(BankaHesap), hesap.Id,
                "banka hesabı pasife alındı", hassas: false);
            await db.SaveChangesAsync();
        }
        return new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Banka hesabı pasife alındı"), aktif = false };
    }

    /// <summary>
    /// PAKET SON-KALANLAR — para birimi değişim kilidi (Kasa <c>GecmisiBosMu</c> eşi): bakiye 0 ve
    /// banka_hareketleri boş olmalı; değilse 409 alan:paraBirimi (geçmiş dekontların anlamı bozulmaz).
    /// </summary>
    private async Task ParaBirimiKilidi(BankaHesap hesap, AktifKullanici islem)
    {
        if (hesap.Bakiye != 0m)
            throw new ConflictException(
                "Bakiyesi sıfır olmayan banka hesabının para birimi değiştirilemez — yeni hesap açın")
                .AlanIle("paraBirimi");
        if (await db.BankaHareketleri.AsNoTracking()
                .AnyAsync(h => h.TenantId == islem.TenantId && h.BankaHesapId == hesap.Id))
            throw new ConflictException(
                "Hareket görmüş banka hesabının para birimi değiştirilemez — yeni hesap açın")
                .AlanIle("paraBirimi");
    }

    private async Task KapatmaKapisi(BankaHesap hesap, AktifKullanici islem)
    {
        if (hesap.Bakiye != 0m)
            throw new ConflictException("Bakiyesi sıfır olmayan banka hesabı kapatılamaz");

        if (await db.BankaHareketleri.AsNoTracking()
                .AnyAsync(h => h.TenantId == islem.TenantId && h.BankaHesapId == hesap.Id))
            throw new ConflictException(
                "Hareket görmüş banka hesabı kapatılamaz — dekontlar hesaba bağlıdır");
    }

    // ─── Hesap listesi ────────────────────────────────────────────────────────

    public async Task<object> HesapListele(AktifKullanici islem)
    {
        var hesaplar = await db.BankaHesaplari.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId)
            .OrderByDescending(h => h.Olusturuldu)
            .ToListAsync();
        return new { hesaplar };
    }

    public async Task<object> HesapGetir(string id, AktifKullanici islem)
    {
        var hesap = await db.BankaHesaplari.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id && h.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Banka hesabı bulunamadı");
        return hesap;
    }

    // ─── Hareket ekle ─────────────────────────────────────────────────────────
    /// <summary>
    /// Manuel dekont girişi (canlı banka entegrasyonu gelene kadar).
    /// GELEN → bakiye artar, GIDEN → bakiye azalır.
    ///
    /// ⚠ PAKET 41 — MASAK KAPISI İKİ FAZLI olarak buraya bağlıdır:
    ///   FAZ 1 <c>DogrulaKumulatif</c> → cari doğrulamasından SONRA, transaction
    ///         AÇILMADAN önce (eksik kimlikle hiçbir yazma başlamasın).
    ///   FAZ 2 <c>Kaydet</c> → BankaHareketleri.Add'den sonra, SaveChanges'ten
    ///         önce, AYNI transaction içinde ("dekont var, tespit yok" ara
    ///         durumu doğmasın).
    ///
    /// ⚠ TEVSİK KAPISI BİLİNÇLİ OLARAK ÇAĞRILMAZ: banka hareketi tanımı gereği
    /// banka kanalıdır; 459 s. VUK Genel Tebliği'nin istediği tevsik zaten
    /// sağlanmıştır ve nakit payı 0 olduğu için <c>GunlukNakitToplam</c>'a da
    /// girmez. Bu kapıyı GEVŞETEN değil, kapsamını doğru çizen bir karardır —
    /// sonraki oturum "unutulmuş" sanıp bağlamasın diye burada yazılıdır.
    /// Gün kilidi (GunKapanisiKapisi) ve vade kapısı da bu pakette BAĞLANMADI:
    /// ayrı kararlar, ayrı paketler.
    /// </summary>
    public Task<object> HareketEkle(string hesapId, HareketEkleDto dto, AktifKullanici islem)
    {
        // PAKET GOC-A — kullanıcı girdisi kapısı (servis yolu; DataAnnotations yalnız controller'da):
        // karşı IBAN Core kuralı + kayıt biçimi (boşluksuz, büyük harf), açıklama ekran sınırı,
        // dekont tarihi K8a (ileri gün yok, en çok 1 yıl geri — TarihPenceresi.Islem).
        // ⚠ Ekstre onayı (HareketEkleIc'e doğrudan girer) bankanın gönderdiği veriyi taşır; ona
        //   uygulanmaz — ekstre satırındaki karşı IBAN/açıklama bankanın belgesidir, kullanıcı girdisi değil.
        dto.KarsiIban = IbanDenetle(dto.KarsiIban, "karsiIban");
        GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.AdKurali.Unvan(dto.KarsiTarafAd), "karsiTarafAd");
        dto.Aciklama = GirdiKapisi.Uzunluk(dto.Aciklama, Kuyumcu.Core.Domain.Girdi.AlanSinirlari.DekontAciklama, "aciklama");
        if (!string.IsNullOrEmpty(dto.Tarih)
            && DateTime.TryParse(dto.Tarih, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t))
            GirdiKapisi.IslemTarihi(Zaman.YerelBugun(t), "tarih");
        return HareketEkleIc(hesapId, dto, islem);
    }

    /// <summary>
    /// ⚠ BANKA DEFTERİNE YAZAN TEK KAPI (K4-OK-2, paket 32).
    /// <para>
    /// Genel uç buraya <paramref name="ekstreSatirId"/> = null ile girer;
    /// ekstre içe aktarımı (EkstreServisi.Onayla) sahne satırının kimliğiyle.
    /// Ekstre servisi KENDİ <c>BankaHareketleri.Add</c>'ini AÇMAZ — açsaydı
    /// MASAK kapısı (yukarıdaki iki faz) ve ileri tarih kontrolü toplu onayda
    /// sessizce atlanırdı. denetim.sh "BankaHareketleri.Add" sayacı bu kuralı
    /// CI'da zorlar.
    /// </para>
    /// <para>⚠ DAVRANIŞ DEĞİŞMEDİ: paket 32 bu gövdeyi yalnız TAŞIDI; yön
    /// katsayısı, bakiye güncellemesi ve MASAK faz sırası birebir aynıdır.</para>
    /// </summary>
    public async Task<object> HareketEkleIc(
        string hesapId, HareketEkleDto dto, AktifKullanici islem,
        string? ekstreSatirId = null, string? bankaReferans = null)
    {
        // PAKET PATCH-5 — GİRDİ kapısı (servis içi; ekstre onayı da bu kapıdan geçer): tutar önce 2 haneye
        // yuvarlanır sonra ≤ 0 (0,004 → 0,00 dekont doğmaz; bakiye ile satır aynı değeri alır) + kolon tavanı;
        // karşı taraf adı varchar(150) (1406 → 500 olurdu); tarih okunamazsa 400 alan:tarih (FormatException → 500 idi).
        // GIRDI-AILESI-2 tur 3 (doğrulayıcı YÜKSEK · K9): 2'den fazla ondalık SESSİZCE YUVARLANMAZ — ekrandaki
        // Para bileşeniyle TEK kural (Core SayiKurali.Ondalik) ve AYNI metin; "1234,5678" → 400 alan:tutar.
        if (Kuyumcu.Core.Domain.Girdi.SayiKurali.Ondalik(dto.Tutar, Kuyumcu.Core.Domain.Girdi.TutarKurali.OndalikHane) is { } ondalikHatasi)
            throw new BadRequestException(Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(ondalikHatasi, Kuyumcu.Core.I18n.IstekDili.Gecerli)).AlanIle("tutar");
        dto.Tutar = KolonTavani.PozitifTutar(dto.Tutar, "tutar", "Tutar");
        KolonTavani.MetinSiniri(dto.KarsiTarafAd, 150, "karsiTarafAd", "Karşı taraf adı");
        var hesap = await TenantHesabiBul(hesapId, islem.TenantId);

        if (!string.IsNullOrEmpty(dto.CariId))
        {
            var cariVar = await db.Cariler.AnyAsync(c =>
                c.Id == dto.CariId && c.TenantId == islem.TenantId);
            if (!cariVar) throw new NotFoundException("Cari bulunamadı");
        }

        var yonKatsayi = dto.Yon == Kuyumcu.Core.Domain.HareketYonu.BankaGelen ? 1m : -1m;
        var now = DateTime.UtcNow;
        var hareketId = Kimlik.Ulid();
        var tarih = now;
        if (!string.IsNullOrEmpty(dto.Tarih)
            && !DateTime.TryParse(dto.Tarih, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out tarih))
            throw new BadRequestException("Tarih okunamadı — YYYY-AA-GG biçiminde gönderin").AlanIle("tarih");
        KolonTavani.BakiyeSigar(hesap.Bakiye + dto.Tutar * yonKatsayi, "tutar", "Tutar");   // PATCH-5

        // ⚠ K41-1 (Mehmet, şartlı veto): İLERİ TARİHLİ DEKONT REDDEDİLİR.
        // Dekont tarihi kullanıcı girdisidir. Gelecek tarih kabul edilseydi
        // kayıt anı (olusturuldu) ile belge tarihi arasındaki fark mutabakatı
        // bozar ve "yarın" tarihli dekontlarla ekstre uydurulabilirdi.
        if (tarih > now.AddMinutes(5))
            throw new BadRequestException("İleri tarihli dekont kabul edilmez");

        // ── MASAK FAZ 1 — transaction AÇILMADAN ÖNCE (fail-closed) ──────────
        // ⚠ Kümülatif ölçüm SUNUCU SAATİYLE (`now`) yapılır, dto.Tarih ile DEĞİL
        // (K41-1). Kullanıcı girdisi tarihe bağlansaydı 100.000 TL "dün",
        // 100.000 TL "bugün" tarihiyle girilerek o günkü kimlik eşiği (ör. 185.000) hiç tetiklenmeden
        // aşılırdı — eşik bölme (structuring) yolunu kapının kendisi açardı.
        var masakGerek = await masak.DogrulaKumulatif(
            db, islem.TenantId, dto.CariId, dto.Tutar, dto.Masak, now);

        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, dto.CariId, now);   // PAKET MASAK-ESZAMAN: işlem + CARİ SATIRI KİLİDİ (sıra: cari → hesap)
        masakGerek = await masak.KilitAltindaYinele(db, islem.TenantId, dto.CariId, dto.Tutar, dto.Masak, now, masakGerek);
        await BakiyeKilidi.Kilitle(db, hesap);   // PAKET KAPORA-ESZAMAN tur 2: kilitli taze okuma (kayıp güncelleme)
        hesap.Bakiye += dto.Tutar * yonKatsayi;
        hesap.Guncellendi = now;

        db.BankaHareketleri.Add(new BankaHareket
        {
            Id = hareketId,
            TenantId = islem.TenantId,
            BankaHesapId = hesapId,
            Yon = dto.Yon,
            Tutar = dto.Tutar,
            ParaBirimi = hesap.ParaBirimi,
            KarsiTarafAd = dto.KarsiTarafAd,
            KarsiIban = dto.KarsiIban,
            Aciklama = dto.Aciklama,
            CariId = string.IsNullOrEmpty(dto.CariId) ? null : dto.CariId,
            // ⚠ PAKET 41: elle girilen dekontun defter karşılığı YOKTUR; AÇIKÇA
            // null yazılır. MASAK kümülatifi banka kümesini `hareket_id IS NULL`
            // süzgeciyle okur — süzgeç ilk günden kodda olmalıdır (Entities.cs).
            HareketId = null,
            Durum = string.IsNullOrEmpty(dto.CariId) ? "BEKLIYOR" : "ESLESTI",
            Tarih = tarih,
            Olusturuldu = now,
            // ── PAKET 32 — MÜKERRER ANAHTARI ELLE GİRİŞTE DE DOLDURULUR ─────
            // Yalnız ekstreden gelenler anahtarlansaydı, en sık mükerrer
            // senaryosu (aynı havaleyi önce elle gir, sonra dosyadan al)
            // hiç yakalanmazdı.
            EkstreSatirId = ekstreSatirId,
            BankaReferans = bankaReferans,
            DedupeAnahtar = DedupeAnahtari.Uret(
                hesapId, tarih, dto.Tutar, dto.Yon, bankaReferans, dto.KarsiIban, dto.Aciklama),
        });

        // ── MASAK FAZ 2 — Add'den SONRA, SaveChanges'ten ÖNCE, tx İÇİNDE ────
        // Dekont ile kimlik tespiti AYNI commit'te doğar.
        masak.Kaydet(db, masakGerek, hareketId, dto.CariId, dto.Tutar, dto.Masak,
            islem, now, MasakKaynakTuru.BankaHareket);

        // ── PAKET 32 — MÜKERRER SON SAVUNMASI ───────────────────────────────
        // (tenant_id, banka_hesap_id, dedupe_anahtar) BENZERSİZ indeksi burada
        // devreye girer: iki eşzamanlı istek aynı dekontu iki kez yazamaz.
        // ⚠ AYNI GÜN AYNI TUTARDA İKİ MEŞRU HAVALE mümkündür; o durumda banka
        //   referansı ya da açıklama anahtarı değiştirir — mesaj bunu SÖYLER,
        //   kullanıcı çıkmaz sokakta bırakılmaz.
        try
        {
            await db.SaveChangesAsync();
            if (tx is not null) await tx.CommitAsync();
        }
        catch (DbUpdateException ex) when (MukerrerMi(ex))
        {
            if (tx is not null) await tx.RollbackAsync();
            throw new ConflictException(
                "Bu hesapta aynı tarih, tutar, yön ve açıklamaya sahip bir hareket zaten var " +
                "(mükerrer koruması). Gerçekten ikinci bir işlemse banka referansını ya da " +
                "açıklamayı ayırt edici biçimde doldurun.");
        }

        return new { hareketId, hesap = await HesapGetir(hesapId, islem) };
    }

    /// <summary>MySQL benzersizlik ihlali (1062) mi — başka bir DB hatası 409'a çevrilmez.</summary>
    internal static bool MukerrerMi(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase) == true;

    // ─── Hareket listesi ──────────────────────────────────────────────────────

    public async Task<object> HareketListele(
        string hesapId, AktifKullanici islem, string? durum, int limit, int offset,
        bool posEslesmemis = false)
    {
        var hesap = await HesapGetir(hesapId, islem);

        var q = db.BankaHareketleri.AsNoTracking()
            .Where(h => h.BankaHesapId == hesapId && h.TenantId == islem.TenantId);
        if (durum is "BEKLIYOR" or "ESLESTI" or "FATURALANDI")
            q = q.Where(h => h.Durum == durum);
        // PAKET 31 Faz B — POS yatması aranan hareketler: GELEN + henüz eşleşmemiş.
        // HareketEkle davranışı DEĞİŞMEDİ; bu yalnız bir okuma süzgecidir.
        if (posEslesmemis)
            q = q.Where(h => h.Yon == "GELEN" && h.Durum == "BEKLIYOR");

        var hareketler = await q.OrderByDescending(h => h.Tarih)
            .Skip(offset).Take(limit).ToListAsync();

        return new { hesap, hareketler };
    }

    // ─── Faturaya dönüştür ────────────────────────────────────────────────────
    /// <summary>
    /// GELEN banka hareketini satış faturasına çevirir:
    ///  - Tutar KDV dahil kabul edilir: araToplam = toplam / (1 + kdvOran)
    ///  - Fatura no: FTR-&lt;yıl&gt;-&lt;sıra&gt; (tenant bazında)
    ///  - Hareket FATURALANDI durumuna geçer ve faturaya bağlanır
    ///
    /// ⚠ PAKET 41 — MASAK KAPISI BİLİNÇLİ OLARAK EKLENMEDİ: bu uçta PARA EL
    /// DEĞİŞTİRMEZ, yalnız belge üretilir. Paranın el değiştirdiği an dekontun
    /// girildiği andır ve kapı ORADA çalışır (<see cref="HareketEkle"/>).
    /// Buraya da eklenseydi aynı para iki kez eşiğe sayılırdı. Sonraki oturum
    /// "unutulmuş" sanıp bağlamasın diye gerekçesiyle yazılıdır.
    /// </summary>
    public async Task<object> FaturayaDonustur(string hareketId, FaturayaDonusturDto dto, AktifKullanici islem)
    {
        var hareket = await db.BankaHareketleri
            .FirstOrDefaultAsync(h => h.Id == hareketId && h.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Banka hareketi bulunamadı");
        if (hareket.Durum == "FATURALANDI")
            throw new ConflictException("Bu hareket zaten faturalandı");
        if (hareket.Yon != "GELEN")
            throw new BadRequestException("Yalnızca GELEN hareketler faturaya dönüştürülebilir");
        // PAKET P-19 (Y-11): virman bacağı FATURALANMAZ. Para dükkânın içinde yer
        // değiştirmiştir; müşteri yoktur, satış yoktur. Faturalansaydı aynı para
        // hem virman hem hasılat sayılır ve ciro şişerdi.
        if (hareket.KarsiBacakId is not null)
            throw new BadRequestException(
                "Bu dekont bir VİRMANIN bacağıdır (para hesaplar arasında taşındı, dışarıdan "
                + "gelmedi); faturaya dönüştürülemez.");

        var cariId = dto.CariId ?? hareket.CariId;
        if (string.IsNullOrEmpty(cariId))
            throw new BadRequestException("Hareket bir cariye eşleşmemiş; cariId gönderiniz");

        var cari = await db.Cariler.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cariId && c.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Cari bulunamadı");

        // ADR-0005: oran koda gömülü DEĞİL — FaturaServisi ile AYNI ayardan.
        // PAKET PATCH-5: faturalar.kdv_oran 4 ondalık; ara toplam/KDV hesabı ham oranla yapılıp oran 4 haneye
        // yuvarlanarak yazılıyordu (hesap ile kayıt farklı oran). Fazla hane reddedilir; servis içi 0..1.
        if (dto.KdvOran is < 0m or > 1m)
            throw new BadRequestException("KDV oranı 0 ile 1 arasında olmalıdır").AlanIle("kdvOran");
        KolonTavani.OndalikSiniri(dto.KdvOran, 4, "kdvOran", "KDV oranı");
        var kdvOran = dto.KdvOran ?? kdvAyarlari.VarsayilanOran;
        var toplam = hareket.Tutar;
        var araToplam = toplam / (1m + kdvOran);
        var kdvTutar = toplam - araToplam;

        var faturaId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        // ⚠ P-22 / B-3: numara seri kapısından alınır (TEK NOKTA). Bu yol belgeyi
        //   doğrudan KESILDI doğurur, dolayısıyla numarayı HEMEN hak eder.
        await using var tx = await db.Database.BeginTransactionAsync();
        var yeniFatura = new Data.Fatura
        {
            Id = faturaId,
            TenantId = islem.TenantId,
            FaturaNo = null,
            CariId = cariId,
            Tur = dto.Tur ?? "SATIS",
            // ── AÇIKÇA YAZILIR, SESSİZ VARSAYILANA GÜVENİLMEZ (paket 38) ─────
            // Banka dekontu hareketler DEFTERİNDE DEĞİLDİR: has bacağı yoktur,
            // kur kaydı yoktur. Bu yoldan KUYUM (özel matrah) faturası KESİLEMEZ;
            // kesilseydi has bedelinin dayanağı hiçbir yerde durmazdı.
            // Davranış değişmiyor — varsayılan zaten STANDART/0 idi; artık
            // niyet kodda yazılı.
            KdvTuru = "STANDART",
            HasDeger = 0m,
            OzelMatrahKodu = null,
            HasGram = null,
            HasBirimFiyat = null,
            AraToplam = Math.Round(araToplam, 2, MidpointRounding.AwayFromZero),
            KdvOran = Math.Round(kdvOran, 4, MidpointRounding.AwayFromZero),
            KdvTutar = Math.Round(kdvTutar, 2, MidpointRounding.AwayFromZero),
            Toplam = toplam,
            ParaBirimi = hareket.ParaBirimi,
            Durum = "KESILDI",
            BankaHareketId = hareketId,
            Aciklama = dto.Aciklama ?? hareket.Aciklama,
            Tarih = hareket.Tarih,
            Olusturuldu = now,
            Guncellendi = now,
        };
        db.Faturalar.Add(yeniFatura);
        // TEK NOKTA: seri burada tüketilir (FaturaSeriKapisi).
        await Modules.Fatura.FaturaSeriKapisi.Tuket(db, yeniFatura, hareket.Tarih.Year);

        hareket.Durum = "FATURALANDI";
        hareket.FaturaId = faturaId;
        hareket.CariId = cariId;

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        // ADR-0010: global filtre zaten koruyor, ama elle şart da yazılır —
        // tek koruma katmanı bırakılmaz (K4 denetimi, 2026-09-14).
        var fatura = await db.Faturalar.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == faturaId && f.TenantId == islem.TenantId);
        return new { fatura, cariAd = cari.Ad };
    }

    // ─── Yardımcılar ──────────────────────────────────────────────────────────

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET P-12 (B-42) — BANKA DEKONTU DÜZELTME (finansal olmayan alanlar)
    //
    // SÖZLEŞME (PLAN § 2 P-12 DoD):
    //   • FİNANSAL ALAN DÜZELTİLMEZ: tutar · yön · para birimi · hesap. Tek
    //     düzeltme yolu STORNO + yeni dekonttur (StornoServisi.BankaHareketiStorno).
    //   • DÜZELTİLEBİLİR: açıklama · karşı taraf adı · karşı IBAN · tarih.
    //     Tarih dekontun KULLANICI GİRDİSİ olan alanıdır; MASAK kümülatifi
    //     `olusturuldu` eksenini okur (KuyumcuDbContext yorumu, Mehmet K41-1),
    //     bu yüzden tarih düzeltmesi eşik hesabını KAYDIRAMAZ.
    //   • EKSTREDEN GELEN SATIRIN karşı taraf/IBAN/tarihi DEĞİŞMEZ: o alanlar
    //     bankanın kendi kaydıdır; yalnız açıklama (kullanıcı notu) düzeltilir.
    //   • STORNO SATIRI ve STORNOLANMIŞ SATIR düzeltilmez: hükümsüz kılınmış
    //     bir kaydın notu değiştirilirse denetim izi yanıltıcı olur.
    //   • DenetimIzi ESKİ → YENİ değerle yazılır.
    // ═════════════════════════════════════════════════════════════════════════
    public async Task<object> HareketDuzelt(
        string hesapId, string hareketId, HareketDuzeltDto dto, AktifKullanici islem)
    {
        await TenantHesabiBul(hesapId, islem.TenantId);

        var h = await db.BankaHareketleri
            .FirstOrDefaultAsync(x => x.Id == hareketId
                                   && x.TenantId == islem.TenantId
                                   && x.BankaHesapId == hesapId)
            ?? throw new NotFoundException("Banka hareketi bulunamadı");

        if (!dto.AciklamaVerildi && !dto.KarsiTarafAdVerildi
            && !dto.KarsiIbanVerildi && !dto.TarihVerildi)
            throw new BadRequestException(
                "Düzeltilecek alan gönderilmedi. Bu uçta yalnız açıklama, karşı taraf ve tarih "
                + "düzeltilebilir; tutar/yön düzeltmesi yalnız STORNO + yeni dekont yoluyla yapılır.");

        if (h.StornoKaynakId is not null)
            throw new ConflictException(
                "Bu satır bir STORNO kaydıdır; düzeltilemez. Doğru bilgiyi yeni bir dekontla girin.");

        var stornolandi = await db.BankaHareketleri.AsNoTracking()
            .AnyAsync(x => x.TenantId == islem.TenantId && x.StornoKaynakId == h.Id);
        if (stornolandi)
            throw new ConflictException(
                "Bu dekont stornolanmıştır; hükümsüz bir kaydın bilgileri düzeltilmez. "
                + "Doğru bilgiyi yeni bir dekontla girin.");

        var ekstreden = h.EkstreSatirId is not null;
        var degisiklikler = new List<string>();

        if (dto.AciklamaVerildi)
        {
            var yeni = string.IsNullOrWhiteSpace(dto.Aciklama) ? null : dto.Aciklama!.Trim();
            if (yeni != h.Aciklama)
            {
                degisiklikler.Add($"açıklama: '{h.Aciklama ?? "—"}' → '{yeni ?? "—"}'");
                h.Aciklama = yeni;
            }
        }

        if (dto.KarsiTarafAdVerildi)
        {
            EkstreKapisi(ekstreden, "karşı taraf adı");
            var yeni = string.IsNullOrWhiteSpace(dto.KarsiTarafAd) ? null : dto.KarsiTarafAd!.Trim();
            if (yeni != h.KarsiTarafAd)
            {
                degisiklikler.Add($"karşı taraf: '{h.KarsiTarafAd ?? "—"}' → '{yeni ?? "—"}'");
                h.KarsiTarafAd = yeni;
            }
        }

        if (dto.KarsiIbanVerildi)
        {
            EkstreKapisi(ekstreden, "karşı IBAN");
            // GOC-A: Core IBAN kuralı servis yolunda da (kayıt biçimi: boşluksuz, büyük harf).
            var yeni = IbanDenetle(dto.KarsiIban, "karsiIban");
            if (yeni != h.KarsiIban)
            {
                degisiklikler.Add($"karşı IBAN: '{h.KarsiIban ?? "—"}' → '{yeni ?? "—"}'");
                h.KarsiIban = yeni;
            }
        }

        if (dto.TarihVerildi)
        {
            EkstreKapisi(ekstreden, "tarih");
            var yeni = dto.Tarih
                ?? throw new BadRequestException("Tarih boşaltılamaz; yeni bir tarih gönderin.");
            // GOC-A (envanter C · bh-tarih-girdi): POST 5 dk / PATCH 1 gün toleransı TEK kurala indi —
            // K8a işlem tarihi (ileri gün yok, en çok 1 yıl geri), metin sözlükten (ekranla aynı).
            GirdiKapisi.IslemTarihi(Zaman.YerelBugun(yeni.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(yeni, DateTimeKind.Utc) : yeni.ToUniversalTime()), "tarih");
            if (yeni != h.Tarih)
            {
                degisiklikler.Add($"tarih: {h.Tarih:yyyy-MM-dd} → {yeni:yyyy-MM-dd}");
                h.Tarih = yeni;
            }
        }

        if (degisiklikler.Count > 0)
        {
            denetim?.YazmaKaydet(islem, "BankaHareketiDuzeltme", h.Id,
                "banka dekontu düzeltildi · " + string.Join(" · ", degisiklikler));
            await db.SaveChangesAsync();
        }

        return new
        {
            id           = h.Id,
            bankaHesapId = h.BankaHesapId,
            aciklama     = h.Aciklama,
            karsiTarafAd = h.KarsiTarafAd,
            karsiIban    = h.KarsiIban,
            tarih        = h.Tarih,
            tutar        = h.Tutar,
            yon          = h.Yon,
            degisenAlanSayisi = degisiklikler.Count,
        };
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET Q9-1 — DEKONT DURUM MAKİNESİ: BEKLIYOR → ESLESTI (cari eşleştirme)
    //
    // Durumlar (Entities.BankaDurumlari): BEKLIYOR · ESLESTI · FATURALANDI.
    //   BEKLIYOR  → ESLESTI      : BU UÇ (cari seçilir) — ya da POS yatması eşleşmesi.
    //   BEKLIYOR/ESLESTI → FATURALANDI : faturaya dönüştür ucu.
    //   ESLESTI   → BEKLIYOR     : YOK (bilinçli). Cari bağını kaldırmak/değiştirmek
    //     MASAK bağlantılı işlem toplamından tutarı DÜŞÜRÜR — eşik bölme (structuring)
    //     yolu olurdu. Yanlış cari = STORNO + doğru cariyle yeni dekont (ekranda
    //     DegismezIpucu bunu söyler).
    //
    // ⚠ MASAK (fail-closed, YENİ HÜKÜM YAZILMADI): eşleştirme dekontu o carinin
    //   bağlantılı işlem toplamına SONRADAN sokar. Mevcut kapı (MasakKapisi.
    //   DogrulaKumulatif, eşik sayıları MasakKuralMotoru ayarından) dekontun
    //   OLUŞTURULDU gününe göre yeniden koşulur; kimlik tespiti gerekirse
    //   eşleştirme REDDEDİLİR ve doğru yol (storno + kimlik bloğuyla yeni dekont)
    //   söylenir. Sonradan kimlik alıp eşleştirme yolu açılmadı: tespit dekontun
    //   girildiği ana bağlıdır (MASAK FAZ 2 ile aynı commit kuralı).
    // ═════════════════════════════════════════════════════════════════════════
    public async Task<object> DekontEslestir(
        string hesapId, string hareketId, DekontEslestirDto dto, AktifKullanici islem)
    {
        await TenantHesabiBul(hesapId, islem.TenantId);

        // DTO DataAnnotations yalnız controller yolunda koşar → servis içi elle kontrol.
        if (string.IsNullOrWhiteSpace(dto.CariId))
            throw new BadRequestException("Eşleştirilecek müşteri seçilmelidir.").AlanIle("cariId");

        var h = await db.BankaHareketleri
            .FirstOrDefaultAsync(x => x.Id == hareketId
                                   && x.TenantId == islem.TenantId
                                   && x.BankaHesapId == hesapId)
            ?? throw new NotFoundException("Banka hareketi bulunamadı");

        if (h.StornoKaynakId is not null)
            throw new ConflictException(
                "Bu satır bir STORNO kaydıdır; müşteriyle eşleştirilemez.");
        await StornoluysaReddet(h.Id, islem.TenantId);
        if (h.KarsiBacakId is not null)
            throw new ConflictException(
                "Bu dekont bir virmanın bacağıdır (para hesaplar arasında taşındı); müşterisi yoktur.");
        BekliyorDegilseReddet(h.Durum);

        var cariVar = await db.Cariler.AsNoTracking()
            .AnyAsync(c => c.Id == dto.CariId && c.TenantId == islem.TenantId);
        if (!cariVar) throw new NotFoundException("Cari bulunamadı");

        // PAKET MASAK-ESZAMAN: eşleştirme dekontu carinin günlük kümülatifine KATAR → kontrol ile yazma
        // TEK işlemde, cari satırı kilitliyken (eşzamanlı ikinci eşleştirme/tahsilat güncel toplamı görür).
        // PAKET MASAK-ESZAMAN-2: DEKONT SATIRI da kilitlenir (kilit sırası: kaynak satır → cari) ve
        // "Bekliyor mu / stornolandı mı" kilit ALTINDA yeniden okunur (READ COMMITTED → son commit). Aynı
        // dekont iki farklı müşteriye eşzamanlı eşleştirilirse: biri 200, öteki 409 (iki cari kilidi
        // farklı satırlar olduğundan yalnız cari kilidi bunu serileştirmezdi).
        await using var tx = await db.Database.BaslatVeyaDevral();
        await db.Database.Kilitle(KilitTablosu.BankaHareketleri, h.Id, islem.TenantId);
        BekliyorDegilseReddet(await db.BankaHareketleri.AsNoTracking()
            .Where(x => x.Id == h.Id && x.TenantId == islem.TenantId)
            .Select(x => x.Durum).FirstAsync());
        await StornoluysaReddet(h.Id, islem.TenantId);
        await YasalKilit.CariKilitle(db.Database, islem.TenantId, dto.CariId, h.Olusturuldu);
        try
        {
            // PAKET YASAL-PARAM-EKRAN (MASAK-370 hasım DÜŞÜK): eşik İŞLEM GÜNÜNÜN (dekontun
            // oluşturulduğu yerel gün) değeridir — sunucunun bugünü değil; kümülatif pencere de o gündür.
            await masak.DogrulaKumulatif(db, islem.TenantId, dto.CariId, h.Tutar, null, h.Olusturuldu,
                esikGunu: Kuyumcu.Api.Common.YasalParametreServisi.IslemGunu(h.Olusturuldu));
        }
        catch (BadRequestException ex)
        {
            throw new ConflictException(
                $"Bu dekont bu müşteriyle eşleştirilirse yasal kimlik tespiti gerekir; sonradan eşleştirme ile yapılamaz. Dekontu STORNO edin ve müşteriyi seçerek kimlik bilgileriyle yeni dekont girin. ({ex.Message})");
        }

        var eski = h.Durum;
        h.CariId = dto.CariId;
        h.Durum = "ESLESTI";
        denetim?.YazmaKaydet(islem, "BankaHareketiEslestirme", h.Id,
            $"banka dekontu müşteriyle eşleştirildi · durum: {eski} → ESLESTI · cari: {dto.CariId}");
        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        return new
        {
            id           = h.Id,
            bankaHesapId = h.BankaHesapId,
            durum        = h.Durum,
            cariId       = h.CariId,
            oncekiDurum  = eski,
        };
    }

    /// <summary>PAKET MASAK-ESZAMAN-2: eşleştirme yalnız 'Bekliyor' dekontta (kilit öncesi ve kilit altında aynı ileti).</summary>
    private static void BekliyorDegilseReddet(string durum)
    {
        if (durum != "BEKLIYOR")
            throw new ConflictException(
                $"Yalnız 'Bekliyor' durumundaki dekont eşleştirilir (şu an: {durum}). "
                + "Müşteri yanlışsa dekontu STORNO edin ve doğru müşteriyle yeni dekont girin.");
    }

    private async Task StornoluysaReddet(string dekontId, string tenantId)
    {
        var stornolandi = await db.BankaHareketleri.AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.StornoKaynakId == dekontId);
        if (stornolandi)
            throw new ConflictException(
                "Bu dekont stornolanmıştır; hükümsüz bir kayıt müşteriyle eşleştirilmez.");
    }

    private static void EkstreKapisi(bool ekstreden, string alan)
    {
        if (!ekstreden) return;
        throw new ConflictException(
            $"Bu dekont banka ekstresinden geldi; {alan} bankanın kendi kaydıdır ve ekrandan "
            + "değiştirilemez (ekstre yeniden yüklendiğinde mutabakat kırılır). Yalnız açıklama "
            + "notunu düzeltebilirsiniz.");
    }

    internal async Task<BankaHesap> TenantHesabiBul(string id, string tenantId) =>
        await db.BankaHesaplari.FirstOrDefaultAsync(h => h.Id == id && h.TenantId == tenantId)
        ?? throw new NotFoundException("Banka hesabı bulunamadı");

    // ⚠ P-22 / B-3: yerel numara üreticisi KALDIRILDI; seri yalnız
    //   Modules.Fatura.FaturaSeriKapisi.Tuket üzerinden tüketilir.
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/bankalar")]
[Yetki.ModulYetkisi("banka")]
public sealed class BankaController(BankaServisi servis, Satis.StornoServisi storno) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/bankalar — Banka hesabı ekle — MUDUR+</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> HesapOlustur([FromBody] HesapOlusturDto dto) => servis.HesapOlustur(dto, Islem);

    /// <summary>GET /api/v1/bankalar — Hesap listesi</summary>
    [HttpGet]
    public Task<object> HesapListele() => servis.HesapListele(Islem);

    /// <summary>GET /api/v1/bankalar/{id} — Hesap detayı</summary>
    [HttpGet("{id}")]
    public Task<object> HesapGetir(string id) => servis.HesapGetir(id, Islem);

    /// <summary>PATCH /api/v1/bankalar/{id} — Hesap bilgilerini güncelle — MUDUR+</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> HesapGuncelle(string id, [FromBody] HesapGuncelleDto dto)
        => servis.HesapGuncelle(id, dto, Islem);

    /// <summary>DELETE /api/v1/bankalar/{id} — Hesabı PASİFE ALIR (silmez) — SAHIP</summary>
    /// <remarks>Kasa DELETE ile simetrik rol kapısı (Mehmet K5).</remarks>
    [HttpDelete("{id}")]
    [RolGerekli("SAHIP")]
    public Task<object> HesapPasifeAl(string id) => servis.HesapPasifeAl(id, Islem);

    /// <summary>GET /api/v1/bankalar/{id}/hareketler?durum=BEKLIYOR — Hareket listesi</summary>
    [HttpGet("{id}/hareketler")]
    public Task<object> HareketListele(
        string id,
        [FromQuery] string? durum,
        [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery] bool posEslesmemis = false)
        => servis.HareketListele(id, Islem, durum, limit, offset, posEslesmemis);

    /// <summary>POST /api/v1/bankalar/{id}/hareket — Manuel dekont girişi — MUHASEBE+</summary>
    [HttpPost("{id}/hareket")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> HareketEkle(string id, [FromBody] HareketEkleDto dto)
        => servis.HareketEkle(id, dto, Islem);

    /// <summary>
    /// PATCH /api/v1/bankalar/{id}/hareketler/{hareketId} — PAKET P-12 (B-42).
    /// Dekontun açıklama / karşı taraf / tarih alanlarını düzeltir.
    /// Tutar ve yön bu yoldan DEĞİŞMEZ (tek yol: storno + yeni dekont).
    /// </summary>
    [HttpPatch("{id}/hareketler/{hareketId}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> HareketDuzelt(
        string id, string hareketId, [FromBody] HareketDuzeltDto dto)
        => servis.HareketDuzelt(id, hareketId, dto, Islem);

    /// <summary>
    /// POST /api/v1/bankalar/{id}/hareketler/{hareketId}/storno — PAKET P-12 (B-42).
    /// İş mantığı bu dosyaya YAZILMAZ: tek storno motoru (StornoServisi).
    /// </summary>
    [HttpPost("{id}/hareketler/{hareketId}/storno")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> HareketStorno(
        string id, string hareketId, [FromBody] Common.GerekceDto dto)
        => storno.BankaHareketiStorno(id, hareketId, dto.Gerekce, Islem);

    /// <summary>
    /// POST /api/v1/bankalar/{id}/hareketler/{hareketId}/eslestir — PAKET Q9-1.
    /// Bekleyen dekontu müşteriyle eşleştirir (BEKLIYOR → ESLESTI), denetim izi yazar.
    /// </summary>
    [HttpPost("{id}/hareketler/{hareketId}/eslestir")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> DekontEslestir(
        string id, string hareketId, [FromBody] DekontEslestirDto dto)
        => servis.DekontEslestir(id, hareketId, dto, Islem);

    /// <summary>POST /api/v1/bankalar/hareketler/{hareketId}/faturaya-donustur — MUHASEBE+</summary>
    [HttpPost("hareketler/{hareketId}/faturaya-donustur")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> FaturayaDonustur(string hareketId, [FromBody] FaturayaDonusturDto dto)
        => servis.FaturayaDonustur(hareketId, dto, Islem);
}
