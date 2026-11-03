using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Bunit;
using Kuyumcu.Contracts.Cikti;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-5 — KAYDET → YAZDIR + BARKOD ODAĞI + PARA ÜSTÜ (PLAN § S-2 2.7 / 2.8)
//
// Sınanan (ekran, bUnit):
//   E-1 sayfa açılınca odak #islem-barkod (kuyumcuOdak.alana); kayıttan sonra YENİDEN
//   E-2 tek kalemli satış: yanıttaki HAREKET kimliği → #islem-yazdir href
//       /cikti/SATIS_FISI/{id}, yeni sekme; bağlantı 3 sn'lik bantla KAYBOLMAZ
//   E-3 karma fiş (yalnız fisId döner): fiş okunur, İLK SATIŞ kalemi yazdırılır
//       (belge üretmeyen HURDA_ALIS kalemi atlanır)
//   E-4 ham ULID banda BASILMAZ, adreste durur
//   E-5 fiş okunamazsa hata YUTULMAZ (UX-S28): #islem-yazdir-hata, bağlantı yok
//   E-6 tek ödeme nakit: alınan → para üstü / eksik; "alınan" gövdeye KONMAZ
//   E-7 karma: para üstü yalnız NAKİT bacaklarının toplamından
//   E-8 kayıt sonrası alınan sıfırlanır (bir sonraki fişe taşınmaz)
//   E-9 iade: yanıttaki hareketId → yazdırma bağlantısı
// Saf: S-1 para üstü hesabı · S-2 yanıt okuyucuları · S-3 ULID ayıklama
//      · S-4 belge türleri sunucunun kümesiyle AYNI
// HTTP: H-1 gerçek boru hattı — satis-fisi → /islemler/fis → IlkKalem →
//       /cikti/SATIS_FISI/{id} 200 ve belge no = fiş no
// Türkçe JSON gövdesi AYRIŞTIRILARAK sınanır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S25YazdirParaUstuEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly SahteApi api;
    private static readonly FisYazdirMetinleri M = FisYazdirSozlugu.Tr;
    private const string Ulid = "01M2MRT7EJMS8GFD78MM80HWZH";

    public S25YazdirParaUstuEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        api = new SahteApi(istekler);
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(api) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        /// <summary>Tek kalemli satış ucunun yanıtı (sunucu hareketin KENDİSİNİ döndürür).</summary>
        public string SatisYaniti { get; set; } = """{"id":"h1","fisId":"h1","tur":"SATIS","tlTutari":10000,"cariId":"pm1"}""";
        public bool FisHatali { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, yol, govde));

            if (FisHatali && yol.Contains("/islemler/fis/"))
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"statusCode":500,"message":"Fiş okunamadı (sahte)","error":"Internal Server Error"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            // ⚠ "{}" varsayılanına bırakılmaz: sunucu sözleşmesiyle aynı alanlar.
            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") =>
                    """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/pos/cihazlar") => "[]",
                // Karma fiş ucu yalnız fisId döndürür (SatisModulu.SatisFisi) — hareket kimliği YOK.
                var v when v.Contains("/islemler/satis-fisi") =>
                    """{"fisId":"f1","toplamTl":10000,"kalemSayisi":1,"nakitToplam":6000,"veresiyeToplam":0,"iskontoToplam":0,"odemeler":[]}""",
                // Fiş okuma: belge üretmeyen kalem ÖNCE gelir → IlkKalem onu atlamalı.
                var v when v.Contains("/islemler/fis/") => """
                    {"fisId":"f1","kalemSayisi":2,"toplamTl":840,
                     "kalemler":[{"id":"h-hurda","tur":"HURDA_ALIS","tlTutari":-9160},
                                 {"id":"h-sat","tur":"SATIS","tlTutari":10000}],
                     "bacaklar":[],"odemeKalemleri":[]}
                    """,
                var v when v.Contains("/islemler/satis-iade") =>
                    """{"hareketId":"h-iade","kaynakHareketId":"s-1","adet":1,"iadeTutariTl":5000,"iadeHasGrami":1.5,"geriGelenMaliyetTl":0,"kalanIadeHakki":0}""",
                var v when v.Contains("/islemler/satis") => SatisYaniti,
                var v when v.Contains("/iade-adaylari") =>
                    """[{"id":"s-1","tarih":"2026-09-24T10:00:00Z","tlTutari":5000,"urunId":"u1","urunAdi":"Burma Bilezik","toplamAdet":1,"iadeEdilenAdet":0,"kalanAdet":1,"cariId":"c1"}]""",
                var v when v.Contains("/reyon") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    // TEMA TEMİZLİĞİ 17.10: SATIŞ fişi artık hep Yıldız tezgâhıdır. Kayıt sonucu (son fiş satırı + yazdır bağlantısı
    // #yildiz-tezgah-yazdir + yazdırma hatası #yildiz-tezgah-yazdir-hata) Fark bloğunda; "+ Başka ödeme"
    // (#yildiz-tezgah-baska-odeme) karmaya geçirir; para üstü "+ Para üstü" hapının açtığı alandadır (AYNI FisParaUstu,
    // #islem-alinan / #islem-para-ustu). Özgün bantlar (#basari-bandi, #islem-yazdir-sar) Yıldız'da görünmez (Y4B_43).
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod = ArayuzModuServisi.Basit)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "SAHIP", "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#islem-tur-SATIS");
        return e;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> KalemliFis()
    {
        var e = Ekran().YildizHazir();
        e.YildizKalemEkle(beklenenFiyat: "10000");
        return e;
    }

    private int OdakSayisi() => JSInterop.Invocations
        .Count(i => i.Identifier == "kuyumcuOdak.alana" && i.Arguments.Count == 1 && (string?)i.Arguments[0] == "islem-barkod");

    private List<JsonElement> Postlar(string uc) => istekler
        .Where(i => i.Metot == "POST" && i.Yol.EndsWith(uc))
        .Select(i => JsonDocument.Parse(i.Govde).RootElement).ToList();

    /// <summary>Satış dışı türlerin (iade) Kaydet'i — o türün formu Yıldız tür yüzünde AYNEN (#islem-kaydet).</summary>
    private static void Kaydet(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled"),
            "Kaydet kilitli: " + string.Join(" | ", e.FindAll("#islem-kaydet-kilit, .alan-hata").Select(x => x.TextContent.Trim()))));
        e.Find("#islem-kaydet").Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
    }

    /// <summary>Karma: "+ Başka ödeme" → ilk satır 6.000 nakit, ikinci satır kalan 4.000 kart.</summary>
    private static void Karma6000Nakit4000Kart(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        e.YildizBol();                                   // SATIS-3: iki satır açılır
        e.Find("#islem-bacak-0-tutar").Input("6000");    // ikinci satır kalanı (4.000) üstlenir
        SeciciYardim.Sec(e, "islem-bacak-1-arac", "KART");
        e.WaitForAssertion(() => Assert.Equal("4000", SeciciYardim.SeciliDeger(e, "islem-bacak-1-tutar")));
    }

    // ── E-1 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S25_E1_Sayfa_acilinca_ve_kayittan_sonra_odak_barkod_alaninda()
    {
        var e = Ekran().YildizHazir();
        e.WaitForAssertion(() => Assert.Equal(1, OdakSayisi()));
        Assert.NotNull(e.Find("#yildiz-tezgah-giris #islem-barkod"));

        e.YildizKalemEkle(beklenenFiyat: "10000");
        Assert.Equal(1, OdakSayisi());   // yazarken odak ÇALINMAZ
        e.YildizKaydet();
        // 03.11 kurucu: kayıt bitince "Satış tamamlandı" perdesi (modal) — odak perdenin tek birincil düğmesinde, barkoda DEĞİL;
        // "Yeni satış" perdeyi kapatınca sıradaki müşteri için okutucu hazır (odak barkoda).
        var kapat = e.WaitForElement("#yildiz-tezgah-perde-kapat");
        Assert.Equal(1, OdakSayisi());
        kapat.Click();
        e.WaitForAssertion(() => Assert.Equal(2, OdakSayisi()));   // sıradaki müşteri: okutucu hazır
        Assert.Empty(e.FindAll("#yildiz-tezgah-perde"));
    }

    // ── E-2 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S25_E2_Tek_kalem_satis_hareket_kimligiyle_yazdir_baglantisi_yeni_sekmede()
    {
        var e = KalemliFis();
        Assert.Empty(e.FindAll("#yildiz-tezgah-yazdir"));   // kayıttan önce yok
        e.YildizKaydet();

        var a = e.WaitForElement("#yildiz-tezgah-yazdir");
        Assert.Equal("A", a.TagName);
        Assert.Equal($"/cikti/{BelgeTurleri.SatisFisi}/h1?yazdir=1", a.GetAttribute("href"));   // S2-5b: otomatik yazdır sorgusu
        Assert.Equal("_blank", a.GetAttribute("target"));
        Assert.Equal(M.FisYazdir, a.TextContent.Trim());
        Assert.Single(Postlar("/islemler/satis"));
        // Tek kalemli uç hareketi döndürdü → fişi ayrıca okumaya GEREK YOK.
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/islemler/fis/"));
        // Başarı bandı 3 sn'de gizlenir; bağlantı kalır.
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#basari-bandi")), TimeSpan.FromSeconds(5));
        Assert.NotNull(e.Find("#yildiz-tezgah-yazdir"));
    }

    // ── E-3 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S25_E3_Karma_fis_fisi_okur_ilk_SATIS_kalemini_yazdirir()
    {
        var e = KalemliFis();
        Karma6000Nakit4000Kart(e);
        e.YildizKaydet();

        var a = e.WaitForElement("#yildiz-tezgah-yazdir");
        Assert.Equal($"/cikti/{BelgeTurleri.SatisFisi}/h-sat?yazdir=1", a.GetAttribute("href"));   // S2-5b: otomatik yazdır sorgusu
        Assert.Single(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/islemler/fis/f1"));
        Assert.Single(Postlar("/islemler/satis-fisi"));
    }

    // ── E-4 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S25_E4_Ham_ULID_banda_basilmaz_adreste_durur()
    {
        api.SatisYaniti = $$"""{"id":"{{Ulid}}","fisId":"{{Ulid}}","tur":"SATIS","tlTutari":10000}""";
        var e = KalemliFis();
        e.YildizKaydet();

        e.WaitForElement("#yildiz-tezgah-son-fis");
        Assert.DoesNotContain(Ulid, e.Find("#yildiz-tezgah-son-fis").TextContent);
        Assert.DoesNotContain(Ulid, e.Find("#yildiz-tezgah-fark").TextContent);   // kayıt sonucu bloğunun tamamı
        Assert.EndsWith(Ulid + "?" + FisYazdir.OtomatikYazdirSorgusu, e.Find("#yildiz-tezgah-yazdir").GetAttribute("href"));   // S2-5b
    }

    // ── E-5 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S25_E5_Fis_okunamazsa_hata_yutulmaz_baglanti_uydurulmaz()
    {
        api.FisHatali = true;
        var e = KalemliFis();
        e.YildizBol();
        e.Find("#islem-bacak-0-tutar").Input("6000");    // SATIS-3: ikinci satır kalanı üstlenir (karma fiş)
        e.YildizKaydet();

        var h = e.WaitForElement("#yildiz-tezgah-yazdir-hata");
        Assert.Contains("Fiş okunamadı (sahte)", h.TextContent);
        Assert.Empty(e.FindAll("#yildiz-tezgah-yazdir"));
        // Kayıt YİNE başarılıdır: tek POST gitti, hata yalnız yazdırmaya aittir.
        Assert.Single(Postlar("/islemler/satis-fisi"));
    }

    // ── E-6 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S25_E6_Tek_odeme_nakit_alinan_para_ustu_ve_eksik_govdeye_girmez()
    {
        var e = KalemliFis();
        // SATIS-3 t22: isteğe bağlı → açılışta alan YOK; Yıldız'da "+ Para üstü" hapı açar.
        e.WaitForAssertion(() => Assert.False(e.Find("#yildiz-tezgah-para-ustu").HasAttribute("disabled")));
        Assert.Empty(e.FindAll("#islem-alinan"));
        Assert.Empty(e.FindAll("#islem-para-ustu-sar"));
        Assert.Equal("false", e.Find("#yildiz-tezgah-para-ustu").GetAttribute("aria-expanded"));
        e.Find("#yildiz-tezgah-para-ustu").Click();
        e.WaitForElement("#yildiz-tezgah-para-ustu-alan #islem-alinan");
        Assert.Empty(e.FindAll("#islem-alinan-ipucu"));
        Assert.Equal(M.AlinanBekleniyor, e.Find("#islem-para-ustu").TextContent.Trim());

        e.Find("#islem-alinan").Input("12000");
        e.WaitForAssertion(() => Assert.Equal(Bicim.Para(2000m, "TRY"), e.Find("#islem-para-ustu").TextContent.Trim()));
        Assert.Equal(M.ParaUstu, e.Find("label[for='islem-para-ustu']").TextContent.Trim());
        Assert.DoesNotContain("metin-kirmizi", e.Find("#islem-para-ustu").ClassName);

        e.Find("#islem-alinan").Input("9000");
        e.WaitForAssertion(() => Assert.Equal(Bicim.Para(1000m, "TRY"), e.Find("#islem-para-ustu").TextContent.Trim()));
        Assert.Equal(M.Eksik, e.Find("label[for='islem-para-ustu']").TextContent.Trim());
        Assert.Contains("metin-kirmizi", e.Find("#islem-para-ustu").ClassName);

        // Bilgi amaçlıdır: Kaydet'i kilitlemez, gövdeye girmez (D7: deftere girmez).
        e.Find("#islem-alinan").Input("10000");
        e.WaitForAssertion(() => Assert.Equal(Bicim.Para(0m, "TRY"), e.Find("#islem-para-ustu").TextContent.Trim()));
        e.YildizKaydet();
        e.WaitForAssertion(() => Assert.Single(Postlar("/islemler/satis")));
        var g = Postlar("/islemler/satis")[0];
        Assert.Equal(10000m, g.GetProperty("birimFiyatTl").GetDecimal());
        foreach (var p in g.EnumerateObject())
            Assert.DoesNotContain("alinan", p.Name, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("12000", g.GetRawText());
    }

    // ── E-7 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S25_E7_Karmada_para_ustu_yalniz_NAKIT_bacaklarindan()
    {
        var e = KalemliFis();
        Karma6000Nakit4000Kart(e);

        // SATIS-3 t6: karmada para üstü isteğe bağlı → açılışta alan YOK, "+ Para üstü" ile açılır.
        Assert.Empty(e.FindAll("#islem-alinan"));
        e.WaitForAssertion(() => Assert.False(e.Find("#yildiz-tezgah-para-ustu").HasAttribute("disabled")));
        e.Find("#yildiz-tezgah-para-ustu").Click();
        // Karma açıkken tek ödemenin para üstü İKİNCİ KEZ çizilmez (tek alan).
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#islem-alinan")));
        e.Find("#islem-alinan").Input("6500");
        e.WaitForAssertion(() => Assert.Equal(Bicim.Para(500m, "TRY"), e.Find("#islem-para-ustu").TextContent.Trim()));
    }

    // ── E-8 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S25_E8_Kayittan_sonra_alinan_sifirlanir_sonraki_fise_tasinmaz()
    {
        var e = KalemliFis();
        e.WaitForElement("#yildiz-tezgah-para-ustu").Click();   // t22: tek ödemede de kapalı gelir
        e.WaitForElement("#islem-alinan");
        e.Find("#islem-alinan").Input("12000");
        e.WaitForAssertion(() => Assert.Equal(Bicim.Para(2000m, "TRY"), e.Find("#islem-para-ustu").TextContent.Trim()));
        e.YildizKaydet();
        e.WaitForElement("#yildiz-tezgah-yazdir");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-alinan")));   // kalem yok → nakit 0

        e.YildizKalemEkle(beklenenFiyat: "10000");
        // Yeni fişte alınan BOŞTUR (önceki müşterinin parası taşınmaz). Yıldız hapı açık kalmışsa alan kendiliğinden
        // görünür; kapalıysa hapla açılır — her iki yolda değer boş, para üstü "bekleniyor".
        if (e.FindAll("#islem-alinan").Count == 0) e.Find("#yildiz-tezgah-para-ustu").Click();
        e.WaitForAssertion(() => Assert.Equal(M.AlinanBekleniyor, e.Find("#islem-para-ustu").TextContent.Trim()));
        Assert.Equal("", e.Find("#islem-alinan").GetAttribute("value") ?? "");
    }

    // ── E-9 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S25_E9_Iade_yanitindaki_hareketId_ile_yazdir_baglantisi()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        e.Find("#islem-tur-SATIS_IADE").Click();
        e.WaitForElement("#iade-kaynak");
        SeciciYardim.Sec(e, "iade-kaynak", "s-1");
        e.WaitForElement("#iade-adet");
        if (e.FindAll("#islem-cari").Count > 0) SeciciYardim.Sec(e, "islem-cari", "c1");   // SATIS-3 t11: kaynak fişin müşterisi varsa seçici yok (kilitli, kaynaktan)
        if (e.FindAll("#islem-kasa").Count > 0) SeciciYardim.Sec(e, "islem-kasa", "ks1");   // SATIS-3: tek kasada seçici yok (otomatik)   // pro modda kasa sorulur (IadeTestleri deseni)
        Kaydet(e);
        var a = e.WaitForElement("#islem-yazdir");
        Assert.Equal($"/cikti/{BelgeTurleri.SatisFisi}/h-iade?yazdir=1", a.GetAttribute("href"));   // S2-5b: otomatik yazdır sorgusu
        Assert.Single(Postlar("/islemler/satis-iade"));
    }
}

public sealed class S25YazdirParaUstuSafTestleri
{
    // ── S-1 ───────────────────────────────────────────────────────────────
    [Theory]
    [InlineData(12000, 10000, 2000)]
    [InlineData(10000, 10000, 0)]
    [InlineData(9000, 10000, -1000)]
    [InlineData(100.005, 100, 0.01)]    // kuruşa yuvarlanır (AwayFromZero)
    [InlineData(250.5, 199.99, 50.51)]
    public void S25_S1_Para_ustu_alinan_eksi_nakit(double alinan, double nakit, double beklenen) =>
        Assert.Equal((decimal)beklenen, FisYazdir.ParaUstu((decimal)alinan, (decimal)nakit));

    [Fact]
    public void S25_S1_Alinan_yoksa_para_ustu_yok() => Assert.Null(FisYazdir.ParaUstu(null, 10000m));

    // ── S-2 ───────────────────────────────────────────────────────────────
    private static JsonElement J(string s) => JsonDocument.Parse(s).RootElement;

    [Fact]
    public void S25_S2_Yanit_okuyuculari_hareketi_bulur_uydurmaz()
    {
        Assert.Equal("h1", FisYazdir.HareketId(J("""{"id":"h1","tur":"SATIS"}""")));
        Assert.Equal("h2", FisYazdir.HareketId(J("""{"id":"h2","tur":"ALIS"}""")));
        Assert.Equal("hi", FisYazdir.HareketId(J("""{"hareketId":"hi","kaynakHareketId":"k"}""")));
        Assert.Null(FisYazdir.HareketId(J("""{"id":"h3","tur":"HURDA_ALIS"}""")));   // belge üretmez
        Assert.Null(FisYazdir.HareketId(J("""{"id":"h4"}""")));                     // tür yok → uydurulmaz
        Assert.Null(FisYazdir.HareketId(J("""{"fisId":"f1"}""")));
        Assert.Null(FisYazdir.HareketId(J("[]")));

        Assert.Equal("b", FisYazdir.IlkKalem(J("""{"kalemler":[{"id":"a","tur":"HURDA_ALIS"},{"id":"b","tur":"SATIS"},{"id":"c","tur":"SATIS"}]}""")));
        Assert.Null(FisYazdir.IlkKalem(J("""{"kalemler":[{"id":"a","tur":"HURDA_ALIS"}]}""")));
        Assert.Null(FisYazdir.IlkKalem(J("""{"fisId":"f1"}""")));

        Assert.Equal("/cikti/SATIS_FISI/h%2F1", FisYazdir.Adres("h/1"));
        Assert.Null(FisYazdir.Adres(" "));
    }

    // ── S-3 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S25_S3_Ham_ULID_ayiklanir_okunur_numara_kalir()
    {
        Assert.True(FisYazdir.UlidMi("01M2MRT7EJMS8GFD78MM80HWZH"));
        Assert.True(FisYazdir.UlidMi("01m2mrt7ejms8gfd78mm80hwzh"));
        Assert.False(FisYazdir.UlidMi("01M2MRT7EJMS8GFD78MM80HWZ"));    // 25
        Assert.False(FisYazdir.UlidMi("01M2MRT7EJMS8GFD78MM80HWZU"));   // U base32 değil
        Assert.Null(FisYazdir.OkunurNo("01M2MRT7EJMS8GFD78MM80HWZH"));
        Assert.Equal("FIS-000124", FisYazdir.OkunurNo("FIS-000124"));
        Assert.Null(FisYazdir.OkunurNo(null));
    }

    // ── S-4 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S25_S4_Belge_turleri_sunucunun_SATIS_FISI_kumesiyle_AYNI()
    {
        var m = typeof(Kuyumcu.Api.Modules.Cikti.CiktiServisi)
            .GetMethod("BeklenenTurler", BindingFlags.NonPublic | BindingFlags.Static)!;
        var sunucu = (string[])m.Invoke(null, [BelgeTurleri.SatisFisi])!;
        Assert.Equal(sunucu.OrderBy(x => x), FisYazdir.BelgeliTurler.OrderBy(x => x));
    }

    [Fact]
    public void S25_S5_Sozluk_TR_EN_tam_ve_ham_kod_yok()
    {
        foreach (var s in new[] { FisYazdirSozlugu.Tr, FisYazdirSozlugu.En })
            foreach (var p in typeof(FisYazdirMetinleri).GetProperties().Where(p => p.PropertyType == typeof(string)))
            {
                var v = (string)p.GetValue(s)!;
                Assert.False(string.IsNullOrWhiteSpace(v), p.Name);
                Assert.DoesNotContain("SATIS_FISI", v);
            }
        Assert.Same(FisYazdirSozlugu.En, FisYazdirSozlugu.Al("en"));
        Assert.Same(FisYazdirSozlugu.Tr, FisYazdirSozlugu.Al("xx"));
    }
}

[Collection("http")]
public sealed class S25YazdirHttpTestleri(HttpFikstur f)
{
    // ── H-1 ───────────────────────────────────────────────────────────────
    [Fact]
    public async Task S25_H1_Karma_fis_okunur_ilk_kalem_cikti_200_ve_belge_no_fis_no()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETS25-TEST-yazdir");
        var http = f.Ortam.Istemci(k, "TEZGAHTAR");

        var c = await http.PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = k.CariId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 1_000m } },
            odemeler = new[] { new { arac = "NAKIT", tutarTl = 1_000m, kasaId = k.KasaId } },
            kurSnapshotId = k.KurId,
        });
        var ham = await c.Content.ReadAsStringAsync();
        Assert.True(c.StatusCode == HttpStatusCode.OK, ham);
        var fis = JsonDocument.Parse(ham).RootElement;
        var fisId = fis.GetProperty("fisId").GetString()!;
        // Karma uç hareket kimliği DÖNDÜRMEZ — ekranın fişi okuması bu yüzden gerekir.
        Assert.Null(FisYazdir.HareketId(fis));
        Assert.True(FisYazdir.UlidMi(fisId));

        var fo = await http.GetAsync($"/api/v1/islemler/fis/{fisId}");
        Assert.Equal(HttpStatusCode.OK, fo.StatusCode);
        var hareketId = FisYazdir.IlkKalem(await fo.Content.ReadFromJsonAsync<JsonElement>());
        Assert.False(string.IsNullOrEmpty(hareketId));

        // Ekranın kurduğu adres = sunucunun çıktı ucu (/api/v1 öneki ApiIstemci'nindir).
        var adres = FisYazdir.Adres(hareketId)!;
        var cy = await http.GetAsync("/api/v1" + adres);
        var cham = await cy.Content.ReadAsStringAsync();
        Assert.True(cy.StatusCode == HttpStatusCode.OK, cham);
        var belge = JsonDocument.Parse(cham).RootElement;
        Assert.Equal(BelgeTurleri.SatisFisi, belge.GetProperty("tur").GetString());
        Assert.Equal(fisId, belge.GetProperty("belgeNo").GetString());
    }

    [Fact]
    public async Task S25_H2_Tek_kalem_satis_yaniti_hareketi_tasir_cikti_200()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETS25-TEST-tek");
        var http = f.Ortam.Istemci(k, "TEZGAHTAR");
        var c = await http.PostAsJsonAsync("/api/v1/islemler/satis", new
        {
            cariId = k.CariId, urunId = k.UrunId, adet = 1m, birimFiyatTl = 1_000m,
            odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId,
        });
        var ham = await c.Content.ReadAsStringAsync();
        Assert.True(c.StatusCode == HttpStatusCode.OK, ham);
        var hareketId = FisYazdir.HareketId(JsonDocument.Parse(ham).RootElement);
        Assert.False(string.IsNullOrEmpty(hareketId), ham);

        var cy = await http.GetAsync("/api/v1" + FisYazdir.Adres(hareketId));
        Assert.Equal(HttpStatusCode.OK, cy.StatusCode);
    }
}
