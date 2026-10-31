using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Modules.Banka;
using Kuyumcu.Api.Modules.Banka.Baglayicilar;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D2-05b (19.10) — banka bağlantısı uçtan uca: SAHTE BANKA SUNUCUSU (HttpMessageHandler) Garanti'nin
// token + hareket uçlarını taklit eder. Kaydet → hemen dene → hareket yazılır; sır ne yanıtta ne
// veritabanında düz; banka 401 → sade hata; yeniden kayıtta boş şifre eskisini korur; kaldır; yetki.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class D2BankaBaglantiHttpTestleri(HttpFikstur f)
{
    private const string Sir = "GIZLI-sir-9f3a";

    /// <summary>Sahte banka: istekleri kaydeder, yanıtı test belirler.</summary>
    private sealed class SahteBanka : HttpMessageHandler
    {
        public List<(string Yol, string Govde)> Istekler { get; } = [];
        public Func<string, HttpResponseMessage> Yanit { get; set; } = _ => new(HttpStatusCode.OK);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken c)
        {
            var govde = r.Content is null ? "" : await r.Content.ReadAsStringAsync(c);
            lock (Istekler) Istekler.Add((r.RequestUri!.AbsolutePath, govde));
            return Yanit(r.RequestUri!.AbsolutePath);
        }
    }

    private static HttpResponseMessage Json(string s, HttpStatusCode d = HttpStatusCode.OK) =>
        new(d) { Content = new StringContent(s, System.Text.Encoding.UTF8, "application/json") };

    private (WebApplicationFactory<Program> Ortam, SahteBanka Banka) Kur()
    {
        var banka = new SahteBanka();
        var ortam = f.Ortam.WithWebHostBuilder(b => b.ConfigureServices(s =>
            s.AddHttpClient(BankaBaglantiCekici.HttpAdi).ConfigurePrimaryHttpMessageHandler(() => banka)));
        return (ortam, banka);
    }

    private static HttpClient Istemci(WebApplicationFactory<Program> o, Kiraci k, string rol)
    {
        var token = new TokenServisi(new JwtAyarlar { Secret = HttpOrtami.TestSirri, AccessDakika = 15, RefreshGun = 7 })
            .AccessUret(k.KullaniciId, k.TenantId, k.SubeId, rol, false);
        var c = o.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private static string GarantiYaniti(string iban, string tc) => $$"""
        { "result": { "returnCode": 200, "reasonCode": 1, "messageText": "Başarılı" },
          "transactions": [ { "IBAN": "{{iban}}", "activityDate": "2026-10-19", "txnCreditDebitIndicator": "A", "amount": 4200.5,
            "explanation": "bilezik", "corrTCKN": "{{tc}}", "transactionReferenceId": "r1", "transactionInstanceId": "2026-10-19T10:00:00.000001",
            "enrichmentInformation": [ { "enrichmentCode": "MUS", "enrichmentValue": { "corrNameSurnameText": "Hatice Demirkol" } } ] } ] }
        """;

    [Fact]
    public async Task Kaydet_hemen_dener_hareket_yazilir_sir_hic_donmez_ve_veritabaninda_duz_degil_ikinci_deneme_tekrar_yazmaz()
    {
        var (o, banka) = Kur();
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D2BB-akis");
        var s = Istemci(o, k, "SAHIP");
        var iban = TestIban.Uret();
        var c = await s.PostAsJsonAsync("/api/v1/bankalar", new { bankaAdi = "Garanti", hesapAdi = "Vadesiz", iban, paraBirimi = "TRY" });
        var hesap = (await c.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
        banka.Yanit = yol => yol.EndsWith("/token") ? Json("""{"access_token":"tk1"}""") : Json(GarantiYaniti(iban, "10000000146"));

        var katalog = await s.GetFromJsonAsync<JsonElement>("/api/v1/bankalar/baglayicilar");
        Assert.Equal(8, katalog.GetProperty("bankalar").GetArrayLength());

        var p = await s.PutAsJsonAsync($"/api/v1/bankalar/{hesap}/baglanti",
            new { kod = "GARANTI", alanlar = new Dictionary<string, string> { ["consentId"] = "cons-1", ["clientId"] = "cid-1", ["clientSecret"] = Sir } });
        var metin = await p.Content.ReadAsStringAsync();
        Assert.True(p.IsSuccessStatusCode, metin);
        Assert.DoesNotContain(Sir, metin);
        var y = JsonDocument.Parse(metin).RootElement;
        Assert.True(y.GetProperty("deneme").GetProperty("basarili").GetBoolean(), metin);
        Assert.Equal("Garanti BBVA", y.GetProperty("baglanti").GetProperty("bankaAdi").GetString());

        // Bankaya giden istek: token gövdesinde sır, hareket isteğinde onay no + IBAN.
        Assert.Contains(banka.Istekler, i => i.Yol.EndsWith("/token") && i.Govde.Contains("client_secret=" + Uri.EscapeDataString(Sir)));
        Assert.Contains(banka.Istekler, i => i.Yol.EndsWith("/gettransactions") && i.Govde.Contains("\"consentId\":\"cons-1\"") && i.Govde.Contains(iban));

        // Kaydetme denemesi hareketi yazdı; "şimdi dene" aynı hareketi İKİNCİ kez yazmaz.
        Assert.Equal(1, y.GetProperty("deneme").GetProperty("hareketSayisi").GetInt32());
        var d2 = await (await s.PostAsync($"/api/v1/bankalar/{hesap}/baglanti/dene", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, d2.GetProperty("yeni").GetInt32());

        using (var db = f.Vt.OlusturSistem())
        {
            var bag = await db.BankaBaglantilari.AsNoTracking().SingleAsync(b => b.TenantId == k.TenantId);
            Assert.DoesNotContain(Sir, bag.KimlikSifreli);
            Assert.DoesNotContain("cons-1", bag.KimlikSifreli);
            Assert.StartsWith("v1:", bag.KimlikSifreli);
            Assert.NotNull(bag.SonBasari);
            var h = await db.BankaHareketleri.AsNoTracking().SingleAsync(x => x.TenantId == k.TenantId);
            Assert.Equal(4200.5m, h.Tutar);
            Assert.StartsWith("GARANTI-", h.SaglayiciReferans);
            Assert.Equal("GARANTI", (await db.BankaHesaplari.AsNoTracking().SingleAsync(x => x.Id == hesap)).SaglayiciKodu);
            // Bilinmeyen gönderen TC ile yeni müşteri olarak açıldı (M7a yolu).
            Assert.True(await db.Cariler.AnyAsync(x => x.TenantId == k.TenantId && x.Ad == "Hatice Demirkol"));
        }
        var get = await s.GetStringAsync($"/api/v1/bankalar/{hesap}/baglanti");
        Assert.DoesNotContain(Sir, get);
        Assert.DoesNotContain("cons-1", get);   // açık alan da yalnız maskeli özetle döner
    }

    // ── Kurucu 31.10: bankadaki bakiye + gelen/giden ──
    private static string GarantiGelenGiden(string iban) => $$"""
        { "result": { "returnCode": 200, "reasonCode": 1, "messageText": "Başarılı" },
          "transactions": [
            { "IBAN": "{{iban}}", "activityDate": "2026-10-30", "txnCreditDebitIndicator": "A", "amount": 1500,
              "explanation": "havale", "transactionReferenceId": "g1", "transactionInstanceId": "2026-10-30T10:00:00.000001" },
            { "IBAN": "{{iban}}", "activityDate": "2026-10-30", "txnCreditDebitIndicator": "B", "amount": 300,
              "explanation": "eft", "transactionReferenceId": "c1", "transactionInstanceId": "2026-10-30T11:00:00.000001" } ] }
        """;

    private static string GarantiBakiye(string iban) => $$"""
        { "result": { "returnCode": 200, "reasonCode": 1, "messageText": "Başarılı" },
          "accounts": [ { "balances": [ { "type": "Balance", "Amount": "8200.00" }, { "type": "AvailableBalance", "Amount": "8000.00" } ],
            "IBAN": "{{iban}}", "currencyCode": "TL " } ] }
        """;

    private async Task<(HttpClient S, string Hesap, string Iban, Kiraci K)> GarantiHesabi(WebApplicationFactory<Program> o, string ad)
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, ad);
        var s = Istemci(o, k, "SAHIP");
        var iban = TestIban.Uret();
        var c = await s.PostAsJsonAsync("/api/v1/bankalar", new { bankaAdi = "Garanti", hesapAdi = "Vadesiz", iban, paraBirimi = "TRY" });
        return (s, (await c.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!, iban, k);
    }

    private static Task<HttpResponseMessage> GarantiKaydet(HttpClient s, string hesap) =>
        s.PutAsJsonAsync($"/api/v1/bankalar/{hesap}/baglanti",
            new { kod = "GARANTI", alanlar = new Dictionary<string, string> { ["consentId"] = "cons-1", ["clientId"] = "cid-1", ["clientSecret"] = Sir } });

    [Fact]
    public async Task Garanti_bakiye_hesaba_yazilir_listede_doner_gelen_ve_giden_hareket_yazilir_her_istege_yeni_token()
    {
        var (o, banka) = Kur();
        var (s, hesap, iban, k) = await GarantiHesabi(o, "D2BB-bakiye");
        banka.Yanit = yol => yol.EndsWith("/token") ? Json("""{"access_token":"tk1"}""")
            : yol.EndsWith("/getaccountinformation") ? Json(GarantiBakiye(iban)) : Json(GarantiGelenGiden(iban));

        var p = await GarantiKaydet(s, hesap);
        Assert.True(p.IsSuccessStatusCode, await p.Content.ReadAsStringAsync());
        Assert.Contains(banka.Istekler, i => i.Yol == GarantiBaglayicisi.BakiyeYolu && i.Govde.Contains("\"consentId\":\"cons-1\"") && i.Govde.Contains(iban));
        // Token tek kullanımlık: hareket + bakiye = 2 istek → 2 token.
        Assert.Equal(banka.Istekler.Count(i => !i.Yol.EndsWith("/token")), banka.Istekler.Count(i => i.Yol.EndsWith("/token")));

        var liste = await s.GetFromJsonAsync<JsonElement>("/api/v1/bankalar");
        var h = liste.GetProperty("hesaplar").EnumerateArray().Single(x => x.GetProperty("id").GetString() == hesap);
        Assert.Equal(8200m, h.GetProperty("bankaBakiye").GetDecimal());
        Assert.Equal(8000m, h.GetProperty("bankaKullanilabilir").GetDecimal());
        Assert.NotEqual(JsonValueKind.Null, h.GetProperty("bankaBakiyeZamani").ValueKind);

        using var db = f.Vt.OlusturSistem();
        var hareketler = await db.BankaHareketleri.AsNoTracking().Where(x => x.TenantId == k.TenantId).ToListAsync();
        Assert.Contains(hareketler, x => x.Yon == "GELEN" && x.Tutar == 1500m);
        Assert.Contains(hareketler, x => x.Yon == "GIDEN" && x.Tutar == 300m);
    }

    [Fact]
    public async Task Garanti_bakiye_ucu_hata_verirse_hareketler_yine_yazilir_baglanti_basarili_bakiye_bos_kalir()
    {
        var (o, banka) = Kur();
        var (s, hesap, iban, k) = await GarantiHesabi(o, "D2BB-bakiyehata");
        banka.Yanit = yol => yol.EndsWith("/token") ? Json("""{"access_token":"tk1"}""")
            : yol.Contains("/accountinformation/account/") ? Json("""{ "result": { "returnCode": 400, "reasonCode": 13, "messageText": "Hesap bilgisi ile İban bilgisi uyumsuzdur." } }""", HttpStatusCode.BadRequest)
            : Json(GarantiGelenGiden(iban));

        var y = await (await GarantiKaydet(s, hesap)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(y.GetProperty("deneme").GetProperty("basarili").GetBoolean(), y.ToString());
        Assert.Equal(2, y.GetProperty("deneme").GetProperty("hareketSayisi").GetInt32());
        using var db = f.Vt.OlusturSistem();
        var hs = await db.BankaHesaplari.AsNoTracking().SingleAsync(x => x.Id == hesap);
        Assert.Null(hs.BankaBakiye);
        Assert.Null(hs.BankaBakiyeZamani);
        Assert.Null((await db.BankaBaglantilari.AsNoTracking().SingleAsync(b => b.TenantId == k.TenantId)).SonHata);
    }

    [Fact]
    public async Task Garanti_belgedeki_bakiye_yolu_404_ise_swagger_yolu_denenir()
    {
        var (o, banka) = Kur();
        var (s, hesap, iban, _) = await GarantiHesabi(o, "D2BB-bakiye404");
        banka.Yanit = yol => yol.EndsWith("/token") ? Json("""{"access_token":"tk1"}""")
            : yol == GarantiBaglayicisi.BakiyeYolu ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : yol == GarantiBaglayicisi.BakiyeYoluSwagger ? Json(GarantiBakiye(iban)) : Json(GarantiGelenGiden(iban));

        Assert.True((await GarantiKaydet(s, hesap)).IsSuccessStatusCode);
        Assert.Contains(banka.Istekler, i => i.Yol == GarantiBaglayicisi.BakiyeYoluSwagger);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(8200m, (await db.BankaHesaplari.AsNoTracking().SingleAsync(x => x.Id == hesap)).BankaBakiye);
    }

    [Fact]
    public async Task Banka_401_sade_hata_yazar_yeniden_kayitta_bos_sifre_eskisini_korur_kaldir_siler()
    {
        var (o, banka) = Kur();
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D2BB-hata");
        var s = Istemci(o, k, "SAHIP");
        var iban = TestIban.Uret();
        var c = await s.PostAsJsonAsync("/api/v1/bankalar", new { bankaAdi = "QNB", hesapAdi = "Vadesiz", iban, paraBirimi = "TRY" });
        var hesap = (await c.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
        banka.Yanit = _ => new HttpResponseMessage(HttpStatusCode.Unauthorized);

        var p = await (await s.PutAsJsonAsync($"/api/v1/bankalar/{hesap}/baglanti",
            new { kod = "QNB", alanlar = new Dictionary<string, string> { ["kullanici"] = "kul", ["sifre"] = Sir, ["hesapNo"] = "123" } }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(p.GetProperty("deneme").GetProperty("basarili").GetBoolean());
        Assert.Equal("QNB servis kullanıcı adını ya da şifresini kabul etmedi.", p.GetProperty("baglanti").GetProperty("sonHata").GetString());

        // Şifre boş bırakılarak yeniden kayıt: eski şifre korunur, bankaya yine gider.
        banka.Istekler.Clear();
        banka.Yanit = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(
            "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"><soapenv:Body><r><return><errorCode>0</errorCode></return></r></soapenv:Body></soapenv:Envelope>") };
        var p2 = await (await s.PutAsJsonAsync($"/api/v1/bankalar/{hesap}/baglanti",
            new { kod = "QNB", alanlar = new Dictionary<string, string> { ["kullanici"] = "kul2", ["hesapNo"] = "123" } }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(p2.GetProperty("deneme").GetProperty("basarili").GetBoolean(), p2.ToString());
        Assert.Contains(banka.Istekler, i => i.Govde.Contains($"<password>{Sir}</password>") && i.Govde.Contains("<userName>kul2</userName>"));

        // Başka bankaya geçişte şifre taşınmaz (zorunlu alan eksik → 400).
        var gecis = await s.PutAsJsonAsync($"/api/v1/bankalar/{hesap}/baglanti",
            new { kod = "KUVEYT", alanlar = new Dictionary<string, string> { ["kullanici"] = "kul", ["hesapNo"] = "1-1" } });
        Assert.Equal(HttpStatusCode.BadRequest, gecis.StatusCode);

        var sil = await s.DeleteAsync($"/api/v1/bankalar/{hesap}/baglanti");
        Assert.True(sil.IsSuccessStatusCode);
        using var db2 = f.Vt.OlusturSistem();
        Assert.False(await db2.BankaBaglantilari.AnyAsync(b => b.TenantId == k.TenantId));
        Assert.Null((await db2.BankaHesaplari.AsNoTracking().SingleAsync(x => x.Id == hesap)).SaglayiciKodu);
    }

    [Fact]
    public async Task Mudur_kaydedemez_muhasebe_goremez_baska_kiracinin_hesabi_404_tanimsiz_banka_400()
    {
        var (o, _) = Kur();
        Kiraci k, baska;
        using (var db = f.Vt.OlusturSistem()) { k = TestKurulum.KiraciKur(db, "D2BB-yetki"); baska = TestKurulum.KiraciKur(db, "D2BB-baska"); }
        var s = Istemci(o, k, "SAHIP");
        var c = await s.PostAsJsonAsync("/api/v1/bankalar", new { bankaAdi = "Akbank", hesapAdi = "Vadesiz", iban = TestIban.Uret(), paraBirimi = "TRY" });
        var hesap = (await c.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
        var govde = new { kod = "AKBANK", alanlar = new Dictionary<string, string> { ["kullanici"] = "a", ["sifre"] = "b", ["urf"] = "U1", ["hesapNo"] = "1" } };

        Assert.Equal(HttpStatusCode.Forbidden, (await Istemci(o, k, "MUDUR").PutAsJsonAsync($"/api/v1/bankalar/{hesap}/baglanti", govde)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Istemci(o, k, "MUHASEBE").GetAsync($"/api/v1/bankalar/{hesap}/baglanti")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Istemci(o, baska, "SAHIP").PutAsJsonAsync($"/api/v1/bankalar/{hesap}/baglanti", govde)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PutAsJsonAsync($"/api/v1/bankalar/{hesap}/baglanti", new { kod = "YOKBANK", alanlar = new Dictionary<string, string>() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PutAsJsonAsync($"/api/v1/bankalar/{hesap}/baglanti",
            new { kod = "AKBANK", alanlar = new Dictionary<string, string> { ["kullanici"] = "a", ["sifre"] = "b", ["urf"] = "U1", ["hesapNo"] = new string('9', 501) } })).StatusCode);
    }
}
