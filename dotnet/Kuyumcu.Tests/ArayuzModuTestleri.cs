using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Kullanici;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ARAYÜZ MODU — paket UX-3 Faz A
//
// Üç katman ayrı ayrı sınanır:
//   1) SAF     : ArayuzModu.Coz — tek doğruluk kaynağı (M-UX3-02)
//   2) DB      : migration sonrası davranış DEĞİŞMEDİ (arayuz_modu NULL,
//                varsayilan_arayuz_modu = PROFESYONEL)
//   3) HTTP    : her PATCH ucu için ÜÇ DURUM (alan yok / null / değer),
//                geçersiz değer 400, hiyerarşi 403, kur kaynağı rol kapısı
//
// ⚠ DataAnnotations YALNIZ controller yolunda koşar → "geçersiz değer 400"
//   yalnız HTTP testi olabilir (Aşkın'ın çizgisi).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class ArayuzModuCozTestleri
{
    [Fact]
    public void Varsayilan_profesyoneldir_kullanici_tercihi_yoksa()
    {
        // M-UX3-02 hedefi: null kullanıcı modu BASIT'e çözülürse bu test kırmızı.
        Assert.Equal("PROFESYONEL", ArayuzModu.Coz(null, "PROFESYONEL"));
    }

    [Fact]
    public void Kullanici_tercihi_kiraci_varsayilanini_EZER()
    {
        Assert.Equal("BASIT", ArayuzModu.Coz("BASIT", "PROFESYONEL"));
        Assert.Equal("PROFESYONEL", ArayuzModu.Coz("PROFESYONEL", "BASIT"));
    }

    [Fact]
    public void Tercihi_olmayan_kullanici_kiraci_varsayilanini_DEVRALIR()
    {
        Assert.Equal("BASIT", ArayuzModu.Coz(null, "BASIT"));
    }

    [Fact]
    public void Bilinmeyen_deger_PROFESYONELE_duser_sade_yuze_DUSMEZ()
    {
        // Bozuk/eski veri gizli alanla karşılanmaz: bilinmeyen durumda
        // kullanıcıdan alan GİZLEMEK, o alanın hiç doldurulmaması riskidir.
        Assert.Equal("PROFESYONEL", ArayuzModu.Coz("basit", "sacma"));
        Assert.Equal("PROFESYONEL", ArayuzModu.Coz(null, null));
        Assert.Equal("PROFESYONEL", ArayuzModu.Coz("", ""));
    }

    [Fact]
    public void Gecerli_TAM_eslesmedir_kucuk_harf_kabul_edilmez()
    {
        Assert.True(ArayuzModu.Gecerli("BASIT"));
        Assert.True(ArayuzModu.Gecerli("PROFESYONEL"));
        Assert.False(ArayuzModu.Gecerli("basit"));
        Assert.False(ArayuzModu.Gecerli("Basit"));
        Assert.False(ArayuzModu.Gecerli("X"));
        Assert.False(ArayuzModu.Gecerli(null));
    }
}

[Collection("veritabani")]
public sealed class ArayuzModuVeritabaniTestleri(TestVeritabani vt)
{
    [Fact]
    public void Migration_sonrasi_MEVCUT_davranis_DEGISMEZ()
    {
        // M-UX3-02 hedefi: çözümleyici null'ı BASIT'e çevirirse bu test de kırmızı.
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "UX3 migration");

        var kullanici = db.Kullanicilar.AsNoTracking().Single(x => x.Id == k.KullaniciId);
        var tenant = db.Tenants.AsNoTracking().Single(t => t.Id == k.TenantId);

