using System.Net;
using System.Text;
using System.Text.Json;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ════════════════════════════════════════════════════════════════════════
// PAKET SON-KALANLAR — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT metin/sayıdır (üretim
// kodundan hesaplanmaz). Kapsam: banka hesabı para birimi kilidi (null = koru,
// "" = 400, küçük harf, aynı değer), Gün Kapat sayılan nakit (sıfır fark, eksi
// 0,0001, yarım kuruş 0,005 → 0,01 fark yönü, çok büyük tutar, gövdesiz, boşluk
// gerekçe), kasa hareketi yarım kuruş (0,005 → 0,01 yazılır; 0,0049 → en az 0,01),
// eksi tutar, sunucu ileti para biçimi, Türkçe sayı girişi (FormAlani.SayiCoz).
// Proje yuvarlama kuralı: MidpointRounding.AwayFromZero (2 hane para).
// ════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class SONKALANLARSinirHttpTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"PAKETSON-KALANLAR-TEST D {ad}");
    }

    private static Task<HttpResponseMessage> Ham(HttpClient c, HttpMethod m, string yol, string json) =>
        c.SendAsync(new HttpRequestMessage(m, yol) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    private static async Task<string> Red(HttpResponseMessage r, HttpStatusCode kod, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == kod, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), "gövdede 'alanlar' yok: " + govde);
        var p = alanlar.EnumerateObject().FirstOrDefault(x => x.Name.Equals(alan, StringComparison.OrdinalIgnoreCase));
        Assert.True(p.Value.ValueKind == JsonValueKind.Array, $"'{alan}' alanına işaretli değil: {govde}");
        return p.Value.EnumerateArray().First().GetString() ?? "";
    }

    private static async Task Tamam(HttpResponseMessage r)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode} {govde}");
    }

    private async Task<string> UsdBakiyeliHesap(Kiraci k, HttpClient c)
    {
        var iban = TestIban.Tamamla(string.Concat(Enumerable.Range(0, 22).Select(i => (char)('0' + (k.TenantId[(i * 5 + 3) % k.TenantId.Length] % 10)))));
        await Tamam(await c.PostJson("/api/v1/bankalar", new { bankaAdi = "DS Banka", hesapAdi = "DS Vadesiz", iban, paraBirimi = "USD" }));
        string id;
        using (var db = f.Vt.OlusturSistem()) id = db.BankaHesaplari.AsNoTracking().Single(x => x.TenantId == k.TenantId).Id;
        await Tamam(await Ham(c, HttpMethod.Post, $"/api/v1/bankalar/{id}/hareket", """{"yon":"GELEN","tutar":100}"""));
        return id;
    }

    private (string Pb, decimal Bakiye) Hesap(string id)
    {
        using var db = f.Vt.OlusturSistem();
        var h = db.BankaHesaplari.AsNoTracking().Single(x => x.Id == id);
        return (h.ParaBirimi, h.Bakiye);
    }

    // ── Banka para birimi: boş / null / küçük harf / aynı değer ──

    [Fact]
    public async Task DSK01_banka_bakiyeli_PATCH_paraBirimi_NULL_KORUR_200_USD_100()
    {
        var k = Kur("pb null");
        var c = f.Ortam.Istemci(k);
        var id = await UsdBakiyeliHesap(k, c);
        await Tamam(await Ham(c, HttpMethod.Patch, $"/api/v1/bankalar/{id}", """{"paraBirimi":null,"hesapAdi":"DS Yeni Ad"}"""));
        Assert.Equal(("USD", 100m), Hesap(id));
    }

    [Fact]
    public async Task DSK02_banka_bakiyeli_PATCH_paraBirimi_BOS_DIZGE_400_SQL_DEGISMEZ()
    {
        var k = Kur("pb bos");
        var c = f.Ortam.Istemci(k);
        var id = await UsdBakiyeliHesap(k, c);
        await Red(await Ham(c, HttpMethod.Patch, $"/api/v1/bankalar/{id}", """{"paraBirimi":""}"""),
            HttpStatusCode.BadRequest, "paraBirimi");
        Assert.Equal(("USD", 100m), Hesap(id));
    }

    [Fact]
    public async Task DSK03_banka_bakiyeli_PATCH_kucuk_harf_usd_REDDEDILIR_SQL_USD_kalir()
    {
        var k = Kur("pb kucuk");
        var c = f.Ortam.Istemci(k);
        var id = await UsdBakiyeliHesap(k, c);
        var r = await Ham(c, HttpMethod.Patch, $"/api/v1/bankalar/{id}", """{"paraBirimi":"usd"}""");
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True((int)r.StatusCode is 400 or 409, $"{(int)r.StatusCode} {govde}");
        Assert.Equal(("USD", 100m), Hesap(id));
    }

    // ── Gün Kapat sınırları ──

    [Fact]
    public async Task DSK04_gun_kapat_SIFIR_FARK_gerekcesiz_KAPANIR_sayilan_250000()
    {
        var k = Kur("gun sifir fark");
        await Tamam(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi", """{"sayilanNakit":250000}"""));
        using var db = f.Vt.OlusturSistem();
        var g = db.GunKapanislari.AsNoTracking().Single(x => x.TenantId == k.TenantId);
        Assert.Equal(250000m, g.SayilanNakit);
        Assert.Equal(0m, g.Fark);
    }

    [Fact]
    public async Task DSK05_gun_kapat_EKSI_0_0001_400_sayilanNakit_kapanis_YOK()
    {
        var k = Kur("gun eksi");
        var m = await Red(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            """{"sayilanNakit":-0.0001,"aciklama":"eksi deneme"}"""), HttpStatusCode.BadRequest, "sayilanNakit");
        // PAKET GOC-A: ileti sözlükten (ekranın alan altı metniyle AYNI — K9a "eksi yazılamaz").
        Assert.Equal(Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(Kuyumcu.Core.Domain.Girdi.GirdiKodu.SayiEksi), "tr"), m);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.GunKapanislari.AsNoTracking().Count(x => x.TenantId == k.TenantId));
    }

    // PAKET GOC-A (K9a): 250.000,005 YUVARLANMAZ — 400 alan:sayilanNakit (ondalık); fark kuralı tam kuruşla (0,01 fazla).
    [Fact]
    public async Task DSK06_gun_kapat_YARIM_KURUS_400_ondalik_tam_kurus_0_01_FAZLA_gerekce_ister()
    {
        var k = Kur("gun yarim kurus");
        var o = await Red(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            """{"sayilanNakit":250000.005}"""), HttpStatusCode.BadRequest, "sayilanNakit");
        Assert.Equal(Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(Kuyumcu.Core.Domain.Girdi.GirdiKodu.SayiOndalik, 2), "tr"), o);
        var m = await Red(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            """{"sayilanNakit":250000.01}"""), HttpStatusCode.BadRequest, "aciklama");
        Assert.StartsWith("Sayılan nakit beklenenden 0,01 ₺ fazla", m);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.GunKapanislari.AsNoTracking().Count(x => x.TenantId == k.TenantId));
    }

    // PAKET GOC-A (K9a): 250.000,004 YUVARLANMAZ — 400, kapanış YOK; tam sayımla fark 0 kapanır.
    [Fact]
    public async Task DSK07_gun_kapat_YARIM_KURUS_ALTI_400_tam_sayim_fark_SIFIR_kapanir()
    {
        var k = Kur("gun yarim alti");
        await Red(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi", """{"sayilanNakit":250000.004}"""),
            HttpStatusCode.BadRequest, "sayilanNakit");
        using (var bos = f.Vt.OlusturSistem())
            Assert.Equal(0, bos.GunKapanislari.AsNoTracking().Count(x => x.TenantId == k.TenantId));
        await Tamam(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi", """{"sayilanNakit":250000}"""));
        using var db = f.Vt.OlusturSistem();
        var g = db.GunKapanislari.AsNoTracking().Single(x => x.TenantId == k.TenantId);
        Assert.Equal(250000.00m, g.SayilanNakit);
        Assert.Equal(0m, g.Fark);
    }

    [Fact]
    public async Task DSK08_gun_kapat_COK_BUYUK_999999999_99_gerekceli_KAPANIR_fark_dogru()
    {
        var k = Kur("gun buyuk");
        await Tamam(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            """{"sayilanNakit":999999999.99,"aciklama":"büyük sayım sınır denemesi"}"""));
        using var db = f.Vt.OlusturSistem();
        var g = db.GunKapanislari.AsNoTracking().Single(x => x.TenantId == k.TenantId);
        Assert.Equal(999999999.99m, g.SayilanNakit);
        Assert.Equal(999749999.99m, g.Fark);
    }

    [Fact]
    public async Task DSK09_gun_kapat_GOVDESIZ_ve_BOSLUK_gerekce_400_alanli()
    {
        var k = Kur("gun bos");
        var c = f.Ortam.Istemci(k);
        await Red(await Ham(c, HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi", "{}"),
            HttpStatusCode.BadRequest, "sayilanNakit");
        var m = await Red(await Ham(c, HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            """{"sayilanNakit":1000,"aciklama":"   "}"""), HttpStatusCode.BadRequest, "aciklama");
        Assert.Contains("249.000,00 ₺ eksik", m);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.GunKapanislari.AsNoTracking().Count(x => x.TenantId == k.TenantId));
    }

    // ── Kasa hareketi: yarım kuruş ve eksi ──

    // Doğrulayıcı notu (tur 1): HTTP yolunda DTO [Range(0.01)] 0,005'i servise ulaşmadan reddediyordu; servis
    // yolu ve tahsilat 0,01 olarak kabul ediyordu (uç tutarsızlığı, DÜŞÜK bulgu).
    // SON-KALANLAR tur 2: TEK KURAL — DTO alt sınırı kaldırıldı, kapı KolonTavani.PozitifTutar (önce 2 haneye
    // yuvarla AwayFromZero, sonra en az 0,01). HTTP yolu artık servis yoluyla AYNI: 0,005 → 0,01 yazılır.
    // PAKET GOC-A (K9 · envanter ortak kök 3): TEK KURAL artık "yuvarlama YOK" — 0,005 HTTP yolunda 400 alan:tutar,
    // satır doğmaz, bakiye değişmez (eski: 0,01 yazılırdı). Servis yolu da AYNI (KasaServisi → GirdiKapisi.Tutar).
    [Fact]
    public async Task DSK10_kasa_hareketi_HTTP_0_005_TEK_KURAL_yuvarlanmaz_400_satir_YOK()
    {
        var k = Kur("kasa 0005");
        var m = await Red(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/hareket",
            """{"yon":"GIRIS","tutar":0.005,"odemeTuru":"NAKIT","aciklama":"yarim kurus"}"""), HttpStatusCode.BadRequest, "tutar");
        Assert.Equal(Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(Kuyumcu.Core.Domain.Girdi.GirdiKodu.SayiOndalik, 2), "tr"), m);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(250000m, TestKurulum.KasaBakiye(db, k.KasaId));
        Assert.Equal(0, db.KasaHareketleri.AsNoTracking().Count(x => x.TenantId == k.TenantId && x.Aciklama == "yarim kurus"));
    }

    [Fact]
    public async Task DSK11_kasa_hareketi_0_0049_ve_EKSI_400_tutar_ileti_en_az_0_01()
    {
        var k = Kur("kasa 00049");
        var c = f.Ortam.Istemci(k);
        var m = await Red(await Ham(c, HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/hareket",
            """{"yon":"GIRIS","tutar":0.0049,"odemeTuru":"NAKIT"}"""), HttpStatusCode.BadRequest, "tutar");
        // PAKET GOC-A: 0,0049 dört ondalıklıdır → K9 ondalık iletisi (sözlük; ekranla AYNI cümle).
        Assert.Equal(Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(Kuyumcu.Core.Domain.Girdi.GirdiKodu.SayiOndalik, 2), "tr"), m);
        await Red(await Ham(c, HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/hareket",
            """{"yon":"GIRIS","tutar":-5,"odemeTuru":"NAKIT"}"""), HttpStatusCode.BadRequest, "tutar");
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(250000m, TestKurulum.KasaBakiye(db, k.KasaId));
    }
}

public sealed class SONKALANLARSinirBirimTestleri
{
    [Theory]
    [InlineData("0.005", "TRY", "0,01 ₺")]
    [InlineData("-5", "TRY", "-5,00 ₺")]
    [InlineData("999999999.99", "TRY", "999.999.999,99 ₺")]
    [InlineData("0", "USD", "0,00 $")]
    [InlineData("1.5", null, "1,50 ₺")]
    public void DSK20_sunucu_ileti_para_bicimi_tr_TR(string tutar, string? pb, string beklenen)
    {
        var d = decimal.Parse(tutar, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(beklenen, Kuyumcu.Api.Modules.Kasa.GunKapanisiKapisi.Para(d, pb));
    }

    [Fact]
    public void DSK21_turkce_giris_45000_1_5_belirsiz_1500_bos()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(0.0001m, FormAlani.SayiCoz("0,0001").Deger);
        var b = FormAlani.SayiCoz("1.500");
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, b.Sonuc);
        Assert.Null(b.Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
    }

    [Theory]
    [InlineData("0.005", "0.01")]
    [InlineData("0.0049", "0.00")]
    [InlineData("-0.005", "-0.01")]
    public void DSK22_IsaretliTutar_AwayFromZero_yarim_kurus(string g, string b)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        Assert.Equal(decimal.Parse(b, inv),
            Kuyumcu.Api.Modules.KolonTavani.IsaretliTutar(decimal.Parse(g, inv), "tutar", "Tutar"));
    }

    [Fact]
    public void DSK23_PozitifTutar_tavan_ve_en_az()
    {
        var ex = Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(() =>
            Kuyumcu.Api.Modules.KolonTavani.PozitifTutar(0.0049m, "tutar", "Tutar"));
        Assert.Equal("Tutar en az 0,01 olmalıdır — daha küçük tutar kaydedilemez", ex.Message);
        var buyuk = Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(() =>
            Kuyumcu.Api.Modules.KolonTavani.PozitifTutar(10_000_000_000_000m, "tutar", "Tutar"));
        Assert.Equal("Tutar çok büyük — kaydedilebilecek en büyük değeri aşıyor", buyuk.Message);
        Assert.Equal(999_999_999.99m, Kuyumcu.Api.Modules.KolonTavani.PozitifTutar(999_999_999.99m, "tutar", "Tutar"));
    }
}

// ════════════════════════════════════════════════════════════════════════
// SON-KALANLAR TUR 2 — HASIM DOĞRULAYICI (Aşkın + Mehmet) EK SINIR TESTLERİ
// Kapsam: banka para birimi kilidinin HAREKET dalı (bakiye 0'a dönmüş ama dekontu
// olan hesap), geçmişsiz hesapta serbest değişim, DTO [Range] kaldırıldıktan sonra
// gider/virman/banka hareketinde eksi ve sıfır tutar, gövdede tutar YOK, yarım
// kuruş (0,005 → 0,01 AwayFromZero), çok büyük tutar (999.999.999,99) kabul.
// Beklenen değerler SABİT; üretim kodundan hesaplanmaz.
// ════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class SONKALANLARTur2SinirTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"PAKETSON-KALANLAR-TEST T2 {ad}");
    }

    private static Task<HttpResponseMessage> Ham(HttpClient c, HttpMethod m, string yol, string json) =>
        c.SendAsync(new HttpRequestMessage(m, yol) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    private static async Task<string> Red(HttpResponseMessage r, HttpStatusCode kod, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == kod, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), "gövdede 'alanlar' yok: " + govde);
        var p = alanlar.EnumerateObject().FirstOrDefault(x => x.Name.Equals(alan, StringComparison.OrdinalIgnoreCase));
        Assert.True(p.Value.ValueKind == JsonValueKind.Array, $"'{alan}' alanına işaretli değil: {govde}");
        return p.Value.EnumerateArray().First().GetString() ?? "";
    }

    private static async Task Tamam(HttpResponseMessage r)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode} {govde}");
    }

    private async Task<string> UsdHesap(Kiraci k, HttpClient c)
    {
        var iban = TestIban.Tamamla(string.Concat(Enumerable.Range(0, 22).Select(i => (char)('0' + (k.TenantId[(i * 7 + 1) % k.TenantId.Length] % 10)))));
        await Tamam(await c.PostJson("/api/v1/bankalar", new { bankaAdi = "T2 Banka", hesapAdi = "T2 Vadesiz", iban, paraBirimi = "USD" }));
        using var db = f.Vt.OlusturSistem();
        return db.BankaHesaplari.AsNoTracking().Single(x => x.TenantId == k.TenantId).Id;
    }

    private (string Pb, decimal Bakiye, int Hareket) Hesap(string id)
    {
        using var db = f.Vt.OlusturSistem();
        var h = db.BankaHesaplari.AsNoTracking().Single(x => x.Id == id);
        return (h.ParaBirimi, h.Bakiye, db.BankaHareketleri.AsNoTracking().Count(x => x.BankaHesapId == id));
    }

    private async Task<string> Kategori(Kiraci k, HttpClient c)
    {
        await Tamam(await c.PostJson("/api/v1/gider-kategorileri", new { ad = "T2 Kira" }));
        using var db = f.Vt.OlusturSistem();
        return db.GiderKategorileri.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Ad == "T2 Kira").Id;
    }

    private (decimal Bakiye, int Hareket) Kasa(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return (TestKurulum.KasaBakiye(db, k.KasaId), db.KasaHareketleri.AsNoTracking().Count(x => x.TenantId == k.TenantId));
    }

    [Fact]
    public async Task DSK30_banka_bakiye_SIFIRA_donmus_ama_HAREKETLI_hesap_PATCH_TRY_409_alan_paraBirimi_SQL_USD()
    {
        var k = Kur("pb hareket");
        var c = f.Ortam.Istemci(k);
        var id = await UsdHesap(k, c);
        await Tamam(await Ham(c, HttpMethod.Post, $"/api/v1/bankalar/{id}/hareket", """{"yon":"GELEN","tutar":100}"""));
        await Tamam(await Ham(c, HttpMethod.Post, $"/api/v1/bankalar/{id}/hareket", """{"yon":"GIDEN","tutar":100}"""));
        Assert.Equal(("USD", 0m, 2), Hesap(id));
        var m = await Red(await Ham(c, HttpMethod.Patch, $"/api/v1/bankalar/{id}", """{"paraBirimi":"TRY"}"""),
            HttpStatusCode.Conflict, "paraBirimi");
        Assert.Equal("Hareket görmüş banka hesabının para birimi değiştirilemez — yeni hesap açın", m);
        Assert.Equal(("USD", 0m, 2), Hesap(id));
    }

    [Fact]
    public async Task DSK31_banka_GECMISSIZ_hesap_PATCH_EUR_200_ve_ayni_deger_USD_bakiyeli_200()
    {
        var k = Kur("pb serbest");
        var c = f.Ortam.Istemci(k);
        var id = await UsdHesap(k, c);
        await Tamam(await Ham(c, HttpMethod.Patch, $"/api/v1/bankalar/{id}", """{"paraBirimi":"EUR"}"""));
        Assert.Equal(("EUR", 0m, 0), Hesap(id));
        await Tamam(await Ham(c, HttpMethod.Post, $"/api/v1/bankalar/{id}/hareket", """{"yon":"GELEN","tutar":50}"""));
        // Aynı para birimi gönderimi değişim sayılmaz → kilit tetiklenmez.
        await Tamam(await Ham(c, HttpMethod.Patch, $"/api/v1/bankalar/{id}", """{"paraBirimi":"EUR","hesapAdi":"T2 Yeni"}"""));
        Assert.Equal(("EUR", 50m, 1), Hesap(id));
    }

    [Theory]
    [InlineData("-5")]
    [InlineData("0")]
    [InlineData("0.004")]
    public async Task DSK32_gider_EKSI_SIFIR_YARIM_ALTI_400_alan_tutar_kasa_DEGISMEZ(string tutar)
    {
        var k = Kur("gider " + tutar);
        var c = f.Ortam.Istemci(k);
        var kat = await Kategori(k, c);
        var once = Kasa(k);
        await Red(await Ham(c, HttpMethod.Post, "/api/v1/giderler",
            $$"""{"kasaId":"{{k.KasaId}}","giderKategoriId":"{{kat}}","tutar":{{tutar}}}"""), HttpStatusCode.BadRequest, "tutar");
        Assert.Equal(once, Kasa(k));
    }

    [Fact]
    public async Task DSK33_gider_GOVDEDE_TUTAR_YOK_400_alan_tutar_kasa_DEGISMEZ()
    {
        var k = Kur("gider tutarsiz");
        var c = f.Ortam.Istemci(k);
        var kat = await Kategori(k, c);
        var once = Kasa(k);
        await Red(await Ham(c, HttpMethod.Post, "/api/v1/giderler",
            $$"""{"kasaId":"{{k.KasaId}}","giderKategoriId":"{{kat}}"}"""), HttpStatusCode.BadRequest, "tutar");
        Assert.Equal(once, Kasa(k));
    }

    // PAKET GOC-A (K9): gider 0,005 YUVARLANMAZ (eski: 0,01 yazılırdı) — 400 alan:tutar, kasa DEĞİŞMEZ; 0,01 aynen yazılır.
    [Fact]
    public async Task DSK34_gider_YARIM_KURUS_0_005_yuvarlanmaz_400_0_01_yazilir_bakiye_249999_99()
    {
        var k = Kur("gider 0005");
        var c = f.Ortam.Istemci(k);
        var kat = await Kategori(k, c);
        var once = Kasa(k);
        await Red(await Ham(c, HttpMethod.Post, "/api/v1/giderler",
            $$"""{"kasaId":"{{k.KasaId}}","giderKategoriId":"{{kat}}","tutar":0.005}"""), HttpStatusCode.BadRequest, "tutar");
        Assert.Equal(once, Kasa(k));
        await Tamam(await Ham(c, HttpMethod.Post, "/api/v1/giderler",
            $$"""{"kasaId":"{{k.KasaId}}","giderKategoriId":"{{kat}}","tutar":0.01}"""));
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(249999.99m, TestKurulum.KasaBakiye(db, k.KasaId));
        Assert.Equal(1, db.KasaHareketleri.AsNoTracking().Count(x => x.TenantId == k.TenantId && x.Tutar == -0.01m));
    }

    [Theory]
    [InlineData("-10")]
    [InlineData("0")]
    public async Task DSK35_virman_EKSI_ve_SIFIR_400_alan_tutar_iki_kasa_DEGISMEZ(string tutar)
    {
        var k = Kur("virman " + tutar);
        string hedef;
        using (var db = f.Vt.OlusturSistem()) hedef = TestKurulum.KasaEkle(db, k.TenantId, k.SubeId, "TRY", 0m, "T2 Hedef");
        var once = Kasa(k);
        await Red(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, "/api/v1/virman",
            $$"""{"kaynakTur":"KASA","kaynakId":"{{k.KasaId}}","hedefTur":"KASA","hedefId":"{{hedef}}","tutar":{{tutar}}}"""),
            HttpStatusCode.BadRequest, "tutar");
        Assert.Equal(once, Kasa(k));
        using var db2 = f.Vt.OlusturSistem();
        Assert.Equal(0m, TestKurulum.KasaBakiye(db2, hedef));
    }

    [Theory]
    [InlineData("-100")]
    [InlineData("0")]
    [InlineData("0.0049")]
    public async Task DSK36_banka_hareket_EKSI_SIFIR_YARIM_ALTI_400_alan_tutar_bakiye_DEGISMEZ(string tutar)
    {
        var k = Kur("banka " + tutar);
        var c = f.Ortam.Istemci(k);
        var id = await UsdHesap(k, c);
        await Red(await Ham(c, HttpMethod.Post, $"/api/v1/bankalar/{id}/hareket", $$"""{"yon":"GELEN","tutar":{{tutar}}}"""),
            HttpStatusCode.BadRequest, "tutar");
        Assert.Equal(("USD", 0m, 0), Hesap(id));
    }

    [Fact]
    public async Task DSK37_kasa_hareketi_COK_BUYUK_999999999_99_kabul_bakiye_1000249999_99()
    {
        var k = Kur("kasa buyuk");
        await Tamam(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/hareket",
            """{"yon":"GIRIS","tutar":999999999.99,"odemeTuru":"HAVALE","aciklama":"buyuk"}"""));
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(1000249999.99m, TestKurulum.KasaBakiye(db, k.KasaId));
    }

    [Fact]
    public async Task DSK38_gun_kapat_TIRNAKLI_sayi_400_sade_ileti_teknik_ayrinti_YOK()
    {
        var k = Kur("gun dizge");
        var m = await Red(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            """{"sayilanNakit":"1000"}"""), HttpStatusCode.BadRequest, "sayilanNakit");
        Assert.DoesNotContain("System.", m);
        Assert.DoesNotContain("$.", m);
        Assert.Contains("geçerli bir sayı değil", m);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.GunKapanislari.AsNoTracking().Count(x => x.TenantId == k.TenantId));
    }
}
