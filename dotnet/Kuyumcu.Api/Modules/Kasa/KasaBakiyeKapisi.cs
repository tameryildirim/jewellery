using System.Globalization;
using Kuyumcu.Api.Common;

namespace Kuyumcu.Api.Modules.Kasa;

/// <summary>
/// NAKİT KASA EKSİYE DÜŞEMEZ — tek merkez.
///
/// <para>
/// Çekmecede olmayan para ödenemez. Kural kâğıtta vardı ama kodda YALNIZ iki
/// yolda (gider, virman) elle tekrarlanmıştı; alış, hurda alımı, iade, para
/// üstü ve elle kasa çıkışı kapısızdı. Canlı ölçüm (2026-09-23, kuyumcu_net
/// kasa <c>01M3227XT7DXVZKS9HC54MBYAN</c>): bakiye <b>−738.298,80 TL</b>,
/// defter toplamı da −738.298,80 TL — yani defter tutarlı, kapı yoktu.
/// Dökümde dört <c>ALIS</c> (−858.500,00 TL) ve bir <c>HURDA_ALIS</c>
/// (−24.548,80 TL) bakiyeyi aşıyordu. Sızıntı yolu ölçülmüştür, tahmin değildir.
/// </para>
/// <para>
/// Kapının <see cref="GunKapanisiKapisi"/> ile aynı gerekçesi vardır:
/// kontrolün çağrı noktalarına serpiştirilmesi "biri unutulur" demektir.
/// </para>
/// <para>
/// ⚠ <b>KAPSAM DIŞI — BANKA HESAPLARI:</b> banka hesabında bu kapı YOKTUR ve
/// bu bilinçlidir. KMH (kredili mevduat) negatif bakiyeye izin verir; kuralı
/// kurucu/mali müşavir belirler — uydurulmaz.
/// </para>
/// </summary>
public static class KasaBakiyeKapisi
{
    /// <summary>
    /// Kasa bakiyesini <paramref name="isaretliDelta"/> kadar değiştirmek
    /// bakiyeyi eksiye düşürüyorsa <see cref="BadRequestException"/> atar
    /// (fail-closed). Artı delta (para GİRİŞİ) hiçbir zaman engellenmez —
    /// aksi hâlde zaten eksiye düşmüş bir kasa bir daha DOLDURULAMAZDI.
    /// </summary>
    public static void Dogrula(Data.Kasa kasa, decimal isaretliDelta)
    {
        if (isaretliDelta >= 0m) return;
        if (kasa.Bakiye + isaretliDelta < 0m)
            throw new BadRequestException(Mesaj(kasa.Ad, kasa.Bakiye, kasa.ParaBirimi, -isaretliDelta)).KodIle(YetersizKodu);
    }

    /// <summary>
    /// Aynı kapı, <c>Data.Kasa</c> örneği elde olmayan çağrı noktaları için
    /// (virman tarafı bir projeksiyon taşır). Mesaj TEK yerden üretilir.
    /// </summary>
    public static void Dogrula(string kasaAdi, decimal bakiye, string paraBirimi, decimal isaretliDelta)
    {
        if (isaretliDelta >= 0m) return;
        if (bakiye + isaretliDelta < 0m)
            throw new BadRequestException(Mesaj(kasaAdi, bakiye, paraBirimi, -isaretliDelta)).KodIle(YetersizKodu);
    }

    /// <summary>
    /// PARA-DEMO-2 (madde 3, PARA) — NAKİT ÇIKIŞ KAPISI. Toplam bakiye kapısı (<see cref="Dogrula(Data.Kasa, decimal)"/>)
    /// aynen uygulanır; çıkan araç NAKİT ise (kart/havale DEĞİLSE) ayrıca kasanın NAKİT payına bakılır
    /// (<see cref="KasaNakitAyrimi"/>): kasaya yazılmış kart/havale tahsilatı çekmecede DURMAZ, nakit
    /// sanılıp dışarı verilemez. Örnek: nakit 1000 + kart 500 iken 1200 nakit çıkışı 400.
    /// Kart/havale çıkışı: ELLE kasa çıkışında (<paramref name="aracPayiKapisi"/> = true) kendi aracının payını aşamaz
    /// (tur 5, S16); belgeli yollarda (alış/iade/takas) yalnız toplam kapısına tabidir — nakit payını değiştirmez.
    /// </summary>
    public static async Task NakitDogrula(Data.KuyumcuDbContext db, string tenantId, Data.Kasa kasa,
        decimal isaretliDelta, string? arac, bool aracPayiKapisi = false)
    {
        Dogrula(kasa, isaretliDelta);
        await NakitDogrula(db, tenantId, kasa.Id, kasa.Ad, kasa.Bakiye, kasa.ParaBirimi, isaretliDelta, arac, aracPayiKapisi);
    }

