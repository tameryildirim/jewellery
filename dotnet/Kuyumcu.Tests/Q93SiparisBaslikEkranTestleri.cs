using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q9-3 — SİPARİŞ BAŞLIĞI (cari · tür · şube) DÜZENLEMESİNİN GÖRÜNÜR SONUCU.
//
// Asıl bulgu ("TASLAK'ta cari/tür/şube düzenlenemiyor") P-51 ile kapanmıştı ve
// canlı ölçümde YENİDEN ÜRETİLEMEDİ (rapor § 1). Aynı ölçüm, düzenlemenin
// çevresinde gerçek kusurlar gösterdi; bu sınıf onları ölçer:
//   • kartta şube yoktu (UX-S27) · sınır notları yanlıştı (UX-S29)
//   • geçiş izi ham kod + kimlik basıyordu (UX-S31 · B-25)
//   • müşteri seçeneğinde ham "MUSTERI" (UX-S31)
//   • terminal siparişte var olmayan "iptal edin" yolu (UX-S25)
// Kilit davranışı (ONAYLANDI ve sonrası) da burada uçtan uca ölçülür.
//
// ⚠ Sahte API DURUMLUDUR: PATCH gövdesindeki cari/tür/şube bir sonraki GET'e
//   yansır — "kaydedince kartta görünür" iddiası gerçekten ölçülür.
// ⚠ "{}" varsayılanı YOK: her uca gerçekçi camelCase yanıt.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Q93SiparisBaslikEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    // Durumlu sipariş (sahte sunucunun "veritabanı").
    private string durum = "TASLAK";
    private string tur = "MUSTERI_SIPARISI";
    private string cariId = "01M396C642K89MJRHS9TRN6Z21";
    private string? subeId = "01M396BQNTZYV97PAG5EX8Z8ZK";
    private bool terminal;
    private string[] izinli = ["ONAYLANDI", "IPTAL"];
    private string[] geriYonlu = [];
    private string? izGerekce;
    private int cariSayisi = 2;
    private int urunSayisi = 1;

    private const string CariA = "01M396C642K89MJRHS9TRN6Z21";
    private const string CariB = "01M396C660S85310P2GNFZEZX3";
    private const string SubeMerkez = "01M396BQNTZYV97PAG5EX8Z8ZK";
    private const string SubeCarsi = "01M396C62M21TAMDMX1SXN240D";

    public Q93SiparisBaslikEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private string CariAd(string id) => id == CariA ? "Ayşe Yılmaz" : id == CariB ? "Mehmet Demir" : "Başka";

    private string DetayJson()
    {
        var gecisler = izGerekce is null
            ? """[{"id":"g1","oncekiDurum":null,"yeniDurum":"TASLAK","gerekce":null,"kullaniciId":"u1","kullaniciAd":"Test Kullanıcı","olusturuldu":"2026-09-16T08:00:00Z"}]"""
            : "[{\"id\":\"g1\",\"oncekiDurum\":null,\"yeniDurum\":\"TASLAK\",\"gerekce\":null,\"kullaniciId\":\"u1\",\"kullaniciAd\":\"Test Kullanıcı\",\"olusturuldu\":\"2026-09-16T08:00:00Z\"},"
              + "{\"id\":\"g2\",\"oncekiDurum\":\"TASLAK\",\"yeniDurum\":\"TASLAK\",\"gerekce\":" + System.Text.Json.JsonSerializer.Serialize(izGerekce)
              + ",\"kullaniciId\":\"u1\",\"kullaniciAd\":\"Test Kullanıcı\",\"olusturuldu\":\"2026-09-16T09:00:00Z\"}]";
        static string j<T>(T v) => System.Text.Json.JsonSerializer.Serialize(v);
        return "{\"id\":\"s1\",\"no\":\"S-2026-000001\",\"tur\":" + j(tur) + ",\"durum\":" + j(durum)
            + ",\"cariId\":" + j(cariId) + ",\"cariAd\":" + j(CariAd(cariId)) + ",\"subeId\":" + j(subeId)
            + ",\"sozVerilenGun\":null,\"aciklama\":\"bilezik\",\"tahminiTutarTl\":null,\"kurSnapshotId\":null,"
            + "\"emanetHareketId\":null,\"geciken\":false,\"duzenlenebilir\":" + (durum == "TASLAK" ? "true" : "false")
            + ",\"terminal\":" + (terminal ? "true" : "false")
            + ",\"izinliGecisler\":" + j(izinli) + ",\"geriYonluGecisler\":" + j(geriYonlu)
            + ",\"geriAlmaYetkisi\":true,\"geriAlmaRolleri\":[\"SAHIP\",\"MUDUR\"]"
            + ",\"gerekceIsteyenGecisler\":[\"IPTAL\"],\"gerekceAsgari\":10,\"olusturanKullaniciId\":\"u1\","
            + "\"olusturuldu\":\"2026-09-16T08:00:00Z\",\"guncellendi\":\"2026-09-16T08:00:00Z\","
            + "\"kalemler\":[{\"id\":\"k1\",\"urunId\":null,\"urunAd\":null,\"tanim\":\"yüzük\",\"ayarId\":\"K22\",\"gramAgirlik\":3.5,\"adet\":1.0,\"sira\":0}],"
            + "\"gecisler\":" + gecisler + "}";
    }

    private string CarilerJson()
    {
        var liste = new List<string>
        {
            $$"""{"id":"{{CariA}}","ad":"Ayşe Yılmaz","tur":"MUSTERI"}""",
            $$"""{"id":"{{CariB}}","ad":"Mehmet Demir","tur":"MUSTERI"}""",
        };
        for (var i = 2; i < cariSayisi; i++)
            liste.Add($$"""{"id":"cx{{i}}","ad":"Müşteri {{i}}","tur":"TEDARIKCI"}""");
        return "[" + string.Join(",", liste) + "]";
    }

    private string UrunlerJson()
    {
        var liste = new List<string>();
        for (var i = 0; i < urunSayisi; i++)
            liste.Add($$"""{"id":"u{{i}}","kod":"K{{i}}","ad":"Ürün {{i}}","ayarId":"K22","gramAgirlik":1.0}""");
        return "[" + string.Join(",", liste) + "]";
    }

    private sealed class SahteApi(Q93SiparisBaslikEkranTestleri sahip) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            sahip.istekler.Add((istek.Method.Method, yol, govde));

            if (istek.Method == HttpMethod.Patch)
            {
                using var d = System.Text.Json.JsonDocument.Parse(govde);
                var k = d.RootElement;
                if (k.TryGetProperty("cariId", out var c)) sahip.cariId = c.GetString()!;
                if (k.TryGetProperty("tur", out var t)) sahip.tur = t.GetString()!;
                if (k.TryGetProperty("subeId", out var sb)) sahip.subeId = sb.GetString();
            }

            var g = yol switch
            {
                var y when y.EndsWith("/subeler") =>
                    $$"""[{"id":"{{SubeMerkez}}","ad":"Merkez"},{"id":"{{SubeCarsi}}","ad":"Çarşı"}]""",
                var y when y.EndsWith("/cari") => sahip.CarilerJson(),
                var y when y.EndsWith("/urunler") => sahip.UrunlerJson(),
                var y when y.Contains("/siparisler/") => sahip.DetayJson(),
                var y when y.Contains("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":4200}""",
                var y when y.Contains("/auth/ben") =>
                    """{"id":"u1","ad":"Test","soyad":"Kullanici","email":"t@t.local","rol":"SAHIP","subeId":"sb1"}""",
                _ => "[]",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.SiparisDetay> Detay()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", SubeMerkez));
        return Render<Kuyumcu.Web.Pages.SiparisDetay>(p => p.Add(x => x.Id, "s1"));
    }

    private static readonly SiparisBaslikMetinleri B = SiparisBaslikSozlugu.Tr;

    // ── Asıl bulgu: TASLAK'ta üç alan düzenlenir ve SONUÇ kartta görünür ─────

    [Fact]
    public void Q93_taslakta_cari_tur_sube_degisir_ve_KARTTA_gorunur()
    {
        var ekran = Detay();
        Assert.Contains("Merkez", ekran.Find("#sipd-sube").TextContent);

        ekran.Find("#sipd-duzenle").Click();
        SeciciYardim.Sec(ekran, "sipd-d-tur", "TAMIR");
        SeciciYardim.Sec(ekran, "sipd-d-cari", CariB);
        SeciciYardim.Sec(ekran, "sipd-d-sube", SubeCarsi);
        ekran.Find("#sipd-d-kaydet").Click();

        var patch = istekler.Last(i => i.Metot == "PATCH");
        using (var d = System.Text.Json.JsonDocument.Parse(patch.Govde))
        {
            Assert.Equal("TAMIR", d.RootElement.GetProperty("tur").GetString());
            Assert.Equal(CariB, d.RootElement.GetProperty("cariId").GetString());
            Assert.Equal(SubeCarsi, d.RootElement.GetProperty("subeId").GetString());
        }

        ekran.WaitForAssertion(() =>
        {
            var kart = ekran.Find("#sipd-kart").TextContent;
            Assert.Contains("Mehmet Demir", kart);
            Assert.Contains("Tamir", kart);
            Assert.Contains("Çarşı", ekran.Find("#sipd-sube").TextContent);
        });
    }

    [Fact]
    public void Q93_kartta_sube_ADIYLA_yazar_kimlik_basilmaz()
    {
        var ekran = Detay();
        var sube = ekran.Find("#sipd-sube").TextContent;
        Assert.Contains("Merkez", sube);
        Assert.DoesNotContain(SubeMerkez, ekran.Find("#sipd-kart").TextContent);
    }

    [Fact]
    public void Q93_subesiz_sipariste_kartta_tire_yazar()
    {
        subeId = null;
        var ekran = Detay();
        Assert.Contains(B.SubeYok, ekran.Find("#sipd-sube").TextContent);
    }

    // ── UX-S29: sınır notları doğru nesneyi ve sayıyı söyler ─────────────────

    [Fact]
    public void Q93_musteri_sinir_notu_SAYIYLA_yazar_yer_tutucu_ve_siparis_kelimesi_yok()
    {
        var ekran = Detay();
        ekran.Find("#sipd-duzenle").Click();
        var not = ekran.Find("#sipd-d-cari-sinir").TextContent;
        Assert.DoesNotContain("{0}", not);
        Assert.DoesNotContain("sipariş", not, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Kuyumcu.Web.Bilesenler.Secici.YerelTavan.ToString(), not);
        Assert.Contains("müşteri", not);
        // Sınıra değmeyen listede "daha fazlası var" YOK.
        Assert.Empty(ekran.FindAll("#sipd-d-cari-dahafazla"));
        // İstek sınırı ekrandaki sayıyla AYNI.
        Assert.Contains(istekler, i => i.Yol.EndsWith("/cari"));
    }

    [Fact]
    public void Q93_musteri_sayisi_sinira_degince_DAHA_FAZLA_uyarisi_cikar()
    {
        cariSayisi = Kuyumcu.Web.Bilesenler.Secici.YerelTavan;
        var ekran = Detay();
        ekran.Find("#sipd-duzenle").Click();
        Assert.Contains("Daha fazla müşteri var", ekran.Find("#sipd-d-cari-dahafazla").TextContent);
    }

    [Fact]
    public void Q93_urun_sinir_notu_URUN_der_sinira_degince_uyarir()
    {
        var ekran = Detay();
        ekran.Find("#sipd-duzenle").Click();
        var not = ekran.Find("#sipd-d-urun-sinir").TextContent;
        Assert.DoesNotContain("sipariş", not, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("100", not);
        Assert.Contains("ürün", not);
        Assert.Empty(ekran.FindAll("#sipd-d-urun-dahafazla"));
    }

    [Fact]
    public void Q93_urun_sayisi_sinira_degince_DAHA_FAZLA_uyarisi_cikar_P52_secici_bozulmaz()
    {
        urunSayisi = 100;
        var ekran = Detay();
        ekran.Find("#sipd-duzenle").Click();
        Assert.Contains("Daha fazla ürün var", ekran.Find("#sipd-d-urun-dahafazla").TextContent);
        // P-52: ürün seçici dolu gelir (boş liste yasağı).
        Assert.True(SeciciYardim.Secenekler(ekran, "sipd-d-kalem-urun-0").Count >= 100);
    }

    // ── UX-S31: ham kod ekrana basılmaz ─────────────────────────────────────

    [Fact]
    public void Q93_musteri_seceneginde_HAM_tur_kodu_basilmaz()
    {
        cariSayisi = 3;   // biri TEDARIKCI
        var ekran = Detay();
        ekran.Find("#sipd-duzenle").Click();
        SeciciYardim.Secenekler(ekran, "sipd-d-cari");
        var liste = ekran.Find("#sipd-d-cari-liste").TextContent;
        Assert.DoesNotContain("MUSTERI", liste);
        Assert.DoesNotContain("TEDARIKCI", liste);
        Assert.Contains(B.Musteri, liste);
        Assert.Contains(B.Tedarikci, liste);
    }

    [Fact]
    public void Q93_baslik_duzeltme_izi_ADLARLA_yazar_kimlik_ve_ham_kod_basilmaz()
    {
        izGerekce = $"BASLIK_DUZENLENDI: cari {CariA} → {CariB} · tür MUSTERI_SIPARISI → TAMIR · şube {SubeMerkez} → {SubeCarsi}";
        cariId = CariB;
        var ekran = Detay();
        ekran.Find("#sipd-gecmis-ac").Click(); // KESIF-P6 tur 2: durum geçmişi kapalı akordeon
        var iz = ekran.Find("#sipd-gecis-satir-g2").TextContent;
        Assert.Contains(B.BaslikDuzenlendi, iz);
        Assert.Contains("Ayşe Yılmaz → Mehmet Demir", iz);
        Assert.Contains("Müşteri siparişi → Tamir", iz);
        Assert.Contains("Merkez → Çarşı", iz);
        Assert.DoesNotContain("BASLIK_DUZENLENDI", iz);
        Assert.DoesNotContain("MUSTERI_SIPARISI", iz);
        Assert.DoesNotContain(CariA, iz);
        Assert.DoesNotContain(SubeCarsi, iz);
    }

    [Fact]
    public void Q93_listede_olmayan_eski_cari_kimlikle_DEGIL_notla_yazar()
    {
        izGerekce = $"BASLIK_DUZENLENDI: cari 01M000000000000000000YOK00 → {CariA}";
        var ekran = Detay();
        ekran.Find("#sipd-gecmis-ac").Click(); // KESIF-P6 tur 2: durum geçmişi kapalı akordeon
        var iz = ekran.Find("#sipd-gecis-satir-g2").TextContent;
        Assert.Contains(B.KayitListedeYok, iz);
        Assert.DoesNotContain("01M000000000000000000YOK00", iz);
    }

    [Fact]
    public void Q93_baska_gerekce_oldugu_gibi_yazar()
    {
        izGerekce = "Müşteri vazgeçti, ölçü değişecek";
        var ekran = Detay();
        ekran.Find("#sipd-gecmis-ac").Click(); // KESIF-P6 tur 2: durum geçmişi kapalı akordeon
        Assert.Contains("Müşteri vazgeçti, ölçü değişecek", ekran.Find("#sipd-gecis-satir-g2").TextContent);
    }

    // ── Kilit: ONAYLANDI ve sonrası — gerekçe + GERÇEK yol ─────────────────

    [Fact]
    public void Q93_onayli_sipariste_DUZENLE_yok_kilit_gerekceli_taslaga_don_yolu_yazili()
    {
        durum = "ONAYLANDI";
        izinli = ["ATOLYEDE", "HAZIR", "TASLAK", "IPTAL"];
        geriYonlu = ["TASLAK"];
        var ekran = Detay();
        Assert.Empty(ekran.FindAll("#sipd-duzenle"));
        Assert.Empty(ekran.FindAll("#sipd-d-cari"));
        var ipucu = ekran.Find("#degismez-ipucu-Siparis").TextContent;
        Assert.Contains("TASLAK durumunda değil", ipucu);
        Assert.NotEmpty(ekran.Find("#sipd-taslaga-don-notu").TextContent);
        // Anılan yol gerçekten VAR: "Taslak'a geri al" düğmesi çizilmiş.
        Assert.NotEmpty(ekran.FindAll("#sipd-geri-TASLAK"));
    }

    [Fact]
    public void Q93_atolyedeki_sipariste_geri_alma_zinciri_notu_yazar()
    {
        durum = "ATOLYEDE";
        izinli = ["HAZIR", "ONAYLANDI", "IPTAL"];
        geriYonlu = ["ONAYLANDI"];
        var ekran = Detay();
        Assert.Empty(ekran.FindAll("#sipd-duzenle"));
        Assert.Empty(ekran.FindAll("#sipd-taslaga-don-notu"));
        Assert.Equal(B.GeriAlmaNotu, ekran.Find("#sipd-geri-al-notu").TextContent.Trim());
        Assert.NotEmpty(ekran.FindAll("#sipd-geri-ONAYLANDI"));
    }

    [Fact]
    public void Q93_teslim_edilmis_sipariste_OLMAYAN_iptal_yolu_onerilmez()
    {
        durum = "TESLIM_EDILDI";
        terminal = true;
        izinli = [];
        var ekran = Detay();
        var ipucu = ekran.Find("#degismez-ipucu-Siparis").TextContent;
        Assert.Contains(B.TerminalGerekce, ipucu);
        Assert.Contains(B.TerminalAlternatif, ipucu);
        Assert.DoesNotContain("iptal edin", ipucu);
        Assert.Empty(ekran.FindAll("#sipd-gecis-IPTAL"));
        Assert.Empty(ekran.FindAll("#sipd-geri-al-notu"));
    }

    [Fact]
    public void Q93_sozluk_TR_ve_EN_bos_anahtar_yok()
    {
        foreach (var m in new[] { SiparisBaslikSozlugu.Tr, SiparisBaslikSozlugu.En })
            foreach (var p in typeof(SiparisBaslikMetinleri).GetProperties())
                Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(m)), p.Name);
        Assert.NotEqual(SiparisBaslikSozlugu.Tr.BaslikDuzenlendi, SiparisBaslikSozlugu.En.BaslikDuzenlendi);
    }
}
