using Bunit;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ISLEM-BANT-2 (10.10) — sayfa düzeyi (Islemler): SATIS3EkranTestleri düzeneği (sahte API, kur 6.800 →
// alış bandı 3.400,00–7.480,00). Kaydet kilidi Gonderilebilir ile KaydetKilitSebebi'nden AYNI değeri okur
// (Islemler.IslemBant2.cs); kilitliyken tıklama POST üretmez.
// ═══════════════════════════════════════════════════════════════════════════
public sealed partial class SATIS3EkranTestleri
{
    private const string IB2Gerekce = "Bozuk hurda, ayarı düşük çıktı; ustayla teyit ettim.";

    /// <summary>Hurda alışı EKSİKSİZ doldurur (cari, gram, satıcı, adres, havale) — kalan tek kilit bantsa o görünür.</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> IB2HurdaDolu(string mod, string rol)
    {
        var e = Ekran(mod, rol);
        e.Find("#islem-tur-HURDA_ALIS").Click();
        e.WaitForElement("#hurda-satici");
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.Find("#hurda-gram").Input("10");
        SeciciYardim.Sec(e, "hurda-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#hurda-satici-ad");
        e.Find("#hurda-satici-ad").Input("Ali Veli");
        e.Find("#hurda-satici-tckn").Input("10000000146");
        e.Find("#hurda-satici-adres").Input("Kapalıçarşı No:1");
        if (e.FindAll("#islem-odeme").Count > 0) SeciciYardim.Sec(e, "islem-odeme", "HAVALE");
        return e;
    }

    private int HurdaPostSayisi() { lock (istekler) return istekler.Count(i => i.Metot == "POST" && i.Yol.Contains("/islemler/hurda")); }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void IB2_30_TEZGAHTAR_hurda_alis_formun_basinda_yetki_Kaydet_kilitli_ayni_cumle_POST_yok(string mod)
    {
        var e = IB2HurdaDolu(mod, "TEZGAHTAR");
        var metin = IslemBant2Sozlugu.HurdaAlisYetkiMetni("tr");
        Assert.Equal(metin, e.Find("#hurda-yetki-yok").TextContent.Trim());
        e.WaitForAssertion(() => Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled")));
        Assert.Equal(metin, e.Find("#islem-kaydet").GetAttribute("title"));
        e.Find("#islem-kaydet").Click();
        Assert.Equal(0, HurdaPostSayisi());   // canlıda 403 hiç tetiklenmez
    }

    [Fact]
    public void IB2_31_MUHASEBE_hurda_alis_yetki_cumlesi_yok_Kaydet_acik_POST_gider()
    {
        var e = IB2HurdaDolu(ArayuzModuServisi.Profesyonel, "MUHASEBE");
        Assert.Empty(e.FindAll("#hurda-yetki-yok"));
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled"), e.Find("#islem-kaydet").GetAttribute("title")));
        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(1, HurdaPostSayisi()));
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void IB2_32_Hurda_bant_disi_Kaydet_kilitli_onay_ve_gerekceyle_acilir(string mod)
    {
        var e = IB2HurdaDolu(mod, "MUDUR");
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled"), e.Find("#islem-kaydet").GetAttribute("title")));
        e.Find("#hurda-fiyat").Input("2000");   // oran dışı (3.400–7.480)
        e.WaitForAssertion(() => Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled")));
        Assert.Equal(string.Format(IslemBant2Sozlugu.Tr.BantOnayBekliyor, KurBantSozlugu.Tr.OnayEtiket), e.Find("#islem-kaydet").GetAttribute("title"));
        e.Find("#islem-kaydet").Click();
        Assert.Equal(0, HurdaPostSayisi());
        e.Find("#hurda-bant-onay").Change(true);
        e.Find("#hurda-bant-gerekce").Input("kısa");
        e.WaitForAssertion(() => Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled")));
        e.Find("#hurda-bant-gerekce").Input(IB2Gerekce);
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled"), e.Find("#islem-kaydet").GetAttribute("title")));
        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(1, HurdaPostSayisi()));
        Assert.Contains("\"bantOnay\":true", SonPost("/islemler/hurda").Govde);
    }

    [Theory]
    [InlineData("-5")]
    [InlineData("0")]
    [InlineData("5")]
    [InlineData("99999999")]
    [InlineData("abc")]
    public void IB2_33_Hurda_hasim_fiyatlari_alan_alti_ileti_Kaydet_kilitli_eksi_sifirda_tutar_yok(string metin)
    {
        var e = IB2HurdaDolu(ArayuzModuServisi.Profesyonel, "SAHIP");
        e.Find("#hurda-fiyat").Input(metin);
        e.WaitForAssertion(() => Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled")));
        Assert.Equal("true", e.Find("#hurda-fiyat").GetAttribute("aria-invalid"));
        Assert.Equal("—", e.Find("#hurda-hesap-tutar").TextContent.Trim());   // eksi/0/harf/mutlak dışı: kutu altında tutar YOK
        e.Find("#islem-kaydet").Click();
        Assert.Equal(0, HurdaPostSayisi());
    }

    [Fact]
    public void IB2_34_Takas_TEZGAHTAR_bant_disi_Kaydet_kilitli_yetki_cumlesiyle_eksi_fiyat_alan_alti()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel, "TEZGAHTAR");
        e.Find("#islem-tur-TAKAS").Click();
        e.WaitForElement("#takas-eski-fiyat");
        e.Find("#takas-eski-gram").Input("5");
        e.Find("#takas-eski-fiyat").Input("3000");
        e.WaitForAssertion(() => Assert.Equal(KurBantSozlugu.Tr.YetkiYok, e.Find("#islem-kaydet").GetAttribute("title")));
        Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled"));
        e.Find("#takas-eski-fiyat").Input("-5");
        e.WaitForAssertion(() => Assert.Contains(GirdiSozlugu.Mesaj(TutarKurali.Denetle(-5m, TutarTuru.Pozitif)!.Value, "tr"), e.Markup));
        Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled"));
    }
}
