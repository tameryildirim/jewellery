using System.Net;
using System.Text;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PATCH-3 — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT (üretim kodundan
// hesaplanmaz). Paketin dokunduğu yollar: satış alış fişi kalem adedi / rezerv
// adedi (önce yuvarla sonra ≤ 0), rezerv PATCH tür kapısı, satış yazma uçlarının
// taşma ağı (ödeme bacakları dahil), ürün/şube/kategori PATCH üç durumlu sözleşme,
// tek 6 hane yuvarlama kuralı. Sınırlar: 0 · eksi · yarım (0,0025 → 0,003
// AwayFromZero; ToEven 0,002 verirdi) · kuruş 0,005 → 0,01 · has 6 hane ·
// gram 4 hane (0,0001) · çok büyük (999.999.999,99) · null · Türkçe dize.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class PATCH3SinirHttpTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"PATCH3SINIR {ad}");
    }

    private static Task<HttpResponseMessage> Ham(HttpClient c, HttpMethod m, string yol, string json) =>
        c.SendAsync(new HttpRequestMessage(m, yol) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    private static async Task Alan400(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var alanlar = JsonDocument.Parse(govde).RootElement.GetProperty("alanlar");
        Assert.Contains(alanlar.EnumerateObject().Select(p => p.Name), a => a == alan || a.Split('.')[^1] == alan);
    }

    private async Task<string> RezervKur(Kiraci k)
    {
        var r = await f.Ortam.Istemci(k).PostJson("/api/v1/islemler/rezervler",
            new { urunId = k.UrunId, cariId = k.CariId, adet = 3m });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        using var db = f.Vt.OlusturSistem();
        return db.Rezervasyonlar.AsNoTracking().Single(x => x.TenantId == k.TenantId).Id;
    }

    private (decimal Adet, decimal Bloke) Rezerv(Kiraci k, string id)
    {
        using var db = f.Vt.OlusturSistem();
        return (db.Rezervasyonlar.AsNoTracking().Single(x => x.Id == id).Adet,
                db.Stoklar.AsNoTracking().Single(s => s.TenantId == k.TenantId).BlokeAdet);
    }

    // ── sıfır · eksi · Türkçe dize · çok büyük — rezerv PATCH: 400 alan:adet, SQL DEĞİŞMEZ ──
    [Theory]
    [InlineData("""{"adet":0}""")]
    [InlineData("""{"adet":-1}""")]
    [InlineData("""{"adet":-0.0004}""")]
    [InlineData("""{"adet":"1,5"}""")]
    [InlineData("""{"adet":"45.000"}""")]
    [InlineData("""{"adet":999999999.99}""")]
    [InlineData("""{"adet":79228162514264337593543950336}""")]
    public async Task S_rezerv_PATCH_sinir_disi_adet_400_alan_SQL_degismez(string json)
    {
        var k = Kur("rezerv sinir");
        var id = await RezervKur(k);
        var once = Rezerv(k, id);
        await Alan400(await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/islemler/rezervler/{id}", json), "adet");
        Assert.Equal((3m, once.Bloke), Rezerv(k, id));
    }

    // ── yarım bin: 2,0025 → 2,003 (AwayFromZero), bloke FARK kadar oynar ──
    [Fact]
    public async Task S_rezerv_PATCH_yarim_bin_2_0025_AwayFromZero_2_003()
    {
        var k = Kur("rezerv yarim");
        using (var g = f.Vt.OlusturSistem()) TestKurulum.UrunuGramBirimiYap(g, k.UrunId);   // PAKET GOC-C (K14): kesir yalnız GRAM birimde — yuvarlama sınırı orada ölçülür
        var id = await RezervKur(k);
        var once = Rezerv(k, id);
        var r = await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/islemler/rezervler/{id}", """{"adet":2.0025}""");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal((2.003m, once.Bloke - 3m + 2.003m), Rezerv(k, id));
    }

    // ── kuruş/hane: alış fişi 0,0025 adet × 1000 TL → adet 0,003, tutar 3,00 ──
    [Fact]
    public async Task S_alis_fisi_adet_0_0025_yarim_AwayFromZero_0_003_tutar_3_00()
    {
        var k = Kur("alis yarim");
        using (var g = f.Vt.OlusturSistem()) TestKurulum.UrunuGramBirimiYap(g, k.UrunId);   // PAKET GOC-C (K14): kesir yalnız GRAM birimde — yuvarlama sınırı orada ölçülür
        var r = await f.Ortam.Istemci(k).PostJson("/api/v1/islemler/alis-fisi", new
        {
            cariId = k.CariId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 0.0025m, birimFiyatTl = 1000m } },
            odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId,
        });
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        using var db = f.Vt.OlusturSistem();
        var h = db.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "ALIS");
        Assert.Equal(0.003m, h.Adet);
        Assert.Equal(3.00m, Math.Abs(h.TlTutari));
    }

    // ── kuruş yarımı: 1 adet × 0,005 TL → 0,01 (AwayFromZero; ToEven 0,00 verirdi) ──
    [Fact]
    public async Task S_alis_fisi_kurus_yarimi_0_005_AwayFromZero_0_01()
    {
        var k = Kur("alis kurus");
        var r = await f.Ortam.Istemci(k).PostJson("/api/v1/islemler/alis-fisi", new
        {
            cariId = k.CariId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 0.005m } },
            odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId,
        });
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        using var db = f.Vt.OlusturSistem();
        var h = db.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "ALIS");
        Assert.Equal(0.01m, Math.Abs(h.TlTutari));
    }

    // ── sıfır adet alış fişi kalemi: 400, hiç satır yazılmaz ──
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task S_alis_fisi_sifir_eksi_adet_400_hareket_yok(double adet)
    {
        var k = Kur("alis sifir");
        var r = await f.Ortam.Istemci(k).PostJson("/api/v1/islemler/alis-fisi", new
        {
            cariId = k.CariId,
            kalemler = new[] { new { urunId = k.UrunId, adet = (decimal)adet, birimFiyatTl = 10m } },
            odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId,
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.Hareketler.Count(h => h.TenantId == k.TenantId));
    }

    // ── ödeme bacağı taşması (Satış modülü yazma ucu; DTO aralığı double.MaxValue) → 500 OLMAZ ──
    [Theory]
    [InlineData("""{"arac":"NAKIT","tutarTl":79228162514264337593543950335,"kasaId":"@K"}""")]
    [InlineData("""{"arac":"NAKIT","tutarTl":50000000000000000000000000000,"kasaId":"@K"},{"arac":"NAKIT","tutarTl":50000000000000000000000000000,"kasaId":"@K"}""")]
    [InlineData("""{"arac":"DOVIZ","tutarTl":1000,"paraBirimi":"USD","dovizTutar":79228162514264337593543950335,"kasaId":"@K"}""")]
    [InlineData("""{"arac":"ALTIN","tutarTl":1000,"ayarId":"K22","gramAgirlik":79228162514264337593543950335,"birimFiyatTlHasGram":1000}""")]
    public async Task S_satis_fisi_odeme_bacagi_dev_sayi_500_DEGIL_hareket_yok(string bacaklar)
    {
        var k = Kur("satis fisi tasma");
        var json = "{\"kalemler\":[{\"urunId\":\"" + k.UrunId + "\",\"adet\":1,\"birimFiyatTl\":1000}],"
                 + "\"odemeler\":[" + bacaklar.Replace("@K", k.KasaId) + "],\"kurSnapshotId\":\"" + k.KurId + "\"}";
        var r = await Ham(f.Ortam.Istemci(k), HttpMethod.Post, "/api/v1/islemler/satis-fisi", json);
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.Hareketler.Count(h => h.TenantId == k.TenantId));
    }

    // ── ürün PATCH: çok büyük satış fiyatı 999.999.999,99 birebir · 0 = sabit fiyatı SİL ──
    [Fact]
    public async Task S_urun_PATCH_satis_fiyati_999999999_99_birebir_0_ise_NULL()
    {
        var k = Kur("urun buyuk");
        var c = f.Ortam.Istemci(k);
        var r = await c.PatchJson($"/api/v1/urunler/{k.UrunId}", new { satisFiyati = 999_999_999.99m });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(999_999_999.99m, db.Urunler.AsNoTracking().Single(u => u.Id == k.UrunId).SatisFiyati);
        r = await c.PatchJson($"/api/v1/urunler/{k.UrunId}", new { satisFiyati = 0m });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.Null(db.Urunler.AsNoTracking().Single(u => u.Id == k.UrunId).SatisFiyati);
    }

    // ── gram 4 hane: 0,0001 kabul ve birebir; 0,00004 (4 haneye sığmaz → sıfır) RET ──
    [Fact]
    public async Task S_urun_PATCH_gram_0_0001_birebir_0_00004_RET()
    {
        var k = Kur("urun gram");
        var c = f.Ortam.Istemci(k);
        var r = await c.PatchJson($"/api/v1/urunler/{k.UrunId}", new { gramAgirlik = 0.0001m });
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(0.0001m, db.Urunler.AsNoTracking().Single(u => u.Id == k.UrunId).GramAgirlik);
        await Alan400(await c.PatchJson($"/api/v1/urunler/{k.UrunId}", new { gramAgirlik = 0.00004m }), "gramAgirlik");
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(0.0001m, db.Urunler.AsNoTracking().Single(u => u.Id == k.UrunId).GramAgirlik);
    }

    // ── null / boş: ürün PATCH tırnaklı Türkçe sayı 400, işçilik null KORUR ──
    [Theory]
    [InlineData("""{"satisFiyati":"45.000"}""", "satisFiyati")]
    [InlineData("""{"iscilikTl":"1,5"}""", "iscilikTl")]
    public async Task S_urun_PATCH_Turkce_dize_sayi_400_alan(string json, string alan)
    {
        var k = Kur("urun dize");
        await Alan400(await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/urunler/{k.UrunId}", json), alan);
    }

    [Fact]
    public async Task S_urun_PATCH_iscilik_null_KORUR_aciklama_null_SILER()
    {
        var k = Kur("urun null");
        using (var db = f.Vt.OlusturSistem())
        {
            var u = db.Urunler.Single(x => x.Id == k.UrunId);
            u.IscilikTl = 123.45m; u.Aciklama = "Sil beni";
            db.SaveChanges();
        }
        var r = await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/urunler/{k.UrunId}",
            """{"iscilikTl":null,"aciklama":null}""");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        using var o = f.Vt.OlusturSistem();
        var s = o.Urunler.AsNoTracking().Single(x => x.Id == k.UrunId);
        Assert.Equal(123.45m, s.IscilikTl);
        Assert.Null(s.Aciklama);
    }

    // ── şube: ad "" ve "   " → 400 alan:ad, ad DEĞİŞMEZ ──
    [Theory]
    [InlineData("""{"ad":""}""")]
    [InlineData("""{"ad":"   "}""")]
    public async Task S_sube_PATCH_bos_ad_400_alan_ad_degismez(string json)
    {
        var k = Kur("sube bos");
        string once;
        using (var db = f.Vt.OlusturSistem()) once = db.Subeler.AsNoTracking().Single(x => x.Id == k.SubeId).Ad;
        await Alan400(await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/subeler/{k.SubeId}", json), "ad");
        using var o = f.Vt.OlusturSistem();
        Assert.Equal(once, o.Subeler.AsNoTracking().Single(x => x.Id == k.SubeId).Ad);
    }

    // ── kategori: ad "" → 400 alan:ad; ustId kendisi → reddedilir (döngü) ──
    [Fact]
    public async Task S_kategori_PATCH_bos_ad_400_ve_kendine_ust_RET()
    {
        var k = Kur("kategori");
        var c = f.Ortam.Istemci(k);
        var y = await c.PostJson("/api/v1/urun-kategorileri", new { ad = "Yüzük", sira = 0 });
        Assert.True(y.IsSuccessStatusCode, await y.Content.ReadAsStringAsync());
        var id = JsonDocument.Parse(await y.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetString()!;
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/urun-kategorileri/{id}", """{"ad":""}"""), "ad");
        var r = await Ham(c, HttpMethod.Patch, $"/api/v1/urun-kategorileri/{id}", "{\"ustId\":\"" + id + "\"}");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        using var o = f.Vt.OlusturSistem();
        var kat = o.Set<UrunKategorisi>().AsNoTracking().Single(x => x.Id == id);
        Assert.Equal(("Yüzük", (string?)null), (kat.Ad, kat.UstId));
    }
}

// ═══════════ Saf sınırlar — tek yuvarlama kuralları ve Türkçe giriş çözücüsü ═══════════
public sealed class PATCH3SinirSafTestleri
{
    [Theory]
    [InlineData("0.0000005", "0.000001")]     // has 6 hane yarımı yukarı
    [InlineData("0.0000004", "0.000000")]
    [InlineData("-0.0000005", "-0.000001")]   // eksi: sıfırdan uzağa
    [InlineData("999999999.9999995", "1000000000.000000")]
    public void S_Yuvarla6_AwayFromZero(string g, string b) =>
        Assert.Equal(decimal.Parse(b, System.Globalization.CultureInfo.InvariantCulture),
            KolonTavani.Yuvarla6(decimal.Parse(g, System.Globalization.CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData("0.0005", "0.001")]
    [InlineData("0.0004", "0.000")]
    [InlineData("0.0025", "0.003")]
    [InlineData("-0.0025", "-0.003")]
    public void S_Yuvarla3_AwayFromZero(string g, string b) =>
        Assert.Equal(decimal.Parse(b, System.Globalization.CultureInfo.InvariantCulture),
            KolonTavani.Yuvarla3(decimal.Parse(g, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void S_kolon_tavani_9_999_999_999_999_995_yuvarlaninca_tasar_RET()
    {
        Assert.Equal(9_999_999_999_999.99m, KolonTavani.Denetle(9_999_999_999_999.99m, 2, KolonTavani.D15_2, "x", "X"));
        Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(() =>
            KolonTavani.Denetle(9_999_999_999_999.995m, 2, KolonTavani.D15_2, "x", "X"));
        Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(() =>
            KolonTavani.Denetle(-9_999_999_999_999.995m, 2, KolonTavani.D15_2, "x", "X"));
    }

    [Fact]
    public void S_Turkce_giris_SayiCoz()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(999_999_999.99m, FormAlani.SayiCoz("999.999.999,99").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
    }
}
