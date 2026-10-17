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

    // TEMA TEMİZLİĞİ 17.10: SATIŞ = Yıldız tezgâhı. Seçili müşteri üst satırda (#yildiz-tezgah-ust-musteri); seçim
    // "Değiştir" katmanında. Peşin yükleme hatası tezgâhın not satırında (Uyarilar) ve katmanda (#islem-pesin-hata).
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Pos()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1",
                                null, ArayuzModuServisi.Profesyonel));
        return Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
    }

    [Fact]
    public void B3E_01_satis_Pesin_Musteri_ON_SECILI_ve_POST_govdesinde_o_cari_gider()
    {
        var e = Pos();
        TemaTemizligiGDYardimcisi.MusteriBekle(e, "Peşin Müşteri");
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/cari/sistem-carisi"));

        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        TemaTemizligiGDYardimcisi.KalemSayisiBekle(e, 1);
        TemaTemizligiGDYardimcisi.KaydetAcikBekle(e);
        e.Find(TemaTemizligiGDYardimcisi.Kaydet).Click();

        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")));
        var g = JsonDocument.Parse(istekler.Last(i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")).Govde).RootElement;
        Assert.Equal("pm1", g.GetProperty("cariId").GetString());
    }

    [Fact]
    public void B3E_02_not_metninde_ESIK_SAYISI_YOK()
    {
        // Yıldız tezgâhı Peşin Müşteri'yi üst satırda yazar (ayrı not satırı tuvalde yok): eşik SAYISI orada da yok.
        var e = Pos();
        TemaTemizligiGDYardimcisi.MusteriBekle(e, "Peşin Müşteri");
        var ust = e.Find("#yildiz-tezgah-ust").TextContent;
        Assert.Contains("Peşin Müşteri", ust);
        Assert.DoesNotMatch(@"\d", ust);
        // Öteki türlerin formunda özgün not (#islem-pesin-notu) — Peşin yalnız satışta seçili gelir; not metni burada sınanamaz,
        // Yıldız katmanındaki Peşin satırı da sayı taşımaz:
        e.Find("#yildiz-tezgah-degistir").Click();
        var sat = e.WaitForElement("#yildiz-musteri-sat-pm1").TextContent;
        Assert.Contains("Peşin Müşteri", sat);
        Assert.DoesNotMatch(@"\d", sat);
    }

    [Fact]
    public void B3E_03_alis_turunde_on_secim_TASINMAZ_satisa_donunce_geri_gelir()
    {
        var e = Pos();
        TemaTemizligiGDYardimcisi.MusteriBekle(e, "Peşin Müşteri");

        TemaTemizligiGDYardimcisi.TurSec(e, "ALIS");
        // Alış: Yıldız tür yüzünde özgün müşteri satırı (FisMusteriSatiri) — Peşin ön seçimi TAŞINMAZ.
        e.WaitForElement("#islem-cari");
        e.WaitForAssertion(() => Assert.NotEqual("pm1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        Assert.Empty(e.FindAll("#islem-pesin-notu"));

        TemaTemizligiGDYardimcisi.TurSec(e, "SATIS");
        e.TezgahBekle();
        TemaTemizligiGDYardimcisi.MusteriBekle(e, "Peşin Müşteri");
    }

    [Fact]
    public void B3E_04_kart_okunamazsa_POS_acik_kalir_hata_YUTULMAZ()
    {
        pesinHatali = true;
        var e = Pos();
        // Katman kapalıyken Peşin yükleme hatası tezgâhın not satırında (Uyarilar) — yutulmaz.
        e.WaitForAssertion(() => Assert.Contains(e.FindAll("[id^='yildiz-tezgah-uyari-']"), u => u.TextContent.Contains("Peşin Müşteri")));
        Assert.Null(TemaTemizligiGDYardimcisi.TezgahMusterisi(e));   // Peşin seçili DEĞİL
        Assert.NotNull(e.Find(TemaTemizligiGDYardimcisi.Kaydet));     // tezgâh açık kalır
        // "Değiştir" katmanında da kendi bandı (#islem-pesin-hata)
        e.Find("#yildiz-tezgah-degistir").Click();
        Assert.Contains("Peşin Müşteri", e.WaitForElement("#islem-pesin-hata").TextContent);
    }

    // ── Cari ekranı ──────────────────────────────────────────────────────────

    // TEMA TEMİZLİĞİ 17.10: Müşteriler listesi = YildizMusteriler (tuval Musteri4). Tuval listesinde sistem kartı (Peşin
    // Müşteri) HİÇ yer almaz (Cariler.Yildiz.cs YildizSatirlar: !SistemCarisi) — listede ona "Pasife al" sunulması imkânsız;
    // eski satır rozeti (#c-sistem-carisi-*) ve satır eylemleri listeyle birlikte kalktı.
    private IRenderedComponent<Kuyumcu.Web.Pages.Cariler> CariEkrani()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("#yildiz-musteri-c1");
        return e;
    }

    [Fact]
    public void B3E_05_listede_sistem_kartinda_PASIFE_AL_YOK_kilit_rozeti_VAR()
    {
        var e = CariEkrani();
        // Sistem kartı listede yok → "Pasife al" de yok; sıradan cari listede (liste çizildi — boş DOM değil).
        Assert.Empty(e.FindAll("#yildiz-musteri-pm1"));
        Assert.Empty(e.FindAll("#c-pasife-al-pm1"));
        // sıradan cari: kartında mevcut davranış (Pasife al VAR)
        e.Find("#yildiz-musteri-c1").Click();
        e.WaitForElement("#c-pasife-al");
        Assert.Empty(e.FindAll("#degismez-ipucu-SistemCarisi"));
    }

    // TEMA TEMİZLİĞİ 17.10: B3E_06_duzenle_cekmecesinde_DegismezIpucu_var_pasife_al_dugmesi_yok kaldırıldı — Yıldız Müşteriler
    // listesinde sistem kartı (Peşin Müşteri) BİLEREK yok (Cariler.Yildiz.cs: tuval Musteri4), bağlantı (?cariId=) kartı değil
    // detay penceresini açar; sistem kartının düzenleme çekmecesine kullanıcı yolu kalmadı. "Sistem kartında Pasife al yok"
    // güvencesi B3E_05'te (listede yok → eylemi de yok); sunucu kapısı (DELETE/PATCH aktif:false → 400) PesinMusteriHttpTestleri.B3H_03/B3H_04'te.
}
