using System.Text.Json;
using Bunit;
using Kuyumcu.Contracts.Bilesenler.MasakFormu;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-2 — satış dışı para formlarında MASAK kapısı (tasarım hakemi KRİTİK, 10.10)
//   /cariler Tahsilat çekmecesinde 250.000 ₺ → sunucu MASAK 400 ("masak.aciklama 20 karakter"), çekmecede kimlik
//   alanı yoktu (çıkışsız); tutar 25.000'e inince eski hata kutusu duruyordu.
// Ölçülen: (a) eşik üstünde ORTAK MasakFormu + Kaydet kilidi ve sebebi, (b) yukarı-aşağı-yukarı yeniden
// değerlendirme (eski form/ret cümlesi kalkar, beyan kaybolmaz), (c) ham "masak.aciklama" basılmaz,
// gövde: eşik altında `masak` YOK, üstünde VAR (JSON ayrıştırılarak). Sınırlar: 199.999,99 / 200.000 / 250.000 / 370.000.
// Eşik SAYILARI sahte sunucudan gelir (/masak/esikler) — istemciye gömülü değildir.
// ⚠ Görünürlük/sığma bUnit ile KANITLANMAZ — gerçek Chrome ölçümü rapordadır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Saf: ParaMasakKapisi kuralı (sahte eşik/gereksinim kaynağıyla).</summary>
public sealed class YasalKapi2SafTestleri
{
    internal static readonly Esikler Esik = new(370_000m, 200_000m, 0m, 0m, 20, ["TC_KIMLIK", "PASAPORT"])
    { KimlikIstenenEnDusukTutar = 200_000m };

    /// <summary>Sunucunun kademe kuralının aynası (yalnız TEST fikstürü): ölçü = tutar + müşterinin bugünkü toplamı.</summary>
    internal static MasakGereksinim Gereksinim(decimal tutar, decimal bugunToplam = 0m)
    {
        var olcu = tutar + bugunToplam;
        return olcu >= 370_000m ? new("KIMLIK", true, true, false, false, 20) { OlcuTutarTl = olcu, IslemTutarTl = tutar }
             : olcu >= 200_000m ? new("AMAC_BEYANI", true, true, false, false, 20) { OlcuTutarTl = olcu, IslemTutarTl = tutar }
             : new("YOK", false, false, false, false, 20) { OlcuTutarTl = olcu, IslemTutarTl = tutar };
    }

    internal static void Doldur(MasakFormu.MasakBilgi m)
    {
        m.BelgeTuru = "TC_KIMLIK"; m.BelgeNo = "10000000146"; m.AdSoyad = "Ayşe Demir";
        m.IslemAmaci = "Borç kapama"; m.Aciklama = "Önceki bilezik alışının borcunu kapatıyor";
    }

    private static ParaMasakKapisi Kapi(Func<decimal, string?, MasakGereksinim>? g = null, List<(decimal, string?)>? sorular = null) =>
        new(() => Task.FromResult<Esikler?>(Esik),
            (t, c) => { sorular?.Add((t, c)); return Task.FromResult((g ?? ((x, _) => Gereksinim(x)))(t, c)); });

    [Theory]
    [InlineData("199999.99", false)]
    [InlineData("200000", true)]
    [InlineData("250000", true)]
    [InlineData("370000", true)]
    public async Task YK2_S01_sinir_tablosu_esik_ustu_form_ve_govde(string tutar, bool gerekli)
    {
        var t = decimal.Parse(tutar, System.Globalization.CultureInfo.InvariantCulture);
        var k = Kapi();
        await k.Ac(null);
        await k.Tazele(t, null);
        Assert.Equal(gerekli, k.Gerekli);
        Assert.Equal(gerekli, k.FormCizilir);
        Assert.Equal(gerekli, k.Govde is not null);
        Assert.Equal(gerekli, k.KilitSebebi("tr") is not null);   // boş formda kilit
        Doldur(k.Model);
        Assert.Null(k.KilitSebebi("tr"));                          // doldurunca açılır
    }

