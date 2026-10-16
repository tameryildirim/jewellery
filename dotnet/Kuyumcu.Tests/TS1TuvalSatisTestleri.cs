using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-S1 — SATIŞ BASİT MOD = TUVAL (Tezgah.dc.html)
//
// Sınanan:
//   • sağ sütun "Ne yapıyorsun?" 2×4 TUVAL SIRASIYLA (Satış · Alış·eski altın ·
//     Tahsilat · Ödeme · İade · Değişim · Emanet · İndirim), ikonsuz, id'ler korunur
//   • adım şeridi YOK; üst satır Müşteri · Tarih · Para (salt okunur, gövdeye girmez)
//   • VERDİM tablosu Ürün · Ayar · Gram · Tutar ("K22" ham kodu "22"), Toplam · N g
//   • ALDIM tek ödeme satırı Nakit · kasa · tutar; Fark ✓ ALDIM'ın alt çubuğunda
//   • İndirim karosu: kalem yokken kilitli + sebep; tıklanınca fiş iskontosu açılır
//   • Vazgeç: boş fişte çizilmez; fişi boşaltır, İSTEK ATMAZ
//   • Son 3 satış: en çok 3 satır, "Peşin" / "Ayşe D." kısaltması; PRO tam liste
//   • nakit uyarısı EŞİĞİ SUNUCUDAN: sunucu 50.000 derse 50.000 yazar; sunucu
//     yanıt vermezse SAYISIZ metin (istemcide rakam yok)
//   • Kaydet "Kaydet ve yazdır"; kayıttan sonra fiş no + yazdır bağlantısı alt
//     satırın ÜSTÜNDE kalır (QA-10); PRO yüz DEĞİŞMEDİ
//   • tuval-satis.css sabit renk içermez (yalnız var(--…))
//
// ⚠ Sabit para metni YOK: beklenen tutarlar Bicim.Tl(..) ile üretilir (T-K1).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TS1TuvalSatisTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private decimal? tevsikHad = 30_000m;   // null → sunucu önizlemesi 500 döner
    private string adaylar = "[]";
    private bool ikinciUrun;   // T-S1: çok kalemli düz satış (kalem başına ayrı belge) sınanırken ikinci ürün

    public TS1TuvalSatisTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => tevsikHad, () => adaylar, () => ikinciUrun))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<decimal?> had, Func<string> adaylar, Func<bool> ikinci) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, yol, govde));
            if (yol.Contains("/urunler/barkod/"))   // T-S1: test kiracısında barkodu tutan ürün yok → 404 (adla arama yedeği sınanır)
                return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
                { Content = new StringContent("""{"message":"bulunamadı"}""", System.Text.Encoding.UTF8, "application/json") };
            if (yol.Contains("/tevsik/gunluk") && had() is null)
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                { Content = new StringContent("""{"message":"önizleme yok"}""", System.Text.Encoding.UTF8, "application/json") };
            var h = had()?.ToString(CultureInfo.InvariantCulture);
            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") =>
                    """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/tevsik/gunluk") =>
                    $$"""{"had":{{h}},"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":{{h}},"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":3.94,"iscilikTl":0,"satisFiyati":10000,"gramAgirlik":4.3}"""
                    + (ikinci() ? """,{"id":"u2","ad":"Tek Tas Yuzuk","ayarId":"K14","hasGrami":1.2,"iscilikTl":0,"satisFiyati":5000,"gramAgirlik":2}""" : "") + "]",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/islemler/fis/") => """{"fisId":"f1","fisNo":"FIS-000123","kalemler":[{"hareketId":"h1"}]}""",
                var v when v.Contains("/islemler/satis-fisi") => """{"fisId":"f1","fisNo":"FIS-000123","iskontoToplam":0}""",
                var v when v.Contains("/islemler/satis") => """{"id":"01J00000000000000000000001","fisId":"01J00000000000000000000002","tur":"SATIS","tlTutari":10000}""",
                var v when v.Contains("/iade-adaylari") => adaylar(),
                var v when v.Contains("/reyon") => "[]",
                var v when v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-tur-SATIS");
        return e;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> KalemliFis()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        e.WaitForAssertion(() => Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.Equal("10000", SeciciYardim.SeciliDeger(e, "islem-birimfiyat")));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForElement("#islem-kalem-tablo");
        return e;
    }

    private int PostSayisi() { lock (istekler) return istekler.Count(i => i.Metot == "POST"); }

    // ═══ SAF ════════════════════════════════════════════════════════════════

    [Fact]
    public void Karolar_TUVAL_SIRASIYLA_ve_her_basit_tur_bir_karoda()
    {
        // SATIS-3 t11 (tasarım hakemi YÜKSEK): ucu olmayan, hep pasif "Ödeme" karosu kaldırıldı (gerekçeli tuval farkı).
        Assert.Equal(["SATIS", "HURDA_ALIS", "TAHSILAT", "SATIS_IADE", "TAKAS", "EMANET", "INDIRIM"], BasitPos.Karolar);
        foreach (var t in BasitPos.Turler) Assert.Contains(t, BasitPos.Karolar);
        var kisayol = BasitPos.Kisayollar.Select(k => k.Kod).ToHashSet();
        Assert.All(BasitPos.Karolar, k => Assert.True(BasitPos.TurGorunur(k) || kisayol.Contains(k) || k == BasitPos.Indirim, k));
        Assert.False(BasitPos.TurGorunur("ALIS"));   // tuvalde karosuz → PRO
    }

    [Fact]
    public void Sozluk_TR_EN_bos_anahtar_yok_nakit_uyarisinda_RAKAM_YOK_TRY_yok()
    {
        foreach (var m in new[] { SatisTuvalSozlugu.Tr, SatisTuvalSozlugu.En })
            foreach (var p in typeof(SatisTuvalMetinleri).GetProperties())
            {
                var v = (string)p.GetValue(m)!;
                Assert.False(string.IsNullOrWhiteSpace(v), p.Name);
                Assert.DoesNotMatch(@"\bTRY\b", v);
            }
        foreach (var m in new[] { SatisTuvalSozlugu.Tr, SatisTuvalSozlugu.En })
        {
            Assert.DoesNotMatch(@"\d", m.NakitUyari.Replace("{0}", ""));   // eşik SAYISI metinde yok
            Assert.DoesNotMatch(@"\d", m.NakitUyariSayisiz);
            Assert.Contains("{0}", m.NakitUyari);
        }
        Assert.Same(SatisTuvalSozlugu.En, SatisTuvalSozlugu.Al("en"));
        Assert.Same(SatisTuvalSozlugu.Tr, SatisTuvalSozlugu.Al("xx"));
    }

    [Fact]
    public void Kisa_ad_tuval_kisaltmasi_ve_zaman_bugunse_saat()
    {
        Assert.Equal("Hasan K.", FisSonSatislar.KisaAd("Hasan Kaya"));
        Assert.Equal("Ayşe İ.", FisSonSatislar.KisaAd("Ayşe  Nur  ilhan"));
        Assert.Equal("Peşin", FisSonSatislar.KisaAd("Peşin"));
        Assert.Null(FisSonSatislar.KisaAd("  "));
        var simdi = DateTime.UtcNow;
        Assert.Equal(simdi.ToLocalTime().ToString("HH:mm", CultureInfo.GetCultureInfo("tr-TR")), FisSonSatislar.Zaman(simdi));
        Assert.Contains(".", FisSonSatislar.Zaman(simdi.AddDays(-2)));   // dünün satışı "bugün" sanılmaz
    }

    [Fact]
    public void Ayar_ham_kod_basilmaz_K22_22_bilinmeyen_kartin_ayar_adi()
    {
        UrunVeri U(string? id, string? ayar) => new("u", "x", id, ayar, 0m, null, null);
        Assert.Equal("22", FisKalemTablosu.AyarKisa(U("K22", null)));
        Assert.Equal("14", FisKalemTablosu.AyarKisa(U("K14", "14 Ayar")));
        Assert.Equal("Gümüş 925", FisKalemTablosu.AyarKisa(U("AG925", "Gümüş 925")));
        Assert.Equal("—", FisKalemTablosu.AyarKisa(U("KX", null)));
        Assert.Equal("—", FisKalemTablosu.AyarKisa(null));
    }

    [Fact]
    public void Toplam_gram_kalem_gram_x_adet_karti_olmayan_0()
    {
        UrunVeri U(string id, decimal g) => new(id, "x", "K22", null, 0m, null, null, GramAgirlik: g);
        var urunler = new List<UrunVeri> { U("a", 4.30m), U("b", 6.10m), U("c", 0m) };
        var k = new List<FisKalemi> { new() { UrunId = "a", UrunAdi = "x", Adet = 1 }, new() { UrunId = "b", UrunAdi = "x", Adet = 2 }, new() { UrunId = "c", UrunAdi = "x", Adet = 1 }, new() { UrunId = "yok", UrunAdi = "x", Adet = 1 } };
        Assert.Equal(16.50m, FisHesap.ToplamGram(k, urunler));
    }

    [Fact]
    public void Css_sabit_renk_YOK_yalniz_belirtec()
    {
        var yol = Path.Combine(CozumKoku(), "Kuyumcu.Web", "wwwroot", "css", "tuval-satis.css");
        var css = Regex.Replace(File.ReadAllText(yol), @"/\*.*?\*/", "", RegexOptions.Singleline);
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b", css);
        Assert.DoesNotMatch(@"\b(rgb|rgba|hsl|hsla)\(", css);
        Assert.Contains(".islem-sayfa.tuval-tezgah", css);
    }

    private static string CozumKoku()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        return d!.FullName;
    }

    // ═══ EKRAN — BASİT (tuval) ══════════════════════════════════════════════

    [Fact]
    public void Basit_sag_sutun_karolari_tuval_sirasi_ve_adlari_ikonsuz()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        var t = SatisTuvalSozlugu.Tr;
        var karolar = e.Find("#islem-izgara").QuerySelectorAll(".tuval-karo-izgara > .ekran-karo").ToList();
        Assert.Equal(["islem-tur-SATIS", "islem-tur-HURDA_ALIS", "islem-git-tahsilat",
                      "islem-tur-SATIS_IADE", "islem-tur-TAKAS", "islem-git-emanet", "islem-indirim"], karolar.Select(k => k.Id));   // t11: Ödeme yok
        Assert.Equal(["Satış", t.KaroAlisEskiAltin, "Tahsilat", t.KaroIade, t.KaroDegisim, "Emanet", t.KaroIndirim],
                     karolar.Select(k => k.TextContent.Trim()));
        Assert.All(karolar, k => Assert.Null(k.QuerySelector("svg")));
        Assert.Contains("tuval-tezgah", e.Find(".islem-sayfa").ClassName);
        Assert.Empty(e.FindAll("#islem-adim-serit"));
    }

    [Fact]
    public void Ust_satir_BASIT_yalniz_Musteri_Tarih_Para_GORUNMEZ_ve_govdeye_girmez()
    {
        // SATIS-2 (kurucu temel kuralı 25.09 11:35, tuvalden ÜSTÜN): BASİT'te Tarih (bugün) ve Para (TL)
        // HİÇ görünmez — sistemin bildiği değer (UX-S21). PRO'da özet satırında (SATIS2EkranTestleri).
        var e = KalemliFis();
        var ust = e.Find("#islem-ust-satir");
        Assert.NotNull(ust.QuerySelector("#islem-cari"));
        Assert.Empty(e.FindAll("#islem-tarih"));
        Assert.Empty(e.FindAll("#islem-para"));
        Assert.Empty(ust.QuerySelectorAll("input"));   // sistemin bildiği alan istenmez (UX-S21)

        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.True(PostSayisi() > 0));
        lock (istekler)
            foreach (var p in istekler.Where(i => i.Metot == "POST"))
            {
                var g = JsonDocument.Parse(p.Govde).RootElement;
                Assert.False(g.TryGetProperty("tarih", out _));
                Assert.False(g.TryGetProperty("paraBirimi", out _));
            }
    }

    [Fact]
    public void Verdim_tablosu_Urun_Ayar_Gram_Tutar_ve_Toplam_gram()
    {
        var e = KalemliFis();
        var t = SatisTuvalSozlugu.Tr;
        var basliklar = e.FindAll("#islem-kalem-tablo thead th").Select(h => h.TextContent.Trim()).Take(4);
        Assert.Equal([t.SutunUrun, t.SutunAyar, t.SutunGram, t.SutunTutar], basliklar);
        var hucre = e.FindAll("#islem-kalem-tablo tbody tr:first-child td").Select(h => h.TextContent.Trim()).ToList();
        // GOC-C-2: "ayar · gram" ikinci satırı artık ad hücresindeki span (eskiden td ::after) — ad span'ı ayrı okunur.
        Assert.Equal("Burma Bilezik", e.Find("#islem-kalem-tablo tbody tr:first-child td .fis-tuval-ad-metin").TextContent.Trim());
        Assert.Equal("22 · 4,30 g", e.Find("#islem-kalem-tablo tbody tr:first-child td .fis-tuval-ad-ayrinti").TextContent.Trim());
        Assert.Equal("22", hucre[1]);
        Assert.Equal("4,30", hucre[2]);
        Assert.Equal(Bicim.Tl(10_000m), hucre[3]);
        Assert.Contains(string.Format(t.ToplamGram, "4,30"), e.Find("#islem-verdim-toplam").TextContent);
        Assert.Contains(Bicim.Para(10_000m, "TRY"), e.Find("#islem-verdim-toplam").TextContent);
        // satır içi adet/fiyat/iskonto PRO'da; silme yolu basitte VAR (UX-S25)
        Assert.Empty(e.FindAll("[id^=si-kalem-]"));
        e.Find("#islem-kalem-sil-0").Click();
        e.WaitForElement("#islem-verdim-bos");
    }

    [Fact]
    public void Aldim_tek_odeme_satiri_Nakit_kasa_tutar_Fark_alt_cubukta_onayli()
    {
        var e = KalemliFis();
        // PARA-DEMO-2 (madde 1): ilk hücre artık tek ödemenin türünü SEÇTİREN seçici (Nakit/Kart/Havale); değeri Nakit.
        // PARA-DEMO-2 tur 2: kasa adı seçicinin ALTINDA aynı hücrede (dar sütunda tutar kırpılmasın); tutar ikinci hücre.
        var satir = e.FindAll("#islem-aldim-satir td").Skip(1).Select(h => h.TextContent.Trim()).ToList();
        Assert.Equal([Bicim.Para(10_000m, "TRY")], satir);
        Assert.Equal("Merkez Kasa", e.Find("#islem-aldim-satir #islem-aldim-kasa").TextContent.Trim());
        Assert.Equal("NAKIT", SeciciYardim.SeciliDeger(e, "islem-aldim-odeme"));
        var fark = e.Find("#islem-aldim #islem-fark");   // ALDIM sütununun alt çubuğu
        Assert.Contains("fis-fark-tamam", fark.ClassName);
        Assert.NotNull(fark.QuerySelector("#islem-fark-tutar svg"));   // ✓ ince çizgi ikon
        Assert.Contains("gorsel-gizli", e.Find("#islem-fark-durum").ClassName);
        Assert.Equal(SatisTuvalSozlugu.Tr.KaydetYazdir, e.Find("#islem-kaydet").TextContent.Trim());
        Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled"));
    }

    [Fact]
    public void Indirim_karosu_kalemsiz_KILITLI_sebep_yazar_kalemliyken_iskontoyu_acar_kapatir()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        Assert.True(e.Find("#islem-indirim").HasAttribute("disabled"));
        Assert.Equal(SatisTuvalSozlugu.Tr.IndirimKilit, e.Find("#islem-indirim-kilit").TextContent.Trim());
        Assert.Equal("islem-indirim-kilit", e.Find("#islem-indirim").GetAttribute("aria-describedby"));

        e.Dispose();
        var k = KalemliFis();
        Assert.False(k.Find("#islem-indirim").HasAttribute("disabled"));
        Assert.Empty(k.FindAll("#islem-fis-iskonto"));
        var once = PostSayisi();
        k.Find("#islem-indirim").Click();
        k.WaitForElement("#islem-fis-iskonto");
        Assert.Equal("true", k.Find("#islem-indirim").GetAttribute("aria-pressed"));
        k.Find("#islem-indirim").Click();
        k.WaitForAssertion(() => Assert.Empty(k.FindAll("#islem-fis-iskonto")));
        Assert.Equal(once, PostSayisi());   // karo istek atmaz
    }

    [Fact]
    public void Vazgec_bos_fiste_CIZILMEZ_kalemliyken_fisi_bosaltir_ISTEK_ATMAZ()
    {
        var e = KalemliFis();
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.WaitForAssertion(() => Assert.Equal("c1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        var once = PostSayisi();
        e.Find("#islem-vazgec").Click();
        e.WaitForElement("#islem-verdim-bos");
        Assert.Empty(e.FindAll("#islem-vazgec"));
        e.WaitForAssertion(() => Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        Assert.Equal(once, PostSayisi());
    }

    [Fact]
    public void Nakit_uyarisi_esigi_SUNUCUDAN_okur_istemcide_rakam_yok()
    {
        tevsikHad = 50_000m;
        var e = KalemliFis();
        e.WaitForAssertion(() => Assert.Equal(string.Format(SatisTuvalSozlugu.Tr.NakitUyari, Bicim.Tl(50_000m)),
                                              e.Find("#islem-nakit-uyari").TextContent.Trim()), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Nakit_uyarisi_sunucu_yanit_vermezse_SAYISIZ_metin()
    {
        tevsikHad = null;
        var e = KalemliFis();
        Assert.Equal(SatisTuvalSozlugu.Tr.NakitUyariSayisiz, e.Find("#islem-nakit-uyari").TextContent.Trim());
    }

    [Fact]
    public void Son_uc_satis_en_cok_uc_satir_Pesin_ve_kisa_ad_PRO_tam_liste()
    {
        var bugun = DateTime.UtcNow.ToString("o");
        adaylar = "[" + string.Join(",", new[] { ("a1", "pm1", 12400), ("a2", "c1", 38900), ("a3", "pm1", 6300), ("a4", "c1", 1000) }
            .Select(x => $$"""{"id":"{{x.Item1}}","tarih":"{{bugun}}","tlTutari":{{x.Item3}},"urunId":"u1","urunAdi":"Burma Bilezik","toplamAdet":1,"iadeEdilenAdet":0,"kalanAdet":1,"cariId":"{{x.Item2}}"}""")) + "]";
        var e = Ekran(ArayuzModuServisi.Basit);
        Assert.Empty(e.FindAll("#islem-son-liste"));   // SATIS-2: kart KATLI gelir, liste istenmez
        e.Find("#islem-son-satislar-ac").Click();
        e.WaitForElement("#islem-son-liste");
        var satirlar = e.FindAll("#islem-son-liste > li");
        Assert.Equal(3, satirlar.Count);
        Assert.Contains("Peşin", satirlar[0].TextContent);
        Assert.Contains("Ayşe D.", satirlar[1].TextContent);
        Assert.Contains(Bicim.Tl(38_900m), satirlar[1].TextContent);
        Assert.Equal(SatisTuvalSozlugu.Tr.SonUcSatis, e.Find("#islem-son-satislar h2").TextContent.Trim());
        Assert.Equal("/raporlar/islem-defteri", e.Find("#islem-son-defter").GetAttribute("href"));
        Assert.Empty(e.FindAll("#islem-defter-liste"));

        e.Dispose();
        var p = Ekran(ArayuzModuServisi.Profesyonel);
        p.Find("#islem-son-satislar-ac").Click();
        p.WaitForElement("#islem-defter-liste-sar");
        Assert.Empty(p.FindAll("#islem-son-liste"));
    }

    [Fact]
    public void Kayittan_sonra_fis_no_ve_yazdir_alt_satirin_USTUNDE_kalir()
    {
        var e = KalemliFis();
        e.Find("#islem-kaydet").Click();
        e.WaitForElement("#islem-yazdir");
        var govde = e.Find(".islem-fis-kart > .kart-govde");
        var sira = govde.Children.ToList();
        var yazdir = sira.FindIndex(c => c.Id == "islem-yazdir-sar");
        var alt = sira.FindIndex(c => c.Id == "islem-alt-satir");
        Assert.True(yazdir >= 0 && alt > yazdir, $"yazdır {yazdir} · alt satır {alt}");
    }

    [Fact]
    public void Kayittan_sonra_son_fis_ozeti_ULIDsiz_saat_ve_tutar_fis_bosalir_ozet_KALIR()
    {
        // QA-10 kalanı: sunucu fiş no'yu ULID döndürür (okunur numara yok) → "Son fiş"
        // satırı kaydedilenin saati + tutarıyla bir sonraki Kaydet'e kadar kalır.
        var e = KalemliFis();
        var toplam = Bicim.Tl(10000m);
        e.Find("#islem-kaydet").Click();
        e.WaitForElement("#islem-son-fis");
        var metin = e.Find("#islem-son-fis").TextContent;
        Assert.Contains(toplam, metin);
        Assert.Matches(@"\d{2}:\d{2}", metin);
        Assert.DoesNotMatch(@"01J0{10}", metin);   // ham ULID basılmaz
        Assert.Empty(e.FindAll("#islem-kalem-tablo tbody tr"));   // fiş boşaldı, özet kaldı
        Assert.NotNull(e.Find("#islem-yazdir"));
    }

    // ═══ "KAYDET VE YAZDIR" — ETİKET DAVRANIŞLA AYNI (doğrulayıcı bulgusu, 24.09) ═══
    // Tek belge → kayıttan SONRA yazdırma sekmesi kendiliğinden açılır (window.open).
    // Çok kalemli düz satış kalem başına ayrı belge doğurur → etiket "Kaydet", not yazılı,
    // sekme açılmaz. PRO yüz yazdırma vaat etmez → açılmaz.

    private List<JSRuntimeInvocation> AcilanSekmeler() => JSInterop.Invocations.Where(i => i.Identifier == "open").ToList();

    private int SatisPostSayisi() { lock (istekler) return istekler.Count(i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis")); }

    [Fact]
    public void Aldim_sutununda_para_birimi_eki_girdiyle_AYNI_SATIRDA_kalir_css()
    {
        // Doğrulayıcı bulgusu (DÜŞÜK): 1280 px ALDIM sütununda "Alınan" kutusunun ₺ eki alt satıra düşüyordu.
        var yol = Path.Combine(CozumKoku(), "Kuyumcu.Web", "wwwroot", "css", "tuval-satis.css");
        var css = Regex.Replace(File.ReadAllText(yol), @"/\*.*?\*/", "", RegexOptions.Singleline);
        Assert.Matches(@"\.tuval-tezgah #islem-aldim \.alan-girdi-satir \{ flex-wrap: nowrap; \}", css);
        Assert.Matches(@"\.tuval-tezgah #islem-aldim \.alan-girdi-satir > \.girdi \{ flex: 1 1 0; min-width: 0; \}", css);
    }

    [Fact]
    public void TekBelge_iade_fis_ucu_tek_kalem_EVET_cok_kalemli_duz_satis_HAYIR()
    {
        Assert.True(FisYazdir.TekBelge(iade: false, kalemSayisi: 1, fisUcu: false));
        Assert.True(FisYazdir.TekBelge(iade: false, kalemSayisi: 3, fisUcu: true));
        Assert.True(FisYazdir.TekBelge(iade: true, kalemSayisi: 2, fisUcu: false));
        Assert.False(FisYazdir.TekBelge(iade: false, kalemSayisi: 2, fisUcu: false));
    }

    [Fact]
    public void Kaydet_ve_yazdir_TEK_belgede_kayittan_SONRA_yazdirma_sekmesini_KENDILIGINDEN_acar()
    {
        var e = KalemliFis();
        Assert.Equal(SatisTuvalSozlugu.Tr.KaydetYazdir, e.Find("#islem-kaydet").TextContent.Trim());
        Assert.Empty(e.FindAll("#islem-kaydet-cokfis"));
        Assert.Empty(AcilanSekmeler());   // kayıttan ÖNCE yazdırma yok
        e.Find("#islem-kaydet").Click();
        e.WaitForElement("#islem-yazdir-otomatik", TimeSpan.FromSeconds(5));
        var acilan = Assert.Single(AcilanSekmeler());
        Assert.Equal(FisYazdir.YazdirAdresi("01J00000000000000000000001"), acilan.Arguments[0]);
        Assert.Equal("_blank", acilan.Arguments[1]);
        Assert.Equal(e.Find("#islem-yazdir").GetAttribute("href"), acilan.Arguments[0]);   // açılan belge = görünen bağlantı
        Assert.Single(e.FindAll("#islem-yazdir-sar a"));   // tek fiş, tek belge
        Assert.Equal(SatisTuvalSozlugu.Tr.YazdirAcildi, e.Find("#islem-yazdir-otomatik").TextContent.Trim());
        Assert.Empty(e.FindAll("#islem-yazdir-ipucu"));   // aynı bilgi iki kez yazılmaz
        Assert.Equal(1, SatisPostSayisi());
    }

    // PAKET L-1 (tevsik açığı, 25.09): bu test T-S1'de ESKİ yolu (kalem başına ayrı POST, "N ayrı fiş"
    // notu) belgeliyordu. Çok kalemli satış artık TEK POST /islemler/satis-fisi → tek belge; test yeni
    // davranışı sınar (ayrıntı: L1TekFisEkranTestleri).
    [Fact]
    public void Cok_kalemli_satis_L1_TEK_fis_etiket_KAYDET_VE_YAZDIR_kalem_basi_POST_YOK()
    {
        ikinciUrun = true;
        var e = KalemliFis();
        SeciciYardim.Sec(e, "islem-urun", "u2");
        e.WaitForAssertion(() => Assert.Equal("u2", SeciciYardim.SeciliDeger(e, "islem-urun")));
        e.WaitForAssertion(() => Assert.Equal("10000", SeciciYardim.SeciliDeger(e, "islem-birimfiyat")));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll("#islem-kalem-tablo tbody tr").Count), TimeSpan.FromSeconds(5));
        Assert.Equal(SatisTuvalSozlugu.Tr.KaydetYazdir, e.Find("#islem-kaydet").TextContent.Trim());
        Assert.Empty(e.FindAll("#islem-kaydet-cokfis"));
        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => { lock (istekler) Assert.Equal(1, istekler.Count(i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis-fisi"))); }, TimeSpan.FromSeconds(5));
        Assert.Equal(0, SatisPostSayisi());   // L-1: kalem başına POST YOK
        Assert.Empty(e.FindAll("#islem-yazdir-2"));
    }

    // T-S2 (tuval Profesyonel.dc.html "Kaydet ve yazdır"): PRO satış yüzü artık BASİT ile AYNI kuralı
    // izler — tek belgede etiket yazdırma vaat eder ve kayıttan SONRA sekme açar. (T-S1'deki
    // "PRO yazdırma vaat etmez" iddiası T-S2 ile bilerek değişti; rapor § sapmalar.)
    [Fact]
    public void Pro_modda_tek_belgede_Kaydet_ve_yazdir_kayittan_SONRA_sekme_acar()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.Equal("10000", SeciciYardim.SeciliDeger(e, "islem-birimfiyat")));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForElement("#islem-kalem-liste-sar");
        Assert.Contains(SatisTuvalSozlugu.Tr.KaydetYazdir, e.Find("#islem-kaydet").TextContent);
        Assert.Empty(AcilanSekmeler());
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled"),
            "Kaydet kilitli: " + string.Join(" | ", e.FindAll("#islem-kaydet-kilit, .alan-hata").Select(x => x.TextContent.Trim()))), TimeSpan.FromSeconds(5));
        e.Find("#islem-kaydet").Click();
        e.WaitForElement("#islem-yazdir", TimeSpan.FromSeconds(5));
        e.WaitForAssertion(() => Assert.Single(AcilanSekmeler()), TimeSpan.FromSeconds(5));
    }

    // ═══ EKRAN — PROFESYONEL = tuval Profesyonel.dc.html (T-S2) ═══════════════

    [Fact]
    public void Pro_yuz_tezgah_ogeleri_YOK_tuval_pro_ogeleri_VAR()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        Assert.DoesNotContain("tuval-tezgah", e.Find(".islem-sayfa").ClassName);
        Assert.Contains("tuval-pro", e.Find(".islem-sayfa").ClassName);
        Assert.NotNull(e.Find("#islem-ust-satir"));            // T-S2: alan satırı PRO satışta da var
        Assert.NotNull(e.Find("#islem-tarih"));
        Assert.Empty(e.FindAll("#islem-nakit-uyari"));
        Assert.Empty(e.FindAll("#islem-vazgec"));              // boş fişte Vazgeç çizilmez
        Assert.Empty(e.FindAll("#islem-indirim"));
        Assert.Equal(8, e.FindAll("[id^=islem-tur-]").Count);
        Assert.NotNull(e.Find("#islem-tur-ALIS"));
        Assert.Equal(SatisTuvalSozlugu.Tr.KaydetYazdir, e.Find("#islem-kaydet").TextContent.Trim());
    }

    // ═══ T-S1 v2 — tuval metni "Barkodu okut ya da ürün adı yaz" · eşik ilk açılışta · karma satırları ═══

    [Fact]
    public void AdlaBul_birebir_ad_tek_icerik_coklu_eslesme_UYDURULMAZ_trTR_buyuk_kucuk()
    {
        var liste = new[] { ("u1", "Burma Bilezik"), ("u2", "Burma Bilezik 22"), ("u3", "Alyans (çift)"), ("u4", "İnce Zincir") };
        Assert.Equal(("u1", 1), BasitPos.AdlaBul(liste, "burma bilezik"));   // birebir ad → tek ürün
        Assert.Equal(("u3", 1), BasitPos.AdlaBul(liste, "ALYANS"));          // içerik, tek eşleşme
        Assert.Equal(((string?)null, 2), BasitPos.AdlaBul(liste, "burma"));  // iki ürün → seçilmez, sayı söylenir
        Assert.Equal(("u4", 1), BasitPos.AdlaBul(liste, "ince"));            // tr-TR: "İnce" = "ince"
        Assert.Equal(((string?)null, 0), BasitPos.AdlaBul(liste, "b"));      // < 2 karakter aranmaz
        Assert.Equal(((string?)null, 0), BasitPos.AdlaBul(liste, "kolye"));
    }

    [Fact]
    public void Barkod_bulunamazsa_URUN_ADIYLA_eklenir_istek_yalniz_barkod_ucu()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        e.WaitForAssertion(() => Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        Assert.Equal(SatisTuvalSozlugu.Tr.BarkodYerTutucu, e.Find("#islem-barkod").GetAttribute("placeholder"));
        e.Find("#islem-barkod").Input("burma");
        e.Find("#islem-barkod").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#islem-kalem-tablo tbody tr")), TimeSpan.FromSeconds(5));
        Assert.Contains("Burma Bilezik", e.Find("#islem-kalem-tablo tbody tr").TextContent);
        lock (istekler) Assert.Contains(istekler, i => i.Yol.Contains("/urunler/barkod/burma"));
    }

    [Fact]
    public void Barkod_ne_barkod_ne_ad_tutarsa_BULUNAMADI_kalem_eklenmez()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        e.WaitForAssertion(() => Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        e.Find("#islem-barkod").Input("kolye");
        e.Find("#islem-barkod").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        e.WaitForAssertion(() => Assert.Contains(Dil().T.Urun.BarkodBulunamadi, e.Markup), TimeSpan.FromSeconds(5));
        Assert.Empty(e.FindAll("#islem-kalem-tablo tbody tr"));
    }

    [Fact]
    public void Pro_satis_barkod_yer_tutucusu_TEK_GIRIS_SATIS2()
    {
        // SATIS-2 (d+e): PRO satışta tek giriş — tuvalin "Ürün ara ya da barkod okut (F2)" kutusu.
        var p = Ekran(ArayuzModuServisi.Profesyonel);
        Assert.Equal(SatisSadeSozlugu.Tr.UrunAraYerTutucu, p.Find("#islem-barkod").GetAttribute("placeholder"));
    }

    [Fact]
    public void Nakit_uyarisi_BOS_fiste_de_sunucu_esigini_yazar_ilk_acilista_istek_atilir()
    {
        tevsikHad = 40_000m;
        var e = Ekran(ArayuzModuServisi.Basit);
        e.WaitForAssertion(() => Assert.Equal(string.Format(SatisTuvalSozlugu.Tr.NakitUyari, Bicim.Tl(40_000m)),
                                              e.Find("#islem-nakit-uyari").TextContent.Trim()), TimeSpan.FromSeconds(5));
        Assert.Empty(e.FindAll("#islem-kalem-tablo tbody tr"));   // kalem YOK — eşik yine sunucudan
    }

    [Fact]
    public void Profil_SONRADAN_yuklenip_mod_BASIT_olunca_nakit_esigi_o_an_sunucudan_okunur()
    {
        // Gerçek tarayıcıda ölçüldü: profil yüklenene dek mod PROFESYONEL'dir (ArayuzModuServisi);
        // sayfa önce pro açılıp sonra basite dönünce eşik isteği ilk açılışta atılmamış olur.
        tevsikHad = 35_000m;
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        Assert.Empty(e.FindAll("#islem-nakit-uyari"));
        e.InvokeAsync(() => Services.GetRequiredService<ArayuzModuServisi>().Uygula(ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        e.WaitForAssertion(() => Assert.Equal(string.Format(SatisTuvalSozlugu.Tr.NakitUyari, Bicim.Tl(35_000m)),
                                              e.Find("#islem-nakit-uyari").TextContent.Trim()), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Karma_odemede_ALDIM_sutun_basliklari_Odeme_Ayrinti_Tutar_tek_odemede_yok()
    {
        var e = KalemliFis();
        Assert.Empty(e.FindAll("#islem-aldim-bacak-baslik"));
        e.Find("#islem-odeme-bol").Click();
        e.WaitForElement("#islem-aldim-bacak-baslik");
        var b = e.FindAll("#islem-aldim-bacak-baslik > span").Select(x => x.TextContent.Trim()).ToList();
        Assert.Equal([SatisTuvalSozlugu.Tr.SutunOdeme, SatisTuvalSozlugu.Tr.SutunAyrinti, SatisTuvalSozlugu.Tr.SutunTutar, ""], b);
        Assert.NotNull(e.Find("#islem-bacak-0-arac"));   // düzenleme alanları AYNI id'lerle duruyor
        Assert.NotNull(e.Find("#islem-bacak-0-tutar"));
    }

    private DilServisi Dil() => Services.GetRequiredService<DilServisi>();
}
