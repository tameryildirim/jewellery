using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kuyumcu.Api.Modules.Kasa;

// ═════════════════════════════════════════════════════════════════════════════
// ÖRNEK MODÜL — diğer modüller bu deseni birebir izler:
//   • Namespace: Kuyumcu.Api.Modules.{Modul}
//   • Tek dosya: DTOs + Servis + Controller
//   • Rota ve JSON alan adları NestJS API'siyle birebir aynı (Türkçe camelCase)
//   • Hatalar Common.ApiException türevleriyle (NotFound/BadRequest/Conflict...)
//   • Çok tablolu yazmalar tek EF transaction içinde
// Kaynak: apps/api/src/modules/kasa/*
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class KasaOlusturDto
{
    [Required] public string Ad { get; set; } = null!;
    [Required] public string SubeId { get; set; } = null!;
    public string? ParaBirimi { get; set; }
    /// <summary>D1-11 — kasanın sorumlu çalışanı (isteğe bağlı; aynı işletmenin aktif kullanıcısı).</summary>
    [MaxLength(36)] public string? SorumluKullaniciId { get; set; }
}

/// <summary>D1-11 — kör sayım ayarı.</summary>
public sealed class KorSayimDto
{
    public bool Etkin { get; set; }
}

public sealed class KasaGuncelleDto
{
    /// <summary>
    /// Kasa adı — paket UX-1 Faz B (Bertan KOŞUL 1, BLOKLAYICI) ile ZORUNLU
    /// OLMAKTAN ÇIKTI.
    /// <para>
    /// Eskiden [Required] idi: "gövdede olmayan alan korunur" PATCH
    /// sözleşmesiyle çelişiyordu — yalnız para birimini değiştirmek isteyen
    /// istek 400 alırdı. Artık üç durum tek biçimde çözülür (UX-S26):
    ///   alan YOK → ad KORUNUR · alan dolu → yazılır · boş/null → 400
    ///   (ad silinemez, kasanın adı zorunlu bir alandır).
    /// </para>
    /// </summary>
    [MinLength(2, ErrorMessage = "Kasa adı en az 2 karakter olmalıdır")]
    [MaxLength(100, ErrorMessage = "Kasa adı en fazla 100 karakter olabilir")]
    public string? Ad { get; set; }

    /// <summary>
    /// Kasanın para birimi (TRY · USD · EUR · GBP) — UX-1 Faz B.
    /// ⚠ Mehmet K4: yalnız bakiye 0 VE hiç kasa hareketi VE hiç gün kapanışı
    /// yokken değişir; aksi hâlde 409 (geçmiş hareketlerin anlamı bozulurdu).
    /// </summary>
    [RegularExpression("TRY|USD|EUR|GBP", ErrorMessage = "Geçersiz para birimi")]
    public string? ParaBirimi { get; set; }

    /// <summary>Kasanın şubesi — para birimiyle AYNI kapıya tabidir.</summary>
    [MaxLength(36)] public string? SubeId { get; set; }

    /// <summary>
    /// UX-4d madde 13 (UX-S25/DoD): pasife alınan kasa GERİ AÇILAMIYORDU —
    /// cari, banka hesabı ve üründe `aktif` alanı vardı, kasada yoktu ve
    /// DELETE yalnız false yazıyordu. Artık aynı PATCH ile aktifleştirilir.
    /// Gönderilmezse dokunulmaz (PATCH sözleşmesi).
    /// </summary>
    public bool? Aktif { get; set; }

    /// <summary>
    /// D1-11 — kasanın sorumlu çalışanı. Gönderilmezse (null) dokunulmaz; "" sorumluyu kaldırır;
    /// kimlik verilirse aynı işletmenin AKTİF kullanıcısı olmalıdır.
    /// </summary>
    [MaxLength(36)] public string? SorumluKullaniciId { get; set; }
}

public sealed class GunKapanisiDto
{
    /// <summary>
    /// Çekmeceden fiilen SAYILAN nakit. Sıfır geçerli bir sayımdır; gönderilmemesi DEĞİLDİR.
    /// PAKET PATCH-5: <c>decimal?</c> — gövdede yoksa servis 400 alan:sayilanNakit verir
    /// (eskiden varsayılan 0 okunup "0 TL sayıldı" diye kapanıyordu).
    /// </summary>
    /// <para>PAKET SON-KALANLAR: DTO <c>[Range]</c> KALDIRILDI — aynı tavan kuralı iki cümleyle söyleniyordu
    /// ("0 ile 9.999.999.999.999,99 arasında" ↔ diğer uçlarda "çok büyük — kaydedilebilecek en büyük değeri
    /// aşıyor"). Alt sınır (eksi) ve tavan artık TEK yerde, servis içinde (HTTP + servis yolu aynı ileti):
    /// eksi → "Sayılan nakit eksi olamaz", tavan → <see cref="KolonTavani.Denetle(decimal,int,decimal,string,string)"/>.</para>
    /// <para>PAKET GOC-A: K9a (sıfır olabilir) Core kuralı — ekran Para Tur=SifirOlabilir ile AYNI metin; servis de aynı kuralı çağırır.</para>
    [Tutar(TutarTuru.SifirOlabilir)]
    public decimal? SayilanNakit { get; set; }

    /// <summary>Fark sıfır değilse ZORUNLU gerekçe.</summary>
    [MaxLength(1000)] public string? Aciklama { get; set; }
}

public sealed class GunAcmaDto
{
    /// <summary>Kapanmış günü yeniden açma gerekçesi — ZORUNLU, en az 20 karakter.</summary>
    [Required, MinLength(20), MaxLength(1000)] public string Gerekce { get; set; } = null!;
}

public sealed class KasaHareketDto
{
    /// <summary>
    /// Kasa hareketi yönü. UX-4d madde 6: kabul edilen değerler TEK KAYNAKtan
    /// (Kuyumcu.Core HareketYonu) gelir ve mesaj banka sözlüğüyle karışmayı
    /// açıkça anlatır. Sessiz eşleme YOK: "GELEN" gönderilirse 400 döner.
    /// </summary>
    [Required(ErrorMessage = "Yön zorunludur")]
    [RegularExpression(Kuyumcu.Core.Domain.HareketYonu.KasaDesen,
        ErrorMessage = Kuyumcu.Core.Domain.HareketYonu.KasaMesaj)]
    public string Yon { get; set; } = null!;

    // SON-KALANLAR tur 2 (doğrulayıcı DÜŞÜK): alt sınır TEK kapıda — servis KolonTavani.PozitifTutar
    // (önce 2 haneye yuvarla, sonra en az 0,01). DTO [Range(0.01)] HTTP yolunda 0,005'i servisten önce
    // reddediyor, servis yolu ve tahsilat 0,01 olarak kabul ediyordu (uçlar arası tutarsızlık).
    // PAKET GOC-A (K9): > 0, en çok 2 ondalık (SESSİZ YUVARLAMA YOK), decimal(15,2) tavanı — Core kuralı.
    [Required, Tutar]
    public decimal Tutar { get; set; }

    [Required, RegularExpression("NAKIT|KART|HAVALE|ALTIN|DIGER")]
    public string OdemeTuru { get; set; } = null!;

