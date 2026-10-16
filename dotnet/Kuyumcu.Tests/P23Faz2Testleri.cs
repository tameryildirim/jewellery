using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-23 — FAZ 2
//
// Hasım doğrulayıcı (2026-09-22) üç açık bırakmıştı; bu dosya ikisini kapatır,
// üçüncüsü (geri doldurma) migration ile kapanır ve buradaki kapsam bayrağı
// onun okunmasını da güvenceye alır.
//
//  1) [ORTA] "kalemi olmayan fiş = BİLİNMİYOR" kuralı YALNIZ kaynak yorumunda
//     duruyordu → artık yanıtta AÇIK alan: `kalemKapsami`.
//  2) [ORTA] FIS_TOPLAM_ODEME_KALEMI_FARKI sayacı hiçbir toplam ölçmüyordu →
//     artık SatisFisi içinde ÇALIŞMA ZAMANI kapısı var; bu dosya kapının
//     sökülmesini kırmızıya çevirir.
//  3) R-18 MAHSUP: plan "ya tümü ya hiç" diyor; KARAR = HİÇ. Test, kararın
//     sessizce delinmesini (MAHSUP'un araç listesine sızması) engeller.
// ═════════════════════════════════════════════════════════════════════════════

// ── Saf testler ──────────────────────────────────────────────────────────────
public sealed class P23Faz2SafTestleri
{
    [Fact]
    public void R18_Mahsup_Odeme_Araci_Olarak_Girmez()
    {
        // Plan § 2 P-23 / § 11 R-18: "ya TÜM ödeme yollarını kapsar ya HİÇ
        // girmez; kısmi uygulama mimari VETO". Seçilen dal: HİÇ.
        // Kısmi mahsup, satışın bacak toplamını fiş toplamının ÜSTÜNE çıkarır.
        Assert.DoesNotContain("MAHSUP", OdemeAraci.Tumu);
        Assert.DoesNotContain("AVANS", OdemeAraci.Tumu);
        Assert.False(OdemeAraci.Gecerli("MAHSUP"));
        Assert.False(OdemeAraci.Gecerli("AVANS_MAHSUP"));
    }

    [Fact]
    public void Kapsam_kalem_yoksa_BILINMIYOR_der()
    {
        // "Kalem yok" ≠ "ödeme yok" (Anayasa md.1). Bu ayrım artık koddadır.
        Assert.Equal(OdemeKalemKapsami.Bilinmiyor, OdemeKalemKapsami.Belirle(0, 0));
        Assert.Equal(OdemeKalemKapsami.Bilinmiyor, OdemeKalemKapsami.Belirle(-1, 0));
    }

    [Fact]
    public void Kapsam_kalem_varsa_KAYITLI_damgali_kalem_varsa_KISMEN_der()
    {
        Assert.Equal(OdemeKalemKapsami.Kayitli, OdemeKalemKapsami.Belirle(3, 0));
        Assert.Equal(OdemeKalemKapsami.KismenBilinmiyor, OdemeKalemKapsami.Belirle(3, 1));
        Assert.Equal(OdemeKalemKapsami.KismenBilinmiyor, OdemeKalemKapsami.Belirle(1, 1));
    }

    [Fact]
    public void Kapsam_listesi_KAPALIDIR_ucten_fazla_deger_yoktur()
    {
        // Sihirli dize üretimini engeller: okuyan taraf üç değeri bekler.
        Assert.Equal(3, OdemeKalemKapsami.Tumu.Length);
        foreach (var d in new[] { (0, 0), (2, 0), (2, 2) })
            Assert.Contains(OdemeKalemKapsami.Belirle(d.Item1, d.Item2), OdemeKalemKapsami.Tumu);
    }
}

