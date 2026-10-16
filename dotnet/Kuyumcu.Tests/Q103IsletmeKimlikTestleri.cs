using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Cikti;
using Kuyumcu.Core.Domain;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q10-3 — İŞLETME KİMLİĞİ: adres · telefon · vergi dairesi · VKN/TCKN
// (QA turu 10 KRİTİK: basit modda "İşletme kimliğini düzenle" çekmecesinde
//  yalnız "İşletme adı" vardı; dört alan HİÇBİR YERDEN düzenlenemiyordu.)
//
// Ölçülen:
//   [saf]   VKN/TCKN BİÇİM kuralı sınırları (boş, 9/10/11/12 hane, harfli,
//           boşluklu, Unicode rakam) + TR/EN mesaj ayrımı
//   [HTTP]  sunucu aynı sınırları uygular; 400'de satır DEĞİŞMEZ; mesaj Türkçe
//           ve türüne göre ayrı; denetim izi ESKİ → YENİ (maskeli) yazar;
//           çıktı belgesi (müşteri ekstresi) YENİ adres/VKN'yi basar
//   [bUnit] BASİT modda beş alan çizilir ve gövde dört alanı TAŞIR; hatalı
//           biçimde alan altı Türkçe mesaj + Kaydet kilitli + istek GİTMEZ
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Q103VergiKimlikSafTestleri
{
    [Theory]
    [InlineData(null, VergiKimlikHatasi.Yok)]
    [InlineData("", VergiKimlikHatasi.Yok)]
    [InlineData("   ", VergiKimlikHatasi.Yok)]
    [InlineData("123456789", VergiKimlikHatasi.HaneSayisi)]      // 9 hane
    [InlineData("1234567890", VergiKimlikHatasi.Yok)]            // 10 hane VKN
    [InlineData("12345678950", VergiKimlikHatasi.Yok)]           // 11 hane TCKN
    [InlineData("123456789012", VergiKimlikHatasi.HaneSayisi)]   // 12 hane
    [InlineData("12345abcde", VergiKimlikHatasi.RakamDisi)]      // harfli
    [InlineData("12345 67890", VergiKimlikHatasi.RakamDisi)]     // iç boşluk
    [InlineData(" 1234567890 ", VergiKimlikHatasi.Yok)]          // dış boşluk kırpılır
    [InlineData("123-456-7890", VergiKimlikHatasi.RakamDisi)]
    [InlineData("١٢٣٤٥٦٧٨٩٠", VergiKimlikHatasi.RakamDisi)]      // Unicode rakam ('\d' açığı)
    public void Q103_S01_Bicim_siniri(string? girdi, VergiKimlikHatasi beklenen)
        => Assert.Equal(beklenen, VergiKimlik.Denetle(girdi));

    [Fact]
    public void Q103_S02_Mesaj_turune_gore_ayrilir_ve_iki_dilde_farklidir()
    {
        Assert.Equal(VergiKimlik.RakamDisiMesaj, VergiKimlik.Mesaj("12a4567890"));
        Assert.Equal(VergiKimlik.HaneMesaj, VergiKimlik.Mesaj("123"));
        Assert.Null(VergiKimlik.Mesaj("1234567890"));

        var tr = IsletmeKimlikSozlugu.VergiNoMesaji("tr", "12a4567890");
        var en = IsletmeKimlikSozlugu.VergiNoMesaji("en", "12a4567890");
        Assert.False(string.IsNullOrWhiteSpace(tr));
        Assert.False(string.IsNullOrWhiteSpace(en));
        Assert.NotEqual(tr, en);
        Assert.NotEqual(IsletmeKimlikSozlugu.VergiNoMesaji("tr", "123"), tr);
        Assert.Null(IsletmeKimlikSozlugu.VergiNoMesaji("tr", "12345678950"));
    }

    [Fact]
    public void Q103_S03_Denetim_degeri_vergi_no_ve_telefonu_maskeler()
    {
        Assert.Equal("******7890", Kuyumcu.Api.Modules.Admin.AdminServisi.DenetimDegeri("vergi_no", "1234567890"));
        Assert.Equal("*******0101", Kuyumcu.Api.Modules.Admin.AdminServisi.DenetimDegeri("telefon", "02125550101"));
        Assert.Equal("(boş)", Kuyumcu.Api.Modules.Admin.AdminServisi.DenetimDegeri("adres", null));
        Assert.DoesNotContain("Kat No:12",
            Kuyumcu.Api.Modules.Admin.AdminServisi.DenetimDegeri("adres", "Kuyumcukent 1. Kat No:12 İstanbul"));
    }
}

