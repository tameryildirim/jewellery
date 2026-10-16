using System.Net;
using System.Text;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ════════════════════════════════════════════════════════════════════════
// PAKET PATCH-5 — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT sayılardır (üretim
// kodundan hesaplanmaz). Kapsam: Günü Kapat sayılan nakit (sıfır, eksi, yarım
// kuruş, tavan, dize), elle kasa hareketi (yarım kuruş, eksi), cari tahsilat has
// 4/6 hane, cari kredi limiti PATCH (null=sil, yuvarlama, eksi, tavan), virman
// ve gider tavanı (500 değil 400), Türkçe sayı girişi (FormAlani.SayiCoz).
// Proje 2/6 hane kuralı: MidpointRounding.AwayFromZero (KolonTavani.Yuvarla2/6).
// ════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class PATCH5SinirHttpTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"PAKETPATCH-5-TEST D {ad}");
    }

    private static Task<HttpResponseMessage> Ham(HttpClient c, HttpMethod m, string yol, string json) =>
        c.SendAsync(new HttpRequestMessage(m, yol) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    private static async Task Red400(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), "400 gövdesinde 'alanlar' yok: " + govde);
        var adlar = alanlar.EnumerateObject().Select(p => p.Name).ToList();
        Assert.True(adlar.Any(a => a.Contains(alan, StringComparison.OrdinalIgnoreCase)),
            $"400 '{alan}' alanına işaretli değil: {string.Join(",", adlar)} · {govde}");
    }

    private static async Task Ok(HttpResponseMessage r)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode} {govde}");
    }

    private (decimal Bakiye, int KasaSatir, int Kapanis) Kasa(Kiraci k, string? kasaId = null)
    {
        using var db = f.Vt.OlusturSistem();
        return (db.Kasalar.AsNoTracking().Single(x => x.Id == (kasaId ?? k.KasaId)).Bakiye,
                db.KasaHareketleri.AsNoTracking().Count(x => x.TenantId == k.TenantId),
                db.GunKapanislari.AsNoTracking().Count(x => x.TenantId == k.TenantId));
    }

    private GunKapanisi Kapanis(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.GunKapanislari.AsNoTracking().Single(x => x.TenantId == k.TenantId);
    }

    // ── Günü Kapat ─────────────────────────────────────────────────────────

    [Fact]
    public async Task D_P5_gun_kapat_SIFIR_sayim_gecerli_fark_eksi_250000_gerekceyle_200()
    {
        var k = Kur("gk sifir");
        await Ok(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            """{"sayilanNakit":0,"aciklama":"çekmece boş bulundu, sayım tekrarlandı"}"""));
        var g = Kapanis(k);
        Assert.Equal(0m, g.SayilanNakit);
        Assert.Equal(-250_000m, g.Fark);
    }

    [Fact]
    public async Task D_P5_gun_kapat_SIFIR_sayim_gerekcesiz_400_kapanis_YOK()
    {
        var k = Kur("gk sifir gerekcesiz");
        var r = await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            """{"sayilanNakit":0}""");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(0, Kasa(k).Kapanis);
    }

    [Theory]
    [InlineData("""{"sayilanNakit":-0.01,"aciklama":"eksi deneme"}""")]
    [InlineData("""{"sayilanNakit":-250000,"aciklama":"eksi deneme"}""")]
    [InlineData("""{"sayilanNakit":9999999999999.995,"aciklama":"tavan deneme"}""")]
    [InlineData("""{"sayilanNakit":"250000","aciklama":"dize deneme"}""")]
    [InlineData("""{"sayilanNakit":"250.000","aciklama":"turkce dize"}""")]
    [InlineData("""{"sayilanNakit":1e400,"aciklama":"asiri buyuk"}""")]
    public async Task D_P5_gun_kapat_eksi_tavan_dize_400_alan_sayilanNakit_kapanis_YOK(string json)
    {
        var k = Kur("gk red");
        var once = Kasa(k);
        await Red400(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi", json),
            "sayilanNakit");
        Assert.Equal(once, Kasa(k));
    }

    [Fact]
    public async Task D_P5_gun_kapat_yarim_kurus_0_005_AwayFromZero_250000_01_fark_var_gerekcesiz_400()
    {
        var k = Kur("gk yarim kurus");
        var r = await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            """{"sayilanNakit":250000.005}""");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);   // 250000,01 − 250000,00 = 0,01 fark → gerekçe ister
        Assert.Equal(0, Kasa(k).Kapanis);
    }

    // PAKET GOC-A (K9a): kuruş altı hane YUVARLANMAZ (eski: 250.000,004 → 250.000,00 fark 0 kapanır) —
    // 400 alan:sayilanNakit, kapanış YOK; tam kuruşla fark 0 gerekçesiz kapanır (asıl iddia korunur).
    [Fact]
    public async Task D_P5_gun_kapat_0_004_yuvarlanmaz_400_tam_sayim_fark_SIFIR_gerekcesiz_200()
    {
        var k = Kur("gk 0004");
        await Red400(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            """{"sayilanNakit":250000.004}"""), "sayilanNakit");
        Assert.Equal(0, Kasa(k).Kapanis);
        await Ok(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            """{"sayilanNakit":250000.00}"""));
        var g = Kapanis(k);
        Assert.Equal(250_000.00m, g.SayilanNakit);
        Assert.Equal(0m, g.Fark);
    }

    [Fact]
    public async Task D_P5_gun_kapat_TAM_tavan_9999999999999_99_200_kolonda_birebir()
    {
        var k = Kur("gk tavan");
        await Ok(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            """{"sayilanNakit":9999999999999.99,"aciklama":"tavan sayımı — kolon sınırı denemesi"}"""));
        var g = Kapanis(k);
        Assert.Equal(9_999_999_999_999.99m, g.SayilanNakit);
        Assert.Equal(9_999_999_749_999.99m, g.Fark);
    }

    // ── Elle kasa hareketi ─────────────────────────────────────────────────

    // PAKET GOC-A (K9): 0,015 YUVARLANMAZ (eski: AwayFromZero 0,02) — 400 alan:tutar, satır YOK; 0,02 aynen yazılır.
    [Fact]
    public async Task D_P5_kasa_giris_0_015_yuvarlanmaz_400_0_02_yazilir_bakiye_250000_02()
    {
        var k = Kur("kh 0005");
        await Red400(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/hareket",
            """{"yon":"GIRIS","tutar":0.015,"odemeTuru":"NAKIT"}"""), "tutar");
        await Ok(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/hareket",
            """{"yon":"GIRIS","tutar":0.02,"odemeTuru":"NAKIT"}"""));
        using var db = f.Vt.OlusturSistem();
        var h = db.KasaHareketleri.AsNoTracking().Single(x => x.TenantId == k.TenantId);
        Assert.Equal(0.02m, h.Tutar);
        Assert.Equal(250_000.02m, db.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId).Bakiye);
    }

    // PAKET GOC-A (K9): 0,015 YUVARLANMAZ — 400, satır YOK; 0,02 çıkışı işaret EKSİ yazılır.
    [Fact]
    public async Task D_P5_kasa_cikis_0_015_yuvarlanmaz_400_0_02_isaret_EKSI_bakiye_249999_98()
    {
        var k = Kur("kh cikis 0005");
        await Red400(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/hareket",
            """{"yon":"CIKIS","tutar":0.015,"odemeTuru":"NAKIT"}"""), "tutar");
        await Ok(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/hareket",
            """{"yon":"CIKIS","tutar":0.02,"odemeTuru":"NAKIT"}"""));
        using var db = f.Vt.OlusturSistem();
        var h = db.KasaHareketleri.AsNoTracking().Single(x => x.TenantId == k.TenantId);
        Assert.Equal(-0.02m, h.Tutar);
        Assert.Equal(249_999.98m, db.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId).Bakiye);
    }

    [Theory]
    [InlineData("""{"yon":"GIRIS","tutar":0,"odemeTuru":"NAKIT"}""")]
    [InlineData("""{"yon":"GIRIS","tutar":-5,"odemeTuru":"NAKIT"}""")]
    // SON-KALANLAR tur 2: 0,005 bu listeden ÇIKTI — DTO [Range(0.01)] kaldırıldı, HTTP yolu servis yoluyla aynı
    // TEK kuralı uygular (önce yuvarla → 0,01 kabul; SK12/DSK10). Sınırın hemen altı (0,0049) red listesinde.
    [InlineData("""{"yon":"GIRIS","tutar":0.0049,"odemeTuru":"NAKIT"}""")]
    [InlineData("""{"yon":"GIRIS","odemeTuru":"NAKIT"}""")]
    [InlineData("""{"yon":"GIRIS","tutar":"45.000","odemeTuru":"NAKIT"}""")]
    [InlineData("""{"yon":"GIRIS","tutar":999999999999999999999999999999,"odemeTuru":"NAKIT"}""")]
    public async Task D_P5_kasa_hareket_sifir_eksi_bos_dize_asiri_400_alan_tutar_satir_YOK(string json)
    {
        var k = Kur("kh red");
        var once = Kasa(k);
        await Red400(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/hareket", json), "tutar");
        Assert.Equal(once, Kasa(k));
    }

    // ── Cari tahsilat: has 4/6 hane ────────────────────────────────────────

    [Fact]
    public async Task D_P5_cari_tahsilat_has_0_0001_4_hane_birebir_yazilir()
    {
        var k = Kur("tahsilat has 4");
        await Ok(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/cari/{k.CariId}/tahsilat",
            $$"""{"hasGrami":0.0001,"kurSnapshotId":"{{k.KurId}}"}"""));
        using var db = f.Vt.OlusturSistem();
        var h = db.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId);
        Assert.Equal(0.0001m, Math.Abs(h.HasGrami));
    }

    [Fact]
    public async Task D_P5_cari_tahsilat_has_yarim_milyonda_0_0000005_AwayFromZero_0_000001()
    {
        var k = Kur("tahsilat has 6");
        await Ok(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/cari/{k.CariId}/tahsilat",
            $$"""{"hasGrami":1.0000005,"kurSnapshotId":"{{k.KurId}}"}"""));
        using var db = f.Vt.OlusturSistem();
        var h = db.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId);
        Assert.Equal(1.000001m, Math.Abs(h.HasGrami));
    }

    [Fact]
    public async Task D_P5_cari_tahsilat_eksi_TL_400_alan_tlTutari()
    {
        var k = Kur("tahsilat eksi");
        await Red400(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/cari/{k.CariId}/tahsilat",
            $$"""{"tlTutari":-10,"odemeTuru":"HAVALE","kasaId":"{{k.KasaId}}","kurSnapshotId":"{{k.KurId}}"}"""), "tlTutari");
    }

    // ── Cari kredi limiti PATCH ────────────────────────────────────────────

    private decimal? KrediLimiti(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Cariler.AsNoTracking().Single(x => x.Id == k.CariId).KrediLimitiTl;
    }

    [Fact]
    public async Task D_P5_cari_kredi_limiti_yarim_kurus_yuvarlanir_null_SILER_yoksa_KORUR()
    {
        var k = Kur("kredi limiti");
        var c = f.Ortam.Istemci(k);
        await Ok(await Ham(c, HttpMethod.Patch, $"/api/v1/cari/{k.CariId}", """{"krediLimitiTl":1000.005}"""));
        Assert.Equal(1000.01m, KrediLimiti(k));
        await Ok(await Ham(c, HttpMethod.Patch, $"/api/v1/cari/{k.CariId}", """{"notlar":"limit korunmalı"}"""));
        Assert.Equal(1000.01m, KrediLimiti(k));
        await Ok(await Ham(c, HttpMethod.Patch, $"/api/v1/cari/{k.CariId}", """{"krediLimitiTl":9999999999999.99}"""));
        Assert.Equal(9_999_999_999_999.99m, KrediLimiti(k));
        await Ok(await Ham(c, HttpMethod.Patch, $"/api/v1/cari/{k.CariId}", """{"krediLimitiTl":null}"""));
        Assert.Null(KrediLimiti(k));
    }

    [Theory]
    [InlineData("""{"krediLimitiTl":-0.01}""")]
    [InlineData("""{"krediLimitiTl":9999999999999.995}""")]
    [InlineData("""{"krediLimitiTl":"1.500"}""")]
    public async Task D_P5_cari_kredi_limiti_eksi_tavan_dize_400_alan_DEGISMEZ(string json)
    {
        var k = Kur("kredi limiti red");
        await Red400(await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/cari/{k.CariId}", json), "krediLimitiTl");
        Assert.Null(KrediLimiti(k));
    }

    // ── Virman / Gider: çok büyük tutar 500 değil 400 ──────────────────────

    [Fact]
    public async Task D_P5_virman_999999999999_99_ustu_tutar_400_bacak_YOK()
    {
        var k = Kur("virman buyuk");
        string hedef;
        using (var db = f.Vt.OlusturSistem())
            hedef = TestKurulum.KasaEkle(db, k.TenantId, k.SubeId, "TRY", 0m, "Hedef");
        var once = Kasa(k);
        var r = await f.Ortam.Istemci(k).PostJson("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "KASA", hedefId = hedef, tutar = 99_999_999_999_999.99m,
        });
        await Red400(r, "tutar");
        Assert.Equal(once, Kasa(k));
        Assert.Equal(0m, Kasa(k, hedef).Bakiye);
    }

    // PAKET GOC-A (K9): gider 0,015 YUVARLANMAZ — 400 alan:tutar, bakiye değişmez; 0,02 düşülür.
    [Fact]
    public async Task D_P5_gider_0_015_yuvarlanmaz_400_0_02_dusulur_bakiye_249999_98()
    {
        var k = Kur("gider 0005");
        var c = f.Ortam.Istemci(k);
        await Ok(await c.PostJson("/api/v1/gider-kategorileri", new { ad = "Kira" }));
        string kat;
        using (var db = f.Vt.OlusturSistem())
            kat = db.GiderKategorileri.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Ad == "Kira").Id;
        await Red400(await c.PostJson("/api/v1/giderler", new { kasaId = k.KasaId, giderKategoriId = kat, tutar = 0.015m }), "tutar");
        Assert.Equal(250_000m, Kasa(k).Bakiye);
        await Ok(await c.PostJson("/api/v1/giderler", new { kasaId = k.KasaId, giderKategoriId = kat, tutar = 0.02m }));
        Assert.Equal(249_999.98m, Kasa(k).Bakiye);
    }

    // ── Banka hesabı: geçmişi olan hesapta para birimi (Kasa K4 kuralının eşi) ──

    // BULGU D-P5-1 (ORTA, hasım PATCH-5 03.10) — PAKET SON-KALANLAR'da düzeltildi (BankaModulu.ParaBirimiKilidi); Skip kaldırıldı.
    [Fact]
    public async Task D_P5_banka_bakiyeli_hesapta_para_birimi_DEGISMEZ_409()
    {
        var k = Kur("banka pb");
        var c = f.Ortam.Istemci(k);
        var iban = TestIban.Tamamla(string.Concat(Enumerable.Range(0, 22).Select(i => (char)('0' + (k.TenantId[i % k.TenantId.Length] % 10)))));
        await Ok(await c.PostJson("/api/v1/bankalar", new { bankaAdi = "D Banka", hesapAdi = "D Vadesiz", iban, paraBirimi = "USD" }));
        string hesap;
        using (var db = f.Vt.OlusturSistem())
            hesap = db.BankaHesaplari.AsNoTracking().Single(x => x.TenantId == k.TenantId).Id;
        await Ok(await Ham(c, HttpMethod.Post, $"/api/v1/bankalar/{hesap}/hareket", """{"yon":"GELEN","tutar":100}"""));
        var r = await Ham(c, HttpMethod.Patch, $"/api/v1/bankalar/{hesap}", """{"paraBirimi":"TRY"}""");
        Assert.False(r.IsSuccessStatusCode, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        using var db2 = f.Vt.OlusturSistem();
        Assert.Equal("USD", db2.BankaHesaplari.AsNoTracking().Single(x => x.Id == hesap).ParaBirimi);
    }
}