    [Range(0, 1)] public decimal? KomisyonOran { get; set; }
    [MaxLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")] public string? Aciklama { get; set; }
}

/// <summary>
/// PAKET P-12 (B-41) — kasa hareketinin FİNANSAL OLMAYAN alanlarının düzeltmesi.
/// <para>
/// ⚠ Burada tutar/ödeme türü/komisyon/kasa ALANI YOKTUR ve eklenmeyecektir:
/// finansal düzeltmenin tek yolu STORNO + yeni kayıttır (PLAN § 1 md.8,
/// denetim.sh <c>STORNO_DISI_FINANSAL_PATCH</c>).
/// </para>
/// <para>
/// ⚠ Üç durumlu PATCH: <c>...Verildi</c> bayrağı JSON'da anahtarın VARLIĞINDAN
/// doğar (EvrakDuzeltDto deseni). Gönderilmeyen alan "değişmedi" demektir.
/// </para>
/// </summary>
public sealed class KasaHareketDuzeltDto
{
    private string? aciklama;
    [System.Text.Json.Serialization.JsonIgnore] public bool AciklamaVerildi { get; private set; }
    [MaxLength(500)]
    public string? Aciklama { get => aciklama; set { aciklama = value; AciklamaVerildi = true; } }

    private DateTime? tarih;
    [System.Text.Json.Serialization.JsonIgnore] public bool TarihVerildi { get; private set; }
    public DateTime? Tarih { get => tarih; set { tarih = value; TarihVerildi = true; } }
}

// ── Servis ───────────────────────────────────────────────────────────────────

/// <summary>PAKET Q9-5 — kasa şube/para birimi kilidinin sebep kodları (ekran sözlükten çevirir).</summary>
public static class KasaDegisimKilidi
{
    public const string Bakiye = "BAKIYE";
    public const string Hareket = "HAREKET";
    public const string GunKapanisi = "GUN_KAPANISI";
}

public sealed class KasaServisi(KuyumcuDbContext db, ILogger<KasaServisi> logger, DenetimServisi? denetimServisi = null)
{
    /// <summary>D1-11 — kör sayımda tutarsız kapanış iletisi (tutar YAZMAZ).</summary>
    public const string KorSayimFarkIletisi = "Sayım defterle tutmadı. Farkın nedenini yazıp yeniden kaydedin; fark kapanıştan sonra görünür.";

    /// <summary>D1-11 — kör sayım açık mı (kiracı ayarı; yoksa KAPALI).</summary>
    public async Task<bool> KorSayimEtkin(string tenantId) =>
        await db.KiraciAyarlari.AsNoTracking()
            .AnyAsync(a => a.TenantId == tenantId && a.Anahtar == KiraciAyarAnahtari.KasaKorSayim && a.Deger == "true");

    /// <summary>Kör sayım bu kullanıcıdan tutarı gizler mi? SAHİP her zaman görür.</summary>
    private async Task<bool> KorSayimGizler(AktifKullanici islem) => islem.Rol != "SAHIP" && await KorSayimEtkin(islem.TenantId);

    /// <summary>D1-11 — kör sayım ayarını yazar (SAHİP/MÜDÜR); denetim izine düşer.</summary>
    public async Task<bool> KorSayimYaz(bool etkin, AktifKullanici islem)
    {
        var a = await db.KiraciAyarlari.FirstOrDefaultAsync(x => x.TenantId == islem.TenantId && x.Anahtar == KiraciAyarAnahtari.KasaKorSayim);
        if (a is null)
        {
            a = new KiraciAyari { Id = Kimlik.Ulid(), TenantId = islem.TenantId, Anahtar = KiraciAyarAnahtari.KasaKorSayim, Deger = "false" };
            db.KiraciAyarlari.Add(a);
        }
        a.Deger = etkin ? "true" : "false";
        a.Guncellendi = DateTime.UtcNow;
        denetimServisi?.YazmaKaydet(islem, nameof(KiraciAyari), a.Id, $"kör sayım {(etkin ? "açıldı" : "kapatıldı")}", hassas: false);
        await db.SaveChangesAsync();
        return etkin;
    }

    /// <summary>
    /// PAKET DIL-SUNUCU: kapanış farkı iletisi — DÖRT TAM cümle (parça birleştirme YOK); her biri
    /// SunucuIletiSozlugu'nda tam şablon olarak durur ve istek diline göre seçilir.
    /// </summary>
    internal static string KapanisFarkIletisi(string fark, bool fazla, string beklenen, bool kartHavaleVar, string kart, string havale) =>
        (fazla, kartHavaleVar) switch
        {
            (true, true) => $"Sayılan nakit beklenenden {fark} fazla (beklenen nakit {beklenen}; kasadaki kart {kart} ve havale {havale} sayıma girmez). Farkın nedenini yazın.",
            (true, false) => $"Sayılan nakit beklenenden {fark} fazla (beklenen nakit {beklenen}). Farkın nedenini yazın.",
            (false, true) => $"Sayılan nakit beklenenden {fark} eksik (beklenen nakit {beklenen}; kasadaki kart {kart} ve havale {havale} sayıma girmez). Farkın nedenini yazın.",
            _ => $"Sayılan nakit beklenenden {fark} eksik (beklenen nakit {beklenen}). Farkın nedenini yazın.",
        };

    /// <summary>
    /// Denetim izi. DI'sız test kurulumları (new KasaServisi(db, logger)) için
    /// opsiyoneldir; verilmediğinde iz YAZILMAZ ama iş kuralı da değişmez.
    /// Üretimde DI her zaman gerçek servisi verir (ModulKayit).
    /// </summary>
    private DenetimServisi? denetim => denetimServisi;

    public async Task<object> Olustur(KasaOlusturDto dto, AktifKullanici islem)
    {
        // Mehmet'in K4 denetimi (2026-09-14): `dto.SubeId` doğrulanmadan
        // yazılıyordu. `StokSayimGuncelle`de kapatılan aynı kusur burada
        // duruyordu — başka kiracının ya da hiç var olmayan bir şubenin
        // kimliğine bağlı kasa açılabiliyordu.
        // PAKET PATCH-5: ad varchar(100) (1406 → 500 olurdu) ve para birimi listesi servis içinde;
        // RegularExpression "" değerini geçerli saydığı için "" kasaya para birimi olarak yazılıyordu.
        var olusturAd = (dto.Ad ?? "").Trim();
        if (olusturAd.Length == 0) throw new BadRequestException("Kasa adı zorunludur").AlanIle("ad");
        // GOC-A (envanter C · kasa-ad): oluşturmada en az 2 yoktu, düzenlemede vardı ("A" adlı kasa açılıp
        // sonra adı değiştirilmeden kaydedilemiyordu) — iki yol AYNI kural ve metin (KasaGuncelleDto).
        if (olusturAd.Length < 2) throw new BadRequestException("Kasa adı en az 2 karakter olmalıdır").AlanIle("ad");
        KolonTavani.MetinSiniri(olusturAd, AlanSinirlari.KasaAdi, "ad", "Kasa adı");
        dto.Ad = olusturAd;
        dto.ParaBirimi = KolonTavani.ParaBirimi(string.IsNullOrEmpty(dto.ParaBirimi) ? null : dto.ParaBirimi, "paraBirimi");
        await IslemDogrulayici.SubeDogrula(db, dto.SubeId, islem.TenantId);

        var mevcutAd = await db.Kasalar.AnyAsync(k =>
            k.TenantId == islem.TenantId && k.SubeId == dto.SubeId && k.Ad == dto.Ad);
        if (mevcutAd) throw new ConflictException($"'{dto.Ad}' adlı kasa bu şubede zaten mevcut");

        // D1-11: sorumlu çalışan oluşturmada da verilebilir (güncellemeyle AYNI kural — UX-S26).
        var sorumlu = string.IsNullOrWhiteSpace(dto.SorumluKullaniciId) ? null : dto.SorumluKullaniciId.Trim();
        if (sorumlu is not null && !await db.Kullanicilar.AnyAsync(k => k.Id == sorumlu && k.TenantId == islem.TenantId && k.Aktif))
            throw new BadRequestException("Sorumlu çalışan bulunamadı ya da pasif").AlanIle("sorumluKullaniciId");

        var kasa = new Data.Kasa
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            SubeId = dto.SubeId,
            Ad = dto.Ad,
            SorumluKullaniciId = sorumlu,
            ParaBirimi = dto.ParaBirimi ?? "TRY",
            Bakiye = 0m,
            Aktif = true,
            Guncellendi = DateTime.UtcNow,
        };
        db.Kasalar.Add(kasa);
        await db.SaveChangesAsync();
        return await Getir(kasa.Id, islem);
    }

