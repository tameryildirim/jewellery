using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Maliyet;
using StokYaslandirmaServisi = Kuyumcu.Api.Modules.Rapor.StokYaslandirmaServisi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET RAPOR-GORUNUM (03.10, t1) — rapor ekranları kalanları.
//   Veri (a) Panel / Rapor merkezi "Bugün ne sattım" TEK has kaynağı ve biçimi
//   Veri (b) pos-fatura-mutabakati hurda.hasGrami YALNIZ ALTIN, diğer madenler ayrı
//   Veri (c) Stok yaşı: "raf ömrü" bir TAHMİNDİR; gerçek ortalama yaş ayrı alan
//   Veri (d) Kâr-zarar grafiği: hasılat çubuğu kârla AYNI kümeden (bilinen satış)
//   Görünüm: 375 kart yığını, eksen yazıları, tek cümlelik düzeltme notu, tekil/çoğul, sıra.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class RaporGorunumDbTestleri(TestVeritabani vt)
{
    private static JsonElement J(object o) => JsonDocument.Parse(JsonSerializer.Serialize(o)).RootElement;

    private static Hareket Satir(Kiraci k, string tur, decimal tl, decimal has, string maden, DateTime? tarih = null) => new()
    {
        Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = tur,
        Tarih = tarih ?? DateTime.UtcNow, TlTutari = tl, HasGrami = has, Maden = maden,
        ParaBirimi = "TRY", KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId, Olusturuldu = DateTime.UtcNow,
    };

    private static StokPartisi Parti(Kiraci k, DateTime giris, decimal kalan) => new()
    {
        Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, UrunId = k.UrunId,
        Kaynak = PartiKaynagi.Alis, AdetGiren = kalan * 2m, AdetKalan = kalan, HasGramBirim = 1m,
        BirimMaliyetTl = 1_000m, GirisTarihi = giris, Olusturuldu = giris,
    };

    // ── RG01 · veri (b): mutabakat hurda has gramı YALNIZ ALTIN; gümüş madenDagilimi'nde AYRI ──
    [Fact]
    public async Task RG01_Mutabakat_hurda_has_grami_yalniz_altin_gumus_ayri()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-GORUNUM-TEST rg01");
        sistem.Add(Satir(k, "HURDA_ALIS", -10_000m, 9.16m, "ALTIN"));
        sistem.Add(Satir(k, "HURDA_ALIS", -2_000m, 92.5m, "GUMUS"));
        await sistem.SaveChangesAsync();

        using var db = vt.Olustur(k);
        var m = J(await TestKurulum.RaporServisi(db).PosFaturaMutabakati(
            Zaman.YerelBugun().ToString("yyyy-MM-dd"), k.Islem));
        var h = m.GetProperty("hurda");
        Assert.Equal(2, h.GetProperty("sayisi").GetInt32());
        Assert.Equal(9.16m, h.GetProperty("hasGrami").GetDecimal());          // eskiden 101,66 (altın + gümüş)
        Assert.Equal(9.16m, h.GetProperty("altinHasGrami").GetDecimal());
        var dagilim = h.GetProperty("madenDagilimi").EnumerateArray().ToList();
        Assert.Equal(2, dagilim.Count);
        Assert.Equal("ALTIN", dagilim[0].GetProperty("maden").GetString());   // altın önce
        var gumus = dagilim.Single(x => x.GetProperty("maden").GetString() == "GUMUS");
        Assert.Equal(92.5m, gumus.GetProperty("toplamHas").GetDecimal());
        Assert.Equal(1, gumus.GetProperty("satirSayisi").GetInt32());
    }

    // ── RG02 · veri (a): gunluk satis.toplamHas YALNIZ ALTIN (Panel ve Rapor merkezi bu alanı okur) ──
    [Fact]
    public async Task RG02_Gunluk_satis_has_yalniz_altin_iki_ekranin_tek_kaynagi()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-GORUNUM-TEST rg02");
        sistem.Add(Satir(k, "SATIS", 30_000m, -4.25m, "ALTIN"));
        sistem.Add(Satir(k, "SATIS", 12_000m, -1.8088m, "ALTIN"));
        sistem.Add(Satir(k, "SATIS", 3_000m, -50m, "GUMUS"));
        await sistem.SaveChangesAsync();

        using var db = vt.Olustur(k);
        var g = J(await TestKurulum.RaporServisi(db).GunlukOzet(Zaman.YerelBugun().ToString("yyyy-MM-dd"), k.Islem));
        var s = g.GetProperty("satis");
        Assert.Equal(6.0588m, s.GetProperty("toplamHas").GetDecimal());
        Assert.Equal(s.GetProperty("toplamHas").GetDecimal(), s.GetProperty("altinToplamHas").GetDecimal());
    }

    // ── RG03 · veri (c): ortalama yaş (geriye) ≠ tahmini satış süresi (ileriye); ikisi ayrı alan ──
    [Fact]
    public async Task RG03_Stok_yasi_ortalama_yas_ve_tahmini_satis_suresi_ayri()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-GORUNUM-TEST rg03", stokAdet: 0m);
        var simdi = DateTime.UtcNow;
        var p1 = Parti(k, simdi.AddDays(-10), 2m);
        var p2 = Parti(k, simdi.AddDays(-40), 3m);
        sistem.Add(p1);
        sistem.Add(p2);
        sistem.Add(new StokPartiTuketimi
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, PartiId = p1.Id, UrunId = k.UrunId,
            Adet = 1m, BirimMaliyetTl = 1_000m, Tarih = simdi.AddDays(-5),
        });
        await sistem.SaveChangesAsync();

        using var db = vt.Olustur(k);
        var o = J(await new StokYaslandirmaServisi(db, new DenetimServisi(db, new HttpContextAccessor()))
            .Yaslandirma(k.Islem, null, devirPenceresiGun: 90));
        var d = o.GetProperty("devir");
        // Gerçek yaş: (2×10 + 3×40) ÷ 5 = 28,0 gün — kovalarla aynı partilerden.
        Assert.Equal(28.0m, d.GetProperty("ortalamaYasGun").GetDecimal());
        // Tahmin: kalan 5 × 90 ÷ tüketilen 1 = 450 gün (eski adı "ortalama raf ömrü").
        Assert.Equal(450.0m, d.GetProperty("tahminiSatisSuresiGun").GetDecimal());
        Assert.Equal(d.GetProperty("ortalamaRafOmruGun").GetDecimal(), d.GetProperty("tahminiSatisSuresiGun").GetDecimal());
    }

    [Fact]
    public async Task RG04_Stok_yasi_rafta_mal_yokken_ortalama_yas_null()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-GORUNUM-TEST rg04", stokAdet: 0m);
        using var db = vt.Olustur(k);
        var o = J(await new StokYaslandirmaServisi(db, new DenetimServisi(db, new HttpContextAccessor())).Yaslandirma(k.Islem, null));
        Assert.Equal(JsonValueKind.Null, o.GetProperty("devir").GetProperty("ortalamaYasGun").ValueKind);   // 0 DEĞİL
    }

    // ── RG05 · veri (d): karşılaştırılan dönem de bilinen hasılatı ayrı döndürür ──
    [Fact]
    public async Task RG05_Kar_zarar_onceki_donem_bilinen_hasilati_doner()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-GORUNUM-TEST rg05");
        using var db = vt.Olustur(k);
        var bugun = Zaman.YerelBugun();
        var o = J(await new KarZararServisi(db).Donem(k.Islem, bugun, bugun, null, "ONCEKI_DONEM"));
        var on = o.GetProperty("onceki");
        Assert.True(on.TryGetProperty("bilinenHasilatTl", out var bh));
        Assert.Equal(on.GetProperty("hasilatTl").GetDecimal(), bh.GetDecimal());   // veri yok → ikisi de 0
    }
}

