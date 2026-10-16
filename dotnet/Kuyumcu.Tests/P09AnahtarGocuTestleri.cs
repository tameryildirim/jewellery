using System.Net;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Yetki;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-09 — ANAHTAR GÖÇÜ (hasım turu KRİTİK bulgusunun kapanışı)
//
// P-09 bazı ekranların sunucu kapısını YENİDEN ADLANDIRDI:
//   pos ← kasa · vezne ← islem · sayim ← urun · fiyat-listesi ← urun
// Yeni anahtarın eski kiracıda markeri yoktur; modül-başı tohumlama KOŞULSUZ
// çalışsaydı, SAHİBİN BİLEREK KALDIRDIĞI erişim YENİ AD ALTINDA sessizce geri
// açılırdı — üstelik PARA EL DEĞİŞTİREN uçlarda (POS, vezne).
//
// ⚠ Mevcut HttpEvrakTestleri.Sahibin_kaldirdigi_yetki_yeniden_tohumlamadan_sonra_da_403
//   yalnız AYNI anahtarın yeniden tohumlanmasını kapatır. Bu dosya YENİDEN
//   ADLANDIRMA senaryosunu kapatır; ikisi birbirinin yerine geçmez.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class P09AnahtarGocuTestleri(HttpFikstur f)
{
    /// <summary>
    /// ESKİ (tek marker'lı) kiracının O GÜN taşıdığı anahtarlar.
    ///
    /// ⚠ NEDEN ELLE YAZILI, NEDEN <c>Moduller.TohumlamaOncesi</c> DEĞİL:
    ///   fikstür sınanan sabitten türetilseydi, o sabite "pos" eklendiğinde
    ///   fikstür de "pos" satırını yazar ve mutasyon ÖLÇÜLEMEZDİ. Fikstür
    ///   GEÇMİŞİ anlatır, bugünkü sabiti değil; bu yüzden donmuştur.
    /// </summary>
    private static readonly string[] EskiAnahtarlar =
        ["cari", "urun", "islem", "kasa", "banka", "fatura", "rapor", "kullanici"];

    /// <summary>
    /// "Eski kiracı" durumunu ELLE kurar: tek <c>__yapilandirildi__</c> markeri
    /// + yalnız <see cref="EskiAnahtarlar"/> satırları. <paramref name="kaldirilan"/>
    /// anahtarı <paramref name="kaldirilanRol"/>'den ÇIKARILIR (sahip bilerek aldı).
    /// </summary>
    private async Task<Kiraci> EskiKiraciKur(string ad, string kaldirilanRol, string? kaldirilan)
    {
        Kiraci kiraci;
        using (var sistem = f.Vt.OlusturSistem())
            kiraci = TestKurulum.KiraciKur(sistem, ad);

        using var db = f.Vt.Olustur(kiraci.TenantId);
        var mevcut = await db.RolYetkileri.Where(y => y.TenantId == kiraci.TenantId).ToListAsync();
        db.RolYetkileri.RemoveRange(mevcut);
        db.RolYetkileri.Add(new RolYetki
        {
            Id = Kimlik.Ulid(), TenantId = kiraci.TenantId, Rol = "__SISTEM__", Modul = "__yapilandirildi__",
        });
        foreach (var rol in Moduller.Roller)
            foreach (var m in Moduller.Varsayilan[rol].Where(EskiAnahtarlar.Contains))
            {
                if (rol == kaldirilanRol && m == kaldirilan) continue;
                db.RolYetkileri.Add(new RolYetki
                {
                    Id = Kimlik.Ulid(), TenantId = kiraci.TenantId, Rol = rol, Modul = m,
                });
            }
        await db.SaveChangesAsync();
        return kiraci;
    }

    // ── Harita sözleşmesi (saf) ──────────────────────────────────────────────

    [Fact]
    public void AnahtarGocu_haritasi_kilitli_ve_tutarlidir()
    {
        Assert.Equal(
            new Dictionary<string, string[]>
            {
                ["pos"] = ["kasa"],
                ["vezne"] = ["islem"],
                ["sayim"] = ["urun"],
                ["fiyat-listesi"] = ["urun"],
            }.OrderBy(x => x.Key).Select(x => $"{x.Key}<-{string.Join('|', x.Value)}"),
            Moduller.AnahtarGocu.OrderBy(x => x.Key).Select(x => $"{x.Key}<-{string.Join('|', x.Value)}"));

        foreach (var (yeni, eskiler) in Moduller.AnahtarGocu)
        {
            // Göçmen anahtar gerçek bir modüldür ve YENİDİR (marker taşımaz).
            Assert.Contains(yeni, Moduller.Tumu);
            Assert.DoesNotContain(yeni, Moduller.TohumlamaOncesi);
            // Devraldığı anahtar eski kiracıda GERÇEKTEN vardı.
            Assert.NotEmpty(eskiler);
            foreach (var eski in eskiler) Assert.Contains(eski, Moduller.TohumlamaOncesi);
        }
    }

    // ── Sessiz yetki genişlemesi yasağı (HTTP) ───────────────────────────────

    [Theory]
    // yeni anahtar · sahibin kaldırdığı ESKİ anahtar · o anahtarla korunan uç
    [InlineData("kasa", "/api/v1/pos/cihazlar")]
    [InlineData("islem", "/api/v1/vezne/kuyruk")]
    [InlineData("urun", "/api/v1/sayim")]
    public async Task Eski_kiracida_SAHIBIN_kaldirdigi_anahtar_yeni_ad_altinda_GERI_ACILMAZ(
        string eskiAnahtar, string yol)
    {
        // SAHİP eski anahtarı TEZGÂHTAR'dan bilerek kaldırmış bir eski kiracı.
        var k = await EskiKiraciKur($"P09G-{eskiAnahtar}", "TEZGAHTAR", eskiAnahtar);

        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");
        var yanit = await tezgahtar.GetAsync(yol);

        // Tohumlama bu istek sırasında koştu (marker yoktu) — yine de 403.
        Assert.Equal(HttpStatusCode.Forbidden, yanit.StatusCode);

        // Ve DB'de satır YAZILMAMIŞ olmalı: kanıt kodun kendi yan etkisinde.
        using var db = f.Vt.Olustur(k.TenantId);
        var gocmenler = Moduller.AnahtarGocu
            .Where(g => g.Value.Contains(eskiAnahtar)).Select(g => g.Key).ToArray();
        var yazilan = await db.RolYetkileri.AsNoTracking()
            .Where(y => y.TenantId == k.TenantId && y.Rol == "TEZGAHTAR" && gocmenler.Contains(y.Modul))
            .ToListAsync();
        Assert.Empty(yazilan);
    }

    [Fact]
    public async Task Fiyat_listesi_de_urun_kaldirilinca_acilmaz()
    {
        var k = await EskiKiraciKur("P09G-fiyat", "TEZGAHTAR", "urun");
        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await tezgahtar.GetAsync("/api/v1/fiyat/listeler")).StatusCode);
    }

    // ── Fail-closed regresyonu yasağı: eski anahtarı DURAN rol erişimini KORUR ─

    [Theory]
    [InlineData("/api/v1/pos/cihazlar")]
    [InlineData("/api/v1/vezne/kuyruk")]
    [InlineData("/api/v1/sayim")]
    [InlineData("/api/v1/fiyat/listeler")]
    public async Task Eski_anahtari_DURAN_rol_yeni_anahtari_alir_200(string yol)
    {
        // Hiçbir anahtar kaldırılmamış eski kiracı → bugünkü erişim BİREBİR korunur.
        var k = await EskiKiraciKur($"P09G-koru-{yol.GetHashCode():x8}", "TEZGAHTAR", null);
        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");
        Assert.Equal(HttpStatusCode.OK, (await tezgahtar.GetAsync(yol)).StatusCode);
    }

    [Fact]
    public async Task Yeni_kiracida_goc_kapisi_hicbir_seyi_kapatmaz()
    {
        // İlk kurulumda göç kapısı DEVRE DIŞIDIR: varsayılan matris tamdır.
        Kiraci k;
        using (var sistem = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(sistem, "P09G-YENI");

        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");
        foreach (var yol in new[] { "/api/v1/pos/cihazlar", "/api/v1/vezne/kuyruk", "/api/v1/sayim", "/api/v1/fiyat/listeler" })
            Assert.Equal(HttpStatusCode.OK, (await tezgahtar.GetAsync(yol)).StatusCode);
    }

    [Fact]
    public async Task Gercekten_YENI_kapilar_gocmen_sayilmaz_ve_kosulsuz_tohumlanir()
    {
        // emanet/kur uçlarında P-09 ÖNCESİ hiç ModulYetkisi YOKTU (uç tüm
        // kimlikli kullanıcılara açıktı). Bunları koşullu yapmak erişimi
        // GENİŞLETMEZ, var olanı KALDIRIRDI → haritaya girmezler.
        Assert.DoesNotContain("emanet", Moduller.AnahtarGocu.Keys);
        Assert.DoesNotContain("kur", Moduller.AnahtarGocu.Keys);

        // Eski kiracıda "islem" ve "urun" TEZGÂHTAR'dan alınmış olsa bile
        // emanet/kur erişimi BUGÜNKÜ hâliyle korunur.
        var k = await EskiKiraciKur("P09G-yenikapi", "TEZGAHTAR", "islem");
        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");
        Assert.Equal(HttpStatusCode.OK, (await tezgahtar.GetAsync("/api/v1/kur/guncel")).StatusCode);

        using var db = f.Vt.Olustur(k.TenantId);
        Assert.True(await db.RolYetkileri.AsNoTracking()
            .AnyAsync(y => y.TenantId == k.TenantId && y.Rol == "TEZGAHTAR" && y.Modul == "kur"));
    }
}
