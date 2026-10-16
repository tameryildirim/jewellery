using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Masak;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-3 (§ 4.4) — YAPTIRIM (MALVARLIĞI DONDURMA) TARAMASI.
//
// Dayanak (birincil, şartname § 3.1 M-9…M-12): 6415 s. K. md.10 (karara aykırı
// işlem HÜKÜMSÜZ), md.12/3 (alacak/borç 30 gün içinde bildirilir); MASAK Genel
// Tebliği Sıra No 34 md.5 (listedeki kişinin istediği işlem ivedilikle MASAK'a
// bildirilir ve ERTELENİR).
//
// TARAMA NOKTALARI (fail-closed):
//   1. MASAK kimlik tespiti (Faz 1, DogrulaKumulatif): kimlik no TAM eşleşme → 400.
//   2. Cari oluşturma/güncelleme (vergi/kimlik no): TAM → kayıt açılır, cari
//      KİLİTLENİR (yaptirim_eslesmeleri satırı; carilere alan eklenmez).
//   3. Hurda alış satıcısı (TCKN): TAM → 400.
//   4. Liste güncellemesi → mevcut carilerle TAM eşleşme: kilit + açık bakiye /
//      emanet / bekleyen sipariş varsa KRİTİK görev (son gün = RG + 30).
//   5. Ad benzerliği (kimlik no yok/farklı): ENGEL DEĞİL — işlem geçer, SAHİP'e
//      "olası eşleşme" bildirimi. Yanlış pozitifle tezgâh kilitlenmez.
// ⚠ Kilit yalnız listeden karar KALDIRILINCA açılır (kaldirildi damgası).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>POST /sistem/yaptirim-listesi gövdesi — ByGatech merkezî günceller.</summary>
public sealed class YaptirimListesiDto
{
    [Required, MaxLength(150)] public string? Kaynak { get; set; }
    [Required, MaxLength(500)] public string? KaynakUrl { get; set; }
    /// <summary>Resmî listenin güncel olduğu gün.</summary>
    [Required] public DateOnly? ListeTarihi { get; set; }
    public List<YaptirimSatiriDto>? Ekle { get; set; }
    /// <summary>Kaldırılan kararların satır kimlikleri (karar kaldırıldı).</summary>
    public List<string>? Kaldir { get; set; }
}

public sealed class YaptirimSatiriDto
{
    [MaxLength(100)] public string? KararNo { get; set; }
    public DateOnly? RgTarihi { get; set; }
    [Required, MaxLength(300)] public string? AdSoyad { get; set; }
    public DateOnly? DogumTarihi { get; set; }
    [MaxLength(3)] public string? Uyruk { get; set; }
    [MaxLength(50)] public string? KimlikNo { get; set; }
}

/// <summary>PATCH /masak/yaptirim-eslesmeleri/{id} — gövdede yoksa KORU, null ise SİL.</summary>
public sealed class YaptirimEslesmePatchDto
{
    private string? _durum; private DateOnly? _tarih; private string? _evrak; private string? _not;
    public string? Durum { get => _durum; set { _durum = value; DurumVerildi = true; } }
    [System.Text.Json.Serialization.JsonIgnore] public bool DurumVerildi { get; private set; }
    public DateOnly? MasakBildirimTarihi { get => _tarih; set { _tarih = value; TarihVerildi = true; } }
    [System.Text.Json.Serialization.JsonIgnore] public bool TarihVerildi { get; private set; }
    public string? MasakEvrakNo { get => _evrak; set { _evrak = value; EvrakVerildi = true; } }
    [System.Text.Json.Serialization.JsonIgnore] public bool EvrakVerildi { get; private set; }
    public string? Not { get => _not; set { _not = value; NotVerildi = true; } }
    [System.Text.Json.Serialization.JsonIgnore] public bool NotVerildi { get; private set; }
}

public static class YaptirimServisi
{
    /// <summary>Tezgâhın gördüğü tek cümle (kod yok, ne yapmalı söylenir).</summary>
    public const string EngelMesaji = "Bu kişiyle işlem yapılamaz (yasal liste). Yöneticinize haber verin.";

    /// <summary>Hata gövdesindeki makine kodu — ekran kırmızı bandı ve Kaydet kilidini bununla açar.</summary>
    public const string EngelKodu = "YAPTIRIM_ENGEL";

    /// <summary>Denetim izindeki varlık türü.</summary>
    public const string DenetimTuru = "YAPTIRIM_ENGEL";

    /// <summary>Listenin "eski" sayıldığı gün sayısı (SAHİP'e sarı bant).</summary>
    public const int EskiListeGun = 14;

    /// <summary>Bildirim türü (bildirimler.tur ≤ 40).</summary>
    public const string BildirimTuru = "YAPTIRIM";

    private const string Roller = "SAHIP";

    /// <summary>Tek sorguda okunan en çok satır (eşleşme/cari) — B-8 sınırlı döküm.</summary>
    private const int AzamiSorgu = 1000;

