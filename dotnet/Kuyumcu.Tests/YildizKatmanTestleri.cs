using System.Net;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Kabuk;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-KATMAN — ortak katmanlar (ModalPencere / Cekmece / perde / sayfa başlığı) Yıldız'da
// tuval dilinde + 5A kalanları (üst menü seçili öğe, Ürün ağacı adları, Etiketler "Ürün seçin").
// Görsel eşitliğin asıl kanıtı başsız Chrome ölçümüdür (betikler/tur/yildiz-katman-tara.mjs, rapor § 2);
// bu sınıf sözleşmeyi kilitler: seçili öğe eşlemesi (tuval a.on), sayfa başlığı dalı yalnız Yıldız'da,
// katman CSS'inin kapsamı ve değerleri (sağ çekmece, saydam perde, köşe/gölge yok), etiket seçim düğmesi.
// ════════════════════════════════════════════════════════════════════════════
public sealed class YildizKatmanTestleri : BunitContext
{
    private string sema = "yildiz";

    private sealed class SahteApi(YildizKatmanTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/yetkiler/benim") => """{"moduller":["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","kur"]}""",
                var y when y.EndsWith("/tercihler/benim") => """{"tercihler":{},"mesaj":"ok"}""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"k1","tenantId":"t1","altinHasFiyati":6877,"usdTry":41.95,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-10-08T09:02:00Z","ayarFiyatlari":[{"ayarId":"K22","label":"22","milyem":916,"gramFiyati":5555,"hasFiyati":6877}]}""",
                var y when y.EndsWith("/gorunum") => $$"""{"sema":"{{t.sema}}","yaziOlcegi":"18","temaModu":"karanlik"}""",
                var y when y.EndsWith("/bildirimler/sayi") => """{"sayi":0,"enYuksekOnem":null}""",
                var y when y.Contains("/subeler") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    public YildizKatmanTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private void Hazirla(string rota, string rol = "SAHIP")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Hakan", "Tunalı", "h@ornek.com", rol, "s1", ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        Services.GetRequiredService<GorunumServisi>().YukleAsync().GetAwaiter().GetResult();
        Services.GetRequiredService<NavigationManager>().NavigateTo(rota);
    }

    private static string KatmanBlogu()
    {
        var ham = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tema-yildiz.css");
        var a = ham.IndexOf("/* YILDIZ-KATMAN */", StringComparison.Ordinal);
        var b = ham.IndexOf("/* YILDIZ-KATMAN sonu */", StringComparison.Ordinal);
        Assert.True(a >= 0 && b > a, "YILDIZ-KATMAN bloğu yok");
        return Regex.Replace(ham[a..b], @"/\*.*?\*/", "", RegexOptions.Singleline);
    }

    /// <summary>Bloktaki bir kuralın gövdesi (seçici metni birebir).</summary>
    private static string Kural(string blok, string secici)
    {
        var i = blok.IndexOf(secici + " {", StringComparison.Ordinal);
        Assert.True(i >= 0, "kural yok: " + secici);
        var a = blok.IndexOf('{', i);
        var b = blok.IndexOf('}', a);
        return blok[(a + 1)..b];
    }

    // ── Saf: üst menü seçili öğe (tuval Y_*.dc.html a.on — ekranın menü grubu) ──────────────

