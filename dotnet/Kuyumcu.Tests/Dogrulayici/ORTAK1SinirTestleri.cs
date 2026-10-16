using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests.Dogrulayici;

// ════════════════════════════════════════════════════════════════════════
// PAKET ORTAK-1 — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet, 29.09)
//
// Paket para HESAPLAMAZ; para GÖSTEREN yola (StatKart, Panel/Raporlar
// .stat-deger, AltinPozisyonuKarti #pozisyon-net) yazı boyu kademesi
// (DegerBoyu.Kademe → data-deger-boy) ekler. Sınır: değer/biçim DEĞİŞMEMELİ,
// kademe uç tutarlarda doğru olmalı, uzun kademede (white-space: normal)
// tutar ile "₺" arasındaki boşluk BÖLÜNEMEZ (U+00A0) kalmalı.
// Beklenen değerler SABİT metindir; üretim kodundan hesaplanmaz.
// ════════════════════════════════════════════════════════════════════════
public sealed class ORTAK1SinirTestleri : BunitContext
{
    private const string Nb = " ";   // bölünmez boşluk
    private const string Eksi = "−"; // tipografik eksi

    private (string Metin, string Kademe) Kart(string deger)
    {
        var p = Render<StatKart>(x => x.Add(k => k.Etiket, "Kasa").Add(k => k.Deger, deger)).Find("p.stat-deger");
        return (p.TextContent, p.GetAttribute("data-deger-boy") ?? "");
    }

    [Fact]
    public void OS1_sifir_tutar_aynen_basilir_kisa_kademe()
    {
        Assert.Equal("0" + Nb + "₺", Bicim.Tl(0m));
        var (m, k) = Kart(Bicim.Tl(0m));
        Assert.Equal("0" + Nb + "₺", m);
        Assert.Equal("kisa", k);
    }

    [Fact]
    public void OS2_eksi_tutar_isaret_korunur_uzun_kademe()
    {
        var (m, k) = Kart(Bicim.Tl(-728188.36m));
        Assert.Equal(Eksi + "728.188,36" + Nb + "₺", m);   // taban turundaki taşan değer
        Assert.Equal("uzun", k);                            // 13 karakter
    }

    [Fact]
    public void OS3_kurus_yuvarlama_0005_sinirinda_gosterim_degismez()
    {
        // Proje kuralı: MidpointRounding.AwayFromZero (Bicim.Tl).
        Assert.Equal("0,01" + Nb + "₺", Kart(Bicim.Tl(0.005m)).Metin);
        Assert.Equal("0" + Nb + "₺", Kart(Bicim.Tl(0.004m)).Metin);
        Assert.Equal("0" + Nb + "₺", Kart(Bicim.Tl(-0.004m)).Metin);   // işaretsiz sıfır
        Assert.Equal(Eksi + "0,01" + Nb + "₺", Kart(Bicim.Tl(-0.005m)).Metin);
    }

    [Fact]
    public void OS4_cok_buyuk_tutar_cok_uzun_kademe_ve_bolunmez_bosluk()
    {
        var (m, k) = Kart(Bicim.Tl(999999999.99m));
        Assert.Equal("999.999.999,99" + Nb + "₺", m);
        Assert.Equal("cok-uzun", k);   // 16 karakter → white-space: normal kademesi
        var (me, ke) = Kart(Bicim.Tl(-999999999.99m));
        Assert.Equal(Eksi + "999.999.999,99" + Nb + "₺", me);
        Assert.Equal("cok-uzun", ke);
        // Uzun kademede satır yalnız U+0020'de kırılabilir; tutarda hiç yok.
        Assert.DoesNotContain(' ', m);
        Assert.DoesNotContain(' ', me);
    }

    [Fact]
    public void OS5_bos_ve_null_deger_kisa_kademe_bos_metin()
    {
        Assert.Equal("kisa", DegerBoyu.Kademe(null));
        var (m, k) = Kart("");
        Assert.Equal("", m);
        Assert.Equal("kisa", k);
    }

