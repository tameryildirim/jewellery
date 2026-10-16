using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KAT-1 — KURUCU ŞİKÂYET KATALOĞU [saf + bUnit]
//
//  K5  TRY → ₺ (tek merkez Bicim.ParaIsareti; USD/EUR/GBP kodu aynen)
//  K8  basit mod jargonu (D10 katmanı BasitSozluk; PRO sözlük değişmez)
//  UX-S31 işlem defteri ham tür/ödeme kodu ekrana basılmaz
//
// Mutasyonlar (rapor § mutasyon):
//  MKAT-1 ParaIsareti TRY dalı kodu döndürür      → KAT_K5_* KIRMIZI
//  MKAT-2 basit Cari_Baslik = pro metni ("Cariler") → KAT_K8_* KIRMIZI
//  MKAT-3 HareketTuru SATIS_IADE → ham kod          → KAT_HAM_* KIRMIZI
// ⚠ Görünürlük/sığma kanıtı bUnit ile VERİLMEZ — gerçek tarayıcı taraması raporda.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KatalogK1Testleri : BunitContext
{
    /// <summary>sikisma-olc.js K8 ile AYNI kalıp (JS \b ASCII'dir: "Mutabakatı" da yakalanır).</summary>
    private static readonly Regex Jargon = new(
        @"(?<![A-Za-z0-9_])(Cari|Cariler|Cariden|Cariye|Carinin|Storno|Vezne|Mutabakat|Pasife al|Denetim izi)(?![A-Za-z0-9_])");

    /// <summary>Ham enum kalıbı (katalog kapsamı madde 3).</summary>
    private static readonly Regex HamEnum = new(@"\b[A-Z]{2,}_[A-Z_]{2,}\b");

    public KatalogK1Testleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    // ── K5 ───────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("TRY", "₺")]
    [InlineData(null, "₺")]
    [InlineData("", "₺")]
    [InlineData("USD", "USD")]
    [InlineData("EUR", "EUR")]
    [InlineData("GBP", "GBP")]
    public void KAT_K5_01_ParaIsareti_TRY_yi_lira_isaretine_cevirir_dovizi_korur(string? kod, string beklenen)
        => Assert.Equal(beklenen, Bicim.ParaIsareti(kod));

    [Fact]
    public void KAT_K5_02_Para_alani_son_eki_TRY_yazmaz()
    {
        var c = Render<Para>(p => p
            .Add(x => x.ElemanId, "t-tutar")
            .Add(x => x.Etiket, "Tutar")
            .Add(x => x.ParaBirimi, "TRY"));
        Assert.Equal("₺", c.Find("#t-tutar-birim").TextContent.Trim());
        Assert.DoesNotContain("TRY", c.Markup);

        var bos = Render<Para>(p => p.Add(x => x.ElemanId, "t-bos").Add(x => x.Etiket, "Tutar"));
        Assert.Equal("₺", bos.Find("#t-bos-birim").TextContent.Trim());

        var usd = Render<Para>(p => p
            .Add(x => x.ElemanId, "t-usd").Add(x => x.Etiket, "Tutar").Add(x => x.ParaBirimi, "USD"));
        Assert.Equal("USD", usd.Find("#t-usd-birim").TextContent.Trim());
    }

    [Fact]
    public void KAT_K5_03_Para_secicisinde_TRY_okunur_ad_deger_kod_kalir()
    {
        Assert.Equal("₺ Türk lirası", KatalogSozlugu.ParaSecenegi("TRY", "tr"));
        Assert.Equal("₺ Turkish lira", KatalogSozlugu.ParaSecenegi("TRY", "en"));
        Assert.Equal("USD", KatalogSozlugu.ParaSecenegi("USD", "tr"));
        Assert.DoesNotContain("TRY", KatalogSozlugu.ParaSecenegi(null, "tr"));
    }

    // ── K8 ───────────────────────────────────────────────────────────────────

    [Fact]
    public void KAT_K8_01_Basit_katman_Cari_yerine_Musteri_PRO_degismez()
    {
        Assert.Equal("Müşteriler", BasitSozluk.Getir("tr").Cari.Baslik);
        Assert.Equal("Müşteriler", BasitSozluk.Getir("tr").Nav.Cariler);
        Assert.Equal("Kasa kuyruğu", BasitSozluk.Getir("tr").Nav.Vezne);
        Assert.Equal("Müşteri", BasitSozluk.Getir("tr").SayfaDuzeltme.VarlikAdi("Cari"));
        Assert.Equal("Cari", Sozlukler.Getir("tr").SayfaDuzeltme.VarlikAdi("Cari"));
        Assert.Equal("Kullanımdan kaldır", BasitSozluk.Getir("tr").Fiyat.PasifeAl);
        Assert.Equal("Geri alınan", BasitSozluk.Getir("tr").RaporSeti.Storno);
        // PRO sözlük nesnesine DOKUNULMADI (kopya üzerinde çalışıldı) — D10: pro "Cari" kalır.
        Assert.Equal("Cariler", Sozlukler.Getir("tr").Cari.Baslik);
        Assert.Equal("Cariler", Sozlukler.Getir("tr").Nav.Cariler);
        Assert.Equal("Storno", Sozlukler.Getir("tr").RaporSeti.Storno);
        Assert.NotSame(Sozlukler.Getir("tr"), BasitSozluk.Getir("tr"));
        Assert.NotSame(Sozlukler.Getir("tr").Cari, BasitSozluk.Getir("tr").Cari);
        // Katmanın dokunmadığı alt sözlük PAYLAŞILIR (gereksiz kopya yok) ve aynı metni taşır.
        Assert.Equal(Sozlukler.Getir("tr").Ortak.Kaydet, BasitSozluk.Getir("tr").Ortak.Kaydet);
    }

    [Fact]
    public void KAT_K8_02_Basit_katmanin_her_TR_metni_jargonsuz_ve_prodan_farkli()
    {
        var tr = BasitDilSozlugu.Tr;
        var degerler = typeof(BasitDilMetinleri).GetProperties()
            .Where(p => p.PropertyType == typeof(string))
            .Select(p => (Ad: p.Name, Deger: (string)p.GetValue(tr)!))
            .ToList();
        Assert.True(degerler.Count >= 50, $"beklenen ≥50 anahtar, gelen {degerler.Count}");
        foreach (var (ad, deger) in degerler)
            Assert.DoesNotMatch(Jargon, deger);
        Assert.Equal(degerler.Count, BasitSozluk.AnahtarSayisi);

        // Her eşlenen anahtarın PRO değeri jargon taşır (liste boşuna büyümesin) ve basit değer farklıdır.
        var pro = Sozlukler.Getir("tr");
        var basit = BasitSozluk.Getir("tr");
        foreach (var p in typeof(BasitDilMetinleri).GetProperties())
        {
            if (p.Name.Contains("__"))
            {
                // Sözlük girdisi: "Yol_Parca__Anahtar"
                var parca = p.Name.Split("__");
                object? pd = pro, bd = basit;
                foreach (var y in parca[0].Split('_'))
                {
                    pd = pd!.GetType().GetProperty(y)!.GetValue(pd);
                    bd = bd!.GetType().GetProperty(y)!.GetValue(bd);
                }
                var proDeger = ((Dictionary<string, string>)pd!)[parca[1]];
                var basitDeger = ((Dictionary<string, string>)bd!)[parca[1]];
                Assert.Matches(Jargon, proDeger);
                Assert.Equal(p.GetValue(tr), basitDeger);
                Assert.NotSame(pd, bd);   // PRO sözlüğü kopyalandı, değişmedi
                continue;
            }
            var yol = p.Name.Split('_');
            object? po = pro, bo = basit;
            foreach (var parca in yol)
            {
                po = po!.GetType().GetProperty(parca)!.GetValue(po);
                bo = bo!.GetType().GetProperty(parca)!.GetValue(bo);
            }
            Assert.True(Jargon.IsMatch((string)po!), $"{p.Name} PRO metni jargon taşımıyor — gereksiz eşleme");
            Assert.NotEqual(po, bo);
            Assert.Equal(p.GetValue(tr), bo);
        }
    }

    [Fact]
    public void KAT_K8_03_DilServisi_basit_modda_katmani_proda_hub_sozlugunu_verir()
    {
        var dil = Services.GetRequiredService<DilServisi>();
        // Bağ YOKKEN davranış eskisi (bUnit sınıflarının çoğu böyle kurar).
        Assert.False(dil.BasitMi);
        Assert.Equal("Cariler", dil.T.Cari.Baslik);

        var basit = true;
        var olay = 0;
        dil.Degisti += () => olay++;
        dil.BasitModuBagla(() => basit);
        Assert.True(dil.BasitMi);
        Assert.Equal("Müşteriler", dil.T.Cari.Baslik);
        Assert.Equal("Kim ne yaptı kaydı yalnız eklenir: kayıt asla silinmez ya da değiştirilmez.", dil.T.Denetim.DegistirilemezNot);

        dil.ModuDenetle();            // mod aynı → sessiz
        Assert.Equal(0, olay);
        basit = false;
        dil.ModuDenetle();            // mod değişti → ekranlar yeniden çizilsin
        Assert.Equal(1, olay);
        Assert.Equal("Cariler", dil.T.Cari.Baslik);
    }

    // ── UX-S31: işlem defteri ham kodları ────────────────────────────────────

    [Theory]
    [InlineData("SATIS_IADE", "Satış iadesi")]
    [InlineData("EMANET_GIRIS", "Emanet girişi")]
    [InlineData("HURDA_ALIS", "Hurda alışı")]
    [InlineData("EVRAK_GIRIS", "Çek/senet girişi")]
    [InlineData("CARI_DUZELTME", "Cari düzeltme")]
    public void KAT_HAM_01_Hareket_turu_sozluk_adiyla(string kod, string ad)
    {
        Assert.Equal(ad, KatalogSozlugu.HareketTuru(kod, "tr"));
        Assert.DoesNotMatch(HamEnum, KatalogSozlugu.HareketTuru(kod, "tr"));
    }

    [Fact]
    public void KAT_HAM_02_Basit_modda_duzeltme_turu_jargonsuz_odeme_araci_okunur()
    {
        Assert.Equal("Bakiye düzeltme", KatalogSozlugu.HareketTuru("CARI_DUZELTME", "tr", basit: true));
        Assert.Equal("Nakit", KatalogSozlugu.OdemeAraci("NAKIT", "tr"));
        Assert.Equal("Karışık", KatalogSozlugu.OdemeAraci("KARMA", "tr"));
        Assert.Equal("—", KatalogSozlugu.OdemeAraci(null, "tr"));
    }
}

