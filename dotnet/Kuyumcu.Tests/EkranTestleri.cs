using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Contracts.Bilesenler.MasakFormu;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// EKRAN TESTLERİ — PLAN-2 Adım 0b (Aşkın'ın "geçirmem" maddesi #2).
//
// TESPİT (2026-09-13): 30 razor dosyası, 4.836 satır, **0 test**. Kullanıcı
// arayüzünde hiçbir otomatik doğrulama yoktu. 13 yeni ekran eklemeden önce
// kapı kurulması şartı buradan geldi.
//
// KAPSAM KARARI (bilinçli): Aşkın'ın çizgisi — "hepsini test etmeyeceğiz,
// bu bir yalan olurdu. Hedef satır kapsamı değil DAVRANIŞ kapsamı: para/has
// gösteren, yasal kayıt üreten veya yetki gizleyen bileşenler."
//
// CODE-BEHIND AYRIMI YAPILMADI — ve bu bilinçli bir sapmadır:
// bUnit `@code` bloğunu da test eder; ayrım yalnız private üyeye erişmek
// gerektiğinde şart olur. 12 bileşeni toplu hâlde code-behind'a taşımak
// ölçülmüş bir kazanç olmadan 4.836 satırlık bir yeniden düzenleme riskidir.
// Ayrım, ihtiyaç duyan bileşende, o bileşen test edilirken yapılacak.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class MasakFormuEkranTestleri : BunitContext
{
    private static readonly Esikler Esik = new(
        KimlikEsigi: 185_000m,
        AmacBeyaniEsigi: 200_000m,
        BeyanFormuEsigi: 2_000_000.01m,
        EkBelgeEsigi: 20_000_000.01m,
        // ⚠ 20 = İŞLEM AMACI BEYANI açıklamasının asgari uzunluğu (üretim değeri).
        // ŞÜPHELİ İŞLEM BİLDİRİMİ gerekçesinin asgarisi AYRI bir kuraldır (300,
        // MASAK Rehber 2.0 §4.2.7) ve ayrı yapılandırma anahtarındadır.
        // Bu ikisi 2026-09-13'e kadar karıştırılabilir durumdaydı.
        AsgariAciklamaUzunlugu: 20,
        BelgeTurleri: ["TC_KIMLIK", "PASAPORT", "SURUCU_BELGESI"]);

    public MasakFormuEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;   // DilServisi localStorage'a bakar
        Services.AddScoped<DilServisi>();
    }

    /// <summary>
    /// Sunucunun <c>GET /masak/gereksinim</c> yanıtını taklit eder.
    /// ⚠ Bu mantık artık bileşende DEĞİL sunucuda: `MasakKuralMotoru.Gereksinim`.
    /// Burada yalnız sunucunun ne döndüreceği canlandırılır (TB-11 sonrası).
    /// </summary>
    private static MasakGereksinim SunucuGereksinimi(decimal tutar) => tutar switch
    {
        >= 20_000_000.01m => new("BEYAN_FORMU_VE_BELGE", true, true, true, true, 20),
        >= 2_000_000.01m  => new("BEYAN_FORMU",          true, true, true, false, 20),
        >= 200_000m       => new("AMAC_BEYANI",          true, true, false, false, 20),
        >= 185_000m       => new("YALNIZ_KIMLIK",        true, false, false, false, 20),
        _                 => new("YOK",                  false, false, false, false, 0),
    };

    private IRenderedComponent<MasakFormu> Ciz(decimal tutar) =>
        Render<MasakFormu>(p => p
            .Add(c => c.Model, new MasakFormu.MasakBilgi())
            .Add(c => c.Tutar, tutar)
            .Add(c => c.Esik, Esik)
            .Add(c => c.Gereksinim, SunucuGereksinimi(tutar)));

    // ── Kademeye göre alan açılması ──────────────────────────────────────────

    [Fact]
    public void Kimlik_kademesinde_yalniz_kimlik_alanlari_cikar()
    {
        var ekran = Ciz(185_000m);

        Assert.NotNull(ekran.Find("#masak-belge-no"));
        Assert.NotNull(ekran.Find("#masak-ad-soyad"));
        // Üst kademelerin alanları GÖRÜNMEMELİ.
        Assert.Empty(ekran.FindAll("#masak-amac"));
        Assert.Empty(ekran.FindAll("#masak-fon"));
        Assert.Empty(ekran.FindAll("#masak-ekbelge"));
    }

    [Fact]
    public void Amac_kademesinde_amac_ve_aciklama_acilir()
    {
        var ekran = Ciz(200_000m);

        Assert.NotNull(ekran.Find("#masak-amac"));
        Assert.NotNull(ekran.Find("#masak-aciklama"));
        Assert.Empty(ekran.FindAll("#masak-fon"));
    }

    [Fact]
    public void Beyan_formu_kademesinde_fon_kaynagi_acilir()
    {
        var ekran = Ciz(2_000_000.01m);

        Assert.NotNull(ekran.Find("#masak-fon"));
        Assert.Empty(ekran.FindAll("#masak-ekbelge"));
    }

    [Fact]
    public void En_ust_kademede_ek_belge_de_acilir()
    {
        var ekran = Ciz(20_000_000.01m);

        Assert.NotNull(ekran.Find("#masak-fon"));
        Assert.NotNull(ekran.Find("#masak-ekbelge"));
    }

    // ── Eşik SINIRLARI — ekranda da ────────────────────────────────────────
    // Kural motorunda sınır testleri var (MasakKuralTestleri, 32 satır).
    // Ama EKRAN o kuralı kendi yeniden yazıyor; sınırlar burada da sınanmalı.

    [Theory]
    [InlineData(199_999.99, false)]   // amaç kademesinin BİR KURUŞ altı
    [InlineData(200_000.00, true)]    // tam eşik → açılır
    public void Amac_alani_esigin_tam_sinirinda_acilir(decimal tutar, bool acilmali)
    {
        var ekran = Ciz(tutar);
        Assert.Equal(acilmali, ekran.FindAll("#masak-amac").Count == 1);
    }

    [Fact]
    public void Belge_turleri_sunucudan_gelen_listeden_doldurulur()
    {
        // Sabit liste kodda OLMAMALI: belge türleri sunucudan gelir.
        var secenekler = SeciciYardim.Secenekler(Ciz(185_000m), "masak-belge-turu")
            .Select(o => o.Ad()).ToList();

        Assert.Equal(3, secenekler.Count);
        // ZO-ISLEM-A 28.09 (tasarım hakemi K22): ham kod ("TC KIMLIK") DEĞİL sözlükteki ad.
        Assert.Contains(Kuyumcu.Core.I18n.Sozlukler.Tr.SayfaDuzeltme.BelgeTuruAdi("TC_KIMLIK"), secenekler);
        Assert.Contains(Kuyumcu.Core.I18n.Sozlukler.Tr.SayfaDuzeltme.BelgeTuruAdi("PASAPORT"), secenekler);
        Assert.DoesNotContain("TC KIMLIK", secenekler);
    }

    [Fact]
    public void ZO28_uyruk_etiketi_ham_kod_tasimaz_ipucu_kodu_aciklar()
    {
        // ZO-ISLEM-A 28.09 (2. hakem turu): uyruk ham ISO kodu yazdırılmaz — Türkiye
        // varsayılanlı ülke SEÇİCİSİ (ad görünür, gövdeye KOD gider).
        var ekran = Ciz(185_000m);
        // YASAL-PARAM-EKRAN tur 2: isteğe bağlı alanlar kapalı "Diğer bilgiler" katlanırında.
        Assert.Empty(ekran.FindAll("#masak-uyruk-alan"));
        ekran.Find("#masak-diger-ac").Click();
        var sar = ekran.Find("#masak-uyruk-alan").TextContent;
        Assert.Contains(Kuyumcu.Core.I18n.ZoIslemSozlugu.Tr.MasakUyruk, sar);
        Assert.Contains(Kuyumcu.Core.I18n.ZoIslemSozlugu.Tr.MasakUyrukIpucu, sar);
        Assert.Contains("Türkiye", ekran.Find("#masak-uyruk").TextContent);
        Assert.DoesNotContain("TUR", ekran.Find("#masak-uyruk").TextContent);
        Assert.Empty(ekran.FindAll("#masak-uyruk-kod"));
        Assert.Empty(ekran.FindAll("input#masak-uyruk"));
    }

    [Fact]
    public void ZO28b_uyruk_DIGER_ulke_kod_kutusunu_acar_ve_KOD_modele_buyuk_harfle_gider()
    {
        var model = new MasakFormu.MasakBilgi();
        var ekran = Render<MasakFormu>(p => p
            .Add(c => c.Model, model).Add(c => c.Tutar, 185_000m)
            .Add(c => c.Esik, Esik).Add(c => c.Gereksinim, SunucuGereksinimi(185_000m)));
        ekran.Find("#masak-diger-ac").Click();   // YASAL-PARAM-EKRAN tur 2: isteğe bağlı alanlar katlanırda
        Assert.Equal("TUR", model.Uyruk);

        // Listeden bir ülke → gövdeye KOD.
        ekran.Find("#masak-uyruk").Click();
        var azer = ekran.FindAll("li.secici-secenek").First(l => l.TextContent.Contains("Azerbaycan"));
        azer.Click();
        Assert.Equal("AZE", model.Uyruk);

        // "Diğer ülke" → kod kutusu açılır, listedeki eski kod gövdede KALMAZ.
        ekran.Find("#masak-uyruk").Click();
        ekran.FindAll("li.secici-secenek").First(l => l.GetAttribute("data-deger") == Kuyumcu.Core.I18n.UlkeListesi.Diger).Click();
        Assert.Null(model.Uyruk);
        ekran.Find("#masak-uyruk-kod").Input("nld");
        Assert.Equal("NLD", model.Uyruk);
    }

    [Fact]
    public void ZO28b_Sade_modda_baslik_jargonsuz_rozet_ve_aciklama_TEKRARI_yok()
    {
        var sade = Render<MasakFormu>(p => p
            .Add(c => c.Model, new MasakFormu.MasakBilgi()).Add(c => c.Tutar, 185_000m)
            .Add(c => c.Esik, Esik).Add(c => c.Gereksinim, SunucuGereksinimi(185_000m))
            .Add(c => c.Sade, true));
        var baslik = sade.Find(".kart--masak h3").TextContent;
        // YASAL-PARAM-EKRAN tur 2 (tasarım hakemi KRİTİK): başlık tetikleyen kademeyi söyler ve İKİ MODDA AYNIDIR.
        Assert.Equal(string.Format(Kuyumcu.Core.I18n.YasalParamSozlugu.Tr.BaslikKimlik, Bicim.Tl(185_000m)), baslik);
        Assert.DoesNotContain("MASAK", sade.Find(".kart--masak").TextContent);
        Assert.Empty(sade.FindAll(".kart--masak .rozet"));

        // PRO: AYNI başlık, rozet yok; kuralın tek cümlesi (MASAK atıflı) yalnız #masak-kural satırında.
        var tam = Ciz(185_000m);
        Assert.Equal(baslik, tam.Find(".kart--masak h3").TextContent);
        Assert.Empty(tam.FindAll(".kart--masak .rozet"));
        Assert.Contains("MASAK", tam.Find("#masak-kural").TextContent);
    }

    // ZO-ISLEM-A 28.09 (doğrulayıcı DÜŞÜK): uyruk "TUR" YALNIZ T.C. kimlik kartından
    // çıkarılır; pasaportlu (yabancı) müşteri sessizce Türk olarak kaydedilmez.
    [Fact]
    public void ZO28c_uyruk_TUR_yalniz_TC_kimlikte_PASAPORTta_BOS_elle_secilen_EZILMEZ()
    {
        var model = new MasakFormu.MasakBilgi();
        var ekran = Render<MasakFormu>(p => p
            .Add(c => c.Model, model).Add(c => c.Tutar, 185_000m)
            .Add(c => c.Esik, Esik).Add(c => c.Gereksinim, SunucuGereksinimi(185_000m)));
        ekran.Find("#masak-diger-ac").Click();   // YASAL-PARAM-EKRAN tur 2: isteğe bağlı alanlar katlanırda
        Assert.Equal("TC_KIMLIK", model.BelgeTuru);
        Assert.Equal("TUR", model.Uyruk);

        void BelgeSec(string kod)
        {
            ekran.Find("#masak-belge-turu").Click();
            ekran.FindAll("li.secici-secenek").First(l => l.GetAttribute("data-deger") == kod).Click();
        }

        BelgeSec("PASAPORT");
        Assert.Equal("PASAPORT", model.BelgeTuru);
        Assert.Null(model.Uyruk);
        Assert.Equal(Kuyumcu.Core.I18n.ZoIslemSozlugu.Tr.MasakUyrukSecin, ekran.Find("#masak-uyruk-ipucu").TextContent);
        Assert.Contains(Kuyumcu.Core.I18n.ZoIslemSozlugu.Tr.MasakUyrukYerTutucu, ekran.Find("#masak-uyruk").TextContent);

        BelgeSec("TC_KIMLIK");
        Assert.Equal("TUR", model.Uyruk);

        // Elle seçilen uyruk belge türü değişince EZİLMEZ.
        ekran.Find("#masak-uyruk").Click();
        ekran.FindAll("li.secici-secenek").First(l => l.TextContent.Contains("Azerbaycan")).Click();
        BelgeSec("PASAPORT");
        Assert.Equal("AZE", model.Uyruk);
        BelgeSec("SURUCU_BELGESI");
        Assert.Equal("AZE", model.Uyruk);

        // Başlıkta satır içi stil yok (sınıf).
        Assert.Null(ekran.Find(".kart--masak h3").GetAttribute("style"));
        Assert.Contains("zo-masak-baslik", ekran.Find(".kart--masak h3").ClassName);
    }

    // 28.09 (3. doğrulama DÜŞÜK) — "Diğer ülke" kodu ISO 3166-1 alpha-3 listesinden doğrulanır:
    // listede olmayan kod alan altında Türkçe hata verir ve form Eksiksiz sayılmaz.
    [Fact]
    public void ZO28d_diger_ulke_kodu_ISO_DISIYSA_alan_alti_hata_ve_kayit_kilitli()
    {
        var model = new MasakFormu.MasakBilgi();
        var gereksinim = SunucuGereksinimi(185_000m);
        var ekran = Render<MasakFormu>(p => p
            .Add(c => c.Model, model).Add(c => c.Tutar, 185_000m)
            .Add(c => c.Esik, Esik).Add(c => c.Gereksinim, gereksinim));
        model.BelgeTuru = "PASAPORT";   // TCKN-DOGRULAMA: "U1234567" pasaport no — T.C. Kimlik değil (kural belge türüne göre)
        model.BelgeNo = "U1234567"; model.AdSoyad = "John Smith";
        model.IslemAmaci = "Ticari"; model.Aciklama = new string('a', 60);
        model.FonKaynagi = "Maas"; model.EkBelgeReferansi = "EK-1";

        ekran.Find("#masak-diger-ac").Click();   // YASAL-PARAM-EKRAN tur 2: isteğe bağlı alanlar katlanırda
        ekran.Find("#masak-uyruk").Click();
        ekran.FindAll("li.secici-secenek").First(l => l.GetAttribute("data-deger") == Kuyumcu.Core.I18n.UlkeListesi.Diger).Click();
        ekran.Find("#masak-uyruk-kod").Input("zzq");
        Assert.Equal("ZZQ", model.Uyruk);
        Assert.Equal(Kuyumcu.Core.I18n.ZoIslemSozlugu.Tr.MasakUyrukKodGecersiz, ekran.Find("#masak-uyruk-kod-hata").TextContent.Trim());
        Assert.False(MasakFormu.Eksiksiz(model, gereksinim));

        ekran.Find("#masak-uyruk-kod").Input("fra");
        Assert.Equal("FRA", model.Uyruk);
        Assert.Empty(ekran.FindAll("#masak-uyruk-kod-hata"));
        Assert.True(MasakFormu.Eksiksiz(model, gereksinim));

        // ICAO vatansız kodu (pasaportta geçer) kabul edilir; boş uyruk da (zorunluluğu sunucu söyler).
        Assert.True(Kuyumcu.Core.I18n.UlkeListesi.KodGecerli("XXA"));
        Assert.True(Kuyumcu.Core.I18n.UlkeListesi.KodGecerli(null));
        Assert.False(Kuyumcu.Core.I18n.UlkeListesi.KodGecerli("TR"));
        // 28.09 (4. doğrulama DÜŞÜK): Kosova pasaport kodu RKS (AB Konseyi PRADO RKS-AO-03001) kabul
        // edilir; pasaportta doğrulanamayan XKX eklenmedi.
        Assert.True(Kuyumcu.Core.I18n.UlkeListesi.KodGecerli("rks"));
        Assert.False(Kuyumcu.Core.I18n.UlkeListesi.KodGecerli("XKX"));
        Assert.Equal(257, Kuyumcu.Core.I18n.UlkeListesi.IsoKodlari.Count);   // 249 ISO + 7 ICAO + RKS
    }

    // 28.09 (3. doğrulama DÜŞÜK) — BASİT modda belge no etiketi kısaltmasız, belge türüne göre;
    // PRO etiketi (hub) değişmez.
    [Fact]
    public void ZO28d_BASIT_belge_no_etiketi_TCKN_kisaltmasi_tasimaz_belge_turune_gore()
    {
        var model = new MasakFormu.MasakBilgi();
        var ekran = Render<MasakFormu>(p => p
            .Add(c => c.Model, model).Add(c => c.Tutar, 185_000m).Add(c => c.Sade, true)
            .Add(c => c.Esik, Esik).Add(c => c.Gereksinim, SunucuGereksinimi(185_000m)));
        var tr = Kuyumcu.Core.I18n.ZoIslemSozlugu.Tr;
        string Etiket() => ekran.Find("label[for=masak-belge-no]").TextContent;
        Assert.Contains(tr.MasakBelgeNoTc, Etiket());
        Assert.DoesNotContain("TCKN", Etiket());
        ekran.Find("#masak-belge-turu").Click();
        ekran.FindAll("li.secici-secenek").First(l => l.GetAttribute("data-deger") == "PASAPORT").Click();
        Assert.Contains(tr.MasakBelgeNoPasaport, Etiket());
        ekran.Find("#masak-belge-turu").Click();
        ekran.FindAll("li.secici-secenek").First(l => l.GetAttribute("data-deger") == "SURUCU_BELGESI").Click();
        Assert.Contains(tr.MasakBelgeNoDiger, Etiket());

        var pro = Render<MasakFormu>(p => p
            .Add(c => c.Model, new MasakFormu.MasakBilgi()).Add(c => c.Tutar, 185_000m)
            .Add(c => c.Esik, Esik).Add(c => c.Gereksinim, SunucuGereksinimi(185_000m)));
        // YASAL-PARAM-EKRAN tur 2 (tasarım hakemi KRİTİK): PRO da AYNI kısaltmasız etiket.
        Assert.Contains(tr.MasakBelgeNoTc, pro.Find("label[for=masak-belge-no]").TextContent);
        Assert.DoesNotContain("TCKN", pro.Find("label[for=masak-belge-no]").TextContent);
    }

    [Fact]
    public void Asgari_aciklama_uzunlugu_SUNUCUDAN_gelen_degeri_gosterir()
    {
        // Ekran bu sayıyı HESAPLAMAZ, sunucudan gelen değeri gösterir.
        // Sabit yazsaydı mevzuat değişince sessizce yanlış olurdu (ADR-0005).
        Assert.Contains("20", Ciz(200_000m).Markup);
    }

    [Fact]
    public void Kademe_karari_ISTEMCIDE_hesaplanmaz()
    {
        // TB-11: eskiden bileşen hangi alanın hangi eşikte gerektiğini kendisi
        // hesaplıyordu. Artık sunucunun gereksinimi neyse ona uyar — eşik
        // rakamlarıyla çelişse bile. Bu test o bağımlılığı bekçiler.
        var e = Render<MasakFormu>(p => p
            .Add(c => c.Model, new MasakFormu.MasakBilgi())
            .Add(c => c.Tutar, 1_000m)                       // eşiğin ÇOK altında
            .Add(c => c.Esik, Esik)
            // ...ama sunucu "ek belge de gerekli" diyor:
            .Add(c => c.Gereksinim, new MasakGereksinim("BEYAN_FORMU_VE_BELGE", true, true, true, true, 20)));

        // Ekran tutara değil, SUNUCUYA uymalı.
        Assert.NotNull(e.Find("#masak-amac"));
        Assert.NotNull(e.Find("#masak-fon"));
        Assert.NotNull(e.Find("#masak-ekbelge"));
    }

    // ── Eksiksiz() — sunucu kuralının istemci ikizi ─────────────────────────

    [Fact]
    public void Esik_altinda_form_eksiksiz_sayilir()
        => Assert.True(MasakFormu.Eksiksiz(new MasakFormu.MasakBilgi(), SunucuGereksinimi(184_999m)));

    [Fact]
    public void Esik_ustunde_kimlik_eksikse_eksiksiz_degildir()
        => Assert.False(MasakFormu.Eksiksiz(new MasakFormu.MasakBilgi(), SunucuGereksinimi(185_000m)));

    [Fact]
    public void Kisa_aciklama_eksiksiz_sayilmaz()
    {
        var m = new MasakFormu.MasakBilgi
        {
            BelgeNo = "10000000146", AdSoyad = "Ayşe Demir",
            // Sunucunun verdiği asgari 20; sınırın BİR ALTI reddedilmeli.
            IslemAmaci = "Yatırım", Aciklama = new string('a', 19),
        };
        Assert.False(MasakFormu.Eksiksiz(m, SunucuGereksinimi(200_000m)));

        m.Aciklama = new string('a', 20);
        Assert.True(MasakFormu.Eksiksiz(m, SunucuGereksinimi(200_000m)));
    }

    [Fact]
    public void Bosluktan_ibaret_aciklama_uzunluk_sayilmaz()
    {
        var m = new MasakFormu.MasakBilgi
        {
            BelgeNo = "10000000146", AdSoyad = "Ayşe Demir",
            IslemAmaci = "Yatırım", Aciklama = new string(' ', 400),
        };
        Assert.False(MasakFormu.Eksiksiz(m, SunucuGereksinimi(200_000m)));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// DENETİM İZİ EKRANI — PLAN-2 Adım 0d'nin ekran tarafı.
//
// BU TESTLER NEDEN VAR (gerçek olay, 2026-09-13): sayfa ilk yazıldığında
// `<Rozet Metin="..." />` ve `<StatKart Baslik="..." Ikon="..." />` kullanıldı.
// Bu parametreler o bileşenlerde YOK — ama `dotnet build` 0 hata verdi:
// Blazor eşleşmeyen bileşen parametresini DERLEMEDE değil ÇALIŞMA ANINDA
// patlatır. Yani ekran, hiç açılmadan "derlendi" sayılıyordu.
// bUnit bu sınıfı yakalar; yakaladığı için burada duruyor.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class DenetimIziEkranTestleri : BunitContext
{
    private const string OzetJson = """
        {"toplam":3,"hassasAdet":1,"enEskiKayit":"2026-09-01T08:00:00Z",
         "eylemler":[{"eylem":"OKUMA","adet":2},{"eylem":"EKLEME","adet":1}],
         "turler":[{"varlikTuru":"Cari","adet":2},{"varlikTuru":"Kasa","adet":1}]}
        """;

    // Paket 48 Faz A: satırlar artık bağlam damgası taşır. İlk satır damgalı
    // (şube + kasa), ikinci satır DAMGASIZ — ekran onu "—" göstermeli, şube
    // UYDURMAMALI.
    private const string ListeJson = """
        {"toplam":2,"limit":100,"offset":0,"kayitlar":[
          {"id":"1","tarih":"2026-09-13T10:00:00Z","eylem":"OKUMA","varlikTuru":"Cari",
           "varlikId":"c1","hassas":true,"detay":"cari kartı görüntülendi","ipAdresi":"127.0.0.1",
           "kullaniciId":"u1","kullanici":"Ramazan Akkocan","rol":"SAHIP",
           "subeId":"s1","subeAd":"Merkez","kasaId":"k9","kasaAd":"Ana Kasa"},
          {"id":"2","tarih":"2026-09-13T09:00:00Z","eylem":"EKLEME","varlikTuru":"Kasa",
           "varlikId":"k1","hassas":false,"detay":null,"ipAdresi":null,
           "kullaniciId":null,"kullanici":"sistem","rol":null,
           "subeId":null,"subeAd":null,"kasaId":null,"kasaAd":null}]}
        """;

    /// <summary>İki şube — UX-S22: seçici ancak birden fazla şube varsa çıkar.</summary>
    private const string SubelerJson = """
        [{"id":"s1","ad":"Merkez","aktif":true},{"id":"s2","ad":"Şube-2","aktif":true}]
        """;

    public DenetimIziEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi())
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    /// <summary>Gerçek ağ yok: iki ucun sözleşmesini sabit JSON ile karşılar.</summary>
    private sealed class SahteApi : HttpMessageHandler
    {
        /// <summary>Giden isteklerin TAM URL'i — sorgu dizgesi sınanabilsin.</summary>
        internal static readonly List<string> Istekler = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            Istekler.Add(istek.RequestUri!.ToString());
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = yol.EndsWith("/subeler") ? SubelerJson
                : yol.EndsWith("/ozet") ? OzetJson
                : ListeJson;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.DenetimIzi> Ciz()
        => Render<Kuyumcu.Web.Pages.DenetimIzi>();

    /// <summary>ZO-BELGE: süzgeçler, değişmez notu ve yasal dayanak KAPALI "Süzgeç"
    /// katlanırında — açılınca aynen (id'ler değişmedi).</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.DenetimIzi> CizSuzgecAcik()
    {
        var e = Ciz();
        e.WaitForElement("#dz-suzgec-ac");
        e.Find("#dz-suzgec-ac").Click();
        e.WaitForElement("#dz-yasal-dayanak");
        return e;
    }

    [Fact]
    public void Sayfa_hata_vermeden_cizilir()
    {
        // Asıl değeri burada: eşleşmeyen bileşen parametresi olsaydı bu test patlardı.
        var ekran = CizSuzgecAcik();
        // PageTitle bUnit çıktısına girmez; sayfanın kendi metnine bakılır.
        // ZO-BELGE: yasal dayanak açıklamadan çıktı (göz #16), katlanırda YAZILI.
        Assert.Contains("KVKK md.12", ekran.Markup);
        Assert.NotEmpty(ekran.FindAll("table.veri-tablosu"));
    }

    [Fact]
    public void Ozet_kartlari_sunucudan_gelen_sayilari_gosterir()
    {
        var ekran = Ciz();
        // ZO-BELGE: üç kart → TEK özet satırı; sayılar aynı kaynaktan.
        ekran.WaitForElement("#dz-ozet");
        Assert.Equal(string.Format(Kuyumcu.Core.I18n.ZoBelgeSozlugu.Tr.DenetimOzet, "3", "1"),
            ekran.Find("#dz-ozet-sayilar").TextContent.Trim());          // toplam · hassas
        Assert.Contains("01.09.2026", ekran.Find("#dz-ozet-en-eski").TextContent); // en eski
    }

    [Fact]
    public void Kayitlar_tabloda_listelenir()
    {
        var ekran = Ciz();
        var satirlar = ekran.FindAll("tbody tr");

        Assert.Equal(2, satirlar.Count);
        Assert.Contains("Ramazan Akkocan", ekran.Markup);
        Assert.Contains("sistem", ekran.Markup);
    }

    [Fact]
    public void Kisisel_veri_erisimi_isaretlenir()
    {
        var ekran = Ciz();
        // PAKET S1-i: emoji ikon YASAK ("🔐" → Ikon bileşeni, erişilebilir adıyla).
        var hucreler = ekran.FindAll("tbody tr td:last-child");

        var ikon = hucreler[0].QuerySelector("svg[role=img]");   // hassas = true
        Assert.NotNull(ikon);
        Assert.Equal("Hassas kayıt", ikon!.GetAttribute("aria-label"));
        Assert.Null(hucreler[1].QuerySelector("svg"));           // hassas = false
        Assert.DoesNotContain("🔐", ekran.Markup);
    }

    [Fact]
    public void Eylem_adlari_Turkcelestirilir()
    {
        var ekran = Ciz();
        Assert.Contains("Görüntüleme", ekran.Markup);   // OKUMA
        Assert.Contains("Ekleme", ekran.Markup);        // EKLEME
        Assert.DoesNotContain(">OKUMA<", ekran.Markup);
    }

    [Fact]
    public void Ekranda_silme_veya_duzenleme_dugmesi_YOKTUR()
    {
        // Mehmet'in koşulu: denetim izinde silme ucu olmayacak. Ekranda da olmamalı.
        var ekran = Ciz();
        var dugmeler = ekran.FindAll("button").Select(b => b.TextContent.ToLowerInvariant());

        Assert.DoesNotContain(dugmeler, d => d.Contains("sil") || d.Contains("düzenle"));
    }

    [Fact]
    public void Append_only_notu_ekranda_gorunur()
    {
        // KESIF-P3 tasarım hakemi tur 2 (01.10): kural cümlesi TEK kez — PRO'da değişmez kutusunun
        // gerekçesi ("yalnız eklenir"); ayrıca "append-only" satırı ve "Düzeltme şöyle yapılır"
        // tekrarı YOK (aynı cümle üç kez yazılıyordu; "append-only" jargonu da kalktı).
        var e = CizSuzgecAcik();
        Assert.Contains("yalnız eklenir", e.Find("#degismez-ipucu-DenetimKaydi").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("append-only", e.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(e.FindAll("#degismez-ipucu-DenetimKaydi .degismez-alternatif"));
    }

    [Fact]
    public void Varlik_filtresi_sunucudan_gelen_turlerle_dolar()
    {
        // Sabit liste kodda olmamalı — özet ucundan gelir.
        var secenekler = SeciciYardim.Secenekler(CizSuzgecAcik(), "f-varlik")
            .Select(o => o.Ad()).ToList();

        Assert.Contains("Cari", secenekler);
        Assert.Contains("Kasa", secenekler);
    }

    // ── Paket 48 Faz A — bağlam damgası ──────────────────────────────────────

    [Fact]
    public void Tabloda_Sube_ve_Kasa_sutunlari_vardir()
    {
        var basliklar = Ciz().FindAll("thead th").Select(t => t.TextContent.Trim()).ToList();
        Assert.Contains("Şube", basliklar);
        Assert.Contains("Kasa", basliklar);
    }

    [Fact]
    public void Damgasiz_satir_tire_gosterir_sube_UYDURULMAZ()
    {
        var ekran = Ciz();
        var satirlar = ekran.FindAll("tbody tr");

        // 1. satır damgalı: şube ve kasa adı yazılı.
        Assert.Contains("Merkez", satirlar[0].TextContent);
        Assert.Contains("Ana Kasa", satirlar[0].TextContent);

        // 2. satır DAMGASIZ: "—" çıkar; hiçbir şube adı yazılmaz.
        var hucreler = satirlar[1].QuerySelectorAll("td").Select(t => t.TextContent.Trim()).ToList();
        Assert.Equal("—", hucreler[4]);   // şube
        Assert.Equal("—", hucreler[5]);   // kasa
        Assert.DoesNotContain("Merkez", satirlar[1].TextContent);
    }

    [Fact]
    public void Sube_secilince_istek_sorgusuna_subeId_eklenir()
    {
        var ekran = CizSuzgecAcik();
        SahteApi.Istekler.Clear();

        SeciciYardim.Sec(ekran, "f-sube", "s2");

        var denetimIstekleri = SahteApi.Istekler
            .Where(u => u.Contains("/denetim?") || u.Contains("/denetim/ozet")).ToList();
        Assert.NotEmpty(denetimIstekleri);
        Assert.Contains(denetimIstekleri, u => u.Contains("subeId=s2"));
    }

    [Fact]
    public void Iki_sube_varsa_secici_GORUNUR_tek_sube_metni_cikmaz()
    {
        // UX-S22'nin diğer yüzü: birden fazla şube varsa seçici gizlenmez.
        var ekran = CizSuzgecAcik();
        Assert.NotEmpty(ekran.FindAll("#f-sube"));
        Assert.Empty(ekran.FindAll("#f-sube-tek"));
    }

    [Fact]
    public void CJ_06_Cok_subede_Sube_alaninda_TEK_etiket_var_kutu_asagi_itilmez()
    {
        // PAKET CUMA-J (K7, 1440 px ölçümü: Şube kutusu altı ±15 px): Secici kendi
        // etiketini çizer; dıştaki ikinci "Şube" etiketi kutuyu bir satır aşağı itiyordu.
        var ekran = CizSuzgecAcik();
        var alan = ekran.Find("#f-sube").Closest("div.alan")!;
        var etiketler = alan.QuerySelectorAll("label").ToList();
        Assert.Single(etiketler);
        Assert.Equal(Kuyumcu.Core.I18n.Sozlukler.Tr.Denetim.Sube, etiketler[0].TextContent.Trim());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// UYUM VE GİDER PUSULASI EKRANLARI — PLAN-2 Adım 1.
//
// BU TESTLER NEDEN ZORUNLU: Blazor, eşleşmeyen bileşen parametresini DERLEMEDE
// değil ÇALIŞMA ANINDA patlatır. 2026-09-13'te bu gerçekten yaşandı: `dotnet
// build` 0 hata verirken 8 bUnit testi kırmızıya döndü. Bu üç ekran da aynı
// riski taşıyor — derleme temiz olması hiçbir şey kanıtlamıyor.
//
// Ayrıca Mehmet'in PLAN-2 Adım 1 koşulları burada bekçileniyor:
//   • listede kimlik MASKELİ  • prefetch YASAK  • silme/düzenleme düğmesi yok
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Sahte API — gerçek ağ yok, sözleşme sabit JSON ile karşılanır.</summary>
public abstract class UyumEkranTemeli : BunitContext
{
    protected readonly List<string> CagrilanYollar = [];

    protected UyumEkranTemeli()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteUyumApi(CagrilanYollar))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
    }

    private sealed class SahteUyumApi(List<string> kayit) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            kayit.Add(yol);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(Govde(yol), System.Text.Encoding.UTF8, "application/json"),
            });
        }

        private static string Govde(string yol) => yol switch
        {
            var y when y.EndsWith("/masak/ozet") => """
                {"acikBildirim":2,"suresiGecmis":1,"yaklasan":1,"son30GunTespit":7}
                """,
            var y when y.EndsWith("/masak/kimlik-tespitleri") => """
                [{"id":"t1","hareketId":"h1","cariId":null,"islemTutariTl":250000.00,
                  "belgeTuru":"TC_KIMLIK","belgeNo":"*******0146","adSoyad":"Ayşe Demir",
                  "tarih":"2026-09-10T09:00:00Z","saklamaBitis":"2034-09-10T09:00:00Z",
                  "tespitEdenKullaniciId":"u1"}]
                """,
            var y when y.Contains("/masak/kimlik-tespitleri/") => """
                {"id":"t1","hareketId":"h1","cariId":null,"islemTutariTl":250000.00,
                 "belgeTuru":"TC_KIMLIK","belgeNo":"10000000146","adSoyad":"Ayşe Demir",
                 "dogumTarihi":"1985-04-12","uyruk":"TUR","adres":"Ulus, Ankara",
                 "tespitEdenKullaniciId":"u1","tarih":"2026-09-10T09:00:00Z",
                 "saklamaBitis":"2034-09-10T09:00:00Z","duzeltilenId":null,"beyan":null}
                """,
            var y when y.EndsWith("/masak/supheli") => """
                [{"id":"s1","hareketId":null,"cariId":null,"kimlikTespitiId":null,
                  "islemTutariTl":500000.00,"gerekce":"Olağandışı nakit yoğunluğu gözlendi",
                  "tespitTarihi":"2026-09-01T10:00:00Z","sonBildirimTarihi":"2026-09-12T10:00:00Z",
                  "kalanSaat":-24.0,"durum":"ACIK","gonderimTarihi":null,"gonderimReferansi":null,
                  "kapatmaGerekcesi":null,"bildirenKullaniciId":"u1",
                  "saklamaBitis":"2034-09-01T10:00:00Z","olusturuldu":"2026-09-01T10:00:00Z",
                  "guncellendi":"2026-09-01T10:00:00Z"}]
                """,
            // KESIF-P6: YASAL-3'ün saklama bekletme paneli bu uca gerçek şekil ister ('{}' → NRE).
            var y when y.EndsWith("/masak/saklama-bekletmeleri") =>
                """{"kuru":true,"bekletmeler":[],"maskelemeler":[]}""",
            var y when y.EndsWith("/masak/saklama-onizleme") => """
                {"simdi":"2026-09-13T20:00:00Z",
                 "suresiDolmus":{"kimlikTespiti":3,"beyan":1},
                 "enYakinSaklamaBitisi":"2026-08-01T00:00:00Z",
                 "aciklama":"4 kayıtta kimlik alanları maskelenecek; tutar, tarih ve kademe korunacak."}
                """,
            var y when y.EndsWith("/gider-pusulalari/sure-takibi") => """
                {"sure":{"gun":7,"dayanak":"VUK md.234"},"gecikmisSayisi":1,"yaklasanSayisi":0,
                 "gecikmis":[],"yaklasan":[],"uyarilar":["1 gider pusulasının 7 günlük düzenleme süresi DOLMUŞ"]}
                """,
            var y when y.EndsWith("/gider-pusulalari") => """
                [{"id":"p1","pusulaNo":"GP-2026-000001","hareketId":"h1","saticiTipi":"NIHAI_TUKETICI",
                  "saticiAdSoyad":"Ayşe Demir","saticiTckn":"*******0146","ayarId":"K22",
                  "gramAgirlik":10.000,"hasGrami":9.160,"brutTutar":62288.00,"stopajOran":0.00,
                  "stopajTutar":0.00,"netOdenen":62288.00,"teslimTarihi":"2026-09-12T00:00:00Z",
                  "duzenlemeSonTarihi":"2026-09-19T00:00:00Z","gecikti":false,"kalanGun":6}]
                """,
            var y when y.EndsWith("/belge") => """
                {"belgeTuru":"GİDER PUSULASI","dayanak":"213 sayılı VUK md.234","pusulaNo":"GP-2026-000001",
                 "duzenlemeTarihi":"2026-09-12T10:00:00Z","teslimTarihi":"2026-09-12T00:00:00Z",
                 "duzenlemeSonTarihi":"2026-09-19T00:00:00Z","gecikti":false,
                 "duzenleyen":{"unvan":"Demo Kuyumculuk","sube":"Merkez"},
                 "satici":{"saticiTipi":"NIHAI_TUKETICI","saticiTipiAciklama":"Vergi mükellefi olmayan (zati eşya satışı)",
                           "adSoyad":"Ayşe Demir","tckn":"10000000146","adres":"Ulus, Ankara"},
                 "mal":{"cins":"Hurda altın (K22 — 916/1000)","gramAgirlik":10.000,"hasGrami":9.160,"aciklama":null},
                 "tutarlar":{"brutTutar":62288.00,"stopajOran":0.00,"stopajOranYuzde":"%0",
                             "stopajTutar":0.00,"netOdenen":62288.00,
                             "yaziyla":"ALTMIŞİKİBİNİKİYÜZSEKSENSEKİZ TL"},
                 "not":"Vergi mükellefi olmayan kişiden alımda gelir vergisi tevkifatı yapılmamıştır."}
                """,
            var y when y.Contains("/gider-pusulalari/") => """
                {"id":"p1","pusulaNo":"GP-2026-000001","hareketId":"h1","subeId":"sb1","cariId":null,
                 "saticiTipi":"NIHAI_TUKETICI","saticiAdSoyad":"Ayşe Demir","saticiTckn":"10000000146",
                 "saticiAdres":"Ulus, Ankara","ayarId":"K22","gramAgirlik":10.000,"hasGrami":9.160,
                 "malAciklamasi":null,"brutTutar":62288.00,"stopajOran":0.00,"stopajTutar":0.00,
                 "netOdenen":62288.00,"teslimTarihi":"2026-09-12T00:00:00Z",
                 "duzenlemeSonTarihi":"2026-09-19T00:00:00Z","duzeltilenId":null,
                 "olusturuldu":"2026-09-12T10:00:00Z"}
                """,
                // P-10: Urunler ekrani parite kodlarini TEK KAYNAKTAN okur (UX-S31).
                var p10p when p10p.Contains("/kur/pariteler") =>
                    """{"kaynak":"TEST","pariteler":[{"kod":"BILEZIK_22","ad":"22 Ayar Bilezik","alis":6800,"satis":6900,"birim":"GRAM"}]}""",
            _ => "{}",
        };
    }
}

