using System.Globalization;
using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET SATIS-3 — TÜM İŞLEM TÜRLERİ AYNI SADELİKTE
//
// Kurucu 25.09: ~11:50 "satış alış ne kadar ekran varsa hepsi için aynı anlayış" ·
// ~12:05 (KAYNAK 0) "ürün adını yazıyorum liste olarak gelmiyor … elle ekle gibi bir
// seçeneğe gerek yok, tek kalsın" · ~12:00 (KAYNAK 2) karma ödeme ekranındaki 10 madde.
//
// Sınanan:
//   • Tek arama kutusu: yazdıkça debounce'lu TEK /urunler?q= isteği, liste, ↓+Enter ekler,
//     barkod+Enter doğrudan ekler (arama isteği iptal), olmayan metin → Bulunamadı + elle
//     ekle satırı, boş odakta yüklü liste (istek yok), arama hatası → hata bandı; ayrı
//     "Elle ekle" düğmesi YOK; aynı kutu Alış'ta da.
//   • Ödeme: "Ödeme" / "+ Başka ödeme ekle"; tek kasada bacak başına kasa seçici yok ama
//     gövdede kasa var; otomatik kalan (elle yazılmayan bacak kalanı üstlenir); fark anlamı;
//     POS 0 → hiçbir şey, 1 → otomatik, 2 → seçici; taksit "+ Taksit" ile açılır ve gövdeye girer.
//   • Hurda: BASİT'te teknik not gizli, satıcı kimliği açılışta görünür (zorunlu, eşiksiz),
//     adres etiketi "isteğe bağlı" DEMEZ, adressiz Kaydet kilitli + sebep; adres gövdeye girer.
//   • Tüm türlerde açıklama/reyon "+ Ayrıntı ekle" altında kapalı, açılınca gövdeye girer.
//   • İade: kaynak seçilmeden Kaydet kilitli.
//   • Diğer formları: isteğe bağlı alan ve geçmiş/ikincil bölüm kapalı katlanır.
// ⚠ Sabit para metni YOK (Bicim ile üretilir).
// ═════════════════════════════════════════════════════════════════════════════