    [Fact]
    public async Task YK2_S02_yukari_asagi_yukari_ret_izi_ve_beyan_korunur()
    {
        var k = Kapi();
        await k.Ac("c1");
        await k.Tazele(250_000m, "c1");
        Assert.True(k.FormCizilir);
        k.Model.AdSoyad = "Ayşe Demir";
        // sunucu reddetti (form eksikti): alanlar açık, ham metin YOK, sebep kilitte
        Assert.True(await k.Reddet(new ApiHata(400, "MASAK: İşlem amacı belirtilmelidir · MASAK beyan açıklaması (masak.aciklama) en az 20 karakter olmalıdır (şu an 0)"), "tr"));
        Assert.Null(k.RedBandi);
        Assert.Equal(ZoIslemSozlugu.Tr.MasakKilitSebepBasit, k.KilitSebebi("tr"));
        // aşağı: form, kilit ve gövde kalkar
        await k.Tazele(25_000m, "c1");
        Assert.False(k.Gerekli); Assert.False(k.FormCizilir); Assert.Null(k.Govde); Assert.Null(k.KilitSebebi("tr"));
        // yukarı: geri gelir, yazılan ad kaybolmaz
        await k.Tazele(250_000m, "c1");
        Assert.True(k.FormCizilir);
        Assert.Equal("Ayşe Demir", k.Model.AdSoyad);
    }

    [Fact]
    public async Task YK2_S03_bugunku_toplam_esik_altinda_da_formu_acar_musteri_degisince_yeniden_sorulur()
    {
        var sorular = new List<(decimal, string?)>();
        var k = Kapi((t, c) => Gereksinim(t, c == "c1" ? 190_000m : 0m), sorular);
        await k.Ac("c1");
        await k.Tazele(25_000m, "c1");
        Assert.True(k.Gerekli);                 // 25.000 + bugün 190.000 = 215.000 → amaç kademesi
        Assert.Contains((25_000m, "c1"), sorular);
        await k.Tazele(25_000m, "c2");
        Assert.False(k.Gerekli);                // başka müşteri: toplam yok
        Assert.Contains((25_000m, "c2"), sorular);
    }

    [Fact]
    public async Task YK2_S04_form_tamken_ret_tek_sade_cumle_ham_anahtar_yok_tutar_inince_kalkar()
    {
        var k = Kapi();
        await k.Ac("c1");
        await k.Tazele(250_000m, "c1");
        Doldur(k.Model);
        await k.Reddet(new ApiHata(400, "MASAK: uyruk kodu geçersiz ('XX'); pasaporttaki 3 harfli ülke kodunu yazın (masak.uyruk)"), "tr");
        var bant = Assert.IsType<string>(k.RedBandi);
        Assert.StartsWith("Kimlik bilgileri kabul edilmedi", bant, StringComparison.Ordinal);
        Assert.DoesNotContain("masak.", bant, StringComparison.Ordinal);
        Assert.DoesNotContain("MASAK:", bant, StringComparison.Ordinal);
        await k.Tazele(199_999.99m, "c1");
        Assert.Null(k.RedBandi);
        await k.Tazele(250_000m, "c1");
        Assert.NotNull(k.RedBandi);             // iz silinmez: tutar geri çıkınca geri gelir
    }

    [Fact]
    public void YK2_S05_sade_metin_gomulu_ham_MASAK_reddini_ve_alan_anahtarini_cikarir()
    {
        // Sunucunun eşleştirme 409 cümlesi (BankaModulu.DekontEslestir) — içine ham MASAK reddi gömülü.
        const string ham = "Bu dekont bu müşteriyle eşleştirilirse yasal kimlik tespiti gerekir; sonradan eşleştirme ile yapılamaz. Dekontu STORNO edin ve müşteriyi seçerek kimlik bilgileriyle yeni dekont girin. (MASAK: 200.000 TL ve üstü işlemde işlem amacı beyanıyla birlikte kimlik tespiti zorunludur · MASAK beyan açıklaması (masak.aciklama) en az 20 karakter olmalıdır (şu an 0))";
        var s = ParaMasakKapisi.SadeMetin(ham);
        Assert.DoesNotContain("masak.aciklama", s, StringComparison.Ordinal);
        Assert.DoesNotContain("(MASAK:", s, StringComparison.Ordinal);
        Assert.EndsWith("yeni dekont girin.", s, StringComparison.Ordinal);
        Assert.Equal("MASAK: açıklama en az 20 karakter", ParaMasakKapisi.SadeMetin("MASAK: açıklama (masak.aciklama) en az 20 karakter"));
    }

