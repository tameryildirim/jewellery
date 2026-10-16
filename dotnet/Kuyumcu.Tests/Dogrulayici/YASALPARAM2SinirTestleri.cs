using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET YASAL-PARAM-2 finansal sınır testleri.
// (1) An sınırı TICK hassasiyetinde: değişiklik anından 1 tick önce ESKİ, tam anında YENİ.
// (2) Aynı gün düzeltme değer sınırları (önizleme — yazmaz): sıfır, eksi kuruş, null,
//     çok büyük tutar, kuruşaltı (0,005) yuvarlanmadan korunur.
// (3) Türkçe giriş: '45.000' → 45000, '1,5' → 1,5, belirsiz '1.500' reddi (FormAlani.SayiCoz —
//     düzeltme formundaki <Sayi> bileşeninin ayrıştırıcısı).
// Beklenen değerler SABİT; üretim kodundan hesaplanmaz.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class YASALPARAM2SafSinirTestleri
{
    private const string A = YasalAnahtar.MasakKimlikEsigiTl;
    private static readonly DateTime Simdi = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Bugun = new(2026, 10, 8);
    private static readonly DateTime Degisim = new(2026, 10, 8, 9, 0, 0, 123, DateTimeKind.Utc);

    private static YasalParametreSatiri Satir(string id, decimal deger, DateOnly bas, DateOnly? bit, DateTime? ani = null) =>
        new(id, A, deger, null, YasalBirim.Tl, bas, bit, "test", "https://test", YasalTeyit.Dogrulandi, "test")
        { BaslangicAni = ani };

    private static YasalParametreServisi Servis(params YasalParametreSatiri[] s) =>
        YasalParametreServisi.Yukleyicili(() => s, new SahteSaat(new DateTimeOffset(Simdi)));

    [Fact]
    public void An_siniri_tick_hassasiyetinde_1_tick_once_ESKI_tam_aninda_YENI()
    {
        var s = Servis(
            Satir("p", 370_000m, new DateOnly(2026, 10, 7), Bugun),
            Satir("w", 150_000m, Bugun, null, Degisim));
        Assert.Equal(370_000m, Assert.Single(s.DonemlerAn(A, Degisim.AddTicks(-1), Degisim.AddTicks(-1))).Deger);
        Assert.Equal(150_000m, Assert.Single(s.DonemlerAn(A, Degisim, Degisim)).Deger);
    }

    [Fact]
    public void Kurusalti_esik_degeri_yuvarlanmadan_doner()
    {
        var s = Servis(Satir("w", 150_000.005m, new DateOnly(2026, 1, 1), null));
        Assert.Equal(150_000.005m, Assert.Single(s.DonemlerAn(A, Simdi, Simdi)).Deger);
        Assert.Equal(150_000.005m, s.Sayi(A, Bugun));
    }

    [Fact]
    public void Turkce_giris_45_000_ve_1_5_kabul_belirsiz_1_500_ret()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Null(FormAlani.SayiCoz("").Deger);
    }
}

[Collection("http")]
public sealed class YASALPARAM2HttpSinirTestleri(HttpFikstur f)
{
    private const string Uc = "/api/v1/sistem/yasal-parametreler";
    private const string EB = YasalAnahtar.MasakEkBelgeEsigiTl;
    private DateOnly Bugun => YasalParametreServisi.IslemGunu(DateTime.UtcNow);
    private static string G(DateOnly g) => g.ToString("yyyy-MM-dd");
    private const string Gerekce = "Dogrulayici sinir testi: RG rakami yanlis yazildi (test).";

    private static async Task<JsonElement> Govde(HttpResponseMessage c) =>
        await c.Content.ReadFromJsonAsync<JsonElement>();

    private Dictionary<string, object?> YeniGovde(decimal deger) => new()
    {
        ["anahtar"] = EB, ["degerSayi"] = deger, ["baslangic"] = G(Bugun),
        ["kaynakUrl"] = "https://www.resmigazete.gov.tr/test", ["dayanak"] = "YASALPARAM2 dogrulayici sinir (silinecek)",
        ["gerekce"] = "YASALPARAM2 dogrulayici sinir testi bugunden gecerli",
        ["resmiGazeteTarihi"] = G(Bugun), ["resmiGazeteSayisi"] = "33401",
    };

    [Fact]
    public async Task Duzeltme_deger_sinirlari_sifir_eksi_null_cok_buyuk_400_kurusalti_korunur()
    {
        var servis = f.Ortam.Services.GetRequiredService<YasalParametreServisi>();
        var onceki = servis.SatirBul(EB, Bugun)!;
        var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
        try
        {
            var on = await sis.PostAsJsonAsync(Uc + "/onizleme", YeniGovde(10_000_000m));
            Assert.True(on.StatusCode == HttpStatusCode.OK, await on.Content.ReadAsStringAsync());
            var yaz = await sis.PostAsJsonAsync(Uc, new Dictionary<string, object?>(YeniGovde(10_000_000m))
                { ["onayKodu"] = (await Govde(on)).GetProperty("onayKodu").GetString() });
            Assert.Equal(HttpStatusCode.Created, yaz.StatusCode);
            var id = (await Govde(yaz)).GetProperty("id").GetString()!;
            var yol = $"{Uc}/{id}/duzeltme/onizleme";

            async Task Ret(object? deger, string durum)
            {
                var c = await sis.PostAsJsonAsync(yol, new Dictionary<string, object?> { ["degerSayi"] = deger, ["gerekce"] = Gerekce });
                Assert.True(c.StatusCode == HttpStatusCode.BadRequest, $"{durum}: {c.StatusCode} {await c.Content.ReadAsStringAsync()}");
                Assert.True((await Govde(c)).GetProperty("alanlar").TryGetProperty("degerSayi", out _), durum);
            }
            await Ret(0m, "sifir");
            await Ret(-0.01m, "eksi kurus");
            await Ret(null, "null");
            await Ret(999_999_999.99m, "cok buyuk (onceki x10 ustu)");

            var k = await sis.PostAsJsonAsync(yol, new Dictionary<string, object?> { ["degerSayi"] = 10_000_000.005m, ["gerekce"] = Gerekce });
            Assert.True(k.StatusCode == HttpStatusCode.OK, await k.Content.ReadAsStringAsync());
            Assert.Equal(10_000_000.005m, (await Govde(k)).GetProperty("yeniDeger").GetDecimal());

            // Önizlemeler hiçbir şey yazmadı: tek sistem satırı (yanlış giriş), açık uçlu.
            using var db = f.Vt.OlusturSistem();
            var sistemSatirlari = db.YasalParametreler.Where(p => p.Anahtar == EB && p.Olusturan.StartsWith("sistem:")).ToList();
            var tek = Assert.Single(sistemSatirlari);
            Assert.Null(tek.GecerlilikBitis);
            Assert.Equal(10_000_000m, tek.DegerSayi);
        }
        finally
        {
            using var db = f.Vt.OlusturSistem();
            db.Database.ExecuteSqlRaw(
                "DELETE FROM yasal_parametreler WHERE anahtar = 'MASAK_EK_BELGE_ESIGI_TL' AND olusturan LIKE 'sistem:%'");
            db.Database.ExecuteSqlRaw("UPDATE yasal_parametreler SET gecerlilik_bitis = NULL WHERE id = {0}", onceki.Id);
            servis.Tazele();
        }
    }
}
