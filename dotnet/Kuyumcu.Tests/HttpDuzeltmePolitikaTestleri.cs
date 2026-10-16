using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Modules.Meta;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// /api/v1/meta/duzeltme-politikalari — HTTP KAPISI (paket UX-1 Faz A).
//
// Mehmet K8: uç [Authorize] altında kalır — harita bir UÇ ENVANTERİDİR,
// anonim istemciye API yüzeyinin dökümü verilmez.
// Sözleşme camelCase'dir (Tuzaklar: "API yanıtı camelCase").
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class HttpDuzeltmePolitikaTestleri(HttpFikstur f)
{
    private const string Yol = "/api/v1/meta/duzeltme-politikalari";

    [Fact]
    public async Task Anonim_istemci_401_alir()
    {
        var cevap = await f.Ortam.AnonimIstemci().GetAsync(Yol);
        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
    }

    [Fact]
    public async Task TEZGAHTAR_haritayi_okuyabilir_ve_sozlesme_camelCase_tir()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "UX1 politika");

        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync(Yol);
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        var govde = await cevap.Json();
        var varliklar = govde.GetProperty("varliklar").EnumerateArray().ToList();
        var turler = govde.GetProperty("hareketTurleri").EnumerateArray().ToList();

        Assert.Equal(DuzeltmePolitikasi.Varliklar.Count, varliklar.Count);
        Assert.Equal(DuzeltmePolitikasi.HareketTurleri.Count, turler.Count);

        var denetim = varliklar.Single(v => v.GetProperty("varlik").GetString() == "DenetimKaydi");
        Assert.Equal("SALT_OKUNUR", denetim.GetProperty("politika").GetString());
        Assert.Equal("DENETIM", denetim.GetProperty("gerekceAnahtari").GetString());
        Assert.Equal(JsonValueKind.Null, denetim.GetProperty("hedefUc").ValueKind);

        var satis = turler.Single(v => v.GetProperty("varlik").GetString() == "SATIS");
        Assert.Equal("STORNO", satis.GetProperty("politika").GetString());
        Assert.Equal("api/v1/islemler/satis-iade", satis.GetProperty("hedefUc").GetString());

        // Harita kiracıya özel veri TAŞIMAZ.
        Assert.DoesNotContain(k.TenantId, await cevap.Metin());
    }
}
