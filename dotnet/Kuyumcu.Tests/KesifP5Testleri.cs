using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P5 · SATIŞ-VEZNE + SATIS-4 (Kurul 5) — testler.
//   #17  /vezne kalem satırı: ürün seçici ezik değil, BASİT'te "sunucu" yok
//   #67  /islemler alt satır: Vazgeç ile Kaydet aynı sınıf ve aynı boy kuralı
//   #70  satır içi birim fiyat tr-TR binlikli, aynen kaydedilince değişmez
//   (a)  eşik üstünde ödeme varsayılanı nakit dışı, Kaydet etkin; elle seçim ezilmez; had bilinmiyorsa nakit
//   (b)  tek kimlik formu (hurda: satıcı kimliği MASAK kutusundan, gövdede VUK 234 alanları); müşteriden BİR KEZ ön dolum
//   (c)  arama satırında fiyat + stok, satır başına istek YOK (iki toplu istek); uç düşerse "—" (hata "yok"a çevrilmez)
//   (d)  son kayıt satırı PRO'da ve hurdada da kalıcı (başarı bandı kapandıktan sonra durur)
//   HTTP GET /fiyat/teklifler: tekli teklifle AYNI fiyat · sınırlar 400 · kiracı yalıtımı; /vezne şubesiz metni
// ⚠ Eşik sayıları testte SAHTE SUNUCUDAN gelir (istemciye gömülmez); para metni Bicim ile üretilir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Başka test sınıflarının da kullandığı SATIS-4 yardımcıları.</summary>
internal static class Satis4Yardim
{
    private static readonly TimeSpan Bekle = TimeSpan.FromSeconds(5);

    /// <summary>SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — nakit KURALINI sınayan testler kullanıcının
    /// nakdi ELLE seçtiği durumu kurar (elle seçim üzerine yazılmaz).</summary>
    public static void NakitElleSec(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        e.WaitForElement("#islem-odeme", Bekle);
        SeciciYardim.Sec(e, "islem-odeme", "NAKIT");
    }

    /// <summary>SATIS-4 (b): müşteriden dolan belge numarasını siler ("kimlik eksik" hâli; yeniden dolmaz).</summary>
    public static void MasakBelgeNoSil(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        e.WaitForElement("#masak-belge-no", Bekle);
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(e.Find("#masak-belge-no").GetAttribute("value"))), Bekle);
        e.Find("#masak-belge-no").Input("");
        e.WaitForAssertion(() => Assert.True(string.IsNullOrEmpty(e.Find("#masak-belge-no").GetAttribute("value"))), Bekle);
    }
}

