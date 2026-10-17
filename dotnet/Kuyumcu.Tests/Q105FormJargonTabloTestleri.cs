using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q10-5 — ORTAK FORM HATASI + KALAN JARGON + TABLO/MOBİL [saf + bUnit]
// Kaynak: QA turu 10 (24.09, gerçek tarayıcı) — ham/qa-turu-10-bulgular.json.
//
//   A. Ortak form durumu: alanın değeri değişince O ALANIN mesajı düşer
//      (FormDurumu.Sil + FormAlani.DefterdenSil), sunucu 400 alan adı harf
//      duyarsız eşlenir (IBAN kökü).
//   B. Basit mod jargonu: dışa aktarım kapsam adları, denetim detayı, ürün
//      ağacı "Pasife al", kılavuz bölümü, kurulum ve banka dekont metinleri.
//   C. Tablo başlığı tek satır sınıfı + /sayim mobil yığın sözleşmesi + CSS.
//
// ⚠ Sahte API '{}' varsayılanına BIRAKILMADI: dokunulan her uca Contracts
//   kayıtlarıyla BİREBİR alan adlı gerçekçi yanıt verildi.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Q105FormJargonTabloTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly Dictionary<string, string> yanitlar = new(StringComparer.Ordinal);

    public Q105FormJargonTabloTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
    }

    private sealed class SahteApi(Q105FormJargonTabloTestleri sahip) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            sahip.istekler.Add((istek.Method.Method, yol, govde));
            // En UZUN eşleşen önek kazanır ("/sayim" ile "/sayim/x" karışmasın).
            var y = sahip.yanitlar
                .Where(k => yol.Contains(k.Key, StringComparison.Ordinal))
                .OrderByDescending(k => k.Key.Length)
                .Select(k => k.Value).FirstOrDefault() ?? "{}";
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private void Basit() => Services.GetRequiredService<DilServisi>().BasitModuBagla(() => true);

    private void Oturum() => Services.GetRequiredService<OturumServisi>().KullaniciAta(
        new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));

    /// <summary>sikisma-olc.js K8 "yeni regex" ile AYNI desen (QA 10, 24.09).</summary>
    private static readonly Regex K8 = new(
        @"(?<![\p{L}])(cari|cariler|cariden|cariye|carinin|storno|vezne|mutabakat|pasife al|denetim izi)(?![\p{L}])|\b[a-z]+(?:-[a-z]+){1,}\b(?=[^\/.]|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // ═══ A. ORTAK FORM DURUMU ═══════════════════════════════════════════════

    [Fact]
    public void Q105_A01_FormDurumu_Sil_yalniz_o_alani_siler_ve_sirayi_gunceller()
    {
        var f = new FormDurumu();
        f.Ekle("ad", "zorunlu");
        f.Ekle("iban", "geçersiz");
        Assert.Equal("ad", f.IlkHatali);

        Assert.True(f.Sil("AD"));               // harf duyarsız (defter de öyle)
        Assert.False(f.Var("ad"));
        Assert.True(f.Var("iban"));
        Assert.Equal("iban", f.IlkHatali);
        Assert.Equal(1, f.HataSayisi);
        Assert.False(f.Sil("ad"));              // ikinci silme: yok
        Assert.False(f.Sil(null));
    }

    [Fact]
    public void Q105_A02_Sunucu_camelCase_alan_adi_PascalCase_eslemeyle_tutar()
    {
        var esleme = new Dictionary<string, string> { ["Iban"] = "hesap-iban", ["BankaAdi"] = "hesap-banka" };
        Assert.Equal("hesap-iban", FormDurumu.EslemeAdi("iban", esleme));
        Assert.Equal("hesap-iban", FormDurumu.EslemeAdi("Iban", esleme));
        Assert.Equal("hesap-banka", FormDurumu.EslemeAdi("bankaAdi", esleme));
        Assert.Equal("bilinmeyen", FormDurumu.EslemeAdi("bilinmeyen", esleme));
        Assert.Equal("iban", FormDurumu.EslemeAdi("iban", null));

        var f = new FormDurumu();
        f.Sunucudan(new ApiHata(400, "x") { Alanlar = new Dictionary<string, string[]> { ["iban"] = ["Geçerli bir IBAN giriniz"] } }, esleme);
        Assert.Equal("Geçerli bir IBAN giriniz", f.Mesaj("hesap-iban"));
    }

    [Fact]
    public void Q105_A03_Metin_degisince_defterdeki_mesaji_duser()
    {
        var f = new FormDurumu();
        f.Ekle("x-ad", "Bu alan zorunludur.");
        var e = Render<Metin>(p => p.Add(x => x.ElemanId, "x-ad").Add(x => x.Etiket, "Ad")
            .Add(x => x.Durum, f).Add(x => x.HataAlani, "x-ad"));
        Assert.Single(e.FindAll("#alan-hata-x-ad"));
        Assert.Equal("true", e.Find("#x-ad").GetAttribute("aria-invalid"));

        e.Find("#x-ad").Input("Ayşe");

        Assert.False(f.Var("x-ad"));
        Assert.Empty(e.FindAll("#alan-hata-x-ad"));
        Assert.Equal("false", e.Find("#x-ad").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Q105_A04_Sayi_degisince_defterdeki_mesaji_duser()
    {
        var f = new FormDurumu();
        f.Ekle("x-gram", "Sıfırdan büyük olmalı.");
        var e = Render<Sayi>(p => p.Add(x => x.ElemanId, "x-gram").Add(x => x.Etiket, "Gram")
            .Add(x => x.Durum, f).Add(x => x.HataAlani, "x-gram"));
        Assert.Single(e.FindAll("#alan-hata-x-gram"));

        e.Find("#x-gram").Input("5");

        Assert.False(f.Var("x-gram"));
        Assert.Empty(e.FindAll("#alan-hata-x-gram"));
    }

    [Fact]
    public void Q105_A05_Tarih_ve_Onay_degisince_defterdeki_mesaji_duser()
    {
        var f = new FormDurumu();
        f.Ekle("x-tarih", "Tarih seçin.");
        f.Ekle("x-onay", "Onay gerekli.");
        var t = Render<Tarih>(p => p.Add(x => x.ElemanId, "x-tarih").Add(x => x.Etiket, "Tarih")
            .Add(x => x.Durum, f).Add(x => x.HataAlani, "x-tarih"));
        var o = Render<Onay>(p => p.Add(x => x.ElemanId, "x-onay").Add(x => x.Etiket, "Onay")
            .Add(x => x.Durum, f).Add(x => x.HataAlani, "x-onay"));

        t.Find("#x-tarih").Change("2026-09-01");
        o.Find("#x-onay").Change(true);

        Assert.False(f.Var("x-tarih"));
        Assert.False(f.Var("x-onay"));
        Assert.True(f.Gecerli);
    }

    [Fact]
    public void Q105_A06_Baska_alanin_mesajina_dokunulmaz()
    {
        var f = new FormDurumu();
        f.Ekle("x-ad", "zorunlu");
        f.Ekle("x-soyad", "zorunlu");
        var e = Render<Metin>(p => p.Add(x => x.ElemanId, "x-ad").Add(x => x.Etiket, "Ad")
            .Add(x => x.Durum, f).Add(x => x.HataAlani, "x-ad"));
        e.Find("#x-ad").Input("A");
        Assert.False(f.Var("x-ad"));
        Assert.True(f.Var("x-soyad"));
    }

    // ═══ B. BASİT MOD JARGONU ═══════════════════════════════════════════════

    [Fact]
    public void Q105_B01_Denetim_detayi_basitte_kod_adi_basmaz_PRO_ve_eposta_degismez()
    {
        const string ham = "liste: 7 kayıt · cari listesi · islem-defteri · arayuz-modu: BASIT · e-posta";
        var b = Q105Sozlugu.DenetimDetayi(ham, "tr", basit: true)!;
        Assert.Equal("liste: 7 kayıt · müşteri listesi · işlem defteri · görünüm: BASIT · e-posta", b);
        Assert.Equal(ham, Q105Sozlugu.DenetimDetayi(ham, "tr", basit: false));
        Assert.Equal("gider pusulasi", Q105Sozlugu.DenetimDetayi("gider-pusulasi", "tr", true));
        Assert.Null(Q105Sozlugu.DenetimDetayi(null, "tr", true));
        Assert.Equal("customer list", Q105Sozlugu.DenetimDetayi("cari listesi", "en", true));
        // 24.09 canlı ölçümü: "cari-yaslandirma" önce "müşteri-yaslandirma" olup
        // K8'e "teri-yaslandirma" diye takılıyordu — sıra düzeltildi.
        var rapor = Q105Sozlugu.DenetimDetayi("rapor: cari-yaslandirma · kasa-hareketleri", "tr", true)!;
        Assert.Equal("rapor: müşteri yaşlandırma · kasa hareketleri", rapor);
        Assert.DoesNotMatch(K8, rapor);
        Assert.Equal("müşteri defteri", Q105Sozlugu.DenetimDetayi("cari-defteri", "tr", true));
    }

    [Fact]
    public void Q105_B02_Q105_metinleri_TR_EN_dolu_ve_basit_metinler_K8_temiz()
    {
        foreach (var p in typeof(Q105Metinleri).GetProperties())
        {
            var tr = (string)p.GetValue(Q105Sozlugu.Tr)!;
            var en = (string)p.GetValue(Q105Sozlugu.En)!;
            Assert.False(string.IsNullOrWhiteSpace(tr), p.Name);
            Assert.False(string.IsNullOrWhiteSpace(en), p.Name);
            Assert.NotEqual(tr, en);
            Assert.DoesNotMatch(K8, tr);
            Assert.DoesNotContain("K-10", tr);
            Assert.DoesNotContain("K-24", tr);
        }
    }

    private const string Kaynaklar = """
        [{"ad":"cari","kisiselVeri":true,"alanlar":["id"]},{"ad":"urun","kisiselVeri":false,"alanlar":["id"]},
         {"ad":"gider-pusulasi","kisiselVeri":true,"alanlar":["id"]},
         {"ad":"pos-cihazlari","kisiselVeri":false,"alanlar":["id"]},{"ad":"pos-tahsilatlari","kisiselVeri":false,"alanlar":["id"]},
         {"ad":"pos-eslesmeleri","kisiselVeri":false,"alanlar":["id"]},{"ad":"satinalma","kisiselVeri":false,"alanlar":["id"]},
         {"ad":"irsaliye","kisiselVeri":false,"alanlar":["id"]},{"ad":"muhasebe-fisi","kisiselVeri":false,"alanlar":["id"]}]
        """;

    private const string Gecmis = """
        {"toplam":1,"kayitlar":[{"id":"g1","gerekce":"Mali müşavire yıl sonu dökümü","kapsam":"cari,pos-cihazlari,muhasebe-fisi",
          "maskeli":true,"olusturuldu":"2026-09-24T10:00:00Z","kullanildi":null,"satirSayisi":12}]}
        """;

    [Fact]
    public void Q105_B10_Disa_aktarim_basitte_gerekce_metinleri_denetim_izi_demez_PRO_ayni()
    {
        // Doğrulama turu (24.09): tr-TR küçük harfle K8 ölçümü BASİT modda
        // "bu metin denetim izine yazılır" ve "…denetim izine bu gerekçeyle…" buldu.
        var tr = new System.Globalization.CultureInfo("tr-TR");
        yanitlar["/disa-aktarim/kaynaklar"] = Kaynaklar;
        yanitlar["/disa-aktarim/gecmis"] = Gecmis;
        var pro = Render<Kuyumcu.Web.Pages.VeriDisaAktarim>();
        pro.WaitForElement("#da-gerekce");
        Assert.Contains(Sozlukler.Tr.DisaAktarim.GerekceIpucu, string.Concat(pro.Nodes.Select(n => n.TextContent)));
        Basit();
        var e = Render<Kuyumcu.Web.Pages.VeriDisaAktarim>();
        e.WaitForElement("#da-gerekce");
        e.Find("#da-gerekce").Input("kısa");
        var metin = string.Concat(e.Nodes.Select(n => n.TextContent));
        Assert.Contains(Q105Sozlugu.Tr.GerekceIpucuBasit, metin);
        Assert.Contains(string.Format(Q105Sozlugu.Tr.GerekceKisaBasit, 20), metin);
        Assert.DoesNotContain("denetim izi", metin.ToLower(tr));
    }

    [Fact]
    public void Q105_B03_Disa_aktarim_kapsam_etiketleri_ve_gecmis_kod_adi_basmaz()
    {
        yanitlar["/disa-aktarim/kaynaklar"] = Kaynaklar;
        yanitlar["/disa-aktarim/gecmis"] = Gecmis;
        Basit();
        var e = Render<Kuyumcu.Web.Pages.VeriDisaAktarim>();
        e.WaitForElement("#da-kapsam-muhasebe-fisi");

        foreach (var kod in new[] { "pos-cihazlari", "pos-tahsilatlari", "pos-eslesmeleri", "satinalma", "irsaliye", "muhasebe-fisi" })
        {
            var etiket = e.Find($"#da-kapsam-{kod}-sar").TextContent;
            Assert.DoesNotContain(kod, etiket);
        }
        Assert.Contains(Q105Sozlugu.Tr.KapsamMuhasebeFisi, e.Find("#da-kapsam-muhasebe-fisi-sar").TextContent);
        Assert.Contains(Sozlukler.Tr.Nav.Pos, e.Find("#da-kapsam-pos-cihazlari-sar").TextContent);

        var ana = e.Find("table").TextContent;
        Assert.DoesNotContain("pos-cihazlari", ana);
        Assert.DoesNotContain("muhasebe-fisi", ana);

        // Basit modda kişisel veri notu "cariler" demez.
        Assert.DoesNotMatch(K8, e.Find("#da-kisisel-not").TextContent);
    }

    private const string DenetimSayfa = """
        {"toplam":2,"limit":50,"offset":0,"kayitlar":[
          {"id":"d1","tarih":"2026-09-24T10:00:00Z","eylem":"GORUNTULEME","varlikTuru":"Cari","varlikId":null,"hassas":false,
           "detay":"liste: 7 kayıt · cari listesi","ipAdresi":null,"kullaniciId":"u1","kullanici":"Demo Kuyumcu","rol":"SAHIP"},
          {"id":"d2","tarih":"2026-09-24T10:01:00Z","eylem":"EKLEME","varlikTuru":"Kullanici","varlikId":null,"hassas":false,
           "detay":"arayuz-modu: BASIT · islem-defteri","ipAdresi":null,"kullaniciId":"u1","kullanici":"Demo Kuyumcu","rol":"SAHIP"}]}
        """;

    private void DenetimYanitlari()
    {
        yanitlar["/denetim/ozet"] = """{"toplam":2,"hassasAdet":0,"enEskiKayit":"2026-09-24T10:00:00Z","eylemler":[],"turler":[],"subeler":[]}""";
        yanitlar["/denetim?"] = DenetimSayfa;
        yanitlar["/subeler"] = """[{"id":"sb1","ad":"Merkez Şube"}]""";
    }

    [Fact]
    public void Q105_B04_Denetim_izi_basitte_detay_kod_adi_basmaz()
    {
        DenetimYanitlari();
        Oturum();
        Basit();
        var e = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        e.WaitForElement("#dz-tablo tbody tr");
        var detaylar = string.Join(" | ", e.FindAll("#dz-tablo td.hucre-sarar").Select(t => t.TextContent));
        Assert.DoesNotMatch(K8, detaylar);
        Assert.Contains("işlem defteri", detaylar);
    }

    [Fact]
    public void Q105_B09_Denetim_izi_sayfa_basligi_menu_adi_Kim_ne_yapti_basit_ve_PRO()
    {
        // Doğrulama bulgusu (24.09): menü/üst bar "Kim ne yaptı", sayfa başlığı
        // "Denetim İzi" diyordu (UX-S31, K8 — Türkçe 'İ' ölçüm aracından kaçıyordu).
        var menu = Kuyumcu.Core.I18n.MenuAdlari.Tr.KimNeYapti.Ad;
        foreach (var basit in new[] { false, true }) // Basit() bağı geri alınmaz → PRO önce
        {
            DenetimYanitlari();
            Oturum();
            if (basit) Basit();
            var e = Render<Kuyumcu.Web.Pages.DenetimIzi>();
            e.WaitForElement("#sayfa-basi-denetim-izi-baslik");
            var baslik = e.Find("#sayfa-basi-denetim-izi-baslik").TextContent.Trim();
            Assert.Equal(menu, baslik);
            Assert.DoesNotContain("denetim", baslik.ToLower(new System.Globalization.CultureInfo("tr-TR")));
            Assert.DoesNotContain(Kuyumcu.Core.I18n.Sozlukler.Tr.Denetim.Baslik, e.Markup);
        }
    }

    [Fact]
    public void Q105_B05_Denetim_izi_profesyonelde_ham_detay_korunur()
    {
        DenetimYanitlari();
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        e.WaitForElement("#dz-tablo tbody tr");
        var detaylar = string.Join(" | ", e.FindAll("#dz-tablo td.hucre-sarar").Select(t => t.TextContent));
        // KESIF-P3 (ekip kararı 2, 30.09): PRO görünür metinde de ham kod adı YOK; ham kayıt
        // hücrenin title ipucunda AYNEN (denetçi). PRO'da "cari" kalabilir (karar 1).
        Assert.DoesNotContain("islem-defteri", detaylar);
        Assert.Contains("işlem defteri", detaylar);
        Assert.Contains("cari listesi", detaylar);
        // PAKET GOC-D: dz-tablo ortak ızgarada; ham kayıt ipucu (title) hücrenin İÇ öğesinde (ızgara td özniteliği satıra göre değişmez) — sözleşme aynı.
        Assert.Contains(e.FindAll("#dz-tablo td.hucre-sarar [title]"), t => t.GetAttribute("title")!.Contains("islem-defteri"));
    }

    private void AgacYanitlari()
    {
        yanitlar["/urun-kategorileri"] = "[]";
        yanitlar["/reyonlar"] = """[{"id":"r1","subeId":"sb1","ad":"Ön Vitrin","tip":"VITRIN","sorumluKullaniciId":null,"aktif":true,"urunSayisi":3,"yerlesikAdet":12}]""";
        yanitlar["/subeler"] = """[{"id":"sb1","ad":"Merkez Şube"}]""";
    }

    [Fact]
    public void Q105_B06_Urun_agaci_basitte_Pasife_al_yerine_Kullanimdan_kaldir_th_tek_satir()
    {
        AgacYanitlari();
        Oturum();
        Basit();
        var e = Render<Kuyumcu.Web.Pages.UrunAgaci>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Contains("Ön Vitrin", e.Markup));
        var tablo = e.FindAll("table").First(t => t.TextContent.Contains("Ön Vitrin"));
        Assert.Contains("q10-th-tek", tablo.ClassName);
        Assert.DoesNotContain(Sozlukler.Tr.Cari.PasifeAl, tablo.TextContent);
        Assert.Contains(KatalogSozlugu.Tr.KullanimdanKaldir, tablo.TextContent);
    }

    [Fact]
    public void Q105_B07_Urun_agaci_profesyonelde_Pasife_al_korunur()
    {
        AgacYanitlari();
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.UrunAgaci>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Contains("Ön Vitrin", e.Markup));
        var tablo = e.FindAll("table").First(t => t.TextContent.Contains("Ön Vitrin"));
        Assert.Contains(Sozlukler.Tr.Cari.PasifeAl, tablo.TextContent);
    }

    [Fact]
    public void Q105_B08_Kilavuz_basitte_bolum_adlari_K8_temiz()
    {
        Basit();
        var e = Render<Kuyumcu.Web.Pages.Kilavuz>();
        var bolumler = e.Find("#kl-bolumler").TextContent;
        Assert.DoesNotMatch(K8, bolumler);
        Assert.Contains(Q105Sozlugu.Tr.KilavuzFaturaBolumuBasit, bolumler);
    }

    /// <summary>
    /// Q10-5 tur 3 (doğrulama KRİTİK): doğrudan açılışta mod SONRADAN çözülür
    /// (ilk çizim PRO, /auth/ben gelince BASİT). Sayfa DilServisi.Degisti'ye abone
    /// değilse "Cariler" ekranda kalır — gerçek tarayıcıda F5 ile görülen yarış.
    /// </summary>
    [Fact]
    public void Q105_B11_Kilavuz_mod_sonradan_cozulunce_Musteriler_olur()
    {
        var basit = false;
        var dil = Services.GetRequiredService<DilServisi>();
        dil.BasitModuBagla(() => basit);
        var e = Render<Kuyumcu.Web.Pages.Kilavuz>();
        Assert.Contains("Cariler", e.Find("#kl-bolum-cariler").TextContent);

        basit = true;          // oturum yüklendi → etkin mod BASİT
        dil.ModuDenetle();     // Program.cs: oturum.Degisti += dil.ModuDenetle

        e.WaitForAssertion(() =>
        {
            var bolumler = e.Find("#kl-bolumler").TextContent;
            Assert.Contains(KatalogSozlugu.Al("tr").Musteriler, e.Find("#kl-bolum-cariler").TextContent);
            Assert.DoesNotContain("Cariler", bolumler);
            Assert.DoesNotMatch(K8, bolumler);
        });
    }

    // ═══ C. TABLO BAŞLIĞI · MOBİL YIĞIN · CSS SÖZLEŞMESİ ═════════════════════

    [Fact]
    public void Q105_C01_Kullanicilar_tablosu_tek_satir_baslik_sinifi_tasir()
    {
        yanitlar["/kullanicilar"] = """[{"id":"u1","ad":"Demo","soyad":"Kuyumcu","email":"d@ornek.local","rol":"SAHIP","aktif":true,"sonGiris":null,"arayuzModu":null,"subeId":"sb1","mfaEtkin":false}]""";
        yanitlar["/subeler"] = """[{"id":"sb1","ad":"Merkez Şube"}]""";
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Kullanicilar>();
        e.WaitForAssertion(() => Assert.Contains("d@ornek.local", e.Markup));
        Assert.Contains("q10-th-tek", e.FindAll("table").First(t => t.TextContent.Contains("d@ornek.local")).ClassName);
    }

    [Fact]
    public void Q105_C02_Sayim_listesi_mobil_yigin_ve_her_hucre_etiketli()
    {
        yanitlar["/sayim"] = """[{"id":"sy1","ad":"Eylül sayımı vitrin","kapsamTur":"TUMU","durum":"ACIK"}]""";
        // TEMA TEMİZLİĞİ 17.10: Yıldız açık sayımı kendiliğinden açar — özgün listeye "Tüm ayrıntılar → ← Sayım" ile varılır.
        yanitlar["/sayim/"] = """{"id":"sy1","ad":"Eylül sayımı vitrin","subeId":"sb1","kapsamTur":"TUMU","durum":"ACIK","toplamSatir":0,"sayilan":0,"sayilmayan":0,"farkli":0,"eksikAdet":0,"fazlaAdet":0,"netFarkHas":0,"hareketli":0,"satirlar":[]}""";
        yanitlar["/subeler"] = """[{"id":"sb1","ad":"Merkez Şube"}]""";
        yanitlar["/reyonlar"] = "[]";
        yanitlar["/urun-kategorileri/duz"] = "[]";
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Sayim>().SayimOzgunListe();
        e.WaitForElement("#sy-tablo tbody tr");
        var tablo = e.Find("#sy-tablo");
        // PAKET GOC-C: liste ortak ızgarada — 375 px kart kalıbı ızgaranın TEK kalıbıdır (izgara.css, data-etiket).
        Assert.Contains("izgara-tablo", tablo.ClassName);
        var hucreler = tablo.QuerySelectorAll("tbody td");
        Assert.Equal(4, hucreler.Length);
        Assert.Equal(Sozlukler.Tr.Sayim.Ad, hucreler[0].GetAttribute("data-etiket"));
        Assert.Equal(Sozlukler.Tr.Sayim.Kapsam, hucreler[1].GetAttribute("data-etiket"));
        Assert.Equal(Sozlukler.Tr.Sayim.Durum, hucreler[2].GetAttribute("data-etiket"));
        Assert.Equal("eylem", hucreler[3].GetAttribute("data-kolon"));   // eylem hücresi kart düzeninde etiketsiz
        Assert.Single(e.FindAll("#sy-durum-sy1"));   // mevcut id DEĞİŞMEDİ
    }

    private static string Q10Css()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        Assert.NotNull(d);
        return File.ReadAllText(Path.Combine(d!.FullName, "Kuyumcu.Web", "wwwroot", "css", "q10.css"));
    }

    [Fact]
    public void Q105_C03_q10_css_baslik_nowrap_mobil_yigin_ve_karsi_taraf_kurallari_var_sabit_renk_yok()
    {
        var css = Q10Css();
        Assert.Matches(@"\.veri-tablosu\.q10-th-tek\s*>\s*thead\s*>\s*tr\s*>\s*th\s*\{\s*white-space:\s*nowrap;", css);
        Assert.Matches(@"@media \(max-width: 40rem\)[\s\S]*\.veri-tablosu\.q10-yigin[\s\S]*display: block", css);
        Assert.Matches(@"td\[data-etiket\]::before\s*\{[^}]*content:\s*attr\(data-etiket\)", css);
        Assert.Matches(@"\.q10-karsi\s*\{[^}]*flex-direction:\s*column", css);
        // Q10-5 tur 3 (gerçek tarayıcı 375: tablo 1042 / 349): yığın app.css max-content'ini sıfırlar.
        Assert.Matches(@"@media \(max-width: 40rem\)[\s\S]*\.veri-tablosu\.q10-yigin\s*\{\s*min-width:\s*0;\s*\}", css);
        // Q10-5 tur 3: seçimli yığında ilk başlık hücresi ("Tümünü seç") mobilde çizilir.
        Assert.Matches(@"@media \(max-width: 40rem\)[\s\S]*\.q10-yigin-secim\s*>\s*thead\s*>\s*tr\s*>\s*th:first-child\s*\{[^}]*display:\s*flex", css);
        // Doğrulama bulgusu (1280 PRO, tablo 1177 px / sarmal 962 px): ekstre
        // sütunlarının en-az genişlikleri toplamı 'Atla'yı görünür alan dışına
        // itiyordu. Üç sütunun en-az toplamı 28rem'i aşmaz (eski: 42rem).
        decimal EnAz(string sinif)
        {
            var m = System.Text.RegularExpressions.Regex.Match(css, @"\." + sinif + @"\s*\{[^}]*min-width:\s*([0-9.]+)rem");
            Assert.True(m.Success, sinif);
            return decimal.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        }
        Assert.True(EnAz("q10-karsi") + EnAz("q10-ekstre-cari") + EnAz("q10-ekstre-eylem") <= 28m);
        Assert.True(EnAz("q10-ekstre-eylem") <= 8m);
        // Sabit renk YOK: yalnız belirteç (var(--…)).
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgba?\(|hsla?\(", css);
    }
}