public sealed class PATCH5SinirBirimTestleri
{
    [Fact]
    public void D_P5_turkce_giris_45000_1_5_belirsiz_1500_bos()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(999_999_999.99m, FormAlani.SayiCoz("999.999.999,99").Deger);
        var b = FormAlani.SayiCoz("1.500");
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, b.Sonuc);
        Assert.Null(b.Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
        Assert.Equal(0.0001m, FormAlani.SayiCoz("0,0001").Deger);
    }

    [Theory]
    [InlineData("0.005", "0.01")]
    [InlineData("-0.005", "-0.01")]
    [InlineData("0.015", "0.02")]
    [InlineData("0.004", "0.00")]
    [InlineData("999999999.99", "999999999.99")]
    public void D_P5_IsaretliTutar_AwayFromZero(string g, string b)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        Assert.Equal(decimal.Parse(b, inv),
            Kuyumcu.Api.Modules.KolonTavani.IsaretliTutar(decimal.Parse(g, inv), "tutar", "Tutar"));
    }

    [Fact]
    public void D_P5_IsaretliHas_0_0001_ve_yarim_milyonda()
    {
        Assert.Equal(0.0001m, Kuyumcu.Api.Modules.KolonTavani.IsaretliHas(0.0001m, "has", "Has"));
        Assert.Equal(0.000001m, Kuyumcu.Api.Modules.KolonTavani.IsaretliHas(0.0000005m, "has", "Has"));
        Assert.Equal(-0.000001m, Kuyumcu.Api.Modules.KolonTavani.IsaretliHas(-0.0000005m, "has", "Has"));
    }
}
