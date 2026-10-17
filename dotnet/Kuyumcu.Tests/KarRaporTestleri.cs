using System.Net;
using Bunit;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Maliyet;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KAR-RAPOR (02.10) — KÂR FAIL-CLOSED
//
// Bulgu: Rapor merkezi PRO'da "KÂR 195.069,79 ₺" aylık satış toplamına BİREBİR
// eşitti. Kök neden: KarZararServisi.Olc `karTl = hasilatTl − maliyetTl`; hasılat
// TÜM satışları taşırken maliyet yalnız maliyeti bilinen parti satırlarını
// topluyordu → maliyeti bilinmeyen satış kâra SIFIR maliyetle giriyordu.
// Kural: maliyeti bilinmeyen satış kâra GİRMEZ; ayrı "maliyeti eksik N satış, X ₺"
// satırı; hiçbiri bilinmiyorsa kâr null (HESAPLANAMADI).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class KarRaporDbTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static KarZararServisi Rapor(KuyumcuDbContext db) => new(db);

    private static DateOnly Bugun => DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));

    private static System.Text.Json.JsonElement J(object o) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o)).RootElement;

    private static decimal D(System.Text.Json.JsonElement e, string ad) => e.GetProperty(ad).GetDecimal();
    private static bool Bos(System.Text.Json.JsonElement e, string ad) =>
        e.GetProperty(ad).ValueKind == System.Text.Json.JsonValueKind.Null;

    private static Task Alis(KuyumcuDbContext db, Kiraci k, decimal adet, decimal birim) =>
        Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            UrunId = k.UrunId, Adet = adet, BirimFiyatTl = birim,
            KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

    private static async Task<string> Sat(KuyumcuDbContext db, Kiraci k, decimal adet, decimal birim)
    {
        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = adet, BirimFiyatTl = birim,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);
        return await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS")
            .OrderByDescending(h => h.Olusturuldu).ThenByDescending(h => h.Id)
            .Select(h => h.Id).FirstAsync();
    }

    // ── Senaryo 1: maliyeti TAM bilinen 3 satış → kâr = satış − maliyet (kuruşu kuruşuna)

    [Fact]
    public async Task KR01_Uc_satis_maliyeti_tam_bilinir_kar_kurusu_kurusuna()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KR01-tam");
        using var db = vt.Olustur(k);

        await Alis(db, k, 3m, 1_000.37m);
        await Sat(db, k, 1m, 1_500.11m);
        await Sat(db, k, 1m, 1_600.22m);
        await Sat(db, k, 1m, 1_700.33m);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));

        Assert.Equal(4_800.66m, D(r, "hasilatTl"));
        Assert.Equal(3_001.11m, D(r, "maliyetTl"));
        Assert.Equal(1_799.55m, D(r, "karTl"));
        Assert.Equal("TAM", r.GetProperty("karDurumu").GetString());
        Assert.Equal(0, r.GetProperty("maliyetiEksikSatisAdedi").GetInt32());
        Assert.Equal(0m, D(r, "maliyetiEksikTutarTl"));
        Assert.False(Bos(r, "karHas"));
    }

    // ── Senaryo 2: 1 satış maliyetsiz → kâr yalnız bilinenlerden + "maliyeti eksik" satırı

    [Fact]
    public async Task KR02_Bir_satis_maliyetsiz_kar_yalniz_bilinenlerden_eksik_ayri()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KR02-kismi");
        using var db = vt.Olustur(k);

        await Alis(db, k, 2m, 1_000m);
        await Sat(db, k, 1m, 1_500m);
        await Sat(db, k, 1m, 1_600m);
        await Sat(db, k, 1m, 1_700m);   // parti bitti → maliyeti BİLİNMİYOR

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));

        Assert.Equal(4_800m, D(r, "hasilatTl"));          // ciro TÜM satışlar
        Assert.Equal(3_100m, D(r, "bilinenHasilatTl"));
        Assert.Equal(2_000m, D(r, "maliyetTl"));
        Assert.Equal(1_100m, D(r, "karTl"));              // 4.800 − 2.000 = 2.800 OLMAZ
        Assert.Equal("KISMI", r.GetProperty("karDurumu").GetString());
        Assert.Equal(1, r.GetProperty("maliyetiEksikSatisAdedi").GetInt32());
        Assert.Equal(1_700m, D(r, "maliyetiEksikTutarTl"));
        Assert.Equal(1m, D(r, "maliyetiBilinmeyenAdet"));  // satırı hiç yazılmayan adet de sayılır
        Assert.Equal(Math.Round(1_100m / 3_100m * 100m, 2), D(r, "karMarjiYuzde"));

        // Köprü: işaretli kalemlerin toplamı dönem kârına eşit; eksik satış AYRI kalem.
        var kopru = r.GetProperty("karKoprusu").EnumerateArray().ToList();
        var eksik = kopru.Single(x => x.GetProperty("kod").GetString() == "MALIYETI_EKSIK_SATIS");
        Assert.Equal(-1_700m, eksik.GetProperty("tutarTl").GetDecimal());
        var toplam = kopru.Where(x => !x.GetProperty("araToplam").GetBoolean())
                          .Sum(x => x.GetProperty("tutarTl").GetDecimal());
        Assert.Equal(D(r, "donemKariTl"), toplam);
        Assert.Equal(1_100m, kopru.Single(x => x.GetProperty("kod").GetString() == "BRUT_KAR")
                                  .GetProperty("tutarTl").GetDecimal());
    }

    // ── Senaryo 3: tümü maliyetsiz → kâr GÖSTERİLMEZ (null), satış kâr diye dönmez

    [Fact]
    public async Task KR03_Tumu_maliyetsiz_kar_null_HESAPLANAMADI()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KR03-hicbiri");
        using var db = vt.Olustur(k);

        await Sat(db, k, 1m, 1_500m);
        await Sat(db, k, 1m, 2_500m);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));

        Assert.Equal(4_000m, D(r, "hasilatTl"));
        Assert.True(Bos(r, "karTl"));
        Assert.True(Bos(r, "karHas"));
        Assert.True(Bos(r, "donemKariTl"));
        Assert.True(Bos(r, "karMarjiYuzde"));
        Assert.Equal("HESAPLANAMADI", r.GetProperty("karDurumu").GetString());
        Assert.Equal(2, r.GetProperty("maliyetiEksikSatisAdedi").GetInt32());
        Assert.Equal(4_000m, D(r, "maliyetiEksikTutarTl"));
        var kopru = r.GetProperty("karKoprusu").EnumerateArray().ToList();
        Assert.Equal(System.Text.Json.JsonValueKind.Null,
            kopru.Single(x => x.GetProperty("kod").GetString() == "BRUT_KAR").GetProperty("tutarTl").ValueKind);
        Assert.Contains("kâra KATILMADI", r.GetProperty("uyari").GetString());
    }

    // ── Demo kök nedeni: açılış/sayım fazlası partisi (maliyet NULL) tüketilir

    [Fact]
    public async Task KR04_Maliyetsiz_acilis_partisinden_satis_kara_GIRMEZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KR04-devir");
        using var db = vt.Olustur(k);

        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId,
            PartiKaynagi.SayimFazla, 5m, k.UrunHasGrami, DateTime.UtcNow.AddDays(-1));
        await db.SaveChangesAsync();
        await Sat(db, k, 1m, 8_000m);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));

        Assert.True(Bos(r, "karTl"));                      // eskiden 8.000 = satış
        Assert.Equal("HESAPLANAMADI", r.GetProperty("karDurumu").GetString());
        Assert.Equal(1m, D(r, "maliyetiBilinmeyenAdet"));
    }

    // ── Sınır: tek satışın YARISI bilinen partiden → satışın tamamı eksik sayılır

    [Fact]
    public async Task KR05_Yarisi_bilinen_satis_TAMAMEN_eksik_sayilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KR05-yarim");
        using var db = vt.Olustur(k);

        await Alis(db, k, 1m, 1_000m);
        await Sat(db, k, 2m, 1_500m);   // 1 adet partili, 1 adet partisiz

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));

        Assert.True(Bos(r, "karTl"));
        // KAR-RAPOR-2: yarım maliyet kâra karışmaz; hiçbir satış bilinmediği için maliyet
        // "0" DEĞİL null (bilinmiyor) — ekranda "0 ₺" basılmasın.
        Assert.True(Bos(r, "maliyetTl"));
        Assert.True(Bos(r, "maliyetHas"));
        Assert.Equal(1, r.GetProperty("maliyetiEksikSatisAdedi").GetInt32());
        Assert.Equal(3_000m, D(r, "maliyetiEksikTutarTl"));
    }

    // ── Sınır: satış yoksa VERI_YOK, kâr 0 (gerçekten sıfır)

    [Fact]
    public async Task KR06_Satis_yoksa_VERI_YOK_kar_sifir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KR06-bos");
        using var db = vt.Olustur(k);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));

        Assert.Equal("VERI_YOK", r.GetProperty("karDurumu").GetString());
        Assert.Equal(0m, D(r, "karTl"));
        Assert.Equal(0, r.GetProperty("maliyetiEksikSatisAdedi").GetInt32());
    }

    // ── Sınır: maliyeti bilinmeyen satışın İADESİ de eksik tarafta düşer

    [Fact]
    public async Task KR07_Maliyetsiz_satisin_iadesi_eksik_tarafta_duser()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KR07-iade");
        using var db = vt.Olustur(k);

        await Alis(db, k, 1m, 1_000m);
        await Sat(db, k, 1m, 1_500m);                 // bilinen
        var eksikId = await Sat(db, k, 1m, 2_000m);   // partisiz
        await Satis(db).SatisIade(new MSatis.IadeDto
        {
            KaynakHareketId = eksikId, Adet = 1m, OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId,
        }, k.Islem);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));

        Assert.Equal(1_500m, D(r, "hasilatTl"));
        Assert.Equal(500m, D(r, "karTl"));
        Assert.Equal("KISMI", r.GetProperty("karDurumu").GetString());
        Assert.Equal(1, r.GetProperty("maliyetiEksikSatisAdedi").GetInt32());
        Assert.Equal(1, r.GetProperty("maliyetiEksikIadeAdedi").GetInt32());
        Assert.Equal(0m, D(r, "maliyetiEksikTutarTl"));
    }

    // ── Rapor merkezi aylık özeti: kâr AYNI kaynaktan, satışa eşit DEĞİL

    [Fact]
    public async Task KR08_Aylik_ozet_kar_satisa_esit_degil_eksik_alanlari_tasir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KR08-aylik");
        using var db = vt.Olustur(k);

        await Alis(db, k, 2m, 1_000m);
        await Sat(db, k, 1m, 1_500m);
        await Sat(db, k, 1m, 1_600m);
        await Sat(db, k, 1m, 1_700m);

        var yerel = Zaman.UtcdenYerele(DateTime.UtcNow);
        var a = J(await TestKurulum.RaporServisi(db, new KarZararServisi(db)).AylikOzet(yerel.Year, yerel.Month, k.Islem));

        Assert.Equal(1_100m, D(a, "karTl"));
        Assert.NotEqual(D(a.GetProperty("satis"), "toplamTl"), D(a, "karTl"));
        Assert.Equal("KISMI", a.GetProperty("karDurumu").GetString());
        Assert.Equal(1, a.GetProperty("maliyetiEksikSatisAdedi").GetInt32());
        Assert.Equal(1_700m, D(a, "maliyetiEksikTutarTl"));
        Assert.Equal("KAR_ZARAR_SERVISI", a.GetProperty("karKaynagi").GetString());
    }

    // ── Ürün bazlı: aynı kural satırda

    [Fact]
    public async Task KR09_Urun_satiri_kari_yalniz_bilinenlerden()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KR09-urun");
        using var db = vt.Olustur(k);

        await Alis(db, k, 2m, 1_000m);
        await Sat(db, k, 1m, 1_500m);
        await Sat(db, k, 1m, 1_600m);
        await Sat(db, k, 1m, 1_700m);

        var r = J(await Rapor(db).UrunBazli(k.Islem, Bugun, Bugun, null, 50));
        var s = r.GetProperty("satirlar")[0];

        Assert.Equal(4_800m, D(s, "hasilatTl"));
        Assert.Equal(1_100m, D(s, "karTl"));
        Assert.Equal("KISMI", s.GetProperty("karDurumu").GetString());
        Assert.Equal(1_700m, D(s, "maliyetiEksikTutarTl"));
    }

    [Fact]
    public async Task KR10_Urun_satiri_hepsi_maliyetsizse_kar_null()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KR10-urun-bos");
        using var db = vt.Olustur(k);

        await Sat(db, k, 1m, 1_500m);

        var r = J(await Rapor(db).UrunBazli(k.Islem, Bugun, Bugun, null, 50));
        var s = r.GetProperty("satirlar")[0];
        Assert.True(Bos(s, "karTl"));
        Assert.True(Bos(s, "karHas"));
        Assert.Equal("HESAPLANAMADI", s.GetProperty("karDurumu").GetString());
    }

    // ── Tur 2 (hasım KRİTİK): kuruş yarım sınırı proje kuralıyla (AwayFromZero) yuvarlanır;
    //    hasılat − maliyet = kâr ekranda da kuruşu kuruşuna tutar (bankacı yuvarlaması YOK).

    [Fact]
    public async Task KR15_Kurus_yarim_sinirinda_AwayFromZero_hasilat_eksi_maliyet_kar()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KR15-kurus");
        using var db = vt.Olustur(k);
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId,
            PartiKaynagi.SayimFazla, 1m, k.UrunHasGrami, DateTime.UtcNow.AddDays(-1),
            birimMaliyetTl: 1_000.875m);
        await db.SaveChangesAsync();
        await Sat(db, k, 1m, 1_501m);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("TAM", r.GetProperty("karDurumu").GetString());
        Assert.Equal(1_501.00m, D(r, "hasilatTl"));
        Assert.Equal(1_000.88m, D(r, "maliyetTl"));          // 1.000,875 → 1.000,88 (AwayFromZero; ToEven da 1.000,88)
        // KAR-RAPOR-2 TEK KURAL (ekip oyu Bertan + Mustafa): kâr = YUVARLANMIŞ bileşenlerin farkı.
        // Ekranda 1.501,00 − 1.000,88 = 500,12 yazar; eski "kesin değerden" kural 500,13 basıp
        // kartları 1 kuruş tutarsız bırakıyordu. Fark tam 0 olmalı (≤ 0,01 toleransı KALKTI).
        Assert.Equal(500.12m, D(r, "karTl"));
        Assert.Equal(D(r, "hasilatTl") - D(r, "maliyetTl"), D(r, "karTl"));
        Assert.Equal(D(r, "bilinenHasilatTl") - D(r, "maliyetTl"), D(r, "karTl"));

        var u = J(await Rapor(db).UrunBazli(k.Islem, Bugun, Bugun, null, 50)).GetProperty("satirlar")[0];
        Assert.Equal(1_000.88m, D(u, "maliyetTl"));
        Assert.Equal(500.12m, D(u, "karTl"));                // ürün satırı AYNI kural
        Assert.Equal(D(u, "bilinenHasilatTl") - D(u, "maliyetTl"), D(u, "karTl"));
    }

    // ── Tur 2 (PARA-DEMO-2 hakemi): gün sonu aynı tutarı iki uyarıda söylemez.

    private static async Task<List<string>> GunSonuUyarilari(TestVeritabani vt, Kiraci k, DateTime now) =>
        J(await TestKurulum.RaporServisi(vt.Olustur(k))
                .PosFaturaMutabakati(DateOnly.FromDateTime(Zaman.UtcdenYerele(now)).ToString("yyyy-MM-dd"), k.Islem))
            .GetProperty("uyarilar").EnumerateArray().Select(x => x.GetString()!).ToList();

    private static void FaturasizSatis(KuyumcuDbContext sistem, Kiraci k, DateTime now, decimal tl) =>
        sistem.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS",
            Tarih = now, TlTutari = tl, HasGrami = -1m, ParaBirimi = "TRY",
            KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId, Olusturuldu = now,
        });

    [Fact]
    public async Task KR25_Gun_sonu_fark_yalniz_faturasiz_satistan_ise_ikinci_uyari_YOK()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "KR25-tek-uyari");
        var now = DateTime.UtcNow;
        FaturasizSatis(sistem, k, now, 102_005.57m);
        await sistem.SaveChangesAsync();

        var u = await GunSonuUyarilari(vt, k, now);
        var faturasiz = Assert.Single(u, x => x.Contains("faturalandırılmamış", StringComparison.Ordinal));
        Assert.Contains(Bicim.Tl(102_005.57m), faturasiz, StringComparison.Ordinal);
        Assert.DoesNotContain(u, x => x.Contains("fark var", StringComparison.Ordinal));
        Assert.Single(u, x => x.Contains(Bicim.Tl(102_005.57m), StringComparison.Ordinal));
    }

    [Fact]
    public async Task KR26_Gun_sonu_fark_bagsiz_faturayla_karisiksa_fark_uyarisi_KALIR()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "KR26-iki-uyari");
        var now = DateTime.UtcNow;
        FaturasizSatis(sistem, k, now, 100_000m);
        sistem.Faturalar.Add(new Fatura
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, FaturaNo = "KR26-BAGSIZ", CariId = k.CariId,
            Tur = "SATIS", AraToplam = 40_000m, Toplam = 40_000m, Durum = "KESILDI",
            Tarih = now, Olusturuldu = now, Guncellendi = now,
        });
        await sistem.SaveChangesAsync();

        var u = await GunSonuUyarilari(vt, k, now);
        var fark = Assert.Single(u, x => x.Contains("fark var", StringComparison.Ordinal));
        Assert.Contains("60.000", fark, StringComparison.Ordinal);   // 100.000 − 40.000
    }
}

