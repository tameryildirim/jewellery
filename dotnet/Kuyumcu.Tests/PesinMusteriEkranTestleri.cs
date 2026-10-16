using System.Text.Json;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-3 — PEŞİN MÜŞTERİ: EKRAN (bUnit)
//
// Sınanan:
//   • satış ekranı açılınca "Peşin Müşteri" ÖN SEÇİLİ (kimliği sunucudan gelir)
//     ve POST gövdesine o cari gider;
//   • satış dışı türde (alış) ön seçim TAŞINMAZ;
//   • kart okunamazsa POS kapanmaz, hata YUTULMAZ (UX-S28 — kendi bandı);
//   • not metninde EŞİK SAYISI YOK (PLAN § 3 md.6);
//   • cari listesinde sistem kartının satırında PASİFE AL DÜĞMESİ YOK, kilit
//     rozeti var; düzenleme çekmecesinde DegismezIpucu var (UX-S25).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class PesinMusteriEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool pesinHatali;

    private const string CariListesi = """
        [{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":null,"email":null,
          "tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false,"sistemCarisi":false,"aktif":true},
         {"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","telefon":null,"email":null,
          "tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
          "riskSeviye":null,"pepBeyani":false,"sistemCarisi":true,"aktif":true}]
        """;

    public PesinMusteriEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => pesinHatali))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<bool> pesinHatali) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            if (yol.EndsWith("/cari/sistem-carisi") && pesinHatali())
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"message":"sunucu hatası"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") =>
                    """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.EndsWith("/cari") => CariListesi,
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/islemler/satis") => """{"id":"h1","fisId":"f1"}""",
                var v when v.Contains("/reyonlar") || v.Contains("/islemler/son") || v.Contains("/pos/cihazlar") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Pos()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1",
                                null, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-tur-SATIS");
        return e;
    }

    [Fact]
    public void B3E_01_satis_Pesin_Musteri_ON_SECILI_ve_POST_govdesinde_o_cari_gider()
    {
        var e = Pos();
        e.WaitForElement("#islem-pesin-notu");
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/cari/sistem-carisi"));

        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        e.Find("#islem-kaydet").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")));
        var g = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")).Govde).RootElement;
        Assert.Equal("pm1", g.GetProperty("cariId").GetString());
    }

    [Fact]
    public void B3E_02_not_metninde_ESIK_SAYISI_YOK()
    {
        var e = Pos();
        var not = e.WaitForElement("#islem-pesin-notu").TextContent;
        Assert.Contains("Peşin Müşteri", not);
        Assert.DoesNotMatch(@"\d", not);
    }

    [Fact]
    public void B3E_03_alis_turunde_on_secim_TASINMAZ_satisa_donunce_geri_gelir()
    {
        var e = Pos();
        e.WaitForElement("#islem-pesin-notu");

        e.Find("#islem-tur-ALIS").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-pesin-notu")));

        e.Find("#islem-tur-SATIS").Click();
        e.WaitForElement("#islem-pesin-notu");
    }

    [Fact]
    public void B3E_04_kart_okunamazsa_POS_acik_kalir_hata_YUTULMAZ()
    {
        pesinHatali = true;
        var e = Pos();
        var bant = e.WaitForElement("#islem-pesin-hata");
        Assert.Contains("Peşin Müşteri", bant.TextContent);
        Assert.Empty(e.FindAll("#islem-pesin-notu"));
        Assert.NotNull(e.Find("#islem-kaydet"));
    }

    // ── Cari ekranı ──────────────────────────────────────────────────────────

    private IRenderedComponent<Kuyumcu.Web.Pages.Cariler> CariEkrani()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("#c-sistem-carisi-pm1");
        return e;
    }

    [Fact]
    public void B3E_05_listede_sistem_kartinda_PASIFE_AL_YOK_kilit_rozeti_VAR()
    {
        var e = CariEkrani();
        Assert.Empty(e.FindAll("#c-pasife-al-pm1"));
        Assert.NotNull(e.Find("#c-pasife-al-c1"));   // sıradan cari: mevcut davranış
        var rozet = e.Find("#c-sistem-carisi-pm1");
        Assert.False(string.IsNullOrWhiteSpace(rozet.GetAttribute("title")));
    }

    [Fact]
    public void B3E_06_duzenle_cekmecesinde_DegismezIpucu_var_pasife_al_dugmesi_yok()
    {
        var e = CariEkrani();
        // Satır sırası: sunucu ada göre sıralar; burada sahte listede pm1 ikinci satır.
        var duzenle = e.FindAll("button").Where(b => b.TextContent.Contains("Düzenle")).ToList();
        duzenle[1].Click();
        e.WaitForElement("#degismez-ipucu-SistemCarisi");
        Assert.Empty(e.FindAll("#c-pasife-al"));
        // Ipucu bileşeni düğme ÜRETMEZ; DELETE isteği atılmadı.
        Assert.DoesNotContain(istekler, i => i.Metot == "DELETE");
    }
}