    /// <summary>Aynı nakit kapısı, kasa örneği elde olmayan çağrı noktaları için (virman projeksiyonu).</summary>
    public static async Task NakitDogrula(Data.KuyumcuDbContext db, string tenantId, string kasaId, string kasaAdi,
        decimal bakiye, string paraBirimi, decimal isaretliDelta, string? arac, bool aracPayiKapisi = false)
    {
        if (isaretliDelta >= 0m) return;
        // Belgeli kart/havale çıkışı (alış, iade, takas farkı müşteriye kartla/havaleyle) çekmeceden çıkmaz:
        // POS/banka üzerinden gider, nakit payını DEĞİŞTİRMEZ → yalnız toplam kapısına tabidir (bugünkü gibi).
        if (!aracPayiKapisi && arac is not null && KasaNakitAyrimi.NakitDisiAraclar.Contains(arac)) return;
        var ayrim = await KasaNakitAyrimi.Hesapla(db, tenantId, kasaId, bakiye);
        // PARA-DEMO-2 tur 5 (doğrulayıcı S16) — ELLE kasa çıkışında (karşılığında belge yok, aracı kullanıcı seçer)
        // KART/HAVALE çıkışı KENDİ payını aşamaz (virmandaki kuralın aynısı): kasaya yazılmış olandan fazla kart/havale
        // parası kasadan çıkamaz; aksi hâlde kart payı eksiye düşer ve ayrım (Nakit X · Kart/Havale Y) anlamsızlaşır.
        if (arac is not null && KasaNakitAyrimi.NakitDisiAraclar.Contains(arac))
        {
            var pay = arac == Data.OdemeAraci.Kart ? ayrim.Kart : ayrim.Havale;
            if (pay + isaretliDelta < 0m)
                throw new BadRequestException(AracMesaji(kasaAdi, arac, pay, paraBirimi, -isaretliDelta)).KodIle(AracPayiYetersizKodu);
            return;
        }
        if (ayrim.Nakit + isaretliDelta < 0m)
            throw new BadRequestException(NakitMesaji(kasaAdi, ayrim, paraBirimi, -isaretliDelta)).KodIle(NakitYetersizKodu);
    }

    private static string AracMesaji(string kasaAdi, string arac, decimal pay, string paraBirimi, decimal istenen) =>
        $"'{kasaAdi}' kasasındaki {(arac == Data.OdemeAraci.Kart ? "kart" : "havale")} tahsilatı "
        + $"{ParaMetni(Math.Max(0m, pay), paraBirimi)}; "
        + $"{ParaMetni(istenen, paraBirimi)} {(arac == Data.OdemeAraci.Kart ? "kartla" : "havaleyle")} çıkarılamaz.";

    private static string NakitMesaji(string kasaAdi, KasaNakitAyrimiSonuc a, string paraBirimi, decimal istenen) =>
        $"'{kasaAdi}' kasasında nakit yetersiz: çekmecede "
        + $"{ParaMetni(Math.Max(0m, a.Nakit), paraBirimi)} nakit var, "
        + $"{ParaMetni(istenen, paraBirimi)} nakit isteniyor. "
        + $"Kasadaki kart ({ParaMetni(a.Kart, paraBirimi)}) ve havale "
        + $"({ParaMetni(a.Havale, paraBirimi)}) tahsilatı nakit olarak verilemez.";

