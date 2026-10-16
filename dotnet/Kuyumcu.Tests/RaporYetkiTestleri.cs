using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Modules.Rapor;
using Kuyumcu.Api.Modules.Yetki;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 34 Faz A — RAPOR KAPILARI [HTTP boru hattı testleri]
//
// NEDEN SERVİS TESTİ YETMEZ: [RolGerekli] ve [ModulYetkisi] DENETLEYİCİ
// nitelikleridir; servis testi ikisini de görmez. Yeni üç controller
// (SatisRapor / FinansRapor) sınıf niteliğini RaporController'dan MİRAS ALMAZ
// (Bertan/Mehmet K34-3) — bu dosya her katalog rotasını TEK TEK sınar.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class RaporYetkiTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    /// <summary>Katalogdan gelen uç + en az bir zorunlu parametre.</summary>
    private static string Sorgu(string uc) => uc switch
    {
        "raporlar/kar-zarar" or "raporlar/kar-zarar/urun"
            => $"?baslangic={Zaman.YerelBugun():yyyy-MM-dd}&bitis={Zaman.YerelBugun():yyyy-MM-dd}",
        _ => "",
    };

    // ── Rol kapısı — KATALOG ÜZERİNDEN DÖNGÜ (Mehmet K34-3) ─────────────────

    [Fact]
    public async Task TEZGAHTAR_hicbir_rapor_ucuna_giremez_403()
    {
        var k = YeniKiraci("PAKET34-TEST yetki tezgahtar");
        var istemci = f.Ortam.Istemci(k, "TEZGAHTAR");

        foreach (var kart in RaporKatalogu.Hepsi)
        {
            var cevap = await istemci.GetAsync($"/api/v1/{kart.Uc}{Sorgu(kart.Uc)}");
            Assert.True(cevap.StatusCode == HttpStatusCode.Forbidden,
                $"{kart.Kod} ({kart.Uc}) TEZGAHTAR'a {(int)cevap.StatusCode} döndü, 403 bekleniyordu");
        }
    }

    [Fact]
    public async Task MUHASEBE_rapor_uclarini_okuyabilir_200()
    {
        var k = YeniKiraci("PAKET34-TEST yetki muhasebe");
        var istemci = f.Ortam.Istemci(k, "MUHASEBE");

        foreach (var kart in RaporKatalogu.Hepsi)
        {
            var cevap = await istemci.GetAsync($"/api/v1/{kart.Uc}{Sorgu(kart.Uc)}");
            Assert.True(cevap.StatusCode == HttpStatusCode.OK,
                $"{kart.Kod} ({kart.Uc}) MUHASEBE'ye {(int)cevap.StatusCode} döndü: {await cevap.Metin()}");
        }
    }

    [Fact]
    public async Task TEZGAHTAR_rapor_modulu_ACILSA_BILE_rol_kapisina_takilir_403()
    {
        // ⚠ ÇOK KATMANLI KORUMA TUZAĞI (Aşkın): yukarıdaki test tek başına
        //   [RolGerekli]'yi SINAMAZ — TEZGAHTAR varsayılan matriste "rapor"
        //   modülüne zaten sahip değildir, yani 403'ü [ModulYetkisi] üretir.
        //   (Ölçüldü: M-34-Y mutasyonu [RolGerekli]'yi silince hiçbir test
        //   kırmıyordu.) Burada modül AÇIKÇA açılır; kalan tek kapı roldür.
        var k = YeniKiraci("PAKET34-TEST rol kapisi");
        var matris = new Dictionary<string, string[]>
        {
            ["MUDUR"] = [.. Moduller.Tumu],
            ["MUHASEBE"] = ["cari", "urun", "islem", "kasa", "banka", "fatura", "rapor"],
            ["TEZGAHTAR"] = ["cari", "urun", "islem", "rapor"],   // modül AÇIK
        };
        var kaydet = await f.Ortam.Istemci(k, "SAHIP").PutAsJsonAsync("/api/v1/yetkiler", new { matris });
        Assert.Equal(HttpStatusCode.OK, kaydet.StatusCode);

        var istemci = f.Ortam.Istemci(k, "TEZGAHTAR");
        foreach (var uc in new[] { "raporlar/islem-defteri", "raporlar/odeme-araci",
                                   "raporlar/personel", "raporlar/sube-karsilastirma",
                                   "raporlar/kasa-defteri", "raporlar/katalog" })
        {
            var cevap = await istemci.GetAsync($"/api/v1/{uc}");
            Assert.True(cevap.StatusCode == HttpStatusCode.Forbidden,
                $"{uc}: modül açıkken rol kapısı tutmadı ({(int)cevap.StatusCode})");
        }
    }

    [Fact]
    public async Task Jetonsuz_islem_defteri_401()
    {
        var cevap = await f.Ortam.AnonimIstemci().GetAsync("/api/v1/raporlar/islem-defteri");
        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
    }

    // ── Modül yetkisi kapatılınca MUDUR da giremez ───────────────────────────

    [Fact]
    public async Task Rapor_modul_yetkisi_kapatilinca_MUDUR_403()
    {
        var k = YeniKiraci("PAKET34-TEST yetki modul");

        // Önce açık: MUDUR okuyabiliyor.
        Assert.Equal(HttpStatusCode.OK,
            (await f.Ortam.Istemci(k, "MUDUR").GetAsync("/api/v1/raporlar/islem-defteri")).StatusCode);

        // SAHİP, yetki ekranından MUDUR'un "rapor" modülünü kapatır.
        var matris = new Dictionary<string, string[]>
        {
            ["MUDUR"] = [.. Moduller.Tumu.Where(m => m != "rapor")],
            ["MUHASEBE"] = ["cari", "urun", "islem", "kasa", "banka", "fatura", "rapor"],
            ["TEZGAHTAR"] = ["cari", "urun", "islem"],
        };
        var kaydet = await f.Ortam.Istemci(k, "SAHIP").PutAsJsonAsync("/api/v1/yetkiler", new { matris });
        Assert.Equal(HttpStatusCode.OK, kaydet.StatusCode);

        var cevap = await f.Ortam.Istemci(k, "MUDUR").GetAsync("/api/v1/raporlar/islem-defteri");
        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    // ── Sözleşme: camelCase + limit kapısı ───────────────────────────────────

    [Fact]
    public async Task Islem_defteri_yaniti_camelCase_sozlesmesine_uyar()
    {
        var k = YeniKiraci("PAKET34-TEST sozlesme");
        var cevap = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/raporlar/islem-defteri?limit=10");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        var g = await cevap.Json();
        Assert.Equal(JsonValueKind.Array, g.GetProperty("satirlar").ValueKind);
        Assert.Equal(JsonValueKind.Number, g.GetProperty("toplamSatir").ValueKind);
        Assert.Equal(10, g.GetProperty("limit").GetInt32());
        Assert.Equal(0, g.GetProperty("ofset").GetInt32());
        Assert.True(g.TryGetProperty("ozet", out var ozet));
        Assert.True(ozet.TryGetProperty("tlGiris", out _));
        Assert.True(g.TryGetProperty("storno", out _));
    }

    [Fact]
    public async Task Limit_sinirinin_ustunde_400()
    {
        var k = YeniKiraci("PAKET34-TEST limit http");
        var cevap = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/raporlar/islem-defteri?limit=5000");
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    [Fact]
    public async Task Ters_tarih_araligi_400()
    {
        var k = YeniKiraci("PAKET34-TEST ters tarih");
        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .GetAsync("/api/v1/raporlar/islem-defteri?baslangic=2026-09-10&bitis=2026-09-01&limit=10");
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    // ── Katalog ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Katalog_sevk_edilen_raporlari_sayar_ve_rotalari_gercektir()
    {
        var k = YeniKiraci("PAKET34-TEST katalog");
        var cevap = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/raporlar/katalog");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        var g = await cevap.Json();
        var raporlar = g.GetProperty("raporlar").EnumerateArray().ToList();
        Assert.Equal(RaporKatalogu.Hepsi.Length, g.GetProperty("toplam").GetInt32());
        Assert.True(raporlar.Count >= RaporKatalogu.FazAAsgari);

        // UX-S31: ham kod ekrana basılmaz — katalog çevrilmiş adı taşır.
        foreach (var r in raporlar)
            Assert.NotEqual(r.GetProperty("kod").GetString(), r.GetProperty("ad").GetString());
    }
}
