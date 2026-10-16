using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using MFatura = Kuyumcu.Api.Modules.Fatura;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// KALEMLİ FATURA — paket 1 Faz A [DB]
//
// Çekirdek sözleşmeler:
//   • Kalem gönderilmeyen istek BUGÜNKÜ yolu, BUGÜNKÜ hesabı alır (regresyon).
//   • Kalemli faturada toplam SUNUCUDA hesaplanır, TOLERANS YOKTUR.
//   • KUYUM'da has bedeli ELLE GİRİLMEZ; dto.HasDeger YOKSAYILIR.
//   • Kur kaydı yoksa FAIL-CLOSED 400 — "has bedeli 0" varsayılmaz.
//   • Taş ibaresi kaleme METİN olarak KOPYALANIR; ürün kartı değişse de kalem değişmez.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class FaturaKalemTestleri(TestVeritabani vt)
{
    private const decimal KurHasFiyati = 6_827.20m;      // TestKurulum.KiraciKur

    private static decimal Yuvarla2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);

    private static MFatura.FaturaKalemDto Kalem(
        string? urunId, decimal miktar, decimal birimFiyat,
        decimal? iscilik = null, decimal? indirim = null, string? hareketId = null) => new()
    {
        UrunId = urunId, Miktar = miktar, BirimFiyat = birimFiyat,
        IscilikTl = iscilik, IndirimTutari = indirim, HareketId = hareketId,
    };

    // ── 1. Kalemli fatura yazılır ────────────────────────────────────────────

    [Fact]
    public async Task Kalemli_fatura_N_satir_yazar_ve_kalem_kaynagi_KALEM_olur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 kalemli");
        using var db = vt.Olustur(k);

        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 3_600m, KdvOran = 0.20m,
            Kalemler = [Kalem(k.UrunId, 1m, 1_000m), Kalem(k.UrunId, 2m, 1_300m)],
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var fatura = kontrol.Faturalar.Single(f => f.TenantId == k.TenantId);
        var kalemler = kontrol.FaturaKalemleri
            .Where(x => x.TenantId == k.TenantId).OrderBy(x => x.Sira).ToList();

        Assert.Equal("KALEM", fatura.KalemKaynagi);
        Assert.Equal("KALEM", fatura.HasDegerKaynagi);
        Assert.Equal(2, kalemler.Count);
        Assert.Equal([1, 2], kalemler.Select(x => x.Sira));
        Assert.Equal(3_600.00m, fatura.Toplam);
        Assert.Equal(600.00m, fatura.KdvTutar);           // 1000→166,67 + 2600→433,33
        Assert.Equal(kalemler.Sum(x => x.SatirToplam), fatura.Toplam);
        Assert.Equal(kalemler.Sum(x => x.KdvTutar), fatura.KdvTutar);
    }

    /// <summary>
    /// KDV KALEMLERDEN TOPLANIR: 3 × 10,01 TL kalemde satır başına 1,67 → 5,01.
    /// Fatura toplamı üzerinden (30,03) hesaplansaydı 5,00 çıkardı ve belge
    /// toplamı kalem toplamından 1 kuruş saparadı.
    /// </summary>
    [Fact]
    public async Task Uc_kalemli_faturada_KDV_kalemlerden_TOPLANIR_belge_uzerinden_degil()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 yuvarlama");
        using var db = vt.Olustur(k);

        await new MFatura.FaturaServisi(db).Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 30.03m, KdvOran = 0.20m,
            Kalemler = [Kalem(k.UrunId, 1m, 10.01m), Kalem(k.UrunId, 1m, 10.01m),
                        Kalem(k.UrunId, 1m, 10.01m)],
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var fatura = kontrol.Faturalar.Single(f => f.TenantId == k.TenantId);
        Assert.Equal(5.01m, fatura.KdvTutar);          // 3 × 1,67 — belge üzerinden 5,00 olurdu
        Assert.Equal(25.02m, fatura.AraToplam);
        Assert.Equal(5.01m, kontrol.FaturaKalemleri
            .Where(x => x.TenantId == k.TenantId).Sum(x => x.KdvTutar));
    }

    // ── 2. Tolerans yok ──────────────────────────────────────────────────────

    [Fact]
    public async Task Toplam_bir_kurus_sapinca_400_doner_ve_HICBIR_satir_yazilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 tolerans");
        using var db = vt.Olustur(k);

        var servis = new MFatura.FaturaServisi(db);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() => servis.Olustur(
            new MFatura.FaturaOlusturDto
            {
                CariId = k.CariId, Toplam = 3_600.01m, KdvOran = 0.20m,
                Kalemler = [Kalem(k.UrunId, 1m, 1_000m), Kalem(k.UrunId, 2m, 1_300m)],
            }, k.Islem));

        Assert.Contains("tolerans", hata.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, kontrol.FaturaKalemleri.Count(x => x.TenantId == k.TenantId));
        Assert.Equal(0, kontrol.Faturalar.Count(f => f.TenantId == k.TenantId));
    }

    // ── 3. Geriye uyum (REGRESYON — önce yazıldı) ────────────────────────────

    [Fact]
    public async Task Kalemsiz_istek_BUGUNKU_yolu_alir_kalem_kaynagi_BASLIK()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 regresyon");
        using var db = vt.Olustur(k);

        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 12_000m, KdvOran = 0.20m,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var fatura = kontrol.Faturalar.Single(f => f.TenantId == k.TenantId);

        Assert.Equal("BASLIK", fatura.KalemKaynagi);
        Assert.Equal("ELLE", fatura.HasDegerKaynagi);
        Assert.Equal(2_000.00m, fatura.KdvTutar);         // bugünkü hesap, bit bit aynı
        Assert.Equal(10_000.00m, fatura.AraToplam);
        Assert.Equal(0, kontrol.FaturaKalemleri.Count(x => x.TenantId == k.TenantId));
    }

    // ── 4. KUYUM: has bedeli SUNUCUDA türetilir ──────────────────────────────

    [Fact]
    public async Task Kuyum_kalemli_faturada_dto_HasDeger_YOKSAYILIR_kalemlerden_toplanir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 kuyum");
        using var db = vt.Olustur(k);

        var beklenenHas = Yuvarla2(1m * k.UrunHasGrami * KurHasFiyati);
        var toplam = 80_000m;

        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = toplam, KdvOran = 0.20m, KdvTuru = "KUYUM",
            HasDeger = 1m,                                   // ELLE BEYAN — yok sayılmalı
            Kalemler = [Kalem(k.UrunId, 1m, toplam)],
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var fatura = kontrol.Faturalar.Single(f => f.TenantId == k.TenantId);
        var kalem = kontrol.FaturaKalemleri.Single(x => x.TenantId == k.TenantId);

        Assert.Equal(beklenenHas, kalem.HasDeger);
        Assert.Equal(beklenenHas, fatura.HasDeger);
        Assert.NotEqual(1m, fatura.HasDeger);
        // KDV yalnız has dışı tutardan
        var matrahKdvDahil = toplam - beklenenHas;
        Assert.Equal(Yuvarla2(matrahKdvDahil - Yuvarla2(matrahKdvDahil / 1.20m)), fatura.KdvTutar);
        // Kalem ürün kartından KOPYA taşır
        Assert.Equal(k.UrunHasGrami, kalem.HasGrami);
        Assert.Equal("K22", kalem.AyarId);
    }

    [Fact]
    public async Task Kuyum_kalemli_faturada_kur_kaydi_yoksa_400()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 kursuz");
        // Kur kaydı silinir: "has bedeli 0" varsayılmaz, fail-closed 400 beklenir.
        var kur = kurulum.KurSnapshots.Single(x => x.Id == k.KurId);
        kurulum.KurSnapshots.Remove(kur);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() => servis.Olustur(
            new MFatura.FaturaOlusturDto
            {
                CariId = k.CariId, Toplam = 80_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
                Kalemler = [Kalem(k.UrunId, 1m, 80_000m)],
            }, k.Islem));

        // ⚠ Mesaj SERVİS katmanının kendi mesajıdır (motorunki değil): kur kapısı
        // servisten kaldırılırsa motor yine 400 verir ama BAŞKA metinle — bu assert
        // çok katmanlı korumayı ayırır ve M1-3 mutasyonunu gerçekten yakalar.
        Assert.Contains("kalem için kur kaydı bulunamadı", hata.Message);
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, kontrol.Faturalar.Count(f => f.TenantId == k.TenantId));
    }

    // ── 5. Taş ibaresi KOPYADIR ──────────────────────────────────────────────

    [Fact]
    public async Task Tas_ibaresi_kaleme_METIN_olarak_kopyalanir_ve_urun_degisse_de_DEGISMEZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 taş");
        var urun = kurulum.Urunler.Single(u => u.Id == k.UrunId);
        urun.TasKokeni = "SENTETIK";
        urun.TasIbaresi = "sentetik taşlıdır";
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 1_200m, KdvOran = 0.20m,
            Kalemler = [Kalem(k.UrunId, 1m, 1_200m)],
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.TenantId == k.TenantId);
        Assert.Equal("sentetik taşlıdır", kalem.TasIbaresi);

        // Ürün kartı sonradan düzeltilir — BASILMIŞ BELGE DEĞİŞMEZ.
        var urun2 = kontrol.Urunler.Single(u => u.Id == k.UrunId);
        urun2.TasIbaresi = "laboratuvarda üretilmiştir";
        kontrol.SaveChanges();

        using var kontrol2 = vt.OlusturSistem();
        Assert.Equal("sentetik taşlıdır",
            kontrol2.FaturaKalemleri.Single(x => x.TenantId == k.TenantId).TasIbaresi);
    }

    // ── 6. Sınırlar SERVİS İÇİNDE doğrulanır (DataAnnotations yalnız controller yolunda) ──

    [Fact]
    public async Task Ikiyuzbir_kalem_400_doner()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 limit");
        using var db = vt.Olustur(k);

        var kalemler = Enumerable.Range(0, 201).Select(_ => Kalem(k.UrunId, 1m, 10m)).ToList();
        var servis = new MFatura.FaturaServisi(db);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() => servis.Olustur(
            new MFatura.FaturaOlusturDto
            {
                CariId = k.CariId, Toplam = 2_010m, KdvOran = 0.20m, Kalemler = kalemler,
            }, k.Islem));

        Assert.Contains("200", hata.Message);
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, kontrol.FaturaKalemleri.Count(x => x.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Ikiyuz_kalem_SINIRDA_kabul_edilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 sınır");
        using var db = vt.Olustur(k);

        var kalemler = Enumerable.Range(0, 200).Select(_ => Kalem(k.UrunId, 1m, 10m)).ToList();
        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 2_000m, KdvOran = 0.20m, Kalemler = kalemler,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(200, kontrol.FaturaKalemleri.Count(x => x.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Negatif_miktar_ve_negatif_birim_fiyat_400_doner()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 negatif");
        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);

        await Assert.ThrowsAsync<BadRequestException>(() => servis.Olustur(
            new MFatura.FaturaOlusturDto
            {
                CariId = k.CariId, Toplam = 100m, Kalemler = [Kalem(k.UrunId, -1m, 100m)],
            }, k.Islem));

        await Assert.ThrowsAsync<BadRequestException>(() => servis.Olustur(
            new MFatura.FaturaOlusturDto
            {
                CariId = k.CariId, Toplam = 100m, Kalemler = [Kalem(k.UrunId, 1m, -100m)],
            }, k.Islem));
    }

    [Fact]
    public async Task Indirimli_kalemde_matrah_indirim_SONRASI_tutardan_hesaplanir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 indirim");
        using var db = vt.Olustur(k);

        // 1 × 1.000 + 200 işçilik − 200 indirim = 1.000 TL satır toplamı
        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 1_000m, KdvOran = 0.20m,
            Kalemler = [Kalem(k.UrunId, 1m, 1_000m, iscilik: 200m, indirim: 200m)],
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(1_000.00m, kalem.SatirToplam);
        Assert.Equal(833.33m, kalem.KdvMatrah);
        Assert.Equal(166.67m, kalem.KdvTutar);
    }

    // ── 7. Kiracı sınırı (Mehmet K1-1) ───────────────────────────────────────

    /// <summary>
    /// A kiracısının işlemiyle B kiracısının hareketine bağlanma denemesi 404.
    /// ⚠ Bağlam FİLTRESİZ kurulur (vt.OlusturSistem) — aksi hâlde global sorgu
    /// filtresi zaten 404 üretir ve "TenantId şartı kaldırılırsa test kırmızı"
    /// mutasyonu hiçbir şey yakalamaz (Aşkın'ın çok katmanlı koruma tuzağı).
    /// </summary>
    [Fact]
    public async Task Yabanci_kiracinin_hareketine_baglanma_denemesi_404()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "PAKET1 kiracı A");
        var b = TestKurulum.KiraciKur(kurulum, "PAKET1 kiracı B");

        var yabanciHareketId = Kimlik.Ulid();
        kurulum.Hareketler.Add(new Hareket
        {
            Id = yabanciHareketId, TenantId = b.TenantId, SubeId = b.SubeId, Tur = "SATIS",
            Tarih = DateTime.UtcNow, TlTutari = 1_000m, HasGrami = -1m,
            KurSnapshotId = b.KurId, KullaniciId = b.KullaniciId, Olusturuldu = DateTime.UtcNow,
        });
        kurulum.SaveChanges();

        using var filtresiz = vt.OlusturSistem();
        var servis = new MFatura.FaturaServisi(filtresiz);

        await Assert.ThrowsAsync<NotFoundException>(() => servis.Olustur(
            new MFatura.FaturaOlusturDto
            {
                CariId = a.CariId, Toplam = 1_000m, KdvOran = 0.20m,
                Kalemler = [Kalem(a.UrunId, 1m, 1_000m, hareketId: yabanciHareketId)],
            }, a.Islem));
    }

    [Fact]
    public async Task Yabanci_kiracinin_urunune_baglanma_denemesi_404()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "PAKET1 ürün A");
        var b = TestKurulum.KiraciKur(kurulum, "PAKET1 ürün B");

        using var filtresiz = vt.OlusturSistem();
        var servis = new MFatura.FaturaServisi(filtresiz);

        await Assert.ThrowsAsync<NotFoundException>(() => servis.Olustur(
            new MFatura.FaturaOlusturDto
            {
                CariId = a.CariId, Toplam = 1_000m, KdvOran = 0.20m,
                Kalemler = [Kalem(b.UrunId, 1m, 1_000m)],
            }, a.Islem));
    }

    // ── 8. Kalem okuma ucu ───────────────────────────────────────────────────

    [Fact]
    public async Task Kalemsiz_faturada_okuma_ucu_BOS_LISTE_ve_BASLIK_doner()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 okuma");
        using var db = vt.Olustur(k);

        var servis = new MFatura.FaturaServisi(db);
        var fatura = await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 1_200m, KdvOran = 0.20m,
        }, k.Islem);

        var id = System.Text.Json.JsonDocument
            .Parse(System.Text.Json.JsonSerializer.Serialize(fatura))
            .RootElement.GetProperty("Id").GetString()!;

        var yanit = System.Text.Json.JsonDocument
            .Parse(System.Text.Json.JsonSerializer.Serialize(await servis.Kalemler(id, k.Islem)))
            .RootElement;

        Assert.Equal("BASLIK", yanit.GetProperty("kalemKaynagi").GetString());
        Assert.Empty(yanit.GetProperty("kalemler").EnumerateArray());
    }

    [Fact]
    public async Task Yabanci_kiracinin_faturasinin_kalemleri_404()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "PAKET1 okuma A");
        var b = TestKurulum.KiraciKur(kurulum, "PAKET1 okuma B");

        using var dbB = vt.Olustur(b);
        var fatura = await new MFatura.FaturaServisi(dbB).Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = b.CariId, Toplam = 1_200m, KdvOran = 0.20m,
            Kalemler = [Kalem(b.UrunId, 1m, 1_200m)],
        }, b.Islem);
        var id = System.Text.Json.JsonDocument
            .Parse(System.Text.Json.JsonSerializer.Serialize(fatura))
            .RootElement.GetProperty("Id").GetString()!;

        using var filtresiz = vt.OlusturSistem();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new MFatura.FaturaServisi(filtresiz).Kalemler(id, a.Islem));
    }

    // ── 9. Damga kuralı kalemli yolda da geçerli ─────────────────────────────

    [Fact]
    public async Task Kalemli_faturada_fisin_TUM_hareketleri_damgalanir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 damga");
        var fisId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        var h1 = Kimlik.Ulid();
        var h2 = Kimlik.Ulid();
        foreach (var (id, tl) in new[] { (h1, 1_000m), (h2, 2_600m) })
            kurulum.Hareketler.Add(new Hareket
            {
                Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS", Tarih = now,
                TlTutari = tl, HasGrami = -1m, FisId = fisId, KurSnapshotId = k.KurId,
                CariId = k.CariId, UrunId = k.UrunId, KullaniciId = k.KullaniciId, Olusturuldu = now,
            });
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 3_600m, KdvOran = 0.20m, HareketId = h1,
            Kalemler = [Kalem(k.UrunId, 1m, 1_000m, hareketId: h1),
                        Kalem(k.UrunId, 2m, 1_300m, hareketId: h2)],
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var damgalar = kontrol.Hareketler.Where(h => h.FisId == fisId)
            .Select(h => h.FaturaNo).ToList();
        Assert.Equal(2, damgalar.Count);
        Assert.All(damgalar, d => Assert.False(string.IsNullOrEmpty(d)));
        Assert.Single(damgalar.Distinct());
    }

    [Fact]
    public async Task Ayni_fisten_ikinci_kalemli_fatura_400_doner()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 ikinci");
        var fisId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        var h1 = Kimlik.Ulid();
        kurulum.Hareketler.Add(new Hareket
        {
            Id = h1, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS", Tarih = now,
            TlTutari = 1_000m, HasGrami = -1m, FisId = fisId, KurSnapshotId = k.KurId,
            CariId = k.CariId, UrunId = k.UrunId, KullaniciId = k.KullaniciId, Olusturuldu = now,
        });
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        var dto = new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 1_000m, KdvOran = 0.20m, HareketId = h1,
            Kalemler = [Kalem(k.UrunId, 1m, 1_000m, hareketId: h1)],
        };
        await servis.Olustur(dto, k.Islem);

        var hata = await Assert.ThrowsAsync<BadRequestException>(() => servis.Olustur(dto, k.Islem));
        Assert.Contains("faturalandırılmış", hata.Message);
    }
}
