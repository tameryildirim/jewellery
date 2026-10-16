using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-51 — SiparisDetay düzenleme formunda BAŞLIK KİMLİĞİ (cari · tür · şube).
//
// ⚠ NEDEN AYRI DOSYA: SiparisEkranTestleri'nin sahte API'si TEK şube döner ve o
//   kurulum UX-S22 (tek şubede seçici çizilmez) davranışını ölçer. Burada ÇOK
//   şubeli kiracı ölçülür; iki gerçek tek dosyada karışmasın.
// ⚠ Sahte API "{}" varsayılanına düşmez: her uca GERÇEKÇİ camelCase yanıt verilir.
// ⚠ Zamanlayıcı/yoklama YOK; ölçüm ilk yükleme + AÇIK kullanıcı eylemleriyle.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P51EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private int subeSayisi = 2;
    private bool patchReddet;

    private const string TaslakDetay = """
        {"id":"s1","no":"S-2026-000001","tur":"MUSTERI_SIPARISI","durum":"TASLAK","cariId":"c1",
         "cariAd":"Ayşe Yılmaz","subeId":"sb1","sozVerilenGun":"2026-09-10","aciklama":"bilezik",
         "tahminiTutarTl":null,"kurSnapshotId":null,"emanetHareketId":null,
         "geciken":false,"duzenlenebilir":true,"terminal":false,
         "izinliGecisler":["ONAYLANDI","IPTAL"],"gerekceIsteyenGecisler":["IPTAL"],"gerekceAsgari":10,
         "olusturanKullaniciId":"u1","olusturuldu":"2026-09-16T08:00:00Z","guncellendi":"2026-09-16T08:00:00Z",
         "kalemler":[{"id":"k1","urunId":"u-1","urunAd":"Bilezik","tanim":null,"ayarId":"K22",
                      "gramAgirlik":10.0,"adet":1.0,"sira":0}],
         "gecisler":[{"id":"g1","oncekiDurum":null,"yeniDurum":"TASLAK","gerekce":null,
                      "kullaniciId":"u1","kullaniciAd":"Test Kullanıcı","olusturuldu":"2026-09-16T08:00:00Z"}]}
        """;

    private const string RetMesaji =
        "Tür ATOLYE_SIPARISI olarak değiştirilemez: dış atölye siparişinde işi kimin "
        + "yaptığı (usta carisi) boş bırakılamaz. Aynı istekte ustaCariId gönderin.";

    public P51EkranTestleri()
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

    private sealed class SahteApi(P51EkranTestleri sahip) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            sahip.istekler.Add((istek.Method.Method, yol, govde));

            if (istek.Method == HttpMethod.Patch && sahip.patchReddet)
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                        $$"""{"message":"{{RetMesaji}}"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            var g = yol switch
            {
                var y when y.EndsWith("/subeler") => sahip.subeSayisi > 1
                    ? """[{"id":"sb1","ad":"Merkez"},{"id":"sb2","ad":"Çarşı"}]"""
                    : """[{"id":"sb1","ad":"Merkez"}]""",
                var y when y.EndsWith("/cari") =>
                    """[{"id":"c1","ad":"Ayşe Yılmaz","tur":"MUSTERI"},{"id":"c2","ad":"Mehmet Demir","tur":"MUSTERI"}]""",
                var y when y.Contains("/siparisler/") => TaslakDetay,
                var y when y.Contains("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":4200}""",
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

    private IRenderedComponent<Kuyumcu.Web.Pages.SiparisDetay> Detay()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));
        return Render<Kuyumcu.Web.Pages.SiparisDetay>(p => p.Add(x => x.Id, "s1"));
    }

    [Fact]
    public void Duzenleme_formunda_cari_tur_sube_secicileri_dolu_gelir()
    {
        var ekran = Detay();
        ekran.Find("#sipd-duzenle").Click();

        // Tür: kapalı listenin ÜÇÜ de seçenektir.
        Assert.Equal(3, SeciciYardim.Secenekler(ekran, "sipd-d-tur").Count);

        // Cari: boş liste YASAK — en az bir seçenek gelir ve kaydın carisi seçilidir.
        var cariSecenekleri = SeciciYardim.Secenekler(ekran, "sipd-d-cari");
        Assert.True(cariSecenekleri.Count >= 1);
        Assert.Contains(cariSecenekleri, o => o.Deger() == "c1");
        Assert.Equal("c1", SeciciYardim.SeciliDeger(ekran, "sipd-d-cari"));

        // Şube: çok şubeli kiracıda seçici ÇİZİLİR ve dolu gelir.
        Assert.Equal(2, SeciciYardim.Secenekler(ekran, "sipd-d-sube").Count);
    }

    [Fact]
    public void Secilen_yeni_cari_ve_tur_PATCH_govdesinde_gider()
    {
        var ekran = Detay();
        ekran.Find("#sipd-duzenle").Click();
        SeciciYardim.Sec(ekran, "sipd-d-cari", "c2");
        SeciciYardim.Sec(ekran, "sipd-d-sube", "sb2");
        ekran.Find("#sipd-d-kaydet").Click();

        var patch = istekler.Last(i => i.Metot == "PATCH");
        Assert.Contains("\"cariId\":\"c2\"", patch.Govde);
        Assert.Contains("\"subeId\":\"sb2\"", patch.Govde);
        Assert.Contains("\"tur\":\"MUSTERI_SIPARISI\"", patch.Govde);
    }

    [Fact]
    public void Tek_subede_secici_cizilmez_deger_salt_okunur_satirdir()
    {
        subeSayisi = 1;
        var ekran = Detay();
        ekran.Find("#sipd-duzenle").Click();

        Assert.Empty(ekran.FindAll("#sipd-d-sube"));
        Assert.Contains("Merkez", ekran.Find("#sipd-d-sube-sabit").TextContent);
    }

    [Fact]
    public void Sunucu_baslik_degisimini_reddederse_alan_alti_mesaj_cikar_form_kapanmaz()
    {
        patchReddet = true;
        var ekran = Detay();
        ekran.Find("#sipd-duzenle").Click();
        SeciciYardim.Sec(ekran, "sipd-d-tur", "ATOLYE_SIPARISI");
        // PAKET EKSIK-B1-YAZDIR: usta artık ekranda; boş usta istemcide durdurulur (DoD EK md.3).
        // Sunucu reddi senaryosu, usta SEÇİLİ gövdeyle sınanmaya devam eder.
        SeciciYardim.Sec(ekran, "sipd-d-usta", "c2");
        ekran.Find("#sipd-d-kaydet").Click();

        // UX-S28: sunucunun AÇIK gerekçesi okunur; "kayıt yok"a çevrilmez.
        Assert.Contains("usta carisi", ekran.Find("#sipd-d-baslik-hata").TextContent);
        Assert.Contains("usta carisi", ekran.Find("#sipd-hata-bandi").TextContent);
        // Form AÇIK kalır — kullanıcının girdiği değer kaybolmaz.
        Assert.NotEmpty(ekran.FindAll("#sipd-d-tur"));
    }

    [Fact]
    public void Cari_secilmeden_kaydedilirse_alan_alti_hata_cikar_ve_istek_atilmaz()
    {
        var ekran = Detay();
        ekran.Find("#sipd-duzenle").Click();
        SeciciYardim.Sec(ekran, "sipd-d-cari", null);
        ekran.Find("#sipd-d-kaydet").Click();

        Assert.NotEmpty(ekran.Find("#sipd-d-cari-hata").TextContent);
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
    }
}
