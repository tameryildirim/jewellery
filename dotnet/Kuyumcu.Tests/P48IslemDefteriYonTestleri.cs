using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-48 — İŞLEM DEFTERİ YÖN HÜCRESİ [bUnit]
//
// Sunucu yönü İKİ BACAKTAN türetir (IslemYonu.Turet): GIRIS / CIKIS / KARISIK /
// null. Ekran bu kodu HAM basmaz (UX-S31) — sözlükten çevirir; KARISIK iken
// iki bacağı ayrı satırda okutur; null iken "—" basar, "Giriş" TAHMİN ETMEZ.
//
// Mutasyon M48-2 (hücrede @s.Yon ham basılır) → P48_04 KIRMIZI.
// ⚠ Sahte API '{}' varsayılanı BIRAKILMAZ: defter ucuna gerçekçi yanıt verilir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P48IslemDefteriYonTestleri : BunitContext
{
    private const string Defter = """
        {"baslangic":"2026-09-23","bitis":"2026-09-23","toplamSatir":4,"ofset":0,"limit":100,
         "dahaFazlasiVar":false,
         "ozet":{"tlGiris":5000.00,"tlCikis":3250.40,"hasGiris":0,"hasCikis":6.996,"satirSayisi":4},
         "storno":{"adet":0,"tutarTl":0},
         "satirlar":[
           {"id":"h1","tarih":"2026-09-23T09:00:00Z","tur":"SATIS","yon":"KARISIK","yonTl":"GIRIS","yonHas":"CIKIS",
            "tlTutari":5000.00,"hasGrami":-1.5,"maden":"ALTIN","paraBirimi":"TRY","odemeTuru":"NAKIT",
            "nakitTutar":5000.00,"veresiyeTutar":0,"fisId":null,"faturaNo":null,"cariAd":"Ayşe Demir",
            "urunAd":"Bilezik","subeAd":"Merkez","kullaniciAd":"Test","stornoSatiri":false,"stornolandi":false},
           {"id":"h2","tarih":"2026-09-23T09:30:00Z","tur":"EMANET_GIRIS","yon":"CIKIS","yonTl":null,"yonHas":"CIKIS",
            "tlTutari":0,"hasGrami":-5.496,"maden":"ALTIN","paraBirimi":"TRY","odemeTuru":null,
            "nakitTutar":null,"veresiyeTutar":null,"fisId":null,"faturaNo":null,"cariAd":null,
            "urunAd":null,"subeAd":"Merkez","kullaniciAd":"Test","stornoSatiri":true,"stornolandi":false},
           {"id":"h3","tarih":"2026-09-23T10:00:00Z","tur":"EVRAK_GIRIS","yon":"CIKIS","yonTl":"CIKIS","yonHas":null,
            "tlTutari":-3250.40,"hasGrami":0,"maden":null,"paraBirimi":"TRY","odemeTuru":"HAVALE",
            "nakitTutar":null,"veresiyeTutar":null,"fisId":null,"faturaNo":null,"cariAd":null,
            "urunAd":null,"subeAd":"Merkez","kullaniciAd":"Test","stornoSatiri":false,"stornolandi":false},
           {"id":"h4","tarih":"2026-09-23T11:00:00Z","tur":"ISCILIK","yon":null,"yonTl":null,"yonHas":null,
            "tlTutari":0,"hasGrami":0,"maden":null,"paraBirimi":"TRY","odemeTuru":null,
            "nakitTutar":null,"veresiyeTutar":null,"fisId":null,"faturaNo":null,"cariAd":null,
            "urunAd":null,"subeAd":"Merkez","kullaniciAd":"Test","stornoSatiri":false,"stornolandi":false}]}
        """;

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/islem-defteri", StringComparison.Ordinal) => Defter,
                var y when y.EndsWith("/subeler", StringComparison.Ordinal) =>
                    """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/kullanicilar", StringComparison.Ordinal) =>
                    """[{"id":"u1","ad":"Test","soyad":"Kullanici"}]""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    public P48IslemDefteriYonTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi())
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
    }

    [Fact]
    public void P48_04_Ekran_ham_yon_kodunu_basmaz()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var s = Kuyumcu.Core.I18n.Sozlukler.Tr.RaporSeti;

        var e = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        e.WaitForElement("#id-yon-h4");

        string Hucre(string id) => e.Find($"#{id}").TextContent;

        // Hiçbir yön hücresinde ham kod yok.
        foreach (var id in new[] { "id-yon-h1", "id-yon-h2", "id-yon-h3", "id-yon-h4" })
        {
            var metin = Hucre(id);
            Assert.DoesNotContain("GIRIS", metin, StringComparison.Ordinal);
            Assert.DoesNotContain("CIKIS", metin, StringComparison.Ordinal);
            Assert.DoesNotContain("KARISIK", metin, StringComparison.Ordinal);
        }

        // KARISIK → "Karışık" + iki bacak AYRI okunur.
        Assert.Contains(s.YonKarisik, Hucre("id-yon-h1"), StringComparison.Ordinal);
        Assert.Equal($"{s.YonTlBacagi}: {s.Giris}", Hucre("id-yon-tl-h1").Trim());
        Assert.Equal($"{s.YonHasBacagi}: {s.Cikis}", Hucre("id-yon-has-h1").Trim());

        // TL=0, has<0 → Çıkış; EVRAK_GIRIS (TL<0) → Çıkış; bacak satırı basılmaz.
        Assert.Equal(s.Cikis, Hucre("id-yon-h2").Trim());
        Assert.Equal(s.Cikis, Hucre("id-yon-h3").Trim());
        Assert.Empty(e.FindAll("#id-yon-tl-h2"));

        // İki bacak sıfır → "—", "Giriş" YAZMAZ; sebep title'da.
        var bos = e.Find("#id-yon-h4");
        Assert.Equal(s.YonYok, bos.TextContent.Trim());
        Assert.DoesNotContain(s.Giris, bos.TextContent, StringComparison.Ordinal);
        Assert.Equal(s.YonYokIpucu, bos.GetAttribute("title"));
    }
}
