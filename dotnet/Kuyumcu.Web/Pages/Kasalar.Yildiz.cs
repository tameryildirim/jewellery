using System.Text.Json;
using Kuyumcu.Contracts.Sayfalar.Kasalar;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5B — Kasalar "Yıldız" yüzü (tuval Y_Kasalar · BİREBİR · özet kalıbı).
// Yalnız GÖRÜNÜM + OKUMA: büyük rakam = kasanın defter bakiyesi (GET /kasalar), satırlar =
// nakit/kart/havale ayrımı (GET /kasalar/nakit-ayrimi) ve hareket sayısı (GET …/hareketler),
// alt satır = son kapanış (GET …/gun-kapanislari) ile bugünün giriş/çıkışı. Eylemler sayfanın
// MEVCUT çekmeceleridir (Günü kapat · Hareket · Geçmiş). Öteki 8 şemada çizime girmez.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Kasalar
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private bool YildizMi => Kuyumcu.Web.Layout.YildizKabuk.Mi(Hizmetler);
    private bool YildizOzgunCiz => !YildizMi || yildizAyrinti;
    private string? YildizOzgunIsaret => YildizMi ? "kasalar" : null;

    /// <summary>Okunan hareket sınırı (UX-S29: değdiyse sayı "1000+" yazılır).</summary>
    public const int YildizHareketSiniri = 1000;

    private bool yildizAyrinti;
    private string? yildizAnahtar;
    private List<HareketVeri> yildizHareketler = [];
    private DateOnly? yildizSonKapanis;
    private bool yildizKapanisOkundu;

    /// <summary>Yıldız'ın gösterdiği kasa: ilk aktif TL kasası, yoksa ilk aktif kasa.</summary>
    private KasaVeri? YildizKasa =>
        KasaListesi.FirstOrDefault(k => k.Aktif && k.ParaBirimi == "TRY") ?? KasaListesi.FirstOrDefault(k => k.Aktif);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!YildizMi || YildizKasa is not { } k) return;
        var anahtar = $"{k.Id}|{k.Bakiye}|{BugunKapali(k)}";
        if (anahtar == yildizAnahtar) return;
        yildizAnahtar = anahtar;
        try
        {
            yildizHareketler = (await Api.GetAsync<HareketYaniti>($"/kasalar/{k.Id}/hareketler?limit={YildizHareketSiniri}")).Hareketler ?? [];
        }
        catch (Exception ex) when (ex is ApiHata or JsonException) { listeHata ??= ex.Message; }
        try
        {
            var kapanislar = await Api.GetAsync<List<JsonElement>>($"/kasalar/{k.Id}/gun-kapanislari?limit=1");
            yildizSonKapanis = kapanislar.Select(YerelGun).FirstOrDefault(g => g is not null);
            yildizKapanisOkundu = true;
        }
        catch (Exception ex) when (ex is ApiHata or JsonException) { yildizKapanisOkundu = false; listeHata ??= ex.Message; }
        StateHasChanged();
    }

    private static DateOnly? YerelGun(JsonElement e) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty("yerelGun", out var g) && g.ValueKind == JsonValueKind.String
        && DateOnly.TryParse(g.GetString(), System.Globalization.CultureInfo.InvariantCulture, out var gun) ? gun : null;

    /// <summary>Hareketin ayrım sınıfı (sunucu KasaNakitAyrimi ile aynı: kart ve havale dışı her şey nakit).</summary>
    public static string YildizArac(string? odemeTuru) => odemeTuru switch
    {
        "KART" => "KART",
        "HAVALE" => "HAVALE",
        _ => "NAKIT",
    };

    /// <summary>
    /// P-DIL-BORC-KASA (Y8, saf): kasa ekranının büyük rakamı. Nakit ayrımı okunduysa ÇEKMECEDEKİ
    /// NAKİT (kart ve havale çekmecede durmaz), okunamadıysa kasa toplamı (etiketi bunu söyler).
    /// </summary>
    public static decimal BuyukRakam(decimal bakiye, KasaNakitAyrimiOzeti? ayrim) => ayrim?.Nakit ?? bakiye;

    /// <summary>Hareket sayısı metni (saf): sınıra değdiyse "1000+".</summary>
    public static string YildizSayi(int adet, bool sinirDegdi, string dil) =>
        sinirDegdi ? YildizKasalarSozlugu.Sinir(adet, dil) : YildizListeBicim.Adet(adet, dil);

    /// <summary>Son kapanış metni (saf): "bugün" · "dün" · "8 Eki" · "yok".</summary>
    public static string YildizKapanisMetni(DateOnly? gun, DateOnly bugun, string dil) =>
        gun is not { } g ? YildizParaSozlugu.Yok(dil)
        : g == bugun ? YildizParaSozlugu.Bugun(dil)
        : g == bugun.AddDays(-1) ? YildizParaSozlugu.Dun(dil)
        : Bicim.GunBicim(g, "d MMM", dil == "en" ? System.Globalization.CultureInfo.InvariantCulture : System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));

    private YildizOzetBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var baslik = YildizListeBicim.EkranAdi("/kasalar", Dil);
            var bantlar = YildizBant.Ciz(listeHata ?? nakitAyrimHata, basari, basariJeton, 3000);
            if (YildizKasa is not { } k)
            {
                return new YildizOzetBaglami
                {
                    Kok = "yildiz-kasalar", Tur = "kasalar",
                    Ust = YildizParaSozlugu.ParaBanka(dil), Baslik = baslik,
                    Cumle = yukleniyor ? "" : YildizKasalarSozlugu.KasaYok(dil),
                    B1Etiket = yukleniyor ? null : YildizKasalarSozlugu.KasaEkle(dil), B1Id = "yildiz-kasalar-b1",
                    B1Tikla = EventCallback.Factory.Create(this, () => { yildizAyrinti = true; YeniAc(); }),
                    Yukleniyor = yukleniyor, Bantlar = bantlar,
                    AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                    AyrintiAcik = yildizAyrinti, AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
                    DonEtiketi = YildizParaSozlugu.EkranaDon(dil),
                };
            }
            var ayrim = nakitAyrimlari.GetValueOrDefault(k.Id);
            // P-DIL-BORC-KASA (Y8): büyük rakam ÇEKMECEDEKİ NAKİT (sunucu ayrımı); okunamazsa kasa
            // toplamı ve cümlesi bunu söyler. Hesap değişmez — yalnız hangi toplamın büyük yazıldığı.
            var (ana, ek) = YildizParaBicim.BuyukPara(BuyukRakam(k.Bakiye, ayrim), k.ParaBirimi);
            var sinir = yildizHareketler.Count >= YildizHareketSiniri;
            var bugun = DateOnly.FromDateTime(Bicim.IsletmeBugun);
            var bugunku = yildizHareketler.Where(h => Bicim.IsletmeGunu(h.Tarih) == bugun).ToList();
            var kapali = BugunKapali(k);
            YildizListeSatiri Satir(string kod, string ad, decimal? tutar) => new(kod.ToLowerInvariant(),
            [
                new(ad, YildizHucreTuru.Ad, $"yildiz-kasalar-ad-{kod.ToLowerInvariant()}"),
                new(YildizSayi(yildizHareketler.Count(h => YildizArac(h.OdemeTuru) == kod), sinir, dil), YildizHucreTuru.Deger),
                new(tutar is { } t ? YildizParaBicim.Tutar(t, k.ParaBirimi) : "—", YildizHucreTuru.Deger, $"yildiz-kasalar-tutar-{kod.ToLowerInvariant()}"),
            ],
            new YildizEylem(YildizKasalarSozlugu.Gecmis(dil), $"yildiz-kasalar-gecmis-{kod.ToLowerInvariant()}",
                EventCallback.Factory.Create(this, () => GecmisAc(k))));
            return new YildizOzetBaglami
            {
                Kok = "yildiz-kasalar", Tur = "kasalar",
                Ust = $"{YildizParaSozlugu.ParaBanka(dil)} · {k.Ad} · {ParaAdi(k.ParaBirimi)}",
                Baslik = baslik,
                Rakam = ana, RakamEki = ek,
                Cumle = ayrim is not null
                    ? KasaNakitSozlugu.YildizCumleNakit(YildizParaBicim.Tutar(k.Bakiye, k.ParaBirimi), dil)
                    : KasaNakitSozlugu.YildizCumleToplam(dil),
                B1Etiket = YildizKasalarSozlugu.GunuKapat(dil), B1Id = "yildiz-kasalar-b1",
                B1Tikla = EventCallback.Factory.Create(this, () => GunKapanisAc(k)),
                B1Kilit = kapali ? SKL.GunKapandiSebep : null,
                B2Etiket = YildizKasalarSozlugu.HareketEkle(dil), B2Id = "yildiz-kasalar-b2",
                B2Tikla = EventCallback.Factory.Create(this, () => HareketModalAc(k)),
                B2Kilit = kapali ? SKL.GunKapandiSebep : null,
                Sutunlar = [new(YildizKasalarSozlugu.Ne(dil)), new(YildizKasalarSozlugu.Islem(dil), Sag: true), new(YildizParaSozlugu.Tutar(dil), Sag: true)],
                EylemSutunu = true,
                Satirlar =
                [
                    Satir("NAKIT", YildizKasalarSozlugu.Nakit(dil), ayrim?.Nakit),
                    Satir("KART", YildizKasalarSozlugu.Kart(dil), ayrim?.Kart),
                    Satir("HAVALE", YildizKasalarSozlugu.Havale(dil), ayrim?.Havale),
                ],
                Bilgi =
                [
                    (YildizKasalarSozlugu.SonKapanis(dil), yildizKapanisOkundu ? YildizKapanisMetni(yildizSonKapanis, bugun, dil) : "—"),
                    (YildizKasalarSozlugu.BugunGiren(dil), YildizParaBicim.Tutar(bugunku.Where(h => h.Tutar > 0m).Sum(h => h.Tutar), k.ParaBirimi, artiIsareti: true)),
                    (YildizKasalarSozlugu.Cikan(dil), YildizParaBicim.Tutar(bugunku.Where(h => h.Tutar < 0m).Sum(h => h.Tutar), k.ParaBirimi)),
                ],
                Yukleniyor = yukleniyor, Bantlar = bantlar,
                AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                AyrintiAcik = yildizAyrinti, AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
                DonEtiketi = YildizParaSozlugu.EkranaDon(dil),
            };
        }
    }
}
