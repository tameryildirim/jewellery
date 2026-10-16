using System.Text.Json;
using Bunit;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
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

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-tur-SATIS");
        e.WaitForAssertion(() => Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        return e;
    }

    /// <summary>Basit satış ekranı + 10.000 TL'lik bir kalem; sonra Kaydet.</summary>
    private void KalemEkleVeKaydet(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.Equal("10000", SeciciYardim.SeciliDeger(e, "islem-birimfiyat")));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForElement("#islem-kalem-liste-sar");
        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis")));
    }

    private static string Govde400(bool alanli) => alanli
        ? JsonSerializer.Serialize(new { message = KimlikReddi, alanlar = new { cariId = new[] { KimlikReddi } }, error = "Bad Request", statusCode = 400 })
        : JsonSerializer.Serialize(new { message = "Stok yetersiz.", error = "Bad Request", statusCode = 400 });

    // ═══ ROZET ═════════════════════════════════════════════════════════════

    [Fact]
    public void S23E_01_gercek_musteri_secilince_rozet_metni_cari_bakiyesiyle_AYNI()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        SeciciYardim.Sec(e, "islem-cari", "c1");

        var rozet = e.WaitForElement("#islem-cari-bakiye");
        // Liste JSON'unun ekranda kullanılan tipe ayrıştırılmış hâli (sunucu sözleşmesi).
        var c1 = JsonSerializer.Deserialize<List<CariVeri>>(CariListesi, new JsonSerializerOptions(JsonSerializerDefaults.Web))!
            .Single(c => c.Id == "c1");
        Assert.Equal(38900m, c1.TlBakiye);
        Assert.Equal(5.66m, c1.HasGramBakiye);
        Assert.Equal(FisBakiyeRozeti.Olustur(c1, M)!.Value.Metin, rozet.TextContent.Trim());
        Assert.Contains(Bicim.Tl(38900m), rozet.TextContent);
        Assert.Contains("5,660 g has", rozet.TextContent);
        Assert.StartsWith(M.Borcu, rozet.TextContent.Trim());
        Assert.Contains("rozet-sari", rozet.ClassName);
        Assert.False(string.IsNullOrWhiteSpace(rozet.GetAttribute("title")));
    }

    [Fact]
    public void S23E_02_Pesin_Musteri_on_secili_rozet_Borcu_yok_yesil()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        var rozet = e.WaitForElement("#islem-cari-bakiye");
        Assert.Equal(M.BorcuYok, rozet.TextContent.Trim());
        Assert.Contains("rozet-yesil", rozet.ClassName);
    }

    [Fact]
    public void S23E_03_alacakli_musteri_mavi_ve_karisik_yon_iki_yonu_ayri_yazar_doviz_kendi_birimiyle()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        SeciciYardim.Sec(e, "islem-cari", "c2");
        var r2 = e.WaitForElement("#islem-cari-bakiye");
        Assert.Equal($"{M.Alacagi}: {Bicim.Tl(1500m)}", r2.TextContent.Trim());
        Assert.Contains("rozet-mavi", r2.ClassName);

        SeciciYardim.Sec(e, "islem-cari", "c3");
        e.WaitForAssertion(() =>
        {
            var r3 = e.Find("#islem-cari-bakiye").TextContent.Trim();
            Assert.Equal($"{M.Borcu}: {Bicim.Tl(1000m)} · {Bicim.Para(250m, "USD")} — {M.Alacagi}: 22,000 g has", r3);
        });
    }

    [Fact]
    public void S23E_04_musteri_bosaltilinca_rozet_CIZILMEZ()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        e.WaitForElement("#islem-cari-bakiye");
        SeciciYardim.Sec(e, "islem-cari", "");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-cari-bakiye")));
    }

    // ═══ "GERÇEK MÜŞTERİ SEÇİN" BANDI ══════════════════════════════════════

    [Fact]
    public void S23E_05_Pesinde_alan_isaretli_400_bandi_ACAR_mesaj_hata_bandinda_KALIR()
    {
        satisRed = Govde400(alanli: true);
        var e = Ekran(ArayuzModuServisi.Basit);
        Assert.Empty(e.FindAll("#islem-pesin-esik"));
        KalemEkleVeKaydet(e);

        var bant = e.WaitForElement("#islem-pesin-esik");
        Assert.Equal("alert", bant.GetAttribute("role"));
        Assert.Contains(M.KimlikBandi, bant.TextContent);
        Assert.DoesNotMatch(@"\d", bant.TextContent);           // eşik sayısı istemcide YOK
        Assert.Contains(KimlikReddi, e.Find("#hata-bandi").TextContent);   // sunucu sözü yutulmaz
        Assert.Equal("/cariler", e.Find("#islem-pesin-esik-yeni").GetAttribute("href"));
    }

    [Fact]
    public void S23E_06_Musteri_sec_on_secimi_kaldirir_bant_kapanir_istek_ATMAZ()
    {
        satisRed = Govde400(alanli: true);
        var e = Ekran(ArayuzModuServisi.Basit);
        KalemEkleVeKaydet(e);
        e.WaitForElement("#islem-pesin-esik");
        var postSayisi = istekler.Count(i => i.Metot == "POST");

        e.Find("#islem-pesin-esik-sec").Click();

        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-pesin-esik")));
        Assert.Equal("", SeciciYardim.SeciliDeger(e, "islem-cari") ?? "");
        Assert.Empty(e.FindAll("#islem-pesin-notu"));
        Assert.Equal(postSayisi, istekler.Count(i => i.Metot == "POST"));
    }

    [Fact]
    public void S23E_07_alan_isaretsiz_400_bandi_ACMAZ()
    {
        satisRed = Govde400(alanli: false);
        var e = Ekran(ArayuzModuServisi.Basit);
        KalemEkleVeKaydet(e);
        e.WaitForAssertion(() => Assert.Contains("Stok yetersiz.", e.Find("#hata-bandi").TextContent));
        Assert.Empty(e.FindAll("#islem-pesin-esik"));
    }

    [Fact]
    public void S23E_08_gercek_caride_alanli_400_bandi_ACMAZ()
    {
        satisRed = Govde400(alanli: true);
        var e = Ekran(ArayuzModuServisi.Basit);
        SeciciYardim.Sec(e, "islem-cari", "c1");
        KalemEkleVeKaydet(e);
        e.WaitForAssertion(() => Assert.Contains(KimlikReddi, e.Find("#hata-bandi").TextContent));
        Assert.Empty(e.FindAll("#islem-pesin-esik"));
    }

    [Fact]
    public void S23E_09_basarili_kayittan_sonra_bant_yok_rozet_TAZE_listeden()
    {
        ikinciCariListesi = CariListesi.Replace("\"tlBakiye\":38900", "\"tlBakiye\":48900");
        var e = Ekran(ArayuzModuServisi.Basit);
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.WaitForAssertion(() => Assert.Contains(Bicim.Tl(38900m), e.Find("#islem-cari-bakiye").TextContent));
        KalemEkleVeKaydet(e);

        // Kayıttan sonra GET /cari yeniden çekildi; fiş yine Peşin ile açıldı.
        e.WaitForAssertion(() => Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        var sonPost = istekler.FindIndex(i => i.Metot == "POST");
        Assert.Contains(istekler.Skip(sonPost), i => i.Metot == "GET" && i.Yol.EndsWith("/cari"));
        Assert.Empty(e.FindAll("#islem-pesin-esik"));

        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.WaitForAssertion(() => Assert.Contains(Bicim.Tl(48900m), e.Find("#islem-cari-bakiye").TextContent));
    }

    [Fact]
    public void S23E_10_bant_SON_denemeye_aittir_basarili_kayitta_kapanir()
    {
        satisRed = Govde400(alanli: true);
        var e = Ekran(ArayuzModuServisi.Basit);
        KalemEkleVeKaydet(e);
        e.WaitForElement("#islem-pesin-esik");

        satisRed = null;   // sunucu artık kabul ediyor (ör. tutar düşürüldü)
        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(2, istekler.Count(i => i.Metot == "POST" && i.Yol.EndsWith("/islemler/satis"))));
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-pesin-esik")));
        Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari"));
    }

    [Fact]
    public void S23E_11_musteri_degisince_bant_iner_Pesine_donunce_GERI_GELMEZ()
    {
        satisRed = Govde400(alanli: true);
        var e = Ekran(ArayuzModuServisi.Basit);
        KalemEkleVeKaydet(e);
        e.WaitForElement("#islem-pesin-esik");

        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-pesin-esik")));
        SeciciYardim.Sec(e, "islem-cari", "pm1");
        e.WaitForElement("#islem-pesin-notu");
        Assert.Empty(e.FindAll("#islem-pesin-esik"));
    }

    [Fact]
    public void S23E_12_tur_degisince_bant_iner_satisa_donunce_GERI_GELMEZ()
    {
        satisRed = Govde400(alanli: true);
        var e = Ekran(ArayuzModuServisi.Basit);
        KalemEkleVeKaydet(e);
        e.WaitForElement("#islem-pesin-esik");

        e.Find("#islem-tur-HURDA_ALIS").Click();   // T-S1: ALIS basitte karo değil (PRO) — başka türe geçiş aynı
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-pesin-esik")));
        e.Find("#islem-tur-SATIS").Click();
        e.WaitForElement("#islem-pesin-notu");
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
