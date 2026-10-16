namespace Kuyumcu.Core.Domain.Girdi;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET KUR-BANT (09.10) — K16 ek: ELLE GİRİLEN has / altın / döviz fiyatında
// MAKUL BANT. Kaynak: GOC-A kapanış devri (YÜKSEK, VERİ): manuel kurda has fiyat
// 5 ₺ ve 99.999.999 ₺ kaydediliyordu. Envanter (B) K16: "makul bant: son kurdan
// ± %20 sapma".
//
// KAYNAK SINIFI: İŞ KURALI (ekip oyu, fail-closed). Yasal parametre DEĞİLDİR —
// hiçbir mevzuat hükmüne dayanmaz; bir yazım hatası kapısıdır. Bu yüzden sınırlar
// YasalParametre tablosunda değil, burada TEK sabittir (ekran + sunucu aynı sayı).
//
// KURAL (sırayla; ikisi de uygulanır):
//   1) Mutlak sınır (her zaman): has / gram altın 500–50.000 TL, döviz 5–500 TL.
//   2) Önceki geçerli değer varsa: ondan en çok ± %20 sapma.
// Bant dışı değer KAYDEDİLMEZ. İKİ AYRI KAPI (KUR-BANT tur 2, doğrulayıcı ORTA):
//   · Mutlak sınır SERT kapıdır — onayla da AŞILAMAZ (has 5 TL hiçbir yoldan yazılmaz).
//   · Yalnız ± %20 sapma (mutlak sınırın İÇİNDE) SAHİP/MÜDÜR gerekçe yazıp "fiyat doğru"
//     onayıyla kaydedilebilir (denetim izi). Ayrım tek yerde: OnaylanabilirMi. Biçim/işaret/ondalık KurKurali'nindir —
// bu kural yalnız > 0 değere bakar (sıfır/eksi mesajı K16'dan gelir).
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Elle girilen fiyatın türü — mutlak sınırı belirler.</summary>
public enum FiyatTuru
{
    /// <summary>Has altın gram fiyatı (TL / has g).</summary>
    Has,
    /// <summary>Piyasa gram altın (24 ayar) fiyatı (TL / g).</summary>
    GramAltin,
    /// <summary>Döviz kuru (USD/EUR/GBP → TL).</summary>
    Doviz,
}

/// <summary>Elle girilen has/altın/döviz fiyatı için makul bant (İŞ KURALI).</summary>
public static class FiyatBandi
{
    /// <summary>Önceki geçerli değerden izin verilen en büyük sapma (yüzde).</summary>
    public const decimal SapmaYuzde = 20m;

    /// <summary>Has / gram altın mutlak alt sınırı (TL/g).</summary>
    public const decimal HasEnAz = 500m;
    /// <summary>Has / gram altın mutlak üst sınırı (TL/g).</summary>
    public const decimal HasEnCok = 50_000m;
    /// <summary>Döviz kuru mutlak alt sınırı (TL).</summary>
    public const decimal DovizEnAz = 5m;
    /// <summary>Döviz kuru mutlak üst sınırı (TL).</summary>
    public const decimal DovizEnCok = 500m;

    /// <summary>Türün mutlak alt-üst sınırı.</summary>
    public static (decimal EnAz, decimal EnCok) MutlakSinir(FiyatTuru tur) => tur switch
    {
        FiyatTuru.Doviz => (DovizEnAz, DovizEnCok),
        _ => (HasEnAz, HasEnCok),
    };

    /// <summary>
    /// Bu hata "fiyat doğru" onayıyla geçilebilir mi? YALNIZ ± %20 sapma (FiyatBantDisi).
    /// Mutlak sınır dışı (FiyatMutlakDisi) sert kapıdır: hiçbir rol, hiçbir gerekçe geçemez.
    /// Ekran (Kur.razor) ve sunucu (KurServisi) AYNI karar için bunu çağırır.
    /// </summary>
    public static bool OnaylanabilirMi(GirdiHatasi h) =>
        h.Kod is GirdiKodu.FiyatBantDisi or GirdiKodu.IslemFiyatBantDisi;   // ISLEM-BANT: işlem bandı da yalnız onayla