    [Fact]
    public async Task YK2_S06_gec_gelen_eski_yanit_yeni_tutarin_ustune_yazilmaz()
    {
        var kapilar = new Dictionary<decimal, TaskCompletionSource<MasakGereksinim>>();
        var k = new ParaMasakKapisi(() => Task.FromResult<Esikler?>(Esik),
            (t, _) => (kapilar[t] = new TaskCompletionSource<MasakGereksinim>()).Task);
        await k.Ac("c1");
        var eski = k.Tazele(250_000m, "c1");
        var yeni = k.Tazele(25_000m, "c1");
        kapilar[25_000m].SetResult(Gereksinim(25_000m));
        await yeni;
        kapilar[250_000m].SetResult(Gereksinim(250_000m));
        await eski;
        Assert.False(k.Gerekli);                // 250.000'in geç yanıtı 25.000'i kilitlemez
        Assert.Equal(25_000m, k.Tutar);
    }

    [Fact]
    public async Task YK2_S09_tus_vurusu_bekleme_icinde_yeni_tutar_gelirse_eski_soru_HIC_gonderilmez()
    {
        // Tur 2 (doğrulayıcı DÜŞÜK): "250000" yazmak 6 GET demekti. Soru kısa bekler; arada tutar değişirse gönderilmez.
        var sorulan = new List<decimal>();
        var k = new ParaMasakKapisi(() => Task.FromResult<Esikler?>(Esik),
            (t, _) => { lock (sorulan) sorulan.Add(t); return Task.FromResult(Gereksinim(t)); })
            { SoruGecikmesi = TimeSpan.FromMilliseconds(80) };
        await k.Ac("c1");
        var adimlar = new[] { 2m, 25m, 250m, 2_500m, 25_000m, 250_000m }.Select(t => k.Tazele(t, "c1")).ToList();
        Assert.Null(k.Gereksinim);              // beklerken kilit "hazırlanıyor" (fail-closed)
        await Task.WhenAll(adimlar);
        lock (sorulan) Assert.Equal([250_000m], sorulan);
        Assert.True(k.FormCizilir);
    }

    [Fact]
    public async Task YK2_S07_esik_alinamazsa_ret_cikissiz_kalmaz_sebep_yazar()
    {
        var k = new ParaMasakKapisi(() => Task.FromResult<Esikler?>(null), (t, _) => Task.FromResult(Gereksinim(t)));
        await k.Ac("c1");
        await k.Tazele(250_000m, "c1");
        Assert.False(k.Gerekli);                // eşik bilinmez → karar sunucuda
        Assert.True(await k.Reddet(new ApiHata(400, "MASAK: ... (masak.aciklama) ..."), "tr"));
        Assert.Equal(YasalKapi2Sozlugu.Tr.MasakEsikYok, k.RedBandi);
        Assert.Equal(YasalKapi2Sozlugu.Tr.MasakEsikYok, k.KilitSebebi("tr"));
    }

    [Fact]
    public void YK2_S08_sozluk_TR_EN_dolu()
    {
        foreach (var m in new[] { YasalKapi2Sozlugu.Tr, YasalKapi2Sozlugu.En })
            foreach (var p in typeof(YasalKapi2Metinleri).GetProperties())
                Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(m)), p.Name);
        Assert.DoesNotContain("kaydedebilirsiniz", YasalKapi2Sozlugu.Tr.FarkYok, StringComparison.Ordinal);
    }
}