    [Fact]
    public void OS6_has_4_hane_gosterimi_kademe_sinirlari()
    {
        Assert.Equal("0,0001 has", Bicim.Has(0.0001m));
        Assert.Equal("orta", DegerBoyu.Kademe(Bicim.Has(0.0001m)));          // 10 karakter
        Assert.Equal("uzun", DegerBoyu.Kademe(Bicim.Has(1234.5678m)));       // "1.234,5678 has" 13
        // MADEN-AYRIMI tur 2: Bicim.Has binlik gruplu (N4) → "123.456,7891 has" 16 karakter = cok-uzun.
        Assert.Equal("cok-uzun", DegerBoyu.Kademe(Bicim.Has(123456.7891m)));
        Assert.Equal("cok-uzun", DegerBoyu.Kademe(Bicim.Has(1234567.8912m))); // 16
    }

    [Fact]
    public void OS7_kademe_sinirlari_sabit_karakter_sayisiyla()
    {
        Assert.Equal("kisa", DegerBoyu.Kademe("123456789"));
        Assert.Equal("orta", DegerBoyu.Kademe("1234567890"));
        Assert.Equal("orta", DegerBoyu.Kademe("123456789012"));
        Assert.Equal("uzun", DegerBoyu.Kademe("1234567890123"));
        Assert.Equal("uzun", DegerBoyu.Kademe("123456789012345"));
        Assert.Equal("cok-uzun", DegerBoyu.Kademe("1234567890123456"));
        Assert.Equal("cok-uzun", DegerBoyu.Kademe("12345678901234567890"));
        Assert.Equal("en-uzun", DegerBoyu.Kademe("123456789012345678901"));
    }
}

// ════════════════════════════════════════════════════════════════════════
// PAKET ORTAK-1 — HASIM DOĞRULAYICI SINIR TESTLERİ, 30.09 turu (Aşkın + Mehmet)
// Bu turda KarZarar.razor BASİT modda has gramını 2 ondalığa yuvarlayarak
// GÖSTERMEYE başladı (Gram(): BASİT N2, PRO N4). Sınır: has 4 hane (0,0001),
// kuruş yarısı (0,005), eksi işaret, çok büyük değer, sıfır. Beklenen değerler
// SABİT metindir (tr-TR); üretim kodundan hesaplanmaz.
// ════════════════════════════════════════════════════════════════════════
public sealed class ORTAK1KarZararSinirTestleri : BunitContext
{
    private string karHas = "0", hasilatHas = "0", karTl = "0";