    /// <summary>
    /// Bant denetimi. null / ≤ 0 değer → null (o hata K16'nındır). Önce mutlak sınır,
    /// sonra (önceki geçerli değer verildiyse) ± %20 bant.
    /// </summary>
    public static GirdiHatasi? Denetle(decimal? deger, FiyatTuru tur, decimal? onceki)
    {
        if (deger is not { } x || x <= 0) return null;
        var (enAz, enCok) = MutlakSinir(tur);
        if (x < enAz || x > enCok) return new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, enAz, enCok);
        if (onceki is { } o && o > 0)
        {
            var oran = SapmaYuzde / 100m;
            if (x < o * (1 - oran) || x > o * (1 + oran))
                return new GirdiHatasi(GirdiKodu.FiyatBantDisi, o, SapmaYuzde);
        }
        return null;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PAKET ISLEM-BANT (09.10) — İŞLEM İÇİ elle yazılan HAS GRAM FİYATI.
    // Kaynak: KUR-BANT § 0.6 devri (YÜKSEK, VERİ): hurda alışta has gram başına 5 TL
    // kabul ediliyor, ödeme kasadan çıkıyordu. İŞ KURALI (yazım hatası kapısı) — yasal
    // parametre DEĞİL. Referans = o anki GEÇERLİ has fiyatı (son iptal edilmemiş kur kaydı).
    //   1) Mutlak sınır (SERT, onayla aşılamaz): HasEnAz–HasEnCok (kur ekranıyla AYNI sabit).
    //   2) Referansa göre oran bandı (yalnız SAHİP/MÜDÜR onay + gerekçe ile aşılır):
    //      · Dükkânın ALDIĞI eski altın (hurda alış, takasta alınan, eski altınla ödeme):
    //        has fiyatının %50–%110'u (ekip oyu: alış fiyatı piyasanın ALTINDA olağandır;
    //        ± %20'nin alt ucu hurdada gerçek işlemleri durdururdu).
    //      · Dükkânın SATTIĞI hurda (ramatçıya): kur ekranıyla aynı ± %20 (%80–%120).
    //   Yalnız ALTIN maden: gümüş/platin için güncel has referansı yok (kur kaydı altındır).
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Hurda alış / takas / eski altınla ödeme: has fiyatının en az yüzdesi.</summary>
    public const decimal AlisAltYuzde = 50m;
    /// <summary>Hurda alış / takas / eski altınla ödeme: has fiyatının en çok yüzdesi.</summary>
    public const decimal AlisUstYuzde = 110m;

    /// <summary>İşlem bağlamının referans has fiyatına göre bandı (yüzde, alt–üst).</summary>
    public static (decimal AltYuzde, decimal UstYuzde) IslemBandi(IslemFiyatBaglami b) => b switch
    {
        IslemFiyatBaglami.HurdaSatis => (100m - SapmaYuzde, 100m + SapmaYuzde),
        _ => (AlisAltYuzde, AlisUstYuzde),
    };

    /// <summary>Bant uçları TL olarak (ekran ve ileti için iki ondalık; alt yukarı, üst aşağı yuvarlanır ki yazılan uç kabul edilsin).</summary>
    public static (decimal EnAz, decimal EnCok) IslemBantUclari(IslemFiyatBaglami b, decimal referansHas)
    {
        var (a, u) = IslemBandi(b);
        return (Math.Ceiling(referansHas * a) / 100m, Math.Floor(referansHas * u) / 100m);
    }

    /// <summary>
    /// İşlem içi has gram fiyatı denetimi. null / ≤ 0 → null (o hata alanın kendi kuralının).
    /// Maden ALTIN değilse null (referans yok). Önce mutlak sınır, sonra (referans varsa) oran bandı.
    /// Ekran (Fis bileşenleri, Hurda.razor) ve sunucu (IslemFiyatKapisi) AYNI çağrıyı yapar.
    /// </summary>
    public static GirdiHatasi? IslemDenetle(decimal? fiyat, IslemFiyatBaglami b, decimal? referansHas, string? maden)
    {
        if (fiyat is not { } x || x <= 0) return null;
        if (maden != Kuyumcu.Core.Domain.Maden.Altin) return null;
        if (x < HasEnAz || x > HasEnCok) return new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, HasEnAz, HasEnCok);
        if (referansHas is { } r && r > 0)
        {
            var (enAz, enCok) = IslemBantUclari(b, r);
            if (x < enAz || x > enCok) return new GirdiHatasi(GirdiKodu.IslemFiyatBantDisi, enAz, enCok);
        }
        return null;
    }
}

/// <summary>PAKET ISLEM-BANT — elle has gram fiyatı yazılan işlem türü (bandı belirler).</summary>
public enum IslemFiyatBaglami
{
    /// <summary>Müşteriden hurda alış.</summary>
    HurdaAlis,
    /// <summary>Ramatçıya / rafineriye hurda satışı.</summary>
    HurdaSatis,
    /// <summary>Takasta alınan eski ürün.</summary>
    Takas,
    /// <summary>Satış fişinde eski altınla ödeme bacağı.</summary>
    EskiAltinOdeme,
}
