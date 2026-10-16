using System.Net;
using Kuyumcu.Api.Common;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 31 — POS UÇLARININ YETKİ KAPILARI [HTTP]
//
// Servis testi rol filtresini ve modül yetkisini GÖREMEZ: ikisi de MVC
// niteliği olarak boru hattında çalışır. Bu dosya o kör noktayı kapatır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class HttpPosTestleri(HttpFikstur fikstur)
{
    private static object CihazGovde(Kiraci k) => new
    {
        ad = $"POS-{Guid.NewGuid():N}"[..12],
        bankaAdi = "Garanti",
        subeId = k.SubeId,
        kasaId = k.KasaId,
        pesinKomisyonOran = 0.0185m,
        valorGun = 1,
    };

    [Fact]
    public async Task PosUclari_YetkisizRolde_403()
    {
        // TEZGAHTAR cihaz OLUŞTURAMAZ (yalnız SAHIP/MUDUR).
        var k = TestKurulum.KiraciKur(fikstur.Vt.OlusturSistem(), "HTTP POS tezgahtar");
        var c = fikstur.Ortam.Istemci(k, "TEZGAHTAR");

        var y = await c.PostJson("/api/v1/pos/cihazlar", CihazGovde(k));
        Assert.Equal(HttpStatusCode.Forbidden, y.StatusCode);
    }

    [Fact]
    public async Task CihazOlustur_MUHASEBE_403()
    {
        // Mehmet koşulu: MUHASEBE EŞLEŞTİRİR ama cihaz OLUŞTURAMAZ.
        var k = TestKurulum.KiraciKur(fikstur.Vt.OlusturSistem(), "HTTP POS muhasebe");
        var c = fikstur.Ortam.Istemci(k, "MUHASEBE");

        var y = await c.PostJson("/api/v1/pos/cihazlar", CihazGovde(k));
        Assert.Equal(HttpStatusCode.Forbidden, y.StatusCode);
    }

    [Fact]
    public async Task PosUclari_PosModuluKapaliKiracida_403()
    {
        // ⚠ P-09 (Y-39): POS artık "kasa"yı ödünç almaz, KENDİ anahtarını
        //   ("pos") taşır. Ayrıca her modül KENDİ tohum markeriyle bir kez
        //   tohumlanır; marker yazılmazsa ilk erişimde varsayılan izin açılır
        //   ve test yanlışlıkla 200 görür. Bu yüzden "pos" markeri de yazılır:
        //   ölçülen şey "izin satırı YOK → uç KAPALI"dır.
        var k = TestKurulum.KiraciKur(fikstur.Vt.OlusturSistem(), "HTTP POS modül kapalı");
        // RolYetki bir İZİN listesidir: satır VARSA erişim var. Tohumlama
        // işareti yazılıp "kasa" satırı VERİLMEZSE modül o rolde KAPALIdır.
        using (var db = fikstur.Vt.OlusturSistem())
        {
            db.RolYetkileri.Add(new Kuyumcu.Api.Data.RolYetki
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId,
                Rol = "__SISTEM__", Modul = "__yapilandirildi__",
            });
            db.RolYetkileri.Add(new Kuyumcu.Api.Data.RolYetki
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, Rol = "MUDUR", Modul = "islem",
            });
            db.RolYetkileri.Add(new Kuyumcu.Api.Data.RolYetki
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId,
                Rol = "__SISTEM__", Modul = "__tohum:pos__",
            });
            db.SaveChanges();
        }

        var c = fikstur.Ortam.Istemci(k, "MUDUR");
        var y = await c.GetAsync("/api/v1/pos/cihazlar");
        Assert.Equal(HttpStatusCode.Forbidden, y.StatusCode);
    }

    [Fact]
    public async Task CihazOlustur_SAHIP_200_VeCamelCaseDoner()
    {
        var k = TestKurulum.KiraciKur(fikstur.Vt.OlusturSistem(), "HTTP POS sahip");
        var c = fikstur.Ortam.Istemci(k);

        var y = await c.PostJson("/api/v1/pos/cihazlar", CihazGovde(k));
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);

        var json = await y.Json();
        // JSON sözleşmesi camelCase — SPA fallback tuzağına karşı id OKUNUR.
        var id = json.GetProperty("cihaz").GetProperty("id").GetString();
        Assert.False(string.IsNullOrEmpty(id));
        Assert.Equal(0.0185m, json.GetProperty("cihaz").GetProperty("pesinKomisyonOran").GetDecimal());
    }

    [Fact]
    public async Task TahsilatIptal_KisaGerekce_400()
    {
        var k = TestKurulum.KiraciKur(fikstur.Vt.OlusturSistem(), "HTTP POS iptal");
        var cihaz = PosYardim.CihazYaz(fikstur.Vt, k, $"POS-{Guid.NewGuid():N}"[..12]);
        var tahsilat = PosYardim.TahsilatYaz(fikstur.Vt, k, cihaz, 1_000m);

        var c = fikstur.Ortam.Istemci(k);
        var y = await c.PostJson($"/api/v1/pos/tahsilatlar/{tahsilat}/iptal", new { gerekce = "kısa" });
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
    }

    [Fact]
    public async Task ValorTakvimi_TumRollerdeOkunur()
    {
        var k = TestKurulum.KiraciKur(fikstur.Vt.OlusturSistem(), "HTTP POS takvim");
        var c = fikstur.Ortam.Istemci(k, "TEZGAHTAR");

        var y = await c.GetAsync("/api/v1/pos/valor-takvimi");
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PAKET UX-1-D — PATCH SEMANTİĞİ: "gönderilmeyen alan = değişmedi"
    //
    // Kurul engel 4 (QA tur 3 KRİTİK 5): düzenleme formu listeden gelen ÖZETLE
    // tam gövde gönderince ürün bağı / oran / valör sessizce NULL'lanıyordu.
    // Bu test tek alan gönderir ve KALANLARIN VERİTABANINDA aynı kaldığını
    // doğrular — mutasyon hedefi M-UX1D-01.
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PosCihaz_PATCH_yalniz_bir_alan_gonderince_DIGERLERI_KORUNUR()
    {
        var k = TestKurulum.KiraciKur(fikstur.Vt.OlusturSistem(), "HTTP POS patch koru");
        var cihazId = PosYardim.CihazYaz(
            fikstur.Vt, k, $"POS-{Guid.NewGuid():N}"[..12], oran: 0.0240m, valorGun: 3);

        var c = fikstur.Ortam.Istemci(k);
        // YALNIZ ad gönderilir. Oran, valör, kasa ve şube gövdede YOK.
        var y = await c.PatchJson($"/api/v1/pos/cihazlar/{cihazId}", new { ad = "Yeni POS adı" });
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);

        using var db = fikstur.Vt.OlusturSistem();
        var cihaz = await db.PosCihazlari.FirstAsync(x => x.Id == cihazId);
        Assert.Equal("Yeni POS adı", cihaz.Ad);
        Assert.Equal(0.0240m, cihaz.PesinKomisyonOran);   // KORUNDU
        Assert.Equal(3, cihaz.ValorGun);                  // KORUNDU
        Assert.Equal(k.KasaId, cihaz.KasaId);             // KORUNDU
        Assert.Equal(k.SubeId, cihaz.SubeId);             // KORUNDU
        Assert.True(cihaz.Aktif);                         // KORUNDU
    }

    [Fact]
    public async Task PosCihaz_pasife_alinir_ve_AYNI_uctan_geri_acilir()
    {
        // UX-S25: yıkıcı eylemin GERİ DÖNÜŞ YOLU vardır ve hard DELETE yoktur.
        var k = TestKurulum.KiraciKur(fikstur.Vt.OlusturSistem(), "HTTP POS pasif");
        var cihazId = PosYardim.CihazYaz(fikstur.Vt, k, $"POS-{Guid.NewGuid():N}"[..12]);
        var c = fikstur.Ortam.Istemci(k);

        Assert.Equal(HttpStatusCode.OK,
            (await c.PatchJson($"/api/v1/pos/cihazlar/{cihazId}", new { aktif = false })).StatusCode);
        using (var db = fikstur.Vt.OlusturSistem())
            Assert.False((await db.PosCihazlari.FirstAsync(x => x.Id == cihazId)).Aktif);

        Assert.Equal(HttpStatusCode.OK,
            (await c.PatchJson($"/api/v1/pos/cihazlar/{cihazId}", new { aktif = true })).StatusCode);
        using (var db = fikstur.Vt.OlusturSistem())
        {
            var cihaz = await db.PosCihazlari.FirstAsync(x => x.Id == cihazId);
            Assert.True(cihaz.Aktif);
            Assert.NotNull(cihaz);   // satır SİLİNMEDİ — pasife alma = yumuşak silme
        }
    }

    [Fact]
    public async Task Yabanci_kiracinin_POS_cihazina_PATCH_404_doner()
    {
        // Kiracı izolasyonu: düzeltme yolu açmak sızıntı açmaz.
        var a = TestKurulum.KiraciKur(fikstur.Vt.OlusturSistem(), "HTTP POS izolasyon A");
        var b = TestKurulum.KiraciKur(fikstur.Vt.OlusturSistem(), "HTTP POS izolasyon B");
        var cihazId = PosYardim.CihazYaz(fikstur.Vt, a, $"POS-{Guid.NewGuid():N}"[..12]);

        var c = fikstur.Ortam.Istemci(b);
        var y = await c.PatchJson($"/api/v1/pos/cihazlar/{cihazId}", new { ad = "Sızıntı" });
        Assert.Equal(HttpStatusCode.NotFound, y.StatusCode);
    }
}
