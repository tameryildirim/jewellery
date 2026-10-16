using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET EKSIK-B1-YAZDIR — EKRAN (bUnit). Sahte API "{}" varsayılanı YOK: her uca
// gerçekçi camelCase yanıt; bilinmeyen uç 404 döner (sessiz yeşil olmasın).
// Gövdeler JSON olarak AYRIŞTIRILARAK sınanır. İki mod (BASİT + PRO) ayrı ayrı.
// ═════════════════════════════════════════════════════════════════════════════

internal static class EksikB1Ortak
{
    public static readonly string[] Modlar = [ArayuzModuServisi.Basit, ArayuzModuServisi.Profesyonel];

    public static HttpResponseMessage Yanit(HttpStatusCode kod, string g) => new(kod)
    {
        Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
    };

    public static void Kur(BunitContext c, HttpMessageHandler api)
    {
        c.JSInterop.Mode = JSRuntimeMode.Loose;
        c.Services.AddScoped<DilServisi>();
        c.Services.AddScoped<OturumServisi>();
        c.Services.AddScoped<ArayuzModuServisi>();
        c.Services.AddScoped<TemaServisi>();
        c.Services.AddScoped<CanliKurServisi>();
        c.Services.AddScoped(_ => new HttpClient(api) { BaseAddress = new Uri("http://test.local/api/v1/") });
        c.Services.AddScoped<ApiIstemci>();
    }

    public static void Oturum(BunitContext c, string mod, string rol = "SAHIP")
    {
        c.Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "sb1"));
        c.Services.GetRequiredService<ArayuzModuServisi>().Uygula(mod, mod);
    }
}

// ── B1-3: Cariler → cari ekstresi yazdır ─────────────────────────────────────
public sealed class EksikB1CarilerEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol)> istekler = [];

    private sealed class SahteApi(List<(string, string)> kayit) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            lock (kayit) kayit.Add((istek.Method.Method, yol));
            string? g = yol switch
            {
                var y when y.EndsWith("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"gbpTry":null}""",
                var y when y.EndsWith("/cari/c1/ekstre") => """
                    {"bakiye":{"tlBakiye":-1500,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0},
                     "hareketler":[{"id":"h1","tur":"SATIS","tlTutari":1500,"hasGrami":-2,"aciklama":null,"olusturuldu":"2026-09-30T09:00:00Z","cariHasEtkisi":0,"bakiyeHasEtkisi":0,"emanetHas":0,"hasBakiyesineGirer":false,"emanet":false,"maden":"ALTIN"}],
                     "sayfaDisiHasEtkisi":0,"dahaFazlaVar":false,"limit":100,"bakiyeEmanetsizHas":0,"emanetteHas":0}
                    """,
                var y when y.EndsWith("/cari/c1") => """
                    {"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567","email":null,"vergiNo":null,"adres":null,
                     "krediLimitiTl":null,"varsayilanVadeGun":30,"fiyatListesiId":null,"pepBeyani":false,"pepGorev":null,"pepBeyanTarihi":null,"uyruk":"ZZ"}
                    """,
                var y when y.EndsWith("/cari") => """
                    [{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567","email":null,"tlBakiye":-1500.00,
                      "hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"riskSeviye":null,"pepBeyani":false}]
                    """,
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var y when y.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","aktif":true}]""",
                var y when y.Contains("/subeler") => """[{"id":"sb1","ad":"Merkez","aktif":true}]""",
                _ => "[]",
            };
            return Task.FromResult(EksikB1Ortak.Yanit(HttpStatusCode.OK, g));
        }
    }

    public EksikB1CarilerEkranTestleri() => EksikB1Ortak.Kur(this, new SahteApi(istekler));

    [Fact]
    public void B13_cari_detayinda_ekstre_yazdir_IKI_MODDA_A4_belgesine_gider()
    {
        foreach (var mod in EksikB1Ortak.Modlar)
        {
            EksikB1Ortak.Oturum(this, mod);
            var e = Render<Kuyumcu.Web.Pages.Cariler>();
            e.WaitForElement("#c-detay-c1").Click();
            var dugme = e.WaitForElement("#cari-detay-ekstre-yazdir");
            Assert.Equal("/cikti/CARI_EKSTRE/c1", dugme.GetAttribute("href"));
            Assert.Equal(EksikB1Sozlugu.Tr.EkstreYazdir, dugme.TextContent.Trim());
            // Kâğıda basılmaz (yazdırma-gizle kabının içinde).
            Assert.Contains("yazdirma-gizle", dugme.ParentElement!.ClassName);
            e.Dispose();
        }
    }
}

