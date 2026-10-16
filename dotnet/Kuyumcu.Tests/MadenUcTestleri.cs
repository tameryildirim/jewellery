using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 20 Faz A — MADEN UÇLARI (gerçek HTTP).
//
// ⚠ NEDEN SERVİS TESTİ YETMEZ: DTO doğrulaması (AyarIdAttribute) yalnız
//   CONTROLLER yolunda çalışır. Servise elle DTO verilirse "geçersiz ayar 400"
//   hiç görülmez; ayrım ancak gerçek HTTP gövdesiyle kanıtlanır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class MadenUcTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static async Task<JsonElement> Govde(HttpResponseMessage c)
        => JsonDocument.Parse(await c.Content.ReadAsStringAsync()).RootElement.Clone();

    /// <summary>
    /// Hata mesajı — ⚠ HAM GÖVDEYE BAKILMAZ: System.Text.Json Türkçe harfleri
    /// \u00FC gibi kaçış dizileriyle yazar; "gümüş" araması ham metinde ASLA
    /// bulunmaz ve test yanlış sebeple kırmızı olurdu. Gövde AYRIŞTIRILIR.
    /// </summary>
    private static async Task<string> Mesaj(HttpResponseMessage c)
        => (await Govde(c)).GetProperty("message").GetString() ?? "";

    // ── GET /urunler/madenler ────────────────────────────────────────────────

    /// <summary>Mehmet koşulu: uç [Authorize]'dur — anonim AÇILMAZ.</summary>
    [Fact]
    public async Task Madenler_ucu_ANONIM_erisime_KAPALI()
    {
        var c = await f.Ortam.AnonimIstemci().GetAsync("/api/v1/urunler/madenler");
        Assert.Equal(HttpStatusCode.Unauthorized, c.StatusCode);
    }

    [Fact]
    public async Task Madenler_ucu_saflik_tablosunu_ve_etiket_politikasini_doner()
    {
        var k = YeniKiraci("PAKET20-TEST madenler ucu");
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/urunler/madenler");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);

        var g = await Govde(c);
        var madenler = g.GetProperty("madenler");
        Assert.Equal(Maden.Hepsi.Length, madenler.GetArrayLength());

        // ÇELİK: kıymetsiz, has hesaplanmaz, rodyum kaplanamaz.
        var celik = madenler.EnumerateArray().Single(m => m.GetProperty("maden").GetString() == "CELIK");
        Assert.False(celik.GetProperty("rodyumKaplanabilir").GetBoolean());
        var celikAyar = celik.GetProperty("ayarlar")[0];
        Assert.False(celikAyar.GetProperty("kiymetli").GetBoolean());
        Assert.False(celikAyar.GetProperty("hasHesaplanir").GetBoolean());

        // PLATIN: saflık tablosu milyemiyle döner.
        var platin = madenler.EnumerateArray().Single(m => m.GetProperty("maden").GetString() == "PLATIN");
        Assert.Contains(platin.GetProperty("ayarlar").EnumerateArray(),
            a => a.GetProperty("id").GetString() == "PT950" && a.GetProperty("milyem").GetDecimal() == 950m);

        // RODYUM stok madeni DEĞİLDİR — listede olmamalı (K20-7).
        Assert.DoesNotContain(madenler.EnumerateArray(), m => m.GetProperty("maden").GetString() == "RODYUM");

        // ADR-0005: rodyum ibaresi VARSAYILAN KAPALI.
        Assert.False(g.GetProperty("etiket").GetProperty("rodyumIbaresiGoster").GetBoolean());
    }

    // ── POST /urunler — maden kapısı gerçek istekte ─────────────────────────

    private static StringContent Json(string s) => new(s, Encoding.UTF8, "application/json");

    [Fact]
    public async Task Gumus_tipinde_ALTIN_ayari_400_doner()
    {
        var k = YeniKiraci("PAKET20-TEST tip-ayar");
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/urunler", new
        {
            tip = "GUMUS", kod = "P20-G1", ad = "Gümüş kolye", ayarId = "K22", gramAgirlik = 12m,
        });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Contains("gümüş", await Mesaj(c));
    }

    [Fact]
    public async Task Celik_urun_sabit_fiyatsiz_400_fiyatlı_201_doner()
    {
        var k = YeniKiraci("PAKET20-TEST çelik");
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var red = await istemci.PostAsJsonAsync("/api/v1/urunler", new
        {
            tip = "CELIK", kod = "P20-C1", ad = "Çelik saat", ayarId = "CELIK", gramAgirlik = 40m,
        });
        Assert.Equal(HttpStatusCode.BadRequest, red.StatusCode);
        Assert.Contains("satış fiyatı zorunludur", await Mesaj(red));

        var kabul = await istemci.PostAsJsonAsync("/api/v1/urunler", new
        {
            tip = "CELIK", kod = "P20-C2", ad = "Çelik saat", ayarId = "CELIK",
            gramAgirlik = 40m, satisFiyati = 2_500m,
        });
        Assert.Equal(HttpStatusCode.Created, kabul.StatusCode);

        // K20-5: çelikte has gramı 0 — 40 g çelik defterde has altına DÖNÜŞMEZ.
        var g = await Govde(kabul);
        Assert.Equal(0m, g.GetProperty("hasGrami").GetDecimal());
        Assert.Equal("CELIK", g.GetProperty("maden").GetString());
        Assert.Equal("Çelik", g.GetProperty("madenAdi").GetString());
    }

    [Fact]
    public async Task Platin_urun_kaydedilir_ve_madeni_PLATIN_doner()
    {
        var k = YeniKiraci("PAKET20-TEST platin ürün");
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/urunler", new
        {
            tip = "PLATIN", kod = "P20-PT1", ad = "Platin alyans", ayarId = "PT950", gramAgirlik = 5m,
        });
        Assert.Equal(HttpStatusCode.Created, c.StatusCode);

        var g = await Govde(c);
        Assert.Equal("PLATIN", g.GetProperty("maden").GetString());
        Assert.Equal(4.75m, g.GetProperty("hasGrami").GetDecimal());   // PLATIN hası
    }

    [Fact]
    public async Task Rodyum_kaplama_yazilir_okunur_ve_PATCH_ile_kaldirilir()
    {
        var k = YeniKiraci("PAKET20-TEST rodyum");
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var c = await istemci.PostAsJsonAsync("/api/v1/urunler", new
        {
            tip = "ZIYNET", kod = "P20-R1", ad = "Beyaz altın yüzük", ayarId = "K18",
            gramAgirlik = 3m, rodyumKaplama = true,
        });
        Assert.Equal(HttpStatusCode.Created, c.StatusCode);
        var id = (await Govde(c)).GetProperty("id").GetString()!;
        Assert.True((await Govde(c)).GetProperty("rodyumKaplama").GetBoolean());

        // PATCH sözleşmesi: gönderilmeyen alan KORUNUR.
        var korundu = await istemci.PatchAsync($"/api/v1/urunler/{id}", Json("""{"ad":"Yeni ad"}"""));
        Assert.Equal(HttpStatusCode.OK, korundu.StatusCode);
        Assert.True((await Govde(korundu)).GetProperty("rodyumKaplama").GetBoolean());

        // Açıkça false gönderilince KALDIRILIR (düzenleme yolu var — UX-S25).
        var kaldirildi = await istemci.PatchAsync($"/api/v1/urunler/{id}", Json("""{"rodyumKaplama":false}"""));
        Assert.Equal(HttpStatusCode.OK, kaldirildi.StatusCode);
        Assert.False((await Govde(kaldirildi)).GetProperty("rodyumKaplama").GetBoolean());
    }

    [Fact]
    public async Task Rodyum_kaplama_gumus_urunde_400_doner()
    {
        var k = YeniKiraci("PAKET20-TEST rodyum gümüş");
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/urunler", new
        {
            tip = "GUMUS", kod = "P20-RG", ad = "Gümüş yüzük", ayarId = "G925",
            gramAgirlik = 6m, rodyumKaplama = true,
        });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Contains("Rodyum", await Mesaj(c));
    }

    /// <summary>
    /// K20-8: ayar doğrulaması DÖRT DTO'da elle yazılı bir desen değil, TEK
    /// sözlüktür. Çelik hiçbir yerde elle eklenmeden uçtan KABUL edilir.
    /// </summary>
    [Fact]
    public async Task Gecersiz_ayar_400_gecerli_ayar_kabul()
    {
        var k = YeniKiraci("PAKET20-TEST ayar deseni");
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var red = await istemci.PostAsJsonAsync("/api/v1/urunler", new
        {
            tip = "ZIYNET", kod = "P20-X", ad = "Uydurma ayar", ayarId = "K23", gramAgirlik = 3m,
        });
        Assert.Equal(HttpStatusCode.BadRequest, red.StatusCode);

        // EMANET ucu da AYNI sözlüğe bağlı — eskiden desen orada ayrı yazılıydı.
        var emanet = await istemci.PostAsJsonAsync("/api/v1/emanet/giris", new
        {
            cariId = k.CariId, ayarId = "K23", gramAgirlik = 10m, kurSnapshotId = k.KurId,
        });
        Assert.Equal(HttpStatusCode.BadRequest, emanet.StatusCode);
    }

    /// <summary>
    /// UX-S25 + denetim: ürünün madeni değişince denetim izine GEREKÇELİ satır
    /// düşer — geçmiş defter satırları eski madenle kalır, sessiz geçilmez.
    /// </summary>
    [Fact]
    public async Task Maden_degisince_denetim_izine_yazilir()
    {
        var k = YeniKiraci("PAKET20-TEST maden değişimi");
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var c = await istemci.PostAsJsonAsync("/api/v1/urunler", new
        {
            tip = "SAAT", kod = "P20-M1", ad = "Saat", ayarId = "K18", gramAgirlik = 20m,
        });
        var id = (await Govde(c)).GetProperty("id").GetString()!;

        var patch = await istemci.PatchAsync($"/api/v1/urunler/{id}", Json("""{"ayarId":"PT950"}"""));
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        Assert.Equal("PLATIN", (await Govde(patch)).GetProperty("maden").GetString());

        using var db = f.Vt.OlusturSistem();
        var izler = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikId == id).ToListAsync();
        Assert.Contains(izler, d => d.Detay != null && d.Detay.Contains("Ürün madeni Altın → Platin"));
    }

    /// <summary>Maden filtresi sunucuda süzer (TB-19: liste belleğe çekilip orada filtrelenmez).</summary>
    [Fact]
    public async Task Maden_filtresi_yalniz_o_madenin_urunlerini_doner()
    {
        var k = YeniKiraci("PAKET20-TEST maden filtresi");
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        await istemci.PostAsJsonAsync("/api/v1/urunler", new
        {
            tip = "GUMUS", kod = "P20-F1", ad = "Gümüş kolye", ayarId = "G925", gramAgirlik = 12m,
        });

        var c = await istemci.GetAsync("/api/v1/urunler?maden=GUMUS");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var liste = await Govde(c);
        Assert.Equal(1, liste.GetArrayLength());
        Assert.Equal("G925", liste[0].GetProperty("ayarId").GetString());

        // Kiracının kurulum ürünü K22'dir; ALTIN filtresi onu getirir.
        var altin = await Govde(await istemci.GetAsync("/api/v1/urunler?maden=ALTIN"));
        Assert.Equal(1, altin.GetArrayLength());
        Assert.Equal("K22", altin[0].GetProperty("ayarId").GetString());
    }
}
