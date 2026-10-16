using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Yedek;

// ═════════════════════════════════════════════════════════════════════════════
// YEDEK MODÜLÜ — paket T-3 Faz A.
//
// ⚠ BU SERVİS SÜREÇ BAŞLATMAZ. Ne Process.Start, ne mysqldump, ne dosya
//   sistemi. "Elle yedek al" düğmesi bir SATIR yazar (yedek_talepleri);
//   kuyruğu dotnet/yedek-al-ve-dogrula.sh'ın `talep` modu tüketir.
//   Gerekçe: uygulamadan komut çalıştırmak, uzaktan komut enjeksiyonu
//   yüzeyidir (Mehmet oyu). denetim.sh T3-1 sayacı bunu ZORLAR.
//
// ⚠ İNDİRME UCU YOKTUR ve bilinçlidir: yedek dosyası TÜM kiracıların verisini
//   taşır (PAKET-T-3 § Kapsam dışı). Dosya ADI/YOLU da istemciye dönmez.
//
// ⚠ RPO/RTO İKİ AYRI KAVRAMDIR (MÜHÜR 6 / K2): `...Olculen` alanları veriden
//   HESAPLANIR, `...Taahhut` alanları yapılandırmadan gelir. Biri diğerinin
//   yerine yazılmaz — "ölçüldü" demek ancak ölçümle mümkündür (Anayasa md.1).
// ═════════════════════════════════════════════════════════════════════════════

// ── Yapılandırma ─────────────────────────────────────────────────────────────

/// <summary>
/// appsettings "Yedek" bloğu. ⚠ SIR TUTMAZ — yalnız YOL ve SAYI.
/// <para>
/// Şifreleme anahtarının DEĞERİ burada DURMAZ: T-1'in kurduğu tek hattan,
/// <c>Yedek__SifrelemeAnahtari</c> ortam değişkeninden gelir (Linux'ta
/// /etc/kuyumcu/.env, chmod 600) ve alan şifreleme anahtarından AYRIDIR —
/// aynısı verilirse uygulama açılmaz (UretimAcilisDenetimi:120-128).
/// İkinci bir anahtar dosyası yolu AÇILMADI (MÜHÜR 3 · sır disiplini tek hat).
/// </para>
/// </summary>
public sealed class YedekAyarlari
{
    public string Dizin { get; init; } = "/var/lib/kuyumcu/yedek";
    public string DisHedef { get; init; } = "";
    public int SaklamaGun { get; init; } = 30;
    /// <summary>TAAHHÜT (ölçüm değil): iki yedek arası azami kabul edilen dakika.</summary>
    public int RpoDakikaTaahhut { get; init; } = 360;
    /// <summary>TAAHHÜT (ölçüm değil): geri yükleme için verilen azami dakika.</summary>
    public int RtoDakikaTaahhut { get; init; } = 120;
    /// <summary>
    /// PAKET P-33 (BK-30 · Köprü K-5): son BAŞARILI geri yükleme tatbikatının
    /// azami yaşı (gün). Bundan eskiyse /yedekler "bayat" der. Üst sınır 30:
    /// yapılandırma bu kuralı GEVŞETEMEZ, yalnız sıkılaştırabilir (1..30).
    /// </summary>
    public int TatbikatAzamiGun { get; init; } = TatbikatAzamiGunTavani;

    /// <summary>"30 günden eski olamaz" — yapılandırmanın aşamayacağı tavan.</summary>
    public const int TatbikatAzamiGunTavani = 30;

    /// <summary>
    /// Yapılandırmadan okur ve sıkıştırır (KurZamanlayici kalıbı): elle yazılmış
    /// saçma bir değer (0 gün saklama, 1 dakika RTO) sessizce kabul edilmez.
    /// </summary>
    public static YedekAyarlari Oku(IConfiguration y)
    {
        var b = y.GetSection("Yedek");
        return new YedekAyarlari
        {
            Dizin = b["Dizin"] ?? "/var/lib/kuyumcu/yedek",
            DisHedef = b["DisHedef"] ?? "",
            SaklamaGun = Math.Clamp(b.GetValue("SaklamaGun", 30), 7, 3650),
            RpoDakikaTaahhut = Math.Clamp(b.GetValue("RpoDakika", 360), 15, 10080),
            RtoDakikaTaahhut = Math.Clamp(b.GetValue("RtoDakika", 120), 15, 10080),
            TatbikatAzamiGun = Math.Clamp(b.GetValue("TatbikatAzamiGun", TatbikatAzamiGunTavani), 1, TatbikatAzamiGunTavani),
        };
    }
}

