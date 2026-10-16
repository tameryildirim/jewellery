using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Sayfalar.Raporlar;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET RAPOR-YASLANDIRMA
//   (1) GOC-47B § 0.7 — açılış devri (ACILIS) cari yaşlandırmada 0-30 gün değil,
//       "vadesi bilinmiyor" (BELIRSIZ) diliminde; rapor özeti, satır ve ekran aynı.
//   (2) YASAL-3 açık kalanı — döviz bakiyesi USD/EUR/GBP diye ELLE sayılmaz;
//       DovizBirimleri (CariBakiye "…Bakiye" kolonlarından türeyen tek kaynak).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class RaporYaslandirmaDbTestleri(TestVeritabani vt)
{
    private const string Gerekce = "Önceki programdan devralınan açılış bakiyesi (test)";

    private static MCari.CariServisi Servis(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));

    private Kiraci Kur(string ad)
    {
        using var db = vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private string CariEkle(Kiraci k, string ad)
    {
        using var db = vt.OlusturSistem();
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Cariler.Add(new Cari
        {
            Id = id, TenantId = k.TenantId, Tur = "MUSTERI", Ad = ad, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    private async Task Acilis(Kiraci k, string cariId, decimal tl, string? pb = null, decimal? doviz = null)
    {
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).AcilisYaz(cariId, new MCari.CariDuzeltmeDto
        {
            TlDelta = tl, HasDelta = 0m, Gerekce = Gerekce, KurSnapshotId = k.KurId,
            DovizParaBirimi = pb, DovizDelta = doviz,
        }, k.Islem);
    }

    private async Task Veresiye(Kiraci k, string cariId, decimal tl)
    {
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).Duzeltme(cariId, new MCari.CariDuzeltmeDto
        {
            TlDelta = tl, HasDelta = 0m, Gerekce = "Test veresiye borcu yazımı (yaşlandırma)", KurSnapshotId = k.KurId,
        }, k.Islem, krediLimitiUygula: false);
    }

    /// <summary>Carinin son işlem tarihini ve hareketlerini <paramref name="gun"/> gün geriye alır.</summary>
    private void GeriTarihle(Kiraci k, string cariId, int gun)
    {
        using var db = vt.OlusturSistem();
        var t = DateTime.UtcNow.AddDays(-gun).AddHours(-1);
        var b = db.CariBakiyeler.Single(x => x.TenantId == k.TenantId && x.CariId == cariId);
        b.SonIslemTarihi = t;
        foreach (var h in db.Hareketler.Where(x => x.TenantId == k.TenantId && x.CariId == cariId))
            h.Tarih = t;
        db.SaveChanges();
    }

    private async Task<JsonElement> Rapor(Kiraci k)
    {
        using var db = vt.Olustur(k.TenantId);
        var r = await TestKurulum.RaporServisi(db).CariYaslandirma(k.Islem);
        return JsonDocument.Parse(JsonSerializer.Serialize(r)).RootElement;
    }

    private static JsonElement Satir(JsonElement r, string cariId) =>
        r.GetProperty("yaslananCariler").EnumerateArray().Single(s => s.GetProperty("cariId").GetString() == cariId);

    /// <summary>
    /// RY-1 — açılış bakiyeli cari BELIRSIZ dilimde (neden ACILIS, gün yok); 10/40/100 günlük
    /// veresiyeli cariler kendi dilimlerinde; özet her dilimde satırların toplamına eşit ve
    /// açılış 0-30'a KARIŞMAZ. Mutasyon M-RY-1 (ACILIS 0-30'a geri) KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Acilis_bakiyesi_BELIRSIZ_dilimde_veresiyeler_gun_dilimlerinde_ozet_satirlarla_ayni()
    {
        var k = Kur("RY acilis");
        var acilis = CariEkle(k, "RY açılışlı");
        var g10 = CariEkle(k, "RY 10 gün");
        var g40 = CariEkle(k, "RY 40 gün");
        var g100 = CariEkle(k, "RY 100 gün");
        await Acilis(k, acilis, 1_000m);
        await Veresiye(k, g10, 100m);   GeriTarihle(k, g10, 10);
        await Veresiye(k, g40, 200m);   GeriTarihle(k, g40, 40);
        await Veresiye(k, g100, 300m);  GeriTarihle(k, g100, 100);

        var r = await Rapor(k);

        var a = Satir(r, acilis);
        Assert.Equal("BELIRSIZ", a.GetProperty("dilim").GetString());
        Assert.Equal("ACILIS", a.GetProperty("belirsizNedeni").GetString());
        Assert.Equal(JsonValueKind.Null, a.GetProperty("gunSayisi").ValueKind);
        Assert.True(a.GetProperty("acilisDahil").GetBoolean());

        Assert.Equal("0-30", Satir(r, g10).GetProperty("dilim").GetString());
        Assert.Equal("31-60", Satir(r, g40).GetProperty("dilim").GetString());
        Assert.Equal("90+", Satir(r, g100).GetProperty("dilim").GetString());
        Assert.False(Satir(r, g10).GetProperty("acilisDahil").GetBoolean());
        Assert.Equal(JsonValueKind.Null, Satir(r, g10).GetProperty("belirsizNedeni").ValueKind);

        var oz = r.GetProperty("ozetTl");
        Assert.Equal(100m, oz.GetProperty("0-30").GetDecimal());
        Assert.Equal(200m, oz.GetProperty("31-60").GetDecimal());
        Assert.Equal(0m, oz.GetProperty("61-90").GetDecimal());
        Assert.Equal(300m, oz.GetProperty("90+").GetDecimal());
        Assert.Equal(1_000m, oz.GetProperty("BELIRSIZ").GetDecimal());

        // Rapor özeti ↔ satır dilimleri aynı kuraldan (ekran satırdan, özet sunucudan okur).
        foreach (var d in new[] { "0-30", "31-60", "61-90", "90+", "BELIRSIZ" })
        {
            var satirToplami = r.GetProperty("yaslananCariler").EnumerateArray()
                .Where(s => s.GetProperty("dilim").GetString() == d)
                .Sum(s => s.GetProperty("tlBakiye").GetDecimal());
            Assert.Equal(oz.GetProperty(d).GetDecimal(), satirToplami);
        }
    }

    /// <summary>
    /// RY-2 — açılıştan SONRA işlem gören cari tarihli dilimde (mevcut "son işlem" modeli)
    /// ama "açılış bakiyesi içerir" bayrağı taşır; yaşlı pay saklanmaz.
    /// </summary>
    [Fact]
    public async Task Acilistan_sonra_islem_goren_cari_tarihli_dilimde_ve_acilisDahil_bayragi_tasir()
    {
        var k = Kur("RY karma");
        var c = CariEkle(k, "RY açılış + yeni");
        await Acilis(k, c, 500m);
        await Veresiye(k, c, 50m);

        var s = Satir(await Rapor(k), c);
        Assert.Equal("0-30", s.GetProperty("dilim").GetString());
        Assert.True(s.GetProperty("acilisDahil").GetBoolean());
        Assert.Equal(550m, s.GetProperty("tlBakiye").GetDecimal());
    }

    /// <summary>
    /// RY-3 — yalnız GBP bakiyeli cari yaşlandırmada GÖRÜNÜR, dovizBakiyeleri GBP'yi taşır ve
    /// ozetDoviz GBP toplar. Mutasyon M-RY-2 (GBP kolon listesinden çıkar) KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Yalniz_GBP_bakiyeli_cari_yaslandirmada_gorunur_ve_ozetDoviz_GBP_toplar()
    {
        var k = Kur("RY gbp");
        var gbp = CariEkle(k, "RY sterlin");
        var usd = CariEkle(k, "RY dolar");
        await Acilis(k, gbp, 0m, "GBP", 100m);
        await Acilis(k, usd, 0m, "USD", 250m);

        var r = await Rapor(k);
        Assert.Equal(2, r.GetProperty("toplamSatir").GetInt32());

        var s = Satir(r, gbp);
        Assert.True(s.GetProperty("dovizBakiyesiVar").GetBoolean());
        Assert.Equal(100m, s.GetProperty("dovizBakiyeleri").GetProperty("GBP").GetDecimal());
        Assert.False(s.GetProperty("dovizBakiyeleri").TryGetProperty("USD", out _));

        var oz = r.GetProperty("ozetDoviz");
        Assert.Equal(100m, oz.GetProperty("GBP").GetDecimal());
        Assert.Equal(250m, oz.GetProperty("USD").GetDecimal());
        Assert.Equal(0m, oz.GetProperty("EUR").GetDecimal());
    }

    /// <summary>
    /// RY-4 — pasife alma ön koşulu (bağlı kayıtlar) yalnız GBP bakiyesini de döviz bakiyesi
    /// sayar (CariModulu — eski elle liste). Mutasyon M-RY-3 KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Bagli_kayitlar_yalniz_GBP_bakiyesini_doviz_bakiyesi_sayar()
    {
        var k = Kur("RY bagli");
        var c = CariEkle(k, "RY sterlin bağlı");
        await Acilis(k, c, 0m, "GBP", 40m);

        using var db = vt.Olustur(k.TenantId);
        var r = await Servis(db).BagliKayitlariGetir(c, k.Islem);
        var j = JsonDocument.Parse(JsonSerializer.Serialize(r, new JsonSerializerOptions(JsonSerializerDefaults.Web))).RootElement;
        Assert.True(j.GetProperty("dovizBakiyeVar").GetBoolean());
    }

    /// <summary>
    /// RY-9 (tur 2, hasım KRİTİK) — açılış 1000 → 50 TL veresiye → açılış TERS kaydı (−1000):
    /// açılış neti 0, kalan 50 TL bugünkü borç. 0-30'da kalır, "açılış" rozeti/bayrağı YOK,
    /// BELIRSIZ özeti 0. Ters açılış anı "son işlem" sayılmaz. Mutasyon M-RY-8 KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Acilis_ters_kaydindan_sonra_kalan_yeni_borc_0_30_da_acilis_bayragi_yok()
    {
        var k = Kur("RY ters");
        var c = CariEkle(k, "RY ters açılış");
        await Acilis(k, c, 1_000m);
        await Veresiye(k, c, 50m);
        await Acilis(k, c, -1_000m);

        var r = await Rapor(k);
        var s = Satir(r, c);
        Assert.Equal(50m, s.GetProperty("tlBakiye").GetDecimal());
        Assert.Equal("0-30", s.GetProperty("dilim").GetString());
        Assert.False(s.GetProperty("acilisDahil").GetBoolean());
        Assert.Equal(JsonValueKind.Null, s.GetProperty("belirsizNedeni").ValueKind);
        Assert.Equal(50m, r.GetProperty("ozetTl").GetProperty("0-30").GetDecimal());
        Assert.Equal(0m, r.GetProperty("ozetTl").GetProperty("BELIRSIZ").GetDecimal());
    }

    /// <summary>
    /// RY-10 (tur 2, hasım ORTA) — yalnız döviz açılışı USD +100 ve EUR −100 (TL 0): birimler
    /// kendi içinde netlenir (toplanıp sıfırlanmaz) → açılış bakiyede, BELIRSIZ / ACILIS,
    /// acilisDahil true. Ardından 50 TL veresiye → 0-30 ve acilisDahil HÂLÂ true.
    /// Mutasyon M-RY-9 (döviz netini birimden bağımsız topla) KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Karma_doviz_acilisi_birim_bazinda_netlenir_belirsiz_ve_acilisDahil()
    {
        var k = Kur("RY karma doviz");
        var c = CariEkle(k, "RY karma döviz açılışı");
        await Acilis(k, c, 0m, pb: "USD", doviz: 100m);
        await Acilis(k, c, 0m, pb: "EUR", doviz: -100m);

        var s = Satir(await Rapor(k), c);
        Assert.Equal("BELIRSIZ", s.GetProperty("dilim").GetString());
        Assert.Equal("ACILIS", s.GetProperty("belirsizNedeni").GetString());
        Assert.True(s.GetProperty("acilisDahil").GetBoolean());

        await Veresiye(k, c, 50m);
        var r = await Rapor(k);
        s = Satir(r, c);
        Assert.Equal("0-30", s.GetProperty("dilim").GetString());
        Assert.True(s.GetProperty("acilisDahil").GetBoolean());
        Assert.Equal(50m, r.GetProperty("ozetTl").GetProperty("0-30").GetDecimal());
    }
}

/// <summary>Saf kurallar — veritabanı gerektirmez.</summary>
public sealed class RaporYaslandirmaSafTestleri
{
    /// <summary>
    /// RY-5 — döviz kolonları elle değil, CariBakiye "…Bakiye" kolonlarından türer ve kod kümesi
    /// yazıcının kapalı kümesiyle (DovizBakiye.ParaBirimleri) AYNIDIR: biri değişip öteki
    /// değişmezse KIRMIZI. Her birim tek başına "döviz bakiyesi var" sayılır.
    /// </summary>
    [Fact]
    public void Doviz_birimleri_kolonlardan_turer_ve_yazici_kumesiyle_ayni()
    {
        Assert.Equal(
            MCari.DovizBakiye.ParaBirimleri.OrderBy(x => x, StringComparer.Ordinal),
            DovizBirimleri.Kodlar.OrderBy(x => x, StringComparer.Ordinal));
        Assert.DoesNotContain("TL", DovizBirimleri.Kodlar);
        Assert.DoesNotContain("HASGRAM", DovizBirimleri.Kodlar);

        foreach (var pb in MCari.DovizBakiye.ParaBirimleri)
        {
            var b = new CariBakiye { CariId = "c", TenantId = "t" };
            Assert.False(DovizBirimleri.BakiyeVar(b));
            MCari.DovizBakiye.Ekle(b, pb, 1m);
            Assert.True(DovizBirimleri.BakiyeVar(b), pb);
            Assert.Equal(1m, DovizBirimleri.Bakiyeler(b)[pb]);
        }
        // TL / has bakiyesi döviz sayılmaz.
        Assert.False(DovizBirimleri.BakiyeVar(new CariBakiye { CariId = "c", TenantId = "t", TlBakiye = 5m, HasGramBakiye = 1m }));
    }

    [Theory]
    [InlineData(null, "BELIRSIZ")]
    [InlineData(0, "0-30")]
    [InlineData(30, "0-30")]
    [InlineData(31, "31-60")]
    [InlineData(60, "31-60")]
    [InlineData(61, "61-90")]
    [InlineData(90, "61-90")]
    [InlineData(91, "90+")]
    public void Dilim_sinirlari(int? gun, string beklenen) =>
        Assert.Equal(beklenen, Kuyumcu.Api.Modules.Rapor.CariYaslandirmaKurali.Dilim(gun));

    /// <summary>RY-11 — BELIRSIZ nedeni: ACILIS yalnız açılış bakiyedeyken; yoksa TARIH_YOK; tarihli dilimde null.</summary>
    [Theory]
    [InlineData(null, true, "ACILIS")]
    [InlineData(null, false, "TARIH_YOK")]
    [InlineData(5, true, null)]
    [InlineData(5, false, null)]
    public void Belirsiz_nedeni_acilis_yalniz_acilisDahil_iken(int? gun, bool acilisDahil, string? beklenen) =>
        Assert.Equal(beklenen, Kuyumcu.Api.Modules.Rapor.CariYaslandirmaKurali.BelirsizNedeni(gun, acilisDahil));
}

/// <summary>Ekran (bUnit) — "Tarihsiz" sütunu ve döviz özeti.</summary>
public sealed class RaporYaslandirmaEkranTestleri : BunitContext
{
    // a1: açılış devri (BELIRSIZ/ACILIS) 1.000 ₺ · b1: 10 gün 100 ₺ · g1: yalnız GBP 100 (açılış).
    private const string YanitBelirsizli = """
        {"yaslananCariler":[
           {"cariId":"a1","cariAd":"Açılışlı Müşteri","cariTur":"MUSTERI","tlBakiye":1000.00,"hasGramBakiye":0,
            "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyeleri":{},"dovizBakiyesiVar":false,
            "gunSayisi":null,"dilim":"BELIRSIZ","belirsizNedeni":"ACILIS","acilisDahil":true},
           {"cariId":"b1","cariAd":"Yeni Borçlu","cariTur":"MUSTERI","tlBakiye":100.00,"hasGramBakiye":0,
            "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyeleri":{},"dovizBakiyesiVar":false,
            "gunSayisi":10,"dilim":"0-30","belirsizNedeni":null,"acilisDahil":false},
           {"cariId":"g1","cariAd":"Sterlin Müşterisi","cariTur":"MUSTERI","tlBakiye":0,"hasGramBakiye":0,
            "usdBakiye":0,"eurBakiye":0,"gbpBakiye":100.00,"dovizBakiyeleri":{"GBP":100.00},"dovizBakiyesiVar":true,
            "gunSayisi":null,"dilim":"BELIRSIZ","belirsizNedeni":"ACILIS","acilisDahil":true}],
         "ozetTl":{"0-30":100.00,"31-60":0,"61-90":0,"90+":0,"BELIRSIZ":1000.00},
         "ozetDoviz":{"USD":0,"EUR":0,"GBP":100.00},
         "toplamSatir":3,"ofset":0,"limit":500,"dahaFazlasiVar":false}
        """;

    // t1: işlem tarihi olmayan bakiye (BELIRSIZ / TARIH_YOK), açılış yok.
    private const string YanitTarihYok = """
        {"yaslananCariler":[
           {"cariId":"t1","cariAd":"Tarihsiz Müşteri","cariTur":"MUSTERI","tlBakiye":20.00,"hasGramBakiye":0,
            "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyeleri":{},"dovizBakiyesiVar":false,
            "gunSayisi":null,"dilim":"BELIRSIZ","belirsizNedeni":"TARIH_YOK","acilisDahil":false}],
         "ozetTl":{"0-30":0,"31-60":0,"61-90":0,"90+":0,"BELIRSIZ":20.00},
         "ozetDoviz":{"USD":0,"EUR":0,"GBP":0},
         "toplamSatir":1,"ofset":0,"limit":500,"dahaFazlasiVar":false}
        """;

    private const string YanitBelirsizsiz = """
        {"yaslananCariler":[
           {"cariId":"b1","cariAd":"Yeni Borçlu","cariTur":"MUSTERI","tlBakiye":100.00,"hasGramBakiye":0,
            "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyeleri":{},"dovizBakiyesiVar":false,
            "gunSayisi":10,"dilim":"0-30","belirsizNedeni":null,"acilisDahil":false}],
         "ozetTl":{"0-30":100.00,"31-60":0,"61-90":0,"90+":0,"BELIRSIZ":0},
         "ozetDoviz":{"USD":0,"EUR":0,"GBP":0},
         "toplamSatir":1,"ofset":0,"limit":500,"dahaFazlasiVar":false}
        """;

    private string yanit = YanitBelirsizli;

    public RaporYaslandirmaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => yanit))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi(Func<string> yaslandirma) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/odeme-araci") =>
                    """{"baslangic":"2026-09-01","bitis":"2026-09-30","kaynak":"KASA_HAREKETLERI","araclar":[],"veresiye":{"doganTl":0,"kapananTl":0},"bilinmeyen":{"nakitPayiBilinmeyenSatir":0,"nakitPayiBilinenSatir":0},"storno":{"adet":0,"tutarTl":0},"guvenilirlik":"VERI_YOK","satisKirilimi":{"kaynak":"ODEME_KALEMLERI","satirlar":[],"brutSatisTl":0,"kirilimToplamTl":0,"farkTl":0,"kasayaGirenTl":0,"mahsupAltinTl":0,"veresiyeTl":0,"fisAdet":0,"kalemsizFisAdet":0,"kalemsizTl":0}}""",
                var y when y.EndsWith("/raporlar/aylik") => """{"yil":2026,"ay":9,"gunler":[]}""",
                var y when y.EndsWith("/raporlar/cari-yaslandirma") => yaslandirma(),
                var y when y.EndsWith("/raporlar/stok-degeri") =>
                    """{"urunler":[],"toplamHas":0,"madenDagilimi":[],"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false,"uyari":null}""",
                var y when y.EndsWith("/raporlar/reyon-stok") => """{"reyonlar":[],"subeler":[]}""",
                var y when y.EndsWith("/raporlar/stok-yaslandirma") =>
                    """{"olculduAn":"2026-09-22T06:00:00Z","subeId":null,"kovalar":[],"madenDagilimi":[],"yasiBilinmeyenAdet":0,"toplamAdet":0,"devir":null,"oluStok":{"urunler":[],"dahaFazlasiVar":false},"guvenilirlik":"VERI_YOK","uyarilar":[]}""",
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") =>
                    """{"tarih":"2026-09-17","tahsilat":{"nakit":0,"kart":0,"havale":0,"diger":0,"toplam":0},"satis":{"sayisi":0,"toplamTl":0,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":0,"faturasizTl":0},"alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},"hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},"fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},"fark":0,"mutabik":true,"uyarilar":[],"faturasizIslemler":[],"faturaEslesme":null}""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Raporlar> Ac()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yas-tablo")));
        return e;
    }

    /// <summary>
    /// RY-6 — açılış satırı "Tarihsiz" sütununda, 0-30'da DEĞİL; satır
    /// rozeti ham kod basmaz; alt toplam ve açıklama notu çizilir. M-RY-4 KIRMIZI.
    /// </summary>
    [Fact]
    public void Acilis_satiri_ayri_sutunda_0_30_da_degil()
    {
        var e = Ac();
        var m = Kuyumcu.Core.I18n.RaporYaslandirmaSozlugu.Tr;

        Assert.Equal(m.BelirsizBaslik, e.Find("#yas-belirsiz-baslik").TextContent.Trim());
        Assert.Equal(8, e.FindAll("#yas-tablo thead th").Count);

        Assert.Equal(Bicim.TlKurus(1000m), e.Find("#yas-satir-a1 td.yas-belirsiz").TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(0m), e.Find("#yas-satir-a1 td.yas-0-30").TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(100m), e.Find("#yas-satir-b1 td.yas-0-30").TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(0m), e.Find("#yas-satir-b1 td.yas-belirsiz").TextContent.Trim());

        Assert.Equal(m.AcilisRozet, e.Find("#yas-acilis-a1").TextContent.Trim());
        Assert.DoesNotContain("ACILIS", e.Find("#yas-tablo").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("BELIRSIZ", e.Find("#yas-tablo").TextContent, StringComparison.Ordinal);

        Assert.Equal(Bicim.TlKurus(1000m), e.Find("#yas-toplam-belirsiz").TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(100m), e.Find("#yas-alt-toplam td.yas-0-30").TextContent.Trim());
        Assert.Equal(m.BelirsizNotu, e.Find("#yas-belirsiz-notu").TextContent.Trim());
        // Tur 2: sütun başlığı ipucu iki nedeni de anlatır (yalnız açılış değil).
        Assert.Equal(m.BelirsizIpucu, e.Find("#yas-belirsiz-baslik").GetAttribute("title"));
        Assert.Equal(m.AcilisIpucu, e.Find("#yas-acilis-a1").GetAttribute("title"));
    }

    /// <summary>
    /// RY-12 (tur 2, hasım DÜŞÜK) — işlem tarihi olmayan satır "Tarihsiz" sütununda, açılış
    /// rozeti DEĞİL "İşlem tarihi yok" rozeti ve kendi ipucuyla; not iki nedeni de söyler.
    /// </summary>
    [Fact]
    public void Tarih_yok_satiri_kendi_rozeti_ve_ipucuyla()
    {
        yanit = YanitTarihYok;
        var e = Ac();
        var m = Kuyumcu.Core.I18n.RaporYaslandirmaSozlugu.Tr;
        Assert.Equal(m.TarihYokRozet, e.Find("#yas-tarihyok-t1").TextContent.Trim());
        Assert.Equal(m.TarihYokIpucu, e.Find("#yas-tarihyok-t1").GetAttribute("title"));
        Assert.Empty(e.FindAll("#yas-acilis-t1"));
        Assert.Equal(Bicim.TlKurus(20m), e.Find("#yas-satir-t1 td.yas-belirsiz").TextContent.Trim());
        Assert.Equal(m.BelirsizNotu, e.Find("#yas-belirsiz-notu").TextContent.Trim());
        Assert.Contains("işlem tarihi", m.BelirsizNotu, StringComparison.Ordinal);
    }

    /// <summary>
    /// RY-7 — yalnız GBP bakiyeli satır tutarını £ ile basar ve özet satırı GBP toplamını
    /// sunucunun ozetDoviz'inden yazar (istemcide sabit birim listesi yok).
    /// </summary>
    [Fact]
    public void GBP_bakiyesi_satirda_ve_doviz_ozetinde_gorunur()
    {
        var e = Ac();
        var m = Kuyumcu.Core.I18n.RaporYaslandirmaSozlugu.Tr;

        // TOPLU-3 (tasarım hakemi ORTA "döviz rozetinde yön yok"): rozet tutarı YÖNÜYLE basar;
        // özet tablo altı paragrafı değil tfoot satırıdır, açıklama (i) içindedir. Kural aynı:
        // birim sunucudan, sıfır birim yazılmaz.
        var t3 = Kuyumcu.Core.I18n.Toplu3Sozlugu.Tr;
        Assert.Equal(string.Format(t3.YasDovizBizeBorclu, Bicim.Para(100m, "GBP")), e.Find("#yas-doviz-g1").TextContent.Trim());
        var ozetSatiri = e.Find("#yas-doviz-ozet");
        Assert.Equal("TR", ozetSatiri.TagName);
        Assert.NotNull(ozetSatiri.Closest("tfoot"));
        var ozet = ozetSatiri.TextContent;
        Assert.Contains(t3.YasDovizOzetKisa, ozet, StringComparison.Ordinal);
        Assert.Contains(m.DovizSatirIpucu, ozet, StringComparison.Ordinal);
        Assert.Contains(Bicim.Para(100m, "GBP"), ozet, StringComparison.Ordinal);
        // Sıfır olan birimler özete yazılmaz.
        Assert.DoesNotContain(Bicim.Para(0m, "USD"), ozet, StringComparison.Ordinal);
    }

    /// <summary>RY-8 — vadesi bilinmeyen satır yoksa sütun ve not ÇİZİLMEZ (zorunlu önce).</summary>
    [Fact]
    public void Belirsiz_satir_yoksa_sutun_cizilmez()
    {
        yanit = YanitBelirsizsiz;
        var e = Ac();
        Assert.Empty(e.FindAll("#yas-belirsiz-baslik"));
        Assert.Empty(e.FindAll("td.yas-belirsiz"));
        Assert.Empty(e.FindAll("#yas-belirsiz-notu"));
        Assert.Empty(e.FindAll("#yas-doviz-ozet"));
        Assert.Equal(7, e.FindAll("#yas-tablo thead th").Count);
    }
}
