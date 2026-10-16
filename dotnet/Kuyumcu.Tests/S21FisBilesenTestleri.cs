using System.Text.Json;
using Bunit;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.Domain;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-1 — Islemler.razor'un Bilesenler/Fis/ altına bölünmesi
//
// Bu paket YALNIZ YAPIDIR (davranış değişmez); davranışın kendisini mevcut POS
// testleri (Karma, Iade, Barkod, Teklif, BasitMod, P-10, P-16, UX-4b/4c …) sınar.
// Buradaki testler taşınan kodun SÖZLEŞMESİNİ sabitler:
//   (a) saf hesaplar sunucu formülünün aynısı (FisHesap, *Girisi, FisYaniti),
//   (b) yapı kuralları: sayfa ≤ 1.400 satır, fiş bileşenleri POST ETMEZ ve
//       ApiIstemci enjekte etmez, tek POST yolu Kaydet(), id envanteri korunur,
//   (c) bileşen değişikliği sayfaya HABER VERİR (Degisti) — vermezse Kaydet
//       kilidi ve FARK bayat kalır (bölünmenin tek yeni riski).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S21FisSafTestleri
{
    private static SozlukIslem T => Sozlukler.Getir("tr").Islem;

    private static FisKalemi Kalem(decimal adet, decimal fiyat, decimal iskonto = 0m) =>
        new() { UrunId = "u1", UrunAdi = "Bilezik", Adet = adet, BirimFiyat = fiyat, SatirIskontoYuzde = iskonto };

    [Fact]
    public void FisHesap_satir_ve_fis_iskontosu_sunucu_formuluyle_ayni()
    {
        var k = new List<FisKalemi> { Kalem(2m, 1000m, 10m), Kalem(1m, 333.33m) };
        Assert.Equal(1800m, FisHesap.SatirNet(k[0]));                 // 2000 − %10
        Assert.Equal(2133.33m, FisHesap.SatirToplamNet(k));
        Assert.Equal(106.67m, FisHesap.FisIskontoTutari(k, 5m));      // 2133,33 × %5 = 106,6665 → 106,67
        Assert.Equal(2026.66m, FisHesap.Toplam(k, 5m));
        Assert.Equal(306.67m, FisHesap.IskontoOnizleme(k, 5m));       // 200 satır + 106,67 fiş
        Assert.Equal(2133.33m, FisHesap.Toplam(k, null));             // boş yüzde = iskonto yok
    }

    [Fact]
    public void FisHesap_iskonto_brutu_asamaz()
    {
        var k = Kalem(1m, 100m, 150m);   // sunucu da 100'e kırpar
        Assert.Equal(0m, FisHesap.SatirNet(k));
        Assert.Equal(0m, FisHesap.FisIskontoTutari([Kalem(1m, 100m)], 250m) - 100m);
    }

    [Fact]
    public void FisHesap_nakit_kilidi_yalniz_NAKIT_bacakta_ve_sunucu_bayragiyla()
    {
        var nakit = new FisBacagi { Arac = "NAKIT", TutarTl = 100m };
        var kart = new FisBacagi { Arac = "KART" };
        Assert.True(FisHesap.NakitKapali(true, [kart, nakit]));
        // SATIS-3 t10 (hakem KRİTİK): 0 ₺'lik nakit satırı nakit kilidini TETİKLEMEZ ("0 ₺ nakitte de hata duruyor").
        Assert.False(FisHesap.NakitKapali(true, [kart, new FisBacagi { Arac = "NAKIT", TutarTl = 0m }]));
        Assert.False(FisHesap.NakitKapali(true, [kart]));
        Assert.False(FisHesap.NakitKapali(false, [nakit]));
        Assert.Equal(350m, FisHesap.BacakToplami([new FisBacagi { TutarTl = 100m }, new FisBacagi { TutarTl = 250m }]));
    }

    [Fact]
    public void FisHesap_bacak_kasasi_para_birimine_gore_TEK_ise_atanir()
    {
        List<KasaVeri> kasalar =
        [
            new("ks1", "Merkez", "TRY", true),
            new("ks2", "Dolar", "USD", true),
            new("ks3", "Avro 1", "EUR", true),
            new("ks4", "Avro 2", "EUR", true),
        ];
        var b = new FisBacagi { Arac = "NAKIT" };
        FisHesap.BacakKasaSabitle(b, kasalar);
        Assert.Equal("ks1", b.KasaId);

        b.Arac = "DOVIZ"; b.ParaBirimi = "USD";
        FisHesap.BacakKasaSabitle(b, kasalar);
        Assert.Equal("ks2", b.KasaId);

        b.ParaBirimi = "EUR";                       // iki avro kasası → VARSAYIM YOK
        Assert.Null(FisHesap.BacakKasaTek(b, kasalar));
        FisHesap.BacakKasaSabitle(b, kasalar);
        Assert.Equal("", b.KasaId);
        Assert.Equal(["ks3", "ks4"], FisHesap.BacakKasalari(b, kasalar).Select(k => k.Id));
        // KAT-1 (K5): ekranda TRY kodu yerine ₺ işareti (kurucu 24.09); değer kasa kimliği.
        Assert.Equal("Merkez (\u20ba)", FisHesap.KasaSecenekleri(kasalar)[0].Ad);
    }

    [Fact]
    public void FisHesap_POS_cihazi_tek_ise_atanir_KART_disinda_temizlenir()
    {
        var pc = new PosCihazSatir("pc1", "Ziraat POS", "Ziraat", "s1", "ks1", null, null, null, 1.5m, 1, "TRY", true, 0m, 0m);
        var b = new FisBacagi { Arac = "KART" };
        FisHesap.BacakPosSabitle(b, [pc]);
        Assert.Equal("pc1", b.PosCihazId);

        b.PosTaksitAdedi = 6;
        b.Arac = "NAKIT";
        FisHesap.BacakPosSabitle(b, [pc]);
        Assert.Equal("", b.PosCihazId);
        Assert.Equal(1, b.PosTaksitAdedi);
    }

    [Fact]
    public void HurdaGirisi_satici_kimligi_kurallari_sunucu_kapisinin_aynasi()
    {
        var h = new HurdaGirisi();
        Assert.Equal(T.SaticiTipiZorunlu, h.TipiHata(T));
        Assert.False(h.SaticiTamam(T));

        h.SaticiTipiSec("NIHAI_TUKETICI", null);
        Assert.Null(h.TipiHata(T));
        Assert.Equal(T.SaticiAdZorunlu, h.AdHata(T));
        Assert.Equal(T.SaticiTcknZorunlu, h.TcknHata(T));

        h.SaticiAd = "Ali Veli";
        h.SaticiTckn = "1234 567 890";               // 10 rakam
        Assert.Equal(T.SaticiTcknGecersiz, h.TcknHata(T));
        h.SaticiTckn = "123-456-789-50";             // boşluk/tire engellemez (TCKN-DOGRULAMA: kuraldan geçen sentetik numara)
        Assert.Equal("12345678950", h.TcknRakam);
        // SATIS-3 (KAYNAK 2 md.4): sunucu varsayılanı adres ZORUNLU (HurdaAyarlari.SaticiAdresiZorunlu,
        // VUK md.234) — ayna da adressiz tamam saymaz; yapılandırma kapalıysa adres istenmez.
        Assert.False(h.SaticiTamam(T));
        Assert.Equal("adres", h.AdresHata("adres"));
        h.SaticiAdres = "Kapalıçarşı No:1";
        Assert.True(h.SaticiTamam(T));
        h.SaticiAdres = null;
        h.AdresZorunlu = false;
        Assert.Null(h.AdresHata("adres"));
        Assert.True(h.SaticiTamam(T));

        h.SaticiTipiSec("MUKELLEF", null);
        Assert.Equal(T.TedarikciFaturaNoZorunlu, h.FaturaNoHata(T));
        Assert.Null(h.TcknHata(T));
        h.TedarikciFaturaNo = "ABC2026000001";
        Assert.True(h.SaticiTamam(T));
    }

    [Fact]
    public void HurdaGirisi_satici_adi_cariden_on_dolar_ama_yazilanin_ustune_yazmaz()
    {
        var h = new HurdaGirisi();
        h.SaticiTipiSec("ESNAF_MUAF", "Ayşe Demir");
        Assert.Equal("Ayşe Demir", h.SaticiAd);
        Assert.True(h.SaticiAdCariden);

        var h2 = new HurdaGirisi { SaticiAd = "Elle Yazılan" };
        h2.SaticiTipiSec("NIHAI_TUKETICI", "Ayşe Demir");
        Assert.Equal("Elle Yazılan", h2.SaticiAd);
        Assert.False(h2.SaticiAdCariden);

        h.FiyatGirildi(3000m);
        Assert.True(h.FiyatElle);
        h.Sifirla(2950m);
        Assert.Equal(("", null, false, false, 2950m, (decimal?)null),
            (h.SaticiTipi, h.SaticiAd, h.SaticiAdCariden, h.FiyatElle, h.Fiyat, h.Gram));
    }

    [Fact]
    public void TakasGirisi_fark_sunucu_formuluyle_ayni()
    {
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 10m, EskiFiyat = 3000m, YeniUrunId = "u1", YeniAdet = 2, YeniFiyat = 20000m };
        var has = HasHesap.ToHasGram(10m, Ayarlar.Bul("K22")!);
        Assert.Equal(has, t.EskiHas);
        Assert.Equal(has * 3000m, t.EskiDeger);
        Assert.Equal(40000m, t.YeniDeger);
        Assert.Equal(40000m - has * 3000m, t.Fark);        // pozitif → müşteri öder
        Assert.True(t.Tamam);

        t.YeniFiyat = null;
        Assert.Null(t.Fark);
        Assert.False(t.Tamam);

        t.Sifirla(0m);
        Assert.Equal(("", 1, (decimal?)null, (decimal?)null), (t.YeniUrunId, t.YeniAdet, t.EskiFiyat, t.EskiGram));
    }

    [Fact]
    public void TransferGirisi_ayni_subeye_ya_da_urunsuz_gonderilemez()
    {
        var t = new TransferGirisi { KaynakSubeId = "s1", HedefSubeId = "s1", UrunId = "u1", Adet = 1 };
        Assert.False(t.Tamam);
        t.HedefSubeId = "s2";
        Assert.True(t.Tamam);
        t.Adet = 0;
        Assert.False(t.Tamam);
        t.Sifirla();
        Assert.Equal(("", "", "", 1), (t.KaynakSubeId, t.HedefSubeId, t.UrunId, t.Adet));
    }

    [Fact]
    public void FisYaniti_fis_no_uyari_ve_SUNUCUNUN_iskontosu_okunur()
    {
        var y = JsonDocument.Parse("""{"fisId":"F-1","id":"H-1","yerlesimUyarisi":"Reyon dolu","iskontoToplam":150.5,"fisIskontoCaridenMi":true}""").RootElement;
        Assert.Equal("F-1", FisYaniti.FisNo(y));
        Assert.Equal("Reyon dolu", FisYaniti.YerlesimUyarisi(y));
        var not = FisYaniti.IskontoNotu(y, T)!;
        Assert.StartsWith(T.IskontoUygulandi, not);
        Assert.Contains(T.IskontoCariden, not);

        var sadeId = JsonDocument.Parse("""{"id":"H-2","iskontoToplam":0}""").RootElement;
        Assert.Equal("H-2", FisYaniti.FisNo(sadeId));
        Assert.Null(FisYaniti.IskontoNotu(sadeId, T));
        Assert.Null(FisYaniti.YerlesimUyarisi(sadeId));
        Assert.Null(FisYaniti.FisNo(JsonDocument.Parse("[]").RootElement));
    }

    [Fact]
    public void OdemeKirilimi_bilinmeyen_alan_tutucu_varsayilana_duser()
    {
        var k = new OdemeKirilimi();
        k.Ac("H-1");
        Assert.True(k.Yukleniyor);
        k.Oku(JsonDocument.Parse("""
            {"kalemKapsami":"KISMEN_BILINMIYOR","odemeKalemleri":[
              {"arac":"KART","tutarTl":100.25,"paraBirimi":"TRY","bilinmiyor":false,"geriDolduruldu":true},
              {"tutarTl":"bozuk","aktif":false}]}
            """).RootElement);
        Assert.Equal("KISMEN_BILINMIYOR", k.Kapsam);
        Assert.Equal(2, k.Kalemler.Count);
        Assert.Equal(new OdemeKalemiSatiri("KART", 100.25m, "TRY", false, true, true), k.Kalemler[0]);
        Assert.Equal(new OdemeKalemiSatiri("BILINMIYOR", 0m, "TRY", false, false, false), k.Kalemler[1]);

        k.Ac("H-2");                                  // yeni fişe önceki sonuç TAŞINMAZ
        Assert.Empty(k.Kalemler);
        Assert.Null(k.Kapsam);
    }
}

