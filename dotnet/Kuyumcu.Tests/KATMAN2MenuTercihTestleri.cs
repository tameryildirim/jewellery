using System.Net;
using System.Net.Http.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Tercih;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KATMAN-2 — "Grubun açık/kapalı durumu kaydedilemedi, geri alındı"
// kalıcı kırmızı bandı (25.09 tam tur + göz incelemesi, 5 ekranda).
//
// ÖLÇÜLEN KÖK NEDEN (30.09, canlı API 5166, kuyumcu_net, kendi test kiracısı):
//   • 4 rol × sıralı aç-kapa → hepsi 200 (yetki sorunu YOK)
//   • aynı kullanıcı, aynı anahtar, satır YOKKEN 2 eşanlı PATCH → 10 turun
//     10'unda biri 409 ("Bu kayıt zaten var", MySQL 1062) ya da 500 (MySQL 1213
//     kilitlenme) — iki istek de "satır yok" görüp INSERT ediyor.
//   • satır VARKEN 10 eşanlı PATCH → 10×200; satır YOKKEN → 5×409.
//   İstemci her tıklamada AYRI ve EŞANLI PATCH açtığı için (tek yazıcı yoktu)
//   iki sekme / hızlı çift tıklama ilk katlamada bu yarışa giriyordu.
//
// DÜZELTME: (1) sunucu yarışı kaybeden yazmayı baştan okuyup yeniden dener
// (TercihServisi.Yama, sınırlı döngü, sonunda hata GİZLENMEZ); (2) istemci
// katlamada TEK YAZICI + birleştirme (+ durulma beklemesi) kullanır; (3) bant
// yalnız kalıcı hatada çıkar ve kapatılabilir.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class KATMAN2TercihHttpTestleri(HttpFikstur f)
{
    private const string Yol = "/api/v1/tercihler/benim";

    private (Kiraci K, Dictionary<string, string> RolKullanici) RolluKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, ad);
        var roller = new Dictionary<string, string> { ["SAHIP"] = k.KullaniciId };
        var now = DateTime.UtcNow;
        foreach (var rol in new[] { "MUDUR", "TEZGAHTAR", "MUHASEBE" })
        {
            var id = Kimlik.Ulid();
            db.Kullanicilar.Add(new Kullanici
            {
                Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Ad = rol, Soyad = "Test",
                Email = $"{id}@test.local", SifreHash = "x", Rol = rol, Aktif = true,
                Olusturuldu = now, Guncellendi = now,
            });
            roller[rol] = id;
        }
        db.SaveChanges();
        return (k, roller);
    }

    private static object Govde(string anahtar, string? deger) =>
        new { tercihler = new Dictionary<string, string?> { [anahtar] = deger } };

    [Theory]
    [InlineData("SAHIP")]
    [InlineData("MUDUR")]
    [InlineData("TEZGAHTAR")]
    [InlineData("MUHASEBE")]
    public async Task Her_rol_KENDI_katlamasini_ac_kapa_yazar_200(string rol)
    {
        var (k, roller) = RolluKiraci("KATMAN2-ROL-" + rol);
        var istemci = f.Ortam.Istemci(roller[rol], k.TenantId, k.SubeId, rol);

        var ac = await istemci.PatchAsJsonAsync(Yol, Govde(TercihAnahtarlari.MenuAcikGruplar, """["rapor"]"""));
        var kapa = await istemci.PatchAsJsonAsync(Yol, Govde(TercihAnahtarlari.MenuAcikGruplar, "[]"));

        Assert.Equal(HttpStatusCode.OK, ac.StatusCode);
        Assert.Equal(HttpStatusCode.OK, kapa.StatusCode);
        using var db = f.Vt.Olustur(k.TenantId);
        var satir = await db.KullaniciTercihleri.AsNoTracking().SingleAsync(t =>
            t.KullaniciId == roller[rol] && t.Anahtar == TercihAnahtarlari.MenuAcikGruplar);
        Assert.Equal("[]", satir.Deger);   // kimlik JETONDAN: satır o rolün kullanıcısına yazıldı
    }

    [Fact]
    public async Task Satir_YOKKEN_ayni_anahtara_ESANLI_on_PATCH_hepsi_200_ve_TEK_satir()
    {
        // Ölçülen kök neden: bu senaryoda düzeltmeden önce 10 isteğin ~5'i 409/500.
        // Üç tur koşulur; yarışın hiç oluşmaması olasılığı ihmal edilebilir.
        var (k, roller) = RolluKiraci("KATMAN2-ESANLI-AYNI");
        var istemci = f.Ortam.Istemci(roller["MUDUR"], k.TenantId, k.SubeId, "MUDUR");

        for (var tur = 0; tur < 3; tur++)
        {
            using (var db = f.Vt.Olustur(k.TenantId))
            {
                db.KullaniciTercihleri.RemoveRange(
                    db.KullaniciTercihleri.Where(t => t.KullaniciId == roller["MUDUR"]));
                await db.SaveChangesAsync();
            }

            var istekler = Enumerable.Range(0, 10).Select(i => istemci.PatchAsJsonAsync(Yol,
                Govde(TercihAnahtarlari.MenuAcikGruplar, $"[\"g{i}\"]"))).ToArray();
            var yanitlar = await Task.WhenAll(istekler);

            foreach (var y in yanitlar)
                Assert.True(y.StatusCode == HttpStatusCode.OK,
                    $"tur {tur}: {(int)y.StatusCode} {await y.Content.ReadAsStringAsync()}");
            using var db2 = f.Vt.Olustur(k.TenantId);
            var satirlar = await db2.KullaniciTercihleri.AsNoTracking()
                .Where(t => t.KullaniciId == roller["MUDUR"] && t.Anahtar == TercihAnahtarlari.MenuAcikGruplar)
                .ToListAsync();
            Assert.Single(satirlar);
            Assert.Matches("""^\["g[0-9]"\]$""", satirlar[0].Deger);
        }
    }

    [Fact]
    public async Task Esanli_iki_PATCH_FARKLI_anahtar_ikisi_de_KALICI()
    {
        var (k, roller) = RolluKiraci("KATMAN2-ESANLI-FARKLI");
        var istemci = f.Ortam.Istemci(roller["TEZGAHTAR"], k.TenantId, k.SubeId, "TEZGAHTAR");

        var yanitlar = await Task.WhenAll(
            istemci.PatchAsJsonAsync(Yol, Govde(TercihAnahtarlari.MenuAcikGruplar, """["rapor"]""")),
            istemci.PatchAsJsonAsync(Yol, Govde(TercihAnahtarlari.MenuFavoriler, """["/kasalar"]""")));

        Assert.All(yanitlar, y => Assert.Equal(HttpStatusCode.OK, y.StatusCode));
        using var db = f.Vt.Olustur(k.TenantId);
        var t = await db.KullaniciTercihleri.AsNoTracking()
            .Where(x => x.KullaniciId == roller["TEZGAHTAR"]).ToDictionaryAsync(x => x.Anahtar, x => x.Deger);
        Assert.Equal("""["rapor"]""", t[TercihAnahtarlari.MenuAcikGruplar]);
        Assert.Equal("""["/kasalar"]""", t[TercihAnahtarlari.MenuFavoriler]);
    }

    [Fact]
    public async Task Yeniden_deneme_DOGRULAMA_hatasini_yutmaz_bilinmeyen_anahtar_yine_400()
    {
        // Döngü yalnız eşanlı yazma çakışmasını (DbUpdateException) yeniden dener;
        // beyaz liste ihlali 400 kalır (sessiz kabul yok).
        var (k, roller) = RolluKiraci("KATMAN2-400");
        var y = await f.Ortam.Istemci(roller["MUHASEBE"], k.TenantId, k.SubeId, "MUHASEBE")
            .PatchAsJsonAsync(Yol, Govde("menu-bilinmeyen", "[]"));
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
    }
}

