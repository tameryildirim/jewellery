using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P6 — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİTTİR (üretim kodundan
// hesaplanmaz). KESIF-P6'nın para/oran yolları: POS komisyon oranı ölçek
// kapısı (decimal(5,4) — fazla hane reddedilir), Banka dekont tutarı girişi
// (FormAlani.SayiCoz, #47), parite birimi / beyan kademesi çevirisi (boş/null).
// ════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class KESIFP6SinirTestleri(HttpFikstur f)
{
    private static object Cihaz(Kiraci k, decimal oran) => new
    {
        ad = $"S6-{Guid.NewGuid():N}"[..12],
        bankaAdi = "Garanti",
        subeId = k.SubeId,
        kasaId = k.KasaId,
        pesinKomisyonOran = oran,
        valorGun = 1,
    };

    private async Task<HttpStatusCode> CihazDurum(Kiraci k, decimal oran)
        => (await f.Ortam.Istemci(k).PostJson("/api/v1/pos/cihazlar", Cihaz(k, oran))).StatusCode;

    [Fact]
    public async Task POS_oran_sifir_kabul()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "S6 sifir");
        Assert.Equal(HttpStatusCode.OK, await CihazDurum(k, 0m));
    }

    [Fact]
    public async Task POS_oran_eksi_red()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "S6 eksi");
        Assert.Equal(HttpStatusCode.BadRequest, await CihazDurum(k, -0.0001m));
    }

    [Fact]
    public async Task POS_oran_5_hane_yuvarlama_siniri_0_00005_red_0_0001_kabul()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "S6 hane");
        Assert.Equal(HttpStatusCode.BadRequest, await CihazDurum(k, 0.00005m));
        Assert.Equal(HttpStatusCode.BadRequest, await CihazDurum(k, 0.01855m));
        Assert.Equal(HttpStatusCode.OK, await CihazDurum(k, 0.0001m));
    }

    [Fact]
    public async Task POS_oran_sondaki_sifir_fazla_hane_sayilmaz_ve_aynen_saklanir()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "S6 sifir hane");
        var r = await f.Ortam.Istemci(k).PostJson("/api/v1/pos/cihazlar", Cihaz(k, 0.018500m));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var id = (await r.Json()).GetProperty("cihaz").GetProperty("id").GetString()!;
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0.0185m, db.PosCihazlari.AsNoTracking().Single(x => x.Id == id).PesinKomisyonOran);
    }

    [Fact]
    public async Task POS_oran_ust_sinir_1_kabul_1_0001_ve_cok_buyuk_red()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "S6 buyuk");
        Assert.Equal(HttpStatusCode.OK, await CihazDurum(k, 1m));
        Assert.Equal(HttpStatusCode.BadRequest, await CihazDurum(k, 1.0001m));
        Assert.Equal(HttpStatusCode.BadRequest, await CihazDurum(k, 999999999.99m));
    }

    // ── #47 Banka dekont tutarı: Türkçe giriş ──
    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("999.999.999,99", "999999999.99")]
    [InlineData("0,005", "0.005")]
    public void Dekont_tutari_Turkce_giris_cozulur(string ham, string beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1,500.75")]
    public void Dekont_tutari_gecersiz_giris_deger_vermez(string ham)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.NotEqual(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Null(c.Deger);
    }

    [Fact]
    public void Dekont_tutari_belirsiz_1_500_kabul_edilmez()
    {
        var c = FormAlani.SayiCoz("1.500");
        Assert.NotEqual(FormAlani.SayiSonucu.Tamam, c.Sonuc);
    }

    [Fact]
    public void Dekont_tutari_bos_null_deger_yok()
    {
        Assert.Null(FormAlani.SayiCoz(null).Deger);
        Assert.Null(FormAlani.SayiCoz("").Deger);
    }

    // ── #29 / #86 boş-null kod ──
    [Fact]
    public void Birim_ve_kademe_null_bos_metin_doner_uydurmaz()
    {
        Assert.Equal("", KesifP6Sozlugu.Birim(null, "tr"));
        Assert.Equal("", KesifP6Sozlugu.Kademe(null, "en"));
        Assert.Equal("Has gram", KesifP6Sozlugu.Birim("HAS_GRAM", "tr"));
        Assert.Equal("Fine gram", KesifP6Sozlugu.Birim("HAS_GRAM", "en"));
        Assert.Equal("İşlem amacı beyanı", KesifP6Sozlugu.Kademe("AMAC_BEYANI", "tr"));
    }

    // ════════════════════════════════════════════════════════════════════
    // HASIM DOĞRULAYICI TUR 3 (2026-10-01) — dördüncü yazma yolu (taksit PATCH)
    // ve cihaz PATCH sınırları; beklenenler SABİT, kayıt değişmezliği DB'den.
    // ════════════════════════════════════════════════════════════════════
    private async Task<(Kiraci k, string cihazId, string oranId)> CihazVeTaksit(string ad)
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), ad);
        var c = f.Ortam.Istemci(k);
        var r = await c.PostJson("/api/v1/pos/cihazlar", Cihaz(k, 0.0185m));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var id = (await r.Json()).GetProperty("cihaz").GetProperty("id").GetString()!;
        var t = await c.PostJson($"/api/v1/pos/cihazlar/{id}/taksit-oranlari",
            new { taksitAdedi = 3, komisyonOran = 0.0321m, valorGun = 30 });
        Assert.True(t.IsSuccessStatusCode, $"taksit oluşturma {(int)t.StatusCode}");
        using var db = f.Vt.OlusturSistem();
        var oranId = db.PosTaksitOranlari.AsNoTracking().Single(x => x.PosCihazId == id).Id;
        return (k, id, oranId);
    }

    [Theory]
    [InlineData("0.00005")]   // 5. hane — yuvarlanmaz, red
    [InlineData("-0.0001")]   // eksi — red
    [InlineData("1.0001")]    // %100 üstü — red
    [InlineData("0.03215")]   // 0,005 kuruş sınırının oran karşılığı — red (yuvarlama YOK)
    public async Task Tur3_taksit_PATCH_gecersiz_oran_400_ve_kayit_DEGISMEZ(string oranMetni)
    {
        var (k, id, oranId) = await CihazVeTaksit("S6t3 taksit patch");
        var oran = decimal.Parse(oranMetni, System.Globalization.CultureInfo.InvariantCulture);
        var r = await f.Ortam.Istemci(k).PatchJson($"/api/v1/pos/cihazlar/{id}/taksit-oranlari/{oranId}",
            new { komisyonOran = oran });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0.0321m, db.PosTaksitOranlari.AsNoTracking().Single(x => x.Id == oranId).KomisyonOran);
    }

    [Fact]
    public async Task Tur3_taksit_PATCH_sifir_ve_4_hane_kabul_aynen_saklanir()
    {
        var (k, id, oranId) = await CihazVeTaksit("S6t3 taksit kabul");
        var c = f.Ortam.Istemci(k);
        var r0 = await c.PatchJson($"/api/v1/pos/cihazlar/{id}/taksit-oranlari/{oranId}", new { komisyonOran = 0m });
        Assert.Equal(HttpStatusCode.OK, r0.StatusCode);
        var r4 = await c.PatchJson($"/api/v1/pos/cihazlar/{id}/taksit-oranlari/{oranId}", new { komisyonOran = 0.0001m });
        Assert.Equal(HttpStatusCode.OK, r4.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0.0001m, db.PosTaksitOranlari.AsNoTracking().Single(x => x.Id == oranId).KomisyonOran);
    }

    [Fact]
    public async Task Tur3_cihaz_PATCH_eksi_ve_cok_buyuk_400_oran_DEGISMEZ_alan_yoksa_korunur()
    {
        var (k, id, _) = await CihazVeTaksit("S6t3 cihaz patch");
        var c = f.Ortam.Istemci(k);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await c.PatchJson($"/api/v1/pos/cihazlar/{id}", new { pesinKomisyonOran = -0.01m })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await c.PatchJson($"/api/v1/pos/cihazlar/{id}", new { pesinKomisyonOran = 999999999.99m })).StatusCode);
        // PATCH sözleşmesi: oran gövdede yoksa KORUNUR.
        var ad = await c.PatchJson($"/api/v1/pos/cihazlar/{id}", new { valorGun = 2 });
        Assert.Equal(HttpStatusCode.OK, ad.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0.0185m, db.PosCihazlari.AsNoTracking().Single(x => x.Id == id).PesinKomisyonOran);
    }

    // ── #47 dekont tutarı ek sınırlar (SayiCoz tek merkez) ──
    [Theory]
    [InlineData("0,0001", "0.0001")]       // has/gram 4 hane — kırpılmaz
    [InlineData("0", "0")]                 // sıfır çözülür; Kaydet kapısı EnAz=0,01 ile ayrı
    [InlineData("-5", "-5")]               // eksi İŞARETİYLE döner (işaret kaybolmaz)
    [InlineData("1.234.567,89", "1234567.89")]
    public void Tur3_dekont_tutari_sinir_degerleri(string ham, string beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }

    [Theory]
    [InlineData("1.500", FormAlani.SayiSonucu.Belirsiz)]
    [InlineData("   ", FormAlani.SayiSonucu.Bos)]
    [InlineData("-", FormAlani.SayiSonucu.Gecersiz)]
    [InlineData("1,500.75", FormAlani.SayiSonucu.Gecersiz)]
    public void Tur3_dekont_tutari_reddin_sebebi_dogru(string ham, FormAlani.SayiSonucu beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(beklenen, c.Sonuc);
        Assert.Null(c.Deger);
    }
}

