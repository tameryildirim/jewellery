using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Maliyet;
using Kuyumcu.Api.Modules.Satis;
using Kuyumcu.Core.Domain;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET RAPOR-4 — HASIM DOĞRULAYICI (Aşkın + Mehmet) FİNANSAL SINIR TESTLERİ
// Kalıcı gerileme testi. Beklenen değerler ELLE hesaplanmış sabitlerdir (üretim
// kodundan türetilmez).
//   S1  kart maliyeti 0 / eksi / null → bilinmiyor (fail-closed), kâr null
//   S2  kuruş yuvarlaması: R2 AwayFromZero (0,005 sınırı) — bileşen bazında
//   S3  çok büyük tutar (999.999.999,99) taşmasız
//   S4  nakit farkı eksi çıkabilir (alış > net satış) — işaret korunur, kuruş kaybı yok
//   S5  DB: eksi kart maliyeti kâra GİRMEZ (HESAPLANAMADI), büyük kart maliyeti doğru kâr
//   S6  Türkçe giriş: '45.000', '1,5', belirsiz '1.500' reddi (FormAlani.SayiCoz)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class RAPOR4SinirSafTestleri
{
    private static UrunKovasi Yalniz_N(decimal tl, decimal bilinenMal, decimal bosAdet) =>
        new() { HareketSayisi = 1, NAdet = 1, NTl = tl, NMal = bilinenMal, NBosAdet = bosAdet };

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("-0.01")]
    public void S1_kart_sifir_ya_da_eksi_ise_kar_HESAPLANAMAZ(string kart)
    {
        var k = Yalniz_N(25_000m, 0m, 1m);
        Assert.Null(k.Kar(decimal.Parse(kart, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void S1b_kart_null_ise_yalniz_K_kumesi_kara_girer()
    {
        var k = new UrunKovasi { KAdet = 1, KTl = 1_000m, KMal = 400m, NAdet = 1, NTl = 9_999m, NMal = 0m, NBosAdet = 1m };
        Assert.Equal(600m, k.Kar(null));      // N kümesi kart yokken dışarıda
        Assert.Equal(600m, k.Kar(-1m));       // eksi kart = yok
        Assert.Equal(1_000m + 9_999m - 400m - 9_000m, k.Kar(9_000m));   // 1.599
    }

    [Fact]
    public void S2_kurus_yuvarlamasi_AwayFromZero_005_siniri()
    {
        // hasılat 100,005 → 100,01 ; maliyet 0,005 → 0,01 ; kâr 100,00 (bankacı yuvarlaması 100,00 − 0,00 = 100,00 değil, yine 100,00 — ayırt edici durum aşağıda)
        var a = new UrunKovasi { KAdet = 1, KTl = 100.005m, KMal = 0.005m };
        Assert.Equal(100.00m, a.Kar(null));
        // ayırt edici: hasılat 0,125 → AwayFromZero 0,13 (bankacı 0,12) ; maliyet 0 → kâr 0,13
        var b = new UrunKovasi { KAdet = 1, KTl = 0.125m, KMal = 0m };
        Assert.Equal(0.13m, b.Kar(null));
        // kart × boş adet: 0,0025 × 2 = 0,005 → 0,01 ; hasılat 1,00 → kâr 0,99
        var c = Yalniz_N(1.00m, 0m, 2m);
        Assert.Equal(0.99m, c.Kar(0.0025m));
    }

    [Fact]
    public void S3_cok_buyuk_tutar_tasmaz()
    {
        var k = Yalniz_N(999_999_999.99m, 0m, 1_000m);
        Assert.Equal(999_999_999.99m - 999_999_999_990m, k.Kar(999_999_999.99m));   // −998.999.999.990,01
        var t = new UrunKovasi { KAdet = 1, KTl = 999_999_999.99m, KMal = 0.01m };
        Assert.Equal(999_999_999.98m, t.Kar(null));
    }

    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("0,0001", "0.0001")]
    [InlineData("999.999.999,99", "999999999.99")]
    public void S6_Turkce_giris_dogru_cozulur(string ham, string beklenen)
    {
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), FormAlani.SayiCoz(ham).Deger);
    }

    [Fact]
    public void S6b_belirsiz_1500_reddedilir_bos_null()
    {
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
    }
}

[Collection("veritabani")]
public sealed class RAPOR4SinirDbTestleri(TestVeritabani vt)
{
    private static JsonElement J(object o) => JsonDocument.Parse(JsonSerializer.Serialize(o)).RootElement;
    private static DateOnly Bugun => Zaman.YerelBugun();

