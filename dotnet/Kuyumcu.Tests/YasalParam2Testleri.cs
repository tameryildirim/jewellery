using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MMasak = Kuyumcu.Api.Modules.Masak;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-PARAM-2 — (1) eşik dönemi GÜN DEĞİL AN sınırlı: bugün öğlen değişen eşik,
// o sabahki işlemi ESKİ eşikle değerlendirir (risk). (2) Bugünden geçerli YANLIŞ giriş
// AYNI GÜN düzeltilir: yalnız giren operatör (başkası 403), gerekçe ≥ 20, iki adımlı onay,
// denetim izi; yanlış değerin geçerli olduğu aralık (geçmiş) yeniden yazılmaz.
// ⚠ Paylaşılan test veritabanına yazan HTTP testi MASAK_EK_BELGE_ESIGI_TL anahtarını kullanır
//   (Masak370Testleri kimlik eşiği satırlarını sayar) ve finally'de geri yükler.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class YasalParam2SafTestleri
{
    private const string A = YasalAnahtar.MasakKimlikEsigiTl;

    private static YasalParametreSatiri Satir(string id, decimal deger, DateOnly bas, DateOnly? bit, DateTime? ani = null) =>
        new(id, A, deger, null, YasalBirim.Tl, bas, bit, "test", "https://test", YasalTeyit.Dogrulandi, "test")
        { BaslangicAni = ani };

    private static decimal AndakiDeger(YasalParametreServisi s, DateTime an) =>
        Assert.Single(s.DonemlerAn(A, an, an)).Deger;

    // Sabit an: 08.10.2026 13:00 yerel (10:00 UTC); değişiklik 12:00 yerel (09:00 UTC).
    private static readonly DateTime Simdi = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Bugun = new(2026, 10, 8);
    private static readonly DateTime Degisim = new(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc);

    private static YasalParametreServisi Servis(params YasalParametreSatiri[] satirlar) =>
        YasalParametreServisi.Yukleyicili(() => satirlar, new SahteSaat(new DateTimeOffset(Simdi)));

    [Fact]
    public void Ayni_gun_esik_degisimi_AN_siniri_1sn_once_ESKI_1sn_sonra_YENI_gun_basi_ESKI()
    {
        var s = Servis(
            Satir("p", 370_000m, new DateOnly(2026, 10, 7), Bugun),
            Satir("w", 150_000m, Bugun, null, Degisim));

        Assert.Equal(370_000m, AndakiDeger(s, Degisim.AddSeconds(-1)));          // sabah işlemi: eski eşik
        Assert.Equal(370_000m, AndakiDeger(s, Zaman.YerelGunBasiUtc(Bugun)));     // gün başı: eski eşik
        Assert.Equal(150_000m, AndakiDeger(s, Degisim));                          // değişiklik anı DAHİL
        Assert.Equal(150_000m, AndakiDeger(s, Degisim.AddSeconds(1)));
        // Gün içini kesen pencere iki dönem verir; sınır tam değişiklik anı.
        var d = s.DonemlerAn(A, Zaman.YerelGunBasiUtc(Bugun), Simdi);
        Assert.Equal(2, d.Count);
        Assert.Equal(Degisim, d[0].BitUtc);
        Assert.Equal(Degisim, d[1].BasUtc);
    }

    [Fact]
    public void Ayni_gun_duzeltme_yanlis_deger_YALNIZ_iki_an_arasinda_sonrasi_dogru_oncesi_onceki()
    {
        var duzeltme = Degisim.AddMinutes(30);
        var s = Servis(
            Satir("p", 370_000m, new DateOnly(2026, 10, 7), Bugun),
            Satir("w", 150_000m, Bugun, Bugun, Degisim),          // yanlış: bitişi bugün (kapandı)
            Satir("c", 250_000m, Bugun, null, duzeltme));         // doğru: düzeltme anından

        Assert.Equal(370_000m, AndakiDeger(s, Degisim.AddSeconds(-1)));
        Assert.Equal(150_000m, AndakiDeger(s, Degisim.AddSeconds(1)));
        Assert.Equal(150_000m, AndakiDeger(s, duzeltme.AddSeconds(-1)));   // geçmiş: yanlış değerle KALIR
        Assert.Equal(250_000m, AndakiDeger(s, duzeltme.AddSeconds(1)));
        // Gün düzeyinde (kapı) bugün = düzeltilmiş değer; kapanmış yanlış satır bugün geçerli DEĞİL.
        Assert.Equal(250_000m, s.Sayi(A, Bugun));
        Assert.True(s.AyniGunDuzeltilebilir(s.SatirBul(A, Bugun)!));
    }

    [Fact]
    public void Anisiz_tohum_satiri_GUN_BASI_siniri_korunur_bosluk_503()
    {
        var degisimGunu = new DateOnly(2026, 10, 7);
        var s = Servis(
            Satir("p", 185_000m, new DateOnly(2000, 1, 1), degisimGunu),
            Satir("n", 370_000m, degisimGunu, null));
        var gunBasi = Zaman.YerelGunBasiUtc(degisimGunu);
        Assert.Equal(185_000m, AndakiDeger(s, gunBasi.AddSeconds(-1)));
        Assert.Equal(370_000m, AndakiDeger(s, gunBasi));

        var bosluklu = Servis(
            Satir("p", 185_000m, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1)),
            Satir("n", 370_000m, new DateOnly(2026, 3, 1), null));
        var ex = Assert.Throws<YasalParametreYokException>(() =>
            bosluklu.DonemlerAn(A, Zaman.YerelGunBasiUtc(new DateOnly(2026, 1, 15)), Simdi));
        Assert.Equal(503, ex.StatusCode);
    }

    [Fact]
    public void Ayni_gun_duzeltilebilir_yalniz_bugun_anli_ve_yururlukte_satir()
    {
        var s = Servis();
        Assert.True(s.AyniGunDuzeltilebilir(Satir("w", 1m, Bugun, null, Degisim)));
        Assert.False(s.AyniGunDuzeltilebilir(Satir("t", 1m, Bugun, null)));                      // anısız (tohum)
        Assert.False(s.AyniGunDuzeltilebilir(Satir("k", 1m, Bugun, Bugun, Degisim)));            // kapanmış
        Assert.False(s.AyniGunDuzeltilebilir(Satir("d", 1m, Bugun.AddDays(-1), null, Degisim.AddDays(-1)))); // dün
    }
}