public sealed class S21FisYapiTestleri
{
    private static string Sayfa => CssKaynak.Metin("Kuyumcu.Web", "Pages", "Islemler.razor");

    private static IEnumerable<string> FisDosyalari =>
        Directory.GetFiles(CssKaynak.Yol("Kuyumcu.Web", "Bilesenler", "Fis"))
            .Where(d => d.EndsWith(".razor") || d.EndsWith(".cs"));

    /// <summary>Razor (@* *@) ve C# (// , /* */) yorumlarını atar — yorumdaki kelime ihlal sayılmaz.</summary>
    private static string Yorumsuz(string m) =>
        System.Text.RegularExpressions.Regex.Replace(
            System.Text.RegularExpressions.Regex.Replace(
                System.Text.RegularExpressions.Regex.Replace(m, @"@\*.*?\*@", "", System.Text.RegularExpressions.RegexOptions.Singleline),
                @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline),
            @"(?m)^\s*///?.*$", "");

    [Fact]
    public void Islemler_razor_1400_satiri_asmaz()
    {
        var satir = Sayfa.Split('\n').Length;
        Assert.True(satir <= 1400, $"Islemler.razor {satir} satır (kabul: ≤ 1.400 — PLAN § S-2 madde 2.1)");
    }

    [Fact]
    public void Fis_bilesenleri_ApiIstemci_enjekte_etmez_ve_istek_atmaz()
    {
        var dosyalar = FisDosyalari.ToList();
        Assert.True(dosyalar.Count >= 9, "Bilesenler/Fis altında beklenen bileşenler yok");
        foreach (var d in dosyalar)
        {
            var kod = Yorumsuz(File.ReadAllText(d));
            foreach (var yasak in new[] { "ApiIstemci", "HttpClient", "PostAsync", "PatchAsync", "PutAsync", "DeleteAsync", "GetAsync", "Api." })
                Assert.False(kod.Contains(yasak, StringComparison.Ordinal),
                    $"{Path.GetFileName(d)} '{yasak}' içeriyor — fiş bileşeni POST/istek atmaz (plan § 3 TEK POST YOLU)");
        }
    }