/// <summary>bUnit: Müşteriler ekranının dört para formu + satış fark satırı + siparişten satış.</summary>
public sealed class YasalKapi2EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private decimal bugunToplam;          // sahte sunucunun "bugünkü bağlantılı toplam"ı (c1)
    private bool sunucuReddeder;          // POST masaksız gelse de reddet (toplam yalnız sunucuda)
    private bool esikYok;

    public YasalKapi2EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private const string MasakRet = """{"message":"MASAK: 200.000 TL ve üstü işlemde işlem amacı beyanıyla birlikte kimlik tespiti zorunludur · İşlem amacı belirtilmelidir · MASAK beyan açıklaması (masak.aciklama) en az 20 karakter olmalıdır (şu an 0)"}""";

    private sealed class SahteApi(YasalKapi2EkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add((istek.Method.Method, yol, govde));
            var kod = 200;
            string g;
            if (yol.Contains("/masak/esikler"))
                g = t.esikYok ? "[]" : """{"kimlikEsigi":370000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":0,"ekBelgeEsigi":0,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"],"kimlikIstenenEnDusukTutar":200000}""";
            else if (yol.Contains("/masak/gereksinim"))
            {
                var q = System.Web.HttpUtility.ParseQueryString(istek.RequestUri.Query);
                var tutar = decimal.Parse(q["tutar"]!, System.Globalization.CultureInfo.InvariantCulture);
                var r = YasalKapi2SafTestleri.Gereksinim(tutar, q["cariId"] == "c1" ? t.bugunToplam : 0m);
                g = JsonSerializer.Serialize(new { kademe = r.Kademe, kimlikGerekli = r.KimlikGerekli, amacGerekli = r.AmacGerekli, beyanFormuGerekli = false, ekBelgeGerekli = false, asgariAciklamaUzunlugu = 20, olcuTutarTl = r.OlcuTutarTl, islemTutarTl = r.IslemTutarTl });
            }
            else if (istek.Method == HttpMethod.Post && (yol.Contains("/tahsilat") || yol.Contains("/veresiye") || yol.Contains("/doviz-")))
            {
                var e = JsonDocument.Parse(govde).RootElement;
                var masakVar = e.TryGetProperty("masak", out var m) && m.ValueKind == JsonValueKind.Object;
                var tl = e.TryGetProperty("tlTutari", out var x) && x.ValueKind == JsonValueKind.Number ? x.GetDecimal()
                       : e.TryGetProperty("tutar", out var y) && y.ValueKind == JsonValueKind.Number ? y.GetDecimal() * 34m : 0m;
                if (!masakVar && (t.sunucuReddeder || tl + t.bugunToplam >= 200_000m)) { kod = 400; g = MasakRet; }
                else g = """{"id":"h1"}""";
            }
            else g = yol switch
            {
                var z when z.Contains("/tevsik/gunluk") => """{"had":30000,"taksit":{"kuralDevrede":false,"nakitAlinabilir":null}}""",
                var z when z.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},{"id":"ks2","ad":"Dolar Kasası","paraBirimi":"USD","aktif":true}]""",
                var z when z.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-16T09:00:00Z","ayarFiyatlari":[]}""",
                var z when z.Contains("/fiyat/listeler") => "[]",
                var z when z.Contains("/cari/c1") && istek.Method == HttpMethod.Get =>
                    """{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":null,"email":null,"vergiNo":null,"adres":null}""",
                var z when z.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","tlBakiye":0,"hasGramBakiye":0}]""",
                _ => "[]",
            };
            return new HttpResponseMessage((System.Net.HttpStatusCode)kod)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Cariler> Ciz(string? mod = null)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("button");
        if (mod is not null) { Services.GetRequiredService<ArayuzModuServisi>().Uygula(mod, mod); e.Render(); }   // KesifP2 deseni: ilk yüklemeden sonra
        return e;
    }

    private static void Dugme(IRenderedComponent<Kuyumcu.Web.Pages.Cariler> e, string metin) =>
        (e.FindAll("button").FirstOrDefault(b => b.TextContent.Trim() == metin)
         ?? e.FindAll("button").First(b => b.TextContent.Contains(metin) && !b.TextContent.Contains("Döviz"))).Click();

    private static JsonElement? Masak(string govde) =>
        JsonDocument.Parse(govde).RootElement.TryGetProperty("masak", out var m) && m.ValueKind == JsonValueKind.Object ? m : null;

    private static void FormuDoldur<T>(IRenderedComponent<T> e) where T : Microsoft.AspNetCore.Components.IComponent
    {
        e.Find("#masak-belge-no").Input("10000000146");
        e.Find("#masak-ad-soyad").Input("Ayşe Demir");
        e.Find("#masak-amac").Input("Borç kapama");
        e.Find("#masak-aciklama").Input("Önceki bilezik alışının borcunu kapatıyor");
    }

    private static string EsikBaglam<T>(IRenderedComponent<T> e) where T : Microsoft.AspNetCore.Components.IComponent =>
        string.Join(" ‖ ", e.FindAll("*").Where(x => x.Children.Length == 0 && System.Text.RegularExpressions.Regex.IsMatch(x.TextContent, @"200\.000\s*₺ ve üstünde"))
            .Select(x => $"<{x.TagName} id={x.Id} class={x.ClassName}> {x.TextContent.Trim()}"));

    /// <summary>Ekranda (görünen metin) eşik cümlesinin kaç kez geçtiği — "aynı uyarı 2-3 kez" sayacı.</summary>
    private static int EsikCumlesi<T>(IRenderedComponent<T> e) where T : Microsoft.AspNetCore.Components.IComponent =>
        System.Text.RegularExpressions.Regex.Matches(string.Concat(e.Nodes.Select(n => n.TextContent)), @"200\.000\s*₺ ve üstünde").Count;

    [Theory]
    [InlineData("BASIT", 1)]
    [InlineData("PROFESYONEL", 1)]   // YASAL-KAPI-3 (tasarım hakemi "panel olmayan PRO çekmecede eşik iki blokta"): çekmecede #masak-kural YOK — eşik yalnız başlıkta
    public void YK2_01_tahsilat_250000_ortak_MASAK_formu_acilir_Kaydet_kilitli_sebep_tek_esik_bir_kez(string mod, int esikSayisi)
    {
        var e = Ciz(mod);
        Dugme(e, "Tahsilat");
        e.WaitForElement("#th-tutar");
        e.Find("#th-tutar").Input("250000");
        e.WaitForElement("#th-masak");
        Assert.True(e.Find("#th-kaydet").HasAttribute("disabled"));
        Assert.Equal(ZoIslemSozlugu.Tr.MasakKilitSebepBasit, e.Find("#th-kaydet-kilit").TextContent.Trim());
        Assert.True(esikSayisi == EsikCumlesi(e), EsikBaglam(e));   // BASİT: eşik yalnız form başlığında
        Assert.DoesNotContain("200.000", e.Find("#th-kaydet-kilit").TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll(".kart--masak .hata-metni"));         // dokunulmadan zorunluluk hatası yok
        Assert.DoesNotContain("masak.aciklama", e.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void YK2_02_tahsilat_beyanla_POST_govdede_masak_esik_altinda_YOK()
    {
        var e = Ciz();
        Dugme(e, "Tahsilat");
        e.WaitForElement("#th-tutar");
        e.Find("#th-tutar").Input("250000");
        e.WaitForElement("#th-masak");
        FormuDoldur(e);
        e.WaitForAssertion(() => Assert.False(e.Find("#th-kaydet").HasAttribute("disabled")));
        e.Find("#th-kaydet").Click();
        var post = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("/tahsilat"));
        var m = Assert.NotNull(Masak(post.Govde));
        Assert.Equal("10000000146", m.GetProperty("belgeNo").GetString());
        Assert.Equal("Borç kapama", m.GetProperty("islemAmaci").GetString());

        // eşik altı: formsuz, gövdede masak YOK
        istekler.Clear();
        Dugme(e, "Tahsilat");
        e.WaitForElement("#th-tutar");
        e.Find("#th-tutar").Input("199999.99");
        Assert.Empty(e.FindAll("#th-masak"));
        Assert.False(e.Find("#th-kaydet").HasAttribute("disabled"));
        e.Find("#th-kaydet").Click();
        var post2 = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("/tahsilat"));
        Assert.Null(Masak(post2.Govde));
        Assert.DoesNotContain("Borç kapama", post2.Govde, StringComparison.Ordinal);
    }

    [Fact]
    public void YK2_03_yukari_asagi_yukari_form_ve_kilit_kalkar_geri_gelir_beyan_korunur()
    {
        var e = Ciz();
        Dugme(e, "Tahsilat");
        e.WaitForElement("#th-tutar");
        e.Find("#th-tutar").Input("250000");
        e.WaitForElement("#th-masak");
        e.Find("#masak-ad-soyad").Input("Ayşe Demir");
        e.Find("#th-tutar").Input("25000");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#th-masak")));
        Assert.False(e.Find("#th-kaydet").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#th-kaydet-kilit"));
        e.Find("#th-tutar").Input("370000");
        e.WaitForElement("#th-masak");
        Assert.Equal("Ayşe Demir", e.Find("#masak-ad-soyad").GetAttribute("value"));
        Assert.True(e.Find("#th-kaydet").HasAttribute("disabled"));
    }

    [Fact]
    public void YK2_04_sunucu_reddi_ham_metin_basilmaz_alanlar_acilir_tutar_inince_eski_hata_ve_form_kalkar()
    {
        var e = Ciz();
        Dugme(e, "Tahsilat");
        e.WaitForElement("#th-tutar");
        // ekran eşiğin altında (150.000) formu açmaz; bugünkü toplam yalnız sunucuda bilinir → ret
        sunucuReddeder = true;
        e.Find("#th-tutar").Input("150000");
        Assert.Empty(e.FindAll("#th-masak"));
        bugunToplam = 60_000m;                    // sunucu: bugün bu müşteriyle 60.000 daha → 210.000
        e.Find("#th-kaydet").Click();
        e.WaitForElement("#th-masak");             // alanlar açıldı (çıkışsız değil)
        Assert.DoesNotContain("masak.aciklama", e.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("şu an 0", e.Markup, StringComparison.Ordinal);
        Assert.True(e.Find("#th-kaydet").HasAttribute("disabled"));
        // tutar iner: form, kilit ve ret kalkar
        bugunToplam = 0m;
        e.Find("#th-tutar").Input("25000");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#th-masak")));
        Assert.Empty(e.FindAll("#th-masak-ret"));
        Assert.False(e.Find("#th-kaydet").HasAttribute("disabled"));
        Assert.DoesNotContain(e.FindAll("[role=alert]"), b => b.TextContent.Contains("MASAK", StringComparison.Ordinal));
    }

    [Fact]
    public void YK2_13_esik_alinamazsa_sunucu_reddi_cikissiz_kalmaz_ham_metin_yok_tutar_inince_kalkar()
    {
        // Tur 2: /masak/esikler okunamıyor (esikYok) → ekran eşiği bilemez, formu önceden açamaz; sunucu reddederse
        // ham "masak.aciklama" BASILMAZ, ne yapılacağı (sayfayı yenile) yazar; tutar inince ret bandı kalkar.
        esikYok = true;
        var e = Ciz();
        Dugme(e, "Tahsilat");
        e.WaitForElement("#th-tutar");
        e.Find("#th-tutar").Input("250000");
        e.Find("#th-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(YasalKapi2Sozlugu.Tr.MasakEsikYok, e.Find("#th-masak-ret").TextContent.Trim()), TimeSpan.FromSeconds(3));
        Assert.DoesNotContain("masak.aciklama", e.Markup, StringComparison.Ordinal);
        e.Find("#th-tutar").Input("25000");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#th-masak-ret")), TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void YK2_14_tahsilat_tutari_tus_tus_yazilinca_gereksinim_TEK_kez_sorulur()
    {
        // Tur 2 (doğrulayıcı DÜŞÜK): Para.razor @oninput → her tuşta /masak/gereksinim gidiyordu ("250000" = 6 GET).
        var e = Ciz();
        Dugme(e, "Tahsilat");
        e.WaitForElement("#th-tutar");
        foreach (var t in new[] { "2", "25", "250", "2500", "25000", "250000" }) e.Find("#th-tutar").Input(t);
        e.WaitForElement("#th-masak", TimeSpan.FromSeconds(3));
        Assert.Single(istekler.ToArray(), i => i.Yol.Contains("/masak/gereksinim"));   // ToArray: sahte API arka planda ekler
    }

    [Fact]
    public void YK2_05_veresiye_esik_ustu_form_ve_sebep_esik_alti_govdede_masak_yok()
    {
        var e = Ciz();
        Dugme(e, "Veresiye");
        e.WaitForElement("#vr-tutar");
        e.Find("#vr-tutar").Input("200000");
        e.WaitForElement("#vr-masak");
        Assert.True(e.Find("#vr-kaydet").HasAttribute("disabled"));
        Assert.Equal(ZoIslemSozlugu.Tr.MasakKilitSebepBasit, e.Find("#vr-kaydet-kilit").TextContent.Trim());
        FormuDoldur(e);
        e.WaitForAssertion(() => Assert.False(e.Find("#vr-kaydet").HasAttribute("disabled")));
        e.Find("#vr-kaydet").Click();
        Assert.NotNull(Masak(Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("/veresiye")).Govde));

        istekler.Clear();
        Dugme(e, "Veresiye");
        e.WaitForElement("#vr-tutar");
        e.Find("#vr-tutar").Input("199999.99");
        Assert.Empty(e.FindAll("#vr-masak"));
        e.Find("#vr-kaydet").Click();
        Assert.Null(Masak(Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("/veresiye")).Govde));
    }

    [Fact]
    public void YK2_06_doviz_tahsilat_TL_karsiligi_esikte_form_acar_asagi_inince_kapanir()
    {
        var e = Ciz();
        Dugme(e, "Döviz Tahsilat");
        e.WaitForElement("#dt-tutar", TimeSpan.FromSeconds(5));   // YASAL-KAPI-3 tur 2: tam süit yükünde açık zaman aşımı
        e.Find("#dt-tutar").Input("10000");            // 10.000 USD × 34 = 340.000 TL
        e.WaitForElement("#dt-masak", TimeSpan.FromSeconds(5));
        Assert.True(e.Find("#cekmece-doviz-tahsilat-kaydet").HasAttribute("disabled"));
        Assert.Contains(ZoIslemSozlugu.Tr.MasakKilitSebepBasit, e.Find("#cekmece-doviz-tahsilat-kilit").TextContent, StringComparison.Ordinal);
        e.Find("#dt-tutar").Input("1000");             // 34.000 TL
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#dt-masak")), TimeSpan.FromSeconds(5));
        Assert.False(e.Find("#cekmece-doviz-tahsilat-kaydet").HasAttribute("disabled"));
        e.Find("#cekmece-doviz-tahsilat-kaydet").Click();
        Assert.Null(Masak(Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("/doviz-tahsilat")).Govde));
    }

    [Fact]
    public void YK2_07_doviz_veresiye_esikte_form_beyanla_govdede_masak_var()
    {
        var e = Ciz();
        Dugme(e, "Döviz Veresiye");
        e.WaitForElement("#dv-tutar", TimeSpan.FromSeconds(5));
        e.Find("#dv-tutar").Input("6000");             // 204.000 TL
        e.WaitForElement("#dv-masak", TimeSpan.FromSeconds(5));
        Assert.True(e.Find("#cekmece-doviz-veresiye-kaydet").HasAttribute("disabled"));
        FormuDoldur(e);
        e.WaitForAssertion(() => Assert.False(e.Find("#cekmece-doviz-veresiye-kaydet").HasAttribute("disabled")));
        e.Find("#cekmece-doviz-veresiye-kaydet").Click();
        Assert.NotNull(Masak(Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("/doviz-veresiye")).Govde));
    }

    [Theory]
    [InlineData(true, "Fark yok.")]
    [InlineData(false, "Fark yok, kaydedebilirsiniz.")]
    public void YK2_08_fark_satiri_Kaydet_kilitliyken_kaydedebilirsiniz_demez(bool kilitli, string beklenen)
    {
        var c = Render<FisVerdimAldim>(p => p
            .Add(x => x.KalemVar, true).Add(x => x.Toplam, 220_000m).Add(x => x.KaydetKilitli, kilitli));
        Assert.Equal(beklenen, c.Find("#islem-fark-durum").TextContent.Trim());
    }

    [Fact]
    public async Task YK2_09_siparisten_satis_esik_ustu_form_ve_kilit_toplam_inince_kalkar()
    {
        var durum = new DigerIslemDurumu();
        var pm = new ParaMasakKapisi(() => Task.FromResult<Esikler?>(YasalKapi2SafTestleri.Esik),
            (t, _) => Task.FromResult(YasalKapi2SafTestleri.Gereksinim(t)));
        await pm.Ac("c1");
        durum.Siparis.Masak = pm;
        durum.Siparis.Secili = new SiparisDetayi("s1", "SP-1", "HAZIR", "c1", [new SiparisKalemi("k1", "u1", "Bilezik", null, 20m, 1m)]);
        durum.Siparis.Arac = "VERESIYE";
        var baglam = new DigerBaglam([], [], [], [], true, "SAHIP");
        var c = Render<FisDigerSiparis>(p => p.Add(x => x.Durum, durum).Add(x => x.Baglam, baglam));
        c.Find("#siparis-fiyat-k1").Input("250000");
        c.WaitForElement("#siparis-masak");
        Assert.True(c.Find("#siparis-cevir").HasAttribute("disabled"));
        Assert.Equal(ZoIslemSozlugu.Tr.MasakKilitSebepBasit, c.Find("#siparis-cevir-kilit").TextContent.Trim());
        c.Find("#siparis-fiyat-k1").Input("199999.99");
        c.WaitForAssertion(() => Assert.Empty(c.FindAll("#siparis-masak")));
        Assert.False(c.Find("#siparis-cevir").HasAttribute("disabled"));
        Assert.Null(pm.Govde);                         // eşik altında gövdeye beyan gitmez
    }
}

