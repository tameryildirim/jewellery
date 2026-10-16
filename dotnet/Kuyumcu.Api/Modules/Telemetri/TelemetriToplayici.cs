using System.Collections.Concurrent;

namespace Kuyumcu.Api.Modules.Telemetri;

// ═════════════════════════════════════════════════════════════════════════════
// TELEMETRİ TOPLAYICI — PAKET P-28 · SINGLETON, BELLEK İÇİ, KAYIPSIZ DEĞİL.
//
// ⚠ DÜRÜST SINIR (Anayasa md.1): bu toplayıcı bellek içidir. Süreç yazma
//   turundan önce düşerse o pencerenin sayaçları KAYBOLUR. Kayıp GİZLENMEZ:
//   <see cref="DusurulenOlcum"/> sayacı taşma kaybını, yazıcı günlüğü ise
//   tur hatasını raporlar. Telemetri bir MUHASEBE defteri değildir; kapasite
//   paydası için "yaklaşık ama ölçülmüş" yeterlidir, "uydurulmuş" değildir.
//
// ⚠ KİŞİSEL VERİ YOK: anahtar (kiracı, saat), değer dört sayaçtır. Bu sınıfta
//   HttpContext, Request, User, IP, yol ya da metin içerik HİÇ GÖRÜNMEZ —
//   `dotnet/denetim.sh` TELEMETRIDE_KISISEL_VERI sayacı bu dosyayı da tarar.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir kiracı-saat penceresinin ölçülmüş toplamı.</summary>
/// <param name="TenantId">Kiracı ETİKETİ (içerik değil).</param>
/// <param name="Saat">Pencere başı (UTC, saate yuvarlanmış).</param>
public sealed record TelemetriBirikimi(
    string TenantId, DateTime Saat, int IstekSayisi, int HataSayisi, long IslemMs, long YanitBayt);

/// <summary>
/// İstek sayaçlarını bellekte biriktirir; <see cref="TelemetriYazici"/> onları
/// periyodik olarak veritabanına taşır.
/// </summary>
public sealed class TelemetriToplayici
{
    /// <summary>
    /// Aynı anda tutulabilen (kiracı, saat) penceresi sayısı. Tavan: yazıcı
    /// durursa bellek sınırsız büyümesin. Aşıldığında yeni pencere AÇILMAZ ve
    /// kayıp <see cref="DusurulenOlcum"/>'e yazılır (sessiz kayıp yok).
    /// </summary>
    public const int PencereTavani = 20_000;

    private sealed class Sayac
    {
        public int Istek;
        public int Hata;
        public long Ms;
        public long Bayt;
    }

    private readonly ConcurrentDictionary<(string Kiraci, DateTime Saat), Sayac> kutu = new();
    private long dusurulen;

    /// <summary>Tavan yüzünden kaydedilemeyen istek sayısı (kümülatif).</summary>
    public long DusurulenOlcum => Interlocked.Read(ref dusurulen);

    /// <summary>Bellekte bekleyen pencere sayısı.</summary>
    public int BekleyenPencere => kutu.Count;

    /// <summary>UTC anını pencere başına (saat) yuvarlar. Tek kaynak budur.</summary>
    public static DateTime Pencere(DateTime an) =>
        new(an.Year, an.Month, an.Day, an.Hour, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Bir isteği sayaçlara ekler. Kiracı etiketi boşsa ÖLÇÜLMEZ: kiracısız
    /// istek (giriş, sağlık ucu) hiçbir kiracıya fatura edilemez ve ortak bir
    /// "bilinmeyen" kovası açmak kiracılar arası karışma demektir.
    /// </summary>
    public void Kaydet(string? tenantId, DateTime an, long islemMs, long yanitBayt, bool hataMi)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) return;
        var anahtar = (tenantId, Pencere(an));

        if (!kutu.TryGetValue(anahtar, out var sayac))
        {
            if (kutu.Count >= PencereTavani)
            {
                Interlocked.Increment(ref dusurulen);
                return;
            }
            sayac = kutu.GetOrAdd(anahtar, _ => new Sayac());
        }

        Interlocked.Increment(ref sayac.Istek);
        if (hataMi) Interlocked.Increment(ref sayac.Hata);
        Interlocked.Add(ref sayac.Ms, islemMs < 0 ? 0 : islemMs);
        Interlocked.Add(ref sayac.Bayt, yanitBayt < 0 ? 0 : yanitBayt);
    }

    /// <summary>
    /// Biriken pencereleri ALIR ve kutuyu boşaltır (atomik takas).
    /// <para>⚠ Çağıran yazamazsa satırlar kaybolur — bu yüzden yazıcı, aldığı
    /// listeyi yazamadığında <see cref="GeriKoy"/> ile iade eder.</para>
    /// </summary>
    public IReadOnlyList<TelemetriBirikimi> BosaltVeAl()
    {
        var sonuc = new List<TelemetriBirikimi>(kutu.Count);
        foreach (var anahtar in kutu.Keys.ToList())
        {
            if (!kutu.TryRemove(anahtar, out var s)) continue;
            sonuc.Add(new TelemetriBirikimi(
                anahtar.Kiraci, anahtar.Saat,
                Volatile.Read(ref s.Istek), Volatile.Read(ref s.Hata),
                Interlocked.Read(ref s.Ms), Interlocked.Read(ref s.Bayt)));
        }
        return sonuc;
    }

    /// <summary>Yazılamayan birikimi kutuya geri ekler (toplayarak, üzerine yazmadan).</summary>
    public void GeriKoy(IReadOnlyList<TelemetriBirikimi> birikim)
    {
        foreach (var b in birikim)
        {
            var sayac = kutu.GetOrAdd((b.TenantId, b.Saat), _ => new Sayac());
            Interlocked.Add(ref sayac.Istek, b.IstekSayisi);
            Interlocked.Add(ref sayac.Hata, b.HataSayisi);
            Interlocked.Add(ref sayac.Ms, b.IslemMs);
            Interlocked.Add(ref sayac.Bayt, b.YanitBayt);
        }
    }
}