/// <summary>KAT-1 — işlem defteri ekranı: ham tür/ödeme kodu ve basit mod jargonu [bUnit].</summary>
public sealed class KatalogIslemDefteriTestleri : BunitContext
{
    private const string Defter = """
        {"baslangic":"2026-09-23","bitis":"2026-09-23","toplamSatir":3,"ofset":0,"limit":100,
         "dahaFazlasiVar":false,
         "ozet":{"tlGiris":5000.00,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":3},
         "storno":{"adet":1,"tutarTl":100},
         "satirlar":[
           {"id":"h1","tarih":"2026-09-23T09:00:00Z","tur":"SATIS_IADE","yon":"CIKIS","yonTl":"CIKIS","yonHas":null,
            "tlTutari":-100.00,"hasGrami":0,"maden":"ALTIN","paraBirimi":"TRY","odemeTuru":"NAKIT",
            "nakitTutar":100.00,"veresiyeTutar":0,"fisId":null,"faturaNo":null,"cariAd":"Ayşe Demir",
            "urunAd":"Bilezik","subeAd":"Merkez","kullaniciAd":"Test","stornoSatiri":true,"stornolandi":false},
           {"id":"h2","tarih":"2026-09-23T09:30:00Z","tur":"EMANET_GIRIS","yon":"GIRIS","yonTl":null,"yonHas":"GIRIS",
            "tlTutari":0,"hasGrami":5.4,"maden":"ALTIN","paraBirimi":"TRY","odemeTuru":"KARMA",
            "nakitTutar":null,"veresiyeTutar":null,"fisId":null,"faturaNo":null,"cariAd":null,
            "urunAd":null,"subeAd":"Merkez","kullaniciAd":"Test","stornoSatiri":false,"stornolandi":false},
           {"id":"h3","tarih":"2026-09-23T10:00:00Z","tur":"CARI_DUZELTME","yon":"GIRIS","yonTl":"GIRIS","yonHas":null,
            "tlTutari":50,"hasGrami":0,"maden":null,"paraBirimi":"TRY","odemeTuru":null,
            "nakitTutar":null,"veresiyeTutar":null,"fisId":null,"faturaNo":null,"cariAd":null,
            "urunAd":null,"subeAd":"Merkez","kullaniciAd":"Test","stornoSatiri":false,"stornolandi":false}]}
        """;

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/islem-defteri", StringComparison.Ordinal) => Defter,
                var y when y.EndsWith("/subeler", StringComparison.Ordinal) => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/kullanicilar", StringComparison.Ordinal) => """[{"id":"u1","ad":"Test","soyad":"Kullanici"}]""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    public KatalogIslemDefteriTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.IslemDefteri> Ac(bool basit)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        if (basit) Services.GetRequiredService<DilServisi>().BasitModuBagla(() => true);
        var e = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        e.WaitForElement("#id-tur-h3");
        return e;
    }

    [Fact]
    public void KAT_HAM_03_Islem_defteri_tur_ve_odeme_ham_kod_basmaz()
    {
        var e = Ac(basit: false);
        Assert.Equal("Satış iadesi", e.Find("#id-tur-h1").TextContent.Trim());
        Assert.Equal("Emanet girişi", e.Find("#id-tur-h2").TextContent.Trim());
        Assert.Equal("Cari düzeltme", e.Find("#id-tur-h3").TextContent.Trim());
        var tablo = e.Find("table").TextContent;
        Assert.DoesNotMatch(new Regex(@"\b[A-Z]{2,}_[A-Z_]{2,}\b"), tablo);
        Assert.DoesNotContain("NAKIT", tablo);
        Assert.DoesNotContain("KARMA", tablo);
        Assert.Contains("Nakit", tablo);
        Assert.Contains("Karışık", tablo);
        // Tür seçicisinin seçenekleri de okunur ad taşır (ham "SATIS_IADE" değil).
        Assert.DoesNotContain(">SATIS_IADE<", e.Markup);
    }

    [Fact]
    public void KAT_K8_04_Islem_defteri_basit_modda_Storno_ve_Cari_yok_proda_kalir()
    {
        var jargon = new Regex(@"(?<![A-Za-z0-9_])(Cari|Cariler|Storno)(?![A-Za-z0-9_])");
        var basit = Ac(basit: true);
        Assert.DoesNotMatch(jargon, basit.Markup);
        Assert.Equal("Bakiye düzeltme", basit.Find("#id-tur-h3").TextContent.Trim());
        Assert.Contains("Geri alınan", basit.Markup);
        Assert.Contains("Müşteri", basit.Markup);
    }

    [Fact]
    public void KAT_K8_05_Islem_defteri_pro_modda_D10_Cari_ve_Storno_kalir()
    {
        var pro = Ac(basit: false);
        Assert.Contains(">Storno<", pro.Markup.Replace(" ", ""));
        Assert.Contains("Cari", pro.Markup);
    }
}

