using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// REYON DÜZENLEME — paket UX-1 Faz B [HTTP, gerçek MySQL]
//
// NEDEN HTTP: [RolGerekli] ve [ModulYetkisi] DENETLEYİCİ nitelikleridir; servis
// testi ikisini de görmez. Kiracı izolasyonu (404) ve üçlü-durum PATCH gövdesi
// de ancak gerçek JSON bağlamasıyla sınanabilir.
//
// Sözleşme (harita: Modules/Meta/DuzeltmePolitikasi.cs → Reyon DUZENLENEBILIR):
//   • PATCH /reyonlar/{id}  → ad · tip · sorumlu kullanıcı · aktif
//   • DELETE /reyonlar/{id} → SİLMEZ, aktif=0 yazar; üzerinde ürün varsa 409
//   • Şube HİÇ değişmez — DTO'da SubeId alanı YOKTUR (Mehmet K6)
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class ReyonDuzenlemeTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private async Task<string> ReyonAc(Kiraci k, string ad = "Vitrin-1", string tip = "VITRIN")
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/reyonlar",
            new { subeId = k.SubeId, ad, tip });
        Assert.Equal(HttpStatusCode.Created, cevap.StatusCode);
        return (await cevap.Json()).GetProperty("id").GetString()!;
    }

    private async Task<Reyon> ReyonOku(string id)
    {
        using var db = f.Vt.OlusturSistem();
        return await db.Set<Reyon>().AsNoTracking().SingleAsync(r => r.Id == id);
    }

    /// <summary>Temel yol: ad ve tip yerinde düzeltilir.</summary>
    [Fact]
    public async Task PATCH_reyon_adini_ve_tipini_gunceller()
    {
        var k = YeniKiraci("UX1B reyon guncelle");
        var id = await ReyonAc(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/reyonlar/{id}", new { ad = "Pırlanta vitrini", tip = "KASA" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var govde = await cevap.Json();
        Assert.Equal("Pırlanta vitrini", govde.GetProperty("ad").GetString());
        Assert.Equal("KASA", govde.GetProperty("tip").GetString());

        var satir = await ReyonOku(id);
        Assert.Equal("Pırlanta vitrini", satir.Ad);
        Assert.Equal("KASA", satir.Tip);
    }

    /// <summary>M-UX1-02 hedefi — reyonun şubesi HİÇ değişmez (stok satırı reyona bağlı).</summary>
    [Fact]
    public async Task Sube_degistirilemez()
    {
        var k = YeniKiraci("UX1B reyon şube");
        string ikinciSube;
        using (var db = f.Vt.OlusturSistem()) ikinciSube = TestKurulum.SubeEkle(db, k.TenantId);
        var id = await ReyonAc(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchAsJsonAsync(
            $"/api/v1/reyonlar/{id}", new { ad = "Taşındı", subeId = ikinciSube });

        // Gövdede subeId gönderilse bile bağlanacak alan YOKTUR → satır değişmez.
        var satir = await ReyonOku(id);
        Assert.Equal(k.SubeId, satir.SubeId);
        Assert.NotEqual(ikinciSube, satir.SubeId);
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
    }

    /// <summary>PATCH sözleşmesi: gövdede olmayan alan KORUNUR.</summary>
    [Fact]
    public async Task Alan_govdede_yoksa_KORUNUR()
    {
        var k = YeniKiraci("UX1B reyon koru");
        var id = await ReyonAc(k, "Korunacak ad");

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/reyonlar/{id}", new { tip = "DEPO" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var satir = await ReyonOku(id);
        Assert.Equal("Korunacak ad", satir.Ad);
        Assert.Equal("DEPO", satir.Tip);
    }

    /// <summary>PATCH sözleşmesi: açıkça null gönderilen alan SİLİNİR.</summary>
    [Fact]
    public async Task Sorumlu_kullanici_null_gonderilince_SILINIR()
    {
        var k = YeniKiraci("UX1B reyon sorumlu");
        var cevap0 = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/reyonlar",
            new { subeId = k.SubeId, ad = "Sorumlulu", tip = "VITRIN", sorumluKullaniciId = k.KullaniciId });
        var id = (await cevap0.Json()).GetProperty("id").GetString()!;
        Assert.Equal(k.KullaniciId, (await ReyonOku(id)).SorumluKullaniciId);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchAsJsonAsync(
            $"/api/v1/reyonlar/{id}", new { sorumluKullaniciId = (string?)null });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.Null((await ReyonOku(id)).SorumluKullaniciId);
    }

    /// <summary>Üzerinde ürün olan reyon pasife alınamaz — 409 ve satır AKTİF kalır.</summary>
    [Fact]
    public async Task Urunlu_reyon_DELETE_409_ve_satir_aktif_kalir()
    {
        var k = YeniKiraci("UX1B reyon ürünlü");
        var id = await ReyonAc(k);

        using (var db = f.Vt.Olustur(k))
        {
            var u = await db.Urunler.SingleAsync(x => x.Id == k.UrunId);
            u.ReyonId = id;
            await db.SaveChangesAsync();
        }

        var cevap = await f.Ortam.Istemci(k, "SAHIP").DeleteAsync($"/api/v1/reyonlar/{id}");

        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
        // Türkçe gövde AYRIŞTIRILARAK sınanır (JSON \uXXXX kaçışları yüzünden
        // ham metinde "ürün" aranamaz).
        var mesaj = (await cevap.Json()).GetProperty("message").GetString()!;
        Assert.Contains("ürün", mesaj, StringComparison.OrdinalIgnoreCase);
        Assert.True((await ReyonOku(id)).Aktif);
    }

    /// <summary>Ürün çözülünce DELETE pasife alır — SATIR SİLİNMEZ (aktif=0).</summary>
    [Fact]
    public async Task Bos_reyon_DELETE_pasife_alir_satir_DURUR()
    {
        var k = YeniKiraci("UX1B reyon boş");
        var id = await ReyonAc(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").DeleteAsync($"/api/v1/reyonlar/{id}");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.False((await cevap.Json()).GetProperty("aktif").GetBoolean());

        var satir = await ReyonOku(id);      // satır DURUYOR
        Assert.False(satir.Aktif);

        // UX-S25 geri dönüş yolu: PATCH aktif=true ile geri açılır.
        var geri = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/reyonlar/{id}", new { aktif = true });
        Assert.Equal(HttpStatusCode.OK, geri.StatusCode);
        Assert.True((await ReyonOku(id)).Aktif);
    }

    /// <summary>Kiracı izolasyonu: yabancı kiracının reyonu YOK sayılır.</summary>
    [Fact]
    public async Task Yabanci_kiracinin_reyonu_404()
    {
        var a = YeniKiraci("UX1B reyon A");
        var b = YeniKiraci("UX1B reyon B");
        var id = await ReyonAc(a);

        var patch = await f.Ortam.Istemci(b, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/reyonlar/{id}", new { ad = "Ele geçirildi" });
        var delete = await f.Ortam.Istemci(b, "SAHIP").DeleteAsync($"/api/v1/reyonlar/{id}");

        Assert.Equal(HttpStatusCode.NotFound, patch.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        var satir = await ReyonOku(id);
        Assert.Equal("Vitrin-1", satir.Ad);
        Assert.True(satir.Aktif);
    }

    /// <summary>Rol kapısı: tezgâhtar reyon düzenleyemez/pasife alamaz.</summary>
    [Fact]
    public async Task TEZGAHTAR_reyonu_DUZENLEYEMEZ_403()
    {
        var k = YeniKiraci("UX1B reyon rol");
        var id = await ReyonAc(k);

        var patch = await f.Ortam.Istemci(k, "TEZGAHTAR")
            .PatchAsJsonAsync($"/api/v1/reyonlar/{id}", new { ad = "İzinsiz" });
        var delete = await f.Ortam.Istemci(k, "TEZGAHTAR").DeleteAsync($"/api/v1/reyonlar/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, patch.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);

        var satir = await ReyonOku(id);
        Assert.Equal("Vitrin-1", satir.Ad);
        Assert.True(satir.Aktif);
    }

    /// <summary>Geçersiz tip sessizce yazılmaz (sunucu doğrulaması).</summary>
    [Fact]
    public async Task Gecersiz_tip_400_ve_satir_degismez()
    {
        var k = YeniKiraci("UX1B reyon tip");
        var id = await ReyonAc(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/reyonlar/{id}", new { tip = "MAHZEN" });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Equal("VITRIN", (await ReyonOku(id)).Tip);
    }

    /// <summary>Mehmet (UX-1 Faz B): her PATCH/DELETE ucu denetim izine yazar.</summary>
    [Fact]
    public async Task Duzenleme_DENETIM_KAYDI_yazar()
    {
        var k = YeniKiraci("UX1B reyon denetim");
        var id = await ReyonAc(k);

        await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/reyonlar/{id}", new { ad = "İzli ad" });

        using var db = f.Vt.OlusturSistem();
        var kayitlar = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikTuru == "Reyon" && d.VarlikId == id)
            .ToListAsync();

        var yazma = Assert.Single(kayitlar, d => d.Detay != null && d.Detay.Contains("değişen alanlar"));
        Assert.Contains("ad", yazma.Detay!);
        Assert.Equal(k.KullaniciId, yazma.KullaniciId);
    }
}
