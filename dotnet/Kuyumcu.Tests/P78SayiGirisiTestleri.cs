using System.Text.Json;
using Bunit;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests.P78;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-78 — SAYI GİRİŞİ tr-TR AYRIŞTIRMA (demo düzeltmesi, 24.09 ölçümü)
//
// Ölçülen hata: kullanıcı girdisi Invariant ayrıştırılıyordu → Türkçe "45.000"
// = 45 (binlik nokta ondalık sanıldı), "1,5" = 15 (virgül binlik sanıldı).
// Sınananlar:
//   1. FormAlani.SayiCoz — kabul örneklerinin TAMAMI (saf, Theory).
//   2. Gidiş-dönüş — Bicim.Girdi'nin bastığı değer aynı çözücüyle BİREBİR geri okunur.
//   3. FisKalemTablosu (demo ekranı) — "45.000" = 45000 TL, "1,5" adet = 1,5,
//      "1.500" belirsiz → alan altı mesaj, kalem DEĞİŞMEZ.
//   4. SatirIciDuzenle aralık denetimi ve Pos komisyon oranı — tek merkeze bağlı.
// Sayfa testleri (Vezne · Siparisler · SiparisDetay · Vitrin) ilgili sayfanın
// gerçekçi sahte API'sini taşıyan mevcut sınıflara "P78_" önekiyle eklendi.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class FormAlaniSayiTestleri
{
    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1.250.000", "1250000")]
    [InlineData("1.250.000,50", "1250000.50")]
    [InlineData("12.500,5", "12500.5")]
    [InlineData("1,5", "1.5")]
    [InlineData("22.5", "22.5")]
    [InlineData("5.25", "5.25")]
    [InlineData("45000.00", "45000")]
    [InlineData("45000,00", "45000")]
    [InlineData("1500", "1500")]
    [InlineData("0.500", "0.5")]           // tam kısım 0 → binlik olamaz, ondalık
    [InlineData("1234.500", "1234.5")]     // 4 haneli tam kısım → binlik kalıbı değil, ondalık
    [InlineData("-3,5", "-3.5")]           // negatif kuralı ÇAĞIRANIN işi
    [InlineData(" 1 500 ", "1500")]        // boşluk temizlenir
    [InlineData("1 500,25", "1500.25")] // NBSP temizlenir
    [InlineData(",5", "0.5")]
    public void Kabul_ornekleri_tr_TR_kurallariyla_cozulur(string ham, string beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
        Assert.Equal(c.Deger, FormAlani.Ondalik(ham));
    }

    [Theory]
    [InlineData("1.500")]
    [InlineData("12.345")]
    [InlineData("-2.250")]
    public void Tek_nokta_ve_tam_uc_hane_BELIRSIZ_null_ve_ozel_mesaj(string ham)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, c.Sonuc);
        Assert.Null(c.Deger);
        Assert.Null(FormAlani.Ondalik(ham));   // sessiz tahmin YOK
        Assert.Equal(SayiGirisiSozlugu.Tr.Belirsiz, c.Mesaj("tr"));
        Assert.Equal("Ondalık için virgül kullanın: 1,5 ya da 1500", c.Mesaj("tr"));
        Assert.Equal(SayiGirisiSozlugu.En.Belirsiz, c.Mesaj("en"));
    }

    [Theory]
    [InlineData("1,500.25")]
    [InlineData("1,2,3")]
    [InlineData("abc")]
    [InlineData("1.000.5")]
    [InlineData("-")]
    [InlineData("12a")]
    public void Kaliba_uymayan_GECERSIZ_null(string ham)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Gecersiz, c.Sonuc);
        Assert.Null(c.Deger);
        Assert.Equal(SayiGirisiSozlugu.Tr.Gecersiz, c.Mesaj("tr"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Bos_girdi_null_SIFIR_DEGIL_ve_mesaj_cagirana_kalir(string? ham)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Bos, c.Sonuc);
        Assert.Null(c.Deger);
        Assert.Null(c.Mesaj("tr"));
    }

    [Fact]
    public void Sozluk_TR_ve_EN_dolu()
    {
        foreach (var m in new[] { SayiGirisiSozlugu.Tr, SayiGirisiSozlugu.En })
        {
            Assert.False(string.IsNullOrWhiteSpace(m.Belirsiz));
            Assert.False(string.IsNullOrWhiteSpace(m.Gecersiz));
        }
        Assert.NotEqual(SayiGirisiSozlugu.Tr.Belirsiz, SayiGirisiSozlugu.En.Belirsiz);
    }
}

