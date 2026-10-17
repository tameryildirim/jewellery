using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Cari;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q9-5 — QA turu 9 (YÜKSEK ×4):
//   1) /kasalar: kasanın ŞUBESİ düzenlenebilir (geçmişi boşsa) ya da GEREKÇELİ
//      kilit kaydetmeden ÖNCE görünür (GET /kasalar/{id}/degisim-kilidi);
//      basit modda da (mod ≠ yetki; UX-S26).
//   2) /kasalar: eksi bakiyeli kasa sessiz durmaz — kart rozeti + uyarı + düzeltme
//      yolu, özet kartı kırmızı + sayfa uyarısı.
//   3) /sayim: Şube boşken hata ŞUBENİN altında; dolu Ad işaretlenmez.
//   4) GET /cari?q=: telefonla arama (boşluk / baştaki 0 / +90 fark etmez).
// ⚠ Sahte API "{}" varsayılanıyla bırakılmadı; gövdeler JSON ayrıştırılarak sınanır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Q95CariAramaSafTestleri
{
    [Theory]
    [InlineData("0312 444 33 22", "3124443322")]
    [InlineData("03124443322", "3124443322")]
    [InlineData("+90 312 444 33 22", "3124443322")]
    [InlineData("0090 312 444 33 22", "3124443322")]
    [InlineData("(0312) 444-33-22", "3124443322")]
    [InlineData("905329998877", "5329998877")]
    [InlineData("9998877", "9998877")]
    [InlineData("532", "532")]
    public void Q95_telefon_cekirdegi_bicimden_bagimsizdir(string q, string beklenen)
        => Assert.Equal(beklenen, CariArama.TelefonCekirdegi(q));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Yılmaz")]
    [InlineData("ayse@ornek.com")]
    [InlineData("0")]
    [InlineData("00")]
    [InlineData("12")]
    [InlineData("A-101")]
    public void Q95_telefon_olmayan_ya_da_cok_kisa_arama_TELEFON_aramasi_yapmaz(string? q)
        => Assert.Null(CariArama.TelefonCekirdegi(q));
}

