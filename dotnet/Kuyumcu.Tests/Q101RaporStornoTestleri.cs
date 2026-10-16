using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q10-1 — STORNOLU ÖZET TARAMASI: RAPOR ÖZETLERİ [doğrudan servis + MySQL]
//
// /gider toplamındaki kalıp (storno çifti mutlak değerle ya da tek yönlü
// toplanınca rakam şişer) rapor özetlerinde de ÖLÇÜLDÜ:
//   • Aylık özet "cari tahsilatı"   TAHSILAT + stornosu → Σ|tl| = 2 × tutar
//   • Aylık özet "tahsilat" (kasaya giren TL) → gider stornosu (+) GİRİŞ sayılıyor,
//     tahsilat stornosu (-) sayılmadığı için hükümsüz tahsilat duruyordu
//   • POS/fatura mutabakatı (gün sonu) "tahsilat" → aynı kasa girişi kalıbı
// Düzeltme: mevcut StornoSuzgeci (paket 48 Faz A, çift dışlama — Mehmet koşulu)
// bu toplamlara da uygulanır; kasa_hareketleri için simetrik aşırı yükleme.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class Q101RaporStornoTestleri(TestVeritabani vt)
{
    private static object? Alan(object kaynak, string ad) =>
        kaynak.GetType().GetProperty(ad)?.GetValue(kaynak);

    private static object Alt(object kaynak, string ad) => Alan(kaynak, ad)!;

    private static Hareket Satir(Kiraci k, string tur, decimal tl, DateTime tarih, string? stornoKaynak = null)
        => new()
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = tur, Tarih = tarih,
            TlTutari = tl, HasGrami = 0m, Maden = "ALTIN", ParaBirimi = "TRY",
            KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId, Olusturuldu = DateTime.UtcNow,
            StornoKaynakId = stornoKaynak,
        };

    private static KasaHareket Kasa(Kiraci k, decimal tutar, DateTime tarih, string? stornoKaynak = null)
        => new()
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, KasaId = k.KasaId, Tutar = tutar, ParaBirimi = "TRY",
            OdemeTuru = "NAKIT", Tarih = tarih, Olusturuldu = DateTime.UtcNow, StornoKaynakId = stornoKaynak,
        };

    /// <summary>
    /// Tahsilat 1.500 + stornosu, ikinci tahsilat 400 → cari tahsilatı 400 (eskisi 3.400).
    /// MUTASYON HEDEFİ (MQ101-9): aylık sorgudan storno çifti süzgeci kaldırılırsa KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Q101_aylik_cari_tahsilati_storno_ciftini_saymaz()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETQ10-1-TEST aylik tahsilat");
        var bugun = Zaman.YerelBugun();
        var an = Zaman.YerelGunBasiUtc(bugun).AddHours(11);

        var t1 = Satir(k, "TAHSILAT", -1_500m, an);
        sistem.Hareketler.Add(t1);
        sistem.Hareketler.Add(Satir(k, "TAHSILAT", 1_500m, an.AddMinutes(5), t1.Id));
        sistem.Hareketler.Add(Satir(k, "TAHSILAT", -400m, an.AddMinutes(10)));
        await sistem.SaveChangesAsync();

        var o = await TestKurulum.RaporServisi(vt.Olustur(k)).AylikOzet(bugun.Year, bugun.Month, k.Islem);
        Assert.Equal(400m, Alan(Alt(o, "tahsilat"), "cariTahsilatiTl"));
    }

    /// <summary>
    /// Kasaya giren TL: tahsilat girişi 1.000 + stornosu (-1.000) ve gider 125,50 +
    /// stornosu (+125,50) → iki çift de hükümsüz; kalan tek giriş 300.
    /// MUTASYON HEDEFİ (MQ101-10): aylık kasa girişi sorgusundan süzgeç kaldırılırsa KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Q101_aylik_kasa_girisi_storno_ciftlerini_saymaz()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETQ10-1-TEST aylik kasa");
        var bugun = Zaman.YerelBugun();
        var an = Zaman.YerelGunBasiUtc(bugun).AddHours(11);
        var onceki = sistem.KasaHareketleri.Where(x => x.TenantId == k.TenantId && x.Tutar > 0m
            && x.Tarih >= Zaman.YerelGunBasiUtc(new DateOnly(bugun.Year, bugun.Month, 1))).Sum(x => (decimal?)x.Tutar) ?? 0m;

        var giris = Kasa(k, 1_000m, an);
        var gider = Kasa(k, -125.50m, an.AddMinutes(1));
        sistem.KasaHareketleri.AddRange(giris, gider,
            Kasa(k, -1_000m, an.AddMinutes(2), giris.Id),
            Kasa(k, 125.50m, an.AddMinutes(3), gider.Id),
            Kasa(k, 300m, an.AddMinutes(4)));
        await sistem.SaveChangesAsync();

        var o = await TestKurulum.RaporServisi(vt.Olustur(k)).AylikOzet(bugun.Year, bugun.Month, k.Islem);
        Assert.Equal(onceki + 300m, Alan(Alt(o, "tahsilat"), "toplamTl"));
    }

    /// <summary>
    /// Gün sonu (POS/fatura mutabakatı) NAKİT tahsilatı: aynı kalıp.
    /// MUTASYON HEDEFİ (MQ101-11): gün sonu kasa girişi sorgusundan süzgeç kaldırılırsa KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Q101_gun_sonu_tahsilati_storno_ciftlerini_saymaz()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETQ10-1-TEST gun sonu");
        var bugun = Zaman.YerelBugun();
        var an = Zaman.YerelGunBasiUtc(bugun).AddHours(11);
        var servis = TestKurulum.RaporServisi(vt.Olustur(k));
        var once = await servis.PosFaturaMutabakati(bugun.ToString("yyyy-MM-dd"), k.Islem);

        var giris = Kasa(k, 1_000m, an);
        var gider = Kasa(k, -125.50m, an.AddMinutes(1));
        sistem.KasaHareketleri.AddRange(giris, gider,
            Kasa(k, -1_000m, an.AddMinutes(2), giris.Id),
            Kasa(k, 125.50m, an.AddMinutes(3), gider.Id),
            Kasa(k, 300m, an.AddMinutes(4)));
        await sistem.SaveChangesAsync();

        var sonra = await TestKurulum.RaporServisi(vt.Olustur(k)).PosFaturaMutabakati(bugun.ToString("yyyy-MM-dd"), k.Islem);
        Assert.Equal(Nakit(once) + 300m, Nakit(sonra));
    }

    private static decimal Nakit(object mutabakat)
    {
        var j = System.Text.Json.JsonSerializer.SerializeToElement(mutabakat);
        return j.GetProperty("tahsilat").GetProperty("nakit").GetDecimal();
    }
}