public sealed class GidisDonusTestleri
{
    // Her ekranın kullandığı gösterim kalıpları: fiyat "0.00", adet/gram "0.###",
    // iskonto "0.##", varsayılan (girdi ailesi) "0.##########".
    [Theory]
    [InlineData("45000", "0.00", "45000,00")]
    [InlineData("1250000.5", "0.00", "1250000,50")]
    [InlineData("1.5", "0.###", "1,5")]
    [InlineData("1.125", "0.###", "1,125")]      // eski Invariant "1.125" → tr-TR'de BELİRSİZ olurdu
    [InlineData("12.5", "0.##", "12,5")]
    [InlineData("0.0275", "0.##########", "0,0275")]
    [InlineData("1234.567", "0.##########", "1234,567")]
    [InlineData("1000", "0.##########", "1000")]
    public void Gosterilen_deger_ayni_yerde_geri_kaydedilince_DEGISMEZ(string sayi, string kalip, string beklenenMetin)
    {
        var d = decimal.Parse(sayi, System.Globalization.CultureInfo.InvariantCulture);
        var metin = Bicim.Girdi(d, kalip);
        Assert.Equal(beklenenMetin, metin);
        Assert.DoesNotContain(".", metin);   // gruplamasız, nokta HİÇ yok
        Assert.Equal(d, FormAlani.Ondalik(metin));
    }
}

public sealed class FisKalemTablosuTurkceTestleri : BunitContext
{
    private readonly FormDurumu form = new();
    private int haber;

    public FisKalemTablosuTurkceTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        GocC2Ortak.IzgaraServisleri(Services);   // GOC-C-2: kalem tablosu ortak ızgarada (ApiIstemci ister)
    }

    private IRenderedComponent<FisKalemTablosu> Tablo(FisKalemi k)
    {
        var t = Render<FisKalemTablosu>(p => p
            .Add(x => x.Kalemler, [k])
            .Add(x => x.Urunler, Array.Empty<UrunVeri>())
            .Add(x => x.Form, form)
            .Add(x => x.Toplam, k.BirimFiyat * k.Adet)
            .Add(x => x.Degisti, EventCallback.Factory.Create<bool>(this, _ => haber++)));
        t.Find("#islem-kalem-duzenle-0").Click();   // T-S2 (tuval Profesyonel): birim fiyat/iskonto ✎ düzenleme satırında
        return t;
    }

    private static void Yaz(IRenderedComponent<FisKalemTablosu> t, string alan, string metin)
    {
        t.Find($"#si-kalem-{alan}-u1-ac").Click();
        t.Find($"#si-kalem-{alan}-u1").Input(metin);
        t.Find($"#si-kalem-{alan}-u1-kaydet").Click();
    }

    [Fact]
    public void FisKalemTablosu_TurkceFiyatVeAdet()
    {
        var k = new FisKalemi { UrunId = "u1", UrunAdi = "Bilezik", Adet = 1m, BirimFiyat = 100m };
        var t = Tablo(k);

        Yaz(t, "fiyat", "45.000");
        Assert.Equal(45000m, k.BirimFiyat);   // eskiden 45 okunuyordu
        Assert.Equal(45000m, k.OncekiFiyat);

        Yaz(t, "adet", "1,5");
        Assert.Equal(1.5m, k.Adet);           // eskiden 15 okunuyordu

        Yaz(t, "iskonto", "12,5");
        Assert.Equal(12.5m, k.SatirIskontoYuzde);

        Yaz(t, "fiyat", "1.250.000,50");
        Assert.Equal(1250000.50m, k.BirimFiyat);
        Assert.Null(form.Mesaj("si-kalem-fiyat-u1"));
    }

    [Fact]
    public void Belirsiz_fiyat_KALEMI_DEGISTIRMEZ_ve_alan_altinda_TURKCE_mesaj_verir()
    {
        var k = new FisKalemi { UrunId = "u1", UrunAdi = "Bilezik", Adet = 1m, BirimFiyat = 100m };
        var t = Tablo(k);

        Yaz(t, "fiyat", "1.500");

        Assert.Equal(100m, k.BirimFiyat);
        Assert.Equal("Ondalık için virgül kullanın: 1,5 ya da 1500", form.Mesaj("si-kalem-fiyat-u1"));

        Yaz(t, "adet", "2.500");
        Assert.Equal(1m, k.Adet);
        Assert.Equal(SayiGirisiSozlugu.Tr.Belirsiz, form.Mesaj("si-kalem-adet-u1"));
    }

    [Fact]
    public void Gosterilen_fiyat_TR_gruplamasiz_ve_ayni_deger_geri_kaydedilince_DEGISMEZ()
    {
        var k = new FisKalemi { UrunId = "u1", UrunAdi = "Bilezik", Adet = 1.125m, BirimFiyat = 45000m };
        var t = Tablo(k);

        // KESIF-P5 #70: birim fiyat tr-TR BİNLİKLİ gösterilir; kuruş hep yazılır ("45.000,00" — "45.000" P-78'de belirsiz).
        Assert.Equal("45.000,00", t.Find("#si-kalem-fiyat-u1-ac").TextContent.Trim());
        Assert.Equal("1,125", t.Find("#si-kalem-adet-u1-ac").TextContent.Trim());

        // Kullanıcı hiçbir şeye dokunmadan kaydeder: değer AYNEN kalır.
        t.Find("#si-kalem-fiyat-u1-ac").Click();
        Assert.Equal("45.000,00", t.Find("#si-kalem-fiyat-u1").GetAttribute("value"));
        t.Find("#si-kalem-fiyat-u1-kaydet").Click();
        Assert.Equal(45000m, k.BirimFiyat);

        t.Find("#si-kalem-adet-u1-ac").Click();
        t.Find("#si-kalem-adet-u1-kaydet").Click();
        Assert.Equal(1.125m, k.Adet);
        Assert.Null(form.Mesaj("si-kalem-adet-u1"));
    }

    [Fact]
    public void Demo_senaryosu_adim3_adet_1_2_1_aynen_calisir()
    {
        var k = new FisKalemi { UrunId = "u1", UrunAdi = "Bilezik", Adet = 1m, BirimFiyat = 45000m };
        var t = Tablo(k);
        Yaz(t, "adet", "2");
        Assert.Equal(2m, k.Adet);
        Yaz(t, "adet", "1");
        Assert.Equal(1m, k.Adet);
        Assert.True(haber >= 2);
    }
}