    [Fact]
    public void Tek_POST_yolu_Islemler_Kaydet_icinde_kalir()
    {
        var s = Sayfa;
        var bas = s.IndexOf("private async Task Kaydet()", StringComparison.Ordinal);
        var son = s.IndexOf("private string? SecilenReyon", StringComparison.Ordinal);
        Assert.True(bas > 0 && son > bas, "Kaydet() Islemler.razor'da yok");
        var postlar = System.Text.RegularExpressions.Regex.Matches(s, @"Api\.PostAsync").Select(m => m.Index).ToList();
        Assert.True(postlar.Count >= 7, $"Kaydet'in {postlar.Count} POST'u var (8 uç bekleniyordu)");
        Assert.All(postlar, i => Assert.InRange(i, bas, son));
    }

    /// <summary>S2-1 ÖNCESİ Islemler.razor'dan çıkarılan id envanteri (grep -o 'id="[^"]*"', tekil).</summary>
    private static readonly string[] OnceIdler =
    [
        "hata-bandi", "hurda-fiyat-kaynak", "hurda-satici", "hurda-satici-ad-hata", "hurda-satici-ad-kaynak",
        "hurda-satici-fatura-hata", "hurda-satici-not", "hurda-satici-tckn-hata", "hurda-satici-tip-hata",
        "islem-bacak-ekle", "islem-bacak-kalan-ata", "islem-cari-pasif-notu", "islem-fiyat-kaynak",
        "islem-iskonto-onizleme", "islem-kalem-ekle", "islem-kasa-tek", "islem-kaydet", "islem-kaydet-kilit",
        "islem-kirilim-kapsam", "islem-kirilim-liste", "islem-odeme-sade", "islem-pesin-hata", "islem-pesin-notu",
        "islem-reyon-hata", "islem-reyon-ipucu", "islem-reyon-tek", "islem-son-satislar", "islem-son-yenile",
        "islem-teklif", "islem-teklif-yok", "islem-tevsik-ipucu", "islem-yerlesim-uyari", "takas-eski-fiyat-kaynak",
    ];

