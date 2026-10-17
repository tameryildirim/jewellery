using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S3-3 — ÜRÜNLER (/urunler) + ETİKETLER (/urunler/etiketler)
//
// QA tur 8: /urunler "stok adedi sütunu yok; stok kipinde ham ULID; durum
// süzgeci yok"; /urunler/etiketler "sonuçsuz aramada boş durum yok; liste
// boşalsa da 30 etiket seçili kalıyor (yanlış basım riski)".
// Hedef (plan § 6 S-3 + S3-2 şablonu, taslak n3):
//   · satırda ≤ 3 eylem + "⋯" — geri kalan HER eylem "⋯" penceresinde;
//   · süzgeç satırı: arama + tek seçici (Durum) + "Süzgeç ▾" (4 seçici katlı);
//   · stok adedi sütunu (sayı = stok penceresine bağlantı), şube ADI;
//   · basit modda form ≤ 8 alan, gerisi "Daha fazla ▾" (değer korunur);
//   · etiket seçimi daima görünen listenin alt kümesidir.
// Her kural İSTEK ve DOM düzeyinde sınanır; "düğme var" yetmez.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S33UrunlerEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string UlidSube = "01J9ZQ4K7M2V8X3N5B6C7D8E9F";

    // u1: aktif ZİYNET · u2: aktif PIRLANTA · u3: PASİF ziynet
    private string urunListesi = """
        [{"id":"u1","tip":"ZIYNET","kod":"BLZ-01","ad":"Burma Bilezik","ayarId":"K22",
          "gramAgirlik":15.5,"tasAgirlik":0,"hasGrami":14.19,"barkod":"869001","aktif":true,"birim":"ADET"},
         {"id":"u2","tip":"PIRLANTA","kod":"YZK-07","ad":"Tektaş Yüzük","ayarId":"K18",
          "gramAgirlik":3.2,"tasAgirlik":0.25,"hasGrami":2.4,"barkod":"869002","aktif":true,"birim":"ADET",
          "tasKokeni":"SENTETIK","tasIbaresi":"sentetik"},
         {"id":"u3","tip":"ZIYNET","kod":"KLY-03","ad":"Eski Kolye","ayarId":"K14",
          "gramAgirlik":6,"tasAgirlik":0,"hasGrami":3.5,"barkod":null,"aktif":false,"birim":"ADET"}]
        """;

    /// <summary>Stok ucu bu ürün için 500 döner (UX-S28 sınaması).</summary>
    private string? stokHataliUrun;

    private const string TamKart = """
        {"id":"u1","tip":"ZIYNET","kod":"BLZ-01","ad":"Burma Bilezik","ayarId":"K22",
         "gramAgirlik":15.5,"tasAgirlik":0,"iscilikTl":250,"barkod":"869001",
         "tasKokeni":"TAS_YOK","tasIbaresi":null,"birim":"ADET",
         "kategoriId":"k2","reyonId":"r1","satisFiyati":null,
         "aciklama":"vitrin ürünü","kmtsKodu":"KMTS-9","minStok":2,"olcu":"17"}
        """;

    public S33UrunlerEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(S33UrunlerEkranTestleri t) : HttpMessageHandler
    {
        private static string TopluStok(string yol, string? haric)
        {
            var q = Uri.UnescapeDataString(yol[(yol.IndexOf("ids=", StringComparison.Ordinal) + 4)..]);
            var satirlar = q.Split('&')[0].Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Where(id => id != haric)
                .Select(id => id == "u1"
                    ? """{"urunId":"u1","toplamAdet":4.25,"toplamHasGram":56.7}"""
                    : $$"""{"urunId":"{{id}}","toplamAdet":0,"toplamHasGram":0}""");
            return $$"""{"urunler":[{{string.Join(",", satirlar)}}]}""";
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));

            if (t.stokHataliUrun is { } h && yol.EndsWith($"/urunler/{h}/stok"))
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"hata":"stok okunamadı"}""", System.Text.Encoding.UTF8, "application/json"),
                };

            var g = yol switch
            {
                // PAKET ISTEK-1 — toplu stok adedi (UrunStokOzetController): GERÇEK şekil.
                // u1 iki şubede 3 + 1,25 = 4,25; diğerleri 0. "Stoku okunamayan" ürün
                // yanıtta YER ALMAZ (sunucu kimliği tanımıyor) → hücre "?" + bant.
                var y when y.Contains("/urunler/stok-adetleri") => TopluStok(y, t.stokHataliUrun),
                // GERÇEK şekil (UrunModulu.StokListele): zarf + subeStoklar; iki şube.
                var y when y.EndsWith("/urunler/u1/stok") => $$"""
                    {"urunId":"u1","toplamAdet":4.25,"toplamHasGram":56.7,
                     "subeStoklar":[{"subeId":"s1","adet":3.000,"toplamHasGram":40,"guncellendi":"2026-09-17T00:00:00"},
                                    {"subeId":"{{UlidSube}}","adet":1.250,"toplamHasGram":16.7,"guncellendi":"2026-09-17T00:00:00"}]}
                    """,
                var y when y.EndsWith("/stok") => """{"urunId":"x","toplamAdet":0,"toplamHasGram":0,"subeStoklar":[]}""",
                var y when y.EndsWith("/etiket") => """
                    {"urunId":"u1","kod":"BLZ-01","ad":"Burma Bilezik","ibare":"","ibareZorunlu":false,
                     "etiketMetni":"Burma Bilezik","barkod":"869001","ayarId":"K22","milyem":916,
                     "gramAgirlik":15.5,"hasGrami":14.19,"dayanak":"Kuyum Ticareti Yönetmeliği md.11/(e)"}
                    """,
                var y when y.EndsWith("/yerlesim") => """{"subeler":[]}""",
                var y when y.EndsWith("/urunler/madenler") => """
                    {"madenler":[{"maden":"ALTIN","madenAdi":"Altın","rodyumKaplanabilir":true,
                      "ayarlar":[{"id":"K22","ad":"22 Ayar","milyem":916,"kiymetli":true,"hasHesaplanir":true}]}],
                     "etiket":{"rodyumIbaresiGoster":false,"rodyumIbaresi":"rodyum kaplama"}}
                    """,
                var y when y.Contains("/urun-kategorileri") => """
                    [{"id":"k1","ad":"Altın","sira":0,"aktif":true,"urunSayisi":2,
                      "altlar":[{"id":"k2","ad":"Bilezik","sira":0,"aktif":true,"urunSayisi":1,"altlar":[]}]}]
                    """,
                var y when y.Contains("/reyonlar") => """[{"id":"r1","subeId":"s1","ad":"Ön Vitrin","tip":"VITRIN","aktif":true,"urunSayisi":3}]""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                var y when y.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                var y when y.EndsWith("/urunler/u1") => TamKart,
                var y when y.Contains("/belgeler") => "[]",
                var y when y.Contains("/kur/pariteler") => """{"kaynak":"TEST","pariteler":[]}""",
                var y when y.Contains("/urunler") => t.urunListesi,
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Urunler> Ciz(string mod = ArayuzModuServisi.Profesyonel)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("k1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Kuyumcu.Web.Pages.Urunler>().AyrintiAc();
        e.WaitForAssertion(() => Assert.DoesNotContain("…", e.Find("#u-stok-adet-u1").TextContent));
        lock (istekler) istekler.Clear();
        return e;
    }

    private List<(string Metot, string Yol, string Govde)> Istekler()
    {
        lock (istekler) return istekler.ToList();
    }

    private static string? Alan(string govde, string ad)
    {
        var k = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return k.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    private static IReadOnlyList<AngleSharp.Dom.IElement> GorunenEylemler(
        IRenderedComponent<Kuyumcu.Web.Pages.Urunler> e, string id) =>
        e.Find($"#u-eylem-{id}").QuerySelectorAll("button").ToList();

    // ── (1) SATIRDA EN ÇOK 3 EYLEM + "⋯" ───────────────────────────────────

    [Theory]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    [InlineData(ArayuzModuServisi.Basit)]
    public void S33_01_Satirda_uc_gorunen_eylem_aktifte_Stok_pasifte_Aktife_al(string mod)
    {
        var e = Ciz(mod);
        foreach (var id in new[] { "u1", "u2" })
        {
            var d = GorunenEylemler(e, id);
            Assert.Equal(3, d.Count);
            Assert.Equal($"u-stok-{id}", d[0].Id);
            Assert.Equal($"u-duzenle-{id}", d[1].Id);
            Assert.Equal($"u-menu-ac-{id}", d[2].Id);
        }
        var p = GorunenEylemler(e, "u3");
        Assert.Equal(3, p.Count);
        Assert.Equal("u-aktife-al-u3", p[0].Id);     // geri dönüş yolu TEK TIK (UX-S25)
        Assert.Equal("u-duzenle-u3", p[1].Id);
    }

    [Fact]
    public void S33_02_Etiket_ve_Pasife_al_menude_durur_pasif_satirda_Stok_menude()
    {
        var e = Ciz();
        var m1 = e.Find("#u-menu-u1");
        Assert.NotNull(m1.QuerySelector("#u-etiket-u1"));
        Assert.NotNull(m1.QuerySelector("#u-pasife-al-u1"));
        Assert.Null(m1.QuerySelector("#u-stok-u1"));          // aktifte stok satırda görünür

        var m3 = e.Find("#u-menu-u3");
        Assert.NotNull(m3.QuerySelector("#u-stok-u3"));       // pasifte stok KAYBOLMAZ, menüde
        Assert.NotNull(m3.QuerySelector("#u-etiket-u3"));
        Assert.Null(m3.QuerySelector("#u-pasife-al-u3"));     // pasif ürün yeniden pasife alınmaz
        Assert.Empty(e.FindAll("#u-aktife-al-u1"));
    }

    [Fact]
    public void S33_03_Menu_yerli_popover_her_oge_pencereyi_kapatir()
    {
        var e = Ciz();
        Assert.Equal("u-menu-u1", e.Find("#u-menu-ac-u1").GetAttribute("popovertarget"));
        Assert.Equal("auto", e.Find("#u-menu-u1").GetAttribute("popover"));
        foreach (var b in e.Find("#u-menu-u1").QuerySelectorAll("button"))
        {
            Assert.Equal("u-menu-u1", b.GetAttribute("popovertarget"));
            Assert.Equal("hide", b.GetAttribute("popovertargetaction"));
        }
    }

    [Fact]
    public void S33_04_Menuden_Etiket_ve_Pasife_al_GERCEK_istek_atar()
    {
        var e = Ciz();
        e.Find("#u-etiket-u1").Click();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "GET" && i.Yol.EndsWith("/urunler/u1/etiket")));

        e.Find("#u-pasife-al-u1").Click();
        Assert.DoesNotContain(Istekler(), i => i.Metot == "DELETE");   // önce ONAY
        e.FindAll("[role=dialog][aria-modal=true] button").Last(b => b.TextContent.Contains("Pasife Al")).Click();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "DELETE" && i.Yol.EndsWith("/urunler/u1")));
    }

    // ── (2) STOK ADEDİ SÜTUNU + ŞUBE ADI ───────────────────────────────────

    [Fact]
    public void S33_05_Stok_adedi_sutunu_subeleri_toplar_ve_tiklaninca_stok_penceresini_acar()
    {
        var e = Ciz();
        Assert.Equal("4,25 adet", e.Find("#u-stok-adet-u1").TextContent.Trim());
        Assert.Equal("0 adet", e.Find("#u-stok-adet-u2").TextContent.Trim());

        e.Find("#u-stok-adet-u1").Click();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "GET" && i.Yol.EndsWith("/urunler/u1/stok")));
        Assert.Contains("Merkez", e.Find("[role=dialog][aria-modal=true]").TextContent);
    }

    [Fact]
    public void S33_06_Stok_adedi_alinamazsa_hucre_soru_isareti_ve_HATA_BANDI_liste_kalir()
    {
        stokHataliUrun = "u2";
        var e = Ciz();
        e.WaitForAssertion(() => Assert.Equal("?", e.Find("#u-stok-adet-u2").TextContent.Trim()));
        Assert.Contains(UrunListeSozlugu.Tr.StokAlinamadi, e.Find("#urun-stok-hata").TextContent);
        Assert.Equal("4,25 adet", e.Find("#u-stok-adet-u1").TextContent.Trim());   // diğer satırlar sağlam
        Assert.NotNull(e.Find("#urun-liste-sar"));                                     // hata ≠ "kayıt yok"
    }

    [Fact]
    public void S33_07_Stok_penceresinde_ham_sube_kimligi_YOK_sube_ADI_var()
    {
        var e = Ciz();
        e.Find("#u-stok-u1").Click();
        e.WaitForAssertion(() => Assert.Contains("Merkez", e.Find("#u-stok-sube-s1").TextContent));
        var modal = e.Find("[role=dialog][aria-modal=true]").TextContent;
        Assert.DoesNotContain(UlidSube, modal);
        Assert.Contains(UrunListeSozlugu.Tr.SubeBilinmiyor, e.Find($"#u-stok-sube-{UlidSube}").TextContent);
        Assert.Contains(Istekler(), i => i.Yol.EndsWith("/subeler"));
    }

    // ── (3) SÜZGEÇ SATIRI ──────────────────────────────────────────────────

    [Fact]
    public void S33_08_Basit_modda_dort_secici_KATLI_Suzgec_dugmesi_acar_ve_sayar()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        Assert.NotNull(e.Find("#urun-durum-filtre"));
        foreach (var id in new[] { "urun-maden-filtre", "urun-ayar-filtre", "urun-kategori-filtre", "urun-reyon-filtre" })
            Assert.Empty(e.FindAll("#" + id));

        e.Find("#urun-suzgec-ac").Click();
        Assert.Equal("true", e.Find("#urun-suzgec-ac").GetAttribute("aria-expanded"));
        foreach (var id in new[] { "urun-maden-filtre", "urun-ayar-filtre", "urun-kategori-filtre", "urun-reyon-filtre" })
            Assert.NotNull(e.Find("#" + id));

        SeciciYardim.Sec(e, "urun-maden-filtre", "ALTIN");
        e.WaitForAssertion(() => Assert.Contains("(1)", e.Find("#urun-suzgec-ac").TextContent));
        // Kapatınca süzgeç DEĞERİ korunur ve düğmede sayılır — görünmez süzgeç yok.
        e.Find("#urun-suzgec-ac").Click();
        Assert.Empty(e.FindAll("#urun-maden-filtre"));
        Assert.Contains("(1)", e.Find("#urun-suzgec-ac").TextContent);
    }

    [Fact]
    public void S33_09_Profesyonel_modda_dort_secici_ACIK_suzgec_dugmesi_yok()
    {
        var e = Ciz();
        Assert.Empty(e.FindAll("#urun-suzgec-ac"));
        foreach (var id in new[] { "urun-maden-filtre", "urun-ayar-filtre", "urun-kategori-filtre", "urun-reyon-filtre", "urun-durum-filtre" })
            Assert.NotNull(e.Find("#" + id));
    }

    [Fact]
    public void S33_10_Durum_suzgeci_aktif_parametresiyle_uca_gider_ve_limit_ACIKCA_istenir()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        SeciciYardim.Sec(e, "urun-durum-filtre", "false");
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "GET"
            && i.Yol.Contains("/urunler?") && i.Yol.Contains("aktif=false") && i.Yol.Contains("limit=50")));
        // UX-S29: sınır ekranda — TOPLU-6 (TOPLU-4 hakemi ORTA): sınırın altında sayaç yalnız sayıyı yazar, sınır ipucunda (title).
        Assert.Contains("50", e.Find("#urun-sinir").InnerHtml);
    }

    [Fact]
    public void S33_11_Derin_baglantidan_gelen_katli_suzgec_ACIK_baslar()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/urunler?maden=ALTIN");
        var e = Ciz(ArayuzModuServisi.Basit);
        Assert.NotNull(e.Find("#urun-maden-filtre"));
        Assert.Contains("(1)", e.Find("#urun-suzgec-ac").TextContent);
    }

    [Fact]
    public void S33_12_Sinira_degen_listede_DAHA_FAZLASI_VAR_uyarisi()
    {
        urunListesi = "[" + string.Join(",", Enumerable.Range(1, 50).Select(i =>
            $$"""{"id":"x{{i}}","tip":"ZIYNET","kod":"K{{i}}","ad":"Ürün {{i}}","ayarId":"K22","gramAgirlik":1,"tasAgirlik":0,"hasGrami":0.9,"barkod":null,"aktif":true}"""))
            + "]";
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("k1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        var e = Render<Kuyumcu.Web.Pages.Urunler>().AyrintiAc();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#urun-sinir-asildi")));
    }

    // ── (4) SÜTUNLAR ve HAM KOD ────────────────────────────────────────────

    [Fact]
    public void S33_13_Basit_modda_barkod_grup_durum_sutunu_yok_pasif_rozet_ad_hucresinde()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        var basliklar = e.FindAll("#urun-liste-sar thead th").Select(t => t.TextContent.Trim()).ToList();
        Assert.DoesNotContain("Barkod", basliklar);
        Assert.DoesNotContain("Ürün grubu", basliklar);
        Assert.Contains("Stok", basliklar);
        Assert.NotNull(e.Find("#u-pasif-rozet-u3"));
        Assert.Empty(e.FindAll("#u-pasif-rozet-u1"));
    }

    [Fact]
    public void S33_14_Profesyonel_modda_grup_sutunu_AD_basar_ham_kod_basmaz()
    {
        var e = Ciz();
        Assert.Equal("Ziynet", e.Find("#u-grup-u1").TextContent.Trim());
        Assert.Equal("Pırlanta", e.Find("#u-grup-u2").TextContent.Trim());
        Assert.DoesNotContain(">ZIYNET<", e.Markup);
        Assert.DoesNotContain(">PIRLANTA<", e.Markup);
        var basliklar = e.FindAll("#urun-liste-sar thead th").Select(t => t.TextContent.Trim()).ToList();
        // TOPLU-2 (KESIF-P4 açık kalanı, ölçüldü): PRO başlığı kısa "Grup", tam ad "Ürün grubu" title'da —
        // iki kelimelik başlık tabloyu 1280'de 26 px taşırıp DURUM sütununu eylem hücresinin altında bırakıyordu.
        Assert.Contains(Kuyumcu.Core.I18n.Toplu2Sozlugu.Tr.UrunGrubuKisa, basliklar);
        Assert.Contains(e.FindAll("#urun-liste-sar thead th"), t => t.GetAttribute("title") == "Ürün grubu"); // UX-S31: "Kategori" ağaç kategorisidir
        Assert.DoesNotContain("Kategori", basliklar);
    }

    [Fact]
    public void S33_15_Maden_kilidi_grup_ADINI_yazar_kodu_yazmaz()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.Find("#urun-yeni-ac").Click();
        SeciciYardim.Sec(e, "u-tip", "GUMUS");
        var kilit = e.Find("#u-maden-kilit").TextContent;
        Assert.Contains("Gümüş", kilit);
        Assert.DoesNotContain("GUMUS", kilit);
    }

    // ── (5) BASİT MODDA FORM ≤ 8 ALAN ──────────────────────────────────────

    private static readonly string[] YeniCekirdek =
        ["u-ad", "u-kod", "u-barkod", "u-tip", "u-maden", "u-ayar", "u-gram", "u-acilis-stok"];

    [Fact]
    public void S33_16_Basit_modda_yeni_form_sekiz_alan_Daha_fazla_ile_hepsi_acilir()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.Find("#urun-yeni-ac").Click();
        foreach (var id in YeniCekirdek) Assert.NotNull(e.Find("#" + id));
        foreach (var id in new[] { "u-iscilik", "u-kategori", "u-tas", "u-gercek-milyem", "u-aciklama", "u-tedarikci", "u-rodyum", "u-birim" })
            Assert.Empty(e.FindAll("#" + id));
        // Görünen form alanı sayısı ≤ 8 (girdi + seçici tetikleri, modal içinde).
        var modal = e.Find("[role=dialog][aria-modal=true]");
        var alanlar = modal.QuerySelectorAll("input:not([type=hidden]), textarea, [role=combobox]")
            .Select(x => x.Id).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        Assert.True(alanlar.Count <= 8, "görünen alanlar: " + string.Join(",", alanlar));

        e.Find("#u-daha-fazla").Click();
        Assert.Equal("true", e.Find("#u-daha-fazla").GetAttribute("aria-expanded"));
        foreach (var id in new[] { "u-iscilik", "u-kategori", "u-tas", "u-gercek-milyem", "u-aciklama", "u-tedarikci", "u-rodyum", "u-birim" })
            Assert.NotNull(e.Find("#" + id));
    }

    [Fact]
    public void S33_17_Profesyonel_modda_katlama_YOK()
    {
        // PAKET GOC-C (devir URUN-ACILIS-ATOMIK KAPSAM DIŞI #3, kurucu 25.09 "zorunlu olanlar hariç ilk etapta
        // kullanıcı görmesin"): PRO'da da form ZORUNLULARLA açılır; "Daha fazla" iki modda aynı kalıp.
        // Test adı korunur (geçmiş kayıt), sözleşme ters yönde sınanır: açılışta katlı, tıklayınca açık.
        var e = Ciz();
        e.Find("#urun-yeni-ac").Click();
        Assert.Empty(e.FindAll("#u-iscilik"));
        Assert.Empty(e.FindAll("#u-tedarikci"));
        e.Find("#u-daha-fazla").Click();
        Assert.NotNull(e.Find("#u-iscilik"));
        Assert.NotNull(e.Find("#u-tedarikci"));
    }

    [Fact]
    public void S33_18_Basit_modda_katli_alanlar_PATCHte_AYNEN_geri_yazilir_silinmez()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.Find("#u-duzenle-u1").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#d-daha-fazla")));
        Assert.Empty(e.FindAll("#d-kategori"));      // katlı
        lock (istekler) istekler.Clear();
        e.Find("#cekmece-urun-kaydet").Click();

        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "PATCH"));
        var govde = Istekler().First(i => i.Metot == "PATCH").Govde;
        Assert.Equal("k2", Alan(govde, "kategoriId"));
        Assert.Equal("r1", Alan(govde, "reyonId"));
        Assert.Equal("250", Alan(govde, "iscilikTl"));
        Assert.Equal("vitrin ürünü", Alan(govde, "aciklama"));
        // PAKET YASAL-4 / Y4-C: BASİT'te KMTS alanı HİÇ çizilmez → PATCH'te kmtsKodu
        // GÖNDERİLMEZ (null = sunucuda KORU; UrunModulu "null → dokunma"). Kayıtlı kod silinmez.
        Assert.Null(Alan(govde, "kmtsKodu"));
    }

    [Fact]
    public void S33_22_Basit_modda_cekmece_en_cok_sekiz_girdi_belge_yukleyici_katli()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.Find("#u-duzenle-u1").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#d-daha-fazla")));
        var cekmece = e.Find("#cekmece-urun");
        var girdiler = cekmece.QuerySelectorAll("input:not([type=hidden]), textarea, select, [role=combobox]")
            .Select(x => x.Id).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        Assert.True(girdiler.Count <= 8, "görünen girdiler: " + string.Join(",", girdiler));
        Assert.Empty(e.FindAll("#d-belge-tur"));
        e.Find("#d-daha-fazla").Click();
        Assert.NotNull(e.Find("#d-belge-tur"));       // katlı, silinmedi
        Assert.NotNull(e.Find("#d-kategori"));
    }

    [Fact]
    public void S33_19_Katli_alanda_hata_varsa_grup_kendiliginden_acilir_istek_atilmaz()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.Find("#urun-yeni-ac").Click();
        e.Find("#u-daha-fazla").Click();
        e.Find("#u-gorsel-url").Input("javascript:alert(1)");
        e.Find("#u-daha-fazla").Click();              // kullanıcı grubu kapattı
        Assert.Empty(e.FindAll("#u-gorsel-url"));
        e.Find("#u-ad").Input("Yeni yüzük");
        e.Find("#u-kod").Input("YZ-1");
        e.Find("#u-gram").Input("3");
        SeciciYardim.Sec(e, "u-ayar", "K18");   // P-URUN (K6): ayarın varsayılanı yok — kullanıcı seçer
        lock (istekler) istekler.Clear();
        e.Find("#u-kaydet").Click();

        Assert.NotNull(e.Find("#u-gorsel-url"));      // hata katlı alanda → grup AÇILDI
        Assert.Equal("true", e.Find("#u-daha-fazla").GetAttribute("aria-expanded"));
        Assert.DoesNotContain(Istekler(), i => i.Metot == "POST");
    }

    [Fact]
    public void S33_20_Kural_kapisi_olan_alan_katlanmaz_celikte_satis_fiyati_gorunur()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.Find("#urun-yeni-ac").Click();
        Assert.Empty(e.FindAll("#u-satis-fiyati"));
        SeciciYardim.Sec(e, "u-tip", "CELIK");
        Assert.NotNull(e.Find("#u-satis-fiyati"));    // kıymetsiz madende fiyat ZORUNLU
        Assert.NotNull(e.Find("#u-kiymetsiz-uyari"));
    }

    // ── (6) SÖZLÜK ────────────────────────────────────────────────────────

    [Fact]
    public void S33_21_Sozluk_TR_EN_dolu_farkli_ve_her_urun_grubunun_adi_var()
    {
        foreach (var (tr, en) in new (object, object)[]
                 { (UrunListeSozlugu.Tr, UrunListeSozlugu.En), (UrunListeSozlugu.EtiketTr, UrunListeSozlugu.EtiketEn) })
        {
            foreach (var p in tr.GetType().GetProperties().Where(p => p.PropertyType == typeof(string)))
            {
                var t = (string)p.GetValue(tr)!;
                var n = (string)p.GetValue(en)!;
                Assert.False(string.IsNullOrWhiteSpace(t), p.Name);
                Assert.False(string.IsNullOrWhiteSpace(n), p.Name);
                if (p.Name is not "StokYukleniyor") Assert.NotEqual(t, n);
            }
        }
        foreach (var kod in Kuyumcu.Core.Domain.UrunTipleri.Hepsi)
        {
            Assert.NotEqual(kod, UrunListeSozlugu.GrupTr.Ad(kod));
            Assert.NotEqual(kod, UrunListeSozlugu.GrupEn.Ad(kod));
        }
    }
}

