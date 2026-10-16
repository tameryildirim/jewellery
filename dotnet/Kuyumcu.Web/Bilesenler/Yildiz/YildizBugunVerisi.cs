using System.Globalization;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Bilesenler.Yildiz;

/// <summary>
/// PAKET YILDIZ-4A — "Yıldız" Bugün ekranının (tuval Bugun4 / Bugun4a) verisi.
///
/// <para>
/// ⚠ VERİ ÜRETMEZ: her değer Panel'in ZATEN yüklediği mevcut uçlardan gelir
/// (/raporlar/gunluk, /kasalar/nakit-ayrimi, /raporlar/cari-yaslandirma,
/// /raporlar/vade-takibi, /bildirimler, uyum özetleri, /raporlar/kar-zarar).
/// Para/veri/yasal davranış DEĞİŞMEZ; yalnız yerleşim ve yazım tuvaldeki gibidir.
/// </para>
/// <para>
/// ⚠ Bilinmeyen değer UYDURULMAZ (Anayasa md. 1): yüklenemeyen/yetkisiz parça
/// cümleden ve listeden düşer; hata ayrı bantta söylenir (UX-S28).
/// </para>
/// </summary>
public sealed class YildizBugunVerisi
{
    /// <summary>Günün net satış tutarı yükleniyor mu (dev rakam yerine "—").</summary>
    public bool SatisYukleniyor { get; init; }
    /// <summary>Rol satış özetini göremez: dev rakam ve satış cümlesi YAZILMAZ.</summary>
    public bool SatisYetkiYok { get; init; }
    /// <summary>Satış özeti alınamadı: rakam "0 ₺"a çevrilmez, bant çizilir.</summary>
    public string? SatisHata { get; init; }
    public decimal SatisTl { get; init; }
    public int SatisAdet { get; init; }

    /// <summary>Kasa cümlesi tutarı (biçimli); null = parça yazılmaz.</summary>
    public string? KasaTutar { get; init; }
    /// <summary>Nakit ayrımı okundu mu ("nakit" denir mi).</summary>
    public bool KasaNakit { get; init; }

    /// <summary>Müşteri hesapları TL toplamları; null = parça yazılmaz (yetki yok / hata / sınıra değdi).</summary>
    public decimal? AlacakTl { get; init; }
    public decimal? BorcTl { get; init; }

    public IReadOnlyList<YildizBekleyenSatir> Bekleyen { get; init; } = [];
    public bool BekleyenYukleniyor { get; init; }
    /// <summary>Liste altında soluk satır (sunucu sınırı, şube kapsamı …) — UX-S29.</summary>
    public IReadOnlyList<(string Id, string Metin)> Notlar { get; init; } = [];
    /// <summary>Kartların kendi hata bantları (UX-S28) — Id + mesaj.</summary>
    public IReadOnlyList<(string Id, string? Mesaj)> Hatalar { get; init; } = [];

    /// <summary>Alt şerit "Kâr" değeri; null = öğe çizilmez (yetki yok / yüklenmedi).</summary>
    public string? KarMetni { get; init; }
}

/// <summary>
/// "Seni bekleyen" listesinin tek satırı (tuval .sat): metin [· soluk ek] · durum · tek eylem (b3).
/// <paramref name="DurumKotu"/> = durum kırmızı (borç tutarı, KRİTİK iş) — renk yalnız durum içindir.
/// </summary>
public sealed record YildizBekleyenSatir(
    string Kimlik, string Metin, string? Ek, string? Durum, bool DurumKotu,
    string? Eylem, string? Adres, string? Ipucu = null);