// ── Politika (fail-closed kuralların TEK metni) ──────────────────────────────

/// <summary>
/// Yedek koşusunun fail-closed kuralları. Betik (dotnet/yedek-al-ve-dogrula.sh)
/// bu kuralların işletim tarafını uygular; buradaki metin SÖZLEŞMEDİR ve
/// ekranın "sağlıklı mı" cevabı buradan çıkar. Testler ve mutasyon kanıtları
/// bu sınıfa dayanır; betik tarafı ayrıca YedekPolitikaTestleri'ndeki betik
/// sözleşme testi ve denetim.sh T3 sayaçlarıyla korunur.
/// </summary>
public static class YedekPolitikasi
{
    /// <summary>
    /// FAIL-CLOSED 2 + 3: şifresiz yedek ya da alınamamış dış kopya =
    /// BAŞARISIZ. "Tek kopya yedek, yedek değildir" (Mühür 2, Mehmet).
    /// </summary>
    public static string KosuSonucu(bool sifreli, string disKopya, bool checksumUyustu = true)
    {
        if (!sifreli) return YedekDurumu.Basarisiz;
        if (disKopya != DisKopyaDurumu.Kopyalandi) return YedekDurumu.Basarisiz;
        if (!checksumUyustu) return YedekDurumu.Basarisiz;
        return YedekDurumu.Basarili;
    }

    /// <summary>
    /// FAIL-CLOSED 4: son saklama penceresinde DOĞRULANMIŞ yedek yoksa
    /// hiçbir dosya silinmez — silinirse elde doğrulanmamış yedekten başka
    /// bir şey kalmazdı.
    /// </summary>
    public static bool SaklamaSilinebilir(int dogrulanmisYedekSayisi) =>
        dogrulanmisYedekSayisi > 0;

    /// <summary>
    /// Tatbikat raporu metni. ⚠ YALNIZ TABLO ADI: hiçbir satır/alan DEĞERİ
    /// yazılmaz (yedek kişisel veri taşır — KVKK md.12/1, Mehmet koşulu).
    /// </summary>
    public static string TatbikatRaporu(IEnumerable<string> uyusmayanTablolar) =>
        "uyusmayan_tablolar=" + string.Join(",", uyusmayanTablolar);

    /// <summary>
    /// PAKET P-33 (BK-30 · Köprü K-5 · <c>YEDEK_TATBIKAT_BAYAT = 0</c>): son
    /// BAŞARILI geri yükleme tatbikatı <paramref name="azamiGun"/> günden eskiyse
    /// ya da HİÇ yoksa tatbikat BAYATTIR. Başarısız tatbikat tazelik sayılmaz —
    /// "denendi" ile "geri yüklenebildi" aynı şey değildir (Anayasa md.1).
    /// Sınır dâhildir: tam <paramref name="azamiGun"/> gün önceki tatbikat
    /// henüz bayat değildir.
    /// </summary>
    public static bool TatbikatBayatMi(DateTime? sonBasariliTatbikat, DateTime simdiUtc, int azamiGun)
    {
        if (sonBasariliTatbikat is not { } son) return true;
        return (simdiUtc - son) > TimeSpan.FromDays(azamiGun);
    }

    /// <summary>Son başarılı tatbikatın yaşı (tam gün); tatbikat yoksa null.</summary>
    public static int? TatbikatYasGun(DateTime? sonBasariliTatbikat, DateTime simdiUtc) =>
        sonBasariliTatbikat is { } son ? (int)Math.Max(0, Math.Floor((simdiUtc - son).TotalDays)) : null;
}

// ── DTO'lar ──────────────────────────────────────────────────────────────────