[Collection("veritabani")]
public sealed class YasalParam2RiskTestleri(TestVeritabani vt)
{
    private static YasalParametreSatiri Satir(string id, decimal deger, DateOnly bas, DateOnly? bit, DateTime? ani = null) =>
        new(id, YasalAnahtar.MasakKimlikEsigiTl, deger, null, YasalBirim.Tl, bas, bit,
            "test", "https://test", YasalTeyit.Dogrulandi, "test") { BaslangicAni = ani };

    [Fact]
    public async Task Risk_ogle_esik_dususu_SABAH_islemi_ESKI_esikle_1sn_sonrasi_YENI_esikle_degerlendirilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASALPARAM2-TEST-risk");
        using var db = vt.Olustur(k);

        var simdi = DateTime.UtcNow;
        var bugun = YasalParametreServisi.IslemGunu(simdi);
        var gunBasi = Zaman.YerelGunBasiUtc(bugun);
        // Değişiklik anı: bugünün başı ile şimdi arasının ortası (her saatte bugün ve geçmişte kalır).
        var degisim = gunBasi.AddTicks((simdi - gunBasi).Ticks / 2);
        var yasal = YasalParametreServisi.Yukleyicili(() =>
        [
            Satir("p", 370_000m, new DateOnly(2000, 1, 1), bugun),
            Satir("w", 150_000m, bugun, null, degisim),
        ]);

