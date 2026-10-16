using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Common;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-1 — YASAL PARAMETRE SERVİSİ (tarihli eşik okuma · tek kapı)
//
// NE ÇÖZER: tevsik haddi, MASAK kimlik eşiği gibi rakamlar açılışta appsettings'ten
// BİR KEZ okunup singleton yapılıyordu. Had 1 Ocak'ta değişse (1) sunucu o gece
// yeniden başlatılmalıydı, (2) 31 Aralık işleminin 2 Ocak'taki değerlendirmesi
// (risk penceresi, rapor, geç fark edilen ihlal) YENİ rakamla yapılıyordu.
//
// KURAL:
//   • Değer İŞLEMİN YEREL GÜNÜNE göre bulunur (Europe/Istanbul, K5) — UTC değil.
//   • Aralık [başlangıç, bitiş): başlangıç DAHİL, bitiş HARİÇ.
//   • Değer yoksa FAIL-CLOSED: YasalParametreYokException → 503 "Yasal sınır
//     tanımlı değil". Kod varsayılanına (30.000) DÜŞÜLMEZ — sessiz varsayılan yok.
//   • Önbellek: tüm tablo bellekte. Yerel gün değişince (gece 00:00) ilk erişimde
//     ve sistem ucu yazınca tazelenir — yeniden başlatma gerekmez.
//
// ⚠ Kiracı-üstü tablo: okuma SistemKapsami.Sistem() ile yapılır (SistemKapsami.cs
//   izin listesi madde 9). Kişisel veri yoktur.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>İstenen gün için yasal değer tanımlı değil → işlem YAPILMAZ (503).</summary>
public sealed class YasalParametreYokException(string anahtar, DateOnly gun)
    : ApiException(503, "Yasal sınır tanımlı değil; destek ile görüşün.", "Service Unavailable")
{
    public string Anahtar { get; } = anahtar;
    public DateOnly Gun { get; } = gun;
}

/// <summary>Önbellekteki satır (değişmez).</summary>
public sealed record YasalParametreSatiri(
    string Id,
    string Anahtar,
    decimal? DegerSayi,
    string? DegerMetin,
    string Birim,
    DateOnly Baslangic,
    DateOnly? Bitis,
    string Dayanak,
    string KaynakUrl,
    string Teyit,
    string AciklamaSade)
{
    /// <summary>PAKET YASAL-PARAM-EKRAN: Resmî Gazete tarihi (eski tohum satırlarında boş olabilir).</summary>
    public DateOnly? ResmiGazeteTarihi { get; init; }

    /// <summary>PAKET YASAL-PARAM-EKRAN: Resmî Gazete sayısı.</summary>
    public string? ResmiGazeteSayisi { get; init; }

    /// <summary>
    /// PAKET YASAL-PARAM-2: bugünden geçerli yazılan satırın yürürlüğe girdiği AN (UTC);
    /// null = başlangıç gününün yerel 00:00'ı.
    /// </summary>
    public DateTime? BaslangicAni { get; init; }

    /// <summary>PAKET YASAL-PARAM-2: dönem başlangıcının UTC anı (anı yoksa yerel gün başı).</summary>
    public DateTime EtkinBaslangicUtc =>
        BaslangicAni is { } a ? DateTime.SpecifyKind(a, DateTimeKind.Utc) : Zaman.YerelGunBasiUtc(Baslangic);

    /// <summary>[Baslangic, Bitis) — bitiş HARİÇ.</summary>
    public bool GecerliMi(DateOnly gun) => Baslangic <= gun && (Bitis is null || gun < Bitis.Value);

    public static YasalParametreSatiri Kaynaktan(YasalParametre p) => new(
        p.Id, p.Anahtar, p.DegerSayi, p.DegerMetin, p.Birim, p.GecerlilikBaslangic,
        p.GecerlilikBitis, p.Dayanak, p.KaynakUrl, p.Teyit, p.AciklamaSade)
    {
        ResmiGazeteTarihi = p.ResmiGazeteTarihi,
        ResmiGazeteSayisi = p.ResmiGazeteSayisi,
        BaslangicAni = p.GecerlilikBaslangicAni,
    };
}

/// <summary>
/// PAKET YASAL-PARAM-2 — POST /api/v1/sistem/yasal-parametreler/{id}/duzeltme(/onizleme) gövdesi:
/// BUGÜNDEN GEÇERLİ yanlış girişin AYNI GÜN düzeltilmesi. Yalnız değeri giren operatör; gerekçe
/// zorunlu; düzeltme anından ÖNCEKİ işlemler yanlış değerle kayıtlı kalır (geçmiş yeniden yazılmaz).
/// DataAnnotations yalnız controller yolunda koşar — servis her kuralı ELLE de denetler.
/// </summary>
public sealed class YasalParametreDuzeltDto
{
    public decimal? DegerSayi { get; set; }
    [MaxLength(1000)] public string? Gerekce { get; set; }
    [MaxLength(32)] public string? OnayKodu { get; set; }
}

/// <summary>
/// POST /api/v1/sistem/yasal-parametreler (ve /onizleme) gövdesi.
/// PAKET YASAL-PARAM-EKRAN: DataAnnotations yalnız controller yolunda koşar — her kural
/// <see cref="YasalParametreServisi"/> içinde ELLE de denetlenir. Dayanak / sade açıklama /
/// teyit boşsa sunucu türetir (UX-S21): dayanak ve sade açıklama o anahtarın bugünkü
/// satırından, teyit DOGRULANDI (Resmî Gazete künyesi zorunlu olduğu için).
/// </summary>
public sealed class YasalParametreEkleDto
{
    [Required(ErrorMessage = "Anahtar zorunludur."), MaxLength(60)] public string? Anahtar { get; set; }
    public decimal? DegerSayi { get; set; }
    [MaxLength(200)] public string? DegerMetin { get; set; }
    /// <summary>Yerel gün (YYYY-MM-DD). BUGÜN ya da ileri tarih; geçmiş REDDEDİLİR (400).</summary>
    [Required(ErrorMessage = "Başlangıç günü zorunludur.")] public DateOnly? Baslangic { get; set; }
    [MaxLength(300)] public string? Dayanak { get; set; }
    /// <summary>İsteğe bağlı iç not (teyit/mali müşavir notu) — ekrana basılmaz.</summary>
    [MaxLength(300)] public string? IcNot { get; set; }
    [Required(ErrorMessage = "Kaynak adresi zorunludur."), MaxLength(500)] public string? KaynakUrl { get; set; }
    [MaxLength(12)] public string? Teyit { get; set; }
    [MaxLength(300)] public string? AciklamaSade { get; set; }
    [Required(ErrorMessage = "Gerekçe zorunludur."), MaxLength(1000)] public string? Gerekce { get; set; }
    /// <summary>PAKET YASAL-PARAM-EKRAN: Resmî Gazete tarihi — ZORUNLU.</summary>
    public DateOnly? ResmiGazeteTarihi { get; set; }
    /// <summary>PAKET YASAL-PARAM-EKRAN: Resmî Gazete sayısı — ZORUNLU (rakamla başlar).</summary>
    [MaxLength(20)] public string? ResmiGazeteSayisi { get; set; }
    /// <summary>
    /// PAKET YASAL-PARAM-EKRAN — İKİNCİ ONAY ADIMI: /onizleme yanıtındaki onay kodu. Yoksa 400;
    /// önizlemeden sonra içerik ya da o anahtarın bugünkü değeri değiştiyse 409.
    /// </summary>
    [MaxLength(32)] public string? OnayKodu { get; set; }
}

