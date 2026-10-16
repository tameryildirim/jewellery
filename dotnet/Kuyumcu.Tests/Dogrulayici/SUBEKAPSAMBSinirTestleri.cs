using System.Net;
using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET ŞUBE-KAPSAM-B sınır testleri.
//
// Paket para/gram HESAPLAMAZ; yalnız istek yoluna `subeId=` ekler ve Kasa
// raporunda defter yanıtını istemcide daraltır (KasaRaporu.Daralt). Sınırlar
// bu iki yerdedir:
//  S1  SubeliSorgu.Ekle: seçim yok → yol AYNEN; mevcut sorguya '&'; yolda
//      zaten subeId varsa dokunulmaz; özel karakter kaçışlanır.
//  S2  Kasa raporu (finansal ekran): şube seçiliyken /kasalar düşerse ekran
//      "kayıt yok" GÖSTERMEMELİ — hata bandı görünmeli (UX-S28 / Anayasa md.1).
// Beklenen değerler SABİTTİR (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SUBEKAPSAMBSinirTestleri : BunitContext
{
    private string tercihSube = "s2";
    private HttpStatusCode kasalarKodu = HttpStatusCode.OK;
    private readonly List<string> getler = [];

    private const string KasalarHataMetni = "Kasalar okunamadi DGR";

    private sealed class Sahte(SUBEKAPSAMBSinirTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var tam = istek.RequestUri!.PathAndQuery;
            var yol = istek.RequestUri!.AbsolutePath;
            if (istek.Method == HttpMethod.Get) lock (t.getler) t.getler.Add(tam);
            string govde;
            var kod = HttpStatusCode.OK;
            if (yol.EndsWith("/tercihler/benim", StringComparison.Ordinal))
                govde = $$$"""{"tercihler":{"gorunum-subesi":"{{{t.tercihSube}}}"},"gorunumSubesi":{"kayitli":"{{{t.tercihSube}}}","subeId":{{{(t.tercihSube == "TUMU" ? "null" : "\"" + t.tercihSube + "\"")}}},"tumSubeler":true,"gecersizTercih":false,"deger":"{{{t.tercihSube}}}"}}""";
            else if (yol.EndsWith("/subeler", StringComparison.Ordinal))
                govde = """[{"id":"s1","ad":"Merkez","aktif":true},{"id":"s2","ad":"Şube 2","aktif":true}]""";
            else if (yol.EndsWith("/kasalar", StringComparison.Ordinal))
            {
                kod = t.kasalarKodu;
                govde = kod == HttpStatusCode.OK
                    ? """[{"id":"k2","ad":"Şube 2 Kasası","paraBirimi":"TRY","bakiye":999999999.99,"aktif":true,"subeId":"s2"}]"""
                    : $$"""{"hata":"{{KasalarHataMetni}}","message":"{{KasalarHataMetni}}"}""";
            }
            else if (yol.Contains("/raporlar/kasa-defteri", StringComparison.Ordinal))
                govde = $$"""
                    {"baslangic":"2026-09-08","bitis":"{{DateTime.Today:yyyy-MM-dd}}","saatDilimi":"Europe/Istanbul","utcOfsetSaat":3,"yazSaatiRiski":false,"toplamSatir":2,"kasalar":[
                     {"kasaId":"k1","kasaAd":"Merkez Kasa","paraBirimi":"TRY","acilisDeviri":0,"kapanisBakiye":1200.00,"guncelBakiye":1200.00,"gunler":[]},
                     {"kasaId":"k2","kasaAd":"Şube 2 Kasası","paraBirimi":"TRY","acilisDeviri":0,"kapanisBakiye":999999999.99,"guncelBakiye":999999999.99,"gunler":[]}]}
                    """;
            else govde = "{}";
            return Task.FromResult(new HttpResponseMessage(kod)
            {
                Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    public SUBEKAPSAMBSinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
        Services.AddScoped<HavaServisi>();
    }

    private OturumServisi Oturum => Services.GetRequiredService<OturumServisi>();

    private void Hazirla(string? sube)
    {
        tercihSube = sube ?? "TUMU";
        Oturum.KullaniciAta(new OturumKullanici("u1", "Ahmet", "Yılmaz", "a@ornek.com", "SAHIP", "s1", null, ArayuzModuServisi.Profesyonel));
        var s = Render<SubeSecici>();
        s.WaitForAssertion(() =>
        {
            var g = KabukServisi.Ortak.GorunumIcin(Oturum);
            Assert.NotNull(g);
            Assert.True(g!.Yuklendi);
            Assert.Equal(2, g.Subeler.Count);
        });
    }

    // ── S1 — SubeliSorgu.Ekle sınırları ─────────────────────────────────────

    [Fact]
    public void S1a_Secim_yok_TUMU_iken_yol_AYNEN_doner()
    {
        Hazirla(null);
        Assert.Equal("/kasalar", SubeliSorgu.Ekle("/kasalar", Oturum));
        Assert.Equal("/raporlar/gunluk?tarih=2026-10-01", SubeliSorgu.Ekle("/raporlar/gunluk?tarih=2026-10-01", Oturum));
    }

    [Fact]
    public void S1b_Secim_varken_soru_isareti_ya_da_ampersand_dogru_eklenir()
    {
        Hazirla("s2");
        Assert.Equal("/kasalar?subeId=s2", SubeliSorgu.Ekle("/kasalar", Oturum));
        Assert.Equal("/raporlar/gunluk?tarih=2026-10-01&subeId=s2", SubeliSorgu.Ekle("/raporlar/gunluk?tarih=2026-10-01", Oturum));
    }

    [Fact]
    public void S1c_Yolda_subeId_zaten_varsa_ikinci_kez_eklenmez()
    {
        Hazirla("s2");
        Assert.Equal("/hurda/bakiye?subeId=s1", SubeliSorgu.Ekle("/hurda/bakiye?subeId=s1", Oturum));
    }

    [Fact]
    public void S1d_Ozel_karakterli_sube_kimligi_kacislanir()
    {
        Hazirla("a b&c");
        Assert.Equal("/kasalar?subeId=a%20b%26c", SubeliSorgu.Ekle("/kasalar", Oturum));
    }

    // ── S2 — Kasa raporu: hata "kayıt yok"a çevrilmez ───────────────────────

    [Fact]
    public void S2a_Sube_secili_kasa_raporu_yalniz_o_subenin_kasasini_gosterir_buyuk_tutar_bozulmaz()
    {
        Hazirla("s2");
        var e = Render<Kuyumcu.Web.Pages.KasaRaporu>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kr-ozet")), TimeSpan.FromSeconds(10));
        var metin = e.Find("#kr-ozet").TextContent;
        Assert.Contains("Şube 2 Kasası", metin, StringComparison.Ordinal);
        Assert.DoesNotContain("Merkez Kasa", metin, StringComparison.Ordinal);
        Assert.Contains("999.999.999,99", metin, StringComparison.Ordinal);
    }

    [Fact]
    public void S2b_Sube_secili_iken_kasalar_dusarse_hata_bandi_gorunur_bos_durum_degil()
    {
        Hazirla("s2");
        kasalarKodu = HttpStatusCode.InternalServerError;
        var e = Render<Kuyumcu.Web.Pages.KasaRaporu>();
        e.WaitForAssertion(() =>
            Assert.Contains(getler, y => y.Contains("/raporlar/kasa-defteri", StringComparison.Ordinal)),
            TimeSpan.FromSeconds(10));
        e.WaitForAssertion(() =>
            Assert.Contains(KasalarHataMetni, e.Markup, StringComparison.Ordinal),
            TimeSpan.FromSeconds(5));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI tur 3 (Aşkın + Mehmet) — GET /pozisyon?subeId= SINIRLARI
// [HTTP, gerçek MySQL]. Tur 3 pozisyon kartına şube daraltması ekledi (gram/has
// gösteren yol). Beklenen değerler SABİTTİR (üretim kodundan hesaplanmaz):
//  P0 sıfır: boş şube → vitrin 0, hurda 0 (kiracı toplamı sızmaz)
//  P1 eksi: HURDA_SATIS (−) yalnız kendi şubesinde düşer; cari borç (−) her görünümde aynı
//  P2 4 hane: 0,0001 g hurda yalnız B'de; M'de 0; tümü = M + 0,0001
//  P3 çok büyük: 999.999.999,9999 g vitrin B'de birebir; tümü = M + B (taşma/yuvarlama yok)
//  P4 boş/null: ?subeId= (boş) → birleşik (TUM_SUBELER)
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class SUBEKAPSAMBPozisyonSinirTestleri(HttpFikstur f)
{
    private sealed record Kur(Kiraci K, string SubeB);

    private Kur Hazirla(string ad, decimal stokB, params (bool B, string Tur, decimal Has)[] satirlar)
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, ad);   // Merkez: 100 adet × 10 g × 0,916 (K22) = 916 g has vitrin
        var now = DateTime.UtcNow;
        var subeB = Kuyumcu.Api.Common.Kimlik.Ulid();
        db.Subeler.Add(new Kuyumcu.Api.Data.Sube { Id = subeB, TenantId = k.TenantId, Ad = "Şube B", Aktif = true, Olusturuldu = now, Guncellendi = now });
        if (stokB != 0m)
            db.Stoklar.Add(new Kuyumcu.Api.Data.Stok { Id = Kuyumcu.Api.Common.Kimlik.Ulid(), TenantId = k.TenantId, SubeId = subeB, UrunId = k.UrunId, Adet = 1m, ToplamHasGram = stokB, Guncellendi = now });
        foreach (var (b, tur, has) in satirlar)
            db.Hareketler.Add(new Kuyumcu.Api.Data.Hareket
            {
                Id = Kuyumcu.Api.Common.Kimlik.Ulid(), TenantId = k.TenantId, SubeId = b ? subeB : k.SubeId, Tur = tur, Tarih = now,
                TlTutari = -10m, HasGrami = has, ParaBirimi = "TRY", Maden = "ALTIN",
                KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
                OdemeTuru = "VERESIYE", Aciklama = "DGR SKB sinir", Olusturuldu = now,
            });
        db.SaveChanges();
        return new Kur(k, subeB);
    }

    private static async Task<System.Text.Json.JsonElement> Al(HttpClient ist, string yol)
    {
        var c = await ist.GetAsync(yol);
        var g = await c.Content.ReadAsStringAsync();
        Assert.True(c.StatusCode == HttpStatusCode.OK, $"{yol} → {(int)c.StatusCode}: {g}");
        return System.Text.Json.JsonDocument.Parse(g).RootElement;
    }

    private static decimal A(System.Text.Json.JsonElement j, string alan) => j.GetProperty("altin").GetProperty(alan).GetDecimal();

    [Fact]
    public async Task DGR_P0_bos_subede_vitrin_ve_hurda_SIFIR_kiraci_toplami_sizmaz()
    {
        var s = Hazirla("PAKETSUBE-KAPSAM-B-TEST dgr0", 0m, (false, "HURDA_ALIS", 2m));
        var ist = f.Ortam.Istemci(s.K, "SAHIP");
        var b = await Al(ist, $"/api/v1/pozisyon?subeId={s.SubeB}");
        Assert.Equal(0m, A(b, "vitrinGram"));
        Assert.Equal(0m, A(b, "hurdaGram"));
        var m = await Al(ist, $"/api/v1/pozisyon?subeId={s.K.SubeId}");
        Assert.Equal(916m, A(m, "vitrinGram"));
        Assert.Equal(2m, A(m, "hurdaGram"));
    }

    [Fact]
    public async Task DGR_P1_eksi_hurda_satis_yalniz_kendi_subesinde_cari_borc_her_gorunumde_ayni()
    {
        // B: 3 g alış, 0,5 g satış (−) → 2,5; cari VERESIYE +4 has → borç 4 g (kiracı geneli)
        var s = Hazirla("PAKETSUBE-KAPSAM-B-TEST dgr1", 0m,
            (true, "HURDA_ALIS", 3m), (true, "HURDA_SATIS", -0.5m), (false, "VERESIYE", 4m));
        var ist = f.Ortam.Istemci(s.K, "SAHIP");
        var b = await Al(ist, $"/api/v1/pozisyon?subeId={s.SubeB}");
        var m = await Al(ist, $"/api/v1/pozisyon?subeId={s.K.SubeId}");
        Assert.Equal(2.5m, A(b, "hurdaGram"));
        Assert.Equal(0m, A(m, "hurdaGram"));
        Assert.Equal(4m, A(b, "cariBorcGram"));
        Assert.Equal(4m, A(m, "cariBorcGram"));
        // net(B) = 0 vitrin + 2,5 hurda + 0 alacak − 4 borç = −1,5
        Assert.Equal(-1.5m, A(b, "netGram"));
    }

    [Fact]
    public async Task DGR_P2_dort_hane_0_0001_gram_yalniz_B_de_M_de_sifir_tumu_toplam()
    {
        var s = Hazirla("PAKETSUBE-KAPSAM-B-TEST dgr2", 0m, (true, "HURDA_ALIS", 0.0001m), (false, "HURDA_ALIS", 1.2345m));
        var ist = f.Ortam.Istemci(s.K, "MUDUR");
        Assert.Equal(0.0001m, A(await Al(ist, $"/api/v1/pozisyon?subeId={s.SubeB}"), "hurdaGram"));
        Assert.Equal(1.2345m, A(await Al(ist, $"/api/v1/pozisyon?subeId={s.K.SubeId}"), "hurdaGram"));
        Assert.Equal(1.2346m, A(await Al(ist, "/api/v1/pozisyon"), "hurdaGram"));
    }

    [Fact]
    public async Task DGR_P3_cok_buyuk_vitrin_999999999_9999_birebir_tumu_M_arti_B()
    {
        var s = Hazirla("PAKETSUBE-KAPSAM-B-TEST dgr3", 999999999.9999m);
        var ist = f.Ortam.Istemci(s.K, "SAHIP");
        Assert.Equal(999999999.9999m, A(await Al(ist, $"/api/v1/pozisyon?subeId={s.SubeB}"), "vitrinGram"));
        Assert.Equal(1000000915.9999m, A(await Al(ist, "/api/v1/pozisyon?subeId=TUMU"), "vitrinGram"));
    }

    [Fact]
    public async Task DGR_P4_bos_subeId_birlesik_TUM_SUBELER()
    {
        var s = Hazirla("PAKETSUBE-KAPSAM-B-TEST dgr4", 5m);
        var ist = f.Ortam.Istemci(s.K, "SAHIP");
        var t = await Al(ist, "/api/v1/pozisyon?subeId=");
        Assert.Equal("TUM_SUBELER", t.GetProperty("kapsam").GetString());
        Assert.Equal(921m, A(t, "vitrinGram"));
    }
}
