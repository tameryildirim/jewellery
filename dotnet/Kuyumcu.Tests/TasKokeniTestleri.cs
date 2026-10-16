using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Urun;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// SENTETİK TAŞ ETİKETLEME TESTLERİ — PLAN-2 Adım 3
//
// Dayanak: Kuyum Ticareti Yönetmeliği md.11/(e) — RG 24.06.2026/33290.
// "…'sentetik', 'laboratuvar üretimi', 'yapay üretim' … ibarelerinden en az
//  birine … açıkça yer vermeksizin satışa sunamaz. Bu ürünler ile doğal
//  kıymetli taşları … ayrı bölümlerde, internet ortamında ayrı kategorilerde
//  satışa sunar."
//
// Aşkın'ın şartı: fail-closed davranış MUTASYONLA kanıtlanır — TasKapisi'ndeki
// "taşlı + köken yok → dur" satırı kaldırıldığında aşağıdaki testler KIRMIZI
// olmak zorunda. (Bkz. dokumanlar/04-test/test-politikasi.md §mutasyon)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TasKokeniKuralTestleri
{
    // ── İbare zorunluluğu ───────────────────────────────────────────────────

    [Theory]
    [InlineData(TasKokeni.Sentetik,           true)]
    [InlineData(TasKokeni.LaboratuvarUretimi, true)]
    [InlineData(TasKokeni.YapayUretim,        true)]
    [InlineData(TasKokeni.Dogal,              false)]
    [InlineData(TasKokeni.TasYok,             false)]
    [InlineData(null,                         false)]
    public void Ibare_yalniz_insan_yapimi_taslarda_zorunludur(string? koken, bool zorunlu)
        => Assert.Equal(zorunlu, TasKokeni.IbareGerekir(koken));

    [Theory]
    [InlineData(TasKokeni.Sentetik,           "sentetik")]
    [InlineData(TasKokeni.LaboratuvarUretimi, "laboratuvar üretimi")]
    [InlineData(TasKokeni.YapayUretim,        "yapay üretim")]
    public void Varsayilan_ibareler_yonetmeligin_kelimeleridir(string koken, string beklenen)
        => Assert.Equal(beklenen, TasKokeni.VarsayilanIbare(koken));

    [Fact]
    public void Isletme_yonetmeligin_diger_ibaresini_secebilir()
    {
        // md.11/(e) "en az birine" der — üçü de meşrudur, seçim işletmenindir.
        Assert.Equal("laboratuvar üretimi",
            TasKokeni.Ibare(TasKokeni.Sentetik, "laboratuvar üretimi"));
    }

    [Fact]
    public void Dogal_tasta_ibare_bosturulur_ozel_metin_verilse_bile()
    {
        // Doğal taşa "sentetik" yazdırmak yönetmeliğe aykırı olurdu.
        Assert.Equal("", TasKokeni.Ibare(TasKokeni.Dogal, "sentetik"));
    }

    // ── Kategori ayrımı ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(TasKokeni.Dogal,              "DOGAL_TAS")]
    [InlineData(TasKokeni.Sentetik,           "YAPAY_TAS")]
    [InlineData(TasKokeni.LaboratuvarUretimi, "YAPAY_TAS")]
    [InlineData(TasKokeni.YapayUretim,        "YAPAY_TAS")]
    [InlineData(TasKokeni.TasYok,             "TASSIZ")]
    [InlineData(null,                         "TASSIZ")]
    public void Dogal_ve_yapay_ayri_kategoridedir(string? koken, string kategori)
        => Assert.Equal(kategori, TasKokeni.Kategori(koken));

    [Fact]
    public void Dogal_ve_yapay_ayni_kategoride_ASLA_bulunmaz()
    {
        // Yönetmeliğin asıl amacı bu: tüketici ikisini karıştırmasın.
        Assert.NotEqual(TasKokeni.Kategori(TasKokeni.Dogal),
                        TasKokeni.Kategori(TasKokeni.Sentetik));
    }

    // ── Kapı: doğrulama ─────────────────────────────────────────────────────

    [Fact]
    public void Tasli_urun_kokensiz_KABUL_EDILMEZ()
    {
        var hata = Assert.Throws<BadRequestException>(
            () => TasKapisi.Dogrula(0.35m, null, null));
        Assert.Contains("md.11/(e)", hata.Message);
    }

    [Fact]
    public void Tasli_urune_TAS_YOK_yazilarak_kural_atlatilamaz()
    {
        Assert.Throws<BadRequestException>(
            () => TasKapisi.Dogrula(0.35m, TasKokeni.TasYok, null));
    }

    [Fact]
    public void Belirsizlik_DOGAL_sayilmaz()
    {
        // Bilgi eksikliği, taşın doğal olduğu anlamına GELMEZ. Kapı durur.
        Assert.Throws<BadRequestException>(() => TasKapisi.Dogrula(0.01m, null, null));
    }

    [Fact]
    public void Tas_agirligi_sifir_olan_urun_kokensiz_gecer()
        => TasKapisi.Dogrula(0m, null, null);    // istisna fırlatmamalı

    [Fact]
    public void Tas_agirligi_bilinmeyen_urun_kokensiz_gecer()
        => TasKapisi.Dogrula(null, null, null);  // istisna fırlatmamalı

    [Fact]
    public void Tassiz_urune_koken_bildirilemez()
    {
        // Taş ağırlığı 0 ama köken "sentetik" → tutarsız kayıt; sessizce düzeltmeyiz.
        Assert.Throws<BadRequestException>(
            () => TasKapisi.Dogrula(0m, TasKokeni.Sentetik, null));
    }

    [Fact]
    public void Gecersiz_koken_reddedilir()
    {
        var hata = Assert.Throws<BadRequestException>(
            () => TasKapisi.Dogrula(0.5m, "MOISSANITE", null));
        Assert.Contains("Geçersiz taş kökeni", hata.Message);
    }

    [Fact]
    public void Ozel_ibare_yonetmelik_disina_cikamaz()
    {
        // "özel üretim" yönetmeliğin saydığı ibarelerden değildir.
        var hata = Assert.Throws<BadRequestException>(
            () => TasKapisi.Dogrula(0.5m, TasKokeni.Sentetik, "özel üretim"));
        Assert.Contains("md.11/(e)", hata.Message);
    }

    [Theory]
    [InlineData("Sentetik")]
    [InlineData("SENTETİK safir")]
    [InlineData("laboratuvar üretimi zümrüt")]
    public void Kabul_edilen_ibareler_buyuk_kucuk_harften_bagimsizdir(string ibare)
        => TasKapisi.Dogrula(0.5m, TasKokeni.Sentetik, ibare);

    [Fact]
    public void Dogal_tas_ibaresiz_gecer()
        => TasKapisi.Dogrula(2.5m, TasKokeni.Dogal, null);
}

