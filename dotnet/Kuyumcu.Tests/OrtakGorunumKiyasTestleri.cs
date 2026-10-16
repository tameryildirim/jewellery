using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ORTAK-1 — KIYAS DEVRİ 28.09 (ham/KIYAS-BULGULARI-2026-09-28.md § ORTAK-1)
//   KY1–KY2  Panel "Bekleyen iş": aynı başlıklı satırlar müşteri · tutar · gün ile
//            ayırt edilir, müşteri kartına bağlanır; müşteriye bağlı olmayan satır
//            eskisi gibi yalnız başlıktır.
//   KY3      Panel yükleyicisi ağ hatasında SONSUZ dönmez, bandına yazar (UX-S28).
//   KY4–KY5  Çift başlık: SayfaBasi başlığı üst barın rota adıyla AYNIYSA görsel
//            gizlenir (sınıf + ortak.css kuralı), farklıysa görünür kalır.
//   KY6      StatKart renkli sol şerit ortak.css'te nötr.
//   KY7      Devredilen sayfalarda (Panel, GiderPusulalari, UrunAgaci, Uyum,
//            UrunEtiketleri) işaretlemede emoji ikon YOK (Ikon bileşeni).
// Görünürlük kanıtı tarayıcı ölçümüdür (rapor); bunlar sözleşmeyi kilitler.
// ═══════════════════════════════════════════════════════════════════════════

public sealed class OrtakGorunumKiyasPanelTestleri : BunitContext
{
    private static readonly string Bugun = DateTime.Today.ToString("yyyy-MM-dd");
    private static readonly string UcGunSonra = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd");

    private readonly bool bildirimAgHatasi;

