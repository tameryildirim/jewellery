using System.Globalization;
using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET L-1 — TEVSİK AÇIĞI: ÇOK KALEMLİ SATIŞ TEK FİŞ (bUnit, sahte API)
//
// Kök neden (T-S1 v3, canlıda üretildi): Kaydet() çok kalemli düz satışı kalem
// başına AYRI POST /islemler/satis ile gönderiyordu → 42.065,89 ₺'lik nakit fiş
// 28.711,64 + 13.354,25 diye bölündü; sunucunun tevsik kapısı fiş toplamını hiç
// görmedi. Artık ≥2 kalemli satış HER MODDA TEK POST /islemler/satis-fisi'dir.
//
//   • BASİT ve PRO: 2 kalem → satis-fisi 1, satis 0; gövde tek bacak, tutar = ekran toplamı
//   • Sunucu tevsikle reddederse (400 + alanlar.cariId) hata görünür, başarı yok, ikinci istek yok
//   • Tek kalem → bugünkü /islemler/satis yolu (davranış DEĞİŞMEDİ)
//   • Çok kalemli fiş TEK belge: "Kaydet ve yazdır" + kayıttan sonra yazdırma sekmesi açılır
//
// ⚠ Eşik istemcide YOK: sahte API'nin had'i sunucu yanıtıdır; ekran rakam gömmez.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class L1TekFisEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool fisReddet;   // sunucu tevsik reddi (400) taklidi

    public L1TekFisEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => fisReddet)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private const string RetMesaji = "Bu tutar (40.000,00 TL) tevsik (459 s. VUK GT) sınırını aşıyor; gerçek müşteri seçin.";

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<bool> reddet) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, yol, govde));
            if (yol.Contains("/urunler/barkod/"))
                return Yanit(System.Net.HttpStatusCode.NotFound, """{"message":"bulunamadı"}""");
            if (yol.Contains("/islemler/satis-fisi") && istek.Method == HttpMethod.Post && reddet())
                return Yanit(System.Net.HttpStatusCode.BadRequest,
                    $$"""{"message":"{{RetMesaji}}","alanlar":{"cariId":["{{RetMesaji}}"]},"error":"Bad Request","statusCode":400}""");
            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") =>
                    """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":50000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":50000,"islemTutariKuraliDevrede":false}""",   // SATIS-3 t10: 40.000 ₺ had ALTINDA → istemci kilitlemez, sunucu reddi yolu sınanır
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":20000,"hasDegeri":19000,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":20000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":3.94,"iscilikTl":0,"satisFiyati":20000,"gramAgirlik":4.3},{"id":"u2","ad":"Tek Tas Yuzuk","ayarId":"K14","hasGrami":1.2,"iscilikTl":0,"satisFiyati":20000,"gramAgirlik":2}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                // Gerçekçi fiş okuma yanıtı (CiktiModulu/FisOku şekli): kalem id + tur → yazdırma bağlantısı.
                var v when v.Contains("/islemler/fis/") => """{"fisId":"01J000000000000000000000F1","kalemler":[{"id":"01J00000000000000000000011","tur":"SATIS"},{"id":"01J00000000000000000000012","tur":"SATIS"}]}""",
                var v when v.Contains("/islemler/satis-fisi") => """{"fisId":"01J000000000000000000000F1","toplamTl":40000,"kalemSayisi":2,"nakitToplam":40000,"veresiyeToplam":0,"iskontoToplam":0,"yerlesimUyarisi":null}""",
                var v when v.Contains("/islemler/satis") => """{"id":"01J00000000000000000000001","fisId":"01J00000000000000000000002","tur":"SATIS","tlTutari":20000}""",
                var v when v.Contains("/iade-adaylari") => "[]",
                var v when v.Contains("/reyon") => "[]",
                var v when v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return Yanit(System.Net.HttpStatusCode.OK, y);
        }

        private static HttpResponseMessage Yanit(System.Net.HttpStatusCode kod, string govde) =>
            new(kod) { Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json") };
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-tur-SATIS");
        return e;
    }

    private static void KalemEkle(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string urunId, int beklenen)
    {
        SeciciYardim.Sec(e, "islem-urun", urunId);
        e.WaitForAssertion(() => Assert.Equal(urunId, SeciciYardim.SeciliDeger(e, "islem-urun")));
        e.WaitForAssertion(() => Assert.Equal("20000", SeciciYardim.SeciliDeger(e, "islem-birimfiyat")));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.Equal(beklenen, e.FindAll("#islem-kalem-tablo tbody tr, #islem-kalem-pro tr.fis-pro-satir").Count), TimeSpan.FromSeconds(5));
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> IkiKalemliFis(string mod)
    {
        var e = Ekran(mod);
        e.WaitForAssertion(() => Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        KalemEkle(e, "u1", 1);
        KalemEkle(e, "u2", 2);
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled"),
            "Kaydet kilitli: " + string.Join(" | ", e.FindAll("#islem-kaydet-kilit, .alan-hata").Select(x => x.TextContent.Trim()))), TimeSpan.FromSeconds(5));
        return e;
    }

    private int Post(string sonEk) { lock (istekler) return istekler.Count(i => i.Metot == "POST" && i.Yol.EndsWith(sonEk)); }
    private JsonElement FisGovdesi() { lock (istekler) return JsonDocument.Parse(istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis-fisi")).Govde).RootElement.Clone(); }
    private List<JSRuntimeInvocation> AcilanSekmeler() => JSInterop.Invocations.Where(i => i.Identifier == "open").ToList();

    // ═══ SAF ════════════════════════════════════════════════════════════════

    [Fact]
    public void FisUcuMu_iki_ve_ustu_kalem_ya_da_ozel_fis_EVET_tek_duz_kalem_HAYIR()
    {
        Assert.False(FisYazdir.FisUcuMu(0, ozelFis: false));
        Assert.False(FisYazdir.FisUcuMu(1, ozelFis: false));
        Assert.True(FisYazdir.FisUcuMu(2, ozelFis: false));
        Assert.True(FisYazdir.FisUcuMu(3, ozelFis: false));
        Assert.True(FisYazdir.FisUcuMu(1, ozelFis: true));
    }

    // ═══ EKRAN ══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Iki_kalemli_satis_TEK_satis_fisi_istegi_kalem_basi_satis_SIFIR(string mod)
    {
        var e = IkiKalemliFis(mod);
        Assert.Empty(e.FindAll("#islem-kaydet-cokfis"));   // "N ayrı fiş" notu KALKTI
        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Post("/islemler/satis-fisi")), TimeSpan.FromSeconds(5));
        Assert.Equal(0, Post("/islemler/satis"));   // ⚠ L-1: kalem başına POST YOK

        var g = FisGovdesi();
        Assert.Equal("pm1", g.GetProperty("cariId").GetString());
        var kalemler = g.GetProperty("kalemler").EnumerateArray().ToList();
        Assert.Equal(2, kalemler.Count);
        Assert.Equal(["u1", "u2"], kalemler.Select(k => k.GetProperty("urunId").GetString()!).ToArray());
        var bacak = Assert.Single(g.GetProperty("odemeler").EnumerateArray().ToList());
        Assert.Equal("NAKIT", bacak.GetProperty("arac").GetString());
        Assert.Equal(40000m, bacak.GetProperty("tutarTl").GetDecimal());   // fiş BRÜT toplamı tek bacakta
        Assert.Equal("ks1", bacak.GetProperty("kasaId").GetString());
        Assert.Equal(0m, g.GetProperty("fisIskontoYuzde").GetDecimal());   // düz fişe carinin oranı sessizce girmez
        Assert.Equal("kur-1", g.GetProperty("kurSnapshotId").GetString());
        Assert.True(g.TryGetProperty("reyonId", out _));   // tek kalemli uçla aynı reyon sözleşmesi
    }

    [Fact]
    public void Iki_kalemli_fis_TEK_belge_Kaydet_ve_yazdir_kayittan_SONRA_yazdirma_sekmesi_acilir()
    {
        var e = IkiKalemliFis(ArayuzModuServisi.Basit);
        Assert.Equal(SatisTuvalSozlugu.Tr.KaydetYazdir, e.Find("#islem-kaydet").TextContent.Trim());
        Assert.Empty(AcilanSekmeler());
        e.Find("#islem-kaydet").Click();
        e.WaitForElement("#islem-yazdir-otomatik", TimeSpan.FromSeconds(5));
        var acilan = Assert.Single(AcilanSekmeler());
        Assert.Equal(FisYazdir.YazdirAdresi("01J00000000000000000000011"), acilan.Arguments[0]);   // fişin İLK kalemi → belge fişin tüm kalemlerini basar
        Assert.Single(e.FindAll("#islem-yazdir-sar a"));
    }

    [Fact]
    public void Sunucu_tevsikle_reddederse_hata_GORUNUR_basari_YOK_ikinci_istek_YOK()
    {
        fisReddet = true;
        var e = IkiKalemliFis(ArayuzModuServisi.Basit);
        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains("tevsik", e.Markup), TimeSpan.FromSeconds(5));
        Assert.Contains(RetMesaji, e.Markup);
        Assert.Equal(1, Post("/islemler/satis-fisi"));
        Assert.Equal(0, Post("/islemler/satis"));   // red sonrası kalem başına "yedek" yol YOK
        Assert.Empty(e.FindAll(".bant-basari"));
        Assert.Empty(AcilanSekmeler());
        Assert.Equal(2, e.FindAll("#islem-kalem-tablo tbody tr").Count);   // fiş kaybolmaz, düzeltilebilir
    }

    [Fact]
    public void Tek_kalemli_satis_bugunku_satis_ucunda_kalir_davranis_DEGISMEDI()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        e.WaitForAssertion(() => Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        KalemEkle(e, "u1", 1);
        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Post("/islemler/satis")), TimeSpan.FromSeconds(5));
        Assert.Equal(0, Post("/islemler/satis-fisi"));
        lock (istekler)
        {
            var g = JsonDocument.Parse(istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis")).Govde).RootElement;
            Assert.Equal("u1", g.GetProperty("urunId").GetString());
            Assert.Equal("NAKIT", g.GetProperty("odemeTuru").GetString());
        }
    }
}
