using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Cikti;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET SATINALMA-EKRAN — satın alma / irsaliye ekran kalanları.
//   SE01 irsaliye satırı kart iç boşluğu (kart-govde) içinde; taslakta boş irsaliye kartı YOK
//   SE02 açıklama satırları açılışta görünmez (kapalı akordeon), açılınca görünür
//   SE03 seçici yer tutucusu "Tedarikçi seçin" ("Henüz kayıt seçilmedi." DEĞİL)
//   SE04 mal kabul kimlik formu başlığı "tedarikçinin" (alıcı için "müşterinin" yanlış)
//   SE05 eski hata iletisi yeni girdiyle kalmaz
//   SE06 irsaliye çekmecesi iptal bölümünde "Sipariş" metni yok (irsaliye metinleri)
//   SE08 düzenleme formunda dolu fiyat odakta binlik ayracı göstermez
//   SE07 iptal sipariş/irsaliye ekranda "aktife al" yolu → POST
//   SH01 sipariş iptal → aktife-al → TASLAK (SQL), hasım 409/403/404
//   SH02 sevk irsaliyesi iptal → aktife-al → ACIK (SQL), hasım 409/404
//   SH03 irsaliye dökümünde teslim adresi (gelen: işletme · giden: cari · yoksa uydurulmaz)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SatinalmaEkranKalanlarEkranTestleri : BunitContext
{
    private const string Liste = """
        {"satirlar":[
          {"id":"s1","no":"SAS-2026-00001","durum":"TASLAK","subeId":"sb","tedarikciCariId":"c1","tedarikciAdi":"Has Toptan A.Ş.","beklenenTarih":null,"aciklama":null,"hesapKodu":null,"olusturuldu":"2026-10-09T08:00:00Z","guncellendi":"2026-10-09T08:00:00Z","kalemSayisi":1},
          {"id":"s2","no":"SAS-2026-00002","durum":"KISMI_KABUL","subeId":"sb","tedarikciCariId":"c1","tedarikciAdi":"Has Toptan A.Ş.","beklenenTarih":null,"aciklama":null,"hesapKodu":null,"olusturuldu":"2026-10-09T08:00:00Z","guncellendi":"2026-10-09T08:00:00Z","kalemSayisi":1},
          {"id":"s3","no":"SAS-2026-00003","durum":"IPTAL","subeId":"sb","tedarikciCariId":"c1","tedarikciAdi":"Has Toptan A.Ş.","beklenenTarih":null,"aciklama":null,"hesapKodu":null,"olusturuldu":"2026-10-09T08:00:00Z","guncellendi":"2026-10-09T08:00:00Z","kalemSayisi":1}
        ],"toplam":3,"kirilim":[],"limit":200,"azamiLimit":200,"sinirDegdi":false}
        """;

    private const string Taslak = """
        {"id":"s1","no":"SAS-2026-00001","durum":"TASLAK","subeId":"sb","tedarikciCariId":"c1","tedarikciAdi":"Has Toptan A.Ş.",
         "beklenenTarih":null,"aciklama":null,"hesapKodu":null,"iptalGerekce":null,
         "olusturuldu":"2026-10-09T08:00:00Z","guncellendi":"2026-10-09T08:00:00Z",
         "kalemler":[{"id":"k1","urunId":"u1","urunAdi":"22 ayar bilezik","birim":"ADET","tanim":null,"adet":5,"birimFiyatTl":2000,"kabulEdilenAdet":0,"kalanAdet":5,"sira":0}],
         "irsaliyeler":[],"duzenlenebilir":true,"malKabulEdilebilir":false,"kilitSebebi":null}
        """;

    private const string Kismi = """
        {"id":"s2","no":"SAS-2026-00002","durum":"KISMI_KABUL","subeId":"sb","tedarikciCariId":"c1","tedarikciAdi":"Has Toptan A.Ş.",
         "beklenenTarih":null,"aciklama":null,"hesapKodu":null,"iptalGerekce":null,
         "olusturuldu":"2026-10-09T08:00:00Z","guncellendi":"2026-10-09T08:00:00Z",
         "kalemler":[{"id":"k1","urunId":"u1","urunAdi":"22 ayar bilezik","birim":"ADET","tanim":null,"adet":5,"birimFiyatTl":2000,"kabulEdilenAdet":2,"kalanAdet":3,"sira":0}],
         "irsaliyeler":[{"id":"i1","no":"IRS-2026-00001","tarih":"2026-10-09T08:10:00Z","durum":"ACIK","faturaId":null}],
         "duzenlenebilir":false,"malKabulEdilebilir":true,"kilitSebebi":"Onaylanmış siparişin kalemleri değiştirilemez."}
        """;

    private const string IptalSiparis = """
        {"id":"s3","no":"SAS-2026-00003","durum":"IPTAL","subeId":"sb","tedarikciCariId":"c1","tedarikciAdi":"Has Toptan A.Ş.",
         "beklenenTarih":null,"aciklama":null,"hesapKodu":null,"iptalGerekce":"tedarikçi vazgeçti",
         "olusturuldu":"2026-10-09T08:00:00Z","guncellendi":"2026-10-09T08:00:00Z",
         "kalemler":[{"id":"k1","urunId":"u1","urunAdi":"22 ayar bilezik","birim":"ADET","tanim":null,"adet":5,"birimFiyatTl":2000,"kabulEdilenAdet":0,"kalanAdet":5,"sira":0}],
         "irsaliyeler":[],"duzenlenebilir":false,"malKabulEdilebilir":false,"kilitSebebi":"Sipariş iptal edildi."}
        """;

    private const string IrsListe = """
        {"satirlar":[
          {"id":"i1","no":"IRS-2026-00001","yon":"GIDEN","durum":"ACIK","cariId":"c2","cariAdi":"Ayşe Demir","subeId":"sb","tarih":"2026-10-09T08:10:00Z","faturaId":null,"satinalmaSiparisId":null,"aciklama":null,"hesapKodu":null,"bagsiz":true,"kalemSayisi":1},
          {"id":"i2","no":"IRS-2026-00002","yon":"GIDEN","durum":"IPTAL","cariId":"c2","cariAdi":"Ayşe Demir","subeId":"sb","tarih":"2026-10-08T08:10:00Z","faturaId":null,"satinalmaSiparisId":null,"aciklama":null,"hesapKodu":null,"bagsiz":false,"kalemSayisi":1}
        ],"toplam":2,"bagsizToplam":1,"limit":200,"azamiLimit":200,"sinirDegdi":false}
        """;

    private const string IrsSevk = """
        {"id":"i1","no":"IRS-2026-00001","yon":"GIDEN","durum":"ACIK","cariId":"c2","cariAdi":"Ayşe Demir","subeId":"sb",
         "tarih":"2026-10-09T08:10:00Z","satinalmaSiparisId":null,"faturaId":null,"aciklama":null,"hesapKodu":null,"iptalGerekce":null,
         "olusturuldu":"2026-10-09T08:10:00Z","guncellendi":"2026-10-09T08:10:00Z",
         "kalemler":[{"id":"ik1","urunId":"u1","urunAdi":"22 ayar bilezik","birim":"ADET","tanim":null,"adet":2,"hareketId":null,"sira":0}],
         "bagsiz":true,"duzenlenebilir":true,"kilitSebebi":null}
        """;

    private const string IrsIptal = """
        {"id":"i2","no":"IRS-2026-00002","yon":"GIDEN","durum":"IPTAL","cariId":"c2","cariAdi":"Ayşe Demir","subeId":"sb",
         "tarih":"2026-10-08T08:10:00Z","satinalmaSiparisId":null,"faturaId":null,"aciklama":null,"hesapKodu":null,"iptalGerekce":"yanlış alıcı girildi",
         "olusturuldu":"2026-10-08T08:10:00Z","guncellendi":"2026-10-08T08:10:00Z",
         "kalemler":[{"id":"ik1","urunId":"u1","urunAdi":"22 ayar bilezik","birim":"ADET","tanim":null,"adet":2,"hareketId":null,"sira":0}],
         "bagsiz":false,"duzenlenebilir":false,"kilitSebebi":"İrsaliye iptal edildi."}
        """;

    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly List<(string Metot, string Parca, HttpStatusCode Kod, string Govde)> yollar = [];

    public SatinalmaEkranKalanlarEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
    }

    private sealed class SahteApi(SatinalmaEkranKalanlarEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            foreach (var (m, p, k, g) in t.yollar)
                if (m == istek.Method.Method && yol.Contains(p))
                    return new HttpResponseMessage(k) { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
            if (yol.Contains("/masak/esikler"))
                return Json("""{"kimlikEsigi":370000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000.01,"ekBelgeEsigi":20000000.01,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""");
            if (yol.Contains("/masak/gereksinim") && yol.Contains("tutar=6000"))
                return Json("""{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":0}""");
            if (yol.Contains("/masak/gereksinim"))
                return Json("""{"kademe":"AMAC_BEYANI","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20,"toplamdanMi":true,"olcuTutarTl":900000}""");
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            { Content = new StringContent("""{"message":"tanımsız uç"}""", System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private static HttpResponseMessage Json(string g) =>
        new(HttpStatusCode.OK) { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };

    private void Yol(string metot, string parca, string govde, HttpStatusCode kod = HttpStatusCode.OK) =>
        yollar.Add((metot, parca, kod, govde));

    private void Oturum(string rol) =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "sb", null, ArayuzModuServisi.Profesyonel));

    private List<(string Metot, string Yol, string Govde)> Istekler(string metot)
    {
        lock (istekler) return istekler.Where(i => i.Metot == metot).ToList();
    }

    private static SatinAlmaMetinleri M => SatinAlmaSozlugu.Tr;

    private IRenderedComponent<Kuyumcu.Web.Pages.SatinAlma> SiparisAc(string id, string detayJson)
    {
        Oturum("SAHIP");
        Yol("GET", "/satinalma?limit=200", Liste);
        Yol("GET", $"/satinalma/{id}", detayJson);
        Yol("GET", "/kur/guncel", """{"id":"kur1"}""");
        var e = Render<Kuyumcu.Web.Pages.SatinAlma>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll($"#sa-ac-{id}")));
        e.FindAll($"#sa-ac-{id}")[0].Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-detay-bilgi")));
        return e;
    }

    [Fact]
    public void SE01_irsaliye_satiri_kart_ic_boslugunda_taslakta_bos_irsaliye_karti_yok()
    {
        var e = SiparisAc("s2", Kismi);
        // Satırlar kart-govde (iç boşluk) içinde; kart kenarına yapışık değil.
        Assert.Contains("kart-govde", e.Find("#sa-detay-irs-liste").GetAttribute("class"));
        Assert.Contains("kart", e.Find("#sa-detay-irsaliyeler").GetAttribute("class"));
        Assert.NotEmpty(e.FindAll("#sa-detay-irs-i1"));

        var t = SiparisAc2("s1", Taslak);
        Assert.Empty(t.FindAll("#sa-detay-irsaliyeler"));          // taslakta boş "irsaliyeleri" kartı yok
        Assert.Empty(t.FindAll("#sa-detay-irs-liste"));
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.SatinAlma> SiparisAc2(string id, string json)
    {
        var ctx = new SatinalmaEkranKalanlarEkranTestleri();
        return ctx.SiparisAc(id, json);
    }

    [Fact]
    public void SE02_aciklama_satirlari_acilista_gorunmez_acilinca_gorunur()
    {
        var e = SiparisAc("s2", Kismi);
        Assert.Empty(e.FindAll("#sa-kabul-aciklama"));
        Assert.Equal("false", e.Find("#sa-kabul-bilgi-ac").GetAttribute("aria-expanded"));
        e.Find("#sa-kabul-bilgi-ac").Click();
        Assert.Contains(M.MalKabulAciklama, e.Find("#sa-kabul-aciklama").TextContent);

        // Sipariş formu: fiyat açıklaması da kapalı akordeonda.
        e.Find("#sa-yeni").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-form-bilgi-ac")));
        Assert.Empty(e.FindAll("#sa-form-fiyat-aciklama"));
        e.Find("#sa-form-bilgi-ac").Click();
        Assert.Contains(M.FiyatAciklama, e.Find("#sa-form-fiyat-aciklama").TextContent);
    }

    [Fact]
    public void SE03_secici_yer_tutucusu_tedarikci_secin_degil_genel_metin()
    {
        Oturum("SAHIP");
        Yol("GET", "/satinalma?limit=200", Liste);
        var e = Render<Kuyumcu.Web.Pages.SatinAlma>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-yeni")));
        e.Find("#sa-yeni").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-form-tedarikci")));
        Assert.Contains(M.TedarikciYerTutucu, e.Markup);
        Assert.Contains(M.UrunYerTutucu, e.Markup);
        Assert.DoesNotContain("Henüz kayıt seçilmedi", e.Markup);
    }

    [Fact]
    public void SE04_mal_kabul_kimlik_formu_basligi_tedarikci_der_musteri_demez()
    {
        Oturum("SAHIP");
        var buyuk = Kismi.Replace("\"birimFiyatTl\":2000", "\"birimFiyatTl\":150000");
        Yol("GET", "/satinalma?limit=200", Liste);
        Yol("GET", "/satinalma/s2", buyuk);
        Yol("GET", "/kur/guncel", """{"id":"kur1"}""");
        var e = Render<Kuyumcu.Web.Pages.SatinAlma>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-ac-s2")));
        e.FindAll("#sa-ac-s2")[0].Click();
        e.WaitForElement("#masak-baslik");
        var baslik = e.Find("#masak-baslik").TextContent;
        Assert.DoesNotContain("müşteri", baslik, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tedarikçi", baslik, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SE05_eski_hata_iletisi_yeni_girdiyle_ve_vazgecmekle_kalmaz()
    {
        Oturum("SAHIP");
        Yol("GET", "/satinalma?limit=200", Liste);
        Yol("GET", "/satinalma/s2", Kismi);
        Yol("GET", "/kur/guncel", """{"id":"kur1"}""");
        Yol("POST", "/satinalma/s2/mal-kabul", """{"message":"Kur eski, tekrar deneyin."}""", HttpStatusCode.BadRequest);
        var e = Render<Kuyumcu.Web.Pages.SatinAlma>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-ac-s2")));
        e.FindAll("#sa-ac-s2")[0].Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-kabul-0")));
        e.Find("#sa-detay-birincil").Click();
        e.WaitForAssertion(() => Assert.Contains("Kur eski", e.Find("#sa-detay-hata").TextContent));
        // Adet değişince eski ileti kalkar.
        // 0 yazılınca MASAK yeniden sorulmaz (tutar 0): ileti yalnız KabulAdetDegisti'nin kendi temizliğiyle kalkar.
        e.FindAll("#sa-kabul-0")[0].Input("0");
        e.WaitForAssertion(() => Assert.DoesNotContain("Kur eski", e.Markup));
    }

    [Fact]
    public void SE06_irsaliye_cekmecesinde_iptal_bolumu_irsaliye_der_siparis_demez()
    {
        Oturum("SAHIP");
        Yol("GET", "/irsaliyeler?limit=200", IrsListe);
        Yol("GET", "/irsaliyeler/i1", IrsSevk);
        var e = Render<Kuyumcu.Web.Pages.Irsaliyeler>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#irs-ac-i1")));
        e.FindAll("#irs-ac-i1")[0].Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#irs-detay-iptal-ac")));
        e.Find("#irs-detay-iptal-ac").Click();
        var iptal = e.Find("#irs-detay-iptal").TextContent;
        Assert.Contains(M.IrsIptalEt, iptal);
        Assert.DoesNotContain("Sipariş", iptal);
        Assert.DoesNotContain("sipariş", e.Find("#irs-detay-iptal").InnerHtml);
        e.Find("#irs-iptal-gerekce").Input("yanlış alıcıya yazıldı");
        e.WaitForAssertion(() => Assert.False(e.Find("#irs-iptal-ac").HasAttribute("disabled")));
        e.Find("#irs-iptal-ac").Click();
        e.WaitForAssertion(() => Assert.Contains(M.IrsIptalBaslik, e.Markup));
        Assert.DoesNotContain(M.IptalBaslik, e.Markup);            // "Sipariş iptal edilsin mi?" YOK
    }

    [Fact]
    public void SE09_iptalli_siparis_kilit_kutusu_yok_tek_cumle_ve_aktife_al_birincil_duzeltme_yolu_yok()
    {
        Oturum("SAHIP");
        Yol("GET", "/satinalma?limit=200", Liste);
        Yol("GET", "/satinalma/s3", IptalSiparis);
        var e = Render<Kuyumcu.Web.Pages.SatinAlma>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-gecmis-ac")));
        e.Find("#sa-gecmis-ac").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-ac-s3")));
        e.FindAll("#sa-ac-s3")[0].Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-aktife-al")));
        Assert.Empty(e.FindAll("#degismez-ipucu-SatinalmaSiparisi"));          // "değiştirilemez" kutusu YOK
        Assert.DoesNotContain(M.DegismezYol, e.Markup);                         // mal gelmemiş: alış iadesi önerisi YOK
        Assert.Equal(M.IptalliNot, e.Find("#sa-iptalli-not").TextContent.Trim());
        Assert.Contains("dugme-birincil", e.Find("#sa-aktife-al").ClassName);    // tek birincil düğme
    }

    [Fact]
    public void SE10_iptal_bolumunde_ayni_uyari_tek_kez_ve_irsaliye_aktife_al_tek_eylem_birincil()
    {
        Oturum("SAHIP");
        Yol("GET", "/irsaliyeler?limit=200", IrsListe);
        Yol("GET", "/irsaliyeler/i1", IrsSevk);
        Yol("GET", "/irsaliyeler/i2", IrsIptal);
        var e = Render<Kuyumcu.Web.Pages.Irsaliyeler>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#irs-ac-i1")));
        e.FindAll("#irs-ac-i1")[0].Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#irs-detay-iptal-ac")));
        e.Find("#irs-detay-iptal-ac").Click();
        // "en az N karakter" uyarısı yalnız alan etiketinde; ayrı kilit satırı YOK, düğme sebebi title'da.
        Assert.Empty(e.FindAll("#irs-iptal-kilit"));
        Assert.Empty(e.FindAll("#irs-iptal-gerekce-sayac"));                  // sayaç da yok: uyarı tek yerde (etiket)
        Assert.Contains(string.Format(M.IrsGerekceKisa, 10), e.Find("#irs-iptal-ac").GetAttribute("title"));
        e.Find("#irs-iptal-gerekce").Input("yanlış alıcıya yazıldı");
        e.WaitForAssertion(() => Assert.DoesNotContain(string.Format(M.IrsGerekceKisa, 10), e.Find("#irs-iptal-ac").GetAttribute("title")));
        // İptalli irsaliye: tek eylem "aktife al" birincil.
        var r = new SatinalmaEkranKalanlarEkranTestleri();
        r.Oturum("SAHIP");
        r.Yol("GET", "/irsaliyeler?limit=200", IrsListe);
        r.Yol("GET", "/irsaliyeler/i2", IrsIptal);
        var c = r.Render<Kuyumcu.Web.Pages.Irsaliyeler>();
        c.Find("#irs-gecmis-ac").Click();
        c.WaitForAssertion(() => Assert.NotEmpty(c.FindAll("#irs-ac-i2")));
        c.FindAll("#irs-ac-i2")[0].Click();
        c.WaitForAssertion(() => Assert.NotEmpty(c.FindAll("#irs-aktife-al")));
        Assert.Contains("dugme-birincil", c.Find("#irs-aktife-al").ClassName);
        Assert.Empty(c.FindAll("#degismez-ipucu-Irsaliye"));
    }

    [Fact]
    public void SE11_mal_kabul_masak_formunda_fon_ve_ek_belge_tezgahtar_dilinde_ve_hala_zorunlu()
    {
        Oturum("SAHIP");
        var buyuk = Kismi.Replace("\"birimFiyatTl\":2000", "\"birimFiyatTl\":150000");
        Yol("GET", "/satinalma?limit=200", Liste);
        Yol("GET", "/satinalma/s2", buyuk);
        Yol("GET", "/kur/guncel", """{"id":"kur1"}""");
        Yol("GET", "/masak/gereksinim", """{"kademe":"EK_BELGE","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":true,"ekBelgeGerekli":true,"asgariAciklamaUzunlugu":20,"toplamdanMi":true,"olcuTutarTl":30000000}""");
        var e = Render<Kuyumcu.Web.Pages.SatinAlma>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-ac-s2")));
        e.FindAll("#sa-ac-s2")[0].Click();
        e.WaitForElement("#masak-fon");
        e.WaitForElement("#masak-ekbelge");
        var form = e.Find("#sa-masak").TextContent;
        Assert.Contains(M.MasakFonEtiketi, form);
        Assert.Contains(M.MasakEkBelgeEtiketi, form);
        Assert.Empty(e.FindAll("#sa-masak-not"));            // başlık zaten söylüyor: aynı bilgi ikinci kez yazılmaz
        Assert.Empty(e.FindAll("#degismez-ipucu-MasakKimlikTespiti"));   // ikinci "değiştirilemez" kutusu YOK
        Assert.DoesNotContain("Fon Kaynağı", form);
        Assert.DoesNotContain("Ek Belge Referansı", form);
        Assert.Equal("true", e.Find("#masak-fon").GetAttribute("aria-required"));
        Assert.Equal("true", e.Find("#masak-ekbelge").GetAttribute("aria-required"));   // zorunluluk sunucu kuralıyla aynı
    }

    [Fact]
    public void SE13_iptal_bolumunde_tek_kilit_cumlesi_kutu_ve_duzeltme_satiri_yok()
    {
        Oturum("SAHIP");
        Yol("GET", "/satinalma?limit=200", Liste);
        Yol("GET", "/satinalma/s2", Kismi);
        var e = Render<Kuyumcu.Web.Pages.SatinAlma>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-ac-s2")));
        e.FindAll("#sa-ac-s2")[0].Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-detay-iptal-ac")));
        e.Find("#sa-detay-iptal-ac").Click();
        Assert.Equal(M.IptalKilit, e.Find("#sa-iptal-kilit").TextContent.Trim());
        Assert.Empty(e.FindAll(".degismez-ipucu"));                              // kilit kutusu / başlığı YOK
        Assert.Empty(e.FindAll(".degismez-alternatif"));                         // "Düzeltme şöyle yapılır" YOK
        Assert.DoesNotContain(M.DegismezYol, e.Markup);                          // ikinci alış iadesi cümlesi YOK
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(e.Markup, "alış iadesi"));

        var r = new SatinalmaEkranKalanlarEkranTestleri();
        r.Oturum("SAHIP");
        r.Yol("GET", "/irsaliyeler?limit=200", IrsListe);
        r.Yol("GET", "/irsaliyeler/i1", IrsSevk.Replace("\"hareketId\":null", "\"hareketId\":\"h1\""));
        var c = r.Render<Kuyumcu.Web.Pages.Irsaliyeler>();
        c.WaitForAssertion(() => Assert.NotEmpty(c.FindAll("#irs-ac-i1")));
        c.FindAll("#irs-ac-i1")[0].Click();
        c.WaitForAssertion(() => Assert.NotEmpty(c.FindAll("#irs-detay-iptal-ac")));
        c.Find("#irs-detay-iptal-ac").Click();
        Assert.Equal(M.DefterBagliIpucu, c.Find("#irs-iptal-kilit").TextContent.Trim());
        Assert.Empty(c.FindAll(".degismez-ipucu"));
        Assert.Empty(c.FindAll(".degismez-alternatif"));
        Assert.DoesNotContain(M.DegismezYol, c.Markup);
    }

    [Fact]
    public void SE12_irsaliye_listesinde_yon_hucresi_tek_satir()
    {
        Oturum("SAHIP");
        Yol("GET", "/irsaliyeler?limit=200", IrsListe);
        var e = Render<Kuyumcu.Web.Pages.Irsaliyeler>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#irs-yon-i1")));
        Assert.Contains("hucre-tek-satir", e.Find("#irs-yon-i1").ClassName);
    }

    [Fact]
    public void SE08_duzenleme_formunda_dolu_fiyat_odakta_binlik_ayracsiz_ham_gorunur()
    {
        var e = SiparisAc("s1", Taslak.Replace("\"birimFiyatTl\":2000", "\"birimFiyatTl\":42500"));
        e.Find("#sa-detay-duzenle").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-form-fiyat-0")));
        Assert.Equal("42.500", e.Find("#sa-form-fiyat-0").GetAttribute("value"));   // odak dışı: biçimli
        e.Find("#sa-form-fiyat-0").Focus();
        e.WaitForAssertion(() => Assert.Equal("42500", e.Find("#sa-form-fiyat-0").GetAttribute("value")));   // odakta: ham
    }

    [Fact]
    public void SE07_iptal_siparis_ve_irsaliye_aktife_al_yolu_POST_atar_ve_bandi_yazar()
    {
        Oturum("SAHIP");
        Yol("GET", "/satinalma?limit=200", Liste);
        Yol("GET", "/satinalma/s3", IptalSiparis);
        var e = Render<Kuyumcu.Web.Pages.SatinAlma>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-gecmis-ac")));
        e.Find("#sa-gecmis-ac").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-ac-s3")));
        e.FindAll("#sa-ac-s3")[0].Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-aktife-al")));
        Yol("POST", "/satinalma/s3/aktife-al", Taslak.Replace("\"s1\"", "\"s3\"").Replace("SAS-2026-00001", "SAS-2026-00003"));
        Assert.Contains(M.AktifeAl, e.Find("#sa-aktife-al").TextContent);
        e.Find("#sa-aktife-al").Click();
        e.WaitForAssertion(() => Assert.Single(Istekler("POST"), i => i.Yol.EndsWith("/satinalma/s3/aktife-al")));
        e.WaitForAssertion(() => Assert.Contains("SAS-2026-00003", e.Find("#sa-basari").TextContent));
        Assert.Empty(e.FindAll("#sa-aktife-al"));                  // artık taslak: düğme yok, Onayla var

        var i = new SatinalmaEkranKalanlarEkranTestleri();
        i.Oturum("SAHIP");
        i.Yol("GET", "/irsaliyeler?limit=200", IrsListe);
        i.Yol("GET", "/irsaliyeler/i2", IrsIptal);
        i.Yol("POST", "/irsaliyeler/i2/aktife-al", IrsIptal.Replace("\"IPTAL\"", "\"ACIK\""));
        var r = i.Render<Kuyumcu.Web.Pages.Irsaliyeler>();
        r.Find("#irs-gecmis-ac").Click();
        r.WaitForAssertion(() => Assert.NotEmpty(r.FindAll("#irs-ac-i2")));
        r.FindAll("#irs-ac-i2")[0].Click();
        r.WaitForAssertion(() => Assert.NotEmpty(r.FindAll("#irs-aktife-al")));
        r.Find("#irs-aktife-al").Click();
        r.WaitForAssertion(() => Assert.Single(i.Istekler("POST"), x => x.Yol.EndsWith("/irsaliyeler/i2/aktife-al")));
        r.WaitForAssertion(() => Assert.Contains("IRS-2026-00002", r.Find("#irs-basari").TextContent));
    }
}

[Collection("http")]
public sealed class SatinalmaEkranKalanlarHttpTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "PAKETSATINALMA-EKRAN-TEST " + ad);
        db.SaveChanges();
        return k;
    }

    private static async Task<JsonElement> Tamam(HttpResponseMessage r)
    {
        var govde = await r.Metin();
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"{(int)r.StatusCode}: {govde}");
        return JsonDocument.Parse(govde).RootElement;
    }

    private static async Task<string> SiparisAc(HttpClient c, Kiraci k)
    {
        var y = await Tamam(await c.PostJson("/api/v1/satinalma", new
        {
            tedarikciCariId = k.CariId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 5m, birimFiyatTl = 1_000m } },
        }));
        return y.GetProperty("id").GetString()!;
    }

    private static string Gerekce => "tedarikçi vazgeçti, iptal";

    [Fact]
    public async Task SH01_siparis_iptal_aktife_al_taslaga_doner_hasimlar_reddedilir()
    {
        var k = Kur("SH01");
        var c = f.Ortam.Istemci(k, "MUDUR");
        var id = await SiparisAc(c, k);

        // Taslak/aktif sipariş aktife alınamaz.
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync($"/api/v1/satinalma/{id}/aktife-al", null)).StatusCode);

        var iptal = await Tamam(await c.PostJson($"/api/v1/satinalma/{id}/iptal", new { gerekce = Gerekce }));
        Assert.Equal("IPTAL", iptal.GetProperty("durum").GetString());

        // Başka kiracı 404, tezgâhtar 403 — durum değişmez.
        var diger = Kur("SH01-diger");
        Assert.Equal(HttpStatusCode.NotFound, (await f.Ortam.Istemci(diger, "MUDUR").PostAsync($"/api/v1/satinalma/{id}/aktife-al", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsync($"/api/v1/satinalma/{id}/aktife-al", null)).StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal("IPTAL", await db.SatinalmaSiparisleri.AsNoTracking().Where(s => s.Id == id).Select(s => s.Durum).SingleAsync());

        var geri = await Tamam(await c.PostAsync($"/api/v1/satinalma/{id}/aktife-al", null));
        Assert.Equal("TASLAK", geri.GetProperty("durum").GetString());
        Assert.True(geri.GetProperty("duzenlenebilir").GetBoolean());
        Assert.Equal(JsonValueKind.Null, geri.GetProperty("iptalGerekce").ValueKind);
        using (var db = f.Vt.OlusturSistem())
        {
            var s = await db.SatinalmaSiparisleri.AsNoTracking().SingleAsync(x => x.Id == id);
            Assert.Equal("TASLAK", s.Durum);
            Assert.Null(s.IptalGerekce);
            // Denetim kaydı yazıldı.
            Assert.True(await db.DenetimKayitlari.AsNoTracking().AnyAsync(d => d.TenantId == k.TenantId && d.VarlikId == id && d.Detay!.Contains("aktife alındı")));
        }
        // Geri alınan sipariş yeniden iptal edilebilir; ikinci aktife-al taslağa 409.
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync($"/api/v1/satinalma/{id}/aktife-al", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PostJson($"/api/v1/satinalma/{id}/iptal", new { gerekce = Gerekce })).StatusCode);
    }

    [Fact]
    public async Task SH02_sevk_irsaliyesi_iptal_aktife_al_acik_olur_hasimlar_reddedilir()
    {
        var k = Kur("SH02");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var iyi = await Tamam(await c.PostJson("/api/v1/irsaliyeler", new
        {
            yon = "GIDEN", cariId = k.CariId, kalemler = new[] { new { urunId = k.UrunId, adet = 2m } },
        }));
        var id = iyi.GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync($"/api/v1/irsaliyeler/{id}/aktife-al", null)).StatusCode);
        await Tamam(await c.PostJson($"/api/v1/irsaliyeler/{id}/iptal", new { gerekce = Gerekce }));

        var diger = Kur("SH02-diger");
        Assert.Equal(HttpStatusCode.NotFound, (await f.Ortam.Istemci(diger, "SAHIP").PostAsync($"/api/v1/irsaliyeler/{id}/aktife-al", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsync($"/api/v1/irsaliyeler/{id}/aktife-al", null)).StatusCode);

        var geri = await Tamam(await c.PostAsync($"/api/v1/irsaliyeler/{id}/aktife-al", null));
        Assert.Equal("ACIK", geri.GetProperty("durum").GetString());
        using var db = f.Vt.OlusturSistem();
        var i = await db.Irsaliyeler.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("ACIK", i.Durum);
        Assert.Null(i.IptalGerekce);
    }

    [Fact]
    public async Task SH03_irsaliye_dokumunde_teslim_adresi_gelende_isletme_gidende_cari_yoksa_uydurulmaz()
    {
        var k = Kur("SH03");
        var c = f.Ortam.Istemci(k, "MUDUR");
        // Adres kaydı yokken: UYDURULMAZ, eksik olduğu yazılır.
        var giden = await Tamam(await c.PostJson("/api/v1/irsaliyeler", new
        {
            yon = "GIDEN", cariId = k.CariId, kalemler = new[] { new { urunId = k.UrunId, adet = 1m } },
        }));
        var gId = giden.GetProperty("id").GetString()!;
        var bos = await Tamam(await c.GetAsync($"/api/v1/cikti/{BelgeTurleri.Irsaliye}/{gId}"));
        var bosNot = bos.TryGetProperty("not", out var bn) && bn.ValueKind == JsonValueKind.String ? bn.GetString() : null;
        Assert.DoesNotContain("Teslim adresi", bosNot ?? "");
        Assert.DoesNotContain("kart bilgisinden", bosNot ?? "");

        using (var db = f.Vt.OlusturSistem())
        {
            (await db.Cariler.SingleAsync(x => x.Id == k.CariId)).Adres = "Kapalıçarşı Kuyumcular Sk. No:7 Fatih";
            (await db.Tenants.SingleAsync(x => x.Id == k.TenantId)).Adres = "Bedesten Cd. No:3 Eminönü";
            await db.SaveChangesAsync();
        }
        // GİDEN: malı alan tarafın (cari) adresi.
        var gDok = await Tamam(await c.GetAsync($"/api/v1/cikti/{BelgeTurleri.Irsaliye}/{gId}"));
        Assert.Contains("Teslim adresi: Kapalıçarşı Kuyumcular Sk. No:7 Fatih", gDok.GetProperty("not").GetString());
        Assert.DoesNotContain("Bedesten", gDok.GetProperty("not").GetString());

        // GELEN (mal kabul): malın geldiği yer = işletme adresi.
        var sid = await SiparisAc(c, k);
        var kalem = (await Tamam(await c.GetAsync($"/api/v1/satinalma/{sid}"))).GetProperty("kalemler")[0].GetProperty("id").GetString()!;
        await Tamam(await c.PostAsync($"/api/v1/satinalma/{sid}/onayla", null));
        var kabul = await Tamam(await c.PostJson($"/api/v1/satinalma/{sid}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 1m } } }));
        var irs = kabul.GetProperty("irsaliyeId").GetString()!;
        var dok = await Tamam(await c.GetAsync($"/api/v1/cikti/{BelgeTurleri.Irsaliye}/{irs}"));
        Assert.Contains("Teslim adresi: Bedesten Cd. No:3 Eminönü", dok.GetProperty("not").GetString());
        Assert.DoesNotContain("Kapalıçarşı", dok.GetProperty("not").GetString());
    }
}
