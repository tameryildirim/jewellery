using System.Net;
using System.Text;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ════════════════════════════════════════════════════════════════════════
// PAKET PATCH-4 — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT sayılardır (üretim
// kodundan hesaplanmaz). Kapsam: ürün PATCH'inde gerçek milyem → has gram →
// stok has toplamı zinciri (sıfır, eksi, üst sınır, 4/6 hane, aynı değer,
// ayar değişimi + değer, dize, çok büyük) ve satış fişi istemci tutar tavanı
// (FisKalemGirisi.TavanAsildi) + Türkçe sayı girişi (FormAlani.SayiCoz).
// Proje 6 hane kuralı: OzelMatrahMotoru.Yuvarla6 = MidpointRounding.AwayFromZero.
// ════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class PATCH4SinirHttpTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad, string ayarId = "K22", decimal gram = 10m)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"PAKETPATCH-4-TEST {ad}", urunGram: gram, ayarId: ayarId);
    }

    private void Milyem920(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var u = db.Urunler.Single(x => x.Id == k.UrunId);
        u.Milyem = 920m; u.HasGrami = 9.2m;
        var s = db.Stoklar.Single(x => x.UrunId == k.UrunId);
        s.ToplamHasGram = 777.777777m;   // tarihsel birikmiş değer — has değişmezse dokunulmamalı
        db.SaveChanges();
    }

    private (decimal Milyem, decimal Has, decimal StokHas, string AyarId) Durum(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var u = db.Urunler.AsNoTracking().Single(x => x.Id == k.UrunId);
        var s = db.Stoklar.AsNoTracking().Single(x => x.UrunId == k.UrunId);
        return (u.Milyem, u.HasGrami, s.ToplamHasGram, u.AyarId);
    }

    private async Task<HttpResponseMessage> Patch(Kiraci k, string json) =>
        await f.Ortam.Istemci(k).SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/urunler/{k.UrunId}")
        { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    private static async Task Red400(HttpResponseMessage r, string? alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        if (alan is null) return;
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), "400 gövdesinde 'alanlar' yok: " + govde);
        var adlar = alanlar.EnumerateObject().Select(p => p.Name).ToList();
        Assert.True(adlar.Any(a => a.Contains(alan, StringComparison.OrdinalIgnoreCase)),
            $"400 '{alan}' alanına işaretli değil: {string.Join(",", adlar)}");
    }

    // ── sıfır ────────────────────────────────────────────────────────────
    [Fact]
    public async Task D_P4_gercek_milyem_SIFIR_400_alanli_veri_DEGISMEZ()
    {
        var k = Kur("sifir");
        Milyem920(k);
        await Red400(await Patch(k, """{"gercekMilyem":0}"""), "gercekMilyem");
        Assert.Equal((920m, 9.2m, 777.777777m, "K22"), Durum(k));
    }

    // ── eksi ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task D_P4_gercek_milyem_EKSI_400_alanli_veri_DEGISMEZ()
    {
        var k = Kur("eksi");
        Milyem920(k);
        await Red400(await Patch(k, """{"gercekMilyem":-920}"""), "gercekMilyem");
        Assert.Equal((920m, 9.2m, 777.777777m, "K22"), Durum(k));
    }

    // ── üst sınır: 1000 kabul, 1000,01 ret ───────────────────────────────
    [Fact]
    public async Task D_P4_gercek_milyem_1000_KABUL_1000_01_RET()
    {
        var k = Kur("ust sinir");
        Milyem920(k);
        await Red400(await Patch(k, """{"gercekMilyem":1000.01}"""), "gercekMilyem");
        Assert.Equal((920m, 9.2m, 777.777777m, "K22"), Durum(k));

        var r = await Patch(k, """{"gercekMilyem":1000}""");
        Assert.True(r.StatusCode == HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        // 10 g × 1000/1000 = 10 has; 100 adet × 10 = 1000
        Assert.Equal((1000m, 10m, 1000m, "K22"), Durum(k));
    }

    // ── çok büyük + dize (Strict) ────────────────────────────────────────
    [Fact]
    public async Task D_P4_gercek_milyem_COK_BUYUK_ve_DIZE_400_veri_DEGISMEZ()
    {
        var k = Kur("buyuk");
        Milyem920(k);
        await Red400(await Patch(k, """{"gercekMilyem":999999999.99}"""), "gercekMilyem");
        await Red400(await Patch(k, """{"gercekMilyem":"920"}"""), null);
        Assert.Equal((920m, 9.2m, 777.777777m, "K22"), Durum(k));
    }

    // ── aynı değer açıkça gönderilir → has ve stok has DOKUNULMAZ ─────────
    [Fact]
    public async Task D_P4_ayni_gercek_milyem_ACIKCA_gelirse_stok_has_DOKUNULMAZ()
    {
        var k = Kur("ayni deger");
        Milyem920(k);
        var r = await Patch(k, """{"ayarId":"K22","gercekMilyem":920,"gramAgirlik":10,"tasAgirlik":0,"birim":"ADET","barkod":"D-B"}""");
        Assert.True(r.StatusCode == HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        Assert.Equal((920m, 9.2m, 777.777777m, "K22"), Durum(k));
        var y = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(920m, y.GetProperty("gercekMilyem").GetDecimal());
    }

    // ── has/gram 4 hane + 6 hane (yuvarlama sınırı değil) ─────────────────
    [Fact]
    public async Task D_P4_gram_4_hane_ondalik_milyem_has_6_hane()
    {
        var k = Kur("4 hane");
        var r = await Patch(k, """{"gramAgirlik":10.0001,"gercekMilyem":916.5}""");
        Assert.True(r.StatusCode == HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        // 10,0001 × 916,5 / 1000 = 9,16509165 → 6 hane 9,165092 ; stok 100 × 9,165092 = 916,5092
        Assert.Equal((916.5m, 9.165092m, 916.5092m, "K22"), Durum(k));
        var y = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(916.5m, y.GetProperty("gercekMilyem").GetDecimal());
    }

    // ── 6. hane YARIM sınırı (0,0000005) — proje kuralı AwayFromZero ──────
    // 1,0003 g × 995 / 1000 = 0,9952985 → AwayFromZero 0,995299 (ToEven 0,995298 verir).
    [Fact]
    public async Task D_P4_has_6_hane_YARIM_sinirinda_AwayFromZero()
    {
        var k = Kur("yarim", ayarId: "K24", gram: 1.0003m);
        var r = await Patch(k, """{"gercekMilyem":995}""");
        Assert.True(r.StatusCode == HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        var d = Durum(k);
        Assert.Equal(995m, d.Milyem);
        Assert.Equal(0.995299m, d.Has);
    }

    // ── ayar GERÇEKTEN değişir + değer verilir → değer yeni ayara yazılır ──
    [Fact]
    public async Task D_P4_ayar_degisir_ve_deger_verilirse_DEGER_yazilir()
    {
        var k = Kur("ayar ve deger");
        Milyem920(k);
        var r = await Patch(k, """{"ayarId":"K18","gercekMilyem":755}""");
        Assert.True(r.StatusCode == HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        // 10 g × 755 / 1000 = 7,55 ; stok 100 × 7,55 = 755
        Assert.Equal((755m, 7.55m, 755m, "K18"), Durum(k));
    }

    // ── boş/null: ayar aynı + null → standart 916 ; anahtar yok → korur ───
    [Fact]
    public async Task D_P4_null_STANDART_anahtar_yok_KORUR()
    {
        var k = Kur("null");
        Milyem920(k);
        var r1 = await Patch(k, """{"barkod":"YALNIZ-BARKOD"}""");
        Assert.True(r1.StatusCode == HttpStatusCode.OK, await r1.Content.ReadAsStringAsync());
        Assert.Equal((920m, 9.2m, 777.777777m, "K22"), Durum(k));

        var r2 = await Patch(k, """{"gercekMilyem":null}""");
        Assert.True(r2.StatusCode == HttpStatusCode.OK, await r2.Content.ReadAsStringAsync());
        Assert.Equal((916m, 9.16m, 916m, "K22"), Durum(k));
    }

    // ── RangeAttribute(int,int) ondalığı TAMSAYIYA yuvarlayarak sınar: 1000,4 ve 0,6 da geçer ──
    [Theory]
    [InlineData("1000.4")]
    [InlineData("1000.01")]
    [InlineData("0.6")]
    public async Task D_P4_gercek_milyem_ARALIK_DISI_ondalik_PATCH_400(string deger)
    {
        var k = Kur("aralik " + deger);
        Milyem920(k);
        await Red400(await Patch(k, "{\"gercekMilyem\":" + deger + "}"), "gercekMilyem");
        Assert.Equal((920m, 9.2m, 777.777777m, "K22"), Durum(k));
    }

    [Theory]
    [InlineData("1000.4")]
    [InlineData("0.6")]
    public async Task D_P4_gercek_milyem_ARALIK_DISI_ondalik_POST_400(string deger)
    {
        var k = Kur("post " + deger);
        var r = await f.Ortam.Istemci(k).SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/v1/urunler")
        { Content = new StringContent("{\"tip\":\"ZIYNET\",\"kod\":\"DP4-P\",\"ad\":\"Dp4\",\"ayarId\":\"K22\",\"gramAgirlik\":10,\"birim\":\"ADET\",\"tasKokeni\":\"TAS_YOK\",\"gercekMilyem\":" + deger + "}", Encoding.UTF8, "application/json") });
        await Red400(r, "gercekMilyem");
    }
}

public sealed class PATCH4SinirSaltTestleri
{
    // ── satış fişi istemci tavanı: sınırda eşit = geçer, bir kuruş üstü = aşar ──
    [Fact]
    public void D_P4_tavan_SINIR_esit_gecer_kurus_ustu_asar()
    {
        Assert.False(FisKalemGirisi.TavanAsildi(1m, 9_999_999_999_999.99m));
        Assert.True(FisKalemGirisi.TavanAsildi(1m, 10_000_000_000_000.00m));
        Assert.True(FisKalemGirisi.TavanAsildi(1m, 9_999_999_999_999.991m));
    }

    [Fact]
    public void D_P4_tavan_SIFIR_EKSI_BOS_cok_buyuk()
    {
        Assert.False(FisKalemGirisi.TavanAsildi(1m, 0m));
        Assert.False(FisKalemGirisi.TavanAsildi(1m, null));
        Assert.False(FisKalemGirisi.TavanAsildi(1m, 999_999_999.99m));          // 999.999.999,99 tavanın altında
        Assert.True(FisKalemGirisi.TavanAsildi(2m, 5_000_000_000_000m));         // satır 10^13 > tavan
        Assert.True(FisKalemGirisi.TavanAsildi(-2m, 5_000_000_000_000m));        // eksi adet: mutlak değer
        Assert.True(FisKalemGirisi.TavanAsildi(1e20m, 1e10m));                   // decimal taşması = aşıyor
    }

    // ── Türkçe giriş ─────────────────────────────────────────────────────
    [Fact]
    public void D_P4_turkce_giris_45000_1_5_belirsiz_1500()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(920.5m, FormAlani.SayiCoz("920,5").Deger);
        var b = FormAlani.SayiCoz("1.500");
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, b.Sonuc);
        Assert.Null(b.Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("  ").Sonuc);
        Assert.Equal(0.0001m, FormAlani.SayiCoz("0,0001").Deger);
    }
}

// ════════════════════════════════════════════════════════════════════════
// PATCH-4 TUR 2 — HASIM DOĞRULAYICI EK SINIR TESTLERİ (03.10, Aşkın + Mehmet)
// Beklenen değerler el hesabı SABİT sayılardır; üretim kodundan türetilmez.
// ════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class PATCH4T2SinirHttpTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"PAKETPATCH-4-TEST t2 {ad}", urunGram: 10m, ayarId: "K22");
    }

    private void Hazirla(Kiraci k, decimal milyem, decimal has, decimal? tas)
    {
        using var db = f.Vt.OlusturSistem();
        var u = db.Urunler.Single(x => x.Id == k.UrunId);
        u.Milyem = milyem; u.HasGrami = has; u.TasAgirlik = tas;
        db.Stoklar.Single(x => x.UrunId == k.UrunId).ToplamHasGram = 555.555555m;   // nöbetçi
        db.SaveChanges();
    }

    private (decimal Milyem, decimal Has, decimal StokHas, decimal Gram) Durum(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var u = db.Urunler.AsNoTracking().Single(x => x.Id == k.UrunId);
        var s = db.Stoklar.AsNoTracking().Single(x => x.UrunId == k.UrunId);
        return (u.Milyem, u.HasGrami, s.ToplamHasGram, u.GramAgirlik);
    }

    private Task<HttpResponseMessage> Patch(Kiraci k, string json) =>
        f.Ortam.Istemci(k).SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/urunler/{k.UrunId}")
        { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    // Taş kolonu NULL iken ekran tasAgirlik:0 gönderir → "değişmedi" sayılmalı; 920 korunur, stok has'a dokunulmaz.
    [Fact]
    public async Task D2_P4_tas_NULL_iken_ekran_0_gonderir_HAS_YENIDEN_HESAPLANMAZ()
    {
        var k = Kur("tas null");
        Hazirla(k, 920m, 9.2m, null);
        var r = await Patch(k, """{"ayarId":"K22","birim":"ADET","gramAgirlik":10.000,"tasAgirlik":0,"barkod":"T2-B"}""");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal((920m, 9.2m, 555.555555m), (Durum(k).Milyem, Durum(k).Has, Durum(k).StokHas));
    }

    // 6. hane YARIM sınırı: 0,0001 g × 5‰ = 0,0000005 → AwayFromZero 0,000001 (ToEven 0 verirdi); stok 100 × = 0,0001.
    [Fact]
    public async Task D2_P4_has_7_hane_tam_YARIM_AwayFromZero_ve_stok_has()
    {
        var k = Kur("yarim t2");
        var r = await Patch(k, """{"gramAgirlik":0.0001,"gercekMilyem":5}""");
        Assert.True(r.StatusCode == HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        var d = Durum(k);
        Assert.Equal(5m, d.Milyem);
        Assert.Equal(0.000001m, d.Has);
        Assert.Equal(0.0001m, d.StokHas);
    }

    // 4 hane gram + 2 hane milyem: 10,0001 × 916,5 / 1000 = 9,16509165 → 9,165092; stok 100 × = 916,5092.
    [Fact]
    public async Task D2_P4_gram_4_hane_milyem_2_hane_has_6_hane()
    {
        var k = Kur("4 hane t2");
        var r = await Patch(k, """{"gramAgirlik":10.0001,"gercekMilyem":916.5}""");
        Assert.True(r.StatusCode == HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        var d = Durum(k);
        Assert.Equal((916.5m, 9.165092m, 916.5092m), (d.Milyem, d.Has, d.StokHas));
        var y = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(916.5m, y.GetProperty("gercekMilyem").GetDecimal());
    }

    // Çok büyük gram: stok has decimal(15,6) taşar → 500 DEĞİL 400, veri değişmez.
    [Fact]
    public async Task D2_P4_cok_buyuk_gram_stok_has_tasarsa_400_veri_DEGISMEZ()
    {
        var k = Kur("buyuk t2");
        Hazirla(k, 920m, 9.2m, 0m);
        var r = await Patch(k, """{"gramAgirlik":999999999.999}""");
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        var d = Durum(k);
        Assert.Equal((920m, 9.2m, 555.555555m, 10m), (d.Milyem, d.Has, d.StokHas, d.Gram));
    }

    // GET: standart milyemli ürün null; 920 yazılınca 920; null gönderilince yine null (standart).
    [Fact]
    public async Task D2_P4_GET_gercekMilyem_null_920_null_dongusu()
    {
        var k = Kur("get dongu");
        var c = f.Ortam.Istemci(k);
        async Task<JsonElement> Get() => JsonDocument.Parse(await (await c.GetAsync($"/api/v1/urunler/{k.UrunId}")).Content.ReadAsStringAsync()).RootElement.GetProperty("gercekMilyem");
        Assert.Equal(JsonValueKind.Null, (await Get()).ValueKind);
        Assert.Equal(HttpStatusCode.OK, (await Patch(k, """{"gercekMilyem":920}""")).StatusCode);
        Assert.Equal(920m, (await Get()).GetDecimal());
        Assert.Equal(HttpStatusCode.OK, (await Patch(k, """{"gercekMilyem":null}""")).StatusCode);
        Assert.Equal(JsonValueKind.Null, (await Get()).ValueKind);
        Assert.Equal(916m, Durum(k).Milyem);
    }
}

public sealed class PATCH4T2SinirSaltTestleri
{
    private static bool Gecerli(decimal tutar)
    {
        var dto = new Kuyumcu.Api.Modules.Satis.OdemeBacagiDto { Arac = "NAKIT", TutarTl = tutar };
        var sonuc = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        return System.ComponentModel.DataAnnotations.Validator.TryValidateProperty(
            tutar, new System.ComponentModel.DataAnnotations.ValidationContext(dto) { MemberName = "TutarTl" }, sonuc);
    }

    // Ödeme bacağı tutarı: 0,01 ve 999.999.999,99 ve tavan 9.999.999.999.999,99 geçer; 0, eksi, tavan+1 kuruş reddedilir.
    [Fact]
    public void D2_P4_odeme_bacagi_TutarTl_sinirlari()
    {
        Assert.True(Gecerli(0.01m));
        Assert.True(Gecerli(999_999_999.99m));
        Assert.True(Gecerli(9_999_999_999_999.99m));
        Assert.False(Gecerli(0m));
        Assert.False(Gecerli(-0.01m));
        Assert.False(Gecerli(10_000_000_000_000.00m));
    }

    // İstemci tavanı ile sunucu kolon tavanı birebir (derleyici dışı kilit, sabit sayı).
    [Fact]
    public void D2_P4_istemci_tavani_SABIT_9999999999999_99()
    {
        Assert.Equal(9_999_999_999_999.99m, Kuyumcu.Core.I18n.PatchDortSozlugu.TutarTavani);
        Assert.Equal(9_999_999_999_999.99d, Kuyumcu.Api.Modules.KolonTavani.UstTutar);
    }

    // Servis içi gerçek milyem kapısı: sınır tablosu (sabit).
    [Theory]
    [InlineData("1", true)]
    [InlineData("1000", true)]
    [InlineData("920.25", true)]
    [InlineData("0.99", false)]
    [InlineData("1000.01", false)]
    [InlineData("920.005", false)]
    [InlineData("-1", false)]
    [InlineData("0", false)]
    public void D2_P4_GercekMilyemKapisi_sinir(string deger, bool gecer)
    {
        var m = decimal.Parse(deger, System.Globalization.CultureInfo.InvariantCulture);
        var ex = Record.Exception(() => Kuyumcu.Api.Modules.Urun.UrunServisi.GercekMilyemKapisi(m));
        Assert.Equal(gecer, ex is null);
        Kuyumcu.Api.Modules.Urun.UrunServisi.GercekMilyemKapisi(null);   // null serbest
    }
}
