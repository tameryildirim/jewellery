using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;
using SayfaCariler = Kuyumcu.Web.Pages.Cariler;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET CUMA-M — MÜŞTERİLER: çekmece "İptal" → tuval "Vazgeç"; telefon
// "05324112233" → tuval "0532 411 22 33" (gösterim ve girdi biçimi).
//
// (1) SAF: TelefonBicimi.Goster / Girdi / Sunucuya — kabul listesi birebir.
// (2) bUnit: Vazgeç metni (düzenle çekmecesi + yeni müşteri penceresi), girdi
//     alandan çıkınca biçimlenir, yazarken DEĞER YENİDEN YAZILMAZ (imleç),
//     sunucuya giden değer ölçülen saklama biçimi ("05324112233"), dokunulmayan
//     telefon PATCH'te AYNEN.
// (3) HTTP: kaydedilen değer normalize biçimde saklanır; arama "0532 411" ve
//     "0532411" ikisini de bulur.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class CumaMTelefonBicimiTestleri
{
    [Theory]
    [InlineData("05324112233", "0532 411 22 33")]
    [InlineData("+905324112233", "0532 411 22 33")]
    [InlineData("02121234567", "0212 123 45 67")]
    [InlineData("0532 411 22 33", "0532 411 22 33")]
    [InlineData("+90 532 411 22 33", "0532 411 22 33")]
    [InlineData("5324112233", "0532 411 22 33")]
    [InlineData("08501234567", "0850 123 45 67")]
    [InlineData("1234567", "1234567")]               // 7 hane → AYNEN
    [InlineData("0532abc2233", "0532abc2233")]       // harf → AYNEN
    [InlineData("+44 20 7946 0958", "+44 20 7946 0958")]   // yabancı → AYNEN
    [InlineData("12125551234", "12125551234")]       // 11 hane, 0'sız → AYNEN
    [InlineData("01234567890", "01234567890")]       // 0 + 1xx → TR alan kodu değil → AYNEN
    public void CUMA_M_01_Goster_tuval_bicimi(string girdi, string beklenen)
        => Assert.Equal(beklenen, TelefonBicimi.Goster(girdi));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CUMA_M_02_Bos_ve_null(string? girdi)
    {
        Assert.Equal("—", TelefonBicimi.Goster(girdi));
        Assert.Equal("", TelefonBicimi.Girdi(girdi));
        Assert.Null(TelefonBicimi.Sunucuya(girdi));
    }

    [Theory]
    [InlineData("0532 411 22 33", "05324112233")]
    [InlineData("+905324112233", "05324112233")]
    [InlineData("(0212) 123-45-67", "02121234567")]
    [InlineData("05324112233", "05324112233")]
    [InlineData(" 1234567 ", "1234567")]             // tanınmayan AYNEN (kırpılmış) — sunucu doğrular
    [InlineData("+44 20 7946 0958", "+44 20 7946 0958")]
    public void CUMA_M_03_Sunucuya_olculen_saklama_bicimi(string girdi, string beklenen)
        => Assert.Equal(beklenen, TelefonBicimi.Sunucuya(girdi));

    [Theory]
    [InlineData("05324112233", "0532 411 22 33")]
    [InlineData("0532411", "0532411")]               // yarım numara bozulmaz
    [InlineData("  +90 532 4112233 ", "0532 411 22 33")]
    public void CUMA_M_04_Girdi_bicimi(string girdi, string beklenen)
        => Assert.Equal(beklenen, TelefonBicimi.Girdi(girdi));

    [Fact]
    public void CUMA_M_05_Sayfa_gosterimi_tek_kaynaktan()
        => Assert.Equal(TelefonBicimi.Goster("+905324112233"), SayfaCariler.TelefonGoster("+905324112233"));
}

