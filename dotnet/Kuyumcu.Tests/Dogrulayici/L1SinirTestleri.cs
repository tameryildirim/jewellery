using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET L-1 — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİTTİR (üretim kodundan
// hesaplanmaz). L-1 çok kalemli düz satışı TEK POST /islemler/satis-fisi'ye
// yönlendirdi: ekran tek bacak gönderir, tutarı = FisHesap.Toplam (istemci), sunucu
// fiş toplamını kalem başına AwayFromZero ile hesaplar ve bacak eşitliğini ister.
// Sınır: sıfır · eksi · 0,005 kuruş · has 4 hane · 999.999.999,99 · boş/null ·
// tr-TR giriş (SayiCoz).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class L1SinirTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad, decimal urunGram = 10m, string ayarId = "K22")
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETL1-TEST-DOG-" + ad, urunGram: urunGram, ayarId: ayarId);
    }

    private static object Fis(Kiraci k, string? cariId, string arac, decimal[] birimler, decimal toplam,
                              decimal adet = 1m) => new
    {
        cariId,
        kalemler = birimler.Select(b => new { urunId = k.UrunId, adet, birimFiyatTl = b, hediye = false }),
        odemeler = new[] { new { arac, tutarTl = toplam, kasaId = arac == "VERESIYE" ? null : k.KasaId } },
        kurSnapshotId = k.KurId,
        fisIskontoYuzde = 0m,
    };

    private Task<HttpResponseMessage> Gonder(Kiraci k, object govde) =>
        f.Ortam.Istemci(k, "TEZGAHTAR").PostAsJsonAsync("/api/v1/islemler/satis-fisi", govde);

    private List<Hareket> Satislar(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS").ToList();
    }

    private static FisKalemi Kalem(decimal adet, decimal birim) =>
        new() { UrunId = "u", UrunAdi = "u", Adet = adet, BirimFiyat = birim };

    // ── SIFIR ───────────────────────────────────────────────────────────────
    [Fact]
    public async Task S1_sifir_tutarli_iki_kalem_reddedilir_hareket_yazilmaz()
    {
        var k = YeniKiraci("sifir");
        var c = await Gonder(k, Fis(k, k.CariId, "NAKIT", [0m, 0m], 0m));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Empty(Satislar(k));
    }

    // ── EKSİ ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task S2_eksi_birim_fiyat_reddedilir_hareket_yazilmaz()
    {
        var k = YeniKiraci("eksi");
        var c = await Gonder(k, Fis(k, k.CariId, "NAKIT", [-100m, 200m], 100m));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Empty(Satislar(k));
    }

    // ── KURUŞ 0,005 (AwayFromZero) — istemci toplamı = sunucu toplamı ─────────
    [Fact]
    public void S3a_istemci_toplami_0_005_kalem_basina_yukari_yuvarlar()
    {
        Assert.Equal(0.02m, FisHesap.Toplam([Kalem(1m, 0.005m), Kalem(1m, 0.005m)], null));
        Assert.Equal(0.00m, FisHesap.Toplam([Kalem(1m, 0.004m), Kalem(1m, 0.004m)], null));
        // 1,5 × 0,333 = 0,4995 → 0,50 ; iki kalem → 1,00
        Assert.Equal(1.00m, FisHesap.Toplam([Kalem(1.5m, 0.333m), Kalem(1.5m, 0.333m)], null));
    }

    [Fact]
    public async Task S3b_sunucu_ayni_kuralla_0_005_iki_kalem_0_02_tek_fis()
    {
        var k = YeniKiraci("kurus");
        var c = await Gonder(k, Fis(k, k.CariId, "NAKIT", [0.005m, 0.005m], 0.02m));
        Assert.True(c.IsSuccessStatusCode, $"{(int)c.StatusCode}: {await c.Content.ReadAsStringAsync()}");
        var s = Satislar(k);
        Assert.Equal(2, s.Count);
        Assert.All(s, x => Assert.Equal(0.01m, x.TlTutari));
        Assert.Single(s.Select(x => x.FisId).Distinct());
    }

    [Fact]
    public async Task S3c_kesirli_adet_ekran_toplami_sunucuda_kabul_1_00()
    {
        var k = YeniKiraci("kesir");
        using (var g = f.Vt.OlusturSistem()) TestKurulum.UrunuGramBirimiYap(g, k.UrunId);   // PAKET GOC-C (K14): kesirli miktar yalnız GRAM birimde — sınır ölçüsü GRAM birimli üründe korunur
        var c = await Gonder(k, Fis(k, k.CariId, "NAKIT", [0.333m, 0.333m], 1.00m, adet: 1.5m));
        Assert.True(c.IsSuccessStatusCode, $"{(int)c.StatusCode}: {await c.Content.ReadAsStringAsync()}");
        Assert.Equal(1.00m, Satislar(k).Sum(x => x.TlTutari));
    }

    // ── HAS/GRAM 4 HANE ─────────────────────────────────────────────────────
    [Fact]
    public async Task S4_has_4_hane_iki_kalem_kaybolmaz()
    {
        var k = YeniKiraci("has", urunGram: 1.0001m, ayarId: "K24");
        var c = await Gonder(k, Fis(k, k.CariId, "NAKIT", [100m, 100m], 200m));
        Assert.True(c.IsSuccessStatusCode, $"{(int)c.StatusCode}: {await c.Content.ReadAsStringAsync()}");
        var s = Satislar(k);
        Assert.All(s, x => Assert.Equal(-1.0001m, x.HasGrami));
        Assert.Equal(-2.0002m, s.Sum(x => x.HasGrami));
        using var db = f.Vt.OlusturSistem();
        var stok = db.Stoklar.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.UrunId == k.UrunId);
        Assert.Equal(98m, stok.Adet);
        Assert.Equal(98.0098m, stok.ToplamHasGram);
    }

    // ── ÇOK BÜYÜK TUTAR ─────────────────────────────────────────────────────
    [Fact]
    public async Task S5_999_999_999_99_KART_500_uretmez_bilgisiz_MASAK_reddi_hareket_yok()
    {
        var k = YeniKiraci("buyuk");
        var c = await Gonder(k, Fis(k, k.CariId, "KART", [499_999_999.99m, 500_000_000.00m], 999_999_999.99m));
        Assert.True((int)c.StatusCode < 500, $"{(int)c.StatusCode}: {await c.Content.ReadAsStringAsync()}");
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);   // MASAK kimlik bloğu yok → fail-closed
        Assert.Empty(Satislar(k));
        Assert.Equal(999_999_999.99m, FisHesap.Toplam([Kalem(1m, 499_999_999.99m), Kalem(1m, 500_000_000.00m)], null));
    }

    // ── BOŞ / NULL ──────────────────────────────────────────────────────────
    [Fact]
    public async Task S6_bos_kalem_ve_null_odeme_reddedilir()
    {
        var k = YeniKiraci("bos");
        var c1 = await Gonder(k, new { cariId = k.CariId, kalemler = Array.Empty<object>(),
            odemeler = new[] { new { arac = "NAKIT", tutarTl = 10m, kasaId = k.KasaId } }, kurSnapshotId = k.KurId });
        Assert.Equal(HttpStatusCode.BadRequest, c1.StatusCode);
        var c2 = await Gonder(k, new { cariId = k.CariId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 10m } },
            odemeler = (object?)null, kurSnapshotId = k.KurId });
        Assert.Equal(HttpStatusCode.BadRequest, c2.StatusCode);
        Assert.Empty(Satislar(k));
    }

    // ── TÜRKÇE GİRİŞ ────────────────────────────────────────────────────────
    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("28.711,64", "28711.64")]
    [InlineData("0,005", "0.005")]
    [InlineData("0,0001", "0.0001")]
    [InlineData("999.999.999,99", "999999999.99")]
    public void S7_SayiCoz_turkce_giris(string girdi, string beklenen)
    {
        var c = FormAlani.SayiCoz(girdi);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }

    [Theory]
    [InlineData("1.500", "Belirsiz")]
    [InlineData("", "Bos")]
    [InlineData(null, "Bos")]
    [InlineData("abc", "Gecersiz")]
    public void S8_SayiCoz_belirsiz_bos_red(string? girdi, string sonuc)
    {
        var c = FormAlani.SayiCoz(girdi);
        Assert.Equal(sonuc, c.Sonuc.ToString());
        Assert.Null(c.Deger);
    }
}