    /// <summary>
    /// PAKET SATIS-3 (KAYNAK 2 md.1+2): bu rozetler artık ortak AlanKabugu'nun KaynakRozeti'yle
    /// etiket satırında çizilir; id = {ElemanId}-kaynak (DOM'da AYNI — SATIS3 ekran testleri bulur).
    /// </summary>
    private static readonly string[] KaynakRozetliIdler = ["hurda-fiyat-kaynak", "takas-eski-fiyat-kaynak", "hurda-satici-ad-kaynak"];

    [Fact]
    public void Id_envanteri_korunur_hicbir_id_silinmedi()
    {
        var hepsi = Sayfa + string.Concat(FisDosyalari.Select(File.ReadAllText));
        foreach (var id in OnceIdler)
            Assert.True(hepsi.Contains($"d=\"{id}\"", StringComparison.Ordinal)
                || hepsi.Contains($"HataKimligi=\"{id}\"", StringComparison.Ordinal)
                || (id == "hurda-satici-tip-hata" && hepsi.Contains("ElemanId=\"hurda-satici-tip\"", StringComparison.Ordinal))   // Secici hata id'si {ElemanId}-hata
                || (id.EndsWith("-kaynak") && hepsi.Contains($"ElemanId=\"{id[..^"-kaynak".Length]}\"", StringComparison.Ordinal)
                    && KaynakRozetliIdler.Contains(id)),
                id);   // id= ya da bileşen Id= · SATIS-3: alan hatası HataKimligi ile, kaynak rozeti AlanKabugu {ElemanId}-kaynak ile
        // Dinamik id kalıpları (satır başına) da korunur.
        foreach (var kalip in new[] { "islem-bacak-{ix}-arac", "islem-bacak-sil-{ix}", "islem-kalem-hediye-", "islem-iade-et-{h.Id}",
                                      "islem-storno-{h.Id}", "islem-kirilim-{h.Id}", "islem-tur-{deger}", "si-kalem-adet-{k.UrunId}" })
            Assert.Contains(kalip, hepsi, StringComparison.Ordinal);
    }

