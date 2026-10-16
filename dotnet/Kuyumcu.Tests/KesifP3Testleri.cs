using System.Net;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MHurda = Kuyumcu.Api.Modules.Hurda;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P3 · BELGE — ham ayar kodu (#64 #66 #78) ve gider pusulası tablo
// düzeni (#11, P7 devri K3). Fatura/denetim/kasa testleri kendi ekran test
// sınıflarında (FaturaEkranTestleri, ZoBelgeTestleri) — aynı sahte API.
// ⚠ Sahte API her uca GERÇEKÇİ yanıt döndürür ('{}' varsayılanı yok).
// ═════════════════════════════════════════════════════════════════════════════

internal static class KesifP3Ortak
{
    /// <summary>Kabul ölçütü: görünür metinde ham ayar kodu 0 (K22 · G925 · K24).</summary>
    public static readonly System.Text.RegularExpressions.Regex HamAyarKodu =
        new(@"\b[KG]\d{2,3}\b", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
}

public sealed class KesifP3GiderPusulasiEkranTestleri : BunitContext
{
    private const string Liste = """
        [{"id":"gp1","pusulaNo":"GP-2026-000001","hareketId":null,"saticiTipi":"NIHAI_TUKETICI",
          "saticiAdSoyad":"Tuna Akarsu","saticiTckn":"*******0146","ayarId":"K22","gramAgirlik":4.35,
          "hasGrami":3.9846,"brutTutar":25875.47,"stopajOran":0.0,"stopajTutar":0.00,
          "netOdenen":25875.47,"teslimTarihi":"2099-09-01T00:00:00Z",
          "duzenlemeSonTarihi":"2099-09-08T00:00:00Z","gecikti":false,"kalanGun":30,
          "iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null},
         {"id":"gp2","pusulaNo":"GP-2026-000002","hareketId":null,"saticiTipi":"NIHAI_TUKETICI",
          "saticiAdSoyad":"Ayşe Kaya","saticiTckn":"*******0220","ayarId":"G925","gramAgirlik":50.0,
          "hasGrami":46.25,"brutTutar":3000.00,"stopajOran":0.0,"stopajTutar":0.00,
          "netOdenen":3000.00,"teslimTarihi":"2099-09-01T00:00:00Z",
          "duzenlemeSonTarihi":"2099-09-08T00:00:00Z","gecikti":false,"kalanGun":30,
          "iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null}]
        """;

    private const string Detay = """
        {"id":"gp1","pusulaNo":"GP-2026-000001","saticiTipi":"NIHAI_TUKETICI","saticiAdSoyad":"Tuna Akarsu",
         "saticiTckn":"10000000146","saticiAdres":"Kadıköy","ayarId":"K22","gramAgirlik":4.35,
         "hasGrami":3.9846,"malAciklamasi":"22 ayar hurda bilezik","brutTutar":25875.47,
         "stopajOran":0.0,"stopajTutar":0.00,"netOdenen":25875.47,
         "teslimTarihi":"2099-09-01T00:00:00Z","duzenlemeSonTarihi":"2099-09-08T00:00:00Z",
         "subeId":null,"cariId":null,"hareketId":null,"duzeltilenId":null,
         "olusturuldu":"2099-09-01T00:00:00Z","iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null}
        """;

    public KesifP3GiderPusulasiEkranTestleri()
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
        Services.AddScoped(_ => new HttpClient(new SahteApi())
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/gider-pusulalari/sure-takibi") => """
                    {"sure":{"gun":7,"dayanak":"VUK md.234"},"gecikmisSayisi":0,"yaklasanSayisi":0,
                     "gecikmis":[],"yaklasan":[],"uyarilar":[]}
                    """,
                var y when y.Contains("/gider-pusulalari/") => Detay,
                var y when y.EndsWith("/gider-pusulalari") => Liste,
                var y when y.EndsWith("/yetkiler/benim") => """{"moduller":["cari","urun"]}""",
                var y when y.Contains("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6827.20,"gramAltinFiyati":6861.51}""",
                _ => "[]",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    [Fact]
    public void GiderPusulalari_ayar_adi()
    {
        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>();
        c.WaitForState(() => c.FindAll("#gp-tablo tbody tr").Count == 2);

        // Liste: ayar ADI görünür, ham kod yalnız title'da; maden adı korunur (gümüş).
        Assert.StartsWith("22 Ayar ·", c.Find("#gp-mal-gp1").TextContent.Trim());
        Assert.Equal("K22", c.Find("#gp-mal-gp1").GetAttribute("title"));
        Assert.StartsWith("925 Gümüş ·", c.Find("#gp-mal-gp2").TextContent.Trim());
        Assert.DoesNotMatch(KesifP3Ortak.HamAyarKodu, c.Find("#gp-tablo").TextContent);

        // Detay: aynı tek kaynak.
        c.Find("#gp-detay-gp1").Click();
        c.WaitForElement("#gp-detay-mal");
        Assert.StartsWith("22 Ayar ·", c.Find("#gp-detay-mal").TextContent.Trim());
        Assert.Equal("K22", c.Find("#gp-detay-mal").GetAttribute("title"));
    }

    [Fact]
    public void GiderPusulalari_tablo_kart_yigini_ve_tek_satir_hucreler()
    {
        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>();
        c.WaitForState(() => c.FindAll("#gp-tablo tbody tr").Count == 2);
        var tablo = c.Find("#gp-tablo");
        // #11: dar kapta kart yığını (zo-belge.css container kuralı) — sınıflar yerinde.
        Assert.Contains("zo-yigin-dar", tablo.ClassList);
        Assert.Contains("q10-yigin", tablo.ClassList);
        Assert.NotNull(c.Find(".zo-belge #gp-tablo"));
        Assert.Equal("golge", tablo.ParentElement!.GetAttribute("data-zo"));   // PAKET GOC-A: ızgara kabı da taşır (KartListe.Zo)
        Assert.NotNull(c.Find(".izgara-kap #gp-tablo"));
        // Eylem hücresi kart düzeninde tam satır; diğer her hücre kendi etiketini taşır.
        var hucreler = c.FindAll("#gp-tablo tbody tr:first-child td");
        Assert.Contains("q10-yigin-eylem", hucreler.Last().ClassList);
        Assert.All(hucreler.Take(hucreler.Count - 1), td => Assert.False(string.IsNullOrEmpty(td.GetAttribute("data-etiket"))));
        // K3 (P7 devri): ad, tutar ve numara bölünmez; TCKN ayrı sütun değil (8 sütun).
        Assert.Equal(8, c.FindAll("#gp-tablo thead th").Count);
        Assert.Equal(hucreler.Count, c.FindAll("#gp-tablo thead th").Count);
        Assert.All(hucreler.Take(hucreler.Count - 1), td => Assert.Contains("hucre-tek-satir", td.ClassList));
        Assert.NotNull(c.Find("#gp-tckn-gp1"));
        // Satır içi style yok.
        Assert.Empty(c.FindAll("#gp-tablo [style]"));
    }

    /// <summary>KESIF-P3 tasarım hakemi tur 2: detay çekmecesi iki sütunlu etiket/değer ızgarası;
    /// değişmezlik gerekçeleri listenin içinde DEĞİL, tek KAPALI ipucunda; has Bicim.Has (4 hane).</summary>
    [Fact]
    public void GiderPusulalari_detay_izgara_tek_kapali_ipucu_has_dort_hane()
    {
        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>();
        c.WaitForState(() => c.FindAll("#gp-tablo tbody tr").Count == 2);
        c.Find("#gp-detay-gp1").Click();
        c.WaitForElement("#gp-detay-mal");
        Assert.Contains("zo-tanim-izgara", c.Find("#gp-detay-liste").ClassList);
        Assert.Empty(c.FindAll("#gp-detay-liste .degismez-ipucu"));
        var kapali = c.Find("#gp-degismez");
        Assert.Equal("DETAILS", kapali.TagName);
        Assert.False(kapali.HasAttribute("open"));
        // Çekmecedeki bütün değişmez kutuları bu tek ipucunun İÇİNDE.
        Assert.Equal(c.FindAll("#cekmece-gider-pusulasi .degismez-ipucu").Count,
                     c.FindAll("#gp-degismez .degismez-ipucu").Count);
        Assert.EndsWith("3,9846 has", c.Find("#gp-detay-mal").TextContent.Trim());
    }

    /// <summary>BASİT: tek sade gerekçe, yasal madde numarası (VUK/KVKK md.) YOK.</summary>
    [Fact]
    public void GiderPusulalari_detay_BASIT_tek_sade_gerekce_madde_yok()
    {
        var m = Services.GetRequiredService<ArayuzModuServisi>();
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => m.BasitMi);
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1", "BASIT", "BASIT"));
        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>();
        c.WaitForState(() => c.FindAll("#gp-tablo tbody tr").Count == 2);
        c.Find("#gp-detay-gp1").Click();
        c.WaitForElement("#gp-degismez");
        Assert.Single(c.FindAll("#cekmece-gider-pusulasi .degismez-ipucu"));
        Assert.Contains(KesifP3Sozlugu.Tr.PusulaDegismezSade, c.Find("#degismez-ipucu-GiderPusulasi").TextContent);
        // Çekmecenin TAMAMINDA (liste, ipucu, iptal düğmesi) madde numarası yok.
        var cekmece = c.Find("#cekmece-gider-pusulasi").TextContent;
        Assert.DoesNotContain("md.", cekmece);
        Assert.DoesNotContain("KVKK", cekmece);
        Assert.Equal(KesifP3Sozlugu.Tr.PusulaIptalEtSade, c.Find("#gp-iptal-gp1").TextContent.Trim());
    }
}

public sealed class KesifP3RaporEkranTestleri : BunitContext
{
    private const string Stok = """
        {"urunler":[{"urunId":"u1","kod":"BLZ-1","ad":"Bilezik","ayarId":"K24","adet":2,"toplamHas":18.32},
                    {"urunId":"u2","kod":"GMS-1","ad":"Gümüş zincir","ayarId":"G925","adet":3,"toplamHas":299.7}],
         "toplamHas":18.32,
         "madenDagilimi":[{"maden":"ALTIN","madenAdi":"Altın","adet":2,"toplamHas":18.32},
                          {"maden":"GUMUS","madenAdi":"Gümüş","adet":3,"toplamHas":299.7}],
         "toplamSatir":2,"ofset":0,"limit":500,"dahaFazlasiVar":false,"uyari":null}
        """;

    public KesifP3RaporEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi())
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/aylik") => """
                    {"yil":2026,"ay":9,"satis":{"toplamTl":0,"adet":0},"alis":{"toplamTl":0,"adet":0},
                     "tahsilat":{"toplamTl":0},"veresiye":{"toplamTl":0},"karTl":null,"karKaynagi":"HESAPLANAMADI",
                     "satisAlisFarkiTl":0,"netHasGramHareketi":0,"madenDagilimi":[],"madenUyarisi":null,
                     "hareketSayisi":0,"gunler":[]}
                    """,
                var y when y.EndsWith("/raporlar/cari-yaslandirma") =>
                    """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""",
                var y when y.EndsWith("/raporlar/stok-degeri") => Stok,
                var y when y.EndsWith("/raporlar/reyon-stok") =>
                    """{"reyonlar":[],"atanmamis":[],"toplamAltinHasGram":0,"uyari":null}""",
                var y when y.EndsWith("/raporlar/stok-yaslandirma") =>
                    """{"olculduAn":"2026-09-22T06:00:00Z","subeId":null,"kovalar":[],"toplamAdet":0,"altinHasGram":0,"madenDagilimi":[],"yasiBilinmeyenAdet":0,"devir":null,"oluStok":null,"guvenilirlik":"YUKSEK","uyarilar":[]}""",
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") =>
                    """{"tarih":"2026-09-17","tahsilat":{"nakit":0,"kart":0,"havale":0,"diger":0,"toplam":0},"satis":{"sayisi":0,"toplamTl":0,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":0,"faturasizTl":0},"alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},"hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},"fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},"fark":0,"mutabik":true,"uyarilar":[],"faturasizIslemler":[],"faturaEslesme":null}""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    [Fact]
    public void Raporlar_stok_ayar_adi()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#rapor-stok-has-baslik")));
        var tablo = e.Find("#rapor-stok-has-baslik").Closest("table")!;
        var metin = tablo.TextContent;
        Assert.Contains("24 Ayar", metin);
        Assert.Contains("925 Gümüş", metin);
        Assert.DoesNotMatch(KesifP3Ortak.HamAyarKodu, metin);
        // Ham kod yalnız title ipucunda (destek).
        // PAKET GOC-D: rapor-stok ortak ızgarada; ham kod ipucu hücrenin İÇ öğesinde (ızgara td özniteliği satıra göre değişmez).
        Assert.Contains(tablo.QuerySelectorAll("td [title]"), td => td.GetAttribute("title") == "K24");
    }
}

