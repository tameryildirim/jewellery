using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-16 — EKRAN TESTLERİ [bUnit]
// Kimlikler: Y-4 (kalem satırı + iskonto) · Y-26 (ürün kartı) · Y-27 (cari kartı)
//            B-31 (pasif cari) · B-69 (sistemin bildiği alan · UX-S21)
//
// SÖZLEŞME: alan VARLIĞI yetmez — gittiği UÇ ve GÖVDE ölçülür (DoD EK md.1).
// Sahte API'de '{}' varsayılanı bırakılmadı: bu pakette dokunulan her uca
// GERÇEKÇİ yanıt verildi, aksi hâlde ekran sessizce boş kalır ve test
// "geçmiş gibi" görünürdü.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P16EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Sorgu, string Govde)> istekler = [];

    public P16EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<KabukServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi(List<(string, string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var sorgu = istek.RequestUri!.Query;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, sorgu, govde));

            var g = yol switch
            {
                // ── cari: liste + tam kart (P-16 alanları DAHİL) ─────────────
                var y when y.EndsWith("/cari/c1") =>
                    """{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567","email":null,"vergiNo":"1234567890","adres":"Şişli","krediLimitiTl":null,"varsayilanVadeGun":null,"fiyatListesiId":null,"notlar":null,"pepBeyani":false,"pepGorev":null,"pepBeyanTarihi":null,"unvan":"Demir Kuyumculuk Ltd.","kimlikTuru":"VKN","dogumTarihi":"1985-04-12","uyruk":"TR","iskontoYuzde":7.5,"segment":"VIP"}""",
                var y when y.EndsWith("/cari") =>
                    """[{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567","tlBakiye":-1500.00,"hasGramBakiye":2.5,"aktif":true}]""",

                // ── ürün: liste (Islemler ayar/milyem/gram/has okur) + tam kart
                var y when y.EndsWith("/urunler/u1") =>
                    """{"id":"u1","tip":"ZIYNET","kod":"Z-1","ad":"Tek taş","ayarId":"K22","gramAgirlik":5.0,"tasAgirlik":0.0,"iscilikTl":0,"barkod":"B1","tasKokeni":"TAS_YOK","tasIbaresi":null,"kategoriId":null,"reyonId":null,"satisFiyati":null,"birim":"ADET","aktif":true,"tedarikciId":"c1","alisMaliyeti":12500.50,"hedefMarj":22.5,"kdvOrani":20,"seriNo":"SN-001","olcu":"17","minStok":3}""",
                var y when y.EndsWith("/urunler") =>
                    """[{"id":"u1","tip":"ZIYNET","kod":"Z-1","ad":"Tek taş","ayarId":"K22","ayar":"K22","milyem":916.00,"gramAgirlik":5.0,"tasAgirlik":0.0,"hasGrami":4.58,"iscilikTl":0,"satisFiyati":null,"birim":"ADET","barkod":"B1","aktif":true}]""",

                // ── yan kaynaklar
                var y when y.EndsWith("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                var y when y.EndsWith("/urun-kategorileri") => "[]",
                var y when y.Contains("/fiyat/listeler") =>
                    """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","aktif":true}]""",
                var y when y.Contains("/fiyat/teklif") =>
                    """{"teklifVar":true,"sebep":null,"listeFiyati":10000.00,"hasDegeri":null,"marjTl":null,"iscilikTl":null,"indirimTutari":null,"satisFiyati":10000.00,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur1","altinHasFiyati":2500.00,"usdTry":40,"eurTry":44,"gbpTry":52}""",
                var y when y.EndsWith("/kur/pariteler") => """{"pariteler":[]}""",
                var y when y.EndsWith("/admin/tenant") =>
                    """{"id":"t1","ad":"Kuyumcu","plan":"PRO","aiEtkin":false,"aktif":true,"varsayilanArayuzModu":"PROFESYONEL"}""",
                var y when y.EndsWith("/admin/vezne") => """{"zorunlu":false}""",
                var y when y.Contains("/masak/esikler") =>
                    """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var y when y.Contains("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var y when y.Contains("/pos/cihazlar") =>
                    """[{"id":"pc1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"s1","kasaId":"ks1","pesinKomisyonOran":1.5,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}]""",

                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private bool Gitti(string metot, string yolParcasi) =>
        istekler.Any(i => i.Metot == metot && i.Yol.Contains(yolParcasi));

    // ═══ B-31 — POS CARİ LİSTESİ YALNIZ AKTİFLERİ ÇEKER ═════════════════════

    [Fact]
    public void Islemler_cari_listesini_YALNIZ_aktif_suzgeciyle_ceker()
    {
        // TEMA TEMİZLİĞİ 17.10: SATIŞ = Yıldız tezgâhı; müşteri listesi aynı istekten (aktif=true) gelir.
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();

        // Sorgu dizesinde aktif=true YOKSA pasif cari POS'ta seçilebilir olurdu.
        e.WaitForAssertion(() => Assert.Contains(
            istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/cari") && i.Sorgu.Contains("aktif=true")));

        // UX-S29/S30: listenin neden eksik olabileceği EKRANDA yazar — müşteri satırı (FisMusteriSatiri) Yıldız'da
        // öteki türlerin formunda çizilir (tezgâhın "Değiştir" katmanı tuval dilinde, not satırı yok).
        TemaTemizligiGDYardimcisi.TurSec(e, "ALIS");
        Assert.Contains("Pasif cariler", e.WaitForElement("#islem-cari-pasif-notu").TextContent);
    }

    // ═══ Y-4 — KALEM SATIRINDA AYAR / MİLYEM / GRAM / HAS ═══════════════════

    [Fact]
    public void Kalem_satiri_ayar_milyem_gram_ve_has_GOSTERIR()
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#islem-urun");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForElement("#islem-kalem-ekle").Click();

        // TEMA TEMİZLİĞİ 17.10: Yıldız tezgâh kalem satırı (tuval Tezgah4): [ayar] [ad · adet · gram · has] [tutar].
        // Milyem tuvalin kalem satırında YOK (bilerek); ayar/gram/has güvencesi aynen.
        var satir = e.WaitForElement("#yildiz-kalem-0").TextContent;
        // ayar ham kod değil ("K22" → "22", UX-S31), gram/has tr-TR.
        Assert.Equal("22", e.Find("#yildiz-kalem-0 .yz-tezgah-ayar").TextContent.Trim());   // ayar
        Assert.DoesNotContain("K22", satir);
        Assert.Contains("5,00", satir);       // gram
        Assert.Contains("4,58", satir);       // has
    }

    /// <summary>
    /// B-69 / UX-S21 — sistem liste fiyatını BİLİR: birim fiyat alanı sunucudan
    /// doldurulur ve değerin NEREDEN geldiği rozetle söylenir.
    /// </summary>
    [Fact]
    public void Birim_fiyat_sunucudan_DOLDURULUR_ve_kaynagi_rozetle_yazar()
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#islem-urun");
        SeciciYardim.Sec(e, "islem-urun", "u1");

        e.WaitForAssertion(() => Assert.True(Gitti("GET", "/fiyat/teklif")));
        // ⚠ KARARSIZLIK ÖNLEMİ (P-36c/2): teklif yanıtı geldikten SONRA bir
        //   render daha olur; değeri tek seferde okumak yüklü makinede yarışır.
        //   Doğrulayıcı bu testi kararsız kırmızı olarak yakalamıştı.
        e.WaitForAssertion(
            // P-36c/3: kutu değeri tek yerden biçimlenir (Bicim.Makine) — anlamsız
            //   sondaki sıfırlar taşınmaz; SAYI aynıdır.
            () => Assert.Equal("10.000", e.Find("#islem-birimfiyat").GetAttribute("value")),   // BICIM-2: odak dışı kutu gruplu
            TimeSpan.FromSeconds(5));
        var kutu = e.Find("#islem-birimfiyat");
        Assert.Contains("fiyat listesinden geldi", e.Find("#islem-fiyat-kaynak").TextContent);
    }

    /// <summary>
    /// Y-4 — fiş iskontosu kutusu vardır ve girilen YÜZDE gövdeye YÜZDE olarak
    /// gider (tutar DEĞİL). İskonto girilen satış TEK KALEMLİ uca değil,
    /// iskontoyu hesaplayan FİŞ ucuna gider.
    /// </summary>
    [Fact]
    public void Fis_iskontosu_YUZDE_olarak_FIS_ucuna_gonderilir()
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#islem-urun");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForElement("#islem-kalem-ekle").Click();
        TemaTemizligiGDYardimcisi.KalemSayisiBekle(e, 1);
        TemaTemizligiGDYardimcisi.MusteriSec(e, "c1");
        // Yıldız: "+ İndirim" fiş iskontosu alanını açar (FisIskontoSatiri — aynı alan, aynı oran kuralı)
        if (e.FindAll("#islem-fis-iskonto").Count == 0) e.Find("#yildiz-tezgah-indirim").Click();
        e.WaitForElement("#islem-fis-iskonto").Input("10");
        istekler.Clear();

        TemaTemizligiGDYardimcisi.KaydetAcikBekle(e);
        e.Find(TemaTemizligiGDYardimcisi.Kaydet).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);

        e.WaitForAssertion(() => Assert.True(Gitti("POST", "/islemler/satis-fisi")));
        var govde = istekler.Single(i => i.Metot == "POST" && i.Yol.Contains("satis-fisi")).Govde;
        Assert.Contains("\"fisIskontoYuzde\":10", govde.Replace(" ", ""));
        Assert.Contains("satirIskontoYuzde", govde);
        // Tutar ASLA gönderilmez: hesap sunucudadır.
        Assert.DoesNotContain("iskontoTl", govde);
        // ⚠ "satis-fisi" da "/islemler/satis" ile başlar: TAM eşleşme aranır.
        Assert.False(istekler.Any(i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis")),
            "iskonto varken tek kalemli uca gidilemez");
    }

    // ═══ Y-26 — ÜRÜN KARTI: OLUŞTURMA VE DÜZENLEME AYNI KÜME (UX-S26) ══════

    [Fact]
    public void Urun_olusturma_formunda_yedi_kart_alani_VAR_ve_GONDERILIR()
    {
        var e = Render<Kuyumcu.Web.Pages.Urunler>().AyrintiAc();
        e.WaitForElement("#urun-yeni-ac").Click();
        e.Find("#u-daha-fazla").Click();   // PAKET GOC-C (devir "zorunlu önce"): yeni ürün formu PRO'da da yalnız zorunlularla açılır — isteğe bağlılar "Daha fazla"da

        foreach (var id in new[] { "#u-tedarikci", "#u-alis-maliyeti", "#u-hedef-marj",
                                   "#u-kdv-orani", "#u-seri-no", "#u-olcu", "#u-min-stok" })
            Assert.NotNull(e.WaitForElement(id));

        e.Find("#u-ad").Input("Yeni ürün");
        e.Find("#u-kod").Input("K-1");
        e.Find("#u-gram").Input("5");
        SeciciYardim.Sec(e, "u-ayar", "K18");   // P-URUN (K6): ayarın varsayılanı yok — kullanıcı seçer
        e.Find("#u-alis-maliyeti").Input("1250.75");
        e.Find("#u-seri-no").Input("SN-9");
        istekler.Clear();
        e.Find("#u-kaydet").Click();

        e.WaitForAssertion(() => Assert.True(Gitti("POST", "/urunler")));
        var govde = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/urunler")).Govde;
        Assert.Contains("alisMaliyeti", govde);
        Assert.Contains("SN-9", govde);
        Assert.Contains("minStok", govde);
    }

    [Fact]
    public void Urun_duzenleme_cekmecesi_kart_alanlarini_OKUR_ve_PATCH_ile_GONDERIR()
    {
        var e = Render<Kuyumcu.Web.Pages.Urunler>().AyrintiAc();
        e.WaitForElement("#urun-yeni-ac");
        e.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();

        e.WaitForAssertion(() => Assert.True(Gitti("GET", "/urunler/u1")));
        // P-36c/3: sayısal kutu değeri tek yerden biçimlenir (Bicim.Makine);
        // ondalık sondaki anlamsız sıfır taşınmaz — SAYI aynı, gösterim tek tip.
        // P-78: girdi kutusu değeri tr-TR gruplamasız basılır (virgül ondalık); eski
        // Invariant yazım ("1.234") tr-TR çözücüde BELİRSİZ sayılıp kutu kendi değerini
        // geri okuyamazdı. Değer aynı, yazım değişti.
        // BICIM-2: odak DIŞINDA para kutusu gruplu, kuruş iki hane.
        Assert.Equal("12.500,50", e.WaitForElement("#d-alis-maliyeti").GetAttribute("value"));
        Assert.Equal("SN-001", e.Find("#d-seri-no").GetAttribute("value"));
        Assert.Equal("17", e.Find("#d-olcu").GetAttribute("value"));

        e.Find("#d-alis-maliyeti").Input("9000");
        istekler.Clear();
        e.Find("#cekmece-urun-kaydet").Click();

        e.WaitForAssertion(() => Assert.True(Gitti("PATCH", "/urunler/u1")));
        var govde = istekler.Single(i => i.Metot == "PATCH").Govde.Replace(" ", "");
        Assert.Contains("\"alisMaliyeti\":9000", govde);
        Assert.Contains("\"seriNo\":\"SN-001\"", govde);   // görünen alan KORUNARAK gider
    }

    // ═══ Y-27 — CARİ KARTI: KİMLİK ALANLARI + TCKN/VKN HANE DOĞRULAMASI ════

    [Fact]
    public void Cari_olusturma_formunda_kimlik_alanlari_VAR_ve_GONDERILIR()
    {
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("#cari-yeni-ac").Click();

        foreach (var id in new[] { "#cari-kimlik-turu", "#cari-unvan", "#cari-dogum",
                                   "#cari-uyruk", "#cari-iskonto", "#cari-segment" })
            Assert.NotNull(e.WaitForElement(id));

        e.Find("#cari-ad").Input("Yeni Müşteri");
        SeciciYardim.Sec(e, "cari-kimlik-turu", "VKN");
        e.Find("#cari-vergino").Input("1234567890");
        e.Find("#cari-iskonto").Input("12.5");
        SeciciYardim.Sec(e, "cari-segment", "TOPTAN");
        istekler.Clear();
        e.Find("#cari-kaydet").Click();

        e.WaitForAssertion(() => Assert.True(Gitti("POST", "/cari")));
        var govde = istekler.Single(i => i.Metot == "POST").Govde.Replace(" ", "");
        Assert.Contains("\"kimlikTuru\":\"VKN\"", govde);
        Assert.Contains("\"iskontoYuzde\":12.5", govde);
        Assert.Contains("\"segment\":\"TOPTAN\"", govde);
    }

    /// <summary>
    /// PLAN P-16 DoD: "TCKN 11 / VKN 10 hane doğrulaması ALAN ALTINDA".
    /// Sessiz etkisizlik yasak: istek ATILMAZ ama sebep alanın altında YAZAR.
    /// </summary>
    [Fact]
    public void Eksik_haneli_TCKN_alan_altinda_mesaj_verir_ve_istek_ATILMAZ()
    {
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("#cari-yeni-ac").Click();

        e.Find("#cari-ad").Input("Hatalı Kimlik");
        SeciciYardim.Sec(e, "cari-kimlik-turu", "TCKN");
        e.Find("#cari-vergino").Input("1234567890");   // 10 hane — TCKN 11 ister
        istekler.Clear();
        e.Find("#cari-kaydet").Click();

        e.WaitForAssertion(() =>
            Assert.Contains("11 hane", e.Find("#cari-p16").ParentElement!.TextContent));
        Assert.False(istekler.Any(i => i.Metot == "POST"), "geçersiz kimlikle istek atılmamalı");
    }

    [Fact]
    public void Cari_duzenleme_cekmecesi_kimlik_alanlarini_OKUR()
    {
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("#cari-yeni-ac");
        // TEMA TEMİZLİĞİ 17.10: liste = YildizMusteriler — satıra dokununca kart (özgün düzenleme çekmecesi, DuzenleAc).
        e.WaitForElement("#yildiz-musteri-c1").Click();

        e.WaitForAssertion(() => Assert.True(Gitti("GET", "/cari/c1")));
        Assert.Equal("Demir Kuyumculuk Ltd.", e.WaitForElement("#duz-unvan").GetAttribute("value"));
        Assert.Equal("VKN", e.Find("#duz-kimlik-turu").Deger());
        // P-78: girdi kutusu değeri tr-TR gruplamasız basılır (virgül ondalık); eski
        // Invariant yazım ("1.234") tr-TR çözücüde BELİRSİZ sayılıp kutu kendi değerini
        // geri okuyamazdı. Değer aynı, yazım değişti.
        Assert.Equal("7,5", e.Find("#duz-iskonto").GetAttribute("value"));
        Assert.Equal("VIP", e.Find("#duz-segment").Deger());
    }
}