    [Fact]
    public void Fis_bilesenlerinde_satir_ici_style_artmadi_ve_emoji_yok()
    {
        var style = FisDosyalari.Sum(d => System.Text.RegularExpressions.Regex.Matches(Yorumsuz(File.ReadAllText(d)), "style=\"").Count);
        // Taşınan 5 satır içi style (bacak başlığı 1 · takas 4); flex:1 ×2 → .esnek, grid-column:span 1 kaldırıldı.
        Assert.True(style <= 5, $"Fis bileşenlerinde {style} satır içi style (≤ 5)");
        var kod = string.Concat(FisDosyalari.Select(d => Yorumsuz(File.ReadAllText(d))));
        Assert.DoesNotContain("🔎", kod);   // bileşen klasöründe emoji yasak → Ikon (IkonTestleri)
        Assert.DoesNotContain("✕", kod);
    }
}

public sealed class S21FisBilesenTestleri : BunitContext
{
    public S21FisBilesenTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new BosApi()) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
    }

    /// <summary>Fiş bileşenleri istek ATMAMALI: gelen her istek kayda düşer ve test onu reddeder.</summary>
    private sealed class BosApi : HttpMessageHandler
    {
        public static int IstekSayisi;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            Interlocked.Increment(ref IstekSayisi);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("{}") });
        }
    }

    [Fact]
    public void Bacak_ekle_kalani_onerir_tek_kasayi_atar_ve_SAYFAYA_haber_verir()
    {
        var bacaklar = new List<FisBacagi>();
        var haber = 0;
        var cut = Render<FisOdemeBacaklari>(p => p
            .Add(x => x.Bacaklar, bacaklar)
            .Add(x => x.Kasalar, [new KasaVeri("ks1", "Merkez", "TRY", true)])
            .Add(x => x.PosCihazlari, [])
            .Add(x => x.FisToplami, 1000m)
            .Add(x => x.Degisti, EventCallback.Factory.Create(this, () => haber++)));

        cut.Find("#islem-bacak-ekle").Click();

        Assert.Single(bacaklar);
        Assert.Equal(1000m, bacaklar[0].TutarTl);
        Assert.Equal("ks1", bacaklar[0].KasaId);
        Assert.Equal(1, haber);

        cut.Find("#islem-bacak-sil-0").Click();
        Assert.Empty(bacaklar);
        Assert.Equal(2, haber);
    }

    [Fact]
    public void Kalani_ata_son_bacagi_fis_toplamina_tamamlar()
    {
        var bacaklar = new List<FisBacagi> { new() { Arac = "NAKIT", TutarTl = 400m, KasaId = "ks1" } };
        var haber = 0;
        var cut = Render<FisOdemeBacaklari>(p => p
            .Add(x => x.Bacaklar, bacaklar)
            .Add(x => x.Kasalar, [new KasaVeri("ks1", "Merkez", "TRY", true)])
            .Add(x => x.PosCihazlari, [])
            .Add(x => x.FisToplami, 1000m)
            .Add(x => x.Degisti, EventCallback.Factory.Create(this, () => haber++)));

        cut.Find("#islem-bacak-kalan-ata").Click();

        Assert.Equal(1000m, bacaklar[0].TutarTl);
        Assert.Equal(1, haber);
        Assert.Empty(cut.FindAll("#islem-bacak-kalan-ata"));   // kalan 0 → düğme kalkar
    }

    [Fact]
    public void Hurda_satici_tipi_secilince_ad_cariden_dolar_ve_sayfaya_haber_verir()
    {
        var hurda = new HurdaGirisi();
        var haber = 0;
        var cut = Render<FisHurdaAlis>(p => p
            .Add(x => x.Hurda, hurda)
            .Add(x => x.Ayarlar, Ayarlar.EkranSecenekleri)
            .Add(x => x.CariAdi, "Ayşe Demir")
            .Add(x => x.Degisti, EventCallback.Factory.Create(this, () => haber++)));

        // SATIS-3 doğrulama: açılışta, dokunulmadan kırmızı mesaj YOK; formla ilk etkileşimden sonra
        // boş zorunlu alanın altında mesaj görünür (DoD EK md.3 — sessiz etkisizlik yok).
        Assert.Empty(cut.FindAll("#hurda-satici-tip-hata"));
        cut.Find("#hurda-gram").Input("5");
        Assert.NotNull(cut.Find("#hurda-satici-tip-hata"));
        SeciciYardim.Sec(cut, "hurda-satici-tip", "NIHAI_TUKETICI");

        Assert.Equal("NIHAI_TUKETICI", hurda.SaticiTipi);
        Assert.Equal("Ayşe Demir", hurda.SaticiAd);
        Assert.True(haber >= 1);
        Assert.NotNull(cut.Find("#hurda-satici-ad-kaynak"));
        Assert.Empty(cut.FindAll("#hurda-satici-tip-hata"));
    }

    [Fact]
    public void Fis_bilesenleri_cizilirken_istek_atmaz()
    {
        var once = BosApi.IstekSayisi;
        Render<FisTakas>(p => p
            .Add(x => x.Takas, new TakasGirisi { EskiGram = 10m, EskiFiyat = 3000m, YeniFiyat = 50000m })
            .Add(x => x.Ayarlar, Ayarlar.EkranSecenekleri)
            .Add(x => x.UrunSecenekleri, []));
        Render<FisSonSatislar>(p => p
            .Add(x => x.Satislar, [])
            .Add(x => x.Yuklendi, true)
            .Add(x => x.Kirilim, new OdemeKirilimi()));
        Render<FisIade>(p => p.Add(x => x.Adaylar, []));
        Render<FisTransfer>(p => p
            .Add(x => x.Transfer, new TransferGirisi())
            .Add(x => x.Subeler, [])
            .Add(x => x.UrunSecenekleri, []));
        Assert.Equal(once, BosApi.IstekSayisi);
    }
}

