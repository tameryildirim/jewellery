using System.Net.Http.Json;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Kuyumcu.Tests;

// 20.10 — raporsuz paket kalanları 2. parti: PWA bandı (44), sipariş özet ucu (03), POS–fatura kalemli mutabakat (01 B-3).
// E-belge olay izi/XML testleri EBelgeHttpTestleri'ndedir.
public sealed class PwaBandiTestleri : BunitContext
{
    public PwaBandiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<PwaServisi>();
        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
    }

    [Fact]
    public void Bant_varsayilan_gizli_yeni_surum_ve_cevrimdisi_sirasiyla_gorunur_cevrimdisi_once()
    {
        var pwa = Services.GetRequiredService<PwaServisi>();
        var e = Render<Kuyumcu.Web.Bilesenler.PwaBandi>();
        Assert.Empty(e.FindAll("#pwa-yeni-surum, #pwa-cevrimdisi"));
        pwa.GuncellemeHazir();
        e.WaitForAssertion(() => Assert.Equal("Yeni sürüm hazır. Şimdi yenile", System.Text.RegularExpressions.Regex.Replace(e.Find("#pwa-yeni-surum").TextContent, @"\s+", " ").Trim()));
        pwa.AgDurumu(false);
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#pwa-cevrimdisi")));
        Assert.Empty(e.FindAll("#pwa-yeni-surum"));   // çevrimdışı önce
        pwa.AgDurumu(true);
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#pwa-yenile")));
    }

    [Fact]
    public void Pwa_servisi_JS_yokken_sessiz_kalir_durumlar_degismez()
    {
        var pwa = Services.GetRequiredService<PwaServisi>();
        Assert.False(pwa.GuncellemeVar); Assert.False(pwa.KurulabilirMi); Assert.False(pwa.Cevrimdisi);
        pwa.KurulumMumkun(true);
        Assert.True(pwa.KurulabilirMi);
    }
}

[Collection("http")]
public sealed class RaporsuzPaket2HttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Siparis_ozeti_acik_geciken_bugun_teslim_sayar()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "RP2-siparis");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var bugun = Zaman.YerelBugun();
        foreach (var gun in new[] { bugun.AddDays(5), bugun })
        {
            var r = await s.PostAsJsonAsync("/api/v1/siparisler", new
            {
                tur = "MUSTERI_SIPARISI", cariId = k.CariId, sozVerilenGun = gun.ToString("yyyy-MM-dd"),
                kalemler = new[] { new { tanim = "Alyans", adet = 1m, gramAgirlik = 5m } },
            });
            Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        }
        using (var db = f.Vt.OlusturSistem())
        {
            // Geciken: söz günü dün olan açık sipariş (sunucu geçmiş tarihi reddeder → doğrudan yazılır).
            var sp = await db.Siparisler.FirstAsync(x => x.TenantId == k.TenantId);
            var gecik = new Siparis
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, No = "S-GECIK", Tur = sp.Tur, Durum = sp.Durum, CariId = k.CariId,
                SozVerilenGun = bugun.AddDays(-1), Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
                OlusturanKullaniciId = sp.OlusturanKullaniciId, SubeId = sp.SubeId,
            };
            db.Siparisler.Add(gecik);
            await db.SaveChangesAsync();
        }
        var o = await s.GetFromJsonAsync<JsonElement>("/api/v1/siparisler/ozet");
        Assert.Equal(3, o.GetProperty("acik").GetInt32());
        Assert.Equal(1, o.GetProperty("geciken").GetInt32());
        Assert.Equal(1, o.GetProperty("bugunTeslim").GetInt32());
        Assert.True(o.GetProperty("durumKirilimi").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Mutabakat_KALEM_faturasinin_satir_toplami_belgeyle_tutmuyorsa_uyari_yazar()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "RP2-mutabakat");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var bugun = Zaman.YerelBugun();
        using (var db = f.Vt.OlusturSistem())
        {
            var fid = Kimlik.Ulid();
            db.Faturalar.Add(new Fatura
            {
                Id = fid, TenantId = k.TenantId, CariId = k.CariId, FaturaNo = "FTR-UYUMSUZ", Tur = "SATIS", Durum = "KESILDI", KalemKaynagi = "KALEM",
                AraToplam = 1_000m, KdvOran = 0.2m, KdvTutar = 200m, Toplam = 1_200m, Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
            });
            db.FaturaKalemleri.Add(new FaturaKalemi
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, FaturaId = fid, Sira = 1, Aciklama = "Bilezik", Miktar = 1m, Birim = "ADET",
                BirimFiyat = 900m, KdvTuru = "STANDART", KdvOran = 0.2m, KdvMatrah = 900m, KdvTutar = 180m, SatirToplam = 1_080m,   // 1.080 ≠ 1.200
                Olusturuldu = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }
        var r = await s.GetFromJsonAsync<JsonElement>($"/api/v1/raporlar/pos-fatura-mutabakati?tarih={bugun:yyyy-MM-dd}");
        var uyarilar = r.GetProperty("uyarilar").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Contains(uyarilar, u => u!.Contains("satır toplamı") && u.Contains("FTR-UYUMSUZ"));
    }
}