public sealed partial class SATIS3EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string posCihazlari = "[]";
    private bool aramaHata;
    private bool tevsikHata;   // t7: ikinci önizleme isteği düşerse (had PRO'da saklanmış olmalı)
    private string tevsikJson = """{"had":50000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":50000,"islemTutariKuraliDevrede":false}""";
    private (string Uc, string Govde)? postRed;   // t10
    /// <summary>t19: belirli gereksinim sorgusuna (tutar/müşteri) sunucu yanıtı — null = varsayılan "YOK".</summary>
    private Func<string, string?>? gereksinimOzel;
    private const string GereksinimAmac = """{"kademe":"AMAC_BEYANI","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""";
    private const string GereksinimKimlik = """{"kademe":"YALNIZ_KIMLIK","kimlikGerekli":true,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""";
    /// <summary>k2: altın bacağı ayarları — varsayılan oransız (eski testler); K19/satıcı testleri oranlı verir.</summary>
    private string altinAyarJson = """{"stopajOranlari":[],"alisIskontoOran":null,"saticiAdresiZorunlu":true}""";
    private const string OranliAltinAyar = """{"stopajOranlari":[{"saticiTipi":"NIHAI_TUKETICI","oran":0,"pusulaGerekir":true},{"saticiTipi":"ESNAF_MUAF","oran":0.02,"pusulaGerekir":true},{"saticiTipi":"MUKELLEF","oran":0,"pusulaGerekir":false}],"alisIskontoOran":null,"saticiAdresiZorunlu":true}""";
    private string kasalarJson = """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""";

    public SATIS3EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private const string Urun1 = """{"id":"u1","ad":"Burma Bilezik 22","ayarId":"K22","hasGrami":3.94,"iscilikTl":0,"satisFiyati":10000,"gramAgirlik":4.3,"milyem":916,"barkod":"8690000000412","kod":"K-1"}""";
    private const string Urun2 = """{"id":"u2","ad":"Hasır Bilezik","ayarId":"K14","ayar":"14 Ayar","hasGrami":2.1,"iscilikTl":0,"satisFiyati":null,"gramAgirlik":3.6,"milyem":585,"barkod":"8690000000999","kod":"K-2"}""";

    private sealed class SahteApi(SATIS3EkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            if (istek.Method == HttpMethod.Post && t.postRed is { } red && yol.Contains(red.Uc))   // t10: sunucu reddi taklidi
                return new(System.Net.HttpStatusCode.BadRequest)
                { Content = new StringContent(red.Govde, System.Text.Encoding.UTF8, "application/json") };
            if (yol.Contains("/tevsik/gunluk") && t.tevsikHata)
                return new(System.Net.HttpStatusCode.InternalServerError)
                { Content = new StringContent("""{"message":"önizleme düştü"}""", System.Text.Encoding.UTF8, "application/json") };
            if (yol.Contains("/urunler?") && yol.Contains("q=") && t.aramaHata)
                return new(System.Net.HttpStatusCode.InternalServerError)
                { Content = new StringContent("""{"message":"arama çöktü"}""", System.Text.Encoding.UTF8, "application/json") };
            var bugun = DateTime.UtcNow.ToString("o");
            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") =>
                    """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/tevsik/gunluk") => t.tevsikJson,
                var v when v.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende Varsayilan","varsayilan":true,"aktif":true}]""",
                var v when v.Contains("/fiyat/teklif") && v.Contains("urunId=u2") => """{"teklifVar":false,"sebep":"LISTE_YOK"}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler/barkod/8690000000412") => Urun1,
                var v when v.Contains("/urunler/barkod/") => "__404__",
                var v when v.Contains("/urunler?") && v.Contains("q=zzz") => "[]",
                var v when v.Contains("/urunler?") && v.Contains("q=") => $"[{Urun1},{Urun2}]",
                var v when v.Contains("/urunler?") && v.Contains("siralama=cok-satan") => $"[{Urun2},{Urun1}]",   // sunucu sırası (en çok satılan önce)
                var v when v.Contains("/urunler") => $"[{Urun1},{Urun2}]",
                var v when v.Contains("/kasalar") => t.kasalarJson,
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Anadolu Holding A.Ş.","vergiNo":"10000000146","kimlikTuru":"TCKN","adres":"Ulus, Ankara"}]""",
                var v when v.Contains("/masak/gereksinim") && t.gereksinimOzel?.Invoke(v) is { } oz => oz,   // t19
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => t.masakEsikJson,   // t24: eşik testlerde ayarlanır
                var v when v.Contains("/islemler/personeller") => """{"secimSerbest":false,"oturumId":"u1","personeller":[{"id":"u1","ad":"Test","soyad":"Tezgâhtar"}],"dahaFazlasiVar":false}""",
                var v when v.Contains("/islemler/altin-bacagi/ayarlar") => t.altinAyarJson,
                var v when v.Contains("/islemler/iade-adaylari") =>
                    $$"""[{"id":"h1","tarih":"{{bugun}}","tlTutari":12400,"urunId":"u1","urunAdi":"Burma Bilezik 22","toplamAdet":1,"iadeEdilenAdet":0,"kalanAdet":1,"cariId":"c1"}]""",
                var v when v.Contains("/islemler/satis") || v.Contains("/islemler/alis") || v.Contains("/islemler/hurda") =>
                    """{"id":"01J00000000000000000000001","fisId":"01J00000000000000000000002","tur":"SATIS","tlTutari":10000}""",
                var v when v.Contains("/reyon") => "[]",
                var v when v.Contains("/pos/cihazlar") => t.posCihazlari,
                _ => "{}",
            };
            if (y == "__404__")
                return new(System.Net.HttpStatusCode.NotFound)
                { Content = new StringContent("""{"message":"bulunamadı"}""", System.Text.Encoding.UTF8, "application/json") };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod = ArayuzModuServisi.Profesyonel, string rol = "TEZGAHTAR")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", rol, "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        // TEMA TEMİZLİĞİ 17.10: SATIŞ fişi Yıldız tezgâhıdır (#yildiz-tezgah); Peşin Müşteri varsayılanı üst satırda yazılır.
        e.WaitForElement("#yildiz-tezgah-kaydet", Bekle);
        e.WaitForAssertion(() => Assert.Equal("pm1", GA.MusteriId(e)), Bekle);
        e.WaitForAssertion(() => Assert.StartsWith("Peşin Müşteri", e.Find("#yildiz-tezgah-ust-musteri").TextContent.Trim()), Bekle);
        return e;
    }

    private static Satis3Metinleri S3 => Satis3Sozlugu.Tr;
    /// <summary>Debounce (220 ms) + sahte istek + çizim — yük altında 1 sn varsayılanı dar.</summary>
    private static readonly TimeSpan Bekle = TimeSpan.FromSeconds(5);
    private int AramaIstegi(string q) { lock (istekler) return istekler.Count(i => i.Yol.Contains("/urunler?") && i.Yol.Contains($"q={q}")); }
    private int HerhangiArama() { lock (istekler) return istekler.Count(i => i.Yol.Contains("/urunler?") && i.Yol.Contains("q=")); }
    private (string Metot, string Yol, string Govde) SonPost(string uc) { lock (istekler) return istekler.Last(i => i.Metot == "POST" && i.Yol.Contains(uc)); }

    private static void Yaz(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string metin)
    {
        var k = e.Find("#islem-barkod");
        k.Focus();
        for (var i = 1; i <= metin.Length; i++) e.Find("#islem-barkod").Input(metin[..i]);
    }

    // ═══ KAYNAK 0 — TEK ARAMA KUTUSU ═════════════════════════════════════════

    [Fact]
    public void Arama_harf_harf_yazilinca_DEBOUNCE_ile_TEK_istek_ve_liste_gorunur()
    {
        var e = Ekran();
        Yaz(e, "bil");
        e.WaitForAssertion(() => Assert.True(e.FindAll("#islem-barkod-liste [role=option]").Count >= 2), Bekle);
        Assert.Equal(1, AramaIstegi("bil"));   // "b" ve "bi" için istek YOK
        Assert.Equal(1, HerhangiArama());
        Assert.Equal("true", e.Find("#islem-barkod").GetAttribute("aria-expanded"));
        var satir = e.Find("#islem-barkod-oneri-0").TextContent;
        Assert.Contains("Burma Bilezik 22", satir);
        Assert.Contains(Bicim.Tl(10000m), satir);
        Assert.Contains("22 Ayar", satir);                                            // UX-S31: ayar ADI, ham kod değil
        Assert.Equal("—", e.Find("#islem-barkod-oneri-1 .fis-arama-fiyat").TextContent.Trim()); // KAYNAK 0: bilinmeyen fiyat "—"
    }

    [Fact]
    public void Ok_asagi_ve_Enter_ONERIYI_kalem_olarak_ekler()
    {
        var e = Ekran();
        Yaz(e, "bil");
        e.WaitForElement("#islem-barkod-oneri-0", Bekle);
        e.Find("#islem-barkod").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        e.WaitForAssertion(() => Assert.True("islem-barkod-oneri-0" == e.Find("#islem-barkod").GetAttribute("aria-activedescendant"), e.Find("#islem-barkod-kutu").OuterHtml));
        e.Find("#islem-barkod").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        e.WaitForAssertion(() => Assert.Contains("Burma Bilezik 22", e.Find("#yildiz-tezgah-kalemler").TextContent));
        Assert.Empty(e.FindAll("#islem-barkod-liste"));
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Yol.Contains("/urunler/barkod/"));   // seçim barkod ucuna gitmez
    }

    [Fact]
    public void ARANIRKEN_ok_ve_Enter_elle_satirini_ACMAZ_barkod_okur()
    {
        var e = Ekran();
        e.Find("#islem-barkod").Focus();
        e.Find("#islem-barkod").Input("8690000000412");
        e.Find("#islem-barkod").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        e.Find("#islem-barkod").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        e.WaitForAssertion(() => Assert.Contains("Burma Bilezik 22", e.Find("#yildiz-tezgah-kalemler").TextContent));
        Assert.False(e.Find("#islem-elle").HasAttribute("open"));
    }

    [Fact]
    public void Fare_ile_secim_de_ayni_kapidan_ekler()
    {
        var e = Ekran();
        Yaz(e, "bil");
        e.WaitForElement("#islem-barkod-oneri-0", Bekle);
        e.WaitForAssertion(() => e.Find("#islem-barkod-oneri-0").MouseDown(), Bekle);
        e.WaitForAssertion(() => Assert.Contains("Burma Bilezik 22", e.Find("#yildiz-tezgah-kalemler").TextContent));
    }

    [Fact]
    public void Fiyati_cikmayan_oneri_elle_satirini_ACAR_ve_sebep_yazar()
    {
        var e = Ekran();
        Yaz(e, "has");
        e.WaitForElement("#islem-barkod-oneri-1", Bekle);
        e.WaitForAssertion(() => e.Find("#islem-barkod-oneri-1").MouseDown(), Bekle);
        e.WaitForAssertion(() => Assert.True(e.Find("#islem-elle").HasAttribute("open")));
        Assert.Equal("u2", SeciciYardim.SeciliDeger(e, "islem-urun"));
    }

    [Fact]
    public void Barkod_dizisi_ve_Enter_DOGRUDAN_ekler_bekleyen_arama_IPTAL()
    {
        var e = Ekran();
        Yaz(e, "8690000000412");
        e.Find("#islem-barkod").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        e.WaitForAssertion(() => Assert.Contains("Burma Bilezik 22", e.Find("#yildiz-tezgah-kalemler").TextContent));
        Thread.Sleep(Kuyumcu.Web.Bilesenler.Fis.FisKalemGirisi.BeklemeMs + 200);
        Assert.Equal(0, HerhangiArama());   // okuyucu hızı: arama isteği hiç gitmez
        lock (istekler) Assert.Contains(istekler, i => i.Yol.Contains("/urunler/barkod/8690000000412"));
    }

    [Fact]
    public void Olmayan_metin_BULUNAMADI_ve_son_satir_ELLE_EKLE_acar()
    {
        var e = Ekran();
        Yaz(e, "zzz");
        e.WaitForElement("#islem-barkod-bulunamadi", Bekle);
        Assert.Equal(S3.Bulunamadi, e.Find("#islem-barkod-bulunamadi").TextContent.Trim());
        var elle = e.Find("#islem-barkod-elle");
        Assert.Equal(S3.ElleEkleSatiri, elle.TextContent.Trim());
        Assert.Equal("option", elle.GetAttribute("role"));
        Assert.False(e.Find("#islem-elle").HasAttribute("open"));
        elle.MouseDown();
        e.WaitForAssertion(() => Assert.True(e.Find("#islem-elle").HasAttribute("open")));
        Assert.NotNull(e.Find("#islem-elle #islem-urun"));   // elle kalem özelliği SİLİNMEDİ
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Ayri_ELLE_EKLE_dugmesi_DOMda_YOK(string mod)
    {
        var e = Ekran(mod);
        Assert.Empty(e.FindAll("#islem-elle-ac"));
        Assert.DoesNotContain(e.FindAll("button, summary"), b => b.TextContent.Trim() == TezgahFisiSozlugu.Tr.ElleEkle);
        Assert.All(e.FindAll("#islem-elle > summary"), s => Assert.True(s.HasAttribute("hidden")));
    }

    private int CokSatanIstegi() { lock (istekler) return istekler.Count(i => i.Yol.Contains("/urunler?") && i.Yol.Contains("siralama=cok-satan")); }

    [Fact]
    public void Bos_odakta_EN_COK_SATANLAR_ilk_odakta_TEK_istekle_sunucu_sirasiyla_gelir()
    {
        // SATIS-3 doğrulama bulgusu (ORTA): liste "son eklenen" diye koda göre ters sıradaydı; spesifikasyon
        // "odakta en çok satılan 8". Sunucu sıralar (GET /urunler?siralama=cok-satan&limit=8), sayfa açılışına istek eklenmez.
        var e = Ekran();
        Assert.Equal(0, CokSatanIstegi());
        e.Find("#islem-barkod").Click();                 // t13: liste kullanıcı eylemiyle açılır (odak tek başına açmaz)
        e.WaitForElement("#islem-barkod-liste", Bekle);
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll("#islem-barkod-liste [role=option]").Count), Bekle);
        Assert.Equal("Hasır Bilezik", e.Find("#islem-barkod-oneri-0 .fis-arama-ad").TextContent.Trim());   // sunucu sırası korunur
        Assert.Equal(S3.BosOdakBaslik, e.Find("#islem-barkod-bos-baslik").TextContent.Trim());
        Assert.Equal("En çok satılanlar", S3.BosOdakBaslik);
        Assert.Contains(istekler, i => i.Yol.Contains("siralama=cok-satan") && i.Yol.Contains($"limit={Kuyumcu.Web.Bilesenler.Fis.FisKalemGirisi.BosOdakBoyu}"));
        Assert.Empty(e.FindAll("#islem-barkod-elle"));   // boş kutuda "elle ekle" satırı yok
        Assert.Equal(0, HerhangiArama());
        e.Find("#islem-barkod").Blur();
        Assert.Empty(e.FindAll("#islem-barkod-liste"));
        e.Find("#islem-barkod").Click();                 // ikinci açılış: önbellekten, yeni istek YOK
        e.WaitForElement("#islem-barkod-liste", Bekle);
        Assert.Equal(1, CokSatanIstegi());
    }

    [Fact]
    public void Zaten_odaktaki_kutuya_TIKLAMA_ve_OK_ASAGI_bos_odak_listesini_ACAR()
    {
        // SATIS-3 doğrulama (28.09, canlı ölçüm): kutu açılışta OdakJeton ile ZATEN odakta — tıklama yeni focus
        // olayı doğurmadığı için liste hiç açılmıyordu. Tıklama da, boş kutuda ↓ da listeyi açar (tek istek).
        var e = Ekran();
        e.Find("#islem-barkod").Click();
        e.WaitForElement("#islem-barkod-liste", Bekle);
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll("#islem-barkod-liste [role=option]").Count), Bekle);
        Assert.Equal(1, CokSatanIstegi());
        e.Find("#islem-barkod").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(e.FindAll("#islem-barkod-liste"));
        e.Find("#islem-barkod").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        e.WaitForElement("#islem-barkod-liste", Bekle);
        Assert.Equal("true", e.Find("#islem-barkod-oneri-0").GetAttribute("aria-selected"));
        Assert.Equal(1, CokSatanIstegi());   // önbellekten — yeni istek yok
        Assert.Equal(0, HerhangiArama());
    }

    // ═══ t13 — tasarım hakemi YÜKSEK (29.09): liste mod/sekme değişince kendiliğinden açılıp fişin üstünü kapatıyordu ═══

    [Fact]
    public void T13_ODAK_tek_basina_listeyi_ACMAZ_F2_ve_kisayol_F2_ACAR()
    {
        var e = Ekran();
        e.Find("#islem-barkod").Focus();                       // programın/sekmenin odak vermesi
        Assert.Empty(e.FindAll("#islem-barkod-liste"));
        Assert.Equal("false", e.Find("#islem-barkod").GetAttribute("aria-expanded"));
        Assert.Equal(0, CokSatanIstegi());                      // açılmayan liste için istek de gitmez
        e.Find("#islem-barkod").KeyDown(new KeyboardEventArgs { Key = "F2" });   // kutudayken F2
        e.WaitForElement("#islem-barkod-liste", Bekle);
        e.Find("#islem-barkod").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(e.FindAll("#islem-barkod-liste"));
        e.Find(".islem-sayfa").KeyDown(new KeyboardEventArgs { Key = "F2" });     // sayfa kısayolu F2 (odak başka yerde)
        e.WaitForElement("#islem-barkod-liste", Bekle);
        Assert.Equal(1, CokSatanIstegi());
    }

    [Fact]
    public void T13_MOD_ve_SEKME_degisince_liste_kendiliginden_ACILMAZ()
    {
        var e = Ekran();
        e.Find(".islem-sayfa").KeyDown(new KeyboardEventArgs { Key = "F2" });     // sayfanın F2 jetonu 0'dan büyük olsun
        e.WaitForElement("#islem-barkod-liste", Bekle);
        e.Find("#islem-barkod").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        e.Find("#islem-barkod").Blur();
        Assert.Empty(e.FindAll("#islem-barkod-liste"));
        var modu = Services.GetRequiredService<ArayuzModuServisi>();
        e.InvokeAsync(() => modu.Uygula(ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        e.WaitForElement("#islem-barkod");
        e.Find("#islem-barkod").Focus();
        Assert.Empty(e.FindAll("#islem-barkod-liste"));
        e.InvokeAsync(() => modu.Uygula(ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel));
        e.WaitForElement("#islem-tur-HURDA_ALIS");
        e.Find("#islem-barkod").Focus();
        Assert.Empty(e.FindAll("#islem-barkod-liste"));
        e.Find("#islem-tur-HURDA_ALIS").Click();
        e.Find("#islem-tur-SATIS").Click();
        e.WaitForElement("#islem-barkod");
        e.Find("#islem-barkod").Focus();
        Assert.Empty(e.FindAll("#islem-barkod-liste"));
        Assert.Equal(1, CokSatanIstegi());   // yalnız F2'nin açtığı liste için
    }

    [Fact]
    public void T13_ELLE_satiri_yaptigi_isi_soyler_ve_ARANAN_metni_seciciye_tasir()
    {
        var e = Ekran();
        Yaz(e, "zzqqxx");
        e.WaitForElement("#islem-barkod-elle", Bekle);
        var elle = e.Find("#islem-barkod-elle").TextContent.Trim();
        Assert.Equal("Ürün seç, fiyatı kendin yaz", elle);            // mevcut ürün + elle fiyat — "listede olmayan ürün" DEMEZ
        Assert.DoesNotContain("olmayan", elle);
        Assert.Equal("Pick a product, enter the price yourself", Satis3Sozlugu.En.ElleEkleSatiri);
        e.Find("#islem-barkod-elle").MouseDown();
        e.WaitForAssertion(() => Assert.True(e.Find("#islem-elle").HasAttribute("open")));
        Assert.Equal(string.Format(S3.ElleAranan, "zzqqxx"), e.Find("#islem-urun .secici-metin").TextContent.Trim());
        Assert.Contains("zzqqxx", e.Find("#islem-urun .secici-metin").TextContent);
    }

    [Fact]
    public void Arama_HATASI_hata_bandi_olur_BULUNAMADI_DEMEZ()
    {
        aramaHata = true;
        var e = Ekran();
        Yaz(e, "bil");
        e.WaitForElement("#islem-barkod-arama-hata", Bekle);
        Assert.Contains("arama çöktü", e.Find("#islem-barkod-arama-hata").TextContent);
        Assert.Empty(e.FindAll("#islem-barkod-bulunamadi"));
    }

    [Fact]
    public void Alis_turunde_AYNI_arama_kutusu_calisir()
    {
        var e = Ekran();
        e.Find("#islem-tur-ALIS").Click();
        e.WaitForElement("#islem-barkod");
        Yaz(e, "bil");
        e.WaitForElement("#islem-barkod-oneri-0", Bekle);
        Assert.Equal(1, AramaIstegi("bil"));
    }

    // ═══ KAYNAK 2 / 5 — ÖDEME ════════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> KarmaFis()
    {
        var e = Ekran();
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForElement(".yz-tezgah-kalem");
        GA.KarmaAc(e);     // TEMA TEMİZLİĞİ 17.10: tezgâhın "+ Başka ödeme"si → tek ödeme İLK satır (fişin tamamı) + boş İKİNCİ satır
        return e;
    }

    // TEMA TEMİZLİĞİ 17.10: KarmaTekSatir kaldırıldı — tezgâhta karmanın ikinci satırı "Çıkar"la silinince sayfa kendiliğinden
    // tek ödemeye döner (YildizTezgah: TekOdemeyeDonAracla; YildizTezgahOdemeTestleri'nde sınanır). Kart satırı senaryoları
    // aşağıdaki KartBacagi'nda tezgâhın ulaşılabilir hâliyle kurulur (ilk satır Kart · fişin tamamı, ikinci satır boş Havale).

    [Theory]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    [InlineData(ArayuzModuServisi.Basit)]
    public void Baska_odeme_ekle_ILK_tikta_IKI_satir_acar_bos_satir_Kaydeti_KILITLEMEZ_govdeye_girmez(string mod)
    {
        // SATIS-3 doğrulama bulgusu (DÜŞÜK): ilk tıkta yalnız 1 satır açılıyordu, ikinci satır için ikinci tık gerekiyordu.
        var e = Ekran(mod);
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled")));
        GA.KarmaAc(e);
        Assert.Equal(2, e.FindAll("#islem-bacaklar .bacak-satiri").Count);
        Assert.Equal("10000", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar"));
        Assert.Equal("0", SeciciYardim.SeciliDeger(e, "islem-bacak-1-tutar"));
        // k2 (tasarım hakemi K19, 29.09): "Fark 0 ✓" iken boş (0) satır Kaydet'i KİLİTLEMEZ ("Ödeme tutarını girin" çelişkisi)
        // ve sunucuya GİTMEZ — gövdede yalnız tutarı olan satır. Kilit sebebi yok.
        Assert.False(GA.Kaydet(e).HasAttribute("disabled"));
        Assert.Null(GA.Kaydet(e).GetAttribute("title"));
        Assert.DoesNotContain(S3.OdemeTutariGirin, e.Find("#yildiz-tezgah-alt").TextContent);
        Assert.NotEmpty(e.FindAll("#yildiz-tezgah-fark .yz-tezgah-onay"));   // tezgâhın Fark satırı "0 ₺ ✓"
        Assert.DoesNotContain("bacak", e.Find("#yildiz-tezgah-odeme-baslik").TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bacak", e.Find("#islem-bacaklar").TextContent, StringComparison.OrdinalIgnoreCase);
        // t3 hakemi ("fark üç kez"): TEMA TEMİZLİĞİ 17.10 — tezgâhta iki modda da "Alınan toplam" satırı yok (fark Fark bloğunda).
        Assert.DoesNotContain(S3.OdemeToplami, e.Find("#islem-bacaklar").TextContent);
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis-fisi")), Bekle);
        var odemeler = JsonDocument.Parse(SonPost("/islemler/satis-fisi").Govde).RootElement.GetProperty("odemeler");
        Assert.Equal(1, odemeler.GetArrayLength());
        Assert.Equal(10_000m, odemeler[0].GetProperty("tutarTl").GetDecimal());
    }

    // ═══ k2 (tasarım hakemi K19, 29.09) — karma ödemede toplam tutar: tek kapı + boş satır kalan çalmaz ═══

    [Theory]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    [InlineData(ArayuzModuServisi.Basit)]
    public void K19_Baska_odeme_sonra_Eski_altin_KART_KALANA_duser_bos_satir_hedef_olmaz(string mod)
    {
        altinAyarJson = OranliAltinAyar;
        // Hakem: Kart tüm tutar + "+ Başka ödeme ekle" (boş satır) + Eski altın → Kart kalanda kalıyordu, fark −63.101.
        var e = Ekran(mod);
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled")));
        // Ayrı "Eski altın" düğmesi YOK — tek kapı.
        Assert.Empty(e.FindAll("#islem-odeme-altin"));
        GA.KarmaAc(e);
        GA.BacakEkle(e);                                           // üçüncü satır (boş) — kalan onu hedef almaz
        e.WaitForElement("#islem-bacak-2-arac");
        SeciciYardim.Sec(e, "islem-bacak-2-arac", "ALTIN");
        e.WaitForElement("#altin-bacagi");
        SeciciYardim.Sec(e, "altin-ayar", "K22");
        e.Find("#altin-gram").Input("2");
        e.Find("#altin-fiyat").Input("1000");
        // İlk (tutarlı, elle yazılmamış) satır kalanı üstlenir; boş satır 0'da kalır; fark 0.
        e.WaitForAssertion(() =>
        {
            var altinTl = decimal.Parse(SeciciYardim.SeciliDeger(e, "altin-tl")!, System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(altinTl > 0m);
            Assert.Equal((10_000m - altinTl).ToString(System.Globalization.CultureInfo.InvariantCulture), SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar"));
            Assert.Equal("0", SeciciYardim.SeciliDeger(e, "islem-bacak-1-tutar"));
            Assert.Equal("0", e.Find("#islem-bacak-fark").GetAttribute("data-deger"));
        }, Bekle);
    }

    /// <summary>k2: kalemli fiş + seçili müşteri (c1 ya da Peşin) + tek kapıdan eski altın satırı.</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> EskiAltinFisi(string mod, string cari)
    {
        altinAyarJson = OranliAltinAyar;
        var e = Ekran(mod);
        if (cari != "pm1") GA.MusteriSec(e, cari);
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled")));
        GA.KarmaAc(e);
        e.WaitForElement("#islem-bacak-1-arac");
        SeciciYardim.Sec(e, "islem-bacak-1-arac", "ALTIN");
        e.WaitForElement("#altin-bacagi");
        return e;
    }

    [Fact]
    public void K2_BASIT_satici_kimligi_SECILI_MUSTERIDEN_dolar_yalniz_eksik_alan_gorunur_govdede_musteri()
    {
        // Hakem: "Müşteri Anadolu Holding A.Ş. seçili ama satıcı alanları boş; 6 maddelik 'Kaydetmek için' listesi; iç içe çerçeve."
        var e = EskiAltinFisi(ArayuzModuServisi.Basit, "c1");
        e.WaitForAssertion(() => Assert.Contains("Anadolu Holding A.Ş.", e.Find("#altin-satici-ozet").TextContent), Bekle);
        Assert.Equal(Services.GetRequiredService<DilServisi>().T.Islem.CaridenGeldi, e.Find("#altin-satici-kaynak").TextContent.Trim());
        // Müşteriden dolan ad/TCKN/adres kutuları BASİT'te çizilmez; satıcı türü de özet satırında.
        foreach (var id in new[] { "altin-satici-ad", "altin-satici-tckn", "altin-satici-adres", "altin-satici-tip" })
            Assert.Empty(e.FindAll("#" + id));
        Assert.DoesNotContain("10000000146", e.Find("#altin-bacagi").TextContent);   // TCKN ekrana basılmaz
        // İç içe çerçeve yok: satıcı bölümü fieldset DEĞİL.
        Assert.Equal("DIV", e.Find("#altin-satici").TagName);
        Assert.Empty(e.FindAll("#altin-bacagi fieldset"));
        // "Kaydetmek için" listesi görsel olarak yok (ekran okuyucuya kalır).
        if (e.FindAll("#altin-eksikler").FirstOrDefault() is { } liste) Assert.True(liste.HasAttribute("hidden"));
        // "Değiştir" hepsini açar, değerler müşteriden ve rozetli.
        e.Find("#altin-satici-degistir").Click();
        Assert.Equal("Anadolu Holding A.Ş.", SeciciYardim.SeciliDeger(e, "altin-satici-ad"));
        Assert.Equal("10000000146", SeciciYardim.SeciliDeger(e, "altin-satici-tckn"));
        Assert.NotEmpty(e.FindAll("#altin-satici-tip"));

        SeciciYardim.Sec(e, "altin-ayar", "K22");
        e.Find("#altin-gram").Input("2");
        e.Find("#altin-fiyat").Input("4000");   // ISLEM-BANT-2: 1.000 oran dışıydı (bant 3.400–7.480 → Kaydet kilitli)
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled")), Bekle);
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis-fisi")), Bekle);
        var odemeler = JsonDocument.Parse(SonPost("/islemler/satis-fisi").Govde).RootElement.GetProperty("odemeler");
        var altin = odemeler.EnumerateArray().Single(o => o.GetProperty("arac").GetString() == "ALTIN");
        var satici = altin.GetProperty("satici");
        Assert.Equal("Anadolu Holding A.Ş.", satici.GetProperty("adSoyad").GetString());
        Assert.Equal("10000000146", satici.GetProperty("tckn").GetString());
        Assert.Equal("Ulus, Ankara", satici.GetProperty("adres").GetString());
        var toplam = odemeler.EnumerateArray().Sum(o => o.GetProperty("tutarTl").GetDecimal());
        Assert.Equal(10_000m, toplam);   // otomatik kalan: kart + eski altın = fiş toplamı
    }

    [Fact]
    public void K2_Pesin_musteride_satici_alanlari_bos_EKSIK_alan_altinda_mesaj_liste_yok()
    {
        var e = EskiAltinFisi(ArayuzModuServisi.Basit, "pm1");
        // Peşin Müşteri'den doldurulmaz; eksik alan kutuları görünür, açılışta kırmızı mesaj YOK.
        Assert.Equal("", SeciciYardim.SeciliDeger(e, "altin-satici-ad") ?? "");
        Assert.Empty(e.FindAll("#altin-satici-ad-hata"));
        SeciciYardim.Sec(e, "altin-ayar", "K22");
        e.Find("#altin-gram").Input("2");
        e.Find("#altin-fiyat").Input("1000");
        // Değer hesaplanınca eksik satıcı alanları KENDİ kutusunun altında söylenir.
        e.WaitForAssertion(() => Assert.Equal(Services.GetRequiredService<DilServisi>().T.Islem.SaticiAdZorunlu, e.Find("#altin-satici-ad-hata").TextContent.Trim()), Bekle);
        Assert.Equal(Services.GetRequiredService<DilServisi>().T.Islem.SaticiTcknZorunlu, e.Find("#altin-satici-tckn-hata").TextContent.Trim());
        Assert.True(e.Find("#altin-eksikler").HasAttribute("hidden"));   // "Kaydetmek için" listesi ekranda yok
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
    }

    [Fact]
    public void K2_PRO_satici_alanlari_gorunur_musteriden_dolu_rozetli()
    {
        var e = EskiAltinFisi(ArayuzModuServisi.Profesyonel, "c1");
        e.WaitForAssertion(() => Assert.Equal("Anadolu Holding A.Ş.", SeciciYardim.SeciliDeger(e, "altin-satici-ad")), Bekle);
        Assert.Equal("Ulus, Ankara", SeciciYardim.SeciliDeger(e, "altin-satici-adres"));
        Assert.Equal(Services.GetRequiredService<DilServisi>().T.Islem.CaridenGeldi, e.Find("#altin-satici-ad-kaynak").TextContent.Trim());
        Assert.Empty(e.FindAll("#altin-satici-ozet"));
    }

    [Fact]
    public void K19_nakit_kullanilamayan_fiste_yeni_odeme_satiri_NAKIT_acilmaz()
    {
        Assert.Equal("KART", FisFark.YeniBacakAraci([], nakitsiz: true));
        Assert.Equal("HAVALE", FisFark.YeniBacakAraci([new FisBacagi { Arac = "KART" }], nakitsiz: true));
        Assert.Equal("KART", FisFark.YeniBacakAraci([new FisBacagi { Arac = "KART" }, new FisBacagi { Arac = "HAVALE" }], nakitsiz: true));
        Assert.Equal("NAKIT", FisFark.YeniBacakAraci([new FisBacagi { Arac = "KART" }], nakitsiz: false));
    }

    [Fact]
    public void K19_OtoDengele_hedef_TUTARLI_satir_bos_satir_kalani_calmaz()
    {
        var kart = new FisBacagi { Arac = "KART", TutarTl = 10_000m };
        var bos = new FisBacagi { Arac = "HAVALE", TutarTl = 0m };
        var altin = new FisBacagi { Arac = FisAltinBacagi.Arac, TutarTl = 3_000m };
        var l = new List<FisBacagi> { kart, bos, altin };
        Assert.True(FisHesap.OtoDengele(l, 10_000m));
        Assert.Equal(7_000m, kart.TutarTl);
        Assert.Equal(0m, bos.TutarTl);
        Assert.Equal([kart, altin], FisFark.Gonderilecek(l));   // boş satır gövdeye girmez; altın her zaman
    }

    [Fact]
    public void Odeme_dili_Odeme_ve_Baska_odeme_ekle_BACAK_kelimesi_yok()
    {
        // TEMA TEMİZLİĞİ 17.10: ödeme bölümünün başlığı ve "başka ödeme" düğmesi tezgâhın sağ sütunundadır (tuval dili).
        var e = KarmaFis();
        Assert.Equal(S3.OdemeBaslik, e.Find("#yildiz-tezgah-odeme-baslik").TextContent.Trim());
        Assert.Equal(YildizTezgahSozlugu.BaskaOdeme("tr"), e.Find("#yildiz-tezgah-baska-odeme").TextContent.Trim());
        Assert.Contains("Başka ödeme", e.Find("#yildiz-tezgah-baska-odeme").TextContent);
        Assert.DoesNotContain("bacak", e.Find("#yildiz-tezgah-baska-odeme").TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bacak", e.Find("#islem-bacaklar").TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tek_kasada_bacak_basina_KASA_SECICI_YOK_govdede_kasa_VAR()
    {
        var e = KarmaFis();
        Assert.Empty(e.FindAll("#islem-bacak-0-kasa"));
        Assert.Empty(e.FindAll("#islem-bacak-1-kasa"));
        e.Find("#islem-bacak-1-tutar").Input("4000");   // ikinci ödeme dolar → ilk satır kalanı (6.000) üstlenir
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")));
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis-fisi")));
        var g = JsonDocument.Parse(SonPost("/islemler/satis-fisi").Govde).RootElement;
        Assert.Equal("ks1", g.GetProperty("odemeler")[0].GetProperty("kasaId").GetString());
        Assert.Equal("ks1", g.GetProperty("odemeler")[1].GetProperty("kasaId").GetString());   // ikinci satırın kasası da otomatik
    }

    [Fact]
    public void Otomatik_KALAN_elle_yazilmayan_bacak_kalani_ustlenir_ve_iki_bacak_GOVDEYE_girer()
    {
        var e = KarmaFis();                     // fiş 10.000 · satır 0 = 10.000 (tek ödemeden) · satır 1 = 0 (ilk tıkta açıldı)
        SeciciYardim.Sec(e, "islem-bacak-1-arac", "KART");
        e.Find("#islem-bacak-1-tutar").Input("4000");   // elle → bacak 0 kalanı üstlenir
        e.WaitForAssertion(() => Assert.Equal("6000", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")));
        Assert.Equal(S3.FarkTamam, e.Find("#islem-bacak-fark-metin").TextContent.Trim());
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")));
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis-fisi")));
        var od = JsonDocument.Parse(SonPost("/islemler/satis-fisi").Govde).RootElement.GetProperty("odemeler");
        Assert.Equal(2, od.GetArrayLength());
        Assert.Equal(6000m, od[0].GetProperty("tutarTl").GetDecimal());
        Assert.Equal("KART", od[1].GetProperty("arac").GetString());
        Assert.Equal(4000m, od[1].GetProperty("tutarTl").GetDecimal());
    }

    [Fact]
    public void Iki_bacak_da_elle_yazildiysa_FARK_anlamini_soyler_ve_Kaydet_kilitli()
    {
        var e = KarmaFis();
        e.Find("#islem-bacak-0-tutar").Input("8000");
        e.Find("#islem-bacak-1-tutar").Input("4000");   // 12.000 > 10.000
        e.WaitForAssertion(() => Assert.Equal(string.Format(S3.FarkFazla, Bicim.Tl(2000m)), e.Find("#islem-bacak-fark-metin").TextContent.Trim()));
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
        e.Find("#islem-bacak-1-tutar").Input("1000");    // 9.000 < 10.000
        e.WaitForAssertion(() => Assert.Equal(string.Format(S3.FarkEksik, Bicim.Tl(1000m)), e.Find("#islem-bacak-fark-metin").TextContent.Trim()));
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> KartBacagi()
    {
        var e = KarmaFis();
        SeciciYardim.Sec(e, "islem-bacak-1-arac", "HAVALE");   // boş (0 ₺) ikinci satır Kart olmasın — gövdeye girmez
        SeciciYardim.Sec(e, "islem-bacak-0-arac", "KART");
        e.WaitForAssertion(() => Assert.Equal("KART", SeciciYardim.SeciliDeger(e, "islem-bacak-0-arac")), Bekle);
        Assert.Equal("10000", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar"));
        return e;
    }

    [Fact]
    public void POS_cihazi_YOKSA_secici_de_not_da_YOK()
    {
        posCihazlari = "[]";
        var e = KartBacagi();
        Assert.Empty(e.FindAll("#islem-bacak-0-pos"));
        Assert.Empty(e.FindAll("#islem-bacak-0-pos-yok"));
        Assert.Empty(e.FindAll("#islem-bacak-0-taksit, #islem-bacak-0-taksit-ac"));
        Assert.DoesNotContain(Dil(e).BacakPosCihazYok, e.Markup);
    }

    private static SozlukIslem Dil(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> _) => Sozlukler.Tr.Islem;

    private const string TekPos = """[{"id":"pc1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"s1","kasaId":"ks1","pesinKomisyonOran":1.5,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}]""";

    [Fact]
    public void TEK_POS_otomatik_secici_yok_TAKSIT_arti_ile_acilir_ve_GOVDEYE_girer()
    {
        posCihazlari = TekPos;
        var e = KartBacagi();
        Assert.Empty(e.FindAll("#islem-bacak-0-pos"));
        Assert.Empty(e.FindAll("#islem-bacak-0-taksit"));
        e.Find("#islem-bacak-0-taksit-ac").Click();
        e.WaitForElement("#islem-bacak-0-taksit");
        SeciciYardim.Sec(e, "islem-bacak-0-taksit", "3");
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")));
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis-fisi")));
        var b = JsonDocument.Parse(SonPost("/islemler/satis-fisi").Govde).RootElement.GetProperty("odemeler")[0];
        Assert.Equal("pc1", b.GetProperty("posCihazId").GetString());
        Assert.Equal(3, b.GetProperty("posTaksitAdedi").GetInt32());
    }

    [Fact]
    public void IKI_POS_varsa_secici_SORULUR()
    {
        posCihazlari = "[" + TekPos.Trim('[', ']') + ",{\"id\":\"pc2\",\"ad\":\"Garanti POS\",\"bankaAdi\":\"Garanti\",\"subeId\":\"s1\",\"kasaId\":\"ks1\",\"pesinKomisyonOran\":1.5,\"valorGun\":1,\"paraBirimi\":\"TRY\",\"aktif\":true,\"bekleyenAlacak\":0,\"bugunYatacakNet\":0,\"tahsilatVar\":false}]";
        var e = KartBacagi();
        Assert.NotNull(e.Find("#islem-bacak-0-pos"));
        Assert.False(e.Find("#islem-bacak-0-pos").HasAttribute("disabled"));
    }

    // ═══ MOD DEĞİŞİMİ ödemeyi SİLMEZ (doğrulama bulgusu ORTA) ═════════════════

    [Fact]
    public void PRO_dan_BASIT_e_geciste_KARMA_odeme_KORUNUR_ve_GOVDEYE_girer()
    {
        var e = KarmaFis();
        SeciciYardim.Sec(e, "islem-bacak-1-arac", "KART");
        e.Find("#islem-bacak-1-tutar").Input("4000");
        e.WaitForAssertion(() => Assert.Equal("6000", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")));
        e.InvokeAsync(() => Services.GetRequiredService<ArayuzModuServisi>().Uygula(ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        e.WaitForAssertion(() => Assert.NotNull(e.Find(".tuval-tezgah")), Bekle);
        Assert.Equal("KART", SeciciYardim.SeciliDeger(e, "islem-bacak-1-arac"));
        Assert.Equal("4000", SeciciYardim.SeciliDeger(e, "islem-bacak-1-tutar"));
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")));
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis-fisi")));
        var od = JsonDocument.Parse(SonPost("/islemler/satis-fisi").Govde).RootElement.GetProperty("odemeler");
        Assert.Equal(2, od.GetArrayLength());
        Assert.Equal("KART", od[1].GetProperty("arac").GetString());
    }

    [Fact]
    public void PRO_tek_KART_odeme_BASIT_e_geciste_Nakite_DONMEZ()
    {
        var e = Ekran();
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForElement(".yz-tezgah-kalem");
        GA.OdemeSec(e, "KART");
        e.InvokeAsync(() => Services.GetRequiredService<ArayuzModuServisi>().Uygula(ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        // PARA-DEMO-2 (madde 1): tek KART ödemesi BASİT'te "tek satırlık ödeme listesi"ne (karma → sunucuda KARMA etiketi)
        // ÇEVRİLMEZ; tek ödeme satırı Kart'ı gösterir ve seçtirir. Nakit'e de dönmez.
        e.WaitForElement("#islem-aldim-odeme", Bekle);   // TEMA TEMİZLİĞİ 17.10: ödeme düzeni (PRO'da açılan) BASİT'te de açık kalır
        Assert.Equal("KART", SeciciYardim.SeciliDeger(e, "islem-aldim-odeme"));
        Assert.Contains(Sozlukler.Tr.Islem.OdemeKart, e.Find("#yildiz-odeme-0").TextContent);   // tezgâhın ödeme satırı Kart der
        Assert.Empty(e.FindAll("#islem-bacak-0-arac"));
    }

    // ═══ HURDA ALIŞ ═══════════════════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Hurda(string mod)
    {
        var e = Ekran(mod, "MUDUR");
        e.Find("#islem-tur-HURDA_ALIS").Click();
        e.WaitForElement("#hurda-satici");
        return e;
    }

    [Fact]
    public void Hurda_BASIT_teknik_not_GIZLI_satici_kimligi_ACILISTA_gorunur_adres_isteğe_bagli_DEMEZ()
    {
        var e = Hurda(ArayuzModuServisi.Basit);
        Assert.True(e.Find("#hurda-satici-not").HasAttribute("hidden"));
        Assert.Empty(e.FindAll("#hurda-satici-tip-hata"));   // doğrulama bulgusu: açılışta, dokunulmadan kırmızı mesaj YOK
        // KAYNAK 2 md.1+2: kaynak rozeti etiket SATIRINDA (id aynı), fiyat kutusu Gram/Ayar ile AYNI ızgara hücresi düzeyinde
        Assert.Contains("Kurdan geldi", e.Find("label[for=hurda-fiyat] #hurda-fiyat-kaynak").TextContent);
        Assert.Empty(e.FindAll(".form-grid-3 > .alan > .alan #hurda-fiyat"));
        Assert.Contains("18 Ayar", e.Find("#hurda-ayar").TextContent);   // UX-S31: "K18" değil
        Assert.NotNull(e.Find("#hurda-satici-tip"));   // yasal zorunlu → açılışta, katlı DEĞİL
        Assert.Null(e.Find("#hurda-satici").Closest("details"));
        SeciciYardim.Sec(e, "hurda-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#hurda-satici-adres");
        var etiket = e.Find("label[for=hurda-satici-adres]").TextContent;
        Assert.Contains(S3.SaticiAdres, etiket);
        Assert.DoesNotContain("isteğe", etiket, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("numeric", e.Find("#hurda-satici-tckn").GetAttribute("inputmode"));
    }

    [Fact]
    public void Hurda_PRO_teknik_not_GORUNUR()
    {
        var e = Hurda(ArayuzModuServisi.Profesyonel);
        Assert.False(e.Find("#hurda-satici-not").HasAttribute("hidden"));
    }

    [Fact]
    public void Hurda_ADRESSIZ_kaydet_KILITLI_sebep_yazar_adres_girilince_acilir_ve_GOVDEYE_girer()
    {
        var e = Hurda(ArayuzModuServisi.Profesyonel);
        GA.MusteriSec(e, "c1");
        e.Find("#hurda-gram").Input("10");
        SeciciYardim.Sec(e, "hurda-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#hurda-satici-ad");
        e.Find("#hurda-satici-ad").Input("Ali Veli");
        e.Find("#hurda-satici-tckn").Input("10000000146");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#hurda-satici-adres-hata")));
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
        Assert.False(string.IsNullOrEmpty(GA.Kaydet(e).GetAttribute("title")));
        e.Find("#hurda-satici-adres").Input("Kapalıçarşı No:1");
        // SATIS-3 t10: tutar sunucu haddini aşıyor → Nakit'te Kaydet kilitli (istemci yasal kilidi); havaleye geçilir.
        if (e.FindAll("#islem-odeme").Count > 0) GA.OdemeSec(e, "HAVALE");
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")));
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/hurda")));
        var g = SonPost("/islemler/hurda").Govde;
        Assert.Contains("Kapalıçarşı No:1", System.Text.RegularExpressions.Regex.Unescape(g));
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Hurda_KILIT_SEBEBI_bos_acilista_da_GORUNUR(string mod)
    {
        // SATIS-3 doğrulama (28.09, canlı ölçüm — UX-S30): kalemsiz türde sebep yalnız FisDolu iken görünüyordu;
        // Hurda alışta gram yazılsa da Kaydet SEBEPSİZ kilitliydi.
        // P-TEZGAH-TUR-KIMLIK tur 2 (tasarım hakemi KRİTİK): "Kaydet kilitliyse sebebi tek cümle yanında" — boş açılışta da.
        var e = Hurda(mod);
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
        Assert.DoesNotContain("gorunmez", e.Find("#islem-kaydet-kilit").ClassName ?? "");
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#islem-kaydet-kilit").TextContent));
        e.Find("#hurda-gram").Input("10");
        e.WaitForAssertion(() => Assert.DoesNotContain("gorunmez", e.Find("#islem-kaydet-kilit").ClassName ?? ""));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#islem-kaydet-kilit").TextContent));
    }

    [Fact]
    public void Hurda_cariden_dolan_ad_ELLE_degisince_CARIDEN_GELDI_rozeti_DUSER()
    {
        // SATIS-3 doğrulama (28.09, canlı görüntü — UX-S21): ad elle yazıldığı hâlde "Müşteriden geldi" rozeti kalıyordu.
        var e = Hurda(ArayuzModuServisi.Profesyonel);
        GA.MusteriSec(e, "c1");
        SeciciYardim.Sec(e, "hurda-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#hurda-satici-ad-kaynak", Bekle);
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#hurda-satici-ad").GetAttribute("value")));
        e.Find("#hurda-satici-ad").Input("Mehmet Yılmaz");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hurda-satici-ad-kaynak")), Bekle);
        Assert.Equal("Mehmet Yılmaz", e.Find("#hurda-satici-ad").GetAttribute("value"));
    }

    // ═══ İSTEĞE BAĞLI ALANLAR — TÜM TÜRLER ═══════════════════════════════════

    [Theory]
    [InlineData("ALIS")]
    [InlineData("HURDA_ALIS")]
    [InlineData("TAKAS")]
    [InlineData("SATIS_IADE")]
    public void Aciklama_TUM_turlerde_ARTI_AYRINTI_altinda_KAPALI(string tur)
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel, "MUDUR");
        e.Find($"#islem-tur-{tur}").Click();
        e.WaitForElement("#islem-aciklama");
        var d = e.Find("#islem-aciklama").Closest("details")!;
        Assert.Equal("islem-diger-ayrinti", d.Id);
        Assert.False(d.HasAttribute("open"));
    }

    [Fact]
    public void Alis_acilinca_yazilan_ACIKLAMA_govdeye_girer()
    {
        var e = Ekran();
        e.Find("#islem-tur-ALIS").Click();
        e.WaitForElement("#islem-urun");
        GA.MusteriSec(e, "c1");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))));
        e.Find("#islem-kalem-ekle").Click();
        e.Find("#islem-aciklama").Input("vitrin sayımı");
        e.WaitForAssertion(() => Assert.True(e.Find("#islem-diger-ayrinti").HasAttribute("open")));
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")));
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/alis")));
        Assert.Contains("vitrin sayımı", System.Text.RegularExpressions.Regex.Unescape(SonPost("/islemler/alis").Govde));
    }

    [Fact]
    public void Iade_KAYNAK_secilmeden_Kaydet_KILITLI()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel, "MUDUR");
        e.Find("#islem-tur-SATIS_IADE").Click();
        e.WaitForElement("#iade-kaynak");
        GA.MusteriSec(e, "c1");   // müşteri seçili olsa bile kaynak fiş seçilmeden kayıt YOK
        e.WaitForAssertion(() => Assert.Equal(S3.IadeKaynakSecin, GA.Kaydet(e).GetAttribute("title")));   // t11: eksik olan KAYNAK
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#iade-adet"));   // tutar/adet kaynaktan gelir — seçilmeden sorulmaz
        SeciciYardim.Sec(e, "iade-kaynak", "h1");
        e.WaitForElement("#iade-adet");
        // UX-S24 + t11 (hakem YÜKSEK): müşteri kaynaktan gelir ve KİLİTLİ — seçici çizilmez, salt okunur ad yazılır
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-cari")));
        Assert.Contains("Anadolu Holding A.Ş.", e.Find("#islem-iade-musteri").TextContent);
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")));
    }

    // ═══ t3 TASARIM HAKEMİ (28.09) ═══════════════════════════════════════════

    [Theory]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    [InlineData(ArayuzModuServisi.Basit)]
    public void T3_Takas_yeni_urun_TEK_arama_kutusundan_secilir_kalem_EKLENMEZ_govdeye_girer(string mod)
    {
        var e = Ekran(mod);
        e.Find("#islem-tur-TAKAS").Click();
        e.WaitForElement("#takas-eski-gram");
        Assert.Empty(e.FindAll("#takas-yeni-fiyat"));          // zorunlu önce: ürün seçilmeden adet/fiyat yok
        Assert.Null(e.Find("#takas-elle").GetAttribute("open")); // eski açılır liste katlı (özellik silinmedi)
        Yaz(e, "bil");
        e.WaitForElement("#islem-barkod-oneri-0", Bekle);
        e.WaitForAssertion(() => e.Find("#islem-barkod-oneri-0").MouseDown(), Bekle);
        e.WaitForAssertion(() => Assert.Contains("Burma Bilezik 22", e.Find("#takas-yeni-secili").TextContent));
        Assert.Empty(e.FindAll("#islem-kalem-pro"));            // kalem EKLENMEDİ
        Assert.Empty(e.FindAll("#islem-kalem-tablo"));
        Assert.Null(e.Find("#islem-elle").GetAttribute("open")); // elle satırı açık kalmadı
        Assert.Equal("10.000", e.Find("#takas-yeni-fiyat").GetAttribute("value"));   // teklif fiyatı
        GA.MusteriSec(e, "c1");
        ParaDemoEkranYardim.TakasSaticiDoldur(e);   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
        e.Find("#takas-eski-gram").Input("5");
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")));
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains("\"yeniUrunId\":\"u1\"", SonPost("/islemler/takas").Govde));
    }

    [Fact]
    public void T3_BASIT_Kaydet_TUR_ADI_tasimaz_PRO_sekme_adiyla_ayni()
    {
        var b = Ekran(ArayuzModuServisi.Basit);
        b.Find("#islem-tur-HURDA_ALIS").Click();
        b.WaitForAssertion(() => Assert.Equal(Sozlukler.Tr.Islem.Kaydet, GA.Kaydet(b).TextContent.Trim()));
        var p = Ekran(ArayuzModuServisi.Profesyonel);
        p.Find("#islem-tur-HURDA_ALIS").Click();
        p.WaitForAssertion(() => Assert.Equal($"{Sozlukler.Tr.Islem.HurdaAlis} {Sozlukler.Tr.Islem.Kaydet}", GA.Kaydet(p).TextContent.Trim()));
    }

    [Fact]
    public void T3_PRO_Yasal_kapilar_karti_YALNIZ_satista_diger_turde_Bilinmiyor_basmaz()
    {
        // TEMA TEMİZLİĞİ 17.10: satışta yasal durum tezgâhın Fark bloğundaki not satırındadır (eski PRO yan sütun kartı
        // #islem-pro-yasal satışta çizilmez); öteki türde ne o satır ne kart "Bilinmiyor" basar; satışa dönünce satır geri gelir.
        var e = Ekran();
        e.WaitForAssertion(() => Assert.StartsWith(YildizTezgahSozlugu.YasalUygun("tr"), e.Find("#yildiz-tezgah-not").TextContent.Trim()), Bekle);
        Assert.Empty(e.FindAll("#islem-pro-yasal"));
        e.Find("#islem-tur-HURDA_ALIS").Click();
        e.WaitForElement("#hurda-satici", Bekle);
        Assert.Empty(e.FindAll("#islem-pro-yasal"));
        Assert.Empty(e.FindAll("#yildiz-tezgah-not"));
        Assert.DoesNotContain(SatisProSozlugu.Tr.Bilinmiyor, e.Markup, StringComparison.Ordinal);
        e.Find("#islem-tur-SATIS").Click();
        e.WaitForAssertion(() => Assert.StartsWith(YildizTezgahSozlugu.YasalUygun("tr"), e.Find("#yildiz-tezgah-not").TextContent.Trim()), Bekle);
    }

    [Fact]
    public void T3_Karma_kilit_sebebi_FARKI_soyler_ODEME_BACAKLARI_demez()
    {
        var e = KarmaFis();
        e.Find("#islem-bacak-0-tutar").Input("4000");
        e.Find("#islem-bacak-1-tutar").Input("3000");   // ikisi de elle → otomatik kalan yok, eksik 3.000
        e.WaitForAssertion(() => Assert.Equal(string.Format(S3.FarkEksik, Bicim.Tl(3000m)), e.Find("#yildiz-tezgah-not").TextContent.Trim()));   // TEMA TEMİZLİĞİ: Kaydet'in yanındaki tek cümle tezgâhın not satırı
        Assert.Equal(string.Format(S3.FarkEksik, Bicim.Tl(3000m)), GA.Kaydet(e).GetAttribute("title"));
        Assert.DoesNotContain("bacak", e.Find("#yildiz-tezgah-not").TextContent, StringComparison.OrdinalIgnoreCase);
    }

    // ═══ t6 — doğrulama bulguları: şube kasası · kaynak rozeti · başlık · para üstü ══════════════

    private const string IkiSubeKasalari =
        """[{"id":"ks-a","ad":"Kadıköy Kasa","paraBirimi":"TRY","aktif":true,"subeId":"s2"},{"id":"ks-b","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true,"subeId":"s1"}]""";

    [Fact]
    public void T6_Varsayilan_kasa_OTURUMUN_subesinden_BASKA_subenin_kasasi_secilmez()
    {
        // Sunucu sırasında ilk kasa BAŞKA şubenin (s2) — tezgâhtar s1'de. Eski kural AnaKasalar[0] = ks-a idi.
        kasalarJson = IkiSubeKasalari;
        var e = Ekran(ArayuzModuServisi.Basit);
        e.WaitForAssertion(() => Assert.Contains("Merkez Kasa", e.Find("#yildiz-odeme-0").TextContent), Bekle);   // TEMA TEMİZLİĞİ: tezgâhın ödeme satırı "Nakit · kasa"
        Assert.DoesNotContain("Kadıköy", e.Find("#yildiz-odeme-0").TextContent);
        GA.OdemeDuzenAc(e);
        e.WaitForAssertion(() => Assert.Contains("Merkez Kasa", e.Find("#islem-kasa-tek").TextContent), Bekle);
        Assert.DoesNotContain("Kadıköy", e.Find("#islem-kasa-tek").TextContent);
        Assert.Empty(e.FindAll("#islem-kasa"));
    }

    [Fact]
    public void T6_Subede_kasa_YOKSA_BASIT_te_de_kasa_secici_gorunur_fail_closed()
    {
        kasalarJson = """[{"id":"ks-a","ad":"Kadıköy Kasa","paraBirimi":"TRY","aktif":true,"subeId":"s2"},{"id":"ks-c","ad":"Bostancı Kasa","paraBirimi":"TRY","aktif":true,"subeId":"s3"}]""";
        var e = Ekran(ArayuzModuServisi.Basit);
        Assert.DoesNotContain("Kadıköy", e.Find("#yildiz-odeme-0").TextContent);   // TEMA TEMİZLİĞİ: tezgâh satırı başka şubenin kasasını yazmaz
        GA.OdemeDuzenAc(e);
        e.WaitForElement("#islem-kasa", Bekle);
        Assert.Empty(e.FindAll("#islem-kasa-tek"));
        Assert.True(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-kasa")));   // sessizce başka şubenin kasası atanmadı
    }

    [Theory]
    [InlineData("s1", "ks-b")]     // oturum şubesi → o şubenin ilk kasası
    [InlineData("s9", null)]       // şubede kasa yok → null (seçici zorunlu)
    [InlineData(null, null)]       // şubesiz oturum + kasalar iki şubeye dağılmış → null
    public void T6_VarsayilanKasa_saf_kural(string? sube, string? beklenen)
    {
        var kasalar = new List<Kuyumcu.Contracts.Sayfalar.Islemler.KasaVeri>
        {
            new("ks-a", "Kadıköy", "TRY", true, "s2"), new("ks-b", "Merkez", "TRY", true, "s1"), new("ks-d", "Merkez 2", "TRY", true, "s1"),
        };
        Assert.Equal(beklenen, Kuyumcu.Web.Bilesenler.Fis.FisHesap.VarsayilanKasa(kasalar, sube));
        // Şube bilgisi taşımayan eski yanıt: ilk kasa (davranış korunur); tek şubede şubesiz oturum: ilk kasa.
        Assert.Equal("x1", Kuyumcu.Web.Bilesenler.Fis.FisHesap.VarsayilanKasa([new("x1", "A", "TRY", true), new("x2", "B", "TRY", true)], "s1"));
        Assert.Equal("ks-b", Kuyumcu.Web.Bilesenler.Fis.FisHesap.VarsayilanKasa([kasalar[1], kasalar[2]], null));
    }

    [Fact]
    public void T6_Kurdan_geldi_rozeti_PRO_dan_BASIT_e_geciste_KORUNUR()
    {
        var e = KarmaFis();
        SeciciYardim.Sec(e, "islem-bacak-1-arac", "ALTIN");
        e.WaitForElement("#altin-fiyat-kurdan", Bekle);
        e.Find("#altin-fiyat-kurdan").Click();
        e.WaitForAssertion(() => Assert.Equal(Services.GetRequiredService<DilServisi>().T.Islem.KurdanGeldi, e.Find("#altin-fiyat-kaynak").TextContent.Trim()));
        e.InvokeAsync(() => Services.GetRequiredService<ArayuzModuServisi>().Uygula(ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        e.WaitForAssertion(() => Assert.NotNull(e.Find(".tuval-tezgah")), Bekle);
        e.WaitForAssertion(() => Assert.Equal(Services.GetRequiredService<DilServisi>().T.Islem.KurdanGeldi, e.Find("#altin-fiyat-kaynak").TextContent.Trim()), Bekle);
        // Elle yazınca rozet "Kurdan geldi"den düşer (kaynak dürüst kalır).
        e.Find("#altin-fiyat").Input("7000");
        e.WaitForAssertion(() => Assert.NotEqual(Services.GetRequiredService<DilServisi>().T.Islem.KurdanGeldi, e.Find("#altin-fiyat-kaynak").TextContent.Trim()));
    }

    private DilServisi Dil() => Services.GetRequiredService<DilServisi>();

    [Fact]
    public void T6_Sayfa_basligi_SECILI_turu_tasir_BASIT_karo_adi_PRO_sekme_adi()
    {
        var b = Ekran(ArayuzModuServisi.Basit);
        var menuAdi = MenuAdlari.Icin(Dil().T).Satis.Ad;
        Assert.Equal(menuAdi, b.Find("#sayfa-basi-islemler-baslik").TextContent.Trim());
        b.Find("#islem-tur-TAKAS").Click();
        b.WaitForAssertion(() => Assert.Equal(SatisTuvalSozlugu.Tr.KaroDegisim, b.Find("#sayfa-basi-islemler-baslik").TextContent.Trim()));
        b.Find("#islem-tur-HURDA_ALIS").Click();
        b.WaitForAssertion(() => Assert.Equal(SatisTuvalSozlugu.Tr.KaroAlisEskiAltin, b.Find("#sayfa-basi-islemler-baslik").TextContent.Trim()));
    }

    [Fact]
    public void T6_Sayfa_basligi_PRO_da_sekme_adi()
    {
        var p = Ekran();
        p.Find("#islem-tur-TAKAS").Click();
        p.WaitForAssertion(() => Assert.Equal(Services.GetRequiredService<DilServisi>().T.Islem.Takas, p.Find("#sayfa-basi-islemler-baslik").TextContent.Trim()));
    }

    [Fact]
    public void T6_Karmada_para_ustu_ACILISTA_kapali_arti_ile_acilir_GOVDEYE_girmez()
    {
        // TEMA TEMİZLİĞİ 17.10: tezgâhta para üstü "+ Para üstü" hapıdır (karmada NAKİT satırlarının toplamı tabanı).
        var e = KarmaFis();
        e.WaitForElement("#yildiz-tezgah-para-ustu", Bekle);
        Assert.Empty(e.FindAll("#islem-alinan"));
        Assert.Equal(YildizTezgahSozlugu.ParaUstu("tr"), e.Find("#yildiz-tezgah-para-ustu").TextContent.Trim());
        Assert.Equal("false", e.Find("#yildiz-tezgah-para-ustu").GetAttribute("aria-expanded"));
        e.WaitForAssertion(() => Assert.False(e.Find("#yildiz-tezgah-para-ustu").HasAttribute("disabled")), Bekle);
        e.Find("#yildiz-tezgah-para-ustu").Click();
        e.WaitForElement("#islem-alinan", Bekle);
        e.Find("#islem-alinan").Input("20000");
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis-fisi")), Bekle);
        Assert.DoesNotContain("20000", SonPost("/islemler/satis-fisi").Govde);   // alınan nakit gövdeye GİRMEZ
    }

    [Fact]
    public void T6_Nakit_kurali_bandi_YALNIZ_odemede_nakit_varken_kartla_odenen_fiste_celiskili_bant_YOK()
    {
        // Sunucu önizlemesi: bu fiş nakit kuralına takılıyor (islemTutariKuraliDevrede). Ödeme NAKİT → bant "nakit alınamaz";
        // kullanıcı KART'a geçince aynı bant "nakit alınamaz — kartla alın" DEMEZ (hakem ORTA: tevsik çelişkisi).
        tevsikJson = """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":true}""";
        var e = Ekran(ArayuzModuServisi.Basit);
        Yaz(e, "bil");
        e.WaitForElement("#islem-barkod-liste [role=option]", Bekle);
        e.Find("#islem-barkod").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        e.Find("#islem-barkod").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        GA.NakitSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        // TEMA TEMİZLİĞİ 17.10: özgün BASİT bant (#islem-yasal-kapi) tezgâhta display:none — aynı cümle Fark bloğunun not satırında.
        e.WaitForAssertion(() => Assert.Contains(S3.NakitAlinamaz, e.Find("#yildiz-tezgah-not").TextContent), Bekle);
        GA.OdemeSec(e, "KART");
        e.WaitForAssertion(() => Assert.DoesNotContain(S3.NakitAlinamaz, GA.TumMetin(e)), Bekle);
    }

    // ═══ t7 — doğrulayıcı bulguları ═══════════════════════════════════════════════

    [Fact]
    public void T7_Fiyati_cikmayan_oneride_SUNUCU_SEBEBI_yazar_gumus_metni_DEGIL_kalem_eklenince_bant_KALKAR()
    {
        var e = Ekran();
        Yaz(e, "has");
        e.WaitForElement("#islem-barkod-oneri-1", Bekle);
        e.WaitForAssertion(() => e.Find("#islem-barkod-oneri-1").MouseDown(), Bekle);
        // Sahte sunucu u2 için LISTE_YOK döndürür (altın ürün, fiyat listesi yok) — gümüşe özgü metin BASILMAZ.
        e.WaitForAssertion(() => Assert.Contains(S3.FiyatElleListeYok, e.Find("#islem-barkod-hata").TextContent), Bekle);
        Assert.DoesNotContain(Sozlukler.Tr.Urun.BarkodFiyatGirilmeli, e.Find("#islem-barkod-hata").TextContent);
        Assert.Empty(e.FindAll("#islem-teklif-yok"));   // aynı sebep ikinci kez (sarı satır) yazılmaz
        e.Find("#islem-birimfiyat").Input("5000");
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForElement(".yz-tezgah-kalem", Bekle);
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-barkod-hata")), Bekle);
    }

    [Theory]
    [InlineData(null, "KUR")]
    [InlineData("LISTE_YOK", "LISTE")]
    [InlineData("KOTASYON_YOK", "GUMUS")]
    [InlineData("FIYAT_YOK", "HESAP")]
    [InlineData("HATA", "BAG")]
    public void T7_Fiyat_sebebi_metni_sebep_koduna_gore(string? sebep, string beklenen)
    {
        var e = Ekran();
        var yontem = typeof(Kuyumcu.Web.Pages.Islemler).GetMethod("FiyatHesaplanamadiMetni",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var t = new Kuyumcu.Contracts.Sayfalar.Islemler.Teklif(false, sebep, null, null, null, null, null, null, null, false, null);
        if (beklenen == "KUR") typeof(Kuyumcu.Web.Pages.Islemler).GetField("kurId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(e.Instance, null);
        var metin = (string)yontem.Invoke(e.Instance, [t])!;
        var s = beklenen switch
        {
            "KUR" => S3.FiyatElleKurYok,
            "LISTE" => S3.FiyatElleListeYok,
            "GUMUS" => Sozlukler.Tr.Urun.BarkodFiyatGirilmeli,
            "BAG" => S3.FiyatElleBaglanti,
            _ => S3.FiyatElleHesaplanamadi,
        };
        Assert.Equal(s, metin);
    }

    [Fact]
    public void T7_PRO_dan_BASIT_e_geciste_nakit_uyari_kutusu_0_TL_fiste_GIZLI_ve_sunucu_haddi_KORUNUR()
    {
        var e = Ekran();   // PRO açılış: yasal kapılar kartı /tevsik/gunluk ister (had 50.000)
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/tevsik/gunluk")), Bekle);
        tevsikHata = true;   // BASİT'e geçişteki ikinci istek düşse bile had bilinir (sayısız metin + açık kutu YOK)
        e.InvokeAsync(() => Services.GetRequiredService<ArayuzModuServisi>().Uygula(ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        e.WaitForAssertion(() => Assert.NotNull(e.Find(".tuval-tezgah")), Bekle);
        e.WaitForAssertion(() =>
        {
            var k = e.Find("#islem-nakit-uyari");
            Assert.True(k.HasAttribute("hidden"), k.OuterHtml);
            Assert.Contains(Bicim.Tl(50000m), k.TextContent);
        }, Bekle);
        // TEMA TEMİZLİĞİ 17.10: tezgâhta haddi kullanıcıya söyleyen yer not satırıdır — had (sunucudan, saklanmış) orada da korunur.
        e.WaitForAssertion(() => Assert.Contains(Bicim.Tl(50000m), e.Find("#yildiz-tezgah-not").TextContent), Bekle);
    }

    [Fact]
    public void T13_PRO_sekme_donusunde_yasal_kapi_ONCEKI_fisin_ASILDI_durumunu_TASIMAZ()
    {
        // t13 canlı ölçümde bulundu: 94.959 ₺'lik fiş → Hurda Alış → Satış; kalemler boşalmış (0 ₺) ama kart "Aşıldı".
        tevsikJson = """{"had":5000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":5000,"islemTutariKuraliDevrede":true}""";
        // TEMA TEMİZLİĞİ 17.10: eski PRO yan sütunun "Aşıldı/Uygun" hücresi (#islem-pro-nakit) satışta çizilmez; tezgâhta yasal durum
        // Fark bloğunun not satırıdır — engel varken kötü (yz-kotu), dönüşte önceki fişin engelini TAŞIMAZ ("Yasal kurallar uygun").
        var e = TekKalemli(ArayuzModuServisi.Profesyonel);
        e.WaitForAssertion(() => Assert.Contains("yz-kotu", e.Find("#yildiz-tezgah-not").ClassName), Bekle);
        Assert.DoesNotContain(YildizTezgahSozlugu.YasalUygun("tr"), e.Find("#yildiz-tezgah-not").TextContent);
        tevsikJson = TevsikJson(5000m);   // boş fişte sunucu kuralı devrede DEĞİL
        e.Find("#islem-tur-HURDA_ALIS").Click();
        e.Find("#islem-tur-SATIS").Click();
        e.WaitForAssertion(() =>
        {
            Assert.StartsWith(YildizTezgahSozlugu.YasalUygun("tr"), e.Find("#yildiz-tezgah-not").TextContent.Trim());
            Assert.DoesNotContain("yz-kotu", e.Find("#yildiz-tezgah-not").ClassName);
        }, Bekle);
    }

    // ═══ t10 — TASARIM HAKEMİ KRİTİK bulguları (28.09 gece) ═════════════════════════════════════

    private static string TevsikJson(decimal had) =>
        $$"""{"had":{{had.ToString(CultureInfo.InvariantCulture)}},"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":{{had.ToString(CultureInfo.InvariantCulture)}},"islemTutariKuraliDevrede":false}""";

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> TekKalemli(string mod)
    {
        var e = Ekran(mod);
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#islem-kalem-tablo tbody tr, #islem-kalem-pro tr.fis-pro-satir, .yz-tezgah-kalem")), Bekle);
        return e;
    }

    [Fact]
    public void T10_karma_KAYITTAN_SONRA_odeme_satirlari_ve_eski_altin_saticisi_SIFIRLANIR()
    {
        // Hakem KRİTİK: kayıttan sonra ödeme satırları ve satıcı kimliği sıradaki müşterinin fişine taşınıyordu.
        var e = KarmaFis();
        e.Find("#islem-bacak-1-tutar").Input("4000");
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")));
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis-fisi")));
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-bacaklar .bacak-satiri")), Bekle);   // tek ödeme varsayılanı
        e.WaitForElement("#yildiz-odeme-0", Bekle);                                                  // tezgâhın tek ödeme satırı geri geldi
        Assert.Empty(e.FindAll("#yildiz-odeme-1"));
        Assert.Empty(e.FindAll("#altin-satici-ad"));
        Assert.Empty(e.FindAll("#islem-bacak-fark-metin"));
    }

    [Fact]
    public void T10_tek_KART_odemeli_kayittan_sonra_odeme_NAKIT_varsayilanina_doner()
    {
        // Hakem KRİTİK: kayıt başarılı olunca ödeme durumu Vazgeç'teki gibi tek ödeme "Nakit · kasa" varsayılanına döner.
        tevsikJson = TevsikJson(5000m);
        var e = TekKalemli(ArayuzModuServisi.Basit);
        GA.MusteriSec(e, "c1");
        GA.OdemeSec(e, "KART");
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")), Bekle);
        e.WaitForElement("#yildiz-tezgah-son-fis", Bekle);   // kayıt sonrası sıfırlama bitti (TEMA TEMİZLİĞİ: tezgâhın sonuç satırı)
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-kalem-tablo tbody tr.fis-tuval-satir, #islem-kalem-pro tr.fis-pro-satir, .yz-tezgah-kalem")), Bekle);
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))), Bekle);
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#islem-kalem-tablo tbody tr, #islem-kalem-pro tr.fis-pro-satir, .yz-tezgah-kalem")), Bekle);
        GA.MusteriSec(e, "c1");
        // yeni fiş önceki müşterinin ELLE seçtiği kartı TAŞIMAZ: ödeme yeniden VARSAYILANDAN gelir — SATIS-4 (a) (Kurul 5):
        // tutar sunucu haddini aştığı için varsayılan kendiliğinden Kart ve bunu söyleyen not görünür (elle seçimde not yok).
        GA.OdemeDuzenAc(e);   // TEMA TEMİZLİĞİ: not tezgâhın ödeme düzeninde
        e.WaitForElement("#islem-odeme-otomatik", Bekle);
        Assert.Contains(Sozlukler.Tr.Islem.OdemeKart, e.Find("#yildiz-odeme-0").TextContent);
        GA.NakitSec(e);   // kullanıcı nakde dönerse aynı tutar yine nakit haddine takılır
        e.WaitForAssertion(() => Assert.Equal(string.Format(S3.NakitHaddiKilit, Bicim.Tl(5000m)), GA.Kaydet(e).GetAttribute("title")), Bekle);
    }

    [Fact]
    public void T10_baska_odeme_ikinci_satir_KART_acilir_tam_tutar_yazilinca_0_TL_nakit_satiri_DUSMEZ_govdeye_GIRMEZ()
    {
        // GOC-C-2 VERI-1 (tasarım hakemi, VERİ) — sözleşme güncellendi, GEVŞETİLMEDİ: t10 "0 ₺'lik nakit satırı düşer" diyordu;
        // rakam rakam yazımda (9 → 99999) ilk satır (Kart) kullanıcı sormadan siliniyordu. Artık hiçbir satır kendiliğinden
        // silinmez; 0 ₺'lik satır Kaydet'i KİLİTLEMEZ ve gövdeye GİRMEZ (t10'un asıl amacı: kullanıcı elle silmek zorunda değil).
        var e = KarmaFis();
        Assert.Equal("KART", SeciciYardim.SeciliDeger(e, "islem-bacak-1-arac"));   // ilk satırla aynı araç ("Nakit · 0") DEĞİL
        e.Find("#islem-bacak-1-tutar").Input("10000");
        e.WaitForAssertion(() => Assert.Equal("0", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")), Bekle);
        Assert.Equal(2, e.FindAll("#islem-bacaklar .bacak-satiri").Count);   // SİLİNMEDİ
        Assert.Equal("NAKIT", SeciciYardim.SeciliDeger(e, "islem-bacak-0-arac"));
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")));
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis-fisi")), Bekle);
        var odemeler = JsonDocument.Parse(SonPost("/islemler/satis-fisi").Govde).RootElement.GetProperty("odemeler");
        Assert.Equal(1, odemeler.GetArrayLength());   // 0 ₺'lik satır gönderilmez
        Assert.Equal("KART", odemeler[0].GetProperty("arac").GetString());
    }

    [Fact]
    public void GOCC2_karma_ikinci_satira_RAKAM_RAKAM_toplamdan_buyuk_tutar_satir_SILINMEZ_Kaydet_KILITLI_sebep_kalani_ata_ile_kaydeder()
    {
        // PAKET GOC-C-2 VERI-1 (tasarım hakemi, VERİ): BASİT 1280'de 99999 rakam rakam yazılınca ilk satır sessizce siliniyordu.
        var e = KarmaFis();
        foreach (var metin in new[] { "9", "99", "999", "9999", "99999" })
        {
            e.Find("#islem-bacak-1-tutar").Input(metin);
            e.WaitForAssertion(() => Assert.Equal(2, e.FindAll("#islem-bacaklar .bacak-satiri").Count), Bekle);
        }
        e.WaitForAssertion(() =>
        {
            Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
            Assert.Equal(string.Format(S3.FarkFazla, Bicim.Tl(89_999m)), GA.Kaydet(e).GetAttribute("title"));
            Assert.Equal(string.Format(Kuyumcu.Core.I18n.GocC2Sozlugu.Tr.OdemeAsimi, Bicim.Tl(89_999m)), e.Find("#islem-bacak-1-asim").TextContent.Trim());
        }, Bekle);
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis-fisi"));
        e.Find("#islem-bacak-kalan-ata").Click();
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        Assert.Equal(2, e.FindAll("#islem-bacaklar .bacak-satiri").Count);
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis-fisi")), Bekle);
        var odemeler = JsonDocument.Parse(SonPost("/islemler/satis-fisi").Govde).RootElement.GetProperty("odemeler");
        Assert.Equal(1, odemeler.GetArrayLength());
        Assert.Equal("KART", odemeler[0].GetProperty("arac").GetString());
        Assert.Equal(10_000m, odemeler[0].GetProperty("tutarTl").GetDecimal());
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T10_Pesin_Musteri_sunucu_haddinde_KAYDET_KILITLI_sebep_tek_kisa_cumle_istek_YOK(string mod)
    {
        tevsikJson = TevsikJson(5000m);   // fiş 10.000 ≥ sunucu haddi 5.000
        var e = TekKalemli(mod);
        GA.NakitSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        e.WaitForAssertion(() => Assert.True(GA.Kaydet(e).HasAttribute("disabled")), Bekle);
        Assert.Equal(S3.PesinVeNakitAl, GA.Kaydet(e).GetAttribute("title"));
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis"));
    }

    [Fact]
    public void T10_gercek_musteride_NAKIT_haddi_asarsa_KILITLI_sebep_haddi_soyler_karta_gecince_ACILIR()
    {
        tevsikJson = TevsikJson(5000m);
        var e = TekKalemli(ArayuzModuServisi.Basit);
        GA.MusteriSec(e, "c1");
        GA.NakitSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        e.WaitForAssertion(() => Assert.Equal(string.Format(S3.NakitHaddiKilit, Bicim.Tl(5000m)), GA.Kaydet(e).GetAttribute("title")), Bekle);
        GA.OdemeSec(e, "KART");
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
    }

    [Fact]
    public void T10_sunucu_haddi_BILINMIYORSA_istemci_kilitlemez_son_soz_sunucuda()
    {
        tevsikJson = TevsikJson(0m);   // had gelmedi → sayı gömülmez, kilit yok
        var e = TekKalemli(ArayuzModuServisi.Basit);
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
    }

    [Fact]
    public void T10_Hurda_had_ustu_NAKITTE_kilit_sebebi_haddi_soyler()
    {
        var e = Hurda(ArayuzModuServisi.Basit);
        GA.MusteriSec(e, "c1");
        e.Find("#hurda-gram").Input("10");   // has × kur ≈ 62.000 ≥ 50.000
        SeciciYardim.Sec(e, "hurda-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#hurda-satici-ad");
        e.Find("#hurda-satici-ad").Input("Ali Veli");
        e.Find("#hurda-satici-tckn").Input("10000000146");
        e.Find("#hurda-satici-adres").Input("Kapalıçarşı No:1");
        GA.NakitSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        e.WaitForAssertion(() => Assert.Equal(string.Format(S3.NakitHaddiKilit, Bicim.Tl(50000m)), GA.Kaydet(e).GetAttribute("title")), Bekle);
        Assert.NotEmpty(e.FindAll("#islem-odeme"));   // ödeme türü seçicisi BASİT'te de açıldı ("Nakit — sistem belirledi" değil)
        Assert.Equal(S3.SaticiTipiVatandas, SeciciYardim.Secenekler(e, "hurda-satici-tip").First(o => o.Deger() == "NIHAI_TUKETICI").TextContent.Trim());
    }

    private const string TevsikReddi = "Tevsik zorunluluğu: ödemenin BİR KISMININ (40.000,00 TL) nakit yapılması… (459 s. VUK Genel Tebliği §4.1.2)";

    [Fact]
    public void T10_BASIT_sunucu_reddi_KISA_cumle_ham_metin_BILGI_icinde_PRO_aynen_TL_yerine_simge()
    {
        postRed = ("/islemler/satis", $$"""{"message":"{{TevsikReddi}}","statusCode":400}""");
        var b = TekKalemli(ArayuzModuServisi.Basit);
        GA.Kaydet(b).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(b);
        // TEMA TEMİZLİĞİ 17.10: tezgâhta sunucu hatası Fark bloğunun hata satırında (#yildiz-tezgah-hata; özgün #hata-bandi
        // display:none); ham dayanak satırın ipucunda (title = sayfanın SunucuAyrinti'si) — yutulmaz.
        b.WaitForAssertion(() => Assert.Equal(string.Format(S3.NakitHaddiKilit, Bicim.Tl(50000m)), b.Find("#yildiz-tezgah-hata").TextContent.Trim()), Bekle);
        Assert.DoesNotContain("VUK", b.Find("#yildiz-tezgah-hata").TextContent);
        Assert.Contains(TevsikReddi, b.Find("#yildiz-tezgah-hata").GetAttribute("title"));
    }

    [Fact]
    public void T10_PRO_sunucu_reddi_aynen_TL_tutari_simgeyle()
    {
        postRed = ("/islemler/satis", $$"""{"message":"{{TevsikReddi}}","statusCode":400}""");
        var p = TekKalemli(ArayuzModuServisi.Profesyonel);
        GA.Kaydet(p).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(p);
        p.WaitForAssertion(() => Assert.Contains("VUK", p.Find("#yildiz-tezgah-hata").TextContent), Bekle);   // TEMA TEMİZLİĞİ: tezgâhın hata satırı
        Assert.Contains(Bicim.Tl(40000m), p.Find("#yildiz-tezgah-hata").TextContent);
        Assert.DoesNotContain("40.000,00 TL", p.Find("#yildiz-tezgah-hata").TextContent);
        Assert.True(string.IsNullOrEmpty(p.Find("#yildiz-tezgah-hata").GetAttribute("title")));   // PRO: ayrı ham dayanak yok
        Assert.Empty(p.FindAll("#hata-ayrinti"));
    }

    [Fact]
    public void T10_onceki_musterinin_sunucu_reddi_MUSTERI_DEGISINCE_silinir()
    {
        postRed = ("/islemler/satis", """{"message":"Stok yetersiz.","statusCode":400}""");
        var e = TekKalemli(ArayuzModuServisi.Basit);
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains("Stok yetersiz.", e.Find("#yildiz-tezgah-hata").TextContent), Bekle);   // TEMA TEMİZLİĞİ: tezgâhın hata satırı
        GA.MusteriSec(e, "c1");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#yildiz-tezgah-hata, #hata-bandi")), Bekle);
    }

    [Fact]
    public void T10_Iade_MASAK_reddi_MASAK_alanlarini_ACAR_ham_anahtar_BASILMAZ()
    {
        postRed = ("/islemler/satis-iade", """{"message":"MASAK: kimlik tespiti zorunludur · MASAK beyan açıklaması (masak.aciklama) en az 20 karakter","alanlar":{"masak.aciklama":["en az 20 karakter"]},"statusCode":400}""");
        var e = Ekran(ArayuzModuServisi.Basit, "MUDUR");
        e.Find("#islem-tur-SATIS_IADE").Click();
        e.WaitForElement("#iade-kaynak", Bekle);
        SeciciYardim.Sec(e, "iade-kaynak", "h1");
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        Assert.Empty(e.FindAll(".kart--masak"));
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kart--masak")), Bekle);   // alanlar GERÇEKTEN açıldı
        Assert.Equal(S3.SunucuKimlikGerekli, e.Find("#hata-bandi").TextContent.Trim());
        Assert.DoesNotContain("masak.aciklama", e.Find("#hata-bandi").TextContent);
    }

    [Fact]
    public void T10_PRO_MASAK_reddi_ham_anahtar_BASILMAZ_alanlar_ACILIR()
    {
        postRed = ("/islemler/satis-iade", """{"message":"MASAK: 185.000 TL ve üstü işlemde kimlik zorunludur · açıklama (masak.aciklama) en az 20 karakter","alanlar":{"masak.aciklama":["x"]},"statusCode":400}""");
        var e = Ekran(ArayuzModuServisi.Profesyonel, "MUDUR");
        e.Find("#islem-tur-SATIS_IADE").Click();
        e.WaitForElement("#iade-kaynak", Bekle);
        SeciciYardim.Sec(e, "iade-kaynak", "h1");
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kart--masak")), Bekle);
        Assert.DoesNotContain("masak.aciklama", e.Find("#hata-bandi").TextContent);
        Assert.Contains(Bicim.Tl(185000m), e.Find("#hata-bandi").TextContent);
        Assert.Contains(istekler, i => i.Yol.Contains("/masak/gereksinim") && i.Yol.Contains("200000"));   // kümülatif red → en az kimlik eşiği sorulur
    }

    // ═══ t11 — tasarım hakeminin 4 YÜKSEK bulgusu (29.09) ═══════════════════════════════════════

    [Fact]
    public void T11_nakit_notu_kuralla_AYNI_cumle_bant_varken_GIZLI_karta_gecince_de_GIZLI()
    {
        // Hakem: sağ not "program uyarır; kalanı kartla alırsın" derken bant "nakit alınamaz" diyor ve Kaydet kilitli — çelişki.
        // Sunucu: bu fişte işlem-tutarı kuralı devrede (hakemin senaryosu — kısmi nakit de kilitli); fiş 10.000 haddin (12.000) %80'ini geçti.
        tevsikJson = """{"had":12000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":12000,"islemTutariKuraliDevrede":true}""";
        var e = Ekran(ArayuzModuServisi.Basit);
        Yaz(e, "bil");
        e.WaitForElement("#islem-barkod-liste [role=option]", Bekle);
        e.Find("#islem-barkod").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        e.Find("#islem-barkod").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        GA.NakitSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        // TEMA TEMİZLİĞİ 17.10: tezgâhta kural cümlesi Fark bloğunun not satırında (özgün bant ve nakit kutusu display:none) — TEK kez.
        e.WaitForAssertion(() => Assert.Contains(S3.NakitAlinamaz, e.Find("#yildiz-tezgah-not").TextContent), Bekle);
        Assert.True(e.Find("#islem-nakit-uyari").HasAttribute("hidden"));   // ekranda TEK bant
        Assert.Equal(1, Kac(GA.TumMetin(e), S3.NakitAlinamaz));
        GA.OdemeSec(e, "KART");
        // t15 (tasarım hakemi YÜKSEK "nakit yokken de kalıcı"): ödemede nakit yoksa not çıkmaz; metni DOM'da aynı kalır.
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);   // kart işlendi
        Assert.True(e.Find("#islem-nakit-uyari").HasAttribute("hidden"));
        Assert.Equal(string.Format(SatisTuvalSozlugu.Tr.NakitUyari, Bicim.Tl(12000m)), e.Find("#islem-nakit-uyari").TextContent.Trim());
        foreach (var m in new[] { SatisTuvalSozlugu.Tr.NakitUyari, SatisTuvalSozlugu.Tr.NakitUyariSayisiz, S3.NakitAlinamaz })
        {
            Assert.DoesNotContain("program uyarır", m);
            Assert.Contains("tamamını kart ya da havaleyle", m);   // not ve bant aynı kuralı aynı sözle söyler
        }
    }

    [Fact]
    public void T11_TAKSIT_baglantisi_satirin_ALTINDA_kendi_satirinda_sil_satirin_son_ogesi()
    {
        posCihazlari = TekPos;
        var e = KartBacagi();
        var ac = e.Find("#islem-bacak-0-taksit-ac");
        Assert.Contains("bacak-ek-satiri", ac.ParentElement!.ClassName);
        Assert.Null(ac.Closest(".bacak-satiri"));   // satırın içinde değil → alanlar ve ✕ aynı hizada kalır (K7)
        Assert.Contains("yz-b2", ac.ClassName);   // TEMA TEMİZLİĞİ 17.10: tezgâhta ikincil düğme tuvalin .b2 kalıbı (özgün btn-ikincil)
        var satir = e.Find("#islem-bacak-sil-0").ParentElement!;
        Assert.Contains("bacak-satiri", satir.ClassName);
        Assert.Equal("islem-bacak-sil-0", satir.LastElementChild!.Id);
        ac.Click();
        e.WaitForElement("#islem-bacak-0-taksit");
        Assert.Empty(e.FindAll("#islem-bacak-0-taksit-ac, .bacak-ek-satiri"));
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T11_ODEME_karosu_ve_sekmesi_YOK_hicbir_tur_dugmesi_hep_pasif_degil(string mod)
    {
        var e = Ekran(mod);
        Assert.Empty(e.FindAll("#islem-git-odeme, #islem-git-odeme-kilit"));
        var turDugmeleri = e.FindAll(".tuval-karo-izgara > .ekran-karo, .tuval-pro-sekmeler > *, #yildiz-tur-secici > *");
        Assert.NotEmpty(turDugmeleri);
        Assert.NotEmpty(e.FindAll("#yildiz-tur-secici > *"));   // TEMA TEMİZLİĞİ: tezgâhın GÖRÜNÜR tür sekmeleri de sayılır
        Assert.All(turDugmeleri.Where(d => d.Id != "islem-indirim"), d => Assert.False(d.HasAttribute("disabled"), d.Id));
    }

    [Fact]
    public void T11_Iade_nakit_bandi_GERI_ODEME_yonunde_musteri_kaynaktan_kalan_ve_tutar_alan_kabugunda()
    {
        tevsikJson = TevsikJson(5000m);   // iade tutarı 12.400 ≥ sunucu haddi
        var e = Ekran(ArayuzModuServisi.Basit);
        e.Find("#islem-tur-SATIS_IADE").Click();
        e.WaitForElement("#iade-kaynak");
        SeciciYardim.Sec(e, "iade-kaynak", "h1");
        e.WaitForElement("#iade-adet");
        GA.NakitSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        e.WaitForAssertion(() => Assert.Contains(S3.NakitIadeEdilemez, e.Find("#islem-yasal-kapi").TextContent), Bekle);
        Assert.DoesNotContain(S3.NakitAlinamaz, e.Markup);   // "alın" yönü iadede yok
        Assert.Empty(e.FindAll("#islem-cari"));
        Assert.Contains("Anadolu Holding A.Ş.", e.Find("#islem-iade-musteri").TextContent);
        Assert.Contains(S3.IadeMusteriKaynak, e.Find("#islem-iade-musteri-sar").TextContent);
        // "Kalan iade hakkı" ve "Orantılı tutar" Adet kutusuyla AYNI alan kabuğunda (etiket üstte, değer altında)
        foreach (var id in new[] { "iade-kalan", "iade-orantili", "iade-adet" })
            Assert.NotNull(e.Find($"#{id}-sar"));
        Assert.Equal("1", e.Find("#iade-kalan").GetAttribute("data-deger"));
        Assert.Equal("12400.00", e.Find("#iade-orantili").GetAttribute("data-deger"));
    }

    // SATIS-3 t14 (doğrulayıcı KRİTİK): PRO kalem kartı overflow:hidden ile bağlı ürün listesini kırpıyordu (8 öneriden 4'ü,
    // "elle" satırı erişilemez). Kartın SON geçerli overflow bildirimi visible olmalı; köşe yuvarlatması iç öğelerde.
    // (Görünürlük canlıda elementFromPoint ile ölçüldü — rapor § 26; bu test kuralın geri dönmesini yakalar.)
    [Fact]
    public void T14_PRO_kalem_karti_arama_listesini_KIRPMAZ()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        var css = System.Text.RegularExpressions.Regex.Replace(
            File.ReadAllText(Path.Combine(d!.FullName, "Kuyumcu.Web", "wwwroot", "css", "tuval-satis.css")),
            @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        var son = System.Text.RegularExpressions.Regex.Matches(css, @"(?m)^\.tuval-pro \.fis-pro-kart \{([^}]*)\}")
            .Select(m => m.Groups[1].Value).Last(g => g.Contains("overflow"));
        Assert.Matches(@"overflow:\s*visible", son);
        Assert.Contains(".tuval-pro .fis-pro-kart > .fis-pro-toplam:last-child { border-radius: 0 0 11px 11px; }", css);
        Assert.Contains(".tuval-pro .fis-pro-kart > .fis-pro-giris:first-child { border-radius: 11px 11px 0 0; }", css);
    }
    // ═══ t15 — TASARIM HAKEMİ KRİTİK (29.09): Takas/Değişim farkı nakit haddini aşınca ödeme türü sorulur ═══════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> TakasDolu(string mod, string eskiFiyat)
    {
        var e = Ekran(mod);
        e.Find("#islem-tur-TAKAS").Click();
        e.WaitForElement("#takas-eski-gram");
        Yaz(e, "bil");
        e.WaitForElement("#islem-barkod-oneri-0", Bekle);
        e.WaitForAssertion(() => e.Find("#islem-barkod-oneri-0").MouseDown(), Bekle);
        e.WaitForElement("#takas-yeni-fiyat", Bekle);
        GA.MusteriSec(e, "c1");
        ParaDemoEkranYardim.TakasSaticiDoldur(e);   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
        e.Find("#takas-eski-gram").Input("1");
        e.Find("#takas-eski-fiyat").Input(eskiFiyat);
        return e;
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T15_Takas_farki_HADDI_asinca_odeme_turu_sorulur_nakitte_KILITLI_kartla_govdeye_girer(string mod)
    {
        tevsikJson = TevsikJson(5000m);   // fark ≈ 10.000 − (1 g × 100) > sunucu haddi 5.000
        var e = TakasDolu(mod, "3500");   // ISLEM-BANT-2: "100" mutlak dışıydı (Kaydet artık kilitli); fark 10.000 − 0,916×3.500 > had 5.000 korunur
        e.WaitForElement("#islem-odeme", Bekle);   // olmayan alana yönlendirilmez: seçici EKRANDA
        GA.NakitSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        e.WaitForAssertion(() => Assert.True(GA.Kaydet(e).HasAttribute("disabled")), Bekle);
        Assert.Equal(S3.NakitAlinamaz, GA.Kaydet(e).GetAttribute("title"));   // bantla AYNI cümle (tek metin)
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/takas"));
        GA.OdemeSec(e, "KART");
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        Assert.NotEmpty(e.FindAll("#islem-odeme"));   // kart seçilince BASİT'te "Nakit" diye gizlenmez
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Equal("KART",
            JsonDocument.Parse(SonPost("/islemler/takas").Govde).RootElement.GetProperty("odemeTuru").GetString()), Bekle);
    }

    [Fact]
    public void T15_BASIT_Takas_farki_HAD_altinda_odeme_sorulmaz_govdede_NAKIT()
    {
        tevsikJson = TevsikJson(5000m);
        var e = TakasDolu(ArayuzModuServisi.Basit, "7000");   // fark ≈ 10.000 − 1 g × 22 ayar × 7.000 → hadden küçük (ISLEM-BANT-2: 9.000 oran dışıydı)
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        Assert.Empty(e.FindAll("#islem-odeme"));      // zorunlu önce: gerek yokken sorulmaz
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Equal("NAKIT",
            JsonDocument.Parse(SonPost("/islemler/takas").Govde).RootElement.GetProperty("odemeTuru").GetString()), Bekle);
    }
    [Fact]
    public void T15_PRO_yasal_kapi_gercek_musteride_KARTLA_odenen_fiste_ASILDI_demez_nakitte_der()
    {
        // Hakem YÜKSEK: ödemede nakit yokken kart "Aşıldı" (kırmızı) gösteriyordu; sunucu nakit 0'da kapıyı işletmez.
        tevsikJson = """{"had":5000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":5000,"islemTutariKuraliDevrede":true}""";
        var e = TekKalemli(ArayuzModuServisi.Profesyonel);
        GA.MusteriSec(e, "c1");
        GA.NakitSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        // TEMA TEMİZLİĞİ 17.10: eski PRO yan sütunun "Aşıldı/Uygun" hücresi satışta çizilmez — tezgâhta yasal durum not satırıdır:
        // nakitte engel (kötü, haddi söyler), kartla ödenen fişte engel YOK ("Yasal kurallar uygun").
        e.WaitForAssertion(() =>
        {
            Assert.Equal(string.Format(S3.NakitHaddiKilit, Bicim.Tl(5000m)), e.Find("#yildiz-tezgah-not").TextContent.Trim());
            Assert.Contains("yz-kotu", e.Find("#yildiz-tezgah-not").ClassName);
        }, Bekle);
        GA.OdemeSec(e, "KART");
        e.WaitForAssertion(() =>
        {
            Assert.Equal(YildizTezgahSozlugu.YasalUygun("tr"), e.Find("#yildiz-tezgah-not").TextContent.Trim());
            Assert.DoesNotContain("yz-kotu", e.Find("#yildiz-tezgah-not").ClassName);
        }, Bekle);
        Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title"));
    }

    [Fact]
    public void T15_nakit_notu_ODEMEDE_NAKIT_yokken_gizli_nakitte_gorunur()
    {
        var yok = Render<FisNakitUyari>(p => p.Add(x => x.Had, 5000m).Add(x => x.Toplam, 4900m).Add(x => x.NakitVar, false));
        Assert.True(yok.Find("#islem-nakit-uyari").HasAttribute("hidden"));
        var var_ = Render<FisNakitUyari>(p => p.Add(x => x.Had, 5000m).Add(x => x.Toplam, 4900m).Add(x => x.NakitVar, true));
        Assert.False(var_.Find("#islem-nakit-uyari").HasAttribute("hidden"));
    }
    // SATIS-3 t15 (tasarım hakemi YÜKSEK): hizalar canlıda ölçüldü (rapor § 28); bu test kuralların geri dönmesini yakalar.
    [Fact]
    public void T15_css_MASAK_uyruk_rayda_rozet_kutuya_binmez_PRO_birincil_sagda()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        var css = System.Text.RegularExpressions.Regex.Replace(
            File.ReadAllText(Path.Combine(d!.FullName, "Kuyumcu.Web", "wwwroot", "css", "tuval-satis.css")),
            @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.Contains(".islem-sayfa #masak-uyruk-alan > .secici-alan { grid-row: 1 / span 3; grid-column: 1; display: grid; grid-template-rows: subgrid; row-gap: inherit; }", css);
        Assert.Contains(".islem-sayfa :is(#altin-fiyat-sar, #hurda-fiyat-sar, #takas-eski-fiyat-sar) { padding-bottom: 24px; }", css);
        Assert.Contains(".islem-sayfa.tuval-pro .islem-alt-satir { justify-content: flex-end; }", css);
    }

    // ══ SATIS-3 t16 — doğrulayıcı KRİTİK (29.09): belirsiz "1.500" hata gösterirken Kaydet açık kalıyor, kur fiyatı
    //    alanın üzerine yazılıp kullanıcının GÖRMEDİĞİ fiyat POST ediliyordu. Kök: SayisalAlan geçersizde DegerChanged
    //    ateşlemiyordu → FiyatElle=false → KurYansit üzerine yazıyordu. ══
    private static void KurYansit(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) =>
        e.InvokeAsync(() => typeof(Kuyumcu.Web.Pages.Islemler)
            .GetMethod("KurYansit", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(e.Instance, null));

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T16_Takas_belirsiz_fiyat_Kaydet_KILITLI_sebep_alani_soyler_kur_UZERINE_YAZMAZ(string mod)
    {
        var e = TakasDolu(mod, "7000");   // ISLEM-BANT-2: bant içi (9.000 / 1.500 oran dışıydı → Kaydet artık kilitli)
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        e.Find("#takas-eski-fiyat").Input("4.500");
        e.WaitForAssertion(() => Assert.True(GA.Kaydet(e).HasAttribute("disabled")), Bekle);
        Assert.Equal(S3.FiyatAlaniDuzeltin, GA.Kaydet(e).GetAttribute("title"));
        KurYansit(e);   // canlı kur itmesi (6 sn sonra gelen) — alanın üzerine yazmamalı
        e.WaitForAssertion(() => Assert.Equal("4.500", e.Find("#takas-eski-fiyat").GetAttribute("value")), Bekle);
        Assert.Equal("true", e.Find("#takas-eski-fiyat").GetAttribute("aria-invalid"));
        Assert.Contains(SayiGirisiSozlugu.Tr.Belirsiz, e.Find("#takas-eski-fiyat-sar").TextContent);
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/takas"));
        // kullanıcı düzeltir → açılır, gövdeye YAZDIĞI değer girer
        e.Find("#takas-eski-fiyat").Input("4500");
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Equal(4500m,
            JsonDocument.Parse(SonPost("/islemler/takas").Govde).RootElement.GetProperty("eskiUrunBirimFiyatTlHasGram").GetDecimal()), Bekle);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T16_Hurda_belirsiz_fiyat_Kaydet_KILITLI_sebep_alani_soyler_kur_UZERINE_YAZMAZ(string mod)
    {
        var e = Hurda(mod);
        GA.MusteriSec(e, "c1");
        e.Find("#hurda-gram").Input("2");
        e.WaitForAssertion(() => Assert.Contains("Kurdan geldi", e.Find("#hurda-fiyat-kaynak").TextContent), Bekle);
        e.Find("#hurda-fiyat").Input("1.500");
        e.WaitForAssertion(() => Assert.Equal(S3.FiyatAlaniDuzeltin, GA.Kaydet(e).GetAttribute("title")), Bekle);
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
        Assert.Contains("Elle girildi", e.Find("#hurda-fiyat-kaynak").TextContent);   // ilk tuşta elle — kur artık yazmaz
        KurYansit(e);
        e.WaitForAssertion(() => Assert.Equal("1.500", e.Find("#hurda-fiyat").GetAttribute("value")), Bekle);
        Assert.Equal("true", e.Find("#hurda-fiyat").GetAttribute("aria-invalid"));
        Assert.Contains(SayiGirisiSozlugu.Tr.Belirsiz, e.Find("#hurda-fiyat-sar").TextContent);
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/hurda"));
    }

    // t16 (doğrulayıcı DÜŞÜK): BASİT'te üst bant kilit sebebiyle AYNI cümleyi söylüyorsa Kaydet altındaki satır görsel gizli
    // (metin DOM'da/title'da kalır); PRO'da bant yok → satır görünür.
    [Theory]
    [InlineData(ArayuzModuServisi.Basit, true)]
    [InlineData(ArayuzModuServisi.Profesyonel, false)]
    public void T16_kilit_sebebi_bantla_ayniysa_BASITte_tek_kez_gorunur(string mod, bool gizli)
    {
        tevsikJson = TevsikJson(5000m);
        var e = TakasDolu(mod, "3500");   // ISLEM-BANT-2: "100" mutlak dışıydı (Kaydet artık kilitli); fark 10.000 − 0,916×3.500 > had 5.000 korunur
        GA.NakitSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        e.WaitForAssertion(() => Assert.True(GA.Kaydet(e).HasAttribute("disabled")), Bekle);
        var kilit = e.Find("#islem-kaydet-kilit");
        // YASAL-KAPI-3 tur 2 (hakem: görünmez kopya da tekrar sayılır): bant söylüyorsa kilit satırı METİNSİZ — sebep düğmenin title'ında
        Assert.Equal(gizli ? "" : S3.NakitAlinamaz, kilit.TextContent.Trim());
        Assert.Equal(S3.NakitAlinamaz, GA.Kaydet(e).GetAttribute("title"));
        Assert.Equal(gizli, kilit.ClassList.Contains("gorunmez"));
        if (gizli) Assert.Contains(S3.NakitAlinamaz, e.Markup.Replace(kilit.OuterHtml, ""));   // bant cümleyi söylüyor
    }

    // t16 (doğrulayıcı DÜŞÜK): takas farkı KARTLA — çok POS'ta cihaz sorulur (sunucu fail-closed), tek POS'ta sorulmaz.
    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T16_Takas_KART_cok_POSta_cihaz_secilmeden_KILITLI_secince_govdede(string mod)
    {
        posCihazlari = "[" + TekPos.Trim('[', ']') + ",{\"id\":\"pc2\",\"ad\":\"Garanti POS\",\"bankaAdi\":\"Garanti\",\"subeId\":\"s1\",\"kasaId\":\"ks1\",\"pesinKomisyonOran\":1.5,\"valorGun\":1,\"paraBirimi\":\"TRY\",\"aktif\":true,\"bekleyenAlacak\":0,\"bugunYatacakNet\":0,\"tahsilatVar\":false}]";
        tevsikJson = TevsikJson(5000m);
        var e = TakasDolu(mod, "3500");   // ISLEM-BANT-2: "100" mutlak dışıydı (Kaydet artık kilitli); fark 10.000 − 0,916×3.500 > had 5.000 korunur
        e.WaitForElement("#islem-odeme", Bekle);
        GA.NakitSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar
        Assert.Empty(e.FindAll("#islem-pos"));   // nakitte cihaz sorulmaz
        GA.OdemeSec(e, "KART");
        e.WaitForElement("#islem-pos", Bekle);
        e.WaitForAssertion(() => Assert.Equal(S3.PosCihaziSecin, GA.Kaydet(e).GetAttribute("title")), Bekle);
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
        SeciciYardim.Sec(e, "islem-pos", "pc2");
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Equal("pc2",
            JsonDocument.Parse(SonPost("/islemler/takas").Govde).RootElement.GetProperty("posCihazId").GetString()), Bekle);
    }

    [Fact]
    public void T16_Takas_KART_tek_POSta_secici_YOK_govdede_cihaz_null()
    {
        posCihazlari = TekPos;
        tevsikJson = TevsikJson(5000m);
        var e = TakasDolu(ArayuzModuServisi.Profesyonel, "3500");   // ISLEM-BANT-2: bant içi (3.400–7.480)
        e.WaitForElement("#islem-odeme", Bekle);
        GA.OdemeSec(e, "KART");
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        Assert.Empty(e.FindAll("#islem-pos"));
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Equal(JsonValueKind.Null,
            JsonDocument.Parse(SonPost("/islemler/takas").Govde).RootElement.GetProperty("posCihazId").ValueKind), Bekle);
    }

    // t16 (doğrulayıcı DÜŞÜK K10): PRO üst satırda müşteri adı kutu İÇİNDE sarar (kontrol ölçüsü değişmeden) — canlı ölçüm rapor § 29.
    [Fact]
    public void T16_css_PRO_musteri_adi_kutuda_sarar_kesilmez()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        var css = System.Text.RegularExpressions.Regex.Replace(
            File.ReadAllText(Path.Combine(d!.FullName, "Kuyumcu.Web", "wwwroot", "css", "tuval-satis.css")),
            @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.Contains(".tuval-pro #islem-ust-satir > .alan:first-child .secici-metin { white-space: normal; overflow-wrap: anywhere; text-overflow: clip; line-height: 1.15; }", css);
        Assert.Contains(".islem-sayfa .form-grid-2 > .alan > #islem-pos-sar { row-gap: 4px; }", css);   // POS seçicisi Ödeme Türü ile aynı hiza (K7)
    }

    // ══ SATIS-3 t17 — orkestratör 29.09 12:10 + hasım doğrulayıcı KRİTİK ×2: "EKRANDA GÖRÜNEN ≠ GÖNDERİLEN" HATA SINIFI.
    //    Kural: sayfadaki HER Sayi/Para/Gram kutusu anlaşılamayan/belirsiz metin taşırken (tuş tuş yazımın ara değerleri
    //    dahil) (a) alan hatası görünür, (b) Kaydet PASİF ve sebep O ALANI söyler, (c) gövdeye eski/teklif/kur/ara değer
    //    GİTMEZ, (d) kur/teklif/otomatik kalan yansıması kullanıcının yazdığının üstüne YAZMAZ. Tek kapı: GecersizAlanKapisi. ══
    private static readonly string[] TusTus1500 = ["1", "1.", "1.5", "1.50", "1.500"];

    private static void TusTus(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string id, IEnumerable<string> adimlar)
    {
        foreach (var q in adimlar) e.Find($"#{id}").Input(q);
    }

    private Sozluk T => Services.GetRequiredService<DilServisi>().T;

    private int PostSayisi() { lock (istekler) return istekler.Count(i => i.Metot == "POST" && i.Yol.Contains("/islemler/")); }

    /// <summary>Kutunun alan altı hata metni (kimlik aria-describedby'dan: "-hata" ya da defter "alan-hata-").</summary>
    private static string HataMetni(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string id) =>
        e.Find("#" + e.Find($"#{id}").GetAttribute("aria-describedby")!.Split(' ').First(x => x.Contains("hata"))).TextContent;

    /// <summary>Kutu belirsiz metinle kırmızı, Kaydet pasif + sebep alanın etiketi, tıklama POST atmaz, kutu metni korunur.</summary>
    private void KilitliVeSessizDegil(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string id, string etiket, string sebep, bool tamSayi = false)
    {
        e.WaitForAssertion(() => Assert.Equal("true", e.Find($"#{id}").GetAttribute("aria-invalid")), Bekle);
        e.WaitForAssertion(() => Assert.True(GA.Kaydet(e).HasAttribute("disabled")), Bekle);
        Assert.Equal(sebep, GA.Kaydet(e).GetAttribute("title"));
        Assert.Contains(etiket, sebep);
        Assert.Equal("1.500", e.Find($"#{id}").GetAttribute("value"));
        Assert.Contains(tamSayi ? SayiGirisiSozlugu.Tr.BelirsizTam : SayiGirisiSozlugu.Tr.Belirsiz, HataMetni(e, id));   // TOPLU-7A: tam sayı alanı "1,5" önermez
        var once = PostSayisi();
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        Thread.Sleep(200);
        Assert.Equal(once, PostSayisi());
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T17_Takas_YENI_fiyat_tus_tus_1_500_teklif_POST_EDILMEZ_1_5_yazinca_1_5_gider(string mod)
    {
        var e = TakasDolu(mod, "5000");
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        TusTus(e, "takas-yeni-fiyat", TusTus1500);
        var fe = mod == ArayuzModuServisi.Basit ? DegisimGunSonuSozlugu.Tr.YeniFiyat : T.Islem.BirimFiyat;   // DEGISIM-GUNSONU: BASİT tuval etiketi sade
        KilitliVeSessizDegil(e, "takas-yeni-fiyat", fe, string.Format(S3.AlanDuzeltin, fe));
        KurYansit(e);   // canlı kur/teklif itmesi alanın üstüne yazmaz
        e.WaitForAssertion(() => Assert.Equal("1.500", e.Find("#takas-yeni-fiyat").GetAttribute("value")), Bekle);
        Assert.Empty(e.FindAll("#takas-yeni-fiyat-onizleme"));   // eski teklif "bilgi" diye altına yazılmaz
        TusTus(e, "takas-yeni-fiyat", ["1", "1,", "1,5"]);
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Equal(1.5m,
            JsonDocument.Parse(SonPost("/islemler/takas").Govde).RootElement.GetProperty("yeniUrunBirimFiyatTl").GetDecimal()), Bekle);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T17_Takas_ESKI_gram_tus_tus_1_500_ara_deger_1_5_POST_EDILMEZ_1_5_yazinca_1_5_gider(string mod)
    {
        var e = TakasDolu(mod, "5000");
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        TusTus(e, "takas-eski-gram", TusTus1500);
        var ge = mod == ArayuzModuServisi.Basit ? DegisimGunSonuSozlugu.Tr.EskiGram : T.Urun.GramAgirlik;   // DEGISIM-GUNSONU: BASİT tuval etiketi sade
        KilitliVeSessizDegil(e, "takas-eski-gram", ge, string.Format(S3.AlanDuzeltin, ge));
        TusTus(e, "takas-eski-gram", ["1", "1,", "1,5"]);
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Equal(1.5m,
            JsonDocument.Parse(SonPost("/islemler/takas").Govde).RootElement.GetProperty("eskiUrunGram").GetDecimal()), Bekle);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T17_Hurda_gram_tus_tus_1_500_POST_EDILMEZ(string mod)
    {
        var e = Hurda(mod);
        GA.MusteriSec(e, "c1");
        TusTus(e, "hurda-gram", TusTus1500);
        KilitliVeSessizDegil(e, "hurda-gram", T.Urun.GramAgirlik, string.Format(S3.AlanDuzeltin, T.Urun.GramAgirlik));
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/hurda"));
    }

    [Theory]
    [InlineData("islem-birimfiyat")]
    [InlineData("islem-adet")]
    public void T17_Satis_giris_satiri_tus_tus_1_500_kalem_EKLENMEZ_teklif_USTUNE_YAZMAZ_Kaydet_kilitli(string id)
    {
        var e = TekKalemli(ArayuzModuServisi.Profesyonel);   // bir kalem var → Kaydet açık
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))), Bekle);
        TusTus(e, id, TusTus1500);
        var etiket = id == "islem-adet" ? T.Ortak.Adet : T.Islem.BirimFiyat;
        KilitliVeSessizDegil(e, id, etiket, string.Format(S3.AlanDuzeltin, etiket), tamSayi: id == "islem-adet");
        e.Find("#islem-kalem-ekle").Click();
        Assert.Single(e.FindAll("#islem-kalem-tablo tbody tr, #islem-kalem-pro tr.fis-pro-satir, .yz-tezgah-kalem"));   // ikinci kalem EKLENMEDİ
        // TOPLU-7A: tam sayı alanında (adet) "1,5 ya da 1500" seçeneği sunulmaz → BelirsizTam; ondalıklı alanda (birim fiyat) eski metin.
        Assert.Contains(id == "islem-adet" ? SayiGirisiSozlugu.Tr.BelirsizTam : SayiGirisiSozlugu.Tr.Belirsiz, HataMetni(e, id));   // "sıfırdan büyük" ile ezilmedi
        Assert.Equal("1.500", e.Find($"#{id}").GetAttribute("value"));
    }

    [Fact]
    public void T17_Karma_odeme_tutari_tus_tus_1_500_otomatik_kalan_USTUNE_YAZMAZ_satir_silinince_kilit_kalkar()
    {
        var e = KarmaFis();
        TusTus(e, "islem-bacak-1-tutar", TusTus1500);
        KilitliVeSessizDegil(e, "islem-bacak-1-tutar", T.Ortak.Tutar, string.Format(S3.AlanDuzeltin, T.Ortak.Tutar));
        e.Find("#islem-bacak-1-tutar").Focus();
        e.Find("#islem-bacak-1-tutar").Input("");   // canlı bulgu: silinen kutuya "0" basılıyor, ardından "1,5" → "01,5" görünüyordu
        Assert.Equal("", e.Find("#islem-bacak-1-tutar").GetAttribute("value"));
        TusTus(e, "islem-bacak-1-tutar", TusTus1500);
        e.Find("#islem-bacak-0-tutar").Input("4000");   // öbür satır değişir → otomatik kalan yazılamaz
        e.WaitForAssertion(() => Assert.Equal("1.500", e.Find("#islem-bacak-1-tutar").GetAttribute("value")), Bekle);
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
        e.Find("#islem-bacak-sil-1").Click();   // alan DOM'dan kalkınca kapıdaki kaydı silinir (Dispose)
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-bacak-1-tutar")), Bekle);
        // TEMA TEMİZLİĞİ 17.10: tezgâhta "Çıkar"la tek satır kalınca sayfa tek ödemeye döner (fiş toplamı — elle "fişin tamamı"
        // yazmanın karşılığı); kalan tek ödeme → Kaydet AÇILIR (bayat kayıt kilitlemez).
        e.WaitForElement("#yildiz-odeme-0", Bekle);
        Assert.Empty(e.FindAll("#islem-bacak-0-tutar"));
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
    }

    [Fact]
    public void T17_Para_ustu_alinan_tus_tus_1_500_Kaydet_kilitli()
    {
        var e = KarmaFis();
        e.WaitForElement("#yildiz-tezgah-para-ustu", Bekle).Click();   // TEMA TEMİZLİĞİ: tezgâhın "+ Para üstü" hapı (FisParaUstu AYNI)
        e.WaitForElement("#islem-alinan");
        TusTus(e, "islem-alinan", TusTus1500);
        var etiket = FisYazdirSozlugu.Al("tr").Alinan;
        KilitliVeSessizDegil(e, "islem-alinan", etiket, string.Format(S3.AlanDuzeltin, etiket));
    }

    [Fact]
    public void T17_Iade_adedi_tus_tus_1_500_POST_EDILMEZ()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel, "MUDUR");
        e.Find("#islem-tur-SATIS_IADE").Click();
        e.WaitForElement("#iade-kaynak");
        SeciciYardim.Sec(e, "iade-kaynak", "h1");
        e.WaitForElement("#iade-adet");
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        TusTus(e, "iade-adet", TusTus1500);
        KilitliVeSessizDegil(e, "iade-adet", T.Islem.IadeAdedi, string.Format(S3.AlanDuzeltin, T.Islem.IadeAdedi));
    }

    /// <summary>Kapı yoksa (başka ekranlar) SayisalAlan'ın eski davranışı BİREBİR: null bildirilmez, çağıranın hatası önce.</summary>
    [Fact]
    public void T17_Kapisiz_formda_SayisalAlan_davranisi_DEGISMEZ()
    {
        decimal? deger = 7m; var cagri = 0;
        var c = Render<Kuyumcu.Web.Bilesenler.Form.Para>(p => p
            .Add(x => x.ElemanId, "k").Add(x => x.Etiket, "K").Add(x => x.Deger, deger)
            .Add(x => x.DegerChanged, (decimal? v) => { cagri++; deger = v; }));
        foreach (var q in TusTus1500) c.Find("#k").Input(q);
        Assert.Equal(1.5m, deger);   // kapısız: belirsiz metin çağırana bildirilmez (eski sözleşme; P78/EMANET1 bunu sabitliyor)
        Assert.Equal("true", c.Find("#k").GetAttribute("aria-invalid"));
    }

    /// <summary>t17 canlı görüntü: ödeme satırı "1.500" derken alt çubuk "Fark 0 ₺ ✓" diyordu (satır hesaba 0 giriyordu).</summary>
    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T17_Karma_odeme_tutari_belirsizken_FARK_tamam_DEMEZ(string mod)
    {
        var e = Ekran(mod);
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled")), Bekle);
        GA.KarmaAc(e);
        TusTus(e, "islem-bacak-1-tutar", TusTus1500);
        var beklenen = string.Format(S3.AlanDuzeltin, T.Ortak.Tutar);
        // TEMA TEMİZLİĞİ 17.10: iki modda da tezgâhın Fark bloğu (#yildiz-tezgah-fark) — belirsizken "—", onay işareti YOK,
        // sebep not satırında görünür; karmanın "Kalan" satırı da tamam demez.
        e.WaitForAssertion(() => Assert.Equal("—", e.Find("#yildiz-tezgah-fark-tutar").TextContent.Trim()), Bekle);
        Assert.Empty(e.FindAll("#yildiz-tezgah-fark .yz-tezgah-onay"));
        Assert.Equal(beklenen, e.Find("#yildiz-tezgah-not").TextContent.Trim());
        Assert.Equal(beklenen, e.Find("#islem-bacak-fark-metin").TextContent.Trim());
        Assert.Contains("metin-kirmizi", e.Find("#islem-bacak-fark-metin").ClassName);
        TusTus(e, "islem-bacak-1-tutar", ["0"]);   // düzeltince eski davranış: fark tamam
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yildiz-tezgah-fark .yz-tezgah-onay")), Bekle);
        Assert.Equal(S3.FarkTamam, e.Find("#islem-bacak-fark-metin").TextContent.Trim());
    }

    /// <summary>t17 canlı ölçüm (K7 ±25): uyarı taşıyan ödeme satırında kutular üstten hizalı kalır.</summary>
    [Fact]
    public void T17_css_gecersiz_odeme_satiri_kutulari_USTTEN_hizali()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        var css = System.Text.RegularExpressions.Regex.Replace(
            File.ReadAllText(Path.Combine(d!.FullName, "Kuyumcu.Web", "wwwroot", "css", "tuval-satis.css")),
            @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.Contains(".islem-sayfa :is(#islem-aldim, #islem-bacaklar) .bacak-satiri:has(.girdi[aria-invalid=\"true\"]),\n.islem-sayfa .fis-odeme-kart .bacak-satiri:has(.girdi[aria-invalid=\"true\"]) { align-items: start; }", css);
        // BASİT <1400: sil düğmesi etiket payı kadar aşağıda (kutu hizası)
        Assert.Contains(".islem-sayfa.tuval-tezgah .fis-odeme-kart .bacak-satiri:has(.girdi[aria-invalid=\"true\"]) > [id^=\"islem-bacak-sil-\"] { align-self: flex-start; margin-top: calc(var(--bacak-etiket-b) + .25rem); }", css);
        Assert.Contains(".islem-sayfa.tuval-tezgah .fis-odeme-kart .bacak-satiri:has(.girdi[aria-invalid=\"true\"]) > * > .alan-etiket { line-height: 1.4; min-height: var(--bacak-etiket-b); }", css);
    }
    /// <summary>t18 (doğrulayıcı YÜKSEK — PRO 1024/960 karma: "Kaydet ve yazdır" ayrı satıra düşüyordu). Alt eylem şeridi:
    /// içi boş özet sarmalayıcısı yer kaplamaz, ikinciller solda ve nötr, birincil her satırda en sağda, gerekçe hemen altında.
    /// Yerleşim gerçek tarayıcıda ölçüldü (rapor § 31); bu test sözleşmenin CSS'te durduğunu sabitler.</summary>
    [Fact]
    public void T18_css_PRO_alt_eylem_seridi_birincil_sagda_tek_satir()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        var css = System.Text.RegularExpressions.Regex.Replace(
            File.ReadAllText(Path.Combine(d!.FullName, "Kuyumcu.Web", "wwwroot", "css", "tuval-satis.css")),
            @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.Contains("html[data-mod] .islem-sayfa.tuval-pro .islem-alt-satir > .fis-pro-odeme-ozet-sar:not(:has(> :not([hidden]))) { display: none; }", css);
        Assert.Contains("html[data-mod] .islem-sayfa.tuval-pro .islem-alt-satir > #islem-kaydet { margin-left: auto; }", css);
        Assert.Contains("html[data-mod] .islem-sayfa.tuval-pro .islem-alt-satir { justify-content: flex-start; }", css);
        Assert.Contains("html[data-mod] .islem-sayfa.tuval-pro .fis-pro-odeme-hap > #islem-pro-tek { color: var(--metin);", css);
        Assert.Contains("html[data-mod] .islem-sayfa.tuval-pro .islem-alt-satir > .fis-pro-odeme-ozet-sar { flex: 1 1 0; min-width: 7.5rem; }", css);
    }

    // ═══ t19 — MASAK kapısı TEK KAYNAKTAN (orkestratör 15:40 + tasarım hakemi KRİTİK) ════════════════════════
    private void FiyatliKalem(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string urunId, string fiyat, int beklenen)
    {
        SeciciYardim.Sec(e, "islem-urun", urunId);
        e.WaitForAssertion(() => Assert.Equal(urunId, SeciciYardim.SeciliDeger(e, "islem-urun")));
        if (urunId == "u1") e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))), Bekle);
        e.Find("#islem-birimfiyat").Input(fiyat);
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.Equal(beklenen, e.FindAll("#islem-kalem-tablo tbody tr, #islem-kalem-pro tr.fis-pro-satir, .yz-tezgah-kalem").Count), Bekle);
    }

    [Fact]
    public void T19_iki_kalem_her_biri_esik_ALTI_toplam_USTU_MASAK_amac_ACILIR_Kaydet_KILITLI()
    {
        // Hakem: 2 kalem (her biri 185 bin altı) = 301 bin; ekran "Altında" dedi, Kaydet açıktı, sunucu reddetti,
        // amaç/açıklama hiç açılmadı. Sahte eşik 200.000: 150.000 + 150.000 = 300.000 → sunucuya TOPLAM sorulur.
        gereksinimOzel = v => v.Contains("tutar=300000") ? GereksinimAmac : null;
        var e = Ekran();
        GA.MusteriSec(e, "c1");
        FiyatliKalem(e, "u1", "150000", 1);
        GA.OdemeSec(e, "KART");   // nakit (tevsik) kilidi bu testin konusu değil
        Assert.Empty(e.FindAll("#masak-amac"));
        GA.KimlikFormuYok(e);   // TEMA TEMİZLİĞİ: tezgâhta "+ Kimlik bilgisi" de yok (kapalı çekmece yanlış yeşil vermez)
        FiyatliKalem(e, "u2", "150000", 2);
        GA.KimlikFormuAc(e);    // tezgâhta form "+ Kimlik bilgisi" çekmecesinde (aynı MasakParcasi)
        e.WaitForElement("#masak-amac", Bekle);
        Assert.NotEmpty(e.FindAll("#masak-aciklama"));
        lock (istekler) Assert.Contains(istekler, i => i.Yol.Contains("/masak/gereksinim?tutar=300000&cariId=c1"));
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
        // YASAL-KAPI-2 tur 2: PRO kilit metni eşik SAYISI söylemez (kademe "Yasal kapılar" panelinde); neyin eksik olduğunu söyler.
        PTezgahTurKimlikYardim.EksikKilit(GA.Kaydet(e).GetAttribute("title"), Kuyumcu.Core.I18n.ParaDemo3Sozlugu.Tr.AlanIslemAmaci);   // P-TEZGAH-TUR-KIMLIK K4
    }

    [Fact]
    public void T19_tek_kalem_esik_ALTI_ama_musterinin_gunluk_KUMULATIFI_ustu_alanlar_ONCEDEN_acilir()
    {
        // Bağlantılı işlem: sunucu aynı gün aynı müşterinin işlemlerini ekler → kimlik ister. İstemci eşik TAHMİN
        // ETMEZ; müşteriyle sorar, cevaba göre alanları açar ve Kaydet'i sebebiyle kilitler (red beklenmez).
        gereksinimOzel = v => v.Contains("tutar=150000&cariId=c1") ? GereksinimKimlik : null;
        var e = Ekran();
        GA.MusteriSec(e, "c1");
        FiyatliKalem(e, "u1", "150000", 1);
        GA.OdemeSec(e, "KART");   // nakit (tevsik) kilidi bu testin konusu değil
        GA.KimlikFormuAc(e);      // TEMA TEMİZLİĞİ: tezgâhta form "+ Kimlik bilgisi" çekmecesinde
        GA.MasakBelgeNoSil(e);   // SATIS-4 (b): kimlik müşteriden dolar; kilit sınaması için belge no silinir (yeniden dolmaz)
        Assert.True(GA.Kaydet(e).HasAttribute("disabled"));
        PTezgahTurKimlikYardim.EksikKilit(GA.Kaydet(e).GetAttribute("title"));   // YASAL-KAPI-2 tur 2 (PRO) · P-TEZGAH-TUR-KIMLIK K4: eksik alanın adı
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis"));
    }

    [Fact]
    public void T19_kumulatif_formu_doldurulup_KAYDEDILINCE_bos_fiste_MASAK_karti_KALKAR()
    {
        // Canlı ölçüm (t19, 1. deneme): kayıttan sonra kalemler boşaldı ama eski "kimlik gerekli" cevabı kaldı;
        // boş fişte MASAK kartı açık ve Kaydet "Kimlik tespiti eksik" diye kilitliydi.
        gereksinimOzel = v => v.Contains("tutar=150000&cariId=c1") ? GereksinimKimlik : null;
        var e = Ekran();
        GA.MusteriSec(e, "c1");
        FiyatliKalem(e, "u1", "150000", 1);
        GA.OdemeSec(e, "KART");
        GA.KimlikFormuAc(e);   // TEMA TEMİZLİĞİ: tezgâhta form "+ Kimlik bilgisi" çekmecesinde
        e.WaitForElement("#masak-belge-no", Bekle).Input("10000000146");
        e.Find("#masak-ad-soyad").Input("Mehmet Yılmaz");
        e.WaitForAssertion(() => Assert.False(GA.Kaydet(e).HasAttribute("disabled"), GA.Kaydet(e).GetAttribute("title")), Bekle);
        GA.Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")), Bekle);
        Assert.Contains("\"masak\":{", SonPost("/islemler/satis").Govde);
        e.WaitForAssertion(() => GA.KimlikFormuYok(e), Bekle);
        Assert.NotEqual(Kuyumcu.Core.I18n.YasalKapi2Sozlugu.Tr.MasakKilitProKimlik, GA.Kaydet(e).GetAttribute("title"));   // YASAL-KAPI-2 tur 2 (PRO)
    }

    [Fact]
    public void T19_PESIN_musteride_gereksinim_MUSTERISIZ_sorulur_esik_altinda_hic_sorulmaz()
    {
        // Peşin Müşteri kümülatife girmez (sunucu kuralı) → müşteri parametresi gitmez; eşik altında soru yok.
        var e = Ekran();
        FiyatliKalem(e, "u1", "150000", 1);
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Yol.Contains("/masak/gereksinim"));
        GA.KimlikFormuYok(e);
    }


    // ═══ t19 — PRO 960/375 kalem tablosunda Tutar kesiliyordu (tasarım hakemi KRİTİK) ═══════════════════════
    [Fact]
    public void T19_css_PRO_dar_kartta_tablo_SIGAR_ikincil_sutunlar_duser_tutar_kalir()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        var css = System.Text.RegularExpressions.Regex.Replace(
            File.ReadAllText(Path.Combine(d!.FullName, "Kuyumcu.Web", "wwwroot", "css", "tuval-satis.css")),
            @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        var dar = css[css.LastIndexOf("@container fis-pro-kalem (max-width: 760px)", StringComparison.Ordinal)..];
        Assert.Contains(".islem-sayfa.tuval-pro .fis-pro-tablo { min-width: 0; width: 100%; }", dar);
        Assert.Contains(".islem-sayfa.tuval-pro .fis-pro-tablo :is(th, td):nth-child(8) { display: none; }", dar);
        Assert.DoesNotContain("nth-child(9) { display: none", css);   // Tutar sütunu hiçbir kuralda gizlenmez
        var cokDar = css[css.LastIndexOf("@container fis-pro-kalem (max-width: 480px)", StringComparison.Ordinal)..];
        Assert.Contains(".islem-sayfa.tuval-pro .fis-pro-tablo :is(th, td):nth-child(5) { display: none; }", cokDar);
        Assert.Contains("td.fis-pro-ad > .fis-pro-ad-ozet {\n    display: block;", cokDar.Replace("\r\n", "\n"));
        Assert.Contains(".tuval-pro .fis-pro-tablo td.fis-pro-ad > .fis-pro-ad-ozet { display: none; }", css);
        // 375: satır iki kata dizilir (üstte ad tam genişlik, altta adet · tutar · düzenle) — ad kırılmaz, tutar görünür.
        Assert.Contains("display: grid; grid-template-columns: minmax(0, 1fr) auto auto;", cokDar);
        Assert.Contains("> tr.fis-pro-satir > td.fis-pro-ad { grid-column: 1 / -1;", cokDar);
        Assert.Contains(".islem-sayfa.tuval-pro .fis-pro-tablo :is(th, td):nth-child(4),", dar);   // 768'de kart 742 px: Milyem de düşer
        // "Düzenle" satırının tek hücresi barkod gizleme kuralına (:nth-child(1)) takılmaz — tıklayınca ayrıntı GÖRÜNÜR.
        Assert.Contains(".tuval-pro .fis-pro-tablo > tbody > tr.fis-pro-ayrinti > td { display: table-cell; }", css);
    }

    [Fact]
    public void T19_PRO_ad_hucresinde_ayar_gram_OZETI_var_dar_kartta_gosterilir()
    {
        // TEMA TEMİZLİĞİ 17.10: eski PRO kalem tablosu satışta çizilmez — tezgâhın kalem satırı ayarı (ham kod değil) ve gramı
        // her genişlikte satırın kendisinde taşır (tuval: [ayar] [ad · soluk ayrıntı] [tutar]).
        var e = Ekran();
        FiyatliKalem(e, "u1", "10000", 1);
        Assert.Equal("22", e.Find("#yildiz-kalem-0 .yz-tezgah-ayar").TextContent.Trim());
        Assert.Contains("4,30 g", e.Find("#yildiz-kalem-0 .yz-tezgah-ayrinti").TextContent);
        Assert.DoesNotContain("K22", e.Find("#yildiz-kalem-0").TextContent);
    }
}
