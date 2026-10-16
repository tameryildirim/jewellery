using System.Net;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// /api/v1/meta/surum — paket T-4 Faz A.
//
// Sınanan sözleşme:
//   • migration sayısı __EFMigrationsHistory ile BİREBİR aynıdır (sabit değil)
//   • gövde sır / bağlantı dizesi / makine adı / dosya yolu SIZDIRMAZ
//   • ayrıntı ucu SAHIP'e açık, TEZGAHTAR'a 403
//   • kimliksiz istek 401 (uç anonim DEĞİL — /saglik o işi görür)
//
// ⚠ 60 sn ÖNBELLEK NOTU: künye süreç genelinde 60 sn önbelleklenir ve önbellek
//   anahtarı kiracıya göre AYRIŞMAZ (şema tüm kiracılarda tektir). Bu testler
//   koşarken migration UYGULANMAZ — şema TestVeritabani tarafından süreç
//   başında bir kez kurulur — bu yüzden önbellek ile sayım çakışamaz.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class SurumUcuTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    [Fact]
    public async Task Surum_ucu_200_doner_ve_migration_sayisi_EFMigrationsHistory_ile_ayni()
    {
        var k = YeniKiraci("T4 sürüm sayım");
        var cevap = await f.Ortam.Istemci(k).GetAsync("/api/v1/meta/surum");

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var json = await cevap.Json();

        using var db = f.Vt.OlusturSistem();
        var gercek = (await db.Database.GetAppliedMigrationsAsync()).ToList();

        Assert.Equal(gercek.Count, json.GetProperty("migrationSayisi").GetInt32());
        Assert.Equal(gercek.Last(), json.GetProperty("sonMigration").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("surum").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("derlemeZamani").GetString()));
    }

    [Fact]
    public async Task Surum_ucu_sir_veya_baglanti_dizesi_icermez()
    {
        var k = YeniKiraci("T4 sürüm sızıntı");
        var cevap = await f.Ortam.Istemci(k).GetAsync("/api/v1/meta/surum");
        var metin = await cevap.Metin();

        foreach (var yasak in new[]
                 {
                     "password", "Password", "server=", "Server=", "user=", "Uid=",
                     "Secret", "secret", "kuyumcu_test", "/Users/", "C:\\\\", ".dll",
                     HttpOrtami.TestSirri,
                 })
            Assert.DoesNotContain(yasak, metin);

        // Ortam YALNIZ iki dizgeden biri olabilir; özel ortam adı ("Testing")
        // dışarı çıkmaz — dağıtım topolojisi de bilgidir.
        var ortam = (await cevap.Json()).GetProperty("ortam").GetString();
        Assert.Contains(ortam, new[] { "Production", "Development" });
    }

    [Fact]
    public async Task Kimlik_dogrulanmamis_istek_401()
    {
        var cevap = await f.Ortam.AnonimIstemci().GetAsync("/api/v1/meta/surum");
        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
    }

    [Fact]
    public async Task Ayrinti_ucu_TEZGAHTAR_icin_403()
    {
        var k = YeniKiraci("T4 ayrıntı tezgâhtar");
        var cevap = await f.Ortam
            .Istemci(k.KullaniciId, k.TenantId, k.SubeId, "TEZGAHTAR")
            .GetAsync("/api/v1/meta/surum/ayrinti");

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    [Fact]
    public async Task Ayrinti_ucu_SAHIP_icin_uygulanmis_ve_bekleyen_listeleri_doner()
    {
        var k = YeniKiraci("T4 ayrıntı sahip");
        var cevap = await f.Ortam.Istemci(k).GetAsync("/api/v1/meta/surum/ayrinti");

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var json = await cevap.Json();

        using var db = f.Vt.OlusturSistem();
        var uygulanmis = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        var bekleyen = (await db.Database.GetPendingMigrationsAsync()).ToList();

        Assert.Equal(uygulanmis.Count, json.GetProperty("uygulanmis").GetArrayLength());
        Assert.Equal(bekleyen.Count, json.GetProperty("bekleyen").GetArrayLength());
        // Bekleyen yoksa uyarı NULL; varsa dolu (sessiz geçilmez).
        Assert.Equal(bekleyen.Count == 0,
            json.GetProperty("uyari").ValueKind == System.Text.Json.JsonValueKind.Null);
        // Kapı özeti YALNIZ bool taşır (mühür koşulu): ortam değişkeni adı/değeri yok.
        foreach (var alan in json.GetProperty("kapilar").EnumerateObject())
            Assert.Contains(alan.Value.ValueKind,
                new[] { System.Text.Json.JsonValueKind.True, System.Text.Json.JsonValueKind.False });
    }

    [Fact]
    public async Task Ayrinti_ucu_denetim_kaydi_dusurur()
    {
        var k = YeniKiraci("T4 ayrıntı denetim");
        var cevap = await f.Ortam.Istemci(k).GetAsync("/api/v1/meta/surum/ayrinti");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var db = f.Vt.Olustur(k.TenantId);
        Assert.True(await db.DenetimKayitlari.AnyAsync(d => d.VarlikTuru == "Surum"));
    }
}