/// <summary>Yıldız Bugün saf yardımcıları (bUnit'siz sınanır).</summary>
public static class YildizBugunBicim
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// Üst satır: "Perşembe · 8 Ekim 2026 · Merkez Şube" (büyük harf CSS'ten).
    /// Şube adı bilinmiyorsa (null) parça YAZILMAZ — tahmin yok.
    /// </summary>
    public static string UstSatir(DateTime gun, string? sube, string dil)
    {
        // TOPLU-1 TARIH-TEK: tarih yazımı Bicim.GunBicim'den (tek yol).
        var tarih = Bicim.GunBicim(gun, "dddd '·' d MMMM yyyy", dil == "en" ? CultureInfo.InvariantCulture : Tr);
        return string.IsNullOrWhiteSpace(sube) ? tarih : $"{tarih} · {sube}";
    }

    /// <summary>
    /// Gösterilen şubenin adı (SubeSecici.KartMetni ile AYNI kural): tek şube → adı;
    /// seçili görünüm şubesi ya da oturum şubesi listede → adı; tüm-şube yetkili ve
    /// birden çok şube, seçim yok → "Tüm şubeler"; bilinmiyorsa null.
    /// </summary>
    public static string? SubeAdi(IReadOnlyList<Kuyumcu.Core.Sozlesmeler.SubeOzeti> subeler,
        string? gorunumSubeId, bool tumSubeler, string? oturumSubeId, string tumuMetni)
    {
        if (subeler.Count == 0) return null;
        if (subeler.Count == 1) return subeler[0].Ad;
        var hedef = tumSubeler ? gorunumSubeId : oturumSubeId;
        if (hedef is null) return tumSubeler ? tumuMetni : null;
        return subeler.FirstOrDefault(s => s.Id == hedef)?.Ad;
    }

    /// <summary>
    /// Cümle (tuval): "Bugün 11 satış. Kasada 230.356,54 ₺ nakit. Alacağın 32.654,79 ₺, borcun 3.840 ₺."
    /// Bilinmeyen parça cümleye GİRMEZ.
    /// </summary>
    public static string Cumle(YildizBugunVerisi v, string dil)
    {
        var parcalar = new List<string>();
        if (!v.SatisYukleniyor && !v.SatisYetkiYok && v.SatisHata is null)
            parcalar.Add(YildizBugunSozlugu.CumleSatis(v.SatisAdet, dil));
        if (v.KasaTutar is { } kasa)
            parcalar.Add(YildizBugunSozlugu.CumleKasa(kasa, v.KasaNakit, dil));
        if (v.AlacakTl is decimal a && v.BorcTl is decimal b)
            parcalar.Add(YildizBugunSozlugu.CumleCari(Bicim.Tl(a), Bicim.Tl(b), dil));
        return string.Join(" ", parcalar);
    }

    /// <summary>Kur şeridi değeri: tam lira, binlik, ₺ İŞARETSİZ (tuval "HAS 6.656").</summary>
    public static string KurTam(decimal n) =>
        Math.Round(n, 0, MidpointRounding.AwayFromZero).ToString("N0", Tr);

    /// <summary>
    /// Alt şerit has miktarı (tuval "+157,3 g has"): bir ondalık, işaret yuvarlanmış
    /// değerden; sıfır işaretsiz. <paramref name="isaretli"/> false → "157,3 g" (vitrin).
    /// </summary>
    public static string HasKisa(decimal gram, string birim, bool isaretli)
    {
        var y = Math.Round(gram, 1, MidpointRounding.AwayFromZero);
        var isaret = !isaretli || y == 0m ? (y < 0m ? "−" : "") : y > 0m ? "+" : "−";
        return $"{isaret}{Math.Abs(y).ToString("N1", Tr)} {birim}";
    }

    // ── PAKET YILDIZ-4C — "Seni bekleyen" listesinin iki tuval satırı (Bugun4: "Dünkü gün sonu kapatılmadı",
    //    "Derya Yıldızlı'nın emaneti · 9,16 g has — 30 günü aştı"). YALNIZ OKUMA; veri yoksa satır ÇIKMAZ. ──

    /// <summary>
    /// Emanet "bekledi" eşiği (gün) — ekip oyu (YILDIZ-4C): 30 gün; ayar SABİTİ (yasal kural DEĞİL,
    /// yalnız hatırlatma). Tuvaldeki "30 günü aştı" metni bu sayıdan üretilir.
    /// </summary>
    public const int EmanetEsikGun = 30;

    /// <summary>Panelin emanet yaşı için okuyacağı en çok müşteri (her biri GET /emanet/{id}); aşılırsa not satırı (UX-S29).</summary>
    public const int EmanetOkumaSiniri = 10;

    /// <summary>
    /// Dün kapanmamış kasalar: dün hareketi OLAN (hareketSayisi &gt; 0) ve kapanışı KAPALI olmayan
    /// (kayıt yok = AÇIK ya da YENIDEN_ACILDI) kasaların adları. Hareketsiz gün kapanış istemez
    /// (dükkân kapalıydı) → listeye girmez. Gün yanıtta yoksa kasa sayılmaz (uydurma yok).
    /// </summary>
    public static IReadOnlyList<string> DunKapanmayanKasalar(KasaDefteriYaniti? yanit, DateOnly dun)
    {
        var gun = Bicim.Makine(dun);   // TOPLU-1 TARIH-TEK: makine tarihi tek yoldan (sunucunun "tarih" alanı yyyy-MM-dd)
        return [.. (yanit?.Kasalar ?? [])
            .Where(k => (k.Gunler ?? []).Any(g => g.Tarih == gun && g.HareketSayisi > 0 && g.Durum != "KAPALI"))
            .Select(k => k.KasaAd)];
    }

    /// <summary>
    /// Emanetin elde durduğu ilk YEREL gün: ALTIN emanet hareketleri tarih sırasıyla toplanır; bakiye sıfırdan
    /// artıya geçtiği son an "o günden beri" sayılır, bakiye sıfırlanırsa sayaç düşer. Bakiye artı değilse ya da
    /// sunucu listesi sınıra değdiyse (eski hareketler eksik) <c>null</c> — yaş UYDURULMAZ.
    /// </summary>
    public static DateOnly? EmanetBaslangici(IEnumerable<(DateTime Tarih, decimal Has, string? Maden)> hareketler, bool sinirDegdi)
    {
        if (sinirDegdi) return null;
        const decimal Esik = 0.000001m;
        decimal bakiye = 0m;
        DateOnly? baslangic = null;
        foreach (var h in hareketler.Where(x => string.IsNullOrWhiteSpace(x.Maden) || x.Maden == "ALTIN").OrderBy(x => x.Tarih))
        {
            var once = bakiye;
            bakiye += h.Has;
            if (once <= Esik && bakiye > Esik) baslangic = Bicim.IsletmeGunu(h.Tarih);
            else if (bakiye <= Esik) baslangic = null;
        }
        return bakiye > Esik ? baslangic : null;
    }

    /// <summary>Emanet kaç gündür duruyor (bugün − başlangıç); bilinmiyorsa null.</summary>
    public static int? EmanetGunu(DateOnly? baslangic, DateOnly bugun) =>
        baslangic is { } b ? bugun.DayNumber - b.DayNumber : null;

    /// <summary>Emanet has miktarı (tuval "9,16 g has" — Müşteriler "emanette 9,16 g has" ile aynı iki ondalık).</summary>
    public static string HasIki(decimal gram, string birim) => $"{gram.ToString("N2", Tr)} {birim}";

    /// <summary>Satır çıkar mı: gün bilinir ve eşiği AŞAR (tuval "30 günü aştı" = &gt; 30).</summary>
    public static bool EmanetSatiriCikar(int? gun) => gun is int g && g > EmanetEsikGun;

    /// <summary>
    /// Türkçe ad + iyelik eki (tuval "Derya Yıldızlı'nın"): son ünlüye göre ı/i/u/ü; ad ünlüyle bitiyorsa
    /// kaynaştırma "n". Ünlü yoksa (rakam, kısaltma) ek UYDURULMAZ → null (çağıran yedek metni kullanır).
    /// </summary>
    public static string? Iyelik(string ad)
    {
        var a = ad.TrimEnd();
        if (a.Length == 0) return null;
        const string Unluler = "aeıioöuüAEIİOÖUÜ";
        var son = a.LastOrDefault(c => Unluler.Contains(c));
        if (son == default) return null;
        var u = char.ToLower(son, CultureInfo.GetCultureInfo("tr-TR")) switch
        {
            'a' or 'ı' => 'ı',
            'e' or 'i' => 'i',
            'o' or 'u' => 'u',
            _ => 'ü',
        };
        var unluyleBiter = Unluler.Contains(a[^1]);
        if (!unluyleBiter && !char.IsLetter(a[^1])) return null;
        return unluyleBiter ? $"{a}'n{u}n" : $"{a}'{u}n";
    }

    /// <summary>Müşteri hesapları TL toplamları: Σ pozitif bakiye = alacak, Σ |negatif| = borç.</summary>
    public static (decimal Alacak, decimal Borc) CariToplamlari(IEnumerable<decimal> tlBakiyeler)
    {
        decimal a = 0m, b = 0m;
        foreach (var t in tlBakiyeler)
        {
            if (t > 0m) a += t;
            else if (t < 0m) b -= t;
        }
        return (a, b);
    }
}
