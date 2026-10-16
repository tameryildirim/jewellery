using System.Net;
using System.Text;
using System.Text.Json;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET GOC-A TUR 2 finansal sınır testleri (08.10.2026).
// HTTP yolunda (controller + DTO + servis kapısı birlikte) para sınırları. Beklenen değerler
// SABİT; üretim kodundan hesaplanmaz. Kural: fazla ondalık yuvarlanmaz → 400; satır DOĞMAZ.
// olumsuz-fikstür: dosya — geçersiz değerler bilinçlidir.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class GOCASinirTestleriTur2(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETGOC-A-TEST dg2 " + ad);
    }

    private static StringContent Ham(string json) => new(json, Encoding.UTF8, "application/json");

    private static async Task<string> RedAlan(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), govde);
        Assert.Contains(alanlar.EnumerateObject(), p => p.Name.Equals(alan, StringComparison.OrdinalIgnoreCase));
        return govde;
    }

    private int KasaHareketSayisi(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.KasaHareketleri.AsNoTracking().Count(h => h.TenantId == k.TenantId);
    }

    [Theory]
    [InlineData("0")]          // sıfır
    [InlineData("-100")]       // eksi
    [InlineData("0.005")]      // kuruş altı — yuvarlanıp 0,01 OLMAZ
    [InlineData("1.005")]      // kuruş altı — yuvarlanıp 1,01/1,00 OLMAZ
    [InlineData("null")]       // boş
    public async Task DG2_S01_Kasa_hareket_tutar_sinirlari_400_satir_DOGMAZ(string tutar)
    {
        var k = YeniKiraci("kasa " + tutar);
        var c = f.Ortam.Istemci(k);
        var r = await c.PostAsync($"/api/v1/kasalar/{k.KasaId}/hareket",
            Ham($$"""{"yon":"GIRIS","tutar":{{tutar}},"odemeTuru":"NAKIT","aciklama":"dg2 sinir"}"""));
        await RedAlan(r, "tutar");
        Assert.Equal(0, KasaHareketSayisi(k));
    }

    [Fact]
    public async Task DG2_S02_Kasa_hareket_tam_kurus_0_01_kabul_birebir_yazilir()
    {
        var k = YeniKiraci("kasa kurus");
        var c = f.Ortam.Istemci(k);
        var r = await c.PostAsync($"/api/v1/kasalar/{k.KasaId}/hareket",
            Ham("""{"yon":"GIRIS","tutar":0.01,"odemeTuru":"NAKIT","aciklama":"dg2 kurus"}"""));
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0.01m, db.KasaHareketleri.AsNoTracking().Single(h => h.TenantId == k.TenantId).Tutar);
    }

    private async Task<string> BankaHesabi(HttpClient c)
    {
        var r = await c.PostAsync("/api/v1/bankalar",
            Ham($$"""{"bankaAdi":"DG2 Banka","hesapAdi":"Vadesiz","iban":"{{TestIban.Uret()}}","paraBirimi":"TRY"}"""));
        var g = await r.Content.ReadAsStringAsync();
        Assert.True(r.IsSuccessStatusCode, g);
        return JsonDocument.Parse(g).RootElement.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task DG2_S03_Banka_dekont_999_999_999_99_kabul_birebir_10_uzeri_13_ve_1_005_red()
    {
        var k = YeniKiraci("banka buyuk");
        var c = f.Ortam.Istemci(k);
        var hesap = await BankaHesabi(c);
        await RedAlan(await c.PostAsync($"/api/v1/bankalar/{hesap}/hareket",
            Ham("""{"yon":"GELEN","tutar":10000000000000}""")), "tutar");
        await RedAlan(await c.PostAsync($"/api/v1/bankalar/{hesap}/hareket",
            Ham("""{"yon":"GELEN","tutar":1.005}""")), "tutar");
        await RedAlan(await c.PostAsync($"/api/v1/bankalar/{hesap}/hareket",
            Ham("""{"yon":"GELEN","tutar":-1}""")), "tutar");
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(0, db.BankaHareketleri.AsNoTracking().Count(h => h.TenantId == k.TenantId));
        // 999.999.999,99 tutar kuralından GEÇER; dekont MASAK kapısına takılır (370.000 TL üstü kimlik/beyan
        // zorunlu) — red gerekçesi 'tutar' alanı OLMAMALI, MASAK olmalı (tavan kuralı yanlışlıkla 400 vermiyor).
        var buyuk = await c.PostAsync($"/api/v1/bankalar/{hesap}/hareket",
            Ham("""{"yon":"GELEN","tutar":999999999.99}"""));
        var bg = await buyuk.Content.ReadAsStringAsync();
        if (!buyuk.IsSuccessStatusCode)
        {
            Assert.Contains("MASAK", bg);
            var j = JsonDocument.Parse(bg).RootElement;
            if (j.TryGetProperty("alanlar", out var al))
                Assert.DoesNotContain(al.EnumerateObject(), p => p.Name.Equals("tutar", StringComparison.OrdinalIgnoreCase));
        }
        Assert.Equal(999_999_999.99m, Kuyumcu.Api.Common.Dogrulama.GirdiKapisi.Tutar(999_999_999.99m, "tutar"));
        // Eşik altı büyük tutar (199.999,99; MASAK 200.000 eşiği altı) birebir yazılır — yuvarlama yok.
        var ok = await c.PostAsync($"/api/v1/bankalar/{hesap}/hareket",
            Ham("""{"yon":"GELEN","tutar":199999.99}"""));
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        using var db2 = f.Vt.OlusturSistem();
        Assert.Equal(199_999.99m, db2.BankaHareketleri.AsNoTracking().Single(h => h.TenantId == k.TenantId).Tutar);
    }

    [Fact]
    public async Task DG2_S04_Emanet_gram_0_0001_red_0_001_kabul_has_kur_4_hane_birebir()
    {
        var k = YeniKiraci("emanet gram");
        var c = f.Ortam.Istemci(k);
        await RedAlan(await c.PostAsync("/api/v1/emanet/giris",
            Ham($$"""{"cariId":"{{k.CariId}}","ayarId":"K22","gramAgirlik":0.0001,"kurSnapshotId":"{{k.KurId}}"}""")), "gramAgirlik");
        await RedAlan(await c.PostAsync("/api/v1/emanet/giris",
            Ham($$"""{"cariId":"{{k.CariId}}","ayarId":"K22","gramAgirlik":-1,"kurSnapshotId":"{{k.KurId}}"}""")), "gramAgirlik");
        // Kur: has fiyatı 4 hane (0,0001 çözünürlük) birebir; 5. hane 400.
        await RedAlan(await c.PostAsync("/api/v1/kur/manuel", Ham("""{"altinHasFiyati":0.00001}""")), "altinHasFiyati");
        // PAKET KUR-BANT (09.10): 0,0001 TL has fiyatı artık makul bant dışı (500–50.000) → 4 hane
        // çözünürlüğü bant içindeki bir değerle sınanır (fikstür has 6.827,20).
        var ok = await c.PostAsync("/api/v1/kur/manuel", Ham("""{"altinHasFiyati":6827.0001}"""));
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        using var db = f.Vt.OlusturSistem();
        var s = db.KurSnapshots.AsNoTracking().Where(x => x.TenantId == k.TenantId).OrderByDescending(x => x.Olusturuldu).First();
        Assert.Equal(6_827.0001m, s.AltinHasFiyati);
    }

    [Fact]
    public void DG2_S05_Turkce_giris_tutar_ayristirma()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(0.01m, FormAlani.SayiCoz("0,01").Deger);
        var b = FormAlani.SayiCoz("1.500");
        Assert.Null(b.Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, b.Sonuc);
        Assert.Null(FormAlani.SayiCoz("").Deger);
        Assert.Null(FormAlani.SayiCoz(null).Deger);
    }
}
