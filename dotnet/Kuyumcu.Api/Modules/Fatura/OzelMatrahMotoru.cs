using Kuyumcu.Api.Common;

namespace Kuyumcu.Api.Modules.Fatura;

// ═════════════════════════════════════════════════════════════════════════════
// ÖZEL MATRAH MOTORU — SAF sınıf (veritabanı, ağ, saat GÖRMEZ).
//
// TEK MOTOR KARARI (Bertan kısmi vetosunun çözümü · Mehmet K1-4 / K38-4):
// Has bedelinin defterden türetilmesi PROJEDE TEK YERDE yapılır. Paket 1 bunu
// kalem bazına, paket 38 hareket bazına uygular; ikisi de BU sınıfı çağırır.
// İki motor sevk edilseydi aynı faturanın has bedeli iki farklı yoldan
// hesaplanır, ilk sapmada hangisinin doğru olduğunun cevabı olmazdı
// (TB-11 "ikizlenmiş kural" borcunun mali hesaba taşınması).
//
// KURAL (KDVK md.23/e — DOĞRULANDI 30.09.2026, birincil metin, PAKET YASAL-4 § 3.1 M-9:
//   "sikke altınların teslim ve ithalinde matrah, külçe altın bedeli düşüldükten
//   sonra kalan miktardır" — https://www.mevzuat.gov.tr/mevzuatmetin/1.5.3065.pdf).
//   ⚠ AÇIK (M-10, T-5): KDV Genel Uygulama Tebliği § III/A-4.2.1 has fiyatını Borsa
//   İstanbul son resmî işgünü KAPANIŞI olarak ister; ürün kur kaydını kullanır —
//   kapanış verisi kaynağı/lisansı kurucu + mali müşavir kararı (Y4-D notu).
//   has bedeli = Σ round2(has_grami × kur kaydındaki altin_has_fiyati)
//   KDV yalnız (toplam − has bedeli) tutarı üzerinden, KDV DAHİL kabul edilerek
//   iç yüzde ile ayrıştırılır.
//
// YUVARLAMA: her bacak ve her kalem AYRI AYRI 2 haneye
// (MidpointRounding.AwayFromZero) — FaturaModulu.cs'teki mevcut yuvarlamayla
// birebir aynı. Önce toplayıp sonra yuvarlanırsa kalem toplamı belge
// toplamından kuruş sapar.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir has bacağı: has gram × o hareketin KENDİ kur kaydındaki has fiyatı.</summary>
/// <param name="HasGrami">Bacağın has gramı (işaretsiz, ≥ 0).</param>
/// <param name="HasBirimFiyat">O bacağın kur kaydındaki <c>altin_has_fiyati</c>.</param>
public readonly record struct HasBacagi(decimal HasGrami, decimal HasBirimFiyat);

/// <param name="HasGram">Bacakların toplam has gramı.</param>
/// <param name="HasDeger">Matrah DIŞINDA bırakılan has altın bedeli (TL).</param>
/// <param name="Matrah">KDV matrahı (KDV hariç).</param>
/// <param name="KdvTutar">Hesaplanan KDV.</param>
/// <param name="AraToplam">KDV hariç toplam = toplam − KDV.</param>
/// <param name="Uyari">Bilgi notu; hesabı DEĞİŞTİRMEZ. Has bacağı yoksa doldurulur.</param>
public sealed record OzelMatrahSonuc(
    decimal HasGram,
    decimal HasDeger,
    decimal Matrah,
    decimal KdvTutar,
    decimal AraToplam,
    string? Uyari);

