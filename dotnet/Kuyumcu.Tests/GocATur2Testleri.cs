using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET GOC-A · TUR 2 — hasım doğrulayıcının (08.10) kalan bulguları:
//  [ORTA]   G1: sunucu sınırına değen listede (Banka hareketleri 100, Hurda defteri 100,
//           Gider pusulaları 100, Emanet son 200 hareket) istemci sıralaması YAPILMAZ —
//           sıralama düğmesi çıkmaz, sınır notu yazılır; tam listede yerel sıralama sürer.
//  [DÜŞÜK]  Servis kapısı uzunluk iletisi sözlükten (ekranla AYNI anahtar: AdUzun);
//           POS bilinmeyen durum iletisi iç kod (BEKLIYOR…) yerine sözlük etiketleri.
//  [KRİTİK] Kur "Manuel kur" çekmecesi: BASİT'te tek sade cümle; ham kod MANUAL hiçbir modda yok.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Saf: servis kapısı uzunluk iletisi sözlükten.</summary>
public sealed class GocATur2SafTestleri
{
    [Fact]
    public void GT01_Servis_uzunluk_iletisi_sozluk_AdUzun_ekranla_ayni_ciplak_metin_yok()
    {
        Assert.Equal("kısa", GirdiKapisi.Uzunluk("  kısa  ", 500, "aciklama"));
        var ex = Assert.Throws<BadRequestException>(() => GirdiKapisi.Uzunluk(new string('a', 501), 500, "aciklama"));
        Assert.Equal(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.AdUzun, 500), "tr"), ex.Message);
        Assert.DoesNotContain("karakter olabilir", ex.Message);
        // eski imza (etiketli) de AYNI sözlük iletisini verir — etiket iletiye girmez
        var ex2 = Assert.Throws<BadRequestException>(() => GirdiKapisi.Uzunluk(new string('a', 301), 300, "aciklama", "Kalem açıklaması"));
        Assert.Equal(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.AdUzun, 300), "tr"), ex2.Message);
    }

    [Fact]
    public void GT02_Yeni_sozluk_metinleri_TR_EN_dolu_ve_ham_kod_tasimaz()
    {
        foreach (var m in new[] { GocASozlugu.Tr, GocASozlugu.En })
        {
            Assert.False(string.IsNullOrWhiteSpace(m.SiralamaSinirNotu));
            Assert.Contains("{0}", m.SiralamaSinirNotu);
            Assert.Contains("{0}", m.PosDurumBilinmiyor);
            Assert.False(string.IsNullOrWhiteSpace(m.KurManuelSade));
        }
        Assert.DoesNotContain("MANUAL", Sozlukler.Tr.Kur.ManuelUyari);
        Assert.DoesNotContain("MANUAL", Sozlukler.En.Kur.ManuelUyari);
        Assert.DoesNotContain("denetim izi", Sozlukler.Tr.Kur.ManuelUyari);
    }
}

/// <summary>HTTP: POS durum iletisi etiketli · Emanet yanıtı sınır bayrağı taşır.</summary>
[Collection("http")]
public sealed class GocATur2SunucuTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETGOC-A-TEST t2 " + ad);
    }

    [Fact]
    public async Task GT03_POS_bilinmeyen_durum_400_ileti_sozluk_etiketleri_ic_kod_ve_ham_girdi_YOK()
    {
        var k = YeniKiraci("pos durum");
        var r = await f.Ortam.Istemci(k).GetAsync("/api/v1/pos/tahsilatlar?durum=XYZ");
        var govde = await r.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var j = JsonDocument.Parse(govde).RootElement;
        var ileti = j.GetProperty("alanlar").GetProperty("durum")[0].GetString()!;
        var p = Sozlukler.Tr.Pos;
        Assert.Equal(string.Format(GocASozlugu.Tr.PosDurumBilinmiyor,
            string.Join(", ", p.DurumBekliyor, p.DurumKismi, p.DurumYatti, p.DurumIptal)), ileti);
        foreach (var kod in new[] { "BEKLIYOR", "KISMI", "YATTI", "IPTAL", "XYZ" })
            Assert.DoesNotContain(kod, govde);
    }

    [Fact]
    public async Task GT04_Emanet_gecmisi_yaniti_sinir_bayragi_tasir_kisa_listede_TAM()
    {
        var k = YeniKiraci("emanet sinir");
        var r = await f.Ortam.Istemci(k).GetAsync($"/api/v1/emanet/{k.CariId}");
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.IsSuccessStatusCode, govde);
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.False(j.GetProperty("sinirDegdi").GetBoolean());
        Assert.Equal(200, j.GetProperty("limit").GetInt32());
    }
}

