using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-69 — "toplamHas" ADLANDIRMASI ("toplam" değil "altın").
//
// QA turu 8: /raporlar/aylik satis.toplamHas ölçüldüğünde YALNIZ ALTINDIR
// (Paket 20) ama adı ve ekran etiketi "toplam has" der. Veri doğru, AD yanlış.
//   P69_01  yanıt `altinToplamHas` taşır ve madenDagilimi'nin ALTIN satırına eşittir
//   P69_02  eski `toplamHas` alanı KALDIRILMADI (şekil sözleşmesi)
//   P69_03  ekran altın dışı maden varken "Toplam Has" yazmaz; madeni söyler
//
// ⚠ Neden RaporSetiTestleri.cs değil: o dosyaya aynı dalgada P-48 dokunuyor
//   (dosya çakışma haritası); iki şerit aynı test dosyasını açmasın diye ayrı dosya.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class P69HasAdlandirmaDbTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static object Alan(object o, string ad) =>
        o.GetType().GetProperty(ad)?.GetValue(o)
        ?? throw new Xunit.Sdk.XunitException($"Yanıtta '{ad}' alanı YOK ({o.GetType().Name}).");

    /// <summary>Kiracıya gümüş ürün ekler (stoklu).</summary>
    private static string GumusEkle(KuyumcuDbContext db, Kiraci k)
    {
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();
        var ayar = Ayarlar.Bul("G999")!;
        var has = HasHesap.ToHasGram(100m, ayar);
        db.Urunler.Add(new Urun
        {
            Id = id, TenantId = k.TenantId, Tip = "GUMUS", Kod = $"P69-{id[..8]}", Ad = "P69 gümüş",
            AyarId = "G999", Milyem = ayar.Milyem, GramAgirlik = 100m, TasAgirlik = 0m,
            HasGrami = has, IscilikTl = 0m, SatisFiyati = 5_000m, Aktif = true,
            Olusturuldu = now, Guncellendi = now,
        });
        db.Stoklar.Add(new Stok
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, UrunId = id,
            Adet = 10m, ToplamHasGram = 10m * has, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    /// <summary>Altın (K22, 10 g → 9,16 has) + gümüş (100 g → 99,9 has) satışı olan kiracı.</summary>
    private async Task<Kiraci> IkiMadenliKiraci(string ad)
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, ad);
        var gumusId = GumusEkle(kurulum, k);
        using var db = vt.Olustur(k);
        var satis = Satis(db);
        await satis.Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 60_000m,
            OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
        await satis.Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = gumusId, Adet = 1m, BirimFiyatTl = 5_000m,
            OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
        return k;
    }

    /// <summary>
    /// M69-1 — aylık yanıtın satis ve alis düğümleri `altinToplamHas` taşır;
    /// satış değeri madenDagilimi'nin ALTIN satırına EŞİTTİR (9,16), gümüş
    /// (99,9) buna EKLENMEZ. Alan yanıttan çıkarılırsa KIRMIZI.
    /// </summary>
    [Fact]
    public async Task P69_01_Aylik_yanit_altinToplamHas_alanini_tasir()
    {
        var k = await IkiMadenliKiraci("PAKETP-69-TEST aylık altın");
        using var okuma = vt.Olustur(k);
        var bugun = Zaman.YerelBugun();
        var rapor = await TestKurulum.RaporServisi(okuma).AylikOzet(bugun.Year, bugun.Month, k.Islem);

        var satis = Alan(rapor, "satis");
        var altin = (decimal)Alan(satis, "altinToplamHas");
        Assert.Equal(9.160000m, altin);

        var dagilim = ((System.Collections.IEnumerable)Alan(satis, "madenDagilimi")).Cast<object>().ToList();
        Assert.Equal(2, dagilim.Count);
        var altinSatiri = Assert.Single(dagilim, d => (string)Alan(d, "maden") == Maden.Altin);
        Assert.Equal((decimal)Alan(altinSatiri, "toplamHas"), altin);
        var gumusSatiri = Assert.Single(dagilim, d => (string)Alan(d, "maden") == Maden.Gumus);
        Assert.Equal(99.900000m, (decimal)Alan(gumusSatiri, "toplamHas"));
        Assert.NotEqual(altin + 99.900000m, altin);   // toplam DEĞİL, yalnız altın

        // Alış düğümü de aynı adı taşır (bu kiracıda alış yok → 0).
        Assert.Equal(0m, (decimal)Alan(Alan(rapor, "alis"), "altinToplamHas"));
    }

    /// <summary>
    /// M69-2 — şekil sözleşmesi: eski `toplamHas` alanı aylık (satis, alis) ve
    /// günlük (satis, alis, hurda) yanıtlarında DURUYOR ve `altinToplamHas` ile
    /// AYNI değeri taşıyor. Alan yanıttan silinirse KIRMIZI.
    /// </summary>
    [Fact]
    public async Task P69_02_toplamHas_alani_KALDIRILMADI_sekil_regresyonu()
    {
        var k = await IkiMadenliKiraci("PAKETP-69-TEST şekil");
        using var okuma = vt.Olustur(k);
        var servis = TestKurulum.RaporServisi(okuma);
        var bugun = Zaman.YerelBugun();

        var aylik = await servis.AylikOzet(bugun.Year, bugun.Month, k.Islem);
        foreach (var dugum in new[] { "satis", "alis" })
        {
            var d = Alan(aylik, dugum);
            Assert.Equal((decimal)Alan(d, "altinToplamHas"), (decimal)Alan(d, "toplamHas"));
        }
        Assert.Equal(9.160000m, (decimal)Alan(Alan(aylik, "satis"), "toplamHas"));

        var gunluk = await servis.GunlukOzet(bugun.ToString("yyyy-MM-dd"), k.Islem);
        foreach (var dugum in new[] { "satis", "alis", "hurda" })
        {
            var d = Alan(gunluk, dugum);
            Assert.Equal((decimal)Alan(d, "altinToplamHas"), (decimal)Alan(d, "toplamHas"));
        }
        Assert.Equal(9.160000m, (decimal)Alan(Alan(gunluk, "satis"), "altinToplamHas"));
    }
}

