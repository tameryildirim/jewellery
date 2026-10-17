using System.Net;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Maliyet;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET MADEN-2 (03.10) — KRİTİK: altın kârı (has) KISMI durumda has maliyeti
// bilinmeyen altın satışı SIFIR maliyetle kâra giriyordu (MAS20) + MADEN-AYRIMI kalanları.
//
// İLKE (KAR-RAPOR fail-closed, TL bacağıyla AYNI): has bacağında bir altın satışı
// ancak TÜM parti satırlarında has maliyeti biliniyorsa "bilinen" sayılır;
// karHas = bilinen has hasılatı − bilinen has maliyeti; eksik olanın has hasılatı
// ayrı alanda (maliyetiEksikHasilatHas); hiçbiri bilinmiyorsa null. TL kuralı ile
// TEK yardımcı (KarZararServisi.HasMaliyetiBilinenler → MaliyetiBilinenler).
//
// Kur fikstürü: AltinHasFiyati 6.827,20 ₺ (TestKurulum). Beklenenler SABİT literal:
//   1.500 / 6.827,20 = 0,21970939… → 0,2197 has ; 1.000 / 6.827,20 = 0,14647293… → 0,1465 has.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MadenIkiDbTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) => new(db, NullLogger<MSatis.SatisServisi>.Instance);
    private static DateOnly Bugun => DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));
    private static System.Text.Json.JsonElement J(object o) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o)).RootElement;
    private static decimal D(System.Text.Json.JsonElement e, string ad) => e.GetProperty(ad).GetDecimal();
    private static bool Bos(System.Text.Json.JsonElement e, string ad) =>
        e.GetProperty(ad).ValueKind == System.Text.Json.JsonValueKind.Null;
    private static string S(System.Text.Json.JsonElement e, string ad) => e.GetProperty(ad).GetString()!;
    private Kiraci Kur(KuyumcuDbContext kurulum, string ad) => TestKurulum.KiraciKur(kurulum, "PAKETMADEN-2-TEST " + ad);

    /// <summary>Parti: TL maliyeti 1.000; hasBilinir=false ise alış günü has fiyatı YOK (BirimMaliyetHas null).</summary>
    private static void Parti(KuyumcuDbContext db, Kiraci k, int gunOnce, bool hasBilinir) =>
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.SayimFazla, 1m,
            k.UrunHasGrami, DateTime.UtcNow.AddDays(-gunOnce), birimMaliyetTl: 1_000m,
            altinHasFiyati: hasBilinir ? 6_827.20m : null);

    private static async Task<string> Sat(KuyumcuDbContext db, Kiraci k, decimal birim)
    {
        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = birim,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);
        return await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS")
            .OrderByDescending(h => h.Olusturuldu).ThenByDescending(h => h.Id).Select(h => h.Id).FirstAsync();
    }

    // ── TAM: has maliyeti biliniyor → karHas = 0,2197 − 0,1465 = 0,0732; durum TAM
    [Fact]
    public async Task MI01_Has_bacagi_TAM_bilinen_maliyetle()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "tam");
        using var db = vt.Olustur(k);
        Parti(db, k, 1, hasBilinir: true);
        await db.SaveChangesAsync();
        await Sat(db, k, 1_500m);

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal(500m, D(r, "karTl"));
        Assert.Equal("TAM", S(r, "hasKarDurumu"));
        Assert.Equal(0.2197m, D(r, "bilinenHasilatHas"));
        Assert.Equal(0.1465m, D(r, "maliyetHas"));
        Assert.Equal(0.0732m, D(r, "karHas"));
        Assert.Equal(0, r.GetProperty("yalnizHasMaliyetiEksikAdedi").GetInt32());
        Assert.Equal(0m, D(r, "maliyetiEksikHasilatHas"));
    }

    // ── KISMI: biri has-bilinmeyen (TL bilinen) + biri bilinen → TL TAM, has KISMI; eksik has hasılatı ayrı
    [Fact]
    public async Task MI02_Has_bacagi_KISMI_has_maliyeti_bilinmeyen_satis_has_karina_girmez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "kismi");
        using var db = vt.Olustur(k);
        Parti(db, k, 2, hasBilinir: false);   // FIFO: ilk satış bunu tüketir
        Parti(db, k, 1, hasBilinir: true);
        await db.SaveChangesAsync();
        await Sat(db, k, 1_500m);
        await Sat(db, k, 1_500m);

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("TAM", S(r, "karDurumu"));                     // TL bacağı: iki satışın da TL maliyeti var
        Assert.Equal(1_000m, D(r, "karTl"));
        Assert.Equal("KISMI", S(r, "hasKarDurumu"));
        Assert.Equal(0.4394m, D(r, "hasilatHas"));
        Assert.Equal(0.2197m, D(r, "bilinenHasilatHas"));          // YALNIZ has maliyeti bilinen satış
        Assert.Equal(0.2197m, D(r, "maliyetiEksikHasilatHas"));
        Assert.Equal(0.1465m, D(r, "maliyetHas"));
        Assert.Equal(0.0732m, D(r, "karHas"));                      // eski hata: 0,4394 − 0,1465 = 0,2929
        Assert.Equal(1, r.GetProperty("hasMaliyetiEksikSatisAdedi").GetInt32());
        Assert.Equal(1, r.GetProperty("yalnizHasMaliyetiEksikAdedi").GetInt32());
    }

    // ── HİÇBİRİ: has maliyeti hiç bilinmiyor → karHas/maliyetHas null; TL kârı yine 500
    [Fact]
    public async Task MI03_Has_bacagi_HESAPLANAMADI_hicbiri_bilinmiyorsa_null()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "hicbiri");
        using var db = vt.Olustur(k);
        Parti(db, k, 1, hasBilinir: false);
        await db.SaveChangesAsync();
        await Sat(db, k, 1_500m);

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal(500m, D(r, "karTl"));
        Assert.Equal("HESAPLANAMADI", S(r, "hasKarDurumu"));
        Assert.True(Bos(r, "karHas"));
        Assert.True(Bos(r, "maliyetHas"));
        Assert.Equal(0m, D(r, "bilinenHasilatHas"));
        Assert.Equal(0.2197m, D(r, "maliyetiEksikHasilatHas"));
        Assert.Equal(1, r.GetProperty("yalnizHasMaliyetiEksikAdedi").GetInt32());
    }

    // ── ÜRÜN SATIRI: aynı kural (KISMI: 0,0732; HİÇBİRİ: null)
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MI04_Urun_satiri_has_bacagi_ayni_kural(bool kismi)
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, kismi ? "urun-kismi" : "urun-hic");
        using var db = vt.Olustur(k);
        Parti(db, k, 2, hasBilinir: false);
        if (kismi) Parti(db, k, 1, hasBilinir: true);
        await db.SaveChangesAsync();
        await Sat(db, k, 1_500m);
        if (kismi) await Sat(db, k, 1_500m);

        var r = J(await new KarZararServisi(db).UrunBazli(k.Islem, Bugun, Bugun, null, 50));
        var s = r.GetProperty("satirlar").EnumerateArray().Single(x => S(x, "urunId") == k.UrunId);
        Assert.Equal(1, s.GetProperty("hasMaliyetiEksikSatisAdedi").GetInt32());
        if (kismi)
        {
            Assert.Equal("KISMI", S(s, "hasKarDurumu"));
            Assert.Equal(0.1465m, D(s, "maliyetHas"));
            Assert.Equal(0.0732m, D(s, "karHas"));
            Assert.Equal(1_000m, D(s, "karTl"));
        }
        else
        {
            Assert.Equal("HESAPLANAMADI", S(s, "hasKarDurumu"));
            Assert.True(Bos(s, "karHas"));
            Assert.True(Bos(s, "maliyetHas"));
            Assert.Equal(500m, D(s, "karTl"));
        }
    }

    // ── AYLIK: maliyeti eksik iade sayısı Rapor merkezi özetine taşınır (bant "2 satış ve 1 iade")
    [Fact]
    public async Task MI05_Aylik_maliyeti_eksik_iade_adedi_tasinir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "aylik-iade");
        using var db = vt.Olustur(k);
        var eksik = await Sat(db, k, 2_000m);   // partisiz → maliyeti eksik
        await Satis(db).SatisIade(new MSatis.IadeDto
        {
            KaynakHareketId = eksik, Adet = 1m, OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId,
        }, k.Islem);
        var b = Bugun;
        var r = J(await TestKurulum.RaporServisi(db, new KarZararServisi(db)).AylikOzet(b.Year, b.Month, k.Islem));
        Assert.Equal(1, r.GetProperty("maliyetiEksikIadeAdedi").GetInt32());
        Assert.True(r.GetProperty("maliyetiEksikSatisAdedi").GetInt32() >= 1);
    }
}