[Collection("veritabani")]
public sealed class KesifP3PusulaBelgeTestleri(TestVeritabani vt)
{
    private static System.Text.Json.JsonElement Kok(object o) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o)).RootElement;

    /// <summary>
    /// #66 — gider pusulası belgesinde malın cinsi ayarın ADIYLA yazılır ("Hurda altın
    /// (22 Ayar — 916/1000)"); ham kod (K22) basılmaz; maden adı ve milyem KORUNUR (VUK md.234).
    /// </summary>
    [Fact]
    public async Task GiderPusulasi_belge_cins_ayar_adi_ve_maden()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P3-cins"));
        using var db = vt.Olustur(k);
        await new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance).HurdaAlis(new MSatis.HurdaAlisDto
        {
            CariId = k.CariId, AyarId = "K22", GramAgirlik = 10m,
            BirimFiyatTlHasGram = 2_800m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
            Satici = TestKurulum.HurdaSatici(SaticiTipi.NihaiTuketici),
        }, k.Islem);

        string id;
        using (var kontrol = vt.OlusturSistem())
            id = kontrol.GiderPusulalari.Where(g => g.TenantId == k.TenantId)
                .OrderByDescending(g => g.Olusturuldu).First().Id;

        var servis = new MHurda.HurdaServisi(db, new DenetimServisi(db, new HttpContextAccessor()), new MHurda.HurdaAyarlari());
        var mal = Kok(await servis.Belge(id, k.Islem)).GetProperty("mal");
        var cins = mal.GetProperty("cins").GetString()!;

        Assert.Equal("Hurda altın (22 Ayar — 916/1000)", cins);
        Assert.DoesNotMatch(KesifP3Ortak.HamAyarKodu, cins);
        Assert.Equal("Altın", mal.GetProperty("madenAdi").GetString());
    }
}

