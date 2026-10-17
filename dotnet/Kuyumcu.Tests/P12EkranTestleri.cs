using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-12 — EKRAN TARAFI [bUnit] · Kimlikler: B-38 · B-42 · B-43 · B-19
//
// Sınananlar:
//   1. B-38 — /vezne "Düzenle" çekmecesinde İŞÇİLİK (TL) alanı VAR ve
//      değişikliği PATCH gövdesine `iscilikTl` olarak taşır.
//   2. B-42/B-19 — /banka dekont satırında STORNO düğmesi VAR (eskiden yalnız
//      gerekçe metni vardı) ve gerçek istek atar; stornolanmış satırda KİLİTLİ.
//   3. B-43 — /emanet hareket satırında açıklama SATIR İÇİ düzeltilebilir.
//   4. DoD EK md.1 — "tıklanınca hiçbir şey olmayan düğme" yok: her düğme ya
//      istek atar ya disabled + gerekçe taşır.
//
// ⚠ Sahte API '{}' varsayılanıyla BIRAKILMADI: bu paketin dokunduğu her uca
//   gerçekçi yanıt verildi (yeni PATCH/storno uçları dâhil).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P12EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string bankaHareketleri = TekDekont;
    private string emanetYaniti = TekEmanet;

    private const string TekDekont = """
        {"hareketler":[{"id":"bh1","yon":"GELEN","tutar":1250.00,"karsiTarafAd":"Ali Veli",
          "aciklama":"Dekont","durum":"BEKLIYOR","tarih":"2026-09-16T10:00:00Z",
          "karsiIban":null,"stornoKaynakId":null,"ekstreSatirId":null}]}
        """;

    /// <summary>Kaynak + onu hükümsüz kılan ters satır (storno ÇİFTİ).</summary>
    private const string StornoluDekont = """
        {"hareketler":[
          {"id":"bh1","yon":"GELEN","tutar":1250.00,"karsiTarafAd":"Ali Veli",
           "aciklama":"Dekont","durum":"BEKLIYOR","tarih":"2026-09-16T10:00:00Z",
           "karsiIban":null,"stornoKaynakId":null,"ekstreSatirId":null},
          {"id":"bh2","yon":"GIDEN","tutar":1250.00,"karsiTarafAd":"Ali Veli",
           "aciklama":"STORNO: yanlis hesaba girildi duzeltiliyor","durum":"BEKLIYOR",
           "tarih":"2026-09-16T11:00:00Z","karsiIban":null,"stornoKaynakId":"bh1",
           "ekstreSatirId":null}]}
        """;

    private const string TekEmanet = """
        {"cariId":"c1","mevcutHasGramBakiye":9.17,
         "hareketler":[{"id":"eh1","tur":"EMANET_GIRIS","tarih":"2026-09-16T10:00:00Z",
           "hasGrami":9.17,"aciklama":"Bilezik","stornoKaynakId":null,"fisId":null}],
         "madenBakiyeleri":[{"maden":"ALTIN","hasGram":9.17}]}
        """;

    private const string AcikFisKuyruk = """
        {"durum":"VEZNEDE","istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":1,
         "dahaFazlasiVar":false,"vezneZorunlu":false,"gerekceAsgari":10,"subeId":"sb1",
         "durumKirilimi":[{"durum":"ACIK","adet":1}],
         "fisler":[{"id":"v2","durum":"ACIK","toplamTl":5000.00,"cariId":"c1","cariAd":"Ayşe Yılmaz",
            "aciklama":"bilezik","hazirlayanKullaniciId":"u1","hazirlayanAd":"Test Kullanıcı",
            "kalemSayisi":1,"hareketFisId":null,"duzenlenebilir":true,"mutabakatGerekli":false,
            "olusturuldu":"2026-09-17T09:00:00Z","veznelendi":null,
            "guncellendi":"2026-09-17T09:00:00Z"}]}
        """;

    private const string AcikFisDetay = """
        {"id":"v2","durum":"ACIK","cariId":"c1","cariAd":"Ayşe Yılmaz","aciklama":"bilezik",
         "kurSnapshotId":"kur-1","toplamTl":5000.00,"damgaliToplamTl":5000.00,"hareketFisId":null,
         "hazirlayanKullaniciId":"u1","hazirlayanAd":"Test Kullanıcı","tahsilEdenKullaniciId":null,
         "tahsilEdenAd":null,"iptalGerekce":null,"vezneZorunlu":false,"gerekceAsgari":10,
         "duzenlenebilir":true,"veznelenebilir":true,"tahsilEdilebilir":false,
         "iptalEdilebilir":true,"terminal":false,"mutabakatGerekli":false,
         "olusturuldu":"2026-09-17T09:00:00Z","guncellendi":"2026-09-17T09:00:00Z",
         "veznelendi":null,"tahsilEdildi":null,"iptalEdildi":null,
         "kalemler":[{"id":"k1","urunId":"u-1","urunAd":"Bilezik","adet":1.0,
                      "birimFiyatTl":4900.00,"iscilikTl":100.00,"sira":0,"tutarTl":5000.00}]}
        """;

    public P12EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(P12EkranTestleri sahip) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            sahip.istekler.Add((istek.Method.Method, yol, govde));

            var g = yol switch
            {
                // ── PAKET P-12 uçları (gerçekçi yanıt; '{}' bırakılmadı) ──
                var y when y.Contains("/hareketler/") && y.EndsWith("/storno") =>
                    """{"stornoBankaHareketId":"bh9","kaynakBankaHareketId":"bh1","yon":"GIDEN","tutar":1250.00,"paraBirimi":"TRY","hesapBakiyesi":0.00}""",
                var y when y.Contains("/bankalar/") && y.Contains("/hareketler/") =>
                    """{"id":"bh1","bankaHesapId":"b1","aciklama":"Kira","karsiTarafAd":"Veli","karsiIban":null,"tarih":"2026-09-16T10:00:00Z","tutar":1250.00,"yon":"GELEN","degisenAlanSayisi":1}""",
                var y when y.Contains("/emanet/hareketler/") =>
                    """{"id":"eh1","tur":"EMANET_GIRIS","aciklama":"22 ayar bilezik","hasGrami":9.17,"tarih":"2026-09-16T10:00:00Z","degisenAlanSayisi":1}""",

                var y when y.Contains("/bankalar/") && y.Contains("/hareketler") => sahip.bankaHareketleri,
                var y when y.EndsWith("/bankalar") =>
                    """{"hesaplar":[{"id":"b1","bankaAdi":"Ziraat","hesapAdi":"Ana Hesap","hesapSahibi":"X Ltd.","iban":"TR200000000000000000000001","paraBirimi":"TRY","bakiye":1250.00,"aktif":true}]}""",

                var y when y.StartsWith("/api/v1/emanet/") => sahip.emanetYaniti,

                var y when y.Contains("/vezne/fisler/") => AcikFisDetay,
                var y when y.EndsWith("/vezne/fisler") => AcikFisDetay,
                var y when y.EndsWith("/vezne/kuyruk") => AcikFisKuyruk,
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez","paraBirimi":"TRY"}]""",
                // PAKET GOC-A: ürün birimi GRAM — miktar 3 ondalıklı gram (K10); "1,5" tr-TR ile 1,5 okunur (P-78 iddiası).
                var y when y.EndsWith("/urunler") => """[{"id":"u-1","ad":"Bilezik","birim":"GRAM"}]""",
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz"}]""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800}""",
                var y when y.Contains("/masak/esikler") =>
                    """{"kimlikEsigi":185000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000,"ekBelgeEsigi":20000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var y when y.Contains("/auth/ben") =>
                    """{"id":"u1","ad":"Test","soyad":"Kullanici","email":"t@t.local","rol":"SAHIP","subeId":"sb1"}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private void Oturum(string rol = "SAHIP") =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "sb1"));

    // ═════════════════════════════════════════════════════════════════════════
    // B-38 — VEZNE DÜZENLE PENCERESİNDE İŞÇİLİK ALANI
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void B38_duzenleme_cekmecesinde_ISCILIK_alani_VAR()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Vezne>().AyrintiAc();
        e.Find("#vz-duzenle-v2").Click();

        // UX-S26: oluşturma formunun alan kümesi düzenlemede de eksiksizdir.
        var girdi = e.Find("#vz-d-iscilik-0");
        // P-78: girdi değeri tr-TR gruplamasız basılır ("100,00"); eski Invariant
        // "100.00" kullanıcıyı noktalı yazıma alıştırıyordu ("45.000" = 45 hatası).
        // Değer aynı, yazım değişti — bu testin sınadığı alanın VARLIĞI korunur.
        // PAKET GOC-A: işçilik ortak Para (K9a) — odak dışında binlikli biçim ("100"); değer AYNI (100).
        Assert.Equal("100", girdi.GetAttribute("value"));
        Assert.Equal("100", girdi.GetAttribute("data-deger"));
    }

    [Fact]
    public void B38_iscilik_degisikligi_PATCH_govdesine_gider()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Vezne>().AyrintiAc();
        e.Find("#vz-duzenle-v2").Click();
        e.Find("#vz-d-iscilik-0").Input("250.00");
        e.Find("#cekmece-vezne-fisi-kaydet").Click();

        var patch = istekler.Last(i => i.Metot == "PATCH");
        Assert.Contains("/vezne/fisler/v2", patch.Yol);
        Assert.Contains("\"iscilikTl\":250.00", patch.Govde);
    }

    // ── PAKET P-78 — Vezne sayı girişi tr-TR (eskiden Invariant: "1,5" = 15) ──
    [Fact]
    public void P78_vezne_duzenlemede_1_5_adet_ve_45_000_fiyat_TURKCE_okunur()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Vezne>().AyrintiAc();
        e.Find("#vz-duzenle-v2").Click();
        // Ön doldurma tr-TR gruplamasız: "4900,00" (eskiden "4900.00").
        // TOPLU-1 (K20): birim fiyat binlikli Para girdisi — odak dışında "4.900" (değer 4900 aynı).
        Assert.Equal("4.900", e.Find("#vz-d-fiyat-0").GetAttribute("value"));
        e.Find("#vz-d-adet-0").Input("1,5");
        e.Find("#vz-d-fiyat-0").Input("45.000");
        e.Find("#cekmece-vezne-fisi-kaydet").Click();

        var patch = istekler.Last(i => i.Metot == "PATCH");
        using var j = System.Text.Json.JsonDocument.Parse(patch.Govde);
        var k = j.RootElement.GetProperty("kalemler").EnumerateArray().Single();
        Assert.Equal(1.5m, k.GetProperty("adet").GetDecimal());
        Assert.Equal(45000m, k.GetProperty("birimFiyatTl").GetDecimal());
        Assert.Equal(100m, k.GetProperty("iscilikTl").GetDecimal());   // dokunulmayan alan gidiş-dönüşte DEĞİŞMEDİ
    }

    [Fact]
    public void P78_vezne_belirsiz_fiyat_ALAN_ALTINDA_soylenir_ve_PATCH_GITMEZ()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Vezne>().AyrintiAc();
        e.Find("#vz-duzenle-v2").Click();
        e.Find("#vz-d-fiyat-0").Input("1.500");

        Assert.Equal(Kuyumcu.Core.I18n.SayiGirisiSozlugu.Tr.Belirsiz,
            e.Find("#vz-d-fiyat-0-hata").TextContent.Trim());
        e.Find("#cekmece-vezne-fisi-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // B-42 / B-19 — BANKA DEKONTUNDA STORNO GÖRÜNÜR
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void B42_dekont_satirinda_STORNO_dugmesi_VAR_ve_ACIK()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Banka>().AyrintiAc().HareketleriAc();
        var dugme = e.Find("#bh-storno-bh1");
        Assert.False(dugme.HasAttribute("disabled"));
    }

    [Fact]
    public void B42_storno_dugmesi_GERCEK_ISTEK_atar()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Banka>().AyrintiAc().HareketleriAc();
        e.Find("#bh-storno-bh1").Click();
        e.Find("#bh-storno-gerekce").Input("Yanlis hesaba girildi, duzeltiliyor.");
        e.Find("#bh-storno-onayla").Click();

        Assert.Contains(istekler, i =>
            i.Metot == "POST" && i.Yol == "/api/v1/bankalar/b1/hareketler/bh1/storno");
    }

    /// <summary>DoD EK md.1 — gerekçe kısayken düğme KİLİTLİ ve sebebi yazılı.</summary>
    [Fact]
    public void B42_kisa_gerekcede_onay_dugmesi_KILITLI_ve_sebebi_YAZILI()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Banka>().AyrintiAc().HareketleriAc();
        e.Find("#bh-storno-bh1").Click();
        e.Find("#bh-storno-gerekce").Input("kisa");

        Assert.True(e.Find("#bh-storno-onayla").HasAttribute("disabled"));
        Assert.NotNull(e.Find("#bh-storno-kilit"));
    }

    [Fact]
    public void B42_stornolanmis_dekontta_dugme_KILITLI_ve_rozet_VAR()
    {
        bankaHareketleri = StornoluDekont;
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Banka>().AyrintiAc().HareketleriAc();

        Assert.True(e.Find("#bh-storno-bh1").HasAttribute("disabled"));   // kaynak
        Assert.True(e.Find("#bh-storno-bh2").HasAttribute("disabled"));   // ters kayıt
        // UX-S30: kilidin sebebi ekranda YAZILI.
        Assert.NotNull(e.Find("#degismez-ipucu-BankaHareketi"));
    }

    [Fact]
    public void B42_dekont_aciklamasi_SATIR_ICI_duzeltilir()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Banka>().AyrintiAc().HareketleriAc();
        e.Find("#si-banka-dekont-aciklama-bh1-ac").Click();
        e.Find("#si-banka-dekont-aciklama-bh1").Input("Kira tahsilati");
        e.Find("#si-banka-dekont-aciklama-bh1-kaydet").Click();

        var patch = istekler.Last(i => i.Metot == "PATCH");
        Assert.Equal("/api/v1/bankalar/b1/hareketler/bh1", patch.Yol);
        Assert.Contains("Kira tahsilati", patch.Govde);
        // Finansal alan gövdeye SIZMAZ.
        Assert.DoesNotContain("tutar", patch.Govde);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // B-43 — EMANET AÇIKLAMASI DÜZELTİLEBİLİR
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void B43_emanet_aciklamasi_SATIR_ICI_duzeltilir()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Emanet>().EmanetOzgunAc();
        SeciciYardim.Sec(e, "e-cari", "c1");
        e.WaitForElement("#e-gecmis-ac").Click();   // EMANET-1: geçmiş katlanırda KAPALI

        e.WaitForElement("#si-emanet-hareket-aciklama-eh1-ac").Click();
        e.Find("#si-emanet-hareket-aciklama-eh1").Input("22 ayar bilezik");
        e.Find("#si-emanet-hareket-aciklama-eh1-kaydet").Click();

        var patch = istekler.Last(i => i.Metot == "PATCH");
        Assert.Equal("/api/v1/emanet/hareketler/eh1", patch.Yol);
        Assert.Contains("22 ayar bilezik", patch.Govde);
        Assert.DoesNotContain("hasGrami", patch.Govde);
    }
}