public sealed class YasalParametreServisi
{
    /// <summary>Sistem anahtarlı yazma ucunun gerekçe alt sınırı (sistem uçlarıyla aynı).</summary>
    public const int GerekceAsgari = 20;

    /// <summary>Panel bandının ufku (gün) — şartname § 4.5.</summary>
    public const int BantUfkuGun = 7;

    /// <summary>"Yaklaşan değişiklikler" ucunun ufku (gün) — şartname § 4.3.</summary>
    public const int YaklasanUfkuGun = 30;

    /// <summary>
    /// PAKET YASAL-PARAM-EKRAN tur 2 — AKLA YATKINLIK BANDI (yazım hatası koruması, MEVZUAT DEĞERİ
    /// DEĞİLDİR): bugünkü değeri pozitif olan sınırda yeni değer sıfır/negatif olamaz ve bugünkünün
    /// bu katından fazla büyüyüp küçülemez (ör. 370.000 yerine 999.999.999,99 ya da saklama yılı 0).
    /// Gerçekten böyle bir mevzuat değişikliği olursa ByGatech veritabanı düzeltmesiyle (migration +
    /// denetim) yazılır.
    /// </summary>
    public const decimal AklaYatkinKat = 10m;

    /// <summary>Onay penceresinde EK UYARI eşiği: yeni değer bugünkünün 2 katı ya da yarısı ve ötesi.</summary>
    public const decimal BuyukDegisimKat = 2m;

    private sealed record Durum(IReadOnlyList<YasalParametreSatiri> Satirlar, DateOnly YuklenenGun, DateTime YuklenmeUtc);

    private readonly Func<IReadOnlyList<YasalParametreSatiri>> yukleyici;
    private readonly SistemKapsami? kapsam;
    private readonly TimeProvider saat;
    private readonly object kilit = new();
    private volatile Durum? durum;

    /// <summary>DI kurucusu: tablo SistemKapsami ile okunur.</summary>
    public YasalParametreServisi(SistemKapsami kapsam, TimeProvider? saat = null)
    {
        this.kapsam = kapsam;
        this.saat = saat ?? TimeProvider.System;
        yukleyici = () =>
        {
            using var k = kapsam.Sistem();
            var db = k.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
            return db.YasalParametreler.AsNoTracking()
                .OrderBy(p => p.Anahtar).ThenBy(p => p.GecerlilikBaslangic)
                .AsEnumerable().Select(YasalParametreSatiri.Kaynaktan).ToList();
        };
    }

    private YasalParametreServisi(Func<IReadOnlyList<YasalParametreSatiri>> yukleyici, TimeProvider? saat)
    {
        this.yukleyici = yukleyici;
        this.saat = saat ?? TimeProvider.System;
    }

    /// <summary>Veritabanısız kurulum (saf testler): satırlar yükleyiciden gelir.</summary>
    public static YasalParametreServisi Yukleyicili(
        Func<IReadOnlyList<YasalParametreSatiri>> yukleyici, TimeProvider? saat = null) => new(yukleyici, saat);

    // ── Gün ─────────────────────────────────────────────────────────────────

    /// <summary>İşlem anının (UTC) YEREL günü — tevsik/MASAK'la aynı gün kuralı (K5).</summary>
    public static DateOnly IslemGunu(DateTime utc) => DateOnly.FromDateTime(Zaman.UtcdenYerele(utc));

    /// <summary>Servisin saatine göre yerel bugün.</summary>
    public DateOnly Bugun => IslemGunu(saat.GetUtcNow().UtcDateTime);

    /// <summary>PAKET YASAL-PARAM-2: servisin saatine göre şimdiki UTC an.</summary>
    public DateTime SimdiUtc => saat.GetUtcNow().UtcDateTime;

    // ── Önbellek ────────────────────────────────────────────────────────────

    /// <summary>Tabloyu yeniden okur (sistem ucu yazınca ve gece gün değişince).</summary>
    public void Tazele()
    {
        var satirlar = yukleyici();
        lock (kilit) durum = new Durum(satirlar, Bugun, saat.GetUtcNow().UtcDateTime);
    }

    private IReadOnlyList<YasalParametreSatiri> Satirlar()
    {
        var d = durum;
        // GECE TAZELEMESİ: yerel gün değiştiyse ilk erişim tabloyu yeniden okur —
        // başka bir örneğin/migration'ın eklediği satır yeniden başlatmasız görünür.
        if (d is null || d.YuklenenGun != Bugun)
        {
            Tazele();
            d = durum!;
        }
        return d.Satirlar;
    }

    /// <summary>Önbellek ölçüsü (sağlık ucu): satır sayısı ve yükleme anı.</summary>
    public (int Sayi, DateTime? YuklenmeUtc, DateOnly? YuklenenGun) OnbellekOlcusu()
    {
        var d = durum;
        return (d?.Satirlar.Count ?? 0, d?.YuklenmeUtc, d?.YuklenenGun);
    }

    // ── Okuma ───────────────────────────────────────────────────────────────

    /// <summary>O gün geçerli satır; yoksa null.</summary>
    public YasalParametreSatiri? SatirBul(string anahtar, DateOnly gun) =>
        Satirlar().FirstOrDefault(s => s.Anahtar == anahtar && s.GecerliMi(gun));

    /// <summary>O gün geçerli satır; yoksa FAIL-CLOSED (503).</summary>
    public YasalParametreSatiri Satir(string anahtar, DateOnly gun) =>
        SatirBul(anahtar, gun) ?? throw new YasalParametreYokException(anahtar, gun);

    /// <summary>O günün sayısal değeri; yoksa FAIL-CLOSED (503).</summary>
    public decimal Sayi(string anahtar, DateOnly gun) =>
        Satir(anahtar, gun).DegerSayi ?? throw new YasalParametreYokException(anahtar, gun);

    /// <summary>O günün metin değeri (boş olabilir — KAPALI satır); satır yoksa 503.</summary>
    public string? Metin(string anahtar, DateOnly gun) => Satir(anahtar, gun).DegerMetin;

    /// <summary>O gün geçerli TÜM satırlar (tatil satırları hariç — onlar ayrı listelenir).</summary>
    public IReadOnlyList<YasalParametreSatiri> Gecerliler(DateOnly gun) =>
        Satirlar().Where(s => s.GecerliMi(gun) && !YasalAnahtar.CokDegerli(s.Anahtar))
            .OrderBy(s => s.Anahtar, StringComparer.Ordinal).ToList();

    /// <summary>Bir anahtarın tarihçesi (yeniden eskiye).</summary>
    public IReadOnlyList<YasalParametreSatiri> Gecmis(string anahtar) =>
        Satirlar().Where(s => s.Anahtar == anahtar).OrderByDescending(s => s.Baslangic).ToList();

    /// <summary>
    /// YASAL-1 tur 3: <paramref name="gun"/>'den SONRA başlayacak TÜM satırlar (ufuksuz,
    /// tatil hariç, başlangıca göre artan). Ekran bunları geçmişte değil "Yaklaşan
    /// değişiklik" satırında gösterir.
    /// </summary>
    public IReadOnlyList<YasalParametreSatiri> Sonrakiler(DateOnly gun) =>
        Satirlar().Where(s => !YasalAnahtar.CokDegerli(s.Anahtar) && s.Baslangic > gun)
            .OrderBy(s => s.Baslangic).ThenBy(s => s.Anahtar, StringComparer.Ordinal).ToList();

