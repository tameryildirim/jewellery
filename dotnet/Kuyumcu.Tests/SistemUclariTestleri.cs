using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Sistem;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// SİSTEM YÖNETİMİ UÇLARI — paket F-2-min Faz A [HTTP + saf]
//
// Kimlik YAPILANDIRMA SIRRIDIR (MÜHÜR 4 / K1): 'SISTEM' rolü açılmadı, JWT bu
// uçları AÇMAZ. Fail-closed: anahtar yoksa / yer tutucuysa uçlar KAPALIDIR.
//
// ⚠ "Yer tutucu anahtarla uç kapalı" davranışı SAF testtir: anahtar süreç
//   genelindeki ortam değişkeninden gelir (HttpOrtami), tek fikstürde iki
//   farklı değer kurulamaz. Kapının kendisi doğrudan örneklenerek sınanır —
//   üretimdeki sınıfın AYNISI, sarmalayıcı değil.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class SistemUclariTestleri(HttpFikstur f)
{
    private const string Gerekce = "Pilot musterinin denemesi uzatildi - satis ekibi talebi";
    private const string DavetGerekce = "Fuarda tanisilan pilot kuyumcu icin deneme daveti";

    // ── Kapı: anahtar ────────────────────────────────────────────────────────

    [Fact]
    public async Task Anahtarsiz_Istek_403()
    {
        var cevap = await f.Ortam.SistemIstemci(anahtar: null).GetAsync("/api/v1/sistem/kiracilar");
        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    [Fact]
    public async Task Yanlis_Anahtar_403()
    {
        var cevap = await f.Ortam.SistemIstemci("bu-anahtar-yanlis-ama-yeterince-uzun-0123")
            .GetAsync("/api/v1/sistem/kiracilar");
        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    [Fact]
    public async Task SAHIP_Jwt_Ile_Sistem_Ucuna_Erisilemez()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETF2-TEST jwt");

        // Geçerli SAHIP token'ı — ama sistem anahtarı YOK.
        var cevap = await f.Ortam.Istemci(k).GetAsync("/api/v1/sistem/kiracilar");
        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    /// <summary>
    /// FAIL-CLOSED: appsettings.json'daki DEGISTIR- yer tutucusu ile uçlar
    /// KAPALIDIR (503). "Anahtar yok = serbest" davranışı yasaktır.
    /// </summary>
    [Fact]
    public async Task Varsayilan_DEGISTIR_Anahtari_Ucu_Kapatir()
    {
        var yerTutucu = SistemAyarlari.YerTutucuOneki + "-en-az-32-karakter-sistem-anahtari";
        Assert.True(yerTutucu.Length >= SistemAyarlari.AsgariAnahtarUzunlugu);

        foreach (var anahtar in new[] { yerTutucu, "", "kisa-anahtar" })
        {
            var ayarlar = new SistemAyarlari { Anahtari = anahtar };
            Assert.False(ayarlar.AnahtarKullanilabilir);

            var baglam = Baglam("GET", anahtar);
            await new SistemAnahtariKapisi(ayarlar).OnAuthorizationAsync(baglam);

            var sonuc = Assert.IsType<ObjectResult>(baglam.Result);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, sonuc.StatusCode);
        }

        // Gerçek anahtar → kapı AÇIK (aynı kapı, aynı yol).
        var gercek = new SistemAyarlari { Anahtari = HttpOrtami.TestSistemAnahtari };
        Assert.True(gercek.AnahtarKullanilabilir);
        var acik = Baglam("GET", HttpOrtami.TestSistemAnahtari);
        await new SistemAnahtariKapisi(gercek).OnAuthorizationAsync(acik);
        Assert.Null(acik.Result);
    }

    [Fact]
    public async Task Operator_Basligi_Olmadan_Mutasyon_400()
    {
        var cevap = await f.Ortam
            .SistemIstemci(HttpOrtami.TestSistemAnahtari, operatorAdi: null)
            .PostJson("/api/v1/sistem/davetler", new { denemeGun = 14, gerekce = DavetGerekce });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Contains("X-Sistem-Operator", await cevap.Metin(), StringComparison.Ordinal);
    }

    // ── Davetler ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Davet_Uretilir_Ham_Jeton_Bir_Kez_Doner_Ve_Listede_GORUNMEZ()
    {
        var istemci = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
        var uret = await istemci.PostJson("/api/v1/sistem/davetler", new
        {
            email = "pilot@ornek.test", isletmeAdi = "PAKETF2-TEST Pilot",
            denemeGun = 21, not = "pilot-1", gerekce = DavetGerekce,
        });

        Assert.Equal(HttpStatusCode.Created, uret.StatusCode);
        var json = await uret.Json();
        var jeton = json.GetProperty("jeton").GetString()!;
        var id = json.GetProperty("id").GetString()!;
        Assert.NotEmpty(jeton);
        // PII maskelenir (Mehmet koşulu).
        Assert.Equal("p***@o***.test", json.GetProperty("email").GetString());

        var liste = await istemci.GetAsync("/api/v1/sistem/davetler");
        Assert.Equal(HttpStatusCode.OK, liste.StatusCode);
        var govde = await liste.Metin();
        Assert.DoesNotContain(jeton, govde, StringComparison.Ordinal);
        Assert.DoesNotContain("jetonHash", govde, StringComparison.Ordinal);
        Assert.Contains(id, govde, StringComparison.Ordinal);
        Assert.DoesNotContain("pilot@ornek.test", govde, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PATCH_Govdede_Olmayan_Alan_Korunur()
    {
        var istemci = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
        var uret = await istemci.PostJson("/api/v1/sistem/davetler", new
        {
            denemeGun = 30, not = "korunacak-not", gerekce = DavetGerekce,
        });
        var id = (await uret.Json()).GetProperty("id").GetString()!;

        // YALNIZ iptal gönderilir → not ve denemeGun KORUNUR (UX-S3).
        var patch = await istemci.PatchAsJsonAsync($"/api/v1/sistem/davetler/{id}",
            new { iptal = true, gerekce = DavetGerekce });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        var json = await patch.Json();
        Assert.Equal("IPTAL", json.GetProperty("durum").GetString());
        Assert.Equal("korunacak-not", json.GetProperty("not").GetString());
        Assert.Equal(30, json.GetProperty("denemeGun").GetInt32());

        // İptal GERİ ALINABİLİR (DoD EK md.4 — geri dönüş yolu).
        var geri = await istemci.PatchAsJsonAsync($"/api/v1/sistem/davetler/{id}",
            new { iptal = false, gerekce = DavetGerekce });
        Assert.Equal("BEKLIYOR", (await geri.Json()).GetProperty("durum").GetString());
        Assert.Equal("korunacak-not", (await geri.Json()).GetProperty("not").GetString());
    }

    // ── Kiracılar ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Dogru_Anahtar_Kiraci_Listesi_Doner()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(db, "PAKETF2-TEST liste");
            TestKurulum.KiraciDurumuYaz(db, k.TenantId, KiraciDurumlari.Deneme, DateTime.UtcNow.AddDays(5));
        }

        var cevap = await f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari)
            .GetAsync("/api/v1/sistem/kiracilar");

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var kayitlar = (await cevap.Json()).GetProperty("kayitlar").EnumerateArray()
            .Single(x => x.GetProperty("id").GetString() == k.TenantId);
        Assert.Equal("DENEME", kayitlar.GetProperty("durum").GetString());
        Assert.InRange(kayitlar.GetProperty("kalanGun").GetInt32(), 4, 6);
        Assert.Equal(1, kayitlar.GetProperty("kullaniciSayisi").GetInt32());
        Assert.True(kayitlar.GetProperty("yazabilirMi").GetBoolean());
    }

    [Fact]
    public async Task Gerekcesiz_SureUzatma_400()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETF2-TEST gerekce");
        var istemci = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);

        var bos = await istemci.PatchAsJsonAsync($"/api/v1/sistem/kiracilar/{k.TenantId}",
            new { durum = "SALT_OKUNUR" });
        Assert.Equal(HttpStatusCode.BadRequest, bos.StatusCode);

        var kisa = await istemci.PatchAsJsonAsync($"/api/v1/sistem/kiracilar/{k.TenantId}",
            new { durum = "SALT_OKUNUR", gerekce = "kisa" });
        Assert.Equal(HttpStatusCode.BadRequest, kisa.StatusCode);
    }

    /// <summary>
    /// Süre uzatma DENEME süresi dolmuş kiracıda yazmayı yeniden açar — ve
    /// bunu ÖNBELLEK GECİKMESİ OLMADAN yapar (önbellek açıkça boşaltılır).
    /// </summary>
    [Fact]
    public async Task SureUzatma_PATCH_DenemeBitisi_Ileri_Alir_Ve_Yazma_Yeniden_Acilir()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(db, "PAKETF2-TEST uzatma");
            TestKurulum.KiraciDurumuYaz(db, k.TenantId, KiraciDurumlari.Deneme, DateTime.UtcNow.AddDays(-1));
        }
        var kiraciIstemci = f.Ortam.Istemci(k);

        // Süre dolmuş → yazma kapalı (ve durum ÖNBELLEĞE girer).
        var once = await kiraciIstemci.PatchAsJsonAsync($"/api/v1/urunler/{k.UrunId}", new { ad = "A" });
        Assert.Equal(HttpStatusCode.Forbidden, once.StatusCode);

        var yeniBitis = DateTime.UtcNow.AddDays(30);
        var uzat = await f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari)
            .PatchAsJsonAsync($"/api/v1/sistem/kiracilar/{k.TenantId}",
                new { denemeBitis = yeniBitis, gerekce = Gerekce });
        Assert.Equal(HttpStatusCode.OK, uzat.StatusCode);
        Assert.True((await uzat.Json()).GetProperty("yazabilirMi").GetBoolean());

        // ÖNBELLEK BOŞALTILDI → aynı anda yazma yeniden açık.
        var sonra = await kiraciIstemci.PatchAsJsonAsync($"/api/v1/urunler/{k.UrunId}", new { ad = "B" });
        Assert.Equal(HttpStatusCode.OK, sonra.StatusCode);
    }

    /// <summary>
    /// Mehmet koşulu — KISITLAMA yönü: AKTIF kiracı önbellekteyken SALT_OKUNUR'a
    /// çekilirse yazma 30 sn beklemeden kapanır.
    /// </summary>
    [Fact]
    public async Task AKTIF_ten_SALT_OKUNUR_a_gecis_ANINDA_uygulanir()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETF2-TEST kisitlama");
        var kiraciIstemci = f.Ortam.Istemci(k);

        var once = await kiraciIstemci.PatchAsJsonAsync($"/api/v1/urunler/{k.UrunId}", new { ad = "A" });
        Assert.Equal(HttpStatusCode.OK, once.StatusCode);      // AKTIF önbelleğe girdi

        var kapat = await f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari)
            .PatchAsJsonAsync($"/api/v1/sistem/kiracilar/{k.TenantId}",
                new { durum = "SALT_OKUNUR", gerekce = Gerekce });
        Assert.Equal(HttpStatusCode.OK, kapat.StatusCode);

        var sonra = await kiraciIstemci.PatchAsJsonAsync($"/api/v1/urunler/{k.UrunId}", new { ad = "B" });
        Assert.Equal(HttpStatusCode.Forbidden, sonra.StatusCode);
    }

    [Fact]
    public async Task SureUzatma_Denetim_Kaydi_Yazar()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETF2-TEST denetim");

        var uzat = await f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari, "mehmet-guvenlik")
            .PatchAsJsonAsync($"/api/v1/sistem/kiracilar/{k.TenantId}",
                new { denemeBitis = DateTime.UtcNow.AddDays(10), gerekce = Gerekce });
        Assert.Equal(HttpStatusCode.OK, uzat.StatusCode);

        await using var db2 = f.Vt.OlusturSistem();
        // ⚠ AYNI transaction'da İKİ iz doğar: (1) DenetimYazmaDinleyicisi'nin
        // otomatik GUNCELLEME izi (hangi varlık değişti), (2) buradaki
        // GEREKÇELİ iz (kim · neden). Aranan ikincisidir.
        var kayitlar = await db2.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikTuru == SistemServisi.DenetimVarligi)
            .ToListAsync();
        var kayit = Assert.Single(kayitlar,
            d => d.Detay is not null && d.Detay.StartsWith(SistemServisi.DenetimIslemi, StringComparison.Ordinal));
        Assert.Contains("mehmet-guvenlik", kayit.Detay!, StringComparison.Ordinal);
        Assert.Contains(Gerekce, kayit.Detay!, StringComparison.Ordinal);
        Assert.Contains("deneme_bitis", kayit.Detay!, StringComparison.Ordinal);
        // Sistem işleminde kullanıcı YOKTUR — uydurulmaz.
        Assert.Null(kayit.KullaniciId);
        // ⚠ Sistem anahtarı DEĞERİ hiçbir koşulda ize yazılmaz.
        Assert.DoesNotContain(HttpOrtami.TestSistemAnahtari, kayit.Detay!, StringComparison.Ordinal);
    }

    /// <summary>
    /// ⚠ TEST AÇIĞI KAPATMASI (mutasyon M-F2-14): gerekçe kuralı yalnız
    /// DataAnnotations ile sınanıyordu; öznitelikler YALNIZ controller yolunda
    /// koşar. Servis doğrudan çağrıldığında (arka plan işi, gelecekteki bir
    /// modül) kural sessizce kaybolurdu. Bu test servisi DOĞRUDAN çağırır.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("kisa gerekce")]
    public async Task Servis_DOGRUDAN_cagrildiginda_da_gerekce_zorunludur(string gerekce)
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETF2-TEST servis");

        await using var kapsam = f.Ortam.Services
            .GetRequiredService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>()
            .CreateAsyncScope();
        var servis = kapsam.ServiceProvider.GetRequiredService<SistemServisi>();

        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() =>
            servis.KiraciGuncelle(k.TenantId,
                new KiraciGuncelleDto { Durum = KiraciDurumlari.SaltOkunur, Gerekce = gerekce },
                "asikin-qa"));
    }

    /// <summary>Operatör başlığı servis katmanında da zorunludur (K3).</summary>
    [Fact]
    public async Task Servis_DOGRUDAN_cagrildiginda_da_operator_zorunludur()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETF2-TEST operator");

        await using var kapsam = f.Ortam.Services
            .GetRequiredService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>()
            .CreateAsyncScope();
        var servis = kapsam.ServiceProvider.GetRequiredService<SistemServisi>();

        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() =>
            servis.KiraciGuncelle(k.TenantId,
                new KiraciGuncelleDto { Durum = KiraciDurumlari.SaltOkunur, Gerekce = Gerekce },
                "x"));
    }


    // ── Davet denetim izi (MÜHÜR 4 / K3) ─────────────────────────────────────

    /// <summary>
    /// ⚠ ÖNCEKİ TURUN KRİTİK BULGUSU: davet üretimi — yeni kiracı açma yetkisi
    /// DOĞURAN ayrıcalıklı eylem — hiçbir kalıcı iz bırakmıyordu. Otomatik yol
    /// kapalıdır (KiraciDaveti IKiraciVarlik değil → DenetimYazmaDinleyicisi
    /// onu atlar), bu yüzden iz AÇIKÇA yazılır ve bu test onu zorlar.
    /// </summary>
    [Fact]
    public async Task Davet_Uretimi_Denetim_Izi_Birakir()
    {
        var uret = await f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari, "tamer-patron")
            .PostJson("/api/v1/sistem/davetler", new
            {
                email = "izli@ornek.test", isletmeAdi = "PAKETF2-TEST iz",
                denemeGun = 14, gerekce = DavetGerekce,
            });
        Assert.Equal(HttpStatusCode.Created, uret.StatusCode);
        var json = await uret.Json();
        var davetId = json.GetProperty("id").GetString()!;
        var jeton = json.GetProperty("jeton").GetString()!;

        await using var db = f.Vt.OlusturSistem();
        var iz = Assert.Single(await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == SistemServisi.SistemTenantId
                        && d.VarlikTuru == SistemServisi.DavetDenetimVarligi
                        && d.VarlikId == davetId)
            .ToListAsync());

        Assert.StartsWith(SistemServisi.DenetimIslemiDavetUret, iz.Detay!, StringComparison.Ordinal);
        Assert.Contains("tamer-patron", iz.Detay!, StringComparison.Ordinal);
        Assert.Contains(DavetGerekce, iz.Detay!, StringComparison.Ordinal);
        Assert.Null(iz.KullaniciId);                       // sistem işleminde kullanıcı YOK
        Assert.True(iz.Hassas);
        // ⚠ Ham jeton ve sistem anahtarı ize ASLA yazılmaz.
        Assert.DoesNotContain(jeton, iz.Detay!, StringComparison.Ordinal);
        Assert.DoesNotContain(HttpOrtami.TestSistemAnahtari, iz.Detay!, StringComparison.Ordinal);
    }

    /// <summary>Daveti KİM iptal etti / geri aldı — her ikisi de ayrı iz satırı üretir.</summary>
    [Fact]
    public async Task Davet_Iptali_Ve_Geri_Alinmasi_Denetim_Izi_Birakir()
    {
        var istemci = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari, "mehmet-guvenlik");
        var uret = await istemci.PostJson("/api/v1/sistem/davetler", new
        {
            isletmeAdi = "PAKETF2-TEST iptal izi", denemeGun = 7, gerekce = DavetGerekce,
        });
        var davetId = (await uret.Json()).GetProperty("id").GetString()!;

        const string iptalGerekce = "Pilot vazgecti - davet guvenlik geregi iptal edildi";
        var iptal = await istemci.PatchAsJsonAsync($"/api/v1/sistem/davetler/{davetId}",
            new { iptal = true, gerekce = iptalGerekce });
        Assert.Equal(HttpStatusCode.OK, iptal.StatusCode);

        await using var db = f.Vt.OlusturSistem();
        var izler = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == SistemServisi.SistemTenantId && d.VarlikId == davetId)
            .ToListAsync();

        Assert.Equal(2, izler.Count);                       // üretim + iptal
        var iptalIzi = Assert.Single(izler,
            d => d.Detay!.StartsWith(SistemServisi.DenetimIslemiDavetGuncelle, StringComparison.Ordinal));
        Assert.Contains("mehmet-guvenlik", iptalIzi.Detay!, StringComparison.Ordinal);
        Assert.Contains(iptalGerekce, iptalIzi.Detay!, StringComparison.Ordinal);
        Assert.Contains(DavetDurumlari.Iptal, iptalIzi.Detay!, StringComparison.Ordinal);
    }

    /// <summary>Gerekçesiz ayrıcalıklı eylem YOK (K3) — 400, ve hiçbir davet doğmaz.</summary>
    [Fact]
    public async Task Gerekcesiz_Davet_Uretimi_400_Ve_Kayit_Dogmaz()
    {
        var istemci = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
        int once;
        await using (var db = f.Vt.OlusturSistem()) once = await db.Davetler.CountAsync();

        foreach (var govde in new object[]
                 {
                     new { denemeGun = 14 },
                     new { denemeGun = 14, gerekce = "kisa" },
                 })
        {
            var cevap = await istemci.PostJson("/api/v1/sistem/davetler", govde);
            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        await using var son = f.Vt.OlusturSistem();
        Assert.Equal(once, await son.Davetler.CountAsync());
    }

    /// <summary>
    /// Servis DOĞRUDAN çağrıldığında da gerekçe/operatör zorunludur:
    /// DataAnnotations yalnız controller yolunda koşar.
    /// </summary>
    [Fact]
    public async Task Servis_DOGRUDAN_cagrildiginda_da_davet_gerekcesi_zorunludur()
    {
        await using var kapsam = f.Ortam.Services
            .GetRequiredService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>()
            .CreateAsyncScope();
        var servis = kapsam.ServiceProvider.GetRequiredService<SistemServisi>();

        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() =>
            servis.DavetUret(new DavetUretDto { Gerekce = "kisa" }, "asikin-qa"));
        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() =>
            servis.DavetUret(new DavetUretDto { Gerekce = DavetGerekce }, "x"));
    }


    /// <summary>
    /// ⚠ CANLI DOĞRULAMA BULGUSU (2026-09-17): davet üretimi <c>OranSiniri.Kayit</c>
    /// ilkesini kullanıyordu; kayıt kotası IP başına 3/saat olduğu için
    /// yöneticinin pilot daveti üretmesi, aynı IP'den gelen sıradan kayıt
    /// denemeleriyle (ve kendi 400 alan denemeleriyle) kilitleniyordu.
    /// Bu test ayrımı SABİTLER: davet ucu kayıt kotasını PAYLAŞMAZ.
    /// </summary>
    [Fact]
    public void Davet_Ucu_Kayit_Oran_Kotasini_PAYLASMAZ()
    {
        var yontem = typeof(SistemController).GetMethod(nameof(SistemController.DavetUret))!;
        var ilke = yontem.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: false)
            .Cast<EnableRateLimitingAttribute>().Single();

        Assert.Equal(OranSiniri.SistemDavet, ilke.PolicyName);
        Assert.NotEqual(OranSiniri.Kayit, ilke.PolicyName);
    }

    // ── Yardımcı ────────────────────────────────────────────────────────────

    private static AuthorizationFilterContext Baglam(string fiil, string? anahtar)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = fiil;
        if (anahtar is not null) http.Request.Headers[SistemAnahtariKapisi.Baslik] = anahtar;
        return new AuthorizationFilterContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()), []);
    }
}