    public async Task<List<Data.Kasa>> Listele(AktifKullanici islem, string? subeId)
    {
        // PAKET ŞUBE-KAPSAM-A: görünüm şubesi TEK yoldan çözülür (kilitli rol başka
        // şube → 403, pasif/yabancı şube → 404) ve TEK yoldan daraltılır.
        var sube = await GorunumSubesi.Coz(db, islem, subeId);
        var q = db.Kasalar.AsNoTracking().Daralt(sube).Where(k => k.TenantId == islem.TenantId);
        // SATIS-3 t6: belirli sıra — ekran "ilk TL kasası"nı varsayılan yapar; ULID kimlik = açılış sırası.
        return await q.OrderBy(k => k.Id).ToListAsync();
    }

    public async Task<object> Getir(string id, AktifKullanici islem)
    {
        var kasa = await db.Kasalar.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == id && k.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Kasa bulunamadı");
        return kasa;
    }

    public async Task<object> Guncelle(string id, KasaGuncelleDto dto, AktifKullanici islem)
    {
        var kasa = await TenantKasasiBul(id, islem.TenantId);
        var degisen = new List<string>();

        // DataAnnotations YALNIZ controller yolunda koşar → servis içi elle kontrol.
        if (dto.Ad is not null)
        {
            var ad = dto.Ad.Trim();
            if (ad.Length < 2) throw new BadRequestException("Kasa adı en az 2 karakter olmalıdır").AlanIle("ad");
            KolonTavani.MetinSiniri(ad, 100, "ad", "Kasa adı");

            // Aynı şubede aynı ada sahip başka kasa varsa çakışma
            var hedefSube = dto.SubeId ?? kasa.SubeId;
            var mevcutAd = await db.Kasalar.AnyAsync(k =>
                k.TenantId == islem.TenantId && k.SubeId == hedefSube && k.Ad == ad && k.Id != id);
            if (mevcutAd) throw new ConflictException($"'{ad}' adlı kasa bu şubede zaten mevcut");

            if (ad != kasa.Ad) { kasa.Ad = ad; degisen.Add("ad"); }
        }

        // ── Para birimi / şube: GEÇMİŞİ OLAN KASADA DEĞİŞMEZ (Mehmet K4) ─────
        // Bakiye == 0 YETMEZ: kasa_hareketleri ve gun_kapanislari da boş olmalı.
        // Bakiyesi 0'a dönmüş ama geçmişi olan kasada para birimi değişirse
        // geçmiş dekontların anlamı geriye dönük bozulur.
        KolonTavani.ParaBirimi(dto.ParaBirimi, "paraBirimi");   // PATCH-5: "" / "XYZ" → 400 (null = koru)
        var paraBirimiDegisiyor = dto.ParaBirimi is not null && dto.ParaBirimi != kasa.ParaBirimi;
        var subeDegisiyor = dto.SubeId is not null && dto.SubeId != kasa.SubeId;
        if (paraBirimiDegisiyor || subeDegisiyor)
        {
            await GecmisiBosMu(kasa, islem, paraBirimiDegisiyor ? "paraBirimi" : "subeId");   // SON-KALANLAR: alanlı 409
            if (subeDegisiyor)
            {
                await IslemDogrulayici.SubeDogrula(db, dto.SubeId!, islem.TenantId);
                kasa.SubeId = dto.SubeId!;
                degisen.Add("sube_id");
            }
            if (paraBirimiDegisiyor) { kasa.ParaBirimi = dto.ParaBirimi!; degisen.Add("para_birimi"); }
        }

        // D1-11: sorumlu çalışan (null = koru · "" = kaldır · kimlik = aynı işletmenin aktif kullanıcısı).
        if (dto.SorumluKullaniciId is { } sorumlu)
        {
            var yeni = sorumlu.Trim().Length == 0 ? null : sorumlu.Trim();
            if (yeni is not null && !await db.Kullanicilar.AnyAsync(k => k.Id == yeni && k.TenantId == islem.TenantId && k.Aktif))
                throw new BadRequestException("Sorumlu çalışan bulunamadı ya da pasif").AlanIle("sorumluKullaniciId");
            if (yeni != kasa.SorumluKullaniciId) { kasa.SorumluKullaniciId = yeni; degisen.Add("sorumlu_kullanici_id"); }
        }

        // UX-4d madde 13: aktif/pasif geçişi. Pasife alma kapısı DeaktifEt ile
        // AYNI kuraldır (bakiyesi sıfır olmayan kasa pasife alınamaz) —
        // ikinci bir kural yazılmadı.
        if (dto.Aktif is { } yeniAktif && yeniAktif != kasa.Aktif)
        {
            if (!yeniAktif && kasa.Bakiye != 0m)
                throw new BadRequestException("Bakiyesi sıfır olmayan kasa pasife alınamaz");
            kasa.Aktif = yeniAktif;
            degisen.Add(yeniAktif ? "aktif(açıldı)" : "aktif(pasife alındı)");
            denetim?.YazmaKaydet(islem, nameof(Data.Kasa), kasa.Id,
                yeniAktif ? "kasa aktifleştirildi" : "kasa pasife alındı", hassas: false);
        }

        // Mehmet (UX-1 Faz B): her PATCH asgari denetim kaydı bırakır —
        // kim/ne zaman/hangi varlık/hangi ALANLAR (değer değil).
        if (degisen.Count > 0)
            denetim?.YazmaKaydet(islem, nameof(Data.Kasa), kasa.Id,
                $"kasa güncellendi · değişen alanlar: {string.Join(", ", degisen)}", hassas: false);

        kasa.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await Getir(id, islem);
    }

    /// <summary>
    /// Kasanın geçmişi TAMAMEN boş mu? (Mehmet K4 — üç kaynak)
    /// Bakiye · kasa_hareketleri · gun_kapanislari. Biri doluysa 409.
    /// Kural <see cref="DegisimKilidiSebebi"/>'ndedir; burası yalnız 409'a çevirir.
    /// </summary>
    private async Task GecmisiBosMu(Data.Kasa kasa, AktifKullanici islem, string alan)
    {
        switch (await DegisimKilidiSebebi(kasa, islem))
        {
            case KasaDegisimKilidi.Bakiye:
                throw new ConflictException(
                    "Bakiyesi sıfır olmayan kasanın para birimi ya da şubesi değiştirilemez").AlanIle(alan);
            case KasaDegisimKilidi.Hareket:
                throw new ConflictException(
                    "Hareket görmüş kasanın para birimi ya da şubesi değiştirilemez — yeni kasa açın").AlanIle(alan);
            case KasaDegisimKilidi.GunKapanisi:
                throw new ConflictException(
                    "Gün kapanışı yapılmış kasanın para birimi ya da şubesi değiştirilemez — yeni kasa açın").AlanIle(alan);
        }
    }

