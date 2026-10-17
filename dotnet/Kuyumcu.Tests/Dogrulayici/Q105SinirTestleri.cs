using Bunit;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q10-5 — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİTTİR (üretim kodundan
// hesaplanmaz). Q10-5 sayısal girdi bileşenine (SayisalAlan.Degisti →
// DefterdenSil) ve vezne kalem tutar kapısına (adet/fiyat sıfır-eksi sebebi)
// dokundu; bu yüzden:
//   (A) FormAlani.SayiCoz — tr-TR giriş sınırları (değer DEĞİŞMEDEN çözülür,
//       ayrıştırmada yuvarlama YAPILMAZ: 0,005 → 0,005; 0,0001 → 0,0001).
//   (B) "Alan değişince eski mesaj düşer" kuralı, yeni değer HÂLÂ hatalıysa
//       hatayı YUTMAMALI (defter mesajı düşer, iç hata görünür kalır).
//   (C) Vezne fiş açma: sıfır/eksi/boş fiyat ve adet → istek GİTMEZ, doğru
//       sebep; çok büyük tutar ve kuruş altı değer gövdeye AYNEN gider.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Q105SinirTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public Q105SinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(Q105SinirTestleri sahip) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            sahip.istekler.Add((istek.Method.Method, yol, govde));
            var g = yol switch
            {
                var y when y.EndsWith("/vezne/fisler") && istek.Method == HttpMethod.Post =>
                    """{"id":"v9","durum":"ACIK","toplamTl":0,"kalemler":[]}""",
                var y when y.EndsWith("/vezne/kuyruk") =>
                    """{"durum":"VEZNEDE","istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":0,"dahaFazlasiVar":false,"vezneZorunlu":false,"gerekceAsgari":10,"subeId":"sb1","durumKirilimi":[],"fisler":[]}""",
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez kasa","paraBirimi":"TRY"}]""",
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz"}]""",
                var y when y.EndsWith("/urunler") => """[{"id":"u-1","ad":"Bilezik"}]""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800}""",
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

    // ── (A) SayiCoz sınırları ─────────────────────────────────────────────────

    [Theory]
    [InlineData("0", "0")]
    [InlineData("-5", "-5")]
    [InlineData("-0,01", "-0.01")]
    [InlineData("0,005", "0.005")]
    [InlineData("0,0001", "0.0001")]
    [InlineData("999.999.999,99", "999999999.99")]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("1.234.567,8912", "1234567.8912")]
    public void A_SayiCoz_turkce_giris_degeri_aynen_cozer(string girdi, string beklenen)
    {
        var c = FormAlani.SayiCoz(girdi);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }

    [Theory]
    [InlineData(null, "Bos")]
    [InlineData("", "Bos")]
    [InlineData("   ", "Bos")]
    [InlineData("1.500", "Belirsiz")]
    [InlineData("-", "Gecersiz")]
    [InlineData("1,500.25", "Gecersiz")]
    [InlineData("abc", "Gecersiz")]
    public void A_SayiCoz_bos_belirsiz_gecersiz_deger_uretmez(string? girdi, string sonuc)
    {
        var c = FormAlani.SayiCoz(girdi);
        Assert.Equal(Enum.Parse<FormAlani.SayiSonucu>(sonuc), c.Sonuc);
        Assert.Null(c.Deger);
    }

    // ── (B) defter mesajı düşer ama yeni değer hâlâ hatalıysa hata YUTULMAZ ──

    [Theory]
    [InlineData("1.500")]
    [InlineData("abc")]
    public void B_Sayi_alani_hatali_yeni_degerde_ic_hatayi_gosterir(string yeni)
    {
        var f = new FormDurumu();
        f.Ekle("x-tutar", "Sıfırdan büyük bir değer girin.");
        var e = Render<Sayi>(p => p.Add(x => x.ElemanId, "x-tutar").Add(x => x.Etiket, "Tutar")
            .Add(x => x.Durum, f).Add(x => x.HataAlani, "x-tutar"));

        e.Find("#x-tutar").Input(yeni);

        Assert.False(f.Var("x-tutar"));                                   // defter mesajı düştü
        Assert.Equal("true", e.Find("#x-tutar").GetAttribute("aria-invalid")); // ama alan hâlâ HATALI
        Assert.Contains("hatali", e.Find("#x-tutar").ClassName);
    }

    [Fact]
    public void B_Sayi_alani_bosaltilinca_defter_mesaji_duser_ve_deger_null()
    {
        var f = new FormDurumu();
        f.Ekle("x-tutar", "Bu alan zorunludur.");
        decimal? son = 5m;
        var e = Render<Sayi>(p => p.Add(x => x.ElemanId, "x-tutar").Add(x => x.Etiket, "Tutar")
            .Add(x => x.Deger, 5m).Add(x => x.DegerChanged, (decimal? v) => son = v)
            .Add(x => x.Durum, f).Add(x => x.HataAlani, "x-tutar"));
        e.Find("#x-tutar").Input("");
        Assert.False(f.Var("x-tutar"));
        Assert.Null(son);
    }

    // ── (C) Vezne fiş açma tutar kapısı ──────────────────────────────────────

    private IRenderedComponent<Kuyumcu.Web.Pages.Vezne> YeniFis(string adet, string fiyat)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));
        var e = Render<Kuyumcu.Web.Pages.Vezne>().AyrintiAc();
        e.Find("#vz-yeni-ac").Click();
        SeciciYardim.Sec(e, "vz-f-urun-0", "u-1");
        e.Find("#vz-f-adet-0").Input(adet);
        e.Find("#vz-f-fiyat-0").Input(fiyat);
        e.Find("#vz-f-kaydet").Click();
        return e;
    }

    private bool FisPostEdildi() => istekler.Any(i => i.Metot == "POST" && i.Yol.EndsWith("/vezne/fisler"));

    [Theory]
    [InlineData("1", "0")]
    [InlineData("1", "-5")]
    [InlineData("1", "-0,01")]
    [InlineData("1", "")]
    [InlineData("0", "1500")]
    [InlineData("-1", "1500")]
    public void C_Vezne_sifir_eksi_bos_fiyat_ya_da_adet_istek_gondermez_dogru_sebep(string adet, string fiyat)
    {
        var e = YeniFis(adet, fiyat);
        Assert.False(FisPostEdildi());
        Assert.Equal(Kuyumcu.Core.I18n.Q105Sozlugu.Tr.VezneKalemSayiHatasi,
            e.Find("#vz-f-kalem-hata").TextContent.Trim());
    }

    [Fact]
    public void C_Vezne_belirsiz_fiyat_1_500_reddedilir_sessizce_1500_gonderilmez()
    {
        var e = YeniFis("1", "1.500");
        Assert.False(FisPostEdildi());
        // TOPLU-1 (K20): birim fiyat binlikli Para girdisi oldu — belirsiz "1.500" sebebi ALANIN ALTINDA
        // (Para iç hatası, aynı sözlük cümlesi); değer gövdeye "sessizce 1500" değil BOŞ gider → form bandı
        // genel kalem sebebini de söyler. Kural (sessiz gönderim yok + sebep yazılı) aynen korunur.
        Assert.Equal(Kuyumcu.Core.I18n.SayiGirisiSozlugu.Tr.Belirsiz, e.Find("#vz-f-fiyat-0-hata").TextContent.Trim());
        Assert.Equal(Kuyumcu.Core.I18n.Q105Sozlugu.Tr.VezneKalemSayiHatasi, e.Find("#vz-f-kalem-hata").TextContent.Trim());
    }

    [Fact]
    public void C_Vezne_cok_buyuk_tutar_govdeye_aynen_gider()
    {
        YeniFis("1", "999.999.999,99");
        var post = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/vezne/fisler"));
        using var j = System.Text.Json.JsonDocument.Parse(post.Govde);
        var k = j.RootElement.GetProperty("kalemler")[0];
        Assert.Equal(999999999.99m, k.GetProperty("birimFiyatTl").GetDecimal());
        Assert.Equal(1m, k.GetProperty("adet").GetDecimal());
    }

    [Fact]
    public void C_Vezne_kurus_alti_ve_4_hane_istemcide_yuvarlanmadan_gider()
    {
        // İstemci yuvarlamaz — bu kural AYNEN (değer ne yuvarlanır ne sessizce gider).
        // GIRDI-AILESI-2 tur 4 (doğrulayıcı YÜKSEK · K9 "en çok 2 ondalık, fazlası sessizce yuvarlanmaz,
        // hata"): ortak Para bileşeni artık Tur verilmese de 2 ondalığı uygular. "0,005" birim fiyat
        // alan altında Core metniyle hata verir ve gövdeye HİÇ gitmez (eskiden 0,005 gidip sunucuda
        // AwayFromZero ile 0,01 oluyordu — görünen ≠ kaydedilen).
        var e = YeniFis("0,0001", "0,005");
        Assert.False(FisPostEdildi());
        Assert.Equal(Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(
                new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(Kuyumcu.Core.Domain.Girdi.GirdiKodu.SayiOndalik, 2), "tr"),
            e.Find("#vz-f-fiyat-0-hata").TextContent.Trim());
    }

    [Fact]
    public void C_Vezne_hata_sonrasi_fiyat_duzelince_mesaj_duser_ve_gonderim_basarili()
    {
        var e = YeniFis("1", "0");
        Assert.Single(e.FindAll("#vz-f-kalem-hata"));
        e.Find("#vz-f-fiyat-0").Input("45.000");
        Assert.Empty(e.FindAll("#vz-f-kalem-hata"));
        e.Find("#vz-f-kaydet").Click();
        var post = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/vezne/fisler"));
        using var j = System.Text.Json.JsonDocument.Parse(post.Govde);
        Assert.Equal(45000m, j.RootElement.GetProperty("kalemler")[0].GetProperty("birimFiyatTl").GetDecimal());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q10-5 — HASIM DOĞRULAYICI 2. TUR (24.09 22:xx) EK SINIR TESTLERİ.
// Q10-5 ekstre ekranında tutarı Bicim.Para ile basar ve FormDurumu'nun
// Pozitif/NegatifDegil kapılarını (form hatası altyapısı) değiştirdi. Beklenen
// değerler SABİT; proje kuralı MidpointRounding.AwayFromZero, eksi işareti
// U+2212, ayraç U+00A0 ₺ (Bicim.TlYaz).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class Q105SinirTestleriTur2
{
    private static DilServisi Dil() => new(null!);

    [Theory]
    [InlineData("0", "0 ₺")]
    [InlineData("0.005", "0,01 ₺")]
    [InlineData("0.004", "0 ₺")]
    [InlineData("-0.005", "−0,01 ₺")]
    [InlineData("-1250.5", "−1.250,50 ₺")]
    [InlineData("1250.50", "1.250,50 ₺")]
    [InlineData("999999999.99", "999.999.999,99 ₺")]
    public void D2_Ekstre_tutari_Para_TRY_trTR_ve_AwayFromZero(string ham, string beklenen)
    {
        var n = decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(beklenen, Bicim.Para(n, "TRY"));
        Assert.Equal(beklenen, Bicim.Para(n, null));
        Assert.DoesNotContain("TRY", Bicim.Para(n, "TRY"));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("0", false)]
    [InlineData("-0.01", false)]
    [InlineData("0.0001", true)]
    [InlineData("999999999.99", true)]
    public void D2_FormDurumu_Pozitif_sifir_eksi_bos_reddeder_4_hane_kabul(string? ham, bool gecer)
    {
        var f = new FormDurumu();
        decimal? n = ham is null ? null : decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(gecer, f.Pozitif("tutar", n, Dil()));
        Assert.Equal(gecer, f.Gecerli);
        if (!gecer) Assert.False(string.IsNullOrWhiteSpace(f.Mesaj("tutar")));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("0", true)]
    [InlineData("-0.0001", false)]
    public void D2_FormDurumu_NegatifDegil_sinirlari(string? ham, bool gecer)
    {
        var f = new FormDurumu();
        decimal? n = ham is null ? null : decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(gecer, f.NegatifDegil("gram", n, Dil()));
    }

    [Fact]
    public void D2_Tutar_duzeltilince_mesaj_duser_hala_hataliysa_yeniden_yazilir()
    {
        var f = new FormDurumu();
        Assert.False(f.Pozitif("tutar", -5m, Dil()));
        Assert.True(f.Sil("tutar"));           // kullanıcı değeri değiştirdi
        Assert.True(f.Gecerli);
        Assert.False(f.Pozitif("tutar", 0m, Dil())); // yeni değer hâlâ hatalı → yeniden yazılır
        Assert.Equal(1, f.HataSayisi);
    }
}

/// <summary>
/// Q10-5 hasım doğrulayıcı TUR 3 (24.09 23:3x) — kendi sınır testleri. Paketin dokunduğu
/// sayısal yollar: ortak sayı girdisi (SayisalAlan → FormAlani.SayiCoz, defterden silme) ve
/// ekstre tutar gösterimi (Bicim.Para, satırın kendi para birimiyle). Beklenen değerler SABİT
/// yazılmıştır; üretim kodundan hesaplanmaz.
/// </summary>
public sealed class Q105SinirTestleriTur3
{
    [Theory]
    [InlineData("0", "0")]
    [InlineData("-5", "-5")]
    [InlineData("-1.250,50", "-1250.50")]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("0,005", "0.005")]
    [InlineData("0,0001", "0.0001")]
    [InlineData("999.999.999,99", "999999999.99")]
    public void T3_SayiCoz_sinirlar_deger_ve_isaret_aynen(string girdi, string beklenen)
    {
        var c = FormAlani.SayiCoz(girdi);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }

    [Theory]
    [InlineData(null, "Bos")]
    [InlineData("   ", "Bos")]
    [InlineData("1.500", "Belirsiz")]
    [InlineData("12abc", "Gecersiz")]
    [InlineData("-", "Gecersiz")]
    public void T3_SayiCoz_bos_belirsiz_gecersiz_deger_yok(string? girdi, string sonuc)
    {
        var c = FormAlani.SayiCoz(girdi);
        Assert.Equal(sonuc, c.Sonuc.ToString());
        Assert.Null(c.Deger);
    }

    [Theory]
    [InlineData("0.005", "USD", "0,01 $")]
    [InlineData("0.015", "USD", "0,02 $")]
    [InlineData("0.004", "EUR", "0,00 €")]
    [InlineData("-0.005", "USD", "-0,01 $")]
    [InlineData("999999999.99", "EUR", "999.999.999,99 €")]
    [InlineData("0", "GBP", "0,00 £")]
    public void T3_Ekstre_doviz_tutari_trTR_AwayFromZero(string ham, string pb, string beklenen)
    {
        var n = decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(beklenen, Bicim.Para(n, pb));
    }
}
