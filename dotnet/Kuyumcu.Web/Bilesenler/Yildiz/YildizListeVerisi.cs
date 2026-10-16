using System.Globalization;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Bilesenler.Yildiz;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5A — "liste" kalıbı (tuval Y_Emanet · Y_Siparisvetamir · Y_Bildirimler ·
// Y_Urunler · Y_Urunagaci · Y_Fiyatlisteleri): üst satır + 64 px ince başlık, ortada
// 420 px alt çizgili arama, sağda TEK beyaz hap; altında sekmeler (.sek); sütun başlığı
// (.sat 10 px dolgu, .ust 0,14em) ve kıl çizgili satırlar (.sat: ad 19/400 · değer 17/300
// · soluk 17/300); sağda isteğe bağlı satır eylemi (.b3); en altta soluk not.
// Bileşen İSTEK ATMAZ — her eylem sayfanın MEVCUT geri çağrısıdır (davranış değişmez).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Hücre yazı türü (tuval satır hücreleri).</summary>
public enum YildizHucreTuru
{
    /// <summary>İlk sütun: 19 px · 400 · metin rengi.</summary>
    Ad,
    /// <summary>Soluk değer: 17 px · 300 · --s (tuval class="s").</summary>
    Soluk,
    /// <summary>Değer: 17 px · 300 · metin rengi (tuval class="").</summary>
    Deger,
    /// <summary>Kötü durum değeri: 17 px · 300 · --kotu.</summary>
    Kotu,
    /// <summary>Değer hücresi, metni İÇ span'da --kotu (tuval Y_Sayim: &lt;span class=""&gt;&lt;span style="color:var(--kotu)"&gt;−1&lt;/span&gt;&lt;/span&gt;).</summary>
    IcKotu,
    /// <summary>Değer hücresi, metni İÇ span'da soluk (tuval Y_Sayim: &lt;span class=""&gt;&lt;span class="s"&gt;?&lt;/span&gt;&lt;/span&gt;).</summary>
    IcSoluk,
}

/// <summary>Bir satır hücresi.</summary>
public sealed record YildizHucre(string Metin, YildizHucreTuru Tur = YildizHucreTuru.Soluk, string? Id = null);

/// <summary>Sütun başlığı; <paramref name="Sag"/> = sağa yaslı (sayı).</summary>
public sealed record YildizSutun(string Baslik, bool Sag = false);

/// <summary>
/// Satır eylemi (tuval .b3). Ya <paramref name="Tikla"/> (sayfanın geri çağrısı) ya da <paramref name="Href"/> (gezinme);
/// <paramref name="KilitSebebi"/> doluysa düğme kilitli ve sebebi title + aria'da (UX-S30).
/// </summary>
public sealed record YildizEylem(string Etiket, string Id, EventCallback Tikla = default, string? Href = null, string? KilitSebebi = null);

/// <summary>Liste satırı. <paramref name="Ac"/> doluysa ilk hücre satırı açan düğmedir (sayfanın mevcut düzenleme yolu).</summary>
public sealed record YildizListeSatiri(string Id, IReadOnlyList<YildizHucre> Hucreler, YildizEylem? Eylem = null,
                                       EventCallback Ac = default, bool Soluk = false, string? AcEtiketi = null);

/// <summary>PAKET YILDIZ-5A — liste ekranının bağlamı: sayfa doldurur, <see cref="YildizListe"/> çizer.</summary>
public sealed class YildizListeBaglami
{
    /// <summary>Kök kimlik (ör. "yildiz-emanet"); alt öğeler "{Kok}-ust", "{Kok}-baslik", "{Kok}-liste", "{Kok}-not".</summary>
    public string Kok { get; init; } = "yildiz-liste";
    /// <summary>Ekran sınıfı soneki (ızgara sütunları tema-yildiz.css'te: .yz-liste--{Tur}).</summary>
    public string Tur { get; init; } = "";
    public string Ust { get; init; } = "";
    public string Baslik { get; init; } = "";

    // Arama (yoksa çizilmez). Kimlik sayfanın MEVCUT arama kimliğidir.
    public string? AramaId { get; init; }
    public string AramaEtiket { get; init; } = "";
    public string AramaYerTutucu { get; init; } = "";
    public string? Arama { get; init; }
    public EventCallback<string?> AramaDegisti { get; init; }
    public EventCallback AramaEnter { get; init; }

    // Tek birincil hap (yoksa çizilmez). Kimlik sayfanın MEVCUT birincil düğme kimliğidir.
    public string? B1Etiket { get; init; }
    public string B1Id { get; init; } = "";
    public EventCallback B1Tikla { get; init; }
    public string? B1KilitSebebi { get; init; }

    // Sekmeler (yoksa çizilmez).
    public string SekmelerId { get; init; } = "";
    public IReadOnlyList<YildizSekme> Sekmeler { get; init; } = [];
    public EventCallback<string> SekmeSec { get; init; }
    /// <summary>Sekme düğmesi kimliği öneki ("{Onek}{Kod}"); boşsa "{Kok}-sekme-".</summary>
    public string? SekmeIdOneki { get; init; }

    public IReadOnlyList<YildizSutun> Sutunlar { get; init; } = [];
    /// <summary>Son sütun satır eylemidir (başlıkta boş hücre — tuval &lt;span&gt;&lt;/span&gt;).</summary>
    public bool EylemSutunu { get; init; }
    public IReadOnlyList<YildizListeSatiri> Satirlar { get; init; } = [];

    public bool Yukleniyor { get; init; }
    /// <summary>İstek BAŞARILI ve sonuç boşken yazılacak tek cümle (UX-S28: hata ayrı bant, boş durum değil).</summary>
    public string? BosMetni { get; init; }
    /// <summary>Sunucu sınırına değdi (UX-S29) — sınır cümlesi.</summary>
    public string? SinirNotu { get; init; }
    /// <summary>Tuvalin alt satırı (soluk not).</summary>
    public string? Not { get; init; }
    /// <summary>Hata/başarı bantları (sayfanın mevcut bantları).</summary>
    public RenderFragment? Bantlar { get; init; }
    /// <summary>
    /// "+ Tüm ayrıntılar" (tuval .ekle kalıbı, notun sağında): ekranın özgün tüm özellikleri (süzgeç, ikinci
    /// liste, iş ayarları …) tuvalde yok — özellik SİLİNMEZ, bu bağlantının arkasındadır. Boşsa çizilmez.
    /// </summary>
    public EventCallback AyrintiAc { get; init; }
    /// <summary>Ayrıntı görünümü açık: liste yerine kısa başlık + "Listeye dön" (b2); sayfanın özgün içeriği altında.</summary>
    public bool AyrintiAcik { get; init; }
    public EventCallback AyrintiKapat { get; init; }
}

/// <summary>PAKET YILDIZ-5A — liste ekranlarının ortak yazımı (saf).</summary>
public static class YildizListeBicim
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Ekranın Yıldız adı = "Tüm ekranlar" karesinin adı (menü adı, UX-S31 tek kaynak; tuvaldeki başlık).</summary>
    public static string EkranAdi(string yol, DilServisi dil)
    {
        var oge = Kuyumcu.Web.Layout.MenuTanimi.Ogeler.FirstOrDefault(o => o.Yol == yol);
        return oge is null ? yol : YildizEkranSozlugu.KareAdi(yol, oge.Etiket(dil.T), dil.Dil);
    }

    /// <summary>Tuvaldeki büyük harf (sekme/sütun başlığı) — Türkçe "i → İ" kuralıyla.</summary>
    public static string Buyuk(string metin, string dil) =>
        dil == "en" ? metin.ToUpperInvariant() : metin.ToUpper(Tr);

    private static IFormatProvider Kultur(string dil) => dil == "en" ? CultureInfo.InvariantCulture : Tr;

    /// <summary>Tuval tarih kalıbı: "2 Eyl 2026" (tr) · "2 Sep 2026" (en) — sunucu anı işletme gününe çevrilir (TARIH-TEK).</summary>
    public static string Gun(DateTime an, string dil) =>
        Bicim.GunBicim(DateOnly.FromDateTime(Bicim.IsletmeZamani(an)), "d MMM yyyy", Kultur(dil));

    /// <summary>Tuval kısa gün: "14 Eki" · bugün ise "Bugün" (takvim günü, çevrilmez).</summary>
    public static string KisaGun(DateOnly g, DateOnly bugun, string dil) =>
        g == bugun ? YildizListeSozlugu.Bugun(dil) : Bicim.GunBicim(g, "d MMM", Kultur(dil));

    /// <summary>Tuval "Bugün 09:00" · "Dün 21:00" · "8 Eki 09:00" — sunucu anı işletme saatine çevrilir.</summary>
    public static string GunSaat(DateTime an, DateOnly bugun, string dil)
    {
        var yerel = Bicim.IsletmeZamani(an);
        var g = DateOnly.FromDateTime(yerel);
        var saat = Bicim.Saat(yerel);
        if (g == bugun) return YildizListeSozlugu.Bugun(dil) + " " + saat;
        if (g == bugun.AddDays(-1)) return YildizListeSozlugu.Dun(dil) + " " + saat;
        return Bicim.GunBicim(g, "d MMM", Kultur(dil)) + " " + saat;
    }

    /// <summary>Has gram, tuval "9,1600 g" (dört ondalık).</summary>
    public static string HasDort(decimal gram, string dil) =>
        gram.ToString("N4", dil == "en" ? CultureInfo.InvariantCulture : Tr) + " g";

    /// <summary>Has gram, tuval "8,66 g" (iki ondalık); 0 → "—".</summary>
    public static string HasIki(decimal gram, string dil) =>
        gram == 0m ? "—" : gram.ToString("N2", dil == "en" ? CultureInfo.InvariantCulture : Tr) + " g";

    /// <summary>Gram, tuval "15,20".</summary>
    public static string Gram(decimal gram, string dil) =>
        gram.ToString("N2", dil == "en" ? CultureInfo.InvariantCulture : Tr);

    /// <summary>Adet/sayı, tuval "2" · "13" (binlik ayraçlı tam sayı; kesirliyse iki ondalık).</summary>
    public static string Adet(decimal adet, string dil) =>
        adet == decimal.Truncate(adet)
            ? adet.ToString("N0", dil == "en" ? CultureInfo.InvariantCulture : Tr)
            : adet.ToString("N2", dil == "en" ? CultureInfo.InvariantCulture : Tr);

    /// <summary>Fiyat, tuval "6.922 ₺" — tam lira (ortak biçim <see cref="Bicim.TlTam"/>; TL fiyat teklifi).</summary>
    public static string Lira(decimal tutar) => Bicim.TlTam(tutar);

    /// <summary>Yüzde, tuval "%8" (tr) · "8%" (en).</summary>
    public static string Yuzde(decimal y, string dil)
    {
        var s = y.ToString("0.##", dil == "en" ? CultureInfo.InvariantCulture : Tr);
        return dil == "en" ? s + "%" : "%" + s;
    }

    /// <summary>İşaretli adet farkı, tuval "−1" · "0" · "+2" (eksi U+2212).</summary>
    public static string Fark(decimal f, string dil)
    {
        var s = Adet(Math.Abs(f), dil);
        return f < 0m ? "−" + s : f > 0m ? "+" + s : s;
    }

    /// <summary>İşaretli has gram, tuval "−3,21 g".</summary>
    public static string HasFark(decimal g, string dil)
    {
        var s = Math.Abs(g).ToString("N2", dil == "en" ? CultureInfo.InvariantCulture : Tr) + " g";
        return g < 0m ? "−" + s : g > 0m ? "+" + s : s;
    }
}