        void Ekle(DateTime tarih) => HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(200_000m), new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = "SATIS", Tarih = tarih, TlTutari = 200_000m, HasGrami = 0m,
            ParaBirimi = "TRY", OdemeTuru = "NAKIT",
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            Olusturuldu = simdi,
        });
        // 200.000: eski eşik 370.000'in ALTINDA, yeni eşik 150.000'in ÜSTÜNDE.
        Ekle(degisim.AddSeconds(-1));   // sabah işlemi → eski eşikle: kimlik GEREKMEZDİ
        Ekle(degisim.AddSeconds(1));    // değişiklikten sonra → yeni eşikle: kimlik gerekir
        await db.SaveChangesAsync();

        var servis = new MMasak.RiskServisi(db, new MMasak.MasakRiskAyarlari(), new MMasak.MasakEsikleri(),
            new DenetimServisi(db, new HttpContextAccessor()), yasal);
        var kok = JsonDocument.Parse(JsonSerializer.Serialize(await servis.Hesapla(k.CariId, k.Islem))).RootElement;

        // Gün çözünürlüğünde (eski davranış) ikisi de 150.000'le değerlendirilir → 2.
        Assert.Equal(1, kok.GetProperty("girdiOzeti").GetProperty("kimlikEksik").GetInt32());
    }
}

[Collection("http")]
public sealed class YasalParam2HttpTestleri(HttpFikstur f)
{
    private const string Uc = "/api/v1/sistem/yasal-parametreler";
    private const string EB = YasalAnahtar.MasakEkBelgeEsigiTl;
    private DateOnly Bugun => YasalParametreServisi.IslemGunu(DateTime.UtcNow);
    private static string G(DateOnly g) => g.ToString("yyyy-MM-dd");

    private static async Task<JsonElement> Govde(HttpResponseMessage c) =>
        await c.Content.ReadFromJsonAsync<JsonElement>();

    private Dictionary<string, object?> YeniGovde(decimal deger) => new()
    {
        ["anahtar"] = EB, ["degerSayi"] = deger, ["baslangic"] = G(Bugun),
        ["kaynakUrl"] = "https://www.resmigazete.gov.tr/test", ["dayanak"] = "YASAL-PARAM-2 test satırı (silinecek)",
        ["gerekce"] = "YASAL-PARAM-2 HTTP testi bugünden geçerli değişiklik",
        ["resmiGazeteTarihi"] = G(Bugun), ["resmiGazeteSayisi"] = "33400",
    };

    private static Dictionary<string, object?> DuzeltGovde(decimal? deger, string? gerekce, string? kod = null) => new()
    {
        ["degerSayi"] = deger, ["gerekce"] = gerekce, ["onayKodu"] = kod,
    };

    private const string Gerekce = "RG 33400'deki rakam yanlış yazıldı; doğru değer giriliyor (test).";

    private void GeriYukle(string? acilacakId)
    {
        using var db = f.Vt.OlusturSistem();
        db.Database.ExecuteSqlRaw(
            "DELETE FROM yasal_parametreler WHERE anahtar = 'MASAK_EK_BELGE_ESIGI_TL' AND olusturan LIKE 'sistem:%'");
        if (acilacakId is not null)
            db.Database.ExecuteSqlRaw("UPDATE yasal_parametreler SET gecerlilik_bitis = NULL WHERE id = {0}", acilacakId);
        f.Ortam.Services.GetRequiredService<YasalParametreServisi>().Tazele();
    }

