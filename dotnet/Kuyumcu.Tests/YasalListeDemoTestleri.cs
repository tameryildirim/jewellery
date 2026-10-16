using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MMasak = Kuyumcu.Api.Modules.Masak;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-LISTE-DEMO — Panel 'Yasal liste henüz yüklenmedi' bandı.
//
// Kök neden: yaptirim_kayitlari KİRACI-ÜSTÜDÜR ve tek yazma yolu
// POST /sistem/yaptirim-listesi'dir; demo/geliştirme veritabanında bu uç hiç
// çağrılmadığı için tablo boştur → Ozet fail-closed "hiç yüklenmedi" der (DOĞRU).
// Bu testler: (1) eskilik kuralı saf ve fail-closed; (2) liste yalnız birincil
// kaynak konağından yüklenir (uydurma liste girmez); (3) sistemde yüklü liste
// SONRADAN açılan kiracıya da görünür (bant çıkmaz) ve tarama o kiracıda da
// aynen çalışır; (4) demo betiğinin önbellek doğrulaması sunucuyla aynı konakları
// kabul eder.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class YasalListeDemoSafTestleri
{
    private static readonly DateOnly Bugun = new(2026, 10, 2);

    [Fact]
    public void Hic_yuklenmemis_liste_eski_sayilir_fail_closed()
    {
        var (gun, eski) = MMasak.YaptirimServisi.ListeDurumu(null, Bugun);
        Assert.Null(gun);
        Assert.True(eski);
    }

    [Fact]
    public void Ondort_gunluk_liste_guncel_onbes_gunluk_eski()
    {
        Assert.Equal((14, false), MMasak.YaptirimServisi.ListeDurumu(Bugun.AddDays(-14), Bugun));
        Assert.Equal((15, true), MMasak.YaptirimServisi.ListeDurumu(Bugun.AddDays(-15), Bugun));
        Assert.Equal((0, false), MMasak.YaptirimServisi.ListeDurumu(Bugun, Bugun));
    }

    [Theory]
    [InlineData("https://masak.hmb.gov.tr/bkk-ile-malvarliklari-dondurulanlar", true)]
    [InlineData("https://www.resmigazete.gov.tr/eskiler/2026/01/x.pdf", true)]
    [InlineData("https://scsanctions.un.org/resources/xml/en/consolidated.xml", true)]
    [InlineData("http://masak.hmb.gov.tr/liste", false)]
    [InlineData("https://example.com/liste", false)]
    [InlineData("https://masak.hmb.gov.tr.ornek.com/liste", false)]
    [InlineData("masak.hmb.gov.tr/liste", false)]
    [InlineData(null, false)]
    public void Birincil_kaynak_konagi(string? url, bool beklenen) =>
        Assert.Equal(beklenen, MMasak.YaptirimServisi.BirincilKaynakMi(url));
}

[Collection("http")]
public sealed class YasalListeDemoHttpTestleri(HttpFikstur f)
{
    private static string Bugun => YasalParametreServisi.IslemGunu(DateTime.UtcNow).ToString("yyyy-MM-dd");