public sealed class UyumEkranTestleri : UyumEkranTemeli
{
    private IRenderedComponent<Kuyumcu.Web.Pages.Uyum> Ciz() => Render<Kuyumcu.Web.Pages.Uyum>();

    [Fact]
    public void Sayfa_hata_vermeden_cizilir()
    {
        // Asıl değer burada: eşleşmeyen bileşen parametresi olsaydı patlardı.
        var e = Ciz();
        Assert.Contains("MASAK", e.Markup);
        Assert.NotEmpty(e.FindAll("[role=tablist] [role=tab]"));
    }

    [Fact]
    public void Ozet_kartlari_sunucudan_gelen_sayilari_gosterir()
    {
        var kartlar = Ciz().FindAll(".stat-deger").Select(x => x.TextContent.Trim()).ToList();
        Assert.Contains("2", kartlar);   // açık bildirim
        Assert.Contains("7", kartlar);   // son 30 gün tespit
    }

    [Fact]
    public void Kimlik_tespitleri_listesinde_belge_no_MASKELIDIR()
    {
        // Mehmet'in koşulu: toplu tam kimlik gösteren ekran YASAK.
        var m = Ciz().Markup;
        Assert.Contains("*******0146", m);
        Assert.DoesNotContain("10000000146", m);
    }

