using Kuyumcu.Api.Common;
using Microsoft.Extensions.Logging.Abstractions;
using MKur = Kuyumcu.Api.Modules.Kur;
using MUrun = Kuyumcu.Api.Modules.Urun;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// Parite (sarrafiye kalemi) sözlüğü.
//
// DURUM (Kurucu kararı, 2026-09-11): parite fiyatının ürüne/işleme YANSITILMASI
// GERİ ALINDI — kaynağın kotasyonu toptancının fiyatıdır, dükkânın satış fiyatı
// değildir ve marj alanı yoktur. Ekranlarda parite fiyatı kullanılmıyor, ada
// göre otomatik eşleşme kapalı.
//
// Bu testler UYKUDA olan altyapıyı korur: kanonik kod sözlüğü, fiyat birimi
// (ADET/GRAM/HAS_GRAM) ve tahmin fonksiyonunun doğruluğu. Marj + alış/satış
// modeli gelince (Faz 1) yeniden devreye alınacak.
// Kanonik kod kaynaktan bağımsızdır: Altınkaynak "PY", Ankara "YENI-YARIM",
// Truncgil "yarim-altin" → hepsi YARIM_YENI.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class PariteTahminTestleri
{
    [Theory]
    [InlineData("Yeni Yarım Altın", "YARIM_YENI")]
    [InlineData("Eski Yarım", "YARIM_ESKI")]
    [InlineData("Çeyrek Altın (Yeni Tarihli)", "CEYREK_YENI")]
    [InlineData("Eski Çeyrek", "CEYREK_ESKI")]
    [InlineData("Teklik Altın", "TAM_YENI")]
    [InlineData("Eski Teklik", "TAM_ESKI")]
    [InlineData("Ata Lira", "ATA_YENI")]
    [InlineData("Eski Ata Lira", "ATA_ESKI")]
    [InlineData("Reşat Altın", "RESAT")]
    [InlineData("Hamit Altın", "HAMIT")]
    [InlineData("Gremse Altın", "GREMSE_YENI")]
    [InlineData("Eski Gremse", "GREMSE_ESKI")]
    [InlineData("Ata Beşli", "BESLI")]
    [InlineData("Külçe Altın 24 Ayar (10 g)", "KULCE")]
    [InlineData("Gram Altın 24 Ayar (1 g)", "GRAM")]
    public void UrunAdindanPariteTahminEdilir(string ad, string beklenen)
    {
        Assert.Equal(beklenen, MKur.PariteKodlari.Tahmin(ad));
    }

    [Theory]
    [InlineData("14 Ayar Taşlı Kolye")]
    [InlineData("18 Ayar Pırlanta Yüzük 0,30 ct")]
    [InlineData("925 Gümüş Zincir Kolye")]
    // Gram kotasyonlu kalemler (22 Ayar Bilezik, 14/18 Ayar, Hurda, Gümüş) BİLİNÇLİ
    // olarak otomatik eşleşmez: dükkânın kendi işçiliğini ezmemesi için elle seçilir.
    [InlineData("22 Ayar Burma Bilezik")]
    [InlineData("22 Ayar Hurda")]
    public void ImalatTakisiOtomatikPariteyeEslesmez(string ad)
    {
        Assert.Null(MKur.PariteKodlari.Tahmin(ad));
    }

    [Theory]
    [InlineData("HAS", "HAS_GRAM")]
    [InlineData("GRAM", "GRAM")]
    [InlineData("KULCE", "GRAM")]
    [InlineData("BILEZIK_22", "GRAM")]
    [InlineData("AYAR_14", "GRAM")]
    [InlineData("HURDA_22", "GRAM")]
    [InlineData("GUMUS", "GRAM")]
    [InlineData("CEYREK_YENI", "ADET")]
    [InlineData("YARIM_ESKI", "ADET")]
    [InlineData("RESAT", "ADET")]
    [InlineData("BESLI", "ADET")]
    public void ParitelerinFiyatBirimiDogru(string kod, string beklenenBirim)
    {
        Assert.Equal(beklenenBirim, MKur.PariteKodlari.Birim(kod));
    }

    [Fact]
    public void GramKotasyonuElleSecilebilir()
    {
        // Otomatik eşleşmiyor ama geçerli bir koddur — kullanıcı elle seçebilir
        Assert.True(MKur.PariteKodlari.Gecerli("BILEZIK_22"));
        Assert.True(MKur.PariteKodlari.Gecerli("HURDA_22"));
    }

    [Fact]
    public void ParitelerinTumuIcinGorunenAdVardir()
    {
        foreach (var kod in MKur.PariteKodlari.Adlar.Keys)
        {
            Assert.True(MKur.PariteKodlari.Gecerli(kod));
            Assert.False(string.IsNullOrWhiteSpace(MKur.PariteKodlari.Ad(kod)));
        }
        Assert.False(MKur.PariteKodlari.Gecerli("YOK_BOYLE_BIR_KOD"));
        Assert.False(MKur.PariteKodlari.Gecerli(null));
    }
}

