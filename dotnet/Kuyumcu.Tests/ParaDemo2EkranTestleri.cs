using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO-2 — ekran testleri (bUnit). Sahte API'ler "{}" varsayılanına BIRAKILMADI:
// yeni uç (/kasalar/nakit-ayrimi) ve değişen yanıt alanları (tahsilat.virmanGiris/acilisDevri/tanim,
// ekstre odemeAraclari) gerçekçi yanıtla; gövdeler JSON ayrıştırılarak sınanır.
// ═════════════════════════════════════════════════════════════════════════════

// ── (2) Günü Kapat · (4) kasa kartı ayrım satırı ─────────────────────────────
public sealed class ParaDemo2KasaEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public ParaDemo2KasaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            var get = istek.Method == HttpMethod.Get;
            return ParaDemoSahte.Tamam(yol switch
            {
                var v when v.Contains("/kur/guncel") => """{"id":"kur1","altinHasFiyati":4100}""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez Şube"}]""",
                var v when get && v.Contains("/kasalar/nakit-ayrimi") =>
                    """[{"kasaId":"k1","paraBirimi":"TRY","toplam":1800,"nakit":1000,"kart":500,"havale":300}]""",
                var v when get && v.Contains("/gun-kapanisi/onizleme") =>
                    """{"kasaId":"k1","paraBirimi":"TRY","toplamBakiye":1800,"kartToplam":500,"havaleToplam":300,"nakitDefter":1000}""",
                var v when get && v.Contains("/kasalar/") && v.Contains("/hareketler") => """{"hareketler":[]}""",
                var v when get && v.EndsWith("/kasalar") =>
                    """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":1800,"aktif":true,"subeId":"s1"}]""",
                var v when v.EndsWith("/gun-kapanisi") =>
                    """{"kasaId":"k1","yerelGun":"2026-10-02","sayilanNakit":1000,"defterBakiye":1000,"toplamBakiye":1800,"kartToplam":500,"havaleToplam":300,"fark":0,"aciklama":null,"kilitli":true}""",
                var v when v.Contains("/kasalar/") => """{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":1800,"aktif":true,"subeId":"s1"}""",
                _ => "{}",
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Kasalar> Ciz()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(new OturumKullanici(
            "u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1", ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        var e = Render<Kuyumcu.Web.Pages.Kasalar>();
        e.WaitForElement("#kasa-gun-kapat-k1");
        return e;
    }

    [Fact]
    public void PD2E_2_Gunu_Kapat_acilista_sayilan_BOS_fark_YOK_kilit_sebebi_TEK_ve_altlikta()
    {
        var e = Ciz();
        e.Find("#kasa-gun-kapat-k1").Click();
        e.WaitForElement("#gun-nakit-defter");

        Assert.Equal("", e.Find("#gun-sayilan").GetAttribute("value") ?? "");   // 0 değil, BOŞ
        Assert.Empty(e.FindAll("#gun-fark-satir"));                              // kırmızı sahte fark yok
        Assert.Empty(e.FindAll("#gun-aciklama"));
        var dugme = e.Find("#gun-kapat-onayla");
        Assert.True(dugme.HasAttribute("disabled"));
        Assert.NotNull(dugme.Closest(".cekmece-alt"));                           // birincil düğme altlıkta
        Assert.Equal(Kuyumcu.Core.I18n.Sozlukler.Tr.Kasa.GunKapat, dugme.TextContent.Trim());
        Assert.Equal(ParaDemo2Sozlugu.Tr.Vazgec, e.Find("#cekmece-kasa-gun-iptal").TextContent.Trim());
        Assert.Equal(ParaDemo2Sozlugu.Tr.GunSayimBekleniyor, e.Find("#gun-kapat-kilit").TextContent.Trim());
        // Sebep TEK kez: çekmecede aynı cümle ikinci kez yazılmaz.
        var govde = e.Find("#cekmece-kasa-gun").TextContent;
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(govde,
            System.Text.RegularExpressions.Regex.Escape(ParaDemo2Sozlugu.Tr.GunSayimBekleniyor)));
    }

    [Fact]
    public void PD2E_2_sayi_girilince_fark_gorunur_tutmuyorsa_gerekce_ister_sebep_TEK()
    {
        var e = Ciz();
        e.Find("#kasa-gun-kapat-k1").Click();
        e.WaitForElement("#gun-nakit-defter");
        e.Find("#gun-sayilan").Input("900");
        // SON-KALANLAR (hasım DÜŞÜK): fark YÖNÜYLE yazılır — "100 ₺ eksik" (işaretsiz kırmızı sayı değil).
        Assert.Equal(string.Format(SonKalanlarSozlugu.Tr.FarkEksik, Bicim.Tl(100m)), e.Find("#gun-fark").TextContent.Trim());
        Assert.NotNull(e.Find("#gun-aciklama"));
        Assert.True(e.Find("#gun-kapat-onayla").HasAttribute("disabled"));
        var sebep = Kuyumcu.Core.I18n.Sozlukler.Tr.Kasa.FarkGerekcesiZorunlu;
        Assert.Equal(sebep, e.Find("#gun-kapat-kilit").TextContent.Trim());
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(e.Find("#cekmece-kasa-gun").TextContent,
            System.Text.RegularExpressions.Regex.Escape(sebep)));

        e.Find("#gun-aciklama").Input("Akşam sayımında 100 ₺ eksik çıktı");
        Assert.False(e.Find("#gun-kapat-onayla").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#gun-kapat-kilit"));
        e.Find("#gun-kapat-onayla").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/kasalar/k1/gun-kapanisi")));
        var g = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST").Govde).RootElement;
        Assert.Equal(900m, g.GetProperty("sayilanNakit").GetDecimal());
    }

    [Fact]
    public void PD2E_4_kasa_kartinda_Nakit_ve_Kart_Havale_alt_satiri()
    {
        var e = Ciz();
        e.WaitForElement("#kasa-ayrim-k1");
        // P-DIL-BORC-KASA (Y8): büyük rakam çekmecedeki nakit; kart ve havale ADLARIYLA ayrı, toplam alt satırda.
        Assert.Equal(KasaNakitSozlugu.AyrimSatiri(Bicim.Para(500m, "TRY"), Bicim.Para(300m, "TRY"), Bicim.Para(1800m, "TRY"), "tr"),
            e.Find("#kasa-ayrim-k1").TextContent.Trim());
    }
}

// ── (4/5) Rapor merkezi: BASİT "Kasada ne var?" + gün sonu tek satır · PRO virman/açılış satırları ──
public sealed class ParaDemo2RaporEkranTestleri : BunitContext
{
    public ParaDemo2RaporEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            return Task.FromResult(ParaDemoSahte.Tamam(yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/aylik") => """{"yil":2026,"ay":10,"gunler":[]}""",
                var y when y.EndsWith("/raporlar/gunluk") => """{"tarih":"2026-10-02","satis":{"adet":3,"toplamTl":1800.00,"toplamHas":1.2},"alis":{"adet":0,"toplamTl":0,"toplamHas":0},"hurda":{"toplamHas":0,"odenenTl":0},"netHasGramHareketi":-1.2,"hareketSayisi":3,"kasalar":[{"id":"k1","ad":"Ana Kasa","bakiye":1800.00,"paraBirimi":"TRY"}]}""",
                var y when y.EndsWith("/kasalar/nakit-ayrimi") =>
                    """[{"kasaId":"k1","paraBirimi":"TRY","toplam":1800,"nakit":1000,"kart":500,"havale":300}]""",
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") => """
                    {"tarih":"2026-10-02",
                     "tahsilat":{"nakit":1000.00,"kart":500.00,"havale":300.00,"diger":0,"toplam":1800.00,
                                 "virmanGiris":250.00,"acilisDevri":700.00,"tanim":"Toplam tahsilat = o gün satış ve tahsilatla kasaya giren para."},
                     "satis":{"sayisi":3,"toplamTl":1800.00,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":3,"faturasizTl":1800.00},
                     "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
                     "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
                     "fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},
                     "fark":1800.00,"mutabik":false,"uyarilar":[],"faturasizIslemler":[],"faturaEslesme":null}
                    """,
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                var y when y.EndsWith("/pozisyon") => """{"anaMaden":"ALTIN","altin":{"maden":"ALTIN","vitrinGram":10,"hurdaGram":0,"cariAlacakGram":0,"cariBorcGram":0,"netGram":10,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},"digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,"hesaplanmaAni":"2026-10-02T09:00:00Z","donemdenBagimsiz":true}""",
                var y when y.Contains("/bildirimler") => """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""",
                _ => "{}",
            }));
        }
    }

    private void Oturum(string mod) => Services.GetRequiredService<OturumServisi>().KullaniciAta(
        new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));

    [Fact]
    public void PD2E_4_BASIT_gun_sonu_karti_tahsilat_kirilimi_TEK_satir_ve_kasa_ayrimi()
    {
        Oturum(ArayuzModuServisi.Basit);
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForElement("#bugun-tahsilat-kirilim");
        Assert.Equal(string.Format(ParaDemo2Sozlugu.Tr.TahsilatTekSatir, Bicim.TlKurus(1800m), Bicim.TlKurus(1000m), Bicim.TlKurus(500m), Bicim.TlKurus(300m)),
            e.Find("#bugun-tahsilat-kirilim").TextContent.Trim());
        e.WaitForElement("#bugun-kasa-ayrim");
        // PAKET RAPOR-4 (tasarım DÜŞÜK): Panel kasa kartıyla aynı düzen — iki AYRI öğe (satır sonunda tutar yalnız düşmez);
        // birleşik cümle ekran okuyucu etiketinde AYNEN durur.
        Assert.Equal(string.Format(ParaDemo2Sozlugu.Tr.KasaAyrimSatiri, Bicim.BelgePara(1000m, "TRY"), Bicim.BelgePara(800m, "TRY")),
            e.Find("#bugun-kasa-ayrim").GetAttribute("aria-label"));
        Assert.Contains(Bicim.BelgePara(1000m, "TRY"), e.Find("#bugun-kasa-nakit").TextContent);
        Assert.Contains(Bicim.BelgePara(800m, "TRY"), e.Find("#bugun-kasa-karthavale").TextContent);
    }

    [Fact]
    public void PD2E_5_PRO_gun_sonu_virman_ve_acilis_AYRI_satir_toplama_girmez_tanim_katlanirda()
    {
        Oturum(ArayuzModuServisi.Profesyonel);
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForElement("#rp-sekme-gunsonu");
        e.Find("#rp-sekme-gunsonu").Click();
        e.WaitForElement("#mut-virman-giris");
        Assert.Contains(ParaDemo2Sozlugu.Tr.VirmanGirisSatiri, e.Find("#mut-virman-giris").TextContent);
        Assert.Contains(Bicim.TlKurus(250m), e.Find("#mut-virman-giris").TextContent);
        Assert.Contains(Bicim.TlKurus(700m), e.Find("#mut-acilis-devri").TextContent);
        Assert.NotNull(e.Find("#mut-tahsilat-tanim"));
        // Tur 4: tanım cümlesi istemci sözlüğünden (TR+EN), API'nin gönderdiği metin GÖSTERİLMEZ.
        e.Find("#mut-tahsilat-tanim-ac").Click();
        var tanim = e.Find("#mut-tahsilat-tanim-metin").TextContent;
        Assert.Equal(ParaDemo2Sozlugu.Tr.TahsilatTanimMetni, tanim.Trim());
        // Toplam kart yine yalnız tahsilat (1.800), virman/açılış eklenmez.
        Assert.Contains(Bicim.TlKurus(1800m), e.Find("#rp-panel-gunsonu").TextContent);
        Assert.DoesNotContain(Bicim.TlKurus(2750m), e.Find("#rp-panel-gunsonu").TextContent);
    }
}