    [Fact]
    public void Liste_acilirken_tekil_detay_ucu_CAGRILMAZ()
    {
        // PREFETCH YASAK: tekil uç okuma izi düşürür (KVKK md.12). Sayfa
        // açılışında 50 detay çekmek izi gürültüye boğar.
        Ciz();
        Assert.DoesNotContain(CagrilanYollar, y => y.Contains("/kimlik-tespitleri/"));
    }

    [Fact]
    public void Detay_YALNIZ_tiklayinca_cekilir_ve_tam_kimlik_gosterir()
    {
        var e = Ciz();
        e.FindAll("tbody tr td:last-child button").First().Click();

        Assert.Contains(CagrilanYollar, y => y.Contains("/kimlik-tespitleri/t1"));
        Assert.Contains("10000000146", e.Markup);          // modalda TAM değer
        Assert.Contains("KVKK", e.Markup);                  // okuma izi uyarısı
    }

    [Fact]
    public void Suresi_gecmis_bildirim_kirmizi_isaretlenir()
    {
        var e = Ciz();
        e.FindAll("[role=tablist] [role=tab]")[1].Click();

        Assert.Contains("satir-kirmizi", e.Markup);
        Assert.Contains("SÜRE DOLDU", e.Markup);
    }

    [Fact]
    public void MASAK_gonderiminin_ELLE_oldugu_ekranda_yazar()
    {
        // Ürün MASAK'a otomatik göndermiyor; bu kullanıcıdan gizlenmez.
        var e = Ciz();
        e.FindAll("[role=tablist] [role=tab]")[1].Click();
        Assert.Contains("elle", e.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Saklama_sekmesinde_onay_kutusu_isaretlenmeden_dugme_KAPALI()
    {
        // ADR-0008: geri alınamaz işlem, açık onay olmadan çalıştırılamaz.
        // KESIF-P6 (#72 alt): saklama sekmesi yalnız SAHİP'te doğar (uçları [RolGerekli("SAHIP")]).
        SahipOturumu();
        var e = Ciz();
        e.Find("#uy-sekme-saklama").Click();

        var dugme = e.Find("button.dugme-tehlike");
        Assert.True(dugme.HasAttribute("disabled"));

        e.Find("input[type=checkbox]").Change(true);
        Assert.False(e.Find("button.dugme-tehlike").HasAttribute("disabled"));
    }

    [Fact]
    public void Saklama_sekmesi_geri_alinamaz_uyarisi_gosterir()
    {
        SahipOturumu();   // KESIF-P6: sekme yalnız SAHİP'te
        var e = Ciz();
        e.Find("#uy-sekme-saklama").Click();
        Assert.Contains("geri alınamaz", e.Markup, StringComparison.OrdinalIgnoreCase);
    }

    private void SahipOturumu() =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));