    [Theory]
    [InlineData("/", "bugun")]
    [InlineData("/islemler", "tezgah")]
    [InlineData("/islemler/abc", "tezgah")]
    [InlineData("/emanet", "bugun")]
    [InlineData("/siparisler", "bugun")]
    [InlineData("/bildirimler", "bugun")]
    [InlineData("/cariler", "musteriler")]
    [InlineData("/cariler/c1", "musteriler")]
    [InlineData("/urunler", "musteriler")]
    [InlineData("/urunler/agac", "musteriler")]
    [InlineData("/urunler/etiketler", "musteriler")]
    [InlineData("/fiyatlandirma", "musteriler")]
    [InlineData("/sayim", "musteriler")]
    [InlineData("/satin-alma", "musteriler")]
    [InlineData("/kasalar", "kasa")]
    [InlineData("/vezne", "kasa")]
    [InlineData("/pos/tahsilatlar", "kasa")]
    [InlineData("/evraklar/takvim", "kasa")]
    [InlineData("/kur", "kasa")]
    [InlineData("/gider", "kasa")]
    [InlineData("/hurda", "kasa")]
    [InlineData("/faturalar", "tum-ekranlar")]
    [InlineData("/gider-pusulalari", "tum-ekranlar")]
    [InlineData("/uyum", "tum-ekranlar")]
    [InlineData("/raporlar/kar-zarar", "tum-ekranlar")]
    [InlineData("/raporlar/kasa", "tum-ekranlar")]
    [InlineData("/kullanicilar", "tum-ekranlar")]
    [InlineData("/ayarlar", "tum-ekranlar")]
    [InlineData("/ekranlar", "tum-ekranlar")]
    [InlineData("/bilinmeyen-sayfa", "tum-ekranlar")]
    public void YK_01_secili_ust_menu_sozcugu_tuvaldeki_grup_eslemesidir(string yol, string beklenen)
    {
        Assert.Equal(beklenen, YildizKabuk.SeciliKimlik(yol, _ => true));
    }

    [Fact]
    public void YK_02_grubun_sozcugu_gorunmuyorsa_Tum_ekranlar_secilir()
    {
        // Yetkisiz kullanıcı "Müşteriler"i görmez → /urunler'de seçim yine VAR ("Tüm ekranlar").
        Assert.Equal("tum-ekranlar", YildizKabuk.SeciliKimlik("/urunler", o => o.Kimlik != "musteriler"));
        Assert.Equal("tum-ekranlar", YildizKabuk.SeciliKimlik("/kasalar", o => o.Kimlik is "bugun" or "tum-ekranlar"));
    }

    [Theory]
    [InlineData("/urunler", "yildiz-menu-musteriler")]
    [InlineData("/emanet", "yildiz-menu-bugun")]
    [InlineData("/hurda", "yildiz-menu-kasa")]
    [InlineData("/yetkiler", "yildiz-menu-tum-ekranlar")]
    public async Task YK_03_ust_menu_her_ekranda_tek_secili_ogeyi_aria_current_ile_cizer(string rota, string id)
    {
        Hazirla(rota);
        await Services.GetRequiredService<YetkiServisi>().HazirlaAsync();
        var e = Render<YildizUstMenu>();
        e.WaitForAssertion(() =>
        {
            var secili = e.FindAll("#yildiz-menu a[aria-current=page]");
            Assert.Single(secili);
            Assert.Equal(id, secili[0].Id);
            Assert.Contains("aktif", secili[0].ClassName);
        });
    }

    // ── bUnit: sayfa başlığı (denetim Y17/O16) ───────────────────────────────────────────

    [Fact]
    public void YK_10_Yildizda_yuzu_olmayan_ekran_tuval_basligini_tasir()
    {
        Hazirla("/kullanicilar");
        var e = Render<UstBar>();
        var bas = e.Find("#ust-bar-yz-baslik");
        Assert.Equal("64", bas.GetAttribute("data-boy"));
        Assert.Equal("Yönetim · kim giriyor, hangi yetkiyle", e.Find("#ust-bar-yz-ust").TextContent);
        Assert.Equal("Kullanıcılar", bas.QuerySelector("h1")!.TextContent);
        Assert.Single(e.FindAll("h1"));   // tek başlık
    }

    [Fact]
    public void YK_11_ozet_kalibi_40_ve_tuval_basligi_Uyum()
    {
        Hazirla("/uyum");
        var e = Render<UstBar>();
        Assert.Equal("40", e.Find("#ust-bar-yz-baslik").GetAttribute("data-boy"));
        Assert.Equal("Uyum", e.Find("#ust-bar-yz-baslik h1").TextContent);
        Assert.Equal("Belge ve uyum · MASAK · yasal bildirim", e.Find("#ust-bar-yz-ust").TextContent);
    }

