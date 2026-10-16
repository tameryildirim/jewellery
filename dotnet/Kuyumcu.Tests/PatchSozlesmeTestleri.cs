using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using MFatura = Kuyumcu.Api.Modules.Fatura;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PATCH-SOZLESME (02.10) — PATCH sözleşmesi taraması
//   Sözleşme: gövdede YOKSA KORU · null ise SİL (silinemeyen alanda alan altı 400)
//   · doluysa doğrula ve yaz. Kısmi gövdede "zorunlu" hatası VERİLMEZ.
//   Düzeltilen uçlar (her biri: tek-alan PATCH + SQL'de diğer alanlar aynı,
//   boş gövde, yetki):
//     PATCH /subeler/{id}                     (SUBE-KAPSAM-B: 'Ad zorunludur')
//     PATCH /fiyat/listeler/{id}              (OLUŞTURMA DTO'su: Ad zorunlu + tür/marj/çarpan EZİLİYORDU)
//     PATCH /fiyat/kampanyalar/{id}           (OLUŞTURMA DTO'su: Ad/tarih zorunlu + alanlar EZİLİYORDU)
//     PATCH /urunler/{id}/stok/{subeId}       (adet yoksa stok SESSİZCE 0'a çekiliyordu)
//     PATCH /urunler/{id}                     (ad/kod boş dize → adsız ürün)
//     PATCH /urun-kategorileri/{id}           (ad boş dize → adsız kategori)
//   Fatura taslak kalem: 1000 gidiş-dönüş PATCH'te has toplamı birebir (0 birikim).
//   Kur kaynağı: MÜDÜR 403 → KesifP6HttpTestleri (bu pakette mutasyonla yeniden kanıtlandı).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class PatchSozlesmeHttpTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"PATCH {ad}");
    }

    private static async Task Alan400(HttpResponseMessage r, string alan)
    {
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var j = await r.Json();
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), "400 gövdesinde 'alanlar' yok");
        Assert.True(alanlar.TryGetProperty(alan, out _), $"400 '{alan}' alanına işaretli değil");
    }

    // ═══════════ ŞUBE ═══════════

    private Sube SubeOku(string id)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Subeler.AsNoTracking().Single(s => s.Id == id);
    }

    private string SubeHazirla(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var s = db.Subeler.Single(x => x.Id == k.SubeId);
        s.Ad = "Merkez PATCH"; s.Adres = "Kapalıçarşı No:1"; s.Telefon = "02125550000";
        db.SaveChanges();
        return s.Id;
    }

    [Fact]
    public async Task PATCH_sube_tek_alan_telefon_digerleri_SQLde_AYNI_ad_zorunlu_DEMEZ()
    {
        var k = Kur("şube tek alan");
        var id = SubeHazirla(k);
        var r = await f.Ortam.Istemci(k).PatchJson($"/api/v1/subeler/{id}", new { telefon = "02125551111" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var s = SubeOku(id);
        Assert.Equal("02125551111", s.Telefon);
        Assert.Equal("Merkez PATCH", s.Ad);
        Assert.Equal("Kapalıçarşı No:1", s.Adres);
        Assert.True(s.Aktif);
    }

    [Fact]
    public async Task PATCH_sube_bos_govde_200_hicbir_alan_DEGISMEZ()
    {
        var k = Kur("şube boş");
        var id = SubeHazirla(k);
        var r = await f.Ortam.Istemci(k).PatchJson($"/api/v1/subeler/{id}", new { });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var s = SubeOku(id);
        Assert.Equal(("Merkez PATCH", "Kapalıçarşı No:1", "02125550000", true), (s.Ad, s.Adres, s.Telefon, s.Aktif));
    }

    [Fact]
    public async Task PATCH_sube_null_adres_SILER_null_ad_KORUR_bos_ad_400_alan_ad()
    {
        var k = Kur("şube null");
        var id = SubeHazirla(k);
        var c = f.Ortam.Istemci(k);
        var sil = await c.PatchJson($"/api/v1/subeler/{id}", new Dictionary<string, object?> { ["adres"] = null });
        Assert.Equal(HttpStatusCode.OK, sil.StatusCode);
        Assert.Null(SubeOku(id).Adres);
        // PAKET PATCH-3 tutarlılık kuralı: ZORUNLU alanda null = KORU (ürün/kategori ile aynı).
        var adNull = await c.PatchJson($"/api/v1/subeler/{id}", new Dictionary<string, object?> { ["ad"] = null });
        Assert.Equal(HttpStatusCode.OK, adNull.StatusCode);
        await Alan400(await c.PatchJson($"/api/v1/subeler/{id}", new { ad = "   " }), "ad");
        Assert.Equal("Merkez PATCH", SubeOku(id).Ad);
    }

    [Fact]
    public async Task PATCH_sube_yetki_TEZGAHTAR_ve_MUHASEBE_403_kayit_DEGISMEZ()
    {
        var k = Kur("şube yetki");
        var id = SubeHazirla(k);
        foreach (var rol in new[] { "TEZGAHTAR", "MUHASEBE" })
        {
            var r = await f.Ortam.Istemci(k, rol).PatchJson($"/api/v1/subeler/{id}", new { telefon = "02120000000" });
            Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        }
        Assert.Equal("02125550000", SubeOku(id).Telefon);
        var mudur = await f.Ortam.Istemci(k, "MUDUR").PatchJson($"/api/v1/subeler/{id}", new { telefon = "02120000000" });
        Assert.Equal(HttpStatusCode.OK, mudur.StatusCode);
    }

    // ═══════════ FİYAT LİSTESİ ═══════════

    private (string varsayilanId, string id) ListeHazirla(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var now = DateTime.UtcNow;
        var v = new FiyatListesi
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, Ad = "Perakende", Tur = "PERAKENDE",
            HasMarjYuzde = 5m, IscilikCarpanYuzde = 100m, Varsayilan = true, Aktif = true, Olusturuldu = now, Guncellendi = now,
        };
        var l = new FiyatListesi
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, Ad = "Toptancı", Tur = "TOPTAN",
            HasMarjYuzde = 2.5m, IscilikCarpanYuzde = 80m, Varsayilan = false, Aktif = true, Olusturuldu = now, Guncellendi = now,
        };
        db.FiyatListeleri.AddRange(v, l);
        db.SaveChanges();
        return (v.Id, l.Id);
    }

    private FiyatListesi ListeOku(string id)
    {
        using var db = f.Vt.OlusturSistem();
        return db.FiyatListeleri.AsNoTracking().Single(x => x.Id == id);
    }

    [Fact]
    public async Task PATCH_fiyat_listesi_yalniz_aktif_200_ad_tur_marj_carpan_SQLde_AYNI()
    {
        var k = Kur("liste tek alan");
        var (_, id) = ListeHazirla(k);
        var r = await f.Ortam.Istemci(k).PatchJson($"/api/v1/fiyat/listeler/{id}", new { aktif = false });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var l = ListeOku(id);
        Assert.False(l.Aktif);
        Assert.Equal(("Toptancı", "TOPTAN", 2.5m, 80m, false), (l.Ad, l.Tur, l.HasMarjYuzde, l.IscilikCarpanYuzde, l.Varsayilan));
    }

    [Fact]
    public async Task PATCH_fiyat_listesi_bos_govde_200_DEGISMEZ_ve_null_ad_marj_400()
    {
        var k = Kur("liste boş");
        var (_, id) = ListeHazirla(k);
        var c = f.Ortam.Istemci(k);
        Assert.Equal(HttpStatusCode.OK, (await c.PatchJson($"/api/v1/fiyat/listeler/{id}", new { })).StatusCode);
        var l = ListeOku(id);
        Assert.Equal(("Toptancı", "TOPTAN", 2.5m, 80m, true), (l.Ad, l.Tur, l.HasMarjYuzde, l.IscilikCarpanYuzde, l.Aktif));

        await Alan400(await c.PatchJson($"/api/v1/fiyat/listeler/{id}", new Dictionary<string, object?> { ["ad"] = null }), "ad");
        await Alan400(await c.PatchJson($"/api/v1/fiyat/listeler/{id}", new Dictionary<string, object?> { ["hasMarjYuzde"] = null }), "hasMarjYuzde");
        await Alan400(await c.PatchJson($"/api/v1/fiyat/listeler/{id}", new Dictionary<string, object?> { ["tur"] = null }), "tur");
        Assert.Equal(("Toptancı", "TOPTAN", 2.5m), (ListeOku(id).Ad, ListeOku(id).Tur, ListeOku(id).HasMarjYuzde));
    }

    [Fact]
    public async Task PATCH_fiyat_listesi_istemcinin_TAM_govdesi_hala_calisir()
    {
        // Fiyatlandirma.razor ListeDurumDegistir gövdesi birebir — istemci KIRILMAZ.
        var k = Kur("liste istemci");
        var (_, id) = ListeHazirla(k);
        var r = await f.Ortam.Istemci(k).PatchJson($"/api/v1/fiyat/listeler/{id}", new
        {
            ad = "Toptancı 2", tur = "OZEL", hasMarjYuzde = 3m, iscilikCarpanYuzde = 90m, varsayilan = false, aktif = false,
        });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var l = ListeOku(id);
        Assert.Equal(("Toptancı 2", "OZEL", 3m, 90m, false), (l.Ad, l.Tur, l.HasMarjYuzde, l.IscilikCarpanYuzde, l.Aktif));
    }

    [Fact]
    public async Task PATCH_fiyat_listesi_yetki_TEZGAHTAR_MUHASEBE_403()
    {
        var k = Kur("liste yetki");
        var (_, id) = ListeHazirla(k);
        foreach (var rol in new[] { "TEZGAHTAR", "MUHASEBE" })
            Assert.Equal(HttpStatusCode.Forbidden,
                (await f.Ortam.Istemci(k, rol).PatchJson($"/api/v1/fiyat/listeler/{id}", new { aktif = false })).StatusCode);
        Assert.True(ListeOku(id).Aktif);
    }

    // ═══════════ KAMPANYA ═══════════

    private string KampanyaHazirla(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var now = DateTime.UtcNow;
        var bugun = DateOnly.FromDateTime(now);
        var km = new Kampanya
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, Ad = "Bayram", Baslangic = bugun, Bitis = bugun.AddDays(30),
            HedefTur = "URUN", HedefId = k.UrunId, IndirimTuru = "TUTAR_YUZDE", Deger = 7.5m, Aktif = true,
            Olusturuldu = now, Guncellendi = now,
        };
        db.Kampanyalar.Add(km);
        db.SaveChanges();
        return km.Id;
    }

    private Kampanya KampanyaOku(string id)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Kampanyalar.AsNoTracking().Single(x => x.Id == id);
    }

    [Fact]
    public async Task PATCH_kampanya_yalniz_aktif_200_tarih_hedef_indirim_deger_SQLde_AYNI()
    {
        var k = Kur("kampanya tek alan");
        var id = KampanyaHazirla(k);
        var once = KampanyaOku(id);
        var r = await f.Ortam.Istemci(k).PatchJson($"/api/v1/fiyat/kampanyalar/{id}", new { aktif = false });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var s = KampanyaOku(id);
        Assert.False(s.Aktif);
        Assert.Equal((once.Ad, once.Baslangic, once.Bitis, once.HedefTur, once.HedefId, once.IndirimTuru, once.Deger),
                     (s.Ad, s.Baslangic, s.Bitis, s.HedefTur, s.HedefId, s.IndirimTuru, s.Deger));
    }

    [Fact]
    public async Task PATCH_kampanya_bos_govde_200_DEGISMEZ_null_tarih_deger_400()
    {
        var k = Kur("kampanya boş");
        var id = KampanyaHazirla(k);
        var once = KampanyaOku(id);
        var c = f.Ortam.Istemci(k);
        Assert.Equal(HttpStatusCode.OK, (await c.PatchJson($"/api/v1/fiyat/kampanyalar/{id}", new { })).StatusCode);
        var s = KampanyaOku(id);
        Assert.Equal((once.Ad, once.Bitis, once.Deger, once.Aktif), (s.Ad, s.Bitis, s.Deger, s.Aktif));

        await Alan400(await c.PatchJson($"/api/v1/fiyat/kampanyalar/{id}", new Dictionary<string, object?> { ["bitis"] = null }), "bitis");
        await Alan400(await c.PatchJson($"/api/v1/fiyat/kampanyalar/{id}", new Dictionary<string, object?> { ["deger"] = null }), "deger");
        await Alan400(await c.PatchJson($"/api/v1/fiyat/kampanyalar/{id}", new { ad = "" }), "ad");
    }

    [Fact]
    public async Task PATCH_kampanya_tek_alan_birlesik_kayitla_DOGRULANIR()
    {
        // Yalnız bitiş gönderilir; mevcut BAŞLANGIÇTAN önceyse 400 — birleşik kayıt doğrulanır.
        var k = Kur("kampanya birleşik");
        var id = KampanyaHazirla(k);
        var once = KampanyaOku(id);
        var r = await f.Ortam.Istemci(k).PatchJson($"/api/v1/fiyat/kampanyalar/{id}",
            new { bitis = once.Baslangic.AddDays(-1).ToString("yyyy-MM-dd") });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(once.Bitis, KampanyaOku(id).Bitis);
        var ok = await f.Ortam.Istemci(k).PatchJson($"/api/v1/fiyat/kampanyalar/{id}",
            new { bitis = once.Baslangic.AddDays(10).ToString("yyyy-MM-dd") });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var s = KampanyaOku(id);
        Assert.Equal((once.Baslangic.AddDays(10), once.Deger, "URUN"), (s.Bitis, s.Deger, s.HedefTur));
    }

    [Fact]
    public async Task PATCH_kampanya_yetki_TEZGAHTAR_403()
    {
        var k = Kur("kampanya yetki");
        var id = KampanyaHazirla(k);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await f.Ortam.Istemci(k, "TEZGAHTAR").PatchJson($"/api/v1/fiyat/kampanyalar/{id}", new { aktif = false })).StatusCode);
        Assert.True(KampanyaOku(id).Aktif);
    }

    // ═══════════ STOK SAYIM ═══════════

    private (decimal adet, int hareket) StokOku(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return (db.Stoklar.AsNoTracking().Single(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId).Adet,
                db.Hareketler.AsNoTracking().Count(h => h.TenantId == k.TenantId));
    }

    [Fact]
    public async Task PATCH_stok_bos_govde_400_stok_SIFIRLANMAZ_deftere_YAZILMAZ()
    {
        var k = Kur("stok boş");
        var once = StokOku(k);
        Assert.Equal(100m, once.adet);
        var r = await f.Ortam.Istemci(k).PatchJson($"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", new { });
        await Alan400(r, "adet");
        Assert.Equal(once, StokOku(k));
    }

    [Fact]
    public async Task PATCH_stok_adet_verilince_yazar_yetki_TEZGAHTAR_403()
    {
        var k = Kur("stok adet");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await f.Ortam.Istemci(k, "TEZGAHTAR").PatchJson($"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", new { adet = 95 })).StatusCode);
        Assert.Equal(100m, StokOku(k).adet);
        var r = await f.Ortam.Istemci(k).PatchJson($"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", new { adet = 95 });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(95m, StokOku(k).adet);
    }

    // ── tur 2 (hasım KRİTİK ×4): eksi · kolon tavanı · yarım binde · tırnaklı sayı ──

    private static Task<HttpResponseMessage> HamPatch(HttpClient c, string yol, string json) =>
        c.PatchAsync(yol, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

    [Fact]
    public async Task PATCH_stok_eksi_adet_400_alan_adet_stok_ve_defter_DEGISMEZ()
    {
        var k = Kur("stok eksi");
        var once = StokOku(k);
        await Alan400(await f.Ortam.Istemci(k).PatchJson($"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", new { adet = -1 }), "adet");
        await Alan400(await f.Ortam.Istemci(k).PatchJson($"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", new { adet = -0.001m }), "adet");
        Assert.Equal(once, StokOku(k));
    }

    [Fact]
    public async Task PATCH_stok_kolon_tavanini_asan_adet_400_alan_adet_500_DEGIL()
    {
        var k = Kur("stok tavan");
        var once = StokOku(k);
        var c = f.Ortam.Istemci(k);
        // adet kolona sığar ama adet × has gram decimal(15,6) has kolonunu taşırır
        await Alan400(await HamPatch(c, $"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", "{\"adet\":999999999.99}"), "adet");
        Assert.Equal(once, StokOku(k));
        // has gramı 0,5 olan üründe has kolona sığar; YALNIZ adet kolonu decimal(12,3) tavanı aşılır
        using (var db = f.Vt.OlusturSistem())
        {
            var u = db.Urunler.Single(x => x.Id == k.UrunId);
            u.HasGrami = 0.5m; db.SaveChanges();
        }
        await Alan400(await HamPatch(c, $"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", "{\"adet\":1000000000}"), "adet");
        Assert.Equal(once, StokOku(k));
    }

    [Fact]
    public async Task PATCH_stok_yarim_binde_AwayFromZero_1_0005_1_001_olur()
    {
        var k = Kur("stok yarım");
        using (var g = f.Vt.OlusturSistem()) TestKurulum.UrunuGramBirimiYap(g, k.UrunId);   // PAKET GOC-C (K14): kesirli miktar yalnız GRAM birimde — sınır ölçüsü GRAM birimli üründe korunur
        var r = await HamPatch(f.Ortam.Istemci(k), $"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", "{\"adet\":1.0005}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(1.001m, StokOku(k).adet);
    }

    [Theory]
    [InlineData("\"45.000\"")]
    [InlineData("\"1.500\"")]
    [InlineData("\"7\"")]
    public async Task PATCH_stok_tirnakli_sayi_400_stok_DEGISMEZ(string deger)
    {
        var k = Kur("stok dize");
        var once = StokOku(k);
        var r = await HamPatch(f.Ortam.Istemci(k), $"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", "{\"adet\":" + deger + "}");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(once, StokOku(k));
    }

    [Theory]
    [InlineData("hasMarjYuzde", "\"1.500\"")]
    [InlineData("iscilikCarpanYuzde", "\"45.000\"")]
    public async Task PATCH_fiyat_listesi_tirnakli_sayi_400_marj_carpan_DEGISMEZ(string alan, string deger)
    {
        var k = Kur("liste dize");
        var (_, id) = ListeHazirla(k);
        var r = await HamPatch(f.Ortam.Istemci(k), $"/api/v1/fiyat/listeler/{id}", "{\"" + alan + "\":" + deger + "}");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var l = ListeOku(id);
        Assert.Equal((2.5m, 80m), (l.HasMarjYuzde, l.IscilikCarpanYuzde));
    }

    [Fact]
    public async Task PATCH_kampanya_tirnakli_deger_400_deger_DEGISMEZ()
    {
        var k = Kur("kampanya dize");
        var id = KampanyaHazirla(k);
        var r = await HamPatch(f.Ortam.Istemci(k), $"/api/v1/fiyat/kampanyalar/{id}", "{\"deger\":\"45.000\"}");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(7.5m, KampanyaOku(id).Deger);
        // sayı olarak gönderilen değer hâlâ kabul edilir (istemci sözleşmesi)
        Assert.Equal(HttpStatusCode.OK, (await HamPatch(f.Ortam.Istemci(k), $"/api/v1/fiyat/kampanyalar/{id}", "{\"deger\":8.25}")).StatusCode);
        Assert.Equal(8.25m, KampanyaOku(id).Deger);
    }

    // ═══════════ ÜRÜN ═══════════

    private Urun UrunOku(string id)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Urunler.AsNoTracking().Single(u => u.Id == id);
    }

    [Fact]
    public async Task PATCH_urun_tek_alan_aciklama_ad_kod_AYNI_bos_ad_kod_400()
    {
        var k = Kur("ürün");
        var once = UrunOku(k.UrunId);
        var c = f.Ortam.Istemci(k);
        Assert.Equal(HttpStatusCode.OK, (await c.PatchJson($"/api/v1/urunler/{k.UrunId}", new { aciklama = "Vitrin 3" })).StatusCode);
        var s = UrunOku(k.UrunId);
        Assert.Equal(("Vitrin 3", once.Ad, once.Kod, once.GramAgirlik), (s.Aciklama, s.Ad, s.Kod, s.GramAgirlik));

        Assert.Equal(HttpStatusCode.OK, (await c.PatchJson($"/api/v1/urunler/{k.UrunId}", new { })).StatusCode);
        Assert.Equal((once.Ad, once.Kod), (UrunOku(k.UrunId).Ad, UrunOku(k.UrunId).Kod));

        await Alan400(await c.PatchJson($"/api/v1/urunler/{k.UrunId}", new { ad = "  " }), "ad");
        await Alan400(await c.PatchJson($"/api/v1/urunler/{k.UrunId}", new { kod = "" }), "kod");
        Assert.Equal((once.Ad, once.Kod), (UrunOku(k.UrunId).Ad, UrunOku(k.UrunId).Kod));

        Assert.Equal(HttpStatusCode.Forbidden,
            (await f.Ortam.Istemci(k, "TEZGAHTAR").PatchJson($"/api/v1/urunler/{k.UrunId}", new { aciklama = "x" })).StatusCode);
    }

    [Fact]
    public async Task PATCH_urun_ad_kod_KIRPILARAK_yazilir_kirpilmis_kod_CAKISIR()
    {
        var k = Kur("ürün kırp");
        var c = f.Ortam.Istemci(k);
        Assert.Equal(HttpStatusCode.OK, (await c.PatchJson($"/api/v1/urunler/{k.UrunId}", new { ad = "  Burma bilezik  ", kod = "  PS-KIRP  " })).StatusCode);
        var s = UrunOku(k.UrunId);
        Assert.Equal(("Burma bilezik", "PS-KIRP"), (s.Ad, s.Kod));

        // ikinci ürün: kırpılmış hâli birinciyle aynı olan kod → 409 çakışma, kod DEĞİŞMEZ
        string ikinci;
        using (var db = f.Vt.OlusturSistem())
        {
            var u = db.Urunler.AsNoTracking().Single(x => x.Id == k.UrunId);
            u.Id = Kimlik.Ulid(); u.Kod = "PS-DIGER"; u.Barkod = null;
            db.Urunler.Add(u); db.SaveChanges();
            ikinci = u.Id;
        }
        var r = await c.PatchJson($"/api/v1/urunler/{ikinci}", new { kod = " PS-KIRP " });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        // servisin ön kontrolü (kırpılmış kodla) yakalar — veritabanı benzersizlik ağına düşmez
        var hata = await r.Json();   // Türkçe JSON gövdesi AYRIŞTIRILARAK sınanır (\u0027 kaçışı)
        Assert.Equal("'PS-KIRP' kodu zaten kullanımda", hata.GetProperty("message").GetString());
        Assert.True(hata.GetProperty("alanlar").TryGetProperty("kod", out _));
        Assert.Equal("PS-DIGER", UrunOku(ikinci).Kod);
    }

    // ═══════════ KATEGORİ ═══════════

    [Fact]
    public async Task PATCH_kategori_tek_alan_sira_ad_AYNI_bos_ad_400_yetki_TEZGAHTAR_403()
    {
        var k = Kur("kategori");
        string id;
        using (var db = f.Vt.OlusturSistem())
        {
            var now = DateTime.UtcNow;
            var kat = new UrunKategorisi { Id = Kimlik.Ulid(), TenantId = k.TenantId, Ad = "Bilezik", Sira = 1, Aktif = true, Olusturuldu = now, Guncellendi = now };
            db.UrunKategorileri.Add(kat);
            db.SaveChanges();
            id = kat.Id;
        }
        UrunKategorisi Oku() { using var o = f.Vt.OlusturSistem(); return o.UrunKategorileri.AsNoTracking().Single(x => x.Id == id); }
        var c = f.Ortam.Istemci(k);
        Assert.Equal(HttpStatusCode.OK, (await c.PatchJson($"/api/v1/urun-kategorileri/{id}", new { sira = 5 })).StatusCode);
        Assert.Equal(("Bilezik", 5, true), (Oku().Ad, Oku().Sira, Oku().Aktif));
        Assert.Equal(HttpStatusCode.OK, (await c.PatchJson($"/api/v1/urun-kategorileri/{id}", new { })).StatusCode);
        Assert.Equal(("Bilezik", 5), (Oku().Ad, Oku().Sira));
        await Alan400(await c.PatchJson($"/api/v1/urun-kategorileri/{id}", new { ad = " " }), "ad");
        Assert.Equal("Bilezik", Oku().Ad);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await f.Ortam.Istemci(k, "TEZGAHTAR").PatchJson($"/api/v1/urun-kategorileri/{id}", new { sira = 9 })).StatusCode);
        Assert.Equal(5, Oku().Sira);
    }
}

