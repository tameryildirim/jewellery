using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Hurda;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ŞUBE-KAPSAM-B tur 3 — ALTIN POZİSYONU ŞUBE KAPSAMI [HTTP, gerçek MySQL]
//
// Doğrulayıcı KRİTİK: Panel/Rapor merkezindeki "Altın pozisyonum" kartı seçili
// şubeyi değil TÜM şubeleri gösteriyordu (GET /pozisyon şube parametresi yoktu).
// Kural: ?subeId=B → VİTRİN/KONSİNYE/HURDA yalnız B (SQL ile aynı); subeId yok →
// tüm şubeler (M+B); CARİ alacak/borç + emanet müşteri hesabıdır, şubeye bölünmez
// (şartname § 1) — iki durumda da AYNI. Yetki: GorunumSubesi.Coz (pasif/yabancı → 404),
// TEZGÂHTAR rol kapısından 403.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class SubeKapsamBPozisyonHttpTestleri(HttpFikstur f)
{
    private sealed record Kurulum(Kiraci K, string SubeB, string SubePasif);

    private const decimal StokB = 3m;
    private const decimal HurdaM = 4.5m, HurdaB = 1.25m;
    private const decimal VeresiyeB = -2m;   // K4: bakiye += −HasGrami → +2 g alacak (B şubesinde yazıldı)

    private Kurulum Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, ad);
        var now = DateTime.UtcNow;
        var subeB = Kimlik.Ulid();
        var pasif = Kimlik.Ulid();
        db.Subeler.Add(new Sube { Id = subeB, TenantId = k.TenantId, Ad = "Şube B", Aktif = true, Olusturuldu = now, Guncellendi = now });
        db.Subeler.Add(new Sube { Id = pasif, TenantId = k.TenantId, Ad = "Kapalı şube", Aktif = false, Olusturuldu = now, Guncellendi = now });
        db.Stoklar.Add(new Stok { Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = subeB, UrunId = k.UrunId, Adet = StokB, ToplamHasGram = StokB * k.UrunHasGrami, Guncellendi = now });

        void H(string sube, string tur, decimal has) => db.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = sube, Tur = tur, Tarih = now,
            TlTutari = -10m, HasGrami = has, ParaBirimi = "TRY", Maden = "ALTIN",
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            OdemeTuru = "VERESIYE", Aciklama = "SK-B tur 3 fikstür", Olusturuldu = now,
        });
        H(k.SubeId, "HURDA_ALIS", HurdaM);
        H(subeB, "HURDA_ALIS", HurdaB);
        H(subeB, "VERESIYE", VeresiyeB);
        db.SaveChanges();
        return new Kurulum(k, subeB, pasif);
    }

    private static async Task<JsonElement> Ok(HttpResponseMessage c)
    {
        var govde = await c.Metin();
        Assert.True(c.StatusCode == HttpStatusCode.OK, $"beklenen 200, gelen {(int)c.StatusCode}: {govde}");
        return JsonDocument.Parse(govde).RootElement;
    }

    private static decimal A(JsonElement j, string alan) => j.GetProperty("altin").GetProperty(alan).GetDecimal();

    [Fact]
    public async Task SKB_P1_secili_sube_vitrin_ve_hurda_YALNIZ_o_sube_SQL_ile_ayni_cari_kiraci_geneli()
    {
        var s = Kur("PAKETSUBE-KAPSAM-B-TEST poz1");
        var ist = f.Ortam.Istemci(s.K, "SAHIP");
        var b = await Ok(await ist.GetAsync($"/api/v1/pozisyon?subeId={s.SubeB}"));
        var m = await Ok(await ist.GetAsync($"/api/v1/pozisyon?subeId={s.K.SubeId}"));
        var tumu = await Ok(await ist.GetAsync("/api/v1/pozisyon"));

        await using var db = f.Vt.OlusturSistem();
        decimal SqlVitrin(string? sube) => db.Stoklar.Where(x => x.TenantId == s.K.TenantId && (sube == null || x.SubeId == sube))
            .Sum(x => x.ToplamHasGram);
        decimal SqlHurda(string? sube) => db.Hareketler.Where(x => x.TenantId == s.K.TenantId
            && HurdaStokKapisi.HurdaTurleri.Contains(x.Tur) && (sube == null || x.SubeId == sube)).Sum(x => x.HasGrami);

        Assert.Equal(SqlVitrin(s.SubeB), A(b, "vitrinGram"));
        Assert.Equal(StokB * s.K.UrunHasGrami, A(b, "vitrinGram"));
        Assert.Equal(SqlHurda(s.SubeB), A(b, "hurdaGram"));
        Assert.Equal(HurdaB, A(b, "hurdaGram"));
        Assert.Equal("SUBE", b.GetProperty("kapsam").GetString());
        Assert.Equal("KIRACI_GENELI", b.GetProperty("cariKapsam").GetString());

        Assert.Equal(SqlVitrin(s.K.SubeId), A(m, "vitrinGram"));
        Assert.Equal(HurdaM, A(m, "hurdaGram"));

        Assert.Equal(SqlVitrin(null), A(tumu, "vitrinGram"));
        Assert.Equal(A(m, "vitrinGram") + A(b, "vitrinGram"), A(tumu, "vitrinGram"));
        Assert.Equal(HurdaM + HurdaB, A(tumu, "hurdaGram"));
        Assert.Equal("TUM_SUBELER", tumu.GetProperty("kapsam").GetString());

        // Cari alacak müşteri hesabıdır: B'de yazıldı ama M görünümünde de AYNI (şubeye bölünmez).
        Assert.Equal(-VeresiyeB, A(b, "cariAlacakGram"));
        Assert.Equal(A(tumu, "cariAlacakGram"), A(m, "cariAlacakGram"));
        Assert.Equal(A(tumu, "cariAlacakGram"), A(b, "cariAlacakGram"));
        // NET = vitrin + hurda + alacak − borç (seçili şube için de aynı formül).
        Assert.Equal(A(b, "vitrinGram") + A(b, "hurdaGram") + A(b, "cariAlacakGram") - A(b, "cariBorcGram"), A(b, "netGram"));
    }

    [Fact]
    public async Task SKB_P2_TUMU_degeri_birlesik_doner()
    {
        var s = Kur("PAKETSUBE-KAPSAM-B-TEST poz2");
        var ist = f.Ortam.Istemci(s.K, "MUDUR");
        var t = await Ok(await ist.GetAsync("/api/v1/pozisyon?subeId=TUMU"));
        Assert.Equal("TUM_SUBELER", t.GetProperty("kapsam").GetString());
        Assert.Equal(HurdaM + HurdaB, A(t, "hurdaGram"));
    }

    [Fact]
    public async Task SKB_P3_pasif_ve_yabanci_sube_404_tezgahtar_403()
    {
        var s = Kur("PAKETSUBE-KAPSAM-B-TEST poz3");
        var baska = Kur("PAKETSUBE-KAPSAM-B-TEST poz3b");
        var ist = f.Ortam.Istemci(s.K, "SAHIP");
        var pasif = await ist.GetAsync($"/api/v1/pozisyon?subeId={s.SubePasif}");
        Assert.Equal(HttpStatusCode.NotFound, pasif.StatusCode);
        Assert.Contains("Şube", JsonDocument.Parse(await pasif.Metin()).RootElement.GetProperty("message").GetString()!, StringComparison.Ordinal);
        var yabanci = await ist.GetAsync($"/api/v1/pozisyon?subeId={baska.SubeB}");
        Assert.Equal(HttpStatusCode.NotFound, yabanci.StatusCode);

        var tz = f.Ortam.Istemci(s.K.KullaniciId, s.K.TenantId, s.K.SubeId, "TEZGAHTAR");
        var c = await tz.GetAsync($"/api/v1/pozisyon?subeId={s.SubeB}");
        Assert.Equal(HttpStatusCode.Forbidden, c.StatusCode);
    }
}
