using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using MFatura = Kuyumcu.Api.Modules.Fatura;
using MUrun = Kuyumcu.Api.Modules.Urun;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PATCH-2 (02.10) — PATCH-SOZLESME kapanış hasımının KRİTİK'leri
//   (1) Tırnaklı Türkçe sayı ("1.500") sessizce invariant okunuyordu → 1,50 TL.
//       Çözüm TEK NOKTA: MVC JSON ayarı NumberHandling=Strict (Program.cs) +
//       UrunGuncelleDto/UrunOlusturDto sınıf düzeyi Strict (ikinci hat) +
//       KolonTavani.JsonListe Strict (JsonElement'ten kalem çözen uçlar).
//   (2) Miktar/adet 0,0004 sıfır kontrolünden geçip kolonda 0,000 oluyordu → önce
//       3 haneye yuvarla, sonra <= 0 kontrolü: fatura oluşturma, taslak kalem ekle,
//       taslak kalem PATCH, vezne, sipariş, satın alma (+ mal kabul), irsaliye.
//   (3) JSON tür hatası 400 gövdesi: anahtar "$.alan" → "alan"; yapay "dto zorunludur" yok.
// Her vaka: 400 + alanlar.<ad> (gövde AYRIŞTIRILARAK) ve veritabanında satır önce/sonra AYNI.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class PatchIkiHttpTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"PATCH2 {ad}");
    }

    private static Task<HttpResponseMessage> Ham(HttpClient c, HttpMethod m, string yol, string json) =>
        c.SendAsync(new HttpRequestMessage(m, yol) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    /// <summary>400 + alanlar.<paramref name="alan"/>; anahtarların hiçbiri "$" ile başlamaz, "dto" yoktur.</summary>
    private static async Task<JsonElement> Alan400(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), "400 gövdesinde 'alanlar' yok: " + govde);
        var anahtarlar = alanlar.EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains(alan, anahtarlar);
        Assert.DoesNotContain(anahtarlar, a => a.StartsWith('$'));
        Assert.DoesNotContain("dto", anahtarlar);
        return alanlar.GetProperty(alan);
    }

    /// <summary>Türkçe JSON gövdesi AYRIŞTIRILARAK okunur (\u kaçışları çözülür); message dize ya da dizi.</summary>
    private static string Mesaj(string govde)
    {
        var m = JsonDocument.Parse(govde).RootElement.GetProperty("message");
        return m.ValueKind == JsonValueKind.Array
            ? string.Join(" | ", m.EnumerateArray().Select(x => x.GetString()))
            : m.GetString() ?? "";
    }

    private string UrunDurum(string urunId)
    {
        using var db = f.Vt.OlusturSistem();
        return JsonSerializer.Serialize(db.Urunler.AsNoTracking().Single(u => u.Id == urunId));
    }

    // ═══════════ (1) ÜRÜN PATCH — 11 sayı alanı, tırnaklı Türkçe sayı ═══════════

    [Theory]
    [InlineData("gramAgirlik")]
    [InlineData("tasAgirlik")]
    [InlineData("satisFiyati")]
    [InlineData("alisMaliyeti")]
    [InlineData("konsinyeBirimBedel")]
    [InlineData("iscilikTl")]
    [InlineData("minStok")]
    [InlineData("gercekMilyem")]
    [InlineData("pirlantaKarat")]
    [InlineData("hedefMarj")]
    [InlineData("kdvOrani")]
    public async Task P2_urun_PATCH_tirnakli_1_500_400_alan_temiz_SQL_DEGISMEZ(string alan)
    {
        var k = Kur("urun dize " + alan);
        var c = f.Ortam.Istemci(k);
        var once = UrunDurum(k.UrunId);
        foreach (var deger in new[] { "\"1.500\"", "\"45.000\"", "\"1,5\"" })
        {
            var mesaj = await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/urunler/{k.UrunId}",
                "{\"" + alan + "\":" + deger + "}"), alan);
            Assert.Contains("geçerli bir sayı değil", mesaj.EnumerateArray().First().GetString());
        }
        Assert.Equal(once, UrunDurum(k.UrunId));
    }

    [Fact]
    public async Task P2_urun_PATCH_sayi_gonderimi_AYNEN_calisir_1500_yazilir()
    {
        var k = Kur("urun sayi");
        var r = await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/urunler/{k.UrunId}", "{\"satisFiyati\":1500}");
        Assert.True(r.StatusCode == HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(1500m, db.Urunler.AsNoTracking().Single(u => u.Id == k.UrunId).SatisFiyati);
    }

    [Fact]
    public async Task P2_urun_PATCH_1e30_400_alan_gramAgirlik_dto_YOK()
    {
        var k = Kur("urun 1e30");
        var once = UrunDurum(k.UrunId);
        await Alan400(await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/urunler/{k.UrunId}",
            "{\"gramAgirlik\":1e30}"), "gramAgirlik");
        Assert.Equal(once, UrunDurum(k.UrunId));
    }

    [Fact]
    public async Task P2_urun_POST_tirnakli_satis_fiyati_400_urun_ACILMAZ()
    {
        var k = Kur("urun post");
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.Urunler.Count(u => u.TenantId == k.TenantId); }
        var once = Sayi();
        await Alan400(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, "/api/v1/urunler",
            "{\"kod\":\"P2-DIZE\",\"ad\":\"Dize fiyat\",\"tip\":\"ALTIN\",\"gramAgirlik\":1,\"satisFiyati\":\"1.500\"}"),
            "satisFiyati");
        Assert.Equal(once, Sayi());
    }

    // ═══════════ GENEL AYAR — özniteliksiz DTO'lar da korunur ═══════════

    [Fact]
    public async Task P2_GENEL_fatura_POST_tirnakli_toplam_400_alan_toplam_fatura_ACILMAZ()
    {
        var k = Kur("fatura dize");
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.Faturalar.Count(x => x.TenantId == k.TenantId); }
        var once = Sayi();
        await Alan400(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, "/api/v1/faturalar",
            "{\"cariId\":\"" + k.CariId + "\",\"tur\":\"SATIS\",\"toplam\":\"1.500\",\"kdvOran\":0.2,"
            + "\"kalemler\":[{\"aciklama\":\"x\",\"miktar\":1,\"birimFiyat\":1500}]}"), "toplam");
        Assert.Equal(once, Sayi());
    }

    [Fact]
    public async Task P2_GENEL_fatura_kalem_POST_tirnakli_miktar_400_ic_yol_temiz_kalem_EKLENMEZ()
    {
        var k = Kur("fatura kalem dize");
        var c = f.Ortam.Istemci(k);
        var (fid, _) = await Taslak(k, c);
        var once = FaturaDurum(fid);
        await Alan400(await Ham(c, HttpMethod.Post, $"/api/v1/faturalar/{fid}/kalemler",
            "{\"aciklama\":\"ek\",\"miktar\":\"1.500\",\"birimFiyat\":10}"), "miktar");
        Assert.Equal(once, FaturaDurum(fid));
    }

    [Fact]
    public async Task P2_GENEL_ic_ice_kalem_yolu_anahtari_kalemler_0_adet()
    {
        var k = Kur("irsaliye dize");
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.Irsaliyeler.Count(x => x.TenantId == k.TenantId); }
        var once = Sayi();
        await Alan400(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, "/api/v1/irsaliyeler",
            "{\"yon\":\"GIDEN\",\"cariId\":\"" + k.CariId + "\",\"subeId\":\"" + k.SubeId + "\","
            + "\"kalemler\":[{\"tanim\":\"Hurda\",\"adet\":\"1.500\"}]}"), "kalemler[0].adet");
        Assert.Equal(once, Sayi());
    }

    [Fact]
    public async Task P2_GENEL_bozuk_JSON_400_alan_govde()
    {
        var k = Kur("bozuk json");
        var once = UrunDurum(k.UrunId);
        var m = await Alan400(await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/urunler/{k.UrunId}",
            "{\"satisFiyati\": }"), "satisFiyati");
        Assert.False(string.IsNullOrWhiteSpace(m.EnumerateArray().First().GetString()));
        Assert.Equal(once, UrunDurum(k.UrunId));
    }

    [Fact]
    public async Task P2_JsonListe_vezne_PATCH_kalem_tirnakli_adet_400_alan_kalemler_fis_DEGISMEZ()
    {
        var k = Kur("vezne dize");
        var c = f.Ortam.Istemci(k);
        var y = await c.PostAsJsonAsync("/api/v1/vezne/fisler", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId, aciklama = "dize",
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 1000m, iscilikTl = 0m } },
        });
        Assert.True(y.StatusCode == HttpStatusCode.OK, await y.Content.ReadAsStringAsync());
        var id = (await y.Json()).GetProperty("id").GetString()!;
        var once = VezneDurum(id);
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/vezne/fisler/{id}",
            "{\"kalemler\":[{\"urunId\":\"" + k.UrunId + "\",\"adet\":\"1.500\",\"birimFiyatTl\":1000}]}"), "kalemler");
        Assert.Equal(once, VezneDurum(id));
    }

    // ═══════════ (2) MİKTAR 0,0004 → yuvarlanınca 0 → RET (tüm kalem yolları) ═══════════

    private static async Task<(string Fid, string Kid)> Taslak(Kiraci k, HttpClient c)
    {
        var y = await c.PostAsJsonAsync("/api/v1/faturalar", new
        {
            cariId = k.CariId, tur = "SATIS", toplam = 1200m, kdvOran = 0.20m,
            kalemler = new object[] { new { aciklama = "P2 bilezik", miktar = 1m, birimFiyat = 1200m } },
        });
        var govde = await y.Content.ReadAsStringAsync();
        Assert.True(y.StatusCode == HttpStatusCode.OK, $"{(int)y.StatusCode} {govde}");
        var id = JsonDocument.Parse(govde).RootElement.GetProperty("id").GetString()!;
        var kid = (await (await c.GetAsync($"/api/v1/faturalar/{id}/kalemler")).Json())
            .GetProperty("kalemler").EnumerateArray().Single().GetProperty("id").GetString()!;
        return (id, kid);
    }

    private string FaturaDurum(string fid)
    {
        using var db = f.Vt.OlusturSistem();
        var fa = db.Faturalar.AsNoTracking().Single(x => x.Id == fid);
        var ka = db.FaturaKalemleri.AsNoTracking().Where(x => x.FaturaId == fid).OrderBy(x => x.Id).ToList();
        return $"{fa.Toplam}|{fa.KdvTutar}|{fa.AraToplam}|{fa.HasGram}|{fa.HasDeger}|" + string.Join(";", ka.Select(x =>
            $"{x.Miktar}/{x.BirimFiyat}/{x.IscilikTl}/{x.IndirimTutari}/{x.SatirToplam}/{x.HasGrami}"));
    }

    [Theory]
    [InlineData("0.0004")]
    [InlineData("0.0001")]
    [InlineData("0.00049999")]
    public async Task P2_fatura_kalem_PATCH_miktar_yuvarlaninca_sifir_iscilikli_satirda_400_alan_miktar(string miktar)
    {
        var k = Kur("kalem patch " + miktar);
        var c = f.Ortam.Istemci(k);
        var (fid, kid) = await Taslak(k, c);
        var r1 = await Ham(c, HttpMethod.Patch, $"/api/v1/faturalar/{fid}/kalemler/{kid}", "{\"iscilikTl\":100}");
        Assert.True(r1.StatusCode == HttpStatusCode.OK, await r1.Content.ReadAsStringAsync());
        var once = FaturaDurum(fid);
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/faturalar/{fid}/kalemler/{kid}",
            "{\"miktar\":" + miktar + "}"), "miktar");
        Assert.Equal(once, FaturaDurum(fid));
    }

    // PAKET GOC-A (K10): 0,0005 YUVARLANMAZ (eski: 0,001 olarak yazılırdı) — 400 alan:miktar, kalem DEĞİŞMEZ;
    // en küçük geçerli miktar 0,001 aynen yazılır.
    [Fact]
    public async Task P2_fatura_kalem_PATCH_miktar_0_0005_yuvarlanmaz_400_SINIR_0_001_yazilir()
    {
        var k = Kur("kalem patch sinir");
        var c = f.Ortam.Istemci(k);
        var (fid, kid) = await Taslak(k, c);
        var once = FaturaDurum(fid);
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/faturalar/{fid}/kalemler/{kid}", "{\"miktar\":0.0005}"), "miktar");
        Assert.Equal(once, FaturaDurum(fid));
        var r = await Ham(c, HttpMethod.Patch, $"/api/v1/faturalar/{fid}/kalemler/{kid}", "{\"miktar\":0.001}");
        Assert.True(r.StatusCode == HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0.001m, db.FaturaKalemleri.AsNoTracking().Single(x => x.Id == kid).Miktar);
    }

    [Fact]
    public async Task P2_fatura_taslak_kalem_EKLE_miktar_0_0004_iscilikli_400_kalem_EKLENMEZ()
    {
        var k = Kur("kalem ekle");
        var c = f.Ortam.Istemci(k);
        var (fid, _) = await Taslak(k, c);
        var once = FaturaDurum(fid);
        await Alan400(await Ham(c, HttpMethod.Post, $"/api/v1/faturalar/{fid}/kalemler",
            "{\"aciklama\":\"ek\",\"miktar\":0.0004,\"birimFiyat\":1200,\"iscilikTl\":100}"), "miktar");
        Assert.Equal(once, FaturaDurum(fid));
    }

    [Fact]
    public async Task P2_fatura_OLUSTUR_kalem_miktar_0_0004_iscilikli_400_fatura_ACILMAZ()
    {
        var k = Kur("fatura olustur");
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.Faturalar.Count(x => x.TenantId == k.TenantId); }
        var once = Sayi();
        await Alan400(await f.Ortam.Istemci(k).PostAsJsonAsync("/api/v1/faturalar", new
        {
            cariId = k.CariId, tur = "SATIS", toplam = 100m, kdvOran = 0.20m,
            kalemler = new object[] { new { aciklama = "sıfır", miktar = 0.0004m, birimFiyat = 1200m, iscilikTl = 100m } },
        }), "kalemler[0].miktar");   // controller yolu: DTO Range(0.001) — servis kapısı aşağıdaki DB testinde
        Assert.Equal(once, Sayi());
    }

    private string VezneDurum(string id)
    {
        using var db = f.Vt.OlusturSistem();
        var fis = db.VezneFisleri.AsNoTracking().Single(x => x.Id == id);
        return JsonSerializer.Serialize(fis) + "#" + string.Join(";", db.VezneFisKalemleri.AsNoTracking()
            .Where(x => x.TenantId == fis.TenantId).OrderBy(x => x.Id).ToList().Select(x => JsonSerializer.Serialize(x)));
    }

    [Fact]
    public async Task P2_vezne_POST_ve_PATCH_kalem_adet_0_0004_400_alan_kalemler()
    {
        var k = Kur("vezne sifir");
        var c = f.Ortam.Istemci(k);
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.VezneFisleri.Count(x => x.TenantId == k.TenantId); }
        await Alan400(await c.PostAsJsonAsync("/api/v1/vezne/fisler", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId, aciklama = "sıfır",
            kalemler = new[] { new { urunId = k.UrunId, adet = 0.0004m, birimFiyatTl = 1000m, iscilikTl = 100m } },
        }), "kalemler[0].adet");   // PAKET GOC-A: controller yolu VezneKalemDto [Gram] (K10) — alan kalem düzeyinde işaretli
        Assert.Equal(0, Sayi());
        var y = await c.PostAsJsonAsync("/api/v1/vezne/fisler", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId, aciklama = "bir",
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 1000m, iscilikTl = 0m } },
        });
        Assert.True(y.StatusCode == HttpStatusCode.OK, await y.Content.ReadAsStringAsync());
        var id = (await y.Json()).GetProperty("id").GetString()!;
        var once = VezneDurum(id);
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/vezne/fisler/{id}",
            "{\"kalemler\":[{\"urunId\":\"" + k.UrunId + "\",\"adet\":0.0004,\"birimFiyatTl\":1000,\"iscilikTl\":100}]}"),
            "kalemler");
        Assert.Equal(once, VezneDurum(id));
    }

    [Fact]
    public async Task P2_siparis_POST_kalem_adet_0_0004_400_siparis_ACILMAZ()
    {
        var k = Kur("siparis sifir");
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.Siparisler.Count(x => x.TenantId == k.TenantId); }
        var r = await f.Ortam.Istemci(k).PostAsJsonAsync("/api/v1/siparisler", new
        {
            tur = "MUSTERI_SIPARISI", cariId = k.CariId,
            sozVerilenGun = Kuyumcu.Api.Common.Zaman.YerelBugun().AddDays(3).ToString("yyyy-MM-dd"),
            aciklama = "sıfır", kalemler = new[] { new { urunId = k.UrunId, adet = 0.0004m } },
        });
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        // PAKET GOC-C (K14): sipariş kalem adedi tam sayı ≥ 1 — 0,0004 artık Core AdetKurali metniyle reddedilir (sözlük).
        Assert.Contains(Kuyumcu.Core.I18n.GirdiSozlugu.Tr.SayiTam, Mesaj(govde));
        // PAKET GOC-C: DTO özniteliği ([Adet]) alanı kalem yoluyla adlandırır ("kalemler[0].adet") — daha kesin konum.
        Assert.Contains(JsonDocument.Parse(govde).RootElement.GetProperty("alanlar").EnumerateObject(), a => a.Name.EndsWith("adet", StringComparison.Ordinal));
        Assert.Equal(0, Sayi());
    }

    [Fact]
    public async Task P2_satinalma_POST_kalem_ve_MAL_KABUL_adet_0_0004_400()
    {
        var k = Kur("satinalma sifir");
        var c = f.Ortam.Istemci(k);
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.SatinalmaSiparisleri.Count(x => x.TenantId == k.TenantId); }
        var r = await c.PostAsJsonAsync("/api/v1/satinalma", new
        {
            tedarikciCariId = k.CariId, subeId = k.SubeId, aciklama = "sıfır",
            kalemler = new[] { new { tanim = "Hurda altın", adet = 0.0004m, birimFiyatTl = 100m } },
        });
        var rGovde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {rGovde}");
        Assert.True(JsonDocument.Parse(rGovde).RootElement.TryGetProperty("alanlar", out var saAlan) && saAlan.TryGetProperty("kalemler", out _), rGovde);
        Assert.Equal(0, Sayi());

        var y = await c.PostAsJsonAsync("/api/v1/satinalma", new
        {
            tedarikciCariId = k.CariId, subeId = k.SubeId, aciklama = "bir",
            kalemler = new[] { new { urunId = k.UrunId, tanim = "Bilezik", adet = 2m, birimFiyatTl = 100m } },
        });
        Assert.True(y.StatusCode == HttpStatusCode.OK, await y.Content.ReadAsStringAsync());
        var id = (await y.Json()).GetProperty("id").GetString()!;
        string KalemDurum()
        {
            using var db = f.Vt.OlusturSistem();
            return string.Join(";", db.SatinalmaKalemleri.AsNoTracking().Where(x => x.SatinalmaSiparisId == id)
                .OrderBy(x => x.Id).Select(x => x.Adet + "/" + x.KabulEdilenAdet));
        }
        var onay = await c.PostAsync($"/api/v1/satinalma/{id}/onayla", null);
        Assert.True(onay.StatusCode == HttpStatusCode.OK, await onay.Content.ReadAsStringAsync());
        var once = KalemDurum();
        var kalemId = (await (await c.GetAsync($"/api/v1/satinalma/{id}")).Json())
            .GetProperty("kalemler").EnumerateArray().First().GetProperty("id").GetString()!;
        var mk = await c.PostAsJsonAsync($"/api/v1/satinalma/{id}/mal-kabul", new
        {
            kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId, adet = 0.0004m } },
        });
        var mkGovde = await mk.Content.ReadAsStringAsync();
        Assert.True(mk.StatusCode == HttpStatusCode.BadRequest, $"{(int)mk.StatusCode} {mkGovde}");
        Assert.Contains("Kabul adedi sıfırdan büyük", Mesaj(mkGovde));
        Assert.True(JsonDocument.Parse(mkGovde).RootElement.TryGetProperty("alanlar", out var mkAlan) && mkAlan.TryGetProperty("kalemler", out _), mkGovde);
        Assert.Equal(once, KalemDurum());
    }

    [Fact]
    public async Task P2_irsaliye_POST_kalem_adet_0_0004_400_irsaliye_ACILMAZ()
    {
        var k = Kur("irsaliye sifir");
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.Irsaliyeler.Count(x => x.TenantId == k.TenantId); }
        var r = await f.Ortam.Istemci(k).PostAsJsonAsync("/api/v1/irsaliyeler", new
        {
            yon = "GIDEN", cariId = k.CariId, subeId = k.SubeId,
            kalemler = new[] { new { tanim = "Hurda", adet = 0.0004m } },
        });
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        Assert.Contains("adet sıfırdan büyük", Mesaj(govde));
        Assert.True(JsonDocument.Parse(govde).RootElement.GetProperty("alanlar").TryGetProperty("kalemler", out _), govde);
        Assert.Equal(0, Sayi());
    }

    [Theory]
    [InlineData("{\"miktar\":\"1.500\"}", "miktar")]
    [InlineData("{\"birimFiyat\":\"45.000\"}", "birimFiyat")]
    [InlineData("{\"iscilikTl\":\"1.500\"}", "iscilikTl")]
    public async Task P2_fatura_kalem_PATCH_JsonElement_tirnakli_sayi_400_alan_SQL_DEGISMEZ(string govde, string alan)
    {
        var k = Kur("kalem dize " + alan);
        var c = f.Ortam.Istemci(k);
        var (fid, kid) = await Taslak(k, c);
        var once = FaturaDurum(fid);
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/faturalar/{fid}/kalemler/{kid}", govde), alan);
        Assert.Equal(once, FaturaDurum(fid));
    }
}

