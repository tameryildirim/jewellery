using System.Text.Json;
using Bunit;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET L-1b — TEVSİK AÇIĞI (ALIŞ): ÇOK KALEMLİ ALIŞ TEK FİŞ (bUnit, sahte API)
//
// Kök neden (L-1 doğrulayıcısının ORTA bulgusu): Kaydet() çok kalemli ALIŞ'ı kalem
// başına AYRI POST /islemler/alis ile gönderiyordu → Peşin Müşteri'den iki parça
// alınırken toplam haddi aşan nakit ödeme iki kayda bölünüyordu. Artık ≥2 kalemli
// alış TEK POST /islemler/alis-fisi'dir; tek kalemli alış eski yolda kalır.
//
// ⚠ Eşik istemcide YOK: ekran rakam gömmez; reddi sunucu verir, ekran gösterir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class L1bAlisFisiEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool fisReddet;

    public L1bAlisFisiEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => fisReddet)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private const string RetMesaji = "Peşin Müşteri ile bu tutarda işlem yapılamaz: tevsik (459 s. VUK GT) eşiğine ulaşıyor.";

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<bool> reddet) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, yol, govde));
            if (yol.Contains("/urunler/barkod/"))
                return Yanit(System.Net.HttpStatusCode.NotFound, """{"message":"bulunamadı"}""");
            if (yol.Contains("/islemler/alis-fisi") && istek.Method == HttpMethod.Post && reddet())
                return Yanit(System.Net.HttpStatusCode.BadRequest,
                    $$"""{"message":"{{RetMesaji}}","alanlar":{"cariId":["{{RetMesaji}}"]},"error":"Bad Request","statusCode":400}""");
            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") =>
                    """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":50000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":50000,"islemTutariKuraliDevrede":false}""",   // SATIS-3 t10: 40.000 ₺ had ALTINDA → istemci kilitlemez, sunucu reddi yolu sınanır
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":20000,"hasDegeri":19000,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":20000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":3.94,"iscilikTl":0,"satisFiyati":20000,"gramAgirlik":4.3},{"id":"u2","ad":"Tek Tas Yuzuk","ayarId":"K14","hasGrami":1.2,"iscilikTl":0,"satisFiyati":20000,"gramAgirlik":2}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                // Gerçekçi alış fişi yanıtı (SatisServisi.AlisFisi şekli).
                var v when v.Contains("/islemler/alis-fisi") => """{"fisId":"01J00000000000000000000AF1","hareketId":"01J00000000000000000000A11","hareketIdleri":["01J00000000000000000000A11","01J00000000000000000000A12"],"kalemSayisi":2,"toplamTl":40000,"odenenTl":40000,"kalanTl":0,"odemeTuru":"NAKIT","yerlesimUyarisi":"Mal ATANMAMIŞ olarak girdi"}""",
                // Tek kalemli alış: hareketin kendisi (HareketGetir şekli).
                var v when v.Contains("/islemler/alis") => """{"id":"01J00000000000000000000A01","fisId":null,"tur":"ALIS","tlTutari":-20000}""",
                var v when v.Contains("/iade-adaylari") => "[]",
                var v when v.Contains("/reyon") => "[]",
                var v when v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return Yanit(System.Net.HttpStatusCode.OK, y);
        }

        private static HttpResponseMessage Yanit(System.Net.HttpStatusCode kod, string govde) =>
            new(kod) { Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json") };
    }

    /// <summary>ALIŞ yalnız PRO'dadır (T-S1: basit ızgarada ALIS karosu yok) — SAHİP, PRO.</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> AlisEkrani()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Sahip", "s@ornek.com", "SAHIP", "s1", null, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#islem-tur-ALIS").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#islem-urun")));
        return e;
    }

    private static void KalemEkle(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string urunId, int beklenen, string birim = "20000")
    {
        SeciciYardim.Sec(e, "islem-urun", urunId);
        e.WaitForAssertion(() => Assert.Equal(urunId, SeciciYardim.SeciliDeger(e, "islem-urun")));
        e.Find("#islem-birimfiyat").Input(birim);
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForAssertion(() => Assert.Equal(beklenen, e.FindAll("#islem-kalem-tablo tbody tr, #islem-kalem-pro tr.fis-pro-satir").Count), TimeSpan.FromSeconds(5));
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Hazir(int kalemSayisi)
    {
        var e = AlisEkrani();
        SeciciYardim.Sec(e, "islem-cari", "pm1");
        if (e.FindAll("#islem-kasa").Count > 0) SeciciYardim.Sec(e, "islem-kasa", "ks1");   // SATIS-3: tek kasada seçici yok (otomatik)
        KalemEkle(e, "u1", 1);
        if (kalemSayisi > 1) KalemEkle(e, "u2", 2);
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled"),
            "Kaydet kilitli: " + string.Join(" | ", e.FindAll("#islem-kaydet-kilit, .alan-hata").Select(x => x.TextContent.Trim()))), TimeSpan.FromSeconds(5));
        lock (istekler) istekler.Clear();
        return e;
    }

    private int Post(string sonEk) { lock (istekler) return istekler.Count(i => i.Metot == "POST" && i.Yol.EndsWith(sonEk)); }
    private JsonElement Govdesi(string sonEk) { lock (istekler) return JsonDocument.Parse(istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith(sonEk)).Govde).RootElement.Clone(); }

    [Fact]
    public void Iki_kalemli_ALIS_TEK_alis_fisi_istegi_kalem_basi_alis_SIFIR_govde_ayristirilir()
    {
        var e = Hazir(2);
        e.Find("#islem-kaydet").Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Equal(1, Post("/islemler/alis-fisi")), TimeSpan.FromSeconds(5));
        Assert.Equal(0, Post("/islemler/alis"));   // ⚠ L-1b: kalem başına POST YOK

        var g = Govdesi("/islemler/alis-fisi");
        Assert.Equal("pm1", g.GetProperty("cariId").GetString());
        var kalemler = g.GetProperty("kalemler").EnumerateArray().ToList();
        Assert.Equal(["u1", "u2"], kalemler.Select(k => k.GetProperty("urunId").GetString()!).ToArray());
        Assert.All(kalemler, k => Assert.Equal(20000m, k.GetProperty("birimFiyatTl").GetDecimal()));
        Assert.All(kalemler, k => Assert.Equal(1m, k.GetProperty("adet").GetDecimal()));
        Assert.Equal("NAKIT", g.GetProperty("odemeTuru").GetString());
        Assert.Equal("ks1", g.GetProperty("kasaId").GetString());
        Assert.Equal("kur-1", g.GetProperty("kurSnapshotId").GetString());
        Assert.True(g.TryGetProperty("reyonId", out _));   // tek kalemli uçla aynı reyon sözleşmesi
        Assert.False(g.TryGetProperty("urunId", out _));    // kalem alanı fiş düzeyine sızmaz

        // UX-S27: sonuç okunur — başarı bandında fiş no, yerleşim uyarısı ve TEK yazdırma bağlantısı.
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".bant-basari")), TimeSpan.FromSeconds(5));
        Assert.DoesNotContain("01J00000000000000000000AF1", e.Markup);   // S2-5: ham ULID basılmaz (FisYazdir.OkunurNo)
        Assert.Contains("ATANMAMIŞ", e.Markup);
        Assert.Single(e.FindAll("#islem-yazdir-sar a"));
    }

    [Fact]
    public void Sunucu_tevsikle_reddederse_hata_GORUNUR_basari_YOK_kalem_basi_yedek_POST_YOK()
    {
        fisReddet = true;
        var e = Hazir(2);
        e.Find("#islem-kaydet").Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Contains(RetMesaji, e.Markup), TimeSpan.FromSeconds(5));
        Assert.Equal(1, Post("/islemler/alis-fisi"));
        Assert.Equal(0, Post("/islemler/alis"));
        Assert.Empty(e.FindAll(".bant-basari"));
        Assert.Equal(2, e.FindAll("#islem-kalem-tablo tbody tr, #islem-kalem-pro tr.fis-pro-satir").Count);   // fiş kaybolmaz
    }

    [Fact]
    public void Tek_kalemli_ALIS_bugunku_alis_ucunda_kalir_davranis_DEGISMEDI()
    {
        var e = Hazir(1);
        e.Find("#islem-kaydet").Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Equal(1, Post("/islemler/alis")), TimeSpan.FromSeconds(5));
        Assert.Equal(0, Post("/islemler/alis-fisi"));
        var g = Govdesi("/islemler/alis");
        Assert.Equal("u1", g.GetProperty("urunId").GetString());
        Assert.Equal(20000m, g.GetProperty("birimFiyatTl").GetDecimal());
        Assert.Equal("NAKIT", g.GetProperty("odemeTuru").GetString());
    }

    [Fact]
    public void MASAK_formu_FIS_toplamiyla_acilir_her_kalem_esik_altinda_olsa_da()
    {
        // Sunucu alış fişinde MASAK'ı fiş TOPLAMIYLA çağırır; ekran kalem başına en büyük kaleme
        // baksaydı form açılmaz, Kaydet sunucudan 400 alırdı. Eşik sahte API'den gelir (185.000).
        var e = AlisEkrani();
        SeciciYardim.Sec(e, "islem-cari", "c1");
        if (e.FindAll("#islem-kasa").Count > 0) SeciciYardim.Sec(e, "islem-kasa", "ks1");   // SATIS-3: tek kasada seçici yok (otomatik)
        KalemEkle(e, "u1", 1, "100000");
        // SATIS-3 t19: gerçek müşteride eşik altında da sorulur (kümülatif sunucuda) — ama KALEM tutarıyla değil, fiş toplamıyla;
        // sunucu "YOK" dediği için form açılmaz.
        Assert.Empty(e.FindAll(".kart--masak"));
        KalemEkle(e, "u2", 2, "100000");
        e.WaitForAssertion(() => Assert.Contains(istekler.ToList(), i => i.Yol.Contains("/masak/gereksinim?tutar=200000")), TimeSpan.FromSeconds(5));
    }
}
