using System.Net;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Urun;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET URUN-ACILIS-ATOMIK sınır testleri.
// Değişmez: açılış stoğu adımı HANGİ sınırda reddederse reddetsin ürün kartı
// dahil HİÇBİR satır kalmaz; kabul edilen sınırda kart + stok + defter tutarlı.
// Beklenen değerler SABİT yazılır (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class URUNACILISATOMIKSinirTestleri(TestVeritabani vt)
{
    private static UrunOlusturDto Dto(string kod, AcilisStoguDto? acilis, decimal gram = 10m, string birim = "ADET") => new()
    {
        Tip = "ZIYNET", Kod = kod, Ad = "Sınır bilezik", AyarId = "K22",
        GramAgirlik = gram, Birim = birim, AcilisStogu = acilis,
    };

    private async Task<(int Urun, int Stok, int Hareket)> Sayim(string tenantId, string kod)
    {
        await using var s = vt.OlusturSistem();
        var idler = await s.Urunler.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.Kod == kod).Select(u => u.Id).ToListAsync();
        return (idler.Count,
            await s.Stoklar.AsNoTracking().CountAsync(x => x.TenantId == tenantId && idler.Contains(x.UrunId)),
            await s.Hareketler.AsNoTracking().CountAsync(h => h.TenantId == tenantId && h.UrunId != null && idler.Contains(h.UrunId)));
    }

    [Fact]
    public async Task S1_Sifir_adet_kart_dogar_stok_ve_defter_YOK()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETURUN-ACILIS-ATOMIK-TEST S1");
        await new UrunServisi(vt.Olustur(k)).Olustur(Dto("SNR-0", new AcilisStoguDto { Adet = 0m }), k.Islem);
        Assert.Equal((1, 0, 0), await Sayim(k.TenantId, "SNR-0"));
    }

    [Fact]
    public async Task S2_Bos_null_acilis_kart_dogar_stok_YOK()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETURUN-ACILIS-ATOMIK-TEST S2");
        await new UrunServisi(vt.Olustur(k)).Olustur(Dto("SNR-N", null), k.Islem);
        Assert.Equal((1, 0, 0), await Sayim(k.TenantId, "SNR-N"));
    }

    [Fact]
    public async Task S3_Cok_buyuk_adet_REDDEDILIR_ve_kart_DAHIL_hicbir_satir_kalmaz()
    {
        // 999.999.999,99 adet ≤ adet tavanı ama × 9,16 has = 9,16e9 > has tavanı →
        // red KART YAZILDIKTAN SONRA (StokSayimGuncelle içinde) gelir: atomiklik sınırı.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETURUN-ACILIS-ATOMIK-TEST S3");
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            new UrunServisi(vt.Olustur(k)).Olustur(
                Dto("SNR-BYK", new AcilisStoguDto { Adet = 999_999_999m }), k.Islem));   // PAKET GOC-C (K14): ADET birimde tam sayı — sınır aynı (× 9,16 has > tavan)
        Assert.Contains("çok büyük", hata.Message);
        Assert.Equal((0, 0, 0), await Sayim(k.TenantId, "SNR-BYK"));

        // Tekrar (geçerli adetle) aynı kod → 409 değil, başarı.
        await new UrunServisi(vt.Olustur(k)).Olustur(
            Dto("SNR-BYK", new AcilisStoguDto { Adet = 2m }), k.Islem);
        Assert.Equal((1, 1, 1), await Sayim(k.TenantId, "SNR-BYK"));
    }

    [Fact]
    public async Task S4_Kurus_yuvarlamasi_0_0005_AwayFromZero_0_001_adet()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETURUN-ACILIS-ATOMIK-TEST S4");
        await new UrunServisi(vt.Olustur(k)).Olustur(
            Dto("SNR-YUV", new AcilisStoguDto { Adet = 0.0005m }, birim: "GRAM"), k.Islem);   // PAKET GOC-C (K14): kesir yalnız GRAM birimde
        await using var s = vt.OlusturSistem();
        var urunId = await s.Urunler.AsNoTracking().Where(u => u.TenantId == k.TenantId && u.Kod == "SNR-YUV")
            .Select(u => u.Id).SingleAsync();
        var stok = await s.Stoklar.AsNoTracking().SingleAsync(x => x.TenantId == k.TenantId && x.UrunId == urunId);
        Assert.Equal(0.001m, stok.Adet);
    }

    [Fact]
    public async Task S5_Has_gram_4_hane_0_0001_defter_ve_stok_SABIT_deger()
    {
        // gram 10,0001 × 916/1000 = 9,1600916 → ürün has (4 hane kuralı ne ise) ;
        // 3 adet açılış: stok toplam has = Yuvarla6(3 × urun.has). Sabit: has 9,1601 ise 27,4803.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETURUN-ACILIS-ATOMIK-TEST S5");
        await new UrunServisi(vt.Olustur(k)).Olustur(
            Dto("SNR-HAS", new AcilisStoguDto { Adet = 3m }, gram: 10.0001m), k.Islem);
        await using var s = vt.OlusturSistem();
        var urun = await s.Urunler.AsNoTracking().SingleAsync(u => u.TenantId == k.TenantId && u.Kod == "SNR-HAS");
        var stok = await s.Stoklar.AsNoTracking().SingleAsync(x => x.TenantId == k.TenantId && x.UrunId == urun.Id);
        var har = await s.Hareketler.AsNoTracking().SingleAsync(h => h.TenantId == k.TenantId && h.UrunId == urun.Id);
        Assert.Equal(9.1601m, Math.Round(urun.HasGrami, 4));
        Assert.Equal(3m, stok.Adet);
        Assert.Equal(27.480276m, stok.ToplamHasGram);   // SABİT: 10,0001 × 0,916 = 9,1600916 → 6 hane 9,160092 × 3
        Assert.Equal(stok.ToplamHasGram, har.HasGrami);
        Assert.Equal(0m, har.TlTutari);
    }

    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    public void S6_Turkce_giris_cozulur(string ham, string beklenen) =>
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture),
            FormAlani.SayiCoz(ham).Deger);

    [Fact]
    public void S7_Turkce_belirsiz_1_500_REDDEDILIR()
    {
        var c = FormAlani.SayiCoz("1.500");
        Assert.Null(c.Deger);
    }

    // ── Doğrulayıcı tur 3 (08.10 12:5x) — kendi sınırlarım ─────────────────────

    [Fact]
    public async Task S9_Servis_yolunda_eksi_acilis_EKSI_STOK_ve_DEFTER_URETMEZ()
    {
        // HTTP [Range] eksiyi reddeder (S8); servis yolunda (DataAnnotations koşmaz)
        // eksi adet hiçbir koşulda eksi stok / defter satırı doğurmamalı.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETURUN-ACILIS-ATOMIK-TEST S9");
        try
        {
            await new UrunServisi(vt.Olustur(k)).Olustur(
                Dto("SNR-EKSI", new AcilisStoguDto { Adet = -1m }), k.Islem);
        }
        catch (BadRequestException) { /* red de kabul: aşağıdaki değişmez yine sınanır */ }
        var (_, stok, hareket) = await Sayim(k.TenantId, "SNR-EKSI");
        Assert.Equal(0, stok);
        Assert.Equal(0, hareket);
    }

    [Fact]
    public async Task S10_Has_tavani_SINIRI_bir_binde_bir_ustu_REDDEDILIR_alti_KABUL()
    {
        // Has tavanı 999.999.999,999999; ürün has = 10 × 0,916 = 9,16 g.
        // PAKET GOC-C (K14): ADET birimde açılış adedi TAM SAYI — sınır bir ADET üstü/altı ile ölçülür:
        // 109.170.306 × 9,16 = 1.000.000.002,96 → RED, kart dahil 0 satır.
        // 109.170.305 × 9,16 =   999.999.993,80 → KABUL, kart+stok+defter 1/1/1.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETURUN-ACILIS-ATOMIK-TEST S10");
        await Assert.ThrowsAsync<BadRequestException>(() =>
            new UrunServisi(vt.Olustur(k)).Olustur(
                Dto("SNR-TVN", new AcilisStoguDto { Adet = 109_170_306m }), k.Islem));
        Assert.Equal((0, 0, 0), await Sayim(k.TenantId, "SNR-TVN"));

        await new UrunServisi(vt.Olustur(k)).Olustur(
            Dto("SNR-TVN", new AcilisStoguDto { Adet = 109_170_305m }), k.Islem);
        Assert.Equal((1, 1, 1), await Sayim(k.TenantId, "SNR-TVN"));
        await using var s = vt.OlusturSistem();
        var urunId = await s.Urunler.AsNoTracking().Where(u => u.TenantId == k.TenantId && u.Kod == "SNR-TVN")
            .Select(u => u.Id).SingleAsync();
        var stok = await s.Stoklar.AsNoTracking().SingleAsync(x => x.TenantId == k.TenantId && x.UrunId == urunId);
        Assert.Equal(109_170_305m, stok.Adet);
        Assert.Equal(999_999_993.8m, stok.ToplamHasGram);   // SABİT (109.170.305 × 9,16)
    }

    [Fact]
    public async Task S11_Servis_yolunda_0_0004_adet_yuvarlaninca_SIFIR_defter_YOK()
    {
        // 0,0004 > 0 olduğu için açılış dalına girer; 3 haneye AwayFromZero → 0,000.
        // Sıfır farkın defter satırı OLMAZ; stok satırı varsa adedi 0'dır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETURUN-ACILIS-ATOMIK-TEST S11");
        await new UrunServisi(vt.Olustur(k)).Olustur(
            Dto("SNR-0004", new AcilisStoguDto { Adet = 0.0004m }, birim: "GRAM"), k.Islem);   // PAKET GOC-C (K14): kesir yalnız GRAM birimde
        var (urun, _, hareket) = await Sayim(k.TenantId, "SNR-0004");
        Assert.Equal(1, urun);
        Assert.Equal(0, hareket);
        await using var s = vt.OlusturSistem();
        var urunId = await s.Urunler.AsNoTracking().Where(u => u.TenantId == k.TenantId && u.Kod == "SNR-0004")
            .Select(u => u.Id).SingleAsync();
        Assert.False(await s.Stoklar.AsNoTracking().AnyAsync(x => x.TenantId == k.TenantId && x.UrunId == urunId && x.Adet != 0m));
    }
}

[Collection("http")]
public sealed class URUNACILISATOMIKSinirHttpTestleri(HttpFikstur f)
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0.0004)]
    public async Task S8_HTTP_eksi_ve_tavan_alti_acilis_400_ve_kart_YOK(double adet)
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETURUN-ACILIS-ATOMIK-TEST S8");
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var kod = "SNR-H" + (adet < 0 ? "E" : "K");
        var cevap = await istemci.PostJson("/api/v1/urunler", new
        {
            tip = "ZIYNET", kod, ad = "Sınır HTTP", ayarId = "K22",
            gramAgirlik = 10m, birim = "ADET", acilisStogu = new { adet = (decimal)adet },
        });
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        using var kontrol = f.Vt.OlusturSistem();
        Assert.Equal(0, kontrol.Urunler.AsNoTracking().Count(u => u.TenantId == k.TenantId && u.Kod == kod));
    }
}