public sealed class P69HasAdlandirmaEkranTestleri : BunitContext
{
    // Gerçek sunucu şekli (canlı 5185 ölçümünden): satis'te altın + gümüş, dönem
    // uyarısı sunucunun ham metniyle (içinde `toplamHas` geçer).
    private const string Aylik = """
        {"yil":2026,"ay":9,
         "satis":{"sayisi":2,"toplamTl":65000.00,"toplamHas":9.16,"altinToplamHas":9.16,
                  "madenDagilimi":[{"maden":"GUMUS","madenAdi":"Gümüş","toplamHas":99.9,"satirSayisi":1},
                                   {"maden":"ALTIN","madenAdi":"Altın","toplamHas":9.16,"satirSayisi":1}]},
         "alis":{"sayisi":0,"toplamTl":0,"toplamHas":0,"altinToplamHas":0,"madenDagilimi":[]},
         "tahsilat":{"toplamTl":65000.00},"veresiye":{"toplamTl":0},
         "karTl":null,"karKaynagi":"HESAPLANAMADI","satisAlisFarkiTl":65000.00,
         "netHasGramHareketi":-9.16,
         "madenDagilimi":[{"maden":"GUMUS","madenAdi":"Gümüş","toplamHas":-99.9,"satirSayisi":1},
                          {"maden":"ALTIN","madenAdi":"Altın","toplamHas":-9.16,"satirSayisi":1}],
         "madenUyarisi":"Bu dönemde birden çok maden var. `toplamHas` yalnız ALTIN has gramıdır; diğer madenler `madenDagilimi` altında ayrı verilir — farklı madenlerin gramları toplanmaz.",
         "hareketSayisi":2,"gunler":[]}
        """;

    private const string Stok = """
        {"urunler":[{"urunId":"u1","kod":"BLZ-1","ad":"Bilezik","ayarId":"K22","adet":2,"toplamHas":18.32},
                    {"urunId":"u2","kod":"GMS-1","ad":"Gümüş zincir","ayarId":"G999","adet":3,"toplamHas":299.7}],
         "toplamHas":18.32,
         "madenDagilimi":[{"maden":"ALTIN","madenAdi":"Altın","adet":2,"toplamHas":18.32},
                          {"maden":"GUMUS","madenAdi":"Gümüş","adet":3,"toplamHas":299.7}],
         "toplamSatir":2,"ofset":0,"limit":500,"dahaFazlasiVar":false,"uyari":null}
        """;

    private const string ReyonStok = """
        {"reyonlar":[{"reyonId":"r1","reyonAdi":"Vitrin 1","tip":"VITRIN","aktif":true,"sorumluKullaniciId":null,
                      "subeId":"s1","subeAdi":"Merkez","adet":5,"urunSayisi":2,
                      "madenler":[{"maden":"ALTIN","madenAdi":"Altın","adet":2,"toplamGram":18.32},
                                  {"maden":"GUMUS","madenAdi":"Gümüş","adet":3,"toplamGram":299.7}],
                      "altinHasGram":18.32}],
         "atanmamis":[],"toplamAltinHasGram":18.32,"uyari":null}
        """;

