using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using EmanetHareket = Kuyumcu.Contracts.Sayfalar.Emanet.HareketVeri;
using SiparisSatir = Kuyumcu.Contracts.Sayfalar.Siparisler.Satir;
using SayimSatir = Kuyumcu.Contracts.Sayfalar.Sayim.Satir;
using FiyatListe = Kuyumcu.Contracts.Sayfalar.Fiyatlandirma.Liste;
using AgacDugum = Kuyumcu.Contracts.Sayfalar.UrunAgaci.KategoriDugum;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5A — Günlük iş + Müşteri ve stok kalan 8 ekranın Yıldız yüzü (tuval Y_Emanet,
// Y_Siparisvetamir, Y_Bildirimler, Y_Urunler, Y_Urunagaci, Y_Etiketler, Y_Fiyatlisteleri, Y_Sayim).
// Görsel eşitliğin asıl kanıtı başsız Chrome tuval kıyasıdır (rapor § 2); bu sınıf sözleşmeyi kilitler:
// kalıp yapısı, her düğmenin gerçek geri çağrısı / kilit sebebi, boş-hata ayrımı, saf satır kuralları,
// CSS ızgaraları (tuvalden AYNEN) ve "öteki 8 şemada özgün ekran aynen" dalı.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class Yildiz5ATestleri : BunitContext
{
    public Yildiz5ATestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private static string Css() =>
        Regex.Replace(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tema-yildiz.css"), @"/\*.*?\*/", "", RegexOptions.Singleline);

    private static string Blok5A()
    {
        var ham = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tema-yildiz.css");
        var a = ham.IndexOf("/* YILDIZ-5A */", StringComparison.Ordinal);
        var b = ham.IndexOf("/* YILDIZ-5A sonu */", StringComparison.Ordinal);
        Assert.True(a >= 0 && b > a, "YILDIZ-5A bloğu yok");
        return Regex.Replace(ham[a..b], @"/\*.*?\*/", "", RegexOptions.Singleline);
    }

    private static string Kaynak(params string[] yol) => CssKaynak.Metin(yol);

    private static YildizListeBaglami ListeBaglami(List<string> cagri, bool ayrinti = false, bool bos = false, bool yukleniyor = false) => new()
    {
        Kok = "yildiz-test",
        Tur = "emanet",
        Ust = "Günlük iş · bırakılan altın, kayıt altında",
        Baslik = "Emanet",
        AramaId = "yildiz-test-arama",
        AramaEtiket = "Müşteri ara",
        AramaYerTutucu = "Müşteri ara",
        AramaDegisti = EventCallback.Factory.Create<string?>(new object(), v => cagri.Add("ara:" + v)),
        B1Etiket = "Emanet al",
        B1Id = "yildiz-test-al",
        B1Tikla = EventCallback.Factory.Create(new object(), () => cagri.Add("b1")),
        SekmelerId = "yildiz-test-sekmeler",
        Sekmeler = [new("hepsi", "Hepsi", true), new("emanette", "Emanette", false), new("geri", "Geri verildi", false)],
        SekmeSec = EventCallback.Factory.Create<string>(new object(), k => cagri.Add("sekme:" + k)),
        Sutunlar = [new("Müşteri"), new("Ne bıraktı"), new("Has", true), new("Ne zaman"), new("Durum")],
        EylemSutunu = true,
        Satirlar = bos ? [] :
        [
            new("h1", [new("Derya Yıldızlı", YildizHucreTuru.Ad, "ad-h1"), new("22 ayar"), new("9,1600 g", YildizHucreTuru.Deger), new("2 Eyl 2026"), new("30 günü aştı")],
                new YildizEylem("Geri ver", "geri-h1", EventCallback.Factory.Create(new object(), () => cagri.Add("geri:h1"))),
                EventCallback.Factory.Create(new object(), () => cagri.Add("ac:h1")), AcEtiketi: "Derya — emanet ayrıntısı"),
            new("h2", [new("Tuna Akarsu", YildizHucreTuru.Ad), new("tamir"), new("2,9312 g", YildizHucreTuru.Deger), new("1 Eki 2026"), new("Geri verildi")],
                new YildizEylem("Geri ver", "geri-h2", KilitSebebi: "Bu emanet geri verildi.")),
        ],
        Yukleniyor = yukleniyor,
        BosMetni = "Emanette altın yok.",
        Not = "Emanet, müşterinin borcuna ya da alacağına karışmaz.",
        AyrintiAcik = ayrinti,
        AyrintiAc = EventCallback.Factory.Create(new object(), () => cagri.Add("ayrinti")),
        AyrintiKapat = EventCallback.Factory.Create(new object(), () => cagri.Add("don")),
    };

    [Fact]
    public void Y5A_01_liste_kalibi_tuval_yapisi_ve_her_dugme_gercek_geri_cagri()
    {
        var cagri = new List<string>();
        var e = Render<YildizListe>(p => p.Add(x => x.B, ListeBaglami(cagri)));
        Assert.Equal("Günlük iş · bırakılan altın, kayıt altında", e.Find("#yildiz-test-ust").TextContent);
        Assert.Contains("yz-ince", e.Find("#yildiz-test-baslik").ClassName);
        Assert.Single(e.FindAll("#yildiz-test-ara #yildiz-test-arama"));
        // Sekme ve sütun başlıkları tuvaldeki büyük harf (Türkçe İ)
        Assert.Equal(["HEPSİ", "EMANETTE", "GERİ VERİLDİ"], e.FindAll("#yildiz-test-sekmeler > button").Select(b => b.TextContent));
        var bas = e.FindAll("#yildiz-test-liste > div:first-child > span");
        Assert.Equal(["MÜŞTERİ", "NE BIRAKTI", "HAS", "NE ZAMAN", "DURUM", ""], bas.Select(s => s.TextContent));
        Assert.Contains("yz-sag", bas[2].ClassName);
        // Satır: ilk hücre satırı açar (19/400 ad), değer hücresi sağa yaslı, eylem .b3
        Assert.Contains("yz-liste-ad", e.Find("#ad-h1").ClassName);
        Assert.Equal("BUTTON", e.Find("#ad-h1").TagName);
        Assert.Contains("yz-sag", e.Find("#yildiz-test-h1 > :nth-child(3)").ClassName);
        e.Find("#yildiz-test-al").Click();
        e.Find("#yildiz-test-sekme-geri").Click();
        e.Find("#ad-h1").Click();
        e.Find("#geri-h1").Click();
        e.Find("#yildiz-test-ayrinti").Click();
        Assert.Equal(["b1", "sekme:geri", "ac:h1", "geri:h1", "ayrinti"], cagri);
        // Kilitli eylem: disabled + sebep (UX-S30)
        var kilit = e.Find("#geri-h2");
        Assert.True(kilit.HasAttribute("disabled"));
        Assert.Equal("Bu emanet geri verildi.", kilit.GetAttribute("title"));
        Assert.Equal("Emanet, müşterinin borcuna ya da alacağına karışmaz.", e.Find("#yildiz-test-not").TextContent);
        // Satır içi style yok (sütunlar CSS'te)
        Assert.DoesNotContain("style=", e.Markup);
    }

    [Fact]
    public void Y5A_02_bos_durum_yalniz_basarili_bos_yanitta_yukleniyor_ayri()
    {
        var bos = Render<YildizListe>(p => p.Add(x => x.B, ListeBaglami([], bos: true)));
        Assert.Equal("Emanette altın yok.", bos.Find("#yildiz-test-bos").TextContent);
        Assert.Empty(bos.FindAll("#yildiz-test-liste"));
        var hata = ListeBaglami([], bos: true);
        var e = Render<YildizListe>(p => p.Add(x => x.B, new YildizListeBaglami { Kok = "yildiz-test", BosMetni = null, Satirlar = [] }));
        Assert.Empty(e.FindAll("#yildiz-test-bos"));   // hata varken boş durum YOK (UX-S28)
        var y = Render<YildizListe>(p => p.Add(x => x.B, ListeBaglami([], bos: true, yukleniyor: true)));
        Assert.Empty(y.FindAll("#yildiz-test-bos"));
        Assert.NotNull(hata);
    }

    [Fact]
    public void Y5A_03_ayrinti_gorunumu_liste_arama_b1_yok_listeye_don_var()
    {
        var cagri = new List<string>();
        var e = Render<YildizListe>(p => p.Add(x => x.B, ListeBaglami(cagri, ayrinti: true)));
        Assert.Contains("yz-liste--ayrinti", e.Find("#yildiz-test").ClassName);
        Assert.Empty(e.FindAll("#yildiz-test-liste"));
        Assert.Empty(e.FindAll("#yildiz-test-arama"));
        Assert.Empty(e.FindAll("#yildiz-test-al"));
        Assert.Empty(e.FindAll("#yildiz-test-sekmeler"));
        e.Find("#yildiz-test-listeye-don").Click();
        Assert.Equal(["don"], cagri);
        Assert.Contains("yz-b2", e.Find("#yildiz-test-listeye-don").ClassName);
    }

    private static EmanetHareket H(string id, string tur, int gunOnce, decimal has, string? storno = null, string? maden = null) =>
        new(id, tur, DateTime.UtcNow.Date.AddHours(9).AddDays(-gunOnce), has, null, storno, null, maden);

    [Fact]
    public void Y5A_04_emanet_satirlari_fifo_storno_esik()
    {
        var bugun = DateOnly.FromDateTime(Bicim.IsletmeBugun);
        var k = Emanet.YildizKayitlari("c", "Derya", [
            H("g1", "EMANET_GIRIS", 40, 2m),
            H("g2", "EMANET_GIRIS", 5, 3m),
            H("g3", "EMANET_GIRIS", 2, 1m),
            H("c1", "EMANET_CIKIS", 1, -2m),
            H("s3", "EMANET_GIRIS", 1, -1m, storno: "g3"),   // g3 geri alındı → satır yok
        ], bugun);
        Assert.Equal(["g1", "g2"], k.Select(x => x.HareketId));
        Assert.Equal(YildizEmanetDurum.GeriVerildi, k[0].Durum);   // en eski giriş çıkışla karşılandı
        Assert.Equal(YildizEmanetDurum.Emanette, k[1].Durum);
        var asan = Emanet.YildizKayitlari("c", "D", [H("g", "EMANET_GIRIS", YildizBugunBicim.EmanetEsikGun + 1, 1m)], bugun);
        Assert.Equal(YildizEmanetDurum.Asti, asan.Single().Durum);
        var esikte = Emanet.YildizKayitlari("c", "D", [H("g", "EMANET_GIRIS", YildizBugunBicim.EmanetEsikGun, 1m)], bugun);
        Assert.Equal(YildizEmanetDurum.Emanette, esikte.Single().Durum);
        // Maden ayrı: gümüş çıkışı altın girişini karşılamaz
        var maden = Emanet.YildizKayitlari("c", "D", [H("a", "EMANET_GIRIS", 3, 1m), H("x", "EMANET_CIKIS", 1, -1m, maden: "GUMUS")], bugun);
        Assert.Equal(YildizEmanetDurum.Emanette, maden.Single().Durum);
        Assert.True(Emanet.YildizSekmeyeUyar(YildizEmanetDurum.Asti, "emanette"));
        Assert.False(Emanet.YildizSekmeyeUyar(YildizEmanetDurum.GeriVerildi, "emanette"));
        Assert.True(Emanet.YildizSekmeyeUyar(YildizEmanetDurum.GeriVerildi, "geri"));
        // Okuma sırası: emaneti sürenler önce (büyükten küçüğe), sonra geri verilmiş olabilecekler
        var sira = Emanet.YildizOkumaSirasi([new("1", "Ali"), new("2", "Can", 3m), new("3", "Ece", 9m)]);
        Assert.Equal(["3", "2", "1"], sira.Select(c => c.Id));
    }

    private static SiparisSatir S(string durum, params string[] gecis) =>
        new("s", "SP-1", "MUSTERI_SIPARISI", durum, "c", "Ali Can", null, null, null, "Zincir kaynak", 1, false, [.. gecis], true, DateTime.UtcNow, DateTime.UtcNow);

    [Fact]
    public void Y5A_05_siparis_sekmeleri_ve_teslim_kilidi()
    {
        Assert.True(Siparisler.YildizSekmeyeUyar("HAZIR", "acik"));
        Assert.True(Siparisler.YildizSekmeyeUyar("TASLAK", "acik"));
        Assert.False(Siparisler.YildizSekmeyeUyar("TESLIM_EDILDI", "acik"));
        Assert.False(Siparisler.YildizSekmeyeUyar("IPTAL", "acik"));
        Assert.True(Siparisler.YildizSekmeyeUyar("HAZIR", "hazir"));
        Assert.False(Siparisler.YildizSekmeyeUyar("ATOLYEDE", "hazir"));
        Assert.True(Siparisler.YildizSekmeyeUyar("TESLIM_EDILDI", "teslim"));
        Assert.True(Siparisler.YildizTeslimAcik(S("HAZIR", "TESLIM_EDILDI", "IPTAL")));
        Assert.False(Siparisler.YildizTeslimAcik(S("ATOLYEDE", "HAZIR")));
        Assert.True(Siparisler.YildizAramayaUyar(S("HAZIR"), "zincir"));
        Assert.True(Siparisler.YildizAramayaUyar(S("HAZIR"), "ali"));
        Assert.False(Siparisler.YildizAramayaUyar(S("HAZIR"), "bilezik"));
    }

    [Fact]
    public void Y5A_06_bildirim_nereden_menu_adindan_yoksa_sistem()
    {
        var dil = Services.GetRequiredService<DilServisi>();
        Assert.Equal(YildizListeBicim.EkranAdi("/raporlar/vade-takibi", dil), Bildirimler.YildizNereden("/raporlar/vade-takibi?x=1", dil));
        Assert.Equal(YildizListeBicim.EkranAdi("/emanet", dil), Bildirimler.YildizNereden("/emanet", dil));
        Assert.Equal("Sistem", Bildirimler.YildizNereden(null, dil));
        Assert.Equal("Sistem", Bildirimler.YildizNereden("/olmayan-ekran", dil));
        Assert.NotEqual("/raporlar/vade-takibi", YildizListeBicim.EkranAdi("/raporlar/vade-takibi", dil));
    }

    [Fact]
    public void Y5A_07_urun_milyem_ve_agac_toplamlari_girintili_alt_grup()
    {
        Assert.Equal("916", Urunler.YildizMilyem("K22"));
        Assert.Equal("995", Urunler.YildizMilyem("K995"));
        Assert.Equal("585", Urunler.YildizMilyem("K14"));
        Assert.Equal("ZZ", Urunler.YildizMilyem("ZZ"));
        var gram = new AgacDugum("g", "Gram", 1, true, 0, null);
        var ceyrek = new AgacDugum("c", "Çeyrek", 2, true, 0, null);
        var sarrafiye = new AgacDugum("s", "Sarrafiye", 1, true, 0, [ceyrek, gram]);
        var yuzuk = new AgacDugum("y", "Yüzük", 2, true, 0, null);
        var urunler = new List<YildizAgacUrun> { new("u1", "g"), new("u2", "c"), new("u3", "s"), new("u4", null) };
        var stok = new Dictionary<string, (decimal, decimal)> { ["u1"] = (2m, 2m), ["u2"] = (3m, 4.8m), ["u3"] = (1m, 1m), ["u4"] = (9m, 9m) };
        var r = UrunAgaci.YildizSatirlar([yuzuk, sarrafiye], urunler, stok);
        Assert.Equal(["s", "s-alt", "y"], r.Select(x => x.Id));
        Assert.Equal(new YildizAgacToplam(3, 6m, 7.8m), r[0].Toplam);         // kök: alt ağacın tamamı
        Assert.Equal(new YildizAgacToplam(2, 5m, 6.8m), r[1].Toplam);         // alt grup satırı: yalnız çocuklar
        Assert.Equal(UrunAgaci.YildizGirinti + "Gram · Çeyrek", r[1].Metin);   // sıra = Sira
        Assert.Null(r[1].Grup);
        Assert.Equal(new YildizAgacToplam(0, 0m, 0m), r[2].Toplam);
        Assert.Equal("—", YildizListeBicim.HasIki(0m, "tr"));
        Assert.Equal("8,66 g", YildizListeBicim.HasIki(8.66m, "tr"));
    }

    [Fact]
    public void Y5A_08_fiyat_kullanim_varsayilan_toptanci_musteri_pasif()
    {
        var cariler = new List<YildizFiyatCari> { new("1", "T", "TOPTAN"), new("2", "T", "TOPTAN"), new("3", "S", null), new("4", null, "TOPTAN") };
        Assert.Equal("Varsayılan", Fiyatlandirma.YildizKullanim(new FiyatListe("P", "Perakende", "PERAKENDE", 6, 8, true, true), cariler, "tr"));
        Assert.Equal("2 toptancı", Fiyatlandirma.YildizKullanim(new FiyatListe("T", "Toptan", "TOPTAN", 2, 4, false, true), cariler, "tr"));
        Assert.Equal("1 müşteri", Fiyatlandirma.YildizKullanim(new FiyatListe("S", "Sadık", "OZEL", 4, 6, false, true), cariler, "tr"));
        Assert.Equal("Kullanılmıyor", Fiyatlandirma.YildizKullanim(new FiyatListe("X", "X", "OZEL", 4, 6, false, true), cariler, "tr"));
        Assert.Equal("Kullanımda değil", Fiyatlandirma.YildizKullanim(new FiyatListe("T", "Toptan", "TOPTAN", 2, 4, false, false), cariler, "tr"));
        Assert.Equal("%8", YildizListeBicim.Yuzde(8m, "tr"));
        Assert.Equal("%2,5", YildizListeBicim.Yuzde(2.5m, "tr"));
    }

    private static SayimSatir SS(string ad, decimal sistem, decimal? sayilan) =>
        new("u-" + ad, null, ad, null, "ADET", sistem, sayilan, sistem, sayilan is { } s ? s - sistem : null, false);

    [Fact]
    public void Y5A_09_sayim_sirasi_fark_hucresi_ve_cumle()
    {
        var sira = Sayim.YildizSirala([SS("A", 2, 2), SS("B", 2, null), SS("C", 2, 1)]).Select(s => s.Ad).ToList();
        Assert.Equal(["C", "B", "A"], sira);   // fark → sayılmayan → eşit (tuval: −1, ?, ?, 0)
        var eksik = Sayim.YildizFarkHucresi(SS("C", 2, 1), "tr");
        Assert.Equal(("−1", YildizHucreTuru.IcKotu), (eksik.Metin, eksik.Tur));
        var bilinmez = Sayim.YildizFarkHucresi(SS("B", 2, null), "tr");
        Assert.Equal(("?", YildizHucreTuru.IcSoluk), (bilinmez.Metin, bilinmez.Tur));
        // Tuval Y_Sayim: dış hücre değer sınıfı (metin rengi), renk İÇ span'da; sağdaki liste en çok 4 satır
        var e = Render<YildizSayim>(p => p.Add(x => x.B, new YildizSayimBaglami
        {
            Rakam = 11, RakamEki = " / 13 ürün", Cumle = "c", Sutunlar = [new("Ürün"), new("Kayıt", true), new("Sayılan", true), new("Fark", true)],
            Satirlar = [new("u", [new("A", YildizHucreTuru.Ad), new("2", YildizHucreTuru.Deger), new("1", YildizHucreTuru.Deger), eksik])],
        }));
        Assert.Equal("yz-liste-d yz-sag", e.Find("#yildiz-sayim-u > :nth-child(4)").ClassName);
        Assert.Equal("yz-kotu", e.Find("#yildiz-sayim-u > :nth-child(4) > span").ClassName);
        Assert.Equal("11 / 13 ürün", e.Find("#yildiz-sayim-rakam").TextContent);
        Assert.Equal(4, Sayim.YildizSatirSiniri);
        Assert.Equal("+1", Sayim.YildizFarkHucresi(SS("D", 1, 2), "tr").Metin);
        Assert.Equal("0", Sayim.YildizFarkHucresi(SS("A", 2, 2), "tr").Metin);
        Assert.Equal("Sayım sürüyor. İki ürün henüz okutulmadı; bir üründe adet farkı var.", YildizSayimSozlugu.Cumle("ACIK", 2, 1, "tr"));
        Assert.Equal("Sayım sürüyor. Bütün ürünler okutuldu; adet farkı yok.", YildizSayimSozlugu.Cumle("ACIK", 0, 0, "tr"));
        Assert.Equal("Sayım kapandı.", YildizSayimSozlugu.Cumle("KAPANDI", 2, 1, "tr"));
        Assert.Equal("−3,21 g", YildizListeBicim.HasFark(-3.21m, "tr"));
    }

    private YildizEtiketBaglami EtiketBaglami(List<string> cagri, int secili = 1, string? kilit = null, string? adetHata = null)
    {
        var u = new List<YildizEtiketUrunu> { new("u1", "22 Ayar Burma Bilezik", "8698256000046", "916", "15,20"), new("u2", "925 Gümüş Kolye", null, "925", "12,00") };
        return new YildizEtiketBaglami
        {
            Ust = "Müşteri ve stok · barkodlu ürün etiketi", Baslik = "Etiketler",
            Urunler = u, Secili = [.. u.Take(secili)], Kopya = 2, KopyaEnCok = 100, KopyaHata = adetHata,
            Ekle = EventCallback.Factory.Create<string>(new object(), id => cagri.Add("ekle:" + id)),
            Cikar = EventCallback.Factory.Create<string>(new object(), id => cagri.Add("cikar:" + id)),
            KopyaGirildi = EventCallback.Factory.Create<decimal?>(new object(), v => cagri.Add("adet:" + v)),
            Yazdir = EventCallback.Factory.Create(new object(), () => cagri.Add("yazdir")),
            YazdirKilit = kilit,
            Vazgec = EventCallback.Factory.Create(new object(), () => cagri.Add("vazgec")),
        };
    }

    [Fact]
    public void Y5A_10_etiketler_form_kalibi_kilitler_ve_secim()
    {
        var cagri = new List<string>();
        var e = Render<YildizEtiketler>(p => p.Add(x => x.B, EtiketBaglami(cagri)));
        Assert.Contains("yazdirma-gizle", e.Find("#yildiz-etiket").ClassName);   // kâğıda bu bileşen basılmaz
        Assert.Equal("22 Ayar Burma Bilezik · 8698256000046", e.Find("#yildiz-etiket-urun-deger").TextContent);
        Assert.Equal(["ÜRÜN", "ADET", "ETİKET BOYU", "ETİKETTE NE YAZSIN"], e.FindAll(".yz-k-etiket").Take(4).Select(x => x.TextContent.Trim()));
        Assert.Equal("50 × 30 mm", e.Find("#yildiz-etiket-boy").TextContent);
        Assert.Equal("Hayır", e.Find("#yildiz-etiket-fiyat").TextContent);
        Assert.Contains("22 Ayar Burma Bilezik", e.Find("#yildiz-etiket-kutu").TextContent);
        Assert.Contains("916 · 15,20 g", e.Find("#yildiz-etiket-kutu").TextContent);
        e.Find("#yildiz-etiket-yazdir").Click();
        e.Find("#yildiz-etiket-vazgec").Click();
        e.Find("#yildiz-etiket-adet").Change("3");
        e.Find("#yildiz-etiket-baska").Click();
        e.Find("#yildiz-etiket-sec-u2").Click();
        e.Find("#yildiz-etiket-sec-u1").Click();
        Assert.Equal(["yazdir", "vazgec", "adet:3", "ekle:u2", "cikar:u1"], cagri);
        // Kilit: Yazdır disabled + sebep; seçim yokken Vazgeç disabled + sebep; adet hatası alan altında
        var k = Render<YildizEtiketler>(p => p.Add(x => x.B, EtiketBaglami([], secili: 0, kilit: "Önce ürün seçin.", adetHata: "Sıfırdan büyük olmalı.")));
        Assert.True(k.Find("#yildiz-etiket-yazdir").HasAttribute("disabled"));
        Assert.Equal("Önce ürün seçin.", k.Find("#yildiz-etiket-yazdir-kilit").TextContent);
        Assert.True(k.Find("#yildiz-etiket-vazgec").HasAttribute("disabled"));
        Assert.Equal("Seçili ürün yok.", k.Find("#yildiz-etiket-vazgec").GetAttribute("title"));
        Assert.Equal("Sıfırdan büyük olmalı.", k.Find("#yildiz-etiket-adet-hata").TextContent);
        Assert.Equal("Ürün seçin", k.Find("#yildiz-etiket-urun-deger").TextContent);
        Assert.Empty(k.FindAll("#yildiz-etiket-kutu"));
    }

    [Fact]
    public void Y5A_11_css_izgaralar_tuvalden_aynen_ve_kapsam()
    {
        var c = Css();
        Assert.Contains(".yz-liste--emanet .yz-liste-sat { grid-template-columns: minmax(0, 1.2fr) minmax(0, 1fr) 120px 160px 150px 140px; }", c);
        Assert.Contains(".yz-liste--siparis .yz-liste-sat { grid-template-columns: minmax(0, 1.3fr) minmax(0, 1fr) 160px 150px 140px; }", c);
        Assert.Contains(".yz-liste--bildirim .yz-liste-sat { grid-template-columns: minmax(0, 1.6fr) 160px 160px 140px; }", c);
        Assert.Contains(".yz-liste--urun .yz-liste-sat { grid-template-columns: minmax(0, 1.4fr) 170px 90px 110px 90px 150px; }", c);
        Assert.Contains(".yz-liste--agac .yz-liste-sat { grid-template-columns: minmax(0, 1.6fr) 110px 110px 150px; }", c);
        Assert.Contains(".yz-liste--fiyat .yz-liste-sat { grid-template-columns: minmax(0, 1.4fr) 130px 110px 110px 170px 140px; }", c);
        Assert.Contains(".yz-sayim .yz-liste-sat { grid-template-columns: minmax(0, 1.4fr) 100px 100px 100px; }", c);
        Assert.Contains(".yz-etiket .yz-etiket-ana { flex-grow: 1; min-height: 0; display: grid; grid-template-columns: minmax(0, 1fr) 420px; gap: 96px; padding: 48px 64px 40px; }", c);
        Assert.Contains(".yz-liste .yz-liste-ana { flex-grow: 1; min-height: 0; display: flex; flex-direction: column; gap: 32px; padding: 48px 64px 40px; }", c);
        Assert.Contains(".yz .yz-liste-ara { width: 420px; height: 56px; flex-shrink: 0; }", c);
        Assert.Contains(".yz .yz-liste-ad { font-size: 19px; font-weight: 400; min-width: 0; }", c);
        Assert.Contains(".yz .yz-liste-d { font-size: 17px; font-weight: 300; min-width: 0; }", c);
        Assert.Contains(".yz .yz-liste-bas { letter-spacing: .14em; }", c);
        // Dar ekran (≤1000 px): satır akar, sütun başlığı satırı ÇİZİLMEZ (akış kuralından SONRA gelir — aynı özgüllük)
        var akis = c.IndexOf(".yz .yz-liste-tablo > .yz-liste-sat { display: flex; flex-wrap: wrap;", StringComparison.Ordinal);
        var basGizli = c.IndexOf(".yz .yz-liste-tablo > .yz-liste-sat.yz-liste-sat--bas { display: none; }", StringComparison.Ordinal);
        Assert.True(akis > 0 && basGizli > akis, "dar ekranda başlık satırı gizleme kuralı akış kuralından sonra olmalı");
        // Blok: her kural Yıldız kapsamlı, sabit renk yok
        var blok = Blok5A();
        foreach (Match m in Regex.Matches(blok, @"(?:^|[};])\s*([^{}@;]+)\{"))
        {
            var sec = m.Groups[1].Value.Trim();
            if (sec.Length == 0 || sec.StartsWith("@")) continue;
            foreach (var p in ParantezDuyarliBol(sec))
                Assert.True(p.Contains("[data-sema=\"yildiz\"]"), "kapsamsız seçici: " + p.Trim());
        }
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgba?\(", blok);
        // Özgün etiket ekranı YALNIZ ekranda gizli (kâğıt muaf)
        Assert.Matches(@"@media screen \{\s*html:root\[data-sema=""yildiz""\] \[data-yz-etiket=""kapali""\] \{ display: none !important; \}", blok);
    }

    private static IEnumerable<string> ParantezDuyarliBol(string s)
    {
        int d = 0, bas = 0;
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == '(') d++; else if (s[i] == ')') d--;
            else if (s[i] == ',' && d == 0) { yield return s[bas..i]; bas = i + 1; }
        }
        yield return s[bas..];
    }

    [Fact]
    public void Y5A_12_sayfalar_Yildiz_yuzunu_kosulsuz_cizer_ozgun_govde_Tum_ayrintilar_arkasinda()
    {
        // Tema temizliği (17.10): şema koşulu (YildizMi) kalktı. Yıldız bileşeni KOŞULSUZ ve özgün kökten ÖNCE;
        // özgün gövde CANLIDIR ama yalnız "Tüm ayrıntılar" açılınca çizilir (YildizOzgunCiz => yildizAyrinti).
        foreach (var (sayfa, isaret) in new[] {
            ("Emanet", "YildizOzgunIsaret"), ("Siparisler", "YildizOzgunIsaret"), ("Bildirimler", "YildizOzgunIsaret"),
            ("Urunler", "YildizOzgunIsaret"), ("UrunAgaci", "YildizOzgunIsaret"), ("Fiyatlandirma", "YildizOzgunIsaret"),
            ("Sayim", "YildizOzgunIsaret"), ("UrunEtiketleri", "YildizEtiketIsaret") })
        {
            var razor = Kaynak("Kuyumcu.Web", "Pages", sayfa + ".razor");
            Assert.DoesNotMatch(@"\bYildizMi\b", razor);
            var bilesen = razor.IndexOf("<Kuyumcu.Web.Bilesenler.Yildiz.Yildiz", StringComparison.Ordinal);
            Assert.True(bilesen > 0, sayfa + ": Yıldız bileşeni yok");
            if (sayfa != "UrunEtiketleri") Assert.True(bilesen < razor.IndexOf("@if (YildizOzgunCiz) {", StringComparison.Ordinal), sayfa + ": Yıldız yüzü özgün kökten sonra");
            Assert.Contains($"data-yz-{(sayfa == "UrunEtiketleri" ? "etiket" : "ozgun")}=\"@{isaret}\"", razor);
            var cs = Kaynak("Kuyumcu.Web", "Pages", sayfa + ".Yildiz.cs");
            Assert.DoesNotMatch(@"\bYildizMi\b", cs);
            if (sayfa != "UrunEtiketleri")
            {
                Assert.Contains("@if (YildizOzgunCiz) {", razor);
                Assert.Contains("private bool YildizOzgunCiz => yildizAyrinti;", cs);
                Assert.Matches($@"private string\? {isaret} => ""[a-z-]+"";", cs);
            }
            else Assert.Contains("private string? YildizEtiketIsaret => (yildizAyrinti ? \"acik\" : \"kapali\");", cs);
        }
    }

    [Fact]
    public void Y5A_13_bicimler_tuval_yazimi()
    {
        Assert.Equal("9,1600 g", YildizListeBicim.HasDort(9.16m, "tr"));
        Assert.Equal("15,20", YildizListeBicim.Gram(15.2m, "tr"));
        Assert.Equal("6.922\u00a0₺", YildizListeBicim.Lira(6921.6m));   // ortak Bicim.TlTam: ₺ sayıdan ayrılmaz (bölünmez boşluk)
        Assert.Equal("2", YildizListeBicim.Adet(2m, "tr"));
        Assert.Equal("−1", YildizListeBicim.Fark(-1m, "tr"));
        Assert.Equal("2 Eyl 2026", YildizListeBicim.Gun(new DateTime(2026, 9, 2, 9, 0, 0, DateTimeKind.Utc), "tr"));
        var bugun = new DateOnly(2026, 10, 10);
        Assert.Equal("Bugün", YildizListeBicim.KisaGun(bugun, bugun, "tr"));
        Assert.Equal("14 Eki", YildizListeBicim.KisaGun(new DateOnly(2026, 10, 14), bugun, "tr"));
        Assert.Equal("GERİ VERİLDİ", YildizListeBicim.Buyuk("Geri verildi", "tr"));
        Assert.Equal("İlk 10 müşterinin emaneti gösteriliyor; ötekiler için müşteri seçin.", YildizEmanetSozlugu.Sinir(10, "tr"));
    }
}
