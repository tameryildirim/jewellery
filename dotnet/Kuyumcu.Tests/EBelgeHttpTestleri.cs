using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.EBelge;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// E-belge uçtan uca (20.10): SAHTE NİLVERA (HttpMessageHandler) — mükellef sorgusu, model gönderimi, durum,
// iptal. Kesilen fatura kendiliğinden kuyruğa girer → e-Arşiv/e-Fatura seçilir → gönderilir → GİB sonucu
// okunur. Geçici hata yeniden denenir, kalıcı hata sade dille durur; 8 gün içinde e-Arşiv iptali; e-Fatura
// iptali reddedilir ve fatura iptal OLMAZ. Anahtar ne yanıtta ne veritabanında düz.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class EBelgeHttpTestleri(HttpFikstur f)
{
    private const string Anahtar = "NILVERA-test-anahtari-7c1e", IsletmeVkn = "1234567890", AliciTc = "10000000146", AliciVkn = "9876543217";

    private sealed class SahteNilvera : HttpMessageHandler
    {
        public List<(string Metot, string Yol, string Govde, string? Yetki)> Istekler { get; } = [];
        public bool Kayitli { get; set; }
        public Func<HttpResponseMessage>? GonderYaniti { get; set; }
        public string DurumJson { get; set; } = """{"StatusCode":"succeed","ReportStatus":"Reported","CancelStatus":false}""";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken c)
        {
            var yol = r.RequestUri!.AbsolutePath;
            var govde = r.Content is null ? "" : await r.Content.ReadAsStringAsync(c);
            lock (Istekler) Istekler.Add((r.Method.Method, yol, govde, r.Headers.Authorization?.ToString()));
            if (yol.StartsWith("/general/GlobalCompany/Check/TaxNumber/"))
                return Json(Kayitli && !yol.EndsWith(IsletmeVkn) ? """[{"TaxNumber":"x","Name":"urn:mail:defaultpk@alici.com"}]""" : "[]");
            if (yol.EndsWith("/Send/Model"))
                return GonderYaniti?.Invoke() ?? Json("""{"UUID":"x","InvoiceNumber":"MHK2026000000001"}""");
            if (yol.EndsWith("/Status")) return Json(DurumJson);
            if (yol.EndsWith("/xml")) return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<?xml version=\"1.0\"?><Invoice/>", System.Text.Encoding.UTF8, "application/xml") };
            if (yol.EndsWith("/Cancel")) return Json("""["iptal edildi"]""");
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }

    private static HttpResponseMessage Json(string s, HttpStatusCode d = HttpStatusCode.OK) =>
        new(d) { Content = new StringContent(s, System.Text.Encoding.UTF8, "application/json") };

    private (WebApplicationFactory<Program> O, SahteNilvera N) Kur()
    {
        var n = new SahteNilvera();
        return (f.Ortam.WithWebHostBuilder(b => b.ConfigureServices(s =>
            s.AddHttpClient(EBelgeServisi.HttpAdi).ConfigurePrimaryHttpMessageHandler(() => n))), n);
    }

    private static HttpClient Istemci(WebApplicationFactory<Program> o, Kiraci k, string rol)
    {
        var token = new TokenServisi(new JwtAyarlar { Secret = HttpOrtami.TestSirri, AccessDakika = 15, RefreshGun = 7 })
            .AccessUret(k.KullaniciId, k.TenantId, k.SubeId, rol, false);
        var c = o.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private Kiraci Kiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "EB-" + ad);
        var t = db.Tenants.Single(x => x.Id == k.TenantId);
        t.VergiNo = IsletmeVkn; t.VergiDairesi = "Kapalıçarşı";
        db.SaveChanges();
        return k;
    }

    private string FaturaKes(Kiraci k, string? aliciNo = AliciTc)
    {
        using var db = f.Vt.OlusturSistem();
        var id = Kuyumcu.Api.Common.Kimlik.Ulid();
        var simdi = DateTime.UtcNow;
        db.Faturalar.Add(new Fatura
        {
            Id = id, TenantId = k.TenantId, CariId = k.CariId, Tur = "SATIS", Durum = "KESILDI", FaturaNo = $"FTR-T-{id[^6..]}",
            Toplam = 1200m, KdvTutar = 200m, AraToplam = 1000m, KdvOran = 0.20m, Tarih = simdi, Olusturuldu = simdi.AddSeconds(1), Guncellendi = simdi,
            AliciUnvan = "Hatice Demirkol", AliciVergiNo = aliciNo,
        });
        db.SaveChanges();
        return id;
    }

    private static async Task Tur(WebApplicationFactory<Program> o, string tenantId, bool zamaniGetir = false)
    {
        if (zamaniGetir)
        {
            await using var s0 = o.Services.GetRequiredService<SistemKapsami>().Sistem();
            var db0 = s0.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
            foreach (var g in db0.EBelgeGonderimleri.Where(x => x.TenantId == tenantId)) g.SonrakiDeneme = DateTime.UtcNow.AddMinutes(-1);
            await db0.SaveChangesAsync();
        }
        await using var s = o.Services.GetRequiredService<SistemKapsami>().Kiraci(tenantId);
        await s.ServiceProvider.GetRequiredService<EBelgeServisi>().Tur(tenantId, default);
    }

    private EBelgeGonderimi Gonderim(string faturaId)
    {
        using var db = f.Vt.OlusturSistem();
        return db.EBelgeGonderimleri.AsNoTracking().Single(g => g.FaturaId == faturaId);
    }

    private static async Task Bagla(HttpClient s, string? seri = "MHK")
    {
        var p = await s.PutAsJsonAsync("/api/v1/ebelge/ayar", new { saglayici = "NILVERA", ortam = "TEST", anahtar = Anahtar, seriOnEki = seri, etkin = true });
        var m = await p.Content.ReadAsStringAsync();
        Assert.True(p.IsSuccessStatusCode, m);
        Assert.DoesNotContain(Anahtar, m);
        Assert.True(JsonDocument.Parse(m).RootElement.GetProperty("basarili").GetBoolean(), m);
    }

    [Fact]
    public async Task Kesilen_fatura_kendiliginden_eArsiv_gider_GIB_kabulu_okunur_8_gun_icinde_iptal_entegratorden()
    {
        var (o, n) = Kur();
        var k = Kiraci("arsiv");
        var s = Istemci(o, k, "SAHIP");
        var oncesi = FaturaKes(k);   // bağlantıdan ÖNCE kesilen fatura gönderilmez
        await Task.Delay(1100);
        await Bagla(s);
        Assert.Contains(n.Istekler, i => i.Yol == $"/general/GlobalCompany/Check/TaxNumber/{IsletmeVkn}" && i.Yetki == $"Bearer {Anahtar}");

        var fid = FaturaKes(k);
        await Tur(o, k.TenantId);
        var g = Gonderim(fid);
        Assert.Equal((EBelgeDurumlari.Gonderildi, EBelgeSenaryolari.EArsiv, "MHK2026000000001"), (g.Durum, g.Senaryo, g.BelgeNo));
        using (var db = f.Vt.OlusturSistem()) Assert.False(db.EBelgeGonderimleri.Any(x => x.FaturaId == oncesi));
        var gonder = Assert.Single(n.Istekler, i => i.Yol == "/earchive/Send/Model");
        Assert.Contains(g.Ettn.ToString(), gonder.Govde);
        Assert.Contains("\"InvoiceSerieOrNumber\":\"MHK\"", gonder.Govde);
        Assert.Contains($"\"TaxNumber\":\"{AliciTc}\"", gonder.Govde);

        await Tur(o, k.TenantId, zamaniGetir: true);
        Assert.Equal(EBelgeDurumlari.Kabul, Gonderim(fid).Durum);
        // Olay izi (append-only): kuyruk → gönderildi → GİB kabul; XML yasal aslı indirilebilir.
        var olaylar = await s.GetFromJsonAsync<JsonElement>($"/api/v1/ebelge/faturalar/{fid}/olaylar");
        var yeniDurumlar = olaylar.EnumerateArray().Select(x => x.GetProperty("yeniDurum").GetString()).ToList();
        Assert.Equal(["KABUL", "GONDERILDI", "KUYRUKTA"], yeniDurumlar);   // yeniden eskiye
        var xml = await s.GetAsync($"/api/v1/ebelge/faturalar/{fid}/xml");
        Assert.True(xml.IsSuccessStatusCode, await xml.Content.ReadAsStringAsync());
        Assert.StartsWith("<?xml", await xml.Content.ReadAsStringAsync());
        var kutu = await s.GetFromJsonAsync<JsonElement>("/api/v1/ebelge/kutu");
        Assert.Equal(1, kutu.GetProperty("sayaclar").GetProperty("KABUL").GetInt32());
        Assert.Single(kutu.GetProperty("satirlar").EnumerateArray());
        var durum = await s.GetFromJsonAsync<JsonElement>($"/api/v1/ebelge/faturalar/{fid}");
        Assert.True(durum.GetProperty("iptalEdilebilir").GetBoolean());

        var ip = await s.PatchAsJsonAsync($"/api/v1/faturalar/{fid}/durum", new { durum = "IPTAL" });
        Assert.True(ip.IsSuccessStatusCode, await ip.Content.ReadAsStringAsync());
        Assert.Contains(n.Istekler, i => i.Metot == "PUT" && i.Yol == $"/earchive/Invoices/{g.Ettn}/Cancel");
        Assert.Equal(EBelgeDurumlari.Iptal, Gonderim(fid).Durum);

        using var db2 = f.Vt.OlusturSistem();
        var ayar = db2.EBelgeAyarlari.AsNoTracking().Single(a => a.TenantId == k.TenantId);
        Assert.DoesNotContain(Anahtar, ayar.AnahtarSifreli);
        Assert.DoesNotContain(Anahtar, await s.GetStringAsync("/api/v1/ebelge/ayar"));
    }

    [Fact]
    public async Task Kayitli_aliciya_eFatura_etiketle_gider_iptal_reddedilir_fatura_KESILDI_kalir_8_gun_gecince_eArsiv_iptali_de_reddedilir()
    {
        var (o, n) = Kur();
        var k = Kiraci("efatura");
        var s = Istemci(o, k, "SAHIP");
        await Bagla(s);
        n.Kayitli = true;
        var fid = FaturaKes(k, aliciNo: AliciVkn);
        await Tur(o, k.TenantId);
        Assert.Equal(EBelgeSenaryolari.EFatura, Gonderim(fid).Senaryo);
        Assert.Contains(n.Istekler, i => i.Yol == "/einvoice/Send/Model" && i.Govde.Contains("\"CustomerAlias\":\"urn:mail:defaultpk@alici.com\""));

        var ip = await s.PatchAsJsonAsync($"/api/v1/faturalar/{fid}/durum", new { durum = "IPTAL" });
        Assert.Equal(HttpStatusCode.Conflict, ip.StatusCode);
        Assert.Contains("iade faturası", (await ip.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
        using (var db = f.Vt.OlusturSistem()) Assert.Equal("KESILDI", db.Faturalar.AsNoTracking().Single(x => x.Id == fid).Durum);

        // e-Arşiv, 9 gün önce gönderilmiş: süre geçti → iptal yok.
        n.Kayitli = false;
        var f2 = FaturaKes(k);
        await Tur(o, k.TenantId);
        using (var db = f.Vt.OlusturSistem())
        {
            var g2 = db.EBelgeGonderimleri.Single(x => x.FaturaId == f2);
            g2.Gonderildi = DateTime.UtcNow.AddDays(-9);
            db.SaveChanges();
        }
        var ip2 = await s.PatchAsJsonAsync($"/api/v1/faturalar/{f2}/durum", new { durum = "IPTAL" });
        Assert.Equal(HttpStatusCode.Conflict, ip2.StatusCode);
        Assert.Contains("8 günlük", (await ip2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
        Assert.DoesNotContain(n.Istekler, i => i.Yol.EndsWith("/Cancel"));
    }

    [Fact]
    public async Task Gecici_hata_yeniden_denenir_kalici_hata_sade_iletiyle_durur_yeniden_dene_gonderir_kimliksiz_alici_gonderilmez()
    {
        var (o, n) = Kur();
        var k = Kiraci("hata");
        var s = Istemci(o, k, "SAHIP");
        await Bagla(s);

        n.GonderYaniti = () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        var fid = FaturaKes(k);
        await Tur(o, k.TenantId);
        var g = Gonderim(fid);
        Assert.Equal((EBelgeDurumlari.Kuyrukta, 1), (g.Durum, g.DenemeSayisi));
        Assert.True(g.SonrakiDeneme > DateTime.UtcNow.AddSeconds(30));
        await Tur(o, k.TenantId);   // zamanı gelmedi → istek yok
        Assert.Single(n.Istekler, i => i.Yol.EndsWith("/Send/Model"));

        n.GonderYaniti = () => Json("""[{"Code":"422","Description":"Alıcı adresi zorunludur"}]""", HttpStatusCode.UnprocessableEntity);
        await Tur(o, k.TenantId, zamaniGetir: true);
        g = Gonderim(fid);
        Assert.Equal(EBelgeDurumlari.Hata, g.Durum);
        Assert.Equal("Nilvera: Alıcı adresi zorunludur", g.SonHata);

        n.GonderYaniti = null;
        var y = await s.PostAsync($"/api/v1/ebelge/faturalar/{fid}/yeniden-dene", null);
        var yj = await y.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(y.IsSuccessStatusCode, yj.ToString());
        Assert.Equal(EBelgeDurumlari.Gonderildi, yj.GetProperty("durum").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await s.PostAsync($"/api/v1/ebelge/faturalar/{fid}/yeniden-dene", null)).StatusCode);

        var kimliksiz = FaturaKes(k, aliciNo: null);
        var once = n.Istekler.Count(i => i.Yol.EndsWith("/Send/Model"));
        await Tur(o, k.TenantId);
        var gk = Gonderim(kimliksiz);
        Assert.Equal(EBelgeDurumlari.Hata, gk.Durum);
        Assert.Contains("Müşterinin TC", gk.SonHata);
        Assert.Equal(once, n.Istekler.Count(i => i.Yol.EndsWith("/Send/Model")));
    }

    [Fact]
    public async Task Yanlis_anahtar_sade_hata_mudur_kaydedemez_tanimsiz_entegrator_400_seri_on_eki_kurali()
    {
        var (o, _) = Kur();
        var k = Kiraci("yetki");
        var s = Istemci(o, k, "SAHIP");
        var mudur = await Istemci(o, k, "MUDUR").PutAsJsonAsync("/api/v1/ebelge/ayar", new { saglayici = "NILVERA", anahtar = "x" });
        Assert.Equal(HttpStatusCode.Forbidden, mudur.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PutAsJsonAsync("/api/v1/ebelge/ayar", new { saglayici = "YOK", anahtar = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PutAsJsonAsync("/api/v1/ebelge/ayar", new { saglayici = "NILVERA", anahtar = "x", seriOnEki = "M1" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PutAsJsonAsync("/api/v1/ebelge/ayar", new { saglayici = "NILVERA" })).StatusCode);

        var o2 = f.Ortam.WithWebHostBuilder(b => b.ConfigureServices(sv =>
            sv.AddHttpClient(EBelgeServisi.HttpAdi).ConfigurePrimaryHttpMessageHandler(() => new YetkisizIsleyici())));
        var p = await Istemci(o2, k, "SAHIP").PutAsJsonAsync("/api/v1/ebelge/ayar", new { saglayici = "NILVERA", anahtar = "yanlis" });
        var j = await p.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(j.GetProperty("basarili").GetBoolean());
        Assert.Equal("Nilvera API anahtarını kabul etmedi (yetkisi yok ya da süresi dolmuş).", j.GetProperty("hata").GetString());
    }

    private sealed class YetkisizIsleyici : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken c) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
    }
}
