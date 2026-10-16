using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-2 — TEZGÂH FİŞİ: DÜĞME IZGARASI + TEK GRID VERDİM/ALDIM + FARK
// (PLAN-SADELESTIRME § S-2 madde 2.2 + 2.3 · taslak Tezgah.dc.html)
//
// Sınanan:
//   • FARK = VERDİM − ALDIM; Kaydet YALNIZ fark tam 0 iken açık (saf + ekran)
//   • fark > 0 + gerçek müşteri → "Açık hesap olarak kaydet" VERESİYE bacağıyla
//     TEK POST'ta; Peşin Müşteri'de KİLİTLİ ve sebebini yazar
//   • basit modda 8 düğmelik ızgara (5 tür + Tahsilat/Ödeme/Emanet), ≥ 44 px
//     sınıfı, #islem-tur-* id'leri KORUNUR; pro modda 8 sekme AYNEN
//   • basit satışta ilk bakışta ≤ 3 alan + ≤ 12 düğme
//   • "Ödemeyi böl" → tek ödeme ilk bacak olur; karma gövdede vadeGun YOK
//
// ⚠ Eşik SAYISI teste gömülmez; tutarlar sahte sunucunun fiyat teklifinden.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S22TezgahFisiTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private static readonly TezgahFisiMetinleri M = TezgahFisiSozlugu.Tr;

    public S22TezgahFisiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, yol, govde));

            var y = yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") =>
                    """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/pos/cihazlar") =>
                    """[{"id":"pc1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"s1","kasaId":"ks1","pesinKomisyonOran":1.5,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}]""",
                var v when v.Contains("/islemler/satis-fisi") => """{"fisId":"f1","fisNo":"FIS-000123","iskontoToplam":0}""",
                var v when v.Contains("/islemler/satis") => """{"id":"h1","fisId":"f1"}""",
                var v when v.Contains("/iade-adaylari") => "[]",
                var v when v.Contains("/reyon") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-tur-SATIS");
        return e;
    }

    /// <summary>Basit satış ekranı + 10.000 TL'lik bir kalem (elle satırından).</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> KalemliFis(string? cari = null)
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        e.WaitForAssertion(() => Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        if (cari is not null) SeciciYardim.Sec(e, "islem-cari", cari);
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.Equal("10000", SeciciYardim.SeciliDeger(e, "islem-birimfiyat")));
        e.Find("#islem-kalem-ekle").Click();
        e.WaitForElement("#islem-kalem-liste-sar");
        return e;
    }

    private static void Bol(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        e.Find("#islem-odeme-bol").Click();
        e.WaitForElement("#islem-bacak-0-tutar");
    }

    /// <summary>SATIS-3: "+ Başka ödeme ekle" artık İKİ satır açar; tek satırlı karma isteyen senaryo ikinciyi siler.</summary>
    private static void BolTekSatir(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        Bol(e);
        e.WaitForElement("#islem-bacak-sil-1").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-bacak-1-arac")));
    }

    private static bool KaydetKilitli(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) =>
        e.Find("#islem-kaydet").HasAttribute("disabled");

    private List<JsonElement> FisPostlari()
    {
        lock (istekler)
            return istekler.Where(i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis-fisi"))
                           .Select(i => JsonDocument.Parse(i.Govde).RootElement).ToList();
    }

    // ═══ SAF: FARK KURALI (FisFark) ════════════════════════════════════════

    [Fact]
    public void FisFark_fark_VERDIM_eksi_ALDIM_tek_odemede_sifir()
    {
        var b = new List<FisBacagi> { new() { TutarTl = 6_000m }, new() { TutarTl = 3_000m } };
        Assert.Equal(1_000m, FisFark.Hesapla(10_000m, b));
        Assert.Equal(-2_000m, FisFark.Hesapla(7_000m, b));
        Assert.Equal(0m, FisFark.Hesapla(10_000m, null));
    }

    [Fact]
    public void FisFark_Kaydet_YALNIZ_tam_sifirda_acik()
    {
        Assert.True(FisFark.KaydetAcik(0m));
        Assert.False(FisFark.KaydetAcik(0.01m));
        Assert.False(FisFark.KaydetAcik(-0.01m));
        Assert.Equal(FarkDurumu.Tamam, FisFark.Durum(0m));
        Assert.Equal(FarkDurumu.Eksik, FisFark.Durum(5m));
        Assert.Equal(FarkDurumu.Fazla, FisFark.Durum(-5m));
    }

    [Fact]
    public void FisFark_acik_hesap_Pesin_Musteride_KILITLI_gercek_caride_ACIK()
    {
        var bos = new List<FisBacagi>();
        Assert.Null(FisFark.AcikHesapKilidi(4_000m, "c1", "pm1", bos, M));
        Assert.Equal(M.AcikHesapPesin, FisFark.AcikHesapKilidi(4_000m, "pm1", "pm1", bos, M));
        Assert.Equal(M.AcikHesapCariYok, FisFark.AcikHesapKilidi(4_000m, "", "pm1", bos, M));
        Assert.Equal(M.FarkTamam, FisFark.AcikHesapKilidi(0m, "c1", "pm1", bos, M));
        Assert.Equal(M.FarkTamam, FisFark.AcikHesapKilidi(-1m, "c1", "pm1", bos, M));
        var veresiyeli = new List<FisBacagi> { new() { Arac = "VERESIYE", TutarTl = 1m } };
        Assert.Equal(M.AcikHesapVarZaten, FisFark.AcikHesapKilidi(4_000m, "c1", "pm1", veresiyeli, M));
    }

    [Fact]
    public void FisFark_acik_hesap_bacagi_VERESIYE_kasasiz_ve_farki_kapatir()
    {
        var b = FisFark.AcikHesapBacagi(4_000m);
        Assert.Equal("VERESIYE", b.Arac);
        Assert.Equal(4_000m, b.TutarTl);
        Assert.Equal("", b.KasaId);
    }

    [Fact]
    public void FisFark_ilk_bacak_tek_odemeyi_tasir_tanimadigi_arac_NAKITe_duser()
    {
        var k = FisFark.IlkBacak("KART", "ks1", 10_000m);
        Assert.Equal(("KART", 10_000m, "ks1"), (k.Arac, k.TutarTl, k.KasaId));
        var a = FisFark.IlkBacak("ALTIN", "ks1", 10_000m);
        Assert.Equal(("NAKIT", 10_000m, ""), (a.Arac, a.TutarTl, a.KasaId));
    }

    [Fact]
    public void BasitPos_izgara_dort_tur_uc_kisayol_ODEME_kilitli()
    {
        // PAKET T-S1: ALIS tuvalde karosuz → PRO (Turler 5 → 4).
        Assert.Equal(["SATIS", "HURDA_ALIS", "TAKAS", "SATIS_IADE"], BasitPos.Turler);
        // SATIS-3 t11 (tasarım hakemi YÜKSEK): tedarikçiye ödeme ucu yok → hep pasif ODEME kısayolu KALDIRILDI; kalanların hepsi gerçek adres.
        Assert.Equal(["TAHSILAT", "EMANET"], BasitPos.Kisayollar.Select(k => k.Kod).ToArray());
        Assert.All(BasitPos.Kisayollar, k => Assert.False(string.IsNullOrEmpty(k.Adres), k.Kod));
        Assert.False(BasitPos.TurGorunur("TRANSFER"));
    }

    [Fact]
    public void TezgahFisi_sozlugu_TR_EN_bos_anahtar_yok_ve_esik_sayisi_yok()
    {
        foreach (var m in new[] { TezgahFisiSozlugu.Tr, TezgahFisiSozlugu.En })
            foreach (var p in typeof(TezgahFisiMetinleri).GetProperties())
            {
                var v = (string)p.GetValue(m)!;
                Assert.False(string.IsNullOrWhiteSpace(v), p.Name);
                Assert.DoesNotMatch(@"\d{2,}", v);
            }
        Assert.Same(TezgahFisiSozlugu.En, TezgahFisiSozlugu.Al("en"));
        Assert.Same(TezgahFisiSozlugu.Tr, TezgahFisiSozlugu.Al("tr"));
    }

    // ═══ EKRAN: DÜĞME IZGARASI ═════════════════════════════════════════════

    [Fact]
    public void Basit_izgara_sekiz_buyuk_dugme_id_ler_korunur_kisayollar_gercek_adres()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        var izgara = e.Find("#islem-izgara");

        var karolar = izgara.QuerySelectorAll(".ekran-karo");
        Assert.Equal(7, karolar.Length);   // SATIS-3 t11: Ödeme karosu (ucu yok, hep pasif) kaldırıldı
        foreach (var t in BasitPos.Turler)
            Assert.Contains("ekran-karo", e.Find($"#islem-tur-{t}").ClassName);
        Assert.Equal("true", e.Find("#islem-tur-SATIS").GetAttribute("aria-pressed"));
        Assert.Contains("ekran-karo-birincil", e.Find("#islem-tur-SATIS").ClassName);
        Assert.Equal("/emanet", e.Find("#islem-git-emanet").GetAttribute("href"));
        Assert.Equal("/cariler", e.Find("#islem-git-tahsilat").GetAttribute("href"));
        // DoD EK md.1: tıklanınca bir şey olmayan düğme yok — SATIS-3 t11: hep pasif Ödeme karosu hiç çizilmez
        Assert.Empty(e.FindAll("#islem-git-odeme, #islem-git-odeme-kilit"));
        Assert.All(karolar, k => Assert.False(k.HasAttribute("disabled") && k.Id != "islem-indirim", k.Id));
        // PAKET T-S1: tuvalde (Tezgah.dc.html) karolar İKONSUZ ve sekizincisi "İndirim"
        Assert.All(karolar, k => Assert.Null(k.QuerySelector("svg.ikon")));
        Assert.NotNull(izgara.QuerySelector("#islem-indirim"));
    }

    [Fact]
    public void Izgarada_tur_dugmesi_turu_degistirir()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        e.Find("#islem-tur-HURDA_ALIS").Click();

        e.WaitForAssertion(() => Assert.Equal("true", e.Find("#islem-tur-HURDA_ALIS").GetAttribute("aria-pressed")));
        Assert.Empty(e.FindAll("#islem-verdim-aldim"));   // tezgâh fişi yalnız satışta
    }

    [Fact]
    public void Tahsilat_gercek_musteride_o_musteriye_Pesin_Musteride_listeye_acilir()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        e.WaitForAssertion(() => Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        Assert.Equal("/cariler", e.Find("#islem-git-tahsilat").GetAttribute("href"));

        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.WaitForAssertion(() =>
            Assert.Equal("/cariler?cariId=c1", e.Find("#islem-git-tahsilat").GetAttribute("href")));
    }

    [Fact]
    public void Pro_modda_izgara_YOK_sekiz_sekme_AYNEN()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);

        Assert.Empty(e.FindAll("#islem-izgara"));
        Assert.Empty(e.FindAll("#islem-verdim-aldim"));
        var sekmeler = e.FindAll("[id^=islem-tur-]");
        Assert.Equal(8, sekmeler.Count);
        // T-S2 (tuval Profesyonel.dc.html): 6 tür şeritte sekme; İşçilik/Transfer "Diğer ▾" menüsünde (id'ler aynı).
        Assert.All(e.FindAll(".tuval-pro-sekmeler [id^=islem-tur-]"), s => Assert.Contains("sekme", s.ClassName));
        Assert.Equal(6, e.FindAll(".tuval-pro-sekmeler [id^=islem-tur-]").Count);
        Assert.NotNull(e.Find("#islem-diger-menu #islem-tur-ISCILIK"));
        Assert.NotNull(e.Find("#islem-diger-menu #islem-tur-TRANSFER"));
        // SATIS-2 (d+e): PRO satışta da TEK giriş — elle satırı "Elle ekle" altında KATLI (özellik silinmedi).
        Assert.False(e.Find("#islem-elle").HasAttribute("open"));
        Assert.NotNull(e.Find("#islem-elle #islem-urun"));
    }

    // ═══ EKRAN: İLK BAKIŞ ÖLÇÜSÜ (≤ 3 alan + ≤ 12 düğme) ═══════════════════

    /// <summary>Kapalı &lt;details&gt; içindeki öğe ilk bakışta görünmez.</summary>
    private static bool Gorunur(IElement el)
    {
        for (var p = el.ParentElement; p is not null; p = p.ParentElement)
            if (p.LocalName == "details" && !p.HasAttribute("open")
                && !(el.LocalName == "summary" && el.ParentElement == p)) return false;
        return true;
    }

    [Fact]
    public void Basit_satista_ilk_bakista_en_cok_UC_alan_ve_ON_IKI_dugme()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        e.WaitForAssertion(() => Assert.Equal("pm1", SeciciYardim.SeciliDeger(e, "islem-cari")));
        var kapsam = new List<IElement> { e.Find("#islem-izgara"), e.Find("#islem-kaydet").Closest(".kart")! };

        var alanlar = kapsam.SelectMany(k => k.QuerySelectorAll(
                "[role=combobox], input:not([type=hidden]), select, textarea"))
            .Where(Gorunur).ToList();
        var dugmeler = kapsam.SelectMany(k => k.QuerySelectorAll(
                "button:not([role=combobox]), a.ekran-karo, summary"))
            .Where(Gorunur).ToList();

        // SATIS-2 (kurucu 11:35): Açıklama alanı "+ Ayrıntı ekle" altına katlandı → ilk bakışta alan 3 → 2,
        // yerine bir açıcı (summary) geldi; ilk bakıştaki toplam denetim sayısı (3 + 12) AŞILMAZ.
        Assert.True(alanlar.Count <= 2, $"alan {alanlar.Count}: " + string.Join(", ", alanlar.Select(a => a.Id)));
        Assert.DoesNotContain(alanlar, a => a.Id == "islem-aciklama");
        Assert.True(alanlar.Count + dugmeler.Count <= 15, $"alan+düğme {alanlar.Count + dugmeler.Count}");
        Assert.True(dugmeler.Count <= 13, $"düğme {dugmeler.Count}: " + string.Join(", ", dugmeler.Select(a => a.Id ?? a.TextContent.Trim())));
        // üç alan: müşteri, barkod, açıklama
        Assert.Contains(alanlar, a => a.Id == "islem-cari");
        Assert.Contains(alanlar, a => a.Id == "islem-barkod");
    }

    [Fact]
    public void Basit_modda_elle_satiri_KATLI_urun_secilince_KENDILIGINDEN_acik()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        Assert.False(e.Find("#islem-elle").HasAttribute("open"));
        Assert.NotNull(e.Find("#islem-elle #islem-urun"));   // özellik silinmedi, katlı

        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.True(e.Find("#islem-elle").HasAttribute("open")));
    }

    // ═══ EKRAN: VERDİM / ALDIM + FARK ══════════════════════════════════════

    [Fact]
    public void Tek_odemede_VERDIM_ALDIM_tek_kartta_FARK_sifir_KAYDET_acik()
    {
        var e = KalemliFis();

        Assert.NotNull(e.Find("#islem-verdim #islem-kalem-liste-sar"));
        Assert.NotNull(e.Find("#islem-aldim #islem-odeme-sade"));
        Assert.Contains("10.000", e.Find("#islem-verdim-toplam").TextContent);
        Assert.Contains(Bicim.Tl(0m), e.Find("#islem-fark-tutar").TextContent);
        Assert.Equal(M.FarkTamam, e.Find("#islem-fark-durum").TextContent.Trim());
        Assert.False(KaydetKilitli(e));
        Assert.Empty(e.FindAll("#islem-acik-hesap"));
    }

    [Fact]
    public void Odemeyi_bol_tek_odeme_ILK_bacak_olur_fis_toplami_ve_kasasiyla()
    {
        var e = KalemliFis();
        Bol(e);

        Assert.Equal("10000", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar"));
        Assert.Equal("NAKIT", SeciciYardim.SeciliDeger(e, "islem-bacak-0-arac"));
        Assert.Empty(e.FindAll("#islem-bacak-0-kasa"));   // SATIS-3: tek kasa → seçici yok (kasa bacakta taşınır)
        Assert.Contains(Bicim.Tl(0m), e.Find("#islem-fark-tutar").TextContent);
        // SATIS-3 doğrulama: "+ Başka ödeme ekle" ilk tıkta İKİNCİ (boş) ödeme satırını da açar;
        // boş satır tutar girilene ya da silinene dek Kaydet'i kilitler.
        // SATIS-3 t10 (hakem KRİTİK): ikinci satır ilk satırla AYNI araçla açılmaz — Nakit'ten bölününce Kart.
        Assert.Equal("KART", SeciciYardim.SeciliDeger(e, "islem-bacak-1-arac"));
        // k2 (tasarım hakemi K19, 29.09): fark 0 iken boş (0) satır Kaydet'i KİLİTLEMEZ; gövdeye girmez (SATIS3 testi).
        Assert.False(KaydetKilitli(e));
        e.Find("#islem-bacak-sil-1").Click();
        e.WaitForAssertion(() => Assert.False(KaydetKilitli(e)));
    }

    [Fact]
    public void FARK_kapanmadan_KAYDET_kilitli_kapaninca_nakit_kart_TEK_POSTta_gider()
    {
        var e = KalemliFis();
        Bol(e);
        e.Find("#islem-bacak-0-tutar").Input("6000");
        // SATIS-3: elle yazılmayan ikinci satır kalanı (4.000) KENDİ üstlenir
        e.WaitForAssertion(() => Assert.Equal("4000", SeciciYardim.SeciliDeger(e, "islem-bacak-1-tutar")));
        e.Find("#islem-bacak-1-tutar").Input("3000");     // ikisi de elle → fark kapanmaz

        e.WaitForAssertion(() => Assert.Contains("1.000", e.Find("#islem-fark-tutar").TextContent));
        Assert.Equal(M.FarkEksik, e.Find("#islem-fark-durum").TextContent.Trim());
        Assert.True(KaydetKilitli(e));

        e.Find("#islem-bacak-1-tutar").Input("4000");
        SeciciYardim.Sec(e, "islem-bacak-1-arac", "KART");
        e.WaitForAssertion(() => Assert.False(KaydetKilitli(e)));
        Assert.Contains(Bicim.Tl(0m), e.Find("#islem-fark-tutar").TextContent);

        e.Find("#islem-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(FisPostlari()));
        var g = FisPostlari()[0];
        var od = g.GetProperty("odemeler").EnumerateArray().ToList();
        Assert.Equal(2, od.Count);
        Assert.Equal(("NAKIT", 6000m), (od[0].GetProperty("arac").GetString(), od[0].GetProperty("tutarTl").GetDecimal()));
        Assert.Equal(("KART", 4000m), (od[1].GetProperty("arac").GetString(), od[1].GetProperty("tutarTl").GetDecimal()));
        // basit modda gizli alan gövdeye KONMAZ (Mehmet K7) — karma gövdede de
        Assert.False(g.TryGetProperty("vadeGun", out _));
        e.WaitForAssertion(() => Assert.Contains("f1", e.Find("#basari-bandi").TextContent));
    }

    [Fact]
    public void Fazla_odemede_KAYDET_kilitli_ve_sebep_yazar()
    {
        var e = KalemliFis();
        Bol(e);
        e.Find("#islem-bacak-0-tutar").Input("12000");

        e.WaitForAssertion(() => Assert.Equal(M.FarkFazla, e.Find("#islem-fark-durum").TextContent.Trim()));
        Assert.True(KaydetKilitli(e));
        Assert.Empty(e.FindAll("#islem-acik-hesap"));
    }

    [Fact]
    public void Acik_hesap_Pesin_Musteride_KILITLI_sebep_yazar_istek_gitmez()
    {
        var e = KalemliFis();
        BolTekSatir(e);
        e.Find("#islem-bacak-0-tutar").Input("6000");

        e.WaitForElement("#islem-acik-hesap");
        Assert.True(e.Find("#islem-acik-hesap").HasAttribute("disabled"));
        Assert.Equal(M.AcikHesapPesin, e.Find("#islem-acik-hesap-kilit").TextContent.Trim());
        Assert.Empty(FisPostlari());
    }

    [Fact]
    public void Acik_hesap_gercek_caride_farki_VERESIYE_bacagiyla_TEK_POSTta_kaydeder()
    {
        var e = KalemliFis("c1");
        BolTekSatir(e);
        e.Find("#islem-bacak-0-tutar").Input("6000");
        e.WaitForAssertion(() => Assert.False(e.Find("#islem-acik-hesap").HasAttribute("disabled")));

        e.Find("#islem-acik-hesap").Click();

        e.WaitForAssertion(() => Assert.Single(FisPostlari()));
        var g = FisPostlari()[0];
        Assert.Equal("c1", g.GetProperty("cariId").GetString());
        var od = g.GetProperty("odemeler").EnumerateArray().ToList();
        Assert.Equal(2, od.Count);
        Assert.Equal("VERESIYE", od[1].GetProperty("arac").GetString());
        Assert.Equal(4000m, od[1].GetProperty("tutarTl").GetDecimal());
        Assert.Equal(JsonValueKind.Null, od[1].GetProperty("kasaId").ValueKind);
        Assert.Equal(10000m, od.Sum(o => o.GetProperty("tutarTl").GetDecimal()));
    }

    [Fact]
    public void Tek_odemeye_don_bacaklari_siler_tek_odeme_geri_gelir()
    {
        var e = KalemliFis();
        Bol(e);
        e.Find("#islem-odeme-birlestir").Click();

        e.WaitForElement("#islem-odeme-bol");
        Assert.Empty(e.FindAll("#islem-bacak-0-arac"));
        Assert.NotNull(e.Find("#islem-aldim #islem-odeme-sade"));
        Assert.False(KaydetKilitli(e));
    }

    [Fact]
    public void Bos_fiste_bol_kilitli_ve_sebep_yazar()
    {
        var e = Ekran(ArayuzModuServisi.Basit);

        Assert.True(e.Find("#islem-odeme-bol").HasAttribute("disabled"));
        Assert.Equal(M.VerdimBos, e.Find("#islem-odeme-bol-kilit").TextContent.Trim());
        Assert.Equal(M.VerdimBos, e.Find("#islem-verdim-bos").TextContent.Trim());
    }

    // ═══ YAPI ══════════════════════════════════════════════════════════════

    private static string WebDosya(string yol)
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        return File.ReadAllText(Path.Combine(d!.FullName, "Kuyumcu.Web", yol));
    }

    [Theory]
    [InlineData("Bilesenler/Fis/FisIslemIzgarasi.razor")]
    [InlineData("Bilesenler/Fis/FisVerdimAldim.razor")]
    [InlineData("Bilesenler/Fis/FisIskontoSatiri.razor")]
    [InlineData("Bilesenler/Fis/FisFark.cs")]
    [InlineData("Bilesenler/Fis/FisTurleri.cs")]
    public void Yeni_fis_dosyalari_istek_atmaz_satir_ici_style_ve_sabit_renk_yok(string yol)
    {
        var k = WebDosya(yol);
        Assert.DoesNotContain("@inject ApiIstemci", k);
        Assert.DoesNotContain("PostAsync(", k);
        Assert.DoesNotContain("GetAsync(", k);
        Assert.DoesNotContain("new HttpClient", k);
        Assert.DoesNotContain("style=", k);
        Assert.DoesNotMatch("#[0-9a-fA-F]{3,8}\\b", k);
    }
}