// ── (1/9) cari ekstresi · (6/7) tahsilat penceresi ───────────────────────────
public sealed class ParaDemo2CariEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string CariListesi = """
        [{"id":"c1","ad":"Kemal Ardıç","tur":"MUSTERI","telefon":null,"email":null,
          "tlBakiye":6424.89,"hasGramBakiye":-1.5,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false,"aktif":true}]
        """;

    // En yeniden eskiye: altınla tahsilat (has −1,5) · karma satış (Kart + Havale, peşin) · veresiye satış.
    private const string Ekstre = """
        {"bakiye":{"cariId":"c1","tlBakiye":6424.89,"hasGramBakiye":-1.5},
         "hareketler":[
           {"id":"h3","tur":"TAHSILAT","tlTutari":0,"hasGrami":1.5,"aciklama":null,"olusturuldu":"2026-10-02T10:03:00Z","odemeTuru":null,"cariTlEtkisi":0,"cariHasEtkisi":-1.5,"bakiyeHasEtkisi":-1.5,"hasBakiyesineGirer":true},
           {"id":"h2","tur":"SATIS","tlTutari":10000,"hasGrami":-1.5,"aciklama":null,"olusturuldu":"2026-10-02T10:02:00Z","odemeTuru":"KARMA","odemeAraclari":["KART","HAVALE"],"cariTlEtkisi":0,"cariHasEtkisi":0,"bakiyeHasEtkisi":0},
           {"id":"h1","tur":"SATIS","tlTutari":6424.89,"hasGrami":-0.7,"aciklama":null,"olusturuldu":"2026-10-02T10:01:00Z","odemeTuru":"VERESIYE","cariTlEtkisi":6424.89,"cariHasEtkisi":0,"bakiyeHasEtkisi":0}],
         "sayfaDisiHasEtkisi":0,"dahaFazlaVar":false,"limit":100,"bakiyeUyusmazligiHas":0,
         "bakiyeEmanetsizHas":-1.5,"emanetteHas":0,"bakiyeUyusmazligiTl":0}
        """;

    public ParaDemo2CariEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            return ParaDemoSahte.Tamam(yol switch
            {
                var y when y.EndsWith("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"gbpTry":null}""",
                var y when y.Contains("/ekstre") => Ekstre,
                var y when y.EndsWith("/tahsilat") => """{"id":"c1","ad":"Kemal Ardıç","tur":"MUSTERI","bakiye":{"cariId":"c1","tlBakiye":6424.89,"hasGramBakiye":-2.5}}""",
                var y when y.EndsWith("/cari/c1") => """{"id":"c1","ad":"Kemal Ardıç","tur":"MUSTERI","telefon":null,"email":null,"vergiNo":null,"adres":null,"pepBeyani":false}""",
                var y when y.EndsWith("/cari") => CariListesi,
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true}]""",
                var y when y.Contains("/tevsik/gunluk") => """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/listeler") => "[]",
                _ => "{}",
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Cariler> Ciz(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("#c-tahsilat-c1");
        istekler.Clear();
        return e;
    }

    [Fact]
    public void PD2E_7_tahsilat_guncel_borc_HEP_gorunur_istege_baglilar_KAPALI()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.Find("#c-tahsilat-c1").Click();
        e.WaitForElement("#th-guncel");
        Assert.Contains(string.Format(ParaDemo2Sozlugu.Tr.GuncelBorc, Bicim.Para(6424.89m, "TRY")), e.Find("#th-guncel").TextContent);
        Assert.Contains("1,5000", e.Find("#th-guncel-altin").TextContent);
        Assert.Empty(e.FindAll("#th-has"));         // "+ Altınla öde" arkasında
        Assert.Empty(e.FindAll("#th-aciklama"));    // "+ Not ekle" arkasında
        e.Find("#th-not-ekle").Click();
        Assert.NotNull(e.Find("#th-aciklama"));
        Assert.Empty(e.FindAll("#th-not-ekle"));
        // Tek kasa: kasa mesajı TEK satır (kasa adıyla).
        e.Find("#th-tutar").Input("100");
        e.WaitForElement("#th-kasa-tek");
        Assert.Equal(string.Format(ParaDemo2Sozlugu.Tr.KasaTekSatir, "Ana Kasa"), e.Find("#th-kasa-tek").TextContent.Trim());
    }

    [Fact]
    public void PD2E_6_has_uyarisi_BASIT_gercek_dugme_adi_tek_cumle_buyuk_harf_yok_baglanti()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.Find("#c-tahsilat-c1").Click();
        e.WaitForElement("#th-has-ekle").Click();
        e.WaitForElement("#th-has").Input("1");
        var uyari = e.WaitForElement("#th-has-uyari").TextContent;
        Assert.Contains("«" + SatisTuvalSozlugu.Tr.KaroAlisEskiAltin + "»", uyari);
        Assert.DoesNotContain("DÜŞÜRMEZ", uyari);
        Assert.Equal("/islemler?tur=HURDA_ALIS", e.Find("#th-has-yol").GetAttribute("href"));
        // Gram silinince "0,000 g" artığı kalmaz (kutu boş).
        e.Find("#th-has").Input("");
        e.WaitForAssertion(() => Assert.Equal("", e.Find("#th-has").GetAttribute("value") ?? ""));
        Assert.Empty(e.FindAll("#th-has-uyari"));
    }

    [Fact]
    public void PD2E_1_9_ekstre_karma_Kart_arti_Havale_has_satiri_has_bakiyesi_bos_isaret_yok()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        e.Find("#c-detay-c1").Click();
        e.WaitForElement("#cari-detay-odeme-h2");
        Assert.Equal("· Kart + Havale", e.Find("#cari-detay-odeme-h2").TextContent.Trim());
        // Has etkisi olmayan satırda "—" basılmaz; has satırında has bakiyesi yazar.
        Assert.Empty(e.FindAll("#cari-detay-has-h1"));
        Assert.Empty(e.FindAll("#cari-detay-has-h2"));
        Assert.Equal(Kuyumcu.Web.Pages.Cariler.HasBicim(-1.5m), e.Find("#cari-detay-has-h3").TextContent.Trim());
        Assert.Equal(string.Format(ParaDemo2Sozlugu.Tr.EkstreHasBakiye, Bicim.Tl(6424.89m), Kuyumcu.Web.Pages.Cariler.HasBicim(-1.5m)),
            e.Find("#cari-detay-bakiye-h3").TextContent.Trim());
    }
}

