using Kuyumcu.Api.Data;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Modules.Fiyat;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// FİYAT ARALIK KAPISI — SERVİS YOLU (paket S-3c)
//
// NEDEN AYRI: DTO üzerindeki [Range] YALNIZ denetleyici yolunda çalışır. Kapı
// servise konmazsa, servisi doğrudan çağıran her yol (toplu içe aktarım,
// kurulum sihirbazı, ileride bir arka plan işi) sınırsız değeri yazabilir.
// Bu dosya kapının SERVİSTE durduğunu kanıtlar.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class S3cFiyatAralikServisTestleri(TestVeritabani vt)
{
    private static FiyatListesiDto Dto(decimal marj, decimal carpan, bool? onay = null) => new()
    {
        Ad = "Aralık denemesi", HasMarjYuzde = marj, IscilikCarpanYuzde = carpan,
        Varsayilan = true, RiskOnayi = onay,
    };

    private Kiraci Kur(string ad)
    {
        using var db = vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    /// <summary>M20-8 — sert sınır dışı marj SERVİSTE durur (DataAnnotations yok).</summary>
    [Fact]
    public async Task Servis_SERT_sinir_disi_marji_reddeder()
    {
        var k = Kur("S3c servis sert");
        using var db = vt.Olustur(k.TenantId);
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => new FiyatServisi(db).ListeOlustur(Dto(-500m, 100m, onay: true), k.Islem));
        Assert.Contains("Has marjı", hata.Message);
    }

    /// <summary>M20-9 — rahat aralık dışı çarpan SERVİSTE onay ister.</summary>
    [Fact]
    public async Task Servis_riskli_carpani_ONAYSIZ_reddeder_ONAYLA_kabul_eder()
    {
        var k = Kur("S3c servis risk");
        using (var db = vt.Olustur(k.TenantId))
        {
            var hata = await Assert.ThrowsAsync<BadRequestException>(
                () => new FiyatServisi(db).ListeOlustur(Dto(5m, 400m), k.Islem));
            Assert.Contains("İşçilik çarpanı", hata.Message);
        }
        using (var db2 = vt.Olustur(k.TenantId))
        {
            var liste = await new FiyatServisi(db2).ListeOlustur(Dto(5m, 400m, onay: true), k.Islem);
            Assert.Equal(400m, liste.IscilikCarpanYuzde);
        }
    }
}
