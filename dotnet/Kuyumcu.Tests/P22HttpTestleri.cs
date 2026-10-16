using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-22 — GERÇEK HTTP BORU HATTI.
//
// Servis testi [ModulYetkisi] / [RolGerekli] niteliklerini GÖRMEZ. K-23'ün
// birinci şartı (YETKİ) ve B-55/B-56'nın rol kapısı yalnız burada ölçülebilir.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class P22HttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static object SiparisGovde(Kiraci k) => new
    {
        tur = "MUSTERI_SIPARISI",
        cariId = k.CariId,
        sozVerilenGun = Zaman.YerelBugun().AddDays(3).ToString("yyyy-MM-dd"),
        aciklama = "P22 K-23 testi",
        kalemler = new[] { new { urunId = k.UrunId, adet = 1m } },
    };

    /// <summary>Siparişi HAZIR durumuna kadar ileri götürür (ileri geçişler serbest).</summary>
    private async Task<string> HazirSiparis(Kiraci k, HttpClient c)
    {
        var y = await c.PostAsJsonAsync("/api/v1/siparisler", SiparisGovde(k));
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var id = (await y.Json()).GetProperty("id").GetString()!;
        foreach (var hedef in new[] { "ONAYLANDI", "ATOLYEDE", "HAZIR" })
        {
            var g = await c.PostAsJsonAsync($"/api/v1/siparisler/{id}/gecis", new { yeniDurum = hedef });
            Assert.Equal(HttpStatusCode.OK, g.StatusCode);
        }
        return id;
    }

    // ═══ K-23 ŞART 1 · YETKİ ═══════════════════════════════════════════════

    [Fact]
    public async Task K23_tezgahtar_HAZIRdan_geri_ALAMAZ_mudur_ALIR()
    {
        var k = YeniKiraci("P22H-K23-YETKI");
        var mudur = f.Ortam.Istemci(k, "MUDUR");
        var id = await HazirSiparis(k, mudur);

        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");
        var red = await tezgahtar.PostAsJsonAsync($"/api/v1/siparisler/{id}/gecis",
            new { yeniDurum = "ATOLYEDE", gerekce = "yanlışlıkla hazır işaretlendi" });
        Assert.Equal(HttpStatusCode.Forbidden, red.StatusCode);

        // Durum DEĞİŞMEDİ (ölçüm karttan, yanıttan değil).
        var kart = await (await mudur.GetAsync($"/api/v1/siparisler/{id}")).Json();
        Assert.Equal("HAZIR", kart.GetProperty("durum").GetString());

        var kabul = await mudur.PostAsJsonAsync($"/api/v1/siparisler/{id}/gecis",
            new { yeniDurum = "ATOLYEDE", gerekce = "yanlışlıkla hazır işaretlendi, geri alındı" });
        Assert.Equal(HttpStatusCode.OK, kabul.StatusCode);
        Assert.Equal("ATOLYEDE", (await kabul.Json()).GetProperty("durum").GetString());
    }

    // ═══ K-23 ŞART 2 · GEREKÇE ZORUNLU ═════════════════════════════════════

    [Fact]
    public async Task K23_gerekcesiz_geri_donus_400_doner()
    {
        var k = YeniKiraci("P22H-K23-GEREKCE");
        var mudur = f.Ortam.Istemci(k, "MUDUR");
        var id = await HazirSiparis(k, mudur);

        var bos = await mudur.PostAsJsonAsync($"/api/v1/siparisler/{id}/gecis",
            new { yeniDurum = "ATOLYEDE" });
        Assert.Equal(HttpStatusCode.BadRequest, bos.StatusCode);

        var kisa = await mudur.PostAsJsonAsync($"/api/v1/siparisler/{id}/gecis",
            new { yeniDurum = "ATOLYEDE", gerekce = "kısa" });
        Assert.Equal(HttpStatusCode.BadRequest, kisa.StatusCode);

        var kart = await (await mudur.GetAsync($"/api/v1/siparisler/{id}")).Json();
        Assert.Equal("HAZIR", kart.GetProperty("durum").GetString());
    }

    // ═══ K-23 ŞART 3+4 · İZ VE GÖRÜNÜRLÜK ══════════════════════════════════

    [Fact]
    public async Task K23_geri_alinan_gecis_IZDE_ve_KARTTA_gorunur_kalir()
    {
        var k = YeniKiraci("P22H-K23-IZ");
        var mudur = f.Ortam.Istemci(k, "MUDUR");
        var id = await HazirSiparis(k, mudur);

        const string gerekce = "müşteri parçayı beğenmedi, atölyeye geri gönderildi";
        var g = await mudur.PostAsJsonAsync($"/api/v1/siparisler/{id}/gecis",
            new { yeniDurum = "ATOLYEDE", gerekce });
        Assert.Equal(HttpStatusCode.OK, g.StatusCode);

        var kart = await (await mudur.GetAsync($"/api/v1/siparisler/{id}")).Json();
        var gecisler = kart.GetProperty("gecisler").EnumerateArray().ToList();
        // DOĞUŞ (TASLAK) + 3 ileri geçiş + geri dönüş: hiçbiri silinmedi.
        Assert.Equal(5, gecisler.Count);
        var geri = gecisler[^1];
        Assert.Equal("HAZIR", geri.GetProperty("oncekiDurum").GetString());
        Assert.Equal("ATOLYEDE", geri.GetProperty("yeniDurum").GetString());
        Assert.Equal(gerekce, geri.GetProperty("gerekce").GetString());
        Assert.False(string.IsNullOrEmpty(geri.GetProperty("kullaniciId").GetString()));
        Assert.True(geri.GetProperty("olusturuldu").GetDateTime() > DateTime.UtcNow.AddMinutes(-10));

        // Ekranın kilitli düğme sebebini yazabilmesi için sunucu söylüyor (UX-S30).
        Assert.Contains("ONAYLANDI",
            kart.GetProperty("geriYonluGecisler").EnumerateArray().Select(x => x.GetString()));
        Assert.True(kart.GetProperty("geriAlmaYetkisi").GetBoolean());
    }

    [Fact]
    public async Task K23_gecis_dokumu_ucu_geri_donusu_de_tasir()
    {
        var k = YeniKiraci("P22H-K23-DOKUM");
        var mudur = f.Ortam.Istemci(k, "MUDUR");
        var id = await HazirSiparis(k, mudur);
        await mudur.PostAsJsonAsync($"/api/v1/siparisler/{id}/gecis",
            new { yeniDurum = "ONAYLANDI", gerekce = "sipariş sehven hazır işaretlenmişti" });

        var d = await (await mudur.GetAsync($"/api/v1/siparisler/{id}/gecisler")).Json();
        Assert.True(d.GetProperty("appendOnly").GetBoolean());
        Assert.Equal(5, d.GetProperty("gecisler").GetArrayLength());
    }

    // ═══ B-56 · POS KASA DEĞİŞİMİNİN TAHSİLATLI REDDİ (canlı boru hattı) ════

    [Fact]
    public async Task B56_tahsilatli_cihazda_PATCH_kasa_409_doner_ve_GOC_ucunu_gosterir()
    {
        var k = YeniKiraci("P22H-B56");
        var cihazId = PosYardim.CihazYaz(f.Vt, k);
        PosYardim.TahsilatYaz(f.Vt, k, cihazId, 750m);
        var yeniKasa = TestKurulum.KasaEkle(f.Vt.OlusturSistem(), k.TenantId, k.SubeId, "TRY");

        var sahip = f.Ortam.Istemci(k);
        var y = await sahip.PatchAsJsonAsync($"/api/v1/pos/cihazlar/{cihazId}", new { kasaId = yeniKasa });
        Assert.Equal(HttpStatusCode.Conflict, y.StatusCode);
        var govde = await y.Content.ReadAsStringAsync();
        Assert.Contains("kasa-goc", govde, StringComparison.OrdinalIgnoreCase);

        using var oku = f.Vt.OlusturSistem();
        Assert.NotEqual(yeniKasa, oku.PosCihazlari.AsNoTracking().Single(c => c.Id == cihazId).KasaId);
    }

    // ═══ B-55 · GÖÇ AKIŞI — yetki + gerekçe + tutanak ══════════════════════

    [Fact]
    public async Task B55_tezgahtar_kasa_gocu_yapamaz_sahip_yapar()
    {
        var k = YeniKiraci("P22H-B55");
        var cihazId = PosYardim.CihazYaz(f.Vt, k);
        PosYardim.TahsilatYaz(f.Vt, k, cihazId, 500m);
        var yeniKasa = TestKurulum.KasaEkle(f.Vt.OlusturSistem(), k.TenantId, k.SubeId, "TRY");

        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");
        var red = await tezgahtar.PostAsJsonAsync($"/api/v1/pos/cihazlar/{cihazId}/kasa-goc",
            new { yeniKasaId = yeniKasa, gerekce = "cihaz yanlış kasaya bağlanmıştı, taşındı" });
        Assert.Equal(HttpStatusCode.Forbidden, red.StatusCode);

        var sahip = f.Ortam.Istemci(k);
        var ok = await sahip.PostAsJsonAsync($"/api/v1/pos/cihazlar/{cihazId}/kasa-goc",
            new { yeniKasaId = yeniKasa, gerekce = "cihaz yanlış kasaya bağlanmıştı, taşındı" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var j = await ok.Json();
        Assert.Equal(1, j.GetProperty("tahsilatSayisi").GetInt32());
        Assert.True(j.GetProperty("gecmisEtkilenmedi").GetBoolean());

        // Tutanak okuma ucundan GÖRÜNÜR (şart 4).
        var gocler = await (await sahip.GetAsync($"/api/v1/pos/cihazlar/{cihazId}/kasa-gocleri")).Json();
        Assert.Equal(1, gocler.GetArrayLength());
        Assert.Equal(yeniKasa, gocler[0].GetProperty("yeniKasaId").GetString());
    }

    // ═══ B-3 · TASLAK FATURA DÜZENLEME UCU (boru hattı + rol) ══════════════

    [Fact]
    public async Task B3_taslak_fatura_PATCH_ucu_calisir_ve_TEZGAHTARa_kapalidir()
    {
        var k = YeniKiraci("P22H-B3");
        var sahip = f.Ortam.Istemci(k);

        var olustur = await sahip.PostAsJsonAsync("/api/v1/faturalar",
            new { cariId = k.CariId, tur = "SATIS", toplam = 1200m, kdvOran = 0.20m });
        Assert.Equal(HttpStatusCode.OK, olustur.StatusCode);
        var fj = await olustur.Json();
        var id = fj.GetProperty("id").GetString()!;
        Assert.Equal(JsonValueKind.Null, fj.GetProperty("faturaNo").ValueKind);

        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await tezgahtar.PatchAsJsonAsync($"/api/v1/faturalar/{id}", new { aciklama = "olmaz" })).StatusCode);

        var y = await sahip.PatchAsJsonAsync($"/api/v1/faturalar/{id}", new { aciklama = "düzeltildi" });
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        Assert.Equal("düzeltildi", (await y.Json()).GetProperty("aciklama").GetString());

        // Kesim numarayı ŞİMDİ verir.
        var kes = await sahip.PatchAsJsonAsync($"/api/v1/faturalar/{id}/durum", new { durum = "KESILDI" });
        Assert.Equal(HttpStatusCode.OK, kes.StatusCode);
        Assert.StartsWith("FTR-", (await kes.Json()).GetProperty("faturaNo").GetString());
    }

    // ═══ B-44 · SAYIM OTURUMU ADI (boru hattı) ═════════════════════════════

    [Fact]
    public async Task B44_sayim_adi_PATCH_ucundan_duzeltilir()
    {
        var k = YeniKiraci("P22H-B44");
        var sahip = f.Ortam.Istemci(k);

        var ac = await sahip.PostAsJsonAsync("/api/v1/sayim",
            new { ad = "Vitirn", subeId = k.SubeId, kapsamTur = "TUMU" });
        Assert.Equal(HttpStatusCode.OK, ac.StatusCode);
        var id = (await ac.Json()).GetProperty("id").GetString()!;

        var y = await sahip.PatchAsJsonAsync($"/api/v1/sayim/{id}", new { ad = "Vitrin sayımı" });
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        Assert.Equal("Vitrin sayımı", (await y.Json()).GetProperty("ad").GetString());
    }
}
