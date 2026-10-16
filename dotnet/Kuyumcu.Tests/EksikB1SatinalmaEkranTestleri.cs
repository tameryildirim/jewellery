using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET EKSIK-B1-SATINALMA — Satın alma ve İrsaliyeler EKRANLARI (bUnit) + menü kaydı.
//   EK01 açılış yalnız bekleyen siparişler; geçmiş KAPALI akordeon; ham durum kodu basılmaz
//   EK02 yazma yetkisi yok (MUHASEBE) → "+ Yeni sipariş" kilitli + sebep; SAHİP açık
//   EK03 mal kabul: kalan önceden dolu, aşım → birincil kilit + sebep (istek YOK), geçerli → POST gövdesi + bant
//   EK04 boş form gönderimi → istek YOK, alan altı mesaj + kilit sebebi
//   EK05 liste hatası → hata bandı, boş durum YOK (UX-S28)
//   EK06 irsaliyeler: bağsız sayısı, yazdır bağlantısı (dönüş yolu), geçmiş kapalı
//   EK07 irsaliye düzenleme: değişiklik yokken kilit; değişen alan YALNIZ gövdede (PATCH sözleşmesi)
//   EK08 menü: TR+EN ad, rota başlığı, tezgâhtar görmez, sahip/müdür/muhasebe görür
//   EK09 (tur 3) eşik üstü mal kabul: MASAK formu açılır, kilit + sebep, eşik altında kapanır, gövdede masak
// ═════════════════════════════════════════════════════════════════════════════

public sealed class EksikB1SatinalmaEkranTestleri : BunitContext
{
    private const string Liste = """
        {"satirlar":[
          {"id":"s1","no":"SAS-2026-00001","durum":"KISMI_KABUL","subeId":"sb","tedarikciCariId":"c1","tedarikciAdi":"Has Toptan A.Ş.","beklenenTarih":"2026-10-20","aciklama":null,"hesapKodu":null,"olusturuldu":"2026-10-09T08:00:00Z","guncellendi":"2026-10-09T08:00:00Z","kalemSayisi":1},
          {"id":"s2","no":"SAS-2026-00002","durum":"KAPANDI","subeId":"sb","tedarikciCariId":"c1","tedarikciAdi":"Has Toptan A.Ş.","beklenenTarih":null,"aciklama":null,"hesapKodu":null,"olusturuldu":"2026-10-08T08:00:00Z","guncellendi":"2026-10-08T08:00:00Z","kalemSayisi":2}
        ],"toplam":2,"kirilim":[{"durum":"KISMI_KABUL","adet":1},{"durum":"KAPANDI","adet":1}],"limit":200,"azamiLimit":200,"sinirDegdi":false}
        """;

    private const string Detay = """
        {"id":"s1","no":"SAS-2026-00001","durum":"KISMI_KABUL","subeId":"sb","tedarikciCariId":"c1","tedarikciAdi":"Has Toptan A.Ş.",
         "beklenenTarih":"2026-10-20","aciklama":null,"hesapKodu":null,"iptalGerekce":null,
         "olusturuldu":"2026-10-09T08:00:00Z","guncellendi":"2026-10-09T08:00:00Z",
         "kalemler":[{"id":"k1","urunId":"u1","urunAdi":"22 ayar bilezik","birim":"ADET","tanim":null,"adet":5,"birimFiyatTl":2000,"kabulEdilenAdet":2,"kalanAdet":3,"sira":0}],
         "irsaliyeler":[{"id":"i1","no":"IRS-2026-00001","tarih":"2026-10-09T08:10:00Z","durum":"ACIK","faturaId":null}],
         "duzenlenebilir":false,"malKabulEdilebilir":true,"kilitSebebi":"Onaylanmış siparişin kalemleri değiştirilemez; kalan kalem mal kabulüyle kapanır."}
        """;