    /// <summary>
    /// <paramref name="gun"/>'den SONRA ve en çok <paramref name="ufukGun"/> gün içinde
    /// başlayacak değişiklikler + o anahtarın bugünkü satırı (yoksa null).
    /// </summary>
    public IReadOnlyList<(YasalParametreSatiri Yeni, YasalParametreSatiri? Bugunku)> Yaklasan(DateOnly gun, int ufukGun)
    {
        var son = gun.AddDays(ufukGun);
        var liste = Satirlar();
        return liste
            .Where(s => !YasalAnahtar.CokDegerli(s.Anahtar) && s.Baslangic > gun && s.Baslangic <= son)
            .OrderBy(s => s.Baslangic)
            .Select(s => (s, liste.FirstOrDefault(b => b.Anahtar == s.Anahtar && b.GecerliMi(gun))))
            .ToList();
    }

    /// <summary>Tablodaki resmî tatil günleri (dinî bayramlar) — yıl bazında.</summary>
    public IReadOnlyList<DateOnly> Tatiller(int yil) =>
        Satirlar().Where(s => s.Anahtar == YasalAnahtar.ResmiTatil)
            .Select(s => DateOnly.TryParseExact(s.DegerMetin, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var g) ? g : (DateOnly?)null)
            .Where(g => g is { } x && x.Year == yil).Select(g => g!.Value).Distinct().OrderBy(g => g).ToList();

    /// <summary>
    /// Bu yılın dinî bayram günleri, SÜRE HESABININ OKUDUĞU kaynakta (yapılandırma
    /// <c>Takvim:ResmiTatiller</c>) var mı? Yoksa süreler (10 iş günü) 1–2 gün ERKEN
    /// hesaplanır.
    /// ⚠ YASAL-1 tur 2 (hasım ORTA-2): tablo satırı BU UYARIYI KAPATMAZ — MASAK süre
    ///   hesabı (MasakModulu → ResmiTatiller.Aralik) tabloyu YASAL-3'e kadar okumaz;
    ///   uyarı kalkıp hesap bayramı yine iş günü saymasın.
    /// </summary>
    public bool BayramEksik(int yil, TakvimAyarlari? ayarlar) => ResmiTatiller.DiniBayramEksik(yil, ayarlar);

    /// <summary>
    /// [bas, bit] (ikisi de DAHİL) aralığını kesen dönemler ve o dönemin değeri.
    /// Aralığın herhangi bir gününde değer yoksa FAIL-CLOSED (503) — boşluk
    /// "bugünkü değer"le doldurulmaz.
    /// </summary>
    public IReadOnlyList<(DateOnly Bas, DateOnly Bit, decimal Deger)> Donemler(string anahtar, DateOnly bas, DateOnly bit)
    {
        var sonuc = new List<(DateOnly, DateOnly, decimal)>();
        var gun = bas;
        while (gun <= bit)
        {
            var s = Satir(anahtar, gun);
            var deger = s.DegerSayi ?? throw new YasalParametreYokException(anahtar, gun);
            var donemSonu = s.Bitis is { } b && b.AddDays(-1) < bit ? b.AddDays(-1) : bit;
            // İlerleme güvencesi: bozuk aralık (bitiş ≤ başlangıç ya da örtüşme) sonsuz döngü doğurmasın.
            if (donemSonu < gun)
                throw new InvalidOperationException($"Yasal parametre dönemleri ilerlemiyor: {anahtar} {gun:yyyy-MM-dd}");
            sonuc.Add((gun, donemSonu, deger));
            gun = donemSonu.AddDays(1);
        }
        return sonuc;
    }

    /// <summary>
    /// PAKET YASAL-PARAM-2 — AN ÇÖZÜNÜRLÜKLÜ dönemler: [basUtc, bitUtc] aralığını kesen
    /// [BasUtc, BitUtc) dönemleri ve değerleri. Sınır, satırın <see cref="YasalParametreSatiri.EtkinBaslangicUtc"/>
    /// anıdır: bugünden geçerli değişiklik öğlen yazıldıysa o sabahki işlem ÖNCEKİ değerle
    /// değerlendirilir; aynı gün düzeltilen yanlış değer yalnız [yazıldığı an, düzeltildiği an)
    /// aralığını kapsar. Aralıkta değer olmayan bir an varsa FAIL-CLOSED (503).
    /// </summary>
    public IReadOnlyList<(DateTime BasUtc, DateTime BitUtc, decimal Deger)> DonemlerAn(
        string anahtar, DateTime basUtc, DateTime bitUtc)
    {
        var satirlar = Satirlar().Where(s => s.Anahtar == anahtar)
            .OrderBy(s => s.EtkinBaslangicUtc).ToList();
        // Her satırın [etkin başlangıç, etkin bitiş): bitiş, kapanış günü bir sonraki satırın
        // başlangıç günüyse o satırın ANIDIR (aynı gün değişiklik), değilse kapanış gününün 00:00'ı.
        var araliklar = new List<(DateTime Bas, DateTime Bit, YasalParametreSatiri S)>();
        for (var i = 0; i < satirlar.Count; i++)
        {
            var s = satirlar[i];
            var sonraki = i + 1 < satirlar.Count ? satirlar[i + 1] : null;
            var bit = s.Bitis is not { } b ? DateTime.MaxValue
                : sonraki is not null && sonraki.Baslangic == b ? sonraki.EtkinBaslangicUtc
                : Zaman.YerelGunBasiUtc(b);
            if (bit > s.EtkinBaslangicUtc) araliklar.Add((s.EtkinBaslangicUtc, bit, s));
        }
        var sonuc = new List<(DateTime, DateTime, decimal)>();
        var an = basUtc;
        while (an <= bitUtc)
        {
            var kapsayan = araliklar.FirstOrDefault(a => a.Bas <= an && an < a.Bit);
            if (kapsayan.S is null) throw new YasalParametreYokException(anahtar, IslemGunu(an));
            var deger = kapsayan.S.DegerSayi ?? throw new YasalParametreYokException(anahtar, IslemGunu(an));
            sonuc.Add((kapsayan.Bas, kapsayan.Bit, deger));
            if (kapsayan.Bit == DateTime.MaxValue) break;
            an = kapsayan.Bit;
        }
        return sonuc;
    }

    /// <summary>
    /// PAKET YASAL-PARAM-2: satır AYNI GÜN düzeltilebilir mi — bugün, bugünden geçerli
    /// (anlı) yazılmış ve hâlâ yürürlükte (açık uçlu). Tohum ve ileri tarihli satır değil.
    /// </summary>
    public bool AyniGunDuzeltilebilir(YasalParametreSatiri s) =>
        s.BaslangicAni is { } a && IslemGunu(DateTime.SpecifyKind(a, DateTimeKind.Utc)) == Bugun
        && s.Baslangic == Bugun && s.Bitis is null && !YasalAnahtar.CokDegerli(s.Anahtar);

    /// <summary>
    /// K-7 AÇILIŞ EŞİTLİK DENETİMİ: appsettings'teki değer ile tablonun BUGÜNKÜ
    /// değeri arasındaki farklar. Hangisinin sunucuyu durdurduğuna
    /// <see cref="AcilisDegerlendir"/> karar verir (YASAL-PARAM-EKRAN: tabloda okunan
    /// anahtarda tablo esas, yalnız uyarı). Tabloda bugün için satır YOKSA fark
    /// sayılmaz (o anahtar işlem anında 503 verir).
    /// </summary>
    public IReadOnlyList<string> AcilisFarklari(IReadOnlyDictionary<string, decimal> yapilandirma)
    {
        var bugun = Bugun;
        var farklar = new List<string>();
        foreach (var (anahtar, deger) in yapilandirma)
        {
            var s = SatirBul(anahtar, bugun);
            if (s?.DegerSayi is { } tablo && tablo != deger)
                farklar.Add($"{anahtar}: yapılandırma {deger.ToString(CultureInfo.InvariantCulture)} ≠ tablo {tablo.ToString(CultureInfo.InvariantCulture)} ({bugun:yyyy-MM-dd})");
        }
        return farklar;
    }

