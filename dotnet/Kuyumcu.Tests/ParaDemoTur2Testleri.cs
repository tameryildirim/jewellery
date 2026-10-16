using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO tur 2 — hasım bulgularının kapanışı (HTTP + bağımsız DB okuması)
//
//   KRİTİK: komisyonlu elle KART girişi hareketi BRÜT, bakiyeyi NET yazıyordu →
//           nakit defteri komisyon kadar kayıyordu. Artık hareket = NET; Σ hareket
//           = bakiye değişmezi storno sonrası da tutar.
//   ORTA  : kasadan virman hep NAKIT damgalıydı → kart/havale payı virmanla bankaya
//           aktarılınca nakit defteri düşüyordu. Artık virman aracı seçilir
//           (NAKIT varsayılan · KART · HAVALE) ve pay sınırı sunucuda uygulanır.
// Beklenen değerler SABİT ve elle hesaplanmıştır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class ParaDemoTur2Testleri(HttpFikstur f)
{
    private const string Gerekce = "Komisyonlu kart girişi yanlış yazıldı, geri alınıyor (test).";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static async Task Satis(HttpClient c, Kiraci k, string odeme, decimal tutar)
    {
        var r = await c.PostAsJsonAsync("/api/v1/islemler/satis", new
        {
            cariId = k.CariId, urunId = k.UrunId, adet = 1, birimFiyatTl = tutar,
            odemeTuru = odeme, kasaId = k.KasaId, kurSnapshotId = k.KurId,
        });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
    }

    private async Task<string> HesapAc(Kiraci k)
    {
        var r = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/bankalar", new
        {
            bankaAdi = "Test Bankası", hesapAdi = "PD tur2 hesap",
            iban = TestIban.Uret(),
            paraBirimi = "TRY",
        });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Json()).GetProperty("id").GetString()!;
    }

    private static async Task<(decimal Nakit, decimal Kart, decimal Havale, decimal Toplam)> Onizleme(HttpClient c, Kiraci k)
    {
        var r = await c.GetAsync($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi/onizleme");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Json();
        return (j.GetProperty("nakitDefter").GetDecimal(), j.GetProperty("kartToplam").GetDecimal(),
                j.GetProperty("havaleToplam").GetDecimal(), j.GetProperty("toplamBakiye").GetDecimal());
    }

    /// <summary>Bağımsız okuma: (kasa bakiyesi, Σ kasa hareketi).</summary>
    private async Task<(decimal Bakiye, decimal HareketToplami)> KasaDefteri(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var bakiye = (await db.Kasalar.AsNoTracking().SingleAsync(x => x.Id == k.KasaId)).Bakiye;
        var toplam = await db.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.KasaId == k.KasaId).SumAsync(h => h.Tutar);
        return (bakiye, toplam);
    }

    // ── KRİTİK: komisyonlu elle KART girişi ─────────────────────────────────

    [Fact]
    public async Task PD7_komisyonlu_elle_KART_girisi_hareketi_NET_yazar_nakit_ve_sayim_degismez()
    {
        var k = YeniKiraci("PD7 komisyon");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var r = await c.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/hareket", new
        {
            yon = "GIRIS", tutar = 1_000m, odemeTuru = "KART", komisyonOran = 0.02m, aciklama = "komisyonlu kart",
        });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        var (bakiye, hareket) = await KasaDefteri(k);
        Assert.Equal(TestKurulum.KasaAcilis + 980m, bakiye);   // 1.000 × 0,98
        Assert.Equal(980m, hareket);                            // Σ hareket = bakiye − açılış devri (hareketsiz)
        var o = await Onizleme(c, k);
        Assert.Equal(980m, o.Kart);
        Assert.Equal(TestKurulum.KasaAcilis, o.Nakit);          // çekmeceye nakit girmedi

        var kapat = await c.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            new { sayilanNakit = TestKurulum.KasaAcilis });
        Assert.Equal(HttpStatusCode.OK, kapat.StatusCode);
        Assert.Equal(0m, (await kapat.Json()).GetProperty("fark").GetDecimal());
    }

    [Fact]
    public async Task PD7_komisyonlu_elle_hareketin_STORNOSU_bakiyeyi_ve_hareket_toplamini_sifirlar()
    {
        var k = YeniKiraci("PD7 komisyon storno");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var r = await c.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/hareket", new
        {
            yon = "GIRIS", tutar = 1_000m, odemeTuru = "KART", komisyonOran = 0.025m, aciklama = "komisyonlu kart",
        });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        string hareketId;
        using (var db = f.Vt.OlusturSistem())
            hareketId = (await db.KasaHareketleri.AsNoTracking()
                .SingleAsync(h => h.TenantId == k.TenantId && h.KasaId == k.KasaId)).Id;

        var s = await c.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/hareket/{hareketId}/storno", new { gerekce = Gerekce });
        Assert.True(s.IsSuccessStatusCode, await s.Content.ReadAsStringAsync());

        var (bakiye, hareket) = await KasaDefteri(k);
        Assert.Equal(TestKurulum.KasaAcilis, bakiye);   // eskiden 250.000 + 975 − 1.000 = 249.975 kalırdı
        Assert.Equal(0m, hareket);
        Assert.Equal(TestKurulum.KasaAcilis, (await Onizleme(c, k)).Nakit);
    }

    // ── ORTA: kasadan virmanda aktarılan aracın damgası ─────────────────────

    [Fact]
    public async Task PD6_kart_payi_bankaya_virmanla_aktarilinca_nakit_defteri_DEGISMEZ_sayim_fark_sifir()
    {
        var k = YeniKiraci("PD6 virman kart");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Satis(c, k, "NAKIT", 1_000m);
        await Satis(c, k, "KART", 500m);
        await Satis(c, k, "HAVALE", 300m);
        var hesap = await HesapAc(k);

        var v = await c.PostAsJsonAsync("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "BANKA", hedefId = hesap,
            tutar = 500m, kasaAraci = "KART",
        });
        Assert.Equal(HttpStatusCode.OK, v.StatusCode);
        var v2 = await c.PostAsJsonAsync("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "BANKA", hedefId = hesap,
            tutar = 300m, kasaAraci = "HAVALE",
        });
        Assert.Equal(HttpStatusCode.OK, v2.StatusCode);

        var o = await Onizleme(c, k);
        Assert.Equal(0m, o.Kart);
        Assert.Equal(0m, o.Havale);
        Assert.Equal(TestKurulum.KasaAcilis + 1_000m, o.Nakit);
        Assert.Equal(TestKurulum.KasaAcilis + 1_000m, o.Toplam);

        using (var db = f.Vt.OlusturSistem())
        {
            var bacaklar = await db.KasaHareketleri.AsNoTracking()
                .Where(h => h.TenantId == k.TenantId && h.KasaId == k.KasaId && h.KarsiBacakId != null)
                .Select(h => new { h.OdemeTuru, h.Tutar }).ToListAsync();
            Assert.Contains(bacaklar, b => b.OdemeTuru == "KART" && b.Tutar == -500m);
            Assert.Contains(bacaklar, b => b.OdemeTuru == "HAVALE" && b.Tutar == -300m);
        }

        var kapat = await c.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            new { sayilanNakit = TestKurulum.KasaAcilis + 1_000m });
        Assert.Equal(HttpStatusCode.OK, kapat.StatusCode);
        Assert.Equal(0m, (await kapat.Json()).GetProperty("fark").GetDecimal());
    }

    [Fact]
    public async Task PD6_kart_payini_ASAN_virman_400_hicbir_sey_yazilmaz()
    {
        var k = YeniKiraci("PD6 virman aşım");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Satis(c, k, "KART", 500m);
        var hesap = await HesapAc(k);
        var once = await KasaDefteri(k);

        var v = await c.PostAsJsonAsync("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "BANKA", hedefId = hesap,
            tutar = 500.01m, kasaAraci = "KART",
        });
        Assert.Equal(HttpStatusCode.BadRequest, v.StatusCode);
        // PARA-DEMO-2 tur 4 (UX-S32): mesaj tutarı ekran biçiminde ("500,00 ₺"), "500.00 TRY" kalıbı yok.
        Assert.Contains("kart tahsilatı 500,00\u00a0\u20ba", (await v.Json()).GetProperty("message").GetString());
        Assert.Equal(once, await KasaDefteri(k));

        // Sınır: tam pay geçer.
        var tam = await c.PostAsJsonAsync("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "BANKA", hedefId = hesap,
            tutar = 500m, kasaAraci = "KART",
        });
        Assert.Equal(HttpStatusCode.OK, tam.StatusCode);
    }

    [Fact]
    public async Task PD6_bankadan_cikan_virmanda_KART_araci_400()
    {
        var k = YeniKiraci("PD6 banka kaynak");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var hesap = await HesapAc(k);
        var v = await c.PostAsJsonAsync("/api/v1/virman", new
        {
            kaynakTur = "BANKA", kaynakId = hesap, hedefTur = "KASA", hedefId = k.KasaId,
            tutar = 10m, kasaAraci = "KART",
        });
        Assert.Equal(HttpStatusCode.BadRequest, v.StatusCode);
        Assert.Contains("yalnız KASADAN", (await v.Json()).GetProperty("message").GetString());
    }

    [Fact]
    public async Task PD6_arac_verilmezse_virman_NAKIT_damgasi_bugunku_davranis()
    {
        var k = YeniKiraci("PD6 virman nakit");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Satis(c, k, "KART", 500m);
        var hesap = await HesapAc(k);
        var v = await c.PostAsJsonAsync("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "BANKA", hedefId = hesap, tutar = 100m,
        });
        Assert.Equal(HttpStatusCode.OK, v.StatusCode);
        var o = await Onizleme(c, k);
        Assert.Equal(500m, o.Kart);                                // kart payı yerinde
        Assert.Equal(TestKurulum.KasaAcilis - 100m, o.Nakit);       // çekmeceden nakit çıktı
    }

    [Fact]
    public async Task PD6_kart_virmaninin_STORNOSU_kart_payini_geri_getirir()
    {
        var k = YeniKiraci("PD6 virman storno");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Satis(c, k, "KART", 500m);
        var hesap = await HesapAc(k);
        var v = await c.PostAsJsonAsync("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "BANKA", hedefId = hesap,
            tutar = 200m, kasaAraci = "KART",
        });
        Assert.Equal(HttpStatusCode.OK, v.StatusCode);
        var cikis = (await v.Json()).GetProperty("cikisBacakId").GetString()!;
        Assert.Equal(300m, (await Onizleme(c, k)).Kart);

        var s = await c.PostAsJsonAsync($"/api/v1/virman/{cikis}/storno", new { gerekce = Gerekce });
        Assert.True(s.IsSuccessStatusCode, await s.Content.ReadAsStringAsync());
        var o = await Onizleme(c, k);
        Assert.Equal(500m, o.Kart);
        Assert.Equal(TestKurulum.KasaAcilis, o.Nakit);
    }
}
