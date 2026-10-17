using Kuyumcu.Contracts.Sayfalar.Fiyatlandirma;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

/// <summary>GET /cari satırının "Kullanım" sütunu için okunan alanları (yanıtta ZATEN var).</summary>
public sealed record YildizFiyatCari(string Id, string? FiyatListesiId = null, string? Segment = null);

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5A — Fiyat listeleri "Yıldız" yüzü (tuval Y_Fiyatlisteleri · BİREBİR).
// Yalnız GÖRÜNÜM + OKUMA: satırlar sayfanın MEVCUT listeleridir (GET /fiyat/listeler); "Ürün"
// etkin ürün sayısı, "Kullanım" listeye bağlı müşteri sayısıdır (GET /urunler, GET /cari —
// MEVCUT salt-okuma uçları). "Yeni liste", "Düzenle" ve satır (ürünlere etkisi) MEVCUT form
// ve paneldir; kampanyalar ve pasife alma "Tüm ayrıntılar"da. Yeni yazma yolu YOK.
// Tema temizliği (17.10): tek şema Yıldız — bu yüz her zaman çizilir; özgün gövde "Tüm ayrıntılar" arkasındadır.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Fiyatlandirma
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private bool YildizOzgunCiz => yildizAyrinti;
    private string? YildizOzgunIsaret => "fiyat";

    public const int YildizUrunSiniri = 500;
    public const int YildizCariSiniri = 200;

    private bool yildizAyrinti;
    private bool yildizYukleniyor;
    private bool yildizYuklendi;
    private string? yildizHata;
    private int? yildizUrunSayisi;
    private List<YildizFiyatCari> yildizCariler = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (yildizYuklendi || yildizYukleniyor) return;
        yildizYukleniyor = true;
        try
        {
            yildizUrunSayisi = (await Api.GetAsync<List<YildizAgacUrun>>($"/urunler?aktif=true&limit={YildizUrunSiniri}")).Count;
            yildizCariler = await Api.GetAsync<List<YildizFiyatCari>>($"/cari?aktif=true&limit={YildizCariSiniri}");
        }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException) { yildizHata = ex.Message; }
        yildizYukleniyor = false;
        yildizYuklendi = true;
        StateHasChanged();
    }

    /// <summary>
    /// "Kullanım" (saf): varsayılan liste "Varsayılan"; değilse listeye bağlı müşteriler — toptan
    /// segmenti "N toptancı", ötekiler "N müşteri"; kimse yoksa "Kullanılmıyor"; pasif liste "Kullanımda değil".
    /// </summary>
    public static string YildizKullanim(Liste l, IEnumerable<YildizFiyatCari> cariler, string dil)
    {
        if (!l.Aktif) return YildizFiyatSozlugu.Pasif(dil);
        if (l.Varsayilan) return YildizFiyatSozlugu.Varsayilan(dil);
        var bagli = cariler.Where(c => c.FiyatListesiId == l.Id).ToList();
        var toptan = bagli.Count(c => c.Segment == "TOPTAN");
        var diger = bagli.Count - toptan;
        var parca = new List<string>();
        if (toptan > 0) parca.Add(YildizFiyatSozlugu.Toptanci(toptan, dil));
        if (diger > 0) parca.Add(YildizFiyatSozlugu.Musteri(diger, dil));
        return parca.Count == 0 ? YildizFiyatSozlugu.Kullanilmiyor(dil) : string.Join(" · ", parca);
    }

    private YildizListeBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var urun = yildizUrunSayisi is int n ? (n >= YildizUrunSiniri ? $"{YildizListeBicim.Adet(n, dil)}+" : YildizListeBicim.Adet(n, dil)) : "…";
            var satirlar = listeler.OrderByDescending(l => l.Varsayilan).ThenByDescending(l => l.Aktif)
                .ThenBy(l => l.Ad, StringComparer.CurrentCulture).Select(l => new YildizListeSatiri(l.Id,
            [
                new(l.Ad, YildizHucreTuru.Ad, $"yildiz-fiyat-ad-{l.Id}"),
                new(YildizListeBicim.Yuzde(l.IscilikCarpanYuzde, dil), YildizHucreTuru.Deger),
                new(YildizListeBicim.Yuzde(l.HasMarjYuzde, dil), YildizHucreTuru.Deger),
                new(urun, YildizHucreTuru.Deger),
                new(yildizYuklendi ? YildizKullanim(l, yildizCariler, dil) : "…"),
            ],
            new YildizEylem(YildizFiyatSozlugu.Duzenle(dil), $"fl-duzenle-{l.Id}", EventCallback.Factory.Create(this, () => YildizDuzenle(l))),
            EventCallback.Factory.Create(this, () => YildizEtkisi(l)),
            Soluk: !l.Aktif,
            AcEtiketi: YildizFiyatSozlugu.Etkisi(l.Ad, dil))).ToList();
            return new YildizListeBaglami
            {
                Kok = "yildiz-fiyat",
                Tur = "fiyat",
                Ust = YildizFiyatSozlugu.Ust(dil),
                Baslik = YildizListeBicim.EkranAdi("/fiyatlandirma", Dil),
                B1Etiket = YildizFiyatSozlugu.YeniListe(dil),
                B1Id = "fl-yeni",
                B1Tikla = EventCallback.Factory.Create(this, YildizYeniListe),
                Sutunlar =
                [
                    new(YildizFiyatSozlugu.Liste(dil)), new(YildizFiyatSozlugu.Iscilik(dil), Sag: true),
                    new(YildizFiyatSozlugu.Kar(dil), Sag: true), new(YildizFiyatSozlugu.Urun(dil), Sag: true),
                    new(YildizFiyatSozlugu.Kullanim(dil)),
                ],
                EylemSutunu = true,
                Satirlar = satirlar,
                Yukleniyor = yukleniyor,
                BosMetni = hata is not null || !yuklendi ? null : Dil.T.Fiyat.BosListe,
                SinirNotu = yildizCariler.Count >= YildizCariSiniri ? YildizFiyatSozlugu.Sinir(YildizCariSiniri, dil) : null,
                Not = YildizFiyatSozlugu.Not(dil),
                Bantlar = YildizBantlar,
                AyrintiAcik = yildizAyrinti,
                AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                AyrintiKapat = EventCallback.Factory.Create(this, YildizListeyeDon),
            };
        }
    }

    // ── YILDIZ-5A tur 2 (tasarım hakemi KRİTİK): "Yeni liste" ve "Düzenle" eski ekrana GEÇMEZ ────────────────────
    // Form tuval form kalıbında (YildizForm · Y_Gidervevirman): "Tür" seçimi · Liste adı · Kâr · İşçilik (tuval sütun adları,
    // UX-S31) + makul aralık onayı; Varsayılan/Aktif "+ Ayrıntı ekle" arkasında. Alanlar özgün formun bileşenleri (aynı kimlik,
    // aynı listeForm/sayiKapisi); Kaydet = MEVCUT ListeKaydet (POST/PATCH /fiyat/listeler, gövde AYNI). Kampanyalar ve
    // kullanımdan kaldırma "Tüm ayrıntılar"da. Özgün form bu görünümde çizilmez → kimlik tek.
    private bool yildizFiyatEk;

    /// <summary>Yıldız'da liste formu açık mı.</summary>
    private bool YildizListeFormu => yeniListe is not null && !yildizAyrinti;

    private void YildizYeniListe() { yildizAyrinti = false; yildizFiyatEk = false; ListeFormuAc(); }
    private void YildizDuzenle(Liste l) { yildizAyrinti = false; yildizFiyatEk = false; ListeDuzenle(l); }

    private async Task YildizListeKaydet()
    {
        await ListeKaydet();
        if (yeniListe is null) yildizYuklendi = false;   // ürün/kullanım sayısı yeniden okunur
    }

    private YildizFormBaglami YildizListeFormBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var f = yeniListe;
            return new YildizFormBaglami
            {
                Kok = "yildiz-fiyat-form",
                Ust = YildizFiyatSozlugu.Ust(dil),
                Baslik = string.IsNullOrEmpty(f?.Id) ? YildizFiyatSozlugu.YeniListe(dil) : f!.Ad,
                SecimEtiketi = YildizForm5ASozlugu.Tur(dil),
                Secimler = [.. ListeTurSecenekleri.Select(t => (t.Deger, t.Ad, f?.Tur == t.Deger,
                    EventCallback.Factory.Create(this, () => { if (yeniListe is { } y) y.Tur = t.Deger; })))],
                EkleEtiketi = yildizFiyatEk ? null : YildizForm5ASozlugu.AyrintiEkle(dil),
                EkleTikla = EventCallback.Factory.Create(this, () => yildizFiyatEk = true),
                B1Etiket = YildizForm5ASozlugu.Kaydet(dil), B1Id = "fl-kaydet",
                B1Tikla = EventCallback.Factory.Create(this, YildizListeKaydet),
                B1Kilitli = kaydediliyor || sayiKapisi.OnekleVar("fl-") || RiskOnayiBekliyor,
                B1Kilit = RiskOnayiBekliyor ? Dil.T.Fiyat.RiskOnayiSart : null,
                B2Etiket = YildizForm5ASozlugu.Vazgec(dil), B2Id = "fl-vazgec",
                B2Tikla = EventCallback.Factory.Create(this, ListeFormuKapat),
                NotBaslik = YildizForm5ASozlugu.Not(dil),
                Not = YildizFiyatSozlugu.Not(dil),
                Bantlar = YildizFormBantlar,
            };
        }
    }

    private RenderFragment YildizFormBantlar => b =>
    {
        b.OpenComponent<HataBandi>(0);
        b.AddAttribute(1, nameof(HataBandi.Mesaj), listeHata ?? hata);
        b.AddAttribute(2, nameof(HataBandi.Id), "hata-bandi");
        b.CloseComponent();
    };
    private void YildizEtkisi(Liste l) { yildizAyrinti = true; if (seciliListe?.Id != l.Id) PaneliAc(l); }

    private void YildizListeyeDon()
    {
        yildizAyrinti = false;
        yildizFiyatEk = false;
        ListeFormuKapat();
        seciliListe = null;
        yildizYuklendi = false;   // kullanım/ürün sayısı ayrıntıda değişmiş olabilir → yeniden okunur
    }

    private RenderFragment YildizBantlar => b =>
    {
        b.OpenComponent<HataBandi>(0);
        b.AddAttribute(1, nameof(HataBandi.Mesaj), hata ?? yildizHata);
        b.AddAttribute(2, nameof(HataBandi.Id), "hata-bandi");
        b.CloseComponent();
        b.OpenComponent<BasariBandi>(3);
        b.AddAttribute(4, nameof(BasariBandi.Mesaj), listeBasari);
        b.AddAttribute(5, nameof(BasariBandi.Jeton), listeJeton);
        b.AddAttribute(6, nameof(BasariBandi.SureliMs), 3000);
        b.CloseComponent();
    };
}
