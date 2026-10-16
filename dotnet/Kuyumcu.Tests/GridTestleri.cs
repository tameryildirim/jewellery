using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Tercih;
using Kuyumcu.Core.Domain;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Grid;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SayfaCariler = Kuyumcu.Web.Pages.Cariler;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET GRID-1 — ORTAK IZGARA + KİŞİSEL KALICI DÜZEN
//
// Envanter GIRDI-KURALLARI-ENVANTER-2026-10-08 § (E) P2 + § (G). Dört katman:
//   (1) SAF  — IzgaraTercihKurali (anahtar deseni, değer biçimi, boyut) ve
//              IzgaraDuzeni (sıra, gizli, sıralama döngüsü, sürüm dayanıklılığı)
//   (2) HTTP — /tercihler/benim grid anahtarı: yaz/oku, 400'ler, 2000 karakter,
//              kullanıcı başına 200 anahtar, başkasının tercihi okunamaz/yazılamaz;
//              /cari?sirala=&yon= SUNUCU sıralaması + beyaz liste 400
//   (3) bUnit — /cariler listesi ortak ızgarada: varsayılan, kayıtlı düzen, bozuk/eski
//              tercih → varsayılan (hata yok), panel (taşı/gizle/zorunlu kilit),
//              başlık sıralaması sunucuya gider, klavye Alt+→, varsayılana dön,
//              yazma hatası geri alınır + bant, her düğme ya istek atar ya kilitli.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GridKuralTestleri
{
    private const string Gecerli = """{"s":1,"k":["telefon","ad"],"g":["has"],"o":{"a":"ad","y":"azalan"}}""";

    [Theory]
    [InlineData("grid:cariler:cari-liste", true)]
    [InlineData("grid:cariler:x", true)]
    [InlineData("grid:bilinmeyen:cari-liste", false)]     // bilinen ekran listesinde değil
    [InlineData("grid:Cariler:cari-liste", false)]        // büyük harf desen dışı
    [InlineData("grid:cariler:cari liste", false)]
    [InlineData("grid:cariler", false)]
    [InlineData("grid::cari-liste", false)]
    [InlineData("menu-sirasi", false)]
    public void GK01_Anahtar_deseni_ve_bilinen_ekran(string anahtar, bool beklenen) =>
        Assert.Equal(beklenen, IzgaraTercihKurali.AnahtarGecerli(anahtar));

    [Fact]
    public void GK02_Anahtar_sutun_genisligi_60_asilamaz()
    {
        var a60 = "grid:cariler:" + new string('a', 60 - "grid:cariler:".Length);
        Assert.Equal(60, a60.Length);
        Assert.True(IzgaraTercihKurali.AnahtarGecerli(a60));
        Assert.False(IzgaraTercihKurali.AnahtarGecerli(a60 + "a"));
    }

    [Fact]
    public void GK03_Gecerli_deger_cozulur_ve_yazim_gidis_donus_ayni()
    {
        var t = IzgaraTercihKurali.Coz(Gecerli, out var hata);
        Assert.Null(hata);
        Assert.NotNull(t);
        Assert.Equal(1, t!.S);
        Assert.Equal(["telefon", "ad"], t.K);
        Assert.Equal(["has"], t.G);
        Assert.Equal(new IzgaraSiralamaTercihi("ad", "azalan"), t.O);
        var yaz = IzgaraTercihKurali.Yaz(t);
        var geri = IzgaraTercihKurali.Coz(yaz, out _);
        Assert.Equal(t.K, geri!.K);
        Assert.Equal(t.G, geri.G);
        Assert.Equal(t.O, geri.O);
    }

    [Theory]
    [InlineData("{bozuk")]
    [InlineData("[]")]
    [InlineData("""{"k":["ad"]}""")]                                   // sürüm yok
    [InlineData("""{"s":0,"k":[]}""")]                                  // sürüm < 1
    [InlineData("""{"s":"1","k":[]}""")]                                // sürüm metin
    [InlineData("""{"s":1,"k":["ad","ad"]}""")]                         // yinelenen
    [InlineData("""{"s":1,"k":["Ad"]}""")]                              // desen dışı kolon
    [InlineData("""{"s":1,"k":"ad"}""")]                                // dizi değil
    [InlineData("""{"s":1,"o":{"a":"ad","y":"yukari"}}""")]             // geçersiz yön
    [InlineData("""{"s":1,"o":{"a":"ad"}}""")]                          // yön yok
    [InlineData("""{"s":1,"x":1}""")]                                   // bilinmeyen alan
    public void GK04_Bicim_disi_deger_reddedilir_sebep_Turkce(string deger)
    {
        Assert.Null(IzgaraTercihKurali.Coz(deger, out var hata));
        Assert.False(string.IsNullOrWhiteSpace(hata));
    }

    [Fact]
    public void GK05_Boyut_siniri_2000_tam_sinir_gecer_bir_fazlasi_gecmez()
    {
        static string Uret(int uzunluk)
        {
            // {"s":1,"k":[],"g":[],"o":null} + boşluk dolgusu (JSON boşluğu geçerlidir)
            var taban = """{"s":1,"k":[],"g":[],"o":null}""";
            return taban[..^1] + new string(' ', uzunluk - taban.Length) + "}";
        }
        Assert.Equal(2000, Uret(2000).Length);
        Assert.NotNull(IzgaraTercihKurali.Coz(Uret(2000), out _));
        Assert.Null(IzgaraTercihKurali.Coz(Uret(2001), out var hata));
        Assert.Contains("2000", hata);
    }
}

public sealed class GridDuzenTestleri
{
    private static readonly IReadOnlyList<IzgaraKolonTanimi> Tanim =
    [
        new("ad", Zorunlu: true, VarsayilanGorunur: true, Tasinamaz: false, Siralanabilir: true),
        new("tur", false, true, false, true),
        new("telefon", false, true, false, true),
        new("tl-bakiye", true, true, false, true),
        new("has", false, true, false, false),
        new("not", false, false, false, false),     // varsayılan GİZLİ
        new("eylem", true, true, true, false),
    ];

    [Fact]
    public void GD01_Tercih_yokken_varsayilan_sira_ve_varsayilan_gizli()
    {
        var d = IzgaraDuzeni.Varsayilan(Tanim);
        Assert.Equal(["ad", "tur", "telefon", "tl-bakiye", "has", "not", "eylem"], d.Sira);
        Assert.Equal(["not"], d.Gizli.ToList());
        Assert.Null(d.Siralama);
        Assert.True(d.VarsayilanMi);
    }

    [Fact]
    public void GD02_Kayitli_sira_gizli_ve_siralama_uygulanir_eylem_hep_sonda()
    {
        var t = new IzgaraTercihi(1, ["telefon", "ad", "has", "tur", "tl-bakiye", "not"], ["has"], new("tl-bakiye", "azalan"));
        var d = IzgaraDuzeni.Kur(Tanim, t);
        Assert.Equal(["telefon", "ad", "has", "tur", "tl-bakiye", "not", "eylem"], d.Sira);
        Assert.Equal(["telefon", "ad", "tur", "tl-bakiye", "not", "eylem"], d.Gorunen);   // not: tercihte görünür
        Assert.Equal("tl-bakiye", d.Siralama!.A);
    }

    [Fact]
    public void GD03_Yeni_kolon_varsayilan_yerine_eklenir_olmayan_kolon_yok_sayilir()
    {
        // Tercih yazıldığında "tur" ve "not" yoktu; "silinmis" artık yok.
        var t = new IzgaraTercihi(1, ["telefon", "silinmis", "ad", "tl-bakiye", "has"], ["silinmis"], null);
        var d = IzgaraDuzeni.Kur(Tanim, t);
        // tur → varsayılanda öncülü "ad" → ad'ın arkasına; not → öncülü "has" → has'ın arkasına.
        Assert.Equal(["telefon", "ad", "tur", "tl-bakiye", "has", "not", "eylem"], d.Sira);
        Assert.DoesNotContain("silinmis", d.Sira);
        Assert.Contains("not", d.Gizli);          // yeni kolon VARSAYILAN görünürlüğüyle (gizli)
        Assert.DoesNotContain("tur", d.Gizli);
    }

