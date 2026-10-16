using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MMasak = Kuyumcu.Api.Modules.Masak;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET MASAK-ESZAMAN-2 finansal sınır testleri.
// Hedef: PesinPencereKapisi.Denetle (kilit altı yineleme, pencere üst sınırı "şimdi").
// Eşikler ÇAĞRIDA SABİT verilir (kimlik 200.000 · tevsik 30.000 · pencere 60 dk) —
// beklenen değerler üretim kodundan hesaplanmaz. Önceki Peşin NAKİT satış: 10.000,00 TL.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MASAKESZAMAN2SinirTestleri(TestVeritabani vt)
{
    private const decimal Kimlik = 200_000m;
    private const decimal Had = 30_000m;
    private const int Dakika = 60;
    private const decimal Onceki = 10_000m;

    private async Task<(Kiraci K, string Pesin, DateTime SatisOncesi)> Kur(string ad)
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "PAKETMASAK-ESZAMAN-2-TEST DOG " + ad, stokAdet: 10m);
        db.SaveChanges();
        var pesin = (await MCari.SistemCarisi.GetirVeyaOlustur(db, TestKurulum.Denetim(db), k.Islem)).Id;
        var once = DateTime.UtcNow.AddSeconds(-2);
        await new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance).Satis(new MSatis.SatisOlusturDto
        {
            CariId = pesin, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = Onceki,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
        return (k, pesin, once);
    }

    private async Task<Exception?> Denetle(Kiraci k, string pesin, decimal tutar, bool? beyan = null,
        DateTime? an = null, bool kilitAltinda = true, decimal kimlik = 1_000_000_000m, decimal had = Had)
    {
        using var db = vt.OlusturSistem();
        try
        {
            await MMasak.PesinPencereKapisi.Denetle(db, k.TenantId, pesin, k.Islem, null, tutar, beyan,
                an ?? DateTime.UtcNow, kimlik, had, Dakika, kilitAltinda);
            return null;
        }
        catch (Exception e) { return e; }
    }

    private static bool Soru(Exception? e) =>
        e is BadRequestException b && AlanIsareti.Kod(b) == MMasak.PesinPencereKapisi.SoruKodu;

    [Fact]
    public async Task S01_sifir_tutar_soru_yok()
    {
        var (k, p, _) = await Kur("S01");
        Assert.Null(await Denetle(k, p, 0m));
    }

    [Fact]
    public async Task S02_nakit_toplami_tam_hadde_esit_soru_yok_bir_kurus_fazlasi_soru()
    {
        var (k, p, _) = await Kur("S02");
        Assert.Null(await Denetle(k, p, 20_000.00m));          // 10.000 + 20.000 = 30.000 → had AŞILMAZ (>)
        Assert.True(Soru(await Denetle(k, p, 20_000.01m)));    // 30.000,01 > 30.000
    }

    [Fact]
    public async Task S03_yarim_kurus_sinirinda_yuvarlama_ile_gizlenmez()
    {
        var (k, p, _) = await Kur("S03");
        Assert.True(Soru(await Denetle(k, p, 20_000.005m)));   // 30.000,005 > 30.000 — kuruşa yuvarlanıp kaçmaz
    }

    [Fact]
    public async Task S04_eksi_tutar_mutlak_degerle_olculur()
    {
        var (k, p, _) = await Kur("S04");
        Assert.True(Soru(await Denetle(k, p, -20_000.01m)));
        Assert.Null(await Denetle(k, p, -20_000.00m));
    }

    [Fact]
    public async Task S05_kimlik_esigi_esitlikte_soru_altinda_yok()
    {
        var (k, p, _) = await Kur("S05");
        Assert.True(Soru(await Denetle(k, p, 190_000.00m, kimlik: Kimlik, had: 1_000_000_000m)));   // 200.000 >= 200.000
        Assert.Null(await Denetle(k, p, 189_999.99m, kimlik: Kimlik, had: 1_000_000_000m));
    }

    [Fact]
    public async Task S06_cok_buyuk_tutar_soru_ve_ayni_kisi_reddi()
    {
        var (k, p, _) = await Kur("S06");
        Assert.True(Soru(await Denetle(k, p, 999_999_999.99m)));
        var e = await Denetle(k, p, 999_999_999.99m, beyan: true);
        Assert.True(e is BadRequestException b && b.Message == MMasak.PesinPencereKapisi.AyniKisiMesaji);
    }

    [Fact]
    public async Task S07_kilit_altinda_ust_sinir_simdi_an_dan_sonraki_satiri_gorur()
    {
        var (k, p, once) = await Kur("S07");
        // an = satıştan ÖNCE: Faz 1 (kilitAltinda=false) satırı görmez → soru yok;
        // kilit altında üst sınır "şimdi" → satır görülür → soru.
        Assert.Null(await Denetle(k, p, 25_000m, an: once, kilitAltinda: false));
        Assert.True(Soru(await Denetle(k, p, 25_000m, an: once, kilitAltinda: true)));
    }

    [Fact]
    public async Task S08_baska_kisi_beyani_kilit_altinda_iki_kez_tek_iz()
    {
        var (k, p, _) = await Kur("S08");
        using var db = vt.OlusturSistem();
        var an = DateTime.UtcNow;
        await MMasak.PesinPencereKapisi.Denetle(db, k.TenantId, p, k.Islem, null, 25_000m, false, an, Kimlik, Had, Dakika);
        await MMasak.PesinPencereKapisi.Denetle(db, k.TenantId, p, k.Islem, null, 25_000m, false, an, Kimlik, Had, Dakika, kilitAltinda: true);
        var izler = db.ChangeTracker.Entries<DenetimKaydi>()
            .Where(e => e.State == EntityState.Added && e.Entity.VarlikTuru == MMasak.PesinPencereKapisi.DenetimTuru).ToList();
        Assert.Single(izler);
        Assert.Contains("bu işlem 25000.00 TL", izler[0].Entity.Detay!);
        Assert.Contains("önceki nakit 10000.00 TL", izler[0].Entity.Detay!);
    }
}
