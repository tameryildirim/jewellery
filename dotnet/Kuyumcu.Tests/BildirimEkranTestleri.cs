using Bunit;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// BİLDİRİM EKRANI + ÜST BAR ROZETİ (paket 6 Faz A)
//
// ⚠ KARARSIZLIK KORUMASI (Aşkın'ın koşulu): rozet 60 sn'de bir yoklar; test
//   ZAMANLAYICIYI BEKLEMEZ. Ölçüm ilk yükleme + AÇIK "elle yenile" çağrısıyla
//   yapılır (UstBar.RozetiYenile public'tir, tam bu yüzden).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class BildirimEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string bildirimYaniti = BosListe;
    private string sayiYaniti = """{"sayi":0,"enYuksekOnem":null}""";

    private const string BosListe = """
        {"durum":"ACIK","limit":100,"azamiLimit":100,"kapaliGosterimGun":30,
         "dahaFazlasiVar":false,"bildirimler":[]}
        """;

    private const string IkiBildirim = """
        {"durum":"ACIK","limit":100,"azamiLimit":100,"kapaliGosterimGun":30,
         "dahaFazlasiVar":true,
         "bildirimler":[
           {"id":"b1","tur":"VADE_HATIRLATMA","onem":"KRITIK","baslik":"Vadesi geçmiş veresiye borcu",
            "govde":"Bir müşterinin 12000.00 TL borcu 3 gündür gecikmiş.","hedefRota":"/raporlar/vade-takibi",
            "ilgiliVarlikTuru":"Cari","ilgiliVarlikId":"c1","olusturuldu":"2026-09-16T08:00:00Z",
            "kapatildi":null,"kapatanKullaniciId":null,"kapatmaNotu":null},
           {"id":"b2","tur":"SAKLAMA_ONIZLEME","onem":"UYARI","baslik":"Saklama süresi dolmuş kişisel veri var",
            "govde":"2 uyum kaydında saklama süresi doldu.","hedefRota":"/uyum",
            "ilgiliVarlikTuru":null,"ilgiliVarlikId":null,"olusturuldu":"2026-09-16T07:00:00Z",
            "kapatildi":"2026-09-16T09:00:00Z","kapatanKullaniciId":"u1","kapatmaNotu":"bakıldı"}]}
        """;

    // PAKET P-11 (B-24): sahte yanıt GERÇEKÇİDİR — sunucu artık yapilandirmaEtkin /
    // kiraciKapatti ayrımını ve "ayarlar" bloğunu döner. "{}" varsayılanı bırakmak
    // ekranın yeni dalını hiç sınamamak olurdu.
    private const string IsListesi = """
        {"azamiCalisma":50,
         "ayarlar":{"baslangicSaat":0,"vadeUfukGun":7,"masakUyariGun":3,"kapaliIsler":["KIRACI_KAPALI_IS"],
           "saatEnAz":0,"saatEnCok":23,"vadeEnAz":1,"vadeEnCok":365,"masakEnAz":0,"masakEnCok":60,
           "varsayilanVadeUfukGun":7,"varsayilanMasakUyariGun":3},
         "isler":[
           {"kod":"VADE_HATIRLATMA","ad":"Vade hatırlatma","etkin":true,
            "yapilandirmaEtkin":true,"kiraciKapatti":false,
            "sonCalisma":"2026-09-16T08:00:00Z","sonDurum":"BASARILI"},
           {"kod":"KIRACI_KAPALI_IS","ad":"İşletmenin kapattığı iş","etkin":false,
            "yapilandirmaEtkin":true,"kiraciKapatti":true,"sonCalisma":null,"sonDurum":null},
           {"kod":"KAPALI_IS","ad":"Kapalı iş","etkin":false,
            "yapilandirmaEtkin":false,"kiraciKapatti":false,"sonCalisma":null,"sonDurum":null}],
         "calismalar":[
           {"id":"c1","isKodu":"VADE_HATIRLATMA","calismaAnahtari":"2026-09-16","baslangic":"2026-09-16T08:00:00Z",
            "bitis":"2026-09-16T08:00:01Z","durum":"BASARILI","uretilenBildirim":1,"incelenenKayit":4,
            "sureMs":42,"hataOzeti":null,"elle":false}]}
        """;

    public BildirimEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        // PAKET P-15: UstBar artık GlobalArama + SubeSecici barındırıyor;
        // GlobalArama komut paletini menüyle AYNI yetki kaynağından süzer.
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(BildirimEkranTestleri sahip) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            sahip.istekler.Add((istek.Method.Method, yol, govde));

            var g = yol switch
            {
                var y when y.EndsWith("/bildirimler/sayi") => sahip.sayiYaniti,
                var y when y.Contains("/bildirimler/") && y.EndsWith("/kapat") =>
                    """{"id":"b1","tur":"VADE_HATIRLATMA","kapatildi":"2026-09-16T10:00:00Z","mesaj":"Bildirim kapatıldı"}""",
                var y when y.EndsWith("/bildirimler") => sahip.bildirimYaniti,
                // PAKET P-11 (B-24) — ayar PATCH'i gerçekçi yanıt döner.
                var y when y.EndsWith("/isler/ayarlar") =>
                    """{"baslangicSaat":9,"vadeUfukGun":14,"masakUyariGun":5,"kapaliIsler":[],"mesaj":"Zamanlanmış iş ayarları kaydedildi"}""",
                var y when y.EndsWith("/isler") => IsListesi,
                var y when y.Contains("/isler/") && y.EndsWith("/calistir") =>
                    """{"isKodu":"VADE_HATIRLATMA","durum":"BASARILI","atlandi":false,"uretilenBildirim":1,"atlananBildirim":0,"mesaj":"İş çalıştı: 1 yeni bildirim, 0 tekrar (atlandı)."}""",
                var y when y.Contains("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":4200}""",
                var y when y.Contains("/auth/ben") =>
                    """{"id":"u1","ad":"Test","soyad":"Kullanici","email":"t@t.local","rol":"SAHIP","subeId":null}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Bildirimler> Ciz(string rol = "SAHIP")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1"));
        return Render<Kuyumcu.Web.Pages.Bildirimler>().AyrintiAc();
    }

    // ── Yükleme ─────────────────────────────────────────────────────────────

    [Fact]
    public void Ekran_acilinca_ACIK_bildirimler_cekilir()
    {
        Ciz();
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/bildirimler"));
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/isler"));
    }

    [Fact]
    public void Bos_liste_BOS_DURUM_bileseni_cizer()
    {
        var e = Ciz();
        Assert.Single(e.FindAll(".bos-durum"));
        Assert.Empty(e.FindAll(".bildirim-kart"));
    }

    [Fact]
    public void Dolu_listede_kart_onem_rozeti_ve_hedef_baglantisi_cizilir()
    {
        bildirimYaniti = IkiBildirim;
        var e = Ciz();

        Assert.Equal(2, e.FindAll(".bildirim-kart").Count);
        Assert.NotNull(e.Find("#bildirim-b1"));
        Assert.Equal("/raporlar/vade-takibi", e.Find("#bildirim-git-b1").GetAttribute("href"));
        Assert.Contains("Kritik", e.Find("#bildirim-b1 .rozet").TextContent, StringComparison.Ordinal);
        // UX-S29: sunucu sınırı ekranda YAZAR.
        Assert.NotNull(e.Find("#bildirim-sinir-uyarisi"));
    }

    /// <summary>UX-S25: kapatılmış bildirimde düğme YOK, DegismezIpucu VAR.</summary>
    [Fact]
    public void Kapatilmis_bildirimde_KAPAT_dugmesi_yerine_DEGISMEZ_IPUCU_cizilir()
    {
        bildirimYaniti = IkiBildirim;
        var e = Ciz();

        Assert.Single(e.FindAll("#bildirim-kapat-b1"));     // açık olan
        Assert.Empty(e.FindAll("#bildirim-kapat-b2"));      // kapalı olan
        Assert.Single(e.FindAll("#bildirim-b2 #degismez-ipucu-Bildirim"));
    }

    // ── Kapatma ─────────────────────────────────────────────────────────────

    [Fact]
    public void Kapat_dugmesi_POST_kapat_ucuna_govdesiyle_gider()
    {
        bildirimYaniti = IkiBildirim;
        var e = Ciz();
        istekler.Clear();

        e.Find("#bildirim-kapat-b1").Click();
        e.Find("#bildirim-kapatma-notu").Input("tahsil edildi");
        e.Find("#bildirim-kapat-onay").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/bildirimler/b1/kapat"));
        var not = System.Text.Json.JsonDocument.Parse(istek.Govde).RootElement.GetProperty("not").GetString();
        Assert.Equal("tahsil edildi", not);

        // UX-S27: sunucunun döndürdüğü sonuç banda yazıldı.
        e.WaitForAssertion(() => Assert.Contains("Bildirim kapatıldı", e.Find(".bant-basari").TextContent, StringComparison.Ordinal));
    }

    /// <summary>DoD 1: listedeki HER düğme ya istek atar ya da disabled'dır.</summary>
    [Fact]
    public void Her_dugme_ya_istek_atar_ya_da_disabled_dir()
    {
        bildirimYaniti = IkiBildirim;
        var e = Ciz();

        var dugmeler = e.FindAll("button").Select(b => b.Id).Where(x => !string.IsNullOrEmpty(x)).ToList();
        Assert.NotEmpty(dugmeler);

        foreach (var id in dugmeler)
        {
            var dugme = e.Find($"#{id}");
            if (dugme.HasAttribute("disabled"))
            {
                // Kilitli düğme sebebini söyler (UX-S30).
                Assert.False(string.IsNullOrWhiteSpace(dugme.GetAttribute("title")),
                    $"{id}: devre dışı düğmede gerekçe yok");
                continue;
            }

            istekler.Clear();
            dugme.Click();
            // Kapat düğmesi modal açar (yıkıcı eylem onayı — DoD 4); onun isteği
            // onay düğmesinden gider. Diğer her düğme doğrudan istek atar.
            //
            // ⚠ KARARSIZLIK (2026-09-21, P-17 doğrulaması): eylem gövdeleri ASENKRON
            //   (IsiCalistir → await Api.PostAsync → await Yukle). Tıklamadan hemen
            //   sonra SENKRON ölçmek, tam süit paralel koşarken sürücü iş parçacığı
            //   gecikince "hiçbir şey olmadı" sanısı verir. Dahası IsiCalistir
            //   başında `if (calisanIs is not null) return;` bekçisi vardır: önceki
            //   yinelemenin isteği HÂLÂ uçuyorsa SONRAKİ düğme gerçekten no-op olur.
            //   Çözüm: her tıklamanın etkisini BEKLE, bileşen dinlenmeden sıradaki
            //   düğmeye geçme. Kural gevşetilmedi — yalnız ölçüm anı düzeltildi.
            var modalAcildi = false;
            // PAKET GOC-B (yeni sözleşme): ortak ızgaranın "Sütunlar" düğmesi istek atmaz, sütun panelini AÇAR
            // (kullanıcının görünür etkisi); panel açıldıysa düğme etkilidir — kural gevşetilmedi.
            var panelAcildi = false;
            e.WaitForAssertion(() =>
            {
                modalAcildi = e.FindAll("#bildirim-kapat-onay").Count > 0;
                panelAcildi = id!.EndsWith("-sutunlar-ac", StringComparison.Ordinal)
                              && e.FindAll($"#{id[..^"-ac".Length]}").Count > 0;
                Assert.True(istekler.Count > 0 || modalAcildi || panelAcildi, $"{id}: tıklanınca hiçbir şey olmadı");
            });
            if (panelAcildi) { e.Find($"#{id}").Click(); continue; }   // paneli kapat
            if (modalAcildi) e.Find("#bildirim-kapat-vazgec").Click();
            // Sıradaki düğmeden önce uçuşan eylem bitsin (calisanIs bekçisi sıfırlansın).
            e.WaitForAssertion(() => Assert.Empty(e.FindAll("button[disabled]:not([title])")));
        }
    }

    // ── İş paneli ───────────────────────────────────────────────────────────

    [Fact]
    public void Kapali_is_dugmesi_DISABLED_ve_gerekce_tasir()
    {
        var e = Ciz();
        var kapali = e.Find("#is-calistir-KAPALI_IS");

        Assert.True(kapali.HasAttribute("disabled"));
        Assert.Contains("kapalı", kapali.GetAttribute("title")!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Simdi_calistir_POST_calistir_ucuna_gider()
    {
        var e = Ciz();
        istekler.Clear();

        e.Find("#is-calistir-VADE_HATIRLATMA").Click();

        Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/isler/VADE_HATIRLATMA/calistir"));
    }

    [Fact]
    public void Tezgahtar_is_panelini_GORMEZ()
    {
        var e = Ciz("TEZGAHTAR");
        Assert.Empty(e.FindAll("#bildirim-is-paneli"));
        Assert.Single(e.FindAll("#bildirim-is-yetki-notu"));
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/isler"));
    }

    // ── Üst bar rozeti ──────────────────────────────────────────────────────

    [Fact]
    public void Rozet_sayi_0_iken_DOM_da_YOKTUR()
    {
        sayiYaniti = """{"sayi":0,"enYuksekOnem":null}""";
        var e = Render<UstBar>();
        Assert.Empty(e.FindAll("#ust-bildirim-rozet"));
        Assert.Empty(e.FindAll("#ust-bildirim-rozet-hata"));
    }

    [Fact]
    public async Task Rozet_sayi_3_olunca_gorunur_ve_ELLE_YENILE_ile_guncellenir()
    {
        sayiYaniti = """{"sayi":0,"enYuksekOnem":null}""";
        var e = Render<UstBar>();
        Assert.Empty(e.FindAll("#ust-bildirim-rozet"));

        // ⚠ 60 sn'lik zamanlayıcı BEKLENMEZ; elle yenileme çağrılır.
        sayiYaniti = """{"sayi":3,"enYuksekOnem":"KRITIK"}""";
        await e.InvokeAsync(() => e.Instance.RozetiYenile());

        var rozet = e.Find("#ust-bildirim-rozet");
        Assert.Equal("3", e.Find("#ust-bildirim-rozet .bildirim-sayi").TextContent.Trim());
        Assert.Contains("bildirim-rozet-kritik", rozet.ClassName, StringComparison.Ordinal);
        Assert.Equal("/bildirimler", rozet.GetAttribute("href"));

        // UX-3 / mevcut sözleşme: mod anahtarı ve id'leri BOZULMADI.
        Assert.NotNull(e.Find("#ust-mod-anahtar"));
        Assert.NotNull(e.Find("#ust-mod-basit"));
        Assert.NotNull(e.Find("#ust-mod-profesyonel"));
    }

    // ═══ PAKET P-11 (B-24) — ZAMANLANMIŞ İŞ AYARLARI EKRANI ═════════════════

    [Fact]
    public void Ayar_karti_cizilir_ve_degerler_SUNUCUDAN_gelir()
    {
        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#is-ayar-kart")));

        Assert.Equal("0", c.Find("#is-ayar-saat").GetAttribute("value"));
        Assert.Equal("7", c.Find("#is-ayar-vade").GetAttribute("value"));
        Assert.Equal("3", c.Find("#is-ayar-masak").GetAttribute("value"));
        // UX-S29/S30: sınır ve kurulum varsayılanı EKRANDA yazar.
        Assert.Contains("0-23", c.Find("#is-ayar-saat-ipucu").TextContent);
        Assert.Contains("1-365", c.Find("#is-ayar-vade-ipucu").TextContent);
        Assert.Contains("7", c.Find("#is-ayar-vade-ipucu").TextContent);
    }

    [Fact]
    public void Kaydet_dugmesi_GERCEK_PATCH_atar_ve_sonucu_banda_yazar()
    {
        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#is-ayar-saat")));

        c.Find("#is-ayar-saat").Input("9");
        c.Find("#is-ayar-vade").Input("14");
        c.Find("#is-ayar-kaydet").Click();

        c.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "PATCH" && i.Yol.EndsWith("/isler/ayarlar")));
        var istek = istekler.Last(i => i.Metot == "PATCH");
        Assert.Contains("\"baslangicSaat\":9", istek.Govde);
        Assert.Contains("\"vadeUfukGun\":14", istek.Govde);
        // UX-S27: sunucunun döndürdüğü sonuç banda yazılır.
        c.WaitForAssertion(() => Assert.Contains("kaydedildi", c.Markup));
    }

    [Fact]
    public void Aralik_disi_deger_ALAN_ALTI_mesaj_verir_ve_ISTEK_ATILMAZ()
    {
        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#is-ayar-saat")));
        var oncekiPatch = istekler.Count(i => i.Metot == "PATCH");

        c.Find("#is-ayar-saat").Input("49");
        c.Find("#is-ayar-kaydet").Click();

        // Sessiz etkisizlik YASAK: alan altı mesaj çıkar, istek gitmez.
        c.WaitForAssertion(() => Assert.Contains("0-23", c.Markup));
        Assert.Equal(oncekiPatch, istekler.Count(i => i.Metot == "PATCH"));
    }

    [Fact]
    public void Varsayilana_don_alanlari_NULL_gonderir()
    {
        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#is-ayar-varsayilan")));

        c.Find("#is-ayar-varsayilan").Click();

        c.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "PATCH" && i.Yol.EndsWith("/isler/ayarlar")));
        var istek = istekler.Last(i => i.Metot == "PATCH");
        Assert.Contains("\"vadeUfukGun\":null", istek.Govde);
        // Kapatılan işler bu düğmeden ETKİLENMEZ: alan gövdede YOK.
        Assert.DoesNotContain("kapaliIsler", istek.Govde);
    }

    [Fact]
    public void Is_acma_kapama_kutusu_GERCEK_PATCH_atar()
    {
        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#is-acik-VADE_HATIRLATMA")));

        c.Find("#is-acik-VADE_HATIRLATMA").Change(false);

        c.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "PATCH" && i.Yol.EndsWith("/isler/ayarlar")));
        var istek = istekler.Last(i => i.Metot == "PATCH");
        Assert.Contains("VADE_HATIRLATMA", istek.Govde);
        Assert.Contains("KIRACI_KAPALI_IS", istek.Govde);   // mevcut kapama KORUNDU
    }

    [Fact]
    public void Yapilandirmada_kapali_isin_kutusu_KILITLI_ve_sebebi_yazili()
    {
        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#is-acik-KAPALI_IS")));

        Assert.True(c.Find("#is-acik-KAPALI_IS").HasAttribute("disabled"));
        Assert.Contains("kurulum", c.Find("#is-yapkapali-KAPALI_IS").TextContent);
        // Kiracının kapattığı iş ise DÜZENLENEBİLİR kalır (UX-S25).
        Assert.False(c.Find("#is-acik-KIRACI_KAPALI_IS").HasAttribute("disabled"));
        Assert.NotNull(c.Find("#is-kiracikapali-KIRACI_KAPALI_IS"));
    }

    [Fact]
    public void Kiracinin_kapattigi_isin_CALISTIR_dugmesi_KILITLI_ve_sebebi_AYRIDIR()
    {
        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#is-calistir-KIRACI_KAPALI_IS")));

        var kiraci = c.Find("#is-calistir-KIRACI_KAPALI_IS");
        var yapilandirma = c.Find("#is-calistir-KAPALI_IS");
        Assert.True(kiraci.HasAttribute("disabled"));
        Assert.True(yapilandirma.HasAttribute("disabled"));
        // UX-S30: iki farklı sebep, iki farklı metin.
        Assert.NotEqual(kiraci.GetAttribute("title"), yapilandirma.GetAttribute("title"));
    }

    // ═══ ORTAK-1 · KEŞİF #4 (30.09, KRİTİK) — iş ayarı "1.500" ═════════════════════════════════════
    // Kapısız: "1.5" ara değeri (int → 1) kalıyor, kutu hata yazarken PATCH 1 saat/gün gidebilirdi.
    [Fact]
    public void OK1_K4_is_ayari_belirsiz_1_500_PATCH_YOK_sebep_yazar()
    {
        var c = Ciz();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#is-ayar-vade")));
        foreach (var a in new[] { "1", "1.", "1.5", "1.50", "1.500" }) c.Find("#is-ayar-vade").Input(a);
        c.Find("#is-ayar-vade").Blur();
        Assert.Equal("1.500", c.Find("#is-ayar-vade").GetAttribute("value"));
        Assert.Equal("true", c.Find("#is-ayar-vade").GetAttribute("aria-invalid"));
        Assert.True(c.Find("#is-ayar-kaydet").HasAttribute("disabled"));
        Assert.Contains(c.Find("label[for='is-ayar-vade']").TextContent.Replace("*", "").Split("(isteğe")[0].Trim(),
                        c.Find("#is-ayar-sayi-kilit").TextContent);
        istekler.Clear();
        c.Find("#is-ayar-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
        c.Find("#is-ayar-vade").Input("14");
        Assert.False(c.Find("#is-ayar-kaydet").HasAttribute("disabled"));
    }
}