    [Fact]
    public void Bildirim_formunda_300_karakterden_kisa_gerekce_KAYDEDILEMEZ()
    {
        // MASAK Rehber 2.0 §4.2.7 — sunucu da bu sınırı uygular; ekran
        // kullanıcıyı önceden uyarır ama son sözü sunucu söyler.
        var e = Ciz();
        e.FindAll("[role=tablist] [role=tab]")[1].Click();
        e.Find("button.dugme-birincil").Click();

        e.Find("#sb-tutar").Input("500000");
        e.Find("#sb-gerekce").Input(new string('a', 299));
        Assert.True(e.Find(".modal-govde button.dugme-birincil").HasAttribute("disabled"));

        e.Find("#sb-gerekce").Input(new string('a', 300));
        Assert.False(e.Find(".modal-govde button.dugme-birincil").HasAttribute("disabled"));
    }
}

public sealed class GiderPusulasiEkranTestleri : UyumEkranTemeli
{
    private IRenderedComponent<Kuyumcu.Web.Pages.GiderPusulalari> Ciz()
        => Render<Kuyumcu.Web.Pages.GiderPusulalari>();

    [Fact]
    public void Sayfa_hata_vermeden_cizilir_ve_pusulayi_listeler()
    {
        var e = Ciz();
        Assert.Contains("GP-2026-000001", e.Markup);
        Assert.Single(e.FindAll("tbody tr"));
    }

