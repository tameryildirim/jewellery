using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Modules.Rapor;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET G-5 (B-4 devri) — SATIŞ ÖDEME KIRILIMINDA "ESKİ ALTIN (MAHSUP)" SATIRI
// GERÇEK HTTP BORU HATTI: fiş POST /islemler/satis-fisi ile ekrandaki gibi
// yazılır, kırılım GET /raporlar/odeme-araci ile okunur ve gövde AYRIŞTIRILIR.
// Toplamlar sunucuda hesaplanır; test sayıyı DB'deki SATIS satırlarıyla sınar.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class G5SatisKirilimiHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return IslemBantFikstur.ReferansKur(db, TestKurulum.KiraciKur(db, "PAKETG5-TEST-" + ad));
    }

    private static async Task<JsonElement> Govde(HttpResponseMessage c) =>
        await c.Content.ReadFromJsonAsync<JsonElement>();

    /// <summary>B-4 canlı adım 18'in birebir şekli: 20.000 = NAKIT 10.840 + ALTIN 9.160 (K22 5 g × 2.000).</summary>
    private async Task AltinliFis(Kiraci k)
    {
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1, birimFiyatTl = 20_000 } },
            odemeler = new object[]
            {
                new { arac = "NAKIT", tutarTl = 10_840, kasaId = k.KasaId },
                new
                {
                    arac = "ALTIN", tutarTl = 9_160, ayarId = "K22", gramAgirlik = 5, birimFiyatTlHasGram = 2_000,
                    satici = new { saticiTipi = "NIHAI_TUKETICI", adSoyad = "Ali Veli", tckn = "10000000146", adres = "Ulus" },
                },
            },
        });
        Assert.True(c.IsSuccessStatusCode, $"{(int)c.StatusCode} {await c.Content.ReadAsStringAsync()}");
    }

    private async Task AltinsizFis(Kiraci k, decimal tutar = 20_000m)
    {
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1, birimFiyatTl = tutar } },
            odemeler = new object[] { new { arac = "NAKIT", tutarTl = tutar, kasaId = k.KasaId } },
        });
        Assert.True(c.IsSuccessStatusCode, $"{(int)c.StatusCode} {await c.Content.ReadAsStringAsync()}");
    }

    private async Task<JsonElement> Kirilim(Kiraci k)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/raporlar/odeme-araci");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        return await Govde(c);
    }

    private static JsonElement? Satir(JsonElement sk, string arac) =>
        sk.GetProperty("satirlar").EnumerateArray()
          .Where(s => s.GetProperty("arac").GetString() == arac)
          .Select(s => (JsonElement?)s).SingleOrDefault();

    /// <summary>G5-1 — ALTIN bacaklı fiş sonrası kırılımda ALTIN satırı tutarıyla VAR, kasaya girmez.</summary>
    [Fact]
    public async Task G5_Kirilimda_eski_altin_mahsup_satiri_gorunur()
    {
        var k = YeniKiraci("altin satiri");
        await AltinliFis(k);

        var sk = (await Kirilim(k)).GetProperty("satisKirilimi");
        Assert.Equal(SatisOdemeKirilimi.Kaynak, sk.GetProperty("kaynak").GetString());
        var altin = Satir(sk, "ALTIN");
        Assert.NotNull(altin);
        Assert.Equal(9_160m, altin!.Value.GetProperty("tutarTl").GetDecimal());
        Assert.Equal(1, altin.Value.GetProperty("fisAdet").GetInt32());
        Assert.False(altin.Value.GetProperty("kasayaGirer").GetBoolean());
        Assert.Equal(9_160m, sk.GetProperty("mahsupAltinTl").GetDecimal());
    }

    /// <summary>
    /// G5-2 — nakit satırı ve "kasaya giren" toplamı ALTIN bacağı kadar ŞİŞMEZ.
    /// Kasa defterinin NAKIT girişi de 10.840'tır (kaynaklar birbirini tutar).
    /// </summary>
    [Fact]
    public async Task G5_Nakit_toplami_altin_bacagi_kadar_SISMEZ()
    {
        var k = YeniKiraci("nakit sismez");
        await AltinliFis(k);

        var j = await Kirilim(k);
        var sk = j.GetProperty("satisKirilimi");
        Assert.Equal(10_840m, Satir(sk, "NAKIT")!.Value.GetProperty("tutarTl").GetDecimal());
        Assert.Equal(10_840m, sk.GetProperty("kasayaGirenTl").GetDecimal());
        Assert.Null(Satir(sk, "KART"));
        Assert.Null(Satir(sk, "HAVALE"));

        var kasaNakit = j.GetProperty("araclar").EnumerateArray()
            .Single(a => a.GetProperty("arac").GetString() == "NAKIT");
        Assert.Equal(10_840m, kasaNakit.GetProperty("girisTlToplam").GetDecimal());
        // Kasa kırılımında ALTIN yoktur: eski altın kasaya hiç yazılmaz (B-4).
        Assert.DoesNotContain(j.GetProperty("araclar").EnumerateArray(),
            a => a.GetProperty("arac").GetString() == "ALTIN");
    }

    /// <summary>G5-3 — ALTIN bacaksız dönemde ALTIN satırı ÇİZİLMEZ; boş (0 TL) satır da yok.</summary>
    [Fact]
    public async Task G5_Altinsiz_gunde_satir_cizilmez()
    {
        var k = YeniKiraci("altinsiz");
        await AltinsizFis(k);

        var sk = (await Kirilim(k)).GetProperty("satisKirilimi");
        Assert.Null(Satir(sk, "ALTIN"));
        Assert.Equal(0m, sk.GetProperty("mahsupAltinTl").GetDecimal());
        var satirlar = sk.GetProperty("satirlar").EnumerateArray().ToList();
        Assert.Single(satirlar);
        Assert.All(satirlar, s => Assert.True(s.GetProperty("tutarTl").GetDecimal() > 0m));
        Assert.Equal(20_000m, sk.GetProperty("kasayaGirenTl").GetDecimal());
    }

    /// <summary>
    /// G5-4 — kırılım toplamı = brüt satış toplamı (fark 0). Brüt, DB'deki SATIS
    /// satırlarının TL toplamıyla da AYNIDIR (SATIS brüt yazılı, eski altın düşülmez).
    /// </summary>
    [Fact]
    public async Task G5_Kirilim_toplami_brut_satisa_esittir()
    {
        var k = YeniKiraci("toplam brut");
        await AltinliFis(k);
        // Tevsik haddi (aynı gün aynı kişi nakit ≤ 30.000) aşılmasın: 10.840 + 15.000.
        await AltinsizFis(k, 15_000m);

        var sk = (await Kirilim(k)).GetProperty("satisKirilimi");
        Assert.Equal(35_000m, sk.GetProperty("brutSatisTl").GetDecimal());
        Assert.Equal(35_000m, sk.GetProperty("kirilimToplamTl").GetDecimal());
        Assert.Equal(0m, sk.GetProperty("farkTl").GetDecimal());
        Assert.Equal(2, sk.GetProperty("fisAdet").GetInt32());
        Assert.Equal(0, sk.GetProperty("kalemsizFisAdet").GetInt32());
        var satirToplami = sk.GetProperty("satirlar").EnumerateArray().Sum(s => s.GetProperty("tutarTl").GetDecimal());
        Assert.Equal(35_000m, satirToplami);
        Assert.Equal(25_840m, sk.GetProperty("kasayaGirenTl").GetDecimal());

        using var db = f.Vt.OlusturSistem();
        var dbBrut = db.Hareketler.Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS")
            .Select(h => h.TlTutari).ToList().Sum();
        Assert.Equal(dbBrut, sk.GetProperty("brutSatisTl").GetDecimal());
    }
}

