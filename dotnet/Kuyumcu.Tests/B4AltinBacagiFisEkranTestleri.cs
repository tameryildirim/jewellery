using System.Text.Json;
using Bunit;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-4 — DOĞRULAMA BULGUSU [KRİTİK] B4-3: ALTIN BACAĞININ FİŞE BAĞLANMASI
//
// Sunucu tek etiketli OdemeTuru="ALTIN"ı 400 ile reddediyor (G-1). Ekran o
// seçeneği hâlâ sunuyordu, yerine geçen panel HİÇBİR sayfaya bağlı değildi:
// eski altınla ödeme ekrandan alınamıyordu (özellik kaybı). Bu testler:
//   W-1 tek ödeme listesinde ALTIN YOK, yolu söyleyen ipucu VAR;
//   W-2 karma bacak listesinde ALTIN VAR, seçilince panel açılır, kasa yok,
//       ayarlar SUNUCUDAN istenir;
//   W-3 ikinci bacakta ALTIN çıkmaz (B4-6);
//   W-4 satıcı kimliği eksikken Kaydet KİLİTLİ (B4-32), tamamlanınca AÇIK;
//   W-5 gövde ALTIN sözleşmesini taşır (ayar/gram/fiyat/satıcı, kasaId YOK,
//       tutarTl = türetilen net) ve yanıt okunur: pusula no banda yazılır;
//   W-6 "kalanı ata" ALTIN bacağına yazmaz (B4-31).
// Türkçe JSON gövdesi AYRIŞTIRILARAK sınanır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class B4AltinBacagiFisEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public B4AltinBacagiFisEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            // ⚠ Yeni uçlara "{}" bırakılmaz: gerçekçi yanıt (sunucu sözleşmesiyle aynı alanlar).
            var y = yol switch
            {
                var v when v.Contains("/islemler/altin-bacagi/ayarlar") => """
                    {"stopajOranlari":[
                       {"saticiTipi":"NIHAI_TUKETICI","oran":0,"pusulaGerekir":true},
                       {"saticiTipi":"ESNAF_MUAF","oran":0.02,"pusulaGerekir":true},
                       {"saticiTipi":"MUKELLEF","oran":0,"pusulaGerekir":false}],
                     "alisIskontoOran":null,"saticiAdresiZorunlu":true}
                    """,
                var v when v.Contains("/islemler/satis-fisi") => """
                    {"fisId":"FIS-B4","altinBacagi":{"hareketId":"h-hurda","netTl":9160,"belgeSayisi":2,
                      "giderPusulasi":{"id":"gp1","pusulaNo":"GP-2026-000042","brutTutar":9160,
                        "stopajOran":0,"stopajTutar":0,"netOdenen":9160}}}
                    """,
                var v when v.Contains("/tevsik/durum") => """
                    {"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,
                     "bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}
                    """,
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                // ISLEM-BANT-2: has 2.000 — testlerin eski altın fiyatı (2.000, net 9.160) işlem bandının (%50–%110) İÇİNDE kalsın;
                // 6.800'de 2.000 bant dışıydı ve Kaydet artık bant dışında kilitli (onay yalnız SAHİP/MÜDÜR + gerekçe).
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":2000,"usdTry":40,"eurTry":44,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """
                    [{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,
                      "iscilikTl":0,"satisFiyati":10000}]
                    """,
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/esikler") => """
                    {"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,
                     "ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,
                     "belgeTurleri":["TC_KIMLIK","PASAPORT"]}
                    """,
                var v when v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    /// <summary>
    /// Bir kalemli (10.000 TL) satış, karma açık, ilk satır ALTIN seçilmiş.
    /// TEMA TEMİZLİĞİ 17.10: SATIŞ = Yıldız tezgâhı. Müşteri "Değiştir" katmanından; karma "+ Başka ödeme" ile açılır
    /// (sayfanın OdemeyiBol'u: tek ödeme İLK satır + ikinci satır). İlk satır ALTIN'a çevrilir; ikinci satır (kalan için
    /// nakit) AYNI ekranda hazırdır — eski "+ Başka ödeme ekle" düğmesinin (#islem-bacak-ekle) Yıldız karşılığı tezgâhın hapı.
    /// </summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> AltinBacagiAc()
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        TemaTemizligiGDYardimcisi.MusteriSec(e, "c1");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        TemaTemizligiGDYardimcisi.KalemSayisiBekle(e, 1);
        TemaTemizligiGDYardimcisi.KarmaAc(e);
        SeciciYardim.Sec(e, "islem-bacak-0-arac", "ALTIN");
        e.WaitForElement("#altin-bacagi");
        return e;
    }

    /// <summary>Yalnız ALTIN satırı kalsın: ikinci satır "Çıkar" ile kaldırılır (ALTIN tek ödemeye inmez — karma kalır).</summary>
    private static void YalnizAltin(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        e.Find("#islem-bacak-sil-1").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-bacak-1-arac")));
        Assert.Equal("ALTIN", SeciciYardim.SeciliDeger(e, "islem-bacak-0-arac"));
    }

    /// <summary>K22 · 5 g · 2.000 TL/has-g → has 4,58 · net 9.160,00 (nihai tüketici, kesinti 0).</summary>
    private static void AltinDoldur(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, bool saticiyla = true)
    {
        e.Find("#altin-ayar").Click();
        e.Find("#altin-ayar-liste li[data-deger='K22']").Click();
        e.Find("#altin-gram").Input("5");
        e.Find("#altin-fiyat").Input("2000");
        if (!saticiyla) return;
        e.Find("#altin-satici-ad").Input("Ali Veli");
        e.Find("#altin-satici-tckn").Input("10000000146");
        e.Find("#altin-satici-adres").Input("Ulus, Ankara");
    }

    /// <summary>Kalan 840 TL için NAKIT satırı (tek TL kasası otomatik atanır): Yıldız'da ikinci satır "+ Başka ödeme"
    /// ile zaten açıldı — aracı NAKİT yapılır, tutarı fişin kuralı (OtoDengele) yazar.</summary>
    private static void NakitBacakEkle(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        e.WaitForElement("#islem-bacak-1-arac");
        if (SeciciYardim.SeciliDeger(e, "islem-bacak-1-arac") != "NAKIT") SeciciYardim.Sec(e, "islem-bacak-1-arac", "NAKIT");
    }

    private static AngleSharp.Dom.IElement Kaydet(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
        => e.Find(TemaTemizligiGDYardimcisi.Kaydet);

    // ── W-1 ───────────────────────────────────────────────────────────────
    [Fact]
    public void AltinBacagi_Tek_Odeme_Listesinde_ALTIN_Yok_Ipucu_Var()
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        TemaTemizligiGDYardimcisi.OdemeAc(e);   // Yıldız: ödeme satırına dokun → özgün tek ödeme seçicisi
        e.WaitForElement("#islem-odeme");

        var degerler = SeciciYardim.Secenekler(e, "islem-odeme").Select(o => o.Deger()).ToList();
        Assert.DoesNotContain("ALTIN", degerler);
        Assert.Contains("VERESIYE", degerler);
        var ipucu = e.Find("#islem-odeme-altin-ipucu").TextContent;
        Assert.Contains("Karma ödeme", ipucu);   // yolu SÖYLER (UX-S30): hangi anahtar açılacak
    }

    // ── W-2 ───────────────────────────────────────────────────────────────
    [Fact]
    public void AltinBacagi_Karma_Listesinde_Var_Secilince_Panel_Acilir_Kasa_Yok()
    {
        var e = AltinBacagiAc();

        var araclar = SeciciYardim.Secenekler(e, "islem-bacak-0-arac").Select(o => o.Deger()).ToList();
        Assert.Contains("ALTIN", araclar);
        // B4-5: para kasadan geçmez → kasa seçicisi YOK; B4-31: TL giriş alanı YOK.
        Assert.Empty(e.FindAll("#islem-bacak-0-kasa"));
        Assert.Empty(e.FindAll("#islem-bacak-0-tutar"));
        Assert.Empty(e.Find("#altin-tl").QuerySelectorAll("input, textarea"));
        // Oranlar koda gömülmez: ayarlar SUNUCUDAN istenir (ADR-0005).
        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("/islemler/altin-bacagi/ayarlar")));
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#altin-ayarlar-yok")));
        // B4-35: iki belge kayıttan ÖNCE söylenir.
        Assert.Contains("gider pusulası", e.Find("#altin-belgeler").TextContent);   // SATIS-3 t3 (KAYNAK 2 md.7): büyük harf vurgusu kalktı
    }

    // ── W-3 ───────────────────────────────────────────────────────────────
    [Fact]
    public void AltinBacagi_Ikinci_Bacakta_ALTIN_Secenegi_Cikmaz()
    {
        var e = AltinBacagiAc();
        NakitBacakEkle(e);

        var ikinci = SeciciYardim.Secenekler(e, "islem-bacak-1-arac").Select(o => o.Deger()).ToList();
        Assert.DoesNotContain("ALTIN", ikinci);
        Assert.Contains("NAKIT", ikinci);
        // İlk bacak kendi seçimini korur.
        Assert.Contains("ALTIN", SeciciYardim.Secenekler(e, "islem-bacak-0-arac").Select(o => o.Deger()));
    }

    // ── W-4 ───────────────────────────────────────────────────────────────
    [Fact]
    public void AltinBacagi_Satici_Kimligi_Eksikken_Kaydet_Kilitli_Tamamlaninca_Acik()
    {
        var e = AltinBacagiAc();
        AltinDoldur(e, saticiyla: false);
        e.WaitForAssertion(() => Assert.Equal("9160", SeciciYardim.SeciliDeger(e, "altin-tl")));
        NakitBacakEkle(e);

        // Fark 0 ama satıcı kimliği yok → kilitli, sebep yazılı (UX-S30).
        e.WaitForAssertion(() => Assert.True(Kaydet(e).HasAttribute("disabled")));
        Assert.NotEmpty(e.FindAll("#altin-eksikler li"));

        e.Find("#altin-satici-ad").Input("Ali Veli");
        e.Find("#altin-satici-tckn").Input("10000000146");
        e.Find("#altin-satici-adres").Input("Ulus, Ankara");
        e.WaitForAssertion(() => Assert.False(Kaydet(e).HasAttribute("disabled")));
        Assert.Empty(e.FindAll("#altin-eksikler"));
    }

    // ── W-5 ───────────────────────────────────────────────────────────────
    [Fact]
    public void AltinBacagi_Govde_Altin_Sozlesmesini_Tasir_Pusula_No_Banda_Yazilir()
    {
        var e = AltinBacagiAc();
        AltinDoldur(e);
        NakitBacakEkle(e);
        e.WaitForAssertion(() => Assert.False(Kaydet(e).HasAttribute("disabled")));
        istekler.Clear();
        Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Yol.EndsWith("/islemler/satis-fisi"));
            var kok = JsonDocument.Parse(istek.Govde).RootElement;
            var odemeler = kok.GetProperty("odemeler");
            Assert.Equal(2, odemeler.GetArrayLength());
            var altin = odemeler[0];
            Assert.Equal("ALTIN", altin.GetProperty("arac").GetString());
            Assert.Equal(9160m, altin.GetProperty("tutarTl").GetDecimal());
            Assert.Equal("K22", altin.GetProperty("ayarId").GetString());
            Assert.Equal(5m, altin.GetProperty("gramAgirlik").GetDecimal());
            Assert.Equal(2000m, altin.GetProperty("birimFiyatTlHasGram").GetDecimal());
            var satici = altin.GetProperty("satici");
            Assert.Equal("NIHAI_TUKETICI", satici.GetProperty("saticiTipi").GetString());
            Assert.Equal("Ali Veli", satici.GetProperty("adSoyad").GetString());
            Assert.Equal("10000000146", satici.GetProperty("tckn").GetString());
            // B4-5: kasaId HİÇ gönderilmez.
            Assert.False(altin.TryGetProperty("kasaId", out var k) && k.ValueKind != JsonValueKind.Null);
            var nakit = odemeler[1];
            Assert.Equal("NAKIT", nakit.GetProperty("arac").GetString());
            Assert.Equal(840m, nakit.GetProperty("tutarTl").GetDecimal());
            Assert.Equal("ks1", nakit.GetProperty("kasaId").GetString());
        });
        // UX-S27: yanıt okunur — gider pusulası numarası banda yazılır.
        e.WaitForAssertion(() => Assert.Contains("GP-2026-000042", e.Markup));
    }

    // ── W-6 ───────────────────────────────────────────────────────────────
    [Fact]
    public void AltinBacagi_Kalani_Ata_Altin_Bacagina_Yazmaz()
    {
        var e = AltinBacagiAc();
        YalnizAltin(e);
        AltinDoldur(e);
        // Tek bacak ALTIN (9.160) → kalan 840; hedef yok → düğme KİLİTLİ (tıklanınca boşa gitmez).
        e.WaitForAssertion(() => Assert.True(e.Find("#islem-bacak-kalan-ata").HasAttribute("disabled")));

        // Yıldız: "+ Başka ödeme" karmada yeni satır ekler (FisOdemeBacaklari.BacakEkle aynası); aracı NAKİT.
        e.Find("#yildiz-tezgah-baska-odeme").Click();
        NakitBacakEkle(e);
        SeciciYardim.Sec(e, "islem-bacak-1-tutar", "500");   // kalan 340
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-bacak-kalan-ata").HasAttribute("disabled")));
        e.Find("#islem-bacak-kalan-ata").Click();
        e.WaitForAssertion(() => Assert.False(Kaydet(e).HasAttribute("disabled")));
        // Altın bacağının TL'si türetilen net olarak KALDI.
        Assert.Equal("9160", SeciciYardim.SeciliDeger(e, "altin-tl"));
    }
}

/// <summary>PAKET B-4 — fişin ALTIN bacağı kuralları (saf).</summary>
public sealed class B4AltinBacagiFisSafTestleri
{
    [Fact]
    public void AltinBacagi_Arac_Degisince_Kasa_Bosalir_Cikinca_Girdi_Temizlenir()
    {
        var b = new FisBacagi { Arac = "ALTIN", KasaId = "ks1", PosCihazId = "pc1", TutarTl = 500m };
        FisAltinBacagi.AracDegisti(b);
        Assert.Equal(("", "", 0m), (b.KasaId, b.PosCihazId, b.TutarTl));
        Assert.NotNull(b.Altin);

        b.Arac = "NAKIT";
        FisAltinBacagi.AracDegisti(b);
        Assert.Null(b.Altin);
        Assert.Null(b.AltinDurum);
    }

    [Fact]
    public void AltinBacagi_Tamam_Iki_Altin_Ya_Da_Kilitli_Durumda_False()
    {
        FisBacagi Altin(bool tamam) => new()
        {
            Arac = "ALTIN", Altin = new AltinBacagiGirisi(),
            AltinDurum = new AltinBacagiDurumu(tamam, [], 0m, null, null),
        };
        Assert.True(FisAltinBacagi.Tamam([Altin(true), new FisBacagi { Arac = "NAKIT" }]));
        Assert.False(FisAltinBacagi.Tamam([Altin(false)]));
        Assert.False(FisAltinBacagi.Tamam([Altin(true), Altin(true)]));   // B4-6
        Assert.False(FisAltinBacagi.Tamam([new FisBacagi { Arac = "ALTIN" }])); // girdi yok
        Assert.True(FisAltinBacagi.Tamam([new FisBacagi { Arac = "NAKIT" }]));
    }

    [Fact]
    public void AltinBacagi_Kasa_Almaz_Kalan_Hedefi_Olmaz_Govde_Yalniz_Altinda()
    {
        Assert.False(FisAltinBacagi.KasaAlir(new FisBacagi { Arac = "ALTIN" }));
        Assert.False(FisAltinBacagi.KasaAlir(new FisBacagi { Arac = "VERESIYE" }));
        Assert.True(FisAltinBacagi.KasaAlir(new FisBacagi { Arac = "NAKIT" }));

        var altin = new FisBacagi { Arac = "ALTIN", Altin = new AltinBacagiGirisi() };
        var nakit = new FisBacagi { Arac = "NAKIT" };
        Assert.Null(FisAltinBacagi.KalanHedefi([altin]));
        Assert.Same(nakit, FisAltinBacagi.KalanHedefi([nakit, altin]));
        Assert.Null(FisAltinBacagi.Govde(nakit, null));
    }

    [Fact]
    public void AltinBacagi_Pusula_Notu_Yanittan_Okunur_Yoksa_Null()
    {
        var var_ = JsonDocument.Parse("""{"altinBacagi":{"giderPusulasi":{"pusulaNo":"GP-1"}}}""").RootElement;
        Assert.Contains("GP-1", FisAltinBacagi.PusulaNotu(var_, "tr"));
        var mukellef = JsonDocument.Parse("""{"altinBacagi":{"giderPusulasi":null}}""").RootElement;
        Assert.Null(FisAltinBacagi.PusulaNotu(mukellef, "tr"));
        Assert.Null(FisAltinBacagi.PusulaNotu(JsonDocument.Parse("{}").RootElement, "tr"));
        Assert.Equal("a · b", FisAltinBacagi.NotBirlestir("a", "b"));
        Assert.Null(FisAltinBacagi.NotBirlestir(null, null));
    }
}
