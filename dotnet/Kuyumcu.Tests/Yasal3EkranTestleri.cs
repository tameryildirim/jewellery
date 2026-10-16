using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-3 — EKRAN TARAFI (bUnit)
//   § 4.2 tezgâh: PESIN_ART_ARDA_SORU → tek soru; "Hayır" aynı fişi beyanla yeniden gönderir.
//   § 4.4 tezgâh: YAPTIRIM_ENGEL → kırmızı bant (sözlük cümlesi) + Kaydet kilitli.
//   Panel bantları (K-12), Uyum → saklama bekletmesi ve yasal liste bileşenleri.
// Her düğme için: tıklama → gerçek istek (gövdesiyle) ya da disabled + sebep.
// ⚠ Görünürlük/sığma/taşma bUnit ile KANITLANMAZ — gerçek tarayıcı ölçümü rapordadır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Yasal3EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private int satisSayaci;
    private string satisIlkRed = "";   // "" = red yok; aksi hâlde ilk POST bu gövdeyle 400
    private string uyumOzeti = """{"saklamaYaklasan":0,"saklamaGun":30,"saklamaKuru":true,"listeTarihi":"2026-09-30","listeEski":false,"listeYasiGun":1,"acikTam":0,"acikBenzerlik":0}""";
    private int uyumKodu = 200;
    private string bekletmeler = """{"kuru":true,"bekletmeler":[{"id":"b1","kapsam":"TUMU","kimlikTespitiId":null,"baslangic":"2026-09-30T09:00:00Z","bitisGunu":"2026-12-31","gerekce":"Vergi incelemesi sürüyor, saklanmalı","olusturuldu":"2026-09-30T09:00:00Z","kaldirildi":null,"aktif":true}],"maskelemeler":[{"tarih":"2026-09-29T02:00:00Z","otomatik":true,"ozet":"saklama süresi maskelemesi (otomatik): 3 tespit, 1 beyan"}]}""";
    private string yaptirim = """{"listeTarihi":"2026-09-30","kaynak":"6415 s. K. md.5 listesi","kaynakUrl":"https://masak.hmb.gov.tr/bkk-ile-malvarliklari-dondurulanlar","listeYasiGun":1,"listeEski":false,"eskiListeGun":14,"yururlukteSatir":12,"acikTam":1,"acikBenzerlik":1,"eslesmeler":[{"id":"e1","tur":"TAM","nokta":"MASAK_TESPIT","cariId":"c1","cariAd":"Ayşe Demir","durum":"ACIK","bildirimSonGunu":"2026-10-01","masakBildirimTarihi":null,"masakEvrakNo":null,"not":null,"olusturuldu":"2026-09-30T10:00:00Z"},{"id":"e2","tur":"AD_BENZERLIGI","nokta":"CARI","cariId":"c2","cariAd":"Ali Kaya","durum":"ACIK","bildirimSonGunu":null,"masakBildirimTarihi":null,"masakEvrakNo":null,"not":null,"olusturuldu":"2026-09-30T11:00:00Z"}]}""";

    private const string CariListesi = """
        [{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":null,"email":null,
          "tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false,"sistemCarisi":false,"aktif":true},
         {"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","telefon":null,"email":null,
          "tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false,"sistemCarisi":true,"aktif":true}]
        """;

    public Yasal3EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(Yasal3EkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add((istek.Method.Method, yol, govde));
            var kod = 200;
            string y;
            if (istek.Method == HttpMethod.Post && yol.Contains("/islemler/satis"))
            {
                t.satisSayaci++;
                if (t.satisSayaci == 1 && t.satisIlkRed != "") { kod = 400; y = t.satisIlkRed; }
                else y = """{"id":"h1","fisId":"f1"}""";
            }
            else y = yol switch
            {
                var v when v.Contains("/masak/uyum-ozet") => (kod = t.uyumKodu) == 200 ? t.uyumOzeti : """{"message":"sunucu hatası"}""",
                var v when v.Contains("/masak/saklama-bekletmeleri") => t.bekletmeler,
                var v when v.Contains("/kaldir") => """{"id":"b1","kapsam":"TUMU","kimlikTespitiId":null,"baslangic":"2026-09-30T09:00:00Z","bitisGunu":"2026-12-31","gerekce":"Vergi incelemesi sürüyor, saklanmalı","olusturuldu":"2026-09-30T09:00:00Z","kaldirildi":"2026-10-01T09:00:00Z","aktif":false}""",
                var v when v.Contains("/masak/saklama-bekletme") => """{"id":"b2","kapsam":"TUMU","kimlikTespitiId":null,"baslangic":"2026-10-01T09:00:00Z","bitisGunu":"2026-11-30","gerekce":"Mahkeme süreci devam ediyor, saklanmalı","olusturuldu":"2026-10-01T09:00:00Z","kaldirildi":null,"aktif":true}""",
                var v when v.Contains("/masak/yaptirim-eslesmeleri") => """{"id":"e1","durum":"BILDIRILDI","masakBildirimTarihi":"2026-10-01","masakEvrakNo":"M-1","not":null}""",
                var v when v.Contains("/masak/yaptirim") => t.yaptirim,
                var v when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/cari") && !v.Contains("/cari/") => CariListesi,
                var v when v.Contains("/tevsik/gunluk") => """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/reyonlar") || v.Contains("/islemler/son") || v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage((System.Net.HttpStatusCode)kod)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private void Oturum(string rol, string? mod = null) =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1", null, mod ?? ArayuzModuServisi.Profesyonel));

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Pos()
    {
        Oturum("TEZGAHTAR");
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-tur-SATIS");
        e.WaitForElement("#islem-pesin-notu");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        return e;
    }

    private List<JsonElement> SatisGovdeleri() =>
        [.. istekler.Where(i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")).Select(i => JsonDocument.Parse(i.Govde).RootElement)];

    // ── § 4.2 tezgâh ──

    [Fact]
    public void Pesin_art_arda_soru_Hayir_ayni_fisi_beyanla_yeniden_gonderir()
    {
        satisIlkRed = """{"message":"Az önce bu kasada Peşin Müşteri ile işlem yapıldı. Bu, aynı müşteri mi?","alanlar":{"ayniKisiBeyani":["x"]},"kod":"PESIN_ART_ARDA_SORU","error":"Bad Request","statusCode":400}""";
        var e = Pos();
        e.Find("#islem-kaydet").Click();
        e.WaitForElement("#pesin-soru");
        Assert.Equal(Yasal3Sozlugu.Tr.SoruMetin, e.Find("#pesin-soru-metin").TextContent.Trim());
        Assert.Empty(e.FindAll("#hata-bandi"));   // soru, hata bandının YERİNE
        Assert.Contains("Dayanak", e.Find("#pesin-soru-neden summary").GetAttribute("title"), StringComparison.Ordinal);
        foreach (var kod in new[] { "5/1", "md.", "Yönetmeli" })
            Assert.DoesNotContain(kod, e.Find("#pesin-soru").TextContent, StringComparison.Ordinal);
        Assert.False(SatisGovdeleri()[0].TryGetProperty("masak", out var m0) && m0.ValueKind == JsonValueKind.Object);

        e.Find("#pesin-soru-hayir").Click();
        e.WaitForAssertion(() => Assert.Equal(2, SatisGovdeleri().Count));
        var ikinci = SatisGovdeleri()[1];
        Assert.Equal("pm1", ikinci.GetProperty("cariId").GetString());
        Assert.False(ikinci.GetProperty("masak").GetProperty("ayniKisiBeyani").GetBoolean());
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#pesin-soru")));
    }

    [Fact]
    public void Pesin_art_arda_soru_Evet_istek_atmadan_musteri_secimine_doner()
    {
        satisIlkRed = """{"message":"Az önce…","alanlar":{"ayniKisiBeyani":["x"]},"kod":"PESIN_ART_ARDA_SORU","statusCode":400}""";
        var e = Pos();
        e.Find("#islem-kaydet").Click();
        e.WaitForElement("#pesin-soru");
        var once = istekler.Count(i => i.Metot == "POST");
        e.Find("#pesin-soru-evet").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#pesin-soru")));
        Assert.Equal(once, istekler.Count(i => i.Metot == "POST"));
    }

    // ── § 4.4 tezgâh ──

    [Fact]
    public void Yaptirim_engeli_kirmizi_bant_ve_Kaydet_kilitli_sebepli()
    {
        satisIlkRed = """{"message":"Bu kişiyle işlem yapılamaz (yasal liste). Yöneticinize haber verin.","alanlar":{"cariId":["x"]},"kod":"YAPTIRIM_ENGEL","statusCode":400}""";
        var e = Pos();
        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(Yasal3Sozlugu.Tr.EngelMetin, e.Find("#hata-bandi").TextContent.Trim()));
        e.WaitForAssertion(() => Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled")));
        Assert.Contains(Yasal3Sozlugu.Tr.EngelKilit, e.Markup, StringComparison.Ordinal);   // UX-S30: sebep yazılı
    }

    // ── Panel bantları (K-12) ──

    [Fact]
    public void K12_Panel_liste_15_gun_eski_sari_bant_13_gun_yok()
    {
        uyumOzeti = uyumOzeti.Replace("\"listeEski\":false,\"listeYasiGun\":1", "\"listeEski\":true,\"listeYasiGun\":15");
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Bilesenler.Uyum.UyumYasalBantlari>();
        e.WaitForElement("#uyb-liste");
        Assert.Contains("15 gündür", e.Find("#uyb-liste").TextContent, StringComparison.Ordinal);
        Assert.Contains("bant-uyari", e.Find("#uyb-liste").GetAttribute("class"), StringComparison.Ordinal);
    }

    [Fact]
    public void K12_Panel_liste_13_gun_bant_yok()
    {
        uyumOzeti = uyumOzeti.Replace("\"listeYasiGun\":1", "\"listeYasiGun\":13");
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Bilesenler.Uyum.UyumYasalBantlari>();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/masak/uyum-ozet")));
        Assert.Empty(e.FindAll("#uyb-liste"));
    }

    [Fact]
    public void Panel_saklama_mavi_katlanir_ve_eslesme_kirmizi()
    {
        uyumOzeti = """{"saklamaYaklasan":3,"saklamaGun":30,"saklamaKuru":false,"listeTarihi":"2026-09-30","listeEski":false,"listeYasiGun":1,"acikTam":1,"acikBenzerlik":1}""";
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Bilesenler.Uyum.UyumYasalBantlari>();
        e.WaitForElement("#uyb-saklama");
        Assert.Equal("details", e.Find("#uyb-saklama").TagName.ToLowerInvariant());   // katlanır
        Assert.Contains("3 kaydın", e.Find("#uyb-saklama-ozet").TextContent, StringComparison.Ordinal);
        Assert.Contains("uyari-kirmizi", e.Find("#uyb-eslesme").GetAttribute("class"), StringComparison.Ordinal);
        Assert.Contains("2", e.Find("#uyb-eslesme").TextContent, StringComparison.Ordinal);
        Assert.Equal("/uyum?sekme=saklama", e.Find("#uyb-saklama-git").GetAttribute("href"));
    }

    [Fact]
    public void Panel_TEZGAHTAR_istek_atmaz_hata_yutulmaz()
    {
        Oturum("TEZGAHTAR");
        var t = Render<Kuyumcu.Web.Bilesenler.Uyum.UyumYasalBantlari>();
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/masak/uyum-ozet"));
        Assert.Empty(t.FindAll("[id^=uyb]"));

        uyumKodu = 500;
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", null, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Bilesenler.Uyum.UyumYasalBantlari>();
        e.WaitForElement("#uyb-hata");
    }

    // ── Uyum → Saklama bekletmesi ──

    [Fact]
    public void Bekletme_yeni_POST_duzenle_PATCH_kaldir_onayla_POST()
    {
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Bilesenler.Uyum.SaklamaBekletmePaneli>();
        e.WaitForElement("#skb-tablo");
        Assert.NotNull(e.Find("#skb-kuru"));
        Assert.Empty(e.FindAll("#skb-gecmis-liste"));   // geçmiş KAPALI katlanır

        // Yeni: boş formda Kaydet kilitli ve sebep yazılı.
        e.Find("#skb-yeni").Click();
        e.WaitForElement("#skb-bitis");
        e.Find("#skb-bitis").Change(DateTime.Today.AddDays(40).ToString("yyyy-MM-dd"));
        e.Find("#skb-gerekce").Input("Mahkeme süreci devam ediyor, saklanmalı");
        e.FindAll("button").First(b => b.Id?.EndsWith("-kaydet") == true && b.Id.Contains("saklama-bekletme")).Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/masak/saklama-bekletme")));
        var g = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST" && i.Yol.EndsWith("/masak/saklama-bekletme")).Govde).RootElement;
        Assert.Equal("TUMU", g.GetProperty("kapsam").GetString());
        Assert.Equal(DateTime.Today.AddDays(40).ToString("yyyy-MM-dd"), g.GetProperty("bitis").GetString());
        e.WaitForAssertion(() => Assert.Contains("30.11.2026", e.Find("#skb-basari").TextContent, StringComparison.Ordinal));

        // Düzenle → PATCH
        e.Find("#skb-duzenle-b1").Click();
        e.WaitForElement("#skb-bitis");
        e.FindAll("button").First(b => b.Id?.EndsWith("-kaydet") == true && b.Id.Contains("saklama-bekletme")).Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/masak/saklama-bekletme/b1")));

        // Kaldır → onay kipi → POST /kaldir
        e.Find("#skb-kaldir-b1").Click();
        e.WaitForElement("#skb-kaldir-onayla");
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/kaldir"));
        e.Find("#skb-kaldir-onayla").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/masak/saklama-bekletme/b1/kaldir")));
    }

    [Fact]
    public void Bekletme_bos_formda_Kaydet_kilitli_sebepli()
    {
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Bilesenler.Uyum.SaklamaBekletmePaneli>();
        e.WaitForElement("#skb-yeni");
        e.Find("#skb-yeni").Click();
        e.WaitForElement("#skb-bitis");
        var kaydet = e.FindAll("button").First(b => b.Id?.EndsWith("-kaydet") == true && b.Id.Contains("saklama-bekletme"));
        Assert.True(kaydet.HasAttribute("disabled"));
        Assert.Contains(Yasal3Sozlugu.Tr.BekletmeBitis, e.Markup, StringComparison.Ordinal);
    }

    // ── Uyum → Yasal liste ──

    [Fact]
    public void Yasal_liste_tarih_kaynak_eslesmeler_ve_kapatma_PATCH()
    {
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Bilesenler.Uyum.YasalListePaneli>();
        e.WaitForElement("#ylp-tablo");
        Assert.Equal("30.09.2026", e.Find("#ylp-tarih").TextContent.Trim());
        Assert.Equal("https://masak.hmb.gov.tr/bkk-ile-malvarliklari-dondurulanlar", e.Find("#ylp-kaynak").GetAttribute("href"));
        Assert.Contains("01.10.2026", e.Find("#ylp-songun-e1").TextContent, StringComparison.Ordinal);
        Assert.NotNull(e.Find("#degismez-ipucu-YaptirimEslesmesi"));   // kilit elle açılmaz — gerekçe
        Assert.Empty(e.FindAll("#ylp-degil-e1"));                      // TAM eşleşme "aynı kişi değil" yapılamaz

        e.Find("#ylp-kapat-e1").Click();
        e.WaitForElement("#ylp-evrak");
        e.Find("#ylp-evrak").Input("MASAK-2026-77");
        e.FindAll("button").First(b => b.Id?.EndsWith("-kaydet") == true && b.Id.Contains("yaptirim-eslesmesi")).Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/masak/yaptirim-eslesmeleri/e1")));
        var g = JsonDocument.Parse(istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
        Assert.Equal("BILDIRILDI", g.GetProperty("durum").GetString());
        Assert.Equal("MASAK-2026-77", g.GetProperty("masakEvrakNo").GetString());

        // Ad benzerliği: "Aynı kişi değil" → onay kipi → PATCH
        e.Find("#ylp-degil-e2").Click();
        e.WaitForElement("#ylp-degil-onayla");
        e.Find("#ylp-degil-onayla").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/masak/yaptirim-eslesmeleri/e2")));
    }

    // ── Tur 2: VARLIK_YOK durumu ve eşik bölme sinyali ──

    private const string YaptirimTur2 = """{"listeTarihi":"2026-09-30","kaynak":"6415 s. K. md.5 listesi","kaynakUrl":"https://masak.hmb.gov.tr/bkk-ile-malvarliklari-dondurulanlar","listeYasiGun":1,"listeEski":false,"eskiListeGun":14,"yururlukteSatir":12,"acikTam":0,"acikBenzerlik":0,"eslesmeler":[{"id":"e3","tur":"TAM","nokta":"LISTE_GUNCELLEME","cariId":"c3","cariAd":"Veli Can","durum":"VARLIK_YOK","bildirimSonGunu":null,"masakBildirimTarihi":null,"masakEvrakNo":null,"not":null,"olusturuldu":"2026-09-30T10:00:00Z"}],"esikBolmeSinyali":{"gun":30,"adet":2,"sinir":20,"son":[{"tarih":"2026-09-30T12:00:00Z","kullanici":"Ayşe"},{"tarih":"2026-09-29T12:00:00Z","kullanici":null}]}}""";

    [Fact]
    public void Tur2_varliksiz_eslesme_bildirildi_yazmaz_kapat_dugmesi_yok_sinyal_sayisi_ve_kayitlari()
    {
        yaptirim = YaptirimTur2;
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Bilesenler.Uyum.YasalListePaneli>();
        e.WaitForElement("#ylp-satir-e3");
        var satir = e.Find("#ylp-satir-e3").TextContent;
        Assert.Contains(Yasal3Sozlugu.Tr.EslesmeVarlikYok, satir, StringComparison.Ordinal);
        Assert.DoesNotContain(Yasal3Sozlugu.Tr.EslesmeBildirildi, satir, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#ylp-kapat-e3"));

        Assert.Equal(string.Format(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"), Yasal3Sozlugu.Tr.SinyalSayi, 30, 2),
            e.Find("#ylp-sinyal").TextContent.Trim());
        // Kayıtlar kapalı katlanırda (geçmiş kayıt listeleri kapalı gelir); sınır başlıkta yazılı.
        Assert.Contains("20", e.Find("#ylp-sinyal-liste").TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#ylp-sinyal-yok"));
    }

    [Fact]
    public void Tur2_sinyal_yoksa_tek_sade_satir()
    {
        yaptirim = YaptirimTur2.Replace("\"adet\":2", "\"adet\":0", StringComparison.Ordinal).Replace("\"son\":[{\"tarih\":\"2026-09-30T12:00:00Z\",\"kullanici\":\"Ayşe\"},{\"tarih\":\"2026-09-29T12:00:00Z\",\"kullanici\":null}]", "\"son\":[]", StringComparison.Ordinal);
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Bilesenler.Uyum.YasalListePaneli>();
        e.WaitForElement("#ylp-sinyal-yok");
        Assert.Empty(e.FindAll("#ylp-sinyal"));
        Assert.Empty(e.FindAll("#ylp-sinyal-liste"));
    }
}
