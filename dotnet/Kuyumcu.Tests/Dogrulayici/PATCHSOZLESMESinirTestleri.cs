using System.Net;
using System.Text;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using MFatura = Kuyumcu.Api.Modules.Fatura;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PATCH-SOZLESME — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT (üretim kodundan
// hesaplanmaz). Paketin dokunduğu sayısal yollar:
//   • PATCH /urunler/{id}/stok/{subeId} (sayım adedi → stok + deftere has)
//   • PATCH /fiyat/listeler/{id} (has marjı / işçilik çarpanı yüzdesi)
//   • PATCH /fiyat/kampanyalar/{id} (indirim değeri)
//   • Fatura taslak kalem miktar PATCH'i (birim has × miktar)
// Sınırlar: 0 · eksi · 0,0005 adet yarısı (AwayFromZero — fatura miktarıyla aynı
// kural) · has 4 hane (0,0001) · çok büyük (999.999.999,99) · null · Türkçe
// biçimli dize ('45.000', '1,5') JSON gövdede reddedilir.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class PATCHSOZLESMESinirHttpTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"PATCHSINIR {ad}");
    }

    private static Task<HttpResponseMessage> Ham(HttpClient c, string yol, string json) =>
        c.PatchAsync(yol, new StringContent(json, Encoding.UTF8, "application/json"));

    private decimal StokAdet(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Stoklar.AsNoTracking().Single(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId).Adet;
    }

    // ── Stok sayımı ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Sinir_stok_sifir_adet_200_stok_0()
    {
        var k = Kur("stok sıfır");
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", "{\"adet\":0}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(0m, StokAdet(k));
    }

    [Fact]
    public async Task Sinir_stok_eksi_adet_400_stok_DEGISMEZ()
    {
        var k = Kur("stok eksi");
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", "{\"adet\":-1}");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(100m, StokAdet(k));
    }

    [Fact]
    public async Task Sinir_stok_null_adet_400_stok_DEGISMEZ()
    {
        var k = Kur("stok null");
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", "{\"adet\":null}");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(100m, StokAdet(k));
    }

    [Theory]
    [InlineData("\"45.000\"")]
    [InlineData("\"1,5\"")]
    [InlineData("\"1.500\"")]
    public async Task Sinir_stok_Turkce_bicimli_dize_400_stok_DEGISMEZ(string deger)
    {
        var k = Kur("stok dize");
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", "{\"adet\":" + deger + "}");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(100m, StokAdet(k));
    }

    [Fact]
    public async Task Sinir_stok_yarim_binde_AwayFromZero_1_0005_olur_1_001()
    {
        var k = Kur("stok yarım");
        using (var g = f.Vt.OlusturSistem()) TestKurulum.UrunuGramBirimiYap(g, k.UrunId);   // PAKET GOC-C (K14): kesirli miktar yalnız GRAM birimde — sınır ölçüsü GRAM birimli üründe korunur
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", "{\"adet\":1.0005}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(1.001m, StokAdet(k));
    }

    [Fact]
    public async Task Sinir_stok_cok_buyuk_500_DEGIL()
    {
        var k = Kur("stok büyük");
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", "{\"adet\":999999999.99}");
        Assert.True(r.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"çok büyük adet {(int)r.StatusCode} döndü: {await r.Metin()}");
        if (r.StatusCode == HttpStatusCode.OK) Assert.Equal(999999999.99m, StokAdet(k));
        else Assert.Equal(100m, StokAdet(k));
    }

    // ── Fiyat listesi marj/çarpan ──────────────────────────────────────────

    private string Liste(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var now = DateTime.UtcNow;
        var v = new FiyatListesi
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, Ad = "Varsayılan", Tur = "PERAKENDE",
            HasMarjYuzde = 5m, IscilikCarpanYuzde = 100m, Varsayilan = true, Aktif = true, Olusturuldu = now, Guncellendi = now,
        };
        var l = new FiyatListesi
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, Ad = "Sınır", Tur = "TOPTAN",
            HasMarjYuzde = 3m, IscilikCarpanYuzde = 90m, Varsayilan = false, Aktif = true, Olusturuldu = now, Guncellendi = now,
        };
        db.FiyatListeleri.AddRange(v, l);
        db.SaveChanges();
        return l.Id;
    }

    private FiyatListesi ListeOku(string id)
    {
        using var db = f.Vt.OlusturSistem();
        return db.FiyatListeleri.AsNoTracking().Single(x => x.Id == id);
    }

    [Fact]
    public async Task Sinir_liste_marj_sifir_200_carpan_KORUNUR()
    {
        var k = Kur("liste sıfır");
        var id = Liste(k);
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/fiyat/listeler/{id}", "{\"hasMarjYuzde\":0}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var l = ListeOku(id);
        Assert.Equal((0m, 90m, "Sınır", "TOPTAN"), (l.HasMarjYuzde, l.IscilikCarpanYuzde, l.Ad, l.Tur));
    }

    [Theory]
    [InlineData("{\"hasMarjYuzde\":999999999.99}")]
    [InlineData("{\"iscilikCarpanYuzde\":-1}")]
    [InlineData("{\"hasMarjYuzde\":\"1,5\"}")]
    [InlineData("{\"hasMarjYuzde\":null}")]
    public async Task Sinir_liste_gecersiz_marj_carpan_400_kayit_DEGISMEZ(string govde)
    {
        var k = Kur("liste geçersiz");
        var id = Liste(k);
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/fiyat/listeler/{id}", govde);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var l = ListeOku(id);
        Assert.Equal((3m, 90m), (l.HasMarjYuzde, l.IscilikCarpanYuzde));
    }

    // ── Kampanya indirim değeri ─────────────────────────────────────────────

    private string Kampanya(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var now = DateTime.UtcNow;
        var bugun = DateOnly.FromDateTime(now);
        var km = new Kampanya
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, Ad = "Sınır kampanya", Baslangic = bugun, Bitis = bugun.AddDays(10),
            HedefTur = "TUMU", HedefId = null, IndirimTuru = "TUTAR_TL", Deger = 250m, Aktif = true,
            Olusturuldu = now, Guncellendi = now,
        };
        db.Kampanyalar.Add(km);
        db.SaveChanges();
        return km.Id;
    }

    private decimal KampanyaDeger(string id)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Kampanyalar.AsNoTracking().Single(x => x.Id == id).Deger;
    }

    [Theory]
    [InlineData("{\"deger\":0}")]
    [InlineData("{\"deger\":-5}")]
    [InlineData("{\"deger\":0.005}")]
    [InlineData("{\"deger\":null}")]
    [InlineData("{\"deger\":\"45.000\"}")]
    public async Task Sinir_kampanya_gecersiz_deger_400_deger_DEGISMEZ(string govde)
    {
        var k = Kur("kampanya geçersiz");
        var id = Kampanya(k);
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/fiyat/kampanyalar/{id}", govde);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(250m, KampanyaDeger(id));
    }

    [Fact]
    public async Task Sinir_kampanya_kurus_sinirinda_0_01_kabul()
    {
        var k = Kur("kampanya kuruş");
        var id = Kampanya(k);
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/fiyat/kampanyalar/{id}", "{\"deger\":0.01}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(0.01m, KampanyaDeger(id));
    }
}