    private const string IrsListe = """
        {"satirlar":[
          {"id":"i1","no":"IRS-2026-00001","yon":"GELEN","durum":"ACIK","cariId":"c1","cariAdi":"Has Toptan A.Ş.","subeId":"sb","tarih":"2026-10-09T08:10:00Z","faturaId":null,"satinalmaSiparisId":"s1","aciklama":null,"hesapKodu":null,"bagsiz":true,"kalemSayisi":1},
          {"id":"i2","no":"IRS-2026-00002","yon":"GIDEN","durum":"IPTAL","cariId":"c2","cariAdi":"Ayşe Demir","subeId":"sb","tarih":"2026-10-08T08:10:00Z","faturaId":null,"satinalmaSiparisId":null,"aciklama":null,"hesapKodu":null,"bagsiz":false,"kalemSayisi":1}
        ],"toplam":2,"bagsizToplam":1,"limit":200,"azamiLimit":200,"sinirDegdi":false}
        """;

    private const string IrsDetay = """
        {"id":"i1","no":"IRS-2026-00001","yon":"GELEN","durum":"ACIK","cariId":"c1","cariAdi":"Has Toptan A.Ş.","subeId":"sb",
         "tarih":"2026-10-09T08:10:00Z","satinalmaSiparisId":"s1","faturaId":null,"aciklama":"ilk not","hesapKodu":null,"iptalGerekce":null,
         "olusturuldu":"2026-10-09T08:10:00Z","guncellendi":"2026-10-09T08:10:00Z",
         "kalemler":[{"id":"ik1","urunId":"u1","urunAdi":"22 ayar bilezik","birim":"ADET","tanim":null,"adet":2,"hareketId":"h1","sira":0}],
         "bagsiz":true,"duzenlenebilir":true,"kilitSebebi":null}
        """;

    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly List<(string Metot, string Parca, HttpStatusCode Kod, string Govde)> yollar = [];