public sealed class CumaMMusterilerEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string detayTelefon = "05324112233";

    public CumaMMusterilerEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => detayTelefon))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<string> telefon) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, istek.RequestUri!.PathAndQuery, govde));
            var g = yol switch
            {
                var y when y.EndsWith("/kur/guncel") => """{"id":"kur-1","usdTry":40,"eurTry":44,"gbpTry":null}""",
                var y when y.EndsWith("/raporlar/vade-takibi") => """{"ufukGun":30,"cariler":[]}""",
                var y when y.EndsWith("/denetim") => """{"toplam":0,"limit":20,"offset":0,"kayitlar":[]}""",
                var y when y.Contains("/cari/c") && !y.Contains("/ekstre") && !y.Contains("/bagli") => $$"""
                    {"id":"c1","ad":"Hasan Kaya","tur":"MUSTERI","telefon":{{JsonSerializer.Serialize(telefon())}},"email":null,
                     "vergiNo":null,"adres":null,"krediLimitiTl":0,"varsayilanVadeGun":0,
                     "notlar":null,"pepBeyani":false,"pepGorev":null,"pepBeyanTarihi":null}
                    """,
                var y when y.EndsWith("/cari") && istek.Method == HttpMethod.Post => """
                    {"id":"c9","ad":"Yeni","tur":"MUSTERI","telefon":"05324112233"}
                    """,
                var y when y.EndsWith("/cari") => $$"""
                    [{"id":"c1","ad":"Hasan Kaya","tur":"MUSTERI","telefon":{{JsonSerializer.Serialize(telefon())}},"email":null,
                      "tlBakiye":0,"hasGramBakiye":0,"aktif":true,"segment":null,"guncellendi":null}]
                    """,
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var y when y.Contains("/fiyat/listeler") => """[]""",
                var y when y.Contains("/bagli-kayitlar") => """{"cariId":"c1","aktif":true,"hareketSayisi":0,"tlBakiye":0,"hasGramBakiye":0,"dovizBakiyeVar":false}""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<SayfaCariler> Ciz(string mod = ArayuzModuServisi.Basit)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<SayfaCariler>();
        // TEMA TEMİZLİĞİ 17.10: liste = Yıldız tuvali (iki modda aynı yüz).
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#yildiz-musteri-liste > .yz-musteri-sat")));
        return e;
    }

    private IRenderedComponent<SayfaCariler> PaneliAc(string mod = ArayuzModuServisi.Basit)
    {
        var e = Ciz(mod);
        e.KartAc("c1");   // TEMA TEMİZLİĞİ 17.10: Yıldız'da iki modda da satıra dokununca kart açılır
        e.WaitForAssertion(() => Assert.False(e.Find("#duz-telefon").HasAttribute("disabled")));
        return e;
    }

    private JsonElement SonGovde(string metot) =>
        JsonDocument.Parse(istekler.Last(i => i.Metot == metot).Govde).RootElement;

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void CUMA_M_10_Cekmece_ikincil_dugme_Vazgec(string mod)
    {
        var e = PaneliAc(mod);
        Assert.Equal(OnayKilitSozlugu.Tr.Vazgec, e.Find("#cekmece-cari-iptal").TextContent.Trim());
        Assert.Equal("Vazgeç", e.Find("#cekmece-cari-iptal").TextContent.Trim());
        // Vazgeç ÇALIŞIR: çekmece kapanır, istek atılmaz.
        istekler.Clear();
        e.Find("#cekmece-cari-iptal").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#cekmece-cari")));
        Assert.DoesNotContain(istekler, i => i.Metot is "PATCH" or "POST");
    }

    [Fact]
    public void CUMA_M_11_Yeni_musteri_penceresi_Vazgec()
    {
        var e = Ciz();
        e.Find("#cari-yeni-ac").Click();
        e.WaitForElement("#cari-yeni-vazgec");
        Assert.Equal("Vazgeç", e.Find("#cari-yeni-vazgec").TextContent.Trim());
        e.Find("#cari-yeni-vazgec").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#cari-ad")));
    }

    [Fact]
    public void CUMA_M_12_Sayfada_Iptal_diyen_kapat_dugmesi_kalmadi()
    {
        var e = PaneliAc();
        Assert.DoesNotContain(e.FindAll("button"), b => b.TextContent.Trim() == "İptal");
    }

    [Fact]
    public void CUMA_M_13_Duzenle_acilinca_telefon_tuval_biciminde()
    {
        var e = PaneliAc();
        Assert.Equal("0532 411 22 33", e.Find("#duz-telefon").GetAttribute("value"));
        // TEMA TEMİZLİĞİ 17.10: listedeki telefon Yıldız satırının alt bilgisinde (aynı TelefonGoster biçimi).
        Assert.Equal("0532 411 22 33", e.Find("#yildiz-musteri-c1 .yz-musteri-alt").TextContent.Trim());
    }

    [Fact]
    public void CUMA_M_14_Yazarken_deger_yeniden_yazilmaz_cikinca_bicimlenir()
    {
        var e = PaneliAc();
        // GIRDI-AILESI-2 (kurucu 08.10 "yazarken maske"): yazarken de "0541 987 65 43" biçimi
        // (girdi-maske.js aynı maskeyi tarayıcıda uygular; imleç anlamlı hane sayısıyla korunur).
        e.Find("#duz-telefon").Input("05419876543");
        Assert.Equal("0541 987 65 43", e.Find("#duz-telefon").GetAttribute("value"));
        // Alandan çıkınca (change): tuval biçimi.
        e.Find("#duz-telefon").Change("05419876543");
        Assert.Equal("0541 987 65 43", e.Find("#duz-telefon").GetAttribute("value"));
        // +90 ile yapıştırma da aynı biçime gelir.
        e.Find("#duz-telefon").Change("+90 541 987 65 43");
        Assert.Equal("0541 987 65 43", e.Find("#duz-telefon").GetAttribute("value"));
        // Sabit hat ve yabancı numara bozulmaz.
        e.Find("#duz-telefon").Change("02121234567");
        Assert.Equal("0212 123 45 67", e.Find("#duz-telefon").GetAttribute("value"));
        // GIRDI-AILESI-2: yabancı numarada '+' yalnız başta, rakam dışı temizlenir (envanter K2).
        e.Find("#duz-telefon").Change("+44 20 7946 0958");
        Assert.Equal("+442079460958", e.Find("#duz-telefon").GetAttribute("value"));
    }

    [Fact]
    public void CUMA_M_15_Degisen_telefon_sunucuya_normalize_gider()
    {
        var e = PaneliAc();
        e.Find("#duz-telefon").Change("0541 987 65 43");
        istekler.Clear();
        e.Find("#cekmece-cari-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH"));
        Assert.Equal("05419876543", SonGovde("PATCH").GetProperty("telefon").GetString());
    }

    [Fact]
    public void CUMA_M_16_Dokunulmayan_telefon_PATCH_te_AYNEN()
    {
        // Saklı biçim boşluklu (ölçüldü: 52 kaydın 1'i) — ilgisiz düzeltme onu değiştirmez.
        detayTelefon = "0532 411 22 33";
        var e = PaneliAc();
        istekler.Clear();
        e.Find("#cekmece-cari-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH"));
        Assert.Equal("0532 411 22 33", SonGovde("PATCH").GetProperty("telefon").GetString());
    }

    [Fact]
    public void CUMA_M_17_Telefon_silinirse_null_gider()
    {
        var e = PaneliAc();
        e.Find("#duz-telefon").Input("");
        e.Find("#duz-telefon").Change("");
        istekler.Clear();
        e.Find("#cekmece-cari-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH"));
        Assert.Equal(JsonValueKind.Null, SonGovde("PATCH").GetProperty("telefon").ValueKind);
    }

    [Fact]
    public void CUMA_M_18_Yeni_musteri_telefonu_normalize_gider()
    {
        var e = Ciz();
        e.Find("#cari-yeni-ac").Click();
        e.WaitForElement("#cari-ad");
        e.Find("#cari-ad").Input("Yeni Müşteri");
        e.Find("#cari-telefon").Input("+90 532 411 22 33");
        e.Find("#cari-telefon").Change("+90 532 411 22 33");
        Assert.Equal("0532 411 22 33", e.Find("#cari-telefon").GetAttribute("value"));
        istekler.Clear();
        e.Find("#cari-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/cari")));
        Assert.Equal("05324112233", SonGovde("POST").GetProperty("telefon").GetString());
    }

    // TEMA TEMİZLİĞİ 17.10: CUMA_M_19_PRO_satir_ici_telefon_normalize_gider kaldırıldı — eski PRO tablosunun satır içi telefon düzenlemesi (#si-cari-telefon-*) Yıldız listesinde yok; telefon kartta düzenlenir ve normalize gider (CUMA_M_15, PRO kartı CUMA_M_10).
}

[Collection("http")]
public sealed class CumaMHttpTelefonTestleri(HttpFikstur f)
{
    [Fact]
    public async Task CUMA_M_20_Kaydedilen_deger_normalize_ve_arama_iki_bicimde_bulur()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "CUMA-M telefon");
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        // Ekranın göndereceği değer (TelefonBicimi.Sunucuya) — "0532 411 22 33" yazıldı.
        var giden = TelefonBicimi.Sunucuya("0532 411 22 33");
        Assert.Equal("05324112233", giden);
        var cevap = await istemci.PostJson("/api/v1/cari", new { ad = "CUMA-M Telefon", tur = "MUSTERI", telefon = giden });
        Assert.Equal(HttpStatusCode.Created, cevap.StatusCode);
        var id = (await cevap.Json()).GetProperty("id").GetString();
        Assert.False(string.IsNullOrEmpty(id));

        // Sunucu AYNEN saklar (mevcut sözleşme) → okunan değer normalize biçim.
        var detay = await (await istemci.GetAsync($"/api/v1/cari/{id}")).Json();
        Assert.Equal("05324112233", detay.GetProperty("telefon").GetString());

        foreach (var q in new[] { "0532 411", "0532411" })
        {
            var liste = await (await istemci.GetAsync($"/api/v1/cari?q={Uri.EscapeDataString(q)}")).Json();
            Assert.Contains(liste.EnumerateArray(), c => c.GetProperty("id").GetString() == id);
        }
    }
}
