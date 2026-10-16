using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Pano;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MKul = Kuyumcu.Api.Modules.Kullanici;
using MPano = Kuyumcu.Api.Modules.Pano;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET EKSIK-KUCUK — B2-10 (vitrin tam ekran + işçilik ibaresi), B2-11 (yeni
// işletme BASİT doğar), B2-12 (müşteri değişince "fiyat listesi değişti — güncelle").
//
// Mutasyon hedefleri (rapordaki tablo):
//   M-EK-1  PanoModulu.Gorunum ibareyi döndürmezse          → EK_H01, EK_H02
//   M-EK-2  PATCH "verildi" bayrağı yok sayılırsa (boş gövde) → EK_H03
//   M-EK-3  AuthModulu kayıt BASIT yazmazsa                  → EK_H06
//   M-EK-4  VitrinEkrani ibare yokken fiyatı gösterirse        → EK_B03
//   M-EK-5  Islemler müşteri değişince fiyatı SESSİZCE değiştirirse → EK_S01
//   M-EK-6  "Fiyatları güncelle" fiyatı yazmazsa              → EK_S02
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class EksikKucukHttpTestleri(HttpFikstur f)
{
    private const string Uc = "/api/v1/pano/iscilik-ibaresi";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETEKSIK-KUCUK-TEST " + ad);
    }

    [Fact]
    public async Task EK_H01_ibare_secilmemisken_pano_iscilikIbaresi_NULL()
    {
        var k = YeniKiraci("ibare yok");
        var r = await f.Ortam.Istemci(k).GetAsync("/api/v1/pano/guncel");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Json();
        Assert.True(j.TryGetProperty("iscilikIbaresi", out var i), "iscilikIbaresi alanı yanıtta yok");
        Assert.Equal(JsonValueKind.Null, i.ValueKind);
    }

    [Fact]
    public async Task EK_H02_PATCH_DAHIL_kaydeder_panoda_doner_HARIC_ile_degisir_denetim_yazilir()
    {
        var k = YeniKiraci("ibare yaz");
        var c = f.Ortam.Istemci(k);

        var r = await c.PatchJson(Uc, new { iscilikIbaresi = "DAHIL" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("DAHIL", (await r.Json()).GetProperty("iscilikIbaresi").GetString());

        var g = await (await c.GetAsync("/api/v1/pano/guncel")).Json();
        Assert.Equal("DAHIL", g.GetProperty("iscilikIbaresi").GetString());

        // Düzenleme yolu: HARIC'e çevrilir (küçük harf de kabul, sunucu normalize eder).
        r = await c.PatchJson(Uc, new { iscilikIbaresi = "haric" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("HARIC", (await r.Json()).GetProperty("iscilikIbaresi").GetString());

        using var db = f.Vt.OlusturSistem();
        var satir = await db.KiraciAyarlari.AsNoTracking()
            .SingleAsync(a => a.TenantId == k.TenantId && a.Anahtar == MPano.PanoAyarAnahtari.IscilikIbaresi);
        Assert.Equal("HARIC", satir.Deger);
        var izler = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikId == MPano.PanoAyarAnahtari.IscilikIbaresi)
            .Select(d => d.Detay).ToListAsync();
        Assert.Equal(2, izler.Count);
        Assert.Contains(izler, d => d != null && d.Contains("HARIC"));
    }

    [Fact]
    public async Task EK_H03_PATCH_alan_yok_400_gecersiz_deger_400_kayit_DOGMAZ()
    {
        var k = YeniKiraci("ibare 400");
        var c = f.Ortam.Istemci(k);

        var bos = await c.PatchJson(Uc, new { });
        Assert.Equal(HttpStatusCode.BadRequest, bos.StatusCode);
        var gecersiz = await c.PatchJson(Uc, new { iscilikIbaresi = "BELKI" });
        Assert.Equal(HttpStatusCode.BadRequest, gecersiz.StatusCode);
        Assert.Contains("iscilikIbaresi", await gecersiz.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.KiraciAyarlari.AsNoTracking()
            .AnyAsync(a => a.TenantId == k.TenantId && a.Anahtar == MPano.PanoAyarAnahtari.IscilikIbaresi));
    }

    [Fact]
    public async Task EK_H04_PATCH_null_ibareyi_SILER_pano_yeniden_kilitlenir()
    {
        var k = YeniKiraci("ibare sil");
        var c = f.Ortam.Istemci(k);
        Assert.Equal(HttpStatusCode.OK, (await c.PatchJson(Uc, new { iscilikIbaresi = "DAHIL" })).StatusCode);

        var r = await c.PatchJson(Uc, new { iscilikIbaresi = (string?)null });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await r.Json()).GetProperty("iscilikIbaresi").ValueKind);

        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.KiraciAyarlari.AsNoTracking()
            .AnyAsync(a => a.TenantId == k.TenantId && a.Anahtar == MPano.PanoAyarAnahtari.IscilikIbaresi));
    }

    [Fact]
    public async Task EK_H05_tezgahtar_ibare_YAZAMAZ_403_ve_ibare_kalem_listesine_KARISMAZ()
    {
        var k = YeniKiraci("ibare 403");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await f.Ortam.Istemci(k, "TEZGAHTAR").PatchJson(Uc, new { iscilikIbaresi = "DAHIL" })).StatusCode);

        var sahip = f.Ortam.Istemci(k);
        var once = (await (await sahip.GetAsync("/api/v1/pano/ayarlar")).Json()).GetProperty("ayarlar").GetArrayLength();
        Assert.Equal(HttpStatusCode.OK, (await sahip.PatchJson(Uc, new { iscilikIbaresi = "DAHIL" })).StatusCode);
        var sonra = (await (await sahip.GetAsync("/api/v1/pano/ayarlar")).Json()).GetProperty("ayarlar").GetArrayLength();
        Assert.Equal(once, sonra);
    }

    [Fact]
    public async Task EK_H06_yeni_isletme_kaydi_BASIT_dogar_eski_kiraci_PROFESYONEL_KALIR()
    {
        var eski = YeniKiraci("eski kiraci");
        string jeton;
        await using (var db = f.Vt.OlusturSistem()) (jeton, _) = TestKurulum.DavetOlustur(db);
        var isletme = $"PAKETEKSIK-KUCUK-TEST kayit {Kimlik.Ulid()}";

        var r = await f.Ortam.AnonimIstemci().PostJson("/api/v1/auth/kayit", new
        {
            isletmeAdi = isletme, ad = "Yeni", soyad = "Dukkan",
            email = $"ek-{Kimlik.Ulid()}@test.local", sifre = "GucluParola123", davetJetonu = jeton,
        });
        Assert.True(r.StatusCode == HttpStatusCode.Created, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");

        await using var db2 = f.Vt.OlusturSistem();
        var yeni = await db2.Tenants.AsNoTracking().SingleAsync(t => t.Ad == isletme);
        Assert.Equal(MKul.ArayuzModu.Basit, yeni.VarsayilanArayuzModu);
        // Tercihi olmayan SAHİP kullanıcı dükkânın modunu devralır → BASİT görür.
        var sahip = await db2.Kullanicilar.AsNoTracking().SingleAsync(u => u.TenantId == yeni.Id);
        Assert.Equal(MKul.ArayuzModu.Basit, MKul.ArayuzModu.Coz(sahip.ArayuzModu, yeni.VarsayilanArayuzModu));

        // Mevcut kiracıya DOKUNULMADI.
        Assert.Equal(MKul.ArayuzModu.Profesyonel, await db2.Tenants.AsNoTracking()
            .Where(t => t.Id == eski.TenantId).Select(t => t.VarsayilanArayuzModu).SingleAsync());
    }
}

