using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// M8 kalanı (20.10) — (1) banka AÇIKLAMASINDAKİ TC/VKN ile müşteri bulunur, "Gönd: AD" yalnız öneri verir;
// (2) gelen ödeme borcu TAMAMEN kapatınca satışın kesilmiş faturası tek tıkla ÖDENDİ olur, banka hareketine
// bağlanır; kısmi ödemede fatura değişmez; karar geri alınınca fatura yeniden KESİLDİ.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class M8KalanHttpTestleri(HttpFikstur f)
{
    private const string Tc = "10000000146";

    private async Task<string> HesapAc(Kiraci k)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/bankalar", new
        { bankaAdi = "Ziraat", hesapAdi = "Ticari", iban = TestIban.Uret(), paraBirimi = "TRY" });
        Assert.True(c.IsSuccessStatusCode, await c.Content.ReadAsStringAsync());
        return (await c.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    private async Task<JsonElement> Dekont(Kiraci k, string hesap, object govde)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync($"/api/v1/bankalar/{hesap}/hareket", govde);
        var y = await c.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(c.IsSuccessStatusCode, y.ToString());
        return y;
    }

    private async Task<string> CariAc(Kiraci k, string ad, string? tc = null)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/cari", new { tur = "MUSTERI", ad, vergiNo = tc });
        var y = await c.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(c.IsSuccessStatusCode, y.ToString());
        return y.GetProperty("id").GetString()!;
    }

    /// <summary>Faturalı veresiye satış → (satış hareketi, fatura).</summary>
    private async Task<(string HareketId, string FaturaId)> FaturaliVeresiye(Kiraci k, decimal tutar)
    {
        var s = f.Ortam.Istemci(k, "SAHIP");
        var r = await s.PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId, fisIskontoYuzde = 0m, belgeTuru = "FATURA",
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = tutar, hediye = false } },
            odemeler = new[] { new { arac = "VERESIYE", tutarTl = tutar, kasaId = (string?)null } },
        });
        var y = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(r.IsSuccessStatusCode, y.ToString());
        var fisId = y.GetProperty("satis").GetProperty("fisId").GetString();
        using var db = f.Vt.OlusturSistem();
        var hid = db.Hareketler.AsNoTracking().Single(h => h.TenantId == k.TenantId && h.FisId == fisId && h.Tur == "SATIS").Id;
        var fid = db.FaturaKalemleri.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.HareketId == hid).FaturaId;
        Assert.Equal("KESILDI", db.Faturalar.AsNoTracking().Single(x => x.Id == fid).Durum);
        return (hid, fid);
    }

    [Fact]
    public async Task Aciklamadaki_TC_ile_musteri_bulunur_Gond_etiketi_yalniz_oneri_verir()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "M8-aciklama");
        var hesap = await HesapAc(k);
        var tcli = await CariAc(k, "Hatice Demirkol", Tc);
        // Ad alanı boş, kimlik alanı boş: açıklama taşıyor.
        var y = await Dekont(k, hesap, new { yon = "GELEN", tutar = 500m, aciklama = $"HAVALE {Tc} HATICE DEMIRKOL" });
        Assert.Equal("KIMLIK", y.GetProperty("cari").GetProperty("yol").GetString());
        Assert.Equal(tcli, y.GetProperty("cari").GetProperty("cariId").GetString());

        var benzer = await CariAc(k, "Mehmet Ali Yıldırım");
        var y2 = await Dekont(k, hesap, new { yon = "GELEN", tutar = 600m, aciklama = "Gönd: MEHMET ALİ YILDIRIM 0157-Banka", karsiIban = TestIban.Uret() });
        Assert.Equal("ONERI", y2.GetProperty("cari").GetProperty("yol").GetString());
        Assert.Contains(y2.GetProperty("cari").GetProperty("oneriler").EnumerateArray(), o => o.GetProperty("id").GetString() == benzer);
        using var db2 = f.Vt.OlusturSistem();
        Assert.Equal(2, await db2.Cariler.CountAsync(c => c.TenantId == k.TenantId && !c.SistemCarisi && c.Id != k.CariId));   // yeni kart AÇILMADI
    }

    [Fact]
    public async Task Tam_odemede_fatura_ODENDI_ve_banka_hareketine_baglanir_kismi_odemede_degismez_geri_alinca_KESILDI()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "M8-fatura", urunGram: 1m);
        var s = f.Ortam.Istemci(k, "SAHIP");
        Assert.True((await s.PostAsJsonAsync("/api/v1/islemler/musteri-kimlik", new { cariId = k.CariId, kimlikNo = Tc })).IsSuccessStatusCode);
        var hesap = await HesapAc(k);
        var (h1, f1) = await FaturaliVeresiye(k, 10_000m);
        var (h2, f2) = await FaturaliVeresiye(k, 15_000m);

        // Liste fatura numarasını taşır.
        var d1 = (await Dekont(k, hesap, new { yon = "GELEN", tutar = 10_000m, cariId = k.CariId })).GetProperty("hareketId").GetString()!;
        var satir = (await s.GetFromJsonAsync<JsonElement>("/api/v1/bankalar/karar-bekleyen")).EnumerateArray().Single(x => x.GetProperty("id").GetString() == d1);
        var liste = satir.GetProperty("borcluSatislar").EnumerateArray().ToList();
        Assert.All(liste, x => Assert.StartsWith("FTR-", x.GetProperty("faturaNo").GetString()));

        // 10.000 → en eski satış (h1) tamamen kapanır → f1 ÖDENDİ; f2 KESİLDİ kalır.
        var kr = await s.PostAsJsonAsync($"/api/v1/bankalar/hareketler/{d1}/karar", new { karar = "BORC" });
        var ky = await kr.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(kr.IsSuccessStatusCode, ky.ToString());
        Assert.Single(ky.GetProperty("odenenFaturalar").EnumerateArray());
        using (var db = f.Vt.OlusturSistem())
        {
            var fa = await db.Faturalar.AsNoTracking().SingleAsync(x => x.Id == f1);
            Assert.Equal(("ODENDI", d1), (fa.Durum, fa.BankaHareketId));
            Assert.Equal("KESILDI", (await db.Faturalar.AsNoTracking().SingleAsync(x => x.Id == f2)).Durum);
        }

        // 5.000 kısmi → f2 değişmez.
        var d2 = (await Dekont(k, hesap, new { yon = "GELEN", tutar = 5_000m, cariId = k.CariId })).GetProperty("hareketId").GetString()!;
        var kr2 = await s.PostAsJsonAsync($"/api/v1/bankalar/hareketler/{d2}/karar", new { karar = "BORC", mahsuplar = new[] { new { borcHareketId = h2, tutarTl = 5_000m } } });
        var ky2 = await kr2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(kr2.IsSuccessStatusCode, ky2.ToString());
        Assert.Empty(ky2.GetProperty("odenenFaturalar").EnumerateArray());
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal("KESILDI", (await db.Faturalar.AsNoTracking().SingleAsync(x => x.Id == f2)).Durum);

        // Geri al → f1 yeniden KESİLDİ, bağ kalkar.
        Assert.True((await s.PostAsync($"/api/v1/bankalar/hareketler/{d1}/karar/geri-al", null)).IsSuccessStatusCode);
        using (var db = f.Vt.OlusturSistem())
        {
            var fa = await db.Faturalar.AsNoTracking().SingleAsync(x => x.Id == f1);
            Assert.Equal(("KESILDI", (string?)null), (fa.Durum, fa.BankaHareketId));
        }
    }
}
