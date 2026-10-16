using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Yetki;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// SİPARİŞ UÇLARI — GERÇEK HTTP BORU HATTI (paket 3 Faz A).
//
// Servis testi [ModulYetkisi("siparis")] ve [RolGerekli] niteliklerini GÖRMEZ.
// Bu dosya kapıları üretimdeki boru hattında ölçer.
//
// ⚠ TOHUMLAMA BORCU (Bertan + Aşkın'ın ortak koşulu): yeni "siparis" anahtarı
//   ESKİ kiracılarda (tek "__yapilandirildi__" markeri olanlarda) sessiz 403
//   doğurmamalı. Paket 6'nın modül-başı tohumlaması bunu çözer; burada
//   REGRESYON TESTİYLE kilitlenir — mekanizma yeniden yazılmaz.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class HttpSiparisTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static object Govde(Kiraci k, string tur = "MUSTERI_SIPARISI") => new
    {
        tur,
        cariId = k.CariId,
        sozVerilenGun = Zaman.YerelBugun().AddDays(3).ToString("yyyy-MM-dd"),
        aciklama = "HTTP testi",
        kalemler = new[] { new { urunId = k.UrunId, adet = 1m } },
    };

    private async Task<(HttpClient Istemci, string Id)> SiparisAc(Kiraci k, string rol = "SAHIP")
    {
        var c = f.Ortam.Istemci(k, rol);
        var y = await c.PostAsJsonAsync("/api/v1/siparisler", Govde(k));
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var j = await y.Json();
        return (c, j.GetProperty("id").GetString()!);
    }

    // ── Yetki ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Tezgahtar_siparis_olusturabilir_ama_iptal_edemez()
    {
        var k = YeniKiraci("P3H-TEZGAHTAR");
        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");

        var olustur = await tezgahtar.PostAsJsonAsync("/api/v1/siparisler", Govde(k));
        Assert.Equal(HttpStatusCode.OK, olustur.StatusCode);
        var id = (await olustur.Json()).GetProperty("id").GetString()!;

        var iptal = await tezgahtar.PostAsJsonAsync($"/api/v1/siparisler/{id}/gecis",
            new { yeniDurum = "IPTAL", gerekce = "müşteri vazgeçti diyor" });
        Assert.Equal(HttpStatusCode.Forbidden, iptal.StatusCode);

        // Durum DEĞİŞMEDİ
        var kart = await (await tezgahtar.GetAsync($"/api/v1/siparisler/{id}")).Json();
        Assert.Equal("TASLAK", kart.GetProperty("durum").GetString());

        // SAHİP iptal edebilir
        var sahip = f.Ortam.Istemci(k);
        var sahipIptal = await sahip.PostAsJsonAsync($"/api/v1/siparisler/{id}/gecis",
            new { yeniDurum = "IPTAL", gerekce = "müşteri vazgeçti, kayıt kapatıldı" });
        Assert.Equal(HttpStatusCode.OK, sahipIptal.StatusCode);
    }

    [Fact]
    public async Task Tezgahtar_gecis_dokumu_ucunu_goremez()
    {
        var k = YeniKiraci("P3H-DOKUM");
        var (_, id) = await SiparisAc(k);
        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");
        var y = await tezgahtar.GetAsync($"/api/v1/siparisler/{id}/gecisler");
        Assert.Equal(HttpStatusCode.Forbidden, y.StatusCode);
    }

    [Fact]
    public async Task Yetki_matrisinden_siparis_kaldirilan_rol_403_alir()
    {
        var k = YeniKiraci("P3H-YETKI");
        var sahip = f.Ortam.Istemci(k);

        // SAHİP, tezgâhtardan "siparis" modülünü kaldırır (yetki ekranının yolu).
        var matris = new Dictionary<string, string[]>
        {
            ["MUDUR"] = Moduller.Varsayilan["MUDUR"],
            ["MUHASEBE"] = Moduller.Varsayilan["MUHASEBE"],
            ["TEZGAHTAR"] = [.. Moduller.Varsayilan["TEZGAHTAR"].Where(m => m != "siparis")],
        };
        var kaydet = await sahip.PutAsJsonAsync("/api/v1/yetkiler", new { matris });
        Assert.Equal(HttpStatusCode.OK, kaydet.StatusCode);

        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");
        var y = await tezgahtar.GetAsync("/api/v1/siparisler");
        Assert.Equal(HttpStatusCode.Forbidden, y.StatusCode);

        // MÜDÜR hâlâ görebilir — kapı role göre çalışıyor, topyekûn kapanmadı.
        var mudur = f.Ortam.Istemci(k, "MUDUR");
        Assert.Equal(HttpStatusCode.OK, (await mudur.GetAsync("/api/v1/siparisler")).StatusCode);
    }

    [Fact]
    public async Task Eski_markerli_kiracida_MUDUR_siparis_ucunu_gorur_sessiz_403_yok()
    {
        var k = YeniKiraci("P3H-ESKI-MARKER");

        // ESKİ kiracıyı taklit et: yalnız tek "__yapilandirildi__" markeri ve
        // tohumlama ÖNCESİ anahtarların satırları var; "siparis" hiç bilinmiyor.
        using (var db = f.Vt.Olustur(k.TenantId))
        {
            var mevcut = await db.RolYetkileri.Where(y => y.TenantId == k.TenantId).ToListAsync();
            db.RolYetkileri.RemoveRange(mevcut);
            db.RolYetkileri.Add(new RolYetki
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, Rol = "__SISTEM__", Modul = "__yapilandirildi__",
            });
            foreach (var rol in Moduller.Roller)
                foreach (var m in Moduller.Varsayilan[rol].Where(Moduller.TohumlamaOncesi.Contains))
                    db.RolYetkileri.Add(new RolYetki
                    {
                        Id = Kimlik.Ulid(), TenantId = k.TenantId, Rol = rol, Modul = m,
                    });
            await db.SaveChangesAsync();
        }

        var mudur = f.Ortam.Istemci(k, "MUDUR");
        var y = await mudur.GetAsync("/api/v1/siparisler");
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);

        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");
        Assert.Equal(HttpStatusCode.OK, (await tezgahtar.GetAsync("/api/v1/siparisler")).StatusCode);
    }

    // ── Kiracı izolasyonu ────────────────────────────────────────────────────

    [Fact]
    public async Task Yabanci_kiracinin_siparisi_404_doner()
    {
        var a = YeniKiraci("P3H-IZO-A");
        var b = YeniKiraci("P3H-IZO-B");
        var (_, id) = await SiparisAc(a);

        var bIstemci = f.Ortam.Istemci(b);
        Assert.Equal(HttpStatusCode.NotFound, (await bIstemci.GetAsync($"/api/v1/siparisler/{id}")).StatusCode);
        var gecis = await bIstemci.PostAsJsonAsync($"/api/v1/siparisler/{id}/gecis", new { yeniDurum = "ONAYLANDI" });
        Assert.Equal(HttpStatusCode.NotFound, gecis.StatusCode);
    }

    // ── Sözleşme ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Limit_1000_istense_de_en_cok_200_satir_doner()
    {
        var k = YeniKiraci("P3H-LIMIT");
        var c = f.Ortam.Istemci(k);
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.OK,
                (await c.PostAsJsonAsync("/api/v1/siparisler", Govde(k))).StatusCode);

        var j = await (await c.GetAsync("/api/v1/siparisler?limit=1000")).Json();
        Assert.Equal(200, j.GetProperty("limit").GetInt32());
        Assert.Equal(200, j.GetProperty("azamiLimit").GetInt32());
        Assert.Equal(3, j.GetProperty("siparisler").GetArrayLength());

        // M151 ölçümü: sunucuda gerçekten Take VAR — limit=2 istenince 2 satır gelir
        // ama toplam 3 kalır ve "daha fazlası var" YAZILIR (sessiz kırpma yok).
        var kirpik = await (await c.GetAsync("/api/v1/siparisler?limit=2")).Json();
        Assert.Equal(2, kirpik.GetProperty("siparisler").GetArrayLength());
        Assert.Equal(3, kirpik.GetProperty("toplam").GetInt32());
        Assert.True(kirpik.GetProperty("dahaFazlasiVar").GetBoolean());
    }

    [Fact]
    public async Task JSON_sozlesmesi_camelCase_ve_kapali_liste_degerleriyle_doner()
    {
        var k = YeniKiraci("P3H-JSON");
        var (c, id) = await SiparisAc(k);

        var j = await (await c.GetAsync($"/api/v1/siparisler/{id}")).Json();
        foreach (var alan in new[] { "id", "no", "tur", "durum", "cariId", "sozVerilenGun", "kalemler", "gecisler" })
            Assert.True(j.TryGetProperty(alan, out _), $"camelCase alan eksik: {alan}");

        Assert.Contains(j.GetProperty("tur").GetString(), SiparisTuru.Tumu);
        Assert.Contains(j.GetProperty("durum").GetString(), SiparisDurumu.Tumu);
        Assert.True(j.GetProperty("duzenlenebilir").GetBoolean());
        Assert.Contains("ONAYLANDI",
            j.GetProperty("izinliGecisler").EnumerateArray().Select(x => x.GetString()));

        var liste = await (await c.GetAsync("/api/v1/siparisler")).Json();
        foreach (var alan in new[] { "toplam", "gecikenSayisi", "durumKirilimi", "siparisler", "dahaFazlasiVar" })
            Assert.True(liste.TryGetProperty(alan, out _), $"camelCase alan eksik: {alan}");
    }

    [Fact]
    public async Task Gecersiz_durum_ve_yasak_gecis_400_doner()
    {
        var k = YeniKiraci("P3H-GECIS");
        var (c, id) = await SiparisAc(k);

        var uydurma = await c.PostAsJsonAsync($"/api/v1/siparisler/{id}/gecis", new { yeniDurum = "CILA_BITTI" });
        Assert.Equal(HttpStatusCode.BadRequest, uydurma.StatusCode);

        var yasak = await c.PostAsJsonAsync($"/api/v1/siparisler/{id}/gecis", new { yeniDurum = "TESLIM_EDILDI" });
        Assert.Equal(HttpStatusCode.BadRequest, yasak.StatusCode);
        Assert.Equal(400, (await yasak.Json()).GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task PATCH_TASLAK_disinda_400_doner()
    {
        var k = YeniKiraci("P3H-PATCH");
        var (c, id) = await SiparisAc(k);
        Assert.Equal(HttpStatusCode.OK,
            (await c.PostAsJsonAsync($"/api/v1/siparisler/{id}/gecis", new { yeniDurum = "ONAYLANDI" })).StatusCode);

        var istek = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/siparisler/{id}")
        {
            Content = JsonContent.Create(new { aciklama = "sonradan" }),
        };
        var y = await c.SendAsync(istek);
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
    }
}
