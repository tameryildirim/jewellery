using System.Text.Json;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Bilesenler.Fis;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-4 (doğrulama bulgusu KRİTİK — B4-3) — ALTIN BACAĞININ FİŞE BAĞLANMASI
//
// Sunucu tek etiketli OdemeTuru="ALTIN"ı artık reddediyor (G-1). Eski altınla
// ödeme ekrandan YALNIZ satış fişinin ALTIN bacağıyla alınır; bu dosya o
// bacağın fiş tarafındaki kurallarını TEK YERDE toplar:
//   • Kasa YOK (B4-5): para kasadan geçmez → kasa seçicisi çizilmez, gövdeye
//     kasaId konmaz.
//   • En çok BİR altın bacağı (B4-6): ikinci bacağın araç listesinde ALTIN çıkmaz.
//   • TL türetilir (B4-31): TutarTl = panelin hesapladığı net; kalanı ata ALTIN
//     bacağına yazmaz (yazsaydı sunucunun "kuruşu kuruşuna" kapısı 400 dönerdi).
//   • Satıcı kimliği / fark eksikse kayıt kilitli (B4-32/33): AltinDurum.
// ⚠ İSTEK ATMAZ (S-2 kuralı: tek POST yolu Islemler.razor Kaydet()). Ayarların
//   GET'i Servisler/AltinBacagiGirisi.cs → AltinBacagiAyarKaynagi'ndadır; sayfa çağırır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Satış fişinin ALTIN bacağına ait saf kurallar.</summary>
public static class FisAltinBacagi
{
    public const string Arac = "ALTIN";

    /// <summary>Bu bacak ALTIN mı.</summary>
    public static bool AltinMi(FisBacagi b) => b.Arac == Arac;

    /// <summary>PAKET ISLEM-BANT — fişin eski altın bacağındaki bant onayı (fiş gövdesi bantOnay/bantGerekce; yoksa null).</summary>
    public static IslemBantOnayDurumu? Bant(IEnumerable<FisBacagi> bacaklar) =>
        bacaklar.FirstOrDefault(b => AltinMi(b) && b.Altin is not null)?.Altin!.Bant;

    /// <summary>Bacağın kasası var mı (VERESIYE ve ALTIN kasadan geçmez).</summary>
    public static bool KasaAlir(FisBacagi b) => b.Arac is not ("VERESIYE" or Arac);

    /// <summary>
    /// B4-6 — <paramref name="ix"/> sıradaki bacağın seçebileceği araçlar: başka bir
    /// bacak zaten ALTIN ise ALTIN listeden çıkar (bu bacak ALTIN ise kalır).
    /// </summary>
    public static IEnumerable<(string Deger, string Etiket)> AracSecenekleri(
        IEnumerable<(string Deger, string Etiket)> tumu, IReadOnlyList<FisBacagi> bacaklar, int ix)
    {
        var baskaAltinVar = bacaklar.Where((b, i) => i != ix && AltinMi(b)).Any();
        return tumu.Where(x => x.Deger != Arac || !baskaAltinVar);
    }

    /// <summary>Araç değişince: ALTIN'a geçen bacak girdilerini açar, kasa/POS'u boşaltır; çıkan temizler.</summary>
    public static void AracDegisti(FisBacagi b)
    {
        if (AltinMi(b))
        {
            b.Altin ??= new AltinBacagiGirisi();
            b.KasaId = "";
            b.PosCihazId = "";
            b.TutarTl = b.AltinDurum?.Deger?.NetTl ?? 0m;
        }
        else
        {
            b.Altin = null;
            b.AltinDurum = null;
        }
    }

    /// <summary>Panel bildirdi: TL türetilen nettir (B4-31), eksikse 0 (fişi kapatamaz).</summary>
    public static void DurumYaz(FisBacagi b, AltinBacagiDurumu d)
    {
        b.AltinDurum = d;
        b.TutarTl = d.Deger?.NetTl ?? 0m;
    }

    /// <summary>"Kalanı ata" ALTIN bacağına yazmaz: son ALTIN-DIŞI bacak; yoksa null.</summary>
    public static FisBacagi? KalanHedefi(IReadOnlyList<FisBacagi> bacaklar) =>
        bacaklar.LastOrDefault(b => !AltinMi(b));

    /// <summary>
    /// Fişin altın kuralları tamam mı: en çok bir ALTIN bacağı ve her ALTIN bacağı
    /// kaydedilebilir (satıcı kimliği, ayar/gram/fiyat, fark 0 — B4-32/33).
    /// </summary>
    public static bool Tamam(IReadOnlyCollection<FisBacagi> bacaklar) =>
        bacaklar.Count(AltinMi) <= 1
        && bacaklar.Where(AltinMi).All(b => b.Altin is not null && b.AltinDurum is { Kaydedilebilir: true });