    /// <summary>
    /// PARA-DEMO-2 tur 3 (UX-S32, ölçer K5) — kullanıcının gördüğü hata bandındaki tutar ekranla aynı
    /// biçimde yazılır: tr-TR, kuruş 2 hane, para birimi işareti (₺/$/€); "1000.00 TRY" kalıbı yok.
    /// Tanınmayan para birimi kodu olduğu gibi yazılır (uydurma işaret basılmaz).
    /// </summary>
    internal static string ParaMetni(decimal n, string paraBirimi)
    {
        var y = decimal.Round(n, 2, MidpointRounding.AwayFromZero);
        var govde = Math.Abs(y).ToString("N2", CultureInfo.GetCultureInfo("tr-TR"));
        var isaret = paraBirimi switch { "TRY" => "\u20ba", "USD" => "$", "EUR" => "\u20ac", _ => paraBirimi };
        return (y < 0 ? "\u2212" : "") + govde + "\u00a0" + isaret;
    }

    /// <summary>
    /// GEREKÇELİ İSTİSNA — storno ters kaydı bu kapıya TABİ DEĞİLDİR.
    ///
    /// <para>
    /// Defter bütünlüğü: bir hareketin TERSİ her zaman yazılabilmelidir. Aksi
    /// hâlde yanlış girilmiş bir tahsilat, kasa bakiyesi yettiği sürece geri
    /// alınabilir, yetmediğinde defterde ÇAKILI kalırdı — düzeltme yolu
    /// bakiyeye bağlı olamaz.
    /// </para>
    /// <para>
    /// Bu çağrı SESSİZ ATLAMA DEĞİLDİR: kapının atlandığı her satır bu adı
    /// taşır, <c>grep KasaBakiyeKapisi</c> ile sayılabilir.
    /// </para>
    /// </summary>
    public static void StornoIcinAtlanir(decimal isaretliDelta)
    {
        // Kasıtlı olarak hiçbir kontrol uygulanmaz; gerekçe yukarıdadır.
        _ = isaretliDelta;
    }

    /// <summary>
    /// PARA-DEMO-3 (tasarım hakemi KRİTİK, 02.10): toplam kapısı mesajı artık nakit kapısıyla AYNI dil ve
    /// biçimde yazılır — tr-TR, kuruş 2 hane, para birimi işareti (<see cref="ParaMetni"/>): "'Ana Kasa'
    /// kasasının bakiyesi yetersiz: 312.547,91 ₺ var, 2.000.000,00 ₺ isteniyor." Eskiden bilinçli olarak
    /// InvariantCulture ("312547.91 TRY") idi; Kasalar elle hareketi gibi istemci ayrıştırıcısından geçmeyen
    /// her ekranda ham basılıyordu (kurucunun 24.09 K5 şikâyeti). Ret gövdesi ayrıca makine kodu taşır
    /// (<see cref="YetersizKodu"/>) — istemci reddi METNE değil KODA bakarak tanır; istemci ayrıştırıcısı
    /// (<c>KasaBakiyeMesaji</c>) eski invariant biçimi de okumayı sürdürür (geçiş uyumu).
    /// </summary>
    private static string Mesaj(string kasaAdi, decimal bakiye, string paraBirimi, decimal istenen) =>
        $"'{kasaAdi}' kasasının bakiyesi yetersiz: "
        + $"{ParaMetni(bakiye, paraBirimi)} var, "
        + $"{ParaMetni(istenen, paraBirimi)} isteniyor. "
        + "Nakit kasa eksiye düşemez.";

    /// <summary>PARA-DEMO-3: toplam kapısı reddinin makine kodu (gövdede <c>kod</c>).</summary>
    public const string YetersizKodu = "KASA_BAKIYE_YETERSIZ";

    /// <summary>PARA-DEMO-3: nakit payı kapısı reddinin makine kodu.</summary>
    public const string NakitYetersizKodu = "KASA_NAKIT_YETERSIZ";

    /// <summary>PARA-DEMO-3: elle kart/havale çıkışında araç payı kapısı reddinin makine kodu.</summary>
    public const string AracPayiYetersizKodu = "KASA_ARAC_PAYI_YETERSIZ";
}