// ── SAF: tek yardımcı — TL kuralının has karşılığı
public sealed class MadenIkiSafTestleri
{
    [Fact]
    public void MI06_HasMaliyetiBilinenler_TL_kurali_ile_ayni_yardimci()
    {
        var hareketler = new (string, decimal?)[] { ("a", 1m), ("b", 1m), ("c", 2m), ("d", 1m), ("e", 1m) };
        var tuketim = new (string?, decimal, decimal?, decimal?)[]
        {
            ("a", 1m, 1_000m, 0.1465m),     // TL + has → bilinen
            ("b", 1m, 1_000m, null),        // has yok → bilinmiyor (eski hata: 0 sayılıyordu)
            ("c", 1m, 1_000m, 0.1465m),     // adedin yarısı → bilinmiyor
            ("d", 1m, null, 0.1465m),       // TL yoksa has da bilinmiyor sayılır
            ("e", 0.5m, 1_000m, 0.1m), ("e", 0.5m, 1_000m, null),   // bir satırı eksik → bilinmiyor
        };
        var bilinen = KarZararServisi.HasMaliyetiBilinenler(hareketler, tuketim);
        Assert.Equal(["a"], bilinen.Order().ToArray());
    }

    [Fact]
    public void MI07_EksikSayi_metni_satis_iade_birlikte()
    {
        Assert.Equal("2 satış ve 1 iade", KarRaporSozlugu.EksikSayi(KarRaporSozlugu.Tr, 2, 1));
        Assert.Equal("1 iade", KarRaporSozlugu.EksikSayi(KarRaporSozlugu.Tr, 0, 1));
        Assert.Equal("3 satış", KarRaporSozlugu.EksikSayi(KarRaporSozlugu.Tr, 3, 0));
        Assert.Equal("2 sale(s) and 1 return(s)", KarRaporSozlugu.EksikSayi(KarRaporSozlugu.En, 2, 1));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// EKRAN (bUnit) — Kâr/Zarar: has kısmiliği + konsinye notu sözlükten (EN)
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class MadenIkiKarZararEkranTestleri : BunitContext
{
    private string ozet = MadenAyrimiKarZararEkranTestleri.OzetTam;
    private string stok = MadenAyrimiKarZararEkranTestleri.StokBilinmiyor;

    private string urun = """{"satirlar":[],"urunSayisi":0,"limit":50,"dahaFazlasiVar":false,"guvenilirlik":"VERI_YOK"}""";

    public MadenIkiKarZararEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => ozet, () => stok, () => urun)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<RaporServisi>();
    }