    [Fact]
    public void Listede_TCKN_MASKELIDIR()
    {
        var m = Ciz().Markup;
        Assert.Contains("*******0146", m);
        Assert.DoesNotContain("10000000146", m);
    }

    [Fact]
    public void Liste_acilirken_tekil_pusula_ucu_CAGRILMAZ()
    {
        Ciz();
        Assert.DoesNotContain(CagrilanYollar, y => y.EndsWith("/gider-pusulalari/p1"));
    }

    [Fact]
    public void Sure_uyarisi_gosterilir()
    {
        Assert.Contains("süresi DOLMUŞ", Ciz().Markup);
    }

    [Fact]
    public void Detay_tiklayinca_tam_TCKN_gosterir()
    {
        var e = Ciz();
        e.FindAll("tbody tr td:last-child button").First().Click();

        Assert.Contains(CagrilanYollar, y => y.EndsWith("/gider-pusulalari/p1"));
        Assert.Contains("10000000146", e.Markup);
    }

    [Fact]
    public void Ekranda_SILME_veya_DUZENLEME_dugmesi_YOKTUR()
    {
        // Gider pusulası SİLİNMEZ (vergi belgesi). P-55: satırdaki "Düzenle" eylemi
        // ARTIK VARDIR (yalnız satıcı adresi + mal açıklaması — sunucu kilidi
        // HurdaServisi.Guncelle); başka hiçbir "düzenle" düğmesi olamaz.
        var dugmeler = Ciz().FindAll("button")
            .Select(b => (Metin: b.TextContent.ToLowerInvariant(), Id: b.GetAttribute("id") ?? ""));
        Assert.DoesNotContain(dugmeler, d => d.Metin.Contains("sil"));
        // PAKET GOC-A: ortak ızgaranın SIRALAMA düğmeleri sütun başlığını taşır ("Son düzenleme tarihi") — eylem değildir.
        Assert.DoesNotContain(dugmeler, d => d.Metin.Contains("düzenle") && !d.Id.StartsWith("gp-duzenle-") && !d.Id.Contains("-sirala-"));
    }
}