    /// <summary>
    /// PAKET Q9-5 (QA turu 9, YÜKSEK) — şube/para birimi kilidinin SEBEBİ,
    /// kaydetmeden ÖNCE (UX-S30: kilitli alan sebebini söyler).
    /// <para>
    /// ⚠ NEDEN KİLİT (ölçüldü, tahmin değil): <c>kasa_hareketleri</c> tablosunda
    /// <c>sube_id</c> YOKTUR; şube raporları (SatisRaporlari, GiderOlcumu) hareketin
    /// şubesini KASANIN şubesinden türetir. Geçmişi olan kasanın şubesi
    /// değişirse o kasanın bütün eski hareketleri geriye dönük olarak başka
    /// şubenin raporuna taşınırdı. Bu yüzden şube yalnız geçmişi boş kasada
    /// değişir; kural <see cref="GecmisiBosMu"/> ile TEK kaynaktır.
    /// </para>
    /// </summary>
    /// <returns>null = değişebilir; aksi hâlde <see cref="KasaDegisimKilidi"/> kodu.</returns>
    public async Task<string?> DegisimKilidiSebebi(Data.Kasa kasa, AktifKullanici islem)
    {
        if (kasa.Bakiye != 0m) return KasaDegisimKilidi.Bakiye;
        if (await db.KasaHareketleri.AsNoTracking()
                .AnyAsync(h => h.TenantId == islem.TenantId && h.KasaId == kasa.Id))
            return KasaDegisimKilidi.Hareket;
        if (await db.Set<GunKapanisi>().AsNoTracking()
                .AnyAsync(g => g.TenantId == islem.TenantId && g.KasaId == kasa.Id))
            return KasaDegisimKilidi.GunKapanisi;
        return null;
    }

    /// <summary>GET /kasalar/{id}/degisim-kilidi yanıtı — ekran kilidi kaydetmeden önce görür.</summary>
    public async Task<object> DegisimKilidi(string id, AktifKullanici islem)
    {
        var kasa = await TenantKasasiBul(id, islem.TenantId);
        var sebep = await DegisimKilidiSebebi(kasa, islem);
        return new { kasaId = kasa.Id, kilitli = sebep is not null, sebep };
    }

    /// <summary>Satış/alış dışı nakit hareketleri: gider, transfer, sayım farkı vb.</summary>
    public async Task<object> HareketGir(string id, KasaHareketDto dto, AktifKullanici islem)
    {
        var kasa = await TenantKasasiBul(id, islem.TenantId);

        var yonKatsayi = dto.Yon == Kuyumcu.Core.Domain.HareketYonu.KasaGiris ? 1m : -1m;
        // PAKET PATCH-5 — GİRDİ kapıları (servis içi; DataAnnotations yalnız controller yolunda):
        // tutar önce yuvarlanır sonra ≤ 0 (0,004 → 0,00 satırı doğmaz) + kolon tavanı;
        // komisyon oranı 0..1 ve en çok 4 ondalık (kolon decimal(5,4) — sessiz yuvarlama yok);
        // açıklama varchar(500). PARA KURALLARI (nakit/araç payı kapısı) DEĞİŞMEDİ.
        // PAKET GOC-A (K9): 2'den fazla ondalık SESSİZCE YUVARLANMAZ (0,005 → 0,01 olmuyor) — Core kuralı + sözlük metni.
        GirdiKapisi.Tutar(dto.Tutar, "tutar");
        var tutar = KolonTavani.PozitifTutar(dto.Tutar, "tutar", "Tutar");
        if (dto.KomisyonOran is < 0m or > 1m)
            throw new BadRequestException("Komisyon oranı 0 ile 1 arasında olmalıdır").AlanIle("komisyonOran");
        var komisyon = KolonTavani.OndalikSiniri(dto.KomisyonOran, 4, "komisyonOran", "Komisyon oranı") ?? 0m;
        KolonTavani.MetinSiniri(dto.Aciklama?.Trim(), 500, "aciklama", "Açıklama");
        // PAKET PARA-DEMO tur 2 (hasım KRİTİK): bakiyeye yazılan NET tutar harekete de
        // AYNEN yazılır (Σ hareket = bakiye değişmezi). Eskiden hareket BRÜT, bakiye NET
        // yazılıyordu → nakit defteri (bakiye − Σkart) komisyon kadar kayıyor, sayımda
        // sahte fazla çıkıyordu; stornosu da brütü geri aldığı için bakiyeyi bozuyordu.
        // Komisyon oranı satırda kalır: brüt = net / (1 − oran) okunabilir.
        var netTutar = Math.Round(tutar * yonKatsayi * (1m - komisyon), 2, MidpointRounding.AwayFromZero);
        // PATCH-5: komisyon düşülünce 0,00 kalan hareket "sıfır tutarlı satır" olurdu.
        if (netTutar == 0m)
            throw new BadRequestException(
                "Komisyon düşüldükten sonra kasaya yazılacak tutar 0,00 kalıyor; oranı ya da tutarı düzeltin.")
                .AlanIle("komisyonOran");
        KolonTavani.BakiyeSigar(kasa.Bakiye + netTutar, "tutar", "Tutar");
        var now = DateTime.UtcNow;

        // Elle kasa hareketi de gün kilidine tabidir; aksi hâlde kapanmış gün
        // bu uçtan sessizce delinirdi.
        await GunKapanisiKapisi.Dogrula(db, islem.TenantId, id, now);

        // PAKET P-44: elle kasa ÇIKIŞI da bakiye kapısına tabidir. Bu uç
        // kapısızdı: çekmecede olmayan para bu yoldan çıkarılabiliyordu.
        // PARA-DEMO-2 (madde 3): NAKİT çıkışı ayrıca nakit payına bakar (kart/havale payı nakit sanılmaz).
        // Tur 5 (doğrulayıcı S16): elle KART/HAVALE çıkışı kendi payını aşamaz (aracPayiKapisi).
        await KasaBakiyeKapisi.NakitDogrula(db, islem.TenantId, kasa, netTutar, dto.OdemeTuru, aracPayiKapisi: true);

        await using var tx = await db.Database.BeginTransactionAsync();
        await BakiyeKilidi.Kilitle(db, kasa);   // PAKET KAPORA-ESZAMAN tur 2: kilitli taze okuma (kayıp güncelleme)
        kasa.Bakiye += netTutar;
        kasa.Guncellendi = now;

        var hareketId = Kimlik.Ulid();
        db.KasaHareketleri.Add(new KasaHareket
        {
            Id = hareketId,
            TenantId = islem.TenantId,
            KasaId = id,
            Tutar = netTutar,
            ParaBirimi = kasa.ParaBirimi,
            OdemeTuru = dto.OdemeTuru,
            KomisyonOran = komisyon,
            // PAKET S-3b: açıklama artık KAYDEDİLİR. DTO'da alan vardı, ekran
            // gönderiyordu, sunucu sessizce ATIYORDU (Anayasa md.1 ihlali):
            // kullanıcının yazdığı "kira ödemesi" geçmişte hiç görünmüyordu.
            Aciklama = string.IsNullOrWhiteSpace(dto.Aciklama) ? null : dto.Aciklama.Trim(),
            Tarih = now,
            Olusturuldu = now,
        });
        // Elle kasa hareketi defter dışı bir bakiye değişikliğidir; gerekçesi
        // (açıklama) ve yönü denetim izine yazılır.
        denetimServisi?.YazmaKaydet(islem, "KasaHareketi", hareketId,
            $"elle kasa hareketi: {dto.Yon} {dto.Tutar.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} {kasa.ParaBirimi}"
            + (string.IsNullOrWhiteSpace(dto.Aciklama) ? "" : $" · açıklama: {dto.Aciklama.Trim()}"),
            hassas: false, kasaId: kasa.Id);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return await Getir(id, islem);
    }