// ═══════════ Servis düzeyi — DTO Range'i (yalnız controller yolu) olmadan da kapı tutar ═══════════
[Collection("veritabani")]
public sealed class PatchIkiFaturaServisTestleri(TestVeritabani vt)
{
    [Fact]
    public async Task P2_servis_fatura_OLUSTUR_miktar_0_0004_iscilikli_RET_fatura_ACILMAZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PATCH2 servis olustur");
        using var db = vt.Olustur(k);
        var s = new MFatura.FaturaServisi(db);
        var hata = await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() => s.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 100m, KdvOran = 0.20m,
            Kalemler = [new MFatura.FaturaKalemDto { Aciklama = "sıfır", Miktar = 0.0004m, BirimFiyat = 1200m, IscilikTl = 100m }],
        }, k.Islem));
        // PAKET GOC-A (K10): 0,0004 dört ondalıklıdır — Core kuralı + sözlük metni, kalem ve alan adı önekli.
        Assert.Contains("1. kalem · Miktar: Virgülden sonra en çok 3 hane", hata.Message);
        using var o = vt.OlusturSistem();
        Assert.Equal(0, o.Faturalar.Count(x => x.TenantId == k.TenantId));
    }

    [Fact]
    public async Task P2_servis_TaslakKalemEkle_miktar_0_0004_iscilikli_RET_kalem_EKLENMEZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PATCH2 servis ekle");
        using var db = vt.Olustur(k);
        var s = new MFatura.FaturaServisi(db);
        await s.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 1200m, KdvOran = 0.20m,
            Kalemler = [new MFatura.FaturaKalemDto { Aciklama = "bir", Miktar = 1m, BirimFiyat = 1200m }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() => s.TaslakKalemEkle(fid,
            new MFatura.FaturaKalemDto { Aciklama = "ek", Miktar = 0.0004m, BirimFiyat = 1200m, IscilikTl = 100m }, k.Islem));
        using var o = vt.OlusturSistem();
        Assert.Equal(1, o.FaturaKalemleri.Count(x => x.FaturaId == fid));
        Assert.Equal(1200m, o.Faturalar.AsNoTracking().Single(x => x.Id == fid).Toplam);
    }
}