/// <summary>bUnit: G1 — sınıra değen listede sıralama düğmesi yok + sınır notu; Kur çekmecesi sade.</summary>
public sealed class GocATur2EkranTestleri : BunitContext
{
    private int hareketSayisi = 3;
    private int pusulaSayisi = 2;
    private bool hurdaDahaFazla;

    public GocATur2EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private void Kullanici(string mod) =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1", mod, mod));

    private string Hareketler() => "{\"hareketler\":[" + string.Join(",", Enumerable.Range(1, hareketSayisi).Select(i =>
        $$"""{"id":"bh{{i}}","yon":"GELEN","tutar":{{100 + i}},"karsiTarafAd":"Ali Veli","aciklama":"Dekont {{i}}","durum":"BEKLIYOR","tarih":"2026-09-{{(i % 28) + 1:00}}T10:00:00Z"}""")) + "]}";

    private string Pusulalar() => "[" + string.Join(",", Enumerable.Range(1, pusulaSayisi).Select(i =>
        $$"""
        {"id":"gp{{i}}","pusulaNo":"GP-2026-{{i:000000}}","hareketId":null,"saticiTipi":"NIHAI_TUKETICI",
         "saticiAdSoyad":"Tuna Akarsu","saticiTckn":"*******0146","ayarId":"K22","gramAgirlik":4.35,
         "hasGrami":3.9846,"brutTutar":{{1000 + i}},"stopajOran":0.0,"stopajTutar":0.00,
         "netOdenen":{{1000 + i}},"teslimTarihi":"2099-09-01T00:00:00Z",
         "duzenlemeSonTarihi":"2099-09-08T00:00:00Z","gecikti":false,"kalanGun":30,
         "iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null}
        """)) + "]";

    private string HurdaDefter() => $$"""
        {"satirlar":[
           {"id":"h1","tur":"HURDA_ALIS","tarih":"2026-09-20T08:00:00Z","tlTutari":-25648,
            "hasGrami":9.16,"maden":"ALTIN","madenAdi":"Altın","cariId":null,"aciklama":null,
            "paraBirimi":"TRY","stornoSatiri":false,"stornolandi":false,
            "tahlilKaynakHareketId":null,"tahlilFarkiHasGram":0},
           {"id":"h2","tur":"HURDA_ALIS","tarih":"2026-09-21T08:00:00Z","tlTutari":-1000,
            "hasGrami":0.5,"maden":"ALTIN","madenAdi":"Altın","cariId":null,"aciklama":null,
            "paraBirimi":"TRY","stornoSatiri":false,"stornolandi":false,
            "tahlilKaynakHareketId":null,"tahlilFarkiHasGram":0}],
         "toplamAdet":{{(hurdaDahaFazla ? 250 : 2)}},"limit":100,"dahaFazlasiVar":{{(hurdaDahaFazla ? "true" : "false")}},
         "sinirNotu":{{(hurdaDahaFazla ? "\"Yalnız son 100 hurda hareketi gösteriliyor (toplam 250).\"" : "null")}}}
        """;