    /// <summary>
    /// satis-fisi gövdesindeki ALTIN bacağı — girdiler KAYIT ANINDA yeniden
    /// hesaplanır (panelin son bildirimine güvenilmez). Hesaplanamıyorsa null.
    /// </summary>
    public static object? Govde(FisBacagi b, AltinBacagiAyarlari? ayarlar) =>
        AltinMi(b) && b.Altin is { } g ? g.Bacak(g.Deger(ayarlar)) : null;

    /// <summary>
    /// UX-S27 — sunucunun döndürdüğü gider pusulası numarası banda yazılır
    /// (altinBacagi.giderPusulasi.pusulaNo). Yoksa null (mükellef satıcı ya da altın yok).
    /// </summary>
    public static string? PusulaNotu(JsonElement y, string dil)
    {
        var m = AltinBacagiSozlugu.Al(dil);
        if (y.ValueKind != JsonValueKind.Object) return null;
        if (!y.TryGetProperty("altinBacagi", out var a) || a.ValueKind != JsonValueKind.Object) return null;
        if (!a.TryGetProperty("giderPusulasi", out var p) || p.ValueKind != JsonValueKind.Object) return null;
        return p.TryGetProperty("pusulaNo", out var no) && no.ValueKind == JsonValueKind.String
            ? string.Format(m.PusulaDogdu, no.GetString())
            : null;
    }

    // ═══ PAKET S2-4 — EKRAN: "Eski altın" düğmesi, kilit sebebi, iki belge ═══

    /// <summary>Fişin en çok bacak sayısı (FisOdemeBacaklari.BacakEkle ile aynı sınır).</summary>
    public const int EnFazlaBacak = 5;

    /// <summary>
    /// S2-4 — "Eski altın" düğmesi eklenebilir mi: fişte ALTIN bacağı YOK (B4-6)
    /// ve bacak sınırı dolmamış. Doluysa düğme çizilmez; panel zaten açıktır.
    /// </summary>
    public static bool EklenebilirMi(IReadOnlyCollection<FisBacagi> bacaklar) =>
        !bacaklar.Any(AltinMi) && bacaklar.Count < EnFazlaBacak;

    /// <summary>
    /// S2-4 — tek dokunuşla ALTIN bacağı: kasa/POS boş (B4-5), TL 0 doğar ve
    /// panelin türettiği netle dolar (B4-31). Eklenemiyorsa false (ikinci altın yok).
    /// </summary>
    public static bool Ekle(List<FisBacagi> bacaklar)
    {
        if (!EklenebilirMi(bacaklar)) return false;
        var b = new FisBacagi { Arac = Arac };
        AracDegisti(b);
        bacaklar.Add(b);
        return true;
    }

    /// <summary>
    /// UX-S30 — Kaydet kilitliyse ve sebep ALTIN bacağıysa, panelin İLK eksiği
    /// Kaydet'in yanına yazılır (genel "ödeme satırları" yerine). Yoksa null.
    /// </summary>
    public static string? KilitSebebi(IReadOnlyCollection<FisBacagi> bacaklar, string dil)
    {
        var altin = bacaklar.FirstOrDefault(AltinMi);
        if (altin is null) return null;
        // t10 (hakem KRİTİK "fark üç yerde"): fark eksiği burada söylenmez — sayfanın tek fark cümlesi söyler.
        var fark = AltinBacagiSozlugu.Al(dil).FarkSifirOlmali;
        var ilk = altin.AltinDurum?.Eksikler.FirstOrDefault(e => e != fark);
        if (altin.AltinDurum is { Kaydedilebilir: true } || ilk is null) return null;
        return string.Format(EskiAltinSozlugu.Al(dil).KaydetKilit, ilk);
    }

    /// <summary>
    /// B4-35 — fişte ALTIN bacağı varken Kaydet düğmesinin yanında kaç belge
    /// doğacağı KAYITTAN ÖNCE söylenir (panel metniyle aynı kaynak). Yoksa null.
    /// </summary>
    public static string? BelgeNotu(IReadOnlyCollection<FisBacagi> bacaklar, AltinBacagiAyarlari? ayarlar, string dil)
    {
        if (bacaklar.FirstOrDefault(AltinMi)?.Altin is not { } g) return null;
        var m = AltinBacagiSozlugu.Al(dil);
        return g.PusulaGerekir(ayarlar) ? m.IkiBelge : m.TekBelgeMukellef;
    }

    /// <summary>İki notu (iskonto, pusula) tek banda birleştirir; ikisi de yoksa null.</summary>
    public static string? NotBirlestir(string? a, string? b) =>
        (a, b) switch
        {
            (null, null) => null,
            ({ } x, null) => x,
            (null, { } y) => y,
            ({ } x, { } y) => $"{x} · {y}",
        };
}
