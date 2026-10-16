using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S3-2 — MÜŞTERİLER (/cariler): LİSTE + ÇEKMECE
//
// QA tur 8: satırda 8-9 eylem, satır 305-329 px, görsel 2 / dostu 2.
// Hedef (plan § 6 S-3, tuval Musteriler.dc.html not n3):
//   · satırda ≤ 3 eylem + "⋯" — geri kalan HER eylem "⋯" penceresinde (silinmez);
//   · düzenleme sağ çekmecede (Cekmece.razor);
//   · basit modda form ≤ 8 alan, gerisi "Daha fazla ▾" (katlı alanın DEĞERİ korunur).
// Bu dosya her kuralı İSTEK ve DOM düzeyinde sınar; "düğme var" yetmez.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S32MusterilerEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    // c1: aktif müşteri, USD bakiyeli · c2: aktif tedarikçi · c3: PASİF müşteri
    // pm: Peşin Müşteri sistem kartı.
    private string cariListesi = """
        [{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567",
          "email":"ayse@ornek.com","tlBakiye":-1500.00,"hasGramBakiye":2.5,
          "usdBakiye":1250.00,"eurBakiye":0,"gbpBakiye":0,"aktif":true,"sistemCarisi":false},
         {"id":"c2","ad":"Kapalıçarşı Toptan","tur":"TEDARIKCI","telefon":null,
          "email":null,"tlBakiye":0,"hasGramBakiye":0,
          "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"aktif":true,"sistemCarisi":false},
         {"id":"c3","ad":"Eski Müşteri","tur":"MUSTERI","telefon":null,
          "email":null,"tlBakiye":0,"hasGramBakiye":0,
          "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"aktif":false,"sistemCarisi":false},
         {"id":"pm","ad":"Peşin Müşteri","tur":"MUSTERI","telefon":null,
          "email":null,"tlBakiye":0,"hasGramBakiye":0,
          "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"aktif":true,"sistemCarisi":true}]
        """;

    private const string DovizsizListe = """
        [{"id":"c2","ad":"Kapalıçarşı Toptan","tur":"TEDARIKCI","telefon":null,
          "email":null,"tlBakiye":0,"hasGramBakiye":0,
          "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"aktif":true,"sistemCarisi":false}]
        """;

    public S32MusterilerEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => cariListesi))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<string> liste) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            var g = yol switch
            {
                // Gerçekçi yanıtlar — '{}' varsayılanı döviz düğmelerini sessizce kilitlerdi.
                var y when y.EndsWith("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"gbpTry":null}""",
                var y when y.Contains("/ekstre") => """{"bakiye":{"tlBakiye":-1500,"hasGramBakiye":2.5},"hareketler":[]}""",
                var y when y.Contains("/bagli-kayitlar") => """{"acikEmanet":0,"acikSiparis":0,"acikCek":0}""",
                // Tam kart: katlı alanların DEĞERİ burada — PATCH aynen geri yazmalı.
                var y when y.EndsWith("/cari/c1") => """
                    {"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567",
                     "email":"ayse@ornek.com","vergiNo":"1234567890","adres":"Şişli, İstanbul",
                     "krediLimitiTl":5000,"varsayilanVadeGun":30,"hesapKodu":"120.01",
                     "notlar":"iyi müşteri","pepBeyani":false,"pepGorev":null,"pepBeyanTarihi":null}
                    """,
                // Eski/bozuk veri: 3 haneli vergi no — katlı alanda HATA doğar.
                var y when y.EndsWith("/cari/c2") => """
                    {"id":"c2","ad":"Kapalıçarşı Toptan","tur":"TEDARIKCI","telefon":null,
                     "email":null,"vergiNo":"123","adres":null,
                     "pepBeyani":false,"pepGorev":null,"pepBeyanTarihi":null}
                    """,
                // PEP beyanlı kart — basit modda da beyan GÖRÜNÜR.
                var y when y.EndsWith("/cari/c3") => """
                    {"id":"c3","ad":"Eski Müşteri","tur":"MUSTERI","telefon":null,
                     "email":null,"vergiNo":null,"adres":null,
                     "pepBeyani":true,"pepGorev":"Belediye meclis üyesi","pepBeyanTarihi":"2026-09-01T10:00:00Z"}
                    """,
                var y when y.EndsWith("/cari") => liste(),
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","aktif":true}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Cariler> Ciz(string mod = ArayuzModuServisi.Profesyonel, string rol = "SAHIP")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1", mod, mod));
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        istekler.Clear();
        return e;
    }

    private static string? Alan(string govde, string ad)
    {
        var k = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return k.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    /// <summary>Satırda GÖRÜNEN eylemler: yalnız .satir-eylem içindekiler ("⋯" penceresi hariç).</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> GorunenEylemler(
        IRenderedComponent<Kuyumcu.Web.Pages.Cariler> e, string id) =>
        e.Find($"#c-eylem-{id}").QuerySelectorAll("button").ToList();

    // ── (1) SATIRDA EN ÇOK 3 EYLEM ─────────────────────────────────────────

    [Theory]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    [InlineData(ArayuzModuServisi.Basit)]
    public void S32_01_Her_satirda_en_cok_uc_gorunen_eylem_vardir_ucuncusu_menu(string mod)
    {
        var e = Ciz(mod);
        foreach (var id in new[] { "c1", "c2", "c3", "pm" })
        {
            var d = GorunenEylemler(e, id);
            Assert.Equal(3, d.Count);
            Assert.Equal($"c-menu-ac-{id}", d[2].Id);
            Assert.Equal($"c-duzenle-{id}", d[1].Id);
        }
        // Satırda GÖRÜNEN eylemler dışındaki her düğme "⋯" penceresinin içindedir.
        foreach (var satir in e.FindAll("tr.cari-satir"))
        {
            var disarida = satir.QuerySelectorAll("button")
                .Where(b => b.Closest(".satir-menu") is null && b.Closest(".satir-eylem") is null
                            && b.Closest(".satir-ici") is null)
                .ToList();
            Assert.Empty(disarida);
        }
    }

    // ── (2) ÖZELLİK SİLİNMEDİ: eski eylemlerin HEPSİ menüde, id'ler AYNEN ──

    [Fact]
    public void S32_02_Eski_eylemlerin_hepsi_menude_durur_idler_aynen()
    {
        var e = Ciz();
        var menu = e.Find("#c-menu-c1");
        foreach (var id in new[] { "c-detay-c1", "c-veresiye-c1", "c-duzeltme-c1",
                                   "c-doviz-tahsilat-c1", "c-doviz-veresiye-c1", "c-pasife-al-c1" })
            Assert.NotNull(menu.QuerySelector($"#{id}"));
        // Aktife al yalnız PASİF satırda ve GÖRÜNÜR (geri dönüş tek tık).
        Assert.Equal("c-aktife-al-c3", GorunenEylemler(e, "c3")[0].Id);
        Assert.Empty(e.FindAll("#c-pasife-al-c3"));
        // Pasif satırın tahsilat yolu kaybolmadı: menüde.
        Assert.NotNull(e.Find("#c-menu-c3").QuerySelector("#c-tahsilat-c3"));
        // Aktif satırda tahsilat GÖRÜNÜR birinci eylem.
        Assert.Equal("c-tahsilat-c1", GorunenEylemler(e, "c1")[0].Id);
    }

    [Fact]
    public void S32_03_Menu_yerli_popover_her_oge_pencereyi_kapatir()
    {
        var e = Ciz();
        var ac = e.Find("#c-menu-ac-c1");
        Assert.Equal("c-menu-c1", ac.GetAttribute("popovertarget"));
        Assert.False(string.IsNullOrWhiteSpace(ac.GetAttribute("aria-label")));
        var menu = e.Find("#c-menu-c1");
        Assert.Equal("auto", menu.GetAttribute("popover"));
        var ogeler = menu.QuerySelectorAll("button").ToList();
        Assert.True(ogeler.Count >= 6);
        Assert.All(ogeler, b =>
        {
            Assert.Equal("c-menu-c1", b.GetAttribute("popovertarget"));
            Assert.Equal("hide", b.GetAttribute("popovertargetaction"));
        });
    }

    [Fact]
    public void S32_04_Sistem_carisi_kilit_rozeti_ad_hucresinde_pasife_alma_yok()
    {
        var e = Ciz();
        var kilit = e.Find("#c-sistem-carisi-pm");
        Assert.NotNull(kilit.Closest("td.hucre-vurgu"));
        Assert.Empty(e.FindAll("#c-pasife-al-pm"));
        Assert.Empty(e.FindAll("#c-aktife-al-pm"));
    }

    // ── (3) MENÜ ÖĞESİ GERÇEK İSTEK ATAR (DoD EK/1) ────────────────────────

    [Fact]
    public void S32_05_Menuden_Detay_ekstre_ister_Pasife_al_onayla_DELETE_atar()
    {
        var e = Ciz();
        e.Find("#c-detay-c1").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/cari/c1/ekstre")));

        istekler.Clear();
        e.Find("#c-pasife-al-c1").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#c-pasif-onayla")));
        e.Find("#c-pasif-onayla").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "DELETE" && i.Yol.EndsWith("/cari/c1")));
    }

    [Fact]
    public void S32_06_Gorunen_Tahsilat_formu_acar()
    {
        var e = Ciz();
        e.Find("#c-tahsilat-c1").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#th-kaydet")));
    }

    // ── (4) BASİT MOD: SÜTUNLAR ────────────────────────────────────────────

    [Fact]
    public void S32_07_Basit_modda_tur_ve_durum_sutun_degil_ad_hucresinde_rozet()
    {
        var pro = Ciz(ArayuzModuServisi.Profesyonel);
        var proBaslik = pro.FindAll("table.veri-tablosu thead th").Count;

        var e = Ciz(ArayuzModuServisi.Basit);
        var basitBaslik = e.FindAll("table.veri-tablosu thead th").Count;
        // TOPLU-5: PRO'da Durum sütunu yalnız "Pasifleri göster" açıkken (kapalıyken hep "Aktif"tir) → fark yalnız Tür.
        Assert.Equal(proBaslik - 1, basitBaslik);   // Tür sütunu yok (Durum iki yüzde de ad hücresinde rozet)
        var c2Ad = e.Find("#c-satir-c2 td");
        Assert.Contains(Sozlukler.Tr.Cari.Tedarikci, c2Ad.TextContent);
        var c3Ad = e.Find("#c-satir-c3 td");
        Assert.Contains(Sozlukler.Tr.Ortak.Pasif, c3Ad.TextContent);
        // Müşteri türü rozet olarak TEKRARLANMAZ (gürültü yok).
        Assert.DoesNotContain(Sozlukler.Tr.Cari.Musteri, e.Find("#c-satir-c1 td").TextContent);
    }

    [Fact]
    public void S32_08_Basit_modda_doviz_sutunu_yalniz_doviz_bakiyesi_varsa_cizilir()
    {
        var dovizli = Ciz(ArayuzModuServisi.Basit);
        Assert.Contains(dovizli.FindAll("table.veri-tablosu thead th"),
            th => th.TextContent.Contains(Sozlukler.Tr.Cari.DovizBakiye));

        cariListesi = DovizsizListe;
        var dovizsiz = Render<Kuyumcu.Web.Pages.Cariler>();
        Assert.DoesNotContain(dovizsiz.FindAll("table.veri-tablosu thead th"),
            th => th.TextContent.Contains(Sozlukler.Tr.Cari.DovizBakiye));
    }

    // ── (5) BASİT MOD: FORM ≤ 8 ALAN + "DAHA FAZLA" ─────────────────────────

    private static int GorunenAlanSayisi(AngleSharp.Dom.IElement kok) =>
        kok.QuerySelectorAll("input:not([type=hidden]), select, textarea").Length;

    [Fact]
    public void S32_09_Basit_modda_yeni_form_uc_alan_Daha_fazla_ile_hepsi_acilir()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.Find("#cari-yeni-ac").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#cari-ad")));
        Assert.NotEmpty(e.FindAll("#cari-telefon"));
        Assert.NotEmpty(e.FindAll("#cari-notlar"));
        Assert.Empty(e.FindAll("#cari-eposta"));
        Assert.Empty(e.FindAll("#cari-vergino"));
        var dugme = e.Find("#cari-daha-fazla");
        Assert.Equal("false", dugme.GetAttribute("aria-expanded"));
        var kok = e.Find("#cari-ad").Closest(".form-dikey")!;
        Assert.InRange(GorunenAlanSayisi(kok), 1, 8);

        dugme.Click();
        Assert.Equal("true", e.Find("#cari-daha-fazla").GetAttribute("aria-expanded"));
        foreach (var id in new[] { "cari-tur", "cari-eposta", "cari-vergino", "cari-adres", "cari-limit", "cari-fiyat-listesi", "cari-hesap-kodu" })
            Assert.NotEmpty(e.FindAll($"#{id}"));
    }

    [Fact]
    public void S32_10_Profesyonel_modda_Daha_fazla_yok_tum_alanlar_acik()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        e.Find("#cari-yeni-ac").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#cari-eposta")));
        Assert.Empty(e.FindAll("#cari-daha-fazla"));
        Assert.NotEmpty(e.FindAll("#cari-vergino"));
    }

    [Fact]
    public void S32_11_Basit_modda_katli_alanlar_PATCHte_AYNEN_geri_yazilir_silinmez()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.Find("#c-duzenle-c1").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#duz-ad")));
        Assert.Empty(e.FindAll("#duz-vergino"));      // katlı
        Assert.NotEmpty(e.FindAll("#duz-daha-fazla"));
        var kok = e.Find("#duz-ad").Closest(".form-dikey")!;
        Assert.InRange(GorunenAlanSayisi(kok), 1, 8);

        istekler.Clear();
        e.Find("#cekmece-cari-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/cari/c1")));
        var govde = istekler.Single(i => i.Metot == "PATCH" && i.Yol.EndsWith("/cari/c1")).Govde;
        Assert.Equal("1234567890", Alan(govde, "vergiNo"));
        Assert.Equal("Şişli, İstanbul", Alan(govde, "adres"));
        Assert.Equal("ayse@ornek.com", Alan(govde, "email"));
        Assert.Equal("5000", Alan(govde, "krediLimitiTl"));
        Assert.Equal("30", Alan(govde, "varsayilanVadeGun"));
        Assert.Equal("120.01", Alan(govde, "hesapKodu"));
        Assert.Equal("MUSTERI", Alan(govde, "tur"));
    }

    [Fact]
    public void S32_12_Katli_alanda_hata_varsa_grup_kendiliginden_acilir_istek_atilmaz()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.Find("#c-duzenle-c2").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#duz-ad")));
        Assert.Empty(e.FindAll("#duz-vergino"));

        istekler.Clear();
        e.Find("#cekmece-cari-kaydet").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#duz-vergino")));
        Assert.Equal("true", e.Find("#duz-daha-fazla").GetAttribute("aria-expanded"));
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
    }

    [Fact]
    public void S32_13_PEP_beyanli_kartta_beyan_basit_modda_da_gorunur()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.Find("#c-duzenle-c3").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#duz-ad")));
        Assert.NotEmpty(e.FindAll("#duz-pep"));
        Assert.NotEmpty(e.FindAll("#duz-pep-gorev"));
        Assert.Empty(e.FindAll("#duz-vergino"));      // diğerleri katlı

        // PEP beyansız kartta basit modda beyan katlıdır.
        e.Find("#cekmece-cari-iptal").Click();
        e.Find("#c-duzenle-c1").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#duz-ad")));
        Assert.Empty(e.FindAll("#duz-pep"));
    }

    // ── (6) SÖZLÜK: TR + EN, jargon yok ─────────────────────────────────────

    [Fact]
    public void S32_14_Sozluk_TR_EN_dolu_ve_farkli_jargon_yok()
    {
        var tr = MusteriListeSozlugu.Tr;
        var en = MusteriListeSozlugu.En;
        var trAlan = tr.GetType().GetProperties().Select(p => (string)p.GetValue(tr)!).ToList();
        var enAlan = en.GetType().GetProperties().Select(p => (string)p.GetValue(en)!).ToList();
        Assert.All(trAlan, m => Assert.False(string.IsNullOrWhiteSpace(m)));
        Assert.All(enAlan, m => Assert.False(string.IsNullOrWhiteSpace(m)));
        for (var i = 0; i < trAlan.Count; i++) Assert.NotEqual(trAlan[i], enAlan[i]);
        foreach (var jargon in new[] { "Cari", "cari", "storno", "Storno" })
            Assert.All(trAlan, m => Assert.DoesNotContain(jargon, m));
        Assert.Same(en, MusteriListeSozlugu.Al("en"));
        Assert.Same(tr, MusteriListeSozlugu.Al("tr"));
    }
}
