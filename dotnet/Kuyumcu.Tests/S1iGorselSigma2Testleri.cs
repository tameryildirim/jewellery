using System.Net;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-i — GÖRSEL SIĞMA 2 (kurucu 24.09: "tren gibi sayfa", "diğer ekranlara
// da bak", /islemler "yazılar kutuya sığmaz, inputlar aynı seviyede değil")
//
// Kilitler:
//   (1) Ortak Secici paneli: min(max(tetik, 20rem), 92vw), harf ortasından bölme
//       YOK, kenara taşarsa `.sola` (tek yer: app.css + js/secici-yerlesim.js).
//   (2) Form satırı hizası: /islemler "Elle ekle" satırında iç içe `.alan` YOK,
//       düğme KONTROL rayında, fiyat kutusunun yanındaki ikinci "TRY" yok.
//   (3) Basit yüz dili: "Cariler" yerine "Müşteri", uzun notlar tek kısa satır.
//   (4) Son fiş numarası yazdırma bağlantısının yanında kalır (QA-9).
//   (5) Uzun listeler: denetim izi ve işlem defteri SUNUCU sayfalı (10), kasa
//       defteri kasa başına son 7 gün + "N gün daha"; toplam her zaman yazılı.
//   (6) Tren sayfalar ve elle yazılmış tablist'ler ortak Sekmeler bileşeninde.
// bUnit CSS uygulamaz: sığma/hiza/boy ölçümü rapordaki gerçek tarayıcı tablosudur.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S1iGorselSigma2Testleri : BunitContext
{
    private readonly List<string> istekler = [];

    public S1iGorselSigma2Testleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
    }

    // ── Sahte API: her uca GERÇEKÇİ yanıt (bUnit '{}' varsayılanı yok) ────────
    private sealed class SahteApi(List<string> kayit) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var uri = istek.RequestUri!;
            var yol = uri.AbsolutePath;
            kayit.Add($"{istek.Method.Method} {uri.PathAndQuery}");
            var g = yol switch
            {
                var x when x.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var x when x.EndsWith("/kullanicilar") => """[{"id":"u1","ad":"Ayşe","soyad":"Yılmaz"}]""",
                var x when x.EndsWith("/kasalar") => """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true}]""",
                var x when x.EndsWith("/denetim/ozet") =>
                    """{"toplam":40,"hassasAdet":2,"enEskiKayit":"2026-09-01T08:00:00Z","eylemler":[{"eylem":"OKUMA","adet":40}],"turler":[{"varlikTuru":"Cari","adet":40}]}""",
                var x when x.EndsWith("/denetim") => DenetimSayfasi(uri.Query),
                var x when x.EndsWith("/raporlar/kasa-defteri") => KasaDefteri,
                var x when x.EndsWith("/raporlar/islem-defteri") => IslemDefteri(uri.Query),
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private static int SorguSayi(string sorgu, string ad, int varsayilan)
    {
        var m = Regex.Match(sorgu, $@"[?&]{ad}=(\d+)");
        return m.Success ? int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : varsayilan;
    }

    /// <summary>40 kayıtlık denetim izi; istenen limit/offset kadar döner (sunucu sözleşmesi).</summary>
    private static string DenetimSayfasi(string sorgu)
    {
        var limit = SorguSayi(sorgu, "limit", 100);
        var offset = SorguSayi(sorgu, "offset", 0);
        var adet = Math.Max(0, Math.Min(limit, 40 - offset));
        var satirlar = Enumerable.Range(offset, adet).Select(i =>
            $$"""{"id":"d{{i}}","tarih":"2026-09-13T10:00:00Z","eylem":"OKUMA","varlikTuru":"Cari","varlikId":"c{{i}}","hassas":{{(i == 0 ? "true" : "false")}},"detay":"kayıt {{i}}","ipAdresi":null,"kullaniciId":"u1","kullanici":"Ayşe Yılmaz","rol":"SAHIP","subeId":null,"subeAd":null,"kasaId":null,"kasaAd":null}""");
        return $$"""{"toplam":40,"limit":{{limit}},"offset":{{offset}},"kayitlar":[{{string.Join(",", satirlar)}}]}""";
    }

    /// <summary>Tek kasa, 14 gün (eskiden yeniye).</summary>
    private static readonly string KasaDefteri = "{\"baslangic\":\"2026-09-11\",\"bitis\":\"2026-09-24\",\"saatDilimi\":\"Europe/Istanbul\",\"utcOfsetSaat\":3,\"yazSaatiRiski\":false,\"toplamSatir\":14,\"kasalar\":[{\"kasaId\":\"k1\",\"kasaAd\":\"Ana Kasa\",\"paraBirimi\":\"TRY\",\"acilisDeviri\":0,\"kapanisBakiye\":1400,\"guncelBakiye\":1400,\"gunler\":["
        + string.Join(",", Enumerable.Range(11, 14).Select(g =>
            $$"""{"tarih":"2026-09-{{g:00}}","devir":{{(g - 11) * 100}},"giris":100,"cikis":0,"hareketSayisi":1,"kapanis":{{(g - 10) * 100}},"durum":"KAPALI","sayilanNakit":null,"kapanisDefterBakiye":null,"fark":null}"""))
        + "]}]}";

    private static string IslemDefteri(string sorgu)
    {
        var limit = SorguSayi(sorgu, "limit", 100);
        var ofset = SorguSayi(sorgu, "ofset", 0);
        var adet = Math.Max(0, Math.Min(limit, 120 - ofset));
        var satirlar = Enumerable.Range(ofset, adet).Select(i =>
            $$"""{"id":"h{{i}}","tarih":"2026-09-20T10:00:00Z","tur":"SATIS","odemeTuru":"NAKIT","tutarTl":100,"hasGram":0,"cari":"Peşin Müşteri","kullanici":"Ayşe","sube":"Merkez","aciklama":null}""");
        return $$"""{"baslangic":"2026-09-18","bitis":"2026-09-24","toplamSatir":120,"ofset":{{ofset}},"limit":{{limit}},"dahaFazlasiVar":{{(ofset + adet < 120 ? "true" : "false")}},"ozet":{"tlGiris":12000,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":120},"storno":{"adet":0,"tutarTl":0},"satirlar":[{{string.Join(",", satirlar)}}]}""";
    }

    private static string Oku(params string[] yol) => File.ReadAllText(Path.Combine([CssKaynak.Kok(), .. yol]));

    private static string S1iBlogu()
    {
        var css = Oku("Kuyumcu.Web", "wwwroot", "css", "app.css");
        var i = css.IndexOf("PAKET S1-i — GÖRSEL SIĞMA 2", StringComparison.Ordinal);
        Assert.True(i > 0, "app.css'te S1-i bloğu yok.");
        return css[i..];
    }

    private static string Kural(string css, string secici)
    {
        var i = css.IndexOf(secici + " {", StringComparison.Ordinal);
        Assert.True(i >= 0, $"S1-i bloğunda '{secici}' kuralı yok.");
        var son = css.IndexOf('}', i);
        return css[i..son];
    }

    // ── (1) ORTAK SEÇİCİ PANELİ ─────────────────────────────────────────────
    [Fact]
    public void S1i_01_Secici_paneli_en_az_20rem_en_cok_92vw_harf_ortasindan_bolmez()
    {
        var blok = S1iBlogu();
        var panel = Kural(blok, ".secici-liste-kap");
        Assert.Contains("min-width: min(max(100%, 20rem), 92vw)", panel);
        Assert.Contains("max-width: min(max(100%, 32rem), 92vw)", panel);
        Assert.Contains("width: max-content", panel);

        var ad = Kural(blok, ".secici-secenek .secici-ad");
        Assert.Contains("word-wrap: break-word", ad);   // S1-c bloğu kilidi "overflow" sözcüğünü yasaklar (eşdeğer eski ad)
        Assert.DoesNotContain("anywhere", ad);
        Assert.DoesNotContain("break-all", blok);

        Assert.Contains(".secici-liste-kap.sola { left: auto; right: 0; }", blok);
        var js = Oku("Kuyumcu.Web", "wwwroot", "js", "secici-yerlesim.js");
        Assert.Contains("panel.classList.add('sola')", js);
        Assert.Contains("panel.classList.remove('sola')", js);
    }

    // ── (2) FORM SATIRI HİZASI ─────────────────────────────────────────────
    private IRenderedComponent<FisKalemGirisi> KalemCiz(KalemGirisi? g = null) =>
        Render<FisKalemGirisi>(p => p
            .Add(x => x.Giris, g ?? new KalemGirisi())
            .Add(x => x.UrunSecenekleri, [new SecenekOgesi("u1", "22 Ayar Burma Bilezik (K22)")])
            .Add(x => x.Form, new FormDurumu())
            .Add(x => x.Basit, false));

    [Fact]
    public void S1i_02_Elle_ekle_satiri_ortak_form_satiri_ic_ice_alan_yok_dugme_satirda()
    {
        var c = KalemCiz();
        var satir = c.Find("#islem-elle-satir");
        Assert.Contains("form-satiri", satir.ClassList);
        Assert.Contains("fis-kalem-satiri", satir.ClassList);

        // Doğrudan çocuklar: ürün alanı · adet alanı · fiyat alanı · Ekle.
        var cocuk = satir.Children.ToList();
        Assert.Equal(4, cocuk.Count);
        Assert.NotNull(cocuk[0].QuerySelector("#islem-urun"));
        Assert.Equal("islem-adet-sar", cocuk[1].Id);
        Assert.Equal("islem-birimfiyat-sar", cocuk[2].Id);   // iç içe .alan SARMALAYICISI YOK
        Assert.Equal("islem-kalem-ekle", cocuk[3].Id);
        Assert.Empty(c.FindAll("#islem-elle-satir .alan > .alan"));
    }

    [Fact]
    public void S1i_03_Teklif_notu_satirin_ALTINDA_hizayi_bozmaz()
    {
        var g = new KalemGirisi
        {
            UrunId = "u1",
            SonTeklif = new Teklif(true, null, 6200m, null, null, null, null, null, null, false, null),
        };
        var c = KalemCiz(g);
        Assert.Empty(c.FindAll("#islem-elle-satir #islem-teklif"));
        Assert.NotNull(c.Find("#islem-teklif-sar #islem-teklif"));
        Assert.NotNull(c.Find("#islem-fiyat-kaynak"));   // UX-S21 rozeti yaşıyor
    }

    [Fact]
    public void S1i_04_Form_satiri_CSS_dugme_kontrol_rayinda_TRY_eki_gizli()
    {
        var blok = S1iBlogu();
        var dugme = Kural(blok, ".satir.form-satiri:has(> .alan) > :is(button, .btn):not(.alan):not(.secici-alan)");
        Assert.Contains("grid-row: 2 / 3", dugme);
        Assert.Contains("align-self: end", dugme);
        Assert.Contains("height: var(--kontrol-yuksekligi", dugme);
        Assert.Contains(".fis-kalem-satiri .alan-son-ek { display: none; }", blok);
        Assert.Contains(".form-satiri .alan-istege { white-space: nowrap; }", blok);
        Assert.Contains("align-self: start", Kural(blok, ".form-satiri .alan-etiket"));
    }

    // ── (3) BASİT YÜZ DİLİ ────────────────────────────────────────────────
    private IRenderedComponent<FisMusteriSatiri> MusteriCiz(bool basit) =>
        Render<FisMusteriSatiri>(p => p
            .Add(x => x.Cariler, [])
            .Add(x => x.CariId, "p1")
            .Add(x => x.PesinCariId, "p1")
            .Add(x => x.Basit, basit));

    [Fact]
    public void S1i_05_Basit_yuzde_Cariler_yerine_Musteri_ve_kisa_notlar()
    {
        var b = MusteriCiz(basit: true);
        Assert.Contains("Müşteri", b.Find("label[for=islem-cari]").TextContent);
        Assert.DoesNotContain("Cariler", b.Find(".alan").TextContent);   // GÖRÜNEN metinde yok (tam not title'da)
        Assert.Equal("Pasif müşteriler listede yok.", b.Find("#islem-cari-pasif-notu").TextContent.Trim());
        Assert.Equal("Peşin satış: müşteri adı gerekmez.", b.Find("#islem-pesin-notu").TextContent.Trim());
        // Uzun metin silinmedi: title'da yaşar.
        Assert.Contains("Cariler ekranından", b.Find("#islem-cari-pasif-notu").GetAttribute("title"));

        var p = MusteriCiz(basit: false);
        Assert.Contains("Cariler", p.Find("label[for=islem-cari]").TextContent);   // D10: pro dili değişmez
        Assert.Null(p.Find("#islem-cari-pasif-notu").GetAttribute("title"));
    }

    [Fact]
    public void S1i_06_Satis_ekrani_basligi_menu_adiyla_ayni_ve_satir_ici_style_yok()
    {
        var razor = Oku("Kuyumcu.Web", "Pages", "Islemler.razor");
        Assert.Contains("<SayfaBasi Anahtar=\"islemler\" Baslik=\"@SayfaAdi\"", razor);
        Assert.Contains("<PageTitle>@Marka.SayfaBasligi(SayfaAdi)</PageTitle>", razor);
        Assert.DoesNotContain("style=\"max-width:48rem", razor);
        Assert.True(razor.Split('\n').Length <= 1401, "Islemler.razor 1400 satır tavanını aştı.");
        var cs = Oku("Kuyumcu.Web", "Pages", "Islemler.Diger.cs");
        Assert.Contains("MenuAdlari.Icin(Dil.T).Satis.Ad", cs);
        Assert.Equal("Satış", MenuAdlari.Icin(Sozlukler.Getir("tr")).Satis.Ad);
    }

    // ── (4) SON FİŞ ────────────────────────────────────────────────────────
    [Fact]
    public void S1i_07_Son_fis_numarasi_yazdir_baglantisinin_yaninda_kalir()
    {
        var c = Render<FisYazdirBaglantisi>(p => p
            .Add(x => x.Idler, ["01HAREKET"])
            .Add(x => x.FisNo, "F-000123"));
        var satir = c.Find("#islem-yazdir-sar");
        Assert.Equal("Son fiş: F-000123", satir.QuerySelector("#islem-son-fis")!.TextContent.Trim());
        Assert.NotNull(satir.QuerySelector("#islem-yazdir"));

        // Bağlantı yoksa (ör. alış) numara YİNE görünür.
        var yalniz = Render<FisYazdirBaglantisi>(p => p.Add(x => x.Idler, []).Add(x => x.FisNo, "F-9"));
        Assert.Equal("Son fiş: F-9", yalniz.Find("#islem-son-fis").TextContent.Trim());

        var bos = Render<FisYazdirBaglantisi>(p => p.Add(x => x.Idler, []));
        Assert.Empty(bos.FindAll("#islem-son-fis"));
    }

    [Fact]
    public void S1i_08_Hurda_alimi_gir_derin_baglantisi_HURDA_ALIS_turunu_acar()
    {
        var gp = Oku("Kuyumcu.Web", "Pages", "GiderPusulalari.razor");
        Assert.Contains("Nav.NavigateTo(HurdaAlimiAdresi)", gp);
        Assert.Equal("/islemler?tur=HURDA_ALIS", Kuyumcu.Web.Pages.GiderPusulalari.HurdaAlimiAdresi);
        var cs = Oku("Kuyumcu.Web", "Pages", "Islemler.Diger.cs");
        Assert.Contains("DerinBaglanti.Oku(Nav.Uri, \"tur\")", cs);
        Assert.Contains("TurListesi.Any(x => x.Deger == istenen)", cs);   // bilinmeyen tür tahmin edilmez
        var razor = Oku("Kuyumcu.Web", "Pages", "Islemler.razor");
        Assert.Contains("SorgudanSuzgecOku(); BasitModuUygula();", razor);   // denetim.sh P-15 çağrı kanıtı
    }

    // ── (5) UZUN LİSTELER ─────────────────────────────────────────────────
    [Fact]
    public void S1i_09_Denetim_izi_sunucu_sayfali_7_satir_toplam_yazili()
    {
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        // ZO-BELGE 29.09 devam 6 (tasarım hakemi KRİTİK): sayfa boyu 7 → 25 (açılış sayfa kısaltılarak sağlanmaz).
        c.WaitForAssertion(() => Assert.Equal(25, c.FindAll("#dz-tablo tbody tr").Count));
        Assert.Contains(istekler, i => i.StartsWith("GET ", StringComparison.Ordinal) && i.Contains("/denetim?")
                                       && i.Contains("limit=25") && i.Contains("offset=0"));
        // ZO-BELGE (tasarım hakemi t3): aralık TEK yerde (sayfalama satırı); üstteki kopya kalktı.
        Assert.Equal("1–25 / 40 kayıt", c.Find("#dz-sayfa-bilgi").TextContent.Trim());
        Assert.Empty(c.FindAll("#dz-sayfa-ozet"));
        Assert.Single(c.FindAll("*"), e => e.ChildElementCount == 0 && e.TextContent.Trim() == "1–25 / 40 kayıt");
        Assert.True(c.Find("#dz-onceki").HasAttribute("disabled"));

        c.Find("#dz-sonraki").Click();
        c.WaitForAssertion(() => Assert.Equal("26–40 / 40 kayıt", c.Find("#dz-sayfa-bilgi").TextContent.Trim()));
        Assert.Contains(istekler, i => i.Contains("/denetim?") && i.Contains("offset=25"));
        Assert.Equal(15, c.FindAll("#dz-tablo tbody tr").Count);   // biriktirmez, YERİNE koyar
        Assert.False(c.Find("#dz-onceki").HasAttribute("disabled"));
        Assert.True(c.Find("#dz-sonraki").HasAttribute("disabled"));   // uçta kilitli, aralık sebebi söyler
        Assert.Contains("tablo-tek-satir", c.Find("#dz-tablo").ClassList);
    }

    [Fact]
    public void S1i_10_Kasa_defteri_kasa_basina_son_7_gun_ve_N_gun_daha()
    {
        var c = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        c.WaitForElement("#kr-gecmis-ac");
        c.Find("#kr-gecmis-ac").Click();   // ZO-BELGE: gün tablosu kapalı katlanırda
        c.WaitForAssertion(() => Assert.Equal(7, c.FindAll("#kr-tablo-k1 tbody tr").Count));
        var tarihler = c.FindAll("#kr-tablo-k1 tbody tr td:first-child").Select(t => t.TextContent.Trim()).ToList();
        Assert.Equal("18.09.2026", tarihler[0]);    // en YENİ 7 gün, sunucu sırası korunur (ZO-BELGE 29.09 devam 6: yerel tarih, ISO değil)
        Assert.Equal("24.09.2026", tarihler[^1]);
        Assert.Contains("Son 7 gün gösteriliyor · dönemde 14 gün", c.Find("#kr-gun-bilgi-k1").TextContent);

        c.Find("#kr-gun-daha-k1").Click();
        Assert.Equal(14, c.FindAll("#kr-tablo-k1 tbody tr").Count);
        Assert.Empty(c.FindAll("#kr-gun-daha-k1"));
        Assert.Contains("Tüm günler gösteriliyor", c.Find("#kr-gun-bilgi-k1").TextContent);
        Assert.Contains("Toplam 14 kayıt", c.Find("#kr-gun-bilgi-k1").TextContent);
    }

    [Fact]
    public void S1i_11_Islem_defteri_25_satirlik_sunucu_sayfasi_ve_ustte_aralik()
    {
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#id-sinir")));
        Assert.Contains(istekler, i => i.Contains("/raporlar/islem-defteri?") && i.Contains("limit=25"));   // ZO-BELGE 29.09 devam 6: 6 → 25 (tasarım hakemi KRİTİK)
        Assert.Equal("1–25 / 120 kayıt", c.Find("#id-sinir").TextContent.Trim());
        Assert.Equal(25, c.FindAll("#id-tablo tbody tr").Count);
    }

    // ── (6) SEKMELER ──────────────────────────────────────────────────────
    [Theory]
    [InlineData("KarZarar.razor", "kz", 5)]
    [InlineData("Yedekler.razor", "yd", 4)]
    [InlineData("Kur.razor", "kur", 4)]
    [InlineData("Isletme.razor", "isl", 4)]
    [InlineData("SatisAnalizi.razor", "sa", 3)]
    [InlineData("Uyum.razor", "uy", 4)]
    [InlineData("Gider.razor", "gd", 3)]
    public void S1i_12_Tren_sayfalar_ve_elle_tablistler_ortak_Sekmeler_bileseninde(string dosya, string onek, int asgariSekme)
    {
        var razor = Oku("Kuyumcu.Web", "Pages", dosya);
        Assert.Contains($"<Sekmeler Onek=\"{onek}\"", razor);
        Assert.DoesNotContain("role=\"tablist\"", razor);   // elle yazılmış şerit kalmadı
        Assert.DoesNotContain("role=\"tab\"", razor);
        // Sekme tanımları: new("<kod>", …) ya da GorselSigmaSozlugu sabitleri.
        var tanim = Regex.Matches(razor, @"new\((GorselSigmaSozlugu\.\w+|""\w+""),").Count;
        Assert.True(tanim >= asgariSekme, $"{dosya}: {tanim} sekme tanımı (beklenen ≥ {asgariSekme}).");
    }

    [Fact]
    public void S1i_13_Kirik_hucre_kilitleri_telefon_yetki_denetim()
    {
        var blok = S1iBlogu();
        Assert.Contains(".tablo-tek-satir th, .tablo-tek-satir td { white-space: nowrap; }", blok);
        Assert.Contains(".hucre-tek-satir, .hucre-tek-satir .si-tetik { white-space: nowrap; }", blok);
        Assert.Contains(".secim-etiket, .secim-etiket .alan-etiket, .secim-etiket label { white-space: nowrap; }", blok);
        // PAKET GRID-1: /cariler listesi ortak ızgarada — telefon hücresinin sınıfı kolon
        // tanımından gelir (HucreSinif); kilit aynı sınıftır.
        Assert.Contains("HucreSinif = _ => \"hucre-tek-satir\"", Oku("Kuyumcu.Web", "Pages", "Cariler.razor"));
        Assert.Contains("tablo-tek-satir", Oku("Kuyumcu.Web", "Pages", "Yetkiler.razor"));
        // Emoji ikon YOK (denetim izi kilit işaretleri Ikon bileşenine çevrildi).
        var dz = Oku("Kuyumcu.Web", "Pages", "DenetimIzi.razor");
        Assert.DoesNotContain("🔐", dz);
        Assert.DoesNotContain("🔒", dz);
    }

    [Fact]
    public void S1i_14_Sozluk_TR_EN_tam_ve_ayri_dosyada()
    {
        Assert.Equal("Müşteri", SatisEkraniSozlugu.Tr.MusteriEtiketi);
        Assert.Equal("Customer", SatisEkraniSozlugu.En.MusteriEtiketi);
        Assert.Contains("{0}", SatisEkraniSozlugu.Tr.SonFis);
        Assert.Contains("{0}", SatisEkraniSozlugu.En.SonFis);
        Assert.Equal(25, GorselSigmaSozlugu.DenetimSayfaBoyu);       // ZO-BELGE 29.09 devam 6 (7 → 25, tasarım hakemi)
        Assert.Equal(25, GorselSigmaSozlugu.IslemDefteriSayfaBoyu);   // ZO-BELGE 29.09 devam 6 (6 → 25, tasarım hakemi)
        Assert.Equal(10, GorselSigmaSozlugu.KurGecmisIlk);
        Assert.Equal(7, GorselSigmaSozlugu.KasaIlkGun);
    }
    // ── (7) DOĞRULAMA BULGUSU (KRİTİK, 375 px) — Elle ekle satırı dar ekranda ──
    // Doğrulayıcı ölçümü: ≤768 px'te satır esnek/sarmalı kalıyordu → ürün tetiği
    // 123 px, "22 Ayar Burma Bilezik (K22)" üç noktayla kesik; fiyat ↔ Ekle alt ±9,
    // üst ±11 px. Kilit: dar ekran ızgarası (ürün tam satır, adet·fiyat·Ekle aynı
    // üç raylı sıra, Ekle KONTROL rayında ve girdiyle aynı yükseklikte).
    private static string DarEkranKalemBlogu()
    {
        var blok = S1iBlogu();
        var i = blok.IndexOf("@media (max-width: 768px) {\n  .satir.fis-kalem-satiri {", StringComparison.Ordinal);
        Assert.True(i >= 0, "S1-i bloğunda ≤768 px Elle ekle ızgarası yok.");
        var son = blok.IndexOf("\n}\n", i, StringComparison.Ordinal);
        return blok[i..son];
    }

    [Fact]
    public void S1i_15_Dar_ekranda_elle_ekle_izgarasi_urun_tam_satir_ekle_kontrol_rayinda()
    {
        var m = DarEkranKalemBlogu();
        var satir = Kural(m, ".satir.fis-kalem-satiri");
        Assert.Contains("display: grid", satir);
        Assert.Contains("grid-template-columns: minmax(0, 1fr) minmax(0, 1fr) auto", satir);
        Assert.Contains("grid-template-rows: subgrid", Kural(m, ".fis-kalem-satiri > .alan"));
        var urun = Kural(m, ".fis-kalem-satiri > .alan.esnek");
        Assert.Contains("grid-column: 1 / -1", urun);           // ürün adı tam satır → kesilmez
        Assert.Contains("grid-row: 1 / span 3", urun);
        Assert.Contains("grid-row: 4 / span 3", Kural(m, ".fis-kalem-satiri > .alan:not(.esnek)"));
        var ekle = Kural(m, ".fis-kalem-satiri > #islem-kalem-ekle");
        Assert.Contains("grid-row: 5", ekle);                    // KONTROL rayı (4 etiket · 5 kutu · 6 alt)
        Assert.Contains("grid-column: 3", ekle);
        Assert.Contains("height: var(--dokunma-hedefi)", ekle);  // girdiyle (min-height: --dokunma-hedefi) aynı boy
        Assert.DoesNotContain("style=", Oku("Kuyumcu.Web", "Bilesenler", "Fis", "FisKalemGirisi.razor"));
    }

    [Fact]
    public void S1i_16_Barkod_yer_tutucusu_kisa_dugme_metni_ekran_okuyucuda_kalir()
    {
        var c = KalemCiz();
        var kutu = c.Find("#islem-barkod");
        Assert.Equal(SatisEkraniSozlugu.Tr.BarkodKisaIpucu, kutu.GetAttribute("placeholder"));
        Assert.True(SatisEkraniSozlugu.Tr.BarkodKisaIpucu.Length <= 28, "Yer tutucu 375 px'te kutuya sığmıyor.");
        var metin = c.Find(".barkod-satiri .barkod-dugme-metin");
        Assert.Equal("Barkod okut", metin.TextContent.Trim());
        var dugme = c.Find(".barkod-satiri > button");
        Assert.Equal("Barkod okut", dugme.GetAttribute("aria-label"));   // dar ekranda metin gizlenir, erişilebilir ad KALIR
        Assert.Equal("Scan or type the barcode", SatisEkraniSozlugu.En.BarkodKisaIpucu);

        var css = S1iBlogu();
        Assert.Contains("@media (max-width: 480px) {\n  .barkod-satiri .barkod-dugme-metin { display: none; }", css);
        Assert.Contains("#islem-elle[open] > summary { margin-bottom:", css);
    }
}