    public ORTAK1KarZararSinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new System.Net.Http.HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<RaporServisi>();
    }

    private string Govde() => "{\"baslangic\":\"2026-09-01\",\"bitis\":\"2026-09-17\",\"gunSayisi\":17,\"subeId\":null," +
        "\"satisAdedi\":1,\"iadeAdedi\":0,\"brutHasilatTl\":1000,\"iadeTl\":0,\"hasilatTl\":1000,\"brutMaliyetTl\":500,\"iadeMaliyetTl\":0,\"maliyetTl\":500," +
        "\"karTl\":" + karTl + ",\"karMarjiYuzde\":50,\"iscilikTl\":0,\"takasFarkTl\":0,\"takasAdedi\":0,\"takasNotu\":null,\"donemKariTl\":" + karTl + "," +
        "\"hasilatHas\":" + hasilatHas + ",\"iadeHas\":0,\"maliyetHas\":0,\"karHas\":" + karHas + "," +
        "\"hasilatHasDefterGram\":0,\"altinHasilatHasDefterGram\":0,\"hasDefterMadenDagilimi\":[],\"madenUyarisi\":null,\"hasTanimNotu\":\"n\"," +
        "\"karKoprusu\":[],\"storno\":{\"adet\":0,\"tutarTl\":0,\"notu\":\"\"},\"karsilastir\":null,\"onceki\":null,\"degisimYuzde\":null," +
        "\"maliyetiBilinenAdet\":1,\"maliyetiBilinmeyenAdet\":0,\"kuruBulunamayanSatis\":0,\"guvenilirlik\":\"TAM\",\"uyari\":null}";

    private sealed class Sahte(ORTAK1KarZararSinirTestleri t) : System.Net.Http.HttpMessageHandler
    {
        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage i, CancellationToken c)
        {
            var yol = i.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.Contains("kar-zarar/urun") => "{\"satirlar\":[],\"urunSayisi\":0,\"limit\":50,\"dahaFazlasiVar\":false,\"guvenilirlik\":\"TAM\"}",
                var y when y.Contains("kar-zarar") => t.Govde(),
                var y when y.Contains("stok-degerlemesi") => "{\"toplamAdet\":0,\"toplamHasGram\":0,\"guvenilirlik\":\"TAM\",\"kaynakDagilimi\":[]}",
                var y when y.EndsWith("/kur/guncel") => "{\"id\":\"kur-1\",\"altinHasFiyati\":6800,\"gramAltinFiyati\":6850,\"usdTry\":34.0,\"eurTry\":37.0,\"gbpTry\":43.5,\"kaynak\":\"ALTINKAYNAK\",\"olusturuldu\":\"2026-09-17T09:00:00Z\",\"ayarFiyatlari\":[]}",
                var y when y.EndsWith("/subeler") => "[{\"id\":\"s1\",\"ad\":\"Merkez\",\"aktif\":true}]",
                _ => "{}",
            };
            return Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new System.Net.Http.StringContent(g, System.Text.Encoding.UTF8, "application/json") });
        }
    }

    private (string Kar, string Hasilat, string Ozet) Olc(bool basit, string kar, string hasilat = "0", string tl = "0")
    {
        karHas = kar; hasilatHas = hasilat; karTl = tl;
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-panel-ozet")));
        var kartlar = c.FindAll("#kz-panel-ozet .stat-kart").Select(k => (k.QuerySelector(".stat-etiket, .kart-baslik, p:not(.stat-deger)")?.TextContent ?? "", k.QuerySelector(".stat-deger")?.TextContent.Trim() ?? "")).ToList();
        var degerler = c.FindAll("#kz-panel-ozet .stat-deger").Select(p => p.TextContent.Trim()).ToList();
        return (string.Join("|", degerler), "", c.Find("#kz-panel-ozet").TextContent);
    }

    [Fact]
    public void OK1_sifir_has_BASIT_0_00_PRO_0_0000()
    {
        // MADEN-AYRIMI (ekip kararı 5): has iki modda AYNI kural — Bicim.Has, 4 hane, "has" (BASİT N2 kalktı;
        // BASİT yuvarlaması PRO'dan ayrı rakam üretiyordu: 19,16 ↔ 19,1520). Niyet korunur: sıfırdan farklı has sıfır görünmez.
        Assert.Contains("0,0000 has", Olc(true, "0").Kar);
        Assert.Contains("0,0000 has", Olc(false, "0").Kar);
    }

    [Fact]
    public void OK2_has_4_hane_0_0001_PRO_korunur()
    {
        // MADEN-AYRIMI (ekip kararı 5): has iki modda AYNI kural — Bicim.Has, 4 hane, "has" (BASİT N2 kalktı;
        // BASİT yuvarlaması PRO'dan ayrı rakam üretiyordu: 19,16 ↔ 19,1520). Niyet korunur: sıfırdan farklı has sıfır görünmez.
        Assert.Contains("0,0001 has", Olc(false, "0.0001").Kar);
    }

    [Fact]
    public void OK3_has_4_hane_0_0001_BASIT_sifir_gibi_gorunmez()
    {
        // 0,0001 g kâr BASİT'te "0,00 g" olarak basılırsa sıfırdan ayırt edilemez.
        // ORTAK-1 uygulayıcı 30.09 (devir): özgün iddia `DoesNotContain("0,00 g|", tümKartlar)` idi; bu
        // fikstürde hasılat ve maliyet has'ı GERÇEKTEN 0'dır ve OK1 sıfırın BASİT'te "0,00 g" olmasını
        // ister — iki iddia birbirini dışlıyordu. Niyet (yorum satırı) korunarak iddia KÂR kartına
        // daraltıldı: hasılat/maliyet sıfır kartları "0,00 g", kâr kartı "0,0001 g" — üçü sırayla.
        // MADEN-AYRIMI (ekip kararı 5): has iki modda AYNI kural — Bicim.Has, 4 hane, "has" (BASİT N2 kalktı;
        // BASİT yuvarlaması PRO'dan ayrı rakam üretiyordu: 19,16 ↔ 19,1520). Niyet korunur: sıfırdan farklı has sıfır görünmez.
        var degerler = Olc(true, "0.0001").Kar.Split('|');
        var i = Array.IndexOf(degerler, "0,0001 has");
        Assert.True(i >= 2, string.Join("|", degerler));
        Assert.Equal(["0,0000 has", "0,0000 has", "0,0001 has"], degerler[(i - 2)..(i + 1)]);
    }

    [Fact]
    public void OK4_yarim_yuzde_0_005_BASIT_yukari_yuvarlanir()
    {
        // MADEN-AYRIMI (ekip kararı 5): has iki modda AYNI kural — Bicim.Has, 4 hane, "has" (BASİT N2 kalktı;
        // BASİT yuvarlaması PRO'dan ayrı rakam üretiyordu: 19,16 ↔ 19,1520). Niyet korunur: sıfırdan farklı has sıfır görünmez.
        Assert.Contains("0,0050 has", Olc(true, "0.005").Kar);   // 4 hane: yuvarlama yok
    }

    [Fact]
    public void OK5_eksi_kucuk_has_BASIT_eksi_sifir_basilmaz()
    {
        var k = Olc(true, "-0.004").Kar;
        Assert.DoesNotContain("-0,00 g", k);
        Assert.DoesNotContain("−0,00 g", k);
    }

    [Fact]
    public void OK6_eksi_has_isaret_korunur_BASIT()
    {
        // MADEN-AYRIMI (ekip kararı 5): has iki modda AYNI kural — Bicim.Has, 4 hane, "has" (BASİT N2 kalktı;
        // BASİT yuvarlaması PRO'dan ayrı rakam üretiyordu: 19,16 ↔ 19,1520). Niyet korunur: sıfırdan farklı has sıfır görünmez.
        Assert.Contains("-1,2549 has", Olc(true, "-1.2549").Kar);
    }

    [Fact]
    public void OK7_cok_buyuk_has_BASIT_ve_PRO()
    {
        // MADEN-AYRIMI (ekip kararı 5): has iki modda AYNI kural — Bicim.Has, 4 hane, "has" (BASİT N2 kalktı;
        // BASİT yuvarlaması PRO'dan ayrı rakam üretiyordu: 19,16 ↔ 19,1520). Niyet korunur: sıfırdan farklı has sıfır görünmez.
        Assert.Contains("999.999.999,9900 has", Olc(true, "999999999.99").Kar);
        Assert.Contains("999.999.999,9999 has", Olc(false, "999999999.9999").Kar);
    }

    [Fact]
    public void OK8_tl_kar_has_kaybi_uyarisi_BASIT_kayip_gramini_sifir_yazmaz()
    {
        // KarTl > 0 && KarHas < 0 → "... {1} gram has KAYBETTİNİZ" uyarısı çıkar.
        // Kayıp 0,004 g iken BASİT N2 "0,00 gram has KAYBETTİNİZ" yazarsa uyarı kendisiyle çelişir.
        var ozet = Olc(true, "-0.004", "0", "1500").Ozet;
        Assert.Contains("KAYBETTİNİZ", ozet);
        Assert.DoesNotContain("ama 0,00 gram", ozet);
    }
}

