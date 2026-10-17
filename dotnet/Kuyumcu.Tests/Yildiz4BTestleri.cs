using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CariListeVeri = Kuyumcu.Contracts.Sayfalar.Cariler.CariVeri;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-4B — "Yıldız" BİREBİR: Tezgâh (Tezgah4/4a) · Müşteriler (Musteri4/4a) ·
// Tüm ekranlar (Tum4/4a).
//
// Görsel eşitliğin ASIL kanıtı gerçek Chrome'daki tuval-kiyas.mjs ölçümüdür (rapor:
// ham/yildiz-4b/r4-*-tablo.md); bu sınıf o ölçümün dayandığı SÖZLEŞMEYİ kilitler:
//   • saf: kalem/toplam yazımı (tuval "2 adet · 1,00 g · 1,9900 g has"), üst satır,
//     müşteri satırı, sistem carisinin listede olmaması, "Gider ve virman" kare adı;
//   • bUnit: Yıldız'da Satış = tuval düzeni (BASİT ve PRO), başka şemada ve başka
//     türde özgün fiş; düğmeler gerçek geri çağrı ya da kilit + sebep; satış gövdesi
//     Yıldız'da ve platin'de BİREBİR aynı (yalnız görünüm değişti);
//   • CSS: tuval değerleri ve kapsam (öteki 8 şemada etki 0).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class Yildiz4BTestleri : BunitContext
{
    private string sema = "yildiz";
    private string? satisHatasi;          // tur 2: POST /islemler/satis 400 mesajı (null = 200)
    private decimal kimlikEsigi = 185000m; // tur 2: sahte /masak/esikler kimlik eşiği
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private sealed class SahteApi(Yildiz4BTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            if (t.satisHatasi is { } sh && yol.Contains("/islemler/satis") && istek.Method == HttpMethod.Post)
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { message = sh }), System.Text.Encoding.UTF8, "application/json"),
                };
            var y = yol switch
            {
                var v when v.Contains("/gorunum") => $$"""{"sema":"{{t.sema}}","yaziOlcegi":"18","temaModu":"karanlik"}""",
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Gram altın","ayarId":"K24","ayar":"24","hasGrami":0.995,"iscilikTl":0,"satisFiyati":10000,"birim":"ADET","milyem":995,"gramAgirlik":1}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari/sistem-carisi") => """{"id":"p1","ad":"Peşin Müşteri"}""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Selin Ergüven"}]""",
                var v when v.Contains("/masak/gereksinim") && t.kimlikEsigi < 185000m => """{"kademe":"YALNIZ_KIMLIK","kimlikGerekli":true,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => $$"""{"kimlikEsigi":{{t.kimlikEsigi}},"amacBeyaniEsigi":{{t.kimlikEsigi}},"beyanFormuEsigi":{{t.kimlikEsigi}},"ekBelgeEsigi":{{t.kimlikEsigi}},"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var v when v.Contains("/pos/cihazlar") => "[]",
                var v when v.Contains("/islemler/satis") && istek.Method == HttpMethod.Post => """{"id":"h1","hareketId":"h1","fisNo":"F-0001","fisId":"f1"}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    public Yildiz4BTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Satis(string mod, string s = "yildiz")
    {
        sema = s;
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Hakan", "Tunalı", "h@ornek.com", "SAHIP", "s1", mod, mod));
        Services.GetRequiredService<GorunumServisi>().YukleAsync().GetAwaiter().GetResult();
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement(s == "yildiz" ? "#yildiz-tezgah" : "#islem-kaydet", TimeSpan.FromSeconds(10));
        return e;
    }

    private static void KalemEkle(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
    }

    /// <summary>POST /islemler/satis gövdesi; istemci üretimi kimlikler (GUID/ULID biçimli dizgeler) çıkarılır.</summary>
    private string SatisGovdesi()
    {
        string? g = null;
        Assert.True(SpinWait.SpinUntil(() =>
        {
            lock (istekler) g = istekler.LastOrDefault(i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")).Govde;
            return g is not null;
        }, TimeSpan.FromSeconds(10)), "satış POST'u gelmedi");
        var n = JsonNode.Parse(g!)!;
        void Temizle(JsonNode? d)
        {
            if (d is JsonObject o)
            {
                foreach (var k in o.Select(p => p.Key).ToList())
                {
                    if (o[k] is JsonValue v && v.TryGetValue<string>(out var str)
                        && Regex.IsMatch(str, "^([0-9a-f]{8}-[0-9a-f]{4}-|[0-9A-HJKMNP-TV-Z]{26}$)", RegexOptions.IgnoreCase)) o.Remove(k);
                    else Temizle(o[k]);
                }
            }
            else if (d is JsonArray a) foreach (var x in a) Temizle(x);
        }
        Temizle(n);
        return n.ToJsonString();
    }

    private static string Css() => CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tema-yildiz.css");

    /// <summary>tema-yildiz.css'in YILDIZ-4B bölümü (yorumsuz).</summary>
    private static string Blok4B()
    {
        var c = Css();
        var i = c.IndexOf("PAKET YILDIZ-4B — TEZGÂH", StringComparison.Ordinal);
        Assert.True(i > 0, "tema-yildiz.css'te YILDIZ-4B bölümü yok");
        return Regex.Replace(c[c.LastIndexOf("/*", i, StringComparison.Ordinal)..], @"/\*.*?\*/", "", RegexOptions.Singleline);
    }

    private static string Kural(string blok, string secici)
    {
        var i = blok.IndexOf(secici + " {", StringComparison.Ordinal);
        Assert.True(i >= 0, "kural yok: " + secici);
        var a = blok.IndexOf('{', i);
        return blok[(a + 1)..blok.IndexOf('}', a)];
    }

    private static UrunVeri GramAltin => new("u1", "Gram altın", "K24", "24", 0.995m, 0m, 10000m, "ADET", 995m, 1m);

    /// <summary>Seçici listesini YALNIZ en dış virgülden böler (":is(p, h1)" bölünmez).</summary>
    private static IEnumerable<string> Ayir(string liste)
    {
        var derin = 0; var son = 0;
        for (var i = 0; i < liste.Length; i++)
        {
            if (liste[i] == '(') derin++;
            else if (liste[i] == ')') derin--;
            else if (liste[i] == ',' && derin == 0) { yield return liste[son..i]; son = i + 1; }
        }
        yield return liste[son..];
    }

    // ── SAF ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Y4B_01_kalem_ayrintisi_tuval_yazimi_birim_gram_toplam_has()
    {
        var k = new FisKalemi { UrunId = "u1", UrunAdi = "Gram altın", Adet = 2, BirimFiyat = 6922m };
        Assert.Equal("2 adet · 1,00 g · 1,9900 g has", YildizTezgahBicim.KalemAyrinti(k, GramAltin, "tr"));
        // kart yoksa yalnız adet (uydurulmaz)
        Assert.Equal("2 adet", YildizTezgahBicim.KalemAyrinti(k, null, "tr"));
        Assert.Equal("2 pcs · 1,00 g · 1,9900 g fine", YildizTezgahBicim.KalemAyrinti(k, GramAltin, "en"));
    }

    [Fact]
    public void Y4B_02_toplam_ayrintisi_ve_ust_satir()
    {
        Assert.Equal("3,75 g · 3,5930 g has", YildizTezgahBicim.ToplamAyrinti(3.75m, 3.593m, "tr"));
        Assert.Equal("3,75 g", YildizTezgahBicim.ToplamAyrinti(3.75m, null, "tr"));
        Assert.Equal("Satış · Selin Ergüven · Borç yok", YildizTezgahBicim.UstSatir("Satış", "Selin Ergüven", "Borç yok"));
        Assert.Equal("Satış", YildizTezgahBicim.UstSatir("Satış", null, null));
        Assert.Equal("Borç yok", YildizTezgahBicim.MusteriDurumu(new CariVeri("c1", "Selin Ergüven"), "tr"));
        Assert.Null(YildizTezgahBicim.MusteriDurumu(new CariVeri("p", "Peşin Müşteri", SistemCarisi: true), "tr"));
    }

    [Fact]
    public void Y4B_03_musteri_satiri_tuval_yazimi()
    {
        var anadolu = new CariListeVeri("c6", "Anadolu Döküm Toptan Kuyum", "TEDARIKCI", "05320000106", null, -3840m, 0m);
        var s = YildizMusteriBicim.Satir(anadolu, null, null, "0532 000 01 06", "tr");
        Assert.Equal("−3.840 ₺", s.Tutar.Replace('\u00A0', ' ').Replace('\u202F', ' '));
        Assert.True(s.Borc);
        Assert.Equal("biz borçluyuz", s.Durum);
        var tuna = new CariListeVeri("c2", "Tuna Akarsu", "MUSTERI", null, null, 22174.73m, 0m);
        Assert.Equal("bize borçlu · 15 gün", YildizMusteriBicim.Satir(tuna, null, 15, "0532 000 01 02", "tr").Durum);
        var derya = new CariListeVeri("c1", "Derya Yıldızlı", "MUSTERI", null, null, 0m, 0m, EmanetteHas: 9.16m);
        Assert.Equal("emanette 9,16 g has", YildizMusteriBicim.Satir(derya, null, null, "0532 000 01 01", "tr").Durum);
    }

    [Fact]
    public void Y4B_04_listede_sistem_carisi_yok_ust_sayida_var()
    {
        CariListeVeri C(string id, string ad, bool sistem = false) => new(id, ad, "MUSTERI", null, null, 0m, 0m, SistemCarisi: sistem);
        var liste = new[] { C("c1", "Derya"), C("p", "Peşin Müşteri", true), C("c2", "Selin") };
        var gorunen = Kuyumcu.Web.Pages.Cariler.YildizGorunen(liste, Kuyumcu.Web.Pages.Cariler.SegHepsi);
        Assert.Equal(["Derya", "Selin"], gorunen.Select(c => c.Ad));
        Assert.Equal("3 müşteri · 0 bize borçlu · 0 biz borçluyuz", YildizMusteriSozlugu.Ust(liste.Length, 0, 0, "tr"));
    }

    [Fact]
    public void Y4B_05_kare_adi_tuval_metni_gider_ve_virman()
    {
        Assert.Equal("Gider ve virman", YildizEkranSozlugu.KareAdi("/gider", "Gider ve para aktarımı", "tr"));
        Assert.Equal("Expenses", YildizEkranSozlugu.KareAdi("/gider", "Expenses", "en"));
        Assert.Equal("Kasalar", YildizEkranSozlugu.KareAdi("/kasalar", "Kasalar", "tr"));
    }

    // ── bUnit: bileşenler ────────────────────────────────────────────────────

    private (YildizTezgahBaglami B, List<string> Cagri) Baglam(bool kalemVar = true, bool karma = false, bool kilitli = false)
    {
        var cagri = new List<string>();
        EventCallback C(string ad) => EventCallback.Factory.Create(this, () => cagri.Add(ad));
        return (new YildizTezgahBaglami
        {
            TurAdi = "Satış", MusteriAdi = "Selin Ergüven", MusteriDurumu = "Borç yok",
            Kalemler = kalemVar ? [new(0, "24", "Gram altın", "2 adet · 1,00 g · 1,9900 g has", "13.844,00 ₺"),
                                   new(1, "22", "Çeyrek altın", "1 adet · 1,75 g · 1,6030 g has", "11.180,00 ₺")] : [],
            ToplamAyrinti = "3,75 g · 3,5930 g has", ToplamMetni = "25.024,00 ₺",
            Odemeler = [new(0, "Nakit", "Ana Kasa", "15.000,00 ₺"), new(1, "Kart", "Ana Kasa", "10.024,00 ₺")],
            Karma = karma, FarkMetni = "0 ₺", FarkTamam = true,
            Not = "Yasal kurallar uygun · nakit 30.000 ₺ sınırının altında",
            KaydetEtiketi = "Kaydet ve yazdır", KaydetKilitli = kilitli, KaydetKilitSebebi = kilitli ? "Kasa seçilmedi" : null,
            FisDolu = kalemVar, KalemVar = kalemVar, ParaUstuKullanilir = kalemVar && !karma,
            Kaydet = C("kaydet"), Vazgec = C("vazgec"), BaskaOdeme = C("baska"), TekOdeme = C("tek"),
            Indirim = C("indirim"), AcikHesap = C("acik"), MusteriDegistir = C("musteri"), TurDegistir = C("tur"),
        }, cagri);
    }

    [Fact]
    public void Y4B_10_tezgah_tuval_duzeni_ve_metinleri()
    {
        var (b, _) = Baglam();
        var e = Render<YildizTezgah>(p => p.Add(x => x.B, b));
        Assert.Equal("Satış · Selin Ergüven · Borç yok", e.Find("#yildiz-tezgah-ust").TextContent);
        Assert.Equal("Değiştir", e.Find("#yildiz-tezgah-degistir").TextContent.Trim());
        Assert.Equal("24", e.Find("#yildiz-kalem-0 .yz-tezgah-ayar").TextContent);
        Assert.Equal("Gram altın", e.Find("#yildiz-kalem-0 .yz-tezgah-ad").TextContent);
        Assert.Equal("13.844,00 ₺", e.Find("#yildiz-kalem-0 .yz-tezgah-tutar").TextContent);
        // kuruş + ₺ AYRI (soluk, yarı boy) öğe
        Assert.Equal("25.024", e.Find("#yildiz-tezgah-toplam-rakam .yz-tezgah-tam").TextContent);
        Assert.Equal(",00 ₺", e.Find("#yildiz-tezgah-toplam-rakam .yz-tezgah-kurus").TextContent);
        Assert.Contains("yz-s", e.Find("#yildiz-tezgah-toplam-rakam .yz-tezgah-kurus").ClassList);
        // "Nakit · Ana Kasa": ad metni boşlukla biter (tuval metin düğümü "Nakit ")
        Assert.Equal("Nakit · Ana Kasa", e.Find("#yildiz-odeme-0 .yz-tezgah-odeme-ad").TextContent);
        Assert.Equal(["+ Başka ödeme", "+ Para üstü", "+ İndirim"], e.FindAll("#yildiz-tezgah-ekler .yz-b3").Select(x => x.TextContent.Trim()));
        Assert.Equal("0 ₺", e.Find("#yildiz-tezgah-fark-tutar").TextContent.Trim());
        Assert.NotEmpty(e.FindAll("#yildiz-tezgah-fark-tutar svg.yz-tezgah-onay"));
        Assert.Equal("Kaydet ve yazdır", e.Find("#yildiz-tezgah-kaydet").TextContent.Trim());
        Assert.Contains("yz-b1", e.Find("#yildiz-tezgah-kaydet").ClassList);
        Assert.Contains("yz-b2", e.Find("#yildiz-tezgah-vazgec").ClassList);
    }

    [Fact]
    public void Y4B_11_her_dugme_gercek_geri_cagri_ya_da_kilit_sebebi()
    {
        var (b, cagri) = Baglam();
        var e = Render<YildizTezgah>(p => p.Add(x => x.B, b));
        e.Find("#yildiz-tezgah-kaydet").Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.Find("#yildiz-tezgah-vazgec").Click();
        e.Find("#yildiz-tezgah-baska-odeme").Click();
        e.Find("#yildiz-tezgah-indirim").Click();
        e.Find("#yildiz-tezgah-degistir").Click();
        e.Find("#yildiz-tezgah-tur").Click();
        Assert.Equal(["kaydet", "vazgec", "baska", "indirim", "musteri", "tur"], cagri);
        // + Para üstü: istek atmaz, sayfanın mevcut para üstü hesabını açar
        e.Find("#yildiz-tezgah-para-ustu").Click();
        Assert.Equal("true", e.Find("#yildiz-tezgah-para-ustu").GetAttribute("aria-expanded"));
        Assert.NotEmpty(e.FindAll("#yildiz-tezgah-para-ustu-alan"));
        // kalem yokken kilitli düğmeler sebebini söyler (UX-S30)
        var (bos, _) = Baglam(kalemVar: false, kilitli: true);
        var e2 = Render<YildizTezgah>(p => p.Add(x => x.B, bos));
        foreach (var id in new[] { "#yildiz-tezgah-baska-odeme", "#yildiz-tezgah-para-ustu", "#yildiz-tezgah-indirim", "#yildiz-tezgah-kaydet" })
        {
            Assert.True(e2.Find(id).HasAttribute("disabled"), id);
            Assert.False(string.IsNullOrWhiteSpace(e2.Find(id).GetAttribute("title")), id + " sebepsiz kilit");
        }
        Assert.Equal("Satışa başlamak için barkodu okut.", e2.Find("#yildiz-tezgah-bos").TextContent);
    }

    [Fact]
    public void Y4B_12_karma_odemede_tek_odemeye_donus()
    {
        var (b, cagri) = Baglam(karma: true);
        var e = Render<YildizTezgah>(p => p.Add(x => x.B, b));
        // YILDIZ-4C (hakem 4B tur 2 KRİTİK "ödemeler iki kez"): karmada özet satırları ÇİZİLMEZ (satırların kendisi düzenlenir);
        // "+ Başka ödeme" karmada da var (yeni satır), "− Tek ödeme" tuvalin üç hapından sonra.
        Assert.Empty(e.FindAll(".yz-tezgah-odeme"));
        Assert.Single(e.FindAll("#yildiz-tezgah-baska-odeme"));
        e.Find("#yildiz-tezgah-tek-odeme").Click();
        Assert.Equal(["tek"], cagri);
    }

    [Fact]
    public void Y4B_13_musteriler_tuval_duzeni_ve_eylemler()
    {
        var secilen = new List<string>();
        var b = new YildizMusteriBaglami
        {
            Ust = "7 müşteri · 2 alacak · 1 borç",
            Satirlar = [new("c6", "Anadolu Döküm Toptan Kuyum", "Toptancı · 0532 000 01 06", "−3.840 ₺", true, "biz borçluyuz"),
                        new("c2", "Tuna Akarsu", "0532 000 01 02", "22.174,73 ₺", false, "bize borçlu · 15 gün")],
            Sekmeler = [new("hepsi", "HEPSİ", true), new("borclu", "BORÇLU", false), new("toptanci", "TOPTANCI", false)],
            SekmeSec = EventCallback.Factory.Create<string>(this, k => secilen.Add("sekme:" + k)),
            SatirAc = EventCallback.Factory.Create<string>(this, k => secilen.Add("satir:" + k)),
            YeniMusteri = EventCallback.Factory.Create(this, () => secilen.Add("yeni")),
        };
        var e = Render<YildizMusteriler>(p => p.Add(x => x.B, b));
        Assert.Equal("7 müşteri · 2 alacak · 1 borç", e.Find("#yildiz-musteri-ust").TextContent);
        Assert.Equal("Müşteriler", e.Find("#yildiz-musteri-baslik").TextContent);
        Assert.Equal("Yeni müşteri", e.Find("#cari-yeni-ac").TextContent.Trim());
        Assert.Contains("yz-b2", e.Find("#cari-segment-hepsi").ClassList);
        Assert.Contains("yz-kotu", e.Find("#yildiz-musteri-c6 .yz-musteri-tutar").ClassList);
        Assert.DoesNotContain("yz-kotu", e.Find("#yildiz-musteri-c2 .yz-musteri-tutar").ClassList);
        // YILDIZ-4C (hakem 4B tur 2 DÜŞÜK "alt not tuvalde var"): tuvalin alt satırı AYNEN.
        Assert.Equal("Satıra dokununca kart açılır: tahsilat, ekstre, düzenleme.", e.Find("#yildiz-musteri-not").TextContent);
        e.Find("#cari-segment-borclu").Click();
        e.Find("#yildiz-musteri-c2").Click();
        e.Find("#cari-yeni-ac").Click();
        // YILDIZ-4D: "+ Süzgeç" tuvalde yok → ekranda yok (Yildiz4DTestleri.Y4D_01).
        Assert.Empty(e.FindAll("#cari-suzgec-daha"));
        Assert.Equal(["sekme:borclu", "satir:c2", "yeni"], secilen);
    }

    [Fact]
    public void Y4B_14_tum_ekranlar_39_kare_6_bolum_tuval_metni()
    {
        var e = Render<YildizEkranlar>(p => p
            .Add(x => x.Ogeler, MenuTanimi.Ogeler)
            .Add(x => x.Ozet, YildizSozlugu.Ozet("tr", MenuTanimi.Ogeler.Count, 6, true)));
        // 39 → 41: PAKET EKSIK-B1-SATINALMA — /satin-alma (Müşteri ve stok 6 → 7) + /irsaliyeler (Belge ve uyum 4 → 5).
        Assert.Equal(41, e.FindAll(".yz-kare").Count);
        Assert.Equal(6, e.FindAll(".yz-tum-bolum").Count);
        Assert.Equal("41 ekran · 6 bölüm · hiçbiri gizli değil", e.Find("#ekranlar-yildiz-ozet").TextContent);
        Assert.Equal(["5", "7", "10", "5", "6", "8"],
            e.FindAll(".yz-tum-bolum > .yz-ust").Select(h => h.TextContent.Split('·').Last().Trim()));
        Assert.Equal("Gider ve virman", e.Find("#ekran-karo-gider .yz-kad").TextContent);
        Assert.All(e.FindAll(".yz-kare svg path"), p => Assert.False(string.IsNullOrEmpty(p.GetAttribute("d"))));
        var metin = e.Markup;
        Assert.DoesNotContain("YETKİN OLANLAR", metin, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ekranın hepsi burada", metin);
        Assert.Empty(e.FindAll("#yildiz-tum-not"));   // tur 2: tasarım notu ekrana konmaz (paket koşulu)
        Assert.DoesNotContain("Fareyle üstüne", metin);
    }

    // ── bUnit: Satış sayfası (Islemler) ──────────────────────────────────────

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Y4B_20_yildiz_satis_tuval_yuzu_ozgun_fis_parcalari_cizilmez(string mod)
    {
        var e = Satis(mod);
        Assert.Contains("yz-islem", e.Find(".islem-sayfa").ClassList);
        Assert.Single(e.FindAll("#yildiz-tezgah"));
        Assert.Single(e.FindAll("#islem-barkod"));   // tek giriş, tuvalin barkod alanında
        Assert.NotNull(e.Find("#yildiz-tezgah-giris #islem-barkod"));
        foreach (var yok in new[] { "#islem-kaydet", "#islem-alt-satir", "#islem-pro-odeme", "#fis-pro-yan-sutun" })
            Assert.Empty(e.FindAll(yok));
        Assert.Empty(e.FindAll(".fis-tuval-izgara"));   // FisVerdimAldim çizilmez
    }

    [Fact]
    public void Y4B_22_yildizda_alis_turu_ozgun_fis()
    {
        var e = Satis(ArayuzModuServisi.Basit);
        e.Find("#yildiz-tezgah-tur").Click();
        e.WaitForAssertion(() => Assert.Contains("yz-turler-acik", e.Find(".islem-sayfa").ClassList));
        e.FindAll("button[id^='islem-tur-']").First(x => x.Id != "islem-tur-SATIS").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#yildiz-tezgah")));
        Assert.Single(e.FindAll("#islem-kaydet"));
    }

    // TEMA TEMİZLİĞİ 17.10: Y4B_21_baska_semada_ozgun_satis_fisi_aynen kaldırıldı — "başka şema" yok; SATIŞ fişi her
    // zaman Yıldız tezgâhıdır.
    //
    // Y4B_23 — eskiden Yıldız gövdesi Platin'in gövdesiyle KIYASLANIRDI. Tek şemada kıyas nesnesi kalmadı: gövde,
    // tema temizliği öncesi Platin'in ürettiği (ve Yıldız'ın birebir aynısı olduğu bu testle kanıtlanmış) değerle
    // SABİTLENDİ — aynı güvence (tezgâh yüzü gövdeyi değiştirmez) artık kayıtlı beklenen değerle korunur.
    private static readonly Dictionary<string, string> Y4B23Beklenen = new()
    {
        [ArayuzModuServisi.Basit] = "{\"cariId\":\"p1\",\"urunId\":\"u1\",\"adet\":1,\"birimFiyatTl\":10000,\"odemeTuru\":\"NAKIT\",\"kasaId\":\"ks1\",\"kurSnapshotId\":\"kur-1\",\"aciklama\":null,\"masak\":null,\"reyonId\":null}",
        [ArayuzModuServisi.Profesyonel] = "{\"cariId\":\"p1\",\"urunId\":\"u1\",\"adet\":1,\"birimFiyatTl\":10000,\"odemeTuru\":\"NAKIT\",\"kasaId\":\"ks1\",\"vadeGun\":null,\"kurSnapshotId\":\"kur-1\",\"aciklama\":null,\"masak\":null,\"reyonId\":null}",
    };

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Y4B_23_satis_govdesi_tema_temizligi_oncesi_ozgun_govdeyle_birebir(string mod)
    {
        var y = Satis(mod);
        KalemEkle(y);
        y.WaitForAssertion(() => Assert.Single(y.FindAll(".yz-tezgah-kalem")), TimeSpan.FromSeconds(5));
        Assert.Equal("Gram altın", y.Find("#yildiz-kalem-0 .yz-tezgah-ad").TextContent);
        Assert.Equal("1 adet · 1,00 g · 0,9950 g has", y.Find("#yildiz-kalem-0 .yz-tezgah-ayrinti").TextContent);
        y.WaitForAssertion(() => Assert.False(y.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled")), TimeSpan.FromSeconds(5));
        y.Find("#yildiz-tezgah-kaydet").Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(y);
        Assert.Equal(Y4B23Beklenen[mod], SatisGovdesi());
    }

    [Fact]
    public void Y4B_24_degistir_musteri_satirini_acar_kapatir()
    {
        var e = Satis(ArayuzModuServisi.Basit);
        Assert.DoesNotContain("yz-musteri-acik", e.Find(".islem-sayfa").ClassList);
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForAssertion(() => Assert.Contains("yz-musteri-acik", e.Find(".islem-sayfa").ClassList));
        Assert.Single(e.FindAll("#yildiz-musteri-secici"));   // YILDIZ-4C: tuval dilinde seçici (eski fiş üst satırı yok)
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForAssertion(() => Assert.DoesNotContain("yz-musteri-acik", e.Find(".islem-sayfa").ClassList));
    }

    // ── CSS ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Y4B_30_tezgah_css_tuval_degerleri()
    {
        var b = Blok4B();
        const string K = "html:root[data-sema=\"yildiz\"] ";
        var ana = Kural(b, K + ".yz-tezgah .yz-tezgah-ana");
        Assert.Contains("grid-template-columns: minmax(0, 1fr) 380px;", ana);
        Assert.Contains("gap: 80px;", ana);
        Assert.Contains("padding: 48px 64px 40px;", ana);
        Assert.Contains("font-size: 72px;", Kural(b, K + ".yz-tezgah .yz-tezgah-toplam-rakam"));
        Assert.Contains("font-size: 36px;", Kural(b, K + ".yz-tezgah .yz-tezgah-kurus"));
        Assert.Contains("font-size: 40px;", Kural(b, K + ".yz-tezgah .yz-tezgah-ayar"));
        Assert.Contains("grid-template-columns: 72px minmax(0, 1fr) auto;", Kural(b, K + ".yz-tezgah .yz-tezgah-kalem"));
        Assert.Contains("font-size: 56px;", Kural(b, K + ".yz-tezgah .yz-tezgah-fark-rakam"));
        Assert.Contains("border-top: 1px solid var(--kenarlik);", Kural(b, K + ".yz-tezgah .yz-tezgah-fark"));
        Assert.Contains("font-weight: 200;", Kural(b, K + ".yz .yz-ince"));
        Assert.Contains("padding: 1px 2px;", Kural(b, K + ".yz-tezgah .yz-tezgah-giris .barkod-satiri #islem-barkod.girdi-arama"));
    }

    [Fact]
    public void Y4B_31_css_kapsam_ve_belirtec()
    {
        var b = Blok4B();
        foreach (Match m in Regex.Matches(b, @"(?m)^\s*([^@{}\s][^{}]*)\{"))
            foreach (var s in Ayir(m.Groups[1].Value))
                if (!string.IsNullOrWhiteSpace(s) && !Regex.IsMatch(s.Trim(), @"^(from|to|\d+%)$"))
                    Assert.True(Regex.IsMatch(s.Trim(), @"^html:root(:not\(\[data-tema=""aydinlik""\]\)|\[data-tema=""karanlik""\])?\[data-sema=""yildiz""\]"), "kapsamsız seçici: " + s.Trim());
        Assert.DoesNotMatch(@"#[0-9A-Fa-f]{3,8}\b(?![\w-])(?<!#islem)", Regex.Replace(b, @"#[a-z][\w-]*", ""));
        Assert.Contains("grid-template-columns: repeat(8, minmax(0, 1fr));", Kural(b, "html:root[data-sema=\"yildiz\"] .yz-tum .yz-tum-izgara"));
    }

    // ── tur 2 (tasarım hakemi KRİTİK bulguları) ──────────────────────────────

    [Fact]
    public void Y4B_40_alan_odakta_kutu_cercevesi_yok_alt_cizgi_metin_rengi()
    {
        var b = Blok4B();
        var k = Kural(b, "html:root[data-sema=\"yildiz\"] .yz .yz-alan:focus-within");
        Assert.Contains("outline: 0;", k);
        Assert.Contains("border-bottom-color: var(--kenarlik);", k);   // YILDIZ-4C (hakem 4B tur 2): tuvalde odak hâli yok — çizgi --p kalır
        Assert.DoesNotContain("odak-halka", k);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Y4B_41_kaydet_hatasi_fark_blogunda_gorunur_ve_odak_alir(string mod)
    {
        satisHatasi = "Satış fişi (Gram altın): yetersiz satılabilir stok";
        var e = Satis(mod);
        KalemEkle(e);
        e.WaitForAssertion(() => Assert.False(e.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled")), TimeSpan.FromSeconds(5));
        e.Find("#yildiz-tezgah-kaydet").Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-tezgah-fark #yildiz-tezgah-hata")), TimeSpan.FromSeconds(5));
        var h = e.Find("#yildiz-tezgah-hata");
        Assert.Contains("yetersiz satılabilir stok", h.TextContent);
        Assert.Contains("yz-kotu", h.ClassList);
        Assert.Equal("alert", h.GetAttribute("role"));
        Assert.Equal("-1", h.GetAttribute("tabindex"));
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "kuyumcuOdak.alana" && Equals(i.Arguments[0], "yildiz-tezgah-hata"));
        // Sunucu hatası varken genel "yasal kapı" cümlesi Fark bloğuna yazılmaz (gerçek sebep hata satırında).
        Assert.DoesNotContain("yasal bir kapıya", e.Find("#yildiz-tezgah-fark").TextContent);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Y4B_42_basarili_satis_son_fis_ve_yazdir_fark_blogunda(string mod)
    {
        var e = Satis(mod);
        KalemEkle(e);
        e.WaitForAssertion(() => Assert.False(e.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled")), TimeSpan.FromSeconds(5));
        e.Find("#yildiz-tezgah-kaydet").Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-tezgah-fark #yildiz-tezgah-son-fis")), TimeSpan.FromSeconds(5));
        Assert.StartsWith("Son fiş:", e.Find("#yildiz-tezgah-son-fis").TextContent);
        Assert.Empty(e.FindAll("#yildiz-tezgah-hata"));
        var y = e.Find("#yildiz-tezgah-fark #yildiz-tezgah-yazdir");
        Assert.Equal("_blank", y.GetAttribute("target"));
        Assert.StartsWith("/cikti/", y.GetAttribute("href"));
    }

    [Fact]
    public void Y4B_43_ozgun_bantlar_yildiz_islemde_gizli_yasal_kapi_not_satirinda()
    {
        var b = Blok4B();
        var i = b.IndexOf(".yz-islem :is(#hata-bandi", StringComparison.Ordinal);
        Assert.True(i > 0, "özgün bant gizleme kuralı yok");
        var kural = b[i..b.IndexOf('}', i)];
        foreach (var id in new[] { "#hata-bandi", "#basari-bandi", "#islem-yazdir-sar", "#islem-yasal-kapi", "#islem-yerlesim-uyari" })
            Assert.Contains(id, kural);
        Assert.Contains("display: none;", kural);
    }

    [Fact]
    public void Y4B_44_degistir_katmani_musteri_seciciyi_tasir_sayfa_ustune_acilmaz()
    {
        var e = Satis(ArayuzModuServisi.Basit);
        Assert.Empty(e.FindAll("#islem-cari"));
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-tezgah-musteri #yildiz-musteri-secici")));   // YILDIZ-4C
        Assert.Empty(e.FindAll("#islem-ust-satir, #islem-cari"));
        Assert.Single(e.FindAll(".yz-tezgah-sol > #yildiz-tezgah-musteri"));
        var css = Kural(Blok4B(), "html:root[data-sema=\"yildiz\"] .yz-tezgah .yz-tezgah-musteri");
        Assert.Contains("position: static;", css);   // YILDIZ-4C tur 3 (hakem KRİTİK): katman akışta — kalemleri örtmez, aşağı iter
    }

    [Fact]
    public void Y4B_45_kimlik_formu_cekmecede_kaydet_kilitliyken_onay_isareti_yok()
    {
        kimlikEsigi = 5000m;   // 10.000 ₺'lik fiş eşiği aşar
        var e = Satis(ArayuzModuServisi.Basit);
        KalemEkle(e);
        e.WaitForAssertion(() => Assert.Single(e.FindAll(".yz-tezgah-kalem")), TimeSpan.FromSeconds(5));
        // YILDIZ-4C tur 3: katman kendiliğinden AÇILMAZ (hakem KRİTİK) — gerçek müşteri "Değiştir"le seçilir
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-musteri-sec-c1")));
        e.Find("#yildiz-musteri-sec-c1").Click();   // YILDIZ-4C: tuval dilinde seçim satırı
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-tezgah-kimlik")), TimeSpan.FromSeconds(5));
        Assert.True(e.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#yildiz-tezgah-fark .yz-tezgah-onay"));
        Assert.Empty(e.FindAll(".kart--masak, #masak-baslik"));   // fişin altına açılmaz
        Assert.Empty(e.FindAll("#cekmece-yildiz-kimlik"));
        e.Find("#yildiz-tezgah-kimlik").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#cekmece-yildiz-kimlik")));
        Assert.NotEmpty(e.Find("#cekmece-yildiz-kimlik-govde").Children);
    }
}
