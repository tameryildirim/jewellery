using System.Net;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 35 FAZ A — KÂR-ZARAR EKRANI
//
// Ölçülen şey "metin var mı" değil:
//   • Kâr köprüsü tablosu İADE satırını gerçekten çiziyor mu
//   • Takas farkı AYRI kartta ve GEREKÇE metniyle mi çıkıyor (Anayasa md.1'in
//     ekran karşılığı — 'ciro düştü' algısı açıklamasız bırakılamaz)
//   • Karşılaştırma seçilince SUNUCUYA karsilastir= parametresi gidiyor mu
//   • Ürün satırında HAS kâr sütunu duruyor mu (has bacağı gizlenmez)
//   • Eski alanlar (UX-4d/UX-1-C3 kayıtları) HÂLÂ çiziliyor mu — şekil regresyonu
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KarZararEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string KarZararYaniti = """
    {"baslangic":"2026-09-01","bitis":"2026-09-17","gunSayisi":17,"subeId":null,
     "satisAdedi":4,"iadeAdedi":1,
     "brutHasilatTl":6000,"iadeTl":1500,"hasilatTl":4500,
     "brutMaliyetTl":4000,"iadeMaliyetTl":1000,"maliyetTl":3000,
     "karTl":1500,"karMarjiYuzde":33.33,
     "iscilikTl":750,"takasFarkTl":2000,"takasAdedi":1,
     "takasNotu":"Takas satırının tutarı brüt değer değildir.",
     "donemKariTl":2250,
     "hasilatHas":0.6591,"iadeHas":0.2197,"maliyetHas":0.4394,"karHas":0.2197,
     "hasilatHasDefterGram":27.48,"altinHasilatHasDefterGram":27.48,
     "hasDefterMadenDagilimi":[{"maden":"ALTIN","madenAdi":"Altın","toplamHas":27.48,"satirSayisi":3},
                               {"maden":"GUMUS","madenAdi":"Gümüş","toplamHas":9.25,"satirSayisi":1}],
     "madenUyarisi":"Birden çok maden var; gramlar toplanmaz.",
     "hasTanimNotu":"İki büyüklük farklıdır.",
     "karKoprusu":[{"kod":"BRUT_HASILAT","tutarTl":6000,"araToplam":false},
                   {"kod":"IADE","tutarTl":-1500,"araToplam":false},
                   {"kod":"NET_HASILAT","tutarTl":4500,"araToplam":true},
                   {"kod":"MALIYET","tutarTl":-4000,"araToplam":false},
                   {"kod":"IADE_MALIYETI","tutarTl":1000,"araToplam":false},
                   {"kod":"BRUT_KAR","tutarTl":1500,"araToplam":true},
                   {"kod":"ISCILIK","tutarTl":750,"araToplam":false},
                   {"kod":"DONEM_KARI","tutarTl":2250,"araToplam":true}],
     "storno":{"adet":1,"tutarTl":900,"notu":"Storno çifti toplamlara katılmaz."},
     "karsilastir":null,"onceki":null,"degisimYuzde":null,
     "maliyetiBilinenAdet":4,"maliyetiBilinmeyenAdet":0,"kuruBulunamayanSatis":0,
     "guvenilirlik":"TAM","uyari":null}
    """;

    private const string UrunYaniti = """
    {"baslangic":"2026-09-01","bitis":"2026-09-17","subeId":null,"urunSayisi":1,
     "limit":50,"dahaFazlasiVar":false,
     "maliyetiBilinenAdet":4,"maliyetiBilinmeyenAdet":0,"kuruBulunamayanSatis":0,
     "guvenilirlik":"TAM",
     "satirlar":[{"urunId":"u1","kod":"K-1","ad":"Bilezik","satisAdedi":4,"iadeAdedi":1,
                  "brutHasilatTl":6000,"iadeTl":1500,"hasilatTl":4500,"maliyetTl":3000,
                  "karTl":1500,"hasilatHas":0.6591,"maliyetHas":0.4394,"karHas":0.2197,
                  "karMarjiYuzde":33.33,"maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM"}]}
    """;

    private const string StokYaniti = """
    {"subeId":null,"toplamAdet":10,"toplamHasGram":91.6,"maliyetliAdet":10,
     "maliyetsizAdet":0,"stokMaliyetiTl":10000,"stokMaliyetiHas":1.46,
     "guvenilirlik":"TAM","kaynakDagilimi":[],"konsinyeUrunSayisi":0,"uyari":null}
    """;

    // ═════════════════════════════════════════════════════════════════════════
    // P-64 — DOLU KARŞILAŞTIRMA FİKSTÜRLERİ
    // Tur 8 bulgusu: sahte yanıt HEP "onceki":null taşıyordu → delta yolu
    // (KarZarar.razor #kz-karsilastirma + KPI alt metni) HİÇ kapsanmıyordu.
    // Gövdeler KarZararServisi.Donem'in gerçek şeklini (onceki{…},
    // degisimYuzde{hasilatTlYuzde,karTlYuzde,donemKariTlYuzde,karHasYuzde})
    // birebir taşır.
    // ═════════════════════════════════════════════════════════════════════════
    private static string Karsilastirmali(string onceki, string degisim)
    {
        const string eski = "\"karsilastir\":null,\"onceki\":null,\"degisimYuzde\":null";
        // Fikstür sessizce eski gövdeye düşmesin: yer tutucu bulunamazsa patla.
        if (!KarZararYaniti.Contains(eski)) throw new InvalidOperationException("fikstür yer tutucusu yok");
        return KarZararYaniti.Replace(eski,
            $"\"karsilastir\":\"ONCEKI_DONEM\",\"onceki\":{onceki},\"degisimYuzde\":{degisim}");
    }

    private static readonly string DoluKarsilastirma = Karsilastirmali(
        """{"baslangic":"2026-08-15","bitis":"2026-08-31","gunSayisi":17,"hasilatTl":3600,"maliyetTl":2400,"karTl":1200,"donemKariTl":1800,"karHas":0.3,"iscilikTl":600,"iadeTl":0}""",
        """{"hasilatTlYuzde":25,"karTlYuzde":25,"donemKariTlYuzde":25,"karHasYuzde":-26.77}""");

    /// <summary>Önceki dönem SIFIR → sunucu Yuzde() null döndürür.</summary>
    private static readonly string SifirOncekiKarsilastirma = Karsilastirmali(
        """{"baslangic":"2026-08-15","bitis":"2026-08-31","gunSayisi":17,"hasilatTl":0,"maliyetTl":0,"karTl":0,"donemKariTl":0,"karHas":0,"iscilikTl":0,"iadeTl":0}""",
        """{"hasilatTlYuzde":null,"karTlYuzde":null,"donemKariTlYuzde":null,"karHasYuzde":null}""");

    private string karsilastirmaYaniti = DoluKarsilastirma;
    // ORTAK-1 hakem 30.09: küçük has sınır testleri özet gövdesini değiştirir.
    private string ozetYaniti = KarZararYaniti;

    public KarZararEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => karsilastirmaYaniti, () => ozetYaniti))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<RaporServisi>();
    }

    // ORTAK-1 kıyas devri 28.09: #kz-karsilastir yerel <select> değil ortak Secici
    // (id aynı) — gerçek kullanıcı yolu: tetiğe tıkla, listeden seçeneğe tıkla.
    private static void KarsilastirSec(IRenderedComponent<Kuyumcu.Web.Pages.KarZarar> c, string deger)
    {
        // PAKET RAPOR-4 (K16): isteğe bağlı alan açılışta katlı — önce "+ Karşılaştır".
        if (c.FindAll("#kz-karsilastir-ac").Count > 0) c.Find("#kz-karsilastir-ac").Click();
        c.Find("#kz-karsilastir").Click();
        c.Find($"#kz-karsilastir-liste li[data-deger='{deger}']").Click();
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.KarZarar> Ekran()
    {
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-kopru")));
        return c;
    }

    // ── PAKET S1-i — tren sayfa sekmelere bölündü (kurucu 24.09) ────────────
    [Fact]
    public void S1i_KZ_sekiz_bolum_bes_sekmede_idler_ayni_yalniz_ozet_acik()
    {
        var c = Ekran();
        Assert.Equal(["kz-sekme-ozet", "kz-sekme-grafik", "kz-sekme-kopru", "kz-sekme-urunler", "kz-sekme-stok"],
            c.FindAll("#kz-sekme-seridi [role=tab]").Select(s => s.Id ?? "").ToArray());
        // Bölümler SİLİNMEDİ, kendi sekmesinin panelinde yaşar (id'ler aynı).
        Assert.NotNull(c.Find("#kz-panel-ozet #kz-tl-kpi"));
        Assert.NotNull(c.Find("#kz-panel-grafik #kz-grafikler"));
        Assert.NotNull(c.Find("#kz-panel-kopru #kz-kopru"));
        // TL ve HAS bacağı AYNI sekmede (ekranın tek işi ikisini birlikte göstermek).
        Assert.Equal(2, c.FindAll("#kz-panel-ozet h2.bolum-baslik").Count);
        // İlk bakış: yalnız Özet paneli görünür; diğerleri hidden.
        Assert.False(c.Find("#kz-panel-ozet").HasAttribute("hidden"));
        Assert.True(c.Find("#kz-panel-kopru").HasAttribute("hidden"));
        Assert.True(c.Find("#kz-panel-stok").HasAttribute("hidden"));
    }

    [Fact]
    public void P35_01_Kar_koprusu_tablosu_IADE_satirini_gosterir()
    {
        var c = Ekran();
        var metin = c.Find("#kz-kopru").TextContent;

        Assert.Contains("İade", metin);
        Assert.Contains("Net hasılat", metin);
        Assert.Contains("Dönem kârı", metin);
        // Ham kod ekrana BASILMAZ (UX-S31).
        Assert.DoesNotContain("BRUT_HASILAT", metin);
        Assert.DoesNotContain("DONEM_KARI", metin);
    }

    [Fact]
    public void P35_02_Takas_farki_AYRI_kartta_ve_aciklamasiyla_cikar()
    {
        var c = Ekran();
        var kart = c.Find("#kz-takas").TextContent;

        Assert.Contains("Takas farkı", kart);
        Assert.Contains("hasılata", kart);          // gerekçe metni
        Assert.Contains("fark", kart, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void P35_03_Karsilastirma_secilince_sunucuya_parametre_gider()
    {
        var c = Ekran();
        istekler.Clear();

        KarsilastirSec(c, "ONCEKI_DONEM");

        c.WaitForAssertion(() => Assert.Contains(
            istekler, i => i.Yol.Contains("kar-zarar") && i.Yol.Contains("karsilastir=ONCEKI_DONEM")));
    }

    [Fact]
    public void P35_04_Urun_satirinda_HAS_kar_sutunu_vardir()
    {
        var c = Ekran();
        var basliklar = c.FindAll("table.veri-tablosu thead th").Select(x => x.TextContent).ToList();
        // PAKET RAPOR-2: etiket tek yazımla "Has kâr" (büyük harf tutarsızlığı kalktı).
        Assert.Contains(basliklar, b => b.Contains(Kuyumcu.Core.I18n.KarRaporSozlugu.Tr.HasKarEtiket));

        // Satırda gerçek rakam çizilir. MADEN-AYRIMI: has tek kural (Bicim.Has, "has").
        Assert.Contains("0,2197 has", c.Markup);
    }

    [Fact]
    public void P35_05_Eski_alanlar_HALA_cizilir_sekil_regresyonu()
    {
        var c = Ekran();
        var m = c.Markup;

        // MADEN-AYRIMI (hasım YÜKSEK): "Bacağı" jargonu PRO'da da kalktı — iki modda sade başlık.
        Assert.Contains(Kuyumcu.Core.I18n.OrtakGorunumSozlugu.Tr.KarZararParaKariBasit, m);
        // PAKET RAPOR-2: has bölüm başlığı ne olduğunu söyler (satış tutarının altın karşılığı), "has gram" değil.
        Assert.Contains(Kuyumcu.Core.I18n.KarRaporSozlugu.Tr.AltinKariBaslik, m);
        Assert.DoesNotContain("Bacağı", m);
        Assert.Contains("Güvenilirlik: TAM", m);
        Assert.Contains("Stok Değerlemesi", m);
        Assert.Contains("Ürün Bazlı Kârlılık", m);
        Assert.NotNull(c.Find("#kz-bas"));
        Assert.NotNull(c.Find("#kz-bit"));
        Assert.NotNull(c.Find("#kz-bu-ay"));
    }

    [Fact]
    public void P35_06_HAS_tanim_notu_iki_buyuklugu_ayirir()
    {
        var c = Ekran();
        Assert.False(string.IsNullOrWhiteSpace(c.Find("#kz-has-tanim").TextContent));
        // MADEN-AYRIMI tur 2 (tasarım hakemi KRİTİK): 'has' yalnız altın — defter kartının adı "Defterdeki altın (has)".
        Assert.Contains(Kuyumcu.Core.I18n.KarRaporSozlugu.Tr.DefterAltinEtiketi, c.Markup);
    }

    [Fact]
    public void P35_07_Storno_blogu_ayri_gorunur()
    {
        var c = Ekran();
        var metin = c.Find("#kz-storno").TextContent;
        Assert.Contains("Storno", metin);
        Assert.Contains("900", metin);
    }

    [Fact]
    public void P35_08_Her_dugme_gercek_istek_atar()
    {
        // DoD: "tıklanınca hiçbir şey olmayan" düğme = paket geçmez.
        // ⚠ Sıra bilinçli: sayfa AÇILIŞTA "bu ay" dönemiyle gelir ve UX-4d
        // madde 22 gereği AYNI sorgu üst üste gönderilmez. Bu yüzden her
        // düğme, dönemi GERÇEKTEN değiştirecek sırayla tıklanır.
        // P35 ay sınırı (01.10.2026): saat SABİT (15.10 12:00 TSİ). Sistem saatiyle her ayın
        // 1'inde "bu ay" = "bugün" (01.10–01.10) olur, son tık aynı sorguyu üretir ve madde 22
        // gereği gitmez → test takvime bağlı kırmızıydı. Ay başı ayrıca P35_08_ay_siniri'nde.
        Services.AddSingleton<TimeProvider>(new SabitIstanbulSaati(new DateTimeOffset(2026, 10, 15, 9, 0, 0, TimeSpan.Zero)));
        var c = Ekran();
        foreach (var kimlik in new[] { "kz-gecen-ay", "kz-bugun", "kz-bu-ay" })
        {
            istekler.Clear();
            c.Find($"#{kimlik}").Click();
            c.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("kar-zarar")),
                TimeSpan.FromSeconds(2));
        }
    }

    /// <summary>Sabit saat; yerel saat dilimi makineden bağımsız Europe/Istanbul (UTC+3).</summary>
    private sealed class SabitIstanbulSaati(DateTimeOffset utc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utc;
        public override TimeZoneInfo LocalTimeZone { get; } = Kuyumcu.Api.Common.Zaman.Isletme;
    }

    // ── P35 ay sınırı (01.10.2026) — dönem düğmeleri YEREL güne göre doğru aralığı ister ──
    // 01.10 01:30 TSİ = 30.09 22:30 UTC: UTC'ye göre hesaplasaydı "bu ay" EYLÜL çıkardı.
    // Her tık bir öncekinden farklı aralık üretir (geçen ay araya girer) → her biri istek atmalı.
    [Theory]
    [InlineData("2026-09-30T22:30:00Z", "2026-10-01", "2026-10-01", "2026-09-01", "2026-09-30")] // 01.10 01:30 TSİ
    [InlineData("2026-10-15T09:00:00Z", "2026-10-01", "2026-10-15", "2026-09-01", "2026-09-30")] // 15.10 12:00 TSİ
    [InlineData("2026-10-31T22:30:00Z", "2026-11-01", "2026-11-01", "2026-10-01", "2026-10-31")] // 01.11 01:30 TSİ
    public void P35_08_ay_siniri_donem_dugmeleri_yerel_gune_gore(
        string utc, string ayBasi, string bugun, string gecenBas, string gecenBit)
    {
        Services.AddSingleton<TimeProvider>(new SabitIstanbulSaati(DateTimeOffset.Parse(utc, System.Globalization.CultureInfo.InvariantCulture)));
        string Ozet(string bas, string bit) => $"/raporlar/kar-zarar?baslangic={bas}&bitis={bit}";

        var c = Ekran();
        Assert.Contains(istekler, i => i.Yol.EndsWith(Ozet(ayBasi, bugun)));

        foreach (var (kimlik, bas, bit) in new[]
        {
            ("kz-gecen-ay", gecenBas, gecenBit),
            ("kz-bugun", bugun, bugun),
            ("kz-gecen-ay", gecenBas, gecenBit),
            ("kz-bu-ay", ayBasi, bugun),
        })
        {
            istekler.Clear();
            c.Find($"#{kimlik}").Click();
            c.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith(Ozet(bas, bit))),
                TimeSpan.FromSeconds(2));
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // ── ORTAK-1 hakem turu 29.09 (tasarım hakemi, KRİTİK) ─────────────────────
    // BASİT modda bacak başlıkları muhasebe dilindeydi ("TL Bacağı — muhasebenin gördüğü")
    // ve gram 4 ondalıktı ("0,0000 g"). BASİT: sade başlık + 2 ondalık (yalnız gösterim);
    // PRO aynen. 30.09 hakem (DÜŞÜK): beklenen gram SABİT tr-TR metnidir ("0,66 g") ve ekran
    // İNGİLİZCE iş parçacığı kültüründe çizilir — biçim tarayıcı kültürüne bağlıysa "0.66 g" çıkar, test kırılır.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ORTAK1_HK_bacak_basliklari_ve_gram_ondaligi_moda_gore(bool basit)
    {
        var eskiKultur = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        try
        {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        var c = Ekran();
        var b = Kuyumcu.Core.I18n.OrtakGorunumSozlugu.Tr;
        var p = Kuyumcu.Core.I18n.Sozlukler.Getir("tr").KarZarar;
        var basliklar = c.FindAll("#kz-panel-ozet h2.bolum-baslik").Select(h => h.TextContent.Trim()).ToArray();
        var ozet = c.Find("#kz-panel-ozet").TextContent;
        // MADEN-AYRIMI (hasım YÜKSEK + ORTA, ekip kararı 5): başlıklar iki modda sade; has iki modda
        // AYNI rakam (Bicim.Has, 4 hane, "has"). BASİT'te defter gramı ızgaradan ayrı satırda (ⓘ);
        // PRO'da kart + teknik not ⓘ arkasında (id kz-has-tanim korunur).
        Assert.Equal([b.KarZararParaKariBasit, Kuyumcu.Core.I18n.KarRaporSozlugu.Tr.AltinKariBaslik], basliklar);   // RAPOR-2
        Assert.Contains("0,6591 has", ozet);
        Assert.DoesNotContain("0,66 g", ozet);
        Assert.DoesNotContain("0.6591", ozet);
        Assert.DoesNotContain(p.TlBacagi, c.Markup);
        Assert.DoesNotContain(p.HasBacagi, c.Markup);
        if (basit)
        {
            Assert.NotNull(c.Find("#kz-nereden"));                    // defter gramı kapalı katlanırda
            Assert.Empty(c.FindAll("#kz-defter-ayri"));
            Assert.DoesNotContain(string.Format(b.KarZararDefterGramBasit, "Altın"), ozet);
            Assert.Empty(c.FindAll("#kz-has-tanim"));
        }
        else
        {
            Assert.Contains(Kuyumcu.Core.I18n.KarRaporSozlugu.Tr.DefterAltinEtiketi, ozet);   // MADEN-AYRIMI tur 2
            Assert.Empty(c.FindAll("#kz-defter-ayri"));
            Assert.NotNull(c.Find("details#kz-has-tanim-ipucu #kz-has-tanim"));
        }
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = eskiKultur; }
    }

    // ── ORTAK-1 hakem turu 30.09 (KRİTİK ×2): BASİT N2 sıfırdan farklı küçük has'ı "0,00 g"
    // yazıyordu; kayıp uyarısı "0,00 gram has KAYBETTİNİZ" diye kendisiyle çelişiyordu.
    // Beklenen metinler SABİT tr-TR; ekran İngilizce kültürde çizilir (biçim kültürden bağımsız).
    private IRenderedComponent<Kuyumcu.Web.Pages.KarZarar> KucukHasEkrani(bool basit, string karHas, string karTl)
    {
        ozetYaniti = KarZararYaniti.Replace("\"karHas\":0.2197", $"\"karHas\":{karHas}")
                                   .Replace("\"karTl\":1500", $"\"karTl\":{karTl}");
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        return Ekran();
    }

    private static string[] KarKartDegerleri(IRenderedComponent<Kuyumcu.Web.Pages.KarZarar> c) =>
        c.FindAll("#kz-panel-ozet .stat-deger").Select(p => p.TextContent.Trim()).ToArray();

    [Theory]
    // MADEN-AYRIMI (ekip kararı 5): iki modda Bicim.Has (4 hane, "has") — BASİT ayrı yuvarlama yapmaz.
    [InlineData(true, "0.0001", "0,0001 has")]
    [InlineData(true, "-0.004", "-0,0040 has")]
    [InlineData(true, "0.005", "0,0050 has")]
    [InlineData(true, "-1.2549", "-1,2549 has")]
    [InlineData(true, "0", "0,0000 has")]
    [InlineData(false, "0.0001", "0,0001 has")]
    [InlineData(false, "-0.004", "-0,0040 has")]
    public void ORTAK1_HK30_kucuk_has_sifirdan_farkliysa_sifir_gibi_gorunmez(bool basit, string karHas, string beklenen)
    {
        var eskiKultur = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        try
        {
            var degerler = KarKartDegerleri(KucukHasEkrani(basit, karHas, "1500"));
            Assert.Contains(beklenen, degerler);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = eskiKultur; }
    }

    [Theory]
    [InlineData(true, "0,0040")]   // MADEN-AYRIMI: iki modda 4 hane
    [InlineData(false, "0,0040")]
    public void ORTAK1_HK30_has_kaybi_uyarisi_kayip_gramini_sifir_yazmaz(bool basit, string beklenenGram)
    {
        var eskiKultur = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        try
        {
            var c = KucukHasEkrani(basit, "-0.004", "1500");
            var uyari = c.Find("#kz-panel-ozet .uyari-kirmizi p").TextContent;
            var p = Kuyumcu.Core.I18n.Sozlukler.Getir("tr").KarZarar;
            Assert.Equal(string.Format(p.HasKaybiMetin, Bicim.TlKurus(1500m), beklenenGram), uyari);
            Assert.DoesNotContain("0,00 gram", uyari);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = eskiKultur; }
    }

    [Fact]
    public void ORTAK1_HK_donem_karti_ic_boslukta_sekmelerle_arasi_bosluklu()
    {
        var c = Ekran();
        Assert.NotNull(c.Find("#kz-donem-kart > .kart-govde #kz-bas"));
        Assert.NotNull(c.Find("#kz-donem-kart > .kart-govde #kz-bit"));
        Assert.Contains("dikey-bosluk", c.Find("div.sayfa").GetAttribute("class") ?? "");
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<string> karsilastirmaGovdesi, Func<string>? ozetGovdesi = null) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var tam = istek.RequestUri!.PathAndQuery;
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, tam, govde));

            var g = yol switch
            {
                var y when y.Contains("kar-zarar/urun") => UrunYaniti,
                // P-64: karsilastir= parametresi GİTTİYSE sunucu gibi onceki +
                // degisimYuzde DOLU döner; gitmediyse eski (null) gövde.
                var y when y.Contains("kar-zarar") && tam.Contains("karsilastir=") => karsilastirmaGovdesi(),
                var y when y.Contains("kar-zarar") => ozetGovdesi?.Invoke() ?? KarZararYaniti,
                var y when y.Contains("stok-degerlemesi") => StokYaniti,
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-17T09:00:00Z","ayarFiyatlari":[]}""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    // ── PAKET RAPOR-4 ─────────────────────────────────────────────────────────
    [Fact]
    public void RD4E10_Karsilastir_acilista_katli_tiklayinca_secici_acilir_istek_gitmez()
    {
        var c = Ekran();
        Assert.Empty(c.FindAll("#kz-karsilastir"));
        var dugme = c.Find("#kz-karsilastir-ac");
        Assert.Equal(Kuyumcu.Core.I18n.KarRaporSozlugu.Tr.KarsilastirAc, dugme.TextContent.Trim());
        var once = istekler.Count;
        dugme.Click();
        Assert.NotNull(c.Find("#kz-karsilastir"));
        Assert.Empty(c.FindAll("#kz-karsilastir-ac"));
        Assert.Equal(once, istekler.Count);   // açmak sorgu atmaz; seçim atar (P35_03)
    }

    [Fact]
    public void RD4E11_Kart_maliyeti_kullanildiysa_kaynak_rozeti_ve_notu_yazilir()
    {
        ozetYaniti = "{\"kartMaliyetliAdet\":2," + KarZararYaniti.TrimStart()[1..];
        var c = Ekran();
        var not = c.Find("#kz-kart-maliyeti").TextContent;
        Assert.Contains(Kuyumcu.Core.I18n.KarRaporSozlugu.Tr.KartMaliyetiRozet, not);
        Assert.Contains(string.Format(Kuyumcu.Core.I18n.KarRaporSozlugu.Tr.KartMaliyetiNot, "2"), not);
    }

    [Fact]
    public void RD4E12_Kart_maliyeti_yoksa_not_cizilmez()
    {
        var c = Ekran();
        Assert.Empty(c.FindAll("#kz-kart-maliyeti"));
    }

    [Fact]
    public void P45_04_Ekran_maden_basina_ayri_kart_cizer()
    {
        // ⚠ P-45: ekran tek bir "Defter has gramı" kartı basıyor ve altın +
        // gümüş gramlarını TOPLUYORDU (fikstürde 44,4154 g). Her maden ayrı
        // karttır; birleşik rakam ekranda YOKTUR.
        var c = Ekran();
        var metin = c.Markup;

        // MADEN-AYRIMI tur 2 (tasarım hakemi KRİTİK): 'has' yalnız altının etiketinde; gümüş "saf gram".
        Assert.Contains(Kuyumcu.Core.I18n.KarRaporSozlugu.Tr.DefterAltinEtiketi, metin);
        Assert.Contains(string.Format(Kuyumcu.Core.I18n.KarRaporSozlugu.Tr.DefterMadenEtiketi, "Gümüş"), metin);
        Assert.DoesNotContain("Defter has gramı (Gümüş)", metin);
        // MADEN-AYRIMI: altın "has", gümüş kendi biriminde (saf gram) — gümüş "has" yazılmaz.
        Assert.Contains("27,4800 has", metin);
        Assert.Contains("saf 9,25 g", metin);
        Assert.DoesNotContain("9,2500 has", metin);
        // Toplam (36,73) hiçbir yerde çizilmez.
        Assert.DoesNotContain("36,73", metin);
        // Etiketsiz tek birleşik kart kalmadı.
        Assert.DoesNotContain(">Defter has gramı<", metin);

        // Birden çok maden → uyarı görünür.
        var uyari = c.Find("#kz-maden-uyari").TextContent;
        Assert.Contains("toplanmaz", uyari);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // P-64 — GRAFİK BAĞLAMA + DELTA KANITI
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Sayfayı açar ve "Karşılaştır: önceki dönem" seçer (gerçek kullanıcı yolu).</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.KarZarar> KarsilastirmaliEkran()
    {
        var c = Ekran();
        KarsilastirSec(c, "ONCEKI_DONEM");
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-karsilastirma")), TimeSpan.FromSeconds(2));
        return c;
    }

    private static string KpiAlt(IRenderedComponent<Kuyumcu.Web.Pages.KarZarar> c, string izgara, string etiket)
    {
        var kart = c.FindAll($"#{izgara} .stat-kart")
            .First(k => k.QuerySelector(".stat-etiket")!.TextContent.Trim() == etiket);
        return kart.QuerySelector(".stat-alt")?.TextContent.Trim() ?? "";
    }

    [Fact]
    public void P64_01_Dolu_onceki_ile_karsilastirma_blogu_cizilir()
    {
        karsilastirmaYaniti = DoluKarsilastirma;
        var c = KarsilastirmaliEkran();
        var blok = c.Find("#kz-karsilastirma").TextContent;

        // Önceki dönem aralığı, gün sayısı ve hasılatı metinde geçer.
        Assert.Contains("2026-08-15", blok);
        Assert.Contains("2026-08-31", blok);
        Assert.Contains(Bicim.TlKurus(3600m), blok);          // önceki hasılat
        Assert.Contains(Bicim.TlKurus(1800m), blok);          // önceki dönem kârı
        // Yüzde değeri ok ile basılır.
        Assert.Equal("Değişim: ▲ %25", c.Find("#kz-delta-hasilat").TextContent.Trim());
        Assert.Equal("Değişim: ▲ %25", c.Find("#kz-delta-donem-kari").TextContent.Trim());
        // Yüzdeler doluyken "hesaplanamaz" notu ÇIKMAZ.
        Assert.Empty(c.FindAll("#kz-yuzde-yok"));
    }

    [Fact]
    public void P64_02_Degisim_null_iken_tire_basilir()
    {
        karsilastirmaYaniti = SifirOncekiKarsilastirma;
        var c = KarsilastirmaliEkran();

        Assert.Equal("Değişim: —", c.Find("#kz-delta-hasilat").TextContent.Trim());
        Assert.Equal("Değişim: —", c.Find("#kz-delta-donem-kari").TextContent.Trim());
        // 0% UYDURULMAZ — ne blokta ne KPI kartlarında.
        Assert.DoesNotContain("%0", c.Find("#kz-karsilastirma").TextContent);
        Assert.DoesNotContain("%0", c.Find("#kz-tl-kpi").TextContent);
        Assert.StartsWith("—", KpiAlt(c, "kz-tl-kpi", "Hasılat"));
        // Neden tire yazdığı söylenir.
        Assert.Contains("sıfır", c.Find("#kz-yuzde-yok").TextContent);
    }

    [Fact]
    public void P64_03_Karsilastirma_secimi_istege_parametre_olarak_gider()
    {
        karsilastirmaYaniti = DoluKarsilastirma;
        var c = Ekran();
        // Varsayılan KAPALI: ilk istekte parametre yok, ekranda delta yok.
        Assert.Contains(istekler, i => i.Yol.Contains("/raporlar/kar-zarar?") && !i.Yol.Contains("karsilastir="));
        Assert.Empty(c.FindAll("#kz-karsilastirma"));
        Assert.Equal("", KpiAlt(c, "kz-tl-kpi", "Hasılat"));

        istekler.Clear();
        KarsilastirSec(c, "ONCEKI_DONEM");

        c.WaitForAssertion(() =>
        {
            Assert.Contains(istekler, i => i.Yol.Contains("/raporlar/kar-zarar?")
                                           && i.Yol.Contains("karsilastir=ONCEKI_DONEM"));
            // Seçimin SONUCU ekranda: sunucunun döndürdüğü delta çizildi.
            Assert.Contains("▲ %25", KpiAlt(c, "kz-tl-kpi", "Hasılat"));
        }, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Sayfa_EnAzBirVeriGrafigiCizer()
    {
        var c = Ekran();

        // Çubuk grafik: gelir / maliyet / kâr / dönem kârı — yüksekliği > 0 olan çubuk var.
        var cubuklar = c.FindAll("#kz-grafik-cubuk .grafik-sar svg rect");
        // PAKET RAPOR-4 (tasarım DÜŞÜK): karşılaştırma yokken TEK seri — çubuk sütun ortasında, boş 2. çubuk çizilmez.
        Assert.Equal(4, cubuklar.Count);   // 4 kalem × 1 seri
        Assert.Contains(cubuklar, r => double.Parse(r.GetAttribute("height")!,
            System.Globalization.CultureInfo.InvariantCulture) > 0);
        // RAPOR-GORUNUM: eksen etiketi SVG içinde ~6 px'e küçülüyordu → SVG ALTINDA okunur yazı + değer.
        Assert.Empty(c.FindAll("#kz-grafik-cubuk .grafik-sar svg text"));
        Assert.Contains("Hasılat", c.Find("#kz-grafik-cubuk .grafik-eksen-alt").TextContent);
        Assert.Contains("₺", c.Find("#kz-grafik-cubuk .grafik-eksen-alt .grafik-eksen-deger").TextContent);

        // Pasta: ürün bazlı maliyet — fikstürde tek ürün (Bilezik, 3000 TL) → tek tam dilim.
        var dilimler = c.FindAll("#kz-grafik-pasta svg.grafik-pasta path");
        Assert.Single(dilimler);
        Assert.Contains("Bilezik", c.Find("#kz-grafik-pasta").TextContent);
    }

    [Fact]
    public void Karsilastir_KpiKartindaDeltaBasar()
    {
        karsilastirmaYaniti = DoluKarsilastirma;
        var c = KarsilastirmaliEkran();

        Assert.Equal("▲ %25 önceki döneme göre", KpiAlt(c, "kz-tl-kpi", "Hasılat"));
        Assert.Equal("▲ %25 önceki döneme göre", KpiAlt(c, "kz-tl-kpi", "Dönem kârı"));
        // Kâr kartında marj KORUNUR, delta yanına eklenir.
        var karAlt = KpiAlt(c, "kz-tl-kpi", "Kâr");
        Assert.StartsWith("%33", karAlt);
        Assert.EndsWith(" · ▲ %25 önceki döneme göre", karAlt);
        // HAS bacağında has kârı değişimi (negatif) aşağı okla basılır.
        Assert.Contains("▼ %26,77", c.Markup);
        // Çubuk grafiğe ikinci seri (karşılaştırılan dönem) ve lejantı gelir.
        Assert.Contains("Karşılaştırılan dönem", c.Find("#kz-grafik-cubuk").TextContent);
    }

    [Fact]
    public void P64_04_Negatif_kar_cubugu_eksi_yukseklik_uretmez_tabanin_altina_iner()
    {
        // Zarar eden dönemde kâr negatiftir. Eski CubukGrafik eksi yükseklikli
        // <rect> üretiyordu → tarayıcı çizmez, zarar ekranda "yok" görünürdü.
        var c = Render<Kuyumcu.Web.Bilesenler.CubukGrafik>(p => p
            .Add(x => x.Noktalar, new List<Kuyumcu.Web.Bilesenler.CiftNokta>
            {
                new("Hasılat", 1000m, 0m),
                new("Kâr", -500m, 0m),
            }));
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var rects = c.FindAll("rect");
        Assert.All(rects, r => Assert.True(double.Parse(r.GetAttribute("height")!, inv) >= 0));
        var hasilat = rects[0];
        var zarar = rects[2];
        Assert.True(double.Parse(zarar.GetAttribute("height")!, inv) > 0, "zarar çubuğu çizilmeli");
        // Zarar çubuğu, hasılat çubuğunun TABANINDAN başlar (sıfır çizgisi).
        var taban = double.Parse(hasilat.GetAttribute("y")!, inv) + double.Parse(hasilat.GetAttribute("height")!, inv);
        Assert.Equal(taban, double.Parse(zarar.GetAttribute("y")!, inv), 1);
        Assert.Single(c.FindAll("line"));
    }
}