[Collection("http")]
public sealed class Q95HttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static async Task<List<string>> Adlar(HttpResponseMessage y)
    {
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var j = JsonDocument.Parse(await y.Content.ReadAsStringAsync()).RootElement;
        return j.EnumerateArray().Select(x => x.GetProperty("ad").GetString()!).ToList();
    }

    private static async Task<string> CariAc(HttpClient c, string ad, string? telefon, string? email = null)
    {
        var y = await c.PostAsJsonAsync("/api/v1/cari", new { tur = "MUSTERI", ad, telefon, email });
        Assert.True(y.IsSuccessStatusCode, await y.Content.ReadAsStringAsync());
        return JsonDocument.Parse(await y.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Q95_cari_TELEFONLA_bulunur_bosluk_sifir_ve_90_fark_etmez()
    {
        var k = YeniKiraci("Q95H-TEL");
        var c = f.Ortam.Istemci(k);
        await CariAc(c, "Q95 Boşluklu Kayıt", "0312 444 33 22");
        await CariAc(c, "Q95 Bitişik Kayıt", "05329998877");
        await CariAc(c, "Q95 Ülke Kodlu", "+90 216 555 00 33");
        await CariAc(c, "Q95 Başka", "05551234567");

        foreach (var q in new[] { "0312 444 33 22", "03124443322", "+90 312 444 33 22", "4443322" })
        {
            var adlar = await Adlar(await c.GetAsync($"/api/v1/cari?limit=100&q={Uri.EscapeDataString(q)}"));
            Assert.Equal(["Q95 Boşluklu Kayıt"], adlar);
        }
        Assert.Equal(["Q95 Bitişik Kayıt"],
            await Adlar(await c.GetAsync($"/api/v1/cari?q={Uri.EscapeDataString("0532 999 88 77")}")));
        Assert.Equal(["Q95 Ülke Kodlu"],
            await Adlar(await c.GetAsync("/api/v1/cari?q=02165550033")));
        // Adla arama BOZULMADI.
        Assert.Equal(["Q95 Başka"], await Adlar(await c.GetAsync($"/api/v1/cari?q={Uri.EscapeDataString("Başka")}")));
        // Eşleşmeyen numara hiçbirini getirmez (her şeyi döken arama değil).
        Assert.Empty(await Adlar(await c.GetAsync("/api/v1/cari?q=04449999999")));
    }

    [Fact]
    public async Task Q95_cari_EPOSTAYLA_bulunur_ve_baska_kiracinin_telefonu_SIZMAZ()
    {
        var k = YeniKiraci("Q95H-EPOSTA");
        var baska = YeniKiraci("Q95H-BASKA");
        await CariAc(f.Ortam.Istemci(k), "Q95 E-postalı", null, "q95-kisi@ornek.com");
        await CariAc(f.Ortam.Istemci(baska), "Q95 Yabancı", "0312 777 66 55");

        Assert.Equal(["Q95 E-postalı"],
            await Adlar(await f.Ortam.Istemci(k).GetAsync("/api/v1/cari?q=q95-kisi")));
        Assert.Empty(await Adlar(await f.Ortam.Istemci(k).GetAsync("/api/v1/cari?q=03127776655")));
    }

    private async Task<string> TemizKasa(Kiraci k, string ad)
    {
        var y = await f.Ortam.Istemci(k).PostJson("/api/v1/kasalar", new { ad, subeId = k.SubeId, paraBirimi = "TRY" });
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        return (await y.Json()).GetProperty("id").GetString()!;
    }

    private async Task<(bool Kilitli, string? Sebep)> Kilit(Kiraci k, string id, string rol = "SAHIP")
    {
        var y = await f.Ortam.Istemci(k, rol).GetAsync($"/api/v1/kasalar/{id}/degisim-kilidi");
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var j = await y.Json();
        return (j.GetProperty("kilitli").GetBoolean(),
            j.GetProperty("sebep").ValueKind == JsonValueKind.Null ? null : j.GetProperty("sebep").GetString());
    }

    [Fact]
    public async Task Q95_degisim_kilidi_TEMIZ_kasada_serbest_ve_sube_PATCH_ile_TASINIR()
    {
        var k = YeniKiraci("Q95H-KILIT-TEMIZ");
        var id = await TemizKasa(k, "Q95 temiz");
        Assert.Equal((false, null), await Kilit(k, id));
        // Okuma ucu: TEZGAHTAR da kilidi görür (liste/detay gibi).
        Assert.Equal((false, null), await Kilit(k, id, "TEZGAHTAR"));

        string ikinciSube;
        using (var db = f.Vt.Olustur(k))
        {
            ikinciSube = Kimlik.Ulid();
            db.Subeler.Add(new Sube { Id = ikinciSube, TenantId = k.TenantId, Ad = "Q95 Çarşı", Aktif = true,
                Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        var p = await f.Ortam.Istemci(k).PatchAsJsonAsync($"/api/v1/kasalar/{id}", new { subeId = ikinciSube });
        Assert.Equal(HttpStatusCode.OK, p.StatusCode);
        Assert.Equal(ikinciSube, (await p.Json()).GetProperty("subeId").GetString());
    }

    [Fact]
    public async Task Q95_degisim_kilidi_BAKIYE_HAREKET_GUN_KAPANISI_sebebini_soyler_PATCH_ile_AYNI_kural()
    {
        var k = YeniKiraci("Q95H-KILIT-SEBEP");
        // TestKurulum kasası 250.000 TL bakiyeli → BAKIYE.
        Assert.Equal((true, "BAKIYE"), await Kilit(k, k.KasaId));

        // Bakiye 0, hareket var → HAREKET.
        var hareketli = await TemizKasa(k, "Q95 hareketli");
        using (var db = f.Vt.Olustur(k))
        {
            db.KasaHareketleri.Add(new KasaHareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, KasaId = hareketli,
                Tutar = 0m, ParaBirimi = "TRY", OdemeTuru = "NAKIT",
                Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }
        Assert.Equal((true, "HAREKET"), await Kilit(k, hareketli));

        // Hareketsiz ama gün kapanışlı → GUN_KAPANISI.
        var kapanisli = await TemizKasa(k, "Q95 kapanışlı");
        var gk = await f.Ortam.Istemci(k).PostJson($"/api/v1/kasalar/{kapanisli}/gun-kapanisi", new { sayilanNakit = 0m });
        Assert.Equal(HttpStatusCode.OK, gk.StatusCode);
        Assert.Equal((true, "GUN_KAPANISI"), await Kilit(k, kapanisli));

        // Kilit ucunun söylediği = PATCH'in uyguladığı (tek kaynak): 409.
        var p = await f.Ortam.Istemci(k).PatchAsJsonAsync($"/api/v1/kasalar/{hareketli}", new { paraBirimi = "USD" });
        Assert.Equal(HttpStatusCode.Conflict, p.StatusCode);
    }

    [Fact]
    public async Task Q95_degisim_kilidi_baska_kiracinin_kasasi_404()
    {
        var k = YeniKiraci("Q95H-KILIT-A");
        var b = YeniKiraci("Q95H-KILIT-B");
        var y = await f.Ortam.Istemci(b).GetAsync($"/api/v1/kasalar/{k.KasaId}/degisim-kilidi");
        Assert.Equal(HttpStatusCode.NotFound, y.StatusCode);
    }
}

public sealed class Q95KasalarEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly SahteApi sahte;

    public Q95KasalarEkranTestleri()
    {
        sahte = new SahteApi(istekler);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(sahte) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        public string Subeler = """[{"id":"s1","ad":"Merkez Şube"},{"id":"s2","ad":"Çarşı Şube"}]""";
        public string Kasalar = """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":-738298.80,"aktif":true,"subeId":"s1"},{"id":"k2","ad":"Yeni Kasa","paraBirimi":"TRY","bakiye":0,"aktif":true,"subeId":"s1"},{"id":"k3","ad":"Döviz","paraBirimi":"USD","bakiye":100,"aktif":true,"subeId":"s1"}]""";
        /// <summary>kasaId → kilit yanıtı; yoksa serbest.</summary>
        public Dictionary<string, string> Kilit = new() { ["k1"] = """{"kasaId":"k1","kilitli":true,"sebep":"HAREKET"}""" };
        public bool KilitHata;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            var get = istek.Method == HttpMethod.Get;
            if (yol.Contains("/degisim-kilidi") && KilitHata)
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                { Content = new StringContent("""{"message":"okunamadı"}""", System.Text.Encoding.UTF8, "application/json") };
            var y = yol switch
            {
                var v when v.Contains("/degisim-kilidi") =>
                    Kilit.FirstOrDefault(p => v.Contains($"/kasalar/{p.Key}/")).Value
                    ?? $$"""{"kasaId":"x","kilitli":false,"sebep":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur1","altinHasFiyati":4100}""",
                var v when v.Contains("/subeler") => Subeler,
                var v when get && v.Contains("/hareketler") => """{"hareketler":[]}""",
                var v when get && v.EndsWith("/kasalar") => Kasalar,
                var v when !get && v.Contains("/kasalar/k2") => """{"id":"k2","ad":"Yeni Kasa","paraBirimi":"TRY","bakiye":0,"aktif":true,"subeId":"s2"}""",
                var v when v.Contains("/kasalar/") => """{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":-738298.80,"aktif":true,"subeId":"s1"}""",
                _ => "[]",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Kasalar> Ciz(string mod = ArayuzModuServisi.Basit, string dil = "tr")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Kuyumcu.Web.Pages.Kasalar>().AyrintiAc();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kasa-liste")));
        return e;
    }

    private static KasaSubeKilitMetinleri Q => KasaSubeKilitSozlugu.Tr;

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void Q95_BASIT_ve_PRO_modda_duzenlemede_SUBE_var_temiz_kasada_PATCH_subeId_tasir(string mod)
    {
        var e = Ciz(mod);
        e.Find("#kasa-duzenle-k2").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/kasalar/k2/degisim-kilidi")));
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#d-sube")));
        Assert.Empty(e.FindAll("#degismez-ipucu-kasa-sube"));
        Assert.NotNull(e.Find("#d-doviz"));

        SeciciYardim.Sec(e, "d-sube", "s2");
        istekler.Clear();
        e.Find("#cekmece-kasa-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            var p = Assert.Single(istekler, i => i.Metot == "PATCH");
            Assert.EndsWith("/kasalar/k2", p.Yol);
            var g = JsonDocument.Parse(p.Govde).RootElement;
            Assert.Equal("s2", g.GetProperty("subeId").GetString());
            Assert.False(g.TryGetProperty("ad", out _));      // gönderilmeyen alan = korunur
        });
    }

    [Fact]
    public void Q95_HAREKETLI_kasada_sube_KILITLI_sebep_ve_GERCEK_duzeltme_yolu_yazar_PATCH_subeId_TASIMAZ()
    {
        var e = Ciz();
        e.Find("#kasa-duzenle-k1").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#degismez-ipucu-kasa-sube")));
        var ipucu = e.Find("#degismez-ipucu-kasa-sube").TextContent;
        Assert.Contains(Q.KilitHareket, ipucu);
        Assert.Contains(Q.KilitYol, ipucu);
        Assert.Equal("/gider?sekme=virman", e.Find("#d-kilit-virman-yol").GetAttribute("href"));
        Assert.True(e.Find("#d-sube").HasAttribute("disabled"));
        Assert.True(e.Find("#d-doviz").HasAttribute("disabled"));

        e.Find("#d-ad").Input("Ana Kasa 2");
        istekler.Clear();
        e.Find("#cekmece-kasa-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            var p = Assert.Single(istekler, i => i.Metot == "PATCH");
            var g = JsonDocument.Parse(p.Govde).RootElement;
            Assert.Equal("Ana Kasa 2", g.GetProperty("ad").GetString());
            Assert.False(g.TryGetProperty("subeId", out _));
            Assert.False(g.TryGetProperty("paraBirimi", out _));
        });
    }

    [Fact]
    public void Q95_kilit_OKUNAMAZSA_alanlar_KILITLI_kalir_ve_sebep_yazar_serbest_sanilmaz()
    {
        sahte.KilitHata = true;
        var e = Ciz();
        e.Find("#kasa-duzenle-k2").Click();
        e.WaitForAssertion(() => Assert.Contains(Q.KilitBilinmiyor, e.Find("#degismez-ipucu-kasa-sube").TextContent));
        Assert.True(e.Find("#d-sube").HasAttribute("disabled"));
    }

    [Fact]
    public void Q95_EKSI_kasada_rozet_uyari_duzeltme_yolu_ve_KIRMIZI_ozet_pozitif_kasada_YOK()
    {
        var e = Ciz();
        var ozet = e.Find("#kasa-eksi-ozet");
        Assert.Contains(string.Format(Q.EksiOzetUyari, 1), ozet.TextContent);
        Assert.Equal("/raporlar/kasa", e.Find("#kasa-eksi-rapor").GetAttribute("href"));
        Assert.Contains("kirmizi", e.Find(".stat-izgara .stat-kart").ClassName);
        Assert.Contains(Q.EksiOzetAlt, e.Find(".stat-izgara .stat-kart").TextContent);

        Assert.Contains(Q.EksiKartUyari, e.Find("#kasa-eksi-k1").TextContent);
        Assert.NotNull(e.Find("#kasa-eksi-k1 .rozet-kirmizi"));
        Assert.Contains("metin-kirmizi", e.Find("#kasa-bakiye-k1").ClassName);
        Assert.Empty(e.FindAll("#kasa-eksi-k2"));
        Assert.Empty(e.FindAll("#kasa-eksi-k3"));

        // Düzeltme yolu gerçek: hareket çekmecesi GİRİŞ yönünde açılır.
        e.Find("#kasa-eksi-giris-k1").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#cekmece-kasa-hareket")));
    }

    [Fact]
    public void Q95_eksi_kasa_YOKSA_ozet_uyarisi_cizilmez_ve_ozet_yesil()
    {
        sahte.Kasalar = """[{"id":"k2","ad":"Yeni Kasa","paraBirimi":"TRY","bakiye":10,"aktif":true,"subeId":"s1"}]""";
        var e = Ciz();
        Assert.Empty(e.FindAll("#kasa-eksi-ozet"));
        Assert.Contains("yesil", e.Find(".stat-izgara .stat-kart").ClassName);
    }

    [Fact]
    public void Q95_TEK_subede_kilitli_kasada_da_sube_ALANI_ve_sebep_gorunur()
    {
        sahte.Subeler = """[{"id":"s1","ad":"Merkez Şube"}]""";
        var e = Ciz();
        e.Find("#kasa-duzenle-k1").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#degismez-ipucu-kasa-sube")));
        Assert.Equal("s1", SeciciYardim.SeciliDeger(e, "d-sube"));
        Assert.True(e.Find("#d-sube").HasAttribute("disabled"));
    }
}

public sealed class Q95SayimFormTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public Q95SayimFormTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
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
                var v when v.EndsWith("/sayim") && istek.Method == HttpMethod.Get => "[]",
                var v when v.EndsWith("/sayim") => """{"id":"o9","ad":"Q95","subeId":"s2","kapsamTur":"TUMU","durum":"ACIK","toplamSatir":0,"sayilan":0,"sayilmayan":0,"farkli":0,"eksikAdet":0,"fazlaAdet":0,"netFarkHas":0,"hareketli":0,"satirlar":[]}""",
                var v when v.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez"},{"id":"s2","ad":"Çarşı"}]""",
                var v when v.Contains("/reyonlar") => "[]",
                var v when v.Contains("/urun-kategorileri/duz") => "[]",
                _ => "[]",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Sayim> YeniForm()
    {
        var e = Render<Kuyumcu.Web.Pages.Sayim>().AyrintiAc();
        e.WaitForElement("#sy-yeni");
        e.Find("#sy-yeni").Click();
        e.WaitForElement("#sy-form");
        return e;
    }

    [Fact]
    public void Q95_ad_DOLU_sube_BOS_hata_SUBENIN_altinda_ad_ISARETLENMEZ_istek_GITMEZ()
    {
        var e = YeniForm();
        e.Find("#sy-ad").Input("Q95 Vitrin");   // PAKET GOC-C: ortak Metin
        istekler.Clear();
        e.Find("#sy-ac").Click();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sy-sube-hata")));
        Assert.Contains("hatali", e.Find("#sy-sube").ClassName);
        Assert.Equal("true", e.Find("#sy-sube").GetAttribute("aria-invalid"));
        Assert.Empty(e.FindAll("#alan-hata-sy-ad"));
        Assert.DoesNotContain("hatali", e.Find("#sy-ad").ClassName ?? "");
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    [Fact]
    public void Q95_zorunlu_alanlar_ISARETLI_ve_sube_secilince_POST_gider()
    {
        var e = YeniForm();
        Assert.NotNull(e.Find("label[for=sy-ad] .zorunlu-isaret"));
        Assert.NotNull(e.Find("label[for=sy-sube] .zorunlu-isaret"));
        Assert.Equal("true", e.Find("#sy-sube").GetAttribute("aria-required"));

        e.Find("#sy-ad").Input("Q95 Vitrin");   // PAKET GOC-C: ortak Metin
        SeciciYardim.Sec(e, "sy-sube", "s2");   // PAKET GOC-C: ortak Secici (ham select kalktı)
        e.Find("#sy-ac").Click();
        e.WaitForAssertion(() =>
        {
            var p = Assert.Single(istekler, i => i.Metot == "POST");
            var g = JsonDocument.Parse(p.Govde).RootElement;
            Assert.Equal("s2", g.GetProperty("subeId").GetString());
            Assert.Equal("Q95 Vitrin", g.GetProperty("ad").GetString());
        });
    }

    /// <summary>
    /// QA turu 9'un asıl senaryosu: ad YAZILDI ama alan odaktan çıkmadan (change
    /// olayı gelmeden) "Oturumu Aç"a basıldı. Eskiden dolu ad "boş" sayılıp hata
    /// Ad'a düşüyordu; şimdi her tuş modele yazılır ve hata yalnız Şube'dedir.
    /// </summary>
    [Fact]
    public void Q95_ad_YALNIZ_yazildi_change_gelmeden_gonderildi_hata_ADA_dusmez()
    {
        var e = YeniForm();
        e.Find("#sy-ad").Input("Q95 Vitrin");
        e.Find("#sy-ac").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sy-sube-hata")));
        Assert.Empty(e.FindAll("#alan-hata-sy-ad"));
    }

    [Fact]
    public void Q95_ad_BOS_sube_DOLU_hata_yalniz_ADDA()
    {
        var e = YeniForm();
        SeciciYardim.Sec(e, "sy-sube", "s1");   // PAKET GOC-C: ortak Secici
        e.Find("#sy-ad").Input("");             // PAKET P-SAYIM (Y10): ad otomatik dolu — boş ad kullanıcı silince ölçülür
        e.Find("#sy-ac").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#alan-hata-sy-ad")));
        Assert.Empty(e.FindAll("#sy-sube-hata"));
        Assert.Equal("false", e.Find("#sy-sube").GetAttribute("aria-invalid"));   // PAKET GOC-C: ortak Secici aria-invalid hep yazılır
    }
}