    private sealed class SahteApi(bool bildirimAgHatasi) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            if (bildirimAgHatasi && yol.EndsWith("/bildirimler"))
                throw new HttpRequestException("Connection refused (test.local:80)");
            var cevap = yol switch
            {
                var y when y.EndsWith("/raporlar/gunluk") => $$"""
                    {"tarih":"{{Bugun}}","satis":{"adet":0,"toplamTl":0,"toplamHas":0},
                     "alis":{"adet":0,"toplamTl":0,"toplamHas":0},"hurda":{"toplamHas":0,"odenenTl":0},
                     "netHasGramHareketi":0,"hareketSayisi":0,"kasalar":[]}
                    """,
                var y when y.EndsWith("/raporlar/cari-yaslandirma") => """
                    {"yaslananCariler":[],"ozetTl":{"0-30":0,"31-60":0,"61-90":0,"90+":0,"BELIRSIZ":0},
                     "toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}
                    """,
                var y when y.EndsWith("/raporlar/vade-takibi") => $$"""
                    {"ufukGun":7,"gecikenToplam":12345.67,"yaklasanToplam":5000,"vadesizToplam":0,
                     "ufukOtesiToplam":0,"acikToplam":17345.67,"cariler":[
                       {"cariId":"c1","cariAd":"Anadolu Holding A.Ş.","cariTur":"MUSTERI","telefon":null,
                        "gecikenTutar":12345.67,"yaklasanTutar":0,"vadesizTutar":0,"enEskiGecikmeGun":9,
                        "enYakinVade":null,"borclar":[]},
                       {"cariId":"c2","cariAd":"Karadeniz Kuyumculuk","cariTur":"MUSTERI","telefon":null,
                        "gecikenTutar":0,"yaklasanTutar":5000,"vadesizTutar":0,"enEskiGecikmeGun":null,
                        "enYakinVade":"{{UcGunSonra}}","borclar":[]}]}
                    """,
                var y when y.EndsWith("/bildirimler") => $$"""
                    {"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[
                      {"id":"b1","tur":"VADE","onem":"KRITIK","baslik":"Vadesi geçmiş veresiye borcu","govde":"Bir müşterinin borcu gecikmiş.",
                       "hedefRota":"/raporlar/vade-takibi","ilgiliVarlikTuru":"Cari","ilgiliVarlikId":"c1",
                       "olusturuldu":"{{Bugun}}T08:00:00Z","kapatildi":null,"kapatanKullaniciId":null,"kapatmaNotu":null},
                      {"id":"b2","tur":"VADE","onem":"UYARI","baslik":"Vadesi yaklaşan veresiye borcu","govde":"Bir müşterinin borcunun vadesi yaklaşıyor.",
                       "hedefRota":"/raporlar/vade-takibi","ilgiliVarlikTuru":"Cari","ilgiliVarlikId":"c2",
                       "olusturuldu":"{{Bugun}}T08:00:00Z","kapatildi":null,"kapatanKullaniciId":null,"kapatmaNotu":null},
                      {"id":"b3","tur":"SISTEM","onem":"BILGI","baslik":"Yedek alındı","govde":"Gece yedeği tamamlandı.",
                       "hedefRota":"/yedekler","ilgiliVarlikTuru":null,"ilgiliVarlikId":null,
                       "olusturuldu":"{{Bugun}}T08:00:00Z","kapatildi":null,"kapatanKullaniciId":null,"kapatmaNotu":null}]}
                    """,
                var y when y.EndsWith("/raporlar/aylik") => """{"gunler":[]}""",
                var y when y.Contains("/raporlar/islem-defteri") => $$"""
                    {"baslangic":"{{Bugun}}","bitis":"{{Bugun}}","toplamSatir":0,"ofset":0,"limit":5,
                     "dahaFazlasiVar":false,"ozet":{"tlGiris":0,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":0},
                     "storno":{"adet":0,"tutarTl":0},"satirlar":[]}
                    """,
                var y when y.EndsWith("/raporlar/kar-zarar") => """
                    {"hasilatTl":0,"maliyetTl":0,"karTl":0,"karMarjiYuzde":0,"hasilatHas":0,"maliyetHas":0,
                     "karHas":0,"satisAdedi":0,"maliyetiBilinenAdet":0,"maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM","uyari":null}
                    """,
                var y when y.EndsWith("/bankalar") => """{"hesaplar":[]}""",
                var y when y.EndsWith("/pozisyon") => """
                    {"anaMaden":"ALTIN",
                     "altin":{"maden":"ALTIN","vitrinGram":0,"hurdaGram":0,"cariAlacakGram":0,"cariBorcGram":0,
                              "netGram":0,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},
                     "digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,
                     "hesaplanmaAni":"2026-09-29T09:00:00Z","donemdenBagimsiz":true}
                    """,
                var y when y.EndsWith("/kur/kaynaklar") =>
                    """{"kaynaklar":["ALTINKAYNAK"],"secili":"ALTINKAYNAK","varsayilan":"ALTINKAYNAK"}""",
                var y when y.Contains("/tercihler/benim") => """{"tercihler":{}}""",
                var y when y.Contains("/uyum/") => """{"suresiGecmis":0,"yaklasan":0,"gecikmisSayisi":0,"yuksek":0}""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    public OrtakGorunumKiyasPanelTestleri() : this(false) { }

    private OrtakGorunumKiyasPanelTestleri(bool agHatasi)
    {
        bildirimAgHatasi = agHatasi;
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(bildirimAgHatasi)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<Microsoft.Extensions.Configuration.IConfiguration>(
            _ => new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        Services.AddScoped<HavaServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(new OturumKullanici(
            "u1", "Demo", "Kuyumcu", "d@ornek.com", "SAHIP", "s1", ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
    }

    /// <summary>/bildirimler isteği ağ hatasıyla düşen bağlam (KY3).</summary>
    public static OrtakGorunumKiyasPanelTestleri AgHatali() => new(true);

    internal IRenderedComponent<Kuyumcu.Web.Pages.Panel> Ciz()
    {
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForElement("#panel-sade");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#panel-iskelet-soru-bekleyen")), TimeSpan.FromSeconds(10));
        return e;
    }

    [Fact]
    public void KY1_bekleyen_is_ayni_baslikli_satirlar_musteri_tutar_gun_ile_ayrilir()
    {
        var e = Ciz();
        var T = PanelTuvalSozlugu.Al("tr");
        var S = PanelSadeSozlugu.Al("tr");

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-is-ayrinti-b1")), TimeSpan.FromSeconds(5));
        var b1 = e.Find("#panel-is-ayrinti-b1").TextContent;
        Assert.Contains("Anadolu Holding A.Ş.", b1);
        Assert.Contains(Bicim.TlKurus(12345.67m), b1);
        Assert.Contains(string.Format(T.VadeGectiIpucu, 9), b1);

        var b2 = e.Find("#panel-is-ayrinti-b2").TextContent;
        Assert.Contains("Karadeniz Kuyumculuk", b2);
        Assert.Contains(Bicim.TlKurus(5000m), b2);
        Assert.Contains(string.Format(S.GunKaldi, 3), b2);

        // İki satırın görünen metni artık FARKLI (önceden ikisi de yalnız başlıktı).
        Assert.NotEqual(e.Find("#panel-is-b1").TextContent.Trim(), e.Find("#panel-is-b2").TextContent.Trim());
    }

    [Fact]
    public void KY2_musteri_adi_kartina_baglanir_musterisiz_satirda_alt_satir_yok()
    {
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-is-cari-b1")), TimeSpan.FromSeconds(5));
        Assert.Equal("/cariler?cariId=c1", e.Find("#panel-is-cari-b1").GetAttribute("href"));
        Assert.Equal("/cariler?cariId=c2", e.Find("#panel-is-cari-b2").GetAttribute("href"));
        // Başlık bağlantısı (hedef rota) ve id'si AYNI kaldı.
        Assert.Equal("/raporlar/vade-takibi", e.Find("#panel-is-git-b1").GetAttribute("href"));
        Assert.Empty(e.FindAll("#panel-is-ayrinti-b3"));
        Assert.Equal("Yedek alındı", e.Find("#panel-is-git-b3").TextContent.Trim());
    }

    [Fact]
    public void KY3_bildirim_ag_hatasinda_iskelet_sonsuz_donmez_banda_yazar()
    {
        using var c = AgHatali();
        var e = c.Ciz();   // iskelet KALKMAZSA burada zaman aşımıyla kırılır
        e.WaitForAssertion(() =>
            Assert.Contains(OrtakGorunumSozlugu.Al("tr").PanelBaglantiHatasi,
                e.Find("#panel-soru-bekleyen-hata").TextContent), TimeSpan.FromSeconds(5));
        // Hata "kayıt yok"a çevrilmez (UX-S28): boş durum metni çıkmaz.
        Assert.DoesNotContain(PanelSadeSozlugu.Al("tr").BekleyenYok, e.Find("#panel-soru-bekleyen").TextContent);
    }
}

public sealed class OrtakGorunumKiyasSayfaBasiTestleri : BunitContext
{
    public OrtakGorunumKiyasSayfaBasiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    [Fact]
    public void KY4_ust_bardaki_adla_ayni_baslik_gorsel_gizli_sinifi_tasir_id_ve_h2_kalir()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/urunler");
        var dil = Services.GetRequiredService<DilServisi>();
        var ustBar = Kuyumcu.Web.Layout.RotaBasliklari.Baslik("/urunler", dil.T);

        var e = Render<SayfaBasi>(p => p.Add(x => x.Anahtar, "urunler").Add(x => x.Baslik, ustBar));
        var kok = e.Find("#sayfa-basi-urunler");
        Assert.Contains("sayfa-basi--baslik-ustte", kok.ClassList);
        var h2 = e.Find("h2#sayfa-basi-urunler-baslik");   // DOM'da, h2 ve id aynı
        Assert.Equal(ustBar, h2.TextContent.Trim());
    }

    [Fact]
    public void KY5_kayda_ozgu_farkli_baslik_gorunur_kalir()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/gider-pusulalari/p1/belge");
        var e = Render<SayfaBasi>(p => p.Add(x => x.Anahtar, "gider-pusulasi-belge")
            .Add(x => x.Baslik, "GP-2026-000042").Add(x => x.BaslikOgesiYok, true));
        Assert.DoesNotContain("sayfa-basi--baslik-ustte", e.Find("#sayfa-basi-gider-pusulasi-belge").ClassList);
    }

    [Fact]
    public void KY5b_ust_bar_adini_iceren_baslik_da_tekrar_sayilir()
    {
        // Canlı ölçüm (ONCE, 1280): /uyum şerit "Uyum (MASAK)" ↔ üst bar "Uyum".
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/uyum");
        var dil = Services.GetRequiredService<DilServisi>();
        var ustBar = Kuyumcu.Web.Layout.RotaBasliklari.Baslik("/uyum", dil.T);
        var e = Render<SayfaBasi>(p => p.Add(x => x.Anahtar, "uyum").Add(x => x.Baslik, ustBar + " (MASAK)"));
        Assert.Contains("sayfa-basi--baslik-ustte", e.Find("#sayfa-basi-uyum").ClassList);
    }

    [Fact]
    public void KY5c_sayfa_farkli_sozcukle_tekrari_bildirirse_gizlenir_bildirmezse_gorunur()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/urunler/etiketler");
        var bildirmeyen = Render<SayfaBasi>(p => p.Add(x => x.Anahtar, "a").Add(x => x.Baslik, "Barkod kâğıdı"));
        Assert.DoesNotContain("sayfa-basi--baslik-ustte", bildirmeyen.Find("#sayfa-basi-a").ClassList);
        var bildiren = Render<SayfaBasi>(p => p.Add(x => x.Anahtar, "b").Add(x => x.Baslik, "Barkod kâğıdı").Add(x => x.UstBardaGizle, true));
        Assert.Contains("sayfa-basi--baslik-ustte", bildiren.Find("#sayfa-basi-b").ClassList);
    }
}

public sealed class OrtakGorunumKiyasSaflik
{
    private static string OrtakCss() => CssKaynak.YorumsuzCss(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "ortak.css"));

    [Fact]
    public void KY4b_ortak_css_ayni_basligi_gorsel_gizler_eylemi_saga_yaslar()
    {
        var css = OrtakCss();
        // display:none — 1 px görsel-gizli kutu tur ölçerinde "taşan başlık" sayılıyordu (29.09).
        Assert.Matches(@"\.sayfa-basi\.sayfa-basi--baslik-ustte > \.sayfa-basi-metin > \.sayfa-basi-baslik\s*\{\s*display:\s*none;\s*\}", css);
        Assert.Matches(@"\.sayfa-basi\.sayfa-basi--baslik-ustte > \.sayfa-basi-eylem\s*\{\s*margin-left:\s*auto;", css);
    }

    [Fact]
    public void KY6_stat_kart_renkli_sol_serit_notr()
    {
        var css = OrtakCss();
        Assert.Matches(@"\.kart\.stat-kart,\s*\.kart\.stat-kart\.yesil,\s*\.kart\.stat-kart\.kirmizi,\s*\.kart\.stat-kart\.mavi\s*\{\s*border-left:\s*1px solid var\(--ayrac\);", css);
    }

    [Fact]
    public void KY8_gider_form_kartlari_ic_bosluk_alir_hucreler_kirilmaz()
    {
        var css = OrtakCss();
        Assert.Matches(@"\.kart > \.form-dikey:has\(#gd-kaydet\),\s*\.kart > \.form-dikey:has\(#gd-kat-ad\),\s*\.kart > \.form-dikey:has\(#gd-v-kaynak\),\s*\.kart > #gd-virman-aciklama\s*\{\s*margin:\s*0;\s*padding:\s*1rem 1\.5rem;", css);
        var gider = CssKaynak.Metin("Kuyumcu.Web", "Pages", "Gider.razor");
        // PAKET GOC-A: listeler ortak ızgarada — hücre sınıfları kolon tanımlarında AYNI (hucre-ad), değerler AYNI.
        Assert.Contains("HucreSinif = _ => \"hucre-ad\", Deger = g => g.KategoriAdi ?? \"—\"", gider);
        Assert.Contains("HucreSinif = _ => \"metin-kucuk hucre-ad\",\n                Deger = g => StornoOnekiSozlugu.Goster(", gider);
        Assert.Contains("HucreSinif = _ => \"hucre-ad\", Deger = k => k.Ad,", gider);
        Assert.Contains("HucreSinif = _ => \"metin-kucuk hucre-ad\", Deger = g => g.KasaAdi ?? \"—\"", gider);
        var kul = CssKaynak.Metin("Kuyumcu.Web", "Pages", "Kullanicilar.razor");
        // KESIF-P6 tur 2 (hakem 01.10): e-posta ayrı sütun değil, ad hücresinde kesilen ikinci satır
        // (1440'ta tablo kaba sığar); hücreler q10-yigin etiketi (data-etiket) taşır — sınıf AYNI.
        Assert.Contains("<span class=\"k-satir-eposta metin-soluk metin-kucuk\" title=\"@k.Email\">@k.Email</span>", kul);
        // PAKET GOC-D: liste ortak ızgarada — şube hücresinin sınıfı kolon tanımında AYNI; data-etiket'i ızgara yazar.
        Assert.Contains("HucreSinif = _ => \"metin-soluk hucre-tek-satir\", Deger = k => SubeAd(k.SubeId)", kul);
    }

    [Fact]
    public void KY10_vezne_eylem_hucresi_tablo_hucresi_kalir_kisa_hucreler_kirilmaz()
    {
        // 29.09 tur: <td class="rozet-satir"> (display:flex) hücreyi tablo hücresi olmaktan
        // çıkarıyor, katlı kilit satırı düğmelere biniyordu (K9).
        var vezne = CssKaynak.Metin("Kuyumcu.Web", "Pages", "Vezne.razor");
        Assert.DoesNotContain("<td class=\"rozet-satir\">", vezne);
        Assert.Contains("<div class=\"rozet-satir vz-eylem-hucre\">", vezne);
        Assert.Matches(@"\.vz-eylem-hucre > \.vz-kilit-ayrinti\s*\{\s*flex-basis:\s*100%;", OrtakCss());
        Assert.Matches(@"\.vz-eylem-hucre\s*\{\s*min-width:\s*15rem;", OrtakCss());
        // 29.09 canlı ölçüm: --vurgu-metin VURGU ZEMİNİ ÜSTÜ yazı rengidir (#fdfdff) — açık kart
        // zemininde "Neden değiştirilemez?" görünmüyordu. Bağlantı rengi --vurgu.
        Assert.Matches(@"\.vz-kilit-ayrinti > summary\s*\{\s*cursor:\s*pointer;\s*color:\s*var\(--vurgu\);", OrtakCss());
        // PAKET GOC-C: sipariş listesi ortak ızgarada — tür hücresi sınıfı kolon tanımında AYNI (hucre-tek-satir), değer AYNI.
        Assert.Contains("HucreSinif = _ => \"hucre-tek-satir\", Deger = x => TurMetni(x.Tur)",
            CssKaynak.Metin("Kuyumcu.Web", "Pages", "Siparisler.razor"));
        // PAKET GOC-D: kullanıcı listesi ortak ızgarada — ad hücresi sınıfı kolon tanımında AYNI (hucre-tek-satir).
        Assert.Contains("HucreSinif = _ => \"hucre-vurgu hucre-tek-satir\"", // KESIF-P6 tur 2: yığın etiketi
            CssKaynak.Metin("Kuyumcu.Web", "Pages", "Kullanicilar.razor"));
        // KESIF-P5: hücreler yığın etiketi (data-etiket, q10-yigin) taşır — sınıf AYNI.
        // PAKET GOC-A: liste ortak ızgarada — hücre sınıfı kolon tanımında AYNI (hucre-ad), değer AYNI.
        Assert.Matches(@"Anahtar = ""cari"".*HucreSinif = _ => ""hucre-ad"",\s*Deger = f => f\.CariAd \?\? ""—""", vezne);
        // PAKET GOC-B: koşum geçmişi ortak ızgarada — hücre sınıfı kolon tanımında AYNI, değer AYNI.
        Assert.Matches(@"HucreSinif = _ => ""metin-kucuk hucre-tek-satir"", Deger = c => IsAdi\(c\.IsKodu\)",
            CssKaynak.Metin("Kuyumcu.Web", "Pages", "Bildirimler.razor"));
    }

    /// <summary>
    /// Canlı ölçüm (29.09): şerit başlığı üst bar adını FARKLI sözcükle tekrarlayan sayfalar
    /// ("Etiketler"/"Ürün Etiketleri", "Sipariş ve tamir"/"Siparişler", "Vade takibi"/"Gecikmiş
    /// Alacaklar", PRO "Kasa kuyruğu"/"Vezne") tekrarı kendileri bildirir.
    /// </summary>
    [Theory]
    [InlineData("UrunEtiketleri.razor", "<SayfaBasi Anahtar=\"etiketler\"")]
    [InlineData("Siparisler.razor", "<SayfaBasi Anahtar=\"siparisler\"")]
    [InlineData("VadeTakibi.razor", "<SayfaBasi Anahtar=\"vade-takibi\"")]
    [InlineData("Vezne.razor", "<SayfaBasi Anahtar=\"vezne\"")]
    public void KY9_farkli_sozcukle_tekrar_eden_sayfa_basligi_ust_barda_gizlenir(string dosya, string etiket)
    {
        var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Pages", dosya);
        var bas = kaynak.IndexOf(etiket, StringComparison.Ordinal);
        Assert.True(bas >= 0, dosya);
        var son = kaynak.IndexOf('>', bas);
        Assert.Contains("UstBardaGizle=\"true\"", kaynak[bas..son]);
    }

    /// <summary>Emoji ve dingbat aralıkları (UX-5: ikon yalnız Ikon bileşeniyle).</summary>
    private static readonly Regex Emoji = new(@"[☀-➿⭐✅]|[\uD83C-\uD83E][\uDC00-\uDFFF]");

    [Theory]
    [InlineData("Panel.razor")]
    [InlineData("GiderPusulalari.razor")]
    [InlineData("UrunAgaci.razor")]
    [InlineData("Uyum.razor")]
    [InlineData("UrunEtiketleri.razor")]
    public void KY7_devredilen_sayfalarda_isaretlemede_emoji_yok(string dosya)
    {
        // Yorum blokları (@* … *@) ve // satırları geliştirici notudur, ekrana çıkmaz.
        var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Pages", dosya);
        var yorumsuz = Regex.Replace(kaynak, @"@\*.*?\*@", "", RegexOptions.Singleline);
        var bulunan = yorumsuz.Split('\n')
            .Select((s, i) => (s, i))
            .Where(t => !t.s.TrimStart().StartsWith("//", StringComparison.Ordinal) && !t.s.TrimStart().StartsWith("///", StringComparison.Ordinal))
            .Where(t => Emoji.IsMatch(t.s))
            .Select(t => t.s.Trim())
            .ToList();
        Assert.True(bulunan.Count == 0, $"{dosya}: emoji ikon {bulunan.Count}: " + string.Join(" | ", bulunan));
    }

    [Fact]
    public void KY11_kullanicilar_siz_rozeti_tabloda_ad_ile_ayni_boyda()
    {
        // 29.09 devir turu ölçümü (/kullanicilar 960–1440, 10 durum): "(siz)" küçük yazıyla aynı
        // satırda farklı üst kenara oturuyordu (ölçer K3 kırık; gözle taban kayması).
        Assert.Matches(@"\.tablo-sar td > #k-siz\s*\{\s*font-size:\s*inherit;\s*\}", OrtakCss());
        Assert.Contains("id=\"k-siz\"", CssKaynak.Metin("Kuyumcu.Web", "Pages", "Kullanicilar.razor"));
    }

    [Fact]
    public void KY12_panel_dar_kart_alt_satiri_kap_sorgusuyla_kuculur_genis_kartta_degismez()
    {
        // 29.09 devir turu ölçümü (Panel BASİT 960): "Bugün henüz satış yok." 157 px'lik
        // alt satıra 1 px sığmıyor, iki satıra kırılıyordu. Yalnız dar kapta (≤170 px) küçülür.
        Assert.Matches(@"@container \(max-width:\s*170px\)\s*\{\s*\.panel-sade \.panel-soru > \.panel-soru-alt\s*\{\s*font-size:\s*calc\(var\(--yazi-govde\) \* \.7778\);\s*\}\s*\}", OrtakCss());
        Assert.Matches(@"\.panel-soru:has\(> \.panel-soru-sayi\)\s*\{\s*container-type:\s*inline-size;", OrtakCss());
    }
}