    [Fact]
    public async Task YK_12_listede_olmayan_rotada_eski_baslik_AYNEN()
    {
        // TEMA TEMİZLİĞİ 17.10: "öteki şemada eski başlık" yarısı kaldırıldı — başka şema yok.
        await Services.GetRequiredService<GorunumServisi>().YukleAsync();
        Services.GetRequiredService<NavigationManager>().NavigateTo("/kasalar");   // kendi Yıldız yüzü var → başlık o yüzde
        var y = Render<UstBar>();
        Assert.Empty(y.FindAll("#ust-bar-yz-baslik"));
        Assert.NotEmpty(y.FindAll("header.ust-bar > h1"));
    }

    [Fact]
    public void YK_13_baslik_sozlugu_TR_EN_tam_ve_tuval_metni()
    {
        string[] yollar = ["/faturalar", "/irsaliyeler", "/uyum", "/yasal-sinirlar", "/raporlar", "/raporlar/kar-zarar",
            "/raporlar/vade-takibi", "/raporlar/islem-defteri", "/raporlar/satis-analizi", "/kullanicilar", "/isletme",
            "/yetkiler", "/denetim-izi", "/kurulum", "/veri-disa-aktarim", "/veri-ice-aktarim", "/yedekler", "/kilavuz",
            "/ayarlar", "/satin-alma"];
        foreach (var y in yollar)
        {
            var tr = YildizSayfaBasligiSozlugu.Ust(y, "tr");
            var en = YildizSayfaBasligiSozlugu.Ust(y, "en");
            Assert.False(string.IsNullOrWhiteSpace(tr), y);
            Assert.False(string.IsNullOrWhiteSpace(en), y);
            Assert.NotEqual(tr, en);
        }
        // Kendi Yıldız yüzü olan ekranlar bu başlığı ALMAZ (iki başlık olmaz).
        foreach (var y in new[] { "/", "/emanet", "/urunler", "/kasalar", "/kur", "/raporlar/kasa", "/gider-pusulalari" })
            Assert.Null(YildizSayfaBasligiSozlugu.Ust(y, "tr"));
        Assert.Equal("Raporlar · bütün fişler", YildizSayfaBasligiSozlugu.Ust("/raporlar/islem-defteri", "tr"));
        Assert.Equal(64, YildizKabuk.BaslikBoyu("/kullanicilar"));
        Assert.Equal(40, YildizKabuk.BaslikBoyu("/raporlar/kar-zarar"));
        Assert.Equal(40, YildizKabuk.BaslikBoyu("/raporlar/satis-analizi"));
    }

    // ── CSS: katman bloğu (tuval dili, kapsam, öteki şemalar) ─────────────────────────────

    [Fact]
    public void YK_20_katman_blogu_yalniz_Yildiz_kapsaminda_ve_sabit_renksiz()
    {
        var blok = KatmanBlogu();
        foreach (var satir in blok.Split('\n').Where(s => s.Contains('{') && !s.TrimStart().StartsWith("@media", StringComparison.Ordinal)))
            Assert.StartsWith("html:root[data-sema=\"yildiz\"]", satir.TrimStart());
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b", blok);
        Assert.DoesNotMatch(@"rgba?\(", blok);
    }

    [Fact]
    public void YK_21_perde_saydam_ve_modal_sag_cekmece()
    {
        var blok = KatmanBlogu();
        Assert.Contains("background: transparent", Kural(blok, "html:root[data-sema=\"yildiz\"] body :is(.modal-arka, .cekmece-arka):not(.yz *):not(#_)"));
        var arka = Kural(blok, "html:root[data-sema=\"yildiz\"] body .modal-arka:not(.yz *):not(#_)");
        Assert.Contains("justify-content: flex-end", arka);
        Assert.Contains("align-items: stretch", arka);
        var panel = Kural(blok, "html:root[data-sema=\"yildiz\"] body .modal-panel:not(.yz *):not(#_)");
        Assert.Contains("height: 100%", panel);
        Assert.Contains("margin: 0", panel);
        Assert.Contains("max-height: none", panel);
    }

    [Fact]
    public void YK_22_panel_kartsiz_kil_cizgi_kose_ve_golge_yok()
    {
        var panel = Kural(KatmanBlogu(), "html:root[data-sema=\"yildiz\"] body :is(.modal-panel, .cekmece):not(.yz *):not(#_)");
        Assert.Contains("border-radius: 0", panel);
        Assert.Contains("box-shadow: none", panel);
        Assert.Contains("border-left: 1px solid var(--ayrac)", panel);
        Assert.Contains("background: var(--yuzey-govde)", panel);
    }

