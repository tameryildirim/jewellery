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

    // TEMA TEMİZLİĞİ 17.10: SATIŞ fişi artık hep Yıldız tezgâhıdır (BASİT ve PRO). Son satışlar kartı (#islem-son-satislar)
    // Yıldız'da BİLEREK görünmez (tema-yildiz.css: .yz-islem > #islem-son-satislar ve .yz-islem-tur > #islem-son-satislar
    // display:none — tuvalde yok); PRO üst özet satırı, sağ sütun kartları ve alt çubuk ödeme özeti de Yıldız'da yok.
    // Güvenceler Yıldız yolunda: satıcı/fiyat listesi "Değiştir" katmanının ekinde, yasal durum Fark notunda, ödeme
    // alanları tek ödeme satırına dokununca açılan düzende, reyon/açıklama kalem satırının düzenlemesinde, fiş iskontosu
    // "+ İndirim" hapında.
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod = ArayuzModuServisi.Profesyonel)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, mod));
        return Render<Kuyumcu.Web.Pages.Islemler>().YildizHazir();
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> KalemliPro()
    {
        var e = Ekran();
        e.YildizKalemEkle();   // fiyat SUNUCU teklifinden dolar; biçim başka şeridin konusu — değerin GELDİĞİ beklenir
        return e;
    }

    private static string YasalNot(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) =>
        e.FindAll("#yildiz-tezgah-not").FirstOrDefault()?.TextContent.Trim() ?? "";

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

    // TEMA TEMİZLİĞİ 17.10: Acilista_son_satislar_ISTENMEZ_kart_KAPALI_acinca_BIR_KEZ_yuklenir (BASIT/PRO),
    // BASIT_Duzelt_ters_kayit_YOK_PRO_kilitli_ve_sebebi_title, Kirilim_AYRI_SATIR_DEGIL_eylem_hucresinde_ikon_tiklaninca_satirin_altinda
    // ve BASIT_degismezlik_TEK_KISA_satir_jargonsuz_ayrinti_katli kaldırıldı — son satışlar kartı Yıldız'da BİLEREK görünmez
    // (tema-yildiz.css display:none, tuvalde yok); kartın aç/kapa, storno, kırılım ve değişmezlik ipucu yolları kullanıcıya
    // ulaşmaz. Katlanır bileşenin kendisi yukarıdaki Katlanir_* testlerinde AYNEN; "açılışta liste istenmez" güvencesi aşağıda.

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void TemaGC_Yildiz_acilista_son_satislar_listesi_ISTENMEZ(string mod)
    {
        var e = Ekran(mod);
        e.YildizKalemEkle();   // sayfa tam yüklendi, kullanıcı çalışıyor
        Assert.Equal(0, SonSatisIstekSayisi());
    }

    // ═══ PRO SATICI / FİYAT LİSTESİ (Yıldız: "Değiştir" katmanının eki) ══════════

    [Fact]
    public void PRO_ust_satir_Musteri_ve_TEK_SATIR_ozet_kutular_KATLI_tiklaninca_acilir()
    {
        // Yıldız: eski tek satır özetin (Bugün · TL · satıcı · liste) yerinde katmanın "+ Satıcı ve fiyat listesi" eki;
        // kutular KATLI gelir, tıklanınca açılır (id'ler AYNEN). Tarih/Para kutuları Yıldız'da bilerek yok (UX-S21).
        var e = Ekran();
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForElement("#yildiz-musteri-secici");
        Assert.Equal("false", e.Find("#yildiz-musteri-pro-ac").GetAttribute("aria-expanded"));
        Assert.Empty(e.FindAll("#yildiz-musteri-pro, #islem-fiyat-listesi"));
        e.Find("#yildiz-musteri-pro-ac").Click();
        e.WaitForAssertion(() => Assert.Equal("true", e.Find("#yildiz-musteri-pro-ac").GetAttribute("aria-expanded")));
        e.WaitForAssertion(() =>
        {
            Assert.Contains("Vitrin", e.Find("#yildiz-musteri-pro #islem-fiyat-listesi").TextContent);
            Assert.Contains("Test Tezgâhtar", e.Find("#yildiz-musteri-pro #islem-satan").TextContent);
        });
        Assert.Empty(e.FindAll("#islem-tarih, #islem-para"));
    }

    [Fact]
    public void PRO_ust_ozet_fiyat_listesi_okunamazsa_ACIK_gelir_hata_gizlenmez()
    {
        listeHata = true;
        var e = Ekran();
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForElement("#yildiz-musteri-pro #islem-fiyat-listesi-hata");   // ek KENDİLİĞİNDEN açık
        Assert.Equal("true", e.Find("#yildiz-musteri-pro-ac").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Kisa_ad_soyadin_bas_harfi()
    {
        Assert.Equal("Ayşe Y.", Kuyumcu.Web.Pages.Islemler.KisaAd("Ayşe", "yılmaz"));
        Assert.Equal("Ertuğ", Kuyumcu.Web.Pages.Islemler.KisaAd("Ertuğ", null));
    }

    // ═══ PRO YASAL DURUM (Yıldız: Fark notu — eski sağ sütun kartı yok) ═════════

    /// <summary>
    /// Uygunken TEK satır ("Yasal kurallar uygun · nakit &lt;sunucunun haddi&gt; sınırının altında"); sunucu kuralı devrede
    /// derse "uygun" DENMEZ. (GC bulgusu giderildi: sunucunun "kural devrede"
    /// bilgisi Fark notuna yansır — TS2TuvalProTestleri.Yasal_kapilar_sunucu_devrede_derse_Asildi.)
    /// </summary>
    [Fact]
    public void PRO_yasal_kapilar_uygunken_TEK_satir_asilinca_KART()
    {
        var e = Ekran();
        e.WaitForAssertion(() => Assert.Equal(
            $"{YildizTezgahSozlugu.YasalUygun("tr")} · {YildizTezgahSozlugu.NakitSinirAlti(Bicim.Tl(50_000m), "tr")}", YasalNot(e)));
        Assert.DoesNotContain("yz-kotu", e.Find("#yildiz-tezgah-not").ClassList);
        Assert.Empty(e.FindAll("#islem-pro-yasal-baslik, #islem-pro-yasal-uygun"));   // eski kart Yıldız'da yok
        e.Dispose();

        tevsikDevrede = true;
        var a = KalemliPro();
        a.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(YasalNot(a))), TimeSpan.FromSeconds(5));
        Assert.DoesNotContain(YildizTezgahSozlugu.YasalUygun("tr"), YasalNot(a));
    }

    [Fact]
    public void PRO_yasal_kart_esige_YAKLASINCA_acik_gelir_esik_SUNUCUDAN()
    {
        // Yıldız: had Fark notunda her zaman yazılı (eşiğe yaklaşınca ayrıca açılan kart yok); sayı SUNUCUDAN.
        tevsikHad = 12_000m;   // fiş 10.000 ≥ 12.000 × YaklasmaOrani
        Assert.True(10_000m >= tevsikHad * Kuyumcu.Web.Bilesenler.Fis.FisProYanSutun.YaklasmaOrani);
        var e = KalemliPro();
        e.WaitForAssertion(() => Assert.Equal(
            $"{YildizTezgahSozlugu.YasalUygun("tr")} · {YildizTezgahSozlugu.NakitSinirAlti(Bicim.Tl(12_000m), "tr")}", YasalNot(e)));
    }

    // TEMA TEMİZLİĞİ 17.10: PRO_musteri_karti_Pesinde_YOK_gercek_musteride_VAR_kisayollar_TEK_satir kaldırıldı — sağ sütunun
    // müşteri kartı (#islem-pro-musteri, "Kartı aç") ve kısayollar satırı (#islem-pro-kisayollar) Yıldız'da BİLEREK yok
    // (FisProYanSutun Yıldız satışında çizilmez; kısayollar CSS ile gizli — PTezgahTurKimlikTestleri.TK_13).

    [Fact]
    public void PRO_sag_sutunda_tasarim_notu_ve_bilgi_kutusu_YOK()
    {
        var e = Ekran();
        Assert.Single(e.FindAll("#yildiz-tezgah"));   // tezgâh çizili — boş DOM değil
        Assert.Empty(e.FindAll("#islem-pro-not"));
        Assert.Empty(e.FindAll(".fis-pro-yan-bilgi"));
    }

    // ═══ PRO ÖDEME DÜZENİ / AYRINTI ═════════════════════════════════════════════

    [Fact]
    public void PRO_odeme_katmani_KAPALIYKEN_gizli_ozet_tiklaninca_acilir_Kaydet_govdesi_DEGISMEDI()
    {
        // Yıldız: ödeme alanları tek ödeme satırına dokununca açılır, "Tamam" kapatır; kapalıyken de değerler (Nakit +
        // tek kasa) sayfa durumundadır ve gövdeye AYNEN gider.
        var e = KalemliPro();
        Assert.Empty(e.FindAll("#yildiz-tezgah-odeme-duzen, #islem-odeme"));
        Assert.Equal("false", e.Find("#yildiz-odeme-0").GetAttribute("aria-expanded"));
        e.Find("#yildiz-odeme-0").Click();
        e.WaitForElement("#yildiz-tezgah-odeme-duzen #islem-odeme");
        Assert.Equal("true", e.Find("#yildiz-odeme-0").GetAttribute("aria-expanded"));
        e.Find("#yildiz-tezgah-odeme-tamam").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#yildiz-tezgah-odeme-duzen")));

        e.YildizKaydet();
        e.WaitForAssertion(() => { lock (istekler) Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")); });
        string g; lock (istekler) g = istekler.Last(i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")).Govde;
        var kok = JsonDocument.Parse(g).RootElement;
        // tek kalem /islemler/satis (odemeTuru + kasaId) ya da fiş ucu /islemler/satis-fisi (odemeler[0]) — ikisinde de
        // düzende GİZLİ duran değerler (Nakit + tek kasa) gövdeye AYNEN gider.
        var (arac, kasa) = kok.TryGetProperty("odemeler", out var od)
            ? (od[0].GetProperty("arac").GetString(), od[0].GetProperty("kasaId").GetString())
            : (kok.GetProperty("odemeTuru").GetString(), kok.GetProperty("kasaId").GetString());
        Assert.Equal("NAKIT", arac);
        Assert.Equal("ks1", kasa);
    }

    [Fact]
    public void PRO_Ayrinti_ekle_KATLI_icinde_reyon_aciklama_fis_iskontosu()
    {
        // Yıldız: reyon + açıklama kalem satırına dokununca açılan düzenlemede, fiş iskontosu "+ İndirim" hapında —
        // ilk bakışta üçü de KATLI (özellik silinmedi).
        var e = KalemliPro();
        Assert.Empty(e.FindAll("#islem-aciklama, #islem-reyon, #islem-fis-iskonto"));
        e.Find("#yildiz-kalem-0").Click();
        e.WaitForElement("#yildiz-kalem-duzen-0");
        e.WaitForAssertion(() =>
        {
            Assert.NotNull(e.Find("#yildiz-kalem-duzen-0 #islem-aciklama"));
            Assert.NotNull(e.Find("#yildiz-kalem-duzen-0 #islem-reyon"));
        });
        e.Find("#yildiz-tezgah-indirim").Click();
        e.WaitForElement("#yildiz-tezgah-indirim-alan #islem-fis-iskonto");
        Assert.Single(e.FindAll("#islem-fis-iskonto"));   // fiş iskontosu TEK yerde
    }

    [Fact]
    public void BASIT_Aciklama_KATLI_tarih_para_YOK_odeme_bol_yerine_Baska_odeme_ekle()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        Assert.Empty(e.FindAll("#islem-tarih, #islem-para"));
        Assert.Empty(e.FindAll("#islem-aciklama"));                               // KATLI — ilk bakışta alan değil
        Assert.Equal(YildizTezgahSozlugu.BaskaOdeme("tr"), e.Find("#yildiz-tezgah-baska-odeme").TextContent.Trim());
        e.YildizKalemEkle();
        e.Find("#yildiz-kalem-0").Click();
        e.WaitForElement("#yildiz-kalem-duzen-0 #islem-aciklama");                // özellik silinmedi: satırın düzenlemesinde
    }

    // TEMA TEMİZLİĞİ 17.10: KAYNAK2_aciklama_metinleri_sade_yuzde_GORSEL_GIZLI_bilgi_SILINMEDI kaldırıldı — sınadığı notlar
    // (#islem-cari-pasif-notu, #islem-pesin-notu) eski fiş üst satırına (FisMusteriSatiri) aitti; Yıldız satışında müşteri
    // seçimi "Değiştir" katmanında (YildizMusteriSecici) ve o notlar BİLEREK yok (Yildiz4CTestleri.Y4C_13).

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

    // TEMA TEMİZLİĞİ 17.10: BASIT_son_satis_HATASINDA_hata_bandi_ve_Tekrar_dene_istek_atar_kapat_ac_da_yeniden_dener kaldırıldı —
    // son satışlar kartı Yıldız'da BİLEREK görünmez (yukarıdaki not); Katlanir'ın hatalı yüklemede yeniden deneme kuralı
    // Katlanir_HATALI_yuklemeden_sonra_yeniden_acilista_IlkAcilis_TEKRAR_cagrilir testinde AYNEN.
    //
    // TEMA TEMİZLİĞİ 17.10: BASIT_nakit_uyarisi_BOS_fiste_ve_esigin_uzaginda_GIZLI_metin_DOMda_kalir ve
    // BASIT_nakit_uyarisi_fis_toplami_esige_YAKLASINCA_gorunur kaldırıldı — FisNakitUyari kutusu (#islem-nakit-uyari-kutu)
    // Yıldız'da BİLEREK gizli (tema-yildiz.css display:none); Yıldız'ın nakit sınırı cümlesi Fark notunda HER ZAMAN yazılı
    // (sayı sunucudan): TS1TuvalSatisTestleri.Nakit_uyarisi_esigi_SUNUCUDAN_okur_istemcide_rakam_yok ve
    // PRO_yasal_kart_esige_YAKLASINCA_acik_gelir_esik_SUNUCUDAN. Kutunun gizle/göster kuralı aşağıdaki bileşen testinde AYNEN.

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
