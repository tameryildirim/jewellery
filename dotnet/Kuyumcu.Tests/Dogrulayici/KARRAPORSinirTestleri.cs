using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Maliyet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KAR-RAPOR — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT LİTERALDİR (üretim
// kodundan hesaplanmaz). Paket kârı fail-closed yaptı (maliyeti bilinmeyen satış
// kâra girmez). Bu dosya o yolun sınırlarını sınar: sıfır kâr · eksi kâr ·
// kuruş yuvarlaması (0,005 sınırı, projenin AwayFromZero kuralı) · has 4 hane ·
// çok büyük tutar · boş/null adet · adet karşılama toleransı (0,001 eksik).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class KARRAPORSinirTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static DateOnly Bugun => DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));

    private static System.Text.Json.JsonElement J(object o) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o)).RootElement;

    private static decimal D(System.Text.Json.JsonElement e, string ad) => e.GetProperty(ad).GetDecimal();

    private static Task Alis(KuyumcuDbContext db, Kiraci k, decimal adet, decimal birim) =>
        Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            UrunId = k.UrunId, Adet = adet, BirimFiyatTl = birim,
            KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

    private static Task Sat(KuyumcuDbContext db, Kiraci k, decimal adet, decimal birim) =>
        Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = adet, BirimFiyatTl = birim,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

    private Kiraci Kur(KuyumcuDbContext kurulum, string ad) =>
        TestKurulum.KiraciKur(kurulum, "PAKETKAR-RAPOR-TEST " + ad);

    // ── SIFIR: maliyet = satış → kâr 0 ve TAM (null DEĞİL — biliniyor, sıfır)
    [Fact]
    public async Task KRS01_Sifir_kar_TAM_null_degil()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "sifir");
        using var db = vt.Olustur(k);
        await Alis(db, k, 1m, 1_000m);
        await Sat(db, k, 1m, 1_000m);

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal(0m, D(r, "karTl"));
        Assert.Equal("TAM", r.GetProperty("karDurumu").GetString());
        Assert.Equal(0, r.GetProperty("maliyetiEksikSatisAdedi").GetInt32());
    }

    // ── EKSİ: maliyetin altında satış → kâr −500 (işaret doğru), aylık özette de −500
    [Fact]
    public async Task KRS02_Eksi_kar_isareti_dogru_ve_aylik_ozette_ayni()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "eksi");
        using var db = vt.Olustur(k);
        await Alis(db, k, 1m, 2_000m);
        await Sat(db, k, 1m, 1_500m);

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal(-500m, D(r, "karTl"));
        Assert.Equal("TAM", r.GetProperty("karDurumu").GetString());

        var yerel = Zaman.UtcdenYerele(DateTime.UtcNow);
        var a = J(await TestKurulum.RaporServisi(db, new KarZararServisi(db)).AylikOzet(yerel.Year, yerel.Month, k.Islem));
        Assert.Equal(-500m, D(a, "karTl"));
        Assert.Equal(1_500m, D(a.GetProperty("satis"), "toplamTl"));
    }

    // ── KURUŞ: maliyet 1.000,875 (decimal(15,4)), satış 1.501 → kesin kâr 500,125.
    //    Proje kuralı AwayFromZero → 500,13 (bankacı yuvarlaması 500,12 verirdi).
    [Fact]
    public async Task KRS03_Kurus_yarim_sinirinda_AwayFromZero()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "kurus");
        using var db = vt.Olustur(k);
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId,
            PartiKaynagi.SayimFazla, 1m, k.UrunHasGrami, DateTime.UtcNow.AddDays(-1),
            birimMaliyetTl: 1_000.875m);
        await db.SaveChangesAsync();
        await Sat(db, k, 1m, 1_501m);

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("TAM", r.GetProperty("karDurumu").GetString());
        // KAR-RAPOR-2 (ekip oyu Bertan + Mustafa, 02.10): TEK KURAL "kâr = yuvarlanmış bileşenlerin
        // farkı". Maliyet 1.000,875 → 1.000,88 (AwayFromZero); kâr 1.501,00 − 1.000,88 = 500,12.
        // Eski mühür (kesin değer 500,125 → 500,13) ekranda hasılat − maliyet ≠ kâr bırakıyordu.
        Assert.Equal(1_000.88m, D(r, "maliyetTl"));
        Assert.Equal(500.12m, D(r, "karTl"));
        Assert.Equal(D(r, "hasilatTl") - D(r, "maliyetTl"), D(r, "karTl"));
    }

    // ── HAS 4 HANE: has kârı en çok 4 ondalık taşır
    [Fact]
    public async Task KRS04_Has_kari_4_haneye_yuvarli()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "has");
        using var db = vt.Olustur(k);
        await Alis(db, k, 3m, 1_000.37m);
        await Sat(db, k, 1m, 1_777.77m);

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        var kh = D(r, "karHas");
        Assert.Equal(kh, Math.Round(kh, 4));
        Assert.True(kh > 0m);
    }

    // ── ÇOK BÜYÜK TUTAR: 999.999.999,99 satış, 500.000.000 maliyet → 499.999.999,99
    [Fact]
    public async Task KRS05_Cok_buyuk_tutar_kurusu_kurusuna()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "buyuk");
        using var db = vt.Olustur(k);
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId,
            PartiKaynagi.SayimFazla, 1m, k.UrunHasGrami, DateTime.UtcNow.AddDays(-1),
            birimMaliyetTl: 500_000_000m);
        await db.SaveChangesAsync();
        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 999_999_999.99m,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId,
            Masak = new Kuyumcu.Api.Modules.Masak.MasakBilgiDto
            {
                BelgeTuru = "PASAPORT", BelgeNo = "U1234567", AdSoyad = "John Smith", Uyruk = "FRA",
                DogumTarihi = new DateOnly(1980, 1, 1), Adres = "Paris",
                IslemAmaci = "Ticari", Aciklama = "Sinir testi icin buyuk tutarli ticari satis islemi",
                FonKaynagi = "Ticari gelir", EkBelgeReferansi = "EK-1",
            },
        }, k.Islem);

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal(999_999_999.99m, D(r, "hasilatTl"));
        Assert.Equal(499_999_999.99m, D(r, "karTl"));
        Assert.Equal("TAM", r.GetProperty("karDurumu").GetString());
    }

    // ── BOŞ/NULL: adet null + satır yok → bilinmez; boş girdiler → boş küme; VERI_YOK
    [Fact]
    public void KRS06_Bos_ve_null_girdi()
    {
        Assert.Empty(KarZararServisi.MaliyetiBilinenler([], []));
        Assert.Empty(KarZararServisi.MaliyetiBilinenler([("x", null)], []));
        Assert.Empty(KarZararServisi.MaliyetiBilinenler([("x", 1m)], [(null, 1m, 100m)]));
        Assert.Equal("VERI_YOK", KarZararServisi.KarDurumuBelirle(0, 0));
    }

    // ── ADET TOLERANSI: 1,000 adetin 0,999'u karşılandıysa satış EKSİKTİR (0,001 > 0,0005)
    [Fact]
    public void KRS07_Bir_binde_eksik_karsilama_eksik_sayilir()
    {
        Assert.Empty(KarZararServisi.MaliyetiBilinenler([("x", 1.000m)], [("x", 0.999m, 100m)]));
        Assert.Contains("y", KarZararServisi.MaliyetiBilinenler([("y", 1.000m)], [("y", 0.5m, 100m), ("y", 0.5m, 0m)]));
    }

    // ── SIFIR MALİYETLİ parti (bilinçli 0 TL) BİLİNEN sayılır; NULL maliyet bilinmez
    [Fact]
    public void KRS08_Sifir_maliyet_bilinir_null_bilinmez()
    {
        Assert.Contains("a", KarZararServisi.MaliyetiBilinenler([("a", 1m)], [("a", 1m, 0m)]));
        Assert.Empty(KarZararServisi.MaliyetiBilinenler([("b", 1m)], [("b", 1m, null)]));
    }

    // ═══ TUR 2 HASIM EKLERİ (02.10) ════════════════════════════════════════════

    // ── KÖK BULGU (aylık özet): satışların HİÇBİRİNİN maliyeti yokken aylık "kâr"
    //    satış toplamına eşit OLMAMALI — null + HESAPLANAMADI + eksik adet/tutar.
    [Fact]
    public async Task KRS09_Aylik_ozet_hepsi_maliyetsiz_kar_null_satisa_esit_degil()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "aylik-null");
        using var db = vt.Olustur(k);
        await Sat(db, k, 1m, 1_234.56m);
        await Sat(db, k, 1m, 765.44m);

        var yerel = Zaman.UtcdenYerele(DateTime.UtcNow);
        var a = J(await TestKurulum.RaporServisi(db, new KarZararServisi(db)).AylikOzet(yerel.Year, yerel.Month, k.Islem));
        Assert.Equal(2_000m, D(a.GetProperty("satis"), "toplamTl"));
        Assert.Equal(System.Text.Json.JsonValueKind.Null, a.GetProperty("karTl").ValueKind);
        Assert.Equal("HESAPLANAMADI", a.GetProperty("karDurumu").GetString());
        Assert.Equal("HESAPLANAMADI", a.GetProperty("karKaynagi").GetString());
        Assert.Equal(2, a.GetProperty("maliyetiEksikSatisAdedi").GetInt32());
        Assert.Equal(2_000m, D(a, "maliyetiEksikTutarTl"));
    }

    // ── TOLERANS SINIRI tam 0,0005: 0,9995 karşılanan → BİLİNİR; 0,9994 → EKSİK
    [Fact]
    public void KRS10_Tolerans_tam_sinir_0_0005()
    {
        Assert.Contains("a", KarZararServisi.MaliyetiBilinenler([("a", 1m)], [("a", 0.9995m, 10m)]));
        Assert.Empty(KarZararServisi.MaliyetiBilinenler([("b", 1m)], [("b", 0.9994m, 10m)]));
        // Fazla karşılama (iade satırı negatif adet) mutlak değerle sayılır.
        Assert.Contains("c", KarZararServisi.MaliyetiBilinenler([("c", -1m)], [("c", -1m, 10m)]));
    }

    // ── KARIŞIK: biri bilinen biri eksik satır içeren tek satış → TAMAMI eksik
    [Fact]
    public void KRS11_Satirlarindan_biri_null_ise_satis_eksik()
    {
        Assert.Empty(KarZararServisi.MaliyetiBilinenler([("x", 2m)], [("x", 1m, 100m), ("x", 1m, null)]));
        Assert.Equal("KISMI", KarZararServisi.KarDurumuBelirle(1, 1));
        Assert.Equal("HESAPLANAMADI", KarZararServisi.KarDurumuBelirle(0, 1));
        Assert.Equal("TAM", KarZararServisi.KarDurumuBelirle(5, 0));
    }

    // ── KISMI + EKSİ: bilinen satış zararda, eksik satış ayrı → kâr −250,00 (eksik tutar kâra karışmaz)
    [Fact]
    public async Task KRS12_Kismi_eksi_kar_eksik_tutar_karismaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "kismi-eksi");
        using var db = vt.Olustur(k);
        await Alis(db, k, 1m, 1_250m);
        await Sat(db, k, 1m, 1_000m);         // bilinen, zarar 250
        await Sat(db, k, 1m, 9_999.99m);      // partisiz → eksik

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal(-250m, D(r, "karTl"));
        Assert.Equal("KISMI", r.GetProperty("karDurumu").GetString());
        Assert.Equal(9_999.99m, D(r, "maliyetiEksikTutarTl"));
        Assert.Equal(10_999.99m, D(r, "hasilatTl"));
        Assert.Equal(-25m, D(r, "karMarjiYuzde"));
    }
}
