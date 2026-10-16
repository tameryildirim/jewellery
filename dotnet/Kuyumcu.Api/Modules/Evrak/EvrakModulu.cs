using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Sozlesmeler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GD = Kuyumcu.Api.Common.Dogrulama;   // PAKET GOC-B
using GS = Kuyumcu.Core.Domain.Girdi.AlanSinirlari;

namespace Kuyumcu.Api.Modules.Evrak;

// ═════════════════════════════════════════════════════════════════════════════
// KIYMETLİ EVRAK MODÜLÜ — çek / senet portföyü (paket 29 Faz A).
// BankaModulu.cs desenini izler: DTO → servis → controller.
//
// DEĞİŞMEZLER (bu dosyada ikinci bir motor AÇILMAZ):
//   • Deftere yazan tek yol HareketFabrikasi.Ekle
//   • Cari bakiyesi CariModulu.BakiyeGuncelle ile AYNI SÖZLEŞMEDE güncellenir
//     (delta + çağıranın transaction'ı; SaveChanges çağırmaz). ⚠ O metot
//     CariServisi'nde PRIVATE'tır ve CariModulu.cs bu pakette DOKUNULMAZ
//     olduğu için, EmanetModulu.cs:281 ve SatisModulu.cs:1763 ile AYNI
//     desende yerel bir kopya kullanılır. Sapma uygulama raporunda yazılıdır.
//   • Durum geçişine karar veren tek yer EvrakDurumKapisi.Dogrula
//   • Vade ve kredi limiti kararı VadeKapisi.Hazirla
//   • kiymetli_evraklar üzerinde ExecuteUpdate/ExecuteDelete YOKTUR
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class EvrakGirisDto
{
    [Required, RegularExpression("CEK|SENET", ErrorMessage = "Evrak türü CEK ya da SENET olmalıdır")]
    public string EvrakTuru { get; set; } = null!;

    /// <summary>PAKET GOC-B tur 2 (doğrulayıcı KRİTİK): eski [Range(0.01, 999_999_999)] tavan aşımında
    /// "sıfırdan büyük olmalı" diyordu ve ekranın tavanıyla (TutarKurali.Tavan = decimal(15,2)) çelişiyordu →
    /// K9 Core kuralı: sıfır/eksi/fazla ondalık/tavan TEK kaynaktan, ekranın alan altı metniyle AYNI.</summary>
    [GD.Tutar]
    public decimal Tutar { get; set; }

    [Required(ErrorMessage = "Vade günü zorunludur")]
    public DateOnly? VadeGunu { get; set; }

    [Required, StringLength(40, MinimumLength = 1, ErrorMessage = "Evrak no 1-40 karakter olmalıdır")]
    public string EvrakNo { get; set; } = null!;

    [StringLength(100)] public string? BankaAdi { get; set; }
    [StringLength(100)] public string? BankaSube { get; set; }
    [StringLength(100)] public string? KesideYeri { get; set; }
    /// <summary>PAKET GOC-B (K8 + K8d): yıl 1900..2100; keşide ≤ vade servis kuralı (ileri tarihli çek TEYİTSİZ → serbest).</summary>
    [GD.TarihPenceresi(Kuyumcu.Core.Domain.Girdi.TarihTuru.Keside)]
    public DateOnly? KesideTarihi { get; set; }

    [StringLength(36)] public string? BorcluCariId { get; set; }
    [StringLength(GS.EvrakBorcluAd), GD.Ad(GS.EvrakBorcluAd)] public string? BorcluAd { get; set; }   // PAKET GOC-B: K19

    /// <summary>Borçlunun TCKN/VKN ya da (yabancı) pasaport no. Diske ŞİFRELİ iner (ADR-0003).
    /// PAKET GOC-B tur 2: rakamsa K4/K5, harf içeriyorsa K7 (KimlikKurali.TcknVknVeyaPasaport); uzunluk kuralla eşit.</summary>
    [StringLength(GS.EvrakBorcluKimlik), GD.TcknVknVeyaPasaport]
    public string? BorcluKimlikNo { get; set; }

    /// <summary>Evrakı BİZE VEREN cari — borcu kapanan cari.</summary>
    [Required(ErrorMessage = "Evrakı veren cari zorunludur"), StringLength(36)]
    public string KaynakCariId { get; set; } = null!;

    [Required(ErrorMessage = "Kur kaydı zorunludur"), StringLength(36)]
    public string KurSnapshotId { get; set; } = null!;

    [StringLength(500)] public string? Notlar { get; set; }

    /// <summary>TRY dışı REDDEDİLİR (K4-G). Gönderilmezse TRY varsayılır.</summary>
    [RegularExpression("TRY", ErrorMessage = "Faz A'da yalnız TRY evrak kabul edilir")]
    public string? ParaBirimi { get; set; }
}

/// <summary>
/// ALAN BAZLI DÜZELTME (kurucunun bağlayıcı UX şartı — UX-S25/UX-1).
///
/// ⚠ SÖZLEŞME: gönderilmeyen alan "değişmedi" demektir; tam gövdeyle üzerine
/// yazıp NULL'lamak YASAKTIR (sipariş kalem imhası deseni). Bu yüzden her
/// alanın yanında bir <c>...Verildi</c> bayrağı vardır ve bayrak JSON'da
/// anahtarın VARLIĞINDAN doğar (CariGuncelleDto deseni).
///
/// ⚠ TUTAR ve KAYNAK CARİ DÜZELTİLEMEZ: ikisi de deftere yazılmış bir satırı
/// doğurdu. Düzeltme yolu karşılıksız işaretlemek ya da storno'dur; ekranda
/// DegismezIpucu ile yazılıdır.
/// </summary>
public sealed class EvrakDuzeltDto
{
    private DateOnly? vadeGunu; public bool VadeGunuVerildi { get; private set; }
    public DateOnly? VadeGunu { get => vadeGunu; set { vadeGunu = value; VadeGunuVerildi = true; } }

    private string? evrakNo; public bool EvrakNoVerildi { get; private set; }
    [StringLength(40, MinimumLength = 1)]
    public string? EvrakNo { get => evrakNo; set { evrakNo = value; EvrakNoVerildi = true; } }

    private string? bankaAdi; public bool BankaAdiVerildi { get; private set; }
    [StringLength(100)]
    public string? BankaAdi { get => bankaAdi; set { bankaAdi = value; BankaAdiVerildi = true; } }

    private string? bankaSube; public bool BankaSubeVerildi { get; private set; }
    [StringLength(100)]
    public string? BankaSube { get => bankaSube; set { bankaSube = value; BankaSubeVerildi = true; } }

    private string? kesideYeri; public bool KesideYeriVerildi { get; private set; }
    [StringLength(100)]
    public string? KesideYeri { get => kesideYeri; set { kesideYeri = value; KesideYeriVerildi = true; } }

    private DateOnly? kesideTarihi; public bool KesideTarihiVerildi { get; private set; }
    [GD.TarihPenceresi(Kuyumcu.Core.Domain.Girdi.TarihTuru.Keside)]   // PAKET GOC-B: K8
    public DateOnly? KesideTarihi { get => kesideTarihi; set { kesideTarihi = value; KesideTarihiVerildi = true; } }

    private string? borcluAd; public bool BorcluAdVerildi { get; private set; }
    [StringLength(GS.EvrakBorcluAd), GD.Ad(GS.EvrakBorcluAd)]   // PAKET GOC-B: K19
    public string? BorcluAd { get => borcluAd; set { borcluAd = value; BorcluAdVerildi = true; } }

    /// <summary>
    /// ⚠ PAKET GOC-B tur 2 (doğrulayıcı KRİTİK — veri kaybı): kimlik no ŞİFRELİ ve hiçbir uçta istemciye
    /// DÖNMEZ; istemci onu geri gönderemez. Bu yüzden bu alan YALNIZ-YAZILIR: dolu değer = değiştir,
    /// null/boş = DEĞİŞMEZ (genel "null ise sil" sözleşmesinin gerekçeli istisnası). Silmek için AÇIK
    /// <see cref="BorcluKimlikSil"/> bayrağı gönderilir (ekranda "Kimlik no'yu kaldır" eylemi).
    /// </summary>
    private string? borcluKimlikNo; public bool BorcluKimlikNoVerildi { get; private set; }
    [StringLength(GS.EvrakBorcluKimlik), GD.TcknVknVeyaPasaport]
    public string? BorcluKimlikNo { get => borcluKimlikNo; set { borcluKimlikNo = value; BorcluKimlikNoVerildi = true; } }

    /// <summary>true → kayıtlı borçlu kimlik no (ve arama hash'i) silinir. Dolu BorcluKimlikNo ile birlikte 400.</summary>
    public bool BorcluKimlikSil { get; set; }