/// <summary>POST /yedekler/talep gövdesi.</summary>
public sealed class YedekTalepDto
{
    /// <summary>NEDEN — en az 20 karakter. Denetim izine bu yazılır.</summary>
    [Required(ErrorMessage = "Gerekçe zorunludur")]
    [MinLength(20, ErrorMessage = "Gerekçe en az 20 karakter olmalıdır")]
    [MaxLength(500)]
    public string Gerekce { get; set; } = null!;
}

/// <summary>
/// PATCH /yedekler/talep/{id} gövdesi — ÜÇ DURUMLU PATCH sözleşmesi
/// (gövdede yoksa KORU · null ise SİL · dolu ise YAZ). Mevcut
/// <c>…Gonderildi</c> deseni kullanılır; yeni desen açılmaz.
/// </summary>
public sealed class YedekTalepGuncelleDto
{
    private string? _iptalGerekce;

    [JsonIgnore] public bool IptalGerekceGonderildi { get; private set; }

    /// <summary>İptal gerekçesi (≥20). null gönderilirse mevcut gerekçe SİLİNİR.</summary>
    [MaxLength(500)]
    public string? IptalGerekce
    {
        get => _iptalGerekce;
        set { _iptalGerekce = value; IptalGerekceGonderildi = true; }
    }
}

/// <summary>
/// PAKET P-56 — PATCH /yedekler/talep/{id}/gerekce gövdesi.
/// <para>
/// ÜÇ DURUMLU PATCH sözleşmesi (gövdede yoksa KORU · null ise SİL · dolu ise
/// YAZ) burada bilinçli olarak İKİ duruma indirgenir: gerekçe ZORUNLU alandır
/// (talep gerekçesiz doğamaz — POST /talep en az 20 karakter ister), "sil"
/// dalı kaydı mevzuatsız bırakırdı. null gönderimi 400 ile reddedilir ve
/// sebebi yanıtta yazar; sessiz kabul YOKTUR (Anayasa md.1).
/// </para>
/// <para>
/// ⚠ AYRI UÇ açıldı, iptal ucuna alan EKLENMEDİ: mevcut
/// <c>PATCH /yedekler/talep/{id}</c> her çağrıda talebi İPTAL eder; oraya
/// gerekçe alanı eklenseydi "gerekçemi düzelteyim" diyen kullanıcının talebi
/// iptal olurdu.
/// </para>
/// </summary>
public sealed class YedekGerekceGuncelleDto
{
    private string? _gerekce;

    [JsonIgnore] public bool GerekceGonderildi { get; private set; }

    /// <summary>Yeni gerekçe (≥20 karakter). null gönderimi 400'dür — gerekçe silinemez.</summary>
    [MinLength(20, ErrorMessage = "Gerekçe en az 20 karakter olmalıdır")]
    [MaxLength(500)]
    public string? Gerekce
    {
        get => _gerekce;
        set { _gerekce = value; GerekceGonderildi = true; }
    }
}

// ── Servis ───────────────────────────────────────────────────────────────────

/// <summary>
/// Yedek üstverisi servisi. YALNIZ veritabanı okur/yazar.
/// </summary>
public sealed class YedekServisi(KuyumcuDbContext db, DenetimServisi denetim, IConfiguration yapilandirma)
{
    /// <summary>Ekranda da yazan sunucu sınırı (UX-S29).</summary>
    public const int AzamiLimit = 50;
    public const int GerekceEnAz = 20;

    public YedekAyarlari Ayarlar { get; } = YedekAyarlari.Oku(yapilandirma);

