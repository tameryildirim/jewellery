using System.Globalization;
using System.Text.Json;
using Kuyumcu.Contracts.Sayfalar.Kasalar;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5B — Kasa raporu "Yıldız" yüzü (tuval Y_Kasaraporu · BİREBİR · özet kalıbı).
// Yalnız GÖRÜNÜM + OKUMA: büyük rakam = dönem kapanışı − açılış devri (GET /raporlar/kasa-defteri,
// sayfa zaten okuyor); satırlar = dönemin kasa hareketleri ödeme türüne göre (GET /kasalar/{id}/
// hareketler?baslangic&bitis; okunamazsa giriş/çıkış toplamına düşer, sebep bantta). "Günü kapat"
// MEVCUT sayım formunu, "Tarih seç" MEVCUT dönem süzgecini (özgün ekran) açar.
// ═════════════════════════════════════════════════════════════════════════════
public partial class KasaRaporu
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private bool YildizMi => Kuyumcu.Web.Layout.YildizKabuk.Mi(Hizmetler);
    private bool YildizOzgunCiz => !YildizMi || yildizAyrinti;
    private string? YildizOzgunIsaret => YildizMi ? "kasa-raporu" : null;
    private bool yildizAyrinti;

    public const int YildizHareketSiniri = 1000;
    private string? yildizAnahtar;
    private List<HareketVeri>? yildizHareketler;
    private string? yildizHareketHata;

    private KasaDefteriSatiri? YildizKasa => ozet?.Kasalar?.FirstOrDefault(k => k.ParaBirimi is null or "TRY") ?? ozet?.Kasalar?.FirstOrDefault();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!YildizMi || YildizKasa is not { } k) return;
        var anahtar = $"{k.KasaId}|{k.KapanisBakiye}|{k.AcilisDeviri}";
        if (anahtar == yildizAnahtar) return;
        yildizAnahtar = anahtar;
        var bas = Bicim.Makine(OzetBaslangic);
        var bit = Bicim.Makine(DateTime.Today.AddDays(1));
        try
        {
            yildizHareketler = (await Api.GetAsync<HareketYaniti>($"/kasalar/{k.KasaId}/hareketler?baslangic={bas}&bitis={bit}&limit={YildizHareketSiniri}")).Hareketler ?? [];
            yildizHareketHata = null;
        }
        catch (Exception ex) when (ex is ApiHata or JsonException) { yildizHareketler = null; yildizHareketHata = ex.Message; }
        StateHasChanged();
    }

    /// <summary>Tuval "1–8 Ekim" (aynı ay) · "28 Eyl – 8 Eki" (ay değişirse).</summary>
    public static string YildizAralik(DateOnly bas, DateOnly bit, string dil)
    {
        var k = dil == "en" ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo("tr-TR");
        return bas.Month == bit.Month && bas.Year == bit.Year
            ? $"{bas.Day}–{bit.Day} {Bicim.GunBicim(bit, "MMMM", k)}"
            : $"{Bicim.GunBicim(bas, "d MMM", k)} – {Bicim.GunBicim(bit, "d MMM", k)}";
    }

    /// <summary>Hareketleri tuval satırlarına böler (saf): nakit giriş · kart · havale · nakit çıkış → (adet, tutar).</summary>
    public static IReadOnlyList<(string Kod, int Adet, decimal Tutar)> YildizGruplar(IEnumerable<HareketVeri> hareketler)
    {
        var l = hareketler.ToList();
        static bool Nakit(HareketVeri h) => h.OdemeTuru is not ("KART" or "HAVALE");
        return
        [
            ("nakit-giris", l.Count(h => Nakit(h) && h.Tutar > 0m), l.Where(h => Nakit(h) && h.Tutar > 0m).Sum(h => h.Tutar)),
            ("kart", l.Count(h => h.OdemeTuru == "KART"), l.Where(h => h.OdemeTuru == "KART").Sum(h => h.Tutar)),
            ("havale", l.Count(h => h.OdemeTuru == "HAVALE"), l.Where(h => h.OdemeTuru == "HAVALE").Sum(h => h.Tutar)),
            ("nakit-cikis", l.Count(h => Nakit(h) && h.Tutar < 0m), l.Where(h => Nakit(h) && h.Tutar < 0m).Sum(h => h.Tutar)),
        ];
    }

    private YildizOzetBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var baslik = YildizListeBicim.EkranAdi("/raporlar/kasa", Dil);
            var bas = DateOnly.FromDateTime(OzetBaslangic);
            var bit = DateOnly.FromDateTime(DateTime.Today);
            var bantlar = YildizBant.Ciz(kasaHata ?? hata ?? yildizHareketHata, basariMesaj, basariJeton);
            var ayrinti = EventCallback.Factory.Create(this, () => yildizAyrinti = true);
            if (YildizKasa is not { } k)
            {
                return new YildizOzetBaglami
                {
                    Kok = "yildiz-kasarapor", Tur = "kasarapor", Ust = YildizParaSozlugu.ParaBanka(dil), Baslik = baslik,
                    Cumle = yukleniyor ? "" : YildizKasaRaporuSozlugu.Bos(dil), Yukleniyor = yukleniyor, Bantlar = bantlar,
                    AyrintiAc = ayrinti, AyrintiAcik = yildizAyrinti,
                    AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false), DonEtiketi = YildizParaSozlugu.EkranaDon(dil),
                };
            }
            var pb = k.ParaBirimi ?? "TRY";
            var (ana, ek) = YildizParaBicim.BuyukPara(k.KapanisBakiye - k.AcilisDeviri, pb, artiIsareti: true);
            var adlar = new Dictionary<string, string>
            {
                ["nakit-giris"] = YildizKasaRaporuSozlugu.NakitGiris(dil), ["kart"] = YildizKasaRaporuSozlugu.Kart(dil),
                ["havale"] = YildizKasaRaporuSozlugu.Havale(dil), ["nakit-cikis"] = YildizKasaRaporuSozlugu.NakitCikis(dil),
            };
            var sinir = yildizHareketler is { Count: >= YildizHareketSiniri };
            YildizListeSatiri Satir(string id, string ad, string adet, decimal tutar) => new(id,
            [
                new(ad, YildizHucreTuru.Ad, $"yildiz-kasarapor-ad-{id}"),
                new(adet, YildizHucreTuru.Deger),
                new(YildizParaBicim.Tutar(tutar, pb), tutar < 0m ? YildizHucreTuru.Soluk : YildizHucreTuru.Deger, $"yildiz-kasarapor-tutar-{id}"),
            ]);
            IReadOnlyList<YildizListeSatiri> satirlar = yildizHareketler is { } hl
                ? [.. YildizGruplar(hl).Select(g => Satir(g.Kod, adlar[g.Kod], Kasalar.YildizSayi(g.Adet, sinir, dil), g.Tutar))]
                : [
                    Satir("giris", YildizKasaRaporuSozlugu.Giris(dil), "—", (k.Gunler ?? []).Sum(g => g.Giris)),
                    Satir("cikis", YildizKasaRaporuSozlugu.Cikis(dil), "—", -(k.Gunler ?? []).Sum(g => g.Cikis)),
                  ];
            var gunSayisi = bit.DayNumber - bas.DayNumber + 1;
            return new YildizOzetBaglami
            {
                Kok = "yildiz-kasarapor", Tur = "kasarapor",
                Ust = YildizKasaRaporuSozlugu.Ust(k.KasaAd, YildizAralik(bas, bit, dil), dil),
                Baslik = baslik,
                Rakam = ana, RakamEki = ek,
                Cumle = YildizKasaRaporuSozlugu.Cumle(gunSayisi, dil),
                B1Etiket = YildizKasaRaporuSozlugu.GunuKapat(dil), B1Id = "yildiz-kasarapor-b1",
                B1Tikla = EventCallback.Factory.Create(this, async () => { yildizAyrinti = true; await SayimAc(k); }),
                B2Etiket = YildizKasaRaporuSozlugu.TarihSec(dil), B2Id = "yildiz-kasarapor-b2",
                B2Tikla = ayrinti,
                Sutunlar = [new(YildizKasaRaporuSozlugu.Hareket(dil)), new(YildizKasaRaporuSozlugu.Adet(dil), Sag: true), new(YildizParaSozlugu.Tutar(dil), Sag: true)],
                Satirlar = satirlar,
                Bilgi =
                [
                    (YildizKasaRaporuSozlugu.Acilis(dil), YildizParaBicim.Tutar(k.AcilisDeviri, pb)),
                    (YildizKasaRaporuSozlugu.Kapanis(dil), YildizParaBicim.Tutar(k.KapanisBakiye, pb)),
                ],
                Yukleniyor = yukleniyor, Bantlar = bantlar,
                AyrintiAc = ayrinti, AyrintiAcik = yildizAyrinti,
                AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
                DonEtiketi = YildizParaSozlugu.EkranaDon(dil),
            };
        }
    }
}