[Collection("veritabani")]
public sealed class PatchSozlesmeFaturaTestleri(TestVeritabani vt)
{
    /// <summary>
    /// Kuyruk 30.09 08:50 (HAS-KUR hasım bulgusu): taslak kalem PATCH'inde has orantıyla yeniden
    /// hesaplanınca 6. hanede birikim oluyordu. Kalem birim has saklar, toplam = birim × miktar.
    /// 1000 gidiş-dönüş (3 → 1,001 → 3 …) sonunda has toplamı ve başlık has'ı BİREBİR ilk değer.
    /// </summary>
    [Fact]
    public async Task Fatura_taslak_kalem_1000_gidis_donus_PATCH_has_toplami_BIREBIR()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PATCH fatura 1000");
        using (var s = vt.OlusturSistem())
        {
            var u = await s.Urunler.SingleAsync(x => x.Id == k.UrunId);
            u.HasGrami = 1.234567m;
            await s.SaveChangesAsync();
        }
        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 60_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 3m, BirimFiyat = 20_000m }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kid = db.FaturaKalemleri.Single(x => x.FaturaId == fid).Id;

        (decimal kalem, decimal? baslik) Has()
        {
            using var o = vt.OlusturSistem();
            return (o.FaturaKalemleri.AsNoTracking().Single(x => x.Id == kid).HasGrami!.Value,
                    o.Faturalar.AsNoTracking().Single(x => x.Id == fid).HasGram);
        }
        var ilk = Has();
        Assert.Equal(3.703701m, ilk.kalem);

        var git = JsonDocument.Parse("{\"miktar\":1.001}").RootElement;
        var don = JsonDocument.Parse("{\"miktar\":3}").RootElement;
        for (var i = 0; i < 1000; i++)
        {
            await servis.TaslakKalemGuncelle(fid, kid, git, k.Islem);
            await servis.TaslakKalemGuncelle(fid, kid, don, k.Islem);
        }
        Assert.Equal(ilk, Has());   // 0 birikim
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PATCH-SOZLESME TUR 3 (02.10) — KOLON KAPASİTESİ TARAMASI + SAYIM HAS YUVARLAMASI
//   Hasım KRİTİK: kampanya PATCH {"deger":1e14} → 500 (kampanyalar.deger decimal(15,2)).
//   Aynı sınıf hata tarama ile bulunan PATCH uçlarında da kapatıldı (Kasa/Cari/Satış/
//   Virman/Çıktı HARİÇ): her vakada 400 + alan işareti, SQL'de satır DEĞİŞMEZ, 500 YOK.
//   Hasım ORTA: sayım defterine yazılan has fatura kuralıyla (Yuvarla6 = AwayFromZero).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class PatchSozlesmeKolonTavaniTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"PATCH3 {ad}");
    }

    private static Task<HttpResponseMessage> Ham(HttpClient c, string yol, string json) =>
        c.PatchAsync(yol, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

    /// <summary>400 + alanlar.{alan}; 500 ise gövde iletide görünür.</summary>
    private static async Task Alan400(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), "400 gövdesinde 'alanlar' yok: " + govde);
        Assert.True(alanlar.TryGetProperty(alan, out _), $"400 '{alan}' alanına işaretli değil: {govde}");
    }

    // ═══════════ KAMPANYA (hasım KRİTİK) ═══════════

    private string Kampanya(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var now = DateTime.UtcNow;
        var bugun = DateOnly.FromDateTime(now);
        var km = new Kampanya
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, Ad = "Tavan", Baslangic = bugun, Bitis = bugun.AddDays(10),
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
    [InlineData("100000000000000")]
    [InlineData("9999999999999.995")]   // kolona yuvarlanınca 10^13 → taşar
    [InlineData("1e30")]                 // decimal aralığı dışı
    public async Task PATCH_kampanya_kolon_tavanini_asan_deger_400_alan_deger_SQL_DEGISMEZ(string deger)
    {
        var k = Kur("kampanya tavan");
        var id = Kampanya(k);
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/fiyat/kampanyalar/{id}", $"{{\"deger\":{deger}}}");
        if (deger == "1e30")
        {   // decimal'e sığmayan sayı çerçevenin JSON çözücüsünde 400 olur (alan anahtarı "$.deger" — T2.9 genel açık)
            Assert.True(r.StatusCode == HttpStatusCode.BadRequest, (int)r.StatusCode + " " + await r.Content.ReadAsStringAsync());
        }
        else await Alan400(r, "deger");
        if (deger == "100000000000000")
        {   // DTO kapısının iletisi YANILTICI olmamalı ("0,01 veya daha büyük" DEĞİL; aralık söylenir)
            var ileti = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement
                .GetProperty("alanlar").GetProperty("deger")[0].GetString()!;
            Assert.Contains("arasında", ileti);
            Assert.DoesNotContain("veya daha büyük", ileti);
        }
        Assert.Equal(250m, KampanyaDeger(id));
    }

    [Fact]
    public async Task PATCH_kampanya_tam_tavan_9999999999999_99_200_kolonda_birebir()
    {
        var k = Kur("kampanya tam tavan");
        var id = Kampanya(k);
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/fiyat/kampanyalar/{id}", "{\"deger\":9999999999999.99}");
        Assert.True(r.StatusCode == HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        Assert.Equal(9_999_999_999_999.99m, KampanyaDeger(id));
        // kolonun 2 hanesine yuvarlanınca tavana eşit olan değer REDDEDİLMEZ (fazla hane kolonda yuvarlanır)
        var r2 = await Ham(f.Ortam.Istemci(k), $"/api/v1/fiyat/kampanyalar/{id}", "{\"deger\":9999999999999.994}");
        Assert.True(r2.StatusCode == HttpStatusCode.OK, await r2.Content.ReadAsStringAsync());
        Assert.Equal(9_999_999_999_999.99m, KampanyaDeger(id));
    }

    /// <summary>Servis kapısı (controller DataAnnotations'ı OLMADAN) — PATCH ve POST aynı kapıdan geçer.</summary>
    [Fact]
    public async Task Kampanya_servis_kapisi_PATCH_ve_POST_kolon_tavani_400_alan_deger()
    {
        var k = Kur("kampanya servis");
        var id = Kampanya(k);
        using (var db = f.Vt.Olustur(k.TenantId))
        {
            var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
                new Kuyumcu.Api.Modules.Fiyat.FiyatServisi(db).KampanyaGuncelle(id,
                    new Kuyumcu.Api.Modules.Fiyat.KampanyaGuncelleDto { Deger = 100_000_000_000_000m }, k.Islem));
            Assert.Equal("deger", AlanIsareti.Alan(ex));
        }
        Assert.Equal(250m, KampanyaDeger(id));
        using (var db = f.Vt.Olustur(k.TenantId))
        {
            var bugun = DateOnly.FromDateTime(DateTime.UtcNow);
            var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
                new Kuyumcu.Api.Modules.Fiyat.FiyatServisi(db).KampanyaOlustur(new Kuyumcu.Api.Modules.Fiyat.KampanyaDto
                {
                    Ad = "Taşan", Baslangic = bugun, Bitis = bugun.AddDays(5), HedefTur = "TUMU",
                    IndirimTuru = "TUTAR_TL", Deger = 100_000_000_000_000m,
                }, k.Islem));
            Assert.Equal("deger", AlanIsareti.Alan(ex));
        }
        using var o = f.Vt.OlusturSistem();
        Assert.Equal(1, o.Kampanyalar.AsNoTracking().Count(x => x.TenantId == k.TenantId));
    }

    /// <summary>DTO Range üst sınırı kolon kapasitesine çekildi (controller yolu ilk kapı).</summary>
    [Fact]
    public void Kampanya_DTO_Range_ust_siniri_kolon_kapasitesi()
    {
        static bool Gecerli(object dto)
        {
            var sonuc = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            return System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
                dto, new System.ComponentModel.DataAnnotations.ValidationContext(dto), sonuc, true)
                || sonuc.All(s => !s.MemberNames.Contains("Deger"));
        }
        var bugun = DateOnly.FromDateTime(DateTime.UtcNow);
        Kuyumcu.Api.Modules.Fiyat.KampanyaDto Olustur(decimal d) => new()
        { Ad = "x", Baslangic = bugun, Bitis = bugun, HedefTur = "TUMU", IndirimTuru = "TUTAR_TL", Deger = d };
        Assert.True(Gecerli(Olustur(9_999_999_999_999.99m)));
        Assert.False(Gecerli(Olustur(100_000_000_000_000m)));
        Assert.True(Gecerli(new Kuyumcu.Api.Modules.Fiyat.KampanyaGuncelleDto { Deger = 9_999_999_999_999.99m }));
        Assert.False(Gecerli(new Kuyumcu.Api.Modules.Fiyat.KampanyaGuncelleDto { Deger = 100_000_000_000_000m }));
    }

    // ═══════════ STOK SAYIMI — has yuvarlaması fatura kuralı (hasım ORTA) ═══════════

    [Fact]
    public async Task PATCH_stok_sayim_defter_has_yarim_milyonda_AwayFromZero_fatura_ile_AYNI()
    {
        var k = Kur("sayım has yarım");
        using (var g = f.Vt.OlusturSistem()) TestKurulum.UrunuGramBirimiYap(g, k.UrunId);   // PAKET GOC-C (K14): kesirli sayım yalnız GRAM birimde — yarım milyon ölçüsü orada
        using (var db = f.Vt.OlusturSistem())
        {
            var u = db.Urunler.Single(x => x.Id == k.UrunId);
            u.HasGrami = 0.916667m; db.SaveChanges();
        }
        // fark 1,5 × 0,916667 = 1,3750005 → AwayFromZero 1,375001 (ToEven 1,375000)
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", "{\"adet\":101.5}");
        Assert.True(r.StatusCode == HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        using var o = f.Vt.OlusturSistem();
        var hareket = o.Hareketler.AsNoTracking().Single(h => h.TenantId == k.TenantId && h.Tur == "SAYIM_DUZELTME");
        Assert.Equal(MFatura.FaturaServisi.KalemToplamHas(1.5m, 0.916667m), hareket.HasGrami);
        Assert.Equal(1.375001m, hareket.HasGrami);
        // eksi yönde de simetrik: 100 → 98,5 (fark −1,5) → −1,375001
        var r2 = await Ham(f.Ortam.Istemci(k), $"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", "{\"adet\":100}");
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        using var o2 = f.Vt.OlusturSistem();
        Assert.Contains(o2.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId && h.Tur == "SAYIM_DUZELTME")
            .Select(h => h.HasGrami).ToList(), h => h == -1.375001m);
    }

    [Fact]
    public async Task PATCH_stok_sayim_stok_toplam_has_yarim_milyonda_AwayFromZero()
    {
        var k = Kur("sayım toplam has");
        using (var g = f.Vt.OlusturSistem()) TestKurulum.UrunuGramBirimiYap(g, k.UrunId);   // PAKET GOC-C (K14): kesirli sayım yalnız GRAM birimde — yarım milyon ölçüsü orada
        using (var db = f.Vt.OlusturSistem())
        {
            var u = db.Urunler.Single(x => x.Id == k.UrunId);
            u.HasGrami = 0.916667m; db.SaveChanges();
        }
        // 1,5 × 0,916667 = 1,3750005 → stok.toplam_has_gram 1,375001
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/urunler/{k.UrunId}/stok/{k.SubeId}", "{\"adet\":1.5}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        using var o = f.Vt.OlusturSistem();
        Assert.Equal(1.375001m, o.Stoklar.AsNoTracking().Single(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId).ToplamHasGram);
    }

    // ═══════════ ÜRÜN PATCH — gram/taş ağırlığı decimal(12,4) + stok toplam has decimal(15,6) ═══════════

    private string UrunOku(string id)
    {
        using var db = f.Vt.OlusturSistem();
        var u = db.Urunler.AsNoTracking().Single(x => x.Id == id);
        return $"{u.GramAgirlik}|{u.TasAgirlik}|{u.HasGrami}|{u.SatisFiyati}|{u.AlisMaliyeti}|{u.KonsinyeBirimBedel}|{u.IscilikTl}|{u.MinStok}";
    }

    [Theory]
    [InlineData("gramAgirlik", "100000000")]
    [InlineData("gramAgirlik", "1e30")]
    [InlineData("tasAgirlik", "100000000")]
    [InlineData("satisFiyati", "10000000000000")]
    [InlineData("alisMaliyeti", "10000000000000")]
    [InlineData("konsinyeBirimBedel", "10000000000000")]
    [InlineData("iscilikTl", "10000000000")]
    [InlineData("minStok", "1000000000")]
    public async Task PATCH_urun_agirlik_kolon_tavanini_asar_400_alan_SQL_DEGISMEZ(string alan, string deger)
    {
        var k = Kur("ürün ağırlık");
        using (var db = f.Vt.OlusturSistem())
        {   // stok 0: türetilmiş stok has kapısı (ayrı test) ağırlık kapısını gölgelemesin
            foreach (var s in db.Stoklar.Where(x => x.UrunId == k.UrunId)) s.Adet = 0m;
            db.SaveChanges();
        }
        var once = UrunOku(k.UrunId);
        var r = await Ham(f.Ortam.Istemci(k), $"/api/v1/urunler/{k.UrunId}", $"{{\"{alan}\":{deger}}}");
        if (deger == "1e30")
        {   // decimal'e sığmayan sayı çerçevenin JSON çözücüsünde 400 olur (500 değil)
            Assert.True(r.StatusCode == HttpStatusCode.BadRequest, (int)r.StatusCode + " " + await r.Content.ReadAsStringAsync());
        }
        else await Alan400(r, alan);
        Assert.Equal(once, UrunOku(k.UrunId));
    }

    [Fact]
    public async Task PATCH_urun_gram_artinca_stok_toplam_has_tasarsa_400_urun_ve_stok_DEGISMEZ()
    {
        var k = Kur("ürün stok has");
        using (var db = f.Vt.OlusturSistem())
        {
            var s = db.Stoklar.Single(x => x.UrunId == k.UrunId && x.SubeId == k.SubeId);
            s.Adet = 900_000_000m; db.SaveChanges();
        }
        var once = UrunOku(k.UrunId);
        // gram kolona sığar (12,4) ama 900 milyon adet × yeni has stok.toplam_has_gram (15,6) taşar
        await Alan400(await Ham(f.Ortam.Istemci(k), $"/api/v1/urunler/{k.UrunId}", "{\"gramAgirlik\":5000}"), "gramAgirlik");
        Assert.Equal(once, UrunOku(k.UrunId));
        using var o = f.Vt.OlusturSistem();
        Assert.Equal(900_000_000m, o.Stoklar.AsNoTracking().Single(x => x.UrunId == k.UrunId && x.SubeId == k.SubeId).Adet);
    }

    // ═══════════ FATURA taslak kalem / başlık ═══════════

    private static async Task<(string Fid, string Kid)> Taslak(Kiraci k, HttpClient c)
    {
        var y = await c.PostAsJsonAsync("/api/v1/faturalar", new
        {
            cariId = k.CariId, tur = "SATIS", toplam = 2000m, kdvOran = 0.20m,
            kalemler = new object[] { new { aciklama = "Tavan bilezik", miktar = 2m, birimFiyat = 1000m } },
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
        return $"{fa.Toplam}|{fa.KdvTutar}|{fa.AraToplam}|" + string.Join(";", ka.Select(x =>
            $"{x.Miktar}/{x.BirimFiyat}/{x.IscilikTl}/{x.IndirimTutari}/{x.SatirToplam}/{x.KdvTutar}"));
    }

    [Theory]
    [InlineData("{\"miktar\":1000000000}", "miktar")]
    [InlineData("{\"miktar\":1e30}", "miktar")]
    [InlineData("{\"miktar\":0.001,\"birimFiyat\":100000000000000}", "birimFiyat")]   // satır 10^11 sığar, birim fiyat taşar
    [InlineData("{\"iscilikTl\":10000000000}", "iscilikTl")]
    [InlineData("{\"indirimTutari\":10000000000000}", "indirimTutari")]
    [InlineData("{\"miktar\":999999,\"birimFiyat\":100000000}", "miktar")]   // her biri sığar, satır toplamı taşar
    [InlineData("{\"iscilikTl\":1e30}", "iscilikTl")]
    public async Task PATCH_fatura_kalem_kolon_tavani_400_alan_fatura_ve_kalem_DEGISMEZ(string json, string alan)
    {
        var k = Kur("fatura kalem tavan");
        var c = f.Ortam.Istemci(k);
        var (fid, kid) = await Taslak(k, c);
        var once = FaturaDurum(fid);
        await Alan400(await Ham(c, $"/api/v1/faturalar/{fid}/kalemler/{kid}", json), alan);
        Assert.Equal(once, FaturaDurum(fid));
    }

    [Fact]
    public async Task PATCH_fatura_kalem_baslik_toplami_tasarsa_400_ILK_kayit_da_YAZILMAZ()
    {
        var k = Kur("fatura başlık tavan");
        var c = f.Ortam.Istemci(k);
        var y = await c.PostAsJsonAsync("/api/v1/faturalar", new
        {
            cariId = k.CariId, tur = "SATIS", toplam = 2000m, kdvOran = 0.20m,
            kalemler = new object[]
            {
                new { aciklama = "A", miktar = 1m, birimFiyat = 1000m },
                new { aciklama = "B", miktar = 1m, birimFiyat = 1000m },
            },
        });
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var fid = (await y.Json()).GetProperty("id").GetString()!;
        var ids = (await (await c.GetAsync($"/api/v1/faturalar/{fid}/kalemler")).Json())
            .GetProperty("kalemler").EnumerateArray().Select(x => x.GetProperty("id").GetString()!).ToList();
        // A: 6 trilyon (sığar) → 200; B: 6 trilyon (satır sığar, başlık 12 trilyon taşar) → 400
        var a = await Ham(c, $"/api/v1/faturalar/{fid}/kalemler/{ids[0]}", "{\"birimFiyat\":6000000000000}");
        Assert.True(a.StatusCode == HttpStatusCode.OK, await a.Content.ReadAsStringAsync());
        var once = FaturaDurum(fid);
        await Alan400(await Ham(c, $"/api/v1/faturalar/{fid}/kalemler/{ids[1]}", "{\"birimFiyat\":6000000000000}"), "birimFiyat");
        Assert.Equal(once, FaturaDurum(fid));
    }

    [Theory]
    [InlineData("10000000000000")]
    [InlineData("1e30")]
    public async Task PATCH_fatura_baslik_toplam_kolon_tavani_400_alan_toplam_DEGISMEZ(string deger)
    {
        var k = Kur("fatura başlık toplam");
        var c = f.Ortam.Istemci(k);
        var y = await c.PostAsJsonAsync("/api/v1/faturalar", new { cariId = k.CariId, tur = "SATIS", toplam = 1200m, kdvOran = 0.20m });
        Assert.True(y.StatusCode == HttpStatusCode.OK, await y.Content.ReadAsStringAsync());
        var fid = (await y.Json()).GetProperty("id").GetString()!;
        var once = FaturaDurum(fid);
        await Alan400(await Ham(c, $"/api/v1/faturalar/{fid}", $"{{\"toplam\":{deger}}}"), "toplam");
        Assert.Equal(once, FaturaDurum(fid));
        var ok = await Ham(c, $"/api/v1/faturalar/{fid}", "{\"toplam\":2400}");
        Assert.True(ok.StatusCode == HttpStatusCode.OK, await ok.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PATCH_fatura_urunlu_kalem_has_gram_kolonu_tasarsa_400_alan_miktar_DEGISMEZ()
    {
        var k = Kur("fatura has tavan");
        Assert.True(k.UrunHasGrami > 1.000001m, "fikstür ürününün has gramı 1'den büyük olmalı");
        var c = f.Ortam.Istemci(k);
        var y = await c.PostAsJsonAsync("/api/v1/faturalar", new
        {
            cariId = k.CariId, tur = "SATIS", toplam = 1000m, kdvOran = 0.20m,
            kalemler = new object[] { new { urunId = k.UrunId, aciklama = "Has bilezik", miktar = 1m, birimFiyat = 1000m } },
        });
        Assert.True(y.StatusCode == HttpStatusCode.OK, await y.Content.ReadAsStringAsync());
        var fid = (await y.Json()).GetProperty("id").GetString()!;
        var kid = (await (await c.GetAsync($"/api/v1/faturalar/{fid}/kalemler")).Json())
            .GetProperty("kalemler").EnumerateArray().Single().GetProperty("id").GetString()!;
        var once = FaturaDurum(fid);
        // miktar kolona sığar (12,3), satır toplamı ~10^12 sığar; miktar × birim has decimal(15,6) taşar
        await Alan400(await Ham(c, $"/api/v1/faturalar/{fid}/kalemler/{kid}", "{\"miktar\":999999999}"), "miktar");
        Assert.Equal(once, FaturaDurum(fid));
    }

    // ═══════════ SİPARİŞ ═══════════

    private async Task<(HttpClient c, string id)> Siparis(Kiraci k)
    {
        var c = f.Ortam.Istemci(k);
        var y = await c.PostAsJsonAsync("/api/v1/siparisler", new
        {
            tur = "MUSTERI_SIPARISI", cariId = k.CariId,
            sozVerilenGun = Zaman.YerelBugun().AddDays(3).ToString("yyyy-MM-dd"),
            aciklama = "tavan", kalemler = new[] { new { urunId = k.UrunId, adet = 1m } },
        });
        Assert.True(y.StatusCode == HttpStatusCode.OK, await y.Content.ReadAsStringAsync());
        return (c, (await y.Json()).GetProperty("id").GetString()!);
    }

    private string SiparisDurum(string id)
    {
        using var db = f.Vt.OlusturSistem();
        var s = db.Siparisler.AsNoTracking().Single(x => x.Id == id);
        var ka = db.SiparisKalemleri.AsNoTracking().Where(x => x.SiparisId == id).OrderBy(x => x.Id).ToList();
        return $"{s.TahminiTutarTl}|{s.KurSnapshotId}|" + string.Join(";", ka.Select(x => $"{x.Id}/{x.Adet}/{x.GramAgirlik}"));
    }

    [Theory]
    [InlineData("tahminiTutarTl", "1000000000000")]
    [InlineData("tahminiTutarTl", "1e30")]
    [InlineData("adet", "1000000000")]
    [InlineData("adet", "1e30")]
    [InlineData("gramAgirlik", "1000000000")]
    public async Task PATCH_siparis_kolon_tavani_400_alan_siparis_DEGISMEZ(string alan, string deger)
    {
        var k = Kur("sipariş tavan");
        var (c, id) = await Siparis(k);
        var once = SiparisDurum(id);
        string kid;
        using (var db = f.Vt.OlusturSistem())
            kid = db.SiparisKalemleri.AsNoTracking().Single(x => x.SiparisId == id).Id;
        var json = alan == "tahminiTutarTl"
            ? $"{{\"tahminiTutarTl\":{deger},\"kurSnapshotId\":\"{k.KurId}\"}}"
            : $"{{\"kalemler\":[{{\"id\":\"{kid}\",\"{alan}\":{deger}}}]}}";
        await Alan400(await Ham(c, $"/api/v1/siparisler/{id}", json), alan);
        Assert.Equal(once, SiparisDurum(id));
    }

    // ═══════════ VEZNE FİŞİ ═══════════

    private string VezneDurum(string id)
    {
        using var db = f.Vt.OlusturSistem();
        var fi = db.VezneFisleri.AsNoTracking().Single(x => x.Id == id);
        var ka = db.VezneFisKalemleri.AsNoTracking().Where(x => x.FisId == id).OrderBy(x => x.Sira).ToList();
        return $"{fi.TalepTutarTl}|" + string.Join(";", ka.Select(x => $"{x.Adet}/{x.BirimFiyatTl}/{x.IscilikTl}"));
    }

    [Theory]
    [InlineData("{\"adet\":1000000000,\"birimFiyatTl\":1}")]
    [InlineData("{\"adet\":0.001,\"birimFiyatTl\":100000000000000}")]   // satır 10^11 sığar, birim fiyat taşar
    [InlineData("{\"adet\":1,\"birimFiyatTl\":1,\"iscilikTl\":10000000000000}")]
    [InlineData("{\"adet\":1000000,\"birimFiyatTl\":10000000000}")]   // her biri sığar, satır taşar
    [InlineData("{\"adet\":1e30,\"birimFiyatTl\":1}")]
    public async Task PATCH_vezne_kalem_kolon_tavani_400_alan_kalemler_fis_DEGISMEZ(string kalem)
    {
        var k = Kur("vezne kalem tavan");
        var c = f.Ortam.Istemci(k);
        var y = await c.PostAsJsonAsync("/api/v1/vezne/fisler", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId, aciklama = "tavan",
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 1000m, iscilikTl = 0m } },
        });
        Assert.True(y.StatusCode == HttpStatusCode.OK, await y.Content.ReadAsStringAsync());
        var id = (await y.Json()).GetProperty("id").GetString()!;
        var once = VezneDurum(id);
        var json = "{\"kalemler\":[" + kalem.Insert(1, $"\"urunId\":\"{k.UrunId}\",") + "]}";
        await Alan400(await Ham(c, $"/api/v1/vezne/fisler/{id}", json), "kalemler");
        Assert.Equal(once, VezneDurum(id));
    }

    [Theory]
    [InlineData("10000000000000")]
    [InlineData("1e30")]
    public async Task PATCH_vezne_tahsilat_tutar_kolon_tavani_400_alan_tutarTl_DEGISMEZ(string deger)
    {
        var k = Kur("vezne tutar tavan");
        var c = f.Ortam.Istemci(k);
        var y = await c.PostAsJsonAsync("/api/v1/vezne/fisler", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId, aciklama = "tavan", fisTuru = "TAHSILAT", tutarTl = 500m,
        });
        Assert.True(y.StatusCode == HttpStatusCode.OK, await y.Content.ReadAsStringAsync());
        var id = (await y.Json()).GetProperty("id").GetString()!;
        var once = VezneDurum(id);
        await Alan400(await Ham(c, $"/api/v1/vezne/fisler/{id}", $"{{\"tutarTl\":{deger}}}"), "tutarTl");
        Assert.Equal(once, VezneDurum(id));
    }

    [Fact]
    public async Task Vezne_fis_toplami_kolonu_tasarsa_PATCH_ve_POST_400_alan_kalemler()
    {
        var k = Kur("vezne fiş toplam");
        var c = f.Ortam.Istemci(k);
        var iki = new[]
        {
            new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 6_000_000_000_000m, iscilikTl = 0m },
            new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 6_000_000_000_000m, iscilikTl = 0m },
        };
        // POST: her satır sığar, toplam 12 trilyon vezneleme damgasında decimal(15,2)'yi taşırdı
        int Say() { using var db = f.Vt.OlusturSistem(); return db.VezneFisleri.AsNoTracking().Count(x => x.TenantId == k.TenantId); }
        await Alan400(await c.PostAsJsonAsync("/api/v1/vezne/fisler", new
        { cariId = k.CariId, kurSnapshotId = k.KurId, aciklama = "tavan", kalemler = iki }), "kalemler");
        Assert.Equal(0, Say());
        var y = await c.PostAsJsonAsync("/api/v1/vezne/fisler", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId, aciklama = "tavan",
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 1000m, iscilikTl = 0m } },
        });
        Assert.True(y.StatusCode == HttpStatusCode.OK, await y.Content.ReadAsStringAsync());
        var id = (await y.Json()).GetProperty("id").GetString()!;
        var once = VezneDurum(id);
        await Alan400(await c.PatchAsJsonAsync($"/api/v1/vezne/fisler/{id}", new { kalemler = iki }), "kalemler");
        Assert.Equal(once, VezneDurum(id));
    }

    // ═══════════ SATIN ALMA ═══════════

    [Theory]
    [InlineData("{\"tanim\":\"Hurda\",\"adet\":1000000000}")]
    [InlineData("{\"tanim\":\"Hurda\",\"adet\":1,\"birimFiyatTl\":10000000000000}")]
    [InlineData("{\"tanim\":\"Hurda\",\"adet\":1e30}")]
    public async Task PATCH_satinalma_kalem_kolon_tavani_400_alan_kalemler_DEGISMEZ(string kalem)
    {
        var k = Kur("satınalma tavan");
        var c = f.Ortam.Istemci(k);
        var y = await c.PostAsJsonAsync("/api/v1/satinalma", new
        {
            tedarikciCariId = k.CariId, subeId = k.SubeId, aciklama = "tavan",
            kalemler = new[] { new { tanim = "Hurda altın", adet = 2m, birimFiyatTl = 100m } },
        });
        Assert.True(y.StatusCode == HttpStatusCode.OK, await y.Content.ReadAsStringAsync());
        var id = (await y.Json()).GetProperty("id").GetString()!;
        string Durum()
        {
            using var db = f.Vt.OlusturSistem();
            return string.Join(";", db.SatinalmaKalemleri.AsNoTracking().Where(x => x.SatinalmaSiparisId == id)
                .OrderBy(x => x.Id).ToList().Select(x => $"{x.Id}/{x.Adet}/{x.BirimFiyatTl}"));
        }
        var once = Durum();
        await Alan400(await Ham(c, $"/api/v1/satinalma/{id}", "{\"kalemler\":[" + kalem + "]}"), "kalemler");
        Assert.Equal(once, Durum());
    }
}
