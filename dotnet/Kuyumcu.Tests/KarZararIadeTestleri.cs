using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Maliyet;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 35 FAZ A — KÂR-ZARARIN ÜÇ KANITLI ÖLÇÜM HATASI
//
// KARAKTERİZASYON (paket ÖNCESİ ölçülen gerçek çıktı — Bertan koşulu):
//   • 4 adet satış (6.000 TL) + 2 adet iade (3.000 TL) + 500 TL işçilik
//       → hasilatTl 6.000 · maliyetTl 4.000 · karTl 2.000 · hasilatHas 0,8788
//     Yani iade edilen mal kâr-zararda HÂLÂ TAM KÂR olarak duruyordu.
//   • Takas farkı 2.000 TL (verilen malın maliyeti 1.000 TL)
//       → hasilatTl 2.000 · maliyetTl 1.000 · karTl 1.000
//     Yani el değiştiren brüt değer bilinmediği hâlde FARK ciro sayılıyordu.
// Aşağıdaki testler bu iki çıktının DÜZELMİŞ hâlini sabitler; rakamlar
// yukarıdaki ölçümle birebir karşılaştırılabilir (düzeltme mi bozma mı sorusu
// bu yüzden cevaplanabilir).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class KarZararIadeTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MSatis.StornoServisi Storno(KuyumcuDbContext db) =>
        new(db, new DenetimServisi(db, new HttpContextAccessor()),
            NullLogger<MSatis.StornoServisi>.Instance);

    private static KarZararServisi Rapor(KuyumcuDbContext db) => new(db);

    private static DateOnly Bugun => DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));

    private static System.Text.Json.JsonElement J(object o) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o)).RootElement;

    private static decimal D(System.Text.Json.JsonElement e, params string[] yol)
    {
        foreach (var a in yol) e = e.GetProperty(a);
        return e.ValueKind == System.Text.Json.JsonValueKind.Null ? 0m : e.GetDecimal();
    }

    private static async Task<string> AlisSatis(
        KuyumcuDbContext db, Kiraci k, decimal alisAdet, decimal alisFiyat,
        decimal satisAdet, decimal satisFiyat)
    {
        var s = Satis(db);
        await s.Alis(new MSatis.AlisOlusturDto
        {
            UrunId = k.UrunId, Adet = alisAdet, BirimFiyatTl = alisFiyat,
            KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);
        await s.Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = satisAdet, BirimFiyatTl = satisFiyat,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);
        return await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS")
            .OrderByDescending(h => h.Olusturuldu).Select(h => h.Id).FirstAsync();
    }

    private static Task Iade(KuyumcuDbContext db, Kiraci k, string satisId, decimal adet) =>
        Satis(db).SatisIade(new MSatis.IadeDto
        {
            KaynakHareketId = satisId, Adet = adet,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId,
        }, k.Islem);

    // ── 1. İADE ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Tamami_iade_edilen_mal_kar_birakmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KZ35-tam-iade");
        using var db = vt.Olustur(k);

        var satisId = await AlisSatis(db, k, 2m, 1_000m, 2m, 1_500m);
        await Iade(db, k, satisId, 2m);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));

        // ⚠ İKİ BACAK AYRI AYRI: yalnız karTl==0 assert etmek, hasılat ve
        // maliyetin AYNI MİKTARDA yanlış olduğu hâli de yeşil gösterirdi.
        Assert.Equal(3_000m, D(r, "brutHasilatTl"));
        Assert.Equal(3_000m, D(r, "iadeTl"));
        Assert.Equal(0m, D(r, "hasilatTl"));           // brüt − iade

        Assert.Equal(2_000m, D(r, "brutMaliyetTl"));
        Assert.Equal(2_000m, D(r, "iadeMaliyetTl"));
        Assert.Equal(0m, D(r, "maliyetTl"));           // maliyet de geri çıktı

        Assert.Equal(0m, D(r, "karTl"));
        Assert.Equal(0m, D(r, "donemKariTl"));
        Assert.Equal(0m, D(r, "hasilatHas"));
        // Veri VAR ama net sıfır: güvenilirlik "VERI_YOK" olmamalı.
        Assert.Equal("TAM", r.GetProperty("guvenilirlik").GetString());
    }

    [Fact]
    public async Task Kismi_iade_maliyeti_orantili_duser()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KZ35-kismi-iade");
        using var db = vt.Olustur(k);

        var satisId = await AlisSatis(db, k, 4m, 1_000m, 4m, 1_500m);
        await Iade(db, k, satisId, 1m);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));

        Assert.Equal(6_000m, D(r, "brutHasilatTl"));
        Assert.Equal(1_500m, D(r, "iadeTl"));
        Assert.Equal(4_500m, D(r, "hasilatTl"));
        Assert.Equal(4_000m, D(r, "brutMaliyetTl"));
        Assert.Equal(1_000m, D(r, "iadeMaliyetTl"));   // 1 adet × 1.000
        Assert.Equal(3_000m, D(r, "maliyetTl"));
        Assert.Equal(1_500m, D(r, "karTl"));
    }

    [Fact]
    public async Task Iade_CIKTIGI_maliyetle_geri_alinir()
    {
        // Fikstür bilinçli: iadeden ÖNCE FARKLI birim maliyetli ikinci parti
        // girer. "Bugünkü maliyetle yeniden değerleme" mutasyonu 15.000 üretir,
        // doğrusu 5.000'dir — iki maliyet eşit olsaydı test ayırt edemezdi.
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KZ35-iade-maliyeti");
        using var db = vt.Olustur(k);

        var satisId = await AlisSatis(db, k, 5m, 1_000m, 5m, 2_000m);

        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            UrunId = k.UrunId, Adet = 5m, BirimFiyatTl = 3_000m,   // pahalı ikinci parti
            KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

        await Iade(db, k, satisId, 5m);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));

        Assert.Equal(5_000m, D(r, "iadeMaliyetTl"));   // 5 × 1.000 (ÇIKTIĞI maliyet)
        Assert.NotEqual(15_000m, D(r, "iadeMaliyetTl"));
        Assert.Equal(0m, D(r, "maliyetTl"));
        Assert.Equal(0m, D(r, "karTl"));
    }

    // ── 2. TAKAS ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Takas_farki_hasilata_KATILMAZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "KZ35-takas"), 2_000m);
        using var db = vt.Olustur(k);

        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            UrunId = k.UrunId, Adet = 2m, BirimFiyatTl = 1_000m,
            KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

        // eski 10 g K22 (9,16 g has) × 1.000 = 9.160 · yeni 11.160 → fark +2.000
        await Satis(db).Takas(new MSatis.TakasDto
        {
            Satici = ParaDemoTestVerisi.TakasSaticisi,   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
            CariId = k.CariId,
            EskiUrunAyarId = "K22", EskiUrunGram = 10m, EskiUrunBirimFiyatTlHasGram = 1_000m,
            YeniUrunId = k.UrunId, YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = 11_160m,
            KasaId = k.KasaId, OdemeTuru = "NAKIT", KurSnapshotId = k.KurId,
        }, k.Islem);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));

        Assert.Equal(0m, D(r, "hasilatTl"));           // paket öncesi 2.000 idi
        Assert.Equal(2_000m, D(r, "takasFarkTl"));     // ayrı kalem
        Assert.Equal(0m, D(r, "maliyetTl"));           // hasılatı sayılmayanın maliyeti de sayılmaz
        Assert.Equal(1, r.GetProperty("takasAdedi").GetInt32());
        Assert.Contains("brüt değer", r.GetProperty("takasNotu").GetString()!);
    }

    [Fact]
    public async Task Negatif_takas_farki_hasilati_azaltmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "KZ35-takas-negatif"), 2_000m);
        using var db = vt.Olustur(k);

        var satisId = await AlisSatis(db, k, 2m, 1_000m, 1m, 5_000m);
        Assert.NotEmpty(satisId);

        // eski 9.160 TL değerinde, yeni 4.160 TL → fark −5.000 (tediye)
        await Satis(db).Takas(new MSatis.TakasDto
        {
            Satici = ParaDemoTestVerisi.TakasSaticisi,   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
            CariId = k.CariId,
            EskiUrunAyarId = "K22", EskiUrunGram = 10m, EskiUrunBirimFiyatTlHasGram = 1_000m,
            YeniUrunId = k.UrunId, YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = 4_160m,
            KurSnapshotId = k.KurId,
        }, k.Islem);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));

        Assert.Equal(5_000m, D(r, "hasilatTl"));        // yalnız gerçek satış
        Assert.Equal(-5_000m, D(r, "takasFarkTl"));     // negatif fark AYRI durur
    }

    // ── 3. İŞÇİLİK ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Iscilik_geliri_ayri_kalemdir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KZ35-iscilik");
        using var db = vt.Olustur(k);

        await AlisSatis(db, k, 1m, 1_000m, 1m, 2_000m);
        await Satis(db).Iscilik(new MSatis.IscilikDto
        {
            CariId = k.CariId, IscilikTl = 500m, OdemeTuru = "NAKIT",
            KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));

        Assert.Equal(2_000m, D(r, "hasilatTl"));      // işçilik hasılata GİRMEZ
        Assert.Equal(500m, D(r, "iscilikTl"));        // ayrı kalem
        Assert.Equal(1_000m, D(r, "karTl"));          // brüt kâr işçiliksiz
        Assert.Equal(1_500m, D(r, "donemKariTl"));    // dönem kârı işçilikli
        // Marj işçilikle bozulmaz: 1.000 / 2.000 = %50
        Assert.Equal(50m, D(r, "karMarjiYuzde"));
    }

    [Fact]
    public async Task Kar_koprusu_toplami_donem_karina_esittir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KZ35-kopru");
        using var db = vt.Olustur(k);

        var satisId = await AlisSatis(db, k, 4m, 1_000m, 4m, 1_500m);
        await Iade(db, k, satisId, 1m);
        await Satis(db).Iscilik(new MSatis.IscilikDto
        {
            CariId = k.CariId, IscilikTl = 750m, OdemeTuru = "NAKIT",
            KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));
        var kopru = r.GetProperty("karKoprusu").EnumerateArray().ToList();

        Assert.Contains(kopru, x => x.GetProperty("kod").GetString() == "IADE");
        var toplam = kopru.Where(x => !x.GetProperty("araToplam").GetBoolean())
                          .Sum(x => x.GetProperty("tutarTl").GetDecimal());

        Assert.Equal(D(r, "donemKariTl"), toplam);    // kuruş sapması YOK
        Assert.Equal(2_250m, toplam);                 // 6.000 −1.500 −4.000 +1.000 +750
    }

    // ── 4. GÜVENİLİRLİK ÜÇ UÇTA DA ──────────────────────────────────────────

    [Fact]
    public async Task UrunBazli_guvenilirlik_dondurur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KZ35-urun-guven");
        using var db = vt.Olustur(k);

        // Maliyeti BİLİNMEYEN devir partisi → satış maliyetsiz kalır.
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId,
            PartiKaynagi.Devir, 10m, k.UrunHasGrami, DateTime.UtcNow.AddDays(-1));
        await db.SaveChangesAsync();

        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 2_000m,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

        var r = J(await Rapor(db).UrunBazli(k.Islem, Bugun, Bugun, null, 50));

        Assert.Equal("DUSUK", r.GetProperty("guvenilirlik").GetString());
        var satir = r.GetProperty("satirlar")[0];
        Assert.Equal("DUSUK", satir.GetProperty("guvenilirlik").GetString());
        Assert.Equal(1m, satir.GetProperty("maliyetiBilinmeyenAdet").GetDecimal());
    }

    [Fact]
    public async Task StokDegerlemesi_guvenilirlik_dondurur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KZ35-stok-guven");
        using var db = vt.Olustur(k);

        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            UrunId = k.UrunId, Adet = 4m, BirimFiyatTl = 2_500m,
            KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

        var tam = J(await Rapor(db).StokDegerlemesi(k.Islem, null));
        Assert.Equal("TAM", tam.GetProperty("guvenilirlik").GetString());

        // Maliyeti bilinmeyen devir partisi eklenince ölçek düşer.
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId,
            PartiKaynagi.Devir, 40m, k.UrunHasGrami, DateTime.UtcNow.AddDays(-1));
        await db.SaveChangesAsync();

        var dusuk = J(await Rapor(db).StokDegerlemesi(k.Islem, null));
        Assert.Equal("DUSUK", dusuk.GetProperty("guvenilirlik").GetString());
    }

    [Fact]
    public async Task Konsinye_urun_stok_degerlemesine_KATILMAZ()
    {
        // Mevcut kuralın regresyonu: başkasının malı bizim varlığımız değildir.
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KZ35-konsinye");
        using var db = vt.Olustur(k);

        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            UrunId = k.UrunId, Adet = 4m, BirimFiyatTl = 2_500m,
            KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

        using (var sistem = vt.OlusturSistem())
        {
            var u = sistem.Urunler.Single(x => x.Id == k.UrunId);
            u.Konsinye = true;
            sistem.SaveChanges();
        }

        using var db2 = vt.Olustur(k);
        var r = J(await Rapor(db2).StokDegerlemesi(k.Islem, null));

        Assert.Equal(0m, D(r, "stokMaliyetiTl"));
        Assert.Equal(1, r.GetProperty("konsinyeUrunSayisi").GetInt32());
    }

    // ── 5. KARŞILAŞTIRMA ────────────────────────────────────────────────────

    [Fact]
    public async Task Karsilastirma_donemi_esit_gun_sayisidir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KZ35-karsilastirma");
        using var db = vt.Olustur(k);

        var bas = Bugun.AddDays(-14);          // 15 günlük dönem
        var r = J(await Rapor(db).Donem(k.Islem, bas, Bugun, null, "ONCEKI_DONEM"));

        var onceki = r.GetProperty("onceki");
        Assert.Equal(bas.AddDays(-15).ToString("yyyy-MM-dd"), onceki.GetProperty("baslangic").GetString());
        Assert.Equal(bas.AddDays(-1).ToString("yyyy-MM-dd"), onceki.GetProperty("bitis").GetString());
        Assert.Equal(15, onceki.GetProperty("gunSayisi").GetInt32());
        Assert.Equal(15, r.GetProperty("gunSayisi").GetInt32());

        var gecen = J(await Rapor(db).Donem(k.Islem, bas, Bugun, null, "GECEN_YIL"));
        var gy = gecen.GetProperty("onceki");
        Assert.Equal(bas.AddYears(-1).ToString("yyyy-MM-dd"), gy.GetProperty("baslangic").GetString());
        Assert.Equal(15, gy.GetProperty("gunSayisi").GetInt32());
    }

    [Fact]
    public async Task Karsilastirma_varsayilan_KAPALI_ve_gecersiz_deger_REDDEDILIR()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KZ35-karsilastirma-kapali");
        using var db = vt.Olustur(k);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal(System.Text.Json.JsonValueKind.Null, r.GetProperty("onceki").ValueKind);

        // Kapalı liste kontrolü SERVİS İÇİNDE: controller [RegularExpression]
        // doğrudan servis çağrısında görünmez (Aşkın kuralı).
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Rapor(db).Donem(k.Islem, Bugun, Bugun, null, "GECEN_HAFTA"));
        Assert.Contains("ONCEKI_DONEM", hata.Message);
    }

    // ── 6. STORNO (paket 48-A süzgeci) ──────────────────────────────────────

    [Fact]
    public async Task Storno_cifti_ciroyu_ve_adedi_DEGISTIRMEZ_ayri_gorunur()
    {
        // ⚠ SATIŞ stornolanmaz (StornoServisi: düzeltme yolu satış iadesidir);
        // stornolanabilir türlerden ISCILIK ile ölçülür. Kural aynı: çift
        // toplamlara KATILMAZ, ayrı satırda görünür.
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KZ35-storno");
        using var db = vt.Olustur(k);

        await AlisSatis(db, k, 4m, 1_000m, 1m, 2_000m);          // kalıcı satış
        await Satis(db).Iscilik(new MSatis.IscilikDto
        {
            CariId = k.CariId, IscilikTl = 500m, OdemeTuru = "NAKIT",
            KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
        await Satis(db).Iscilik(new MSatis.IscilikDto
        {
            CariId = k.CariId, IscilikTl = 900m, OdemeTuru = "NAKIT",
            KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        var stornolanacak = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "ISCILIK" && h.TlTutari == 900m)
            .Select(h => h.Id).SingleAsync();

        using var db2 = vt.Olustur(k);
        await Storno(db2).Storno(stornolanacak, new MSatis.StornoDto
        {
            Gerekce = "Kasiyer yanlış cariye işledi — mutabakatta düzeltildi",
            KurSnapshotId = k.KurId, KasaId = k.KasaId,
        }, k.Islem);

        using var db3 = vt.Olustur(k);
        var r = J(await Rapor(db3).Donem(k.Islem, Bugun, Bugun, null));

        Assert.Equal(2_000m, D(r, "brutHasilatTl"));    // satış etkilenmedi
        Assert.Equal(1, r.GetProperty("satisAdedi").GetInt32());
        Assert.Equal(500m, D(r, "iscilikTl"));          // 900'lük çift DIŞLANDI
        // ⚠ Yalnız tutar assert etmek YETMEZ: storno çifti TL'de zaten
        // birbirini götürür. Ayırt edici ölçü ADETTİR (3 değil 1).
        Assert.Equal(1, r.GetProperty("iscilikAdedi").GetInt32());
        Assert.Equal(1, r.GetProperty("storno").GetProperty("adet").GetInt32());
        Assert.Equal(900m, D(r, "storno", "tutarTl"));
    }

    // ── 7. ŞEKİL REGRESYONU + HAS TANIMI ────────────────────────────────────

    [Fact]
    public async Task Yanit_sekli_KORUNUR_eski_alanlar_durur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KZ35-sekil");
        using var db = vt.Olustur(k);
        await AlisSatis(db, k, 1m, 1_000m, 1m, 2_000m);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));
        foreach (var alan in new[]
        {
            "baslangic", "bitis", "subeId", "satisAdedi", "hasilatTl", "maliyetTl", "karTl",
            "karMarjiYuzde", "hasilatHas", "maliyetHas", "karHas", "maliyetiBilinenAdet",
            "maliyetiBilinmeyenAdet", "kuruBulunamayanSatis", "guvenilirlik", "uyari",
        })
            Assert.True(r.TryGetProperty(alan, out _), $"Eski alan kayboldu: {alan}");

        var u = J(await Rapor(db).UrunBazli(k.Islem, Bugun, Bugun, null, 50));
        var satir = u.GetProperty("satirlar")[0];
        foreach (var alan in new[] { "urunId", "kod", "ad", "satisAdedi", "hasilatTl",
                                     "maliyetTl", "karTl", "karMarjiYuzde", "maliyetiBilinmeyenAdet" })
            Assert.True(satir.TryGetProperty(alan, out _), $"Eski ürün alanı kayboldu: {alan}");

        var s = J(await Rapor(db).StokDegerlemesi(k.Islem, null));
        foreach (var alan in new[] { "toplamAdet", "toplamHasGram", "maliyetliAdet", "maliyetsizAdet",
                                     "stokMaliyetiTl", "stokMaliyetiHas", "konsinyeUrunSayisi", "uyari" })
            Assert.True(s.TryGetProperty(alan, out _), $"Eski stok alanı kayboldu: {alan}");
    }

    [Fact]
    public async Task Urun_bazli_iade_dusulur_ve_HAS_kar_vardir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KZ35-urun-iade");
        using var db = vt.Olustur(k);

        var satisId = await AlisSatis(db, k, 4m, 1_000m, 4m, 1_500m);
        await Iade(db, k, satisId, 1m);

        var r = J(await Rapor(db).UrunBazli(k.Islem, Bugun, Bugun, null, 50));
        var satir = r.GetProperty("satirlar")[0];

        Assert.Equal(6_000m, satir.GetProperty("brutHasilatTl").GetDecimal());
        Assert.Equal(1_500m, satir.GetProperty("iadeTl").GetDecimal());
        Assert.Equal(4_500m, satir.GetProperty("hasilatTl").GetDecimal());
        Assert.Equal(3_000m, satir.GetProperty("maliyetTl").GetDecimal());
        Assert.Equal(1_500m, satir.GetProperty("karTl").GetDecimal());
        // HAS bacağı hiçbir kırılımda gizlenmez.
        Assert.True(satir.TryGetProperty("karHas", out var karHas));
        Assert.NotEqual(0m, karHas.GetDecimal());
    }

    [Fact]
    public async Task HAS_hasilati_defter_grami_ile_BIRLIKTE_beyan_edilir()
    {
        // QA turu 3 bulgusu: /raporlar aylık özeti (defter has gramı) ile
        // kâr-zarar (TL'nin has karşılığı) aynı satış için farklı sayı veriyordu.
        // İki büyüklük de artık TEK serviste, adıyla ve tanımıyla döner.
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KZ35-has-tanim");
        using var db = vt.Olustur(k);

        await AlisSatis(db, k, 1m, 1_000m, 1m, 4_000m);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));

        // 4.000 TL / 6.827,20 = 0,5859 g HAS karşılığı
        Assert.Equal(Math.Round(4_000m / 6_827.20m, 4), D(r, "hasilatHas"));
        // Defter grami: 10 g K22 ürün → 9,16 g has
        Assert.Equal(k.UrunHasGrami, D(r, "hasilatHasDefterGram"));
        Assert.Contains("defter", r.GetProperty("hasTanimNotu").GetString()!.ToLowerInvariant());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PAKET P-45 — DEFTER HAS GRAMI MADEN KIRILIMI
    //
    // KARAKTERİZASYON (paket ÖNCESİ ölçüldü): KarZararServisi.Olc içinde
    //   defterGram = satislar.Sum(|HasGrami|) − iadeler.Sum(|HasGrami|)
    // h.Maden HİÇ OKUNMUYORDU. QA fikstüründe ekran 44,4154 g basıyordu;
    // bu rakam 32,7604 g ALTIN + 11,6550 g GÜMÜŞ toplamıdır ve hiçbir şeyin
    // karşılığı değildir (1 g gümüş ile 1 g altın aynı şey değildir).
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Kiracıya GÜMÜŞ (925) bir ürün + stok ekler ve kimliğini döner.</summary>
    private static string GumusUrunEkle(KuyumcuDbContext db, Kiraci k, decimal gram = 10m)
    {
        var now = DateTime.UtcNow;
        var urunId = Kimlik.Ulid();
        var ayar = Kuyumcu.Core.Domain.Ayarlar.Bul("G925")!;
        var has = Kuyumcu.Core.Domain.HasHesap.HasGramHesapla(gram, 0m, "G925");
        db.Urunler.Add(new Urun
        {
            Id = urunId, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"G-{urunId[..8]}",
            Ad = "Test gümüş yüzük", AyarId = "G925", Milyem = ayar.Milyem,
            GramAgirlik = gram, TasAgirlik = 0m, HasGrami = has, IscilikTl = 0m,
            Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.Stoklar.Add(new Stok
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, UrunId = urunId,
            Adet = 100m, ToplamHasGram = 100m * has, Guncellendi = now,
        });
        db.SaveChanges();
        return urunId;
    }

    /// <summary>Verilen üründen alış + satış yapar, satış hareketinin kimliğini döner.</summary>
    private static async Task<string> AlisSatisUrun(
        KuyumcuDbContext db, Kiraci k, string urunId,
        decimal alisAdet, decimal alisFiyat, decimal satisAdet, decimal satisFiyat)
    {
        var s = Satis(db);
        await s.Alis(new MSatis.AlisOlusturDto
        {
            UrunId = urunId, Adet = alisAdet, BirimFiyatTl = alisFiyat,
            KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);
        await s.Satis(new MSatis.SatisOlusturDto
        {
            UrunId = urunId, Adet = satisAdet, BirimFiyatTl = satisFiyat,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);
        return await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS" && h.UrunId == urunId)
            .OrderByDescending(h => h.Olusturuldu).Select(h => h.Id).FirstAsync();
    }

    private static decimal MadenGrami(System.Text.Json.JsonElement dagilim, string maden)
    {
        foreach (var x in dagilim.EnumerateArray())
            if (x.GetProperty("maden").GetString() == maden)
                return x.GetProperty("toplamHas").GetDecimal();
        return 0m;
    }

    [Fact]
    public async Task P45_01_Defter_has_grami_altin_ve_gumusu_TOPLAMAZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P45-maden");
        using var db = vt.Olustur(k);

        var gumusId = GumusUrunEkle(db, k);
        await AlisSatis(db, k, 3m, 1_000m, 3m, 1_500m);                 // 3 × 9,16 g ALTIN
        await AlisSatisUrun(db, k, gumusId, 1m, 100m, 1m, 150m);        // 1 × 9,25 g GÜMÜŞ

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));
        var dagilim = r.GetProperty("hasDefterMadenDagilimi");

        var altin = 3m * k.UrunHasGrami;                                 // 27,48 g
        var gumus = Kuyumcu.Core.Domain.HasHesap.HasGramHesapla(10m, 0m, "G925"); // 9,25 g

        Assert.Equal(altin, MadenGrami(dagilim, "ALTIN"));
        Assert.Equal(gumus, MadenGrami(dagilim, "GUMUS"));

        // Birleşik rakam HİÇBİR alanda dönmez.
        Assert.Equal(altin, D(r, "hasilatHasDefterGram"));
        Assert.Equal(altin, D(r, "altinHasilatHasDefterGram"));
        Assert.DoesNotContain(
            (altin + gumus).ToString(System.Globalization.CultureInfo.InvariantCulture),
            System.Text.Json.JsonSerializer.Serialize(r));

        // Birden çok maden var → uyarı doğar.
        Assert.False(string.IsNullOrWhiteSpace(r.GetProperty("madenUyarisi").GetString()));
    }

    [Fact]
    public async Task P45_02_Altin_defter_grami_aylik_ozetle_BIREBIR_ayni()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P45-aylik");
        using var db = vt.Olustur(k);

        var gumusId = GumusUrunEkle(db, k);
        await AlisSatis(db, k, 3m, 1_000m, 3m, 1_500m);
        await AlisSatisUrun(db, k, gumusId, 1m, 100m, 1m, 150m);

        var kz = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));
        var aylik = J(await TestKurulum.RaporServisi(db).AylikOzet(Bugun.Year, Bugun.Month, k.Islem));

        var aylikAltin = MadenGrami(aylik.GetProperty("satis").GetProperty("madenDagilimi"), "ALTIN");
        Assert.True(Math.Abs(D(kz, "altinHasilatHasDefterGram") - aylikAltin) <= 0.000001m,
            $"kâr-zarar {D(kz, "altinHasilatHasDefterGram")} ≠ aylık özet {aylikAltin}");
        Assert.NotEqual(0m, aylikAltin);
    }

    [Fact]
    public async Task P45_03_Iade_maden_bazinda_dusulur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P45-iade");
        using var db = vt.Olustur(k);

        var gumusId = GumusUrunEkle(db, k);
        await AlisSatis(db, k, 3m, 1_000m, 3m, 1_500m);
        var gumusSatis = await AlisSatisUrun(db, k, gumusId, 2m, 100m, 2m, 150m);
        await Iade(db, k, gumusSatis, 1m);   // YALNIZ gümüş iade edildi

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));
        var dagilim = r.GetProperty("hasDefterMadenDagilimi");
        var gumusBirim = Kuyumcu.Core.Domain.HasHesap.HasGramHesapla(10m, 0m, "G925");

        // İade GÜMÜŞTEN düşer: 2 − 1 = 1 adet.
        Assert.Equal(gumusBirim, MadenGrami(dagilim, "GUMUS"));
        // ALTIN'a DOKUNMAZ (maden ayrımı yapılmasaydı altın tarafı da erirdi).
        Assert.Equal(3m * k.UrunHasGrami, MadenGrami(dagilim, "ALTIN"));
        Assert.Equal(3m * k.UrunHasGrami, D(r, "altinHasilatHasDefterGram"));
    }

    [Fact]
    public async Task P45_05_Tek_maden_varsa_uyari_cikmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P45-tek-maden");
        using var db = vt.Olustur(k);

        await AlisSatis(db, k, 2m, 1_000m, 2m, 1_500m);

        var r = J(await Rapor(db).Donem(k.Islem, Bugun, Bugun, null));

        Assert.Equal(1, r.GetProperty("hasDefterMadenDagilimi").GetArrayLength());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, r.GetProperty("madenUyarisi").ValueKind);
        Assert.Equal(2m * k.UrunHasGrami, D(r, "altinHasilatHasDefterGram"));
    }
}
