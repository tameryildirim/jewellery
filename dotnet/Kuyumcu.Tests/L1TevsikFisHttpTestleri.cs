using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Yerlesim;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET L-1 — TEVSİK AÇIĞI: ÇOK KALEMLİ SATIŞ TEK FİŞ (GERÇEK HTTP BORU HATTI)
//
// Ekran artık ≥2 kalemli satışı TEK POST /islemler/satis-fisi ile gönderir. Bu
// dosya ucun o gövdeyi (tek bacak NAKIT/KART, çok kalem) üretim boru hattında
// nasıl karşıladığını ölçer: tevsik kapısı FİŞ toplamını görür (Peşin Müşteri
// fail-closed), sınırlar (had − 0,01 · had · had + 0,01), kuruş yuvarlaması
// (kalem başına AwayFromZero), 3+ kalem, KART'ta tek fiş iki kalem ve yeni
// kabul edilen reyonId (tek kalemli uçla aynı sözleşme, fail-closed).
//
// ⚠ Eşik testte SABİT DEĞİL: had sunucudan (/tevsik/gunluk) okunur — mevzuat
//   rakamı yapılandırmadadır (ADR-0005), test onu kopyalamaz.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class L1TevsikFisHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETL1-TEST-" + ad);
    }

    private static async Task<JsonElement> Govde(HttpResponseMessage c) =>
        await c.Content.ReadFromJsonAsync<JsonElement>();

    private async Task<string> PesinId(Kiraci k)
    {
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/cari/sistem-carisi");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        return (await Govde(c)).GetProperty("id").GetString()!;
    }

    private async Task<decimal> Had(Kiraci k)
    {
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/tevsik/gunluk");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var had = (await Govde(c)).GetProperty("had").GetDecimal();
        Assert.True(had > 0m, "had sunucudan okunamadı");
        return had;
    }

    /// <summary>Ekranın L-1 gövdesiyle AYNI şekil: çok kalem, TEK bacak (Islemler.razor FisUcunaGider dalı).</summary>
    private static object Fis(Kiraci k, string? cariId, string arac, decimal[] birimler, decimal toplam,
                              string? reyonId = null, decimal adet = 1m) => new
    {
        cariId,
        kalemler = birimler.Select(b => new { urunId = k.UrunId, adet, birimFiyatTl = b, hediye = false }),
        odemeler = new[] { new { arac, tutarTl = toplam, kasaId = arac == "VERESIYE" ? null : k.KasaId } },
        kurSnapshotId = k.KurId,
        fisIskontoYuzde = 0m,
        reyonId,
    };

    private int SatisSayisi(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Hareketler.AsNoTracking().Count(h => h.TenantId == k.TenantId && h.Tur == "SATIS");
    }

    private List<Hareket> Satislar(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS").ToList();
    }

    private async Task<HttpResponseMessage> Gonder(Kiraci k, object govde, string rol = "TEZGAHTAR") =>
        await f.Ortam.Istemci(k, rol).PostAsJsonAsync("/api/v1/islemler/satis-fisi", govde);

    // ═══ KABUL — T-S1 v3'ün canlı vakası: Peşin + 2 kalem NAKİT ≥ had ═══════

    [Fact]
    public async Task L1H_01_Pesin_iki_kalem_NAKIT_had_ustu_TEK_istek_400_tevsik_cariId_alani_SQL_0_hareket()
    {
        var k = YeniKiraci("pesin-nakit");
        var pesin = await PesinId(k);
        var had = await Had(k);
        // T-S1 v3 vakasının oranı: 28.711,64 + 13.354,25 (her biri tek başına had altı olabilir; toplam üstü).
        var a = Math.Round(had * 0.7m, 2); var b = Math.Round(had * 0.4m, 2);
        Assert.True(a <= had && b <= had && a + b > had);
        var c = await Gonder(k, Fis(k, pesin, "NAKIT", [a, b], a + b));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        var j = await Govde(c);
        Assert.Contains("tevsik", j.GetProperty("message").GetString()!, StringComparison.OrdinalIgnoreCase);
        Assert.True(j.TryGetProperty("alanlar", out var e) && e.TryGetProperty("cariId", out _),
            "ret müşteri alanına bağlanmalı (ekran bandı): " + j);
        Assert.Equal(0, SatisSayisi(k));
    }

    [Fact]
    public async Task L1H_02_Pesin_iki_kalem_KART_had_ustu_de_400_B3_siniri_gevsemez()
    {
        // B-3 (D2·K2): Peşin Müşteri'de FİŞ TOPLAMI hadde ulaşırsa ödeme aracı ne olursa olsun
        // işlem yapılamaz (kimliksiz kart). L-1 bu kuralı GEVŞETMEZ — fiş artık toplamı taşıdığı
        // için kural ilk kez çok kalemli fişte de işler.
        var k = YeniKiraci("pesin-kart");
        var pesin = await PesinId(k);
        var had = await Had(k);
        var c = await Gonder(k, Fis(k, pesin, "KART", [had, 1m], had + 1m));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Equal(0, SatisSayisi(k));
    }

    [Fact]
    public async Task L1H_03_gercek_cari_iki_kalem_KART_had_ustu_200_TEK_fis_iki_kalem_ortak_fisId()
    {
        var k = YeniKiraci("cari-kart");
        var had = await Had(k);
        var a = Math.Round(had * 0.7m, 2); var b = Math.Round(had * 0.5m, 2);
        var c = await Gonder(k, Fis(k, k.CariId, "KART", [a, b], a + b));
        Assert.True(c.IsSuccessStatusCode, $"{(int)c.StatusCode}: {await c.Content.ReadAsStringAsync()}");
        var j = await Govde(c);
        var fisId = j.GetProperty("fisId").GetString();
        Assert.False(string.IsNullOrEmpty(fisId));
        Assert.Equal(2, j.GetProperty("kalemSayisi").GetInt32());
        Assert.Equal(a + b, j.GetProperty("toplamTl").GetDecimal());
        var satirlar = Satislar(k);
        Assert.Equal(2, satirlar.Count);
        Assert.All(satirlar, s => Assert.Equal(fisId, s.FisId));
        Assert.Equal(a + b, satirlar.Sum(s => s.TlTutari));
    }

    [Fact]
    public async Task L1H_04_gercek_cari_iki_kalem_NAKIT_had_ustu_400_islem_tutari_kurali_fis_toplamiyla()
    {
        // §4.1.2 işlem tutarı kuralı: nakit bacağı olan fişte TOPLAM hadde bakılır; kalem başına
        // bölünmüş istekte her kalem had altı kalıp geçiyordu (L-1'in asıl açığı, gerçek caride de).
        var k = YeniKiraci("cari-nakit");
        var had = await Had(k);
        var a = Math.Round(had * 0.6m, 2); var b = Math.Round(had * 0.6m, 2);
        var c = await Gonder(k, Fis(k, k.CariId, "NAKIT", [a, b], a + b));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Contains("nakit sınırı", (await Govde(c)).GetProperty("message").GetString()!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, SatisSayisi(k));
    }

    // ═══ FİNANSAL SINIRLAR — had − 0,01 · had · had + 0,01 (Peşin, 2 kalem NAKİT) ═══

    [Theory]
    [InlineData(-0.01, true)]
    [InlineData(0.00, true)]    // "haddi AŞMASI" — tam had serbest (TevsikKurali)
    [InlineData(0.01, false)]
    public async Task L1H_05_Pesin_iki_kalem_NAKIT_sinir(double sapma, bool gecer)
    {
        var k = YeniKiraci("sinir" + sapma.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var pesin = await PesinId(k);
        var had = await Had(k);
        var toplam = had + (decimal)sapma;
        var a = Math.Round(toplam / 2m, 2); var b = toplam - a;
        var c = await Gonder(k, Fis(k, pesin, "NAKIT", [a, b], toplam));
        var govde = await c.Content.ReadAsStringAsync();
        if (gecer)
        {
            Assert.True(c.IsSuccessStatusCode, $"{toplam}: {(int)c.StatusCode} {govde}");
            var s = Satislar(k);
            Assert.Equal(2, s.Count);
            Assert.Single(s.Select(x => x.FisId).Distinct());
            Assert.Equal(toplam, s.Sum(x => x.TlTutari));
        }
        else
        {
            Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
            Assert.Contains("tevsik", govde, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, SatisSayisi(k));
        }
    }

    [Fact]
    public async Task L1H_06_kurus_yuvarlama_kalem_basina_AwayFromZero_had_ustune_tasirsa_400()
    {
        // İki kalem birim = had/2 + 0,005 → her kalem sunucuda +0,01'e yuvarlanır (AwayFromZero):
        // toplam had + 0,02 → RED. (Ekran bacak tutarını aynı kuralla hesaplamazsa bacak eşitliği
        // de tutmaz — iki yol da fail-closed.)
        var k = YeniKiraci("kurus-ust");
        var pesin = await PesinId(k);
        var had = await Had(k);
        var birim = had / 2m + 0.005m;
        var c = await Gonder(k, Fis(k, pesin, "NAKIT", [birim, birim], had + 0.02m));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Contains("tevsik", await c.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, SatisSayisi(k));
    }

    [Fact]
    public async Task L1H_07_kurus_yuvarlama_asagi_yuvarlanan_kalemler_tam_hadde_200()
    {
        var k = YeniKiraci("kurus-alt");
        var pesin = await PesinId(k);
        var had = await Had(k);
        var birim = had / 2m + 0.004m;   // her kalem had/2'ye yuvarlanır → toplam tam had → serbest
        var c = await Gonder(k, Fis(k, pesin, "NAKIT", [birim, birim], had));
        Assert.True(c.IsSuccessStatusCode, $"{(int)c.StatusCode}: {await c.Content.ReadAsStringAsync()}");
        Assert.Equal(had, Satislar(k).Sum(x => x.TlTutari));
    }

    [Fact]
    public async Task L1H_08_uc_kalem_her_biri_had_altinin_ucte_biri_ustu_toplam_ustu_400_alti_200_tek_fis()
    {
        var k = YeniKiraci("uc-kalem");
        var pesin = await PesinId(k);
        var had = await Had(k);
        var ust = Math.Round(had / 3m, 2) + 0.01m;   // 3 × → had'i aşar
        var r1 = await Gonder(k, Fis(k, pesin, "NAKIT", [ust, ust, ust], ust * 3m));
        Assert.Equal(HttpStatusCode.BadRequest, r1.StatusCode);
        Assert.Equal(0, SatisSayisi(k));

        var alt = Math.Round(had / 4m, 2);
        var r2 = await Gonder(k, Fis(k, pesin, "NAKIT", [alt, alt, alt], alt * 3m));
        Assert.True(r2.IsSuccessStatusCode, $"{(int)r2.StatusCode}: {await r2.Content.ReadAsStringAsync()}");
        var s = Satislar(k);
        Assert.Equal(3, s.Count);
        Assert.Single(s.Select(x => x.FisId).Distinct());
    }

    // ═══ REYON — fiş ucu tek kalemli uçla AYNI reyon sözleşmesini taşır ═══════

    private string ReyonAc(Kiraci k, string ad)
    {
        using var db = f.Vt.OlusturSistem();
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();
        db.Set<Reyon>().Add(new Reyon
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Ad = ad, Tip = ReyonTipi.Vitrin,
            Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    private decimal ReyonAdedi(Kiraci k, string reyonId)
    {
        using var db = f.Vt.OlusturSistem();
        return db.StokYerlesimleri.AsNoTracking()
            .Where(y => y.TenantId == k.TenantId && y.ReyonId == reyonId).Sum(y => (decimal?)y.Adet) ?? 0m;
    }

    [Fact]
    public async Task L1H_09_reyonId_verilen_fis_YALNIZ_o_reyondan_duser_yetersizse_400_ve_0_hareket()
    {
        var k = YeniKiraci("reyon");
        var bos = ReyonAc(k, "A-BOS");
        var dolu = ReyonAc(k, "B-DOLU");
        using (var db = f.Vt.Olustur(k))
        {
            await new YerlesimServisi(db).Yerlestir(k.Islem, k.SubeId, k.UrunId, dolu, 5m, DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        // Boş reyon: şube stoğu yeterli olsa da başka yerden ALINMAZ (fail-closed) — alan yok sayılsaydı 200 olurdu.
        var red = await Gonder(k, Fis(k, k.CariId, "KART", [100m, 100m], 200m, reyonId: bos));
        Assert.Equal(HttpStatusCode.BadRequest, red.StatusCode);
        Assert.Equal(0, SatisSayisi(k));

        var ok = await Gonder(k, Fis(k, k.CariId, "KART", [100m, 100m], 200m, reyonId: dolu));
        Assert.True(ok.IsSuccessStatusCode, $"{(int)ok.StatusCode}: {await ok.Content.ReadAsStringAsync()}");
        Assert.Equal(3m, ReyonAdedi(k, dolu));   // iki kalem × 1 adet yalnız B-DOLU'dan
        Assert.Equal(0m, ReyonAdedi(k, bos));
    }
    [Fact]
    public async Task L1H_10_reyonsuz_fiste_atanmamis_yetmezse_yerlesimUyarisi_YANITTA_doner_UX_S27()
    {
        // Tek kalemli uçla AYNI sözleşme: reyon seçilmezse önce ATANMAMIŞ tükenir, yetmezse reyondan
        // düşülür ve yanıt bunu söyler (sessizce yutulmaz). Eskiden fiş ucu uyarıyı hiç döndürmüyordu.
        var k = YeniKiraci("yerlesim");
        var vitrin = ReyonAc(k, "A-VITRIN");
        using (var db = f.Vt.Olustur(k))
        {
            await new YerlesimServisi(db).Yerlestir(k.Islem, k.SubeId, k.UrunId, vitrin, 99m, DateTime.UtcNow);   // atanmamış 1
            await db.SaveChangesAsync();
        }
        var c = await Gonder(k, Fis(k, k.CariId, "KART", [100m, 100m, 100m], 300m));
        Assert.True(c.IsSuccessStatusCode, $"{(int)c.StatusCode}: {await c.Content.ReadAsStringAsync()}");
        var j = await Govde(c);
        Assert.True(j.TryGetProperty("yerlesimUyarisi", out var u) && u.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(u.GetString()), "yerlesimUyarisi yanıtta yok: " + j);
        Assert.Equal(97m, ReyonAdedi(k, vitrin));   // 2. ve 3. kalem vitrinden (uyarı sonrası da HER kalem düşer)

        // Reyon verildiğinde (yetiyor) uyarı YOK.
        var c2 = await Gonder(k, Fis(k, k.CariId, "KART", [100m], 100m, reyonId: vitrin));
        Assert.True(c2.IsSuccessStatusCode);
        Assert.Equal(JsonValueKind.Null, (await Govde(c2)).GetProperty("yerlesimUyarisi").ValueKind);
    }
}