public static class OzelMatrahMotoru
{
    public static decimal Yuvarla2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);
    public static decimal Yuvarla6(decimal d) => Math.Round(d, 6, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Matrah DIŞINDA bırakılacak has bedeli — <see cref="Hesapla"/>'nın İLK adımı.
    /// <para>
    /// AYRI METOT AMA AYRI MOTOR DEĞİL: <see cref="Hesapla"/> bunu çağırır, bir
    /// kopyasını taşımaz. Paket 38'in önizleme ucu (belge toplamı daha yazılmamışken
    /// "bu işlemin has bedeli kaç TL" sorusunu cevaplaması gerekir) ikinci bir
    /// toplama döngüsü yazmak zorunda kalmasın diye buradadır (ADR-0016 tek motor).
    /// </para>
    /// </summary>
    /// <exception cref="BadRequestException">
    /// Negatif has gramı; ya da bacağın has fiyatı yok/sıfır (FAIL-CLOSED).
    /// </exception>
    public static (decimal HasGram, decimal HasDeger) HasBedeli(IReadOnlyList<HasBacagi> bacaklar)
    {
        decimal hasGram = 0m, hasDeger = 0m;
        foreach (var b in bacaklar)
        {
            if (b.HasGrami < 0m)
                throw new BadRequestException("Has gramı negatif olamaz");
            // FAIL-CLOSED: fiyatı bilinmeyen bacak 0 TL sayılmaz; 0 yazmak tüm
            // tutarı matraha sokar ve kuyumcuya FAZLA KDV ödetir.
            if (b.HasBirimFiyat <= 0m)
                throw new BadRequestException(
                    "Has altın fiyatı bulunamadı — kur kaydı olmadan özel matrah hesaplanamaz");
            hasGram += Yuvarla6(b.HasGrami);
            hasDeger += Yuvarla2(b.HasGrami * b.HasBirimFiyat);
        }

        return (Yuvarla6(hasGram), Yuvarla2(hasDeger));
    }

    /// <summary>
    /// Özel matrahlı (KUYUM) ya da standart hesap. Bacak listesi BOŞSA has bedeli
    /// 0'dır ve sonuç standart KDV hesabının ta kendisidir — iki ayrı formül
    /// yazılmaz, tek yol vardır.
    /// </summary>
    /// <exception cref="BadRequestException">
    /// Has bedeli toplama eşit ya da ondan büyükse (matrah negatife düşer);
    /// oran veya toplam geçersizse. FAIL-CLOSED: "has bedeli 0" varsayılmaz.
    /// </exception>
    /// <summary>
    /// M2b — müşterinin yöntemi (16.10: "150 bin liralık fatura… binde 1'i, 150 TL'si KDV'ye
    /// giren kısım, geri kalanı %0'a giren… %20 dediğimde 30 TL"): matrah = toplam × binde/1000,
    /// KDV = matrah × oran, kalan (toplam − matrah − KDV) %0 kısmıdır (HasDeger sütununa yazılır).
    /// Satır toplamı DEĞİŞMEZ. Has gramı bilgi olarak bacaklardan taşınır; kur zorunlu değildir.
    /// </summary>
    public static OzelMatrahSonuc HesaplaBinde(
        IReadOnlyList<HasBacagi> bacaklar, decimal toplam, decimal binde, decimal kdvOran)
    {
        if (toplam <= 0m)
            throw new BadRequestException("Toplam tutar sıfırdan büyük olmalıdır");
        if (kdvOran < 0m || kdvOran > 1m)
            throw new BadRequestException("KDV oranı 0 ile 1 arasında olmalıdır");
        if (binde <= 0m || binde > 1000m)
            throw new BadRequestException("KDV'ye giren pay binde 0 ile 1000 arasında olmalıdır");
        var hasGram = Yuvarla6(bacaklar.Sum(b => Yuvarla6(b.HasGrami)));
        var matrah = Yuvarla2(toplam * binde / 1000m);
        var kdvTutar = Yuvarla2(matrah * kdvOran);
        if (matrah + kdvTutar > toplam)
        {   // binde 1000'e yakınsa KDV dahil iç yüzdeye düş — toplam aşılmaz
            matrah = Yuvarla2(toplam / (1m + kdvOran));
            kdvTutar = Yuvarla2(toplam - matrah);
        }
        var sifirKismi = Yuvarla2(toplam - matrah - kdvTutar);
        return new OzelMatrahSonuc(hasGram, sifirKismi, matrah, kdvTutar, Yuvarla2(toplam - kdvTutar), null);
    }

    /// <summary>M2b — ürün payı doluysa müşterinin binde yöntemi, değilse yasal hesap (tek giriş noktası).</summary>
    public static OzelMatrahSonuc Hesapla(
        IReadOnlyList<HasBacagi> bacaklar, decimal toplam, decimal kdvOran, decimal? matrahBinde) =>
        matrahBinde is > 0m ? HesaplaBinde(bacaklar, toplam, matrahBinde.Value, kdvOran)
                            : Hesapla(bacaklar, toplam, kdvOran);

    public static OzelMatrahSonuc Hesapla(
        IReadOnlyList<HasBacagi> bacaklar, decimal toplam, decimal kdvOran)
    {
        if (toplam <= 0m)
            throw new BadRequestException("Toplam tutar sıfırdan büyük olmalıdır");
        if (kdvOran < 0m || kdvOran > 1m)
            throw new BadRequestException("KDV oranı 0 ile 1 arasında olmalıdır");

        var (hasGram, hasDeger) = HasBedeli(bacaklar);

        if (hasDeger >= toplam)
            throw new BadRequestException("Has altın değeri toplam tutardan küçük olmalıdır");

        var matrahKdvDahil = toplam - hasDeger;             // işçilik + kâr, KDV dahil
        var matrah   = Yuvarla2(matrahKdvDahil / (1m + kdvOran));
        var kdvTutar = Yuvarla2(matrahKdvDahil - matrah);
        var araToplam = Yuvarla2(toplam - kdvTutar);

        return new OzelMatrahSonuc(
            hasGram, hasDeger, matrah, kdvTutar, araToplam,
            bacaklar.Count == 0 ? "Has bacağı yok — özel matrah uygulanmadı" : null);
    }
}
