using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Yetki;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// EVRAK UÇLARI — GERÇEK HTTP BORU HATTI (paket 29 Faz A).
//
// Servis testi [ModulYetkisi("evrak")] ve [RolGerekli] niteliklerini GÖRMEZ;
// bu dosya kapıları üretimdeki boru hattında ölçer.
//
// ⚠ TOHUMLAMA REGRESYONU (K4-F): yeni "evrak" anahtarı ESKİ kiracılarda
//   (tek "__yapilandirildi__" markeri olanlarda) sessiz 403 doğurmamalı.
//   Mekanizma paket 6'da kurulmuştur; burada kilitlenir, YENİDEN YAZILMAZ.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class HttpEvrakTestleri(HttpFikstur f)
{
    /// <summary>
    /// ESKİ (tek marker'lı) kiracının O GÜN taşıdığı modül anahtarları.
    ///
    /// ⚠ NEDEN ELLE YAZILI, NEDEN <c>Moduller.TohumlamaOncesi</c> DEĞİL:
    ///   fikstür sınanan sabitten türetilseydi, o sabite "evrak" eklendiğinde
    ///   (M29-09 mutasyonu tam bunu yapar) fikstür de "evrak" satırını yazar ve
    ///   test YEŞİL kalırdı — yani mutasyonu ölçemezdi. Fikstür GEÇMİŞİ anlatır,
    ///   bugünkü sabiti değil; bu yüzden donmuştur.
    /// </summary>
    private static readonly string[] EskiAnahtarlar =
        ["cari", "urun", "islem", "kasa", "banka", "fatura", "rapor", "kullanici"];

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static object Govde(Kiraci k, string no = "CEK-H1", decimal tutar = 10_000m) => new
    {
        evrakTuru = "CEK",
        tutar,
        vadeGunu = Zaman.YerelBugun().AddDays(20).ToString("yyyy-MM-dd"),
        evrakNo = no,
        bankaAdi = "Ziraat",
        kaynakCariId = k.CariId,
        kurSnapshotId = k.KurId,
    };

    private async Task<(HttpClient Istemci, string Id)> EvrakAc(Kiraci k, string rol = "SAHIP", string no = "CEK-H1")
    {
        var c = f.Ortam.Istemci(k, rol);
        var y = await c.PostAsJsonAsync("/api/v1/evraklar", Govde(k, no));
        Assert.Equal(HttpStatusCode.Created, y.StatusCode);
        var j = await y.Json();
        return (c, j.GetProperty("evrak").GetProperty("id").GetString()!);
    }

    // ── Yetki ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Yetkisiz_rol_403()
    {
        // TEZGAHTAR'a "evrak" modülü VARSAYILAN olarak AÇILMAZ (Mehmet mührü):
        // evrak kartı borçlunun kimlik numarasını taşır.
        var k = YeniKiraci("P29H-TEZGAHTAR");
        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");

        Assert.Equal(HttpStatusCode.Forbidden, (await tezgahtar.GetAsync("/api/v1/evraklar")).StatusCode);
        var post = await tezgahtar.PostAsJsonAsync("/api/v1/evraklar", Govde(k));
        Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
    }

    [Fact]
    public async Task Mudur_ve_muhasebe_evrak_ucunu_gorur()
    {
        var k = YeniKiraci("P29H-MUDUR");
        Assert.Equal(HttpStatusCode.OK,
            (await f.Ortam.Istemci(k, "MUDUR").GetAsync("/api/v1/evraklar")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await f.Ortam.Istemci(k, "MUHASEBE").GetAsync("/api/v1/evraklar")).StatusCode);
    }

    [Fact]
    public async Task Mevcut_kiracida_yeni_modul_200()
    {
        // ⚠ FİKSTÜR (Aşkın'ın koşulu): "mevcut kiracı" durumu ESKİ TEK MARKER'lı
        //   kiracıyla ELLE kurulur. Aksi hâlde test yeni tohumlamayı değil,
        //   yeni kiracı yolunu ölçerdi.
        var k = YeniKiraci("P29H-ESKI-MARKER");

        using (var db = f.Vt.Olustur(k.TenantId))
        {
            var mevcut = await db.RolYetkileri.Where(y => y.TenantId == k.TenantId).ToListAsync();
            db.RolYetkileri.RemoveRange(mevcut);
            db.RolYetkileri.Add(new RolYetki
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, Rol = "__SISTEM__", Modul = "__yapilandirildi__",
            });
            foreach (var rol in Moduller.Roller)
                foreach (var m in Moduller.Varsayilan[rol].Where(EskiAnahtarlar.Contains))
                    db.RolYetkileri.Add(new RolYetki
                    {
                        Id = Kimlik.Ulid(), TenantId = k.TenantId, Rol = rol, Modul = m,
                    });
            await db.SaveChangesAsync();
        }

        var mudur = f.Ortam.Istemci(k, "MUDUR");
        Assert.Equal(HttpStatusCode.OK, (await mudur.GetAsync("/api/v1/evraklar")).StatusCode);

        var muhasebe = f.Ortam.Istemci(k, "MUHASEBE");
        Assert.Equal(HttpStatusCode.OK, (await muhasebe.GetAsync("/api/v1/evraklar")).StatusCode);
    }

    [Fact]
    public async Task Sahibin_kaldirdigi_yetki_yeniden_tohumlamadan_sonra_da_403()
    {
        // ⚠ SESSİZ YETKİ GENİŞLEMESİ YASAĞI: SAHİP "evrak"ı MÜDÜR'den bilinçli
        //   kaldırdıysa, tohumlama onu GERİ GETİRMEZ.
        var k = YeniKiraci("P29H-KALDIRILAN");

        // Önce tohumlama koşsun (markerlar yazılsın).
        Assert.Equal(HttpStatusCode.OK,
            (await f.Ortam.Istemci(k, "MUDUR").GetAsync("/api/v1/evraklar")).StatusCode);

        // SAHİP matrisi kaydeder ve MÜDÜR'den "evrak"ı çıkarır.
        var sahip = f.Ortam.Istemci(k, "SAHIP");
        var matris = new Dictionary<string, string[]>
        {
            ["MUDUR"] = [.. Moduller.Varsayilan["MUDUR"].Where(m => m != "evrak")],
            ["MUHASEBE"] = Moduller.Varsayilan["MUHASEBE"],
            ["TEZGAHTAR"] = Moduller.Varsayilan["TEZGAHTAR"],
        };
        var kaydet = await sahip.PutAsJsonAsync("/api/v1/yetkiler", new { matris });
        Assert.True(kaydet.IsSuccessStatusCode, await kaydet.Metin());

        var mudur = f.Ortam.Istemci(k, "MUDUR");
        Assert.Equal(HttpStatusCode.Forbidden, (await mudur.GetAsync("/api/v1/evraklar")).StatusCode);
    }

    // ── Sözleşme ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Json_camelCase_sozlesmesi()
    {
        var k = YeniKiraci("P29H-JSON");
        var (c, id) = await EvrakAc(k);

        var j = await (await c.GetAsync($"/api/v1/evraklar/{id}")).Json();
        Assert.Equal(id, j.GetProperty("id").GetString());
        Assert.Equal("CEK", j.GetProperty("evrakTuru").GetString());
        Assert.Equal("PORTFOYDE", j.GetProperty("durum").GetString());
        Assert.True(j.TryGetProperty("izinliGecisler", out _));
    }

    [Fact]
    public async Task limit_5000_400()
    {
        var k = YeniKiraci("P29H-LIMIT");
        var c = f.Ortam.Istemci(k);
        var y = await c.GetAsync("/api/v1/evraklar?limit=5000");
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
    }

    [Fact]
    public async Task Liste_zarfi_toplamlari_SUNUCUDA_toplar()
    {
        var k = YeniKiraci("P29H-ZARF");
        var c = f.Ortam.Istemci(k);
        foreach (var no in new[] { "Z-1", "Z-2", "Z-3" })
            Assert.Equal(HttpStatusCode.Created,
                (await c.PostAsJsonAsync("/api/v1/evraklar", Govde(k, no, 1_000m))).StatusCode);

        var j = await (await c.GetAsync("/api/v1/evraklar?limit=2")).Json();
        Assert.Equal(3m, j.GetProperty("toplamAdet").GetDecimal());
        Assert.Equal(3_000m, j.GetProperty("toplamTutar").GetDecimal());
        Assert.Equal(2, j.GetProperty("satirlar").GetArrayLength());
        Assert.True(j.GetProperty("dahaFazlasiVar").GetBoolean());
        Assert.Equal(200, j.GetProperty("azamiLimit").GetInt32());
    }

    [Fact]
    public async Task Gerekcesiz_karsiliksiz_istegi_400_ve_standart_hata_zarfi()
    {
        var k = YeniKiraci("P29H-GEREKCE");
        var (c, id) = await EvrakAc(k, no: "CEK-GER");

        var y = await c.PostAsJsonAsync($"/api/v1/evraklar/{id}/karsiliksiz",
            new { aciklama = "kısa", kurSnapshotId = k.KurId });
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);

        var govde = await y.Metin();
        Assert.False(string.IsNullOrWhiteSpace(govde));
        var j = await y.Json();
        // Standart hata zarfı (HataYakalayici): message · error · statusCode
        // (+ alan hatalarında "alanlar"). Biçim testte SABİTLENİR ki bir
        // sonraki uç kendi zarfını uydurmasın.
        Assert.Equal(400, j.GetProperty("statusCode").GetInt32());
        Assert.True(j.TryGetProperty("message", out _), $"'message' bekleniyordu: {govde}");
        Assert.True(j.TryGetProperty("error", out _), $"'error' bekleniyordu: {govde}");
        Assert.True(j.TryGetProperty("alanlar", out var alanlar)
                 && alanlar.TryGetProperty("aciklama", out _),
            $"Alan altı Türkçe mesaj bekleniyordu: {govde}");
    }

    [Fact]
    public async Task Ayni_banka_ve_cek_no_ikinci_kez_409()
    {
        var k = YeniKiraci("P29H-CIFT");
        var (c, _) = await EvrakAc(k, no: "CEK-DUP");
        var y = await c.PostAsJsonAsync("/api/v1/evraklar", Govde(k, "CEK-DUP"));
        Assert.Equal(HttpStatusCode.Conflict, y.StatusCode);
    }

    [Fact]
    public async Task Gecersiz_gecis_400_gövde_ile()
    {
        var k = YeniKiraci("P29H-GECIS");
        var (c, id) = await EvrakAc(k, no: "CEK-GEC");

        // Portföydeki evrak zaten portföydedir → PORTFOYDE'ye geçilemez.
        var y = await c.PostAsJsonAsync($"/api/v1/evraklar/{id}/portfoye-al", new { });
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
        Assert.Contains("PORTFOYDE", await y.Metin());
    }

    // ── Kiracı izolasyonu ────────────────────────────────────────────────────

    [Fact]
    public async Task Yabanci_kiracinin_evraki_404_doner()
    {
        var a = YeniKiraci("P29H-IZO-A");
        var b = YeniKiraci("P29H-IZO-B");
        var (_, id) = await EvrakAc(a, no: "CEK-IZO");

        var bIstemci = f.Ortam.Istemci(b);
        Assert.Equal(HttpStatusCode.NotFound, (await bIstemci.GetAsync($"/api/v1/evraklar/{id}")).StatusCode);
        var post = await bIstemci.PostAsJsonAsync($"/api/v1/evraklar/{id}/portfoye-al", new { });
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
    }

    // ── PATCH sözleşmesi ─────────────────────────────────────────────────────

    [Fact]
    public async Task Patch_gonderilmeyen_alani_KORUR()
    {
        var k = YeniKiraci("P29H-PATCH");
        var c = f.Ortam.Istemci(k);
        var olustur = await c.PostAsJsonAsync("/api/v1/evraklar", new
        {
            evrakTuru = "CEK",
            tutar = 5_000m,
            vadeGunu = Zaman.YerelBugun().AddDays(10).ToString("yyyy-MM-dd"),
            evrakNo = "CEK-PT",
            bankaAdi = "Ziraat",
            notlar = "korunmalı",
            kaynakCariId = k.CariId,
            kurSnapshotId = k.KurId,
        });
        Assert.Equal(HttpStatusCode.Created, olustur.StatusCode);
        var id = (await olustur.Json()).GetProperty("evrak").GetProperty("id").GetString()!;

        var patch = await c.PatchAsJsonAsync($"/api/v1/evraklar/{id}", new { evrakNo = "CEK-PT-2" });
        Assert.True(patch.IsSuccessStatusCode, await patch.Metin());

        var j = await (await c.GetAsync($"/api/v1/evraklar/{id}")).Json();
        Assert.Equal("CEK-PT-2", j.GetProperty("evrakNo").GetString());
        Assert.Equal("korunmalı", j.GetProperty("notlar").GetString());
    }
}