// ════════════════════════════════════════════════════════════════════════
// PAKET ORTAK-1 — HASIM DOĞRULAYICI TUR 3 (30.09): Bicim.KarHasGram saf sınırları.
// Beklenen değerler SABİT tr-TR metindir; üretim kodundan hesaplanmaz. İş parçacığı
// kültürü en-US yapılır: biçim kültürden bağımsız olmalı.
// ════════════════════════════════════════════════════════════════════════
public sealed class ORTAK1KarHasGramSinirTestleri
{
    private static string B(decimal n, bool sade)
    {
        var eski = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        try { return Bicim.KarHasGram(n, sade); }
        finally { System.Globalization.CultureInfo.CurrentCulture = eski; }
    }

    [Theory]
    [InlineData("0", true, "0,00")]
    [InlineData("0", false, "0,0000")]
    [InlineData("-1.2549", true, "-1,25")]
    [InlineData("-1.2549", false, "-1,2549")]
    [InlineData("0.005", true, "0,01")]      // AwayFromZero, 2 hane sınırı
    [InlineData("-0.005", true, "-0,01")]
    [InlineData("0.0049", true, "0,0049")]   // 2 haneye sıfıra düşer → 4 hane
    [InlineData("0.0001", true, "0,0001")]
    [InlineData("-0.0001", true, "-0,0001")]
    [InlineData("0.0001", false, "0,0001")]
    [InlineData("0.00004", true, "0,00")]    // 4 hane altı sıfır sayılır; eksi sıfır yok
    [InlineData("-0.00004", true, "0,00")]
    [InlineData("0.00005", true, "0,0001")]  // 4 hanede AwayFromZero
    [InlineData("999999999.99", true, "999.999.999,99")]
    [InlineData("999999999.9999", false, "999.999.999,9999")]
    [InlineData("-999999999.99", true, "-999.999.999,99")]
    [InlineData("1234.5678", true, "1.234,57")]
    public void DOG3_KarHasGram_sinir(string girdi, bool sade, string beklenen)
    {
        var n = decimal.Parse(girdi, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(beklenen, B(n, sade));
    }
}

// ════════════════════════════════════════════════════════════════════════
// PAKET ORTAK-1 · KEŞİF #4 (30.09-c) — HASIM DOĞRULAYICI SINIR TESTLERİ
// Fiş dışı formlara bağlanan GecersizAlanKapisi EK üyeleri (Sebep/FormaIsle/
// OnekleVar/Func<Task> kurucu) + SayiKapisiSebebi + kapılı Para/Gram kutusunun
// tr-TR sınır girdileri. Beklenen değerler SABİT; üretim kodundan hesaplanmaz.
// ════════════════════════════════════════════════════════════════════════
public sealed class ORTAK1K4SinirTestleri : BunitContext
{
    private const string SebepTr = "«Tutar» kutusundaki sayıyı düzeltin — kutunun altındaki uyarıya bakın.";