/// <summary>Q10-3 doğrulayıcı bulgusu — işletme adı KIRPILMIŞ uzunluk kuralı (saf).</summary>
public sealed class Q103IsletmeAdiSafTestleri
{
    /// <summary>Doğrulayıcı bulgusu (DÜŞÜK): " x " kırpılınca tek karakter — kabul EDİLMEMELİ.</summary>
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("x", false)]
    [InlineData(" x ", false)]
    [InlineData("  x  ", false)]
    [InlineData("xy", true)]
    [InlineData(" xy ", true)]
    public void Q103_S04_Isletme_adi_KIRPILMIS_uzunlukla_olculur(string? girdi, bool gecerli)
        => Assert.Equal(gecerli, IsletmeAdi.Gecerli(girdi));

    [Fact]
    public void Q103_S05_Isletme_adi_ust_siniri_200_KIRPILMIS()
    {
        Assert.True(IsletmeAdi.Gecerli(new string('a', 200)));
        Assert.True(IsletmeAdi.Gecerli("  " + new string('a', 200) + "  "));
        Assert.False(IsletmeAdi.Gecerli(new string('a', 201)));
    }
}

/// <summary>
/// SERVİS YOLU — DataAnnotations yalnız controller yolunda koşar; servis içi elle
/// kontrol ayrı ölçülür (controller özniteliği bu testte YOKTUR).
/// </summary>
[Collection("veritabani")]
public sealed class Q103IsletmeKimlikServisTestleri(TestVeritabani vt)
{
    [Theory]
    [InlineData(" x ")]
    [InlineData("x")]
    public async Task Q103_D02_Servis_yolu_tek_karakterli_adi_reddeder_ad_DEGISMEZ(string girdi)
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "Q103-ad");
        using var ctx = vt.Olustur(k);
        var servis = new Kuyumcu.Api.Modules.Admin.AdminServisi(ctx, TestKurulum.Denetim(ctx));

        var ex = await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() =>
            servis.KimlikGuncelle(new Kuyumcu.Api.Modules.Admin.TenantKimlikGuncelleDto { Ad = girdi }, k.Islem));
        Assert.Equal(IsletmeAdi.Mesaj, ex.Message);

        using var oku = vt.OlusturSistem();
        Assert.Equal("Q103-ad", oku.Tenants.AsNoTracking().Single(t => t.Id == k.TenantId).Ad);
    }

    [Theory]
    [InlineData("12345abcde", VergiKimlik.RakamDisiMesaj)]
    [InlineData("123456789012", VergiKimlik.HaneMesaj)]
    [InlineData("١٢٣٤٥٦٧٨٩٠", VergiKimlik.RakamDisiMesaj)]
    public async Task Q103_D01_Servis_yolu_hatali_VKN_reddeder_satir_yazilmaz(string girdi, string mesaj)
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "Q103-servis");
        using var ctx = vt.Olustur(k);
        var servis = new Kuyumcu.Api.Modules.Admin.AdminServisi(ctx, TestKurulum.Denetim(ctx));

        var ex = await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() =>
            servis.KimlikGuncelle(new Kuyumcu.Api.Modules.Admin.TenantKimlikGuncelleDto { VergiNo = girdi }, k.Islem));
        Assert.Equal(mesaj, ex.Message);

        using var oku = vt.OlusturSistem();
        Assert.Null(oku.Tenants.AsNoTracking().Single(t => t.Id == k.TenantId).VergiNo);
    }
}

[Collection("http")]
public sealed class Q103IsletmeKimlikHttpTestleri(HttpFikstur f)
{
    private const string Yol = "/api/v1/admin/tenant/kimlik";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private async Task<Tenant> TenantOku(string tenantId)
    {
        using var db = f.Vt.OlusturSistem();
        return await db.Tenants.AsNoTracking().SingleAsync(t => t.Id == tenantId);
    }

    private Task<HttpResponseMessage> Kimlik(Kiraci k, object govde)
        => f.Ortam.Istemci(k, "SAHIP").PatchAsJsonAsync(Yol, govde);

    /// <summary>Hata gövdesindeki "message" dizge ya da dizi olabilir — ikisi de okunur.</summary>
    private static async Task<string> Mesaj(HttpResponseMessage c)
    {
        var g = await c.Json();
        var m = g.GetProperty("message");
        return m.ValueKind == JsonValueKind.Array
            ? string.Join(" | ", m.EnumerateArray().Select(x => x.GetString()))
            : m.GetString() ?? "";
    }

