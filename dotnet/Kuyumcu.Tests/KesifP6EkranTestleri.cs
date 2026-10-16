using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P6 · SİPARİŞ-UYUM-KUR-YÖNETİM — ekran sözleşmeleri (bUnit).
//   #13 Siparişi oluştur tasarım sınıfı · #34/#35 tek müşteri seçicisi, ham tür 0,
//   Ayar seçicisi, BASİT'te "sunucu" 0 · #29 parite birimi sözlükten · #75 parite
//   kodu görünür metin değil · #86 beyan kademesi sözlükten · #72 saklama sekmesi
//   MÜDÜR'e çizilmez.
// ⚠ Sahte API her uca GERÇEKÇİ yanıt verir ('{}' varsayılanı yalnız ilgisiz uçlar).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KesifP6EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    /// <summary>Tur 2: sahte sunucudaki aktif müşteri sayısı (varsayılan 2 — küçük dükkân).</summary>
    private int cariSayisi = 2;
    private static readonly TimeSpan Uzun = TimeSpan.FromSeconds(10);   // tam süit yükünde 1 sn yetmiyor

    private const string SiparisListe = """
        {"istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":0,"dahaFazlasiVar":false,
         "gecikenSayisi":0,"durumKirilimi":[],"siparisler":[]}
        """;

    private const string KimlikDetay = """
        {"id":"kt1","tarih":"2026-09-16T09:00:00Z","adSoyad":"Ayşe Yılmaz","belgeTuru":"TC_KIMLIK",
         "belgeNo":"10000000146","dogumTarihi":null,"uyruk":"TUR","adres":"Kadıköy","islemTutariTl":340500,
         "kaynakTur":"HAREKET","saklamaBitis":"2034-09-16T09:00:00Z",
         "beyan":{"kademe":"AMAC_BEYANI","islemAmaci":"Yatırım","fonKaynagi":"Birikim",
                  "ekBelgeReferansi":null,"aciklama":"Hurda altın satışı için beyan"}}
        """;

    private const string SiparisDetayTaslak = """
        {"id":"s1","no":"S-2026-000001","tur":"MUSTERI_SIPARISI","durum":"TASLAK","cariId":"c1",
         "cariAd":"Ayşe Yılmaz","subeId":"sb1","sozVerilenGun":"2026-09-10","aciklama":"bilezik",
         "tahminiTutarTl":25000.00,"kurSnapshotId":"kur-1","emanetHareketId":null,
         "geciken":false,"duzenlenebilir":true,"terminal":false,
         "izinliGecisler":["ONAYLANDI","IPTAL"],"gerekceIsteyenGecisler":["IPTAL"],"gerekceAsgari":10,
         "olusturanKullaniciId":"u1","olusturuldu":"2026-09-16T08:00:00Z","guncellendi":"2026-09-16T08:00:00Z",
         "kalemler":[{"id":"k1","urunId":"u-1","urunAd":"Bilezik","tanim":null,"ayarId":"K22",
                      "gramAgirlik":10.0,"adet":1.0,"sira":0}],
         "gecisler":[{"id":"g1","oncekiDurum":null,"yeniDurum":"TASLAK","gerekce":null,
                      "kullaniciId":"u1","kullaniciAd":"Test Kullanıcı","olusturuldu":"2026-09-16T08:00:00Z"}]}
        """;

    public KesifP6EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => cariSayisi))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<int> cariSayisi) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, istek.RequestUri!.PathAndQuery, govde));
            var g = yol switch
            {
                // ── Sipariş ──
                // Tur 2: TASLAK detay (izinli ONAYLANDI + IPTAL) — detay düğme ailesi testleri.
                var y when y.EndsWith("/siparisler/s1") => SiparisDetayTaslak,
                // ── Kullanıcılar (tur 2: satır eylemleri) ──
                var y when y.EndsWith("/kullanicilar") => """
                    [{"id":"u1","ad":"Test","soyad":"Kullanıcı","email":"t@ornek.com","rol":"SAHIP","aktif":true,"sonGiris":null,"subeId":"sb1","mfaEtkin":true},
                     {"id":"u2","ad":"Ali","soyad":"Tezgâhtar","email":"ali.tezgahtar.uzun.eposta.adresi@kapalicarsi-kuyumculari.com.tr","rol":"TEZGAHTAR","aktif":true,"sonGiris":null,"subeId":"sb1","mfaEtkin":false},
                     {"id":"u3","ad":"Pasif","soyad":"Müdür","email":"p@ornek.com","rol":"MUDUR","aktif":false,"sonGiris":null,"subeId":null,"mfaEtkin":false}]
                    """,
                var y when y.EndsWith("/siparisler") && istek.Method == HttpMethod.Post =>
                    """{"id":"s9","no":"S-2026-000009"}""",
                var y when y.EndsWith("/siparisler") => SiparisListe,
                var y when y.EndsWith("/subeler") => """[{"id":"sb1","ad":"Merkez"}]""",
                // Tur 2: cariSayisi > 2 ise sunucu YerelTavan+1 (201) kayıt döndürür → seçici uzak kipe geçer.
                var y when y.EndsWith("/cari") && cariSayisi() > 2 =>
                    JsonSerializer.Serialize(Enumerable.Range(1, Math.Min(cariSayisi(), 201))
                        .Select(n => new { id = $"c{n}", ad = $"Müşteri {n:000}", tur = "MUSTERI" })),
                var y when y.EndsWith("/cari") =>
                    """[{"id":"c1","ad":"Ayşe Yılmaz","tur":"MUSTERI"},{"id":"c2","ad":"Has Toptan","tur":"TOPTANCI"}]""",
                // Sunucu araması: 201. sayfanın ötesindeki müşteri (yerel listede YOK) yalnız burada bulunur.
                var y when y.EndsWith("/arama") => """
                    {"sorgu":"Zeynep","tur":"CARI","limit":10,"ofset":0,"sayi":1,"dahaVar":false,"tavan":50,
                     "sonuclar":[{"kimlik":"c250","ad":"Zeynep Kaya","altBilgi":"0532 *** 12 34","tur":"CARI","rota":"/cariler?cariId=c250"}]}
                    """,
                var y when y.EndsWith("/urunler") =>
                    """[{"id":"u-1","ad":"Bilezik","ayarId":"K22","gramAgirlik":10.0}]""",
                // ── Kur ──
                var y when y.EndsWith("/kur/kaynaklar") =>
                    """{"kaynaklar":["ALTINKAYNAK","ANKARA"],"varsayilan":"ALTINKAYNAK","secili":"ALTINKAYNAK","sozlesmesizKaynaklarAcik":true,"seciliSozlesmesiz":false,"manuelKod":"MANUAL"}""",
                var y when y.EndsWith("/kur/gecmis") => "[]",
                var y when y.EndsWith("/kur/pariteler") => """
                    {"kaynak":"ALTINKAYNAK","pariteler":[
                      {"kod":"CEYREK_YENI","ad":"Yeni Çeyrek","alis":11500,"satis":11800,"birim":"ADET"},
                      {"kod":"GRAM_ALTIN","ad":"Gram Altın","alis":4200,"satis":4300,"birim":"GRAM"},
                      {"kod":"HAS_ALTIN","ad":"Has Altın","alis":4100,"satis":4250,"birim":"HAS_GRAM"}]}
                    """,
                var y when y.EndsWith("/kur/guncel") => """
                    {"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":7100,"usdTry":34.2,"eurTry":null,"gbpTry":null,
                     "kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-30T09:00:00Z","ayarFiyatlari":[]}
                    """,
                // ── Uyum ──
                var y when y.EndsWith("/masak/ozet") =>
                    """{"acikBildirim":0,"suresiGecmis":0,"yaklasan":0,"son30GunTespit":1}""",
                var y when y.Contains("kimlik-tespitleri/ara") =>
                    """{"adet":0,"toplamTutarTl":0,"ilkIslem":null,"sonIslem":null,"kayitlar":[]}""",
                var y when y.EndsWith("/masak/kimlik-tespitleri") =>
                    """[{"id":"kt1","tarih":"2026-09-16T09:00:00Z","adSoyad":"Ayşe Yılmaz","belgeTuru":"TC_KIMLIK","belgeNo":"100*****146","islemTutariTl":340500,"saklamaBitis":"2034-09-16T09:00:00Z"}]""",
                var y when y.Contains("/masak/kimlik-tespitleri/") => KimlikDetay,
                var y when y.EndsWith("/masak/supheli") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private void Mod(bool basit) => Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);

    private void Oturum(string rol, string mod = "PROFESYONEL") =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "sb1", mod, mod));

    private List<(string Metot, string Yol, string Govde)> Istekler() { lock (istekler) return [.. istekler]; }

    private IRenderedComponent<Kuyumcu.Web.Pages.Siparisler> SiparisFormu()
    {
        var e = Render<Kuyumcu.Web.Pages.Siparisler>();
        e.WaitForElement("#sip-yeni-ac", Uzun).Click();
        e.WaitForElement("#sip-form", Uzun);
        return e;
    }

    // ── #13 ──────────────────────────────────────────────────────────────────
    [Fact]
    public void Siparis_olustur_dugmesi_tasarim_sinifi()
    {
        var e = SiparisFormu();
        var kaydet = e.Find("#sip-f-kaydet").ClassList;
        Assert.Contains("btn", kaydet);
        Assert.Contains("btn-birincil", kaydet);
        var iptal = e.Find("#sip-f-vazgec").ClassList;
        Assert.Contains("btn", iptal);
        Assert.Contains("btn-ikincil", iptal);
        Assert.DoesNotContain("btn-birincil", iptal);
        // TOPLU DUGME-2: form açıkken üstteki "Düzenlemeyi bitir" birincil mavi KALMAZ (tek birincil).
        Assert.DoesNotContain("dugme-birincil", e.Find("#sip-yeni-ac").ClassList);
    }

    // ── #34 / #35 ────────────────────────────────────────────────────────────
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Siparis_musteri_tek_secici_ham_tur_yok(bool basit)
    {
        Mod(basit);
        var e = SiparisFormu();
        var form = e.Find("#sip-form");

        // Tek arama seçicisi: ayrı "Ara" kutusu 0, yerli <select> 0.
        Assert.Empty(e.FindAll("#sip-f-cari-ara"));
        Assert.Empty(form.QuerySelectorAll("select"));
        Assert.Equal("combobox", e.Find("#sip-f-cari").GetAttribute("role"));

        // Seçenekte ham cari tür kodu yok (yalnız ad).
        var liste = SeciciYardim.ListeMetni(e, "sip-f-cari");
        Assert.Contains("Ayşe Yılmaz", liste);
        Assert.DoesNotContain("MUSTERI", liste);
        Assert.DoesNotContain("TOPTANCI", liste);
        e.Find("#sip-f-cari").Click();   // listeyi kapat

        // Kalem ayarı serbest metin değil, Ayar seçicisi; ürün seçilince ayar dolar (UX-S24).
        Assert.Equal("combobox", e.Find("#sip-f-kalem-ayar-0").GetAttribute("role"));
        SeciciYardim.Sec(e, "sip-f-kalem-urun-0", "u-1");
        Assert.Equal("K22", SeciciYardim.SeciliDeger(e, "sip-f-kalem-ayar-0"));
        Assert.Contains("22 Ayar", e.Find("#sip-f-kalem-ayar-0").TextContent);

        // BASİT'te "sunucu" sözcüğü 0.
        if (basit)
        {
            Assert.DoesNotContain("sunucu", e.Find("#sip-form").TextContent, StringComparison.OrdinalIgnoreCase);
            e.Find("#sip-f-detay-ac").Click(); // tur 2: tahmini tutar "Daha fazla"da
            Assert.DoesNotContain("sunucu", e.Find("#sip-form").TextContent, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(KesifP6Sozlugu.Tr.TeklifAlBasit, e.Find("#sip-f-teklif").TextContent.Trim());
            Assert.Contains(KesifP6Sozlugu.Tr.MusteriBasit, e.Find("label[for='sip-f-cari']").TextContent);
        }

        // Seçim gövdeye gider: cari ULID + Ayar seçicisinin kodu.
        SeciciYardim.Sec(e, "sip-f-cari", "c1");
        SeciciYardim.Sec(e, "sip-f-kalem-ayar-0", "K14");
        e.Find("#sip-f-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "POST" && i.Yol.EndsWith("/siparisler")), Uzun);
        var j = JsonDocument.Parse(Istekler().Last(i => i.Metot == "POST").Govde).RootElement;
        Assert.Equal("c1", j.GetProperty("cariId").GetString());
        Assert.Equal("K14", j.GetProperty("kalemler")[0].GetProperty("ayarId").GetString());
    }

    [Fact]
    public void Siparis_kalemleri_kart_satiri_ADET_alani_her_kalemde_var()
    {
        var e = SiparisFormu();
        // #34: kalem TABLOSU yok (1440'ta taşıyordu); her kalem bir kart, ADET alanı görünür.
        Assert.Empty(e.FindAll("#sip-f-kalemler table"));
        e.Find("#sip-f-kalem-ekle").Click();
        Assert.Equal(2, e.FindAll(".sip-kalem").Count);
        Assert.NotNull(e.Find("#sip-f-kalem-adet-1"));
        Assert.NotNull(e.Find("label[for='sip-f-kalem-adet-1']"));
    }

    // ── Tur 2 (doğrulayıcı ORTA): 200'ü aşan müşteri listesi → SUNUCU araması ──────
    [Fact]
    public void Siparis_musteri_200_ustu_dukkanda_sunucuda_aranir_ve_secilir()
    {
        cariSayisi = 250;
        var e = SiparisFormu();
        // Yerel listede 201 kayıt var ama 250. müşteri YOK; seçici sunucu aramasına geçmeli.
        e.Find("#sip-f-cari").Click();
        e.WaitForElement("#sip-f-cari-ara-girdi", Uzun).Input("Zeynep");
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i =>
            i.Metot == "GET" && i.Yol.Contains("/arama?q=Zeynep", StringComparison.Ordinal)
            && i.Yol.Contains("tur=CARI", StringComparison.Ordinal)), Uzun);
        e.WaitForElement("#sip-f-cari-secenek-0", Uzun).Click();
        e.WaitForAssertion(() => Assert.Contains("Zeynep Kaya", e.Find("#sip-f-cari").TextContent), Uzun);
        SeciciYardim.Sec(e, "sip-f-kalem-urun-0", "u-1");
        e.Find("#sip-f-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "POST" && i.Yol.EndsWith("/siparisler")), Uzun);
        var j = JsonDocument.Parse(Istekler().Last(i => i.Metot == "POST").Govde).RootElement;
        Assert.Equal("c250", j.GetProperty("cariId").GetString());
    }

    [Fact]
    public void Siparis_musteri_kucuk_dukkanda_yerel_listeden_istek_atmadan_secilir()
    {
        var e = SiparisFormu();
        SeciciYardim.Sec(e, "sip-f-cari", "c2");
        Assert.DoesNotContain(Istekler(), i => i.Yol.Contains("/arama", StringComparison.Ordinal));
        Assert.Contains("Has Toptan", e.Find("#sip-f-cari").TextContent);
    }

    // ── #29 / #75 ────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("BASIT")]
    [InlineData("PROFESYONEL")]
    public void Kur_parite_birim_sozlukten(string mod)
    {
        Oturum("SAHIP", mod);
        var e = Render<Kuyumcu.Web.Pages.Kur>();
        e.WaitForElement("#kur-sekme-pariteler", Uzun).Click();
        e.WaitForElement("#kur-parite-tablo", Uzun);
        var tablo = e.Find("#kur-parite-tablo");
        var metin = tablo.TextContent;
        Assert.Contains("Has gram", metin);
        Assert.Contains("Gram", metin);
        Assert.Contains("Adet", metin);
        foreach (var ham in new[] { "HAS_GRAM", "CEYREK_YENI", "GRAM_ALTIN", "HAS_ALTIN" })
            Assert.DoesNotContain(ham, metin);
        Assert.DoesNotContain(">ADET<", tablo.InnerHtml);
        Assert.DoesNotContain(">GRAM<", tablo.InnerHtml);
        // Ekip kararı #2: ham kod yalnız PRO'da title ipucunda.
        // PAKET GOC-A: ortak ızgarada title ad hücresinin İÇİNDEKİ öğededir (hücre özniteliği satıra göre verilemez).
        var adHucresi = tablo.QuerySelector("tbody tr td [title], tbody tr td span.hucre-vurgu")!;
        if (mod == "PROFESYONEL") Assert.Equal("CEYREK_YENI", adHucresi.GetAttribute("title"));
        else Assert.Null(adHucresi.GetAttribute("title"));
    }

    // ── #86 ──────────────────────────────────────────────────────────────────
    [Fact]
    public void Uyum_beyan_kademesi_sozlukten()
    {
        Oturum("SAHIP");
        var c = Render<Kuyumcu.Web.Pages.Uyum>();
        c.WaitForAssertion(() => Assert.Contains(c.FindAll("button"), b => b.TextContent.Contains("Görüntüle")), Uzun);
        c.FindAll("button").First(b => b.TextContent.Contains("Görüntüle")).Click();
        c.WaitForElement("#uy-beyan-baslik", Uzun);
        Assert.Contains(KesifP6Sozlugu.Tr.KademeAmacBeyani, c.Find("#uy-beyan-baslik").TextContent);
        Assert.DoesNotContain("AMAC_BEYANI", c.Markup);
    }

    [Theory]
    [InlineData("YALNIZ_KIMLIK", "Yalnız kimlik")]
    [InlineData("BEYAN_FORMU", "Nakit işlem beyan formu")]
    [InlineData("BEYAN_FORMU_VE_BELGE", "Beyan formu ve ek belge")]
    [InlineData("YOK", "Beyan gerekmez")]
    [InlineData("YENI_KADEME", "YENI_KADEME")]   // bilinmeyen kod uydurulmaz
    public void Kademe_cevirisi_tek_kaynak(string kod, string beklenen)
        => Assert.Equal(beklenen, KesifP6Sozlugu.Kademe(kod, "tr"));

    // ── #72 alt ──────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("MUDUR", false)]
    [InlineData("MUHASEBE", false)]
    [InlineData("SAHIP", true)]
    public void Uyum_saklama_sekmesi_MUDUR_gorunmez(string rol, bool gorunur)
    {
        Oturum(rol);
        var c = Render<Kuyumcu.Web.Pages.Uyum>();
        c.WaitForElement("#uy-sekme-tespit", Uzun);
        Assert.Equal(gorunur ? 1 : 0, c.FindAll("#uy-sekme-saklama").Count);
        // Saklama önizlemesi (SAHİP ucu) MÜDÜR'de hiç ÇAĞRILMAZ → 403 bandı doğamaz.
        if (!gorunur) Assert.DoesNotContain(Istekler(), i => i.Yol.Contains("saklama"));
    }

    // ═══ TUR 2 — tasarım hakemi bulguları (01.10) ═══════════════════════════════

    // [KRİTİK] Sipariş detayında tarayıcı gri düğmesi ('Düzenle', 'Onaylandı') — #13'ün aynısı.
    [Fact]
    public void Siparis_detay_dugmeleri_btn_sinifi_tek_birincil_eylem_dili()
    {
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Pages.SiparisDetay>(p => p.Add(x => x.Id, "s1"));
        e.WaitForElement("#sipd-gecis-ONAYLANDI", Uzun);

        // Sayfadaki HER düğme ortak düğme ailesinden (class boş/yalnız 'ikincil' 0).
        // PAKET GOC-C: ortak ızgaranın başlık sıralama düğmesi (izgara-sirala) kendi bileşen sınıfını taşır — akordeon gibi muaf.
        var sipd = e.FindAll("button[id^='sipd-']:not(.katlanir-baslik):not(.izgara-sirala)"); // akordeon başlığı kendi bileşen sınıfını taşır
        Assert.NotEmpty(sipd);
        Assert.All(sipd, b => Assert.Contains("btn", (b.GetAttribute("class") ?? "").Split(' ')));

        // Düzenle: başlık satırında, ikincil; tek düğmelik kart yok.
        var duzenle = e.Find("#sipd-kart > .kart-baslik #sipd-duzenle");
        Assert.Contains("btn-ikincil", duzenle.GetAttribute("class"));
        Assert.Empty(e.FindAll("#sipd-duzenleme"));

        // Durum geçişi: birincil, EYLEM dilinde ("Onayla"), ekranda TEK birincil.
        var gecis = e.Find("#sipd-gecis-ONAYLANDI");
        Assert.Contains("btn-birincil", gecis.GetAttribute("class"));
        Assert.Equal(KesifP6Sozlugu.Tr.GecisOnayla, gecis.TextContent.Trim());
        Assert.Single(e.FindAll(".btn-birincil"));
        Assert.Contains("btn-ikincil", e.Find("#sipd-gecis-IPTAL").GetAttribute("class"));

        // Düzenleme açılınca kart döner; Kaydet birincil, Vazgeç ikincil (yine btn ailesi).
        duzenle.Click();
        Assert.Contains("btn-birincil", e.Find("#sipd-d-kaydet").GetAttribute("class"));
        Assert.Contains("btn-ikincil", e.Find("#sipd-d-vazgec").GetAttribute("class"));
    }

    // [YÜKSEK] K17 açıklama satırları ve K15 geçmiş açık — detay.
    [Fact]
    public void Siparis_detay_aciklama_satiri_yok_gecmis_kapali()
    {
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Pages.SiparisDetay>(p => p.Add(x => x.Id, "s1"));
        e.WaitForElement("#sipd-kart", Uzun);
        Assert.Empty(e.FindAll("#sipd-defter-notu"));
        Assert.Empty(e.FindAll("#sipd-appendonly"));
        Assert.DoesNotContain("uçlar", e.Markup);
        Assert.DoesNotContain(Pro().Siparis.DetayEkranAciklama, e.Markup);
        Assert.Equal("false", e.Find("#sipd-gecmis-ac").GetAttribute("aria-expanded"));
        Assert.Empty(e.FindAll("#sipd-gecisler"));
    }

    // [YÜKSEK] 'Fiyatı hesapla' serbest tanımla asla açılmıyor, sebep metni yanlış yol gösteriyordu.
    [Fact]
    public void Siparis_serbest_tanimli_kalemde_teklif_sebebi_urun_ister()
    {
        var e = SiparisFormu();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Yol.Contains("/kur/guncel")), Uzun);
        e.Find("#sip-f-kalem-tanim-0").Input("22 ayar burgu bilezik el işçiliği");   // PAKET GOC-C: ortak Metin
        e.Find("#sip-f-detay-ac").Click();
        e.WaitForAssertion(() => Assert.Equal(KesifP6Sozlugu.Tr.TeklifUrunGerekli, e.Find("#sip-f-teklif-sebep").TextContent.Trim()), Uzun);
        Assert.True(e.Find("#sip-f-teklif").HasAttribute("disabled"));
        Assert.DoesNotContain("serbest tanım", e.Find("#sip-f-teklif-sebep").TextContent);
        // Ürün seçilince düğme açılır, sebep kaybolur (metin koşulla eşleşir).
        SeciciYardim.Sec(e, "sip-f-kalem-urun-0", "u-1");
        Assert.False(e.Find("#sip-f-teklif").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#sip-f-teklif-sebep"));
    }

    // [YÜKSEK] K16: yeni sipariş açılışta isteğe bağlı alanları göstermez.
    [Fact]
    public void Siparis_istege_bagli_alanlar_acilista_kapali_Daha_fazla_ile_acilir()
    {
        var e = SiparisFormu();
        foreach (var id in new[] { "#sip-f-gun", "#sip-f-aciklama", "#sip-f-tahmini", "#sip-f-teklif" })
            Assert.Empty(e.FindAll(id));
        // TOPLU-6 (TOPLU-4 hakemi ORTA): Hurda'daki "+ … ekle" bağlantı kalıbı.
        Assert.Equal(Toplu6Sozlugu.Tr.SiparisDetayEkle, e.Find("#sip-f-detay-ac").TextContent.Trim());
        Assert.Empty(e.FindAll("#sip-defter-notu"));
        e.Find("#sip-f-detay-ac").Click();
        foreach (var id in new[] { "#sip-f-gun", "#sip-f-aciklama", "#sip-f-tahmini", "#sip-f-teklif" })
            Assert.Single(e.FindAll(id));
        Assert.Empty(e.FindAll("#sip-f-detay-ac"));
    }

    // [YÜKSEK] Uyum Görüntüle çekmecesi: biçimli tanım listesi + uyruk ülke adı.
    [Fact]
    public void Uyum_tanim_listesi_bicimli_ve_uyruk_ulke_adi()
    {
        Oturum("SAHIP");
        var c = Render<Kuyumcu.Web.Pages.Uyum>();
        c.WaitForAssertion(() => Assert.Contains(c.FindAll("button"), b => b.TextContent.Contains("Görüntüle")), Uzun);
        c.FindAll("button").First(b => b.TextContent.Contains("Görüntüle")).Click();
        c.WaitForElement("#uy-tespit-uyruk", Uzun);
        Assert.Equal("Türkiye", c.Find("#uy-tespit-uyruk").TextContent.Trim());
        Assert.DoesNotContain(">TUR<", c.Markup);
        Assert.All(c.FindAll("dl.tanim-listesi"), d => Assert.Contains("uy-tanim", d.GetAttribute("class")));
        var css = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "zo-islem.css");
        Assert.Matches(@"\.uy-tanim\s*\{[^}]*display:\s*grid", css);
        Assert.Matches(@"\.uy-tanim > dt\s*\{[^}]*var\(--metin-soluk\)", css);
    }

    [Theory]
    [InlineData("TUR", "tr", "Türkiye")]
    [InlineData("tur", "tr", "Türkiye")]
    [InlineData("XQZ", "tr", "XQZ")]   // listede olmayan kod uydurulmaz
    [InlineData("", "tr", "")]
    public void Uyruk_cevirisi_tek_kaynak(string kod, string dil, string beklenen) =>
        Assert.Equal(beklenen, KesifP6Sozlugu.Uyruk(kod, dil));

    [Theory]
    [InlineData("ONAYLANDI", "Onayla")]
    [InlineData("ATOLYEDE", "Atölyeye gönder")]
    [InlineData("HAZIR", "Hazır olarak işaretle")]
    [InlineData("TESLIM_EDILDI", "Teslim et")]
    [InlineData("TASLAK", "Taslağa al")]
    public void Siparis_gecis_eylem_dili(string hedef, string beklenen) =>
        Assert.Equal(beklenen, KesifP6Sozlugu.GecisEylemi(hedef, "tr"));

    // [YÜKSEK] Kullanıcılar: satır eylemleri "⋯" menüsünde, satırda tek "Düzenle", dolu kırmızı 0.
    [Fact]
    public void Kullanicilar_satir_eylemleri_menude_satirda_tek_duzenle()
    {
        Oturum("SAHIP");
        var e = Render<Kuyumcu.Web.Pages.Kullanicilar>();
        e.WaitForElement("#k-eylem-u2", Uzun);
        // Satırda görünen: Düzenle + "⋯" (en çok 2 düğme), kırmızı (tehlike) düğme satırda 0.
        var satir = e.Find("#k-eylem-u2");
        var gorunen = satir.QuerySelectorAll("button");
        Assert.Equal(2, gorunen.Length);
        Assert.Equal("k-duzenle-u2", gorunen[0].Id);
        Assert.Equal("k-menu-ac-u2", gorunen[1].Id);
        Assert.Empty(e.FindAll(".satir-eylem .btn-tehlike"));
        // Özellik silinmedi: şifre, erişimi kapat, sahiplik devri "⋯" penceresinde.
        var menu = e.Find("#k-menu-u2");
        Assert.NotNull(menu.QuerySelector("#k-sifre-u2"));
        Assert.NotNull(menu.QuerySelector("#k-pasif-u2"));
        Assert.NotNull(menu.QuerySelector("#k-devret-u2"));
        // Pasif kullanıcıda geri dönüş yolu (aktife al) yine menüde.
        Assert.NotNull(e.Find("#k-menu-u3").QuerySelector("#k-aktiflestir-u3"));
        // Erişimi kapat gerçekten onay kipini açar (tıklanınca etkisiz düğme yok).
        e.Find("#k-pasif-u2").Click();
        e.WaitForAssertion(() => Assert.Contains("Ali Tezgâhtar", e.Markup), Uzun);
        // Dar ekranda kart yığını (q10-yigin, ayrıca zo-islem.css'te 64rem altı) + uzun e-posta kesilir.
        Assert.Contains("q10-yigin", e.Find("#k-tablo").GetAttribute("class"));
        Assert.Equal("ali.tezgahtar.uzun.eposta.adresi@kapalicarsi-kuyumculari.com.tr",
            e.FindAll("#k-tablo .k-satir-eposta")[1].GetAttribute("title"));
        // Ayrı e-posta sütunu yok (adın altında ikinci satır) — 1440'ta tablo kaba sığar.
        Assert.DoesNotContain(e.FindAll("#k-tablo th"), th => th.TextContent.Contains("E-posta"));
    }

    private static Sozluk Pro() => Sozlukler.Getir("tr");
}