// ═════════════════════════════════════════════════════════════════════════════
// ÜRÜN + SATIŞ AKIŞI — veritabanına karşı
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class TasKokeniAkisTestleri(TestVeritabani vt)
{
    private static UrunServisi Urun(KuyumcuDbContext db) => new(db);

    /// <summary>Türkçe harfleri \uXXXX'e kaçırmayan serileştirme (assert okunaklı olsun).</summary>
    private static readonly System.Text.Json.JsonSerializerOptions GevsekJson = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static UrunOlusturDto YuzukDto(string? koken, string? ibare = null, decimal tas = 0.4m) => new()
    {
        Tip = "ZIYNET", Kod = $"Y-{Kimlik.Ulid()[^10..]}", Ad = "Taşlı yüzük",
        AyarId = "K14", GramAgirlik = 4m, TasAgirlik = tas,
        TasKokeni = koken, TasIbaresi = ibare,
    };

    // ── Ürün kartı ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Tasli_urun_kokensiz_OLUSTURULAMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Taş kökensiz");
        using var db = vt.Olustur(k.TenantId);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Urun(db).Olustur(YuzukDto(null), k.Islem));

        Assert.Contains("md.11/(e)", hata.Message);
        Assert.False(await db.Urunler.AnyAsync(u => u.Ad == "Taşlı yüzük"));
    }

    [Fact]
    public async Task Sentetik_urun_koken_ve_ibaresiyle_kaydedilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Sentetik kayıt");
        using var db = vt.Olustur(k.TenantId);

        var dto = YuzukDto(TasKokeni.Sentetik);
        await Urun(db).Olustur(dto, k.Islem);

        var kayit = await db.Urunler.AsNoTracking().SingleAsync(u => u.Kod == dto.Kod);
        Assert.Equal(TasKokeni.Sentetik, kayit.TasKokeni);
        Assert.Equal("YAPAY_TAS", TasKokeni.Kategori(kayit.TasKokeni));
    }

    [Fact]
    public async Task Tassiz_urunde_koken_TAS_YOK_olarak_yazilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Taşsız kayıt");
        using var db = vt.Olustur(k.TenantId);

        var dto = YuzukDto(null, tas: 0m);
        await Urun(db).Olustur(dto, k.Islem);

        var kayit = await db.Urunler.AsNoTracking().SingleAsync(u => u.Kod == dto.Kod);
        Assert.Equal(TasKokeni.TasYok, kayit.TasKokeni);
    }

    [Fact]
    public async Task Guncellemede_tas_eklenip_koken_verilmezse_DURUR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Taş sonradan");
        using var db = vt.Olustur(k.TenantId);

        // Kiracının varsayılan ürünü taşsız (TasAgirlik = 0).
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Urun(db).Guncelle(k.UrunId,
                new UrunGuncelleDto { TasAgirlik = 0.8m }, k.Islem));

        Assert.Contains("md.11/(e)", hata.Message);
    }

    [Fact]
    public async Task Guncellemede_yalniz_koken_degistirilse_bile_kapi_calisir()
    {
        // Regresyon koruması: kapı "has hesabını etkileyen alan" bloğunun
        // İÇİNDE olsaydı bu çağrı sessizce geçerdi.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Yalnız köken");
        using var db = vt.Olustur(k.TenantId);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Urun(db).Guncelle(k.UrunId,
                new UrunGuncelleDto { TasKokeni = TasKokeni.Sentetik }, k.Islem));

        // Taşsız ürüne köken bildirilemez.
        Assert.Contains("Taş ağırlığı girilmemiş", hata.Message);
    }

    // ── Etiket ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Etiket_metni_ibareyi_acikca_tasir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Etiket");
        using var db = vt.Olustur(k.TenantId);

        var dto = YuzukDto(TasKokeni.LaboratuvarUretimi);
        await Urun(db).Olustur(dto, k.Islem);
        var id = (await db.Urunler.AsNoTracking().SingleAsync(u => u.Kod == dto.Kod)).Id;

        var etiket = await Urun(db).Etiket(id, k.Islem);
        var metin = System.Text.Json.JsonSerializer.Serialize(etiket, GevsekJson);

        Assert.Contains("laboratuvar üretimi", metin);
        Assert.Contains("YAPAY_TAS", metin);
        Assert.Contains("md.11/(e)", metin);
    }

    [Fact]
    public async Task Tassiz_urunun_etiketinde_ibare_bulunmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Etiket taşsız");
        using var db = vt.Olustur(k.TenantId);

        var etiket = await Urun(db).Etiket(k.UrunId, k.Islem);
        var metin = System.Text.Json.JsonSerializer.Serialize(etiket, GevsekJson);

        Assert.Contains("\"ibare\":\"\"", metin);
        Assert.Contains("\"ibareZorunlu\":false", metin);
    }

    // ── Kategori ayrımı (internet / vitrin) ─────────────────────────────────

    [Fact]
    public async Task Liste_dogal_ve_yapay_taslari_AYRI_kategoride_dondurur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Kategori ayrımı");
        using var db = vt.Olustur(k.TenantId);
        var s = Urun(db);

        var dogal    = YuzukDto(TasKokeni.Dogal);
        var sentetik = YuzukDto(TasKokeni.Sentetik);
        await s.Olustur(dogal, k.Islem);
        await s.Olustur(sentetik, k.Islem);

        var dogalListe = await s.Listele(k.Islem, null, null, null, null, 50, 0, "DOGAL_TAS");
        var yapayListe = await s.Listele(k.Islem, null, null, null, null, 50, 0, "YAPAY_TAS");

        Assert.Contains(dogalListe, u => u.Kod == dogal.Kod);
        Assert.DoesNotContain(dogalListe, u => u.Kod == sentetik.Kod);

        Assert.Contains(yapayListe, u => u.Kod == sentetik.Kod);
        Assert.DoesNotContain(yapayListe, u => u.Kod == dogal.Kod);
    }

    [Fact]
    public async Task Gecersiz_kategori_filtresi_reddedilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Kategori geçersiz");
        using var db = vt.Olustur(k.TenantId);

        await Assert.ThrowsAsync<BadRequestException>(
            () => Urun(db).Listele(k.Islem, null, null, null, null, 50, 0, "ELMAS"));
    }

    // ── Satış kapısı (göç öncesi kayıtlar) ──────────────────────────────────

    [Fact]
    public async Task Kokensiz_tasli_urun_SATILAMAZ()
    {
        // Migration öncesi kayıtlarda köken NULL'dır; ürün kartı kapısı bunları
        // yakalamaz. Son savunma satış anındadır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Göç öncesi kayıt");
        using var db = vt.Olustur(k.TenantId);

        var urun = await db.Urunler.SingleAsync(u => u.Id == k.UrunId);
        urun.TasAgirlik = 0.6m;      // taşlı ama…
        urun.TasKokeni  = null;      // …köken bilinmiyor (eski kayıt)
        await db.SaveChangesAsync();

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).Satis(new MSatis.SatisOlusturDto
            {
                UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 5_000m,
                OdemeTuru = "NAKIT", KurSnapshotId = k.KurId, CariId = k.CariId,
            }, k.Islem));

        Assert.Contains("taş kökeni belirtilmemiş", hata.Message);

        // Fail-closed kanıtı: stok ve kasa hiç dokunulmamış olmalı.
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(db, k.KasaId));
        Assert.False(await db.Hareketler.AnyAsync(h => h.UrunId == k.UrunId));
    }

    [Fact]
    public async Task Kokeni_bildirilmis_tasli_urun_SATILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Köken bildirilmiş");
        using var db = vt.Olustur(k.TenantId);

        var urun = await db.Urunler.SingleAsync(u => u.Id == k.UrunId);
        urun.TasAgirlik = 0.6m;
        urun.TasKokeni  = TasKokeni.Sentetik;
        urun.TasIbaresi = "sentetik";
        await db.SaveChangesAsync();

        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 5_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId,
            KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

        Assert.True(await db.Hareketler.AnyAsync(h => h.UrunId == k.UrunId && h.Tur == "SATIS"));
    }

    [Fact]
    public async Task Kokensiz_tasli_urun_TAKASTA_da_verilemez()
    {
        // Takasta müşteriye verilen ürün de "satışa sunulmuş" sayılır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Takas kökensiz");
        using var db = vt.Olustur(k.TenantId);

        var urun = await db.Urunler.SingleAsync(u => u.Id == k.UrunId);
        urun.TasAgirlik = 0.6m;
        urun.TasKokeni  = null;
        await db.SaveChangesAsync();

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).Takas(new MSatis.TakasDto
            {
                Satici = ParaDemoTestVerisi.TakasSaticisi,   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
                YeniUrunId = k.UrunId, YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = 5_000m,
                EskiUrunGram = 5m, EskiUrunAyarId = "K22",
                EskiUrunBirimFiyatTlHasGram = 6_800m,
                KurSnapshotId = k.KurId, CariId = k.CariId,
            }, k.Islem));

        Assert.Contains("taş kökeni belirtilmemiş", hata.Message);
    }

    [Fact]
    public async Task Tassiz_urun_satisi_kuraldan_ETKILENMEZ()
    {
        // Regresyon: kapı taşsız ürünlerin satışını engellememeli.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Taşsız satış");
        using var db = vt.Olustur(k.TenantId);

        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 5_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId,
            KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

        Assert.True(await db.Hareketler.AnyAsync(h => h.UrunId == k.UrunId));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// GÖÇ SONRASI EKSİK VERİ — KOKEN_EKSIK kovası
//
// Migration (SentetikTasEtiketleme) sütunları NULLABLE ekler; mevcut taşlı
// ürünlerin kökeni bilinmez. Bu ürünler satışta DURUR. İşletmenin onları
// bulabilmesi için ayrı bir kategori kovası gerekir — aksi hâlde kasada
// beklenmedik bir hata olarak karşılarına çıkardı.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class TasKokeniGocTestleri(TestVeritabani vt)
{
    [Fact]
    public async Task Kokeni_eksik_tasli_urun_TASSIZ_sayilmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Göç kovası");
        using var db = vt.Olustur(k.TenantId);

        var urun = await db.Urunler.SingleAsync(u => u.Id == k.UrunId);
        urun.TasAgirlik = 0.5m;
        urun.TasKokeni  = null;          // göç öncesi kayıt
        await db.SaveChangesAsync();

        var s = new UrunServisi(db);

        // Taşsız vitrinine SIZMAMALI — tüketici onu taşsız sanmamalı.
        var tassiz = await s.Listele(k.Islem, null, null, null, null, 50, 0, "TASSIZ");
        Assert.DoesNotContain(tassiz, u => u.Id == k.UrunId);

        // Doğal ve yapay vitrinlerinde de yeri yoktur.
        Assert.DoesNotContain(
            await s.Listele(k.Islem, null, null, null, null, 50, 0, "DOGAL_TAS"),
            u => u.Id == k.UrunId);
        Assert.DoesNotContain(
            await s.Listele(k.Islem, null, null, null, null, 50, 0, "YAPAY_TAS"),
            u => u.Id == k.UrunId);

        // Yalnız eksik-veri kovasında görünür.
        Assert.Contains(
            await s.Listele(k.Islem, null, null, null, null, 50, 0, "KOKEN_EKSIK"),
            u => u.Id == k.UrunId);
    }

    [Fact]
    public async Task Kokeni_girilmis_urun_eksik_kovasindan_CIKAR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Göç düzeltme");
        using var db = vt.Olustur(k.TenantId);

        var urun = await db.Urunler.SingleAsync(u => u.Id == k.UrunId);
        urun.TasAgirlik = 0.5m;
        urun.TasKokeni  = null;
        await db.SaveChangesAsync();

        var s = new UrunServisi(db);
        await s.Guncelle(k.UrunId, new UrunGuncelleDto { TasKokeni = TasKokeni.Dogal }, k.Islem);

        Assert.DoesNotContain(
            await s.Listele(k.Islem, null, null, null, null, 50, 0, "KOKEN_EKSIK"),
            u => u.Id == k.UrunId);
        Assert.Contains(
            await s.Listele(k.Islem, null, null, null, null, 50, 0, "DOGAL_TAS"),
            u => u.Id == k.UrunId);
    }
}
