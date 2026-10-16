using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Destek;
using Kuyumcu.Api.Modules.Telemetri;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-31 (R-5 · T-4b) — MASKELİ DESTEK PAKETİ UÇLARI (HTTP + gerçek MySQL).
//
// Sınanan sözleşme:
//   • BK-17: kanal yapılandırmayla açılmadıkça paket ucu 503 (fail-closed)
//   • gerekçe < 20 → 400 · geçersiz konu → 400 · serbest metin hata kodu → 400
//   • yalnız SAHIP (MUDUR/TEZGAHTAR 403)
//   • paket = allow-list JSON; cari adı, gerekçe metni, TCKN/telefon YOK
//   • paket üretimi denetim izine düşer ve P-28 destek dakikası tablosuna
//     kaynak=TANILAMA, PASİF, 0 dk satır açar (rakam uydurulmaz)
//   • denetimden geçmeyen paket VERİLMEZ (500) ve ret izi düşer
//   • saatlik tavan (3) → 429 · paket kaydı değişmez (PATCH/DELETE ucu yok)
//   • istemci hata kodu: kodu SUNUCU üretir, yalnız tür adı kabul edilir
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class P31DestekPaketiTestleri(HttpFikstur f)
{
    private const string Yol = "/api/v1/destek/paket";
    private const string UzunGerekce = "Satış ekranı açılırken hata veriyor, destek istiyorum.";

    private static readonly object Kilit = new();
    private static WebApplicationFactory<Program>? acikFabrika;

    /// <summary>Kanal AÇIK yapılandırmayla ikinci sunucu (süreç başına bir kez).</summary>
    private WebApplicationFactory<Program> Acik()
    {
        lock (Kilit)
        {
            return acikFabrika ??= f.Ortam.WithWebHostBuilder(b =>
                b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(
                    new Dictionary<string, string?> { [DestekAyarlari.KanalAcikAnahtari] = "true" })));
        }
    }

    private HttpClient AcikIstemci(Kiraci k, string rol = "SAHIP")
    {
        var token = new TokenServisi(new JwtAyarlar
        {
            Secret = HttpOrtami.TestSirri, AccessDakika = 15, RefreshGun = 7,
        }).AccessUret(k.KullaniciId, k.TenantId, k.SubeId, rol);
        var c = Acik().CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static object Govde(string? gerekce = UzunGerekce, string? konu = "HATA", string? kod = null)
        => new { gerekce, konu, hataKodu = kod };

    // ── BK-17 kanal kapısı ───────────────────────────────────────────────────

    [Fact]
    public async Task Kanal_yapilandirmayla_acilmadikca_paket_503_ve_iz_yok()
    {
        var k = YeniKiraci("P31 kanal kapalı");
        var r = await f.Ortam.Istemci(k).PostJson(Yol, Govde());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, r.StatusCode);
        var json = await r.Json();
        Assert.Contains("güvenlik doğrulaması", json.GetProperty("message").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("istekKimligi").GetString()));

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, await db.DestekDakikalari.CountAsync(d => d.TenantId == k.TenantId));
        Assert.Equal(0, await db.DenetimKayitlari.CountAsync(d =>
            d.TenantId == k.TenantId && d.VarlikTuru == DestekAyarlari.DenetimVarligi));
    }

    [Fact]
    public async Task Gecmis_ucu_kanal_durumunu_ve_siniri_bildirir()
    {
        var k = YeniKiraci("P31 geçmiş kapalı");
        var r = await f.Ortam.Istemci(k).GetAsync(Yol + "/gecmis");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var json = await r.Json();
        Assert.False(json.GetProperty("kanalAcik").GetBoolean());
        Assert.Equal(DestekAyarlari.GecmisSiniri, json.GetProperty("sinir").GetInt32());
        Assert.Equal(20, json.GetProperty("asgariGerekce").GetInt32());
        Assert.Equal(DestekKonulari.Hepsi, json.GetProperty("konular").EnumerateArray().Select(x => x.GetString()).ToArray());
        Assert.Equal(0, json.GetProperty("kayitlar").GetArrayLength());
    }

    // ── Doğrulama ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Gerekce_20_karakterden_kisa_ise_400()
    {
        var k = YeniKiraci("P31 kısa gerekçe");
        var r = await AcikIstemci(k).PostJson(Yol, Govde(gerekce: "  kısa metin 19 krk "));

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("en az 20", (await r.Json()).GetProperty("message").GetString());
    }

    [Fact]
    public async Task Gecersiz_konu_400()
    {
        var k = YeniKiraci("P31 konu");
        var r = await AcikIstemci(k).PostJson(Yol, Govde(konu: "SIFRE_SIFIRLA"));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Serbest_metin_hata_kodu_reddedilir_400()
    {
        var k = YeniKiraci("P31 hata kodu");
        var r = await AcikIstemci(k).PostJson(Yol, Govde(kod: "Ayse 05321234567"));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("Destek kodu", (await r.Json()).GetProperty("message").GetString());
    }

    [Theory]
    [InlineData("MUDUR")]
    [InlineData("TEZGAHTAR")]
    public async Task SAHIP_disi_rol_403(string rol)
    {
        var k = YeniKiraci("P31 rol " + rol);
        Assert.Equal(HttpStatusCode.Forbidden, (await AcikIstemci(k, rol).PostJson(Yol, Govde())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await AcikIstemci(k, rol).GetAsync(Yol + "/gecmis")).StatusCode);
    }

    // ── Üretim + içerik ──────────────────────────────────────────────────────

    [Fact]
    public async Task Paket_uretilir_kisisel_veri_ve_gerekce_icermez()
    {
        var k = YeniKiraci("P31 icerik Zeynep Kuyumculuk");
        const string gerekce = "Musteri Ayse Yilmaz 05321234567 TC 12345678901 satis hatasi";
        // Denetim özetinde en az bir satır olsun (geçmiş okuma izi düşer).
        Assert.Equal(HttpStatusCode.OK, (await AcikIstemci(k).GetAsync(Yol + "/gecmis")).StatusCode);
        var r = await AcikIstemci(k).PostJson(Yol, Govde(gerekce: gerekce, kod: "01KSTKM1K0000000000000ABCD"));

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("application/json", r.Content.Headers.ContentType?.MediaType);
        var dosya = r.Content.Headers.ContentDisposition?.FileNameStar ?? r.Content.Headers.ContentDisposition?.FileName;
        var kimlik = r.Headers.GetValues(IstekKimligiAlani.Baslik).Single();
        Assert.Equal($"destek-paketi-{kimlik[^8..]}.json", dosya?.Trim('"'));

        var metin = await r.Content.ReadAsStringAsync();
        // Gerekçe (kişisel veri taşıyabilir) ve cari adı pakete GİRMEZ.
        Assert.DoesNotContain("Ayse", metin);
        Assert.DoesNotContain("05321234567", metin);
        Assert.DoesNotContain("12345678901", metin);
        Assert.DoesNotContain("müşterisi", metin);
        Assert.DoesNotContain(k.TenantId, metin);
        Assert.DoesNotContain(k.KullaniciId, metin);
        // Bağımsız denetçi de temiz der (allow-list + metin denetimi).
        Assert.Empty(TanilamaDenetcisi.Tara(metin));

        var p = JsonDocument.Parse(metin).RootElement;
        Assert.Equal(kimlik, p.GetProperty("paketKodu").GetString());
        Assert.Equal("HATA", p.GetProperty("konu").GetString());
        Assert.Equal("0000ABCD", p.GetProperty("hataKodu").GetString());
        Assert.Equal("SAHIP", p.GetProperty("isteyenRol").GetString());
        Assert.Equal(k.TenantId[^6..], p.GetProperty("kiraciKisa").GetString());
        Assert.True(p.GetProperty("surum").GetProperty("migrationSayisi").GetInt32() > 0);
        Assert.True(p.GetProperty("sonDenetim").GetArrayLength() > 0);
        foreach (var s in p.GetProperty("sonDenetim").EnumerateArray())
            Assert.Equal(["tarih", "eylem", "varlikTuru", "kullaniciKisa"],
                s.EnumerateObject().Select(o => o.Name).ToArray());
    }

    [Fact]
    public async Task Paket_denetim_izine_ve_P28_destek_dakikasina_TANILAMA_pasif_0dk_yazilir()
    {
        var k = YeniKiraci("P31 dakika");
        var r = await AcikIstemci(k).PostJson(Yol, Govde(konu: "VERI"));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var kimlik = r.Headers.GetValues(IstekKimligiAlani.Baslik).Single();

        using var db = f.Vt.OlusturSistem();
        var iz = await db.DenetimKayitlari.AsNoTracking().SingleAsync(d =>
            d.TenantId == k.TenantId && d.VarlikTuru == DestekAyarlari.DenetimVarligi
            && d.Eylem == DenetimEylemi.Ekleme);
        Assert.Equal(kimlik, iz.VarlikId);
        Assert.Equal(k.KullaniciId, iz.KullaniciId);
        Assert.Contains("konu: VERI", iz.Detay);
        Assert.Contains("gerekçe: " + UzunGerekce, iz.Detay);

        var dk = await db.DestekDakikalari.AsNoTracking().SingleAsync(d => d.TenantId == k.TenantId);
        Assert.Equal(DestekKaynaklari.Tanilama, dk.Kaynak);
        Assert.False(dk.Aktif);                 // ölçüme SAYILMAZ
        Assert.Equal(0, dk.Dakika);             // rakam uydurulmaz
        Assert.Equal("VERI", dk.Konu);
        Assert.Equal(DestekKanallari.Yazili, dk.Kanal);
        Assert.Contains(kimlik[^8..], dk.Aciklama);
    }

    [Fact]
    public async Task Denetimden_gecmeyen_paket_VERILMEZ_500_ve_ret_izi_duser()
    {
        var k = YeniKiraci("P31 fail-closed");
        // Gelecekte serbest metnin varlık türüne sızdığı bir regresyonu taklit
        // eder: denetim satırında e-posta. Paket bu satırı özete koyar →
        // denetçi yakalamalı ve paket VERİLMEMELİ.
        using (var db = f.Vt.OlusturSistem())
        {
            db.DenetimKayitlari.Add(new DenetimKaydi
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, KullaniciId = k.KullaniciId,
                Eylem = DenetimEylemi.Ekleme, VarlikTuru = "sizinti@ornek.com", Tarih = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var r = await AcikIstemci(k).PostJson(Yol, Govde());
        Assert.Equal(HttpStatusCode.InternalServerError, r.StatusCode);
        var metin = await r.Content.ReadAsStringAsync();
        Assert.DoesNotContain("sizinti@ornek.com", metin);
        Assert.Contains("güvenlik denetiminden geçmedi", (await r.Json()).GetProperty("message").GetString());

        using var db2 = f.Vt.OlusturSistem();
        var red = await db2.DenetimKayitlari.AsNoTracking().SingleAsync(d =>
            d.TenantId == k.TenantId && d.VarlikTuru == DestekAyarlari.DenetimVarligi
            && d.Eylem == DenetimEylemi.Reddedildi);
        Assert.Contains("EPOSTA", red.Detay);
        Assert.DoesNotContain("sizinti@ornek.com", red.Detay);
        Assert.Equal(0, await db2.DestekDakikalari.CountAsync(d => d.TenantId == k.TenantId));
        Assert.Equal(0, await db2.DenetimKayitlari.CountAsync(d =>
            d.TenantId == k.TenantId && d.VarlikTuru == DestekAyarlari.DenetimVarligi
            && d.Eylem == DenetimEylemi.Ekleme));
    }

    [Fact]
    public async Task Saatlik_tavan_asilinca_429()
    {
        var k = YeniKiraci("P31 tavan");
        var c = AcikIstemci(k);
        for (var i = 0; i < DestekAyarlari.SaatlikTavan; i++)
            Assert.Equal(HttpStatusCode.OK, (await c.PostJson(Yol, Govde())).StatusCode);

        var r = await c.PostJson(Yol, Govde());
        Assert.Equal(HttpStatusCode.TooManyRequests, r.StatusCode);
        Assert.Contains("en çok 3", (await r.Json()).GetProperty("message").GetString());
    }

    [Fact]
    public async Task Gecmis_kendi_kiracisinin_paketini_kisa_kod_ve_konu_ile_doner()
    {
        var k = YeniKiraci("P31 geçmiş");
        var baska = YeniKiraci("P31 geçmiş başka");
        var r = await AcikIstemci(k).PostJson(Yol, Govde(konu: "KURULUM"));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var kimlik = r.Headers.GetValues(IstekKimligiAlani.Baslik).Single();

        var g = await (await AcikIstemci(k).GetAsync(Yol + "/gecmis")).Json();
        Assert.True(g.GetProperty("kanalAcik").GetBoolean());
        var satir = Assert.Single(g.GetProperty("kayitlar").EnumerateArray());
        Assert.Equal(kimlik[^8..], satir.GetProperty("paketKodu").GetString());
        Assert.Equal("KURULUM", satir.GetProperty("konu").GetString());
        // Tarih UTC damgalı döner (istemci ToLocalTime ile doğru saati basar).
        Assert.EndsWith("Z", satir.GetProperty("tarih").GetString());
        Assert.False(g.GetProperty("dahaFazlasiVar").GetBoolean());

        // Kiracı izolasyonu: başka kiracı bu paketi GÖRMEZ.
        var gb = await (await AcikIstemci(baska).GetAsync(Yol + "/gecmis")).Json();
        Assert.Equal(0, gb.GetProperty("kayitlar").GetArrayLength());
    }

    [Fact]
    public async Task Paket_kaydi_icin_PATCH_ve_DELETE_ucu_yok()
    {
        var k = YeniKiraci("P31 değişmez");
        var c = AcikIstemci(k);
        var patch = await c.PatchJson(Yol + "/01KSTKM1K0000000000000ABCD", new { konu = "DIGER" });
        var sil = await c.DeleteAsync(Yol + "/01KSTKM1K0000000000000ABCD");
        Assert.Contains(patch.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        Assert.Contains(sil.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
    }

    // ── İstemci hata kodu ────────────────────────────────────────────────────

    [Fact]
    public async Task Istemci_hatasi_kodunu_SUNUCU_uretir_istek_kimligiyle_ayni()
    {
        var k = YeniKiraci("P31 istemci hatası");
        var r = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson("/api/v1/destek/istemci-hatasi",
            new { tur = "JsonException", mesaj = "Ayse Yilmaz 05321234567" });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var baslik = r.Headers.GetValues(IstekKimligiAlani.Baslik).Single();
        var json = await r.Json();
        Assert.Equal(baslik, json.GetProperty("istekKimligi").GetString());
        Assert.Equal(baslik[^8..], json.GetProperty("hataKodu").GetString());
    }

    [Fact]
    public async Task Istemci_hatasi_oturumsuz_401()
    {
        var r = await f.Ortam.AnonimIstemci().PostJson("/api/v1/destek/istemci-hatasi", new { tur = "X" });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    // ── Saf: tür süzgeci ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("JsonException", "JsonException")]
    [InlineData("System.NullReferenceException", "System.NullReferenceException")]
    [InlineData("Ayse Yilmaz", "Bilinmeyen")]
    [InlineData("x@y.com", "Bilinmeyen")]
    [InlineData(null, "Bilinmeyen")]
    public void Istemci_hata_turu_beyaz_karakter_kumesine_suzulur(string? girdi, string beklenen)
        => Assert.Equal(beklenen, DestekPaketiServisi.TurSuz(girdi));
}