    private sealed class SahteApi(Func<string> ozet, Func<string> stok, Func<string> urun) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.Contains("kar-zarar/urun") => urun(),
                var y when y.Contains("kar-zarar") => ozet(),
                var y when y.Contains("stok-degerlemesi") => stok(),
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.KarZarar> Ciz(bool basit)
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-panel-stok .stat-izgara")));
        return c;
    }

    private static List<(string Etiket, string Deger, string Alt)> Kartlar(AngleSharp.Dom.IElement izgara) =>
        izgara.QuerySelectorAll(".stat-kart").Select(k => (
            k.QuerySelector(".stat-etiket")!.TextContent.Trim(),
            k.QuerySelector(".stat-deger")!.TextContent.Trim(),
            k.QuerySelector(".stat-alt")?.TextContent.Trim() ?? "")).ToList();

    // [KRİTİK] TL TAM iken has KISMI: has kartları "Bilinen hasılat" + eksik has hasılatı alt satırda; TL kartı etkilenmez;
    //          TL'si bilinip has'ı bilinmeyen altın satışı ayrı not ile söylenir (iki modda).
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MI10_Has_bacagi_kendi_kismiligini_gosterir_TL_TAM_iken(bool basit)
    {
        ozet = MadenAyrimiKarZararEkranTestleri.OzetTam
            .Replace("\"bilinenHasilatHas\":117.595,\"maliyetiEksikHasilatHas\":0",
                     "\"bilinenHasilatHas\":60,\"maliyetiEksikHasilatHas\":57.595", StringComparison.Ordinal)
            .Replace("\"maliyetHas\":98.443,\"karHas\":19.152", "\"maliyetHas\":50,\"karHas\":10", StringComparison.Ordinal)
            .Replace("\"uyari\":null}", "\"uyari\":null,\"hasKarDurumu\":\"KISMI\",\"hasMaliyetiEksikSatisAdedi\":1,\"yalnizHasMaliyetiEksikAdedi\":1}", StringComparison.Ordinal);
        Assert.Contains("\"hasKarDurumu\":\"KISMI\"", ozet, StringComparison.Ordinal);
        var kr = KarRaporSozlugu.Tr;
        var c = Ciz(basit);
        var izgaralar = c.FindAll("#kz-panel-ozet .stat-izgara");
        var tl = Kartlar(izgaralar[0]);
        var has = Kartlar(izgaralar[1]);
        Assert.Equal(Sozlukler.Getir("tr").KarZarar.Hasilat, tl[0].Etiket);   // TL bacağı TAM — bilinen etiketi yok
        Assert.Equal(kr.BilinenHasilat, has[0].Etiket);
        Assert.Equal("60,0000 has", has[0].Deger);
        Assert.Equal(string.Format(kr.ToplamHasilatAlt, "117,5950 has", "57,5950 has"), has[0].Alt);
        Assert.Equal(kr.MaliyetYalnizBilinen, has[1].Alt);
        Assert.Equal("10,0000 has", has[2].Deger);
        // RAPOR-GORUNUM (hasım DÜŞÜK): 1 adette tekil kalıp ("bu satış"), "(has)" eki yok.
        Assert.Equal(KarRaporSozlugu.HasYalnizEksik(kr, 1), c.Find("#kz-has-maliyet-eksik").TextContent.Trim());
        Assert.Empty(c.FindAll("#kz-maliyet-eksik"));                             // TL eksik kutusu yok (TL TAM)
    }

    [Fact]
    public void MI11_Has_TAM_iken_has_notu_yok_etiket_hasilat()
    {
        var c = Ciz(true);
        var has = Kartlar(c.FindAll("#kz-panel-ozet .stat-izgara")[1]);
        // PAKET RAPOR-4 (VERİ 3): has bacağının ilk kartı ne olduğunu söyler (Panel'in defter gramıyla karışmasın).
        Assert.Equal(KarRaporSozlugu.Tr.HasilatAltinKarsiligi, has[0].Etiket);
        Assert.Empty(c.FindAll("#kz-has-maliyet-eksik"));
    }

    // [DÜŞÜK, UX-S31] konsinye notu sunucunun Türkçe metni DEĞİL, sözlükten (EN arayüzde İngilizce)
    [Theory]
    [InlineData("tr")]
    [InlineData("en")]
    public async Task MI12_Stok_konsinye_notu_sozlukten_iki_dilde(string dil)
    {
        stok = """
        {"subeId":null,"toplamAdet":2,"toplamHasGram":3.206,"altinHasGram":3.206,
         "madenKirilimi":[{"maden":"ALTIN","madenAdi":"Altın","adet":2,"toplamGram":3.206,"fizikselGram":3.5}],
         "maliyetliAdet":2,"maliyetsizAdet":0,"stokMaliyetiTl":2000,"stokMaliyetiHas":0.29,"stokMaliyetiKismi":false,
         "guvenilirlik":"TAM","kaynakDagilimi":[],"konsinyeUrunSayisi":3,
         "uyari":"3 konsinye ürün stok değerine DAHİL EDİLMEDİ — bu mallar işletmenin varlığı değildir."}
        """;
        if (dil == "en") await Services.GetRequiredService<DilServisi>().DegistirAsync("en");
        var c = Ciz(false);
        var not = c.Find("#kz-stok-konsinye").TextContent.Trim();
        Assert.Equal(string.Format(KarRaporSozlugu.Al(dil).StokKonsinyeNot, 3), not);
        Assert.DoesNotContain("DAHİL EDİLMEDİ", c.Markup, StringComparison.Ordinal);
    }

    // [DÜŞÜK] stok rozeti / katlanır kart sırasına yapışık değil: ortak boşluk sınıfı (mt-2)
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MI13_Stok_rozeti_ve_katlanir_kartlardan_ayrik(bool basit)
    {
        var c = Ciz(basit);
        var hedef = basit ? c.Find("#kz-stok-nereden") : c.Find("#kz-stok-guven");
        Assert.Contains("mt-2", hedef.ClassList);
    }

    // [DÜŞÜK, tur 2] Ürünler sekmesi has kâr hücresi has kısmiliğini söyler: KISMI → "kısmi" rozeti +
    //   katılmayan satış sayısı; HESAPLANAMADI → "—" açıklamasız kalmaz; TAM ve gümüş satırı not taşımaz (TR+EN).
    [Theory]
    [InlineData("tr")]
    [InlineData("en")]
    public async Task MI14_Urunler_has_kar_hucresi_kismi_rozeti_ve_hesaplanamadi_notu(string dil)
    {
        urun = """
        {"satirlar":[
          {"urunId":"u1","kod":"M2-T","ad":"Tam Bilezik","satisAdedi":1,"iadeAdedi":0,"maden":"ALTIN",
           "hasilatTl":21000,"maliyetTl":15000,"bilinenHasilatTl":21000,"karTl":6000,"karDurumu":"TAM",
           "maliyetiEksikSatisAdedi":0,"maliyetiEksikTutarTl":0,"hasilatHas":3.1711,"maliyetHas":2.2651,"karHas":0.906,
           "karMarjiYuzde":28.57,"maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM","hasKarDurumu":"TAM","hasMaliyetiEksikSatisAdedi":0},
          {"urunId":"u2","kod":"M2-K","ad":"Kismi Bilezik","satisAdedi":3,"iadeAdedi":0,"maden":"ALTIN",
           "hasilatTl":63000,"maliyetTl":45000,"bilinenHasilatTl":63000,"karTl":18000,"karDurumu":"TAM",
           "maliyetiEksikSatisAdedi":0,"maliyetiEksikTutarTl":0,"hasilatHas":9.5133,"maliyetHas":2.2651,"karHas":0.906,
           "karMarjiYuzde":28.57,"maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM","hasKarDurumu":"KISMI","hasMaliyetiEksikSatisAdedi":2},
          {"urunId":"u3","kod":"M2-H","ad":"Hic Bilezik","satisAdedi":1,"iadeAdedi":0,"maden":"ALTIN",
           "hasilatTl":21000,"maliyetTl":15000,"bilinenHasilatTl":21000,"karTl":6000,"karDurumu":"TAM",
           "maliyetiEksikSatisAdedi":0,"maliyetiEksikTutarTl":0,"hasilatHas":3.1711,"maliyetHas":null,"karHas":null,
           "karMarjiYuzde":28.57,"maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM","hasKarDurumu":"HESAPLANAMADI","hasMaliyetiEksikSatisAdedi":1},
          {"urunId":"u4","kod":"M2-G","ad":"Gumus Yuzuk","satisAdedi":1,"iadeAdedi":0,"maden":"GUMUS",
           "hasilatTl":3000,"maliyetTl":2000,"bilinenHasilatTl":3000,"karTl":1000,"karDurumu":"TAM",
           "maliyetiEksikSatisAdedi":0,"maliyetiEksikTutarTl":0,"hasilatHas":null,"maliyetHas":null,"karHas":null,
           "karMarjiYuzde":33.33,"maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM","hasKarDurumu":null,"hasMaliyetiEksikSatisAdedi":0}],
         "urunSayisi":4,"limit":50,"dahaFazlasiVar":false,"guvenilirlik":"TAM"}
        """;
        if (dil == "en") await Services.GetRequiredService<DilServisi>().DegistirAsync("en");
        var kr = KarRaporSozlugu.Al(dil);
        var c = Ciz(false);
        c.WaitForAssertion(() => Assert.Contains("Kismi Bilezik", c.Find("#kz-panel-urunler").TextContent, StringComparison.Ordinal));
        AngleSharp.Dom.IElement Satir(string ad) => c.FindAll("#kz-panel-urunler tbody tr").Single(t => t.TextContent.Contains(ad, StringComparison.Ordinal));
        AngleSharp.Dom.IElement HasHucre(string ad) => Satir(ad).QuerySelectorAll("td")[6];

        var kismi = HasHucre("Kismi Bilezik");
        Assert.Contains("0,9060 has", kismi.TextContent, StringComparison.Ordinal);
        var kNot = kismi.QuerySelector(".kz-urun-has-kismi");
        Assert.NotNull(kNot);
        Assert.Equal(kr.HasKismiRozet, kNot!.QuerySelector(".rozet")!.TextContent.Trim());
        Assert.Contains(string.Format(kr.HasKismiSatirNot, 2), kNot.TextContent, StringComparison.Ordinal);

        var hic = HasHucre("Hic Bilezik");
        Assert.Contains("—", hic.TextContent, StringComparison.Ordinal);
        // RAPOR-GORUNUM (tasarım DÜŞÜK): TL maliyeti bilinen satırda "Altın maliyeti bilinmiyor" çelişkiydi → "Alış günü altın fiyatı yok".
        Assert.Equal(kr.HasFiyatYokSatirNot, hic.QuerySelector(".kz-urun-has-hesaplanamadi")!.TextContent.Trim());
        Assert.Null(hic.QuerySelector(".kz-urun-has-kismi"));

        foreach (var ad in new[] { "Tam Bilezik", "Gumus Yuzuk" })
        {
            Assert.Null(HasHucre(ad).QuerySelector(".kz-urun-has-kismi"));
            Assert.Null(HasHucre(ad).QuerySelector(".kz-urun-has-hesaplanamadi"));
        }
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// EKRAN (bUnit) — Rapor merkezi: eksik iade bandı · stok değeri altın toplamı ·
// bağlantı rengi yok · reyon fiziksel (saf) · ölü stok boş metni + başlık
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class MadenIkiRaporlarEkranTestleri : BunitContext
{
    private const string StokDegeri = """
    {"urunler":[{"urunId":"g1","kod":"G-1","ad":"925 Ayar Gümüş Yüzük Seti","ayarId":"G925","maden":"GUMUS",
                 "adet":3,"blokeAdet":0,"satilabilirAdet":3,"toplamHas":66.6,"fizikselGram":72},
                {"urunId":"a1","kod":"A-1","ad":"22 Ayar Bilezik","ayarId":"K22","maden":"ALTIN",
                 "adet":1,"blokeAdet":0,"satilabilirAdet":1,"toplamHas":1203.206,"fizikselGram":1300}],
     "toplamHas":1203.206,"toplamBlokeAdet":0,"toplamSatir":2,"ofset":0,"limit":500,"dahaFazlasiVar":false}
    """;

    private const string ReyonStok = """
    {"reyonlar":[{"reyonId":"r1","reyonAdi":"Vitrin 1","tip":"VITRIN","aktif":true,"sorumluKullaniciId":null,
                  "subeId":"s1","subeAdi":"Merkez","adet":3,"urunSayisi":1,
                  "madenler":[{"maden":"GUMUS","madenAdi":"Gümüş","adet":3,"toplamGram":66.6,"fizikselGram":72}],
                  "altinHasGram":0}],
     "atanmamis":[],"toplamAltinHasGram":0,"uyari":null}
    """;

    private const string YaslandirmaOlusuz = """
    {"olculduAn":"2026-10-02T06:00:00Z","subeId":null,
     "kovalar":[{"kod":"G0_30","altSinirGun":0,"ustSinirGun":30,"adet":1,"hasGram":3.206,"altinHasGram":3.206,"maliyetTl":0,"maliyetiBilinmeyenAdet":1,
                 "madenKirilimi":[{"maden":"ALTIN","madenAdi":"Altın","adet":1,"toplamGram":3.206,"fizikselGram":3.5}]}],
     "toplamAdet":1,"altinHasGram":3.206,"madenDagilimi":[{"maden":"ALTIN","madenAdi":"Altın","adet":1,"toplamGram":3.206}],
     "yasiBilinmeyenAdet":0,
     "devir":{"pencereGun":90,"tuketilenAdet":1,"kalanAdet":1,"devirHizi":1,"yillikTahmini":4,"ortalamaRafOmruGun":90,"yontem":"PARTI_TUKETIMI","tanim":"t"},
     "oluStok":{"esikGun":180,"toplamSatir":0,"toplamAdet":0,"ofset":0,"limit":100,"dahaFazlasiVar":false,"urunler":[]},
     "guvenilirlik":"DUSUK","uyarilar":[]}
    """;

    private string aylik = KarRaporRaporlarEkranTestleri.AylikHesaplanamadi;

    public MadenIkiRaporlarEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => aylik)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi(Func<string> aylik) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/aylik") => aylik(),
                var y when y.EndsWith("/raporlar/cari-yaslandirma") =>
                    """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""",
                var y when y.EndsWith("/raporlar/stok-degeri") => StokDegeri,
                var y when y.EndsWith("/raporlar/reyon-stok") => ReyonStok,
                var y when y.EndsWith("/raporlar/stok-yaslandirma") => YaslandirmaOlusuz,
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") => """
                    {"tarih":"2026-10-02",
                     "tahsilat":{"nakit":0,"kart":0,"havale":0,"diger":0,"toplam":0},
                     "satis":{"sayisi":0,"toplamTl":0,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":0,"faturasizTl":0},
                     "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
                     "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
                     "fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},
                     "fark":0,"mutabik":true,"uyarilar":[],"faturasizIslemler":[],"faturaEslesme":null}
                    """,
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/kullanicilar") => "[]",
                var y when y.EndsWith("/kasalar") => "[]",
                var y when y.EndsWith("/reyonlar") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    // [DÜŞÜK] Özet bandı maliyetsiz iadeyi de anar: "Maliyeti eksik 2 satış ve 1 iade — kâra katılmadı"
    [Fact]
    public void MI20_Ozet_bandi_eksik_iadeyi_anar()
    {
        aylik = KarRaporRaporlarEkranTestleri.AylikHesaplanamadi
            .Replace("\"karTl\":null", "\"karTl\":1100.00", StringComparison.Ordinal)
            .Replace("\"karDurumu\":\"HESAPLANAMADI\"", "\"karDurumu\":\"KISMI\"", StringComparison.Ordinal)
            .Replace("\"maliyetiEksikSatisAdedi\":9,\"maliyetiEksikTutarTl\":195069.79",
                     "\"maliyetiEksikSatisAdedi\":2,\"maliyetiEksikIadeAdedi\":1,\"maliyetiEksikTutarTl\":18900.00", StringComparison.Ordinal);
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#rp-kar-eksik-metin")));
        Assert.Equal("Maliyeti eksik 2 satış ve 1 iade — kâra katılmadı", e.Find("#rp-kar-eksik-metin").TextContent.Trim());
    }

    [Fact]
    public void MI21_Ozet_bandi_yalniz_iade_eksikken_de_acilir()
    {
        aylik = KarRaporRaporlarEkranTestleri.AylikHesaplanamadi
            .Replace("\"karTl\":null", "\"karTl\":1100.00", StringComparison.Ordinal)
            .Replace("\"karDurumu\":\"HESAPLANAMADI\"", "\"karDurumu\":\"KISMI\"", StringComparison.Ordinal)
            .Replace("\"maliyetiEksikSatisAdedi\":9,\"maliyetiEksikTutarTl\":195069.79",
                     "\"maliyetiEksikSatisAdedi\":0,\"maliyetiEksikIadeAdedi\":1,\"maliyetiEksikTutarTl\":-500.00", StringComparison.Ordinal);
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#rp-kar-eksik-metin")));
        Assert.Equal("Maliyeti eksik 1 iade — kâra katılmadı", e.Find("#rp-kar-eksik-metin").TextContent.Trim());
    }

    // [ORTA] Stok değeri: altın toplamı ayrı satırda (binlik + 4 hane), değerler bağlantı renginde değil; reyon "fiziksel (saf)"
    [Fact]
    public void MI22_Stok_degeri_altin_toplam_satiri_renk_ve_reyon_fiziksel()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#rapor-stok-altin-toplam")));
        var toplam = e.Find("#rapor-stok-altin-toplam");
        // PAKET GOC-D: stok listesi ortak ızgarada; maden toplamları (eski tfoot) ızgaranın ALTINDA aynı kimlikle — ad/değer ayrı öğe.
        Assert.Equal(KarRaporSozlugu.Tr.StokAltinHas, toplam.QuerySelector(".izgara-alt-toplam-ad")!.TextContent.Trim());
        Assert.Equal("1.203,2060 has", toplam.QuerySelector(".izgara-alt-toplam-deger")!.TextContent.Trim());
        var tablo = e.Find("#rapor-stok-tablo");
        Assert.Empty(tablo.QuerySelectorAll(".metin-amber"));
        Assert.Contains("72,00 g (saf 66,60 g)", tablo.TextContent, StringComparison.Ordinal);
        e.WaitForAssertion(() => Assert.Contains("Gümüş: 72,00 g (saf 66,60 g)", e.Find("#reyon-stok-kart").TextContent, StringComparison.Ordinal));
        Assert.Empty(e.FindAll("#reyon-stok-kart .metin-amber"));
        Assert.Empty(e.FindAll("#rapor-maden-dagilimi td.metin-amber"));
    }

    // [DÜŞÜK] Ölü stok boş metni dönemden bağımsız; kova ve devir ızgaraları arasında boşluk
    [Fact]
    public void MI23_Olu_stok_bos_metni_ve_izgara_boslugu()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sy-olu-bos")));
        Assert.Contains(string.Format(KarRaporSozlugu.Tr.OluStokYok, 180), e.Find("#sy-olu-bos").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain(Sozlukler.Getir("tr").RaporSeti.BosDurum, e.Find("#sy-olu-bos").TextContent, StringComparison.Ordinal);
        var izgaralar = e.FindAll("#stok-yaslandirma-kart .stat-izgara");
        Assert.Contains("mt-2", izgaralar[1].ClassList);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// EKRAN (bUnit) — Panel "Bugün ne sattım?" kâr satırı: eksik iade de söylenir
// ═════════════════════════════════════════════════════════════════════════════
public sealed class MadenIkiPanelEkranTestleri : BunitContext
{
    private string kar = """
        {"hasilatTl":1500.00,"maliyetTl":1000,"karTl":500.00,"karDurumu":"KISMI","karMarjiYuzde":null,
         "hasilatHas":0.2,"maliyetHas":0.1,"karHas":0.1,"satisAdedi":2,
         "maliyetiEksikSatisAdedi":0,"maliyetiEksikIadeAdedi":1,"maliyetiEksikTutarTl":-500.00,
         "maliyetiBilinenAdet":1,"maliyetiBilinmeyenAdet":2,"guvenilirlik":"ORTA","uyari":"x"}
        """;

    private const string Gunluk = """
        {"tarih":"2026-10-02","satis":{"adet":2,"toplamTl":1500.00,"toplamHas":0.2},
         "alis":{"adet":0,"toplamTl":0,"toplamHas":0},
         "hurda":{"toplamHas":0,"odenenTl":0},"netHasGramHareketi":0.2,"hareketSayisi":3,
         "kasalar":[{"id":"k1","ad":"Nakit","bakiye":1500.00,"paraBirimi":"TRY"}]}
        """;

    public MadenIkiPanelEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => kar)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
        Services.AddScoped<HavaServisi>();
        Services.BugunServisleri();   // TEMA TEMİZLİĞİ 17.10: Panel = Yıldız Bugün (YetkiServisi)
    }

    private sealed class SahteApi(Func<string> kar) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var cevap = yol switch
            {
                var y when y.EndsWith("/raporlar/gunluk", StringComparison.Ordinal) => Gunluk,
                var y when y.EndsWith("/raporlar/cari-yaslandirma", StringComparison.Ordinal) =>
                    """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""",
                var y when y.EndsWith("/raporlar/vade-takibi", StringComparison.Ordinal) =>
                    """{"ufukGun":7,"gecikenToplam":0,"yaklasanToplam":0,"vadesizToplam":0,"ufukOtesiToplam":0,"acikToplam":0,"cariler":[]}""",
                var y when y.EndsWith("/raporlar/aylik", StringComparison.Ordinal) => """{"gunler":[]}""",
                var y when y.Contains("/raporlar/islem-defteri", StringComparison.Ordinal) => """
                    {"baslangic":"2026-10-02","bitis":"2026-10-02","toplamSatir":0,"ofset":0,"limit":5,
                     "dahaFazlasiVar":false,"ozet":{"tlGiris":0,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":0},
                     "storno":{"adet":0,"tutarTl":0},"satirlar":[]}
                    """,
                var y when y.EndsWith("/bildirimler", StringComparison.Ordinal) =>
                    """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""",
                var y when y.EndsWith("/raporlar/kar-zarar", StringComparison.Ordinal) => kar(),
                var y when y.EndsWith("/bankalar", StringComparison.Ordinal) => """{"hesaplar":[]}""",
                var y when y.EndsWith("/kasalar/nakit-ayrimi", StringComparison.Ordinal) =>
                    """[{"kasaId":"k1","paraBirimi":"TRY","toplam":1500.00,"nakit":1500.00,"kart":0,"havale":0}]""",
                var y when y.EndsWith("/pozisyon", StringComparison.Ordinal) => """
                    {"anaMaden":"ALTIN","altin":{"maden":"ALTIN","vitrinGram":1312.8,"hurdaGram":0,"cariAlacakGram":0,"cariBorcGram":0,
                     "netGram":1312.8,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},
                     "digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,"hesaplanmaAni":"2026-10-02T09:00:00Z","donemdenBagimsiz":true}
                    """,
                var y when y.EndsWith("/kur/kaynaklar", StringComparison.Ordinal) =>
                    """{"kaynaklar":["ALTINKAYNAK"],"secili":"ALTINKAYNAK","varsayilan":"ALTINKAYNAK"}""",
                var y when y.Contains("/tercihler/benim", StringComparison.Ordinal) => """{"tercihler":{}}""",
                var y when y.Contains("/uyum/", StringComparison.Ordinal) =>
                    """{"suresiGecmis":0,"yaklasan":0,"gecikmisSayisi":0,"yuksek":0}""",
                // TEMA TEMİZLİĞİ 17.10 (YILDIZ-4C): Bugün'ün gün sonu / emanet satırlarının uçları gerçekçi.
                var y when y.EndsWith("/raporlar/kasa-defteri", StringComparison.Ordinal) =>
                    """{"baslangic":"2026-10-01","bitis":"2026-10-01","saatDilimi":"Europe/Istanbul","utcOfsetSaat":3,"yazSaatiRiski":false,"toplamSatir":0,"kasalar":[]}""",
                var y when y.EndsWith("/cari", StringComparison.Ordinal) => "[]",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    // TEMA TEMİZLİĞİ 17.10: Panel = Yıldız Bugün — kâr alt şeridin "Kâr" öğesi (#yildiz-bugun-kar), pozisyon alt şeridin
    // "Altın pozisyonu" öğesi (#yildiz-bugun-pozisyon); eski kart ve kırılımı Bugün'de yok.
    private IRenderedComponent<Kuyumcu.Web.Pages.Panel> Basit()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1",
                ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.BugunBekle(pozisyon: true);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yildiz-bugun-kar")), TimeSpan.FromSeconds(10));
        return e;
    }

    // [ORTA] eksik yalnız iadeden geliyorsa kâr bunu söyler (eskiden düz "Kâr 500 ₺"): Bugün'de rakam BASILMAZ, "maliyet eksik".
    [Fact]
    public void MI30_Panel_kar_satiri_eksik_iadeyi_soyler()
    {
        var e = Basit();
        var k = e.Find("#yildiz-bugun-kar");
        Assert.Equal($"{YildizBugunSozlugu.Kar("tr")} {YildizBugunSozlugu.MaliyetEksik("tr")}", k.TextContent.Trim());
        Assert.DoesNotContain("500", k.TextContent, StringComparison.Ordinal);   // kısmi kâr (500 ₺) yazılmaz
        Assert.Equal("/raporlar/kar-zarar", k.GetAttribute("href"));
    }

    // [DÜŞÜK] Altın pozisyonu binlik ayraçlı. TEMA TEMİZLİĞİ 17.10: Bugün alt şeridi tuval biçimidir (bir ondalık,
    // işaretli, "g has" — YildizBugunBicim.HasKisa, Yildiz4ATestleri.Y4A_05); 4 haneli kırılım kartı Bugün'de yok.
    [Fact]
    public void MI31_Altin_pozisyonu_has_bicimi_binlik_dort_hane()
    {
        var e = Basit();
        Assert.Equal("+1.312,8 g has", e.Find("#yildiz-bugun-pozisyon b").TextContent.Trim());
        Assert.Equal("1.312,8 g", e.Find("#yildiz-bugun-vitrin b").TextContent.Trim());
        Assert.Empty(e.FindAll("#pozisyon-kart"));
    }

    // MADEN-2 tur 2 (tasarım hakemi KRİTİK — "Alacak hapı düğme gibi görünmesin"): kırılım öğeleri
    // (a Vitrin ile button Alacak) aynı görünür; dugme.css hap kenarı kimlik özgüllüğüyle ezilir.
    [Fact]
    public void MI32_Pozisyon_kirilim_ogeleri_hap_degil_ayni_gorunum()
    {
        const string secici = "#pozisyon-kart .pozisyon-kirilim-oge";
        var b = CssKaynak.Bildirimler(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css"));
        string Son(string oz) => b.Where(x => x.Ozellik == oz
                && x.Secici.Split(',').Select(p => System.Text.RegularExpressions.Regex.Replace(p, "\\s+", " ").Trim()).Contains(secici))
            .Select(x => x.Deger).LastOrDefault() ?? "(yok)";
        Assert.Equal("0", Son("border"));
        Assert.Equal("0", Son("border-radius"));
        Assert.Equal("0", Son("padding-inline"));
        Assert.Equal("dotted", Son("text-decoration-style"));
        Assert.Equal("var(--dugme-kenar)", Son("text-decoration-color"));
    }
}