// ═══════════ Saf testler — ikinci savunma hattı ve 400 gövde yardımcıları ═══════════
public sealed class PatchIkiSafTestleri
{
    // Web varsayılanı (AllowReadingFromString) ile çözülse BİLE sınıf özniteliği dizeyi reddeder.
    private static readonly JsonSerializerOptions WebVarsayilani = new(JsonSerializerDefaults.Web);

    public static IEnumerable<object[]> UrunSayiUyeleri() =>
        new[] { typeof(MUrun.UrunGuncelleDto), typeof(MUrun.UrunOlusturDto) }
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && (Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType) == typeof(decimal))
                .Select(p => new object[] { t.Name, p.Name }));

    [Theory]
    [MemberData(nameof(UrunSayiUyeleri))]
    public void P2_urun_DTO_sinif_Strict_web_varsayilaninda_da_tirnakli_sayiyi_REDDEDER(string tur, string uye)
    {
        var t = tur == nameof(MUrun.UrunGuncelleDto) ? typeof(MUrun.UrunGuncelleDto) : typeof(MUrun.UrunOlusturDto);
        var ad = char.ToLowerInvariant(uye[0]) + uye[1..];
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("{\"" + ad + "\":\"1.500\"}", t, WebVarsayilani));
        // Sayı gönderimi aynen çözülür (istemci sözleşmesi bozulmaz).
        var o = JsonSerializer.Deserialize("{\"" + ad + "\":1.5}", t, WebVarsayilani)!;
        Assert.Equal(1.5m, (decimal?)t.GetProperty(uye)!.GetValue(o));
    }

    [Fact]
    public void P2_UrunGuncelleDto_11_sayi_uyesinin_hepsi_kapsamda()
    {
        var uyeler = UrunSayiUyeleri().Where(x => (string)x[0] == nameof(MUrun.UrunGuncelleDto)).Select(x => (string)x[1]).ToHashSet();
        foreach (var u in new[] { "GramAgirlik", "TasAgirlik", "SatisFiyati", "AlisMaliyeti", "KonsinyeBirimBedel",
                     "IscilikTl", "MinStok", "GercekMilyem", "PirlantaKarat", "HedefMarj", "KdvOrani" })
            Assert.Contains(u, uyeler);
        Assert.Equal(JsonNumberHandling.Strict,
            typeof(MUrun.UrunGuncelleDto).GetCustomAttribute<JsonNumberHandlingAttribute>()!.Handling);
    }

    [Theory]
    [InlineData("$.satisFiyati", "satisFiyati")]
    [InlineData("$.kalemler[0].adet", "kalemler[0].adet")]
    [InlineData("$", "")]
    [InlineData("", "")]
    public void P2_JSON_yolu_alan_anahtarina_cevrilir(string yol, string beklenen) =>
        Assert.Equal(beklenen, Program.JsonYoluAlani(yol));

    [Fact]
    public void P2_JSON_tur_hatasi_iletisi_alani_adlandirir_ingilizce_sizmaz()
    {
        var m = Program.JsonHataMesaji("satisFiyati",
            "The JSON value could not be converted to System.Nullable`1[System.Decimal]. Path: $.satisFiyati | LineNumber: 0 | BytePositionInLine: 23.", null);
        Assert.Contains("geçerli bir sayı değil", m);
        Assert.DoesNotContain("JSON value", m);
        Assert.DoesNotContain("$.", m);
        Assert.Contains("beklenen türde değil",
            Program.JsonHataMesaji("aktif", "The JSON value could not be converted to System.Boolean. Path: $.aktif", null));
        Assert.StartsWith("İstek gövdesi", Program.JsonHataMesaji("", "'}' is an invalid start of a value.", null));
    }
}
