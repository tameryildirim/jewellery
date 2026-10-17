using System.Text.Json;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-10 (Y-3, Y-8) — EKRAN TARAFI
//
// Y-3: sunucunun KABUL ETTİĞİ ama ekranın GÖNDERMEDİĞİ yedi alan
//      (ürün RFID · KMTS · görsel URL · açıklama · parite · cari notlar ·
//       POS cihazı + kart taksiti) forma bağlanır.
// Y-8: ürün görseli/galerisi ekranda GÖRÜNÜR (GORSEL/FOTOGRAF belgeleri).
//
// "Alan var" yetmez: hangi uca hangi GÖVDEYLE gidildiği okunur (Türkçe JSON
// gövdesi AYRIŞTIRILARAK sınanır, dizge araması yapılmaz).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P10UrunEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string Liste = """
        [{"id":"u1","tip":"ZIYNET","kod":"BLZ-01","ad":"Burma Bilezik","ayarId":"K22",
          "gramAgirlik":15.5,"tasAgirlik":0,"hasGrami":14.19,"barkod":"869001","aktif":true,
          "gorselUrl":"https://ornek.test/bilezik.jpg"},
         {"id":"u2","tip":"ZIYNET","kod":"BLZ-02","ad":"Düz Bilezik","ayarId":"K22",
          "gramAgirlik":10,"tasAgirlik":0,"hasGrami":9.16,"barkod":"869002","aktif":true}]
        """;

    // ⚠ Tam kart P-10'un beş alanını da taşır; çekilmezse düzenleme formu
    //   onları BOŞ gönderir ve sunucudaki değeri SİLER.
    private const string TamKart = """
        {"id":"u1","tip":"ZIYNET","kod":"BLZ-01","ad":"Burma Bilezik","ayarId":"K22",
         "gramAgirlik":15.5,"tasAgirlik":0,"iscilikTl":250,"barkod":"869001",
         "tasKokeni":"TAS_YOK","kategoriId":null,"reyonId":null,"satisFiyati":null,
         "aciklama":"Vitrin 2","rfid":"RF-9","kmtsKodu":"KMTS-5",
         "gorselUrl":"https://ornek.test/bilezik.jpg","pariteKodu":"BILEZIK_22"}
        """;

    private const string Pariteler = """
        {"kaynak":"HARemALTIN","pariteler":[
          {"kod":"BILEZIK_22","ad":"22 Ayar Bilezik","alis":6800,"satis":6900,"birim":"GRAM"},
          {"kod":"CEYREK_YENI","ad":"Yeni Çeyrek","alis":11000,"satis":11200,"birim":"ADET"}]}
        """;

    private const string UrunBelgeleri = """
        [{"id":"b1","ad":"vitrin.jpg","tur":"GORSEL","mime":"image/jpeg","boyut":1200,"sha256":"aa"},
         {"id":"b2","ad":"sertifika.pdf","tur":"SERTIFIKA","mime":"application/pdf","boyut":900,"sha256":"bb"}]
        """;

    public P10UrunEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();   // S3-3: Urunler.razor basit/profesyonel mod okur
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            // ⚠ "{}" varsayılanı YENİ uçlara BIRAKILMAZ: gerçekçi yanıt verilir,
            //   aksi hâlde ekran JsonException yolundan geçer ve test yalancı yeşil olur.
            var y = yol switch
            {
                // PAKET YASAL-4 / Y4-C: KMTS alanı kiracı ayarı açıkken (PRO, "Ek bilgiler" katlanırı) çizilir.
                var v when v.EndsWith("/kmts/ayar") => """{"acik":true,"bicimDenetimi":false}""",
                var v when v.Contains("/kur/pariteler") => Pariteler,
                var v when v.Contains("/belgeler/") && v.EndsWith("/icerik") => "GORSEL-BAYTLARI",
                var v when v.Contains("/belgeler") => UrunBelgeleri,
                var v when v.EndsWith("/urunler/madenler") => """
                    {"madenler":[{"maden":"ALTIN","madenAdi":"Altın","rodyumKaplanabilir":true,
                       "ayarlar":[{"id":"K22","ad":"22 Ayar","milyem":916,"kiymetli":true,"hasHesaplanir":true}]}],
                     "etiket":{"rodyumIbaresiGoster":false,"rodyumIbaresi":"rodyum kaplama"}}
                    """,
                var v when v.Contains("/urun-kategorileri") => "[]",
                var v when v.Contains("/reyonlar") => "[]",
                var v when v.EndsWith("/cari") || v.Contains("/cari?") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.EndsWith("/urunler/u1") => TamKart,
                var v when v.Contains("/urunler") => Liste,
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Urunler> Ciz()
    {
        var e = Render<Kuyumcu.Web.Pages.Urunler>().AyrintiAc();
        istekler.Clear();
        return e;
    }

    private static System.Text.Json.JsonElement Coz(string g)
        => System.Text.Json.JsonDocument.Parse(g).RootElement;

    private static string? Alan(string govde, string ad)
    {
        var e = Coz(govde);
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    // ── Y-3 · OLUŞTURMA FORMU ────────────────────────────────────────────────

    [Fact]
    public void Yeni_urun_formunda_BES_ALAN_da_vardir()
    {
        var e = Ciz();
        e.Find("#urun-yeni-ac").Click();
        e.Find("#u-daha-fazla").Click();   // PAKET GOC-C (devir "zorunlu önce"): yeni ürün formu PRO'da da yalnız zorunlularla açılır — isteğe bağlılar "Daha fazla"da
        e.WaitForElement("#u-ek-bilgiler-ac").Click();   // YASAL-4: KMTS kapalı katlanırda
        foreach (var id in new[] { "#u-aciklama", "#u-parite", "#u-kmts", "#u-rfid", "#u-gorsel-url" })
            Assert.NotNull(e.Find(id));
    }

    [Fact]
    public void Yeni_urun_kaydinda_BES_ALAN_da_GOVDEDE_gider()
    {
        var e = Ciz();
        e.Find("#urun-yeni-ac").Click();
        e.Find("#u-daha-fazla").Click();   // PAKET GOC-C (devir "zorunlu önce"): yeni ürün formu PRO'da da yalnız zorunlularla açılır — isteğe bağlılar "Daha fazla"da
        e.Find("#u-ad").Input("P10 Ürün");
        e.Find("#u-kod").Input("P10-X");
        e.Find("#u-gram").Input("5");
        SeciciYardim.Sec(e, "u-ayar", "K18");   // P-URUN (K6): ayarın varsayılanı yok — kullanıcı seçer
        e.Find("#u-aciklama").Input("Vitrin 3");
        SeciciYardim.Sec(e, "u-parite", "CEYREK_YENI");
        e.WaitForElement("#u-ek-bilgiler-ac").Click();   // YASAL-4: KMTS kapalı katlanırda
        e.Find("#u-kmts").Input("KMTS-11");
        e.Find("#u-rfid").Input("RF-11");
        e.Find("#u-gorsel-url").Input("https://ornek.test/x.jpg");
        e.Find("#u-kaydet").Click();

        var (_, yol, govde) = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/urunler"));
        Assert.EndsWith("/urunler", yol);
        Assert.Equal("Vitrin 3", Alan(govde, "aciklama"));
        Assert.Equal("CEYREK_YENI", Alan(govde, "pariteKodu"));
        Assert.Equal("KMTS-11", Alan(govde, "kmtsKodu"));
        Assert.Equal("RF-11", Alan(govde, "rfid"));
        Assert.Equal("https://ornek.test/x.jpg", Alan(govde, "gorselUrl"));
    }

    [Fact]
    public void Parite_secenekleri_TEK_KAYNAKTAN_gelir()
    {
        // UX-S31: sabit liste kopyalanmaz, GET /kur/pariteler okunur.
        var e = Render<Kuyumcu.Web.Pages.Urunler>().AyrintiAc();
        // PAKET ISTEK-1: parite listesi YALNIZ formda kullanılır → form ilk
        // açıldığında çekilir (liste ekranı her açılışta çekmez); kaynak yine TEK.
        Assert.DoesNotContain(istekler, i => i.Metot == "GET" && i.Yol.Contains("/kur/pariteler"));
        e.Find("#urun-yeni-ac").Click();
        e.Find("#u-daha-fazla").Click();   // PAKET GOC-C (devir "zorunlu önce"): yeni ürün formu PRO'da da yalnız zorunlularla açılır — isteğe bağlılar "Daha fazla"da
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("/kur/pariteler")));
        var kodlar = SeciciYardim.Secenekler(e, "u-parite")
            .Select(o => o.Deger()).ToList();
        Assert.Contains("BILEZIK_22", kodlar);
        Assert.Contains("CEYREK_YENI", kodlar);
    }

    [Fact]
    public void RFID_alaninin_altinda_DONANIM_KAPSAM_DISI_beyani_yazar()
    {
        // Plan § 2 P-10 riski + kurucu kalemi K-21. KESIF-P8 (karar 2): beyan düz cümledir,
        // iç kalem kodu "K-21" görünür metinde YOKTUR.
        var e = Ciz();
        e.Find("#urun-yeni-ac").Click();
        e.Find("#u-daha-fazla").Click();   // PAKET GOC-C (devir "zorunlu önce"): yeni ürün formu PRO'da da yalnız zorunlularla açılır — isteğe bağlılar "Daha fazla"da
        var metin = e.Find("#u-rfid-ipucu").TextContent;
        Assert.Contains("RFID okuyucu bağlantısı yoktur", metin);
        Assert.DoesNotContain("K-21", metin);
        Assert.DoesNotContain("DIŞINDADIR", metin);
    }

    [Fact]
    public void GECERSIZ_gorsel_adresi_ISTEK_ATMAZ_ve_alan_alti_mesaj_verir()
    {
        var e = Ciz();
        e.Find("#urun-yeni-ac").Click();
        e.Find("#u-daha-fazla").Click();   // PAKET GOC-C (devir "zorunlu önce"): yeni ürün formu PRO'da da yalnız zorunlularla açılır — isteğe bağlılar "Daha fazla"da
        e.Find("#u-ad").Input("P10 Ürün");
        e.Find("#u-kod").Input("P10-Y");
        e.Find("#u-gram").Input("5");
        SeciciYardim.Sec(e, "u-ayar", "K18");   // P-URUN (K6): ayarın varsayılanı yok — kullanıcı seçer
        e.Find("#u-gorsel-url").Input("javascript:alert(1)");
        e.Find("#u-kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.Contains("http", e.Find("#alan-hata-u-gorsel-url").TextContent);
    }

    // ── Y-3 · DÜZENLEME FORMU (UX-S26: AYNI alan kümesi) ─────────────────────

    [Fact]
    public void Duzenleme_cekmecesi_OLUSTURMA_ile_AYNI_alan_kumesini_tasir()
    {
        var e = Ciz();
        e.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();
        e.WaitForElement("#d-aciklama");
        e.WaitForElement("#d-ek-bilgiler-ac").Click();   // YASAL-4: KMTS kapalı katlanırda
        foreach (var id in new[] { "#d-aciklama", "#d-parite", "#d-kmts", "#d-rfid", "#d-gorsel-url" })
            Assert.NotNull(e.Find(id));
    }

    [Fact]
    public void Duzenleme_TAM_KARTTAKI_bes_alani_OKUR()
    {
        var e = Ciz();
        e.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();
        e.WaitForElement("#d-rfid");
        Assert.Equal("Vitrin 2", e.Find("#d-aciklama").GetAttribute("value"));
        Assert.Equal("RF-9", e.Find("#d-rfid").GetAttribute("value"));
        e.WaitForElement("#d-ek-bilgiler-ac").Click();   // YASAL-4: KMTS kapalı katlanırda
        Assert.Equal("KMTS-5", e.Find("#d-kmts").GetAttribute("value"));
        Assert.Equal("https://ornek.test/bilezik.jpg", e.Find("#d-gorsel-url").GetAttribute("value"));
    }

    [Fact]
    public void Duzenlemede_BOSALTILAN_alan_GOVDEDE_NULL_gider_silme_kararini_tasir()
    {
        var e = Ciz();
        e.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();
        e.WaitForElement("#d-rfid");
        e.Find("#d-rfid").Input("");
        e.Find("#d-gorsel-url").Input("");
        istekler.Clear();
        e.Find("#cekmece-urun-kaydet").Click();

        var (_, _, govde) = istekler.Single(i => i.Metot == "PATCH");
        var kok = Coz(govde);
        // PAKET PATCH-3: isteğe bağlı alan sözleşmesi "gövdede var + null = SİL" (eskiden "").
        // Anahtar GÖVDEDE OLMALI (yoksa sunucu korur) ve değeri null olmalı.
        Assert.Equal(JsonValueKind.Null, kok.GetProperty("rfid").ValueKind);
        Assert.Equal(JsonValueKind.Null, kok.GetProperty("gorselUrl").ValueKind);
    }

    // ── PATCH-3 tur 2 (tasarım hakemi KRİTİK) — "isteğe bağlı" yazıp boş Kaydet'i
    //    sessizce reddetmek yok: zorunlu alan etiketi zorunlu, boş gönderimde alan altı
    //    mesaj + aria-invalid, istek ÇIKMAZ (DoD EK md.3).

    private static void ZorunluEtiketli(IRenderedComponent<Kuyumcu.Web.Pages.Urunler> e, string id)
    {
        var sar = e.Find($"#{id}-sar");
        Assert.NotNull(sar.QuerySelector(".zorunlu-isaret"));
        Assert.Null(sar.QuerySelector(".alan-istege"));
        Assert.Equal("true", e.Find($"#{id}").GetAttribute("aria-required"));
    }

    [Fact]
    public void Duzenlemede_KOD_ZORUNLU_isaretli_bosaltilinca_ALAN_ALTI_mesaj_PATCH_YOK()
    {
        var e = Ciz();
        e.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();
        e.WaitForElement("#d-kod");
        ZorunluEtiketli(e, "d-kod");
        e.Find("#d-kod").Input("");
        istekler.Clear();
        e.Find("#cekmece-urun-kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
        Assert.Equal("true", e.Find("#d-kod").GetAttribute("aria-invalid"));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-d-kod").TextContent));
    }

    [Fact]
    public void Duzenlemede_ADET_biriminde_GRAM_ZORUNLU_isaretli_bos_kayitta_ALAN_ALTI_mesaj_PATCH_YOK()
    {
        var e = Ciz();
        e.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();
        e.WaitForElement("#d-gram");
        ZorunluEtiketli(e, "d-gram");
        e.Find("#d-gram").Input("");
        istekler.Clear();
        e.Find("#cekmece-urun-kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
        Assert.Equal("true", e.Find("#d-gram").GetAttribute("aria-invalid"));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-d-gram").TextContent));
    }

    // ── Y-8 · GÖRSEL ─────────────────────────────────────────────────────────

    [Fact]
    public void Listede_KAPAK_GORSELI_cizilir_yoksa_sebebi_yazar()
    {
        var e = Ciz();
        Assert.Equal("https://ornek.test/bilezik.jpg", e.Find("#u-gorsel-u1").GetAttribute("src"));
        Assert.NotNull(e.Find("#u-gorsel-yok-u2"));
    }

    [Fact]
    public void Galeri_yuklenen_GORSEL_belgelerini_gosterir_PDF_gostermez()
    {
        var e = Ciz();
        e.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();
        e.WaitForElement("#d-urun-galeri-liste");

        // b1 GORSEL → var; b2 SERTIFIKA (pdf) → YOK; ayrıca kart adresi kapak karesi.
        Assert.NotNull(e.Find("#d-urun-galeri-b1"));
        Assert.Empty(e.FindAll("#d-urun-galeri-b2"));
        Assert.NotNull(e.Find("#d-urun-galeri-kapak"));
        Assert.StartsWith("data:image/jpeg;base64,", e.Find("#d-urun-galeri-b1 img").GetAttribute("src"));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// CARİ NOTLARI (Y-3, yedinci alan)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P10CariNotEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    // Şekil CariEkranTestleri ile AYNIDIR (sunucunun gerçek yanıtı: düz dizi).
    private const string Liste = """
        [{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":null,"email":null,
          "tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false}]
        """;

    private const string Detay = """
        {"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":null,"email":null,
         "vergiNo":null,"adres":null,"krediLimitiTl":0,"varsayilanVadeGun":0,
         "fiyatListesiId":null,"pepBeyani":false,"notlar":"Eski müşteri"}
        """;

    public P10CariNotEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<OturumServisi>();
        // PAKET S3-2: Cariler artık arayüz modunu okur (basit modda satır/form katlama).
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            var y = yol switch
            {
                var v when v.Contains("/fiyat/listeler") => "[]",
                var v when v.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.EndsWith("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800}""",
                var v when v.Contains("/ekstre") => """{"bakiye":{"tlBakiye":0,"hasGramBakiye":0},"hareketler":[]}""",
                var v when v.EndsWith("/cari/c1") => Detay,
                var v when v.EndsWith("/cari") => Liste,
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private static string? Alan(string govde, string ad)
    {
        var e = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    [Fact]
    public void Yeni_cari_formunda_NOT_alani_vardir_ve_govdede_gider()
    {
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.Find("#cari-yeni-ac").Click();
        e.WaitForElement("#cari-notlar");
        e.Find("#cari-ad").Input("P10 Müşteri");
        e.Find("#cari-notlar").Input("Pazarlıkta zorlu");
        istekler.Clear();
        e.Find("#cari-kaydet").Click();

        var (_, _, govde) = istekler.Single(i => i.Metot == "POST");
        Assert.Equal("Pazarlıkta zorlu", Alan(govde, "notlar"));
    }

    [Fact]
    public void Cari_duzenlemesi_mevcut_NOTU_okur_ve_geri_gonderir()
    {
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        // TEMA TEMİZLİĞİ 17.10: liste = YildizMusteriler — satıra dokununca kart (özgün düzenleme çekmecesi, DuzenleAc).
        e.WaitForElement("#yildiz-musteri-c1").Click();
        e.WaitForElement("#duz-notlar");
        Assert.Contains("Eski müşteri", e.Find("#duz-notlar").OuterHtml);

        e.Find("#duz-notlar").Input("Güncel not");
        istekler.Clear();
        e.Find("#cekmece-cari-kaydet").Click();

        var (_, _, govde) = istekler.First(i => i.Metot == "PATCH");
        Assert.Equal("Güncel not", Alan(govde, "notlar"));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// KART BACAĞI: POS CİHAZI + TAKSİT (Y-3, yedinci alan ailesi)
//
// OdemeBacagiDto.PosCihazId/PosTaksitAdedi sunucuda vardı; ekran göndermiyordu.
// Birden çok aktif cihazı olan dükkânda sunucu fail-closed reddediyor
// ("birden çok varsa istek REDDEDİLİR") ve kullanıcı sebebi göremiyordu.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P10PosBacagiEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string cihazlar = CokCihaz;

    private const string CokCihaz = """
        [{"id":"pc1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"s1","kasaId":"ks1",
          "pesinKomisyonOran":1.5,"valorGun":1,"paraBirimi":"TRY","aktif":true,
          "bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false},
         {"id":"pc2","ad":"Garanti POS","bankaAdi":"Garanti","subeId":"s1","kasaId":"ks1",
          "pesinKomisyonOran":2.0,"valorGun":1,"paraBirimi":"TRY","aktif":true,
          "bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}]
        """;

    private const string TekCihaz = """
        [{"id":"pc1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"s1","kasaId":"ks1",
          "pesinKomisyonOran":1.5,"valorGun":1,"paraBirimi":"TRY","aktif":true,
          "bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}]
        """;

    public P10PosBacagiEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => cihazlar))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(
        List<(string, string, string)> kayit, Func<string> cihazlar) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            var y = yol switch
            {
                // ⚠ Yeni uç: "{}" varsayılanı BIRAKILMADI (gerçekçi yanıt).
                var v when v.Contains("/pos/cihazlar") => cihazlar(),
                var v when v.Contains("/tevsik/gunluk") => """
                    {"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,
                     "bugunkuNakitToplam":0,"kalanNakitHakki":30000,
                     "islemTutariKuraliDevrede":false}
                    """,
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """
                    [{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,
                      "iscilikTl":0,"satisFiyati":10000}]
                    """,
                var v when v.Contains("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/esikler") => """
                    {"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,
                     "ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,
                     "belgeTurleri":["TC_KIMLIK","PASAPORT"]}
                    """,
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> KartBacakli()
    {
        var e = KarmaAcik();
        SeciciYardim.Sec(e, "islem-bacak-0-arac", "KART");
        // SATIS-3 (kurucu 25.09, KAYNAK 2 md.6): POS seçicisi YALNIZ birden çok cihazda; taksit "+ Taksit" ile.
        e.WaitForAssertion(() => Assert.True(e.FindAll("#islem-bacak-0-pos, #islem-bacak-0-taksit-ac, #islem-bacak-0-pos-tek").Count > 0 || cihazlar == "[]"));
        return e;
    }

    /// <summary>
    /// TEMA TEMİZLİĞİ 17.10: SATIŞ = Yıldız tezgâhı. Karma ödeme "+ Başka ödeme" ile açılır (sayfanın OdemeyiBol'u):
    /// tek ödeme İLK satır (NAKIT), ikinci satır eklenir. Satır kuralları (FisOdemeBacaklari) AYNI bileşendir.
    /// </summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> KarmaAcik()
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        TemaTemizligiGDYardimcisi.MusteriSec(e, "c1");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        TemaTemizligiGDYardimcisi.KalemSayisiBekle(e, 1);
        TemaTemizligiGDYardimcisi.KarmaAc(e);
        return e;
    }

    /// <summary>
    /// Yıldız'da tek KART satırı kalınca karma kendiliğinden tek ödemeye iner (TekeIndirilebilir) — POS bilgisi
    /// gövdede ödeme satırı olarak gitsin diye kart 6.000 + nakit 4.000 (fiş 10.000) iki satırla kaydedilir.
    /// </summary>
    private static void KartVeNakitTutarlari(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        SeciciYardim.Sec(e, "islem-bacak-0-tutar", "6000");
        SeciciYardim.Sec(e, "islem-bacak-1-arac", "NAKIT");
        SeciciYardim.Sec(e, "islem-bacak-1-tutar", "4000");
        TemaTemizligiGDYardimcisi.KaydetAcikBekle(e);
    }

    [Fact]
    public void NAKIT_bacaginda_POS_alanlari_CIZILMEZ()
    {
        var e = KarmaAcik();
        Assert.Equal("NAKIT", SeciciYardim.SeciliDeger(e, "islem-bacak-0-arac"));   // satır çizildi, araç NAKİT

        Assert.Empty(e.FindAll("#islem-bacak-0-pos"));
        Assert.Empty(e.FindAll("#islem-bacak-0-taksit"));
    }

    [Fact]
    public void KART_secilince_POS_cihazi_ve_TAKSIT_alanlari_ACILIR()
    {
        // UX-S24: bağımlı alanın bağını ekran kurar.
        var e = KartBacakli();
        Assert.NotNull(e.Find("#islem-bacak-0-pos"));
        Assert.Empty(e.FindAll("#islem-bacak-0-taksit"));   // SATIS-3: taksit varsayılan 1 → kapalı
        e.Find("#islem-bacak-0-taksit-ac").Click();
        e.WaitForElement("#islem-bacak-0-taksit");
    }

    [Fact]
    public void COK_CIHAZ_varsa_secici_ACIKTIR_ve_secim_GOVDEDE_gider()
    {
        var e = KartBacakli();
        Assert.False(e.Find("#islem-bacak-0-pos").HasAttribute("disabled"));

        SeciciYardim.Sec(e, "islem-bacak-0-pos", "pc2");
        e.Find("#islem-bacak-0-taksit-ac").Click();
        e.WaitForElement("#islem-bacak-0-taksit");
        SeciciYardim.Sec(e, "islem-bacak-0-taksit", "6");
        KartVeNakitTutarlari(e);
        istekler.Clear();
        e.Find(TemaTemizligiGDYardimcisi.Kaydet).Click();

        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Yol.EndsWith("/islemler/satis-fisi")));
        var g = System.Text.Json.JsonDocument
            .Parse(istekler.First(i => i.Metot == "POST").Govde).RootElement;
        var bacak = g.GetProperty("odemeler")[0];
        Assert.Equal("pc2", bacak.GetProperty("posCihazId").GetString());
        Assert.Equal(6, bacak.GetProperty("posTaksitAdedi").GetInt32());
    }

    [Fact]
    public void TEK_CIHAZ_varsa_secici_KILITLI_ve_sebebi_YAZAR()
    {
        // UX-S22: tek seçenek sorulmaz, atanır. SATIS-3 (kurucu 25.09): kilitli seçici de
        // çizilmez — PRO'da kısa salt okunur satır cihazın adını söyler.
        cihazlar = TekCihaz;
        var e = KartBacakli();

        Assert.Empty(e.FindAll("#islem-bacak-0-pos"));
        Assert.NotNull(e.Find("#islem-bacak-0-pos-tek"));

        KartVeNakitTutarlari(e);
        istekler.Clear();
        e.Find(TemaTemizligiGDYardimcisi.Kaydet).Click();

        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Yol.EndsWith("/islemler/satis-fisi")));
        var g = System.Text.Json.JsonDocument
            .Parse(istekler.First(i => i.Metot == "POST").Govde).RootElement;
        Assert.Equal("pc1", g.GetProperty("odemeler")[0].GetProperty("posCihazId").GetString());
    }

    [Fact]
    public void CIHAZ_YOKSA_sebebi_ekranda_YAZAR_ve_govde_cihazsiz_gider()
    {
        // SATIS-3 (kurucu 25.09, KAYNAK 2 md.6): "POS cihazı yoksa seçici HİÇ görünmez (not da yok)".
        // Gövde cihazsız gider — sunucu kuralı değişmedi.
        cihazlar = "[]";
        var e = KartBacakli();

        Assert.Empty(e.FindAll("#islem-bacak-0-pos-yok"));
        Assert.Empty(e.FindAll("#islem-bacak-0-pos"));
        Assert.Empty(e.FindAll("#islem-bacak-0-taksit, #islem-bacak-0-taksit-ac"));
        KartVeNakitTutarlari(e);
        istekler.Clear();
        e.Find(TemaTemizligiGDYardimcisi.Kaydet).Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/islemler/satis-fisi")));
        var g = System.Text.Json.JsonDocument.Parse(istekler.First(i => i.Metot == "POST").Govde).RootElement;
        var pc = g.GetProperty("odemeler")[0].TryGetProperty("posCihazId", out var v) ? v.ToString() : "";
        Assert.True(string.IsNullOrEmpty(pc) || v.ValueKind == System.Text.Json.JsonValueKind.Null, pc);
    }
}
