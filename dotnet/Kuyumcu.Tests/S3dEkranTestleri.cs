using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S-3d — QA TURU 4 BORCU (ekran kapısı).
//
// Kapatılan bulgular:
//   • KRİTİK 1 (/islemler) — TEK reyon otomatik atanıyor, temizlenemiyor ve
//     reyonda mal yokken HİÇBİR satış kaydedilemiyordu (çıkmaz).
//     Artık seçici HER ZAMAN çizilir ve "—" seçeneği vardır; gövdeye
//     OTOMATİK reyon konmaz.
//   • KRİTİK 2 (/islemler → hurda alış) — form satıcı kimliğini hiç sormuyordu,
//     sunucu VUK md.234 gereği 400 dönüyordu. Artık alanlar formda; eksikse
//     Kaydet KİLİTLİ ve sebebi ekranda (UX-S30), dolu ise gövdede `satici`.
//   • YÜKSEK 2 (/islemler) — MASAK kilit metni yalnız "Kimlik Tespitleri"ydi;
//     artık tam cümle.
//
// ⚠ bUnit CSS uygulamaz: dar ekran / karanlık tema kanıtı burada DEĞİL.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S3dEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private int reyonSayisi = 1;

    public S3dEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => reyonSayisi))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    // Sahte API — '{}' varsayılanı yeni uçlar için BIRAKILMADI.
    private sealed class SahteApi(
        List<(string, string, string)> kayit, Func<int> reyonSayisi) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            var y = yol switch
            {
                var v when v.Contains("/tevsik/durum") => """
                    {"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,
                     "bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}
                    """,
                var v when v.Contains("/fiyat/teklif") =>
                    """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"ayarFiyatlari":[]}""",
                var v when v.Contains("/reyonlar") => reyonSayisi() == 1
                    ? """[{"id":"r1","ad":"Vitrin","subeId":"s1","aktif":true}]"""
                    : """[{"id":"r1","ad":"Vitrin","subeId":"s1","aktif":true},{"id":"r2","ad":"Kasa Arkası","subeId":"s1","aktif":true}]""",
                var v when v.Contains("/urunler") => """
                    [{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,
                      "iscilikTl":0,"satisFiyati":10000}]
                    """,
                var v when v.Contains("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/esikler") => """
                    {"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,
                     "ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,
                     "belgeTurleri":["TC_KIMLIK","PASAPORT"]}
                    """,
                var v when v.Contains("/islemler/hurda-alis") => """{"id":"h1","fisId":"F-1"}""",
                var v when v.Contains("/islemler") && istek.Method == HttpMethod.Post =>
                    """{"id":"i1","fisId":"F-1"}""",
                // P-10: KART bacagi POS cihazi listesini okur.
                var p10c when p10c.Contains("/pos/cihazlar") =>
                    """[{"id":"pc1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"s1","kasaId":"ks1","pesinKomisyonOran":1.5,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    /// <summary>Reyon seçenekleri OTURUMDAKİ şubeden süzülür — claim şart.</summary>
    private void OturumKur()
        => Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));

    // TEMA TEMİZLİĞİ 17.10: İşlemler SATIŞ'ta hep Yıldız tezgâhıyla açılır. Reyon seçicisi (#islem-reyon, aynı
    // ReyonParcasi) kalem satırına dokununca açılan düzenlemede; müşteri "Değiştir" katmanında; Kaydet
    // #yildiz-tezgah-kaydet. Hurda alış Yıldız'ın görünür "Alış" sekmesinden (#yildiz-tur-HURDA_ALIS) açılır —
    // o türün formu (#islem-cari, #islem-kaydet, satıcı kimliği alanları) AYNEN.
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ac()
    {
        OturumKur();
        // Sahte sunucu sistem carisini vermez → Peşin ön seçimi beklenmez.
        return Render<Kuyumcu.Web.Pages.Islemler>().YildizHazir(null);
    }

    private static AngleSharp.Dom.IElement Kaydet(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
        => e.Find("#islem-kaydet");

    /// <summary>Satış tezgâhında kalem ekler ve kalem satırının düzenlemesini (reyon · açıklama) açar.</summary>
    private static void KalemVeDuzen(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        e.YildizKalemEkle();
        e.Find("#yildiz-kalem-0").Click();
        e.WaitForElement("#yildiz-kalem-duzen-0");
    }

    private string SatisGovdesi()
    {
        string? g = null;
        Assert.True(SpinWait.SpinUntil(() =>
        {
            lock (istekler) g = istekler.FirstOrDefault(i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")).Item3;
            return g is not null;
        }, TimeSpan.FromSeconds(10)), "satış POST'u gelmedi");
        return g!;
    }

    // ── DP4-01 · reyon çıkmazı ───────────────────────────────────────────────

    /// <summary>
    /// QA turu 4 KRİTİK 1'in birebir yeniden üretimi: TEK reyon varken
    /// `#islem-reyon` seçim kutusu YOKTU (salt metin `<p id="islem-reyon-tek">`).
    /// </summary>
    [Fact]
    public void Tek_reyon_varken_de_secici_cizilir_ve_bos_secenek_vardir()
    {
        reyonSayisi = 1;
        var e = Ac();
        KalemVeDuzen(e);
        e.WaitForElement("#islem-reyon");

        var secici = e.Find("#islem-reyon");
        // P-36c göçü: ham <select> → ortak <Secici>; eleman KİMLİĞİ aynı, TÜRÜ
        // combobox düğmesi. Sözleşme aynı soruyu sorar: seçici ÇİZİLİYOR mu.
        Assert.Equal("combobox", secici.GetAttribute("role"));
        // "—" seçeneği = reyonsuz devam etme yolu (çıkmaz kapandı).
        Assert.Contains(SeciciYardim.Secenekler(e, "islem-reyon"), o => o.Deger() == "");
        // Tek reyon bilgisi KAYBOLMADI, rozete indi.
        Assert.Equal("Vitrin", e.Find("#islem-reyon-tek").TextContent.Trim());
    }

    /// <summary>
    /// Çıkmazın kökü: seçici olmadığı için gövdeye OTOMATİK reyon konuyordu.
    /// Kullanıcı seçmediyse gövdede reyonId null olmalıdır.
    /// </summary>
    [Fact]
    public void Reyon_secilmemisse_govdeye_OTOMATIK_reyon_konmaz()
    {
        reyonSayisi = 1;
        var e = Ac();
        e.YildizMusteriSec("c1");
        KalemVeDuzen(e);
        e.WaitForElement("#islem-reyon");
        istekler.Clear();

        e.YildizKaydet();

        var satis = SatisGovdesi();
        Assert.Contains("\"reyonId\":null", satis);
        Assert.DoesNotContain("\"reyonId\":\"r1\"", satis);
    }

    [Fact]
    public void Reyon_secilirse_govdeye_SECILEN_reyon_konur()
    {
        reyonSayisi = 2;
        var e = Ac();
        e.YildizMusteriSec("c1");
        KalemVeDuzen(e);
        e.WaitForElement("#islem-reyon");
        SeciciYardim.Sec(e, "islem-reyon", "r2");
        istekler.Clear();

        e.YildizKaydet();

        Assert.Contains("\"reyonId\":\"r2\"", SatisGovdesi());
    }

    // ── DP4-03 · hurda alışta satıcı kimliği ─────────────────────────────────

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> HurdaAc()
    {
        var e = Ac();
        e.Find("#yildiz-tur-HURDA_ALIS").Click();
        e.WaitForElement("#hurda-gram");
        // ⚠ P-10 notu (Anayasa md.1 — gizlenmedi): başlangıç yüklemesine bir
        // istek daha eklenince (GET /pos/cihazlar) bu satır tam süitte ARA SIRA
        // boşa düşüyordu. Sebep ürün kusuru DEĞİL, bUnit'te BAYAT ELEMENT
        // referansıdır: Find ile alınan <select>, arada gelen bir yeniden çizimle
        // DOM'dan düşerse Change olayı kopmuş düğüme gider ve cariId boş kalır
        // (kilit sebebi "Cari seçin"e kayar). Seçim GERÇEKTEN oturana dek
        // tazelenerek tekrarlanır.
        e.WaitForAssertion(() =>
        {
            if (e.Find("#islem-cari").Deger() != "c1")
                SeciciYardim.Sec(e, "islem-cari", "c1");
            Assert.Equal("c1", e.Find("#islem-cari").Deger());
        });
        e.Find("#hurda-gram").Input("3");
        SeciciYardim.Sec(e, "hurda-ayar", "K22");
        e.Find("#hurda-fiyat").Input("6800");
        return e;
    }

    [Fact]
    public void Hurda_formunda_satici_kimligi_alanlari_vardir()
    {
        var e = HurdaAc();
        Assert.NotNull(e.Find("#hurda-satici-tip"));

        SeciciYardim.Sec(e, "hurda-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#hurda-satici-tckn");
        Assert.NotNull(e.Find("#hurda-satici-ad"));
        Assert.NotNull(e.Find("#hurda-satici-adres"));
    }

    /// <summary>
    /// QA turu 4 KRİTİK 2: form eksik olduğu için POST 400 dönüyordu ve
    /// kullanıcı SEBEBİNİ göremiyordu. Artık istek HİÇ atılmaz + sebep yazılı.
    /// </summary>
    [Fact]
    public void Satici_tipi_secilmeden_Kaydet_KILITLI_ve_sebebi_yazili()
    {
        var e = HurdaAc();
        istekler.Clear();

        Assert.True(Kaydet(e).HasAttribute("disabled"));
        Assert.Contains("Satıcı kimliği eksik", e.Find("#islem-kaydet-kilit").TextContent);
        // Alan altı Türkçe mesaj (DoD EK md.3) — sessiz etkisizlik yok.
        Assert.Contains("Satıcı tipi zorunludur", e.Find("#hurda-satici-tip-hata").TextContent);

        Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("hurda-alis"));
    }

    [Fact]
    public void TCKN_11_hane_degilse_alan_alti_mesaj_cikar_ve_Kaydet_kilitli_kalir()
    {
        var e = HurdaAc();
        SeciciYardim.Sec(e, "hurda-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#hurda-satici-tckn");
        e.Find("#hurda-satici-ad").Input("Ali Veli");
        e.Find("#hurda-satici-tckn").Input("123");

        Assert.Contains("11 haneli", e.Find("#hurda-satici-tckn-hata").TextContent);
        Assert.True(Kaydet(e).HasAttribute("disabled"));
    }

    [Fact]
    public void Satici_bilgisi_dolunca_govdede_satici_blogu_gonderilir()
    {
        var e = HurdaAc();
        SeciciYardim.Sec(e, "hurda-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#hurda-satici-tckn");
        e.Find("#hurda-satici-ad").Input("Ali Veli");
        e.Find("#hurda-satici-tckn").Input("10000000146");
        e.Find("#hurda-satici-adres").Input("Ulus Mah. 1");
        istekler.Clear();

        Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);

        var post = istekler.First(i => i.Metot == "POST" && i.Yol.Contains("hurda-alis"));
        // ⚠ Türkçe JSON gövdesi AYRIŞTIRILARAK sınanır; alt nesnenin İÇİNE
        //   inilir (düz metin araması, bloğu başka bir ada taşıyan bir
        //   gerilemeyi yakalayamaz — M3-1 mutasyonu bunu gösterdi).
        var satici = System.Text.Json.JsonDocument.Parse(post.Govde)
            .RootElement.GetProperty("satici");
        Assert.Equal(System.Text.Json.JsonValueKind.Object, satici.ValueKind);
        Assert.Equal("NIHAI_TUKETICI", satici.GetProperty("saticiTipi").GetString());
        Assert.Equal("10000000146", satici.GetProperty("tckn").GetString());
        Assert.Equal("Ali Veli", satici.GetProperty("adSoyad").GetString());
        Assert.Equal("Ulus Mah. 1", satici.GetProperty("adres").GetString());
    }

    [Fact]
    public void MUKELLEF_saticida_tedarikci_fatura_no_istenir_TCKN_istenmez()
    {
        var e = HurdaAc();
        SeciciYardim.Sec(e, "hurda-satici-tip", "MUKELLEF");
        e.WaitForElement("#hurda-satici-fatura");

        Assert.Empty(e.FindAll("#hurda-satici-tckn"));
        Assert.True(Kaydet(e).HasAttribute("disabled"));

        e.Find("#hurda-satici-fatura").Input("FTR-2026-77");
        istekler.Clear();
        Kaydet(e).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);

        var post = istekler.First(i => i.Metot == "POST" && i.Yol.Contains("hurda-alis"));
        var satici = System.Text.Json.JsonDocument.Parse(post.Govde)
            .RootElement.GetProperty("satici");
        Assert.Equal("MUKELLEF", satici.GetProperty("saticiTipi").GetString());
        Assert.Equal("FTR-2026-77", satici.GetProperty("tedarikciFaturaNo").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, satici.GetProperty("tckn").ValueKind);
    }

    /// <summary>UX-S24: satıcı tipi seçilince ad alanı seçili cariden ön dolar.</summary>
    [Fact]
    public void Satici_adi_secili_cariden_on_dolar_ve_kaynagi_yazar()
    {
        var e = HurdaAc();
        SeciciYardim.Sec(e, "hurda-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#hurda-satici-ad");

        Assert.Equal("Ayşe Demir", e.Find("#hurda-satici-ad").GetAttribute("value"));
        Assert.NotNull(e.Find("#hurda-satici-ad-kaynak"));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// DP4-02 — /uyum "Kapat" GÖVDE SÖZLEŞMESİ (istemci tarafı).
// Sunucu sözleşmesi S3dHttpTestleri'nde; burada istemcinin DOĞRU ADI
// gönderdiği çivilenir. İkisi birlikte "ad/tip sözleşmesi" kuralını kapatır.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class S3dUyumSozlesmeTestleri : BunitContext
{
    private readonly Yakalayici yakala = new();

    public S3dUyumSozlesmeTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(yakala) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
    }

    private sealed class Yakalayici : HttpMessageHandler
    {
        public string Govde = "";
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            Govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"id":"s1","durum":"KAPATILDI","gerekce":"x","tespitTarihi":"2026-09-18T00:00:00Z","sonBildirimTarihi":"2026-09-28T00:00:00Z","bildirenKullaniciId":"k1","olusturuldu":"2026-09-18T00:00:00Z"}""",
                    System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    [Fact]
    public async Task Kapat_govdesinde_alan_adi_gerekce_olur_kapatmaGerekcesi_OLMAZ()
    {
        var servis = Services.GetRequiredService<UyumServisi>();

        await servis.Kapat("s1", "Müşteri belgeleri incelendi, şüphe kalktı");

        Assert.Contains("\"gerekce\"", yakala.Govde);
        Assert.DoesNotContain("kapatmaGerekcesi", yakala.Govde);
    }
}
