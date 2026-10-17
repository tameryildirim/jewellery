using System.Globalization;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5B — Gider pusulaları "Yıldız" yüzü (tuval Y_Giderpusulalari · BİREBİR · liste kalıbı).
// Yalnız GÖRÜNÜM + OKUMA: satırlar sayfanın MEVCUT listesidir (GET /hurda/pusulalar; liste ucu
// TCKN'yi maskeli döndürür, tekil uç okunmaz — KVKK). Arama yüklenmiş listeyi EKRANDA süzer.
// "Pusula düzenle" MEVCUT birincil yol (pusulayı üreten hurda alımı), satır MEVCUT Detay çekmecesi;
// tarih süzgeci, stopaj dönemleri, iptal ve yeniden düzenleme "Tüm ayrıntılar"da.
// ═════════════════════════════════════════════════════════════════════════════
public partial class GiderPusulalari
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private bool YildizOzgunCiz => yildizAyrinti;
    private string? YildizOzgunIsaret => "gider-pusulalari";
    private bool yildizAyrinti;
    private string? yildizArama;

    /// <summary>Tuval "14 ayar · 6,0 g" (ayar adı küçük harf, gram bir ondalık).</summary>
    public static string YildizMal(string ayarAdi, decimal gram, string dil)
    {
        var k = dil == "en" ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo("tr-TR");
        var ad = dil == "en" ? ayarAdi : ayarAdi.ToLower(k);
        return $"{ad} · {gram.ToString("0.0", k)} g";
    }

    /// <summary>Satıcı süzgeci (saf, Türkçe büyük/küçük harf duyarsız).</summary>
    public static IEnumerable<PusulaSatir> YildizSuz(IEnumerable<PusulaSatir> liste, string? arama)
    {
        if (string.IsNullOrWhiteSpace(arama)) return liste;
        var tr = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;
        return liste.Where(p => tr.IndexOf(p.SaticiAdSoyad, arama.Trim(), CompareOptions.IgnoreCase) >= 0);
    }

    private YildizListeBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var bugun = DateOnly.FromDateTime(Bicim.IsletmeBugun);
            var satirlar = YildizSuz(pusulalar, yildizArama).OrderByDescending(p => p.TeslimTarihi).ToList();
            return new YildizListeBaglami
            {
                Kok = "yildiz-pusula", Tur = "pusula",
                Ust = YildizPusulaSozlugu.Ust(dil),
                Baslik = YildizListeBicim.EkranAdi("/gider-pusulalari", Dil),
                AramaId = "yildiz-pusula-ara",
                AramaEtiket = YildizPusulaSozlugu.AraYerTutucu(dil), AramaYerTutucu = YildizPusulaSozlugu.AraYerTutucu(dil),
                Arama = yildizArama,
                AramaDegisti = EventCallback.Factory.Create<string?>(this, v => yildizArama = v),
                B1Etiket = YildizPusulaSozlugu.PusulaDuzenle(dil), B1Id = "yildiz-pusula-b1",
                B1Tikla = EventCallback.Factory.Create(this, () => Nav.NavigateTo(HurdaAlimiAdresi)),
                Sutunlar = [new(YildizPusulaSozlugu.Pusula(dil)), new(YildizPusulaSozlugu.Satici(dil)), new(YildizPusulaSozlugu.NeAlindi(dil)), new(YildizParaSozlugu.Tutar(dil), Sag: true), new(YildizPusulaSozlugu.Tarih(dil))],
                Satirlar = [.. satirlar.Select(p => new YildizListeSatiri(p.Id,
                [
                    new(p.PusulaNo, YildizHucreTuru.Ad, $"yildiz-pusula-no-{p.Id}"),
                    new(p.IptalEdildi ? $"{p.SaticiAdSoyad} · {YildizPusulaSozlugu.Iptal(dil)}"
                        : string.IsNullOrWhiteSpace(p.SaticiTckn) ? p.SaticiAdSoyad : $"{p.SaticiAdSoyad} · {YildizPusulaSozlugu.KimlikAlindi(dil)}"),
                    new(YildizMal(KurKaynakAdi.AyarAdi(p.AyarId), p.GramAgirlik, dil)),
                    new(YildizParaBicim.Tutar(p.BrutTutar, "TRY"), YildizHucreTuru.Deger, $"yildiz-pusula-tutar-{p.Id}"),
                    new(YildizListeBicim.KisaGun(Bicim.IsletmeGunu(p.TeslimTarihi), bugun, dil)),
                ],
                null, EventCallback.Factory.Create(this, () => Detay(p.Id)), Soluk: p.IptalEdildi,
                AcEtiketi: YildizPusulaSozlugu.Ac(p.PusulaNo, dil)))],
                Yukleniyor = yukleniyor,
                BosMetni = !yukleniyor && hata is null ? (pusulalar.Count > 0 ? YildizPusulaSozlugu.AramaBos(dil) : YildizPusulaSozlugu.Bos(dil)) : null,
                SinirNotu = pusulalar.Count >= PusulaSiniri ? YildizPusulaSozlugu.Sinir(PusulaSiniri, dil) : null,
                Not = YildizPusulaSozlugu.Not(dil),
                Bantlar = YildizBant.Ciz(hata, null, 0, hataId: null),
                AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                AyrintiAcik = yildizAyrinti,
                AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
            };
        }
    }
}
