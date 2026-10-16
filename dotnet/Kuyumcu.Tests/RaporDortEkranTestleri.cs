using System.Globalization;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET RAPOR-4 — ekran testleri (bUnit). Sahte API "{}" varsayılanına bırakılmadı:
// dokunulan uçlara (aylık, mutabakat, katalog, şubeler, yaşlandırma) gerçekçi yanıt.
//   RD4E01  Rapor merkezi grafiği: değer ekseni + Satış/Alış lejantı + seyrek tarih ekseni
//   RD4E02  "Alış-satış nakit farkı" kartı sunucunun NET tanımını basar, formülü alt satırda yazar
//   RD4E03  Gün sonu PRO: durum "N satışın faturası yok", satış toplamı "(iade düşülmeden)"
//   RD4E04  Stok yaşı "dönemden bağımsız" notu başlıkta değil ⓘ içinde
//   RD4E05  CubukGrafik tek seri: çubuk sütun ORTASINDA (Kâr-zarar grafiği hizası)
//   RD4E06  (tur 2) RM grafiği satış çubuğu NET (AYLIK SATIŞ kartıyla aynı), lejant "iade düşüldü" der
//   RD4E07  (tur 2) KZ grafiği: maliyeti eksik satış varken TOPLAM satış çubuğu + Özet ile aynı "Maliyeti bilinen satış"
//   RD4E08  (tur 2) KZ Stok: kart maliyetli adet rozet + notla söylenir; yoksa çizilmez
//   RD4E13  (tur 2) CubukGrafik yatay kip: tam ad tek satır, çubuk + tutar, negatif sola (KZ grafiği)
//   RD4E09  (tur 2) değer ekseni kipinde SVG'de satır içi style yok; ortak akordeon boşluğu #rp-duzeltme'yi kapsar
// ═════════════════════════════════════════════════════════════════════════════
public sealed class RaporDortEkranTestleri : BunitContext
{
    public RaporDortEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    /// <summary>31 günlük ay: satış 1.000 → 31.000 (gün × 1.000), alış her 3 günde 500; 15. gün 10.000 iade → net 5.000.</summary>
    private static string AylikYanit()
    {
        var gunler = string.Join(",", Enumerable.Range(1, 31).Select(g =>
            $"{{\"tarih\":\"2026-08-{g:00}\",\"satisTl\":{g * 1000},\"alisTl\":{(g % 3 == 0 ? 500 : 0)},\"netSatisTl\":{(g == 15 ? 5000 : g * 1000)}}}"));
        return $$"""
            {"yil":2026,"ay":8,
             "satis":{"sayisi":31,"toplamTl":496000.00,"toplamHas":0,"altinToplamHas":0,"madenDagilimi":[]},
             "iade":{"sayisi":1,"toplamTl":22500.00},"netSatisTl":473500.00,
             "alis":{"sayisi":10,"toplamTl":5000.00,"toplamHas":0,"altinToplamHas":0,"madenDagilimi":[]},
             "tahsilat":{"toplamTl":0,"kaynak":"KASA_HAREKETLERI","cariTahsilatiTl":0,"virmanGiris":0,"acilisDevri":0},
             "veresiye":{"toplamTl":0,"kaynak":"VERESIYE_PAYI","kapananTl":0},
             "karTl":null,"karKaynagi":"HESAPLANAMADI","karDurumu":"HESAPLANAMADI","maliyetiEksikSatisAdedi":0,
             "maliyetiEksikIadeAdedi":0,"maliyetiEksikTutarTl":0,
             "satisAlisFarkiTl":468500.00,"hurdaAlisTl":0,"netHasGramHareketi":0,"madenDagilimi":[],"hareketSayisi":42,
             "gunler":[{{gunler}}]}
            """;
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            return Task.FromResult(ParaDemoSahte.Tamam(yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/aylik") => AylikYanit(),
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") => """
                    {"tarih":"2026-08-31",
                     "tahsilat":{"nakit":1000.00,"kart":500.00,"havale":300.00,"diger":0,"toplam":1800.00},
                     "satis":{"sayisi":8,"toplamTl":658028.20,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":8,"faturasizTl":658028.20},
                     "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
                     "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
                     "fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},
                     "fark":658028.20,"mutabik":false,"uyarilar":[],"faturasizIslemler":[],"faturaEslesme":null}
                    """,
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                _ => "{}",
            }));
        }
    }

    [Fact]
    public void RD4E01_Rapor_merkezi_grafigi_deger_ekseni_lejant_ve_seyrek_tarih_ekseni_cizer()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForElement("#rp-sekme-grafik");
        e.Find("#rp-sekme-grafik").Click();
        e.WaitForElement("#rp-grafik-y-ust");
        var kr = KarRaporSozlugu.Tr;
        // Değer ekseni: en yüksek gün 31.000 ₺, taban 0 ₺ — gerçek yazı (SVG dışında).
        Assert.Equal(Bicim.Tl(31_000m), e.Find("#rp-grafik-y-ust").TextContent.Trim());
        Assert.Equal(Bicim.Tl(0m), e.Find("#rp-grafik-y-alt").TextContent.Trim());
        // Lejant: hangi renk satış, hangisi alış.
        var lejant = e.Find("#rp-panel-grafik .grafik-lejant").TextContent;
        Assert.Contains(kr.GrafikSatis, lejant);
        Assert.Contains(kr.GrafikAlis, lejant);
        // Seyrek tarih ekseni: 31 hücre, en çok 7 yazı (≤6 + son), ilk ve son gün yazılı; SVG içinde <text> YOK.
        var hucreler = e.FindAll("#rp-panel-grafik .grafik-eksen--cubuk .grafik-eksen-hucre");
        Assert.Equal(31, hucreler.Count);
        var yazilar = e.FindAll("#rp-panel-grafik .grafik-eksen--cubuk .grafik-eksen-etiket");
        Assert.InRange(yazilar.Count, 2, 7);
        Assert.Equal("0", yazilar[0].GetAttribute("data-indeks"));
        Assert.Equal("30", yazilar[^1].GetAttribute("data-indeks"));
        Assert.Empty(e.FindAll("#rp-panel-grafik svg text"));
    }

    [Fact]
    public void RD4E02_Nakit_farki_karti_sunucunun_net_tanimini_ve_formulunu_basar()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForElement("#rp-fark");
        var kart = e.Find("#rp-fark").TextContent;
        // 473.500 net satış − 5.000 alış − 0 hurda = 468.500 (sunucu alanı; ekran türetmez).
        Assert.Contains(Bicim.TlKurus(468_500m), kart);
        Assert.Contains(KarRaporSozlugu.Tr.NakitFarkiAlt, kart);
        Assert.Contains(KarRaporSozlugu.Tr.NakitFarkiAciklama, kart);
        // Aynı ekrandaki satış kartı NET (473.500) — çıkarma tutar.
        Assert.Contains(Bicim.TlKurus(473_500m), e.Find("#rp-ozet-kartlar").TextContent);
    }

    [Fact]
    public void RD4E03_Gun_sonu_PRO_durumu_somut_satis_toplami_iade_dusulmeden()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForElement("#rp-sekme-gunsonu");
        e.Find("#rp-sekme-gunsonu").Click();
        e.WaitForElement("#mut-kartlar");
        var metin = e.Find("#mut-kartlar").TextContent;
        Assert.Contains(string.Format(DegisimGunSonuSozlugu.Al("tr").FaturasizAdet, 8), metin);
        Assert.DoesNotContain(Toplu5Sozlugu.Tr.GunSonuFarkVar, metin);
        Assert.Contains($"({KarRaporSozlugu.Tr.GunSonuIadeDusulmeden})", metin);
    }

    [Fact]
    public void RD4E04_Stok_yasi_donemden_bagimsiz_notu_baslikta_degil_ipucunda()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForElement("#rp-sekme-yaslandirma");
        e.Find("#rp-sekme-yaslandirma").Click();
        e.WaitForElement("#sy-nasil-ipucu");
        Assert.Single(e.FindAll("#sy-nasil-ipucu #sy-donem-bagimsiz"));
        Assert.Empty(e.FindAll("#stok-yaslandirma-kart .kart-baslik #sy-donem-bagimsiz"));
    }

    [Fact]
    public void RD4E05_Tek_serili_cubuk_sutunun_ortasinda_iki_serili_cizim_degismez()
    {
        var inv = CultureInfo.InvariantCulture;
        var noktalar = new List<CiftNokta> { new("Satış", 1000m, 0m), new("Maliyet", 600m, 0m), new("Kâr", 400m, 0m) };
        var tek = Render<CubukGrafik>(p => p.Add(x => x.Noktalar, noktalar).Add(x => x.Seri1Adi, "Bu dönem"));
        var r = tek.FindAll(".grafik-sar svg rect");
        Assert.Equal(3, r.Count);
        for (var i = 0; i < 3; i++)
        {
            var orta = double.Parse(r[i].GetAttribute("x")!, inv) + double.Parse(r[i].GetAttribute("width")!, inv) / 2;
            Assert.Equal((i + 0.5) * 600.0 / 3, orta, 1);   // viewBox 600 → sütun ortası
        }
        // İkinci seri adı verilince (karşılaştırma) eski iki çubuklu çizim AYNEN.
        var cift = Render<CubukGrafik>(p => p.Add(x => x.Noktalar, noktalar).Add(x => x.Seri1Adi, "Bu dönem").Add(x => x.Seri2Adi, "Önceki"));
        Assert.Equal(6, cift.FindAll(".grafik-sar svg rect").Count);
    }

    [Fact]
    public void RD4E06_RM_grafigi_satis_cubugu_net_ve_lejant_iade_dusuldu()
    {
        var inv = CultureInfo.InvariantCulture;
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForElement("#rp-sekme-grafik");
        e.Find("#rp-sekme-grafik").Click();
        e.WaitForElement("#rp-grafik-y-ust");
        Assert.Contains(KarRaporSozlugu.Tr.GrafikNetSatis, e.Find("#rp-panel-grafik .grafik-lejant").TextContent);
        // 15. gün: brüt 15.000, net 5.000 → satış çubuğu (2 seri → 15. günün 1. dikdörtgeni) NET yükseklikte.
        var r = e.FindAll("#rp-panel-grafik .grafik-sar svg rect");
        Assert.Equal(62, r.Count);
        var cizim = 220.0 - 12 - 22;
        Assert.Equal(5000.0 / 31000.0 * cizim, double.Parse(r[2 * 14].GetAttribute("height")!, inv), 1);
    }

    [Fact]
    public void RD4E13_Yatay_kip_tam_ad_tek_satirda_cubuk_ve_tutar_negatif_sola()
    {
        var inv = CultureInfo.InvariantCulture;
        var noktalar = new List<CiftNokta>
        {
            new("Toplam satış", 1000m, 0m), new("Maliyeti bilinen satış", 600m, 0m), new("Kâr", -200m, 0m),
        };
        var g = Render<CubukGrafik>(p => p.Add(x => x.Noktalar, noktalar).Add(x => x.Yatay, true)
            .Add(x => x.EtiketAltta, true).Add(x => x.DegerBicimi, Bicim.TlKurus).Add(x => x.Seri1Adi, "Bu dönem"));
        var satirlar = g.FindAll(".grafik-yatay .grafik-eksen-oge");
        Assert.Equal(3, satirlar.Count);
        Assert.Equal("Maliyeti bilinen satış", satirlar[1].QuerySelector(".grafik-eksen-ad")!.TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(600m), satirlar[1].QuerySelector(".grafik-eksen-deger")!.TextContent.Trim());
        // Ölçek: aralık −200 … 1000 = 1200 → sıfır x = 16,67; 1000 → genişlik 83,33; −200 sıfırın SOLUNA iner.
        var r = g.FindAll(".grafik-yatay .grafik-sar svg rect");
        Assert.Equal(3, r.Count);
        Assert.Equal(200.0 / 1200 * 100, double.Parse(r[0].GetAttribute("x")!, inv), 1);
        Assert.Equal(1000.0 / 1200 * 100, double.Parse(r[0].GetAttribute("width")!, inv), 1);
        Assert.Equal(0.0, double.Parse(r[2].GetAttribute("x")!, inv), 1);
        Assert.Equal(200.0 / 1200 * 100, double.Parse(r[2].GetAttribute("width")!, inv), 1);
        Assert.Equal(3, g.FindAll(".grafik-yatay svg line").Count);   // her satırda sıfır çizgisi
        Assert.Empty(g.FindAll("svg text"));
        Assert.Null(g.Find(".grafik-yatay .grafik-sar svg").GetAttribute("style"));
        // Dikey kipin alt etiket satırı yatay kipte ikinci kez çizilmez.
        Assert.Single(g.FindAll(".grafik-eksen-alt"));
    }

    [Fact]
    public void RD4E09_Deger_ekseni_kipinde_satir_ici_style_yok_ve_ortak_akordeon_boslugu()
    {
        var noktalar = new List<CiftNokta> { new("01.08", 1000m, 500m), new("02.08", 2000m, 0m) };
        var g = Render<CubukGrafik>(p => p.Add(x => x.Noktalar, noktalar).Add(x => x.DegerEkseni, Bicim.Tl)
            .Add(x => x.Seri1Adi, "Satış").Add(x => x.Seri2Adi, "Alış"));
        var svg = g.Find(".grafik-izgara .grafik-sar svg");
        Assert.Null(svg.GetAttribute("style"));
        Assert.Contains("grafik-svg-sabit", svg.GetAttribute("class") ?? "");
        // #rp-duzeltme #raporlar-pro'nun DIŞINDA kardeştir — ortak kural kardeş seçiciyle onu da kapsar.
        var css = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "wwwroot", "css", "rapor-gorunum.css"));
        Assert.Matches(@"#raporlar-pro\s*~\s*\.katlanir[^{]*\{\s*margin-top:\s*var\(--bosluk-4\)", css);
        Assert.Matches(@"\.grafik-sar\s+svg\.grafik-svg-sabit\s*\{\s*height:\s*220px", css);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// RAPOR-4 tur 2 — Kâr-zarar ekranı (grafik satış kümeleri + Stok sekmesi kart maliyeti).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class RaporDortKarZararEkranTestleri : BunitContext
{
    // Maliyeti eksik satış VAR (KISMI): toplam 10.000, maliyeti bilinen 6.000, maliyet 4.000, kâr 2.000.
    private static string OzetKismi => MadenAyrimiKarZararEkranTestleri.OzetTam
        .Replace("\"maliyetTl\":8000,", "\"maliyetTl\":4000,", StringComparison.Ordinal)
        .Replace("\"karDurumu\":\"TAM\",\"bilinenHasilatTl\":10000,", "\"karDurumu\":\"KISMI\",\"bilinenHasilatTl\":6000,", StringComparison.Ordinal)
        .Replace("\"maliyetiEksikSatisAdedi\":0,\"maliyetiEksikIadeAdedi\":0,\"maliyetiEksikTutarTl\":0,",
                 "\"maliyetiEksikSatisAdedi\":2,\"maliyetiEksikIadeAdedi\":0,\"maliyetiEksikTutarTl\":4000,", StringComparison.Ordinal);

    private string ozet = OzetKismi;
    private string stok = MadenAyrimiKarZararEkranTestleri.StokBilinmiyor;

    public RaporDortKarZararEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => ozet, () => stok)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<RaporServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi(Func<string> ozet, Func<string> stok) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.Contains("kar-zarar/urun") => """{"satirlar":[],"urunSayisi":0,"limit":50,"dahaFazlasiVar":false,"guvenilirlik":"VERI_YOK"}""",
                var y when y.Contains("kar-zarar") => ozet(),
                var y when y.Contains("stok-degerlemesi") => stok(),
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                _ => "{}",
            };
            return Task.FromResult(ParaDemoSahte.Tamam(g));
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.KarZarar> Ciz(bool basit)
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-panel-stok .stat-izgara")));
        return c;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RD4E07_Maliyeti_eksik_satis_varken_grafik_toplam_satisi_ve_Ozet_ile_ayni_kumeyi_gosterir(bool basit)
    {
        var c = Ciz(basit);
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-grafik-cubuk .grafik-eksen-alt")));
        var ogeler = c.FindAll("#kz-grafik-cubuk .grafik-eksen-oge");
        string Ad(int i) => ogeler[i].QuerySelector(".grafik-eksen-ad")!.TextContent.Trim();
        string Deger(int i) => ogeler[i].QuerySelector(".grafik-eksen-deger")!.TextContent.Trim();
        // Toplam satış çubuğu = Özet kartının "Toplam satış" alt satırındaki sayı (Panel/Köprü ile aynı).
        Assert.Equal(KarRaporSozlugu.Tr.GrafikToplamSatis, Ad(0));
        Assert.Equal(Bicim.TlKurus(10000m), Deger(0));
        // Kârın kümesi: Özet kartıyla AYNI etiket ve AYNI sayı — "Satış" adıyla bilinen küme gösterilmez.
        Assert.Equal(KarRaporSozlugu.Tr.BilinenHasilat, Ad(1));
        Assert.Equal(Bicim.TlKurus(6000m), Deger(1));
        var ozetMetni = c.Find("#kz-panel-ozet").TextContent;
        Assert.Contains(KarRaporSozlugu.Tr.BilinenHasilat, ozetMetni);
        Assert.Contains(Bicim.TlKurus(6000m), ozetMetni);
        Assert.DoesNotContain(ogeler, o => o.QuerySelector(".grafik-eksen-ad")!.TextContent.Trim() == KarRaporSozlugu.Tr.GrafikBilinenSatis);
    }

    [Fact]
    public void RD4E07b_Maliyet_tamken_tek_satis_cubugu_kalir()
    {
        ozet = MadenAyrimiKarZararEkranTestleri.OzetTam;
        var c = Ciz(basit: false);
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-grafik-cubuk .grafik-eksen-alt")));
        var adlar = c.FindAll("#kz-grafik-cubuk .grafik-eksen-ad").Select(a => a.TextContent.Trim()).ToList();
        Assert.DoesNotContain(KarRaporSozlugu.Tr.GrafikToplamSatis, adlar);
        Assert.Equal(Services.GetRequiredService<DilServisi>().T.KarZarar.Hasilat, adlar[0]);
    }

    [Fact]
    public void RD4E08_Stok_sekmesi_kart_maliyetli_adedi_rozetle_soyler_yoksa_cizmez()
    {
        var c = Ciz(basit: true);
        Assert.Empty(c.FindAll("#kz-stok-kart-maliyeti"));
        stok = """
            {"subeId":null,"toplamAdet":26,"toplamHasGram":3.206,"altinHasGram":3.206,"madenKirilimi":[],
             "maliyetliAdet":10,"maliyetsizAdet":16,"stokMaliyetiTl":174000,"stokMaliyetiHas":null,"stokMaliyetiKismi":true,
             "kartMaliyetliAdet":8,"kartMaliyetliTl":144000,
             "guvenilirlik":"DUSUK","kaynakDagilimi":[],"konsinyeUrunSayisi":0,"uyari":null}
            """;
        var c2 = Ciz(basit: true);
        var not = c2.Find("#kz-stok-kart-maliyeti").TextContent;
        Assert.Contains(KarRaporSozlugu.Tr.KartMaliyetiRozet, not);
        Assert.Contains(string.Format(KarRaporSozlugu.Tr.StokKartMaliyetiNot, "8"), not);
    }
}