    /// <summary>
    /// PAKET YASAL-PARAM-EKRAN — TEK KAYNAK: açılış eşitlik denetiminin kararı.
    /// <list type="bullet">
    /// <item><b>Engel</b>: kodun HÂLÂ YAPILANDIRMADAN okuduğu anahtar (gider pusulası, stopaj,
    ///   KDV oranı …) tablodan farklıysa iki kaynak gerçekten ayrışmıştır — program
    ///   yapılandırmadaki rakamı UYGULAR, ekran tablodakini gösterir. Sunucu AÇILMAZ.</item>
    /// <item><b>Uyarı</b>: işlem anında TABLODAN okunan anahtar (<see cref="YasalAnahtar.TablodanOkunan"/>:
    ///   MASAK eşikleri, tevsik haddi …) için tablo ESASTIR; yapılandırmadaki kopya yalnız
    ///   veritabanısız kurulumun varsayılanıdır. Fark günlüğe yazılır, sunucu AÇILIR —
    ///   ekrandan bugünden geçerli değişiklik yapılınca sunucu yanlış yapılandırma yüzünden
    ///   açılmaz duruma DÜŞMEZ. Tablo okunamazsa kapılar işlem anında 503 verir (fail-closed).</item>
    /// </list>
    /// </summary>
    public (IReadOnlyList<string> Engel, IReadOnlyList<string> Uyari) AcilisDegerlendir(
        IReadOnlyDictionary<string, decimal> yapilandirma)
    {
        var engel = new List<string>();
        var uyari = new List<string>();
        foreach (var (anahtar, deger) in yapilandirma)
        {
            var fark = AcilisFarklari(new Dictionary<string, decimal>(StringComparer.Ordinal) { [anahtar] = deger });
            if (fark.Count == 0) continue;
            (YasalAnahtar.TablodanOkunan.Contains(anahtar) ? uyari : engel).AddRange(fark);
        }
        return (engel, uyari);
    }

    // ── Yazma (yalnız sistem anahtarlı uç) ──────────────────────────────────

