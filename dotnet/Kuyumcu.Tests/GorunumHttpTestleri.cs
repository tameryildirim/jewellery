using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Gorunum;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// GÖRÜNÜM UÇLARI — GERÇEK HTTP BORU HATTI (paket P-36b · kurucu talimatı 16)
//
// Servis testi [RolGerekli("SAHIP")] niteliğini GÖRMEZ; bu dosya kapıyı
// üretimdeki boru hattında ölçer. Ayrıca PATCH sözleşmesini ("gövdede yoksa
// koru, null ise varsayılana dön") ve denetim kaydını kilitler.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class GorunumHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static async Task<(string sema, string olcek)> Oku(HttpResponseMessage c)
    {
        var j = await c.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        // camelCase sözleşmesi: yanıt {gorunum:{sema,yaziOlcegi}} ya da {sema,yaziOlcegi}
        var g = j.TryGetProperty("gorunum", out var ic) ? ic : j;
        return (g.GetProperty("sema").GetString()!, g.GetProperty("yaziOlcegi").GetString()!);
    }

    // ── 1) Jetonsuz istek 401 ───────────────────────────────────────────────
    [Fact]
    public async Task GH1_Jetonsuz_istek_401_doner()
    {
        var c = await f.Ortam.AnonimIstemci().GetAsync("/api/v1/gorunum");
        Assert.Equal(HttpStatusCode.Unauthorized, c.StatusCode);
    }

    // ── 2) Kayıt yokken VARSAYILAN döner — mevcut kiracının ekranı DEĞİŞMEZ ──
    [Fact]
    public async Task GH2_Kayit_yokken_varsayilan_doner()
    {
        var k = YeniKiraci("PAKETP36B-TEST-varsayilan");
        var c = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/gorunum");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);

        var (sema, olcek) = await Oku(c);
        Assert.Equal(GorunumSecenekleri.SemaVarsayilan, sema);
        Assert.Equal(GorunumSecenekleri.OlcekVarsayilan, olcek);
    }

    // ── 3) TEZGÂHTAR yazamaz (403) — "renk ve yazı tipi benim işim" ──────────
    [Fact]
    public async Task GH3_Tezgahtar_gorunumu_degistiremez()
    {
        var k = YeniKiraci("PAKETP36B-TEST-rol");
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR")
            .PatchAsJsonAsync("/api/v1/gorunum", new { sema = "kurumsal" });
        Assert.Equal(HttpStatusCode.Forbidden, c.StatusCode);

        // Okumak SERBEST: ekran kendi görünümünü çizebilmeli.
        var oku = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/gorunum");
        Assert.Equal(HttpStatusCode.OK, oku.StatusCode);
    }

    // ── 4) SAHİP yazar, sunucu YAZILANI döndürür (UX-S27) ───────────────────
    [Fact]
    public async Task GH4_Sahip_semayi_degistirir_ve_yanit_sonucu_tasir()
    {
        var k = YeniKiraci("PAKETP36B-TEST-yaz");
        var c = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync("/api/v1/gorunum", new { sema = "zumrut" });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);

        var (sema, olcek) = await Oku(c);
        Assert.Equal("zumrut", sema);
        Assert.Equal(GorunumSecenekleri.OlcekVarsayilan, olcek);

        using var db = f.Vt.Olustur(k.TenantId);
        var satir = await db.KiraciAyarlari.AsNoTracking()
            .SingleAsync(a => a.Anahtar == KiraciAyarAnahtari.GorunumSemasi);
        Assert.Equal("zumrut", satir.Deger);
    }

    // ── 5) PATCH SÖZLEŞMESİ: gövdede olmayan alan KORUNUR ───────────────────
    [Fact]
    public async Task GH5_Gonderilmeyen_alan_korunur()
    {
        var k = YeniKiraci("PAKETP36B-TEST-patch");
        var i = f.Ortam.Istemci(k, "SAHIP");

        await i.PatchAsJsonAsync("/api/v1/gorunum", new { sema = "zumrut" });
        var c = await i.PatchAsJsonAsync("/api/v1/gorunum", new { yaziOlcegi = "20" });

        var (sema, olcek) = await Oku(c);
        Assert.Equal("zumrut", sema);         // ⚠ tam gövdeyle EZİLMEDİ
        Assert.Equal("20", olcek);
    }

    // ── 5b) ESKİ KOD ÖLMEZ (PAKET B-1 göç sözleşmesi): takma adla gelen istek
    //       KABUL edilir (eski istemci 400 almaz) ama depoya KANONİK slug yazılır.
    [Fact]
    public async Task GH5b_Eski_kodlar_kanonige_gocer()
    {
        var k = YeniKiraci("PAKETB-1-TEST-goc");
        var i = f.Ortam.Istemci(k, "SAHIP");

        var c = await i.PatchAsJsonAsync("/api/v1/gorunum",
            new { sema = "kurumsal", yaziOlcegi = "okunakli" });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);

        var (sema, olcek) = await Oku(c);
        Assert.Equal(GorunumSecenekleri.SemaVarsayilan, sema);   // kurumsal → platin
        Assert.Equal("20", olcek);                               // okunakli → 20 px

        using var db = f.Vt.Olustur(k.TenantId);
        var satir = await db.KiraciAyarlari.AsNoTracking()
            .SingleAsync(a => a.Anahtar == KiraciAyarAnahtari.GorunumSemasi);
        Assert.Equal(GorunumSecenekleri.SemaVarsayilan, satir.Deger);
    }

    // ── 6) null = VARSAYILANA DÖN (ayar satırı silinir) ─────────────────────
    [Fact]
    public async Task GH6_Null_gonderilince_varsayilana_donulur()
    {
        var k = YeniKiraci("PAKETP36B-TEST-null");
        var i = f.Ortam.Istemci(k, "SAHIP");

        await i.PatchAsJsonAsync("/api/v1/gorunum", new { sema = "kurumsal" });
        var c = await i.PatchAsJsonAsync("/api/v1/gorunum", new { sema = (string?)null });

        var (sema, _) = await Oku(c);
        Assert.Equal(GorunumSecenekleri.SemaVarsayilan, sema);

        using var db = f.Vt.Olustur(k.TenantId);
        Assert.False(await db.KiraciAyarlari
            .AnyAsync(a => a.Anahtar == KiraciAyarAnahtari.GorunumSemasi));
    }

    // ── 7) BEYAZ LİSTE: bilinmeyen şema 400 — sessizce varsayılana DÜŞMEZ ───
    [Fact]
    public async Task GH7_Bilinmeyen_sema_400_doner()
    {
        var k = YeniKiraci("PAKETP36B-TEST-400");
        var c = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync("/api/v1/gorunum", new { sema = "pembe" });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
    }

    // ── 8) Boş gövde 400 — "hiçbir şey olmayan" istek sessiz geçmez ─────────
    [Fact]
    public async Task GH8_Bos_govde_400_doner()
    {
        var k = YeniKiraci("PAKETP36B-TEST-bos");
        var c = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync("/api/v1/gorunum", new { });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
    }

    // ── 9) DENETİM KAYDI yazılır (DoD sözleşmesi) ───────────────────────────
    [Fact]
    public async Task GH9_Denetim_kaydi_yazilir()
    {
        var k = YeniKiraci("PAKETP36B-TEST-denetim");
        await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync("/api/v1/gorunum", new { yaziOlcegi = "okunakli" });

        using var db = f.Vt.Olustur(k.TenantId);
        Assert.True(await db.DenetimKayitlari.AsNoTracking()
            .AnyAsync(d => d.VarlikTuru == GorunumServisi.Varlik));
    }

    // ── 10) KİRACI SIZINTISI YOK: bir kiracının şeması ötekine geçmez ───────
    [Fact]
    public async Task GH10_Kiraci_sizintisi_yok()
    {
        var a = YeniKiraci("PAKETP36B-TEST-kiraci-a");
        var b = YeniKiraci("PAKETP36B-TEST-kiraci-b");

        await f.Ortam.Istemci(a, "SAHIP")
            .PatchAsJsonAsync("/api/v1/gorunum", new { sema = "zumrut" });

        var c = await f.Ortam.Istemci(b, "SAHIP").GetAsync("/api/v1/gorunum");
        var (sema, _) = await Oku(c);
        Assert.Equal(GorunumSecenekleri.SemaVarsayilan, sema);
    }
}
