using Kuyumcu.Api.Common;
using Microsoft.Extensions.Logging.Abstractions;
using MEmanet = Kuyumcu.Api.Modules.Emanet;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// K1 — Kiracılar arası yazma (Mehmet/Güvenlik: KRİTİK, veto konusu).
// Denetimde kanıtlanan açık: B işletmesi, A'nın kasaId/cariId değerlerini
// kullanarak satış gönderip A'nın kasasına para, carisine borç yazdı.
// Kural: yabancı kimlik kullanıldığında işlem REDDEDİLİR ve yan etki SIFIRDIR.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class KiraciIzolasyonTestleri(TestVeritabani vt)
{
    private MSatis.SatisServisi Satis(Kuyumcu.Api.Data.KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    [Fact]
    public async Task Satis_YabanciKasayaParaYazamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "A işletmesi");
        var b = TestKurulum.KiraciKur(kurulum, "B işletmesi");
        using var db = vt.Olustur(b);   // servis B kiracisi adina calisir

        var dto = new MSatis.SatisOlusturDto
        {
            UrunId = b.UrunId, Adet = 1m, BirimFiyatTl = 99_999m,
            OdemeTuru = "NAKIT", KasaId = a.KasaId, KurSnapshotId = b.KurId,
        };

        await Assert.ThrowsAnyAsync<ApiException>(() => Satis(db).Satis(dto, b.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, a.KasaId));
    }

    [Fact]
    public async Task Satis_YabanciCariyeBorcYazamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "A işletmesi");
        var b = TestKurulum.KiraciKur(kurulum, "B işletmesi");
        using var db = vt.Olustur(b);   // servis B kiracisi adina calisir

        var dto = new MSatis.SatisOlusturDto
        {
            CariId = a.CariId, UrunId = b.UrunId, Adet = 1m, BirimFiyatTl = 77_777m,
            OdemeTuru = "VERESIYE", KurSnapshotId = b.KurId,
        };

        await Assert.ThrowsAnyAsync<ApiException>(() => Satis(db).Satis(dto, b.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, a.CariId).Tl);
    }

    [Fact]
    public async Task Alis_YabanciCariyeBorcYazamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "A işletmesi");
        var b = TestKurulum.KiraciKur(kurulum, "B işletmesi");
        using var db = vt.Olustur(b);   // servis B kiracisi adina calisir

        var dto = new MSatis.AlisOlusturDto
        {
            CariId = a.CariId, UrunId = b.UrunId, Adet = 1m, BirimFiyatTl = 50_000m,
            KurSnapshotId = b.KurId,
        };

        await Assert.ThrowsAnyAsync<ApiException>(() => Satis(db).Alis(dto, b.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, a.CariId).Tl);
    }

    [Fact]
    public async Task HurdaAlis_YabanciKasadanParaCikaramaz()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "A işletmesi");
        var b = TestKurulum.KiraciKur(kurulum, "B işletmesi");
        using var db = vt.Olustur(b);   // servis B kiracisi adina calisir

        var dto = new MSatis.HurdaAlisDto
        {
            AyarId = "K22", GramAgirlik = 7.9m, BirimFiyatTlHasGram = 1_000m,
            KasaId = a.KasaId, KurSnapshotId = b.KurId,
            Satici = TestKurulum.HurdaSatici(),
        };

        await Assert.ThrowsAnyAsync<ApiException>(() => Satis(db).HurdaAlis(dto, b.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, a.KasaId));
    }

    [Fact]
    public async Task Transfer_YabanciSubeyeStokGonderemez()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "A işletmesi");
        var b = TestKurulum.KiraciKur(kurulum, "B işletmesi");
        using var db = vt.Olustur(b);   // servis B kiracisi adina calisir

        var dto = new MSatis.TransferDto
        {
            KaynakSubeId = b.SubeId, HedefSubeId = a.SubeId, UrunId = b.UrunId,
            Adet = 5m, KurSnapshotId = b.KurId,
        };

        await Assert.ThrowsAnyAsync<ApiException>(() => Satis(db).Transfer(dto, b.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.False(kontrol.Stoklar.Any(s => s.SubeId == a.SubeId && s.TenantId == b.TenantId));
    }

    [Fact]
    public async Task Emanet_YabanciCariyeEmanetYazamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "A işletmesi");
        var b = TestKurulum.KiraciKur(kurulum, "B işletmesi");
        using var db = vt.Olustur(b);   // servis B kiracisi adina calisir

        var servis = new MEmanet.EmanetServisi(db, NullLogger<MEmanet.EmanetServisi>.Instance);
        var dto = new MEmanet.EmanetGirisDto
        {
            CariId = a.CariId, AyarId = "K22", GramAgirlik = 10m, KurSnapshotId = b.KurId,
        };

        await Assert.ThrowsAnyAsync<ApiException>(() => servis.Giris(dto, b.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, a.CariId).Has);
    }

    [Fact]
    public async Task Satis_YabanciKurSnapshotIleIslemYapamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "A işletmesi");
        var b = TestKurulum.KiraciKur(kurulum, "B işletmesi");
        using var db = vt.Olustur(b);   // servis B kiracisi adina calisir

        var dto = new MSatis.SatisOlusturDto
        {
            UrunId = b.UrunId, Adet = 1m, BirimFiyatTl = 1_000m,
            OdemeTuru = "NAKIT", KasaId = b.KasaId, KurSnapshotId = a.KurId,
        };

        await Assert.ThrowsAnyAsync<ApiException>(() => Satis(db).Satis(dto, b.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.False(kontrol.Hareketler.Any(h => h.KurSnapshotId == a.KurId && h.TenantId == b.TenantId));
    }
}