/// <summary>Saf kurallar — veritabanısız.</summary>
public sealed class KarRaporSafTestleri
{
    [Theory]
    [InlineData(0, 0, "VERI_YOK")]
    [InlineData(3, 0, "TAM")]
    [InlineData(2, 1, "KISMI")]
    [InlineData(0, 4, "HESAPLANAMADI")]
    public void KR11_Kar_durumu_matrisi(int bilinen, int eksik, string beklenen)
        => Assert.Equal(beklenen, KarZararServisi.KarDurumuBelirle(bilinen, eksik));

    [Fact]
    public void KR12_Bilinen_kumesi_null_maliyeti_satirsizi_ve_eksik_adedi_dislar()
    {
        var sonuc = KarZararServisi.MaliyetiBilinenler(
            [("a", 1m), ("b", 1m), ("c", 2m), ("d", 1m), ("e", 2m)],
            [("a", 1m, 100m),
             ("b", 1m, null),                 // maliyet NULL
             ("c", 1m, 100m),                 // 2 adedin 1'i karşılandı
             ("e", 1m, 100m), ("e", 1m, 120m)]);  // d: hiç satır yok
        Assert.Equal(["a", "e"], sonuc.OrderBy(x => x).ToArray());
    }

    [Fact]
    public void KR13_Iade_negatif_adetli_satirla_karsilanir()
    {
        var sonuc = KarZararServisi.MaliyetiBilinenler([("i", -1m)], [("i", -1m, 100m)]);
        Assert.Contains("i", sonuc);
    }

