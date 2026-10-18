using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Contracts.Sayfalar.Urunler;
using Kuyumcu.Core.Domain.Girdi;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-16 (18.10, rakip eşitleme) — akıllı içe aktarım (yerel sütun önerisi) · toptancı barkodu · ürün seti.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class D1KolonTanimaTestleri
{
    [Fact]
    public void Yaygin_basliklar_alanlara_onerilir_bir_baslik_tek_alana_gider_bilinmeyen_onerilmez()
    {
        string[] basliklar = ["Ürün Adı", "Stok Kodu", "GRAM", "Satış Fiyatı", "Barkod", "Rastgele Sütun"];
        var o = KolonTanima.OnerHepsi([("kod", true), ("ad", true), ("gramAgirlik", true), ("satisFiyati", false), ("barkod", false), ("tasAgirlik", false)], basliklar);
        Assert.Equal("Stok Kodu", o["kod"]);
        Assert.Equal("Ürün Adı", o["ad"]);
        Assert.Equal("GRAM", o["gramAgirlik"]);
        Assert.Equal("Satış Fiyatı", o["satisFiyati"]);
        Assert.Equal("Barkod", o["barkod"]);
        Assert.False(o.ContainsKey("tasAgirlik"));             // dosyada karşılığı yok → öneri YOK
        Assert.Equal(o.Count, o.Values.Distinct().Count());     // bir başlık iki alana önerilmez
        Assert.Equal("musteriadi", KolonTanima.Duzlestir("Müşteri_Adı "));
    }
}

[Collection("http")]
public sealed class D1SetBarkodHttpTestleri(HttpFikstur f)
{
    private static async Task<string> UrunAc(HttpClient s, string kod, string? barkod = null, string? tedarikci = null)
    {
        var r = await s.PostAsJsonAsync("/api/v1/urunler", new { tip = "ZIYNET", kod, ad = kod + " ad", ayarId = "K22", gramAgirlik = 2m, barkod, tedarikciBarkodu = tedarikci, acilisStogu = new { adet = 3m } });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Toptanci_barkoduyla_urun_bulunur_cakisma_409_silinebilir()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1-tedarikci");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var a = await UrunAc(s, "TB-A", "BIZ-001", "TOPTAN-777");
        var bul = await s.GetFromJsonAsync<JsonElement>("/api/v1/urunler/barkod/TOPTAN-777");
        Assert.Equal(a, bul.GetProperty("id").GetString());
        Assert.Equal(a, (await s.GetFromJsonAsync<JsonElement>("/api/v1/urunler/barkod/BIZ-001")).GetProperty("id").GetString());

        var c = await s.PostAsJsonAsync("/api/v1/urunler", new { tip = "ZIYNET", kod = "TB-B", ad = "TB-B ad", ayarId = "K22", gramAgirlik = 2m, barkod = "TOPTAN-777" });
        Assert.Equal(HttpStatusCode.Conflict, c.StatusCode);   // başkasının toptancı barkodu kendi barkodumuz olamaz

        var p = await s.PatchAsJsonAsync($"/api/v1/urunler/{a}", new { tedarikciBarkodu = "" });
        Assert.True(p.IsSuccessStatusCode, await p.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await s.GetAsync("/api/v1/urunler/barkod/TOPTAN-777")).StatusCode);
    }

    [Fact]
    public async Task Set_en_az_iki_parca_barkod_tekil_okutmada_parcalar_doner_tezgahtar_yazamaz()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1-set");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var kolye = await UrunAc(s, "SET-KOLYE", "KOLYE-1");
        var kupe = await UrunAc(s, "SET-KUPE");

        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync("/api/v1/urun-setleri",
            new { ad = "Tek parça", barkod = "SET-0", kalemler = new[] { new { urunId = kolye, adet = 1m } } })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await s.PostAsJsonAsync("/api/v1/urun-setleri",
            new { ad = "Takım", barkod = "KOLYE-1", kalemler = new[] { new { urunId = kolye, adet = 1m }, new { urunId = kupe, adet = 2m } } })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsJsonAsync("/api/v1/urun-setleri",
            new { ad = "Takım", barkod = "SET-1", kalemler = new[] { new { urunId = kolye, adet = 1m }, new { urunId = kupe, adet = 2m } } })).StatusCode);

        var r = await s.PostAsJsonAsync("/api/v1/urun-setleri",
            new { ad = "Gelin takımı", barkod = "SET-1", kalemler = new[] { new { urunId = kolye, adet = 1m }, new { urunId = kupe, adet = 2m } } });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        var set = (await r.Content.ReadFromJsonAsync<UrunSetiVeri>())!;

        var t = f.Ortam.Istemci(k, "TEZGAHTAR");
        var okut = await t.GetFromJsonAsync<UrunSetiVeri>("/api/v1/urun-setleri/barkod/SET-1");
        Assert.Equal([kolye, kupe], okut!.Kalemler.Select(x => x.UrunId));
        Assert.Equal(2m, okut.Kalemler[1].Adet);
        // Set barkodu ürün barkodu olarak da kullanılamaz.
        Assert.Equal(HttpStatusCode.Conflict, (await s.PostAsJsonAsync("/api/v1/urunler", new { tip = "ZIYNET", kod = "X1", ad = "X1 ad", ayarId = "K22", gramAgirlik = 2m, barkod = "SET-1" })).StatusCode);

        var p = await s.PutAsJsonAsync($"/api/v1/urun-setleri/{set.Id}",
            new { ad = "Gelin takımı", barkod = "SET-1", aktif = false, kalemler = new[] { new { urunId = kolye, adet = 1m }, new { urunId = kupe, adet = 1m } } });
        Assert.True(p.IsSuccessStatusCode, await p.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await t.GetAsync("/api/v1/urun-setleri/barkod/SET-1")).StatusCode);   // satışta değil
    }
}