[Collection("veritabani")]
public sealed class PATCHSOZLESMESinirFaturaTestleri(TestVeritabani vt)
{
    /// <summary>Birim has 0,0001 (4. hane) — miktar 3 → 1 → 3 gidiş-dönüşte kalem has 0,0003 / 0,0001 / 0,0003.</summary>
    [Fact]
    public async Task Sinir_fatura_kalem_has_4_hane_0_0001_birikimsiz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PATCHSINIR fatura has4");
        using (var s = vt.OlusturSistem())
        {
            var u = await s.Urunler.SingleAsync(x => x.Id == k.UrunId);
            u.HasGrami = 0.0001m;
            await s.SaveChangesAsync();
        }
        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 600m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 3m, BirimFiyat = 200m }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kid = db.FaturaKalemleri.Single(x => x.FaturaId == fid).Id;

        decimal Has()
        {
            using var o = vt.OlusturSistem();
            return o.FaturaKalemleri.AsNoTracking().Single(x => x.Id == kid).HasGrami!.Value;
        }
        Assert.Equal(0.0003m, Has());
        await servis.TaslakKalemGuncelle(fid, kid, JsonDocument.Parse("{\"miktar\":1}").RootElement, k.Islem);
        Assert.Equal(0.0001m, Has());
        await servis.TaslakKalemGuncelle(fid, kid, JsonDocument.Parse("{\"miktar\":3}").RootElement, k.Islem);
        Assert.Equal(0.0003m, Has());
    }

    [Theory]
    [InlineData("{\"miktar\":0}")]
    [InlineData("{\"miktar\":-1}")]
    public async Task Sinir_fatura_kalem_miktar_sifir_eksi_400_has_DEGISMEZ(string govde)
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PATCHSINIR fatura miktar");
        using (var s = vt.OlusturSistem())
        {
            var u = await s.Urunler.SingleAsync(x => x.Id == k.UrunId);
            u.HasGrami = 0.015m;
            await s.SaveChangesAsync();
        }
        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 1000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 2m, BirimFiyat = 500m }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kid = db.FaturaKalemleri.Single(x => x.FaturaId == fid).Id;
        await Assert.ThrowsAsync<BadRequestException>(() =>
            servis.TaslakKalemGuncelle(fid, kid, JsonDocument.Parse(govde).RootElement, k.Islem));
        using var o = vt.OlusturSistem();
        Assert.Equal(0.03m, o.FaturaKalemleri.AsNoTracking().Single(x => x.Id == kid).HasGrami);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// TUR 2 — HASIM DOĞRULAYICI (02.10, ikinci tur) — ek sınırlar. Beklenen değerler SABİT.
