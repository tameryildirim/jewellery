using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// MANUEL DÜZELTME FİŞİ — paket 33. Gerekçe zorunlu (≥ 20), deftere CARI_DUZELTME,
// bakiye deltası ile birebir, kredi limiti borç artışında işler, para izi YOK.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class CariDuzeltmeTestleri(TestVeritabani vt)
{
    private static MCari.CariServisi Servis(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));

    private static MCari.CariDuzeltmeDto Dto(decimal tl, decimal has, string gerekce, Kiraci k) =>
        new() { TlDelta = tl, HasDelta = has, Gerekce = gerekce, KurSnapshotId = k.KurId };

    private const string Gerekce = "Eski sistemden devir farkı — mutabakat 2026-09";

    [Fact]
    public async Task Duzeltme_deftere_CARI_DUZELTME_yazar_ve_bakiyeyi_delta_kadar_degistirir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Düzeltme temel");
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).Duzeltme(k.CariId, Dto(1_250m, -0.5m, Gerekce, k), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var b = kontrol.CariBakiyeler.AsNoTracking().Single(x => x.CariId == k.CariId);
        Assert.Equal(1_250m, b.TlBakiye); Assert.Equal(-0.5m, b.HasGramBakiye);
        var h = kontrol.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "CARI_DUZELTME");
        Assert.Equal(1_250m, h.TlTutari); Assert.Equal(0.5m, h.HasGrami);        // has işareti veresiye sözleşmesiyle aynı (bakiye − → hareket +)
        Assert.Contains(Gerekce, h.Aciklama);
        Assert.Equal(0m, h.NakitTutar); Assert.Equal(0m, h.VeresiyeTutar); Assert.Null(h.VadeTarihi);   // para/vade izi yok

        // ── DENETİM İZİ (ekip oyu Q1 = B, Mehmet'in şartı 4) ─────────────────
        // "Kim, ne zaman, NEDEN bakiye düzeltti" tek tablodan cevaplanabilmeli.
        // Otomatik yazma izi (DenetimYazmaDinleyicisi) Added varlıkta Detay'ı
        // BOŞ bırakır; gerekçeli satır bu yüzden ayrıca yazılır.
        var iz = Assert.Single(kontrol.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikTuru == "CariDuzeltme").ToList());
        Assert.Equal(DenetimEylemi.Ekleme, iz.Eylem);
        Assert.Equal(h.Id, iz.VarlikId);
        Assert.True(iz.Hassas);
        Assert.Contains(Gerekce, iz.Detay);
        Assert.Contains("tl=+1250", iz.Detay);
        Assert.Contains("has=-0.5", iz.Detay);

        // Otomatik iz de düşmüş olmalı (dinleyici bu yolda gerçekten ateşleniyor).
        Assert.Contains(kontrol.DenetimKayitlari.AsNoTracking().ToList(),
            d => d.TenantId == k.TenantId && d.VarlikTuru == "Hareket"
                 && d.Eylem == DenetimEylemi.Ekleme && d.VarlikId == h.Id);
    }

    [Fact]
    public async Task Gerekce_20_karakterden_kisa_REDDEDILIR_ve_sifir_delta_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Düzeltme gerekçe");
        using var db = vt.Olustur(k.TenantId);
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Duzeltme(k.CariId, Dto(100m, 0m, "kısa", k), k.Islem));
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Duzeltme(k.CariId, Dto(0m, 0m, Gerekce, k), k.Islem));
        using var kontrol = vt.OlusturSistem();
        Assert.False(kontrol.Hareketler.AsNoTracking().Any(x => x.TenantId == k.TenantId && x.Tur == "CARI_DUZELTME"));
    }

    /// <summary>Borcu ARTIRAN düzeltme kredi limitine tabidir; azaltan değildir.</summary>
    [Fact]
    public async Task Borc_artiran_duzeltme_kredi_limitine_TAKILIR_azaltan_takilmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Düzeltme limit");
        using (var s = vt.OlusturSistem()) { s.Cariler.Single(c => c.Id == k.CariId).KrediLimitiTl = 1_000m; s.SaveChanges(); }
        using var db = vt.Olustur(k.TenantId);
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Duzeltme(k.CariId, Dto(1_500m, 0m, Gerekce, k), k.Islem));
        await Servis(db).Duzeltme(k.CariId, Dto(-1_500m, 0m, Gerekce, k), k.Islem);   // azaltan serbest
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(-1_500m, kontrol.CariBakiyeler.AsNoTracking().Single(x => x.CariId == k.CariId).TlBakiye);
    }

    /// <summary>
    /// Sabit tür listesi GERÇEĞİ yansıtır (ekip oyu Q2). Liste kodda zorlanmıyor —
    /// tek işlevi belge olmak; yalan söylerse denetçi/rapor geliştiricisi bu
    /// türleri "geçersiz" sanıp yanlış süzer. Bu test listeyi gerçeğe pimler.
    /// </summary>
    [Fact]
    public void HareketTurleri_listesi_deftere_YAZILAN_turleri_icerir()
    {
        Assert.Contains("CARI_DUZELTME", Sabitler.HareketTurleri);
        Assert.Contains("SATIS_IADE", Sabitler.HareketTurleri);   // SatisModulu.cs
        Assert.Contains("ALIS_IADE", Sabitler.HareketTurleri);    // SatisModulu.cs
    }

    /// <summary>Vade takibi: düzeltmeyle doğan borç vadesi bilinmeyendir — bugüne yazılmaz.</summary>
    [Fact]
    public async Task Duzeltme_borcu_vade_raporunda_VADESIZ_gorunur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Düzeltme vade");
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).Duzeltme(k.CariId, Dto(700m, 0m, Gerekce, k), k.Islem);
        using var kontrol = vt.OlusturSistem();
        var (borclar, vadesiz) = await Kuyumcu.Api.Modules.Vade.VadeKapisi.AcikBorclar(kontrol, k.TenantId, k.CariId, DateTime.UtcNow);
        Assert.Empty(borclar); Assert.Equal(700m, vadesiz);
    }
}