// ── Veritabanı testleri ──────────────────────────────────────────────────────
[Collection("veritabani")]
public sealed class P23Faz2Testleri(TestVeritabani vt)
{
    private static SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<SatisServisi>.Instance);

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

    private static System.Text.Json.JsonElement Json(object o) =>
        System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(o,
                new System.Text.Json.JsonSerializerOptions(
                    System.Text.Json.JsonSerializerDefaults.Web))).RootElement;

    [Fact]
    public async Task Fis_okumasi_kalemKapsami_KAYITLI_doner()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23F2 kapsam kayıtlı");
        using var db = vt.Olustur(k);

        await Satis(db).SatisFisi(
            Fis(k, 5_000m, [B(k, "NAKIT", 6_000m), B(k, "VERESIYE", 4_000m)], kalemAdet: 2), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var fisId = kontrol.OdemeKalemleri.AsNoTracking()
            .First(x => x.TenantId == k.TenantId).FisId;

        using var db2 = vt.Olustur(k);
        var json = Json(await Satis(db2).FisOku(fisId, k.Islem));

        Assert.Equal("KAYITLI", json.GetProperty("kalemKapsami").GetString());
        // Canlı sepetten doğan kalem GERİ DOLDURULMUŞ değildir.
        Assert.False(json.GetProperty("odemeKalemleri")[0].GetProperty("geriDolduruldu").GetBoolean());
    }

    [Fact]
    public async Task Kalemi_olmayan_eski_fiste_kapsam_BILINMIYOR_doner()
    {
        // KUSURUN KENDİSİ: eskiden yanıt yalnız boş bir dizi döndürüyordu ve
        // okuyan taraf onu "bu fişte ödeme yok" diye okuyabiliyordu.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23F2 kapsam bilinmiyor");
        using var db = vt.Olustur(k);

        await Satis(db).SatisFisi(Fis(k, 7_000m, [B(k, "NAKIT", 7_000m)]), k.Islem);
        using var oku = vt.OlusturSistem();
        var fisId = oku.OdemeKalemleri.AsNoTracking().First(x => x.TenantId == k.TenantId).FisId;

        using (var temizle = vt.Olustur(k))   // geri doldurulmamış geçmişi taklit et
        {
            temizle.OdemeKalemleri.RemoveRange(
                temizle.OdemeKalemleri.Where(x => x.TenantId == k.TenantId));
            await temizle.SaveChangesAsync();
        }

        using var db2 = vt.Olustur(k);
        var json = Json(await Satis(db2).FisOku(fisId, k.Islem));

        Assert.Equal(0, json.GetProperty("odemeKalemleri").GetArrayLength());
        Assert.Equal("BILINMIYOR", json.GetProperty("kalemKapsami").GetString());
        Assert.Equal(1, json.GetProperty("bacaklar").GetArrayLength());   // eski yol sağlam
    }

    [Fact]
    public async Task Geri_doldurulmus_BILINMIYOR_damgali_kalem_kapsami_KISMEN_yapar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23F2 kısmen bilinmiyor");
        using var db = vt.Olustur(k);

        await Satis(db).SatisFisi(Fis(k, 9_000m, [B(k, "NAKIT", 9_000m)]), k.Islem);
        using var oku = vt.OlusturSistem();
        var fisId = oku.OdemeKalemleri.AsNoTracking().First(x => x.TenantId == k.TenantId).FisId;

        // Geri doldurma fazının ürettiği türden bir satırı taklit et.
        using (var ekle = vt.Olustur(k))
        {
            ekle.OdemeKalemleri.Add(new OdemeKalemi
            {
                Id = Guid.NewGuid().ToString("N")[..26],
                TenantId = k.TenantId, FisId = fisId,
                HareketId = ekle.OdemeKalemleri.AsNoTracking()
                    .First(x => x.TenantId == k.TenantId && x.FisId == fisId).HareketId,
                Sira = 9, Arac = OdemeAraci.Nakit, TutarTl = 0m, ParaBirimi = "TRY",
                Bilinmiyor = true, GeriDolduruldu = true, Aktif = true,
                Olusturuldu = DateTime.UtcNow,
            });
            await ekle.SaveChangesAsync();
        }

        using var db2 = vt.Olustur(k);
        var json = Json(await Satis(db2).FisOku(fisId, k.Islem));

        Assert.Equal("KISMEN_BILINMIYOR", json.GetProperty("kalemKapsami").GetString());
    }

    [Fact]
    public async Task Toplam_denkligi_kapisi_fisi_YAZAR_ve_kalem_toplami_fis_toplamina_ESITTIR()
    {
        // FIS_TOPLAM_ODEME_KALEMI_FARKI'nın ÇALIŞMA ZAMANI karşılığı.
        // Kapı `!=` yerine hep-doğru bir koşula çevrilirse (mutasyon) bu test
        // ConflictException ile KIRMIZI olur.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23F2 toplam kapısı");
        using var db = vt.Olustur(k);

        await Satis(db).SatisFisi(
            Fis(k, 4_000m, [B(k, "NAKIT", 5_000m), B(k, "KART", 3_000m), B(k, "VERESIYE", 4_000m)],
                kalemAdet: 3), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kalemToplam = kontrol.OdemeKalemleri.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId && x.Aktif).Sum(x => x.TutarTl);
        var fisToplam = kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.FisId != null).Sum(h => h.TlTutari);

        Assert.Equal(12_000m, fisToplam);
        Assert.Equal(fisToplam, kalemToplam);
    }
}