    [Theory]
    [InlineData("123456789", VergiKimlik.HaneMesaj)]
    [InlineData("123456789012", VergiKimlik.HaneMesaj)]
    [InlineData("12345abcde", VergiKimlik.RakamDisiMesaj)]
    [InlineData("12345 67890", VergiKimlik.RakamDisiMesaj)]
    [InlineData("١٢٣٤٥٦٧٨٩٠", VergiKimlik.RakamDisiMesaj)]
    public async Task Q103_H01_Hatali_VKN_400_Turkce_mesaj_ve_satir_DEGISMEZ(string girdi, string mesaj)
    {
        var k = YeniKiraci("Q103 vkn red");
        Assert.Equal(HttpStatusCode.OK, (await Kimlik(k, new { vergiNo = "1111111111" })).StatusCode);

        var c = await Kimlik(k, new { vergiNo = girdi });

        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Contains(mesaj, await Mesaj(c));
        Assert.Equal("1111111111", (await TenantOku(k.TenantId)).VergiNo);
    }

    /// <summary>Doğrulayıcı bulgusu (DÜŞÜK): canlıda PATCH {ad:' x '} → 200, ad='x' idi.</summary>
    [Fact]
    public async Task Q103_H06_Tek_karakterli_ad_kirpilinca_400_ve_ad_DEGISMEZ()
    {
        var k = YeniKiraci("Q103 ad sinir");

        var c = await Kimlik(k, new { ad = " x " });

        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Contains(IsletmeAdi.Mesaj, await Mesaj(c));
        Assert.Equal("Q103 ad sinir", (await TenantOku(k.TenantId)).Ad);

        var ok = await Kimlik(k, new { ad = " Q103 Yeni Ad " });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("Q103 Yeni Ad", (await TenantOku(k.TenantId)).Ad);
    }

    [Theory]
    [InlineData("1234567890", "1234567890")]
    [InlineData("12345678950", "12345678950")]
    [InlineData(" 1234567890 ", "1234567890")]
    public async Task Q103_H02_Gecerli_VKN_TCKN_200_ve_kirpilarak_yazilir(string girdi, string saklanan)
    {
        var k = YeniKiraci("Q103 vkn kabul");
        var c = await Kimlik(k, new { vergiNo = girdi });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        Assert.Equal(saklanan, (await TenantOku(k.TenantId)).VergiNo);
        Assert.Equal(saklanan, (await c.Json()).GetProperty("vergiNo").GetString());
    }

    [Fact]
    public async Task Q103_H03_Bos_VKN_siler_gonderilmeyen_alanlar_korunur()
    {
        var k = YeniKiraci("Q103 bos");
        await Kimlik(k, new { adres = "Çarşı 1", vergiDairesi = "Beyazıt", vergiNo = "1234567890", telefon = "02125550101" });

        var c = await Kimlik(k, new { vergiNo = "" });

        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var t = await TenantOku(k.TenantId);
        Assert.Null(t.VergiNo);
        Assert.Equal("Çarşı 1", t.Adres);
        Assert.Equal("Beyazıt", t.VergiDairesi);
        Assert.Equal("02125550101", t.Telefon);
    }

    [Fact]
    public async Task Q103_H04_Denetim_izi_ESKI_YENI_yazar_tam_deger_yazmaz()
    {
        var k = YeniKiraci("Q103 denetim");
        await Kimlik(k, new { vergiNo = "1234567890", vergiDairesi = "Beyazıt" });
        await Kimlik(k, new { vergiNo = "98765432150", vergiDairesi = "Kadıköy", adres = "Yeni adres" });

        using var db = f.Vt.OlusturSistem();
        var kayitlar = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikTuru == "TenantKimlik")
            .ToListAsync();
        Assert.Equal(2, kayitlar.Count);
        var son = kayitlar.Single(d => d.Detay!.Contains("Kadıköy"));