    private string? notlar; public bool NotlarVerildi { get; private set; }
    [StringLength(500)]
    public string? Notlar { get => notlar; set { notlar = value; NotlarVerildi = true; } }
}

public sealed class TahsileVerDto
{
    [Required(ErrorMessage = "Banka hesabı zorunludur"), StringLength(36)]
    public string BankaHesapId { get; set; } = null!;
}

public sealed class TahsilEdildiDto
{
    [Required, RegularExpression("KASA|BANKA", ErrorMessage = "Hedef KASA ya da BANKA olmalıdır")]
    public string Hedef { get; set; } = null!;

    [StringLength(36)] public string? KasaId { get; set; }
    [StringLength(36)] public string? BankaHesapId { get; set; }

    [Required(ErrorMessage = "Kur kaydı zorunludur"), StringLength(36)]
    public string KurSnapshotId { get; set; } = null!;
}

public sealed class KarsiliksizDto
{
    /// <summary>
    /// ⚠ Uzunluk kontrolü SERVİS İÇİNDE DE yapılır: DataAnnotations yalnız
    /// controller yolunda çalışır, servis doğrudan çağrıldığında kapı açık kalırdı.
    /// </summary>
    [Required, StringLength(500, MinimumLength = EvrakServisi.GerekceAsgari,
        ErrorMessage = "Karşılıksız gerekçesi en az 20 karakter olmalıdır")]
    public string Aciklama { get; set; } = null!;

    [Range(0, 3650)] public int? YeniVadeGun { get; set; }

    [Required(ErrorMessage = "Kur kaydı zorunludur"), StringLength(36)]
    public string KurSnapshotId { get; set; } = null!;
}

/// <summary>PAKET P-21 (Y-21) — evrakın üçüncü bir cariye CİRO edilmesi.</summary>
public sealed class CiroDto
{
    /// <summary>Evrakı devrettiğimiz cari — borcumuz bu cariye karşı kapanır.</summary>
    [Required(ErrorMessage = "Ciro edilen cari zorunludur"), StringLength(36)]
    public string CiroCariId { get; set; } = null!;

    [Required(ErrorMessage = "Kur kaydı zorunludur"), StringLength(36)]
    public string KurSnapshotId { get; set; } = null!;

    [StringLength(500)] public string? Aciklama { get; set; }
}

/// <summary>
/// PAKET P-21 (Y-21) — RÜCU: ciro ettiğimiz evrak karşılıksız çıktı, bize döndü.
/// </summary>
public sealed class RucuDto
{
    /// <summary>
    /// ⚠ Uzunluk kontrolü SERVİS İÇİNDE DE yapılır (KarsiliksizDto ile aynı gerekçe).
    /// </summary>
    [Required, StringLength(500, MinimumLength = EvrakServisi.GerekceAsgari,
        ErrorMessage = "Rücu gerekçesi en az 20 karakter olmalıdır")]
    public string Aciklama { get; set; } = null!;

    /// <summary>Keşideciye tanınan yeni vade (gün). Boşsa carinin varsayılanı.</summary>
    [Range(0, 3650)] public int? YeniVadeGun { get; set; }

    [Required(ErrorMessage = "Kur kaydı zorunludur"), StringLength(36)]
    public string KurSnapshotId { get; set; } = null!;
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class EvrakServisi(
    KuyumcuDbContext db,
    ILogger<EvrakServisi> logger,
    DenetimServisi denetim,
    Tevsik.TevsikKapisi tevsik)
{
    public const int GerekceAsgari = 20;
    public const int AzamiLimit = 200;
    public const int AzamiTakvimGun = 366;
    private const string Varlik = nameof(KiymetliEvrak);

    /// <summary>Evrakın "yakında" sayıldığı ufuk (gün) — ekranda amber satır.</summary>
    private const int YakinUfukGun = 7;

    // ─── Giriş ───────────────────────────────────────────────────────────────
    /// <summary>
    /// MÜŞTERİDEN ALINAN evrak portföye girer ve CARİ BORCUNU KAPATIR.
    ///
    /// Defter satırı: Tur=EVRAK_GIRIS, TlTutari = −tutar,
    /// OdemeIzi.VeresiyeKapanisi(tutar), VadeTarihi = null.
    /// <para>
    /// ⚠ NEDEN <c>VeresiyeKapanisi</c> (vadesiz veresiye payı): bu satır borç
    /// DOĞURMAZ, borç KAPATIR — <c>VadeKapisi.AcikBorclar</c> onu
    /// <c>sign(−)·VeresiyeTutar</c> ile açık partileri tüketen satır olarak
    /// görür. Payı 0 bırakılsaydı bakiye doğru çıkar ama VADE dağılımı yanlış
    /// kalırdı: müşteri borcunu çekle kapattığı hâlde vade takibinde
    /// "gecikmiş" görünmeye devam ederdi (M29-03 bunu ölçer).
    /// </para>
    /// <para>
    /// ⚠ TEVSİK ÇALIŞMAZ: nakit payı 0'dır, para el değiştirmez.
    /// ⚠ MASAK KÜMÜLATİFİNE GİRMEZ (K4-D): altta zaten kümülatife sayılmış bir
    /// SATIS/VERESIYE vardır; evrak girişi onu ikinci kez saydırırdı.
    /// </para>
    /// </summary>
    public async Task<EvrakSonucu> Gir(EvrakGirisDto dto, AktifKullanici islem)
    {
        if (islem.SubeId is null)
            throw new BadRequestException("Evrak kaydı için şube seçimi gerekli");

        var paraBirimi = string.IsNullOrWhiteSpace(dto.ParaBirimi) ? "TRY" : dto.ParaBirimi!;
        if (paraBirimi != "TRY")
            throw new BadRequestException("Faz A'da yalnız TRY evrak kabul edilir (döviz evrak kapsam dışı)");

        // PAKET GOC-B (envanter C satır 87): eksi tutarda "zorunlu alan" yanlış metni + fazla ondalık sessizce
        // yuvarlanıyordu → K9 Core kuralı (eksi/sıfır/2 ondalık/tavan) — ekranın alan altı metniyle aynı.
        GD.GirdiKapisi.Tutar(dto.Tutar, "tutar");

        if (!EvrakTuru.Tumu.Contains(dto.EvrakTuru))
            throw new BadRequestException("Evrak türü CEK ya da SENET olmalıdır");

        var vade = dto.VadeGunu ?? throw new BadRequestException("Vade günü zorunludur");
        var bugun = Zaman.YerelBugun();
        if (vade < bugun)
            throw new BadRequestException($"Vade günü geçmiş olamaz (bugün {bugun:yyyy-MM-dd})");
        TarihleriDenetle(vade, dto.KesideTarihi, bugun);   // PAKET GOC-B: vade ≤ 10 yıl · keşide ≤ vade
        GD.GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.AdKurali.Denetle(dto.BorcluAd, GS.EvrakBorcluAd), "borcluAd");
        dto.EvrakNo = dto.EvrakNo?.Trim()!;   // PAKET GOC-B (C satır 122): kırpılmadan saklanıyordu

        if (string.IsNullOrWhiteSpace(dto.EvrakNo))
            throw new BadRequestException("Evrak no zorunludur");
        BorcluKimlikDenetle(dto.BorcluKimlikNo);   // PAKET TCKN-DOGRULAMA

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);
        await IslemDogrulayici.CariDogrula(db, dto.KaynakCariId, islem.TenantId);
        await IslemDogrulayici.CariDogrula(db, dto.BorcluCariId, islem.TenantId);

        var ayirtEdici = AyirtEdici(dto.EvrakTuru, dto.BankaAdi);

        // ⚠ İKİNCİ KATMAN (M29-07): UNIQUE indeks ayirt_edici kolonu üzerinden
        //   kurulu ve o kolon asla NULL değil — ama TOCTOU penceresini indeks
        //   tek başına kapatmaz ve kullanıcıya 500 yerine anlaşılır bir 409
        //   dönmesi gerekir. Elle kontrol bu iki nedenle DURUR.
        var mevcutVar = await db.KiymetliEvraklar.AsNoTracking().AnyAsync(e =>
            e.TenantId == islem.TenantId &&
            e.EvrakTuru == dto.EvrakTuru &&
            e.BankaAyirtEdici == ayirtEdici &&
            e.EvrakNo == dto.EvrakNo);
        if (mevcutVar)
            throw new ConflictException(
                $"Bu evrak zaten kayıtlı: {dto.EvrakTuru} · {ayirtEdici} · {dto.EvrakNo}");

        var now = DateTime.UtcNow;
        var evrakId = Kimlik.Ulid();
        var hareketId = Kimlik.Ulid();

        await using var tx = await db.Database.BeginTransactionAsync();