/// <summary>B2-10 saf: kabuksuz rota yalnız /vitrin/ekran; ibare metni sözlükten.</summary>
public sealed class EksikKucukSafTestleri
{
    [Theory]
    [InlineData("/vitrin/ekran", true)]
    [InlineData("/vitrin/ekran/", true)]
    [InlineData("/vitrin", false)]
    [InlineData("/ekranlar", false)]
    [InlineData("/vitrin/ekranx", false)]
    public void EK_P01_vitrin_ekrani_KABUKSUZ_rota(string yol, bool beklenen)
        => Assert.Equal(beklenen, AnaYerlesim.VitrinEkranRotasi(yol));

    [Fact]
    public void EK_P02_ibare_metni_sozlukten_ham_kod_basilmaz_TR_EN_dolu()
    {
        Assert.Equal(EksikKucukSozlugu.Tr.IbareDahil, Kuyumcu.Web.Pages.VitrinEkrani.IbareMetni("DAHIL", EksikKucukSozlugu.Tr));
        Assert.Equal(EksikKucukSozlugu.Tr.IbareHaric, Kuyumcu.Web.Pages.VitrinEkrani.IbareMetni("HARIC", EksikKucukSozlugu.Tr));
        Assert.Null(Kuyumcu.Web.Pages.VitrinEkrani.IbareMetni(null, EksikKucukSozlugu.Tr));
        Assert.Null(Kuyumcu.Web.Pages.VitrinEkrani.IbareMetni("BELKI", EksikKucukSozlugu.Tr));
        Assert.Contains("işçilik", EksikKucukSozlugu.Tr.IbareDahil, StringComparison.Ordinal);
        Assert.Contains("dahil değildir", EksikKucukSozlugu.Tr.IbareHaric, StringComparison.Ordinal);
        foreach (var p in typeof(EksikKucukMetinleri).GetProperties())
        {
            Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(EksikKucukSozlugu.Tr)), $"Tr.{p.Name} boş");
            Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(EksikKucukSozlugu.En)), $"En.{p.Name} boş");
        }
    }

    [Fact]
    public void EK_P03_rota_basligi_modul_kapisi_ve_menusuz_gerekce_kayitli()
    {
        Assert.Equal("İşletme ekranı", RotaBasliklari.Baslik("/vitrin/ekran", Sozlukler.Tr));
        Assert.Equal("kur", Kuyumcu.Core.Erisim.RotaModulleri.Harita["/vitrin/ekran"]);
        Assert.Equal("Vitrin.razor", MenuTanimi.MenusuzRotalar["/vitrin/ekran"]);
    }
}

