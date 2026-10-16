using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using IslemCari = Kuyumcu.Contracts.Sayfalar.Islemler.CariVeri;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-3 — PEŞİN MÜŞTERİ EŞİK REDDİ: GERÇEK HTTP BORU HATTI
//
// Ekran bandı ("gerçek müşteri seçin") eşik SAYISINI bilmez; sunucunun alan
// işaretli reddiyle açılır: 400 gövdesinde `alanlar.cariId`. Bu dosya:
//   • tek kalem satış ve karma fiş yollarında Peşin eşik reddinin
//     alanlar.cariId taşıdığını (mesajla aynı) ölçer;
//   • ALAN İŞARETSİZ reddin (fark ≠ 0) gövdesinin DEĞİŞMEDİĞİNİ ölçer —
//     bant yanlış sebeple açılmaz;
//   • GET /cari yanıtının istemci tipine (Islemler CariVeri) bakiyeyle
//     ayrıştığını ve rozet metninin o bakiyeden üretildiğini ölçer.
// Gövdeler AYRIŞTIRILIR (yalnız durum koduna bakılmaz).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class S23PesinBandiHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETS23-TEST-" + ad);
    }

    private static async Task<JsonElement> Govde(HttpResponseMessage c) =>
        await c.Content.ReadFromJsonAsync<JsonElement>();

    private async Task<string> PesinId(Kiraci k)
    {
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/cari/sistem-carisi");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        return (await Govde(c)).GetProperty("id").GetString()!;
    }

    private static void AlanliRed(JsonElement g)
    {
        var mesaj = g.GetProperty("message").GetString()!;
        Assert.Contains("gerçek müşteri", mesaj);
        var alan = g.GetProperty("alanlar").GetProperty("cariId");
        Assert.Equal(mesaj, alan[0].GetString());
        Assert.Equal("Bad Request", g.GetProperty("error").GetString());   // Nest sözleşmesi aynen
        Assert.Equal(400, g.GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task S23H_01_karma_fis_Pesin_MASAK_esigi_ustu_400_alanlar_cariId()
    {
        var k = YeniKiraci("masak");
        var id = await PesinId(k);
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = id,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 200_000m } },
            odemeler = new[] { new { arac = "HAVALE", tutarTl = 200_000m, kasaId = k.KasaId } },
            kurSnapshotId = k.KurId,
        });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        AlanliRed(await Govde(c));
    }

    [Fact]
    public async Task S23H_02_tek_kalem_satis_Pesin_tevsik_esigi_ustu_400_alanlar_cariId()
    {
        var k = YeniKiraci("tevsik");
        var id = await PesinId(k);
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsJsonAsync("/api/v1/islemler/satis", new
        {
            cariId = id, urunId = k.UrunId, adet = 1m, birimFiyatTl = 35_000m,
            odemeTuru = "HAVALE", kasaId = k.KasaId, kurSnapshotId = k.KurId,
        });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        AlanliRed(await Govde(c));

        using var kontrol = f.Vt.OlusturSistem();
        Assert.Equal(0, kontrol.Hareketler.Count(h => h.TenantId == k.TenantId && h.CariId == id));
    }

    [Fact]
    public async Task S23H_03_alan_isaretsiz_red_govdesi_DEGISMEZ_alanlar_YOK()
    {
        var k = YeniKiraci("fark");
        var id = await PesinId(k);
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = id,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 10_000m } },
            odemeler = new[] { new { arac = "NAKIT", tutarTl = 6_000m, kasaId = k.KasaId } },
            kurSnapshotId = k.KurId,
        });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        var g = await Govde(c);
        Assert.False(g.TryGetProperty("alanlar", out _), g.ToString());
        Assert.Equal("Bad Request", g.GetProperty("error").GetString());
    }

    [Fact]
    public async Task S23H_04_gercek_cari_esik_ustu_satista_Pesin_reddi_YOK()
    {
        // Kural yalnız Peşin kartı daraltır: gerçek caride aynı tutar alan
        // işaretli Peşin reddi ÜRETMEZ (bant gerçek caride açılmaz).
        var k = YeniKiraci("gercek");
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsJsonAsync("/api/v1/islemler/satis", new
        {
            cariId = k.CariId, urunId = k.UrunId, adet = 1m, birimFiyatTl = 35_000m,
            odemeTuru = "HAVALE", kasaId = k.KasaId, kurSnapshotId = k.KurId,
        });
        var metin = await c.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Peşin Müşteri ile bu tutarda", metin);
        if (!c.IsSuccessStatusCode)
            Assert.False((await Govde(c)).TryGetProperty("alanlar", out var a) && a.TryGetProperty("cariId", out _), metin);
    }

    [Fact]
    public async Task S23H_06_TevsikKapisi_Pesin_reddi_dogrudan_cagrida_da_cariId_isaretli()
    {
        // Satış yolunda MASAK kapısının tevsik dalı önce patlar; TevsikKapisi'nin
        // kendi Peşin dalı (nakit payı altında, fiş toplamı üstünde) doğrudan ölçülür.
        var k = YeniKiraci("tevsikkapi");
        var id = await PesinId(k);
        using var db = f.Vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() =>
            Kuyumcu.Api.Modules.Tevsik.TevsikKapisi.Varsayilan().Dogrula(db, k.TenantId, id, 10_000m, DateTime.UtcNow,
                islemToplamTutari: 40_000m));
        Assert.Contains("gerçek müşteri", hata.Message);
        Assert.Equal("cariId", Kuyumcu.Api.Common.AlanIsareti.Alan(hata));

        // Gerçek caride aynı çağrı Peşin reddi ÜRETMEZ.
        var gercek = await Kuyumcu.Api.Modules.Tevsik.TevsikKapisi.Varsayilan().Dogrula(db, k.TenantId, k.CariId, 10_000m,
            DateTime.UtcNow, islemToplamTutari: 10_000m);
        Assert.NotNull(gercek);
    }

    [Fact]
    public async Task S23H_05_cari_listesi_istemci_tipine_bakiyeyle_ayrisir_rozet_ayni_bakiyeden()
    {
        var k = YeniKiraci("rozet");
        using (var db = f.Vt.OlusturSistem())
        {
            var b = db.CariBakiyeler.Single(x => x.CariId == k.CariId && x.TenantId == k.TenantId);
            b.TlBakiye = 38_900m;
            b.HasGramBakiye = 5.66m;
            db.SaveChanges();
        }

        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/cari?limit=200&aktif=true");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var liste = await c.Content.ReadFromJsonAsync<List<IslemCari>>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var cari = liste!.Single(x => x.Id == k.CariId);
        Assert.Equal(38_900m, cari.TlBakiye);
        Assert.Equal(5.66m, cari.HasGramBakiye);

        var rozet = FisBakiyeRozeti.Olustur(cari, FisMusteriSozlugu.Tr)!.Value;
        Assert.Equal($"{FisMusteriSozlugu.Tr.Borcu}: 5,660 g has · {Kuyumcu.Web.Servisler.Bicim.Tl(38_900m)}", rozet.Metin);
    }
}
