using System.Net;
using Bunit;
using Xunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-GERI (tur 3, doğrulayıcı ORTA bulgusu) — KAPORA ekranı sınıf kapanışı.
// Sunucunun MASAK reddi (günlük toplam) kapora çekmecesinde kimlik formunu eşik ALTINDA da açar.
// Önceden bu düz bir bool idi (kaporaMasakZorla): tutar düşürülünce form açık, Kaydet kilitli
// kalıyor ve beyan eşik altında gövdeye gidiyordu. Artık reddin İZİ (YasalKapiIzi): tutar reddin
// altına inince form kapanır, kilit kalkar, gövdede masak YOK; tutar geri çıkınca form ve girilen
// beyan geri gelir. Sahte API "{}" varsayılanı YOK: tanımsız uç 404. İki mod (BASİT + PRO).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class YasalKapiGeriKaporaTestleri : BunitContext
{
    private readonly List<string> kaporaGovdeleri = [];
    private readonly List<string> yollar = [];

    private const string DetayJson =
        "{\"id\":\"s1\",\"no\":\"S-2026-000001\",\"tur\":\"MUSTERI_SIPARISI\",\"durum\":\"HAZIR\""
        + ",\"cariId\":\"c1\",\"cariAd\":\"Ayşe Yılmaz\",\"subeId\":\"sb1\""
        + ",\"sozVerilenGun\":null,\"aciklama\":\"bilezik\",\"tahminiTutarTl\":null,\"kurSnapshotId\":null,"
        + "\"emanetHareketId\":null,\"geciken\":false,\"duzenlenebilir\":false,\"terminal\":false"
        + ",\"izinliGecisler\":[],\"geriYonluGecisler\":[],\"geriAlmaYetkisi\":true,\"geriAlmaRolleri\":[\"SAHIP\",\"MUDUR\"]"
        + ",\"gerekceIsteyenGecisler\":[\"IPTAL\"],\"gerekceAsgari\":10,\"olusturanKullaniciId\":\"u1\","
        + "\"olusturuldu\":\"2026-09-16T08:00:00Z\",\"guncellendi\":\"2026-09-16T08:00:00Z\","
        + "\"kalemler\":[{\"id\":\"k1\",\"urunId\":null,\"urunAd\":null,\"tanim\":\"yüzük\",\"ayarId\":\"K22\",\"gramAgirlik\":3.5,\"adet\":1.0,\"sira\":0}],"
        + "\"gecisler\":[],\"ustaCariId\":null,\"ustaAd\":null"
        + ",\"kaporaHareketId\":null,\"kaporaAlinabilir\":true,\"kaporaKilitSebebi\":null,\"kaporaTutari\":null"
        + ",\"kaporaTarihi\":null,\"kaporaKasaId\":null,\"kaporaStornolu\":false,\"kaporaGecmisi\":[]}";

    private sealed class SahteApi(YasalKapiGeriKaporaTestleri s) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            lock (s.yollar) s.yollar.Add(yol);
            if (istek.Method == HttpMethod.Post && yol.EndsWith("/siparisler/s1/kapora"))
            {
                lock (s.yollar) s.kaporaGovdeleri.Add(istek.Content!.ReadAsStringAsync().Result);
                // Günlük toplam yalnız sunucuda bilinir: eşik altı tutarda da MASAK reddi gelebilir.
                return Task.FromResult(EksikB1Ortak.Yanit(HttpStatusCode.BadRequest,
                    """{"message":"MASAK: 370.000 TL ve üstü işlemde kimlik tespiti zorunludur"}"""));
            }
            string? g = yol switch
            {
                var y when istek.Method == HttpMethod.Get && y.EndsWith("/siparisler/s1") => DetayJson,
                var y when y.EndsWith("/subeler") => """[{"id":"sb1","ad":"Merkez"}]""",
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz","tur":"MUSTERI"}]""",
                var y when y.EndsWith("/urunler") => """[{"id":"u0","kod":"K0","ad":"Ürün 0","ayarId":"K22","gramAgirlik":1.0}]""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":4200}""",
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var y when y.Contains("/masak/esikler") =>
                    """{"kimlikEsigi":185000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000,"ekBelgeEsigi":20000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var y when y.Contains("/masak/gereksinim") =>
                    """{"kademe":"AMAC_BEYANI","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                _ => null,
            };
            return Task.FromResult(g is null
                ? EksikB1Ortak.Yanit(HttpStatusCode.NotFound, """{"message":"sahte API bu ucu tanımıyor"}""")
                : EksikB1Ortak.Yanit(HttpStatusCode.OK, g));
        }
    }

    public YasalKapiGeriKaporaTestleri() => EksikB1Ortak.Kur(this, new SahteApi(this));

    private static void Tutar(IRenderedComponent<Kuyumcu.Web.Pages.SiparisDetay> e, string v)
    {
        e.Find("#sipd-kapora-tutar-g").Input(v);
        e.Find("#sipd-kapora-tutar-g").Blur();
    }

    // Kurucu senaryosunun kapora karşılığı: ret (yukarı) → tutar düşür (aşağı) → form kapanır, kilit kalkar,
    // gövdede masak yok → tutarı geri çıkar (yukarı) → form ve girilen beyan geri gelir.
    [Fact]
    public void YKGK01_kapora_masak_reddi_sonrasi_tutar_dusunce_form_kapanir_govdede_masak_yok_geri_cikinca_doner()
    {
        foreach (var mod in EksikB1Ortak.Modlar)
        {
            kaporaGovdeleri.Clear();
            EksikB1Ortak.Oturum(this, mod);
            var e = Render<Kuyumcu.Web.Pages.SiparisDetay>(p => p.Add(x => x.Id, "s1"));
            e.WaitForElement("#sipd-kart");
            e.Find("#sipd-kapora-al").Click();

            // YUKARI: eşik altı 500 ₺ — sunucu (günlük toplam) MASAK'la reddeder → form açılır, Kaydet sebepli kilitli.
            Tutar(e, "500");
            e.WaitForAssertion(() => Assert.False(e.Find("#sipd-kapora-kaydet").HasAttribute("disabled")));
            e.Find("#sipd-kapora-kaydet").Click();
            e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sipd-kapora-masak")));
            Assert.True(e.Find("#sipd-kapora-kaydet").HasAttribute("disabled"));
            Assert.Contains("\"masak\":null", kaporaGovdeleri[0]);

            // AŞAĞI: reddin ölçüsünün altına (300 ₺) → iz geçersiz: form kapanır, kilit kalkar.
            Tutar(e, "300");
            e.WaitForAssertion(() => Assert.Empty(e.FindAll("#sipd-kapora-masak")), TimeSpan.FromSeconds(3));
            Assert.False(e.Find("#sipd-kapora-kaydet").HasAttribute("disabled"));
            Assert.Empty(e.FindAll("#sipd-kapora-form-kilit"));

            // Eşik altında gönderilen gövde beyan TAŞIMAZ.
            e.Find("#sipd-kapora-kaydet").Click();
            e.WaitForAssertion(() => Assert.Equal(2, kaporaGovdeleri.Count));
            Assert.Contains("\"masak\":null", kaporaGovdeleri[1]);
            Assert.Contains("300", kaporaGovdeleri[1]);
            e.Dispose();
        }
    }

    // Ret → aşağı → YUKARI (reddin ölçüsüne geri): iz silinmemiştir, form ve kilit geri gelir.
    [Fact]
    public void YKGK02_kapora_yukari_asagi_yukari_dizisinde_form_geri_gelir()
    {
        foreach (var mod in EksikB1Ortak.Modlar)
        {
            kaporaGovdeleri.Clear();
            EksikB1Ortak.Oturum(this, mod);
            var e = Render<Kuyumcu.Web.Pages.SiparisDetay>(p => p.Add(x => x.Id, "s1"));
            e.WaitForElement("#sipd-kart");
            e.Find("#sipd-kapora-al").Click();
            Tutar(e, "500");
            e.WaitForAssertion(() => Assert.False(e.Find("#sipd-kapora-kaydet").HasAttribute("disabled")));
            e.Find("#sipd-kapora-kaydet").Click();
            e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sipd-kapora-masak")));

            Tutar(e, "499,99");
            e.WaitForAssertion(() => Assert.Empty(e.FindAll("#sipd-kapora-masak")), TimeSpan.FromSeconds(3));

            Tutar(e, "500");
            e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sipd-kapora-masak")), TimeSpan.FromSeconds(3));
            Assert.True(e.Find("#sipd-kapora-kaydet").HasAttribute("disabled"));
            e.Dispose();
        }
    }
}