    /// <summary>Özet kartları: son yedek, doğrulama, dış kopya, ölçülen RPO ve taahhütler.</summary>
    public async Task<object> Ozet(AktifKullanici islem)
    {
        var son = await db.YedekKayitlari.AsNoTracking()
            .OrderByDescending(y => y.Baslangic).FirstOrDefaultAsync();

        var sonBasarili = await db.YedekKayitlari.AsNoTracking()
            .Where(y => y.Durum == YedekDurumu.Basarili)
            .OrderByDescending(y => y.Baslangic).FirstOrDefaultAsync();

        var sonDogrulanmis = await db.YedekKayitlari.AsNoTracking()
            .Where(y => y.Dogrulama == YedekDogrulama.Dogrulandi)
            .OrderByDescending(y => y.Baslangic).FirstOrDefaultAsync();

        var sonTatbikat = await db.YedekTatbikatlari.AsNoTracking()
            .OrderByDescending(t => t.Tarih).FirstOrDefaultAsync();

        // PAKET P-33 (BK-30): tazelik YALNIZ başarılı tatbikattan ölçülür.
        var sonBasariliTatbikat = await db.YedekTatbikatlari.AsNoTracking()
            .Where(t => t.Sonuc == YedekDurumu.Basarili)
            .OrderByDescending(t => t.Tarih).Select(t => (DateTime?)t.Tarih).FirstOrDefaultAsync();
        var simdi = DateTime.UtcNow;

        var kosuSayisi = await db.YedekKayitlari.CountAsync();

        // FAIL-CLOSED 4'ün ekranda görünen yarısı: saklama penceresi içinde
        // DOĞRULANMIŞ yedek var mı?
        var pencere = DateTime.UtcNow.AddDays(-Ayarlar.SaklamaGun);
        var dogrulanmisSayisi = await db.YedekKayitlari
            .CountAsync(y => y.Dogrulama == YedekDogrulama.Dogrulandi && y.Baslangic >= pencere);

        // PAKET P-77: bu işletmenin BEKLEYEN doğrulama talebi (varsa). Ekran
        // "Şimdi doğrula" düğmesini buna göre kilitler ve SEBEBİNİ yazar.
        var bekleyenDogrulama = await db.YedekTalepleri.AsNoTracking()
            .Where(t => t.Tur == YedekTalepTuru.Dogrulama && t.Durum == YedekTalepDurumu.Bekliyor)
            .OrderByDescending(t => t.Olusturuldu)
            .Select(t => new { id = t.Id, olusturuldu = t.Olusturuldu })
            .FirstOrDefaultAsync();

        await denetim.ListeOkumaKaydet(islem, "YedekKaydi", kosuSayisi, "yedek özeti");

        // ÖLÇÜM: son BAŞARILI yedekten bu yana geçen dakika. Yedek hiç yoksa
        // null döner — "0 dakika" demek yalan olurdu (Anayasa md.1).
        int? rpoOlculen = sonBasarili is null
            ? null
            : (int)Math.Max(0, (DateTime.UtcNow - sonBasarili.Baslangic).TotalMinutes);

        return new
        {
            sonYedek = son is null ? null : new
            {
                tur = son.Tur,
                zaman = son.Baslangic,
                bitis = son.Bitis,
                boyutBayt = son.BoyutBayt,
                durum = son.Durum,
                sifreli = son.Sifreli,
                tabloSayisi = son.TabloSayisi,
                hataOzeti = son.HataOzeti,
            },
            dogrulama = son?.Dogrulama ?? YedekDogrulama.Bekliyor,
            dogrulamaTarih = son?.DogrulamaTarih,
            sonDogrulanmisYedek = sonDogrulanmis?.Baslangic,
            disKopya = son?.DisKopya ?? DisKopyaDurumu.Yok,
            saklananKosu = kosuSayisi,
            saklamaGun = Ayarlar.SaklamaGun,
            // Fail-closed sözleşmesinin ekran karşılığı: koşu "BASARILI" yazsa
            // bile şifresiz ya da dış kopyasız ise SAĞLIKLI DEĞİLDİR.
            sonKosuSaglikli = son is not null
                && YedekPolitikasi.KosuSonucu(son.Sifreli, son.DisKopya,
                       son.Dogrulama != YedekDogrulama.Uyusmadi) == YedekDurumu.Basarili,
            saklamaSilmeAktif = YedekPolitikasi.SaklamaSilinebilir(dogrulanmisSayisi),
            rpoDakikaOlculen = rpoOlculen,
            rpoDakikaTaahhut = Ayarlar.RpoDakikaTaahhut,
            rtoDakikaTaahhut = Ayarlar.RtoDakikaTaahhut,
            rtoDakikaOlculen = sonTatbikat?.RtoDakikaOlculen,
            sonTatbikat = sonTatbikat?.Tarih,
            sonTatbikatSonuc = sonTatbikat?.Sonuc,
            // PAKET P-33 — ekran "son tatbikat tarihi"ni ve bayatlığını
            // SUNUCUNUN hesabından gösterir; 30 gün istemciye gömülmez.
            sonBasariliTatbikat,
            tatbikatYasGun = YedekPolitikasi.TatbikatYasGun(sonBasariliTatbikat, simdi),
            tatbikatAzamiGun = Ayarlar.TatbikatAzamiGun,
            tatbikatBayat = YedekPolitikasi.TatbikatBayatMi(sonBasariliTatbikat, simdi, Ayarlar.TatbikatAzamiGun),
            azamiLimit = AzamiLimit,
            bekleyenDogrulamaTalebi = bekleyenDogrulama,
        };
    }