    [Fact]
    public async Task Ayni_gun_duzeltme_ayni_operator_201_baska_operator_403_gerekce_400_onay_kodu_denetim_izi_gecmis_korunur()
    {
        var servis = f.Ortam.Services.GetRequiredService<YasalParametreServisi>();
        var onceki = servis.SatirBul(EB, Bugun)!;
        using var kur = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "PAKETYASALPARAM2-TEST-duzelt");
        var sahip = f.Ortam.Istemci(k, "SAHIP");
        var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
        var baska = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari, "baska-operator");
        try
        {
            // 1) Bugünden geçerli YANLIŞ giriş: 10.000.000.
            var on = await sis.PostAsJsonAsync(Uc + "/onizleme", YeniGovde(10_000_000m));
            Assert.True(on.StatusCode == HttpStatusCode.OK, await on.Content.ReadAsStringAsync());
            var yaz = await sis.PostAsJsonAsync(Uc, new Dictionary<string, object?>(YeniGovde(10_000_000m))
                { ["onayKodu"] = (await Govde(on)).GetProperty("onayKodu").GetString() });
            Assert.Equal(HttpStatusCode.Created, yaz.StatusCode);
            var yanlisId = (await Govde(yaz)).GetProperty("id").GetString()!;
            Assert.True((await Govde(await sahip.GetAsync("/api/v1/masak/gereksinim?tutar=15000000")))
                .GetProperty("ekBelgeGerekli").GetBoolean());

            // Liste: satır aynı gün düzeltilebilir + yürürlük saati.
            var liste = await Govde(await sahip.GetAsync("/api/v1/yasal-parametreler"));
            var ls = liste.GetProperty("satirlar").EnumerateArray().Single(s => s.GetProperty("id").GetString() == yanlisId);
            Assert.True(ls.GetProperty("ayniGunDuzeltilebilir").GetBoolean());
            Assert.Matches(@"^\d{2}:\d{2}$", ls.GetProperty("baslangicSaati").GetString()!);

            // 2) Aynı gün ikinci "yeni değer" yazılmaz → düzeltme yolunu söyleyen 409.
            var ikinci = await sis.PostAsJsonAsync(Uc + "/onizleme", YeniGovde(12_000_000m));
            Assert.Equal(HttpStatusCode.Conflict, ikinci.StatusCode);
            Assert.Contains("Bugün girilen değeri düzelt", (await Govde(ikinci)).GetProperty("message").GetString());

            // 3) BAŞKA operatör düzeltemez → 403 (önizleme ve kayıt), hiçbir şey yazılmaz.
            var b1 = await baska.PostAsJsonAsync($"{Uc}/{yanlisId}/duzeltme/onizleme", DuzeltGovde(16_000_000m, Gerekce));
            Assert.Equal(HttpStatusCode.Forbidden, b1.StatusCode);
            Assert.Contains("yalnız giren kişi", (await Govde(b1)).GetProperty("message").GetString());
            var b2 = await baska.PostAsJsonAsync($"{Uc}/{yanlisId}/duzeltme", DuzeltGovde(16_000_000m, Gerekce, "X"));
            Assert.Equal(HttpStatusCode.Forbidden, b2.StatusCode);
            // JWT (SAHİP) ile hiç erişilemez.
            var jwt = await sahip.PostAsJsonAsync($"{Uc}/{yanlisId}/duzeltme", DuzeltGovde(16_000_000m, Gerekce, "X"));
            Assert.True(jwt.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden, jwt.StatusCode.ToString());

            // 4) Aynı operatör: kısa gerekçe 400 (alan), aynı değer 409, onay kodu yok 400, yanlış kod 409.
            var kisa = await sis.PostAsJsonAsync($"{Uc}/{yanlisId}/duzeltme/onizleme", DuzeltGovde(16_000_000m, "kısa"));
            Assert.Equal(HttpStatusCode.BadRequest, kisa.StatusCode);
            Assert.True((await Govde(kisa)).GetProperty("alanlar").TryGetProperty("gerekce", out _));
            var ayni = await sis.PostAsJsonAsync($"{Uc}/{yanlisId}/duzeltme/onizleme", DuzeltGovde(10_000_000m, Gerekce));
            Assert.Equal(HttpStatusCode.Conflict, ayni.StatusCode);
            var kodsuz = await sis.PostAsJsonAsync($"{Uc}/{yanlisId}/duzeltme", DuzeltGovde(16_000_000m, Gerekce));
            Assert.Equal(HttpStatusCode.BadRequest, kodsuz.StatusCode);
            Assert.True((await Govde(kodsuz)).GetProperty("alanlar").TryGetProperty("onayKodu", out _));
            var yanlisKod = await sis.PostAsJsonAsync($"{Uc}/{yanlisId}/duzeltme", DuzeltGovde(16_000_000m, Gerekce, "0000000000000000"));
            Assert.Equal(HttpStatusCode.Conflict, yanlisKod.StatusCode);
            using (var ara = f.Vt.OlusturSistem())
                Assert.Equal(1, ara.YasalParametreler.Count(p => p.Anahtar == EB && p.Olusturan.StartsWith("sistem:")));

            // 5) Önizleme (yazmaz) → onaylı kayıt 201.
            var don = await sis.PostAsJsonAsync($"{Uc}/{yanlisId}/duzeltme/onizleme", DuzeltGovde(16_000_000m, Gerekce));
            Assert.True(don.StatusCode == HttpStatusCode.OK, await don.Content.ReadAsStringAsync());
            var dj = await Govde(don);
            Assert.Equal(10_000_000m, dj.GetProperty("yanlisDeger").GetDecimal());
            Assert.Equal(16_000_000m, dj.GetProperty("yeniDeger").GetDecimal());
            var dk = await sis.PostAsJsonAsync($"{Uc}/{yanlisId}/duzeltme",
                DuzeltGovde(16_000_000m, Gerekce, dj.GetProperty("onayKodu").GetString()));
            Assert.Equal(HttpStatusCode.Created, dk.StatusCode);
            var sj = await Govde(dk);
            Assert.Equal(yanlisId, sj.GetProperty("duzeltilenSatirId").GetString());
            var dogruId = sj.GetProperty("id").GetString()!;

            // Kapı bugün DÜZELTİLMİŞ değeri okur (15.000.000 < 16.000.000 → ek belge gerekmez).
            Assert.False((await Govde(await sahip.GetAsync("/api/v1/masak/gereksinim?tutar=15000000")))
                .GetProperty("ekBelgeGerekli").GetBoolean());

            // 6) SQL: yanlış satır SİLİNMEDİ/DEĞİŞMEDİ (yalnız bitiş bugün), doğru satır kendi anından açık uçlu.
            using var db = f.Vt.OlusturSistem();
            var yanlis = db.YasalParametreler.AsNoTracking().Single(p => p.Id == yanlisId);
            var dogru = db.YasalParametreler.AsNoTracking().Single(p => p.Id == dogruId);
            Assert.Equal(10_000_000m, yanlis.DegerSayi);
            Assert.Equal(Bugun, yanlis.GecerlilikBitis);
            Assert.Null(dogru.GecerlilikBitis);
            Assert.Equal(16_000_000m, dogru.DegerSayi);
            Assert.Equal("sistem:asikin-qa", dogru.Olusturan);
            Assert.True(dogru.GecerlilikBaslangicAni > yanlis.GecerlilikBaslangicAni);
            var iz = Assert.Single(db.DenetimKayitlari.AsNoTracking().Where(d => d.VarlikId == dogruId).Select(d => d.Detay ?? ""));
            Assert.Contains("AYNI GÜN DÜZELTİLDİ", iz);
            Assert.Contains($"yanlış 10000000 ({yanlisId}) → doğru 16000000", iz);
            Assert.Contains("bu aralıktaki işlemler değişmez", iz);
            Assert.Contains("operatör: asikin-qa", iz);
            Assert.Contains("gerekçe: " + Gerekce, iz);

            // Geçmiş korunur: iki an arasındaki işlem YANLIŞ değerle, öncesi önceki değerle, sonrası doğru değerle.
            var a1 = DateTime.SpecifyKind(yanlis.GecerlilikBaslangicAni!.Value, DateTimeKind.Utc);
            var a2 = DateTime.SpecifyKind(dogru.GecerlilikBaslangicAni!.Value, DateTimeKind.Utc);
            var orta = a1.AddTicks((a2 - a1).Ticks / 2);
            Assert.Equal(10_000_000m, Assert.Single(servis.DonemlerAn(EB, orta, orta)).Deger);
            Assert.Equal(onceki.DegerSayi, Assert.Single(servis.DonemlerAn(EB, a1.AddSeconds(-1), a1.AddSeconds(-1))).Deger);
            Assert.Equal(16_000_000m, Assert.Single(servis.DonemlerAn(EB, a2.AddTicks(1), a2.AddTicks(1))).Deger);

            // 7) Kapanmış (düzeltilmiş) satır yeniden düzeltilemez 409; tohum satırı 409; olmayan 404.
            Assert.Equal(HttpStatusCode.Conflict,
                (await sis.PostAsJsonAsync($"{Uc}/{yanlisId}/duzeltme/onizleme", DuzeltGovde(17_000_000m, Gerekce))).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict,
                (await sis.PostAsJsonAsync($"{Uc}/{onceki.Id}/duzeltme/onizleme", DuzeltGovde(17_000_000m, Gerekce))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await sis.PostAsJsonAsync($"{Uc}/yok-boyle-bir-satir/duzeltme/onizleme", DuzeltGovde(17_000_000m, Gerekce))).StatusCode);
        }
        finally
        {
            GeriYukle(onceki.Id);
        }
        Assert.Equal(onceki.DegerSayi, servis.SatirBul(EB, Bugun)!.DegerSayi);
    }
}

