using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q9-1 — BANKA DEKONTU: TARİH + DURUM MAKİNESİ + STORNO BAKİYESİ
// [HTTP, gerçek MySQL] (QA turu 9: 2 KRİTİK + 1 YÜKSEK)
//
// SÖZLEŞME:
//   • Dekont GEÇMİŞ tarihle girilir; tarih PATCH ile düzeltilir (üç durumlu:
//     gönderilmeyen alan korunur); denetim izi eski → yeni yazılır.
//   • BEKLIYOR → ESLESTI yalnız POST …/eslestir ile (müşteri seçilir); ESLESTI
//     geri BEKLIYOR'a DÖNMEZ; stornolu / storno satırı / virman eşleşmez.
//   • MASAK (fail-closed, mevcut kapı): eşleştirme müşterinin bağlantılı işlem
//     toplamını kimlik eşiğine taşıyorsa REDDEDİLİR, satır DEĞİŞMEZ.
//   • Storno sonrası GET /bankalar bakiyesi yeni tutardır (ekran bunu okur).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class Q91BankaDekontTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private async Task<string> HesapAc(Kiraci k)
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/bankalar", new
        {
            bankaAdi = "Ziraat", hesapAdi = "Ana Hesap",
            iban = TestIban.Tamamla(new string('1', 22)), paraBirimi = "TRY",
        });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        return (await cevap.Json()).GetProperty("id").GetString()!;
    }

    private async Task<string> DekontGir(Kiraci k, string hesapId, object govde)
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson($"/api/v1/bankalar/{hesapId}/hareket", govde);
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        return (await cevap.Json()).GetProperty("hareketId").GetString()!;
    }

    private Task<HttpResponseMessage> Eslestir(Kiraci k, string hesapId, string id, string? cariId, string rol = "SAHIP") =>
        f.Ortam.Istemci(k, rol).PostJson($"/api/v1/bankalar/{hesapId}/hareketler/{id}/eslestir", new { cariId });

    private const string Gerekce = "Yanlis hesaba girildi, duzeltiliyor Q91.";

    // ── K1 — TARİH ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Q91_dekont_GECMIS_tarihle_girilir_ve_oyle_saklanir()
    {
        var k = YeniKiraci("PAKETQ91 tarih giris");
        var hesapId = await HesapAc(k);
        var gun = DateTime.UtcNow.Date.AddDays(-5);
        var id = await DekontGir(k, hesapId, new
        {
            yon = "GELEN", tutar = 700m, aciklama = "gecmis",
            tarih = gun.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) + "T12:00:00Z",
        });

        using var db = f.Vt.OlusturSistem();
        var h = await db.BankaHareketleri.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(gun, h.Tarih.Date);
        Assert.Equal(12, h.Tarih.Hour);
    }

    [Fact]
    public async Task Q91_dekont_tarihi_PATCH_ile_DUZELTILIR_diger_alanlar_KORUNUR_iz_yazilir()
    {
        var k = YeniKiraci("PAKETQ91 tarih duzelt");
        var hesapId = await HesapAc(k);
        var id = await DekontGir(k, hesapId, new { yon = "GELEN", tutar = 900m, karsiTarafAd = "Ali", aciklama = "not" });
        var yeni = DateTime.UtcNow.Date.AddDays(-3).AddHours(12);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchAsJsonAsync(
            $"/api/v1/bankalar/{hesapId}/hareketler/{id}", new { tarih = yeni });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        var h = await db.BankaHareketleri.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(yeni, h.Tarih);
        Assert.Equal("Ali", h.KarsiTarafAd);     // gövdede yoktu → KORUNDU
        Assert.Equal("not", h.Aciklama);
        Assert.Equal(900m, h.Tutar);

        var iz = await db.DenetimKayitlari.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId && x.VarlikTuru == "BankaHareketiDuzeltme" && x.VarlikId == id)
            .SingleAsync();
        Assert.Contains("tarih:", iz.Detay);
        Assert.Contains(yeni.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), iz.Detay);
    }

    [Fact]
    public async Task Q91_ILERI_tarihe_duzeltme_REDDEDILIR()
    {
        var k = YeniKiraci("PAKETQ91 tarih ileri");
        var hesapId = await HesapAc(k);
        var id = await DekontGir(k, hesapId, new { yon = "GELEN", tutar = 900m });

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchAsJsonAsync(
            $"/api/v1/bankalar/{hesapId}/hareketler/{id}", new { tarih = DateTime.UtcNow.AddDays(5) });
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    // ── K2 — DURUM MAKİNESİ ───────────────────────────────────────────────────

    [Fact]
    public async Task Q91_bekleyen_dekont_musteriyle_ESLESIR_durum_ve_iz_yazilir()
    {
        var k = YeniKiraci("PAKETQ91 eslestir");
        var hesapId = await HesapAc(k);
        var id = await DekontGir(k, hesapId, new { yon = "GELEN", tutar = 1200m });

        var cevap = await Eslestir(k, hesapId, id, k.CariId);
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var g = await cevap.Json();
        Assert.Equal("ESLESTI", g.GetProperty("durum").GetString());
        Assert.Equal("BEKLIYOR", g.GetProperty("oncekiDurum").GetString());

        using var db = f.Vt.OlusturSistem();
        var h = await db.BankaHareketleri.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("ESLESTI", h.Durum);
        Assert.Equal(k.CariId, h.CariId);
        Assert.Equal(1200m, h.Tutar);

        var iz = await db.DenetimKayitlari.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId && x.VarlikTuru == "BankaHareketiEslestirme" && x.VarlikId == id)
            .SingleAsync();
        Assert.Contains("BEKLIYOR → ESLESTI", iz.Detay);
    }

    [Fact]
    public async Task Q91_eslesmis_dekont_IKINCI_KEZ_eslestirilemez()
    {
        var k = YeniKiraci("PAKETQ91 eslestir iki");
        var hesapId = await HesapAc(k);
        var id = await DekontGir(k, hesapId, new { yon = "GELEN", tutar = 1200m, cariId = k.CariId });

        var cevap = await Eslestir(k, hesapId, id, k.CariId);
        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
    }

    [Fact]
    public async Task Q91_stornolanmis_dekont_ve_STORNO_satiri_eslestirilemez()
    {
        var k = YeniKiraci("PAKETQ91 eslestir storno");
        var hesapId = await HesapAc(k);
        var id = await DekontGir(k, hesapId, new { yon = "GELEN", tutar = 1500m });
        var st = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/bankalar/{hesapId}/hareketler/{id}/storno", new { gerekce = Gerekce });
        Assert.Equal(HttpStatusCode.OK, st.StatusCode);
        var tersId = (await st.Json()).GetProperty("stornoBankaHareketId").GetString()!;

        Assert.Equal(HttpStatusCode.Conflict, (await Eslestir(k, hesapId, id, k.CariId)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Eslestir(k, hesapId, tersId, k.CariId)).StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.Null((await db.BankaHareketleri.AsNoTracking().SingleAsync(x => x.Id == id)).CariId);
    }

    [Fact]
    public async Task Q91_bos_musteriyle_eslestirme_400_ve_satir_DEGISMEZ()
    {
        var k = YeniKiraci("PAKETQ91 eslestir bos");
        var hesapId = await HesapAc(k);
        var id = await DekontGir(k, hesapId, new { yon = "GELEN", tutar = 1200m });

        Assert.Equal(HttpStatusCode.BadRequest, (await Eslestir(k, hesapId, id, null)).StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal("BEKLIYOR", (await db.BankaHareketleri.AsNoTracking().SingleAsync(x => x.Id == id)).Durum);
    }

    [Fact]
    public async Task Q91_BASKA_kiracinin_musterisiyle_eslestirilemez()
    {
        var k = YeniKiraci("PAKETQ91 eslestir izolasyon");
        var diger = YeniKiraci("PAKETQ91 eslestir diger");
        var hesapId = await HesapAc(k);
        var id = await DekontGir(k, hesapId, new { yon = "GELEN", tutar = 1200m });

        Assert.Equal(HttpStatusCode.NotFound, (await Eslestir(k, hesapId, id, diger.CariId)).StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal("BEKLIYOR", (await db.BankaHareketleri.AsNoTracking().SingleAsync(x => x.Id == id)).Durum);
    }

    [Fact]
    public async Task Q91_eslestirme_ROL_kapisi_TEZGAHTAR_403()
    {
        var k = YeniKiraci("PAKETQ91 eslestir rol");
        var hesapId = await HesapAc(k);
        var id = await DekontGir(k, hesapId, new { yon = "GELEN", tutar = 1200m });

        Assert.Equal(HttpStatusCode.Forbidden, (await Eslestir(k, hesapId, id, k.CariId, "TEZGAHTAR")).StatusCode);
    }

    /// <summary>
    /// MASAK fail-closed: iki ayrı eşik altı dekont (biri müşterisiz) aynı gün
    /// aynı müşteriye bağlanınca bağlantılı işlem toplamı kimlik eşiğini aşar →
    /// sonradan eşleştirme REDDEDİLİR (eşik bölme yolu kapalı), satır DEĞİŞMEZ.
    /// </summary>
    [Fact]
    public async Task Q91_MASAK_esigini_asan_sonradan_eslestirme_REDDEDILIR()
    {
        var k = YeniKiraci("PAKETQ91 eslestir masak");
        var hesapId = await HesapAc(k);
        var musterisiz = await DekontGir(k, hesapId, new { yon = "GELEN", tutar = 100_000m, aciklama = "a" });
        await DekontGir(k, hesapId, new { yon = "GELEN", tutar = 100_000m, aciklama = "b", cariId = k.CariId });

        var cevap = await Eslestir(k, hesapId, musterisiz, k.CariId);
        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
        Assert.Contains("STORNO", await cevap.Content.ReadAsStringAsync());

        using var db = f.Vt.OlusturSistem();
        var h = await db.BankaHareketleri.AsNoTracking().SingleAsync(x => x.Id == musterisiz);
        Assert.Equal("BEKLIYOR", h.Durum);
        Assert.Null(h.CariId);
    }

    // ── Y1 — STORNO SONRASI BAKİYE ────────────────────────────────────────────

    [Fact]
    public async Task Q91_storno_sonrasi_hesap_listesi_YENI_bakiyeyi_doner()
    {
        var k = YeniKiraci("PAKETQ91 storno bakiye");
        var hesapId = await HesapAc(k);
        await DekontGir(k, hesapId, new { yon = "GELEN", tutar = 1250m, aciklama = "a" });
        var id = await DekontGir(k, hesapId, new { yon = "GELEN", tutar = 300m, aciklama = "b" });

        var st = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/bankalar/{hesapId}/hareketler/{id}/storno", new { gerekce = Gerekce });
        Assert.Equal(HttpStatusCode.OK, st.StatusCode);

        var liste = await (await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/bankalar")).Json();
        var hesap = liste.GetProperty("hesaplar").EnumerateArray().Single(x => x.GetProperty("id").GetString() == hesapId);
        Assert.Equal(1250m, hesap.GetProperty("bakiye").GetDecimal());
    }
}