public sealed class S33EtiketEkranTestleri : BunitContext
{
    private string urunler = """
        [{"id":"u1","tip":"ZIYNET","kod":"Y-1","ad":"Safir yüzük","ayarId":"K14",
          "gramAgirlik":4.0,"tasAgirlik":0.4,"hasGrami":2.3,"barkod":"K0123456789","aktif":true,
          "tasKokeni":"SENTETIK","tasIbaresi":null,"milyem":585,"maden":"ALTIN","madenAdi":"Altın","rodyumKaplama":false},
         {"id":"u2","tip":"PLATIN","kod":"Y-2","ad":"Sade alyans","ayarId":"PT950",
          "gramAgirlik":5.0,"tasAgirlik":0.0,"hasGrami":4.75,"barkod":null,"aktif":true,
          "tasKokeni":"TAS_YOK","tasIbaresi":null,"milyem":950,"maden":"PLATIN","madenAdi":"Platin","rodyumKaplama":false},
         {"id":"u3","tip":"ZIYNET","kod":"B-3","ad":"Burma bilezik","ayarId":"K22",
          "gramAgirlik":15.0,"tasAgirlik":0.0,"hasGrami":13.7,"barkod":"869003","aktif":true,
          "tasKokeni":"TAS_YOK","tasIbaresi":null,"milyem":916,"maden":"ALTIN","madenAdi":"Altın","rodyumKaplama":false}]
        """;

