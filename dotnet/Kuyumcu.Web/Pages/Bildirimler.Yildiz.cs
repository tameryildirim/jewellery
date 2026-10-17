using Kuyumcu.Contracts.Sayfalar.Bildirimler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5A — Bildirimler "Yıldız" yüzü (tuval Y_Bildirimler · BİREBİR).
// Yalnız GÖRÜNÜM: satırlar sayfanın MEVCUT listesidir (GET /bildirimler); "Hepsi" = TUMU,
// "Okunmadı" = ACIK (sayfanın kendi sekme yolları). Satıra dokunmak MEVCUT kapatma onayını
// açar; "Git" bildirimin kendi hedef ekranıdır. Okunan (kapalı) satır soluk çizilir.
// Öteki 8 şemada bu dosyanın hiçbir üyesi çizime girmez (YildizMi = false).
// ═════════════════════════════════════════════════════════════════════════════
public partial class Bildirimler
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private bool YildizOzgunCiz => yildizAyrinti;
    private string? YildizOzgunIsaret => "bildirim";

    private bool yildizAyrinti;
    private bool yildizIlkSekme;

    /// <summary>Tuval açılışı "Hepsi" seçili: Yıldız'da ilk çizimden sonra bir kez sayfanın TUMU sekmesine geçilir.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (yildizIlkSekme || yukleniyor) return;
        yildizIlkSekme = true;
        if (durum == "ACIK") { await SekmeTumu(); StateHasChanged(); }
    }

    /// <summary>
    /// "Nereden" (saf): bildirimin hedef ekranının menü adı (UX-S31 tek kaynak — "Tüm ekranlar" karesiyle
    /// aynı ad); hedef yoksa ya da menüde değilse "Sistem".
    /// </summary>
    public static string YildizNereden(string? rota, DilServisi dil)
    {
        var yol = (rota ?? "").Split('?', '#')[0];
        var oge = Kuyumcu.Web.Layout.MenuTanimi.Ogeler
            .Where(o => o.Yol != "/" && (yol == o.Yol || yol.StartsWith(o.Yol + "/", StringComparison.Ordinal)))
            .OrderByDescending(o => o.Yol.Length).FirstOrDefault();
        return oge is null ? YildizBildirimSozlugu.Sistem(dil.Dil) : YildizListeBicim.EkranAdi(oge.Yol, dil);
    }

    private YildizListeBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var bugun = DateOnly.FromDateTime(Bicim.IsletmeBugun);
            var satirlar = (liste?.Satirlar ?? []).Select(b => new YildizListeSatiri(b.Id,
            [
                new(b.Baslik, YildizHucreTuru.Ad, $"yildiz-bildirim-baslik-{b.Id}"),
                new(YildizNereden(b.HedefRota, Dil)),
                new(YildizListeBicim.GunSaat(b.Olusturuldu, bugun, dil)),
            ],
            string.IsNullOrWhiteSpace(b.HedefRota)
                ? new YildizEylem(YildizBildirimSozlugu.Git(dil), $"bildirim-git-{b.Id}", KilitSebebi: YildizBildirimSozlugu.GitKilit(dil))
                : new YildizEylem(YildizBildirimSozlugu.Git(dil), $"bildirim-git-{b.Id}", Href: b.HedefRota),
            b.Kapatildi is null ? EventCallback.Factory.Create(this, () => KapatmaModaliAc(b.Id)) : default,
            Soluk: b.Kapatildi is not null,
            AcEtiketi: YildizBildirimSozlugu.OkunduSay(b.Baslik, dil))).ToList();
            return new YildizListeBaglami
            {
                Kok = "yildiz-bildirim",
                Tur = "bildirim",
                Ust = YildizBildirimSozlugu.Ust(dil),
                Baslik = YildizListeBicim.EkranAdi("/bildirimler", Dil),
                SekmelerId = "yildiz-bildirim-sekmeler",
                Sekmeler =
                [
                    new("tumu", YildizListeSozlugu.Hepsi(dil), durum != "ACIK"),
                    new("acik", YildizBildirimSozlugu.Okunmadi(dil), durum == "ACIK"),
                ],
                SekmeSec = EventCallback.Factory.Create<string>(this, k => k == "acik" ? SekmeAcik() : SekmeTumu()),
                Sutunlar =
                [
                    new(YildizBildirimSozlugu.NeOldu(dil)), new(YildizBildirimSozlugu.Nereden(dil)),
                    new(YildizBildirimSozlugu.NeZaman(dil)),
                ],
                EylemSutunu = true,
                Satirlar = satirlar,
                Yukleniyor = yukleniyor,
                BosMetni = hata is not null || liste is null ? null
                    : Dil.T.Bildirim.BosDurum,
                SinirNotu = liste is { DahaFazlasiVar: true } l
                    ? string.Format(System.Globalization.CultureInfo.InvariantCulture, Dil.T.Bildirim.SinirUyarisi, l.AzamiLimit) : null,
                Not = liste is { } y ? YildizBildirimSozlugu.Not(y.KapaliGosterimGun, dil) : null,
                Bantlar = YildizBantlar,
                AyrintiAcik = yildizAyrinti,
                // İş çatısı (iş ayarları, çalışmalar) tuvalde yok — PRO'da "+ Tüm ayrıntılar" arkasında.
                AyrintiAc = Dil.BasitMi ? default : EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
            };
        }
    }

    private RenderFragment YildizBantlar => b =>
    {
        b.OpenComponent<HataBandi>(0);
        b.AddAttribute(1, nameof(HataBandi.Mesaj), hata);
        b.AddAttribute(2, nameof(HataBandi.Id), "hata-bandi");
        b.CloseComponent();
        b.OpenComponent<BasariBandi>(3);
        b.AddAttribute(4, nameof(BasariBandi.Mesaj), basari);
        b.AddAttribute(5, nameof(BasariBandi.Jeton), basariJeton);
        b.AddAttribute(6, nameof(BasariBandi.SureliMs), 4000);
        b.CloseComponent();
    };
}