// ── (1) BASİT tek ödeme satırı · (8) takas kimlik kilidi · (11) PRO katman ───
public sealed class ParaDemo2IslemEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    /// <summary>Tur 5: MASAK kimlik kutusu açılsın mı (sunucu gereksinimi YALNIZ_KIMLIK).</summary>
    private bool masakKimlik;

    private const string CariListesi = """
        [{"id":"c1","ad":"Tuna Akarsu","tur":"MUSTERI","telefon":null,"email":null,
          "tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false,"sistemCarisi":false,"aktif":true},
         {"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","telefon":null,"email":null,
          "tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false,"sistemCarisi":true,"aktif":true}]
        """;

    public ParaDemo2IslemEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => masakKimlik)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<bool> masakKimlik) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            return ParaDemoSahte.Tamam(yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.EndsWith("/cari") => CariListesi,
                var v when v.Contains("/tevsik/gunluk") => """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Yarım Altın","ayarId":"K22","hasGrami":3.206,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/masak/gereksinim") && masakKimlik() => """{"kademe":"YALNIZ_KIMLIK","kimlikGerekli":true,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/islemler/altin-bacagi/ayarlar") => """{"stopajOranlari":[{"saticiTipi":"NIHAI_TUKETICI","oran":0,"pusulaGerekir":true},{"saticiTipi":"ESNAF_MUAF","oran":0.02,"pusulaGerekir":true},{"saticiTipi":"MUKELLEF","oran":0,"pusulaGerekir":false}],"alisIskontoOran":null,"saticiAdresiZorunlu":true}""",
                var v when v.Contains("/islemler/satis-fisi") => """{"fisId":"f1","fisNo":"FIS-000123","iskontoToplam":0}""",
                var v when v.Contains("/islemler/satis") => """{"id":"h1","fisId":"f1"}""",
                var v when v.Contains("/reyonlar") || v.Contains("/islemler/son") || v.Contains("/pos/cihazlar") || v.Contains("/islemler/personeller") || v.Contains("/iade-adaylari") => "[]",
                _ => "{}",
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Sahip", "t@ornek.com", "SAHIP", "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-tur-SATIS");
        return e;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> BasitKalemli()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        e.WaitForAssertion(() => Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.Equal("10000", SeciciYardim.SeciliDeger(e, "islem-birimfiyat")));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForElement("#islem-aldim-odeme");
        return e;
    }

    [Fact]
    public void PD2E_1_BASIT_tek_odeme_satirindan_HAVALE_secilir_TEK_odemeli_uca_HAVALE_gider()
    {
        var e = BasitKalemli();
        Assert.Equal("NAKIT", SeciciYardim.SeciliDeger(e, "islem-aldim-odeme"));
        SeciciYardim.Sec(e, "islem-aldim-odeme", "HAVALE");
        e.WaitForAssertion(() => Assert.Equal("HAVALE", SeciciYardim.SeciliDeger(e, "islem-aldim-odeme")));
        Assert.Empty(e.FindAll("#islem-odeme"));   // aynı soru ikinci seçicide sorulmaz (UX-S22)
        Assert.Empty(e.FindAll("#islem-bacak-0-arac"));
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled")));
        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis")));
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis-fisi"));   // karma (KARMA etiketi) yolu değil
        var g = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST").Govde).RootElement;
        Assert.Equal("HAVALE", g.GetProperty("odemeTuru").GetString());
    }

    [Fact]
    public void PD2E_1_Tek_odemeye_don_SECILI_araci_korur()
    {
        var e = BasitKalemli();
        e.Find("#islem-odeme-bol").Click();
        e.WaitForElement("#islem-bacak-0-arac");
        SeciciYardim.Sec(e, "islem-bacak-0-arac", "KART");
        e.Find("#islem-odeme-birlestir").Click();
        e.WaitForElement("#islem-aldim-odeme");
        Assert.Equal("KART", SeciciYardim.SeciliDeger(e, "islem-aldim-odeme"));
        Assert.Empty(e.FindAll("#islem-bacak-0-arac"));
    }

    [Fact]
    public void PD2E_1_bos_fiste_satirda_secici_YOK_zorunlu_once()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        e.WaitForElement("#islem-aldim-satir");
        Assert.Empty(e.FindAll("#islem-aldim-odeme"));
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Takas(string mod)
    {
        var e = Ekran(mod);
        e.Find("#islem-tur-TAKAS").Click();
        e.WaitForElement("#takas-satici");
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.Find("#takas-eski-gram").Input("3");
        SeciciYardim.Sec(e, "takas-yeni-urun", "u1");
        e.Find("#takas-yeni-fiyat").Input("10000");
        return e;
    }

    [Fact]
    public void PD2E_8_takas_kilit_sebebi_EKSIK_ALANA_gore_guncellenir_hata_rengi_dokununca()
    {
        var e = Takas(ArayuzModuServisi.Basit);
        SeciciYardim.Sec(e, "takas-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#takas-satici-tckn");
        // Tür seçildi, ad cariden geldi: eksik yalnız TCKN ve adres — ve henüz dokunulmadıkları için kırmızı YOK.
        // DEGISIM-2: BASİT kilit alanın ekrandaki ADIYLA ("TC kimlik no" — kutunun etiketi); koşul AYNEN.
        Assert.Equal(string.Format(ParaDemo2Sozlugu.Tr.TakasEksikBasit, DegisimGunSonuSozlugu.Tr.TcKimlikNo + ", adres"), e.Find("#islem-kaydet-kilit").TextContent.Trim());
        Assert.Empty(e.FindAll("#takas-satici-tckn-hata"));
        Assert.Empty(e.FindAll("#takas-satici-adres-hata"));

        e.Find("#takas-satici-tckn").Input("123");   // dokunuldu, 11 hane değil
        e.WaitForElement("#takas-satici-tckn-hata");
        Assert.Equal(string.Format(ParaDemo2Sozlugu.Tr.TakasEksikBasit, DegisimGunSonuSozlugu.Tr.TcKimlikNo + " (11 hane), adres"), e.Find("#islem-kaydet-kilit").TextContent.Trim());   // DEGISIM-2: etiketle aynı ad
        Assert.Empty(e.FindAll("#takas-satici-adres-hata"));   // adrese dokunulmadı

        e.Find("#takas-satici-tckn").Input("10000000146");
        e.Find("#takas-satici-adres").Input("Çarşı Mah. 1");
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled")));
    }

    [Fact]
    public void PD2E_8_takas_Kaydet_yazisi_iki_modda_AYNI()
    {
        var b = Takas(ArayuzModuServisi.Basit).Find("#islem-kaydet").TextContent.Trim();
        Assert.Equal(SatisTuvalSozlugu.Tr.KaydetTek, b);
    }

    [Fact]
    public void PD2E_8_takas_Kaydet_yazisi_PRO_da_da_Kaydet()
    {
        var p = Takas(ArayuzModuServisi.Profesyonel).Find("#islem-kaydet").TextContent.Trim();
        Assert.Equal(SatisTuvalSozlugu.Tr.KaydetTek, p);
    }

    [Fact]
    public void PD2E_11_PRO_kayittan_sonra_odeme_katmani_KAPALI_basari_bandi_ortulmez()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.Equal("10000", SeciciYardim.SeciliDeger(e, "islem-birimfiyat")));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForElement("#islem-pro-odeme-degistir").Click();
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-pro-odeme").HasAttribute("hidden")));
        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")));
        e.WaitForElement(".bant-basari");
        e.WaitForAssertion(() => Assert.True(e.Find("#islem-pro-odeme").HasAttribute("hidden")));
    }

    [Theory]
    [InlineData("BASIT")]
    [InlineData("PROFESYONEL")]
    public void PD2E_8_takas_MASAK_kutusu_acikken_TCKN_ve_adres_TEK_kez_kilit_alan_adiyla_GORUNUR(string mod)
    {
        masakKimlik = true;
        var e = Takas(mod);
        SeciciYardim.Sec(e, "takas-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#masak-adres");
        // Kimlik BİR KEZ: satıcı bloğunda ad/TCKN/adres kutusu YOK, MASAK kutusundan alındığı yazar.
        e.WaitForElement("#takas-kimlik-not");
        Assert.Empty(e.FindAll("#takas-satici-tckn"));
        Assert.Empty(e.FindAll("#takas-satici-adres"));
        Assert.Empty(e.FindAll("#takas-satici-ad"));
        Assert.Single(e.FindAll("#masak-adres"));
        // Zorunluluk TEK kural: MASAK adresi "isteğe bağlı" değil, zorunlu.
        Assert.DoesNotContain(Sozlukler.Tr.Ortak.Istege, e.Find("#masak-adres").Closest(".alan")!.TextContent, StringComparison.Ordinal);
        Assert.Equal("true", e.Find("#masak-adres").GetAttribute("aria-required"));
        // Kilit sebebi Kaydet'in yanında GÖRÜNÜR (gorunmez sınıfı yok) ve eksik alanı adıyla söyler.
        var kalip = mod == "BASIT" ? ParaDemo2Sozlugu.Tr.TakasEksikBasit : ParaDemo2Sozlugu.Tr.TakasEksikPro;
        // TOPLU-5 (hakem DÜŞÜK "kilit 'TCKN' ↔ alan 'Belge No'"): kilitteki ad, kimlik kutusundaki alanın KENDİ etiketidir.
        var alanAdi = e.Find("#masak-belge-no-sar .alan-etiket-metin").TextContent.Trim();
        Assert.Equal(ZoIslemSozlugu.Tr.MasakBelgeNoTc, alanAdi);   // YASAL-PARAM-EKRAN tur 2: iki modda aynı etiket (TCKN kısaltmasız)
        e.WaitForAssertion(() => Assert.Equal(string.Format(kalip, $"{alanAdi}, adres"), e.Find("#islem-kaydet-kilit").TextContent.Trim()));
        Assert.DoesNotContain("gorunmez", e.Find("#islem-kaydet-kilit").ClassName ?? "", StringComparison.Ordinal);
        // TCKN MASAK kutusuna girilince satıcıya da geçer; eksik yalnız adres.
        e.Find("#masak-belge-no").Input("10000000146");
        e.WaitForAssertion(() => Assert.Equal(string.Format(kalip, "adres"), e.Find("#islem-kaydet-kilit").TextContent.Trim()));
        e.Find("#masak-adres").Input("Çarşı Mah. 1");
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled")));
        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/takas")));
        var g = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/takas")).Govde).RootElement;
        Assert.Equal("10000000146", g.GetProperty("satici").GetProperty("tckn").GetString());
        Assert.Equal("Çarşı Mah. 1", g.GetProperty("satici").GetProperty("adres").GetString());
        Assert.Equal("10000000146", g.GetProperty("masak").GetProperty("belgeNo").GetString());
    }
}

// ── (10) Gider virman: "Hedef" etiketi TEK ───────────────────────────────────
public sealed class ParaDemo2GiderEkranTestleri : BunitContext
{
    public ParaDemo2GiderEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            return Task.FromResult(ParaDemoSahte.Tamam(yol switch
            {
                var y when y.EndsWith("/kasalar") => """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":1000,"aktif":true}]""",
                var y when y.EndsWith("/bankalar") => """{"hesaplar":[{"id":"bh1","bankaAdi":"Ziraat","hesapAdi":"Vadesiz","hesapSahibi":null,"iban":"TR11","paraBirimi":"TRY","bakiye":5000,"aktif":true}]}""",
                var y when y.EndsWith("/gider-kategorileri") => """{"kategoriler":[{"id":"gk1","ad":"Kira","aciklama":null,"aktif":true,"giderAdedi":0,"giderToplamTl":0}],"toplam":1}""",
                var y when y.EndsWith("/giderler") => """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":"En çok 100 gider satırı gösterilir."}""",
                var y when y.EndsWith("/virman") => """{"satirlar":[],"toplam":0,"limit":50,"dahaFazlasiVar":false,"sinirNotu":"En son 50 virman gösterilir."}""",
                var y when y.Contains("/kasalar/") && y.Contains("/hareketler") => """{"hareketler":[]}""",
                var y when y.Contains("/kasalar/") && y.Contains("/onizleme") => """{"toplamBakiye":1000,"kartToplam":0,"havaleToplam":0,"nakitDefter":1000}""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur1","hasAlis":4100,"hasSatis":4150}""",
                var y when y.Contains("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                _ => "{}",
            }));
        }
    }

    [Fact]
    public void PD2E_10_virman_formunda_Hedef_etiketi_TEK()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("http://localhost/gider?sekme=virman");
        var e = Render<Kuyumcu.Web.Pages.Gider>();
        e.WaitForElement("#gd-v-hedef");
        Assert.Single(e.FindAll("label[for=gd-v-hedef]"));
    }
}

