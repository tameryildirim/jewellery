using System.Net;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S-3e — KUR KAYNAĞI SEÇİCİ + /kur QA turu 5 bulguları (bUnit).
//
// KURUCU BULGUSU (2026-09-18): "kur çekme kaynakları görünmüyor — Altınkaynak,
// Ankara Altın Borsası gibi daha önce var olanlar."
// ÖLÇÜM: adaptörler (KurModulu.cs AltinkaynakAdaptoru · AnkaraAltinBorsasiAdaptoru
// · TruncgilAdaptoru) ve uç (KurController [HttpPatch("kaynak")]) VARDI; ekranda
// seçici YOKTU. Bu dosya seçicinin varlığını, listenin SUNUCUDAN geldiğini ve
// seçimin gerçekten istek attığını zorlar.
//
// QA TURU 5 /kur bulguları:
//   • "Şimdi çek" dış kaynaklar kapalıyken de etkin → her tıklama garanti 503
//   • başarısız çekme ekrandaki son bilinen kuru SİLİYOR (UX-S28 ihlali)
//   • kur geçmişindeki "İptal et" hiçbir şey yapmıyor (sessiz etkisizlik)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S3eKurKaynagiEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    /// <summary>Üç kaynaklı (gerçek üretim adaptör kümesi) varsayılan yanıt.</summary>
    private string kaynakYaniti =
        """{"kaynaklar":["ALTINKAYNAK","ANKARA","TRUNCGIL"],"varsayilan":"ALTINKAYNAK","secili":"ALTINKAYNAK","sozlesmesizKaynaklarAcik":true,"seciliSozlesmesiz":true,"manuelKod":"MANUAL"}""";

    private string gecmisYaniti = "[]";
    private HttpStatusCode guncelleKodu = HttpStatusCode.OK;

    /// <summary>PAKET P-11 (B-14) — OKUMA uçlarının dönebileceği kod (503 hata sınırı).</summary>
    private HttpStatusCode okumaKodu = HttpStatusCode.OK;

    public S3eKurKaynagiEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(
            new SahteApi(istekler, () => kaynakYaniti, () => gecmisYaniti, () => guncelleKodu, () => okumaKodu))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Kur> Ciz()
    {
        var c = Render<Kuyumcu.Web.Pages.Kur>().AyrintiAc();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-kaynak-kart")));
        return c;
    }

    // ── 1) KURUCU BULGUSU: kaynak GÖRÜNÜR (adı sözlükten) ───────────────────
    // KESIF-P6 (YETKİ, ekip oyu 30.09 18:10): ikinci seçici MÜDÜR'e kaynağı ONAYSIZ
    // değiştirtiyordu. Bu ekranda kaynak SALT OKUNURDUR; tek yazma yolu İşletme'nin
    // onay kalıbıdır (P13EkranTestleri / KesifP1Testleri). Aşağıdaki üç test eski
    // "seçici çizilir / PATCH gider" sözleşmesinin YERİNE geçti.

    private void OturumAta(string rol) =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1"));

    [Fact]
    public void Cok_kaynakta_SECICI_YOK_secili_kaynak_salt_okunur_adiyla_yazar()
    {
        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-kaynak-secili")), TimeSpan.FromSeconds(10));

        Assert.Empty(c.FindAll("#kur-kaynak-sec"));
        // Ham kod (ALTINKAYNAK) değil, okunur ad (UX-S31).
        Assert.Equal("Altınkaynak", c.Find("#kur-kaynak-secili").TextContent.Trim());
    }

    [Fact]
    public void Kaynak_adi_SUNUCUNUN_secili_dedigidir_bilinmeyen_kod_uydurulmaz()
    {
        kaynakYaniti =
            """{"kaynaklar":["ANKARA","TRUNCGIL","YENIKAYNAK"],"varsayilan":"ANKARA","secili":"YENIKAYNAK","sozlesmesizKaynaklarAcik":true,"seciliSozlesmesiz":true,"manuelKod":"MANUAL"}""";

        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-kaynak-secili")), TimeSpan.FromSeconds(10));
        Assert.Equal("YENIKAYNAK", c.Find("#kur-kaynak-secili").TextContent.Trim());
    }

    [Fact]
    public void KESIFP6_Kur_kaynagi_bu_ekranda_PATCH_ATMAZ_SAHIPte_Isletme_baglantisi_MUDURde_sebep()
    {
        // Rol, sayfa çizildikten SONRA atanır (Oturum.Degisti → yeniden çizim): tam süitte çizimden
        // önce atanan oturum ilk yüklemeyi bekletiyordu (kararsızlık gözlemi, 01.10 tam süit).
        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-kaynak-secili")), TimeSpan.FromSeconds(10));
        OturumAta("SAHIP");
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-kaynak-isletme-bag")), TimeSpan.FromSeconds(10));
        Assert.Equal("/isletme", c.Find("#kur-kaynak-isletme-bag").GetAttribute("href"));
        Assert.Empty(c.FindAll("#kur-kaynak-yalniz-sahip"));
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
    }

    [Fact]
    public void KESIFP6_MUDUR_kur_kaynagini_DEGISTIREMEZ_sebep_yazili_baglanti_yok()
    {
        // Rol, sayfa çizildikten SONRA atanır (Oturum.Degisti → yeniden çizim): tam süitte çizimden
        // önce atanan oturum ilk yüklemeyi bekletiyordu (kararsızlık gözlemi, 01.10 tam süit).
        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-kaynak-secili")), TimeSpan.FromSeconds(10));
        OturumAta("MUDUR");
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-kaynak-yalniz-sahip")), TimeSpan.FromSeconds(10));
        Assert.Empty(c.FindAll("#kur-kaynak-isletme-bag"));
        Assert.Empty(c.FindAll("#kur-kaynak-sec"));
        Assert.Equal(Kuyumcu.Core.I18n.KesifP6Sozlugu.Tr.KaynakYalnizSahip,
            c.Find("#kur-kaynak-yalniz-sahip").TextContent.Trim());
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
    }

    // ── Kurucu 24.10: Yıldız özet yüzünde KAYNAK satırı SAHİP için YERİNDE seçicidir ("kaynağı değiştirmek için
    //    bilmem kaç ekran değiştirmemeli"); yazma yolu yine MEVCUT uç (PATCH /kur/kaynak, yalnız SAHİP). ───────────
    [Fact]
    public void Yildiz_KAYNAK_satiri_SAHIPte_secicidir_secince_PATCH_kur_kaynak_gider()
    {
        var c = Render<Kuyumcu.Web.Pages.Kur>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#yildiz-kurlar-bilgi")), TimeSpan.FromSeconds(10));
        OturumAta("SAHIP");
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#yildiz-kur-kaynak")), TimeSpan.FromSeconds(10));
        var sec = c.Find("#yildiz-kur-kaynak");
        Assert.Equal(3, sec.QuerySelectorAll("option").Length);
        Assert.Equal("Altınkaynak", sec.QuerySelector("option[selected]")!.TextContent.Trim());   // ham kod değil, ad (UX-S31)

        sec.Change("ANKARA");
        c.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/kur/kaynak")), TimeSpan.FromSeconds(10));
        Assert.Contains("\"kaynak\":\"ANKARA\"", istekler.Single(i => i.Metot == "PATCH").Govde);
    }

    [Fact]
    public void Yildiz_KAYNAK_satiri_MUDURde_salt_okunur_secici_YOK()
    {
        var c = Render<Kuyumcu.Web.Pages.Kur>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#yildiz-kurlar-bilgi")), TimeSpan.FromSeconds(10));
        OturumAta("MUDUR");
        c.WaitForAssertion(() => Assert.Contains("Altınkaynak", c.Find("#yildiz-kurlar-bilgi").TextContent), TimeSpan.FromSeconds(10));
        Assert.Empty(c.FindAll("#yildiz-kur-kaynak"));
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
    }

    [Fact]
    public void Yildiz_kur_ekrani_her_5_saniyede_guncel_ve_kalemleri_yeniden_okur()
    {
        Assert.Equal(TimeSpan.FromSeconds(5), Kuyumcu.Web.Pages.Kur.YildizYenilemeAraligi);
        Assert.Equal(5, Kuyumcu.Api.Modules.Kur.KurZamanlayici.AltSinirSaniye);
    }

    [Fact]
    public void Kaynak_listesi_BOSSA_secici_KILITLI_ve_sebebi_yazili()
    {
        kaynakYaniti =
            """{"kaynaklar":[],"varsayilan":"MANUAL","secili":"MANUAL","sozlesmesizKaynaklarAcik":false,"seciliSozlesmesiz":false,"manuelKod":"MANUAL"}""";

        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-kaynak-kilit")));

        Assert.True(c.Find("#kur-kaynak-sec").HasAttribute("disabled"));
        Assert.Contains("Manuel kur", c.Find("#kur-kaynak-sec").TextContent + c.Find("#kur-kaynak-sec").GetAttribute("title"));
        Assert.Contains("Manuel Kur Gir", c.Find("#kur-kaynak-kilit").TextContent);
    }

    [Fact]
    public void Tek_kaynak_varsa_secici_yerine_SALT_OKUNUR_satir_cizilir()
    {
        // UX-S22 — tek seçenek varsa seçici gösterilmez; değer yine EKRANDA yazar.
        kaynakYaniti =
            """{"kaynaklar":["ANKARA"],"varsayilan":"ANKARA","secili":"ANKARA","sozlesmesizKaynaklarAcik":true,"seciliSozlesmesiz":true,"manuelKod":"MANUAL"}""";

        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-kaynak-tek")));

        Assert.Empty(c.FindAll("#kur-kaynak-sec"));
        Assert.Equal("Ankara Altın Borsası", c.Find("#kur-kaynak-tek").TextContent.Trim());
        Assert.False(string.IsNullOrWhiteSpace(c.Find("#kur-kaynak-tek-sebep").TextContent));
    }

    // ── 2) QA turu 5: "Şimdi çek" kapalıyken KİLİTLİ ────────────────────────

    [Fact]
    public void Dis_kaynaklar_kapaliyken_SIMDI_CEK_kilitli_ve_sebebi_yazili()
    {
        kaynakYaniti =
            """{"kaynaklar":[],"varsayilan":"MANUAL","secili":"MANUAL","sozlesmesizKaynaklarAcik":false,"seciliSozlesmesiz":false,"manuelKod":"MANUAL"}""";

        var c = Ciz();
        c.WaitForAssertion(() => Assert.True(c.Find("#kur-simdi-cek").HasAttribute("disabled")));

        var baslik = c.Find("#kur-simdi-cek").GetAttribute("title");
        Assert.False(string.IsNullOrWhiteSpace(baslik));
        Assert.Contains("kapalı", baslik!);
    }

    [Fact]
    public void Kaynaklar_ACIKKEN_simdi_cek_ETKINDIR()
    {
        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-kaynak-secili")), TimeSpan.FromSeconds(10));
        Assert.False(c.Find("#kur-simdi-cek").HasAttribute("disabled"));
    }

    // ── 3) QA turu 5: başarısız çekme ekrandaki kuru SİLMEZ (UX-S28) ────────

    [Fact]
    public void Basarisiz_cekme_son_bilinen_kuru_EKRANDA_BIRAKIR()
    {
        var c = Ciz();
        c.WaitForAssertion(() => Assert.Contains("6.800", c.Markup));

        guncelleKodu = HttpStatusCode.ServiceUnavailable;
        c.Find("#kur-simdi-cek").Click();

        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-guncellenemedi")));
        // Kur kartı ve ayar fiyatları YERİNDE: hata "kayıt yok"a çevrilmedi.
        Assert.Contains("6.800", c.Markup);
        // PAKET S3-5 · UX-S31: ayar tablosu ham kodu (K22) değil görünen adı yazar.
        Assert.Contains("22 Ayar", c.Markup);
    }

    // ── 4) QA turu 5: geçmişteki "İptal et" sessiz DEĞİLDİR ─────────────────

    [Fact]
    public void Gecmisteki_MANUEL_satirin_iptal_dugmesi_ONAY_MODALI_acar()
    {
        gecmisYaniti =
            """[{"id":"k9","altinHasFiyati":6820.50,"gramAltinFiyati":null,"usdTry":null,"eurTry":null,"gbpTry":null,"kaynak":"MANUAL","olusturuldu":"2026-09-18T06:47:00Z","ayarFiyatlari":[],"iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null}]""";

        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-iptal-k9")));
        Assert.False(c.Find("#kur-iptal-k9").HasAttribute("disabled"));

        c.Find("#kur-iptal-k9").Click();

        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-iptal-onay")));
        Assert.NotNull(c.Find("#kur-iptal-onayla"));
    }

    [Fact]
    public void Gecmisteki_DIS_KAYNAK_satirinin_iptal_dugmesi_KILITLIDIR()
    {
        gecmisYaniti =
            """[{"id":"k8","altinHasFiyati":6813.24,"gramAltinFiyati":null,"usdTry":null,"eurTry":null,"gbpTry":null,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-18T06:14:00Z","ayarFiyatlari":[],"iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null}]""";

        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-iptal-k8")));
        Assert.True(c.Find("#kur-iptal-k8").HasAttribute("disabled"));
        // DoD EK madde 1: kilitli düğmenin sebebi ekranda yazılı (DegismezIpucu).
        Assert.Contains("MANUAL", c.Markup);
    }

    // ── 5) PAKET P-11 (B-14): OKUMA hatası ekrandaki veriyi DÜŞÜRMEZ ───────
    //
    // S-3e yalnız "Şimdi çek" (POST) yolunu düzeltmişti. GET yolları hâlâ
    // `guncel = null` / `gecmis = []` / `pariteler = []` yazıyordu: sayfa
    // yenilenince ya da kaynak değişince son bilinen kur EKRANDAN SİLİNİYORDU.
    // UX-S28 / Anayasa md.1: hata "kayıt yok"a çevrilmez.

    [Fact]
    public void Guncel_kur_OKUMASI_503_donunce_son_bilinen_kur_EKRANDA_KALIR()
    {
        gecmisYaniti =
            """[{"id":"k9","altinHasFiyati":6820.50,"gramAltinFiyati":null,"usdTry":null,"eurTry":null,"gbpTry":null,"kaynak":"MANUAL","olusturuldu":"2026-09-18T06:47:00Z","ayarFiyatlari":[],"iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null}]""";
        var c = Ciz();
        c.WaitForAssertion(() => Assert.Contains("6.800", c.Markup));

        // İnternet kesildi: bundan sonraki HER okuma 503.
        okumaKodu = HttpStatusCode.ServiceUnavailable;
        YenidenOkut(c);   // KESIF-P6: kaynak seçici kalktı → manuel kur kaydı güncel+geçmişi yeniden okur

        c.WaitForAssertion(() => Assert.Contains("6.800", c.Markup));
        // Kur kartı ve ayar tablosu YERİNDE; geçmiş satırı da düşmedi.
        // PAKET S3-5 · UX-S31: ayar tablosu ham kodu (K22) değil görünen adı yazar.
        Assert.Contains("22 Ayar", c.Markup);
        Assert.NotNull(c.Find("#kur-iptal-k9"));
    }

    [Fact]
    public void Gecmis_OKUMASI_hata_verince_BOS_DURUM_cizilmez_hata_bandi_cikar()
    {
        gecmisYaniti =
            """[{"id":"k9","altinHasFiyati":6820.50,"gramAltinFiyati":null,"usdTry":null,"eurTry":null,"gbpTry":null,"kaynak":"MANUAL","olusturuldu":"2026-09-18T06:47:00Z","ayarFiyatlari":[],"iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null}]""";
        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-iptal-k9")));

        okumaKodu = HttpStatusCode.ServiceUnavailable;
        YenidenOkut(c);

        // Boş durum metni ÇIKMAMALI: sonuç boş değil, istek başarısız.
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-iptal-k9")));
        Assert.DoesNotContain("Seçilen aralıkta kur kaydı yok", c.Markup);
    }

    [Fact]
    public void Parite_OKUMASI_hata_verince_parite_tablosu_DUSMEZ()
    {
        // KESIF-P6: pariteyi yeniden okutan tek yol (kaynak seçici) bu ekrandan kalktı; kural
        // açılış okumasında sınanır — 503 "parite yok" boş durumuna ÇEVRİLMEZ, bant çıkar.
        okumaKodu = HttpStatusCode.ServiceUnavailable;
        var c = Ciz();
        c.WaitForElement("#kur-sekme-pariteler").Click();
        Assert.DoesNotContain(Kuyumcu.Core.I18n.Sozlukler.Getir("tr").Kur.PariteYok, c.Markup);

        // Hata bandı görünür; "kayıt yok" boş durumu ile karıştırılmaz.
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#parite-hata")));
        Assert.False(string.IsNullOrWhiteSpace(c.Find("#parite-hata").TextContent));
    }

    [Fact]
    public void Kur_gecmisi_satirinda_SATIR_ICI_style_YOKTUR()
    {
        // P-11 sayacı SATIR_ICI_STYLE: Kur.razor'daki tek satır içi style
        // belirteçli sınıfa taşındı; ekran testi geri dönüşü yakalar.
        gecmisYaniti =
            """[{"id":"k9","altinHasFiyati":6820.50,"gramAltinFiyati":null,"usdTry":null,"eurTry":null,"gbpTry":null,"kaynak":"MANUAL","olusturuldu":"2026-09-18T06:47:00Z","ayarFiyatlari":[],"iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null}]""";
        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-iptal-k9")));

        Assert.DoesNotContain("white-space:nowrap", c.Markup);
        Assert.Contains("hucre-tek-satir", c.Markup);
    }

    /// <summary>KESIF-P6: güncel + geçmişi yeniden okutan ekran yolu (manuel kur kaydı).</summary>
    private static void YenidenOkut(IRenderedComponent<Kuyumcu.Web.Pages.Kur> c)
    {
        c.Find("#kur-manuel-ac").Click();
        c.WaitForElement("#m-kaydet").Click();
    }

    // ── Sahte API ───────────────────────────────────────────────────────────

    private sealed class SahteApi(
        List<(string, string, string)> kayit,
        Func<string> kaynaklar,
        Func<string> gecmis,
        Func<HttpStatusCode> guncelleKodu,
        Func<HttpStatusCode> okumaKodu) : HttpMessageHandler
    {
        private const string GuncelKur =
            """{"id":"kur-1","altinHasFiyati":6800.00,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-18T06:14:00Z","ayarFiyatlari":[{"ayarId":"K22","label":"22 ayar","milyem":916,"gramFiyati":6228.80,"hasFiyati":6800.00}]}""";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, istek.RequestUri!.PathAndQuery, govde));

            // PAKET P-11 (B-14): OKUMA uçları da düşebilir (internet kesik).
            if (okumaKodu() != HttpStatusCode.OK
                && (yol.EndsWith("/kur/guncel") || yol.EndsWith("/kur/gecmis") || yol.EndsWith("/kur/pariteler")))
                return new HttpResponseMessage(okumaKodu())
                {
                    Content = new StringContent(
                        """{"message":"Kur servisine ulaşılamadı."}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            if (yol.EndsWith("/kur/guncelle") && guncelleKodu() != HttpStatusCode.OK)
                return new HttpResponseMessage(guncelleKodu())
                {
                    Content = new StringContent(
                        """{"mesaj":"Dış kur kaynakları kapalı (yalnız pilot kaynaklar sözleşmesizdir)."}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            var g = yol switch
            {
                var y when y.EndsWith("/kur/kaynaklar") => kaynaklar(),
                var y when y.EndsWith("/kur/gecmis") => gecmis(),
                var y when y.EndsWith("/kur/guncel") || y.EndsWith("/kur/guncelle") => GuncelKur,
                var y when y.EndsWith("/kur/kaynak") => """{"kaynak":"ANKARA"}""",
                var y when y.Contains("/kur/pariteler") => """{"kaynak":"ALTINKAYNAK","pariteler":[]}""",
                var y when y.Contains("/kur/") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }
}