/// <summary>G-5 saf kırılım: kalemsiz fiş sıfır sayılmaz, BILINMIYOR satırında beyan edilir.</summary>
public sealed class G5SatisKirilimiSafTestleri
{
    [Fact]
    public void G5_Kalemsiz_fis_bilinmiyor_satirinda_beyan_edilir_fark_sifir()
    {
        var k = SatisOdemeKirilimi.Olustur(
            [("f1", 1_000m), ("f2", 500m)],
            [("f1", "NAKIT", 400m), ("f1", "ALTIN", 600m)]);
        Assert.Equal(["ALTIN", "BILINMIYOR", "NAKIT"], k.Satirlar!.Select(s => s.Arac).OrderBy(x => x).ToList());
        Assert.Equal(500m, k.Satirlar!.Single(s => s.Arac == "BILINMIYOR").TutarTl);
        Assert.False(k.Satirlar!.Single(s => s.Arac == "ALTIN").KasayaGirer);
        Assert.Equal(1_500m, k.KirilimToplamTl);
        Assert.Equal(0m, k.FarkTl);
        Assert.Equal(400m, k.KasayaGirenTl);
        Assert.Equal(600m, k.MahsupAltinTl);
        Assert.Equal(1, k.KalemsizFisAdet);
    }

    [Fact]
    public void G5_Kalem_toplami_brutu_tutmazsa_fark_uydurulmaz()
    {
        var k = SatisOdemeKirilimi.Olustur([("f1", 1_000m)], [("f1", "NAKIT", 900m)]);
        Assert.Equal(100m, k.FarkTl);
    }
}
