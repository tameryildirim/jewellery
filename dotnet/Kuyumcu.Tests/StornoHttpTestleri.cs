using System.Net;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// STORNO — HTTP KAPISI (paket 48 Faz A).
//
// NEDEN SERVİS TESTİ YETMEZ: [RolGerekli] ve [ModulYetkisi] birer DENETLEYİCİ
// niteliğidir; servis testi onları hiç görmez. "Tezgâhtar defteri geri
// alamaz" ancak gerçek HTTP isteğiyle kanıtlanır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class StornoHttpTestleri(HttpFikstur f)
{
    private const string Gerekce = "Yanlış cariye işlendi, gün içinde fark edildi";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static object Govde(Kiraci k, string? kasaId, string? gerekce = null)
        => new { gerekce = gerekce ?? Gerekce, kurSnapshotId = k.KurId, kasaId };

    /// <summary>HTTP üzerinden nakit işçilik fişi keser, hareket kimliğini döner.</summary>
    private async Task<string> IscilikKes(Kiraci k, decimal tutar)
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/islemler/iscilik", new
        {
            cariId = k.CariId, iscilikTl = tutar, odemeTuru = "NAKIT",
            kasaId = k.KasaId, kurSnapshotId = k.KurId, aciklama = "İşçilik",
        });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        return await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "ISCILIK" && h.StornoKaynakId == null)
            .OrderByDescending(h => h.Olusturuldu).Select(h => h.Id).FirstAsync();
    }

    [Fact]
    public async Task SAHIP_storno_atar_200_ve_ters_kayit_olusur()
    {
        var k = YeniKiraci("HTTP storno sahip");
        var kaynakId = await IscilikKes(k, 1_100m);

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/islemler/{kaynakId}/storno", Govde(k, k.KasaId));

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var json = await cevap.Json();
        Assert.Equal(kaynakId, json.GetProperty("kaynakHareketId").GetString());
        Assert.Equal(-1_100m, json.GetProperty("tlTutari").GetDecimal());

        using var db = f.Vt.OlusturSistem();
        Assert.True(await db.Hareketler.AsNoTracking()
            .AnyAsync(h => h.TenantId == k.TenantId && h.StornoKaynakId == kaynakId));
    }

    [Fact]
    public async Task TEZGAHTAR_storno_atamaz_403_ve_defterde_satir_olusmaz()
    {
        var k = YeniKiraci("HTTP storno tezgâhtar");
        var kaynakId = await IscilikKes(k, 500m);

        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR")
            .PostJson($"/api/v1/islemler/{kaynakId}/storno", Govde(k, k.KasaId));

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.Hareketler.AsNoTracking()
            .AnyAsync(h => h.TenantId == k.TenantId && h.StornoKaynakId != null));
    }

    [Fact]
    public async Task Yabanci_kiracinin_hareketi_404()
    {
        var a = YeniKiraci("HTTP storno A");
        var b = YeniKiraci("HTTP storno B");
        var kaynakId = await IscilikKes(a, 300m);

        var cevap = await f.Ortam.Istemci(b, "SAHIP")
            .PostJson($"/api/v1/islemler/{kaynakId}/storno", Govde(b, b.KasaId));

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
    }

    [Fact]
    public async Task Gerekce_19_karakter_400()
    {
        var k = YeniKiraci("HTTP storno gerekçe");
        var kaynakId = await IscilikKes(k, 200m);

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/islemler/{kaynakId}/storno",
                      Govde(k, k.KasaId, new string('a', 19)));

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    [Fact]
    public async Task kurSnapshotId_gonderilmeyince_400()
    {
        var k = YeniKiraci("HTTP storno kur");
        var kaynakId = await IscilikKes(k, 200m);

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/islemler/{kaynakId}/storno",
                      new { gerekce = Gerekce, kasaId = k.KasaId });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    [Fact]
    public async Task Ayni_kayda_ikinci_storno_409()
    {
        var k = YeniKiraci("HTTP storno çift");
        var kaynakId = await IscilikKes(k, 800m);

        var ilk = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/islemler/{kaynakId}/storno", Govde(k, k.KasaId));
        Assert.Equal(HttpStatusCode.OK, ilk.StatusCode);

        var ikinci = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/islemler/{kaynakId}/storno", Govde(k, k.KasaId));
        Assert.Equal(HttpStatusCode.Conflict, ikinci.StatusCode);
    }

    [Fact]
    public async Task Bugun_kapaliysa_409_kaynak_gunu_kapaliysa_SAHIP_gecer()
    {
        var k = YeniKiraci("HTTP storno gün kilidi");
        var kaynakId = await IscilikKes(k, 600m);

        // BUGÜN kapatılır: storno bugünün hareketidir → kapalı güne yazılamaz.
        using (var db = f.Vt.OlusturSistem())
        {
            db.GunKapanislari.Add(new GunKapanisi
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, KasaId = k.KasaId, SubeId = k.SubeId,
                YerelGun = DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow)),
                SayilanNakit = 0m, DefterBakiye = 0m, Fark = 0m,
                KapatanKullaniciId = k.KullaniciId, KapanisZamani = DateTime.UtcNow,
                Olusturuldu = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var kapali = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/islemler/{kaynakId}/storno", Govde(k, k.KasaId));
        Assert.Equal(HttpStatusCode.BadRequest, kapali.StatusCode);
        // ⚠ Türkçe gövde AYRIŞTIRILARAK sınanır: ham metinde \u00DC kaçışları olur.
        // SON-KALANLAR: kapanmış gün iletisi sade (büyük harf ve rol kodu yok) — kilit aynı.
        Assert.Contains("günü kapandı", (await kapali.Json()).GetProperty("message").GetString());

        // Gün yeniden açılınca (AcilisZamani dolu) kilit uygulanmaz ve storno geçer.
        using (var db = f.Vt.OlusturSistem())
        {
            var g = db.GunKapanislari.Single(x => x.TenantId == k.TenantId);
            g.AcilisZamani = DateTime.UtcNow;
            g.AcanKullaniciId = k.KullaniciId;
            g.AcilisGerekcesi = "Storno için yeniden açıldı";
            await db.SaveChangesAsync();
        }

        var acik = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/islemler/{kaynakId}/storno", Govde(k, k.KasaId));
        Assert.Equal(HttpStatusCode.OK, acik.StatusCode);
    }

    [Fact]
    public async Task Yanlis_kasa_gonderilirse_400()
    {
        var k = YeniKiraci("HTTP storno kasa");
        var kaynakId = await IscilikKes(k, 450m);

        string baskaKasaId;
        using (var db = f.Vt.OlusturSistem())
        {
            baskaKasaId = Kimlik.Ulid();
            db.Kasalar.Add(new Kasa
            {
                Id = baskaKasaId, TenantId = k.TenantId, SubeId = k.SubeId, Ad = "İkinci kasa",
                ParaBirimi = "TRY", Bakiye = 0m, Aktif = true, Guncellendi = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/islemler/{kaynakId}/storno", Govde(k, baskaKasaId));

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Contains("aynı kasaya", (await cevap.Json()).GetProperty("message").GetString());
    }

    [Fact]
    public async Task Denetim_izinde_storno_kaydi_gorunur_ve_sube_damgasi_vardir()
    {
        var k = YeniKiraci("HTTP storno iz");
        var kaynakId = await IscilikKes(k, 350m);

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/islemler/{kaynakId}/storno", Govde(k, k.KasaId));
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        var iz = await f.Ortam.Istemci(k, "SAHIP")
            .GetAsync($"/api/v1/denetim?varlikTuru=Storno&subeId={k.SubeId}");
        Assert.Equal(HttpStatusCode.OK, iz.StatusCode);

        var json = await iz.Json();
        Assert.Equal(1, json.GetProperty("toplam").GetInt32());
        var kayit = json.GetProperty("kayitlar")[0];
        Assert.Equal(k.SubeId, kayit.GetProperty("subeId").GetString());
        Assert.Equal(k.KasaId, kayit.GetProperty("kasaId").GetString());
        Assert.Contains(Gerekce, kayit.GetProperty("detay").GetString());
    }
}
