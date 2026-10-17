using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-BELGE — "ZORUNLU ÖNCE" (kurucu 25.09) · bUnit + kaynak kilitleri
//
// Yedi ekran: Faturalar · Evraklar · Raporlar · İşlem defteri · Kasa raporu ·
// Denetim izi · Veri içe aktarım. Kural: açılışta başlık + TEK birincil eylem +
// tek özet + ana tablo; isteğe bağlı her şey KAPALI katlanırda; "değiştirilemez"
// kutusu açılışta değil, kayıt açılınca / değiştirme denenince.
//
// ⚠ Sahte API her uca GERÇEKÇİ yanıt verir ("{}" varsayılanı yalnız ilgisiz
//   uçlar için). Hata senaryosu `hatali` kümesiyle o uca 500 döndürür.
// ⚠ Para metni SABİT yazılmaz: beklenen değer Bicim.Para/Tl ile üretilir.
// ⚠ bUnit CSS uygulamaz: görünürlük/sığma kanıtı raporun tarayıcı tablosudur.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class ZoBelgeTestleri : BunitContext
{
    private readonly List<string> istekler = [];
    private readonly HashSet<string> hatali = [];
    private string kasaDefteri = KasaDefteriBugunAcik();
    private string islemDefteri = IslemDefteri;
    private string? denetimListe;
    /// <summary>Kasa defteri: bitiş BUGÜN değilse (süzgeç geçmişe alındı) dönen yanıt.</summary>
    private string? kasaDefteriGecmis;
    private string aylik = "{}";
    private string gunluk = "{}";

    private static ZoBelgeMetinleri ZB => ZoBelgeSozlugu.Tr;

    public ZoBelgeTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Kuyumcu.Web.Bilesenler.KayitSecici.BeklemeMs = 0;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<UyumServisi>();   // Raporlar (PRO) enjekte eder — ZB55
    }

    /// <summary>Mod: DilServisi ArayuzModuServisi'ne bağlanır (Program.cs ile aynı).</summary>
    private void Mod(string mod)
    {
        var m = Services.GetRequiredService<ArayuzModuServisi>();
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => m.BasitMi);
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
    }

    private sealed class SahteApi(ZoBelgeTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var uri = istek.RequestUri!;
            var yol = uri.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add($"{istek.Method.Method} {uri.PathAndQuery} {govde}");

            if (t.hatali.Any(h => yol.EndsWith(h, StringComparison.Ordinal)))
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"message":"Sunucu hatası (test)"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            var g = yol switch
            {
                // ── Kasa raporu ──
                // PARA-DEMO: sayım çekmecesi nakit defterini bu uçtan okur (kart/havale yok → nakit = bakiye).
                var x when x.EndsWith("/kasalar/ks1/gun-kapanisi/onizleme") =>
                    """{"kasaId":"ks1","paraBirimi":"TRY","toplamBakiye":1200.00,"kartToplam":0,"havaleToplam":0,"nakitDefter":1200.00}""",
                var x when x.EndsWith("/kasalar/ks2/gun-kapanisi/onizleme") =>
                    """{"kasaId":"ks2","paraBirimi":"USD","toplamBakiye":300.00,"kartToplam":0,"havaleToplam":0,"nakitDefter":300.00}""",
                var x when x.EndsWith("/gun-kapanisi") =>
                    """{"kasaId":"ks1","yerelGun":"2026-09-25","sayilanNakit":1500.00,"defterBakiye":1200.00,"fark":300.00,"aciklama":null,"kilitli":true}""",
                var x when x.EndsWith("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},{"id":"ks2","ad":"Döviz Kasası","paraBirimi":"USD","aktif":true}]""",
                var x when x.EndsWith("/raporlar/kasa-defteri") =>
                    t.kasaDefteriGecmis is not null && !uri.Query.Contains("bitis=" + Bugun, StringComparison.Ordinal)
                        ? t.kasaDefteriGecmis : t.kasaDefteri,
                // ── İşlem defteri ──
                var x when x.EndsWith("/raporlar/islem-defteri") => t.islemDefteri,
                var x when x.EndsWith("/raporlar/aylik") => t.aylik,
                var x when x.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true},{"id":"s2","ad":"Kadıköy","aktif":true}]""",
                var x when x.EndsWith("/kullanicilar") => """[{"id":"u1","ad":"Ayşe","soyad":"Yılmaz"},{"id":"u2","ad":"Can","soyad":"Demir"}]""",
                // ── Denetim izi ──
                var x when x.EndsWith("/denetim/ozet") =>
                    """{"toplam":3,"hassasAdet":1,"enEskiKayit":"2026-09-01T08:00:00Z","eylemler":[{"eylem":"OKUMA","adet":3}],"turler":[{"varlikTuru":"Cari","adet":3}]}""",
                var x when x.EndsWith("/denetim") && t.denetimListe is not null => t.denetimListe,
                var x when x.EndsWith("/denetim") =>
                    """{"toplam":1,"limit":10,"offset":0,"kayitlar":[{"id":"d1","tarih":"2026-09-13T10:00:00Z","eylem":"OKUMA","varlikTuru":"Cari","varlikId":"c1","hassas":true,"detay":"kayıt","ipAdresi":null,"kullaniciId":"u1","kullanici":"Ayşe Yılmaz","rol":"SAHIP","subeId":null,"subeAd":null,"kasaId":null,"kasaAd":null}]}""",
                // ── Veri içe aktarım ──
                var x when x.EndsWith("/ice-aktarim/sablonlar") => IceSablon,
                var x when x.Contains("/ice-aktarim/gecmis") => IceGecmis,
                // ── Faturalar ──
                var x when x.Contains("/ozel-matrah-ayari") =>
                    """{"kod":null,"acik":false,"aralikta":false,"aralik":"801-812","varsayilanOran":0.20,"kaynakBeyani":"Kaynak beyanı (test).","maliMusavirTeyidi":false}""",
                var x when x.Contains("/kalemler") =>
                    """{"faturaId":"f2","faturaNo":"FTR-2026-000002","kalemKaynagi":"BASLIK","hasDegerKaynagi":"ELLE","toplam":1200.00,"kalemler":[]}""",
                var x when x.EndsWith("/faturalar") && istek.Method == HttpMethod.Get => FaturaListe,
                var x when x.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik"}]""",
                // ── Evraklar ──
                var x when x.EndsWith("/evraklar") && istek.Method == HttpMethod.Get => EvrakListe,
                var x when x.EndsWith("/bankalar") =>
                    """[{"id":"b1","hesapAdi":"Vadesiz TL","bankaAdi":"Ziraat","paraBirimi":"TRY"}]""",
                var x when x.Contains("pos-fatura-mutabakati") => Mutabakat,
                var x when x.EndsWith("/raporlar/gunluk") => t.gunluk,
                var x when x.EndsWith("/pozisyon") =>   // ZB58: BASİT "Altın pozisyonum" kartı gerçekçi zarf ister
                    """{"anaMaden":"ALTIN","altin":{"maden":"ALTIN","vitrinGram":10,"hurdaGram":0,"cariAlacakGram":0,"cariBorcGram":0,"netGram":10,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},"digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,"hesaplanmaAni":"2026-09-29T09:00:00Z","donemdenBagimsiz":true}""",
                var x when x.Contains("/raporlar/") => "{}",   // ör. /raporlar/cari-yaslandirma "/cari" dalına düşmesin (ZB55)
                var x when x.EndsWith("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz"}]""",
                var x when x.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz"}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    // ── Sahte veri ──────────────────────────────────────────────────────────

    private static string Bugun => DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    private static string Dun => DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>İki kasa: ks1 bugün AÇIK (dün farksız), ks2 bugün KAPALI.</summary>
    private static string KasaDefteriBugunAcik(decimal? dunkuFark = null) => $$"""
        {"baslangic":"{{Dun}}","bitis":"{{Bugun}}","saatDilimi":"Europe/Istanbul","utcOfsetSaat":3,
         "yazSaatiRiski":false,"toplamSatir":3,"kasalar":[
          {"kasaId":"ks1","kasaAd":"Merkez Kasa","paraBirimi":"TRY","acilisDeviri":0,
           "kapanisBakiye":1200.00,"guncelBakiye":1200.00,"gunler":[
             {"tarih":"{{Dun}}","devir":0,"giris":500.00,"cikis":0,"hareketSayisi":1,"kapanis":500.00,
              "durum":"KAPALI","sayilanNakit":{{(500m + (dunkuFark ?? 0m)).ToString(System.Globalization.CultureInfo.InvariantCulture)}},
              "kapanisDefterBakiye":500.00,"fark":{{(dunkuFark ?? 0m).ToString(System.Globalization.CultureInfo.InvariantCulture)}}},
             {"tarih":"{{Bugun}}","devir":500.00,"giris":700.00,"cikis":0,"hareketSayisi":2,"kapanis":1200.00,
              "durum":"ACIK","sayilanNakit":null,"kapanisDefterBakiye":null,"fark":null}]},
          {"kasaId":"ks2","kasaAd":"Döviz Kasası","paraBirimi":"USD","acilisDeviri":0,
           "kapanisBakiye":300.00,"guncelBakiye":300.00,"gunler":[
             {"tarih":"{{Bugun}}","devir":300.00,"giris":0,"cikis":0,"hareketSayisi":0,"kapanis":300.00,
              "durum":"KAPALI","sayilanNakit":300.00,"kapanisDefterBakiye":300.00,"fark":0}]}]}
        """;

    private const string IslemDefteri = """
        {"baslangic":"2026-09-18","bitis":"2026-09-24","toplamSatir":2,"ofset":0,"limit":10,"dahaFazlasiVar":false,
         "ozet":{"tlGiris":1500,"tlCikis":200,"hasGiris":0,"hasCikis":0,"satirSayisi":2},
         "storno":{"adet":1,"tutarTl":100},
         "satirlar":[{"id":"h1","tarih":"2026-09-20T10:00:00Z","tur":"SATIS","odemeTuru":"NAKIT","tlTutari":1500,"hasGrami":0},
                     {"id":"h2","tarih":"2026-09-20T11:00:00Z","tur":"ALIS","odemeTuru":"NAKIT","tlTutari":-200,"hasGrami":0}]}
        """;

    private const string IceSablon = """
        {"onayMetni":"VERİLERİ İÇE AKTARMAYI ONAYLIYORUM","maksSatir":5000,
         "maksDosyaBayt":2097152,"gerekceAsgari":20,"politikalar":["HATA","ATLA"],
         "kaynaklar":[{"ad":"cari","kolonlar":[{"ad":"ad","zorunlu":true,"ornek":"Ayşe"}]}]}
        """;

    private const string IceGecmis = """
        {"toplam":1,"limit":20,"kayitlar":[
          {"id":"o0","kullaniciId":"u1","kaynakAd":"cari","dosyaAdi":"eski.csv",
           "gerekce":"Eski programdan müşteri kartları taşınıyor","durum":"UYGULANDI",
           "yinelenenPolitikasi":"HATA","toplamSatir":3,"gecerliSatir":3,"hataliSatir":0,
           "atlananSatir":0,"uygulananSatir":3,"olusturuldu":"2026-09-17T08:00:00Z",
           "uygulandi":null,"geriAlindi":null}]}
        """;

    private const string FaturaListe = """
        {"faturalar":[
          {"id":"f1","faturaNo":null,"cariAd":"Ayşe Demir","tur":"SATIS","araToplam":1000.00,"kdvOran":0.2,
           "kdvTutar":200.00,"toplam":1200.00,"durum":"TASLAK","bankaHareketId":null,"tarih":"2026-09-21T08:00:00Z",
           "kalemKaynagi":"BASLIK","hasDegerKaynagi":"ELLE","cariId":"c1","aciklama":null},
          {"id":"f2","faturaNo":"FTR-2026-000002","cariAd":"Can Demir","tur":"SATIS","araToplam":1000.00,"kdvOran":0.2,
           "kdvTutar":200.00,"toplam":1200.00,"durum":"KESILDI","bankaHareketId":"bh1","tarih":"2026-09-21T09:00:00Z",
           "kalemKaynagi":"BASLIK","hasDegerKaynagi":"ELLE","cariId":"c1","aciklama":null}]}
        """;

    private const string EvrakListe = """
        {"toplamAdet":1,"toplamTutar":30000.00,"limit":200,"azamiLimit":200,"offset":0,
         "dahaFazlasiVar":false,"portfoydeTutar":30000.00,"tahsileVerilenTutar":0.00,
         "buHaftaVadeliTutar":0.00,"karsiliksizTutar":2500.00,
         "satirlar":[
           {"id":"e1","evrakTuru":"CEK","yon":"ALINAN","durum":"PORTFOYDE","tutar":30000.00,
            "paraBirimi":"TRY","vadeGunu":"2026-12-10","evrakNo":"CEK-1","bankaAdi":"Ziraat",
            "bankaSube":null,"kesideYeri":null,"kesideTarihi":null,
            "borcluCariId":null,"borcluAd":null,"kaynakCariId":"c1","kaynakCariAd":"Ayşe Yılmaz",
            "notlar":null,"gecikmeGun":0,"gecikmis":false,"yakindaVadeli":false,"duzenlenebilir":true,
            "izinliGecisler":["TAHSILE_VERILDI","KARSILIKSIZ"]}]}
        """;

    private int Say(string parca) => istekler.Count(i => i.Contains(parca, StringComparison.Ordinal));

    // ═══ KASA RAPORU ════════════════════════════════════════════════════════

    [Fact]
    public void ZB01_Kasa_raporu_acilista_kasa_basina_ozet_satiri_tablo_ve_degismez_kapali()
    {
        var c = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        c.WaitForElement("#kr-ozet-ks1");

        // Özet: ad + kasadaki para (kendi para birimiyle, UX-S32) + bugünün durumu.
        Assert.Equal(Bicim.Para(1200m, "TRY"), c.Find("#kr-bakiye-ks1").TextContent.Trim());
        Assert.Equal(Bicim.Para(300m, "USD"), c.Find("#kr-bakiye-ks2").TextContent.Trim());
        Assert.Equal(ZB.KasaBugunSayilmadi, c.Find("#kr-bugun-ks1").TextContent.Trim());
        Assert.Contains(ZB.KasaBugunSayildi, c.Find("#kr-bugun-ks2").TextContent);
        // TEK birincil eylem: yalnız bugün açık kasada "Sayım gir".
        Assert.Contains("dugme-birincil", c.Find("#kr-sayim-ks1").ClassList);
        Assert.Empty(c.FindAll("#kr-sayim-ks2"));

        // Açılışta: gün tablosu, tarih süzgeci ve "değiştirilemez" kutusu YOK.
        Assert.Empty(c.FindAll("#kr-tablo-ks1"));
        Assert.Empty(c.FindAll("#kr-baslangic"));
        Assert.Empty(c.FindAll("#degismez-ipucu-Rapor"));
        Assert.Equal("false", c.Find("#kr-gecmis-ac").GetAttribute("aria-expanded"));

        // Geçmiş kayıtlar açılınca hepsi AYNEN gelir (yalnız varsayılan kapalı).
        c.Find("#kr-gecmis-ac").Click();
        Assert.Equal(2, c.FindAll("#kr-tablo-ks1 tbody tr").Count);
        Assert.NotNull(c.Find("#kr-baslangic"));
        Assert.NotNull(c.Find("#kr-kasa"));          // iki kasa → seçici
        Assert.NotNull(c.Find("#degismez-ipucu-Rapor"));
    }

    [Fact]
    public void ZB02_Kasa_raporu_sayim_farki_tablo_KAPALIYKEN_de_ozette_kirmizi_yazar()
    {
        kasaDefteri = KasaDefteriBugunAcik(dunkuFark: -50m);
        var c = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        c.WaitForElement("#kr-ozet-ks1");

        var uyari = c.Find("#kr-fark-uyari-ks1");
        Assert.Equal(string.Format(ZB.KasaFarkliGun, 1), uyari.TextContent.Trim());
        Assert.Contains("rozet-kirmizi", uyari.ParentElement!.ClassList);
        // Farksız kasada uyarı çizilmez (sıfır fark "fark" değildir).
        Assert.Empty(c.FindAll("#kr-fark-uyari-ks2"));
    }

    [Fact]
    public void ZB03_Kasa_sayimi_BOS_gonderilemez_sifir_sayilmaz_alan_alti_mesaj()
    {
        var c = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        c.WaitForElement("#kr-sayim-ks1");
        c.Find("#kr-sayim-ks1").Click();
        c.WaitForElement("#kr-sayim-tutar");

        // Girdi ailesi: ham type=number YOK, kutu BOŞ başlar.
        Assert.Equal("text", c.Find("#kr-sayim-tutar").GetAttribute("type"));
        Assert.Equal("", c.Find("#kr-sayim-tutar").GetAttribute("value") ?? "");

        c.Find("#cekmece-gun-sayimi-kaydet").Click();
        c.WaitForAssertion(() => Assert.Equal(ZB.KasaSayimZorunlu,
            c.Find("#alan-hata-kr-sayim-tutar").TextContent.Trim()));
        // PARA-DEMO: çekmece açılışta nakit defterini GET ile okur (onizleme); kural POST yokluğudur.
        Assert.Equal(0, istekler.Count(i => i.StartsWith("POST ", StringComparison.Ordinal) && i.Contains("gun-kapanisi", StringComparison.Ordinal)));

        // Tutar girilince gerçek istek gider; gövdede yazılan sayı var.
        c.Find("#kr-sayim-tutar").Input("1500");
        c.Find("#cekmece-gun-sayimi-kaydet").Click();
        c.WaitForAssertion(() => Assert.Single(istekler, i => i.StartsWith("POST ", StringComparison.Ordinal) && i.Contains("kasalar/ks1/gun-kapanisi")));
        Assert.Contains("\"sayilanNakit\":1500", istekler.Last(i => i.Contains("gun-kapanisi")));
    }

    // ═══ İŞLEM DEFTERİ ══════════════════════════════════════════════════════

    [Fact]
    public void ZB04_Islem_defteri_suzgec_KAPALI_secilince_baslikta_adi_ve_sayisi_yazar()
    {
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-ozet");

        // Açılış: tarih + Yazdır açık; Tür/Ödeme/Şube/Kaydeden kapalı katlanırda.
        Assert.NotNull(c.Find("#id-baslangic"));
        Assert.NotNull(c.Find("#id-yazdir"));
        Assert.Empty(c.FindAll("#id-tur"));
        Assert.Empty(c.FindAll("#degismez-ipucu-Rapor"));
        Assert.Empty(c.FindAll(".stat-izgara"));      // tek özet satırı
        Assert.Contains(Bicim.Tl(1500m), c.Find("#id-ozet").TextContent);
        Assert.Contains(string.Format(ZB.IslemStornoOzet, 1, Bicim.Tl(100m)), c.Find("#id-ozet-storno").TextContent);
        Assert.Empty(c.FindAll("#id-suzgec-sayi"));

        c.Find("#id-suzgec-ac").Click();
        SeciciYardim.Sec(c, "id-tur", "SATIS");
        c.WaitForAssertion(() => Assert.Contains(istekler, i => i.Contains("/raporlar/islem-defteri?") && i.Contains("tur=SATIS")));
        Assert.Equal("1", c.Find("#id-suzgec-sayi").TextContent.Trim());
        Assert.StartsWith(ZB.Suzgec + ":", c.Find("#id-suzgec-ac").TextContent.Trim());

        // Temizle: süzgeç boşalır, yeni istek süzgeçsiz gider.
        c.Find("#id-suzgec-temizle").Click();
        c.WaitForAssertion(() => Assert.Empty(c.FindAll("#id-suzgec-sayi")));
        Assert.DoesNotContain("tur=", istekler.Last(i => i.Contains("/raporlar/islem-defteri?")));
    }

    // ═══ FATURALAR ══════════════════════════════════════════════════════════

    [Fact]
    public void ZB05_Faturalar_acilista_yerel_select_yok_suzgec_kapali_ozel_matrah_altta_kapali()
    {
        var c = Render<Kuyumcu.Web.Pages.Faturalar>();
        c.WaitForElement("#fk-goster-0");

        Assert.Empty(c.FindAll("#sayfa-basi-faturalar select"));
        Assert.Empty(c.FindAll("#fk-durum-filtre"));
        Assert.Empty(c.FindAll("#degismez-ipucu-Fatura"));
        // PAKET YASAL-4 / Y4-C: sayfa sonundaki özel matrah katlanırı KALDIRILDI (bilgi
        // faturanın kendisinde + başlık altı rozet); kapalı kodda teknik kaynak basılmaz.
        Assert.Empty(c.FindAll("#fk-ozel-matrah-kaynak"));
        Assert.Empty(c.FindAll("#fk-ozel-matrah-beyani-ac"));

        // Durum süzgeci: ortak Form/Secim (radyo), seçim gerçek istek atar.
        c.Find("#fk-suzgec-ac").Click();
        Assert.Equal("radiogroup", c.Find("#fk-durum-filtre").GetAttribute("role"));
        c.Find("#fk-durum-filtre-secenek-1").Change("TASLAK");
        c.WaitForAssertion(() => Assert.Contains(istekler, i => i.Contains("/faturalar?") && i.Contains("durum=TASLAK")));
        Assert.Equal($"{ZB.Suzgec}: {Kuyumcu.Core.I18n.Sozlukler.Tr.Fatura.Taslak}", c.Find("#fk-suzgec-ac .katlanir-ad").TextContent.Trim());
    }

    [Fact]
    public void ZB06_Faturalar_Kaynak_sutunu_BASITte_gizli_PROda_ikonla_emoji_yok()
    {
        Mod(ArayuzModuServisi.Basit);
        var basit = Render<Kuyumcu.Web.Pages.Faturalar>();
        basit.WaitForElement("#fk-goster-0");
        Assert.Empty(basit.FindAll("#fk-kaynak-f2"));

        Mod(ArayuzModuServisi.Profesyonel);
        var pro = Render<Kuyumcu.Web.Pages.Faturalar>();
        pro.WaitForElement("#fk-kaynak-f2");
        Assert.NotNull(pro.Find("#fk-kaynak-f2 svg, #fk-kaynak-f2 .ikon"));
        Assert.DoesNotContain("🏛", pro.Markup);
        Assert.DoesNotContain("📄", pro.Markup);
    }

    [Fact]
    public void ZB07_Kesilmis_fatura_ACILINCA_degismez_ipucu_taslakta_yok()
    {
        var c = Render<Kuyumcu.Web.Pages.Faturalar>();
        c.WaitForElement("#fk-goster-1");
        c.Find("#fk-goster-1").Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#degismez-ipucu-Fatura")));
        Assert.Contains(Kuyumcu.Core.I18n.Sozlukler.Tr.Fatura.KesilmisDegismez,
            c.Find("#degismez-ipucu-Fatura").TextContent);
    }

    // ═══ EVRAKLAR ═══════════════════════════════════════════════════════════

    [Fact]
    public void ZB08_Evraklar_iki_hizli_suzgec_digerleri_katlanirda_tek_ozet_Faz_B_yok()
    {
        var c = Render<Kuyumcu.Web.Pages.Evraklar>().AyrintiAc();
        c.WaitForElement("#evrak-satir-e1");

        Assert.NotNull(c.Find("#evrak-suzgec-tumu"));
        Assert.NotNull(c.Find("#evrak-suzgec-portfoyde"));
        // KESIF-P3 (TOPLU ZO-BELGE "bölünmüş süzgeç"): dört durum TEK şeritte, dışarıda.
        Assert.NotNull(c.Find("#evrak-durum-seridi #evrak-suzgec-karsiliksiz"));
        Assert.Empty(c.FindAll("#evrak-tur-suzgec"));
        Assert.Empty(c.FindAll(".stat-izgara"));
        Assert.Contains(Bicim.Para(30000m, "TRY"), c.Find("#evrak-ozet-portfoy").TextContent);
        Assert.Contains("zo-ozet-kirmizi", c.Find("#evrak-ozet-karsiliksiz").ClassList);
        // "değiştirilemez" ve ciro notu açılışta YOK; "Faz B" hiçbir yerde yok.
        Assert.Empty(c.FindAll("#degismez-ipucu-KiymetliEvrakTutar"));
        Assert.Empty(c.FindAll("#evrak-ciro-devredildi"));
        Assert.DoesNotContain("Faz B", c.Markup);

        c.Find("#evrak-suzgec-karsiliksiz").Click();
        c.WaitForAssertion(() => Assert.Contains(istekler, i => i.Contains("/evraklar?") && i.Contains("durum=KARSILIKSIZ")));
        Assert.Equal("true", c.Find("#evrak-suzgec-karsiliksiz").GetAttribute("aria-pressed"));
        // Katlanır yalnız İÇİNDEKİ süzgeçleri sayar (durum dışarıda, görünür).
        Assert.Empty(c.FindAll("#evrak-suzgec-sayi"));

        // Değiştirme denenince (Düzenle çekmecesi) gerekçe + sade ciro notu gelir.
        c.Find("#evrak-duzenle-e1").Click();
        c.WaitForElement("#degismez-ipucu-KiymetliEvrakTutar");
        Assert.Equal(ZB.EvrakCiroNotu, c.Find("#evrak-ciro-devredildi").TextContent.Trim());
    }

    [Fact]
    public void ZB09_Evrak_formunda_ek_bilgiler_yenide_KAPALI_doluysa_ACIK()
    {
        var c = Render<Kuyumcu.Web.Pages.Evraklar>().AyrintiAc();
        c.WaitForElement("#evrak-yeni");
        c.Find("#evrak-yeni").Click();
        c.WaitForElement("#evrak-form-no");
        // Zorunlular açık; 6 isteğe bağlı alan kapalı katlanırda.
        Assert.Empty(c.FindAll("#evrak-form-banka"));
        c.Find("#evrak-form-ek-ac").Click();
        Assert.NotNull(c.Find("#evrak-form-banka"));
        Assert.NotNull(c.Find("#evrak-form-notlar"));

        // Düzenlemede banka adı DOLU evrak → katlanır açık başlar (dolu veri gizlenmez).
        c.Find("#cekmece-kapat").Click();
        c.Find("#evrak-duzenle-e1").Click();
        c.WaitForElement("#evrak-form-banka");
        Assert.Equal("true", c.Find("#evrak-form-ek-ac").GetAttribute("aria-expanded"));
    }

    // ═══ DENETİM İZİ ════════════════════════════════════════════════════════

    [Fact]
    public void ZB10_Denetim_izi_suzgec_kapali_aciklama_jargonsuz_yasal_dayanak_katlanirda()
    {
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        c.WaitForElement("#dz-tablo");

        Assert.Empty(c.FindAll("#f-eylem"));
        Assert.Empty(c.FindAll("#degismez-ipucu-DenetimKaydi"));
        Assert.DoesNotContain("KVKK", c.Find("#sayfa-basi-denetim-izi").TextContent);
        Assert.Equal(string.Format(ZB.DenetimOzet, "3", "1"), c.Find("#dz-ozet-sayilar").TextContent.Trim());

        c.Find("#dz-suzgec-ac").Click();
        Assert.NotNull(c.Find("#f-eylem"));
        Assert.NotNull(c.Find("#degismez-ipucu-DenetimKaydi"));
        Assert.Equal(ZB.DenetimYasalDayanak, c.Find("#dz-yasal-dayanak").TextContent.Trim());
        Assert.DoesNotContain("style=", c.Find("#dz-suzgec").OuterHtml);
    }

    // ═══ VERİ İÇE AKTARIM ═══════════════════════════════════════════════════

    [Fact]
    public void ZB11_Ice_aktarim_sablon_hatasi_Yukleniyorda_DONMAZ_yeniden_dene_istek_atar()
    {
        hatali.Add("/ice-aktarim/sablonlar");
        var c = Render<Kuyumcu.Web.Pages.VeriIceAktarim>();
        c.WaitForElement("#ia-sablon-hata");
        Assert.DoesNotContain(Kuyumcu.Core.I18n.Sozlukler.Tr.Ortak.Yukleniyor,
            c.Find("#ia-adim1-baslik").ParentElement!.TextContent);
        Assert.Equal(1, Say("/ice-aktarim/sablonlar"));

        hatali.Clear();
        c.Find("#ia-sablon-yeniden").Click();
        c.WaitForAssertion(() => Assert.Equal(2, Say("/ice-aktarim/sablonlar")));
        c.WaitForElement("#ia-sablon-indir");
        Assert.Empty(c.FindAll("#ia-sablon-hata"));
    }

    [Fact]
    public void ZB12_Ice_aktarim_gecmisi_KAPALI_istek_atilmaz_acilinca_bir_kez_yuklenir()
    {
        var c = Render<Kuyumcu.Web.Pages.VeriIceAktarim>();
        c.WaitForElement("#ia-sablon-indir");
        Assert.Equal(0, Say("/ice-aktarim/gecmis"));
        Assert.Empty(c.FindAll("#ia-gecmis-tablosu"));
        Assert.DoesNotContain("MASAK", c.Markup);
        // 29.09 devam 6: biçim/sınır bilgisi açılışta YOK, "Dosyayı nasıl hazırlarım?" katlanırında.
        // 29.09 devam 9 (hakem k3 · K17): kapsam notu da açılışta YOK — katlanırın ilk satırı.
        Assert.Empty(c.FindAll("#ia-dosya-ipucu"));
        Assert.Empty(c.FindAll("#ia-kapsam-notu"));
        c.Find("#ia-nasil-ac").Click();
        // Kapsam kuralı YAZILI kalır, jargonsuz.
        Assert.Equal(ZB.IceKapsamNotu, c.Find("#ia-nasil-govde #ia-kapsam-notu").TextContent.Trim());
        // Dosya sınırı binlik ayraçlı (BICIM-2 GRUPSUZ bulgusu).
        Assert.Contains(Bicim.SayiGirdi(5000m), c.Find("#ia-dosya-ipucu").TextContent);

        c.Find("#ia-gecmis-ac").Click();
        c.WaitForElement("#ia-gecmis-tablosu");
        Assert.Equal(1, Say("/ice-aktarim/gecmis"));
        c.Find("#ia-gecmis-ac").Click();
        c.Find("#ia-gecmis-ac").Click();
        Assert.Equal(1, Say("/ice-aktarim/gecmis"));   // aç/kapa istek atmaz
    }

    [Fact]
    public void ZB13_Ice_aktarim_gecmis_hatasi_katlanirin_ICINDE_yazar_yeniden_dener()
    {
        hatali.Add("/ice-aktarim/gecmis");
        var c = Render<Kuyumcu.Web.Pages.VeriIceAktarim>();
        c.WaitForElement("#ia-sablon-indir");
        c.Find("#ia-gecmis-ac").Click();
        c.WaitForElement("#ia-gecmis-hata");
        Assert.Empty(c.FindAll("#ia-gecmis-tablosu"));

        hatali.Clear();
        c.Find("#ia-gecmis-yeniden").Click();
        c.WaitForElement("#ia-gecmis-tablosu");
        Assert.Empty(c.FindAll("#ia-gecmis-hata"));
    }

    // ═══ KAYNAK KİLİTLERİ ═══════════════════════════════════════════════════

    // ═══ 27.09 DEVAM — açılış ≤ 1 ekran ═════════════════════════════════════

    [Fact]
    public void ZB16_Evrak_satirinda_TEK_birincil_eylem_karsiliksiz_pencerede_idler_ayni()
    {
        Mod(ArayuzModuServisi.Basit);
        var c = Render<Kuyumcu.Web.Pages.Evraklar>().AyrintiAc();
        c.WaitForElement("#evrak-eylem-e1");
        // Satırda görünen: Düzenle + "⋯" (dar sütun, ortak SatirEylemleri).
        var satirda = c.FindAll("#evrak-eylem-e1 > button");
        Assert.Equal(2, satirda.Count);
        Assert.Equal("evrak-duzenle-e1", satirda[0].Id);
        Assert.Equal("evrak-menu-ac-e1", satirda[1].Id);
        // Geçişler pencerede, id'ler AYNI; yıkıcı eylem kırmızı metinli ve pencerede.
        Assert.NotNull(c.Find("#evrak-menu-e1 #evrak-tahsile-ver-e1"));
        var ks = c.Find("#evrak-menu-e1 #evrak-karsiliksiz-e1");
        Assert.Contains("metin-kirmizi", ks.ClassName, StringComparison.Ordinal);
        // Pencereden tıklama onay modalını açar (DoD EK 4).
        ks.Click();
        c.WaitForElement("#evrak-karsiliksiz-uyari");
    }

    [Fact]
    public void ZB17_Islem_defteri_sayfa_araligi_OZET_satirinda()
    {
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-sinir");
        Assert.Equal("id-ozet", c.Find("#id-sinir").ParentElement!.Id);
        Assert.Equal("status", c.Find("#id-sinir").GetAttribute("role"));
    }

    [Fact]
    public void ZB18_Ice_aktarim_yinelenen_satir_kurali_KAPALI_baslik_secili_degeri_yazar()
    {
        var c = Render<Kuyumcu.Web.Pages.VeriIceAktarim>();
        c.WaitForElement("#ia-politika-katlanir");
        var t = Dil.T.IceAktarim;
        Assert.Equal($"{t.YinelenenPolitikasi}: {t.PolitikaHata}", c.Find("#ia-politika-katlanir-ac .katlanir-ad").TextContent.Trim());
        Assert.Empty(c.FindAll("#ia-politika"));
        c.Find("#ia-politika-katlanir-ac").Click();
        Assert.NotEmpty(c.FindAll("#ia-politika-katlanir-govde"));
    }

    // ═══ 28.09 DOĞRULAYICI BULGULARI ═════════════════════════════════════════

    [Fact]
    public void ZB22_Islem_defteri_has_DORT_hane_ozette_ve_sutunda_0_0004_kaybolmaz()
    {
        // KRİTİK (doğrulayıcı 27.09): F3 ile 0,0004 has "0,000" olup kayboluyordu.
        islemDefteri = """
            {"baslangic":"2026-09-18","bitis":"2026-09-24","toplamSatir":1,"ofset":0,"limit":10,"dahaFazlasiVar":false,
             "ozet":{"tlGiris":10,"tlCikis":0,"hasGiris":0.0004,"hasCikis":0,"satirSayisi":1},
             "storno":{"adet":0,"tutarTl":0},
             "satirlar":[{"id":"h1","tarih":"2026-09-20T10:00:00Z","tur":"SATIS","odemeTuru":"NAKIT","tlTutari":10,"hasGrami":0.0004}]}
            """;
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-ozet-has");
        // Sabit metin (P-57: has 4 hane) — Bicim değişirse test bunu yakalar.
        Assert.Equal($"{ZB.IslemOzetHas}: 0,0004 has", c.Find("#id-ozet-has").TextContent.Trim());
        Assert.Contains(c.FindAll("#id-tablo tbody td.sayi"), td => td.TextContent.Trim() == "0,0004 has");
        Assert.DoesNotContain("0,000 g", c.Find("#id-sayfa").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ZB23_Islem_defteri_ve_kasa_raporu_basligi_MENU_adiyla_AYNI()
    {
        // UX-S31 (doğrulayıcı 27.09): üst çubuk/menü "İşlem defteri" iken başlık "Hareket Dökümü" idi.
        var m = MenuAdlari.Tr;
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#sayfa-basi-islem-defteri-baslik");
        Assert.Equal(m.IslemDefteri.Ad, c.Find("#sayfa-basi-islem-defteri-baslik").TextContent.Trim());
        Assert.Equal("İşlem defteri", m.IslemDefteri.Ad);

        var k = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        k.WaitForElement("#sayfa-basi-kasa-raporu-baslik");
        Assert.Equal(m.KasaRaporu.Ad, k.Find("#sayfa-basi-kasa-raporu-baslik").TextContent.Trim());
        Assert.Equal("Kasa raporu", m.KasaRaporu.Ad);

        // Aynı çelişki 28.09 tarayıcı ölçümünde iki ekranda daha çıktı (Evraklar, İçe aktarım).
        var ev = Render<Kuyumcu.Web.Pages.Evraklar>().AyrintiAc();
        ev.WaitForElement("#sayfa-basi-evraklar-baslik");
        Assert.Equal(m.CekSenet.Ad, ev.Find("#sayfa-basi-evraklar-baslik").TextContent.Trim());
        var ia = Render<Kuyumcu.Web.Pages.VeriIceAktarim>();
        ia.WaitForElement("#sayfa-basi-veri-ice-aktarim-baslik");
        Assert.Equal(m.VeriIceAktar.Ad, ia.Find("#sayfa-basi-veri-ice-aktarim-baslik").TextContent.Trim());
    }

    [Fact]
    public void ZB24_Islem_defteri_degismez_notu_SUZGECTE_degil_TABLONUN_ALTINDA_kapali()
    {
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-tablo");
        // Süzgeç açılınca içinde "değiştirilemez" kutusu YOK.
        c.Find("#id-suzgec-ac").Click();
        Assert.Empty(c.FindAll("#id-suzgec-govde #degismez-ipucu-Rapor"));
        Assert.Empty(c.FindAll("#degismez-ipucu-Rapor"));
        // Tablodan SONRA gelen kapalı "işlem yanlışsa" katlanırında.
        Assert.Equal("false", c.Find("#id-duzeltme-ac").GetAttribute("aria-expanded"));
        Assert.Equal(ZB.IslemDuzeltmeBaslik, c.Find("#id-duzeltme-ac .katlanir-ad").TextContent.Trim());
        var html = c.Find("#id-sayfa").OuterHtml;
        Assert.True(html.IndexOf("id=\"id-tablo\"", StringComparison.Ordinal) < html.IndexOf("id=\"id-duzeltme\"", StringComparison.Ordinal),
            "değiştirilemez notu tablonun ALTINDA olmalı");
        c.Find("#id-duzeltme-ac").Click();
        Assert.NotNull(c.Find("#id-duzeltme-govde #degismez-ipucu-Rapor"));
    }

    [Fact]
    public void ZB25_Ozet_satiri_dar_ekranda_ayraci_satir_basina_dusurmez()
    {
        // 375 px (doğrulayıcı): "· Tahsile verilen" satır başında kalıyordu. bUnit CSS
        // uygulamaz → kaynak kilidi; görsel kanıt raporun 375 tablosunda.
        var css = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "wwwroot", "css", "zo-belge.css"));
        var i = css.IndexOf("@media (max-width: 640px) {\n  .zo-belge .zo-ozet { flex-direction: column;", StringComparison.Ordinal);
        Assert.True(i > 0, "dar ekran özet kuralı yok");
        Assert.Contains(".zo-belge .zo-ozet > .zo-ozet-ayrac { display: none; }", css[i..], StringComparison.Ordinal);
    }

    // 29.09 devam 9 (tasarım hakemi k3 KRİTİK · K17 "açıklama metni satırı 0"): yedi ekranın hiçbirinde
    // başlığın altında açıklama satırı YOK (iki modda); BASİT rapor yüzünde yalnız günün tarihi (veri).
    // Kasa ve evrak özetindeki parantezli "(bugün itibarıyla …)" alt metni satırda değil (i) ipucunda.
    [Theory]
    [InlineData("BASIT")]
    [InlineData("PROFESYONEL")]
    public void ZB64_Yedi_ekranda_baslik_altinda_ACIKLAMA_SATIRI_yok_S33_notu_ipucunda(string mod)
    {
        Mod(mod);
        var T = Sozlukler.Getir("tr");
        void Yok<TC>(IRenderedComponent<TC> c, string ad) where TC : Microsoft.AspNetCore.Components.IComponent
        {
            var aciklamalar = c.FindAll(".sayfa-basi-aciklama");
            if (ad == "raporlar-bugun")
            {
                // Tek satır: GÜNÜN TARİHİ — açıklama cümlesi yok.
                var a = Assert.Single(aciklamalar);
                Assert.DoesNotContain(RaporBugunSozlugu.Tr.Aciklama, a.TextContent);
                Assert.DoesNotContain("·", a.TextContent);
            }
            else Assert.Empty(aciklamalar);
        }
        var f = Render<Kuyumcu.Web.Pages.Faturalar>(); f.WaitForElement("#sayfa-basi-faturalar-baslik"); Yok(f, "faturalar");
        Assert.DoesNotContain(T.Fatura.SayfaAciklama, f.Markup);
        var e = Render<Kuyumcu.Web.Pages.Evraklar>().AyrintiAc(); e.WaitForElement("#evrak-ozet"); Yok(e, "evraklar");
        Assert.DoesNotContain(T.Evrak.SayfaAciklama, e.Markup);
        // UX-S33 notu kaybolmadı: özet satırında değil, (i) ipucunun içinde.
        Assert.DoesNotContain(T.Evrak.DonemdenBagimsiz, e.Find("#evrak-ozet").TextContent.Replace(e.Find("#evrak-ozet-ipucu").TextContent, ""));
        Assert.Equal(T.Evrak.DonemdenBagimsiz.Trim('(', ')', ' '), e.Find("#evrak-ozet-ipucu p").TextContent.Trim());
        var r = Render<Kuyumcu.Web.Pages.Raporlar>(); r.WaitForElement(mod == "BASIT" ? "#raporlar-bugun" : "#sayfa-basi-raporlar-baslik");
        Yok(r, mod == "BASIT" ? "raporlar-bugun" : "raporlar");
        var i = Render<Kuyumcu.Web.Pages.IslemDefteri>(); i.WaitForElement("#sayfa-basi-islem-defteri-baslik"); Yok(i, "islem-defteri");
        Assert.DoesNotContain(ZB.IslemAciklama, i.Markup);
        var k = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc(); k.WaitForElement("#kr-ozet"); Yok(k, "kasa-raporu");
        Assert.DoesNotContain(ZB.KasaAciklama, k.Markup);
        foreach (var ad in k.FindAll(".zo-kasa-ad"))
        {
            var ipucu = ad.QuerySelector("details.zo-ipucu")!;
            Assert.Equal(T.RaporSeti.DonemBagimsiz.Trim('(', ')', ' '), ipucu.QuerySelector("p")!.TextContent.Trim());
            Assert.DoesNotContain("(", ad.TextContent.Replace(ipucu.TextContent, ""));
        }
        var d = Render<Kuyumcu.Web.Pages.DenetimIzi>(); d.WaitForElement("#sayfa-basi-denetim-izi-baslik"); Yok(d, "denetim-izi");
        Assert.DoesNotContain(ZB.DenetimAciklama, d.Markup);
        var v = Render<Kuyumcu.Web.Pages.VeriIceAktarim>(); v.WaitForElement("#ia-prova"); Yok(v, "veri-ice-aktarim");
        Assert.DoesNotContain(ZB.IceAciklama, v.Markup);
        Assert.Empty(v.FindAll("#ia-kapsam-notu"));
    }

    private DilServisi Dil => Services.GetRequiredService<DilServisi>();

    private static readonly string[] Sayfalar =
        ["Faturalar", "Evraklar", "Raporlar", "IslemDefteri", "KasaRaporu", "DenetimIzi", "VeriIceAktarim"];

    private static string Sayfa(string ad) =>
        File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "Pages", ad + ".razor"));

    [Fact]
    public void ZB14_Yedi_ekranda_ham_number_ve_satir_ici_stil_YOK()
    {
        foreach (var ad in Sayfalar)
        {
            var k = Sayfa(ad);
            Assert.False(k.Contains("type=\"number\"", StringComparison.Ordinal), $"{ad}: ham type=number kaldı");
            Assert.False(k.Contains(" style=\"", StringComparison.Ordinal), $"{ad}: satır içi style kaldı");
            // 29.09 tasarım hakemi t3 (K22): yerel <select> öğesi YOK (yorumlardaki "<select>" sayılmaz).
            Assert.False(System.Text.RegularExpressions.Regex.IsMatch(k, @"<select\s"), $"{ad}: yerel <select> kaldı");
        }
    }

    [Fact]
    public void ZB31_Ice_aktarim_birincil_eylem_ve_kilit_sebebi_AYNI_satirda()
    {
        // 29.09: PRO 1440 açılışı 1,03 ekrandı; kilit sebebi düğmenin altından yanına alındı.
        Mod(ArayuzModuServisi.Profesyonel);
        var c = Render<Kuyumcu.Web.Pages.VeriIceAktarim>();
        c.WaitForElement("#ia-prova");
        var sebep = c.Find("#ia-kilit-sebep");                 // gerekçe boş → düğme kilitli, sebep yazar
        Assert.True(c.Find("#ia-prova").HasAttribute("disabled"));
        var ata = c.Find("#ia-prova").ParentElement!;
        Assert.Contains("zo-ice-eylem", ata.ClassList);
        Assert.Same(ata, sebep.ParentElement);
        var css = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "wwwroot", "css", "zo-belge.css"));
        Assert.Contains(".zo-belge .zo-ice-eylem { display: flex; flex-wrap: wrap;", css, StringComparison.Ordinal);
        // Şablon satırı: .alan ızgarasını esnek satıra çevirir (düğme tam genişlik olmaz, kolon listesi yanında).
        Assert.Contains(".zo-belge .alan.zo-ice-sablon { display: flex; flex-direction: row;", css, StringComparison.Ordinal);
        // 29.09 devam 6: kolon listesi (PRO) "Dosyayı nasıl hazırlarım?" katlanırında — açılışta teknik satır yok.
        Assert.Empty(c.FindAll("#ia-kolonlar"));
        c.Find("#ia-nasil-ac").Click();
        Assert.NotNull(c.Find("#ia-nasil-govde #ia-kolonlar"));
        // 375 px: dosya seçici kartın dışına 24 px taşıyordu (d29 ölçümü) → kap genişliğine sınırlı.
        Assert.Contains(".zo-belge #ia-dosya { display: flex; align-items: center; width: 100%; max-width: 100%; min-width: 0; }", css, StringComparison.Ordinal);
    }

    [Fact]
    public void ZB15_zo_belge_css_belirtecle_yazilir_ve_ortak_cssten_ONCE_yuklenir()
    {
        var kok = CssKaynak.Kok();
        var css = File.ReadAllText(Path.Combine(kok, "Kuyumcu.Web", "wwwroot", "css", "zo-belge.css"));
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b", css);
        Assert.DoesNotMatch(@"\brgba?\(", css);
        var index = File.ReadAllText(Path.Combine(kok, "Kuyumcu.Web", "wwwroot", "index.html"));
        var i = index.IndexOf("css/zo-belge.css", StringComparison.Ordinal);
        Assert.True(i > 0, "index.html zo-belge.css'i yüklemiyor");
        Assert.True(i < index.IndexOf("css/ortak.css", StringComparison.Ordinal), "ortak.css EN SONDA kalmalı (O1)");
    }

    // ═══ 29.09 TASARIM HAKEMİ t3 ════════════════════════════════════════════

    private const string DenetimHam = """
        {"toplam":2,"limit":7,"offset":0,"kayitlar":[
          {"id":"d1","tarih":"2026-09-13T10:00:00Z","eylem":"GUNCELLEME","varlikTuru":"Cari","varlikId":"c1","hassas":false,
           "detay":"Ad, Guncellendi, Telefon, IslemMs","ipAdresi":null,"kullaniciId":"u1","kullanici":"Ayşe Yılmaz","rol":"SAHIP",
           "subeId":null,"subeAd":null,"kasaId":null,"kasaAd":null},
          {"id":"d2","tarih":"2026-09-13T09:00:00Z","eylem":"EKLEME","varlikTuru":"KiymetliEvrak","varlikId":"e1","hassas":false,
           "detay":null,"ipAdresi":null,"kullaniciId":"u1","kullanici":"Ayşe Yılmaz","rol":"SAHIP",
           "subeId":null,"subeAd":null,"kasaId":null,"kasaAd":null}]}
        """;

    [Fact]
    public void ZB35_Denetim_BASIT_sistem_kayitlarini_sunucuda_suzer_alan_ve_tur_adlari_Turkce_aralik_TEK_yerde()
    {
        Mod("BASIT");
        denetimListe = DenetimHam;
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        c.WaitForAssertion(() => Assert.Equal(2, c.FindAll("#dz-tablo tbody tr").Count));

        // Liste VE özet aynı kümeyi sayar (P-24): ikisi de sistemHaric=true taşır.
        Assert.Contains(istekler, i => i.Contains("/denetim?") && i.Contains("sistemHaric=true"));
        Assert.Contains(istekler, i => i.Contains("/denetim/ozet?") && i.Contains("sistemHaric=true"));

        var tablo = c.Find("#dz-tablo").TextContent;
        Assert.Contains("Değişen: ad, telefon (+1 bilgi daha)", tablo);
        Assert.Contains("Çek / senet", tablo);
        Assert.DoesNotContain("Guncellendi", tablo);
        Assert.DoesNotContain("IslemMs", tablo);
        Assert.DoesNotContain("KiymetliEvrak", tablo);
        // "1–2 / 2 kayıt" yalnız sayfalama satırında.
        Assert.Empty(c.FindAll("#dz-sayfa-ozet"));
        Assert.Single(c.FindAll("*"), e => e.ChildElementCount == 0 && e.TextContent.Trim() == c.Find("#dz-sayfa-bilgi").TextContent.Trim());
    }

    [Fact]
    public void ZB36_Denetim_PRO_ham_detay_ve_sistem_kayitlari_ACIK()
    {
        Mod("PROFESYONEL");
        denetimListe = DenetimHam;
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        c.WaitForAssertion(() => Assert.Equal(2, c.FindAll("#dz-tablo tbody tr").Count));
        Assert.DoesNotContain(istekler, i => i.Contains("sistemHaric"));
        // KESIF-P3 (ekip kararı 2, 30.09): PRO görünür metinde ham alan adı YOK — okunur metin;
        // ham kayıt denetçi için hücrenin title ipucunda AYNEN.
        Assert.DoesNotContain("IslemMs", c.Find("#dz-tablo").TextContent);
        // PAKET GOC-D: dz-tablo ortak ızgarada; ham kayıt ipucu (title) hücrenin İÇ öğesinde (ızgara td özniteliği satıra göre değişmez) — sözleşme aynı.
        Assert.Contains(c.FindAll("#dz-tablo td [title]"), td => td.GetAttribute("title") == "Ad, Guncellendi, Telefon, IslemMs");
    }

    [Fact]
    public void ZB37_Islem_defteri_BASIT_bes_sutun_belgeye_tur_adindan_gidilir()
    {
        Mod("BASIT");
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-tablo");
        var basit = c.FindAll("#id-tablo thead th").Select(h => h.TextContent.Trim()).ToList();
        var B = Kuyumcu.Core.I18n.BasitSozluk.Getir("tr");
        Assert.Equal(5, basit.Count);
        Assert.Equal(B.RaporSeti.CariAd, basit[2]);
        Assert.Equal(5, c.FindAll("#id-tablo tbody tr:first-child td").Count);
        // BASİT'te belgeye tür adından gidilir (belge sütunu yok, yol kaybolmadı).
        Assert.Equal("/cikti/SATIS_FISI/h1", c.Find("#id-belge-h1").GetAttribute("href"));
        Assert.Equal("id-belge-h1", c.Find("#id-tur-h1").ParentElement!.Id);
    }

    [Fact]
    public void ZB41_Islem_defteri_PRO_dokuz_sutun_ikincil_bilgi_alt_satirda_veri_kaybolmaz()
    {
        // 29.09 (tasarım hakemi, PRO 1440 kesik sütun): 14 sütun → 9; nakit/veresiye payı
        // tutarın, ürün müşterinin, şube kaydedenin, fatura no türün alt satırında. Veri silinmez.
        Mod("PROFESYONEL");
        islemDefteri = """
            {"baslangic":"2026-09-18","bitis":"2026-09-24","toplamSatir":2,"ofset":0,"limit":25,"dahaFazlasiVar":false,
             "ozet":{"tlGiris":1500,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":2},
             "satirlar":[{"id":"h1","tarih":"2026-09-20T10:00:00Z","tur":"SATIS","odemeTuru":"NAKIT","tlTutari":1500,"hasGrami":0,
                          "nakitTutar":1000,"veresiyeTutar":500,"cariAd":"Anadolu Kuyumculuk Holding Anonim Şirketi",
                          "urunAd":"22 Ayar Bilezik","subeAd":"Merkez Şube","kullaniciAd":"Demo Kuyumcu","faturaNo":"SAT-2026-000001"},
                         {"id":"h2","tarih":"2026-09-20T11:00:00Z","tur":"TAHSILAT","odemeTuru":"NAKIT","tlTutari":-200,"hasGrami":0}]}
            """;
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-tablo");
        var T = Sozlukler.Getir("tr");
        var basliklar = c.FindAll("#id-tablo thead th").Select(t => t.TextContent.Trim()).ToList();
        // 29.09 devam 9 (hakem k3): müşteri + kaydeden TEK sütun → 8.
        Assert.Equal(8, basliklar.Count);
        Assert.Equal(8, c.FindAll("#id-tablo tbody tr:first-child td").Count);
        foreach (var yok in new[] { T.RaporSeti.NakitPayi, T.RaporSeti.VeresiyePayi, T.RaporSeti.UrunAd, T.RaporSeti.SubeAd, T.RaporSeti.FaturaNo })
            Assert.DoesNotContain(yok, basliklar);
        Assert.Contains("zo-yigin-dar", c.Find("#id-tablo").ClassList);   // dar kapta kart, genişte belge sütunu yapışık
        // Veri kaybolmadı: her ikincil bilgi kendi hücresinin alt satırında.
        Assert.Equal($"{T.RaporSeti.NakitPayi} {Bicim.Tl(1000m)} {T.RaporSeti.VeresiyePayi} {Bicim.Tl(500m)}",
            System.Text.RegularExpressions.Regex.Replace(c.Find("#id-pay-h1").TextContent, "[ \t\r\n]+", " ").Trim());
        Assert.Equal(T.RaporSeti.TlTutar, c.Find("#id-pay-h1").ParentElement!.GetAttribute("data-etiket"));
        Assert.Contains("22 Ayar Bilezik", c.Find("#id-urun-h1").TextContent);
        Assert.Equal(ZB.IslemCariKaydeden, c.Find("#id-urun-h1").ParentElement!.GetAttribute("data-etiket"));
        Assert.Equal("Merkez Şube", c.Find("#id-sube-h1").TextContent.Trim());
        Assert.Contains("SAT-2026-000001", c.Find("#id-fatura-h1").TextContent);
        Assert.Equal("TD", c.Find("#id-tur-h1").ParentElement!.TagName);
        Assert.Equal("id-belge-h1", c.Find("#id-belge-h1").Id);
        // Pay bilinmiyorsa 0 yazılmaz (Anayasa md.1); boş ürün/şube/fatura için alt satır açılmaz.
        Assert.Contains(T.RaporSeti.Bilinmiyor, c.Find("#id-pay-h2").TextContent);
        Assert.Empty(c.FindAll("#id-urun-h2, #id-sube-h2, #id-fatura-h2"));
        // Gerçek tarayıcıda 1440'ta 1122/1122 ölçüldü; bunu sağlayan kurallar kilitli:
        // karışık yönün bacakları alt alta.
        var css = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "wwwroot", "css", "zo-belge.css"));
        Assert.Contains(".zo-belge #id-tablo .zo-yon-bacak { display: block; margin-left: 0; white-space: normal; }", css);
        // Kaydeden başlığı kısa ad (tek satır, K3 kırık 0); hücre etiketi başlıkla aynı.
        // 29.09 devam 9 (hakem k3): KAYDEDEN ayrı sütun DEĞİL — müşteri hücresinin alt satırı
        // ("Kaydeden: ad · şube"); başlık birleşik ad, hücre etiketi başlıkla aynı; veri silinmedi.
        Assert.Equal(ZB.IslemCariKaydeden, c.Find("#id-th-kaydeden").TextContent.Trim());
        Assert.DoesNotContain(ZB.IslemKaydeden, basliklar);
        var cariHucre = c.Find("#id-kaydeden-h1").ParentElement!;
        Assert.Equal(ZB.IslemCariKaydeden, cariHucre.GetAttribute("data-etiket"));
        Assert.Same(cariHucre, c.Find("#id-urun-h1").ParentElement);
        Assert.Equal($"{ZB.IslemKaydeden}: Demo Kuyumcu Merkez Şube",
            System.Text.RegularExpressions.Regex.Replace(c.Find("#id-kaydeden-h1").TextContent, "[ \t\r\n]+", " ").Trim());
        Assert.Contains("—", c.Find("#id-kaydeden-h2").TextContent);
        Assert.Contains("zo-sube-satir", c.Find("#id-sube-h1").ClassList);
        Assert.Contains(".zo-belge #id-tablo .zo-hucre-alt > .zo-sube-satir { display: block; }", css);
        // Yapışkan belge sütunu kayan içeriğin üstünde durursa sol kenarında GÖRÜNÜR gölge (hakem k3).
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(css,
            @"-1px 0 0 0 var\(--ayrac\),\s*\n\s*-8px 0 10px -6px color-mix\(in srgb, var\(--metin\) 28%, transparent\);").Count);
    }

    [Fact]
    public void ZB38_Faturalar_BASIT_ara_toplam_ve_KDV_sutunu_YOK_toplam_VAR_PRO_hepsi()
    {
        Mod("BASIT");
        var c = Render<Kuyumcu.Web.Pages.Faturalar>();
        c.WaitForElement("#fk-goster-0");
        var T = Kuyumcu.Core.I18n.Sozlukler.Getir("tr");
        var etiketler = c.FindAll("#fk-tablo tbody tr:first-child td[data-etiket]").Select(t => t.GetAttribute("data-etiket")).ToList();
        Assert.DoesNotContain(T.Fatura.AraToplam, etiketler);
        Assert.DoesNotContain(T.Fatura.KdvTutar, etiketler);
        Assert.Contains(T.Ortak.Toplam, etiketler);
        Assert.Contains("zo-yigin-dar", c.Find("#fk-tablo").ClassName);
    }

    [Fact]
    public void ZB39_Evraklar_BASIT_banka_sutunu_YOK_cari_tek_etiket_zorunlu_tam_satir()
    {
        Mod("BASIT");
        var c = Render<Kuyumcu.Web.Pages.Evraklar>().AyrintiAc();
        c.WaitForElement("#evrak-tablo");
        var T = Kuyumcu.Core.I18n.Sozlukler.Getir("tr");
        Assert.DoesNotContain(c.FindAll("#evrak-tablo td[data-etiket]"), t => t.GetAttribute("data-etiket") == T.Evrak.BankaAdi);
        Assert.Contains("zo-yigin-dar", c.Find("#evrak-tablo").ClassName);

        c.Find("#evrak-yeni").Click();
        var sar = c.WaitForElement("#evrak-form-cari-sar");
        var kap = sar.ParentElement!;
        Assert.Contains("zo-tam-satir", kap.ClassName);
        Assert.Single(kap.QuerySelectorAll("label"));
        Assert.NotNull(sar.QuerySelector(".zorunlu-isaret"));
        Assert.Null(sar.QuerySelector(".alan-istege"));
    }

    [Fact]
    public void ZB40_Kasa_sayim_fark_gerekcesi_farka_gore_zorunlu_isaretli_celiskili_etiket_YOK()
    {
        var c = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        c.WaitForElement("#kr-sayim-ks1");
        c.Find("#kr-sayim-ks1").Click();
        var alan = c.WaitForElement("#kr-sayim-aciklama");
        string Etiket() => c.Find("label[for='kr-sayim-aciklama']").TextContent;
        // Tutar yokken fark yok → isteğe bağlı, çelişkili "(fark varsa zorunlu)" YOK.
        Assert.DoesNotContain("fark varsa", Etiket());
        Assert.Equal(ZB.KasaFarkGerekceIpucu, c.Find("#kr-sayim-aciklama-ipucu").TextContent.Trim());

        c.Find("#kr-sayim-tutar").Input("1500");
        c.WaitForAssertion(() => Assert.NotNull(c.Find("label[for='kr-sayim-aciklama'] .zorunlu-isaret")));
        Assert.Empty(c.FindAll("#kr-sayim-aciklama-sar .alan-istege"));

        Assert.Null(c.Find("#kr-sayim-aciklama-sar").QuerySelector("#kr-sayim-aciklama-ipucu"));
        // Tutar defterle eşitlenince yine isteğe bağlı (işaret kalkar).
        c.Find("#kr-sayim-tutar").Input("1200");
        c.WaitForAssertion(() => Assert.Empty(c.FindAll("label[for='kr-sayim-aciklama'] .zorunlu-isaret")));
        Assert.Single(c.FindAll("#kr-sayim-aciklama-sar .alan-istege"));
    }

    [Fact]
    public void ZB42_Yeni_fatura_ve_fisten_kes_yerel_select_YOK_zorunlu_alanlar_yildizli()
    {
        var c = Render<Kuyumcu.Web.Pages.Faturalar>();
        c.WaitForElement("#fk-goster-0");
        c.Find("#fk-yeni").Click();
        c.WaitForElement("#fatura-cari");
        Assert.Empty(c.FindAll("select"));
        // Toplam her zaman doğrulanır → "*", "(isteğe bağlı)" YOK (K18).
        Assert.NotNull(c.Find("label[for='fatura-toplam'] .zorunlu-isaret"));
        Assert.Empty(c.FindAll("#fatura-toplam-sar .alan-istege"));
        foreach (var id in new[] { "fatura-cari", "fatura-tur", "fatura-kdv", "fatura-kdvturu" })
            Assert.NotNull(c.Find($"#{id}-sar .zorunlu-isaret"));
        Assert.Equal("button", c.Find("#fatura-cari").TagName.ToLowerInvariant());
        Assert.Equal("radiogroup", c.Find("#fatura-kdv").GetAttribute("role"));

        // Fişten kes penceresi: KayitSecici bu sahte API'de çizilemiyor (arama ucu "{}"),
        // o yüzden kaynak kilidi ZB14'te (yedi ekranda yerel <select YOK).
    }

    // ═══ 29.09 DOĞRULAYICI (KALDI) BULGULARI ═══════════════════════════════

    [Fact]
    public void ZB44_Fatura_evrak_tablosu_KAP_genisligine_gore_karta_doner_tabloda_eylem_sutunu_saga_yapisik()
    {
        var css = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "wwwroot", "css", "zo-belge.css"));
        // Eşik pencere değil KAP: PRO 1280'de kap 962 px → kart (K9 binen 6 idi).
        Assert.Contains(".zo-belge .tablo-sar[data-zo=\"golge\"]:has(> .zo-yigin-dar) { container: zoyigin / inline-size; }", css);
        var kap = System.Text.RegularExpressions.Regex.Match(css, @"@container zoyigin \(max-width: (\d+)px\) \{(.*?)\n\}",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.True(kap.Success, "zoyigin kap sorgusu yok");
        Assert.True(int.Parse(kap.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) >= 1024,
            "eşik 1280 PRO kabını (962 px) ve tablo genişliğini (1023 px) kapsamalı");
        Assert.Contains("> thead { display: none; }", kap.Groups[2].Value);
        // Kartta eylem hücresi yapışık DEĞİL (komşuyu örtmez).
        Assert.Contains("td.q10-yigin-eylem {\n    grid-column: 1 / -1; position: static;", kap.Groups[2].Value);
        // 29.09 devam 6 (tasarım hakemi YÜKSEK): tablo olarak durduğunda (kap > 1060) eylem sütunu SAĞA YAPIŞIK,
        // opak zeminli — PRO 1440'ta "⋯" ve "Tahsil edildi" kesilmesin.
        var disari = css.Replace(kap.Value, "", StringComparison.Ordinal);
        Assert.Matches(@"\.zo-yigin-dar > tbody > tr > td\.q10-yigin-eylem \{\s*\n\s*position: sticky; right: 0; z-index: 1;\s*\n\s*box-shadow: inset 0 0 0 100vmax var\(--yuzey\)", disari);
        Assert.Matches(@"\.zo-yigin-dar\.q10-yigin > thead > tr > th:last-child \{\s*\n\s*position: sticky; right: 0;", disari);
        Assert.DoesNotContain("position: static; box-shadow: none;\n}", disari);
    }

    private const string DenetimKasali = """
        {"toplam":1,"limit":7,"offset":0,"kayitlar":[
          {"id":"d1","tarih":"2026-09-13T10:00:00Z","eylem":"OKUMA","varlikTuru":"Urun","varlikId":null,"hassas":false,
           "detay":"liste: 3 kayıt · stok-yaslandirma","ipAdresi":null,"kullaniciId":"u1","kullanici":"Ayşe Yılmazoğlu","rol":"SAHIP",
           "subeId":null,"subeAd":null,"kasaId":"ks1","kasaAd":"Merkez Kasa"}]}
        """;

    [Fact]
    public void ZB45_Denetim_BASIT_kasa_sutunu_YOK_detay_Turkce_ad_ile_rol_arasinda_bosluk_PRO_aynen()
    {
        Mod("BASIT");
        denetimListe = DenetimKasali;
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        c.WaitForAssertion(() => Assert.Single(c.FindAll("#dz-tablo tbody tr")));
        Assert.Empty(c.FindAll("#dz-th-kasa"));
        Assert.Equal(c.FindAll("#dz-tablo thead th").Count, c.FindAll("#dz-tablo tbody tr:first-child td").Count);
        var tablo = c.Find("#dz-tablo").TextContent;
        Assert.DoesNotContain("Merkez Kasa", tablo);
        Assert.Contains("stok yaşlandırma", tablo);
        Assert.DoesNotContain("yaslandirma", tablo);
        // Ad ile rol rozeti arasında boşluk (belirteçli pay) — rozet sarmalayıcısı sınıfı taşır.
        Assert.NotNull(c.Find("#dz-tablo tbody tr td:nth-child(2) span.zo-rol-ara"));
    }

    [Fact]
    public void ZB45b_Denetim_PRO_kasa_sutunu_ve_ham_detay_AYNEN()
    {
        Mod("PROFESYONEL");
        denetimListe = DenetimKasali;
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        c.WaitForAssertion(() => Assert.Single(c.FindAll("#dz-tablo tbody tr")));
        Assert.NotNull(c.Find("#dz-th-kasa"));
        Assert.Contains("Merkez Kasa", c.Find("#dz-tablo").TextContent);
        // KESIF-P3 (ekip kararı 2, 30.09): PRO görünür metinde de ham kod 0 — kod adı okunur
        // ada çevrilir, ham kayıt hücrenin title ipucunda AYNEN durur (denetçi).
        Assert.DoesNotContain("stok-yaslandirma", c.Find("#dz-tablo").TextContent);
        // PAKET GOC-D: dz-tablo ortak ızgarada; ham kayıt ipucu (title) hücrenin İÇ öğesinde (ızgara td özniteliği satıra göre değişmez) — sözleşme aynı.
        Assert.Contains(c.FindAll("#dz-tablo td [title]"), td => td.GetAttribute("title")!.Contains("stok-yaslandirma"));
    }

    [Fact]
    public void ZB46_Kasa_ozeti_ve_Sayim_gir_SUZGECTEN_bagimsiz_bitis_gecmise_alininca_kaybolmaz()
    {
        // Geçmiş aralıkta yalnız dünkü gün ve yalnız ks1 döner (bugün yok).
        kasaDefteriGecmis = $$"""
            {"baslangic":"{{Dun}}","bitis":"{{Dun}}","saatDilimi":"Europe/Istanbul","utcOfsetSaat":3,
             "yazSaatiRiski":false,"toplamSatir":1,"kasalar":[
              {"kasaId":"ks1","kasaAd":"Merkez Kasa","paraBirimi":"TRY","acilisDeviri":0,
               "kapanisBakiye":500.00,"guncelBakiye":1200.00,"gunler":[
                 {"tarih":"{{Dun}}","devir":0,"giris":500.00,"cikis":0,"hareketSayisi":1,"kapanis":500.00,
                  "durum":"KAPALI","sayilanNakit":500.00,"kapanisDefterBakiye":500.00,"fark":0}]}]}
            """;
        var c = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        c.WaitForElement("#kr-sayim-ks1");
        var ilkIstek = Say("/raporlar/kasa-defteri");
        Assert.Equal(1, ilkIstek);   // varsayılan süzgeçte özet ve tablo TEK yanıttan

        c.Find("#kr-gecmis-ac").Click();
        c.Find("#kr-bitis").Change(DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        c.WaitForAssertion(() => Assert.Single(c.FindAll("#kr-tablo-ks1 tbody tr")));

        // Tablo daraldı (yalnız dün, ks2 yok) ama ÖZET bugünkü veriyle kaldı.
        Assert.Empty(c.FindAll("#kr-tablo-ks2"));
        Assert.NotNull(c.Find("#kr-sayim-ks1"));
        Assert.Equal(ZB.KasaBugunSayilmadi, c.Find("#kr-bugun-ks1").TextContent.Trim());
        Assert.NotNull(c.Find("#kr-ozet-ks2"));
        Assert.Contains(ZB.KasaBugunSayildi, c.Find("#kr-bugun-ks2").TextContent);
    }
    // ═══ 29.09 DEVAM 6 — tasarım hakemi bulguları ═══════════════════════════════

    [Fact]
    public void ZB50_Islem_defteri_ve_kim_ne_yapti_375_KART_listesi_her_hucre_etiketli()
    {
        Mod("BASIT");
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-tablo");
        Assert.Contains("q10-yigin", c.Find("#id-tablo").ClassList);
        // Her hücrenin kart etiketi, sütun başlığının AYNISI (Tarih · Tür · Müşteri · Tutar · Ödeme).
        var etiket = c.FindAll("#id-tablo tbody tr:first-child td").Select(t => t.GetAttribute("data-etiket") ?? "").ToList();
        var baslik = c.FindAll("#id-tablo thead th").Select(t => t.TextContent.Trim()).ToList();
        Assert.Equal(5, etiket.Count);
        Assert.Equal(baslik, etiket);
        Assert.Contains("zo-yigin-var", c.Find("#id-sayfa .zo-yana-kaydir").ClassList);   // kartta "yana kaydırın" yazmaz

        var d = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        d.WaitForElement("#dz-tablo");
        Assert.Contains("q10-yigin", d.Find("#dz-tablo").ClassList);
        Assert.All(d.FindAll("#dz-tablo tbody tr:first-child td").Take(5), t => Assert.False(string.IsNullOrEmpty(t.GetAttribute("data-etiket"))));
    }

    [Fact]
    public void ZB51_Kasa_gecmis_tablosunda_tarih_YEREL_ISO_basilmaz()
    {
        var c = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        c.WaitForElement("#kr-gecmis-ac");
        c.Find("#kr-gecmis-ac").Click();
        c.WaitForElement("#kr-tablo-ks1");
        var beklenen = Bicim.Tarih(DateTime.SpecifyKind(DateTime.Today.AddDays(-1), DateTimeKind.Local), "tr-TR");
        var hucre = c.Find($"#kr-tarih-ks1-{Dun}");
        Assert.Equal(beklenen, hucre.TextContent.Trim());
        Assert.Matches(@"^\d{2}\.\d{2}\.\d{4}$", hucre.TextContent.Trim());
        Assert.DoesNotContain(Dun, c.Find("#kr-tablo-ks1").TextContent);
    }

    [Fact]
    public void ZB52_Faturalar_PRO_8_sutun_kaynak_numaranin_altinda_cari_iki_satir()
    {
        Mod(ArayuzModuServisi.Profesyonel);
        var c = Render<Kuyumcu.Web.Pages.Faturalar>();
        c.WaitForElement("#fk-kaynak-f2");
        var T = Sozlukler.Getir("tr");
        var basliklar = c.FindAll("#fk-tablo thead th").Select(t => t.TextContent.Trim()).ToList();
        Assert.Equal(8, basliklar.Count);
        Assert.DoesNotContain(T.Fatura.AraToplam, basliklar);
        Assert.DoesNotContain(T.Fatura.Kaynak, basliklar);
        Assert.Contains(T.Fatura.KdvTutar, basliklar);
        Assert.Equal(c.FindAll("#fk-tablo thead th").Count, c.FindAll("#fk-tablo tbody tr:first-child td").Count);
        // Kaynak bilgisi silinmedi: numaranın hücresinde, ince çizgi ikonla.
        Assert.Equal(T.Fatura.FaturaNo, c.Find("#fk-kaynak-f2").ParentElement!.GetAttribute("data-etiket"));
        Assert.NotNull(c.Find("#fk-kaynak-f2 svg, #fk-kaynak-f2 .ikon"));
        // Ara toplam ipucunda (Toplam hücresi).
        // PAKET GOC-A: ortak ızgarada ipucu Toplam hücresinin İÇİNDEKİ öğede (hücre özniteliği satıra göre verilemez).
        Assert.Contains(T.Fatura.AraToplam, c.FindAll($"#fk-tablo td[data-etiket='{T.Ortak.Toplam}'] > span[title]")[0].GetAttribute("title"));
        // Uzun cari adı kesilmez, iki satırla sınırlı.
        Assert.NotNull(c.Find("#fk-tablo td.zo-metin-sutun > span.zo-iki-satir"));
    }

    [Fact]
    public void ZB53_Evraklar_PRO_banka_ayri_sutun_DEGIL_numaranin_altinda()
    {
        Mod(ArayuzModuServisi.Profesyonel);
        var c = Render<Kuyumcu.Web.Pages.Evraklar>().AyrintiAc();
        c.WaitForElement("#evrak-tablo");
        var T = Sozlukler.Getir("tr");
        Assert.DoesNotContain(c.FindAll("#evrak-tablo thead th"), t => t.TextContent.Trim() == T.Evrak.BankaAdi);
        Assert.Equal("Ziraat", c.Find("#evrak-banka-e1").TextContent.Trim());
        Assert.Equal(T.Evrak.EvrakNo, c.Find("#evrak-banka-e1").ParentElement!.GetAttribute("data-etiket"));
        Assert.Equal(c.FindAll("#evrak-tablo thead th").Count, c.FindAll("#evrak-tablo tbody tr:first-child td").Count);
    }

    [Fact]
    public void ZB54_Yeni_fatura_CEKMECE_kalem_yokken_bos_kutu_yok_bos_kaydette_ilk_hataya_odak()
    {
        Mod("BASIT");
        var c = Render<Kuyumcu.Web.Pages.Faturalar>();
        c.WaitForElement("#fk-yeni");
        c.Find("#fk-yeni").Click();
        var cek = c.WaitForElement("#cekmece-fatura-yeni");
        Assert.NotNull(cek.QuerySelector("#fk-kaydet"));
        Assert.NotNull(cek.QuerySelector("#fatura-cari"));
        // Kalem yokken: yalnız "+ Kalem ekle" + tek satır ipucu; boş durum kutusu YOK.
        Assert.Equal(ZB.FaturaKalemYokIpucu, c.Find("#fk-kalem-yok").TextContent.Trim());
        Assert.Empty(c.FindAll("#fk-kalem-bolum .bos-durum"));
        Assert.DoesNotContain("zo-kalem-kart", c.Find("#fk-kalem-bolum").ClassList);
        c.Find("#fk-ekle").Click();
        Assert.Contains("zo-kalem-kart", c.Find("#fk-kalem-bolum").ClassList);   // iç boşluklu kart
        // Boş kaydet: istek YOK, odak ilk hatalı alana.
        var once = istekler.Count(i => i.StartsWith("POST /api/v1/faturalar ", StringComparison.Ordinal));
        c.Find("#fk-kaydet").Click();
        Assert.Equal(once, istekler.Count(i => i.StartsWith("POST /api/v1/faturalar ", StringComparison.Ordinal)));
        var odak = JSInterop.Invocations["kuyumcuOdak.alana"];
        Assert.Contains(odak, i => (string?)i.Arguments[0] == "fatura-cari");
        // DEVAM 12: Kaydet çekmecenin ALTLIĞINDA (İptal | Kaydet), gövdenin dibine yapışık (ZB71).
        var css = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "wwwroot", "css", "zo-belge.css"));
        Assert.Contains("position: sticky; bottom: 0;", css);
        // Çekmecede kalem satırı KART (ürün tam satır, sayılar iki sütun, etiketler görünür).
        Assert.Contains("html[data-mod] .zo-yeni-fatura .tablo-sar[data-zo=\"kalem\"] > table.veri-tablosu > thead { display: none; }", css);
        Assert.Contains("display: grid; grid-template-columns: repeat(2, minmax(0, 1fr));", css);
        Assert.NotNull(c.Find("#fk-kalem-bolum .tablo-sar[data-zo='kalem'] > table.veri-tablosu"));
        // UX-S26: oluşturma formu düzenleme formuyla aynı zorunluluğu taşır (miktar, birim fiyat "*").
        Assert.Contains("*", c.Find("label[for='fk-miktar-0']").TextContent);
        Assert.Contains("*", c.Find("label[for='fk-fiyat-0']").TextContent);
    }

    /// <summary>Gün sonu mutabakatı (UX1C3 fikstürünün aynısı) — Raporlar PRO çizilebilsin (ZB55).</summary>
    private const string Mutabakat = """{"tarih":"2026-09-16", "tahsilat":{"nakit":42850,"kart":48000,"havale":0,"diger":0,"toplam":90850}, "satis":{"sayisi":4,"toplamTl":169000,"faturaliSayisi":0,"faturaliTl":0, "faturasizSayisi":4,"faturasizTl":169000}, "alis":{"sayisi":1,"toplamTl":340500,"faturaSayisi":0,"faturaToplamTl":0}, "hurda":{"sayisi":1,"odenenTl":74196,"hasGrami":50,"pusulaSayisi":1, "pusulaBrutTl":74196,"stopajToplamTl":0,"belgesizSayisi":0, "gecikmisPusulaSayisi":0}, "fatura":{"sayisi":1,"toplamTl":142500,"kdvToplam":0,"satisFaturasiTl":142500}, "faturaEslesme":{"bagliSayisi":0,"bagliTl":0,"bagimsizSayisi":1, "bagimsizTl":142500,"faturasizTl":169000, "farkTanimi":"FARK = satış toplamı − fişe bağlı faturalar − fişe bağlanmamış faturalar", "bagimsizFaturalar":[{"id":"f1","faturaNo":"SAT-2026-000007", "tarih":"2026-09-16T13:00:00Z","toplam":142500, "cariId":"c1","durum":"KESILDI"}]}, "fark":26500,"mutabik":false,"uyarilar":[],"faturasizIslemler":[]}""";

    private const string AylikAyni = """
        {"yil":2026,"ay":9,"satis":{"sayisi":4,"toplamTl":200000,"toplamHas":10},"alis":{"sayisi":1,"toplamTl":50000,"toplamHas":5},
         "tahsilat":{"toplamTl":0},"veresiye":{"toplamTl":0},"karTl":150000,"karKaynagi":"KAR_ZARAR_SERVISI",
         "satisAlisFarkiTl":150000,"netHasGramHareketi":-5,"hareketSayisi":5,"gunler":[]}
        """;

    [Fact]
    public void ZB55_Rapor_PRO_kar_ile_satis_alis_farki_AYNIYSA_tek_kart_aciklamalar_ipucunda()
    {
        Mod(ArayuzModuServisi.Profesyonel);
        aylik = AylikAyni;
        var c = Render<Kuyumcu.Web.Pages.Raporlar>();
        c.WaitForElement("#rp-kar");
        var T = Sozlukler.Getir("tr");
        Assert.Equal(Bicim.TlKurus(150000m), c.Find("#rp-kar-deger").TextContent.Trim());
        Assert.Equal(ZB.RaporKar, c.Find("#rp-kar-etiket > span").TextContent.Trim());
        Assert.Empty(c.FindAll("#rp-fark"));                                   // aynı büyüklük ikinci kartta YOK
        Assert.Contains(ZB.RaporKarAyni, c.Find("#rp-kar-ipucu").TextContent);   // bunu ipucu söyler
        // Paragraf açıklama kartta basılmaz — (i) ipucunda.
        Assert.Empty(c.FindAll("#rp-ozet-kartlar .stat-alt"));
        Assert.Contains(T.SayfaDuzeltme.NetHasAciklama, c.Find("#rp-nethas-ipucu").TextContent);
        Assert.Contains("zo-stat-izgara", c.Find("#rp-ozet-kartlar").ClassList);
        // Kart en az 15,5rem: kısa etiket ("ALIŞ-SATIŞ NAKİT FARKI") tek satırda (tarayıcıda K3 kırık 0).
        var css = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "wwwroot", "css", "zo-belge.css"));
        Assert.Contains("#raporlar-pro #rp-ozet-kartlar.zo-stat-izgara { grid-template-columns: repeat(auto-fill, minmax(min(100%, 15.5rem), 1fr)); }", css);

        // Farklıysa fark kendi adıyla ayrı kartta kalır (özellik silinmez).
        aylik = AylikAyni.Replace("\"satisAlisFarkiTl\":150000", "\"satisAlisFarkiTl\":140000", StringComparison.Ordinal);
        var d = Render<Kuyumcu.Web.Pages.Raporlar>();
        d.WaitForElement("#rp-fark");
        Assert.Equal(Bicim.TlKurus(140000m), d.Find("#rp-fark-deger").TextContent.Trim());
        Assert.DoesNotContain(ZB.RaporKarAyni, d.Find("#rp-kar-ipucu").TextContent);
    }

    [Fact]
    public void ZB56_Veri_ice_aktar_birincil_dugme_sade_ad_bicim_bilgisi_katlanirda()
    {
        Mod("BASIT");
        var c = Render<Kuyumcu.Web.Pages.VeriIceAktarim>();
        c.WaitForElement("#ia-prova");
        Assert.Equal(ZB.IceProvaDugme, c.Find("#ia-prova").TextContent.Trim());
        Assert.DoesNotContain("prova", c.Find("#ia-prova").TextContent, StringComparison.OrdinalIgnoreCase);
        var T = Sozlukler.Getir("tr");
        Assert.DoesNotContain(T.IceAktarim.SablonIpucu, c.Markup);
        Assert.DoesNotContain(T.IceAktarim.DosyaIpucu, c.Markup);
        Assert.Equal(ZB.IceNasilHazirlarim, c.Find("#ia-nasil-ac .katlanir-ad").TextContent.Trim());
        c.Find("#ia-nasil-ac").Click();
        Assert.DoesNotContain("uyari-metni", c.Find("#ia-kapsam-notu").ClassName);
        Assert.Equal(T.IceAktarim.SablonIpucu, c.Find("#ia-sablon-ipucu").TextContent.Trim());
        Assert.StartsWith(T.IceAktarim.DosyaIpucu, c.Find("#ia-dosya-ipucu").TextContent.Trim());
    }

    // ═══ 29.09 DEVAM 7 — doğrulayıcı (KALDI) bulguları ══════════════════════════

    private const string DenetimElleKasa = """
        {"toplam":3,"limit":25,"offset":0,"kayitlar":[
          {"id":"d1","tarih":"2026-09-29T10:00:00Z","eylem":"OLUSTURMA","varlikTuru":"KasaHareketi","varlikId":"kh1","hassas":false,
           "detay":"elle kasa hareketi: GIRIS 1500.50 TRY · açıklama: dogrulayici","ipAdresi":null,"kullaniciId":"u1","kullanici":"Ayşe Yılmaz","rol":"SAHIP",
           "subeId":null,"subeAd":null,"kasaId":"ks1","kasaAd":"Merkez Kasa"},
          {"id":"d2","tarih":"2026-09-29T10:05:00Z","eylem":"OLUSTURMA","varlikTuru":"KasaHareketi","varlikId":"kh2","hassas":false,
           "detay":"elle kasa hareketi: CIKIS 250 USD","ipAdresi":null,"kullaniciId":"u1","kullanici":"Ayşe Yılmaz","rol":"SAHIP",
           "subeId":null,"subeAd":null,"kasaId":"ks2","kasaAd":"Döviz Kasası"},
          {"id":"d3","tarih":"2026-09-29T10:10:00Z","eylem":"OLUSTURMA","varlikTuru":"KasaHareketiStorno","varlikId":"kh3","hassas":true,
           "detay":"elle kasa hareketi stornosu (kaynak 01ABCDEFGH) · KAPALI GÜN STORNOSU · gerekçe: yanlış tutar girildi","ipAdresi":null,"kullaniciId":"u1","kullanici":"Ayşe Yılmaz","rol":"SAHIP",
           "subeId":null,"subeAd":null,"kasaId":"ks1","kasaAd":"Merkez Kasa"}]}
        """;

    /// <summary>Doğrulayıcı KRİTİK (29.09): BASİT yüzde "GIRIS 1500.50 TRY" (ham yön kodu,
    /// InvariantCulture sayı, TRY kodu) basılıyordu. Beklenen metinler SABİT yazıldı.</summary>
    [Fact]
    public void ZB57_Denetim_elle_kasa_hareketi_trTR_para_ham_kod_ve_TRY_YOK_PRO_ham_kayit_ipucunda()
    {
        Mod("BASIT");
        denetimListe = DenetimElleKasa;
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        c.WaitForAssertion(() => Assert.Equal(3, c.FindAll("#dz-tablo tbody tr").Count));
        var tablo = c.Find("#dz-tablo").TextContent.Replace('\u00a0', ' ');
        Assert.Contains("Kasaya elle giriş: 1.500,50 ₺ · açıklama: dogrulayici", tablo);
        Assert.Contains("Kasadan elle çıkış: 250,00 $", tablo);
        Assert.Contains("Elle kasa hareketi geri alındı · kapanmış gün · gerekçe: yanlış tutar girildi", tablo);
        foreach (var ham in new[] { "TRY", "GIRIS", "CIKIS", "1500.50", "01ABCDEFGH", "STORNOSU", "USD" })
            Assert.DoesNotContain(ham, tablo);

        // BASİT'te ham kayıt ipucunda da YOK.
        Assert.All(c.FindAll("#dz-tablo tbody td"), t => Assert.Null(t.GetAttribute("title")));

        // PRO: aynı sade tutar biçimi (gerçek tarayıcı kuralı "TRY 0" iki modda); ham kayıt
        // denetçi için hücrenin İPUCUNDA aynen durur (kayıt değişmez).
        Mod("PROFESYONEL");
        var p = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        p.WaitForAssertion(() => Assert.Equal(3, p.FindAll("#dz-tablo tbody tr").Count));
        var pro = p.Find("#dz-tablo tbody").TextContent.Replace('\u00a0', ' ');
        Assert.Contains("Kasaya elle giriş: 1.500,50 ₺ · açıklama: dogrulayici", pro);
        Assert.DoesNotContain("TRY", pro);
        // PAKET GOC-D: dz-tablo ortak ızgarada; ham kayıt ipucu (title) hücrenin İÇ öğesinde (ızgara td özniteliği satıra göre değişmez) — sözleşme aynı.
        Assert.Contains(p.FindAll("#dz-tablo tbody td [title]"),
            t => t.GetAttribute("title") == "elle kasa hareketi: GIRIS 1500.50 TRY · açıklama: dogrulayici");
    }

    [Fact]
    public void ZB57b_Elle_kasa_cevirisi_kalip_disinda_DOKUNMAZ_EN_ceviri_kullanici_metni_aynen()
    {
        static string Para(decimal n, string pb) => Bicim.Para(n, pb).Replace('\u00a0', ' ');
        Assert.Null(ZoBelgeDenetimSozlugu.KasaHareketiMetni("liste: 3 kayıt", "tr", Para));
        Assert.Null(ZoBelgeDenetimSozlugu.KasaHareketiMetni(null, "tr", Para));
        Assert.Null(ZoBelgeDenetimSozlugu.KasaHareketiMetni("elle kasa hareketi: YANA 5 TRY", "tr", Para));
        Assert.Equal("Cash in (manual): 1.500,50 ₺ · note: kira",
            ZoBelgeDenetimSozlugu.KasaHareketiMetni("elle kasa hareketi: GIRIS 1500.50 TRY · açıklama: kira", "en", Para));
        Assert.Equal("Kasadan elle çıkış: 0,01 ₺",
            ZoBelgeDenetimSozlugu.KasaHareketiMetni("elle kasa hareketi: CIKIS 0.01 TRY", "tr", Para));
        Assert.Equal("Elle kasa hareketi geri alındı · gerekçe: x",
            ZoBelgeDenetimSozlugu.KasaHareketiMetni("elle kasa hareketi stornosu (kaynak K1) · gerekçe: x", "tr", Para));
    }

    /// <summary>Doğrulayıcı DÜŞÜK (29.09): has 4 hane kuralını bu dosya kilitlemiyordu
    /// (Bicim.Has → Bicim.Gram mutasyonu burada kırmızı vermedi). Beklenen SABİT.</summary>
    [Fact]
    public void ZB58_Rapor_BASIT_bugun_has_DORT_hane_0_0001_kaybolmaz()
    {
        gunluk = """
            {"tarih":"2026-09-29","satis":{"adet":1,"toplamTl":100.00,"toplamHas":0.0001},
             "alis":{"adet":1,"toplamTl":50.00,"toplamHas":12.5},
             "hurda":{"toplamHas":0,"odenenTl":0},"netHasGramHareketi":0,"hareketSayisi":2,
             "kasalar":[{"id":"k1","ad":"Nakit","bakiye":50.00,"paraBirimi":"TRY"}]}
            """;
        Mod("BASIT");
        var c = Render<Kuyumcu.Web.Pages.Raporlar>();
        c.WaitForElement("#bugun-satis-has");
        c.WaitForAssertion(() => Assert.Equal("0,0001 has", c.Find("#bugun-satis-has").TextContent.Trim().Replace('\u00a0', ' ')));
        Assert.Equal("12,5000 has", c.Find("#bugun-alis-has").TextContent.Trim().Replace('\u00a0', ' '));
    }

    /// <summary>Doğrulayıcı DÜŞÜK (29.09): kasa defterinde Ana Kasa "0 ₺", Döviz Kasası "0,00 $"
    /// basılıyordu. Kasa GEÇMİŞ tablolarında tüm para birimleri kuruş İKİ HANE. Beklenen SABİT.</summary>
    [Fact]
    public void ZB59_Kasa_gecmis_tablolarinda_TL_ve_doviz_AYNI_ondalik_kurali_iki_hane()
    {
        var c = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        c.WaitForElement("#kr-bakiye-ks1");
        // Özet satırı ekranın genel biçimini korur (ZB01, doğrulayıcı ZOBS14): "1.200 ₺".
        Assert.Equal("1.200 ₺", c.Find("#kr-bakiye-ks1").TextContent.Trim().Replace('\u00a0', ' '));
        c.Find("#kr-gecmis-ac").Click();
        c.WaitForElement("#kr-tablo-ks1");
        var tl = c.Find("#kr-tablo-ks1 tbody").TextContent.Replace('\u00a0', ' ');
        Assert.Contains("0,00 ₺", tl);        // dünün devri 0 → "0 ₺" DEĞİL
        Assert.Contains("500,00 ₺", tl);
        Assert.DoesNotMatch(@"(^|[^\d,])0 ₺", tl);
        Assert.Contains("300,00 $", c.Find("#kr-tablo-ks2 tbody").TextContent.Replace('\u00a0', ' '));
    }

    /// <summary>Doğrulayıcı DÜŞÜK (29.09): Razor özniteliğinde '{ }' gövdeli lambda yasak
    /// (DEĞİŞMEZLER). Yedi ekranın hiçbirinde `@(async … => { … })` öznitelik değeri yok.</summary>
    [Fact]
    public void ZB60_Yedi_ekranda_ozniteligte_govdeli_lambda_YOK()
    {
        var kalip = new System.Text.RegularExpressions.Regex(@"=""[^""]*=>\s*\{");
        foreach (var ad in Sayfalar)
            Assert.False(kalip.IsMatch(Sayfa(ad)), $"{ad}: öznitelikte gövdeli lambda kaldı");
    }


    /// <summary>Doğrulayıcı ORTA (29.09): İşlem defteri ve Kim ne yaptı açılışta 2,1–2,7 ekrandı.
    /// Masaüstünde tablo KENDİ kabında dikey kayar (başlık yapışık), sayfa boyu 25 kalır; dar
    /// ekranda ve kâğıtta sınır YOK. Tarayıcı ölçümü: 1440/1280 × BASİT/PRO açılış 1,0 (§ L-7).</summary>
    [Fact]
    public void ZB61_Liste_ekranlari_ic_kaydirma_YOK_tablo_sayfayla_akar_sayfa_boyu_25_kalir()
    {
        // KESIF-P3 (TOPLU ZO-BELGE [YÜKSEK]): iç kaydırma KALDIRILDI — tablo sayfayla akar,
        // kesik satır yok (tuvalde iç kaydırmalı tablo yok). Kural geri gelirse KIRMIZI.
        var css = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "wwwroot", "css", "zo-belge.css"));
        Assert.DoesNotContain("@media screen and (min-width: 1024px) and (min-height: 600px) {", css);
        var kod = System.Text.RegularExpressions.Regex.Replace(css, @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.DoesNotContain("--zo-liste-pay", kod);
        Assert.DoesNotMatch(@":has\(> #id-tablo, > #dz-tablo\)\s*\{[^}]*max-height", css);
        // Sayfa boyu hakem kararıyla 25 (açılış ölçüsü için KISALTILMADI).
        Mod("BASIT");
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-tablo");
        Assert.Contains(istekler, x => x.Contains("/raporlar/islem-defteri", StringComparison.Ordinal) && x.Contains("limit=25", StringComparison.Ordinal));
    }

    // ═══ 29.09 DEVAM 8 — M11-10/11/12 hayatta kalan mutasyonlar (test açığı) ════════

    /// <summary>Süzgeç alanı DEĞİŞİNCE yeni değer isteğe GERÇEKTEN girer (işleyici alanı atamazsa
    /// istek eski değerle gider ve ekran "süzdüm" sanır — sessiz etkisizlik).</summary>
    [Fact]
    public void ZB62_Islem_defteri_tarih_degisince_istek_YENI_aralikla_gider()
    {
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-ozet");
        c.Find("#id-baslangic").Change("2026-09-01");
        c.WaitForAssertion(() => Assert.Contains("baslangic=2026-09-01", istekler.Last(i => i.Contains("/raporlar/islem-defteri?", StringComparison.Ordinal)), StringComparison.Ordinal));
        c.Find("#id-bitis").Change("2026-09-15");
        c.WaitForAssertion(() => Assert.Contains("bitis=2026-09-15", istekler.Last(i => i.Contains("/raporlar/islem-defteri?", StringComparison.Ordinal)), StringComparison.Ordinal));
        Assert.Contains("baslangic=2026-09-01", istekler.Last(i => i.Contains("/raporlar/islem-defteri?", StringComparison.Ordinal)), StringComparison.Ordinal);
    }

    [Fact]
    public void ZB62b_Denetim_yalniz_hassas_isaretlenince_istek_hassas_true_ile_gider_rozet_1()
    {
        denetimListe = DenetimElleKasa;
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        c.WaitForAssertion(() => Assert.Equal(3, c.FindAll("#dz-tablo tbody tr").Count));
        Assert.DoesNotContain(istekler, i => i.Contains("/denetim?", StringComparison.Ordinal) && i.Contains("hassas=true", StringComparison.Ordinal));
        c.Find("#dz-suzgec-ac").Click();
        c.Find("#f-yalniz-hassas").Change(true);
        c.WaitForAssertion(() => Assert.Contains("hassas=true", istekler.Last(i => i.Contains("/denetim?", StringComparison.Ordinal)), StringComparison.Ordinal));
        Assert.Equal("1", c.Find("#dz-suzgec-sayi").TextContent.Trim());
    }

    [Fact]
    public void ZB62c_Evrak_vade_araligi_degisince_istek_iki_ucla_gider()
    {
        var c = Render<Kuyumcu.Web.Pages.Evraklar>().AyrintiAc();
        c.WaitForElement("#evrak-ozet-portfoy");
        c.Find("#evrak-suzgec-ac").Click();
        c.Find("#evrak-vade-bas").Change("2026-10-01");
        c.WaitForAssertion(() => Assert.Contains("vadeBas=2026-10-01", istekler.Last(i => i.Contains("/evraklar?", StringComparison.Ordinal)), StringComparison.Ordinal));
        c.Find("#evrak-vade-bit").Change("2026-10-31");
        c.WaitForAssertion(() => Assert.Contains("vadeBit=2026-10-31", istekler.Last(i => i.Contains("/evraklar?", StringComparison.Ordinal)), StringComparison.Ordinal));
        Assert.Contains("vadeBas=2026-10-01", istekler.Last(i => i.Contains("/evraklar?", StringComparison.Ordinal)), StringComparison.Ordinal);
        Assert.Equal("2", c.Find("#evrak-suzgec-sayi").TextContent.Trim());
    }

    // ── Doğrulayıcı 29.09 (ORTA): taslak faturanın iç işareti "TASLAK:<ULID>" ekrana basılmaz ──
    [Fact]
    public void ZB67_Islem_defteri_PRO_taslak_fatura_isareti_ULID_gostermez_Kesimde_verilir_yazar()
    {
        Mod("PROFESYONEL");
        islemDefteri = """
            {"baslangic":"2026-09-18","bitis":"2026-09-24","toplamSatir":2,"ofset":0,"limit":25,"dahaFazlasiVar":false,
             "ozet":{"tlGiris":2500,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":2},
             "satirlar":[{"id":"h1","tarih":"2026-09-20T10:00:00Z","tur":"SATIS","odemeTuru":"NAKIT","tlTutari":1500,"hasGrami":0,
                          "cariAd":"Ayşe Demir","faturaNo":"TASLAK:01M3PNH9H1GB5BEVW35CV216DY"},
                         {"id":"h2","tarih":"2026-09-20T11:00:00Z","tur":"SATIS","odemeTuru":"NAKIT","tlTutari":1000,"hasGrami":0,
                          "cariAd":"Can Demir","faturaNo":"SAT-2026-000009"}]}
            """;
        var c = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        c.WaitForElement("#id-fatura-h1");
        var T = Sozlukler.Getir("tr");
        var kisa = CumaJSozlugu.Ekran("tr").FaturaNoKesimdeKisa;
        Assert.DoesNotContain("TASLAK:", c.Markup);
        Assert.DoesNotContain("01M3PNH9H1GB5BEVW35CV216DY", c.Markup);
        Assert.Contains(kisa, c.Find("#id-fatura-h1").TextContent);
        Assert.Equal(T.Fatura.NoKesimde, c.Find("#id-fatura-h1").GetAttribute("title"));
        // Kesilmiş faturanın numarası AYNEN görünür.
        Assert.Contains("SAT-2026-000009", c.Find("#id-fatura-h2").TextContent);
        Assert.DoesNotContain(kisa, c.Find("#id-fatura-h2").TextContent);
        // Ekranın öneki sunucunun damgasıyla birebir (sunucu değişirse bu test kırılır).
        Assert.Equal(Kuyumcu.Api.Modules.Fatura.FaturaSeriKapisi.TaslakOnEki, Kuyumcu.Web.Pages.IslemDefteri.TaslakOnEki);
    }

    // ── Doğrulayıcı 29.09 (DÜŞÜK): yapışkan sütun gölgesi yalnız TABLO düzeninde ──
    [Fact]
    public void ZB68_Yapiskan_sutun_golgesi_yalniz_tablo_duzeninde_kart_duzeninde_yok()
    {
        var css = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "wwwroot", "css", "zo-belge.css"));
        var golge = "-8px 0 10px -6px color-mix(in srgb, var(--metin) 28%, transparent)";
        var tablo = System.Text.RegularExpressions.Regex.Match(css,
            @"@container zoyigin \(min-width: (\d+)px\) \{(.*?)\n\}", System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.True(tablo.Success, "gölge tablo düzeni kap sorgusunda değil");
        var kart = System.Text.RegularExpressions.Regex.Match(css, @"@container zoyigin \(max-width: (\d+)px\)");
        Assert.True(kart.Success);
        // Eşikler bitişik: kart düzeni ≤ N, gölge ≥ N+1 (arada gölgeli kart kalmaz).
        Assert.Equal(int.Parse(kart.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) + 1,
            int.Parse(tablo.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(tablo.Groups[2].Value, System.Text.RegularExpressions.Regex.Escape(golge)).Count);
        Assert.DoesNotContain(golge, css.Replace(tablo.Value, "", StringComparison.Ordinal));
    }

    // ── Doğrulayıcı 29.09 (DÜŞÜK): <details> bir <p> içinde olamaz (HTML içerik modeli) ──
    [Fact]
    public void ZB69_Evrak_ozeti_div_ipucu_paragraf_icinde_degil()
    {
        var c = Render<Kuyumcu.Web.Pages.Evraklar>().AyrintiAc();
        c.WaitForElement("#evrak-ozet");
        Assert.Equal("DIV", c.Find("#evrak-ozet").TagName);
        Assert.Empty(c.FindAll("p details, p p, p div"));
        Assert.NotNull(c.Find("#evrak-ozet > #evrak-ozet-ipucu"));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET KESIF-P3 — denetim izi ham kod/jargon (#54/#55), kasa raporu tekrar cümlesi.
    // ═════════════════════════════════════════════════════════════════════════

    private const string DenetimKodlu = """
        {"toplam":2,"limit":7,"offset":0,"kayitlar":[
          {"id":"d1","tarih":"2026-09-13T10:00:00Z","eylem":"OKUMA","varlikTuru":"TevsikIhlali","varlikId":"h1","hassas":false,
           "detay":"SATIS_FISI belgesi görüntülendi · islem-defteri","ipAdresi":null,"kullaniciId":"u1","kullanici":"Ayşe Yılmaz","rol":"SAHIP",
           "subeId":null,"subeAd":null,"kasaId":null,"kasaAd":null},
          {"id":"d2","tarih":"2026-09-13T09:00:00Z","eylem":"EKLEME","varlikTuru":"Cari","varlikId":"c1","hassas":false,
           "detay":"Ayşe Yılmaz carisi açıldı · tür SAYIM_DUZELTME","ipAdresi":null,"kullaniciId":"u1","kullanici":"Ayşe Yılmaz","rol":"SAHIP",
           "subeId":null,"subeAd":null,"kasaId":null,"kasaAd":null}]}
        """;

    private static readonly System.Text.RegularExpressions.Regex BuyukAltCizgi =
        new(@"\b[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)+\b", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    [Fact]
    public void DenetimIzi_PRO_gorunur_ham_kod_yok_title_korunur()
    {
        Mod("PROFESYONEL");
        denetimListe = DenetimKodlu;
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        c.WaitForAssertion(() => Assert.Equal(2, c.FindAll("#dz-tablo tbody tr").Count));
        var tablo = c.Find("#dz-tablo").TextContent;
        Assert.DoesNotMatch(BuyukAltCizgi, tablo);
        Assert.Contains("satış fişi belgesi görüntülendi · işlem defteri", tablo);
        Assert.DoesNotContain("islem-defteri", tablo);
        // Sözlükte adı olmayan kayıt türü ham basılmaz (canlı taramada "TevsikIhlali").
        Assert.DoesNotContain("TevsikIhlali", tablo);
        Assert.Contains(KesifP3Sozlugu.Tr.BelgeSatisFisi, tablo);
        Assert.Contains("Bankasız ödeme sınırı aşımı", tablo);
        // Ham kayıt denetçi için title ipucunda AYNEN (kayıt değişmez).
        // PAKET GOC-D: dz-tablo ortak ızgarada; ham kayıt ipucu (title) hücrenin İÇ öğesinde (ızgara td özniteliği satıra göre değişmez) — sözleşme aynı.
        var ipuclari = c.FindAll("#dz-tablo td [title]").Select(t => t.GetAttribute("title")).ToList();
        Assert.Contains("SATIS_FISI belgesi görüntülendi · islem-defteri", ipuclari);
        Assert.Contains("Ayşe Yılmaz carisi açıldı · tür SAYIM_DUZELTME", ipuclari);
        // Karar 1: PRO ekranda "cari" kalabilir.
        Assert.Contains("carisi", tablo);
    }

    [Fact]
    public void DenetimIzi_BASIT_cari_ekli_bicim_ve_ham_kod_yok()
    {
        Mod("BASIT");
        denetimListe = DenetimKodlu;
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        c.WaitForAssertion(() => Assert.Equal(2, c.FindAll("#dz-tablo tbody tr").Count));
        var tablo = c.Find("#dz-tablo").TextContent;
        Assert.DoesNotMatch(BuyukAltCizgi, tablo);
        Assert.DoesNotMatch(new System.Text.RegularExpressions.Regex(@"(?<![\p{L}])[Cc]ari"), tablo);
        Assert.Contains("müşterisi açıldı", tablo);
    }

    /// <summary>KESIF-P3 tasarım hakemi tur 2: süzgeçte "yalnız eklenir" kuralı üç kez yazılıyordu
    /// ve BASİT'te KVKK madde numarası vardı. BASİT: kısa satır + (i) ipucu; kural cümlesi 1 kez,
    /// değişmez kutusu, yasal dayanak ve "Düzeltme şöyle yapılır" YOK.</summary>
    [Fact]
    public void DenetimIzi_BASIT_degismez_tek_ipucu_madde_yok()
    {
        Mod("BASIT");
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        c.WaitForElement("#dz-suzgec-ac");
        c.Find("#dz-suzgec-ac").Click();
        c.WaitForElement("#dz-degismez-ipucu");
        var suz = c.Find("#dz-suzgec").TextContent;
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(suz, "yalnız eklenir"));
        Assert.Equal("DETAILS", c.Find("#dz-degismez-ipucu").TagName);
        Assert.False(c.Find("#dz-degismez-ipucu").HasAttribute("open"));
        Assert.Empty(c.FindAll("#degismez-ipucu-DenetimKaydi"));
        Assert.Empty(c.FindAll("#dz-yasal-dayanak"));
        Assert.DoesNotContain("KVKK", suz);
        Assert.DoesNotContain("md.", suz);
        Assert.DoesNotContain("Düzeltme şöyle", suz);
    }

    /// <summary>PRO: kural cümlesi 1 kez (değişmez kutusunun gerekçesi), "Düzeltme şöyle yapılır"
    /// satırı YOK (bu kaydın düzeltme yolu yoktur); yasal dayanak PRO'da kalır.</summary>
    [Fact]
    public void DenetimIzi_PRO_degismez_tek_cumle_duzeltme_satiri_yok()
    {
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        c.WaitForElement("#dz-suzgec-ac");
        c.Find("#dz-suzgec-ac").Click();
        c.WaitForElement("#degismez-ipucu-DenetimKaydi");
        var suz = c.Find("#dz-suzgec").TextContent;
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(suz, "yalnız eklenir"));
        Assert.Empty(c.FindAll("#degismez-ipucu-DenetimKaydi .degismez-alternatif"));
        Assert.Empty(c.FindAll("#dz-degismez-ipucu"));
        Assert.NotNull(c.Find("#dz-yasal-dayanak"));
    }

    [Fact]
    public void KasaRaporu_sayim_kurali_tabloda_tek_kez()
    {
        // İki kasa, her birinde DÜN AÇIK kalmış gün → eskiden her satırda aynı cümle.
        kasaDefteri = KasaDefteriBugunAcik().Replace("\"durum\":\"KAPALI\",\"sayilanNakit\":500", "\"durum\":\"ACIK\",\"sayilanNakit\":500");
        var c = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        c.WaitForElement("#kr-gecmis-ac");
        c.Find("#kr-gecmis-ac").Click();
        c.WaitForElement("#kr-tablo-ks1");
        var kural = KesifP3Sozlugu.Tr.YalnizBugunSayilir;
        Assert.Equal(kural, c.Find("#kr-sayim-kural").TextContent.Trim());
        // Satırdaki sebep id'si yaşar ama GÖRÜNÜR tekrar yok (ekran okuyucuya kalır).
        var sebep = c.Find($"#kr-sayim-ks1-{Dun}-sebep");
        Assert.Contains("gorunmez", sebep.ClassList);
        var gorunenTekrar = c.FindAll("span.metin-soluk").Count(e => e.TextContent.Contains(Sozlukler.Tr.RaporSeti.SayimYalnizBugun));
        Assert.Equal(0, gorunenTekrar);
    }
}
