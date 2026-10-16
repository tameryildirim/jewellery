using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET QA10-KRITIK — /gider geri alma (storno) SONRASI başarı bandı [bUnit]
//
// Tasarım hakemi bulgusu (30.09, KRİTİK): BASİT modda geri alma sonrası bant
// "Gider stornolandı" diyordu; aynı tablodaki rozet "Geri alındı". BASİT modda
// bantta "storno" (ve virmanda "bacak") geçmez; PROFESYONEL modda muhasebe dili
// korunur.
//
// ⚠ Sahte API "{}" ile bırakılmadı: storno uçları gerçek sunucu biçimini döndürür
//   (virman: bacakSayisi=2), istekler kaydedilir ve tek POST sayılır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class QA10KritikTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol)> istekler = [];

    public QA10KritikTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string)> kayit) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var metot = istek.Method.Method;
            kayit.Add((metot, yol));
            var g = yol switch
            {
                var y when y.EndsWith("/storno") && y.Contains("/virman/") =>
                    """{"kaynakBacakIdleri":["a1","b1"],"stornoBacakIdleri":["t1","t2"],"bacakSayisi":2,"gerekce":"test"}""",
                var y when y.EndsWith("/storno") =>
                    """{"id":"kh2","kaynakId":"kh1","tutar":-3000,"paraBirimi":"TRY"}""",
                var y when y.EndsWith("/gider-kategorileri") =>
                    """{"kategoriler":[{"id":"gk1","ad":"Kira","aciklama":null,"aktif":true,"giderAdedi":1,"giderToplamTl":3000}],"toplam":1}""",
                var y when y.EndsWith("/giderler") => """
                    {"satirlar":[{"id":"kh1","kasaId":"ks1","kasaAdi":"Merkez Kasa","kategoriId":"gk1","kategoriAdi":"Kira","tutar":3000,"paraBirimi":"TRY","aciklama":"Ekim kirası","tarih":"2026-09-20T06:00:00Z","stornoSatiri":false,"stornolandi":false}],
                     "toplamAdet":1,"limit":100,"dahaFazlasiVar":false,"sinirNotu":"En çok 100 gider satırı gösterilir.",
                     "toplamlar":[{"paraBirimi":"TRY","tutar":3000,"adet":1}]}
                    """,
                var y when y.EndsWith("/virman") => """
                    {"satirlar":[{"cikisBacakId":"a1","girisBacakId":"b1","kaynakTur":"KASA","kaynakAdi":"Merkez Kasa","hedefTur":"BANKA","hedefAdi":"Ziraat","tutar":2000,"paraBirimi":"TRY","aciklama":null,"tarih":"2026-09-20T06:00:00Z","stornoSatiri":false}],
                     "toplam":1,"limit":50,"dahaFazlasiVar":false,"sinirNotu":"En son 50 virman gösterilir."}
                    """,
                var y when y.EndsWith("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":10000,"aktif":true,"subeId":null}]""",
                var y when y.EndsWith("/bankalar") =>
                    """{"hesaplar":[{"id":"bh1","bankaAdi":"Ziraat","hesapAdi":"Vadesiz","hesapSahibi":null,"iban":"TR11","paraBirimi":"TRY","bakiye":5000,"aktif":true}]}""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur1","hasAlis":4100,"hasSatis":4150}""",
                var y when y.Contains("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Gider> Ekran(bool basit)
    {
        var mod = basit ? ArayuzModuServisi.Basit : ArayuzModuServisi.Profesyonel;
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var modu = Services.GetRequiredService<ArayuzModuServisi>();
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => modu.BasitMi);
        var e = Render<Kuyumcu.Web.Pages.Gider>();
        GiderAkordeon.Ac(e); // SON-KALANLAR: geçmiş kayıtlar kapalı akordeon
        e.WaitForAssertion(() => Assert.Contains("Ekim kirası", e.Markup));
        return e;
    }

    private static void GerekceyleOnayla(IRenderedComponent<Kuyumcu.Web.Pages.Gider> e, string gerekce)
    {
        e.Find("#gd-storno-gerekce").Input(gerekce);
        e.Find("#gd-storno-onayla").Click();
    }

    private string Bant(IRenderedComponent<Kuyumcu.Web.Pages.Gider> e)
    {
        var metin = "";
        e.WaitForAssertion(() => { metin = e.Find("#gd-basari").TextContent; Assert.False(string.IsNullOrWhiteSpace(metin)); });
        return metin.Trim();
    }

    /// <summary>
    /// KRİTİK (hakem): BASİT modda gider geri alınınca bant "Gider geri alındı" der,
    /// "storno" kelimesi GEÇMEZ. MUTASYON HEDEFİ (MQA10-1): BASİT dalı kaldırılıp hub
    /// metni ("Gider stornolandı") basılırsa KIRMIZI.
    /// </summary>
    [Fact]
    public void QA10_BASIT_gider_geri_alininca_bantta_STORNO_gecmez_Gider_geri_alindi_yazar()
    {
        var e = Ekran(basit: true);
        e.Find("#gd-storno-kh1").Click();
        GerekceyleOnayla(e, "Yanlış kategoriye yazılmış gider düzeltiliyor.");

        var bant = Bant(e);
        Assert.Equal(QA10Sozlugu.Tr.GiderGeriAlindiBasit, bant);
        Assert.DoesNotContain("storno", bant, StringComparison.OrdinalIgnoreCase);
        Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/kasalar/ks1/hareket/kh1/storno"));
    }

    /// <summary>PROFESYONEL modda muhasebe dili korunur: hub metni "Gider stornolandı".</summary>
    [Fact]
    public void QA10_PRO_gider_geri_alininca_bant_muhasebe_dilini_KORUR()
    {
        var e = Ekran(basit: false);
        e.Find("#gd-storno-kh1").Click();
        GerekceyleOnayla(e, "Yanlış kategoriye yazılmış gider düzeltiliyor.");

        Assert.Equal(Services.GetRequiredService<DilServisi>().T.Gider.StornoYapildi, Bant(e));
    }

    /// <summary>
    /// BASİT modda virman geri alınınca bantta "storno" da "bacak" da GEÇMEZ.
    /// MUTASYON HEDEFİ (MQA10-2): BASİT dalı kaldırılırsa KIRMIZI.
    /// </summary>
    [Fact]
    public void QA10_BASIT_virman_geri_alininca_bantta_STORNO_ve_BACAK_gecmez()
    {
        var e = Ekran(basit: true);
        e.Find("#gd-sekme-virman").Click();
        GiderAkordeon.Ac(e); // SON-KALANLAR: geçmiş kayıtlar kapalı akordeon
        e.WaitForAssertion(() => e.Find("#gd-v-storno-a1"));
        e.Find("#gd-v-storno-a1").Click();
        GerekceyleOnayla(e, "Yanlış hesaba yapılan aktarım geri alınıyor.");

        var bant = Bant(e);
        // TOPLU-6 t2 (tasarım hakemi KRİTİK): BASİT bant "virman" da demez — ekranın BASİT adıyla aynı.
        Assert.Equal(Toplu6Sozlugu.Tr.VirmanStornoYapildiBasit, bant);
        Assert.DoesNotContain("storno", bant, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bacak", bant, StringComparison.OrdinalIgnoreCase);
        Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/virman/a1/storno"));
    }

    /// <summary>PROFESYONEL modda virman bandı sunucunun bacak sayısını sözlükten yazar ("2 bacak").</summary>
    [Fact]
    public void QA10_PRO_virman_bandi_bacak_sayisini_SOZLUKTEN_yazar()
    {
        var e = Ekran(basit: false);
        e.Find("#gd-sekme-virman").Click();
        GiderAkordeon.Ac(e); // SON-KALANLAR: geçmiş kayıtlar kapalı akordeon
        e.WaitForAssertion(() => e.Find("#gd-v-storno-a1"));
        e.Find("#gd-v-storno-a1").Click();
        GerekceyleOnayla(e, "Yanlış hesaba yapılan aktarım geri alınıyor.");

        var bant = Bant(e);
        Assert.Contains(Services.GetRequiredService<DilServisi>().T.Gider.VirmanStornoYapildi, bant);
        Assert.Contains(string.Format(QA10Sozlugu.Tr.VirmanBacakSayisi, 2), bant);
    }

    /// <summary>Sözlük: iki dilde de dolu; BASİT metinlerde "storno"/"bacak" yok, EN'de Türkçe yok.</summary>
    [Fact]
    public void QA10_sozluk_TR_EN_dolu_ve_BASIT_metinler_jargonsuz()
    {
        foreach (var m in new[] { QA10Sozlugu.Tr, QA10Sozlugu.En })
        {
            Assert.False(string.IsNullOrWhiteSpace(m.GiderGeriAlindiBasit));
            Assert.False(string.IsNullOrWhiteSpace(m.VirmanGeriAlindiBasit));
            Assert.Contains("{0}", m.VirmanBacakSayisi);
            foreach (var s in new[] { m.GiderGeriAlindiBasit, m.VirmanGeriAlindiBasit })
            {
                Assert.DoesNotContain("storno", s, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("bacak", s, StringComparison.OrdinalIgnoreCase);
            }
        }
        Assert.Same(QA10Sozlugu.En, QA10Sozlugu.Al("en"));
        Assert.Same(QA10Sozlugu.Tr, QA10Sozlugu.Al("tr"));
        Assert.DoesNotContain("ı", QA10Sozlugu.En.GiderGeriAlindiBasit);
        Assert.StartsWith(KatalogSozlugu.Tr.GeriAlindi.ToLowerInvariant().Split(' ')[0],
            QA10Sozlugu.Tr.GiderGeriAlindiBasit.Split(' ', 2)[1]);
    }
}
