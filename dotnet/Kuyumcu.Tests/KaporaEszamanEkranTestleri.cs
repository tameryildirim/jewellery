using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KAPORA-ESZAMAN — EKRAN (bUnit): sipariş detayında kapora geçmişi.
// Sahte API "{}" varsayılanı YOK: tanımsız uç 404. İki mod (BASİT + PRO).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KaporaEszamanEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol)> istekler = [];
    private string gecmisJson = "[]";
    private string? kaporaHareketId;
    private bool kaporaStornolu;
    private readonly List<string> kaporaGovdeleri = [];

    private static string j<T>(T v) => JsonSerializer.Serialize(v);

    private string DetayJson() =>
        "{\"id\":\"s1\",\"no\":\"S-2026-000001\",\"tur\":\"MUSTERI_SIPARISI\",\"durum\":\"HAZIR\""
        + ",\"cariId\":\"c1\",\"cariAd\":\"Ayşe Yılmaz\",\"subeId\":\"sb1\""
        + ",\"sozVerilenGun\":null,\"aciklama\":\"bilezik\",\"tahminiTutarTl\":null,\"kurSnapshotId\":null,"
        + "\"emanetHareketId\":null,\"geciken\":false,\"duzenlenebilir\":false,\"terminal\":false"
        + ",\"izinliGecisler\":[],\"geriYonluGecisler\":[],\"geriAlmaYetkisi\":true,\"geriAlmaRolleri\":[\"SAHIP\",\"MUDUR\"]"
        + ",\"gerekceIsteyenGecisler\":[\"IPTAL\"],\"gerekceAsgari\":10,\"olusturanKullaniciId\":\"u1\","
        + "\"olusturuldu\":\"2026-09-16T08:00:00Z\",\"guncellendi\":\"2026-09-16T08:00:00Z\","
        + "\"kalemler\":[{\"id\":\"k1\",\"urunId\":null,\"urunAd\":null,\"tanim\":\"yüzük\",\"ayarId\":\"K22\",\"gramAgirlik\":3.5,\"adet\":1.0,\"sira\":0}],"
        + "\"gecisler\":[],\"ustaCariId\":null,\"ustaAd\":null"
        + ",\"kaporaHareketId\":" + j(kaporaHareketId)
        + ",\"kaporaAlinabilir\":" + (kaporaHareketId is null || kaporaStornolu ? "true" : "false")
        + ",\"kaporaKilitSebebi\":null"
        + ",\"kaporaTutari\":" + (kaporaHareketId is null ? "null" : "1500.00")
        + ",\"kaporaTarihi\":null,\"kaporaKasaId\":" + j(kaporaHareketId is null ? null : "ks1")
        + ",\"kaporaStornolu\":" + (kaporaStornolu ? "true" : "false")
        + ",\"kaporaGecmisi\":" + gecmisJson + "}";

    private const string IkiSatir =
        """[{"hareketId":"hk2","tarih":"2026-10-09T09:30:00Z","tutar":1500.00,"odemeTuru":"HAVALE","stornolu":false,"stornoTarihi":null,"guncel":true},"""
        + """{"hareketId":"hk1","tarih":"2026-10-09T08:00:00Z","tutar":1000.00,"odemeTuru":"NAKIT","stornolu":true,"stornoTarihi":"2026-10-09T09:00:00Z","guncel":false}]""";

    private sealed class SahteApi(KaporaEszamanEkranTestleri s) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            lock (s.istekler) s.istekler.Add((istek.Method.Method, yol));
            string? g = yol switch
            {
                var y when istek.Method == HttpMethod.Get && y.EndsWith("/siparisler/s1") => s.DetayJson(),
                var y when y.EndsWith("/subeler") => """[{"id":"sb1","ad":"Merkez"}]""",
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz","tur":"MUSTERI"}]""",
                var y when y.EndsWith("/urunler") => """[{"id":"u0","kod":"K0","ad":"Ürün 0","ayarId":"K22","gramAgirlik":1.0}]""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":4200}""",
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                // tur 2 — MASAK (Banka ekran testiyle aynı biçim; eşik SUNUCUDAN)
                var y when y.Contains("/masak/esikler") =>
                    """{"kimlikEsigi":185000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000,"ekBelgeEsigi":20000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var y when y.Contains("/masak/gereksinim") =>
                    """{"kademe":"AMAC_BEYANI","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                _ => null,
            };
            if (istek.Method == HttpMethod.Post && yol.EndsWith("/siparisler/s1/kapora"))
            {
                lock (s.istekler) s.kaporaGovdeleri.Add(istek.Content!.ReadAsStringAsync().Result);
                // Canlıda görülen HAM sunucu metni (tasarım hakemi KRİTİK, 13-s0-gonderildi): ekrana basılmamalı.
                return Task.FromResult(EksikB1Ortak.Yanit(HttpStatusCode.BadRequest,
                    """{"message":"MASAK: 370.000 TL ve üstü işlemde kimlik tespiti zorunludur · İşlem amacı belirtilmelidir · MASAK beyan açıklaması (masak.aciklama) en az 20 karakter olmalıdır (şu an 0)"}"""));
            }
            return Task.FromResult(g is null
                ? EksikB1Ortak.Yanit(HttpStatusCode.NotFound, """{"message":"sahte API bu ucu tanımıyor"}""")
                : EksikB1Ortak.Yanit(HttpStatusCode.OK, g));
        }
    }

    public KaporaEszamanEkranTestleri() => EksikB1Ortak.Kur(this, new SahteApi(this));

    private IRenderedComponent<Kuyumcu.Web.Pages.SiparisDetay> Detay(string mod)
    {
        EksikB1Ortak.Oturum(this, mod);
        var e = Render<Kuyumcu.Web.Pages.SiparisDetay>(p => p.Add(x => x.Id, "s1"));
        e.WaitForElement("#sipd-kart");
        return e;
    }

    // ── Geri verilip yeniden alınan kapora: geçmiş KAPALI akordeonda, iki satır ─────────
    [Fact]
    public void KEE01_kapora_gecmisi_IKI_MODDA_kapali_gelir_acilinca_iki_satir()
    {
        kaporaHareketId = "hk2";
        gecmisJson = IkiSatir;
        var M = KaporaGecmisSozlugu.Tr;
        foreach (var mod in EksikB1Ortak.Modlar)
        {
            var e = Detay(mod);
            var kok = e.Find("#sipd-kapora-gecmis");
            Assert.Contains(M.Baslik, kok.TextContent);
            Assert.Equal("2", e.Find("#sipd-kapora-gecmis-sayi").TextContent.Trim());
            Assert.Empty(e.FindAll("#sipd-kapora-gecmis-govde"));                    // kurucu: geçmiş kapalı gelir
            var once = istekler.Count;
            e.Find("#sipd-kapora-gecmis-ac").Click();
            Assert.Equal(once, istekler.Count);                                       // veri kartla geldi — istek yok
            var eski = e.Find("#sipd-kapora-gecmis-hk1").TextContent;
            var yeni = e.Find("#sipd-kapora-gecmis-hk2").TextContent;
            Assert.Contains(string.Format(M.GeriVerildi, "").Trim(), eski);
            Assert.Contains("Nakit", eski);
            Assert.Contains("1.000", eski); Assert.Contains("₺", eski);
            Assert.Contains(M.Guncel, yeni);
            Assert.Contains("Havale", yeni);
            Assert.Contains("1.500", yeni);
            Assert.DoesNotContain("TRY", eski + yeni);
            // En yeni önce.
            var satirlar = e.FindAll("#sipd-kapora-gecmis-liste li");
            Assert.Equal("sipd-kapora-gecmis-hk2", satirlar[0].Id);
            e.Dispose();
        }
    }

    // ── Tek canlı kapora: özet yeterli → akordeon ÇİZİLMEZ (zorunlu önce) ────────────────
    [Fact]
    public void KEE02_tek_canli_kapora_gecmis_cizilmez()
    {
        kaporaHareketId = "hk2";
        gecmisJson = """[{"hareketId":"hk2","tarih":"2026-10-09T09:30:00Z","tutar":1500.00,"odemeTuru":"HAVALE","stornolu":false,"stornoTarihi":null,"guncel":true}]""";
        foreach (var mod in EksikB1Ortak.Modlar)
        {
            var e = Detay(mod);
            Assert.Empty(e.FindAll("#sipd-kapora-gecmis"));
            Assert.NotEmpty(e.FindAll("#sipd-kapora-tutar"));
            e.Dispose();
        }
    }

    // ── Tek ama GERİ VERİLMİŞ kapora: geçmiş görünür (ne zaman geri verildiği) ───────────
    [Fact]
    public void KEE03_tek_geri_verilmis_kapora_gecmiste_gorunur()
    {
        kaporaHareketId = "hk1";
        kaporaStornolu = true;
        gecmisJson = """[{"hareketId":"hk1","tarih":"2026-10-09T08:00:00Z","tutar":1000.00,"odemeTuru":"KART","stornolu":true,"stornoTarihi":"2026-10-09T09:00:00Z","guncel":true}]""";
        var e = Detay(ArayuzModuServisi.Basit);
        e.Find("#sipd-kapora-gecmis-ac").Click();
        var t = e.Find("#sipd-kapora-gecmis-hk1").TextContent;
        Assert.Contains("Kart", t);
        Assert.Contains(string.Format(KaporaGecmisSozlugu.Tr.GeriVerildi, "").Trim(), t);
    }

    // ── EN: başlık ve durum İngilizce ─────────────────────────────────────────────────
    [Fact]
    public async Task KEE04_EN_modda_gecmis_ingilizce()
    {
        kaporaHareketId = "hk2";
        gecmisJson = IkiSatir;
        EksikB1Ortak.Oturum(this, ArayuzModuServisi.Profesyonel);
        await Services.GetRequiredService<DilServisi>().DegistirAsync("en");
        var e = Render<Kuyumcu.Web.Pages.SiparisDetay>(p => p.Add(x => x.Id, "s1"));
        e.WaitForElement("#sipd-kart");
        Assert.Contains(KaporaGecmisSozlugu.En.Baslik, e.Find("#sipd-kapora-gecmis").TextContent);
        e.Find("#sipd-kapora-gecmis-ac").Click();
        Assert.Contains("Refunded", e.Find("#sipd-kapora-gecmis-hk1").TextContent);
        Assert.Contains("Current", e.Find("#sipd-kapora-gecmis-hk2").TextContent);
        Assert.Contains("Cash", e.Find("#sipd-kapora-gecmis-hk1").TextContent);
    }

    // ── TUR 2 (tasarım hakemi KRİTİK): eşiği aşan kaporada çıkış yolu — kimlik alanları çekmecede açılır,
    // Kaydet kilidinin SEBEBİ sade dille yazılı; ham alan adı/jargon YOK. İki mod. ─────────────────────
    [Fact]
    public void KEE05_esik_ustu_kaporada_kimlik_alanlari_acilir_kaydet_sebepli_kilitli()
    {
        var M = KaporaGecmisSozlugu.Tr;
        foreach (var mod in EksikB1Ortak.Modlar)
        {
            var e = Detay(mod);
            e.Find("#sipd-kapora-al").Click();
            Assert.Empty(e.FindAll("#sipd-kapora-masak-not"));                          // zorunlu önce: küçük tutarda yok
            e.Find("#sipd-kapora-tutar-g").Input("400.000");
            e.Find("#sipd-kapora-tutar-g").Blur();
            e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sipd-kapora-masak")));
            Assert.Empty(e.FindAll("#sipd-kapora-masak-not"));   // YASAL-KAPI-3 tur 2: form açıkken not yok — uyarı tek kez (form başlığı)
            Assert.True(e.Find("#sipd-kapora-kaydet").HasAttribute("disabled"));
            Assert.False(string.IsNullOrWhiteSpace(e.Find("#sipd-kapora-form-kilit").TextContent));   // UX-S30
            Assert.Contains(istekler, i => i.Yol.Contains("/masak/gereksinim"));
            Assert.DoesNotContain("masak.aciklama", e.Markup);
            e.Dispose();
        }
    }

    // ── Eşik altı tutarda sunucu MASAK'la reddederse (günlük toplam) çekmece kapanmaz, alanlar açılır,
    // ham sunucu metni (masak.aciklama, "(şu an 0)") ekranda GÖRÜNMEZ; ilk gövde masak taşımaz. ───────
    [Fact]
    public void KEE06_sunucu_masak_reddi_ham_metin_basmaz_alanlari_acar()
    {
        var M = KaporaGecmisSozlugu.Tr;
        foreach (var mod in EksikB1Ortak.Modlar)
        {
            kaporaGovdeleri.Clear();
            var e = Detay(mod);
            e.Find("#sipd-kapora-al").Click();
            e.Find("#sipd-kapora-tutar-g").Input("500");
            e.Find("#sipd-kapora-tutar-g").Blur();
            e.WaitForAssertion(() => Assert.False(e.Find("#sipd-kapora-kaydet").HasAttribute("disabled")));
            e.Find("#sipd-kapora-kaydet").Click();
            e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#sipd-kapora-masak")));
            Assert.Single(kaporaGovdeleri);
            Assert.Contains("\"masak\":null", kaporaGovdeleri[0]);                       // eşik altı: beyan gönderilmedi
            Assert.Empty(e.FindAll("#sipd-kapora-masak-not"));   // YASAL-KAPI-3 tur 2: form açıkken not yok — uyarı tek kez (form başlığı)
            Assert.NotEmpty(e.FindAll("#sipd-kapora-form"));                              // çekmece AÇIK, değer kaybolmadı
            Assert.DoesNotContain("masak.aciklama", e.Markup);
            Assert.DoesNotContain("(şu an", e.Markup);
            Assert.True(e.Find("#sipd-kapora-kaydet").HasAttribute("disabled"));          // kimlik eksik → sebepli kilit
            e.Dispose();
        }
    }
}
