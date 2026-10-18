using System.Globalization;
using Kuyumcu.Contracts.Sayfalar.Cariler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Bilesenler.Yildiz;

/// <summary>PAKET YILDIZ-4B — Müşteriler listesinin bir satırı (tuval Musteri4 &lt;a class="sat"&gt;).</summary>
/// <param name="Id">Cari kimliği (satır id'si <c>yildiz-musteri-{Id}</c>).</param>
/// <param name="Ad">Ad (20 px · 400).</param>
/// <param name="Alt">Soluk ikinci sütun ("Toptancı · 0532 000 01 06").</param>
/// <param name="Tutar">TL bakiyesi (<see cref="Bicim.Tl"/>: "−3.840 ₺", "10.480,06 ₺", "0 ₺").</param>
/// <param name="Borc">Biz borçluyuz (eksi bakiye) → tutar kırmızı (tuval --kotu).</param>
/// <param name="Durum">Sağdaki soluk durum ("biz borçluyuz", "bize borçlu · 15 gün", "emanette 9,16 g has").</param>
/// <param name="Risk">D1-07 — risk rengi: "kirmizi" (kara liste ya da 30 günden çok gecikme) · "sari" (gecikme ya da
/// kredi limitinin %80'i) · null.</param>
public sealed record YildizMusteriSatiri(string Id, string Ad, string Alt, string Tutar, bool Borc, string Durum, string? Risk = null);

/// <summary>Süzgeç sekmesi (tuval HEPSİ / BORÇLU / TOPTANCI).</summary>
public sealed record YildizSekme(string Kod, string Etiket, bool Secili);

/// <summary>
/// PAKET YILDIZ-4B — Müşteriler ekranının bağlamı: Cariler.razor doldurur, YildizMusteriler çizer.
/// Bileşen İSTEK ATMAZ; her eylem sayfanın mevcut geri çağrısıdır (arama → Yukle, satır → kart,
/// "Yeni müşteri" → mevcut oluşturma penceresi).
/// </summary>
public sealed class YildizMusteriBaglami
{
    public string Ust { get; init; } = "";
    public IReadOnlyList<YildizMusteriSatiri> Satirlar { get; init; } = [];
    public IReadOnlyList<YildizSekme> Sekmeler { get; init; } = [];
    public string Arama { get; init; } = "";
    public bool Yukleniyor { get; init; }
    /// <summary>İstek başarılı ve hiç cari yok (UX-S28: hata ayrı bant).</summary>
    public bool HicYok { get; init; }
    /// <summary>İstek başarılı, süzgeçte kimse yok.</summary>
    public bool SuzgecBos { get; init; }
    /// <summary>YILDIZ-4C (hakem 4B tur 2): arama/süzgeç yanıtı boş — "Henüz müşteri yok" DENMEZ.</summary>
    public bool AramaBos { get; init; }
    public bool HataVar { get; init; }
    /// <summary>YILDIZ-4D: tür/pasif süzgeci BAĞLANTIDAN etkin (tuvalde süzgeç düğmesi yok; şerit yalnız bu durumda görünür).</summary>
    public bool SuzgecEtkin { get; init; }
    /// <summary>Sunucu sınırına değdi (UX-S29) — sınır cümlesi.</summary>
    public string? SinirNotu { get; init; }
    public EventCallback<string?> AramaDegisti { get; init; }
    public EventCallback<string> SekmeSec { get; init; }
    public EventCallback<string> SatirAc { get; init; }
    public EventCallback YeniMusteri { get; init; }
    /// <summary>Hata/başarı bantları + derin bağlantı süzgeci (sayfadan).</summary>
    public RenderFragment? Bantlar { get; init; }
    /// <summary>Tür/pasif süzgeci (sayfadan) — yalnız <see cref="SuzgecEtkin"/> iken çizilir.</summary>
    public RenderFragment? DigerSuzgecler { get; init; }
}

