using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-55 — GİDER PUSULASI: satırda Düzenle + belge ekranından dönüş +
// kilitli alanların gerekçesi (QA turu 8 KRİTİK · UX-1 "gerekçeli istisna").
//
// Sunucu kilidi GERÇEK ve eskidir (HurdaServisi.Guncelle: PATCH yalnız satıcı
// adresi + mal açıklaması; iptalli belge 409). Bu paket o kilidi EKRANDA
// söyletir. Mutasyon kanıtları:
//   M55-1  DegismezIpucu çağrıları kaldırılır → KilitliAlanlardaGerekceGorunur KIRMIZI
//   M55-2  İptal durum kontrolü kaldırılır   → IptalliPusuladaKaydetKilitli KIRMIZI
//
// ⚠ KARARSIZLIK KORUMASI: zamanlayıcı beklenmez; sahte API her uca GERÇEKÇİ
//   yanıt döndürür (`{}` varsayılanı JsonException atar).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class PusulaEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string pusulaDetay = PusulaDolu;

    private static readonly GiderPusulasiDuzenMetinleri P = GiderPusulasiSozlugu.Tr;

    private const string PusulaListesi = """
        [{"id":"gp1","pusulaNo":"GP-2026-000001","hareketId":null,"saticiTipi":"NIHAI_TUKETICI",
          "saticiAdSoyad":"Ali Veli","saticiTckn":"111******10","ayarId":"22K","gramAgirlik":10.0,
          "hasGrami":9.16,"brutTutar":10000.00,"stopajOran":0.0,"stopajTutar":0.00,
          "netOdenen":10000.00,"teslimTarihi":"2099-09-01T00:00:00Z",
          "duzenlemeSonTarihi":"2099-09-08T00:00:00Z","gecikti":false,"kalanGun":30,
          "iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null}]
        """;

    private const string SureTakibi = """
        {"sure":{"gun":7,"dayanak":"VUK md.234"},"gecikmisSayisi":0,"yaklasanSayisi":0,
         "gecikmis":[],"yaklasan":[],"uyarilar":[]}
        """;

    private static string Detay(bool iptal, string adres = "Merkez mah. 1",
        string mal = "22 ayar hurda bilezik") => $$"""
        {"id":"gp1","pusulaNo":"GP-2026-000001","saticiTipi":"NIHAI_TUKETICI","saticiAdSoyad":"Ali Veli",
         "saticiTckn":"11111111110","saticiAdres":{{JsonSerializer.Serialize(adres)}},"ayarId":"22K","gramAgirlik":10.0,
         "hasGrami":9.16,"malAciklamasi":{{JsonSerializer.Serialize(mal)}},"brutTutar":10000.00,
         "stopajOran":0.0,"stopajTutar":0.00,"netOdenen":10000.00,
         "teslimTarihi":"2099-09-01T00:00:00Z","duzenlemeSonTarihi":"2099-09-08T00:00:00Z",
         "subeId":null,"cariId":null,"hareketId":null,"duzeltilenId":null,
         "olusturuldu":"2099-09-01T00:00:00Z",
         "iptalEdildi":{{(iptal ? "true" : "false")}},
         "iptalGerekce":{{(iptal ? "\"Satıcı TCKN yanlış girilmişti, yeniden düzenlenecek\"" : "null")}},
         "iptalTarihi":{{(iptal ? "\"2099-09-02T00:00:00Z\"" : "null")}}}
        """;

    private static readonly string PusulaDolu = Detay(iptal: false);
    private static readonly string PusulaIptalli = Detay(iptal: true);

    private const string Belge = """
        {"belgeTuru":"GİDER PUSULASI","dayanak":"213 sayılı VUK md.234","pusulaNo":"GP-2026-000001",
         "duzenlemeTarihi":"2099-09-01T10:00:00Z","teslimTarihi":"2099-09-01T10:00:00Z",
         "duzenlemeSonTarihi":"2099-09-08T10:00:00Z","gecikti":false,
         "duzenleyen":{"unvan":"Deneme Kuyumculuk","sube":"Merkez","adres":"Ulus","vergiDairesi":"Ulus",
           "vergiNo":"1234567890","telefon":null},
         "satici":{"saticiTipi":"NIHAI_TUKETICI","saticiTipiAciklama":"Vergi mükellefi olmayan",
           "adSoyad":"Ali Veli","tckn":"11111111110","adres":"Merkez mah. 1"},
         "mal":{"cins":"Hurda altın (22K)","gramAgirlik":10.0,"hasGrami":9.16,"aciklama":"22 ayar hurda bilezik"},
         "tutarlar":{"brutTutar":10000,"stopajOran":0,"stopajOranYuzde":"%0","stopajTutar":0,
           "netOdenen":10000,"yaziyla":"ONBİN TL"},
         "not":"Deneme"}
        """;

    public PusulaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));
    }

    private sealed class SahteApi(PusulaEkranTestleri sahip) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            sahip.istekler.Add((istek.Method.Method, yol, govde));

            string g;
            if (istek.Method == HttpMethod.Patch && yol.Contains("/gider-pusulalari/"))
            {
                // Sunucu gibi davran: gövdedeki iki alanı uygular, kaydı döndürür.
                using var j = JsonDocument.Parse(govde);
                var adres = j.RootElement.GetProperty("saticiAdres").GetString()!;
                var mal = j.RootElement.GetProperty("malAciklamasi").GetString()!;
                sahip.pusulaDetay = Detay(iptal: false, adres, mal);
                g = sahip.pusulaDetay;
            }
            else
            {
                g = yol switch
                {
                    var y when y.EndsWith("/belge") => Belge,
                    var y when y.EndsWith("/gider-pusulalari/sure-takibi") => SureTakibi,
                    var y when y.Contains("/gider-pusulalari/") => sahip.pusulaDetay,
                    var y when y.EndsWith("/gider-pusulalari") => PusulaListesi,
                    var y when y.EndsWith("/yetkiler/benim") => """{"moduller":["cari","urun"]}""",
                    var y when y.Contains("/kur/guncel") =>
                        """{"id":"kur-1","altinHasFiyati":6827.20,"gramAltinFiyati":6861.51}""",
                    _ => "[]",
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.GiderPusulalari> DuzenleAc()
    {
        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>();
        c.WaitForState(() => c.FindAll("table tbody tr").Count > 0);
        c.Find("#gp-duzenle-gp1").Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#cekmece-gider-pusulasi")));
        return c;
    }

    // ── 1) Satırda Düzenle ───────────────────────────────────────────────────

    [Fact]
    public void SatirdaDuzenleEylemiVar()
    {
        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>();
        c.WaitForState(() => c.FindAll("table tbody tr").Count > 0);

        var dugme = c.Find("#gp-duzenle-gp1");
        Assert.Equal("Düzenle", dugme.TextContent.Trim());
        Assert.False(dugme.HasAttribute("disabled"));

        dugme.Click();

        // Gerçek istek: tekil pusula GET'i.
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/gider-pusulalari/gp1"));
        // Çekmece adı "Detay / Düzenle".
        Assert.Contains("Detay / Düzenle", c.Find("#cekmece-gider-pusulasi-baslik").TextContent,
            StringComparison.Ordinal);
        // DÜZENLEME KİPİ: düzeltilebilir alanlar kilitli alanlardan ÖNCE çizilir.
        var govde = c.Find("#cekmece-gider-pusulasi-govde").InnerHtml;
        var duzenlenebilir = govde.IndexOf("gp-duzenlenebilir", StringComparison.Ordinal);
        var ilkKilit = govde.IndexOf("degismez-ipucu-GiderPusulasi-Satici", StringComparison.Ordinal);
        Assert.True(duzenlenebilir >= 0 && ilkKilit > duzenlenebilir,
            "Düzenle ile açılan çekmecede düzeltilebilir alanlar en üstte olmalı.");
        Assert.NotNull(c.Find("#gp-duz-adres"));
        Assert.NotNull(c.Find("#gp-duz-mal"));
    }

    // ── 2) Kilitli dört alanda gerekçe + düzeltme yolu ────────────────────────

    [Fact]
    public void KilitliAlanlardaGerekceGorunur()
    {
        var c = DuzenleAc();

        (string Alan, string Gerekce)[] kilitliler =
        [
            ("Satici", P.KilitliSatici),
            ("Tckn", P.KilitliTckn),
            ("Tutar", P.KilitliTutar),
            ("Tarih", P.KilitliTarih),
        ];
        // BELGE-KALAN tur 2: yol, ekrandaki düğmenin GÖRÜNEN adını anar (madde no PRO title ipucunda).
        var yol = string.Format(P.DuzeltmeYolu, KesifP3Sozlugu.Tr.PusulaIptalEtSade);
        foreach (var (alan, gerekce) in kilitliler)
        {
            var ipucu = c.Find($"#degismez-ipucu-GiderPusulasi-{alan}").TextContent;
            Assert.Contains(gerekce, ipucu, StringComparison.Ordinal);
            Assert.Contains(yol, ipucu, StringComparison.Ordinal);
            Assert.Contains("iptal edip yeni pusula düzenleyin", ipucu, StringComparison.Ordinal);
        }
        // Anlatılan yol ekranda GERÇEKTEN var: iptal düğmesi çekmecede.
        Assert.NotNull(c.Find("#gp-iptal-gp1"));
    }

    // ── 3) Belge ekranından pusulaya dönüş (doğru kimlik) ─────────────────────

    [Fact]
    public void BelgeEkranindaPusulayaDonusVar()
    {
        var b = Render<Kuyumcu.Web.Pages.PusulaBelgesi>(p => p.Add(x => x.Id, "gp1"));
        b.WaitForAssertion(() => Assert.NotNull(b.Find(".belge")));

        Assert.Equal("/gider-pusulalari?duzenle=gp1", b.Find("#gpb-duzenle").GetAttribute("href"));
        Assert.Contains(Sozlukler.Tr.Pusula.IptalEt, b.Find("#gpb-iptal-yeri").TextContent,
            StringComparison.Ordinal);

        var nav = Services.GetRequiredService<NavigationManager>();
        b.Find("#gpb-geri").Click();
        Assert.EndsWith("/gider-pusulalari?ac=gp1", nav.Uri, StringComparison.Ordinal);

        // Dönüş adresi LİSTEDE o pusulayı açar; ?duzenle ise düzenleme kipinde.
        nav.NavigateTo("/gider-pusulalari?duzenle=gp1");
        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#gp-duz-adres")));
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/gider-pusulalari/gp1"));
    }

    // ── 4) Adres + mal açıklaması düzenlenip kaydedilir ──────────────────────

    [Fact]
    public void AdresVeAciklamaDuzenlenipKaydedilir_Ekran()
    {
        var c = DuzenleAc();
        var listeGetOnce = istekler.Count(i => i.Metot == "GET" && i.Yol.EndsWith("/gider-pusulalari"));

        c.Find("#gp-duz-adres").Input("Çankaya Mah. 7");
        c.Find("#gp-duz-mal").Input("Hurda 22 ayar bilezik");
        c.Find("#cekmece-gider-pusulasi-kaydet").Click();

        var patch = Assert.Single(istekler, i => i.Metot == "PATCH");
        Assert.EndsWith("/gider-pusulalari/gp1", patch.Yol);
        using var j = JsonDocument.Parse(patch.Govde);
        Assert.Equal("Çankaya Mah. 7", j.RootElement.GetProperty("saticiAdres").GetString());
        Assert.Equal("Hurda 22 ayar bilezik", j.RootElement.GetProperty("malAciklamasi").GetString());

        // Sonuç görünür (UX-S27) ve liste yeniden okundu.
        c.WaitForAssertion(() =>
            Assert.Contains("GP-2026-000001", c.Find("#gp-basari").TextContent, StringComparison.Ordinal));
        Assert.True(istekler.Count(i => i.Metot == "GET" && i.Yol.EndsWith("/gider-pusulalari")) > listeGetOnce);
        Assert.Equal("Çankaya Mah. 7", c.Find("#gp-duz-adres").GetAttribute("value"));
    }

    // ── 5) İptalli pusulada Kaydet kilitli + neden yazılı ────────────────────

    [Fact]
    public void IptalliPusuladaKaydetKilitli()
    {
        pusulaDetay = PusulaIptalli;
        var c = DuzenleAc();

        var kaydet = c.Find("#cekmece-gider-pusulasi-kaydet");
        Assert.True(kaydet.HasAttribute("disabled"), "İptalli pusulada Kaydet açık kalmamalı.");
        Assert.Contains(Sozlukler.Tr.Pusula.IptalliDuzenlenemez,
            c.Find("#cekmece-gider-pusulasi-kilit").TextContent, StringComparison.Ordinal);

        kaydet.Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
    }
}

// ── HTTP kapısı: adres + mal açıklaması GERÇEKTEN kaydedilir (200) ve yansır ──

[Collection("http")]
public sealed class PusulaDuzenleHttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task AdresVeAciklamaDuzenlenipKaydedilir()
    {
        Kiraci k;
        using (var db0 = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db0, "P-55 pusula duzenle");
        var id = Kuyumcu.Api.Common.Kimlik.Ulid();
        using (var db = f.Vt.OlusturSistem())
        {
            db.GiderPusulalari.Add(new GiderPusulasi
            {
                Id = id, TenantId = k.TenantId, PusulaNo = "GP-2026-000055",
                HareketId = Kuyumcu.Api.Common.Kimlik.Ulid(), SubeId = k.SubeId,
                SaticiTipi = SaticiTipi.NihaiTuketici, SaticiAdSoyad = "Test Satıcı",
                SaticiTckn = "11111111110", SaticiTcknHash = new string('a', 64), SifreSurumu = 0,
                SaticiAdres = "Ulus Mah. 1", MalAciklamasi = "Hurda bilezik",
                AyarId = "22K", GramAgirlik = 10m, HasGrami = 9.17m,
                BrutTutar = 10_000m, StopajOran = 0m, StopajTutar = 0m, NetOdenen = 10_000m,
                TeslimTarihi = DateTime.UtcNow, DuzenlemeSonTarihi = DateTime.UtcNow.AddDays(7),
                Olusturuldu = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var cevap = await istemci.PatchJson($"/api/v1/gider-pusulalari/{id}",
            new { saticiAdres = "Çankaya Mah. 7", malAciklamasi = "Hurda 22 ayar bilezik" });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        // Yansıma: tekil okuma yeni değerleri, liste aynı (iptalsiz) kaydı döndürür.
        var tekil = await (await istemci.GetAsync($"/api/v1/gider-pusulalari/{id}")).Json();
        Assert.Equal("Çankaya Mah. 7", tekil.GetProperty("saticiAdres").GetString());
        Assert.Equal("Hurda 22 ayar bilezik", tekil.GetProperty("malAciklamasi").GetString());
        var liste = await (await istemci.GetAsync("/api/v1/gider-pusulalari")).Json();
        var satir = Assert.Single(liste.EnumerateArray());
        Assert.Equal(id, satir.GetProperty("id").GetString());
        Assert.False(satir.GetProperty("iptalEdildi").GetBoolean());

        using var kontrol = f.Vt.OlusturSistem();
        var g = await kontrol.GiderPusulalari.AsNoTracking().FirstAsync(x => x.Id == id);
        Assert.Equal("Çankaya Mah. 7", g.SaticiAdres);
        Assert.Equal("Hurda 22 ayar bilezik", g.MalAciklamasi);
        Assert.Equal(10_000m, g.BrutTutar);   // mali hüviyet DEĞİŞMEDİ
    }
}
