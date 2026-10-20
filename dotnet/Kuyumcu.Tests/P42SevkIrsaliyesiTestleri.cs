using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// PAKET-42 (20.10) — sevk irsaliyesi: TASLAK → SEVK_EDILDI → TESLIM_EDILDI durum makinesi, taşıyan/plaka/teslim alanları,
// sevk sonrası başlık kilidi, irsaliyesiz satış listesi.
[Collection("http")]
public sealed class P42SevkIrsaliyesiHttpTestleri(HttpFikstur f)
{
    private static async Task<JsonElement> Oku(HttpResponseMessage r) =>
        JsonSerializer.Deserialize<JsonElement>(await r.Content.ReadAsStringAsync());

    private static async Task<string> IrsaliyeAc(HttpClient s, Kiraci k, string? hareketId = null)
    {
        var r = await s.PostAsJsonAsync("/api/v1/irsaliyeler", new
        {
            yon = "GIDEN", cariId = k.CariId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, hareketId } },
        });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        return (await Oku(r)).GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Durum_makinesi_TASLAK_SEVK_TESLIM_sirayla_gecer_atlanamaz_ve_sevk_sonrasi_baslik_kilitlenir()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "P42-sevk");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var id = await IrsaliyeAc(s, k);

        var detay = await s.GetFromJsonAsync<JsonElement>($"/api/v1/irsaliyeler/{id}");
        Assert.Equal("TASLAK", detay.GetProperty("sevkDurumu").GetString());

        // TASLAK → TESLIM doğrudan: 400 (makine atlanmaz)
        var atla = await s.PostAsJsonAsync($"/api/v1/irsaliyeler/{id}/teslim-edildi", new { teslimAlanAd = "Ali Veli" });
        Assert.Equal(HttpStatusCode.BadRequest, atla.StatusCode);

        // Sevk: plaka normalize edilir, zaman şimdi
        var sevk = await s.PostAsJsonAsync($"/api/v1/irsaliyeler/{id}/sevk-et", new { tasiyanAd = "Kargo A.Ş.", plaka = "34 abc 123" });
        Assert.True(sevk.IsSuccessStatusCode, await sevk.Content.ReadAsStringAsync());
        var d2 = await Oku(sevk);
        Assert.Equal("SEVK_EDILDI", d2.GetProperty("sevkDurumu").GetString());
        Assert.Equal("34ABC123", d2.GetProperty("plaka").GetString());
        Assert.Equal("Kargo A.Ş.", d2.GetProperty("tasiyanAd").GetString());
        Assert.False(d2.GetProperty("duzenlenebilir").GetBoolean());
        Assert.Contains("sevk edildi", d2.GetProperty("kilitSebebi").GetString());

        // Sevk sonrası başlık değişmez (409); ikinci sevk 409
        var patch = await s.PatchAsJsonAsync($"/api/v1/irsaliyeler/{id}", new { aciklama = "sonradan" });
        Assert.Equal(HttpStatusCode.Conflict, patch.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await s.PostAsJsonAsync($"/api/v1/irsaliyeler/{id}/sevk-et", new { })).StatusCode);

        // Teslim: ad zorunlu (400), sonra TESLIM_EDILDI
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync($"/api/v1/irsaliyeler/{id}/teslim-edildi", new { })).StatusCode);
        var teslim = await s.PostAsJsonAsync($"/api/v1/irsaliyeler/{id}/teslim-edildi", new { teslimAlanAd = "Ali Veli" });
        Assert.True(teslim.IsSuccessStatusCode, await teslim.Content.ReadAsStringAsync());
        var d3 = await Oku(teslim);
        Assert.Equal("TESLIM_EDILDI", d3.GetProperty("sevkDurumu").GetString());
        Assert.Equal("Ali Veli", d3.GetProperty("teslimAlanAd").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await s.PostAsJsonAsync($"/api/v1/irsaliyeler/{id}/teslim-edildi", new { teslimAlanAd = "X" })).StatusCode);

        // Liste satırında da durum görünür; kâğıt çıktıda taşıyan/plaka/teslim alan yazar
        var liste = await s.GetFromJsonAsync<JsonElement>("/api/v1/irsaliyeler?limit=50");
        var satir = liste.GetProperty("satirlar").EnumerateArray().Single(x => x.GetProperty("id").GetString() == id);
        Assert.Equal("TESLIM_EDILDI", satir.GetProperty("sevkDurumu").GetString());
        var cikti = await s.GetStringAsync($"/api/v1/cikti/IRSALIYE/{id}");
        Assert.Contains("Taşıyan: Kargo A.Ş.", cikti);
        Assert.Contains("Plaka: 34ABC123", cikti);
        Assert.Contains("Teslim alan: Ali Veli", cikti);
    }

    [Fact]
    public async Task Irsaliyesiz_satis_listesi_bagli_olmayan_satisi_gosterir_irsaliyeye_baglanan_duser()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "P42-irsaliyesiz");
        var s = f.Ortam.Istemci(k, "SAHIP");
        string hareketId;
        using (var db = f.Vt.Olustur(k))
        {
            var now = DateTime.UtcNow;
            hareketId = Kimlik.Ulid();
            HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
            {
                Id = hareketId, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS", Tarih = now, TlTutari = 10_000m,
                HasGrami = 1.5m, Maden = Maden.Altin, ParaBirimi = "TRY", KurSnapshotId = k.KurId, CariId = k.CariId,
                UrunId = k.UrunId, KullaniciId = k.KullaniciId, Olusturuldu = now,
            });
            await db.SaveChangesAsync();
        }

        var once = await s.GetFromJsonAsync<JsonElement>("/api/v1/irsaliyeler/irsaliyesiz-sevk?gun=7");
        Assert.Contains(once.GetProperty("satirlar").EnumerateArray(), x => x.GetProperty("hareketId").GetString() == hareketId);

        // Yabancı/olmayan hareket 400
        var kotu = await s.PostAsJsonAsync("/api/v1/irsaliyeler", new { yon = "GIDEN", cariId = k.CariId, kalemler = new[] { new { urunId = k.UrunId, adet = 1m, hareketId = "YOK" } } });
        Assert.Equal(HttpStatusCode.BadRequest, kotu.StatusCode);

        await IrsaliyeAc(s, k, hareketId);
        var sonra = await s.GetFromJsonAsync<JsonElement>("/api/v1/irsaliyeler/irsaliyesiz-sevk?gun=7");
        Assert.DoesNotContain(sonra.GetProperty("satirlar").EnumerateArray(), x => x.GetProperty("hareketId").GetString() == hareketId);
    }
}