// ── B1-4: Emanet → teslim belgesi yazdır ─────────────────────────────────────
public sealed class EksikB1EmanetEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol)> istekler = [];

    private sealed class SahteApi(List<(string, string)> kayit) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            lock (kayit) kayit.Add((istek.Method.Method, yol));
            string? g = yol switch
            {
                var y when y.Contains("/kur/guncel") => """{"id":"kur-9","altinHasFiyati":6800}""",
                var y when y.EndsWith("/emanet/c1") => """
                    {"cariId":"c1","mevcutHasGramBakiye":12.5,
                     "madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":12.5}],
                     "hareketler":[{"id":"h1","tur":"EMANET_GIRIS","tarih":"2026-09-10T10:00:00Z","hasGrami":12.5,
                        "aciklama":"Bilezik 2 adet","stornoKaynakId":null,"fisId":null,"maden":"ALTIN"}]}
                    """,
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Ayşe Demir"},{"id":"c2","ad":"Mehmet Kaya"}]""",
                _ => null,
            };
            return Task.FromResult(g is null
                ? EksikB1Ortak.Yanit(HttpStatusCode.NotFound, """{"message":"sahte API bu ucu tanımıyor"}""")
                : EksikB1Ortak.Yanit(HttpStatusCode.OK, g));
        }
    }

    public EksikB1EmanetEkranTestleri() => EksikB1Ortak.Kur(this, new SahteApi(istekler));

    [Fact]
    public void B14_emanet_satirinda_ve_ayrintida_belge_yazdir_IKI_MODDA()
    {
        foreach (var mod in EksikB1Ortak.Modlar)
        {
            EksikB1Ortak.Oturum(this, mod);
            var e = Render<Kuyumcu.Web.Pages.Emanet>();
            e.WaitForElement("#e-cari");
            SeciciYardim.Sec(e, "e-cari", "c1");
            e.WaitForElement("#e-gecmis-ac").Click();

            var satir = e.WaitForElement("#eh-belge-h1");
            Assert.Equal("/cikti/EMANET_TESLIM/h1?donus=%2Femanet", satir.GetAttribute("href"));
            Assert.Equal(EksikB1Sozlugu.Tr.BelgeYazdir, satir.TextContent.Trim());

            e.Find("#eh-detay-h1").Click();
            var detay = e.WaitForElement("#eh-detay-belge-h1");
            Assert.Equal(satir.GetAttribute("href"), detay.GetAttribute("href"));
            e.Dispose();
        }
    }
}