    public ORTAK1K4SinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private (IRenderedComponent<Microsoft.AspNetCore.Components.CascadingValue<Kuyumcu.Web.Bilesenler.Form.GecersizAlanKapisi>> c,
             Kuyumcu.Web.Bilesenler.Form.GecersizAlanKapisi k, List<decimal?> giden) Kutu(string id, bool gram = false)
    {
        var k = new Kuyumcu.Web.Bilesenler.Form.GecersizAlanKapisi();
        var giden = new List<decimal?>();
        var c = Render<Microsoft.AspNetCore.Components.CascadingValue<Kuyumcu.Web.Bilesenler.Form.GecersizAlanKapisi>>(p => p
            .Add(x => x.Value, k).Add(x => x.IsFixed, true)
            .AddChildContent(b =>
            {
                if (gram) b.OpenComponent<Kuyumcu.Web.Bilesenler.Form.Gram>(0);
                else b.OpenComponent<Kuyumcu.Web.Bilesenler.Form.Para>(0);
                b.AddAttribute(1, "ElemanId", id);
                b.AddAttribute(2, "Etiket", "Tutar");
                b.AddAttribute(3, "Deger", (decimal?)0m);
                b.AddAttribute(4, "DegerChanged", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<decimal?>(this, v => giden.Add(v)));
                b.CloseComponent();
            }));
        return (c, k, giden);
    }