    public P69HasAdlandirmaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi())
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/aylik") => Aylik,
                var y when y.EndsWith("/raporlar/cari-yaslandirma") =>
                    """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""",
                var y when y.EndsWith("/raporlar/stok-degeri") => Stok,
                var y when y.EndsWith("/raporlar/reyon-stok") => ReyonStok,
                var y when y.EndsWith("/raporlar/stok-yaslandirma") =>
                    """{"olculduAn":"2026-09-22T06:00:00Z","subeId":null,"kovalar":[],"toplamAdet":0,"altinHasGram":0,"madenDagilimi":[],"yasiBilinmeyenAdet":0,"devir":null,"oluStok":null,"guvenilirlik":"YUKSEK","uyarilar":[]}""",
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

    /// <summary>
    /// M69-3 — dönemde gümüş varken /raporlar ekranında "Toplam Has" ibaresi
    /// (eski Urun.ToplamHas anahtarı ve eski maden uyarısı) GÖRÜNMEZ; yerine
    /// madeni söyleyen etiketler görünür. Etiket eski anahtara dönerse KIRMIZI.
    /// </summary>
    [Fact]
    public void P69_03_Ekran_altin_disi_maden_varken_Toplam_Has_yazmaz()
    {
        var dil = Services.GetRequiredService<DilServisi>();
        var h = RaporHasSozlugu.Al(dil.Dil);
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#rapor-maden-dagilimi")));
        // ZO-BELGE (27.09): maden tablosu KAPALI katlanırda — açılınca aynı içerik.
        e.Find("#rapor-maden-dagilimi-ac").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#rapor-stok-has-baslik")));
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#reyon-stok-has-baslik")));

        var markup = e.Markup;
        // Yasak ibare: eski sözlük değeri ("Toplam Has") ve her yazılışı.
        Assert.DoesNotContain(dil.T.Urun.ToplamHas, markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Toplam Has", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("toplamHas", markup, StringComparison.Ordinal);   // ham alan adı (UX-S31)

        // Yerine madeni söyleyen etiketler:
        // RAPOR-GORUNUM (tasarım DÜŞÜK): "Net has gram hareketi — Has (altın)" 'has'ı iki kez söylüyordu →
        // tek ifade "Net altın hareketi (has)" — madeni (altın) yine söyler, "Toplam Has" yasağı aynen.
        // PAKET RAPOR-3: etiket kapsamı da söyler — "Ayın tüm altın hareketi (açılış, sayım, emanet dâhil)" (madeni yine söyler).
        Assert.Contains(KarRaporSozlugu.Al(dil.Dil).AyinTumAltinHareketi, markup, StringComparison.Ordinal);
        Assert.DoesNotContain(h.NetHasAltin, markup, StringComparison.Ordinal);
        // MADEN-AYRIMI tur 2 (tasarım hakemi): sütunda gümüş de durur → başlık 'has' demez,
        // madenden bağımsız "Miktar (madenine göre)" (hücre birimi madenden: altın has, gümüş saf g).
        var kr = KarRaporSozlugu.Al(dil.Dil);
        Assert.Equal(kr.MiktarMadenineGore, e.Find("#rapor-maden-has-baslik").TextContent.Trim());
        Assert.Equal(kr.MiktarMadenineGore, e.Find("#rapor-stok-has-baslik").TextContent.Trim());
        Assert.Equal(kr.MiktarMadenineGore, e.Find("#reyon-stok-has-baslik").TextContent.Trim());
        Assert.DoesNotContain("Has gram (", markup, StringComparison.Ordinal);
        Assert.Equal(h.MadenUyarisi, e.Find("#rapor-maden-uyari").TextContent.Trim());

        // Maden tablosu gümüş satırını AYRI basar (toplanmaz).
        var tablo = e.Find("#rapor-maden-dagilimi").TextContent;
        Assert.Contains("Gümüş", tablo, StringComparison.Ordinal);
        Assert.Contains("Altın", tablo, StringComparison.Ordinal);
    }

    /// <summary>Sözlük TR + EN: "Has (altın)" etiketi iki dilde de madeni söyler.</summary>
    [Fact]
    public void P69_04_Sozluk_TR_EN_madeni_soyler()
    {
        Assert.NotEqual(RaporHasSozlugu.Tr.NetHasAltin, RaporHasSozlugu.En.NetHasAltin);
        Assert.Contains("altın", RaporHasSozlugu.Tr.HasAltin, StringComparison.Ordinal);
        Assert.Contains("gold", RaporHasSozlugu.En.HasAltin, StringComparison.Ordinal);
    }
}