    /// <summary>Resmî Gazete sayısı: rakamla başlar (ör. "33393", "33393 mükerrer").</summary>
    private static readonly System.Text.RegularExpressions.Regex RgSayiDeseni =
        new(@"^[0-9]{3,6}(\s+[0-9A-Za-zÇĞİÖŞÜçğıöşü.()]+)*$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>
    /// PAKET YASAL-PARAM-EKRAN — değişikliğin yönü. Eşik/had ve süre anahtarlarında
    /// ARTIŞ gevşetir (daha az işlem kapıya takılır, daha uzun süre); saklama yılında
    /// artış sıkılaştırır. Oran ve kod anahtarlarında yön söylenmez (DEGISIKLIK).
    /// </summary>
    public static string Yon(string anahtar, decimal? eski, decimal? yeni)
    {
        if (eski is not { } e || yeni is not { } y) return "DEGISIKLIK";
        if (e == y) return "AYNI";
        var artisGevsetir = anahtar is YasalAnahtar.TevsikHaddiTl or YasalAnahtar.MasakKimlikEsigiTl
            or YasalAnahtar.MasakAmacBeyaniEsigiTl or YasalAnahtar.MasakBeyanFormuEsigiTl
            or YasalAnahtar.MasakEkBelgeEsigiTl or YasalAnahtar.FaturaHaddiTl
            or YasalAnahtar.MasakSibIsGunu or YasalAnahtar.TevsikBildirimIsGunu or YasalAnahtar.GiderPusulasiGun;
        var artisSikilastirir = anahtar is YasalAnahtar.MasakSaklamaYil;
        if (!artisGevsetir && !artisSikilastirir) return "DEGISIKLIK";
        var artis = y > e;
        return artis == artisGevsetir ? "GEVSETME" : "SIKILASTIRMA";
    }

    /// <summary>Akla yatkınlık bandı: yeni &gt; 0 ve eski/kat ≤ yeni ≤ eski×kat (eski &gt; 0 iken).</summary>
    public static bool AklaYatkin(decimal eski, decimal yeni) =>
        yeni > 0m && yeni <= eski * AklaYatkinKat && yeni * AklaYatkinKat >= eski;

    /// <summary>Onay penceresi ek uyarısı: değer en az 2 kat büyüyor ya da yarıya iniyor.</summary>
    public static bool BuyukDegisim(decimal? eski, decimal? yeni) =>
        eski is { } e && yeni is { } y && e > 0m && (y >= e * BuyukDegisimKat || y * BuyukDegisimKat <= e);

    /// <summary>Doğrulanmış yazma planı (önizleme ile kayıt AYNI plandan geçer).</summary>
    private sealed record Plan(
        string Anahtar, string Birim, string Teyit, DateOnly Baslangic, decimal? DegerSayi, string? DegerMetin,
        string Dayanak, string AciklamaSade, string KaynakUrl, DateOnly RgTarihi, string RgSayisi,
        string Gerekce, string Operator, YasalParametre? Kapanan, YasalParametreSatiri? Eski, string? IcNot)
    {
        public bool BugunMu { get; init; }
    }

    /// <summary>
    /// İKİNCİ ONAY ADIMI kodu: önizlenen içerik + o anahtarın o günkü satırı. Gizli değildir
    /// (yetki sistem anahtarıdır); amacı "onaylanan ile yazılan aynı mı" sorusudur — önizlemeden
    /// sonra içerik ya da eski değer değişirse kod tutmaz (409).
    /// </summary>
    private static string OnayKoduHesapla(Plan p)
    {
        var ham = string.Join("|", p.Anahtar,
            p.DegerSayi?.ToString(CultureInfo.InvariantCulture) ?? "", p.DegerMetin ?? "",
            p.Baslangic.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            p.RgTarihi.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), p.RgSayisi,
            p.Eski?.Id ?? "", p.Eski?.DegerSayi?.ToString(CultureInfo.InvariantCulture) ?? "", p.Teyit);
        var ozet = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(ham));
        return Convert.ToHexString(ozet)[..16];
    }

    /// <summary>Önizleme ve kayıt için ORTAK doğrulama (fail-closed; her kural elle).</summary>
    private Plan Dogrula(YasalParametreEkleDto dto, string operatorAdi, List<YasalParametre> ayni)
    {
        // DataAnnotations yalnız controller yolunda koşar → servis içinde elle kontrol.
        var anahtar = dto.Anahtar?.Trim() ?? "";
        if (!YasalAnahtar.Birimler.TryGetValue(anahtar, out var birim))
            throw new BadRequestException("Anahtar tanımlı yasal parametreler listesinde değil.").AlanIle("anahtar");
        // YASAL-1 tur 2 (hasım ORTA-3): kodun hâlâ YAPILANDIRMADAN okuduğu anahtara
        // (gider pusulası/stopaj/KDV → YASAL-4, resmî tatil → YASAL-3) satır yazılırsa
        // satır gününde UYGULANMAZ ve ilk yeniden başlatmada K-7 eşitlik denetimi
        // sunucuyu açmaz → yazma reddedilir (fail-closed).
        if (!YasalAnahtar.TablodanOkunan.Contains(anahtar))
            throw new ConflictException(
                "Bu rakamı program henüz tarihli okumuyor; yeni değer gününde uygulanmaz. "
                + "Değişiklik yapılandırma ve sürümle yapılır.");
        var teyit = string.IsNullOrWhiteSpace(dto.Teyit) ? YasalTeyit.Dogrulandi : dto.Teyit.Trim();
        if (!YasalTeyit.Hepsi.Contains(teyit))
            throw new BadRequestException("Teyit damgası DOGRULANDI, TEYITSIZ ya da KAPALI olmalıdır.").AlanIle("teyit");
        if (dto.Baslangic is not { } bas)
            throw new BadRequestException("Başlangıç günü zorunludur.").AlanIle("baslangic");

        // ⚠ GEÇMİŞ YENİDEN YAZILAMAZ: başlangıç bugünden önce olamaz (400). Bugün başlayan
        //   değer YAZILABİLİR (PAKET YASAL-PARAM-EKRAN, kurucu kararı 07.10): o andan sonraki
        //   işlemler yeni değerle değerlendirilir; daha önce kapanmış işlemler kapanışta
        //   okunan değerle kayıtlıdır ve yeniden açılmaz. Geçmiş düzeltmesi yalnız migration
        //   ve denetim kaydıyla yapılır.
        var bugun = Bugun;
        if (bas < bugun)
            throw new BadRequestException(
                "Geçmiş tarihli yasal değer yazılamaz; başlangıç bugün ya da ileri bir gün olmalıdır. "
                + "Geçmiş düzeltmesi migration ve denetim kaydıyla yapılır.").AlanIle("baslangic");

        // 08.10 (YASAL-PARAM-EKRAN kapanış hasımı, KRİTİK): bu uç yalnız işlem anında TABLODAN
        // okunan anahtarlara yazar. KAPALI damgalı (değersiz) satır o anahtarı okunamaz kılar
        // (Sayi → 503) ve tek kayıtla bütün kiracılarda kapı durur. Kural kaldırma bu uçtan
        // yapılmaz; bilinçli migration + denetim kaydı ister.
        if (teyit == YasalTeyit.Kapali)
            throw new BadRequestException(
                "Program bu sınırı işlem anında okuyor; değersiz (kapalı) satır yazılamaz. "
                + "Kuralın kaldırılması ayrı bir güncellemeyle yapılır.").AlanIle("teyit");

        var sayisal = birim is YasalBirim.Tl or YasalBirim.Gun or YasalBirim.IsGunu or YasalBirim.Yil or YasalBirim.Oran
            or YasalBirim.AyinGunu;
        if (sayisal && teyit != YasalTeyit.Kapali && (dto.DegerSayi is not { } d || d < 0m))
            throw new BadRequestException("Bu sınır sıfır ya da pozitif bir sayı ister.").AlanIle("degerSayi");
        if (!sayisal && teyit != YasalTeyit.Kapali && string.IsNullOrWhiteSpace(dto.DegerMetin))
            throw new BadRequestException("Bu sınır metin değeri ister.").AlanIle("degerMetin");
        if (anahtar == YasalAnahtar.ResmiTatil
            && !DateOnly.TryParseExact(dto.DegerMetin?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            throw new BadRequestException("Tatil günü YYYY-MM-DD biçiminde yazılmalıdır.").AlanIle("degerMetin");

        // Resmî Gazete künyesi ZORUNLU (birincil kaynak kuralı).
        if (dto.ResmiGazeteTarihi is not { } rgTarihi)
            throw new BadRequestException("Resmî Gazete tarihi zorunludur.").AlanIle("resmiGazeteTarihi");
        if (rgTarihi > bugun)
            throw new BadRequestException("Resmî Gazete tarihi ileri bir gün olamaz; yayımlanmış kaynak girilir.")
                .AlanIle("resmiGazeteTarihi");
        if (rgTarihi > bas)
            throw new BadRequestException("Değer, yayımlandığı Resmî Gazete tarihinden önce başlayamaz.")
                .AlanIle("baslangic");
        var rgSayisi = dto.ResmiGazeteSayisi?.Trim() ?? "";
        if (rgSayisi.Length == 0)
            throw new BadRequestException("Resmî Gazete sayısı zorunludur.").AlanIle("resmiGazeteSayisi");
        if (rgSayisi.Length > 20 || !RgSayiDeseni.IsMatch(rgSayisi))
            throw new BadRequestException("Resmî Gazete sayısı rakamla yazılmalıdır (ör. 33393).").AlanIle("resmiGazeteSayisi");

        var url = dto.KaynakUrl?.Trim() ?? "";
        if (!url.StartsWith("https://", StringComparison.Ordinal) || url.Length > 500)
            throw new BadRequestException("Kaynak adresi https:// ile başlayan birincil kaynak olmalıdır.").AlanIle("kaynakUrl");
        if (string.IsNullOrWhiteSpace(dto.Gerekce) || dto.Gerekce.Trim().Length < GerekceAsgari)
            throw new BadRequestException($"Gerekçe en az {GerekceAsgari} karakter olmalıdır.").AlanIle("gerekce");
        if (string.IsNullOrWhiteSpace(operatorAdi))
            throw new BadRequestException($"{Modules.Sistem.SistemAnahtariKapisi.OperatorBasligi} başlığı zorunludur");


        // UX-S21: dayanak ve sade açıklama boşsa o anahtarın bugünkü satırından türetilir.
        var eski = SatirBul(anahtar, bas) ?? SatirBul(anahtar, bugun);
        var dayanak = string.IsNullOrWhiteSpace(dto.Dayanak) ? eski?.Dayanak ?? "" : dto.Dayanak.Trim();
        if (dayanak.Length == 0 || dayanak.Length > 300)
            throw new BadRequestException("Dayanak zorunludur (en çok 300 karakter).").AlanIle("dayanak");
        var sade = string.IsNullOrWhiteSpace(dto.AciklamaSade) ? eski?.AciklamaSade ?? "" : dto.AciklamaSade.Trim();
        if (sade.Length == 0 || sade.Length > 300)
            throw new BadRequestException("Sade açıklama zorunludur (en çok 300 karakter).").AlanIle("aciklamaSade");

        YasalParametre? kapanan = null;
        if (!YasalAnahtar.CokDegerli(anahtar))
        {
            // ARALIK ÇAKIŞMASI: yeni başlangıçta (ya da sonra) başlayan satır varsa ekleme sırayı bozar → 409.
            // PAKET YASAL-PARAM-2: bugün zaten bugünden geçerli bir değer yazıldıysa ikinci "yeni değer"
            // yazılmaz — yanlış giriş AYNI GÜN DÜZELTME yolundan (yalnız giren operatör, gerekçeli) düzeltilir.
            if (bas == bugun && ayni.Any(p => p.GecerlilikBaslangic == bas && p.GecerlilikBaslangicAni != null))
                throw new ConflictException(
                    "Bu sınıra bugün zaten bir değer yazıldı; yanlışsa \"Bugün girilen değeri düzelt\" bölümünden düzeltin.");
            if (ayni.Any(p => p.GecerlilikBaslangic >= bas))
                throw new ConflictException("Bu sınırda aynı ya da daha ileri tarihli bir değer zaten var.");
            kapanan = ayni.FirstOrDefault(p => p.GecerlilikBaslangic < bas
                                            && (p.GecerlilikBitis is null || p.GecerlilikBitis > bas));
            if (kapanan is not null && kapanan.GecerlilikBitis is not null)
                throw new ConflictException("Kapalı bir dönemin ortasına değer eklenemez.");
        }
        else if (ayni.Any(p => p.DegerMetin == dto.DegerMetin!.Trim()))
            throw new ConflictException("Bu tatil günü zaten kayıtlı.");

        // TUR 2 (hasım DÜŞÜK): aynı değer yeni satır olarak yazılmaz (409) ve yeni değer akla
        // yatkınlık bandında olmalı (400) — yazım hatası RG künyesiyle kalıcılaşmasın.
        if (sayisal && dto.DegerSayi is { } yeniD && eski?.DegerSayi is { } eskiD)
        {
            if (yeniD == eskiD)
                throw new ConflictException(
                    "Yeni değer bugünkü değerle aynı; aynı değer yeni satır olarak yazılmaz.").AlanIle("degerSayi");
            if (eskiD > 0m && !AklaYatkin(eskiD, yeniD))
                throw new BadRequestException(
                    $"Yeni değer bugünkü değerin {AklaYatkinKat:0} katından fazla değişemez (sıfır da olamaz); "
                    + "Resmî Gazete'deki rakamı kontrol edin. Böyle bir değişiklik ByGatech veritabanı düzeltmesiyle yazılır.")
                    .AlanIle("degerSayi");
        }

        return new Plan(anahtar, birim, teyit, bas, sayisal ? dto.DegerSayi : null,
            string.IsNullOrWhiteSpace(dto.DegerMetin) ? null : dto.DegerMetin.Trim(),
            dayanak, sade, url, rgTarihi, rgSayisi, dto.Gerekce.Trim(), operatorAdi.Trim(), kapanan, eski,
            string.IsNullOrWhiteSpace(dto.IcNot) ? null : dto.IcNot.Trim()[..Math.Min(300, dto.IcNot.Trim().Length)])
        { BugunMu = bas == bugun };
    }

    /// <summary>
    /// PAKET YASAL-PARAM-EKRAN — "Etkilenecek açık işlem": tutarı eski ile yeni değer
    /// ARASINDA kalan AÇIK (teslim edilmemiş, iptal edilmemiş) siparişlerin sayısı — tüm
    /// kuyumcular, YALNIZ SAYI (kişisel veri yok). Bunlar kapanırken yeni değerle
    /// değerlendirilir (mevzuat belgesi § 5: "önce açılıp sonra kapanan işlemler").
    /// Yalnız TL sınırlarında sayılır; diğerlerinde null ("sayılmaz").
    /// SistemKapsami izin listesi madde 9 (YASAL-PARAM-EKRAN eki).
    /// </summary>
    private static async Task<int?> AcikIslemSayisi(KuyumcuDbContext db, Plan p)
    {
        if (p.Birim != YasalBirim.Tl || p.Eski?.DegerSayi is not { } eski || p.DegerSayi is not { } yeni || eski == yeni)
            return null;
        var alt = Math.Min(eski, yeni);
        var ust = Math.Max(eski, yeni);
        return await db.Siparisler.AsNoTracking()
            .Where(s => SiparisDurumu.Acik.Contains(s.Durum)
                     && s.TahminiTutarTl != null && s.TahminiTutarTl >= alt && s.TahminiTutarTl < ust)
            .CountAsync();
    }

    /// <summary>
    /// POST /api/v1/sistem/yasal-parametreler/onizleme — HİÇBİR ŞEY YAZMAZ. Kayıtla AYNI
    /// doğrulamadan geçer ve onay penceresinin içeriğini döner: eski → yeni değer, geçerlilik
    /// günü, yön (sıkılaştırma/gevşetme), etkilenecek açık işlem sayısı ve onay kodu.
    /// </summary>
    public async Task<object> Onizle(YasalParametreEkleDto dto, string operatorAdi)
    {
        if (kapsam is null) throw new InvalidOperationException("Veritabanısız servis önizleyemez.");
        await using var k = kapsam.Sistem();
        var db = k.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
        var anahtar = dto.Anahtar?.Trim() ?? "";
        var ayni = await db.YasalParametreler.AsNoTracking().Where(p => p.Anahtar == anahtar).ToListAsync();
        var plan = Dogrula(dto, operatorAdi, ayni);
        return new
        {
            anahtar = plan.Anahtar,
            birim = plan.Birim,
            eskiDeger = plan.Eski?.DegerSayi,
            eskiMetin = plan.Eski?.DegerMetin,
            eskiBaslangic = plan.Eski is { } e ? e.Baslangic.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null,
            yeniDeger = plan.DegerSayi,
            yeniMetin = plan.DegerMetin,
            baslangic = plan.Baslangic.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            bugunMu = plan.BugunMu,
            yon = Yon(plan.Anahtar, plan.Eski?.DegerSayi, plan.DegerSayi),
            acikIslemSayisi = await AcikIslemSayisi(db, plan),
            resmiGazeteTarihi = plan.RgTarihi.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            resmiGazeteSayisi = plan.RgSayisi,
            teyit = plan.Teyit,
            dayanak = plan.Dayanak,
            onayKodu = OnayKoduHesapla(plan),
            buyukDegisim = BuyukDegisim(plan.Eski?.DegerSayi, plan.DegerSayi),
        };
    }

    /// <summary>
    /// Yeni değer ekler (BUGÜN ya da ileri tarih); aynı anahtarın açık satırını yeni
    /// başlangıçta KAPATIR. Geçmiş 400. Resmî Gazete künyesi, ≥20 karakter gerekçe ve
    /// önizlemenin onay kodu ZORUNLU. Denetim izi: kim, ne zaman, eski/yeni, kaynak.
    /// </summary>
    public async Task<object> Ekle(YasalParametreEkleDto dto, string operatorAdi)
    {
        if (kapsam is null) throw new InvalidOperationException("Veritabanısız servis yazamaz.");

        var simdi = saat.GetUtcNow().UtcDateTime;
        await using var k = kapsam.Sistem();
        var db = k.ServiceProvider.GetRequiredService<KuyumcuDbContext>();

        var anahtar = dto.Anahtar?.Trim() ?? "";
        var ayni = await db.YasalParametreler.Where(p => p.Anahtar == anahtar).ToListAsync();
        var plan = Dogrula(dto, operatorAdi, ayni);

        // İKİNCİ ONAY ADIMI — önizlemesiz kayıt yok; önizlenen ile yazılan aynı olmalı.
        var onay = dto.OnayKodu?.Trim() ?? "";
        if (onay.Length == 0)
            throw new BadRequestException("Önce önizleyin ve onaylayın: onay kodu eksik.").AlanIle("onayKodu");
        if (!string.Equals(onay, OnayKoduHesapla(plan), StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Önizlemeden sonra değer ya da bugünkü satır değişti; yeniden önizleyin.");

        // Append-only: yalnız bitiş alanı BİR KEZ doldurulur.
        if (plan.Kapanan is not null) plan.Kapanan.GecerlilikBitis = plan.Baslangic;

        var satir = new YasalParametre
        {
            Id = Kimlik.Ulid(),
            Anahtar = plan.Anahtar,
            DegerSayi = plan.DegerSayi,
            DegerMetin = plan.DegerMetin,
            Birim = plan.Birim,
            GecerlilikBaslangic = plan.Baslangic,
            // PAKET YASAL-PARAM-2: bugünden geçerli satır KAYIT ANINDAN itibaren geçerlidir —
            // aynı sabahki işlem önceki değerle değerlendirilir (risk dönemleri an sınırlı).
            GecerlilikBaslangicAni = plan.BugunMu && !YasalAnahtar.CokDegerli(plan.Anahtar) ? simdi : null,
            GecerlilikBitis = YasalAnahtar.CokDegerli(plan.Anahtar) ? plan.Baslangic.AddDays(1) : null,
            Dayanak = plan.Dayanak,
            IcNot = plan.IcNot,
            KaynakUrl = plan.KaynakUrl,
            Teyit = plan.Teyit,
            AciklamaSade = plan.AciklamaSade,
            ResmiGazeteTarihi = plan.RgTarihi,
            ResmiGazeteSayisi = plan.RgSayisi,
            Olusturan = Kirp("sistem:" + plan.Operator, 60),
            Olusturuldu = simdi,
        };
        db.YasalParametreler.Add(satir);

        var yon = Yon(plan.Anahtar, plan.Eski?.DegerSayi, plan.DegerSayi);
        string Deger(decimal? s, string? m) => s?.ToString(CultureInfo.InvariantCulture) ?? m ?? "-";
        k.ServiceProvider.GetRequiredService<DenetimServisi>().SistemYazmaKaydet(
            Modules.Sistem.SistemServisi.SistemTenantId, "YasalParametre", satir.Id,
            $"yasal parametre eklendi · {plan.Anahtar} · eski {Deger(plan.Eski?.DegerSayi, plan.Eski?.DegerMetin)}"
            + $" → yeni {Deger(satir.DegerSayi, satir.DegerMetin)} · başlangıç {plan.Baslangic:yyyy-MM-dd}"
            + (plan.BugunMu ? $" (BUGÜNDEN GEÇERLİ, {Zaman.UtcdenYerele(simdi):HH:mm:ss} itibarıyla)" : "") + $" · yön {yon} · teyit {plan.Teyit}"
            + $" · operatör: {plan.Operator} · kaynak RG {plan.RgTarihi:yyyy-MM-dd}/{plan.RgSayisi}"
            + (plan.Kapanan is null ? "" : $" · önceki satır {plan.Kapanan.Id} {plan.Baslangic:yyyy-MM-dd} itibarıyla kapandı")
            // Detay 1000 karakterde kırpılır: kim/ne/eski/yeni/kaynak künyesi ÖNCE, uzun alanlar sonda.
            + $" · gerekçe: {plan.Gerekce} · kaynak adresi {plan.KaynakUrl}");

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // UNIQUE (anahtar, başlangıç) ya da açık uçlu satır indeksi — eşzamanlı yazım.
            throw new ConflictException("Bu sınırda çakışan bir değer var; kayıt yazılmadı.");
        }

        // YENİDEN BAŞLATMASIZ: önbellek yazımdan hemen sonra tazelenir — bugün başlayan
        // değer bir sonraki işlemde okunur.
        Tazele();

        return new
        {
            id = satir.Id,
            anahtar = plan.Anahtar,
            degerSayi = satir.DegerSayi,
            degerMetin = satir.DegerMetin,
            eskiDeger = plan.Eski?.DegerSayi,
            birim = plan.Birim,
            baslangic = plan.Baslangic.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            bugunMu = plan.BugunMu,
            yon,
            teyit = plan.Teyit,
            resmiGazeteTarihi = plan.RgTarihi.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            resmiGazeteSayisi = plan.RgSayisi,
            kapananSatirId = plan.Kapanan?.Id,
        };
    }

    // ── PAKET YASAL-PARAM-2 — AYNI GÜN DÜZELTME ──────────────────────────────

    /// <summary>Düzeltme planı (önizleme ile kayıt AYNI doğrulamadan geçer).</summary>
    private sealed record DuzeltPlan(YasalParametre Yanlis, decimal Yeni, string Gerekce, string Operator, decimal? Onceki);

    private static string DuzeltOnayKodu(DuzeltPlan p)
    {
        var ham = string.Join("|", "DUZELT", p.Yanlis.Id, p.Yanlis.Anahtar,
            p.Yanlis.DegerSayi?.ToString(CultureInfo.InvariantCulture) ?? "",
            p.Yeni.ToString(CultureInfo.InvariantCulture),
            p.Yanlis.GecerlilikBitis?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "");
        var ozet = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(ham));
        return Convert.ToHexString(ozet)[..16];
    }

    /// <summary>
    /// Ortak doğrulama (fail-closed, her kural elle). Sıra: satır var mı (404) → aynı gün düzeltilebilir
    /// mi (409) → aynı operatör mü (403) → gerekçe (400) → değer (400/409).
    /// </summary>
    private DuzeltPlan DuzeltDogrula(string id, YasalParametreDuzeltDto dto, string operatorAdi, List<YasalParametre> ayni)
    {
        if (string.IsNullOrWhiteSpace(operatorAdi))
            throw new BadRequestException($"{Modules.Sistem.SistemAnahtariKapisi.OperatorBasligi} başlığı zorunludur");
        var yanlis = ayni.FirstOrDefault(p => p.Id == id)
            ?? throw new NotFoundException("Yasal değer bulunamadı.");
        var bugun = Bugun;
        if (yanlis.GecerlilikBaslangicAni is not { } ani
            || IslemGunu(DateTime.SpecifyKind(ani, DateTimeKind.Utc)) != bugun
            || yanlis.GecerlilikBaslangic != bugun
            || yanlis.GecerlilikBitis is not null
            || YasalAnahtar.CokDegerli(yanlis.Anahtar))
            throw new ConflictException(
                "Yalnız bugün girilen, bugünden geçerli ve hâlâ yürürlükteki değer aynı gün düzeltilebilir. "
                + "Diğer değişiklikler yeni bir değer olarak (bugün ya da ileri tarihli) girilir.");
        // YALNIZ AYNI OPERATÖR: değeri giren kişi kendi yanlışını düzeltir; başka yönetici yeni değer girer.
        if (!string.Equals(yanlis.Olusturan, Kirp("sistem:" + operatorAdi.Trim(), 60), StringComparison.Ordinal))
            throw new ForbiddenException("Bu değeri başka bir yönetici girdi; aynı gün yalnız giren kişi düzeltebilir.");
        var gerekce = dto.Gerekce?.Trim() ?? "";
        if (gerekce.Length < GerekceAsgari)
            throw new BadRequestException($"Gerekçe en az {GerekceAsgari} karakter olmalıdır.").AlanIle("gerekce");
        if (dto.DegerSayi is not { } yeni || yeni < 0m || yanlis.DegerSayi is null)
            throw new BadRequestException("Bu sınır sıfır ya da pozitif bir sayı ister.").AlanIle("degerSayi");
        if (yeni == yanlis.DegerSayi)
            throw new ConflictException("Doğru değer, düzeltilecek değerle aynı; düzeltme yazılmaz.").AlanIle("degerSayi");
        // Akla yatkınlık: bugünden ÖNCEKİ (yanlış girişin kapattığı) değere göre — yazım hatası kalıcılaşmasın.
        var onceki = ayni.Where(p => p.Id != yanlis.Id && p.GecerlilikBaslangic < bugun && p.GecerlilikBitis == bugun)
            .Select(p => p.DegerSayi).FirstOrDefault();
        if (onceki is { } o && o > 0m && !AklaYatkin(o, yeni))
            throw new BadRequestException(
                $"Doğru değer önceki değerin {AklaYatkinKat:0} katından fazla değişemez (sıfır da olamaz); "
                + "Resmî Gazete'deki rakamı kontrol edin.").AlanIle("degerSayi");
        return new DuzeltPlan(yanlis, yeni, gerekce, operatorAdi.Trim(), onceki);
    }

    private static string Saat(DateTime utc) =>
        Zaman.UtcdenYerele(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToString("HH:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>
    /// POST /api/v1/sistem/yasal-parametreler/{id}/duzeltme/onizleme — HİÇBİR ŞEY YAZMAZ; onay
    /// penceresinin içeriği (yanlış → doğru, yanlışın yürürlüğe girdiği saat) ve onay kodu.
    /// </summary>
    public async Task<object> DuzeltOnizle(string id, YasalParametreDuzeltDto dto, string operatorAdi)
    {
        if (kapsam is null) throw new InvalidOperationException("Veritabanısız servis önizleyemez.");
        await using var k = kapsam.Sistem();
        var db = k.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
        var anahtar = await db.YasalParametreler.AsNoTracking().Where(p => p.Id == id).Select(p => p.Anahtar)
            .FirstOrDefaultAsync() ?? throw new NotFoundException("Yasal değer bulunamadı.");
        var ayni = await db.YasalParametreler.AsNoTracking().Where(p => p.Anahtar == anahtar).ToListAsync();
        var plan = DuzeltDogrula(id, dto, operatorAdi, ayni);
        return new
        {
            id = plan.Yanlis.Id,
            anahtar = plan.Yanlis.Anahtar,
            birim = plan.Yanlis.Birim,
            yanlisDeger = plan.Yanlis.DegerSayi,
            yeniDeger = plan.Yeni,
            oncekiDeger = plan.Onceki,
            yanlisSaati = Saat(plan.Yanlis.GecerlilikBaslangicAni!.Value),
            onayKodu = DuzeltOnayKodu(plan),
            buyukDegisim = BuyukDegisim(plan.Yanlis.DegerSayi, plan.Yeni),
        };
    }

    /// <summary>
    /// POST /api/v1/sistem/yasal-parametreler/{id}/duzeltme — bugünden geçerli YANLIŞ değeri aynı gün
    /// düzeltir. APPEND-ONLY: yanlış satır silinmez/değiştirilmez, yalnız bitişi (bugün) BİR KEZ dolar;
    /// doğru değer yeni satır olarak DÜZELTME ANINDAN geçerlidir. Yanlış değerin geçerli olduğu
    /// [yazıldığı an, düzeltme anı) aralığındaki işlemler o değerle kayıtlı kalır (geçmişe etki yok).
    /// Denetim izi: kim, yanlış/doğru, iki an, gerekçe.
    /// </summary>
    public async Task<object> Duzelt(string id, YasalParametreDuzeltDto dto, string operatorAdi)
    {
        if (kapsam is null) throw new InvalidOperationException("Veritabanısız servis yazamaz.");
        var simdi = saat.GetUtcNow().UtcDateTime;
        await using var k = kapsam.Sistem();
        var db = k.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
        var anahtar = await db.YasalParametreler.AsNoTracking().Where(p => p.Id == id).Select(p => p.Anahtar)
            .FirstOrDefaultAsync() ?? throw new NotFoundException("Yasal değer bulunamadı.");
        var ayni = await db.YasalParametreler.Where(p => p.Anahtar == anahtar).ToListAsync();
        var plan = DuzeltDogrula(id, dto, operatorAdi, ayni);

        var onay = dto.OnayKodu?.Trim() ?? "";
        if (onay.Length == 0)
            throw new BadRequestException("Önce önizleyin ve onaylayın: onay kodu eksik.").AlanIle("onayKodu");
        if (!string.Equals(onay, DuzeltOnayKodu(plan), StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Önizlemeden sonra değer değişti; yeniden önizleyin.");

        var yanlis = plan.Yanlis;
        var yanlisAni = yanlis.GecerlilikBaslangicAni!.Value;
        yanlis.GecerlilikBitis = yanlis.GecerlilikBaslangic;   // tek değişen alan, bir kez
        var dogru = new YasalParametre
        {
            Id = Kimlik.Ulid(),
            Anahtar = yanlis.Anahtar,
            DegerSayi = plan.Yeni,
            DegerMetin = null,
            Birim = yanlis.Birim,
            GecerlilikBaslangic = yanlis.GecerlilikBaslangic,
            GecerlilikBaslangicAni = simdi,
            GecerlilikBitis = null,
            Dayanak = yanlis.Dayanak,
            IcNot = yanlis.IcNot,
            KaynakUrl = yanlis.KaynakUrl,
            Teyit = yanlis.Teyit,
            AciklamaSade = yanlis.AciklamaSade,
            ResmiGazeteTarihi = yanlis.ResmiGazeteTarihi,
            ResmiGazeteSayisi = yanlis.ResmiGazeteSayisi,
            Olusturan = yanlis.Olusturan,
            Olusturuldu = simdi,
        };
        db.YasalParametreler.Add(dogru);

        string D(decimal? v) => v?.ToString("0.######", CultureInfo.InvariantCulture) ?? "-";
        k.ServiceProvider.GetRequiredService<DenetimServisi>().SistemYazmaKaydet(
            Modules.Sistem.SistemServisi.SistemTenantId, "YasalParametre", dogru.Id,
            $"yasal parametre AYNI GÜN DÜZELTİLDİ · {yanlis.Anahtar} · yanlış {D(yanlis.DegerSayi)} ({yanlis.Id})"
            + $" → doğru {D(plan.Yeni)} · yanlış değer {Saat(yanlisAni)}–{Saat(simdi)} arasında geçerliydi,"
            + " bu aralıktaki işlemler değişmez"
            + $" · operatör: {plan.Operator} · gerekçe: {plan.Gerekce}");

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("Bu sınırda çakışan bir değer var; düzeltme yazılmadı.");
        }
        Tazele();

        return new
        {
            id = dogru.Id,
            anahtar = dogru.Anahtar,
            birim = dogru.Birim,
            degerSayi = dogru.DegerSayi,
            yanlisDeger = yanlis.DegerSayi,
            yanlisSaati = Saat(yanlisAni),
            duzeltmeSaati = Saat(simdi),
            duzeltilenSatirId = yanlis.Id,
        };
    }

    private static string Kirp(string s, int n) => s.Length > n ? s[..n] : s;
}

/// <summary>
/// PAKET YASAL-1 (K-7): açılışta tabloyla karşılaştırılan yapılandırma değerleri.
/// Yalnız MEVZUAT rakamları; politika anahtarları (Etkin, açıklama uzunlukları) girmez.
/// </summary>
public static class YasalAcilisDenetimi
{
    public static IReadOnlyDictionary<string, decimal> YapilandirmaDegerleri(IServiceProvider sp)
    {
        var tevsik = sp.GetRequiredService<Modules.Tevsik.TevsikEsikleri>();
        var masak = sp.GetRequiredService<Modules.Masak.MasakEsikleri>();
        var hurda = sp.GetRequiredService<Modules.Hurda.HurdaAyarlari>();
        var kdv = sp.GetRequiredService<Modules.Fatura.KdvAyarlari>();
        return new Dictionary<string, decimal>(StringComparer.Ordinal)
        {
            [YasalAnahtar.TevsikHaddiTl] = tevsik.Had,
            [YasalAnahtar.MasakKimlikEsigiTl] = masak.KimlikEsigi,
            [YasalAnahtar.MasakAmacBeyaniEsigiTl] = masak.AmacBeyaniEsigi,
            [YasalAnahtar.MasakBeyanFormuEsigiTl] = masak.BeyanFormuEsigi,
            [YasalAnahtar.MasakEkBelgeEsigiTl] = masak.EkBelgeEsigi,
            [YasalAnahtar.MasakSibIsGunu] = masak.SupheliBildirimIsGunu,
            [YasalAnahtar.MasakSaklamaYil] = masak.SaklamaYili,
            [YasalAnahtar.GiderPusulasiGun] = hurda.PusulaDuzenlemeGunu,
            [YasalAnahtar.StopajEsnafMuafHurdaOran] = hurda.EsnafMuafStopajOran,
            [YasalAnahtar.StopajNihaiTuketiciOran] = hurda.NihaiTuketiciStopajOran,
            [YasalAnahtar.KdvGenelOran] = kdv.VarsayilanOran,
        };
    }
}