    // ─── Gün sonu kapanışı ────────────────────────────────────────────────────
    /// <summary>
    /// Kasanın YEREL gününü kapatır: sayılan nakit ile defter bakiyesi
    /// karşılaştırılır, fark kaydedilir ve güne yeni hareket yazılması durur.
    ///
    /// ⚠ FARK VARSA GEREKÇE ZORUNLUDUR. Gerekçesiz kaydedilen fark, ertesi gün
    /// "neden eksik" sorusunu cevapsız bırakır; sayımın tek değeri o cevaptır.
    /// </summary>
    public async Task<object> GunKapat(string id, GunKapanisiDto dto, AktifKullanici islem)
    {
        // PAKET PARA-DEMO tur 2: DTO [Range] yalnız controller yolunda çalışır; servis içi
        // ELLE kontrol (değişmez kural). Eksi nakit sayılamaz.
        // PAKET PATCH-5 (PARA-DEMO-2 hasımı DÜŞÜK): sayılan nakit SUNUCUDA ZORUNLU. Eskiden alan
        // decimal (null olamaz) idi: gövdede hiç yoksa 0 okunuyor, sayılmamış kasa "0 TL sayıldı"
        // diye kapanıyordu (ekran boş gönderimi engelliyordu, API'yi doğrudan çağıran engellenmiyordu).
        if (dto.SayilanNakit is not { } sayilanHam)
            throw new BadRequestException(
                "Sayılan nakit zorunludur — çekmecede sayılan tutarı girin (hiç para yoksa 0 yazın).")
                .AlanIle("sayilanNakit");
        // PAKET SON-KALANLAR (hasım DÜŞÜK): eksi kontrolü HAM değere yapılır. Eskiden önce yuvarlanıyordu:
        // −0,004 → 0,00 olup "0 sayıldı" diye kabul ediliyordu (servis yolu; HTTP'de DTO Range reddediyordu).
        // Kullanıcı eksi bir sayı yazdıysa sıfır SAYILMAZ. Ardından yuvarla (kolon decimal(15,2)) + tavan.
        // PAKET GOC-A: eksi / 2'den fazla ondalık / tavan → Core K9a (ekranla aynı metin; yuvarlama YOK).
        GirdiKapisi.Tutar(sayilanHam, "sayilanNakit", TutarTuru.SifirOlabilir);
        var sayilan = KolonTavani.IsaretliTutar(sayilanHam, "sayilanNakit", "Sayılan nakit");
        var kasa = await TenantKasasiBul(id, islem.TenantId);
        var now = DateTime.UtcNow;
        var yerelGun = DateOnly.FromDateTime(Zaman.UtcdenYerele(now));

        var mevcut = await db.GunKapanislari
            .FirstOrDefaultAsync(g => g.TenantId == islem.TenantId
                                   && g.KasaId == id && g.YerelGun == yerelGun);

        if (mevcut is not null && mevcut.AcilisZamani is null)
            throw new ConflictException(
                $"{yerelGun:dd.MM.yyyy} günü bu kasa için zaten kapatılmış.");

        // PAKET PARA-DEMO (madde 1): sayılan nakit YALNIZ nakit defteriyle karşılaştırılır;
        // kasaya yazılmış kart ve havale çekmecede sayılamaz (KasaNakitAyrimi gerekçesi).
        var ayrim = await KasaNakitAyrimi.Hesapla(db, islem.TenantId, kasa);
        var nakitDefter = ayrim.Nakit;
        var fark = Math.Round(sayilan - nakitDefter, 2, MidpointRounding.AwayFromZero);

        // PAKET SON-KALANLAR (hasım DÜŞÜK): red gerekçe ALANINA bağlı (alan altı mesaj, DoD EK md.3);
        // ileti sade: büyük harf yok, "TL" yerine kasanın para simgesi, farkın YÖNÜ yazılı (fazla/eksik).
        // D1-11 KÖR SAYIM: fark var, gerekçe yok → tutar SÖYLENMEZ; deneme denetim izine yazılır (sayımı değiştirip
        // yeniden deneyen görünür kalır).
        if (fark != 0m && string.IsNullOrWhiteSpace(dto.Aciklama) && await KorSayimGizler(islem))
        {
            denetimServisi?.YazmaKaydet(islem, nameof(GunKapanisi), kasa.Id,
                FormattableString.Invariant($"kör sayım denemesi · sayılan {sayilan:0.00} · tutmadı"), hassas: false, kasaId: kasa.Id);
            await db.SaveChangesAsync();
            throw new BadRequestException(KorSayimFarkIletisi).AlanIle("aciklama");
        }
        if (fark != 0m && string.IsNullOrWhiteSpace(dto.Aciklama))
            throw new BadRequestException(KapanisFarkIletisi(
                GunKapanisiKapisi.Para(Math.Abs(fark), kasa.ParaBirimi), fark > 0m,
                GunKapanisiKapisi.Para(nakitDefter, kasa.ParaBirimi),
                ayrim.Kart != 0m || ayrim.Havale != 0m,
                GunKapanisiKapisi.Para(ayrim.Kart, kasa.ParaBirimi),
                GunKapanisiKapisi.Para(ayrim.Havale, kasa.ParaBirimi))).AlanIle("aciklama");
        // PAKET GOC-A (envanter C · gun-aciklama / kr-sayim-aciklama): kasa farkı gerekçesi tek harfle geçiyordu —
        // K30 proje kuralı (en az 20, en çok 1000; gün açma ve storno ile AYNI) ve AYNI sözlük metni.
        if (fark != 0m) dto.Aciklama = GirdiKapisi.Gerekce(dto.Aciklama, "aciklama");
        else dto.Aciklama = GirdiKapisi.Uzunluk(dto.Aciklama, AlanSinirlari.GerekceEnCok, "aciklama");

        if (mevcut is not null)
        {
            // Yeniden açılmış gün tekrar kapatılıyor: aynı satır güncellenir,
            // böylece kasa+gün benzersizliği korunur ve tarihçe tek yerde kalır.
            mevcut.SayilanNakit = sayilan;
            mevcut.DefterBakiye = nakitDefter;   // PARA-DEMO: karşılaştırılan NAKİT defteri (fark = sayılan − bu)
            mevcut.Fark = fark;
            mevcut.Aciklama = dto.Aciklama;
            mevcut.KapatanKullaniciId = islem.Id;
            mevcut.KapanisZamani = now;
            mevcut.AcanKullaniciId = null;
            mevcut.AcilisZamani = null;
            mevcut.AcilisGerekcesi = null;
        }
        else
        {
            db.GunKapanislari.Add(new GunKapanisi
            {
                Id = Kimlik.Ulid(),
                TenantId = islem.TenantId,
                KasaId = id,
                SubeId = kasa.SubeId,
                YerelGun = yerelGun,
                SayilanNakit = sayilan,
                DefterBakiye = nakitDefter,   // PARA-DEMO: karşılaştırılan NAKİT defteri (fark = sayılan − bu)
                Fark = fark,
                Aciklama = dto.Aciklama,
                KapatanKullaniciId = islem.Id,
                KapanisZamani = now,
                Olusturuldu = now,
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation(
            "[{TenantId}] GUN_KAPANIS: kasa={KasaId} gun={Gun} sayilan={Sayilan} defter={Defter} fark={Fark}",
            islem.TenantId, id, yerelGun, sayilan, nakitDefter, fark);

        return new
        {
            kasaId = id, yerelGun, sayilanNakit = sayilan,
            // PARA-DEMO: defterBakiye = karşılaştırılan NAKİT defteri; toplam ve kart/havale ayrı (UX-S27).
            defterBakiye = nakitDefter, toplamBakiye = ayrim.ToplamBakiye,
            kartToplam = ayrim.Kart, havaleToplam = ayrim.Havale,
            fark, aciklama = dto.Aciklama, kilitli = true,
        };
    }

    /// <summary>
    /// PAKET PARA-DEMO (madde 1) — Günü Kapat penceresinin ÖNİZLEMESİ: toplam bakiye,
    /// sayıma girmeyen kart ve havale, sayılacak NAKİT defteri. Ekran farkı bu değerle
    /// hesaplar; sunucu kapanışta AYNI hesabı (KasaNakitAyrimi) yeniden yapar.
    /// </summary>
    public async Task<Kuyumcu.Contracts.Sayfalar.Kasalar.GunKapanisiOnizleme> GunKapanisiOnizleme(string id, AktifKullanici islem)
    {
        var kasa = await TenantKasasiBul(id, islem.TenantId);
        // D1-11 KÖR SAYIM: SAHİP dışındaki rol beklenen tutarı GÖRMEZ — sunucu tutar GÖNDERMEZ (ekranda gizlemek yetmez).
        if (await KorSayimGizler(islem))
            return new Kuyumcu.Contracts.Sayfalar.Kasalar.GunKapanisiOnizleme(0m, 0m, 0m, 0m, Gizli: true);
        var ayrim = await KasaNakitAyrimi.Hesapla(db, islem.TenantId, kasa);
        // TEK TİP (SozlesmeKapsam): ekranın okuduğu record'un AYNISI döner.
        return new Kuyumcu.Contracts.Sayfalar.Kasalar.GunKapanisiOnizleme(
            ayrim.ToplamBakiye, ayrim.Kart, ayrim.Havale, ayrim.Nakit);
    }

    /// <summary>
    /// PAKET PARA-DEMO-2 (madde 4) — görünümdeki kasaların nakit / kart / havale ayrımı (salt okunur,
    /// türetilmiş; yeni kayıt türü YOK). Tek gruplu sorgu: N kasa için N istek atılmaz.
    /// </summary>
    public async Task<List<Kuyumcu.Contracts.Sayfalar.Kasalar.KasaNakitAyrimiOzeti>> NakitAyrimlari(AktifKullanici islem, string? subeId)
    {
        var kasalar = await Listele(islem, subeId);
        var idler = kasalar.Select(k => k.Id).ToList();
        var toplamlar = await db.KasaHareketleri.AsNoTracking()
                .Where(h => h.TenantId == islem.TenantId && idler.Contains(h.KasaId)
                         && (h.OdemeTuru == OdemeAraci.Kart || h.OdemeTuru == OdemeAraci.Havale))
                .GroupBy(h => new { h.KasaId, h.OdemeTuru })
                .Select(g => new { g.Key.KasaId, g.Key.OdemeTuru, Toplam = g.Sum(x => x.Tutar) })
                .ToListAsync();
        static decimal Y(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);
        // PAKET SON-KALANLAR: bugün (yerel gün) kapanmış ve yeniden açılmamış kasalar — GunKapanisiKapisi ile AYNI ölçüt.
        var bugun = DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));
        var kapaliIdler = (await db.GunKapanislari.AsNoTracking()
                .Where(g => g.TenantId == islem.TenantId && idler.Contains(g.KasaId)
                         && g.YerelGun == bugun && g.AcilisZamani == null)
                .Select(g => g.KasaId)
                .Take(idler.Count)   // kasa+gün benzersiz → en çok kasa sayısı kadar satır (B-8 sınırlı sorgu)
                .ToListAsync()).ToHashSet();
        return kasalar.Select(k =>
        {
            var kart = Y(toplamlar.Where(t => t.KasaId == k.Id && t.OdemeTuru == OdemeAraci.Kart).Sum(t => t.Toplam));
            var havale = Y(toplamlar.Where(t => t.KasaId == k.Id && t.OdemeTuru == OdemeAraci.Havale).Sum(t => t.Toplam));
            return new Kuyumcu.Contracts.Sayfalar.Kasalar.KasaNakitAyrimiOzeti(
                k.Id, k.ParaBirimi, k.Bakiye, Y(k.Bakiye - kart - havale), kart, havale,
                BugunKapali: kapaliIdler.Contains(k.Id));
        }).ToList();
    }

    /// <summary>
    /// Kapanmış günü yeniden açar — SAHİP yetkisi. Kayıt SİLİNMEZ: kim, ne
    /// zaman, hangi gerekçeyle açtığı aynı satırda saklanır.
    /// </summary>
    public async Task<object> GunAc(string id, string tarih, GunAcmaDto dto, AktifKullanici islem)
    {
        await TenantKasasiBul(id, islem.TenantId);

        // PAKET PATCH-5: gerekçe kuralı (≥ 20 karakter) servis içinde KIRPILMIŞ metne uygulanır.
        // DTO MinLength(20) boşlukları da sayıyordu: "a" + 19 boşluk geçip kayda "a" yazılıyordu.
        var acilisGerekcesi = (dto.Gerekce ?? "").Trim();
        if (acilisGerekcesi.Length < 20)
            throw new BadRequestException("Yeniden açma gerekçesi en az 20 karakter olmalıdır").AlanIle("gerekce");

        if (!DateOnly.TryParse(tarih, CultureInfo.InvariantCulture, DateTimeStyles.None, out var yerelGun))
            throw new BadRequestException("Tarih formatı geçersiz (yyyy-MM-dd bekleniyor)");

        var kapanis = await db.GunKapanislari
            .FirstOrDefaultAsync(g => g.TenantId == islem.TenantId
                                   && g.KasaId == id && g.YerelGun == yerelGun)
            ?? throw new NotFoundException("Bu kasa için o güne ait kapanış kaydı yok");

        if (kapanis.AcilisZamani is not null)
            throw new ConflictException("Bu gün zaten yeniden açılmış");

        kapanis.AcanKullaniciId = islem.Id;
        kapanis.AcilisZamani = DateTime.UtcNow;
        kapanis.AcilisGerekcesi = acilisGerekcesi;
        await db.SaveChangesAsync();

        logger.LogWarning(
            "[{TenantId}] GUN_YENIDEN_ACILDI: kasa={KasaId} gun={Gun} kullanici={KullaniciId}",
            islem.TenantId, id, yerelGun, islem.Id);

        return new { kasaId = id, yerelGun, kilitli = false, gerekce = kapanis.AcilisGerekcesi };
    }

    /// <summary>Kasanın kapanış geçmişi — en yeni önce.</summary>
    public async Task<object> GunKapanisListele(string id, AktifKullanici islem, int limit)
    {
        await TenantKasasiBul(id, islem.TenantId);

        var liste = await db.GunKapanislari.AsNoTracking()
            .Where(g => g.TenantId == islem.TenantId && g.KasaId == id)
            .OrderByDescending(g => g.YerelGun)
            .Take(limit)
            .ToListAsync();

        return liste.Select(g => new
        {
            g.YerelGun, g.SayilanNakit, g.DefterBakiye, g.Fark, g.Aciklama,
            g.KapanisZamani, kilitli = g.AcilisZamani is null,
            g.AcilisZamani, g.AcilisGerekcesi,
        }).ToList();
    }

    public async Task<object> HareketListele(
        string id, AktifKullanici islem,
        DateTime? baslangic, DateTime? bitis, int limit, int offset)
    {
        await TenantKasasiBul(id, islem.TenantId);

        var q = db.KasaHareketleri.AsNoTracking()
            .Where(h => h.KasaId == id && h.TenantId == islem.TenantId);
        if (baslangic is not null) q = q.Where(h => h.Tarih >= baslangic);
        if (bitis is not null) q = q.Where(h => h.Tarih <= bitis);

        var liste = await q.OrderByDescending(h => h.Tarih)
            .Skip(offset).Take(limit).ToListAsync();

        var kasa = await Getir(id, islem);
        return new { kasa, hareketler = liste };
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET P-12 (B-41) — KASA HAREKETİ DÜZELTME (defter tarafı düzenlenebilirlik)
    //
    // SÖZLEŞME (PLAN § 2 P-12 DoD):
    //   • FİNANSAL ALAN DÜZELTİLMEZ: tutar · ödeme türü · komisyon oranı · kasa.
    //     Bunların tek düzeltme yolu STORNO + yeni kayıttır. Bu uç onları
    //     OKUMAZ bile (DTO'da alan yoktur → istemci gönderse de yok sayılmaz,
    //     bağlanacak alan olmadığı için 400 üretmez ama etkisi de olmaz;
    //     denetim.sh STORNO_DISI_FINANSAL_PATCH sayacı bunu mekanik tutar).
    //   • AÇIKLAMA her satırda düzeltilebilir (defter bacağı olsun olmasın):
    //     açıklama bakiyeye, güne, rapora ve eşiğe DOKUNMAZ.
    //   • TARİH yalnız ELLE girilmiş satırda (hareket_id NULL) düzeltilebilir:
    //     defter fişinin bacağının tarihi fişin kendi tarihidir, bacaktan
    //     kaydırılırsa kasa geçmişi defterle çelişir.
    //   • TARİH KAPALI GÜNE TAŞINAMAZ ve KAPALI GÜNDEN ÇIKARILAMAZ: kapanış
    //     tutanağı imzalanmış bir günün nakit toplamı sessizce değişemez
    //     (Mehmet koşulu — storno yolundaki kapalı gün kuralının aynısı).
    //   • DenetimIzi ESKİ → YENİ değerle yazılır (plan DoD md.4).
    //
    // ⚠ PATCH ÜÇ DURUMLUDUR: gövdede yoksa KORU · null ise SİL (açıklama) ·
    //   değer varsa yaz. Tam gövdeyle üzerine yazıp veri imha etmek yasaktır.
    // ═════════════════════════════════════════════════════════════════════════
    public async Task<object> HareketDuzelt(
        string kasaId, string hareketId, KasaHareketDuzeltDto dto, AktifKullanici islem)
    {
        var kasa = await TenantKasasiBul(kasaId, islem.TenantId);

        var h = await db.KasaHareketleri
            .FirstOrDefaultAsync(x => x.Id == hareketId
                                   && x.TenantId == islem.TenantId
                                   && x.KasaId == kasaId)
            ?? throw new NotFoundException("Kasa hareketi bulunamadı");

        if (!dto.AciklamaVerildi && !dto.TarihVerildi)
            throw new BadRequestException(
                "Düzeltilecek alan gönderilmedi. Bu uçta yalnız açıklama ve tarih düzeltilebilir; "
                + "tutar/ödeme türü düzeltmesi yalnız STORNO + yeni kayıt yoluyla yapılır.");

        // Hükümsüz kılınmış kayıt düzeltilmez: storno satırının açıklaması
        // GEREKÇEDİR (denetim izinin okunabilir yüzü), stornolanmış satırınki de
        // artık tarihsel bir gerçektir.
        if (h.StornoKaynakId is not null)
            throw new ConflictException(
                "Bu satır bir STORNO kaydıdır; açıklaması storno gerekçesidir ve düzeltilemez. "
                + "Doğru bilgiyi yeni bir kasa hareketiyle girin.");

        var stornolandi = await db.KasaHareketleri.AsNoTracking()
            .AnyAsync(x => x.TenantId == islem.TenantId && x.StornoKaynakId == h.Id);
        if (stornolandi)
            throw new ConflictException(
                "Bu kasa hareketi stornolanmıştır; hükümsüz bir kaydın bilgileri düzeltilmez.");

        var degisiklikler = new List<string>();

        if (dto.AciklamaVerildi)
        {
            var yeni = string.IsNullOrWhiteSpace(dto.Aciklama) ? null : dto.Aciklama!.Trim();
            KolonTavani.MetinSiniri(yeni, 500, "aciklama", "Açıklama");   // PATCH-5: varchar(500)
            if (yeni != h.Aciklama)
            {
                degisiklikler.Add($"açıklama: '{h.Aciklama ?? "—"}' → '{yeni ?? "—"}'");
                h.Aciklama = yeni;
            }
        }

        if (dto.TarihVerildi)
        {
            var yeniTarih = dto.Tarih
                ?? throw new BadRequestException("Tarih boşaltılamaz; yeni bir tarih gönderin.").AlanIle("tarih");

            if (h.HareketId is not null)
                throw new ConflictException(
                    "Bu kasa hareketi bir defter işleminin bacağıdır; tarihi bacaktan değiştirilemez. "
                    + "Yanlış tarihli bir işlem STORNO edilip doğru tarihle yeniden girilir.");

            if (yeniTarih > DateTime.UtcNow.AddDays(1))
                throw new BadRequestException("Kasa hareketi tarihi gelecekte olamaz.");

            if (yeniTarih != h.Tarih)
            {
                await GunKapaliMi(islem.TenantId, kasa.Id, h.Tarih, "kaynak");
                await GunKapaliMi(islem.TenantId, kasa.Id, yeniTarih, "hedef");
                degisiklikler.Add($"tarih: {h.Tarih:yyyy-MM-dd HH:mm} → {yeniTarih:yyyy-MM-dd HH:mm}");
                h.Tarih = yeniTarih;
            }
        }

        if (degisiklikler.Count > 0)
        {
            denetim?.YazmaKaydet(islem, "KasaHareketiDuzeltme", h.Id,
                "kasa hareketi düzeltildi · " + string.Join(" · ", degisiklikler),
                hassas: true, kasaId: kasa.Id);
            await db.SaveChangesAsync();
        }

        return new
        {
            id         = h.Id,
            kasaId     = h.KasaId,
            aciklama   = h.Aciklama,
            tarih      = h.Tarih,
            tutar      = h.Tutar,
            paraBirimi = h.ParaBirimi,
            degisenAlanSayisi = degisiklikler.Count,
        };
    }

    /// <summary>
    /// Kapanmış günün nakit toplamı sessizce değişmesin: hem KAYNAK hem HEDEF
    /// gün açık olmalıdır. (Storno yolundaki kuralla aynı kaynak: kapanış kaydı
    /// var ve yeniden açılmamışsa gün KAPALIDIR.)
    /// </summary>
    private async Task GunKapaliMi(string tenantId, string kasaId, DateTime an, string taraf)
    {
        var yerelGun = DateOnly.FromDateTime(Zaman.UtcdenYerele(an));
        var kapanis = await db.GunKapanislari.AsNoTracking()
            .FirstOrDefaultAsync(g => g.TenantId == tenantId && g.KasaId == kasaId
                                   && g.YerelGun == yerelGun);
        if (kapanis is not null && kapanis.AcilisZamani is null)
            throw new ConflictException(
                $"{yerelGun:yyyy-MM-dd} günü kapatılmıştır ({taraf} gün); kapalı günün kasa "
                + "hareketleri tarih olarak taşınamaz. Önce günü yeniden açın (SAHİP) ya da "
                + "hareketi STORNO edip doğru tarihle yeniden girin.");
    }

    public async Task<object> DeaktifEt(string id, AktifKullanici islem)
    {
        var kasa = await TenantKasasiBul(id, islem.TenantId);
        if (kasa.Bakiye != 0m)
            throw new BadRequestException("Bakiyesi sıfır olmayan kasa deaktif edilemez");

        // Zaten pasifse tekrar yazmak yerine aynı sonucu döner (idempotent).
        if (kasa.Aktif)
        {
            kasa.Aktif = false;
            kasa.Guncellendi = DateTime.UtcNow;
            denetim?.YazmaKaydet(islem, nameof(Data.Kasa), kasa.Id, "kasa pasife alındı", hassas: false);
            await db.SaveChangesAsync();
        }
        return new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Kasa deaktif edildi"), aktif = false };
    }

    internal async Task<Data.Kasa> TenantKasasiBul(string id, string tenantId) =>
        await db.Kasalar.FirstOrDefaultAsync(k => k.Id == id && k.TenantId == tenantId)
        ?? throw new NotFoundException("Kasa bulunamadı");
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/kasalar")]
[Yetki.ModulYetkisi("kasa")]
public sealed class KasaController(KasaServisi servis, Satis.StornoServisi storno) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/kasalar — Kasa oluştur — MUDUR+</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Olustur([FromBody] KasaOlusturDto dto) => servis.Olustur(dto, Islem);

    /// <summary>GET /api/v1/kasalar?subeId=...</summary>
    [HttpGet]
    public Task<List<Data.Kasa>> Listele([FromQuery] string? subeId) => servis.Listele(Islem, subeId);

    /// <summary>
    /// GET /api/v1/kasalar/nakit-ayrimi?subeId=... — PAKET PARA-DEMO-2 (madde 4). Kasa listesiyle AYNI
    /// erişim (modül yetkisi "kasa", rol süzgeci yok — liste gibi); salt okunur.
    /// </summary>
    [HttpGet("nakit-ayrimi")]
    public Task<List<Kuyumcu.Contracts.Sayfalar.Kasalar.KasaNakitAyrimiOzeti>> NakitAyrimlari([FromQuery] string? subeId)
        => servis.NakitAyrimlari(Islem, subeId);

    /// <summary>GET /api/v1/kasalar/{id}</summary>
    [HttpGet("{id}")]
    public Task<object> Getir(string id) => servis.Getir(id, Islem);

    /// <summary>GET /api/v1/kasalar/{id}/hareketler</summary>
    [HttpGet("{id}/hareketler")]
    public Task<object> HareketListele(
        string id,
        [FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis,
        [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        => servis.HareketListele(id, Islem, baslangic, bitis, limit, offset);

    /// <summary>
    /// POST /api/v1/kasalar/{id}/gun-kapanisi — Gün sonu sayımı ve kapanış.
    /// Fark varsa gerekçe zorunludur; kapanıştan sonra o güne hareket yazılamaz.
    /// </summary>
    [HttpPost("{id}/gun-kapanisi")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> GunKapat(string id, [FromBody] GunKapanisiDto dto)
        => servis.GunKapat(id, dto, Islem);

    /// <summary>GET /api/v1/kasalar/kor-sayim — D1-11 kör sayım ayarı (tezgâh ve kasa ekranı okur).</summary>
    [HttpGet("kor-sayim")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public async Task<object> KorSayim() => new { etkin = await servis.KorSayimEtkin(Islem.TenantId) };

    /// <summary>PUT /api/v1/kasalar/kor-sayim — D1-11 kör sayımı aç / kapat (SAHİP/MÜDÜR).</summary>
    [HttpPut("kor-sayim")]
    [RolGerekli("SAHIP", "MUDUR")]
    public async Task<object> KorSayimYaz([FromBody] KorSayimDto dto) => new { etkin = await servis.KorSayimYaz(dto.Etkin, Islem) };

    /// <summary>
    /// GET /api/v1/kasalar/{id}/gun-kapanisi/onizleme — PAKET PARA-DEMO. Sayılacak nakit
    /// defteri ile sayıma girmeyen kart/havale; kapanışa yetkili rollerle aynı küme.
    /// </summary>
    [HttpGet("{id}/gun-kapanisi/onizleme")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<Kuyumcu.Contracts.Sayfalar.Kasalar.GunKapanisiOnizleme> GunKapanisiOnizleme(string id) => servis.GunKapanisiOnizleme(id, Islem);

    /// <summary>
    /// POST /api/v1/kasalar/{id}/gun-kapanisi/{tarih}/ac — Kapanmış günü yeniden aç.
    /// YALNIZ SAHİP: kapanışın altını açmak sayım tutanağını geçersiz kılar.
    /// </summary>
    [HttpPost("{id}/gun-kapanisi/{tarih}/ac")]
    [RolGerekli("SAHIP")]
    public Task<object> GunAc(string id, string tarih, [FromBody] GunAcmaDto dto)
        => servis.GunAc(id, tarih, dto, Islem);

    /// <summary>GET /api/v1/kasalar/{id}/gun-kapanislari — Kapanış geçmişi</summary>
    [HttpGet("{id}/gun-kapanislari")]
    public Task<object> GunKapanisListele(string id, [FromQuery, Range(1, 200)] int limit = 30)
        => servis.GunKapanisListele(id, Islem, limit);

    /// <summary>POST /api/v1/kasalar/{id}/hareket — MUHASEBE+</summary>
    [HttpPost("{id}/hareket")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> HareketGir(string id, [FromBody] KasaHareketDto dto)
        => servis.HareketGir(id, dto, Islem);

    /// <summary>PATCH /api/v1/kasalar/{id} — Kasa bilgilerini güncelle (ad · para birimi · şube · aktif) — MUDUR+</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Guncelle(string id, [FromBody] KasaGuncelleDto dto)
        => servis.Guncelle(id, dto, Islem);

    /// <summary>
    /// POST /api/v1/kasalar/{id}/hareket/{hareketId}/storno — PAKET S-3b.
    /// ELLE girilmiş kasa hareketini gerekçeli ters kayıtla hükümsüz kılar.
    /// İş mantığı bu dosyaya YAZILMAZ: tek storno motoru (StornoServisi).
    /// </summary>
    [HttpPost("{id}/hareket/{hareketId}/storno")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> HareketStorno(
        string id, string hareketId, [FromBody] GerekceDto dto)
        => storno.KasaHareketiStorno(id, hareketId, dto.Gerekce, Islem);

    /// <summary>
    /// GET /api/v1/kasalar/{id}/degisim-kilidi — PAKET Q9-5. Şube/para birimi
    /// değişebilir mi, değişemiyorsa NEDEN (BAKIYE · HAREKET · GUN_KAPANISI).
    /// Okuma ucu: modül yetkisi yeter (liste ve detay gibi).
    /// </summary>
    [HttpGet("{id}/degisim-kilidi")]
    public Task<object> DegisimKilidi(string id) => servis.DegisimKilidi(id, Islem);

    /// <summary>DELETE /api/v1/kasalar/{id} — SAHIP</summary>
    /// <summary>
    /// PATCH /api/v1/kasalar/{id}/hareket/{hareketId} — PAKET P-12 (B-41).
    /// Kasa hareketinin AÇIKLAMA ve (yalnız elle girilmiş satırda) TARİH
    /// alanını düzeltir. Finansal alan bu yoldan DEĞİŞMEZ.
    /// </summary>
    [HttpPatch("{id}/hareket/{hareketId}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> HareketDuzelt(
        string id, string hareketId, [FromBody] KasaHareketDuzeltDto dto)
        => servis.HareketDuzelt(id, hareketId, dto, Islem);

    [HttpDelete("{id}")]
    [RolGerekli("SAHIP")]
    public Task<object> DeaktifEt(string id) => servis.DeaktifEt(id, Islem);
}