/// <summary>KAT-1 — /raporlar profesyonel yüz sekmeleri (K4 tren) [saf].</summary>
public sealed class KatalogRaporSekmeTestleri
{
    [Theory]
    [InlineData("R05", "/raporlar", "/raporlar?sekme=gunsonu")]
    [InlineData("R06", "/raporlar", "/raporlar?sekme=stok")]
    [InlineData("R20", "/raporlar", "/raporlar?sekme=stok")]
    [InlineData("R15", "/raporlar", "/raporlar?sekme=yaslandirma")]
    [InlineData("R03", "/raporlar", "/raporlar?sekme=cari")]
    [InlineData("R01", "/raporlar", "/raporlar")]
    [InlineData("R10", "/raporlar/islem-defteri", "/raporlar/islem-defteri")]
    public void KAT_K4_01_Rapor_merkezi_karti_ilgili_sekmeyi_acar(string kod, string rota, string beklenen)
        => Assert.Equal(beklenen, Kuyumcu.Web.Pages.Raporlar.KartRotasi(kod, rota));
}

/// <summary>KAT-1 — /islemler son satışlar kartı: ilk 5 + sınır yazısı + hepsini göster (K4 · UX-S29) [bUnit].</summary>
public sealed class KatalogSonSatisTestleri : BunitContext
{
    public KatalogSonSatisTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        GocC2Ortak.IzgaraServisleri(Services);   // GOC-C-2: tuval tabloları ortak ızgarada (ApiIstemci ister)
    }

    private static List<Kuyumcu.Contracts.Sayfalar.Islemler.IadeAdayi> Satis(int n) =>
        [.. Enumerable.Range(1, n).Select(i => new Kuyumcu.Contracts.Sayfalar.Islemler.IadeAdayi(
            $"s{i}", new DateTime(2026, 9, 24, 10, i, 0, DateTimeKind.Utc), 1000m * i, "u1", "Bilezik", 1m, 0m, 1m, null))];

    [Fact]
    public void KAT_K4_02_Son_satislar_ilk_bes_fis_sinir_yazar_hepsi_acilir()
    {
        var c = Render<Kuyumcu.Web.Bilesenler.Fis.FisSonSatislar>(p => p
            .Add(x => x.Satislar, Satis(7)).Add(x => x.Yuklendi, true)
            .Add(x => x.Kirilim, new Kuyumcu.Web.Bilesenler.Fis.OdemeKirilimi()));
        Assert.Empty(c.FindAll("[id^=islem-iade-et-]"));   // SATIS-2: kart KATLI gelir
        c.Find("#islem-son-satislar-ac").Click();
        Assert.Equal(5, c.FindAll("[id^=islem-iade-et-]").Count);
        Assert.Equal("5 / 7 fiş gösteriliyor", c.Find("#islem-son-sinir-metin").TextContent.Trim());
        Assert.Equal("Hepsini göster (7)", c.Find("#islem-son-hepsi").TextContent.Trim());

        c.Find("#islem-son-hepsi").Click();
        Assert.Equal(7, c.FindAll("[id^=islem-iade-et-]").Count);
        Assert.Equal("İlk 5 fişi göster", c.Find("#islem-son-hepsi").TextContent.Trim());
        Assert.Equal("/raporlar/islem-defteri", c.Find("#islem-son-defter").GetAttribute("href"));
    }

    [Fact]
    public void KAT_K4_03_Bes_ve_alti_fiste_sinir_satiri_yok()
    {
        var c = Render<Kuyumcu.Web.Bilesenler.Fis.FisSonSatislar>(p => p
            .Add(x => x.Satislar, Satis(5)).Add(x => x.Yuklendi, true)
            .Add(x => x.Kirilim, new Kuyumcu.Web.Bilesenler.Fis.OdemeKirilimi()));
        c.Find("#islem-son-satislar-ac").Click();   // SATIS-2: kart KATLI gelir
        Assert.Equal(5, c.FindAll("[id^=islem-iade-et-]").Count);
        Assert.Empty(c.FindAll("#islem-son-hepsi"));
    }
}