public sealed class KesifP5EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string tevsikJson = Tevsik(30000m);
    private bool tevsikHata, topluHata;
    /// <summary>Sunucu gibi: islemTutariKuraliDevrede = |fisToplamTl| &gt; had (TevsikController ile aynı ifade; had 30.000 test verisi).</summary>
    private bool tevsikTutaraGore;
    private static string TevsikTutaraGore(string yol)
    {
        var m = Regex.Match(yol, @"fisToplamTl=([0-9.]+)");
        var t = m.Success ? decimal.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0m;
        var devrede = t > 30000m ? "true" : "false";
        return $$"""{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":{{(t > 30000m ? "0" : "30000")}},"islemTutariKuraliDevrede":{{devrede}}}""";
    }
    private string kasalarJson = """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""";
    private string gereksinimJson = GereksinimYok;
    private string masakEsikJson = Esik(185000m);
    private decimal fiyat = 129247.86m;
    private static readonly TimeSpan Bekle = TimeSpan.FromSeconds(5);
    private static KesifP5Metinleri P5 => KesifP5Sozlugu.Tr;

    private const string GereksinimYok = """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""";
    private const string GereksinimKimlik = """{"kademe":"YALNIZ_KIMLIK","kimlikGerekli":true,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""";

    private static string Tevsik(decimal had)
    {
        var h = had.ToString(CultureInfo.InvariantCulture);
        return $$"""{"had":{{h}},"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":{{h}},"islemTutariKuraliDevrede":false}""";
    }

    private static string Esik(decimal k)
    {
        var s = k.ToString(CultureInfo.InvariantCulture);
        return $$"""{"kimlikEsigi":{{s}},"amacBeyaniEsigi":{{s}},"beyanFormuEsigi":{{s}},"ekBelgeEsigi":{{s}},"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""";
    }

    public KesifP5EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private string Urun1 => $$"""{"id":"u1","ad":"Burma Bilezik 22","ayarId":"K22","hasGrami":15.2,"iscilikTl":0,"satisFiyati":null,"gramAgirlik":16.6,"milyem":916,"barkod":"8690000000412","kod":"K-1","birim":"ADET"}""";
    private const string Urun2 = """{"id":"u2","ad":"Hasır Bilezik","ayarId":"K14","hasGrami":2.1,"iscilikTl":0,"satisFiyati":null,"gramAgirlik":3.6,"milyem":585,"barkod":"8690000000999","kod":"K-2","birim":"ADET"}""";

    private sealed class SahteApi(KesifP5EkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            if ((yol.Contains("/tevsik/gunluk") && t.tevsikHata)
                || (t.topluHata && (yol.Contains("/fiyat/teklifler") || yol.Contains("/urunler/stok-adetleri"))))
                return new(HttpStatusCode.InternalServerError)
                { Content = new StringContent("""{"message":"uç düştü"}""", System.Text.Encoding.UTF8, "application/json") };
            var f = t.fiyat.ToString(CultureInfo.InvariantCulture);
            var bugun = DateTime.UtcNow.ToString("o");
            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/tevsik/gunluk") && t.tevsikTutaraGore => TevsikTutaraGore(v),
                var v when v.Contains("/tevsik/gunluk") => t.tevsikJson,
                var v when v.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende","varsayilan":true,"aktif":true}]""",
                var v when v.Contains("/fiyat/teklifler") =>
                    $$"""{"satirlar":[{"urunId":"u1","teklifVar":true,"satisFiyati":{{f}},"sebep":null},{"urunId":"u2","teklifVar":false,"satisFiyati":null,"sebep":"FIYAT_YOK"}]}""",
                var v when v.Contains("/fiyat/teklif") => $$"""{"teklifVar":true,"listeFiyati":{{f}},"hasDegeri":{{f}},"marjTl":0,"iscilikTl":0,"indirimTutari":0,"satisFiyati":{{f}},"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler/stok-adetleri") => """{"urunler":[{"urunId":"u1","toplamAdet":3,"toplamHasGram":45.6},{"urunId":"u2","toplamAdet":0,"toplamHasGram":0}]}""",
                var v when v.Contains("/urunler") => $"[{t.Urun1},{Urun2}]",
                var v when v.Contains("/kasalar") => t.kasalarJson,
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Ayşe Nur Demir","vergiNo":"10000000146","kimlikTuru":"TCKN","adres":"Ulus, Ankara"},{"id":"c2","ad":"Hasan Hüseyin Çelikkanat"},{"id":"c3","ad":"Anadolu Holding A.Ş.","vergiNo":"1234567890","kimlikTuru":"VKN","adres":"Kızılay"}]""",
                var v when v.Contains("/masak/gereksinim") => t.gereksinimJson,
                var v when v.Contains("/masak/esikler") => t.masakEsikJson,
                var v when v.Contains("/islemler/personeller") => """{"secimSerbest":false,"oturumId":"u1","personeller":[{"id":"u1","ad":"Test","soyad":"Tezgâhtar"}],"dahaFazlasiVar":false}""",
                var v when v.Contains("/islemler/altin-bacagi/ayarlar") => """{"stopajOranlari":[],"alisIskontoOran":null,"saticiAdresiZorunlu":true}""",
                var v when v.Contains("/islemler/iade-adaylari") => "[]",
                var v when v.Contains("/islemler/fis/") => """{"fisId":"01J00000000000000000000002","kapsam":"KAYITLI","odemeKalemleri":[]}""",
                var v when v.Contains("/islemler/") && istek.Method == HttpMethod.Post =>
                    """{"id":"01J00000000000000000000001","fisId":"01J00000000000000000000002","tur":"SATIS","tlTutari":10000}""",
                var v when v.Contains("/reyon") => "[]",
                var v when v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod, string rol = "TEZGAHTAR")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", rol, "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-kaydet");
        e.WaitForAssertion(() => Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari")), Bekle);
        return e;
    }

    private void KalemEkle(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, int beklenen = 1, string urunId = "u1")
    {
        SeciciYardim.Sec(e, "islem-urun", urunId);
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))), Bekle);
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.Equal(beklenen, e.FindAll("#islem-kalem-tablo tbody tr, #islem-kalem-pro tr.fis-pro-satir").Count), Bekle);
    }

    private List<(string Metot, string Yol, string Govde)> Postlar(string uc) { lock (istekler) return istekler.Where(i => i.Metot == "POST" && i.Yol.Contains(uc)).ToList(); }
    private int Say(string parca) { lock (istekler) return istekler.Count(i => i.Yol.Contains(parca)); }

    // ═══ (a) ═════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void SATIS4a_esik_ustu_satista_odeme_varsayilani_KART_Kaydet_ETKIN_govdede_KART(string mod)
    {
        var e = Ekran(mod);
        SeciciYardim.Sec(e, "islem-cari", "c1");   // gerçek müşteri (Peşin Müşteri bu tutarda yasal olarak kullanılamaz — sunucu K2)
        KalemEkle(e);                              // 129.247,86 > sunucu haddi 30.000
        e.WaitForAssertion(() => Assert.Equal("KART", SeciciYardim.SeciliDeger(e, "islem-odeme")), Bekle);
        // YASAL-KAPI-3 tur 2 (hakem "aynı uyarı tek kez"): PRO'da not "Yasal kapılar" panelinde TEK yerde, alan altında ikinci kez yok.
        Assert.Equal(string.Format(P5.OdemeOtomatikKart, Bicim.Tl(30000m)), e.Find(mod == ArayuzModuServisi.Profesyonel ? "#islem-pro-nakit-not" : "#islem-odeme-otomatik").TextContent.Trim());
        if (mod == ArayuzModuServisi.Profesyonel) Assert.Empty(e.FindAll("#islem-odeme-otomatik"));
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled"), e.Find("#islem-kaydet").GetAttribute("title")), Bekle);
        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/islemler/satis")), Bekle);
        var g = Postlar("/islemler/satis")[0].Govde;
        Assert.Contains("\"KART\"", g);
        Assert.DoesNotContain("\"NAKIT\"", g);
    }

    [Fact]
    public void SATIS4a_KAYITTAN_sonra_BOS_fiste_0_TL_nakit_bandi_ve_acik_odeme_secici_YOK()
    {
        // canlı ölçümde bulundu: önceki fişin önizlemesi (işlem tutarı kuralı devrede) boş fişte kalıyordu
        tevsikJson = """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":true}""";
        var e = Ekran(ArayuzModuServisi.Basit);
        SeciciYardim.Sec(e, "islem-cari", "c1");
        KalemEkle(e);
        e.WaitForAssertion(() => Assert.Equal("KART", SeciciYardim.SeciliDeger(e, "islem-odeme")), Bekle);
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled"), e.Find("#islem-kaydet").GetAttribute("title")), Bekle);
        e.Find("#islem-kaydet").Click();
        e.WaitForElement("#islem-son-fis", Bekle);
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-yasal-kapi")), Bekle);
        Assert.Empty(e.FindAll("#islem-odeme"));
        Assert.DoesNotContain(Satis3Sozlugu.Tr.NakitAlinamaz, e.Markup);
    }

    [Fact]
    public void SATIS4a_PRO_kart_satisi_KAYDEDILINCE_bos_fiste_yasal_kapi_ASILDI_YOK()
    {
        // tasarım hakemi KRİTİK (01.10): PRO "Yasal kapılar" paneli önceki fişin önizlemesiyle 0 ₺ fişte "Aşıldı" kalıyordu.
        // Sahte sunucu gerçeğiyle aynı: kural fiş tutarına göre devrede (129.247,86 → devrede; 0 ₺ → değil).
        tevsikTutaraGore = true;
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        SeciciYardim.Sec(e, "islem-cari", "c1");
        KalemEkle(e);
        e.WaitForAssertion(() => Assert.Equal("KART", SeciciYardim.SeciliDeger(e, "islem-odeme")), Bekle);
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled"), e.Find("#islem-kaydet").GetAttribute("title")), Bekle);
        Assert.Equal(SatisProSozlugu.Tr.Uygun, e.Find("#islem-pro-nakit").TextContent.Trim());   // Kart: nakit kapısı işlemez
        SeciciYardim.Sec(e, "islem-odeme", "NAKIT");                                             // önce sunucunun "devrede"si görünür olsun
        e.WaitForAssertion(() => Assert.Equal(SatisProSozlugu.Tr.Asildi, e.Find("#islem-pro-nakit").TextContent.Trim()), Bekle);
        SeciciYardim.Sec(e, "islem-odeme", "KART");
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled"), e.Find("#islem-kaydet").GetAttribute("title")), Bekle);
        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/islemler/satis")), Bekle);
        e.WaitForElement("#islem-son-fis", Bekle);
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-kalem-pro tr.fis-pro-satir")), Bekle);
        // boş fiş: nakit satırı "Aşıldı" DEĞİL (önceki fişin bayrağı bayat) — panel kırmızı açılmaz
        e.WaitForAssertion(() =>
        {
            var nakit = e.FindAll("#islem-pro-nakit");
            Assert.NotEmpty(nakit);
            Assert.All(nakit, n => Assert.NotEqual(SatisProSozlugu.Tr.Asildi, n.TextContent.Trim()));
            Assert.All(nakit, n => Assert.DoesNotContain("metin-kirmizi", n.ClassName ?? ""));
        }, Bekle);
    }

    [Fact]
    public void SATIS4a_PRO_otomatik_kart_nedeni_YASAL_panelde_gorunur_nakit_satiri_ASILDI_degil()
    {
        // doğrulayıcı DÜŞÜK (01.10): PRO'da ödeme katmanı kapalıyken otomatik seçimin nedeni görünmüyordu
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        SeciciYardim.Sec(e, "islem-cari", "c1");
        KalemEkle(e);
        e.WaitForAssertion(() => Assert.Equal("KART", SeciciYardim.SeciliDeger(e, "islem-odeme")), Bekle);
        e.WaitForAssertion(() => Assert.Equal(string.Format(P5.OdemeOtomatikKart, Bicim.Tl(30000m)), e.Find("#islem-pro-nakit-not").TextContent.Trim()), Bekle);
        Assert.Equal(SatisProSozlugu.Tr.Uygun, e.Find("#islem-pro-nakit").TextContent.Trim());
        SeciciYardim.Sec(e, "islem-odeme", "NAKIT");   // elle Nakit → not düşer
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-pro-nakit-not")), Bekle);
    }

    [Fact]
    public void SATIS4a_Vazgec_ile_bosalan_fiste_OTOMATIK_kart_varsayilana_NAKITe_doner()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        SeciciYardim.Sec(e, "islem-cari", "c1");
        KalemEkle(e);
        e.WaitForAssertion(() => Assert.Equal("KART", SeciciYardim.SeciliDeger(e, "islem-odeme")), Bekle);
        e.Find("#islem-vazgec").Click();
        e.WaitForAssertion(() => Assert.Equal("NAKIT", SeciciYardim.SeciliDeger(e, "islem-odeme")), Bekle);
        Assert.Empty(e.FindAll("#islem-odeme-otomatik"));
    }

    [Fact]
    public void SATIS4a_elle_NAKIT_secimi_EZILMEZ_kilit_sebebi_haddi_soyler()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        SeciciYardim.Sec(e, "islem-cari", "c1");
        KalemEkle(e);
        e.WaitForAssertion(() => Assert.Equal("KART", SeciciYardim.SeciliDeger(e, "islem-odeme")), Bekle);
        SeciciYardim.Sec(e, "islem-odeme", "NAKIT");
        e.WaitForAssertion(() => Assert.Equal(string.Format(Satis3Sozlugu.Tr.NakitHaddiKilit, Bicim.Tl(30000m)), e.Find("#islem-kaydet").GetAttribute("title")), Bekle);
        Assert.Equal("NAKIT", SeciciYardim.SeciliDeger(e, "islem-odeme"));
        Assert.Empty(e.FindAll("#islem-odeme-otomatik"));
        e.Find("#islem-kaydet").Click();
        Assert.Empty(Postlar("/islemler/satis"));
    }

    [Fact]
    public void SATIS4a_had_BILINMIYORSA_varsayilan_NAKIT_kalir_istemci_esik_uydurmaz()
    {
        tevsikHata = true;   // sunucu haddi okunamadı → istemci sayı gömmez, davranış eskisi gibi (son söz sunucuda)
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        SeciciYardim.Sec(e, "islem-cari", "c1");
        KalemEkle(e);
        e.WaitForAssertion(() => Assert.Equal("NAKIT", SeciciYardim.SeciliDeger(e, "islem-odeme")), Bekle);
        Assert.Empty(e.FindAll("#islem-odeme-otomatik"));
    }

    [Fact]
    public void SATIS4a_tutar_esik_ALTINA_inince_otomatik_secim_NAKITe_doner()
    {
        fiyat = 20000m; tevsikJson = Tevsik(30000m);
        var e = Ekran(ArayuzModuServisi.Basit);
        SeciciYardim.Sec(e, "islem-cari", "c1");
        KalemEkle(e);
        Assert.Empty(e.FindAll("#islem-odeme-otomatik"));   // 20.000 ≤ had → nakit
        KalemEkle(e, 2, "u2");                               // ikinci kalem: 40.000 > had
        e.WaitForAssertion(() => Assert.Equal("KART", SeciciYardim.SeciliDeger(e, "islem-odeme")), Bekle);
        Assert.NotEmpty(e.FindAll("#islem-odeme-otomatik"));
        // doğrulayıcı (ORTA, 01.10): fiş DOLU kalarak tutar hadde ALTINA iner (boş fiş dalı değil) — 2 kalem → 1 kalem
        e.Find("#islem-kalem-sil-1").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#islem-kalem-tablo tbody tr")), Bekle);
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-odeme-otomatik")), Bekle);
        Assert.Empty(e.FindAll("#islem-odeme"));                          // BASİT: Nakit'e dönen ödeme seçicisi yeniden gizlenir
        Assert.Contains(Sozlukler.Tr.Islem.OdemeNakit, e.Find("#islem-odeme-sade").TextContent);
    }

    [Fact]
    public void SATIS4a_Hurda_esik_ustu_varsayilan_HAVALE_para_disari_yonunde()
    {
        var e = Ekran(ArayuzModuServisi.Basit, "MUDUR");
        e.Find("#islem-tur-HURDA_ALIS").Click();
        e.WaitForElement("#hurda-satici");
        SeciciYardim.Sec(e, "islem-cari", "c2");
        e.Find("#hurda-gram").Input("10");   // 10 g × 750‰ × 6.800 ≈ 51.000 > had 30.000
        e.WaitForAssertion(() => Assert.Equal("HAVALE", SeciciYardim.SeciliDeger(e, "islem-odeme")), Bekle);
        Assert.Equal(string.Format(P5.OdemeOtomatikHavale, Bicim.Tl(30000m)), e.Find("#islem-odeme-otomatik").TextContent.Trim());
    }

    // ═══ (b) ═════════════════════════════════════════════════════════════════

    [Fact]
    public void SATIS4b_Hurda_MASAK_acikken_satici_kimligi_TEK_form_POST_bir_kez_govdede_VUK234_alanlari()
    {
        masakEsikJson = Esik(1000m); gereksinimJson = GereksinimKimlik;
        var e = Ekran(ArayuzModuServisi.Basit, "MUDUR");
        e.Find("#islem-tur-HURDA_ALIS").Click();
        e.WaitForElement("#hurda-satici");
        SeciciYardim.Sec(e, "islem-cari", "c2");
        e.Find("#hurda-gram").Input("10");
        SeciciYardim.Sec(e, "hurda-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#hurda-kimlik-not", Bekle);
        // satıcı ad/TCKN/adres kutuları ikinci kez ÇİZİLMEZ — kimlik tek kutuda
        Assert.Empty(e.FindAll("#hurda-satici-ad"));
        Assert.Empty(e.FindAll("#hurda-satici-tckn"));
        Assert.Empty(e.FindAll("#hurda-satici-adres"));
        Assert.Equal(P5.KimlikAsagidaAlinir, e.Find("#hurda-kimlik-not").TextContent.Trim());
        // kimlik kutusu seçili müşteriden ön dolar (ad)
        e.WaitForAssertion(() => Assert.Equal("Hasan Hüseyin Çelikkanat", e.Find("#masak-ad-soyad").GetAttribute("value")), Bekle);
        e.Find("#masak-belge-no").Input("10000000146");
        e.Find("#masak-adres").Input("Kapalıçarşı No:1");
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled"), e.Find("#islem-kaydet").GetAttribute("title")), Bekle);
        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar("/islemler/hurda")), Bekle);
        var g = JsonDocument.Parse(Postlar("/islemler/hurda")[0].Govde).RootElement;
        var s = g.GetProperty("satici");
        Assert.Equal("NIHAI_TUKETICI", s.GetProperty("saticiTipi").GetString());
        Assert.Equal("Hasan Hüseyin Çelikkanat", s.GetProperty("adSoyad").GetString());
        Assert.Equal("10000000146", s.GetProperty("tckn").GetString());
        Assert.Equal("Kapalıçarşı No:1", s.GetProperty("adres").GetString());
        var m = g.GetProperty("masak");
        Assert.Equal("10000000146", m.GetProperty("belgeNo").GetString());
        Assert.Equal("Hasan Hüseyin Çelikkanat", m.GetProperty("adSoyad").GetString());
    }

    [Fact]
    public void SATIS4b_Hurda_tek_formda_adres_EKSIKSE_yolu_kimlik_kutusunu_gosteren_alan_hatasi()
    {
        masakEsikJson = Esik(1000m); gereksinimJson = GereksinimKimlik;
        var e = Ekran(ArayuzModuServisi.Basit, "MUDUR");
        e.Find("#islem-tur-HURDA_ALIS").Click();
        e.WaitForElement("#hurda-satici");
        SeciciYardim.Sec(e, "islem-cari", "c2");
        e.Find("#hurda-gram").Input("10");
        SeciciYardim.Sec(e, "hurda-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#hurda-kimlik-not", Bekle);
        e.WaitForAssertion(() => Assert.Equal(P5.KimlikAdresGerekli, e.Find("#hurda-satici-adres-hata").TextContent.Trim()), Bekle);
        e.Find("#masak-belge-no").Input("10000000146");
        e.Find("#masak-adres").Input("Kapalıçarşı No:1");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hurda-satici-adres-hata")), Bekle);
    }

    [Fact]
    public void SATIS4b_Hurda_MASAK_PASAPORT_ise_satici_TCKN_ayrica_sorulur_uydurulmaz()
    {
        masakEsikJson = Esik(1000m); gereksinimJson = GereksinimKimlik;
        var e = Ekran(ArayuzModuServisi.Basit, "MUDUR");
        e.Find("#islem-tur-HURDA_ALIS").Click();
        e.WaitForElement("#hurda-satici");
        SeciciYardim.Sec(e, "islem-cari", "c2");
        e.Find("#hurda-gram").Input("10");
        SeciciYardim.Sec(e, "hurda-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#hurda-kimlik-not", Bekle);
        SeciciYardim.Sec(e, "masak-belge-turu", "PASAPORT");
        e.WaitForElement("#hurda-satici-tckn", Bekle);
        Assert.Empty(e.FindAll("#hurda-satici-ad"));
    }

    [Fact]
    public void SATIS4b_Satis_MASAK_secili_musteriden_BIR_KEZ_doldu_silinen_alan_yeniden_DOLMAZ()
    {
        masakEsikJson = Esik(1000m); gereksinimJson = GereksinimKimlik;
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        SeciciYardim.Sec(e, "islem-cari", "c1");
        KalemEkle(e);
        e.WaitForElement("#masak-belge-no", Bekle);
        e.WaitForAssertion(() => Assert.Equal("10000000146", e.Find("#masak-belge-no").GetAttribute("value")), Bekle);
        Assert.Equal("Ayşe Nur Demir", e.Find("#masak-ad-soyad").GetAttribute("value"));
        e.Find("#masak-diger-ac").Click();   // YASAL-PARAM-EKRAN tur 2: isteğe bağlı adres katlanırda (ön dolu)
        Assert.Equal("Ulus, Ankara", e.Find("#masak-adres").GetAttribute("value"));
        e.Find("#masak-belge-no").Input("");
        e.Find("#masak-ad-soyad").Input("Ayşe Nur Demir");   // başka bir çizim tetiklenir
        Assert.True(string.IsNullOrEmpty(e.Find("#masak-belge-no").GetAttribute("value")));
    }

    [Fact]
    public void SATIS4b_TCKN_disi_kimlik_turlu_musteride_11_haneli_no_BELGE_NO_olarak_dolmaz()
    {
        masakEsikJson = Esik(1000m); gereksinimJson = GereksinimKimlik;
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        SeciciYardim.Sec(e, "islem-cari", "c3");
        KalemEkle(e);
        e.WaitForElement("#masak-belge-no", Bekle);
        e.WaitForAssertion(() => Assert.Equal("Anadolu Holding A.Ş.", e.Find("#masak-ad-soyad").GetAttribute("value")), Bekle);
        Assert.True(string.IsNullOrEmpty(e.Find("#masak-belge-no").GetAttribute("value")));
    }

    // ═══ (c) ═════════════════════════════════════════════════════════════════

    private static void Yaz(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string metin)
    {
        e.Find("#islem-barkod").Focus();
        for (var i = 1; i <= metin.Length; i++) e.Find("#islem-barkod").Input(metin[..i]);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void SATIS4c_arama_satirinda_FIYAT_ve_STOK_iki_toplu_istek_satir_basina_istek_YOK(string mod)
    {
        var e = Ekran(mod);
        Yaz(e, "bil");
        e.WaitForAssertion(() => Assert.Equal(Bicim.Tl(fiyat), e.Find("#islem-barkod-oneri-fiyat-0").TextContent.Trim()), Bekle);
        e.WaitForAssertion(() => Assert.Equal(string.Format(P5.Stok, "3"), e.Find("#islem-barkod-oneri-stok-0").TextContent.Trim()), Bekle);
        Assert.Equal("—", e.Find("#islem-barkod-oneri-fiyat-1").TextContent.Trim());   // sunucu: fiyat yok → "—"
        Assert.Equal(P5.StokYok, e.Find("#islem-barkod-oneri-stok-1").TextContent.Trim());
        Assert.Contains("stok-yok", e.Find("#islem-barkod-oneri-stok-1").ClassName);
        Assert.Equal(1, Say("/fiyat/teklifler"));
        Assert.Equal(1, Say("/urunler/stok-adetleri"));
        Assert.Equal(0, Say("/fiyat/teklif?urunId="));   // satır başına tekli teklif YOK
        lock (istekler) Assert.Contains(istekler, i => i.Yol.Contains("/fiyat/teklifler") && i.Yol.Contains("kurSnapshotId=kur-1"));
    }

    [Fact]
    public void SATIS4c_toplu_uclar_DUSERSE_liste_kalir_fiyat_ve_stok_bilinmiyor_yazar()
    {
        topluHata = true;
        var e = Ekran(ArayuzModuServisi.Basit);
        Yaz(e, "bil");
        e.WaitForElement("#islem-barkod-oneri-0", Bekle);
        e.WaitForAssertion(() => Assert.Equal(P5.StokBilinmiyor, e.Find("#islem-barkod-oneri-stok-0").TextContent.Trim()), Bekle);
        Assert.Equal("—", e.Find("#islem-barkod-oneri-fiyat-0").TextContent.Trim());
        Assert.NotEqual(P5.StokYok, e.Find("#islem-barkod-oneri-stok-0").TextContent.Trim());   // hata "stok yok"a çevrilmez
    }

    // ═══ (d) ═════════════════════════════════════════════════════════════════

    [Fact]
    public void SATIS4d_PRO_satista_son_kayit_BANT_KAPANDIKTAN_sonra_da_gorunur()
    {
        fiyat = 10000m;
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        KalemEkle(e);
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled"), e.Find("#islem-kaydet").GetAttribute("title")), Bekle);
        e.Find("#islem-kaydet").Click();
        e.WaitForElement("#islem-son-fis", Bekle);
        var metin = e.Find("#islem-son-fis").TextContent;
        Assert.Contains(Bicim.Tl(10000m), metin);
        Thread.Sleep(3600);   // başarı bandı 3 sn'de kapanır
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#basari-bandi .bant-basari, .bant-basari")), Bekle);
        Assert.Equal(metin, e.Find("#islem-son-fis").TextContent);
    }

    [Fact]
    public void SATIS4d_Hurda_kaydinda_da_son_kayit_satiri_tur_ve_tutarla_kalir()
    {
        var e = Ekran(ArayuzModuServisi.Basit, "MUDUR");
        e.Find("#islem-tur-HURDA_ALIS").Click();
        e.WaitForElement("#hurda-satici");
        SeciciYardim.Sec(e, "islem-cari", "c2");
        e.Find("#hurda-gram").Input("1");   // 5.100 < had
        SeciciYardim.Sec(e, "hurda-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#hurda-satici-ad");
        e.Find("#hurda-satici-tckn").Input("10000000146");
        e.Find("#hurda-satici-adres").Input("Kapalıçarşı No:1");
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled"), e.Find("#islem-kaydet").GetAttribute("title")), Bekle);
        e.Find("#islem-kaydet").Click();
        e.WaitForElement("#islem-son-fis", Bekle);
        Assert.Matches(@"\d{2}:\d{2}", e.Find("#islem-son-fis").TextContent);
    }

    [Fact]
    public void Kasasiz_kiracida_BASIT_kasa_secici_yer_tutucusu_satirin_TAMAMINI_alir()
    {
        // doğrulayıcı DÜŞÜK (01.10): kasa yokken "Kasa seçin..." yarım sütunda iki satıra kırılıyordu (BASİT 1280)
        kasalarJson = "[]";
        var e = Ekran(ArayuzModuServisi.Basit);
        e.WaitForElement("#islem-kasa", Bekle);
        var alan = e.Find("#islem-kasa-sar").ParentElement!;
        Assert.Contains("alan", alan.ClassList);
        Assert.Contains("form-grid-2", alan.ParentElement!.ClassList);
        Assert.Contains("fis-tuval-icerik", alan.ParentElement!.ParentElement!.ClassList);
        Assert.NotNull(e.Find("#islem-kasa").QuerySelector(".secici-metin.yer-tutucu"));
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tuval-satis.css");
        Assert.Contains(".tuval-tezgah .fis-tuval-icerik > .form-grid-2 > .alan:has(> .secici-alan .secici-metin.yer-tutucu) { grid-column: 1 / -1; }", css);
    }

    // ═══ #67 · #70 ═══════════════════════════════════════════════════════════

    [Fact]
    public void Islemler_alt_dugmeler_ayni_sinif()
    {
        fiyat = 10000m;
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        KalemEkle(e);
        var vazgec = e.Find("#islem-vazgec");
        var kaydet = e.Find("#islem-kaydet");
        Assert.Contains("btn", vazgec.ClassList);
        Assert.Contains("btn", kaydet.ClassList);
        Assert.Equal("islem-alt-satir", vazgec.ParentElement!.Id);   // aynı satırın doğrudan çocukları (css kuralı onlara)
        Assert.Equal("islem-alt-satir", kaydet.ParentElement!.Id);
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tuval-satis.css");
        Assert.Matches(new Regex(@"html\[data-mod\] \.islem-sayfa #islem-alt-satir > #islem-vazgec \{\s*height: var\(--dugme-yuksekligi-buyuk\); min-height: var\(--dugme-yuksekligi-buyuk\);"), css);
        Assert.Matches(new Regex(@"#islem-alt-satir > :is\(#islem-vazgec, #islem-kaydet\) \{ flex: 1 1 calc\(50% - 6px\);"), css);
    }
}

