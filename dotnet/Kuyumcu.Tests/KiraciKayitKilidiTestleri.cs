using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// KAYIT UCU DAVET KİLİDİ — paket F-2-min Faz A [gerçek MySQL]
//
// KAPATILAN BULGU: POST /api/v1/auth/kayit sınırsız ve bedava kiracı açıyordu.
// Artık kiracı YALNIZ tek kullanımlık, süreli bir davetle doğar.
//
// ⚠ ORAN SINIRI: kayıt ucu IP başına 3/saat. Her test kendi istemci IP'siyle
//   gelir (HttpOrtami.AnonimIstemci) ve 3'ten az kayıt isteği yapar.
//
// ⚠ EŞZAMANLILIK TESTİ HTTP'DEN GEÇMEZ: 5 paralel istek aynı IP'den gelseydi
//   oran sınırına takılır, farklı IP'lerden gelseydi sınanan şey yine yarış
//   olurdu ama araya 429 riski girerdi. Yarış VERİTABANI düzeyindedir, bu
//   yüzden AuthServisi doğrudan (üretimdeki DI ile aynı biçimde) çağrılır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class KiraciKayitKilidiTestleri(HttpFikstur f)
{
    private const string Sifre = "GucluParola123!";

    /// <summary>
    /// ⚠ İşletme adı TESTE ÖZGÜDÜR. Tam süitte başka koleksiyonlar PARALEL koşar
    /// ve tenants/kullanicilar satır sayısı sürekli değişir — "toplam sayı
    /// değişmedi" iddiası KARARSIZ olurdu (2026-09-17 tam süit koşumunda
    /// ölçüldü: 752 → 764). Yan etki yokluğu bu yüzden BU kayda bakılarak
    /// sınanır, genel sayımla değil.
    /// </summary>
    private static object Govde(string eposta, string? jeton, string isletme) => new
    {
        isletmeAdi = isletme,
        ad = "Test", soyad = "Kayit",
        email = eposta, sifre = Sifre, davetJetonu = jeton,
    };

    private static string Isletme() => $"PAKETF2-TEST {Kimlik.Ulid()}";

    private static string Eposta() => $"f2-{Kimlik.Ulid()}@test.local";

    private async Task<bool> TenantVarMi(string isletme)
    {
        await using var db = f.Vt.OlusturSistem();
        return await db.Tenants.AnyAsync(t => t.Ad == isletme);
    }

    // ── Davetsiz / geçersiz ──────────────────────────────────────────────────

    [Fact]
    public async Task Davetsiz_Kayit_403_ve_Hicbir_Tenant_Olusmaz()
    {
        var isletme = Isletme();
        var eposta = Eposta();
        Assert.False(await TenantVarMi(isletme));

        var cevap = await f.Ortam.AnonimIstemci()
            .PostJson("/api/v1/auth/kayit", Govde(eposta, null, isletme));

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        // Yan etki YOK: tenant / şube / kullanıcı hiçbiri doğmaz.
        Assert.False(await TenantVarMi(isletme));
        await using var son = f.Vt.OlusturSistem();
        Assert.False(await son.Kullanicilar.AnyAsync(k => k.Email == eposta));
    }

    [Fact]
    public async Task Gecersiz_Jeton_Kayit_Reddedilir_Kullanici_Tablosu_Degismez()
    {
        var isletme = Isletme();
        var eposta = Eposta();

        var cevap = await f.Ortam.AnonimIstemci()
            .PostJson("/api/v1/auth/kayit", Govde(eposta, "boyle-bir-jeton-yok", isletme));

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        await using var son = f.Vt.OlusturSistem();
        Assert.False(await son.Tenants.AnyAsync(t => t.Ad == isletme));
        Assert.False(await son.Kullanicilar.AnyAsync(k => k.Email == eposta));
    }

    [Fact]
    public async Task Kullanilmis_Davet_Ikinci_Kez_Kabul_Edilmez()
    {
        string jeton;
        await using (var db = f.Vt.OlusturSistem()) (jeton, _) = TestKurulum.DavetOlustur(db);

        var ilk = await f.Ortam.AnonimIstemci().PostJson("/api/v1/auth/kayit", Govde(Eposta(), jeton, Isletme()));
        Assert.Equal(HttpStatusCode.Created, ilk.StatusCode);

        // AYNI jeton, BAŞKA e-posta → reddedilir.
        var ikinci = await f.Ortam.AnonimIstemci().PostJson("/api/v1/auth/kayit", Govde(Eposta(), jeton, Isletme()));
        Assert.Equal(HttpStatusCode.Forbidden, ikinci.StatusCode);
    }

    [Fact]
    public async Task Suresi_Gecmis_Davet_Reddedilir()
    {
        string jeton;
        await using (var db = f.Vt.OlusturSistem())
            (jeton, _) = TestKurulum.DavetOlustur(db, gecerlilikSaat: -1);

        var cevap = await f.Ortam.AnonimIstemci().PostJson("/api/v1/auth/kayit", Govde(Eposta(), jeton, Isletme()));
        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    [Fact]
    public async Task Iptal_Edilmis_Davet_Reddedilir()
    {
        string jeton;
        await using (var db = f.Vt.OlusturSistem())
            (jeton, _) = TestKurulum.DavetOlustur(db, iptal: true);

        var cevap = await f.Ortam.AnonimIstemci().PostJson("/api/v1/auth/kayit", Govde(Eposta(), jeton, Isletme()));
        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    // ── Geçerli davet ────────────────────────────────────────────────────────

    [Fact]
    public async Task Gecerli_Davet_Tenant_DENEME_Durumunda_Ve_DenemeBitis_Dolu_Dogar()
    {
        string jeton; string davetId;
        await using (var db = f.Vt.OlusturSistem())
            (jeton, davetId) = TestKurulum.DavetOlustur(db, denemeGun: 7);

        var oncesi = DateTime.UtcNow;
        var cevap = await f.Ortam.AnonimIstemci().PostJson("/api/v1/auth/kayit", Govde(Eposta(), jeton, Isletme()));
        Assert.Equal(HttpStatusCode.Created, cevap.StatusCode);
        var tenantId = (await cevap.Json()).GetProperty("tenantId").GetString()!;

        await using var son = f.Vt.OlusturSistem();
        var tenant = await son.Tenants.AsNoTracking().SingleAsync(t => t.Id == tenantId);
        Assert.Equal(KiraciDurumlari.Deneme, tenant.Durum);
        Assert.NotNull(tenant.DenemeBitis);
        Assert.Equal(davetId, tenant.DavetId);
        // 7 günlük deneme: bitiş, kayıt anından 6-8 gün sonrası aralığında.
        Assert.InRange(tenant.DenemeBitis!.Value, oncesi.AddDays(6), oncesi.AddDays(8));

        // Davet AYNI işlemde tüketilmiş ve kiracıya bağlanmış olmalı.
        var davet = await son.Davetler.AsNoTracking().SingleAsync(d => d.Id == davetId);
        Assert.True(davet.KullanildiMi);
        Assert.Equal(tenantId, davet.KullananTenantId);
    }

    /// <summary>
    /// ⚠ Jeton GERÇEK ÜRETİM YOLUNDAN (POST /sistem/davetler) alınır: test
    /// yardımcısı üzerinden üretilseydi, servisin ham jetonu bir kolona
    /// yazması bu testten KAÇARDI (mutasyon M-F2-09 bunu kanıtladı).
    /// </summary>
    [Fact]
    public async Task Ham_Jeton_Veritabaninda_Saklanmaz_Yalniz_Hash_Vardir()
    {
        var uret = await f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari)
            .PostJson("/api/v1/sistem/davetler", new
            {
                email = "hamjeton@ornek.test", isletmeAdi = "PAKETF2-TEST ham",
                denemeGun = 14, not = "ham-jeton-testi",
                gerekce = "Ham jeton saklanmiyor testi icin uretilen davet",
            });
        Assert.Equal(HttpStatusCode.Created, uret.StatusCode);
        var json = await uret.Json();
        var jeton = json.GetProperty("jeton").GetString()!;
        var davetId = json.GetProperty("id").GetString()!;

        await using var db = f.Vt.OlusturSistem();
        var davet = await db.Davetler.AsNoTracking().SingleAsync(d => d.Id == davetId);

        // Hash dolu ve ham jetonun kendisi DEĞİL.
        Assert.Equal(64, davet.JetonHash.Length);                 // SHA-256 hex
        Assert.NotEqual(jeton, davet.JetonHash);

        // Ham jeton HİÇBİR metin kolonunda geçmez.
        foreach (var deger in new[]
                 { davet.JetonHash, davet.Email, davet.IsletmeAdi, davet.OlusturanNot, davet.KullananTenantId, davet.Id })
            Assert.DoesNotContain(jeton, deger ?? "", StringComparison.Ordinal);
    }

    // ── Eşzamanlılık (GERÇEK paralel, gerçek MySQL) ──────────────────────────

    /// <summary>
    /// Aşkın'ın koşulu: sıralı simülasyon KABUL EDİLMEZ. 5 kayıt isteği
    /// <c>Task.WhenAll</c> ile AYNI ANDA, AYNI jetonla, GERÇEK MySQL'e karşı
    /// koşar. Yarışı çözen şey koşullu UPDATE'tir (kullanildi_mi = 0 iken):
    /// InnoDB satır kilidi ikinci işlemi bekletir, kilit bırakılınca 0 satır
    /// güncellenir ve istek reddedilir.
    /// </summary>
    [Fact]
    public async Task Es_Zamanli_Iki_Kayit_Ayni_Jetonla_Yalniz_Birini_Basarir()
    {
        string jeton;
        await using (var db = f.Vt.OlusturSistem()) (jeton, _) = TestKurulum.DavetOlustur(db);

        // HTTP DEĞİL: oran sınırı (3/saat) 5 paralel isteği ölçülemez hâle
        // getirirdi. Servis üretimdeki DI kapsamından çözülür.
        var yarisAdi = Isletme();
        var fabrika = f.Ortam.Services.GetRequiredService<IServiceScopeFactory>();

        async Task<bool> Dene()
        {
            await using var kapsam = fabrika.CreateAsyncScope();
            var servis = kapsam.ServiceProvider
                .GetRequiredService<Kuyumcu.Api.Modules.Auth.AuthServisi>();
            try
            {
                await servis.Kayit(new Kuyumcu.Api.Modules.Auth.KayitDto
                {
                    IsletmeAdi = yarisAdi,
                    Ad = "Y", Soyad = "Z",
                    Email = Eposta(), Sifre = Sifre, DavetJetonu = jeton,
                }, "203.0.113.9", "test");
                return true;
            }
            catch (ForbiddenException) { return false; }
        }

        var sonuclar = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Dene()));

        Assert.Equal(1, sonuclar.Count(x => x));
        await using var son = f.Vt.OlusturSistem();
        Assert.Equal(1, await son.Tenants.CountAsync(t => t.Ad == yarisAdi));
    }
}