public sealed class PusulaBelgesiEkranTestleri : UyumEkranTemeli
{
    private IRenderedComponent<Kuyumcu.Web.Pages.PusulaBelgesi> Ciz()
        => Render<Kuyumcu.Web.Pages.PusulaBelgesi>(p => p.Add(c => c.Id, "p1"));

    [Fact]
    public void Belge_VUK_md234_alanlarini_icerir()
    {
        var m = Ciz().Markup;

        Assert.Contains("GİDER PUSULASI", m);
        Assert.Contains("213 sayılı VUK md.234", m);
        Assert.Contains("GP-2026-000001", m);
        Assert.Contains("Demo Kuyumculuk", m);        // düzenleyen
        Assert.Contains("Ayşe Demir", m);             // satıcı
        Assert.Contains("10000000146", m);            // belgede TAM TCKN olmalı
        Assert.Contains("Hurda altın", m);            // malın cinsi
    }

    [Fact]
    public void Belgede_tutar_YAZIYLA_yer_alir()
    {
        // VUK md.234 tutarın yazıyla yazılmasını ister.
        Assert.Contains("ALTMIŞİKİBİNİKİYÜZSEKSENSEKİZ TL", Ciz().Markup);
    }

    [Fact]
    public void Belgede_imza_alanlari_bulunur()
    {
        var e = Ciz();
        Assert.Equal(2, e.FindAll(".imza-cizgi").Count);
    }