    [Fact]
    public void GD04_Zorunlu_kolon_tercihte_gizli_olsa_bile_gorunur()
    {
        var d = IzgaraDuzeni.Kur(Tanim, new IzgaraTercihi(1, [], ["ad", "tl-bakiye", "eylem", "has"], null));
        // ad / tl-bakiye / eylem zorunlu → gizlenmez; "not" tercihte yok ve varsayılanı gizli.
        Assert.True(d.Gizli.SetEquals(["has", "not"]), string.Join(",", d.Gizli));
        Assert.Null(d.GizleDegistir("ad"));
        Assert.Null(d.GizleDegistir("eylem"));
    }

    [Fact]
    public void GD05_Surum_uyusmazsa_varsayilan_duzen()
    {
        var t = new IzgaraTercihi(2, ["telefon", "ad"], ["has"], new("ad", "azalan"));
        var d = IzgaraDuzeni.Kur(Tanim, t, surum: 1);
        Assert.True(d.VarsayilanMi);
        Assert.Null(d.Siralama);
    }

    [Fact]
    public void GD06_Tasi_sinirda_null_tasinamazin_otesine_gecmez_gizlileri_atlayabilir()
    {
        var d = IzgaraDuzeni.Varsayilan(Tanim);
        Assert.Null(d.Tasi("ad", -1));                       // en solda
        Assert.Null(d.Tasi("not", 1));                       // sağında yalnız taşınamaz eylem var
        Assert.Null(d.Tasi("eylem", -1));                    // eylem taşınamaz
        Assert.Equal(["tur", "ad"], d.Tasi("ad", 1)!.Sira.Take(2));
        // "has"ı gizle → başlıkta (yalnız görünen) "tl-bakiye" sağa taşınınca has'ın üzerinden atlar
        var g = d.GizleDegistir("has")!;
        var t = g.Tasi("tl-bakiye", 1, yalnizGorunen: true);
        Assert.Null(t);                                      // "not" da gizli → sağda görünen yok
        var t2 = g.GizleDegistir("not")!.Tasi("tl-bakiye", 1, yalnizGorunen: true)!;
        Assert.Equal(["ad", "tur", "telefon", "has", "not", "tl-bakiye", "eylem"], t2.Sira);
    }

    [Fact]
    public void GD07_Birak_kaynagi_hedefin_yerine_koyar()
    {
        var d = IzgaraDuzeni.Varsayilan(Tanim).Birak("has", "ad")!;
        Assert.Equal(["has", "ad", "tur", "telefon", "tl-bakiye", "not", "eylem"], d.Sira);
        Assert.Null(IzgaraDuzeni.Varsayilan(Tanim).Birak("eylem", "ad"));
        Assert.Null(IzgaraDuzeni.Varsayilan(Tanim).Birak("ad", "ad"));
    }

    [Fact]
    public void GD08_Siralama_dongusu_artan_azalan_varsayilan_siralanamaz_kolon_null()
    {
        var d = IzgaraDuzeni.Varsayilan(Tanim);
        var a = d.SiralamaDongusu("ad")!;
        Assert.Equal(new IzgaraSiralamaTercihi("ad", "artan"), a.Siralama);
        var z = a.SiralamaDongusu("ad")!;
        Assert.Equal(new IzgaraSiralamaTercihi("ad", "azalan"), z.Siralama);
        Assert.Null(z.SiralamaDongusu("ad")!.Siralama);
        Assert.Equal("telefon", z.SiralamaDongusu("telefon")!.Siralama!.A);   // başka kolon → artan baştan
        Assert.Null(d.SiralamaDongusu("has"));                                 // sunucu adı yok
    }

    [Fact]
    public void GD09_Siralanan_kolon_gizlenince_siralama_kalkar()
    {
        var d = IzgaraDuzeni.Varsayilan(Tanim).SiralamaDongusu("telefon")!;
        Assert.Null(d.GizleDegistir("telefon")!.Siralama);
    }

    [Fact]
    public void GD10_Tercih_karsiligi_tasinamazi_yazmaz_ve_kurala_uyar()
    {
        var d = IzgaraDuzeni.Varsayilan(Tanim).Tasi("ad", 1)!.GizleDegistir("has")!;
        var t = d.Tercih();
        Assert.DoesNotContain("eylem", t.K);
        Assert.Equal(["has", "not"], t.G);
        Assert.NotNull(IzgaraTercihKurali.Coz(IzgaraTercihKurali.Yaz(t), out _));
    }
}

[Collection("http")]
public sealed class GridTercihHttpTestleri(HttpFikstur f)
{
    private const string Anahtar = "grid:cariler:cari-liste";
    private const string Deger = """{"s":1,"k":["telefon","ad"],"g":["has"],"o":{"a":"ad","y":"azalan"}}""";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static object Yama(string anahtar, string? deger) =>
        new { tercihler = new Dictionary<string, string?> { [anahtar] = deger } };

