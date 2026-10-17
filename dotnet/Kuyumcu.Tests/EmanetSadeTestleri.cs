using System.Net;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET EMANET-1 — EMANET EKRANI "ZORUNLU ÖNCE" (dalga T8)
//
// Kurucu 25.09: "zorunlu olanlar hariç ilk etapta kullanıcı görmesin; isterse
// ekler ama eklemese bir kayıp yoksa açılır kapanır bir seçenek olarak kalsın".
// Sınanan sözleşme:
//   E1  açılışta yalnız zorunlu: tek seçici, iki eylem, sebep tek kısa cümle
//   E2  geçmiş "Eski kayıtlar" katlanırı KAPALI gelir, tıklayınca açılır
//   E3  emaneti olmayan müşteri: "Emaneti yok" (0,0000 has YOK), iade kilitli
//   E4  emaneti olan müşteri: has 4 hane, iade açık
//   E5  büyük harfle vurgu yok (ekran + sözlük)
//   E6  iki büyük EŞİT eylem, tek birincil
//   E7  ayrı sunucu arama kutusu yalnız liste sınırdayken
//   E8  mülkiyet bilgisi (i) ipucunda, kapalı
//   E9  giriş çekmecesinde isteğe bağlı alanlar katlanırda; hata olunca açık
//   E10 geçmiş yükleme hatası katlanır KAPALIYKEN de görünür (UX-S28)
//   E18 geri alınmış kayıtta jargon (storno) ve büyük harf yok; kilit sebebi satıra özgü
//   E19 iade alt sınırı (0,001) istemcide: istek yok, alan altı mesaj
//   E20 liste sınırı metni sade
//   E21 ilk yükleme hatasında "Emaneti yok" denmez (UX-S28)
//   E22 yükleme sürerken "Yükleniyor…"; yanıt gelince gerçek değer
//   E23 iade: belirsiz "1.500" istek ATMAZ, varsayılan (tümü) sıfırlanır, sonra 1,5 gider
//   E24 giriş: önce geçerli sonra geçersiz gram → istek yok, alan altı mesaj
//   E25 iade: yardımcı gram kutusu çözülemezse de istek yok
//   E26 eski kayıtlarda maden adı: karma geçmişte her satırda, yalnız altında hiç
//   E27 maden notu yalnız birden çok dolu madende (tek madende yok)
// ═════════════════════════════════════════════════════════════════════════════
public sealed class EmanetSadeTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private int cariSayisi = 2;
    private bool emanetHatali;
    private bool karmaMaden;
    private bool telefonlu;      // E30: c1 telefonlu (seçicide telefonla arama)
    private bool bozukJson;      // E34: /emanet/c1 bozuk JSON döner
    // TEMA TEMİZLİĞİ 17.10: Yıldız listesi açılışta müşterilerin /emanet/{id} uçlarını okur; c9 yalnız
    // liste kurulduktan SONRA bekletilir (E22) — yoksa liste hiç kurulmaz ve özgün gövdeye geçilemez.
    private bool c9Beklet;
    private readonly TaskCompletionSource c9Bekle = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static readonly EmanetSadeMetinleri S = EmanetSadeSozlugu.Tr;

    private sealed class SahteApi(EmanetSadeTestleri s) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (s.istekler) s.istekler.Add((istek.Method.Method, yol, govde));

            if (yol.Contains("/emanet/c1", StringComparison.Ordinal) && s.emanetHatali)
                return Yanit(HttpStatusCode.ServiceUnavailable, """{"message":"emanet servisi yok"}""");
            // c9: yanıt test bırakana dek bekler (yükleme durumu).
            if (s.c9Beklet && yol.Contains("/emanet/c9", StringComparison.Ordinal))
                await s.c9Bekle.Task;
            // E26: c1'in geçmişi altın + gümüş karışık.
            if (s.bozukJson && yol.EndsWith("/emanet/c1", StringComparison.Ordinal))
                return Yanit(HttpStatusCode.OK, """{"cariId":"c1","mevcutHasGramBakiye":""");
            if (s.karmaMaden && yol.EndsWith("/emanet/c1", StringComparison.Ordinal))
                return Yanit(HttpStatusCode.OK, """
                    {"cariId":"c1","mevcutHasGramBakiye":11.45,
                     "madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":11.45},{"maden":"GUMUS","madenAdi":"Gümüş","gram":1387.5}],
                     "hareketler":[
                       {"id":"k1","tur":"EMANET_GIRIS","tarih":"2026-09-10T10:00:00Z","hasGrami":1387.5,
                        "aciklama":"Gümüş tepsi","stornoKaynakId":null,"fisId":null,"maden":"GUMUS"},
                       {"id":"k2","tur":"EMANET_GIRIS","tarih":"2026-09-11T10:00:00Z","hasGrami":11.45,
                        "aciklama":"Bilezik","stornoKaynakId":null,"fisId":null,"maden":"ALTIN"}]}
                    """);

            var g = yol switch
            {
                var y when y.Contains("/kur/guncel") => """{"id":"kur-9","altinHasFiyati":6800}""",
                var y when y.Contains("/emanet/giris") => """{"id":"h9"}""",
                var y when y.Contains("/emanet/c1") => """
                    {"cariId":"c1","mevcutHasGramBakiye":12.5,
                     "madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":12.5}],
                     "hareketler":[
                       {"id":"h1","tur":"EMANET_GIRIS","tarih":"2026-09-10T10:00:00Z","hasGrami":12.5,
                        "aciklama":"Bilezik 2 adet","stornoKaynakId":null,"fisId":null,"maden":"ALTIN"},
                       {"id":"h2","tur":"EMANET_GIRIS","tarih":"2026-09-11T10:00:00Z","hasGrami":0,
                        "aciklama":null,"stornoKaynakId":null,"fisId":null,"maden":"ALTIN"}]}
                    """,
                // c3: YALNIZ GÜMÜŞ emanet — sunucu mevcutHasGramBakiye'yi yalnız altın verir (0).
                var y when y.Contains("/emanet/c3") => """
                    {"cariId":"c3","mevcutHasGramBakiye":0,
                     "madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":0},{"maden":"GUMUS","madenAdi":"Gümüş","gram":92.5}],
                     "hareketler":[]}
                    """,
                // c4: 6 hane saklanan, 4 haneye yuvarlanınca 0 olan artık.
                var y when y.Contains("/emanet/c4") => """
                    {"cariId":"c4","mevcutHasGramBakiye":0.00004,
                     "madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":0.00004}],"hareketler":[]}
                    """,
                // c5: sunucunun iade alt sınırının (0,001) ALTINDA toz artık (doğrulayıcı 27.09 canlı: 0,0005).
                var y when y.Contains("/emanet/c5") => """
                    {"cariId":"c5","mevcutHasGramBakiye":0.0005,
                     "madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":0.0005}],"hareketler":[]}
                    """,
                // c6: tam sınır 0,001 — sunucu kabul eder, iade AÇIK.
                var y when y.Contains("/emanet/c6") => """
                    {"cariId":"c6","mevcutHasGramBakiye":0.001,
                     "madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":0.001}],"hareketler":[]}
                    """,
                // c7: yarım sınır 0,00005 — Bicim.Has yarım UZAĞA yuvarlar ("0,0001 has").
                var y when y.Contains("/emanet/c7") => """
                    {"cariId":"c7","mevcutHasGramBakiye":0.00005,
                     "madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":0.00005}],"hareketler":[]}
                    """,
                // c8 (doğrulayıcı 28.09): geri alınmış kayıt durumu — h81 geri alındı, h82 onun
                // 48-A ters kaydı (sunucu açıklaması "STORNO: gerekçe"), h83 çok satırlı fiş parçası,
                // h84 geri alınabilir sıradan kayıt.
                var y when y.Contains("/emanet/c8") => """
                    {"cariId":"c8","mevcutHasGramBakiye":5,
                     "madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":5}],
                     "hareketler":[
                       {"id":"h81","tur":"EMANET_GIRIS","tarih":"2026-09-20T10:00:00Z","hasGrami":5.5,
                        "aciklama":"Bilezik","stornoKaynakId":null,"fisId":null,"maden":"ALTIN"},
                       {"id":"h82","tur":"EMANET_GIRIS","tarih":"2026-09-20T11:00:00Z","hasGrami":-5.5,
                        "aciklama":"STORNO: Yanlis ayar girildi, dogrusu yeniden girilecek","stornoKaynakId":"h81","fisId":null,"maden":"ALTIN"},
                       {"id":"h83","tur":"EMANET_GIRIS","tarih":"2026-09-21T10:00:00Z","hasGrami":3,
                        "aciklama":"Kolye","stornoKaynakId":null,"fisId":"f1","maden":"ALTIN"},
                       {"id":"h84","tur":"EMANET_GIRIS","tarih":"2026-09-22T10:00:00Z","hasGrami":2,
                        "aciklama":"Yüzük","stornoKaynakId":null,"fisId":null,"maden":"ALTIN"}]}
                    """,
                var y when y.Contains("/emanet/c9") => """
                    {"cariId":"c9","mevcutHasGramBakiye":4,
                     "madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":4}],"hareketler":[]}
                    """,
                var y when y.Contains("/emanet/c2") => """
                    {"cariId":"c2","mevcutHasGramBakiye":0,"madenBakiyeleri":[],"hareketler":[]}
                    """,
                // TEMA TEMİZLİĞİ 17.10: Yıldız'da özgün gövdeye geçiş listede bir emanet satırıdır → kapı müşterisi c0.
                var y when y.Contains("/emanet/c0") => TemaTemizligiGEYardimcisi.KapiEmanet,
                var y when y.Contains("/cari") => "[" + string.Join(",",
                    new[] { TemaTemizligiGEYardimcisi.KapiCari, s.telefonlu ? """{"id":"c1","ad":"Ayşe Demir","telefon":"05441112233"}""" : """{"id":"c1","ad":"Ayşe Demir"}""", """{"id":"c2","ad":"Mehmet Kaya"}""" }
                        .Concat(Enumerable.Range(3, Math.Max(0, s.cariSayisi - 2))
                            .Select(i => $$"""{"id":"c{{i}}","ad":"Müşteri {{i}}"}"""))) + "]",
                _ => """{"hata":"sahte API bu ucu tanımıyor"}""",
            };
            var kod = g.StartsWith("""{"hata""", StringComparison.Ordinal) ? HttpStatusCode.NotFound : HttpStatusCode.OK;
            return Yanit(kod, g);
        }

        private static HttpResponseMessage Yanit(HttpStatusCode kod, string g) => new(kod)
        {
            Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    public EmanetSadeTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    /// <summary>
    /// TEMA TEMİZLİĞİ 17.10: Yıldız yolu — açılış listedir; özgün gövde kapı müşterisinin (c0) satırına
    /// basılınca o müşteri SEÇİLİ açılır (bkz. TemaTemizligiGEYardimcisi).
    /// </summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Emanet> Ciz() =>
        Render<Kuyumcu.Web.Pages.Emanet>().EmanetSatiriAc();

    /// <param name="listeSonrasi">Liste kurulup özgün gövde açıldıktan SONRA, müşteri seçilmeden önce (ör. sunucu hatası başlar).</param>
    private IRenderedComponent<Kuyumcu.Web.Pages.Emanet> Secili(string cari, Action? listeSonrasi = null)
    {
        var e = Ciz();
        // Kur yüklenmeden "+ Emanet al" kilitli kalır — kur gelene dek bekle.
        e.WaitForAssertion(() => Assert.DoesNotContain(OnayKilitSozlugu.Tr.EmanetKurYok, e.Markup, StringComparison.Ordinal));
        listeSonrasi?.Invoke();
        SeciciYardim.Sec(e, "e-cari", cari);
        e.WaitForElement("#e-ozet-deger");
        e.WaitForElement("#e-gecmis");
        return e;
    }

    // TEMA TEMİZLİĞİ 17.10: E1_Acilista_yalniz_zorunlu_ogeler_ve_TEK_kisa_sebep kaldırıldı — Yıldız'da açılış listedir; özgün gövde yalnız bir satırın müşterisi SEÇİLİ açılır (müşterisiz özgün durum ulaşılamaz). "Önce müşteri seçin" kapısı Yıldız "Emanet al" formunda alan altında: Yildiz5AFormTestleri.Y5A_F01; tek birincil + iki eşit eylem E6'da.

    // ── E2 ─────────────────────────────────────────────────────────────────
    [Fact]
    public void E2_Gecmis_Eski_kayitlar_katlaniri_KAPALI_gelir_tiklayinca_acilir()
    {
        var e = Secili("c1");

        var ac = e.Find("#e-gecmis-ac");
        Assert.Equal("false", ac.GetAttribute("aria-expanded"));
        Assert.Contains(S.EskiKayitlar, ac.TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#e-gecmis-govde"));
        Assert.Empty(e.FindAll("#eh-detay-h1"));
        // Kayıt sayısı kapalıyken de söylenir.
        e.WaitForAssertion(() => Assert.Equal("2", e.Find("#e-gecmis-sayi").TextContent.Trim()));

        e.Find("#e-gecmis-ac").Click();

        Assert.Equal("true", e.Find("#e-gecmis-ac").GetAttribute("aria-expanded"));
        Assert.NotNull(e.Find("#eh-detay-h1"));
    }

    // ── E3 ─────────────────────────────────────────────────────────────────
    [Fact]
    public void E3_Emaneti_olmayan_musteride_Emaneti_yok_yazar_sifir_has_YAZMAZ_iade_kilitli()
    {
        var e = Secili("c2");

        Assert.Equal(S.EmanetiYok, e.Find("#e-ozet-deger").TextContent.Trim());
        Assert.DoesNotContain("0,0000", e.Markup, StringComparison.Ordinal);

        var iade = e.Find("#e-cikis-ac");
        Assert.True(iade.HasAttribute("disabled"));
        Assert.Equal(S.IadeYok, iade.GetAttribute("title"));
        Assert.Equal(S.IadeYok, e.Find("#e-kilit").TextContent.Trim());
        Assert.DoesNotContain("Eksik olan", e.Markup, StringComparison.Ordinal);
        // "Emanet al" açık — kilit sebebi yalnız iadeye aittir.
        Assert.False(e.Find("#e-giris-ac").HasAttribute("disabled"));
    }

    // ── E4 ─────────────────────────────────────────────────────────────────
    [Fact]
    public void E4_Emaneti_olan_musteride_has_4_hane_ve_iade_acik()
    {
        var e = Secili("c1");

        e.WaitForAssertion(() => Assert.Equal(Bicim.Has(12.5m), e.Find("#e-ozet-deger").TextContent.Trim()));
        Assert.False(e.Find("#e-cikis-ac").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#e-kilit"));
    }

    // ── E5 ─────────────────────────────────────────────────────────────────
    /// <summary>≥ 3 harfli, tamamı büyük harf bir kelime (Türkçe harfler dahil).</summary>
    private static readonly Regex BuyukKelime = new(@"\b[A-ZÇĞİÖŞÜ]{3,}\b", RegexOptions.CultureInvariant);

    [Fact]
    public void E5_Ekranda_ve_sozlukte_BUYUK_HARF_vurgu_yok()
    {
        var e = Secili("c1");
        e.Find("#e-gecmis-ac").Click();

        // Görünen metin (etiketler, ipucu, düğmeler, özet, tablo) — öznitelikler hariç.
        var metin = e.Find("#emanet-sayfa").TextContent;
        var bulunan = BuyukKelime.Matches(metin).Select(m => m.Value).Distinct().ToList();
        Assert.True(bulunan.Count == 0, "Büyük harf vurgu: " + string.Join(", ", bulunan));

        // Eski büyük harfli metinler ekrandan kalktı.
        Assert.DoesNotContain("SİZİN DEĞİLDİR", e.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("GİRMEZ", e.Markup, StringComparison.Ordinal);

        // İade çekmecesi de büyük harf vurgusuz; başlığı düğmeyle AYNI ad (UX-S31).
        e.Find("#e-cikis-ac").Click();
        var cekmece = e.WaitForElement("#cekmece-emanet-cikis");
        var cm = BuyukKelime.Matches(cekmece.TextContent).Select(m => m.Value).Distinct().ToList();
        Assert.True(cm.Count == 0, "Çekmecede büyük harf: " + string.Join(", ", cm));
        // Doğrulayıcı 28.09 (DÜŞÜK): tek madenli müşteride "toplam has yalnız altındır" notu YOK.
        Assert.DoesNotContain(S.MadenNotu, cekmece.TextContent, StringComparison.Ordinal);
        Assert.Contains(S.EmanetIade, cekmece.TextContent, StringComparison.Ordinal);

        foreach (var m in new[] { EmanetSadeSozlugu.Tr, EmanetSadeSozlugu.En })
            foreach (var p in typeof(EmanetSadeMetinleri).GetProperties().Where(p => p.PropertyType == typeof(string)))
            {
                var deger = (string)p.GetValue(m)!;
                Assert.False(BuyukKelime.IsMatch(deger), $"{p.Name}: {deger}");
            }
    }

    // ── E6 ─────────────────────────────────────────────────────────────────
    [Fact]
    public void E6_Iki_buyuk_ESIT_eylem_tek_birincil()
    {
        var e = Ciz();

        var eylemler = e.FindAll("#e-eylemler > button");
        Assert.Equal(2, eylemler.Count);
        Assert.All(eylemler, b => Assert.True(b.ClassList.Contains("btn-buyuk") && b.ClassList.Contains("emanet-eylem"), b.Id));
        Assert.Single(eylemler, b => b.ClassList.Contains("btn-birincil"));
        Assert.Equal(S.EmanetAl, e.Find("#e-giris-ac").TextContent.Trim());
        Assert.Equal(S.EmanetIade, e.Find("#e-cikis-ac").TextContent.Trim());
    }

    // ── E7 ─────────────────────────────────────────────────────────────────
    [Fact]
    public void E7_Ayri_sunucu_arama_kutusu_YALNIZ_liste_sinirdayken_gorunur()
    {
        cariSayisi = 50;
        var e = Ciz();

        e.WaitForElement("#e-cari-ara");
        Assert.NotNull(e.Find("#e-cari-sinir"));
        // Sunucu araması hâlâ /cari?aktif=true&q=...&limit=50 (sözleşme değişmedi).
        lock (istekler) istekler.Clear();
        e.Find("#e-cari-ara").Input("Mehmet");
        e.WaitForAssertion(() =>
        {
            lock (istekler)
                Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("/cari?aktif=true&q=Mehmet&limit=50"));
        });
    }

    // ── E8 ─────────────────────────────────────────────────────────────────
    [Fact]
    public void E8_Mulkiyet_bilgisi_i_ipucunda_ve_KAPALI()
    {
        var e = Ciz();

        var bilgi = e.Find("#e-bilgi");
        Assert.Equal("DETAILS", bilgi.TagName, ignoreCase: true);
        Assert.False(bilgi.HasAttribute("open"));
        Assert.Equal(S.Bilgi, e.Find("#e-bilgi-metin").TextContent.Trim());
        Assert.Equal(S.Aciklama, e.Find("#sayfa-basi-emanet-aciklama").TextContent.Trim());
    }

    // ── E9 ─────────────────────────────────────────────────────────────────
    [Fact]
    public void E9_Giris_cekmecesinde_istege_bagli_alanlar_katlanirda_hata_olunca_ACIK()
    {
        var e = Secili("c1");
        e.Find("#e-giris-ac").Click();

        var detay = e.WaitForElement("#e-giris-detay");
        Assert.False(detay.HasAttribute("open"));
        Assert.Equal(S.GirisDetayEkle, e.Find("#e-giris-detay-ac").TextContent.Trim());
        // Zorunlu alanlar katlanırın DIŞINDA.
        Assert.Empty(e.FindAll("#e-giris-detay #e-gram"));
        Assert.NotNull(e.Find("#e-giris-detay #e-tas"));
        Assert.NotNull(e.Find("#e-giris-detay #e-aciklama"));

        // Taş ≥ gram → alan hatası; hatalı alan KATLANIRDA SAKLI KALMAZ.
        e.Find("#e-gram").Input("5");
        e.Find("#e-tas").Input("6");
        e.Find("#e-giris-kaydet").Click();
        Assert.True(e.Find("#e-giris-detay").HasAttribute("open"));
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");

        // İade çekmecesinde not da katlanırda, kapalı.
        var e2 = Secili("c1");
        e2.Find("#e-cikis-ac").Click();
        Assert.False(e2.WaitForElement("#e-cikis-detay").HasAttribute("open"));
        Assert.NotNull(e2.Find("#e-cikis-detay #c-aciklama"));
        Assert.Empty(e2.FindAll("#e-cikis-detay #c-has"));
    }

    // ── E10 ────────────────────────────────────────────────────────────────
    [Fact]
    public void E10_Gecmis_yukleme_hatasi_katlanir_KAPALIYKEN_de_gorunur()
    {
        // TEMA TEMİZLİĞİ 17.10: hata, liste kurulduktan sonra başlar (liste hatası Yıldız listesinin kendi bandında).
        var e = Ciz();
        emanetHatali = true;
        SeciciYardim.Sec(e, "e-cari", "c1");

        e.WaitForElement("#e-liste-hata");
        Assert.Equal("false", e.Find("#e-gecmis-ac").GetAttribute("aria-expanded"));
        // Sayı bilinmiyor → rozet yok ("0" yazılmaz, UX-S28).
        Assert.Empty(e.FindAll("#e-gecmis-sayi"));
        // Hata "kayıt yok"a çevrilmez: açıldığında boş durum BASILMAZ.
        e.Find("#e-gecmis-ac").Click();
        Assert.DoesNotContain(Dil().T.Emanet.HareketYok, e.Markup, StringComparison.Ordinal);
    }

    // ── E11 ────────────────────────────────────────────────────────────────
    // Doğrulayıcı bulgusu (25.09, KRİTİK): yalnız gümüş emaneti olan müşteride
    // özet maden adsız "92,5000 has" yazıyor, aynı anda "emaneti yok" diyip
    // iadeyi kilitliyordu. Özet madeni söyler; iade kilidi MADEN BAZLI.
    [Fact]
    public void E11_Yalniz_gumus_emanette_ozet_madeni_soyler_iade_ACIK_ve_gumusle_acilir()
    {
        cariSayisi = 4;
        var e = Secili("c3");

        e.WaitForAssertion(() => Assert.Equal($"Gümüş {Bicim.Has(92.5m)}", e.Find("#e-ozet-deger").TextContent.Trim()));
        Assert.False(e.Find("#e-cikis-ac").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#e-kilit"));
        Assert.DoesNotContain(S.IadeYok, e.Markup, StringComparison.Ordinal);

        // İade çekmecesi GÜMÜŞÜN netiyle açılır (altının 0'ı ile değil).
        e.Find("#e-cikis-ac").Click();
        // data-deger makine değeridir (Bicim.Girdi) — beklenen aynı biçimciden üretilir.
        var has = e.Find("#cekmece-emanet-cikis #c-has");
        Assert.Equal(Bicim.Girdi(92.5m), has.GetAttribute("data-deger"));
        // Sıfır bakiyeli altın satırı çekmecede "0,0000 has" olarak basılmaz.
        Assert.DoesNotContain("0,0000", e.Find("#cekmece-emanet-cikis").TextContent, StringComparison.Ordinal);
    }

    // ── E12 ────────────────────────────────────────────────────────────────
    // Doğrulayıcı bulgusu (25.09, KRİTİK): 0,00004 has "0,0000 has" basılıyordu.
    [Fact]
    public void E12_Dort_haneye_sifira_yuvarlanan_artik_Emaneti_yok_ve_iade_kilitli()
    {
        cariSayisi = 4;
        var e = Secili("c4");

        e.WaitForAssertion(() => Assert.Equal(S.EmanetiYok, e.Find("#e-ozet-deger").TextContent.Trim()));
        Assert.DoesNotContain("0,0000", e.Markup, StringComparison.Ordinal);
        Assert.True(e.Find("#e-cikis-ac").HasAttribute("disabled"));
        Assert.Equal(S.IadeYok, e.Find("#e-kilit").TextContent.Trim());
    }

    // ═══ 27.09 DEVRALMA — doğrulayıcı KALDI bulguları ═══════════════════════

    // ── E13 ────────────────────────────────────────────────────────────────
    // [ORTA] Sunucunun reddettiği (HasGrami < 0,001) toz artıkta iade AÇIK kalıyordu.
    [Fact]
    public void E13_Iade_alt_sinirinin_altindaki_artikta_iade_kilitli_ve_tek_kisa_sebep()
    {
        cariSayisi = 7;
        var e = Secili("c5");

        e.WaitForAssertion(() => Assert.Equal(Bicim.Has(0.0005m), e.Find("#e-ozet-deger").TextContent.Trim()));
        Assert.True(e.Find("#e-cikis-ac").HasAttribute("disabled"));
        Assert.Equal(S.IadeCokKucuk, e.Find("#e-kilit").TextContent.Trim());
        Assert.Equal("Kalan miktar iade için çok küçük.", EmanetSadeSozlugu.Tr.IadeCokKucuk);
    }

    // ── E14 ────────────────────────────────────────────────────────────────
    [Fact]
    public void E14_Tam_sinir_0_001_iade_ACIK_kilit_metni_yok()
    {
        cariSayisi = 7;
        var e = Secili("c6");

        e.WaitForAssertion(() => Assert.Equal(Bicim.Has(0.001m), e.Find("#e-ozet-deger").TextContent.Trim()));
        Assert.False(e.Find("#e-cikis-ac").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#e-kilit"));
    }

    // ── E15 ────────────────────────────────────────────────────────────────
    // [DÜŞÜK] SifirMi ToEven iken 0,00005 "Emaneti yok" diyordu; Bicim.Has aynı
    // değeri "0,0001 has" yazar. Tek kural: yarım UZAĞA.
    [Fact]
    public void E15_Yarim_sinir_0_00005_Bicim_ile_ayni_kural_ozet_0_0001_iade_kilitli()
    {
        cariSayisi = 7;
        var e = Secili("c7");

        e.WaitForAssertion(() => Assert.Equal(Bicim.Has(0.00005m), e.Find("#e-ozet-deger").TextContent.Trim()));
        Assert.NotEqual(S.EmanetiYok, e.Find("#e-ozet-deger").TextContent.Trim());
        Assert.True(e.Find("#e-cikis-ac").HasAttribute("disabled"));
        Assert.Equal(S.IadeCokKucuk, e.Find("#e-kilit").TextContent.Trim());
    }

    // ── E16 ────────────────────────────────────────────────────────────────
    // [KRİTİK, tasarım hakemi] Eski kayıtlar: hücrede yalnız sayı (birim başlıkta),
    // satır düğmesi kısa "Geri al", tarih kırılmaz sınıfta, eylemler yan yana kabı.
    [Fact]
    public void E16_Eski_kayitlar_hucrede_yalniz_sayi_kisa_Geri_al_tarih_kirilmaz()
    {
        var e = Secili("c1");
        e.Find("#e-gecmis-ac").Click();

        var satir = e.Find("#eh-detay-h1").Closest("tr")!;
        var hucreler = satir.QuerySelectorAll("td");
        Assert.Contains("emanet-tarih", hucreler[0].ClassName ?? "", StringComparison.Ordinal);
        var hasHucre = satir.QuerySelector("td.sayi")!.TextContent.Trim();
        Assert.Equal(Bicim.Has(12.5m).Replace(" has", "", StringComparison.Ordinal), hasHucre);
        Assert.DoesNotContain("has", hasHucre, StringComparison.Ordinal);
        Assert.Equal(S.SatirGeriAl, e.Find("#eh-storno-h1").TextContent.Trim());
        Assert.NotNull(satir.QuerySelector(".emanet-satir-eylem #eh-storno-h1"));
    }

    // ── E17 ────────────────────────────────────────────────────────────────
    // [DÜŞÜK] İade çekmecesinde isteğe bağlı ayar+gram yardımcısı açıktaydı.
    [Fact]
    public void E17_Iade_cekmecesinde_ayar_gram_yardimcisi_katlanirda_KAPALI_gram_girilince_ACIK()
    {
        var e = Secili("c1");
        e.Find("#e-cikis-ac").Click();

        var hesap = e.Find("#cekmece-emanet-cikis details#e-cikis-hesap");
        Assert.False(hesap.HasAttribute("open"));
        Assert.Equal(S.CikisHesapEkle, e.Find("#e-cikis-hesap-ac").TextContent.Trim());
        Assert.NotNull(hesap.QuerySelector("#c-ayar"));
        Assert.NotNull(hesap.QuerySelector("#c-gram"));
        // Zorunlu alan (iade edilecek has) katlanırın DIŞINDA.
        Assert.Null(hesap.QuerySelector("#c-has"));
        Assert.NotNull(e.Find("#cekmece-emanet-cikis #c-has"));

        e.Find("#c-gram").Input("10");
        e.WaitForAssertion(() => Assert.True(e.Find("details#e-cikis-hesap").HasAttribute("open")));
    }

    // ═══ 28.09 DEVRALMA — doğrulayıcı KALDI bulguları (geri alınmış kayıt) ═══

    /// <summary>Jargon: "storno", "stornolandı", "stornolanmış"… (büyük/küçük harf fark etmez).</summary>
    private static readonly Regex StornoJargon = new(@"storno\p{L}*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // ── E18 ────────────────────────────────────────────────────────────────
    // [KRİTİK] Geri alınmış kaydı olan müşteride tablo "STORNO: …", "Stornolandı" ve hub
    // HareketDegismez metinlerini basıyordu (BASİT K8 = 2). Ekran, satır title'ları,
    // Ayrıntı çekmecesi ve geri alma onayı jargon ve büyük harf vurgu taşımaz.
    [Fact]
    public void E18_Geri_alinmis_kayitta_jargon_ve_buyuk_harf_yok_sebep_satira_ozgu()
    {
        cariSayisi = 8;
        var e = Secili("c8");
        e.Find("#e-gecmis-ac").Click();
        e.WaitForElement("#eh-storno-h84");

        var metin = e.Find("#emanet-sayfa").TextContent;
        var buyuk = BuyukKelime.Matches(metin).Select(m => m.Value).Distinct().ToList();
        Assert.True(buyuk.Count == 0, "Büyük harf vurgu: " + string.Join(", ", buyuk));
        var jargon = StornoJargon.Matches(metin).Select(m => m.Value).Distinct().ToList();
        Assert.True(jargon.Count == 0, "Jargon: " + string.Join(", ", jargon));

        // Sunum çevirisi: ters kaydın açıklaması "Geri alma: gerekçe".
        Assert.Contains(string.Format(S.GeriAlmaAciklama, "Yanlis ayar girildi, dogrusu yeniden girilecek"), metin, StringComparison.Ordinal);
        Assert.Contains(S.GeriAlindi, metin, StringComparison.Ordinal);
        // Tasarım hakemi 28.09 (KRİTİK): tablonun altında kalıcı "değiştirilemez" kutusu YOK.
        Assert.DoesNotContain(S.HareketDegismezYolu, metin, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#emanet-sayfa .degismez-ipucu"));

        // UX-S30: kilitli satır düğmesi o satıra özgü TEK sebebi söyler.
        Assert.Equal(S.NedenGeriAlindi, e.Find("#eh-storno-h81").GetAttribute("title"));
        Assert.Equal(S.NedenGeriAlmaKaydi, e.Find("#eh-storno-h82").GetAttribute("title"));
        Assert.Equal(S.NedenFis, e.Find("#eh-storno-h83").GetAttribute("title"));
        Assert.True(e.Find("#eh-storno-h82").HasAttribute("disabled"));
        Assert.False(e.Find("#eh-storno-h84").HasAttribute("disabled"));
        Assert.Null(e.Find("#eh-storno-h84").GetAttribute("title"));
        foreach (var d in e.FindAll("button[title]"))
            Assert.DoesNotMatch(StornoJargon, d.GetAttribute("title")!);

        // Ayrıntı çekmecesi (ters kayıt).
        e.Find("#eh-detay-h82").Click();
        var cekmece = e.WaitForElement("#cekmece-emanet-hareket");
        Assert.DoesNotMatch(StornoJargon, cekmece.TextContent);
        Assert.False(BuyukKelime.IsMatch(cekmece.TextContent), cekmece.TextContent);
        Assert.Equal(string.Format(S.GeriAlmaAciklama, "Yanlis ayar girildi, dogrusu yeniden girilecek"),
            e.Find("#eh-detay-aciklama").TextContent.Trim());
        Assert.Contains(S.NedenGeriAlmaKaydi, e.Find("#eh-detay-geri-al-kilit-h82").TextContent, StringComparison.Ordinal);

        // Geri alma onayı (ortak onay modalı) sade metinle açılır.
        e.Find("#eh-storno-h84").Click();
        var modal = e.WaitForElement(".modal-panel");
        Assert.DoesNotMatch(StornoJargon, modal.TextContent);
        Assert.Contains(S.GeriAlBaslik, modal.TextContent, StringComparison.Ordinal);
        Assert.Contains(S.GeriAlOnay, modal.TextContent, StringComparison.Ordinal);

        // Sözlük: Türkçe metinlerin hiçbiri "storno" taşımaz.
        foreach (var p in typeof(EmanetSadeMetinleri).GetProperties().Where(p => p.PropertyType == typeof(string)))
            Assert.DoesNotMatch(StornoJargon, (string)p.GetValue(EmanetSadeSozlugu.Tr)!);
    }

    // ── E19 ────────────────────────────────────────────────────────────────
    // [DÜŞÜK] İadede 0 < has < 0,001 istemcide engellenmiyordu (sunucu 400).
    [Fact]
    public void E19_Iade_alt_sinirin_altinda_istek_atmaz_alan_alti_mesaj()
    {
        var e = Secili("c1");
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-has").Input("0,0005");
        e.Find("#e-cikis-kaydet").Click();

        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("/cikis", StringComparison.Ordinal));
        Assert.Contains(string.Format(S.IadeEnAz, Bicim.Has(0.001m)),
            e.Find("#cekmece-emanet-cikis").TextContent, StringComparison.Ordinal);

        // Tam sınır 0,001 gönderilir.
        e.Find("#c-has").Input("0,001");
        e.Find("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            lock (istekler) Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("/emanet/c1/cikis", StringComparison.Ordinal));
        });
    }

    // ── E20 ────────────────────────────────────────────────────────────────
    // [DÜŞÜK] 50+ müşteride liste sınırı metni teknik dil taşıyordu.
    [Fact]
    public void E20_Liste_siniri_metni_sade()
    {
        cariSayisi = 50;
        var e = Ciz();
        var p = e.WaitForElement("#e-cari-sinir").TextContent.Trim();
        Assert.Equal(S.AramaSinir, p);
        Assert.DoesNotContain("sunucu", p, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("50", p, StringComparison.Ordinal);
    }

    // ── E21 ────────────────────────────────────────────────────────────────
    // 28.09 canlı ölçüm (UX-S28): ilk yükleme HATA verince özet "Emaneti yok",
    // kilit "Bu müşterinin emaneti yok." diyordu — bilinmeyen değer boş sayılamaz.
    [Fact]
    public void E21_Yukleme_hatasinda_Emaneti_yok_DENMEZ_kilit_sebebi_hata()
    {
        var e = Secili("c1", () => emanetHatali = true);

        e.WaitForElement("#e-liste-hata");
        var ozet = e.Find("#e-ozet-deger").TextContent.Trim();
        Assert.NotEqual(S.EmanetiYok, ozet);
        Assert.Equal("—", ozet);
        Assert.Equal(S.BilgiAlinamadi, e.Find("#e-kilit").TextContent.Trim());
        Assert.DoesNotContain(S.IadeYok, e.Find("#emanet-sayfa").TextContent, StringComparison.Ordinal);
        Assert.True(e.Find("#e-cikis-ac").HasAttribute("disabled"));
    }

    // ── E22 ────────────────────────────────────────────────────────────────
    // Yükleme SÜRERKEN de "Emaneti yok" denmez; yanıt gelince gerçek değer yazılır.
    [Fact]
    public void E22_Yukleme_surerken_Yukleniyor_yazar_yanit_gelince_deger()
    {
        cariSayisi = 9;
        var e = Secili("c9", () => c9Beklet = true);

        Assert.Equal(S.Yukleniyor, e.Find("#e-ozet-deger").TextContent.Trim());
        Assert.Equal(S.Yukleniyor, e.Find("#e-kilit").TextContent.Trim());
        Assert.True(e.Find("#e-cikis-ac").HasAttribute("disabled"));

        c9Bekle.SetResult();
        e.WaitForAssertion(() => Assert.Equal(Bicim.Has(4m), e.Find("#e-ozet-deger").TextContent.Trim()));
        Assert.Empty(e.FindAll("#e-kilit"));
        Assert.False(e.Find("#e-cikis-ac").HasAttribute("disabled"));
    }

    // ── E23 ────────────────────────────────────────────────────────────────
    // Doğrulayıcı 28.09 (t30, KRİTİK): "1.500" kutuyu kırmızı yapıyordu ama Kaydet
    // "tümünü iade et" varsayılanını (12,5) POST ediyordu.
    [Fact]
    public void E23_Iade_belirsiz_giris_istek_atmaz_varsayilan_sifirlanir_sonra_gecerli_gider()
    {
        var e = Secili("c1");
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-has").Input("1.500");
        e.Find("#e-cikis-kaydet").Click();

        bool CikisPostu((string Metot, string Yol, string Govde) i) =>
            i.Metot == "POST" && i.Yol.Contains("/cikis", StringComparison.Ordinal);
        lock (istekler) Assert.DoesNotContain(istekler, CikisPostu);
        Assert.Contains(SayiGirisiSozlugu.Tr.Belirsiz, e.Find("#cekmece-emanet-cikis").TextContent, StringComparison.Ordinal);

        // Yeniden çizimde varsayılan (12,5) geri GELMEZ: ikinci Kaydet de istek atmaz.
        e.Find("#e-cikis-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, CikisPostu);
        Assert.Equal("true", e.Find("#c-has").GetAttribute("aria-invalid"));

        e.Find("#c-has").Input("1,5");
        e.Find("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            lock (istekler)
            {
                var post = Assert.Single(istekler, CikisPostu);
                using var j = System.Text.Json.JsonDocument.Parse(post.Govde);
                Assert.Equal(1.5m, j.RootElement.GetProperty("hasGrami").GetDecimal());
            }
        });
    }

    // ── E24 ────────────────────────────────────────────────────────────────
    // Giriş çekmecesi: önce geçerli (10) sonra çözülemeyen ("abc") gram yazılırsa
    // eski 10 gönderilmez.
    [Fact]
    public void E24_Giris_once_gecerli_sonra_gecersiz_gram_istek_atmaz_alan_alti_mesaj()
    {
        var e = Secili("c1");
        e.Find("#e-giris-ac").Click();
        var gram = e.WaitForElement("#e-gram");
        gram.Input("10");
        e.Find("#e-gram").Input("abc");
        e.Find("#e-giris-kaydet").Click();

        bool GirisPostu((string Metot, string Yol, string Govde) i) =>
            i.Metot == "POST" && i.Yol.Contains("/emanet/giris", StringComparison.Ordinal);
        lock (istekler) Assert.DoesNotContain(istekler, GirisPostu);
        Assert.Contains(Dil().T.Form.SayiAnlasilmadi, e.Find("#cekmece-emanet-giris").TextContent, StringComparison.Ordinal);

        e.Find("#e-gram").Input("10");
        e.Find("#e-giris-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            lock (istekler)
            {
                var post = Assert.Single(istekler, GirisPostu);
                using var j = System.Text.Json.JsonDocument.Parse(post.Govde);
                Assert.Equal(10m, j.RootElement.GetProperty("gramAgirlik").GetDecimal());
            }
        });
    }

    // ── E25 ────────────────────────────────────────────────────────────────
    [Fact]
    public void E25_Iade_yardimci_gram_cozulemezse_istek_atmaz()
    {
        var e = Secili("c1");
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-gram").Input("2.500");
        e.Find("#e-cikis-kaydet").Click();

        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.Contains(SayiGirisiSozlugu.Tr.Belirsiz, e.Find("#cekmece-emanet-cikis").TextContent, StringComparison.Ordinal);
    }

    // ── E26 ────────────────────────────────────────────────────────────────
    // Doğrulayıcı 28.09 (DÜŞÜK): gümüş ve altın "Has gram" sütununda etiketsiz yan yanaydı.
    [Fact]
    public void E26_Eski_kayitlarda_maden_adi_karma_gecmiste_var_yalniz_altinda_yok()
    {
        karmaMaden = true;
        var e = Secili("c1");
        e.Find("#e-gecmis-ac").Click();
        var etiketler = e.FindAll("#e-gecmis .emanet-satir-maden").Select(x => x.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Gümüş", "Altın" }, etiketler);
        // Hücrede yalnız sayı kalır (maden etiketi tür sütununda).
        Assert.All(e.FindAll("#e-gecmis td.sayi"), h => Assert.DoesNotMatch(@"\p{L}", h.TextContent));
    }

    [Fact]
    public void E26b_Yalniz_altin_gecmiste_maden_etiketi_yok()
    {
        var e = Secili("c1");
        e.Find("#e-gecmis-ac").Click();
        Assert.Empty(e.FindAll("#e-gecmis .emanet-satir-maden"));
    }

    // ── E27 ────────────────────────────────────────────────────────────────
    // Doğrulayıcı 28.09 (DÜŞÜK): maden notu yalnız birden çok dolu maden varken.
    [Fact]
    public void E27_Maden_notu_yalniz_birden_cok_madende()
    {
        karmaMaden = true;
        var e = Secili("c1");
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#cekmece-emanet-cikis");
        Assert.Equal(2, e.FindAll("#c-maden-bakiyeleri li").Count);
        Assert.Contains(S.MadenNotu, e.Find("#c-maden-notu").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void E27b_Tek_madende_maden_notu_yok()
    {
        cariSayisi = 4;
        var e = Secili("c3");
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#cekmece-emanet-cikis");
        Assert.Single(e.FindAll("#c-maden-bakiyeleri li"));
        Assert.Empty(e.FindAll("#c-maden-notu"));
    }

    // ── E28 ────────────────────────────────────────────────────────────────
    // Doğrulayıcı 28.09 (KRİTİK, X5 eşi): kullanıcı iade has'ını ELLE yazdıysa
    // maden değişince DOKUNULMAZ; yazmadıysa varsayılan yeni madenin netine kurulur.
    [Fact]
    public void E28_Elle_yazilan_iade_has_maden_degisince_korunur_elle_yazilmamissa_yeni_net()
    {
        karmaMaden = true;
        var e = Secili("c1");
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#cekmece-emanet-cikis");
        Assert.Equal(Bicim.Girdi(11.45m), e.Find("#c-has").GetAttribute("data-deger"));

        // Elle yazmadan gümüşe geç → gümüşün neti; geri altına → altının neti.
        SeciciYardim.Sec(e, "c-maden", "GUMUS");
        Assert.Equal(Bicim.Girdi(1387.5m), e.Find("#c-has").GetAttribute("data-deger"));
        SeciciYardim.Sec(e, "c-maden", "ALTIN");
        Assert.Equal(Bicim.Girdi(11.45m), e.Find("#c-has").GetAttribute("data-deger"));

        // Elle yaz → maden değişince korunur.
        e.Find("#c-has").Input("5");
        SeciciYardim.Sec(e, "c-maden", "GUMUS");
        Assert.Equal(Bicim.Girdi(5m), e.Find("#c-has").GetAttribute("data-deger"));

        // Çekmece kapanıp yeniden açılınca "elle" bayrağı sıfırlanır: gümüş netiyle
        // açılır, altına geçince yine altının neti gelir (5 korunmaz).
        e.Find("#cekmece-emanet-cikis #cekmece-kapat").Click();
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#cekmece-emanet-cikis");
        Assert.Equal(Bicim.Girdi(1387.5m), e.Find("#c-has").GetAttribute("data-deger"));
        SeciciYardim.Sec(e, "c-maden", "ALTIN");
        Assert.Equal(Bicim.Girdi(11.45m), e.Find("#c-has").GetAttribute("data-deger"));
    }

    // E28b: gram girilmeden yardımcıda gümüş AYARI seçilince maden ayardan
    // türetilir (UX-S22) ve varsayılan yine o madenin netine kurulur.
    [Fact]
    public void E28b_Gram_girilmeden_gumus_ayari_secilince_varsayilan_gumus_neti()
    {
        karmaMaden = true;
        var e = Secili("c1");
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#cekmece-emanet-cikis");
        Assert.Equal(Bicim.Girdi(11.45m), e.Find("#c-has").GetAttribute("data-deger"));
        SeciciYardim.Sec(e, "c-ayar", "G925");
        Assert.Equal(Bicim.Girdi(1387.5m), e.Find("#c-has").GetAttribute("data-deger"));
    }

    // Doğrulayıcı 28.09 (DÜŞÜK, 375 px): karma özette her maden parçası ayrı,
    // kırılmaz blok; metin aynı kalır.
    [Fact]
    public void E29_Karma_ozet_maden_parcalari_ayri_blok_metin_ayni()
    {
        karmaMaden = true;
        var e = Secili("c1");
        var parcalar = e.FindAll("#e-ozet-deger .emanet-ozet-parca").Select(x => x.TextContent).ToList();
        Assert.Equal(new[] { $"Altın {Bicim.Has(11.45m)}", $"Gümüş {Bicim.Has(1387.5m)}" }, parcalar);
        Assert.Equal(string.Join(" · ", parcalar), e.Find("#e-ozet-deger").TextContent.Trim());
    }

    // ═══ Tasarım hakemi + doğrulayıcı 28.09 (sekizinci tur) ═══════════════════

    // E30 (KRİTİK): müşteri TELEFONLA da bulunur; eşleşme yoksa liste sessiz
    // boş kalmaz, "eşleşme yok" satırı görünür.
    [Fact]
    public void E30_Musteri_secicide_telefonla_arama_ve_eslesme_yok_satiri()
    {
        telefonlu = true;
        var e = Ciz();
        e.WaitForAssertion(() => Assert.DoesNotContain(OnayKilitSozlugu.Tr.EmanetKurYok, e.Markup, StringComparison.Ordinal));
        e.Find("#e-cari").Click();
        e.Find("#e-cari-ara-girdi").Input("0544");
        var bulunan = SeciciYardim.Secenekler(e, "e-cari");
        Assert.Equal(new[] { "c1" }, bulunan.Select(o => o.Deger()).ToArray());
        Assert.Contains("05441112233", bulunan[0].TextContent, StringComparison.Ordinal);
        Assert.Empty(e.FindAll("#e-cari-eslesme-yok"));

        e.Find("#e-cari-ara-girdi").Input("xyz");
        Assert.DoesNotContain(SeciciYardim.Secenekler(e, "e-cari"), o => o.Deger() != "");
        Assert.Equal(Dil().T.Kabuk.SeciciEslesmeYok, e.Find("#e-cari-eslesme-yok").TextContent.Trim());
        // Tetik metni değişmedi: arama seçimi bozmaz (TEMA TEMİZLİĞİ 17.10: Yıldız'da özgün gövde satırın
        // müşterisi SEÇİLİ açılır → tetik o müşterinin adını taşır; "Müşteri seçin" durumu ulaşılamaz).
        Assert.Contains("Kapı Müşteri", e.Find("#e-cari").TextContent, StringComparison.Ordinal);
    }

    // E31 (KRİTİK): çekmecede Vazgeç + Kaydet TABANDA yan yana (form gövdesinin
    // ortasında değil); Vazgeç gerçekten kapatır; kimlikler aynı.
    [Fact]
    public void E31_Cekmece_tabaninda_Vazgec_ve_Kaydet_yan_yana_Vazgec_kapatir()
    {
        var e = Secili("c1");
        foreach (var (ac, cekmece, eylem, vazgec, kaydet) in new[]
        {
            ("#e-giris-ac", "#cekmece-emanet-giris", "e-giris-eylem", "e-giris-vazgec", "e-giris-kaydet"),
            ("#e-cikis-ac", "#cekmece-emanet-cikis", "e-cikis-eylem", "e-cikis-vazgec", "e-cikis-kaydet"),
        })
        {
            e.Find(ac).Click();
            e.WaitForElement(cekmece);
            var taban = e.Find("#" + eylem);
            Assert.Contains("emanet-cekmece-alt", taban.ClassName, StringComparison.Ordinal);   // yapışkan taban kalıbı (css/emanet.css)
            // Taban çekmece gövdesinin DOĞRUDAN çocuğu ve form bloğundan SONRA.
            Assert.Equal("cekmece-govde", taban.ParentElement!.ClassName);
            Assert.Contains("form-dikey", taban.PreviousElementSibling!.ClassName, StringComparison.Ordinal);
            var dugmeler = taban.Children.Select(c => c.Id).ToArray();
            Assert.Equal(new[] { vazgec, kaydet }, dugmeler);   // ikincil önce, birincil sonda
            Assert.Equal(S.Vazgec, e.Find("#" + vazgec).TextContent.Trim());
            Assert.Empty(e.FindAll($"{cekmece} .form-dikey #{kaydet}"));
            e.Find("#" + vazgec).Click();
            Assert.Empty(e.FindAll(cekmece));
        }
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    // E32 (KRİTİK — K7 hizası): Gram hücresi ham-metin kabının içinde olsa da
    // ızgaranın üç raylı alt ızgarasına oturur (bUnit geometri ölçemez: yapı +
    // kural birlikte sınanır; geometri canlı ölçümle raporda).
    [Fact]
    public void E32_Ayar_ve_Gram_ayni_izgara_raylarinda_kural_emanet_css_de()
    {
        var e = Secili("c1");
        e.Find("#e-giris-ac").Click();
        e.WaitForElement("#cekmece-emanet-giris");
        var gramHucre = e.Find("#e-gram-sar");
        Assert.Contains("emanet-ham", gramHucre.ParentElement!.ClassName, StringComparison.Ordinal);
        Assert.Contains("form-grid-2", gramHucre.ParentElement!.ParentElement!.ClassName, StringComparison.Ordinal);

        var kurallar = CssKaynak.Bildirimler(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "emanet.css"));
        bool Var(string secici, string ozellik, string deger) => kurallar.Any(k =>
            k.Secici.Split(',').Select(s => Regex.Replace(s, @"\s+", " ").Trim()).Contains(secici)
            && k.Ozellik == ozellik && k.Deger.Trim() == deger);
        Assert.True(Var(".form-grid-2 > .emanet-ham > .alan", "grid-template-rows", "subgrid"));
        Assert.True(Var(".form-grid-2 > .emanet-ham > .alan", "grid-row", "span 3"));
        Assert.True(Var("#cekmece-emanet-giris > .cekmece-alt", "display", "none"));
        Assert.True(Var("#cekmece-emanet-cikis > .cekmece-alt", "display", "none"));
    }

    // E33 (DÜŞÜK): hata bandı alan düzelince bir sonraki Kaydet'i beklemeden kalkar.
    [Fact]
    public void E33_Hata_bandi_alan_duzelince_kalkar()
    {
        var e = Secili("c1");
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#c-has").Input("1.500");
        e.Find("#e-cikis-kaydet").Click();
        Assert.Contains(Dil().T.Kabuk.FormEksik, e.Find("#cekmece-emanet-cikis .bant-hata").TextContent, StringComparison.Ordinal);
        e.Find("#c-has").Input("2");
        Assert.Empty(e.FindAll("#cekmece-emanet-cikis .bant-hata"));

        e.Find("#e-giris-ac").Click();
        e.WaitForElement("#e-gram");
        e.Find("#e-giris-kaydet").Click();                 // gram boş → band
        Assert.Contains(Dil().T.Kabuk.FormEksik, e.Find("#cekmece-emanet-giris .bant-hata").TextContent, StringComparison.Ordinal);
        e.Find("#e-gram").Input("3");
        Assert.Empty(e.FindAll("#cekmece-emanet-giris .bant-hata"));
    }

    // E34 (DÜŞÜK): bozuk JSON da hata bandına düşer; sayfa çökmez, "Emaneti yok" denmez.
    [Fact]
    public void E34_Bozuk_JSON_hata_bandina_duser()
    {
        var e = Secili("c1", () => bozukJson = true);
        e.WaitForElement("#e-liste-hata");
        Assert.Equal("—", e.Find("#e-ozet-deger").TextContent.Trim());
        Assert.Equal(S.BilgiAlinamadi, e.Find("#e-kilit").TextContent.Trim());
    }

    // E35 (DÜŞÜK): giriş düğmesi #e-kilit'e yalnız sebep AYNIYSA bağlanır.
    [Fact]
    public void E35_Giris_dugmesi_iadenin_sebebini_aciklayan_olarak_almaz()
    {
        // TEMA TEMİZLİĞİ 17.10: "müşteri seçilmedi → iki sebep aynı" durumu Yıldız'da ulaşılamaz (özgün gövde
        // satırın müşterisi seçili açılır). Emaneti olan müşteride (c0) kilit yok → iki düğme de açıklayan almaz.
        var e = Ciz();
        e.WaitForAssertion(() => Assert.False(e.Find("#e-cikis-ac").HasAttribute("disabled")));
        Assert.Empty(e.FindAll("#e-kilit"));
        Assert.False(e.Find("#e-giris-ac").HasAttribute("aria-describedby"));
        Assert.False(e.Find("#e-cikis-ac").HasAttribute("aria-describedby"));

        SeciciYardim.Sec(e, "e-cari", "c2");               // emaneti yok: iade kilitli, giriş açık
        e.WaitForAssertion(() => Assert.Equal(S.IadeYok, e.Find("#e-kilit").TextContent.Trim()));
        Assert.False(e.Find("#e-giris-ac").HasAttribute("aria-describedby"));
        Assert.Equal("e-kilit", e.Find("#e-cikis-ac").GetAttribute("aria-describedby"));
    }

    // E36 (doğrulayıcı 28.09, ORTA): iade yardımcısında gram silinince bölüm
    // odak içindeyken KAPANMAZ ve has bayat kalmaz — elle yazılmadıysa "tümünü
    // iade et" varsayılanına (seçili madenin neti) döner. Yeni çekmecede kapalı.
    [Fact]
    public void E36_Iade_gram_silinince_bolum_acik_kalir_has_varsayilana_doner()
    {
        var e = Secili("c1");
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#cekmece-emanet-cikis");
        var varsayilan = e.Find("#c-has").GetAttribute("data-deger");
        Assert.False(e.Find("#e-cikis-hesap").HasAttribute("open"));

        e.Find("#c-gram").Input("10");
        Assert.True(e.Find("#e-cikis-hesap").HasAttribute("open"));
        var k22 = Kuyumcu.Core.Domain.Ayarlar.Bul("K22")!;
        Assert.Equal(Bicim.Girdi(Kuyumcu.Core.Domain.HasHesap.ToHasGram(10m, k22)),
            e.Find("#c-has").GetAttribute("data-deger"));

        e.Find("#c-gram").Input("");
        Assert.True(e.Find("#e-cikis-hesap").HasAttribute("open"));
        Assert.Equal(varsayilan, e.Find("#c-has").GetAttribute("data-deger"));

        // Elle yazılan has, gram girilip silinince korunur.
        e.Find("#c-has").Input("5");
        e.Find("#c-gram").Input("");
        Assert.Equal(Bicim.Girdi(5m), e.Find("#c-has").GetAttribute("data-deger"));

        // Çekmece yeniden açılınca bölüm yine kapalı başlar.
        e.Find("#e-cikis-vazgec").Click();
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#cekmece-emanet-cikis");
        Assert.False(e.Find("#e-cikis-hesap").HasAttribute("open"));
    }

    // E37 (doğrulayıcı 28.09, ORTA — aynı kalıp girişte): taş/not silinince
    // isteğe bağlı bölüm kapanmaz; yeni çekmecede kapalı başlar.
    [Fact]
    public void E37_Giris_tas_ve_not_silinince_detay_acik_kalir()
    {
        var e = Secili("c1");
        e.Find("#e-giris-ac").Click();
        e.WaitForElement("#e-gram");
        Assert.False(e.Find("#e-giris-detay").HasAttribute("open"));

        e.Find("#e-tas").Input("1");
        Assert.True(e.Find("#e-giris-detay").HasAttribute("open"));
        e.Find("#e-tas").Input("");
        Assert.True(e.Find("#e-giris-detay").HasAttribute("open"));

        e.Find("#e-giris-vazgec").Click();
        e.Find("#e-giris-ac").Click();
        e.WaitForElement("#e-gram");
        Assert.False(e.Find("#e-giris-detay").HasAttribute("open"));

        e.Find("#e-aciklama").Input("not");
        Assert.True(e.Find("#e-giris-detay").HasAttribute("open"));
        e.Find("#e-aciklama").Input("");
        Assert.True(e.Find("#e-giris-detay").HasAttribute("open"));
    }

    // ═══ 28.09 t39 — tasarım hakemi KRİTİK + doğrulayıcı DÜŞÜK bulguları ═══════

    private void BasitOturum()
        => Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1",
                ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));

    // ── E38 ────────────────────────────────────────────────────────────────
    // [KRİTİK] Eski kayıtlar açıkken tablonun altında DegismezIpucu kutusu YOK
    // (BASİT ve PRO); kilitli satırın sebebi yalnız kendi düğmesinin title'ında.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void E38_Eski_kayitlar_acikken_degismez_kutusu_yok(bool basit)
    {
        if (basit) BasitOturum();
        cariSayisi = 8;
        var e = Secili("c8");
        e.Find("#e-gecmis-ac").Click();
        e.WaitForElement("#eh-storno-h84");
        Assert.Empty(e.FindAll("#e-gecmis .degismez-ipucu"));
        Assert.Empty(e.FindAll("[id^='degismez-ipucu-EmanetHareketi']"));
        Assert.Equal(S.NedenGeriAlindi, e.Find("#eh-storno-h81").GetAttribute("title"));
    }

    // ── E39 ────────────────────────────────────────────────────────────────
    // [KRİTİK] BASİT modda iade çekmecesinde teknik "toplam has yalnız altındır" notu YOK.
    [Fact]
    public void E39_Basit_modda_iade_cekmecesinde_maden_notu_yok()
    {
        BasitOturum();
        karmaMaden = true;
        var e = Secili("c1");
        e.Find("#e-cikis-ac").Click();
        var c = e.WaitForElement("#cekmece-emanet-cikis");
        Assert.Equal(2, e.FindAll("#c-maden-bakiyeleri li").Count);
        Assert.Empty(e.FindAll("#c-maden-notu"));
        Assert.DoesNotContain(S.MadenNotu, c.TextContent, StringComparison.Ordinal);
    }

    // ── E40 ────────────────────────────────────────────────────────────────
    // [KRİTİK] Ayrıntı çekmecesi: ayrı "Değiştirilemez" başlığı, gram/ayar teknik satırı
    // ve kilit kutusu YOK; düzeltme yolu TEK kısa satır; düğme yalnız "Geri al".
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void E40_Ayrinti_cekmecesi_tek_kisa_satir_dugme_Geri_al(bool basit)
    {
        if (basit) BasitOturum();
        cariSayisi = 8;
        var e = Secili("c8");
        e.Find("#e-gecmis-ac").Click();
        e.WaitForElement("#eh-detay-h84").Click();
        var c = e.WaitForElement("#cekmece-emanet-hareket");
        Assert.Empty(e.FindAll("#eh-detay-kilitli-baslik"));
        Assert.Empty(e.FindAll("#eh-detay-gram-notu"));
        Assert.Empty(c.QuerySelectorAll(".degismez-ipucu"));
        Assert.DoesNotContain(KurVitrinEmanetSozlugu.Tr.KilitliAlanlar, c.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain(KurVitrinEmanetSozlugu.Tr.GramAyarNotu, c.TextContent, StringComparison.Ordinal);
        Assert.Equal(S.DetayDuzeltme, e.Find("#eh-detay-duzeltme").TextContent.Trim());
        Assert.Equal(S.SatirGeriAl, e.Find("#eh-detay-geri-al-h84").TextContent.Trim());
        // Kilitli alanlar yine yazılı (girdisiz).
        Assert.NotNull(e.Find("#eh-detay-has"));
        Assert.Empty(c.QuerySelectorAll("input, textarea, select"));
    }

    // ── E41 ────────────────────────────────────────────────────────────────
    // [DÜŞÜK] İade has alanı özetle aynı 4 hane; gram silinince alan "0" değil BOŞ.
    [Fact]
    public void E41_Iade_has_alani_4_hane_gram_silinince_bos()
    {
        var e = Secili("c1");
        e.Find("#e-cikis-ac").Click();
        e.WaitForElement("#cekmece-emanet-cikis");
        var beklenen = Bicim.HasGirdi(decimal.Parse(e.Find("#c-has").GetAttribute("data-deger")!,
            System.Globalization.CultureInfo.GetCultureInfo("tr-TR")));
        Assert.Equal(beklenen, e.Find("#c-has").GetAttribute("value"));
        Assert.Equal(4, beklenen.Split(',')[1].Length);
        e.WaitForElement("#c-gram").Input("10");
        e.Find("#c-gram").Input("");
        e.Find("#c-gram").Blur();
        Assert.Equal("", e.Find("#c-gram").GetAttribute("value") ?? "");
    }

    private DilServisi Dil() => Services.GetRequiredService<DilServisi>();
}