    /// <summary>Koşu listesi. ⚠ dosya adı/yolu DÖNMEZ.</summary>
    public async Task<object> Liste(AktifKullanici islem, int limit)
    {
        var sinir = Math.Clamp(limit, 1, AzamiLimit);
        var kayitlar = await db.YedekKayitlari.AsNoTracking()
            .OrderByDescending(y => y.Baslangic)
            .Take(sinir + 1)
            .ToListAsync();

        var dahaFazlasiVar = kayitlar.Count > sinir;
        if (dahaFazlasiVar) kayitlar.RemoveAt(kayitlar.Count - 1);

        await denetim.ListeOkumaKaydet(islem, "YedekKaydi", kayitlar.Count);

        return new
        {
            limit = sinir,
            azamiLimit = AzamiLimit,
            dahaFazlasiVar,
            kayitlar = kayitlar.Select(y => new
            {
                id = y.Id,
                tur = y.Tur,
                baslangic = y.Baslangic,
                bitis = y.Bitis,
                durum = y.Durum,
                boyutBayt = y.BoyutBayt,
                sifreli = y.Sifreli,
                disKopya = y.DisKopya,
                dogrulama = y.Dogrulama,
                tabloSayisi = y.TabloSayisi,
                hataOzeti = y.HataOzeti,
            }),
        };
    }

    /// <summary>Aylık geri-yükleme tatbikatları. Rapor alanı yalnız TABLO ADI taşır.</summary>
    public async Task<object> Tatbikatlar(AktifKullanici islem, int limit)
    {
        var sinir = Math.Clamp(limit, 1, AzamiLimit);
        var liste = await db.YedekTatbikatlari.AsNoTracking()
            .OrderByDescending(t => t.Tarih).Take(sinir).ToListAsync();

        await denetim.ListeOkumaKaydet(islem, "YedekTatbikati", liste.Count);

        return new
        {
            limit = sinir,
            azamiLimit = AzamiLimit,
            tatbikatlar = liste.Select(t => new
            {
                id = t.Id,
                tarih = t.Tarih,
                tabloSayisi = t.TabloSayisi,
                uyusan = t.Uyusan,
                uyusmayan = t.Uyusmayan,
                sonuc = t.Sonuc,
                sureSn = t.SureSn,
                semaDusuruldu = t.SemaDusuruldu,
                rtoDakikaOlculen = t.RtoDakikaOlculen,
                rapor = t.Rapor,
            }),
        };
    }

    /// <summary>Kiracının BEKLEYEN talepleri (kuyruk görünürlüğü).</summary>
    public async Task<object> Talepler(AktifKullanici islem, int limit)
    {
        var sinir = Math.Clamp(limit, 1, AzamiLimit);
        var liste = await db.YedekTalepleri.AsNoTracking()
            .OrderByDescending(t => t.Olusturuldu).Take(sinir).ToListAsync();

        await denetim.ListeOkumaKaydet(islem, "YedekTalebi", liste.Count);

        return new
        {
            limit = sinir,
            azamiLimit = AzamiLimit,
            talepler = liste.Select(t => new
            {
                id = t.Id,
                gerekce = t.Gerekce,
                durum = t.Durum,
                olusturuldu = t.Olusturuldu,
                islendi = t.Islendi,
                yedekKaydiId = t.YedekKaydiId,
                iptalGerekce = t.IptalGerekce,
                tur = t.Tur,
            }),
        };
    }