public sealed class YasalParam2EkranTestleri : BunitContext
{
    private readonly List<(string Yol, string? Operator, string? Govde)> istekler = [];
    private bool duzeltilebilir = true;
    private int kayitKodu = 201;

    private string Liste => """
        {"tarih":"2026-10-08","bugun":"2026-10-08","bayramEksik":false,"yonetimAcik":true,"tatiller":[],
         "satirlar":[
          {"id":"w1","anahtar":"MASAK_KIMLIK_ESIGI_TL","degerSayi":250000,"degerMetin":null,"birim":"TL",
           "baslangic":"2026-10-08","bitis":null,"dayanak":"Tedbirler Yönetmeliği md.5/1-b",
           "kaynakUrl":"https://www.resmigazete.gov.tr/x.pdf","teyit":"DOGRULANDI","aciklamaSade":"x","kullaniliyor":true,
           "esikTuru":"VE_USTU","esikTutari":250000,"resmiGazeteTarihi":"2026-10-08","resmiGazeteSayisi":"33400",
           "baslangicSaati":"12:05","ayniGunDuzeltilebilir":DZ}]}
        """.Replace("DZ", duzeltilebilir ? "true" : "false");

    private const string Onizleme = """
        {"id":"w1","anahtar":"MASAK_KIMLIK_ESIGI_TL","birim":"TL","yanlisDeger":250000,"yeniDeger":260000,
         "oncekiDeger":370000,"yanlisSaati":"12:05:10","onayKodu":"ABCDEF0123456789","buyukDegisim":false}
        """;