    [Fact]
    public void YK_23_katman_basligi_ve_alt_serit_tuval_kalibi()
    {
        var blok = KatmanBlogu();
        var h = Kural(blok, "html:root[data-sema=\"yildiz\"] body :is(.modal-baslik, .cekmece-bas) :is(h2, h3):not(.yz *):not(#_)");
        Assert.Contains("font-size: 22px", h);
        Assert.Contains("font-weight: 300 !important", h);
        Assert.Contains("letter-spacing: -0.01em", h);
        Assert.Contains("background-color: var(--yuzey-govde)", Kural(blok, "html:root[data-sema=\"yildiz\"] body :is(.modal-alt, .cekmece-alt):not(.yz *):not(#_)"));
        Assert.Contains("order: -1", Kural(blok, "html:root[data-sema=\"yildiz\"] body :is(.modal-alt, .cekmece-alt) :is(.dugme-birincil, .btn-birincil):not(.yz *):not(#_)"));
    }

    [Fact]
    public void YK_24_sayfa_basligi_tuval_olcusu()
    {
        var blok = KatmanBlogu();
        var ust = Kural(blok, "html:root[data-sema=\"yildiz\"] .yz-sayfa-baslik > .yz-sayfa-baslik-ust:not(#_)");
        Assert.Contains("font-size: 12px", ust);
        Assert.Contains("font-weight: 500", ust);
        Assert.Contains("letter-spacing: .22em", ust);
        Assert.Contains("text-transform: uppercase", ust);
        var h1 = Kural(blok, "html:root[data-sema=\"yildiz\"] .yz-sayfa-baslik > h1:not(#_)");
        Assert.Contains("font-size: 64px", h1);
        Assert.Contains("font-weight: 200 !important", h1);
        Assert.Contains("letter-spacing: -0.035em", h1);
        Assert.Contains("font-size: 40px; font-weight: 300 !important", blok);
        Assert.Contains(".yz-sayfa-baslik[data-boy=\"40\"] { gap: 24px; }", blok);
    }

    [Fact]
    public void YK_25_altligi_olmayan_pencerede_birincil_eylem_satiri_yapisir()
    {
        var k = Kural(KatmanBlogu(), "html:root[data-sema=\"yildiz\"] body :is(.modal-govde, .cekmece-govde) > :is(.form-dikey, form) > :has(> :is(.btn-birincil, .dugme-birincil)):not(.yz *):not(#_)");
        Assert.Contains("position: sticky", k);
        Assert.Contains("bottom: -24px", k);
        Assert.Contains("fieldset:not(.yz *):not(#_) { margin: 0; padding: 16px 0 0; border: 0; border-top: 1px solid var(--ayrac); border-radius: 0; }", KatmanBlogu());
        Assert.Contains("background-color: var(--yuzey-govde)", k);
        Assert.Contains("border-radius: 0; background: transparent;", Kural(KatmanBlogu(), "html:root[data-sema=\"yildiz\"] body :is(.modal-panel, .cekmece) details:not(.yz *):not(#_)"));
    }

    // ── 5A kalanları ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void YK_30_etiketler_urun_yokken_deger_secim_listesini_acar()
    {
        var u = new List<YildizEtiketUrunu> { new("u1", "22 Ayar Burma Bilezik", "8698256000046", "916", "15,20") };
        var b = new YildizEtiketBaglami
        {
            Ust = "Müşteri ve stok · barkodlu ürün etiketi", Baslik = "Etiketler",
            Urunler = u, Secili = [], Kopya = 1, KopyaEnCok = 100, YazdirKilit = "Önce listeden ürün seçin.",
        };
        var e = Render<YildizEtiketler>(p => p.Add(x => x.B, b));
        var deger = e.Find("#yildiz-etiket-urun-deger");
        Assert.Equal("BUTTON", deger.TagName);
        Assert.Equal("Ürün seçin", deger.TextContent);
        Assert.Equal("false", deger.GetAttribute("aria-expanded"));
        Assert.Empty(e.FindAll("#yildiz-etiket-secim"));
        deger.Click();
        Assert.NotEmpty(e.FindAll("#yildiz-etiket-secim"));
        Assert.Equal("true", e.Find("#yildiz-etiket-urun-deger").GetAttribute("aria-expanded"));
        Assert.NotEmpty(e.FindAll("#yildiz-etiket-sec-u1"));
    }