/// <summary>bUnit: Banka dekontu (müşterili) — sunucu MASAK reddi YasalKapiIzi deseniyle (ham metin yok, tutar inince kalkar).</summary>
public sealed class YasalKapi2BankaTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private decimal bugunToplam;

    public YasalKapi2BankaTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
    }

    private sealed class SahteApi(YasalKapi2BankaTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add((istek.Method.Method, yol, govde));
            var kod = 200;
            string g;
            if (yol.Contains("/masak/esikler"))
                g = """{"kimlikEsigi":370000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":0,"ekBelgeEsigi":0,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"],"kimlikIstenenEnDusukTutar":200000}""";
            else if (yol.Contains("/masak/gereksinim"))
            {
                var q = System.Web.HttpUtility.ParseQueryString(istek.RequestUri.Query);
                var r = YasalKapi2SafTestleri.Gereksinim(decimal.Parse(q["tutar"]!, System.Globalization.CultureInfo.InvariantCulture), q["cariId"] == "c1" ? t.bugunToplam : 0m);
                g = JsonSerializer.Serialize(new { kademe = r.Kademe, kimlikGerekli = r.KimlikGerekli, amacGerekli = r.AmacGerekli, beyanFormuGerekli = false, ekBelgeGerekli = false, asgariAciklamaUzunlugu = 20, olcuTutarTl = r.OlcuTutarTl, islemTutarTl = r.IslemTutarTl });
            }
            else if (istek.Method == HttpMethod.Post && yol.EndsWith("/hareket"))
            {
                var masakVar = JsonDocument.Parse(govde).RootElement.TryGetProperty("masak", out var m) && m.ValueKind == JsonValueKind.Object;
                if (!masakVar) { kod = 400; g = """{"message":"MASAK: İşlem amacı belirtilmelidir · MASAK beyan açıklaması (masak.aciklama) en az 20 karakter olmalıdır (şu an 0)"}"""; }
                else g = """{"id":"bh9"}""";
            }
            else g = yol switch
            {
                var v when v.Contains("/bankalar/") && v.Contains("/hareketler") => """{"hareketler":[]}""",
                var v when v.Contains("/bankalar") && istek.Method == HttpMethod.Get =>
                    """{"hesaplar":[{"id":"b1","bankaAdi":"Ziraat","hesapAdi":"Ana Hesap","hesapSahibi":"X Ltd.","iban":"TR200000000000000000000001","paraBirimi":"TRY","bakiye":0,"aktif":true}]}""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz"}]""",
                _ => "{}",
            };
            return new HttpResponseMessage((System.Net.HttpStatusCode)kod)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    [Fact]
    public void YK2_10_banka_dekontu_sunucu_reddi_ham_metin_yok_alanlar_acilir_tutar_inince_kalkar()
    {
        var e = Render<Kuyumcu.Web.Pages.Banka>();
        e.WaitForElement("#bh-dekont-gir");
        e.Find("#bh-dekont-gir").Click();
        e.WaitForElement("#dekont-tutar");
        e.DekontDetayAc();
        SeciciYardim.Sec(e, "dekont-cari", "c1");
        e.Find("#dekont-tutar").Input("150000");
        Assert.Empty(e.FindAll("#masak-belge-no"));         // ekran eşiğin altında formu açmaz
        bugunToplam = 60_000m;                                // toplam yalnız sunucuda: 210.000
        e.Find("#dekont-kaydet").Click();
        e.WaitForElement("#masak-belge-no");                  // ret sonrası alanlar açık (çıkışsız değil)
        Assert.DoesNotContain("masak.aciklama", e.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("şu an 0", e.Markup, StringComparison.Ordinal);
        Assert.True(e.Find("#dekont-kaydet").HasAttribute("disabled"));
        bugunToplam = 0m;
        e.Find("#dekont-tutar").Input("25000");               // reddin altına indi: iz geçersiz
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#masak-belge-no")));
        Assert.False(e.Find("#dekont-kaydet").HasAttribute("disabled"));
        Assert.DoesNotContain(e.FindAll("[role=alert]"), b => b.TextContent.Contains("MASAK", StringComparison.Ordinal)
                                                          || b.TextContent.Contains("kabul edilmedi", StringComparison.Ordinal));
    }
}