    private IRenderedComponent<Kuyumcu.Web.Bilesenler.Form.SayiKapisiSebebi> SebepSatiri(Kuyumcu.Web.Bilesenler.Form.GecersizAlanKapisi k) =>
        Render<Kuyumcu.Web.Bilesenler.Form.SayiKapisiSebebi>(p => p.Add(x => x.ElemanId, "h-sayi-kilit").Add(x => x.Onek, "h-").Add(x => x.Kapi, k));

    [Theory]
    [InlineData("0", "0")]                         // sıfır geçerli
    [InlineData("-5", "-5")]                       // eksi: işaret korunur, kapı YALNIZ ayrıştırma hatası için
    // GIRDI-AILESI-2 tur 3 (K9): "0,005" artık geçerli DEĞİL — Para varsayılan 2 ondalık; ayrı sınır testi aşağıda.
    [InlineData("999.999.999,99", "999999999.99")] // çok büyük tutar, binlik noktalı
    [InlineData("45.000", "45000")]                // tr-TR binlik
    [InlineData("1,5", "1.5")]                     // tr-TR ondalık
    public void K4S_gecerli_tr_TR_girdi_kapiya_yazilmaz_yazilan_deger_gider(string yazilan, string beklenen)
    {
        var (c, k, giden) = Kutu("h-tutar");
        c.Find("#h-tutar").Input(yazilan);
        Assert.False(k.Var);
        Assert.False(k.OnekleVar("h-"));
        Assert.Null(k.Sebep("tr", "h-"));
        Assert.Empty(SebepSatiri(k).FindAll("#h-sayi-kilit"));
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), giden[^1]);
    }

    /// <summary>
    /// GIRDI-AILESI-2 tur 3 (doğrulayıcı YÜKSEK · K9): kuruştan küçük girdi YUVARLANMAZ ve GEÇMEZ — alan altı
    /// Core metni + kapı (sebep AYNI metin), çağırana null gider (eskiden "0,005" olduğu gibi gidiyordu).
    /// </summary>
    [Fact]
    public void K4S_kurus_alti_0_005_kapiya_yazilir_yuvarlanmaz_null_gider()
    {
        var (c, k, giden) = Kutu("h-tutar");
        c.Find("#h-tutar").Input("0,005");
        var mesaj = Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(Kuyumcu.Core.Domain.Girdi.GirdiKodu.SayiOndalik, 2), "tr");
        Assert.True(k.OnekleVar("h-"));
        Assert.Equal(mesaj, k.Sebep("tr", "h-"));
        Assert.Null(giden[^1]);
    }

    [Fact]
    public void K4S_gram_4_hane_0_0001_korunur()
    {
        var (c, k, giden) = Kutu("h-has", gram: true);
        c.Find("#h-has").Input("0,0001");
        Assert.False(k.Var);
        Assert.Equal(0.0001m, giden[^1]);
    }

    [Fact]
    public void K4S_bos_kutu_null_gider_kapi_bos()
    {
        var (c, k, giden) = Kutu("h-tutar");
        c.Find("#h-tutar").Input("5");
        c.Find("#h-tutar").Input("");
        Assert.False(k.Var);
        Assert.Null(giden[^1]);
        Assert.Empty(SebepSatiri(k).FindAll("#h-sayi-kilit"));
    }

    [Fact]
    public void K4S_tus_tus_1_500_BELIRSIZ_null_gider_kapi_dolu_sebep_etiketle_sonra_duzeltilince_1500()
    {
        var (c, k, giden) = Kutu("h-tutar");
        foreach (var a in new[] { "1", "1.", "1.5", "1.50", "1.500" }) c.Find("#h-tutar").Input(a);
        c.Find("#h-tutar").Blur();
        Assert.True(k.OnekleVar("h-"));
        Assert.Null(giden[^1]);                                   // ara değer 1,5 GİTMEZ
        Assert.Equal("1.500", c.Find("#h-tutar").GetAttribute("value"));
        Assert.Equal(SebepTr, k.Sebep("tr", "h-"));
        Assert.Equal("Fix the number in “Tutar” — see the note under the box.", k.Sebep("en", "h-"));
        Assert.Equal(SebepTr, SebepSatiri(k).Find("#h-sayi-kilit").TextContent.Trim());
        c.Find("#h-tutar").Input("1500");
        Assert.False(k.Var);
        Assert.Equal(1500m, giden[^1]);
        Assert.Empty(SebepSatiri(k).FindAll("#h-sayi-kilit"));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1,2,3")]
    [InlineData("12.345")]
    public void K4S_anlasilamayan_ve_belirsiz_null_gider_kapi_dolu(string yazilan)
    {
        var (c, k, giden) = Kutu("h-tutar");
        c.Find("#h-tutar").Input(yazilan);
        Assert.True(k.Var);
        Assert.Null(giden[^1]);
    }

    [Fact]
    public void K4S_onek_ayrimi_baska_formun_gecersiz_alani_bu_formu_kilitlemez()
    {
        var k = new Kuyumcu.Web.Bilesenler.Form.GecersizAlanKapisi();
        k.Bildir("gun-sayilan", "Sayılan", true);
        Assert.False(k.OnekleVar("h-"));
        Assert.Null(k.Sebep("tr", "h-"));
        Assert.True(k.OnekleVar("gun-"));
        Assert.Equal("«Sayılan» kutusundaki sayıyı düzeltin — kutunun altındaki uyarıya bakın.", k.Sebep("tr", "gun-"));
        // "gd-tutar" öneki "gd-v-tutar"ı KAPSAMAZ (Gider virmanı ayrı form)
        k.Bildir("gd-v-tutar", "Tutar", true);
        Assert.False(k.OnekleVar("gd-tutar"));
        Assert.True(k.OnekleVar("gd-v-"));
    }

    [Fact]
    public void K4S_FormaIsle_yalniz_onekli_alanlari_forma_isler()
    {
        var k = new Kuyumcu.Web.Bilesenler.Form.GecersizAlanKapisi();
        var f = new FormDurumu();
        Assert.False(k.FormaIsle(f, "u-", "X"));
        Assert.True(f.Gecerli);
        k.Bildir("d-gram", "Gram", true);
        Assert.False(k.FormaIsle(f, "u-", "X"));
        Assert.True(f.Gecerli);
        k.Bildir("u-iscilik", "İşçilik", true);
        Assert.True(k.FormaIsle(f, "u-", "X"));
        Assert.False(f.Gecerli);
        Assert.Equal(1, f.HataSayisi);
        Assert.Equal("X", f.Mesaj("u-iscilik"));
        Assert.Null(f.Mesaj("d-gram"));
    }

    [Fact]
    public void K4S_tazele_kurucusu_kapanmis_sayfada_patlamaz()
    {
        var sayac = 0;
        var k1 = new Kuyumcu.Web.Bilesenler.Form.GecersizAlanKapisi((Func<Task>)(() => { sayac++; throw new ObjectDisposedException("sayfa"); }));
        k1.Bildir("u-x", "X", true);
        k1.Bildir("u-x", "X", false);
        Assert.Equal(2, sayac);
        var k2 = new Kuyumcu.Web.Bilesenler.Form.GecersizAlanKapisi((Func<Task>)(() => throw new InvalidOperationException("renderer yok")));
        k2.Bildir("u-x", "X", true);
        Assert.True(k2.Var);
    }
}