        Assert.Contains("vergi_no: ******7890 → *******2150", son.Detay!);
        Assert.Contains("vergi_dairesi: \"Beyazıt\" → \"Kadıköy\"", son.Detay!);
        Assert.Contains("adres: (boş) → \"Yeni adres\"", son.Detay!);
        Assert.DoesNotContain("1234567890", son.Detay!);
        Assert.DoesNotContain("98765432150", son.Detay!);
    }

    [Fact]
    public async Task Q103_H05_Cikti_belgesi_YENI_adres_ve_VKN_basar()
    {
        var k = YeniKiraci("Q103 cikti");
        await Kimlik(k, new { adres = "Kapalıçarşı Q103 No:7", vergiDairesi = "Beyazıt", vergiNo = "12345678950" });

        var c = await f.Ortam.Istemci(k, "SAHIP")
            .GetAsync($"/api/v1/cikti/{BelgeTurleri.CariEkstre}/{k.CariId}");

        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var d = (await c.Json()).GetProperty("duzenleyen");
        Assert.Equal("Kapalıçarşı Q103 No:7", d.GetProperty("adres").GetString());
        Assert.Equal("12345678950", d.GetProperty("vergiNo").GetString());
        Assert.Equal("Beyazıt", d.GetProperty("vergiDairesi").GetString());
    }
}