    [Fact]
    public void KR14_Sozluk_TR_EN_tam_ve_ham_kod_yok()
    {
        foreach (var m in new[] { KarRaporSozlugu.Tr, KarRaporSozlugu.En })
        {
            foreach (var p in typeof(KarRaporMetinleri).GetProperties())
            {
                var v = (string)p.GetValue(m)!;
                Assert.False(string.IsNullOrWhiteSpace(v), p.Name);
                Assert.DoesNotContain("`", v, StringComparison.Ordinal);
                Assert.DoesNotContain("_", v, StringComparison.Ordinal);
            }
        }
        Assert.NotEqual(KarRaporSozlugu.Tr.KarHesaplanamadi, KarRaporSozlugu.En.KarHesaplanamadi);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// EKRAN (bUnit) — Rapor merkezi kâr kartı · stok yaşı · gün sonu tek uyarı kutusu
// ═════════════════════════════════════════════════════════════════════════════
public sealed class KarRaporRaporlarEkranTestleri : BunitContext
{
    private string aylik = AylikHesaplanamadi;
    private string yaslandirma = Yaslandirma;   // KAR-RAPOR-2: testten değiştirilebilir

    internal const string AylikHesaplanamadi = """
        {"yil":2026,"ay":10,"satis":{"sayisi":9,"toplamTl":195069.79,"toplamHas":30.5},
         "alis":{"sayisi":2,"toplamTl":40000.00,"toplamHas":5.0},
         "tahsilat":{"toplamTl":120000.00},"veresiye":{"toplamTl":0},
         "karTl":null,"karKaynagi":"HESAPLANAMADI","karDurumu":"HESAPLANAMADI",
         "maliyetiEksikSatisAdedi":9,"maliyetiEksikTutarTl":195069.79,
         "satisAlisFarkiTl":155069.79,"netHasGramHareketi":25.5,"madenDagilimi":[],
         "madenUyarisi":null,"hareketSayisi":11,"gunler":[]}
        """;

    private const string Yaslandirma = """
        {"olculduAn":"2026-10-02T06:00:00Z","subeId":null,
         "kovalar":[{"kod":"G0_30","altSinirGun":0,"ustSinirGun":30,"adet":4.000,"hasGram":12.5,"maliyetTl":0,"maliyetiBilinmeyenAdet":0}],
         "toplamAdet":4.000,"altinHasGram":12.5,
         "madenDagilimi":[{"maden":"ALTIN","madenAdi":"Altın","adet":3.000,"toplamGram":12.5},
                          {"maden":"GUMUS","madenAdi":"Gümüş","adet":1.000,"toplamGram":11.1}],
         "yasiBilinmeyenAdet":0,
         "devir":{"pencereGun":90,"tuketilenAdet":0,"kalanAdet":0,"devirHizi":null,
                  "yillikTahmini":null,"ortalamaRafOmruGun":null,"yontem":"PARTI_TUKETIMI",
                  "tanim":"devirHizi = pencerede tüketilen adet ÷ bugün rafta kalan adet"},
         "oluStok":{"esikGun":180,"toplamSatir":0,"toplamAdet":0,"ofset":0,"limit":100,"dahaFazlasiVar":false,"urunler":[]},
         "guvenilirlik":"YUKSEK",
         "uyarilar":["Stokta birden çok maden var. `altinHasGram` yalnız ALTIN has gramıdır; diğer madenler `madenDagilimi` altında ayrı verilir — gramları toplanmaz.",
                     "Rafta mal olmadığı için devir hızı hesaplanamadı (sıfır DEĞİL, bilinmiyor)."]}
        """;

    private const string Mutabakat = """
        {"tarih":"2026-10-02",
         "tahsilat":{"nakit":150.00,"kart":0,"havale":0,"diger":0,"toplam":150.00},
         "satis":{"sayisi":1,"toplamTl":4000.00,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":1,"faturasizTl":4000.00},
         "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
         "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
         "fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},
         "fark":4000.00,"mutabik":false,
         "uyarilar":["UYARI-BIR: 4.000 TL faturasız satış var","UYARI-IKI: fark 4.000 TL"],
         "faturasizIslemler":[],"faturaEslesme":null}
        """;

    public KarRaporRaporlarEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => aylik, () => yaslandirma)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi(Func<string> aylik, Func<string> yaslandirma) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/aylik") => aylik(),
                var y when y.EndsWith("/raporlar/cari-yaslandirma") =>
                    """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""",
                var y when y.EndsWith("/raporlar/stok-degeri") =>
                    """{"urunler":[],"toplamHas":0,"madenDagilimi":[],"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false,"uyari":null}""",
                var y when y.EndsWith("/raporlar/reyon-stok") => """{"reyonlar":[],"subeler":[]}""",
                var y when y.EndsWith("/raporlar/stok-yaslandirma") => yaslandirma(),
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") => Mutabakat,
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/kullanicilar") => "[]",
                var y when y.EndsWith("/kasalar") => "[]",
                var y when y.EndsWith("/reyonlar") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    [Fact]
    public void KR20_Rapor_merkezi_kar_HESAPLANAMADI_satisi_kar_diye_basmaz()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#rp-kar-deger")));

        var kar = e.Find("#rp-kar-deger").TextContent;
        Assert.Equal(KarRaporSozlugu.Tr.KarHesaplanamadi, kar.Trim());
        Assert.DoesNotContain(Bicim.Tl(195_069.79m), kar, StringComparison.Ordinal);

        var eksik = e.Find("#rp-kar-eksik-metin").TextContent;
        Assert.Equal(string.Format(KarRaporSozlugu.Tr.MaliyetiEksikSatir, 9, Bicim.Tl(195_069.79m)), eksik.Trim());
        Assert.Equal("/raporlar/kar-zarar", e.Find("#rp-kar-eksik-git").GetAttribute("href"));
        Assert.Empty(e.FindAll("#rp-kar-alt"));   // hesaplanamadı: kısmi notu yok
    }

    [Fact]
    public void KR21_Rapor_merkezi_kar_KISMI_rakam_ve_eksik_satiri_birlikte()
    {
        aylik = AylikHesaplanamadi
            .Replace("\"karTl\":null", "\"karTl\":1100.00", StringComparison.Ordinal)
            .Replace("\"karKaynagi\":\"HESAPLANAMADI\",\"karDurumu\":\"HESAPLANAMADI\"", "\"karKaynagi\":\"KAR_ZARAR_SERVISI\",\"karDurumu\":\"KISMI\"", StringComparison.Ordinal)
            .Replace("\"maliyetiEksikSatisAdedi\":9,\"maliyetiEksikTutarTl\":195069.79", "\"maliyetiEksikSatisAdedi\":1,\"maliyetiEksikTutarTl\":1700.00", StringComparison.Ordinal);
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.Equal(Bicim.TlKurus(1_100m), e.Find("#rp-kar-deger").TextContent.Trim()));
        Assert.Contains(Bicim.TlKurus(1_700m), e.Find("#rp-kar-eksik-metin").TextContent, StringComparison.Ordinal);
        // Tur 2: kısmi kâr kartın KENDİSİNDE işaretlenir (tam kâr gibi görünmez) + satırda ayraç.
        Assert.Equal(KarRaporSozlugu.Tr.KarKismiNotu, e.Find("#rp-kar-alt").TextContent.Trim());
        Assert.Contains(" · ", e.Find("#rp-kar-eksik").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void KR22_Kar_tam_bilinirse_eksik_satiri_yok()
    {
        aylik = AylikHesaplanamadi
            .Replace("\"karTl\":null", "\"karTl\":5000.00", StringComparison.Ordinal)
            .Replace("\"maliyetiEksikSatisAdedi\":9", "\"maliyetiEksikSatisAdedi\":0", StringComparison.Ordinal);
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.Equal(Bicim.TlKurus(5_000m), e.Find("#rp-kar-deger").TextContent.Trim()));
        Assert.Empty(e.FindAll("#rp-kar-eksik"));
        Assert.Empty(e.FindAll("#rp-kar-alt"));
    }

    [Fact]
    public void KR23_Stok_yasi_ham_API_alan_adlarini_basmaz_sade_metin_yazar()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sy-uyari-maden")));

