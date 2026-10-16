using System.Net;
using Bunit;
using Kuyumcu.Contracts.Sayfalar.Cariler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;
using SayfaCariler = Kuyumcu.Web.Pages.Cariler;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-M1 — MÜŞTERİLER TUVALE (tuval/project/Musteriler.dc.html)
//
// Kapsam: BASİT yüz — arama "Ad ya da telefon", Hepsi/Borçlu/Toptancı segmenti,
// sade tablo (MÜŞTERİ + "Vade geçti"/"Toptancı" rozeti · TELEFON · BORÇ),
// "N müşteri · M gösteriliyor", satıra dokununca SAĞDAN panel (uyarı bandı,
// Ad soyad*, Telefon, Not, "Daha fazla (adres, TC, e-posta, limit)", "Son
// değişiklik … · Kim ne yaptı"). PRO yüz DEĞİŞMEZ.
// Her kural İSTEK ve DOM düzeyinde sınanır; para metni SABİT yazılmaz (Bicim.Tl).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TM1MusterilerTuvalTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly Dictionary<string, (HttpStatusCode Kod, string Govde)> ozel = [];
    // Yarış testleri: yolun SONU bu anahtarla bitiyorsa yanıt, kaynak tamamlanana dek BEKLETİLİR.
    private readonly Dictionary<string, TaskCompletionSource> bekle = [];

    // c1: bize borçlu, vadesi 12 gün geçmiş · c2: toptancı, biz borçluyuz, gecikme 0
    // c3: sıfır bakiye, yalnız has borcu · c4: sıfır.
    private string cariListesi = """
        [{"id":"c1","ad":"Hasan Kaya","tur":"MUSTERI","telefon":"05324112233","email":null,
          "tlBakiye":38900,"hasGramBakiye":0,"aktif":true,"segment":null,
          "guncellendi":"2026-09-23T13:20:00"},
         {"id":"c2","ad":"Mehmet Öz","tur":"TEDARIKCI","telefon":"02125114040","email":null,
          "tlBakiye":-151300,"hasGramBakiye":-22,"aktif":true,"segment":"TOPTAN",
          "guncellendi":"2026-09-20T08:00:00"},
         {"id":"c3","ad":"Zeynep Demir","tur":"MUSTERI","telefon":"5412089017","email":null,
          "tlBakiye":0,"hasGramBakiye":1.5,"aktif":true,"segment":null,
          "guncellendi":"2026-09-20T08:00:00"},
         {"id":"c4","ad":"Ali Can","tur":"MUSTERI","telefon":null,"email":null,
          "tlBakiye":0,"hasGramBakiye":0,"aktif":true,"segment":"BIREYSEL",
          "guncellendi":null}]
        """;

    private const string VadeYaniti = """
        {"ufukGun":30,"gecikenToplam":38900,"yaklasanToplam":0,"vadesizToplam":0,
         "ufukOtesiToplam":0,"acikToplam":38900,
         "cariler":[{"cariId":"c1","cariAd":"Hasan Kaya","cariTur":"MUSTERI","telefon":null,
                     "gecikenTutar":38900,"yaklasanTutar":0,"vadesizTutar":0,
                     "enEskiGecikmeGun":12,"enYakinVade":null,"borclar":[]},
                    {"cariId":"c2","cariAd":"Mehmet Öz","cariTur":"TEDARIKCI","telefon":null,
                     "gecikenTutar":0,"yaklasanTutar":100,"vadesizTutar":0,
                     "enEskiGecikmeGun":0,"enYakinVade":null,"borclar":[]}]}
        """;

    // En YENİ kayıt bir OKUMA'dır → "son değişiklik" o olamaz; yazma olan GUNCELLEME seçilir.
    private const string DenetimYaniti = """
        {"toplam":2,"limit":20,"offset":0,"kayitlar":[
          {"id":"d1","tarih":"2026-09-24T10:00:00Z","eylem":"OKUMA","varlikTuru":"Cari","varlikId":"c1",
           "hassas":false,"detay":null,"ipAdresi":null,"kullaniciId":"u9","kullanici":"Okuyan Kişi","rol":"SAHIP"},
          {"id":"d2","tarih":"2026-09-23T13:20:00Z","eylem":"GUNCELLEME","varlikTuru":"Cari","varlikId":"c1",
           "hassas":false,"detay":null,"ipAdresi":null,"kullaniciId":"u2","kullanici":"Ayşe Yılmaz","rol":"TEZGAHTAR"}]}
        """;

    public TM1MusterilerTuvalTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => cariListesi, ozel, bekle))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<string> liste,
        Dictionary<string, (HttpStatusCode Kod, string Govde)> ozel,
        Dictionary<string, TaskCompletionSource> bekle) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, istek.RequestUri!.PathAndQuery, govde));
            foreach (var (anahtar, kaynak) in bekle.ToList())
                if (yol.EndsWith(anahtar)) await kaynak.Task;
            foreach (var (anahtar, yanit) in ozel)
                if (yol.EndsWith(anahtar))
                    return new HttpResponseMessage(yanit.Kod)
                    { Content = new StringContent(yanit.Govde, System.Text.Encoding.UTF8, "application/json") };

            var g = yol switch
            {
                var y when y.EndsWith("/kur/guncel") => """{"id":"kur-1","usdTry":40,"eurTry":44,"gbpTry":null}""",
                var y when y.EndsWith("/raporlar/vade-takibi") => VadeYaniti,
                var y when y.EndsWith("/denetim") => DenetimYaniti,
                var y when y.Contains("/cari/c") && !y.Contains("/ekstre") && !y.Contains("/bagli") => """
                    {"id":"c1","ad":"Hasan Kaya","tur":"MUSTERI","telefon":"05324112233","email":"h@ornek.com",
                     "vergiNo":"12345678950","adres":"Kapalıçarşı","krediLimitiTl":5000,"varsayilanVadeGun":30,
                     "notlar":"Eşinin doğum günü ekim","pepBeyani":false,"pepGorev":null,"pepBeyanTarihi":null}
                    """,
                var y when y.EndsWith("/cari") => liste(),
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var y when y.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","aktif":true}]""",
                var y when y.Contains("/bagli-kayitlar") => """{"cariId":"c1","aktif":true,"hareketSayisi":0,"tlBakiye":0,"hasGramBakiye":0,"dovizBakiyeVar":false}""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<SayfaCariler> Ciz(string mod = ArayuzModuServisi.Basit, string rol = "SAHIP")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1", mod, mod));
        var e = Render<SayfaCariler>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("tr.cari-satir")));
        return e;
    }

    private static List<string> SatirIdleri(IRenderedComponent<SayfaCariler> e) =>
        [.. e.FindAll("tr.cari-satir").Select(t => t.Id!.Replace("c-satir-", ""))];

    // ── (1) SEGMENT: Hepsi / Borçlu / Toptancı + sayaç ─────────────────────────

    [Fact]
    public void TM1_01_Segment_Borclu_ve_Toptanci_suzer_sayac_N_ve_M_yazar()
    {
        var e = Ciz();
        Assert.Equal("true", e.Find("#cari-segment-hepsi").GetAttribute("aria-pressed"));
        Assert.Equal(4, SatirIdleri(e).Count);
        Assert.Equal(string.Format(MusteriTuvalSozlugu.Tr.Sayac, 4, 4), e.Find("#cari-sayac-metin").TextContent.Trim());

        e.Find("#cari-segment-borclu").Click();
        Assert.Equal(["c1", "c3"], SatirIdleri(e));      // TL ya da has ile bize borçlu
        Assert.Equal("true", e.Find("#cari-segment-borclu").GetAttribute("aria-pressed"));
        Assert.Equal("false", e.Find("#cari-segment-hepsi").GetAttribute("aria-pressed"));
        Assert.Equal(string.Format(MusteriTuvalSozlugu.Tr.Sayac, 4, 2), e.Find("#cari-sayac-metin").TextContent.Trim());

        e.Find("#cari-segment-toptanci").Click();
        Assert.Equal(["c2"], SatirIdleri(e));
        Assert.Equal(string.Format(MusteriTuvalSozlugu.Tr.Sayac, 4, 1), e.Find("#cari-sayac-metin").TextContent.Trim());

        // Segment İSTEMCİDE çalışır: sunucuya ek liste isteği atılmaz.
        Assert.Single(istekler, i => i.Metot == "GET" && i.Yol.Contains("/cari?"));
    }

    [Fact]
    public void TM1_01b_Segmentte_kimse_yoksa_bos_durum_suzgec_adina_konusur()
    {
        cariListesi = """[{"id":"c4","ad":"Ali Can","tur":"MUSTERI","telefon":null,"email":null,"tlBakiye":0,"hasGramBakiye":0,"aktif":true}]""";
        var e = Ciz();
        e.Find("#cari-segment-toptanci").Click();
        Assert.Empty(e.FindAll("tr.cari-satir"));
        Assert.Contains(MusteriTuvalSozlugu.Tr.SegmentBos, e.Markup);
        Assert.Equal(string.Format(MusteriTuvalSozlugu.Tr.Sayac, 1, 0), e.Find("#cari-sayac-metin").TextContent.Trim());
    }

    // ── (2) SADE TABLO: sütunlar, rozetler, telefon biçimi, Borç = Bicim.Tl ────

    [Fact]
    public void TM1_02_Basit_tablo_Musteri_Telefon_Borc_rozetler_ve_telefon_bicimi()
    {
        var e = Ciz();
        var basliklar = e.FindAll("table.musteri-tablo thead th").Select(t => t.TextContent.Trim()).ToList();
        Assert.Equal(MusteriTuvalSozlugu.Tr.SutunMusteri, basliklar[0]);
        Assert.Equal(MusteriTuvalSozlugu.Tr.SutunTelefon, basliklar[1]);
        // KESIF-P2 tur 3 (hakem): "Borç" başlığı altında eksi tutar ↔ detayda "Alacak" çelişkisi kalktı —
        // başlık "Bakiye", yön her satırda tek bakış açısıyla ("Bize borçlu" / "Biz borçluyuz").
        Assert.Equal(Kuyumcu.Core.I18n.KesifP2Sozlugu.Tr.SutunBakiye, basliklar[2]);

        Assert.Equal(MusteriTuvalSozlugu.Tr.VadeGecti, e.Find("#c-vade-c1").TextContent.Trim());
        Assert.Empty(e.FindAll("#c-vade-c2"));             // gecikme 0 → rozet YOK
        Assert.Equal(MusteriTuvalSozlugu.Tr.Toptanci, e.Find("#c-toptan-c2").TextContent.Trim());
        Assert.Empty(e.FindAll("#c-toptan-c4"));           // BIREYSEL

        Assert.Equal("0532 411 22 33", e.Find("#c-telefon-c1").TextContent.Trim());
        Assert.Equal("0212 511 40 40", e.Find("#c-telefon-c2").TextContent.Trim());
        Assert.Equal("0541 208 90 17", e.Find("#c-telefon-c3").TextContent.Trim());
        Assert.Equal("—", e.Find("#c-telefon-c4").TextContent.Trim());

        var borc = e.Find("#c-satir-c2 td.musteri-sutun-borc");
        Assert.Contains(Bicim.Tl(-151300m), borc.TextContent);
        Assert.Contains("metin-kirmizi", borc.ClassName);
        // KESIF-P2 tur 3: BASİT ve PRO aynı yön kelimesi; muhasebe "Alacak/Borç" rozeti yok.
        Assert.DoesNotContain(Dil(e).Cari.Borc, borc.TextContent);
        Assert.Contains(Kuyumcu.Core.I18n.KesifP2Sozlugu.Tr.YonBizBorcluyuz, borc.TextContent);
        // Satır düğmeleri tuvaldeki gibi YALNIZ yazı: emoji yok.
        var eylem = e.Find("#c-eylem-c1").TextContent;
        Assert.DoesNotContain("💵", eylem);
        Assert.DoesNotContain("✏", eylem);
        // Arama kutusu: tuval yer tutucusu ve büyüteç ikonu.
        Assert.Equal(MusteriTuvalSozlugu.Tr.AramaYerTutucu, e.Find("#cari-arama").GetAttribute("placeholder"));
        Assert.NotNull(e.Find("#cari-arama-kutu svg.ikon"));
    }

    private Kuyumcu.Core.I18n.Sozluk Dil(IRenderedComponent<SayfaCariler> _) =>
        Services.GetRequiredService<DilServisi>().T;

    [Fact]
    public void TM1_03_Vade_ucu_403_ise_rozet_cizilmez_hata_bandi_cikmaz()
    {
        ozel["/raporlar/vade-takibi"] = (HttpStatusCode.Forbidden, """{"message":"Yetkiniz yok"}""");
        var e = Ciz(rol: "TEZGAHTAR");
        Assert.Empty(e.FindAll(".cari-rozet--vade"));
        Assert.DoesNotContain("Yetkiniz yok", e.Markup);
        Assert.Equal(4, SatirIdleri(e).Count);   // liste YERİNDE
    }

    [Fact]
    public void TM1_03b_Vade_ucu_500_ise_hata_bandi_GORUNUR_liste_kalir()
    {
        ozel["/raporlar/vade-takibi"] = (HttpStatusCode.InternalServerError, """{"message":"Vade raporu okunamadı"}""");
        var e = Ciz();
        e.WaitForAssertion(() => Assert.Contains("Vade raporu okunamadı", e.Find("#hata-bandi").TextContent));
        Assert.Equal(4, SatirIdleri(e).Count);
    }

    // ── (3) SATIRA DOKUN → SAĞ PANEL ─────────────────────────────────────────

    [Fact]
    public void TM1_04_Satira_dokununca_panel_acilir_basligi_ad_satir_secili()
    {
        var e = Ciz();
        e.Find("#c-satir-c1 td").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#cekmece-cari")));
        Assert.Equal("Hasan Kaya", e.Find("#cekmece-cari-baslik").TextContent.Trim());
        Assert.Contains("musteri-satir--secili", e.Find("#c-satir-c1").ClassName);
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/cari/c1"));
        Assert.NotNull(e.Find(".musteri-sade--panel-acik"));
    }

    [Fact]
    public void TM1_04b_PRO_modda_satir_hucresi_panel_acmaz_Duzenle_dugmesi_acar()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        e.Find("#c-satir-c1 td").Click();
        Assert.Empty(e.FindAll("#cekmece-cari"));
        Assert.Empty(e.FindAll(".musteri-sade"));
        e.Find("#c-duzenle-c1").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#cekmece-cari")));
        Assert.Equal(Dil(e).Cari.CariDuzenle, e.Find("#cekmece-cari-baslik").TextContent.Trim());
    }

    [Fact]
    public void TM1_05_Panel_uyari_bandi_gecikme_gununu_yazar_Tahsilat_penceresini_acar()
    {
        var e = Ciz();
        e.Find("#c-satir-c1 td").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#cari-panel-uyari")));
        Assert.Contains(string.Format(MusteriTuvalSozlugu.Tr.VadeUyariOnce, 12), e.Find("#cari-panel-uyari").TextContent);
        // Düğme metni YALNIZ "Tahsilat" (tırnak düğmenin dışında; doğrulayıcı bulgusu 24.09).
        Assert.Equal(MusteriTuvalSozlugu.Tr.TahsilatDugmesi, e.Find("#cari-panel-tahsilat").TextContent);
        Assert.Contains(string.Format(MusteriTuvalSozlugu.Tr.VadeUyariOnce, 12) + MusteriTuvalSozlugu.Tr.TahsilatDugmesi + MusteriTuvalSozlugu.Tr.VadeUyariSonra,
            e.Find("#cari-panel-uyari").TextContent);

        e.Find("#cari-panel-tahsilat").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#th-kaydet")));
        Assert.Empty(e.FindAll("#cekmece-cari"));          // panel kapandı, pencere açıldı
        Assert.Contains(istekler, i => i.Yol.EndsWith("/kasalar"));

        // Gecikmesi olmayan kartta bant YOK.
        e.Find("#c-satir-c2 td").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#cekmece-cari")));
        Assert.Empty(e.FindAll("#cari-panel-uyari"));
    }

    [Fact]
    public void TM1_06_Panel_alanlari_tuval_etiketleri_kaldir_dugmesi_Daha_fazla_icinde()
    {
        var e = Ciz();
        e.Find("#c-satir-c1 td").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#duz-ad")));
        Assert.Equal(MusteriTuvalSozlugu.Tr.AdSoyad, e.Find("#duz-ad-sar .alan-etiket-metin").TextContent.Trim());
        Assert.Equal(MusteriTuvalSozlugu.Tr.Not, e.Find("#duz-notlar-sar .alan-etiket-metin").TextContent.Trim());
        Assert.Contains(MusteriListeSozlugu.Tr.DahaFazlaIcerik, e.Find("#duz-daha-fazla").TextContent);
        Assert.Empty(e.FindAll("#duz-telefon-ipucu"));   // biçim ipucu tuvalde yok
        Assert.Empty(e.FindAll("#c-pasife-al"));         // katlı

        e.Find("#duz-daha-fazla").Click();
        Assert.Equal(MusteriTuvalSozlugu.Tr.KullanimdanKaldir, e.Find("#c-pasife-al").TextContent.Trim());
        e.Find("#c-pasife-al").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#c-pasif-onayla")));
        Assert.Equal(MusteriTuvalSozlugu.Tr.KullanimdanKaldir, e.Find("#c-pasif-onayla").TextContent.Trim());
    }

    [Fact]
    public void TM1_06b_Panel_Kaydet_yolu_DEGISMEDI_PATCH_katli_alanlari_aynen_tasir()
    {
        var e = Ciz();
        e.Find("#c-satir-c1 td").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#duz-ad")));
        istekler.Clear();
        e.Find("#cekmece-cari-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/cari/c1")));
        var govde = System.Text.Json.JsonDocument.Parse(istekler.Single(i => i.Metot == "PATCH").Govde).RootElement;
        Assert.Equal("12345678950", govde.GetProperty("vergiNo").GetString());
        Assert.Equal("Eşinin doğum günü ekim", govde.GetProperty("notlar").GetString());
        Assert.Equal(5000m, govde.GetProperty("krediLimitiTl").GetDecimal());
    }

    // ── (4) SON DEĞİŞİKLİK · KİM NE YAPTI ─────────────────────────────────────

    [Fact]
    public void TM1_07_Sahipte_son_degisiklik_YAZMA_kaydindan_kim_ve_baglanti()
    {
        var e = Ciz(rol: "SAHIP");
        e.Find("#c-satir-c1 td").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#cari-son-degisiklik")));
        var d = istekler.Single(i => i.Yol.Contains("/denetim?"));
        Assert.Contains("varlikTuru=Cari", d.Yol);
        Assert.Contains("varlikId=c1", d.Yol);
        var metin = e.Find("#cari-son-degisiklik-metin").TextContent;
        Assert.Contains("Ayşe Y.", metin);
        Assert.DoesNotContain("Okuyan", metin);          // OKUMA "değişiklik" değildir
        Assert.Equal("/denetim-izi", e.Find("#cari-kim-ne-yapti").GetAttribute("href"));
    }

    [Fact]
    public void TM1_07b_Sahip_disinda_denetim_istenmez_ad_uydurulmaz_yalniz_zaman()
    {
        var e = Ciz(rol: "TEZGAHTAR");
        e.Find("#c-satir-c1 td").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#cari-son-degisiklik")));
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/denetim"));
        Assert.Empty(e.FindAll("#cari-kim-ne-yapti"));
        var beklenen = string.Format(MusteriTuvalSozlugu.Tr.SonDegisiklik,
            SayfaCariler.ZamanMetni(SayfaCariler.Yerel(new DateTime(2026, 9, 23, 13, 20, 0)), DateTime.Now, MusteriTuvalSozlugu.Tr, "tr"));
        Assert.Equal(beklenen, e.Find("#cari-son-degisiklik-metin").TextContent.Trim());
    }

    [Fact]
    public void TM1_07c_Denetim_ucu_patlarsa_sebep_yazilir_zaman_kalir()
    {
        ozel["/denetim"] = (HttpStatusCode.InternalServerError, """{"message":"Denetim okunamadı"}""");
        var e = Ciz(rol: "SAHIP");
        e.Find("#c-satir-c1 td").Click();
        e.WaitForAssertion(() => Assert.Contains("Denetim okunamadı", e.Find("#cari-son-degisiklik-hata").TextContent));
        Assert.NotEmpty(e.Find("#cari-son-degisiklik-metin").TextContent.Trim());
    }

    // ── (5) ÖZELLİK SİLİNMEZ: tür/pasif süzgeci, PRO yüz, sınır ────────────────

    [Fact]
    public void TM1_08_Diger_suzgecler_tur_ve_pasif_suzgecini_BASITte_de_acar()
    {
        var e = Ciz();
        Assert.Empty(e.FindAll("#cari-tur-filtre"));
        Assert.Equal("false", e.Find("#cari-suzgec-daha").GetAttribute("aria-expanded"));
        e.Find("#cari-suzgec-daha").Click();
        Assert.NotEmpty(e.FindAll("#cari-tur-filtre"));
        Assert.NotEmpty(e.FindAll("#cari-pasifleri-goster"));
        Assert.Equal("true", e.Find("#cari-suzgec-daha").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void TM1_09_PRO_yuz_bugunku_gibi_satir_ici_telefon_rozet_ve_suzgecler()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        Assert.Empty(e.FindAll("#cari-segment"));
        Assert.Empty(e.FindAll("#cari-sayac"));
        Assert.NotEmpty(e.FindAll("#cari-tur-filtre"));
        // TOPLU-5 (hakem ORTA "12 px 'En çok 100 kayıt' notu"): PRO da sınır cümlesini yalnız sınıra değince yazar (TM1_10).
        Assert.Empty(e.FindAll("#cari-sinir"));
        Assert.Empty(e.FindAll("#c-telefon-c1"));
        // KESIF-P2 tur 3: PRO rozeti de BASİT/detay ile aynı kelime.
        Assert.Contains(Kuyumcu.Core.I18n.KesifP2Sozlugu.Tr.YonBizBorcluyuz, e.Find("#c-satir-c2 td.musteri-sutun-borc").TextContent);
        Assert.Equal(4, SatirIdleri(e).Count);            // segment PRO'da uygulanmaz
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/raporlar/vade-takibi"));
    }

    [Fact]
    public void TM1_10_Sinira_degince_sayac_100_arti_ve_sinir_uyarisi_gorunur()
    {
        var satirlar = Enumerable.Range(1, 100).Select(i =>
            $$"""{"id":"s{{i}}","ad":"Müşteri {{i}}","tur":"MUSTERI","telefon":null,"email":null,"tlBakiye":0,"hasGramBakiye":0,"aktif":true}""");
        cariListesi = "[" + string.Join(",", satirlar) + "]";
        var e = Ciz();
        Assert.Equal(string.Format(MusteriTuvalSozlugu.Tr.Sayac, "100+", 100), e.Find("#cari-sayac-metin").TextContent.Trim());
        Assert.NotEmpty(e.FindAll("#cari-sinir-asildi"));
        Assert.NotEmpty(e.FindAll("#cari-sinir"));
    }

    [Fact]
    public void TM1_10b_Sinirin_altinda_BASITte_sinir_cumlesi_yok_sayi_gercek()
    {
        var e = Ciz();
        Assert.Empty(e.FindAll("#cari-sinir"));
        Assert.Empty(e.FindAll("#cari-sinir-asildi"));
    }

    // ── (6) SAF: telefon biçimi, kısa ad, zaman metni, segment süzgeci ───────

    [Theory]
    [InlineData("05324112233", "0532 411 22 33")]
    [InlineData("0532 411 22 33", "0532 411 22 33")]
    [InlineData("(0532) 411-22-33", "0532 411 22 33")]
    [InlineData("5324112233", "0532 411 22 33")]   // CUMA-M: tuval biçimi 0'lı
    [InlineData("+905324112233", "0532 411 22 33")]  // CUMA-M: +90 → tuval biçimi
    [InlineData("12345", "12345")]
    [InlineData(null, "—")]
    [InlineData("  ", "—")]
    public void TM1_11_Telefon_gorunumu(string? girdi, string beklenen) =>
        Assert.Equal(beklenen, SayfaCariler.TelefonGoster(girdi));

    [Theory]
    [InlineData("Ayşe Yılmaz", "Ayşe Y.")]
    [InlineData("Ali Rıza Çelik", "Ali Rıza Ç.")]
    [InlineData("ayşe irmak", "ayşe İ.")]
    [InlineData("sistem", "sistem")]
    public void TM1_12_Kisa_ad(string ad, string beklenen) =>
        Assert.Equal(beklenen, SayfaCariler.KisaAd(ad));

    [Fact]
    public void TM1_13_Zaman_metni_bugun_dun_ve_tarih()
    {
        var simdi = new DateTime(2026, 9, 24, 21, 0, 0);
        var t = MusteriTuvalSozlugu.Tr;
        Assert.Equal(string.Format(t.Bugun, "09:05"), SayfaCariler.ZamanMetni(new DateTime(2026, 9, 24, 9, 5, 0), simdi, t, "tr"));
        Assert.Equal(string.Format(t.Dun, "16:20"), SayfaCariler.ZamanMetni(new DateTime(2026, 9, 23, 16, 20, 0), simdi, t, "tr"));
        Assert.Equal("20.09.2026 08:00", SayfaCariler.ZamanMetni(new DateTime(2026, 9, 20, 8, 0, 0), simdi, t, "tr"));
        // TOPLU-1 tur 2 (TARIH-TEK): tek kalıp dd.MM.yyyy dilden bağımsız (eskiden en-GB "20/09/2026").
        Assert.Equal("20.09.2026 08:00", SayfaCariler.ZamanMetni(new DateTime(2026, 9, 20, 8, 0, 0), simdi, MusteriTuvalSozlugu.En, "en"));
        // Sunucu UTC yazar: Kind belirsiz değer UTC sayılır.
        var utc = new DateTime(2026, 9, 23, 13, 20, 0, DateTimeKind.Unspecified);
        Assert.Equal(DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime(), SayfaCariler.Yerel(utc));
    }

    [Fact]
    public void TM1_14_Segment_suzgeci_saf()
    {
        CariVeri C(string id, decimal tl, decimal has, string? seg = null) => new(id, id, "MUSTERI", null, null, tl, has, Segment: seg);
        var l = new List<CariVeri> { C("a", 10, 0), C("b", -5, 0), C("c", 0, 0.5m), C("d", 0, -1, "TOPTAN"), C("e", 0, 0, "VIP") };
        Assert.Equal(["a", "c"], SayfaCariler.SegmentSuz(l, SayfaCariler.SegBorclu).Select(c => c.Id));
        Assert.Equal(["d"], SayfaCariler.SegmentSuz(l, SayfaCariler.SegToptanci).Select(c => c.Id));
        Assert.Equal(5, SayfaCariler.SegmentSuz(l, SayfaCariler.SegHepsi).Count);
    }

    [Fact]
    public void TM1_15_Sozluk_TR_EN_dolu_farkli_jargonsuz()
    {
        var tr = MusteriTuvalSozlugu.Tr;
        var en = MusteriTuvalSozlugu.En;
        var trA = tr.GetType().GetProperties().Select(p => (string)p.GetValue(tr)!).ToList();
        var enA = en.GetType().GetProperties().Select(p => (string)p.GetValue(en)!).ToList();
        Assert.All(trA, m => Assert.False(string.IsNullOrWhiteSpace(m)));
        Assert.All(enA, m => Assert.False(string.IsNullOrWhiteSpace(m)));
        for (var i = 0; i < trA.Count; i++) Assert.NotEqual(trA[i], enA[i]);
        foreach (var j in new[] { "cari", "storno", "vezne", "pasife al", "denetim izi" })
            Assert.All(trA, m => Assert.DoesNotContain(j, m, StringComparison.OrdinalIgnoreCase));
        Assert.Same(en, MusteriTuvalSozlugu.Al("en"));
        Assert.Same(tr, MusteriTuvalSozlugu.Al("tr"));
        // Tuval etiketleri birebir.
        Assert.Equal("Ad ya da telefon", tr.AramaYerTutucu);
        Assert.Equal("{0} müşteri · {1} gösteriliyor", tr.Sayac);
        Assert.Equal("adres, TC, e-posta, limit", MusteriListeSozlugu.Tr.DahaFazlaIcerik);
    }
    // ═════════════════════════════════════════════════════════════════════════
    // DOĞRULAYICI KRİTİK (24.09) — YARIŞ: panel önceki müşterinin alanlarıyla
    // açılıyor, Kaydet açık → B'nin kartı A'nın verisiyle PATCH'leniyordu.
    // Gecikmeli sahte API: kart yanıtı TaskCompletionSource ile bekletilir.
    // ═════════════════════════════════════════════════════════════════════════

    private const string ZeynepKarti = """
        {"id":"c3","ad":"Zeynep Demir","tur":"MUSTERI","telefon":"5412089017","email":null,
         "vergiNo":null,"adres":null,"krediLimitiTl":null,"varsayilanVadeGun":null,
         "notlar":null,"pepBeyani":false,"pepGorev":null,"pepBeyanTarihi":null}
        """;

    private static bool Kilitli(IRenderedComponent<SayfaCariler> e, string secici) =>
        e.Find(secici).HasAttribute("disabled");

    [Fact]
    public void TM1_16_Yaris_A_kapat_B_ac_yanit_gelmeden_alanlar_bos_Kaydet_kilitli_PATCH_B_verisiyle()
    {
        var e = Ciz();
        // A (Hasan) açılır, kart yüklenir, kapatılır.
        e.Find("#c-satir-c1 td").Click();
        e.WaitForAssertion(() => Assert.Equal("Hasan Kaya", e.Find("#duz-ad").GetAttribute("value")));
        e.Find("#cekmece-kapat").Click();
        Assert.Empty(e.FindAll("#cekmece-cari"));

        // B (Zeynep) açılır; kart yanıtı BEKLETİLİR.
        var kaynak = new TaskCompletionSource();
        bekle["/cari/c3"] = kaynak;
        ozel["/cari/c3"] = (HttpStatusCode.OK, ZeynepKarti);
        istekler.Clear();
        e.Find("#c-satir-c3 td").Click();
        e.WaitForAssertion(() => Assert.Equal("Zeynep Demir", e.Find("#cekmece-cari-baslik").TextContent.Trim()));

        // Yanıt gelmeden: A'nın hiçbir değeri alanlarda YOK, alanlar ve Kaydet KİLİTLİ, sebep yazılı.
        Assert.Equal("", e.Find("#duz-ad").GetAttribute("value") ?? "");
        Assert.Equal("", e.Find("#duz-telefon").GetAttribute("value") ?? "");
        Assert.DoesNotContain("Hasan", e.Find("#cekmece-cari-govde").InnerHtml);
        Assert.True(Kilitli(e, "#duz-ad"));
        Assert.True(Kilitli(e, "#cekmece-cari-kaydet"));
        Assert.Contains(MusteriTuvalSozlugu.Tr.KartYukleniyor, e.Find("#cekmece-cari-kilit").TextContent);
        // `disabled` yalnız tarayıcı kilididir: olay yine de gelirse (betik, eski tarayıcı)
        // ad dolu diye Kaydet AÇILMAMALI — kart gelmeden PATCH katlı alanları siler.
        e.Find("#duz-ad").Input("Elle Yazılan");
        Assert.True(Kilitli(e, "#cekmece-cari-kaydet"));
        e.Find("#cekmece-cari-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");

        // Yanıt gelince B'nin verisi dolar, kilit kalkar; PATCH B'ye B'nin verisiyle gider.
        kaynak.SetResult();
        e.WaitForAssertion(() => Assert.Equal("Zeynep Demir", e.Find("#duz-ad").GetAttribute("value")));
        Assert.False(Kilitli(e, "#cekmece-cari-kaydet"));
        Assert.False(Kilitli(e, "#duz-ad"));
        e.Find("#cekmece-cari-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH"));
        var patch = istekler.Single(i => i.Metot == "PATCH");
        Assert.EndsWith("/cari/c3", patch.Yol);
        var govde = System.Text.Json.JsonDocument.Parse(patch.Govde).RootElement;
        Assert.Equal("Zeynep Demir", govde.GetProperty("ad").GetString());
        Assert.Equal("5412089017", govde.GetProperty("telefon").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, govde.GetProperty("vergiNo").ValueKind);
        Assert.DoesNotContain("Hasan", patch.Govde);
    }

    [Fact]
    public async Task TM1_17_Yaris_gec_gelen_eski_kart_yaniti_yeni_panele_YAZILMAZ()
    {
        var e = Ciz();
        // A'nın kart yanıtı bekletilir; A kapatılır, B açılır (B hemen döner).
        var kaynakA = new TaskCompletionSource();
        bekle["/cari/c1"] = kaynakA;
        ozel["/cari/c3"] = (HttpStatusCode.OK, ZeynepKarti);
        e.Find("#c-satir-c1 td").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#cekmece-cari")));
        e.Find("#cekmece-kapat").Click();
        e.Find("#c-satir-c3 td").Click();
        e.WaitForAssertion(() => Assert.Equal("Zeynep Demir", e.Find("#duz-ad").GetAttribute("value")));

        // A'nın GEÇ yanıtı şimdi gelir: B'nin alanlarını EZMEMELİ.
        kaynakA.SetResult();
        // Geç yanıtın devamı çiziciye düşsün (yanıt gelir, sayfa işler) — sonra durum okunur.
        await Task.Delay(400);
        e.Render();
        Assert.Equal("Zeynep Demir", e.Find("#duz-ad").GetAttribute("value"));
        Assert.Equal("Zeynep Demir", e.Find("#cekmece-cari-baslik").TextContent.Trim());
        istekler.Clear();
        e.Find("#cekmece-cari-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH"));
        var patch = istekler.Single(i => i.Metot == "PATCH");
        Assert.EndsWith("/cari/c3", patch.Yol);
        Assert.DoesNotContain("Hasan", patch.Govde);
        Assert.DoesNotContain("Kapalıçarşı", patch.Govde);
    }

    [Fact]
    public void TM1_18_Kart_alinamazsa_Kaydet_KILITLI_kalir_sebep_yazar()
    {
        var e = Ciz();
        ozel["/cari/c3"] = (HttpStatusCode.InternalServerError, """{"hata":"Sunucu hatası"}""");
        e.Find("#c-satir-c3 td").Click();
        e.WaitForAssertion(() => Assert.Contains(MusteriTuvalSozlugu.Tr.KartYuklenemedi, e.Find("#cekmece-cari-kilit").TextContent));
        Assert.True(Kilitli(e, "#cekmece-cari-kaydet"));
        istekler.Clear();
        e.Find("#cekmece-cari-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
    }

    [Fact]
    public void TM1_19_Kisi_bilgisi_gelene_dek_yer_tutucu_kart_PARALEL_yuklenir()
    {
        var e = Ciz();
        var denetim = new TaskCompletionSource();
        bekle["/denetim"] = denetim;
        e.Find("#c-satir-c1 td").Click();
        // Denetim yanıtı beklerken kart YÜKLENMİŞ olmalı (sıralı bekleme kilidi uzatmaz).
        e.WaitForAssertion(() => Assert.Equal("Hasan Kaya", e.Find("#duz-ad").GetAttribute("value")));
        Assert.False(Kilitli(e, "#cekmece-cari-kaydet"));
        Assert.Contains(MusteriTuvalSozlugu.Tr.KisiAliniyor, e.Find("#cari-son-degisiklik-metin").TextContent);
        denetim.SetResult();
        e.WaitForAssertion(() => Assert.Contains("Ayşe Y.", e.Find("#cari-son-degisiklik-metin").TextContent));
        Assert.DoesNotContain(MusteriTuvalSozlugu.Tr.KisiAliniyor, e.Find("#cari-son-degisiklik-metin").TextContent);
    }
}
