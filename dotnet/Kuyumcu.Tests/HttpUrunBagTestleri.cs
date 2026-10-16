using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ÜRÜN BAĞ ALANLARI — ÜÇ DURUMLU PATCH SÖZLEŞMESİ (paket UX-1-C1).
//
// UX-1-B raporu § açık kalanlar (a): "PATCH /urunler/{id} gövdesinde
// reyonId:null ürünün reyon bağını TEMİZLEMİYOR (null = 'dokunma' işleniyor)".
// Ekran, reyon seçicisi boşaltıldığında gövdeye `reyonId: null` koyuyordu —
// yani kullanıcının SİLME kararı sunucuya "dokunma" diye ulaşıyordu. Bu bir
// SESSİZ ETKİSİZLİKTİR ve DoD'de yasaktır.
//
// Sözleşme artık üçe ayrılır ve burada UÇTAN UCA kanıtlanır:
//   • alan gövdede YOK      → KORU
//   • alan gövdede null/""  → SİL  (veritabanında NULL)
//   • alan gövdede değer    → ATA  (doğrulanır, yoksa 404)
//
// ⚠ NEDEN SERVİS TESTİ YETMEZ: üç durum ayrımı JSON MODEL BAĞLAMAYA dayanır
//   (setter çağrıldı mı?). Servise elle DTO verilirse "gönderilmedi" durumu
//   hiç oluşmaz; ayrım ancak gerçek HTTP gövdesiyle görülür.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class HttpUrunBagTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    /// <summary>Kiracıya bir reyon açar ve ürünü ona bağlar.</summary>
    private async Task<string> ReyonluUrun(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var reyon = new Reyon
        {
            Id = Kuyumcu.Api.Common.Kimlik.Ulid(),
            TenantId = k.TenantId,
            SubeId = k.SubeId,
            Ad = "C1 Vitrin",
            Tip = "VITRIN",
            Aktif = true,
            Olusturuldu = DateTime.UtcNow,
            Guncellendi = DateTime.UtcNow,
        };
        db.Set<Reyon>().Add(reyon);
        var urun = await db.Urunler.FirstAsync(u => u.Id == k.UrunId && u.TenantId == k.TenantId);
        urun.ReyonId = reyon.Id;
        await db.SaveChangesAsync();
        return reyon.Id;
    }

    private async Task<string?> ReyonIdOku(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return await db.Urunler.AsNoTracking()
            .Where(u => u.Id == k.UrunId && u.TenantId == k.TenantId)
            .Select(u => u.ReyonId).SingleAsync();
    }

    private static Task<HttpResponseMessage> Patch(HttpClient c, string yol, object govde)
        => c.PatchAsJsonAsync(yol, govde);

    // ── (1) GÖNDERİLMEDİ → KORU ──────────────────────────────────────────────

    [Fact]
    public async Task reyonId_GONDERILMEZSE_bag_KORUNUR()
    {
        var k = YeniKiraci("C1 reyon koru");
        var reyonId = await ReyonluUrun(k);

        var cevap = await Patch(f.Ortam.Istemci(k, "SAHIP"),
            $"/api/v1/urunler/{k.UrunId}", new { ad = "Yalnız ad değişti" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.Equal(reyonId, await ReyonIdOku(k));   // bağ DURUYOR
    }

    // ── (2) null GÖNDERİLDİ → SİL ────────────────────────────────────────────

    [Fact]
    public async Task reyonId_NULL_gonderilince_bag_SILINIR()
    {
        var k = YeniKiraci("C1 reyon sil");
        await ReyonluUrun(k);

        // Ekranın gerçekte gönderdiği gövde: seçici boşaltıldı → null.
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchAsync(
            $"/api/v1/urunler/{k.UrunId}",
            new StringContent("""{"reyonId":null}""", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.Null(await ReyonIdOku(k));   // SQL: reyon_id IS NULL
    }

    [Fact]
    public async Task reyonId_BOS_DIZGE_gonderilince_de_bag_SILINIR()
    {
        var k = YeniKiraci("C1 reyon bos dizge");
        await ReyonluUrun(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchAsync(
            $"/api/v1/urunler/{k.UrunId}",
            new StringContent("""{"reyonId":""}""", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.Null(await ReyonIdOku(k));
    }

    // ── (3) DEĞER GÖNDERİLDİ → ATA (ve doğrulanır) ───────────────────────────

    [Fact]
    public async Task reyonId_DEGER_gonderilince_ATANIR()
    {
        var k = YeniKiraci("C1 reyon ata");
        var reyonId = await ReyonluUrun(k);

        // Önce temizle, sonra yeniden ata: iki yön de çalışmalı.
        await f.Ortam.Istemci(k, "SAHIP").PatchAsync(
            $"/api/v1/urunler/{k.UrunId}",
            new StringContent("""{"reyonId":null}""", System.Text.Encoding.UTF8, "application/json"));
        Assert.Null(await ReyonIdOku(k));

        var cevap = await Patch(f.Ortam.Istemci(k, "SAHIP"),
            $"/api/v1/urunler/{k.UrunId}", new { reyonId });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.Equal(reyonId, await ReyonIdOku(k));
    }

    [Fact]
    public async Task YABANCI_reyon_atanamaz_404()
    {
        var k = YeniKiraci("C1 reyon yabanci");
        var yabanci = YeniKiraci("C1 reyon yabanci kaynak");
        var yabanciReyon = await ReyonluUrun(yabanci);

        var cevap = await Patch(f.Ortam.Istemci(k, "SAHIP"),
            $"/api/v1/urunler/{k.UrunId}", new { reyonId = yabanciReyon });

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        Assert.Null(await ReyonIdOku(k));   // kiracı sızıntısı YOK
    }

    // ── Kategori aynı sözleşmeyi taşır (ikinci kalıp açılmadı) ───────────────

    [Fact]
    public async Task kategoriId_NULL_gonderilince_bag_SILINIR_gonderilmezse_KORUNUR()
    {
        var k = YeniKiraci("C1 kategori uc durum");
        string kategoriId;
        using (var db = f.Vt.OlusturSistem())
        {
            var kategori = new UrunKategorisi
            {
                Id = Kuyumcu.Api.Common.Kimlik.Ulid(),
                TenantId = k.TenantId,
                Ad = "C1 Ziynet",
                Sira = 0,
                Aktif = true,
                Olusturuldu = DateTime.UtcNow,
                Guncellendi = DateTime.UtcNow,
            };
            db.Set<UrunKategorisi>().Add(kategori);
            var u = await db.Urunler.FirstAsync(x => x.Id == k.UrunId && x.TenantId == k.TenantId);
            u.KategoriId = kategori.Id;
            await db.SaveChangesAsync();
            kategoriId = kategori.Id;
        }

        // Gönderilmedi → korunur
        await Patch(f.Ortam.Istemci(k, "SAHIP"), $"/api/v1/urunler/{k.UrunId}", new { ad = "ad" });
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(kategoriId, await db.Urunler.AsNoTracking()
                .Where(x => x.Id == k.UrunId).Select(x => x.KategoriId).SingleAsync());

        // null → silinir
        await f.Ortam.Istemci(k, "SAHIP").PatchAsync(
            $"/api/v1/urunler/{k.UrunId}",
            new StringContent("""{"kategoriId":null}""", System.Text.Encoding.UTF8, "application/json"));
        using (var db = f.Vt.OlusturSistem())
            Assert.Null(await db.Urunler.AsNoTracking()
                .Where(x => x.Id == k.UrunId).Select(x => x.KategoriId).SingleAsync());
    }

    // ── Ekranın kullandığı diğer iki uç: aktife alma ve reyon düzenleme ──────

    [Fact]
    public async Task PATCH_aktif_true_pasif_urunu_GERI_ACAR()
    {
        var k = YeniKiraci("C1 urun aktife al");
        var silme = await f.Ortam.Istemci(k, "SAHIP").DeleteAsync($"/api/v1/urunler/{k.UrunId}");
        Assert.Equal(HttpStatusCode.OK, silme.StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.False(await db.Urunler.AsNoTracking()
                .Where(x => x.Id == k.UrunId).Select(x => x.Aktif).SingleAsync());

        var cevap = await Patch(f.Ortam.Istemci(k, "SAHIP"),
            $"/api/v1/urunler/{k.UrunId}", new { aktif = true });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.True(await db.Urunler.AsNoTracking()
                .Where(x => x.Id == k.UrunId).Select(x => x.Aktif).SingleAsync());
    }

    [Fact]
    public async Task TEZGAHTAR_reyon_duzenleyemez_403()
    {
        // Ekranın "Düzenle" düğmesi rolle gizlenmez; kapı SUNUCUDADIR.
        var k = YeniKiraci("C1 reyon rol");
        var reyonId = await ReyonluUrun(k);

        var cevap = await Patch(f.Ortam.Istemci(k, "TEZGAHTAR"),
            $"/api/v1/reyonlar/{reyonId}", new { ad = "Tezgâhtar denedi" });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal("C1 Vitrin", await db.Set<Reyon>().AsNoTracking()
            .Where(r => r.Id == reyonId).Select(r => r.Ad).SingleAsync());
    }

    [Fact]
    public async Task Reyon_pasife_alinir_ve_PATCH_aktif_true_ile_GERI_ACILIR()
    {
        var k = YeniKiraci("C1 reyon pasif dongu");
        var reyonId = await ReyonluUrun(k);

        // Üzerinde ürün varken pasife alma İŞ KURALI ile reddedilir (403 değil, 409).
        var engel = await f.Ortam.Istemci(k, "SAHIP").DeleteAsync($"/api/v1/reyonlar/{reyonId}");
        Assert.Equal(HttpStatusCode.Conflict, engel.StatusCode);

        // Ürünün bağı çözülünce pasife alınabilir.
        await f.Ortam.Istemci(k, "SAHIP").PatchAsync(
            $"/api/v1/urunler/{k.UrunId}",
            new StringContent("""{"reyonId":null}""", System.Text.Encoding.UTF8, "application/json"));

        var sil = await f.Ortam.Istemci(k, "SAHIP").DeleteAsync($"/api/v1/reyonlar/{reyonId}");
        Assert.Equal(HttpStatusCode.OK, sil.StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.False(await db.Set<Reyon>().AsNoTracking()
                .Where(r => r.Id == reyonId).Select(r => r.Aktif).SingleAsync());

        // GERİ DÖNÜŞ YOLU (UX-S25): PATCH {aktif:true}
        var geri = await Patch(f.Ortam.Istemci(k, "SAHIP"),
            $"/api/v1/reyonlar/{reyonId}", new { aktif = true });
        Assert.Equal(HttpStatusCode.OK, geri.StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.True(await db.Set<Reyon>().AsNoTracking()
                .Where(r => r.Id == reyonId).Select(r => r.Aktif).SingleAsync());
    }

    // ── Cari: ekranın "Aktife al" düğmesinin sunucu karşılığı ───────────────

    [Fact]
    public async Task Cari_pasife_alinir_ve_PATCH_aktif_true_ile_GERI_ACILIR()
    {
        var k = YeniKiraci("C1 cari pasif dongu");

        var sil = await f.Ortam.Istemci(k, "SAHIP").DeleteAsync($"/api/v1/cari/{k.CariId}");
        Assert.Equal(HttpStatusCode.OK, sil.StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.False(await db.Cariler.AsNoTracking()
                .Where(c => c.Id == k.CariId).Select(c => c.Aktif).SingleAsync());

        // Liste varsayılanı aktiftir: pasif cari `aktif=true` süzgecinde GÖRÜNMEZ.
        var liste = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/cari?aktif=true&limit=100");
        Assert.Equal(HttpStatusCode.OK, liste.StatusCode);
        Assert.DoesNotContain(k.CariId, await liste.Metin());

        var geri = await Patch(f.Ortam.Istemci(k, "SAHIP"),
            $"/api/v1/cari/{k.CariId}", new { aktif = true });
        Assert.Equal(HttpStatusCode.OK, geri.StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.True(await db.Cariler.AsNoTracking()
                .Where(c => c.Id == k.CariId).Select(c => c.Aktif).SingleAsync());
    }
}
