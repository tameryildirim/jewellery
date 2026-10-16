using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET L-1b — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİTTİR ya da tevsik haddi
// sunucudan (/tevsik/gunluk) okunur (eşik testte gömülü değil). L-1b çok kalemli
// ALIŞ'ı TEK POST /islemler/alis-fisi'ye taşıdı: kapılar fişin BRÜT toplamıyla,
// kalem tutarı kalem başına AwayFromZero 2 haneye yuvarlanır.
// Sınır: sıfır · eksi · 0,005 kuruş · has 4 hane · 999.999.999,99 · boş/null ·
// tr-TR giriş (SayiCoz) + bölme saldırıları (kısmi nakit, günlük kümülatif, aynı ürün iki kez).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class L1bSinirTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETL1b-TEST-DOG-" + ad);   // K22, 10 g
    }

    private string IkinciUrun(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Urunler.Add(new Urun
        {
            Id = id, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"D2-{id[..8]}", Ad = "Dogrulayici urun",
            AyarId = "K22", Milyem = Kuyumcu.Core.Domain.Ayarlar.Bul("K22")!.Milyem, GramAgirlik = 10m, TasAgirlik = 0m,
            HasGrami = 9.16m, IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    private static object Fis(Kiraci k, string? cariId, string odeme, (string U, decimal Adet, decimal Birim)[] kalemler,
                              decimal? odenenTl = null, bool kasaYok = false) => new
    {
        cariId,
        kalemler = kalemler.Select(x => new { urunId = x.U, adet = x.Adet, birimFiyatTl = x.Birim }),
        odemeTuru = odeme,
        kasaId = kasaYok ? null : k.KasaId,
        odenenTl,
        kurSnapshotId = k.KurId,
    };

    private Task<HttpResponseMessage> FisGonder(Kiraci k, object govde) =>
        f.Ortam.Istemci(k, "MUHASEBE").PostAsJsonAsync("/api/v1/islemler/alis-fisi", govde);

    private Task<HttpResponseMessage> TekGonder(Kiraci k, object govde) =>
        f.Ortam.Istemci(k, "MUHASEBE").PostAsJsonAsync("/api/v1/islemler/alis", govde);

    private List<Hareket> Alislar(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId && h.Tur == "ALIS").ToList();
    }

    private decimal Kasa(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId).Bakiye;
    }

    private decimal Stok(Kiraci k, string urunId)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Stoklar.AsNoTracking().Where(s => s.TenantId == k.TenantId && s.UrunId == urunId).Sum(s => s.Adet);
    }

    private async Task<decimal> Had(Kiraci k)
    {
        var c = await f.Ortam.Istemci(k, "MUHASEBE").GetAsync("/api/v1/tevsik/gunluk");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var had = (await c.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("had").GetDecimal();
        Assert.True(had > 0m);
        return had;
    }

    private async Task<string> PesinId(Kiraci k)
    {
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/cari/sistem-carisi");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        return (await c.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    // ── SIFIR ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task D1_sifir_birim_fiyat_iki_kalem_kasa_DEGISMEZ_stok_artar_tutar_0()
    {
        var k = YeniKiraci("sifir");
        var u2 = IkinciUrun(k);
        var kasaOnce = Kasa(k);
        var c = await FisGonder(k, Fis(k, k.CariId, "HAVALE", [(k.UrunId, 1m, 0m), (u2, 1m, 0m)]));
        Assert.True(c.IsSuccessStatusCode, $"{(int)c.StatusCode}: {await c.Content.ReadAsStringAsync()}");
        Assert.Equal(0m, (await c.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("toplamTl").GetDecimal());
        Assert.Equal(kasaOnce, Kasa(k));
        var s = Alislar(k);
        Assert.Equal(2, s.Count);
        Assert.All(s, h => Assert.Equal(0m, h.TlTutari));
        Assert.Equal(1m, Stok(k, u2));
    }

    // ── EKSİ ───────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData(1, -100)]    // eksi birim fiyat
    [InlineData(-1, 100)]    // eksi adet (kalem başına iade taklidi)
    [InlineData(0, 100)]     // sıfır adet
    public async Task D2_eksi_ya_da_sifir_adet_eksi_fiyat_400_yazim_yok(int adet, int birim)
    {
        var k = YeniKiraci("eksi");
        var kasaOnce = Kasa(k);
        var c = await FisGonder(k, Fis(k, k.CariId, "HAVALE", [(k.UrunId, 1m, 500m), (k.UrunId, adet, birim)]));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Empty(Alislar(k));
        Assert.Equal(kasaOnce, Kasa(k));
    }

    // ── KURUŞ 0,005 (AwayFromZero, kalem başına) ───────────────────────────────
    [Fact]
    public async Task D3a_kurus_0_005_kalem_basina_yukari_yuvarlanir_kasa_0_02_duser()
    {
        var k = YeniKiraci("kurus");
        var u2 = IkinciUrun(k);
        var kasaOnce = Kasa(k);
        var c = await FisGonder(k, Fis(k, k.CariId, "HAVALE", [(k.UrunId, 1m, 0.005m), (u2, 1m, 0.005m)]));
        Assert.True(c.IsSuccessStatusCode, $"{(int)c.StatusCode}: {await c.Content.ReadAsStringAsync()}");
        Assert.Equal(0.02m, (await c.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("toplamTl").GetDecimal());
        Assert.Equal(kasaOnce - 0.02m, Kasa(k));
        Assert.Equal(-0.02m, Alislar(k).Sum(h => h.TlTutari));
    }

    [Fact]
    public async Task D3b_Pesin_NAKIT_kalemler_0_005_ile_had_ustune_yuvarlanir_400_bolme_kurusla_kacmaz()
    {
        var k = YeniKiraci("kurus-had");
        var u2 = IkinciUrun(k);
        var pesin = await PesinId(k);
        var had = await Had(k);
        // Her kalem had/2 + 0,005 → kalem başına 0,01 yukarı → toplam had + 0,02 > had → RED.
        var yarim = had / 2m + 0.005m;
        var c = await FisGonder(k, Fis(k, pesin, "NAKIT", [(k.UrunId, 1m, yarim), (u2, 1m, yarim)]));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Contains("tevsik", await c.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Alislar(k));
        // Her kalem had/2 + 0,004 → aşağı → toplam = had → KABUL (sınır dahil değil; tek kalemli alışla aynı).
        var yarim2 = had / 2m + 0.004m;
        var c2 = await FisGonder(k, Fis(k, pesin, "NAKIT", [(k.UrunId, 1m, yarim2), (u2, 1m, yarim2)]));
        Assert.True(c2.IsSuccessStatusCode, $"{(int)c2.StatusCode}: {await c2.Content.ReadAsStringAsync()}");
        Assert.Equal(had, (await c2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("toplamTl").GetDecimal());
    }

    // ── HAS 4 HANE (0,0001) ─────────────────────────────────────────────────────
    [Fact]
    public async Task D4_has_ince_adet_0_001_has_gram_6_haneye_dogru_adet_0_0001_red()
    {
        var k = YeniKiraci("has");
        var u2 = IkinciUrun(k);   // HasGrami 9,16 (sabit)
        using (var g = f.Vt.OlusturSistem()) TestKurulum.UrunuGramBirimiYap(g, u2);   // PAKET GOC-C (K14): kesirli miktar yalnız GRAM birimde — sınır ölçüsü GRAM birimli üründe korunur
        var c = await FisGonder(k, Fis(k, k.CariId, "HAVALE", [(u2, 0.001m, 100m), (u2, 0.001m, 100m)]));
        Assert.True(c.IsSuccessStatusCode, $"{(int)c.StatusCode}: {await c.Content.ReadAsStringAsync()}");
        var s = Alislar(k);
        Assert.Equal(2, s.Count);
        Assert.All(s, h => Assert.Equal(0.00916m, h.HasGrami));   // 0,001 × 9,16
        Assert.Equal(0.002m, Stok(k, u2));                         // aynı ürün iki kalem → tek stok satırı, toplanır

        var red = await FisGonder(k, Fis(k, k.CariId, "HAVALE", [(u2, 0.0001m, 100m), (u2, 1m, 100m)]));
        Assert.Equal(HttpStatusCode.BadRequest, red.StatusCode);
        Assert.Equal(2, Alislar(k).Count);
    }

    // ── ÇOK BÜYÜK TUTAR ─────────────────────────────────────────────────────────
    [Fact]
    public async Task D5_cok_buyuk_999_999_999_99_fis_toplami_MASAK_kapisina_takilir_yazim_yok_tek_kalemliyle_ayni()
    {
        var k = YeniKiraci("buyuk");
        var u2 = IkinciUrun(k);
        var c = await FisGonder(k, Fis(k, k.CariId, "HAVALE", [(k.UrunId, 1m, 999_999_999.98m), (u2, 1m, 0.01m)], kasaYok: true));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Empty(Alislar(k));
        var tek = await TekGonder(k, new { cariId = k.CariId, urunId = k.UrunId, adet = 1m, birimFiyatTl = 999_999_999.99m,
                                           odemeTuru = "HAVALE", kasaId = (string?)null, kurSnapshotId = k.KurId });
        Assert.Equal(tek.StatusCode, c.StatusCode);
        Assert.Empty(Alislar(k));
    }

    // ── BOŞ / NULL ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task D6_null_kalemler_bos_urunId_null_kur_400_yazim_yok()
    {
        var k = YeniKiraci("bos");
        var c1 = await FisGonder(k, new { cariId = k.CariId, kalemler = (object?)null, odemeTuru = "HAVALE", kasaId = k.KasaId, kurSnapshotId = k.KurId });
        Assert.Equal(HttpStatusCode.BadRequest, c1.StatusCode);
        Assert.Contains("kalem", await c1.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        var c2 = await FisGonder(k, Fis(k, k.CariId, "HAVALE", [(k.UrunId, 1m, 10m), ("", 1m, 10m)]));
        Assert.Equal(HttpStatusCode.BadRequest, c2.StatusCode);
        var c3 = await FisGonder(k, new { cariId = k.CariId, kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 10m } },
                                          odemeTuru = "HAVALE", kasaId = k.KasaId, kurSnapshotId = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, c3.StatusCode);
        Assert.Empty(Alislar(k));
    }

    // ── BÖLME SALDIRILARI ────────────────────────────────────────────────────────
    [Fact]
    public async Task D7_gercek_cari_fis_toplami_had_ustu_KISMI_nakit_1000_de_400_islem_tutari_kurali()
    {
        var k = YeniKiraci("kismi");
        var u2 = IkinciUrun(k);
        var had = await Had(k);
        var a = Math.Round(had * 0.6m, 2);
        var c = await FisGonder(k, Fis(k, k.CariId, "NAKIT", [(k.UrunId, 1m, a), (u2, 1m, a)], odenenTl: 1_000m));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Empty(Alislar(k));
    }

    [Fact]
    public async Task D8_gercek_cari_ayni_gun_iki_fis_kumulatif_nakit_had_ustu_ikinci_fis_400()
    {
        var k = YeniKiraci("kumulatif");
        var u2 = IkinciUrun(k);
        var had = await Had(k);
        var ceyrek = Math.Round(had * 0.3m, 2);   // fiş 1 = %60 had
        var c1 = await FisGonder(k, Fis(k, k.CariId, "NAKIT", [(k.UrunId, 1m, ceyrek), (u2, 1m, ceyrek)]));
        Assert.True(c1.IsSuccessStatusCode, $"{(int)c1.StatusCode}: {await c1.Content.ReadAsStringAsync()}");
        Assert.Equal(2 * ceyrek, Alislar(k).Sum(h => h.NakitTutar ?? 0m));   // kümülatifin okuduğu pay yazıldı
        var c2 = await FisGonder(k, Fis(k, k.CariId, "NAKIT", [(k.UrunId, 1m, ceyrek), (u2, 1m, ceyrek)]));
        Assert.Equal(HttpStatusCode.BadRequest, c2.StatusCode);   // %120 > had
        Assert.Equal(2, Alislar(k).Count);
    }

    [Fact]
    public async Task D9_odenen_fis_toplamindan_buyuk_400_yazim_yok()
    {
        var k = YeniKiraci("fazla-odeme");
        var u2 = IkinciUrun(k);
        var c = await FisGonder(k, Fis(k, k.CariId, "HAVALE", [(k.UrunId, 1m, 100m), (u2, 1m, 100m)], odenenTl: 200.01m));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Empty(Alislar(k));
    }

    // ── TÜRKÇE GİRİŞ (FormAlani.SayiCoz) ─────────────────────────────────────────
    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("0,0001", "0.0001")]
    [InlineData("999.999.999,99", "999999999.99")]
    public void D10_SayiCoz_turkce_giris(string girdi, string beklenen)
    {
        var c = FormAlani.SayiCoz(girdi);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }

    [Theory]
    [InlineData("1.500", "Belirsiz")]
    [InlineData("", "Bos")]
    [InlineData(null, "Bos")]
    public void D11_SayiCoz_belirsiz_ve_bos_red(string? girdi, string sonuc)
    {
        var c = FormAlani.SayiCoz(girdi);
        Assert.Equal(sonuc, c.Sonuc.ToString());
        Assert.Null(c.Deger);
    }
}
