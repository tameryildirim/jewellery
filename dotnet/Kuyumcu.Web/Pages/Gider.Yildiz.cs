using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5B — Gider ve virman "Yıldız" yüzü (tuval Y_Gidervevirman · BİREBİR · form kalıbı).
// Yalnız GÖRÜNÜM: gider formunun alanları sayfanın MEVCUT bileşenleridir (aynı kimlikler gd-kasa ·
// gd-kategori · gd-tutar · gd-aciklama, aynı FormDurumu ve sayı kapısı, aynı kilit sebebi); "Kaydet"
// MEVCUT GiderKaydet. "Virman" seçimi MEVCUT virman sekmesini (özgün ekran: kaynak/hedef, araç,
// yetersiz bakiye bandı) açar; geçmiş, kategoriler ve storno "Ayrıntı ekle"de.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Gider
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private bool YildizMi => Kuyumcu.Web.Layout.YildizKabuk.Mi(Hizmetler);
    private bool YildizOzgunCiz => !YildizMi || YildizAyrintida;
    /// <summary>Özgün ekran açık mı: "Ayrıntı ekle" ya da gider dışı bir sekme (bağlantıdan ?sekme=virman gelmiş olabilir).</summary>
    private bool YildizAyrintida => yildizAyrinti || sekme != "GIDER";
    private string? YildizOzgunIsaret => YildizMi ? "gider" : null;
    private bool yildizAyrinti;

    private void YildizVazgec()
    {
        giderTutar = null;
        giderAciklama = null;
        giderForm.Temizle();
        sunucuYetersiz = null;
    }

    private void YildizVirman()
    {
        sekme = "VIRMAN";
        yildizAyrinti = true;
    }

    private YildizFormBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            return new YildizFormBaglami
            {
                Kok = "yildiz-gider",
                Ust = YildizGiderSozlugu.Ust(dil),
                Baslik = YildizListeBicim.EkranAdi("/gider", Dil),
                SecimEtiketi = YildizGiderSozlugu.NeYapiyorsun(dil),
                Secimler =
                [
                    ("gider", YildizGiderSozlugu.Gider(dil), true, EventCallback.Factory.Create(this, () => sekme = "GIDER")),
                    ("virman", YildizGiderSozlugu.Virman(dil), false, EventCallback.Factory.Create(this, YildizVirman)),
                ],
                EkleEtiketi = YildizGiderSozlugu.AyrintiEkle(dil),
                EkleTikla = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                B1Etiket = YildizGiderSozlugu.Kaydet(dil), B1Id = "yildiz-gider-kaydet",
                B1Tikla = EventCallback.Factory.Create(this, GiderKaydet),
                B1Kilitli = GiderKilidi is not null || kaydediliyor,
                B1Kilit = GiderKilidi,
                B2Etiket = YildizGiderSozlugu.Vazgec(dil), B2Id = "yildiz-gider-vazgec",
                B2Tikla = EventCallback.Factory.Create(this, YildizVazgec),
                NotBaslik = YildizGiderSozlugu.Not(dil),
                Not = YildizGiderSozlugu.NotMetni(dil),
                Bantlar = YildizBant.Ciz(hata ?? sunucuYetersiz?.Metin, basari, basariJeton, hataId: "gd-hata", basariId: "gd-basari"),
                AyrintiAcik = YildizAyrintida,
                AyrintiKapat = EventCallback.Factory.Create(this, () => { yildizAyrinti = false; sekme = "GIDER"; }),
                DonEtiketi = YildizParaSozlugu.EkranaDon(dil),
            };
        }
    }
}
