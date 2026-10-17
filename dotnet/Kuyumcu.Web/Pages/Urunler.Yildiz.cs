using Kuyumcu.Contracts.Sayfalar.Urunler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5A — Ürünler "Yıldız" yüzü (tuval Y_Urunler · BİREBİR).
// Yalnız GÖRÜNÜM + OKUMA: satırlar sayfanın MEVCUT listesi ve stok adetleridir; "Fiyat" sütunu
// satıştaki fiyatla AYNI motordan okunur (GET /kur/guncel + GET /fiyat/teklifler — tezgâhın
// kullandığı MEVCUT salt-okuma uçları; istemcide fiyat HESAPLANMAZ). Sekmeler ürün ağacının
// kök gruplarıdır (sayfanın kategori süzgeci). Satır = MEVCUT düzenleme çekmecesi.
// Tema temizliği (17.10): tek şema Yıldız — bu yüz her zaman çizilir; özgün gövde "Tüm ayrıntılar" arkasındadır.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Urunler
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private bool YildizOzgunCiz => yildizAyrinti;
    private string? YildizOzgunIsaret => "urun";

    /// <summary>/fiyat/teklifler tek istekte en çok 25 ürün (sunucu sabiti FiyatTopluTeklifController.EnFazlaUrun).</summary>
    public const int YildizTeklifParcasi = 25;

    private bool yildizAyrinti;
    private bool yildizFiyatOkunuyor;
    private string? yildizKurId;
    private string? yildizFiyatHata;
    private readonly Dictionary<string, decimal?> yildizFiyatlar = new(StringComparer.Ordinal);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (yildizAyrinti || yukleniyor || yildizFiyatOkunuyor || yildizFiyatHata is not null) return;
        var eksik = UrunListesi.Select(u => u.Id).Where(id => !yildizFiyatlar.ContainsKey(id)).Distinct().ToList();
        if (eksik.Count == 0) return;
        await YildizFiyatOku(eksik);
        StateHasChanged();
    }

    /// <summary>Satış fiyatı — tezgâhın arama satırıyla AYNI uç (salt okuma). Teklif yoksa hücre "—".</summary>
    private async Task YildizFiyatOku(List<string> kimlikler)
    {
        yildizFiyatOkunuyor = true;
        try
        {
            yildizKurId ??= (await Api.GetAsync<Kuyumcu.Contracts.Sayfalar.Fiyatlandirma.KurOzet>("/kur/guncel")).Id;
            if (string.IsNullOrEmpty(yildizKurId))
            {
                foreach (var id in kimlikler) yildizFiyatlar[id] = null;
            }
            else
            {
                foreach (var parca in kimlikler.Chunk(YildizTeklifParcasi))
                {
                    var y = await Api.GetAsync<Kuyumcu.Contracts.Fiyat.TopluTeklifYaniti>(
                        $"/fiyat/teklifler?urunIds={Uri.EscapeDataString(string.Join(',', parca))}&kurSnapshotId={Uri.EscapeDataString(yildizKurId)}");
                    var gelen = (y?.Satirlar ?? []).ToDictionary(s => s.UrunId, s => s.TeklifVar ? s.SatisFiyati : null, StringComparer.Ordinal);
                    foreach (var id in parca) yildizFiyatlar[id] = gelen.TryGetValue(id, out var f) ? f : null;
                }
            }
        }
        // UX-S28: fiyat okunamazsa hücre "—" değil hata bandı söyler; liste yerinde kalır.
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException) { yildizFiyatHata = ex.Message; }
        yildizFiyatOkunuyor = false;
    }

    /// <summary>Ayar sütunu (saf): tuval milyem ("995", "916", "585"); bilinmeyen ayarda kod.</summary>
    public static string YildizMilyem(string ayarId) =>
        Kuyumcu.Core.Domain.Ayarlar.Bul(ayarId) is { } a
            ? a.Milyem.ToString("0", System.Globalization.CultureInfo.InvariantCulture) : ayarId;

    private string YildizAdet(UrunVeri u) =>
        !stokAdetleri.TryGetValue(u.Id, out var a) ? L.StokYukleniyor
        : a is { } adet ? YildizListeBicim.Adet(adet, Dil.Dil) : "?";

    private string YildizFiyat(UrunVeri u) =>
        yildizFiyatlar.TryGetValue(u.Id, out var f) ? (f is { } tl ? YildizListeBicim.Lira(tl) : "—") : "…";

    private YildizListeBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var kokler = kategoriAgaci.Where(k => k.Aktif).OrderBy(k => k.Sira).ToList();
            var satirlar = UrunListesi.Select(u => new YildizListeSatiri(u.Id,
            [
                new(u.Ad, YildizHucreTuru.Ad, $"yildiz-urun-ad-{u.Id}"),
                new(string.IsNullOrEmpty(u.Barkod) ? "—" : u.Barkod!),
                new(YildizAyarKisa(u.AyarId), YildizHucreTuru.Deger),   // P-URUN (O13): formla aynı dil "22"
                new(YildizListeBicim.Gram(u.GramAgirlik, dil), YildizHucreTuru.Deger),
                new(YildizAdet(u), YildizHucreTuru.Deger, $"yildiz-urun-adet-{u.Id}"),
                new(YildizFiyat(u), YildizHucreTuru.Deger, $"yildiz-urun-fiyat-{u.Id}"),
            ],
            null,
            EventCallback.Factory.Create(this, () => DuzenleAc(u.Id)),
            Soluk: !u.Aktif,
            AcEtiketi: YildizUrunSozlugu.Duzenle(u.Ad, dil))).ToList();
            return new YildizListeBaglami
            {
                Kok = "yildiz-urun",
                Tur = "urun",
                Ust = YildizUrunSozlugu.Ust(dil),
                Baslik = YildizListeBicim.EkranAdi("/urunler", Dil),
                AramaId = "urun-arama",
                AramaEtiket = Dil.T.Urun.AraPlaceholder,
                AramaYerTutucu = YildizUrunSozlugu.AraYerTutucu(dil),
                Arama = arama,
                AramaDegisti = EventCallback.Factory.Create<string?>(this, async v => { arama = v ?? ""; await Yukle(); }),
                AramaEnter = EventCallback.Factory.Create(this, BarkodBul),
                B1Etiket = YildizUrunSozlugu.YeniUrun(dil),
                B1Id = "urun-yeni-ac",
                B1Tikla = EventCallback.Factory.Create(this, YeniModalAc),
                // M4 (16.10) — "+ Fiyat kuralları" (isteğe bağlı; ana ekranda tek birincil düğme kalır).
                EkEtiket = UrunKuraliSozlugu.Ac(dil),
                EkId = "urun-kurallar-ac",
                EkTikla = EventCallback.Factory.Create(this, () => kuralAcik = true),
                SekmelerId = "yildiz-urun-sekmeler",
                Sekmeler =
                [
                    new("hepsi", YildizListeSozlugu.Hepsi(dil), string.IsNullOrEmpty(kategoriFil)),
                    .. kokler.Select(k => new YildizSekme(k.Id, k.Ad, kategoriFil == k.Id)),
                ],
                SekmeSec = EventCallback.Factory.Create<string>(this, YildizSekmeSec),
                Sutunlar =
                [
                    new(YildizUrunSozlugu.Urun(dil)), new(YildizUrunSozlugu.Barkod(dil)),
                    new(YildizUrunSozlugu.Ayar(dil), Sag: true), new(YildizUrunSozlugu.Gram(dil), Sag: true),
                    new(YildizUrunSozlugu.Adet(dil), Sag: true), new(YildizUrunSozlugu.Fiyat(dil), Sag: true),
                ],
                Satirlar = satirlar,
                Yukleniyor = yukleniyor,
                BosMetni = listeHata is not null ? null : Dil.T.Urun.UrunBulunamadi,
                SinirNotu = UrunListesi.Count >= ListeSiniri
                    ? $"{Dil.T.Kabuk.SinirAsildi} {string.Format(Dil.T.Kabuk.SinirUyarisi, ListeSiniri)}" : null,
                Not = YildizUrunSozlugu.Not(dil),
                Bantlar = YildizBantlar,
                AyrintiAcik = yildizAyrinti,
                AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
            };
        }
    }

    private Task YildizSekmeSec(string kod)
    {
        kategoriFil = kod == "hepsi" ? "" : kod;
        return Yukle();
    }

    private RenderFragment YildizBantlar => b =>
    {
        b.OpenComponent<BasariBandi>(0);
        b.AddAttribute(1, nameof(BasariBandi.Mesaj), basari);
        b.AddAttribute(2, nameof(BasariBandi.Jeton), basariJeton);
        b.AddAttribute(3, nameof(BasariBandi.SureliMs), 3000);
        b.AddAttribute(4, nameof(BasariBandi.Id), "basari-bandi");
        b.CloseComponent();
        b.OpenComponent<HataBandi>(5);
        b.AddAttribute(6, nameof(HataBandi.Mesaj), listeHata ?? barkodHata ?? stokAdetHata ?? yanKaynakHata ?? yildizFiyatHata);
        b.AddAttribute(7, nameof(HataBandi.Id), "hata-bandi");
        b.CloseComponent();
    };
}