    /// <summary>
    /// Bir güncellemede işlenen en çok liste satırı. Resmî dondurma listeleri birkaç bin
    /// satırdır; sınır aşılırsa güncelleme parçalı gönderilir (ByGatech operatörü).
    /// </summary>
    public const int AzamiListeSatiri = 50_000;

    /// <summary>Eşik bölme sinyali penceresi (gün) — politika, mevzuat değil (T-4).</summary>
    public const int SinyalGun = 30;
    /// <summary>Uyum ekranında listelenen son sinyal sayısı (sınır ekranda yazılır — UX-S29).</summary>
    public const int SinyalListe = 20;
    private const string Rota = "/uyum";

    // ── PAKET YASAL-LISTE-DEMO — liste YALNIZ birincil kaynaktan yüklenir ──
    // Kök neden (02.10): yaptirim_kayitlari kiracı-üstüdür ve tek yazma yolu
    // POST /sistem/yaptirim-listesi'dir; geliştirme/demo veritabanında bu uç hiç
    // çağrılmadığı için tablo BOŞTUR → Ozet "hiç yüklenmedi" (fail-closed) der.
    // Bant DOĞRU uyarıdır; çözüm listeyi birincil kaynaktan yüklemektir. Uydurma/
    // örnek liste sokulmasın diye kaynak adresi yalnız resmî yayın konaklarından
    // kabul edilir (MASAK — 6415 md.5/6/7 listeleri; Resmî Gazete — karar yayımı;
    // BM Güvenlik Konseyi konsolide listesi — 6415 md.5'in dayandığı liste).

    /// <summary>Kaynak adresinin konağı bunlardan biri olmalı (tam eşleşme, küçük harf).</summary>
    public static readonly IReadOnlyList<string> BirincilKaynakKonaklari =
    [
        "masak.hmb.gov.tr", "ms.hmb.gov.tr",
        "www.resmigazete.gov.tr", "resmigazete.gov.tr",
        "scsanctions.un.org", "main.un.org", "www.un.org",
    ];

    /// <summary>https + birincil konak mı (saf; demo betiği aynı listeyi uygular).</summary>
    public static bool BirincilKaynakMi(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var u)
        && u.Scheme == Uri.UriSchemeHttps
        && BirincilKaynakKonaklari.Contains(u.Host.ToLowerInvariant());

    /// <summary>
    /// Listenin yaşı ve eskiliği (saf). Hiç yüklenmemişse (null) ESKİ sayılır —
    /// fail-closed: yanlış güven verilmez. Liste kiracı-üstüdür; yeni kiracı da
    /// sistemde yüklü son listeyi görür (kiracıya bağlı "son yükleme" kaydı yoktur).
    /// </summary>
    public static (int? Gun, bool Eski) ListeDurumu(DateOnly? sonListeTarihi, DateOnly bugun)
    {
        if (sonListeTarihi is not { } t) return (null, true);
        var gun = bugun.DayNumber - t.DayNumber;
        return (gun, gun > EskiListeGun);
    }

    /// <summary>
    /// Uyum özetinin "bugün"ü için DI dikişi. Üretimde KAYITLI DEĞİLDİR → sistem işlem günü.
    /// Yalnız testler kaydeder: liste tablosu kiracı-üstü ve test veritabanında paylaşıldığı
    /// için eskilik bayrağının (fail-closed) /masak/yaptirim ve /masak/uyum-ozet uçlarına
    /// taşındığı ileri tarihli günle sınanır (PAKET YASAL-LISTE-DEMO tur 2).
    /// </summary>
    public sealed record YaptirimOzetGunu(DateOnly Bugun);

    // ── Normalizasyon ───────────────────────────────────────────────────────

