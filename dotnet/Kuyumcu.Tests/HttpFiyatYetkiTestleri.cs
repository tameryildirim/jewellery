using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// FİYAT / SAYIM — YETKİ KAPISI (paket UX-4a madde 14, Mehmet'in koşulu).
//
// NEDEN SERVİS TESTİ YETMEZ: [ModulYetkisi] ve [RolGerekli] DENETLEYİCİ
// nitelikleridir; servis testi ikisini de görmez. Önceki hâlde FiyatModulu'nde
// yalnız [Authorize] vardı → "rapor" yetkisi verilen TEZGÂHTAR uçtan marjı
// değiştirebiliyordu (menüde gizli olması kapı değildir).
//
// Sözleşme:
//   • Okuma  → "urun" modül yetkisi (menüdeki blokla AYNI anahtar)
//   • Yazma  → ek olarak SAHIP/MUDUR
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class HttpFiyatYetkiTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private async Task<string> ListeAc(Kiraci k, string ad = "Perakende", decimal marj = 5m)
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/fiyat/listeler",
            new { ad, tur = "PERAKENDE", hasMarjYuzde = marj, iscilikCarpanYuzde = 100m, varsayilan = true });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        return (await cevap.Json()).GetProperty("id").GetString()!;
    }

    private static object TamGovde(string ad, decimal marj, bool varsayilan) =>
        new { ad, tur = "PERAKENDE", hasMarjYuzde = marj, iscilikCarpanYuzde = 100m, varsayilan };

    /// <summary>M14-1 — fiyat YAZMA rol kapısı: tezgâhtar marjı değiştiremez.</summary>
    [Fact]
    public async Task TEZGAHTAR_fiyat_listesini_GUNCELLEYEMEZ_403()
    {
        var k = YeniKiraci("UX4a fiyat tezgâhtar");
        var listeId = await ListeAc(k);

        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR")
            .PatchAsJsonAsync($"/api/v1/fiyat/listeler/{listeId}", TamGovde("Ele geçirildi", 99m, true));

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);

        // Kapı GERÇEKTEN kapandı: marj değişmedi.
        using var db = f.Vt.OlusturSistem();
        var liste = await db.FiyatListeleri.AsNoTracking().SingleAsync(x => x.Id == listeId);
        Assert.Equal(5m, liste.HasMarjYuzde);
        Assert.Equal("Perakende", liste.Ad);
    }

    /// <summary>Tezgâhtar kampanya da kuramaz/değiştiremez.</summary>
    [Fact]
    public async Task TEZGAHTAR_kampanya_OLUSTURAMAZ_403()
    {
        var k = YeniKiraci("UX4a kampanya tezgâhtar");
        var bugun = DateOnly.FromDateTime(DateTime.Today);
        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson("/api/v1/fiyat/kampanyalar", new
        {
            ad = "İzinsiz", baslangic = bugun.ToString("yyyy-MM-dd"), bitis = bugun.AddDays(3).ToString("yyyy-MM-dd"),
            hedefTur = "TUMU", indirimTuru = "TUTAR_YUZDE", deger = 50m,
        });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.Kampanyalar.AsNoTracking().AnyAsync(x => x.TenantId == k.TenantId));
    }

    /// <summary>Okuma kapısı tezgâhtara AÇIK: POS fiyat teklifi alabilmelidir.</summary>
    [Fact]
    public async Task TEZGAHTAR_fiyat_listelerini_OKUYABILIR()
    {
        var k = YeniKiraci("UX4a fiyat okuma");
        await ListeAc(k);
        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/fiyat/listeler");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
    }

    /// <summary>MUDUR yazabilir — kapı rol kümesini doğru tanıyor.</summary>
    [Fact]
    public async Task MUDUR_fiyat_listesini_GUNCELLEYEBILIR()
    {
        var k = YeniKiraci("UX4a fiyat müdür");
        var listeId = await ListeAc(k);

        var cevap = await f.Ortam.Istemci(k, "MUDUR")
            .PatchAsJsonAsync($"/api/v1/fiyat/listeler/{listeId}", TamGovde("Perakende", 7.5m, true));

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(7.5m, (await db.FiyatListeleri.AsNoTracking().SingleAsync(x => x.Id == listeId)).HasMarjYuzde);
    }

    /// <summary>
    /// M14-2 — fiyat/sayım modül yetkisi kaldırılan MUDUR ekranı GÖREMEZ.
    /// <para>
    /// ⚠ P-09 (Y-39) SÖZLEŞME DEĞİŞİKLİĞİ: fiyat listeleri ve sayım ekranları
    /// "urun" anahtarını ödünç alıyordu; artık KENDİ anahtarları vardır
    /// ("fiyat-listesi", "sayim"). Varsayılan matris bugünkü erişimi korur —
    /// değişen, bu iki ekranın ürün kartlarından AYRI kapatılabilmesidir.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Fiyat_yetkisi_OLMAYAN_MUDUR_fiyat_listelerini_goremez_403()
    {
        var k = YeniKiraci("UX4a fiyat modül yetkisi");
        await ListeAc(k);

        // SAHIP matrisi yeniden yazar: MUDUR'dan "fiyat-listesi"/"sayim"
        // çıkar, "rapor" kalır; MUHASEBE ikisini de korur.
        var kaydet = await f.Ortam.Istemci(k, "SAHIP").PutAsJsonAsync("/api/v1/yetkiler", new
        {
            matris = new Dictionary<string, string[]>
            {
                ["MUDUR"] = ["cari", "islem", "kasa", "banka", "fatura", "rapor", "kullanici"],
                ["MUHASEBE"] = ["cari", "urun", "islem", "kasa", "banka", "fatura", "rapor", "fiyat-listesi", "sayim"],
                ["TEZGAHTAR"] = ["cari", "urun", "islem", "kasa", "banka", "fatura"],
            },
        });
        Assert.Equal(HttpStatusCode.OK, kaydet.StatusCode);

        var fiyat = await f.Ortam.Istemci(k, "MUDUR").GetAsync("/api/v1/fiyat/listeler");
        Assert.Equal(HttpStatusCode.Forbidden, fiyat.StatusCode);

        // Aynı kapı SAYIM için de geçerlidir (menü ile sunucu aynı anahtar).
        var sayim = await f.Ortam.Istemci(k, "MUDUR").GetAsync("/api/v1/sayim");
        Assert.Equal(HttpStatusCode.Forbidden, sayim.StatusCode);

        // "fiyat-listesi" duran MUHASEBE hâlâ görebiliyor — kapı doğru anahtarda.
        var muhasebe = await f.Ortam.Istemci(k, "MUHASEBE").GetAsync("/api/v1/fiyat/listeler");
        Assert.Equal(HttpStatusCode.OK, muhasebe.StatusCode);
    }

    /// <summary>
    /// Madde 1 canlı sözleşmesi: varsayılan listeye gövdede varsayilan=false gelen
    /// PATCH sonrası liste HÂLÂ varsayılandır ve POS teklifi üretilmeye devam eder.
    /// </summary>
    [Fact]
    public async Task Varsayilan_false_PATCH_sonrasi_teklif_URETILMEYE_devam_eder()
    {
        var k = YeniKiraci("UX4a teklif sürekliliği");
        var listeId = await ListeAc(k, "Perakende", 5m);
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var once = await istemci.GetAsync($"/api/v1/fiyat/teklif?urunId={k.UrunId}&kurSnapshotId={k.KurId}");
        Assert.True((await once.Json()).GetProperty("teklifVar").GetBoolean());

        var patch = await istemci.PatchAsJsonAsync($"/api/v1/fiyat/listeler/{listeId}",
            TamGovde("Perakende düzeltildi", 6m, varsayilan: false));
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        Assert.True((await patch.Json()).GetProperty("varsayilan").GetBoolean());

        var sonra = await istemci.GetAsync($"/api/v1/fiyat/teklif?urunId={k.UrunId}&kurSnapshotId={k.KurId}");
        var json = await sonra.Json();
        Assert.True(json.GetProperty("teklifVar").GetBoolean());
        Assert.True(json.GetProperty("listeFiyati").GetDecimal() > 0m);
    }

    /// <summary>Madde 3 — teklif ucu listeId ile SEÇİLEN listeyi fiyatlar.</summary>
    [Fact]
    public async Task Teklif_listeId_ile_SECILEN_listeyi_fiyatlar()
    {
        var k = YeniKiraci("UX4a teklif listeId");
        await ListeAc(k, "Varsayılan", 0m);
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var ikinci = await istemci.PostJson("/api/v1/fiyat/listeler",
            new { ad = "Yüksek marj", tur = "TOPTAN", hasMarjYuzde = 25m, iscilikCarpanYuzde = 100m, varsayilan = false });
        var ikinciId = (await ikinci.Json()).GetProperty("id").GetString()!;

        var varsayilan = await (await istemci.GetAsync($"/api/v1/fiyat/teklif?urunId={k.UrunId}&kurSnapshotId={k.KurId}")).Json();
        var secilen = await (await istemci.GetAsync($"/api/v1/fiyat/teklif?urunId={k.UrunId}&kurSnapshotId={k.KurId}&listeId={ikinciId}")).Json();

        Assert.Equal(0m, varsayilan.GetProperty("marjTl").GetDecimal());
        Assert.True(secilen.GetProperty("marjTl").GetDecimal() > 0m);
        Assert.True(secilen.GetProperty("listeFiyati").GetDecimal() > varsayilan.GetProperty("listeFiyati").GetDecimal());

        // Başka kiracının listesi kullanılamaz (kiracı sızıntısı kapısı).
        var yabanci = YeniKiraci("UX4a teklif yabancı");
        var yabanciListeId = await ListeAc(yabanci, "Yabancı", 50m);
        var capraz = await istemci.GetAsync($"/api/v1/fiyat/teklif?urunId={k.UrunId}&kurSnapshotId={k.KurId}&listeId={yabanciListeId}");
        Assert.Equal(HttpStatusCode.NotFound, capraz.StatusCode);
    }

    // ── PAKET 49 — liste × ürün uçlarının yetki kapısı ───────────────────────
    // Okuma (önizleme) "urun" yetkisi olan HERKESE açık; yazma SAHIP/MUDUR.

    /// <summary>M49-Y1 — tezgâhtar ürüne özel fiyat TANIMLAYAMAZ (403) ve satır yazılmaz.</summary>
    [Fact]
    public async Task TEZGAHTAR_urune_ozel_fiyat_TANIMLAYAMAZ_403()
    {
        var k = YeniKiraci("P49 yetki tezgâhtar");
        var listeId = await ListeAc(k);

        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson(
            $"/api/v1/fiyat/listeler/{listeId}/urun-fiyatlari",
            new { urunId = k.UrunId, sabitFiyat = 1m });
        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);

        var toplu = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson(
            $"/api/v1/fiyat/listeler/{listeId}/toplu-uygula?onizleme=true",
            new { hedefTur = "SECILI", urunIdler = new[] { k.UrunId }, hasMarjYuzde = 1m, kurSnapshotId = k.KurId });
        Assert.Equal(HttpStatusCode.Forbidden, toplu.StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.Empty(db.FiyatListesiUrunFiyatlari.Where(x => x.TenantId == k.TenantId));
    }

    /// <summary>M49-Y2 — tezgâhtar ÖNİZLEMEYİ görebilir (POS fiyatı zaten görüyor); müdür yazabilir.</summary>
    [Fact]
    public async Task Onizleme_TEZGAHTARA_acik_yazma_MUDURE_acik()
    {
        var k = YeniKiraci("P49 yetki müdür");
        var listeId = await ListeAc(k);

        var onizleme = await f.Ortam.Istemci(k, "TEZGAHTAR")
            .GetAsync($"/api/v1/fiyat/listeler/{listeId}/onizleme?kurSnapshotId={k.KurId}");
        Assert.Equal(HttpStatusCode.OK, onizleme.StatusCode);
        var json = await onizleme.Json();
        Assert.True(json.GetProperty("toplam").GetInt32() >= 1);
        Assert.Equal("LISTE", json.GetProperty("satirlar")[0].GetProperty("kaynak").GetString());

        var yaz = await f.Ortam.Istemci(k, "MUDUR").PostJson(
            $"/api/v1/fiyat/listeler/{listeId}/urun-fiyatlari",
            new { urunId = k.UrunId, hasMarjYuzde = 1m, not = "toptancı" });
        Assert.Equal(HttpStatusCode.OK, yaz.StatusCode);

        // Teklif ucu geriye dönük uyumlu genişledi: kaynak alanı geçersiz kılmayı söyler.
        var teklif = await (await f.Ortam.Istemci(k, "TEZGAHTAR")
            .GetAsync($"/api/v1/fiyat/teklif?urunId={k.UrunId}&kurSnapshotId={k.KurId}&listeId={listeId}")).Json();
        Assert.Equal("URUN_GECERSIZ_KILMA", teklif.GetProperty("kaynak").GetString());
    }

    /// <summary>Toplu uygulama: önizleme damgası onaylanmadan YAZILMAZ (409) — HTTP yolunda da.</summary>
    [Fact]
    public async Task Toplu_uygula_onaysiz_cagrilirsa_409_doner()
    {
        var k = YeniKiraci("P49 damga http");
        var listeId = await ListeAc(k);
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var govde = new { hedefTur = "SECILI", urunIdler = new[] { k.UrunId }, hasMarjYuzde = 1m, kurSnapshotId = k.KurId };

        var onaysiz = await istemci.PostJson($"/api/v1/fiyat/listeler/{listeId}/toplu-uygula", govde);
        Assert.Equal(HttpStatusCode.Conflict, onaysiz.StatusCode);

        var onizleme = await (await istemci.PostJson(
            $"/api/v1/fiyat/listeler/{listeId}/toplu-uygula?onizleme=true", govde)).Json();
        Assert.False(onizleme.GetProperty("uygulandi").GetBoolean());
        var damga = onizleme.GetProperty("parmakIzi").GetString()!;

        var uygula = await istemci.PostJson($"/api/v1/fiyat/listeler/{listeId}/toplu-uygula",
            new { hedefTur = "SECILI", urunIdler = new[] { k.UrunId }, hasMarjYuzde = 1m, kurSnapshotId = k.KurId, parmakIzi = damga });
        Assert.Equal(HttpStatusCode.OK, uygula.StatusCode);
        Assert.True((await uygula.Json()).GetProperty("uygulandi").GetBoolean());
    }
}