// TEMA TEMİZLİĞİ 17.10: KATMAN2MenuTercihEkranTestleri sınıfı kaldırıldı — dokuz testin hepsi kenar
// çubuğunun grup katlama düğmesini (#kenar-grup-dugme-*), #kenar-sira-hatasi bandını ve
// #kenar-katlama-sifirla düğmesini sürüyordu; kenar çubuğu (Layout/KenarCubugu.razor) ve katlanır
// menü Yıldız kabuğunda BİLEREK yok (tuval Bugun4/Tum4), katlama yazımının kullanıcı yolu kalmadı.
// Sunucu tarafı güvenceleri (eşanlı PATCH yarışı, rol başına 200, 400 yutulmaz) yukarıdaki
// KATMAN2TercihHttpTestleri'nde AYNEN durur.
// TEMA TEMİZLİĞİ 17.10: On_hizli_tiklama_ESANLI_PATCH_acmaz_SON_durum_yazilir_bant_YOK kaldırıldı — grup katlama düğmesi yok.
// TEMA TEMİZLİĞİ 17.10: Yazma_surerken_gelen_tiklamalar_DURULMA_beklemesinden_sonra_yazilir kaldırıldı — grup katlama düğmesi yok.
// TEMA TEMİZLİĞİ 17.10: Tek_tiklama_TEK_PATCH_bekleme_yok kaldırıldı — grup katlama düğmesi yok.
// TEMA TEMİZLİĞİ 17.10: Kalici_hatada_SUNUCUNUN_bildigi_duruma_doner_bant_TEK_ve_KAPATILABILIR kaldırıldı — katlama ve #kenar-sira-hatasi bandı yok.
// TEMA TEMİZLİĞİ 17.10: Gecici_429_SESSIZCE_yeniden_denenir_bant_YOK_durum_KORUNUR kaldırıldı — katlama yazımı yok.
// TEMA TEMİZLİĞİ 17.10: Suren_429_denemeler_BITINCE_bant_cizilir_sessiz_kalinmaz kaldırıldı — katlama yazımı ve bandı yok.
// TEMA TEMİZLİĞİ 17.10: Ilk_yazma_BITTIKTEN_sonra_ikinci_tiklama_da_yazilir kaldırıldı — grup katlama düğmesi yok.
// TEMA TEMİZLİĞİ 17.10: Istek_firtinasi_SENKRON_hatasindan_sonra_pencere_acilinca_katlama_YENIDEN_yazilir kaldırıldı — katlama yazımı yok.
// TEMA TEMİZLİĞİ 17.10: Grup_sifirla_ucustaki_PATCH_bitmeden_DELETE_atmaz kaldırıldı — #kenar-katlama-sifirla yok.