    private static async Task<JsonElement> Json(HttpResponseMessage c) =>
        JsonDocument.Parse(await c.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task GH01_Grid_tercihi_yazilir_geri_okunur_SQL_ve_denetim_izi()
    {
        var k = YeniKiraci("GRID1-H1");
        var c = f.Ortam.Istemci(k);
        var yaz = await c.PatchAsJsonAsync("/api/v1/tercihler/benim", Yama(Anahtar, Deger));
        Assert.Equal(HttpStatusCode.OK, yaz.StatusCode);

        var oku = await Json(await c.GetAsync("/api/v1/tercihler/benim"));
        Assert.Equal(Deger, oku.GetProperty("tercihler").GetProperty(Anahtar).GetString());

        using var db = f.Vt.Olustur(k.TenantId);
        var satir = await db.KullaniciTercihleri.AsNoTracking()
            .SingleAsync(t => t.KullaniciId == k.KullaniciId && t.Anahtar == Anahtar);
        Assert.Equal(Deger, satir.Deger);
        Assert.True(await db.DenetimKayitlari.AsNoTracking().AnyAsync(d =>
            d.VarlikTuru == "KullaniciTercihi" && d.KullaniciId == k.KullaniciId && d.Detay!.Contains(Anahtar)));
    }

    [Theory]
    [InlineData("grid:bilinmeyen:cari-liste", Deger)]
    [InlineData("grid:cariler:Cari", Deger)]
    [InlineData(Anahtar, "{bozuk")]
    [InlineData(Anahtar, """{"s":1,"k":["ad","ad"]}""")]
    [InlineData(Anahtar, """{"s":1,"o":{"a":"ad","y":"yukari"}}""")]
    public async Task GH02_Bilinmeyen_ekran_ya_da_bicim_disi_deger_400_depoya_girmez(string anahtar, string deger)
    {
        var k = YeniKiraci("GRID1-H2");
        var cevap = await f.Ortam.Istemci(k).PatchAsJsonAsync("/api/v1/tercihler/benim", Yama(anahtar, deger));
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        using var db = f.Vt.Olustur(k.TenantId);
        Assert.False(await db.KullaniciTercihleri.AnyAsync(t => t.KullaniciId == k.KullaniciId));
    }

    [Fact]
    public async Task GH03_Boyut_siniri_2000_gecer_2001_400()
    {
        var k = YeniKiraci("GRID1-H3");
        var c = f.Ortam.Istemci(k);
        var taban = """{"s":1,"k":[],"g":[],"o":null}""";
        string Uret(int n) => taban[..^1] + new string(' ', n - taban.Length) + "}";

        var tam = await c.PatchAsJsonAsync("/api/v1/tercihler/benim", Yama(Anahtar, Uret(2000)));
        Assert.Equal(HttpStatusCode.OK, tam.StatusCode);
        var fazla = await c.PatchAsJsonAsync("/api/v1/tercihler/benim", Yama(Anahtar, Uret(2001)));
        Assert.Equal(HttpStatusCode.BadRequest, fazla.StatusCode);
        Assert.Contains("2000", await fazla.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GH04_Kullanici_basina_200_grid_anahtari_201_inci_400_mevcut_guncellenebilir()
    {
        var k = YeniKiraci("GRID1-H4");
        using (var db = f.Vt.Olustur(k.TenantId))
        {
            var simdi = DateTime.UtcNow;
            for (var i = 0; i < IzgaraTercihKurali.KullaniciBasinaEnCokAnahtar; i++)
                db.KullaniciTercihleri.Add(new KullaniciTercihi
                {
                    Id = Kimlik.Ulid(), TenantId = k.TenantId, KullaniciId = k.KullaniciId,
                    Anahtar = $"grid:cariler:g{i}", Deger = """{"s":1}""", Olusturuldu = simdi, Guncellendi = simdi,
                });
            await db.SaveChangesAsync();
        }
        var c = f.Ortam.Istemci(k);

        var yeni = await c.PatchAsJsonAsync("/api/v1/tercihler/benim", Yama("grid:cariler:g-yeni", Deger));
        Assert.Equal(HttpStatusCode.BadRequest, yeni.StatusCode);
        Assert.Contains("200", await yeni.Content.ReadAsStringAsync());

        var guncelle = await c.PatchAsJsonAsync("/api/v1/tercihler/benim", Yama("grid:cariler:g5", Deger));
        Assert.Equal(HttpStatusCode.OK, guncelle.StatusCode);

        // Aynı yamada biri silinip biri eklenirse sınır aşılmaz.
        var takas = await c.PatchAsJsonAsync("/api/v1/tercihler/benim", new
        {
            tercihler = new Dictionary<string, string?> { ["grid:cariler:g7"] = null, ["grid:cariler:g-yeni"] = Deger },
        });
        Assert.Equal(HttpStatusCode.OK, takas.StatusCode);
    }

    [Fact]
    public async Task GH05_Baskasinin_grid_tercihi_OKUNAMAZ_YAZILAMAZ_SILINEMEZ_B_varsayilan_gorur()
    {
        var k = YeniKiraci("GRID1-H5");
        var a = f.Ortam.Istemci(k);
        Assert.Equal(HttpStatusCode.OK, (await a.PatchAsJsonAsync("/api/v1/tercihler/benim", Yama(Anahtar, Deger))).StatusCode);

        var b = f.Ortam.Istemci("grid-b-kullanici", k.TenantId, k.SubeId, "TEZGAHTAR");
        var bOku = await Json(await b.GetAsync("/api/v1/tercihler/benim"));
        Assert.False(bOku.GetProperty("tercihler").TryGetProperty(Anahtar, out _));     // B: varsayılan

        // B yazar/siler — kimlik JETONDAN: yalnız KENDİ satırı etkilenir, A'nınki aynen durur.
        var bDeger = """{"s":1,"k":["has"],"g":[],"o":null}""";
        Assert.Equal(HttpStatusCode.OK, (await b.PatchAsJsonAsync("/api/v1/tercihler/benim", Yama(Anahtar, bDeger))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await b.DeleteAsync($"/api/v1/tercihler/benim/{Anahtar}")).StatusCode);

        var aOku = await Json(await a.GetAsync("/api/v1/tercihler/benim"));
        Assert.Equal(Deger, aOku.GetProperty("tercihler").GetProperty(Anahtar).GetString());
        using var db = f.Vt.Olustur(k.TenantId);
        Assert.False(await db.KullaniciTercihleri.AnyAsync(t => t.KullaniciId == "grid-b-kullanici"));
    }

    [Fact]
    public async Task GH06_Varsayilana_don_DELETE_ve_bozuk_kayit_da_silinebilir()
    {
        var k = YeniKiraci("GRID1-H6");
        using (var db = f.Vt.Olustur(k.TenantId))
        {
            var simdi = DateTime.UtcNow;
            db.KullaniciTercihleri.Add(new KullaniciTercihi
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, KullaniciId = k.KullaniciId,
                Anahtar = Anahtar, Deger = "{eski-surum-bozuk", Olusturuldu = simdi, Guncellendi = simdi,
            });
            await db.SaveChangesAsync();
        }
        var c = f.Ortam.Istemci(k);
        var oku = await c.GetAsync("/api/v1/tercihler/benim");
        Assert.Equal(HttpStatusCode.OK, oku.StatusCode);                // bozuk kayıt okumayı BOZMAZ

        var sil = await c.DeleteAsync($"/api/v1/tercihler/benim/{Uri.EscapeDataString(Anahtar)}");
        Assert.Equal(HttpStatusCode.OK, sil.StatusCode);
        using var db2 = f.Vt.Olustur(k.TenantId);
        Assert.False(await db2.KullaniciTercihleri.AnyAsync(t => t.KullaniciId == k.KullaniciId && t.Anahtar == Anahtar));

        Assert.Equal(HttpStatusCode.BadRequest, (await c.DeleteAsync("/api/v1/tercihler/benim/grid:bilinmeyen:x")).StatusCode);
    }

    [Fact]
    public async Task GH07_Kimliksiz_grid_tercihi_401()
    {
        var cevap = await f.Ortam.AnonimIstemci().PatchAsJsonAsync("/api/v1/tercihler/benim", Yama(Anahtar, Deger));
        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
    }

    // ── Sunucu sıralaması (/cari?sirala=&yon=) ───────────────────────────────

    private async Task<(Kiraci K, HttpClient C)> SiraliCariler(string ad)
    {
        var k = YeniKiraci(ad);
        var c = f.Ortam.Istemci(k);
        var bakiye = new Dictionary<string, decimal> { ["GRIDSIRA Beta"] = 500m, ["GRIDSIRA Alfa"] = -200m, ["GRIDSIRA Gama"] = 50m };
        foreach (var (cariAd, _) in bakiye)
        {
            var y = await c.PostAsJsonAsync("/api/v1/cari", new { tur = "MUSTERI", ad = cariAd });
            Assert.Equal(HttpStatusCode.Created, y.StatusCode);
        }
        using var db = f.Vt.Olustur(k.TenantId);
        var cariler = await db.Cariler.Where(x => x.Ad.StartsWith("GRIDSIRA")).ToListAsync();
        foreach (var cari in cariler)
        {
            var b = await db.CariBakiyeler.SingleAsync(x => x.CariId == cari.Id);
            b.TlBakiye = bakiye[cari.Ad];
        }
        await db.SaveChangesAsync();
        return (k, c);
    }

    private static async Task<List<string>> Adlar(HttpClient c, string sorgu)
    {
        var cevap = await c.GetAsync($"/api/v1/cari?q=GRIDSIRA&limit=100{sorgu}");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        return [.. (await Json(cevap)).EnumerateArray().Select(e => e.GetProperty("ad").GetString()!)];
    }

    [Fact]
    public async Task GH08_Cari_listesi_SUNUCUDA_siralanir_ad_ve_tl_bakiye_iki_yonde()
    {
        var (_, c) = await SiraliCariler("GRID1-H8");
        Assert.Equal(["GRIDSIRA Alfa", "GRIDSIRA Beta", "GRIDSIRA Gama"], await Adlar(c, ""));                  // varsayılan: ad
        Assert.Equal(["GRIDSIRA Gama", "GRIDSIRA Beta", "GRIDSIRA Alfa"], await Adlar(c, "&sirala=ad&yon=azalan"));
        Assert.Equal(["GRIDSIRA Alfa", "GRIDSIRA Gama", "GRIDSIRA Beta"], await Adlar(c, "&sirala=tl-bakiye&yon=artan"));
        Assert.Equal(["GRIDSIRA Beta", "GRIDSIRA Gama", "GRIDSIRA Alfa"], await Adlar(c, "&sirala=tl-bakiye&yon=azalan"));
        // Sınır sıralamadan SONRA uygulanır: en büyük borçlu limit=1'de de gelir.
        var tek = await Json(await c.GetAsync("/api/v1/cari?q=GRIDSIRA&limit=1&sirala=tl-bakiye&yon=azalan"));
        Assert.Equal("GRIDSIRA Beta", tek[0].GetProperty("ad").GetString());
    }

    [Theory]
    [InlineData("&sirala=has")]               // has bilinçli olarak sıralanmaz (emanetsiz hesap)
    [InlineData("&sirala=AD")]
    [InlineData("&sirala=ad&yon=yukari")]
    [InlineData("&yon=azalan")]               // yön var, alan yok
    public async Task GH09_Bilinmeyen_siralama_adi_ya_da_yon_400_sessiz_varsayilan_yok(string sorgu)
    {
        var k = YeniKiraci("GRID1-H9");
        var cevap = await f.Ortam.Istemci(k).GetAsync($"/api/v1/cari?limit=10{sorgu}");
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }
}

public sealed class GridEkranTestleri : BunitContext
{
    private const string Anahtar = "grid:cariler:cari-liste";
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string tercihGovdesi = """{"tercihler":{}}""";
    private HttpStatusCode yazmaKodu = HttpStatusCode.OK;
    private bool yazmaAgHatasi;
    private string cariListesi = CariListesi;

    private const string CariListesi = """
        [{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567",
          "email":"ayse@ornek.com","tlBakiye":-1500.00,"hasGramBakiye":2.5,
          "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"aktif":true,
          "riskSeviye":null,"pepBeyani":false},
         {"id":"c2","ad":"Kapalıçarşı Toptan","tur":"TEDARIKCI","telefon":null,
          "email":null,"tlBakiye":0,"hasGramBakiye":0,"aktif":true,
          "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false}]
        """;

    public GridEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(GridEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, Uri.UnescapeDataString(istek.RequestUri!.PathAndQuery), govde));
            var kod = HttpStatusCode.OK;
            string g;
            if (yol.Contains("/tercihler/benim"))
            {
                if (istek.Method != HttpMethod.Get && t.yazmaAgHatasi)
                    throw new HttpRequestException("TypeError: Failed to fetch (net::ERR_CONNECTION_REFUSED)");
                if (istek.Method == HttpMethod.Get) g = t.tercihGovdesi;
                else { kod = t.yazmaKodu; g = kod == HttpStatusCode.OK ? """{"tercihler":{}}""" : """{"message":"Geçersiz ızgara tercihi"}"""; }
            }
            else g = yol switch
            {
                var y when y.EndsWith("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"gbpTry":null}""",
                var y when y.EndsWith("/cari") => t.cariListesi,
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var y when y.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","aktif":true}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(kod)
            { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private List<(string Metot, string Yol, string Govde)> Istekler()
    {
        lock (istekler) return [.. istekler];
    }

    private void TercihVer(string deger) =>
        tercihGovdesi = JsonSerializer.Serialize(new { tercihler = new Dictionary<string, string> { [Anahtar] = deger } });

    private IRenderedComponent<SayfaCariler> Ciz(string mod = ArayuzModuServisi.Profesyonel)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<SayfaCariler>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("tr.cari-satir")));
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "GET" && i.Yol.EndsWith("/tercihler/benim")));
        return e;
    }

    private static List<string> Kolonlar(IRenderedComponent<SayfaCariler> e) =>
        [.. e.FindAll("#cari-liste-tablo thead th").Select(t => t.GetAttribute("data-kolon")!)];

    private static IzgaraTercihi SonYazilan(List<(string Metot, string Yol, string Govde)> istekler)
    {
        var patch = istekler.Last(i => i.Metot == "PATCH" && i.Yol.EndsWith("/tercihler/benim"));
        var ic = JsonDocument.Parse(patch.Govde).RootElement.GetProperty("tercihler").GetProperty(Anahtar).GetString();
        return IzgaraTercihKurali.Coz(ic, out _)!;
    }

    [Fact]
    public void GE01_Tercih_yokken_varsayilan_duzen_kimlikler_korunur()
    {
        var e = Ciz();
        Assert.Equal(["ad", "tur", "telefon", "tl-bakiye", "has", "eylem"], Kolonlar(e));
        // Mevcut id sözleşmesi: KartListe → #cari-liste-sar, satır #c-satir-<id>, eylem #c-eylem-<id>.
        Assert.NotNull(e.Find("#cari-liste-sar > table.veri-tablosu"));
        Assert.NotNull(e.Find("#c-satir-c1"));
        Assert.NotNull(e.Find("#c-eylem-c1"));
        Assert.NotNull(e.Find("#cari-liste-sutunlar-ac"));
        Assert.Empty(e.FindAll("#cari-liste-hata"));
        Assert.Empty(e.FindAll("#cari-liste-okunamadi"));
        // Her hücre kart düzeni için data-etiket taşır (ad ve eylem hariç: kendini söyler).
        var tel = e.Find("#c-satir-c1 td[data-kolon='telefon']");
        Assert.Equal(Dil(e).Cari.Telefon, tel.GetAttribute("data-etiket"));
        Assert.Null(e.Find("#c-satir-c1 td[data-kolon='ad']").GetAttribute("data-etiket"));
        // Başlık metni yalnız başlıktır (yön oku CSS'te).
        Assert.Equal(Dil(e).Auth.Ad, e.Find("th[data-kolon='ad']").TextContent.Trim());
    }

    private static Kuyumcu.Core.I18n.Sozluk Dil(IRenderedComponent<SayfaCariler> e) =>
        e.Services.GetRequiredService<DilServisi>().T;

    [Fact]
    public void GE02_Kayitli_duzen_uygulanir_gizli_kolon_cizilmez_sayaci_yazilir()
    {
        TercihVer("""{"s":1,"k":["telefon","ad","tur","tl-bakiye","has"],"g":["has"],"o":null}""");
        var e = Ciz();
        e.WaitForAssertion(() => Assert.Equal(["telefon", "ad", "tur", "tl-bakiye", "eylem"], Kolonlar(e)));
        Assert.Empty(e.FindAll("#c-satir-c1 td[data-kolon='has']"));
        Assert.Equal(string.Format(IzgaraSozlugu.Tr.GizliSutunSayisi, 1), e.Find("#cari-liste-gizli-sayi").TextContent.Trim());
    }

    [Theory]
    [InlineData("{bozuk-json")]
    [InlineData("""{"s":2,"k":["telefon","ad"],"g":["has"],"o":null}""")]          // eski/yeni sürüm
    [InlineData("""{"s":1,"k":["ad","ad"],"g":[],"o":null}""")]                    // biçim dışı
    public void GE03_Bozuk_ya_da_eski_surum_tercih_VARSAYILAN_duzen_hata_yok(string deger)
    {
        TercihVer(deger);
        var e = Ciz();
        Assert.Equal(["ad", "tur", "telefon", "tl-bakiye", "has", "eylem"], Kolonlar(e));
        Assert.Empty(e.FindAll("#cari-liste-hata"));
        Assert.Empty(e.FindAll(".bant-hata"));
        // Tercih SİLİNMEDİ (yalnız yok sayıldı) — "Varsayılana dön" onu silebilir: kilitli DEĞİL.
        Assert.DoesNotContain(Istekler(), i => i.Metot == "DELETE");
        e.Find("#cari-liste-sutunlar-ac").Click();
        Assert.False(e.Find("#cari-liste-varsayilan").HasAttribute("disabled"));
    }

    [Fact]
    public void GE04_Panel_saga_tasi_PATCH_tam_duzen_yazar_ekran_hemen_degisir()
    {
        var e = Ciz();
        e.Find("#cari-liste-sutunlar-ac").Click();
        e.Find("#cari-liste-saga-ad").Click();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "PATCH"));
        var t = SonYazilan(Istekler());
        Assert.Equal(1, t.S);
        Assert.Equal(["tur", "ad", "telefon", "tl-bakiye", "has"], t.K);    // eylem yazılmaz (taşınamaz)
        Assert.Equal(["tur", "ad", "telefon", "tl-bakiye", "has", "eylem"], Kolonlar(e));
        e.WaitForAssertion(() => Assert.Contains(IzgaraSozlugu.Tr.Kaydedildi, e.Find("#cari-liste-basari").TextContent));
    }

    [Fact]
    public void GE05_Gizle_PATCH_g_yazar_zorunlu_kolon_kilitli_sebebi_yazili()
    {
        var e = Ciz();
        e.Find("#cari-liste-sutunlar-ac").Click();
        Assert.True(e.Find("#cari-liste-gizle-ad").HasAttribute("disabled"));
        Assert.True(e.Find("#cari-liste-gizle-tl-bakiye").HasAttribute("disabled"));
        // GRID-1 tur 3: taşınamaz eylem sütunu panelde YOK (üç kilitli düğmeli ölü satırdı).
        Assert.Empty(e.FindAll("#cari-liste-sutun-eylem"));
        Assert.Empty(e.FindAll("#cari-liste-gizle-eylem"));
        // Kilit sebebi: düğmenin yanında simge + title + erişilebilir ad (alt satır metni yok).
        var kilit = e.Find("#cari-liste-kilit-ad");
        Assert.Equal(IzgaraSozlugu.Tr.HepGorunur, kilit.GetAttribute("title"));
        Assert.Equal(IzgaraSozlugu.Tr.HepGorunur, kilit.GetAttribute("aria-label"));
        Assert.Equal("cari-liste-kilit-ad", e.Find("#cari-liste-gizle-ad").GetAttribute("aria-describedby"));

        e.Find("#cari-liste-gizle-has").Click();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "PATCH"));
        Assert.Equal(["has"], SonYazilan(Istekler()).G);
        Assert.DoesNotContain("has", Kolonlar(e));
        // "Göster" İKİNCİL kalır (aria-pressed dolu/birincil görünüm vermiyordu — tur 3).
        Assert.Null(e.Find("#cari-liste-gizle-has").GetAttribute("aria-pressed"));
        Assert.Contains("btn-ikincil", e.Find("#cari-liste-gizle-has").ClassList);
        Assert.Equal(IzgaraSozlugu.Tr.Goster, e.Find("#cari-liste-gizle-has").TextContent.Trim());
    }

    [Fact]
    public void GE06_Baslik_siralamasi_SUNUCUYA_gider_artan_azalan_varsayilan_aria_sort()
    {
        var e = Ciz();
        Assert.Equal("none", e.Find("th[data-kolon='ad']").GetAttribute("aria-sort"));
        Assert.Null(e.Find("th[data-kolon='has']").GetAttribute("aria-sort"));     // sunucu adı yok → düğme yok
        Assert.Empty(e.FindAll("#cari-liste-sirala-has"));

        e.Find("#cari-liste-sirala-tl-bakiye").Click();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "GET" && i.Yol.Contains("/cari?") && i.Yol.Contains("sirala=tl-bakiye&yon=artan")));
        Assert.Equal("ascending", e.Find("th[data-kolon='tl-bakiye']").GetAttribute("aria-sort"));
        Assert.Equal(new IzgaraSiralamaTercihi("tl-bakiye", "artan"), SonYazilan(Istekler()).O);

        e.Find("#cari-liste-sirala-tl-bakiye").Click();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Yol.Contains("sirala=tl-bakiye&yon=azalan")));
        Assert.Equal("descending", e.Find("th[data-kolon='tl-bakiye']").GetAttribute("aria-sort"));

        var once = Istekler().Count(i => i.Metot == "GET" && i.Yol.Contains("/cari?"));
        e.Find("#cari-liste-sirala-tl-bakiye").Click();
        e.WaitForAssertion(() => Assert.True(Istekler().Count(i => i.Metot == "GET" && i.Yol.Contains("/cari?")) > once));
        Assert.DoesNotContain("sirala=", Istekler().Last(i => i.Metot == "GET" && i.Yol.Contains("/cari?")).Yol);
        Assert.Null(SonYazilan(Istekler()).O);
    }

    [Fact]
    public void GE07_Kayitli_siralama_acilista_sunucudan_istenir()
    {
        TercihVer("""{"s":1,"k":[],"g":[],"o":{"a":"ad","y":"azalan"}}""");
        var e = Ciz();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "GET" && i.Yol.Contains("sirala=ad&yon=azalan")));
        Assert.Equal("descending", e.Find("th[data-kolon='ad']").GetAttribute("aria-sort"));
    }

    [Fact]
    public void GE08_Klavye_Alt_sag_ok_basligi_tasir_yalniz_Alt_ile()
    {
        var e = Ciz();
        e.Find("th[data-kolon='telefon']").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.DoesNotContain(Istekler(), i => i.Metot == "PATCH");                 // Alt yok → taşıma yok
        e.Find("th[data-kolon='telefon']").KeyDown(new KeyboardEventArgs { Key = "ArrowRight", AltKey = true });
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "PATCH"));
        Assert.Equal(["ad", "tur", "tl-bakiye", "telefon", "has", "eylem"], Kolonlar(e));
    }

    [Fact]
    public void GE09_Varsayilana_don_DELETE_atar_kayit_yokken_kilitli_ve_sebebi_yazili()
    {
        var e = Ciz();
        e.Find("#cari-liste-sutunlar-ac").Click();
        Assert.True(e.Find("#cari-liste-varsayilan").HasAttribute("disabled"));
        Assert.Equal(IzgaraSozlugu.Tr.ZatenVarsayilan, e.Find("#cari-liste-varsayilan-kilit").TextContent.Trim());

        e.Find("#cari-liste-gizle-has").Click();
        e.WaitForAssertion(() => Assert.False(e.Find("#cari-liste-varsayilan").HasAttribute("disabled")));
        e.Find("#cari-liste-varsayilan").Click();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "DELETE" && i.Yol.EndsWith($"/tercihler/benim/{Anahtar}")));
        Assert.Contains("has", Kolonlar(e));
        e.WaitForAssertion(() => Assert.Contains(IzgaraSozlugu.Tr.VarsayilanaDonuldu, e.Find("#cari-liste-basari").TextContent));
    }

    [Fact]
    public void GE10_Yazma_hatasi_duzeni_GERI_ALIR_ve_bant_sebebi_soyler()
    {
        yazmaKodu = HttpStatusCode.BadRequest;
        var e = Ciz();
        e.Find("#cari-liste-sutunlar-ac").Click();
        e.Find("#cari-liste-gizle-has").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#cari-liste-hata")));
        Assert.StartsWith(IzgaraSozlugu.Tr.KaydedilemediOnek, e.Find("#cari-liste-hata").TextContent.Trim());
        Assert.Contains("has", Kolonlar(e));                                      // geri alındı
    }

    [Fact]
    public void GE11_Her_panel_dugmesi_ya_istek_atar_ya_kilitli()
    {
        TercihVer("""{"s":1,"k":["ad","tur","telefon","tl-bakiye","has"],"g":[],"o":null}""");
        var e = Ciz();
        e.Find("#cari-liste-sutunlar-ac").Click();
        Assert.NotNull(e.Find("#cari-liste-sutunlar"));                           // aç/kapa düğmesi paneli açar
        var idler = e.FindAll("#cari-liste-sutunlar button").Select(b => b.Id!).ToList();
        Assert.NotEmpty(idler);
        foreach (var id in idler)
        {
            var b = e.Find($"#{id}");
            if (b.HasAttribute("disabled")) continue;
            var once = Istekler().Count(i => i.Metot is "PATCH" or "DELETE");
            b.Click();
            e.WaitForAssertion(() => Assert.True(Istekler().Count(i => i.Metot is "PATCH" or "DELETE") > once, $"{id} istek atmadı"));
            if (e.FindAll("#cari-liste-sutunlar").Count == 0) e.Find("#cari-liste-sutunlar-ac").Click();
        }
    }

    [Fact]
    public void GE13_Surukle_birak_JS_geri_cagrisi_tercihi_yazar_ve_HEMEN_cizer()
    {
        var e = Ciz();
        var izgara = e.FindComponent<Izgara<Kuyumcu.Contracts.Sayfalar.Cariler.CariVeri>>();
        izgara.InvokeAsync(() => izgara.Instance.Birak("has", "ad"));
        e.WaitForAssertion(() => Assert.Equal(["has", "ad", "tur", "telefon", "tl-bakiye", "eylem"], Kolonlar(e)));
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "PATCH"));
        Assert.Equal(["has", "ad", "tur", "telefon", "tl-bakiye"], SonYazilan(Istekler()).K);
        // Taşınamaz kolon bırakılamaz → istek yok.
        var once = Istekler().Count(i => i.Metot == "PATCH");
        izgara.InvokeAsync(() => izgara.Instance.Birak("eylem", "ad"));
        Assert.Equal(once, Istekler().Count(i => i.Metot == "PATCH"));
    }

    private List<string> ListeIstekleri() =>
        [.. Istekler().Where(i => i.Metot == "GET" && (i.Yol.EndsWith("/cari") || i.Yol.Contains("/cari?"))).Select(i => i.Yol)];

    [Fact]
    public void GE14_Kayitli_siralama_ILK_liste_isteginde_gider_ikinci_GET_yok()
    {
        // GRID-1 tur 2 (doğrulayıcı ORTA): önce sırasız, sonra sıralı iki GET atılıyordu.
        TercihVer("""{"s":1,"k":[],"g":[],"o":{"a":"tl-bakiye","y":"azalan"}}""");
        var e = Ciz();
        e.WaitForAssertion(() => Assert.Equal("descending", e.Find("th[data-kolon='tl-bakiye']").GetAttribute("aria-sort")));
        var liste = ListeIstekleri();
        Assert.Single(liste);
        Assert.Contains("sirala=tl-bakiye&yon=azalan", liste[0]);
        // Tercih liste isteğinden ÖNCE okunur.
        var tum = Istekler();
        Assert.True(tum.FindIndex(i => i.Yol.EndsWith("/tercihler/benim"))
                    < tum.FindIndex(i => i.Yol.Contains("/cari?")), "tercih liste isteğinden önce okunmalı");
    }

    [Fact]
    public void GE15_Tercih_yokken_tek_liste_istegi_siralamasiz()
    {
        var e = Ciz();
        var liste = ListeIstekleri();
        Assert.Single(liste);
        Assert.DoesNotContain("sirala=", liste[0]);
        Assert.Equal("none", e.Find("th[data-kolon='ad']").GetAttribute("aria-sort"));
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    [InlineData(ArayuzModuServisi.Basit)]
    public void GE16_Kart_rolu_sutun_SIRASINDAN_bagimsiz_ad_birincil_eylem_sabit(string mod)
    {
        // GRID-1 tur 2 (doğrulayıcı KRİTİK): telefon başa alınınca ilk hücre artık ad değil;
        // kart kalıbı yeri ROLDEN alır.
        TercihVer("""{"s":1,"k":["telefon","ad"],"g":["has"],"o":null}""");
        var e = Ciz(mod);
        e.WaitForAssertion(() => Assert.Equal("telefon", e.Find("#c-satir-c1 td").GetAttribute("data-kolon")));
        Assert.Equal("birincil", e.Find("#c-satir-c1 td[data-kolon='ad']").GetAttribute("data-izgara-rol"));
        Assert.Equal("sabit", e.Find("#c-satir-c1 td[data-kolon='eylem']").GetAttribute("data-izgara-rol"));
        Assert.Null(e.Find("#c-satir-c1 td[data-kolon='telefon']").GetAttribute("data-izgara-rol"));
        Assert.Null(e.Find("#c-satir-c1 td[data-kolon='tl-bakiye']").GetAttribute("data-izgara-rol"));
    }

    // ═══ GRID-1 tur 3 (doğrulayıcı bulguları) ═══

    [Theory]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    [InlineData(ArayuzModuServisi.Basit)]
    public void GE17_Sayisal_sutun_basligi_ve_degeri_AYNI_hiza_sinifi_tutar_rozet_sabit_duzen(string mod)
    {
        var e = Ciz(mod);
        foreach (var a in new[] { "tl-bakiye", "has" })
        {
            Assert.Contains("izgara-sayi", e.Find($"th[data-kolon='{a}']").ClassList);
            Assert.Contains("izgara-sayi", e.Find($"#c-satir-c1 td[data-kolon='{a}']").ClassList);
        }
        foreach (var a in new[] { "ad", "telefon" })
        {
            Assert.DoesNotContain("izgara-sayi", e.Find($"th[data-kolon='{a}']").ClassList);
            Assert.DoesNotContain("izgara-sayi", e.Find($"#c-satir-c1 td[data-kolon='{a}']").ClassList);
        }
        // Tutar hücresi: tutar kendi kutusunda, rozet ondan SONRA (sabit dikey düzen).
        var tutar = e.Find("#c-satir-c1 td[data-kolon='tl-bakiye'] .izgara-tutar");
        Assert.Equal("izgara-tutar-deger", tutar.Children[0].ClassName);
        Assert.Contains("₺", tutar.Children[0].TextContent);
        Assert.True(tutar.Children.Length >= 2, "rozet tutarın altında ayrı öğe");
    }

    /// <summary>Sayfanın listeyi yeniden çekmesi (arama/süzgeç yolu) + yeniden çizim.</summary>
    private static void YukleCagir(IRenderedComponent<SayfaCariler> e)
    {
        const System.Reflection.BindingFlags b = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        e.InvokeAsync(async () =>
        {
            await (Task)typeof(SayfaCariler).GetMethod("Yukle", b)!.Invoke(e.Instance, null)!;
            typeof(Microsoft.AspNetCore.Components.ComponentBase).GetMethod("StateHasChanged", b)!.Invoke(e.Instance, null);
        }).GetAwaiter().GetResult();
    }

    private int OdakSayisi() => JSInterop.Invocations.Count(i => i.Identifier.EndsWith("focus", StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void GE18_Acilista_tercih_TEK_GET_liste_bosalip_dolunca_da_yeniden_okunmaz()
    {
        TercihVer("""{"s":1,"k":["telefon","ad"],"g":[],"o":{"a":"tl-bakiye","y":"azalan"}}""");
        var e = Ciz();
        e.WaitForAssertion(() => Assert.Equal("telefon", Kolonlar(e)[0]));
        Assert.Single(Istekler(), i => i.Metot == "GET" && i.Yol.EndsWith("/tercihler/benim"));
        // Liste boşalır (ızgara kalkar) → dolar (ızgara yeniden kurulur): tercih ÖNBELLEKTEN.
        cariListesi = "[]";
        YukleCagir(e);
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("tr.cari-satir")));
        cariListesi = CariListesi;
        YukleCagir(e);
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("tr.cari-satir")));
        Assert.Equal("telefon", Kolonlar(e)[0]);
        Assert.Single(Istekler(), i => i.Metot == "GET" && i.Yol.EndsWith("/tercihler/benim"));
        // Yazılan değişiklik önbelleğe de işlenir: ızgara yeniden kurulunca SON düzen gelir.
        e.Find("#cari-liste-sutunlar-ac").Click();
        e.Find("#cari-liste-gizle-has").Click();
        e.WaitForAssertion(() => Assert.DoesNotContain("has", Kolonlar(e)));
        cariListesi = "[]";
        YukleCagir(e);
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("tr.cari-satir")));
        cariListesi = CariListesi;
        YukleCagir(e);
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("tr.cari-satir")));
        Assert.DoesNotContain("has", Kolonlar(e));
        Assert.Single(Istekler(), i => i.Metot == "GET" && i.Yol.EndsWith("/tercihler/benim"));
    }

    [Fact]
    public void GE19_Ag_hatasinda_bantta_HAM_istisna_metni_yok_genel_Turkce_mesaj()
    {
        yazmaAgHatasi = true;
        var e = Ciz();
        e.Find("#cari-liste-sutunlar-ac").Click();
        e.Find("#cari-liste-gizle-has").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#cari-liste-hata")));
        var metin = e.Find("#cari-liste-hata").TextContent;
        Assert.Contains(IzgaraSozlugu.Tr.GenelYazmaHatasi, metin);
        Assert.DoesNotContain("TypeError", metin);
        Assert.DoesNotContain("Failed to fetch", metin);
        Assert.Contains("has", Kolonlar(e));                                      // geri alındı
    }

    [Fact]
    public void GE20_Sunucu_mesaji_ApiHata_ise_bantta_O_yazar()
    {
        yazmaKodu = HttpStatusCode.BadRequest;
        var e = Ciz();
        e.Find("#cari-liste-sutunlar-ac").Click();
        e.Find("#cari-liste-gizle-has").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#cari-liste-hata")));
        Assert.Contains("Geçersiz ızgara tercihi", e.Find("#cari-liste-hata").TextContent);
        Assert.DoesNotContain(IzgaraSozlugu.Tr.GenelYazmaHatasi, e.Find("#cari-liste-hata").TextContent);
    }

    [Fact]
    public void GE21_Panel_sade_aciklama_yok_yatay_ve_dikey_yon_adlari_kayit_onayi_SATIR_ICI()
    {
        var e = Ciz();
        e.Find("#cari-liste-sutunlar-ac").Click();
        Assert.Empty(e.FindAll("#cari-liste-sutunlar p"));                         // açıklama paragrafı yok
        Assert.Empty(e.FindAll("#cari-liste-sutunlar-ipucu"));
        var sag = e.Find("#cari-liste-saga-telefon");
        Assert.Contains($"{IzgaraSozlugu.Tr.SagaTasi}: ", sag.QuerySelector(".izgara-yon-yatay")!.TextContent);
        Assert.Contains($"{IzgaraSozlugu.Tr.AsagiTasi}: ", sag.QuerySelector(".izgara-yon-dikey")!.TextContent);
        var sol = e.Find("#cari-liste-sola-telefon");
        Assert.Contains($"{IzgaraSozlugu.Tr.YukariTasi}: ", sol.QuerySelector(".izgara-yon-dikey")!.TextContent);
        // İlk sütunun "sola"sı kilitli ve sebebi title'da.
        Assert.True(e.Find("#cari-liste-sola-ad").HasAttribute("disabled"));
        Assert.Equal(IzgaraSozlugu.Tr.EnBasta, e.Find("#cari-liste-sola-ad").GetAttribute("title"));
        // Kayıt onayı ARAÇ SATIRINDA (bant değil): tabloyu itmez.
        sag.Click();
        e.WaitForAssertion(() => Assert.Contains(IzgaraSozlugu.Tr.Kaydedildi, e.Find("#cari-liste-arac #cari-liste-basari").TextContent));
        Assert.Empty(e.FindAll(".izgara > .basari-bandi"));
    }

    [Fact]
    public void GE22_Klavyeyle_tasima_sonrasi_ODAK_ayni_sutunun_siralama_dugmesine_doner()
    {
        var e = Ciz();
        var odak0 = OdakSayisi();
        e.Find("th[data-kolon='telefon']").KeyDown(new KeyboardEventArgs { Key = "ArrowRight", AltKey = true });
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "PATCH"));
        e.WaitForAssertion(() => Assert.True(OdakSayisi() > odak0, "taşıma sonrası odak verilmedi"));
        Assert.Equal(["ad", "tur", "tl-bakiye", "telefon", "has", "eylem"], Kolonlar(e));
        // Panelden taşıma da odağı geri verir.
        e.Find("#cari-liste-sutunlar-ac").Click();
        var odak1 = OdakSayisi();
        var once = Istekler().Count(i => i.Metot == "PATCH");
        e.Find("#cari-liste-saga-ad").Click();
        e.WaitForAssertion(() => Assert.True(Istekler().Count(i => i.Metot == "PATCH") > once));
        e.WaitForAssertion(() => Assert.True(OdakSayisi() > odak1, "panel taşıması sonrası odak verilmedi"));
    }

    [Fact]
    public void GE23_Mod_gecisinde_hucre_oznitelikleri_KORUNUR_data_kolon_ve_etiket()
    {
        // GRID-1 tur 3 (canlı bulundu, 375 BASİT): PRO→BASİT geçişinde telefon td'si (iki ayrı dal,
        // aynı @key) class/data-kolon/data-etiket özniteliklerini kaybediyordu → kart etiketi yoktu.
        var e = Ciz();
        Assert.Equal("true", e.Find("#c-satir-c1 td[data-kolon='telefon']").GetAttribute("data-duzenlenebilir"));
        var modu = Services.GetRequiredService<ArayuzModuServisi>();
        e.InvokeAsync(() => modu.Uygula(ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        e.WaitForAssertion(() => Assert.NotNull(e.Find("table.musteri-tablo")));
        var td = e.Find("#c-satir-c1 td:has(.cari-telefon-metin)");
        Assert.Equal("telefon", td.GetAttribute("data-kolon"));
        Assert.Equal(MusteriTuvalSozlugu.Tr.SutunTelefon, td.GetAttribute("data-etiket"));
        Assert.Contains("hucre-tek-satir", td.ClassList);
        Assert.Null(td.GetAttribute("data-duzenlenebilir"));
        // Ve geri: PRO'da düzenleme özniteliği geri gelir.
        e.InvokeAsync(() => modu.Uygula(ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel));
        e.WaitForAssertion(() => Assert.Equal("true", e.Find("#c-satir-c1 td[data-kolon='telefon']").GetAttribute("data-duzenlenebilir")));
        Assert.Equal(Kuyumcu.Core.I18n.Sozlukler.Tr.Cari.Telefon, e.Find("#c-satir-c1 td[data-kolon='telefon']").GetAttribute("data-etiket"));
    }

    [Fact]
    public void GE12_Basit_modda_da_ayni_izgara_basliklar_tuval_metinleri()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        Assert.Equal(["ad", "telefon", "tl-bakiye", "has", "eylem"], Kolonlar(e));
        Assert.NotNull(e.Find("table.musteri-tablo.izgara-tablo"));
        Assert.Equal(MusteriTuvalSozlugu.Tr.SutunMusteri, e.Find("th[data-kolon='ad']").TextContent.Trim());
    }
}

