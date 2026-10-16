using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-19 (Y-11 · Y-12) — GİDER + VİRMAN: İKİ BACAKLI ATOMİK DESEN
// [HTTP + doğrudan DB doğrulaması, gerçek MySQL]
//
// HER TEST BİR İŞ KURALINI YENİDEN ÜRETİR:
//   • Virmanın İKİ bacağı TEK işlemde yazılır ve KARŞILIKLI bağlanır.
//   • Virman ciroya (hareketler) GİRMEZ ve gider SAYILMAZ.
//   • Virmanın tek bacağı stornolanamaz; storno İKİ bacağı birlikte döndürür.
//   • Gider kategorilidir, kâr-zararda AYRI satırdır ve dönem kârından düşer.
//   • Gider kategorisi SİLİNMEZ (pasife alınır); PATCH gönderilmeyen alanı korur.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class P19CiftBacakTestleri(HttpFikstur f)
{
    // ── yardımcılar ─────────────────────────────────────────────────────────

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private async Task<string> KasaAc(Kiraci k, string ad, decimal acilis = 0m, string doviz = "TRY")
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson("/api/v1/kasalar", new { ad, subeId = k.SubeId, paraBirimi = doviz });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var id = (await cevap.Json()).GetProperty("id").GetString()!;
        if (acilis != 0m)
        {
            using var db = f.Vt.OlusturSistem();
            var kasa = await db.Kasalar.SingleAsync(x => x.Id == id);
            kasa.Bakiye = acilis;
            await db.SaveChangesAsync();
        }
        return id;
    }

    private async Task<string> HesapAc(Kiraci k, string ad, string doviz = "TRY")
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/bankalar", new
        {
            bankaAdi = "Test Bankası", hesapAdi = ad,
            iban = TestIban.Uret(),
            paraBirimi = doviz,
        });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        return (await cevap.Json()).GetProperty("id").GetString()!;
    }

    private async Task<string> KategoriAc(Kiraci k, string ad)
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/gider-kategorileri", new { ad });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        return (await cevap.Json()).GetProperty("id").GetString()!;
    }

    private async Task<decimal> KasaBakiye(string id)
    {
        using var db = f.Vt.OlusturSistem();
        return (await db.Kasalar.AsNoTracking().SingleAsync(x => x.Id == id)).Bakiye;
    }

    private async Task<decimal> HesapBakiye(string id)
    {
        using var db = f.Vt.OlusturSistem();
        return (await db.BankaHesaplari.AsNoTracking().SingleAsync(x => x.Id == id)).Bakiye;
    }

    private const string Gerekce = "Yanlış kasa seçilerek yapılan aktarım geri alınıyor (test gerekçesi).";

    // ═══════════════ Y-11 — VİRMAN ═══════════════════════════════════════════

    /// <summary>
    /// DoD: virmanın İKİ bacağı TEK transaction'da yazılır ve karşılıklı bağlanır.
    /// MUTASYON HEDEFİ (M19-1): <c>giris.KarsiBacakId = cikis.Id</c> silinirse
    /// bu test KIRMIZI olur.
    /// </summary>
    [Fact]
    public async Task Virman_iki_bacagi_TEK_islemde_yazilir_ve_KARSILIKLI_baglanir()
    {
        var k = YeniKiraci("P19 virman kasa-kasa");
        var kaynak = await KasaAc(k, "Kaynak kasa", 10_000m);
        var hedef = await KasaAc(k, "Hedef kasa");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kaynak,
            hedefTur = "KASA", hedefId = hedef,
            tutar = 2_500m, aciklama = "gün sonu devri",
        });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var g = await cevap.Json();
        var cikisId = g.GetProperty("cikisBacakId").GetString()!;
        var girisId = g.GetProperty("girisBacakId").GetString()!;
        Assert.NotEqual(cikisId, girisId);

        using var db = f.Vt.OlusturSistem();
        var cikis = await db.KasaHareketleri.AsNoTracking().SingleAsync(x => x.Id == cikisId);
        var giris = await db.KasaHareketleri.AsNoTracking().SingleAsync(x => x.Id == girisId);

        // KARŞILIKLILIK: A → B ve B → A
        Assert.Equal(girisId, cikis.KarsiBacakId);
        Assert.Equal(cikisId, giris.KarsiBacakId);
        // İŞARET: çıkış negatif, giriş pozitif, mutlak değerler eşit
        Assert.Equal(-2_500m, cikis.Tutar);
        Assert.Equal(2_500m, giris.Tutar);
        // BAKİYE: toplam etki SIFIR
        Assert.Equal(7_500m, await KasaBakiye(kaynak));
        Assert.Equal(2_500m, await KasaBakiye(hedef));
    }

    /// <summary>DoD: virman CİROYA GİRMEZ — `hareketler` defterine satır yazılmaz.</summary>
    [Fact]
    public async Task Virman_ciroya_GIRMEZ_defter_satiri_yazilmaz()
    {
        var k = YeniKiraci("P19 virman ciro");
        var kaynak = await KasaAc(k, "Kaynak", 5_000m);
        var hedef = await KasaAc(k, "Hedef");

        int onceki;
        using (var db0 = f.Vt.OlusturSistem())
            onceki = await db0.Hareketler.CountAsync(h => h.TenantId == k.TenantId);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kaynak, hedefTur = "KASA", hedefId = hedef, tutar = 1_000m,
        });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(onceki, await db.Hareketler.CountAsync(h => h.TenantId == k.TenantId));
        // Gider de DEĞİLDİR: kategori boştur.
        Assert.Empty(await db.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.KarsiBacakId != null && h.GiderKategoriId != null)
            .ToListAsync());
    }

    /// <summary>Kasa → banka: iki FARKLI tabloya yazılan bacaklar da eşleşir.</summary>
    [Fact]
    public async Task Virman_kasadan_bankaya_her_iki_bakiyeyi_dogru_isler()
    {
        var k = YeniKiraci("P19 virman kasa-banka");
        var kasa = await KasaAc(k, "Nakit kasa", 8_000m);
        var hesap = await HesapAc(k, "Vadesiz");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kasa, hedefTur = "BANKA", hedefId = hesap, tutar = 3_000m,
        });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var g = await cevap.Json();

        Assert.Equal(5_000m, await KasaBakiye(kasa));
        Assert.Equal(3_000m, await HesapBakiye(hesap));

        using var db = f.Vt.OlusturSistem();
        var girisId = g.GetProperty("girisBacakId").GetString()!;
        var dekont = await db.BankaHareketleri.AsNoTracking().SingleAsync(x => x.Id == girisId);
        Assert.Equal("GELEN", dekont.Yon);
        Assert.Equal(g.GetProperty("cikisBacakId").GetString(), dekont.KarsiBacakId);
        // Virman bacağı eşleşme bekleyen dekont listesine DÜŞMEZ (faturalanamaz).
        Assert.Equal("ESLESTI", dekont.Durum);
    }

    /// <summary>Virman bacağı FATURAYA DÖNÜŞTÜRÜLEMEZ (para dışarıdan gelmedi).</summary>
    [Fact]
    public async Task Virman_bacagi_faturaya_DONUSTURULEMEZ()
    {
        var k = YeniKiraci("P19 virman fatura");
        var kasa = await KasaAc(k, "Nakit", 4_000m);
        var hesap = await HesapAc(k, "Vadesiz");

        var v = await (await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kasa, hedefTur = "BANKA", hedefId = hesap, tutar = 1_000m,
        })).Json();
        var girisId = v.GetProperty("girisBacakId").GetString()!;

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/bankalar/hareketler/{girisId}/faturaya-donustur", new { cariId = k.CariId });
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    [Fact]
    public async Task Virman_para_birimleri_farkliysa_REDDEDILIR()
    {
        var k = YeniKiraci("P19 virman doviz");
        var kaynak = await KasaAc(k, "TL kasa", 10_000m);
        var hedef = await KasaAc(k, "USD kasa", 0m, "USD");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kaynak, hedefTur = "KASA", hedefId = hedef, tutar = 100m,
        });
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Equal(10_000m, await KasaBakiye(kaynak));   // hiçbir bacak yazılmadı
    }

    [Fact]
    public async Task Virman_kaynak_ve_hedef_AYNIYSA_reddedilir()
    {
        var k = YeniKiraci("P19 virman ayni");
        var kasa = await KasaAc(k, "Tek kasa", 5_000m);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kasa, hedefTur = "KASA", hedefId = kasa, tutar = 100m,
        });
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    [Fact]
    public async Task Virman_kasa_bakiyesi_yetersizse_reddedilir_ve_hicbir_bacak_yazilmaz()
    {
        var k = YeniKiraci("P19 virman bakiye");
        var kaynak = await KasaAc(k, "Az kasa", 100m);
        var hedef = await KasaAc(k, "Hedef kasa");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kaynak, hedefTur = "KASA", hedefId = hedef, tutar = 500m,
        });
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.Empty(await db.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.KarsiBacakId != null).ToListAsync());
        Assert.Equal(100m, await KasaBakiye(kaynak));
        Assert.Equal(0m, await KasaBakiye(hedef));
    }

    /// <summary>
    /// DoD: virman stornolanırsa İKİ BACAK BİRLİKTE döner.
    /// MUTASYON HEDEFİ (M19-2): storno yolunda ikinci ters bacak yazılmazsa KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Virman_stornosu_IKI_BACAGI_birlikte_dondurur()
    {
        var k = YeniKiraci("P19 virman storno");
        var kaynak = await KasaAc(k, "Kaynak", 6_000m);
        var hedef = await KasaAc(k, "Hedef");

        var v = await (await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kaynak, hedefTur = "KASA", hedefId = hedef, tutar = 2_000m,
        })).Json();
        var cikisId = v.GetProperty("cikisBacakId").GetString()!;

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/virman/{cikisId}/storno", new { gerekce = Gerekce });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var s = await cevap.Json();
        Assert.Equal(2, s.GetProperty("bacakSayisi").GetInt32());
        Assert.Equal(2, s.GetProperty("stornoBacakIdleri").GetArrayLength());

        // BAKİYELER BAŞLANGICA DÖNER
        Assert.Equal(6_000m, await KasaBakiye(kaynak));
        Assert.Equal(0m, await KasaBakiye(hedef));

        using var db = f.Vt.OlusturSistem();
        var tersler = await db.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.StornoKaynakId != null).ToListAsync();
        Assert.Equal(2, tersler.Count);
        // Ters satırlar da bir ÇİFTTİR (karşılıklı bağlı).
        Assert.All(tersler, t => Assert.NotNull(t.KarsiBacakId));
        Assert.Equal(tersler[0].Id, tersler[1].KarsiBacakId);
        Assert.Equal(tersler[1].Id, tersler[0].KarsiBacakId);
        // DEFTER YERİNDE DEĞİŞMEZ: kaynak satırlara dokunulmadı.
        var kaynakSatir = await db.KasaHareketleri.AsNoTracking().SingleAsync(x => x.Id == cikisId);
        Assert.Equal(-2_000m, kaynakSatir.Tutar);
    }

    [Fact]
    public async Task Virman_ayni_virman_IKI_KEZ_stornolanamaz()
    {
        var k = YeniKiraci("P19 cift storno");
        var kaynak = await KasaAc(k, "Kaynak", 6_000m);
        var hedef = await KasaAc(k, "Hedef");
        var v = await (await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kaynak, hedefTur = "KASA", hedefId = hedef, tutar = 1_000m,
        })).Json();
        var cikisId = v.GetProperty("cikisBacakId").GetString()!;

        Assert.Equal(HttpStatusCode.OK, (await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/virman/{cikisId}/storno", new { gerekce = Gerekce })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/virman/{cikisId}/storno", new { gerekce = Gerekce })).StatusCode);
    }

    /// <summary>
    /// DoD: virmanın TEK bacağı kasa storno ucundan geri alınamaz — yoksa
    /// paranın yalnız yarısı döner.
    /// MUTASYON HEDEFİ (M19-3): StornoServisi'ndeki `KarsiBacakId is not null`
    /// kapısı silinirse bu test KIRMIZI olur.
    /// </summary>
    [Fact]
    public async Task Virman_bacagi_TEK_BASINA_stornolanamaz()
    {
        var k = YeniKiraci("P19 tek bacak storno");
        var kaynak = await KasaAc(k, "Kaynak", 6_000m);
        var hedef = await KasaAc(k, "Hedef");
        var v = await (await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kaynak, hedefTur = "KASA", hedefId = hedef, tutar = 1_500m,
        })).Json();
        var cikisId = v.GetProperty("cikisBacakId").GetString()!;

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/kasalar/{kaynak}/hareket/{cikisId}/storno", new { gerekce = Gerekce });
        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
        Assert.Equal(4_500m, await KasaBakiye(kaynak));   // yarım dönüş OLMADI
    }

    /// <summary>Banka dekontu bacağı da tek başına stornolanamaz (kasa ile AYNI kural).</summary>
    [Fact]
    public async Task Banka_virman_bacagi_TEK_BASINA_stornolanamaz()
    {
        var k = YeniKiraci("P19 banka tek bacak");
        var kasa = await KasaAc(k, "Nakit", 4_000m);
        var hesap = await HesapAc(k, "Vadesiz");
        var v = await (await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kasa, hedefTur = "BANKA", hedefId = hesap, tutar = 1_000m,
        })).Json();
        var girisId = v.GetProperty("girisBacakId").GetString()!;

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/bankalar/{hesap}/hareketler/{girisId}/storno", new { gerekce = Gerekce });
        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
        Assert.Equal(1_000m, await HesapBakiye(hesap));
    }

    /// <summary>Virman OLMAYAN satır virman storno ucundan geri alınamaz.</summary>
    [Fact]
    public async Task Virman_olmayan_satir_virman_stornosu_ALMAZ()
    {
        var k = YeniKiraci("P19 virman degil");
        var kasa = await KasaAc(k, "Nakit", 3_000m);
        var h = await (await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/kasalar/{kasa}/hareket", new { yon = "CIKIS", tutar = 100m, odemeTuru = "NAKIT" })).Json();

        using var db = f.Vt.OlusturSistem();
        var satir = await db.KasaHareketleri.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId && x.KasaId == kasa).SingleAsync();

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/virman/{satir.Id}/storno", new { gerekce = Gerekce });
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.NotEqual(default, h.ValueKind);
    }

    [Fact]
    public async Task Virman_storno_gerekcesi_20_karakterden_kisaysa_reddedilir()
    {
        var k = YeniKiraci("P19 gerekce");
        var kaynak = await KasaAc(k, "Kaynak", 2_000m);
        var hedef = await KasaAc(k, "Hedef");
        var v = await (await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kaynak, hedefTur = "KASA", hedefId = hedef, tutar = 500m,
        })).Json();

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/virman/{v.GetProperty("cikisBacakId").GetString()}/storno", new { gerekce = "kısa" });
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    // ═══════════════ Y-12 — GİDER ════════════════════════════════════════════

    [Fact]
    public async Task Gider_kasadan_duser_ve_KATEGORILI_yazilir()
    {
        var k = YeniKiraci("P19 gider temel");
        var kasa = await KasaAc(k, "Nakit", 10_000m);
        var kategori = await KategoriAc(k, "Kira");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/giderler", new
        {
            kasaId = kasa, giderKategoriId = kategori, tutar = 3_000m, aciklama = "Ekim kirası",
        });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var g = await cevap.Json();
        Assert.Equal(7_000m, g.GetProperty("kasaBakiyesi").GetDecimal());

        using var db = f.Vt.OlusturSistem();
        var satir = await db.KasaHareketleri.AsNoTracking()
            .SingleAsync(x => x.Id == g.GetProperty("id").GetString());
        Assert.Equal(-3_000m, satir.Tutar);           // ÇIKIŞ: negatif
        Assert.Equal(kategori, satir.GiderKategoriId);
        Assert.Null(satir.KarsiBacakId);              // gider virman DEĞİLDİR
        Assert.Equal(7_000m, await KasaBakiye(kasa));
    }

    [Fact]
    public async Task Gider_kategorisiz_yazilamaz()
    {
        var k = YeniKiraci("P19 gider kategorisiz");
        var kasa = await KasaAc(k, "Nakit", 5_000m);

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson("/api/v1/giderler", new { kasaId = kasa, tutar = 100m });
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    [Fact]
    public async Task Gider_PASIF_kategoriye_yazilamaz()
    {
        var k = YeniKiraci("P19 gider pasif");
        var kasa = await KasaAc(k, "Nakit", 5_000m);
        var kategori = await KategoriAc(k, "Eski kalem");

        Assert.Equal(HttpStatusCode.OK,
            (await f.Ortam.Istemci(k, "SAHIP").DeleteAsync($"/api/v1/gider-kategorileri/{kategori}")).StatusCode);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/giderler", new
        {
            kasaId = kasa, giderKategoriId = kategori, tutar = 100m,
        });
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    [Fact]
    public async Task Gider_kasa_bakiyesinden_fazlaysa_reddedilir()
    {
        var k = YeniKiraci("P19 gider bakiye");
        var kasa = await KasaAc(k, "Az nakit", 50m);
        var kategori = await KategoriAc(k, "Elektrik");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/giderler", new
        {
            kasaId = kasa, giderKategoriId = kategori, tutar = 100m,
        });
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Equal(50m, await KasaBakiye(kasa));
    }

    /// <summary>
    /// DoD: gider kâr-zararda AYRI SATIRDIR ve dönem kârından DÜŞÜLÜR.
    /// MUTASYON HEDEFİ (M19-4): <c>donemKariTl = karTl + iscilikTl - giderTl</c>
    /// ifadesinden <c>- giderTl</c> silinirse bu test KIRMIZI olur.
    /// </summary>
    [Fact]
    public async Task Gider_kar_zararda_AYRI_SATIR_ve_donem_karindan_duser()
    {
        var k = YeniKiraci("P19 gider karzarar");
        var kasa = await KasaAc(k, "Nakit", 20_000m);
        var kategori = await KategoriAc(k, "Personel");

        Assert.Equal(HttpStatusCode.OK, (await f.Ortam.Istemci(k, "SAHIP").PostJson(
            "/api/v1/giderler", new { kasaId = kasa, giderKategoriId = kategori, tutar = 4_000m })).StatusCode);

        var bugun = Kuyumcu.Api.Common.Zaman.YerelBugun();
        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .GetAsync($"/api/v1/raporlar/kar-zarar?baslangic={bugun:yyyy-MM-dd}&bitis={bugun:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var r = await cevap.Json();

        Assert.Equal(4_000m, r.GetProperty("giderTl").GetDecimal());
        Assert.Equal(-4_000m, r.GetProperty("donemKariTl").GetDecimal());

        // Kâr köprüsünde GIDER kalemi ADIYLA durur ve işaretlidir.
        var koprü = r.GetProperty("karKoprusu").EnumerateArray()
            .Single(x => x.GetProperty("kod").GetString() == "GIDER");
        Assert.Equal(-4_000m, koprü.GetProperty("tutarTl").GetDecimal());

        // Kırılım kategori ADIYLA döner (UX-S31: ham kimlik ekrana basılmaz).
        var kirilim = r.GetProperty("giderKirilimi").EnumerateArray().Single();
        Assert.Equal("Personel", kirilim.GetProperty("kategoriAdi").GetString());
        Assert.Equal(1, kirilim.GetProperty("adet").GetInt32());
    }

    /// <summary>
    /// Stornolanan gider kâr-zarardan KENDİLİĞİNDEN düşer: ters satır AYNI
    /// kategoriyi taşır, ikinci bir süzgeç yazılmaz.
    /// MUTASYON HEDEFİ (M19-5): storno satırında
    /// <c>GiderKategoriId = kaynak.GiderKategoriId</c> silinirse KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Stornolanan_gider_kar_zarar_toplamindan_DUSER()
    {
        var k = YeniKiraci("P19 gider storno kz");
        var kasa = await KasaAc(k, "Nakit", 20_000m);
        var kategori = await KategoriAc(k, "Nakliye");

        var g = await (await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/giderler",
            new { kasaId = kasa, giderKategoriId = kategori, tutar = 1_500m })).Json();
        var satirId = g.GetProperty("id").GetString()!;

        Assert.Equal(HttpStatusCode.OK, (await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/kasalar/{kasa}/hareket/{satirId}/storno", new { gerekce = Gerekce })).StatusCode);

        // Ters satır AYNI kategoriyi taşır.
        using (var db = f.Vt.OlusturSistem())
        {
            var ters = await db.KasaHareketleri.AsNoTracking()
                .SingleAsync(x => x.TenantId == k.TenantId && x.StornoKaynakId == satirId);
            Assert.Equal(kategori, ters.GiderKategoriId);
            Assert.Equal(1_500m, ters.Tutar);
        }

        var bugun = Kuyumcu.Api.Common.Zaman.YerelBugun();
        var r = await (await f.Ortam.Istemci(k, "SAHIP")
            .GetAsync($"/api/v1/raporlar/kar-zarar?baslangic={bugun:yyyy-MM-dd}&bitis={bugun:yyyy-MM-dd}")).Json();
        Assert.Equal(0m, r.GetProperty("giderTl").GetDecimal());
        Assert.Equal(20_000m, await KasaBakiye(kasa));
    }

    // ═══════════════ Y-12 — KATEGORİ (DoD sözleşmesi) ════════════════════════

    [Fact]
    public async Task Kategori_ayni_adla_IKI_KEZ_acilamaz()
    {
        var k = YeniKiraci("P19 kategori benzersiz");
        await KategoriAc(k, "Kira");
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/gider-kategorileri", new { ad = "Kira" });
        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
    }

    /// <summary>PATCH sözleşmesi: gövdede OLMAYAN alan KORUNUR.</summary>
    [Fact]
    public async Task Kategori_PATCH_gonderilmeyen_alani_KORUR()
    {
        var k = YeniKiraci("P19 kategori patch");
        var cevap0 = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson("/api/v1/gider-kategorileri", new { ad = "Kira", aciklama = "işletme kirası" });
        var id = (await cevap0.Json()).GetProperty("id").GetString()!;

        // YALNIZ ad gönderilir → açıklama KORUNUR
        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/gider-kategorileri/{id}", new { ad = "İşletme kirası" });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        var kat = await db.GiderKategorileri.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("İşletme kirası", kat.Ad);
        Assert.Equal("işletme kirası", kat.Aciklama);
    }

    /// <summary>PATCH sözleşmesi: null gönderilen alan SİLİNİR.</summary>
    [Fact]
    public async Task Kategori_PATCH_null_gonderilen_alani_SILER()
    {
        var k = YeniKiraci("P19 kategori patch null");
        var id = (await (await f.Ortam.Istemci(k, "SAHIP").PostJson(
            "/api/v1/gider-kategorileri", new { ad = "Elektrik", aciklama = "fatura" })).Json())
            .GetProperty("id").GetString()!;

        Assert.Equal(HttpStatusCode.OK, (await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/gider-kategorileri/{id}", new { aciklama = (string?)null })).StatusCode);

        using var db = f.Vt.OlusturSistem();
        var kat = await db.GiderKategorileri.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("Elektrik", kat.Ad);
        Assert.Null(kat.Aciklama);
    }

    /// <summary>DELETE = PASİFE ALMA. Satır SİLİNMEZ; aktife alma yolu vardır.</summary>
    [Fact]
    public async Task Kategori_HARD_DELETE_edilmez_pasife_alinir_ve_geri_aktiflesir()
    {
        var k = YeniKiraci("P19 kategori pasif");
        var id = await KategoriAc(k, "Geçici kalem");

        Assert.Equal(HttpStatusCode.OK,
            (await f.Ortam.Istemci(k, "SAHIP").DeleteAsync($"/api/v1/gider-kategorileri/{id}")).StatusCode);

        using (var db = f.Vt.OlusturSistem())
        {
            var kat = await db.GiderKategorileri.AsNoTracking().SingleAsync(x => x.Id == id);
            Assert.False(kat.Aktif);   // SATIR DURUYOR
        }

        Assert.Equal(HttpStatusCode.OK, (await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/gider-kategorileri/{id}", new { aktif = true })).StatusCode);

        using var db2 = f.Vt.OlusturSistem();
        Assert.True((await db2.GiderKategorileri.AsNoTracking().SingleAsync(x => x.Id == id)).Aktif);
    }

    /// <summary>UX-S23: kategori listesi kaydın ETKİSİNİ (adet + tutar) döndürür.</summary>
    [Fact]
    public async Task Kategori_listesi_ETKIYI_dondurur()
    {
        var k = YeniKiraci("P19 kategori etki");
        var kasa = await KasaAc(k, "Nakit", 9_000m);
        var id = await KategoriAc(k, "Temizlik");
        await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/giderler",
            new { kasaId = kasa, giderKategoriId = id, tutar = 700m });

        var r = await (await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/gider-kategorileri")).Json();
        var kat = r.GetProperty("kategoriler").EnumerateArray().Single(x => x.GetProperty("id").GetString() == id);
        Assert.Equal(1, kat.GetProperty("giderAdedi").GetInt32());
        Assert.Equal(700m, kat.GetProperty("giderToplamTl").GetDecimal());
    }

    /// <summary>TEZGÂHTAR gider yazamaz (rol kapısı sunucudadır).</summary>
    [Fact]
    public async Task Tezgahtar_gider_YAZAMAZ()
    {
        var k = YeniKiraci("P19 rol");
        var kasa = await KasaAc(k, "Nakit", 5_000m);
        var kategori = await KategoriAc(k, "Kira");

        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson("/api/v1/giderler", new
        {
            kasaId = kasa, giderKategoriId = kategori, tutar = 100m,
        });
        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    /// <summary>Kiracı izolasyonu: başka kiracının kasasına virman yapılamaz.</summary>
    [Fact]
    public async Task Baska_kiracinin_kasasina_virman_YAPILAMAZ()
    {
        var a = YeniKiraci("P19 izolasyon A");
        var b = YeniKiraci("P19 izolasyon B");
        var kaynak = await KasaAc(a, "A kasa", 5_000m);
        var yabanci = await KasaAc(b, "B kasa", 0m);

        var cevap = await f.Ortam.Istemci(a, "SAHIP").PostJson("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kaynak, hedefTur = "KASA", hedefId = yabanci, tutar = 100m,
        });
        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        Assert.Equal(5_000m, await KasaBakiye(kaynak));
    }
}