    /// <summary>
    /// Elle yedek TALEBİ açar. Süreç BAŞLATMAZ; betik kuyruğu tüketir.
    /// Kiracı başına aynı anda TEK bekleyen talep olur — ikincisi 429.
    /// </summary>
    public async Task<object> TalepAc(AktifKullanici islem, YedekTalepDto dto)
    {
        // DTO DataAnnotations yalnız controller yolunda çalışır; servis içi
        // elle kontrol ZORUNLUDUR (değişmez kural).
        var gerekce = (dto.Gerekce ?? "").Trim();
        if (gerekce.Length < GerekceEnAz)
            throw new BadRequestException($"Gerekçe en az {GerekceEnAz} karakter olmalıdır.");

        var bekleyen = await db.YedekTalepleri
            .AnyAsync(t => t.Durum == YedekTalepDurumu.Bekliyor
                           && t.Tur == YedekTalepTuru.Yedek);
        if (bekleyen)
            throw new ApiException(429,
                "Bu işletme için bekleyen bir yedek talebi zaten var; " +
                "kuyruk tüketilmeden ikinci talep açılamaz.", "Too Many Requests");

        var kayit = new YedekTalebi
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            KullaniciId = islem.Id,
            Gerekce = gerekce,
            Durum = YedekTalepDurumu.Bekliyor,
            Tur = YedekTalepTuru.Yedek,
            Olusturuldu = DateTime.UtcNow,
        };
        db.YedekTalepleri.Add(kayit);
        denetim.YazmaKaydet(islem, "YedekTalebi", kayit.Id,
            $"elle yedek talebi açıldı · gerekçe: {gerekce}", hassas: true);
        await db.SaveChangesAsync();

