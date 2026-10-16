using System.Globalization;
using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET SATIS-2 — SATIŞ EKRANI = TUVAL + PUSULA SADELİĞİ (CUMA-S birleşik)
//
// Kurucu 25.09: "eski kayıtlar direkt açık gelmesin, piyano gibi açılır kapanır olsun" ·
// "daha basit olamaz mı, Pusula'da böyle mi" · TEMEL KURAL 11:35 "zorunlu olanlar hariç
// ilk etapta kullanıcı görmesin; isterse ekler — açılır kapanır seçenek olarak kalsın".
//
// Sınanan:
//   • Katlanir: varsayılan KAPALI (gövde DOM'da yok), aria-expanded, ilk açılışta BİR KEZ yükler
//   • Son satışlar: açılışta liste İSTENMEZ; açınca bir kez istenir; BASİT'te "Düzelt (ters kayıt)"
//     YOK, PRO'da kilitli + sebep; kırılım ayrı satır DEĞİL (eylem hücresinde ikon)
//   • BASİT değişmezlik ipucu tek kısa satır, jargonsuz
//   • PRO üst satır: Müşteri + tek satır özet (Bugün · TL · satıcı · liste), kutular katlı;
//     hata varken açık
//   • PRO sağ sütun: yasal kapılar uygunken tek satır, aşılınca/yaklaşınca kart; müşteri kartı yalnız
//     gerçek müşteride; kısayollar tek satır
//   • PRO ödeme: katman kapalıyken DOM'da gizli, özet tıklanınca açılır; Kaydet gövdesi (kasa/ödeme) DEĞİŞMEDİ
//   • "+ Ayrıntı ekle" içinde reyon · açıklama · fiş iskontosu
//
// ⚠ Sabit para metni YOK (Bicim.Tl ile üretilir).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SATIS2EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private decimal tevsikHad = 50_000m;
    private bool tevsikDevrede;
    private bool listeHata;
    private bool sonSatisHata;
    private const string SonSatisUcu = "/islemler/iade-adaylari?tur=SATIS&limit=20";

    public SATIS2EkranTestleri()
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

    private sealed class SahteApi(SATIS2EkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            if (yol.Contains("/fiyat/listeler") && t.listeHata)
                return new(System.Net.HttpStatusCode.InternalServerError)
                { Content = new StringContent("""{"message":"sunucu hatası"}""", System.Text.Encoding.UTF8, "application/json") };
            if (yol.Contains("/islemler/iade-adaylari") && t.sonSatisHata)
                return new(System.Net.HttpStatusCode.InternalServerError)
                { Content = new StringContent("""{"message":"liste okunamadı"}""", System.Text.Encoding.UTF8, "application/json") };
            var h = t.tevsikHad.ToString(CultureInfo.InvariantCulture);
            var bugun = DateTime.UtcNow.ToString("o");
            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") =>
                    """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/cari/c1/bakiye") => """{"cariId":"c1","tlBakiye":38900,"hasGramBakiye":5.66,"sonIslemTarihi":"2026-09-04T10:00:00"}""",
                var v when v.Contains("/tevsik/gunluk") =>
                    $$"""{"had":{{h}},"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":{{h}},"islemTutariKuraliDevrede":{{(t.tevsikDevrede ? "true" : "false")}}}""",
                var v when v.Contains("/fiyat/listeler") =>
                    """[{"id":"fl1","ad":"Vitrin","varsayilan":true,"aktif":true}]""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":3.94,"iscilikTl":0,"satisFiyati":10000,"gramAgirlik":4.3,"milyem":916,"barkod":"BLZ-00412","kod":"K-1"}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Hasan Kaya","tlBakiye":38900,"hasGramBakiye":5.66}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":200000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":200000,"ekBelgeEsigi":200000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/islemler/personeller") => """{"secimSerbest":false,"oturumId":"u1","personeller":[{"id":"u1","ad":"Test","soyad":"Tezgâhtar"}],"dahaFazlasiVar":false}""",
                var v when v.Contains("/islemler/iade-adaylari") =>
                    $$"""[{"id":"h1","tarih":"{{bugun}}","tlTutari":12400,"urunId":"u1","urunAdi":"Burma Bilezik","toplamAdet":1,"iadeEdilenAdet":0,"kalanAdet":1,"cariId":"pm1"},{"id":"h2","tarih":"{{bugun}}","tlTutari":3800,"urunId":"u1","urunAdi":"Burma Bilezik","toplamAdet":1,"iadeEdilenAdet":0,"kalanAdet":1,"cariId":"c1"}]""",
                var v when v.Contains("/islemler/fis/") => """{"fisId":"h1","kapsam":"KAYITLI","odemeKalemleri":[{"arac":"NAKIT","tutarTl":12400,"paraBirimi":"TRY","geriDoldurulmus":false}]}""",
                var v when v.Contains("/islemler/satis") => """{"id":"01J00000000000000000000001","fisId":"01J00000000000000000000002","tur":"SATIS","tlTutari":10000}""",
                var v when v.Contains("/reyon") => """[{"id":"r1","ad":"Vitrin reyonu","subeId":"s1","aktif":true}]""",
                var v when v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod = ArayuzModuServisi.Profesyonel)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-kaydet");
        e.WaitForAssertion(() => Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        return e;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> KalemliPro()
    {
        var e = Ekran();
        SeciciYardim.Sec(e, "islem-urun", "u1");
        // Fiyat SUNUCU teklifinden dolar; biçim (binlik ayırıcı) başka şeridin konusu — değerin GELDİĞİ beklenir.
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForElement("#islem-kalem-pro");
        return e;
    }

    private int SonSatisIstekSayisi() { lock (istekler) return istekler.Count(i => i.Metot == "GET" && i.Yol.Contains(SonSatisUcu)); }
    private static SatisSadeMetinleri SS => SatisSadeSozlugu.Tr;

    // ═══ KATLANIR (ortak bileşen) ═════════════════════════════════════════════

    [Fact]
    public void Katlanir_varsayilan_KAPALI_govde_DOMda_yok_aria_expanded_false()
    {
        var c = Render<Katlanir>(p => p.Add(x => x.Id, "k1").Add(x => x.Baslik, "Eski kayıtlar")
            .Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p id=\"k1-ic\">iç</p>"))));
        var dugme = c.Find("#k1-ac");
        Assert.Equal("false", dugme.GetAttribute("aria-expanded"));
        Assert.Equal("k1-govde", dugme.GetAttribute("aria-controls"));
        Assert.Equal("button", dugme.LocalName);
        Assert.Empty(c.FindAll("#k1-govde"));
        Assert.Empty(c.FindAll("#k1-ic"));
        Assert.False(c.Instance.Acik);
    }

    [Fact]
    public void Katlanir_acilinca_IlkAcilis_BIR_KEZ_cagrilir_ikinci_acilista_istek_yok()
    {
        var sayac = 0; var durumlar = new List<bool>();
        var c = Render<Katlanir>(p => p.Add(x => x.Id, "k2").Add(x => x.Baslik, "Son satışlar").Add(x => x.Sayi, 7)
            .Add(x => x.IlkAcilis, () => { sayac++; })
            .Add(x => x.Degisti, (bool a) => durumlar.Add(a))
            .Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p id=\"k2-ic\">iç</p>"))));
        Assert.Equal(0, sayac);                      // kapalı kart için istek YOK
        Assert.Equal("7", c.Find("#k2-sayi").TextContent.Trim());
        c.Find("#k2-ac").Click();
        Assert.Equal(1, sayac);
        Assert.Equal("true", c.Find("#k2-ac").GetAttribute("aria-expanded"));
        Assert.NotNull(c.Find("#k2-govde #k2-ic"));
        c.Find("#k2-ac").Click();
        Assert.Empty(c.FindAll("#k2-govde"));
        c.Find("#k2-ac").Click();
        Assert.Equal(1, sayac);                      // sonraki aç/kapa istek atmaz
        Assert.Equal([true, false, true], durumlar);
    }

    [Fact]
    public void Katlanir_Sayi_bilinmiyorsa_rozet_CIZILMEZ()
    {
        var c = Render<Katlanir>(p => p.Add(x => x.Id, "k3").Add(x => x.Baslik, "Eski kayıtlar"));
        Assert.Empty(c.FindAll("#k3-sayi"));
    }

    // ═══ SON SATIŞLAR = KATLANIR ══════════════════════════════════════════════

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Acilista_son_satislar_ISTENMEZ_kart_KAPALI_acinca_BIR_KEZ_yuklenir(string mod)
    {
        var e = Ekran(mod);
        Assert.Equal(0, SonSatisIstekSayisi());
        Assert.Equal("false", e.Find("#islem-son-satislar-ac").GetAttribute("aria-expanded"));
        Assert.Empty(e.FindAll("#islem-son-satislar-govde"));
        e.Find("#islem-son-satislar-ac").Click();
        e.WaitForAssertion(() => Assert.Equal(1, SonSatisIstekSayisi()));
        e.WaitForElement("#islem-son-satislar-govde");
        e.Find("#islem-son-satislar-ac").Click();
        e.Find("#islem-son-satislar-ac").Click();
        Assert.Equal(1, SonSatisIstekSayisi());
    }

    [Fact]
    public void BASIT_Duzelt_ters_kayit_YOK_PRO_kilitli_ve_sebebi_title()
    {
        var b = Ekran(ArayuzModuServisi.Basit);
        b.Find("#islem-son-satislar-ac").Click();
        b.WaitForElement("#islem-son-liste");
        Assert.Empty(b.FindAll("[id^=islem-storno-]"));
        Assert.DoesNotContain(Sozlukler.Tr.Duzeltme.StornoDugme, b.Find("#islem-son-satislar").TextContent);
        b.Dispose();

        var p = Ekran(ArayuzModuServisi.Profesyonel);
        p.Find("#islem-son-satislar-ac").Click();
        p.WaitForElement("#islem-storno-h1");
        var d = p.Find("#islem-storno-h1");
        Assert.True(d.HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(d.GetAttribute("title")));   // UX-S30
    }

    [Fact]
    public void Kirilim_AYRI_SATIR_DEGIL_eylem_hucresinde_ikon_tiklaninca_satirin_altinda()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        e.Find("#islem-son-satislar-ac").Click();
        e.WaitForElement("#islem-kirilim-h1");
        var dugme = e.Find("#islem-kirilim-h1");
        Assert.Same(dugme.Closest("td"), e.Find("#islem-iade-et-h1").Closest("td"));   // aynı eylem hücresi
        Assert.Equal("false", dugme.GetAttribute("aria-expanded"));
        Assert.Equal(Sozlukler.Tr.Islem.OdemeKirilimiGoster, dugme.GetAttribute("title"));
        Assert.Empty(e.FindAll(".islem-kirilim-satir"));
        // kırılım düğmesi içeren "yalnız düğme" satırı YOK
        Assert.DoesNotContain(e.FindAll("#islem-son-satislar tbody tr"), tr => tr.TextContent.Contains(Sozlukler.Tr.Islem.OdemeKirilimiGoster));
        dugme.Click();
        e.WaitForAssertion(() => Assert.Equal("true", e.Find("#islem-kirilim-h1").GetAttribute("aria-expanded")));
        e.WaitForElement(".islem-kirilim-satir");
        lock (istekler) Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("/islemler/fis/h1"));
    }

    [Fact]
    public void BASIT_degismezlik_TEK_KISA_satir_jargonsuz_ayrinti_katli()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        e.Find("#islem-son-satislar-ac").Click();
        e.WaitForElement("#islem-son-basit-ipucu");
        var ipucu = e.Find("#islem-son-basit-ipucu").TextContent.Trim();
        Assert.Equal(SS.BasitDegismez, ipucu);
        Assert.DoesNotContain("storno", ipucu, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cari", ipucu, StringComparison.OrdinalIgnoreCase);
        Assert.False(e.Find("#islem-son-neden").HasAttribute("open"));
        Assert.NotNull(e.Find("#islem-son-neden #degismez-ipucu-SatisFisi"));   // ayrıntı SİLİNMEDİ, katlı
    }

    // ═══ PRO ÜST SATIR ÖZETİ (kurucu 11:35) ═══════════════════════════════════

    [Fact]
    public void PRO_ust_satir_Musteri_ve_TEK_SATIR_ozet_kutular_KATLI_tiklaninca_acilir()
    {
        var e = Ekran();
        // TOPLU-7A (kararsız test): özet metni müşteri/vitrin yüklemesi bittikçe tamamlanır — tek anlık Assert yerine TÜM özet
        // iddiaları WaitForAssertion içinde (önce yalnız "Vitrin" bekleniyordu, "Test T." henüz yazılmamış olabiliyordu).
        e.WaitForAssertion(() =>
        {
            var ozet = e.Find("#islem-ust-ozet").TextContent;
            Assert.Contains("Vitrin", ozet);
            Assert.Contains(SS.Bugun, ozet);
            Assert.Contains(SatisTuvalSozlugu.Tr.ParaTl, ozet);
            Assert.Contains("Test T.", ozet);
        });
        Assert.Equal("false", e.Find("#islem-ust-ozet").GetAttribute("aria-expanded"));
        Assert.True(e.Find("#islem-ust-ayrinti").HasAttribute("hidden"));
        Assert.NotNull(e.Find("#islem-ust-ayrinti #islem-tarih"));             // id'ler AYNEN, katlı
        Assert.NotNull(e.Find("#islem-ust-ayrinti #islem-fiyat-listesi"));
        e.Find("#islem-ust-ozet").Click();
        e.WaitForAssertion(() =>
        {
            Assert.False(e.Find("#islem-ust-ayrinti").HasAttribute("hidden"));
            Assert.Equal("true", e.Find("#islem-ust-ozet").GetAttribute("aria-expanded"));
        });
    }

    [Fact]
    public void PRO_ust_ozet_fiyat_listesi_okunamazsa_ACIK_gelir_hata_gizlenmez()
    {
        listeHata = true;
        var e = Ekran();
        e.WaitForElement("#islem-fiyat-listesi-hata");
        Assert.False(e.Find("#islem-ust-ayrinti").HasAttribute("hidden"));
    }

    [Fact]
    public void Kisa_ad_soyadin_bas_harfi()
    {
        Assert.Equal("Ayşe Y.", Kuyumcu.Web.Pages.Islemler.KisaAd("Ayşe", "yılmaz"));
        Assert.Equal("Ertuğ", Kuyumcu.Web.Pages.Islemler.KisaAd("Ertuğ", null));
    }

    // ═══ PRO SAĞ SÜTUN ════════════════════════════════════════════════════════

    [Fact]
    public void PRO_yasal_kapilar_uygunken_TEK_satir_asilinca_KART()
    {
        var e = Ekran();
        e.WaitForElement("#islem-pro-yasal-uygun");
        Assert.Equal(SS.YasalUygun, e.Find("#islem-pro-yasal-uygun").TextContent.Trim());
        Assert.Empty(e.FindAll("#islem-pro-yasal-baslik"));
        Assert.False(e.Find("#islem-pro-yasal-ayrinti").HasAttribute("open"));
        Assert.NotNull(e.Find("#islem-pro-yasal-ayrinti #islem-pro-nakit"));   // satırlar SİLİNMEDİ, katlı
        e.Dispose();

        tevsikDevrede = true;
        var a = Ekran();
        a.WaitForElement("#islem-pro-yasal-baslik");
        Assert.Empty(a.FindAll("#islem-pro-yasal-uygun"));
        Assert.Equal(SatisProSozlugu.Tr.Asildi, a.Find("#islem-pro-nakit").TextContent.Trim());
    }

    [Fact]
    public void PRO_yasal_kart_esige_YAKLASINCA_acik_gelir_esik_SUNUCUDAN()
    {
        tevsikHad = 12_000m;   // fiş 10.000 ≥ 12.000 × YaklasmaOrani
        Assert.True(10_000m >= tevsikHad * Kuyumcu.Web.Bilesenler.Fis.FisProYanSutun.YaklasmaOrani);
        var e = KalemliPro();
        e.WaitForElement("#islem-pro-yasal-baslik");
        Assert.Equal(SatisProSozlugu.Tr.Uygun, e.Find("#islem-pro-nakit").TextContent.Trim());
    }

    [Fact]
    public void PRO_musteri_karti_Pesinde_YOK_gercek_musteride_VAR_kisayollar_TEK_satir()
    {
        var e = Ekran();
        Assert.Empty(e.FindAll("#islem-pro-musteri"));
        var k = e.Find("#islem-pro-kisayollar");
        Assert.Equal("p", k.LocalName);
        Assert.Equal(["F2", "F4", "F8", "F9"], k.QuerySelectorAll("kbd").Select(x => x.TextContent.Trim()).ToArray());
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.WaitForElement("#islem-pro-musteri");
        e.WaitForElement("#islem-pro-karti-ac");
    }

    [Fact]
    public void PRO_sag_sutunda_tasarim_notu_ve_bilgi_kutusu_YOK()
    {
        var e = Ekran();
        Assert.Empty(e.FindAll("#islem-pro-not"));
        Assert.Empty(e.FindAll(".fis-pro-yan-bilgi"));
    }

    // ═══ PRO ÖDEME ÖZETİ / TEK BAĞLANTI / AYRINTI ═════════════════════════════

    [Fact]
    public void PRO_odeme_katmani_KAPALIYKEN_gizli_ozet_tiklaninca_acilir_Kaydet_govdesi_DEGISMEDI()
    {
        var e = KalemliPro();
        var katman = e.Find("#islem-pro-odeme");
        Assert.True(katman.HasAttribute("hidden"));
        Assert.NotNull(katman.QuerySelector("#islem-odeme"));                  // alan DOM'da, görünmez
        e.Find("#islem-pro-odeme-degistir").Click();
        Assert.False(e.Find("#islem-pro-odeme").HasAttribute("hidden"));
        Assert.Equal("true", e.Find("#islem-pro-odeme-degistir").GetAttribute("aria-expanded"));
        e.Find("#islem-pro-odeme-tamam").Click();
        Assert.True(e.Find("#islem-pro-odeme").HasAttribute("hidden"));

        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => { lock (istekler) Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")); });
        string g; lock (istekler) g = istekler.Last(i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")).Govde;
        var kok = JsonDocument.Parse(g).RootElement;
        // tek kalem /islemler/satis (odemeTuru + kasaId) ya da fiş ucu /islemler/satis-fisi (odemeler[0]) — ikisinde de
        // katmanda GİZLİ duran değerler (Nakit + tek kasa) gövdeye AYNEN gider.
        var (arac, kasa) = kok.TryGetProperty("odemeler", out var od)
            ? (od[0].GetProperty("arac").GetString(), od[0].GetProperty("kasaId").GetString())
            : (kok.GetProperty("odemeTuru").GetString(), kok.GetProperty("kasaId").GetString());
        Assert.Equal("NAKIT", arac);
        Assert.Equal("ks1", kasa);
    }

    [Fact]
    public void PRO_Ayrinti_ekle_KATLI_icinde_reyon_aciklama_fis_iskontosu()
    {
        var e = KalemliPro();
        var d = e.Find("#islem-diger-ayrinti");
        Assert.False(d.HasAttribute("open"));
        Assert.Equal(SS.DigerAyrintilar, e.Find("#islem-diger-ayrinti-ac").TextContent.Trim());
        Assert.NotNull(d.QuerySelector("#islem-aciklama"));
        Assert.NotNull(d.QuerySelector("#islem-reyon"));
        Assert.NotNull(d.QuerySelector("#islem-iskonto-onizleme"));
        Assert.Empty(e.FindAll("#islem-kalem-pro ~ #islem-iskonto-onizleme"));
    }

    [Fact]
    public void BASIT_Aciklama_KATLI_tarih_para_YOK_odeme_bol_yerine_Baska_odeme_ekle()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        Assert.Empty(e.FindAll("#islem-tarih"));
        Assert.NotNull(e.Find("#islem-diger-ayrinti #islem-aciklama"));          // alt çubuktaki "+ Ayrıntı ekle" içinde
        Assert.False(e.Find("#islem-diger-ayrinti").HasAttribute("open"));        // KATLI — ilk bakışta alan değil
        Assert.Equal(SS.BaskaOdemeEkle, e.Find("#islem-odeme-bol").TextContent.Trim());
    }

    [Fact]
    public void KAYNAK2_aciklama_metinleri_sade_yuzde_GORSEL_GIZLI_bilgi_SILINMEDI()
    {
        var e = Ekran();
        Assert.Contains("fis-not-gizli", e.Find("#islem-cari-pasif-notu").ClassName);
        Assert.Contains("fis-not-gizli", e.Find("#islem-pesin-notu").ClassName);
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#islem-pesin-notu").GetAttribute("title")));
        Assert.Empty(e.FindAll("#islem-git-odeme, #islem-git-odeme-kilit"));   // SATIS-3 t11: ucu olmayan Ödeme karosu kaldırıldı
    }

    [Fact]
    public void Sozluk_TR_EN_bos_anahtar_yok_basit_metinde_jargon_yok()
    {
        foreach (var m in new[] { SatisSadeSozlugu.Tr, SatisSadeSozlugu.En })
            foreach (var p in typeof(SatisSadeMetinleri).GetProperties())
                Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(m)), p.Name);
        Assert.DoesNotContain("storno", SatisSadeSozlugu.Tr.BasitDegismez, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cari", SatisSadeSozlugu.Tr.BasitDegismez, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"\d", SatisSadeSozlugu.Tr.YasalUygun);   // eşik sayısı sözlükte YOK
    }
    // ═══ r2 — doğrulayıcı bulguları (25.09) ═══════════════════════════════════

    [Fact]
    public void Katlanir_HATALI_yuklemeden_sonra_yeniden_acilista_IlkAcilis_TEKRAR_cagrilir()
    {
        var sayac = 0;
        var c = Render<Katlanir>(p => p.Add(x => x.Id, "k4").Add(x => x.Baslik, "Son satışlar")
            .Add(x => x.Hatali, true).Add(x => x.IlkAcilis, () => { sayac++; }));
        c.Find("#k4-ac").Click();
        Assert.Equal(1, sayac);
        c.Find("#k4-ac").Click();                     // kapat
        c.Find("#k4-ac").Click();                     // yeniden aç → hata varken TEKRAR yükler
        Assert.Equal(2, sayac);
        c.Render(p => p.Add(x => x.Hatali, false));   // yükleme başarılı oldu
        c.Find("#k4-ac").Click();
        c.Find("#k4-ac").Click();
        Assert.Equal(2, sayac);                       // hata yokken yine BİR KEZ kuralı
    }

    [Fact]
    public void BASIT_son_satis_HATASINDA_hata_bandi_ve_Tekrar_dene_istek_atar_kapat_ac_da_yeniden_dener()
    {
        sonSatisHata = true;
        var e = Ekran(ArayuzModuServisi.Basit);
        e.Find("#islem-son-satislar-ac").Click();
        e.WaitForElement("#islem-son-hata", TimeSpan.FromSeconds(5));
        Assert.Empty(e.FindAll("#islem-son-bos"));                        // hata "kayıt yok"a çevrilmez (UX-S28)
        e.WaitForAssertion(() => Assert.Equal(SS.TekrarDene, e.Find("#islem-son-tekrar").TextContent.Trim()), TimeSpan.FromSeconds(5));
        Assert.Equal(1, SonSatisIstekSayisi());
        e.WaitForAssertion(() => e.Find("#islem-son-tekrar").Click());   // yeniden çizimde bayat öğe tutulmaz
        e.WaitForAssertion(() => Assert.Equal(2, SonSatisIstekSayisi()), TimeSpan.FromSeconds(5));
        e.Find("#islem-son-satislar-ac").Click();                          // kapat
        e.Find("#islem-son-satislar-ac").Click();                          // aç → hata sürüyorsa yeniden dener
        e.WaitForAssertion(() => Assert.Equal(3, SonSatisIstekSayisi()), TimeSpan.FromSeconds(5));
        sonSatisHata = false;
        e.WaitForAssertion(() => e.Find("#islem-son-tekrar").Click());
        e.WaitForElement("#islem-son-liste", TimeSpan.FromSeconds(5));
        Assert.Empty(e.FindAll("#islem-son-tekrar"));
        Assert.Empty(e.FindAll("#islem-son-hata"));
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> KalemliBasit()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#islem-kalem-tablo tbody tr")));
        return e;
    }

    [Fact]
    public void BASIT_nakit_uyarisi_BOS_fiste_ve_esigin_uzaginda_GIZLI_metin_DOMda_kalir()
    {
        tevsikHad = 50_000m;
        var e = Ekran(ArayuzModuServisi.Basit);
        e.WaitForAssertion(() => Assert.Equal(string.Format(SatisTuvalSozlugu.Tr.NakitUyari, Bicim.Tl(50_000m)),
                                              e.Find("#islem-nakit-uyari").TextContent.Trim()));
        Assert.True(e.Find("#islem-nakit-uyari").HasAttribute("hidden"));          // sürekli durmaz
        Assert.Equal("0", e.Find("#islem-nakit-uyari").GetAttribute("data-yakin"));
    }

    [Fact]
    public void BASIT_nakit_uyarisi_fis_toplami_esige_YAKLASINCA_gorunur()
    {
        tevsikHad = 12_000m;                                  // teklif 10.000 ≥ 12.000 × YaklasmaOrani
        var e = KalemliBasit();
        e.WaitForAssertion(() => Assert.Equal("1", e.Find("#islem-nakit-uyari").GetAttribute("data-yakin")));
        Assert.False(e.Find("#islem-nakit-uyari").HasAttribute("hidden"));
    }

    [Fact]
    public void Nakit_uyarisi_esik_BILINMIYORSA_tutucu_GORUNUR()
    {
        var c = Render<Kuyumcu.Web.Bilesenler.Fis.FisNakitUyari>(p => p.Add(x => x.Had, (decimal?)null).Add(x => x.Toplam, 0m));
        Assert.False(c.Find("#islem-nakit-uyari").HasAttribute("hidden"));
        var d = Render<Kuyumcu.Web.Bilesenler.Fis.FisNakitUyari>(p => p.Add(x => x.Had, 30_000m).Add(x => x.Toplam, 23_999m));
        Assert.True(d.Find("#islem-nakit-uyari").HasAttribute("hidden"));
        var f = Render<Kuyumcu.Web.Bilesenler.Fis.FisNakitUyari>(p => p.Add(x => x.Had, 30_000m).Add(x => x.Toplam, 24_000m));
        Assert.False(f.Find("#islem-nakit-uyari").HasAttribute("hidden"));
    }
}