/// <summary>
/// GRID-1 tur 2 (doğrulayıcı KRİTİK, 08.10): 375 px kart kalıbı hücre yerini SIRADAN
/// (:first-child / :last-child) değil ROL / SÜTUN ANAHTARINDAN alır. Gerçek tarayıcıda
/// telefon başa alınınca bakiye uzun adın üstüne biniyordu (K9 binen=1).
/// </summary>
public sealed class GridKartCssTestleri
{
    private static string Yorumsuz(string css) =>
        System.Text.RegularExpressions.Regex.Replace(css, @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);

    /// <summary>"@media (max-width: N)" bloğunun gövdesi (iç içe süslü parantez sayılarak).</summary>
    private static string MedyaBlogu(string css, string medya)
    {
        var bas = css.IndexOf(medya, StringComparison.Ordinal);
        Assert.True(bas >= 0, medya + " bulunamadı");
        var ac = css.IndexOf('{', bas);
        var derinlik = 0;
        for (var i = ac; i < css.Length; i++)
        {
            if (css[i] == '{') derinlik++;
            else if (css[i] == '}' && --derinlik == 0) return css[(ac + 1)..i];
        }
        throw new Xunit.Sdk.XunitException(medya + " kapanmıyor");
    }

    [Fact]
    public void GC01_Ortak_kart_kalibi_birincil_ustte_sabit_altta_ORDER_ile()
    {
        var blok = MedyaBlogu(Yorumsuz(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "izgara.css")), "@media (max-width: 560px)");
        Assert.Matches(@"td\[data-izgara-rol=""birincil""\]\)\s*\{[^}]*order:\s*-1;[^}]*grid-column:\s*1\s*/\s*-1", blok);
        Assert.Matches(@"td\[data-izgara-rol=""sabit""\]\)\s*\{[^}]*order:\s*1;[^}]*grid-column:\s*1\s*/\s*-1", blok);
    }

    [Fact]
    public void GC02_Cariler_sayfaya_ozgu_kart_kurali_KALMADI_ortak_kalip_yonetir()
    {
        // GRID-1 tur 3 (doğrulayıcı YÜKSEK): ortak.css #cari-liste-sar (kimlik özgüllüğü) ve
        // tuval-panel-musteri.css musteri-tablo kart kuralları ortak kalıbı eziyordu → telefon ile
        // tutar aynı satırda bitişik, etiketler çapraz. Kart düzeni YALNIZ izgara.css'te.
        var ortak = Yorumsuz(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "ortak.css"));
        Assert.DoesNotMatch(@"#cari-liste-sar\s*>\s*\.veri-tablosu[^{]*>\s*tbody\s*>\s*tr\s*\{[^}]*display:\s*grid", ortak);
        var tuval = MedyaBlogu(Yorumsuz(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tuval-panel-musteri.css")), "@media (max-width: 768px)");
        Assert.DoesNotMatch(@"musteri-tablo[^{]*>\s*tbody\s*>\s*tr[^{]*\{[^}]*display:\s*grid", tuval);
        Assert.DoesNotMatch(@"musteri-tablo[^{]*td[^{]*\{[^}]*grid-column", tuval);
    }

    [Fact]
    public void GC03_Cariler_kart_kurallarinin_HICBIRI_sira_secicisiyle_yer_vermez_PRO_dahil()
    {
        var kok = Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "wwwroot", "css");
        var ihlal = new List<string>();
        foreach (var f in Directory.GetFiles(kok, "*.css"))
        {
            var css = Yorumsuz(File.ReadAllText(f));
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(css,
                         @"(cari-liste-sar|musteri-tablo|izgara-tablo)[^{}]*td:(first-child|last-child(?!:has\(> \.satir-eylem\)))[^{}]*\{[^}]*grid-(column|row)"))
                // İstisna: olcek.css "td:last-child:has(> .satir-eylem)" İÇERİĞE bağlıdır; eylem sütunu Tasinamaz (hep sonda).
                ihlal.Add(Path.GetFileName(f) + ": " + m.Value[..Math.Min(120, m.Value.Length)]);
        }
        Assert.Empty(ihlal);
    }

    [Fact]
    public void GC04_Kart_kalibi_TEK_SUTUN_her_deger_kendi_satirinda_etiket_solda_deger_sagda()
    {
        var blok = MedyaBlogu(Yorumsuz(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "izgara.css")), "@media (max-width: 560px)");
        // Satır tek sütun: iki değer yan yana gelmez (telefon | tutar bitişikliği).
        Assert.Matches(@"izgara-tablo\s*>\s*tbody\s*>\s*tr\s*\{[^}]*grid-template-columns:\s*minmax\(0,\s*1fr\);", blok);
        // Etiketli hücre: kendi içinde 2 sütun — ::before (etiket) 1. sütunda, içerik 2. sütunda sağa yaslı.
        Assert.Matches(@"td\[data-etiket\]\s*\{[^}]*display:\s*grid;[^}]*grid-template-columns:", blok);
        Assert.Matches(@"td\[data-etiket\]::before\s*\{[^}]*content:\s*attr\(data-etiket\);[^}]*grid-column:\s*1;", blok);
        Assert.Matches(@"td\[data-etiket\]\s*>\s*\*\s*\{[^}]*grid-column:\s*2;[^}]*justify-self:\s*end;", blok);
        // 375'te yön düğmeleri dikey adlarını gösterir.
        Assert.Matches(@"\.izgara-yon-yatay\s*\{\s*display:\s*none;", blok);
        Assert.Matches(@"\.izgara-yon-dikey\s*\{\s*display:\s*inline;", blok);
    }

    [Fact]
    public void GC05_Sayisal_baslik_ve_deger_saga_tutar_ustte_rozet_altta_sabit()
    {
        var css = Yorumsuz(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "izgara.css"));
        Assert.Matches(@"th\.izgara-sayi,\s*\.izgara-tablo\s*>\s*tbody\s*>\s*tr\s*>\s*td\.izgara-sayi\s*\{\s*text-align:\s*right;", css);
        Assert.Matches(@"th\.izgara-sayi\s*>\s*\.izgara-sirala\s*\{\s*flex-direction:\s*row-reverse;", css);
        Assert.Matches(@"\.izgara-tutar\.izgara-tutar\.izgara-tutar\.izgara-tutar\s*\{[^}]*flex-direction:\s*column;[^}]*align-items:\s*flex-end;[^}]*flex-wrap:\s*nowrap;", css);
        Assert.Matches(@"\.izgara-tutar-deger\s*\{[^}]*tabular-nums", css);
        // Araç satırı tek satır (onay/sayı satırı tabloyu itmez); panel açılır kutu.
        Assert.Matches(@"\.izgara-arac\s*\{[^}]*flex-wrap:\s*nowrap;[^}]*min-height:", css);
        Assert.Matches(@"\.izgara-panel\s*\{[^}]*position:\s*absolute;", css);
    }
}