//   • kampanya çok büyük tutar 999.999.999,99 (decimal(15,2) içinde) → kabul, birebir
//   • kampanya kolon kapasitesini aşan değer → 400 (500 DEĞİL), değer korunur
//   • kampanya kuruş yarısı 12,345 → kolonda 12,35 (yarım kuruş AwayFromZero)
//   • liste marjı 4 hane 1,2345 → decimal(7,3) kolonda 1,235 (AwayFromZero)
//   • stok sayımı -0,0005 → AwayFromZero ile -0,001 → 400 (eksi), stok korunur
//   • stok sayımı has: 22 ayar 0,916667 × fark 1,5 = 1,3750005 → 1,375001
//     (fatura kalemi KalemToplamHas/Yuvarla6 ile AYNI kural — defter ve fatura aynı has'ı yazmalı)
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class PATCHSOZLESMESinirTur2Testleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"PATCHSINIR2 {ad}");
    }

    private static Task<HttpResponseMessage> Ham(HttpClient c, string yol, string json) =>
        c.PatchAsync(yol, new StringContent(json, Encoding.UTF8, "application/json"));

    private string Kampanya(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var now = DateTime.UtcNow;
        var bugun = DateOnly.FromDateTime(now);
        var km = new Kampanya
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, Ad = "Sınır2", Baslangic = bugun, Bitis = bugun.AddDays(10),
            HedefTur = "TUMU", HedefId = null, IndirimTuru = "TUTAR_TL", Deger = 250m, Aktif = true,
            Olusturuldu = now, Guncellendi = now,
        };
        db.Kampanyalar.Add(km);
        db.SaveChanges();
        return km.Id;
    }

    private decimal KampanyaDeger(string id)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Kampanyalar.AsNoTracking().Single(x => x.Id == id).Deger;
    }

    [Fact]
    public async Task Sinir2_kampanya_cok_buyuk_999999999_99_birebir()
    {
        var k = Kur("kampanya büyük");
        var id = Kampanya(k);
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/fiyat/kampanyalar/{id}", "{\"deger\":999999999.99}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(999999999.99m, KampanyaDeger(id));
    }

    [Fact]
    public async Task Sinir2_kampanya_kolon_tavanini_asan_deger_400_500_DEGIL()
    {
        var k = Kur("kampanya tavan");
        var id = Kampanya(k);
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/fiyat/kampanyalar/{id}", "{\"deger\":100000000000000}");
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, (int)r.StatusCode + " " + await r.Content.ReadAsStringAsync());
        Assert.Equal(250m, KampanyaDeger(id));
    }

    [Fact]
    public async Task Sinir2_kampanya_yarim_kurus_12_345_kolonda_12_35()
    {
        var k = Kur("kampanya yarım kuruş");
        var id = Kampanya(k);
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/fiyat/kampanyalar/{id}", "{\"deger\":12.345}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(12.35m, KampanyaDeger(id));
    }

    [Fact]
    public async Task Sinir2_liste_marj_4_hane_1_2345_kolonda_1_235()
    {
        var k = Kur("liste 4 hane");
        string id;
        using (var db = f.Vt.OlusturSistem())
        {
            var now = DateTime.UtcNow;
            var l = new FiyatListesi
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, Ad = "Sınır2", Tur = "TOPTAN",
                HasMarjYuzde = 3m, IscilikCarpanYuzde = 90m, Varsayilan = false, Aktif = true, Olusturuldu = now, Guncellendi = now,
            };
            db.FiyatListeleri.Add(new FiyatListesi
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, Ad = "Varsayılan2", Tur = "PERAKENDE",
                HasMarjYuzde = 5m, IscilikCarpanYuzde = 100m, Varsayilan = true, Aktif = true, Olusturuldu = now, Guncellendi = now,
            });
            db.FiyatListeleri.Add(l); db.SaveChanges(); id = l.Id;
        }
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/fiyat/listeler/{id}", "{\"hasMarjYuzde\":1.2345}");
        Assert.True(r.StatusCode == HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        using var o = f.Vt.OlusturSistem();
        var liste = o.FiyatListeleri.AsNoTracking().Single(x => x.Id == id);
        Assert.Equal(1.235m, liste.HasMarjYuzde);
        Assert.Equal(90m, liste.IscilikCarpanYuzde);   // gönderilmedi → KORUNUR
        Assert.Equal("TOPTAN", liste.Tur);
    }

    [Fact]
    public async Task Sinir2_stok_eksi_yarim_binde_0_0005_400_stok_DEGISMEZ()
    {
        var k = Kur("stok -0,0005");
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", "{\"adet\":-0.0005}");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        using var o = f.Vt.OlusturSistem();
        Assert.Equal(100m, o.Stoklar.AsNoTracking().Single(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId).Adet);
    }

    // ORTA bulgu (doğrulayıcı tur 2): StokSayimGuncelle has gramını Math.Round(x,6) (ToEven) ile yazıyor;
    // fatura kalemi Yuvarla6 (AwayFromZero). Ölçüm: 1,375000 ≠ 1,375001. Proje genelinde 6 hane kuralı karışık
    // (29 ToEven / 20 AwayFromZero) → tek kural kararı orkestratörde; düzeltilince Skip kaldırılır.
    // PATCH-SOZLESME tur 3: StokSayimGuncelle Yuvarla6 (AwayFromZero) kullanıyor → Skip kaldırıldı.
    [Fact]
    public async Task Sinir2_stok_sayim_defter_has_yarim_milyonda_fatura_kuraliyla_1_375001()
    {
        var k = Kur("stok has yarım");
        using (var g = f.Vt.OlusturSistem()) TestKurulum.UrunuGramBirimiYap(g, k.UrunId);   // PAKET GOC-C (K14): kesirli sayım yalnız GRAM birimde — yarım milyon ölçüsü orada
        using (var db = f.Vt.OlusturSistem())
        {
            var u = db.Urunler.Single(x => x.Id == k.UrunId);
            u.HasGrami = 0.916667m; db.SaveChanges();
        }
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", "{\"adet\":101.5}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        using var o = f.Vt.OlusturSistem();
        Assert.Equal(101.5m, o.Stoklar.AsNoTracking().Single(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId).Adet);
        var has = o.Hareketler.AsNoTracking().Single(h => h.TenantId == k.TenantId && h.Tur == "SAYIM_DUZELTME").HasGrami;
        Assert.Equal(MFatura.FaturaServisi.KalemToplamHas(1.5m, 0.916667m), 1.375001m); // fatura kuralı (sabit)
        Assert.Equal(1.375001m, has);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// TUR 3 — HASIM DOĞRULAYICI (02.10, kapanış turu) — KOLON TAVANI (KolonTavani.cs) sınırları.
// Beklenen değerler SABİT; üretim kodundan hesaplanmaz.
//   • decimal(15,2) tam tavan 9.999.999.999.999,99 ve ,994 kabul; ,995 (yarım kuruş AwayFromZero → taşar) ret
//   • eksi tavan da mutlak değerle reddedilir; 0 kabul
//   • decimal(12,4) gram: 99.999.999,9999 kabul; 99.999.999,99995 ret (4 hane yarısı)
//   • fatura kalem birim fiyat 0,005 → 0,01 (yarım kuruş AwayFromZero), satır toplamı 0,01
//   • fatura kalem miktar 0,0004 → 3 haneye yuvarlanınca 0 → işçilikli satırda bile RET (miktar 0 kalem olmaz)
//   • fatura başlık toplamı 999.999.999,99 → kabul, kolonda birebir
//   • ürün PATCH Türkçe biçimli dize '45.000' ve '1,5' → 400, SQL değişmez
// ═════════════════════════════════════════════════════════════════════════════
public sealed class PATCHSOZLESMESinirTur3SaltTestleri
{
    [Theory]
    [InlineData("9999999999999.99")]
    [InlineData("9999999999999.994")]
    [InlineData("0")]
    [InlineData("-9999999999999.99")]
    public void Sinir3_D15_2_tavan_ici_kabul(string d)
    {
        var v = decimal.Parse(d, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(v, Kuyumcu.Api.Modules.KolonTavani.Denetle(v, 2, 9_999_999_999_999.99m, "x", "X"));
    }

    [Theory]
    [InlineData("9999999999999.995")]
    [InlineData("-9999999999999.995")]
    [InlineData("10000000000000")]
    public void Sinir3_D15_2_tavan_disi_400_alan(string d)
    {
        var v = decimal.Parse(d, System.Globalization.CultureInfo.InvariantCulture);
        var e = Assert.Throws<BadRequestException>(() =>
            Kuyumcu.Api.Modules.KolonTavani.Denetle(v, 2, Kuyumcu.Api.Modules.KolonTavani.D15_2, "deger", "Değer"));
        Assert.Contains("çok büyük", e.Message);
    }

    [Fact]
    public void Sinir3_D12_4_gram_4_hane_yarisi()
    {
        Assert.Equal(99_999_999.9999m, Kuyumcu.Api.Modules.KolonTavani.Denetle(99_999_999.9999m, 4, Kuyumcu.Api.Modules.KolonTavani.D12_4, "g", "G"));
        Assert.Throws<BadRequestException>(() =>
            Kuyumcu.Api.Modules.KolonTavani.Denetle(99_999_999.99995m, 4, Kuyumcu.Api.Modules.KolonTavani.D12_4, "g", "G"));
        // Tavanlar kolon tanımıyla birebir (information_schema: decimal(15,2) / (12,4) / (12,3) / (15,6))
        Assert.Equal(9_999_999_999_999.99m, Kuyumcu.Api.Modules.KolonTavani.D15_2);
        Assert.Equal(99_999_999.9999m, Kuyumcu.Api.Modules.KolonTavani.D12_4);
        Assert.Equal(999_999_999.999m, Kuyumcu.Api.Modules.KolonTavani.D12_3);
        Assert.Equal(999_999_999.999999m, Kuyumcu.Api.Modules.KolonTavani.D15_6);
    }
}

[Collection("veritabani")]
public sealed class PATCHSOZLESMESinirTur3FaturaTestleri(TestVeritabani vt)
{
    private async Task<(Kiraci k, MFatura.FaturaServisi s, KuyumcuDbContext db, string fid, string kid)> Kur(string ad)
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, $"PATCHSINIR3 {ad}");
        var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 1200m, KdvOran = 0.20m,
            Kalemler = [new MFatura.FaturaKalemDto { Aciklama = "Sınır3", Miktar = 1m, BirimFiyat = 1200m }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kid = db.FaturaKalemleri.Single(x => x.FaturaId == fid).Id;
        return (k, servis, db, fid, kid);
    }

    private (decimal miktar, decimal birim, decimal satir, decimal toplam) Durum(string fid, string kid)
    {
        using var o = vt.OlusturSistem();
        var kl = o.FaturaKalemleri.AsNoTracking().Single(x => x.Id == kid);
        return (kl.Miktar, kl.BirimFiyat, kl.SatirToplam, o.Faturalar.AsNoTracking().Single(x => x.Id == fid).Toplam);
    }

    // PAKET GOC-A (K9a): birim fiyatta kuruş altı hane YUVARLANMAZ (eski: 0,005 → 0,01) — 400, kalem DEĞİŞMEZ; 0,01 aynen.
    [Fact]
    public async Task Sinir3_fatura_kalem_birim_fiyat_yarim_kurus_0_005_yuvarlanmaz_400_0_01_yazilir()
    {
        var (k, s, db, fid, kid) = await Kur("yarim kurus");
        using var _ = db;
        var once = Durum(fid, kid);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            s.TaslakKalemGuncelle(fid, kid, JsonDocument.Parse("{\"birimFiyat\":0.005}").RootElement, k.Islem));
        Assert.Contains("Virgülden sonra en çok 2 hane", hata.Message);
        Assert.Equal(once, Durum(fid, kid));
        await s.TaslakKalemGuncelle(fid, kid, JsonDocument.Parse("{\"birimFiyat\":0.01}").RootElement, k.Islem);
        Assert.Equal((1.000m, 0.01m, 0.01m, 0.01m), Durum(fid, kid));
    }

    [Fact]
    public async Task Sinir3_fatura_kalem_miktar_0_0004_yuvarlaninca_sifir_iscilikli_satirda_da_RET()
    {
        var (k, s, db, fid, kid) = await Kur("miktar sifir");
        using var _ = db;
        await s.TaslakKalemGuncelle(fid, kid, JsonDocument.Parse("{\"iscilikTl\":100}").RootElement, k.Islem);
        var once = Durum(fid, kid);
        Assert.Equal((1.000m, 1200m, 1300m, 1300m), once);
        await Assert.ThrowsAsync<BadRequestException>(() =>
            s.TaslakKalemGuncelle(fid, kid, JsonDocument.Parse("{\"miktar\":0.0004}").RootElement, k.Islem));
        Assert.Equal(once, Durum(fid, kid));
    }

    [Fact]
    public async Task Sinir3_fatura_kalem_cok_buyuk_999999999_99_birebir()
    {
        var (k, s, db, fid, kid) = await Kur("cok buyuk");
        using var _ = db;
        await s.TaslakKalemGuncelle(fid, kid, JsonDocument.Parse("{\"birimFiyat\":999999999.99}").RootElement, k.Islem);
        Assert.Equal((1.000m, 999_999_999.99m, 999_999_999.99m, 999_999_999.99m), Durum(fid, kid));
    }
}

[Collection("http")]
public sealed class PATCHSOZLESMESinirTur3HttpTestleri(HttpFikstur f)
{
    [Theory]
    [InlineData("{\"satisFiyati\":\"45.000\"}")]
    [InlineData("{\"gramAgirlik\":\"1,5\"}")]
    [InlineData("{\"satisFiyati\":\"1.500\"}")]
    public async Task Sinir3_urun_PATCH_Turkce_dize_400_SQL_DEGISMEZ(string govde)
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PATCHSINIR3 urun dize");
        (decimal g, decimal? sf) Oku()
        {
            using var o = f.Vt.OlusturSistem();
            var u = o.Urunler.AsNoTracking().Single(x => x.Id == k.UrunId);
            return (u.GramAgirlik, u.SatisFiyati);
        }
        var once = Oku();
        var r = await f.Ortam.Istemci(k).PatchAsync($"/api/v1/urunler/{k.UrunId}",
            new StringContent(govde, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(once, Oku());
    }
}
