using System.Net;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PATCH-4 (02.10) — VERİ KRİTİK: ürün düzenle Kaydet, DOKUNULMAYAN gerçek milyemi
// sessizce ayarın standardına çeviriyordu (920 → 916; has gram ve stoktaki has toplamı
// değişiyordu). Sebep iki taraflı: (a) GET yanıtında gercekMilyem yoktu, çekmece hep
// boş açılıyor ve her kayıtta null gönderiyordu; (b) sunucu "ayarId gövdede var" =
// "ayar değişti" sayıyordu. Ayrıca PATCH-3 kalanları: ödeme bacağı kolon tavanı, rezerv
// geçmiş bitiş günü alanı, satış fişi istemci tutar tavanı.
// Her HTTP vakası: kod + alan (gövde AYRIŞTIRILARAK) ve veritabanında satır önce/sonra.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class PatchDortHttpTestleri(HttpFikstur f)
{
    private const decimal TarihselStokHas = 920.123456m;   // stok has toplamı hareketlerden birikmiş (adet × has DEĞİL)

    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"PATCH4 {ad}");
    }

    /// <summary>Ürünü 920 milyemli (22 ayar, standart 916) yapar; stok has toplamı tarihsel bir değer.</summary>
    private void Milyem920(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var u = db.Urunler.Single(x => x.Id == k.UrunId);
        u.Milyem = 920m; u.HasGrami = 9.2m; u.Barkod = "B-ESKI";
        var s = db.Stoklar.Single(x => x.UrunId == k.UrunId);
        s.ToplamHasGram = TarihselStokHas;
        db.SaveChanges();
    }

    private (decimal Milyem, decimal Has, decimal StokHas, string? Barkod, string AyarId) Durum(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var u = db.Urunler.AsNoTracking().Single(x => x.Id == k.UrunId);
        var s = db.Stoklar.AsNoTracking().Single(x => x.UrunId == k.UrunId);
        return (u.Milyem, u.HasGrami, s.ToplamHasGram, u.Barkod, u.AyarId);
    }

    private static Task<HttpResponseMessage> Ham(HttpClient c, HttpMethod m, string yol, string json) =>
        c.SendAsync(new HttpRequestMessage(m, yol) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    private static async Task Alan400(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), "400 gövdesinde 'alanlar' yok: " + govde);
        var anahtarlar = alanlar.EnumerateObject().Select(p => p.Name).ToList();
        Assert.True(anahtarlar.Any(a => a.Equals(alan, StringComparison.OrdinalIgnoreCase)),
            $"400 '{alan}' alanına işaretli değil: {string.Join(",", anahtarlar)}");
    }

    /// <summary>Ekranın (Urunler.razor UrunGuncelle) değişmeyen gerçek milyemle gönderdiği TAM gövde — yalnız barkod farklı.</summary>
    private static Dictionary<string, object?> EkraninTamGovdesi(Kiraci k, string barkod) => new()
    {
        ["tip"] = "ZIYNET", ["kod"] = null, ["ad"] = null, ["ayarId"] = "K22", ["rodyumKaplama"] = false,
        ["birim"] = "ADET", ["gramAgirlik"] = 10m, ["tasAgirlik"] = 0m, ["tasKokeni"] = "TAS_YOK",
        ["tasIbaresi"] = null, ["kategoriId"] = null, ["reyonId"] = null, ["iscilikTl"] = 0m,
        ["satisFiyati"] = null, ["barkod"] = barkod,
        ["pirlantaKarat"] = null, ["pirlantaRenk"] = null, ["pirlantaBerraklik"] = null,
        ["pirlantaKesim"] = null, ["sertifikaKurumu"] = null, ["sertifikaNo"] = null,
        ["konsinye"] = false, ["konsinyeCariId"] = null, ["aciklama"] = null, ["pariteKodu"] = null,
        ["rfid"] = null, ["gorselUrl"] = null, ["tedarikciId"] = null, ["alisMaliyeti"] = null,
        ["hedefMarj"] = null, ["kdvOrani"] = null, ["seriNo"] = null, ["olcu"] = null,
        ["minStok"] = null, ["konsinyeBirimBedel"] = null,
    };

    // ═══════════ ÖNCELİK 1 — GERÇEK MİLYEM KORUNUR ═══════════

    [Fact]
    public async Task P4_urun_PATCH_ekranin_TAM_govdesi_gercek_milyem_920_KORUNUR_has_ve_stok_has_DEGISMEZ()
    {
        var k = Kur("tam govde");
        Milyem920(k);
        var g = EkraninTamGovdesi(k, "B-YENI");
        g.Remove("kod"); g.Remove("ad");   // zorunlu alanlar: null = koru, ama ekran dolu gönderir; burada gövdeden çıkarıldı
        var r = await f.Ortam.Istemci(k).PatchJson($"/api/v1/urunler/{k.UrunId}", g);
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");

        var d = Durum(k);
        Assert.Equal(920m, d.Milyem);
        Assert.Equal(9.2m, d.Has);
        Assert.Equal(TarihselStokHas, d.StokHas);   // has değişmedi → stok has toplamına DOKUNULMAZ
        Assert.Equal("B-YENI", d.Barkod);
        // PATCH yanıtı da kayıtlı gerçek milyemi döndürür (çekmece yeniden açılınca dolu gelsin)
        var y = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(920m, y.GetProperty("gercekMilyem").GetDecimal());
    }

    [Fact]
    public async Task P4_urun_PATCH_ayni_ayarId_ve_ayni_gram_tek_basina_HAS_YENIDEN_HESAPLATMAZ()
    {
        var k = Kur("ayni ayar");
        Milyem920(k);
        var r = await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/urunler/{k.UrunId}",
            """{"ayarId":"K22","birim":"ADET","gramAgirlik":10,"tasAgirlik":0}""");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var d = Durum(k);
        Assert.Equal((920m, 9.2m, TarihselStokHas), (d.Milyem, d.Has, d.StokHas));
    }

    [Fact]
    public async Task P4_urun_PATCH_gercekMilyem_null_STANDARDA_doner_has_ve_stok_has_yeniden_hesaplanir()
    {
        var k = Kur("null standart");
        Milyem920(k);
        var r = await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/urunler/{k.UrunId}",
            """{"ayarId":"K22","gercekMilyem":null}""");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var d = Durum(k);
        Assert.Equal(916m, d.Milyem);
        Assert.Equal(9.16m, d.Has);
        Assert.Equal(916m, d.StokHas);   // 100 adet × 9,16
        var y = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(JsonValueKind.Null, y.GetProperty("gercekMilyem").ValueKind);   // standart → null
    }

    [Fact]
    public async Task P4_urun_PATCH_gercekMilyem_DEGER_yazar_has_yeniden_hesaplanir()
    {
        var k = Kur("deger");
        var r = await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/urunler/{k.UrunId}",
            """{"gercekMilyem":918}""");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var d = Durum(k);
        Assert.Equal((918m, 9.18m, 918m), (d.Milyem, d.Has, d.StokHas));
    }

    [Fact]
    public async Task P4_urun_PATCH_ayar_GERCEKTEN_degisirse_gercek_milyem_verilmediyse_YENI_AYARIN_standardi()
    {
        var k = Kur("ayar degisti");
        Milyem920(k);
        var k18 = Kuyumcu.Core.Domain.Ayarlar.Bul("K18")!.Milyem;
        var r = await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/urunler/{k.UrunId}",
            """{"ayarId":"K18","gramAgirlik":10}""");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var d = Durum(k);
        Assert.Equal("K18", d.AyarId);
        Assert.Equal(k18, d.Milyem);
        Assert.Equal(10m * k18 / 1000m, d.Has);
    }

    [Fact]
    public async Task P4_urun_GET_ve_LISTE_gercekMilyem_DONDURUR_standartsa_null()
    {
        var k = Kur("get liste");
        var c = f.Ortam.Istemci(k);
        var y1 = await c.GetFromJsonAsync<JsonElement>($"/api/v1/urunler/{k.UrunId}");
        Assert.Equal(JsonValueKind.Null, y1.GetProperty("gercekMilyem").ValueKind);
        Assert.Equal(916m, y1.GetProperty("milyem").GetDecimal());   // varlığın kendi alanları korunur

        Milyem920(k);
        var y2 = await c.GetFromJsonAsync<JsonElement>($"/api/v1/urunler/{k.UrunId}");
        Assert.Equal(920m, y2.GetProperty("gercekMilyem").GetDecimal());
        Assert.Equal(k.UrunId, y2.GetProperty("id").GetString());

        var liste = await c.GetFromJsonAsync<JsonElement>("/api/v1/urunler?limit=50");
        var satir = liste.EnumerateArray().Single(x => x.GetProperty("id").GetString() == k.UrunId);
        Assert.Equal(920m, satir.GetProperty("gercekMilyem").GetDecimal());
        Assert.Equal("K22", satir.GetProperty("ayarId").GetString());
    }

    /// <summary>
    /// KALIP TARAMASI ("her kayıtta gönderilen ama sunucudan dolmayan alan"): ekranın
    /// UrunGuncelle gövdesindeki HER anahtar GET /urunler/{id} yanıtında da bulunmalı —
    /// yoksa çekmece o alanı boş açar ve Kaydet kayıtlı değeri ezer (gercekMilyem kusuru).
    /// Anahtarlar kaynak dosyadan okunur: ekrana yeni alan eklenip yanıtta yoksa test kırmızı.
    /// </summary>
    [Fact]
    public async Task P4_duzenle_govdesindeki_HER_anahtar_GET_yanitinda_VAR_sunucudan_dolmayan_alan_YOK()
    {
        var kaynak = File.ReadAllText(Path.Combine(Kok(), "Kuyumcu.Web", "Pages", "Urunler.razor"));
        var bas = kaynak.IndexOf("private async Task UrunGuncelle()", StringComparison.Ordinal);
        Assert.True(bas > 0);
        var son = kaynak.IndexOf("await Api.PatchAsync($\"/urunler/{dId}\", govde);", bas, StringComparison.Ordinal);
        Assert.True(son > bas);
        var govdeKodu = kaynak[bas..son];
        var anahtarlar = Regex.Matches(govdeKodu, "\\[\"([a-zA-Z]+)\"\\]\\s*=").Select(m => m.Groups[1].Value)
            .Distinct().ToList();
        Assert.Contains("gercekMilyem", anahtarlar);
        Assert.True(anahtarlar.Count >= 35, $"anahtar sayısı beklenenden az: {anahtarlar.Count}");

        var k = Kur("kalip");
        var y = await f.Ortam.Istemci(k).GetFromJsonAsync<JsonElement>($"/api/v1/urunler/{k.UrunId}");
        var eksik = anahtarlar.Where(a => !y.TryGetProperty(a, out _)).ToList();
        Assert.True(eksik.Count == 0, "GET yanıtında olmayan gövde anahtarları: " + string.Join(",", eksik));
    }

    private static string Kok()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        Assert.True(d is not null, "Kuyumcu.sln bulunamadı.");
        return d!.FullName;
    }

    // ═══════════ PATCH-3 KALANLARI ═══════════

    [Theory]
    [InlineData("tutarTl", """{"arac":"NAKIT","tutarTl":1e20,"kasaId":"__KASA__"}""")]
    [InlineData("dovizTutar", """{"arac":"DOVIZ","tutarTl":100,"kasaId":"__KASA__","paraBirimi":"USD","dovizTutar":1e20}""")]
    [InlineData("gramAgirlik", """{"arac":"ALTIN","tutarTl":100,"ayarId":"K22","gramAgirlik":1e20,"birimFiyatTlHasGram":2000}""")]
    [InlineData("birimFiyatTlHasGram", """{"arac":"ALTIN","tutarTl":100,"ayarId":"K22","gramAgirlik":5,"birimFiyatTlHasGram":1e20}""")]
    public async Task P4_satis_fisi_odeme_bacagi_kolon_tavani_400_ALANLI_hareket_YAZILMAZ(string alan, string bacak)
    {
        var k = Kur("odeme tavan " + alan);
        var json = "{\"cariId\":\"" + k.CariId + "\",\"kurSnapshotId\":\"" + k.KurId +
                   "\",\"kalemler\":[{\"urunId\":\"" + k.UrunId + "\",\"adet\":1,\"birimFiyatTl\":100}],\"odemeler\":[" +
                   bacak.Replace("__KASA__", k.KasaId) + "]}";
        var r = await Ham(f.Ortam.Istemci(k), HttpMethod.Post, "/api/v1/islemler/satis-fisi", json);
        await Alan400(r, $"odemeler[0].{alan}");
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.Hareketler.AsNoTracking().Count(h => h.TenantId == k.TenantId));
    }

    [Fact]
    public async Task P4_rezerv_gecmis_bitis_gunu_OLUSTUR_ve_PATCH_400_alan_bitisGunu_SQL_DEGISMEZ()
    {
        var k = Kur("rezerv bitis");
        var c = f.Ortam.Istemci(k);
        await Alan400(await c.PostJson("/api/v1/islemler/rezervler",
            new { urunId = k.UrunId, cariId = k.CariId, adet = 1m, bitisGunu = "2000-01-01" }), "bitisGunu");

        var ok = await c.PostJson("/api/v1/islemler/rezervler", new { urunId = k.UrunId, cariId = k.CariId, adet = 1m });
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        string id; DateOnly? once;
        using (var db = f.Vt.OlusturSistem())
        {
            var z = db.Rezervasyonlar.AsNoTracking().Single(x => x.TenantId == k.TenantId);
            id = z.Id; once = z.BitisGunu;
        }
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/islemler/rezervler/{id}",
            """{"bitisGunu":"2000-01-01"}"""), "bitisGunu");
        using var db2 = f.Vt.OlusturSistem();
        Assert.Equal(once, db2.Rezervasyonlar.AsNoTracking().Single(x => x.Id == id).BitisGunu);
    }

    /// <summary>
    /// PATCH-4 t2 (hasım KRİTİK): 2'den fazla ondalık milyem (916,555) ARALIKTA olsa da kolon
    /// decimal(7,2) — sessiz yuvarlama yerine 400 alan:gercekMilyem; veri DEĞİŞMEZ. Bunu yalnız
    /// SERVİS içi kapı yakalar (DTO Range aralığa bakar). POST da aynı kapıdan geçer.
    /// </summary>
    [Fact]
    public async Task P4_gercek_milyem_UC_ondalik_PATCH_ve_POST_400_veri_DEGISMEZ()
    {
        var k = Kur("uc ondalik");
        Milyem920(k);
        var once = Durum(k);
        var c = f.Ortam.Istemci(k);
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/urunler/{k.UrunId}", """{"gercekMilyem":916.555}"""), "gercekMilyem");
        Assert.Equal(once, Durum(k));
        await Alan400(await Ham(c, HttpMethod.Post, "/api/v1/urunler",
            """{"tip":"ZIYNET","kod":"P4-UC","ad":"P4 uc","ayarId":"K22","gramAgirlik":10,"birim":"ADET","tasKokeni":"TAS_YOK","gercekMilyem":916.555}"""), "gercekMilyem");
        using var db = f.Vt.OlusturSistem();
        Assert.False(db.Urunler.AsNoTracking().Any(x => x.TenantId == k.TenantId && x.Kod == "P4-UC"));
    }
}

/// <summary>PATCH-4 t2 — gerçek milyem kapısı (servis içi + DTO) SAF sınır tablosu.</summary>
public sealed class PatchDortMilyemKapisiTestleri
{
    [Theory]
    [InlineData("1", true)] [InlineData("1000", true)] [InlineData("916.5", true)] [InlineData("999.99", true)]
    [InlineData("0.99", false)] [InlineData("0.6", false)] [InlineData("1000.01", false)] [InlineData("1000.4", false)]
    [InlineData("916.555", false)] [InlineData("-920", false)] [InlineData("0", false)]
    public void P4_GercekMilyemKapisi_SERVIS_ici_sinir_tablosu(string deger, bool gecer)
    {
        var m = decimal.Parse(deger, System.Globalization.CultureInfo.InvariantCulture);
        var hata = Record.Exception(() => Kuyumcu.Api.Modules.Urun.UrunServisi.GercekMilyemKapisi(m));
        if (gecer) Assert.Null(hata);
        else
        {
            var b = Assert.IsType<Kuyumcu.Api.Common.BadRequestException>(hata);
            Assert.Equal("gercekMilyem", Kuyumcu.Api.Common.AlanIsareti.Alan(b));
        }
        Kuyumcu.Api.Modules.Urun.UrunServisi.GercekMilyemKapisi(null);   // verilmedi/null serbest
    }

    [Theory]
    [InlineData("1000.4")] [InlineData("1000.01")] [InlineData("0.6")]
    public void P4_DTO_Range_ONDALIGI_yuvarlamadan_sinar(string deger)
    {
        var m = decimal.Parse(deger, System.Globalization.CultureInfo.InvariantCulture);
        var o = new Kuyumcu.Api.Modules.Urun.UrunOlusturDto { Tip = "ZIYNET", Kod = "X", Ad = "X", AyarId = "K22", GercekMilyem = m };
        var g = new Kuyumcu.Api.Modules.Urun.UrunGuncelleDto { GercekMilyem = m };
        foreach (var dto in new object[] { o, g })
        {
            var sonuc = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto,
                new System.ComponentModel.DataAnnotations.ValidationContext(dto), sonuc, validateAllProperties: true);
            Assert.Contains(sonuc, r => r.MemberNames.Contains("GercekMilyem"));
        }
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// EKRAN — düzenleme çekmecesi kayıtlı gerçek milyemi GÖSTERİR ve değişmediyse GÖNDERMEZ.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class PatchDortUrunEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string Liste = """
        [{"id":"u1","tip":"ZIYNET","kod":"BLZ-01","ad":"Burma Bilezik","ayarId":"K22",
          "gramAgirlik":10,"tasAgirlik":0,"hasGrami":9.2,"barkod":"869001","aktif":true,"gercekMilyem":920}]
        """;

    // Sunucunun GERÇEK yanıt biçimi: varlık (milyem) + PATCH-4 gercekMilyem.
    private const string TamKart = """
        {"id":"u1","tip":"ZIYNET","kod":"BLZ-01","ad":"Burma Bilezik","ayarId":"K22","milyem":920,
         "gercekMilyem":920,"gramAgirlik":10,"tasAgirlik":0,"iscilikTl":0,"barkod":"869001",
         "tasKokeni":"TAS_YOK","kategoriId":null,"reyonId":null,"satisFiyati":null,"birim":"ADET"}
        """;

    public PatchDortUrunEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            var y = yol switch
            {
                var v when v.EndsWith("/kmts/ayar") => """{"acik":false,"bicimDenetimi":false}""",
                var v when v.Contains("/kur/pariteler") => """{"kaynak":"X","pariteler":[]}""",
                var v when v.Contains("/belgeler") => "[]",
                var v when v.EndsWith("/urunler/madenler") => """
                    {"madenler":[{"maden":"ALTIN","madenAdi":"Altın","rodyumKaplanabilir":true,
                       "ayarlar":[{"id":"K22","ad":"22 Ayar","milyem":916,"kiymetli":true,"hasHesaplanir":true},
                                  {"id":"K18","ad":"18 Ayar","milyem":750,"kiymetli":true,"hasHesaplanir":true}]}],
                     "etiket":{"rodyumIbaresiGoster":false,"rodyumIbaresi":"rodyum kaplama"}}
                    """,
                var v when v.Contains("/urun-kategorileri") => "[]",
                var v when v.Contains("/reyonlar") => "[]",
                var v when v.EndsWith("/cari") || v.Contains("/cari?") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.EndsWith("/urunler/u1") => TamKart,
                var v when v.Contains("/urunler") => Liste,
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(y, Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Urunler> DuzenleAc()
    {
        var e = Render<Kuyumcu.Web.Pages.Urunler>().AyrintiAc();
        e.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();
        e.WaitForElement("#d-barkod");
        // BASİT modda gerçek milyem "Daha fazla" katında; PRO'da doğrudan görünür.
        if (e.FindAll("#d-gercek-milyem").Count == 0)
            e.Find("#d-daha-fazla").Click();
        e.WaitForElement("#d-gercek-milyem");
        return e;
    }

    private JsonElement PatchGovdesi()
    {
        var (_, _, g) = istekler.Single(i => i.Metot == "PATCH");
        return JsonDocument.Parse(g).RootElement;
    }

    [Fact]
    public void P4_duzenle_acilinca_d_gercek_milyem_KAYITLI_920_gosterir()
    {
        var e = DuzenleAc();
        var kutu = e.Find("#d-gercek-milyem");
        Assert.Equal("920", kutu.GetAttribute("data-deger"));
    }

    [Fact]
    public void P4_yalniz_barkod_degisince_govdede_gercekMilyem_ANAHTARI_YOK_ayarId_ayni()
    {
        var e = DuzenleAc();
        e.Find("#d-barkod").Input("Y-1");
        istekler.Clear();
        e.Find("#cekmece-urun-kaydet").Click();
        var g = PatchGovdesi();
        Assert.False(g.TryGetProperty("gercekMilyem", out _), "değişmeyen gerçek milyem gövdeye konmamalı");
        Assert.Equal("K22", g.GetProperty("ayarId").GetString());
        Assert.Equal("Y-1", g.GetProperty("barkod").GetString());
    }

    [Fact]
    public void P4_gercek_milyem_BOSALTILINCA_govdede_NULL_gider_standarda_don()
    {
        var e = DuzenleAc();
        e.Find("#d-gercek-milyem").Input("");
        istekler.Clear();
        e.Find("#cekmece-urun-kaydet").Click();
        Assert.Equal(JsonValueKind.Null, PatchGovdesi().GetProperty("gercekMilyem").ValueKind);
    }

    [Fact]
    public void P4_gercek_milyem_DEGISINCE_yeni_deger_gider()
    {
        var e = DuzenleAc();
        e.Find("#d-gercek-milyem").Input("918");
        istekler.Clear();
        e.Find("#cekmece-urun-kaydet").Click();
        Assert.Equal(918m, PatchGovdesi().GetProperty("gercekMilyem").GetDecimal());
    }

    [Fact]
    public void P4_ayar_degisince_eski_ayarin_gercek_milyemi_TASINMAZ_kutu_bosalir_geri_donunce_gelir()
    {
        var e = DuzenleAc();
        SeciciYardim.Sec(e, "d-ayar", "K18");
        e.WaitForAssertion(() => Assert.Equal("", e.Find("#d-gercek-milyem").GetAttribute("data-deger") ?? ""));
        SeciciYardim.Sec(e, "d-ayar", "K22");
        e.WaitForAssertion(() => Assert.Equal("920", e.Find("#d-gercek-milyem").GetAttribute("data-deger")));
        SeciciYardim.Sec(e, "d-ayar", "K18");
        istekler.Clear();
        e.Find("#cekmece-urun-kaydet").Click();
        var g = PatchGovdesi();
        Assert.Equal("K18", g.GetProperty("ayarId").GetString());
        Assert.Equal(JsonValueKind.Null, g.GetProperty("gercekMilyem").ValueKind);   // 22 ayarın 920'si 18 ayara GİTMEZ
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// SATIŞ FİŞİ — istemci tutar tavanı (sunucu kolon tavanıyla TEK sınır).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class PatchDortFisTavanTestleri : BunitContext
{
    public PatchDortFisTavanTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        GocC2Ortak.IzgaraServisleri(Services);   // GOC-C-2: kalem tablosu ortak ızgarada (ApiIstemci ister)
    }

    [Fact]
    public void P4_istemci_tutar_tavani_SUNUCU_kolon_tavaniyla_BIREBIR()
        => Assert.Equal(Kuyumcu.Api.Modules.KolonTavani.D15_2, Kuyumcu.Core.I18n.PatchDortSozlugu.TutarTavani);

    [Theory]
    [InlineData(1, 9_999_999_999_999.99, false)]
    [InlineData(1, 10_000_000_000_000, true)]
    [InlineData(2, 5_000_000_000_000, true)]
    [InlineData(3, 1_000, false)]
    [InlineData(0.5, 12_000_000_000_000, true)]   // GRAM ürün yarım gram: satır tavan altında ama BİRİM FİYAT kolona sığmaz
    public void P4_tavan_birim_fiyat_ve_SATIR_TUTARI_icin_isler(double adet, double fiyat, bool asildi)
        => Assert.Equal(asildi, Kuyumcu.Web.Bilesenler.Fis.FisKalemGirisi.TavanAsildi((decimal)adet, (decimal)fiyat));

    [Fact]
    public void P4_tavani_asan_fiyatla_Ekle_KALEM_EKLEMEZ_fiyat_alti_TURKCE_mesaj()
    {
        var form = new FormDurumu();
        var ekleCagrildi = 0;
        var giris = new Kuyumcu.Web.Bilesenler.Fis.KalemGirisi { UrunId = "u1", Adet = 1m, BirimFiyat = 99_999_999_999_999m };
        var e = Render<Kuyumcu.Web.Bilesenler.Fis.FisKalemGirisi>(p => p
            .Add(x => x.Giris, giris)
            .Add(x => x.UrunSecenekleri, new List<Kuyumcu.Web.Bilesenler.SecenekOgesi>())
            .Add(x => x.Form, form)
            .Add(x => x.Ekle, () => { ekleCagrildi++; }));
        e.Find("#islem-kalem-ekle").Click();
        Assert.Equal(0, ekleCagrildi);
        var mesaj = form.Mesaj("islem-birimfiyat");
        Assert.NotNull(mesaj);
        Assert.Equal("Tutar çok büyük — satır tutarı en çok 13 haneli olabilir.", mesaj);
        // "13 hane" tavanın gerçek tam sayı hanesiyle tutarlı (9.999.999.999.999 → 13 hane)
        Assert.Equal(13, decimal.Truncate(Kuyumcu.Core.I18n.PatchDortSozlugu.TutarTavani).ToString(System.Globalization.CultureInfo.InvariantCulture).Length);

        giris.BirimFiyat = 1_000m;
        e.Find("#islem-kalem-ekle").Click();
        Assert.Equal(1, ekleCagrildi);   // tavan altında ekleme SAYFAYA bırakılır
    }

    [Fact]
    public void P4_satir_ici_fiyat_TAVANI_asarsa_KALEM_DEGISMEZ_alan_alti_mesaj()
    {
        var form = new FormDurumu();
        var k = new Kuyumcu.Web.Bilesenler.Fis.FisKalemi { UrunId = "u1", UrunAdi = "Bilezik", Adet = 1m, BirimFiyat = 100m };
        var t = Render<Kuyumcu.Web.Bilesenler.Fis.FisKalemTablosu>(p => p
            .Add(x => x.Kalemler, [k])
            .Add(x => x.Urunler, Array.Empty<Kuyumcu.Contracts.Sayfalar.Islemler.UrunVeri>())
            .Add(x => x.Form, form)
            .Add(x => x.Toplam, 100m));
        t.Find("#islem-kalem-duzenle-0").Click();
        t.Find("#si-kalem-fiyat-u1-ac").Click();
        t.Find("#si-kalem-fiyat-u1").Input("99999999999999");
        t.Find("#si-kalem-fiyat-u1-kaydet").Click();
        Assert.Equal(100m, k.BirimFiyat);
        Assert.Contains("en çok", form.Mesaj("si-kalem-fiyat-u1") ?? "");
    }

    /// <summary>
    /// PATCH-4 t2 (hasım DÜŞÜK): satır içi ADET düzenlemesi de tavana tabidir — 9e12 ₺ fiyatlı
    /// kalemde adet 2 yapılırsa satır 1,8e13 olur → adet DEĞİŞMEZ, alan altında Türkçe ileti.
    /// Tavanın altındaki adet (1,5) ise kabul edilir (kapı fazla geniş değil).
    /// </summary>
    [Fact]
    public void P4_satir_ici_ADET_TAVANI_asarsa_KALEM_DEGISMEZ_alan_alti_mesaj()
    {
        var form = new FormDurumu();
        var k = new Kuyumcu.Web.Bilesenler.Fis.FisKalemi { UrunId = "u1", UrunAdi = "Bilezik", Adet = 1m, BirimFiyat = 9_000_000_000_000m };
        var t = Render<Kuyumcu.Web.Bilesenler.Fis.FisKalemTablosu>(p => p
            .Add(x => x.Kalemler, [k])
            .Add(x => x.Urunler, Array.Empty<Kuyumcu.Contracts.Sayfalar.Islemler.UrunVeri>())
            .Add(x => x.Form, form)
            .Add(x => x.Toplam, 9_000_000_000_000m));
        t.Find("#si-kalem-adet-u1-ac").Click();
        t.Find("#si-kalem-adet-u1").Input("2");
        t.Find("#si-kalem-adet-u1-kaydet").Click();
        Assert.Equal(1m, k.Adet);
        Assert.Equal("Satır tutarı çok büyük.", form.Mesaj("si-kalem-adet-u1"));
        // Ekranda görünür: ileti satırın altındaki tam genişlik satırda (dar adet hücresinde değil).
        // GOC-C-2: kalem tablosu ortak ızgarada — tam genişlik ileti satırı artık ızgaranın ALTINDA div (tr değil; id aynı).
        Assert.Equal("Satır tutarı çok büyük.", t.Find(".fis-pro-satir-hata #alan-hata-si-kalem-adet-u1").TextContent.Trim());

        t.Find("#si-kalem-adet-u1-ac").Click();
        t.Find("#si-kalem-adet-u1").Input("1,1");
        t.Find("#si-kalem-adet-u1-kaydet").Click();
        Assert.Equal(1.1m, k.Adet);
        Assert.Null(form.Mesaj("si-kalem-adet-u1"));
        Assert.Empty(t.FindAll(".fis-pro-satir-hata"));   // ileti yokken boş satır çizilmez (GOC-C-2: div)
    }
}