public sealed class P42SevkIrsaliyesiEkranTestleri : BunitContext
{
    private const string Liste = """
        {"satirlar":[{"id":"i1","no":"IRS-2026-00003","yon":"GIDEN","durum":"ACIK","cariId":"c2","cariAdi":"Ayşe Demir","subeId":"sb","tarih":"2026-10-20T08:10:00Z","faturaId":null,"satinalmaSiparisId":null,"aciklama":null,"hesapKodu":null,"bagsiz":true,"kalemSayisi":1,"sevkDurumu":"TASLAK"}],
         "toplam":1,"bagsizToplam":1,"limit":200,"azamiLimit":200,"sinirDegdi":false}
        """;
    private const string Irsaliyesiz = """
        {"satirlar":[{"hareketId":"h9","tarih":"2026-10-19T10:00:00Z","cariId":"c3","cariAdi":"Veli Can","urunAdi":"Çeyrek altın","hasGrami":1.603,"tlTutari":7400}],"toplam":1,"gun":30,"sinirDegdi":false}
        """;
    private static string Detay(string sevk, string ek = "") => $$"""
        {"id":"i1","no":"IRS-2026-00003","yon":"GIDEN","durum":"ACIK","cariId":"c2","cariAdi":"Ayşe Demir","subeId":"sb",
         "tarih":"2026-10-20T08:10:00Z","satinalmaSiparisId":null,"faturaId":null,"aciklama":null,"hesapKodu":null,"iptalGerekce":null,
         "olusturuldu":"2026-10-20T08:10:00Z","guncellendi":"2026-10-20T08:10:00Z",
         "kalemler":[{"id":"ik1","urunId":"u1","urunAdi":"22 ayar bilezik","birim":"ADET","tanim":null,"adet":1,"hareketId":null,"sira":0}],
         "bagsiz":true,"duzenlenebilir":{{(sevk == "TASLAK" ? "true" : "false")}},"kilitSebebi":null,"sevkDurumu":"{{sevk}}"{{ek}}}
        """;

    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string detayGovde = Detay("TASLAK");

