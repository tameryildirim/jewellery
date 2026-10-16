using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-S2 — SATIŞ PROFESYONEL MOD = TUVAL (Profesyonel.dc.html)
//
// Sınanan:
//   • sekmeler TUVAL SIRASIYLA (Satış · Alış · Hurda alış · Tahsilat · Ödeme · Takas ·
//     Satış iadesi · Alış iadesi); Tahsilat → Müşteriler, Ödeme kilitli + sebep;
//     İşçilik/Transfer "Diğer ▾" menüsünde (id'ler aynı), Diğer düğmesinde özet
//   • alan satırı Müşteri(borç rozeti) · Tarih · Para · Satıcı · Fiyat listesi (sunucu
//     çözümünün aynası: müşteri listesi, yoksa varsayılan; okunamazsa sebep)
//   • geniş kalem tablosu Barkod · Ürün · Ayar · Milyem · Gram · Has · Adet · İşçilik · Tutar
//     + ✎ (birim fiyat · iskonto · hediye · sil düzenleme satırında); işçilik yalnız teklifle
//   • alt toplam: Toplam has · İşçilik · KDV "Faturada" (istemcide KDV hesabı YOK) · Genel toplam
//   • en alt Karma/Tek hapı + özet + Vazgeç + Kaydet ve yazdır
//   • sağ sütun: Yasal kapılar EŞİKLERİ SUNUCUDAN (sunucu 500 → sayısız "Bilinmiyor"),
//     Müşteri kartı (/cari/{id}/bakiye son işlem; Peşin'de istek yok), Kısayollar F2/F4/F8/F9
//     (gerçek tuşlar), not
//   • BASİT yüz etkilenmez; basit modda /fiyat/listeler istenmez
//
// ⚠ Sabit para metni YOK: beklenen tutarlar Bicim.Tl(..) ile üretilir (T-K1).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TS2TuvalProTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private decimal? tevsikHad = 50_000m;          // null → /tevsik/gunluk 500
    private bool tevsikDevrede;
    private decimal? tevsikKalan;                  // null → kalan = had
    private bool listeHata;                        // true → /fiyat/listeler 500
    private bool teklifSabit;                      // true → /fiyat/teklif SABİT FİYATLI (sunucu ayrıştırmaz: has/marj/işçilik 0)
    private string urunHas = "3.94", urunGram = "4.3";
    private bool teklifKampanya;                   // T-S2b: /fiyat/teklif KAMPANYA İNDİRİMLİ (liste 10000, işçilik 500 indirim ÖNCESİ, satış 9000)
    private string urunMilyem = "916";             // T-S2b: milyem kayıtlı değilse (0) hücre "—"   // doğrulayıcı bulgusu: küçük has/gram ekranda kaybolmaz

    public TS2TuvalProTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(TS2TuvalProTestleri t) : HttpMessageHandler
    {
        private static HttpResponseMessage Hata() => new(System.Net.HttpStatusCode.InternalServerError)
        { Content = new StringContent("""{"message":"sunucu hatası"}""", System.Text.Encoding.UTF8, "application/json") };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            if (yol.Contains("/tevsik/gunluk") && t.tevsikHad is null) return Hata();
            if (yol.Contains("/fiyat/listeler") && t.listeHata) return Hata();
            var h = t.tevsikHad?.ToString(CultureInfo.InvariantCulture);
            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") =>
                    """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/cari/c1/bakiye") => """{"cariId":"c1","tlBakiye":38900,"hasGramBakiye":5.66,"sonIslemTarihi":"2026-09-04T10:00:00"}""",
                var v when v.Contains("/tevsik/gunluk") =>
                    $$"""{"had":{{h}},"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":{{(t.tevsikKalan ?? t.tevsikHad)?.ToString(CultureInfo.InvariantCulture)}},"islemTutariKuraliDevrede":{{(t.tevsikDevrede ? "true" : "false")}}}""",
                var v when v.Contains("/fiyat/listeler") =>
                    """[{"id":"fl1","ad":"Vitrin","varsayilan":true,"aktif":true},{"id":"fl2","ad":"Toptan","varsayilan":false,"aktif":true},{"id":"fl3","ad":"Eski","varsayilan":false,"aktif":false}]""",
                var v when v.Contains("/fiyat/teklif") && t.teklifKampanya => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":1000,"satisFiyati":9000,"kampanyaId":"kmp1","sabitFiyat":false,"kaynak":"LISTE","indirimOncesiFiyat":10000}""",
                var v when v.Contains("/fiyat/teklif") && t.teklifSabit => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":0,"marjTl":0,"iscilikTl":0,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":true,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => $$"""[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":{{t.urunHas}},"iscilikTl":0,"satisFiyati":10000,"gramAgirlik":{{t.urunGram}},"milyem":{{t.urunMilyem}},"barkod":"BLZ-00412","kod":"K-1"}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Hasan Kaya","tlBakiye":38900,"hasGramBakiye":5.66,"fiyatListesiId":"fl2"},{"id":"c2","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":200000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":200000,"ekBelgeEsigi":200000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/islemler/personeller") => """{"secimSerbest":false,"oturumId":"u1","personeller":[{"id":"u1","ad":"Test","soyad":"Tezgâhtar"}],"dahaFazlasiVar":false}""",
                var v when v.Contains("/islemler/satis") => """{"id":"01J00000000000000000000001","fisId":"01J00000000000000000000002","tur":"SATIS","tlTutari":10000}""",
                var v when v.Contains("/reyon") => "[]",
                var v when v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod = ArayuzModuServisi.Profesyonel)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-tur-SATIS");
        return e;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> KalemliPro(string beklenenFiyat = "10000")
    {
        var e = Ekran();
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.Equal(beklenenFiyat, SeciciYardim.SeciliDeger(e, "islem-birimfiyat")));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForElement("#islem-kalem-pro");
        return e;
    }

    private int SatisPostSayisi() { lock (istekler) return istekler.Count(i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")); }
    private bool Istendi(string parca) { lock (istekler) return istekler.Any(i => i.Yol.Contains(parca)); }

    private static string CozumKoku()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        return d!.FullName;
    }

    // ═══ SAF ════════════════════════════════════════════════════════════════

    [Fact]
    public void Sozluk_TR_EN_bos_anahtar_yok_esik_SAYISI_ve_TRY_yok()
    {
        foreach (var m in new[] { SatisProSozlugu.Tr, SatisProSozlugu.En })
            foreach (var p in typeof(SatisProMetinleri).GetProperties())
            {
                var v = (string)p.GetValue(m)!;
                Assert.False(string.IsNullOrWhiteSpace(v), p.Name);
                Assert.DoesNotMatch(@"\d{2,}", v.Replace("{0}", ""));   // eşik rakamı sözlükte YOK (sunucudan gelir)
                Assert.DoesNotContain("TRY", v);
            }
        Assert.Contains("{0}", SatisProSozlugu.Tr.NakitKurali);
        Assert.Contains("{0}", SatisProSozlugu.Tr.KimlikEsigi);
        Assert.Same(SatisProSozlugu.En, SatisProSozlugu.Al("en"));
        Assert.Same(SatisProSozlugu.Tr, SatisProSozlugu.Al("tr"));
    }

    [Fact]
    public void Pro_sekmeler_TUVAL_SIRASIYLA_ve_her_tur_bir_yerde()
    {
        Assert.Equal(["SATIS", "ALIS", "HURDA_ALIS", "TAHSILAT", "TAKAS", "SATIS_IADE", "ALIS_IADE"], FisTurleri.ProSekmeler);   // SATIS-3 t11: ODEME (ucu yok) kaldırıldı
        Assert.Equal(["ISCILIK", "TRANSFER"], FisTurleri.ProDigerTurleri);
        foreach (var (deger, _) in FisTurleri.Tumu(Sozlukler.Tr.Islem))
            Assert.True(FisTurleri.ProSekmeler.Contains(deger) ^ FisTurleri.ProDigerTurleri.Contains(deger), deger);
    }

    [Fact]
    public void Fiyat_listesi_cozumu_sunucunun_AYNASI_musteri_listesi_yoksa_varsayilan()
    {
        var l = new List<(string, string, bool)> { ("fl1", "Vitrin", true), ("fl2", "Toptan", false) };
        Assert.Equal(("Toptan", true), Kuyumcu.Web.Pages.Islemler.FiyatListesiCoz(l, "fl2"));
        Assert.Equal(("Vitrin", false), Kuyumcu.Web.Pages.Islemler.FiyatListesiCoz(l, "fl-pasif"));   // atanan aktif değil → varsayılan
        Assert.Equal(("Vitrin", false), Kuyumcu.Web.Pages.Islemler.FiyatListesiCoz(l, null));
        Assert.Equal(((string?)null, false), Kuyumcu.Web.Pages.Islemler.FiyatListesiCoz([("fl2", "Toptan", false)], null));
        Assert.Equal(((string?)null, false), Kuyumcu.Web.Pages.Islemler.FiyatListesiCoz(null, "fl2"));
    }

    [Fact]
    public void Kisayol_hedefleri_F2_urun_F4_musteri_F8_odeme_F9_odak_degil()
    {
        Assert.Equal("islem-barkod", Kuyumcu.Web.Pages.Islemler.KisayolHedefi("F2", false));
        Assert.Equal("islem-cari", Kuyumcu.Web.Pages.Islemler.KisayolHedefi("F4", false));
        Assert.Equal("islem-odeme", Kuyumcu.Web.Pages.Islemler.KisayolHedefi("F8", false));
        Assert.Equal("islem-pro-tek", Kuyumcu.Web.Pages.Islemler.KisayolHedefi("F8", true));   // SATIS-2: karmada "Tek ödemeye dön"
        Assert.Null(Kuyumcu.Web.Pages.Islemler.KisayolHedefi("F9", false));
        Assert.Null(Kuyumcu.Web.Pages.Islemler.KisayolHedefi("F5", false));
    }

    [Fact]
    public void Iscilik_yalniz_teklif_fiyatiyla_hediye_sifir_bilinmeyen_EKSIK_isaretli()
    {
        var teklif = new Teklif(true, null, 10000m, 9500m, 0m, 500m, 0m, 10000m, null, false, null);
        Assert.Equal(500m, FisProKalemKarti.TeklifIscilik(teklif, 10000m));
        Assert.Null(FisProKalemKarti.TeklifIscilik(teklif, 9000m));                       // fiyat elle değişti
        Assert.Null(FisProKalemKarti.TeklifIscilik(teklif with { TeklifVar = false }, 10000m));
        Assert.Null(FisProKalemKarti.TeklifIscilik(null, 10000m));
        Assert.Null(FisProKalemKarti.TeklifIscilik(teklif with { SabitFiyat = true, IscilikTl = 0m }, 10000m));   // sabit fiyat: ayrıştırılmadı

        var a = new FisKalemi { UrunId = "a", UrunAdi = "A", Adet = 2m, BirimFiyat = 100m, IscilikTl = 25m };
        var b = new FisKalemi { UrunId = "b", UrunAdi = "B", Adet = 1m, BirimFiyat = 0m, IscilikTl = 40m, Hediye = true };
        var c = new FisKalemi { UrunId = "c", UrunAdi = "C", Adet = 1m, BirimFiyat = 50m };
        Assert.Equal(50m, FisHesap.SatirIscilik(a));
        Assert.Equal(0m, FisHesap.SatirIscilik(b));
        Assert.Null(FisHesap.SatirIscilik(c));
        Assert.Equal((50m, true), FisHesap.ToplamIscilik([a, b, c]));
        Assert.Equal((50m, false), FisHesap.ToplamIscilik([a, b]));
    }

    [Fact]
    public void Css_tuval_pro_kapsami_sabit_renk_yok_sag_sutun_320_secili_sekme_belirtecle()
    {
        var yol = Path.Combine(CozumKoku(), "Kuyumcu.Web", "wwwroot", "css", "tuval-satis.css");
        var css = Regex.Replace(File.ReadAllText(yol), @"/\*.*?\*/", "", RegexOptions.Singleline);
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b", css);
        Assert.DoesNotMatch(@"\b(rgb|rgba|hsl|hsla)\(", css);
        Assert.Contains("grid-template-columns: minmax(0, 1fr) 320px; column-gap: 18px;", css);
        Assert.Matches(@"\.tuval-pro \.tuval-pro-sekmeler > \.sekme\.secili \{\s*background: var\(--kenar-yuzey\)", css);
        Assert.Contains(".tuval-pro .tuval-pro-sekmeler { display: flex; flex-wrap: wrap;", css);   // dar ekranda sarar
    }

    // ═══ EKRAN — PROFESYONEL ═══════════════════════════════════════════════

    [Fact]
    public void Sekmeler_tuval_sirasi_adlari_Tahsilat_baglanti_Odeme_kilitli_sebepli()
    {
        var e = Ekran();
        var idler = e.FindAll(".tuval-pro-sekmeler > *").Select(x => x.Id ?? "").ToArray();
        Assert.Equal(["islem-tur-SATIS", "islem-tur-ALIS", "islem-tur-HURDA_ALIS", "islem-git-tahsilat",
                      "islem-tur-TAKAS", "islem-tur-SATIS_IADE", "islem-tur-ALIS_IADE"], idler);   // SATIS-3 t11: Ödeme sekmesi yok
        Assert.Equal(Sozlukler.Tr.Islem.HurdaAlis, e.Find("#islem-tur-HURDA_ALIS").TextContent.Trim());   // UX-S31 tek ad
        Assert.Equal(Sozlukler.Tr.Islem.SatisIade, e.Find("#islem-tur-SATIS_IADE").TextContent.Trim());
        Assert.Equal("/cariler", e.Find("#islem-git-tahsilat").GetAttribute("href"));
        Assert.Empty(e.FindAll("#islem-git-odeme, #islem-git-odeme-kilit, .tuval-pro-sekmeler > [disabled]"));   // t11: hep pasif sekme yok
        Assert.Contains("secili", e.Find("#islem-tur-SATIS").ClassName);
    }

    [Fact]
    public void Diger_dugmesi_ozet_yazar_Iscilik_menude_secilince_form_acilir_ve_dugme_adini_yazar()
    {
        var e = Ekran();
        var ozet = e.Find("#islem-diger-ozet").TextContent;
        Assert.Contains(FisDigerSozlugu.Tr.KonsinyeBaslik.ToLower(new CultureInfo("tr-TR")), ozet);
        Assert.Contains(FisDigerSozlugu.Tr.SiparisBaslik.ToLower(new CultureInfo("tr-TR")), ozet);
        Assert.Empty(e.FindAll(".tuval-pro-sekmeler #islem-tur-ISCILIK"));
        e.Find("#islem-diger-menu #islem-tur-ISCILIK").Click();
        e.WaitForElement("#iscilik-tutar");
        Assert.Contains(string.Format(SatisProSozlugu.Tr.DigerSecili, Sozlukler.Tr.Islem.Iscilik), e.Find("#islem-diger-ozet").TextContent);
        Assert.Contains("secili", e.Find("#islem-diger-ac").ClassName);
        Assert.Empty(e.FindAll("#islem-ust-satir"));   // alan satırı yalnız satışta
    }

    [Fact]
    public void Alan_satiri_Musteri_Tarih_Para_Satici_Fiyat_listesi_varsayilan_sonra_musteriden()
    {
        var e = Ekran();
        var satir = e.WaitForElement("#islem-ust-satir");
        Assert.Contains(SatisEkraniSozlugu.Tr.MusteriEtiketi, e.Find("label[for=islem-cari]").TextContent);
        Assert.NotNull(satir.QuerySelector("#islem-tarih"));
        Assert.NotNull(satir.QuerySelector("#islem-para"));
        Assert.Contains(SatisProSozlugu.Tr.AlanSatici, satir.QuerySelector("#islem-satan-sar")!.TextContent);
        Assert.Contains("Test Tezgâhtar", satir.QuerySelector("#islem-satan")!.TextContent);
        e.WaitForAssertion(() => Assert.Contains("Vitrin", e.Find("#islem-fiyat-listesi").TextContent));
        Assert.Contains(SatisProSozlugu.Tr.FiyatListesiVarsayilan, e.Find("#islem-fiyat-listesi-kaynak").TextContent);
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.WaitForAssertion(() => Assert.Contains("Toptan", e.Find("#islem-fiyat-listesi").TextContent));
        Assert.Contains(SatisProSozlugu.Tr.FiyatListesiCariden, e.Find("#islem-fiyat-listesi-kaynak").TextContent);
        Assert.NotNull(e.Find("#islem-cari-bakiye"));   // borç rozeti Müşteri kutusunda
    }

    [Fact]
    public void Fiyat_listesi_okunamazsa_sebep_yazar_rakam_uydurmaz()
    {
        listeHata = true;
        var e = Ekran();
        e.WaitForElement("#islem-fiyat-listesi-hata");
        Assert.Contains("sunucu hatası", e.Find("#islem-fiyat-listesi-hata").TextContent);
        Assert.Empty(e.FindAll("#islem-fiyat-listesi-kaynak"));
    }

    [Fact]
    public void Genis_tablo_tuval_kolonlari_urun_kartindan_ve_teklif_iscilikli()
    {
        var e = KalemliPro();
        var basliklar = e.FindAll("#islem-kalem-pro thead th").Select(x => x.TextContent.Trim()).Take(9).ToArray();
        var P = SatisProSozlugu.Tr;
        Assert.Equal([P.SutunBarkod, P.SutunUrun, P.SutunAyar, P.SutunMilyem, P.SutunGram, P.SutunHas, P.SutunAdet, P.SutunIscilik, P.SutunTutar], basliklar);
        var hucre = e.FindAll("#islem-kalem-pro tbody tr.fis-pro-satir td").Select(x => x.TextContent.Trim()).ToArray();
        Assert.Equal("BLZ-00412", hucre[0]);
        Assert.Equal("Burma Bilezik", e.Find("#islem-kalem-pro tbody tr.fis-pro-satir td.fis-pro-ad .fis-pro-ad-metin").TextContent.Trim());
        Assert.Equal("BLZ-00412", e.Find("#islem-kalem-pro tbody tr.fis-pro-satir td.fis-pro-ad .fis-pro-ad-barkod").TextContent.Trim());   // SATIS-3: barkod adın altında
        Assert.Equal("22", hucre[2]);
        Assert.Equal("916", hucre[3]);
        Assert.Equal("4,30", hucre[4]);
        Assert.Equal("3,94", hucre[5]);
        Assert.Equal(Bicim.Tl(500m), hucre[7]);
        Assert.Equal(Bicim.Tl(10000m), hucre[8]);
        // Birim fiyat / iskonto / hediye / sil ✎ satırında — kapalıyken DOM'da yok
        Assert.Empty(e.FindAll("#si-kalem-fiyat-u1-ac"));
        e.Find("#islem-kalem-duzenle-0").Click();
        Assert.Equal("true", e.Find("#islem-kalem-duzenle-0").GetAttribute("aria-expanded"));
        Assert.NotNull(e.Find("#islem-kalem-ayrinti-0 #si-kalem-fiyat-u1-ac"));
        Assert.NotNull(e.Find("#islem-kalem-ayrinti-0 #si-kalem-iskonto-u1-ac"));
        Assert.NotNull(e.Find("#islem-kalem-ayrinti-0 #islem-kalem-hediye-0"));
        // fiyat elle değişince teklifin işçiliği artık bilinmez → "—"
        e.Find("#si-kalem-fiyat-u1-ac").Click();
        e.Find("#si-kalem-fiyat-u1").Input("9000");
        e.Find("#si-kalem-fiyat-u1-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal("—", e.Find("#islem-kalem-iscilik-0").TextContent.Trim()));
    }

    [Fact]
    public void Sabit_fiyatli_kalemde_iscilik_hucre_ve_serit_TIRE_0_TL_uydurulmaz()
    {
        // Doğrulayıcı bulgusu (KRİTİK): sabit fiyatta sunucu işçiliği ayrıştırmaz (iscilikTl 0 = "ayrıştırılmadı").
        teklifSabit = true;
        var e = KalemliPro();
        Assert.Equal("—", e.Find("#islem-kalem-iscilik-0").TextContent.Trim());
        Assert.Equal(SatisProSozlugu.Tr.IscilikBilinmiyor, e.Find("#islem-kalem-iscilik-0").GetAttribute("title"));
        Assert.Equal("—", e.Find("#islem-pro-toplam-iscilik").TextContent.Trim());
        Assert.Equal(SatisProSozlugu.Tr.IscilikBilinmiyor, e.Find("#islem-pro-toplam-iscilik").GetAttribute("title"));
        Assert.NotEqual(Bicim.Tl(0m), e.Find("#islem-pro-toplam-iscilik").TextContent.Trim());
        Assert.NotEqual(Bicim.Tl(0m), e.Find("#islem-kalem-iscilik-0").TextContent.Trim());
        Assert.Equal(Bicim.Tl(10000m), e.Find("#islem-pro-genel-toplam").TextContent.Trim());   // fiyat yine teklifin fiyatı
    }

    // ═══ PAKET T-S2b — doğrulayıcı bulgusu (t3, KRİTİK): kampanya indirimli üründe İşçilik
    //     indirim ÖNCESİ değeri gösteriyordu. İndirim fiyatın bütününden düşer; işçilik payı
    //     ayrıştırılmaz → bilinmiyor ("—"). Aynı sınıf: satır iskontosu, fiş iskontosu. ═══

    [Fact]
    public void TS2b_indirimli_teklifte_ve_iskontolu_satirda_iscilik_BILINMIYOR_null()
    {
        var teklif = new Teklif(true, null, 10000m, 9500m, 0m, 500m, 1000m, 9000m, "kmp1", false, 10000m);
        Assert.Null(FisProKalemKarti.TeklifIscilik(teklif, 9000m));                      // kampanya: 500 indirim ÖNCESİ
        Assert.Null(FisProKalemKarti.TeklifIscilik(teklif, 10000m));                     // liste fiyatı da satış fiyatı değil
        Assert.Equal(500m, FisProKalemKarti.TeklifIscilik(teklif with { IndirimTutari = 0m, SatisFiyati = 10000m, KampanyaId = null }, 10000m));
        Assert.Equal(500m, FisProKalemKarti.TeklifIscilik(teklif with { IndirimTutari = null, SatisFiyati = 10000m, KampanyaId = null }, 10000m));

        var a = new FisKalemi { UrunId = "a", UrunAdi = "A", Adet = 2m, BirimFiyat = 100m, IscilikTl = 25m };
        var isk = new FisKalemi { UrunId = "i", UrunAdi = "I", Adet = 1m, BirimFiyat = 100m, IscilikTl = 25m, SatirIskontoYuzde = 10m };
        var hed = new FisKalemi { UrunId = "h", UrunAdi = "H", Adet = 1m, BirimFiyat = 0m, IscilikTl = 40m, Hediye = true };
        Assert.Null(FisHesap.SatirIscilik(isk));                                          // satır iskontosu: pay ayrıştırılmaz
        Assert.Equal((50m, true), FisHesap.ToplamIscilik([a, isk]));
        Assert.Equal((50m, false), FisHesap.ToplamIscilik([a], null));
        Assert.Equal((50m, false), FisHesap.ToplamIscilik([a], 0m));
        Assert.Equal((0m, true), FisHesap.ToplamIscilik([a], 5m));                        // fiş iskontosu: toplam bilinmez
        Assert.Equal((0m, false), FisHesap.ToplamIscilik([hed], 5m));                     // yalnız hediye: işçilik 0 (bedelsiz)

        var urunler = new List<UrunVeri> { new("a", "A", "K22", null, 2m, null, 100m) };
        Assert.Equal(4m, FisHesap.ToplamHasBilinen([a], urunler));
        Assert.Null(FisHesap.ToplamHasBilinen([a, isk], urunler));                        // "i" ürün kartı yok → has bilinmez
    }

    [Fact]
    public void TS2b_kampanyali_kalemde_iscilik_hucre_ve_serit_TIRE_indirim_oncesi_500_BASILMAZ()
    {
        teklifKampanya = true;
        var e = KalemliPro("9000");
        var hucre = e.Find("#islem-kalem-iscilik-0");
        Assert.Equal("—", hucre.TextContent.Trim());
        Assert.Equal(SatisProSozlugu.Tr.IscilikBilinmiyor, hucre.GetAttribute("title"));
        Assert.Equal("—", e.Find("#islem-pro-toplam-iscilik").TextContent.Trim());
        Assert.DoesNotContain(Bicim.Tl(500m), e.Find("#islem-kalem-pro").TextContent);
        Assert.DoesNotContain(Bicim.Tl(500m), e.Find("#islem-pro-toplam").TextContent);
        // Tutar ve genel toplam SUNUCUNUN indirim SONRASI satış fiyatı
        var satir = e.FindAll("#islem-kalem-pro tbody tr.fis-pro-satir td").Select(x => x.TextContent.Trim()).ToArray();
        Assert.Equal(Bicim.Tl(9000m), satir[8]);
        Assert.Equal(Bicim.Tl(9000m), e.Find("#islem-pro-genel-toplam").TextContent.Trim());
        Assert.Equal("3,94 g", e.Find("#islem-pro-toplam-has").TextContent.Trim());   // has indirimden etkilenmez (ürün kartı)
    }

    [Fact]
    public void TS2b_satir_iskontosu_girilince_iscilik_TIRE_kaldirilinca_geri_gelir()
    {
        var e = KalemliPro();
        Assert.Equal(Bicim.Tl(500m), e.Find("#islem-kalem-iscilik-0").TextContent.Trim());
        e.Find("#islem-kalem-duzenle-0").Click();
        e.Find("#si-kalem-iskonto-u1-ac").Click();
        e.Find("#si-kalem-iskonto-u1").Input("10");
        e.Find("#si-kalem-iskonto-u1-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal("—", e.Find("#islem-kalem-iscilik-0").TextContent.Trim()));
        Assert.Equal("—", e.Find("#islem-pro-toplam-iscilik").TextContent.Trim());
        Assert.Equal(Bicim.Tl(9000m), e.Find("#islem-pro-genel-toplam").TextContent.Trim());
        e.Find("#si-kalem-iskonto-u1-ac").Click();
        e.Find("#si-kalem-iskonto-u1").Input("0");
        e.Find("#si-kalem-iskonto-u1-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(Bicim.Tl(500m), e.Find("#islem-kalem-iscilik-0").TextContent.Trim()));
    }

    [Fact]
    public void TS2b_fis_iskontosu_girilince_serit_iscilik_TIRE_silinince_geri_gelir()
    {
        var e = KalemliPro();
        Assert.Equal(Bicim.Tl(500m), e.Find("#islem-pro-toplam-iscilik").TextContent.Trim());
        e.Find("#islem-fis-iskonto").Input("5");
        e.WaitForAssertion(() => Assert.Equal("—", e.Find("#islem-pro-toplam-iscilik").TextContent.Trim()));
        Assert.Equal(SatisProSozlugu.Tr.IscilikBilinmiyor, e.Find("#islem-pro-toplam-iscilik").GetAttribute("title"));
        Assert.Equal(Bicim.Tl(9500m), e.Find("#islem-pro-genel-toplam").TextContent.Trim());
        e.Find("#islem-fis-iskonto").Input("");
        e.WaitForAssertion(() => Assert.Equal(Bicim.Tl(500m), e.Find("#islem-pro-toplam-iscilik").TextContent.Trim()));
    }

    [Fact]
    public void TS2b_milyem_kayitli_degilse_hucre_TIRE_0_uydurulmaz()
    {
        urunMilyem = "0";
        var e = KalemliPro();
        Assert.Equal("—", e.Find("#islem-kalem-milyem-0").TextContent.Trim());
        Assert.Equal("4,30", e.Find("#islem-kalem-gram-0").TextContent.Trim());
        Assert.Equal("3,94", e.Find("#islem-kalem-has-0").TextContent.Trim());
    }

    [Fact]
    public void Alt_toplam_has_iscilik_KDV_faturada_genel_toplam()
    {
        var e = KalemliPro();
        Assert.Equal("3,94 g", e.Find("#islem-pro-toplam-has").TextContent.Trim());
        Assert.Equal(Bicim.Tl(500m), e.Find("#islem-pro-toplam-iscilik").TextContent.Trim());
        Assert.Equal(SatisProSozlugu.Tr.KdvFaturada, e.Find("#islem-pro-kdv").TextContent.Trim());
        Assert.Equal(SatisProSozlugu.Tr.KdvIpucu, e.Find("#islem-pro-kdv").GetAttribute("title"));
        Assert.Equal(Bicim.Tl(10000m), e.Find("#islem-pro-genel-toplam").TextContent.Trim());
        Assert.Contains(SatisProSozlugu.Tr.Kdv, e.Find("#islem-pro-toplam").TextContent);
    }

    [Fact]
    public void Bos_fiste_kart_ve_toplam_seridi_CIZILIR_bos_durum_metniyle()
    {
        var e = Ekran();
        Assert.Equal(SatisProSozlugu.Tr.KalemBos, e.Find("#islem-pro-kalem-bos").TextContent.Trim());
        Assert.NotNull(e.Find("#islem-pro-toplam"));
        Assert.Equal(Bicim.Tl(0m), e.Find("#islem-pro-genel-toplam").TextContent.Trim());
        Assert.Empty(e.FindAll("#islem-kalem-pro"));
    }

    [Fact]
    public void Hap_Karma_boler_ozet_Fark_yazar_Tek_geri_doner_istek_atmaz()
    {
        // SATIS-2 (kurucu 11:35): "Karma | Tek" anahtarı yerine TEK bağlantı — "+ Başka ödeme ekle" (#islem-pro-karma)
        // mevcut OdemeyiBol yolunu, karmada "Tek ödemeye dön" (#islem-pro-tek) TekOdemeyeDon'u çağırır.
        var e = KalemliPro();
        Assert.Empty(e.FindAll("#islem-pro-tek"));
        Assert.Equal(SatisSadeSozlugu.Tr.BaskaOdemeEkle, e.Find("#islem-pro-karma").TextContent.Trim());
        Assert.Contains(Bicim.Tl(10000m), e.Find("#islem-pro-odeme-ozet").TextContent);
        var once = istekler.Count(i => i.Metot == "POST");
        e.Find("#islem-pro-karma").Click();
        e.WaitForElement("#islem-pro-tek");
        Assert.Empty(e.FindAll("#islem-pro-karma"));
        Assert.NotNull(e.Find("#islem-pro-fark"));
        Assert.Equal(Bicim.Tl(0m), e.Find("#islem-pro-fark").TextContent.Trim());   // tek ödeme ilk bacak oldu → fark 0
        Assert.True(e.Find("#islem-karma").HasAttribute("checked") || e.Find("#islem-karma").GetAttribute("checked") is not null);
        e.Find("#islem-pro-tek").Click();
        e.WaitForElement("#islem-pro-karma");
        Assert.Empty(e.FindAll("#islem-pro-fark"));
        Assert.Equal(once, istekler.Count(i => i.Metot == "POST"));
    }

    [Fact]
    public void Vazgec_dolu_fiste_cizilir_bosaltir_POST_atmaz()
    {
        var e = KalemliPro();
        var once = SatisPostSayisi();
        e.Find("#islem-vazgec").Click();
        e.WaitForElement("#islem-pro-kalem-bos");
        Assert.Empty(e.FindAll("#islem-vazgec"));
        Assert.Equal(once, SatisPostSayisi());
    }

    [Fact]
    public void Yasal_kapilar_esikler_SUNUCUDAN_nakit_Uygun_kimlik_Altinda()
    {
        var e = Ekran();
        e.WaitForAssertion(() => Assert.Equal(string.Format(SatisProSozlugu.Tr.NakitKurali, Bicim.Tl(50_000m)), e.Find("#islem-pro-nakit-etiket").TextContent.Trim()));
        Assert.Equal(SatisProSozlugu.Tr.Uygun, e.Find("#islem-pro-nakit").TextContent.Trim());
        Assert.Equal(string.Format(SatisProSozlugu.Tr.KimlikEsigi, Bicim.Tl(200_000m)), e.Find("#islem-pro-kimlik-etiket").TextContent.Trim());
        Assert.Equal(SatisProSozlugu.Tr.Altinda, e.Find("#islem-pro-kimlik").TextContent.Trim());
        Assert.Equal(SatisProSozlugu.Tr.KdvFaturada, e.Find("#islem-pro-kdv-matrah").TextContent.Trim());
    }

    [Fact]
    public void Yasal_kapilar_sunucu_devrede_derse_Asildi()
    {
        tevsikDevrede = true;
        var e = Ekran();
        e.WaitForAssertion(() => Assert.Equal(SatisProSozlugu.Tr.Asildi, e.Find("#islem-pro-nakit").TextContent.Trim()));
    }

    [Fact]
    public void Yasal_kapilar_gunluk_KALAN_nakit_hakkini_asan_fiste_Asildi()
    {
        tevsikKalan = 5_000m;   // sunucu: bu müşterinin bugünkü kalan nakit hakkı 5.000 (had yine sunucudan)
        var e = KalemliPro();   // fiş 10.000
        e.WaitForAssertion(() => Assert.Equal(SatisProSozlugu.Tr.Asildi, e.Find("#islem-pro-nakit").TextContent.Trim()));
    }

    [Fact]
    public void Yasal_kapilar_sunucu_yanit_vermezse_SAYISIZ_Bilinmiyor_ve_sebep()
    {
        tevsikHad = null;
        var e = Ekran();
        e.WaitForElement("#islem-pro-yasal-hata");
        Assert.Equal(SatisProSozlugu.Tr.NakitKuraliSayisiz, e.Find("#islem-pro-nakit-etiket").TextContent.Trim());
        Assert.Equal(SatisProSozlugu.Tr.Bilinmiyor, e.Find("#islem-pro-nakit").TextContent.Trim());
        Assert.DoesNotMatch(@"\d", e.Find("#islem-pro-nakit-etiket").TextContent);   // istemci varsayılanı YOK
    }

    [Fact]
    public void Musteri_karti_bakiye_son_islem_Karti_ac_Pesinde_istek_yok()
    {
        var e = Ekran();
        // SATIS-2 (kurucu 11:35): müşteri kartı YALNIZ gerçek müşteri seçiliyken — Peşin'de kart hiç çizilmez.
        e.WaitForAssertion(() => Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        Assert.Empty(e.FindAll("#islem-pro-musteri"));
        Assert.False(Istendi("/cari/pm1/bakiye"));
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.WaitForAssertion(() => Assert.Contains("4", e.Find("#islem-pro-musteri-son").TextContent));
        Assert.Equal(new DateTime(2026, 9, 4).ToString("d MMMM", new CultureInfo("tr-TR")), e.Find("#islem-pro-musteri-son").TextContent.Trim());
        Assert.Equal(string.Format(SatisProSozlugu.Tr.Borclu, "5,66 g"), e.Find("#islem-pro-musteri-has").TextContent.Trim());
        Assert.Equal(string.Format(SatisProSozlugu.Tr.Borclu, Bicim.Tl(38_900m)), e.Find("#islem-pro-musteri-tl").TextContent.Trim());
        Assert.Equal("/cariler?cariId=c1", e.Find("#islem-pro-karti-ac").GetAttribute("href"));
        Assert.Single(istekler, i => i.Yol.Contains("/cari/c1/bakiye"));
    }

    [Fact]
    public void Musteri_karti_Vazgec_ile_Pesine_donunce_Pesin_bakiyesi_ISTENMEZ()
    {
        var e = KalemliPro();
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#islem-pro-karti-ac")));
        e.Find("#islem-vazgec").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-pro-musteri")));   // SATIS-2: Peşin'de kart yok
        Assert.False(Istendi("/cari/pm1/bakiye"));
    }

    [Fact]
    public void Kisayollar_karti_dort_tus_ve_F2_F4_F8_gercekten_odaklar()
    {
        var e = Ekran();
        Assert.Equal(["F2", "F4", "F8", "F9"], e.FindAll("#islem-pro-kisayollar kbd").Select(x => x.TextContent.Trim()).ToArray());
        int Odak(string hedef) => JSInterop.Invocations.Count(i => i.Identifier == "kuyumcuOdak.alana" && Equals(i.Arguments[0], hedef));
        // Kararsızlık düzeltmesi (doğrulayıcı bulgusu ORTA): açılıştaki ASENKRON barkod odağı
        // (FisKalemGirisi.OnAfterRenderAsync) bitmeden sayaç okunursa F2 sayımı kayar →
        // önce açılış odağı ve yükleme bitsin, sonra her tuşun KENDİ çağrısı beklenerek sayılır.
        e.WaitForElement("#islem-barkod");
        e.WaitForAssertion(() => Assert.Equal(1, Odak("islem-barkod")));
        foreach (var (tus, hedef) in new[] { ("F2", "islem-barkod"), ("F4", "islem-cari"), ("F8", "islem-odeme") })
        {
            var once = Odak(hedef);
            e.Find(".islem-sayfa").KeyDown(new KeyboardEventArgs { Key = tus });
            e.WaitForAssertion(() => Assert.Equal(once + 1, Odak(hedef)));
        }
        Assert.Equal(2, Odak("islem-barkod"));   // açılış 1 + F2 1 — fazladan odak çağrısı yok
    }

    [Fact]
    public void F9_bos_fiste_POST_atmaz()
    {
        var bos = Ekran();
        bos.Find(".islem-sayfa").KeyDown(new KeyboardEventArgs { Key = "F9" });
        Assert.Equal(0, SatisPostSayisi());
        // kilitli Kaydet'e F9 HİÇ gitmez: hata bandı/alan altı mesaj doğmaz (kilit sebebi zaten yazılı)
        Assert.True(string.IsNullOrWhiteSpace(bos.FindAll("#hata-bandi").FirstOrDefault()?.TextContent));
        Assert.DoesNotContain(bos.FindAll(".alan-hata"), x => !string.IsNullOrWhiteSpace(x.TextContent));
    }

    [Fact]
    public void F9_kilitli_Kaydette_HICBIR_SEY_gondermez_hata_bandi_dogurmaz()
    {
        var e = KalemliPro();
        e.Find("#islem-pro-karma").Click();
        e.WaitForElement("#islem-bacak-0-tutar");
        e.Find("#islem-bacak-0-tutar").Input("6000");
        e.WaitForElement("#islem-bacak-1-tutar").Input("3000");   // SATIS-3: iki satır da elle → fark ≠ 0 → Kaydet kilitli (sebep altta)
        e.WaitForAssertion(() => Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled")));
        e.Find(".islem-sayfa").KeyDown(new KeyboardEventArgs { Key = "F9" });
        Assert.Equal(0, SatisPostSayisi());
        Assert.Empty(e.FindAll("#hata-bandi"));
    }

    [Fact]
    public void F9_dolu_fiste_KAYDEDER()
    {
        var e = KalemliPro();
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled")), TimeSpan.FromSeconds(5));
        e.Find(".islem-sayfa").KeyDown(new KeyboardEventArgs { Key = "F9" });
        e.WaitForAssertion(() => Assert.Equal(1, SatisPostSayisi()), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Kaydet_ve_yazdir_etiketi_ve_sag_sutun_notu()
    {
        var e = Ekran();
        Assert.Equal(SatisTuvalSozlugu.Tr.KaydetYazdir, e.Find("#islem-kaydet").TextContent.Trim());
        // SATIS-2 (j): tuvalin tasarım notu ("Basit modda katlı olan her şey burada açık…") ürün metni DEĞİL — ekranda yok.
        Assert.Empty(e.FindAll("#islem-pro-not"));
        Assert.DoesNotContain(SatisProSozlugu.Tr.ProNot, e.Markup);
        Assert.Contains(SatisProSozlugu.Tr.YasalKapilar, e.Find("#islem-pro-yasal").TextContent);
    }

    [Fact]
    public void Basit_yuz_ETKILENMEZ_pro_ogeleri_yok_fiyat_listesi_istenmez()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        Assert.Contains("tuval-tezgah", e.Find(".islem-sayfa").ClassName);
        Assert.DoesNotContain("tuval-pro", e.Find(".islem-sayfa").ClassName);
        Assert.Empty(e.FindAll("#islem-pro-yan"));
        Assert.Empty(e.FindAll("#islem-fiyat-listesi-sar"));
        Assert.Empty(e.FindAll("#islem-pro-kalem-kart"));
        Assert.Empty(e.FindAll("#islem-pro-odeme-hap"));
        Assert.False(Istendi("/fiyat/listeler"));
        e.Find(".islem-sayfa").KeyDown(new KeyboardEventArgs { Key = "F4" });   // F-tuşları yalnız PRO
        Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "kuyumcuOdak.alana" && Equals(i.Arguments[0], "islem-cari"));
    }

    // ═══ DOĞRULAYICI BULGUSU (KRİTİK) — has/gram hanesi PRO ekranında KAYBOLMAZ ═══
    // P-57: Bicim.Has = F4, Bicim.Gram = F3. Tuval iki hane gösterir ("5,66"); iki hane
    // yalnız değer iki haneye sığıyorsa kullanılır — 0,0003 g "0,00 g" diye BASILMAZ.

    [Theory]
    [InlineData("5.66", "5,66")]
    [InlineData("1.5", "1,50")]
    [InlineData("0.0003", "0,0003")]
    [InlineData("0.003", "0,003")]
    [InlineData("1234.5678", "1.234,5678")]
    [InlineData("0.00004", "0,00")]          // has 4 hane taşır; altı yuvarlanır (Bicim.Has ile aynı)
    [InlineData("0.00005", "0,0001")]        // AwayFromZero
    public void HasSayi_en_az_iki_en_cok_dort_hane_tr_TR(string deger, string beklenen) =>
        Assert.Equal(beklenen, FisHesap.HasSayi(decimal.Parse(deger, CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData("4.3", "4,30")]
    [InlineData("0.001", "0,001")]
    [InlineData("10.4", "10,40")]
    [InlineData("0.0004", "0,00")]
    [InlineData("1234.5675", "1.234,568")]
    public void GramSayi_en_az_iki_en_cok_uc_hane_tr_TR(string deger, string beklenen) =>
        Assert.Equal(beklenen, FisHesap.GramSayi(decimal.Parse(deger, CultureInfo.InvariantCulture)));

    [Fact]
    public void Kalem_tablosu_ve_toplam_serit_kucuk_has_ve_grami_KAYBETMEZ()
    {
        urunHas = "0.0001"; urunGram = "0.001";
        var e = KalemliPro();
        var hucre = e.FindAll("#islem-kalem-pro tbody tr.fis-pro-satir td").Select(x => x.TextContent.Trim()).ToArray();
        Assert.Equal("0,001", hucre[4]);    // gram
        Assert.Equal("0,0001", hucre[5]);   // has
        Assert.Equal("0,0001 g", e.Find("#islem-pro-toplam-has").TextContent.Trim());
        // Hücre dar kalırsa kesilen değer title'da TAM okunur
        var td = e.FindAll("#islem-kalem-pro tbody tr.fis-pro-satir td");
        // GOC-C-2: hücre kimliği ve title'ı ortak ızgara hücresinin içindeki span'da (td satıra özgü öznitelik almaz).
        Assert.Equal("0,0001", td[5].QuerySelector("span")!.GetAttribute("title"));
        Assert.Equal("0,001", td[4].QuerySelector("span")!.GetAttribute("title"));
    }

    [Fact]
    public void Musteri_karti_kucuk_bakiye_tutari_yaziliyorsa_yonu_de_dogru_sifira_yuvarlaniyorsa_hesap_kapali()
    {
        var P = SatisProSozlugu.Tr;
        var e = Render<FisProYanSutun>(p => p.Add(x => x.Cari, new CariVeri("c1", "Hasan", TlBakiye: -0.004m, HasGramBakiye: 0.003m)));
        Assert.Equal(string.Format(P.Borclu, "0,003 g"), e.Find("#islem-pro-musteri-has").TextContent.Trim());
        Assert.Equal(P.HesapSifir, e.Find("#islem-pro-musteri-tl").TextContent.Trim());   // "0 ₺ alacaklı" DEĞİL
        var e2 = Render<FisProYanSutun>(p => p.Add(x => x.Cari, new CariVeri("c2", "Ayşe", TlBakiye: 0.005m, HasGramBakiye: -0.00004m)));
        Assert.Equal(P.HesapSifir, e2.Find("#islem-pro-musteri-has").TextContent.Trim());   // "0,00 g alacaklı" DEĞİL
        Assert.Equal(string.Format(P.Borclu, Bicim.Tl(0.01m)), e2.Find("#islem-pro-musteri-tl").TextContent.Trim());
    }

    [Fact]
    public void Css_1100_1399_yasal_kapilar_formun_USTUNDE_karma_sil_dugmesi_kutu_altinda()
    {
        // Doğrulayıcı bulguları (DÜŞÜK): 1280 PRO'da Yasal kapılar ilk ekranda görünmüyordu; karma kipte
        // sil düğmesi kutu altlarını ±29 px ayırıyordu (K7). Gerçek ölçüm raporda; burada kuralın varlığı.
        var yol = Path.Combine(CozumKoku(), "Kuyumcu.Web", "wwwroot", "css", "tuval-satis.css");
        var css = Regex.Replace(File.ReadAllText(yol), @"/\*.*?\*/", "", RegexOptions.Singleline);
        var blok = Regex.Match(css, @"@media \(min-width: 1100px\) and \(max-width: 1399\.98px\) \{(.*?)\n\}", RegexOptions.Singleline);
        Assert.True(blok.Success, "1100–1399 bloğu yok");
        Assert.Contains(".islem-sayfa.tuval-pro > #islem-pro-yan { display: contents; }", blok.Value);
        Assert.Matches(@"#islem-pro-yan > #islem-pro-yasal \{\s*order: 1; grid-column: 1 / -1;", blok.Value);
        Assert.Contains(".islem-sayfa.tuval-pro > #islem-pro-yan > * { order: 3;", blok.Value);
        Assert.Contains(".tuval-pro .bacak-satiri > [id^=\"islem-bacak-sil-\"] { order: 1; align-self: flex-start; margin-top: calc(var(--bacak-etiket) + 4px); height: var(--kontrol-yuksekligi);", css);
        Assert.Contains(".tuval-pro .bacak-satiri > * > .alan-etiket { font-size: var(--tp-16); line-height: 1.4; height: var(--bacak-etiket);", css);
        Assert.Contains(".tuval-pro .bacak-satiri > [id$=\"-kasa-tek\"] { order: 2; flex: 1 1 100%; }", css);
    }

    [Fact]
    public void Css_Diger_etiketi_ok_ile_tek_satir()
    {
        var yol = Path.Combine(CozumKoku(), "Kuyumcu.Web", "wwwroot", "css", "tuval-satis.css");
        var css = Regex.Replace(File.ReadAllText(yol), @"/\*.*?\*/", "", RegexOptions.Singleline);
        Assert.Contains(".tuval-pro #islem-diger-ac .islem-diger-etiket { white-space: nowrap; }", css);
        // 4 haneli has ("3,9388") 56 px'te "3,93…" diye KESİLİYORDU (1440 gerçek Chrome ölçümü) → has ≥ 72, gram ≥ 62
        var has = int.Parse(Regex.Match(css, @"\.fis-pro-tablo th:nth-child\(6\) \{ width: (\d+)px; \}").Groups[1].Value);
        var gram = int.Parse(Regex.Match(css, @"\.fis-pro-tablo th:nth-child\(5\) \{ width: (\d+)px; \}").Groups[1].Value);
        Assert.True(has >= 72, $"has sütunu {has}px");
        Assert.True(gram >= 62, $"gram sütunu {gram}px");
        var barkod = int.Parse(Regex.Match(css, @"\.fis-pro-tablo th:nth-child\(1\) \{ width: (\d+)px; \}").Groups[1].Value);
        Assert.True(barkod >= 118, $"barkod sütunu {barkod}px (104'te 'BLZ-004…' kesildi)");
        var e = Ekran();
        Assert.Equal(FisDigerSozlugu.Tr.MenuAc + " ▾", e.Find("#islem-diger-ac .islem-diger-etiket").TextContent.Trim());
    }
}