    private bool listeHatali;

    public S33EtiketEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(S33EtiketEkranTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            if (yol.EndsWith("/urunler/madenler"))
                return Task.FromResult(Tamam("""{"madenler":[],"etiket":{"rodyumIbaresiGoster":false,"rodyumIbaresi":"rodyum kaplama"}}"""));
            if (t.listeHatali)
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"hata":"sunucu hatası"}""", System.Text.Encoding.UTF8, "application/json"),
                });
            return Task.FromResult(Tamam(t.urunler));
        }

        private static HttpResponseMessage Tamam(string g) => new(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.UrunEtiketleri> Ciz() => Render<Kuyumcu.Web.Pages.UrunEtiketleri>().AyrintiAc();

    private static void Sec(IRenderedComponent<Kuyumcu.Web.Pages.UrunEtiketleri> e, string urunAdi)
        => e.FindAll("input[type=checkbox]").First(i => i.GetAttribute("aria-label") == urunAdi).Change(true);

    [Fact]
    public void S33_E1_Sonucsuz_aramada_BOS_DURUM_aranan_metinle_cikar()
    {
        var e = Ciz();
        e.Find("#et-ara").Input("abc");
        var bos = e.Find("#et-arama-bos").TextContent;
        Assert.Contains("\"abc\"", bos);
        Assert.Empty(e.FindAll(".tablo-sar table"));
    }

    [Fact]
    public void S33_E2_Arama_degisince_GORUNMEYEN_secim_duser_onizleme_yalniz_gorunenleri_basar()
    {
        var e = Ciz();
        Sec(e, "Safir yüzük");
        Sec(e, "Sade alyans");
        Assert.Equal(2, e.FindAll(".urun-etiket").Count);

        e.Find("#et-ara").Input("safir");
        Assert.Single(e.FindAll(".urun-etiket"));
        Assert.Contains("Safir yüzük", e.Find(".urun-etiket").TextContent);
        Assert.Contains(string.Format(UrunListeSozlugu.EtiketTr.SecimKaldirildi, 1), e.Find("#et-secim-kaldirildi").TextContent);

        // Liste boşalınca HİÇBİR etiket basılamaz (QA tur 8: 30 etiket seçili kalıyordu).
        e.Find("#et-ara").Input("zzz");
        Assert.Empty(e.FindAll(".urun-etiket"));
        Assert.True(e.Find("#et-yazdir").HasAttribute("disabled"));

        // Arama temizlenince eski seçim GERİ GELMEZ.
        e.Find("#et-ara").Input("");
        Assert.Empty(e.FindAll(".urun-etiket"));
    }

    [Fact]
    public void S33_E3_Otuz_etiketlik_onizleme_calisir_ve_dugme_sayiyi_yazar()
    {
        var e = Ciz();
        e.Find("#et-tumunu-sec").Click();
        e.Find("#et-adet").Input("10");
        Assert.Equal(30, e.FindAll(".urun-etiket").Count);
        Assert.Contains("30", e.Find("#et-yazdir").TextContent);
        Assert.Contains(string.Format(UrunListeSozlugu.EtiketTr.SeciliOzet, 3, 30), e.Find("#et-secili-ozet").TextContent);
        Assert.False(e.Find("#et-yazdir").HasAttribute("disabled"));
    }

    [Fact]
    public void S33_E4_Secim_yokken_kilit_sebebi_ne_yapilacagini_yazar_dugmeler_kilitli()
    {
        var e = Ciz();
        Assert.Equal(UrunListeSozlugu.EtiketTr.OnceSecin, e.Find("#et-yazdir-kilit").TextContent.Trim());
        Assert.True(e.Find("#et-secimi-temizle").HasAttribute("disabled"));
        e.Find("#et-ara").Input("zzz");
        Assert.True(e.Find("#et-tumunu-sec").HasAttribute("disabled"));
    }

    [Fact]
    public void S33_E5_Bos_liste_BOS_DURUM_gosterir()
    {
        urunler = "[]";
        var e = Ciz();
        Assert.Contains(UrunListeSozlugu.EtiketTr.ListeBos, e.Markup);
    }

    [Fact]
    public void S33_E6_Hata_bos_duruma_CEVRILMEZ_hata_bandi_cikar()
    {
        listeHatali = true;
        var e = Ciz();
        Assert.DoesNotContain(UrunListeSozlugu.EtiketTr.ListeBos, e.Markup);
        Assert.NotNull(e.Find("#hata-bandi"));
    }
}