/// <summary>
/// GRID-1 tur 3 (canlı bulundu, 375 BASİT): PRO→BASİT geçişinde telefon td'si class/data-kolon/
/// data-etiket özniteliklerini kaybediyordu. Sebep: iki ayrı td dalı (tıklanabilir / değil) AYNI
/// @key'i taşıyordu; tarayıcı tarafı DOM yaması öğeyi yeniden kullanıp öznitelikleri sildi.
/// bUnit kendi DOM'unu baştan ürettiği için bunu YAKALAYAMAZ (GE23 yeşil kalır — mutasyon M3-05
/// ile görüldü) → kural kaynakta kilitlenir: hücre dallarının anahtarları AYRIŞIR.
/// </summary>
public sealed class GridKaynakTestleri
{
    [Fact]
    public void GS01_Hucre_dallari_AYRI_key_tasir_mod_gecisinde_oznitelik_kaybi_olmaz()
    {
        var razor = CssKaynak.Metin("Kuyumcu.Web", "Bilesenler", "Grid", "Izgara.razor");
        var anahtarlar = System.Text.RegularExpressions.Regex.Matches(razor, @"<td @key=""([^""]+)""")
            .Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(2, anahtarlar.Count);
        Assert.Equal(2, anahtarlar.Distinct(StringComparer.Ordinal).Count());
    }
}
