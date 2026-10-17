using Kuyumcu.Contracts.Sayfalar.Emanet;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5A — Emanet "Yıldız" yüzü (tuval Y_Emanet · BİREBİR).
// Yalnız GÖRÜNÜM + OKUMA: liste, müşterilerin emanet satırlarından kurulur (GET /cari ve
// GET /emanet/{id} — ekranın ve Bugün'ün kullandığı MEVCUT uçlar; yeni uç YOK). Emanet alma,
// iade ve geri alma Emanet.razor'un MEVCUT çekmeceleri/yollarıdır (davranış değişmez).
// Tema temizliği (17.10): tek şema Yıldız — bu yüz her zaman çizilir; özgün gövde "Tüm ayrıntılar" arkasındadır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>GET /cari satırının Yıldız listesinin okuduğu üç alanı (yanıtta ZATEN var).</summary>
public sealed record YildizEmanetCari(string Id, string Ad, decimal EmanetteHas = 0m);

/// <summary>Emanet satırının durumu (tuval "30 günü aştı" · "Emanette" · "Geri verildi").</summary>
public enum YildizEmanetDurum { Emanette, Asti, GeriVerildi }

/// <summary>Bir emanet girişi (tuval satırı).</summary>
public sealed record YildizEmanetKaydi(string CariId, string CariAd, string HareketId, DateTime Tarih,
                                       decimal Has, string? Aciklama, string Maden, YildizEmanetDurum Durum,
                                       // P-DIL-BORC-KASA (Y18): bırakıldığı gibi iz (tuval "22 ayar · 12,5 g · taş 0,5 g"); eski satırda null.
                                       string? AyarId = null, decimal? Gram = null, decimal? Tas = null);