/// <summary>
/// Sayfa düzeyi: fiş bileşeni sayfaya haber verince KAYDET kilidi doğru hesaplanır.
/// (M-S21-10 test açığını kapatır: takas kilidi takas.Tamam'a bağlı.)
/// </summary>
public sealed class S21FisSayfaTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public S21FisSayfaTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            var g = yol switch
            {
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-16T09:00:00Z","ayarFiyatlari":[]}""",
                var y when y.Contains("/fiyat/teklif") => """{"teklifVar":false,"sebep":"LISTE_YOK"}""",
                var y when y.Contains("/urunler") =>
                    """[{"id":"u1","ad":"Burma Bilezik","kod":"K-1","tip":"ZIYNET","ayarId":"K22","gramAgirlik":10,"tasAgirlik":0,"hasGrami":9.16,"iscilikTl":500,"satisFiyati":null,"aktif":true}]""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                var y when y.Contains("/masak/esikler") =>
                    """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var y when y.Contains("/iade-adaylari") => "[]",
                var y when y.Contains("/pos/cihazlar") => "[]",
                var y when y.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","sistemCarisi":true}""",
                var y when y.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    [Fact]
    public void Takas_eksikken_KAYDET_kilitli_bilesen_doldurunca_ACILIR()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-cari");
        e.Find("#islem-tur-TAKAS").Click();
        e.WaitForAssertion(() => Assert.Equal("6.800", e.Find("#takas-eski-fiyat").GetAttribute("value")));   // BICIM-2: odak dışı kutu gruplu
        SeciciYardim.Sec(e, "islem-cari", "c1");

        Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled"), "eski gram / yeni ürün yokken Kaydet açık");

        e.Find("#takas-eski-gram").Input("10");
        SeciciYardim.Sec(e, "takas-yeni-urun", "u1");
        e.Find("#takas-yeni-fiyat").Input("20000");
        ParaDemoEkranYardim.TakasSaticiDoldur(e);   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu

        e.WaitForAssertion(() => Assert.False(e.Find("#islem-kaydet").HasAttribute("disabled"),
            "takas dolduğu hâlde Kaydet kilitli — bileşen sayfaya haber vermiyor"));
        Assert.NotNull(e.Find("#takas-eski-fiyat-kaynak"));
    }
}
