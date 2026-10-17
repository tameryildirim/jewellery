using System.Text.Json;
using Bunit;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-3 — FİŞ ÜST SATIRI: PEŞİN MÜŞTERİ BANDI + BAKİYE ROZETİ (bUnit + saf)
//
// Sınanan (PLAN § S-2 madde 2.4 + 2.6):
//   • müşteri seçilince #islem-cari-bakiye rozeti GET /cari listesindeki
//     bakiyeyle AYNI metni yazar (HAS + TL, döviz); Peşin'de "Borcu yok";
//   • Peşin Müşteri ile kayıt SUNUCUDA müşteri alanına işaretli reddedilirse
//     (400 + alanlar.cariId) "gerçek müşteri seçin" bandı çıkar, sunucu mesajı
//     hata bandında KALIR; "Müşteri seç" ön seçimi kaldırır (istek atmaz);
//   • alan işaretsiz 400 ya da gerçek caride 400 bandı AÇMAZ (yanlış yönlendirme yok);
//   • kayıttan sonra rozet TAZE listeden okunur;
//   • metinlerde eşik SAYISI yok (PLAN § 3 md.6).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S23FisMusteriEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private static readonly FisMusteriMetinleri M = FisMusteriSozlugu.Tr;

    /// <summary>POST /islemler/satis yanıtı: null = 200; aksi hâlde 400 gövdesi.</summary>
    private string? satisRed;
    /// <summary>Kayıttan SONRA dönen cari listesi (tazeleme testi); null = ilk liste.</summary>
    private string? ikinciCariListesi;

    private const string KimlikReddi = "Peşin Müşteri ile bu tutarda işlem yapılamaz. Kimlik tespiti için gerçek müşteri seçin.";

    private const string CariListesi = """
        [{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","tlBakiye":0,"hasGramBakiye":0,
          "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"sistemCarisi":true,"aktif":true},
         {"id":"c1","ad":"Hasan Kaya","tur":"MUSTERI","tlBakiye":38900,"hasGramBakiye":5.66,
          "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"sistemCarisi":false,"aktif":true},
         {"id":"c2","ad":"Elif Arslan","tur":"MUSTERI","tlBakiye":-1500,"hasGramBakiye":0,
          "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"sistemCarisi":false,"aktif":true},
         {"id":"c3","ad":"Mehmet Öz","tur":"TEDARIKCI","tlBakiye":1000,"hasGramBakiye":-22,
          "usdBakiye":250,"eurBakiye":0,"gbpBakiye":0,"sistemCarisi":false,"aktif":true}]
        """;

    public S23FisMusteriEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(S23FisMusteriEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));

            if (istek.Method == HttpMethod.Post && yol.EndsWith("/islemler/satis") && t.satisRed is not null)
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(t.satisRed, System.Text.Encoding.UTF8, "application/json"),
                };

            var postVar = t.istekler.Any(i => i.Metot == "POST");
            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") =>
                    """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.EndsWith("/cari") => postVar && t.ikinciCariListesi is not null ? t.ikinciCariListesi : CariListesi,
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/pos/cihazlar") => "[]",
                var v when v.Contains("/islemler/satis") => """{"id":"h1","fisId":"f1"}""",
                var v when v.Contains("/reyonlar") || v.Contains("/islemler/son") || v.Contains("/iade-adaylari") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    // TEMA TEMİZLİĞİ 17.10: SATIŞ fişi artık hep Yıldız tezgâhıdır. Müşteri seçimi "Değiştir" katmanında
    // (YildizMusteriSecici); seçili müşterinin TL durumu üst satırda (#yildiz-tezgah-ust-musteri,
    // YildizTezgahBicim.MusteriDurumu). Peşin reddinin "gerçek müşteri seçin" bandı AYNI id'lerle
    // (#islem-pesin-esik, #islem-pesin-esik-sec, #islem-pesin-esik-yeni) bu katmanda; sunucu mesajı
    // Fark bloğunun hata satırında (#yildiz-tezgah-hata). Eski fiş üst satırının HAS+döviz rozeti
    // (#islem-cari-bakiye) Yıldız'da BİLEREK yok (tuvalde yok — Yildiz4CTestleri.Y4C_13 "eski üst özet YOK").

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, mod));
        return Render<Kuyumcu.Web.Pages.Islemler>().YildizHazir();
    }

    /// <summary>Basit satış ekranı + 10.000 TL'lik bir kalem; sonra Kaydet.</summary>
    private void KalemEkleVeKaydet(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        e.YildizKalemEkle(beklenenFiyat: "10000");
        e.YildizKaydet();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis")));
    }

    private static string Govde400(bool alanli) => alanli
        ? JsonSerializer.Serialize(new { message = KimlikReddi, alanlar = new { cariId = new[] { KimlikReddi } }, error = "Bad Request", statusCode = 400 })
        : JsonSerializer.Serialize(new { message = "Stok yetersiz.", error = "Bad Request", statusCode = 400 });

    private static CariVeri Cari(string id) =>
        JsonSerializer.Deserialize<List<CariVeri>>(CariListesi, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.Single(c => c.Id == id);

    private static string UstMusteri(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) =>
        e.Find("#yildiz-tezgah-ust-musteri").TextContent.Replace(' ', ' ').Trim();

    /// <summary>"Değiştir" katmanını açar (bant yalnız orada çizilir — boş DOM'da yanlış yeşil olmasın diye katman beklenir).</summary>
    private static void KatmanAc(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        if (e.FindAll("#yildiz-musteri-secici").Count == 0) e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForElement("#yildiz-musteri-secici");
    }

    // ═══ ÜST SATIR DURUMU (eski "rozet") ═══════════════════════════════════

    [Fact]
    public void S23E_01_gercek_musteri_secilince_rozet_metni_cari_bakiyesiyle_AYNI()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        e.YildizMusteriSec("c1");

        // Liste JSON'unun ekranda kullanılan tipe ayrıştırılmış hâli (sunucu sözleşmesi).
        var c1 = Cari("c1");
        Assert.Equal(38900m, c1.TlBakiye);
        var durum = YildizTezgahBicim.MusteriDurumu(c1, "tr")!;
        Assert.Contains(Bicim.Tl(38900m), durum);   // durum GET /cari listesindeki bakiyeden
        e.WaitForAssertion(() => Assert.Equal($"Hasan Kaya · {durum}".Replace(' ', ' '), UstMusteri(e)));
        // Yıldız katmanındaki satır da AYNI durumu yazar (tek kaynak)
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForAssertion(() => Assert.Contains(durum, e.Find("#yildiz-musteri-sat-c1").TextContent));
    }

    [Fact]
    public void S23E_02_Pesin_Musteri_on_secili_rozet_Borcu_yok_yesil()
    {
        // Yıldız: Peşin Müşteri'de bakiye durumu YAZILMAZ (sistem carisi — YildizTezgahBicim.MusteriDurumu null).
        var e = Ekran(ArayuzModuServisi.Basit);
        Assert.Equal("Peşin Müşteri", UstMusteri(e));
        Assert.Null(YildizTezgahBicim.MusteriDurumu(Cari("pm1"), "tr"));
    }

    [Fact]
    public void S23E_03_alacakli_musteri_mavi_ve_karisik_yon_iki_yonu_ayri_yazar_doviz_kendi_birimiyle()
    {
        // Yıldız üst satırı TL durumunu yazar (tuval "· bize borçlu / biz borçluyuz"); eski rozetin HAS + döviz
        // yön satırı tuvalde yok (bilerek). Yön kuralı tek kaynaktan (BorcDiliSozlugu.Durum).
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        e.YildizMusteriSec("c2");
        var d2 = YildizTezgahBicim.MusteriDurumu(Cari("c2"), "tr")!;
        Assert.Contains(Bicim.Tl(1500m), d2);
        e.WaitForAssertion(() => Assert.Equal($"Elif Arslan · {d2}".Replace(' ', ' '), UstMusteri(e)));

        e.YildizMusteriSec("c3");
        var d3 = YildizTezgahBicim.MusteriDurumu(Cari("c3"), "tr")!;
        Assert.NotEqual(d2, d3);   // alacaklı ↔ borçlu yönü ayrı yazılır
        e.WaitForAssertion(() => Assert.Equal($"Mehmet Öz · {d3}".Replace(' ', ' '), UstMusteri(e)));
    }

    // TEMA TEMİZLİĞİ 17.10: S23E_04_musteri_bosaltilinca_rozet_CIZILMEZ kaldırıldı — Yıldız müşteri katmanında "boş müşteri"
    // seçimi yok (katman yalnız müşteri seçer; boşaltma yalnız "gerçek müşteri seçin" bandıyla — S23E_06'da sınanır).

    // ═══ "GERÇEK MÜŞTERİ SEÇİN" BANDI ══════════════════════════════════════

    [Fact]
    public void S23E_05_Pesinde_alan_isaretli_400_bandi_ACAR_mesaj_hata_bandinda_KALIR()
    {
        satisRed = Govde400(alanli: true);
        var e = Ekran(ArayuzModuServisi.Basit);
        KatmanAc(e);
        Assert.Empty(e.FindAll("#islem-pesin-esik"));
        e.Find("#yildiz-tezgah-degistir").Click();   // katman kapanır
        KalemEkleVeKaydet(e);

        e.WaitForAssertion(() => Assert.Contains(KimlikReddi, e.Find("#yildiz-tezgah-hata").TextContent));   // sunucu sözü yutulmaz
        KatmanAc(e);
        var bant = e.WaitForElement("#islem-pesin-esik");
        Assert.Equal("alert", bant.GetAttribute("role"));
        Assert.Contains(M.KimlikBandi, bant.TextContent);
        Assert.DoesNotMatch(@"\d", bant.TextContent);           // eşik sayısı istemcide YOK
        Assert.Equal("/cariler", e.Find("#islem-pesin-esik-yeni").GetAttribute("href"));
    }

    [Fact]
    public void S23E_06_Musteri_sec_on_secimi_kaldirir_bant_kapanir_istek_ATMAZ()
    {
        satisRed = Govde400(alanli: true);
        var e = Ekran(ArayuzModuServisi.Basit);
        KalemEkleVeKaydet(e);
        KatmanAc(e);
        e.WaitForElement("#islem-pesin-esik");
        var postSayisi = istekler.Count(i => i.Metot == "POST");

        e.Find("#islem-pesin-esik-sec").Click();

        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-pesin-esik")));
        Assert.Single(e.FindAll("#yildiz-musteri-secici"));                       // katman açık — boş DOM değil
        Assert.Empty(e.FindAll("#yildiz-musteri-liste [aria-current=true]"));     // ön seçim kalktı
        Assert.Empty(e.FindAll("#yildiz-tezgah-ust-musteri"));
        Assert.Equal(postSayisi, istekler.Count(i => i.Metot == "POST"));
    }

    [Fact]
    public void S23E_07_alan_isaretsiz_400_bandi_ACMAZ()
    {
        satisRed = Govde400(alanli: false);
        var e = Ekran(ArayuzModuServisi.Basit);
        KalemEkleVeKaydet(e);
        e.WaitForAssertion(() => Assert.Contains("Stok yetersiz.", e.Find("#yildiz-tezgah-hata").TextContent));
        KatmanAc(e);
        Assert.Empty(e.FindAll("#islem-pesin-esik"));
    }

    [Fact]
    public void S23E_08_gercek_caride_alanli_400_bandi_ACMAZ()
    {
        satisRed = Govde400(alanli: true);
        var e = Ekran(ArayuzModuServisi.Basit);
        e.YildizMusteriSec("c1");
        KalemEkleVeKaydet(e);
        e.WaitForAssertion(() => Assert.Contains(KimlikReddi, e.Find("#yildiz-tezgah-hata").TextContent));
        KatmanAc(e);
        Assert.Empty(e.FindAll("#islem-pesin-esik"));
    }

    [Fact]
    public void S23E_09_basarili_kayittan_sonra_bant_yok_rozet_TAZE_listeden()
    {
        ikinciCariListesi = CariListesi.Replace("\"tlBakiye\":38900", "\"tlBakiye\":48900");
        var e = Ekran(ArayuzModuServisi.Basit);
        e.YildizMusteriSec("c1");
        e.WaitForAssertion(() => Assert.Contains(Bicim.Tl(38900m).Replace(' ', ' '), UstMusteri(e)));
        KalemEkleVeKaydet(e);

        // Kayıttan sonra GET /cari yeniden çekildi; fiş yine Peşin ile açıldı.
        e.WaitForAssertion(() => Assert.Equal("Peşin Müşteri", UstMusteri(e)));
        var sonPost = istekler.FindIndex(i => i.Metot == "POST");
        Assert.Contains(istekler.Skip(sonPost), i => i.Metot == "GET" && i.Yol.EndsWith("/cari"));
        KatmanAc(e);
        Assert.Empty(e.FindAll("#islem-pesin-esik"));

        e.YildizMusteriSec("c1");
        e.WaitForAssertion(() => Assert.Contains(Bicim.Tl(48900m).Replace(' ', ' '), UstMusteri(e)));
    }

    [Fact]
    public void S23E_10_bant_SON_denemeye_aittir_basarili_kayitta_kapanir()
    {
        satisRed = Govde400(alanli: true);
        var e = Ekran(ArayuzModuServisi.Basit);
        KalemEkleVeKaydet(e);
        KatmanAc(e);
        e.WaitForElement("#islem-pesin-esik");

        satisRed = null;   // sunucu artık kabul ediyor (ör. tutar düşürüldü)
        e.Find("#yildiz-tezgah-kaydet").Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() => Assert.Equal(2, istekler.Count(i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis"))));
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-pesin-esik")));
        Assert.Equal("Peşin Müşteri", UstMusteri(e));
        KatmanAc(e);
        Assert.Empty(e.FindAll("#islem-pesin-esik"));
    }

    [Fact]
    public void S23E_11_musteri_degisince_bant_iner_Pesine_donunce_GERI_GELMEZ()
    {
        satisRed = Govde400(alanli: true);
        var e = Ekran(ArayuzModuServisi.Basit);
        KalemEkleVeKaydet(e);
        KatmanAc(e);
        e.WaitForElement("#islem-pesin-esik");

        e.YildizMusteriSec("c1");
        KatmanAc(e);
        Assert.Empty(e.FindAll("#islem-pesin-esik"));
        e.YildizMusteriSec("pm1");
        e.WaitForAssertion(() => Assert.Equal("Peşin Müşteri", UstMusteri(e)));
        KatmanAc(e);
        Assert.Empty(e.FindAll("#islem-pesin-esik"));
    }

    [Fact]
    public void S23E_12_tur_degisince_bant_iner_satisa_donunce_GERI_GELMEZ()
    {
        satisRed = Govde400(alanli: true);
        var e = Ekran(ArayuzModuServisi.Basit);
        KalemEkleVeKaydet(e);
        KatmanAc(e);
        e.WaitForElement("#islem-pesin-esik");

        e.Find("#yildiz-tur-HURDA_ALIS").Click();   // Yıldız'ın görünür "Alış" sekmesi
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#yildiz-tezgah")));
        Assert.Empty(e.FindAll("#islem-pesin-esik"));
        e.Find("#yildiz-tur-SATIS").Click();
        e.WaitForElement("#yildiz-tezgah-degistir");
        KatmanAc(e);
        Assert.Empty(e.FindAll("#islem-pesin-esik"));
    }

    // ═══ SAF ═══════════════════════════════════════════════════════════════

    [Fact]
    public void S23_01_sozluk_TR_EN_bos_anahtar_yok_ve_esik_sayisi_yok()
    {
        foreach (var m in new[] { FisMusteriSozlugu.Tr, FisMusteriSozlugu.En })
            foreach (var p in typeof(FisMusteriMetinleri).GetProperties())
            {
                var v = (string)p.GetValue(m)!;
                Assert.False(string.IsNullOrWhiteSpace(v), p.Name);
                Assert.DoesNotMatch(@"\d", v);
            }
        Assert.Same(FisMusteriSozlugu.En, FisMusteriSozlugu.Al("en"));
        Assert.Same(FisMusteriSozlugu.Tr, FisMusteriSozlugu.Al("tr"));
        // Etiketler en çok 3 kelime (PLAN § 5B).
        foreach (var etiket in new[] { M.BorcuYok, M.Borcu, M.Alacagi, M.GercekMusteriSec, M.YeniMusteri })
            Assert.True(etiket.Split(' ').Length <= 3, etiket);
    }

    [Fact]
    public void S23_02_rozet_kurali_yon_ve_birim()
    {
        Assert.Null(FisBakiyeRozeti.Olustur(null, M));
        var sifir = FisBakiyeRozeti.Olustur(new CariVeri("x", "X"), M)!.Value;
        Assert.Equal((M.BorcuYok, "rozet rozet-yesil"), sifir);

        var borc = FisBakiyeRozeti.Olustur(new CariVeri("x", "X", TlBakiye: 100m, HasGramBakiye: 1.5m), M)!.Value;
        Assert.Equal($"{M.Borcu}: 1,500 g has · {Bicim.Tl(100m)}", borc.Metin);
        Assert.Equal("rozet rozet-sari", borc.Sinif);

        var yalnizHasAlacak = FisBakiyeRozeti.Olustur(new CariVeri("x", "X", HasGramBakiye: -2m), M)!.Value;
        Assert.Equal($"{M.Alacagi}: 2,000 g has", yalnizHasAlacak.Metin);
        Assert.Equal("rozet rozet-mavi", yalnizHasAlacak.Sinif);

        var euro = FisBakiyeRozeti.Olustur(new CariVeri("x", "X", EurBakiye: 10m, GbpBakiye: -5m), M)!.Value;
        Assert.Equal($"{M.Borcu}: {Bicim.Para(10m, "EUR")} — {M.Alacagi}: {Bicim.Para(5m, "GBP")}", euro.Metin);
    }
}
