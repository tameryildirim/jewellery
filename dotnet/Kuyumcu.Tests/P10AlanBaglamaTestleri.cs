using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Urun;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-10 (Y-3, Y-8) — SUNUCUNUN KABUL ETTİĞİ ALANLARIN SUNUCU TARAFI
//
// Bu dosya yalnız SUNUCU sözleşmesini kanıtlar (saf kapı + gerçek HTTP):
//   • GorselKapisi  — kapalı şema listesi (http/https), boş = temizle
//   • PATCH/POST    — beş alanın yazıldığı, boşun SİLDİĞİ, gönderilmeyenin
//                     KORUNDUĞU (PATCH sözleşmesi)
//
// Ekran tarafı P10EkranTestleri.cs dosyasındadır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GorselKapisiTestleri
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Bos_adres_NULL_olur(string? ham)
    {
        // "Boş dizge = temizle": kolona "" yazılırsa kart "adres var ama boş"
        // durumuna düşer ve galeri kırık resim çizer.
        Assert.Null(GorselKapisi.Dogrula(ham));
        Assert.Null(GorselKapisi.Bosla(ham));
    }

    [Theory]
    [InlineData("http://ornek.test/yuzuk.jpg")]
    [InlineData("https://ornek.test/yuzuk.png")]
    public void Http_ve_https_KABUL_edilir(string adres)
        => Assert.Equal(adres, GorselKapisi.Dogrula(adres));

    [Fact]
    public void Bastaki_ve_sondaki_bosluk_KIRPILIR()
        => Assert.Equal("https://ornek.test/a.jpg",
                        GorselKapisi.Dogrula("  https://ornek.test/a.jpg  "));

    [Theory]
    [InlineData("data:image/png;base64,iVBORw0KGgo=")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://ornek.test/a.jpg")]
    [InlineData("/yerel/yol/a.jpg")]
    [InlineData("ornek.test/a.jpg")]
    public void Kapali_liste_disindaki_sema_REDDEDILIR(string adres)
    {
        var h = Assert.Throws<BadRequestException>(() => GorselKapisi.Dogrula(adres));
        Assert.Contains("http://", h.Message);
    }

    [Fact]
    public void Cok_uzun_adres_hata_mesajinda_KISALTILIR()
    {
        var uzun = "ftp://" + new string('a', 200);
        var h = Assert.Throws<BadRequestException>(() => GorselKapisi.Dogrula(uzun));
        Assert.DoesNotContain(new string('a', 200), h.Message);
        Assert.Contains("…", h.Message);
    }
}