// ════════════════════════════════════════════════════════════════════════
// TUR 3 (doğrulayıcı, 01.10) — sipariş kalemi gram/adet sınırları (/siparisler
// formu tur 2'de yeniden düzenlendi: kart satırı + "Daha fazla"). Beklenenler
// SABİT; gövde JSON ayrıştırılarak sınanır.
// ════════════════════════════════════════════════════════════════════════
public sealed class KESIFP6SiparisKalemSinirTestleri : Bunit.BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private static readonly TimeSpan Uzun = TimeSpan.FromSeconds(10);

    public KESIFP6SiparisKalemSinirTestleri()
    {
        JSInterop.Mode = Bunit.JSRuntimeMode.Loose;
        Services.AddScoped<Kuyumcu.Web.Servisler.DilServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.OturumServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.ArayuzModuServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.TemaServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<Kuyumcu.Web.Servisler.ApiIstemci>();
        Services.AddScoped<Kuyumcu.Web.Servisler.YetkiServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.DuzeltmePolitikaServisi>();
    }

    private sealed class Sahte(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage i, CancellationToken t)
        {
            var yol = i.RequestUri!.AbsolutePath;
            var govde = i.Content is null ? "" : await i.Content.ReadAsStringAsync(t);
            lock (kayit) kayit.Add((i.Method.Method, yol, govde));
            var g = yol switch
            {
                var y when y.EndsWith("/siparisler") && i.Method == HttpMethod.Post => """{"id":"s9","no":"S-2026-000009"}""",
                var y when y.EndsWith("/siparisler") => """{"istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":0,"dahaFazlasiVar":false,"gecikenSayisi":0,"durumKirilimi":[],"siparisler":[]}""",
                var y when y.EndsWith("/subeler") => """[{"id":"sb1","ad":"Merkez"}]""",
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz","tur":"MUSTERI"}]""",
                var y when y.EndsWith("/urunler") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private List<(string Metot, string Yol, string Govde)> Istekler() { lock (istekler) return [.. istekler]; }

    private Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Siparisler> Form(string gram, string adet)
    {
        Services.GetRequiredService<Kuyumcu.Web.Servisler.OturumServisi>().KullaniciAta(
            new Kuyumcu.Web.Servisler.OturumKullanici("u1", "T", "K", "t@ornek.com", "SAHIP", "sb1", "PROFESYONEL", "PROFESYONEL"));
        var e = Render<Kuyumcu.Web.Pages.Siparisler>();
        e.WaitForElement("#sip-yeni-ac", Uzun).Click();
        e.WaitForElement("#sip-form", Uzun);
        SeciciYardim.Sec(e, "sip-f-cari", "c1");
        // PAKET GOC-C (08.10): kutular ortak Metin/Gram/Adet bileşeni — yazarken 'input' olayı (eskiden ham input 'change').
        e.Find("#sip-f-kalem-tanim-0").Input("Burgu bilezik");
        e.Find("#sip-f-kalem-gram-0").Input(gram);
        e.Find("#sip-f-kalem-adet-0").Input(adet);
        return e;
    }

    private System.Text.Json.JsonElement GonderVeKalem(Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Siparisler> e)
    {
        e.Find("#sip-f-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), x => x.Metot == "POST"), Uzun);
        var g = Istekler().Last(x => x.Metot == "POST").Govde;
        return System.Text.Json.JsonDocument.Parse(g).RootElement.GetProperty("kalemler")[0];
    }

    [Theory]
    // PAKET GOC-C (08.10, envanter K10/K14 — kurucu "yazarken denetle"): 0 gram, eksi gram, 4 ondalık gram ve
    // kesirli adet artık EKRANDA alan altı hata + Kaydet kilidi (aşağıdaki "anlaşılmayan/kurala aykırı" teorisine taşındı);
    // sunucu aynı Core kuralıyla 400 verir (GocCTestleri). Geçerli Türkçe giriş gövdeye tam değerle gider.
    [InlineData("128,45", "45.000", "128.45", "45000")]            // Türkçe ondalık + binlik
    [InlineData("999.999.999,99", "1", "999999999.99", "1")]       // çok büyük gram
    public void Siparis_kalem_gram_adet_Turkce_giris_govdeye_tam_deger(string gram, string adet, string beklenenGram, string beklenenAdet)
    {
        var e = Form(gram, adet);
        Assert.Empty(e.FindAll("#sip-f-sayi-kilit"));
        var k = GonderVeKalem(e);
        Assert.Equal(decimal.Parse(beklenenGram, System.Globalization.CultureInfo.InvariantCulture), k.GetProperty("gramAgirlik").GetDecimal());
        Assert.Equal(decimal.Parse(beklenenAdet, System.Globalization.CultureInfo.InvariantCulture), k.GetProperty("adet").GetDecimal());
    }

    [Theory]
    [InlineData("1.500", "1")]       // belirsiz binlik/ondalık — reddedilir
    [InlineData("1,500.75", "1")]    // İngilizce biçim — geçersiz
    [InlineData("abc", "1")]
    [InlineData("10", "1.500")]      // adet kutusunda da aynı kapı
    // PAKET GOC-C: kural ihlalleri de aynı kilit (eskiden gövdeye gidip sunucuda düşüyordu).
    [InlineData("0", "1")]           // K10: gram 0'dan büyük
    [InlineData("-5", "1")]          // K10: eksi gram
    [InlineData("0,0001", "1")]      // K10: en çok 3 ondalık (kolon decimal(12,3))
    [InlineData("10", "1,5")]        // K14: adet tam sayı
    public void Siparis_kalem_anlasilmayan_sayi_Olustur_pasif_istek_yok(string gram, string adet)
    {
        var e = Form(gram, adet);
        Assert.True(e.Find("#sip-f-kaydet").HasAttribute("disabled"));
        Assert.NotEmpty(e.Find("#sip-f-sayi-kilit").TextContent.Trim());
        Assert.DoesNotContain(Istekler(), x => x.Metot == "POST");
    }

    [Fact]
    public void Siparis_kalem_bos_gram_null_adet_bos_1_sayilir()
    {
        var e = Form("", "");
        var k = GonderVeKalem(e);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, k.GetProperty("gramAgirlik").ValueKind);
        Assert.Equal(1m, k.GetProperty("adet").GetDecimal());
    }
}
