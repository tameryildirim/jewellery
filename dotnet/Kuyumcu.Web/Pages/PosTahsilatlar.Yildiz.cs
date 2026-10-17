using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5B — POS alacakları "Yıldız" yüzü (tuval Y_POSalacaklari · BİREBİR · liste kalıbı).
// Yalnız GÖRÜNÜM + OKUMA: satırlar sayfanın MEVCUT tahsilat listesinin bankaya geçmeyi
// bekleyenleridir (GET /pos/tahsilatlar; durum BEKLİYOR/KISMİ, kalan > 0). Eşleştirme, iptal,
// geri alma, valör takvimi ve süzgeçler "Tüm ayrıntılar"da (özgün ekran).
// ═════════════════════════════════════════════════════════════════════════════
public partial class PosTahsilatlar
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private bool YildizOzgunCiz => yildizAyrinti;
    private string? YildizOzgunIsaret => "pos-tahsilatlar";
    private bool yildizAyrinti;

    /// <summary>Bankaya geçmeyi bekleyen satır mı (saf).</summary>
    public static bool YildizBekliyor(PosTahsilatSatir t) => t.Durum is "BEKLIYOR" or "KISMI" && t.KalanTutar > 0m;

    private YildizListeBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var bugun = DateOnly.FromDateTime(Bicim.IsletmeBugun);
            var satirlar = (sayfa?.Kayitlar ?? []).Where(YildizBekliyor).OrderBy(t => t.ValorTarihi).ToList();
            var paraBirimleri = satirlar.Select(t => t.ParaBirimi).Distinct().ToList();
            var toplam = paraBirimleri.Count == 1
                ? YildizParaBicim.Tutar(satirlar.Sum(t => t.BrutTutar), paraBirimleri[0])
                : string.Join(" + ", paraBirimleri.Select(p => YildizParaBicim.Tutar(satirlar.Where(t => t.ParaBirimi == p).Sum(t => t.BrutTutar), p)));
            return new YildizListeBaglami
            {
                Kok = "yildiz-posalacak", Tur = "posalacak",
                Ust = YildizPosAlacakSozlugu.Ust(dil),
                Baslik = YildizListeBicim.EkranAdi("/pos/tahsilatlar", Dil),
                Sutunlar = [new(YildizPosAlacakSozlugu.Pos(dil)), new(YildizPosAlacakSozlugu.Banka(dil)), new(YildizParaSozlugu.Tutar(dil), Sag: true), new(YildizPosAlacakSozlugu.Bankaya(dil))],
                Satirlar = [.. satirlar.Select(t => new YildizListeSatiri(t.Id,
                [
                    new(t.CihazAd, YildizHucreTuru.Ad, $"yildiz-posalacak-ad-{t.Id}"),
                    new(t.BankaAdi),
                    new(YildizParaBicim.Tutar(t.BrutTutar, t.ParaBirimi), YildizHucreTuru.Deger, $"yildiz-posalacak-tutar-{t.Id}"),
                    new(YildizListeBicim.KisaGun(DateOnly.FromDateTime(t.ValorTarihi), bugun, dil), YildizHucreTuru.Soluk, $"yildiz-posalacak-valor-{t.Id}"),
                ]))],
                Yukleniyor = yukleniyor,
                BosMetni = !yukleniyor && veriGeldi && hata is null ? YildizPosAlacakSozlugu.Bos(dil) : null,
                SinirNotu = sayfa is { SinirDegdi: true } s ? YildizPosAlacakSozlugu.Sinir(s.Limit, dil) : null,
                Not = satirlar.Count > 0 ? YildizPosAlacakSozlugu.Not(toplam, dil) : null,
                Bantlar = YildizBant.Ciz(hata, basari, 0, hataId: "pos-t-hata", basariId: "pos-t-basari"),
                AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                AyrintiAcik = yildizAyrinti,
                AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
            };
        }
    }
}
