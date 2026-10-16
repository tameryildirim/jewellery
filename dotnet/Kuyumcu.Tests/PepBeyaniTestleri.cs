using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Cari;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 40-A — PEP BEYANI (PATCH /api/v1/cari/{id}/pep).
//
// ⚠ GERÇEK PATCH SEMANTİĞİ: gönderilmeyen alan "değişmedi" demektir. Kurul
//   engel 4'ün karşılığı: bu uç CariGuncelleDto'nun TAM gövdesiyle cari üzerine
//   YAZMAZ; ad/telefon/adres bu yoldan asla sıfırlanamaz.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class PepBeyaniTestleri(TestVeritabani vt)
{
    private static CariServisi Servis(KuyumcuDbContext db) =>
        new(db, NullLogger<CariServisi>.Instance, TestKurulum.Denetim(db));

    private static async Task<Cari> CariDoldur(KuyumcuDbContext db, Kiraci k)
    {
        var cari = await db.Cariler.FirstAsync(c => c.Id == k.CariId);
        cari.Telefon = "05321112233";
        cari.Adres = "Kuyumcular Çarşısı No:1";
        cari.VergiNo = "1234567890";
        await db.SaveChangesAsync();
        return cari;
    }

    [Fact]
    public async Task Beyan_true_gorev_bos_400_doner_ve_hicbir_alan_degismez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Pep-gorev-bos");
        using var db = vt.Olustur(k);
        await CariDoldur(db, k);

        var hata = await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).PepGuncelle(
            k.CariId, new CariPepDto { PepBeyani = true }, k.Islem));

        Assert.Contains("görev", hata.Message, StringComparison.OrdinalIgnoreCase);

        using var kontrol = vt.Olustur(k);
        var cari = await kontrol.Cariler.AsNoTracking().FirstAsync(c => c.Id == k.CariId);
        Assert.False(cari.PepBeyani);
        Assert.Null(cari.PepGorev);
        Assert.Null(cari.PepBeyanTarihi);
    }

    [Fact]
    public async Task Beyan_true_gorev_dolu_kaydedilir_ve_tarih_SUNUCUDA_damgalanir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Pep-kaydet");
        using var db = vt.Olustur(k);
        var once = DateTime.UtcNow.AddSeconds(-1);

        await Servis(db).PepGuncelle(k.CariId,
            new CariPepDto { PepBeyani = true, PepGorev = "Belediye meclis üyesi" }, k.Islem);

        using var kontrol = vt.Olustur(k);
        var cari = await kontrol.Cariler.AsNoTracking().FirstAsync(c => c.Id == k.CariId);
        Assert.True(cari.PepBeyani);
        Assert.Equal("Belediye meclis üyesi", cari.PepGorev);
        Assert.NotNull(cari.PepBeyanTarihi);
        Assert.True(cari.PepBeyanTarihi >= once, "Beyan tarihi sunucu saatiyle damgalanmalı.");
    }

    // ── Kurul engel 4: PATCH diğer alanları SİLMEZ ───────────────────────────

    [Fact]
    public async Task Pep_ucu_carinin_DIGER_alanlarina_dokunmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Pep-koruma");
        using var db = vt.Olustur(k);
        var onceki = await CariDoldur(db, k);
        var (ad, tel, adres, vergi) = (onceki.Ad, onceki.Telefon, onceki.Adres, onceki.VergiNo);

        using var db2 = vt.Olustur(k);
        await Servis(db2).PepGuncelle(k.CariId,
            new CariPepDto { PepBeyani = true, PepGorev = "Kamu görevlisi" }, k.Islem);

        using var kontrol = vt.Olustur(k);
        var sonra = await kontrol.Cariler.AsNoTracking().FirstAsync(c => c.Id == k.CariId);
        Assert.Equal(ad, sonra.Ad);
        Assert.Equal(tel, sonra.Telefon);
        Assert.Equal(adres, sonra.Adres);
        Assert.Equal(vergi, sonra.VergiNo);
    }

    [Fact]
    public async Task Gonderilmeyen_alan_DEGISMEDI_demektir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Pep-patch-semantik");
        using var db = vt.Olustur(k);
        await Servis(db).PepGuncelle(k.CariId,
            new CariPepDto { PepBeyani = true, PepGorev = "Üst düzey yönetici" }, k.Islem);

        // Yalnız görev gönderilir; beyan alanı GÖNDERİLMEZ → true kalmalı.
        using var db2 = vt.Olustur(k);
        await Servis(db2).PepGuncelle(k.CariId,
            new CariPepDto { PepGorev = "Yönetim kurulu üyesi" }, k.Islem);

        using var kontrol = vt.Olustur(k);
        var cari = await kontrol.Cariler.AsNoTracking().FirstAsync(c => c.Id == k.CariId);
        Assert.True(cari.PepBeyani);
        Assert.Equal("Yönetim kurulu üyesi", cari.PepGorev);
    }

    [Fact]
    public async Task Beyan_geri_alininca_gorev_ve_tarih_de_temizlenir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Pep-geri-al");
        using var db = vt.Olustur(k);
        await Servis(db).PepGuncelle(k.CariId,
            new CariPepDto { PepBeyani = true, PepGorev = "Kamu görevlisi" }, k.Islem);

        using var db2 = vt.Olustur(k);
        await Servis(db2).PepGuncelle(k.CariId, new CariPepDto { PepBeyani = false }, k.Islem);

        using var kontrol = vt.Olustur(k);
        var cari = await kontrol.Cariler.AsNoTracking().FirstAsync(c => c.Id == k.CariId);
        Assert.False(cari.PepBeyani);
        Assert.Null(cari.PepGorev);
        Assert.Null(cari.PepBeyanTarihi);
    }

    [Fact]
    public async Task Yabanci_kiracinin_carisine_PEP_yazilamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "Pep-kiraci-A");
        var b = TestKurulum.KiraciKur(kurulum, "Pep-kiraci-B");
        using var db = vt.Olustur(a);

        await Assert.ThrowsAsync<NotFoundException>(() => Servis(db).PepGuncelle(
            b.CariId, new CariPepDto { PepBeyani = true, PepGorev = "X" }, a.Islem));

        using var sistem = vt.OlusturSistem();
        Assert.False((await sistem.Cariler.AsNoTracking().FirstAsync(c => c.Id == b.CariId)).PepBeyani);
    }
}
