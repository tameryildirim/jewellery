using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q10-2 — /hurda RAMAT ÇIKIŞI ONAYI + BAKİYE MESAJI + CANLI ALAN HATASI
// (QA turu 10, 24.09 — gerçek tarayıcı bulguları) [bUnit]
//
//   • KRİTİK: ramat çıkışı (geri alınamaz hurda satışı) onaysız tek tıkla
//     kaydediliyordu → "Ramat çıkışını kaydet" artık YALNIZ doğrular ve özet
//     onay penceresini açar; POST yalnız onayla gider, Vazgeç'te istek YOKTUR.
//   • Bakiye yeterliyken / aşıldığında "Bu madende hurda bakiyesi yok" → mesaj
//     gerçek sebebi ve mevcut bakiyeyi söyler; sınır dahildir (= bakiye geçer,
//     bakiye + 0,0001 geçmez).
//   • Alan hatası değer düzeltilince kalkar (has, kasa, gerekçe).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Q102HurdaOnayEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    // Q10-2 düzeltme: bakiye fikstürü değiştirilebilir (6 haneli has sınanır).
    private string bakiyeHas = "9.16";

    // Q10-2 düzeltme turu: defter yanıtı değiştirilebilir (BASİT mod jargon sınaması).
    private string defterYaniti = """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}""";

    private string fireNotuJson = "null";

    private string kasaYaniti = """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":250000,"aktif":true}]""";

    public Q102HurdaOnayEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => kasaYaniti, () => bakiyeHas, () => defterYaniti, () => fireNotuJson))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<string> kasalar, Func<string> bakiye, Func<string> defter, Func<string> fire) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, istek.RequestUri!.PathAndQuery, govde));

            // Gerçekçi yanıtlar ("{}" varsayılanı yeni uçlara YETMEZ).
            var g = yol switch
            {
                var y when y.EndsWith("/hurda/bakiye") =>
                    "{\"subeId\":\"s1\",\"satirlar\":[{\"maden\":\"ALTIN\",\"madenAdi\":\"Altın\",\"hasGram\":" + bakiye()
                    + ",\"girenHas\":" + bakiye() + ",\"cikanHas\":0,\"tahlilFarkiHas\":0}],\"fireKaydiAcik\":false,\"fireNotu\":" + fire() + "}",
                var y when y.EndsWith("/hurda/defter") => defter(),
                var y when y.EndsWith("/islemler/hurda-satis") => """
                    {"id":"h9","maden":"ALTIN","madenAdi":"Altın","hasGrami":0.5,
                     "birimFiyatTlHasGram":6800,"toplamTl":3400,"tahsilEdilenTl":3400,"kalanTl":0,
                     "kalanHurdaHasGram":8.66,"kasaBakiyesi":253400,"paraBirimi":"TRY",
                     "tarih":"2026-09-24T09:00:00Z","fireKaydiAcik":false,"fireNotu":null,
                     "aliciKabulHasGrami":0.5,"tahlilFarkiHasGram":0}
                    """,
                var y when y.EndsWith("/kasalar") => kasalar(),
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-24T09:00:00Z","ayarFiyatlari":[]}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Hurda> Ekran()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.Hurda>();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/hurda/defter")));
        // Kur gelmeden düğme kilitlidir; kilit açılmadan tıklamak testi kararsız yapar.
        e.WaitForAssertion(() => Assert.False(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        // ZO-ISLEM-A: isteğe bağlı alanlar "+ Detay ekle", defter kapalı akordeon
        // arkasında. Bu dosyanın testleri o alanların KURALLARINI sınar; açılış
        // davranışı ZOISLEMAEkranTestleri'nde ayrıca sınanır.
        e.WaitForElement("#hrd-detay-ac").Click();
        e.WaitForElement("#hrd-gecmis-ac").Click();
        return e;
    }

    /// <summary>
    /// Q10-2 düzeltme — bUnit yarışı: sayfa ilk yüklemeden sonra bir kez daha çizilir
    /// (CanliKurServisi → KurYansit → InvokeAsync(StateHasChanged)). Find ile Input
    /// arasına düşen bir çizim olay kimliğini yeniler ve girdi KAYBOLUR (kutu boş
    /// kalır: value=""). Ölçüldü: DQ102_turkce… ve Asim_mesaji… sırası bağımlı
    /// kırmızı veriyordu. Yardımcı girdiyi kutu değeri tutana dek YENİDEN verir;
    /// sınanan kural değişmez (değer aynen, bir kez etkili olur).
    /// </summary>
    private static void Gir(IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e, string secici, string deger) =>
        e.WaitForAssertion(() =>
        {
            if (e.Find(secici).GetAttribute("value") != deger) e.Find(secici).Input(deger);
            Assert.Equal(deger, e.Find(secici).GetAttribute("value"));
        });

    private bool PostVar() => istekler.Any(i => i.Metot == "POST" && i.Yol.Contains("hurda-satis"));

    // ── KRİTİK — onaysız tek tık yok ─────────────────────────────────────────

    [Fact]
    public void Kaydet_tiklaninca_POST_ATILMAZ_once_ozetli_onay_penceresi_acilir()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0.5");
        e.Find("#hrd-kaydet").Click();

        e.WaitForElement("#hrd-onay-onay-kipi");
        Assert.False(PostVar());

        var ozet = e.Find("#hrd-onay-hedef").TextContent;
        Assert.Contains("Altın", ozet);                 // maden
        Assert.Contains("0,500 g", ozet);               // gram / has
        Assert.Contains("3.400", ozet);                 // tutar = 0,5 × 6.800
        Assert.Contains("Merkez Kasa", ozet);           // kasa
        Assert.Contains("değiştirilemez", e.Find("#hrd-onay-aciklama").TextContent);
        // Gerçek düzeltme yolu: ekrandaki GERÇEK düğme adı ile.
        Assert.Contains(Sozlukler.Tr.Hurda.Duzelt, e.Find("#hrd-onay-geri-donus").TextContent);
    }

    [Fact]
    public void Vazgec_istek_ATMAZ_ve_pencere_kapanir()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0.5");
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-vazgec").Click();

        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hrd-onay-onay-kipi")));
        Assert.False(PostVar());
    }

    [Fact]
    public void Onaylaninca_POST_gider_bant_yazilir_ve_pencere_kapanir()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0.5");
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();

        e.WaitForAssertion(() => Assert.True(PostVar()));
        Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("hurda-satis"));
        var govde = istekler.Single(i => i.Metot == "POST").Govde;
        Assert.Contains("\"hasGrami\":0.5", govde);
        Assert.Contains("\"kasaId\":\"ks1\"", govde);
        e.WaitForAssertion(() => Assert.Contains("8,660 g", e.Find("#hrd-basari").TextContent));
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hrd-onay-onay-kipi")));
    }

    [Fact]
    public void Onay_penceresi_acikken_her_dugme_ya_istek_atar_ya_DISABLED()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0.5");
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onay-kipi");
        foreach (var d in e.FindAll("button"))
        {
            var kilitli = d.HasAttribute("disabled");
            var tiklanabilir = d.Attributes.Any(a => a.Name.Contains("onclick", StringComparison.OrdinalIgnoreCase));
            Assert.True(kilitli || tiklanabilir, $"Ölü düğme: #{d.Id}");
        }
    }

    // ── Finansal sınırlar: 0 · eksi · bakiye = istenen · bakiye + 0,0001 ─────

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Sifir_ve_eksi_gram_onaya_GITMEZ_POST_ATILMAZ(string gram)
    {
        var e = Ekran();
        Gir(e, "#hrd-has", gram);
        e.Find("#hrd-kaydet").Click();

        e.WaitForElement("#alan-hata-hrd-has");
        Assert.Empty(e.FindAll("#hrd-onay-onay-kipi"));
        Assert.False(PostVar());
    }

    [Fact]
    public void Bakiyeye_ESIT_cikis_gecerlidir_hata_yok_onay_acilir()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "9.16");
        Assert.Empty(e.FindAll("#alan-hata-hrd-has"));
        e.Find("#hrd-kaydet").Click();

        e.WaitForElement("#hrd-onay-onay-kipi");
        Assert.Empty(e.FindAll("#alan-hata-hrd-has"));
    }

    [Fact]
    public void Bakiye_arti_0_0001_ANINDA_gercek_sebep_ve_mevcut_bakiye_ile_reddedilir()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "9.1601");

        // Canlı: Kaydet'e basılmadan mesaj çıkar ve GERÇEK sebebi söyler.
        var mesaj = e.WaitForElement("#alan-hata-hrd-has").TextContent;
        Assert.Contains("Kalan hurdayı aşıyorsunuz", mesaj);
        Assert.Contains("9,160 g", mesaj);
        Assert.DoesNotContain(Sozlukler.Tr.Hurda.KilitBakiyeYok, mesaj);

        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#alan-hata-hrd-has");
        Assert.Empty(e.FindAll("#hrd-onay-onay-kipi"));
        Assert.False(PostVar());
    }

    // ── Q10-2 düzeltme (doğrulayıcı KRİTİK 1-2): 6 haneli has YUVARLANMAZ ────
    // Canlı durum: 10,005 g × K22 alışı → defterde has 9,164580. Eski ekran
    // bakiyeyi "9,165 g" gösterip 9,165'i "9,165 g mevcut" diyerek reddediyordu.

    [Fact]
    public void Alti_haneli_bakiye_kart_ipucu_ve_asim_mesajinda_YUVARLANMADAN_yazilir()
    {
        bakiyeHas = "9.16458";
        var e = Ekran();
        Assert.Contains("9,16458 g", e.Find("#hrd-bakiye-ALTIN").TextContent);
        Assert.Contains("9,16458 g", e.Find("#hrd-has-sinir").TextContent);
        Assert.DoesNotContain("9,165 g", e.Find("#hrd-bakiye-ALTIN").TextContent);

        // Kullanıcı kartta gördüğü değerin yuvarlanmışını yazar → aşım 0,00042.
        Gir(e, "#hrd-has", "9,165");
        var mesaj = e.WaitForElement("#alan-hata-hrd-has").TextContent;
        Assert.Contains("(9,16458 g", mesaj);
        Assert.DoesNotContain("9,165 g", mesaj);
    }

    [Fact]
    public void Alti_haneli_bakiyeye_ESIT_cikis_gecer_ozet_ve_govde_ayni_degeri_tasir()
    {
        bakiyeHas = "9.16458";
        var e = Ekran();
        Gir(e, "#hrd-has", "9,16458");
        Assert.Empty(e.FindAll("#alan-hata-hrd-has"));
        e.Find("#hrd-kaydet").Click();
        var ozet = e.WaitForElement("#hrd-onay-hedef").TextContent;
        Assert.Contains("9,16458 g", ozet);
        e.Find("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.True(PostVar()));
        Assert.Contains("\"hasGrami\":9.16458", istekler.Single(i => i.Metot == "POST").Govde);
    }

    [Fact]
    public void Alti_haneli_bakiye_arti_0_000001_reddedilir_POST_ATILMAZ()
    {
        bakiyeHas = "9.16458";
        var e = Ekran();
        Gir(e, "#hrd-has", "9,164581");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#alan-hata-hrd-has")));
        e.Find("#hrd-kaydet").Click();
        Assert.Empty(e.FindAll("#hrd-onay-onay-kipi"));
        Assert.False(PostVar());
    }

    [Fact]
    public void Onay_ozeti_gonderilen_4_haneli_grami_AYNEN_gosterir_kabul_dahil()
    {
        bakiyeHas = "9.16458";
        var e = Ekran();
        Gir(e, "#hrd-has", "9,1604");
        e.Find("#hrd-kaydet").Click();
        var ozet = e.WaitForElement("#hrd-onay-hedef").TextContent;
        // Bizim kaydımız ve (boş bırakılan) alıcı kabulü ikisi de 9,1604 g.
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(ozet, "9,1604 g").Count);
        Assert.DoesNotContain("9,160 g", ozet);
    }

    // ── Q10-2 düzeltme turu — K8: BASİT modda jargon 0 ───────────────────────
    // Canlı ölçüm (sikisma-olc.js, BASİT, 4 durum): K8 = 5 ("MUTABAKAT",
    // "mutabakat", "cari"). Ölçüm aracıyla AYNI desen (büyük/küçük harf duyarsız).
    private static readonly System.Text.RegularExpressions.Regex JargonDeseni = new(
        @"(?<![\p{L}])(cari|cariler|cariden|cariye|carinin|storno|vezne|mutabakat|pasife al|denetim izi)(?![\p{L}])",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private void DefterTahlilFarkliSatisla()
    {
        defterYaniti = """
            {"satirlar":[{"id":"h1","tur":"HURDA_SATIS","tarih":"2026-09-24T09:00:00Z","tlTutari":3400,"hasGrami":-0.5,
              "maden":"ALTIN","madenAdi":"Altın","cariId":null,"aciklama":null,"paraBirimi":"TRY","stornoSatiri":false,
              "stornolandi":false,"tahlilKaynakHareketId":null,"tahlilFarkiHasGram":-0.01}],
             "toplamAdet":1,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}
            """;
        fireNotuJson = System.Text.Json.JsonSerializer.Serialize(Kuyumcu.Api.Modules.Hurda.HurdaStokKapisi.FireKapaliMesaji);
    }

    [Fact]
    public void Basit_modda_hurda_ekrani_jargon_TASIMAZ_defter_tahlil_farki_ve_fire_notu_dahil()
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => true);
        DefterTahlilFarkliSatisla();
        var e = Ekran();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#hrd-defter")));
        // ZO-ISLEM-A 28.09: fire notu (geliştirici notu) ekranda YOK.
        Assert.Empty(e.FindAll("#hrd-fire-notu"));
        Gir(e, "#hrd-has", "0,5");
        Gir(e, "#hrd-kabul-has", "0,4");   // fark ≠ 0 → fark notu da çizilir

        var metin = e.Markup;
        var bulunan = JargonDeseni.Matches(System.Text.RegularExpressions.Regex.Replace(metin, "<[^>]+>", " "))
            .Select(m => m.Value).ToList();
        Assert.True(bulunan.Count == 0, "BASİT modda jargon: " + string.Join(", ", bulunan));
        Assert.Contains(HurdaOnaySozlugu.Tr.TahlilFarkiBasit, metin);
        Assert.Contains(HurdaOnaySozlugu.Tr.RamatciBasit, metin);
        // 28.09 (tasarım hakemi): uzun fark açıklaması ("defter satırı", "DEĞİLDİR")
        // yerine alıcının kabul alanında TEK sade cümle.
        Assert.DoesNotContain(HurdaOnaySozlugu.Tr.TahlilFarkiNotuBasit, metin);
        Assert.Contains(ZoIslemSozlugu.Tr.HurdaKabulIpucu, metin);
    }

    [Fact]
    public void Pro_modda_hurda_ekrani_PRO_metnini_korur_ozellik_silinmez()
    {
        DefterTahlilFarkliSatisla();
        var e = Ekran();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#hrd-defter")));
        var metin = e.Markup;
        Assert.Contains(Sozlukler.Tr.Hurda.TahlilFarki, metin);
        Assert.Contains(Sozlukler.Tr.Hurda.Ramatci, metin);
    }

    [Fact]
    public void Sozluk_basit_metinler_TR_EN_dolu_ve_jargonsuz()
    {
        foreach (var s in new[] { HurdaOnaySozlugu.Tr, HurdaOnaySozlugu.En })
            foreach (var d in new[] { s.TahlilFarkiBasit, s.TahlilFarkiNotuBasit, s.RamatciBasit, s.DegismezNotuBasit, s.DuzeltmeNotuBasit })
            {
                Assert.False(string.IsNullOrWhiteSpace(d));
                Assert.DoesNotMatch(JargonDeseni, d);
            }
        Assert.DoesNotMatch(JargonDeseni, Kuyumcu.Api.Modules.Hurda.HurdaStokKapisi.FireKapaliMesaji);
    }

    // ── Alan hatası değer düzeltilince kalkar ────────────────────────────────

    [Fact]
    public void Asim_mesaji_deger_bakiye_icine_duzeltilince_KALKAR()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "5000");
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#alan-hata-hrd-has");

        Gir(e, "#hrd-has", "0.5");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#alan-hata-hrd-has")));
    }

    [Fact]
    public void Kasa_zorunlu_mesaji_kasa_secilince_KALKAR()
    {
        kasaYaniti = """
            [{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":250000,"aktif":true},
             {"id":"ks2","ad":"Vitrin Kasa","paraBirimi":"TRY","bakiye":1000,"aktif":true}]
            """;
        var e = Ekran();
        Gir(e, "#hrd-has", "0.5");
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#alan-hata-hrd-kasa");
        Assert.Empty(e.FindAll("#hrd-onay-onay-kipi"));

        e.Find("#hrd-kasa").Click();
        e.WaitForElement("[role=option][data-deger='ks2']").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#alan-hata-hrd-kasa")));

        e.Find("#hrd-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains("Vitrin Kasa", e.Find("#hrd-onay-hedef").TextContent));
    }

    [Fact]
    public void Gerekce_mesaji_gerekce_yazilinca_KALKAR()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "5");
        Gir(e, "#hrd-kabul-has", "4.8");
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#alan-hata-hrd-tahlil-gerekce");

        e.Find("#hrd-tahlil-gerekce").Input("Rafineri tahlil raporu 2026/144 ile mutabakat farkı.");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#alan-hata-hrd-tahlil-gerekce")));
    }

    // ── Sözlük: TR+EN, düğme adı yer tutucudan ───────────────────────────────

    [Fact]
    public void Sozluk_TR_ve_EN_dolu_yer_tutuculari_esit()
    {
        foreach (var m in new[] { HurdaOnaySozlugu.Tr, HurdaOnaySozlugu.En })
        {
            foreach (var p in typeof(HurdaOnayMetinleri).GetProperties())
                Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(m)), p.Name);
            Assert.Contains("{0}", m.BakiyeAsildi);
            Assert.Contains("{0}", m.OnayDuzeltmeYolu);
        }
        Assert.NotEqual(HurdaOnaySozlugu.Tr.BakiyeAsildi, HurdaOnaySozlugu.En.BakiyeAsildi);
        Assert.False(Kuyumcu.Web.Bilesenler.OnayKipi.AyniFiil(HurdaOnaySozlugu.Tr.OnayDugmesi, "Vazgeç"));
    }

    // ── Q10-2 v2 (doğrulayıcı KRİTİK): onaylanan gövde DONDURULUR ─────────────

    private static System.Text.Json.JsonElement Govde(string ham) =>
        System.Text.Json.JsonDocument.Parse(ham).RootElement;

    [Fact]
    public void Onay_acikken_kur_degisirse_POST_ONAYLANAN_fiyat_tahsil_ve_kurId_ile_gider()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0.5");
        e.Find("#hrd-kaydet").Click();
        var ozet = e.WaitForElement("#hrd-onay-hedef").TextContent;
        Assert.Contains("6.800", ozet);
        Assert.Contains("3.400", ozet);

        // Pencere AÇIKKEN canlı kur güncellenir (10 sn yoklamanın bUnit karşılığı).
        var canli = Services.GetRequiredService<CanliKurServisi>();
        e.InvokeAsync(() => canli.Uygula(new CanliKurServisi.KurAnlik("kur-2", 7_000m, 7_050m, 34m, 37m, 43.5m)));
        // Arka plandaki form alanı yeni kuru gösterir (ekranın geri kalanı canlı kalır)…
        e.WaitForAssertion(() => Assert.Contains("3.500", e.Find("#hrd-tutar").TextContent));
        // …ama onay özeti kullanıcının gördüğü değerde KALIR.
        Assert.Equal(ozet, e.Find("#hrd-onay-hedef").TextContent);

        e.Find("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.True(PostVar()));
        var g = Govde(istekler.Single(i => i.Metot == "POST").Govde);
        Assert.Equal(6800m, g.GetProperty("birimFiyatTlHasGram").GetDecimal());
        Assert.Equal(3400m, g.GetProperty("tahsilEdilenTl").GetDecimal());
        Assert.Equal("kur-1", g.GetProperty("kurSnapshotId").GetString());
        Assert.Equal(0.5m, g.GetProperty("hasGrami").GetDecimal());
        Assert.Equal("ALTIN", g.GetProperty("maden").GetString());
        Assert.Equal("ks1", g.GetProperty("kasaId").GetString());
        Assert.Equal("NAKIT", g.GetProperty("odemeTuru").GetString());
    }

    [Fact]
    public void Vazgec_sonrasi_yeniden_Kaydet_GUNCEL_kurla_YENI_ozet_ve_govde_uretir()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0.5");
        e.Find("#hrd-kaydet").Click();
        Assert.Contains("6.800", e.WaitForElement("#hrd-onay-hedef").TextContent);

        var canli = Services.GetRequiredService<CanliKurServisi>();
        e.InvokeAsync(() => canli.Uygula(new CanliKurServisi.KurAnlik("kur-2", 7_000m, 7_050m, 34m, 37m, 43.5m)));
        e.Find("#hrd-onay-vazgec").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hrd-onay-onay-kipi")));
        Assert.False(PostVar());

        e.Find("#hrd-kaydet").Click();
        var ozet = e.WaitForElement("#hrd-onay-hedef").TextContent;
        Assert.Contains("7.000", ozet);
        Assert.Contains("3.500", ozet);
        e.Find("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.True(PostVar()));
        var g = Govde(istekler.Single(i => i.Metot == "POST").Govde);
        Assert.Equal(7000m, g.GetProperty("birimFiyatTlHasGram").GetDecimal());
        Assert.Equal(3500m, g.GetProperty("tahsilEdilenTl").GetDecimal());
        Assert.Equal("kur-2", g.GetProperty("kurSnapshotId").GetString());
    }
}
