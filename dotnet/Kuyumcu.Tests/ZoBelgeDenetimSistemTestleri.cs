using System.Net;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-BELGE (tasarım hakemi t3, 29.09) — "Kim ne yaptı" BASİT yüzü sistem
// kayıtlarını (kullanıcısız satır, telemetri, kişisel tercih …) SUNUCUDA süzer.
// `sistemHaric` isteğe bağlı, varsayılan KAPALI: mevcut çağrılar DEĞİŞMEZ.
// Liste ve özet aynı süzgeç nesnesinden geçer (P-24) — iki uç aynı kümeyi sayar.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class ZoBelgeDenetimSistemTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private void Yaz(Kiraci k, string? kullaniciId, string varlikTuru, string isaret)
    {
        using var db = f.Vt.Olustur(k.TenantId);
        db.DenetimKayitlari.Add(DenetimServisi.Olustur(
            k.TenantId, kullaniciId, DenetimEylemi.Guncelleme, varlikTuru, isaret, "Ad, Guncellendi", null));
        db.SaveChanges();
    }

    [Fact]
    public async Task ZBH01_sistemHaric_kullanicisiz_ve_sistem_turlerini_SUZER_varsayilan_hepsini_doner()
    {
        var k = YeniKiraci("ZOBELGE sistem haric");
        Yaz(k, k.KullaniciId, "Cari", "zbh-insan");
        Yaz(k, k.KullaniciId, "TelemetriOlcumu", "zbh-telemetri");
        Yaz(k, k.KullaniciId, "KullaniciTercihi", "zbh-tercih");
        Yaz(k, null, "Cari", "zbh-sistem");

        var istemci = f.Ortam.Istemci(k, "SAHIP");

        // Özet ÖNCE: liste ucu kendi okuma izini (DenetimKaydi) yazar; sıra tersse
        // özet o izi de sayardı.
        var oz = await (await istemci.GetAsync("/api/v1/denetim/ozet?sistemHaric=true")).Json();

        var suzulmus = await istemci.GetAsync("/api/v1/denetim?sistemHaric=true&limit=500");
        Assert.Equal(HttpStatusCode.OK, suzulmus.StatusCode);
        var s = await suzulmus.Json();
        var sKimlikler = s.GetProperty("kayitlar").EnumerateArray()
            .Select(e => e.GetProperty("varlikId").GetString()).ToList();
        Assert.Contains("zbh-insan", sKimlikler);
        Assert.DoesNotContain("zbh-telemetri", sKimlikler);
        Assert.DoesNotContain("zbh-tercih", sKimlikler);
        Assert.DoesNotContain("zbh-sistem", sKimlikler);
        Assert.All(s.GetProperty("kayitlar").EnumerateArray(),
            e => Assert.NotEqual(System.Text.Json.JsonValueKind.Null, e.GetProperty("kullaniciId").ValueKind));

        // Özet AYNI kümeyi sayar (P-24).
        Assert.Equal(s.GetProperty("toplam").GetInt32(), oz.GetProperty("toplam").GetInt32());
        Assert.DoesNotContain(oz.GetProperty("turler").EnumerateArray(),
            t => t.GetProperty("varlikTuru").GetString() is "TelemetriOlcumu" or "KullaniciTercihi");

        // Varsayılan (parametresiz) davranış DEĞİŞMEDİ: dördü de listede.
        var hepsi = await (await istemci.GetAsync("/api/v1/denetim?limit=500")).Json();
        var hKimlikler = hepsi.GetProperty("kayitlar").EnumerateArray()
            .Select(e => e.GetProperty("varlikId").GetString()).ToList();
        foreach (var i in new[] { "zbh-insan", "zbh-telemetri", "zbh-tercih", "zbh-sistem" })
            Assert.Contains(i, hKimlikler);
        Assert.True(hepsi.GetProperty("toplam").GetInt32() > s.GetProperty("toplam").GetInt32());
    }
}
