namespace Kuyumcu.Api.Common;

// ═════════════════════════════════════════════════════════════════════════════
// RESMÎ TATİL TAKVİMİ — ADIM-A raporu §5/2'deki açığı kapatır.
//
// MASAK'ın 10 İŞ GÜNÜ sayacı hafta sonunu atlıyordu ama bayramları İŞ GÜNÜ
// sayıyordu; süre erken doluyor, kuyumcuya olduğundan az zaman veriliyordu.
//
// TASARIM:
//   • SABİT tatiller koda gömülüdür — tarihleri mevzuatla belirli, değişmez.
//   • DİNÎ bayramlar (Ramazan, Kurban) hicri takvime bağlıdır; MİLADÎ karşılığı
//     her yıl kayar ve Diyanet ilanıyla kesinleşir. TAHMİN EDİLMEZ:
//     yapılandırmadan (Takvim:ResmiTatiller) okunur.
//   • Yapılandırmada o yıl için kayıt yoksa dinî bayramlar SAYILMAZ — sonuç
//     süreyi KISALTIR, yani kuyumcu aleyhine değil LEHİNE hata olmaz.
//     Bu yüzden eksik yıl AÇIKÇA bildirilir (Eksik()).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Yapılandırmadan okunan tatil listesi (dinî bayramlar + idari tatiller).</summary>
public sealed class TakvimAyarlari
{
    /// <summary>"2026-03-20", "2026-03-21" biçiminde ek tatil günleri.</summary>
    public string[] ResmiTatiller { get; init; } = [];
}

public static class ResmiTatiller
{
    /// <summary>Tarihi her yıl AYNI olan resmî tatiller (ay, gün).</summary>
    private static readonly (int Ay, int Gun, string Ad)[] Sabitler =
    [
        (1, 1, "Yılbaşı"),
        (4, 23, "Ulusal Egemenlik ve Çocuk Bayramı"),
        (5, 1, "Emek ve Dayanışma Günü"),
        (5, 19, "Atatürk'ü Anma, Gençlik ve Spor Bayramı"),
        (7, 15, "Demokrasi ve Millî Birlik Günü"),
        (8, 30, "Zafer Bayramı"),
        (10, 29, "Cumhuriyet Bayramı"),
    ];

    /// <summary>Bir yılın sabit tatilleri.</summary>
    public static IEnumerable<DateOnly> Sabit(int yil) =>
        Sabitler.Select(t => new DateOnly(yil, t.Ay, t.Gun));

    /// <summary>Sabit tatilin adı; tatil değilse null.</summary>
    public static string? Ad(DateOnly gun) =>
        Sabitler.FirstOrDefault(t => t.Ay == gun.Month && t.Gun == gun.Day).Ad;

    /// <summary>
    /// Sabit tatiller + yapılandırmadaki ek günler (dinî bayramlar).
    /// Geçersiz biçimdeki kayıtlar sessizce atlanmaz — <see cref="Gecersiz"/> ile raporlanır.
    /// </summary>
    public static HashSet<DateOnly> Tumu(int yil, TakvimAyarlari? ayarlar)
    {
        var kume = new HashSet<DateOnly>(Sabit(yil));
        foreach (var g in Ayrıştır(ayarlar).Where(g => g.Year == yil)) kume.Add(g);
        return kume;
    }

    /// <summary>Birden çok yılı kapsayan küme (10 iş günü yıl sınırını aşabilir).</summary>
    public static HashSet<DateOnly> Aralik(int baslangicYili, int yilSayisi, TakvimAyarlari? ayarlar)
    {
        var kume = new HashSet<DateOnly>();
        for (var y = baslangicYili; y < baslangicYili + yilSayisi; y++)
            foreach (var g in Sabit(y)) kume.Add(g);
        foreach (var g in Ayrıştır(ayarlar)) kume.Add(g);
        return kume;
    }

    /// <summary>Yapılandırmada biçimi bozuk olan kayıtlar — sessizce yutulmaz.</summary>
    public static IReadOnlyList<string> Gecersiz(TakvimAyarlari? ayarlar) =>
        (ayarlar?.ResmiTatiller ?? [])
            .Where(s => !DateOnly.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _))
            .ToList();

    /// <summary>
    /// Dinî bayram kaydı bulunmayan yıllar. Bu yıllarda 10 iş günü hesabı
    /// bayramları iş günü sayar → süre OLDUĞUNDAN ERKEN dolar.
    /// </summary>
    public static bool DiniBayramEksik(int yil, TakvimAyarlari? ayarlar) =>
        !Ayrıştır(ayarlar).Any(g => g.Year == yil);

    private static IEnumerable<DateOnly> Ayrıştır(TakvimAyarlari? ayarlar) =>
        (ayarlar?.ResmiTatiller ?? [])
            .Select(s => DateOnly.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var g) ? g : (DateOnly?)null)
            .Where(g => g is not null)
            .Select(g => g!.Value);
}