        return new
        {
            id = kayit.Id,
            durum = kayit.Durum,
            olusturuldu = kayit.Olusturuldu,
            mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Yedek talebi kuyruğa alındı; zamanlayıcı en geç 60 saniye içinde çalıştırır."),
        };
    }

    /// <summary>
    /// PAKET P-77 — "Şimdi doğrula": yedek DOĞRULAMA talebi açar.
    /// <para>
    /// Süreç BAŞLATMAZ (T3-1): kuyruğa <c>tur=DOGRULAMA</c> satırı yazar; betiğin
    /// <c>talep</c> modu bunu görünce son BAŞARILI yedeği ayrı şemaya geri yükleyip
    /// sağlama toplamıyla doğrular (tatbikat). Doğrulama yedek DOSYASINI yalnız
    /// OKUR — silmez, değiştirmez.
    /// </para>
    /// <para>
    /// Kiracı başına aynı anda TEK bekleyen doğrulama talebi olur; ikincisi 409
    /// (çift kuyruk yok). Elle YEDEK talebi kuyruğundan bağımsızdır: bekleyen bir
    /// yedek talebi doğrulama talebini engellemez, tersi de.
    /// </para>
    /// </summary>
    public async Task<object> DogrulamaTalepAc(AktifKullanici islem)
    {
        var bekleyen = await db.YedekTalepleri
            .AnyAsync(t => t.Tur == YedekTalepTuru.Dogrulama && t.Durum == YedekTalepDurumu.Bekliyor);
        if (bekleyen)
            throw new ConflictException(
                "Bu işletme için bekleyen bir yedek doğrulama talebi zaten var; " +
                "sonuçlanmadan ikinci doğrulama talebi açılamaz.");

        var kayit = new YedekTalebi
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            KullaniciId = islem.Id,
            Gerekce = DogrulamaGerekcesi,
            Durum = YedekTalepDurumu.Bekliyor,
            Tur = YedekTalepTuru.Dogrulama,
            Olusturuldu = DateTime.UtcNow,
        };
        db.YedekTalepleri.Add(kayit);
        denetim.YazmaKaydet(islem, "YedekTalebi", kayit.Id,
            "yedek doğrulama talebi açıldı (ekrandan · son başarılı yedek geri yüklenip doğrulanacak)",
            hassas: true);
        await db.SaveChangesAsync();

        return new
        {
            id = kayit.Id,
            tur = kayit.Tur,
            durum = kayit.Durum,
            olusturuldu = kayit.Olusturuldu,
            mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Doğrulama talebi kuyruğa alındı; zamanlayıcı en geç 60 saniye içinde başlatır."),
        };
    }

    /// <summary>Doğrulama talebinin kayıtlı gerekçesi (≥20 karakter; P-56 ile düzeltilebilir).</summary>
    public const string DogrulamaGerekcesi = "Ekrandan başlatılan yedek doğrulama talebi";

    /// <summary>
    /// PAKET P-56 — BEKLEYEN talebin GEREKÇESİNİ düzeltir.
    /// <para>
    /// Gerekçe, talep kuyruktan tüketilene dek yalnız bir NİYET beyanıdır;
    /// taşa yazılması için hiçbir mevzuat dayanağı yoktur (UX-S25/UX-1).
    /// Talep işlenmeye başladıysa (CALISIYOR/ISLENDI/HATA/IPTAL) artık
    /// denetim izinin parçasıdır ve DEĞİŞMEZ → 400.
    /// </para>
    /// <para>⚠ Durum değişmez, satır silinmez: yalnız <c>Gerekce</c> yazılır.</para>
    /// </summary>
    public async Task<object> TalepGerekceGuncelle(
        AktifKullanici islem, string id, YedekGerekceGuncelleDto dto)
    {
        // ⚠ Find/FindAsync YASAK (kiracı süzgecini atlar): açık sorgu.
        var kayit = await db.YedekTalepleri.FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new NotFoundException("Yedek talebi bulunamadı.");

        // ── DURUM KAPISI ────────────────────────────────────────────────────
        if (kayit.Durum != YedekTalepDurumu.Bekliyor)
            throw new BadRequestException(
                "Yalnız BEKLIYOR durumundaki talebin gerekçesi düzenlenebilir " +
                $"(mevcut durum: {kayit.Durum}). İşlenmiş talebin gerekçesi denetim izinin parçasıdır.");

        // PATCH sözleşmesi: alan gövdede YOKSA korunur — kayıt değişmez,
        // denetim izine satır YAZILMAZ ("değişmedi" demek için iz yazmak,
        // izi kirletmekten başka işe yaramaz).
        if (!dto.GerekceGonderildi)
            return new
            {
                id = kayit.Id,
                durum = kayit.Durum,
                gerekce = kayit.Gerekce,
                degisti = false,
                mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Gövdede gerekçe alanı yok; kayıt korundu."),
            };

        // null = SİL dalı UYGULANMAZ: gerekçe zorunlu alandır (DTO belgesi).
        // DTO DataAnnotations yalnız controller yolunda çalışır; servis içi
        // elle kontrol ZORUNLUDUR (değişmez kural).
        var yeni = (dto.Gerekce ?? "").Trim();
        if (yeni.Length < GerekceEnAz)
            throw new BadRequestException(
                $"Gerekçe en az {GerekceEnAz} karakter olmalıdır; gerekçe silinemez.");

        var eski = kayit.Gerekce;
        if (string.Equals(eski, yeni, StringComparison.Ordinal))
            return new
            {
                id = kayit.Id,
                durum = kayit.Durum,
                gerekce = kayit.Gerekce,
                degisti = false,
                mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Gerekçe zaten aynı; kayıt değişmedi."),
            };

        kayit.Gerekce = yeni;
        // Denetim izi ESKİ ve YENİ değeri birlikte taşır: "ne yazıyordu, ne
        // oldu" sorusunun cevabı tek satırda durur (kabul koşulu 4).
        denetim.YazmaKaydet(islem, "YedekTalebi", kayit.Id,
            $"yedek talebi gerekçesi güncellendi · eski: {eski} · yeni: {yeni}", hassas: true);
        await db.SaveChangesAsync();

        return new
        {
            id = kayit.Id,
            durum = kayit.Durum,
            gerekce = kayit.Gerekce,
            degisti = true,
            mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Gerekçe güncellendi; eski ve yeni değer denetim izine yazıldı."),
        };
    }

    /// <summary>
    /// Bekleyen talebi İPTAL eder. ⚠ HARD DELETE YOK: satır durur, durumu
    /// IPTAL olur (DoD sözleşmesi — "kim ne istedi, kim iptal etti" izi).
    /// </summary>
    public async Task<object> TalepIptal(AktifKullanici islem, string id, YedekTalepGuncelleDto dto)
    {
        // ⚠ Find/FindAsync YASAK (kiracı süzgecini atlar): açık sorgu.
        var kayit = await db.YedekTalepleri.FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new NotFoundException("Yedek talebi bulunamadı.");

        if (kayit.Durum != YedekTalepDurumu.Bekliyor)
            throw new BadRequestException(
                $"Yalnız BEKLIYOR durumundaki talep iptal edilebilir (mevcut durum: {kayit.Durum}).");

        // PATCH sözleşmesi: alan gövdede YOKSA korunur.
        if (dto.IptalGerekceGonderildi)
        {
            var g = dto.IptalGerekce?.Trim();
            if (g is not null && g.Length < GerekceEnAz)
                throw new BadRequestException($"İptal gerekçesi en az {GerekceEnAz} karakter olmalıdır.");
            kayit.IptalGerekce = string.IsNullOrEmpty(g) ? null : g;
        }

        if (string.IsNullOrEmpty(kayit.IptalGerekce))
            throw new BadRequestException($"İptal gerekçesi en az {GerekceEnAz} karakter olmalıdır.");

        kayit.Durum = YedekTalepDurumu.Iptal;
        kayit.Islendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, "YedekTalebi", kayit.Id,
            $"yedek talebi iptal edildi · gerekçe: {kayit.IptalGerekce}", hassas: true);
        await db.SaveChangesAsync();

        return new
        {
            id = kayit.Id,
            durum = kayit.Durum,
            iptalGerekce = kayit.IptalGerekce,
            mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Talep iptal edildi; kayıt silinmedi (iz korunur)."),
        };
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