    /// <summary>Ad-soyad: büyük harf, Türkçe harf katlanmış, harf/rakam dışı boşluk, tek boşluk.</summary>
    public static string AdNormalize(string? ad)
    {
        if (string.IsNullOrWhiteSpace(ad)) return "";
        var b = new StringBuilder(ad.Length);
        foreach (var c0 in ad.Trim().ToUpper(new CultureInfo("tr-TR")))
        {
            var c = c0 switch
            {
                'Ç' => 'C', 'Ğ' => 'G', 'İ' => 'I', 'I' => 'I', 'Ö' => 'O', 'Ş' => 'S', 'Ü' => 'U',
                'Â' => 'A', 'Î' => 'I', 'Û' => 'U',
                _ => c0,
            };
            b.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }
        return string.Join(' ', b.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Kimlik no normalize (AlanSifreleyici.Normalize ile AYNI — hash tutarlı).</summary>
    private static string KimlikNormalize(string s) => AesGcmAlanSifreleyici.Normalize(s);

    // ── Okuma yardımcıları ──────────────────────────────────────────────────

    private static IQueryable<YaptirimKaydi> Yururlukte(KuyumcuDbContext db) =>
        db.YaptirimKayitlari.AsNoTracking().Where(y => y.Kaldirildi == null);

    /// <summary>Bu cari yasal liste nedeniyle KİLİTLİ mi (TAM eşleşme, karar yürürlükte).</summary>
    public static async Task<bool> CariKilitliMi(KuyumcuDbContext db, string tenantId, string cariId)
    {
        var kayitIdleri = await db.YaptirimEslesmeleri.AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.CariId == cariId && e.Tur == YaptirimEslesmeTuru.Tam)
            .Select(e => e.YaptirimKaydiId).Take(AzamiSorgu).ToListAsync();
        if (kayitIdleri.Count == 0) return false;
        return await Yururlukte(db).AnyAsync(y => kayitIdleri.Contains(y.Id));
    }

    /// <summary>Kilitli cariyle her işlem 400 (tarama noktası 2'nin sonucu).</summary>
    public static async Task CariKilidiDenetle(KuyumcuDbContext db, string tenantId, string? cariId)
    {
        if (string.IsNullOrEmpty(cariId)) return;
        if (await CariKilitliMi(db, tenantId, cariId))
            throw new BadRequestException(EngelMesaji).AlanIle("cariId").KodIle(EngelKodu);
    }

    /// <summary>Kimlik no için yürürlükteki TAM eşleşme (yoksa null).</summary>
    public static async Task<YaptirimKaydi?> TamEslesme(KuyumcuDbContext db, string? kimlikNo)
    {
        if (string.IsNullOrWhiteSpace(kimlikNo)) return null;
        var hash = db.Sifreleyici.Hashle(KimlikNormalize(kimlikNo));
        return await Yururlukte(db).FirstOrDefaultAsync(y => y.KimlikNoHash == hash);
    }

    /// <summary>Normalize ad-soyad eşitliği (kimlik no yok ya da farklı) — yalnız sinyal.</summary>
    public static async Task<YaptirimKaydi?> AdBenzerligi(KuyumcuDbContext db, string? adSoyad)
    {
        var n = AdNormalize(adSoyad);
        if (n.Length < 5) return null;   // tek kelimelik/boş ad sinyal üretmez
        return await Yururlukte(db).FirstOrDefaultAsync(y => y.AdSoyad == n);
    }

    // ── Tarama noktaları 1 · 3 · 5 ─────────────────────────────────────────

    /// <summary>
    /// Kişinin kimliği işleme girerken çağrılır (MASAK kimlik tespiti, hurda satıcısı).
    /// TAM → eşleşme + KRİTİK bildirim + YAPTIRIM_ENGEL denetimi (kendi SaveChanges'i:
    /// reddedilen işlemle birlikte kaybolmasın) → 400. Ad benzerliği → eşleşme +
    /// UYARI bildirimi, işlem GEÇER. Faz 1'de (transaction AÇILMADAN) çağrılır.
    /// </summary>
    public static async Task KimlikTara(
        KuyumcuDbContext db, string tenantId, AktifKullanici? islem, string nokta,
        string? kimlikNo, string? adSoyad, string? cariId)
    {
        var tam = await TamEslesme(db, kimlikNo);
        var simdi = DateTime.UtcNow;
        if (tam is not null)
        {
            var e = EslesmeEkle(db, tenantId, tam, YaptirimEslesmeTuru.Tam, nokta, cariId, islem?.Id, simdi,
                // M-12: "ivedilikle" — son gün bugündür (uydurulmuş bir süre YAZILMAZ).
                YasalParametreServisi.IslemGunu(simdi));
            BildirimEkle(db, tenantId, BildirimOnem.Kritik,
                "Yasal liste eşleşmesi — işlem durduruldu",
                "Listedeki bir kişiyle işlem denendi ve durduruldu. MASAK'a ivedilikle bildirin; bildirdikten sonra Uyum ekranında kapatın.",
                e.Id, $"YAPTIRIM:{e.Id}");
            var detay = $"{DenetimTuru}: nokta {nokta} · liste satırı {tam.Id} · eşleşme {e.Id}"
                        + (cariId is null ? "" : $" · cari {cariId}");
            var denetim = new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor());
            if (islem is not null)
                await denetim.RedKaydet(islem, DenetimTuru, e.Id, detay);   // SaveChanges içerir
            else
            {
                denetim.SistemYazmaKaydet(tenantId, DenetimTuru, e.Id, detay);
                await db.SaveChangesAsync();
            }
            throw new BadRequestException(EngelMesaji)
                .AlanIle(nokta == YaptirimNoktasi.HurdaSatici ? "satici" : "cariId").KodIle(EngelKodu);
        }

        var benzer = await AdBenzerligi(db, adSoyad);
        if (benzer is null) return;
        // Aynı cari/kayıt için açık ad eşleşmesi varsa ikinci satır yazılmaz.
        var varMi = await db.YaptirimEslesmeleri.AsNoTracking().AnyAsync(x =>
            x.TenantId == tenantId && x.YaptirimKaydiId == benzer.Id && x.Tur == YaptirimEslesmeTuru.AdBenzerligi
            && x.Durum == YaptirimEslesmeDurumu.Acik && x.CariId == cariId);
        if (varMi) return;
        var ab = EslesmeEkle(db, tenantId, benzer, YaptirimEslesmeTuru.AdBenzerligi, nokta, cariId, islem?.Id, simdi, null);
        BildirimEkle(db, tenantId, BildirimOnem.Uyari,
            "Olası yasal liste eşleşmesi",
            "Bir müşterinin adı yasal listedeki bir adla aynı. İşlem durdurulmadı; kimlik bilgilerini karşılaştırıp Uyum ekranında sonucu işaretleyin.",
            ab.Id, $"YAPTIRIM:{ab.Id}");
        await db.SaveChangesAsync();
    }

