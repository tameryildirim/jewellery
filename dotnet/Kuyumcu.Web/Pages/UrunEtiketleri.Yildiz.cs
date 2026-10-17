using Kuyumcu.Contracts.Sayfalar.UrunEtiketleri;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5A — Etiketler "Yıldız" yüzü (tuval Y_Etiketler · BİREBİR).
// Yalnız GÖRÜNÜM: seçim (Sec), adet (AdetGirildi), yazdırma (Yazdir → window.print) ve vazgeç
// (SecimiTemizle) UrunEtiketleri.razor'un MEVCUT yollarıdır; basılan kâğıt özgün
// .urun-etiket-sayfa'dır (kâğıt muaf). Özgün ekran DOM'da kalır (kâğıt için), ekranda yalnız
// "Tüm ayrıntılar"da görünür (data-yz-etiket). Öteki 8 şemada hiçbir üye çizime girmez.
// ═════════════════════════════════════════════════════════════════════════════
public partial class UrunEtiketleri
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    /// <summary>Özgün kök işareti: Yıldız dışında YAZILMAZ (null); Yıldız'da "kapali" (ekranda gizli) / "acik".</summary>
    private string? YildizEtiketIsaret => (yildizAyrinti ? "acik" : "kapali");

    private bool yildizAyrinti;

    /// <summary>Etiket ürünü (saf): milyem etiketteki ibare ("916"); sunucu milyemi yoksa ayar tablosundan.</summary>
    public static YildizEtiketUrunu YildizUrun(UrunVeri u, string dil) => new(
        u.Id, u.Ad, string.IsNullOrWhiteSpace(u.Barkod) ? null : u.Barkod,
        u.Milyem > 0m ? u.Milyem.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : Urunler.YildizMilyem(u.AyarId),
        YildizListeBicim.Gram(u.GramAgirlik, dil));

    private YildizEtiketBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            return new YildizEtiketBaglami
            {
                Ust = YildizEtiketSozlugu.Ust(dil),
                Baslik = YildizListeBicim.EkranAdi("/urunler/etiketler", Dil),
                Urunler = [.. UrunListesi.Select(u => YildizUrun(u, dil))],
                Secili = [.. UrunListesi.Where(u => secili.Contains(u.Id)).Select(u => YildizUrun(u, dil))],
                Ekle = EventCallback.Factory.Create<string>(this, id => Sec(id, true)),
                Cikar = EventCallback.Factory.Create<string>(this, id => Sec(id, false)),
                Kopya = kopya,
                KopyaEnCok = Kuyumcu.Core.Domain.Girdi.AlanSinirlari.EtiketKopyaEnCok,
                KopyaGirildi = EventCallback.Factory.Create<decimal?>(this, AdetGirildi),
                KopyaHata = form.Mesaj("kopya"),
                Yazdir = EventCallback.Factory.Create(this, Yazdir),
                YazdirKilit = Yazdirilabilir ? null : YazdirKilitSebebi,
                Vazgec = EventCallback.Factory.Create(this, SecimiTemizle),
                Yukleniyor = yukleniyor,
                Bantlar = YildizBantlar,
                AyrintiAcik = yildizAyrinti,
                AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
            };
        }
    }

    /// <summary>Kayıp sessiz olmaz (P-66): hata, yazdırma sonucu, taslak geri yükleme/eksik/temizleme (+ geri al).</summary>
    private RenderFragment YildizBantlar => b =>
    {
        b.OpenComponent<HataBandi>(0);
        b.AddAttribute(1, nameof(HataBandi.Mesaj), hata);
        b.AddAttribute(2, nameof(HataBandi.Id), "yildiz-etiket-hata");
        b.CloseComponent();
        if (yazdirmaBasari is not null)
        {
            b.OpenComponent<BasariBandi>(3);
            b.AddAttribute(4, nameof(BasariBandi.Mesaj), yazdirmaBasari);
            b.AddAttribute(5, nameof(BasariBandi.Id), "yildiz-etiket-basari");
            b.CloseComponent();
        }
        if (geriYuklendi)
        {
            b.OpenElement(6, "p");
            b.AddAttribute(7, "class", "yz-s yz-not");
            b.AddAttribute(8, "id", "yildiz-etiket-geri-yuklendi");
            b.AddAttribute(9, "role", "status");
            b.AddContent(10, string.Format(T.GeriYuklendi, secili.Count, kopya));
            b.CloseElement();
        }
        if (eksikKalem > 0)
        {
            b.OpenElement(11, "p");
            b.AddAttribute(12, "class", "yz-kotu yz-not");
            b.AddAttribute(13, "id", "yildiz-etiket-eksik");
            b.AddAttribute(14, "role", "status");
            b.AddContent(15, string.Format(T.EksikKalem, eksikKalem));
            b.CloseElement();
        }
        if (temizlenenTaslak is not null)
        {
            b.OpenComponent<BasariBandi>(16);
            b.AddAttribute(17, nameof(BasariBandi.Mesaj), T.Temizlendi);
            b.AddAttribute(18, nameof(BasariBandi.Id), "yildiz-etiket-temizlendi");
            b.CloseComponent();
            b.OpenElement(19, "button");
            b.AddAttribute(20, "type", "button");
            b.AddAttribute(21, "class", "yz-b2");
            b.AddAttribute(22, "id", "yildiz-etiket-geri-al");
            b.AddAttribute(23, "onclick", EventCallback.Factory.Create(this, TemizlemeyiGeriAl));
            b.AddContent(24, T.GeriAl);
            b.CloseElement();
        }
    };
}
