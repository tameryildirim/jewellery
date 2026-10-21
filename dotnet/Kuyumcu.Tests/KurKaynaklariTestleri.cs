using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Kur;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MFatura = Kuyumcu.Api.Modules.Fatura;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// KUR KAYNAKLARI PAKETİ (kurucu kararları 17.10 — KARAR-MERKEZI-MIMARI § 9 md.1–3, 6)
//   1) Harem ayrıştırıcısı GERÇEK yükle (17.10.2026 17:34'te tek bağlantıyla alınan ilk
//      price_changed mesajından kısaltıldı; kişisel veri içermez).
//   2) EVDS / TCMB döviz ayrıştırıcıları, önbellek, birleştirme, çekim planı.
//   3) Has bedeli (KDVK 23/e) kaynağı: son işgünü kapanışı, fail-closed, faturaya kod.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KurKaynaklariSafTestleri
{
    /// <summary>Gerçek yük (kısaltılmış): dizge ve sayı karışık alis/satis, sayı kapanis, yayın dışı kod 0.</summary>
    public const string HaremGercekYuk = """
["price_changed",{"meta":{"time":1792233585986},"data":{"USDTRY":{"code":"USDTRY","alis":"49.4400","satis":"49.5000","tarih":"17-10-2026 13:39:44","dir":{"alis_dir":"","satis_dir":""},"dusuk":"49.4720","yuksek":"49.5000","kapanis":49.256},"ALTIN":{"code":"ALTIN","alis":"6601.950","satis":"6624.010","tarih":"17-10-2026 13:39:44","dir":{"alis_dir":"","satis_dir":""},"dusuk":"6615.040","yuksek":"6624.010","kapanis":6542.58},"OMRUSD":{"alis":0,"satis":0,"tarih":"","dusuk":null,"yuksek":null,"kapanis":2.591,"dir":{"alis_dir":"","satis_dir":""}},"EURTRY":{"code":"EURTRY","alis":"55.4380","satis":"55.5900","tarih":"17-10-2026 13:39:44","dir":{"alis_dir":"","satis_dir":""},"dusuk":"55.5440","yuksek":"55.5900","kapanis":55.12},"GBPTRY":{"code":"GBPTRY","alis":65.549,"satis":65.853,"tarih":"17-10-2026 13:39:44","dir":{"alis_dir":"","satis_dir":""},"dusuk":65.816,"yuksek":65.853,"kapanis":64.955},"AYAR22":{"code":"AYAR22","alis":"6005.770","satis":"6206.560","tarih":"17-10-2026 13:39:44","dir":{"alis_dir":"","satis_dir":""},"dusuk":"6198.140","yuksek":"6206.560","kapanis":6130.03},"KULCEALTIN":{"code":"KULCEALTIN","alis":6568.94,"satis":6643.88,"tarih":"17-10-2026 13:39:44","dir":{"alis_dir":"","satis_dir":""},"dusuk":6634.89,"yuksek":6643.88,"kapanis":6562.21},"CEYREK_YENI":{"code":"CEYREK_YENI","alis":10761,"satis":10829,"tarih":"17-10-2026 13:39:44","dir":{"alis_dir":"","satis_dir":""},"dusuk":10814,"yuksek":10829,"kapanis":10696},"CEYREK_ESKI":{"code":"CEYREK_ESKI","alis":10550,"satis":10637,"tarih":"17-10-2026 13:39:44","dir":{"alis_dir":"","satis_dir":""},"dusuk":10622,"yuksek":10637,"kapanis":10571},"ATA5_ESKI":{"code":"ATA5_ESKI","alis":217534,"satis":219636,"tarih":"17-10-2026 13:39:44","dir":{"alis_dir":"","satis_dir":""},"dusuk":219339,"yuksek":219636,"kapanis":216937},"PLATIN":{"code":"PLATIN","alis":47280,"satis":52820,"tarih":"17-10-2026 13:39:44","dir":{"alis_dir":"","satis_dir":""},"dusuk":52820,"yuksek":52820,"kapanis":52580}}}]
""";

    [Fact]
    public void Harem_gercek_yuk_has_doviz_ve_pariteleri_dogru_okur()
    {
        var v = HaremAyristirici.Ayristir(HaremGercekYuk);

        Assert.Equal("HAREM", v.Kaynak);
        Assert.Equal(6624.01m, v.AltinHasFiyati);       // "6624.010" dizge, nokta ondalık
        Assert.Equal(49.5m, v.UsdTry);
        Assert.Equal(55.59m, v.EurTry);
        Assert.Equal(65.853m, v.GbpTry);                // sayı olarak gelen alan
        Assert.Null(v.GramAltinFiyati);                 // Harem'de "gram altın" kodu yok — uydurulmaz

        var p = v.Pariteler!.ToDictionary(x => x.Kod);
        Assert.Equal((10761m, 10829m), (p[PariteKodlari.CeyrekYeni].Alis!.Value, p[PariteKodlari.CeyrekYeni].Satis!.Value));
        Assert.Equal(10637m, p[PariteKodlari.CeyrekEski].Satis);
        Assert.Equal(6206.56m, p[PariteKodlari.Bilezik22].Satis);
        Assert.Equal(PariteKodlari.BirimGram, p[PariteKodlari.Bilezik22].Birim);
        Assert.Equal(6643.88m, p[PariteKodlari.Kulce].Satis);
        Assert.Equal(6601.95m, p[PariteKodlari.Has].Alis);
        // Eşlemesi olmayan kod (ATA5_ESKI, PLATIN) parite listesine GİRMEZ.
        Assert.Equal(5, p.Count);   // Has · Külçe · Yeni/Eski çeyrek · 22 ayar
    }

    [Fact]
    public void Harem_kapanisi_yayin_gunu_ve_has_satis_ile_kurulur_kapanis_alani_dayanakta()
    {
        var k = HaremAyristirici.Ayristir(HaremGercekYuk).Kapanis!;
        Assert.Equal(new DateOnly(2026, 10, 17), k.Gun);
        Assert.Equal(6624.01m, k.Fiyat);
        Assert.Contains("6542.58", k.Dayanak);          // Harem "kapanis" alanı yalnız çapraz kontrol
        Assert.Contains("17.10.2026 13:39", k.Dayanak);
    }

    [Fact]
    public void Harem_baska_olay_ya_da_has_yoksa_HATA_sessiz_sifir_yok()
    {
        Assert.Throws<InvalidOperationException>(() =>
            HaremAyristirici.Ayristir("""["baska_olay",{"data":{}}]"""));
        Assert.Throws<InvalidOperationException>(() =>
            HaremAyristirici.Ayristir("""["price_changed",{"data":{"USDTRY":{"alis":"1","satis":"2"}}}]"""));
        Assert.Throws<InvalidOperationException>(() =>
            HaremAyristirici.Ayristir("""["price_changed",{"data":{"ALTIN":{"alis":0,"satis":0,"tarih":""}}}]"""));
    }

    [Theory]
    [InlineData("0{\"sid\":\"x\",\"upgrades\":[],\"pingInterval\":25000}", HaremCerceveTuru.Acilis)]
    [InlineData("40{\"sid\":\"y\"}", HaremCerceveTuru.Baglandi)]
    [InlineData("40", HaremCerceveTuru.Baglandi)]
    [InlineData("2", HaremCerceveTuru.Ping)]
    [InlineData("42[\"price_changed\",{}]", HaremCerceveTuru.Olay)]
    [InlineData("3", HaremCerceveTuru.Bilinmeyen)]
    public void Engine_IO_cerceve_turleri(string cerceve, HaremCerceveTuru beklenen) =>
        Assert.Equal(beklenen, HaremCerceve.Tur(cerceve));

    [Fact]
    public void Altinkaynak_IAB_kapanisi_gercek_satirdan_gun_ve_fiyat()
    {
        const string altin = """
[{"Alis":"6.587","Satis":"6.640","Kod":"HH_T","Aciklama":"Has","GuncellenmeZamani":"17.10.2026 16:48:43","IsMobile":true},
 {"Alis":"6.622,00","Satis":"6.622,00","Kod":"IAB_KAPANIS","Aciklama":"İAB Kapanış","GuncellenmeZamani":"16.10.2026 00:00:00","IsMobile":true}]
""";
        var k = AltinkaynakAdaptoru.Kapanis(altin)!;
        Assert.Equal(new DateOnly(2026, 10, 16), k.Gun);
        Assert.Equal(6622.00m, k.Fiyat);
        Assert.Null(AltinkaynakAdaptoru.Kapanis("""[{"Alis":"1","Satis":"2","Kod":"HH_T"}]"""));
        Assert.Equal(6640m, AltinkaynakAdaptoru.Kalemler(altin)["HH_T"].Satis);
    }

    [Fact]
    public void EVDS_son_dolu_satiri_alir_tatil_gunu_bos_satiri_atlar()
    {
        const string json = """
{"totalCount":3,"items":[
 {"Tarih":"14-10-2026","TP_DK_USD_S":"49.1000","TP_DK_EUR_S":"55.0000","TP_DK_GBP_S":"65.0000"},
 {"Tarih":"15-10-2026","TP_DK_USD_S":"49.2345","TP_DK_EUR_S":55.4321,"TP_DK_GBP_S":"65.4321"},
 {"Tarih":"16-10-2026","TP_DK_USD_S":null,"TP_DK_EUR_S":null,"TP_DK_GBP_S":null}]}
""";
        var v = EvdsAyristirici.Ayristir(json);
        Assert.Equal("EVDS", v.Kaynak);
        Assert.Equal(49.2345m, v.UsdTry);
        Assert.Equal(55.4321m, v.EurTry);
        Assert.Equal(65.4321m, v.GbpTry);
        Assert.Equal(new DateOnly(2026, 10, 15), v.Gun);
        Assert.Throws<InvalidOperationException>(() => EvdsAyristirici.Ayristir("""{"items":[]}"""));
        Assert.Throws<InvalidOperationException>(() => EvdsAyristirici.Ayristir("""{"status":"403"}"""));
    }

    [Fact]
    public void EVDS_adresi_seri_kodlarini_tasir_anahtar_ADRESTE_DEGIL()
    {
        var a = EvdsAyristirici.Adres(new DateOnly(2026, 10, 17));
        Assert.StartsWith("https://evds3.tcmb.gov.tr/igmevdsms-dis/series=TP.DK.USD.S-TP.DK.EUR.S-TP.DK.GBP.S", a);
        Assert.Contains("startDate=07-10-2026&endDate=17-10-2026&type=json", a);
        Assert.DoesNotContain("key", a, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TCMB_today_xml_satis_kurlari()
    {
        const string xml = """
<Tarih_Date Tarih="16.10.2026" Date="10/16/2026" Bulten_No="2026/196">
 <Currency CrossOrder="0" Kod="USD" CurrencyCode="USD"><ForexBuying>49.1</ForexBuying><ForexSelling>49.2</ForexSelling></Currency>
 <Currency CrossOrder="9" Kod="EUR" CurrencyCode="EUR"><ForexSelling>55.3</ForexSelling></Currency>
</Tarih_Date>
""";
        var v = TcmbAyristirici.Ayristir(xml);
        Assert.Equal((49.2m, 55.3m, (decimal?)null), (v.UsdTry!.Value, v.EurTry!.Value, v.GbpTry));
        Assert.Equal(new DateOnly(2026, 10, 16), v.Gun);
    }

    // ── 21.10 kurucu "döviz alışını da ekle": her kaynak alış kurunu da taşır ──

    [Fact]
    public void Doviz_alisi_Harem_TCMB_EVDS_ayristiricilarindan_okunur()
    {
        var h = HaremAyristirici.Ayristir(HaremGercekYuk);
        Assert.Equal((49.44m, 55.438m, 65.549m), (h.Alislar!.Usd!.Value, h.Alislar.Eur!.Value, h.Alislar.Gbp!.Value));

        var t = TcmbAyristirici.Ayristir("""
<Tarih_Date Tarih="16.10.2026" Date="10/16/2026"><Currency Kod="USD"><ForexBuying>49.1</ForexBuying><ForexSelling>49.2</ForexSelling></Currency><Currency Kod="EUR"><ForexSelling>55.3</ForexSelling></Currency></Tarih_Date>
""");
        Assert.Equal(49.1m, t.Alislar!.Usd);
        Assert.Null(t.Alislar.Eur);   // yayımlanmayan alış UYDURULMAZ (satıştan türetilmez)

        var e = EvdsAyristirici.Ayristir("""
{"items":[{"Tarih":"15-10-2026","TP_DK_USD_S":"49.2345","TP_DK_EUR_S":"55.4","TP_DK_GBP_S":"65.4","TP_DK_USD_A":"49.1500","TP_DK_EUR_A":"55.3","TP_DK_GBP_A":null}]}
""");
        Assert.Equal((49.15m, 55.3m, (decimal?)null), (e.Alislar!.Usd!.Value, e.Alislar.Eur!.Value, e.Alislar.Gbp));
        Assert.Contains("TP.DK.USD.A-TP.DK.EUR.A-TP.DK.GBP.A", EvdsAyristirici.Adres(new DateOnly(2026, 10, 17)));
    }

    [Fact]
    public void Birlestir_doviz_alisini_dovizle_AYNI_kaynaktan_tasir_okunamazsa_bos()
    {
        var altin = Altin() with { Alislar = new DovizAlislari(39m, 44m, 49m) };
        Assert.Equal(39m, KurServisi.Birlestir(altin, KurKaynakKodlari.DovizAltinKaynagi, null, null).Alislar!.Usd);
        var evds = KurServisi.Birlestir(altin, "EVDS", null, new DovizVerisi(49.2m, 55.4m, 65.4m, "EVDS", null, new DovizAlislari(49.1m, null, null)));
        Assert.Equal((49.1m, (decimal?)null), (evds.Alislar!.Usd!.Value, evds.Alislar.Eur));   // altın kaynağının alışı KARIŞMAZ
        Assert.Null(KurServisi.Birlestir(altin, "EVDS", null, null).Alislar);
    }

    private static DisKurVerisi Altin() => new(6600m, 6650m, 40m, 45m, 50m, "ALTINKAYNAK");

    [Fact]
    public void Birlestir_altin_kaynagiyla_ayni_ise_kendi_dovizi_ve_kodu()
    {
        var v = KurServisi.Birlestir(Altin(), KurKaynakKodlari.DovizAltinKaynagi, null, null);
        Assert.Equal((40m, 45m, 50m), (v.UsdTry!.Value, v.EurTry!.Value, v.GbpTry!.Value));
        Assert.Equal("ALTINKAYNAK", v.DovizKaynak);
    }

    [Fact]
    public void Birlestir_EVDS_dovizi_altin_verisine_yazar_altin_degismez()
    {
        var v = KurServisi.Birlestir(Altin(), "EVDS", null, new DovizVerisi(49.2m, 55.4m, 65.4m, "EVDS"));
        Assert.Equal(6600m, v.AltinHasFiyati);
        Assert.Equal("ALTINKAYNAK", v.Kaynak);              // tazelik kapısı altın kaynağına bakar
        Assert.Equal((49.2m, 55.4m, 65.4m), (v.UsdTry!.Value, v.EurTry!.Value, v.GbpTry!.Value));
        Assert.Equal("EVDS", v.DovizKaynak);
    }

    [Fact]
    public void Birlestir_doviz_okunamadiysa_baska_kaynagin_kuru_KONMAZ()
    {
        var v = KurServisi.Birlestir(Altin(), "EVDS", null, null);
        Assert.Null(v.UsdTry); Assert.Null(v.EurTry); Assert.Null(v.GbpTry);
        Assert.Equal("EVDS", v.DovizKaynak);
    }

    [Theory]
    [InlineData(6_612_345, "TL_KG", 6612.345)]
    [InlineData(6_612_345.6789, "TL_KG", 6612.3457)]
    [InlineData(6612.3456, "TL_GRAM", 6612.3456)]
    public void Kapanis_TL_kg_bin_e_bolunerek_has_grama_cevrilir(decimal deger, string birim, decimal beklenen) =>
        Assert.Equal(beklenen, OzelMatrahKapanisServisi.GramaCevir(deger, birim));

    // ── önbellek ──

    private sealed class ElleSaat(DateTimeOffset t) : TimeProvider
    {
        public DateTimeOffset Simdi = t;
        public override DateTimeOffset GetUtcNow() => Simdi;
    }

    private sealed class SayanDoviz : IDovizAdaptoru
    {
        public int Cagri;
        public bool Bozuk;
        public string Kod => "EVDS";
        public Task<DovizVerisi> DovizCek(CancellationToken iptal = default)
        {
            Cagri++;
            return Bozuk ? throw new HttpRequestException("EVDS HTTP 503")
                : Task.FromResult(new DovizVerisi(40m + Cagri, null, null, "EVDS"));
        }
    }

    [Fact]
    public async Task Gunluk_doviz_15_dk_onbellekli_hata_24_saat_icinde_son_degeri_doner_sonra_HATA()
    {
        var saat = new ElleSaat(new DateTimeOffset(2026, 10, 17, 9, 0, 0, TimeSpan.Zero));
        var ic = new SayanDoviz();
        var a = new OnbellekliDovizAdaptoru(ic, saat);

        Assert.Equal(41m, (await a.DovizCek()).UsdTry);
        saat.Simdi = saat.Simdi.AddMinutes(10);
        Assert.Equal(41m, (await a.DovizCek()).UsdTry);
        Assert.Equal(1, ic.Cagri);                                  // kiracı sayısı kadar istek yok

        saat.Simdi = saat.Simdi.AddMinutes(10);
        ic.Bozuk = true;
        Assert.Equal(41m, (await a.DovizCek()).UsdTry);             // geçici hata: son değer
        saat.Simdi = saat.Simdi.AddHours(25);
        await Assert.ThrowsAsync<HttpRequestException>(() => a.DovizCek());   // 24 saat sonra fail-closed
    }

    // ── defter + çekim planı ──

    private sealed class AgsizFabrika : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new Engel());
        private sealed class Engel : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal) =>
                throw new InvalidOperationException("Bu testte ağ çağrısı beklenmez: " + istek.RequestUri);
        }
    }

    public static KurKaynakDefteri Defter(bool sozlesmesiz, string? evdsAnahtar) =>
        new(new AgsizFabrika(), NullLogger<KurKaynakDefteri>.Instance,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kur:SozlesmesizKaynaklarAcik"] = sozlesmesiz ? "true" : "false",
                ["Kur:EvdsAnahtar"] = evdsAnahtar,
            }).Build());

    [Fact]
    public void EVDS_anahtarsiz_secilemez_anahtarla_varsayilan_olur_anahtar_DEGERI_disari_cikmaz()
    {
        var yok = Defter(sozlesmesiz: true, evdsAnahtar: null);
        Assert.Equal("ANAHTAR_YOK", yok.DovizEngeli("EVDS"));
        Assert.Equal(KurKaynakKodlari.DovizAltinKaynagi, yok.VarsayilanDovizKod);
        Assert.False(yok.EvdsAnahtarVar);

        var var_ = Defter(sozlesmesiz: false, evdsAnahtar: "gizli-deger-123");
        Assert.Null(var_.DovizEngeli("EVDS"));                        // EVDS sözleşmesiz kapıya bağlı DEĞİL
        Assert.Equal("EVDS", var_.VarsayilanDovizKod);
        Assert.Equal("SOZLESMESIZ_KAPALI", var_.DovizEngeli("HAREM"));
        Assert.Equal("SOZLESMESIZ_KAPALI", var_.DovizEngeli("TCMB"));
        Assert.Equal("SOZLESMESIZ_KAPALI", var_.OzelMatrahEngeli("HAREM"));
        Assert.Null(var_.OzelMatrahEngeli("BIST"));
        var json = JsonSerializer.Serialize(var_);
        Assert.DoesNotContain("gizli-deger-123", json);
    }

    [Fact]
    public void Cekim_plani_kaynak_basina_TEK_cekim_kiraci_sayisindan_bagimsiz()
    {
        var defter = Defter(sozlesmesiz: true, evdsAnahtar: "x");
        var kiracilar = Enumerable.Range(0, 50).Select(i => new KiraciKurAyari($"t{i}",
                i % 2 == 0 ? "ALTINKAYNAK" : "TRUNCGIL",
                i % 3 == 0 ? "EVDS" : i % 3 == 1 ? "HAREM" : KurKaynakKodlari.DovizAltinKaynagi,
                i % 5 == 0 ? "ALTINKAYNAK" : "BIST"))
            .ToList();
        var plan = KurCekimPlani.Kur(kiracilar, defter);
        Assert.Equal(["ALTINKAYNAK", "HAREM", "TRUNCGIL"], plan.AltinKodlari);   // HAREM döviz için
        Assert.Equal(["EVDS"], plan.GunlukDovizKodlari);
    }
}