        HareketFabrikasi.Ekle(db,
            OdemeIzi.VeresiyeKapanisi(dto.Tutar),
            new Hareket
            {
                Id = hareketId,
                TenantId = islem.TenantId,
                SubeId = islem.SubeId,
                Tur = "EVRAK_GIRIS",
                Tarih = now,
                Aciklama = $"{dto.EvrakTuru} girişi · {dto.EvrakNo}",
                TlTutari = -dto.Tutar,        // −negatif: borç kapandı
                HasGrami = 0m,
                ParaBirimi = paraBirimi,
                KurSnapshotId = dto.KurSnapshotId,
                CariId = dto.KaynakCariId,
                KullaniciId = islem.Id,
                Olusturuldu = now,
            });

        db.KiymetliEvraklar.Add(new KiymetliEvrak
        {
            Id = evrakId,
            TenantId = islem.TenantId,
            SubeId = islem.SubeId,
            EvrakTuru = dto.EvrakTuru,
            Yon = EvrakYonu.Alinan,
            Durum = EvrakDurumu.Portfoyde,
            Tutar = Math.Round(dto.Tutar, 2, MidpointRounding.AwayFromZero),
            ParaBirimi = paraBirimi,
            VadeGunu = vade,
            EvrakNo = dto.EvrakNo,
            BankaAdi = Bosaltilmis(dto.BankaAdi),
            BankaAyirtEdici = ayirtEdici,
            BankaSube = Bosaltilmis(dto.BankaSube),
            KesideYeri = Bosaltilmis(dto.KesideYeri),
            KesideTarihi = dto.KesideTarihi,
            BorcluCariId = Bosaltilmis(dto.BorcluCariId),
            BorcluAd = Bosaltilmis(dto.BorcluAd),
            BorcluKimlikNo = Bosaltilmis(dto.BorcluKimlikNo) is { } bk ? KimlikNormal(bk) : null,
            KaynakCariId = dto.KaynakCariId,
            GirisHareketId = hareketId,
            Notlar = Bosaltilmis(dto.Notlar),
            OlusturanKullaniciId = islem.Id,
            Olusturuldu = now,
            Guncellendi = now,
        });

        // Bakiye: borç kapandı → tlBakiye −= tutar. TEK YOL CariServisi'dir.
        await BakiyeGuncelle(dto.KaynakCariId, islem.TenantId, -dto.Tutar, 0m, now);