public sealed class SatirIciDuzenleSayiTestleri : BunitContext
{
    public SatirIciDuzenleSayiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    [Fact]
    public void Aralikli_alanda_belirsiz_sayi_ALAN_ALTINDA_soylenir_ve_kaydetmez()
    {
        string? kaydedilen = null;
        var c = Render<SatirIciDuzenle>(p => p
            .Add(x => x.Varlik, "fk").Add(x => x.Alan, "miktar").Add(x => x.Id, "k1")
            .Add(x => x.Deger, "1")
            .Add(x => x.AsgariDeger, 0.001m)
            .Add(x => x.Kaydedildi, EventCallback.Factory.Create<string>(this, v => kaydedilen = v)));

        c.Find("#si-fk-miktar-k1-ac").Click();
        c.Find("#si-fk-miktar-k1").Input("1.500");
        c.Find("#si-fk-miktar-k1-kaydet").Click();

        Assert.Null(kaydedilen);
        Assert.Equal(SayiGirisiSozlugu.Tr.Belirsiz, c.Find("#si-fk-miktar-k1-hata").TextContent.Trim());

        c.Find("#si-fk-miktar-k1").Input("1,5");
        c.Find("#si-fk-miktar-k1-kaydet").Click();
        Assert.Equal("1,5", kaydedilen);
    }
}

