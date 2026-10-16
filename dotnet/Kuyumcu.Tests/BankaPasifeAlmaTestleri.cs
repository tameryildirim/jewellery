using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// BANKA HESABI PASİFE ALMA — paket UX-1 Faz B [HTTP, gerçek MySQL]
//
// Banka hesabı açılabiliyordu ama KAPATILAMIYORDU (BankaModulu'nda [HttpDelete]
// hiç yoktu). Mehmet K5 / MÜHÜR 2: finansal varlıkta HARD DELETE YASAK —
// DELETE ucu satırı silmez, `aktif=0` yazar; bakiyesi ya da hareketi olan hesap
// hiç kapanmaz (dekontlar hesaba bağlıdır, hesap kaybolursa mutabakat kırılır).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class BankaPasifeAlmaTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static string Iban() => TestIban.Uret();

    private async Task<string> HesapAc(Kiraci k)
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/bankalar", new
        {
            bankaAdi = "Ziraat", hesapAdi = "Ticari hesap", iban = Iban(), paraBirimi = "TRY",
        });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        return (await cevap.Json()).GetProperty("id").GetString()!;
    }

    private async Task<BankaHesap> HesapOku(string id)
    {
        using var db = f.Vt.OlusturSistem();
        return await db.BankaHesaplari.AsNoTracking().SingleAsync(h => h.Id == id);
    }

    /// <summary>Temiz hesap kapanır — SATIR SİLİNMEZ, aktif=0 olur.</summary>
    [Fact]
    public async Task Temiz_hesap_DELETE_ile_PASIFE_ALINIR_satir_DURUR()
    {
        var k = YeniKiraci("UX1B banka temiz");
        var id = await HesapAc(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").DeleteAsync($"/api/v1/bankalar/{id}");

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.False((await cevap.Json()).GetProperty("aktif").GetBoolean());
        var satir = await HesapOku(id);          // satır DURUYOR (hard delete yok)
        Assert.False(satir.Aktif);
        Assert.Equal("Ziraat", satir.BankaAdi);
    }

    /// <summary>UX-S25 geri dönüş yolu: pasif hesap PATCH ile geri açılır.</summary>
    [Fact]
    public async Task Pasif_hesap_PATCH_ile_GERI_ACILIR()
    {
        var k = YeniKiraci("UX1B banka geri aç");
        var id = await HesapAc(k);
        await f.Ortam.Istemci(k, "SAHIP").DeleteAsync($"/api/v1/bankalar/{id}");

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/bankalar/{id}", new { aktif = true });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.True((await HesapOku(id)).Aktif);
    }

    /// <summary>Bakiyesi olan hesap kapatılamaz — 409 ve satır AKTİF kalır.</summary>
    [Fact]
    public async Task Bakiyeli_hesap_DELETE_409()
    {
        var k = YeniKiraci("UX1B banka bakiyeli");
        var id = await HesapAc(k);
        using (var db = f.Vt.Olustur(k))
        {
            var h = await db.BankaHesaplari.SingleAsync(x => x.Id == id);
            h.Bakiye = 1_250m;
            await db.SaveChangesAsync();
        }

        var cevap = await f.Ortam.Istemci(k, "SAHIP").DeleteAsync($"/api/v1/bankalar/{id}");

        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
        var mesaj = (await cevap.Json()).GetProperty("message").GetString()!;
        Assert.Contains("Bakiyesi", mesaj);
        Assert.True((await HesapOku(id)).Aktif);
    }

    /// <summary>Hareketi olan hesap bakiyesi 0 olsa da kapatılamaz (dekont bağı).</summary>
    [Fact]
    public async Task Hareketli_hesap_DELETE_409()
    {
        var k = YeniKiraci("UX1B banka hareketli");
        var id = await HesapAc(k);
        using (var db = f.Vt.Olustur(k))
        {
            db.BankaHareketleri.Add(new BankaHareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, BankaHesapId = id,
                Yon = "GIRIS", Tutar = 0m, ParaBirimi = "TRY", Durum = "BEKLIYOR",
                Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var cevap = await f.Ortam.Istemci(k, "SAHIP").DeleteAsync($"/api/v1/bankalar/{id}");

        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
        var satir = await HesapOku(id);
        Assert.True(satir.Aktif);
        Assert.Equal(0m, satir.Bakiye);
    }

    /// <summary>Aynı kapı PATCH { aktif:false } yolundan da geçer — ikinci kural yok.</summary>
    [Fact]
    public async Task PATCH_aktif_false_de_AYNI_kapiya_tabidir()
    {
        var k = YeniKiraci("UX1B banka patch kapı");
        var id = await HesapAc(k);
        using (var db = f.Vt.Olustur(k))
        {
            var h = await db.BankaHesaplari.SingleAsync(x => x.Id == id);
            h.Bakiye = 500m;
            await db.SaveChangesAsync();
        }

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/bankalar/{id}", new { aktif = false });

        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
        Assert.True((await HesapOku(id)).Aktif);
    }

    /// <summary>Kiracı izolasyonu: yabancı kiracının hesabı YOK sayılır.</summary>
    [Fact]
    public async Task Yabanci_kiracinin_hesabi_404()
    {
        var a = YeniKiraci("UX1B banka A");
        var b = YeniKiraci("UX1B banka B");
        var id = await HesapAc(a);

        var cevap = await f.Ortam.Istemci(b, "SAHIP").DeleteAsync($"/api/v1/bankalar/{id}");

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        Assert.True((await HesapOku(id)).Aktif);
    }

    /// <summary>Rol kapısı Kasa DELETE ile simetrik: yalnız SAHİP.</summary>
    [Theory]
    [InlineData("MUDUR")]
    [InlineData("MUHASEBE")]
    [InlineData("TEZGAHTAR")]
    public async Task SAHIP_disi_rol_hesabi_KAPATAMAZ_403(string rol)
    {
        var k = YeniKiraci($"UX1B banka rol {rol}");
        var id = await HesapAc(k);

        var cevap = await f.Ortam.Istemci(k, rol).DeleteAsync($"/api/v1/bankalar/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        Assert.True((await HesapOku(id)).Aktif);
    }

    /// <summary>Mehmet (UX-1 Faz B): pasife alma denetim izine yazar.</summary>
    [Fact]
    public async Task Pasife_alma_DENETIM_KAYDI_yazar()
    {
        var k = YeniKiraci("UX1B banka denetim");
        var id = await HesapAc(k);

        await f.Ortam.Istemci(k, "SAHIP").DeleteAsync($"/api/v1/bankalar/{id}");

        using var db = f.Vt.OlusturSistem();
        var kayit = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikTuru == "BankaHesap" && d.VarlikId == id
                        && d.Detay != null && d.Detay.Contains("pasife alındı"))
            .SingleAsync();
        Assert.Equal(k.KullaniciId, kayit.KullaniciId);
    }
}
