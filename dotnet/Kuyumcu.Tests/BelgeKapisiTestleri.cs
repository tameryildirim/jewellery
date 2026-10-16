using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Urun;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// BELGE İÇERİK KAPISI — paket 4 Faz A [saf]
//
// İLKE: içerik türü BEYANA GÜVENİLMEZ. Karar yalnız magic byte'a göre verilir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class BelgeKapisiTestleri
{
    private static byte[] Pdf(int boyut = 32)
    {
        var b = new byte[boyut];
        "%PDF-1.7"u8.ToArray().CopyTo(b, 0);
        return b;
    }

    private static byte[] Jpeg(int boyut = 32)
    {
        var b = new byte[boyut];
        b[0] = 0xFF; b[1] = 0xD8; b[2] = 0xFF;
        return b;
    }

    private static byte[] Png(int boyut = 32)
    {
        var b = new byte[boyut];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(b, 0);
        return b;
    }

    [Fact]
    public void PDF_imzasi_taninir_mime_application_pdf_doner()
    {
        BelgeKapisi.IcerikDogrula(Pdf(), BelgeTuru.Sertifika, out var mime);
        Assert.Equal("application/pdf", mime);
    }

    [Fact]
    public void JPEG_imzasi_taninir()
    {
        BelgeKapisi.IcerikDogrula(Jpeg(), BelgeTuru.Gorsel, out var mime);
        Assert.Equal("image/jpeg", mime);
    }

    [Fact]
    public void PNG_imzasi_taninir()
    {
        BelgeKapisi.IcerikDogrula(Png(), BelgeTuru.Fotograf, out var mime);
        Assert.Equal("image/png", mime);
    }

    [Fact]
    public void Rastgele_baytlar_REDDEDILIR_ve_mesaj_imza_kelimesini_icerir()
    {
        var ex = Assert.Throws<BadRequestException>(
            () => BelgeKapisi.IcerikDogrula("rastgele veri"u8.ToArray(), BelgeTuru.Ek, out _));
        Assert.Contains("imza", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>⚠ Mehmet K4-4: SVG betik taşır — imza tablosunda YOKTUR.</summary>
    [Fact]
    public void SVG_KABUL_EDILMEZ()
    {
        var ex = Assert.Throws<BadRequestException>(
            () => BelgeKapisi.IcerikDogrula("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"u8.ToArray(), BelgeTuru.Gorsel, out _));
        Assert.Contains("imza", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SERTIFIKA_turune_JPEG_verilince_REDDEDILIR()
    {
        var ex = Assert.Throws<BadRequestException>(
            () => BelgeKapisi.IcerikDogrula(Jpeg(), BelgeTuru.Sertifika, out _));
        Assert.Contains("PDF", ex.Message);
    }

    [Fact]
    public void GORSEL_turune_PDF_verilince_REDDEDILIR()
    {
        var ex = Assert.Throws<BadRequestException>(
            () => BelgeKapisi.IcerikDogrula(Pdf(), BelgeTuru.Gorsel, out _));
        Assert.Contains("JPEG/PNG", ex.Message);
    }

    [Fact]
    public void EK_turu_hem_PDF_hem_gorsel_kabul_eder()
    {
        BelgeKapisi.IcerikDogrula(Pdf(), BelgeTuru.Ek, out var m1);
        BelgeKapisi.IcerikDogrula(Png(), BelgeTuru.Ek, out var m2);
        Assert.Equal("application/pdf", m1);
        Assert.Equal("image/png", m2);
    }

    [Fact]
    public void Bes_MB_bir_bayt_PDF_REDDEDILIR() =>
        Assert.Throws<BadRequestException>(
            () => BelgeKapisi.IcerikDogrula(Pdf(Belge.EnFazlaPdfBayt + 1), BelgeTuru.Sertifika, out _));

    [Fact]
    public void Iki_MB_bir_bayt_JPEG_REDDEDILIR()
    {
        var ex = Assert.Throws<BadRequestException>(
            () => BelgeKapisi.IcerikDogrula(Jpeg(Belge.EnFazlaGorselBayt + 1), BelgeTuru.Gorsel, out _));
        Assert.Contains("MB", ex.Message);
    }

    [Fact]
    public void Kapali_liste_disi_belge_turu_REDDEDILIR() =>
        Assert.Throws<BadRequestException>(() => BelgeKapisi.IcerikDogrula(Pdf(), "GARANTI", out _));

    [Fact]
    public void Bos_icerik_REDDEDILIR() =>
        Assert.Equal("Belge boş",
            Assert.Throws<BadRequestException>(() => BelgeKapisi.IcerikDogrula([], BelgeTuru.Ek, out _)).Message);

    /// <summary>Tavanlar yapılandırmadan gelir (ADR-0005) — sabit değildir.</summary>
    [Fact]
    public void Tavan_YAPILANDIRMADAN_okunur()
    {
        var dar = new BelgeAyarlari { EnFazlaPdfMb = 1, EnFazlaGorselMb = 1 };
        Assert.Throws<BadRequestException>(
            () => BelgeKapisi.IcerikDogrula(Pdf(2 * 1024 * 1024), BelgeTuru.Sertifika, out _, dar));
        BelgeKapisi.IcerikDogrula(Pdf(512 * 1024), BelgeTuru.Sertifika, out _, dar);
    }

    /// <summary>
    /// REGRESYON: PirlantaKapisi.PdfDogrula eski mesajını AYNEN üretir
    /// (A16 canlı kanıtı bu metne bakıyordu).
    /// </summary>
    [Fact]
    public void PirlantaKapisi_PdfDogrula_eski_mesajini_AYNEN_uretir()
    {
        Assert.Equal("Belge PDF değil (imza %PDF- bulunamadı)",
            Assert.Throws<BadRequestException>(() => PirlantaKapisi.PdfDogrula("hello"u8.ToArray())).Message);
        Assert.Equal("Belge boş",
            Assert.Throws<BadRequestException>(() => PirlantaKapisi.PdfDogrula([])).Message);
        Assert.Equal("Belge en fazla 5 MB olabilir",
            Assert.Throws<BadRequestException>(() => PirlantaKapisi.PdfDogrula(new byte[Belge.EnFazlaPdfBayt + 1])).Message);
    }
}
