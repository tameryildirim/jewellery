using System.Net;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// HTTP BORU HATTI TESTLERİ — PLAN-2 Adım 0a · Aşkın'ın K2 kapısı.
//
// Bu dosyadaki hiçbir test servis çağırmaz; hepsi GERÇEK HTTP isteği yapar.
// Servis testinin göremediği katman burada sınanır: middleware sırası,
// kimlik doğrulama, rol yetkisi, oran sınırı, JSON sözleşmesi, model bağlama.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class HttpBoruHattiTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    // ── Kimlik doğrulama ─────────────────────────────────────────────────────

    [Fact]
    public async Task Tokensiz_istek_401_doner_ve_govde_sozlesmeye_uyar()
    {
        var cevap = await f.Ortam.AnonimIstemci().GetAsync("/api/v1/cari");

        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);

        var json = await cevap.Json();
        Assert.Equal("Token bulunamadı", json.GetProperty("message").GetString());
        Assert.Equal("Unauthorized", json.GetProperty("error").GetString());
        Assert.Equal(401, json.GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task Bozuk_token_401_doner_ve_farkli_mesaj_verir()
    {
        var cevap = await f.Ortam.BozukTokenIstemcisi().GetAsync("/api/v1/cari");

        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
        // "Token yok" ile "token geçersiz" AYRI mesajlar — sözleşme bunu ayırıyor.
        Assert.Equal("Geçersiz veya süresi dolmuş token",
            (await cevap.Json()).GetProperty("message").GetString());
    }

    [Fact]
    public async Task Gecerli_token_200_doner()
    {
        var k = YeniKiraci("HTTP geçerli token");
        var cevap = await f.Ortam.Istemci(k).GetAsync("/api/v1/cari");

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
    }

    [Fact]
    public async Task Saglik_ucu_kimlik_istemeden_200_doner()
    {
        var cevap = await f.Ortam.AnonimIstemci().GetAsync("/saglik");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
    }

    // ── JSON sözleşmesi ──────────────────────────────────────────────────────

    [Fact]
    public async Task Yanit_camelCase_alan_adlari_kullanir()
    {
        var k = YeniKiraci("HTTP camelCase");
        var cevap = await f.Ortam.Istemci(k).GetAsync("/api/v1/cari");
        var govde = await cevap.Metin();

        // HTTP yolunda ASP.NET camelCase uygular; servis testi bunu göremez
        // (orada nesne doğrudan serileştirilir ve PascalCase kalır).
        Assert.Contains("\"tlBakiye\"", govde);
        Assert.DoesNotContain("\"TlBakiye\"", govde);
    }

    [Fact]
    public async Task Gecersiz_govde_400_ve_Nest_dogrulama_bicimi_doner()
    {
        var cevap = await f.Ortam.AnonimIstemci()
            .PostJson("/api/v1/auth/giris", new { email = "bu-email-degil", sifre = "kisa" });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);

        var json = await cevap.Json();
        Assert.Equal("Bad Request", json.GetProperty("error").GetString());
        Assert.Equal(400, json.GetProperty("statusCode").GetInt32());
        // message bir DİZİ olmalı (Nest ValidationPipe sözleşmesi), tek string değil.
        Assert.Equal(System.Text.Json.JsonValueKind.Array, json.GetProperty("message").ValueKind);
        Assert.NotEmpty(json.GetProperty("message").EnumerateArray());
    }

    // ── Rol yetkisi (0d: denetim izi yalnız SAHİP) ───────────────────────────

    [Fact]
    public async Task Denetim_izini_SAHIP_okuyabilir()
    {
        var k = YeniKiraci("HTTP denetim sahip");
        var cevap = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/denetim");

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.True((await cevap.Json()).TryGetProperty("kayitlar", out _));
    }

    [Theory]
    [InlineData("MUDUR")]
    [InlineData("MUHASEBE")]
    [InlineData("TEZGAHTAR")]
    public async Task Denetim_izini_SAHIP_disindaki_roller_okuyamaz(string rol)
    {
        var k = YeniKiraci($"HTTP denetim {rol}");
        var cevap = await f.Ortam.Istemci(k, rol).GetAsync("/api/v1/denetim");

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    // ── Paket 38: özel matrah önizlemesi MUHASEBE+ ───────────────────────────

    /// <summary>
    /// Mehmet K38-2: önizleme yazma ucu değildir AMA fatura matrahını ifşa eder;
    /// rol kapısı POST /faturalar ile AYNIDIR. TEZGAHTAR göremez.
    /// </summary>
    [Fact]
    public async Task Ozel_matrah_onizlemesini_TEZGAHTAR_goremez()
    {
        var k = YeniKiraci("HTTP özel matrah tezgâhtar");
        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR")
            .GetAsync("/api/v1/faturalar/ozel-matrah-onizleme?hareketId=YOK&toplam=100");

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    // ── PAKET 40-A — risk uçlarının rol kapısı ───────────────────────────────

    [Fact]
    public async Task Risk_profilini_TEZGAHTAR_goremez_MUHASEBE_gorur()
    {
        var k = YeniKiraci("HTTP risk rol");

        var tezgah = await f.Ortam.Istemci(k, "TEZGAHTAR")
            .GetAsync($"/api/v1/masak/risk/{k.CariId}");
        Assert.Equal(HttpStatusCode.Forbidden, tezgah.StatusCode);

        var muhasebe = await f.Ortam.Istemci(k, "MUHASEBE")
            .GetAsync($"/api/v1/masak/risk/{k.CariId}");
        Assert.Equal(HttpStatusCode.OK, muhasebe.StatusCode);
        // Sözleşme: kimlik belge numarası bu uçta HİÇ geçmez (Mehmet K40-3).
        var govde = await muhasebe.Json();
        Assert.False(govde.TryGetProperty("belgeNo", out _));
        Assert.True(govde.TryGetProperty("tarihce", out _));
    }

    [Fact]
    public async Task Elle_seviye_ezmesi_MUHASEBEye_KAPALI_MUDURe_aciktir()
    {
        var k = YeniKiraci("HTTP risk elle rol");
        var govde = new { seviye = "YUKSEK", gerekce = "Yirmi karakterden uzun bir gerekçe metnidir." };

        var muhasebe = await f.Ortam.Istemci(k, "MUHASEBE")
            .PostJson($"/api/v1/masak/risk/{k.CariId}/elle", govde);
        Assert.Equal(HttpStatusCode.Forbidden, muhasebe.StatusCode);

        var mudur = await f.Ortam.Istemci(k, "MUDUR")
            .PostJson($"/api/v1/masak/risk/{k.CariId}/elle", govde);
        Assert.Equal(HttpStatusCode.OK, mudur.StatusCode);
    }

    [Fact]
    public async Task Risk_ozetini_TEZGAHTAR_goremez()
    {
        var k = YeniKiraci("HTTP risk ozet rol");
        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/masak/risk/ozet");

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    [Fact]
    public async Task Pep_beyaninda_gorev_bos_ise_400_doner()
    {
        var k = YeniKiraci("HTTP pep 400");
        var istemci = f.Ortam.Istemci(k, "MUHASEBE");

        var eksik = await istemci.PatchJson(
            $"/api/v1/cari/{k.CariId}/pep", new { pepBeyani = true });
        Assert.Equal(HttpStatusCode.BadRequest, eksik.StatusCode);

        var tam = await istemci.PatchJson(
            $"/api/v1/cari/{k.CariId}/pep", new { pepBeyani = true, pepGorev = "Belediye meclis üyesi" });
        Assert.Equal(HttpStatusCode.OK, tam.StatusCode);
        Assert.True((await tam.Json()).GetProperty("pepBeyani").GetBoolean());
    }

    [Fact]
    public async Task Faturalanabilir_islem_listesini_TEZGAHTAR_goremez_MUHASEBE_gorur()
    {
        var k = YeniKiraci("HTTP faturalanabilir");

        var tezgah = await f.Ortam.Istemci(k, "TEZGAHTAR")
            .GetAsync("/api/v1/faturalar/faturalanabilir-islemler");
        Assert.Equal(HttpStatusCode.Forbidden, tezgah.StatusCode);

        var muhasebe = await f.Ortam.Istemci(k, "MUHASEBE")
            .GetAsync("/api/v1/faturalar/faturalanabilir-islemler");
        Assert.Equal(HttpStatusCode.OK, muhasebe.StatusCode);
        Assert.True((await muhasebe.Json()).TryGetProperty("islemler", out _));
    }

    [Fact]
    public async Task Denetim_izinde_SILME_ucu_yoktur()
    {
        var k = YeniKiraci("HTTP denetim silme");
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var liste = await istemci.GetAsync("/api/v1/denetim");
        var id = (await liste.Json()).GetProperty("kayitlar").EnumerateArray()
            .First().GetProperty("id").GetString();

        var sil = await istemci.DeleteAsync($"/api/v1/denetim/{id}");

        // 404 (yol yok) ya da 405 (yöntem yok) — ama ASLA 200/204.
        Assert.True(sil.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed,
            $"Denetim izinde silme ucu bulundu: {(int)sil.StatusCode}");
    }

    // ── Kiracı izolasyonu, HTTP katmanında (Aşkın K3) ────────────────────────

    [Fact]
    public async Task Bir_kiraci_digerinin_carisini_HTTP_uzerinden_goremez()
    {
        var a = YeniKiraci("HTTP kiracı A");
        var b = YeniKiraci("HTTP kiracı B");

        var cevap = await f.Ortam.Istemci(b).GetAsync($"/api/v1/cari/{a.CariId}");

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
    }

    [Fact]
    public async Task Kiraci_listesi_yalnizca_kendi_carilerini_doner()
    {
        var a = YeniKiraci("HTTP liste A");
        YeniKiraci("HTTP liste B");

        var json = await (await f.Ortam.Istemci(a).GetAsync("/api/v1/cari")).Json();
        var idler = json.EnumerateArray().Select(c => c.GetProperty("id").GetString()).ToList();

        Assert.Contains(a.CariId, idler);
        Assert.Single(idler);   // B'nin carisi görünmemeli
    }

    // ── Okuma izi, HTTP yolunda gerçekten düşüyor mu? (0c kanıtı) ────────────

    [Fact]
    public async Task Cari_karti_HTTP_uzerinden_okununca_denetim_izi_duser()
    {
        var k = YeniKiraci("HTTP cari okuma izi");

        await f.Ortam.Istemci(k).GetAsync($"/api/v1/cari/{k.CariId}");

        using var db = f.Vt.OlusturSistem();
        var iz = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId
                     && d.Eylem == DenetimEylemi.Okuma
                     && d.VarlikTuru == nameof(Cari)
                     && d.VarlikId == k.CariId)
            .ToListAsync();

        Assert.NotEmpty(iz);
        Assert.True(iz[0].Hassas, "Cari okuması HASSAS işaretlenmeli (KVKK md.12)");
        Assert.Contains("görüntülendi", iz[0].Detay);
    }

    [Fact]
    public async Task Cari_listesi_okununca_tek_satir_iz_duser()
    {
        var k = YeniKiraci("HTTP cari liste izi");

        await f.Ortam.Istemci(k).GetAsync("/api/v1/cari");

        using var db = f.Vt.OlusturSistem();
        var iz = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId
                     && d.Eylem == DenetimEylemi.Okuma
                     && d.VarlikTuru == nameof(Cari)
                     && d.VarlikId == null)
            .ToListAsync();

        Assert.Single(iz);
        Assert.Contains("liste:", iz[0].Detay);
    }

    [Fact]
    public async Task Cari_yazma_isleminde_sahte_okuma_izi_uretilmez()
    {
        // Getir() yazma akışlarının sonunda da çağrılıyor. Oraya iz konsaydı
        // her POST bir "okuma" uydururdu. Bu test o tuzağı bekçiler.
        var k = YeniKiraci("HTTP sahte okuma izi");

        var cevap = await f.Ortam.Istemci(k).PostJson("/api/v1/cari",
            new { tur = "MUSTERI", ad = "Yeni Müşteri" });
        Assert.Equal(HttpStatusCode.Created, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        var okumaIzi = await db.DenetimKayitlari.AsNoTracking()
            .CountAsync(d => d.TenantId == k.TenantId
                          && d.Eylem == DenetimEylemi.Okuma
                          && d.VarlikTuru == nameof(Cari));

        Assert.Equal(0, okumaIzi);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// ORAN SINIRI — ayrı sınıf, ayrı koleksiyon DEĞİL ama ayrı tutuldu:
// sayaçlar süreç genelinde paylaşıldığı için diğer testlerle karışmasın.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class OranSiniriHttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Giris_ucunda_ardisik_denemeler_429_dondurur()
    {
        // OranSiniri.Giris = 5/dk (IP + e-posta bölütü). 6. deneme kapanmalı.
        // ⚠ Bu testin kendisi bir REGRESYON BEKÇİSİ: 429 daha önce HİÇ
        // dönmüyordu, çünkü MapControllers().RequireRateLimiting uç
        // niteliklerini eziyordu. 300 yeşil servis testi bunu göremedi.
        var istemci = f.Ortam.AnonimIstemci();
        var epostaBenzersiz = $"oran-{Guid.NewGuid():N}@test.local";

        HttpResponseMessage? sonuncu = null;
        var kodlar = new List<int>();

        for (var i = 0; i < 8; i++)
        {
            sonuncu = await istemci.PostJson("/api/v1/auth/giris",
                new { email = epostaBenzersiz, sifre = "gecersizsifre" });
            kodlar.Add((int)sonuncu.StatusCode);
            if (sonuncu.StatusCode == HttpStatusCode.TooManyRequests) break;
        }

        Assert.True(
            kodlar.Contains(429),
            $"Oran sınırı devreye girmedi. Dönen kodlar: {string.Join(", ", kodlar)}");

        Assert.NotNull(sonuncu!.Headers.RetryAfter);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// GÜVENLİK BAŞLIKLARI — OWASP ASVS 5.0.0 §V3 (Web Frontend Security) ve §V12.
//
// 2026-09-13 ASVS öz-değerlendirmesinde açık olarak tespit edildi: uygulama
// hiçbir güvenlik başlığı göndermiyordu. Bu testler o açığın kapandığını ve
// KAPALI KALDIĞINI bekçiler.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class GuvenlikBaslikTestleri(HttpFikstur f)
{
    private async Task<HttpResponseMessage> Cevap()
        => await f.Ortam.AnonimIstemci().GetAsync("/saglik");

    private static string Baslik(HttpResponseMessage c, string ad)
        => c.Headers.TryGetValues(ad, out var d) ? string.Join(" ", d) : "";

    [Theory]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("Referrer-Policy", "strict-origin-when-cross-origin")]
    public async Task Guvenlik_basliklari_gonderilir(string ad, string beklenen)
        => Assert.Equal(beklenen, Baslik(await Cevap(), ad));

    [Fact]
    public async Task Permissions_Policy_kullanilmayan_yetenekleri_kapatir()
    {
        var p = Baslik(await Cevap(), "Permissions-Policy");
        Assert.Contains("camera=()", p);
        Assert.Contains("microphone=()", p);
        Assert.Contains("geolocation=()", p);
    }

    [Fact]
    public async Task CSP_gonderilir_ve_temel_yonergeleri_icerir()
    {
        var csp = Baslik(await Cevap(), "Content-Security-Policy");

        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("object-src 'none'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.Contains("base-uri 'self'", csp);
        Assert.Contains("form-action 'self'", csp);
    }

    [Fact]
    public async Task CSP_script_icin_unsafe_inline_VERMEZ()
    {
        // Blazor WASM 'wasm-unsafe-eval' ister — bu kaçınılmazdır.
        // Ama script-src'ye 'unsafe-inline' VERİLMEMELİDİR: o, CSP'nin
        // XSS'e karşı sağladığı korumayı büyük ölçüde iptal eder.
        var csp = Baslik(await Cevap(), "Content-Security-Policy");

        var scriptBolumu = csp.Split(';')
            .First(b => b.Trim().StartsWith("script-src", StringComparison.Ordinal));

        Assert.DoesNotContain("unsafe-inline", scriptBolumu);
        Assert.Contains("wasm-unsafe-eval", scriptBolumu);
    }

    [Fact]
    public async Task Basliklar_kimlik_dogrulamadan_ONCE_de_gonderilir()
    {
        // Başlıklar boru hattının başında ekleniyor; 401 yanıtında da olmalı.
        var cevap = await f.Ortam.AnonimIstemci().GetAsync("/api/v1/cari");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, cevap.StatusCode);
        Assert.Equal("nosniff", Baslik(cevap, "X-Content-Type-Options"));
        Assert.NotEmpty(Baslik(cevap, "Content-Security-Policy"));
    }
}
