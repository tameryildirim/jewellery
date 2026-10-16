using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-23 (R-17) — ÖDEME KALEMİ TABLOSU, FAZ 1
//
// Sınanan çekirdek:
//   • Fişin HER bacağı (VERESIYE dâhil) kalıcı bir kalem satırı doğurur
//   • Kalem toplamı fiş toplamına EŞİTTİR  → FIS_TOPLAM_ODEME_KALEMI_FARKI = 0
//   • Eski alanlar (odeme_turu · nakit_tutar · veresiye_tutar) AYNEN yazılır
//     → TevsikKapisi (459 s. VUK) ve MASAK kümülatifi kaynağını DEĞİŞTİRMEDİ
//   • Storno kalemi PASİFE ALIR, silmez (finansal varlıkta hard DELETE yok)
//   • Kalem tablosu kiracı süzgecinin içindedir
// ═════════════════════════════════════════════════════════════════════════════

// ── Saf testler: veritabanı yok ──────────────────────────────────────────────
public sealed class OdemeAraciTestleri
{
    [Fact]
    public void Arac_listesi_OdemeBacagiDto_regexi_ile_AYNIDIR()
    {
        // ⚠ İki listenin ayrışması, sunucunun KABUL ETTİĞİ bir bacağın kalem
        // olarak yazılamaması demektir; o fişte kalem toplamı ≠ fiş toplamı
        // çıkar ve sayaç (FIS_TOPLAM_ODEME_KALEMI_FARKI) sessizce bozulur.
        // Bu yüzden ölçüt DTO'nun KENDİ regex'inden okunur, elle yazılmaz.
        var desen = typeof(MSatis.OdemeBacagiDto)
            .GetProperty(nameof(MSatis.OdemeBacagiDto.Arac))!
            .GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations
                .RegularExpressionAttribute), false)
            .Cast<System.ComponentModel.DataAnnotations.RegularExpressionAttribute>()
            .Single().Pattern;

        var dtoAraclar = desen.Split('|').OrderBy(x => x).ToArray();
        Assert.Equal(dtoAraclar, OdemeAraci.Tumu.OrderBy(x => x).ToArray());
    }

    [Fact]
    public void VERESIYE_kasaya_GIRMEZ_digerleri_girer()
    {
        // Veresiye bacağının kasa hareketi YOKTUR; kalem yazarken kasa/POS
        // adımı yalnız bu ölçütle atlanır.
        Assert.False(OdemeAraci.KasayaGirer(OdemeAraci.Veresiye));
        // PAKET B-4 (B4-19): eski altın bacağının da kasa satırı YOKTUR.
        Assert.False(OdemeAraci.KasayaGirer(OdemeAraci.Altin));
        foreach (var a in OdemeAraci.Tumu.Where(a => a != OdemeAraci.Veresiye && a != OdemeAraci.Altin))
            Assert.True(OdemeAraci.KasayaGirer(a));
    }

    [Fact]
    public void Gecersiz_arac_listede_YOKTUR()
    {
        // PAKET B-4 (B4-2): ALTIN artık GEÇERLİ bir araçtır (B4AltinBacagiSafTestleri
        // .OdemeKalemi_Altin_Araci_Yazilabilir_saf); özet etiketler araç DEĞİLDİR.
        Assert.False(OdemeAraci.Gecerli("DIGER"));
        Assert.False(OdemeAraci.Gecerli("KARMA"));
        Assert.False(OdemeAraci.Gecerli(null));
        Assert.True(OdemeAraci.Gecerli(OdemeAraci.Nakit));
    }
}