    private const string Sonuc = """
        {"id":"c1","anahtar":"MASAK_KIMLIK_ESIGI_TL","birim":"TL","degerSayi":260000,"yanlisDeger":250000,
         "yanlisSaati":"12:05:10","duzeltmeSaati":"12:40:02","duzeltilenSatirId":"w1"}
        """;

    public YasalParam2EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi(YasalParam2EkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? null : await istek.Content.ReadAsStringAsync(iptal);
            var op = istek.Headers.TryGetValues("X-Sistem-Operator", out var o) ? o.First() : null;
            lock (t.istekler) t.istekler.Add((yol, op, govde));
            var (kod, cevap) = yol switch
            {
                var y when y.EndsWith("/duzeltme/onizleme") => (200, Onizleme),
                var y when y.EndsWith("/duzeltme") => (t.kayitKodu, t.kayitKodu == 201 ? Sonuc
                    : """{"message":"Bu değeri başka bir yönetici girdi; aynı gün yalnız giren kişi düzeltebilir.","statusCode":403}"""),
                var y when y.Contains("/yasal-parametreler/gecmis") => (200, """{"anahtar":"MASAK_KIMLIK_ESIGI_TL","satirlar":[]}"""),
                var y when y.Contains("/yasal-parametreler") => (200, t.Liste),
                _ => (200, "{}"),
            };
            return new HttpResponseMessage((HttpStatusCode)kod)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.YasalSinirlar> Ac()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/yasal-sinirlar?yonetim=1");
        var e = Render<Kuyumcu.Web.Pages.YasalSinirlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ys-liste")));
        return e;
    }