/// <summary>B2-10 bUnit: Vitrin sayfası (ibare seçimi + dükkân ekranı düğmesi) ve dükkân ekranı.</summary>
public sealed class EksikKucukVitrinTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly Sahte sahte = new();

    public EksikKucukVitrinTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        sahte.Kayit = istekler;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(sahte) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Sahte : HttpMessageHandler
    {
        public List<(string, string, string)> Kayit = [];
        public string? Ibare;
        public bool Tezgahtar;

        private string Pano => $$"""
        {"kaynak":"ALTINKAYNAK","zaman":"2026-10-10T08:00:00Z",
         "satirlar":[
           {"kod":"GRAM","etiket":"Gram Altın","alis":6800.00,"satis":6900.00,"birim":"GRAM"},
           {"kod":"CEYREK_YENI","etiket":"Yeni Çeyrek","alis":11500.00,"satis":11800.00,"birim":"ADET"}],
         "atlananlar":[],"tavanSatir":16,"okunabilirlikEsigi":12,"okunabilirlikUyarisi":false,
         "iscilikIbaresi":{{(Ibare is null ? "null" : $"\"{Ibare}\"")}}}
        """;

        private const string Ayarlar = """
        {"ayarlar":[{"kod":"GRAM","ad":"Gram Altın","etiket":null,"sira":0,"alisMarjTl":0,"satisMarjTl":0,
            "yuvarlamaAdimi":10,"aktif":true,"ozel":false}],
         "yuvarlamaAdimlari":[0,1,5,10,50,100],"tavanSatir":16,"okunabilirlikEsigi":12,"aktifSayisi":1}
        """;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            Kayit.Add((istek.Method.Method, yol, govde));

            if (yol.Contains("/pano/ayarlar", StringComparison.Ordinal) && Tezgahtar)
                return new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent("""{"message":"Yetkiniz yok"}""", System.Text.Encoding.UTF8, "application/json"),
                };
            if (yol.Contains("/pano/iscilik-ibaresi", StringComparison.Ordinal))
                Ibare = JsonDocument.Parse(govde).RootElement.GetProperty("iscilikIbaresi").GetString();

            var y = yol.Contains("/pano/ayarlar", StringComparison.Ordinal) ? Ayarlar : Pano;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private static EksikKucukMetinleri E => EksikKucukSozlugu.Tr;

    [Fact]
    public void EK_B01_ibare_yokken_dukkan_ekrani_KILITLI_sebep_yazili_secince_ACILIR_ve_PATCH_govdesi_dogru()
    {
        var e = Render<Kuyumcu.Web.Pages.Vitrin>();
        e.WaitForElement("#vitrin-ibare-yok");
        Assert.Contains(E.IbareSecilmedi, e.Find("#vitrin-ibare-yok").TextContent, StringComparison.Ordinal);
        var kilitli = e.Find("#vitrin-tam-ekran");
        Assert.Equal("button", kilitli.TagName.ToLowerInvariant());
        Assert.True(kilitli.HasAttribute("disabled"));
        Assert.Equal(E.TamEkranKilitli, kilitli.GetAttribute("title"));

        e.Find("#vitrin-ibare-dahil").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH" && i.Yol.Contains("/pano/iscilik-ibaresi")));
        var g = JsonDocument.Parse(istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
        Assert.Equal("DAHIL", g.GetProperty("iscilikIbaresi").GetString());
        // UX-S23: etkisi aynı ekranda — ibare yazılır, düğme bağlantıya döner.
        e.WaitForAssertion(() => Assert.Equal(E.IbareDahil, e.Find("#vitrin-ibare").TextContent.Trim()));
        var ac = e.Find("#vitrin-tam-ekran");
        Assert.Equal("a", ac.TagName.ToLowerInvariant());
        Assert.Equal("/vitrin/ekran", ac.GetAttribute("href"));
        Assert.Equal("true", e.Find("#vitrin-ibare-dahil").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void EK_B02_tezgahtar_secim_dugmesi_GORMEZ_kimin_secegini_okur()
    {
        sahte.Tezgahtar = true;
        var e = Render<Kuyumcu.Web.Pages.Vitrin>();
        e.WaitForElement("#vitrin-ibare-yok");
        Assert.Contains(E.IbareSecilmediTezgahtar, e.Find("#vitrin-ibare-yok").TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#vitrin-ibare-dahil"));
        Assert.Empty(e.FindAll("#vitrin-ibare-haric"));
    }

    [Fact]
    public void EK_B03_dukkan_ekrani_ibare_YOKKEN_fiyat_GOSTERMEZ_fail_closed()
    {
        var e = Render<Kuyumcu.Web.Pages.VitrinEkrani>();
        e.WaitForElement("#vtam-kilitli");
        Assert.Empty(e.FindAll("#vtam-tablo"));
        Assert.DoesNotContain("6.900", e.Markup, StringComparison.Ordinal);
        Assert.Equal("/vitrin", e.Find("#vtam-cik").GetAttribute("href"));
    }

    [Fact]
    public void EK_B04_dukkan_ekrani_fiyatlar_ve_ibare_BIRLIKTE_kagitta_da_gorunur()
    {
        sahte.Ibare = "HARIC";
        var e = Render<Kuyumcu.Web.Pages.VitrinEkrani>();
        e.WaitForElement("#vtam-tablo");
        Assert.NotNull(e.Find("#vtam-satir-GRAM"));
        Assert.Contains("6.900", e.Find("#vtam-satir-GRAM").TextContent, StringComparison.Ordinal);
        var ibare = e.Find("#vtam-ibare");
        Assert.Equal(E.IbareHaric, ibare.TextContent.Trim());
        // Kâğıt = ekran: ibare yazdırmada gizlenmez.
        Assert.DoesNotContain("yazdirma-gizle", ibare.ClassName ?? "", StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#vtam-kilitli"));
        // Her düğme ya istek/olay taşır ya disabled.
        foreach (var d in e.FindAll("button"))
            Assert.True(d.HasAttribute("disabled") || d.HasAttribute("blazor:onclick"), $"#{d.Id}");
    }
}

/// <summary>B2-12 bUnit: müşteri değişince fiyat listesi farkı SORULUR (sessiz değil).</summary>
public sealed class EksikKucukSatisTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool teklifHatali;

    private const string CariListesi = """
        [{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":null,"email":null,
          "tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false,"sistemCarisi":false,"aktif":true},
         {"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","telefon":null,"email":null,
          "tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false,"sistemCarisi":true,"aktif":true}]
        """;

    public EksikKucukSatisTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => teklifHatali))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private static string Teklif(decimal fiyat) =>
        $$"""{"teklifVar":true,"listeFiyati":{{fiyat}},"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":{{fiyat}},"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""";

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<bool> teklifHatali) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            // Ayşe Demir'in (c1) listesi farklı: 11.000; varsayılan/peşin 10.000.
            if (yol.Contains("/fiyat/teklif") && yol.Contains("cariId=c1") && teklifHatali())
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"message":"sunucu hatası"}""", System.Text.Encoding.UTF8, "application/json"),
                };

            var y = yol switch
            {
                var v when v.Contains("/cari/sistem-carisi") =>
                    """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.EndsWith("/cari") || v.Contains("/cari?") => CariListesi,
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":300000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":300000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") && v.Contains("cariId=c1") => Teklif(11000m),
                var v when v.Contains("/fiyat/teklif") => Teklif(10000m),
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/islemler/satis") => """{"id":"h1","fisId":"f1"}""",
                var v when v.Contains("/reyonlar") || v.Contains("/islemler/son") || v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private static EksikKucukMetinleri E => EksikKucukSozlugu.Tr;

    /// <summary>PRO satış: Peşin Müşteri ön seçili, 10.000'lik kalem sepette.</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> SepetliPos()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1",
                                null, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-tur-SATIS");
        e.WaitForElement("#islem-pesin-notu");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/fiyat/teklif")));
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        return e;
    }

    private JsonElement SatisGovdesi(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")));
        return JsonDocument.Parse(istekler.Last(i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")).Govde).RootElement;
    }

    /// <summary>Tek kalemli satış /islemler/satis (düz gövde), çok kalemli /satis-fisi (kalemler[]) gider.</summary>
    private static decimal IlkKalemFiyati(JsonElement g) =>
        (g.TryGetProperty("kalemler", out var k) ? k[0] : g).GetProperty("birimFiyatTl").GetDecimal();

    [Fact]
    public void EK_S01_musteri_degisince_SORAR_basilmazsa_ESKI_fiyat_kalir_ve_SQLe_o_gider()
    {
        var e = SepetliPos();
        SeciciYardim.Sec(e, "islem-cari", "c1");

        var bant = e.WaitForElement("#islem-liste-degisti");
        Assert.Contains(string.Format(E.ListeDegisti, 1), bant.TextContent, StringComparison.Ordinal);
        // Yeni müşterinin teklifi AYNI kur kaydıyla istendi (fark yalnız listeden doğar).
        Assert.Contains(istekler, i => i.Yol.Contains("/fiyat/teklif") && i.Yol.Contains("cariId=c1") && i.Yol.Contains("kurSnapshotId=kur-1"));

        // Düğmeye BASILMADAN kaydedilirse eski fiyat gider (sessiz değişim yok).
        Assert.Equal(10000m, IlkKalemFiyati(SatisGovdesi(e)));
    }

    [Fact]
    public void EK_S02_Fiyatlari_guncelle_basilinca_sepet_YENI_listeden_govde_ekranla_AYNI()
    {
        var e = SepetliPos();
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.WaitForElement("#islem-liste-guncelle").Click();

        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-liste-degisti")));
        Assert.Contains(string.Format(E.ListeGuncellendi, 1), e.Find("#islem-liste-guncellendi").TextContent, StringComparison.Ordinal);
        Assert.Contains("11.000", e.Markup, StringComparison.Ordinal);   // ekrandaki sepet yeni fiyatı gösterir

        var g = SatisGovdesi(e);
        Assert.Equal(11000m, IlkKalemFiyati(g));
        Assert.Equal("c1", g.GetProperty("cariId").GetString());
    }

    [Fact]
    public void EK_S03_Eski_fiyatlar_kalsin_bandi_kapatir_fiyat_DEGISMEZ()
    {
        var e = SepetliPos();
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.WaitForElement("#islem-liste-eski").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-liste-degisti")));
        Assert.Equal(10000m, IlkKalemFiyati(SatisGovdesi(e)));
    }

    [Fact]
    public void EK_S04_teklif_alinamazsa_sepet_DEGISMEZ_hata_bandi_YUTULMAZ()
    {
        teklifHatali = true;
        var e = SepetliPos();
        SeciciYardim.Sec(e, "islem-cari", "c1");
        var hata = e.WaitForElement("#islem-liste-hata");
        Assert.Contains(E.ListeHata, hata.TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#islem-liste-degisti"));
        Assert.Equal(10000m, IlkKalemFiyati(SatisGovdesi(e)));
    }

    [Fact]
    public void EK_S06_ELLE_fiyatli_kalem_SORULMAZ_ve_fiyati_KORUNUR()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1",
                                null, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-pesin-notu");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/fiyat/teklif")));
        e.Find("#islem-birimfiyat").Input("9500");   // tezgâhtar fiyatı ELLE yazdı
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();

        var once = istekler.Count(i => i.Yol.Contains("cariId=c1"));
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/tevsik")));
        Assert.Empty(e.FindAll("#islem-liste-degisti"));
        Assert.Equal(once, istekler.Count(i => i.Yol.Contains("/fiyat/teklif") && i.Yol.Contains("cariId=c1")));   // teklif bile istenmez
        Assert.Equal(9500m, IlkKalemFiyati(SatisGovdesi(e)));
    }

    [Fact]
    public void EK_S05_ayni_fiyatli_musteriye_gecince_SORMAZ()
    {
        var e = SepetliPos();
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.WaitForElement("#islem-liste-degisti");
        e.Find("#islem-liste-guncelle").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-liste-degisti")));
        // Peşin'e geri dön: liste farkı var (11.000 → 10.000) → yine SORAR.
        SeciciYardim.Sec(e, "islem-cari", "pm1");
        e.WaitForElement("#islem-liste-degisti");
        e.Find("#islem-liste-eski").Click();
        // Aynı müşteriye (pm1) yeniden seçimde sepetteki fiyat 11.000, teklif 10.000 → sorar; c1'e dönüşte fark yok.
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-liste-degisti")));
    }
}