    [Fact]
    public void YK_31_urun_agaci_cekmecesi_grup_der_kategori_demez()
    {
        Assert.Equal("Grup adı", YildizKatmanSozlugu.GrupAdi("tr"));
        Assert.Equal("Group name", YildizKatmanSozlugu.GrupAdi("en"));
        Assert.Equal("Üst grup", YildizKatmanSozlugu.UstGrup("tr"));
        Assert.Equal("Parent group", YildizKatmanSozlugu.UstGrup("en"));
        var razor = CssKaynak.Metin("Kuyumcu.Web", "Pages", "UrunAgaci.razor");
        // Tema temizliği (17.10): tek şema — çekmece her zaman "grup" dilindedir; eski "kategori" metinleri çizilmez.
        Assert.Contains("YildizAgacSozlugu.YeniGrup(Dil.Dil)", razor);
        Assert.Contains("YildizKatmanSozlugu.GrupAdi(Dil.Dil)", razor);
        Assert.Contains("YildizKatmanSozlugu.UstGrup(Dil.Dil)", razor);
        Assert.Contains("YildizKatmanSozlugu.DonguUyarisi(Dil.Dil)", razor);
        // Çekmece (kategori formu) eski "kategori" etiketlerini taşımaz. ("+ Yeni kategori" düğmesi özgün gövdede
        // "Tüm ayrıntılar" arkasında yaşar — o, çekmecenin başlığı değildir.)
        foreach (var eski in new[] { "Dil.T.UrunAgaci.KategoriAdi", "Dil.T.UrunAgaci.UstKategori", "Dil.T.UrunAgaci.DonguUyarisi", "YildizMi" })
            Assert.DoesNotContain(eski, razor);
        Assert.Contains("Baslik=\"@(kDuzenlenenId is null ? (YildizAgacSozlugu.YeniGrup(Dil.Dil)) : Dil.T.Ortak.Duzenle)\"", razor);
        Assert.DoesNotContain("kategori", YildizKatmanSozlugu.DonguUyarisi("tr"));
    }
    // ── Tur 2 (tasarım hakemi KRİTİK): katman eylem satırı TEK KALIP — b1 hap 56 · b2 altı çizili "Vazgeç" ─────────

    private const string Satir = "html:root[data-sema=\"yildiz\"] body :is(.modal-panel, .cekmece):not(.yz-tezgah *) :has(> :is(.btn-birincil, .dugme-birincil, .btn-tehlike, .dugme-tehlike)):not(form, fieldset, .form-dikey, .modal-govde, .cekmece-govde, .modal-panel, .cekmece)";

    [Fact]
    public void YK_40_katman_eylem_satiri_tuval_b1_b2_kalibi_tek_olcu()
    {
        var blok = KatmanBlogu();
        var satir = Kural(blok, Satir + ":not(#_)");
        Assert.Contains("gap: 16px 40px", satir);
        Assert.Contains("justify-content: flex-start", satir);
        var ortak = Kural(blok, Satir + " > :is(.btn, .dugme, button):not(#_)");
        Assert.Contains("flex: 0 0 auto !important", ortak);       // esnek / esit-pay / flex:1 nötr
        Assert.Contains("height: 56px", ortak);
        Assert.Contains("font-size: 16px", ortak);
        Assert.Contains("letter-spacing: .02em", ortak);
        Assert.Contains("width: auto", ortak);
        var b1 = Kural(blok, Satir + " > :is(.btn-birincil, .dugme-birincil, .btn-tehlike, .dugme-tehlike):not(#_)");
        Assert.Contains("order: -1", b1);
        Assert.Contains("padding: 0 32px", b1);
        Assert.Contains("border-radius: 999px !important", b1);
        Assert.Contains("background: var(--metin)", b1);
        Assert.Contains("color: var(--yuzey-govde)", b1);
        Assert.Contains("font-weight: 500", b1);
        var b2 = Kural(blok, Satir + " > :is(.btn, .dugme, button):not(.btn-birincil):not(.dugme-birincil):not(.btn-tehlike):not(.dugme-tehlike):not(#_)");
        Assert.Contains("padding: 0 4px", b2);
        Assert.Contains("border-bottom: 1px solid var(--kenarlik)", b2);
        Assert.Contains("border-radius: 0 !important", b2);
        Assert.Contains("background: transparent", b2);
        Assert.Contains("font-weight: 400", b2);
    }