    private sealed class Sahte(GocATur2EkranTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.Contains("/bankalar/") && y.EndsWith("/hareketler") => t.Hareketler(),
                var y when y.EndsWith("/bankalar") =>
                    """{"hesaplar":[{"id":"b1","bankaAdi":"Ziraat","hesapAdi":"Ana Hesap","hesapSahibi":"Deneme Ltd.","iban":"TR200000000000000000000001","paraBirimi":"TRY","bakiye":0,"aktif":true}]}""",
                var y when y.EndsWith("/hurda/bakiye") => """
                    {"subeId":"sb1",
                     "satirlar":[{"maden":"ALTIN","madenAdi":"Altın","hasGram":9.16,"girenHas":9.16,"cikanHas":0,"tahlilFarkiHas":0}],
                     "fireKaydiAcik":false,"fireNotu":null}
                    """,
                var y when y.EndsWith("/hurda/defter") => t.HurdaDefter(),
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez kasa","paraBirimi":"TRY","aktif":true}]""",
                var y when y.EndsWith("/gider-pusulalari/sure-takibi") => """
                    {"sure":{"gun":7,"dayanak":"VUK md.234"},"gecikmisSayisi":0,"yaklasanSayisi":0,
                     "gecikmis":[],"yaklasan":[],"uyarilar":[]}
                    """,
                var y when y.EndsWith("/gider-pusulalari") => t.Pusulalar(),
                var y when y.EndsWith("/yetkiler/benim") => """{"moduller":["cari","urun","banka","kasa","islem"]}""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":null,"usdTry":40,"eurTry":null,"gbpTry":null,"kaynak":"TCMB","olusturuldu":"2026-10-08T08:00:00Z","ayarFiyatlari":[]}""",
                var y when y.Contains("/kur/gecmis") => "[]",
                var y when y.Contains("/kur/pariteler") => """{"pariteler":[]}""",
                var y when y.Contains("/tercihler/benim") => """{"tercihler":{}}""",
                var y when y.Contains("/cari") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Banka> BankaHareketleri()
    {
        Kullanici("PROFESYONEL");
        var e = Render<Kuyumcu.Web.Pages.Banka>().AyrintiAc();
        e.WaitForElement("#bh-gecmis-ac");
        e.Find("#bh-gecmis-ac").Click();
        e.WaitForAssertion(() => Assert.Equal(hareketSayisi, e.FindAll("#bh-hareket-tablo tbody tr").Count));
        return e;
    }

    [Fact]
    public void GT05_Banka_hareketleri_sinirin_altinda_TAM_siralama_dugmesi_var_not_yok()
    {
        hareketSayisi = 3;
        var e = BankaHareketleri();
        Assert.NotEmpty(e.FindAll("#bh-hareket-sirala-tarih"));
        Assert.NotEmpty(e.FindAll("#bh-hareket-sirala-tutar"));
        Assert.Empty(e.FindAll("#bh-sinir-notu"));
    }

    [Fact]
    public void GT06_Banka_hareketleri_sunucu_sinirina_degdi_100_siralama_dugmesi_YOK_sinir_notu_var()
    {
        hareketSayisi = 100;
        var e = BankaHareketleri();
        Assert.Empty(e.FindAll("#bh-hareket-sirala-tarih"));
        Assert.Empty(e.FindAll("#bh-hareket-sirala-tutar"));
        Assert.Empty(e.FindAll("#bh-hareket-tablo .izgara-sirala"));
        Assert.Equal(string.Format(GocASozlugu.Tr.SiralamaSinirNotu, 100), e.Find("#bh-sinir-notu").TextContent.Trim());
    }

    [Fact]
    public void GT07_Gider_pusulalari_sinira_degen_listede_siralama_YOK_kisa_listede_var()
    {
        Kullanici("PROFESYONEL");
        pusulaSayisi = 2;
        var e = Render<Kuyumcu.Web.Pages.GiderPusulalari>().AyrintiAc();
        e.WaitForState(() => e.FindAll("#gp-tablo tbody tr").Count == 2);
        Assert.NotEmpty(e.FindAll("#gp-liste-sirala-net"));
        Assert.Empty(e.FindAll("#gp-sinir-notu"));
        e.Dispose();

        pusulaSayisi = 100;
        var e2 = Render<Kuyumcu.Web.Pages.GiderPusulalari>().AyrintiAc();
        e2.WaitForState(() => e2.FindAll("#gp-tablo tbody tr").Count == 100);
        Assert.Empty(e2.FindAll("#gp-tablo .izgara-sirala"));
        Assert.Equal(string.Format(GocASozlugu.Tr.SiralamaSinirNotu, 100), e2.Find("#gp-sinir-notu").TextContent.Trim());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GT09_Hurda_defteri_sunucu_dahaFazlasiVar_derse_siralama_YOK_tam_listede_var(bool dahaFazla)
    {
        hurdaDahaFazla = dahaFazla;
        Kullanici("PROFESYONEL");
        var e = Render<Kuyumcu.Web.Pages.Hurda>().AyrintiAc();
        e.WaitForElement("#hrd-gecmis-ac").Click();
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll("#hrd-defter tbody tr").Count));
        if (dahaFazla)
        {
            Assert.Empty(e.FindAll("#hrd-defter .izgara-sirala"));
            Assert.NotEmpty(e.FindAll("#hrd-sinir-notu"));
        }
        else
        {
            Assert.Equal(3, e.FindAll("#hrd-defter .izgara-sirala").Count);   // tarih · has · tutar
            Assert.Empty(e.FindAll("#hrd-sinir-notu"));
        }
    }

    [Theory]
    [InlineData("BASIT")]
    [InlineData("PROFESYONEL")]
    public void GT08_Kur_manuel_cekmecesi_BASIT_tek_sade_cumle_PRO_ayrintili_ham_kod_hicbir_modda_YOK(string mod)
    {
        Kullanici(mod);
        var e = Render<Kuyumcu.Web.Pages.Kur>().AyrintiAc();
        e.WaitForElement("#kur-manuel-ac");
        e.Find("#kur-manuel-ac").Click();
        e.WaitForElement("#m-uyari");
        var metin = e.Find("#m-uyari").TextContent.Trim();
        Assert.Equal(mod == "BASIT" ? GocASozlugu.Tr.KurManuelSade : Sozlukler.Tr.Kur.ManuelUyari, metin);
        Assert.DoesNotContain("MANUAL", metin);
        if (mod == "BASIT") Assert.DoesNotContain("denetim", metin, StringComparison.OrdinalIgnoreCase);
    }
}
