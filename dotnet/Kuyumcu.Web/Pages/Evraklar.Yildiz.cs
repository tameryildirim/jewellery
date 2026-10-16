using Kuyumcu.Core.I18n;
using Kuyumcu.Core.Sozlesmeler;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5B — Çek ve senet "Yıldız" yüzü (tuval Y_Cekvesenet · BİREBİR · liste kalıbı).
// Yalnız GÖRÜNÜM + OKUMA: satırlar sayfanın MEVCUT portföy listesidir (GET /evraklar). Arama ve
// Hepsi/Alınan/Verilen sekmeleri yüklenmiş listeyi EKRANDA süzer (istek atmaz). "Çek al" MEVCUT
// YeniAc, satır MEVCUT DuzenleAc, "Tahsil et" MEVCUT TahsilAc (çekmeceler ORTAK); durum/vade
// süzgeci, tahsile ver, karşılıksız ve takvim bağlantısı "Tüm ayrıntılar"da.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Evraklar
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private bool YildizMi => Kuyumcu.Web.Layout.YildizKabuk.Mi(Hizmetler);
    private bool YildizOzgunCiz => !YildizMi || yildizAyrinti;
    private string? YildizOzgunIsaret => YildizMi ? "evraklar" : null;
    private bool yildizAyrinti;
    private string? yildizArama;
    private string yildizSekme = "";

    /// <summary>Belge adı (saf): tuval "Çek · Ziraat" · "Senet".</summary>
    public static string YildizBelge(string turAdi, string? bankaAdi) =>
        string.IsNullOrWhiteSpace(bankaAdi) ? turAdi : $"{turAdi} · {bankaAdi}";

    /// <summary>Kimden (saf): kaynak cari, yoksa borçlu adı.</summary>
    public static string YildizKimden(EvrakSatiri e) => e.KaynakCariAd ?? e.BorcluAd ?? "—";

    /// <summary>Ekran süzgeci (saf): sekme (yön) + arama (kimden ya da banka, Türkçe büyük/küçük harf duyarsız).</summary>
    public static IEnumerable<EvrakSatiri> YildizSuz(IEnumerable<EvrakSatiri> liste, string sekme, string? arama)
    {
        var q = liste.Where(e => sekme.Length == 0 || e.Yon == sekme);
        if (string.IsNullOrWhiteSpace(arama)) return q;
        var a = arama.Trim();
        var tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR").CompareInfo;
        bool Icerir(string? s) => s is not null && tr.IndexOf(s, a, System.Globalization.CompareOptions.IgnoreCase) >= 0;
        return q.Where(e => Icerir(e.KaynakCariAd) || Icerir(e.BorcluAd) || Icerir(e.BankaAdi));
    }

    private YildizListeBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var bugun = DateOnly.FromDateTime(Bicim.IsletmeBugun);
            var tum = liste?.Satirlar ?? [];
            var satirlar = YildizSuz(tum, yildizSekme, yildizArama).ToList();
            return new YildizListeBaglami
            {
                Kok = "yildiz-evrak", Tur = "evrak",
                Ust = YildizEvrakSozlugu.Ust(dil),
                Baslik = YildizListeBicim.EkranAdi("/evraklar", Dil),
                AramaId = "yildiz-evrak-ara",
                AramaEtiket = YildizEvrakSozlugu.AraYerTutucu(dil), AramaYerTutucu = YildizEvrakSozlugu.AraYerTutucu(dil),
                Arama = yildizArama,
                AramaDegisti = EventCallback.Factory.Create<string?>(this, v => yildizArama = v),
                B1Etiket = YildizEvrakSozlugu.CekAl(dil), B1Id = "yildiz-evrak-b1",
                B1Tikla = EventCallback.Factory.Create(this, () => { YeniAc(); fTur = "CEK"; }),
                SekmelerId = "yildiz-evrak-sekmeler",
                Sekmeler =
                [
                    new("", YildizEvrakSozlugu.Hepsi(dil), yildizSekme == ""),
                    new("ALINAN", YildizEvrakSozlugu.Alinan(dil), yildizSekme == "ALINAN"),
                    new("VERILEN", YildizEvrakSozlugu.Verilen(dil), yildizSekme == "VERILEN"),
                ],
                SekmeIdOneki = "yildiz-evrak-sekme-",
                SekmeSec = EventCallback.Factory.Create<string>(this, k => yildizSekme = k),
                Sutunlar = [new(YildizEvrakSozlugu.Belge(dil)), new(YildizEvrakSozlugu.Kimden(dil)), new(YildizParaSozlugu.Tutar(dil), Sag: true), new(YildizEvrakSozlugu.Vade(dil)), new(YildizEvrakSozlugu.Durum(dil))],
                EylemSutunu = true,
                Satirlar = [.. satirlar.Select(e => new YildizListeSatiri(e.Id,
                [
                    new(YildizBelge(TurAdi(e.EvrakTuru), e.BankaAdi), YildizHucreTuru.Ad, $"yildiz-evrak-ad-{e.Id}"),
                    new(YildizKimden(e)),
                    new(YildizParaBicim.Tutar(e.Tutar, e.ParaBirimi), YildizHucreTuru.Deger, $"yildiz-evrak-tutar-{e.Id}"),
                    new(YildizListeBicim.KisaGun(e.VadeGunu, bugun, dil), e.Gecikmis ? YildizHucreTuru.Kotu : YildizHucreTuru.Soluk),
                    new(DurumAdi(e.Durum), YildizHucreTuru.Soluk, $"yildiz-evrak-durum-{e.Id}"),
                ],
                new YildizEylem(YildizEvrakSozlugu.TahsilEt(dil), $"yildiz-evrak-tahsil-{e.Id}",
                    EventCallback.Factory.Create(this, () => TahsilAc(e)),
                    KilitSebebi: e.IzinliGecisler.Contains("TAHSIL_EDILDI") ? null : YildizEvrakSozlugu.TahsilKilit(DurumAdi(e.Durum), dil)),
                Ac: EventCallback.Factory.Create(this, () => DuzenleAc(e)),
                Soluk: e.Durum is "TAHSIL_EDILDI" or "KARSILIKSIZ",
                AcEtiketi: YildizEvrakSozlugu.Ac(YildizBelge(TurAdi(e.EvrakTuru), e.BankaAdi), dil)))],
                Yukleniyor = yukleniyor,
                BosMetni = !yukleniyor && hata is null && liste is not null
                    ? (tum.Count > 0 ? YildizEvrakSozlugu.AramaBos(dil) : YildizEvrakSozlugu.Bos(dil)) : null,
                SinirNotu = liste is { DahaFazlasiVar: true } l ? YildizEvrakSozlugu.Sinir(l.Limit, dil) : null,
                Bantlar = YildizBant.Ciz(hata, basari, basariJetonu, hataId: null, basariId: "evrak-basari"),
                AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                AyrintiAcik = yildizAyrinti,
                AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
            };
        }
    }
}
