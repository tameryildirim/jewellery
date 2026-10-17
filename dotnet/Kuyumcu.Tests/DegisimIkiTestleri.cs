using System.Globalization;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET DEGISIM-2 — Değişim (takas) ekranı ve gün sonu kartı GÖRÜNÜM kalanları (tasarım hakemi t2 + hasım t2).
//   (V) Vazgeç sonrası boş formda "Kim getirdi?" hatası YOK; hata ilk dokunuştan sonra, etiketin adıyla.
//   (K) BASİT kimlik tek dil: "Getirenin adı soyadı / TC kimlik no / Adresi"; kilit "Eksik: kim getirdi." — PRO AYNEN.
//   (O) BASİT farkın ödemesi eşik altında da salt okunur "Ödeme türü: Nakit" (Satış'la aynı); kasa TEK biçim (rozet).
//   (R) Fark rengi iki modda tek anlam: boş nötr · açık uyarı · kapalı yeşil.
//   (H) K7: VERDİM arama kutusu etiket satırıyla ALDIM kutularıyla aynı hizada; tekrar açıklama satırları görsel gizli.
//   (G) Gün sonu: BASİT açıklama satırı görsel gizli, "Ayrıntı (N uyarı)"; PRO bağsız fatura TEK ad, aynı tutarın
//       ikinci (yanıltıcı) uyarısı yok, fatura no boşsa "—".
// ⚠ MASAK/VUK kimlik kuralı, 30.000 sınırı ve tutar hesabı DEĞİŞMEDİ — testler yalnız görünümü kilitler.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class DegisimIkiEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol)> istekler = [];
    private static readonly TimeSpan Bekle = TimeSpan.FromSeconds(5);

    public DegisimIkiEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(DegisimIkiEkranTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol));
            return Task.FromResult(ParaDemoSahte.Tamam(yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.EndsWith("/cari") => """
                    [{"id":"c1","ad":"Tuna Akarsu","tur":"MUSTERI","telefon":null,"email":null,"tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"riskSeviye":null,"pepBeyani":false,"sistemCarisi":false,"aktif":true},
                     {"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","telefon":null,"email":null,"tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"riskSeviye":null,"pepBeyani":false,"sistemCarisi":true,"aktif":true}]
                    """,
                var v when v.Contains("/tevsik/gunluk") => """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":1000,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":3.206,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/islemler/altin-bacagi/ayarlar") => """{"stopajOranlari":[{"saticiTipi":"NIHAI_TUKETICI","oran":0,"pusulaGerekir":true}],"alisIskontoOran":null,"saticiAdresiZorunlu":true}""",
                var v when v.Contains("/islemler/takas") => """{"id":"h9","fisId":"f9"}""",
                var v when v.Contains("/reyonlar") || v.Contains("/islemler/son") || v.Contains("/pos/cihazlar") || v.Contains("/islemler/personeller") || v.Contains("/iade-adaylari") => "[]",
                _ => "{}",
            }));
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Sahip", "t@ornek.com", "SAHIP", "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#islem-tur-SATIS");
        e.Find("#islem-tur-TAKAS").Click();
        e.WaitForElement("#takas-satici");
        return e;
    }

    /// <summary>Eski 1 g 24 ayar × has 1.000 = 1.000; yeni fiyat = 1.000 + fark (fark ≥ 0).</summary>
    private static void Doldur(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, decimal fark)
    {
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        SeciciYardim.Sec(e, "islem-cari", "c1");
        SeciciYardim.Sec(e, "takas-eski-ayar", "K24");
        e.Find("#takas-eski-gram").Input("1");
        e.Find("#takas-eski-fiyat").Input(1000m.ToString("0.00", tr));
        SeciciYardim.Sec(e, "takas-yeni-urun", "u1");
        e.Find("#takas-yeni-fiyat").Input((1000m + fark).ToString("0.00", tr));
    }

    private static string? Hata(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string id) =>
        e.FindAll($"#{id}-hata").FirstOrDefault()?.TextContent.Trim() is { Length: > 0 } h ? h : null;

    private static string Etiket(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string id) =>
        e.Find($"label[for='{id}']").TextContent.Replace("*", "").Trim();

    // ═══ (V) Vazgeç ═══════════════════════════════════════════════════════════
    [Fact]
    public void DG2_V_BASIT_Vazgec_sonrasi_bos_formda_kim_getirdi_hatasi_YOK_dokununca_etiket_adiyla()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        Doldur(e, 5000m);
        SeciciYardim.Sec(e, "takas-satici-tip", "NIHAI_TUKETICI");   // Kim getirdi? = Vatandaş (dokunuldu)
        e.WaitForElement("#islem-vazgec", Bekle).Click();
        e.WaitForAssertion(() => Assert.Equal("—", e.Find("#takas-tuval-fark-tutar").TextContent.Trim()), Bekle);
        // Bulgu: sıfırlanan formda "Satıcı tipi zorunludur." kırmızısı kalıyordu
        Assert.Null(Hata(e, "takas-satici-tip"));
        Assert.DoesNotContain(Sozlukler.Tr.Islem.SaticiTipiZorunlu, e.Markup, StringComparison.Ordinal);
        // Yeni fişte alana yeniden dokunulup boş bırakılırsa hata çıkar — alanın ADIYLA
        SeciciYardim.Sec(e, "takas-satici-tip", "NIHAI_TUKETICI");
        SeciciYardim.Sec(e, "takas-satici-tip", "");
        e.WaitForAssertion(() => Assert.Equal(DegisimGunSonuSozlugu.Tr.KimGetirdiSecin, Hata(e, "takas-satici-tip")), Bekle);
    }

    [Fact]
    public void DG2_V_onceki_fiste_dokunulan_alanin_hatasi_yeni_fiste_DIRILMEZ()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        Doldur(e, 5000m);
        SeciciYardim.Sec(e, "takas-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#takas-satici-tckn", Bekle).Input("123");   // 1. fiş: TCKN'ye dokunuldu (hatalı)
        e.WaitForAssertion(() => Assert.NotNull(Hata(e, "takas-satici-tckn")), Bekle);
        e.Find("#islem-vazgec").Click();
        e.WaitForAssertion(() => Assert.Equal("—", e.Find("#takas-tuval-fark-tutar").TextContent.Trim()), Bekle);
        // 2. fiş: yalnız "Kim getirdi?" seçilir — TCKN'ye bu fişte dokunulmadı → kırmızı YOK
        Doldur(e, 5000m);
        SeciciYardim.Sec(e, "takas-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#takas-satici-tckn", Bekle);
        Assert.Null(Hata(e, "takas-satici-tckn"));
        Assert.Null(Hata(e, "takas-satici-tip"));
    }

    // ═══ (K) kimlik tek dil ═══════════════════════════════════════════════════
    [Fact]
    public void DG2_K_BASIT_kimlik_etiketleri_getiren_dili_kilit_kim_getirdi_PRO_AYNEN()
    {
        var d = DegisimGunSonuSozlugu.Tr;
        var e = Ekran(ArayuzModuServisi.Basit);
        Doldur(e, 5000m);
        e.WaitForAssertion(() => Assert.Contains(d.KilitKimGetirdi, e.Find("#islem-kaydet-kilit").TextContent, StringComparison.Ordinal), Bekle);
        Assert.DoesNotContain(ParaDemo2Sozlugu.Tr.AlanSaticiTuru, e.Find("#islem-kaydet-kilit").TextContent, StringComparison.Ordinal);
        SeciciYardim.Sec(e, "takas-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#takas-satici-tckn", Bekle);
        Assert.StartsWith(d.GetireninAdSoyad, Etiket(e, "takas-satici-ad"), StringComparison.Ordinal);   // ardından "Cariden geldi" rozeti
        Assert.Equal(d.TcKimlikNo, Etiket(e, "takas-satici-tckn"));
        Assert.Equal(d.GetireninAdresi, Etiket(e, "takas-satici-adres"));
        Assert.Contains(d.TcKimlikNo, e.Find("#islem-kaydet-kilit").TextContent, StringComparison.Ordinal);
        Assert.Contains("gorsel-gizli", e.Find("#takas-satici-not").ClassName);   // "Eski altını getiren kişinin kimliği yazılır." tekrarı görünmez

        // PRO terimleri AYNEN
        var p = Ekran(ArayuzModuServisi.Profesyonel);
        Doldur(p, 5000m);
        SeciciYardim.Sec(p, "takas-satici-tip", "NIHAI_TUKETICI");
        p.WaitForElement("#takas-satici-tckn", Bekle);
        Assert.Equal(Sozlukler.Tr.Islem.SaticiTckn, Etiket(p, "takas-satici-tckn"));
        Assert.Equal(Satis3Sozlugu.Tr.SaticiAdres, Etiket(p, "takas-satici-adres"));
    }

    // ═══ (O) farkın ödemesi ════════════════════════════════════════════════════
    [Fact]
    public void DG2_O_BASIT_esik_altinda_salt_okunur_Odeme_turu_Nakit_esikte_secici_kasa_TEK_bicim()
    {
        var d = DegisimGunSonuSozlugu.Tr;
        var e = Ekran(ArayuzModuServisi.Basit);
        Doldur(e, 5000m);
        e.WaitForElement("#islem-odeme-sade", Bekle);
        Assert.Contains(Sozlukler.Tr.Islem.OdemeNakit, e.Find("#islem-odeme-sade").TextContent, StringComparison.Ordinal);
        Assert.Equal(d.OdemeTuruSade, e.Find("label[for='islem-odeme-sade']").TextContent.Trim());
        Assert.Empty(e.FindAll("#islem-odeme"));
        Assert.Contains(Sozlukler.Tr.Arayuz.SistemBelirledi, e.Find("#islem-kasa-tek").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain(Sozlukler.Tr.Islem.TekKasaOtomatik, e.Find("#takas-tuval-odeme").TextContent, StringComparison.Ordinal);

        Doldur(e, 40000m);   // had (sunucu 30.000) aşılır → seçici, Kart otomatik; kasa AYNI biçim
        e.WaitForElement("#islem-odeme", Bekle);
        Assert.Equal(d.OdemeTuruSade, Etiket(e, "islem-odeme"));
        Assert.Contains(Sozlukler.Tr.Arayuz.SistemBelirledi, e.Find("#islem-kasa-tek").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain(Sozlukler.Tr.Islem.TekKasaOtomatik, e.Find("#takas-tuval-odeme").TextContent, StringComparison.Ordinal);
        Assert.NotEqual(Sozlukler.Tr.Islem.OdemeTuru, d.OdemeTuruSade);   // cümle düzeni, ortak "Ödeme Türü" değil
    }

    // ═══ (R) fark rengi ═══════════════════════════════════════════════════════
    [Fact]
    public void DG2_R_fark_rengi_iki_modda_TEK_anlam_bos_notr_acik_uyari_kapali_yesil()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        var bant = e.Find("#takas-tuval-fark").ClassList;
        Assert.Contains("dg-fark-bos", bant);
        Assert.DoesNotContain("fis-fark-tamam", bant);   // boş formda "tamam" yeşili yok
        Doldur(e, 5000m);
        e.WaitForAssertion(() => Assert.Contains("dg-fark-acik", e.Find("#takas-tuval-fark").ClassList), Bekle);
        Assert.DoesNotContain("fis-fark-acik", e.Find("#takas-tuval-fark").ClassList);   // kırmızı "hata" rengi yok
        Doldur(e, 0m);
        e.WaitForAssertion(() => Assert.Contains("fis-fark-tamam", e.Find("#takas-tuval-fark").ClassList), Bekle);

        var p = Ekran(ArayuzModuServisi.Profesyonel);
        Doldur(p, 5000m);
        p.WaitForElement(".takas-fark-toplam .sayi", Bekle);
        Assert.Contains("metin-uyari", p.Find(".takas-fark-toplam .sayi").ClassList);
        Assert.DoesNotContain("metin-yesil", p.Find(".takas-fark-toplam .sayi").ClassList);
        Doldur(p, 0m);
        p.WaitForAssertion(() => Assert.Contains("metin-yesil", p.Find(".takas-fark-toplam .sayi").ClassList), Bekle);
    }

    // ═══ (H) hiza + tekrar satırları ═════════════════════════════════════════
    [Fact]
    public void DG2_H_BASIT_VERDIM_arama_etiket_satiri_tekrar_aciklamalar_gorsel_gizli_PRO_etiket_YOK()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        var et = e.Find("#takas-verdim .alan.dg-takas-arama > #takas-verdim-etiket");   // ALDIM alanlarıyla aynı sarmalayıcı (14 px etiket)
        Assert.NotNull(e.Find("#takas-verdim .alan.dg-takas-arama #islem-barkod"));
        Assert.Equal("islem-barkod", et.GetAttribute("for"));
        Assert.Contains("alan-etiket", et.ClassList);
        Assert.Equal(DegisimGunSonuSozlugu.Tr.VerdimAramaEtiketi, et.TextContent.Trim());
        Assert.Contains("gorsel-gizli", e.Find("#takas-verdim-bos").ClassList);
        Assert.Empty(e.FindAll("#takas-tuval-fark-bekle"));   // tekrar satırı yok (ölçer görsel gizliyi de K9 sayıyordu)

        var p = Ekran(ArayuzModuServisi.Profesyonel);
        Assert.Empty(p.FindAll("#takas-verdim-etiket"));
    }
}

public sealed class DegisimIkiGunSonuTestleri : BunitContext
{
    private string mutabakat = "";

    public DegisimIkiGunSonuTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(DegisimIkiGunSonuTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            return Task.FromResult(ParaDemoSahte.Tamam(yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/aylik") => """{"yil":2026,"ay":10,"gunler":[]}""",
                var y when y.EndsWith("/raporlar/gunluk") => """{"tarih":"2026-10-03","satis":{"adet":4,"toplamTl":5000,"toplamHas":1.2},"alis":{"adet":0,"toplamTl":0,"toplamHas":0},"hurda":{"toplamHas":0,"odenenTl":0},"netHasGramHareketi":-1.2,"hareketSayisi":4,"kasalar":[{"id":"k1","ad":"Ana Kasa","bakiye":5000,"paraBirimi":"TRY"}]}""",
                var y when y.EndsWith("/kasalar/nakit-ayrimi") => """[{"kasaId":"k1","paraBirimi":"TRY","toplam":5000,"nakit":5000,"kart":0,"havale":0}]""",
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") => t.mutabakat,
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                var y when y.EndsWith("/pozisyon") => """{"anaMaden":"ALTIN","altin":{"maden":"ALTIN","vitrinGram":10,"hurdaGram":0,"cariAlacakGram":0,"cariBorcGram":0,"netGram":10,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},"digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,"hesaplanmaAni":"2026-10-03T09:00:00Z","donemdenBagimsiz":true}""",
                var y when y.Contains("/bildirimler") => """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""",
                _ => "{}",
            }));
        }
    }

    private const string BagsizSunucu = "1 satış faturası hiçbir satış fişine bağlı değil (toplam 800,00 TL) — elle kesilmiş olabilir";
    private const string FarkSunucu = "Faturalanmamış satış ile fişe bağlanmamış fatura arasında 800,00 TL fark var";
    private const string BaskaUyari = "1 hurda alımının belgesi yok (ne geçerli gider pusulası ne tedarikçi faturası)";

    /// <summary>DegisimGunSonuEkranTestleri.Mutabakat + sunucu uyarıları + (isteğe bağlı) boş fatura no.</summary>
    private static string Yanit(decimal fark, int faturasizSayi, decimal faturasizTl, decimal bagliTl, int bagsizSayi, decimal bagsizTl,
        string[] uyarilar, bool faturaNoBos = false)
    {
        var j = DegisimGunSonuEkranTestleri.Mutabakat(fark: fark, satis: 5000m, faturasizSayi: faturasizSayi, faturasizTl: faturasizTl,
            bagliTl: bagliTl, bagsizSayi: bagsizSayi, bagsizTl: bagsizTl, nakit: 5000m, kart: 0m, havale: 0m, satisFaturasi: bagliTl + bagsizTl);
        j = j.Replace("\"uyarilar\":[]", "\"uyarilar\":" + System.Text.Json.JsonSerializer.Serialize(uyarilar));
        return faturaNoBos ? j.Replace("\"faturaNo\":\"GIB2026000000001\"", "\"faturaNo\":\"\"") : j;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Raporlar> Ciz(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        if (mod == ArayuzModuServisi.Profesyonel)
        {
            e.WaitForElement("#rp-sekme-gunsonu").Click();
            e.WaitForElement("#mut-kartlar");
        }
        else e.WaitForElement("#bugun-gun-sonu .t5-kirilim");
        return e;
    }

    [Fact]
    public void DG2_G_PRO_C_fark_tamamen_bagsizdan_IKINCI_uyari_YOK_bagsiz_TEK_ad_fatura_no_bos_tire()
    {
        // C senaryosu: faturasız satış 0, fark = −bağsız toplam (−800)
        mutabakat = Yanit(fark: -800m, faturasizSayi: 0, faturasizTl: 0m, bagliTl: 5000m, bagsizSayi: 1, bagsizTl: 800m,
            [BagsizSunucu, FarkSunucu, BaskaUyari], faturaNoBos: true);
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        var d = DegisimGunSonuSozlugu.Tr;
        var uyarilar = e.FindAll("#mut-uyarilar > p:not(#mut-uyarilar-baslik)").Select(p => p.TextContent.Replace("⚠", "").Trim()).ToList();
        Assert.Equal([string.Format(d.BagsizUyari, 1, Bicim.TlKurus(800m)), BaskaUyari], uyarilar);   // ikinci (yanıltıcı) uyarı yok, başkası AYNEN
        Assert.DoesNotContain("hiçbir satış fişine", e.Find("#mut-uyarilar").TextContent, StringComparison.Ordinal);
        Assert.Equal(d.BagsizFaturaListe, e.Find("#mut-bagsiz-faturalar h3").TextContent.Trim());
        Assert.Contains(e.FindAll("#mut-ozet-tablo td"), td => td.TextContent.Trim() == d.BagsizFaturaListe);
        Assert.DoesNotContain(Sozlukler.Tr.SayfaDuzeltme.BagsizFaturalar, e.Find("#mut-bagsiz-faturalar").TextContent, StringComparison.Ordinal);
        Assert.Equal("—", e.Find("#mut-bagsiz-faturalar tbody td").TextContent.Trim());
    }

    [Fact]
    public void DG2_G_PRO_fark_KARISIKSA_ikinci_uyari_AYNEN_kalir_uydurma_yok()
    {
        // faturasız 1.300 + bağsız 800 → fark 500: fark bağsızdan ibaret DEĞİL → sunucunun fark uyarısı aynen geçer
        const string farkKarisik = "Faturalanmamış satış ile fişe bağlanmamış fatura arasında 500,00 TL fark var";
        mutabakat = Yanit(fark: 500m, faturasizSayi: 1, faturasizTl: 1300m, bagliTl: 3700m, bagsizSayi: 1, bagsizTl: 800m,
            [BagsizSunucu, farkKarisik]);
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        var metin = e.Find("#mut-uyarilar").TextContent;
        Assert.Contains(farkKarisik, metin, StringComparison.Ordinal);
        Assert.Contains(string.Format(DegisimGunSonuSozlugu.Tr.BagsizUyari, 1, Bicim.TlKurus(800m)), metin, StringComparison.Ordinal);
        Assert.Equal(2, e.FindAll("#mut-uyarilar > p:not(#mut-uyarilar-baslik)").Count);
    }

    [Fact]
    public void DG2_G_BASIT_aciklama_gorsel_gizli_notlar_basligi_uyari_SAYISIYLA_rozet_YOK()
    {
        mutabakat = Yanit(fark: -800m, faturasizSayi: 0, faturasizTl: 0m, bagliTl: 5000m, bagsizSayi: 1, bagsizTl: 800m,
            [BagsizSunucu, FarkSunucu, BaskaUyari]);
        var e = Ciz(ArayuzModuServisi.Basit);
        Assert.Contains("gorsel-gizli", e.Find("#bugun-fark-aciklama").ClassList);
        // süzülmüş liste 2 uyarı → başlık "Ayrıntı (2 uyarı)", çıplak sayaç rozeti yok
        Assert.Contains(string.Format(DegisimGunSonuSozlugu.Tr.NotlarUyariSayili, 2), e.Find("#bugun-gun-sonu-notlar").TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#bugun-gun-sonu-notlar-sayi"));
        e.Find("#bugun-gun-sonu-notlar-ac").Click();
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll("#bugun-gun-sonu-notlar .bugun-gun-sonu-uyari").Count));
    }
}

