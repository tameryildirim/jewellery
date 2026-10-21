using System.Globalization;
using Kuyumcu.Contracts.Sayfalar.Kur;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5B — Kur "Yıldız" yüzü (tuval Y_Kur · BİREBİR · özet kalıbı).
// Yalnız GÖRÜNÜM + OKUMA: büyük rakam = güncel has satış (GET /kur/guncel, üst şeritle AYNI
// tam lira biçimi), satırlar = kaynağın has alış/satışı (GET /kur/pariteler) + 22 ayar gram
// (ayar fiyatı) + USD/EUR; gün farkı = önceki günün son kaydına göre (GET /kur/gecmis, sayfa
// zaten okuyor). "Şimdi güncelle" MEVCUT SimdiCek, "Elle gir" MEVCUT manuel çekmece.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Kur
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private bool YildizOzgunCiz => yildizAyrinti;
    private string? YildizOzgunIsaret => "kur";
    private bool yildizAyrinti;

    private static readonly CultureInfo YildizTr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Tuval "6.656 ₺" — tam lira (üst şeritle aynı yuvarlama).</summary>
    public static string YildizLira(decimal? n) => n is { } d ? YildizBugunBicim.KurTam(d) + " ₺" : "—";

    // 21.10 (kurucu: "eski yeni tam liste gelmesi lazım · ürünler birkaç yerde farklı liste geliyor"): kur tablosu artık
    // 10 kalemlik sabit süzgeç DEĞİL — kaynağın yayınladığı TÜM kalemler, sıra ve ad Core FiyatKalemleri'nden (tek kaynak).
    // Elle eklenen "22 ayar gram" satırı (has × milyem — dördüncü bir "22 ayar" adı) tablodan çıktı: 22 ayar satış hesabı
    // üst şeritte yazar. Döviz (USD/EUR/GBP) alış + satışla tablonun SONUNDA (kurucu 21.10 "döviz alışını da ekle").

    /// <summary>Kaynak fiyatı: 1.000 ₺ altı (gümüş) kuruşlu, üstü tam lira.</summary>
    public static string YildizKaynakFiyat(decimal? n) => n is { } d && d < 1000m ? Bicim.TlKurus(d) : YildizLira(n);

    /// <summary>Kalemin para birimiyle (21.10: ONS ons başına DOLAR — "4.126 ₺" yazıyordu).</summary>
    public static string YildizKaynakFiyat(decimal? n, string? kod) =>
        Kuyumcu.Core.Domain.FiyatKalemleri.Para(kod) is "TRY" || n is null ? YildizKaynakFiyat(n) : Bicim.Para(n.Value, Kuyumcu.Core.Domain.FiyatKalemleri.Para(kod));

    public static string YildizDoviz(decimal? n) => n is { } d ? Bicim.TlKurus(d) : "—";

    /// <summary>Gün farkı (saf): sunucunun verdiği önceki işletme günü has fiyatına göre yüzde (GET /kur/pariteler).</summary>
    public static string? YildizGunFarki(decimal? guncelHas, decimal? oncekiGunHas, string dil)
    {
        if (guncelHas is not { } g || oncekiGunHas is not { } o || o <= 0m) return null;
        var y = Math.Round((g - o) / o * 100m, 1, MidpointRounding.AwayFromZero);
        var ok = y > 0m ? "▲ " : y < 0m ? "▼ " : "";
        return ok + Math.Abs(y).ToString("0.0", dil == "en" ? CultureInfo.InvariantCulture : YildizTr) + "%";
    }

    /// <summary>Gün farkı (saf): önceki işletme gününün son geçerli kaydına göre yüzde; tuval "▲ 0,3%" · "▼ 0,3%".</summary>
    public static string? YildizGunFarki(KurVeri? guncel, IEnumerable<KurVeri> gecmis, string dil)
    {
        if (guncel is null) return null;
        var bugun = Bicim.IsletmeGunu(guncel.Olusturuldu);
        var once = gecmis.Where(k => !k.IptalEdildi && Bicim.IsletmeGunu(k.Olusturuldu) < bugun && k.AltinHasFiyati > 0m)
                         .OrderByDescending(k => k.Olusturuldu).FirstOrDefault();
        if (once is null) return null;
        var y = Math.Round((guncel.AltinHasFiyati - once.AltinHasFiyati) / once.AltinHasFiyati * 100m, 1, MidpointRounding.AwayFromZero);
        var ok = y > 0m ? "▲ " : y < 0m ? "▼ " : "";
        return ok + Math.Abs(y).ToString("0.0", dil == "en" ? CultureInfo.InvariantCulture : YildizTr) + "%";
    }

    private YildizOzetBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var baslik = YildizListeBicim.EkranAdi("/kur", Dil);
            var g = guncel is { IptalEdildi: false } ? guncel : null;
            var saat = g is null ? null : Bicim.IsletmeSaati(g.Olusturuldu);
            var kaynak = g is null ? null : KaynakAdi(g.Kaynak);
            var hasVar = pariteler.Any(p => p.Kod == "HAS");
            YildizListeSatiri Satir(string id, string ad, string alis, string satis) => new(id,
            [
                new(ad, YildizHucreTuru.Ad, $"yildiz-kur-ad-{id}"),
                new(alis, YildizHucreTuru.Deger, $"yildiz-kur-alis-{id}"),
                new(satis, YildizHucreTuru.Deger, $"yildiz-kur-satis-{id}"),
            ]);
            return new YildizOzetBaglami
            {
                Kok = "yildiz-kurlar", Tur = "kur",   // D-13: "yildiz-kur" üst şeridin kimliği — satır kimlikleri (has, 22) çakışıyordu
                Ust = string.Join(" · ", new[] { YildizParaSozlugu.ParaBanka(dil), kaynak, saat }.Where(x => !string.IsNullOrEmpty(x))),
                Baslik = baslik,
                Rakam = g is null ? null : YildizBugunBicim.KurTam(g.AltinHasFiyati),
                RakamEki = $" ₺ {YildizParaSozlugu.Has(dil)}",
                Cumle = g is null && guncelHata is null ? YildizKurSozlugu.KurYok(dil) : YildizKurSozlugu.Cumle(dil),
                B1Etiket = YildizKurSozlugu.SimdiGuncelle(dil), B1Id = "yildiz-kur-b1",
                B1Tikla = EventCallback.Factory.Create(this, SimdiCek),
                B1Kilit = CekmeKapali ? Dil.T.Kur.CekmeKapaliSebep : null,
                B2Etiket = YildizKurSozlugu.ElleGir(dil), B2Id = "yildiz-kur-b2",
                B2Tikla = EventCallback.Factory.Create(this, ManuelModalAc),
                B2Kilit = kaydediliyor ? Dil.T.Kur.Cekiliyor : null,
                Sutunlar = [new(YildizKurSozlugu.Birim(dil)), new(YildizKurSozlugu.Alis(dil), Sag: true), new(YildizKurSozlugu.Satis(dil), Sag: true)],
                Satirlar = g is null ? [] :
                [
                    // Elle kur (MANUAL) kaynağında kalem listesi yoktur: has satırı güncel kayıttan (alışı yok → yalnız satış).
                    .. hasVar ? Array.Empty<YildizListeSatiri>() : [Satir("has", Kuyumcu.Core.Domain.FiyatKalemleri.Ad("HAS", dil), "—", YildizLira(g.AltinHasFiyati))],
                    // KURUCU 15.10 "alış fiyatları neden gelmiyor": kaynağın yayınladığı alış/satış (salt okunur, hesapta KULLANILMAZ).
                    .. pariteler.OrderBy(p => Kuyumcu.Core.Domain.FiyatKalemleri.Sira(p.Kod))
                                .Select(p => Satir(p.Kod.ToLowerInvariant(), Kuyumcu.Core.Domain.FiyatKalemleri.Ad(p.Kod, dil, p.Ad),
                                                   YildizKaynakFiyat(p.Alis, p.Kod), YildizKaynakFiyat(p.Satis, p.Kod))),
                    .. new[] { ("usd", "USD", g.UsdAlis, g.UsdTry), ("eur", "EUR", g.EurAlis, g.EurTry), ("gbp", "GBP", g.GbpAlis, g.GbpTry) }
                        .Where(d => d.Item3 is not null || d.Item4 is not null)
                        .Select(d => Satir(d.Item1, d.Item2, YildizDoviz(d.Item3), YildizDoviz(d.Item4))),
                ],
                Bilgi = g is null ? [] :
                [
                    (YildizKurSozlugu.Kaynak(dil), kaynak ?? "—"),
                    (YildizKurSozlugu.SonSorgu(dil), saat ?? "—"),
                    (YildizKurSozlugu.GunFarki(dil), YildizGunFarki(g.AltinHasFiyati, oncekiGunHas, dil) ?? "—"),
                ],
                Bantlar = YildizBant.Ciz(guncelHata ?? pariteHata ?? kaynakHata, basariMesaji, basariJeton, 3000),
                AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                AyrintiAcik = yildizAyrinti, AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
                DonEtiketi = YildizParaSozlugu.EkranaDon(dil),
            };
        }
    }
}