    public EksikB1SatinalmaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();      // tur 3: mal kabul MASAK gereksinimini sorar
    }

    private sealed class SahteApi(EksikB1SatinalmaEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            foreach (var (m, p, k, g) in t.yollar)
                if (m == istek.Method.Method && yol.Contains(p))
                    return new HttpResponseMessage(k) { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
            // TUR 3 — gerçekçi MASAK yanıtları (sunucu MasakKuralMotoru şekli; eşik 370.000 kimlik):
            // canlı 5166 /masak/esikler ile aynı: 200.000 altı YOK, 200.000+ kimlik + amaç, 2.000.000,01+ ayrıca fon kaynağı (canlı 5166 yanıtıyla aynı kademe).
            if (yol.Contains("/masak/esikler"))
                return Json("""{"kimlikEsigi":370000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000.01,"ekBelgeEsigi":20000000.01,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""");
            if (yol.Contains("/masak/gereksinim"))
            {
                var m2 = System.Text.RegularExpressions.Regex.Match(yol, @"tutar=([0-9.]+)");
                var tutar = m2.Success ? decimal.Parse(m2.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0m;
                return Json(tutar < 200000m ? """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":0}"""
                    : tutar < 2000000.01m
                    ? """{"kademe":"AMAC_BEYANI","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}"""
                    : """{"kademe":"BEYAN_FORMU","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":true,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""");
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            { Content = new StringContent("""{"message":"tanımsız uç"}""", System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private static HttpResponseMessage Json(string g) =>
        new(HttpStatusCode.OK) { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };

    private void Yol(string metot, string parca, string govde, HttpStatusCode kod = HttpStatusCode.OK) =>
        yollar.Add((metot, parca, kod, govde));

    private void Oturum(string rol, string mod = ArayuzModuServisi.Profesyonel) =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "sb", null, mod));

    private List<(string Metot, string Yol, string Govde)> Istekler(string metot)
    {
        lock (istekler) return istekler.Where(i => i.Metot == metot).ToList();
    }

    private static SatinAlmaMetinleri M => SatinAlmaSozlugu.Tr;

    // ── EK01 ────────────────────────────────────────────────────────────────
    [Fact]
    public void EK01_acilista_yalniz_bekleyen_siparis_gecmis_kapali_ham_kod_yok()
    {
        Oturum("SAHIP");
        Yol("GET", "/satinalma?limit=200", Liste);
        var e = Render<Kuyumcu.Web.Pages.SatinAlma>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-ac-s1")));

        Assert.Empty(e.FindAll("#sa-ac-s2"));                                  // geçmiş gövdesi DOM'da yok
        Assert.Equal("false", e.Find("#sa-gecmis-ac").GetAttribute("aria-expanded"));
        Assert.Equal("1", e.Find("#sa-gecmis-sayi").TextContent.Trim());
        Assert.DoesNotContain("KISMI_KABUL", e.Markup);
        Assert.Contains(M.DurumKismi, e.Markup);

        e.Find("#sa-gecmis-ac").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-ac-s2")));
        Assert.Contains(M.DurumKapandi, e.Markup);
        Assert.Single(Istekler("GET"), i => i.Yol.Contains("/satinalma"));    // akordeon ek istek ATMAZ
    }

    // ── EK02 ────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("MUHASEBE", true)]
    [InlineData("SAHIP", false)]
    [InlineData("MUDUR", false)]
    public void EK02_yazma_yetkisi_yoksa_yeni_siparis_kilitli_ve_sebebi_yazili(string rol, bool kilitli)
    {
        Oturum(rol);
        Yol("GET", "/satinalma?limit=200", Liste);
        var e = Render<Kuyumcu.Web.Pages.SatinAlma>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-ac-s1")));
        Assert.Equal(kilitli, e.Find("#sa-yeni").HasAttribute("disabled"));
        Assert.Equal(kilitli, e.FindAll("#sa-yeni-kilit").Count == 1);
        if (kilitli) Assert.Equal(M.YazmaYetkisiYok, e.Find("#sa-yeni-kilit").TextContent.Trim());
    }

    // ── EK03 ────────────────────────────────────────────────────────────────
    [Fact]
    public void EK03_mal_kabul_kalan_dolu_asim_kilitler_gecerli_POST_ve_bant()
    {
        Oturum("SAHIP");
        Yol("GET", "/satinalma?limit=200", Liste);
        Yol("GET", "/satinalma/s1", Detay);
        Yol("GET", "/kur/guncel", """{"id":"kur1"}""");
        Yol("POST", "/satinalma/s1/mal-kabul", "{\"siparis\":" + Detay.Replace("\"KISMI_KABUL\"", "\"KAPANDI\"")
            + ",\"irsaliyeId\":\"i9\",\"irsaliyeNo\":\"IRS-2026-00009\",\"kabulEdilenKalem\":1,\"yeniDurum\":\"KAPANDI\"}");
        var e = Render<Kuyumcu.Web.Pages.SatinAlma>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-ac-s1")));
        e.FindAll("#sa-ac-s1")[0].Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-kabul-0")));

        // TUR 2 (gerçek Chrome bulgusu): çekmecede kalemler KART listesidir — tablo yok, "Bu teslimatta gelen"
        // etiketi kalem başına TEK kez (sütun başlığı tekrarı yok); özet listesi iki sütunlu ortak düzende (uy-tanim).
        Assert.Empty(e.FindAll("#sa-kalem-liste table"));
        Assert.Single(e.FindAll("#sa-kalem-0 .alan-etiket-metin"), x => x.TextContent.Trim() == M.BuTeslimat);
        Assert.Contains("uy-tanim", e.Find("#sa-detay-bilgi").GetAttribute("class"));

        // Kalan (3) önceden dolu; birincil düğme açık ve adı "Malı teslim al".
        Assert.Equal("3", e.FindAll("#sa-kabul-0")[0].GetAttribute("value"));
        var birincil = e.Find("#sa-detay-birincil");
        Assert.Equal(M.MalKabulYap, birincil.TextContent.Trim());
        Assert.False(birincil.HasAttribute("disabled"));

        // Aşım: 4 > kalan 3 → kilit + sebep, istek YOK.
        e.FindAll("#sa-kabul-0")[0].Input("4");
        e.WaitForAssertion(() => Assert.True(e.Find("#sa-detay-birincil").HasAttribute("disabled")));
        // Sebep: ortak sayı kutusunun EnCok (= kalan) iletisi — alan altı ve kilitte AYNI metin (UX-S30).
        Assert.Contains("3", e.Find("#sa-detay-kilit").TextContent);
        e.Find("#sa-detay-birincil").Click();
        Assert.Empty(Istekler("POST"));

        // Geçerli: 2 → POST gövdesi kur kimliği + kalem + adet; bant irsaliye no.
        e.FindAll("#sa-kabul-0")[0].Input("2");
        e.WaitForAssertion(() => Assert.False(e.Find("#sa-detay-birincil").HasAttribute("disabled")));
        e.Find("#sa-detay-birincil").Click();
        e.WaitForAssertion(() => Assert.Single(Istekler("POST")));
        var g = System.Text.Json.JsonDocument.Parse(Istekler("POST")[0].Govde).RootElement;
        Assert.Equal("kur1", g.GetProperty("kurSnapshotId").GetString());
        Assert.Equal("k1", g.GetProperty("kalemler")[0].GetProperty("kalemId").GetString());
        Assert.Equal(2m, g.GetProperty("kalemler")[0].GetProperty("adet").GetDecimal());
        // TUR 3: eşik altı (2 × 2.000) → MASAK formu yok, gövdede masak null (kişisel veri gönderilmez).
        Assert.Equal(System.Text.Json.JsonValueKind.Null, g.GetProperty("masak").ValueKind);
        Assert.Empty(e.FindAll("#sa-masak"));
        e.WaitForAssertion(() => Assert.Contains("IRS-2026-00009", e.Find("#sa-basari").TextContent));
        Assert.Contains(M.DurumKapandi, e.Find("#sa-basari").TextContent);
    }

    // ── EK09 (tur 3 — hasım KRİTİK) ─────────────────────────────────────────
    /// <summary>
    /// Eşik üstü mal kabul ÇIKMAZ SOKAK değildir: tutar (adet × siparişteki fiyat) tedarikçiyle birlikte
    /// sunucuya sorulur, gerekiyorsa ortak MASAK formu açılır; eksikken birincil kilitli + sebep (istek YOK),
    /// eşik altına inince form kapanır, dolunca POST gövdesi masak bloğunu taşır.
    /// </summary>
    [Fact]
    public void EK09_esik_ustu_mal_kabulde_masak_formu_acilir_kilit_sebebi_ve_govdede_masak()
    {
        Oturum("SAHIP");
        var buyuk = Detay.Replace("\"birimFiyatTl\":2000", "\"birimFiyatTl\":150000");
        Yol("GET", "/satinalma?limit=200", Liste);
        Yol("GET", "/satinalma/s1", buyuk);
        Yol("GET", "/kur/guncel", """{"id":"kur1"}""");
        Yol("POST", "/satinalma/s1/mal-kabul", "{\"siparis\":" + buyuk.Replace("\"KISMI_KABUL\"", "\"KAPANDI\"")
            + ",\"irsaliyeId\":\"i9\",\"irsaliyeNo\":\"IRS-2026-00009\",\"kabulEdilenKalem\":1,\"yeniDurum\":\"KAPANDI\"}");
        var e = Render<Kuyumcu.Web.Pages.SatinAlma>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-ac-s1")));
        e.FindAll("#sa-ac-s1")[0].Click();

        // Kalan 3 × 150.000 = 450.000 → form açık, tedarikçi (c1) ile sorulur (bağlantılı işlemler toplamı).
        e.WaitForElement("#masak-belge-no");
        Assert.Contains(Istekler("GET"), i => i.Yol.Contains("/masak/gereksinim?tutar=450000") && i.Yol.Contains("cariId=c1"));
        Assert.NotEmpty(e.FindAll("#masak-amac"));
        Assert.Empty(e.FindAll("#masak-fon"));                                // fon kaynağı yalnız beyan formu kademesinde
        Assert.True(e.Find("#sa-detay-birincil").HasAttribute("disabled"));
        Assert.Contains(M.MasakKabulKilit, e.Find("#sa-detay-kilit").TextContent);
        e.Find("#sa-detay-birincil").Click();
        Assert.Empty(Istekler("POST"));

        // Eşik altına inince (1 × 150.000) form kapanır, düğme açılır — gereksiz kimlik istenmez.
        e.FindAll("#sa-kabul-0")[0].Input("1");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#masak-belge-no")));
        Assert.False(e.Find("#sa-detay-birincil").HasAttribute("disabled"));

        // Yeniden 3 → form; alanlar dolunca POST gövdesinde masak bloğu.
        e.FindAll("#sa-kabul-0")[0].Input("3");
        e.WaitForElement("#masak-belge-no");
        e.Find("#masak-belge-no").Input("10000000146");
        e.Find("#masak-ad-soyad").Input("Ayşe Demir");
        e.Find("#masak-amac").Input("Ticari mal alımı");
        e.Find("#masak-aciklama").Input("Toptancıdan işletme stoğu için bilezik alımı");
        e.WaitForAssertion(() => Assert.False(e.Find("#sa-detay-birincil").HasAttribute("disabled")));
        e.Find("#sa-detay-birincil").Click();
        e.WaitForAssertion(() => Assert.Single(Istekler("POST")));
        var g = System.Text.Json.JsonDocument.Parse(Istekler("POST")[0].Govde).RootElement;
        var masak = g.GetProperty("masak");
        Assert.Equal("10000000146", masak.GetProperty("belgeNo").GetString());
        Assert.Equal("Ticari mal alımı", masak.GetProperty("islemAmaci").GetString());
        Assert.Equal(3m, g.GetProperty("kalemler")[0].GetProperty("adet").GetDecimal());
        e.WaitForAssertion(() => Assert.Contains("IRS-2026-00009", e.Find("#sa-basari").TextContent));
    }

    // ── EK04 ────────────────────────────────────────────────────────────────
    [Fact]
    public void EK04_bos_siparis_formu_istek_atmaz_kilit_sebebi_ve_alan_mesaji()
    {
        Oturum("SAHIP");
        Yol("GET", "/satinalma?limit=200", Liste);
        var e = Render<Kuyumcu.Web.Pages.SatinAlma>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sa-ac-s1")));
        e.Find("#sa-yeni").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#cekmece-sa-form")));

        var kaydet = e.Find("#cekmece-sa-form-kaydet");
        Assert.True(kaydet.HasAttribute("disabled"));
        Assert.Contains(M.TedarikciSecin, e.Find("#cekmece-sa-form-kilit").TextContent);
        Assert.Empty(Istekler("POST"));
        // Satır ekle/çıkar düğmeleri gerçek iş yapar (DoD EK md.1).
        e.Find("#sa-form-satir-ekle").Click();
        Assert.Equal(2, e.FindAll("fieldset[id^='sa-form-satir-']").Count);
        e.Find("#sa-form-sil-1").Click();
        Assert.Single(e.FindAll("fieldset[id^='sa-form-satir-']"));
    }

    // ── EK05 ────────────────────────────────────────────────────────────────
    [Fact]
    public void EK05_liste_hatasi_bos_duruma_cevrilmez_hata_bandi_gorunur()
    {
        Oturum("SAHIP");
        Yol("GET", "/satinalma?limit=200", """{"message":"Sunucu hatası (deneme)"}""", HttpStatusCode.InternalServerError);
        var e = Render<Kuyumcu.Web.Pages.SatinAlma>();
        e.WaitForAssertion(() => Assert.Contains("Sunucu hatası", e.Find("#sa-hata").TextContent));
        Assert.DoesNotContain(M.SiparisYok, e.Markup);
    }

    // ── EK06 ────────────────────────────────────────────────────────────────
    [Fact]
    public void EK06_irsaliyeler_bagsiz_sayisi_yazdir_baglantisi_ve_gecmis_kapali()
    {
        Oturum("MUDUR");
        Yol("GET", "/irsaliyeler?limit=200", IrsListe);
        var e = Render<Kuyumcu.Web.Pages.Irsaliyeler>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#irs-ac-i1")));
        Assert.Contains(string.Format(M.Bagsizlar, 1), e.Find("#irs-bagsiz-sayi").TextContent);
        Assert.Equal("/cikti/IRSALIYE/i1?donus=/irsaliyeler", e.FindAll("#irs-yazdir-i1")[0].GetAttribute("href"));
        Assert.Empty(e.FindAll("#irs-ac-i2"));
        Assert.Contains(M.YonGelen, e.Markup);
        Assert.DoesNotContain(">ACIK<", e.Markup);
    }

    // ── EK07 ────────────────────────────────────────────────────────────────
    [Fact]
    public void EK07_irsaliye_duzenleme_degisiklik_yokken_kilitli_degisen_alan_yalniz_govdede()
    {
        Oturum("SAHIP");
        Yol("GET", "/irsaliyeler?limit=200", IrsListe);
        Yol("GET", "/irsaliyeler/i1", IrsDetay);
        Yol("PATCH", "/irsaliyeler/i1", IrsDetay.Replace("ilk not", "ikinci not"));
        var e = Render<Kuyumcu.Web.Pages.Irsaliyeler>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#irs-ac-i1")));
        e.FindAll("#irs-ac-i1")[0].Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#irs-detay-aciklama")));

        Assert.True(e.Find("#irs-detay-kaydet").HasAttribute("disabled"));
        Assert.Contains(M.DegisiklikYok, e.Find("#irs-detay-kilit").TextContent);
        // Mal kabulden doğan (deftere bağlı) irsaliye iptal edilemez → gerekçe + yol (UX-S25).
        e.Find("#irs-detay-iptal-ac").Click();
        Assert.Contains(M.DefterBagliIpucu, e.Markup);

        e.Find("#irs-detay-aciklama").Input("ikinci not");
        e.WaitForAssertion(() => Assert.False(e.Find("#irs-detay-kaydet").HasAttribute("disabled")));
        e.Find("#irs-detay-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(Istekler("PATCH")));
        var g = System.Text.Json.JsonDocument.Parse(Istekler("PATCH")[0].Govde).RootElement;
        Assert.Equal("ikinci not", g.GetProperty("aciklama").GetString());
        Assert.False(g.TryGetProperty("tarih", out _));                        // gönderilmeyen alan KORUNUR
        Assert.False(g.TryGetProperty("hesapKodu", out _));
        e.WaitForAssertion(() => Assert.Contains("IRS-2026-00001", e.Find("#irs-basari").TextContent));
    }

    // ── EK08 ────────────────────────────────────────────────────────────────
    [Fact]
    public void EK08_menu_TR_EN_ad_rota_basligi_ve_rol_suzgeci()
    {
        foreach (var yol in new[] { "/satin-alma", "/irsaliyeler" })
        {
            var oge = MenuTanimi.Ogeler.Single(o => o.Yol == yol);
            Assert.False(string.IsNullOrWhiteSpace(oge.Etiket(Sozlukler.Tr)));
            Assert.False(string.IsNullOrWhiteSpace(oge.Etiket(Sozlukler.En)));
            Assert.NotEqual(oge.Etiket(Sozlukler.Tr), oge.Etiket(Sozlukler.En));
            Assert.NotNull(RotaBasliklari.Bul(yol));
            Assert.False(MenuTanimi.YetkiliMi(oge, _ => true, "TEZGAHTAR"));
            foreach (var rol in new[] { "SAHIP", "MUDUR", "MUHASEBE" })
                Assert.True(MenuTanimi.YetkiliMi(oge, _ => true, rol));
        }
        Assert.Equal("Satın alma", MenuAdlari.Tr.SatinAlma.Ad);
        Assert.Equal("İrsaliyeler", MenuAdlari.Tr.Irsaliyeler.Ad);
        Assert.Equal("Purchasing", MenuAdlari.En.SatinAlma.Ad);
        Assert.Equal("Waybills", MenuAdlari.En.Irsaliyeler.Ad);
    }
}