[Collection("http")]
public sealed class HttpP10AlanTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private async Task<Kuyumcu.Api.Data.Urun> UrunOku(Kiraci k, string urunId)
    {
        using var db = f.Vt.OlusturSistem();
        return await db.Urunler.AsNoTracking()
            .SingleAsync(u => u.Id == urunId && u.TenantId == k.TenantId);
    }

    private static StringContent Ham(string json)
        => new(json, System.Text.Encoding.UTF8, "application/json");

    // ── OLUŞTURMA: beş alan da yazılır (UX-S26) ──────────────────────────────

    [Fact]
    public async Task POST_urunler_BES_ALANI_da_kaydeder()
    {
        var k = YeniKiraci("P10 olustur");
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/urunler", new
        {
            tip = "ZIYNET", kod = "P10-001", ad = "P10 Bilezik", ayarId = "K22",
            gramAgirlik = 10.0m, birim = "ADET", tasAgirlik = 0m, tasKokeni = "TAS_YOK",
            aciklama = "Vitrin 2 numara",
            rfid = "RF-0001",
            kmtsKodu = "KMTS-77",
            gorselUrl = "https://ornek.test/bilezik.jpg",
            pariteKodu = "BILEZIK_22",
        });

        Assert.Equal(HttpStatusCode.Created, cevap.StatusCode);
        var govde = await cevap.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var id = govde.GetProperty("id").GetString()!;

        var u = await UrunOku(k, id);
        Assert.Equal("Vitrin 2 numara", u.Aciklama);
        Assert.Equal("RF-0001", u.Rfid);
        Assert.Equal("KMTS-77", u.KmtsKodu);
        Assert.Equal("https://ornek.test/bilezik.jpg", u.GorselUrl);
        Assert.Equal("BILEZIK_22", u.PariteKodu);
    }

    [Fact]
    public async Task POST_urunler_GECERSIZ_gorsel_adresini_400_ile_reddeder()
    {
        var k = YeniKiraci("P10 olustur gecersiz");
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/urunler", new
        {
            tip = "ZIYNET", kod = "P10-002", ad = "P10 Kolye", ayarId = "K22",
            gramAgirlik = 5.0m, birim = "ADET", tasAgirlik = 0m, tasKokeni = "TAS_YOK",
            gorselUrl = "javascript:alert(1)",
        });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.Urunler.AnyAsync(u => u.Kod == "P10-002" && u.TenantId == k.TenantId));
    }

    [Fact]
    public async Task POST_urunler_BOS_rfid_ve_gorseli_NULL_yazar()
    {
        var k = YeniKiraci("P10 olustur bos");
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/urunler", new
        {
            tip = "ZIYNET", kod = "P10-003", ad = "P10 Yüzük", ayarId = "K22",
            gramAgirlik = 3.0m, birim = "ADET", tasAgirlik = 0m, tasKokeni = "TAS_YOK",
            aciklama = "  ", rfid = "", gorselUrl = "",
        });

        Assert.Equal(HttpStatusCode.Created, cevap.StatusCode);
        var id = (await cevap.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetString()!;
        var u = await UrunOku(k, id);
        Assert.Null(u.Aciklama);
        Assert.Null(u.Rfid);
        Assert.Null(u.GorselUrl);
    }

    // ── PATCH: gönderilmeyen KORUNUR, boş SİLER ──────────────────────────────

    [Fact]
    public async Task PATCH_gonderilmeyen_alan_KORUNUR()
    {
        var k = YeniKiraci("P10 patch koru");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await c.PatchAsync($"/api/v1/urunler/{k.UrunId}",
            Ham("""{"rfid":"RF-KORU","gorselUrl":"https://ornek.test/a.jpg","kmtsKodu":"KMTS-9"}"""));

        var cevap = await c.PatchAsync($"/api/v1/urunler/{k.UrunId}", Ham("""{"ad":"Yalnız ad"}"""));

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var u = await UrunOku(k, k.UrunId);
        Assert.Equal("RF-KORU", u.Rfid);
        Assert.Equal("https://ornek.test/a.jpg", u.GorselUrl);
        Assert.Equal("KMTS-9", u.KmtsKodu);
    }

    [Fact]
    public async Task PATCH_BOS_gonderilen_rfid_ve_gorsel_SILINIR()
    {
        var k = YeniKiraci("P10 patch sil");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await c.PatchAsync($"/api/v1/urunler/{k.UrunId}",
            Ham("""{"rfid":"RF-1","gorselUrl":"https://ornek.test/a.jpg","aciklama":"not"}"""));

        // Ekranın gerçekte gönderdiği gövde: kullanıcı alanı boşalttı.
        var cevap = await c.PatchAsync($"/api/v1/urunler/{k.UrunId}",
            Ham("""{"rfid":"","gorselUrl":"","aciklama":""}"""));

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var u = await UrunOku(k, k.UrunId);
        Assert.Null(u.Rfid);
        Assert.Null(u.GorselUrl);
        Assert.Null(u.Aciklama);
    }

    [Fact]
    public async Task PATCH_GECERSIZ_gorsel_adresi_400_ve_eski_deger_DURUR()
    {
        var k = YeniKiraci("P10 patch gecersiz");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await c.PatchAsync($"/api/v1/urunler/{k.UrunId}",
            Ham("""{"gorselUrl":"https://ornek.test/eski.jpg"}"""));

        var cevap = await c.PatchAsync($"/api/v1/urunler/{k.UrunId}",
            Ham("""{"gorselUrl":"data:image/png;base64,AAAA"}"""));

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Equal("https://ornek.test/eski.jpg", (await UrunOku(k, k.UrunId)).GorselUrl);
    }

    // ── GET: ekranın okuduğu alanlar yanıtta GERÇEKTEN var ───────────────────

    [Fact]
    public async Task GET_urun_tam_karti_BES_ALANI_da_dondurur()
    {
        var k = YeniKiraci("P10 tam kart");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await c.PatchAsync($"/api/v1/urunler/{k.UrunId}",
            Ham("""{"rfid":"RF-TK","kmtsKodu":"KMTS-TK","gorselUrl":"https://ornek.test/tk.jpg","aciklama":"tk","pariteKodu":"BILEZIK_22"}"""));

        var y = await c.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/v1/urunler/{k.UrunId}");

        // camelCase sözleşmesi: istemci tipi (UrunTamKart) bu adları okur.
        Assert.Equal("RF-TK", y.GetProperty("rfid").GetString());
        Assert.Equal("KMTS-TK", y.GetProperty("kmtsKodu").GetString());
        Assert.Equal("https://ornek.test/tk.jpg", y.GetProperty("gorselUrl").GetString());
        Assert.Equal("tk", y.GetProperty("aciklama").GetString());
        Assert.Equal("BILEZIK_22", y.GetProperty("pariteKodu").GetString());
    }

    // ── Cari notu (Y-3, yedinci alan) ────────────────────────────────────────

    [Fact]
    public async Task Cari_NOTLARI_yazilir_korunur_ve_silinir()
    {
        var k = YeniKiraci("P10 cari not");
        var c = f.Ortam.Istemci(k, "SAHIP");

        var olustur = await c.PostAsJsonAsync("/api/v1/cari",
            new { ad = "P10 Müşteri", tur = "MUSTERI", notlar = "Pazarlıkta zorlu" });
        Assert.Equal(HttpStatusCode.Created, olustur.StatusCode);
        var id = (await olustur.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetString()!;

        async Task<string?> NotOku()
        {
            var y = await c.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/v1/cari/{id}");
            return y.GetProperty("notlar").ValueKind == System.Text.Json.JsonValueKind.Null
                ? null : y.GetProperty("notlar").GetString();
        }

        Assert.Equal("Pazarlıkta zorlu", await NotOku());

        // Gönderilmeyen alan KORUNUR (CariGuncelleDto.NotlarVerildi).
        await c.PatchAsync($"/api/v1/cari/{id}", Ham("""{"ad":"P10 Müşteri 2"}"""));
        Assert.Equal("Pazarlıkta zorlu", await NotOku());

        // null GÖNDERİLDİ → SİL.
        await c.PatchAsync($"/api/v1/cari/{id}", Ham("""{"notlar":null}"""));
        Assert.Null(await NotOku());
    }
}