        Assert.Null(kullanici.ArayuzModu);                       // hiç kimse tercih belirtmedi
        Assert.Equal("PROFESYONEL", tenant.VarsayilanArayuzModu); // kiracı varsayılanı
        Assert.Equal("PROFESYONEL",
            ArayuzModu.Coz(kullanici.ArayuzModu, tenant.VarsayilanArayuzModu));
    }

    [Fact]
    public async Task Servis_govdede_alan_YOKSA_mevcut_degeri_KORUR()
    {
        // M-UX3-03 hedefi: "gönderildi mi" bayrağı kaldırılırsa burası kırmızı.
        using var db = vt.Olustur(KiraciHazirla(vt, "UX3 koru", out var k));
        var servis = new KullaniciServisi(db, TestKurulum.Denetim(db));

        // ⚠ Hiyerarşi kapısı SAHIP.in SAHIP üzerinde işlem yapmasını engeller →
        // kendi kaydı için /ben yolu kullanılır (sözleşme aynı: yok/null/değer).
        await servis.KendinGuncelle(new KullaniciGuncelleDto { ArayuzModu = "BASIT" }, Ust(k));
        await servis.KendinGuncelle(new KullaniciGuncelleDto { Ad = "Yeni" }, Ust(k));

        using var oku = vt.OlusturSistem();
        var satir = oku.Kullanicilar.AsNoTracking().Single(x => x.Id == k.KullaniciId);
        Assert.Equal("BASIT", satir.ArayuzModu);
        Assert.Equal("Yeni", satir.Ad);
    }

    [Fact]
    public async Task Servis_ACIK_null_ile_DEVRALMAYA_doner()
    {
        using var db = vt.Olustur(KiraciHazirla(vt, "UX3 devral", out var k));
        var servis = new KullaniciServisi(db, TestKurulum.Denetim(db));

        await servis.KendinGuncelle(new KullaniciGuncelleDto { ArayuzModu = "BASIT" }, Ust(k));
        await servis.KendinGuncelle(new KullaniciGuncelleDto { ArayuzModu = null }, Ust(k));

        using var oku = vt.OlusturSistem();
        Assert.Null(oku.Kullanicilar.AsNoTracking().Single(x => x.Id == k.KullaniciId).ArayuzModu);
    }

    [Fact]
    public async Task Servis_gecersiz_degeri_REDDEDER_satir_degismez()
    {
        using var db = vt.Olustur(KiraciHazirla(vt, "UX3 gecersiz", out var k));
        var servis = new KullaniciServisi(db, TestKurulum.Denetim(db));

        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() =>
            servis.KendinGuncelle(new KullaniciGuncelleDto { ArayuzModu = "basit" }, Ust(k)));

        using var oku = vt.OlusturSistem();
        Assert.Null(oku.Kullanicilar.AsNoTracking().Single(x => x.Id == k.KullaniciId).ArayuzModu);
    }

    [Fact]
    public async Task Yeni_kullanici_olustururken_mod_verilebilir_ve_bos_DEVRALIR()
    {
        // Bertan KOŞUL 8: oluşturma yolunda alan yoksa sahip ikinci bir adım yapardı.
        using var db = vt.Olustur(KiraciHazirla(vt, "UX3 olustur", out var k));
        var servis = new KullaniciServisi(db, TestKurulum.Denetim(db));

        var basit = await servis.Olustur(new KullaniciOlusturDto
        {
            Ad = "Tez", Soyad = "Gahtar", Email = $"t{Kimlik.Ulid()[^10..]}@ux3.local",
            Sifre = "Parola123", Rol = "TEZGAHTAR", ArayuzModu = "BASIT",
        }, Ust(k));

        var devral = await servis.Olustur(new KullaniciOlusturDto
        {
            Ad = "Tez", Soyad = "Gahtar", Email = $"d{Kimlik.Ulid()[^10..]}@ux3.local",
            Sifre = "Parola123", Rol = "TEZGAHTAR",
        }, Ust(k));

        Assert.Equal("BASIT", Alan(basit, "arayuzModu"));
        Assert.Null(Alan(devral, "arayuzModu"));
    }

    // ── yardımcılar ──────────────────────────────────────────────────────────

    private static Kiraci KiraciHazirla(TestVeritabani vt, string ad, out Kiraci k)
    {
        using var kurulum = vt.OlusturSistem();
        k = TestKurulum.KiraciKur(kurulum, ad);
        return k;
    }

    private static AktifKullanici Ust(Kiraci k) => k.Islem;   // SAHIP

    private static string? Alan(object cevap, string ad)
    {
        var oge = JsonDocument.Parse(JsonSerializer.Serialize(cevap)).RootElement;
        return oge.GetProperty(ad).ValueKind == JsonValueKind.Null
            ? null : oge.GetProperty(ad).GetString();
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HTTP — sözleşme camelCase; üç durum her PATCH ucu için ayrı ayrı sınanır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class HttpArayuzModuTestleri(HttpFikstur f)
{
    private const string Ben = "/api/v1/kullanicilar/ben";
    private const string AuthBen = "/api/v1/auth/ben";
    private const string AdminTenant = "/api/v1/admin/tenant";

    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static async Task<HttpResponseMessage> Patch(HttpClient c, string yol, object govde)
        => await c.PatchAsync(yol, JsonContent.Create(govde));

    /// <summary>Ham JSON gövdeli PATCH — "alan hiç yok" ve "alan null" ayrımı için.</summary>
    private static async Task<HttpResponseMessage> PatchHam(HttpClient c, string yol, string json)
        => await c.PatchAsync(yol, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

    private static async Task<string?> Metin(HttpResponseMessage r, string alan)
    {
        var g = await r.Json();
        return g.TryGetProperty(alan, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;
    }

    // ── /auth/ben sözleşmesi ────────────────────────────────────────────────

    [Fact]
    public async Task Auth_ben_arayuzModu_ve_etkinArayuzModu_doner()
    {
        var k = Kur("UX3 http ben");
        var cevap = await f.Ortam.Istemci(k).GetAsync(AuthBen);

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var g = await cevap.Json();
        Assert.Equal(JsonValueKind.Null, g.GetProperty("arayuzModu").ValueKind);
        Assert.Equal("PROFESYONEL", g.GetProperty("etkinArayuzModu").GetString());
    }

    [Fact]
    public async Task Mod_BASIT_yapilinca_auth_ben_BASIT_doner()
    {
        var k = Kur("UX3 http basit");
        var c = f.Ortam.Istemci(k);

        Assert.Equal(HttpStatusCode.OK, (await Patch(c, Ben, new { arayuzModu = "BASIT" })).StatusCode);

        var g = await (await c.GetAsync(AuthBen)).Json();
        Assert.Equal("BASIT", g.GetProperty("arayuzModu").GetString());
        Assert.Equal("BASIT", g.GetProperty("etkinArayuzModu").GetString());
    }

    [Fact]
    public async Task Kiraci_varsayilani_BASIT_olunca_tercihsiz_kullanici_BASIT_gorur()
    {
        var k = Kur("UX3 http kiraci");
        var c = f.Ortam.Istemci(k);

        var patch = await Patch(c, AdminTenant, new { varsayilanArayuzModu = "BASIT" });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        Assert.Equal("BASIT", await Metin(patch, "varsayilanArayuzModu"));

        var g = await (await c.GetAsync(AuthBen)).Json();
        // Kullanıcı satırı HÂLÂ null — davranış çözümleyiciden gelir.
        Assert.Equal(JsonValueKind.Null, g.GetProperty("arayuzModu").ValueKind);
        Assert.Equal("BASIT", g.GetProperty("etkinArayuzModu").GetString());

        using var db = f.Vt.OlusturSistem();
        Assert.Null(db.Kullanicilar.AsNoTracking().Single(x => x.Id == k.KullaniciId).ArayuzModu);
    }

    // ── ÜÇ DURUM: PATCH /kullanicilar/ben ───────────────────────────────────

    [Fact]
    public async Task Ben_uc_durum_yok_null_deger()
    {
        var k = Kur("UX3 http uc durum ben");
        var c = f.Ortam.Istemci(k);

        // (1) DEĞER
        Assert.Equal("BASIT", await Metin(await Patch(c, Ben, new { arayuzModu = "BASIT" }), "arayuzModu"));

        // (2) ALAN YOK → korunur
        Assert.Equal("BASIT", await Metin(await PatchHam(c, Ben, """{"ad":"Deneme"}"""), "arayuzModu"));

        // (3) AÇIK null → devral
        var devral = await PatchHam(c, Ben, """{"arayuzModu":null}""");
        Assert.Equal(JsonValueKind.Null, (await devral.Json()).GetProperty("arayuzModu").ValueKind);
    }

    [Fact]
    public async Task Ben_gecersiz_deger_400_ve_satir_DEGISMEZ()
    {
        var k = Kur("UX3 http gecersiz ben");
        var c = f.Ortam.Istemci(k);
        await Patch(c, Ben, new { arayuzModu = "BASIT" });

        var cevap = await Patch(c, Ben, new { arayuzModu = "basit" });
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.Equal("BASIT", db.Kullanicilar.AsNoTracking().Single(x => x.Id == k.KullaniciId).ArayuzModu);
    }

    // ── ÜÇ DURUM: PATCH /kullanicilar/{id} ──────────────────────────────────

    [Fact]
    public async Task Sahip_calisaninin_modunu_degistirir_uc_durum()
    {
        var k = Kur("UX3 http baskasi");
        var c = f.Ortam.Istemci(k);

        var olustur = await c.PostJson("/api/v1/kullanicilar", new
        {
            ad = "Tez", soyad = "Gahtar", email = $"t{Kimlik.Ulid()[^10..]}@ux3.local",
            sifre = "Parola123", rol = "TEZGAHTAR",
        });
        Assert.Equal(HttpStatusCode.Created, olustur.StatusCode);
        var hedefId = (await olustur.Json()).GetProperty("id").GetString()!;
        var yol = $"/api/v1/kullanicilar/{hedefId}";

        Assert.Equal("BASIT", await Metin(await Patch(c, yol, new { arayuzModu = "BASIT" }), "arayuzModu"));
        Assert.Equal("BASIT", await Metin(await PatchHam(c, yol, """{"ad":"Tezgah"}"""), "arayuzModu"));
        Assert.Equal(JsonValueKind.Null,
            (await (await PatchHam(c, yol, """{"arayuzModu":null}""")).Json()).GetProperty("arayuzModu").ValueKind);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await Patch(c, yol, new { arayuzModu = "X" })).StatusCode);
    }

    [Fact]
    public async Task TEZGAHTAR_baskasinin_modunu_DEGISTIREMEZ()
    {
        var k = Kur("UX3 http tezgahtar");
        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");

        var cevap = await Patch(tezgahtar, $"/api/v1/kullanicilar/{k.KullaniciId}",
            new { arayuzModu = "BASIT" });

        // Uç zaten SAHIP/MUDUR'a kapalı — mod alanı bu kapıyı gevşetmez.
        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    // ── ÜÇ DURUM: PATCH /admin/tenant ───────────────────────────────────────

    [Fact]
    public async Task Admin_tenant_uc_durum_ve_rol_kapisi()
    {
        var k = Kur("UX3 http tenant");
        var sahip = f.Ortam.Istemci(k);

        // (1) DEĞER
        Assert.Equal("BASIT",
            await Metin(await Patch(sahip, AdminTenant, new { varsayilanArayuzModu = "BASIT" }),
                        "varsayilanArayuzModu"));

        // (2) ALAN YOK → [Required] 400 (kiracı varsayılanı sessizce sıfırlanmaz)
        Assert.Equal(HttpStatusCode.BadRequest, (await PatchHam(sahip, AdminTenant, "{}")).StatusCode);

        // (3) AÇIK null → 400 (NOT NULL kolon; "devral" kavramı kiracıda YOKTUR)
        Assert.Equal(HttpStatusCode.BadRequest,
            (await PatchHam(sahip, AdminTenant, """{"varsayilanArayuzModu":null}""")).StatusCode);

        // Geçersiz değer
        Assert.Equal(HttpStatusCode.BadRequest,
            (await Patch(sahip, AdminTenant, new { varsayilanArayuzModu = "basit" })).StatusCode);

        // Rol kapısı sınıf düzeyinde: MUDUR bile giremez
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Patch(f.Ortam.Istemci(k, "MUDUR"), AdminTenant,
                new { varsayilanArayuzModu = "BASIT" })).StatusCode);

        // Değer ilk PATCH'ten sonra BASIT kalmış olmalı (başarısızlar yazmadı)
        using var db = f.Vt.OlusturSistem();
        Assert.Equal("BASIT", db.Tenants.AsNoTracking().Single(t => t.Id == k.TenantId).VarsayilanArayuzModu);
    }

    // ── Mehmet K3: kur kaynağı ucu rol kapısı ───────────────────────────────

    // KESIF-P6 (ekip oyu Mehmet+Taşkın, 30.09 18:10): kur kaynağı YALNIZ SAHİP — MÜDÜR de 403.
    [Fact]
    public async Task Kur_kaynagi_TEZGAHTAR_ve_MUDURE_403_SAHIPE_aciktir()
    {
        var k = Kur("UX3 http kur kaynak");

        var tezgahtar = await Patch(f.Ortam.Istemci(k, "TEZGAHTAR"),
            "/api/v1/kur/kaynak", new { kaynak = "MANUAL" });
        Assert.Equal(HttpStatusCode.Forbidden, tezgahtar.StatusCode);

        var mudur = await Patch(f.Ortam.Istemci(k, "MUDUR"),
            "/api/v1/kur/kaynak", new { kaynak = "MANUAL" });
        Assert.Equal(HttpStatusCode.Forbidden, mudur.StatusCode);

        var sahip = await Patch(f.Ortam.Istemci(k, "SAHIP"),
            "/api/v1/kur/kaynak", new { kaynak = "MANUAL" });
        Assert.NotEqual(HttpStatusCode.Forbidden, sahip.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, sahip.StatusCode);
    }

    // ── Mehmet K6: mod JWT claim'ine KONMAZ ─────────────────────────────────

    [Fact]
    public async Task Mod_degisimi_YENIDEN_GIRIS_gerektirmez_claimde_tasinmaz()
    {
        var k = Kur("UX3 http claim");
        var c = f.Ortam.Istemci(k);   // token mod DEĞİŞMEDEN ÖNCE üretildi

        await Patch(c, Ben, new { arayuzModu = "BASIT" });

        // AYNI token ile okunan /auth/ben yeni modu görür → değer claim'de değil.
        var g = await (await c.GetAsync(AuthBen)).Json();
        Assert.Equal("BASIT", g.GetProperty("etkinArayuzModu").GetString());
    }
}
