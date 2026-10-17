using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5B — POS cihazları "Yıldız" yüzü (tuval Y_POScihazlari · BİREBİR · boş durum kalıbı;
// dolu liste tuvalde çizilmedi → liste kalıbı, belirsiz eşleşme). Yalnız GÖRÜNÜM + OKUMA:
// satırlar sayfanın MEVCUT cihaz listesidir (GET /pos/cihazlar). "POS ekle" MEVCUT YeniAc,
// "Düzenle" MEVCUT DuzenleAc, "Aktife al" MEVCUT Aktiflestir (çekmeceler ORTAK); taksit oranları,
// kasa göçü ve pasife alma "Tüm ayrıntılar"da.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Pos
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private bool YildizOzgunCiz => yildizAyrinti;
    private string? YildizOzgunIsaret => "pos";
    private bool yildizAyrinti;

    /// <summary>Boş durum yalnız istek BAŞARILI ve cihaz gerçekten yokken (UX-S28).</summary>
    private bool YildizBosMu => !yildizAyrinti && !yukleniyor && hata is null && cihazlar.Count == 0;

    private YildizBosBaglami YildizBosBaglami => new()
    {
        Kok = "yildiz-pos",
        Ust = YildizPosSozlugu.Ust(Dil.Dil),
        Baslik = YildizListeBicim.EkranAdi("/pos", Dil),
        BosBaslik = YildizPosSozlugu.BosBaslik(Dil.Dil),
        BosMetin = YildizPosSozlugu.BosMetin(Dil.Dil),
        B1Etiket = YildizPosSozlugu.PosEkle(Dil.Dil), B1Id = "yildiz-pos-b1",
        B1Tikla = EventCallback.Factory.Create(this, YeniAc),
        Bantlar = YildizBant.Ciz(hata, basari, 0, hataId: "pos-hata", basariId: "pos-basari"),
    };

    private YildizListeBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            return new YildizListeBaglami
            {
                Kok = "yildiz-pos", Tur = "pos",
                Ust = YildizPosSozlugu.Ust(dil),
                Baslik = YildizListeBicim.EkranAdi("/pos", Dil),
                B1Etiket = YildizPosSozlugu.PosEkle(dil), B1Id = "yildiz-pos-b1",
                B1Tikla = EventCallback.Factory.Create(this, YeniAc),
                Sutunlar = [new(YildizPosSozlugu.Cihaz(dil)), new(YildizPosSozlugu.Banka(dil)), new(YildizPosSozlugu.Bekleyen(dil), Sag: true), new(YildizPosSozlugu.Komisyon(dil), Sag: true)],
                EylemSutunu = true,
                Satirlar = [.. cihazlar.Select(c => new YildizListeSatiri(c.Id,
                [
                    new(c.Ad, YildizHucreTuru.Ad, $"yildiz-pos-ad-{c.Id}"),
                    new(c.Aktif ? c.BankaAdi : $"{c.BankaAdi} · {YildizPosSozlugu.KullanimdaDegil(dil)}"),
                    new(YildizParaBicim.Tutar(c.BekleyenAlacak, c.ParaBirimi), YildizHucreTuru.Deger, $"yildiz-pos-bekleyen-{c.Id}"),
                    new(YildizListeBicim.Yuzde(c.PesinKomisyonOran * 100m, dil), YildizHucreTuru.Deger),
                ],
                c.Aktif
                    ? new YildizEylem(YildizPosSozlugu.Duzenle(dil), $"yildiz-pos-duzenle-{c.Id}", EventCallback.Factory.Create(this, () => DuzenleAc(c)))
                    : new YildizEylem(YildizPosSozlugu.Aktiflestir(dil), $"yildiz-pos-aktif-{c.Id}", EventCallback.Factory.Create(this, () => Aktiflestir(c))),
                Soluk: !c.Aktif))],
                Yukleniyor = yukleniyor,
                Bantlar = YildizBant.Ciz(hata, basari, 0, hataId: "pos-hata", basariId: "pos-basari"),
                AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                AyrintiAcik = yildizAyrinti,
                AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
            };
        }
    }
}