// ── Veritabanı testleri ──────────────────────────────────────────────────────
[Collection("veritabani")]
public sealed class P23OdemeKalemiTestleri(TestVeritabani vt)
{
    private static SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<SatisServisi>.Instance);

    private static StornoServisi Storno(KuyumcuDbContext db) =>
        new(db, new DenetimServisi(db, new HttpContextAccessor()), NullLogger<StornoServisi>.Instance);

    private static MSatis.OdemeBacagiDto B(Kiraci k, string arac, decimal tutar) =>
        new() { Arac = arac, TutarTl = tutar, KasaId = arac == "VERESIYE" ? null : k.KasaId };

    private static MSatis.SatisFisiDto Fis(
        Kiraci k, decimal birimFiyat, List<MSatis.OdemeBacagiDto> odemeler, int kalemAdet = 1) => new()
    {
        CariId = k.CariId,
        Kalemler = Enumerable.Range(0, kalemAdet)
            .Select(_ => new MSatis.SatisFisKalemDto
            {
                UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = birimFiyat,
            }).ToList(),
        Odemeler = odemeler,
        KurSnapshotId = k.KurId,
    };

    private List<OdemeKalemi> Kalemler(Kiraci k)
    {
        using var kontrol = vt.OlusturSistem();
        return kontrol.OdemeKalemleri.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId)
            .OrderBy(x => x.Sira).ToList();
    }

    [Fact]
    public async Task Her_bacak_VERESIYE_dahil_KALEM_yazar()
    {
        // Kusurun kendisi: veresiye bacağının kasa hareketi olmadığı için
        // kırılım kasa defterinden TAMAMLANAMIYORDU. Kalem tablosu onu da yazar.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23 bacak-kalem");
        using var db = vt.Olustur(k);

        await Satis(db).SatisFisi(
            Fis(k, 5_000m, [B(k, "NAKIT", 6_000m), B(k, "KART", 4_000m), B(k, "VERESIYE", 5_000m)],
                kalemAdet: 3), k.Islem);

        var kalemler = Kalemler(k);
        Assert.Equal(3, kalemler.Count);
        Assert.Equal(["NAKIT", "KART", "VERESIYE"], kalemler.Select(x => x.Arac).ToArray());
        Assert.Equal([0, 1, 2], kalemler.Select(x => x.Sira).ToArray());   // girdi sırası KORUNUR
        Assert.Equal(6_000m, kalemler[0].TutarTl);
        Assert.Equal(4_000m, kalemler[1].TutarTl);
        Assert.Equal(5_000m, kalemler[2].TutarTl);
    }

    [Fact]
    public async Task Kalem_toplami_fis_toplamina_ESITTIR()
    {
        // denetim.sh sayacı FIS_TOPLAM_ODEME_KALEMI_FARKI = 0 bu kuralın
        // canlı ölçümüdür; burada aynı soru testte sorulur.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23 toplam farkı");
        using var db = vt.Olustur(k);

        await Satis(db).SatisFisi(
            Fis(k, 5_000m, [B(k, "NAKIT", 6_000m), B(k, "KART", 4_000m), B(k, "VERESIYE", 5_000m)],
                kalemAdet: 3), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var fisToplam = kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.FisId != null).Sum(h => h.TlTutari);
        var kalemToplam = kontrol.OdemeKalemleri.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId && x.Aktif).Sum(x => x.TutarTl);

        Assert.Equal(15_000m, fisToplam);
        Assert.Equal(fisToplam, kalemToplam);
    }

    [Fact]
    public async Task VERESIYE_kaleminde_kasa_bagi_YOKTUR_digerlerinde_VARDIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23 kasa bağı");
        using var db = vt.Olustur(k);

        await Satis(db).SatisFisi(
            Fis(k, 10_000m, [B(k, "NAKIT", 6_000m), B(k, "VERESIYE", 4_000m)]), k.Islem);

        var kalemler = Kalemler(k);
        var nakit = kalemler.Single(x => x.Arac == "NAKIT");
        var veresiye = kalemler.Single(x => x.Arac == "VERESIYE");

        Assert.Equal(k.KasaId, nakit.KasaId);
        Assert.False(string.IsNullOrEmpty(nakit.KasaHareketId));   // kasa defterine geri iz
        Assert.Null(veresiye.KasaId);
        Assert.Null(veresiye.KasaHareketId);
    }

    [Fact]
    public async Task Kalem_UYDURULMAZ_bilinmiyor_bayragi_FALSE_dogar()
    {
        // Anayasa md.1: canlı sepetten doğan kalem ÖLÇÜLMÜŞTÜR; "BİLİNMİYOR"
        // işareti YALNIZ geri doldurma fazının tahmin edilemeyen satırları
        // içindir. Faz 1'de true yazan tek satır bile olmamalıdır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23 bilinmiyor");
        using var db = vt.Olustur(k);

        await Satis(db).SatisFisi(Fis(k, 8_000m, [B(k, "HAVALE", 8_000m)]), k.Islem);

        var kalem = Assert.Single(Kalemler(k));
        Assert.False(kalem.Bilinmiyor);
        Assert.True(kalem.Aktif);
        Assert.Null(kalem.StornoHareketId);
        Assert.Equal("TRY", kalem.ParaBirimi);       // UX-S32: birim tutarla taşınır
        Assert.Null(kalem.DovizTutar);
        Assert.Null(kalem.EvrakId);                  // bu fazda yazılmaz, uydurulmaz
        Assert.False(string.IsNullOrEmpty(kalem.KurSnapshotId));
    }

    [Fact]
    public async Task ESKI_alanlar_AYNEN_yazilir_tevsik_kaynagi_DEGISMEDI()
    {
        // GERİLEME KAPISI: kalem tablosu eklendi diye nakit_tutar / veresiye_tutar /
        // odeme_turu yazımı DEĞİŞMEMELİDİR. Değişseydi 459 s. VUK kümülatif nakit
        // toplamı (TevsikKapisi) ve MASAK kümülatifi sessizce sıfırlanır, had
        // delinirdi — geçmişi olmayan bir tabloya kaynak taşımanın bedeli budur.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23 eski alanlar");
        using var db = vt.Olustur(k);

        await Satis(db).SatisFisi(
            Fis(k, 5_000m, [B(k, "NAKIT", 6_000m), B(k, "KART", 4_000m), B(k, "VERESIYE", 5_000m)],
                kalemAdet: 3), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var satirlar = kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.FisId != null).ToList();

        Assert.All(satirlar, h => Assert.Equal("KARMA", h.OdemeTuru));
        Assert.Equal(6_000m, satirlar.Sum(h => h.NakitTutar));
        Assert.Equal(5_000m, satirlar.Sum(h => h.VeresiyeTutar));
    }

    [Fact]
    public async Task Kalemler_fisin_ILK_hareketine_baglanir()
    {
        // MASAK kaydı ve POS defteri de ilk harekete bağlanır; kalem başka bir
        // satıra bağlansaydı storno motoru kalemleri BULAMAZDI.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23 ilk hareket");
        using var db = vt.Olustur(k);

        await Satis(db).SatisFisi(
            Fis(k, 3_000m, [B(k, "NAKIT", 9_000m)], kalemAdet: 3), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var ilk = kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.FisId != null)
            .OrderBy(h => h.Olusturuldu).ThenBy(h => h.Id).First();
        var kalem = Assert.Single(Kalemler(k));

        Assert.Equal(ilk.FisId, kalem.FisId);
        Assert.Equal(ilk.Id, kalem.HareketId);
    }

    [Fact]
    public async Task Kalem_tablosu_KIRACI_suzgecinin_icindedir()
    {
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23 kiracı A");
        var b = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23 kiracı B");

        using (var dbA = vt.Olustur(a))
            await Satis(dbA).SatisFisi(Fis(a, 4_000m, [B(a, "NAKIT", 4_000m)]), a.Islem);

        using var dbB = vt.Olustur(b);
        Assert.Empty(dbB.OdemeKalemleri.AsNoTracking().ToList());   // A'nın kalemi B'ye SIZMAZ
    }

    [Fact]
    public async Task Storno_kalemi_PASIFE_ALIR_SILMEZ()
    {
        // Plan DoD: "Storno motoru ödeme kalemlerini de döndürür; unutulursa
        // para tek yönlü kaybolur."
        // ⚠ SATIS bugün StornolanabilirTurler İÇİNDE DEĞİLDİR (StornoServisi:72),
        //   yani satış fişi henüz stornolanamaz. Bu test motorun KALEM DAVRANIŞINI
        //   stornolanabilir bir tür (ISCILIK) üzerinden sınar: kalem o harekete
        //   bağlanır ve storno onu pasifleştirmelidir. SATIS listeye eklendiği gün
        //   davranış ZATEN yerindedir.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23 storno");

        string kaynakId;
        using (var db = vt.Olustur(k))
        {
            await Satis(db).Iscilik(new IscilikDto
            {
                CariId = k.CariId, IscilikTl = 1_200m, OdemeTuru = "NAKIT",
                KasaId = k.KasaId, KurSnapshotId = k.KurId, Aciklama = "İşçilik",
            }, k.Islem);
            kaynakId = await db.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == k.TenantId && h.Tur == "ISCILIK" && h.StornoKaynakId == null)
                .OrderByDescending(h => h.Olusturuldu).Select(h => h.Id).FirstAsync();

            // ⚠ P-23c: kalem ARTIK ELLE EKLENMİYOR. Faz 1/2'de işçilik yolu
            // kalem yazmadığı için bu test kalemi kendisi uyduruyordu; artık
            // HareketFabrikasi ödeme taşıyan her harekete kalemi KENDİ yazıyor.
            // Elle ekleme SÜRSEYDİ aynı harekette İKİ kalem olurdu — test bu
            // yüzden kırmızıya döndü ve kırılması DOĞRUDUR.
            var dogan = await db.OdemeKalemleri.AsNoTracking()
                .Where(x => x.TenantId == k.TenantId && x.HareketId == kaynakId).ToListAsync();
            Assert.Single(dogan);
            Assert.Equal(OdemeAraci.Nakit, dogan[0].Arac);
            Assert.Equal(1_200m, dogan[0].TutarTl);
        }

        using (var db2 = vt.Olustur(k))
            await Storno(db2).Storno(kaynakId, new StornoDto
            {
                Gerekce = "Kasiyer yanlış cariye işledi — mutabakatta düzeltildi",
                KurSnapshotId = k.KurId, KasaId = k.KasaId,
            }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.OdemeKalemleri.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.HareketId == kaynakId);

        Assert.False(kalem.Aktif);                                   // hükümsüz
        Assert.False(string.IsNullOrEmpty(kalem.StornoHareketId));   // kim hükümsüz kıldı
        var ters = kontrol.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.StornoKaynakId == kaynakId);
        Assert.Equal(ters.Id, kalem.StornoHareketId);
    }

    [Fact]
    public async Task FisOku_kalemleri_ve_KALEM_TOPLAMINI_dondurur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23 fiş okuma");
        using var db = vt.Olustur(k);

        await Satis(db).SatisFisi(
            Fis(k, 5_000m, [B(k, "NAKIT", 6_000m), B(k, "VERESIYE", 9_000m)], kalemAdet: 3), k.Islem);

        var fisId = Kalemler(k)[0].FisId;
        using var db2 = vt.Olustur(k);
        var json = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(await Satis(db2).FisOku(fisId, k.Islem),
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))).RootElement;

        var kalemler = json.GetProperty("odemeKalemleri");
        Assert.Equal(2, kalemler.GetArrayLength());
        Assert.Equal("NAKIT", kalemler[0].GetProperty("arac").GetString());
        Assert.Equal("VERESIYE", kalemler[1].GetProperty("arac").GetString());
        Assert.Equal(15_000m, json.GetProperty("odemeKalemToplam").GetDecimal());
        // Eski alanlar YERİNDE: okuyan taraf zorla göç ettirilmedi.
        Assert.True(json.TryGetProperty("bacaklar", out _));
        Assert.Equal(9_000m, json.GetProperty("veresiyeToplam").GetDecimal());
    }

    [Fact]
    public async Task Kalemi_OLMAYAN_eski_fiste_eski_yol_BIREBIR_calisir()
    {
        // Geçmiş fişin kalemi YOKTUR (geri doldurma koşmadı). Bu "ödeme yok"
        // demek DEĞİLDİR; okuyan taraf eski alanlardan okumaya devam eder.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23 eski fiş");
        using var db = vt.Olustur(k);

        await Satis(db).SatisFisi(Fis(k, 7_000m, [B(k, "NAKIT", 7_000m)]), k.Islem);
        var fisId = Kalemler(k)[0].FisId;

        // Geri doldurulmamış geçmişi TAKLİT et: kalemleri kaldır (test kurgusu).
        using (var temizle = vt.Olustur(k))
        {
            temizle.OdemeKalemleri.RemoveRange(
                temizle.OdemeKalemleri.Where(x => x.TenantId == k.TenantId));
            await temizle.SaveChangesAsync();
        }

        using var db2 = vt.Olustur(k);
        var json = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(await Satis(db2).FisOku(fisId, k.Islem),
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))).RootElement;

        Assert.Equal(0, json.GetProperty("odemeKalemleri").GetArrayLength());
        Assert.Equal(0m, json.GetProperty("odemeKalemToplam").GetDecimal());
        Assert.Equal(1, json.GetProperty("bacaklar").GetArrayLength());   // eski yol sağlam
    }
}
