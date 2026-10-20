using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// PAKET-05 (20.10) — sayım tutanağı (/cikti/SAYIM_TUTANAGI) ve gün sonu kasa fişi (/cikti/GUN_KAPANISI); kapanış yanıtı kapanisId taşır.
[Collection("http")]
public sealed class P05SayimTutanagiKasaFisiTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Sayim_tutanagi_satirlari_farki_ve_ara_tutanak_uyarisini_basar()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "P05-sayim");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var oturumId = Kimlik.Ulid();
        using (var db = f.Vt.Olustur(k))
        {
            var now = DateTime.UtcNow;
            db.SayimOturumlari.Add(new SayimOturumu { Id = oturumId, TenantId = k.TenantId, SubeId = k.SubeId, Ad = "Ekim sayımı", Durum = "ACIK", Acildi = now, AcanKullaniciId = k.KullaniciId, Notlar = "Vitrin 1" });
            db.SayimSatirlari.Add(new SayimSatiri { Id = Kimlik.Ulid(), TenantId = k.TenantId, OturumId = oturumId, UrunId = k.UrunId, SistemAdet = 5m, SayilanAdet = 4m, Sayildi = now, SayanKullaniciId = k.KullaniciId });
            await db.SaveChangesAsync();
        }
        var d = await s.GetStringAsync($"/api/v1/cikti/SAYIM_TUTANAGI/{oturumId}");
        Assert.Contains("SAYIM TUTANAĞI", d);
        Assert.Contains("Ekim sayımı", d);
        Assert.Contains("ARA TUTANAK", d);
        Assert.Contains("Eksik: 1", d);
        Assert.Contains("Vitrin 1", d);
        var j = JsonSerializer.Deserialize<JsonElement>(d);
        var satir = j.GetProperty("bolumler")[0].GetProperty("satirlar")[0].GetProperty("hucreler").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Equal([5, 4, -1], satir.Skip(2).Select(x => int.Parse(System.Text.RegularExpressions.Regex.Match(x!.Replace("−", "-"), @"^-?\d+").Value)).ToList());   // biçimden bağımsız (5,000 / −1,000)
    }

    [Fact]
    public async Task Gun_kapanisi_yaniti_kapanisId_tasir_ve_fis_dokumu_defter_sayilan_fark_basar()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "P05-kasa");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var r = await s.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi", new { sayilanNakit = TestKurulum.KasaAcilis - 100m, aciklama = "Bozuk para eksik çıktı, sayım yeniden yapıldı" });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        var y = JsonSerializer.Deserialize<JsonElement>(await r.Content.ReadAsStringAsync());
        var kapanisId = y.GetProperty("kapanisId").GetString();
        Assert.False(string.IsNullOrEmpty(kapanisId));
        var d = await s.GetStringAsync($"/api/v1/cikti/GUN_KAPANISI/{kapanisId}");
        Assert.Contains("GÜN SONU KASA FİŞİ", d);
        Assert.Contains("Defter nakit", d);
        Assert.Contains("Sayılan nakit", d);
        Assert.Contains("EKSİK nakit", d);
        Assert.Contains("Bozuk para eksik çıktı", d);
    }
}
