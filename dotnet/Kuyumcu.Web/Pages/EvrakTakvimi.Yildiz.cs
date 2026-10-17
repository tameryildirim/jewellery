using System.Globalization;
using System.Text.Json;
using Kuyumcu.Core.I18n;
using Kuyumcu.Core.Sozlesmeler;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5B — Vade takvimi "Yıldız" yüzü (tuval Y_Vadetakvimi · BİREBİR · takvim kalıbı).
// Yalnız GÖRÜNÜM + OKUMA: içinde bulunulan ayın evrakları (GET /evraklar?vadeBas&vadeBit — Çek ve
// senet ekranının MEVCUT ucu; tahsil edilmiş evrak takvimde gösterilmez). Vadeli gün o günün evrak
// listesine gider (/evraklar?vadeBas=…&vadeBit=…, özgün takvimle aynı bağ). Tarih aralığı ve gün
// toplamları tablosu "Tüm ayrıntılar"da (özgün ekran).
// ═════════════════════════════════════════════════════════════════════════════
public partial class EvrakTakvimi
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private bool YildizOzgunCiz => yildizAyrinti;
    private string? YildizOzgunIsaret => "evrak-takvimi";
    private bool yildizAyrinti;

    public const int YildizSinir = 200;
    private bool yildizOkundu, yildizYukleniyor = true;
    private EvrakListesi? yildizListe;
    private string? yildizHata;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (yildizOkundu) return;
        yildizOkundu = true;
        var (ilk, son) = YildizAy(DateOnly.FromDateTime(Bicim.IsletmeBugun));
        try
        {
            yildizListe = await Api.GetAsync<EvrakListesi>($"/evraklar?limit={YildizSinir}&vadeBas={Bicim.Makine(ilk)}&vadeBit={Bicim.Makine(son)}");
        }
        catch (Exception ex) when (ex is ApiHata or JsonException) { yildizHata = ex.Message; }
        yildizYukleniyor = false;
        StateHasChanged();
    }

    /// <summary>Ayın ilk ve son günü (saf).</summary>
    public static (DateOnly Ilk, DateOnly Son) YildizAy(DateOnly gun)
    {
        var ilk = new DateOnly(gun.Year, gun.Month, 1);
        return (ilk, ilk.AddMonths(1).AddDays(-1));
    }

    /// <summary>Pazartesi başlangıçlı ızgarada ayın ilk gününden önceki boş hücre (saf): Pzt 0 … Paz 6.</summary>
    public static int YildizBosluk(DateOnly ilk) => ((int)ilk.DayOfWeek + 6) % 7;

    /// <summary>Takvimde gösterilen evrak (saf): tahsil edilmiş ve karşılıksız kapanmış olanlar gösterilmez.</summary>
    public static bool YildizGoster(EvrakSatiri e) => e.Durum is not ("TAHSIL_EDILDI" or "KARSILIKSIZ");

    /// <summary>Satır tutarı (saf): verilen evrak bizim ödememizdir → eksi (tuval "−3.840 ₺", kırmızı).</summary>
    public static (string Metin, bool Kotu) YildizTutar(EvrakSatiri e) =>
        e.Yon == "VERILEN" ? ("−" + Bicim.Para(e.Tutar, e.ParaBirimi), true) : (Bicim.Para(e.Tutar, e.ParaBirimi), false);

    private YildizTakvimBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var kultur = dil == "en" ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo("tr-TR");
            var (ilk, son) = YildizAy(DateOnly.FromDateTime(Bicim.IsletmeBugun));
            var evraklar = (yildizListe?.Satirlar ?? []).Where(YildizGoster)
                .Where(e => e.VadeGunu >= ilk && e.VadeGunu <= son).OrderBy(e => e.VadeGunu).ToList();
            var ayAdi = Bicim.GunBicim(ilk, "MMMM yyyy", kultur);
            return new YildizTakvimBaglami
            {
                Kok = "yildiz-vade",
                Ust = YildizVadeSozlugu.Ust(dil),
                Baslik = YildizListeBicim.EkranAdi("/evraklar/takvim", Dil),
                AyAdi = dil == "en" ? ayAdi : char.ToUpper(ayAdi[0], kultur) + ayAdi[1..],
                GunAdlari = YildizVadeSozlugu.GunAdlari(dil),
                Bosluk = YildizBosluk(ilk),
                GunSayisi = son.Day,
                VadeliGunler = evraklar.Select(e => e.VadeGunu).Distinct()
                    .ToDictionary(g => g.Day, g => $"/evraklar?vadeBas={Bicim.Makine(g)}&vadeBit={Bicim.Makine(g)}"),
                ListeBaslik = YildizVadeSozlugu.BuAy(dil),
                Sutunlar = [new(YildizVadeSozlugu.Gun(dil)), new(YildizVadeSozlugu.Kim(dil)), new(YildizParaSozlugu.Tutar(dil), Sag: true)],
                Satirlar = [.. evraklar.Select(e =>
                {
                    var (metin, kotu) = YildizTutar(e);
                    var kim = Evraklar.YildizKimden(e);
                    return new YildizListeSatiri(e.Id,
                    [
                        new(e.VadeGunu.Day.ToString(CultureInfo.InvariantCulture), YildizHucreTuru.Ad, $"yildiz-vade-gun-{e.Id}"),
                        new(e.Yon == "VERILEN" ? YildizVadeSozlugu.Biz(kim, dil) : kim),
                        new(metin, kotu ? YildizHucreTuru.Kotu : YildizHucreTuru.Deger, $"yildiz-vade-tutar-{e.Id}"),
                    ]);
                })],
                Yukleniyor = yildizYukleniyor,
                BosMetni = !yildizYukleniyor && yildizHata is null ? YildizVadeSozlugu.Bos(dil) : null,
                SinirNotu = yildizListe is { DahaFazlasiVar: true } l ? YildizVadeSozlugu.Sinir(l.Limit, dil) : null,
                Bantlar = YildizBant.Ciz(yildizHata ?? hata, null, 0, hataId: null),
                AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                AyrintiAcik = yildizAyrinti,
                AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
                DonEtiketi = YildizParaSozlugu.EkranaDon(dil),
            };
        }
    }
}
