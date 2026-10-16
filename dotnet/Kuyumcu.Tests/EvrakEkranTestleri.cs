using System.Text.Json;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// EVRAK EKRANLARI — bUnit (paket 29 Faz A).
//
// ⚠ KARARSIZLIK KORUMASI: zamanlayıcı yok, yoklama yok, ateşle-unut yan kanal
//   beklenmiyor. Ölçüm ilk yükleme + AÇIK kullanıcı eylemleriyle yapılır.
//
// ⚠ SAHTE API: yeni uçların HEPSİ gerçekçi yanıt döndürür. `_ => "{}"`
//   varsayılanı JsonException atar ve ilgisiz testleri kırardı.
//
// ⚠ GECİKME/YAKINLIK ekranda HESAPLANMAZ: sunucunun döndürdüğü `gecikmis` /
//   `yakindaVadeli` bayrakları çizilir — gece yarısı tuzağı (K5) bUnit'te
//   sahte saat gerektirmez.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class EvrakEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Sorgu, string Govde)> istekler = [];
    private string listeYaniti = BosListe;

    private const string BosListe = """
        {"toplamAdet":0,"toplamTutar":0.00,"limit":200,"azamiLimit":200,"offset":0,
         "dahaFazlasiVar":false,"portfoydeTutar":0.00,"tahsileVerilenTutar":0.00,
         "buHaftaVadeliTutar":0.00,"karsiliksizTutar":0.00,"satirlar":[]}
        """;

    private const string IkiEvrak = """
        {"toplamAdet":2,"toplamTutar":45000.00,"limit":200,"azamiLimit":200,"offset":0,
         "dahaFazlasiVar":true,"portfoydeTutar":30000.00,"tahsileVerilenTutar":15000.00,
         "buHaftaVadeliTutar":15000.00,"karsiliksizTutar":0.00,
         "satirlar":[
           {"id":"e1","evrakTuru":"CEK","yon":"ALINAN","durum":"PORTFOYDE","tutar":30000.00,
            "paraBirimi":"TRY","vadeGunu":"2026-09-10","evrakNo":"CEK-1","bankaAdi":"Ziraat",
            "bankaSube":"Merkez","kesideYeri":"İstanbul","kesideTarihi":"2026-08-01",
            "borcluCariId":null,"borcluAd":"Ahmet Yılmaz","kaynakCariId":"c1","kaynakCariAd":"Ayşe Yılmaz",
            "notlar":null,"gecikmeGun":8,"gecikmis":true,"yakindaVadeli":false,"duzenlenebilir":true,
            "izinliGecisler":["TAHSILE_VERILDI","TAHSIL_EDILDI","KARSILIKSIZ","CIRO_EDILDI"]},
           {"id":"e2","evrakTuru":"SENET","yon":"ALINAN","durum":"TAHSILE_VERILDI","tutar":15000.00,
            "paraBirimi":"TRY","vadeGunu":"2026-12-01","evrakNo":"SNT-9","bankaAdi":null,
            "bankaSube":null,"kesideYeri":null,"kesideTarihi":null,
            "borcluCariId":null,"borcluAd":null,"kaynakCariId":"c1","kaynakCariAd":"Ayşe Yılmaz",
            "notlar":null,"gecikmeGun":0,"gecikmis":false,"yakindaVadeli":false,"duzenlenebilir":false,
            "izinliGecisler":["PORTFOYDE","TAHSIL_EDILDI","KARSILIKSIZ"]}]}
        """;

    private const string SonDurumEvrak = """
        {"toplamAdet":1,"toplamTutar":5000.00,"limit":200,"azamiLimit":200,"offset":0,
         "dahaFazlasiVar":false,"portfoydeTutar":0.00,"tahsileVerilenTutar":0.00,
         "buHaftaVadeliTutar":0.00,"karsiliksizTutar":0.00,
         "satirlar":[
           {"id":"e9","evrakTuru":"CEK","yon":"ALINAN","durum":"TAHSIL_EDILDI","tutar":5000.00,
            "paraBirimi":"TRY","vadeGunu":"2026-10-01","evrakNo":"CEK-SON","bankaAdi":"Ziraat",
            "bankaSube":null,"kesideYeri":null,"kesideTarihi":null,
            "borcluCariId":null,"borcluAd":null,"kaynakCariId":"c1","kaynakCariAd":"Ayşe Yılmaz",
            "notlar":null,"gecikmeGun":0,"gecikmis":false,"yakindaVadeli":false,"duzenlenebilir":false,
            "izinliGecisler":[]}]}
        """;

    private const string Takvim = """
        {"bas":"2026-09-18","bit":"2026-10-18","toplamAdet":3,"toplamTutar":45000.00,
         "gunler":[{"gun":"2026-09-20","adet":2,"tutar":30000.00},
                   {"gun":"2026-10-01","adet":1,"tutar":15000.00}]}
        """;

    private const string YazmaYaniti = """
        {"evrak":{"id":"e1","evrakTuru":"CEK","yon":"ALINAN","durum":"TAHSILE_VERILDI","tutar":30000.00,
          "paraBirimi":"TRY","vadeGunu":"2026-09-10","evrakNo":"CEK-1","bankaAdi":"Ziraat",
          "bankaSube":null,"kesideYeri":null,"kesideTarihi":null,"borcluCariId":null,"borcluAd":null,
          "kaynakCariId":"c1","kaynakCariAd":"Ayşe Yılmaz","notlar":null,"gecikmeGun":0,
          "gecikmis":false,"yakindaVadeli":false,"duzenlenebilir":false,"izinliGecisler":[]},
         "cariTlBakiye":20000.00,"mesaj":null}
        """;

    public EvrakEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(EvrakEkranTestleri sahip) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var sorgu = istek.RequestUri!.Query;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            sahip.istekler.Add((istek.Method.Method, yol, sorgu, govde));

            var g = yol switch
            {
                var y when y.EndsWith("/evraklar/takvim") => Takvim,
                var y when y.Contains("/evraklar/") && y.EndsWith("/tahsile-ver") => YazmaYaniti,
                var y when y.Contains("/evraklar/") && y.EndsWith("/portfoye-al") => YazmaYaniti,
                var y when y.Contains("/evraklar/") && y.EndsWith("/tahsil-edildi") => YazmaYaniti,
                var y when y.Contains("/evraklar/") && y.EndsWith("/karsiliksiz") => YazmaYaniti,
                var y when y.EndsWith("/evraklar") && istek.Method == HttpMethod.Post => YazmaYaniti,
                // PATCH /evraklar/{id} — alan bazlı düzeltme.
                var y when y.Contains("/evraklar/") => YazmaYaniti,
                var y when y.EndsWith("/evraklar") => sahip.listeYaniti,
                var y when y.EndsWith("/kasalar") =>
                    """[{"id":"k1","ad":"Merkez kasa","paraBirimi":"TRY"}]""",
                var y when y.EndsWith("/bankalar") =>
                    """[{"id":"b1","hesapAdi":"Vadesiz TL","bankaAdi":"Ziraat","paraBirimi":"TRY"}]""",
                var y when y.EndsWith("/cari") =>
                    """[{"id":"c1","ad":"Ayşe Yılmaz","tur":"MUSTERI"}]""",
                var y when y.Contains("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6827.20,"gramAltinFiyati":6861.51}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    // ── Liste ────────────────────────────────────────────────────────────────

    [Fact]
    public void Liste_ekrani_GET_evraklar_cagirir_ve_satirlari_basar()
    {
        listeYaniti = IkiEvrak;
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();

        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/evraklar"));
        Assert.NotNull(c.Find("#evrak-satir-e1"));
        Assert.NotNull(c.Find("#evrak-satir-e2"));
        Assert.Contains("CEK-1", c.Markup);
        Assert.Contains("SNT-9", c.Markup);
    }

    [Fact]
    public void Vadesi_gecmis_satir_kirmizi_sinif_alir()
    {
        listeYaniti = IkiEvrak;
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();

        Assert.Contains("satir-kirmizi", c.Find("#evrak-satir-e1").GetAttribute("class"));
        Assert.DoesNotContain("satir-kirmizi", c.Find("#evrak-satir-e2").GetAttribute("class") ?? "");
    }

    [Fact]
    public void Bos_portfoyde_bos_durum_metni_SOZLUKTEN_gelir()
    {
        listeYaniti = BosListe;
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();

        var sozlukMetni = Kuyumcu.Core.I18n.Sozlukler.Tr.Evrak.KayitYok;
        Assert.Contains(sozlukMetni, c.Markup);
        Assert.NotNull(c.Find("#evrak-bos-aciklama"));
    }

    [Fact]
    public void Sunucu_siniri_ekranda_YAZILIR()
    {
        // UX-S29: "dahaFazlasiVar" true iken sessiz kırpma YASAK.
        listeYaniti = IkiEvrak;
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();
        Assert.NotNull(c.Find("#evrak-sinir-uyarisi"));
    }

    // ── PAKET CUMA-J ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CJ_05_Degismez_yol_BASIT_modda_storno_demez_PRO_metni_korunur(bool basit)
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        listeYaniti = IkiEvrak;
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();
        // ZO-BELGE: gerekçe açılışta değil, DEĞİŞTİRME DENENİNCE (Düzenle çekmecesi).
        Assert.Empty(c.FindAll("#degismez-ipucu-KiymetliEvrakTutar"));
        c.Find("#evrak-duzenle-e1").Click();
        c.WaitForElement("#degismez-ipucu-KiymetliEvrakTutar");
        var ipucu = c.Find("#degismez-ipucu-KiymetliEvrakTutar").TextContent;
        if (basit)
        {
            Assert.Contains("ya da girişi ters kayıtla geri alın; sonra doğru evrakı yeniden girin.", ipucu, StringComparison.Ordinal);
            Assert.DoesNotMatch(CumaJJargon.Re, ipucu);
            Assert.DoesNotMatch(CumaJJargon.Re, c.Find("#evrak-duzenle-e2").GetAttribute("title") ?? "");
        }
        else
        {
            Assert.Contains(Kuyumcu.Core.I18n.Sozlukler.Tr.Evrak.DegismezYol, ipucu, StringComparison.Ordinal);
            Assert.Contains("storno edin", ipucu, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(true, "Kimden")]
    [InlineData(false, "Veren")]
    public void CJ_03b_Tablo_basliklari_kisa_tam_ad_title_da_id_ler_korunur(bool basit, string veren)
    {
        // 4. tur: "Veren" sütunu PRO'da "Müşteri" DEĞİL (evrakı tedarikçi de verebilir);
        // BASİT'te jargonsuz "Kimden". Tam ad iki modda da title'da.
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        listeYaniti = IkiEvrak;
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();
        var d = Services.GetRequiredService<DilServisi>().T.Evrak;
        foreach (var (id, kisa, tam) in new[] { ("evrak-th-tur", "Tür", d.EvrakTuru),
                                               ("evrak-th-veren", veren, d.KaynakCari),
                                               ("evrak-th-vade", "Vade", d.VadeGunu) })
        {
            var th = c.Find("#" + id);
            Assert.Equal(kisa, th.TextContent.Trim());
            Assert.Equal(tam, th.GetAttribute("title"));
        }
        // Başlık satırı kırılmaz (q10-th-tek: white-space:nowrap).
        Assert.Contains("q10-th-tek", c.Find("table.veri-tablosu").ClassList);
        Assert.NotNull(c.Find("#evrak-satir-e1"));
    }

    [Fact]
    public void CJ_10_Durum_suzgecleri_filtre_yuvasinda_degil_serit_dugmesi_istek_atar()
    {
        // 4. tur (doğrulayıcı ORTA): filtre yuvasının sütun kuralı düğme metnini kutunun
        // dibine itiyordu. Süzgeçler artık süzgeç kartındaki şeritte; yuvada düğme yok.
        listeYaniti = IkiEvrak;
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();
        Assert.Empty(c.FindAll(".sayfa-basi-filtre button, .sayfa-basi-filtre a"));
        var serit = c.Find("#evrak-durum-seridi");
        var dugmeler = serit.QuerySelectorAll("button");
        // KESIF-P3 (TOPLU ZO-BELGE ORTA "bölünmüş süzgeç"): dört durum TEK şeritte, dışarıda;
        // #evrak-durum-diger kimliği şeridin İÇİNDE yaşar — id'ler aynı, SİLİNMEDİ.
        Assert.Equal(new[] { "evrak-suzgec-tumu", "evrak-suzgec-portfoyde", "evrak-suzgec-tahsile", "evrak-suzgec-karsiliksiz" },
                     dugmeler.Select(b => b.Id).ToArray());
        Assert.Equal(new[] { "evrak-suzgec-tahsile", "evrak-suzgec-karsiliksiz" },
                     serit.QuerySelector("#evrak-durum-diger")!.QuerySelectorAll("button").Select(b => b.Id).ToArray());
        c.Find("#evrak-suzgec-ac").Click();
        Assert.Empty(c.FindAll("#evrak-suzgec [id^='evrak-suzgec-tahsile'], #evrak-suzgec #evrak-suzgec-karsiliksiz"));
        Assert.Equal("true", c.Find("#evrak-suzgec-tumu").GetAttribute("aria-pressed"));
        // Eylemler (takvim + TEK birincil Yeni) eylem yuvasında.
        Assert.NotNull(c.Find("#sayfa-basi-evraklar-eylem #evrak-yeni"));
        Assert.NotNull(c.Find("#sayfa-basi-evraklar-eylem #evrak-takvime-git"));

        istekler.Clear();
        c.Find("#evrak-suzgec-portfoyde").Click();
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Sorgu.Contains("durum=PORTFOYDE"));
        Assert.Contains("secili", c.Find("#evrak-suzgec-portfoyde").ClassList);
        Assert.Equal("true", c.Find("#evrak-suzgec-portfoyde").GetAttribute("aria-pressed"));
        Assert.Equal("false", c.Find("#evrak-suzgec-tumu").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void CJ_11_Vade_hucresi_yerel_tarih_ve_tek_satir_ISO_basmaz()
    {
        listeYaniti = IkiEvrak;
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();
        var vade = c.Find("#evrak-vade-e2");
        Assert.Equal("01.12.2026", vade.TextContent.Trim());
        Assert.Contains("hucre-tek-satir", vade.ClassList);
        Assert.DoesNotContain("2026-12-01", c.Find("#evrak-satir-e2").TextContent);
        // Evrak no ("SNT-9") tireden bölünmez (1280 BASİT ekran görüntüsünde "SNT-/2002").
        Assert.Contains("hucre-tek-satir", c.Find("#evrak-satir-e2 td.hucre-vurgu").ClassList);
    }

    // ── Eylemler ─────────────────────────────────────────────────────────────

    [Fact]
    public void Tahsile_Ver_dugmesi_POST_atar_ve_govdede_bankaHesapId_vardir()
    {
        listeYaniti = IkiEvrak;
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();

        c.Find("#evrak-tahsile-ver-e1").Click();
        SeciciYardim.Sec(c, "evrak-tahsile-hesap", "b1");
        c.Find("#cekmece-evrak-tahsile-kaydet").Click();

        var istek = istekler.Last(i => i.Metot == "POST" && i.Yol.EndsWith("/tahsile-ver"));
        Assert.Contains("e1", istek.Yol);
        Assert.Contains("bankaHesapId", istek.Govde);
        Assert.Contains("b1", istek.Govde);
    }

    [Fact]
    public void Karsiliksiz_20_karakterden_kisa_gerekceyle_istek_GONDERMEZ()
    {
        listeYaniti = IkiEvrak;
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();

        c.Find("#evrak-karsiliksiz-e1").Click();
        c.Find("#evrak-karsiliksiz-gerekce").Input("kısa");

        // Düğme KİLİTLİ ve sebebi yazılı (UX-S30).
        Assert.True(c.Find("#evrak-karsiliksiz-onayla").HasAttribute("disabled"));
        Assert.NotNull(c.Find("#evrak-karsiliksiz-kilit"));
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/karsiliksiz"));
    }

    [Fact]
    public void Karsiliksiz_yeterli_gerekceyle_POST_atar()
    {
        listeYaniti = IkiEvrak;
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();

        c.Find("#evrak-karsiliksiz-e1").Click();
        c.Find("#evrak-karsiliksiz-gerekce").Input("Banka karşılıksız bildirdi, müşteri arandı.");
        c.Find("#evrak-karsiliksiz-onayla").Click();

        var istek = istekler.Last(i => i.Metot == "POST" && i.Yol.EndsWith("/karsiliksiz"));
        Assert.Contains("aciklama", istek.Govde);
    }

    [Fact]
    public void Son_durumdaki_evrakta_TIKLANINCA_HICBIR_SEY_OLMAYAN_dugme_YOKTUR()
    {
        // DoD EK madde 1: her düğme ya istek atar ya disabled + sebep taşır.
        listeYaniti = SonDurumEvrak;
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();

        Assert.NotNull(c.Find("#evrak-son-durum-e9"));
        Assert.True(c.Find("#evrak-duzenle-e9").HasAttribute("disabled"));
        Assert.Throws<Bunit.ElementNotFoundException>(() => c.Find("#evrak-tahsile-ver-e9"));
        Assert.Throws<Bunit.ElementNotFoundException>(() => c.Find("#evrak-karsiliksiz-e9"));
    }

    [Fact]
    public void Duzenlenebilir_evrakta_duzenleme_YOLU_VARDIR()
    {
        // Kurucunun bağlayıcı şartı: /evraklar için düzenlenemeyen kalem = 0.
        listeYaniti = IkiEvrak;
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();

        var dugme = c.Find("#evrak-duzenle-e1");
        Assert.False(dugme.HasAttribute("disabled"));
        dugme.Click();

        // Aynı Cekmece kalıbı, AYNI alan kümesi (UX-S26).
        Assert.NotNull(c.Find("#evrak-form-no"));
        Assert.NotNull(c.Find("#evrak-form-vade"));
        Assert.NotNull(c.Find("#evrak-form-banka"));
        // Tutar düzenlenemez ama SEBEBİ yazılı (UX-S25).
        Assert.NotNull(c.Find("#evrak-form-tutar-degismez"));
    }

    [Fact]
    public void Duzenleme_PATCH_atar_ve_tutar_gondermez()
    {
        listeYaniti = IkiEvrak;
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();

        c.Find("#evrak-duzenle-e1").Click();
        c.Find("#evrak-form-no").Input("CEK-1-D");
        c.Find("#cekmece-evrak-kaydet").Click();

        var istek = istekler.Last(i => i.Metot == "PATCH");
        Assert.Contains("evrakNo", istek.Govde);
        Assert.DoesNotContain("\"tutar\"", istek.Govde);
        Assert.DoesNotContain("kaynakCariId", istek.Govde);
    }

    [Fact]
    public void Yeni_evrak_formu_POST_atar()
    {
        listeYaniti = BosListe;
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();

        c.Find("#evrak-yeni").Click();
        c.Find("#evrak-form-no").Input("CEK-YENI");
        c.Find("#evrak-form-tutar").Input("1500");
        c.Find("#evrak-form-vade").Change("2026-12-31");
        SeciciYardim.Sec(c, "evrak-form-cari", "c1");
        c.Find("#cekmece-evrak-kaydet").Click();

        var istek = istekler.Last(i => i.Metot == "POST" && i.Yol.EndsWith("/evraklar"));
        Assert.Contains("CEK-YENI", istek.Govde);
        Assert.Contains("kurSnapshotId", istek.Govde);
    }

    [Fact]
    public void Eksik_alanla_gonderim_SESSIZ_ETKISIZ_degildir()
    {
        // DoD EK madde 3: boş/geçersiz gönderimde alan altı Türkçe mesaj çıkar.
        listeYaniti = BosListe;
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();

        c.Find("#evrak-yeni").Click();
        // Kaydet düğmesi KİLİTLİ (sebebi çekmece altında yazılı) → istek gitmez.
        Assert.True(c.Find("#cekmece-evrak-kaydet").HasAttribute("disabled"));
        Assert.NotNull(c.Find("#cekmece-evrak-kilit"));
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/evraklar"));
    }

    [Fact]
    public void KaydetKilidi_EksikAlanlariListeler()
    {
        // PAKET P-65 (E49 · UX-S30): Kaydet kilidi yalnız "Bu alan zorunludur." demez —
        // EKSİK ALANLARIN ADINI sayar; alan doldukça listeden düşer.
        listeYaniti = BosListe;
        var c = Render<Kuyumcu.Web.Pages.Evraklar>();
        c.Find("#evrak-yeni").Click();

        var tr = Kuyumcu.Core.I18n.Sozlukler.Tr.Evrak;
        var kilit = c.Find("#cekmece-evrak-kilit").TextContent;
        Assert.True(c.Find("#cekmece-evrak-kaydet").HasAttribute("disabled"));
        Assert.Contains(string.Format(Kuyumcu.Core.I18n.OnayKilitSozlugu.Tr.EvrakEksikAlanlar, ""), kilit, StringComparison.Ordinal);
        foreach (var alan in new[] { tr.EvrakNo, tr.VadeGunu, tr.Tutar, tr.KaynakCari })
            Assert.Contains(alan, kilit, StringComparison.Ordinal);

        // Evrak no doldu → listeden DÜŞER, diğerleri kalır.
        c.Find("#evrak-form-no").Input("CEK-9");
        kilit = c.Find("#cekmece-evrak-kilit").TextContent;
        Assert.DoesNotContain(tr.EvrakNo, kilit, StringComparison.Ordinal);
        Assert.Contains(tr.VadeGunu, kilit, StringComparison.Ordinal);
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/evraklar"));
    }

    // ── Takvim ───────────────────────────────────────────────────────────────

    [Fact]
    public void Takvim_ekrani_GET_takvim_cagirir_ve_gun_hucrelerini_toplamla_basar()
    {
        var c = Render<Kuyumcu.Web.Pages.EvrakTakvimi>();

        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/evraklar/takvim"));
        Assert.NotNull(c.Find("#evrak-takvim-gun-2026-09-20"));
        Assert.NotNull(c.Find("#evrak-takvim-gun-2026-10-01"));
        // Gün hücresi O GÜNÜN listesine gider (UX-S23).
        var bag = c.Find("#evrak-takvim-bag-2026-09-20");
        Assert.Contains("vadeBas=2026-09-20", bag.GetAttribute("href"));
    }
}
