using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Urun;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PIRLANTA 4C + SERTİFİKA PDF — paket 22 (PAKET-22-PIRLANTA-KARAR)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class PirlantaKapisiTestleri
{
    [Fact] public void Pirlantada_karat_ZORUNLU() =>
        Assert.Contains("karat", Assert.Throws<BadRequestException>(() => PirlantaKapisi.Dogrula("PIRLANTA", null, null, null, null, null, null)).Message);
    [Fact] public void Pirlanta_disi_tipe_4C_YAZILAMAZ() =>
        Assert.Throws<BadRequestException>(() => PirlantaKapisi.Dogrula("ZIYNET", 0.5m, null, null, null, null, null));
    [Fact] public void Listeler_KAPALI() {
        Assert.Throws<BadRequestException>(() => PirlantaKapisi.Dogrula("PIRLANTA", 0.5m, "A", null, null, null, null));
        Assert.Throws<BadRequestException>(() => PirlantaKapisi.Dogrula("PIRLANTA", 0.5m, null, "VS3", null, null, null));
        Assert.Throws<BadRequestException>(() => PirlantaKapisi.Dogrula("PIRLANTA", 0.5m, null, null, "SUPER", null, null));
        Assert.Throws<BadRequestException>(() => PirlantaKapisi.Dogrula("PIRLANTA", 0.5m, null, null, null, "XYZ", "1"));
        PirlantaKapisi.Dogrula("PIRLANTA", 0.52m, "G", "VS1", "EXCELLENT", "GIA", "123");
        PirlantaKapisi.Dogrula("PIRLANTA", 0.52m, null, null, null, "DIGER:Yerel Lab", "9");
    }
    [Fact] public void Sertifika_no_icin_kurum_ZORUNLU() =>
        Assert.Throws<BadRequestException>(() => PirlantaKapisi.Dogrula("PIRLANTA", 0.5m, null, null, null, null, "GIA-1"));
    [Fact] public void Pdf_imzasi_ve_boyut_denetlenir() {
        Assert.Throws<BadRequestException>(() => PirlantaKapisi.PdfDogrula([]));
        Assert.Throws<BadRequestException>(() => PirlantaKapisi.PdfDogrula("hello"u8.ToArray()));
        Assert.Throws<BadRequestException>(() => PirlantaKapisi.PdfDogrula(new byte[Belge.EnFazlaPdfBayt + 1]));
        PirlantaKapisi.PdfDogrula("%PDF-1.4 test"u8.ToArray());
    }
    [Fact] public void Ozet_metni() => Assert.Equal("0,52 ct · G · VS1 · EX", Kuyumcu.Core.Domain.Pirlanta.Ozet(0.52m, "G", "VS1", "EXCELLENT"));
}

[Collection("veritabani")]
public sealed class PirlantaBelgeTestleri(TestVeritabani vt)
{
    private static T Prop<T>(object o, string ad) => (T)o.GetType().GetProperties().Single(p => string.Equals(p.Name, ad, StringComparison.OrdinalIgnoreCase)).GetValue(o)!;
    private static string Pdf() => Convert.ToBase64String("%PDF-1.7 sertifika"u8.ToArray());

    [Fact]
    public async Task Pirlanta_urun_4C_ile_kaydedilir_karatsiz_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Pırlanta kart");
        using var db = vt.Olustur(k.TenantId);
        var s = new UrunServisi(db);
        await Assert.ThrowsAsync<BadRequestException>(() => s.Olustur(new UrunOlusturDto { Tip = "PIRLANTA", Kod = "P-0", Ad = "Tektaş", AyarId = "K18", GramAgirlik = 3m, TasAgirlik = 0.1m, TasKokeni = "DOGAL" }, k.Islem));
        var y = await s.Olustur(new UrunOlusturDto { Tip = "PIRLANTA", Kod = "P-1", Ad = "Tektaş", AyarId = "K18", GramAgirlik = 3m, TasAgirlik = 0.1m, TasKokeni = "DOGAL",
            PirlantaKarat = 0.52m, PirlantaRenk = "G", PirlantaBerraklik = "VS1", PirlantaKesim = "EXCELLENT", SertifikaKurumu = "GIA", SertifikaNo = "GIA-77" }, k.Islem);
        Assert.Equal(0.52m, Prop<decimal?>(y, "PirlantaKarat")); Assert.Equal("GIA", Prop<string>(y, "SertifikaKurumu"));
        await Assert.ThrowsAsync<BadRequestException>(() => s.Olustur(new UrunOlusturDto { Tip = "ZIYNET", Kod = "Z-1", Ad = "Bilezik", AyarId = "K22", GramAgirlik = 10m, PirlantaKarat = 0.3m }, k.Islem));
    }

    [Fact]
    public async Task Sertifika_PDF_yuklenir_listelenir_icerik_ve_sha256_dogru_baska_kiraci_GOREMEZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Pırlanta belge");
        var k2 = TestKurulum.KiraciKur(vt.OlusturSistem(), "Pırlanta yabancı");
        using var db = vt.Olustur(k.TenantId);
        var b = new BelgeServisi(db);
        var y = await b.Yukle(k.UrunId, new BelgeYukleDto { Ad = "GIA-77.pdf", IcerikBase64 = Pdf() }, k.Islem);
        var id = Prop<string>(y, "Id");
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData("%PDF-1.7 sertifika"u8.ToArray())).ToLowerInvariant(), Prop<string>(y, "Sha256"));
        Assert.Single(await b.Liste(k.UrunId, k.Islem));
        Assert.Equal("%PDF-1.7 sertifika", System.Text.Encoding.UTF8.GetString((await b.Icerik(id, k.Islem)).Icerik));

        await Assert.ThrowsAsync<BadRequestException>(() => b.Yukle(k.UrunId, new BelgeYukleDto { Ad = "x.pdf", IcerikBase64 = Convert.ToBase64String("PK zip"u8.ToArray()) }, k.Islem));
        await Assert.ThrowsAsync<BadRequestException>(() => b.Yukle(k.UrunId, new BelgeYukleDto { Ad = "x.pdf", IcerikBase64 = "***" }, k.Islem));

        using var db2 = vt.Olustur(k2.TenantId);
        await Assert.ThrowsAsync<NotFoundException>(() => new BelgeServisi(db2).Icerik(id, k2.Islem));
        await Assert.ThrowsAsync<NotFoundException>(() => new BelgeServisi(db2).Yukle(k.UrunId, new BelgeYukleDto { Ad = "y.pdf", IcerikBase64 = Pdf() }, k2.Islem));

        await b.PasifeAl(id, k.Islem);
        Assert.Empty(await b.Liste(k.UrunId, k.Islem));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, kontrol.Belgeler.Count(x => x.Id == id));   // silinmedi, pasif
    }
}
