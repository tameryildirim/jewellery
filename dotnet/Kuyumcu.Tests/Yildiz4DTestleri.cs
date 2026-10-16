using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using CariListeVeri = Kuyumcu.Contracts.Sayfalar.Cariler.CariVeri;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-4D — 4C-2 kalanları (kurucu 09.10: "tuvalde olmayan öğe ekrana konmaz";
// 375: "ad tek satır, '…' yok, tutar tek satır; alt şerit içeriğin üstüne binmez").
//   • Müşteriler: "+ Süzgeç" tuvalde YOK → çizilmez; özellik silinmez (pasif = arama,
//     tür = bağlantı süzgeci şeridi); "Toptancı ·" öneki segment TOPTAN carisinde.
//   • Tezgâh 375: Fark + Kaydet şeridi akışta yapışkan (sabit 300 px boşluk tahmini yok);
//     kalem adı sütunu genişler, tutar adın altında tek satır; müşteri seçicide tutar
//     kesilmez; ödeme aracı adı kendi <span>'ında (kasa eki ayrı parça).
// Görsel eşitliğin asıl kanıtı başsız Chrome ölçümüdür (rapor § 2); bu sınıf sözleşmeyi kilitler.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class Yildiz4DTestleri : BunitContext
{
    public Yildiz4DTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private static string Css() =>
        Regex.Replace(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tema-yildiz.css"), @"/\*.*?\*/", "", RegexOptions.Singleline);

    /// <summary>İLK eşleşen kural gövdesi (medya sorgusu içindekiler dahil — sıra dosya sırasıdır).</summary>
    private static string Kural(string css, string secici, string? medyaIci = null)
    {
        var bas = 0;
        if (medyaIci is not null)
        {
            // medyaIci = bloğun içindeki BENZERSİZ bir kural parçası; arama o bloğun @media başından başlar.
            var k = css.IndexOf(medyaIci, StringComparison.Ordinal);
            Assert.True(k >= 0, "medya bloğu yok: " + medyaIci);
            bas = css.LastIndexOf("@media", k, StringComparison.Ordinal);
            Assert.True(bas >= 0, "medya başı yok: " + medyaIci);
        }
        var i = css.IndexOf(secici + " {", bas, StringComparison.Ordinal);
        Assert.True(i >= 0, "kural yok: " + secici);
        var a = css.IndexOf('{', i);
        return css[(a + 1)..css.IndexOf('}', a)];
    }

    private static YildizMusteriBaglami MusteriBaglami(List<string> cagri, bool suzgecEtkin = false) => new()
    {
        Ust = "7 müşteri · 2 alacak · 1 borç",
        Satirlar = [new("c6", "Anadolu Döküm Toptan Kuyum", "Toptancı · 0532 000 01 06", "−3.840 ₺", true, "biz borçluyuz")],
        Sekmeler = [new("hepsi", "HEPSİ", true), new("borclu", "BORÇLU", false), new("toptanci", "TOPTANCI", false)],
        SekmeSec = EventCallback.Factory.Create<string>(new object(), k => cagri.Add("sekme:" + k)),
        SuzgecEtkin = suzgecEtkin,
        DigerSuzgecler = b => { b.OpenElement(0, "span"); b.AddAttribute(1, "id", "sahte-suzgec"); b.CloseElement(); },
    };

    [Fact]
    public void Y4D_01_musteriler_suzgec_dugmesi_yok_tuval_kalibi_aynen()
    {
        var e = Render<YildizMusteriler>(p => p.Add(x => x.B, MusteriBaglami([])));
        Assert.Empty(e.FindAll("#cari-suzgec-daha"));
        Assert.DoesNotContain("Süzgeç", e.Markup);
        Assert.DoesNotContain("SÜZGEÇ", e.Markup);
        Assert.Empty(e.FindAll("#yildiz-musteri-suzgec"));
        // tuvalin üç sekmesi + alt notu AYNEN; alt satırda nottan başka öğe yok
        Assert.Equal(["HEPSİ", "BORÇLU", "TOPTANCI"], e.FindAll("#cari-segment > button").Select(x => x.TextContent.Trim()));
        Assert.Equal(["yildiz-musteri-not"], e.FindAll(".yz-musteri-alt > *").Select(x => x.Id));
        // satırdaki her eleman tıklanınca gerçek geri çağrı (sekme)
        var cagri = new List<string>();
        var e2 = Render<YildizMusteriler>(p => p.Add(x => x.B, MusteriBaglami(cagri)));
        e2.Find("#cari-segment-toptanci").Click();
        Assert.Equal(["sekme:toptanci"], cagri);
    }

    [Fact]
    public void Y4D_02_baglanti_suzgeci_etkinse_serit_gorunur_degilse_yok()
    {
        Assert.Single(Render<YildizMusteriler>(p => p.Add(x => x.B, MusteriBaglami([], suzgecEtkin: true))).FindAll("#yildiz-musteri-suzgec #sahte-suzgec"));
        Assert.Empty(Render<YildizMusteriler>(p => p.Add(x => x.B, MusteriBaglami([], suzgecEtkin: false))).FindAll("#sahte-suzgec"));
        // Şerit yalnız BAĞLANTI süzgecinde: tür ya da bağlantıdan pasif → var; aramanın açtığı pasif → yok
        Assert.True(Kuyumcu.Web.Pages.Cariler.YildizSuzgecEtkin("TEDARIKCI", false, false));
        Assert.True(Kuyumcu.Web.Pages.Cariler.YildizSuzgecEtkin("", true, false));
        Assert.False(Kuyumcu.Web.Pages.Cariler.YildizSuzgecEtkin("", true, true));
        Assert.False(Kuyumcu.Web.Pages.Cariler.YildizSuzgecEtkin(null, false, false));
    }

    [Fact]
    public void Y4D_03_pasif_musteri_aramayla_bulunur_arama_silinince_geri_doner()
    {
        // arama yazıldı, pasifler kapalıydı → pasifler istenir (aramanın açtığı)
        Assert.Equal((true, true), Kuyumcu.Web.Pages.Cariler.YildizAramaPasifi("Kemal", false, false));
        // arama sürüyor → değişmez
        Assert.Equal((true, true), Kuyumcu.Web.Pages.Cariler.YildizAramaPasifi("Kema", true, true));
        // arama silindi → yalnız aramanın açtığı pasif görünüm kapanır
        Assert.Equal((false, false), Kuyumcu.Web.Pages.Cariler.YildizAramaPasifi("", true, true));
        Assert.Equal((false, false), Kuyumcu.Web.Pages.Cariler.YildizAramaPasifi("  ", true, true));
        // bağlantıdan gelen aktif=false (aramayla açılmamış) arama silinince KORUNUR
        Assert.Equal((true, false), Kuyumcu.Web.Pages.Cariler.YildizAramaPasifi("", true, false));
        Assert.Equal((true, false), Kuyumcu.Web.Pages.Cariler.YildizAramaPasifi("Ali", true, false));
        Assert.Equal((false, false), Kuyumcu.Web.Pages.Cariler.YildizAramaPasifi("", false, false));
    }

    [Fact]
    public void Y4D_04_toptanci_oneki_segment_toptan_ve_pasif_satirda_yazar()
    {
        var anadolu = new CariListeVeri("c6", "Anadolu Döküm Toptan Kuyum", "TEDARIKCI", "05320000106", null, -3840m, 0m)
            { Segment = Kuyumcu.Core.Domain.CariSegmentleri.Toptan };
        Assert.Equal("Toptancı · 0532 000 01 06", YildizMusteriBicim.Satir(anadolu, null, null, "0532 000 01 06", "tr").Alt);
        var segmentsiz = anadolu with { Segment = null };
        Assert.Equal("0532 000 01 06", YildizMusteriBicim.Satir(segmentsiz, null, null, "0532 000 01 06", "tr").Alt);
        var pasif = anadolu with { Segment = null, Aktif = false };
        Assert.Equal("0532 000 01 06 · kullanımda değil", YildizMusteriBicim.Satir(pasif, null, null, "0532 000 01 06", "tr").Alt);
    }

    [Fact]
    public void Y4D_05_dar_ekranda_alt_serit_akista_yapiskan_sabit_bosluk_tahmini_yok()
    {
        var css = Css();
        const string m = ".yz-tezgah .yz-tezgah-ana { display: flex; flex-direction: column; gap: 28px;";
        var alt = Kural(css, "  html:root[data-sema=\"yildiz\"] .yz-tezgah .yz-tezgah-alt", m);
        Assert.Contains("position: sticky;", alt);
        Assert.Contains("bottom: 0;", alt);
        Assert.DoesNotContain("position: fixed", alt);
        var ana = Kural(css, "  html:root[data-sema=\"yildiz\"] .yz-tezgah .yz-tezgah-ana", m);
        Assert.Contains("padding-bottom: 0;", ana);
        Assert.Contains("display: contents;", Kural(css, "  html:root[data-sema=\"yildiz\"] .yz-tezgah .yz-tezgah-sag", m));
        Assert.DoesNotContain("padding-bottom: 300px", css);
        // PRO'da (.tuval-pro) ana alanın overflow:auto'su yapışkanı hapsetmez — Yıldız Tezgâhı içeren ana alan
        var ana2 = Kural(css, "  html:root[data-sema=\"yildiz\"] .ana-icerik:has(#yildiz-tezgah)", ".ana-icerik:has(#yildiz-tezgah) { overflow-x: clip;");
        Assert.Contains("overflow-y: visible;", ana2);
    }

    [Fact]
    public void Y4D_06_375_kalem_adi_genis_sutunda_tutar_tek_satir()
    {
        var css = Css();
        const string m = ".yz-tezgah .yz-tezgah-kalem { grid-template-columns: 72px minmax(0, 1fr); row-gap: 8px; }";
        var kalem = Kural(css, "  html:root[data-sema=\"yildiz\"] .yz-tezgah .yz-tezgah-kalem", m);
        Assert.Contains("grid-template-columns: 72px minmax(0, 1fr);", kalem);
        var tutar = Kural(css, "  html:root[data-sema=\"yildiz\"] .yz-tezgah .yz-tezgah-kalem > .yz-tezgah-tutar", m);
        Assert.Contains("grid-column: 2;", tutar);
        Assert.Contains("white-space: nowrap;", tutar);
        Assert.Contains("grid-row: span 2;", Kural(css, "  html:root[data-sema=\"yildiz\"] .yz-tezgah .yz-tezgah-kalem > .yz-tezgah-ayar", m));
    }

    [Fact]
    public void Y4D_07_375_musteri_secicide_ad_kirpilmaz_tutar_tek_parca()
    {
        var css = Css();
        const string m = ".yz-tezgah .yz-tezgah-kalem { grid-template-columns: 72px minmax(0, 1fr); row-gap: 8px; }";
        var ad = Kural(css, "  html:root[data-sema=\"yildiz\"] .yz .yz-tezgah-musteri-ad", m);
        Assert.Contains("overflow: visible;", ad);
        Assert.Contains("white-space: normal;", ad);
        Assert.Contains("text-overflow: clip;", ad);
        Assert.Contains("white-space: nowrap;", Kural(css, "  html:root[data-sema=\"yildiz\"] .yz .yz-tezgah-musteri-ad > .yz-s", m));
    }

    [Fact]
    public void Y4D_08_odeme_araci_adi_ayri_parca_metin_aynen()
    {
        var b = new YildizTezgahBaglami
        {
            TurAdi = "Satış", MusteriAdi = "Peşin Müşteri",
            Kalemler = [new(0, "22", "22 Ayar Çeyrek Altın", "1 adet · 1,75 g · 1,6030 g has", "11.099,68 ₺")],
            ToplamMetni = "11.099,68 ₺", ToplamAyrinti = "1,75 g",
            Odemeler = [new(0, "Nakit", "Ana Kasa", "11.099,68 ₺"), new(1, "Havale", "", "0 ₺")],
            FarkMetni = "0 ₺", FarkTamam = true, KaydetEtiketi = "Kaydet ve yazdır", FisDolu = true, KalemVar = true,
        };
        var e = Render<YildizTezgah>(p => p.Add(x => x.B, b));
        Assert.Equal("Nakit", e.Find("#yildiz-odeme-0 .yz-tezgah-odeme-ad > .yz-tezgah-arac").TextContent);
        Assert.Equal("Nakit · Ana Kasa", e.Find("#yildiz-odeme-0 .yz-tezgah-odeme-ad").TextContent);
        // kasasız araçta boşluk ya da "·" kalmaz
        Assert.Equal("Havale", e.Find("#yildiz-odeme-1 .yz-tezgah-odeme-ad").TextContent);
        Assert.Equal(2, e.Find("#yildiz-odeme-0 .yz-tezgah-odeme-ad").Children.Length);
    }

    [Fact]
    public void Y4D_09_dar_ekranda_musteri_satirlari_ortak_sutunlu_ad_tam_genislik_tutar_ayni_kenarda()
    {
        // Hakem KRİTİK (tur 1): 375'te her satır kendi ızgarasını kuruyordu → "Anadolu Döküm Toptan Kuyum" 3 satır,
        // tutarın sağ kenarı satırdan satıra 151/197/202/224 px. Liste ortak sütunlu (subgrid) olmalı.
        var css = Css();
        const string m = ".yz-musteri .yz-musteri-liste { display: grid; grid-template-columns: minmax(0, 1fr) max-content;";
        var k = css.IndexOf(m, StringComparison.Ordinal);
        Assert.True(k >= 0, "dar ekran müşteri listesi kuralı yok");
        var medya = css[css.LastIndexOf("@media", k, StringComparison.Ordinal)..k];
        Assert.StartsWith("@media (max-width: 1000px)", medya);
        const string on = "  html:root[data-sema=\"yildiz\"] .yz-musteri ";
        var liste = Kural(css, on + ".yz-musteri-liste", m);
        Assert.Contains("display: grid;", liste);
        Assert.Contains("grid-template-columns: minmax(0, 1fr) max-content;", liste);
        var sat = Kural(css, on + ".yz-musteri-liste > .yz-musteri-sat", m);
        Assert.Contains("grid-column: 1 / -1;", sat);
        Assert.Contains("grid-template-columns: subgrid;", sat);
        var ad = Kural(css, on + ".yz-musteri-sat > .yz-musteri-ad", m);
        Assert.Contains("grid-column: 1 / -1;", ad);
        Assert.Contains("grid-row: 1;", ad);
        var alt = Kural(css, on + ".yz-musteri-sat > .yz-musteri-alt", m);
        Assert.Contains("grid-column: 1;", alt);
        Assert.Contains("grid-row: 2;", alt);
        var durum = Kural(css, on + ".yz-musteri-sat > .yz-musteri-durum", m);
        Assert.Contains("grid-column: 1;", durum);
        Assert.Contains("grid-row: 3;", durum);
        var tutar = Kural(css, on + ".yz-musteri-sat > .yz-musteri-tutar", m);
        Assert.Contains("grid-column: 2;", tutar);
        Assert.Contains("grid-row: 2;", tutar);
        Assert.Contains("justify-self: end;", tutar);
        Assert.Contains("white-space: nowrap;", tutar);
        // 1440 tuval düzeni değişmez: geniş ekran satır kuralı 4 sütun.
        Assert.Contains("grid-template-columns: minmax(0, 1.3fr) minmax(0, 1fr) 180px 240px;",
            Kural(css, "html:root[data-sema=\"yildiz\"] .yz-musteri .yz-musteri-sat"));
    }

    [Fact]
    public void Y4D_10_musteri_satiri_dort_parca_sirasi_aynen()
    {
        var cagri = new List<string>();
        var e = Render<YildizMusteriler>(p => p.Add(x => x.B, MusteriBaglami(cagri)));
        var sat = e.Find("#yildiz-musteri-c6");
        Assert.Equal(new[] { "yz-musteri-ad", "yz-s yz-musteri-alt", "yz-musteri-tutar yz-kotu", "yz-s yz-musteri-durum" },
            sat.Children.Select(c => c.ClassName).ToArray());
        Assert.Equal("Anadolu Döküm Toptan Kuyum", sat.Children[0].TextContent);
        Assert.Equal("Toptancı · 0532 000 01 06", sat.Children[1].TextContent);
    }
}
