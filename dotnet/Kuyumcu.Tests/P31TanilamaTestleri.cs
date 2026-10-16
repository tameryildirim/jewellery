using System.Reflection;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Modules.Destek;
using Kuyumcu.Api.Modules.Telemetri;
using Kuyumcu.Core.I18n;
using Kuyumcu.Core.Sozlesmeler;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-31 (R-5) — SAF TESTLER: kısa hata kodu, tanılama denetçisi,
// allow-list, sunucu günlüğünde kodun bulunması, sözlük kapsamı.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P31TanilamaTestleri
{
    // ── Kısa hata kodu ───────────────────────────────────────────────────────

    [Fact]
    public void Kisa_kod_ULIDin_son_8_karakteridir_ve_uydurulmaz()
    {
        Assert.Equal("0000ABCD", HataKodu.Kisa("01KSTKM1K0000000000000ABCD"));
        Assert.Equal("REQ-42", HataKodu.Kisa("req-42"));
        Assert.Null(HataKodu.Kisa(null));
        Assert.Null(HataKodu.Kisa("   "));
    }

    [Theory]
    [InlineData("0000ABCD", true)]
    [InlineData("0000abcd", true)]
    [InlineData("01KSTKM1K0000000000000ABCD", true)]
    [InlineData("0000ABC", false)]        // 7 karakter
    [InlineData("0000ABCI", false)]       // I Crockford'da yok
    [InlineData("Ayse 053", false)]       // serbest metin
    [InlineData("", false)]
    public void Kullanici_kodu_yalniz_Crockford_8_ya_da_26(string kod, bool gecerli)
        => Assert.Equal(gecerli, HataKodu.GecerliMi(kod));

    // ── Tanılama denetçisi (fail-closed) ─────────────────────────────────────

    [Theory]
    [InlineData("TCKN", "\"not\": \"12345678901\"")]
    [InlineData("TELEFON", "\"x\": \"0532 123 45 67\"")]
    [InlineData("TELEFON", "\"x\": \"+905321234567\"")]
    [InlineData("EPOSTA", "\"x\": \"ayse@ornek.com\"")]
    [InlineData("IBAN", "\"x\": \"TR330006100519786457841326\"")]
    [InlineData("KART", "\"x\": \"4543123412341234\"")]
    [InlineData("JWT", "\"x\": \"eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.abc\"")]
    [InlineData("BAGLANTI_DIZESI", "\"x\": \"server=localhost;password=gizli\"")]
    [InlineData("OZEL_ANAHTAR", "\"x\": \"-----BEGIN PRIVATE KEY-----\"")]
    [InlineData("BEARER", "\"x\": \"Bearer abcdefghijklmnop\"")]
    [InlineData("SIR_ALANI", "\"parola\": \"x\"")]
    public void Denetci_sir_ve_kisisel_veriyi_yakalar(string tur, string parca)
        => Assert.Contains(tur, TanilamaDenetcisi.Tara("{" + parca + "}"));

    [Fact]
    public void Gercekci_temiz_paket_denetimden_gecer()
    {
        // Paketin gerçek alanlarına benzeyen örnek: ULID'ler, migration adları
        // (14 haneli damga), ISO tarih, küçük sayaçlar — hiçbiri yanlış alarm
        // üretmemeli; yoksa fail-closed kapı ürünü kullanılmaz kılar.
        const string ornek = """
            {"bicim":1,"paketKodu":"01M37H5Z2NZA72V8GY8K4NHDCW","olusturmaUtc":"2026-09-23T17:10:11.1234567Z",
             "kiraciKisa":"4NHDCW","isteyenRol":"SAHIP","konu":"HATA","hataKodu":"8K4NHDCW",
             "surum":{"surum":"1.0.0","derlemeZamani":"2026-09-23T06:04:00.0000000Z","ortam":"Production",
               "migrationSayisi":70,"sonMigration":"20260923174110_B3PesinMusteriSistemCarisi",
               "bekleyenMigrationlar":["20260922034537_P28TelemetriVeDestekDakikasi"]},
             "kapilar":{"uretim":true,"httpsYonlendirme":true,"swagger":false},
             "saglik":{"veritabaniErisilebilir":true,"son24SaatIstek":1523,"son24SaatSunucuHatasi":2},
             "sonDenetim":[{"tarih":"2026-09-23T17:00:00Z","eylem":"EKLEME","varlikTuru":"Satis","kullaniciKisa":"Z2NZA7"}],
             "denetimKirpildi":false,"icermez":["tutar, bakiye, has gram, fiyat"]}
            """;
        Assert.Empty(TanilamaDenetcisi.Tara(ornek));
    }

    // ── Allow-list: yasak alan adı YOK ───────────────────────────────────────

    private static readonly string[] YasakAdlar =
    [
        "Ad", "Soyad", "Unvan", "Tckn", "Telefon", "Eposta", "Email", "Adres",
        "Tutar", "Bakiye", "Fiyat", "Gram", "Parola", "Sifre", "Anahtar", "Token",
        "Jeton", "Baglanti", "Ip", "IpAdresi", "UserAgent", "Detay", "Gerekce", "Aciklama",
        "KullaniciId", "TenantId", "KullaniciAdi",
    ];

    [Theory]
    [InlineData(typeof(TanilamaPaketi))]
    [InlineData(typeof(TanilamaSurumu))]
    [InlineData(typeof(TanilamaKapilari))]
    [InlineData(typeof(TanilamaSagligi))]
    [InlineData(typeof(TanilamaDenetimSatiri))]
    public void Paket_allow_list_kayitlarinda_yasak_alan_adi_yok(Type tip)
    {
        var alanlar = tip.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name).ToList();
        Assert.NotEmpty(alanlar);
        foreach (var ad in alanlar)
            Assert.DoesNotContain(ad, YasakAdlar);
    }

    // ── Sunucu günlüğü: kullanıcının gördüğü kod günlükte ────────────────────

    private sealed class Kaydedici : ILogger<HataYakalayici>
    {
        public readonly List<(LogLevel Duzey, string Metin)> Satirlar = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Satirlar.Add((logLevel, formatter(state, exception)));
    }

    private static async Task<Kaydedici> Kos(Exception firlat)
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        ctx.Items[IstekKimligiAlani.OgeAnahtari] = "01TESTKIMLIK0000000000TEST";
        var g = new Kaydedici();
        await new HataYakalayici(_ => throw firlat, g).InvokeAsync(ctx);
        return g;
    }

    [Fact]
    public async Task Beklenmeyen_hata_gunlugunde_tam_kimlik_ve_KISA_KOD_var()
    {
        var g = await Kos(new InvalidOperationException("patladı"));
        var satir = Assert.Single(g.Satirlar, s => s.Duzey == LogLevel.Error);
        Assert.Contains("01TESTKIMLIK0000000000TEST", satir.Metin);
        Assert.Contains("hata kodu: 0000TEST", satir.Metin);
    }

    [Fact]
    public async Task Sunucu_500_ApiException_da_gunluge_kisa_kodla_yazilir()
    {
        var g = await Kos(new ApiException(500, "Sürüm damgası okunamadı"));
        var satir = Assert.Single(g.Satirlar, s => s.Duzey == LogLevel.Error);
        Assert.Contains("hata kodu: 0000TEST", satir.Metin);
    }

    [Theory]
    [InlineData(503)]
    [InlineData(404)]
    [InlineData(400)]
    public async Task Bilincli_durumlar_hata_duzeyinde_gunluge_yazilmaz(int kod)
    {
        var g = await Kos(new ApiException(kod, "bilinçli"));
        Assert.DoesNotContain(g.Satirlar, s => s.Duzey >= LogLevel.Error);
    }

    // ── Sözlük: her konu kodunun iki dilde karşılığı var (UX-S31) ────────────

    [Theory]
    [InlineData("tr")]
    [InlineData("en")]
    public void Her_destek_konusu_sozlukte_cevrilir_ham_kod_basilmaz(string dil)
    {
        foreach (var k in DestekKonulari.Hepsi)
        {
            var ad = DestekSozlugu.KonuAdi(k, dil);
            Assert.NotEqual(k, ad);
            Assert.True(DestekSozlugu.Al(dil).Konular.ContainsKey(k), $"{dil}: {k} karşılıksız");
        }
        Assert.Equal(DestekSozlugu.Al(dil).Konular["DIGER"], DestekSozlugu.KonuAdi("BILINMEYEN", dil));
    }
}
