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

    private bool YildizMi => Kuyumcu.Web.Layout.YildizKabuk.Mi(Hizmetler);
    private bool YildizOzgunCiz => !YildizMi || yildizAyrinti;
    private string? YildizOzgunIsaret => YildizMi ? "kur" : null;
    private bool yildizAyrinti;

    private static readonly CultureInfo YildizTr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Tuval "6.656 ₺" — tam lira (üst şeritle aynı yuvarlama).</summary>
    public static string YildizLira(decimal? n) => n is { } d ? YildizBugunBicim.KurTam(d) + " ₺" : "—";

    /// <summary>Tuval "49,16 ₺" — döviz iki ondalık.</summary>
    /// <summary>Kur tablosunda kaynaktan gösterilen kalemler (sıra = ekran sırası; has ayrı satırda).</summary>
    private static readonly string[] YildizKaynakSirasi =
        ["GRAM", "BILEZIK_22", "HURDA_22", "AYAR_18", "AYAR_14", "CEYREK_YENI", "YARIM_YENI", "TAM_YENI", "ATA_YENI", "GUMUS"];

    /// <summary>Kaynak fiyatı: 1.000 ₺ altı (gümüş) kuruşlu, üstü tam lira.</summary>
    public static string YildizKaynakFiyat(decimal? n) => n is { } d && d < 1000m ? Bicim.TlKurus(d) : YildizLira(n);

    public static string YildizDoviz(decimal? n) => n is { } d ? Bicim.TlKurus(d) : "—";

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
            var has = pariteler.FirstOrDefault(p => p.Kod == "HAS");
            var g22 = g?.AyarFiyatlari?.FirstOrDefault(a => a.AyarId == Kuyumcu.Web.Layout.UstBar.Ayar22)?.GramFiyati;
            YildizListeSatiri Satir(string id, string ad, string alis, string satis) => new(id,
            [
                new(ad, YildizHucreTuru.Ad, $"yildiz-kur-ad-{id}"),
                new(alis, YildizHucreTuru.Deger, $"yildiz-kur-alis-{id}"),
                new(satis, YildizHucreTuru.Deger, $"yildiz-kur-satis-{id}"),
            ]);
            return new YildizOzetBaglami
            {
                Kok = "yildiz-kur", Tur = "kur",
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
                    Satir("has", YildizKurSozlugu.HasAltin(dil), YildizLira(has?.Alis), YildizLira(has?.Satis ?? g.AltinHasFiyati)),
                    Satir("22", YildizKurSozlugu.Ayar22Gram(dil), "—", YildizLira(g22)),
                    // KURUCU 15.10 "alış fiyatları neden gelmiyor · liste biraz uzun olabilir": kaynağın
                    // yayınladığı alış/satış (GET /kur/pariteler — salt okunur, hesapta KULLANILMAZ).
                    .. pariteler.Where(p => YildizKaynakSirasi.Contains(p.Kod))
                                .OrderBy(p => Array.IndexOf(YildizKaynakSirasi, p.Kod))
                                .Select(p => Satir(p.Kod.ToLowerInvariant(), p.Ad, YildizKaynakFiyat(p.Alis), YildizKaynakFiyat(p.Satis))),
                    Satir("usd", "USD", "—", YildizDoviz(g.UsdTry)),
                    Satir("eur", "EUR", "—", YildizDoviz(g.EurTry)),
                ],
                Bilgi = g is null ? [] :
                [
                    (YildizKurSozlugu.Kaynak(dil), kaynak ?? "—"),
                    (YildizKurSozlugu.SonSorgu(dil), saat ?? "—"),
                    (YildizKurSozlugu.GunFarki(dil), YildizGunFarki(g, gecmis, dil) ?? "—"),
                ],
                Bantlar = YildizBant.Ciz(guncelHata ?? pariteHata ?? kaynakHata, basariMesaji, basariJeton, 3000),
                AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                AyrintiAcik = yildizAyrinti, AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
                DonEtiketi = YildizParaSozlugu.EkranaDon(dil),
            };
        }
    }
}