[Collection("veritabani")]
public sealed class UrunPariteTestleri(TestVeritabani vt)
{
    private static MUrun.UrunOlusturDto Dto(string ad, string? parite = null, string tip = "SIKKE") => new()
    {
        Tip = tip, Kod = $"K-{Guid.NewGuid():N}"[..12], Ad = ad,
        AyarId = "K22", GramAgirlik = 3.5m, TasAgirlik = 0m, PariteKodu = parite,
    };

    [Fact]
    public async Task UrunEklenirkenOtomatikEslesmeYapilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Parite otomatik kapalı");
        using var db = vt.Olustur(k);
        var servis = new MUrun.UrunServisi(db);

        // Ad birebir "Yeni Yarım" paritesine uyuyor, ama otomatik eşleşme
        // bilinçli olarak KAPALI (bkz. UrunServisi.PariteCoz gerekçesi).
        await servis.Olustur(Dto("Yeni Yarım Altın"), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var eklenen = kontrol.Urunler.Single(u => u.TenantId == k.TenantId && u.Ad == "Yeni Yarım Altın");
        Assert.Null(eklenen.PariteKodu);
    }

    [Fact]
    public async Task ElleVerilenPariteKoduKorunur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Parite elle");
        using var db = vt.Olustur(k);
        var servis = new MUrun.UrunServisi(db);

        // Ad "çeyrek" diyor ama kullanıcı bilinçli olarak RESAT seçmiş
        await servis.Olustur(Dto("Çeyrek görünümlü özel ürün", parite: "RESAT"), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var urun = kontrol.Urunler.Single(u => u.TenantId == k.TenantId && u.Ad.StartsWith("Çeyrek görünümlü"));
        Assert.Equal("RESAT", urun.PariteKodu);
    }

    [Fact]
    public async Task GecersizPariteKoduReddedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Parite geçersiz");
        using var db = vt.Olustur(k);
        var servis = new MUrun.UrunServisi(db);

        await Assert.ThrowsAnyAsync<ApiException>(() =>
            servis.Olustur(Dto("Bir ürün", parite: "OLMAYAN_KOD"), k.Islem));
    }

    [Fact]
    public async Task ImalatTakisindaPariteBosKalir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Parite yok");
        using var db = vt.Olustur(k);
        var servis = new MUrun.UrunServisi(db);

        await servis.Olustur(Dto("14 Ayar Taşlı Kolye", tip: "ZIYNET"), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var urun = kontrol.Urunler.Single(u => u.TenantId == k.TenantId && u.Ad == "14 Ayar Taşlı Kolye");
        Assert.Null(urun.PariteKodu);
    }

    [Fact]
    public async Task PariteEslesmesiBosStringIleKaldirilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Parite kaldır");
        using var db = vt.Olustur(k);
        var servis = new MUrun.UrunServisi(db);

        await servis.Olustur(Dto("Yeni Yarım Altın", parite: "YARIM_YENI"), k.Islem);
        var id = db.Urunler.Single(u => u.TenantId == k.TenantId && u.Ad == "Yeni Yarım Altın").Id;
        Assert.Equal("YARIM_YENI", db.Urunler.Single(u => u.Id == id).PariteKodu);

        await servis.Guncelle(id, new MUrun.UrunGuncelleDto { PariteKodu = "" }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Null(kontrol.Urunler.Single(u => u.Id == id).PariteKodu);
    }
}