// ── Tur 5 (doğrulayıcı KRİTİK) — Panel "Kasada ne var?" alt satırı = Kasalar/Rapor merkezi ile AYNI ayrım ──
public sealed class ParaDemo2PanelEkranTestleri : BunitContext
{
    private readonly List<string> istekler = [];
    private System.Net.HttpStatusCode ayrimKodu = System.Net.HttpStatusCode.OK;

    // Kasa toplamı 1.500 ₺ (tek TL kasası) — içinde kart 300 + havale 200; çekmecedeki nakit 1.000 ₺.
    private const string Gunluk = """
        {"tarih":"2026-10-02","satis":{"adet":1,"toplamTl":1500.00,"toplamHas":0.2},
         "alis":{"adet":0,"toplamTl":0,"toplamHas":0},"hurda":{"toplamHas":0,"odenenTl":0},
         "netHasGramHareketi":0.2,"hareketSayisi":1,
         "kasalar":[{"id":"k1","ad":"Ana Kasa","bakiye":1500.00,"paraBirimi":"TRY"}]}
        """;
    private const string Ayrim = """
        [{"kasaId":"k1","paraBirimi":"TRY","toplam":1500.00,"nakit":1000.00,"kart":300.00,"havale":200.00}]
        """;

    public ParaDemo2PanelEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<Microsoft.Extensions.Configuration.IConfiguration>(_ => new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        Services.AddScoped<HavaServisi>();
    }