[Collection("veritabani")]
public sealed class KurKaynaklariVeriTestleri(TestVeritabani vt)
{
    private const decimal KurHas = 6_827.20m;          // TestKurulum.KiraciKur
    private static decimal Y2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);
    private static DateOnly Bugun => Zaman.YerelBugun(DateTime.UtcNow);

    private static void KaynakYaz(KuyumcuDbContext db, string tenantId, string? kod)
    {
        db.Tenants.Single(t => t.Id == tenantId).OzelMatrahKaynagi = kod;
        db.SaveChanges();
    }

    private static void KapanisYaz(KuyumcuDbContext db, string tenantId, string kaynak, DateOnly gun, decimal fiyat)
    {
        db.OzelMatrahKapanislari.Add(new OzelMatrahKapanisi
        {
            Id = Kimlik.Ulid(), TenantId = tenantId, Kaynak = kaynak, Gun = gun, Fiyat = fiyat,
            GirilenDeger = fiyat, GirilenBirim = "TL_GRAM", Dayanak = "test", Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    private static MFatura.FaturaOlusturDto KuyumKalemli(Kiraci k, decimal toplam) => new()
    {
        CariId = k.CariId, Toplam = toplam, KdvOran = 0.20m, KdvTuru = "KUYUM",
        Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 1m, BirimFiyat = toplam }],
    };

    [Fact]
    public async Task Cozucu_son_isgunu_kapanisi_bugunu_ve_10_gunden_eskiyi_KULLANMAZ()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "KK çözücü");
        KaynakYaz(db, k.TenantId, "BIST");
        KapanisYaz(db, k.TenantId, "BIST", Bugun, 7_000m);                // bugünkü: KULLANILMAZ (son işgünü < fatura günü)
        KapanisYaz(db, k.TenantId, "BIST", Bugun.AddDays(-11), 6_000m);   // 10 günden eski
        KapanisYaz(db, k.TenantId, "ELLE", Bugun.AddDays(-1), 6_100m);    // başka kaynak

        var c = await OzelMatrahKaynakCozucu.Coz(db, k.TenantId, DateTime.UtcNow);
        Assert.Equal("BIST", c.Kod);
        Assert.Null(c.KapanisFiyati);
        var hata = Assert.Throws<BadRequestException>(() => c.Fiyat(KurHas, "kur yok"));
        Assert.Contains("BIST kapanışı bulunamadı", hata.Message);

        KapanisYaz(db, k.TenantId, "BIST", Bugun.AddDays(-3), 6_500m);
        var c2 = await OzelMatrahKaynakCozucu.Coz(db, k.TenantId, DateTime.UtcNow);
        Assert.Equal(Bugun.AddDays(-3), c2.KapanisGunu);
        Assert.Equal(6_500m, c2.Fiyat(KurHas, "kur yok"));
    }

    [Fact]
    public async Task Eski_kiraci_bos_sutun_ISLEM_KURU_bugunku_davranis_ve_faturaya_kod()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KK eski kiracı");
        using var db = vt.Olustur(k);

        var cevap = await new MFatura.FaturaServisi(db).Olustur(KuyumKalemli(k, 80_000m), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var f = kontrol.Faturalar.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(Y2(k.UrunHasGrami * KurHas), f.HasDeger);
        Assert.Equal("ISLEM_KURU", f.OzelMatrahKaynakKodu);
        Assert.Null(f.OzelMatrahKapanisGunu);
        Assert.Null(f.OzelMatrahKapanisFiyati);
        Assert.Equal("ISLEM_KURU", TestKurulum.Metin(cevap, "OzelMatrahKaynakKodu"));
    }

    [Fact]
    public async Task BIST_secili_faturada_son_isgunu_kapanisi_kullanilir_kod_gun_fiyat_yazilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KK BIST fatura");
        KaynakYaz(kurulum, k.TenantId, "BIST");
        var gun = Bugun.AddDays(-2);
        KapanisYaz(kurulum, k.TenantId, "BIST", gun, 6_600.5m);
        using var db = vt.Olustur(k);

        var cevap = await new MFatura.FaturaServisi(db).Olustur(KuyumKalemli(k, 80_000m), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var f = kontrol.Faturalar.Single(x => x.TenantId == k.TenantId);
        var kalem = kontrol.FaturaKalemleri.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(Y2(k.UrunHasGrami * 6_600.5m), f.HasDeger);             // kur kaydı (6.827,20) DEĞİL
        Assert.Equal(6_600.5m, kalem.HasBirimFiyat);
        Assert.Equal("BIST", f.OzelMatrahKaynakKodu);
        Assert.Equal(gun, f.OzelMatrahKapanisGunu);
        Assert.Equal(6_600.5m, f.OzelMatrahKapanisFiyati);
        Assert.Equal("BIST", TestKurulum.Metin(cevap, "OzelMatrahKaynakKodu"));
        Assert.Equal(gun.ToString("yyyy-MM-dd"), TestKurulum.Metin(cevap, "OzelMatrahKapanisGunu"));
    }

    [Fact]
    public async Task BIST_kapanisi_yoksa_fatura_KESILMEZ_fail_closed_sebep_ve_yol()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KK BIST yok");
        KaynakYaz(kurulum, k.TenantId, "BIST");
        using var db = vt.Olustur(k);

        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            new MFatura.FaturaServisi(db).Olustur(KuyumKalemli(k, 80_000m), k.Islem));
        Assert.Contains("BIST kapanışı bulunamadı", hata.Message);
        Assert.Contains("kapanışını girin", hata.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, kontrol.Faturalar.Count(x => x.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Binde_yontemli_urunde_kapanis_ARANMAZ_kod_BINDE()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KK binde");
        KaynakYaz(kurulum, k.TenantId, "BIST");                 // kapanış YOK — binde yine kesilir
        kurulum.Urunler.Single(u => u.Id == k.UrunId).KdvMatrahBinde = 1m;
        kurulum.SaveChanges();
        using var db = vt.Olustur(k);

        await new MFatura.FaturaServisi(db).Olustur(KuyumKalemli(k, 150_000m), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var f = kontrol.Faturalar.Single(x => x.TenantId == k.TenantId);
        Assert.Equal("BINDE", f.OzelMatrahKaynakKodu);
        Assert.Equal(30.00m, f.KdvTutar);                       // matrah 150.000 × 1/1000 = 150, KDV %20 = 30
        Assert.Null(f.OzelMatrahKapanisGunu);
    }

    [Fact]
    public async Task Elle_kapanis_TL_kg_cevrilir_duzeltilir_faturada_kullanilinca_DEGISMEZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KK kapanış girişi");
        KaynakYaz(kurulum, k.TenantId, "BIST");
        var gun = Bugun.AddDays(-1);
        while (gun.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) gun = gun.AddDays(-1);

        using (var db = vt.Olustur(k))
        {
            var s = new OzelMatrahKapanisServisi(db, TestKurulum.Denetim(db), NullLogger<OzelMatrahKapanisServisi>.Instance);
            var c = await s.Gir(new KapanisGirDto { Kaynak = "BIST", Gun = gun, Deger = 6_700_000m, Birim = "TL_KG" }, k.Islem);
            Assert.Equal(6_700m, c.Fiyat);
            Assert.Equal("TL_KG", c.GirilenBirim);
            Assert.Contains("TL/kg ÷ 1000", c.Dayanak);

            // düzeltme (faturada kullanılmadı): aynı satır güncellenir
            var d = await s.Gir(new KapanisGirDto { Kaynak = "BIST", Gun = gun, Deger = 6_710m, Birim = "TL_GRAM" }, k.Islem);
            Assert.Equal(c.Id, d.Id);
            Assert.Equal(6_710m, d.Fiyat);

            // TL/kg değeri TL/g diye yazılırsa (birim hatası) bant kapısı durdurur
            await Assert.ThrowsAnyAsync<Exception>(() =>
                s.Gir(new KapanisGirDto { Kaynak = "BIST", Gun = gun, Deger = 6_710_000m, Birim = "TL_GRAM" }, k.Islem));
        }

        using (var db = vt.Olustur(k))
            await new MFatura.FaturaServisi(db).Olustur(KuyumKalemli(k, 80_000m), k.Islem);

        using (var db = vt.Olustur(k))
        {
            var s = new OzelMatrahKapanisServisi(db, TestKurulum.Denetim(db), NullLogger<OzelMatrahKapanisServisi>.Instance);
            var hata = await Assert.ThrowsAsync<ConflictException>(() =>
                s.Gir(new KapanisGirDto { Kaynak = "BIST", Gun = gun, Deger = 6_720m, Birim = "TL_GRAM" }, k.Islem));
            Assert.Contains("1 faturada kullanıldı", hata.Message);
        }
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(6_710m, kontrol.OzelMatrahKapanislari.Single(x => x.TenantId == k.TenantId).Fiyat);
    }

    [Fact]
    public async Task BIST_hafta_sonu_kapanisi_400()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KK hafta sonu");
        var cmt = Bugun;
        while (cmt.DayOfWeek != DayOfWeek.Saturday) cmt = cmt.AddDays(-1);
        using var db = vt.Olustur(k);
        var s = new OzelMatrahKapanisServisi(db, TestKurulum.Denetim(db), NullLogger<OzelMatrahKapanisServisi>.Instance);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            s.Gir(new KapanisGirDto { Kaynak = "BIST", Gun = cmt, Deger = 6_700m, Birim = "TL_GRAM" }, k.Islem));
        Assert.Contains("hafta sonu", hata.Message);
    }

    [Fact]
    public async Task Otomatik_kapanis_gunu_gunceller_faturada_kullanilmis_gunu_DEGISTIRMEZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "KK otomatik");
        using var db = vt.Olustur(k);
        var s = new OzelMatrahKapanisServisi(db, TestKurulum.Denetim(db), NullLogger<OzelMatrahKapanisServisi>.Instance);
        var gun = Bugun.AddDays(-1);
        await s.OtomatikKaydet(k.TenantId, "HAREM", new KaynakKapanisi(gun, 6_600m, "Harem 1"));
        await s.OtomatikKaydet(k.TenantId, "HAREM", new KaynakKapanisi(gun, 6_610m, "Harem 2"));
        await s.OtomatikKaydet(k.TenantId, "BIST", new KaynakKapanisi(gun, 1m, "yanlış kaynak"));   // BIST otomatik YAZILMAZ

        using var kontrol = vt.OlusturSistem();
        var satirlar = kontrol.OzelMatrahKapanislari.Where(x => x.TenantId == k.TenantId).ToList();
        Assert.Single(satirlar);
        Assert.Equal(6_610m, satirlar[0].Fiyat);
        Assert.Null(satirlar[0].KaydedenKullaniciId);
    }
}

