using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q9-2 — TASLAK FATURAYA SATIR EKLEME: GERÇEK HTTP BORU HATTI (QA turu 9, KRİTİK)
//
// POST /api/v1/faturalar/{id}/kalemler sözleşmesi:
//   • yalnız TASLAK + KALEMLİ belgeye satır eklenir; yanıt KalemYaniti'dır ve
//     başlık toplamı KALEMLERDEN yeniden toplanır (ekran toplamı buradan okur);
//   • kesilmiş belge DEĞİŞMEZ (400 + düzeltme yolu) — VUK/P-22 kuralı;
//   • kalemsiz (BASLIK) taslağa satır eklenmez (elle toplam sessizce ezilmez);
//   • satış kaydına (hareket) bağlı satır sonradan eklenmez;
//   • miktar 0 / negatif fiyat → 400, veri DEĞİŞMEZ;
//   • KUYUM'da ürünsüz satır 400; ürünlü satırda has bedeli SUNUCUDA türetilir;
//   • yetki: SAHIP/MUDUR/MUHASEBE; TEZGAHTAR 403; başka kiracının belgesi 404.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class Q92FaturaKalemEklemeHttpTestleri(HttpFikstur f)
{
    private const decimal KurHasFiyati = 6_827.20m;      // TestKurulum.KiraciKur

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static decimal Yuvarla2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);

    /// <summary>İki kalemli STANDART taslak: 2×1000 + 500 = 2.500.</summary>
    private static async Task<string> KalemliTaslak(Kiraci k, HttpClient c)
    {
        var y = await c.PostAsJsonAsync("/api/v1/faturalar", new
        {
            cariId = k.CariId, tur = "SATIS", toplam = 2500m, kdvOran = 0.20m,
            kalemler = new object[]
            {
                new { aciklama = "Q92 bilezik", miktar = 2m, birimFiyat = 1000m },
                new { aciklama = "Q92 kolye", miktar = 1m, birimFiyat = 500m },
            },
        });
        var govde = await y.Content.ReadAsStringAsync();
        Assert.True(y.StatusCode == HttpStatusCode.OK, $"{(int)y.StatusCode} {govde}");
        return JsonDocument.Parse(govde).RootElement.GetProperty("id").GetString()!;
    }

    private async Task<(Fatura Fatura, List<FaturaKalemi> Kalemler)> Oku(Kiraci k, string id)
    {
        using var db = f.Vt.OlusturSistem();
        var fatura = await db.Faturalar.AsNoTracking().SingleAsync(x => x.Id == id && x.TenantId == k.TenantId);
        var kalemler = await db.FaturaKalemleri.AsNoTracking()
            .Where(x => x.FaturaId == id && x.TenantId == k.TenantId).OrderBy(x => x.Sira).ToListAsync();
        return (fatura, kalemler);
    }

    private static async Task<string> Mesaj(HttpResponseMessage y)
    {
        var j = JsonDocument.Parse(await y.Content.ReadAsStringAsync()).RootElement;
        return j.GetProperty("message") is { ValueKind: JsonValueKind.Array } dizi
            ? string.Join(" ", dizi.EnumerateArray().Select(x => x.GetString()))
            : j.GetProperty("message").GetString()!;
    }

    [Fact]
    public async Task Q92_taslaga_satir_EKLENIR_ve_BASLIK_TOPLAMI_kalemlerden_toplanir()
    {
        var k = YeniKiraci("Q92H-EKLE");
        var sahip = f.Ortam.Istemci(k);
        var id = await KalemliTaslak(k, sahip);

        var y = await sahip.PostAsJsonAsync($"/api/v1/faturalar/{id}/kalemler",
            new { aciklama = "Q92 yuzuk", miktar = 2m, birimFiyat = 300m, iscilikTl = 50m });
        var metin = await y.Content.ReadAsStringAsync();
        Assert.True(y.StatusCode == HttpStatusCode.OK, $"{(int)y.StatusCode} {metin}");

        // Yanıt KalemYaniti: 2.500 + (2×300 + 50) = 3.150; yeni satır 3. sırada.
        var yj = JsonDocument.Parse(metin).RootElement;
        Assert.Equal(3150m, yj.GetProperty("toplam").GetDecimal());
        var satirlar = yj.GetProperty("kalemler").EnumerateArray().ToList();
        Assert.Equal(3, satirlar.Count);
        var yeni = satirlar[2];
        Assert.Equal(3, yeni.GetProperty("sira").GetInt32());
        Assert.Equal("Q92 yuzuk", yeni.GetProperty("aciklama").GetString());
        Assert.Equal(650m, yeni.GetProperty("satirToplam").GetDecimal());

        var (fatura, kalemler) = await Oku(k, id);
        Assert.Equal("TASLAK", fatura.Durum);
        Assert.Null(fatura.FaturaNo);                          // taslak numara TÜKETMEZ
        Assert.Equal(3150m, fatura.Toplam);
        Assert.Equal(kalemler.Sum(x => x.KdvTutar), fatura.KdvTutar);
        Assert.Equal(fatura.Toplam - fatura.KdvTutar, fatura.AraToplam);
        Assert.Equal(Yuvarla2(650m - 650m / 1.20m), kalemler[2].KdvTutar);
        Assert.Equal("STANDART", kalemler[2].KdvTuru);
        Assert.Equal(0.20m, kalemler[2].KdvOran);                 // oran belgeden
        Assert.Null(kalemler[2].HareketId);

        // Eklenen satır da P-54 ucuyla düzeltilebilir (tek düzenleme yolu).
        var p = await sahip.PatchAsJsonAsync($"/api/v1/faturalar/{id}/kalemler/{kalemler[2].Id}", new { miktar = 1m });
        Assert.Equal(HttpStatusCode.OK, p.StatusCode);
        Assert.Equal(2850m, (await p.Json()).GetProperty("toplam").GetDecimal());
    }

    [Fact]
    public async Task Q92_urunlu_satirda_ACIKLAMA_urun_adindan_gelir_ve_urun_kopyalanir()
    {
        var k = YeniKiraci("Q92H-URUN");
        var sahip = f.Ortam.Istemci(k);
        var id = await KalemliTaslak(k, sahip);

        var y = await sahip.PostAsJsonAsync($"/api/v1/faturalar/{id}/kalemler",
            new { urunId = k.UrunId, miktar = 1m, birimFiyat = 400m });
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);

        var (_, kalemler) = await Oku(k, id);
        var yeni = kalemler.Single(x => x.Sira == 3);
        Assert.Equal("Test bilezik", yeni.Aciklama);
        Assert.Equal(k.UrunId, yeni.UrunId);
        Assert.Equal(k.UrunHasGrami, yeni.HasGrami);
        Assert.Equal("K22", yeni.AyarId);
    }

    [Fact]
    public async Task Q92_KESILMIS_faturaya_satir_EKLENMEZ_400_ve_DUZELTME_YOLU_yazar()
    {
        var k = YeniKiraci("Q92H-KESIK");
        var sahip = f.Ortam.Istemci(k);
        var id = await KalemliTaslak(k, sahip);
        var kes = await sahip.PatchAsJsonAsync($"/api/v1/faturalar/{id}/durum", new { durum = "KESILDI" });
        Assert.Equal(HttpStatusCode.OK, kes.StatusCode);

        var y = await sahip.PostAsJsonAsync($"/api/v1/faturalar/{id}/kalemler",
            new { aciklama = "Q92 sonradan", miktar = 1m, birimFiyat = 100m });
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
        var mesaj = await Mesaj(y);
        Assert.Contains("Yalnız TASLAK", mesaj);
        Assert.Contains("iptal edip yeni belge", mesaj);

        var (fatura, kalemler) = await Oku(k, id);
        Assert.Equal(2, kalemler.Count);
        Assert.Equal(2500m, fatura.Toplam);
        Assert.Equal("KESILDI", fatura.Durum);
    }

    [Fact]
    public async Task Q92_SATIRSIZ_taslaga_satir_EKLENMEZ_elle_toplam_EZILMEZ()
    {
        var k = YeniKiraci("Q92H-BASLIK");
        var sahip = f.Ortam.Istemci(k);
        var y0 = await sahip.PostAsJsonAsync("/api/v1/faturalar",
            new { cariId = k.CariId, tur = "SATIS", toplam = 900m, kdvOran = 0.20m });
        Assert.Equal(HttpStatusCode.OK, y0.StatusCode);
        var id = (await y0.Json()).GetProperty("id").GetString()!;

        var y = await sahip.PostAsJsonAsync($"/api/v1/faturalar/{id}/kalemler",
            new { aciklama = "Q92 x", miktar = 1m, birimFiyat = 100m });
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
        Assert.Contains("satırsız", await Mesaj(y));

        var (fatura, kalemler) = await Oku(k, id);
        Assert.Empty(kalemler);
        Assert.Equal(900m, fatura.Toplam);
        Assert.Equal("BASLIK", fatura.KalemKaynagi);
    }

    [Fact]
    public async Task Q92_miktar_SIFIR_negatif_fiyat_ve_bos_aciklama_400_veri_DEGISMEZ()
    {
        var k = YeniKiraci("Q92H-GECERSIZ");
        var sahip = f.Ortam.Istemci(k);
        var id = await KalemliTaslak(k, sahip);

        var sifir = await sahip.PostAsJsonAsync($"/api/v1/faturalar/{id}/kalemler",
            new { aciklama = "Q92 x", miktar = 0m, birimFiyat = 100m });
        Assert.Equal(HttpStatusCode.BadRequest, sifir.StatusCode);
        // PAKET GOC-A: controller yolunda FaturaKalemDto.Miktar [Gram] (K10) — ileti sözlükten (ekranın alan altı
        // metniyle AYNI), alan adı gövdenin 'alanlar' anahtarında: miktar.
        var sifirGovde = await sifir.Content.ReadAsStringAsync();
        Assert.True(JsonDocument.Parse(sifirGovde).RootElement.GetProperty("alanlar").TryGetProperty("miktar", out _), sifirGovde);
        Assert.Contains(Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(
            Kuyumcu.Core.Domain.Girdi.GirdiKodu.SayiSifir), "tr"), await Mesaj(sifir));

        var negatif = await sahip.PostAsJsonAsync($"/api/v1/faturalar/{id}/kalemler",
            new { aciklama = "Q92 x", miktar = 1m, birimFiyat = -5m });
        Assert.Equal(HttpStatusCode.BadRequest, negatif.StatusCode);

        var bos = await sahip.PostAsJsonAsync($"/api/v1/faturalar/{id}/kalemler",
            new { aciklama = "   ", miktar = 1m, birimFiyat = 100m });
        Assert.Equal(HttpStatusCode.BadRequest, bos.StatusCode);
        Assert.Contains("açıklama", await Mesaj(bos), StringComparison.CurrentCultureIgnoreCase);

        var indirimFazla = await sahip.PostAsJsonAsync($"/api/v1/faturalar/{id}/kalemler",
            new { aciklama = "Q92 x", miktar = 1m, birimFiyat = 100m, indirimTutari = 100m });
        Assert.Equal(HttpStatusCode.BadRequest, indirimFazla.StatusCode);
        Assert.Contains("satır toplamı", await Mesaj(indirimFazla));

        var (fatura, kalemler) = await Oku(k, id);
        Assert.Equal(2, kalemler.Count);
        Assert.Equal(2500m, fatura.Toplam);
    }

    [Fact]
    public async Task Q92_hareket_bagli_satir_sonradan_EKLENMEZ_400()
    {
        var k = YeniKiraci("Q92H-HAREKET");
        var sahip = f.Ortam.Istemci(k);
        var id = await KalemliTaslak(k, sahip);

        var y = await sahip.PostAsJsonAsync($"/api/v1/faturalar/{id}/kalemler",
            new { aciklama = "Q92 x", miktar = 1m, birimFiyat = 100m, hareketId = "01HAREKETYOK0000000000000A" });
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
        Assert.Contains("Fişten Fatura Kes", await Mesaj(y));
        Assert.Equal(2, (await Oku(k, id)).Kalemler.Count);
    }

    [Fact]
    public async Task Q92_KUYUM_taslakta_urunsuz_satir_400_urunlu_satirda_HAS_BEDELI_sunucuda_turetilir()
    {
        var k = YeniKiraci("Q92H-KUYUM");
        var sahip = f.Ortam.Istemci(k);
        var y0 = await sahip.PostAsJsonAsync("/api/v1/faturalar", new
        {
            cariId = k.CariId, tur = "SATIS", toplam = 80000m, kdvOran = 0.20m, kdvTuru = "KUYUM",
            kalemler = new object[] { new { urunId = k.UrunId, miktar = 1m, birimFiyat = 80000m } },
        });
        var m0 = await y0.Content.ReadAsStringAsync();
        Assert.True(y0.StatusCode == HttpStatusCode.OK, $"{(int)y0.StatusCode} {m0}");
        var id = JsonDocument.Parse(m0).RootElement.GetProperty("id").GetString()!;

        var urunsuz = await sahip.PostAsJsonAsync($"/api/v1/faturalar/{id}/kalemler",
            new { aciklama = "Q92 elle", miktar = 1m, birimFiyat = 1000m });
        Assert.Equal(HttpStatusCode.BadRequest, urunsuz.StatusCode);
        Assert.Contains("ürün", await Mesaj(urunsuz));

        var y = await sahip.PostAsJsonAsync($"/api/v1/faturalar/{id}/kalemler",
            new { urunId = k.UrunId, miktar = 1m, birimFiyat = 80000m });
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);

        var beklenenHas = Yuvarla2(1m * k.UrunHasGrami * KurHasFiyati);
        var (fatura, kalemler) = await Oku(k, id);
        Assert.Equal(2, kalemler.Count);
        Assert.Equal(beklenenHas, kalemler[1].HasDeger);
        Assert.Equal("KUYUM", kalemler[1].KdvTuru);
        Assert.Equal(160000m, fatura.Toplam);
        Assert.Equal(kalemler.Sum(x => x.HasDeger), fatura.HasDeger);
        Assert.Equal(kalemler.Sum(x => x.KdvTutar), fatura.KdvTutar);
        // Başlığın has gram kanıtı da iki satırın toplamı (UX-S21 — iki ayrı gerçek yok).
        Assert.Equal(2m * k.UrunHasGrami, fatura.HasGram);
    }

    [Fact]
    public async Task Q92_TEZGAHTAR_satir_ekleyemez_403_baska_kiracinin_belgesi_404()
    {
        var k = YeniKiraci("Q92H-YETKI");
        var sahip = f.Ortam.Istemci(k);
        var id = await KalemliTaslak(k, sahip);

        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");
        var y = await tezgahtar.PostAsJsonAsync($"/api/v1/faturalar/{id}/kalemler",
            new { aciklama = "Q92 x", miktar = 1m, birimFiyat = 100m });
        Assert.Equal(HttpStatusCode.Forbidden, y.StatusCode);

        var baska = YeniKiraci("Q92H-BASKA");
        var yabanci = await f.Ortam.Istemci(baska).PostAsJsonAsync($"/api/v1/faturalar/{id}/kalemler",
            new { aciklama = "Q92 x", miktar = 1m, birimFiyat = 100m });
        Assert.Equal(HttpStatusCode.NotFound, yabanci.StatusCode);

        // Başka kiracının ÜRÜNÜ de kullanılamaz.
        var yabanciUrun = await sahip.PostAsJsonAsync($"/api/v1/faturalar/{id}/kalemler",
            new { urunId = baska.UrunId, miktar = 1m, birimFiyat = 100m });
        Assert.Equal(HttpStatusCode.NotFound, yabanciUrun.StatusCode);

        Assert.Equal(2, (await Oku(k, id)).Kalemler.Count);
    }
}