    private static string H(KuyumcuDbContext db, Kiraci k, string tur, decimal tl, decimal? adet)
    {
        var id = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Tur = tur, Tarih = DateTime.UtcNow.AddSeconds(-30), TlTutari = tl,
            HasGrami = 1.5m, Maden = Maden.Altin, ParaBirimi = "TRY", KurSnapshotId = k.KurId, UrunId = k.UrunId,
            KullaniciId = k.KullaniciId, Olusturuldu = DateTime.UtcNow, Adet = adet,
        });
        return id;
    }

    private static void T(KuyumcuDbContext db, Kiraci k, string cikisId, decimal adet, decimal? birimTl)
    {
        db.Set<StokPartiTuketimi>().Add(new StokPartiTuketimi
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, PartiId = "R4S-PARTI", CikisHareketId = cikisId,
            UrunId = k.UrunId, SubeId = k.SubeId, Adet = adet, BirimMaliyetTl = birimTl, BirimMaliyetHas = null,
            Tarih = DateTime.UtcNow.AddSeconds(-30),
        });
    }

    private static async Task KartYaz(KuyumcuDbContext db, Kiraci k, decimal? kart)
    {
        var u = await db.Urunler.FirstAsync(x => x.TenantId == k.TenantId && x.Id == k.UrunId);
        u.AlisMaliyeti = kart;
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task S4_nakit_farki_eksi_isaret_ve_kurus_korunur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "R4S-S4");
        using var db = vt.Olustur(k);
        H(db, k, "SATIS", 10_000.01m, 1m);
        H(db, k, "SATIS_IADE", -0.01m, 1m);
        H(db, k, "ALIS", -15_000.00m, 1m);
        H(db, k, "HURDA_ALIS", -0.05m, null);
        db.SaveChanges();
        var r = J(await TestKurulum.RaporServisi(db).AylikOzet(Bugun.Year, Bugun.Month, k.Islem));
        Assert.Equal(10_000.00m, r.GetProperty("netSatisTl").GetDecimal());
        Assert.Equal(-5_000.05m, r.GetProperty("satisAlisFarkiTl").GetDecimal());   // 10.000,00 − 15.000,00 − 0,05
    }

    [Fact]
    public async Task S4b_nakit_farki_sifir_ve_cok_buyuk()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "R4S-S4b");
        using var db = vt.Olustur(k);
        var r0 = J(await TestKurulum.RaporServisi(db).AylikOzet(Bugun.Year, Bugun.Month, k.Islem));
        Assert.Equal(0m, r0.GetProperty("satisAlisFarkiTl").GetDecimal());
        H(db, k, "SATIS", 999_999_999.99m, 1m);
        db.SaveChanges();
        var r = J(await TestKurulum.RaporServisi(db).AylikOzet(Bugun.Year, Bugun.Month, k.Islem));
        Assert.Equal(999_999_999.99m, r.GetProperty("satisAlisFarkiTl").GetDecimal());
    }

    [Theory]
    [InlineData("-5")]
    [InlineData("0")]
    public async Task S5_eksi_ya_da_sifir_kart_maliyeti_kara_GIRMEZ(string kart)
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "R4S-S5" + kart);
        using var db = vt.Olustur(k);
        var s = H(db, k, "SATIS", 25_000m, 1m);
        T(db, k, s, 1m, null);
        db.SaveChanges();
        await KartYaz(db, k, decimal.Parse(kart, System.Globalization.CultureInfo.InvariantCulture));
        var servis = new KarZararServisi(db);
        var d = J(await servis.Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("HESAPLANAMADI", d.GetProperty("karDurumu").GetString());
        Assert.Equal(JsonValueKind.Null, d.GetProperty("karTl").ValueKind);
        Assert.Equal(0m, d.GetProperty("kartMaliyetliAdet").GetDecimal());
        var u = J(await servis.UrunBazli(k.Islem, Bugun, Bugun, null, 50));
        var satir = Assert.Single(u.GetProperty("satirlar").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, satir.GetProperty("karTl").ValueKind);
    }

    [Fact]
    public async Task S5b_parti_maliyeti_varsa_kart_YOK_SAYILIR_buyuk_kart_dogru_kar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "R4S-S5b");
        using var db = vt.Olustur(k);
        var s1 = H(db, k, "SATIS", 999_999_999.99m, 1m);
        T(db, k, s1, 1m, null);                    // kartla bilinir
        var s2 = H(db, k, "SATIS", 100.005m, 1m);  // kolon 2 hane: 100,01 olarak yazılır
        T(db, k, s2, 1m, 50.0049m);                // parti önceliklidir
        db.SaveChanges();
        await KartYaz(db, k, 999_999_000.00m);
        var d = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("TAM", d.GetProperty("karDurumu").GetString());
        Assert.Equal(1m, d.GetProperty("kartMaliyetliAdet").GetDecimal());
        // hasılat 999.999.999,99 + 100,01 = 1.000.000.100,00 ; maliyet 999.999.000,00 + 50,0049 → 999.999.050,00 (R2)
        Assert.Equal(1_050.00m, d.GetProperty("karTl").GetDecimal());
    }
}