[Collection("http")]
public sealed class KurKaynaklariHttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Kaynaklar_ucu_uc_secimi_ve_durumlari_dondurur_anahtar_degeri_yok()
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "KK HTTP kaynaklar");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/kur/kaynaklar");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var g = await cevap.Json();

        var doviz = g.GetProperty("dovizSecenekleri").EnumerateArray().ToList();
        Assert.Contains(doviz, s => s.GetProperty("kod").GetString() == "EVDS");
        Assert.Contains(doviz, s => s.GetProperty("kod").GetString() == "ALTIN_KAYNAGI");
        Assert.True(g.TryGetProperty("dovizSecili", out _));
        Assert.Contains(g.GetProperty("evdsAnahtarVar").ValueKind, new[] { JsonValueKind.True, JsonValueKind.False });
        var matrah = g.GetProperty("ozelMatrahSecenekleri").EnumerateArray().Select(s => s.GetProperty("kod").GetString()).ToList();
        Assert.Equal(["BIST", "HAREM", "ALTINKAYNAK", "ISLEM_KURU", "ELLE"], matrah);
        Assert.Equal("ISLEM_KURU", g.GetProperty("ozelMatrahSecili").GetString());   // eski kiracı (boş sütun)
        Assert.Equal("BIST", g.GetProperty("ozelMatrahVarsayilan").GetString());
        Assert.DoesNotContain("evdsAnahtar\"", (await cevap.Metin()).Replace("evdsAnahtarVar\"", ""));
    }

    [Fact]
    public async Task Doviz_ve_has_fiyati_kaynagi_SAHIP_MUDUR_yazar_TEZGAHTAR_403_gecersiz_400()
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "KK HTTP ayar");

        var tez = f.Ortam.Istemci(k, "TEZGAHTAR");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await tez.PatchJson("/api/v1/kur/doviz-kaynak", new { kaynak = "ALTIN_KAYNAGI" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await tez.PatchJson("/api/v1/kur/ozel-matrah-kaynak", new { kaynak = "BIST" })).StatusCode);

        var mudur = f.Ortam.Istemci(k, "MUDUR");
        Assert.Equal(HttpStatusCode.OK,
            (await mudur.PatchJson("/api/v1/kur/ozel-matrah-kaynak", new { kaynak = "BIST" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await mudur.PatchJson("/api/v1/kur/ozel-matrah-kaynak", new { kaynak = "YOK" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await mudur.PatchJson("/api/v1/kur/doviz-kaynak", new { kaynak = "ALTIN_KAYNAGI" })).StatusCode);

        // Test ortamında EVDS anahtarı tanımlı değil → seçilemez, ayar DEĞİŞMEZ.
        var evds = await f.Ortam.Istemci(k, "SAHIP").PatchJson("/api/v1/kur/doviz-kaynak", new { kaynak = "EVDS" });
        Assert.Equal(HttpStatusCode.BadRequest, evds.StatusCode);
        Assert.Contains("EVDS", await evds.Metin());

        using var db2 = f.Vt.OlusturSistem();
        var t = db2.Tenants.Single(x => x.Id == k.TenantId);
        Assert.Equal("BIST", t.OzelMatrahKaynagi);
        Assert.Equal("ALTIN_KAYNAGI", t.DovizKaynagi);
    }

    [Fact]
    public async Task Kapanis_girilir_listelenir_TEZGAHTAR_giremez()
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "KK HTTP kapanış");
        var gun = Zaman.YerelBugun(DateTime.UtcNow).AddDays(-1);
        while (gun.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) gun = gun.AddDays(-1);
        var govde = new { kaynak = "BIST", gun = gun.ToString("yyyy-MM-dd"), deger = 6_700_000m, birim = "TL_KG" };

        Assert.Equal(HttpStatusCode.Forbidden,
            (await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson("/api/v1/kur/kapanis", govde)).StatusCode);
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/kur/kapanis", govde);
        Assert.Equal(HttpStatusCode.Created, c.StatusCode);
        Assert.Equal(6_700m, (await c.Json()).GetProperty("fiyat").GetDecimal());

        var liste = await (await f.Ortam.Istemci(k, "MUHASEBE").GetAsync("/api/v1/kur/kapanislar")).Json();
        var ilk = liste.EnumerateArray().Single();
        Assert.Equal("BIST", ilk.GetProperty("kaynak").GetString());
        Assert.Equal(gun.ToString("yyyy-MM-dd"), ilk.GetProperty("gun").GetString());

        var gecersiz = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/kur/kapanis",
            new { kaynak = "HAREM", gun = gun.ToString("yyyy-MM-dd"), deger = 6_700m, birim = "TL_GRAM" });
        Assert.Equal(HttpStatusCode.BadRequest, gecersiz.StatusCode);
    }
}
