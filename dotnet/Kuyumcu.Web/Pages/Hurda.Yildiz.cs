using Kuyumcu.Contracts.Sayfalar.Hurda;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5B — Hurda ve ramat "Yıldız" yüzü (tuval Y_Hurdaveramat · BİREBİR · özet kalıbı).
// Yalnız GÖRÜNÜM + OKUMA: büyük rakam = altın hurda bakiyesi (GET /hurda/bakiye), satırlar =
// son hurda girişleri (GET /hurda/defter, sayfa zaten okuyor), son gönderim = son HURDA_SATIS.
// "Rafineriye gönder" MEVCUT ramat formunu (özgün ekran) açar; "Hurda al" Tezgâh'a gider.
// ⚠ Hurda.razor davranış kodu başka şeridindir (t5) — bu dosya yalnız Yıldız dalıdır.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Hurda
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private bool YildizOzgunCiz => yildizAyrinti;
    private string? YildizOzgunIsaret => "hurda";
    private bool yildizAyrinti;

    /// <summary>Yıldız listesinde gösterilen en çok giriş satırı (sayfa kısa; tamamı "Tüm ayrıntılar"da).</summary>
    public const int YildizSatirSiniri = 5;

    /// <summary>Hurda girişi mi (saf): satış ve tahlil farkı bacağı çıkıştır; stornolu/storno satırı gösterilmez.</summary>
    public static bool YildizGirisMi(HurdaDefterSatiri d) =>
        d.Tur is not ("HURDA_SATIS" or "TAHLIL_FARKI") && !d.StornoSatiri && !d.Stornolandi;

    private YildizOzetBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var bugun = DateOnly.FromDateTime(Bicim.IsletmeBugun);
            var altin = bakiyeler.FirstOrDefault(b => b.Maden == "ALTIN") ?? bakiyeler.FirstOrDefault();
            var girisler = defter.Where(YildizGirisMi).OrderByDescending(d => d.Tarih).Take(YildizSatirSiniri).ToList();
            var sonGonderim = defter.Where(d => d.Tur == "HURDA_SATIS" && !d.Stornolandi).OrderByDescending(d => d.Tarih).FirstOrDefault();
            return new YildizOzetBaglami
            {
                Kok = "yildiz-hurda", Tur = "hurda",
                Ust = YildizHurdaSozlugu.Ust(dil),
                Baslik = YildizListeBicim.EkranAdi("/hurda", Dil),
                Rakam = veriGeldi ? YildizListeBicim.Gram(altin?.HasGram ?? 0m, dil) : null,
                RakamEki = $" g {YildizParaSozlugu.Has(dil)}",
                Cumle = YildizHurdaSozlugu.Cumle(dil),
                B1Etiket = YildizHurdaSozlugu.RafineriyeGonder(dil), B1Id = "yildiz-hurda-b1",
                B1Tikla = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                B2Etiket = YildizHurdaSozlugu.HurdaAl(dil), B2Id = "yildiz-hurda-b2", B2Href = GiderPusulalari.HurdaAlimiAdresi,
                Sutunlar = [new(YildizHurdaSozlugu.Alim(dil)), new(YildizHurdaSozlugu.Ayar(dil), Sag: true), new(YildizHurdaSozlugu.Gram(dil), Sag: true), new(YildizHurdaSozlugu.Has(dil), Sag: true)],
                Satirlar = [.. girisler.Select(d => new YildizListeSatiri(d.Id,
                [
                    new($"{TurAdi(d.Tur)} · {YildizListeBicim.KisaGun(Bicim.IsletmeGunu(d.Tarih), bugun, dil)}", YildizHucreTuru.Ad, $"yildiz-hurda-ad-{d.Id}"),
                    new("—", YildizHucreTuru.Deger),
                    new("—", YildizHucreTuru.Deger),
                    new(YildizListeBicim.HasIki(d.HasGrami, dil), YildizHucreTuru.Deger, $"yildiz-hurda-has-{d.Id}"),
                ],
                // D1-12 (18.10): hurda alımının fotoğrafı (telefonda kamera) — tam defterdeki "Fotoğraf" ile aynı pencere.
                Eylem: d.Tur == "HURDA_ALIS"
                    ? new YildizEylem(D1Sozlugu.Al(dil).HurdaFoto, $"yildiz-hurda-foto-{d.Id}", EventCallback.Factory.Create(this, () => fotoHareketId = d.Id))
                    : null))],
                EylemSutunu = true,
                BosMetni = veriGeldi && hata is null ? YildizHurdaSozlugu.AlimYok(dil) : null,
                Bilgi = veriGeldi ?
                [
                    (YildizHurdaSozlugu.BekleyenRamat(dil), YildizParaSozlugu.Yok(dil)),
                    (YildizHurdaSozlugu.SonGonderim(dil), sonGonderim is null ? "—" : YildizListeBicim.KisaGun(Bicim.IsletmeGunu(sonGonderim.Tarih), bugun, dil)),
                ] : [],
                Yukleniyor = yukleniyor,
                Bantlar = YildizBant.Ciz(hata, basari, basariJeton, hataId: "hrd-hata", basariId: "hrd-basari"),
                AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                AyrintiAcik = yildizAyrinti, AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
                DonEtiketi = YildizParaSozlugu.EkranaDon(dil),
            };
        }
    }
}