public sealed class KesifP5BilesenTestleri : BunitContext
{
    private readonly FormDurumu form = new();

    public KesifP5BilesenTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        GocC2Ortak.IzgaraServisleri(Services);   // GOC-C-2: kalem tablosu ortak ızgarada (ApiIstemci ister)
    }

    [Fact]
    public void FisKalemTablosu_birim_fiyat_binlikli()
    {
        var k = new FisKalemi { UrunId = "u1", UrunAdi = "Burma Bilezik", Adet = 1m, BirimFiyat = 11052.67m };
        var t = Render<FisKalemTablosu>(p => p
            .Add(x => x.Kalemler, [k])
            .Add(x => x.Urunler, Array.Empty<UrunVeri>())
            .Add(x => x.Form, form)
            .Add(x => x.Toplam, k.BirimFiyat)
            .Add(x => x.Degisti, EventCallback.Factory.Create<bool>(this, _ => { })));
        t.Find("#islem-kalem-duzenle-0").Click();
        Assert.Equal("11.052,67", t.Find("#si-kalem-fiyat-u1-ac").TextContent.Trim());
        t.Find("#si-kalem-fiyat-u1-ac").Click();
        Assert.Equal("11.052,67", t.Find("#si-kalem-fiyat-u1").GetAttribute("value"));
        t.Find("#si-kalem-fiyat-u1-kaydet").Click();
        Assert.Equal(11052.67m, k.BirimFiyat);   // aynen kaydedilince değer DEĞİŞMEZ
        Assert.Equal("1.500,00", FisKalemTablosu.BirimFiyatMetni(1500m));   // "1.500" değil (P-78 belirsiz)
        Assert.Equal("0,00", FisKalemTablosu.BirimFiyatMetni(0m));
        Assert.Equal("12,34567", FisKalemTablosu.BirimFiyatMetni(12.34567m));   // yuvarlanmaz
    }

    [Fact]
    public void KesifP5_sozluk_TR_EN_dolu_esik_sayisi_metne_gomulu_DEGIL()
    {
        foreach (var m in new[] { KesifP5Sozlugu.Tr, KesifP5Sozlugu.En })
            foreach (var p in typeof(KesifP5Metinleri).GetProperties())
            {
                var v = (string)p.GetValue(m)!;
                Assert.False(string.IsNullOrWhiteSpace(v), p.Name);
                Assert.DoesNotMatch(@"\d{2,}", v);   // eşik/sayı yer tutucudan gelir
            }
        Assert.DoesNotContain("sunucu", KesifP5Sozlugu.Tr.KurTahsilAnindaBasit, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class KesifP5VezneTestleri : BunitContext
{
    private const string Kuyruk = """
        {"durum":"VEZNEDE","istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":1,
         "dahaFazlasiVar":false,"vezneZorunlu":false,"gerekceAsgari":10,"subeId":"sb1",
         "durumKirilimi":[{"durum":"VEZNEDE","adet":1}],
         "fisler":[
           {"id":"v1","durum":"VEZNEDE","toplamTl":12000.00,"damgaliToplamTl":12000.00,
            "toplamTahmini":false,"cariId":null,"cariAd":null,
            "aciklama":null,"hazirlayanKullaniciId":"u2","hazirlayanAd":"Demo Kuyumcu",
            "kalemSayisi":1,"hareketFisId":null,"duzenlenebilir":false,"mutabakatGerekli":false,
            "olusturuldu":"2026-09-17T08:00:00Z","veznelendi":"2026-09-17T08:05:00Z",
            "guncellendi":"2026-09-17T08:05:00Z"}]}
        """;

    public KesifP5VezneTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/vezne/kuyruk") => Kuyruk,
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez kasa","paraBirimi":"TRY"}]""",
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Anadolu Holding A.Ş."}]""",
                var y when y.EndsWith("/urunler") => """[{"id":"u-1","ad":"22 Ayar Burma Bilezik"}]""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800}""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Vezne> FormAcik(bool basit)
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        var e = Render<Kuyumcu.Web.Pages.Vezne>();
        e.WaitForElement("#vz-satir-v1");
        e.Find("#vz-yeni-ac").Click();
        e.WaitForElement("#vz-f-kalem-0");
        return e;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Vezne_kalem_urun_secici_ezik_degil(bool basit)
    {
        var e = FormAcik(basit);
        var satir = e.Find("#vz-f-kalem-0");
        Assert.Contains("vz-kalem-satir", satir.ClassList);
        // ürün seçici satırın İLK alanıdır (css kuralı ilk alana en az 12rem verir) ve "Kalemi sil" aynı satırdadır
        Assert.NotNull(satir.QuerySelector(":scope > .alan:first-child #vz-f-urun-0"));
        Assert.NotNull(satir.QuerySelector(":scope > #vz-f-kalem-sil-0"));
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tuval-satis.css");
        Assert.Matches(new Regex(@"#vz-form \.vz-kalem-satir > \.alan:first-child \{ flex: 2 1 16rem; min-width: 12rem; \}"), css);
        Assert.Matches(new Regex(@"#vz-form \.vz-kalem-satir \{ display: flex; flex-wrap: wrap; align-items: flex-end;"), css);
        var form = e.Find("#vz-form").TextContent;
        if (basit) Assert.DoesNotContain("sunucu", form, StringComparison.OrdinalIgnoreCase);
        else Assert.Contains(Sozlukler.Getir("tr").Vezne.KurTahsilAninda, form);   // PRO metni (HUB) aynen
    }

    [Fact]
    public void Vezne_kuyruk_dar_ekranda_yigin_kart_ve_hucre_etiketleri()
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => false);
        var e = Render<Kuyumcu.Web.Pages.Vezne>();
        e.WaitForElement("#vz-satir-v1");
        Assert.Contains("q10-yigin", e.Find("#vz-liste").ClassList);
        var hucreler = e.FindAll("#vz-satir-v1 > td");
        Assert.Equal(7, hucreler.Count);
        Assert.All(hucreler.Take(6), h => Assert.False(string.IsNullOrWhiteSpace(h.GetAttribute("data-etiket"))));
        Assert.Contains("q10-yigin-eylem", hucreler[6].ClassList);
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tuval-satis.css");
        Assert.Matches(new Regex(@"#vz-liste > tbody > tr > td:last-child \{ grid-column: 1 / -1;"), css);
        Assert.Matches(new Regex(@"#vz-liste > tbody > tr > td \{ height: auto;"), css);
    }
}

[Collection("http")]
public sealed class KesifP5HttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private async Task ListeAc(Kiraci k)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/fiyat/listeler",
            new { ad = "Perakende", tur = "PERAKENDE", hasMarjYuzde = 5m, iscilikCarpanYuzde = 100m, varsayilan = true });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
    }

    [Fact]
    public async Task Toplu_teklif_TEKLI_teklifle_AYNI_fiyat_sinirlar_400_kiraci_yalitimi()
    {
        var k = YeniKiraci("PAKETKESIF-P5-HTTP toplu teklif");
        var baska = YeniKiraci("PAKETKESIF-P5-HTTP başka kiracı");
        await ListeAc(k);
        var tezgah = f.Ortam.Istemci(k, "TEZGAHTAR");

        var tekli = await (await tezgah.GetAsync($"/api/v1/fiyat/teklif?urunId={k.UrunId}&kurSnapshotId={k.KurId}")).Json();
        Assert.True(tekli.GetProperty("teklifVar").GetBoolean());
        var r = await tezgah.GetAsync($"/api/v1/fiyat/teklifler?urunIds={k.UrunId},{baska.UrunId}&kurSnapshotId={k.KurId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var satirlar = (await r.Json()).GetProperty("satirlar");
        Assert.Equal(1, satirlar.GetArrayLength());   // başka kiracının ürünü satır ÜRETMEZ
        var s = satirlar[0];
        Assert.Equal(k.UrunId, s.GetProperty("urunId").GetString());
        Assert.True(s.GetProperty("teklifVar").GetBoolean());
        Assert.Equal(tekli.GetProperty("satisFiyati").GetDecimal(), s.GetProperty("satisFiyati").GetDecimal());

        var bos = await tezgah.GetAsync($"/api/v1/fiyat/teklifler?urunIds=&kurSnapshotId={k.KurId}");
        Assert.Equal(HttpStatusCode.BadRequest, bos.StatusCode);
        var cok = string.Join(',', Enumerable.Range(0, Kuyumcu.Api.Modules.Fiyat.FiyatTopluTeklifController.EnFazlaUrun + 1).Select(i => $"x{i}"));
        Assert.Equal(HttpStatusCode.BadRequest, (await tezgah.GetAsync($"/api/v1/fiyat/teklifler?urunIds={cok}&kurSnapshotId={k.KurId}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await tezgah.GetAsync($"/api/v1/fiyat/teklifler?urunIds={k.UrunId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await tezgah.GetAsync($"/api/v1/fiyat/teklifler?urunIds={k.UrunId}&kurSnapshotId={baska.KurId}")).StatusCode);
    }

    [Fact]
    public async Task Toplu_teklif_liste_YOKSA_satir_teklifVar_false_sebep_LISTE_YOK()
    {
        var k = YeniKiraci("PAKETKESIF-P5-HTTP liste yok");
        var r = await f.Ortam.Istemci(k, "SAHIP").GetAsync($"/api/v1/fiyat/teklifler?urunIds={k.UrunId}&kurSnapshotId={k.KurId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var s = (await r.Json()).GetProperty("satirlar")[0];
        Assert.False(s.GetProperty("teklifVar").GetBoolean());
        Assert.Equal("LISTE_YOK", s.GetProperty("sebep").GetString());
        Assert.Equal(JsonValueKind.Null, s.GetProperty("satisFiyati").ValueKind);
    }

    [Fact]
    public async Task Vezne_subesiz_kullanici_mesaji_ekran_adiyla_ve_yapilacak_isle()
    {
        var k = YeniKiraci("PAKETKESIF-P5-HTTP vezne şubesiz");
        var r = await f.Ortam.Istemci(k.KullaniciId, k.TenantId, null, "MUDUR").GetAsync("/api/v1/vezne/kuyruk");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var m = (await r.Json()).GetProperty("message").GetString()!;
        Assert.StartsWith("Kasa kuyruğu", m);
        Assert.DoesNotContain("Vezne", m);
    }
}