    [Fact]
    public void Yazdirma_dugmeleri_kagida_CIKMAZ()
    {
        // `yazdirma-gizle` sınıfı @media print ile display:none yapılır.
        var e = Ciz();
        var arac = e.Find(".belge-arac");
        Assert.Contains("yazdirma-gizle", arac.ClassName);
    }

    [Fact]
    public void Stopajsiz_belgede_dogru_not_yazar()
    {
        Assert.Contains("tevkifatı yapılmamıştır", Ciz().Markup);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// ÜRÜN EKRANI — SENTETİK TAŞ İBARESİ (PLAN-2 Adım 3)
//
// md.11/(e) ibarenin ETİKET, FATURA ve İNTERNET SAYFASINDA görünmesini ister.
// Ürün listesi işletmenin vitrin/internet görünümüdür — ibare orada da görünür
// olmak zorundadır. Bu test o görünürlüğü sabitler.
//
// NOT: UI'daki gizleme bir güvenlik önlemi DEĞİLDİR; sunucu kapısı (TasKapisi)
// her hâlükârda doğrular. Burada sınanan şey yalnızca GÖRÜNÜRLÜK.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UrunEkraniTasIbaresiTestleri : BunitContext
{
    private static readonly string Urunler = """
        [{"id":"u1","tip":"ZIYNET","kod":"Y-1","ad":"Safir yüzük","ayarId":"K14",
          "gramAgirlik":4.0,"tasAgirlik":0.4,"hasGrami":2.3,"barkod":null,"aktif":true,
          "tasKokeni":"SENTETIK","tasIbaresi":null},
         {"id":"u2","tip":"ZIYNET","kod":"Y-2","ad":"Pırlanta yüzük","ayarId":"K14",
          "gramAgirlik":4.0,"tasAgirlik":0.4,"hasGrami":2.3,"barkod":null,"aktif":true,
          "tasKokeni":"DOGAL","tasIbaresi":null},
         {"id":"u3","tip":"ZIYNET","kod":"Y-3","ad":"Sade alyans","ayarId":"K22",
          "gramAgirlik":3.0,"tasAgirlik":0.0,"hasGrami":2.75,"barkod":null,"aktif":true,
          "tasKokeni":"TAS_YOK","tasIbaresi":null}]
        """;

    public UrunEkraniTasIbaresiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();   // S3-3: Urunler.razor basit/profesyonel mod okur
        Services.AddScoped(_ => new HttpClient(new SahteUrunApi())
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteUrunApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(Urunler, System.Text.Encoding.UTF8, "application/json"),
            });
    }

    private string Markup() => Render<Kuyumcu.Web.Pages.Urunler>().Markup;

    /// <summary>
    /// Adı geçen ürünün TABLO SATIRI.
    ///
    /// ⚠ Bu testler eskiden ham metinde konum arıyordu ("Pırlanta"nın
    /// indeksinden "alyans"ın indeksine kadar olan parça). Ekrana ürün adlarını
    /// taşıyan bir açılır liste eklendiği anda o konumlar kaydı ve testler
    /// ürün kusuru yokken kırmızıya döndü. Satırı DOM'dan almak hem daha
    /// dar hem daha doğru: rozet gerçekten o ürünün satırında mı, onu sorar.
    /// </summary>
    private static AngleSharp.Dom.IElement Satir(
        IRenderedComponent<Kuyumcu.Web.Pages.Urunler> e, string urunAdi)
        => e.FindAll("tbody tr").First(tr => tr.TextContent.Contains(urunAdi));

    [Fact]
    public void Sentetik_urunun_yaninda_ibare_GORUNUR()
    {
        // Köken SENTETIK, özel ibare yok → varsayılan "sentetik" basılmalı.
        var e = Render<Kuyumcu.Web.Pages.Urunler>();
        var satir = Satir(e, "Safir yüzük");

        Assert.Contains("sentetik", satir.QuerySelector(".rozet-mavi")!.TextContent);
    }

    [Fact]
    public void Dogal_tasli_urunde_ibare_BASILMAZ()
    {
        // Doğal taşa "sentetik" etiketi basmak yönetmeliğe aykırıdır.
        var e = Render<Kuyumcu.Web.Pages.Urunler>();
        var satir = Satir(e, "Pırlanta yüzük");

        Assert.DoesNotContain("sentetik", satir.TextContent);
        Assert.DoesNotContain("laboratuvar üretimi", satir.TextContent);
        Assert.DoesNotContain("yapay üretim", satir.TextContent);
    }