// ── B1-5 · B1-6: Sipariş detayı → şerit · usta · kapora ──────────────────────
// Tur 2: EN testi statik Bicim.Dil'i değiştirir → paralelsiz koleksiyon (DilEkran2 ile aynı).
[Collection("DilEkranBicimDil")]
public sealed class EksikB1SiparisDetayEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string tur = "MUSTERI_SIPARISI";
    private string durum = "HAZIR";
    private string? kaporaHareketId;
    private bool kaporaStornolu;
    private string? ustaCariId;
    private int kasaSayisi = 1;
    private bool kurVar = true;

    private const string CariA = "01M396C642K89MJRHS9TRN6Z21";
    private const string UstaB = "01M396C660S85310P2GNFZEZX3";
    private static readonly EksikB1Metinleri M = EksikB1Sozlugu.Tr;

    private static string j<T>(T v) => JsonSerializer.Serialize(v);

    private string DetayJson(string id = "s1", string? dil = null, string? durumZorla = null)
    {
        var durum = durumZorla ?? this.durum;
        var iptal = durum == "IPTAL";
        var teslim = durum == "TESLIM_EDILDI";
        // Tur 2: kilit cümlesi SUNUCUNUN gerçek fonksiyonundan + gerçek sözlükten, isteğin
        // Accept-Language başlığına göre (sunucunun Detay'ı ile aynı yol) üretilir.
        var kb = kaporaHareketId is null ? null
            : new Kuyumcu.Api.Modules.Siparis.SiparisServisi.KaporaBilgisi(2000m, DateTime.UtcNow, "ks1", kaporaStornolu);
        string? kilit = Kuyumcu.Api.Modules.Siparis.SiparisServisi.KaporaKilidi(durum, kb) is { } tr
            ? SunucuIletiSozlugu.Cevir(tr, dil) : null;
        return "{\"id\":" + j(id) + ",\"no\":\"S-2026-000001\",\"tur\":" + j(tur) + ",\"durum\":" + j(durum)
            + ",\"cariId\":" + j(CariA) + ",\"cariAd\":\"Ayşe Yılmaz\",\"subeId\":\"sb1\""
            + ",\"sozVerilenGun\":null,\"aciklama\":\"bilezik\",\"tahminiTutarTl\":null,\"kurSnapshotId\":null,"
            + "\"emanetHareketId\":null,\"geciken\":false,\"duzenlenebilir\":" + (durum == "TASLAK" ? "true" : "false")
            + ",\"terminal\":" + (iptal || teslim ? "true" : "false")
            + ",\"izinliGecisler\":[],\"geriYonluGecisler\":[],\"geriAlmaYetkisi\":true,\"geriAlmaRolleri\":[\"SAHIP\",\"MUDUR\"]"
            + ",\"gerekceIsteyenGecisler\":[\"IPTAL\"],\"gerekceAsgari\":10,\"olusturanKullaniciId\":\"u1\","
            + "\"olusturuldu\":\"2026-09-16T08:00:00Z\",\"guncellendi\":\"2026-09-16T08:00:00Z\","
            + "\"kalemler\":[{\"id\":\"k1\",\"urunId\":null,\"urunAd\":null,\"tanim\":\"yüzük\",\"ayarId\":\"K22\",\"gramAgirlik\":3.5,\"adet\":1.0,\"sira\":0}],"
            + "\"gecisler\":[],"
            + "\"ustaCariId\":" + j(ustaCariId) + ",\"ustaAd\":" + j(ustaCariId is null ? null : "Usta Hasan")
            + ",\"kaporaHareketId\":" + j(kaporaHareketId)
            + ",\"kaporaAlinabilir\":" + (kilit is null ? "true" : "false")
            + ",\"kaporaKilitSebebi\":" + j(kilit)
            + ",\"kaporaTutari\":" + (kaporaHareketId is null ? "null" : "2000.00")
            + ",\"kaporaTarihi\":null,\"kaporaKasaId\":" + j(kaporaHareketId is null ? null : "ks1")
            + ",\"kaporaStornolu\":" + (kaporaStornolu ? "true" : "false") + "}";
    }

    private sealed class SahteApi(EksikB1SiparisDetayEkranTestleri s) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (s.istekler) s.istekler.Add((istek.Method.Method, yol, govde));
            var dil = istek.Headers.AcceptLanguage.FirstOrDefault()?.Value;

            if (istek.Method == HttpMethod.Post && yol.EndsWith("/siparisler/s1/kapora"))
            {
                s.kaporaHareketId = "hk1";
                s.kaporaStornolu = false;
                return EksikB1Ortak.Yanit(HttpStatusCode.OK, "{\"siparis\":" + s.DetayJson() + ",\"kaporaHareketId\":\"hk1\"}");
            }
            if (istek.Method == HttpMethod.Post && yol.EndsWith("/islemler/hk1/storno"))
            {
                s.kaporaStornolu = true;
                return EksikB1Ortak.Yanit(HttpStatusCode.OK, """{"tersHareketId":"hk1-ters","kaynakHareketId":"hk1","tlTutari":-2000.00,"hasGrami":0}""");
            }
            if (istek.Method == HttpMethod.Patch && yol.EndsWith("/siparisler/s1"))
            {
                using var d = JsonDocument.Parse(govde);
                if (d.RootElement.TryGetProperty("ustaCariId", out var u)) s.ustaCariId = u.ValueKind == JsonValueKind.Null ? null : u.GetString();
                if (d.RootElement.TryGetProperty("tur", out var t)) s.tur = t.GetString()!;
                return EksikB1Ortak.Yanit(HttpStatusCode.OK, s.DetayJson());
            }

            string? g = yol switch
            {
                var y when y.EndsWith("/siparisler/s1") => s.DetayJson("s1", dil),
                var y when y.EndsWith("/siparisler/s2") => s.DetayJson("s2", dil, "IPTAL"),
                var y when y.EndsWith("/subeler") => """[{"id":"sb1","ad":"Merkez"}]""",
                var y when y.EndsWith("/cari") => $$"""[{"id":"{{CariA}}","ad":"Ayşe Yılmaz","tur":"MUSTERI"},{"id":"{{UstaB}}","ad":"Usta Hasan","tur":"TEDARIKCI"}]""",
                var y when y.EndsWith("/urunler") => """[{"id":"u0","kod":"K0","ad":"Ürün 0","ayarId":"K22","gramAgirlik":1.0}]""",
                var y when y.Contains("/kur/guncel") => s.kurVar ? """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":4200}""" : """{"id":"","altinHasFiyati":0}""",
                var y when y.EndsWith("/kasalar") => s.kasaSayisi switch
                {
                    0 => """[{"id":"ksd","ad":"Dolar Kasası","paraBirimi":"USD","aktif":true}]""",
                    1 => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},{"id":"ksp","ad":"Eski Kasa","paraBirimi":"TRY","aktif":false}]""",
                    _ => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},{"id":"ks2","ad":"Çarşı Kasa","paraBirimi":"TRY","aktif":true}]""",
                },
                _ => null,
            };
            return g is null
                ? EksikB1Ortak.Yanit(HttpStatusCode.NotFound, """{"message":"sahte API bu ucu tanımıyor"}""")
                : EksikB1Ortak.Yanit(HttpStatusCode.OK, g);
        }
    }

    public EksikB1SiparisDetayEkranTestleri() => EksikB1Ortak.Kur(this, new SahteApi(this));

    private IRenderedComponent<Kuyumcu.Web.Pages.SiparisDetay> Detay(string mod = ArayuzModuServisi.Profesyonel, string rol = "SAHIP")
    {
        EksikB1Ortak.Oturum(this, mod, rol);
        var e = Render<Kuyumcu.Web.Pages.SiparisDetay>(p => p.Add(x => x.Id, "s1"));
        e.WaitForElement("#sipd-kart");
        return e;
    }

    // ── B1-6: süreç şeridi ─────────────────────────────────────────────────

    [Fact]
    public void B16_serit_IKI_MODDA_hazir_asamasi_aktif_oncekiler_bitti()
    {
        foreach (var mod in EksikB1Ortak.Modlar)
        {
            var e = Detay(mod);
            var serit = e.Find("#sipd-adim-serit");
            Assert.Equal(4, serit.QuerySelectorAll("li").Length);
            Assert.Contains("adim-aktif", e.Find("#sipd-adim-2").ClassName);
            Assert.Equal("step", e.Find("#sipd-adim-2").GetAttribute("aria-current"));
            Assert.Contains("adim-bitti", e.Find("#sipd-adim-0").ClassName);
            Assert.Contains("adim-bitti", e.Find("#sipd-adim-1").ClassName);
            Assert.DoesNotContain("adim-bitti", e.Find("#sipd-adim-3").ClassName);
            Assert.Contains(M.AdimIslemde, serit.TextContent);
            e.Dispose();
        }
    }

    [Fact]
    public void B16_tamirde_ikinci_asama_Tamirde_iptalde_serit_yok()
    {
        tur = "TAMIR";
        durum = "ATOLYEDE";
        var e = Detay();
        Assert.Contains(M.AdimTamirde, e.Find("#sipd-adim-1").TextContent);
        Assert.Contains("adim-aktif", e.Find("#sipd-adim-1").ClassName);
        e.Dispose();

        durum = "IPTAL";
        var e2 = Detay();
        Assert.Empty(e2.FindAll("#sipd-adim-serit"));
    }

    // ── B1-5: usta ─────────────────────────────────────────────────────────

    [Fact]
    public void B15_usta_kartta_adiyla_ve_duzenlemede_PATCH_ile_gider()
    {
        tur = "TAMIR";
        durum = "TASLAK";
        var e = Detay();
        Assert.Equal(M.UstaYok, e.Find("#sipd-usta").TextContent.Trim());

        e.Find("#sipd-duzenle").Click();
        SeciciYardim.Sec(e, "sipd-d-usta", UstaB);
        e.Find("#sipd-d-kaydet").Click();

        var patch = istekler.Last(i => i.Metot == "PATCH");
        using (var d = JsonDocument.Parse(patch.Govde))
            Assert.Equal(UstaB, d.RootElement.GetProperty("ustaCariId").GetString());
        e.WaitForAssertion(() => Assert.Equal("Usta Hasan", e.Find("#sipd-usta").TextContent.Trim()));
    }

    [Fact]
    public void B15_musteri_siparisinde_usta_alani_yok_ve_PATCH_null_gonderir()
    {
        durum = "TASLAK";
        var e = Detay();
        Assert.Empty(e.FindAll("#sipd-usta"));
        e.Find("#sipd-duzenle").Click();
        Assert.Empty(e.FindAll("#sipd-d-usta"));
        e.Find("#sipd-d-kaydet").Click();
        var patch = istekler.Last(i => i.Metot == "PATCH");
        using var d = JsonDocument.Parse(patch.Govde);
        Assert.Equal(JsonValueKind.Null, d.RootElement.GetProperty("ustaCariId").ValueKind);
    }

    [Fact]
    public void B15_atolye_isinde_usta_bos_ise_PATCH_gitmez_alan_alti_mesaj()
    {
        tur = "ATOLYE_SIPARISI";
        durum = "TASLAK";
        var e = Detay();
        e.Find("#sipd-duzenle").Click();
        e.Find("#sipd-d-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
        e.WaitForAssertion(() => Assert.Contains(M.UstaSecin, e.Find("#sipd-duzenleme").TextContent));
    }

    // ── B1-5: kapora alma ──────────────────────────────────────────────────

    [Fact]
    public void B15_kapora_al_IKI_MODDA_govde_ayristirilir_basari_bandina_tutar_yazar()
    {
        foreach (var mod in EksikB1Ortak.Modlar)
        {
            kaporaHareketId = null;
            istekler.Clear();
            var e = Detay(mod);
            Assert.Equal(M.KaporaYok, e.Find("#sipd-kapora-yok").TextContent.Trim());
            e.Find("#sipd-kapora-al").Click();
            // Tek TL kasası: seçici ÇİZİLMEZ, değer salt okunur (UX-S22); pasif kasa listelenmez.
            e.WaitForElement("#sipd-kapora-kasa-sabit");
            Assert.Empty(e.FindAll("#sipd-kapora-kasa"));
            Assert.DoesNotContain("Eski Kasa", e.Find("#sipd-kapora-form").TextContent);

            e.Find("#sipd-kapora-tutar-g").Input("2.000");
            e.Find("#sipd-kapora-tutar-g").Blur();
            SeciciYardim.Sec(e, "sipd-kapora-odeme", "KART");
            e.Find("#sipd-kapora-kaydet").Click();

            e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/siparisler/s1/kapora")));
            var post = istekler.Last(i => i.Metot == "POST");
            using (var d = JsonDocument.Parse(post.Govde))
            {
                Assert.Equal(2000m, d.RootElement.GetProperty("tlTutari").GetDecimal());
                Assert.Equal("KART", d.RootElement.GetProperty("odemeTuru").GetString());
                Assert.Equal("ks1", d.RootElement.GetProperty("kasaId").GetString());
                Assert.Equal("kur-1", d.RootElement.GetProperty("kurSnapshotId").GetString());
            }
            e.WaitForAssertion(() =>
            {
                Assert.Contains(string.Format(M.KaporaAlindi, Bicim.Para(2000m, "TRY")), e.Find("#sipd-basari-bandi").TextContent);
                Assert.Contains(Bicim.Para(2000m, "TRY"), e.Find("#sipd-kapora-tutar").TextContent);
            });
            e.Dispose();
        }
    }

    [Fact]
    public void B15_kapora_bos_tutar_POST_gitmez_alan_alti_mesaj()
    {
        var e = Detay();
        e.Find("#sipd-kapora-al").Click();
        e.WaitForElement("#sipd-kapora-kasa-sabit");
        e.Find("#sipd-kapora-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        e.WaitForAssertion(() => Assert.Contains(M.TutarGerekli, e.Find("#sipd-kapora-form").TextContent));
    }

    [Fact]
    public void B15_cok_kasada_kasa_secilmeden_POST_gitmez()
    {
        kasaSayisi = 2;
        var e = Detay();
        e.Find("#sipd-kapora-al").Click();
        e.WaitForElement("#sipd-kapora-kasa");
        e.Find("#sipd-kapora-tutar-g").Input("500");
        e.Find("#sipd-kapora-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        e.WaitForAssertion(() => Assert.Contains(M.KasaGerekli, e.Find("#sipd-kapora-form").TextContent));
    }

    [Fact]
    public void B15_TL_kasasi_yoksa_kaydet_kilitli_ve_sebep_yazar()
    {
        kasaSayisi = 0;
        var e = Detay();
        e.Find("#sipd-kapora-al").Click();
        e.WaitForAssertion(() =>
        {
            Assert.True(e.Find("#sipd-kapora-kaydet").HasAttribute("disabled"));
            Assert.Contains(M.KasaYok, e.Markup);
        });
    }

    [Fact]
    public void B15_iptal_sipariste_kapora_al_yok_sunucu_sebebi_yazar()
    {
        durum = "IPTAL";
        var e = Detay();
        Assert.Empty(e.FindAll("#sipd-kapora-al"));
        Assert.Contains("İptal edilmiş", e.Find("#sipd-kapora-kilit").TextContent);
    }

    // Tur 2 (doğrulayıcı KRİTİK bulgusu): EN modda kilit sebebi İngilizce görünür.
    // Sahte API kilidi sunucunun KaporaKilidi + SunucuIletiSozlugu.Cevir yoluyla ve isteğin
    // Accept-Language başlığıyla üretir; sözlükte karşılık yoksa Türkçe sızar ve test kırmızıdır.
    [Theory]
    [InlineData("IPTAL", false, "A deposit cannot be taken on a cancelled order.")]
    [InlineData("TESLIM_EDILDI", false, "A deposit cannot be taken on a delivered order.")]
    // Canlı kapora varken ekran kilit satırı DEĞİL kapora kartını (tutar · makbuz) gösterir;
    // "zaten alındı" cümlesinin EN karşılığı Dil_kapora_kilidi_… ve HTTP 409 testinde sınanır.
    public async Task B15_EN_modda_kapora_kilit_sebebi_ingilizce(string d, bool canliKapora, string beklenen)
    {
        var yayilim = Bicim.DilYayilimi; var onceki = Bicim.Dil;
        try
        {
            durum = d;
            kaporaHareketId = canliKapora ? "hk1" : null;
            Bicim.DilYayilimi = true;
            await Services.GetRequiredService<DilServisi>().DegistirAsync("en");
            var e = Detay();
            var kilit = e.Find("#sipd-kapora-kilit").TextContent.Trim();
            Assert.StartsWith(beklenen, kilit);
            Assert.DoesNotMatch("[çğıİöşüÇĞÖŞÜ]", kilit);
            Assert.Contains("<h3>" + EksikB1Sozlugu.En.KaporaBaslik + "</h3>", e.Markup);
            Assert.Contains(istekler, i => i.Yol.EndsWith("/siparisler/s1"));
        }
        finally { Bicim.DilYayilimi = yayilim; Bicim.Dil = onceki; }
    }

    // Tur 2 (doğrulayıcı ORTA bulgusu): rota başka siparişe geçince kapora çekmecesi kapanır,
    // girilen tutar yeni siparişe taşınmaz.
    [Fact]
    public void B15_rota_degisince_kapora_cekmecesi_kapanir_form_temizlenir()
    {
        var e = Detay();
        e.Find("#sipd-kapora-al").Click();
        e.WaitForElement("#sipd-kapora-kasa-sabit");
        e.Find("#sipd-kapora-tutar-g").Input("750");
        Assert.NotEmpty(e.FindAll("#sipd-kapora-form"));

        e.Render(p => p.Add(x => x.Id, "s2"));
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/siparisler/s2")));
        e.WaitForAssertion(() =>
        {
            Assert.Empty(e.FindAll("#sipd-kapora-form"));
            Assert.Empty(e.FindAll("#sipd-kapora-kaydet"));
            Assert.Contains("İptal edilmiş", e.Find("#sipd-kapora-kilit").TextContent);
        });
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    // ── B1-5: kapora canlı → makbuz + geri verme (48-A storno) ─────────────

    [Fact]
    public void B15_canli_kapora_makbuz_ve_geri_verme_storno_govdesi()
    {
        kaporaHareketId = "hk1";
        durum = "IPTAL";
        var e = Detay();
        Assert.Empty(e.FindAll("#sipd-kapora-al"));
        Assert.Equal("/cikti/TAHSILAT_MAKBUZU/hk1?donus=%2Fsiparisler%2Fs1", e.Find("#sipd-kapora-makbuz").GetAttribute("href"));
        Assert.Empty(e.FindAll("#sipd-kapora-not"));   // KAPORA-EKRAN: açıklama cümlesi yok

        e.Find("#sipd-kapora-geri").Click();
        var onay = e.WaitForElement("#sipd-kapora-geri-onayla");
        Assert.True(onay.HasAttribute("disabled"));                         // gerekçe yok → kilitli + sebep
        Assert.NotEmpty(e.FindAll("#sipd-kapora-geri-gerekce-kilit"));
        e.Find("#sipd-kapora-geri-gerekce").Input("Sipariş iptal edildi, kapora müşteriye geri verildi");
        e.WaitForAssertion(() => Assert.False(e.Find("#sipd-kapora-geri-onayla").HasAttribute("disabled")));
        e.Find("#sipd-kapora-geri-onayla").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/hk1/storno")));
        var post = istekler.Last(i => i.Metot == "POST");
        using (var d = JsonDocument.Parse(post.Govde))
        {
            Assert.Equal("ks1", d.RootElement.GetProperty("kasaId").GetString());
            Assert.Equal("kur-1", d.RootElement.GetProperty("kurSnapshotId").GetString());
            Assert.True(d.RootElement.GetProperty("gerekce").GetString()!.Length >= 20);
        }
        e.WaitForAssertion(() =>
        {
            Assert.Contains(M.KaporaGeriVerildi, e.Find("#sipd-basari-bandi").TextContent);
            Assert.Contains(M.KaporaStornolu, e.Find("#sipd-kapora-stornolu").TextContent);
            Assert.Empty(e.FindAll("#sipd-kapora-geri"));
        });
    }

    [Fact]
    public void B15_tezgahtar_kaporayi_geri_veremez_dugme_yok_sebep_yazar()
    {
        kaporaHareketId = "hk1";
        var e = Detay(rol: "TEZGAHTAR");
        Assert.Empty(e.FindAll("#sipd-kapora-geri"));   // KAPORA-EKRAN: silik düğme yok; sebep okunur cümle
        Assert.Equal(M.GeriVerYetki, e.Find("#sipd-kapora-geri-kilit").TextContent.Trim());
    }

    [Fact]
    public void B15_her_kapora_dugmesi_istek_atar_ya_da_kilitli_ve_sebepli()
    {
        // DoD EK md.1: kapora kartındaki düğmeler — "tıklanınca hiçbir şey olmayan" düğme yok.
        var e = Detay();
        var al = e.Find("#sipd-kapora-al");
        Assert.False(al.HasAttribute("disabled"));
        al.Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/kasalar")));
    }
}
