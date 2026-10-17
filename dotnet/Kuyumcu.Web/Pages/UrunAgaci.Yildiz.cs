using Kuyumcu.Contracts.Sayfalar.UrunAgaci;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

/// <summary>GET /urunler satırının ağaç toplamı için okunan iki alanı (yanıtta ZATEN var: varlığın kendisi).</summary>
public sealed record YildizAgacUrun(string Id, string? KategoriId);

/// <summary>Bir ağaç satırının toplamları: ürün sayısı · stok adedi · stok has gramı.</summary>
public sealed record YildizAgacToplam(int Urun, decimal Adet, decimal Has);

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5A — Ürün ağacı "Yıldız" yüzü (tuval Y_Urunagaci · BİREBİR).
// Yalnız GÖRÜNÜM + OKUMA: gruplar sayfanın MEVCUT ağacıdır (GET /urun-kategorileri); "Ürün ·
// Adet · Has" sütunları MEVCUT salt-okuma uçlarından toplanır (GET /urunler + GET
// /urunler/stok-adetleri — Ürünler ekranının kullandığı uçlar). Satır = MEVCUT grup çekmecesi;
// "Yeni grup" = MEVCUT oluşturma çekmecesi. Reyonlar ve pasif/aktif "Tüm ayrıntılar"da.
// Öteki 8 şemada bu dosyanın hiçbir üyesi çizime girmez (YildizMi = false).
// ═════════════════════════════════════════════════════════════════════════════
public partial class UrunAgaci
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private bool YildizOzgunCiz => yildizAyrinti;
    private string? YildizOzgunIsaret => "agac";

    /// <summary>Toplam için okunan en çok ürün (stok-adetleri ucunun tek istek sınırı, UrunStokOzetController.EnFazlaKimlik).</summary>
    public const int YildizUrunSiniri = 500;

    /// <summary>Alt grup satırının girintisi (tuval "&amp;nbsp;" × 4).</summary>
    public const string YildizGirinti = "    ";

    private bool yildizAyrinti;
    private bool yildizYukleniyor;
    private bool yildizYuklendi;
    private bool yildizSinirda;
    private string? yildizHata;
    private List<YildizAgacUrun> yildizUrunler = [];
    private Dictionary<string, (decimal Adet, decimal Has)> yildizStok = new(StringComparer.Ordinal);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (yildizYuklendi || yildizYukleniyor) return;
        await YildizYukle();
        StateHasChanged();
    }

    private async Task YildizYukle()
    {
        yildizYukleniyor = true;
        yildizHata = null;
        try
        {
            yildizUrunler = await Api.GetAsync<List<YildizAgacUrun>>($"/urunler?aktif=true&limit={YildizUrunSiniri}");
            yildizSinirda = yildizUrunler.Count >= YildizUrunSiniri;
            if (yildizUrunler.Count > 0)
            {
                var y = await Api.GetAsync<Kuyumcu.Contracts.Sayfalar.Urunler.StokAdetleriYaniti>(
                    $"/urunler/stok-adetleri?ids={Uri.EscapeDataString(string.Join(',', yildizUrunler.Select(u => u.Id)))}");
                yildizStok = (y?.Urunler ?? []).ToDictionary(s => s.UrunId, s => (s.ToplamAdet, s.ToplamHasGram), StringComparer.Ordinal);
            }
            else yildizStok = new(StringComparer.Ordinal);
        }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException) { yildizHata = ex.Message; }
        yildizYukleniyor = false;
        yildizYuklendi = true;
    }

    /// <summary>Bir düğümün ALT AĞACINDAKİ kategori kimlikleri (kendisi dahil) — saf.</summary>
    public static HashSet<string> YildizAltAgac(KategoriDugum k)
    {
        var s = new HashSet<string>(StringComparer.Ordinal) { k.Id };
        foreach (var c in k.Cocuklar) s.UnionWith(YildizAltAgac(c));
        return s;
    }

    /// <summary>Kimlik kümesindeki ürünlerin toplamı (saf): ürün sayısı, stok adedi, stok has gramı.</summary>
    public static YildizAgacToplam YildizTopla(HashSet<string> kategoriler, IEnumerable<YildizAgacUrun> urunler,
                                                IReadOnlyDictionary<string, (decimal Adet, decimal Has)> stok)
    {
        var secili = urunler.Where(u => u.KategoriId is { } k && kategoriler.Contains(k)).ToList();
        decimal adet = 0m, has = 0m;
        foreach (var u in secili)
            if (stok.TryGetValue(u.Id, out var s)) { adet += s.Adet; has += s.Has; }
        return new(secili.Count, adet, has);
    }

    /// <summary>Tuval satırları (saf): her kök grup + (alt grubu varsa) alt grupların adları tek satırda, girintili.</summary>
    public static List<(string Id, string Metin, YildizAgacToplam Toplam, KategoriDugum? Grup, bool Pasif)> YildizSatirlar(
        IEnumerable<KategoriDugum> agac, IEnumerable<YildizAgacUrun> urunler, IReadOnlyDictionary<string, (decimal Adet, decimal Has)> stok)
    {
        var liste = urunler.ToList();
        var sonuc = new List<(string, string, YildizAgacToplam, KategoriDugum?, bool)>();
        foreach (var k in agac.OrderBy(x => x.Sira).ThenBy(x => x.Ad, StringComparer.CurrentCulture))
        {
            sonuc.Add((k.Id, k.Ad, YildizTopla(YildizAltAgac(k), liste, stok), k, !k.Aktif));
            var cocuklar = k.Cocuklar.OrderBy(x => x.Sira).ThenBy(x => x.Ad, StringComparer.CurrentCulture).ToList();
            if (cocuklar.Count == 0) continue;
            var alt = new HashSet<string>(StringComparer.Ordinal);
            foreach (var c in cocuklar) alt.UnionWith(YildizAltAgac(c));
            sonuc.Add((k.Id + "-alt", YildizGirinti + string.Join(" · ", cocuklar.Select(c => c.Ad)),
                       YildizTopla(alt, liste, stok), null, !k.Aktif));
        }
        return sonuc;
    }

    private YildizListeBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var satirlar = YildizSatirlar(agac, yildizUrunler, yildizStok).Select(r => new YildizListeSatiri(r.Id,
            [
                new(r.Metin, YildizHucreTuru.Ad, $"yildiz-agac-ad-{r.Id}"),
                new(YildizListeBicim.Adet(r.Toplam.Urun, dil), YildizHucreTuru.Deger),
                new(YildizListeBicim.Adet(r.Toplam.Adet, dil), YildizHucreTuru.Deger),
                new(YildizListeBicim.HasIki(r.Toplam.Has, dil), YildizHucreTuru.Deger),
            ],
            null,
            r.Grup is { } g ? EventCallback.Factory.Create(this, () => KategoriModalAc(g)) : default,
            Soluk: r.Pasif,
            AcEtiketi: r.Grup is { } g2 ? YildizAgacSozlugu.Duzenle(g2.Ad, dil) : null)).ToList();
            return new YildizListeBaglami
            {
                Kok = "yildiz-agac",
                Tur = "agac",
                Ust = YildizAgacSozlugu.Ust(dil),
                Baslik = YildizListeBicim.EkranAdi("/urunler/agac", Dil),
                B1Etiket = YildizAgacSozlugu.YeniGrup(dil),
                B1Id = "agac-yeni-kategori",
                B1Tikla = EventCallback.Factory.Create(this, () => KategoriModalAc(null)),
                Sutunlar =
                [
                    new(YildizAgacSozlugu.Grup(dil)), new(YildizAgacSozlugu.Urun(dil), Sag: true),
                    new(YildizAgacSozlugu.Adet(dil), Sag: true), new(YildizAgacSozlugu.Has(dil), Sag: true),
                ],
                Satirlar = satirlar,
                Yukleniyor = kategoriYukleniyor || yildizYukleniyor || !yildizYuklendi,
                BosMetni = kategoriHata is not null || yildizHata is not null ? null : Dil.T.UrunAgaci.KategoriYok,
                SinirNotu = yildizSinirda ? YildizAgacSozlugu.Sinir(YildizUrunSiniri, dil) : null,
                Not = YildizAgacSozlugu.Not(dil),
                Bantlar = YildizBantlar,
                AyrintiAcik = yildizAyrinti,
                AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                AyrintiKapat = EventCallback.Factory.Create(this, YildizListeyeDon),
            };
        }
    }

    private async Task YildizListeyeDon()
    {
        yildizAyrinti = false;
        await YildizYukle();
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
        b.AddAttribute(6, nameof(HataBandi.Mesaj), kategoriHata ?? yildizHata);
        b.AddAttribute(7, nameof(HataBandi.Id), "hata-bandi");
        b.CloseComponent();
    };
}