    [Fact]
    public void Tassiz_urunde_ibare_BASILMAZ()
    {
        var e = Render<Kuyumcu.Web.Pages.Urunler>();
        var satir = Satir(e, "Sade alyans");

        Assert.DoesNotContain("sentetik", satir.TextContent);
        Assert.Null(satir.QuerySelector(".rozet-mavi"));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// VERİ DIŞA AKTARIM EKRANI — PLAN-2 Adım 4
//
// Mehmet'in şartları sunucuda zaten kapalı; bu testler ekranın KULLANICIYA
// ne söylediğini sabitler. Bir uyarı metninin sessizce kaldırılması, kodun
// davranışını değiştirmeden güveni bozar — o yüzden testle tutuluyor.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class DisaAktarimEkranTestleri : BunitContext
{
    private const string Kaynaklar = """
        [{"ad":"cari","kisiselVeri":true,"alanlar":["id","ad"]},
         {"ad":"urun","kisiselVeri":false,"alanlar":["id","kod"]}]
        """;

    private const string Gecmis = """
        {"toplam":1,"kayitlar":[{"id":"t1","kullaniciId":"u1",
          "gerekce":"Mali müşavire devir dosyası","kapsam":"cari,urun","maskeli":true,
          "olusturuldu":"2026-09-14T08:00:00Z","sonKullanma":"2026-09-14T08:10:00Z",
          "kullanildi":"2026-09-14T08:01:00Z","satirSayisi":42,"ipAdresi":null}]}
        """;

    public DisaAktarimEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi())
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var govde = istek.RequestUri!.AbsolutePath.Contains("kaynaklar") ? Kaynaklar : Gecmis;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.VeriDisaAktarim> Ciz()
        => Render<Kuyumcu.Web.Pages.VeriDisaAktarim>();

    [Fact]
    public void Gerekce_bos_iken_indirme_dugmesi_KAPALIDIR()
    {
        // Şart 1 sunucuda zorunlu; ekran da kullanıcıyı boşuna göndermez.
        var dugme = Ciz().FindAll("button").First(b => b.TextContent.Contains("Hazırla"));
        Assert.True(dugme.HasAttribute("disabled"));
    }

    [Fact]
    public void Gerekce_yazilinca_dugme_ACILIR()
    {
        var e = Ciz();
        e.Find("#da-gerekce").Input("Mali müşavire yıl sonu devir dosyası hazırlanıyor");

        var dugme = e.FindAll("button").First(b => b.TextContent.Contains("Hazırla"));
        Assert.False(dugme.HasAttribute("disabled"));
    }

    [Fact]
    public void Kisisel_veri_tasiyan_kaynak_ROZETLE_isaretlenir()
    {
        // Kullanıcı neyin kişisel veri olduğunu görmeden seçim yapmamalı.
        Assert.Contains("kişisel veri", Ciz().Markup);
    }

    [Fact]
    public void Tam_kimlik_kutusu_varsayilan_olarak_KAPALIDIR()
    {
        // Şart 2: varsayılan maskeli.
        var e = Ciz();
        var kutu = e.FindAll("input[type=checkbox]")
            .First(i => i.ParentElement!.TextContent.Contains("MASKESİZ"));
        Assert.False(kutu.HasAttribute("checked"));
    }

    [Fact]
    public void Tam_kimlik_secilince_ONAY_METNI_alani_cikar()
    {
        var e = Ciz();
        var kutu = e.FindAll("input[type=checkbox]")
            .First(i => i.ParentElement!.TextContent.Contains("MASKESİZ"));
        kutu.Change(true);

        Assert.Contains("TAM KİMLİK BİLGİSİ DIŞA AKTARILACAKTIR", e.Markup);
        Assert.NotNull(e.Find("#da-onay"));
    }

    [Fact]
    public void Onay_metni_YANLIS_yazilirsa_dugme_kapali_kalir()
    {
        var e = Ciz();
        e.Find("#da-gerekce").Input("Mali müşavire yıl sonu devir dosyası hazırlanıyor");
        e.FindAll("input[type=checkbox]")
            .First(i => i.ParentElement!.TextContent.Contains("MASKESİZ")).Change(true);
        e.Find("#da-onay").Input("tamam");

        var dugme = e.FindAll("button").First(b => b.TextContent.Contains("Hazırla"));
        Assert.True(dugme.HasAttribute("disabled"));
    }

    [Fact]
    public void Gecmis_satir_sayisini_ve_gerekceyi_GOSTERIR()
    {
        // Şart 3'ün kullanıcıya görünen yüzü: aktarımlar gizlenmez.
        var m = Ciz().Markup;
        Assert.Contains("42", m);
        Assert.Contains("Mali müşavire devir dosyası", m);
        Assert.Contains("Maskeli", m);
    }

    [Fact]
    public void KVKK_uyarisi_ekranda_YAZILIDIR()
    {
        var e = Ciz();
        e.FindAll("input[type=checkbox]")
            .First(i => i.ParentElement!.TextContent.Contains("MASKESİZ")).Change(true);
        Assert.Contains("KVKK md.12", e.Markup);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// ÜRÜN ETİKETİ EKRANI — PLAN-2 Adım 5
//
// Etiket iki yasal yükü birden taşır: barkod (okunabilirlik) ve md.11/(e)
// taş ibaresi. İbarenin etikete BASILMASI yönetmelik yükümlülüğüdür —
// ekranda görünüp kâğıtta kaybolması kabul edilemez, o yüzden testle tutulur.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UrunEtiketEkranTestleri : BunitContext
{
    // PAKET 20: sunucu maden/milyem ve rodyum işaretini DE döner (etikete basılır).
    private const string Urunler = """
        [{"id":"u1","tip":"ZIYNET","kod":"Y-1","ad":"Safir yüzük","ayarId":"K14",
          "gramAgirlik":4.0,"tasAgirlik":0.4,"hasGrami":2.3,"barkod":"K0123456789","aktif":true,
          "tasKokeni":"SENTETIK","tasIbaresi":null,
          "milyem":585,"maden":"ALTIN","madenAdi":"Altın","rodyumKaplama":true},
         {"id":"u2","tip":"PLATIN","kod":"Y-2","ad":"Sade alyans","ayarId":"PT950",
          "gramAgirlik":5.0,"tasAgirlik":0.0,"hasGrami":4.75,"barkod":null,"aktif":true,
          "tasKokeni":"TAS_YOK","tasIbaresi":null,
          "milyem":950,"maden":"PLATIN","madenAdi":"Platin","rodyumKaplama":false}]
        """;

    /// <summary>
    /// GET /urunler/madenler — etiket ibare politikası. Rodyum ibaresi
    /// VARSAYILAN KAPALIDIR (ADR-0005: zorunluluğu doğrulanmadı).
    /// </summary>
    private const string MadenTablosu = """
        {"madenler":[],"etiket":{"rodyumIbaresiGoster":false,"rodyumIbaresi":"rodyum kaplama"}}
        """;

    public UrunEtiketEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteUrunApi())
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteUrunApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                // Sahte yanıt GERÇEK şekle uyar: /madenler bir ZARF döner,
                // ürün listesi DİZİ. Tek yanıt verilseydi istemci JsonException
                // ile kırılır ve ekran testleri bunu görmezden gelirdi (S-2 dersi).
                Content = new StringContent(
                    istek.RequestUri!.PathAndQuery.EndsWith("/urunler/madenler") ? MadenTablosu : Urunler,
                    System.Text.Encoding.UTF8, "application/json"),
            });
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.UrunEtiketleri> Ciz()
        => Render<Kuyumcu.Web.Pages.UrunEtiketleri>();

    private static void Sec(IRenderedComponent<Kuyumcu.Web.Pages.UrunEtiketleri> e, string urunAdi)
        => e.FindAll("input[type=checkbox]")
            .First(i => i.GetAttribute("aria-label") == urunAdi).Change(true);

    [Fact]
    public void Secim_yokken_yazdirma_KAPALIDIR()
    {
        var dugme = Ciz().FindAll("button").First(b => b.TextContent.Contains("Yazdır"));
        Assert.True(dugme.HasAttribute("disabled"));
    }

    [Fact]
    public void Secilen_urun_icin_etiket_CIZILIR()
    {
        var e = Ciz();
        Sec(e, "Safir yüzük");

        Assert.Single(e.FindAll(".urun-etiket"));
        Assert.Contains("Safir yüzük", e.Find(".urun-etiket").TextContent);
    }

    [Fact]
    public void Etikette_barkod_SVG_si_bulunur()
    {
        var e = Ciz();
        Sec(e, "Safir yüzük");

        var svg = e.Find(".urun-etiket-barkod svg");
        Assert.Contains("barkod K0123456789", svg.GetAttribute("aria-label"));
        // Çizilen bar sayısı sıfır olmamalı — boş bir SVG "barkod var" yalanıdır.
        Assert.True(e.FindAll(".urun-etiket-barkod svg rect").Count > 5);
    }

    [Fact]
    public void Etikette_barkod_METNI_de_yazar()
    {
        // Okuyucu çalışmazsa personel elle girebilmeli.
        var e = Ciz();
        Sec(e, "Safir yüzük");
        Assert.Contains("K0123456789", e.Find(".urun-etiket-barkod-metin").TextContent);
    }

    [Fact]
    public void SENTETIK_ibaresi_ETIKETE_basilir()
    {
        // md.11/(e): "etiket … açıkça yer vermeksizin satışa sunamaz".
        var e = Ciz();
        Sec(e, "Safir yüzük");
        Assert.Equal("SENTETİK", e.Find(".urun-etiket-ibare").TextContent.Trim());
    }

    [Fact]
    public void Tassiz_urunun_etiketinde_ibare_YOKTUR()
    {
        var e = Ciz();
        Sec(e, "Sade alyans");
        Assert.Empty(e.FindAll(".urun-etiket-ibare"));
    }

    [Fact]
    public void Barkodsuz_urunde_SAHTE_barkod_cizilmez()
    {
        // Boş bir barkod görseli basmak, olmayan bir kodu varmış gibi gösterir.
        var e = Ciz();
        Sec(e, "Sade alyans");
        Assert.Empty(e.FindAll(".urun-etiket-barkod svg"));
        Assert.Contains("barkod yok", e.Find(".urun-etiket-barkod-metin").TextContent);
    }

    [Fact]
    public void Kopya_adedi_kadar_etiket_cikar()
    {
        var e = Ciz();
        e.Find("#et-adet").Input("3");
        Sec(e, "Safir yüzük");
        Assert.Equal(3, e.FindAll(".urun-etiket").Count);
    }

    [Fact]
    public void Arac_cubugu_kagida_CIKMAZ()
    {
        // `yazdirma-gizle` @media print ile display:none olur.
        var e = Ciz();
        Assert.All(e.FindAll(".sayfa-baslik"), d => Assert.Contains("yazdirma-gizle", d.ClassName));
    }
}