public sealed class SayisalAlanGidisDonusTestleri : BunitContext
{
    public SayisalAlanGidisDonusTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    [Fact]
    public void Gram_kutusu_uc_haneli_degeri_TR_basar_ve_kendi_degerini_BELIRSIZ_saymaz()
    {
        decimal? gelen = 1.125m;
        var c = Render<Gram>(p => p
            .Add(x => x.ElemanId, "p78-gram")
            .Add(x => x.Etiket, "Gram")
            .Add(x => x.Deger, 1.125m)
            .Add(x => x.DegerChanged, EventCallback.Factory.Create<decimal?>(this, v => gelen = v)));

        // Eski Makine yazımı "1.125" tr-TR çözücüde BELİRSİZ olurdu (1,125 mi 1125 mi?).
        Assert.Equal("1,125", c.Find("#p78-gram").GetAttribute("value"));
        Assert.Equal("false", c.Find("#p78-gram").GetAttribute("aria-invalid"));

        // Kullanıcı kutudaki değeri aynen yeniden yazar: değer DEĞİŞMEZ, hata doğmaz.
        c.Find("#p78-gram").Input("1,125");
        Assert.Equal(1.125m, gelen);
        Assert.Empty(c.FindAll("#p78-gram-hata"));

        c.Find("#p78-gram").Input("2.500");
        Assert.Equal(SayiGirisiSozlugu.Tr.Belirsiz, c.Find("#p78-gram-hata").TextContent.Trim());
        Assert.Equal(1.125m, gelen);   // belirsiz girdi değeri DEĞİŞTİRMEDİ
    }
}

public sealed class PosOranTurkceTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public PosOranTurkceTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<PosServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        private const string Cihaz =
            """{"id":"p1","ad":"Garanti POS","bankaAdi":"Garanti","subeId":"s1","kasaId":"k1","bankaHesapId":null,"uyeIsyeriNo":null,"terminalNo":null,"pesinKomisyonOran":0.0185,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0.00,"bugunYatacakNet":0.00}""";

        private static string Detay(string oran) =>
            $$"""{"cihaz":{{Cihaz}},"taksitOranlari":[{"id":"o1","posCihazId":"p1","taksitAdedi":3,"komisyonOran":{{oran}},"valorGun":null,"gecerliBaslangic":"2026-09-01T00:00:00Z","aktif":true}]}""";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            var y = yol switch
            {
                var v when v.Contains("/taksit-oranlari/o1") => Detay("0.035"),
                var v when v.Contains("/pos/cihazlar/p1") => Detay("0.0275"),
                var v when v.Contains("/pos/cihazlar") => $"[{Cihaz}]",
                var v when v.Contains("/pos/valor-takvimi") => "[]",
                var v when v.Contains("/pos/tahsilatlar") =>
                    """{"satirlar":[],"toplamSatir":0,"limit":50,"offset":0,"azamiSatir":200,"sinirDegdi":false}""",
                var v when v.Contains("/kasalar") =>
                    """[{"id":"k1","ad":"Kasa","paraBirimi":"TRY","subeId":"s1","aktif":true}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Pos> Ac()
    {
        var e = Render<Kuyumcu.Web.Pages.Pos>();
        e.WaitForElement("#pos-duzenle-p1").Click();
        e.WaitForElement("#si-pos-taksit-oran-o1-ac");
        return e;
    }

    [Fact]
    public void Pos_komisyon_orani_TR_gosterilir_ve_virgullu_giris_1_5_gibi_OKUNUR()
    {
        var e = Ac();
        // KESIF-P1 (#31): satır içi oran artık YÜZDE okunur/yazılır — 0,0275 → "2,75";
        // "3,5" yazılınca gövdeye kesir (0,035) gider. Virgüllü giriş tr-TR okunur (P-78 kuralı aynen).
        Assert.Equal("2,75", e.Find("#si-pos-taksit-oran-o1-ac").TextContent.Trim());

        e.Find("#si-pos-taksit-oran-o1-ac").Click();
        e.Find("#si-pos-taksit-oran-o1").Input("3,5");   // eskiden kesir isteniyordu
        e.Find("#si-pos-taksit-oran-o1-kaydet").Click();

        e.WaitForState(() => istekler.Any(i => i.Metot == "PATCH"));
        var patch = istekler.Last(i => i.Metot == "PATCH");
        Assert.Contains("/taksit-oranlari/o1", patch.Yol);
        using var j = JsonDocument.Parse(patch.Govde);
        Assert.Equal(0.035m, j.RootElement.GetProperty("komisyonOran").GetDecimal());
    }

    [Fact]
    public void Pos_belirsiz_oran_istek_ATMAZ_ve_sebebini_soyler()
    {
        var e = Ac();
        e.Find("#si-pos-taksit-oran-o1-ac").Click();
        e.Find("#si-pos-taksit-oran-o1").Input("1.500");
        e.Find("#si-pos-taksit-oran-o1-kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
        e.WaitForAssertion(() => Assert.Contains(SayiGirisiSozlugu.Tr.Belirsiz, e.Markup));
    }
}