    private Task<HttpResponseMessage> Yukle(string kaynakUrl, string ad, string? tckn) =>
        f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari).PostAsJsonAsync("/api/v1/sistem/yaptirim-listesi", new
        {
            kaynak = "Test listesi (6415 s. K. md.5)",
            kaynakUrl,
            listeTarihi = Bugun,
            ekle = new[] { new { adSoyad = ad, kimlikNo = tckn, rgTarihi = (string?)null } },
        });

    /// <summary>Birincil olmayan kaynak adresi 400 ve tabloya satır EKLENMEZ.</summary>
    [Fact]
    public async Task Birincil_olmayan_kaynak_400_satir_eklenmez()
    {
        var ad = "UYDURMA KAYNAK " + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        var r = await Yukle("https://example.com/yaptirim", ad, null);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var govde = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("birincil kaynak", govde.GetProperty("message").GetString());
        using var kontrol = f.Vt.OlusturSistem();
        Assert.Equal(0, await kontrol.YaptirimKayitlari.AsNoTracking().CountAsync(y => y.AdSoyad == ad));
    }

    /// <summary>
    /// Sistemde liste yüklendikten SONRA açılan kiracı listeyi görür: uyum-ozet
    /// listeEski=false, liste tarihi bugün, kaynak birincil (Panel bandı çıkmaz).
    /// </summary>
    [Fact]
    public async Task Yuklu_sistem_listesi_yeni_kiraciya_gorunur_bant_cikmaz()
    {
        var r = await Yukle("https://masak.hmb.gov.tr/bkk-ile-malvarliklari-dondurulanlar",
            "YENI KIRACI GORUR " + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(), null);
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode}: {await r.Content.ReadAsStringAsync()}");

        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETYASAL-LISTE-DEMO-TEST-YENI");
        var ozet = await (await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/masak/uyum-ozet")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(ozet.GetProperty("listeEski").GetBoolean());
        Assert.Equal(0, ozet.GetProperty("listeYasiGun").GetInt32());

        var yaptirim = await (await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/masak/yaptirim")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(Bugun, yaptirim.GetProperty("listeTarihi").GetString());
        Assert.True(MMasak.YaptirimServisi.BirincilKaynakMi(yaptirim.GetProperty("kaynakUrl").GetString()));
    }

    /// <summary>
    /// TUR 2 (hakem bulgusu): liste HİÇ yokken servis özeti listeTarihi=null, listeYasiGun=null,
    /// listeEski=TRUE döner (fail-closed). Tablo kiracı-üstü ve test veritabanında diğer
    /// testlerce dolu olduğundan "liste yok" kurgusu bir işlem (transaction) içinde tabloyu
    /// boşaltıp özeti AYNI bağlantıda okuyarak kurulur; sonda geri alınır (satır kalıcı silinmez).
    /// </summary>
    [Fact]
    public async Task Liste_yokken_ozet_listeEski_true_tarih_null_fail_closed()
    {
        Kiraci k;
        using (var kur = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(kur, "PAKETYASAL-LISTE-DEMO-TEST-BOS");
        using var db = f.Vt.Olustur(k);
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("DELETE FROM yaptirim_kayitlari");
        try
        {
            Assert.Equal(0, await db.YaptirimKayitlari.AsNoTracking().CountAsync());
            var ozet = JsonSerializer.SerializeToElement(await MMasak.YaptirimServisi.Ozet(db, k.TenantId));
            Assert.Equal(JsonValueKind.Null, ozet.GetProperty("listeTarihi").ValueKind);
            Assert.Equal(JsonValueKind.Null, ozet.GetProperty("listeYasiGun").ValueKind);
            Assert.True(ozet.GetProperty("listeEski").GetBoolean(), "liste yokken listeEski=false (fail-open)");
            Assert.Equal(0, ozet.GetProperty("yururlukteSatir").GetInt32());
        }
        finally { await tx.RollbackAsync(); }
    }

    /// <summary>
    /// TUR 2 (hakem bulgusu): eskilik bayrağı İKİ UCA da taşınır — liste yüklü ama
    /// EskiListeGun'den yaşlıyken (ileri tarihli gün dikişi) GET /masak/yaptirim ve
    /// GET /masak/uyum-ozet listeEski=TRUE ve yaş > eşik döner (Panel bandı görünür).
    /// Dikişsiz aynı kiracıda bayrak false (liste bugünkü) — fark yalnız gündür.
    /// </summary>
    [Fact]
    public async Task Eski_liste_bayragi_yaptirim_ve_uyum_ozet_ucuna_tasinir()
    {
        var r = await Yukle("https://masak.hmb.gov.tr/bkk-ile-malvarliklari-dondurulanlar",
            "ESKILIK UCU " + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(), null);
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode}: {await r.Content.ReadAsStringAsync()}");
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETYASAL-LISTE-DEMO-TEST-ESKI");

        var ileri = YasalParametreServisi.IslemGunu(DateTime.UtcNow).AddDays(3650);
        using var ortam = f.Ortam.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.AddSingleton(new MMasak.YaptirimServisi.YaptirimOzetGunu(ileri))));
        var c = ortam.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new TokenServisi(new JwtAyarlar
        {
            Secret = HttpOrtami.TestSirri, AccessDakika = 15, RefreshGun = 7,
        }).AccessUret(k.KullaniciId, k.TenantId, k.SubeId, "SAHIP"));

        foreach (var yol in new[] { "/api/v1/masak/yaptirim", "/api/v1/masak/uyum-ozet" })
        {
            var yanit = await c.GetAsync(yol);
            Assert.True(yanit.IsSuccessStatusCode, $"{yol} {(int)yanit.StatusCode}");
            var g = await yanit.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(g.GetProperty("listeEski").GetBoolean(), $"{yol}: eski liste listeEski=false döndü (fail-open)");
            Assert.True(g.GetProperty("listeYasiGun").GetInt32() > MMasak.YaptirimServisi.EskiListeGun, yol);
            Assert.NotEqual(JsonValueKind.Null, g.GetProperty("listeTarihi").ValueKind);
        }
        var bugunku = await (await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/masak/uyum-ozet")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(bugunku.GetProperty("listeEski").GetBoolean());
    }

    /// <summary>Tarama DEĞİŞMEDİ: listeden sonra açılan kiracıda listedeki TCKN ile satış 400 YAPTIRIM_ENGEL.</summary>
    [Fact]
    public async Task Yeni_kiracida_tarama_aynen_calisir_tam_eslesme_400()
    {
        var tc = Yasal3Yardim.Tckn();
        var r = await Yukle("https://scsanctions.un.org/resources/xml/en/consolidated.xml", "Liste Kisi " + tc, tc);
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode}: {await r.Content.ReadAsStringAsync()}");

        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETYASAL-LISTE-DEMO-TEST-TARAMA");
        var s = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/islemler/satis", new
        {
            cariId = k.CariId, urunId = k.UrunId, adet = 1m, birimFiyatTl = 200_000m,
            odemeTuru = "HAVALE", kasaId = k.KasaId, kurSnapshotId = k.KurId, masak = Yasal3Yardim.Kimlik(tc, "Başka Yazım"),
        });
        Assert.Equal(HttpStatusCode.BadRequest, s.StatusCode);
        var g = await s.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(MMasak.YaptirimServisi.EngelKodu, g.GetProperty("kod").GetString());
        using var kontrol = f.Vt.OlusturSistem();
        Assert.Equal(0, kontrol.Hareketler.Count(h => h.TenantId == k.TenantId));
    }
}