public sealed class Q103IsletmeKimlikEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public Q103IsletmeKimlikEkranTestleri()
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
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<KullaniciGorunumServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "QA", "Test", "qa@ornek.com", "SAHIP", "s1"));
    }

    private void Basit() =>
        Services.GetRequiredService<ArayuzModuServisi>().Uygula(ArayuzModuServisi.Basit, ArayuzModuServisi.Basit);

    private IRenderedComponent<Kuyumcu.Web.Pages.Isletme> Ac()
    {
        var c = Render<Kuyumcu.Web.Pages.Isletme>();
        c.WaitForElement("#i-kimlik-duzenle").Click();
        c.WaitForElement("#cekmece-isletme-kimlik");
        return c;
    }

    private bool PatchGitti() =>
        istekler.Any(i => i.Metot == "PATCH" && i.Yol.Contains("tenant/kimlik"));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Q103_E01_Her_modda_bes_alan_cizilir(bool basit)
    {
        if (basit) Basit();
        var c = Ac();
        foreach (var id in new[] { "ikimlik-ad", "ikimlik-adres", "ikimlik-telefon", "ikimlik-vergidairesi", "ikimlik-vergino" })
            Assert.NotNull(c.Find($"#{id}"));
        Assert.False(string.IsNullOrWhiteSpace(c.Find("#ikimlik-belge-notu").TextContent));
        // PAKET GOC-D (envanter F2 · GIRDI_TUR_GENEL_BILESEN): vergi no artık anlamsal TcknVeyaVkn — rakam dışı
        // tuş yazılmaz (maske), en çok 11 hane; hane dolunca TCKN-DOGRULAMA algoritması alan altında + kilit.
        // (Q10-3'ün "sessiz kırpma" kaygısı: kırpılan yapıştırma algoritmadan geçmeden Kaydet açılmaz — E03.)
        Assert.Equal("rakam", c.Find("#ikimlik-vergino").GetAttribute("data-maske"));
        Assert.Equal("11", c.Find("#ikimlik-vergino").GetAttribute("maxlength"));
    }

    [Fact]
    public void Q103_E02_BASIT_modda_gövde_dort_alani_TASIR()
    {
        Basit();
        var c = Ac();
        c.Find("#ikimlik-adres").Input("Kapalıçarşı No:9");
        c.Find("#ikimlik-telefon").Input("02125550101");
        c.Find("#ikimlik-vergidairesi").Input("Beyazıt");
        c.Find("#ikimlik-vergino").Input("12345678950");
        c.Find("#cekmece-isletme-kimlik-kaydet").Click();

        c.WaitForAssertion(() => Assert.True(PatchGitti()));
        var govde = JsonDocument.Parse(istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
        Assert.Equal("Kapalıçarşı No:9", govde.GetProperty("adres").GetString());
        Assert.Equal("02125550101", govde.GetProperty("telefon").GetString());
        Assert.Equal("Beyazıt", govde.GetProperty("vergiDairesi").GetString());
        Assert.Equal("12345678950", govde.GetProperty("vergiNo").GetString());
        Assert.Equal("QA-TEST Kuyumculuk", govde.GetProperty("ad").GetString());
    }

    [Theory]
    [InlineData("12345abcde", false)]
    [InlineData("12345 67890", false)]
    [InlineData("123456789", true)]
    [InlineData("123456789012", true)]
    public void Q103_E03_Hatali_VKN_alan_alti_mesaj_kilit_ve_istek_GITMEZ(string girdi, bool haneHatasi)
    {
        Basit();
        var c = Ac();
        c.Find("#ikimlik-vergino").Input(girdi);
        c.Find("#ikimlik-vergino").Change(girdi);

        // PAKET GOC-D: anlamsal TcknVeyaVkn — harf/boşluk YAZILMAZ (maske temizler), hane/algoritma hatası alan
        // altında GirdiSozlugu metniyle; kilit sebebi alan altı metniyle BİREBİR (UX-S30). haneHatasi parametresi
        // eski iki ayrı metni seçiyordu; artık metin bileşenin kuralından gelir.
        _ = haneHatasi;
        Assert.DoesNotMatch("[^0-9]", c.Find("#ikimlik-vergino").GetAttribute("value") ?? "");
        if (girdi == "12345 67890")
        {
            // Yapıştırmadaki boşluk temizlenir → 10 hane VKN biçimi; VKN kontrol hanesi TEYİTSİZ (envanter K4) → hata yok.
            Assert.Equal("1234567890", c.Find("#ikimlik-vergino").GetAttribute("value"));
            Assert.Empty(c.FindAll("#ikimlik-vergino-hata"));
            return;
        }
        var beklenen = c.Find("#ikimlik-vergino-hata").TextContent.Trim();
        Assert.False(string.IsNullOrEmpty(beklenen));
        Assert.True(c.Find("#cekmece-isletme-kimlik-kaydet").HasAttribute("disabled"));
        Assert.Contains(beklenen, c.Find("#cekmece-isletme-kimlik-kilit").TextContent);
        c.Find("#cekmece-isletme-kimlik-kaydet").Click();
        Assert.False(PatchGitti());
    }

    [Fact]
    public void Q103_E05_Tek_karakterli_ad_alan_alti_mesaj_kilit_ve_istek_GITMEZ()
    {
        Basit();
        var c = Ac();
        c.Find("#ikimlik-ad").Input(" x ");

        var beklenen = IsletmeKimlikSozlugu.Tr.AdUzunluk;
        Assert.Contains(beklenen, c.Find("#ikimlik-ad-hata").TextContent);
        Assert.True(c.Find("#cekmece-isletme-kimlik-kaydet").HasAttribute("disabled"));
        Assert.Contains(beklenen, c.Find("#cekmece-isletme-kimlik-kilit").TextContent);
        c.Find("#cekmece-isletme-kimlik-kaydet").Click();
        Assert.False(PatchGitti());
    }

    [Fact]
    public void Q103_E04_Bos_VKN_acik_null_gonderir_silme()
    {
        var c = Ac();
        c.Find("#ikimlik-vergino").Input("");
        c.Find("#cekmece-isletme-kimlik-kaydet").Click();
        c.WaitForAssertion(() => Assert.True(PatchGitti()));
        var govde = JsonDocument.Parse(istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
        Assert.Equal(JsonValueKind.Null, govde.GetProperty("vergiNo").ValueKind);
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, istek.RequestUri!.PathAndQuery, govde));
            var g = yol switch
            {
                var y when y.EndsWith("/admin/tenant/kimlik") || y.EndsWith("/admin/tenant") => TenantYaniti,
                var y when y.EndsWith("/subeler") =>
                    """[{"id":"s1","ad":"Merkez","adres":"Çarşı","telefon":"05321112233","aktif":true}]""",
                var y when y.EndsWith("/abonelik") =>
                    """{"planKodu":"BASLANGIC","planAdi":"Başlangıç","yazabilirMi":true,"planlar":[],"gecmis":[]}""",
                var y when y.EndsWith("/yetkiler/benim") => """{"moduller":["cari","urun"]}""",
                var y when y.EndsWith("/kullanici/gorunum", StringComparison.Ordinal) =>
                    istek.Method == HttpMethod.Patch ? B1SahteGorunumYaniti.Sarmal : B1SahteGorunumYaniti.Govde,
                var y when y.EndsWith("/gorunum", StringComparison.Ordinal) => B1SahteGorunumYaniti.Kiraci,
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }

        private const string TenantYaniti =
            """{"id":"t1","ad":"QA-TEST Kuyumculuk","plan":"BASLANGIC","aiEtkin":false,"varsayilanArayuzModu":"PROFESYONEL","adres":null,"telefon":null,"vergiDairesi":null,"vergiNo":"1234567890","kurKaynagi":"ALTINKAYNAK","maliyetYontemi":"FIFO"}""";
    }
}