public sealed class DegisimIkiSafTestleri
{
    [Fact]
    public void DG2_K_bilesen_kilit_metni_BASIT_alan_adiyla_PRO_AYNEN()
    {
        var s = new Kuyumcu.Web.Bilesenler.Fis.HurdaGirisi();   // tür seçilmemiş → eksik: tür
        var t = Sozlukler.Tr.Islem;
        Assert.Equal(string.Format(ParaDemo2Sozlugu.Tr.TakasEksikBasit, DegisimGunSonuSozlugu.Tr.KilitKimGetirdi),
            Kuyumcu.Web.Bilesenler.Fis.FisTakasSatici.EksikMetni(s, "tr", basit: true, t));
        Assert.Equal(string.Format(ParaDemo2Sozlugu.Tr.TakasEksikPro, ParaDemo2Sozlugu.Tr.AlanSaticiTuru),
            Kuyumcu.Web.Bilesenler.Fis.FisTakasSatici.EksikMetni(s, "tr", basit: false, t));
    }
}

public sealed class DegisimIkiCssTestleri
{
    private static string Govde()
    {
        var css = File.ReadAllText(Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web", "wwwroot", "css", "degisim-gunsonu.css"));
        return System.Text.RegularExpressions.Regex.Replace(css, @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
    }

    [Fact]
    public void DG2_C_fark_renkleri_belirtecle_kartlar_tek_satir_bagsiz_tablo_dolgusu()
    {
        var g = Govde();
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgb\(|hsl\(", g);
        Assert.Matches(@"\.dg-fark-acik \{ background: var\(--uyari-yuzey\); border-top-color: var\(--uyari-kenar\); \}", g);
        Assert.Matches(@"\.dg-fark-bos \.fis-tuval-tutar \{ color: var\(--metin-soluk\); \}", g);
        Assert.Matches(@"#mut-bagsiz-faturalar \.veri-tablosu :is\(th, td\):first-child \{ padding-left: 1\.5rem; \}", g);
        Assert.Matches(@"#mut-kartlar \{ grid-template-columns: repeat\(auto-fit, minmax\(min\(100%, 9\.5rem\), 1fr\)\); \}", g);
        Assert.Matches(@"#mut-kartlar \.stat-etiket \{ line-height: 1\.35; min-height: 2\.7em; \}", g);
        // K7: VERDİM arama sarmalayıcısının satır aralığı = ALDIM ızgarasının satır aralığı (ölçüldü: 278 = 278)
        Assert.Matches(@"\.dg-takas-arama \{ display: grid; row-gap: 10px;", g);
        Assert.Matches(@"\.dg-takas-eski,\s*\.islem-sayfa #takas-tuval \.dg-takas-yeni \{ grid-template-columns: repeat\(2, minmax\(0, 1fr\)\); gap: 10px 12px; \}", g);
    }
}