    private static void Doldur(IRenderedComponent<Kuyumcu.Web.Pages.YasalSinirlar> e)
    {
        e.Find("#ysy-dz-deger").Input("260000");
        e.Find("#ysy-dz-gerekce").Input("RG rakamı yanlış yazılmıştı, düzeltiliyor (test).");
        e.Find("#ysy-dz-anahtar").Input("gizli-test-anahtari-gizli-test-anahtari");
        e.Find("#ysy-dz-operator").Input("Ramazan");
    }

    [Fact]
    public void Duzeltilebilir_satir_yoksa_bolum_YOK_varsa_KAPALI_katlanir_tek_satir_secicisiz()
    {
        duzeltilebilir = false;
        var yok = Ac();
        Assert.Empty(yok.FindAll("#ysy-dz"));

        duzeltilebilir = true;
        var e = Render<Kuyumcu.Web.Pages.YasalSinirlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ysy-dz")));
        Assert.Empty(e.FindAll("#ysy-dz-deger"));                 // zorunlu önce: kapalı gelir
        e.Find("#ysy-dz-ac").Click();
        Assert.Contains("12:05", e.Find("#ysy-dz-satir-tek").TextContent);
        Assert.Empty(e.FindAll("#ysy-dz-satir"));                 // UX-S22: tek satır → seçici yok
    }

    [Fact]
    public void Bos_onizle_alan_alti_hatalar_istek_ATILMAZ()
    {
        var e = Ac();
        e.Find("#ysy-dz-ac").Click();
        e.Find("#ysy-dz-onizle").Click();
        var m = e.Markup;
        Assert.Contains("Doğru değeri yazın.", m);
        Assert.Contains("Gerekçe en az 20 karakter olmalı (0/20).", m);
        Assert.Contains("Sistem anahtarını girin.", m);
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/duzeltme"));
    }

    [Fact]
    public void Onizle_onay_penceresi_yanlis_dogru_saat_kaydet_onay_kodu_basari_bandi()
    {
        var e = Ac();
        e.Find("#ysy-dz-ac").Click();
        Doldur(e);
        e.Find("#ysy-dz-onizle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ysy-dz-onay")));
        var on = istekler.Single(i => i.Yol.EndsWith("/duzeltme/onizleme"));
        Assert.Equal("/api/v1/sistem/yasal-parametreler/w1/duzeltme/onizleme", on.Yol);
        var j = JsonDocument.Parse(on.Govde!).RootElement;
        Assert.Equal(260_000m, j.GetProperty("degerSayi").GetDecimal());
        Assert.Equal(JsonValueKind.Null, j.GetProperty("onayKodu").ValueKind);
        Assert.Contains("yanlış 250.000", e.Find("#ysy-dz-onay-deger").TextContent);
        Assert.Contains("doğru 260.000", e.Find("#ysy-dz-onay-deger").TextContent);
        Assert.Contains("12:05:10", e.Find("#ysy-dz-onay-etki").TextContent);
        Assert.Contains("işlemler değişmez", e.Find("#ysy-dz-onay-etki").TextContent);

        e.Find("#ysy-dz-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains("Düzeltildi", e.Find("#ysy-basari").TextContent));
        var kayit = istekler.Single(i => i.Yol.EndsWith("/w1/duzeltme"));
        Assert.Equal("ABCDEF0123456789", JsonDocument.Parse(kayit.Govde!).RootElement.GetProperty("onayKodu").GetString());
        Assert.Contains("12:05:10–12:40:02", e.Find("#ysy-basari").TextContent);
        Assert.Empty(e.FindAll("#ysy-dz-onay"));
    }

    [Fact]
    public void Baska_operator_403_onay_penceresinde_hata_bandi_sessiz_kalmaz()
    {
        kayitKodu = 403;
        var e = Ac();
        e.Find("#ysy-dz-ac").Click();
        Doldur(e);
        e.Find("#ysy-dz-onizle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ysy-dz-onay")));
        e.Find("#ysy-dz-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains("yalnız giren kişi", e.Find("#ysy-dz-onay-hata").TextContent));
    }
}
