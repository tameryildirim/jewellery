using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using MFatura = Kuyumcu.Api.Modules.Fatura;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// KDV ÖZEL MATRAH OTOMATİĞİ — paket 38 Faz A [DB]
//
// Çekirdek sözleşmeler:
//   • KUYUM + hareketId → has bedeli DEFTERDEN türetilir (has gram × o hareketin
//     KENDİ kur kaydındaki altin_has_fiyati); kaynak = HAREKET.
//   • Matrahın kümesi = DAMGANIN kümesi (fişin tamamı).
//   • İstemci farklı hasDeger gönderirse 400 ve HİÇBİR satır yazılmaz.
//   • Kur kaydı yok/0 → 400 (FAIL-CLOSED); "has bedeli 0" varsayılmaz.
//   • hareketId yoksa ELLE değer kabul edilir (bugünkü davranış korunur).
//   • ozel_matrah_kodu YALNIZ KUYUM'da ve YALNIZ yapılandırmadan; varsayılan NULL.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class FaturaOzelMatrahTestleri(TestVeritabani vt)
{
    private const decimal KurHasFiyati = 6_827.20m;      // TestKurulum.KiraciKur

    /// <summary>Satış satırı: has gramı NEGATİF (defter sözleşmesi), kur kaydı bağlı.</summary>
    private static Hareket Satir(
        Kiraci k, string? fisId, decimal tl, decimal hasGram,
        string? kurId = null, string? faturaNo = null, string? tenantId = null)
    {
        var now = DateTime.UtcNow;
        return new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = tenantId ?? k.TenantId, SubeId = k.SubeId,
            Tur = "SATIS", Tarih = now, TlTutari = tl, HasGrami = hasGram,
            OdemeTuru = "HAVALE", NakitTutar = 0m, VeresiyeTutar = 0m,
            FisId = fisId, KurSnapshotId = kurId ?? k.KurId, CariId = k.CariId,
            UrunId = k.UrunId, KullaniciId = k.KullaniciId, FaturaNo = faturaNo,
            Olusturuldu = now,
        };
    }

    private static JsonElement Oku(object cevap) =>
        JsonDocument.Parse(JsonSerializer.Serialize(cevap)).RootElement;

    private static decimal Y2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);

    // ── 1. Türetim: has_deger / has_gram / has_birim_fiyat / kaynak ──────────

    [Fact]
    public async Task HareketId_verilen_KUYUM_faturada_has_deger_DEFTERDEN_turetilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 türetim");
        var h = Satir(k, null, 75_000m, -10m);
        kurulum.Hareketler.Add(h);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 75_000m, KdvOran = 0.20m,
            KdvTuru = "KUYUM", HareketId = h.Id,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var f = kontrol.Faturalar.Single(x => x.TenantId == k.TenantId);

        var beklenenHas = Y2(10m * KurHasFiyati);            // 68.272,00
        Assert.Equal(beklenenHas, f.HasDeger);
        Assert.Equal(10.000000m, f.HasGram);
        Assert.Equal(KurHasFiyati, f.HasBirimFiyat);
        Assert.Equal("HAREKET", f.HasDegerKaynagi);
        // KDV yalnız (75.000 − 68.272) = 6.728 üzerinden: 6728 − 6728/1,2 = 1.121,33
        Assert.Equal(1_121.33m, f.KdvTutar);
        // Hareket damgalandı (fatura ↔ defter bağı).
        // P-22 / B-3: taslakta damga TASLAK İŞARETİDİR (numara kesimde verilir).
        Assert.Equal(Kuyumcu.Api.Modules.Fatura.FaturaSeriKapisi.Isaret(f.Id),
                     kontrol.Hareketler.Single(x => x.Id == h.Id).FaturaNo);
    }

    // ── 2. MATRAHIN KÜMESİ = DAMGANIN KÜMESİ (fişin tamamı) ──────────────────

    [Fact]
    public async Task Cok_kalemli_fiste_matrah_FISIN_TAMAMINDAN_cikar_ve_damga_kumesiyle_AYNIDIR()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 fiş");
        var fisId = Kimlik.Ulid();
        var h1 = Satir(k, fisId, 40_000m, -6m);
        var h2 = Satir(k, fisId, 35_000m, -4m);
        kurulum.Hareketler.AddRange(h1, h2);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        // Tek kalemin hareketId'si verildi; fişin TAMAMI faturalanmalı.
        await new MFatura.FaturaServisi(db).Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 75_000m, KdvOran = 0.20m,
            KdvTuru = "KUYUM", HareketId = h1.Id,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var f = kontrol.Faturalar.Single(x => x.TenantId == k.TenantId);
        var hareketler = kontrol.Hareketler.Where(x => x.TenantId == k.TenantId).ToList();

        // 6 g + 4 g = 10 g → tek kalemden (6 g) hesaplansaydı has bedeli 40.963,20 olurdu.
        Assert.Equal(10.000000m, f.HasGram);
        Assert.Equal(Y2(10m * KurHasFiyati), f.HasDeger);
        Assert.Equal(2, hareketler.Count);
        Assert.All(hareketler, x => Assert.Equal(
            Kuyumcu.Api.Modules.Fatura.FaturaSeriKapisi.Isaret(f.Id), x.FaturaNo));
    }

    // ── 3. TUTARLILIK KAPISI: tolerans YOK ───────────────────────────────────

    [Fact]
    public async Task Istemci_FARKLI_hasDeger_gonderirse_400_ve_HICBIR_satir_yazilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 kapı");
        var h = Satir(k, null, 75_000m, -10m);
        kurulum.Hareketler.Add(h);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            new MFatura.FaturaServisi(db).Olustur(new MFatura.FaturaOlusturDto
            {
                CariId = k.CariId, Toplam = 75_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
                HareketId = h.Id, HasDeger = Y2(10m * KurHasFiyati) + 1m,
            }, k.Islem));

        Assert.Contains("tolerans yoktur", hata.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, kontrol.Faturalar.Count(x => x.TenantId == k.TenantId));
        // Damga da vurulmadı: hareket hâlâ faturasız.
        Assert.Null(kontrol.Hareketler.Single(x => x.Id == h.Id).FaturaNo);
    }

    [Fact]
    public async Task Istemci_AYNI_hasDeger_gonderirse_kabul_edilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 eşit");
        var h = Satir(k, null, 75_000m, -10m);
        kurulum.Hareketler.Add(h);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 75_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            HareketId = h.Id, HasDeger = Y2(10m * KurHasFiyati),
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal("HAREKET", kontrol.Faturalar.Single(x => x.TenantId == k.TenantId).HasDegerKaynagi);
    }

    // ── 4. FAIL-CLOSED: kur kaydı yok / has fiyatı 0 ─────────────────────────

    [Fact]
    public async Task Kur_kaydi_has_fiyati_SIFIR_olan_hareket_400_verir_ve_fatura_yazilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 kursuz");
        var bozukKur = Kimlik.Ulid();
        kurulum.KurSnapshots.Add(new KurSnapshot
        {
            Id = bozukKur, TenantId = k.TenantId, AltinHasFiyati = 0m,
            GramAltinFiyati = 0m, Kaynak = "MANUAL", Olusturuldu = DateTime.UtcNow,
        });
        var h = Satir(k, null, 75_000m, -10m, kurId: bozukKur);
        kurulum.Hareketler.Add(h);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            new MFatura.FaturaServisi(db).Olustur(new MFatura.FaturaOlusturDto
            {
                CariId = k.CariId, Toplam = 75_000m, KdvOran = 0.20m,
                KdvTuru = "KUYUM", HareketId = h.Id,
            }, k.Islem));

        Assert.Contains("kur kaydı bulunamadı", hata.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, kontrol.Faturalar.Count(x => x.TenantId == k.TenantId));
    }

    // ── 5. ELLE yol korunur (hareketId YOK) ──────────────────────────────────

    [Fact]
    public async Task HareketId_yoksa_ELLE_has_bedeli_REDDEDILIR_satir_yazilmaz()
    {
        // 17.10 P0-5 (mevzuat kilidi): eskiden ELLE değer kabul edilip kaynak "ELLE" kalıyordu.
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 elle");
        using var db = vt.Olustur(k);

        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() => new MFatura.FaturaServisi(db).Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 75_000m, KdvOran = 0.20m,
            KdvTuru = "KUYUM", HasDeger = 60_000m,
        }, k.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.False(kontrol.Faturalar.Any(x => x.TenantId == k.TenantId));
    }

    // ── 6. Özel matrah kodu: yalnız KUYUM, yalnız yapılandırmadan ────────────

    [Fact]
    // ⚠ PAKET P-26 (Y-40) — hasım doğrulayıcı düzeltmesi: varsayılan KAPALIDIR
    //   (PLAN:578/778, K-10 kurucu/mali müşavir kalemi). Test iki şeyi sınar:
    //   (a) ayar verilmeyince KUYUM faturasına kod YAZILMAZ (kolon NULL),
    //   (b) teyit gelip yapılandırmaya 805 yazılınca kod faturaya GEÇER.
    public async Task Ozel_matrah_kodu_VARSAYILANDA_YAZILMAZ_teyit_gelince_yazilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 kodsuz");
        var h = Satir(k, null, 75_000m, -10m);
        kurulum.Hareketler.Add(h);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        // Ayar verilmedi → VARSAYILAN KAPALI: kolon NULL kalır, faturaya kod basılmaz.
        await new MFatura.FaturaServisi(db).Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 75_000m, KdvTuru = "KUYUM", HareketId = h.Id,
        }, k.Islem);

        using (var kontrol = vt.OlusturSistem())
            Assert.Null(kontrol.Faturalar.Single(x => x.TenantId == k.TenantId).OzelMatrahKodu);

        // AÇMA YOLU AÇIK: teyit gelince yapılandırmaya tek satır 805 yazılır.
        var h2 = Satir(k, null, 60_000m, -8m);
        using (var kurulum2 = vt.OlusturSistem())
        {
            kurulum2.Hareketler.Add(h2);
            kurulum2.SaveChanges();
        }
        using var db2 = vt.Olustur(k);
        await new MFatura.FaturaServisi(
                db2, new MFatura.KdvAyarlari { OzelMatrahKodu = MFatura.KdvAyarlari.OneriKod })
            .Olustur(new MFatura.FaturaOlusturDto
            {
                CariId = k.CariId, Toplam = 60_000m, KdvTuru = "KUYUM", HareketId = h2.Id,
            }, k.Islem);

        using var kontrol2 = vt.OlusturSistem();
        Assert.Equal("805", kontrol2.Faturalar
            .Single(x => x.TenantId == k.TenantId && x.Toplam == 60_000m).OzelMatrahKodu);
    }

    [Fact]
    public async Task Yapilandirmada_kod_ACIKSA_YALNIZ_KUYUM_faturaya_yazilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 kodlu");
        var h = Satir(k, null, 75_000m, -10m);
        kurulum.Hareketler.Add(h);
        kurulum.SaveChanges();

        // appsettings DEĞİL, ctor nesnesi (HttpOrtami ortam değişkenleri süreç
        // geneli olduğu için test içinde değiştirilemez — Aşkın'ın koşulu).
        var ayar = new MFatura.KdvAyarlari { OzelMatrahKodu = "805" };

        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db, ayar);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 75_000m, KdvTuru = "KUYUM", HareketId = h.Id,
        }, k.Islem);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 1_200m, KdvTuru = "STANDART",
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var faturalar = kontrol.Faturalar.Where(x => x.TenantId == k.TenantId).ToList();
        Assert.Equal("805", faturalar.Single(x => x.KdvTuru == "KUYUM").OzelMatrahKodu);
        // STANDART faturaya yazılsaydı yanlış beyan kaynağı olurdu.
        Assert.Null(faturalar.Single(x => x.KdvTuru == "STANDART").OzelMatrahKodu);
    }

    // ── 7. KDV oranı yapılandırmadan; davranış değişmez ──────────────────────

    [Fact]
    public async Task Kdv_orani_yapilandirmadan_okunur_varsayilan_020_davranisi_AYNI()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 oran");
        using var db = vt.Olustur(k);

        // Oran gönderilmedi → ayardan (0.10) okunmalı; koda gömülü 0.20 kalsaydı
        // KDV 200,00 çıkardı.
        await new MFatura.FaturaServisi(db, new MFatura.KdvAyarlari { VarsayilanOran = 0.10m })
            .Olustur(new MFatura.FaturaOlusturDto { CariId = k.CariId, Toplam = 1_100m }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var f = kontrol.Faturalar.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(0.1000m, f.KdvOran);
        Assert.Equal(100.00m, f.KdvTutar);
    }

    // ── 8. Kiracı sınırı: yabancı hareketId 404 ──────────────────────────────

    [Fact]
    public async Task Yabanci_kiracinin_hareketId_si_404_verir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 kiracı A");
        var yabanci = TestKurulum.KiraciKur(kurulum, "PAKET38 kiracı B");
        var h = Satir(yabanci, null, 75_000m, -10m);
        kurulum.Hareketler.Add(h);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new MFatura.FaturaServisi(db).Olustur(new MFatura.FaturaOlusturDto
            {
                CariId = k.CariId, Toplam = 75_000m, KdvTuru = "KUYUM", HareketId = h.Id,
            }, k.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, kontrol.Faturalar.Count(x => x.TenantId == k.TenantId));
    }

    // ── 9. Önizleme ucu ──────────────────────────────────────────────────────

    [Fact]
    public async Task Onizleme_fisin_TAMAMINI_kapsar_ve_turetilen_kirilimi_dondurur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 önizleme");
        var fisId = Kimlik.Ulid();
        var h1 = Satir(k, fisId, 40_000m, -6m);
        var h2 = Satir(k, fisId, 35_000m, -4m);
        kurulum.Hareketler.AddRange(h1, h2);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var y = Oku(await new MFatura.FaturaServisi(db)
            .OzelMatrahOnizleme(h1.Id, 0.20m, 75_000m, k.Islem));

        Assert.Equal(2, y.GetProperty("kapsananHareketSayisi").GetInt32());
        Assert.Equal(2, y.GetProperty("kapsananHareketIdler").GetArrayLength());
        Assert.Equal(10.000000m, y.GetProperty("hasGram").GetDecimal());
        Assert.Equal(Y2(10m * KurHasFiyati), y.GetProperty("hasDeger").GetDecimal());
        Assert.Equal(KurHasFiyati, y.GetProperty("hasBirimFiyat").GetDecimal());
        Assert.Equal("HAREKET", y.GetProperty("hasDegerKaynagi").GetString());
        Assert.Equal(75_000.00m, y.GetProperty("defterToplami").GetDecimal());
        // Matrah = 75.000 − 68.272 = 6.728 (KDV dahil) → 5.606,67 + 1.121,33
        Assert.Equal(5_606.67m, y.GetProperty("matrah").GetDecimal());
        Assert.Equal(1_121.33m, y.GetProperty("kdvTutar").GetDecimal());
    }

    [Fact]
    public async Task Onizleme_zaten_faturalanmis_kumede_fatura_no_BILDIRIR()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 damgalı");
        var h = Satir(k, null, 75_000m, -10m, faturaNo: "FTR-2026-000042");
        kurulum.Hareketler.Add(h);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var y = Oku(await new MFatura.FaturaServisi(db)
            .OzelMatrahOnizleme(h.Id, 0.20m, 75_000m, k.Islem));

        // Önizleme SALT OKUNUR: hata fırlatmaz, sebebi BİLDİRİR (UX-S30).
        Assert.Equal("FTR-2026-000042", y.GetProperty("faturaNo").GetString());
    }

    [Fact]
    public async Task Onizleme_yabanci_kiracinin_hareketinde_404_verir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 önizleme A");
        var yabanci = TestKurulum.KiraciKur(kurulum, "PAKET38 önizleme B");
        var h = Satir(yabanci, null, 75_000m, -10m);
        kurulum.Hareketler.Add(h);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new MFatura.FaturaServisi(db).OzelMatrahOnizleme(h.Id, 0.20m, 75_000m, k.Islem));
    }

    /// <summary>
    /// ÇOK KATMANLI KORUMA TUZAĞI (Aşkın): global HasQueryFilter zaten 404
    /// ürettiği için "elle TenantId şartı" kiracılı bağlamda mutasyonla
    /// öldürülemez. Bu test FİLTRESİZ bağlamda koşar; geriye tek koruma olarak
    /// servisin kendi yazdığı <c>h.TenantId == islem.TenantId</c> şartı kalır
    /// (ADR-0010 "tek koruma katmanı bırakılmaz").
    /// </summary>
    [Fact]
    public async Task Onizleme_FILTRESIZ_baglamda_da_yabanci_hareketi_REDDEDER()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 filtresiz A");
        var yabanci = TestKurulum.KiraciKur(kurulum, "PAKET38 filtresiz B");
        var h = Satir(yabanci, null, 75_000m, -10m);
        kurulum.Hareketler.Add(h);
        kurulum.SaveChanges();

        using var filtresiz = vt.OlusturSistem();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new MFatura.FaturaServisi(filtresiz)
                .OzelMatrahOnizleme(h.Id, 0.20m, 75_000m, k.Islem));
    }

    // ── 10. Faturalanabilir işlem listesi ────────────────────────────────────

    [Fact]
    public async Task Faturalanabilir_liste_DAMGALI_hareketi_getirmez_ve_sinirini_soyler()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 liste");
        kurulum.Hareketler.Add(Satir(k, null, 1_000m, -1m));
        kurulum.Hareketler.Add(Satir(k, null, 2_000m, -2m, faturaNo: "FTR-2026-000001"));
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var y = Oku(await new MFatura.FaturaServisi(db).FaturalanabilirIslemler(k.Islem, 30, 50));

        Assert.Equal(1, y.GetProperty("islemler").GetArrayLength());
        Assert.Equal(50, y.GetProperty("limit").GetInt32());
        Assert.False(y.GetProperty("dahaVar").GetBoolean());
        // İşaret temizlenir: tutar ve has gram ekranda pozitif görünür.
        var ilk = y.GetProperty("islemler")[0];
        Assert.Equal(1_000.00m, ilk.GetProperty("tlTutari").GetDecimal());
        Assert.Equal(1.000000m, ilk.GetProperty("hasGrami").GetDecimal());
    }

    [Fact]
    public async Task Faturalanabilir_liste_sinira_degince_dahaVar_TRUE_doner()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 sınır");
        for (var i = 0; i < 3; i++) kurulum.Hareketler.Add(Satir(k, null, 1_000m + i, -1m));
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var y = Oku(await new MFatura.FaturaServisi(db).FaturalanabilirIslemler(k.Islem, 30, 2));

        Assert.Equal(2, y.GetProperty("islemler").GetArrayLength());
        Assert.True(y.GetProperty("dahaVar").GetBoolean());
    }

    [Fact]
    public async Task Faturalanabilir_liste_YABANCI_kiracinin_hareketini_getirmez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 liste A");
        var yabanci = TestKurulum.KiraciKur(kurulum, "PAKET38 liste B");
        kurulum.Hareketler.Add(Satir(yabanci, null, 9_999m, -3m));
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var y = Oku(await new MFatura.FaturaServisi(db).FaturalanabilirIslemler(k.Islem, 30, 50));

        Assert.Equal(0, y.GetProperty("islemler").GetArrayLength());
    }

    // ── 11. Banka dekontu: KDV türü AÇIKÇA STANDART ──────────────────────────

    [Fact]
    public async Task Banka_dekontundan_kesilen_faturada_kdv_turu_STANDART_ve_kod_NULL()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET38 banka");
        var hesapId = Kimlik.Ulid();
        var hareketId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        kurulum.BankaHesaplari.Add(new BankaHesap
        {
            Id = hesapId, TenantId = k.TenantId, BankaAdi = "Test Bank", HesapAdi = "Vadesiz",
            Iban = "TR200000000000000000000001", ParaBirimi = "TRY", Bakiye = 0m,
            Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        kurulum.BankaHareketleri.Add(new BankaHareket
        {
            Id = hareketId, TenantId = k.TenantId, BankaHesapId = hesapId, Yon = "GELEN",
            Tutar = 1_200m, ParaBirimi = "TRY", Tarih = now, Durum = "BEKLIYOR",
            CariId = k.CariId, Olusturuldu = now,
        });
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await new Kuyumcu.Api.Modules.Banka.BankaServisi(db).FaturayaDonustur(
            hareketId, new Kuyumcu.Api.Modules.Banka.FaturayaDonusturDto(), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var f = kontrol.Faturalar.Single(x => x.TenantId == k.TenantId);
        Assert.Equal("STANDART", f.KdvTuru);
        Assert.Equal(0m, f.HasDeger);
        Assert.Null(f.OzelMatrahKodu);
        Assert.Null(f.HasGram);
        Assert.Equal(200.00m, f.KdvTutar);          // davranış DEĞİŞMEDİ
    }
}