    private sealed class SahteApi(ParaDemo2PanelEkranTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            lock (t.istekler) t.istekler.Add(yol);
            if (yol.EndsWith("/kasalar/nakit-ayrimi", StringComparison.Ordinal))
                return Task.FromResult(t.ayrimKodu == System.Net.HttpStatusCode.OK
                    ? ParaDemoSahte.Tamam(Ayrim) : ParaDemoSahte.Hata((int)t.ayrimKodu, "Sunucu hatası"));
            return Task.FromResult(ParaDemoSahte.Tamam(yol switch
            {
                var y when y.EndsWith("/raporlar/gunluk") => Gunluk,
                var y when y.EndsWith("/raporlar/kar-zarar") => """{"hasilatTl":1500,"maliyetTl":1400,"karTl":100,"karMarjiYuzde":6.6,"hasilatHas":0.2,"maliyetHas":0.2,"karHas":0,"satisAdedi":1,"maliyetiBilinenAdet":1,"maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM","uyari":null}""",
                var y when y.EndsWith("/bankalar") => """{"hesaplar":[]}""",
                var y when y.EndsWith("/raporlar/cari-yaslandirma") => """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""",
                var y when y.EndsWith("/raporlar/vade-takibi") => """{"ufukGun":7,"gecikenToplam":0,"yaklasanToplam":0,"vadesizToplam":0,"ufukOtesiToplam":0,"acikToplam":0,"cariler":[]}""",
                var y when y.EndsWith("/bildirimler") => """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""",
                var y when y.EndsWith("/pozisyon") => """{"anaMaden":"ALTIN","altin":{"maden":"ALTIN","vitrinGram":0,"hurdaGram":0,"cariAlacakGram":0,"cariBorcGram":0,"netGram":0,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},"digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,"hesaplanmaAni":"2026-10-02T09:00:00Z","donemdenBagimsiz":true}""",
                var y when y.EndsWith("/kur/kaynaklar") => """{"kaynaklar":["ALTINKAYNAK"],"secili":"ALTINKAYNAK","varsayilan":"ALTINKAYNAK"}""",
                var y when y.Contains("/tercihler/benim") => """{"tercihler":{}}""",
                var y when y.Contains("/uyum/") => """{"suresiGecmis":0,"yaklasan":0,"gecikmisSayisi":0,"yuksek":0}""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            }));
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Panel> Basit()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1", ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-sade");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll(".karsilama-iskelet")), TimeSpan.FromSeconds(10));
        return e;
    }

    [Fact]
    public void PD2E_4_Panel_Kasada_ne_var_alt_satiri_Nakit_ve_Kart_Havale_AYNI_ayrim()
    {
        var e = Basit();
        // Büyük sayı kasanın TAMAMI (değişmedi); alt satır çekmecedeki NAKİT ile kasaya yazılmış kart+havale.
        Assert.Equal(Bicim.BelgePara(1500m, "TRY"), e.Find("#panel-soru-kasa-deger").TextContent.Trim());
        e.WaitForAssertion(() => Assert.Equal($"{PanelTuvalSozlugu.Al("tr").Nakit} {Bicim.BelgePara(1000m, "TRY")}",
            e.Find("#panel-soru-kasa-grup").TextContent.Trim()));
        Assert.Equal($"{ParaDemo2Sozlugu.Tr.KartHavaleEtiket} {Bicim.BelgePara(500m, "TRY")}",
            e.Find("#panel-soru-kasa-karthavale").TextContent.Trim());
        Assert.DoesNotContain($"Nakit {Bicim.BelgePara(1500m, "TRY")}", e.Find("#panel-soru-kasa").TextContent, StringComparison.Ordinal);
        Assert.Contains(istekler, y => y.EndsWith("/kasalar/nakit-ayrimi", StringComparison.Ordinal));
        Assert.Empty(e.FindAll("#panel-soru-kasa-ayrim-hata"));
    }

    [Fact]
    public void PD2E_4_Panel_ayrim_okunamazsa_toplam_NAKIT_diye_basilmaz_kisa_hata()
    {
        ayrimKodu = System.Net.HttpStatusCode.InternalServerError;
        var e = Basit();
        e.WaitForElement("#panel-soru-kasa-ayrim-hata");
        var grup = e.Find("#panel-soru-kasa-grup").TextContent.Trim();
        Assert.DoesNotContain("Nakit", grup, StringComparison.Ordinal);
        Assert.Equal($"{PanelSadeSozlugu.KasaGrubu("TRY", "tr")} {Bicim.BelgePara(1500m, "TRY")}", grup);
        Assert.Empty(e.FindAll("#panel-soru-kasa-karthavale"));
    }
}

// ── Tur 5 (doğrulayıcı YÜKSEK) — tahsilat Tutar BOŞ ve zorunlu; kural tek cümle, yalnız Kaydet denemesinden sonra ──
public sealed class ParaDemo2TahsilatTutarTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public ParaDemo2TahsilatTutarTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, yol, govde));
            return ParaDemoSahte.Tamam(yol switch
            {
                var y when y.EndsWith("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"gbpTry":null}""",
                var y when y.EndsWith("/tahsilat") => """{"id":"c1","ad":"Kemal Ardıç","tur":"MUSTERI","bakiye":{"cariId":"c1","tlBakiye":6324.89,"hasGramBakiye":0}}""",
                var y when y.EndsWith("/cari") => """
                    [{"id":"c1","ad":"Kemal Ardıç","tur":"MUSTERI","telefon":null,"email":null,
                      "tlBakiye":6424.89,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
                      "riskSeviye":null,"pepBeyani":false,"aktif":true}]
                    """,
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true}]""",
                var y when y.Contains("/tevsik/gunluk") => """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/listeler") => "[]",
                _ => "{}",
            });
        }
    }

    [Theory]
    [InlineData("BASIT")]
    [InlineData("PROFESYONEL")]
    public void PD2E_7_tahsilat_Tutar_BOS_zorunlu_hata_YALNIZ_Kaydet_denemesinden_sonra(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("#c-tahsilat-c1").Click();
        e.WaitForElement("#th-tutar");
        // Açılışta: kutu BOŞ (0 yazmaz), "isteğe bağlı" yok, kırmızı yazı ve kilit satırı yok, Kaydet açık.
        Assert.Equal("", e.Find("#th-tutar").GetAttribute("value") ?? "");
        var alan = e.Find("#th-tutar").Closest(".alan")!;
        Assert.DoesNotContain(Sozlukler.Tr.Ortak.Istege, alan.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain(ParaDemo2Sozlugu.Tr.TutarYaDaAltin, e.Markup, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#th-kaydet-kilit"));
        Assert.False(e.Find("#th-kaydet").HasAttribute("disabled"));

        // Boş deneme: istek GİTMEZ, alanın altında TEK cümle.
        lock (istekler) istekler.Clear();
        e.Find("#th-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(ParaDemo2Sozlugu.Tr.TutarYaDaAltin, e.Find("#th-tutar").Closest(".alan")!.TextContent, StringComparison.Ordinal));
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.Single(e.FindAll(".alan"), a => a.TextContent.Contains(ParaDemo2Sozlugu.Tr.TutarYaDaAltin, StringComparison.Ordinal));

        // Tutar girilince hata kalkar, Kaydet tahsilat gönderir.
        e.Find("#th-tutar").Input("100");
        e.WaitForAssertion(() => Assert.DoesNotContain(ParaDemo2Sozlugu.Tr.TutarYaDaAltin, e.Markup, StringComparison.Ordinal));
        e.Find("#th-kaydet").Click();
        e.WaitForAssertion(() => { lock (istekler) Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/tahsilat")); });
        string g; lock (istekler) g = istekler.Last(i => i.Metot == "POST").Govde;
        Assert.Equal(100m, JsonDocument.Parse(g).RootElement.GetProperty("tlTutari").GetDecimal());
    }
}