    // ── Tarama noktası 2 — cari ────────────────────────────────────────────

    /// <summary>
    /// Cari yazıldıktan SONRA çağrılır (kayıt açılır). Vergi/kimlik no TAM eşleşirse
    /// cari KİLİTLENİR ve SAHİP'e KRİTİK bildirim gider; ad benzerliği → UYARI.
    /// Kendi SaveChanges'ini çağırır (cari zaten kaydedildi). Döner: kilitlendi mi.
    /// </summary>
    public static async Task<bool> CariTara(
        KuyumcuDbContext db, string tenantId, string cariId, string? kimlikNo, string? ad, AktifKullanici islem)
    {
        var simdi = DateTime.UtcNow;
        var tam = await TamEslesme(db, kimlikNo);
        if (tam is not null)
        {
            var mevcut = await db.YaptirimEslesmeleri.AsNoTracking().AnyAsync(x =>
                x.TenantId == tenantId && x.CariId == cariId && x.YaptirimKaydiId == tam.Id && x.Tur == YaptirimEslesmeTuru.Tam);
            if (mevcut) return true;
            var e = EslesmeEkle(db, tenantId, tam, YaptirimEslesmeTuru.Tam, YaptirimNoktasi.Cari, cariId, islem.Id, simdi,
                YasalParametreServisi.IslemGunu(simdi));
            BildirimEkle(db, tenantId, BildirimOnem.Kritik,
                "Yasal liste eşleşmesi — müşteri kilitlendi",
                "Yeni kaydedilen müşteri yasal listede. Bu müşteriyle işlem yapılamaz; MASAK'a ivedilikle bildirin ve Uyum ekranında kapatın.",
                e.Id, $"YAPTIRIM:{e.Id}");
            new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor())
                .YazmaKaydet(islem, DenetimTuru, e.Id, $"{DenetimTuru}: nokta {YaptirimNoktasi.Cari} · cari {cariId} kilitlendi · liste satırı {tam.Id}");
            await db.SaveChangesAsync();
            return true;
        }
        var benzer = await AdBenzerligi(db, ad);
        if (benzer is not null)
        {
            var varMi = await db.YaptirimEslesmeleri.AsNoTracking().AnyAsync(x =>
                x.TenantId == tenantId && x.CariId == cariId && x.YaptirimKaydiId == benzer.Id);
            if (!varMi)
            {
                var ab = EslesmeEkle(db, tenantId, benzer, YaptirimEslesmeTuru.AdBenzerligi, YaptirimNoktasi.Cari, cariId, islem.Id, simdi, null);
                BildirimEkle(db, tenantId, BildirimOnem.Uyari, "Olası yasal liste eşleşmesi",
                    "Bir müşterinin adı yasal listedeki bir adla aynı. Kimlik bilgilerini karşılaştırıp Uyum ekranında sonucu işaretleyin.",
                    ab.Id, $"YAPTIRIM:{ab.Id}");
                await db.SaveChangesAsync();
            }
        }
        return false;
    }

    // ── Tarama noktası 4 — liste güncellemesi (sistem anahtarı) ─────────────

    /// <summary>
    /// ByGatech'in sistem ucundan listeyi günceller: yeni satırlar eklenir, kaldırılan
    /// kararlar damgalanır (satır silinmez). Yeni satır mevcut bir cariyle TAM
    /// eşleşirse cari kilitlenir; açık bakiye/emanet/bekleyen sipariş varsa SAHİP'e
    /// KRİTİK görev: "30 gün içinde MASAK'a bildirin — son gün GG.AA" (6415 md.12/3).
    /// <paramref name="sistemDb"/> SistemKapsami.Sistem() bağlamıdır (kiracı-üstü).
    /// </summary>
    public static async Task<object> ListeGuncelle(
        KuyumcuDbContext sistemDb, DenetimServisi denetim, YaptirimListesiDto dto, string operatorAdi)
    {
        var simdi = DateTime.UtcNow;
        var url = dto.KaynakUrl!.Trim();
        if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("Kaynak adresi https:// ile başlamalı (resmî liste sayfası).");
        if (!BirincilKaynakMi(url))
            throw new BadRequestException("Kaynak adresi birincil kaynak olmalı (MASAK, Resmî Gazete ya da BM Güvenlik Konseyi listesi).");
        var ekle = dto.Ekle ?? [];
        if (ekle.Count + (dto.Kaldir?.Count ?? 0) > AzamiListeSatiri)
            throw new BadRequestException($"Tek güncellemede en çok {AzamiListeSatiri} satır; listeyi parçalara bölün.");
        foreach (var (s, i) in ekle.Select((s, i) => (s, i)))
            if (AdNormalize(s.AdSoyad).Length == 0)
                throw new BadRequestException($"{i + 1}. satırda ad-soyad boş.");

        var yeni = new List<YaptirimKaydi>();
        foreach (var s in ekle)
        {
            var kimlik = string.IsNullOrWhiteSpace(s.KimlikNo) ? null : KimlikNormalize(s.KimlikNo);
            var k = new YaptirimKaydi
            {
                Id = Kimlik.Ulid(),
                Kaynak = dto.Kaynak!.Trim(),
                KaynakUrl = url,
                KararNo = string.IsNullOrWhiteSpace(s.KararNo) ? null : s.KararNo.Trim(),
                RgTarihi = s.RgTarihi,
                ListeTarihi = dto.ListeTarihi!.Value,
                AdSoyad = AdNormalize(s.AdSoyad),
                DogumTarihi = s.DogumTarihi,
                Uyruk = string.IsNullOrWhiteSpace(s.Uyruk) ? null : s.Uyruk.Trim().ToUpperInvariant(),
                KimlikNo = kimlik,
                KimlikNoHash = kimlik is null ? null : sistemDb.Sifreleyici.Hashle(kimlik),
                SifreSurumu = sistemDb.Sifreleyici.Surum,
                Eklendi = simdi,
                Olusturan = Kirp("sistem:" + operatorAdi.Trim(), 60),
            };
            sistemDb.YaptirimKayitlari.Add(k);
            yeni.Add(k);
        }

        var kaldirilan = 0;
        var kaldirIdler = dto.Kaldir ?? [];
        if (kaldirIdler.Count > 0)
        {
            var satirlar = await sistemDb.YaptirimKayitlari
                .Where(y => kaldirIdler.Contains(y.Id) && y.Kaldirildi == null).Take(AzamiListeSatiri).ToListAsync();
            foreach (var y in satirlar) { y.Kaldirildi = simdi; kaldirilan++; }
        }

        // Liste TARİHİ yeni satır olmasa da ilerlemeli ("liste güncel") → mevcut
        // yürürlükteki satırların liste tarihi, verilen tarih daha yeniyse ilerletilir.
        var ilerle = await sistemDb.YaptirimKayitlari
            .Where(y => y.Kaldirildi == null && y.ListeTarihi < dto.ListeTarihi!.Value).Take(AzamiListeSatiri).ToListAsync();
        foreach (var y in ilerle) y.ListeTarihi = dto.ListeTarihi!.Value;

        denetim.SistemYazmaKaydet(Modules.Sistem.SistemServisi.SistemTenantId, "YaptirimListesi", null,
            $"yaptırım listesi güncellendi · eklenen {yeni.Count} · kaldırılan {kaldirilan} · liste tarihi {dto.ListeTarihi:yyyy-MM-dd} · operatör: {operatorAdi.Trim()}");
        await sistemDb.SaveChangesAsync();

        // Mevcut carilerle eşleştirme (tüm kiracılar). Cari vergi/kimlik no düz metin durur.
        var gorev = 0; var kilit = 0;
        foreach (var k in yeni.Where(k => k.KimlikNo is not null))
        {
            var cariler = await sistemDb.Cariler.AsNoTracking()
                .Where(c => c.VergiNo == k.KimlikNo && !c.SistemCarisi)
                .Select(c => new { c.Id, c.TenantId }).Take(AzamiSorgu).ToListAsync();
            foreach (var c in cariler)
            {
                // Açık varlık ölçüsü TEK YERDEN: CariVarlikVar (bakiye satırının TÜM birim
                // kolonları + maden başına emanet + bekleyen sipariş). Sabit para birimi listesi YOK.
                var acik = await CariVarlikVar(sistemDb, c.TenantId, c.Id);
                // M-10: son gün = kararın RG yayımı + 30 gün. RG tarihi yoksa liste
                // tarihi esas alınır (daha ERKEN ya da eşit → tutucu yön).
                var sonGun = (k.RgTarihi ?? k.ListeTarihi).AddDays(30);
                var e = EslesmeEkle(sistemDb, c.TenantId, k, YaptirimEslesmeTuru.Tam, YaptirimNoktasi.ListeGuncelleme,
                    c.Id, null, simdi, acik ? sonGun : null);
                if (!acik) e.Durum = YaptirimEslesmeDurumu.VarlikYok;   // bildirilecek alacak/borç/emanet yok — görev açılmaz (BİLDİRİLDİ DEĞİL)
                kilit++;
                if (acik)
                {
                    BildirimEkle(sistemDb, c.TenantId, BildirimOnem.Kritik,
                        "Yasal liste — müşteri bilgisini MASAK'a bildirin",
                        string.Format(CultureInfo.GetCultureInfo("tr-TR"),
                            "Bu müşteriyle ilgili alacak/borç/emanet bilgisini 30 gün içinde MASAK'a bildirin — son gün {0:dd.MM.yyyy}. Müşteri kilitlendi.",
                            sonGun.ToDateTime(TimeOnly.MinValue)),
                        e.Id, $"YAPTIRIM:{e.Id}");
                    gorev++;
                }
                denetim.SistemYazmaKaydet(c.TenantId, DenetimTuru, e.Id,
                    $"{DenetimTuru}: nokta {YaptirimNoktasi.ListeGuncelleme} · cari {c.Id} kilitlendi · liste satırı {k.Id} · görev {(acik ? "açıldı" : "yok (açık bakiye/emanet/sipariş yok)")}");
            }
        }
        if (kilit > 0) await sistemDb.SaveChangesAsync();

        return new { eklenen = yeni.Count, kaldirilan, kilitlenenCari = kilit, acilanGorev = gorev, listeTarihi = dto.ListeTarihi };
    }

    // ── Kiracı ekranı: özet + eşleşmeler + kapatma ─────────────────────────

    /// <summary>Uyum/Panel özeti: liste tarihi ve kaynağı, eskilik, açık eşleşmeler.</summary>
    /// <param name="bugun">Yalnız testler verir (bkz. <see cref="YaptirimOzetGunu"/>); null → sistem işlem günü.</param>
    public static async Task<object> Ozet(KuyumcuDbContext db, string tenantId, DateOnly? bugun = null)
    {
        var son = await db.YaptirimKayitlari.AsNoTracking()
            .OrderByDescending(y => y.ListeTarihi).ThenByDescending(y => y.Eklendi)
            .Select(y => new { y.ListeTarihi, y.Kaynak, y.KaynakUrl }).FirstOrDefaultAsync();
        var islemGunu = bugun ?? YasalParametreServisi.IslemGunu(DateTime.UtcNow);
        var yururlukte = await db.YaptirimKayitlari.AsNoTracking().CountAsync(y => y.Kaldirildi == null);
        var eslesmeler = await db.YaptirimEslesmeleri.AsNoTracking()
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.Olusturuldu).Take(100)
            .Select(e => new
            {
                e.Id, e.Tur, e.Nokta, e.CariId, e.Durum, e.BildirimSonGunu,
                e.MasakBildirimTarihi, e.MasakEvrakNo, not_ = e.Not, e.Olusturuldu,
            }).ToListAsync();
        var cariAdlari = await db.Cariler.AsNoTracking()
            .Where(c => c.TenantId == tenantId && eslesmeler.Select(e => e.CariId).Contains(c.Id))
            .Select(c => new { c.Id, c.Ad }).Take(AzamiSorgu).ToDictionaryAsync(c => c.Id, c => c.Ad);
        var (gun, eski) = ListeDurumu(son?.ListeTarihi, islemGunu);

        // PAKET YASAL-3 tur 2 (§ 4.2 "risk listesinde eşik bölme sinyali satırı"): son
        // SinyalGun günde tezgâhtarın art arda Peşin satışta "başka biri" beyanı
        // (denetim PESIN_ART_ARDA_FARKLI_KISI). Kişisel veri yok: tarih + kullanıcı adı.
        var sinyalAlt = DateTime.UtcNow.AddDays(-SinyalGun);
        var sinyalQ = db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.VarlikTuru == PesinPencereKapisi.DenetimTuru && d.Tarih >= sinyalAlt);
        var sinyalAdet = await sinyalQ.CountAsync();
        var sinyalSon = await sinyalQ.OrderByDescending(d => d.Tarih).Take(SinyalListe)
            .Select(d => new { d.Tarih, d.KullaniciId }).ToListAsync();
        var sinyalKullanicilar = sinyalSon.Where(x => x.KullaniciId != null).Select(x => x.KullaniciId!).Distinct().ToList();
        var kullaniciAdlari = sinyalKullanicilar.Count == 0 ? new Dictionary<string, string>()
            : await db.Kullanicilar.AsNoTracking().Where(u => sinyalKullanicilar.Contains(u.Id))
                .Select(u => new { u.Id, u.Ad }).Take(SinyalListe).ToDictionaryAsync(u => u.Id, u => u.Ad);
        return new
        {
            esikBolmeSinyali = new
            {
                gun = SinyalGun,
                adet = sinyalAdet,
                sinir = SinyalListe,
                son = sinyalSon.Select(x => new
                {
                    tarih = x.Tarih,
                    kullanici = x.KullaniciId is { } ki && kullaniciAdlari.TryGetValue(ki, out var ka) ? ka : null,
                }).ToList(),
            },
            listeTarihi = son?.ListeTarihi,
            kaynak = son?.Kaynak,
            kaynakUrl = son?.KaynakUrl,
            listeYasiGun = gun,
            // Liste hiç yüklenmediyse de "eski" sayılır (fail-closed: yanlış güven verilmez).
            listeEski = eski,
            eskiListeGun = EskiListeGun,
            yururlukteSatir = yururlukte,
            acikTam = eslesmeler.Count(e => e.Tur == YaptirimEslesmeTuru.Tam && e.Durum == YaptirimEslesmeDurumu.Acik),
            acikBenzerlik = eslesmeler.Count(e => e.Tur == YaptirimEslesmeTuru.AdBenzerligi && e.Durum == YaptirimEslesmeDurumu.Acik),
            eslesmeler = eslesmeler.Select(e => new
            {
                id = e.Id, tur = e.Tur, nokta = e.Nokta, cariId = e.CariId,
                cariAd = e.CariId is { } ci && cariAdlari.TryGetValue(ci, out var ad) ? ad : null,
                durum = e.Durum, bildirimSonGunu = e.BildirimSonGunu,
                masakBildirimTarihi = e.MasakBildirimTarihi, masakEvrakNo = e.MasakEvrakNo, not = e.not_,
                olusturuldu = e.Olusturuldu,
            }).ToList(),
        };
    }

    /// <summary>
    /// PATCH — MASAK'a bildirim kapatması (tarih + evrak no) ya da ad benzerliğinde
    /// "aynı kişi değil". TAM eşleşme "aynı kişi değil" yapılamaz (kimlik birebir aynı).
    /// Kilit bu uçla AÇILMAZ (karar kaldırılınca açılır).
    /// </summary>
    public static async Task<object> EslesmeGuncelle(
        KuyumcuDbContext db, string id, YaptirimEslesmePatchDto dto, AktifKullanici islem)
    {
        var e = await db.YaptirimEslesmeleri.FirstOrDefaultAsync(x => x.TenantId == islem.TenantId && x.Id == id)
            ?? throw new NotFoundException("Eşleşme kaydı bulunamadı.");
        var alanlar = new List<string>();
        if (dto.DurumVerildi)
        {
            var d = dto.Durum?.Trim().ToUpperInvariant();
            if (d is null || !YaptirimEslesmeDurumu.Hepsi.Contains(d))
                throw new BadRequestException("Durum geçersiz — Açık, MASAK'a bildirildi ya da Aynı kişi değil seçin.").AlanIle("durum");
            if (d == YaptirimEslesmeDurumu.AyniKisiDegil && e.Tur == YaptirimEslesmeTuru.Tam)
                throw new BadRequestException("Kimlik numarası birebir aynı olan eşleşme 'aynı kişi değil' işaretlenemez.").AlanIle("durum");
            e.Durum = d; alanlar.Add("durum");
        }
        if (dto.TarihVerildi)
        {
            // PAKET GOC-B (envanter C satır 61, K8a): MASAK bildirim tarihi yasal kanıt kaydıdır — ileri tarih
            // YOK (bugün dâhil), yıl 1900..2100 penceresi. Ekran (#ylp-tarih-gir EnGec=bugün) ile AYNI Core kuralı
            // ve sözlük metni. Geri pencere bilinçli olarak AÇIK: geç kaydedilen eski bildirim reddedilmez.
            Kuyumcu.Api.Common.Dogrulama.GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.TarihPenceresi.Denetle(
                dto.MasakBildirimTarihi, Kuyumcu.Core.Domain.Girdi.TarihTuru.Serbest, Zaman.YerelBugun(),
                enGec: Zaman.YerelBugun()), "masakBildirimTarihi");
            e.MasakBildirimTarihi = dto.MasakBildirimTarihi; alanlar.Add("masakBildirimTarihi");
        }
        if (dto.EvrakVerildi)
        {
            var ev = dto.MasakEvrakNo?.Trim();
            if (ev is { Length: > Kuyumcu.Core.Domain.Girdi.AlanSinirlari.YaptirimEvrakNo }) throw new BadRequestException("Evrak numarası en çok 100 karakter.").AlanIle("masakEvrakNo");
            e.MasakEvrakNo = string.IsNullOrEmpty(ev) ? null : ev; alanlar.Add("masakEvrakNo");
        }
        if (dto.NotVerildi)
        {
            var n = dto.Not?.Trim();
            if (n is { Length: > Kuyumcu.Core.Domain.Girdi.AlanSinirlari.YaptirimNot }) throw new BadRequestException("Not en çok 500 karakter.").AlanIle("not");
            e.Not = string.IsNullOrEmpty(n) ? null : n; alanlar.Add("not");
        }
        // "Bildirildi" ancak tarih ve evrak no ile kapanır (kanıtsız kapatma yok).
        if (e.Durum == YaptirimEslesmeDurumu.Bildirildi
            && (e.MasakBildirimTarihi is null || string.IsNullOrEmpty(e.MasakEvrakNo)))
            throw new BadRequestException("MASAK'a bildirim tarihini ve evrak numarasını yazın.")
                .AlanIle(e.MasakBildirimTarihi is null ? "masakBildirimTarihi" : "masakEvrakNo");
        if (alanlar.Count == 0) throw new BadRequestException("Değiştirilecek alan yok.");
        e.GuncelleyenKullaniciId = islem.Id;
        e.Guncellendi = DateTime.UtcNow;
        new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor())
            .YazmaKaydet(islem, "YaptirimEslesmesi", e.Id, $"yaptırım eşleşmesi güncellendi: {string.Join(", ", alanlar)} · durum {e.Durum}");
        await db.SaveChangesAsync();
        return new { id = e.Id, durum = e.Durum, masakBildirimTarihi = e.MasakBildirimTarihi, masakEvrakNo = e.MasakEvrakNo, not = e.Not };
    }

    // ── Açık varlık ölçüsü (tek kaynak) ─────────────────────────────────────

    /// <summary>
    /// <see cref="CariBakiye"/> satırındaki TÜM bakiye kolonları (TL, has gram ve her döviz:
    /// USD/EUR/GBP, ileride eklenecek birim de) — adı <c>…Bakiye</c> ile biten her
    /// <c>decimal</c> özellik. Para birimi listesi elle TUTULMAZ: yeni birim kolonu eklenince
    /// ölçü kendiliğinden kapsar (orkestratör bulgusu 01.10 03:25 — eski kod GBP'yi kaçırıyordu).
    /// </summary>
    public static readonly System.Reflection.PropertyInfo[] BakiyeKolonlari =
        typeof(CariBakiye).GetProperties()
            .Where(p => p.PropertyType == typeof(decimal) && p.Name.EndsWith("Bakiye", StringComparison.Ordinal))
            .ToArray();

    /// <summary>Bakiye satırında sıfırdan farklı herhangi bir birim var mı.</summary>
    public static bool AcikBakiyeVar(CariBakiye? b) =>
        b is not null && BakiyeKolonlari.Any(p => (decimal)p.GetValue(b)! != 0m);

    /// <summary>
    /// Carinin MASAK'a bildirilecek varlığı var mı: herhangi bir birimde açık bakiye ·
    /// HER MADEN için ayrı net emanet (altın + gümüş toplanıp birbirini sıfırlayamaz) ·
    /// kapanmamış sipariş.
    /// </summary>
    public static async Task<bool> CariVarlikVar(KuyumcuDbContext db, string tenantId, string cariId)
    {
        var bakiye = await db.CariBakiyeler.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.CariId == cariId).FirstOrDefaultAsync();
        if (AcikBakiyeVar(bakiye)) return true;
        var emanetMadenleri = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.CariId == cariId
                     && (h.Tur == "EMANET_GIRIS" || h.Tur == "EMANET_CIKIS"))
            .GroupBy(h => h.Maden)
            .Select(g => g.Sum(h => h.HasGrami))
            .ToListAsync();
        if (emanetMadenleri.Any(t => t != 0m)) return true;
        return await db.Siparisler.AsNoTracking()
            .AnyAsync(s => s.TenantId == tenantId && s.CariId == cariId
                        && !SiparisDurumu.Terminal.Contains(s.Durum));
    }

    // ── Yazım yardımcıları ──────────────────────────────────────────────────

    private static YaptirimEslesmesi EslesmeEkle(
        KuyumcuDbContext db, string tenantId, YaptirimKaydi k, string tur, string nokta,
        string? cariId, string? kullaniciId, DateTime simdi, DateOnly? sonGun)
    {
        var e = new YaptirimEslesmesi
        {
            Id = Kimlik.Ulid(), TenantId = tenantId, YaptirimKaydiId = k.Id, Tur = tur, Nokta = nokta,
            CariId = cariId, Durum = YaptirimEslesmeDurumu.Acik, BildirimSonGunu = sonGun,
            OlusturanKullaniciId = kullaniciId, Olusturuldu = simdi, Guncellendi = simdi,
        };
        db.YaptirimEslesmeleri.Add(e);
        return e;
    }

    /// <summary>Uygulama içi bildirim (kişisel veri YOK). Tekillik anahtarı eşleşme başınadır.</summary>
    private static void BildirimEkle(
        KuyumcuDbContext db, string tenantId, string onem, string baslik, string govde,
        string eslesmeId, string tekillik) =>
        db.Bildirimler.Add(new Bildirim
        {
            Id = Kimlik.Ulid(), TenantId = tenantId, Tur = BildirimTuru, Onem = onem,
            Baslik = baslik, Govde = govde, HedefRota = Rota, GorunurRoller = Roller,
            IlgiliVarlikTuru = "YaptirimEslesmesi", IlgiliVarlikId = eslesmeId,
            TekillikAnahtari = tekillik, Olusturuldu = DateTime.UtcNow,
        });

    private static string Kirp(string s, int n) => s.Length > n ? s[..n] : s;
}
