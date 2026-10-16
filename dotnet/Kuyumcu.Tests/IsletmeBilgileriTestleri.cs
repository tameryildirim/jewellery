using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// İŞLETME KİMLİK BİLGİLERİ — paket UX-1 Faz B [HTTP, gerçek MySQL]
//
// Kayıt sırasında girilen işletme adı BİR DAHA DEĞİŞTİRİLEMİYORDU; adres,
// vergi dairesi, vergi no ve telefon ise hiç tutulmuyordu. Bu bilgiler fatura
// ve gider pusulasında SATICI kimliğidir (tek kaynak: tenants satırı).
//
// Sözleşme:
//   • PATCH /admin/tenant/kimlik — SAHIP (sınıf düzeyinde [RolGerekli])
//   • Üç durum: alan YOK → korunur · null/boş → silinir · dolu → yazılır
//   • TOPLU ATAMA YOK (Mehmet K2): plan · aiEtkin · aktif · arayüz modu
//     bu gövdeden ASLA atanmaz
//   • Kimlik değişimi denetim izine yazılır (Mehmet K3)
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class IsletmeBilgileriTestleri(HttpFikstur f)
{
    private const string Yol = "/api/v1/admin/tenant/kimlik";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private async Task<Tenant> TenantOku(string tenantId)
    {
        using var db = f.Vt.OlusturSistem();
        return await db.Tenants.AsNoTracking().SingleAsync(t => t.Id == tenantId);
    }

    private Task<HttpResponseMessage> Kimlik(Kiraci k, object govde, string rol = "SAHIP")
        => f.Ortam.Istemci(k, rol).PatchAsJsonAsync(Yol, govde);

    /// <summary>Temel yol: ad ve vergi no girilir, GET aynısını döner.</summary>
    [Fact]
    public async Task PATCH_isletme_adini_ve_vergi_nosunu_gunceller()
    {
        var k = YeniKiraci("UX1B işletme temel");

        var cevap = await Kimlik(k, new
        {
            ad = "Akkoç Kuyumculuk A.Ş.",
            adres = "Kuyumcukent 1. Kat No:12 İstanbul",
            vergiDairesi = "Bayrampaşa",
            vergiNo = "1234567890",
            telefon = "02125550101",
        });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var satir = await TenantOku(k.TenantId);
        Assert.Equal("Akkoç Kuyumculuk A.Ş.", satir.Ad);
        Assert.Equal("Kuyumcukent 1. Kat No:12 İstanbul", satir.Adres);
        Assert.Equal("Bayrampaşa", satir.VergiDairesi);
        Assert.Equal("1234567890", satir.VergiNo);
        Assert.Equal("02125550101", satir.Telefon);

        var getir = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/admin/tenant");
        var govde = await getir.Json();
        Assert.Equal("1234567890", govde.GetProperty("vergiNo").GetString());
        Assert.Equal("Bayrampaşa", govde.GetProperty("vergiDairesi").GetString());
    }

    /// <summary>M-UX1-04 hedefi — gövdede OLMAYAN alan KORUNUR (her alan için ayrı satır).</summary>
    [Theory]
    [InlineData("adres")]
    [InlineData("vergiDairesi")]
    [InlineData("vergiNo")]
    [InlineData("telefon")]
    public async Task Alan_govdede_yoksa_korunur(string alan)
    {
        var k = YeniKiraci($"UX1B koru {alan}");
        await Kimlik(k, new
        {
            adres = "Eski adres", vergiDairesi = "Eski daire",
            vergiNo = "11111111110", telefon = "05551112233",
        });

        // Yalnız SINANAN alan gönderilir; diğer üçü gövdede HİÇ yoktur.
        object govde = alan switch
        {
            "adres" => new { adres = "Yeni adres" },
            "vergiDairesi" => new { vergiDairesi = "Yeni daire" },
            "vergiNo" => new { vergiNo = "2222222222" },
            _ => new { telefon = "05559998877" },
        };
        var cevap = await Kimlik(k, govde);
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        var s = await TenantOku(k.TenantId);
        Assert.Equal(alan == "adres" ? "Yeni adres" : "Eski adres", s.Adres);
        Assert.Equal(alan == "vergiDairesi" ? "Yeni daire" : "Eski daire", s.VergiDairesi);
        Assert.Equal(alan == "vergiNo" ? "2222222222" : "11111111110", s.VergiNo);
        Assert.Equal(alan == "telefon" ? "05559998877" : "05551112233", s.Telefon);
        Assert.Equal($"UX1B koru {alan}", s.Ad);        // ad da korunur
    }

    /// <summary>Açıkça null gönderilen alan SİLİNİR (sözleşmenin ikinci yarısı).</summary>
    [Fact]
    public async Task Null_gonderilen_alan_silinir()
    {
        var k = YeniKiraci("UX1B sil");
        await Kimlik(k, new { adres = "Silinecek", vergiDairesi = "Kalacak", telefon = "05551112233" });

        var cevap = await Kimlik(k, new { adres = (string?)null });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var s = await TenantOku(k.TenantId);
        Assert.Null(s.Adres);
        Assert.Equal("Kalacak", s.VergiDairesi);
        Assert.Equal("05551112233", s.Telefon);
    }

    /// <summary>Vergi no biçimi sunucuda zorlanır — 9 hane kabul edilmez.</summary>
    [Fact]
    public async Task Gecersiz_vergi_no_400_ve_satir_degismez()
    {
        var k = YeniKiraci("UX1B vergi no");
        await Kimlik(k, new { vergiNo = "1234567890" });

        var cevap = await Kimlik(k, new { vergiNo = "123456789" });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Equal("1234567890", (await TenantOku(k.TenantId)).VergiNo);
    }

    /// <summary>İşletme adı SİLİNEMEZ — belgenin satıcı adıdır.</summary>
    [Fact]
    public async Task Bos_ad_400_ve_ad_degismez()
    {
        var k = YeniKiraci("UX1B boş ad");

        var cevap = await Kimlik(k, new { ad = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Equal("UX1B boş ad", (await TenantOku(k.TenantId)).Ad);
    }

    /// <summary>Rol kapısı: SAHİP dışı hiçbir rol işletme kimliğine dokunamaz.</summary>
    [Theory]
    [InlineData("MUDUR")]
    [InlineData("MUHASEBE")]
    [InlineData("TEZGAHTAR")]
    public async Task SAHIP_disi_rol_403(string rol)
    {
        var k = YeniKiraci($"UX1B rol {rol}");

        var cevap = await Kimlik(k, new { ad = "Ele geçirildi", vergiNo = "9999999999" }, rol);

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        var s = await TenantOku(k.TenantId);
        Assert.Equal($"UX1B rol {rol}", s.Ad);
        Assert.Null(s.VergiNo);
    }

    /// <summary>Mehmet K2 — TOPLU ATAMA YOK: plan/AI/aktif bu gövdeden atanmaz.</summary>
    [Fact]
    public async Task Plan_ve_AI_govdeden_ATANMAZ()
    {
        var k = YeniKiraci("UX1B toplu atama");
        var once = await TenantOku(k.TenantId);

        var cevap = await Kimlik(k, new
        {
            ad = "Kimlik değişti",
            plan = "KURUMSAL", aiEtkin = true, aktif = false,
            varsayilanArayuzModu = "BASIT", kurKaynagi = "TRUNCGIL", maliyetYontemi = "LIFO",
        });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var sonra = await TenantOku(k.TenantId);
        Assert.Equal("Kimlik değişti", sonra.Ad);          // yalnız kimlik alanı değişti
        Assert.Equal(once.Plan, sonra.Plan);
        Assert.Equal(once.AiEtkin, sonra.AiEtkin);
        Assert.Equal(once.Aktif, sonra.Aktif);
        Assert.Equal(once.VarsayilanArayuzModu, sonra.VarsayilanArayuzModu);
        Assert.Equal(once.KurKaynagi, sonra.KurKaynagi);
        Assert.Equal(once.MaliyetYontemi, sonra.MaliyetYontemi);
    }

    /// <summary>Kiracı izolasyonu: A'nın isteği B'nin kimliğine dokunmaz.</summary>
    [Fact]
    public async Task Kiraci_yalnizca_KENDI_kimligini_degistirir()
    {
        var a = YeniKiraci("UX1B izolasyon A");
        var b = YeniKiraci("UX1B izolasyon B");

        await Kimlik(a, new { ad = "A yeni ad", vergiNo = "1010101010" });

        var bSatir = await TenantOku(b.TenantId);
        Assert.Equal("UX1B izolasyon B", bSatir.Ad);
        Assert.Null(bSatir.VergiNo);
    }

    /// <summary>Mehmet K3 — kimlik değişimi denetim izine ALAN ADLARIYLA yazılır.</summary>
    [Fact]
    public async Task Kimlik_degisimi_DENETIM_KAYDI_yazar()
    {
        var k = YeniKiraci("UX1B denetim");

        await Kimlik(k, new { vergiNo = "1234567890", telefon = "02120000000" });

        using var db = f.Vt.OlusturSistem();
        var kayit = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikTuru == "TenantKimlik")
            .SingleAsync();

        Assert.Equal(k.KullaniciId, kayit.KullaniciId);
        Assert.Contains("vergi_no", kayit.Detay!);
        Assert.Contains("telefon", kayit.Detay!);
        Assert.DoesNotContain("1234567890", kayit.Detay!);   // DEĞER yazılmaz
        Assert.True(kayit.Hassas);
    }

    /// <summary>Hiçbir alan değişmediyse gereksiz denetim satırı üretilmez.</summary>
    [Fact]
    public async Task Ayni_degerle_PATCH_denetim_kaydi_URETMEZ()
    {
        var k = YeniKiraci("UX1B idempotent");
        await Kimlik(k, new { telefon = "02121111111" });

        await Kimlik(k, new { telefon = "02121111111" });

        using var db = f.Vt.OlusturSistem();
        var adet = await db.DenetimKayitlari.AsNoTracking()
            .CountAsync(d => d.TenantId == k.TenantId && d.VarlikTuru == "TenantKimlik");
        Assert.Equal(1, adet);
    }
}
