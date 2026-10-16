using Kuyumcu.Api.Common;
using Kuyumcu.Api.Modules.Urun;
using Kuyumcu.Web.Pages;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI — PAKET P-URUN (K6, Y19, O13) sınır testleri.
// Ayar seçimi milyem → has gram → fiyat zincirini belirler; sınırlar burada.
// Has kuralı (üretim): Math.Round(net × milyem / 1000, 6, AwayFromZero), kolon decimal(15,6).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class PURUNSinirSafTestleri
{
    [Theory]
    [InlineData("22ayar bilezik", "K22")]          // boşluksuz yazım
    [InlineData("14k kolye", "K14")]               // küçük k bitişik
    [InlineData("122 ayar", null)]                 // 122 bir ayar değil (önünde rakam)
    [InlineData("22,5 ayar", null)]                // ondalık kesir — ayar sayılmaz
    [InlineData("18K/22K set", null)]              // iki farklı ayar → tahmin yok
    [InlineData("Ayar 22 bilezik", null)]          // sıra ters — tahmin yok (uyarı çıkmaz, kilit yine çalışır)
    [InlineData("8 kg kasa", null)]                // kg ayar değil
    [InlineData("   ", null)]                      // boşluk
    public void PURUN_SIN_S1_Addaki_ayar_sinirlari(string ad, string? beklenen) =>
        Assert.Equal(beklenen, Urunler.AdindakiAyar(ad));

    [Fact]
    public void PURUN_SIN_S2_Celiski_null_ad_ve_null_secimde_patlamaz()
    {
        Assert.Null(Urunler.AdAyarCelismesi(null, null, "tr"));
        Assert.Null(Urunler.AdAyarCelismesi(null, "K22", "tr"));
        Assert.Null(Urunler.AdAyarCelismesi("22 ayar", null, "tr"));
    }

    [Fact]
    public void PURUN_SIN_S3_Maden_degisimi_bilinmeyen_madende_bos()
    {
        Assert.Equal("", Urunler.MadeninAyariVarsayilansiz("K22", "YOKMADEN"));
        Assert.Equal("", Urunler.MadeninAyariVarsayilansiz("", ""));
    }

    [Fact]
    public void PURUN_SIN_S4_Liste_kisa_ayar_bos_ve_8_ayar()
    {
        Assert.Equal("8", Urunler.YildizAyarKisa("K8"));
        Assert.Equal("", Urunler.YildizAyarKisa(""));
    }
}

[Collection("veritabani")]
public sealed class PURUNSinirVeriTestleri(TestVeritabani vt)
{
    private async Task<Kuyumcu.Api.Data.Urun> Yaz(string ad, string ayar, decimal gram, string kod)
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETP-URUN-TEST SIN-" + kod);
        await new UrunServisi(vt.Olustur(k)).Olustur(new UrunOlusturDto
        {
            Tip = "ZIYNET", Kod = kod, Ad = ad, AyarId = ayar, GramAgirlik = gram, Birim = "ADET",
        }, k.Islem);
        return await vt.OlusturSistem().Urunler.AsNoTracking().SingleAsync(x => x.TenantId == k.TenantId && x.Kod == kod);
    }

    private async Task<BadRequestException> Reddet(string? ayar, decimal gram, string kod)
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETP-URUN-TEST SIN-" + kod);
        var h = await Assert.ThrowsAsync<BadRequestException>(() =>
            new UrunServisi(vt.Olustur(k)).Olustur(new UrunOlusturDto
            {
                Tip = "ZIYNET", Kod = kod, Ad = "22 Ayar Halka", AyarId = ayar!, GramAgirlik = gram, Birim = "ADET",
            }, k.Islem));
        Assert.Equal(0, await vt.OlusturSistem().Urunler.AsNoTracking().CountAsync(u => u.TenantId == k.TenantId && u.Kod == kod));
        return h;
    }

    [Fact]
    public async Task PURUN_SIN_D1_Has_4_hane_sinirinda_kaybolmaz()
    {
        // 0,0001 g × 916/1000 = 0,0000916 → 6 hane AwayFromZero = 0,000092 (sıfıra düşmez)
        var u = await Yaz("22 Ayar Mini", "K22", 0.0001m, "SIN-D1");
        Assert.Equal(0.000092m, u.HasGrami);
        Assert.Equal(916m, u.Milyem);
    }

    [Fact]
    public async Task PURUN_SIN_D2_18_ayar_secimi_750_ile_hesaplanir()
    {
        // K6 senaryosunun tersi: ad 22 diyor ama kullanıcı bilerek 18 seçti → 750 yazılır, sessiz düzeltme yok.
        var u = await Yaz("22 Ayar Halka Küpe", "K18", 3.40m, "SIN-D2");
        Assert.Equal("K18", u.AyarId);
        Assert.Equal(750m, u.Milyem);
        Assert.Equal(2.55m, u.HasGrami);
    }

    [Fact]
    public async Task PURUN_SIN_D3_Bilinmeyen_ayar_400_satir_yok() =>
        Assert.Contains("K99", (await Reddet("K99", 3.4m, "SIN-D3")).Message);

    [Fact]
    public async Task PURUN_SIN_D4_Bos_ayar_400_alan_ayarId() =>
        Assert.Equal("ayarId", AlanIsareti.Alan(await Reddet("", 3.4m, "SIN-D4")));

    // NOT (doğrulayıcı): servis içi yol (UrunServisi.Olustur) eksi gramı REDDETMİYOR — kapı yalnız
    // DTO [Range(0.0001,…)] özniteliğinde (controller yolu). Tek çağıran controller olduğu için
    // canlıda 400 döner (5297'de ölçüldü); P-URUN öncesinden kalan açık, raporda ORTA bulgu.
    // Bu test controller yolundaki kapıyı sabitler.
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void PURUN_SIN_D5_Eksi_ve_sifir_gram_DTO_kapisinda_reddedilir(int gram)
    {
        var dto = new UrunOlusturDto { Tip = "ZIYNET", Kod = "SIN-D5", Ad = "22 Ayar", AyarId = "K22", GramAgirlik = gram, Birim = "ADET" };
        var sonuc = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        Assert.False(System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            dto, new System.ComponentModel.DataAnnotations.ValidationContext(dto), sonuc, validateAllProperties: true));
        Assert.Contains(sonuc, r => r.MemberNames.Contains(nameof(UrunOlusturDto.GramAgirlik)));
    }
}
