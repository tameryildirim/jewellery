using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Maliyet;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET MADEN-2 — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenenler SABİT LİTERAL (üretim kodundan
// türetilmez). İddia: has bacağı kendi "bilinen" kümesiyle — TL maliyeti bilinen
// ama has maliyeti bilinmeyen altın satışı has kârına sıfır maliyetle GİRMEZ.
// Kur fikstürü AltinHasFiyati 6.827,20: 1.500→0,2197 · 1.000→0,1465 · 500→0,0732 has.
// Yuvarlama: MidpointRounding.AwayFromZero (proje kuralı).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MADEN2SinirTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) => new(db, NullLogger<MSatis.SatisServisi>.Instance);
    private static DateOnly Bugun => DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));
    private static System.Text.Json.JsonElement J(object o) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o)).RootElement;
    private static decimal D(System.Text.Json.JsonElement e, string ad) => e.GetProperty(ad).GetDecimal();
    private static int I(System.Text.Json.JsonElement e, string ad) => e.GetProperty(ad).GetInt32();
    private static string S(System.Text.Json.JsonElement e, string ad) => e.GetProperty(ad).GetString()!;
    private static bool Bos(System.Text.Json.JsonElement e, string ad) =>
        e.GetProperty(ad).ValueKind == System.Text.Json.JsonValueKind.Null;
    private Kiraci Kur(KuyumcuDbContext kurulum, string ad) => TestKurulum.KiraciKur(kurulum, "PAKETMADEN-2-TEST D " + ad);

    private static void Parti(KuyumcuDbContext db, Kiraci k, int gunOnce, bool hasBilinir) =>
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.SayimFazla, 1m,
            k.UrunHasGrami, DateTime.UtcNow.AddDays(-gunOnce), birimMaliyetTl: 1_000m,
            altinHasFiyati: hasBilinir ? 6_827.20m : null);

    private static async Task<string> Sat(KuyumcuDbContext db, Kiraci k, decimal birim)
    {
        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = birim,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);
        return await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS")
            .OrderByDescending(h => h.Olusturuldu).ThenByDescending(h => h.Id).Select(h => h.Id).FirstAsync();
    }

    // SIFIR: satış fiyatı = maliyet → has kârı 0,0000 (null DEĞİL), durum TAM
    [Fact]
    public async Task MS01_Sifir_kar_has_TAM_sifir_doner()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "sifir");
        using var db = vt.Olustur(k);
        Parti(db, k, 1, hasBilinir: true);
        await db.SaveChangesAsync();
        await Sat(db, k, 1_000m);
        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("TAM", S(r, "hasKarDurumu"));
        Assert.Equal(0m, D(r, "karTl"));
        Assert.Equal(0m, D(r, "karHas"));
    }

    // EKSİ: maliyetin altında satış → karHas işareti eksi; has-bilinmeyen satış zararı da şişirmez
    [Fact]
    public async Task MS02_Eksi_kar_has_isareti_dogru_ve_hasi_bilinmeyen_katilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "eksi");
        using var db = vt.Olustur(k);
        Parti(db, k, 2, hasBilinir: false);  // ilk satış (FIFO) bunu tüketir
        Parti(db, k, 1, hasBilinir: true);
        await db.SaveChangesAsync();
        await Sat(db, k, 3_000m);   // has bilinmiyor — has kârına GİRMEMELİ (girseydi +)
        await Sat(db, k, 500m);     // has bilinen: 0,0732 − 0,1465 = −0,0733
        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("KISMI", S(r, "hasKarDurumu"));
        Assert.Equal(1_500m, D(r, "karTl"));        // TL iki satış: 2.000 + (−500)
        Assert.Equal(-0.0733m, D(r, "karHas"));
        Assert.Equal(0.0732m, D(r, "bilinenHasilatHas"));
        // KAR-RAPOR-2 kuruş kuralı: türetilen = yuvarlanmış bileşenlerin farkı → R4(3.500/6.827,20)=0,5127 − 0,0732 = 0,4395
        // (doğrudan R4(3.000/6.827,20)=0,4394 DEĞİL; ekranda toplam − bilinen toplanabilir kalsın diye bilinçli).
        Assert.Equal(0.5127m, D(r, "hasilatHas"));
        Assert.Equal(0.4395m, D(r, "maliyetiEksikHasilatHas"));
    }

    // BOŞ: hiç hareket yok → has durumu VERI_YOK, eksik sayıları 0
    [Fact]
    public async Task MS03_Bos_donem_VERI_YOK()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "bos");
        using var db = vt.Olustur(k);
        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("VERI_YOK", S(r, "hasKarDurumu"));
        Assert.Equal(0, I(r, "hasMaliyetiEksikSatisAdedi"));
        Assert.Equal(0, I(r, "yalnizHasMaliyetiEksikAdedi"));
    }

    // KARIŞIK: TL de bilinmeyen (partisiz) + has bilinmeyen + bilinen → "yalnız has" sayımı 1 (çift sayım yok)
    [Fact]
    public async Task MS04_Karisik_eksik_cift_sayilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "karisik");
        using var db = vt.Olustur(k);
        Parti(db, k, 2, hasBilinir: false);
        Parti(db, k, 1, hasBilinir: true);
        await db.SaveChangesAsync();
        await Sat(db, k, 1_500m);
        await Sat(db, k, 1_500m);
        await Sat(db, k, 1_500m);   // parti kalmadı → TL maliyeti de bilinmiyor
        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("KISMI", S(r, "karDurumu"));
        Assert.Equal(1_000m, D(r, "karTl"));
        Assert.Equal("KISMI", S(r, "hasKarDurumu"));
        Assert.Equal(2, I(r, "hasMaliyetiEksikSatisAdedi"));
        Assert.Equal(1, I(r, "yalnizHasMaliyetiEksikAdedi"));
        Assert.Equal(0.0732m, D(r, "karHas"));
    }

    // İADE: has'ı bilinmeyen satışın iadesi de has-eksik iade sayılır; has kârı bozulmaz
    [Fact]
    public async Task MS05_Hasi_bilinmeyen_satisin_iadesi_has_eksik_iade()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "iade");
        using var db = vt.Olustur(k);
        Parti(db, k, 2, hasBilinir: false);
        Parti(db, k, 1, hasBilinir: true);
        await db.SaveChangesAsync();
        var hasiz = await Sat(db, k, 1_500m);
        await Sat(db, k, 1_500m);
        await Satis(db).SatisIade(new MSatis.IadeDto
        {
            KaynakHareketId = hasiz, Adet = 1m, OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId,
        }, k.Islem);
        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("KISMI", S(r, "hasKarDurumu"));
        Assert.Equal(1, I(r, "hasMaliyetiEksikSatisAdedi"));
        Assert.Equal(1, I(r, "hasMaliyetiEksikIadeAdedi"));
        Assert.Equal(0.0732m, D(r, "karHas"));
    }

    // 4 HANE: adet karşılama toleransı 0,0005 sınırı — 0,9995 bilinen, 0,9994 bilinmiyor
    [Fact]
    public void MS06_Has_bilinen_tolerans_sinir_4_hane()
    {
        var h = new (string, decimal?)[] { ("a", 1m), ("b", 1m) };
        var t = new (string?, decimal, decimal?, decimal?)[]
        {
            ("a", 0.9995m, 1_000m, 0.1465m),
            ("b", 0.9994m, 1_000m, 0.1465m),
        };
        Assert.Equal(["a"], KarZararServisi.HasMaliyetiBilinenler(h, t).Order().ToArray());
    }

    // SIFIR has maliyeti (null değil) bilinen sayılır; null bilinmez
    [Fact]
    public void MS07_Sifir_has_maliyeti_bilinen_null_bilinmeyen()
    {
        var h = new (string, decimal?)[] { ("a", 1m), ("b", 1m) };
        var t = new (string?, decimal, decimal?, decimal?)[] { ("a", 1m, 0m, 0m), ("b", 1m, 0m, null) };
        Assert.Equal(["a"], KarZararServisi.HasMaliyetiBilinenler(h, t).Order().ToArray());
    }

    // BİÇİM: 4 hane AwayFromZero, binlik gruplu, çok büyük değer
    [Theory]
    [InlineData("0.00005", "0,0001 has")]
    [InlineData("0.00004", "0,0000 has")]
    [InlineData("1234.56785", "1.234,5679 has")]
    [InlineData("999999999.99", "999.999.999,9900 has")]
    [InlineData("-0.0733", "-0,0733 has")]
    public void MS08_Has_bicimi_sinirlar(string girdi, string beklenen)
    {
        var d = decimal.Parse(girdi, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(beklenen, Bicim.Has(d));
    }

    // TÜRKÇE GİRİŞ: '45.000' binlik, '1,5' ondalık, '1.500' belirsiz → RED
    [Fact]
    public void MS09_Turkce_giris_SayiCoz()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(999_999_999.99m, FormAlani.SayiCoz("999.999.999,99").Deger);
    }

    // ═══ TUR 2 DOĞRULAYICI (03.10) — ek sınırlar; beklenenler SABİT LİTERAL ═══

    // BÜYÜK (MASAK eşiği 185.000 altı en büyük yuvarlak): 180.000 satış → karTl 179.000; karHas 26,3651 − 0,1465 = 26,2186
    // (999.999.999,99 satış MASAK kapısında reddedilir — ölçüldü; çok büyük değer biçim sınırı MS08 + SayiCoz MS09)
    [Fact]
    public async Task MS10_Cok_buyuk_tutar_has_kari_tasmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "buyuk");
        using var db = vt.Olustur(k);
        Parti(db, k, 1, hasBilinir: true);
        await db.SaveChangesAsync();
        await Sat(db, k, 180_000m);
        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("TAM", S(r, "hasKarDurumu"));
        Assert.Equal(179_000m, D(r, "karTl"));
        Assert.Equal(26.3651m, D(r, "bilinenHasilatHas"));
        Assert.Equal(26.2186m, D(r, "karHas"));
    }

    // KURUŞ 0,005: parti birim maliyeti 1.000,005 → R2 AwayFromZero 1.000,01 → kâr 499,99 (500,00 DEĞİL)
    [Fact]
    public async Task MS11_Kurus_yarim_maliyet_AwayFromZero()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "kurus");
        using var db = vt.Olustur(k);
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.SayimFazla, 1m,
            k.UrunHasGrami, DateTime.UtcNow.AddDays(-1), birimMaliyetTl: 1_000.005m, altinHasFiyati: 6_827.20m);
        await db.SaveChangesAsync();
        await Sat(db, k, 1_500m);
        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal(1_000.01m, D(r, "maliyetTl"));
        Assert.Equal(499.99m, D(r, "karTl"));
        Assert.Equal("TAM", S(r, "hasKarDurumu"));
        Assert.Equal(0.0732m, D(r, "karHas"));
    }

    // GÜMÜŞ KARIŞIK: has fiyatsız gümüş satışı has bacağını KISMI yapmaz, has-eksik sayılmaz
    [Fact]
    public async Task MS12_Gumus_satisi_has_bacagini_kismi_yapmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "gumus");
        using var db = vt.Olustur(k);
        var gid = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Urunler.Add(new Urun
        {
            Id = gid, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"DG-{gid[..8]}", Ad = "Dogrulayici gumus",
            AyarId = "G925", Milyem = 925m, GramAgirlik = 20m, TasAgirlik = 0m, HasGrami = 18.5m,
            IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.Stoklar.Add(new Stok
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, UrunId = gid,
            Adet = 5m, ToplamHasGram = 92.5m, Guncellendi = now,
        });
        Parti(db, k, 1, hasBilinir: true);
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, gid, PartiKaynagi.SayimFazla, 1m, 18.5m,
            DateTime.UtcNow.AddDays(-1), birimMaliyetTl: 600m, altinHasFiyati: null);
        await db.SaveChangesAsync();
        await Sat(db, k, 1_500m);
        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = gid, Adet = 1m, BirimFiyatTl = 900m,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);
        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal(800m, D(r, "karTl"));                  // 500 + 300
        Assert.Equal("TAM", S(r, "hasKarDurumu"));          // gümüş has bacağına girmez
        Assert.Equal(0, I(r, "hasMaliyetiEksikSatisAdedi"));
        Assert.Equal(0, I(r, "yalnizHasMaliyetiEksikAdedi"));
        Assert.Equal(0.2197m, D(r, "hasilatHas"));
        Assert.Equal(0.0732m, D(r, "karHas"));
    }

    // NULL ADET: adedi null hareket + satırsız → bilinmeyen (0 adet karşılanmış sayılmaz değil: satırı yoksa dışarıda)
    [Fact]
    public void MS13_Satirsiz_ve_null_adet_has_bilinen_disi()
    {
        var h = new (string, decimal?)[] { ("a", null), ("b", 1m) };
        var t = new (string?, decimal, decimal?, decimal?)[] { ("b", 1m, null, 0.1465m) };
        // a: hiç satırı yok → bilinmiyor; b: TL maliyeti null → has de bilinmiyor sayılır
        Assert.Empty(KarZararServisi.HasMaliyetiBilinenler(h, t));
    }
}
