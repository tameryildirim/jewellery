using Bunit;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// /yedekler EKRANI (paket T-3 Faz A)
//
// Sınanan sözleşme:
//   • son yedek zamanı/boyutu/şifreli rozeti görünür
//   • doğrulanmamış yedek → KIRMIZI rozet + uyarı bandı (sabit yeşil YASAK)
//   • gerekçe < 20 karakter → düğme disabled ve SEBEBİ yazılı (UX-S30)
//   • hata BOŞ DURUMA çevrilmez — hata bandı ayrı (UX-S28, Anayasa md.1)
//   • "tüm işletmeleri kapsayan sistem yedeği" uyarısı ekranda (Mehmet)
//   • menü: SAHİP dışı görmez, basit modda hiç görünmez
// ═════════════════════════════════════════════════════════════════════════════

public sealed class YedekEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool hataDondur;
    private string dogrulama = "DOGRULANDI";
    // PAKET P-77: sunucudaki bekleyen doğrulama talebi. POST dogrulama-talep
    // gelince sahte API bunu doldurur — gerçek sunucunun özet davranışı.
    private bool bekleyenDogrulama;
    private string BekleyenJson => bekleyenDogrulama
        ? """{"id":"dg1","olusturuldu":"2026-09-17T10:05:00Z"}"""
        : "null";

    // PAKET P-76: RPO ölçümü ve taahhüdü testten ayarlanır (null = hiç başarılı yedek yok).
    private int? rpoOlculen = 95;
    private int rpoTaahhut = 360;
    private string RpoJson => rpoOlculen is { } r ? r.ToString(System.Globalization.CultureInfo.InvariantCulture) : "null";

    private string Ozet => $$"""
        {"sonYedek":{"tur":"TAM","zaman":"2026-09-17T03:15:00Z","bitis":"2026-09-17T03:16:00Z",
          "boyutBayt":3760268,"durum":"BASARILI","sifreli":true,"tabloSayisi":54,"hataOzeti":null},
         "dogrulama":"{{dogrulama}}","dogrulamaTarih":"2026-09-17T04:30:00Z",
         "sonDogrulanmisYedek":"2026-09-17T03:15:00Z","disKopya":"KOPYALANDI",
         "saklananKosu":7,"saklamaGun":30,"rpoDakikaOlculen":{{RpoJson}},"rpoDakikaTaahhut":{{rpoTaahhut}},
         "rtoDakikaTaahhut":120,"rtoDakikaOlculen":1,"sonTatbikat":"2026-09-17T04:30:00Z",
         "sonTatbikatSonuc":"BASARILI","azamiLimit":50,"sonKosuSaglikli":true,"saklamaSilmeAktif":true,
         "sonBasariliTatbikat":"2026-09-17T04:30:00Z","tatbikatYasGun":6,"tatbikatAzamiGun":30,
         "tatbikatBayat":false,"bekleyenDogrulamaTalebi":{{BekleyenJson}}}
        """;

    private const string Kosular = """
        {"limit":50,"azamiLimit":50,"dahaFazlasiVar":false,
         "kayitlar":[{"id":"y1","tur":"TAM","baslangic":"2026-09-17T03:15:00Z","bitis":"2026-09-17T03:16:00Z",
           "durum":"BASARILI","boyutBayt":3760268,"sifreli":true,"disKopya":"KOPYALANDI",
           "dogrulama":"DOGRULANDI","tabloSayisi":54,"hataOzeti":null}]}
        """;

    private const string Tatbikatlar = """
        {"limit":12,"azamiLimit":50,
         "tatbikatlar":[{"id":"t1","tarih":"2026-09-17T04:30:00Z","tabloSayisi":54,"uyusan":54,
           "uyusmayan":0,"sonuc":"BASARILI","sureSn":8,"semaDusuruldu":true,"rtoDakikaOlculen":1,
           "rapor":"uyusmayan_tablolar="}]}
        """;

    /// <summary>PAKET P-56 — düzeltilmiş gerekçe (20+ karakter).</summary>
    private const string YeniGerekce = "mali müşavir talebiyle ek yedek alınıyor";

    // PAKET P-56: ikinci satır İŞLENMİŞ taleptir — gerekçesi artık denetim
    // izinin parçasıdır ve düzenlenemez. (⚠ Şartnamedeki "CALISIYOR" durumu
    // KODDA YOKTUR: YedekTalepDurumu = BEKLIYOR/ISLENDI/IPTAL/HATA.)
    private const string Talepler = """
        {"limit":20,"azamiLimit":50,
         "talepler":[{"id":"tal1","gerekce":"aylık denetim öncesi elle yedek","durum":"BEKLIYOR",
           "olusturuldu":"2026-09-17T09:00:00Z","islendi":null,"yedekKaydiId":null,"iptalGerekce":null},
          {"id":"tal2","gerekce":"çeyreklik kapanış öncesi elle yedek","durum":"ISLENDI",
           "olusturuldu":"2026-09-16T09:00:00Z","islendi":"2026-09-16T09:01:00Z",
           "yedekKaydiId":"y1","iptalGerekce":null}]}
        """;

    public YedekEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        // P-36b kapanışı (P-37 artığı): KenarCubugu favori/sıra tercihini bu
        // servisten okur; Program.cs'te kayıtlıydı, fikstürlerde DEĞİLDİ →
        // kabuk çizen her test DI hatasıyla kırmızıydı.
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
    }

    private sealed class SahteApi(YedekEkranTestleri sahip) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            sahip.istekler.Add((istek.Method.Method, yol, govde));

            if (sahip.hataDondur && yol.Contains("/yedekler"))
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(
                        """{"message":"Yedek üstverisi okunamadı","error":"Internal Server Error","statusCode":500}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };
            }

            var g = yol switch
            {
                var y when y.EndsWith("/yedekler/ozet") => sahip.Ozet,
                var y when y.EndsWith("/yedekler/tatbikatlar") => Tatbikatlar,
                var y when y.EndsWith("/yedekler/talepler") => Talepler,
                // PAKET P-56 — gerekçe düzeltme ucu GERÇEKÇİ yanıt döner
                // (sözleşme camelCase; "{}" varsayılanına düşmesi yasak).
                var y when y.EndsWith("/gerekce") => $$"""
                    {"id":"tal1","durum":"BEKLIYOR","gerekce":"{{YeniGerekce}}","degisti":true,
                     "mesaj":"Gerekçe güncellendi; eski ve yeni değer denetim izine yazıldı."}
                    """,
                // PAKET P-77 — doğrulama talebi ucu GERÇEKÇİ yanıt döner ve
                // sonraki özet bekleyen talebi taşır (sunucu davranışı).
                var y when y.EndsWith("/yedekler/dogrulama-talep") => sahip.DogrulamaYaniti(),
                var y when y.Contains("/yedekler/talep") =>
                    """{"id":"tal2","durum":"BEKLIYOR","olusturuldu":"2026-09-17T10:00:00Z","mesaj":"Yedek talebi kuyruğa alındı"}""",
                var y when y.EndsWith("/yedekler") => Kosular,
                var y when y.Contains("/yetkiler/benim") =>
                    """{"moduller":["cari","urun","islem","kasa","rapor"]}""",
                var y when y.Contains("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":4200}""",
                var y when y.Contains("/auth/ben") =>
                    """{"id":"u1","ad":"Test","soyad":"Kullanici","email":"t@t.local","rol":"SAHIP","subeId":null}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private string DogrulamaYaniti()
    {
        bekleyenDogrulama = true;
        return """
            {"id":"dg1","tur":"DOGRULAMA","durum":"BEKLIYOR","olusturuldu":"2026-09-17T10:05:00Z",
             "mesaj":"Doğrulama talebi kuyruğa alındı; zamanlayıcı en geç 60 saniye içinde başlatır."}
            """;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Yedekler> Ciz(string rol = "SAHIP")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1"));
        return Render<Kuyumcu.Web.Pages.Yedekler>();
    }

    [Fact]
    public void Yedekler_SonYedekBoyutVeDogrulamaDurumunuGosterir()
    {
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".stat-kart")));
        var metin = e.Markup;

        Assert.Contains("3,59 MB", metin.Replace('.', ','));   // 3760268 bayt
        Assert.Contains("Doğrulandı", metin);
        Assert.Contains("Kopyalandı", metin);
        // ÖLÇÜM ve TAAHHÜT ayrı yazılır (MÜHÜR 6 / K2).
        Assert.Contains("95 dk", metin);
        Assert.Contains("360 dk", metin);
        Assert.Contains("120 dk", metin);
        // Koşu ve tatbikat tabloları doldu.
        Assert.NotEmpty(e.FindAll("#yd-kosu-tablosu tbody tr"));
        Assert.NotEmpty(e.FindAll("#yd-tatbikat-tablosu tbody tr"));
    }

    [Fact]
    public void SistemGeneliUyarisi_Ve_SaklamaNotu_Ekranda_Yazili()
    {
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yd-sistem-uyari")));
        Assert.Contains("tüm işletmeleri", e.Find("#yd-sistem-uyari").TextContent);
        // 30 gün YEDEK saklamasıdır, KAYIT saklaması değildir (VUK/TTK ayrımı).
        Assert.Contains("30", e.Find("#yd-saklama-notu").TextContent);
        Assert.NotEmpty(e.FindAll("#yd-anahtar-uyari"));
    }

    [Fact]
    public void DogrulanmamisYedek_KirmiziRozetVeUyariBandi()
    {
        dogrulama = "UYUSMADI";
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yd-dogrulama-uyari")));

        Assert.Contains("Uyuşmadı", e.Markup);
        Assert.Contains("kirmizi", e.Find(".stat-izgara").InnerHtml);
        Assert.Contains("doğrulanmadı", e.Find("#yd-dogrulama-uyari").TextContent);
    }

    [Fact]
    public void Gerekce_YirmiKarakterAltinda_DugmeDisabled_SebepYazili()
    {
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yd-talep")));

        var dugme = e.Find("#yd-talep");
        Assert.True(dugme.HasAttribute("disabled"));
        Assert.NotEmpty(e.FindAll("#yd-talep-kilit"));      // UX-S30: sebep yazılı

        e.Find("#yd-gerekce").Input("kısa");
        Assert.True(e.Find("#yd-talep").HasAttribute("disabled"));
        Assert.NotEmpty(e.FindAll("#yd-gerekce-hata"));

        e.Find("#yd-gerekce").Input("aylık denetim öncesi elle yedek alınıyor");
        Assert.False(e.Find("#yd-talep").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#yd-talep-kilit"));

        // DoD/1: düğme gerçek istek atar ve sonuç başarı bandına yazılır (UX-S27).
        e.Find("#yd-talep").Click();
        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/yedekler/talep")));
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yd-basari-bandi")));
    }

    [Fact]
    public void Hata_BosDurumaCevrilmez_HataBandiGorunur()
    {
        hataDondur = true;
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yd-hata-bandi")));

        // UX-S28: hata "kayıt yok"a çevrilmez — boş durum ÇIKMAZ.
        Assert.Empty(e.FindAll(".bos-durum"));
        Assert.Empty(e.FindAll("#yd-kosu-tablosu"));
    }

    [Fact]
    public void TalepIptali_YikiciOnayModaliIster_VeGerekceSiz_Kilitli()
    {
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yd-iptal-tal1")));

        e.Find("#yd-iptal-tal1").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yd-iptal-onay")));
        Assert.True(e.Find("#yd-iptal-onay").HasAttribute("disabled"));

        e.Find("#yd-iptal-gerekce").Input("yanlışlıkla açıldı, iptal edildi");
        Assert.False(e.Find("#yd-iptal-onay").HasAttribute("disabled"));

        e.Find("#yd-iptal-onay").Click();
        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Metot == "PATCH" && i.Yol.Contains("/yedekler/talep/tal1")));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET P-56 — BEKLEYEN TALEBİN GEREKÇESİ DÜZENLENEBİLİR
    //
    // UX-1/UX-S25: talep henüz işlenmemişken gerekçenin taşa yazılması için
    // hiçbir mevzuat dayanağı yok. Tek alanlı düzenleme kalıbı =
    // SatirIciDuzenle (DoD EK md.2): tetikleyici id'si
    // #si-yedek-talep-gerekce-{id}-ac, girdi #si-yedek-talep-gerekce-{id}.
    // ═════════════════════════════════════════════════════════════════════════

    private const string DuzenleTetik = "#si-yedek-talep-gerekce-tal1-ac";
    private const string DuzenleGirdi = "#si-yedek-talep-gerekce-tal1";

    [Fact]
    public void BekleyenTalebinGerekcesiDuzenlenir()
    {
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(DuzenleTetik)));

        // UX-S25: eylem GÖRÜNÜR ve "Düzenle" diye adlandırılmıştır.
        Assert.Contains("Düzenle", e.Find(DuzenleTetik).GetAttribute("aria-label"));

        e.Find(DuzenleTetik).Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(DuzenleGirdi)));

        e.Find(DuzenleGirdi).Input(YeniGerekce);
        e.Find("#si-yedek-talep-gerekce-tal1-kaydet").Click();

        // Gerçek PATCH atılır (DoD/1: tıklanınca hiçbir şey olmayan düğme yok)
        // ve İPTAL ucuna DEĞİL, gerekçe ucuna gider (talep iptal OLMAZ).
        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "PATCH" && i.Yol.EndsWith("/yedekler/talep/tal1/gerekce")));
        var istek = istekler.Last(i => i.Yol.EndsWith("/gerekce"));
        // ⚠ Gövde JSON'unda Türkçe harfler ü gibi kaçışlıdır; ASCII parça
        //   üzerinden sınanır (gerekçenin kendisi yanıt/banttan doğrulanıyor).
        Assert.Contains("\"gerekce\"", istek.Govde);
        Assert.Contains("ek yedek", istek.Govde);

        // UX-S27: sunucunun döndürdüğü sonuç başarı bandına yazılır.
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yd-basari-bandi")));
        Assert.Contains(YeniGerekce, e.Find("#yd-basari-bandi").TextContent);
    }

    [Fact]
    public void CalisanTalepteDuzenleGorunmez()
    {
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(DuzenleTetik)));

        // İŞLENMİŞ talepte (tal2) düzenleme tetiği YOK — durum kapısı.
        Assert.Empty(e.FindAll("#si-yedek-talep-gerekce-tal2-ac"));
        Assert.Empty(e.FindAll("#si-yedek-talep-gerekce-tal2"));
        // Gerekçe metni yine de GÖRÜNÜR (salt okunur).
        Assert.Contains("çeyreklik kapanış öncesi elle yedek", e.Markup);
        // UX-S30/DoD EK md.1: eylem yerine değişmezlik gerekçesi durur.
        Assert.NotEmpty(e.FindAll(".degismez-ipucu"));
    }

    [Fact]
    public void KisaGerekceIstekGondermez()
    {
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(DuzenleTetik)));

        e.Find(DuzenleTetik).Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(DuzenleGirdi)));

        e.Find(DuzenleGirdi).Input("kısa gerekçe");        // 12 karakter
        e.Find("#si-yedek-talep-gerekce-tal1-kaydet").Click();

        // Sessiz etkisizlik YASAK: alan düzeyinde Türkçe mesaj çıkar…
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yd-gerekce-hata-tal1")));
        Assert.Contains("20", e.Find("#yd-gerekce-hata-tal1").TextContent);
        // …ve İSTEK GİTMEZ.
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/gerekce"));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET P-77 — "Şimdi doğrula"
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Yedekler_SimdiDogrulaDugmesi_GorunurVeCalisir()
    {
        dogrulama = "UYUSMADI";
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yd-dogrula")));

        // Uyarının yanında düğme var, AÇIK ve kilit sebebi yok.
        Assert.NotEmpty(e.FindAll("#yd-dogrulama-uyari"));
        var dugme = e.Find("#yd-dogrula");
        Assert.Contains("Şimdi doğrula", dugme.TextContent);
        Assert.False(dugme.HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#yd-dogrula-kilit"));

        dugme.Click();

        // Gerçek istek gitti.
        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "POST" && i.Yol.EndsWith("/yedekler/dogrulama-talep")));
        // Başarı bandı + role=alert sonuç satırı sunucunun yanıtını yazar (UX-S27).
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yd-basari-bandi")));
        Assert.Contains("dg1", e.Find("#yd-basari-bandi").TextContent);
        var sonuc = e.Find("#yd-dogrula-sonuc");
        Assert.Equal("alert", sonuc.GetAttribute("role"));
        Assert.Contains("kuyruğa alındı", sonuc.TextContent);
        // Yeniden yüklenen özet bekleyen talebi taşır → düğme SEBEBİYLE kilitli.
        e.WaitForAssertion(() => Assert.True(e.Find("#yd-dogrula").HasAttribute("disabled")));
        Assert.Contains("bekleyen", e.Find("#yd-dogrula-kilit").TextContent);
    }

    [Fact]
    public void Yedekler_BekleyenTalepVarken_DugmeKilitliVeSebepYazar()
    {
        bekleyenDogrulama = true;
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yd-dogrula")));

        var dugme = e.Find("#yd-dogrula");
        Assert.True(dugme.HasAttribute("disabled"));
        // UX-S30: sessiz kilit YASAK — sebep ve talep tarihi yazılı.
        var sebep = e.Find("#yd-dogrula-kilit").TextContent;
        Assert.Contains("bekleyen bir doğrulama", sebep);
        Assert.Contains("17.09.2026", sebep);

        // Kilitliyken istek GİTMEZ.
        var once = istekler.Count(i => i.Yol.EndsWith("/yedekler/dogrulama-talep"));
        dugme.Click();
        Assert.Equal(once, istekler.Count(i => i.Yol.EndsWith("/yedekler/dogrulama-talep")));
        Assert.Equal(0, once);
    }

    // ═════════════════════════════════════════════════════════════════════
    // PAKET P-76 — RPO aşımı: üç bantlı renk, role=alert uyarı, insan okunur
    // süre, ham dakika ipucunda. QA tur 8 ölçümü: 7985 dk / 360 dk (22 kat).
    // ═════════════════════════════════════════════════════════════════════

    private AngleSharp.Dom.IElement RpoKarti(IRenderedComponent<Kuyumcu.Web.Pages.Yedekler> e)
    {
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yd-rpo-kart")));
        return e.Find("#yd-rpo-kart");
    }

    private static string[] Siniflar(AngleSharp.Dom.IElement k) =>
        (k.GetAttribute("class") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

    [Fact]
    public void Yedekler_RpoIkiKattanFazlaAsim_KartKirmiziOlur()
    {
        rpoOlculen = 7985;   // tur 8 ölçümü: taahhüdün 22 katı
        var k = RpoKarti(Ciz());
        var s = Siniflar(k);
        Assert.Contains("stat-kart", s);
        Assert.Contains("kirmizi", s);
        Assert.DoesNotContain("yesil", s);
        Assert.DoesNotContain("sari", s);

        // Sınır: tam 2 kat = sarı; 2 katın 1 dakika üstü = kırmızı.
        rpoOlculen = 721;
        var k2 = RpoKarti(Ciz());
        Assert.Contains("kirmizi", Siniflar(k2));
    }

    [Fact]
    public void Yedekler_RpoHafifAsim_KartSariOlur()
    {
        rpoOlculen = 500;    // 360 < 500 <= 720
        var s = Siniflar(RpoKarti(Ciz()));
        Assert.Contains("sari", s);
        Assert.DoesNotContain("kirmizi", s);
        Assert.DoesNotContain("yesil", s);

        rpoOlculen = 720;    // tam 2 kat hâlâ sarı bant
        Assert.Contains("sari", Siniflar(RpoKarti(Ciz())));
    }

    [Fact]
    public void Yedekler_RpoTaahhutIcinde_KartYesilOlur()
    {
        rpoOlculen = 360;    // tam taahhüt = içinde
        var e = Ciz();
        var s = Siniflar(RpoKarti(e));
        Assert.Contains("yesil", s);
        Assert.DoesNotContain("kirmizi", s);
        Assert.DoesNotContain("sari", s);
        // Taahhüt içindeyken aşım uyarısı BASILMAZ.
        Assert.Empty(e.FindAll("#yd-rpo-uyari"));
        Assert.Empty(e.FindAll("#yd-rpo-olculmedi"));
    }

    [Fact]
    public void Yedekler_RpoAsimi_RoleAlertUyarisiGosterir()
    {
        rpoOlculen = 7985;
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yd-rpo-uyari")));
        var u = e.Find("#yd-rpo-uyari");
        Assert.Equal("alert", u.GetAttribute("role"));
        var metin = u.TextContent;
        Assert.Contains("22,2 kat", metin);            // 7985 / 360 = 22,18 → kaç kat
        Assert.Contains("5 gün 13 saat önce", metin);  // son başarılı yedek ne zaman
        Assert.Contains("6 saat", metin);              // taahhüt
        Assert.Contains("Veri kaybı riski", metin);

        // Hafif aşımda da (sarı bant) uyarı var ve katı yazar.
        rpoOlculen = 540;
        var e2 = Ciz();
        e2.WaitForAssertion(() => Assert.NotNull(e2.Find("#yd-rpo-uyari")));
        Assert.Equal("alert", e2.Find("#yd-rpo-uyari").GetAttribute("role"));
        Assert.Contains("1,5 kat", e2.Find("#yd-rpo-uyari").TextContent);
    }

    [Fact]
    public void Yedekler_RpoOlculmedi_YesilGosterilmez()
    {
        rpoOlculen = null;   // hiç başarılı yedek yok
        var e = Ciz();
        var k = RpoKarti(e);
        var s = Siniflar(k);
        Assert.DoesNotContain("yesil", s);
        Assert.Contains("sari", s);                    // bilinmeyen = uyarı, nötr/yeşil değil
        Assert.Contains("ölçülmedi", e.Find("#yd-rpo-deger").TextContent);
        // Aşım uyarısı değil, ayrı "bilinmiyor" uyarısı.
        Assert.Empty(e.FindAll("#yd-rpo-uyari"));
        var o = e.Find("#yd-rpo-olculmedi");
        Assert.Equal("alert", o.GetAttribute("role"));
        Assert.Contains("Hiç başarılı yedek alınmadı", o.TextContent);
    }

    [Fact]
    public void Yedekler_RpoSuresi_InsanOkunurBicimde()
    {
        rpoOlculen = 7985;
        var e = Ciz();
        var k = RpoKarti(e);
        var deger = e.Find("#yd-rpo-deger").TextContent.Trim();
        Assert.Equal("5 gün 13 saat", deger);
        Assert.DoesNotContain("7985", k.TextContent);  // ham dakika görünür metinde YOK
        // Ham dakika ipucunda (title) kalır.
        var ipucu = k.GetAttribute("title") ?? "";
        Assert.Contains("7985 dk", ipucu);
        Assert.Contains("360 dk", ipucu);
        // Alt satırdaki taahhütler de insan okunur.
        Assert.Contains("6 saat", k.TextContent);
        Assert.Contains("2 saat", k.TextContent);      // RTO taahhüdü 120 dk

        // Saf biçimlendirici sınırları (Bicim.Sure).
        Assert.Equal("42 dk", Bicim.Sure(42, "gün", "saat", "dk"));
        Assert.Equal("1 saat 35 dk", Bicim.Sure(95, "gün", "saat", "dk"));
        Assert.Equal("6 saat", Bicim.Sure(360, "gün", "saat", "dk"));
        Assert.Equal("1 gün", Bicim.Sure(1440, "gün", "saat", "dk"));
        Assert.Equal("0 dk", Bicim.Sure(-5, "gün", "saat", "dk"));
    }

    /// <summary>
    /// Canlı ölçümle aynı değerler (2026-09-24 01:36 UTC, port 5184,
    /// GET /yedekler/ozet → rpoDakikaOlculen=381, rpoDakikaTaahhut=360):
    /// hafif aşım → sarı kart, "6 saat 21 dk", "1,1 kat" uyarısı.
    /// </summary>
    [Fact]
    public void Yedekler_RpoCanliOlcum381_SariVeUyari()
    {
        rpoOlculen = 381;
        var e = Ciz();
        var k = RpoKarti(e);
        Assert.Contains("sari", Siniflar(k));
        Assert.Equal("6 saat 21 dk", e.Find("#yd-rpo-deger").TextContent.Trim());
        Assert.Contains("381 dk", k.GetAttribute("title") ?? "");
        var u = e.Find("#yd-rpo-uyari");
        Assert.Equal("alert", u.GetAttribute("role"));
        Assert.Contains("1,1 kat", u.TextContent);
        Assert.Contains("6 saat 21 dk önce", u.TextContent);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// KENAR ÇUBUĞU — /yedekler bağlantısı YALNIZ SAHİP'te ve YALNIZ profesyonel
// modda görünür (BasitMenu listesine EKLENMEDİ).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class YedekMenuTestleri : BunitContext
{
    private sealed class SahteYetkiApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"moduller":["cari","urun","islem","kasa","banka","fatura","rapor","kullanici"]}""",
                    System.Text.Encoding.UTF8, "application/json"),
            });
    }

    public YedekMenuTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteYetkiApi()) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        // P-36b kapanışı (P-37 artığı): KenarCubugu favori/sıra tercihini bu
        // servisten okur; Program.cs'te kayıtlıydı, fikstürlerde DEĞİLDİ →
        // kabuk çizen her test DI hatasıyla kırmızıydı.
        Services.AddScoped<MenuTercihServisi>();
    }

    /// <summary>
    /// TEMA TEMİZLİĞİ 17.10: kenar çubuğu kaldırıldı — menünün bütünü "Tüm ekranlar" sayfasıdır
    /// (üst menünün son sözcüğü). Yedekler karesinin varlığı orada sınanır.
    /// </summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Ekranlar> Ciz(string rol, string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Ekranlar>().KareleriBekle();
        Assert.Contains("/", e.KareYollari());   // sayfa gerçekten çizildi (yanlış yeşil yok)
        return e;
    }

    [Fact]
    public void Menu_SahipDisindaGorunmez_BasitModdaDaGorunur()
    {
        Assert.Contains("/yedekler", Ciz("SAHIP", "PROFESYONEL").KareYollari());
        Assert.DoesNotContain("/yedekler", Ciz("TEZGAHTAR", "PROFESYONEL").KareYollari());
        Assert.DoesNotContain("/yedekler", Ciz("MUDUR", "PROFESYONEL").KareYollari());
        // S1-g: basit mod menüyü DARALTMAZ; yetki (yalnız SAHİP) aynen durur.
        Assert.Contains("/yedekler", Ciz("SAHIP", "BASIT").KareYollari());
        Assert.DoesNotContain("/yedekler", Ciz("MUDUR", "BASIT").KareYollari());
    }
}
