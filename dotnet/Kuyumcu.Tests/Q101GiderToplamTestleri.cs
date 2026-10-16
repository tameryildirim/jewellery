using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q10-1 — GİDER TOPLAMI STORNO SONRASI NET OLUR [HTTP + gerçek MySQL]
//
// Kaynak: QA turu 10 (KRİTİK, /gider): ₺125,50 gider + storno → "Toplam gider:
// ₺251,00" görünüyordu (doğrusu ₺0,00). Kök neden istemcideydi (eksi × eksi);
// toplam artık SUNUCUDA, işaretli, satır sınırından bağımsız ve para birimi
// başına ayrı hesaplanır (GET /giderler → toplamlar[]).
//
// Finansal sınır testleri (doğrulayıcı koşulu):
//   • tam storno            125,50 + storno              → 0,00 (adet 2)
//   • kısmi storno          100 + 200,25 + 50, 200,25 storno → 150,00
//   • satır sınırı          limit=1 iken toplam TÜM satırları kapsar
//   • para birimi           TRY ve USD giderler TOPLANMAZ, ayrı döner
//   • süzgeç tutarlılığı    kategoriId süzgeci toplama da uygulanır
//   • boş liste             toplamlar boş dizi (uydurma 0 satırı yok)
//   • kasa bakiyesi         storno sonrası açılış bakiyesine döner (değişmedi)
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class Q101GiderToplamTestleri(HttpFikstur f)
{
    private const string Gerekce = "Q10-1 test gideri geri alınıyor (toplam doğrulaması).";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private async Task<string> KasaAc(Kiraci k, string ad, decimal acilis, string doviz = "TRY")
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson("/api/v1/kasalar", new { ad, subeId = k.SubeId, paraBirimi = doviz });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var id = (await cevap.Json()).GetProperty("id").GetString()!;
        using var db = f.Vt.OlusturSistem();
        var kasa = await db.Kasalar.SingleAsync(x => x.Id == id);
        kasa.Bakiye = acilis;
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<string> KategoriAc(Kiraci k, string ad)
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/gider-kategorileri", new { ad });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        return (await cevap.Json()).GetProperty("id").GetString()!;
    }

    private async Task<string> Gider(Kiraci k, string kasa, string kategori, decimal tutar)
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/giderler",
            new { kasaId = kasa, giderKategoriId = kategori, tutar });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        return (await cevap.Json()).GetProperty("id").GetString()!;
    }

    private async Task Storno(Kiraci k, string kasa, string satir)
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/kasalar/{kasa}/hareket/{satir}/storno", new { gerekce = Gerekce });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
    }

    private async Task<JsonElement> Liste(Kiraci k, string sorgu = "")
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/giderler" + sorgu);
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        return await cevap.Json();
    }

    /// <summary>para birimi → (tutar, adet) haritası; JSON ayrıştırılarak okunur.</summary>
    private static Dictionary<string, (decimal Tutar, int Adet)> Toplamlar(JsonElement liste) =>
        liste.GetProperty("toplamlar").EnumerateArray().ToDictionary(
            t => t.GetProperty("paraBirimi").GetString()!,
            t => (t.GetProperty("tutar").GetDecimal(), t.GetProperty("adet").GetInt32()));

    /// <summary>
    /// QA 10 senaryosu birebir. MUTASYON HEDEFİ (MQ101-1): sunucu toplamında
    /// <c>Sum(x =&gt; -x.Tutar)</c> → <c>Sum(x =&gt; Math.Abs(x.Tutar))</c> ise 251,00 çıkar → KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Q101_gider_ve_stornosu_toplamda_NET_SIFIR_olur_kasa_bakiyesi_doner()
    {
        var k = YeniKiraci("PAKETQ10-1 tam storno");
        var kasa = await KasaAc(k, "Nakit", 5_508.51m);
        var kat = await KategoriAc(k, "Kira");
        var satir = await Gider(k, kasa, kat, 125.50m);
        await Storno(k, kasa, satir);

        var l = await Liste(k);
        var t = Toplamlar(l);
        Assert.Single(t);
        Assert.Equal(0.00m, t["TRY"].Tutar);
        Assert.Equal(2, t["TRY"].Adet);

        // Satırlar İŞARETLİ gelir: gider +125,50, storno satırı -125,50.
        var tutarlar = l.GetProperty("satirlar").EnumerateArray()
            .Select(s => (s.GetProperty("stornoSatiri").GetBoolean(), s.GetProperty("tutar").GetDecimal()))
            .OrderBy(x => x.Item1).ToList();
        Assert.Equal([(false, 125.50m), (true, -125.50m)], tutarlar);

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(5_508.51m, (await db.Kasalar.AsNoTracking().SingleAsync(x => x.Id == kasa)).Bakiye);
    }

    /// <summary>3 gider, 1 storno: yalnız stornolanan düşer.</summary>
    [Fact]
    public async Task Q101_kismi_storno_yalniz_stornolanan_gideri_duser()
    {
        var k = YeniKiraci("PAKETQ10-1 kismi storno");
        var kasa = await KasaAc(k, "Nakit", 10_000m);
        var kat = await KategoriAc(k, "Nakliye");
        await Gider(k, kasa, kat, 100m);
        var orta = await Gider(k, kasa, kat, 200.25m);
        await Gider(k, kasa, kat, 50m);
        await Storno(k, kasa, orta);

        var t = Toplamlar(await Liste(k));
        Assert.Equal(150.00m, t["TRY"].Tutar);
        Assert.Equal(4, t["TRY"].Adet);
    }

    /// <summary>
    /// Toplam, ekrana gelen satır SINIRINDAN bağımsızdır (limit=1 iken de 3 satır toplanır).
    /// MUTASYON HEDEFİ (MQ101-4): toplam sorgusu <c>Take(limit)</c> ile kırpılırsa KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Q101_toplam_satir_sinirindan_BAGIMSIZDIR()
    {
        var k = YeniKiraci("PAKETQ10-1 limit");
        var kasa = await KasaAc(k, "Nakit", 10_000m);
        var kat = await KategoriAc(k, "Kırtasiye");
        await Gider(k, kasa, kat, 10m);
        await Gider(k, kasa, kat, 20m);
        await Gider(k, kasa, kat, 30m);

        var l = await Liste(k, "?limit=1");
        Assert.Single(l.GetProperty("satirlar").EnumerateArray());
        Assert.True(l.GetProperty("dahaFazlasiVar").GetBoolean());
        var t = Toplamlar(l);
        Assert.Equal(60m, t["TRY"].Tutar);
        Assert.Equal(3, t["TRY"].Adet);
    }

    /// <summary>
    /// UX-S32: TRY ve USD gider TOPLANMAZ. MUTASYON HEDEFİ (MQ101-5): gruplama
    /// para biriminden koparılırsa (tek grup) KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Q101_para_birimleri_ayri_toplanir_KARISMAZ()
    {
        var k = YeniKiraci("PAKETQ10-1 doviz");
        var tl = await KasaAc(k, "TL Kasa", 10_000m);
        var usd = await KasaAc(k, "Dolar Kasa", 1_000m, "USD");
        var kat = await KategoriAc(k, "Kargo");
        await Gider(k, tl, kat, 300m);
        var u1 = await Gider(k, usd, kat, 40m);
        await Gider(k, usd, kat, 15.75m);
        await Storno(k, usd, u1);

        var t = Toplamlar(await Liste(k));
        Assert.Equal(2, t.Count);
        Assert.Equal(300m, t["TRY"].Tutar);
        Assert.Equal(15.75m, t["USD"].Tutar);
        Assert.Equal(3, t["USD"].Adet);
    }

    /// <summary>Süzgeç (kategori) listeye ne uygulanıyorsa toplama da AYNEN uygulanır.</summary>
    [Fact]
    public async Task Q101_kategori_suzgeci_toplama_da_uygulanir()
    {
        var k = YeniKiraci("PAKETQ10-1 suzgec");
        var kasa = await KasaAc(k, "Nakit", 10_000m);
        var kira = await KategoriAc(k, "Kira");
        var yemek = await KategoriAc(k, "Yemek");
        await Gider(k, kasa, kira, 1_000m);
        await Gider(k, kasa, yemek, 75.40m);

        var t = Toplamlar(await Liste(k, $"?kategoriId={yemek}"));
        Assert.Equal(75.40m, t["TRY"].Tutar);
        Assert.Equal(1, t["TRY"].Adet);
    }

    /// <summary>Hiç gider yoksa toplam dizisi BOŞ döner; uydurma "0 TRY" satırı yazılmaz.</summary>
    [Fact]
    public async Task Q101_gider_yoksa_toplamlar_BOS_dizidir()
    {
        var k = YeniKiraci("PAKETQ10-1 bos");
        var l = await Liste(k);
        Assert.Equal(JsonValueKind.Array, l.GetProperty("toplamlar").ValueKind);
        Assert.Empty(l.GetProperty("toplamlar").EnumerateArray());
    }
}
