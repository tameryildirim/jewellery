using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Maliyet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KAR-RAPOR-2 — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT LİTERALDİR. Paketin
// iddiaları: (1) özet kartları toplanabilir — bilinenHasılat − maliyet = kâr,
// hasılat − eksik = bilinenHasılat (yuvarlanmış bileşenler, AwayFromZero);
// (2) maliyeti hiç bilinmiyorsa maliyet null ("0 ₺" değil); (3) stok yaşı kova
// hasGram'ına gümüş gramı karışmaz. Bu dosya o yolların sınırlarını sınar.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class KARRAPOR2SinirTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static DateOnly Bugun => DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));

    private static System.Text.Json.JsonElement J(object o) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o)).RootElement;

    private static decimal D(System.Text.Json.JsonElement e, string ad) => e.GetProperty(ad).GetDecimal();
    private static bool Bos(System.Text.Json.JsonElement e, string ad) =>
        e.GetProperty(ad).ValueKind == System.Text.Json.JsonValueKind.Null;

    private static Task Sat(KuyumcuDbContext db, Kiraci k, decimal adet, decimal birim) =>
        Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = adet, BirimFiyatTl = birim,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

    private static async Task Parti(KuyumcuDbContext db, Kiraci k, decimal adet, decimal? birimTl)
    {
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId,
            PartiKaynagi.SayimFazla, adet, k.UrunHasGrami, DateTime.UtcNow.AddDays(-1),
            birimMaliyetTl: birimTl);
        await db.SaveChangesAsync();
    }

    private Kiraci Kur(KuyumcuDbContext kurulum, string ad) =>
        TestKurulum.KiraciKur(kurulum, "PAKETKAR-RAPOR-2-TEST D " + ad);

    // ── SIFIR (KISMI): bilinen satış maliyetine eşit → kâr 0 (null DEĞİL), maliyet 1.000 (null DEĞİL)
    [Fact]
    public async Task K2S01_Sifir_kar_KISMI_maliyet_null_degil()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "sifir");
        using var db = vt.Olustur(k);
        await Parti(db, k, 1m, 1_000m);
        await Sat(db, k, 1m, 1_000m);
        await Sat(db, k, 1m, 777m);       // parti bitti → maliyet bilinmiyor

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("KISMI", r.GetProperty("karDurumu").GetString());
        Assert.Equal(1_777m, D(r, "hasilatTl"));
        Assert.Equal(1_000m, D(r, "bilinenHasilatTl"));
        Assert.Equal(777m, D(r, "maliyetiEksikTutarTl"));
        Assert.Equal(1_000m, D(r, "maliyetTl"));
        Assert.Equal(0m, D(r, "karTl"));
    }

    // ── EKSİ (KISMI): bilinen satış maliyetin altında → kâr −500,00; maliyet 2.000,004 → 2.000,00
    [Fact]
    public async Task K2S02_Eksi_kar_KISMI_isaret_ve_toplanabilirlik()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "eksi");
        using var db = vt.Olustur(k);
        await Parti(db, k, 1m, 2_000.004m);
        await Sat(db, k, 1m, 1_500m);
        await Sat(db, k, 1m, 300.30m);

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("KISMI", r.GetProperty("karDurumu").GetString());
        Assert.Equal(1_800.30m, D(r, "hasilatTl"));
        Assert.Equal(1_500m, D(r, "bilinenHasilatTl"));
        Assert.Equal(2_000.00m, D(r, "maliyetTl"));
        Assert.Equal(-500.00m, D(r, "karTl"));
        Assert.Equal(-500.00m, D(r, "donemKariTl"));
    }

    // ── KURUŞ 0,005: maliyet 1.000,005 → 1.000,01 (AwayFromZero; ToEven 1.000,00 verirdi) → kâr 499,99
    [Fact]
    public async Task K2S03_Kurus_0_005_maliyet_AwayFromZero_kar_gorunen_farktir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "kurus");
        using var db = vt.Olustur(k);
        await Parti(db, k, 1m, 1_000.005m);
        await Sat(db, k, 1m, 1_500m);

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("TAM", r.GetProperty("karDurumu").GetString());
        Assert.Equal(1_000.01m, D(r, "maliyetTl"));
        Assert.Equal(499.99m, D(r, "karTl"));

        var u = J(await new KarZararServisi(db).UrunBazli(k.Islem, Bugun, Bugun, null, 50)).GetProperty("satirlar")[0];
        Assert.Equal(1_000.01m, D(u, "maliyetTl"));
        Assert.Equal(499.99m, D(u, "karTl"));
    }

    // ── KURUŞ toplamda TEK yuvarlama: 2 × 100,0025 = 200,005 → 200,01 (kalem kalem yuvarlama 200,00 verirdi)
    [Fact]
    public async Task K2S04_Kurus_toplam_tek_kez_yuvarlanir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "kurus-toplam");
        using var db = vt.Olustur(k);
        await Parti(db, k, 1m, 100.0025m);
        await Parti(db, k, 1m, 100.0025m);
        await Sat(db, k, 2m, 300m);

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal(600m, D(r, "hasilatTl"));
        Assert.Equal(200.01m, D(r, "maliyetTl"));
        Assert.Equal(399.99m, D(r, "karTl"));
    }

    // ── HAS 4/6 HANE: gümüş partisi 3 × 0,0001 g → kova hasGram'a KATILMAZ, kırılımda 0,0003 g
    [Fact]
    public async Task K2S05_Stok_yasi_gumus_0_0001_has_toplamina_katilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "has");
        using var db = vt.Olustur(k);
        var gumusId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Urunler.Add(new Urun
        {
            Id = gumusId, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"G-{gumusId[..8]}", Ad = "Gümüş tel",
            AyarId = "G925", Milyem = 925m, GramAgirlik = 0.0001m, TasAgirlik = 0m, HasGrami = 0.0001m,
            IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        await db.SaveChangesAsync();
        var motor = new MaliyetMotoru(db);
        motor.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.SayimFazla, 1m, 1.2345m, now.AddDays(-1));
        motor.PartiAc(k.TenantId, k.SubeId, gumusId, PartiKaynagi.SayimFazla, 3m, 0.0001m, now.AddDays(-1));
        await db.SaveChangesAsync();

        var servis = new Kuyumcu.Api.Modules.Rapor.StokYaslandirmaServisi(db,
            new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor()));
        var r = J(await servis.Yaslandirma(k.Islem, null));
        var kova = r.GetProperty("kovalar")[0];
        Assert.Equal(1.2345m, D(kova, "hasGram"));
        Assert.Equal(4m, D(kova, "adet"));
        var madenler = kova.GetProperty("madenKirilimi").EnumerateArray().ToList();
        Assert.Equal(0.0003m, madenler.Single(m => m.GetProperty("maden").GetString() == "GUMUS")
                                      .GetProperty("toplamGram").GetDecimal());
        Assert.Equal(1.2345m, D(r, "altinHasGram"));
    }

    // ── ÇOK BÜYÜK: maliyet 500.000.000,005 → 500.000.000,01; satış 999.999.999,99 → kâr 499.999.999,98
    [Fact]
    public async Task K2S06_Cok_buyuk_tutar_toplanabilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "buyuk");
        using var db = vt.Olustur(k);
        await Parti(db, k, 1m, 500_000_000.005m);
        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 999_999_999.99m,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId,
            Masak = new Kuyumcu.Api.Modules.Masak.MasakBilgiDto
            {
                BelgeTuru = "PASAPORT", BelgeNo = "U7654321", AdSoyad = "Jane Doe", Uyruk = "FRA",
                DogumTarihi = new DateOnly(1981, 2, 2), Adres = "Lyon",
                IslemAmaci = "Ticari", Aciklama = "Sinir testi icin buyuk tutarli ticari satis islemi",
                FonKaynagi = "Ticari gelir", EkBelgeReferansi = "EK-2",
            },
        }, k.Islem);

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal(999_999_999.99m, D(r, "hasilatTl"));
        Assert.Equal(500_000_000.01m, D(r, "maliyetTl"));
        Assert.Equal(499_999_999.98m, D(r, "karTl"));
    }

    // ── BOŞ/NULL: satış yok → maliyet 0 (biliniyor, null DEĞİL); hepsi maliyetsiz → maliyet,
    //    köprü MALIYET ve önceki dönem maliyeti null (0 DEĞİL)
    [Fact]
    public async Task K2S07_Bos_donem_maliyet_sifir_hepsi_maliyetsiz_null_onceki_dahil()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "bos");
        using var db = vt.Olustur(k);

        var bos = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.False(Bos(bos, "maliyetTl"));
        Assert.Equal(0m, D(bos, "maliyetTl"));
        Assert.Equal(0m, D(bos, "hasilatTl"));

        await Parti(db, k, 1m, null);
        await Sat(db, k, 1m, 2_500m);
        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null, "ONCEKI_DONEM"));
        Assert.Equal("HESAPLANAMADI", r.GetProperty("karDurumu").GetString());
        Assert.True(Bos(r, "maliyetTl"));
        Assert.True(Bos(r, "karTl"));
        Assert.Equal(System.Text.Json.JsonValueKind.Null,
            r.GetProperty("karKoprusu").EnumerateArray()
             .Single(x => x.GetProperty("kod").GetString() == "MALIYET").GetProperty("tutarTl").ValueKind);
        // Önceki dönem satışsız → maliyet 0 (bilinir), null değil
        Assert.Equal(0m, D(r.GetProperty("onceki"), "maliyetTl"));
    }

    // ═══ Doğrulayıcı tur 3 (02.10 akşam) — ek sınırlar ═══════════════════════
    // ── KURUŞ 0,005 KISMI: maliyet 333,335 → 333,34; bilinen 500 − 333,34 = 166,66; eksik 100.
    //    Dönem, ürün satırı ve AYLIK ÖZET aynı kâr rakamını taşır (tek kural, tüm ekranlar).
    [Fact]
    public async Task K2S08_Kurus_0_005_KISMI_donem_urun_aylik_ayni_kar()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "kismi-kurus");
        using var db = vt.Olustur(k);
        await Parti(db, k, 1m, 333.335m);
        await Sat(db, k, 1m, 500m);
        await Sat(db, k, 1m, 100m);       // parti bitti → maliyet bilinmiyor

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("KISMI", r.GetProperty("karDurumu").GetString());
        Assert.Equal(600m, D(r, "hasilatTl"));
        Assert.Equal(500m, D(r, "bilinenHasilatTl"));
        Assert.Equal(100m, D(r, "maliyetiEksikTutarTl"));
        Assert.Equal(333.34m, D(r, "maliyetTl"));
        Assert.Equal(166.66m, D(r, "karTl"));
        Assert.Equal(166.66m, D(r, "donemKariTl"));

        var u = J(await new KarZararServisi(db).UrunBazli(k.Islem, Bugun, Bugun, null, 50)).GetProperty("satirlar")[0];
        Assert.Equal(333.34m, D(u, "maliyetTl"));
        Assert.Equal(166.66m, D(u, "karTl"));
        Assert.Equal(100m, D(u, "maliyetiEksikTutarTl"));

        var yerel = Zaman.UtcdenYerele(DateTime.UtcNow);
        var a = J(await TestKurulum.RaporServisi(db, new KarZararServisi(db)).AylikOzet(yerel.Year, yerel.Month, k.Islem));
        Assert.Equal(166.66m, D(a, "karTl"));
        Assert.Equal(100m, D(a, "maliyetiEksikTutarTl"));
    }

    // ── HAS 4 HANE: birim has maliyeti 1.000 / 3.000 = 0,333333 (6 hane) × 2 = 0,666666 → 0,6667.
    //    karHas = bilinenHasilatHas − 0,6667 (görünen bileşenlerin farkı), en çok 4 ondalık.
    [Fact]
    public async Task K2S09_Has_maliyet_4_haneye_AwayFromZero_kar_has_toplanabilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "has4");
        using var db = vt.Olustur(k);
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId,
            PartiKaynagi.SayimFazla, 2m, k.UrunHasGrami, DateTime.UtcNow.AddDays(-1),
            birimMaliyetTl: 1_000m, altinHasFiyati: 3_000m);
        await db.SaveChangesAsync();
        await Sat(db, k, 2m, 1_500m);

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("TAM", r.GetProperty("karDurumu").GetString());
        Assert.Equal(2_000m, D(r, "maliyetTl"));
        Assert.Equal(1_000m, D(r, "karTl"));
        Assert.Equal(0.6667m, D(r, "maliyetHas"));
        var kh = D(r, "karHas");
        Assert.Equal(D(r, "bilinenHasilatHas") - 0.6667m, kh);
        Assert.Equal(kh, Math.Round(kh, 4));
        Assert.Equal(D(r, "hasilatHas"), D(r, "bilinenHasilatHas"));   // TAM: iki küme aynı
        Assert.Equal(0m, D(r, "maliyetiEksikHasilatHas"));
    }

    // ── ÇOK BÜYÜK KISMI: bilinen 999.999.999,99 (maliyet 0,005 sınırında) + maliyetsiz 0,01
    [Fact]
    public async Task K2S10_Cok_buyuk_KISMI_toplanabilir_kurus_kurusuna()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "buyuk-kismi");
        using var db = vt.Olustur(k);
        await Parti(db, k, 1m, 123_456_789.125m);
        var masak = new Kuyumcu.Api.Modules.Masak.MasakBilgiDto
        {
            BelgeTuru = "PASAPORT", BelgeNo = "U7654322", AdSoyad = "Jane Roe", Uyruk = "FRA",
            DogumTarihi = new DateOnly(1980, 3, 3), Adres = "Paris",
            IslemAmaci = "Ticari", Aciklama = "Sinir testi icin buyuk tutarli ticari satis islemi 2",
            FonKaynagi = "Ticari gelir", EkBelgeReferansi = "EK-3",
        };
        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 999_999_999.99m,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId, Masak = masak,
        }, k.Islem);
        // maliyet bilinmiyor; aynı cariyle kümülatif MASAK eşiği aşıldığı için beyan bu satışta da verilir
        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 0.01m,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId, Masak = masak,
        }, k.Islem);

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("KISMI", r.GetProperty("karDurumu").GetString());
        Assert.Equal(1_000_000_000.00m, D(r, "hasilatTl"));
        Assert.Equal(999_999_999.99m, D(r, "bilinenHasilatTl"));
        Assert.Equal(0.01m, D(r, "maliyetiEksikTutarTl"));
        Assert.Equal(123_456_789.13m, D(r, "maliyetTl"));
        Assert.Equal(876_543_210.86m, D(r, "karTl"));
    }
}
