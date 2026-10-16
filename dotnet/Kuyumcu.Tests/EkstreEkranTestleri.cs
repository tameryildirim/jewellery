using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 32 — EKSTRE EKRANLARI [bUnit]
//
// Sınananlar:
//   1. /banka'daki 'Ekstre Yükle' base64 gövdeyle DOĞRU uca gider
//   2. Eşleştirme ekranı ilk açılışta SAYFALI ister (limit=100&offset=0)
//   3. MUKERRER satırın onay kutusu VARSAYILAN OLARAK İŞARETSİZ
//   4. Hiç satır seçilmemişken 'Seçilileri Onayla' KAPALI ve sebebi YAZILI
//   5. Cari seçilince PATCH doğru gövdeyle gider (yalnız DEĞİŞEN alan)
//   6. HATALI satırın hata mesajı EKRANDA görünür (sessizce gizlenmez)
//   7. ONAYLANDI satırda düzenleme yerine DegismezIpucu gösterilir
//
// ⚠ Sahte API '{}' varsayılanıyla BIRAKILMADI: bu paketin dokunduğu her uca
//   SUNUCU DTO'SUYLA BİREBİR alan adı/tipinde gerçekçi yanıt verildi —
//   uyumsuz alan adı geçmişte çalışma zamanı tip hatası üretmişti.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class EkstreEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly SahteApi sahte = new();

    public EkstreEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        sahte.Kayit = istekler;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(sahte) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        public List<(string, string, string)> Kayit = [];

        /// <summary>Q10-5: doluysa ekstre ayrıntısı bu gövdeyle döner.</summary>
        public string? DetayYaniti;

        /// <summary>Sunucunun EkstreServisi.Getir yanıtıyla BİREBİR alan adı ve tipleri.</summary>
        private const string Detay = """
        {"ithalatId":"i1","bankaHesapId":"b1","hesapAdi":"Ziraat — Ana Hesap","paraBirimi":"TRY",
         "dosyaAdi":"eylul.csv","format":"CSV","durum":"PROVA","satirSayisi":3,"yeniSayisi":1,
         "mukerrerSayisi":1,"hataliSayisi":1,"onaylananSayisi":0,"acilisBakiye":null,
         "kapanisBakiye":null,"olusturuldu":"2026-09-17T10:00:00Z","maksOnaySatir":500,
         "limit":100,"offset":0,"toplam":3,
         "satirlar":[
           {"id":"s1","satirNo":1,"hamSatir":"01.09.2026;HAVALE;;100,00","tarih":"2026-09-01T00:00:00Z",
            "yon":"GELEN","tutar":100.00,"paraBirimi":"TRY","karsiTarafAd":"ALİ VELİ",
            "karsiIban":"TR33 **** 1326","bankaReferans":"R1","aciklama":"HAVALE","durum":"YENI",
            "hataMesaji":null,"eslesenCariId":null,"eslesenCariAd":null,"eslesmeKaynagi":null,
            "bankaHareketId":null},
           {"id":"s2","satirNo":2,"hamSatir":"02.09.2026;TEKRAR;;100,00","tarih":"2026-09-02T00:00:00Z",
            "yon":"GELEN","tutar":100.00,"paraBirimi":"TRY","karsiTarafAd":"ALİ VELİ",
            "karsiIban":null,"bankaReferans":null,"aciklama":"TEKRAR","durum":"MUKERRER",
            "hataMesaji":null,"eslesenCariId":null,"eslesenCariAd":null,"eslesmeKaynagi":null,
            "bankaHareketId":null},
           {"id":"s3","satirNo":3,"hamSatir":"99.99.9999;BOZUK","tarih":"2026-09-03T00:00:00Z",
            "yon":"GELEN","tutar":0.00,"paraBirimi":"TRY","karsiTarafAd":null,
            "karsiIban":null,"bankaReferans":null,"aciklama":null,"durum":"HATALI",
            "hataMesaji":"Tarih çözülemedi: '99.99.9999'","eslesenCariId":null,"eslesenCariAd":null,
            "eslesmeKaynagi":null,"bankaHareketId":null}]}
        """;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            Kayit.Add((istek.Method.Method, yol, govde));

            var y = yol switch
            {
                var v when v.Contains("/ekstre/yukle") =>
                    """{"ithalatId":"i1","durum":"PROVA","dosyaAdi":"eylul.csv","satirSayisi":3,"yeniSayisi":1,"mukerrerSayisi":1,"hataliSayisi":1,"onaylananSayisi":0,"uretilenHareketSayisi":0,"maksOnaySatir":500}""",
                var v when v.Contains("/ekstre/satir/") && istek.Method == HttpMethod.Patch =>
                    """{"satir":{"id":"s1","satirNo":1,"hamSatir":"x","tarih":"2026-09-01T00:00:00Z","yon":"GELEN","tutar":100.00,"paraBirimi":"TRY","karsiTarafAd":"ALİ VELİ","karsiIban":null,"bankaReferans":null,"aciklama":null,"durum":"YENI","hataMesaji":null,"eslesenCariId":"c1","eslesenCariAd":"Ayşe Yılmaz","eslesmeKaynagi":"ELLE","bankaHareketId":null}}""",
                var v when v.Contains("/bankalar/ekstre/") =>
                    DetayYaniti ?? Detay,
                var v when v.Contains("/bankalar/") && v.Contains("/hareketler") =>
                    """{"hareketler":[]}""",
                var v when v.Contains("/bankalar") && istek.Method == HttpMethod.Get =>
                    """{"hesaplar":[{"id":"b1","bankaAdi":"Ziraat","hesapAdi":"Ana Hesap","hesapSahibi":"X Ltd.","iban":"TR200000000000000000000001","paraBirimi":"TRY","bakiye":0,"aktif":true}]}""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz"}]""",
                var v when v.Contains("/masak/esikler") =>
                    """{"kimlikEsigi":185000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000,"ekBelgeEsigi":20000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                _ => "{}",
            };

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.BankaEkstre> Ekran()
    {
        var e = Render<Kuyumcu.Web.Pages.BankaEkstre>(p => p.Add(x => x.IthalatId, "i1"));
        e.WaitForElement("#ekstre-onayla");
        return e;
    }

    // ── /banka — yükleme ────────────────────────────────────────────────────

    [Fact]
    public void Banka_ekraninda_Ekstre_Yukle_dugmesi_VARDIR_ve_cekmeceyi_acar()
    {
        var e = Render<Kuyumcu.Web.Pages.Banka>();
        e.WaitForElement("#bh-ekstre-yukle");

        e.Find("#bh-ekstre-yukle").Click();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ekstre-dosya")));
        // ⚠ Yüklemenin deftere hiçbir şey yazmadığı ÖNCEDEN yazar (UX-S27).
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#ekstre-yukle-not").TextContent));
    }

    [Fact]
    public void Dosya_secilmeden_yukleme_dugmesi_KAPALI_ve_SEBEBI_yazili()
    {
        var e = Render<Kuyumcu.Web.Pages.Banka>();
        e.WaitForElement("#bh-ekstre-yukle");
        e.Find("#bh-ekstre-yukle").Click();
        e.WaitForElement("#ekstre-dosya");

        // UX-S30: Cekmece kilitli kaydet düğmesinin sebebini gösterir.
        Assert.Contains("ekstre-dosya", e.Markup);
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/ekstre/yukle"));
    }

    // ── /banka/ekstre/{id} — eşleştirme ─────────────────────────────────────

    [Fact]
    public void Ilk_acilista_SAYFALI_istek_atilir_limit_100_offset_0()
    {
        Ekran();

        Assert.Contains(istekler, i =>
            i.Metot == "GET" && i.Yol.Contains("/bankalar/ekstre/i1") &&
            i.Yol.Contains("limit=100") && i.Yol.Contains("offset=0"));
    }

    [Fact]
    public void MUKERRER_satirin_onay_kutusu_VARSAYILAN_OLARAK_ISARETSIZ()
    {
        var e = Ekran();

        var kutu = e.Find("#ekstre-sec-s2");
        Assert.False(kutu.HasAttribute("checked"));
        Assert.False(e.Find("#ekstre-sec-s1").HasAttribute("checked"));
    }

    [Fact]
    public void HATALI_satirin_onay_kutusu_KAPALIDIR()
        => Assert.True(Ekran().Find("#ekstre-sec-s3").HasAttribute("disabled"));

    [Fact]
    public void Hic_satir_secilmemisken_onay_dugmesi_KAPALI_ve_sebebi_YAZILI()
    {
        var e = Ekran();

        Assert.True(e.Find("#ekstre-onayla").HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#ekstre-onay-sebep").TextContent));
    }

    [Fact]
    public void Satir_secilince_onay_dugmesi_ACILIR()
    {
        var e = Ekran();

        e.Find("#ekstre-sec-s1").Change(true);

        e.WaitForAssertion(() => Assert.False(e.Find("#ekstre-onayla").HasAttribute("disabled")));
    }

    [Fact]
    public void HATALI_satirin_hata_mesaji_EKRANDA_gorunur()
    {
        var e = Ekran();

        Assert.Contains("Tarih çözülemedi", e.Find("#ekstre-hata-s3").TextContent);
    }

    [Fact]
    public void Cari_secilince_PATCH_YALNIZ_cariId_govdesiyle_gider()
    {
        var e = Ekran();
        istekler.Clear();

        SeciciYardim.Sec(e, "ekstre-cari-s1", "c1");

        e.WaitForAssertion(() =>
        {
            var patch = Assert.Single(istekler, i => i.Metot == "PATCH");
            Assert.Contains("/bankalar/ekstre/satir/s1", patch.Yol);
            Assert.Contains("\"cariId\":\"c1\"", patch.Govde);
            // ⚠ PATCH SÖZLEŞMESİ: durum gönderilmez → sunucuda DEĞİŞMEZ.
            Assert.DoesNotContain("durum", patch.Govde);
        });
    }

    [Fact]
    public void Atla_dugmesi_YALNIZ_durum_govdesiyle_PATCH_atar()
    {
        var e = Ekran();
        istekler.Clear();

        e.Find("#ekstre-atla-s1").Click();

        e.WaitForAssertion(() =>
        {
            var patch = Assert.Single(istekler, i => i.Metot == "PATCH");
            Assert.Contains("\"durum\":\"ATLANDI\"", patch.Govde);
            Assert.DoesNotContain("cariId", patch.Govde);
        });
    }

    [Fact]
    public void Onay_modali_ONAY_ISTER_ve_onaylanmadan_POST_ATILMAZ()
    {
        var e = Ekran();
        e.Find("#ekstre-sec-s1").Change(true);
        istekler.Clear();

        e.Find("#ekstre-onayla").Click();

        e.WaitForElement("#ekstre-onay-onayla");
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    [Fact]
    public void Onay_modalinda_onaylaninca_SECILI_SATIR_KIMLIKLERI_govdede_gider()
    {
        var e = Ekran();
        e.Find("#ekstre-sec-s1").Change(true);
        e.Find("#ekstre-onayla").Click();
        e.WaitForElement("#ekstre-onay-onayla");
        istekler.Clear();

        e.Find("#ekstre-onay-onayla").Click();

        e.WaitForAssertion(() =>
        {
            var post = Assert.Single(istekler, i => i.Metot == "POST");
            Assert.Contains("/bankalar/ekstre/i1/onayla", post.Yol);
            Assert.Contains("\"s1\"", post.Govde);
            Assert.DoesNotContain("\"s2\"", post.Govde);
        });
    }

    [Fact]
    public void Mukerrer_korumasinin_GECMISE_islemedigi_ekranda_YAZILIDIR()
        => Assert.False(string.IsNullOrWhiteSpace(
            Ekran().Find("#ekstre-mukerrer-sinir").TextContent));

    [Fact]
    public void Sunucu_sayfalama_siniri_ekranda_YAZILIDIR()
        => Assert.False(string.IsNullOrWhiteSpace(
            Ekran().Find("#ekstre-sayfa-not").TextContent));

    [Fact]
    public void Ham_satir_METIN_olarak_basilir_MarkupString_YOK()
    {
        var e = Ekran();

        Assert.Contains("01.09.2026;HAVALE", e.Find("#ekstre-ham-s1").TextContent);
    }

    // ── PAKET Q10-5 (QA turu 10 /banka/ekstre) ───────────────────────────────

    /// <summary>ATLANDI + ONAYLANDI satırlı ayrıntı — sunucu DTO'suyla aynı alanlar.</summary>
    private const string Q105Detay = """
        {"ithalatId":"i1","bankaHesapId":"b1","hesapAdi":"Ziraat — Ana Hesap","paraBirimi":"TRY",
         "dosyaAdi":"eylul.csv","format":"CSV","durum":"PROVA","satirSayisi":3,"yeniSayisi":1,
         "mukerrerSayisi":0,"hataliSayisi":0,"onaylananSayisi":1,"acilisBakiye":null,
         "kapanisBakiye":null,"olusturuldu":"2026-09-17T10:00:00Z","maksOnaySatir":500,
         "limit":100,"offset":0,"toplam":3,
         "satirlar":[
           {"id":"s1","satirNo":1,"hamSatir":null,"tarih":"2026-09-01T00:00:00Z",
            "yon":"GELEN","tutar":100.00,"paraBirimi":"TRY","karsiTarafAd":"PAKETQ105 Karsi Taraf Kuyumculuk",
            "karsiIban":"TR12 **** 0001","bankaReferans":"R1","aciklama":"HAVALE","durum":"YENI",
            "hataMesaji":null,"eslesenCariId":null,"eslesenCariAd":null,"eslesmeKaynagi":null,
            "bankaHareketId":null},
           {"id":"s2","satirNo":2,"hamSatir":null,"tarih":"2026-09-02T00:00:00Z",
            "yon":"GIDEN","tutar":1250.00,"paraBirimi":"TRY","karsiTarafAd":"PAKETQ105 Karsi2",
            "karsiIban":"TR12 **** 0002","bankaReferans":"R2","aciklama":"EFT","durum":"ATLANDI",
            "hataMesaji":null,"eslesenCariId":null,"eslesenCariAd":null,"eslesmeKaynagi":null,
            "bankaHareketId":null},
           {"id":"s3","satirNo":3,"hamSatir":null,"tarih":"2026-09-03T00:00:00Z",
            "yon":"GELEN","tutar":750.00,"paraBirimi":"TRY","karsiTarafAd":"Ayşe Yılmaz",
            "karsiIban":null,"bankaReferans":"R3","aciklama":"GELEN","durum":"ONAYLANDI",
            "hataMesaji":null,"eslesenCariId":"c1","eslesenCariAd":"Ayşe Yılmaz","eslesmeKaynagi":"ELLE",
            "bankaHareketId":"bh9"}]}
        """;

    private IRenderedComponent<Kuyumcu.Web.Pages.BankaEkstre> Q105Ekran()
    {
        sahte.DetayYaniti = Q105Detay;
        return Ekran();
    }

    [Fact]
    public void Q105_Kilit_sebebi_ipucunda_gorsel_gizli_duz_metin_olarak_sutunu_sisirmez()
    {
        // Doğrulama bulgusu (24.09, 1280 PRO): sebep ilk sütunda düz metin olarak
        // 5 satıra sarıyor, tabloyu genişletip 'Atla'yı görünür alan dışına itiyordu.
        var e = Q105Ekran();
        var tr = Kuyumcu.Core.I18n.Q105Sozlugu.Tr;
        var sebep = e.Find("#ekstre-sec-s2-durum");
        Assert.Contains("gorsel-gizli", sebep.ClassName);
        Assert.Equal(tr.EkstreKilitAtlandi, e.Find("#ekstre-sec-s2").Closest("label")!.GetAttribute("title"));
        Assert.Contains("ekstre-sec-s2-durum", e.Find("#ekstre-sec-s2").GetAttribute("aria-describedby") ?? "");
        // Kilitsiz satırda ipucu yok.
        Assert.Null(e.Find("#ekstre-sec-s1").Closest("label")!.GetAttribute("title"));
    }

    [Fact]
    public void Q105_Atlanan_satir_rozeti_Atlandi_kilit_sebebi_gercek_durum()
    {
        var e = Q105Ekran();
        var tr = Kuyumcu.Core.I18n.Q105Sozlugu.Tr;
        var satir2 = e.Find("#ekstre-sec-s2").Closest("tr")!;
        Assert.Contains(tr.EkstreAtlandi, satir2.TextContent);
        Assert.Equal(tr.EkstreKilitAtlandi, e.Find("#ekstre-sec-s2-durum").TextContent.Trim());
        Assert.Equal(tr.EkstreKilitOnaylandi, e.Find("#ekstre-sec-s3-durum").TextContent.Trim());
        // Eski kusur: her kilitli satır "Eşleşti" diyordu.
        Assert.DoesNotContain(Kuyumcu.Core.I18n.Sozlukler.Tr.Banka.Eslesti, satir2.TextContent);
        // Durum sütununda emir kipi ("Atla") DEĞİL, durum adı.
        Assert.DoesNotContain(">" + Kuyumcu.Core.I18n.Sozlukler.Tr.Banka.Atla + "<", satir2.QuerySelector(".rozet")?.OuterHtml ?? "");
    }

    [Fact]
    public void Q105_Atla_sonrasi_bant_hangi_satirin_ne_oldugunu_soyler()
    {
        var e = Q105Ekran();
        e.Find("#ekstre-atla-s1").Click();
        e.WaitForAssertion(() =>
        {
            Assert.Contains(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/ekstre/satir/s1") && i.Govde.Contains("ATLANDI"));
            Assert.Equal(string.Format(Kuyumcu.Core.I18n.Q105Sozlugu.Tr.EkstreSatirAtlandi, 1),
                e.Find("#basari-bandi").TextContent.Trim());
        });
    }

    [Fact]
    public void Q105_Karsi_taraf_adi_ve_iban_ayri_bloklarda()
    {
        var e = Q105Ekran();
        var kap = e.Find("#ekstre-sec-s1").Closest("tr")!.QuerySelector(".q10-karsi");
        Assert.NotNull(kap);
        Assert.NotNull(kap!.QuerySelector(".q10-karsi-ad"));
        Assert.Equal("TR12 **** 0001", kap.QuerySelector(".q10-karsi-iban")!.TextContent.Trim());
        Assert.Contains("q10-th-tek", e.Find("table.veri-tablosu").ClassName);
    }

    /// <summary>
    /// Q10-5 tur 3 (doğrulama DÜŞÜK): 375 px'te ekstre tablosu 1162 px / kapsayıcı
    /// 349 px idi, 'Atla' ekran dışındaydı. Dar ekranda satır kart olur: her veri
    /// hücresi etiketini taşır, eylem hücresi yığın eylemidir, "Tümünü seç" başlığı
    /// mobilde de etiketli kalır (özellik silinmez).
    /// </summary>
    [Fact]
    public void Q105_Ekstre_tablosu_mobil_yigin_her_hucre_etiketli_tumunu_sec_korunur()
    {
        var e = Q105Ekran();
        var tablo = e.Find("table.veri-tablosu");
        Assert.Contains("q10-yigin", tablo.ClassList);
        Assert.Contains("q10-yigin-secim", tablo.ClassList);
        // PAKET GOC-A: ortak ızgara başlığı yalnız metin taşır → "Tümünü seç" ızgaranın ÜSTÜNDE (kimlik AYNI),
        // her genişlikte görünür ve etiketlidir (özellik silinmedi).
        var tumu = e.Find("#ekstre-tumunu-sec");
        Assert.Null(tumu.Closest("table"));
        Assert.Contains(Kuyumcu.Core.I18n.Sozlukler.Tr.Banka.TumunuSec, e.Find("label[for='ekstre-tumunu-sec']").TextContent);
        var hucreler = e.Find("#ekstre-sec-s1").Closest("tr")!.QuerySelectorAll("td");
        Assert.Equal(9, hucreler.Length);
        foreach (var td in hucreler.Skip(1).Take(7))
            Assert.False(string.IsNullOrWhiteSpace(td.GetAttribute("data-etiket")), td.OuterHtml[..Math.Min(80, td.OuterHtml.Length)]);
        Assert.Contains("q10-yigin-eylem", hucreler[8].ClassName);
    }

    [Fact]
    public void Q105_Sahne_satiri_gerekceli_degismez_ve_ithalat_durumu_ham_kod_degil()
    {
        var e = Q105Ekran();
        var tr = Kuyumcu.Core.I18n.Q105Sozlugu.Tr;
        var ipucu = e.Find("#degismez-ipucu-EkstreSahneSatiri");
        Assert.Contains(tr.EkstreSahneDegismez, ipucu.TextContent);
        Assert.Contains(tr.EkstreSahneYolu, ipucu.TextContent);
        Assert.Empty(ipucu.QuerySelectorAll("button"));
        var hesap = e.Find("#ekstre-hesap").TextContent;
        Assert.DoesNotContain("PROVA", hesap);
        Assert.Contains(tr.IthalatProva, hesap);
    }
}
