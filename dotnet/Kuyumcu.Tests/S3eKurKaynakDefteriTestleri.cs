using Kuyumcu.Api.Modules.Kur;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S-3e — SEÇİCİNİN BESLENDİĞİ DEFTER.
//
// KURUCU: "Altınkaynak, Ankara Altın Borsası gibi daha önce var olanlar
// görünmüyor." Ekran seçeneklerini KurKaynakDefteri.Kodlar'dan alır; bu test
// o listenin İSİMLERİNİ dondurur — bir adaptör sessizce düşerse kırmızı olur.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S3eKurKaynakDefteriTestleri
{
    /// <summary>Ağa çıkmayan fabrika: defter kurulurken HTTP çağrısı OLMAMALIDIR.</summary>
    private sealed class AgsizFabrika : System.Net.Http.IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new Engel());

        private sealed class Engel : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage istek, CancellationToken iptal) =>
                throw new InvalidOperationException("Bu testte ağ çağrısı beklenmez: " + istek.RequestUri);
        }
    }

    private static KurKaynakDefteri Defter(bool acik) =>
        new(new AgsizFabrika(), NullLogger<KurKaynakDefteri>.Instance,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kur:SozlesmesizKaynaklarAcik"] = acik ? "true" : "false",
            }).Build());

    [Fact]
    public void Kaynak_listesi_ALTINKAYNAK_ANKARA_TRUNCGIL_icerir()
    {
        var kodlar = Defter(acik: true).Kodlar;

        Assert.Contains("ALTINKAYNAK", kodlar);
        Assert.Contains("ANKARA", kodlar);   // Ankara Altın Borsası
        Assert.Contains("TRUNCGIL", kodlar);
        Assert.Equal(3, kodlar.Count);
    }

    [Fact]
    public void Bayrak_kapaliyken_liste_BOSTUR_ve_ekran_bunu_soyler()
    {
        // Ekran boş listede seçiciyi KİLİTLER ve sebebini yazar (UX-S30);
        // sunucu tarafında da hiç adaptör kurulmaz (ikinci okuma yolu yok).
        Assert.Empty(Defter(acik: false).Kodlar);
    }

    [Fact]
    public void MANUAL_bir_KAYNAK_SECENEGI_DEGILDIR()
    {
        var defter = Defter(acik: true);
        Assert.DoesNotContain(KurKaynakDefteri.ManuelKod, defter.Kodlar);
        Assert.False(defter.Gecerli(KurKaynakDefteri.ManuelKod));
    }
}