/// <summary>
/// Yedek uçları. Sınıf düzeyinde <c>[RolGerekli("SAHIP")]</c>: yedek üstverisi
/// tüm örneği kapsar, tezgâhtar görmez (AdminController kalıbı).
/// </summary>
[ApiController]
[Authorize]
[RolGerekli("SAHIP")]
[Route("api/v1/yedekler")]
[Yetki.ModulYetkisi("yedek")]
public sealed class YedekController(YedekServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    [HttpGet("ozet")]
    public Task<object> Ozet() => servis.Ozet(Islem);

    [HttpGet]
    public Task<object> Liste([FromQuery] int limit = 50) => servis.Liste(Islem, limit);

    [HttpGet("tatbikatlar")]
    public Task<object> Tatbikatlar([FromQuery] int limit = 12) => servis.Tatbikatlar(Islem, limit);

    [HttpGet("talepler")]
    public Task<object> Talepler([FromQuery] int limit = 20) => servis.Talepler(Islem, limit);

    [HttpPost("talep")]
    public async Task<IActionResult> TalepAc([FromBody] YedekTalepDto dto)
        => StatusCode(201, await servis.TalepAc(Islem, dto));

    /// <summary>
    /// PAKET P-77 — "Şimdi doğrula": kuyruğa doğrulama talebi yazar (201).
    /// Bekleyen doğrulama talebi varken 409. Rol kapısı sınıf düzeyindeki
    /// <c>[RolGerekli("SAHIP")]</c> + <c>[ModulYetkisi("yedek")]</c>'tir — mevcut
    /// yedek uçlarıyla BİREBİR aynı (ölçüldü; ayrı, gevşek bir kapı açılmadı).
    /// Pasife alma yolu: <c>PATCH talep/{id}</c> (iptal) — tür fark etmez.
    /// </summary>
    [HttpPost("dogrulama-talep")]
    public async Task<IActionResult> DogrulamaTalepAc()
        => StatusCode(201, await servis.DogrulamaTalepAc(Islem));

    [HttpPatch("talep/{id}")]
    public Task<object> TalepIptal(string id, [FromBody] YedekTalepGuncelleDto dto)
        => servis.TalepIptal(Islem, id, dto);

    /// <summary>PAKET P-56 — bekleyen talebin gerekçesini düzeltir (iptal ETMEZ).</summary>
    [HttpPatch("talep/{id}/gerekce")]
    public Task<object> TalepGerekceGuncelle(string id, [FromBody] YedekGerekceGuncelleDto dto)
        => servis.TalepGerekceGuncelle(Islem, id, dto);
}
