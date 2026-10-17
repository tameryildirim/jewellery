using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET UX-4c — EKRAN TARAFI (madde 13 · 10 · 11 · 12 + Q4)
//
//   13 — `catch (ApiHata) { liste = []; }` kalıbı kalktı: sunucu hatası
//        kullanıcıya "kayıt yok" diye gösterilemez (UX-S28 · Anayasa md.1).
//        Boş durum YALNIZ istek başarılı ve liste gerçekten boşken çıkar.
//   10 — Emanet: cari seçici aramaya çevrildi (51. müşteri seçilebiliyor),
//        çıkış modalı ayar+gram alıyor, ayar listesi TEK KAYNAKTAN.
//   11 — Sayım: kapanış sonucu görünür, "İptal" onay ister, tek şube otomatik.
//   12 — Kullanıcı: şube atanabiliyor, pasif kullanıcı tek tıkla aktifleşiyor,
//        istemci şifre kuralı sunucununkiyle EŞİT.
//   Q4 — Panel "Net Kasa" kartı artık ne olduğunu söylüyor.
//
// Ölçülen şey "metin var mı" değil: hangi uca HANGİ GÖVDEYLE gidildiği ve
// hata durumunda kullanıcının NE GÖRDÜĞÜ.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UX4cEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    /// <summary>Bu parçayı içeren her yol 500 döner (madde 13 senaryoları).</summary>
    private string? hataliYolParcasi;

    /// <summary>Cari listesi testten teste değişir (tek cari / 50'lik sayfa).</summary>
    private Func<string, string> cariYaniti = _ => """[{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","tlBakiye":0,"hasGramBakiye":0}]""";

    /// <summary>Şube listesi (tek şube / iki şube).</summary>
    private string subeYaniti = """[{"id":"s1","ad":"Merkez","aktif":true}]""";

    public UX4cEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(
            new SahteApi(istekler, () => hataliYolParcasi, y => cariYaniti(y), () => subeYaniti))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();          // P-37b: karşılama "son işlemler" kutucuğu
        Services.AddScoped<MenuTercihServisi>();     // P-37b: kullanıcı bazlı kutucuk düzeni
        Services.AddScoped<Microsoft.Extensions.Configuration.IConfiguration>(
            _ => new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        Services.AddScoped<HavaServisi>();           // P-37b: hava KAPALI (boş yapılandırma)
    }

    private sealed class SahteApi(
        List<(string, string, string)> kayit,
        Func<string?> hataliParca,
        Func<string, string> cariler,
        Func<string> subeler) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var tam = istek.RequestUri!.PathAndQuery;
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, tam, govde));

            if (hataliParca() is { } p && yol.Contains(p))
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"message":"Sunucu hatası: kasa listesi okunamadı"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            var metot = istek.Method.Method;
            var g = yol switch
            {
                // PAKET 41: /banka dekont formu MASAK kapısına bağlandı.
                var y when y.Contains("/masak/esikler") =>
                    """{"kimlikEsigi":185000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000,"ekBelgeEsigi":20000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var y when y.EndsWith("/kur/kaynaklar") =>
                    """{"kaynaklar":["ALTINKAYNAK"],"varsayilan":"ALTINKAYNAK","secili":"ALTINKAYNAK"}""",
                var y when y.EndsWith("/sayim") && metot == "POST" => Oturum,
                var y when y.EndsWith("/sayim") => """[{"id":"sy1","ad":"Eylül sayımı","kapsamTur":"TUMU","durum":"ACIK"}]""",
                var y when y.EndsWith("/kapat") => KapatYaniti,
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":100,"aktif":true}]""",
                var y when y.EndsWith("/bankalar") => """{"hesaplar":[{"id":"bh1","ad":"Ziraat","iban":"TR11","paraBirimi":"TRY","bakiye":500,"aktif":true}]}""",
                var y when y.Contains("/bankalar/") => """{"hareketler":[]}""",
                var y when y.Contains("/faturalar") => """{"faturalar":[]}""",
                var y when y.EndsWith("/subeler") => subeler(),
                var y when y.EndsWith("/kullanicilar") => """
                    [{"id":"u1","ad":"Test","soyad":"Kullanıcı","email":"t@ornek.com","rol":"SAHIP","aktif":true,"sonGiris":null,"subeId":"s1"},
                     {"id":"u2","ad":"Pasif","soyad":"Tezgâhtar","email":"p@ornek.com","rol":"TEZGAHTAR","aktif":false,"sonGiris":null,"subeId":null}]
                    """,
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-16T09:00:00Z","ayarFiyatlari":[]}""",
                // TEMA TEMİZLİĞİ 17.10: Yıldız'da özgün emanet gövdesine geçiş listedeki bir emanet satırıdır → bir giriş (h1).
                var y when y.Contains("/emanet/") => """
                    {"cariId":"c1","mevcutHasGramBakiye":12.5,
                     "hareketler":[{"id":"h1","tur":"EMANET_GIRIS","tarih":"2026-09-10T10:00:00Z","hasGrami":12.5,
                                    "aciklama":null,"stornoKaynakId":null,"fisId":null,"maden":"ALTIN"}]}
                    """,
                var y when y.Contains("/raporlar/gunluk") =>
                    """{"satisTl":1000,"alisTl":200,"hurdaTl":100,"netHasGram":5,"kasalar":[]}""",
                var y when y.Contains("/raporlar/aylik") => """{"gunler":[]}""",
                var y when y.Contains("/cari") => cariler(tam),
                var y when y.Contains("/sayim/") => Oturum,
                var y when y.EndsWith("/reyonlar") => "[]",
                var y when y.Contains("/urun-kategorileri") => "[]",
                var y when y.Contains("/kur/") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }

        private const string KapatYaniti = """
            {"uygulanan":2,"atlanan":1,"detay":
              {"id":"sy1","ad":"Eylül sayımı","subeId":"s1","kapsamTur":"TUMU","durum":"KAPANDI",
               "toplamSatir":3,"sayilan":2,"sayilmayan":1,"farkli":1,"eksikAdet":1,"fazlaAdet":0,
               "netFarkHas":-9.16,"hareketli":0,"satirlar":[]}}
            """;

        private const string Oturum = """
            {"id":"sy1","ad":"Eylül sayımı","subeId":"s1","kapsamTur":"TUMU","durum":"ACIK",
             "toplamSatir":3,"sayilan":2,"sayilmayan":1,"farkli":1,"eksikAdet":1,"fazlaAdet":0,
             "netFarkHas":-9.16,"hareketli":0,"satirlar":[]}
            """;
    }

    private void OturumAc(string rol = "SAHIP") =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1"));

    private static string? Alan(string govde, string ad)
    {
        var e = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    private static bool AlanGonderildi(string govde, string ad) =>
        System.Text.Json.JsonDocument.Parse(govde).RootElement.TryGetProperty(ad, out _);

    // ═════════════════════════════════════════════════════════════════════════
    // MADDE 13 — hata yutma: 500'de hata bandı görünür, BOŞ DURUM görünmez
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void M13_Kasalar_500_donunce_hata_bandi_gorunur_bos_durum_GORUNMEZ()
    {
        hataliYolParcasi = "/kasalar";
        var e = Render<Kuyumcu.Web.Pages.Kasalar>().AyrintiAc();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#hata-bandi")));
        Assert.Contains("kasa listesi okunamadı", e.Find("#hata-bandi").TextContent);
        // ⚠ Asıl kural bu: sunucu hatası "kasa bulunamadı" diye SUNULAMAZ.
        Assert.Empty(e.FindAll(".bos-durum"));
    }

    [Fact]
    public void M13_Banka_500_donunce_hata_bandi_gorunur_bos_durum_GORUNMEZ()
    {
        hataliYolParcasi = "/bankalar";
        var e = Render<Kuyumcu.Web.Pages.Banka>().AyrintiAc();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#hata-bandi")));
        Assert.Empty(e.FindAll(".bos-durum"));
    }

    [Fact]
    public void M13_Faturalar_500_donunce_hata_bandi_gorunur_bos_durum_GORUNMEZ()
    {
        hataliYolParcasi = "/faturalar";
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#hata-bandi")));
        Assert.Empty(e.FindAll(".bos-durum"));
    }

    [Fact]
    public void M13_Kullanicilar_500_donunce_hata_bandi_gorunur_bos_durum_GORUNMEZ()
    {
        OturumAc();
        hataliYolParcasi = "/kullanicilar";
        var e = Render<Kuyumcu.Web.Pages.Kullanicilar>();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#hata-bandi")));
        Assert.Empty(e.FindAll(".bos-durum"));
    }

    [Fact]
    public void M13_Cariler_500_donunce_hata_bandi_gorunur_bos_durum_GORUNMEZ()
    {
        OturumAc();
        hataliYolParcasi = "/cari";
        var e = Render<Kuyumcu.Web.Pages.Cariler>();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#hata-bandi")));
        Assert.Empty(e.FindAll(".bos-durum"));
    }

    [Fact]
    public void M13_Istek_BASARILI_ve_liste_bossa_bos_durum_CIKAR()
    {
        // UX-S28'in ikinci yarısı: boş durum kaldırılmadı, KOŞULU düzeltildi.
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();

        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".bos-durum")));
        Assert.Empty(e.FindAll("#hata-bandi"));
    }

    [Fact]
    public void M13_Hata_yutan_kalip_HICBIR_sayfada_KALMADI()
    {
        // Envanter kanıtı: `catch (ApiHata) { <liste> = []; }` kalıbı tek satır
        // bile kalırsa bu test kırmızıya döner (kalıbın geri sızmasına karşı kapı).
        var kokDizin = Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "Pages");
        var ihlaller = Directory.GetFiles(kokDizin, "*.razor")
            .SelectMany(d => File.ReadAllLines(d).Select((s, i) => (Dosya: Path.GetFileName(d), No: i + 1, Satir: s)))
            .Where(x => System.Text.RegularExpressions.Regex.IsMatch(
                x.Satir, @"catch\s*\(ApiHata\)\s*\{\s*\w+\s*=\s*\[\]\s*;\s*\}"))
            .Select(x => $"{x.Dosya}:{x.No}")
            .ToList();

        Assert.True(ihlaller.Count == 0,
            "UX-S28 ihlali — hata sessizce boş listeye çevriliyor: " + string.Join(", ", ihlaller));
    }

    [Fact]
    public void M13_Sureli_basari_bandinin_KOPYA_metotlari_silindi()
    {
        // Süre BasariBandi bileşenindedir; sayfalarda Task.Delay(3000) kopyası kalmaz.
        var kokDizin = Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "Pages");
        var kopyalar = Directory.GetFiles(kokDizin, "*.razor")
            .Where(d => File.ReadAllText(d).Contains("Task.Delay(3000)")
                     || File.ReadAllText(d).Contains("Task.Delay(3000,"))
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(kopyalar.Count == 0, "Kopya süreli bant metodu: " + string.Join(", ", kopyalar));
        Assert.Contains("SureliMs", CssKaynak.Metin("Kuyumcu.Web", "Bilesenler", "BasariBandi.razor"));
    }

    [Fact]
    public void M13_BasariBandi_SURELI_ise_kendi_kendine_KAYBOLUR()
    {
        // Süre artık bileşenin İÇİNDE. Zamanlayıcı kaldırılırsa bant hiç
        // kaybolmaz ve kullanıcı eski bir "kaydedildi" mesajını yeni işlemin
        // sonucu sanar — bu test o gerilemeyi yakalar.
        var e = Render<Kuyumcu.Web.Bilesenler.BasariBandi>(p => p
            .Add(x => x.Mesaj, "Kaydedildi")
            .Add(x => x.SureliMs, 60)
            .Add(x => x.Id, "basari-bandi"));

        Assert.NotNull(e.Find("#basari-bandi"));
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#basari-bandi")),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void M13_AYNI_mesaj_JETON_artinca_YENIDEN_gorunur()
    {
        // İki kez üst üste aynı işlemi kaydeden kullanıcı ikinci onayı da
        // görmeli. Dizge değişmediği için bunu yalnız jeton sağlar; jeton
        // kontrolü düşerse ikinci kayıt SESSİZ kalır (UX-S27 ihlali).
        var e = Render<Kuyumcu.Web.Bilesenler.BasariBandi>(p => p
            .Add(x => x.Mesaj, "Kaydedildi")
            .Add(x => x.SureliMs, 60)
            .Add(x => x.Jeton, 1)
            .Add(x => x.Id, "basari-bandi"));

        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#basari-bandi")), TimeSpan.FromSeconds(5));

        e.Render(p => p
            .Add(x => x.Mesaj, "Kaydedildi")
            .Add(x => x.SureliMs, 60)
            .Add(x => x.Jeton, 2)
            .Add(x => x.Id, "basari-bandi"));

        Assert.NotNull(e.Find("#basari-bandi"));
    }

    [Fact]
    public void M13_BasariBandi_SURESIZ_ise_KALICI_kalir()
    {
        var e = Render<Kuyumcu.Web.Bilesenler.BasariBandi>(p => p
            .Add(x => x.Mesaj, "Kaydedildi")
            .Add(x => x.Id, "basari-bandi"));

        Thread.Sleep(120);
        e.Render();
        Assert.NotNull(e.Find("#basari-bandi"));
    }

    [Fact]
    public void M13_b_Faturalar_ekraninda_SATIR_ICI_stil_kalmadi()
    {
        // Dalga 1 doğrulayıcısının taşınan bulgusu (b).
        var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Pages", "Faturalar.razor");

        Assert.DoesNotContain("style=\"flex:1\"", kaynak);
        Assert.Contains(".esnek { flex: 1; }", CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css"));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // MADDE 10 — Emanet
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>60 carili kiracı: sunucu sayfa başına 50 döndürür, q ile daraltılır.</summary>
    private void AltmisCari()
    {
        cariYaniti = yol =>
        {
            var q = System.Web.HttpUtility.ParseQueryString(new Uri("http://x" + yol).Query)["q"];
            var hepsi = Enumerable.Range(1, 60).Select(i => (Id: $"c{i}", Ad: $"Müşteri {i:00}")).ToList();
            var suzulmus = string.IsNullOrEmpty(q)
                ? hepsi.Take(50).ToList()
                : hepsi.Where(c => c.Ad.Contains(q, StringComparison.OrdinalIgnoreCase)).Take(50).ToList();
            return "[" + string.Join(",", suzulmus.Select(c => $$"""{"id":"{{c.Id}}","ad":"{{c.Ad}}"}""")) + "]";
        };
    }

    [Fact]
    public void M10_Elli_birinci_musteri_ARAMAYLA_secilebilir()
    {
        // ESKİDEN: "/cari" limitsiz çağrılıyordu, sunucu varsayılanı 50'ydi →
        // 51. müşterinin emaneti bu ekrandan HİÇ alınamıyordu.
        AltmisCari();
        var e = Render<Kuyumcu.Web.Pages.Emanet>().EmanetSatiriAc("h1");   // TEMA TEMİZLİĞİ 17.10: Yıldız yolu (satıra bas)

        // Sayfa açılışında 51 listede YOK (sunucu sınırı).
        e.WaitForAssertion(() => Assert.DoesNotContain("Müşteri 51", SeciciYardim.ListeMetni(e, "e-cari")));

        istekler.Clear();
        e.Find("#e-cari-ara").Input("Müşteri 51");

        e.WaitForAssertion(() => Assert.Contains("Müşteri 51", SeciciYardim.ListeMetni(e, "e-cari")));
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("q=M") && i.Yol.Contains("limit=50"));

        // Ve seçilebiliyor: emanet defteri o cari için çekiliyor.
        istekler.Clear();
        SeciciYardim.Sec(e, "e-cari", "c51");
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("/emanet/c51"));
    }

    [Fact]
    public void M10_Liste_sunucu_sinirina_degince_DARALTIN_uyarisi_basilir()
    {
        // UX-S29 — sessiz kırpma yok.
        AltmisCari();
        var e = Render<Kuyumcu.Web.Pages.Emanet>().EmanetSatiriAc("h1");   // TEMA TEMİZLİĞİ 17.10: Yıldız yolu (satıra bas)

        // EMANET-1 (doğrulayıcı 28.09): teknik "sunucu sınırı (50 kayıt)" dili sade metne döndü;
        // sözleşme aynı — sınır söylenir ("tüm müşteriler görünmüyor") + daraltma yolu ("adını yazıp arayın").
        e.WaitForAssertion(() => Assert.Equal(Kuyumcu.Core.I18n.EmanetSadeSozlugu.Tr.AramaSinir, e.Find("#e-cari-sinir").TextContent.Trim()));
        Assert.Contains("görünmüyor", e.Find("#e-cari-sinir").TextContent);
        Assert.Contains("arayın", e.Find("#e-cari-sinir").TextContent);
    }

    [Fact]
    public void M10_Cikista_22_ayar_10_gram_govdeye_9_16_HAS_GRAM_olarak_gider()
    {
        // UX-S21 — dönüşümü sistem biliyor; kuyumcudan kafadan has çevirisi istenmez.
        var e = Render<Kuyumcu.Web.Pages.Emanet>().EmanetSatiriAc("h1");   // TEMA TEMİZLİĞİ 17.10: Yıldız yolu (satıra bas)
        SeciciYardim.Sec(e, "e-cari", "c1");
        e.WaitForAssertion(() => Assert.Contains("12,5000", e.Markup));   // P-57: ekran tr-TR

        e.Find("#e-cikis-ac").Click();   // EMANET-1: metin "Emanet iade et"; id aynı
        SeciciYardim.Sec(e, "c-ayar", "K22");
        e.Find("#c-gram").Input("10");

        // Hesaplanan değer ekranda da yazar.
        Assert.Contains("9,1600", e.Find("#c-hesaplanan").TextContent);   // P-57: ekran tr-TR; gövde aşağıda Invariant

        istekler.Clear();
        e.FindAll("button").First(b => b.TextContent.Contains("Kaydet")).Click();

        var istek = Assert.Single(istekler, i => i.Metot == "POST");
        Assert.Contains("/emanet/c1/cikis", istek.Yol);
        Assert.Equal(9.16m, decimal.Parse(Alan(istek.Govde, "hasGrami")!,
            System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void M10_Ayar_listesi_UC_EKRANDA_TEK_KAYNAKTAN_beslenir()
    {
        // Eskiden dizi üç ekranda elle yazılıydı ve İÇERİKLERİ FARKLIYDI:
        // emanette K995/G999 vardı, ürün ve POS ekranlarında yoktu.
        foreach (var dosya in new[] { "Emanet.razor", "Urunler.razor", "Islemler.razor" })
        {
            var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Pages", dosya);
            Assert.Contains("Kuyumcu.Core.Domain.Ayarlar.EkranSecenekleri", kaynak);
            Assert.DoesNotContain("[\"K8\", \"K14\"", kaynak);
        }

        var e = Render<Kuyumcu.Web.Pages.Emanet>().EmanetSatiriAc("h1");   // TEMA TEMİZLİĞİ 17.10: Yıldız yolu (satıra bas)
        e.Find("#e-giris-ac").Click();   // EMANET-1: metin "Emanet al"; id aynı
        // PAKET S3-5 · UX-S31: seçeneğin DEĞERİ ayar kodudur (K995 · G999 —
        // gövdeye kod gider), GÖRÜNEN metni tek kaynaktaki ayar adıdır.
        var degerler = SeciciYardim.Secenekler(e, "e-ayar").Select(o => o.Deger()).ToList();
        Assert.Contains("K995", degerler);
        Assert.Contains("G999", degerler);
        var secenekler = SeciciYardim.ListeMetni(e, "e-ayar");
        Assert.Contains(Kuyumcu.Core.Domain.Ayarlar.Bul("K995")!.Ad, secenekler);
        Assert.Contains(Kuyumcu.Core.Domain.Ayarlar.Bul("G999")!.Ad, secenekler);
        Assert.Equal(
            Kuyumcu.Core.Domain.Ayarlar.EkranSecenekleri.Length,
            SeciciYardim.Secenekler(e, "e-ayar").Count);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // MADDE 11 — Sayım
    // ═════════════════════════════════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Sayim> AcikOturum()
    {
        // TEMA TEMİZLİĞİ 17.10: Yıldız açık sayımı kendiliğinden açar (sy1); "Tüm ayrıntılar" özgün gövdeyi o sayımla
        // açar → "← Sayım" (sy-listeye-don) oturum listesine döner (#sy-yeni ve "→" düğmeleri orada).
        var e = Render<Kuyumcu.Web.Pages.Sayim>().AyrintiAc();
        e.WaitForElement("#sy-listeye-don").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sy-yeni")));
        return e;
    }

    [Fact]
    public void M11_Kapanista_UYGULANAN_ve_ATLANAN_sayilari_basari_bandinda_yazar()
    {
        // ESKİDEN: yanıtın yalnız Detay'ı alınıyor, iki sayı çöpe atılıyordu —
        // hem de geri alınamaz bir işlemde.
        var e = AcikOturum();
        e.FindAll("button").First(b => b.TextContent.Contains("→")).Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sy-kapat")));

        e.Find("#sy-kapat").Click();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#basari-bandi")));
        var metin = e.Find("#basari-bandi").TextContent;
        Assert.Contains("satır stoğa uygulandı", metin);
        Assert.Contains("satır atlandı", metin);
    }

    [Fact]
    public void M11_Iptal_dugmesi_ONCE_ONAY_MODALI_acar_POST_ATMAZ()
    {
        var e = AcikOturum();
        e.FindAll("button").First(b => b.TextContent.Contains("→")).Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sy-iptal")));

        istekler.Clear();
        e.Find("#sy-iptal").Click();

        // Onay modalı açıldı; kaç satır sayıldığı yazıyor ve GERİ ALINAMAZ deniyor.
        Assert.Contains("2 / 3", e.Find("#sy-iptal-sayilan").TextContent);
        Assert.Contains("GERİ ALINAMAZ", e.Markup);
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");

        // Onaylanınca iptal POST'u gider.
        e.Find("#sy-iptal-onayla").Click();
        Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("/iptal"));
    }

    [Fact]
    public void M11_Tek_subede_sube_secici_GIZLI_ve_govdede_subeId_DOLU()
    {
        // UX-S22 — tek seçenek varsa seçici gösterilmez.
        var e = AcikOturum();
        e.Find("#sy-yeni").Click();

        Assert.Empty(e.FindAll("#sy-sube"));
        Assert.Contains("Merkez", e.Find("#sy-sube-tek").TextContent);

        e.Find("#sy-ad").Input("Eylül sayımı");   // PAKET GOC-C: ortak Metin
        // ⚠ Seçici gizlenince SubeId boş kalırsa "Aç" düğmesi KİLİTLİ kalır ve
        // kullanıcı neden kilitli olduğunu göremez — asıl tuzak budur.
        Assert.False(e.Find("#sy-ac").HasAttribute("disabled"));

        istekler.Clear();
        e.Find("#sy-ac").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "POST");
        Assert.Equal("s1", Alan(istek.Govde, "subeId"));
    }

    [Fact]
    public void M11_Iki_subede_secici_GERI_GELIR()
    {
        subeYaniti = """[{"id":"s1","ad":"Merkez","aktif":true},{"id":"s2","ad":"Şube 2","aktif":true}]""";
        var e = AcikOturum();
        e.Find("#sy-yeni").Click();

        Assert.NotNull(e.Find("#sy-sube"));
        Assert.Empty(e.FindAll("#sy-sube-tek"));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // MADDE 12 — Kullanıcı şubesi, aktifleştirme, şifre kuralı
    // ═════════════════════════════════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Kullanicilar> KullaniciEkrani()
    {
        OturumAc();
        var e = Render<Kuyumcu.Web.Pages.Kullanicilar>();
        e.WaitForAssertion(() => Assert.Contains("Pasif", e.Markup));
        return e;
    }

    [Fact]
    public void M12_Iki_subede_k_sube_secilince_govdede_subeId_gider()
    {
        subeYaniti = """[{"id":"s1","ad":"Merkez","aktif":true},{"id":"s2","ad":"Şube 2","aktif":true}]""";
        var e = KullaniciEkrani();
        e.FindAll("button").First(b => b.TextContent.Contains("Yeni kullanıcı")).Click();

        e.Find("#k-ad").Input("Yeni");
        e.Find("#k-soyad").Input("Çalışan");
        e.Find("#k-eposta").Input("y@ornek.com");
        e.Find("#k-sifre").Input("Parola12");
        SeciciYardim.Sec(e, "k-sube", "s2");

        istekler.Clear();
        e.FindAll("button").First(b => b.TextContent.Trim() == "Kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "POST");
        Assert.Equal("s2", Alan(istek.Govde, "subeId"));
    }

    [Fact]
    public void M12_Tek_subede_secici_GIZLI_ama_govde_yine_DOLU()
    {
        var e = KullaniciEkrani();
        e.FindAll("button").First(b => b.TextContent.Contains("Yeni kullanıcı")).Click();

        Assert.Empty(e.FindAll("#k-sube"));
        Assert.Contains("Merkez", e.Find("#k-sube-tek").TextContent);

        e.Find("#k-ad").Input("Yeni");
        e.Find("#k-soyad").Input("Çalışan");
        e.Find("#k-eposta").Input("y@ornek.com");
        e.Find("#k-sifre").Input("Parola12");

        istekler.Clear();
        e.FindAll("button").First(b => b.TextContent.Trim() == "Kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "POST");
        Assert.Equal("s1", Alan(istek.Govde, "subeId"));
    }

    [Fact]
    public void M12_Listede_SUBE_kolonu_var()
    {
        var e = KullaniciEkrani();

        Assert.Contains("Şube", e.Find("table thead").TextContent);
        // Şubesi olan kullanıcıda şube ADI yazar, olmayanda "—".
        Assert.Contains("Merkez", e.Find("table tbody").TextContent);
    }

    [Fact]
    public void M12_aaaaaaaa_sifresinde_KAYDET_KILITLI_kural_listesi_gorunur()
    {
        // İstemci yalnız uzunluğa bakıyordu; sunucu harf+rakam istiyordu →
        // kullanıcı kaydediyor, 400 yiyordu.
        var e = KullaniciEkrani();
        e.FindAll("button").First(b => b.TextContent.Contains("Yeni kullanıcı")).Click();

        e.Find("#k-ad").Input("Yeni");
        e.Find("#k-soyad").Input("Çalışan");
        e.Find("#k-eposta").Input("y@ornek.com");
        e.Find("#k-sifre").Input("aaaaaaaa");

        var kaydet = e.FindAll("button").First(b => b.TextContent.Trim() == "Kaydet");
        Assert.True(kaydet.HasAttribute("disabled"));
        Assert.Contains("En az bir rakam", e.Find("#k-sifre-kural").TextContent);

        e.Find("#k-sifre").Input("Parola12");
        Assert.False(e.FindAll("button").First(b => b.TextContent.Trim() == "Kaydet").HasAttribute("disabled"));
    }

    [Fact]
    public void M12_Pasif_kullanici_AKTIFLESTIR_dugmesiyle_geri_acilir()
    {
        // UX-S25 — pasife alma tek tıkla yapılabiliyorken tersi gizliydi.
        var e = KullaniciEkrani();

        istekler.Clear();
        e.Find("#k-aktiflestir-u2").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "PATCH");
        Assert.Contains("/kullanicilar/u2", istek.Yol);
        Assert.Equal("True", Alan(istek.Govde, "aktif"));
        // PATCH sözleşmesi: gönderilmeyen alan KORUNUR — ad/rol/şube yollanmaz.
        Assert.False(AlanGonderildi(istek.Govde, "ad"));
        Assert.False(AlanGonderildi(istek.Govde, "subeId"));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Q4 — Panel kartı (mühürlü karar: A)
    // ═════════════════════════════════════════════════════════════════════════

    // TEMA TEMİZLİĞİ 17.10: Q4_Panel_karti_NE_OLDUGUNU_soyler kaldırıldı — Panel'in eski karşılama + kutucuk ızgarası gövdesi (StatKart "Günün nakit akışı" + formül) bilerek kaldırıldı; Panel artık yalnız YildizBugun (tuvalde bu kart yok).
    // TEMA TEMİZLİĞİ 17.10: Q4_Panel_grafigi_veri_yok_ile_UC_CEVAP_VERMEDI_yi_ayirir kaldırıldı — aylık grafik kutucuğu (#panel-grafik-hata) eski kutucuk ızgarasıyla birlikte kaldırıldı; Panel artık yalnız YildizBugun.
}