        denetim.YazmaKaydet(islem, Varlik, evrakId,
            $"evrak girişi · {dto.EvrakTuru} {dto.EvrakNo} · {dto.Tutar:0.00} TL · vade {vade:yyyy-MM-dd}");

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] Evrak girişi: {Id} tur={Tur} tutar={Tutar}",
            islem.TenantId, evrakId, dto.EvrakTuru, dto.Tutar);

        return await SonucGetir(evrakId, islem, dto.KaynakCariId);
    }

    // ─── Düzeltme (UX-S25 / kurul engel 1 ve 4) ──────────────────────────────
    /// <summary>
    /// PORTFÖYDEKİ evrakın tanımlayıcı alanlarını düzeltir.
    /// <para>
    /// ⚠ Yalnız <c>PORTFOYDE</c> durumunda: tahsile verilmiş ya da tahsil
    /// edilmiş bir evrakın çek numarası değiştirilemez — o numara artık
    /// bankaya bildirilmiş bir gerçektir.
    /// </para>
    /// <para>⚠ Tutar ve kaynak cari BURADA DA değişmez (defter satırı doğurdular).</para>
    /// </summary>
    public async Task<EvrakSonucu> Duzelt(string id, EvrakDuzeltDto dto, AktifKullanici islem)
    {
        var e = await EvrakBul(id, islem.TenantId);

        if (e.Durum != EvrakDurumu.Portfoyde)
            throw new BadRequestException(
                $"Yalnız portföydeki evrak düzeltilebilir; bu evrak '{e.Durum}' durumunda. " +
                "Yanlış girilmiş bir evrak karşılıksız işaretlenerek ya da storno ile geri alınır.");

        var eskiOzet = $"{e.EvrakNo}/{e.VadeGunu:yyyy-MM-dd}";

        if (dto.VadeGunuVerildi)
        {
            var yeni = dto.VadeGunu ?? throw new BadRequestException("Vade günü boşaltılamaz");
            if (yeni < Zaman.YerelBugun())
                throw new BadRequestException("Vade günü geçmiş olamaz");
            e.VadeGunu = yeni;
        }

        if (dto.EvrakNoVerildi)
        {
            if (string.IsNullOrWhiteSpace(dto.EvrakNo))
                throw new BadRequestException("Evrak no boşaltılamaz");
            e.EvrakNo = dto.EvrakNo!.Trim();   // PAKET GOC-B: kırpılır
        }

        if (dto.BankaAdiVerildi) e.BankaAdi = Bosaltilmis(dto.BankaAdi);
        if (dto.BankaSubeVerildi) e.BankaSube = Bosaltilmis(dto.BankaSube);
        if (dto.KesideYeriVerildi) e.KesideYeri = Bosaltilmis(dto.KesideYeri);
        if (dto.KesideTarihiVerildi) e.KesideTarihi = dto.KesideTarihi;
        // PAKET GOC-B: vade/keşide birlikte — yalnız biri değişse de SONUÇ (kayıtlı + yeni) sınanır.
        if (dto.VadeGunuVerildi || dto.KesideTarihiVerildi) TarihleriDenetle(e.VadeGunu, e.KesideTarihi, Zaman.YerelBugun());
        if (dto.BorcluAdVerildi)
        {
            GD.GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.AdKurali.Denetle(dto.BorcluAd, GS.EvrakBorcluAd), "borcluAd");   // PAKET GOC-B
            e.BorcluAd = Bosaltilmis(dto.BorcluAd);
        }
        // PAKET GOC-B tur 2: kimlik YALNIZ-YAZILIR — null/boş = değişmez; silme yalnız AÇIK bayrakla.
        var yeniKimlik = dto.BorcluKimlikNoVerildi ? Bosaltilmis(dto.BorcluKimlikNo) : null;
        if (dto.BorcluKimlikSil && yeniKimlik is not null)
            throw new BadRequestException(KimlikSilCelisikMesaj).AlanIle("borcluKimlikNo");
        if (yeniKimlik is not null)
        {
            BorcluKimlikDenetle(yeniKimlik);   // PAKET TCKN-DOGRULAMA
            e.BorcluKimlikNo = KimlikNormal(yeniKimlik);
        }
        else if (dto.BorcluKimlikSil)
            e.BorcluKimlikNo = null;
        if (dto.NotlarVerildi) e.Notlar = Bosaltilmis(dto.Notlar);

        // Ayırt edici banka adından TÜRER; elle yazılmaz (tek kaynak).
        e.BankaAyirtEdici = AyirtEdici(e.EvrakTuru, e.BankaAdi);

        var cakisma = await db.KiymetliEvraklar.AsNoTracking().AnyAsync(x =>
            x.TenantId == islem.TenantId && x.Id != e.Id &&
            x.EvrakTuru == e.EvrakTuru && x.BankaAyirtEdici == e.BankaAyirtEdici &&
            x.EvrakNo == e.EvrakNo);
        if (cakisma)
            throw new ConflictException(
                $"Bu evrak zaten kayıtlı: {e.EvrakTuru} · {e.BankaAyirtEdici} · {e.EvrakNo}");

        e.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, Varlik, e.Id,
            $"evrak düzeltildi · {eskiOzet} → {e.EvrakNo}/{e.VadeGunu:yyyy-MM-dd}");
        await db.SaveChangesAsync();

        return await SonucGetir(e.Id, islem, e.KaynakCariId);
    }

    // ─── Tahsile ver / portföye al (para EL DEĞİŞTİRMEZ) ─────────────────────

    public async Task<EvrakSonucu> TahsileVer(string id, TahsileVerDto dto, AktifKullanici islem)
    {
        var e = await EvrakBul(id, islem.TenantId);
        EvrakDurumKapisi.Dogrula(e.Durum, EvrakDurumu.TahsileVerildi);

        var hesap = await db.BankaHesaplari.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == dto.BankaHesapId && h.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Banka hesabı bulunamadı");

        var now = DateTime.UtcNow;
        e.Durum = EvrakDurumu.TahsileVerildi;
        e.TahsileVerenBankaHesapId = hesap.Id;
        e.TahsileVermeTarihi = now;
        e.Guncellendi = now;

        // ⚠ DEFTER SATIRI YAZILMAZ: para el değiştirmedi, yalnız evrak bankaya
        //   teslim edildi. Hareket yazılsaydı tahsilat iki kez sayılırdı.
        denetim.YazmaKaydet(islem, Varlik, e.Id, $"tahsile verildi · hesap {hesap.HesapAdi}");
        await db.SaveChangesAsync();

        return await SonucGetir(e.Id, islem, e.KaynakCariId);
    }

    public async Task<EvrakSonucu> PortfoyeAl(string id, AktifKullanici islem)
    {
        var e = await EvrakBul(id, islem.TenantId);
        EvrakDurumKapisi.Dogrula(e.Durum, EvrakDurumu.Portfoyde);

        var now = DateTime.UtcNow;
        e.Durum = EvrakDurumu.Portfoyde;
        e.TahsileVerenBankaHesapId = null;
        e.TahsileVermeTarihi = null;
        e.Guncellendi = now;

        denetim.YazmaKaydet(islem, Varlik, e.Id, "portföye geri alındı (banka iade etti)");
        await db.SaveChangesAsync();

        return await SonucGetir(e.Id, islem, e.KaynakCariId);
    }

    // ─── Tahsil edildi (para GİRER) ──────────────────────────────────────────
    /// <summary>
    /// Evrak tahsil edildi: para kasaya (nakit) ya da banka hesabına girer.
    /// <para>
    /// ⚠ ÇİFT SAYMA YASAĞI: cari borcu evrak GİRİŞİNDE zaten kapandı. Tahsil
    /// hareketinin veresiye payı 0'dır ve <c>CariBakiye</c> BURADA
    /// DEĞİŞTİRİLMEZ; aksi hâlde aynı borç iki kez kapanırdı.
    /// </para>
    /// <para>
    /// ⚠ TÜR "TAHSILAT"tır ve bu bilinçlidir (MASAK PARİTE ŞARTI — Mehmet):
    /// paraya dönen bacak, mevcut nakit/banka tahsilatıyla AYNI türdür,
    /// dolayısıyla <c>MasakKapisi.KapiyaTabiTurler</c> davranışı da aynıdır.
    /// K4-D gevşemesi yalnız EVRAK_GIRIS içindir.
    /// </para>
    /// </summary>
    public async Task<EvrakSonucu> TahsilEdildi(string id, TahsilEdildiDto dto, AktifKullanici islem)
    {
        if (islem.SubeId is null)
            throw new BadRequestException("Tahsil kaydı için şube seçimi gerekli");

        var e = await EvrakBul(id, islem.TenantId);
        EvrakDurumKapisi.Dogrula(e.Durum, EvrakDurumu.TahsilEdildi);

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);

        var nakit = dto.Hedef == "KASA";
        var now = DateTime.UtcNow;

        Data.Kasa? kasa = null;
        BankaHesap? hesap = null;

        if (nakit)
        {
            if (string.IsNullOrWhiteSpace(dto.KasaId))
                throw new BadRequestException("Nakit tahsil için kasa seçimi gerekli");

            // K6 (TL yalnız TL kasasına) + GÜN KİLİDİ — İKİSİ DE bu tek çağrıda.
            // ⚠ `now` parametresi ZORUNLUDUR: verilmezse KasaDogrula yine DERLENİR
            //    ama gün sonu kapanışı kilidi HİÇ çalışmaz ve kapanmış bir günün
            //    kasasına yeni para yazılabilir (M29-05 bunu ölçer).
            kasa = await IslemDogrulayici.KasaDogrula(db, dto.KasaId!, islem.TenantId, "TRY", now);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.BankaHesapId))
                throw new BadRequestException("Banka tahsili için hesap seçimi gerekli");
            hesap = await db.BankaHesaplari
                .FirstOrDefaultAsync(h => h.Id == dto.BankaHesapId && h.TenantId == islem.TenantId)
                ?? throw new NotFoundException("Banka hesabı bulunamadı");
            if (hesap.ParaBirimi != "TRY")
                throw new BadRequestException(
                    $"Bu işlem TRY hesabı gerektirir; '{hesap.HesapAdi}' hesabının para birimi {hesap.ParaBirimi}");
        }

        // 459 s. VUK — nakit tahsil haddi. TRANSACTION'DAN ÖNCE (M29-06).
        if (nakit)
            await tevsik.Dogrula(db, islem.TenantId, e.KaynakCariId, e.Tutar, now);
        // PAKET YASAL-2 (TB-15): senedi nakde çevirmek, büyük işlemden kalan borcun
        // taksitini nakit almaktır — aynı kapı (FIFO, kaynak günün haddi).
        var taksit = nakit
            ? await tevsik.TaksitDogrula(db, islem.TenantId, e.KaynakCariId, e.Tutar, e.Tutar, now)
            : null;
        if (taksit is { Dogrulanamayan: > 0m } tk)
            denetim.YazmaKaydet(islem, "TEVSIK_TAKSIT_DOGRULANAMADI", e.Id,
                FormattableString.Invariant(
                    $"evrak nakit tahsili {e.Tutar:0.00} TL; kaynağı bilinmeyen bakiyeden {tk.Dogrulanamayan:0.00} TL"),
                hassas: false);

        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, e.KaynakCariId, now);   // PAKET MASAK-ESZAMAN: işlem + CARİ SATIRI KİLİDİ
        if (nakit)
        {   // PAKET MASAK-ESZAMAN: kümülatif nakit (tevsik) + taksit kapısı kilit altında yinelenir
            await tevsik.Dogrula(db, islem.TenantId, e.KaynakCariId, e.Tutar, now);
            await tevsik.TaksitDogrula(db, islem.TenantId, e.KaynakCariId, e.Tutar, e.Tutar, now);
        }

        var hareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            nakit ? OdemeIzi.Nakitten(e.Tutar) : OdemeIzi.Yok,
            new Hareket
            {
                Id = hareketId,
                TenantId = islem.TenantId,
                SubeId = islem.SubeId,
                Tur = "TAHSILAT",
                Tarih = now,
                Aciklama = $"{e.EvrakTuru} tahsili · {e.EvrakNo}",
                TlTutari = e.Tutar,           // +pozitif: para girdi
                HasGrami = 0m,
                OdemeTuru = nakit ? "NAKIT" : "HAVALE",
                ParaBirimi = "TRY",
                KurSnapshotId = dto.KurSnapshotId,
                CariId = e.KaynakCariId,
                KullaniciId = islem.Id,
                Olusturuldu = now,
            });

        if (kasa is not null)
        {
            await BakiyeKilidi.Kilitle(db, kasa);   // PAKET KAPORA-ESZAMAN tur 2: kilitli taze okuma (kayıp güncelleme)
            kasa.Bakiye = Math.Round(kasa.Bakiye + e.Tutar, 2, MidpointRounding.AwayFromZero);
            kasa.Guncellendi = now;
            var kasaHareketId = Kimlik.Ulid();
            db.KasaHareketleri.Add(new KasaHareket
            {
                Id = kasaHareketId,
                TenantId = islem.TenantId,
                KasaId = kasa.Id,
                HareketId = hareketId,
                Tutar = Math.Round(e.Tutar, 2, MidpointRounding.AwayFromZero),
                ParaBirimi = kasa.ParaBirimi,
                OdemeTuru = "NAKIT",
                Tarih = now,
                Olusturuldu = now,
            });
            e.TahsilKasaId = kasa.Id;
            e.TahsilKasaHareketId = kasaHareketId;
        }

        if (hesap is not null)
        {
            await BakiyeKilidi.Kilitle(db, hesap);   // PAKET KAPORA-ESZAMAN tur 2: kilitli taze okuma (kayıp güncelleme)
            hesap.Bakiye = Math.Round(hesap.Bakiye + e.Tutar, 2, MidpointRounding.AwayFromZero);
            hesap.Guncellendi = now;
            var bankaHareketId = Kimlik.Ulid();
            db.BankaHareketleri.Add(new BankaHareket
            {
                Id = bankaHareketId,
                TenantId = islem.TenantId,
                BankaHesapId = hesap.Id,
                Yon = "GELEN",
                Tutar = Math.Round(e.Tutar, 2, MidpointRounding.AwayFromZero),
                ParaBirimi = hesap.ParaBirimi,
                Aciklama = $"{e.EvrakTuru} tahsili · {e.EvrakNo}",
                CariId = e.KaynakCariId,
                Durum = "ESLESTI",
                Tarih = now,
                Olusturuldu = now,
            });
            e.TahsilBankaHesapId = hesap.Id;
            e.TahsilBankaHareketId = bankaHareketId;
        }

        e.Durum = EvrakDurumu.TahsilEdildi;
        e.TahsilTarihi = now;
        e.Guncellendi = now;

        denetim.YazmaKaydet(islem, Varlik, e.Id,
            $"tahsil edildi · {(nakit ? "KASA" : "BANKA")} · {e.Tutar:0.00} TL",
            kasaId: kasa?.Id);

        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] Evrak tahsil: {Id} hedef={Hedef}",
            islem.TenantId, e.Id, dto.Hedef);

        return await SonucGetir(e.Id, islem, e.KaynakCariId);
    }

    // ─── Karşılıksız (BORÇ GERİ DOĞAR) ───────────────────────────────────────
    /// <summary>
    /// Evrak karşılıksız çıktı: kapanan borç TERS KAYITLA geri doğar.
    /// Hareket: Tur=EVRAK_KARSILIKSIZ, TlTutari = +tutar,
    /// OdemeIzi.Veresiyeden(tutar, vade) — vade <c>VadeKapisi.Hazirla</c>'dan gelir.
    /// <para>
    /// ⚠ Vade sabit yazılamaz: <c>Hazirla</c> aynı zamanda KREDİ LİMİTİ kapısıdır
    /// (alacak yönü). Geri doğan borç limiti aşıyorsa işlem 400'dür (M29-04).
    /// </para>
    /// </summary>
    public async Task<EvrakSonucu> Karsiliksiz(string id, KarsiliksizDto dto, AktifKullanici islem)
    {
        if (islem.SubeId is null)
            throw new BadRequestException("Karşılıksız kaydı için şube seçimi gerekli");

        // ⚠ DataAnnotations YALNIZ controller yolunda çalışır; servis doğrudan
        //   çağrıldığında kapıyı bu satır kapatır.
        var aciklama = (dto.Aciklama ?? "").Trim();
        if (aciklama.Length < GerekceAsgari)
            throw new BadRequestException(
                $"Karşılıksız gerekçesi en az {GerekceAsgari} karakter olmalıdır (şu an {aciklama.Length}).");

        var e = await EvrakBul(id, islem.TenantId);
        EvrakDurumKapisi.Dogrula(e.Durum, EvrakDurumu.Karsiliksiz);

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);

        var now = DateTime.UtcNow;

        // Kredi limiti + vade — TEK KAPI. alacakYonu: true (müşteri bize borçlanıyor).
        var vade = await Vade.VadeKapisi.Hazirla(
            db, islem.TenantId, e.KaynakCariId, dto.YeniVadeGun, e.Tutar,
            alacakYonu: true, now, dto.KurSnapshotId);

        await using var tx = await db.Database.BeginTransactionAsync();

        var hareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            OdemeIzi.Veresiyeden(e.Tutar, vade),
            new Hareket
            {
                Id = hareketId,
                TenantId = islem.TenantId,
                SubeId = islem.SubeId,
                Tur = "EVRAK_KARSILIKSIZ",
                Tarih = now,
                Aciklama = $"{e.EvrakTuru} karşılıksız · {e.EvrakNo}",
                TlTutari = e.Tutar,           // +pozitif: borç yeniden doğdu
                HasGrami = 0m,
                ParaBirimi = "TRY",
                KurSnapshotId = dto.KurSnapshotId,
                CariId = e.KaynakCariId,
                KullaniciId = islem.Id,
                Olusturuldu = now,
            });

        e.Durum = EvrakDurumu.Karsiliksiz;
        e.KarsiliksizTarihi = now;
        e.KarsiliksizHareketId = hareketId;
        e.KarsiliksizAciklama = aciklama;
        e.Guncellendi = now;

        await BakiyeGuncelle(e.KaynakCariId, islem.TenantId, e.Tutar, 0m, now);

        denetim.YazmaKaydet(islem, Varlik, e.Id,
            $"karşılıksız · {e.Tutar:0.00} TL · yeni vade {vade:yyyy-MM-dd} · gerekçe: {aciklama}");

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] Evrak karşılıksız: {Id}", islem.TenantId, e.Id);

        return await SonucGetir(e.Id, islem, e.KaynakCariId);
    }

    // ─── PAKET P-21 (Y-21) · CİRO ve RÜCU ────────────────────────────────────

    /// <summary>
    /// CİRO — elimizdeki evrakı üçüncü bir cariye devrederiz; ONA olan BORCUMUZ kapanır.
    ///
    /// <para>Defter satırı: Tur=EVRAK_CIRO, TlTutari = +tutar (borcumuz azalır —
    /// EVRAK_GIRIS'in aynası), OdemeIzi.VeresiyeKapanisi (borç KAPATIR, vade taşımaz).</para>
    ///
    /// <para>⚠ TAHSİLAT DEĞİLDİR: para kasaya girmez, kasa hiç açılmaz. Nakit
    /// payı 0 olduğu için 459 s. VUK tevsik kümülatifi ÇALIŞMAZ ve MASAK
    /// kümülatifine GİRMEZ (EVRAK_GIRIS ile aynı K4-D gerekçesi: altta zaten
    /// sayılmış bir ALIS/borç vardır).</para>
    ///
    /// <para>⚠ TERMİNAL DEĞİLDİR (plan DoD): CIRO_EDILDI durumundan
    /// KARSILIKSIZ'a (rücu) çıkış vardır, oradan da TAHSIL_EDILDI'ye. "Bizden
    /// çıktı, bitti" diye kapatılsaydı geri dönen evrak sisteme giremezdi.</para>
    /// </summary>
    public async Task<EvrakSonucu> Ciro(string id, CiroDto dto, AktifKullanici islem)
    {
        if (islem.SubeId is null)
            throw new BadRequestException("Ciro kaydı için şube seçimi gerekli");

        var e = await EvrakBul(id, islem.TenantId);
        EvrakDurumKapisi.Dogrula(e.Durum, EvrakDurumu.CiroEdildi);

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);
        await IslemDogrulayici.CariDogrula(db, dto.CiroCariId, islem.TenantId);

        // Evrakı bize veren cariye geri ciro etmek bir işlem değildir: borç
        // kapanmaz, sadece iki kayıt birbirini götürür ve portföy yalan söyler.
        if (string.Equals(dto.CiroCariId, e.KaynakCariId, StringComparison.Ordinal))
            throw new BadRequestException(
                "Evrak, onu size veren cariye ciro edilemez. Geri vermek istiyorsanız storno yolunu kullanın.");

        var now = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();

        var hareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            OdemeIzi.VeresiyeKapanisi(e.Tutar),
            new Hareket
            {
                Id = hareketId,
                TenantId = islem.TenantId,
                SubeId = islem.SubeId,
                Tur = "EVRAK_CIRO",
                Tarih = now,
                Aciklama = dto.Aciklama ?? $"{e.EvrakTuru} ciro · {e.EvrakNo}",
                TlTutari = e.Tutar,          // +pozitif: BİZİM borcumuz kapandı
                HasGrami = 0m,
                ParaBirimi = "TRY",
                KurSnapshotId = dto.KurSnapshotId,
                CariId = dto.CiroCariId,
                KullaniciId = islem.Id,
                Olusturuldu = now,
            });

        e.Durum = EvrakDurumu.CiroEdildi;
        e.CiroCariId = dto.CiroCariId;
        e.CiroTarihi = now;
        e.CiroHareketId = hareketId;
        e.Guncellendi = now;

        await BakiyeGuncelle(dto.CiroCariId, islem.TenantId, e.Tutar, 0m, now);

        denetim.YazmaKaydet(islem, Varlik, e.Id, $"ciro · {e.Tutar:0.00} TL · cari {dto.CiroCariId}");

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] Evrak ciro edildi: {Id}", islem.TenantId, e.Id);

        return await SonucGetir(e.Id, islem, dto.CiroCariId);
    }

    /// <summary>
    /// RÜCU — ciro ettiğimiz evrak karşılıksız çıktı; İKİ borç birden geri doğar.
    ///
    /// <para>(1) <c>EVRAK_RUCU</c>: ciro ettiğimiz cariye olan BORCUMUZ geri
    /// doğar (TlTutari −, bakiye −). TTK md.724 anlamında bize rücu edilmiştir.</para>
    /// <para>(2) <c>EVRAK_KARSILIKSIZ</c>: keşidecinin (kaynak cari) bize olan
    /// borcu geri doğar (TlTutari +, bakiye +, yeni vade).</para>
    ///
    /// <para>⚠ NEDEN İKİ SATIR, NEDEN TEK SATIR YETMEZ: iki borç farklı
    /// taraflaradır. Tek satır yazılsaydı ciro ettiğimiz kuyumcuya olan
    /// borcumuz defterde HİÇ görünmezdi — ödemeyi unuturduk ve bunu ancak o
    /// kişi kapıya geldiğinde öğrenirdik.</para>
    ///
    /// <para>⚠ SONUÇ DURUMU KARSILIKSIZ'dır, TERMİNAL DEĞİLDİR: keşideci
    /// sonradan öderse TAHSIL_EDILDI'ye geçilir (EvrakGecisleri).</para>
    /// <para>⚠ OdemeIzi.Yok (1. satırda): veresiye payı yazılsaydı
    /// <c>VadeKapisi.AcikBorclar</c> BİZİM borcumuzu müşteri alacağı sanardı —
    /// o defter alacak yönlüdür. Bizim borcumuzun tek kaydı cari bakiyesidir.</para>
    /// </summary>
    public async Task<EvrakSonucu> Rucu(string id, RucuDto dto, AktifKullanici islem)
    {
        if (islem.SubeId is null)
            throw new BadRequestException("Rücu kaydı için şube seçimi gerekli");

        // ⚠ DataAnnotations yalnız controller yolunda çalışır.
        var aciklama = (dto.Aciklama ?? "").Trim();
        if (aciklama.Length < GerekceAsgari)
            throw new BadRequestException(
                $"Rücu gerekçesi en az {GerekceAsgari} karakter olmalıdır (şu an {aciklama.Length}).");

        var e = await EvrakBul(id, islem.TenantId);

        if (e.Durum != EvrakDurumu.CiroEdildi)
            throw new BadRequestException(
                $"Rücu yalnız CİRO EDİLMİŞ evrakta olur; bu evrak '{e.Durum}' durumunda. " +
                "Elimizdeki evrak karşılıksız çıktıysa 'karşılıksız' yolunu kullanın.");
        if (string.IsNullOrEmpty(e.CiroCariId))
            throw new BadRequestException("Ciro carisi kayıtlı değil; rücu yazılamaz.");

        // Kapı yine TEK yerdedir (EvrakDurumKapisi) — burada da çağrılır.
        EvrakDurumKapisi.Dogrula(e.Durum, EvrakDurumu.Karsiliksiz);

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);

        var now = DateTime.UtcNow;

        var vade = await Vade.VadeKapisi.Hazirla(
            db, islem.TenantId, e.KaynakCariId, dto.YeniVadeGun, e.Tutar,
            alacakYonu: true, now, dto.KurSnapshotId);

        await using var tx = await db.Database.BeginTransactionAsync();

        // (1) BİZİM borcumuz geri doğar.
        var rucuHareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            OdemeIzi.Yok,
            new Hareket
            {
                Id = rucuHareketId,
                TenantId = islem.TenantId,
                SubeId = islem.SubeId,
                Tur = "EVRAK_RUCU",
                Tarih = now,
                Aciklama = $"{e.EvrakTuru} rücu · {e.EvrakNo} · {aciklama}",
                TlTutari = -e.Tutar,         // −negatif: borcumuz yeniden doğdu
                HasGrami = 0m,
                ParaBirimi = "TRY",
                KurSnapshotId = dto.KurSnapshotId,
                CariId = e.CiroCariId,
                KullaniciId = islem.Id,
                Olusturuldu = now,
            });

        // (2) Keşidecinin bize olan borcu geri doğar (Karsiliksiz ile aynı satır).
        var karsiliksizHareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            OdemeIzi.Veresiyeden(e.Tutar, vade),
            new Hareket
            {
                Id = karsiliksizHareketId,
                TenantId = islem.TenantId,
                SubeId = islem.SubeId,
                Tur = "EVRAK_KARSILIKSIZ",
                Tarih = now,
                Aciklama = $"{e.EvrakTuru} karşılıksız (rücu sonrası) · {e.EvrakNo}",
                TlTutari = e.Tutar,
                HasGrami = 0m,
                ParaBirimi = "TRY",
                KurSnapshotId = dto.KurSnapshotId,
                CariId = e.KaynakCariId,
                KullaniciId = islem.Id,
                Olusturuldu = now,
            });

        e.Durum = EvrakDurumu.Karsiliksiz;
        e.RucuTarihi = now;
        e.RucuHareketId = rucuHareketId;
        e.RucuTutar = e.Tutar;
        e.KarsiliksizTarihi = now;
        e.KarsiliksizHareketId = karsiliksizHareketId;
        e.KarsiliksizAciklama = aciklama;
        e.Guncellendi = now;

        await BakiyeGuncelle(e.CiroCariId, islem.TenantId, -e.Tutar, 0m, now);
        await BakiyeGuncelle(e.KaynakCariId, islem.TenantId, e.Tutar, 0m, now);

        denetim.YazmaKaydet(islem, Varlik, e.Id,
            $"rücu · {e.Tutar:0.00} TL · ciro cari {e.CiroCariId} · gerekçe: {aciklama}");

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] Evrak rücu: {Id}", islem.TenantId, e.Id);

        return await SonucGetir(e.Id, islem, e.KaynakCariId);
    }

    // ─── Okuma ───────────────────────────────────────────────────────────────

    public async Task<EvrakListesi> Listele(
        AktifKullanici islem, string? durum, string? evrakTuru, string? yon, string? cariId,
        DateOnly? vadeBas, DateOnly? vadeBit, int limit, int offset)
    {
        if (limit is < 1 or > AzamiLimit)
            throw new BadRequestException($"limit 1-{AzamiLimit} aralığında olmalıdır (istenen: {limit})");
        if (offset < 0) throw new BadRequestException("offset negatif olamaz");

        var sorgu = db.KiymetliEvraklar.AsNoTracking().Where(e => e.TenantId == islem.TenantId);

        if (!string.IsNullOrWhiteSpace(durum))
        {
            var durumlar = durum.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var d in durumlar)
                if (!EvrakDurumu.Tumu.Contains(d))
                    throw new BadRequestException($"Bilinmeyen durum: {d}");
            sorgu = sorgu.Where(e => durumlar.Contains(e.Durum));
        }
        // PAKET GOC-B (envanter C satır 121/33): bilinmeyen tür ve ters vade aralığı SESSİZCE boş liste
        // dönüyordu (UX-S28 — "kayıt yok" sanılıyordu) → 400 + alan.
        if (!string.IsNullOrWhiteSpace(evrakTuru) && !EvrakTuru.Tumu.Contains(evrakTuru))
            throw new BadRequestException("Evrak türü CEK ya da SENET olmalıdır").AlanIle("evrakTuru");
        GD.GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.TarihPenceresi.AralikDenetle(
            vadeBas, vadeBit, Kuyumcu.Core.Domain.Girdi.TarihPenceresi.VadeEnCokYil + 1), "vadeBit");
        if (!string.IsNullOrWhiteSpace(evrakTuru)) sorgu = sorgu.Where(e => e.EvrakTuru == evrakTuru);
        if (!string.IsNullOrWhiteSpace(yon)) sorgu = sorgu.Where(e => e.Yon == yon);
        if (!string.IsNullOrWhiteSpace(cariId))
            sorgu = sorgu.Where(e => e.KaynakCariId == cariId || e.BorcluCariId == cariId);
        if (vadeBas is { } vb) sorgu = sorgu.Where(e => e.VadeGunu >= vb);
        if (vadeBit is { } vs) sorgu = sorgu.Where(e => e.VadeGunu <= vs);

        // ⚠ TOPLAMLAR SUNUCUDA (TB-19): sayfalanan satırlardan toplanırsa
        //   ikinci sayfaya geçen kullanıcı farklı bir toplam görür.
        var toplamAdet = await sorgu.CountAsync();
        var toplamTutar = await sorgu.SumAsync(e => (decimal?)e.Tutar) ?? 0m;

        var satirlar = await sorgu
            .OrderBy(e => e.VadeGunu).ThenBy(e => e.Id)
            .Skip(offset).Take(limit)
            .Select(e => new { E = e, KaynakAd = db.Cariler.Where(c => c.Id == e.KaynakCariId).Select(c => c.Ad).FirstOrDefault() })
            .ToListAsync();

        // Kart toplamları SÜZGEÇTEN BAĞIMSIZDIR (UX-S33: dönemden bağımsız kart).
        var kiraciHepsi = db.KiymetliEvraklar.AsNoTracking().Where(e => e.TenantId == islem.TenantId);
        var bugun = Zaman.YerelBugun();
        var haftaSonu = bugun.AddDays(YakinUfukGun);

        var portfoyde = await kiraciHepsi.Where(e => e.Durum == EvrakDurumu.Portfoyde)
            .SumAsync(e => (decimal?)e.Tutar) ?? 0m;
        var tahsileVerilen = await kiraciHepsi.Where(e => e.Durum == EvrakDurumu.TahsileVerildi)
            .SumAsync(e => (decimal?)e.Tutar) ?? 0m;
        var buHafta = await kiraciHepsi
            .Where(e => (e.Durum == EvrakDurumu.Portfoyde || e.Durum == EvrakDurumu.TahsileVerildi)
                     && e.VadeGunu >= bugun && e.VadeGunu <= haftaSonu)
            .SumAsync(e => (decimal?)e.Tutar) ?? 0m;
        var karsiliksiz = await kiraciHepsi.Where(e => e.Durum == EvrakDurumu.Karsiliksiz)
            .SumAsync(e => (decimal?)e.Tutar) ?? 0m;

        return new EvrakListesi(
            toplamAdet, toplamTutar, limit, AzamiLimit, offset,
            DahaFazlasiVar: offset + satirlar.Count < toplamAdet,
            portfoyde, tahsileVerilen, buHafta, karsiliksiz,
            [.. satirlar.Select(x => Satira(x.E, x.KaynakAd, bugun))]);
    }

    /// <summary>
    /// Vade takvimi — GÜN BAŞINA adet + toplam (GROUP BY). Satır DÖNDÜRMEZ (TB-19).
    /// </summary>
    public async Task<Core.Sozlesmeler.EvrakTakvimi> Takvim(
        AktifKullanici islem, DateOnly? bas, DateOnly? bit, string? durum)
    {
        var bugun = Zaman.YerelBugun();
        var b = bas ?? bugun;
        var s = bit ?? bugun.AddDays(30);
        if (s < b) throw new BadRequestException("Bitiş günü başlangıçtan önce olamaz");
        // PAKET GOC-B (envanter C satır 31/32, K8e): yıl penceresi 2000..bugün+1 (ekranın Tarih Tur=Rapor'u ile aynı).
        GD.GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.TarihPenceresi.Denetle(b, Kuyumcu.Core.Domain.Girdi.TarihTuru.Rapor, bugun), "bas");
        GD.GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.TarihPenceresi.Denetle(s, Kuyumcu.Core.Domain.Girdi.TarihTuru.Rapor, bugun), "bit");

        var gunSayisi = s.DayNumber - b.DayNumber + 1;
        if (gunSayisi > AzamiTakvimGun)
            throw new BadRequestException(
                $"Takvim aralığı en çok {AzamiTakvimGun} gün olabilir (istenen: {gunSayisi})");

        var durumlar = string.IsNullOrWhiteSpace(durum)
            ? new[] { EvrakDurumu.Portfoyde, EvrakDurumu.TahsileVerildi }
            : durum.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var d in durumlar)
            if (!EvrakDurumu.Tumu.Contains(d))
                throw new BadRequestException($"Bilinmeyen durum: {d}");

        var gunler = await db.KiymetliEvraklar.AsNoTracking()
            .Where(e => e.TenantId == islem.TenantId && durumlar.Contains(e.Durum)
                     && e.VadeGunu >= b && e.VadeGunu <= s)
            .GroupBy(e => e.VadeGunu)
            // ⚠ ÖNCE anonim tip, SONRA sözleşme tipi: GROUP BY sonucunu
            //   doğrudan bir record kurucusuna yansıtmak EF Core'da SQL'e
            //   ÇEVRİLEMEZ ve sorgu istemciye düşerdi (tüm satırlar belleğe).
            .Select(g => new { Gun = g.Key, Adet = g.Count(), Tutar = g.Sum(x => x.Tutar) })
            .OrderBy(g => g.Gun)
            .ToListAsync();

        var kalemler = gunler
            .Select(g => new EvrakTakvimGunu(g.Gun, g.Adet, g.Tutar))
            .ToList();

        // QA turu 5 (YÜKSEK): aralık boş çıktığında ekran "portföyde evrak yok"
        // diyordu. Portföy özeti ARALIKTAN BAĞIMSIZ hesaplanır (UX-S33) ki ekran
        // doğru cümleyi kurabilsin ve aralığı portföye genişletebilsin.
        var portfoy = await db.KiymetliEvraklar.AsNoTracking()
            .Where(e => e.TenantId == islem.TenantId && durumlar.Contains(e.Durum))
            .GroupBy(e => 1)
            .Select(g => new
            {
                Adet = g.Count(),
                Tutar = g.Sum(x => x.Tutar),
                Ilk = g.Min(x => x.VadeGunu),
                Son = g.Max(x => x.VadeGunu),
            })
            .FirstOrDefaultAsync();

        return new Core.Sozlesmeler.EvrakTakvimi(
            b, s, kalemler.Sum(g => g.Adet), kalemler.Sum(g => g.Tutar), kalemler,
            portfoy?.Adet ?? 0, portfoy?.Tutar ?? 0m, portfoy?.Ilk, portfoy?.Son, AzamiTakvimGun);
    }

    public async Task<EvrakSatiri> Getir(string id, AktifKullanici islem)
    {
        var e = await EvrakBul(id, islem.TenantId);
        var ad = await db.Cariler.AsNoTracking()
            .Where(c => c.Id == e.KaynakCariId).Select(c => c.Ad).FirstOrDefaultAsync();
        return Satira(e, ad, Zaman.YerelBugun());
    }

    /// <summary>Cari kartındaki "bu müşterinin evrakları" kutusu — CariModulu şişmesin diye burada.</summary>
    public async Task<EvrakListesi> CariEvraklari(string cariId, AktifKullanici islem)
    {
        await IslemDogrulayici.CariDogrula(db, cariId, islem.TenantId);
        return await Listele(islem, durum: null, evrakTuru: null, yon: null, cariId: cariId,
            vadeBas: null, vadeBit: null, limit: AzamiLimit, offset: 0);
    }

    /// <summary>Hash üzerinden borçlu araması — şifreli sütunda eşitlik araması YAPILAMAZ.</summary>
    public async Task<EvrakListesi> KimlikIleAra(string kimlikNo, AktifKullanici islem)
    {
        if (string.IsNullOrWhiteSpace(kimlikNo))
            throw new BadRequestException("Aranacak kimlik no boş olamaz");

        var hash = db.Sifreleyici.Hashle(kimlikNo);
        var bugun = Zaman.YerelBugun();
        var satirlar = await db.KiymetliEvraklar.AsNoTracking()
            .Where(e => e.TenantId == islem.TenantId && e.BorcluKimlikNoHash == hash)
            .OrderBy(e => e.VadeGunu).Take(AzamiLimit)
            .ToListAsync();

        return new EvrakListesi(
            satirlar.Count, satirlar.Sum(x => x.Tutar), AzamiLimit, AzamiLimit, 0, false,
            0m, 0m, 0m, 0m, [.. satirlar.Select(x => Satira(x, null, bugun))]);
    }

    // ─── Yardımcılar ─────────────────────────────────────────────────────────

    /// <summary>
    /// ADR-0010: global sorgu filtresi VARKEN tenant şartı ELLE DE yazılır.
    /// Filtre bir kolaylıktır; kimlikle erişimin tek koruması olamaz.
    /// </summary>
    private async Task<KiymetliEvrak> EvrakBul(string id, string tenantId) =>
        await db.KiymetliEvraklar.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId)
        ?? throw new NotFoundException("Evrak bulunamadı");

    private static string? Bosaltilmis(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    /// <summary>
    /// PAKET GOC-B (envanter #9 + C satır 40/41): vade günü en çok 10 yıl ileri (K8c — 9999 yılı geçiyordu);
    /// keşide tarihi yıl penceresi (K8) ve <b>keşide ≤ vade</b> (vadeden sonra keşide edilmiş çek/senet olmaz).
    /// İleri tarihli keşide (5941 s. Çek Kanunu) TEYİTSİZ → serbest bırakıldı (bugünkü davranış).
    /// Metin GirdiSozlugu (ekranın alan altı metniyle aynı); geçmiş vade kuralı çağıranda (eski metin aynen).
    /// </summary>
    public static void TarihleriDenetle(DateOnly vade, DateOnly? keside, DateOnly bugun)
    {
        if (vade >= bugun)
            GD.GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.TarihPenceresi.Denetle(
                vade, Kuyumcu.Core.Domain.Girdi.TarihTuru.Vade, bugun), "vadeGunu");
        GD.GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.TarihPenceresi.Denetle(
            keside, Kuyumcu.Core.Domain.Girdi.TarihTuru.Keside, bugun, enGec: vade), "kesideTarihi");
    }

    /// <summary>
    /// PAKET TCKN-DOGRULAMA — borçlu kimlik no 11 hane rakamsa TC kimlik no doğrulama kuralı
    /// (tek kaynak Kuyumcu.Core KimlikNoDogrulayici) uygulanır; geçmezse 400 (alan borcluKimlikNo).
    /// Diğer biçimler (10 hane VKN, 10-20 karakter) mevcut DTO kuralıyla AYNEN kalır.
    /// </summary>
    private static void BorcluKimlikDenetle(string? no)
    {
        if (Kuyumcu.Core.Domain.KimlikNoDogrulayici.TcknVeyaVknDenetle(no) == Kuyumcu.Core.Domain.KimlikNoHatasi.TcknGecersiz)
            throw new BadRequestException(Kuyumcu.Core.Domain.KimlikNoDogrulayici.TcknGecersizMesaj).AlanIle("borcluKimlikNo");
        // PAKET GOC-B: borçlu kimlik no — rakamsa K4/K5 (10 VKN / 11 TCKN), harf içeriyorsa yabancı pasaport
        // no K7 (tur 2: paket öncesi yabancı borçlu girilebiliyordu). Ekranla AYNI Core kuralı ve sözlük metni.
        GD.GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.KimlikKurali.TcknVknVeyaPasaport(no?.Trim()), "borcluKimlikNo");
    }

    /// <summary>Saklanan biçim: kırpılmış; pasaport büyük harf (K7 ile aynı normalleştirme).</summary>
    private static string KimlikNormal(string no) =>
        Kuyumcu.Core.Domain.KimlikNoDogrulayici.YalnizRakam(no.Trim()) ? no.Trim()
            : Kuyumcu.Core.Domain.Girdi.PasaportKurali.Normallestir(no);

    public const string KimlikSilCelisikMesaj = "Kimlik no hem yazıldı hem kaldırılmak istendi; birini seçin.";

    /// <summary>
    /// Bakiye satırını delta ile günceller — ÇAĞIRANIN transaction'ı içinde,
    /// SaveChanges ÇAĞIRMADAN (CariServisi.BakiyeGuncelle ile aynı sözleşme).
    /// <para>
    /// ⚠ Döviz kolonuna ASLA dokunmaz: evrak Faz A'da yalnız TRY'dir ve döviz
    /// kolonuna yazan tek yer <c>DovizBakiye.Ekle</c>'dir (denetim.sh sayar).
    /// </para>
    /// </summary>
    private async Task BakiyeGuncelle(
        string cariId, string tenantId, decimal tlDelta, decimal hasGramDelta, DateTime now)
    {
        // PAKET CARI-BAKIYE: TEK YAZICI (Common/CariBakiyeServisi.cs).
        await CariBakiyeYazici.Uygula(db, tenantId, cariId, tlDelta, hasGramDelta, now);
    }

    /// <summary>
    /// Çift giriş ayırt edicisi. SENET'te banka yoktur; NULL bırakılsaydı
    /// MySQL UNIQUE indeksi çoklu NULL'a izin verdiği için aynı senet ikinci
    /// kez girilebilirdi.
    /// </summary>
    private static string AyirtEdici(string evrakTuru, string? bankaAdi) =>
        string.IsNullOrWhiteSpace(bankaAdi) ? evrakTuru : bankaAdi.Trim();

    private async Task<EvrakSonucu> SonucGetir(string evrakId, AktifKullanici islem, string cariId)
    {
        var satir = await Getir(evrakId, islem);
        var bakiye = await db.CariBakiyeler.AsNoTracking()
            .Where(b => b.CariId == cariId && b.TenantId == islem.TenantId)
            .Select(b => (decimal?)b.TlBakiye).FirstOrDefaultAsync();
        return new EvrakSonucu(satir, bakiye, null);
    }

    private static EvrakSatiri Satira(KiymetliEvrak e, string? kaynakAd, DateOnly bugun)
    {
        var acik = e.Durum is EvrakDurumu.Portfoyde or EvrakDurumu.TahsileVerildi;
        var gecikme = bugun.DayNumber - e.VadeGunu.DayNumber;
        return new EvrakSatiri(
            e.Id, e.EvrakTuru, e.Yon, e.Durum, e.Tutar, e.ParaBirimi, e.VadeGunu, e.EvrakNo,
            e.BankaAdi, e.BankaSube, e.KesideYeri, e.KesideTarihi,
            e.BorcluCariId, e.BorcluAd, e.KaynakCariId, kaynakAd, e.Notlar,
            GecikmeGun: gecikme > 0 ? gecikme : 0,
            Gecikmis: acik && gecikme > 0,
            YakindaVadeli: acik && gecikme <= 0 && -gecikme <= YakinUfukGun,
            Duzenlenebilir: e.Durum == EvrakDurumu.Portfoyde,
            IzinliGecisler: EvrakDurumKapisi.IzinliGecisler(e.Durum),
            BorcluKimlikKayitli: e.BorcluKimlikNo != null);   // PAKET GOC-B tur 2: değer DÖNMEZ, yalnız var/yok
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/evraklar")]
[Yetki.ModulYetkisi("evrak")]
public sealed class EvrakController(EvrakServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/evraklar — Evrak girişi (borç kapatır).</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public async Task<IActionResult> Gir([FromBody] EvrakGirisDto dto)
        => StatusCode(201, await servis.Gir(dto, Islem));

    /// <summary>GET /api/v1/evraklar — Portföy listesi.</summary>
    [HttpGet]
    public Task<Core.Sozlesmeler.EvrakListesi> Listele(
        [FromQuery] string? durum, [FromQuery] string? evrakTuru, [FromQuery] string? yon,
        [FromQuery] string? cariId, [FromQuery] DateOnly? vadeBas, [FromQuery] DateOnly? vadeBit,
        [FromQuery] int limit = 50, [FromQuery] int offset = 0)
        => servis.Listele(Islem, durum, evrakTuru, yon, cariId, vadeBas, vadeBit, limit, offset);

    /// <summary>GET /api/v1/evraklar/takvim — Gün başına adet + toplam.</summary>
    [HttpGet("takvim")]
    public Task<Core.Sozlesmeler.EvrakTakvimi> Takvim(
        [FromQuery] DateOnly? bas, [FromQuery] DateOnly? bit, [FromQuery] string? durum)
        => servis.Takvim(Islem, bas, bit, durum);

    /// <summary>GET /api/v1/evraklar/cari/{cariId} — Cari kartındaki evrak kutusu.</summary>
    [HttpGet("cari/{cariId}")]
    public Task<Core.Sozlesmeler.EvrakListesi> CariEvraklari(string cariId)
        => servis.CariEvraklari(cariId, Islem);

    /// <summary>GET /api/v1/evraklar/ara?kimlikNo= — Borçlu kimlik no ile arama (HASH üzerinden).</summary>
    [HttpGet("ara")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<Core.Sozlesmeler.EvrakListesi> Ara([FromQuery] string kimlikNo)
        => servis.KimlikIleAra(kimlikNo, Islem);

    /// <summary>GET /api/v1/evraklar/{id} — Evrak kartı.</summary>
    [HttpGet("{id}")]
    public Task<EvrakSatiri> Getir(string id) => servis.Getir(id, Islem);

    /// <summary>PATCH /api/v1/evraklar/{id} — Alan bazlı düzeltme (gönderilmeyen alan korunur).</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<EvrakSonucu> Duzelt(string id, [FromBody] EvrakDuzeltDto dto)
        => servis.Duzelt(id, dto, Islem);

    /// <summary>POST /api/v1/evraklar/{id}/tahsile-ver — Bankaya tahsile verir (defter satırı YOK).</summary>
    [HttpPost("{id}/tahsile-ver")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<EvrakSonucu> TahsileVer(string id, [FromBody] TahsileVerDto dto)
        => servis.TahsileVer(id, dto, Islem);

    /// <summary>POST /api/v1/evraklar/{id}/portfoye-al — Banka iade etti (defter satırı YOK).</summary>
    [HttpPost("{id}/portfoye-al")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<EvrakSonucu> PortfoyeAl(string id) => servis.PortfoyeAl(id, Islem);

    /// <summary>POST /api/v1/evraklar/{id}/tahsil-edildi — Para kasaya ya da bankaya girer.</summary>
    [HttpPost("{id}/tahsil-edildi")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<EvrakSonucu> TahsilEdildi(string id, [FromBody] TahsilEdildiDto dto)
        => servis.TahsilEdildi(id, dto, Islem);

    /// <summary>POST /api/v1/evraklar/{id}/ciro — Evrakı üçüncü cariye devret (P-21 · Y-21).</summary>
    [HttpPost("{id}/ciro")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<EvrakSonucu> Ciro(string id, [FromBody] CiroDto dto)
        => servis.Ciro(id, dto, Islem);

    /// <summary>POST /api/v1/evraklar/{id}/rucu — Ciro edilen evrak geri döndü (P-21 · Y-21).</summary>
    [HttpPost("{id}/rucu")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<EvrakSonucu> Rucu(string id, [FromBody] RucuDto dto)
        => servis.Rucu(id, dto, Islem);

    /// <summary>POST /api/v1/evraklar/{id}/karsiliksiz — Borç ters kayıtla geri doğar.</summary>
    [HttpPost("{id}/karsiliksiz")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<EvrakSonucu> Karsiliksiz(string id, [FromBody] KarsiliksizDto dto)
        => servis.Karsiliksiz(id, dto, Islem);
}