public sealed class KesifP3CssTestleri
{
    private static string Kod()
    {
        var css = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "wwwroot", "css", "zo-belge.css"));
        return System.Text.RegularExpressions.Regex.Replace(css, @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
    }

    /// <summary>TOPLU ZO-BELGE: süzgeç yuvası olmayan sayfa başında boş bant yok; Fatura No tek
    /// hizalı; özet ipucu sağdan taşmaz; dört durum tek şerit (iç grup kutu açmaz); satır içi
    /// renk yok (belirteç).</summary>
    [Fact]
    public void KesifP3_css_kurallari()
    {
        var kod = Kod();
        Assert.Contains(".zo-belge .sayfa-basi.sayfa-basi--baslik-ustte:not(:has(> .sayfa-basi-filtre)) > .sayfa-basi-eylem { padding-top: 0; }", kod);
        Assert.Contains(".zo-belge #fk-tablo > tbody > tr > td:first-child { text-align: start; }", kod);
        Assert.Contains(".zo-belge .zo-ozet .zo-ipucu > p { inset-inline-start: auto; inset-inline-end: 0; }", kod);
        Assert.Contains(".zo-belge .zo-grup-ic { display: contents; }", kod);
        // Tasarım hakemi tur 2 (01.10):
        Assert.Contains(".modal-panel:has(#fd-kalem-tablosu) { max-width: min(84rem, calc(100vw - 2rem)); }", kod);
        Assert.Contains("@container zokalem (max-width: 1140px) {\n  #fd-kalem-tablosu.zo-kalem-genis, #fd-kalem-tablosu.zo-kalem-genis > tbody { display: block;", kod);
        Assert.Contains("@container zoyigin (max-width: 1320px) {\n  html[data-mod] .ana-icerik .zo-belge #gp-tablo.veri-tablosu,", kod);
        Assert.Contains("gap: var(--bosluk-2); padding: var(--bosluk-4);", kod);
        Assert.Contains("margin: 0; font-size: .875rem; line-height: 1.45;", kod);
        Assert.Contains(".zo-tanim-izgara {\n  display: grid;", kod);
        Assert.Contains("@container zokalem (max-width: 520px) {\n  #fd-kalem-tablosu.zo-kalem-genis > tbody > tr { grid-template-columns: minmax(0, 1fr); }", kod);
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b", kod);
    }
}
