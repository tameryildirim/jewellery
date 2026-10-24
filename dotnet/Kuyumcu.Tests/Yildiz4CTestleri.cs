using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-4C — Tezgâh etkileşim hâlleri TUVAL DİLİNDE · Müşteriler kalanları ·
// Bugün'ün iki tuval satırı (dünkü gün sonu · 30 günü aşan emanet).
//
// Görsel eşitliğin ASIL kanıtı gerçek başsız Chrome'daki tuval-kiyas.mjs ölçümüdür
// (ham/yildiz-4c/hal/*-tablo.md — tuvalde çizilmemiş hâl tuvalin EN YAKIN kalıbıyla).
// Bu sınıf o ölçümün dayandığı SÖZLEŞMEYİ kilitler:
//   • saf: dün kapanmamış kasa kuralı, emanetin elde durduğu ilk gün, eşik (> 30 gün),
//     Türkçe iyelik eki, müşteri katmanı süzgeci;
//   • bUnit: eski kalem tablosu / eski karma formu / eski fiş üst özeti Yıldız'da ÇİZİLMEZ
//     (hakem 4B tur 2 KRİTİK); kalem/indirim/karma/müşteri hâlleri sayfanın MEVCUT
//     kuralıyla çalışır (gövde platin'deki özgün yolla BİREBİR); Bugün satırları canlı veriden,
//     veri/yetki yoksa satır yok, hata "yok"a çevrilmez;
//   • CSS: tuval kalıplarının değerleri (.k · .ekle · küçük .b2) ve kapsam.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class Yildiz4CTestleri : BunitContext
{
    private string sema = "yildiz";
    private string? satisHatasi;
    private string? satisKod;   // 400 gövdesindeki "kod" (ör. PESIN_ART_ARDA_SORU)
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    // Panel (Bugün) sahte yanıtları — testler değiştirir.
    private string kasaDefteri = """{"kasalar":[]}""";
    private HttpStatusCode kasaDefteriKod = HttpStatusCode.OK;
    private string cariListe = "[]";
    private HttpStatusCode cariKod = HttpStatusCode.OK;
    private readonly Dictionary<string, string> emanetler = [];
    private bool sistemCariHatasi;   // tur 3: Peşin Müşteri kartı okunamazsa

    private sealed class SahteApi(Yildiz4CTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var yalin = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            HttpResponseMessage Yanit(HttpStatusCode kod, string icerik) => new(kod)
            {
                Content = new StringContent(icerik, System.Text.Encoding.UTF8, "application/json"),
            };
            if (t.satisHatasi is { } sh && yol.Contains("/islemler/satis") && istek.Method == HttpMethod.Post)
                return Yanit(HttpStatusCode.BadRequest, JsonSerializer.Serialize(new { message = sh, kod = t.satisKod }));
            if (t.sistemCariHatasi && yol.Contains("/cari/sistem-carisi")) return Yanit(HttpStatusCode.InternalServerError, """{"message":"okunamadi"}""");
            if (yalin.EndsWith("/raporlar/kasa-defteri")) return Yanit(t.kasaDefteriKod, t.kasaDefteri);
            if (yalin.Contains("/emanet/"))
            {
                var id = yalin[(yalin.LastIndexOf('/') + 1)..];
                return Yanit(HttpStatusCode.OK, t.emanetler.TryGetValue(id, out var em) ? em : """{"cariId":"x","mevcutHasGramBakiye":0,"hareketler":[]}""");
            }
            if (yol.Contains("/cari?aktif=true&limit=200"))   // Panel.Yildiz (Islemler "/cari?limit=200&aktif=true" çağırır — ayrı)
                return Yanit(t.cariKod, t.cariListe);
            var y = yol switch
            {
                var v when v.Contains("/gorunum") => $$"""{"sema":"{{t.sema}}","yaziOlcegi":"18","temaModu":"karanlik"}""",
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Gram altın","ayarId":"K24","ayar":"24","hasGrami":0.995,"iscilikTl":0,"satisFiyati":10000,"birim":"ADET","milyem":995,"gramAgirlik":1}]""",
                var v when v.Contains("/kasalar/nakit-ayrimi") => "[]",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/yetkiler/benim") => """{"moduller":["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","kur","emanet"]}""",
                var v when v.Contains("/cari/sistem-carisi") => """{"id":"p1","ad":"Peşin Müşteri"}""",
                var v when v.Contains("/cari") => """[{"id":"p1","ad":"Peşin Müşteri","sistemCarisi":true},{"id":"c1","ad":"Selin Ergüven"},{"id":"c2","ad":"Tuna Akarsu","tlBakiye":22174.73}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var v when v.Contains("/pos/cihazlar") => "[]",
                var v when v.Contains("/pozisyon") => """{"anaMaden":"ALTIN","altin":{"maden":"ALTIN","vitrinGram":157.26,"hurdaGram":0,"cariAlacakGram":0,"cariBorcGram":0,"netGram":157.26,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},"digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,"hesaplanmaAni":"2026-10-08T09:00:00Z","donemdenBagimsiz":true}""",
                var v when v.Contains("/islemler/satis") && istek.Method == HttpMethod.Post => """{"id":"h1","hareketId":"h1","fisNo":"F-0001","fisId":"f1"}""",
                _ => "{}",
            };
            return Yanit(HttpStatusCode.OK, y);
        }
    }

    public Yildiz4CTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<HavaServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
    }

    private void Oturum(string mod, string rol = "SAHIP") =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Hakan", "Tunalı", "h@ornek.com", rol, "s1", mod, mod));

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Satis(string mod, string s = "yildiz")
    {
        sema = s;
        Oturum(mod);
        Services.GetRequiredService<GorunumServisi>().YukleAsync().GetAwaiter().GetResult();
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement(s == "yildiz" ? "#yildiz-tezgah" : "#islem-kaydet", TimeSpan.FromSeconds(10));
        return e;
    }

    /// <summary>
    /// Ürün seç + "Ekle" — ortak yol (TemaTemizligiGCYardimcisi.YildizKalemEkle): fiyat dolana dek bekler, eklemenin satır
    /// ya da toplamda görünmesini bekler. Beklemeden art arda ekleme (Y4C_44, 4 kez) önceki "Ekle" işlenirken seçiciyi
    /// açmaya çalışıyordu: tıklama kuyruğa düşüp liste kapalı kalıyordu ("Listedekiler: []", 17.10 dalgalı kırmızı).
    /// </summary>
    private static void KalemEkle(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) => e.YildizKalemEkle();

    /// <summary>POST gövdesi (yolu içeren son POST); istemci üretimi kimlikler çıkarılır.</summary>
    private string Govde(string yolParcasi)
    {
        string? g = null;
        Assert.True(SpinWait.SpinUntil(() =>
        {
            lock (istekler) g = istekler.LastOrDefault(i => i.Metot == "POST" && i.Yol.Contains(yolParcasi)).Govde;
            return g is not null;
        }, TimeSpan.FromSeconds(10)), yolParcasi + " POST'u gelmedi");
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

    /// <summary>tema-yildiz.css'in YILDIZ-4C bölümü (yorumsuz).</summary>
    private static string Blok4C()
    {
        var c = Css();
        var i = c.IndexOf("PAKET YILDIZ-4C — TEZGÂH ETKİLEŞİM", StringComparison.Ordinal);
        Assert.True(i > 0, "tema-yildiz.css'te YILDIZ-4C bölümü yok");
        var son = c.IndexOf("PAKET YILDIZ-", i + 20, StringComparison.Ordinal);   // sonraki Yıldız paketinin bölümü (ör. YILDIZ-AMBLEM) dahil değil
        var bas = c.LastIndexOf("/*", i, StringComparison.Ordinal);
        var blok = son < 0 ? c[bas..] : c[bas..c.LastIndexOf("/*", son, StringComparison.Ordinal)];
        return Regex.Replace(blok, @"/\*.*?\*/", "", RegexOptions.Singleline);
    }

    private static string Kural(string blok, string secici)
    {
        var i = blok.IndexOf(secici + " {", StringComparison.Ordinal);
        Assert.True(i >= 0, "kural yok: " + secici);
        var a = blok.IndexOf('{', i);
        return blok[(a + 1)..blok.IndexOf('}', a)];
    }

    private static KasaDefteriYaniti Defter(params (string Kasa, string Tarih, int Hareket, string Durum)[] gunler) =>
        new(null, null, null, 3, false, gunler.Length,
            [.. gunler.GroupBy(g => g.Kasa).Select(g => new KasaDefteriSatiri(g.Key, g.Key, "TRY", 0m, 0m, 0m,
                [.. g.Select(x => new KasaGunu(x.Tarih, 0m, 0m, 0m, x.Hareket, 0m, x.Durum, null, null, null))]))]);

    // ── SAF ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Y4C_01_dun_kapanmayan_kasa_yalniz_hareketli_ve_kapali_olmayan()
    {
        var dun = new DateOnly(2026, 10, 8);
        var y = Defter(("Ana Kasa", "2026-10-08", 3, "ACIK"), ("Yan Kasa", "2026-10-08", 2, "KAPALI"),
                       ("Boş Kasa", "2026-10-08", 0, "ACIK"), ("Açılan Kasa", "2026-10-08", 1, "YENIDEN_ACILDI"),
                       ("Eski Gün", "2026-10-07", 5, "ACIK"));
        Assert.Equal(["Ana Kasa", "Açılan Kasa"], YildizBugunBicim.DunKapanmayanKasalar(y, dun));
        Assert.Empty(YildizBugunBicim.DunKapanmayanKasalar(null, dun));                // yanıt yoksa uydurma yok
        Assert.Empty(YildizBugunBicim.DunKapanmayanKasalar(Defter(("Ana Kasa", "2026-10-08", 4, "KAPALI")), dun));
    }

    [Fact]
    public void Y4C_02_emanet_baslangici_bakiye_sifirdan_artiya_gectigi_gun_sinirda_null()
    {
        var a = new DateTime(2026, 9, 1, 7, 0, 0, DateTimeKind.Utc);
        var b = new DateTime(2026, 9, 8, 7, 0, 0, DateTimeKind.Utc);
        var c = new DateTime(2026, 9, 20, 7, 0, 0, DateTimeKind.Utc);
        // giriş → tam çıkış → yeniden giriş: sayaç ikinci girişten başlar
        var h = new[] { (a, 5m, (string?)"ALTIN"), (b, -5m, (string?)"ALTIN"), (c, 9.16m, (string?)null) };
        Assert.Equal(new DateOnly(2026, 9, 20), YildizBugunBicim.EmanetBaslangici(h, false));
        // sunucu listesi sınıra değdiyse yaş bilinmez → null (uydurulmaz)
        Assert.Null(YildizBugunBicim.EmanetBaslangici(h, true));
        // bakiye sıfır → null; gümüş emaneti altın yaşına karışmaz
        Assert.Null(YildizBugunBicim.EmanetBaslangici([(a, 5m, "ALTIN"), (b, -5m, "ALTIN")], false));
        Assert.Null(YildizBugunBicim.EmanetBaslangici([(a, 100m, "GUMUS")], false));
        // kısmi çıkış başlangıcı değiştirmez
        Assert.Equal(new DateOnly(2026, 9, 1), YildizBugunBicim.EmanetBaslangici([(a, 5m, "ALTIN"), (b, -2m, "ALTIN")], false));
    }

    [Fact]
    public void Y4C_03_emanet_esigi_otuz_gunu_ASAN_satir_verir()
    {
        var bugun = new DateOnly(2026, 10, 9);
        Assert.Equal(30, YildizBugunBicim.EmanetEsikGun);
        Assert.Equal(31, YildizBugunBicim.EmanetGunu(new DateOnly(2026, 9, 8), bugun));
        Assert.True(YildizBugunBicim.EmanetSatiriCikar(31));
        Assert.False(YildizBugunBicim.EmanetSatiriCikar(30));    // tuval "30 günü aştı" = > 30
        Assert.False(YildizBugunBicim.EmanetSatiriCikar(null));  // yaş bilinmiyorsa satır yok
        Assert.Equal("30 günü aştı", YildizBugunSozlugu.EmanetAsti(YildizBugunBicim.EmanetEsikGun, "tr"));
    }

    [Fact]
    public void Y4C_04_turkce_iyelik_eki_tuval_yazimi()
    {
        Assert.Equal("Derya Yıldızlı'nın", YildizBugunBicim.Iyelik("Derya Yıldızlı"));
        Assert.Equal("Kemal Ardıç'ın", YildizBugunBicim.Iyelik("Kemal Ardıç"));
        Assert.Equal("Selin Ergüven'in", YildizBugunBicim.Iyelik("Selin Ergüven"));
        Assert.Equal("Tuna Akarsu'nun", YildizBugunBicim.Iyelik("Tuna Akarsu"));
        Assert.Equal("Ömür Gök'ün", YildizBugunBicim.Iyelik("Ömür Gök"));
        Assert.Equal("Nermin Çalışkan'ın", YildizBugunBicim.Iyelik("Nermin Çalışkan"));
        Assert.Null(YildizBugunBicim.Iyelik("XYZ"));          // ünlü yok → ek uydurulmaz
        Assert.Null(YildizBugunBicim.Iyelik("Ayşe 2"));        // harfle bitmiyor
        Assert.Equal("Derya Yıldızlı'nın emaneti", YildizBugunSozlugu.Emaneti("Derya Yıldızlı", YildizBugunBicim.Iyelik("Derya Yıldızlı"), "tr"));
        Assert.Equal("XYZ · emanet", YildizBugunSozlugu.Emaneti("XYZ", null, "tr"));
    }

    [Fact]
    public void Y4C_05_musteri_katmani_suzgeci_turkce_ve_secili_ilk()
    {
        var l = new List<YildizMusteriSecenegi> { new("p1", "Peşin Müşteri", null), new("c1", "Selin Ergüven", "borcu yok"), new("c2", "İlkay Işık", null) };
        Assert.Equal(["c1"], YildizTezgahBicim.MusteriSuz(l, "SELİN", null).Select(x => x.Id));
        Assert.Equal(["c2"], YildizTezgahBicim.MusteriSuz(l, "ilkay", null).Select(x => x.Id));   // tr-TR: i ↔ İ
        Assert.Equal(["c2", "p1", "c1"], YildizTezgahBicim.MusteriSuz(l, "", "c2").Select(x => x.Id));
        Assert.Empty(YildizTezgahBicim.MusteriSuz(l, "zzz", "c2"));
    }

    // ── bUnit: bileşenler ────────────────────────────────────────────────────

    private (YildizTezgahBaglami B, List<string> Cagri) Baglam(bool karma = false, bool pro = false, bool indirim = false)
    {
        var cagri = new List<string>();
        EventCallback C(string ad) => EventCallback.Factory.Create(this, () => cagri.Add(ad));
        return (new YildizTezgahBaglami
        {
            TurAdi = "Satış", MusteriAdi = "Selin Ergüven", MusteriDurumu = "borcu yok",
            Kalemler = [new(0, "24", "Gram altın", "2 adet · 1,00 g · 1,9900 g has", "13.844,00 ₺"),
                        new(1, "22", "Çeyrek altın", "1 adet · 1,75 g · 1,6030 g has", "11.180,00 ₺")],
            KalemDuzenleri = [new(0, "u1", "2", "6.922,00", "0", false, "ADET"), new(1, "u2", "1", "11.180,00", "0", false, "ADET")],
            ToplamAyrinti = "3,75 g · 3,5930 g has", ToplamMetni = "25.024,00 ₺",
            Odemeler = [new(0, "Nakit", "Ana Kasa", "25.024,00 ₺")],
            Karma = karma, FarkMetni = "0 ₺", FarkTamam = true, Pro = pro,
            KaydetEtiketi = "Kaydet ve yazdır", FisDolu = true, KalemVar = true, ParaUstuKullanilir = true, ParaUstuNakit = 25024m,
            IndirimGorunur = indirim, IndirimAlani = b => b.AddMarkupContent(0, "<div id=\"sahte-indirim\"></div>"),
            Kaydet = C("kaydet"), Vazgec = C("vazgec"), BaskaOdeme = C("baska"), TekOdeme = C("tek"), BacakEkle = C("bacak"),
            Indirim = C("indirim"), AcikHesap = C("acik"), MusteriDegistir = C("musteri"), TurDegistir = C("tur"),
            KalemYaz = EventCallback.Factory.Create<YildizKalemGirdi>(this, g => cagri.Add($"yaz:{g.Sira}:{g.Alan}:{g.Metin}")),
            KalemHediye = EventCallback.Factory.Create<YildizKalemHediye>(this, h => cagri.Add($"hediye:{h.Sira}:{h.Deger}")),
            KalemCikar = EventCallback.Factory.Create<int>(this, i => cagri.Add($"cikar:{i}")),
            MusteriSecenekleri = [new("p1", "Peşin Müşteri", null), new("c1", "Selin Ergüven", "borcu yok"), new("c2", "Tuna Akarsu", "borcu 22.174,73 ₺")],
            MusteriSeciliId = "c1",
            MusteriSec = EventCallback.Factory.Create<string>(this, id => cagri.Add("sec:" + id)),
        }, cagri);
    }

    [Fact]
    public void Y4C_10_kalem_satirina_dokununca_tuval_k_satirlari_eski_tablo_yok()
    {
        var (b, cagri) = Baglam(pro: true);
        var e = Render<YildizTezgah>(p => p.Add(x => x.B, b));
        Assert.Empty(e.FindAll(".yz-tezgah-kalem-duzen"));
        e.Find("#yildiz-kalem-1").Click();
        Assert.Equal("true", e.Find("#yildiz-kalem-1").GetAttribute("aria-expanded"));
        Assert.Single(e.FindAll("#yildiz-kalem-duzen-1"));
        Assert.Empty(e.FindAll("#yildiz-kalem-duzen-0"));
        Assert.Empty(e.FindAll("table, #islem-kalem-tablo, .izgara"));
        Assert.Equal(4, e.FindAll("#yildiz-kalem-duzen-1 .yz-k").Count);   // adet · birim fiyat · satır indirimi · hediye
        e.Find("#yildiz-kalem-1-adet").Change("3");
        e.Find("#yildiz-kalem-1-fiyat").Change("12.000,00");
        e.Find("#yildiz-kalem-1-iskonto").Change("5");
        e.Find("#yildiz-kalem-1-hediye").Change(true);
        e.Find("#yildiz-kalem-cikar-1").Click();
        Assert.Equal(["yaz:1:adet:3", "yaz:1:fiyat:12.000,00", "yaz:1:iskonto:5", "hediye:1:True", "cikar:1"], cagri);
        Assert.Empty(e.FindAll(".yz-tezgah-kalem-duzen"));   // çıkarınca düzen kapanır
        // BASİT: özgün fişte olduğu gibi yalnız "Çıkar" (adet/fiyat düzeltmesi PRO alanı)
        var (bb, _) = Baglam(pro: false);
        var e2 = Render<YildizTezgah>(p => p.Add(x => x.B, bb));
        e2.Find("#yildiz-kalem-0").Click();
        Assert.Empty(e2.FindAll("#yildiz-kalem-duzen-0 input"));
        Assert.Single(e2.FindAll("#yildiz-kalem-cikar-0"));
        Assert.Single(e2.FindAll("#yildiz-kalem-tamam-0"));
    }

    [Fact]
    public void Y4C_11_indirim_yalniz_indirim_alanini_acar_kalem_listesi_tek()
    {
        var (b, cagri) = Baglam(indirim: true);
        var e = Render<YildizTezgah>(p => p.Add(x => x.B, b));
        Assert.Single(e.FindAll("#yildiz-tezgah-indirim-alan #sahte-indirim"));
        Assert.Equal("true", e.Find("#yildiz-tezgah-indirim").GetAttribute("aria-expanded"));
        Assert.Equal(2, e.FindAll(".yz-tezgah-kalem").Count);
        e.Find("#yildiz-tezgah-indirim").Click();
        Assert.Equal(["indirim"], cagri);
        var (kapali, _) = Baglam(indirim: false);
        var e2 = Render<YildizTezgah>(p => p.Add(x => x.B, kapali));
        Assert.Empty(e2.FindAll("#yildiz-tezgah-indirim-alan"));
    }

    [Fact]
    public void Y4C_12_karmada_ozet_satirlari_yok_baska_odeme_yeni_satir_tavanda_kilit_sebebi()
    {
        var (b, cagri) = Baglam(karma: true);
        var e = Render<YildizTezgah>(p => p.Add(x => x.B, b));
        Assert.Empty(e.FindAll("#yildiz-tezgah-odemeler, .yz-tezgah-odeme"));
        Assert.Single(e.FindAll("#yildiz-tezgah-odeme-duzen.yz-tezgah-karma"));
        e.Find("#yildiz-tezgah-baska-odeme").Click();
        Assert.Equal(["bacak"], cagri);
        // tuval sırası: + Başka ödeme · + Para üstü · + İndirim (· − Tek ödeme)
        Assert.Equal(["yildiz-tezgah-baska-odeme", "yildiz-tezgah-para-ustu", "yildiz-tezgah-indirim", "yildiz-tezgah-tek-odeme"],
            e.FindAll("#yildiz-tezgah-ekler .yz-b3").Select(x => x.Id));
        var tavan = new YildizTezgahBaglami { Karma = true, KalemVar = true, BacakTavan = true, BacakEkle = b.BacakEkle };
        var e2 = Render<YildizTezgah>(p => p.Add(x => x.B, tavan));
        Assert.True(e2.Find("#yildiz-tezgah-baska-odeme").HasAttribute("disabled"));
        Assert.Equal("En çok 5 ödeme satırı.", e2.Find("#yildiz-tezgah-baska-odeme").GetAttribute("title"));
    }

    /// <summary>
    /// YILDIZ-4C-2 tur 2 (tasarım hakemi KRİTİK, basari.mjs BASİT 1440 karma): karma satış kaydedilince fiş tek ödemeye
    /// döner (Karma true → false); "+ Başka ödeme"nin açtığı düzen paneli (eski "Ödeme şekli / Kasa") açık KALMAZ.
    /// Karma olmadan kullanıcının kendi açtığı panel ise yeniden çizimde açık kalır (düzeltme aşırı geniş değil).
    /// </summary>
    [Fact]
    public void Y4C2_08_karma_kaydi_sonrasi_odeme_duzen_paneli_kapanir()
    {
        var (b, cagri) = Baglam();
        var e = Render<YildizTezgah>(p => p.Add(x => x.B, b));
        Assert.Empty(e.FindAll("#yildiz-tezgah-odeme-duzen"));
        e.Find("#yildiz-tezgah-baska-odeme").Click();                 // "+ Başka ödeme" → panel + karmaya geçiş
        Assert.Equal(["baska"], cagri);
        var (karma, _) = Baglam(karma: true);
        e.Render(p => p.Add(x => x.B, karma));
        Assert.Single(e.FindAll("#yildiz-tezgah-odeme-duzen.yz-tezgah-karma"));
        var (kayitSonrasi, _) = Baglam(karma: false);                  // Kaydet başarılı → sayfa fişi sıfırladı
        e.Render(p => p.Add(x => x.B, kayitSonrasi));
        Assert.Empty(e.FindAll("#yildiz-tezgah-odeme-duzen"));
        Assert.Empty(e.FindAll("#islem-aldim-odeme, #yildiz-tezgah-odeme-tamam"));
        Assert.Single(e.FindAll("#yildiz-tezgah-odemeler .yz-tezgah-odeme"));   // tuvalin tek "Nakit · Ana Kasa" satırı

        // Karma olmadan kullanıcı satıra dokunup paneli açtıysa yeniden çizim paneli kapatmaz.
        var (tek, _) = Baglam();
        var e2 = Render<YildizTezgah>(p => p.Add(x => x.B, tek));
        e2.Find("#yildiz-odeme-0").Click();
        Assert.Single(e2.FindAll("#yildiz-tezgah-odeme-duzen"));
        var (tek2, _) = Baglam();
        e2.Render(p => p.Add(x => x.B, tek2));
        Assert.Single(e2.FindAll("#yildiz-tezgah-odeme-duzen"));
    }

    [Fact]
    public void Y4C_13_musteri_katmani_seni_bekleyen_kalibi_secim_ve_notlar()
    {
        var (b, cagri) = Baglam();
        var e = Render<YildizMusteriSecici>(p => p.Add(x => x.B, b));
        Assert.Equal("Müşteri ara", e.Find("#yildiz-musteri-ara").GetAttribute("placeholder"));
        var satirlar = e.FindAll("#yildiz-musteri-liste > .yz-sat");
        Assert.Equal(3, satirlar.Count);
        Assert.Equal("yildiz-musteri-sat-c1", satirlar[0].Id);                  // seçili ilk sırada
        Assert.Equal("seçili", e.Find("#yildiz-musteri-sat-c1 .yz-tezgah-musteri-durum").TextContent);
        Assert.Empty(e.FindAll("#yildiz-musteri-sec-c1"));
        Assert.Equal("Selin Ergüven · borcu yok", e.Find("#yildiz-musteri-sat-c1 .yz-tezgah-musteri-ad").TextContent);
        e.Find("#yildiz-musteri-sec-c2").Click();
        Assert.Equal(["sec:c2"], cagri);
        e.Find("#yildiz-musteri-ara").Input("zzz");
        Assert.Equal("Aramaya uyan müşteri yok.", e.Find("#yildiz-musteri-yok").TextContent);
        Assert.Empty(e.FindAll("#islem-cari, .fis-ust-ozet, #islem-ust-satir, .secici-tetik"));   // eski seçici / üst özet YOK
        // UX-S29: 8'den fazla müşteri → sınır satırı
        var cok = new YildizTezgahBaglami { MusteriSecenekleri = [.. Enumerable.Range(0, 11).Select(i => new YildizMusteriSecenegi($"m{i}", $"Müşteri {i}", null))] };
        var e2 = Render<YildizMusteriSecici>(p => p.Add(x => x.B, cok));
        Assert.Equal(YildizMusteriSecici.EnCok, e2.FindAll("#yildiz-musteri-liste > .yz-sat").Count);
        Assert.Equal("3 müşteri daha — listeyi daraltmak için ad yazın.", e2.Find("#yildiz-musteri-sinir").TextContent);
        // Peşin/kimlik kapısının notları (id'ler özgünle AYNI) + PRO ek alanları .ekle ardında
        var kapi = new YildizTezgahBaglami
        {
            PesinHata = "Bu tutarda Peşin Müşteri olmaz.", PesinKimlikGerekli = true,
            GercekMusteriSec = EventCallback.Factory.Create(this, () => cagri.Add("gercek")),
            ProUstAlanlar = x => x.AddMarkupContent(0, "<div id=\"sahte-pro\"></div>"),
        };
        var e3 = Render<YildizMusteriSecici>(p => p.Add(x => x.B, kapi));
        Assert.Contains("yz-kotu", e3.Find("#islem-pesin-hata").ClassList);
        e3.Find("#islem-pesin-esik-sec").Click();
        Assert.Equal("gercek", cagri[^1]);
        Assert.Equal("/cariler", e3.Find("#islem-pesin-esik-yeni").GetAttribute("href"));
        Assert.Empty(e3.FindAll("#sahte-pro"));
        e3.Find("#yildiz-musteri-pro-ac").Click();
        Assert.Single(e3.FindAll("#yildiz-musteri-pro #sahte-pro"));
    }

    [Fact]
    public void Y4C_14_musteriler_arama_bos_yanlis_bos_durum_demez_suzgec_ekle_kalibinda()
    {
        var cagri = new List<string>();
        var b = new YildizMusteriBaglami
        {
            Ust = "7 müşteri · 2 alacak · 1 borç", AramaBos = true,
            Sekmeler = [new("hepsi", "HEPSİ", true), new("borclu", "BORÇLU", false), new("toptanci", "TOPTANCI", false)],
        };
        var e = Render<YildizMusteriler>(p => p.Add(x => x.B, b));
        Assert.Equal("Aramaya uyan müşteri yok.", e.Find("#yildiz-musteri-arama-bos").TextContent);
        Assert.Empty(e.FindAll("#yildiz-musteri-bos"));
        Assert.DoesNotContain("Henüz müşteri yok", e.Markup);
        // süzgeç sekme satırında DEĞİL; YILDIZ-4D: "+ Süzgeç" tuvalde yok → hiç çizilmez (Yildiz4DTestleri)
        Assert.Equal(3, e.FindAll("#cari-segment-serit > *").Count);
        Assert.Empty(e.FindAll("#cari-suzgec-daha"));
        Assert.Empty(cagri);
        var hic = new YildizMusteriBaglami { HicYok = true };
        Assert.Equal("Henüz müşteri yok.", Render<YildizMusteriler>(p => p.Add(x => x.B, hic)).Find("#yildiz-musteri-bos").TextContent);
    }

    // ── bUnit: sayfa (Islemler) — eski öğe görünmez, davranış özgünle BİREBİR ──

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Y4C_20_yildizda_eski_kalem_tablosu_karma_formu_ust_ozet_cizilmez(string mod)
    {
        var e = Satis(mod);
        KalemEkle(e);
        e.WaitForAssertion(() => Assert.Single(e.FindAll(".yz-tezgah-kalem")), TimeSpan.FromSeconds(5));
        e.Find("#yildiz-kalem-0").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-kalem-duzen-0")));
        if (mod == ArayuzModuServisi.Profesyonel) Assert.Single(e.FindAll("#yildiz-kalem-0-adet"));
        e.Find("#yildiz-tezgah-indirim").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-tezgah-indirim-alan #islem-fis-iskonto")));
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-musteri-secici")));
        e.Find("#yildiz-tezgah-baska-odeme").Click();
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll(".bacak-satiri").Count));
        // hakem 4B tur 2 KRİTİK: kalem listesi ikinci kez (eski tablo), ödemeler ikinci kez, eski üst özet — hiçbiri yok
        Assert.Empty(e.FindAll("#islem-kalem-tablo, #islem-kalem-pro, .fis-tuval-tablo, #kalem-tuval-sutunlar-ac, [id^='islem-kalem-sil-']"));
        Assert.Empty(e.FindAll("#yildiz-tezgah-odemeler, .yz-tezgah-odeme"));
        Assert.Empty(e.FindAll("#islem-ust-satir, #islem-ust-ozet, #islem-cari"));
        Assert.Single(e.FindAll(".yz-tezgah-kalem"));
    }

    [Fact]
    public void Y4C_21_yildiz_pro_barkod_yer_tutucusu_tuval_metni_F2_yok()
    {
        var e = Satis(ArayuzModuServisi.Profesyonel);
        var yt = e.Find("#islem-barkod").GetAttribute("placeholder");
        Assert.Equal("Barkodu okut ya da ürün adı yaz", yt);
        Assert.DoesNotContain("F2", yt);
    }

    private const string Y4C22Beklenen = "Nakit=4.000|Kart=6.000|Havale=0";

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Y4C_22_karma_baska_odeme_ozgun_bacak_ekle_ile_ayni_satiri_uretir(string mod)
    {
        // Yıldız: "+ Başka ödeme" (OdemeyiBol) → 2 satır; ikinci kez → 3. satır (YildizBacakEkle)
        var y = Satis(mod);
        KalemEkle(y);
        y.WaitForAssertion(() => Assert.Single(y.FindAll(".yz-tezgah-kalem")), TimeSpan.FromSeconds(5));
        y.Find("#yildiz-tezgah-baska-odeme").Click();
        y.WaitForAssertion(() => Assert.Equal(2, y.FindAll(".bacak-satiri").Count));
        y.Find("#islem-bacak-0-tutar").Input("4000");
        y.Find("#islem-bacak-0-tutar").Blur();
        y.Find("#yildiz-tezgah-baska-odeme").Click();
        y.WaitForAssertion(() => Assert.Equal(3, y.FindAll(".bacak-satiri").Count));
        var yildiz = string.Join("|", Enumerable.Range(0, 3).Select(i =>
            y.Find($"#islem-bacak-{i}-arac").TextContent.Trim() + "=" + y.Find($"#islem-bacak-{i}-tutar").GetAttribute("value")));

        // TEMA TEMİZLİĞİ 17.10: özgün (Platin BASİT) yüz kalktı; onun "+ Başka ödeme ekle" → "bacak ekle" yolunun
        // ürettiği satırlar (Yıldız'la birebir olduğu bu testle kanıtlanmıştı) beklenen değer olarak SABİTLENDİ.
        Assert.Equal(Y4C22Beklenen, yildiz);
    }

    private static readonly Dictionary<string, string> Y4C23Beklenen = new()
    {
        [ArayuzModuServisi.Basit] = "{\"cariId\":\"c1\",\"kalemler\":[{\"urunId\":\"u1\",\"adet\":1,\"birimFiyatTl\":10000,\"hediye\":false,\"satirIskontoYuzde\":0}],\"odemeler\":[{\"arac\":\"NAKIT\",\"tutarTl\":9500,\"kasaId\":\"ks1\"}],\"kurSnapshotId\":\"kur-1\",\"aciklama\":null,\"masak\":null,\"personelId\":null,\"fisIskontoYuzde\":5,\"reyonId\":null,\"belgeTuru\":null}",
        [ArayuzModuServisi.Profesyonel] = "{\"cariId\":\"c1\",\"kalemler\":[{\"urunId\":\"u1\",\"adet\":1,\"birimFiyatTl\":10000,\"hediye\":false,\"satirIskontoYuzde\":0}],\"odemeler\":[{\"arac\":\"NAKIT\",\"tutarTl\":9500,\"kasaId\":\"ks1\"}],\"vadeGun\":null,\"kurSnapshotId\":\"kur-1\",\"aciklama\":null,\"masak\":null,\"personelId\":null,\"fisIskontoYuzde\":5,\"reyonId\":null,\"belgeTuru\":null}",
    };

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Y4C_23_indirimli_ve_musteri_secilmis_satis_govdesi_ozgun_govdeyle_birebir(string mod)
    {
        // Yıldız: "+ İndirim" %5 · "Değiştir" → Selin · Kaydet
        var y = Satis(mod);
        KalemEkle(y);
        y.WaitForAssertion(() => Assert.Single(y.FindAll(".yz-tezgah-kalem")), TimeSpan.FromSeconds(5));
        y.Find("#yildiz-tezgah-indirim").Click();
        y.WaitForAssertion(() => Assert.Single(y.FindAll("#islem-fis-iskonto")));
        y.Find("#islem-fis-iskonto").Input("5");
        y.Find("#islem-fis-iskonto").Blur();
        y.Find("#yildiz-tezgah-degistir").Click();
        y.WaitForAssertion(() => Assert.Single(y.FindAll("#yildiz-musteri-sec-c1")));
        y.Find("#yildiz-musteri-sec-c1").Click();
        y.WaitForAssertion(() => Assert.Contains("Selin Ergüven", y.Find("#yildiz-tezgah-ust").TextContent));
        y.WaitForAssertion(() => Assert.False(y.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled")), TimeSpan.FromSeconds(5));
        y.Find("#yildiz-tezgah-kaydet").Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(y);
        var yildiz = Govde("/islemler/satis");
        Assert.Contains("\"cariId\":\"c1\"", yildiz);
        Assert.Contains("5", yildiz);
        // TEMA TEMİZLİĞİ 17.10: özgün (Platin, aynı mod) yüz kalktı; onun ürettiği gövde (Yıldız'la birebir olduğu bu
        // testle kanıtlanmıştı) beklenen değer olarak SABİTLENDİ.
        Assert.Equal(Y4C23Beklenen[mod], yildiz);
    }

    [Fact]
    public void Y4C_24_musteri_degisince_onceki_kayit_hatasi_silinir_MusteriDegisti_cagrilir()
    {
        satisHatasi = "Satış: yetersiz satılabilir stok.";
        var e = Satis(ArayuzModuServisi.Basit);
        KalemEkle(e);
        e.WaitForAssertion(() => Assert.Single(e.FindAll(".yz-tezgah-kalem")), TimeSpan.FromSeconds(5));
        e.Find("#yildiz-tezgah-kaydet").Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-tezgah-hata")), TimeSpan.FromSeconds(5));
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-musteri-sec-c2")));
        e.Find("#yildiz-musteri-sec-c2").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#yildiz-tezgah-hata")), TimeSpan.FromSeconds(5));   // özgün MusteriDegisti: hata = null
        Assert.Contains("Tuna Akarsu", e.Find("#yildiz-tezgah-ust").TextContent);
    }

    [Fact]
    public void Y4C_26_pesin_art_arda_sorusu_fark_blogunda_tek_kez_ve_cevap_ozgun_yolla()
    {
        satisHatasi = "Az önce bu kasada Peşin Müşteri ile işlem yapıldı.";
        satisKod = "PESIN_ART_ARDA_SORU";
        var e = Satis(ArayuzModuServisi.Basit);
        KalemEkle(e);
        e.WaitForAssertion(() => Assert.Single(e.FindAll(".yz-tezgah-kalem")), TimeSpan.FromSeconds(5));
        e.Find("#yildiz-tezgah-kaydet").Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        // özgün yerinde (fişin altında, Yıldız'da ekran dışı) DEĞİL — Fark bloğunda, tek kez
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-tezgah-fark #yildiz-tezgah-pesin-soru #pesin-soru")), TimeSpan.FromSeconds(5));
        Assert.Single(e.FindAll("#pesin-soru"));
        // 4C-2 (gerçek Chrome ESKI sayacı): Yıldız tezgâhında soru eylemleri eski .btn değil, tuvalin küçük .b2'si
        Assert.Empty(e.FindAll("#yildiz-tezgah-pesin-soru .btn"));
        Assert.Equal(2, e.FindAll("#yildiz-tezgah-pesin-soru button.yz-b2").Count);
        satisHatasi = null; satisKod = null;
        lock (istekler) istekler.Clear();
        e.Find("#pesin-soru-hayir").Click();
        var g = Govde("/islemler/satis");
        Assert.Contains("\"ayniKisiBeyani\":false", g);
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#pesin-soru")), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Y4C_25_pro_kalem_duzeltmesi_ozgun_kurallarla_ileti_ve_toplam()
    {
        var e = Satis(ArayuzModuServisi.Profesyonel);
        KalemEkle(e);
        e.WaitForAssertion(() => Assert.Single(e.FindAll(".yz-tezgah-kalem")), TimeSpan.FromSeconds(5));
        var once = e.Find("#yildiz-tezgah-toplam-rakam").TextContent;
        e.Find("#yildiz-kalem-0").Click();
        e.Find("#yildiz-kalem-0-adet").Change("0");
        e.WaitForAssertion(() => Assert.Equal(Services.GetRequiredService<DilServisi>().T.Kabuk.SifirdanBuyuk, e.Find("#alan-hata-si-kalem-adet-u1").TextContent));
        e.Find("#yildiz-kalem-0-fiyat").Change("-5");
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#alan-hata-si-kalem-fiyat-u1")));
        e.Find("#yildiz-kalem-0-iskonto").Change("150");
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#alan-hata-si-kalem-iskonto-u1")));
        Assert.Equal(once, e.Find("#yildiz-tezgah-toplam-rakam").TextContent);   // geçersiz giriş değer değiştirmez
        e.Find("#yildiz-kalem-0-adet").Change("2");
        e.WaitForAssertion(() => Assert.NotEqual(once, e.Find("#yildiz-tezgah-toplam-rakam").TextContent));
        Assert.Empty(e.FindAll("#alan-hata-si-kalem-adet-u1"));
        Assert.Equal("2", e.Find("#yildiz-kalem-0-adet").GetAttribute("value"));
        e.Find("#yildiz-kalem-cikar-0").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll(".yz-tezgah-kalem")));
    }

    // ── bUnit: Bugün (Panel) — iki tuval satırı ─────────────────────────────

    private IRenderedComponent<Kuyumcu.Web.Pages.Panel> Bugun(string rol = "SAHIP")
    {
        sema = "yildiz";
        Oturum(ArayuzModuServisi.Basit, rol);
        Services.GetRequiredService<GorunumServisi>().YukleAsync().GetAwaiter().GetResult();
        Services.GetRequiredService<YetkiServisi>().HazirlaAsync().GetAwaiter().GetResult();
        var oturum = Services.GetRequiredService<OturumServisi>();
        KabukServisi.Ortak.GorunumHazirla(Services.GetRequiredService<ApiIstemci>(), oturum).GetAwaiter().GetResult();
        KabukServisi.Ortak.SubeListesiAta(oturum, [new Kuyumcu.Core.Sozlesmeler.SubeOzeti("s1", "Merkez Şube", true)]);
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#yildiz-bugun", TimeSpan.FromSeconds(10));
        return e;
    }

    private static string Dun => DateOnly.FromDateTime(Bicim.IsletmeBugun).AddDays(-1).ToString("yyyy-MM-dd");
    private static string GunOnce(int gun) => DateTime.SpecifyKind(Bicim.IsletmeBugun.AddDays(-gun).AddHours(9), DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ss");

    [Fact]
    public void Y4C_30_bugun_dunku_gun_sonu_satiri_canli_veriden_kapat_kasalar()
    {
        kasaDefteri = $$"""{"kasalar":[{"kasaId":"k1","kasaAd":"Ana Kasa","paraBirimi":"TRY","acilisDeviri":0,"kapanisBakiye":0,"guncelBakiye":0,"gunler":[{"tarih":"{{Dun}}","devir":0,"giris":100,"cikis":0,"hareketSayisi":3,"kapanis":100,"durum":"ACIK"}]}]}""";
        var e = Bugun();
        e.WaitForElement("#yildiz-bekleyen-gun-sonu-dun", TimeSpan.FromSeconds(10));
        Assert.Equal("Dünkü gün sonu kapatılmadı", e.Find("#yildiz-bekleyen-gun-sonu-dun .yildiz-bugun-sat-metin").TextContent);
        Assert.Equal("Kasa", e.Find("#yildiz-bekleyen-gun-sonu-dun-durum").TextContent);
        Assert.Equal("Kapat", e.Find("#yildiz-bekleyen-gun-sonu-dun-eylem").TextContent);
        Assert.Equal("/kasalar", e.Find("#yildiz-bekleyen-gun-sonu-dun-eylem").GetAttribute("href"));
        Assert.Contains("Ana Kasa", e.Find("#yildiz-bekleyen-gun-sonu-dun").GetAttribute("title"));
        lock (istekler) Assert.Contains(istekler, i => i.Yol.Contains("/raporlar/kasa-defteri") && i.Yol.Contains("baslangic=" + Dun) && i.Yol.Contains("bitis=" + Dun));
    }

    [Fact]
    public async Task Y4C_31_bugun_kapali_gun_ya_da_yetkisiz_rol_satir_yok_hata_bant_olur()
    {
        kasaDefteri = $$"""{"kasalar":[{"kasaId":"k1","kasaAd":"Ana Kasa","paraBirimi":"TRY","acilisDeviri":0,"kapanisBakiye":0,"guncelBakiye":0,"gunler":[{"tarih":"{{Dun}}","devir":0,"giris":100,"cikis":0,"hareketSayisi":3,"kapanis":100,"durum":"KAPALI"}]}]}""";
        var e = Bugun();
        e.WaitForAssertion(() => { lock (istekler) Assert.Contains(istekler, i => i.Yol.Contains("/raporlar/kasa-defteri")); }, TimeSpan.FromSeconds(10));
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#yildiz-bekleyen-gun-sonu-dun")));
        e.Dispose();
        kasaDefteriKod = HttpStatusCode.Forbidden;
        kasaDefteri = """{"message":"yetki yok"}""";
        int onceki; lock (istekler) onceki = istekler.Count(i => i.Yol.Contains("/raporlar/kasa-defteri"));
        var e2 = Bugun("TEZGAHTAR");
        e2.WaitForElement("#yildiz-bugun", TimeSpan.FromSeconds(10));
        await Task.Delay(600);
        // Görsel denetim 2 (23.10): rapor rolü olmayan TEZGAHTAR için istek HİÇ atılmaz (403 yolu yerel) — satır da bant da yok.
        lock (istekler) Assert.Equal(onceki, istekler.Count(i => i.Yol.Contains("/raporlar/kasa-defteri")));
        Assert.Empty(e2.FindAll("#yildiz-bekleyen-gun-sonu-dun, #yildiz-bugun-gunsonu-hata"));
        e2.Dispose();
        kasaDefteriKod = HttpStatusCode.InternalServerError;
        kasaDefteri = """{"message":"Rapor alınamadı (sahte)"}""";
        var e3 = Bugun();
        e3.WaitForElement("#yildiz-bugun-gunsonu-hata", TimeSpan.FromSeconds(10));   // UX-S28: hata "kapanmamış gün yok"a çevrilmez
        Assert.Empty(e3.FindAll("#yildiz-bekleyen-gun-sonu-dun"));
    }

    [Fact]
    public void Y4C_32_bugun_otuz_gunu_asan_emanet_satiri_tuval_yazimi()
    {
        cariListe = """[{"id":"c3","ad":"Derya Yıldızlı","tur":"MUSTERI","emanetteHas":9.16},{"id":"c4","ad":"Kemal Ardıç","tur":"MUSTERI","emanetteHas":2},{"id":"c5","ad":"Nermin Çalışkan","tur":"MUSTERI","emanetteHas":0}]""";
        emanetler["c3"] = $$"""{"cariId":"c3","mevcutHasGramBakiye":9.16,"hareketler":[{"id":"h1","tur":"EMANET_GIRIS","tarih":"{{GunOnce(31)}}","hasGrami":9.16,"maden":"ALTIN"}],"sinirDegdi":false}""";
        emanetler["c4"] = $$"""{"cariId":"c4","mevcutHasGramBakiye":2,"hareketler":[{"id":"h2","tur":"EMANET_GIRIS","tarih":"{{GunOnce(30)}}","hasGrami":2,"maden":"ALTIN"}],"sinirDegdi":false}""";
        var e = Bugun();
        e.WaitForElement("#yildiz-bekleyen-emanet-c3", TimeSpan.FromSeconds(10));
        Assert.Equal("Derya Yıldızlı'nın emaneti · 9,16 g has", e.Find("#yildiz-bekleyen-emanet-c3 .yildiz-bugun-sat-metin").TextContent);
        Assert.Equal("30 günü aştı", e.Find("#yildiz-bekleyen-emanet-c3-durum").TextContent);
        Assert.Equal("Aç", e.Find("#yildiz-bekleyen-emanet-c3-eylem").TextContent);
        Assert.Equal("/emanet", e.Find("#yildiz-bekleyen-emanet-c3-eylem").GetAttribute("href"));
        Assert.Empty(e.FindAll("#yildiz-bekleyen-emanet-c4"));   // tam 30 gün → satır yok (eşik AŞILMADI)
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Yol.Contains("/emanet/c5"));   // emaneti olmayan okunmaz
    }

    [Fact]
    public async Task Y4C_33_bugun_emanet_okunamazsa_bant_yetkisizse_sessiz()
    {
        cariKod = HttpStatusCode.InternalServerError;
        cariListe = """{"message":"Cari listesi alınamadı (sahte)"}""";
        var e = Bugun();
        e.WaitForElement("#yildiz-bugun-emanet-hata", TimeSpan.FromSeconds(10));
        Assert.Empty(e.FindAll("[id^='yildiz-bekleyen-emanet-']"));
        e.Dispose();
        cariKod = HttpStatusCode.Forbidden;
        var e2 = Bugun("TEZGAHTAR");
        e2.WaitForAssertion(() => { lock (istekler) Assert.True(istekler.Count(i => i.Yol.Contains("/cari?aktif=true&limit=200")) >= 2); }, TimeSpan.FromSeconds(10));
        await Task.Delay(400);
        Assert.Empty(e2.FindAll("#yildiz-bugun-emanet-hata, [id^='yildiz-bekleyen-emanet-']"));
    }

    // TEMA TEMİZLİĞİ 17.10: Y4C_34_baska_semada_bugun_ek_istek_atilmaz kaldırıldı — "başka şema" yok; Bugün'ün
    // ek istekleri her zaman atılır (Y4C_3x testleri sınar).

    // ── CSS ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Y4C_40_css_tuval_kaliplari_k_ekle_kucuk_b2()
    {
        var b = Blok4C();
        var k = Kural(b, "html:root[data-sema=\"yildiz\"] .yz .yz-k");
        Assert.Contains("padding: 18px 0;", k);
        Assert.Contains("gap: 6px;", k);
        Assert.Contains("border-bottom: 1px solid var(--ayrac);", k);                 // tuval .k çizgisi --h
        var et = Kural(b, "html:root[data-sema=\"yildiz\"] .yz .yz-k-etiket");
        Assert.Contains("font-size: 12px;", et);
        Assert.Contains("letter-spacing: .18em;", et);
        Assert.Contains("text-transform: uppercase;", et);
        var d = Kural(b, "html:root[data-sema=\"yildiz\"] .yz .yz-k-deger");
        Assert.Contains("font-size: 22px;", d);
        Assert.Contains("font-weight: 300;", d);
        Assert.Contains("letter-spacing: -0.01em;", d);
        var ek = Kural(b, "html:root[data-sema=\"yildiz\"] .yz .yz-ekle");
        Assert.Contains("font-size: 15px;", ek);
        Assert.Contains("letter-spacing: .04em;", ek);
        Assert.Contains("padding: 16px 0;", ek);
        Assert.Contains("height: 32px;", b[b.IndexOf("html:root[data-sema=\"yildiz\"] .yz .yz-duzen :is(.btn, .dugme):not(#_)", StringComparison.Ordinal)..]);
        Assert.Contains("letter-spacing: .08em;", b[b.IndexOf("html:root[data-sema=\"yildiz\"] .yz .yz-duzen :is(.btn, .dugme):not(#_)", StringComparison.Ordinal)..]);
    }

    [Fact]
    public void Y4C_41_css_eski_gorunum_yildiz_duzende_yok_ve_karmada_cift_yok()
    {
        var b = Blok4C();
        // kutu/dolgu/gölge yok
        var kap = Kural(b, "html:root[data-sema=\"yildiz\"] .yz .yz-duzen :is(.kart, .kart-govde, .form-grid-2, .form-grid-3, .form-grid-4, .form-dikey, .satir, .satir--alt-hizali, .fis-ust-ayrinti, .alan-kontrol, .fis-aldim-durum)");
        Assert.Contains("border: 0;", kap);
        Assert.Contains("background: transparent;", kap);
        Assert.Contains("box-shadow: none;", kap);
        var bant = Kural(b, "html:root[data-sema=\"yildiz\"] .yz .yz-duzen :is(.bant, .bant-hata, .bant-basari, .bant-uyari, .bant-bilgi)");
        Assert.Contains("background: transparent;", bant);
        Assert.Contains("border: 0;", bant);
        // karma: özgün kart başlığı (ikinci "Ödeme" + "+ Başka ödeme ekle") ve özgün para üstü düğmesi çizilmez
        Assert.Matches(@"\.yz-tezgah-karma \.kart-baslik,[^{]*\{ display: none; \}", b);
        Assert.Contains("html:root[data-sema=\"yildiz\"] .yz .yz-duzen #islem-para-ustu-ac:not(#_):not(#__) { display: none; }", b);
        // tur 3 → 4C-2: "Çıkar" (.b2) adla tutar arasında; tutar tuvaldeki gibi satırın sağ ucunda — ✕ yok (Y4C_46, Y4C2_06)
        Assert.Contains("grid-column: 2;", Kural(b, "html:root[data-sema=\"yildiz\"] .yz .yz-tezgah-karma .bacak-satiri > .yz-bacak-sil"));
    }

    [Fact]
    public void Y4C_44_css_bugun_sol_sutun_yapiskan_ve_ana_icerik_hapsetmez()
    {
        // Kurucu 24.10: Bugün sol sütunu ilk ekran boyunda yapışkan; `.ana-icerik{overflow:auto}` yapışkanı hapsetmesin.
        var c = Css();
        Assert.Contains("html:root[data-sema=\"yildiz\"] .yildiz-bugun .yildiz-bugun-sol:not(#_) { align-self: start; position: sticky; top: 72px; min-height: calc(100vh - 72px - 72px - 56px); }", c);
        Assert.Contains("html:root[data-sema=\"yildiz\"] .ana-icerik:has(#yildiz-bugun) { overflow-x: clip; overflow-y: visible; }", c);
    }

    [Fact]
    public void Y4C_42_css_kaydet_ve_bantlar_ekranda_genis_sabit_dar_altta_sabit()
    {
        var b = Blok4C();
        // Kurucu 24.10: geniş ekranda sütunlar ekran boyuna kilitlenmez, iç kaydırma yok — sayfa kayar; Fark+Kaydet yapışkan.
        Assert.Contains("html:root[data-sema=\"yildiz\"] .yz-tezgah { min-height: calc(100vh - 72px); }", b);
        Assert.DoesNotMatch(@"\.yz-tezgah :is\(\.yz-tezgah-kalemler, \.yz-tezgah-duzen\) \{[^}]*overflow-y: auto", b);
        Assert.DoesNotMatch(@"\.yz-tezgah-sag:not\(#_\) \{ overflow-y: auto", b);
        var i = b.IndexOf("@media (max-width: 1000px)", StringComparison.Ordinal);
        var dar = b[i..];
        var alt = Kural(dar, "  html:root[data-sema=\"yildiz\"] .yz-tezgah .yz-tezgah-alt");
        // YILDIZ-4D: sabit (fixed) + 300 px boşluk tahmini yerine akışta yapışkan şerit (Yildiz4DTestleri.Y4D_05).
        Assert.Contains("position: sticky;", alt);
        Assert.Contains("bottom: 0;", alt);
        Assert.Contains("padding-bottom: 0;", Kural(dar, "  html:root[data-sema=\"yildiz\"] .yz-tezgah .yz-tezgah-ana"));
        Assert.Contains("background: var(--yuzey-govde);", alt);
    }

    [Fact]
    public void Y4C_43_css_kapsam_yalniz_yildiz_ve_sabit_renk_yok()
    {
        var b = Blok4C();
        // Her kural seçicisi (virgülle ayrılan her parça) Yıldız kapsamında; @media satırları atlanır.
        foreach (Match m in Regex.Matches(b, @"([^{}]+)\{([^{}]*)\}"))
        {
            var sec = m.Groups[1].Value.Trim();
            if (sec.StartsWith("@")) sec = sec[(sec.IndexOf('{') + 1)..].Trim();
            foreach (var parca in sec.Split(",\n"))
                if (parca.Trim().Length > 0)
                    Assert.True(parca.Contains("html:root[data-sema=\"yildiz\"]"), "kapsam dışı seçici: " + parca.Trim());
            Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgba?\(|hsla?\(", m.Groups[2].Value);   // gövdede sabit renk yok — yalnız var()
        }
        // odak alt çizgisi tuvaldeki gibi --p (hakem 4B tur 2)
        Assert.Contains("html:root[data-sema=\"yildiz\"] .yz .yz-alan:focus-within { outline: 0; border-bottom-color: var(--kenarlik); }", Css());
    }

    // ═══ YILDIZ-4C tur 3 — tasarım hakemi KRİTİK: (1) müşteri katmanı 30.000 ₺'de kendiliğinden açılıp kalemleri örtüyordu;
    //     (2) karmada BASİT "Satan" satırı, eski kart ve .btn ✕ öğeleri Yıldız'da görünüyordu. ═══

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Y4C_44_esik_ustunde_musteri_katmani_kendiliginden_acilmaz_sebep_fark_satirinda(string mod)
    {
        var e = Satis(mod);
        for (var i = 0; i < 4; i++) KalemEkle(e);   // 4 × 10.000 ₺ → Peşin Müşteri haddi (30.000, sunucudan) aşılır
        e.WaitForAssertion(() => Assert.Contains("40.000", e.Find("#yildiz-tezgah-toplam-rakam").TextContent), TimeSpan.FromSeconds(5));
        e.WaitForAssertion(() => Assert.True(e.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled")));
        // katman KENDİLİĞİNDEN açılmaz — kalemler ve toplam örtülmez
        Assert.Empty(e.FindAll("#yildiz-tezgah-musteri"));
        Assert.Equal("false", e.Find("#yildiz-tezgah-degistir").GetAttribute("aria-expanded"));
        // kapı AYNI: Kaydet kilitli, sebebi Fark satırında (UX-S30)
        Assert.Contains("müşteri seçin", e.Find("#yildiz-tezgah-not").TextContent, StringComparison.OrdinalIgnoreCase);
        // müşteri yalnız "Değiştir"le açılır; seçince kapı açılır
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll(".yz-tezgah-sol > #yildiz-tezgah-musteri #yildiz-musteri-secici")));
        e.Find("#yildiz-musteri-sec-c1").Click();
        e.WaitForAssertion(() => Assert.False(e.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled")), TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Y4C_45_karmada_eski_kart_btn_ve_satan_yok_satirlar_tuval_sat_kalibinda(string mod)
    {
        var e = Satis(mod);
        KalemEkle(e);
        e.WaitForAssertion(() => Assert.Single(e.FindAll(".yz-tezgah-kalem")), TimeSpan.FromSeconds(5));
        Assert.DoesNotContain("kart", e.Find(".islem-fis-kart").ClassList);   // sayfa sarmalayıcısı eski .kart değil
        e.Find("#yildiz-tezgah-baska-odeme").Click();
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll(".bacak-satiri").Count));
        var bacaklar = e.Find("#islem-bacaklar");
        Assert.Contains("yz-bacaklar", bacaklar.ClassList);
        Assert.Equal("", string.Join(" ", e.FindAll(".yz-tezgah-sag .kart, .yz-tezgah-sag .kart-baslik, .yz-tezgah-sag .btn, #islem-bacak-ekle")
            .Select(x => x.TagName + "#" + x.Id + "." + x.ClassName)));
        Assert.Empty(e.FindAll("#islem-satan-sar, #islem-satan, #islem-personel"));   // BASİT "Satan" satırı tuvalde yok
        Assert.Empty(e.FindAll("#islem-para-ustu-ac"));   // para üstü Yıldız hapında
        for (var i = 0; i < 2; i++)
        {
            var sil = e.Find($"#islem-bacak-sil-{i}");
            Assert.Contains("yz-b2", sil.ClassList);
            Assert.Equal("Çıkar", sil.TextContent.Trim());
            Assert.Equal($"{i + 1}. ödeme satırını çıkar", sil.GetAttribute("aria-label"));
            Assert.Equal("· Ana Kasa", e.Find($"#yildiz-bacak-{i}-kasa").TextContent.Trim());   // tuval "Nakit · Ana Kasa"
            Assert.Single(e.FindAll($".bacak-satiri .yz-bacak-ad #islem-bacak-{i}-arac"));
        }
        e.Find("#islem-bacak-sil-1").Click();
        e.WaitForAssertion(() => Assert.True(e.FindAll(".bacak-satiri").Count < 2));
        // TEMA TEMİZLİĞİ 17.10: "öteki şema AYNEN" (Platin karması) yarısı kaldırıldı — başka şema yok.
    }

    [Fact]
    public void Y4C_46_css_katman_akista_karma_cikar_b2_satirin_ucunda()
    {
        var css = Css();
        var katman = Kural(css, "html:root[data-sema=\"yildiz\"] .yz-tezgah .yz-tezgah-musteri");
        Assert.Contains("position: static;", katman);
        Assert.DoesNotContain("position: absolute;", katman);
        Assert.Contains("background: var(--yuzey-govde);", katman);   // opak zemin
        var sil = Kural(css, "html:root[data-sema=\"yildiz\"] .yz .yz-tezgah-karma .bacak-satiri > .yz-bacak-sil");
        Assert.Contains("grid-column: 2;", sil);   // 4C-2: tutar sağ uçta (grid-column 3), Çıkar arada
        Assert.Contains("grid-row: 1;", sil);
        Assert.Contains("display: flex;", Kural(css, "html:root[data-sema=\"yildiz\"] .yz .yz-tezgah-karma .bacak-satiri > .yz-bacak-ad"));
        Assert.DoesNotContain("left: -40px;", css);
        Assert.DoesNotContain("margin-left: -48px;", css);
    }

    [Fact]
    public void Y4C_47_pesin_yukleme_hatasi_katman_kapaliyken_not_satirinda_gorunur()
    {
        sistemCariHatasi = true;
        var e = Satis(ArayuzModuServisi.Basit);
        var bek = PesinMusteriSozlugu.Al("tr").YuklemeHatasi;
        // katman kendiliğinden açılmaz; hata yutulmaz (UX-S28) — Fark bloğunun not satırında
        e.WaitForAssertion(() => Assert.Contains(e.FindAll("[id^='yildiz-tezgah-uyari-']"), u => u.TextContent.Contains(bek)), TimeSpan.FromSeconds(5));
        Assert.Empty(e.FindAll("#yildiz-tezgah-musteri"));
        // "Değiştir" açınca katmandaki özgün yerinde (#islem-pesin-hata) — iki kez yazılmaz
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#yildiz-tezgah-musteri #islem-pesin-hata")));
        Assert.DoesNotContain(e.FindAll("[id^='yildiz-tezgah-uyari-']"), u => u.TextContent.Contains(bek));
    }
}
