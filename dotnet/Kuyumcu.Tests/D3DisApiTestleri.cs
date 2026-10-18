using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Contracts.Sayfalar.Isletme;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D3-11 — dış API: anahtar yalnız sahipte, düz metin saklanmaz; kapsam kapısı; iptal; canlı kur kanalı (SignalR).
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class D3DisApiTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Anahtar_kapsam_iptal_ve_veri_kiraciya_sabit()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D3API");
        var s = f.Ortam.Istemci(k, "SAHIP");
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "MUDUR").PostAsJsonAsync("/api/v1/api-anahtarlari", new { ad = "x", kapsamlar = new[] { "KUR" } })).StatusCode);
        var yeni = (await (await s.PostAsJsonAsync("/api/v1/api-anahtarlari", new { ad = "Web sitesi", kapsamlar = new[] { "KUR", "URUN" } })).Content.ReadFromJsonAsync<YeniApiAnahtari>())!;
        Assert.StartsWith("mhk_", yeni.Anahtar);
        var sadeceKur = (await (await s.PostAsJsonAsync("/api/v1/api-anahtarlari", new { ad = "Vitrin ekranı", kapsamlar = new[] { "KUR" } })).Content.ReadFromJsonAsync<YeniApiAnahtari>())!;
        using (var db = f.Vt.OlusturSistem())
        {
            var kayit = db.ApiAnahtarlari.AsNoTracking().Single(a => a.Id == yeni.Ozet.Id);
            Assert.NotEqual(yeni.Anahtar, kayit.Ozet);
            Assert.DoesNotContain(yeni.Anahtar, JsonSerializer.Serialize(kayit));   // düz anahtar hiçbir alanda yok
        }
        Assert.DoesNotContain(yeni.Anahtar, await s.GetStringAsync("/api/v1/api-anahtarlari"));

        var dis = f.Ortam.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await dis.GetAsync("/api/disa/v1/kur")).StatusCode);
        HttpRequestMessage Istek(string yol, string anahtar) { var m = new HttpRequestMessage(HttpMethod.Get, yol); m.Headers.Add("X-Api-Anahtari", anahtar); return m; }
        var kur = await dis.SendAsync(Istek("/api/disa/v1/kur", yeni.Anahtar));
        Assert.Equal(HttpStatusCode.OK, kur.StatusCode);
        Assert.Contains("altinHasFiyati", await kur.Content.ReadAsStringAsync());
        var ur = await dis.SendAsync(Istek("/api/disa/v1/urunler", yeni.Anahtar));
        Assert.Equal(HttpStatusCode.OK, ur.StatusCode);
        var j = JsonDocument.Parse(await ur.Content.ReadAsStringAsync()).RootElement;
        Assert.True(j.GetProperty("toplam").GetInt32() >= 1);
        Assert.Equal(HttpStatusCode.Forbidden, (await dis.SendAsync(Istek("/api/disa/v1/urunler", sadeceKur.Anahtar))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await dis.SendAsync(Istek("/api/disa/v1/kur", yeni.Anahtar + "x"))).StatusCode);

        Assert.True((await s.PostAsync($"/api/v1/api-anahtarlari/{yeni.Ozet.Id}/iptal", null)).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await dis.SendAsync(Istek("/api/disa/v1/kur", yeni.Anahtar))).StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.NotNull(db.ApiAnahtarlari.AsNoTracking().Single(a => a.Id == sadeceKur.Ozet.Id).SonKullanim);
    }

    [Fact]
    public async Task Canli_kur_kanali_anahtarla_baglanir_kur_yazilinca_yayin_alir_gecersiz_anahtar_reddedilir()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D3HUB");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var anahtar = (await (await s.PostAsJsonAsync("/api/v1/api-anahtarlari", new { ad = "Kur ekranı", kapsamlar = new[] { "KUR" } })).Content.ReadFromJsonAsync<YeniApiAnahtari>())!.Anahtar;
        HubConnection Baglanti(string a) => new HubConnectionBuilder()
            .WithUrl(new Uri(f.Ortam.Server.BaseAddress, $"/hubs/disa-kur?anahtar={a}"), o =>
            {
                o.HttpMessageHandlerFactory = _ => f.Ortam.Server.CreateHandler();
                o.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
            }).Build();
        var gelen = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var h = Baglanti(anahtar);
        h.On<JsonElement>("KurGuncellendi", v => gelen.TrySetResult(v));
        await h.StartAsync();
        var r = await s.PostAsJsonAsync("/api/v1/kur/manuel", new { altinHasFiyati = 6_901.25m });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        var v = await gelen.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(6_901.25m, v.GetProperty("altinHasFiyati").GetDecimal());

        await using var kotu = Baglanti("mhk_yanlis0000000000000000000000000000000000000000");
        var kapandi = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        kotu.Closed += _ => { kapandi.TrySetResult(); return Task.CompletedTask; };
        try { await kotu.StartAsync(); await kapandi.Task.WaitAsync(TimeSpan.FromSeconds(10)); }
        catch (Exception) { /* sunucu bağlantıyı başlangıçta da düşürebilir — ikisi de ret */ }
        Assert.NotEqual(HubConnectionState.Connected, kotu.State);
    }
}
