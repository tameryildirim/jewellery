using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Kuyumcu.Core.Domain;
using Kuyumcu.Web.Bilesenler.Grid;
using Xunit;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET GRID-1 SINIR TESTLERİ (gerileme)
// Ortak ızgaranın tür-biçimli hücresi (IzgaraKolonu.BicimliDeger: Tl/Has/Gram) para
// ve has GÖSTERİR → sıfır · eksi · kuruş yuvarlaması (AwayFromZero, 0,005) · has 4 hane ·
// çok büyük tutar · null. Beklenen değerler SABİT metindir (üretim kodundan hesaplanmaz).
// Ayrıca tercih kuralının sayısal sınırları (sürüm 1–1000, kolon 60, değer 2000).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GRID1SinirTestleri
{
    private sealed record Satir(decimal? D);

    private static string Bicimle(IzgaraHucreTuru tur, decimal? d)
    {
        var k = new IzgaraKolonu<Satir> { Anahtar = "x", Baslik = "X", Tur = tur, Deger = s => s.D };
        var m = typeof(IzgaraKolonu<Satir>).GetMethod("BicimliDeger", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (string)m.Invoke(k, [new Satir(d)])!;
    }

    // NBSP/dar boşluk farkını yok say: ölçülen şey rakam ve ayraç dilidir.
    private static string N(string s) => s.Replace(' ', ' ').Replace(' ', ' ');

    [Fact] public void Tl_sifir() => Assert.Equal("0 ₺", N(Bicimle(IzgaraHucreTuru.Tl, 0m)));
    [Fact] public void Tl_eksi_isaret_korunur() => Assert.Contains("1.234,50", N(Bicimle(IzgaraHucreTuru.Tl, -1234.5m)));
    [Fact] public void Tl_eksi_isaretli() => Assert.Contains("-", N(Bicimle(IzgaraHucreTuru.Tl, -1234.5m)).Replace("−", "-"));
    [Fact] public void Tl_kurus_0_005_yukari_yuvarlanir() => Assert.Equal("0,01 ₺", N(Bicimle(IzgaraHucreTuru.Tl, 0.005m)));
    [Fact] public void Tl_kurus_0_004_asagi_yuvarlanir() => Assert.Equal("0 ₺", N(Bicimle(IzgaraHucreTuru.Tl, 0.004m)));
    [Fact] public void Tl_cok_buyuk() => Assert.Equal("999.999.999,99 ₺", N(Bicimle(IzgaraHucreTuru.Tl, 999_999_999.99m)));
    [Fact] public void Tl_null_tire() => Assert.Equal("—", Bicimle(IzgaraHucreTuru.Tl, null));
    [Fact] public void Has_4_hane() => Assert.Equal("0,0001 has", N(Bicimle(IzgaraHucreTuru.Has, 0.0001m)));
    [Fact] public void Has_sifir() => Assert.Equal("0,0000 has", N(Bicimle(IzgaraHucreTuru.Has, 0m)));
    [Fact] public void Gram_3_hane() => Assert.Equal("1,500 g", N(Bicimle(IzgaraHucreTuru.Gram, 1.5m)));
    [Fact] public void TRY_kalibi_yok() => Assert.DoesNotContain("TRY", Bicimle(IzgaraHucreTuru.Tl, 6754.91m));

    [Theory]
    [InlineData("""{"s":0,"k":[],"g":[],"o":null}""", false)]
    [InlineData("""{"s":1,"k":[],"g":[],"o":null}""", true)]
    [InlineData("""{"s":1000,"k":[],"g":[],"o":null}""", true)]
    [InlineData("""{"s":1001,"k":[],"g":[],"o":null}""", false)]
    [InlineData("""{"s":-1,"k":[],"g":[],"o":null}""", false)]
    [InlineData("""{"s":1.5,"k":[],"g":[],"o":null}""", false)]
    [InlineData("""{"s":"1","k":[],"g":[],"o":null}""", false)]
    [InlineData("", false)]
    [InlineData("null", false)]
    public void Surum_sinirlari(string deger, bool gecerli) =>
        Assert.Equal(gecerli, IzgaraTercihKurali.Coz(deger, out _) is not null);

    [Fact]
    public void Kolon_sayisi_60_gecer_61_gecmez()
    {
        string Uret(int n) => "{\"s\":1,\"k\":[" + string.Join(",", Enumerable.Range(1, n).Select(i => $"\"k{i}\"")) + "]}";
        Assert.NotNull(IzgaraTercihKurali.Coz(Uret(60), out _));
        Assert.Null(IzgaraTercihKurali.Coz(Uret(61), out var h));
        Assert.Contains("60", h);
    }

    [Theory]
    [InlineData("grid:cariler:cari-liste", true)]
    [InlineData("GRID:cariler:cari-liste", false)]
    [InlineData("grid:cariler:cari liste", false)]
    [InlineData("grid:cariler:", false)]
    [InlineData("grid:cariler:cari-liste:x", false)]
    [InlineData(null, false)]
    public void Anahtar_sinirlari(string? anahtar, bool gecerli) =>
        Assert.Equal(gecerli, IzgaraTercihKurali.AnahtarGecerli(anahtar));
}

// ── Sunucu sıralaması (/cari?sirala=tl-bakiye) para sınırları ─────────────────
// ⚠ Not (tur 2 doğrulayıcı): bu sınıf, önceki doğrulayıcının aynı adlı GS01/GS02
//   testlerinin yerine YENİDEN yazıldı (dosya üzerine yazıldı; içerik ad ve
//   kapsamıyla yeniden kuruldu). Beklenen sıralar SABİT listedir.
[Collection("http")]
public sealed class GRID1SiralamaSinirTestleri(HttpFikstur f)
{
    private async Task<HttpClient> Kur(string kiraciAd, Dictionary<string, decimal?> bakiye)
    {
        Kiraci k;
        using (var sdb = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(sdb, kiraciAd);
        var c = f.Ortam.Istemci(k);
        foreach (var ad in bakiye.Keys)
            Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/v1/cari", new { tur = "MUSTERI", ad })).StatusCode);
        using var db = f.Vt.Olustur(k.TenantId);
        foreach (var cari in await db.Cariler.Where(x => x.Ad.StartsWith("GSINIR")).ToListAsync())
        {
            var b = await db.CariBakiyeler.SingleAsync(x => x.CariId == cari.Id);
            if (bakiye[cari.Ad] is { } v) b.TlBakiye = v; else db.CariBakiyeler.Remove(b);
        }
        await db.SaveChangesAsync();
        return c;
    }

    private static async Task<List<string>> Adlar(HttpClient c, string yon)
    {
        var r = await c.GetAsync($"/api/v1/cari?q=GSINIR&limit=100&sirala=tl-bakiye&yon={yon}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return [.. JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.EnumerateArray().Select(e => e.GetProperty("ad").GetString()!)];
    }

    [Fact]
    public async Task GS01_Sifir_eksi_kurus_farki_ve_cok_buyuk_tutar_dogru_siralanir()
    {
        var c = await Kur("PAKETGRID-1-TEST GS01", new()
        {
            ["GSINIR Buyuk"] = 999_999_999.99m, ["GSINIR Eksi"] = -0.01m, ["GSINIR Sifir"] = 0m,
            ["GSINIR KurusA"] = 0.01m, ["GSINIR KurusB"] = 0.02m, ["GSINIR EksiBuyuk"] = -999_999_999.99m,
        });
        Assert.Equal(["GSINIR EksiBuyuk", "GSINIR Eksi", "GSINIR Sifir", "GSINIR KurusA", "GSINIR KurusB", "GSINIR Buyuk"], await Adlar(c, "artan"));
        Assert.Equal(["GSINIR Buyuk", "GSINIR KurusB", "GSINIR KurusA", "GSINIR Sifir", "GSINIR Eksi", "GSINIR EksiBuyuk"], await Adlar(c, "azalan"));
    }

    [Fact]
    public async Task GS02_Esit_bakiyede_ad_ile_kararli_ve_bakiye_satiri_yoksa_sifir_sayilir()
    {
        var c = await Kur("PAKETGRID-1-TEST GS02", new()
        {
            ["GSINIR Cem"] = 5m, ["GSINIR Ali"] = 5m, ["GSINIR Satirsiz"] = null, ["GSINIR Eksi"] = -1m,
        });
        Assert.Equal(["GSINIR Eksi", "GSINIR Satirsiz", "GSINIR Ali", "GSINIR Cem"], await Adlar(c, "artan"));
        Assert.Equal(["GSINIR Ali", "GSINIR Cem", "GSINIR Satirsiz", "GSINIR Eksi"], await Adlar(c, "azalan"));
    }
}

// ── Doğrulayıcı tur 4 (08.10 06:5x) — ek sınırlar; beklenenler SABİT metin ─────
public sealed class GRID1Tur4SinirTestleri
{
    private sealed record Satir(object? D);

    private static string Bicimle(IzgaraHucreTuru tur, object? d, string? pb = null)
    {
        var k = new IzgaraKolonu<Satir> { Anahtar = "x", Baslik = "X", Tur = tur, Deger = s => s.D, ParaBirimi = _ => pb };
        var m = typeof(IzgaraKolonu<Satir>).GetMethod("BicimliDeger", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return System.Text.RegularExpressions.Regex.Replace((string)m.Invoke(k, [new Satir(d)])!, "[\u00A0\u202F]", " ");
    }

    [Fact] public void T4_Tl_eksi_kurus_0_005_AwayFromZero() => Assert.Equal("-0,01 ₺", Bicimle(IzgaraHucreTuru.Tl, -0.005m).Replace("−", "-"));
    [Fact] public void T4_Tl_999_999_999_995_yukari() => Assert.Equal("1.000.000.000 ₺", Bicimle(IzgaraHucreTuru.Tl, 999_999_999.995m));
    [Fact] public void T4_Has_0_00005_dort_haneye_yukari() => Assert.Equal("0,0001 has", Bicimle(IzgaraHucreTuru.Has, 0.00005m));
    [Fact] public void T4_Has_eksi_4_hane() => Assert.Equal("-1,2346 has", Bicimle(IzgaraHucreTuru.Has, -1.23456m).Replace("−", "-"));
    [Fact] public void T4_Gram_sifir() => Assert.Equal("0,000 g", Bicimle(IzgaraHucreTuru.Gram, 0m));
    [Fact] public void T4_Para_USD_buyuk() => Assert.Equal("999.999.999,99 $", Bicimle(IzgaraHucreTuru.Para, 999_999_999.99m, "USD"));
    [Fact] public void T4_Para_null_deger_tire() => Assert.Equal("—", Bicimle(IzgaraHucreTuru.Para, null, "USD"));

    [Fact]
    public void T4_Deger_2000_Turkce_karakter_gecer_2001_gecmez()
    {
        // 2000 KARAKTER sınırı (bayt değil): ğ/ş çok baytlı olsa da karakter sayılır.
        var taban = """{"s":1,"k":[],"g":[],"o":null}""";
        string Uret(int n) => taban[..^1] + new string(' ', n - taban.Length) + "}";
        Assert.NotNull(IzgaraTercihKurali.Coz(Uret(2000), out _));
        Assert.Null(IzgaraTercihKurali.Coz(Uret(2001), out var h));
        Assert.Contains("2000", h);
    }

    [Fact]
    public void T4_Yinelenen_kolon_g_icinde_ve_bos_sira_reddi()
    {
        Assert.Null(IzgaraTercihKurali.Coz("""{"s":1,"k":[],"g":["has","has"],"o":null}""", out _));
        Assert.Null(IzgaraTercihKurali.Coz("""{"s":1,"o":{"a":"","y":"artan"}}""", out _));
        Assert.Null(IzgaraTercihKurali.Coz("""{"s":1,"o":{"a":"ad"}}""", out _));
    }

    [Fact]
    public void T4_Zorunlu_kolon_gizlenemez_ve_eylem_sonda_kalir()
    {
        var tanim = new List<IzgaraKolonTanimi>
        {
            new("ad", true, true, false, true), new("tel", false, true, false, true),
            new("has", false, true, false, false), new("eylem", true, true, true, false),
        };
        var d = IzgaraDuzeni.Kur(tanim, new IzgaraTercihi(1, ["eylem", "has", "tel", "ad"], ["ad", "eylem", "tel"], new("has", "artan")));
        Assert.Equal(["has", "tel", "ad", "eylem"], d.Sira);
        Assert.Equal(["tel"], d.Gizli.OrderBy(x => x));
        Assert.Null(d.Siralama);                       // has sıralanamaz → kayıtlı sıralama yok sayılır
        Assert.Null(d.GizleDegistir("ad"));
        Assert.Null(d.Tasi("ad", 1));                  // ad sonda (eylem taşınamaz) → ötesine geçemez
    }
}
