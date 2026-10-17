using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Core.Sozlesmeler;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using KasaHareket = Kuyumcu.Contracts.Sayfalar.Kasalar.HareketVeri;
using KurKaydi = Kuyumcu.Contracts.Sayfalar.Kur.KurVeri;
using HurdaSatiri = Kuyumcu.Contracts.Sayfalar.Hurda.HurdaDefterSatiri;
using EvrakTakvimi = Kuyumcu.Web.Pages.EvrakTakvimi;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5B — Para ve banka 12 ekranın Yıldız yüzü (tuval Y_Kasalar, Y_Kasakuyrugu,
// Y_POScihazlari, Y_POSalacaklari, Y_Banka, Y_Cekvesenet, Y_Vadetakvimi, Y_Kur, Y_Gidervevirman,
// Y_Hurdaveramat, Y_Kasaraporu, Y_Giderpusulalari). Görsel eşitliğin asıl kanıtı başsız Chrome tuval
// kıyasıdır (rapor § 2); bu sınıf sözleşmeyi kilitler: kalıp yapısı (özet · boş · takvim · form),
// her düğmenin gerçek geri çağrısı ya da kilit sebebi, para biçimi (kuruş/₺ ayrı parça), saf satır
// kuralları, CSS ızgaraları (tuvalden AYNEN) ve "öteki 8 şemada özgün ekran aynen" dalı.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class Yildiz5BTestleri : BunitContext
{
    public Yildiz5BTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private static readonly DateOnly Bugun = new(2026, 10, 10);
    private static DateTime Oglen(int gun, int ay = 10) => new(2026, ay, gun, 9, 0, 0, DateTimeKind.Utc);

    private static string Css() =>
        Regex.Replace(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tema-yildiz.css"), @"/\*.*?\*/", "", RegexOptions.Singleline);

    private static string Blok5B()
    {
        var ham = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tema-yildiz.css");
        var a = ham.IndexOf("/* YILDIZ-5B */", StringComparison.Ordinal);
        var b = ham.IndexOf("/* YILDIZ-5B sonu */", StringComparison.Ordinal);
        Assert.True(a >= 0 && b > a, "YILDIZ-5B bloğu yok");
        return Regex.Replace(ham[a..b], @"/\*.*?\*/", "", RegexOptions.Singleline);
    }

    private static YildizOzetBaglami OzetBaglami(List<string> cagri, string? b1Kilit = null, bool ayrinti = false) => new()
    {
        Kok = "yildiz-t", Tur = "kasalar",
        Ust = "Para ve banka · Ana Kasa · Türk lirası", Baslik = "Kasalar",
        Rakam = "318.916", RakamEki = ",43 ₺",
        Cumle = "Defter bakiyesi.",
        B1Etiket = "Günü kapat", B1Id = "yildiz-t-b1", B1Kilit = b1Kilit,
        B1Tikla = EventCallback.Factory.Create(new object(), () => cagri.Add("b1")),
        B2Etiket = "Hareket ekle", B2Id = "yildiz-t-b2",
        B2Tikla = EventCallback.Factory.Create(new object(), () => cagri.Add("b2")),
        Sutunlar = [new("Ne"), new("İşlem", true), new("Tutar", true)],
        EylemSutunu = true,
        Satirlar =
        [
            new("nakit", [new("Nakit", YildizHucreTuru.Ad, "ad-nakit"), new("12", YildizHucreTuru.Deger), new("186.030,86 ₺", YildizHucreTuru.Deger)],
                new YildizEylem("Geçmiş", "gecmis-nakit", EventCallback.Factory.Create(new object(), () => cagri.Add("gecmis")))),
        ],
        Bilgi = [("Son kapanış", "dün"), ("Bugün giren", "+102.781,77 ₺")],
        AyrintiAc = EventCallback.Factory.Create(new object(), () => cagri.Add("ayrinti")),
        AyrintiAcik = ayrinti,
        AyrintiKapat = EventCallback.Factory.Create(new object(), () => cagri.Add("don")),
        DonEtiketi = "Özete dön",
    };

    [Fact]
    public void Y5B_01_ozet_kalibi_tuval_yapisi_ve_her_dugme_gercek_geri_cagri()
    {
        var cagri = new List<string>();
        var e = Render<YildizOzet>(p => p.Add(x => x.B, OzetBaglami(cagri)));
        Assert.Equal("Para ve banka · Ana Kasa · Türk lirası", e.Find("#yildiz-t-ust").TextContent);
        Assert.Contains("yz-ozet-h1", e.Find("#yildiz-t-baslik").ClassName);
        // Kuruş ve ₺ AYRI parça (tuval: ",43 ₺" soluk + yarı boy)
        var rakam = e.Find("#yildiz-t-rakam");
        Assert.StartsWith("318.916", rakam.TextContent);
        Assert.Equal(",43 ₺", e.Find("#yildiz-t-rakam-ek").TextContent);
        Assert.Contains("yz-s", e.Find("#yildiz-t-rakam-ek").ClassName);
        Assert.Equal(["NE", "İŞLEM", "TUTAR"], e.FindAll("#yildiz-t-liste > div:first-child > span").Take(3).Select(s => s.TextContent));
        Assert.Equal(["SON KAPANIŞ dün", "BUGÜN GİREN +102.781,77 ₺"], e.FindAll("#yildiz-t-bilgi > span").Select(s => s.TextContent));
        e.Find("#yildiz-t-b1").Click();
        e.Find("#yildiz-t-b2").Click();
        e.Find("#gecmis-nakit").Click();
        e.Find("#yildiz-t-ayrinti").Click();
        Assert.Equal(["b1", "b2", "gecmis", "ayrinti"], cagri);
        // Kilitli b1: disabled + sebep title ve altında yazılı (UX-S30)
        var k = Render<YildizOzet>(p => p.Add(x => x.B, OzetBaglami([], b1Kilit: "Bugünün kasası kapandı.")));
        Assert.True(k.Find("#yildiz-t-b1").HasAttribute("disabled"));
        Assert.Equal("Bugünün kasası kapandı.", k.Find("#yildiz-t-b1-kilit").TextContent);
    }

    [Fact]
    public void Y5B_02_ozet_ayrinti_gorunumu_yalniz_baslik_ve_don()
    {
        var cagri = new List<string>();
        var e = Render<YildizOzet>(p => p.Add(x => x.B, OzetBaglami(cagri, ayrinti: true)));
        Assert.Empty(e.FindAll("#yildiz-t-rakam"));
        Assert.Empty(e.FindAll("#yildiz-t-b1"));
        Assert.Contains("yz-liste--ayrinti", e.Find("#yildiz-t").ClassName);
        e.Find("#yildiz-t-don").Click();
        Assert.Equal(["don"], cagri);
        // B2Href doluysa ikincil eylem gezinmedir (a.yz-b2)
        var h = Render<YildizOzet>(p => p.Add(x => x.B, new YildizOzetBaglami { Kok = "yildiz-h", B2Etiket = "Hurda al", B2Id = "yildiz-h-b2", B2Href = "/islemler?tur=HURDA_ALIS" }));
        Assert.Equal("/islemler?tur=HURDA_ALIS", h.Find("a#yildiz-h-b2").GetAttribute("href"));
    }

    [Fact]
    public void Y5B_03_bos_durum_kalibi_cizgi_cumle_aciklama_ve_tek_hap()
    {
        var cagri = new List<string>();
        var e = Render<YildizBos>(p => p.Add(x => x.B, new YildizBosBaglami
        {
            Kok = "yildiz-b", Ust = YildizBankaSozlugu.Ust("tr"), Baslik = "Banka",
            BosBaslik = YildizBankaSozlugu.BosBaslik("tr"), BosMetin = YildizBankaSozlugu.BosMetin("tr"),
            B1Etiket = YildizBankaSozlugu.HesapEkle("tr"), B1Id = "yildiz-b-b1",
            B1Tikla = EventCallback.Factory.Create(new object(), () => cagri.Add("ekle")),
        }));
        Assert.Single(e.FindAll("#yildiz-b .yz-bosekran-cizgi"));
        Assert.Equal("Banka hesabı yok", e.Find("#yildiz-b-bos-cumle").TextContent);
        Assert.Equal("Havale ve kart tahsilatlarının gideceği hesabı ekle; hareketler burada görünür.", e.Find("#yildiz-b-bos-aciklama").TextContent);
        Assert.Contains("yz-ince", e.Find("#yildiz-b-bos-cumle").ClassName);
        e.Find("#yildiz-b-b1").Click();
        Assert.Equal(["ekle"], cagri);
        Assert.Empty(e.FindAll("#yildiz-b-ayrinti"));   // ayrıntı yolu yoksa çizilmez (ölü düğme yok)
    }

    [Fact]
    public void Y5B_04_takvim_kalibi_bosluk_vadeli_gun_baglanti_ve_liste()
    {
        var e = Render<YildizTakvim>(p => p.Add(x => x.B, new YildizTakvimBaglami
        {
            Kok = "yildiz-v", Ust = "Para ve banka · hangi gün kimden ne", Baslik = "Vade takvimi",
            AyAdi = "Ekim 2026", GunAdlari = YildizVadeSozlugu.GunAdlari("tr"),
            Bosluk = EvrakTakvimi.YildizBosluk(new DateOnly(2026, 10, 1)), GunSayisi = 31,
            VadeliGunler = new Dictionary<int, string> { [23] = "/evraklar?vadeBas=2026-10-23&vadeBit=2026-10-23" },
            ListeBaslik = "Bu ay",
            Sutunlar = [new("Gün"), new("Kim"), new("Tutar", true)],
            Satirlar = [new("e1", [new("23", YildizHucreTuru.Ad), new("Tuna Akarsu"), new("22.174,73 ₺", YildizHucreTuru.Deger)])],
        }));
        Assert.Equal(3, e.FindAll("#yildiz-v-izgara > span[aria-hidden]").Count);   // 1 Ekim 2026 Perşembe
        Assert.Equal(31, e.FindAll("#yildiz-v-izgara > .yz-gun").Count);
        var v = e.Find("#yildiz-v-gun-23");
        Assert.Equal("A", v.TagName);
        Assert.Contains("yz-gun--v", v.ClassName);
        Assert.Equal("/evraklar?vadeBas=2026-10-23&vadeBit=2026-10-23", v.GetAttribute("href"));
        Assert.Equal("SPAN", e.Find("#yildiz-v-gun-22").TagName);
        Assert.Equal("PZT SAL ÇAR PER CUM CMT PAZ", e.Find("#yildiz-v .yz-takvim-gunler").TextContent);
        Assert.Equal(["GÜN", "KİM", "TUTAR"], e.FindAll("#yildiz-v-liste > div:first-child > span").Select(s => s.TextContent));
    }

    [Fact]
    public void Y5B_05_form_kalibi_secim_alanlar_kilit_ve_ayrinti()
    {
        var cagri = new List<string>();
        YildizFormBaglami B(bool kilit, bool ayrinti) => new()
        {
            Kok = "yildiz-f", Ust = YildizGiderSozlugu.Ust("tr"), Baslik = "Gider ve virman",
            SecimEtiketi = YildizGiderSozlugu.NeYapiyorsun("tr"),
            Secimler =
            [
                ("gider", "Gider", true, EventCallback.Factory.Create(new object(), () => cagri.Add("gider"))),
                ("virman", "Virman", false, EventCallback.Factory.Create(new object(), () => cagri.Add("virman"))),
            ],
            EkleEtiketi = YildizGiderSozlugu.AyrintiEkle("tr"), EkleTikla = EventCallback.Factory.Create(new object(), () => cagri.Add("ekle")),
            B1Etiket = "Kaydet", B1Id = "yildiz-f-kaydet", B1Kilitli = kilit, B1Kilit = kilit ? "Tutar girin." : null,
            B1Tikla = EventCallback.Factory.Create(new object(), () => cagri.Add("kaydet")),
            B2Etiket = "Vazgeç", B2Id = "yildiz-f-vazgec", B2Tikla = EventCallback.Factory.Create(new object(), () => cagri.Add("vazgec")),
            NotBaslik = "Not", Not = YildizGiderSozlugu.NotMetni("tr"), AyrintiAcik = ayrinti, DonEtiketi = "Özete dön",
        };
        RenderFragment alan = b => { b.OpenElement(0, "div"); b.AddAttribute(1, "id", "gd-tutar"); b.CloseElement(); };
        var e = Render<YildizForm>(p => p.Add(x => x.B, B(false, false)).Add(x => x.ChildContent, alan));
        Assert.Equal("NE YAPIYORSUN", e.Find("#yildiz-f-secim .yz-k-etiket").TextContent);
        Assert.Equal("true", e.Find("#yildiz-f-secim-gider").GetAttribute("aria-pressed"));
        Assert.Single(e.FindAll("#yildiz-f-satirlar.yz-duzen #gd-tutar"));   // sayfanın MEVCUT alanı .yz-duzen içinde
        e.Find("#yildiz-f-secim-virman").Click();
        e.Find("#yildiz-f-ekle").Click();
        e.Find("#yildiz-f-kaydet").Click();
        e.Find("#yildiz-f-vazgec").Click();
        Assert.Equal(["virman", "ekle", "kaydet", "vazgec"], cagri);
        var k = Render<YildizForm>(p => p.Add(x => x.B, B(true, false)).Add(x => x.ChildContent, alan));
        Assert.True(k.Find("#yildiz-f-kaydet").HasAttribute("disabled"));
        Assert.Equal("Tutar girin.", k.Find("#yildiz-f-kaydet").GetAttribute("title"));
        // Ayrıntı görünümünde alanlar ÇİZİLMEZ (özgün form tek kimlik sahibi)
        var a = Render<YildizForm>(p => p.Add(x => x.B, B(false, true)).Add(x => x.ChildContent, alan));
        Assert.Empty(a.FindAll("#gd-tutar"));
        Assert.Single(a.FindAll("#yildiz-f-don"));
    }

    [Fact]
    public void Y5B_06_para_bicimi_kurus_ve_isaret_ayri_parca()
    {
        Assert.Equal(("318.916", ",43 ₺"), YildizParaBicim.BuyukPara(318916.43m, "TRY"));
        Assert.Equal(("+79.623", ",18 ₺"), YildizParaBicim.BuyukPara(79623.18m, "TRY", artiIsareti: true));
        Assert.Equal(("−1.250", ",00 ₺"), YildizParaBicim.BuyukPara(-1250m, "TRY"));
        Assert.Equal(("1.250", ",00 $"), YildizParaBicim.BuyukPara(1250m, "USD"));
        Assert.Equal("25.024,00 ₺", YildizParaBicim.Tutar(25024m, "TRY"));   // tuval: kuruş HER ZAMAN iki hane
        Assert.Equal("−30.028,39 ₺", YildizParaBicim.Tutar(-30028.39m, "TRY"));
        Assert.Equal("+102.781,77 ₺", YildizParaBicim.Tutar(102781.77m, "TRY", artiIsareti: true));
        Assert.Equal("6.656 ₺", Kur.YildizLira(6655.6m));
        Assert.Equal("49,29 ₺", Kur.YildizDoviz(49.29m));
        Assert.Equal("—", Kur.YildizLira(null));
    }

    [Fact]
    public void Y5B_07_kasalar_ayrim_sayi_siniri_ve_son_kapanis()
    {
        Assert.Equal("KART", Kasalar.YildizArac("KART"));
        Assert.Equal("HAVALE", Kasalar.YildizArac("HAVALE"));
        Assert.Equal("NAKIT", Kasalar.YildizArac("NAKIT"));
        Assert.Equal("NAKIT", Kasalar.YildizArac("DOVIZ"));   // sunucu KasaNakitAyrimi: kart/havale dışı her şey nakit
        Assert.Equal("12", Kasalar.YildizSayi(12, false, "tr"));
        Assert.Equal("1000+", Kasalar.YildizSayi(1000, true, "tr"));
        Assert.Equal("yok", Kasalar.YildizKapanisMetni(null, Bugun, "tr"));
        Assert.Equal("dün", Kasalar.YildizKapanisMetni(Bugun.AddDays(-1), Bugun, "tr"));
        Assert.Equal("bugün", Kasalar.YildizKapanisMetni(Bugun, Bugun, "tr"));
        Assert.Equal("8 Eki", Kasalar.YildizKapanisMetni(new DateOnly(2026, 10, 8), Bugun, "tr"));
    }

    [Fact]
    public void Y5B_08_kasa_raporu_gruplar_aralik_ve_gun_sayisi_yaziyla()
    {
        var h = new List<KasaHareket>
        {
            new("1", 100m, "NAKIT", Oglen(1), null, null, null),
            new("2", 50m, "NAKIT", Oglen(2), null, null, null),
            new("3", -30m, "NAKIT", Oglen(2), null, null, null),
            new("4", 99m, "KART", Oglen(3), null, null, null),
            new("5", 33m, "HAVALE", Oglen(3), null, null, null),
        };
        var g = KasaRaporu.YildizGruplar(h);
        Assert.Equal(("nakit-giris", 2, 150m), g[0]);
        Assert.Equal(("kart", 1, 99m), g[1]);
        Assert.Equal(("havale", 1, 33m), g[2]);
        Assert.Equal(("nakit-cikis", 1, -30m), g[3]);
        Assert.Equal("1–8 Ekim", KasaRaporu.YildizAralik(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 8), "tr"));
        Assert.Equal("28 Eyl – 8 Eki", KasaRaporu.YildizAralik(new DateOnly(2026, 9, 28), new DateOnly(2026, 10, 8), "tr"));
        Assert.Equal("Sekiz", YildizKasaRaporuSozlugu.GunSayisi(8, "tr"));
        Assert.Equal("On dört", YildizKasaRaporuSozlugu.GunSayisi(14, "tr"));
        Assert.StartsWith("Sekiz günde kasaya giren ile çıkanın farkı.", YildizKasaRaporuSozlugu.Cumle(8, "tr"));
    }

    private static VezneKuyrukSatiri Fis(string tur, int kalem, DateTime? veznelendi) =>
        new("f1", "VEZNEDE", tur, kalem > 0, 25024m, "c1", "Selin Ergüven", null, null, null, kalem, null, false, false,
            Oglen(10), veznelendi, Oglen(10));

    private static EvrakSatiri Evrak(string id, string tur, string yon, string durum, string? banka, string? kaynak, string? borclu, int gun, params string[] gecis) =>
        new(id, tur, yon, durum, 1000m, "TRY", new DateOnly(2026, 10, gun), "N" + id, banka, null, null, null, null, borclu, null, kaynak, null,
            0, false, false, true, gecis);

    [Fact]
    public void Y5B_09_liste_ekranlari_saf_satir_kurallari()
    {
        // Kasa kuyruğu: "Satış · 2 ürün" · "Tahsilat" · bekleme dakika
        Assert.Equal("Satış · 2 ürün", Vezne.YildizFisAdi(Fis("SATIS", 2, null), "tr"));
        Assert.Equal("Tahsilat", Vezne.YildizFisAdi(Fis("TAHSILAT", 0, null), "tr"));
        Assert.Equal("4 dk", Vezne.YildizBekleme(Fis("SATIS", 1, Oglen(10)), Oglen(10).AddMinutes(4), "tr"));
        // Çek ve senet: belge adı, kimden, sekme + arama süzgeci (Türkçe büyük/küçük)
        Assert.Equal("Çek · Ziraat", Evraklar.YildizBelge("Çek", "Ziraat"));
        Assert.Equal("Senet", Evraklar.YildizBelge("Senet", null));
        var l = new[]
        {
            Evrak("a", "CEK", "ALINAN", "PORTFOYDE", "Ziraat", "Tuna Akarsu", null, 23, "TAHSIL_EDILDI"),
            Evrak("b", "SENET", "ALINAN", "PORTFOYDE", null, null, "Kemal Ardıç", 30),
            Evrak("c", "CEK", "VERILEN", "CIRO_EDILDI", "İş Bankası", "Anadolu Döküm", null, 15),
        };
        Assert.Equal("Kemal Ardıç", Evraklar.YildizKimden(l[1]));
        Assert.Equal(["c"], Evraklar.YildizSuz(l, "VERILEN", null).Select(e => e.Id));
        Assert.Equal(["a", "b"], Evraklar.YildizSuz(l, "ALINAN", null).Select(e => e.Id));
        Assert.Equal(["c"], Evraklar.YildizSuz(l, "", "iş bankası").Select(e => e.Id));
        Assert.Equal(["b"], Evraklar.YildizSuz(l, "", "KEMAL").Select(e => e.Id));
        // Vade takvimi: verilen evrak bizim ödememiz (eksi, kırmızı); tahsil edilen gösterilmez; pazartesi başlangıç
        Assert.Equal((("−" + Bicim.Para(1000m, "TRY")), true), EvrakTakvimi.YildizTutar(l[2]));
        Assert.Equal((Bicim.Para(1000m, "TRY"), false), EvrakTakvimi.YildizTutar(l[0]));
        Assert.False(EvrakTakvimi.YildizGoster(l[0] with { Durum = "TAHSIL_EDILDI" }));
        Assert.Equal(3, EvrakTakvimi.YildizBosluk(new DateOnly(2026, 10, 1)));
        Assert.Equal(0, EvrakTakvimi.YildizBosluk(new DateOnly(2026, 6, 1)));
        Assert.Equal((new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)), EvrakTakvimi.YildizAy(new DateOnly(2026, 2, 14)));
        // POS alacakları: yalnız bankaya geçmeyi bekleyen
        PosTahsilatSatir Pos(string durum, decimal kalan) => new("p", "c", "Ziraat POS", "Ziraat", 100m, 0.0175m, 1.75m, 98.25m, 0m, kalan, 1, "TRY", Oglen(9), Oglen(10), durum, null);
        Assert.True(PosTahsilatlar.YildizBekliyor(Pos("BEKLIYOR", 98.25m)));
        Assert.True(PosTahsilatlar.YildizBekliyor(Pos("KISMI", 10m)));
        Assert.False(PosTahsilatlar.YildizBekliyor(Pos("ESLESTI", 0m)));
        Assert.False(PosTahsilatlar.YildizBekliyor(Pos("IPTAL", 98.25m)));
        // Gider pusulası: "14 ayar · 6,0 g"; satıcı süzgeci
        Assert.Equal("14 ayar · 6,0 g", GiderPusulalari.YildizMal("14 Ayar", 6m, "tr"));
        // Hurda: giriş = satış ve tahlil farkı dışı, stornolu değil
        HurdaSatiri H(string tur, bool storno = false) => new("h", tur, Oglen(1), 0m, 3.51m, "ALTIN", "Altın", null, null, "TRY", false, storno, null, 0m);
        Assert.True(Hurda.YildizGirisMi(H("HURDA_ALIS")));
        Assert.False(Hurda.YildizGirisMi(H("HURDA_SATIS")));
        Assert.False(Hurda.YildizGirisMi(H("TAHLIL_FARKI")));
        Assert.False(Hurda.YildizGirisMi(H("HURDA_ALIS", storno: true)));
        // Kur: gün farkı önceki işletme gününün son geçerli kaydına göre
        KurKaydi K(decimal has, DateTime an, bool iptal = false) => new("k", has, null, null, null, null, "ALTINKAYNAK", an, null, iptal);
        Assert.Equal("▲ 0,3%", Kur.YildizGunFarki(K(6656m, Oglen(10)), [K(6636m, Oglen(9)), K(6000m, Oglen(9).AddHours(1), iptal: true)], "tr"));
        Assert.Equal("▼ 1,0%", Kur.YildizGunFarki(K(6600m, Oglen(10)), [K(6666.67m, Oglen(8))], "tr"));
        Assert.Null(Kur.YildizGunFarki(K(6656m, Oglen(10)), [K(6600m, Oglen(10))], "tr"));
    }

    [Fact]
    public void Y5B_10_css_izgaralar_tuvalden_aynen_ve_kapsam()
    {
        var c = Css();
        Assert.Contains(".yz-ozet .yz-ozet-ana { flex-grow: 1; display: grid; grid-template-columns: minmax(0, 1.05fr) minmax(0, 1fr); gap: 80px; padding: 64px 64px 48px; }", c);
        Assert.Contains(".yz-ozet .yz-ozet-h1 { margin: 0; font-size: 40px; line-height: 1; font-weight: 300; }", c);
        Assert.Contains(".yz-ozet .yz-ozet-rakam { font-size: 104px; line-height: .95; }", c);
        Assert.Contains(".yz-ozet .yz-ozet-rakam-ek { font-size: 52px; font-weight: 200; }", c);
        Assert.Contains(".yz-ozet .yz-ozet-bilgi { display: flex; flex-wrap: wrap; gap: 40px; font-size: 14px; letter-spacing: .06em; }", c);
        Assert.Contains(".yz-ozet--kasalar .yz-liste-sat { grid-template-columns: minmax(0, 1.2fr) 100px 170px 140px; }", c);
        Assert.Contains(".yz-ozet--kur .yz-liste-sat { grid-template-columns: minmax(0, 1fr) 160px 160px; }", c);
        Assert.Contains(".yz-ozet--hurda .yz-liste-sat { grid-template-columns: minmax(0, 1.2fr) 90px 110px 120px; }", c);
        Assert.Contains(".yz-ozet--kasarapor .yz-liste-sat { grid-template-columns: minmax(0, 1.2fr) 90px 170px; }", c);
        Assert.Contains(".yz-liste--vezne .yz-liste-sat { grid-template-columns: minmax(0, 1.3fr) minmax(0, 1fr) 160px 140px 140px; }", c);
        Assert.Contains(".yz-liste--posalacak .yz-liste-sat { grid-template-columns: minmax(0, 1.2fr) minmax(0, 1fr) 160px 150px; }", c);
        Assert.Contains(".yz-liste--evrak .yz-liste-sat { grid-template-columns: minmax(0, 1.2fr) minmax(0, 1fr) 160px 130px 130px 140px; }", c);
        Assert.Contains(".yz-liste--pusula .yz-liste-sat { grid-template-columns: minmax(0, 1fr) minmax(0, 1.2fr) minmax(0, 1fr) 150px 110px; }", c);
        Assert.Contains(".yz-takvim .yz-takvim-ana { flex-grow: 1; min-height: 0; display: grid; grid-template-columns: minmax(0, 1fr) 460px; gap: 80px; padding: 48px 64px 40px; }", c);
        Assert.Contains(".yz-takvim .yz-liste-sat { grid-template-columns: 70px minmax(0, 1fr) 130px; }", c);
        Assert.Contains(".yz-form .yz-form-ana { flex-grow: 1; min-height: 0; display: grid; grid-template-columns: minmax(0, 1fr) 420px; gap: 96px; padding: 48px 64px 40px; }", c);
        Assert.Contains(".yz-bosekran .yz-bosekran-cizgi { width: 1px; height: 80px; background: var(--ayrac); }", c);
        Assert.Contains(".yz-bosekran .yz-bosekran-metin { max-width: 520px; font-size: 18px; font-weight: 300; }", c);
        // ≤480: büyük rakam ve eki tek satır
        Assert.Matches(@"@media \(max-width: 480px\) \{\s*html:root\[data-sema=""yildiz""\] \.yz-ozet \.yz-ozet-rakam \{ font-size: min\(104px, 15vw\); white-space: nowrap; \}", c);
        // Blok: her kural Yıldız kapsamlı, sabit renk yok
        var blok = Blok5B();
        foreach (Match m in Regex.Matches(blok, @"(?:^|[};])\s*([^{}@;]+)\{"))
        {
            var sec = m.Groups[1].Value.Trim();
            if (sec.Length == 0 || sec.StartsWith("@")) continue;
            foreach (var p in ParantezDuyarliBol(sec))
                Assert.True(p.Contains("[data-sema=\"yildiz\"]"), "kapsamsız seçici: " + p.Trim());
        }
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgba?\(", blok);
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
    public void Y5B_11_sayfalar_Yildiz_yuzunu_kosulsuz_cizer_ozgun_govde_Tum_ayrintilar_arkasinda()
    {
        foreach (var (sayfa, bilesen) in new[] {
            ("Kasalar", "YildizOzet"), ("Kur", "YildizOzet"), ("Hurda", "YildizOzet"), ("KasaRaporu", "YildizOzet"),
            ("Vezne", "YildizListe"), ("PosTahsilatlar", "YildizListe"), ("Evraklar", "YildizListe"), ("GiderPusulalari", "YildizListe"),
            ("Pos", "YildizBos"), ("Banka", "YildizBos"), ("EvrakTakvimi", "YildizTakvim"), ("Gider", "YildizForm") })
        {
            var razor = CssKaynak.Metin("Kuyumcu.Web", "Pages", sayfa + ".razor");
            // Tema temizliği (17.10): şema koşulu kalktı — Yıldız yüzü KOŞULSUZ, özgün gövde "Tüm ayrıntılar" arkasında.
            Assert.Contains($"<Kuyumcu.Web.Bilesenler.Yildiz.{bilesen} B=", razor);
            Assert.DoesNotContain("YildizMi", razor);
            Assert.Contains("@if (YildizOzgunCiz) {", razor);
            Assert.True(razor.IndexOf($"<Kuyumcu.Web.Bilesenler.Yildiz.{bilesen} B=", StringComparison.Ordinal)
                        < razor.IndexOf("@if (YildizOzgunCiz) {", StringComparison.Ordinal), sayfa + ": Yıldız yüzü özgün kökten sonra");
            Assert.Contains("data-yz-ozgun=\"@YildizOzgunIsaret\"", razor);
            var cs = CssKaynak.Metin("Kuyumcu.Web", "Pages", sayfa + ".Yildiz.cs");
            Assert.Matches(@"private string\? YildizOzgunIsaret => ""[a-z-]+"";", cs);
            Assert.DoesNotContain("YildizMi", cs);
            Assert.Contains(sayfa == "Gider" ? "private bool YildizOzgunCiz => YildizAyrintida;" : "private bool YildizOzgunCiz => yildizAyrinti;", cs);
            // Yıldız yüzü İSTEK atmaz ya da yalnız OKUR (yazma uçlarına doğrudan istek yok)
            Assert.DoesNotMatch(@"Api\.(PostAsync|PatchAsync|PutAsync|DeleteAsync)", cs);
        }
        // Gider: bağlantıdan virman sekmesi gelirse özgün ekran açılır
        Assert.Contains("private bool YildizAyrintida => yildizAyrinti || sekme != \"GIDER\";", CssKaynak.Metin("Kuyumcu.Web", "Pages", "Gider.Yildiz.cs"));
    }

    [Fact]
    public void Y5B_12_tuval_metinleri_aynen_ve_en_karsiligi()
    {
        Assert.Equal("Defter bakiyesi. Nakit, kart ve havale ayrı sayılır; gün sonu sayımı yalnız nakitle karşılaştırılır.", YildizKasalarSozlugu.Cumle("tr"));
        Assert.Equal("Para ve banka · ödemesi bekleyen fişler", YildizVezneSozlugu.Ust("tr"));
        Assert.Equal("Henüz POS tanımlı değil", YildizPosSozlugu.BosBaslik("tr"));
        Assert.Equal("POS tanımlanmadan kartlı satışlar nakit kasasına yazılır ve gün sonunda sahte fark çıkar. Önce cihazı, sonra bağlı banka hesabını ekle.", YildizPosSozlugu.BosMetin("tr"));
        Assert.Equal("Toplam 99.097,27 ₺ · komisyon kesildikten sonra bankaya geçer.", YildizPosAlacakSozlugu.Not("99.097,27 ₺", "tr"));
        Assert.Equal("Kimden ya da banka", YildizEvrakSozlugu.AraYerTutucu("tr"));
        Assert.Equal("Kimlik ve adres olmadan pusula açılmaz (VUK md. 234).", YildizPusulaSozlugu.Not("tr"));
        Assert.Equal("Belge ve uyum · vatandaştan alınan altının belgesi", YildizPusulaSozlugu.Ust("tr"));
        Assert.Equal("Para ve banka · işletme gideri ya da kasalar arası aktarma", YildizGiderSozlugu.Ust("tr"));
        Assert.Equal("Para ve banka · biriken eski altın", YildizHurdaSozlugu.Ust("tr"));
        foreach (var en in new[] { YildizKasalarSozlugu.Cumle("en"), YildizKurSozlugu.Cumle("en"), YildizHurdaSozlugu.Cumle("en"),
                                   YildizVezneSozlugu.Not("en"), YildizPosSozlugu.BosMetin("en"), YildizBankaSozlugu.BosMetin("en"),
                                   YildizEvrakSozlugu.Ust("en"), YildizVadeSozlugu.Ust("en"), YildizGiderSozlugu.NotMetni("en"),
                                   YildizPusulaSozlugu.Not("en"), YildizKasaRaporuSozlugu.Cumle(8, "en") })
        {
            Assert.False(string.IsNullOrWhiteSpace(en));
            Assert.DoesNotMatch("[çğışöüÇĞİŞÖÜ]", en);
        }
    }
}