    public P42SevkIrsaliyesiEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb", null, ArayuzModuServisi.Profesyonel));
    }

    private sealed class SahteApi(P42SevkIrsaliyesiEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            string? yanit =
                yol.Contains("/irsaliyeler/irsaliyesiz-sevk") ? Irsaliyesiz
                : yol.EndsWith("/sevk-et") ? (t.detayGovde = Detay("SEVK_EDILDI", ",\"tasiyanAd\":\"Kargo A.Ş.\",\"plaka\":\"34ABC123\",\"fiiliSevkTarihi\":\"2026-10-20T09:00:00Z\""))
                : yol.EndsWith("/teslim-edildi") ? (t.detayGovde = Detay("TESLIM_EDILDI", ",\"tasiyanAd\":\"Kargo A.Ş.\",\"plaka\":\"34ABC123\",\"fiiliSevkTarihi\":\"2026-10-20T09:00:00Z\",\"teslimAlanAd\":\"Ali Veli\",\"teslimTarihi\":\"2026-10-20T12:00:00Z\""))
                : yol.Contains("/irsaliyeler/i1") ? t.detayGovde
                : yol.Contains("/irsaliyeler?") ? Liste
                : null;
            return yanit is null
                ? new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("""{"message":"tanımsız uç"}""", System.Text.Encoding.UTF8, "application/json") }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(yanit, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    [Fact]
    public void Sevk_ve_teslim_dugmeleri_duruma_gore_gelir_irsaliyesiz_satislar_katlanir_kapali()
    {
        var e = Render<Kuyumcu.Web.Pages.Irsaliyeler>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#irs-ac-i1")));

        // İrsaliyesiz satışlar: katlanır kapalı gelir, sayısı başlıkta; içinde satır var
        var kat = e.Find("#irs-irsaliyesiz");
        Assert.Contains("1", kat.TextContent);
        Assert.Empty(e.FindAll("#irs-irsaliyesiz-govde"));   // kapalı gelir
        e.Find("#irs-irsaliyesiz-ac").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#irs-irsaliyesiz-h9")));

        e.Find("#irs-ac-i1").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#irs-detay-sevk")));
        Assert.Contains("Henüz yola çıkmadı", e.Find("#irs-detay-sevk").TextContent);
        Assert.NotEmpty(e.FindAll("#irs-sevk"));          // "Yola çıktı" katlanırı var, kapalı (zorunlu önce)
        Assert.Empty(e.FindAll("#irs-teslim"));
        e.Find("#irs-sevk-ac").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#irs-sevk-et")));

        e.Find("#irs-sevk-tasiyan").Input("Kargo A.Ş.");
        e.Find("#irs-sevk-plaka").Input("34 abc 123");
        e.Find("#irs-sevk-et").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#irs-teslim")));
        Assert.Empty(e.FindAll("#irs-sevk"));
        e.Find("#irs-teslim-ac").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#irs-teslim-et")));
        var sevkIstek = istekler.Single(x => x.Yol.EndsWith("/sevk-et"));
        Assert.Contains("\"plaka\":\"34 abc 123\"", sevkIstek.Govde);   // ekran normalize ETMEZ, sunucu eder
        Assert.Contains("\"tasiyanAd\":\"Kargo A.", sevkIstek.Govde);
        Assert.Contains("Yola çıktı", e.Find("#irs-detay-sevk").TextContent);
        Assert.Contains("34ABC123", e.Find("#irs-detay-sevk").TextContent);

        // Teslim alan boşken düğme pasif; yazınca açılır
        Assert.True(e.Find("#irs-teslim-et").HasAttribute("disabled"));
        e.Find("#irs-teslim-alan").Input("Ali Veli");
        e.WaitForAssertion(() => Assert.False(e.Find("#irs-teslim-et").HasAttribute("disabled")));
        e.Find("#irs-teslim-et").Click();
        e.WaitForAssertion(() => Assert.Contains("Teslim edildi", e.Find("#irs-detay-sevk").TextContent));
        Assert.Contains("Ali Veli", e.Find("#irs-detay-sevk").TextContent);
        Assert.Empty(e.FindAll("#irs-teslim"));
        Assert.Empty(e.FindAll("#irs-sevk"));
    }
}