// ── RG06 · tur 2 (hasım DÜŞÜK): Stok değeri sırası SUNUCUDA, sayfalamadan ÖNCE — altın önce, maden içinde miktar ──
[Collection("veritabani")]
public sealed class RaporGorunumStokSiraDbTestleri(TestVeritabani vt)
{
    [Fact]
    public async Task RG06_Stok_degeri_sirasi_sunucuda_altin_once_sayfalar_arasi_tutarli()
    {
        using var kurulum = vt.OlusturSistem();
        // Altın ürün: 1 adet × k.UrunHasGrami; gümüş 10 adet × 27,75 = 277,5 saf — eski sıra (miktar) gümüşü öne alırdı.
        var k = TestKurulum.KiraciKur(kurulum, "PAKETRAPOR-GORUNUM-TEST rg06", stokAdet: 1m);
        using var db = vt.Olustur(k);
        var now = DateTime.UtcNow;
        var gumusId = Kimlik.Ulid();
        db.Urunler.Add(new Urun
        {
            Id = gumusId, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"G-{gumusId[..8]}", Ad = "Gümüş yüzük",
            AyarId = "G925", Milyem = 925m, GramAgirlik = 30m, TasAgirlik = 0m, HasGrami = 27.75m,
            IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.Stoklar.Add(new Stok
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, UrunId = gumusId,
            Adet = 10m, ToplamHasGram = 277.5m, Guncellendi = now,
        });
        await db.SaveChangesAsync();

        var srv = TestKurulum.RaporServisi(db);
        var s1 = JsonDocument.Parse(JsonSerializer.Serialize(await srv.StokDegeri(k.Islem, null, 1, 0))).RootElement;
        var s2 = JsonDocument.Parse(JsonSerializer.Serialize(await srv.StokDegeri(k.Islem, null, 1, 1))).RootElement;
        var u1 = s1.GetProperty("urunler")[0];
        var u2 = s2.GetProperty("urunler")[0];
        Assert.Equal("ALTIN", u1.GetProperty("maden").GetString());      // 1. sayfa: altın (gümüşten AZ gram olsa da)
        Assert.Equal(k.UrunId, u1.GetProperty("urunId").GetString());
        Assert.Equal("GUMUS", u2.GetProperty("maden").GetString());      // 2. sayfa: gümüş
        Assert.Equal(gumusId, u2.GetProperty("urunId").GetString());
        Assert.True(u1.GetProperty("toplamHas").GetDecimal() < u2.GetProperty("toplamHas").GetDecimal());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// SAF — tek kural yardımcıları
// ═════════════════════════════════════════════════════════════════════════════
public sealed class RaporGorunumSafTestleri
{
    [Fact]
    public void RG10_Has_yalniz_eksik_notu_tekil_cogul_TR_EN()
    {
        Assert.Equal("1 altın satışının alış günü altın fiyatı yok; bu satış altın kârına katılmadı.",
            KarRaporSozlugu.HasYalnizEksik(KarRaporSozlugu.Tr, 1));
        Assert.Equal("3 altın satışının alış günü altın fiyatı yok; bu satışlar altın kârına katılmadı.",
            KarRaporSozlugu.HasYalnizEksik(KarRaporSozlugu.Tr, 3));
        Assert.DoesNotContain("(has)", KarRaporSozlugu.HasYalnizEksik(KarRaporSozlugu.Tr, 3), StringComparison.Ordinal);
        Assert.Contains("this sale is", KarRaporSozlugu.HasYalnizEksik(KarRaporSozlugu.En, 1), StringComparison.Ordinal);
        Assert.Contains("these sales are", KarRaporSozlugu.HasYalnizEksik(KarRaporSozlugu.En, 2), StringComparison.Ordinal);
    }

    [Fact]
    public void RG11_HasIsaretli_Bicim_Has_ile_ayni_sayi_dili()
    {
        Assert.Equal("26,0588 has", Bicim.HasIsaretli(26.0588m));
        Assert.Equal("−1.234,5000 has", Bicim.HasIsaretli(-1234.5m));
        Assert.Equal(Bicim.Has(171.448m), Bicim.HasIsaretli(171.448m));
    }

    // Tur 2 (hasım KRİTİK): işaret YUVARLANMIŞ değerden — "−0,0000 has" yazılmaz; birim sözlükten (EN "fine").
    [Fact]
    public void RG12_HasIsaretli_once_yuvarlar_sonra_isaret_birim_sozlukten()
    {
        Assert.Equal("0,0000 has", Bicim.HasIsaretli(-0.00004m));
        Assert.Equal("0,0000 has", Bicim.HasIsaretli(-0.00001m));
        Assert.Equal("−0,0001 has", Bicim.HasIsaretli(-0.00005m));    // yarım hane sıfırdan uzağa
        Assert.Equal("−0,0001 has", Bicim.HasIsaretli(-0.0001m));
        Assert.Equal("0,0000 has", Bicim.HasIsaretli(0.00004m));
        Assert.Equal("−12,3000 fine", Bicim.HasIsaretli(-12.3m, KarRaporSozlugu.En.HasBirimKisa));
        Assert.Equal("has", KarRaporSozlugu.Tr.HasBirimKisa);
        Assert.Equal("fine", KarRaporSozlugu.En.HasBirimKisa);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// EKRAN (bUnit) — Rapor merkezi
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class RaporGorunumRaporlarEkranTestleri : BunitContext
{
    internal const string Gunluk = """
        {"tarih":"2026-10-03","satis":{"adet":11,"toplamTl":327878.00,"toplamHas":26.0588,"altinToplamHas":26.0588},
         "alis":{"adet":0,"toplamTl":0,"toplamHas":0},
         "hurda":{"toplamHas":0,"odenenTl":0},"netHasGramHareketi":26.0588,"hareketSayisi":11,
         "kasalar":[{"id":"k1","ad":"Nakit","bakiye":1500.00,"paraBirimi":"TRY"}]}
        """;

    private const string StokDegeri = """
    {"urunler":[{"urunId":"a1","kod":"A-1","ad":"22 Ayar Bilezik","ayarId":"K22","maden":"ALTIN",
                 "adet":1,"blokeAdet":0,"satilabilirAdet":1,"toplamHas":12.5,"fizikselGram":13.6},
                {"urunId":"a2","kod":"A-2","ad":"14 Ayar Küpe","ayarId":"K14","maden":"ALTIN",
                 "adet":2,"blokeAdet":0,"satilabilirAdet":2,"toplamHas":3.5,"fizikselGram":6},
                {"urunId":"g1","kod":"G-1","ad":"925 Ayar Gümüş Yüzük Seti","ayarId":"G925","maden":"GUMUS",
                 "adet":3,"blokeAdet":0,"satilabilirAdet":3,"toplamHas":66.6,"fizikselGram":72},
                {"urunId":"g0","kod":"G-0","ad":"925 Ayar Gümüş Tektaş","ayarId":"G925","maden":"GUMUS",
                 "adet":0,"blokeAdet":0,"satilabilirAdet":0,"toplamHas":0,"fizikselGram":0}],
     "toplamHas":16,"madenDagilimi":[{"maden":"GUMUS","madenAdi":"Gümüş","adet":3,"toplamGram":66.6,"fizikselGram":72},
                                     {"maden":"ALTIN","madenAdi":"Altın","adet":3,"toplamGram":16,"fizikselGram":19.6}],
     "toplamBlokeAdet":0,"toplamSatir":4,"ofset":0,"limit":500,"dahaFazlasiVar":false}
    """;

    private const string ReyonStok = """
    {"reyonlar":[{"reyonId":"r1","reyonAdi":"Vitrin 1","tip":"VITRIN","aktif":true,"sorumluKullaniciId":null,
                  "subeId":"s1","subeAdi":"Merkez","adet":3,"urunSayisi":1,
                  "madenler":[{"maden":"GUMUS","madenAdi":"Gümüş","adet":3,"toplamGram":66.6,"fizikselGram":72}],
                  "altinHasGram":0}],
     "atanmamis":[],"toplamAltinHasGram":0,"uyari":null}
    """;

    private const string Yaslandirma = """
    {"olculduAn":"2026-10-03T06:00:00Z","subeId":null,
     "kovalar":[{"kod":"G0_30","altSinirGun":0,"ustSinirGun":30,"adet":5,"hasGram":5,"altinHasGram":5,"maliyetTl":0,"maliyetiBilinmeyenAdet":0,
                 "madenKirilimi":[{"maden":"ALTIN","madenAdi":"Altın","adet":5,"toplamGram":5,"fizikselGram":5.5}]}],
     "toplamAdet":5,"altinHasGram":5,"madenDagilimi":[{"maden":"ALTIN","madenAdi":"Altın","adet":5,"toplamGram":5}],
     "yasiBilinmeyenAdet":0,
     "devir":{"pencereGun":90,"tuketilenAdet":1,"kalanAdet":5,"devirHizi":0.2895,"yillikTahmini":1.1741,"ortalamaRafOmruGun":310.9,
              "tahminiSatisSuresiGun":310.9,"ortalamaYasGun":12.4,"yontem":"PARTI_TUKETIMI","tanim":"t"},
     "oluStok":{"esikGun":180,"toplamSatir":1,"toplamAdet":1,"ofset":0,"limit":100,"dahaFazlasiVar":false,
                "urunler":[{"urunId":"a9","kod":"A-9","ad":"Eski Küpe","ayarId":"K14","maden":"ALTIN","adet":1,"hasGram":0.585,"enEskiGiris":"2025-01-02T00:00:00Z","yasGun":640}]},
     "guvenilirlik":"TAM","uyarilar":[]}
    """;

    public RaporGorunumRaporlarEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<Kuyumcu.Web.Servisler.RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/aylik") => KarRaporRaporlarEkranTestleri.AylikHesaplanamadi,
                var y when y.EndsWith("/raporlar/gunluk") => Gunluk,
                var y when y.EndsWith("/raporlar/cari-yaslandirma") =>
                    """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""",
                var y when y.EndsWith("/raporlar/stok-degeri") => StokDegeri,
                var y when y.EndsWith("/raporlar/reyon-stok") => ReyonStok,
                var y when y.EndsWith("/raporlar/stok-yaslandirma") => Yaslandirma,
                var y when y.EndsWith("/pozisyon") => """
                    {"anaMaden":"ALTIN","altin":{"maden":"ALTIN","vitrinGram":171.448,"hurdaGram":0,"cariAlacakGram":0,"cariBorcGram":0,
                     "netGram":171.448,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},
                     "digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,"hesaplanmaAni":"2026-10-03T09:00:00Z","donemdenBagimsiz":true}
                    """,
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") => """
                    {"tarih":"2026-10-03",
                     "tahsilat":{"nakit":0,"kart":0,"havale":0,"diger":0,"toplam":0},
                     "satis":{"sayisi":0,"toplamTl":0,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":0,"faturasizTl":0},
                     "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
                     "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"altinHasGrami":0,"madenDagilimi":[],"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
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

    private IRenderedComponent<Kuyumcu.Web.Pages.Raporlar> Ciz(bool basit)
    {
        var mod = basit ? ArayuzModuServisi.Basit : ArayuzModuServisi.Profesyonel;
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        return Render<Kuyumcu.Web.Pages.Raporlar>();
    }

    // Veri (a): Rapor merkezi Bugün kartı — Bicim.Has (N4), Panel'le aynı metin (RG30).
    [Fact]
    public void RG20_Bugun_karti_has_tek_bicim()
    {
        var e = Ciz(basit: true);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#bugun-satis-has")));
        Assert.Equal("26,0588 has", e.Find("#bugun-satis-has").TextContent.Trim());
    }

    // Tur 2 (hasım DÜŞÜK): EN arayüzde Rapor merkezi Bugün kartı Panel ile AYNI birimi ("fine") yazar.
    [Fact]
    public async Task RG24_Bugun_karti_EN_birim_Panel_ile_ayni()
    {
        await Services.GetRequiredService<DilServisi>().DegistirAsync("en");
        var e = Ciz(basit: true);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#bugun-satis-has")));
        Assert.Equal("26,0588 fine", e.Find("#bugun-satis-has").TextContent.Trim());
    }

    // Görünüm: 375'te kart yığını — her td başlığını taşır; SUNUCU sırası korunur (tur 2: sıralama RG06'da sunucuda);
    // gümüş tek kalıp; gümüş toplamı sunucudan.
    [Fact]
    public void RG21_Stok_degeri_kart_yigini_sira_gumus_bicimi_ve_toplami()
    {
        var e = Ciz(basit: false);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#rapor-stok-tablo")));
        var tablo = e.Find("#rapor-stok-tablo");
        Assert.Contains("q10-yigin", tablo.ClassList);
        Assert.All(tablo.QuerySelectorAll("tbody td"), td => Assert.False(string.IsNullOrEmpty(td.GetAttribute("data-etiket"))));
        var adlar = tablo.QuerySelectorAll("tbody tr").Select(t => t.QuerySelector("td")!.TextContent.Trim()).ToList();
        Assert.Equal(["22 Ayar Bilezik", "14 Ayar Küpe", "925 Ayar Gümüş Yüzük Seti", "925 Ayar Gümüş Tektaş"], adlar);
        var sifir = tablo.QuerySelectorAll("tbody tr").Last().QuerySelectorAll("td")[3].TextContent.Trim();
        Assert.Equal("0,00 g (saf 0,00 g)", sifir);
        // PAKET GOC-D: stok listesi ortak ızgarada; maden toplamları (eski tfoot) ızgaranın ALTINDA aynı kimlikle — ad/değer ayrı öğe.
        Assert.Equal("16,0000 has", e.Find("#rapor-stok-altin-toplam .izgara-alt-toplam-deger").TextContent.Trim());
        var gumus = e.Find("#rapor-stok-toplam-gumus");
        Assert.Equal(string.Format(KarRaporSozlugu.Tr.StokMadenToplam, "Gümüş"), gumus.QuerySelector(".izgara-alt-toplam-ad")!.TextContent.Trim());
        Assert.Equal("72,00 g (saf 66,60 g)", gumus.QuerySelector(".izgara-alt-toplam-deger")!.TextContent.Trim());
        // Reyon tablosu: sayı başlıkları değerle aynı hizada (sağ).
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#reyon-stok-tablo")));
        var basliklar = e.FindAll("#reyon-stok-tablo thead th");
        Assert.Contains("sayi", basliklar[2].ClassList);
        Assert.Contains("sayi", basliklar[3].ClassList);
    }

    // Veri (c): "raf ömrü" tahmin adıyla; gerçek ortalama yaş ayrı kart; oranlar 2 hane, gün tam sayı.
    [Fact]
    public void RG22_Stok_yasi_tahmini_satis_suresi_ve_ortalama_yas()
    {
        var e = Ciz(basit: false);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#stok-yaslandirma-kart .stat-izgara")));
        var kr = KarRaporSozlugu.Tr;
        var kartlar = e.FindAll("#stok-yaslandirma-kart .stat-izgara")[1].QuerySelectorAll(".stat-kart")
            .ToDictionary(k => k.QuerySelector(".stat-etiket")!.TextContent.Trim(), k => k.QuerySelector(".stat-deger")!.TextContent.Trim());
        Assert.Equal("311", kartlar[kr.TahminiSatisSuresi]);
        Assert.Equal("12", kartlar[kr.OrtalamaYasGun]);
        Assert.Equal("0,29", kartlar[Sozlukler.Getir("tr").RaporSeti.DevirHizi]);
        Assert.Equal("1,17", kartlar[Sozlukler.Getir("tr").RaporSeti.YillikTahmini]);
        Assert.False(kartlar.ContainsKey(Sozlukler.Getir("tr").RaporSeti.OrtalamaRafOmru));
        // Ölü stok tablosu da 375'te kart yığını.
        Assert.Contains("q10-yigin", e.Find("#sy-olu-tablo").ClassList);
    }

    // Görünüm: "Rapordaki bir rakam yanlışsa" — rapora özgü TEK cümle, ikinci düzeltme yolu yok.
    [Fact]
    public void RG23_Duzeltme_notu_tek_cumle()
    {
        var e = Ciz(basit: false);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#rp-duzeltme-ac")));
        e.Find("#rp-duzeltme-ac").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#rp-duzeltme-govde .degismez-gerekce")));
        Assert.Equal(KarRaporSozlugu.Tr.RaporTuretilmisGerekce, e.Find("#rp-duzeltme-govde .degismez-gerekce").TextContent.Trim());
        Assert.Empty(e.FindAll("#rp-duzeltme-govde .degismez-alternatif"));
        Assert.DoesNotContain("Bakiye", e.Find("#rp-duzeltme-govde").TextContent, StringComparison.Ordinal);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// EKRAN (bUnit) — Panel: "Bugün ne sattım" Rapor merkeziyle AYNI metin
// ═════════════════════════════════════════════════════════════════════════════
public sealed class RaporGorunumPanelEkranTestleri : BunitContext
{
    public RaporGorunumPanelEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
        Services.AddScoped<HavaServisi>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var cevap = yol switch
            {
                var y when y.EndsWith("/raporlar/gunluk", StringComparison.Ordinal) => RaporGorunumRaporlarEkranTestleri.Gunluk,
                var y when y.EndsWith("/raporlar/cari-yaslandirma", StringComparison.Ordinal) => """
                    {"yaslananCariler":[{"cariId":"c3","cariAd":"Mehmet Öz","cariTur":"TEDARIKCI","tlBakiye":0,"hasGramBakiye":-22.0,
                      "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"dovizBakiyesiVar":false,"gunSayisi":3,"dilim":"0-30"}],
                     "ozetTl":{},"toplamSatir":1,"ofset":0,"limit":500,"dahaFazlasiVar":false}
                    """,
                var y when y.EndsWith("/raporlar/vade-takibi", StringComparison.Ordinal) =>
                    """{"ufukGun":7,"gecikenToplam":0,"yaklasanToplam":0,"vadesizToplam":0,"ufukOtesiToplam":0,"acikToplam":0,"cariler":[]}""",
                var y when y.EndsWith("/raporlar/aylik", StringComparison.Ordinal) => """{"gunler":[]}""",
                var y when y.Contains("/raporlar/islem-defteri", StringComparison.Ordinal) => """
                    {"baslangic":"2026-10-03","bitis":"2026-10-03","toplamSatir":0,"ofset":0,"limit":5,
                     "dahaFazlasiVar":false,"ozet":{"tlGiris":0,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":0},
                     "storno":{"adet":0,"tutarTl":0},"satirlar":[]}
                    """,
                var y when y.EndsWith("/bildirimler", StringComparison.Ordinal) =>
                    """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""",
                var y when y.EndsWith("/raporlar/kar-zarar", StringComparison.Ordinal) => """
                    {"hasilatTl":327878.00,"maliyetTl":145431,"karTl":5000.00,"karDurumu":"KISMI","karMarjiYuzde":null,
                     "hasilatHas":26.0588,"maliyetHas":0.1,"karHas":0.1,"satisAdedi":11,
                     "maliyetiEksikSatisAdedi":9,"maliyetiEksikIadeAdedi":1,"maliyetiEksikTutarTl":95566.52,
                     "maliyetiBilinenAdet":2,"maliyetiBilinmeyenAdet":9,"guvenilirlik":"DUSUK","uyari":"x"}
                    """,
                var y when y.EndsWith("/bankalar", StringComparison.Ordinal) => """{"hesaplar":[]}""",
                var y when y.EndsWith("/kasalar/nakit-ayrimi", StringComparison.Ordinal) =>
                    """[{"kasaId":"k1","paraBirimi":"TRY","toplam":1500.00,"nakit":1500.00,"kart":0,"havale":0}]""",
                var y when y.EndsWith("/pozisyon", StringComparison.Ordinal) => """
                    {"anaMaden":"ALTIN","altin":{"maden":"ALTIN","vitrinGram":171.448,"hurdaGram":0,"cariAlacakGram":0,"cariBorcGram":0,
                     "netGram":171.448,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},
                     "digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,"hesaplanmaAni":"2026-10-03T09:00:00Z","donemdenBagimsiz":true}
                    """,
                var y when y.EndsWith("/kur/kaynaklar", StringComparison.Ordinal) =>
                    """{"kaynaklar":["ALTINKAYNAK"],"secili":"ALTINKAYNAK","varsayilan":"ALTINKAYNAK"}""",
                var y when y.Contains("/tercihler/benim", StringComparison.Ordinal) => """{"tercihler":{}}""",
                var y when y.Contains("/uyum/", StringComparison.Ordinal) =>
                    """{"suresiGecmis":0,"yaklasan":0,"gecikmisSayisi":0,"yuksek":0}""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Panel> Basit()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1",
                ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-sade");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-soru-satis-has")), TimeSpan.FromSeconds(10));
        return e;
    }

    // Veri (a): Panel "Bugün ne sattım" = Rapor merkezi Bugün kartı (RG20) — "26,1 g has" DEĞİL.
    [Fact]
    public void RG30_Panel_bugun_has_Rapor_merkeziyle_ayni_metin()
    {
        var e = Basit();
        Assert.Equal("26,0588 has", e.Find("#panel-soru-satis-has").TextContent.Trim());
        Assert.DoesNotContain(" g has", e.Find("#panel-soru-satis").TextContent, StringComparison.Ordinal);
        // Kim borçlu has satırı da aynı dil.
        e.WaitForAssertion(() => Assert.Contains("−22,0000 has", e.Find("#panel-borclu-tutar-c3").TextContent, StringComparison.Ordinal));
    }

    // Görünüm: "9 satış ve 1 iade" öbeği bölünmez — "iade" tek başına alt satıra düşmez.
    [Fact]
    public void RG31_Panel_kar_satiri_eksik_obegi_bolunmez()
    {
        var e = Basit();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-soru-satis-kar")));
        var metin = e.Find("#panel-soru-satis-kar").TextContent;
        // PAKET RAPOR-2: kısa öbek "9 satış, 1 iade", "eksik:" ile birlikte bölünmez.
        // PAKET RAPOR-3: öbek "9 satış ve 1 iadenin maliyeti yok" — yine bölünmez.
        Assert.Contains("9\u00A0satış\u00A0ve\u00A01\u00A0iadenin\u00A0maliyeti\u00A0yok", metin, StringComparison.Ordinal);
    }

    // Tur 2 (hasım DÜŞÜK): EN arayüzde has birimi sözlükten ("fine") — Türkçe "has" sabiti basılmaz.
    [Fact]
    public async Task RG32_Panel_EN_has_birimi_sozlukten()
    {
        await Services.GetRequiredService<DilServisi>().DegistirAsync("en");
        var e = Basit();
        Assert.Equal("26,0588 fine", e.Find("#panel-soru-satis-has").TextContent.Trim());
        e.WaitForAssertion(() => Assert.Contains("−22,0000 fine", e.Find("#panel-borclu-tutar-c3").TextContent, StringComparison.Ordinal));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// EKRAN (bUnit) — Kâr-zarar
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class RaporGorunumKarZararEkranTestleri : BunitContext
{
    // KISMI: bilinen 6.000 − maliyet 4.000 = kâr 2.000; toplam satış 10.000 (4.000 eksik, iadeden netlenmiş).
    private static string OzetKismi => MadenAyrimiKarZararEkranTestleri.OzetTam
        .Replace("\"maliyetTl\":8000,", "\"maliyetTl\":4000,", StringComparison.Ordinal)
        .Replace("\"karDurumu\":\"TAM\",\"bilinenHasilatTl\":10000,", "\"karDurumu\":\"KISMI\",\"bilinenHasilatTl\":6000,", StringComparison.Ordinal)
        .Replace("\"maliyetiEksikSatisAdedi\":0,\"maliyetiEksikIadeAdedi\":0,\"maliyetiEksikTutarTl\":0,",
                 "\"maliyetiEksikSatisAdedi\":2,\"maliyetiEksikIadeAdedi\":1,\"maliyetiEksikTutarTl\":4000,", StringComparison.Ordinal);

    private string ozet = OzetKismi;
    private string urun = """
        {"satirlar":[
          {"urunId":"u1","kod":"K-1","ad":"24 Ayar Külçe Gram Altın","satisAdedi":1,"iadeAdedi":0,"maden":"ALTIN",
           "hasilatTl":36391.32,"maliyetTl":30500,"bilinenHasilatTl":36391.32,"karTl":5891.32,"karDurumu":"TAM",
           "maliyetiEksikSatisAdedi":0,"maliyetiEksikTutarTl":0,"hasilatHas":5.33,"maliyetHas":null,"karHas":null,
           "karMarjiYuzde":16.19,"maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM","hasKarDurumu":"VERI_YOK","hasMaliyetiEksikSatisAdedi":0}],
         "urunSayisi":1,"limit":50,"dahaFazlasiVar":false,"guvenilirlik":"TAM"}
        """;

    public RaporGorunumKarZararEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => ozet, () => urun)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.RaporServisi>();
    }

    private sealed class SahteApi(Func<string> ozet, Func<string> urun) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.Contains("kar-zarar/urun") => urun(),
                var y when y.Contains("kar-zarar") => ozet(),
                var y when y.Contains("stok-degerlemesi") => MadenAyrimiKarZararEkranTestleri.StokBilinmiyor,
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

    [Fact]
    public void RG40_Fikstur_KISMI()
    {
        Assert.Contains("\"karDurumu\":\"KISMI\",\"bilinenHasilatTl\":6000", OzetKismi, StringComparison.Ordinal);
        Assert.Contains("\"maliyetTl\":4000,", OzetKismi, StringComparison.Ordinal);
        Assert.Contains("\"maliyetiEksikIadeAdedi\":1", OzetKismi, StringComparison.Ordinal);
    }

    // Veri (d): hasılat çubuğu = maliyeti BİLİNEN satış (6.000), maliyet 4.000, kâr 2.000 → çubuklar toplanabilir.
    [Fact]
    public void RG41_Grafik_hasilat_cubugu_kar_ile_ayni_kume_ve_okunur_eksen()
    {
        var c = Ciz(basit: false);
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-grafik-cubuk .grafik-eksen-alt")));
        var ogeler = c.FindAll("#kz-grafik-cubuk .grafik-eksen-oge");
        // PAKET RAPOR-4 tur 2 (tasarım KRİTİK): önce TOPLAM satış (10.000 — Özet "Toplam satış" ile aynı), sonra Özet
        // kartıyla AYNI etiketli "Maliyeti bilinen satış" (6.000); bilinen − maliyet = kâr hâlâ toplanabilir.
        Assert.Equal(KarRaporSozlugu.Tr.GrafikToplamSatis, ogeler[0].QuerySelector(".grafik-eksen-ad")!.TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(10000m), ogeler[0].QuerySelector(".grafik-eksen-deger")!.TextContent.Trim());
        Assert.Equal(KarRaporSozlugu.Tr.BilinenHasilat, ogeler[1].QuerySelector(".grafik-eksen-ad")!.TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(6000m), ogeler[1].QuerySelector(".grafik-eksen-deger")!.TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(4000m), ogeler[2].QuerySelector(".grafik-eksen-deger")!.TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(2000m), ogeler[3].QuerySelector(".grafik-eksen-deger")!.TextContent.Trim());
        Assert.Empty(c.FindAll("#kz-grafik-cubuk svg text"));
        Assert.Contains(Bicim.TlKurus(10000m), c.Find("#kz-grafik-hasilat-notu").TextContent, StringComparison.Ordinal);
    }

    // Tur 2 (hasım DÜŞÜK): bu dönem HESAPLANAMADI (kâr null), önceki dönem KISMI → 1. çubuk TOPLAM hasılat ve
    // etiketi "Hasılat" (değerle aynı koşul); 2. serinin bilinen kümesi ayrı notla söylenir.
    [Fact]
    public void RG44_Grafik_etiketi_degerle_ayni_kosul_onceki_kume_notu()
    {
        ozet = MadenAyrimiKarZararEkranTestleri.OzetTam
            .Replace("\"maliyetTl\":8000,", "\"maliyetTl\":null,", StringComparison.Ordinal)
            .Replace("\"karTl\":2000,\"karDurumu\":\"TAM\",\"bilinenHasilatTl\":10000,",
                     "\"karTl\":null,\"karDurumu\":\"HESAPLANAMADI\",\"bilinenHasilatTl\":0,", StringComparison.Ordinal)
            .Replace("\"onceki\":null,",
                     "\"onceki\":{\"baslangic\":\"2026-09-29\",\"bitis\":\"2026-09-30\",\"gunSayisi\":2,\"hasilatTl\":9000,\"maliyetTl\":3000,\"karTl\":2000,\"donemKariTl\":2000,\"bilinenHasilatTl\":5000},", StringComparison.Ordinal);
        Assert.Contains("\"bilinenHasilatTl\":5000}", ozet, StringComparison.Ordinal);
        Assert.Contains("\"karDurumu\":\"HESAPLANAMADI\"", ozet, StringComparison.Ordinal);
        var c = Ciz(basit: false);
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-grafik-cubuk .grafik-eksen-alt")));
        var ilk = c.FindAll("#kz-grafik-cubuk .grafik-eksen-oge")[0];
        Assert.Equal(Services.GetRequiredService<DilServisi>().T.KarZarar.Hasilat, ilk.QuerySelector(".grafik-eksen-ad")!.TextContent.Trim());
        Assert.Contains(Bicim.TlKurus(10000m), ilk.QuerySelector(".grafik-eksen-deger")!.TextContent, StringComparison.Ordinal);
        Assert.Empty(c.FindAll("#kz-grafik-hasilat-notu"));
        Assert.Equal(string.Format(KarRaporSozlugu.Tr.GrafikOncekiHasilatNotu, Bicim.TlKurus(9000m)),
            c.Find("#kz-grafik-onceki-hasilat-notu").TextContent.Trim());
    }

    // Tur 2 (hasım DÜŞÜK): gümüş satırının "—" has kârı gerekçesiz kalmaz.
    [Fact]
    public void RG45_Gumus_satiri_has_kari_gerekceli()
    {
        urun = """
            {"satirlar":[
              {"urunId":"g1","kod":"G-1","ad":"DG 925 Gümüş Zincir Kolye","satisAdedi":1,"iadeAdedi":0,"maden":"GUMUS",
               "hasilatTl":2500,"maliyetTl":1500,"bilinenHasilatTl":2500,"karTl":1000,"karDurumu":"TAM",
               "maliyetiEksikSatisAdedi":0,"maliyetiEksikTutarTl":0,"hasilatHas":null,"maliyetHas":null,"karHas":null,
               "karMarjiYuzde":40,"maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM","hasKarDurumu":null,"hasMaliyetiEksikSatisAdedi":0}],
             "urunSayisi":1,"limit":50,"dahaFazlasiVar":false,"guvenilirlik":"TAM"}
            """;
        var c = Ciz(basit: false);
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-urun-tablo")));
        var has = c.Find("#kz-urun-tablo tbody td.kz-urun-has");
        Assert.Equal(KarRaporSozlugu.Tr.HasYalnizAltinNot, has.QuerySelector(".kz-urun-has-altin-degil")!.TextContent.Trim());
        Assert.Empty(has.QuerySelectorAll(".kz-urun-has-hesaplanamadi"));
    }

    // Görünüm: 375 kart yığını + gerekçesiz "—" yok (TL maliyeti bilinen altın satırı "Alış günü altın fiyatı yok").
    [Fact]
    public void RG42_Urunler_kart_yigini_ve_has_hucresi_gerekceli()
    {
        var c = Ciz(basit: false);
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-urun-tablo")));
        var tablo = c.Find("#kz-urun-tablo");
        Assert.Contains("q10-yigin", tablo.ClassList);
        // PAKET RAPOR-2: kart yığınında ad başlıktır — "Ürün Adı" etiketi taşımaz; öteki her hücre etiketli.
        Assert.All(tablo.QuerySelectorAll("tbody td:not(.kz-urun-ad)"), td => Assert.False(string.IsNullOrEmpty(td.GetAttribute("data-etiket"))));
        Assert.All(tablo.QuerySelectorAll("tbody td.kz-urun-ad"), td => Assert.Null(td.GetAttribute("data-etiket")));
        var has = tablo.QuerySelector("tbody td.kz-urun-has")!;
        Assert.NotNull(has.QuerySelector(".kz-has-blok"));
        Assert.Equal(KarRaporSozlugu.Tr.HasFiyatYokSatirNot, has.QuerySelector(".kz-urun-has-hesaplanamadi")!.TextContent.Trim());
    }

    // Görünüm/veri: eksik tutar iadeden netlenmişse bunu söyler; PRO defter kartlarında altın önce; 3+3 ızgara kimliği.
    [Fact]
    public void RG43_Ozet_eksik_tutar_iadeli_altin_once()
    {
        var c = Ciz(basit: false);
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-maliyet-eksik-satir")));
        Assert.Equal(string.Format(KarRaporSozlugu.Tr.MaliyetiEksikSatirIadeli, 2, Bicim.TlKurus(4000m)),
            c.Find("#kz-maliyet-eksik-satir").TextContent.Trim());
        var hasIzgara = c.FindAll("#kz-panel-ozet .stat-izgara")[1];
        var etiketler = hasIzgara.QuerySelectorAll(".stat-etiket").Select(x => x.TextContent.Trim()).ToList();
        var altin = etiketler.FindIndex(x => x == KarRaporSozlugu.Tr.DefterAltinEtiketi);
        var gumus = etiketler.FindIndex(x => x == string.Format(KarRaporSozlugu.Tr.DefterMadenEtiketi, "Gümüş"));
        Assert.True(altin >= 0 && gumus >= 0 && altin < gumus, string.Join(" | ", etiketler));
        Assert.NotNull(c.Find("#kz-tl-kpi"));
    }
}