/// <summary>PAKET YILDIZ-4B — Müşteriler satır yazımı (saf; tuval Musteri4 yazımı).</summary>
public static class YildizMusteriBicim
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// Bir cari kartından tuval satırı. Bakiye işareti: artı = bize borçlu, eksi = biz borçluyuz
    /// (K4 — Cariler'in tek bakış açısı). Gün: vadesi GEÇMİŞSE "N gün geçti", vadeye kalan varsa
    /// "N gün" (YILDIZ-4A Bugün ile aynı söz); bilinmiyorsa yazılmaz (uydurulmaz).
    /// </summary>
    public static YildizMusteriSatiri Satir(CariVeri c, int? gecikmeGun, int? kalanGun, string telefon, string dil)
    {
        var alt = new List<string>();
        if (c.Segment == Kuyumcu.Core.Domain.CariSegmentleri.Toptan) alt.Add(YildizMusteriSozlugu.ToptanciAlt(dil));
        if (!string.IsNullOrWhiteSpace(telefon)) alt.Add(telefon);
        if (!c.Aktif) alt.Add(YildizMusteriSozlugu.Pasif(dil));

        var durum = new List<string>();
        var tl = decimal.Round(c.TlBakiye, 2, MidpointRounding.AwayFromZero);
        if (tl < 0m) durum.Add(YildizBugunSozlugu.BizBorcluyuz(dil));
        else if (tl > 0m)
        {
            durum.Add(YildizMusteriSozlugu.BizeBorclu(dil));
            if (gecikmeGun is int g and > 0) durum.Add(YildizBugunSozlugu.GunGecti(g, dil));
            else if (kalanGun is int k and > 0) durum.Add(YildizBugunSozlugu.Gun(k, dil));
        }
        var has = decimal.Round(c.HasGramBakiye, 2, MidpointRounding.AwayFromZero);
        if (has != 0m) durum.Add(YildizMusteriSozlugu.HasBakiye((has < 0m ? "−" : "") + Math.Abs(has).ToString("N2", Tr), dil));
        // P-DIL-BORC-KASA (Y18): emanet bırakıldığı gibi ("emanette 22 ayar 12,50 g"); ayar bilinmiyorsa has.
        if (c.EmanetteHas > 0m)
            durum.Add(c.EmanetBirakildigi is { Count: > 0 } bg
                ? EmanetDiliSozlugu.EmanetteDuz(bg, dil)
                : YildizMusteriSozlugu.Emanette(c.EmanetteHas.ToString("N2", Tr), dil));

        if (c.VeresiyeEngeli) durum.Insert(0, Kuyumcu.Core.I18n.D1Sozlugu.Al(dil).VeresiyeKapali);
        return new(c.Id, c.Ad, string.Join(" · ", alt), Bicim.Tl(c.TlBakiye), tl < 0m, string.Join(" · ", durum), Risk(c, gecikmeGun));
    }

    /// <summary>D1-07 — risk rengi (saf). Kara liste ya da 30+ gün gecikme kırmızı; gecikme ya da limitin %80'i sarı.</summary>
    public static string? Risk(CariVeri c, int? gecikmeGun)
    {
        if (c.VeresiyeEngeli || gecikmeGun is > 30) return "kirmizi";
        if (gecikmeGun is > 0) return "sari";
        if (c.KrediLimitiTl is > 0m and var limit && c.TlBakiye >= limit * 0.8m) return "sari";
        return null;
    }

    /// <summary>Üst satır sayıları: alacak = TL bakiyesi artı (bize borçlu), borç = eksi (biz borçluyuz).</summary>
    public static (int Alacak, int Borc) Sayilar(IEnumerable<CariVeri> liste)
    {
        int a = 0, b = 0;
        foreach (var c in liste)
        {
            var tl = decimal.Round(c.TlBakiye, 2, MidpointRounding.AwayFromZero);
            if (tl > 0m) a++;
            else if (tl < 0m) b++;
        }
        return (a, b);
    }
}