    [Fact]
    public void YK_41_cekmece_ikincil_metni_Yildizda_Vazgec_otekinde_Iptal()
    {
        Hazirla("/kasalar");
        var y = Render<Kuyumcu.Web.Bilesenler.Cekmece>(p => p.Add(x => x.Varlik, "yk").Add(x => x.Baslik, "B").Add(x => x.Acik, true));
        Assert.Equal("Vazgeç", y.Find("#cekmece-yk-iptal").TextContent.Trim());
        var verilen = Render<Kuyumcu.Web.Bilesenler.Cekmece>(p => p.Add(x => x.Varlik, "yv").Add(x => x.Baslik, "B").Add(x => x.Acik, true).Add(x => x.IptalMetni, "Kapat"));
        Assert.Equal("Kapat", verilen.Find("#cekmece-yv-iptal").TextContent.Trim());
    }

    [Fact]
    public void YK_42_YildizVazgec_Yildizda_Vazgec_otekinde_Ortak_Iptal_AYNEN()
    {
        Hazirla("/kullanicilar");
        Assert.Equal("Vazgeç", Render<YildizVazgec>().Markup.Trim());
        Assert.Equal("Cancel", YildizKatmanSozlugu.Vazgec("en"));
        Assert.Equal("Vazgeç", YildizKatmanSozlugu.Vazgec("tr"));
    }

    // TEMA TEMİZLİĞİ 17.10: YK_41b ve YK_42b ("öteki şemada İptal AYNEN") kaldırıldı — başka şema yok; çekmece ve
    // YildizVazgec her zaman "Vazgeç" der (YK_41/YK_42 sınar).

    [Fact]
    public void YK_43_sayfalardaki_katman_ikincil_dugmeleri_YildizVazgec_kullanir()
    {
        var kok = Path.GetDirectoryName(CssKaynak.Yol("Kuyumcu.Web", "Pages", "Kullanicilar.razor"))!;
        var ihlal = new List<string>();
        var sayi = 0;
        foreach (var dosya in Directory.GetFiles(kok, "*.razor"))
        {
            var derinlik = 0; var no = 0;
            foreach (var satir in File.ReadLines(dosya))
            {
                no++;
                if (Regex.IsMatch(satir, @"<(ModalPencere|Cekmece)([ >]|$)")) derinlik++;
                if (Regex.IsMatch(satir, @"</(ModalPencere|Cekmece)>")) derinlik--;
                if (derinlik <= 0) continue;
                if (satir.Contains("@Dil.T.Ortak.Iptal", StringComparison.Ordinal)) ihlal.Add(Path.GetFileName(dosya) + ":" + no);
                if (satir.Contains("<Kuyumcu.Web.Bilesenler.Yildiz.YildizVazgec />", StringComparison.Ordinal)) sayi++;
            }
        }
        Assert.True(ihlal.Count == 0, "katman içinde çıplak Ortak.Iptal: " + string.Join(", ", ihlal));
        Assert.True(sayi >= 31, "YildizVazgec kullanımı " + sayi);
    }
    [Fact]
    public void YK_44_Emanet_govde_ici_eylem_satiri_alt_serit_ile_ayni_yer_ve_cizgi()
    {
        var k = Kural(KatmanBlogu(), "html:root[data-sema=\"yildiz\"] body .cekmece .cekmece-govde > .emanet-cekmece-alt:not(#_)");
        Assert.Contains("margin: auto -32px -24px", k);
        Assert.Contains("bottom: -24px", k);
        Assert.Contains("padding: 24px 32px", k);
        Assert.Contains("border-top: 1px solid var(--ayrac)", k);
        Assert.Contains("background-color: var(--yuzey-govde)", k);
    }
}