        var metin = e.Find("#stok-yaslandirma-kart").TextContent;
        Assert.DoesNotContain("`", metin, StringComparison.Ordinal);
        Assert.DoesNotContain("altinHasGram", metin, StringComparison.Ordinal);
        Assert.DoesNotContain("madenDagilimi", metin, StringComparison.Ordinal);
        Assert.DoesNotContain("devirHizi", metin, StringComparison.Ordinal);
        Assert.Equal(KarRaporSozlugu.Tr.StokCokluMaden, e.Find("#sy-uyari-maden").TextContent.Trim());
        Assert.Equal(KarRaporSozlugu.Tr.StokDevirYok, e.Find("#sy-uyari-devir").TextContent.Trim());
    }

    [Fact]
    public void KR24_Gun_sonu_uyarilari_TEK_kutuda()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#mut-uyarilar")));

        var kutu = e.Find("#mut-uyarilar");
        Assert.Contains("UYARI-BIR", kutu.TextContent, StringComparison.Ordinal);
        Assert.Contains("UYARI-IKI", kutu.TextContent, StringComparison.Ordinal);
        // Her uyarı yalnız BİR sarı kutuda (eskiden her biri ayrı .uyari-kutu idi).
        Assert.Single(e.FindAll(".uyari-kutu"), k => k.TextContent.Contains("UYARI-", StringComparison.Ordinal));
        Assert.Equal(KarRaporSozlugu.Tr.GunSonuUyarilarBaslik, e.Find("#mut-uyarilar-baslik").TextContent.Trim());
    }

    // KAR-RAPOR-2 (hasım YÜKSEK): kova değeri yalnız altın hası; gümüş ayrı alt satırda, has'a katılmaz.
    [Fact]
    public void KR66_Stok_yasi_kova_yalniz_altin_has_gumus_alt_satirda_ayri()
    {
        yaslandirma = Yaslandirma.Replace(
            "\"kovalar\":[{\"kod\":\"G0_30\",\"altSinirGun\":0,\"ustSinirGun\":30,\"adet\":4.000,\"hasGram\":12.5,\"maliyetTl\":0,\"maliyetiBilinmeyenAdet\":0}]",
            "\"kovalar\":[{\"kod\":\"G0_30\",\"altSinirGun\":0,\"ustSinirGun\":30,\"adet\":4.000,\"hasGram\":159.2652,\"altinHasGram\":159.2652,\"maliyetTl\":0,\"maliyetiBilinmeyenAdet\":0," +
            "\"madenKirilimi\":[{\"maden\":\"ALTIN\",\"madenAdi\":\"Altın\",\"adet\":3.000,\"toplamGram\":159.2652},{\"maden\":\"GUMUS\",\"madenAdi\":\"Gümüş\",\"adet\":1.000,\"toplamGram\":83.25,\"fizikselGram\":90}]}]",
            StringComparison.Ordinal);
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sy-uyari-maden")));

        var kart = e.Find("#stok-yaslandirma-kart").QuerySelectorAll(".stat-kart")
            .First(k => k.QuerySelector(".stat-etiket")!.TextContent.Contains("30", StringComparison.Ordinal));
        Assert.Equal(Bicim.Has(159.2652m), kart.QuerySelector(".stat-deger")!.TextContent.Trim());
        Assert.DoesNotContain(Bicim.Has(242.5152m), e.Markup, StringComparison.Ordinal);
        var alt = kart.QuerySelector(".stat-alt")!.TextContent;
        // MADEN-AYRIMI (hasım DÜŞÜK): tartı gramı + saf gram, 2 hane ("138,750 g" saf/3 hane/birimi belirsiz idi).
        Assert.Contains("Gümüş: 90,00 g (saf 83,25 g)", alt, StringComparison.Ordinal);
        Assert.DoesNotContain("Altın:", alt, StringComparison.Ordinal);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// EKRAN (bUnit) — Kâr/Zarar ekranı
// ═════════════════════════════════════════════════════════════════════════════
public sealed class KarRaporKarZararEkranTestleri : BunitContext
{
    private const string Ozet = """
    {"baslangic":"2026-10-01","bitis":"2026-10-02","gunSayisi":2,"subeId":null,
     "satisAdedi":3,"iadeAdedi":0,
     "brutHasilatTl":24000,"iadeTl":0,"hasilatTl":24000,
     "brutMaliyetTl":null,"iadeMaliyetTl":null,"maliyetTl":null,
     "karTl":null,"karDurumu":"HESAPLANAMADI","bilinenHasilatTl":0,
     "maliyetiEksikSatisAdedi":3,"maliyetiEksikIadeAdedi":0,"maliyetiEksikTutarTl":24000,
     "karMarjiYuzde":null,
     "iscilikTl":0,"takasFarkTl":0,"takasAdedi":0,"takasNotu":null,
     "giderTl":0,"giderKirilimi":[],"donemKariTl":null,
     "hasilatHas":3.1,"iadeHas":0,"maliyetHas":null,"karHas":null,"bilinenHasilatHas":0,"maliyetiEksikHasilatHas":3.1,
     "hasilatHasDefterGram":3.0,"altinHasilatHasDefterGram":3.0,"hasDefterMadenDagilimi":[],"madenUyarisi":null,
     "karKoprusu":[{"kod":"BRUT_HASILAT","tutarTl":24000,"araToplam":false},
                   {"kod":"IADE","tutarTl":0,"araToplam":false},
                   {"kod":"NET_HASILAT","tutarTl":24000,"araToplam":true},
                   {"kod":"MALIYETI_EKSIK_SATIS","tutarTl":-24000,"araToplam":false},
                   {"kod":"MALIYET","tutarTl":null,"araToplam":false},
                   {"kod":"IADE_MALIYETI","tutarTl":null,"araToplam":false},
                   {"kod":"BRUT_KAR","tutarTl":null,"araToplam":true},
                   {"kod":"ISCILIK","tutarTl":0,"araToplam":false},
                   {"kod":"GIDER","tutarTl":0,"araToplam":false},
                   {"kod":"DONEM_KARI","tutarTl":null,"araToplam":true}],
     "storno":{"adet":0,"tutarTl":0,"notu":""},
     "karsilastir":null,"onceki":null,"degisimYuzde":null,
     "maliyetiBilinenAdet":0,"maliyetiBilinmeyenAdet":3,"kuruBulunamayanSatis":0,
     "guvenilirlik":"DUSUK","uyari":"3 satışın alış maliyeti bilinmiyor."}
    """;

    private const string Urun = """
    {"baslangic":"2026-10-01","bitis":"2026-10-02","subeId":null,"urunSayisi":1,"limit":50,"dahaFazlasiVar":false,
     "maliyetiBilinenAdet":0,"maliyetiBilinmeyenAdet":3,"kuruBulunamayanSatis":0,"guvenilirlik":"DUSUK",
     "satirlar":[{"urunId":"u1","kod":"K-1","ad":"Bilezik","satisAdedi":3,"iadeAdedi":0,
                  "brutHasilatTl":24000,"iadeTl":0,"hasilatTl":24000,"maliyetTl":null,"bilinenHasilatTl":0,
                  "karTl":null,"karDurumu":"HESAPLANAMADI","maliyetiEksikSatisAdedi":3,"maliyetiEksikTutarTl":24000,
                  "hasilatHas":3.1,"maliyetHas":null,"karHas":null,"karMarjiYuzde":null,
                  "maliyetiBilinmeyenAdet":3,"guvenilirlik":"DUSUK"}]}
    """;

    // KAR-RAPOR-2: fikstür testten değiştirilebilir (KISMI / TAM senaryoları).
    private string ozet = Ozet;
    private string urun = Urun;

    public KarRaporKarZararEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => ozet, () => urun)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<RaporServisi>();
    }

    private sealed class SahteApi(Func<string> ozet, Func<string> urun) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.Contains("kar-zarar/urun") => urun(),
                var y when y.Contains("kar-zarar") => ozet(),
                var y when y.Contains("stok-degerlemesi") =>
                    """{"subeId":null,"toplamAdet":0,"toplamHasGram":0,"maliyetliAdet":0,"maliyetsizAdet":0,"stokMaliyetiTl":0,"stokMaliyetiHas":0,"guvenilirlik":"VERI_YOK","kaynakDagilimi":[],"konsinyeUrunSayisi":0,"uyari":null}""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    [Fact]
    public void KR30_Kar_karti_hesaplanamadi_der_satis_tutarini_kar_diye_basmaz()
    {
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-maliyet-eksik")));

        var karKarti = c.FindAll("#kz-tl-kpi .stat-kart")
            .Single(k => k.QuerySelector(".stat-etiket")!.TextContent.Trim() == Dil().T.KarZarar.Kar);
        Assert.Equal(KarRaporSozlugu.Tr.KarHesaplanamadi, karKarti.QuerySelector(".stat-deger")!.TextContent.Trim());
        Assert.Equal(string.Format(KarRaporSozlugu.Tr.MaliyetiEksikSatir, 3, Bicim.TlKurus(24_000m)),
            c.Find("#kz-maliyet-eksik-satir").TextContent.Trim());
    }

    [Fact]
    public void KR31_Kopru_eksik_kalemi_sozlukten_hesaplanamayan_ara_toplam_sifir_yazilmaz()
    {
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-kopru")));

        var metin = c.Find("#kz-kopru").TextContent;
        Assert.Contains(KarRaporSozlugu.Tr.KopruMaliyetiEksik, metin, StringComparison.Ordinal);
        Assert.DoesNotContain("MALIYETI_EKSIK_SATIS", metin, StringComparison.Ordinal);
        Assert.DoesNotContain(Bicim.Tl(0m) + "\n", metin.Replace(" ", ""), StringComparison.Ordinal);
        Assert.Contains(KarRaporSozlugu.Tr.KarHesaplanamadi, metin, StringComparison.Ordinal);
    }

    [Fact]
    public void KR32_Urun_satirinda_kar_hesaplanamadi()
    {
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.Contains("Bilezik", c.Markup, StringComparison.Ordinal));
        var satir = c.FindAll("tr").First(t => t.TextContent.Contains("Bilezik", StringComparison.Ordinal));
        Assert.Contains(KarRaporSozlugu.Tr.KarHesaplanamadi, satir.TextContent, StringComparison.Ordinal);
    }

    // ═══ KAR-RAPOR-2 ═════════════════════════════════════════════════════════
    // KISMI fikstürü: toplam 268.572,47 · bilinen 168.900,55 · eksik 99.671,92 · maliyet 152.345,67
    // · kâr 16.554,88 (= 168.900,55 − 152.345,67). Has: bilinen 40,1749 − maliyet 22,7751 = 17,3998.
    private static string Kismi() => Ozet
        .Replace("\"brutHasilatTl\":24000,\"iadeTl\":0,\"hasilatTl\":24000,", "\"brutHasilatTl\":268572.47,\"iadeTl\":0,\"hasilatTl\":268572.47,", StringComparison.Ordinal)
        .Replace("\"brutMaliyetTl\":null,\"iadeMaliyetTl\":null,\"maliyetTl\":null,", "\"brutMaliyetTl\":152345.67,\"iadeMaliyetTl\":0,\"maliyetTl\":152345.67,", StringComparison.Ordinal)
        .Replace("\"karTl\":null,\"karDurumu\":\"HESAPLANAMADI\",\"bilinenHasilatTl\":0,", "\"karTl\":16554.88,\"karDurumu\":\"KISMI\",\"bilinenHasilatTl\":168900.55,", StringComparison.Ordinal)
        .Replace("\"maliyetiEksikSatisAdedi\":3,\"maliyetiEksikIadeAdedi\":0,\"maliyetiEksikTutarTl\":24000,", "\"maliyetiEksikSatisAdedi\":2,\"maliyetiEksikIadeAdedi\":0,\"maliyetiEksikTutarTl\":99671.92,", StringComparison.Ordinal)
        .Replace("\"donemKariTl\":null,", "\"donemKariTl\":16554.88,", StringComparison.Ordinal)
        .Replace("\"hasilatHas\":3.1,\"iadeHas\":0,\"maliyetHas\":null,\"karHas\":null,\"bilinenHasilatHas\":0,\"maliyetiEksikHasilatHas\":3.1,",
                 "\"hasilatHas\":40.1749,\"iadeHas\":0,\"maliyetHas\":22.7751,\"karHas\":17.3998,\"karHasIkiHane\":17.39,\"bilinenHasilatHas\":40.1749,\"maliyetiEksikHasilatHas\":0,", StringComparison.Ordinal)
        .Replace("\"kod\":\"GIDER\",\"tutarTl\":0", "\"kod\":\"GIDER\",\"tutarTl\":-250", StringComparison.Ordinal);

    private Dictionary<string, string> Kartlar(IRenderedComponent<Kuyumcu.Web.Pages.KarZarar> c, string izgara) =>
        c.FindAll($"{izgara} .stat-kart").ToDictionary(
            k => k.QuerySelector(".stat-etiket")!.TextContent.Trim(),
            k => k.QuerySelector(".stat-deger")!.TextContent.Trim() + "|" + (k.QuerySelector(".stat-alt")?.TextContent.Trim() ?? ""));

    private static decimal Tl(string metin) =>
        decimal.Parse(metin.Split('|')[0].Replace("₺", "").Replace(".", "").Replace(",", ".").Trim(),
            System.Globalization.CultureInfo.InvariantCulture);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void KR60_KISMI_ozet_kartlari_toplanabilir_bilinen_hasilat_ilk_kartta(bool basit)
    {
        ozet = Kismi();
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-tl-kpi")));

        var k = Kartlar(c, "#kz-tl-kpi");
        var tr = KarRaporSozlugu.Tr;
        Assert.True(k.ContainsKey(tr.BilinenHasilat));
        Assert.False(k.ContainsKey(Dil().T.KarZarar.Hasilat));     // toplam hasılat KART değil, alt satır
        var hasilat = Tl(k[tr.BilinenHasilat]);
        var maliyet = Tl(k[Dil().T.KarZarar.Maliyet]);
        var kar = Tl(k[Dil().T.KarZarar.Kar]);
        Assert.Equal(168_900.55m, hasilat);
        Assert.Equal(hasilat - maliyet, kar);                         // EKRANDA toplanabilir
        Assert.Contains(string.Format(tr.ToplamHasilatAlt, Bicim.Tl(268_572.47m), Bicim.Tl(99_671.92m)),
            k[tr.BilinenHasilat], StringComparison.Ordinal);
        Assert.Contains(tr.MaliyetYalnizBilinen, k[Dil().T.KarZarar.Maliyet], StringComparison.Ordinal);
    }

    [Fact]
    public void KR61_BASIT_has_karti_gorunen_bilesenlerden_toplanabilir()
    {
        ozet = Kismi();
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => true);
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-tl-kpi")));
        var hasKartlar = c.FindAll(".stat-izgara")[1].QuerySelectorAll(".stat-kart")
            .Select(x => x.QuerySelector(".stat-deger")!.TextContent.Trim()).ToList();
        // MADEN-AYRIMI (ekip kararı 5): BASİT de 4 hane "has" — PRO ile AYNI rakam, kartlar yine toplanabilir:
        // 40,1749 − 22,7751 = 17,3998.
        Assert.Equal("40,1749 has", hasKartlar[0]);
        Assert.Equal("22,7751 has", hasKartlar[1]);
        Assert.Equal("17,3998 has", hasKartlar[2]);
    }

    // ═══ KAR-RAPOR-2 tur 2 (hasım bulguları) → MADEN-AYRIMI (hasım ORTA ×2, ekip kararı 5) ═══
    // Has kârı iki modda AYNI sunucu alanıdır (karHas, 4 hane); istemci kârı yeniden hesaplamaz ve
    // BASİT ayrı bir 2 haneli rakam (karHasIkiHane) basmaz — mod değişince kâr DEĞİŞMEZ.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void KR67_Has_kari_BASIT_ve_PRO_ayni_sunucu_karHas(bool basit)
    {
        ozet = Kismi().Replace("\"karHasIkiHane\":17.39", "\"karHasIkiHane\":17.41", StringComparison.Ordinal);
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-tl-kpi")));
        var hasKar = c.FindAll(".stat-izgara")[1].QuerySelectorAll(".stat-kart")[2]
            .QuerySelector(".stat-deger")!.TextContent.Trim();
        Assert.Equal("17,3998 has", hasKar);
        Assert.DoesNotContain("17,41", c.Markup, StringComparison.Ordinal);
    }

    // KISMI ürün satırı: "Maliyeti bilinen satış" alt satırı tutara YAPIŞMAZ — ayrı blok öğedir.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void KR68_Urunler_KISMI_satir_bilinen_hasilat_ayri_blokta_tutara_yapismaz(bool basit)
    {
        urun = Urun
            .Replace("\"brutHasilatTl\":24000,\"iadeTl\":0,\"hasilatTl\":24000,\"maliyetTl\":null,\"bilinenHasilatTl\":0,",
                     "\"brutHasilatTl\":5000.55,\"iadeTl\":0,\"hasilatTl\":5000.55,\"maliyetTl\":2000.01,\"bilinenHasilatTl\":3500.55,", StringComparison.Ordinal)
            .Replace("\"karTl\":null,\"karDurumu\":\"HESAPLANAMADI\",\"maliyetiEksikSatisAdedi\":3,\"maliyetiEksikTutarTl\":24000,",
                     "\"karTl\":1500.54,\"karDurumu\":\"KISMI\",\"maliyetiEksikSatisAdedi\":1,\"maliyetiEksikTutarTl\":1500,", StringComparison.Ordinal);
        Assert.Contains("\"KISMI\"", urun, StringComparison.Ordinal);
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-tl-kpi")));
        c.Find("#kz-sekme-urunler").Click();
        c.WaitForAssertion(() => Assert.Contains("Bilezik", c.Markup, StringComparison.Ordinal));
        var satir = c.FindAll("tr").First(t => t.TextContent.Contains("Bilezik", StringComparison.Ordinal));
        var hasilatHucresi = satir.QuerySelectorAll("td")[2];
        var alt = hasilatHucresi.QuerySelector(".kz-urun-bilinen")!;
        Assert.Equal("DIV", alt.TagName);                                   // blok öğe: kendi satırında
        Assert.Equal($"{KarRaporSozlugu.Tr.BilinenHasilat}: {Bicim.Tl(3_500.55m)}", alt.TextContent.Trim());
        // Tutar, alt satırın DIŞINDA kendi metin düğümüdür (iki metin tek kelime gibi birleşmez)
        var tutarMetni = string.Concat(hasilatHucresi.ChildNodes
            .Where(n => n.NodeType == AngleSharp.Dom.NodeType.Text).Select(n => n.TextContent)).Trim();
        Assert.Equal(Bicim.Tl(5_000.55m), tutarMetni);
        // Satırda toplanabilirlik: bilinen hasılat − maliyet = kâr
        var hucreler = satir.QuerySelectorAll("td").Select(t => t.TextContent.Trim()).ToList();
        Assert.Equal(Bicim.Tl(2_000.01m), hucreler[3]);
        Assert.Equal(Bicim.Tl(1_500.54m), hucreler[5]);
    }

    // Önceki dönemde maliyet/kâr bilinmiyorsa grafik 0 çubuğu sanılmasın: not yazılır.
    [Fact]
    public void KR69_Grafik_onceki_donem_bilinmeyen_maliyet_icin_bilinmiyor_notu()
    {
        ozet = Kismi().Replace("\"onceki\":null",
            "\"onceki\":{\"baslangic\":\"2026-09-01\",\"bitis\":\"2026-09-30\",\"gunSayisi\":30,\"hasilatTl\":1000,\"maliyetTl\":null,\"karTl\":null,\"donemKariTl\":null}",
            StringComparison.Ordinal);
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => false);
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-tl-kpi")));
        c.Find("#kz-sekme-grafik").Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-grafik-cubuk")));
        var not = c.Find("#kz-grafik-onceki-bilinmiyor").TextContent.Trim();
        var d = Dil().T.KarZarar;
        Assert.Equal(string.Format(KarRaporSozlugu.Tr.GrafikOncekiBilinmiyor,
            string.Join(", ", d.Maliyet, d.Kar, d.DonemKari)), not);
    }

    [Fact]
    public void KR69b_Grafik_onceki_donem_biliniyorsa_not_yok()
    {
        ozet = Kismi().Replace("\"onceki\":null",
            "\"onceki\":{\"baslangic\":\"2026-09-01\",\"bitis\":\"2026-09-30\",\"gunSayisi\":30,\"hasilatTl\":1000,\"maliyetTl\":600,\"karTl\":400,\"donemKariTl\":400}",
            StringComparison.Ordinal);
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => false);
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-tl-kpi")));
        c.Find("#kz-sekme-grafik").Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-grafik-cubuk")));
        Assert.Empty(c.FindAll("#kz-grafik-onceki-bilinmiyor"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void KR62_HESAPLANAMADI_maliyet_0_basilmaz_hesaplanamadi_ve_bilinmiyor(bool basit)
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-tl-kpi")));
        var tr = KarRaporSozlugu.Tr;
        var k = Kartlar(c, "#kz-tl-kpi");
        Assert.Equal(tr.KarHesaplanamadi, k[Dil().T.KarZarar.Maliyet].Split('|')[0]);
        Assert.Equal(Bicim.TlKurus(24_000m), k[Dil().T.KarZarar.Hasilat].Split('|')[0]);
        // Has bacağının maliyeti de "0,0000 g / 0,00 g" değil.
        // PAKET RAPOR-4 (VERİ 3): BASİT'te altın kârı hesaplanamıyorken üç kart yerine tek cümle çizilir.
        if (basit)
        {
            Assert.Equal(tr.AltinKariBasitYok, c.Find("#kz-altin-kari-yok").TextContent.Trim());
        }
        else
        {
            var hasMaliyet = c.FindAll(".stat-izgara")[1].QuerySelectorAll(".stat-kart")[1]
                .QuerySelector(".stat-deger")!.TextContent.Trim();
            Assert.Equal(tr.KarHesaplanamadi, hasMaliyet);
        }
        // Ürünler sekmesi: maliyet hücresi "Bilinmiyor"
        c.Find("#kz-sekme-urunler").Click();
        c.WaitForAssertion(() => Assert.Contains("Bilezik", c.Markup, StringComparison.Ordinal));
        var satir = c.FindAll("tr").First(t => t.TextContent.Contains("Bilezik", StringComparison.Ordinal));
        var hucreler = satir.QuerySelectorAll("td").Select(t => t.TextContent.Trim()).ToList();
        // PAKET RAPOR-3 (K17): hücrede görünen "—"; "Bilinmiyor" ekran okuyucuda (neden tablonun üstünde TEK ⓘ).
        Assert.Equal($"—{tr.MaliyetBilinmiyor}", hucreler[3]);
        Assert.DoesNotContain(Bicim.TlKurus(0m), hucreler[3], StringComparison.Ordinal);
    }

    [Fact]
    public void KR63_Kopru_GIDER_sozlukten_tutar_basligi_saga_hizali()
    {
        ozet = Kismi();
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-tl-kpi")));
        c.Find("#kz-sekme-kopru").Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-kopru")));
        var metin = c.Find("#kz-kopru").TextContent;
        Assert.DoesNotContain("GIDER", metin, StringComparison.Ordinal);
        Assert.Contains(KarRaporSozlugu.Tr.KopruGider, metin, StringComparison.Ordinal);
        Assert.Contains("sayi", c.Find("#kz-kopru-tutar-baslik").ClassName, StringComparison.Ordinal);
        // KAR-RAPOR-2 tur 2: Ürünler tablosunda da sayısal başlıklar değerlerle aynı (sağ) hizada.
        c.Find("#kz-sekme-urunler").Click();
        c.WaitForAssertion(() => Assert.Contains(c.FindAll("table"), t => t.TextContent.Contains("Bilezik", StringComparison.Ordinal)));
        var urunTablosu = c.FindAll("table").First(t => t.TextContent.Contains("Bilezik", StringComparison.Ordinal));
        var basliklar = urunTablosu.QuerySelectorAll("thead th").ToList();
        Assert.Equal(10, basliklar.Count);   // M9 (17.10): + İşçilik kârı, Altın farkı
        Assert.DoesNotContain("sayi", basliklar[0].ClassName ?? "", StringComparison.Ordinal);
        Assert.All(basliklar.Skip(1), th => Assert.Contains("sayi", th.ClassName ?? "", StringComparison.Ordinal));
    }

    [Fact]
    public void KR64_BASIT_guvenilirlik_ve_degismez_kutusu_kapali_katlanirda_PRO_acik()
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => true);
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-nereden")));
        Assert.Empty(c.FindAll("#kz-guven"));
        Assert.Empty(c.FindAll(".degismez-ipucu"));
        c.Find("#kz-nereden-ac").Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-guven")));
        Assert.NotEmpty(c.FindAll("#kz-nereden-govde .degismez-ipucu"));
    }

    [Fact]
    public void KR65_PRO_guvenilirlik_karti_dogrudan_gorunur()
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => false);
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-guven")));
        Assert.Empty(c.FindAll("#kz-nereden"));
        // RAPOR-GORUNUM (hasım/tasarım ORTA): PRO'da açık 4 satırlık kutu yerine KAPALI "Rapordaki bir rakam
        // yanlışsa" katlanırı (Rapor merkeziyle aynı kalıp); açılınca rapora özgü TEK cümle.
        Assert.Empty(c.FindAll(".degismez-ipucu"));
        c.Find("#kz-duzeltme-ac").Click();
        c.WaitForAssertion(() => Assert.NotEmpty(c.FindAll("#kz-duzeltme-govde .degismez-ipucu")));
        Assert.Equal(KarRaporSozlugu.Tr.RaporTuretilmisGerekce, c.Find("#kz-duzeltme-govde .degismez-gerekce").TextContent.Trim());
        Assert.Empty(c.FindAll("#kz-duzeltme-govde .degismez-alternatif"));
    }

    private DilServisi Dil() => Services.GetRequiredService<DilServisi>();
}

// ═════════════════════════════════════════════════════════════════════════════
// EKRAN (bUnit) — Panel "Bugün ne sattım?" kâr satırı (YALNIZ kâr alanı)
// ═════════════════════════════════════════════════════════════════════════════
public sealed class KarRaporPanelEkranTestleri : BunitContext
{
    private string kar = KarHesaplanamadi;

    private const string KarHesaplanamadi = """
        {"hasilatTl":57550.00,"maliyetTl":0,"karTl":null,"karDurumu":"HESAPLANAMADI","karMarjiYuzde":null,
         "hasilatHas":10.4,"maliyetHas":0,"karHas":null,"satisAdedi":3,
         "maliyetiEksikSatisAdedi":3,"maliyetiEksikTutarTl":57550.00,
         "maliyetiBilinenAdet":0,"maliyetiBilinmeyenAdet":3,"guvenilirlik":"DUSUK","uyari":"x"}
        """;

    private const string Gunluk = """
        {"tarih":"2026-10-02","satis":{"adet":3,"toplamTl":57550.00,"toplamHas":10.4},
         "alis":{"adet":0,"toplamTl":0,"toplamHas":0},
         "hurda":{"toplamHas":0,"odenenTl":0},"netHasGramHareketi":10.4,"hareketSayisi":3,
         "kasalar":[{"id":"k1","ad":"Nakit","bakiye":44200.00,"paraBirimi":"TRY"}]}
        """;

    public KarRaporPanelEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => kar)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
        Services.AddScoped<HavaServisi>();
        Services.BugunServisleri();   // TEMA TEMİZLİĞİ 17.10: Panel = Yıldız Bugün (YetkiServisi)
    }

    private sealed class SahteApi(Func<string> kar) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var cevap = yol switch
            {
                var y when y.EndsWith("/raporlar/gunluk", StringComparison.Ordinal) => Gunluk,
                var y when y.EndsWith("/raporlar/cari-yaslandirma", StringComparison.Ordinal) =>
                    """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""",
                var y when y.EndsWith("/raporlar/vade-takibi", StringComparison.Ordinal) =>
                    """{"ufukGun":7,"gecikenToplam":0,"yaklasanToplam":0,"vadesizToplam":0,"ufukOtesiToplam":0,"acikToplam":0,"cariler":[]}""",
                var y when y.EndsWith("/raporlar/aylik", StringComparison.Ordinal) => """{"gunler":[]}""",
                var y when y.Contains("/raporlar/islem-defteri", StringComparison.Ordinal) => """
                    {"baslangic":"2026-10-02","bitis":"2026-10-02","toplamSatir":0,"ofset":0,"limit":5,
                     "dahaFazlasiVar":false,"ozet":{"tlGiris":0,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":0},
                     "storno":{"adet":0,"tutarTl":0},"satirlar":[]}
                    """,
                var y when y.EndsWith("/bildirimler", StringComparison.Ordinal) =>
                    """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""",
                var y when y.EndsWith("/raporlar/kar-zarar", StringComparison.Ordinal) => kar(),
                var y when y.EndsWith("/bankalar", StringComparison.Ordinal) => """{"hesaplar":[]}""",
                var y when y.EndsWith("/kasalar/nakit-ayrimi", StringComparison.Ordinal) =>
                    """[{"kasaId":"k1","paraBirimi":"TRY","toplam":44200.00,"nakit":44200.00,"kart":0,"havale":0}]""",
                var y when y.EndsWith("/pozisyon", StringComparison.Ordinal) => """
                    {"anaMaden":"ALTIN","altin":{"maden":"ALTIN","vitrinGram":0,"hurdaGram":0,"cariAlacakGram":0,"cariBorcGram":0,
                     "netGram":0,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},
                     "digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,"hesaplanmaAni":"2026-10-02T09:00:00Z","donemdenBagimsiz":true}
                    """,
                var y when y.EndsWith("/kur/kaynaklar", StringComparison.Ordinal) =>
                    """{"kaynaklar":["ALTINKAYNAK"],"secili":"ALTINKAYNAK","varsayilan":"ALTINKAYNAK"}""",
                var y when y.Contains("/tercihler/benim", StringComparison.Ordinal) => """{"tercihler":{}}""",
                var y when y.Contains("/uyum/", StringComparison.Ordinal) =>
                    """{"suresiGecmis":0,"yaklasan":0,"gecikmisSayisi":0,"yuksek":0}""",
                // TEMA TEMİZLİĞİ 17.10 (YILDIZ-4C): Bugün'ün gün sonu / emanet satırlarının uçları gerçekçi.
                var y when y.EndsWith("/raporlar/kasa-defteri", StringComparison.Ordinal) =>
                    """{"baslangic":"2026-10-01","bitis":"2026-10-01","saatDilimi":"Europe/Istanbul","utcOfsetSaat":3,"yazSaatiRiski":false,"toplamSatir":0,"kasalar":[]}""",
                var y when y.EndsWith("/cari", StringComparison.Ordinal) => "[]",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    // TEMA TEMİZLİĞİ 17.10: Panel = Yıldız Bugün — eski "Bugün ne sattım?" kartının kâr satırı kaldırıldı; kâr Bugün
    // alt şeridindeki "Kâr" öğesidir (#yildiz-bugun-kar, aynı uç: GET /raporlar/kar-zarar bugün..bugün). Kural: maliyeti
    // eksik satış/iade varsa (ya da eski şekil yanıtta maliyetsiz adet) rakam BASILMAZ — "maliyet eksik" (fail-closed).
    private IRenderedComponent<Kuyumcu.Web.Pages.Panel> Basit()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1",
                ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.BugunBekle();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yildiz-bugun-kar")), TimeSpan.FromSeconds(10));
        return e;
    }

    private static string MaliyetEksik => $"{YildizBugunSozlugu.Kar("tr")} {YildizBugunSozlugu.MaliyetEksik("tr")}";

    [Fact]
    public void KR40_Panel_kar_hesaplanamadi_der_rakam_basmaz()
    {
        var e = Basit();
        var k = e.Find("#yildiz-bugun-kar");
        Assert.Equal(MaliyetEksik, k.TextContent.Trim());
        Assert.Equal("/raporlar/kar-zarar", k.GetAttribute("href"));
        Assert.Empty(e.FindAll("#yildiz-bugun-kar-hata"));
    }

    [Fact]
    public void KR41_Panel_kar_KISMI_rakam_ve_eksik_sayisi()
    {
        kar = KarHesaplanamadi
            .Replace("\"karTl\":null", "\"karTl\":1100.00", StringComparison.Ordinal)
            .Replace("\"maliyetiEksikSatisAdedi\":3", "\"maliyetiEksikSatisAdedi\":1", StringComparison.Ordinal);
        var e = Basit();
        // Bugün'de kısmi kâr günün kârı gibi okunmasın diye rakam YAZILMAZ; kapsam (yalnız N satış) Kâr-zarar'da.
        Assert.Equal(MaliyetEksik, e.Find("#yildiz-bugun-kar").TextContent.Trim());
        Assert.DoesNotContain("1.100", e.Find("#yildiz-bugun").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void KR42_Panel_kar_TAM_yalniz_rakam()
    {
        kar = KarHesaplanamadi
            .Replace("\"karTl\":null", "\"karTl\":4120.00", StringComparison.Ordinal)
            .Replace("\"maliyetiEksikSatisAdedi\":3", "\"maliyetiEksikSatisAdedi\":0", StringComparison.Ordinal)
            .Replace("\"maliyetiBilinmeyenAdet\":3", "\"maliyetiBilinmeyenAdet\":0", StringComparison.Ordinal);
        var e = Basit();
        Assert.Equal($"{YildizBugunSozlugu.Kar("tr")} {Bicim.Tl(4_120m)}", e.Find("#yildiz-bugun-kar").TextContent.Trim());
    }

    // KAR-RAPOR-2 (hasım DÜŞÜK): yeni şekil yanıtta (karDurumu var) kâr TAM ve eksik satış 0 iken
    // yuvarlama artığı "maliyeti bilinmeyen adet" Panel'de "maliyet eksik" yazdırmaz.
    [Fact]
    public void KR43_Panel_TAM_yuvarlama_artigi_bilinmeyen_adet_eski_dali_acmaz()
    {
        kar = KarHesaplanamadi
            .Replace("\"karTl\":null", "\"karTl\":4120.00", StringComparison.Ordinal)
            .Replace("\"karDurumu\":\"HESAPLANAMADI\"", "\"karDurumu\":\"TAM\"", StringComparison.Ordinal)
            .Replace("\"maliyetiEksikSatisAdedi\":3", "\"maliyetiEksikSatisAdedi\":0", StringComparison.Ordinal)
            .Replace("\"maliyetiBilinmeyenAdet\":3", "\"maliyetiBilinmeyenAdet\":0.0004", StringComparison.Ordinal);
        var e = Basit();
        Assert.Equal($"{YildizBugunSozlugu.Kar("tr")} {Bicim.Tl(4_120m)}", e.Find("#yildiz-bugun-kar").TextContent.Trim());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KAR-RAPOR-2 (02.10) — tasarım hakemi + hasım kalanları
//   • Özet kartları TOPLANABİLİR: bilinenHasılat − maliyet = kâr (TL ve has), toplam
//     hasılat − maliyeti eksik = bilinen hasılat; köprü kalemleri dönem kârına eşit.
//   • Kuruş kuralı TEK: kâr = YUVARLANMIŞ bileşenlerin farkı (ekip oyu Bertan + Mustafa).
//   • Bilinmeyen maliyet null (0 değil) — dönem ve ürün satırı.
//   • Stok yaşı kova has toplamına gümüş gramı KARIŞMAZ.
//   • Satırsız adet, MaliyetiBilinenler ile AYNI toleransı kullanır (Panel çelişkisi).
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class KarRapor2DbTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);
    private static KarZararServisi Rapor(KuyumcuDbContext db) => new(db);
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

    private static async Task MaliyetliParti(KuyumcuDbContext db, Kiraci k, decimal adet, decimal birimTl)
    {
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId,
            PartiKaynagi.SayimFazla, adet, k.UrunHasGrami, DateTime.UtcNow.AddDays(-1),
            // MADEN-2: has fiyatı da verilir (kur fikstürü 6.827,20) — has fiyatsız parti artık has kârına
            // 0 maliyetle GİRMEZ (MAS20); bu testin niyeti has bacağının TOPLANABİLİRLİĞİ, has bilinmezliği değil.
            birimMaliyetTl: birimTl, altinHasFiyati: 6_827.20m);
        await db.SaveChangesAsync();
    }

    // ── KISMI: kesirli maliyet (2 × 1.000,333 = 2.000,666 → 2.000,67) + maliyetsiz bir satış.
    [Fact]
    public async Task KR50_KISMI_ozet_kartlari_toplanabilir_TL_ve_has_kopru_ve_urun_satiri()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETKAR-RAPOR-2-TEST KR50");
        using var db = vt.Olustur(k);
        await MaliyetliParti(db, k, 2m, 1_000.333m);
        await Sat(db, k, 1m, 1_501m);
        await Sat(db, k, 1m, 1_501m);
        await Sat(db, k, 1m, 2_000.55m);   // parti bitti → maliyeti BİLİNMİYOR

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("KISMI", r.GetProperty("karDurumu").GetString());
        Assert.Equal(5_002.55m, D(r, "hasilatTl"));
        Assert.Equal(3_002.00m, D(r, "bilinenHasilatTl"));
        Assert.Equal(2_000.55m, D(r, "maliyetiEksikTutarTl"));
        Assert.Equal(2_000.67m, D(r, "maliyetTl"));
        Assert.Equal(1_001.33m, D(r, "karTl"));
        // Kartlar TOPLANABİLİR (kuruşu kuruşuna, tolerans YOK)
        Assert.Equal(D(r, "bilinenHasilatTl") - D(r, "maliyetTl"), D(r, "karTl"));
        Assert.Equal(D(r, "hasilatTl") - D(r, "maliyetiEksikTutarTl"), D(r, "bilinenHasilatTl"));
        Assert.Equal(D(r, "brutMaliyetTl") - D(r, "iadeMaliyetTl"), D(r, "maliyetTl"));
        // Has bacağı AYNI kümeden
        Assert.Equal(D(r, "bilinenHasilatHas") - D(r, "maliyetHas"), D(r, "karHas"));
        Assert.Equal(D(r, "hasilatHas") - D(r, "bilinenHasilatHas"), D(r, "maliyetiEksikHasilatHas"));
        Assert.True(D(r, "maliyetiEksikHasilatHas") > 0m);
        // tur 2: BASİT 2 haneli has kârı SUNUCUDA = R2(bilinen has) − R2(maliyet has)
        Assert.Equal(Math.Round(D(r, "bilinenHasilatHas"), 2, MidpointRounding.AwayFromZero)
                   - Math.Round(D(r, "maliyetHas"), 2, MidpointRounding.AwayFromZero), D(r, "karHasIkiHane"));
        // Köprü: işaretsiz kalemler dönem kârına eşit
        var kopru = r.GetProperty("karKoprusu").EnumerateArray().ToList();
        var toplam = kopru.Where(x => !x.GetProperty("araToplam").GetBoolean())
                          .Sum(x => x.GetProperty("tutarTl").GetDecimal());
        Assert.Equal(D(r, "donemKariTl"), toplam);
        Assert.Equal(D(r, "karTl"), kopru.Single(x => x.GetProperty("kod").GetString() == "BRUT_KAR")
                                        .GetProperty("tutarTl").GetDecimal());
        // Ürün satırı aynı kural
        var u = J(await Rapor(db).UrunBazli(k.Islem, Bugun, Bugun, null, 50)).GetProperty("satirlar")[0];
        Assert.Equal("KISMI", u.GetProperty("karDurumu").GetString());
        Assert.Equal(3_002.00m, D(u, "bilinenHasilatTl"));
        Assert.Equal(D(u, "bilinenHasilatTl") - D(u, "maliyetTl"), D(u, "karTl"));
        Assert.Equal(D(u, "hasilatTl") - D(u, "bilinenHasilatTl"), D(u, "maliyetiEksikTutarTl"));
    }

    // ── HESAPLANAMADI: bilinmeyen maliyet "0" DEĞİL null (dönem + ürün + köprü)
    [Fact]
    public async Task KR51_HESAPLANAMADI_maliyet_null_sifir_degil_donem_urun_kopru()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETKAR-RAPOR-2-TEST KR51");
        using var db = vt.Olustur(k);
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId,
            PartiKaynagi.SayimFazla, 2m, k.UrunHasGrami, DateTime.UtcNow.AddDays(-1));   // maliyetsiz
        await db.SaveChangesAsync();
        await Sat(db, k, 1m, 2_500m);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("HESAPLANAMADI", r.GetProperty("karDurumu").GetString());
        Assert.Equal(2_500m, D(r, "hasilatTl"));
        Assert.True(Bos(r, "maliyetTl"));
        Assert.True(Bos(r, "brutMaliyetTl"));
        Assert.True(Bos(r, "iadeMaliyetTl"));
        Assert.True(Bos(r, "maliyetHas"));
        Assert.True(Bos(r, "karTl"));
        Assert.True(Bos(r, "karHasIkiHane"));
        Assert.True(Bos(r, "karHas"));
        var kopru = r.GetProperty("karKoprusu").EnumerateArray().ToList();
        Assert.Equal(System.Text.Json.JsonValueKind.Null,
            kopru.Single(x => x.GetProperty("kod").GetString() == "MALIYET").GetProperty("tutarTl").ValueKind);

        var u = J(await Rapor(db).UrunBazli(k.Islem, Bugun, Bugun, null, 50)).GetProperty("satirlar")[0];
        Assert.Equal("HESAPLANAMADI", u.GetProperty("karDurumu").GetString());
        Assert.True(Bos(u, "maliyetTl"));
        Assert.True(Bos(u, "maliyetHas"));
        Assert.True(Bos(u, "karTl"));
    }

    // ── Panel çelişkisi: ≤ 0,0005 adet karşılanmamış açık MaliyetiBilinenler'de "bilinen" sayılıyorsa
    //    satırsız adet de 0 olmalı. ⚠ Saf test: adet kolonları decimal(12,3) olduğundan bu açık
    //    veritabanında üretilemez (ilk sürüm DB testiydi; mutasyon M2-5 onu kırmızı yapamadı → saf).
    [Fact]
    public void KR52_Satirsiz_adet_MaliyetiBilinenler_ile_ayni_toleransi_kullanir()
    {
        var hareket = new[] { ("h1", (decimal?)1m), ("h2", (decimal?)2m) };
        var tuketim = new[] { ((string?)"h1", 0.9996m), ((string?)"h2", 1.998m) };
        var bilinen = KarZararServisi.MaliyetiBilinenler(hareket,
            tuketim.Select(t => (t.Item1, t.Item2, (decimal?)1_000m)));
        Assert.Contains("h1", bilinen);                                  // 0,0004 açık → bilinen
        Assert.DoesNotContain("h2", bilinen);                            // 0,002 açık → eksik
        Assert.Equal(0.002m, KarZararServisi.SatirsizAdet(hareket, tuketim));   // h1'in artığı SAYILMAZ
        Assert.Equal(0m, KarZararServisi.SatirsizAdet(hareket.Take(1), tuketim.Take(1)));
    }

    // ── Stok yaşı: altın + gümüş aynı kovada → kova hasGram YALNIZ altın hası
    [Fact]
    public async Task KR53_Stok_yasi_kova_has_toplamina_gumus_gramı_karismaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETKAR-RAPOR-2-TEST KR53");
        using var db = vt.Olustur(k);
        var gumusId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Urunler.Add(new Urun
        {
            Id = gumusId, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"G-{gumusId[..8]}", Ad = "Gümüş künye",
            AyarId = "G925", Milyem = 925m, GramAgirlik = 30m, TasAgirlik = 0m, HasGrami = 27.75m,
            IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        await db.SaveChangesAsync();
        var motor = new MaliyetMotoru(db);
        motor.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.SayimFazla, 2m, k.UrunHasGrami, now.AddDays(-1));
        motor.PartiAc(k.TenantId, k.SubeId, gumusId, PartiKaynagi.SayimFazla, 3m, 27.75m, now.AddDays(-1));
        await db.SaveChangesAsync();

        var servis = new Kuyumcu.Api.Modules.Rapor.StokYaslandirmaServisi(db,
            new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor()));
        var r = J(await servis.Yaslandirma(k.Islem, null));
        var kova = r.GetProperty("kovalar")[0];
        var altinHas = 2m * k.UrunHasGrami;
        Assert.Equal(altinHas, D(kova, "hasGram"));                  // 83,25 g gümüş KATILMAZ
        Assert.Equal(altinHas, D(kova, "altinHasGram"));
        Assert.Equal(5m, D(kova, "adet"));
        var madenler = kova.GetProperty("madenKirilimi").EnumerateArray().ToList();
        Assert.Equal(83.25m, madenler.Single(m => m.GetProperty("maden").GetString() == "GUMUS")
                                     .GetProperty("toplamGram").GetDecimal());
        Assert.Equal(altinHas, madenler.Single(m => m.GetProperty("maden").GetString() == "ALTIN")
                                       .GetProperty("toplamGram").GetDecimal());
        // Kovaların has toplamı, raporun altinHasGram'ına eşit (iki rakam aynı kümeden)
        Assert.Equal(D(r, "altinHasGram"),
            r.GetProperty("kovalar").EnumerateArray().Sum(x => x.GetProperty("hasGram").GetDecimal()));
    }

    // tur 2 (hasım bulgusu): kova (LEFT JOIN) ile madenDagilimi aynı join kuralı — ürünsüz parti iki
    // rakamda da aynı varsayımla (ALTIN) sayılır; kovaların has toplamı = raporun altinHasGram'ı.
    [Fact]
    public async Task KR54_Stok_yasi_urunsuz_parti_kova_ve_maden_dagilimi_ayni_kume()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETKAR-RAPOR-2-TEST KR54");
        using var db = vt.Olustur(k);
        var now = DateTime.UtcNow;
        var motor = new MaliyetMotoru(db);
        motor.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.SayimFazla, 2m, k.UrunHasGrami, now.AddDays(-1));
        motor.PartiAc(k.TenantId, k.SubeId, Kimlik.Ulid(), PartiKaynagi.SayimFazla, 1m, 5m, now.AddDays(-1));   // ürünü yok
        await db.SaveChangesAsync();

        var servis = new Kuyumcu.Api.Modules.Rapor.StokYaslandirmaServisi(db,
            new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor()));
        var r = J(await servis.Yaslandirma(k.Islem, null));
        var beklenen = 2m * k.UrunHasGrami + 5m;
        Assert.Equal(beklenen, D(r.GetProperty("kovalar")[0], "hasGram"));
        Assert.Equal(beklenen, D(r, "altinHasGram"));
        Assert.Equal(D(r, "altinHasGram"),
            r.GetProperty("kovalar").EnumerateArray().Sum(x => x.GetProperty("hasGram").GetDecimal()));
    }
}
