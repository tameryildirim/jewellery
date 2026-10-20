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
    private string urunHas = "3.94", urunGram = "4.3";
    // TEMA TEMİZLİĞİ 17.10: teklifSabit / teklifKampanya bayrakları yalnız silinen geniş tablo testlerinindi (kaldırıldı).
    private readonly string urunMilyem = "916";             // T-S2b: milyem kayıtlı değilse (0) hücre "—"   // doğrulayıcı bulgusu: küçük has/gram ekranda kaybolmaz

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
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#islem-tur-SATIS");
        return e;
    }

    // TEMA TEMİZLİĞİ 17.10: PRO SATIŞ da artık Yıldız tezgâhıdır. Eski T-S2 tuvalinin geniş kalem tablosu + alt toplam
    // şeridi (FisProKalemKarti), sağ sütunu (FisProYanSutun: yasal kapılar kartı · müşteri kartı · kısayollar) ve ödeme
    // hapı (FisProOdemeHapi) Yıldız satışında BİLEREK çizilmez (Yildiz4BTestleri.Y4B_20, Yildiz4CTestleri.Y4C_20).
    // Satıcı + fiyat listesi "Değiştir" katmanının ekinde (#yildiz-musteri-pro-ac → #yildiz-musteri-pro); yasal durum
    // Fark satırının notunda (#yildiz-tezgah-not); karma "+ Başka ödeme"/"− Tek ödeme" hapları.
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> KalemliPro(string beklenenFiyat = "10000")
    {
        var e = Ekran().YildizHazir();
        e.YildizKalemEkle(beklenenFiyat: beklenenFiyat);
        return e;
    }

    /// <summary>"Değiştir" katmanını ve satıcı/fiyat listesi ekini açar (hata varsa ek kendiliğinden açık gelir).</summary>
    private static void ProEkAc(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        if (e.FindAll("#yildiz-musteri-secici").Count == 0) e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForElement("#yildiz-musteri-secici");
        if (e.FindAll("#yildiz-musteri-pro").Count == 0) e.Find("#yildiz-musteri-pro-ac").Click();
        e.WaitForElement("#yildiz-musteri-pro");
    }

    private static string YasalNot(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) =>
        e.FindAll("#yildiz-tezgah-not").FirstOrDefault()?.TextContent.Trim() ?? "";

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
        // Yıldız: Satıcı + Fiyat listesi "Değiştir" katmanının ekinde (aynı FisSatici / FisProFiyatListesi, aynı id'ler).
        // Tarih/Para kutuları Yıldız'da BİLEREK yok (sistemin bildiği değer — UX-S21, tuvalde yok).
        var e = Ekran().YildizHazir();
        ProEkAc(e);
        Assert.Contains(SatisProSozlugu.Tr.AlanSatici, e.Find("#yildiz-musteri-pro #islem-satan-sar").TextContent);
        Assert.Contains("Test Tezgâhtar", e.Find("#islem-satan").TextContent);
        e.WaitForAssertion(() => Assert.Contains("Vitrin", e.Find("#islem-fiyat-listesi").TextContent));
        Assert.Contains(SatisProSozlugu.Tr.FiyatListesiVarsayilan, e.Find("#islem-fiyat-listesi-kaynak").TextContent);
        Assert.Empty(e.FindAll("#islem-tarih, #islem-para"));
        e.YildizMusteriSec("c1");   // seçimle katman kapanır
        ProEkAc(e);
        e.WaitForAssertion(() => Assert.Contains("Toptan", e.Find("#islem-fiyat-listesi").TextContent));
        Assert.Contains(SatisProSozlugu.Tr.FiyatListesiCariden, e.Find("#islem-fiyat-listesi-kaynak").TextContent);
        Assert.Contains(Bicim.Tl(38_900m).Replace(' ', ' '), e.Find("#yildiz-tezgah-ust").TextContent.Replace(' ', ' '));   // borç durumu üst satırda
    }

    [Fact]
    public void Fiyat_listesi_okunamazsa_sebep_yazar_rakam_uydurmaz()
    {
        listeHata = true;
        var e = Ekran().YildizHazir();
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForElement("#yildiz-musteri-pro");   // hata gizlenmez: ek KENDİLİĞİNDEN açık gelir (UX-S28)
        e.WaitForElement("#islem-fiyat-listesi-hata");
        Assert.Contains("sunucu hatası", e.Find("#islem-fiyat-listesi-hata").TextContent);
        Assert.Empty(e.FindAll("#islem-fiyat-listesi-kaynak"));
    }

    // TEMA TEMİZLİĞİ 17.10: Genis_tablo_tuval_kolonlari_urun_kartindan_ve_teklif_iscilikli ve
    // Sabit_fiyatli_kalemde_iscilik_hucre_ve_serit_TIRE_0_TL_uydurulmaz kaldırıldı — geniş kalem tablosu ve alt toplam
    // şeridi (FisProKalemKarti: Barkod/Milyem/İşçilik sütunları, #islem-pro-toplam-iscilik) Yıldız satışında BİLEREK yok
    // (Y4C_20 "#islem-kalem-pro yok"); Yıldız kalem satırı işçilik yazmaz → uydurulacak işçilik rakamı da yok. İşçilik
    // kuralı (TeklifIscilik / SatirIscilik) saf testlerde AYNEN (Iscilik_yalniz_teklif..., TS2b_indirimli...).

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

    // TEMA TEMİZLİĞİ 17.10: TS2b_kampanyali_kalemde_iscilik_hucre_ve_serit_TIRE_indirim_oncesi_500_BASILMAZ,
    // TS2b_satir_iskontosu_girilince_iscilik_TIRE_kaldirilinca_geri_gelir, TS2b_fis_iskontosu_girilince_serit_iscilik_TIRE_silinince_geri_gelir,
    // TS2b_milyem_kayitli_degilse_hucre_TIRE_0_uydurulmaz, Alt_toplam_has_iscilik_KDV_faturada_genel_toplam ve
    // Bos_fiste_kart_ve_toplam_seridi_CIZILIR_bos_durum_metniyle kaldırıldı — hepsi geniş kalem tablosunun hücreleri
    // (#islem-kalem-iscilik/-milyem-N, ✎ satırı) ve alt toplam şeridi (#islem-pro-toplam-*, KDV "Faturada") içindi; ikisi de
    // Yıldız satışında BİLEREK yok (Y4C_20). Kampanya/iskonto sonrası tutarın sunucu fiyatından geldiği gövde testleri
    // (Yildiz4CTestleri.Y4C_23 indirimli gövde = platin) ve boş fiş durumu (Yildiz4BTestleri.Y4B_11) Yıldız yolunda.

    [Fact]
    public void Hap_Karma_boler_ozet_Fark_yazar_Tek_geri_doner_istek_atmaz()
    {
        // Yıldız: "+ Başka ödeme" mevcut OdemeyiBol yolunu, karmada "− Tek ödeme" TekOdemeyeDon(Aracla) yolunu çağırır.
        var e = KalemliPro();
        Assert.Empty(e.FindAll("#yildiz-tezgah-tek-odeme"));
        Assert.Equal(YildizTezgahSozlugu.BaskaOdeme("tr"), e.Find("#yildiz-tezgah-baska-odeme").TextContent.Trim());
        Assert.Equal(Bicim.TlKurus(10000m), e.Find("#yildiz-odeme-0 .yz-tezgah-odeme-tutar").TextContent.Trim());
        var once = istekler.Count(i => i.Metot == "POST");
        e.YildizBol();
        e.WaitForElement("#yildiz-tezgah-tek-odeme");
        Assert.Empty(e.FindAll("#yildiz-odeme-0"));
        Assert.Equal(Bicim.Tl(0m).Replace(' ', ' '), e.YildizFark());   // tek ödeme ilk satır oldu → fark 0
        e.Find("#yildiz-tezgah-tek-odeme").Click();
        e.WaitForElement("#yildiz-odeme-0");
        Assert.Empty(e.FindAll("#yildiz-tezgah-tek-odeme, .bacak-satiri"));
        Assert.Equal(once, istekler.Count(i => i.Metot == "POST"));
    }

    [Fact]
    public void Vazgec_dolu_fiste_cizilir_bosaltir_POST_atmaz()
    {
        var e = KalemliPro();
        var once = SatisPostSayisi();
        Assert.Equal("button", e.Find("#yildiz-tezgah-vazgec").LocalName);
        e.Find("#yildiz-tezgah-vazgec").Click();
        e.WaitForElement("#yildiz-tezgah-bos");
        Assert.Equal("a", e.Find("#yildiz-tezgah-vazgec").LocalName);   // boş fişte boşaltan düğme yok (ana sayfa bağlantısı)
        Assert.Equal(once, SatisPostSayisi());
    }

    [Fact]
    public void Yasal_kapilar_esikler_SUNUCUDAN_nakit_Uygun_kimlik_Altinda()
    {
        // Yıldız: yasal durum tek satır — "Yasal kurallar uygun · nakit <SUNUCUNUN haddi> sınırının altında".
        // Eski kartın kimlik eşiği ve KDV matrahı satırları Yıldız'da BİLEREK yok (tuval).
        var e = Ekran().YildizHazir();
        e.WaitForAssertion(() => Assert.Equal(
            $"{YildizTezgahSozlugu.YasalUygun("tr")} · {YildizTezgahSozlugu.NakitSinirAlti(Bicim.Tl(50_000m), "tr")}", YasalNot(e)));
    }

    /// <summary>
    /// TEMA TEMİZLİĞİ 17.10 (GC bulgusu, giderildi): Yıldız PRO tek ödemede sunucunun "işlem tutarı kuralı devrede" bilgisi
    /// eskiden yalnız PRO yan sütununda "Aşıldı" diye görünüyordu; Fark notu (YildizNot) artık proTevsik'i okur ve "uygun" DEMEZ.
    /// </summary>
    [Fact]
    public void Yasal_kapilar_sunucu_devrede_derse_Asildi()
    {
        tevsikDevrede = true;
        var e = KalemliPro();
        // önce sunucu yanıtı işlendi (not satırı yazıldı) — boş metinde yanlış yeşil olmaz
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(YasalNot(e))), TimeSpan.FromSeconds(5));
        // sunucu nakit kuralını bu fişte devrede sayıyorsa ekran "uygun" DEMEZ
        Assert.DoesNotContain(YildizTezgahSozlugu.YasalUygun("tr"), YasalNot(e));
    }

    /// <summary>Aynı kök (giderildi): sunucunun günlük KALAN nakit hakkı Yıldız PRO tek ödemede not satırına yansır.</summary>
    [Fact]
    public void Yasal_kapilar_gunluk_KALAN_nakit_hakkini_asan_fiste_Asildi()
    {
        tevsikKalan = 5_000m;   // sunucu: bu müşterinin bugünkü kalan nakit hakkı 5.000 (had yine sunucudan)
        var e = KalemliPro();   // fiş 10.000
        e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(YasalNot(e))), TimeSpan.FromSeconds(5));
        Assert.DoesNotContain(YildizTezgahSozlugu.YasalUygun("tr"), YasalNot(e));
    }

    [Fact]
    public void Yasal_kapilar_sunucu_yanit_vermezse_SAYISIZ_Bilinmiyor_ve_sebep()
    {
        // Yıldız: had bilinmiyorsa "uygun" DENMEZ, sayı UYDURULMAZ — not satırı yazılmaz.
        tevsikHad = null;
        var e = KalemliPro();
        e.WaitForAssertion(() => Assert.True(Istendi("/tevsik/gunluk")));
        Assert.Single(e.FindAll("#yildiz-tezgah-fark"));   // blok çizili — boş DOM değil
        Assert.DoesNotContain(YildizTezgahSozlugu.YasalUygun("tr"), e.Find("#yildiz-tezgah-fark").TextContent);
        Assert.DoesNotMatch(@"\d", YasalNot(e));   // istemci varsayılanı YOK
    }

    // TEMA TEMİZLİĞİ 17.10: Musteri_karti_bakiye_son_islem_Karti_ac_Pesinde_istek_yok kaldırıldı — sağ sütunun müşteri kartı
    // (#islem-pro-musteri: son işlem, has/TL bakiye, "Kartı aç") Yıldız'da BİLEREK yok; müşterinin TL durumu üst satırda
    // (S23FisMusteriEkranTestleri.S23E_01). "Peşin'de bakiye isteği yok" güvencesi aşağıdaki testte Yıldız yolunda.

    [Fact]
    public void Musteri_karti_Vazgec_ile_Pesine_donunce_Pesin_bakiyesi_ISTENMEZ()
    {
        var e = KalemliPro();
        Assert.False(Istendi("/cari/pm1/bakiye"));
        e.YildizMusteriSec("c1");
        e.WaitForAssertion(() => Assert.Contains("Hasan Kaya", e.Find("#yildiz-tezgah-ust").TextContent));
        e.Find("#yildiz-tezgah-vazgec").Click();
        e.WaitForAssertion(() => Assert.Contains("Peşin Müşteri", e.Find("#yildiz-tezgah-ust").TextContent));
        Assert.False(Istendi("/cari/pm1/bakiye"));
    }

    // TEMA TEMİZLİĞİ 17.10: Kisayollar_karti_dort_tus_ve_F2_F4_F8_gercekten_odaklar kaldırıldı — kısayollar kartı
    // (#islem-pro-kisayollar) Yıldız'da BİLEREK gizli (PTezgahTurKimlikTestleri.TK_13). F2 aşağıda Yıldız yolunda sınanır;
    // F4 (#islem-cari) ve F8 (#islem-odeme) hedefleri Yıldız satış tezgâhında çizili değildir → ÜRÜN BULGUSU (rapor).

    [Fact]
    public void TemaGC_Yildiz_PRO_F2_barkod_alanina_odaklar()
    {
        var e = Ekran().YildizHazir();
        int Odak(string hedef) => JSInterop.Invocations.Count(i => i.Identifier == "kuyumcuOdak.alana" && Equals(i.Arguments[0], hedef));
        e.WaitForElement("#yildiz-tezgah-giris #islem-barkod");
        e.WaitForAssertion(() => Assert.Equal(1, Odak("islem-barkod")));   // açılış odağı
        e.Find(".islem-sayfa").KeyDown(new KeyboardEventArgs { Key = "F2" });
        e.WaitForAssertion(() => Assert.Equal(2, Odak("islem-barkod")));
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
        e.YildizBol();
        e.Find("#islem-bacak-0-tutar").Input("6000");
        e.WaitForElement("#islem-bacak-1-tutar").Input("3000");   // SATIS-3: iki satır da elle → fark ≠ 0 → Kaydet kilitli (sebep Fark notunda)
        e.WaitForAssertion(() => Assert.True(e.YildizKaydetKilitli()));
        e.Find(".islem-sayfa").KeyDown(new KeyboardEventArgs { Key = "F9" });
        Assert.Equal(0, SatisPostSayisi());
        Assert.Empty(e.FindAll("#hata-bandi, #yildiz-tezgah-hata"));
    }

    [Fact]
    public void F9_dolu_fiste_KAYDEDER()
    {
        var e = KalemliPro();
        e.WaitForAssertion(() => Assert.False(e.YildizKaydetKilitli()), TimeSpan.FromSeconds(5));
        e.Find(".islem-sayfa").KeyDown(new KeyboardEventArgs { Key = "F9" });
        e.WaitForAssertion(() => Assert.Equal(1, SatisPostSayisi()), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Kaydet_ve_yazdir_etiketi_ve_sag_sutun_notu()
    {
        var e = Ekran().YildizHazir();
        Assert.Equal(SatisTuvalSozlugu.Tr.KaydetYazdir, e.Find("#yildiz-tezgah-kaydet").TextContent.Trim());
        // SATIS-2 (j): tuvalin tasarım notu ("Basit modda katlı olan her şey burada açık…") ürün metni DEĞİL — ekranda yok.
        // (Sağ sütunun "Yasal kapılar" kartı Yıldız'da yok; yasal durum Fark notunda — Yasal_kapilar_* testleri.)
        Assert.Empty(e.FindAll("#islem-pro-not"));
        Assert.DoesNotContain(SatisProSozlugu.Tr.ProNot, e.Markup);
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

    /// <summary>
    /// TEMA TEMİZLİĞİ 17.10: eski geniş tablo hücreleri (#islem-kalem-pro) Yıldız'da yok; AYNI güvence ("küçük has/gram
    /// ekranda KAYBOLMAZ — P-57: 0,001 g '0,00 g' diye BASILMAZ") Yıldız kalem satırının ayrıntısında ve toplam ayrıntısında
    /// sınanır. (GC bulgusu giderildi: YildizTezgahBicim.Gram
    /// artık FisHesap.GramSayi — en az iki, en çok üç hane.)
    /// </summary>
    [Fact]
    public void Kalem_tablosu_ve_toplam_serit_kucuk_has_ve_grami_KAYBETMEZ()
    {
        urunHas = "0.0001"; urunGram = "0.001";
        var e = KalemliPro();
        var ayrinti = e.Find("#yildiz-kalem-0 .yz-tezgah-ayrinti").TextContent.Trim();
        Assert.Contains("0,0001 g has", ayrinti);   // has
        Assert.Contains("0,001 g", ayrinti);        // gram (P-57: en çok üç hane, değer kaybolmaz)
        Assert.Contains("0,001 g", e.Find("#yildiz-tezgah-toplam-ayrinti").TextContent);
        Assert.Contains("0,0001 g has", e.Find("#yildiz-tezgah-toplam-ayrinti").TextContent);
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

    // ── Y-02 (20.10): ekranda kısayol yardımı — pro modda katlanır kapalı, basit modda yok; eşleme KisayolHedefi ile aynı ──
    [Fact]
    public void Kisayol_yardimi_pro_modda_katlanir_gelir_basit_modda_cizilmez()
    {
        var e = Ekran();
        var d = e.Find("#islem-kisayollar");
        Assert.False(d.HasAttribute("open"));                       // zorunlu önce: kapalı gelir
        var liste = e.Find("#islem-kisayollar-liste").TextContent;
        foreach (var tus in new[] { "F2", "F4", "F8", "F9", "Ctrl K" }) Assert.Contains(tus, liste);
        Assert.Contains("kaydet", liste);

        var basit = Ekran(ArayuzModuServisi.Basit);
        Assert.Empty(basit.FindAll("#islem-kisayollar"));
    }

    // ── Y-11 (20.10): fiyat teklifi — kur kilidi yokken teklif verilmez (POST yok), süre seçici açılır ──
    [Fact]
    public void Teklif_ver_kur_kilidi_yokken_POST_atmaz_kilit_ister()
    {
        var e = KalemliPro();
        e.WaitForElement("#yildiz-tezgah-teklif");
        e.Find("#yildiz-tezgah-teklif").Click();
        e.WaitForAssertion(() => Assert.Contains("kilit", e.Find("#yildiz-tezgah-bekleyen-not").TextContent, StringComparison.OrdinalIgnoreCase));
        Assert.False(Istendi("/bekleyen-fisler?teklif"));
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("/bekleyen-fisler"));
        Assert.NotEmpty(e.FindAll("#yildiz-kur-kilidi-sureler"));   // süre seçici açıldı
    }
}
