using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Kur;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S-3e — KUR KAYNAĞI SEÇİCİSİNİN SUNUCU SÖZLEŞMESİ (HTTP).
//
// Ekran seçiciyi GET /api/v1/kur/kaynaklar yanıtından çizer (sabit liste YOK).
// Bu dosya o sözleşmenin alanlarını ve kaynak değiştirme ucunun kapısını zorlar:
//   • GET  /kur/kaynaklar  → kaynaklar[] · varsayilan · secili ·
//                            sozlesmesizKaynaklarAcik · seciliSozlesmesiz · manuelKod
//   • PATCH /kur/kaynak    → tanımsız/geçersiz kod 400 (uydurma kaynak yazılmaz)
//
// DİKKAT: uç PATCH'tir (KurController [HttpPatch("kaynak")]), POST değil.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class S3eKurKaynagiHttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Kaynaklar_ucu_secicinin_TUM_alanlarini_dondurur()
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "S3e kaynak sözleşmesi");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/kur/kaynaklar");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        var g = await cevap.Json();
        Assert.Equal(JsonValueKind.Array, g.GetProperty("kaynaklar").ValueKind);
        Assert.True(g.TryGetProperty("varsayilan", out _));
        Assert.True(g.TryGetProperty("secili", out _));
        // Ekranın kilit/uyarı şeridi bu iki bayrağa bakar (UX-S30).
        Assert.Contains(g.GetProperty("sozlesmesizKaynaklarAcik").ValueKind,
            new[] { JsonValueKind.True, JsonValueKind.False });
        Assert.Contains(g.GetProperty("seciliSozlesmesiz").ValueKind,
            new[] { JsonValueKind.True, JsonValueKind.False });
        Assert.Equal("MANUAL", g.GetProperty("manuelKod").GetString());
    }

    [Fact]
    public async Task Tanimsiz_kaynak_kodu_400_doner_ve_kiraci_ayari_DEGISMEZ()
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "S3e geçersiz kaynak");
        var oncesi = db.Tenants.First(t => t.Id == k.TenantId).KurKaynagi;

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchJson("/api/v1/kur/kaynak", new { kaynak = "YOKBOYLEKAYNAK" });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Contains("kaynak", await cevap.Metin(), StringComparison.OrdinalIgnoreCase);

        using var db2 = f.Vt.OlusturSistem();
        Assert.Equal(oncesi, db2.Tenants.First(t => t.Id == k.TenantId).KurKaynagi);
    }

    [Fact]
    public async Task MANUAL_kodu_kaynak_olarak_SECILEMEZ_400()
    {
        // MANUAL bir ADAPTÖR değildir (elle giriş yolu). Seçiciye de konmaz;
        // sunucu da kabul etmez — iki taraf aynı şeyi söyler.
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "S3e manuel kaynak");

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchJson("/api/v1/kur/kaynak", new { kaynak = "MANUAL" });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-73 — KUR ZAMANLAYICI ARALIĞI (10 → 60 sn, alt sınır 15, sessiz
// sıkıştırma YOK). QA tur 8 ölçümü: 10 sn aralıkla canlı DB'de kiracı başına
// saatte ~340 KurSnapshot yazılıyordu ("değişmedi → yazma" koruması fiyat her
// turda kıpırdadığı için çoğunlukla devreye girmiyor).
//   • Anahtar yoksa ve sevk edilen üç appsettings'te etkin aralık 60 sn.
//   • 15 altı / 3600 üstü / tamsayı olmayan değer AÇILIŞTA anlaşılır hatayla
//     durdurur (Math.Clamp ile sessiz kenetleme YOK).
//   • Etkin aralık açılışta Information seviyesinde loglanır.
//   • 1 saatlik simülasyonda (en kötü durum: her tur fiyat değişir) kiracı
//     başına yazılan KurSnapshot ≤ 60.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class P73KurZamanlayiciTestleri(HttpFikstur f)
{
    private static Microsoft.Extensions.Configuration.IConfiguration Yapilandirma(string? saniye) =>
        new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(saniye is null
                ? new Dictionary<string, string?>()
                : new Dictionary<string, string?> { ["Kur:GuncellemeSaniye"] = saniye })
            .Build();

    private KurZamanlayici Zamanlayici(Microsoft.Extensions.Configuration.IConfiguration y,
        Microsoft.Extensions.Logging.ILogger<KurZamanlayici>? gunluk = null) =>
        new(f.Ortam.Services.GetRequiredService<SistemKapsami>(),
            gunluk ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<KurZamanlayici>.Instance, y);

    [Fact]
    public void KurZamanlayici_VarsayilanAralik_AltmisSaniye()
    {
        // (1) Anahtar hiç yoksa: kod varsayılanı.
        Assert.Equal(60, KurZamanlayici.VarsayilanAralikSaniye);
        Assert.Equal(TimeSpan.FromSeconds(60), KurZamanlayici.AraligiOku(Yapilandirma(null)));
        Assert.Equal(TimeSpan.FromSeconds(60), Zamanlayici(Yapilandirma(null)).EtkinAralik);

        // (2) Sevk edilen yapılandırma: base + ortam katmanı birlikte okunur
        // (Program.cs'in gördüğü sırayla) → etkin aralık 60 sn.
        var api = Path.Combine(CssKaynak.Kok(), "Kuyumcu.Api");
        foreach (var ortam in new[] { "Development", "Production" })
        {
            var y = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddJsonFile(Path.Combine(api, "appsettings.json"))
                .AddJsonFile(Path.Combine(api, $"appsettings.{ortam}.json"))
                .Build();
            Assert.Equal("60", y["Kur:GuncellemeSaniye"]);
            Assert.Equal(TimeSpan.FromSeconds(60), KurZamanlayici.AraligiOku(y));
        }
        var yalinTaban = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddJsonFile(Path.Combine(api, "appsettings.json")).Build();
        Assert.Equal(TimeSpan.FromSeconds(60), KurZamanlayici.AraligiOku(yalinTaban));

        // (3) Kurulumda kopyalanan .ornek şablonları da aynı varsayılanı taşır
        // (kuran kişi eski 10 sn'yi geri getirmesin).
        foreach (var ornek in new[] { "appsettings.Development.json.ornek", "appsettings.Production.json.ornek" })
        {
            var y = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddJsonStream(new MemoryStream(File.ReadAllBytes(Path.Combine(api, ornek)))).Build();
            Assert.Equal("60", y["Kur:GuncellemeSaniye"]);
        }
    }

    [Theory]
    [InlineData("5")]
    [InlineData("14")]
    [InlineData("0")]
    [InlineData("-10")]
    [InlineData("3601")]
    public void KurZamanlayici_AltSinirinAltindaDeger_AcilistaHataVerir(string deger)
    {
        // Sessiz kenetleme YOK: sınır dışı değer 15/3600'e yuvarlanıp çalışmaz,
        // kurucu (= barındırılan hizmetin açılışı) anlaşılır hatayla patlar.
        var hata = Assert.Throws<InvalidOperationException>(() => Zamanlayici(Yapilandirma(deger)));
        Assert.Contains("Kur:GuncellemeSaniye", hata.Message);
        Assert.Contains(deger, hata.Message);
        Assert.Contains("15", hata.Message);
        Assert.Contains("3600", hata.Message);
        Assert.Contains("sessizce sıkıştırılmaz", hata.Message);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("60sn")]
    [InlineData("")]
    [InlineData("60.5")]
    public void KurZamanlayici_OkunamayanDeger_AcilistaHataVerir_VarsayimYapmaz(string deger)
    {
        var hata = Assert.Throws<InvalidOperationException>(() => KurZamanlayici.AraligiOku(Yapilandirma(deger)));
        Assert.Contains("Kur:GuncellemeSaniye", hata.Message);
        Assert.Contains("ayrıştırılamadı", hata.Message);
    }

    [Theory]
    [InlineData("15", 15)]
    [InlineData("60", 60)]
    [InlineData(" 120 ", 120)]
    [InlineData("3600", 3600)]
    public void KurZamanlayici_SinirIcindekiDeger_AynenKullanilir(string deger, int beklenen) =>
        Assert.Equal(TimeSpan.FromSeconds(beklenen), KurZamanlayici.AraligiOku(Yapilandirma(deger)));

    [Fact]
    public async Task KurZamanlayici_AltSinirinAltindaDeger_UygulamaAcilisiniDurdurur()
    {
        // Gerçek barındırma yolu: AddHostedService<KurZamanlayici> + StartAsync.
        // Hizmet başlarken çözülür → kurucu atar → host AÇILMAZ.
        var b = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        b.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Kur:GuncellemeSaniye"] = "5" });
        b.Services.AddSingleton<SistemKapsami>();
        b.Services.AddHostedService<KurZamanlayici>();
        using var host = b.Build();
        var hata = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());
        Assert.Contains("Kur:GuncellemeSaniye", hata.Message);
        Assert.Contains("= 5 sn", hata.Message);
    }

    [Fact]
    public async Task KurZamanlayici_EtkinAralik_Loglanir()
    {
        var gunluk = new YakalayanGunluk();
        var z = Zamanlayici(Yapilandirma(null), gunluk);
        using var iptal = new CancellationTokenSource();
        iptal.Cancel(); // döngü dış ağa/DB'ye çıkmasın: yalnız açılış logu
        await z.StartAsync(iptal.Token);
        await z.StopAsync(CancellationToken.None);

        var kayit = Assert.Single(gunluk.Kayitlar,
            k => k.Seviye == Microsoft.Extensions.Logging.LogLevel.Information
                 && k.Mesaj.Contains("etkin aralık", StringComparison.Ordinal));
        Assert.Contains("etkin aralık: 60 sn", kayit.Mesaj);

        // Yapılandırılmış değer de logda aynen görünür.
        var gunluk2 = new YakalayanGunluk();
        var z2 = Zamanlayici(Yapilandirma("90"), gunluk2);
        await z2.StartAsync(iptal.Token);
        await z2.StopAsync(CancellationToken.None);
        Assert.Contains(gunluk2.Kayitlar, k => k.Seviye == Microsoft.Extensions.Logging.LogLevel.Information
                                               && k.Mesaj.Contains("etkin aralık: 90 sn"));
    }

    [Fact]
    public async Task KurZamanlayici_BirSaatlikSimulasyon_KiraciBasinaEnFazla60Snapshot()
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "P73 bir saat simülasyonu");
        // KiraciKur fikstürü başlangıç kuru tohumlar; simülasyon yalnız ARTIŞI sayar.
        var oncesi = db.KurSnapshots.Count(s => s.TenantId == k.TenantId);
        var aralik = Zamanlayici(Yapilandirma(null)).EtkinAralik;
        var kapsam = f.Ortam.Services.GetRequiredService<SistemKapsami>();

        // ExecuteAsync döngüsünün birebir zaman çizelgesi: tur → Delay(aralık).
        // EN KÖTÜ DURUM: her turda fiyat değişir ("değişmedi → yazma" koruması
        // hiç devreye girmez — canlıda 10 sn'de ölçülen tablo buydu).
        var tur = 0;
        for (var t = TimeSpan.Zero; t < TimeSpan.FromHours(1); t += aralik)
        {
            await using var kk = kapsam.Kiraci(k.TenantId);
            var sonuc = await kk.ServiceProvider.GetRequiredService<KurServisi>()
                .OtomatikGuncelle(k.TenantId,
                    // 17.10 P0-3: değerler fikstür kurunun (6.827,20) YAKININDA — devre kesici gerçekçi
                    // olmayan sıçramayı (eskiden 3.000 = %56 düşüş) yazmaz; bu test yalnız tur sayısını ölçer.
                    new DisKurVerisi(6_827m + tur, 6_861m + tur, 34m, 37m, 43m, "ALTINKAYNAK"));
            Assert.NotNull(sonuc);
            tur++;
        }

        using var db2 = f.Vt.OlusturSistem();
        var yazilan = db2.KurSnapshots.Count(s => s.TenantId == k.TenantId) - oncesi;
        Assert.Equal(tur, yazilan);          // her tur gerçekten yazdı (en kötü durum)
        Assert.True(yazilan <= 60, $"1 saatte {yazilan} KurSnapshot yazıldı (sınır 60)");
    }

    private sealed class YakalayanGunluk : Microsoft.Extensions.Logging.ILogger<KurZamanlayici>
    {
        public List<(Microsoft.Extensions.Logging.LogLevel Seviye, string Mesaj)> Kayitlar { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Kayitlar.Add((logLevel, formatter(state, exception)));
    }
}
