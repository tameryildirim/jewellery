using System.Reflection;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ROTA BAŞLIKLARI — paket UX-2 Faz A1 (Bertan KOŞUL 3 · Aşkın K5)
//
// Bugüne kadar UstBar'da 12 rota tanınıyor, kalan 15'i `_ => "Kuyumcu"` geri
// dalına düşüyordu. Geri dal KALDIRILDI ve haritaya çevrildi.
//
// ⚠ Rota listesi HARİTADAN DEĞİL, Kuyumcu.Web assembly'sindeki [Route]
//   özniteliklerinden YANSIMAYLA gelir. Liste haritanın kendisinden alınsaydı
//   "/emanet satırını sil" mutasyonu testi YEŞİL bırakırdı.
//
// ⚠ Bu, projedeki İLK yerleşim (UstBar) testidir. UstBar OnInitializedAsync'te
//   CanliKurServisi.BaslatAsync'i BEKLER; kurulum sahte HttpClient ile
//   /kur/guncel yanıtı verir. SignalR bağlanması ateşle-unut olduğu için
//   (CanliKurServisi.cs:64) test askıda kalmaz.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class RotaBaslikTestleri : BunitContext
{
    public RotaBaslikTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi())
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();   // UX-3: UstBar mod anahtarı
        // PAKET P-15: UstBar artık GlobalArama + SubeSecici barındırıyor;
        // GlobalArama komut paletini menüyle AYNI yetki kaynağından süzer.
        Services.AddScoped<YetkiServisi>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = yol switch
            {
                var v when v.Contains("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":4200,"usdTry":34.5,"eurTry":37.1,"gbpTry":44.2}""",
                var v when v.Contains("/auth/ben") =>
                    """{"id":"k1","ad":"Test","soyad":"Kullanici","email":"t@t.local","rol":"SAHIP","subeId":null}""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    /// <summary>Kuyumcu.Web'deki tüm @page rotaları — yansımayla, haritadan DEĞİL.</summary>
    public static IEnumerable<object[]> GercekRotalar() =>
        YansimayaGoreRotalar().Select(r => new object[] { r });

    internal static List<string> YansimayaGoreRotalar() =>
        typeof(UstBar).Assembly.GetTypes()
            .Where(t => typeof(IComponent).IsAssignableFrom(t))
            .SelectMany(t => t.GetCustomAttributes<RouteAttribute>())
            .Select(a => RotaBasliklari.Duzgunlestir(a.Template))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

    [Fact]
    public void Yansima_TAM_42_rota_bulur()
    {
        // Sayı sabiti kasıtlı: yeni bir sayfa eklendiğinde bu test kırmızıya
        // döner ve uygulayıcıyı haritaya satır eklemeye zorlar (UX-S13).
        // 27 → 28: paket 6 Faz A /bildirimler sayfasını ekledi.
        // 28 → 30: paket 3 Faz A /siparisler ve /siparisler/{Id} sayfalarını ekledi.
        // 30 → 31: paket 19 Faz A /vezne sayfasını ekledi.
        // 31 → 34: paket 34 Faz A rapor seti ekranlarını ekledi
        // (/raporlar/islem-defteri · /raporlar/satis-analizi · /raporlar/kasa).
        // 34 → 35: paket 47 Faz A /veri-ice-aktarim sayfasını ekledi.
        // 35 → 36: paket T-2 Faz A /kurulum (kuruluş sihirbazı) sayfasını ekledi.
        // 36 → 37: paket T-3 Faz A /yedekler (yedek otomasyonu) sayfasını ekledi.
        // 37 → 39: paket 29 Faz A /evraklar ve /evraklar/takvim sayfalarını ekledi.
        // 39 → 40: paket 32 Faz A /banka/ekstre/{IthalatId} ekranını ekledi
        // (menüde YOK; /banka'dan ithalat kimliğiyle açılır).
        // 40 → 42: paket 31 /pos ve /pos/tahsilatlar (POS alacak defteri).
        var rotalar = YansimayaGoreRotalar();
        // 42 → 44: PAKET P-17 /vitrin (Y-7) ve /cikti/{Tur}/{Id} (Y-6).
        // 44 → 45: PAKET P-19 /gider (Y-11 virman + Y-12 gider).
        // 45 → 46: PAKET P-20 /hurda (Y-10 hurda stoğu + ramat çıkışı).
        // 46 → 47: PAKET S1-d /ekranlar (Tüm ekranlar — karo menü).
        // 47 → 48: PAKET YASAL-1 /yasal-sinirlar (salt okunur yasal sınırlar).
        // 48 → 49: PAKET AUTH-SIFIRLA /sifre-sifirla ("Parolamı unuttum", kimlik öncesi).
        // 49 → 51: PAKET EKSIK-B1-SATINALMA /satin-alma + /irsaliyeler (B1-1 · B1-2).
        // 51 → 52: PAKET EKSIK-KUCUK /vitrin/ekran (B2-10 kabuksuz dükkân ekranı).
        // 52 → 53: D1-19 /kvkk.
        Assert.Equal(53, rotalar.Count);
    }

    [Theory]
    [MemberData(nameof(GercekRotalar))]
    public void Tum_rotalar_baslik_tasir(string rota)
    {
        var secici = RotaBasliklari.Bul(rota);

        Assert.True(secici is not null,
            $"'{rota}' rotası RotaBasliklari.Harita'da YOK — üst barda başlıksız kalır.");

        foreach (var dil in new[] { "tr", "en" })
        {
            var baslik = secici!(Sozlukler.Getir(dil));
            Assert.False(string.IsNullOrWhiteSpace(baslik), $"'{rota}' ({dil}) başlığı boş.");
            Assert.NotEqual("Kuyumcu", baslik);
            Assert.NotEqual(Sozlukler.Getir(dil).Nav.Bulunamadi, baslik);
        }
    }

    [Fact]
    public void Haritada_GERCEK_olmayan_anahtar_YOKTUR()
    {
        // İki yönlü kontrol (Bertan KOŞUL 3): yazım hatası yakalanır.
        var gercek = YansimayaGoreRotalar().ToHashSet(StringComparer.Ordinal);
        var hayalet = RotaBasliklari.Harita.Keys
            .Select(RotaBasliklari.Duzgunlestir)
            .Where(k => !gercek.Contains(k))
            .ToList();

        Assert.True(hayalet.Count == 0,
            "Haritada gerçek bir @page rotası olmayan anahtar(lar): " + string.Join(", ", hayalet));
    }

    [Fact]
    public void Parametreli_rota_SABLONLA_eslesir()
    {
        // Eski kod ham AbsolutePath karşılaştırıyordu; bu rota HİÇ eşleşmiyordu.
        var baslik = RotaBasliklari.Baslik("/gider-pusulalari/01JABCDEF/belge", Sozlukler.Tr);
        Assert.Equal(Sozlukler.Tr.Nav.PusulaBelgesi, baslik);
        Assert.NotEqual("Kuyumcu", baslik);
    }

    [Fact]
    public void Ust_bar_GERCEK_rotada_gercek_basligi_cizer()
    {
        var nav = Services.GetRequiredService<NavigationManager>();

        foreach (var rota in YansimayaGoreRotalar())
        {
            var somut = rota.Replace("{Id}", "01JABCDEF", StringComparison.Ordinal);
            nav.NavigateTo(somut);

            var e = Render<UstBar>();
            // PAKET Q9-4: belge rotalarında (/cikti/…, /gider-pusulalari/{Id}/belge)
            // sayfanın TEK <h1>'i belgenin kendisidir; üst bar rota adını aynı
            // görünümde <p id="ust-bar-rota-adi"> ile yazar. Başlık yine dolu olmalı.
            var h1 = e.Find("header.ust-bar h1, header.ust-bar #ust-bar-rota-adi").TextContent.Trim();

            Assert.False(string.IsNullOrWhiteSpace(h1), $"{somut}: başlık boş");
            Assert.NotEqual("Kuyumcu", h1);
            Assert.NotEqual(Sozlukler.Tr.Nav.Bulunamadi, h1);
        }
    }

    [Fact]
    public async Task Dil_TR_den_EN_e_degisince_baslik_da_degisir()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        var dil = Services.GetRequiredService<DilServisi>();
        nav.NavigateTo("/denetim-izi");

        var e = Render<UstBar>();
        // S1-g YENİDEN YAZIM: başlık artık MENÜ ADIDIR (tuval Ekranlar.dc.html
        // "Kim ne yaptı"); ESKİ beklenen hub adı Nav.DenetimIzi ("Denetim İzi").
        Assert.Equal(MenuAdlari.Tr.KimNeYapti.Ad, e.Find("header.ust-bar h1").TextContent.Trim());

        // UX-4c (a/c): bloklayan .GetAwaiter().GetResult() xUnit1031 uyarısı
        // üretiyordu (kilitlenme riski). Test async, çağrı await edildi.
        await e.InvokeAsync(() => dil.DegistirAsync("en"));
        e.WaitForAssertion(() =>
            Assert.Equal(MenuAdlari.En.KimNeYapti.Ad, e.Find("header.ust-bar h1").TextContent.Trim()));
    }

    [Fact]
    public void Bilinmeyen_adres_KUYUMCU_degil_SAYFA_BULUNAMADI_der()
    {
        // `_ => "Kuyumcu"` geri dalı geri getirilirse bu test de kırmızı olur:
        // eksik rota "Kuyumcu" altında sessizce saklanamaz.
        Assert.Null(RotaBasliklari.Bul("/boyle-bir-sayfa-yok"));
        Assert.Equal(Sozlukler.Tr.Nav.Bulunamadi, RotaBasliklari.Baslik("/boyle-bir-sayfa-yok", Sozlukler.Tr));
    }
}