public partial class Emanet
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    /// <summary>Etkin şema Yıldız mı (kabukla AYNI karar noktası)?</summary>

    /// <summary>Yıldız'da özgün ekran yalnız "ayrıntı" görünümünde çizilir (liste = tuval).</summary>
    private bool YildizOzgunCiz => yildizAyrinti;
    private string? YildizOzgunIsaret => "emanet";

    /// <summary>Listede okunacak en çok müşteri (her biri GET /emanet/{id}; Bugün'le aynı sınır).</summary>
    public const int YildizOkumaSiniri = YildizBugunBicim.EmanetOkumaSiniri;

    private bool yildizAyrinti;
    private bool yildizYukleniyor;
    private bool yildizYuklendi;
    private bool yildizSinirda;
    private string? yildizHata;
    private string yildizSekme = "hepsi";
    private string yildizArama = "";
    private Dictionary<string, (string Ad, List<HareketVeri> Hareketler)> yildizEmanetler = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (yildizYuklendi || yildizYukleniyor) return;
        await YildizYukle();
        StateHasChanged();
    }

    /// <summary>Emaneti olan müşteriler (sunucunun emanet payı &gt; 0) ve her birinin emanet hareketleri — YALNIZ OKUMA.</summary>
    private async Task YildizYukle()
    {
        yildizYukleniyor = true;
        yildizHata = null;
        try
        {
            await SubeliSorgu.HazirAsync(Oturum);
            var liste = await Api.GetAsync<List<YildizEmanetCari>>($"/cari?aktif=true&limit={CariLimit}");
            var sira = YildizOkumaSirasi(liste);
            yildizSinirda = sira.Count > YildizOkumaSiniri || liste.Count >= CariLimit;
            var yeni = new Dictionary<string, (string, List<HareketVeri>)>(StringComparer.Ordinal);
            foreach (var c in sira.Take(YildizOkumaSiniri))
            {
                var y = await Api.GetAsync<EmanetYaniti>(SubeliSorgu.Ekle($"/emanet/{c.Id}", Oturum));
                if ((y.Hareketler ?? []).Count > 0) yeni[c.Id] = (c.Ad, y.Hareketler ?? []);
            }
            yildizEmanetler = yeni;
        }
        // UX-S28: hata "kayıt yok"a çevrilmez — eldeki liste kalır, hata bandı söyler.
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException) { yildizHata = ex.Message; }
        yildizYukleniyor = false;
        yildizYuklendi = true;
    }

    /// <summary>
    /// Okuma sırası (saf): önce emaneti SÜREN müşteriler (büyükten küçüğe), sonra ötekiler — "Geri verildi"
    /// satırları emaneti bitmiş müşterilerdedir; sunucunun müşteri listesinde emanet geçmişi alanı yok.
    /// </summary>
    public static List<YildizEmanetCari> YildizOkumaSirasi(IEnumerable<YildizEmanetCari> liste) =>
        [.. liste.OrderByDescending(c => c.EmanetteHas > 0m).ThenByDescending(c => c.EmanetteHas).ThenBy(c => c.Ad, StringComparer.CurrentCulture)];

    /// <summary>
    /// Bir müşterinin emanet GİRİŞLERİ ve durumları (saf). Geri alınan (storno) giriş ve çıkış sayılmaz;
    /// çıkışlar maden maden EN ESKİ girişten düşülür: tamamı karşılanan giriş "Geri verildi", kalanı
    /// "Emanette" — eşikten (<see cref="YildizBugunBicim.EmanetEsikGun"/>) eski olan "N günü aştı".
    /// </summary>
    /// <summary>
    /// "Ne bıraktı" hücresi (tuval Y_Emanet): ayar + gram biliniyorsa "22 ayar · 12,50 g · taş 0,50 g"
    /// (açıklama varsa önde: "tamir · 22 ayar · 3,20 g"); bilinmiyorsa (eski satır) açıklama ya da maden adı.
    /// </summary>
    public static string YildizNeBirakti(YildizEmanetKaydi k, string dil)
    {
        var aciklama = string.IsNullOrWhiteSpace(k.Aciklama) ? null : k.Aciklama!.Trim();
        if (k.AyarId is not null && k.Gram is > 0m)
        {
            var iz = EmanetDiliSozlugu.Satir(k.AyarId, k.Gram.Value, k.Tas, dil);
            return aciklama is null ? iz : $"{aciklama} · {iz}";
        }
        return aciklama ?? KurKaynakAdi.MadenAdi(k.Maden);
    }

    public static List<YildizEmanetKaydi> YildizKayitlari(string cariId, string cariAd, IReadOnlyList<HareketVeri> hareketler, DateOnly bugun)
    {
        var stornolu = hareketler.Where(h => h.StornoKaynakId is not null).Select(h => h.StornoKaynakId!).ToHashSet(StringComparer.Ordinal);
        bool Gecerli(HareketVeri h) => h.StornoKaynakId is null && !stornolu.Contains(h.Id);
        static string MadenKodu(HareketVeri h) => string.IsNullOrWhiteSpace(h.Maden) ? Kuyumcu.Core.Domain.Maden.Altin : h.Maden!;
        var sonuc = new List<YildizEmanetKaydi>();
        foreach (var g in hareketler.Where(Gecerli).GroupBy(MadenKodu))
        {
            var cikan = g.Where(h => h.Tur == "EMANET_CIKIS").Sum(h => Math.Abs(h.HasGrami));
            foreach (var h in g.Where(h => h.Tur == "EMANET_GIRIS").OrderBy(h => h.Tarih))
            {
                var karsilanan = Math.Min(cikan, h.HasGrami);
                cikan -= karsilanan;
                var geri = Math.Round(h.HasGrami - karsilanan, 4, MidpointRounding.AwayFromZero) <= 0m;
                var gun = bugun.DayNumber - DateOnly.FromDateTime(Bicim.IsletmeZamani(h.Tarih)).DayNumber;
                var durum = geri ? YildizEmanetDurum.GeriVerildi
                    : gun > YildizBugunBicim.EmanetEsikGun ? YildizEmanetDurum.Asti : YildizEmanetDurum.Emanette;
                sonuc.Add(new(cariId, cariAd, h.Id, h.Tarih, h.HasGrami, h.Aciklama, g.Key, durum,
                              h.EmanetAyarId, h.EmanetGram, h.EmanetTas));
            }
        }
        return sonuc;
    }

    /// <summary>Sekme süzgeci (saf): hepsi · emanette (aşanlar dahil) · geri.</summary>
    public static bool YildizSekmeyeUyar(YildizEmanetDurum d, string sekme) => sekme switch
    {
        "emanette" => d != YildizEmanetDurum.GeriVerildi,
        "geri" => d == YildizEmanetDurum.GeriVerildi,
        _ => true,
    };

    private IEnumerable<YildizEmanetKaydi> YildizTumKayitlar()
    {
        var bugun = DateOnly.FromDateTime(Bicim.IsletmeBugun);
        foreach (var (id, (ad, hs)) in yildizEmanetler)
        {
            // Seçili müşteride ekranın TAZE hareketleri (çekmeceden kayıt sonrası sayfa yeniden okur).
            var kaynak = id == cariId && !yukleniyor && listeHata is null ? hareketler : hs;
            foreach (var k in YildizKayitlari(id, ad, kaynak, bugun)) yield return k;
        }
    }

    private string YildizDurumMetni(YildizEmanetDurum d) => d switch
    {
        YildizEmanetDurum.Asti => YildizEmanetSozlugu.GunuAsti(YildizBugunBicim.EmanetEsikGun, Dil.Dil),
        YildizEmanetDurum.GeriVerildi => YildizEmanetSozlugu.GeriVerildi(Dil.Dil),
        _ => YildizEmanetSozlugu.Emanette(Dil.Dil),
    };

    private YildizListeBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var hepsi = YildizTumKayitlar().OrderBy(k => k.Tarih).ToList();
            var gorunen = hepsi.Where(k => YildizSekmeyeUyar(k.Durum, yildizSekme))
                .Where(k => string.IsNullOrWhiteSpace(yildizArama) || k.CariAd.Contains(yildizArama.Trim(), StringComparison.CurrentCultureIgnoreCase))
                .ToList();
            var satirlar = gorunen.Select(k => new YildizListeSatiri(k.HareketId,
            [
                new(k.CariAd, YildizHucreTuru.Ad, $"yildiz-emanet-musteri-{k.HareketId}"),
                new(YildizNeBirakti(k, dil)),
                new(YildizListeBicim.HasDort(k.Has, dil), YildizHucreTuru.Deger),
                new(YildizListeBicim.Gun(k.Tarih, dil)),
                new(YildizDurumMetni(k.Durum)),
            ],
            new YildizEylem(YildizEmanetSozlugu.GeriVer(dil), $"yildiz-emanet-geri-{k.HareketId}",
                            EventCallback.Factory.Create(this, () => YildizGeriVer(k.CariId)),
                            KilitSebebi: k.Durum == YildizEmanetDurum.GeriVerildi ? YildizEmanetSozlugu.GeriVerildiKilit(dil) : null),
            EventCallback.Factory.Create(this, () => YildizMusteriAc(k.CariId)),
            AcEtiketi: YildizEmanetSozlugu.Ayrinti(k.CariAd, dil))).ToList();
            return new YildizListeBaglami
            {
                Kok = "yildiz-emanet",
                Tur = "emanet",
                Ust = YildizEmanetSozlugu.Ust(dil),
                Baslik = YildizListeBicim.EkranAdi("/emanet", Dil),
                AramaId = "yildiz-emanet-arama",
                AramaEtiket = YildizEmanetSozlugu.AraYerTutucu(dil),
                AramaYerTutucu = YildizEmanetSozlugu.AraYerTutucu(dil),
                Arama = yildizArama,
                AramaDegisti = EventCallback.Factory.Create<string?>(this, v => yildizArama = v ?? ""),
                B1Etiket = YildizEmanetSozlugu.EmanetAl(dil),
                B1Id = "yildiz-emanet-al",
                B1Tikla = EventCallback.Factory.Create(this, YildizEmanetAl),
                SekmelerId = "yildiz-emanet-sekmeler",
                Sekmeler =
                [
                    new("hepsi", YildizListeSozlugu.Hepsi(dil), yildizSekme == "hepsi"),
                    new("emanette", YildizEmanetSozlugu.Emanette(dil), yildizSekme == "emanette"),
                    new("geri", YildizEmanetSozlugu.GeriVerildi(dil), yildizSekme == "geri"),
                ],
                SekmeSec = EventCallback.Factory.Create<string>(this, k => yildizSekme = k),
                Sutunlar =
                [
                    new(YildizEmanetSozlugu.Musteri(dil)), new(YildizEmanetSozlugu.NeBirakti(dil)),
                    new(YildizEmanetSozlugu.Has(dil), Sag: true), new(YildizEmanetSozlugu.NeZaman(dil)),
                    new(YildizEmanetSozlugu.Durum(dil)),
                ],
                EylemSutunu = true,
                Satirlar = satirlar,
                Yukleniyor = yildizYukleniyor || !yildizYuklendi,
                BosMetni = yildizHata is not null ? null
                    : hepsi.Count == 0 ? YildizEmanetSozlugu.Bos(dil) : YildizEmanetSozlugu.SekmeBos(dil),
                SinirNotu = yildizSinirda ? YildizEmanetSozlugu.Sinir(YildizOkumaSiniri, dil) : null,
                Not = YildizEmanetSozlugu.Not(dil),
                Bantlar = YildizBantlar,
                AyrintiAcik = yildizAyrinti,
                AyrintiKapat = EventCallback.Factory.Create(this, YildizListeyeDon),
            };
        }
    }

    /// <summary>Liste görünümünün bantları: sayfanın MEVCUT başarı/hata mesajları (özgün kök çizilmezken).</summary>
    private RenderFragment YildizBantlar => b =>
    {
        b.OpenComponent<BasariBandi>(0);
        b.AddAttribute(1, nameof(BasariBandi.Mesaj), basari);
        b.AddAttribute(2, nameof(BasariBandi.Jeton), basariJeton);
        b.AddAttribute(3, nameof(BasariBandi.SureliMs), 3000);
        b.AddAttribute(4, nameof(BasariBandi.Id), "basari-bandi");
        b.CloseComponent();
        b.OpenComponent<HataBandi>(5);
        b.AddAttribute(6, nameof(HataBandi.Mesaj), yildizHata ?? cariHata ?? kurHata);
        b.AddAttribute(7, nameof(HataBandi.Id), "hata-bandi");
        b.CloseComponent();
    };

    // ── YILDIZ-5A tur 2 (tasarım hakemi KRİTİK): "Emanet al" eski ekrana GEÇMEZ ─────────────────────────────
    // Form tuval form kalıbında (YildizForm · Y_Gidervevirman): Müşteri · Ayar · Gram zorunlu; taş ve not "+ Ayrıntı ekle"
    // arkasında. Alanlar özgün çekmecenin bileşenleri (aynı kimlik, aynı girisForm, aynı HamMetinKabi); Kaydet = MEVCUT
    // GirisKaydet (POST /emanet/giris, gövde AYNI). Özgün çekmece bu görünümde açılmaz (girisAcik = false) → kimlik tek.
    private bool yildizGiris;
    private bool yildizGirisEk;
    private string? yildizCariHata;

    /// <summary>Yıldız'da emanet alma formu açık mı.</summary>
    private bool YildizGirisFormu => yildizGiris && !yildizAyrinti;

    /// <summary>"Emanet al": MEVCUT form sıfırlaması (GirisModalAc) + Yıldız form kalıbı.</summary>
    private void YildizEmanetAl()
    {
        GirisModalAc();
        girisAcik = false;
        yildizGirisEk = false;
        yildizCariHata = null;
        yildizGiris = true;
    }

    /// <summary>Kaydet: müşteri seçilmediyse alan altında söyler (sessiz etkisizlik yok), sonra MEVCUT GirisKaydet.</summary>
    private async Task YildizGirisKaydet()
    {
        yildizCariHata = null;
        if (string.IsNullOrEmpty(cariId)) { yildizCariHata = OnayM.EmanetMusteriSecilmedi; girisHata = Dil.T.Kabuk.FormEksik; return; }
        if (kur is null) { girisHata = OnayM.EmanetKurYok; return; }
        await GirisKaydet();
        if (girisHata is null)
        {
            yildizGiris = false;
            await YildizYukle();
        }
    }

    private async Task YildizCariSecildi()
    {
        yildizCariHata = null;
        await CariDegisti();
    }

    private YildizFormBaglami YildizGirisBaglami
    {
        get
        {
            var dil = Dil.Dil;
            return new YildizFormBaglami
            {
                Kok = "yildiz-emanet-form",
                Ust = YildizEmanetSozlugu.Ust(dil),
                Baslik = YildizEmanetSozlugu.EmanetAl(dil),
                EkleEtiketi = yildizGirisEk || GirisDetayAcik ? null : YildizForm5ASozlugu.AyrintiEkle(dil),
                EkleTikla = EventCallback.Factory.Create(this, () => yildizGirisEk = true),
                B1Etiket = YildizForm5ASozlugu.Kaydet(dil), B1Id = "e-giris-kaydet",
                B1Tikla = EventCallback.Factory.Create(this, YildizGirisKaydet),
                B1Kilitli = kaydediliyor,
                B2Etiket = YildizForm5ASozlugu.Vazgec(dil), B2Id = "e-giris-vazgec",
                B2Tikla = EventCallback.Factory.Create(this, () => yildizGiris = false),
                NotBaslik = YildizForm5ASozlugu.Not(dil),
                Not = YildizEmanetSozlugu.Not(dil),
                Bantlar = YildizGirisBantlar,
            };
        }
    }

    private RenderFragment YildizGirisBantlar => b =>
    {
        b.OpenComponent<HataBandi>(0);
        b.AddAttribute(1, nameof(HataBandi.Mesaj), girisHata ?? kurHata ?? cariHata);
        b.AddAttribute(2, nameof(HataBandi.Id), "hata-bandi");
        b.CloseComponent();
    };

    private async Task YildizMusteriAc(string id)
    {
        cariId = id;
        yildizAyrinti = true;
        await CariDegisti();
    }

    /// <summary>"Geri ver": müşteri seçilir, MEVCUT iade çekmecesi açılır (kilitliyse sebebi bantta).</summary>
    private async Task YildizGeriVer(string id)
    {
        cariId = id;
        await CariDegisti();
        if (CikisKilitSebebi is { } sebep) { yildizHata = sebep; return; }
        yildizHata = null;
        CikisModalAc();
    }

    private async Task YildizListeyeDon()
    {
        yildizAyrinti = false;
        yildizGiris = false;
        await YildizYukle();
    }
}
