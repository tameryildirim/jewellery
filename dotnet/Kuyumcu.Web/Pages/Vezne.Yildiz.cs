using Kuyumcu.Core.I18n;
using Kuyumcu.Core.Sozlesmeler;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5B — Kasa kuyruğu "Yıldız" yüzü (tuval Y_Kasakuyrugu · BİREBİR · liste kalıbı).
// Yalnız GÖRÜNÜM + OKUMA: satırlar sayfanın MEVCUT kuyruğudur (GET /vezne/kuyruk, varsayılan
// VEZNEDE). "Tahsil et" MEVCUT Sec(fiş) + tahsil paneli (özgün ekran); yeni fiş, durum süzgeci,
// düzenleme ve iptal "Tüm ayrıntılar"da. Öteki 8 şemada çizime girmez.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Vezne
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private bool YildizMi => Kuyumcu.Web.Layout.YildizKabuk.Mi(Hizmetler);
    private bool YildizOzgunCiz => !YildizMi || yildizAyrinti;
    private string? YildizOzgunIsaret => YildizMi ? "vezne" : null;
    private bool yildizAyrinti;

    /// <summary>Fiş adı (saf): tuval "Satış · 2 ürün" · "Tahsilat" · "İşçilik".</summary>
    public static string YildizFisAdi(VezneKuyrukSatiri f, string dil) => f.FisTuru switch
    {
        "TAHSILAT" => YildizVezneSozlugu.Tahsilat(dil),
        "ISCILIK" => YildizVezneSozlugu.Iscilik(dil),
        _ => f.KalemSayisi > 0 ? YildizVezneSozlugu.Satis(f.KalemSayisi, dil) : YildizVezneSozlugu.SatisYalin(dil),
    };

    /// <summary>Bekleme süresi (saf): kasaya gönderildiği andan (yoksa oluşturmadan) bu yana, tuval "4 dk".</summary>
    public static string YildizBekleme(VezneKuyrukSatiri f, DateTime simdiUtc, string dil) =>
        Bicim.Sure((int)Math.Floor((simdiUtc - (f.Veznelendi ?? f.Olusturuldu)).TotalMinutes),
                   YildizVezneSozlugu.Gun(dil), YildizVezneSozlugu.Sa(dil), YildizVezneSozlugu.Dk(dil));

    private async Task YildizTahsil(string id)
    {
        yildizAyrinti = true;
        await Sec(id);
    }

    private YildizListeBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var simdi = DateTime.UtcNow;
            var satirlar = liste?.Satirlar ?? [];
            return new YildizListeBaglami
            {
                Kok = "yildiz-vezne", Tur = "vezne",
                Ust = YildizVezneSozlugu.Ust(dil),
                Baslik = YildizListeBicim.EkranAdi("/vezne", Dil),
                Sutunlar = [new(YildizParaSozlugu.Fis(dil)), new(YildizParaSozlugu.Musteri(dil)), new(YildizParaSozlugu.Tutar(dil), Sag: true), new(YildizVezneSozlugu.Bekliyor(dil))],
                EylemSutunu = true,
                Satirlar = [.. satirlar.Select(f => new YildizListeSatiri(f.Id,
                [
                    new(YildizFisAdi(f, dil), YildizHucreTuru.Ad, $"yildiz-vezne-ad-{f.Id}"),
                    new(f.CariAd ?? YildizParaSozlugu.PesinMusteri(dil)),
                    new(YildizParaBicim.Tutar(f.ToplamTl, "TRY"), YildizHucreTuru.Deger, $"yildiz-vezne-tutar-{f.Id}"),
                    new(YildizBekleme(f, simdi, dil)),
                ],
                new YildizEylem(YildizVezneSozlugu.TahsilEt(dil), $"yildiz-vezne-tahsil-{f.Id}",
                    EventCallback.Factory.Create(this, () => YildizTahsil(f.Id)),
                    KilitSebebi: f.Durum == "VEZNEDE" ? null : YildizVezneSozlugu.TahsilKilit(dil))))],
                Yukleniyor = yukleniyor,
                BosMetni = !yukleniyor && hata is null && liste is not null ? YildizVezneSozlugu.Bos(dil) : null,
                SinirNotu = liste is { DahaFazlasiVar: true } l ? YildizVezneSozlugu.Sinir(l.Limit, dil) : null,
                Not = YildizVezneSozlugu.Not(dil),
                Bantlar = YildizBant.Ciz(hata, basari, basariJeton, 6000, "vz-hata-bandi", "vz-basari-bandi"),
                AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                AyrintiAcik = yildizAyrinti,
                AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
            };
        }
    }
}