/// <summary>Demo betiğinin önbellek doğrulaması — gerçek node sürecinde (ağ yok).</summary>
public sealed class YasalListeDemoBetikTestleri
{
    private static string BetikYolu()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        Assert.NotNull(d);
        return Path.Combine(Directory.GetParent(d!.FullName)!.FullName,
            "dokumanlar", "09-uygulama-kayitlari", "betikler", "demo-tohumla.js");
    }

    private static string Node(string js)
    {
        var psi = new ProcessStartInfo("node") { RedirectStandardOutput = true, RedirectStandardError = true };
        if (!File.Exists("/usr/bin/node") && File.Exists("/opt/homebrew/bin/node")) psi.FileName = "/opt/homebrew/bin/node";
        psi.ArgumentList.Add("-e"); psi.ArgumentList.Add(js); psi.ArgumentList.Add(BetikYolu());
        using var p = Process.Start(psi)!;
        var cikti = p.StandardOutput.ReadToEnd(); var hata = p.StandardError.ReadToEnd();
        p.WaitForExit();
        Assert.True(p.ExitCode == 0, $"node çıkış {p.ExitCode}: {hata}");
        return cikti;
    }

    private static List<string> Dogrula(string govdeJs) => JsonSerializer.Deserialize<List<string>>(
        Node("const m=require(process.argv[1]);process.stdout.write(JSON.stringify(m.yasalListeOnbellekDogrula(" + govdeJs + ")));"))!;

    private const string Saglam = "{kaynak:'BMGK konsolide listesi',kaynakUrl:'https://scsanctions.un.org/resources/xml/en/consolidated.xml',listeTarihi:'2026-10-01',ekle:[{adSoyad:'X'}]}";

    [Fact]
    public void Birincil_kaynakli_onbellek_kabul() => Assert.Empty(Dogrula(Saglam));

    [Fact]
    public void Birincil_olmayan_konak_reddedilir() =>
        Assert.Contains(Dogrula(Saglam.Replace("scsanctions.un.org", "ornek.com")), e => e.Contains("birincil kaynak değil"));

    [Fact]
    public void Satirsiz_onbellek_reddedilir() =>
        Assert.Contains(Dogrula(Saglam.Replace("ekle:[{adSoyad:'X'}]", "ekle:[]")), e => e.Contains("ekle boş"));

    private static string Durum(string ozetJs) => Node(
        "const m=require(process.argv[1]);try{process.stdout.write(m.yasalListeDurumu(" + ozetJs + "))}catch(e){process.stdout.write('HATA:'+e.message)}");

    /// <summary>TUR 2: betik sunucunun listeEski bayrağına körü körüne güvenmez (fail-closed).</summary>
    [Fact]
    public void Betik_liste_tarihi_yoksa_yuklenmedi_der_fail_open_yaniti_hata()
    {
        Assert.Equal("YUKLENMEDI", Durum("{listeTarihi:null,listeEski:true}"));
        Assert.Equal("YUKLENMEDI", Durum("null"));
        Assert.StartsWith("HATA:TUTARSIZ", Durum("{listeTarihi:null,listeEski:false}"));
        Assert.Equal("SISTEMDE_GUNCEL", Durum("{listeTarihi:'2026-10-01',listeEski:false}"));
        Assert.Equal("ESKI", Durum("{listeTarihi:'2026-09-01',listeEski:true}"));
    }

    [Fact]
    public void Betik_konak_listesi_sunucuyla_ayni() =>
        Assert.Equal(MMasak.YaptirimServisi.BirincilKaynakKonaklari,
            JsonSerializer.Deserialize<List<string>>(Node("process.stdout.write(JSON.stringify(require(process.argv[1]).BIRINCIL_KONAKLAR));"))!);
}
