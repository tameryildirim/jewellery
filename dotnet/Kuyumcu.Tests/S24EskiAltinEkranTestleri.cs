using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-4 — KARMA ÖDEMEDE ALTIN BACAĞI: EKRAN (basit mod tezgâh fişi)
// Şartname: KARAR-HAS-DEFTERI-2026-09-23 § 4.5 B4-31..B4-35 · PLAN D3
// ("basit modda karma HER ZAMAN açık").
//
// Sınanan:
//   E-1 basit satışta kalem varken "Eski altın" düğmesi HER ZAMAN var; kalem
//       yokken çizilmez; pro anahtarı (#islem-karma) ikinci kez çizilmez
//   E-2 tek dokunuş: ödeme bölünür + ALTIN bacağı açılır; kasa yok, ayarlar
//       sunucudan istenir, bacak adı taslaktaki "Eski altın"
//   E-3 B4-31 alan sırası ayar → gram → fiyat → has → TL; has ve TL YAZILAMAZ
//   E-4 B4-32 satıcı kimliği hemen görünür; eksikse Kaydet kilitli ve Kaydet'in
//       yanındaki sebep altının İLK eksiğidir (UX-S30)
//   E-5 B4-35 iki belge Kaydet'in yanında KAYITTAN ÖNCE; mükellefte tek belge
//   E-6 B4-33 FARK 0 olmadan kaydedilmez; "kalanı ata" ile 0 → tek POST, iki
//       bacaklı gövde (ALTIN kasasız + NAKIT), pusula no banda yazılır
//   E-7 basit modda tek ödeme ipucu var olan düğmeyi söyler (pro: anahtar)
//   S-1..S-3 saf kurallar (FisAltinBacagi.Ekle / KilitSebebi / BelgeNotu)
// Türkçe JSON gövdesi AYRIŞTIRILARAK sınanır. Eşik/oran sayısı teste gömülmez:
// oranlar sahte sunucunun /islemler/altin-bacagi/ayarlar yanıtından gelir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S24EskiAltinEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private static readonly EskiAltinMetinleri E = EskiAltinSozlugu.Tr;
    private static readonly AltinBacagiMetinleri A = AltinBacagiSozlugu.Tr;

    public S24EskiAltinEkranTestleri()
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

            // ⚠ "{}" varsayılanına bırakılmaz: sunucu sözleşmesiyle aynı alanlar.
            var y = yol switch
            {
                var v when v.Contains("/islemler/altin-bacagi/ayarlar") => """
                    {"stopajOranlari":[
                       {"saticiTipi":"NIHAI_TUKETICI","oran":0,"pusulaGerekir":true},
                       {"saticiTipi":"ESNAF_MUAF","oran":0.02,"pusulaGerekir":true},
                       {"saticiTipi":"MUKELLEF","oran":0,"pusulaGerekir":false}],
                     "alisIskontoOran":null,"saticiAdresiZorunlu":true}
                    """,
                var v when v.EndsWith("/cari/sistem-carisi") =>
                    """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                // ISLEM-BANT-2: has 2.000 — testin eski altın fiyatı (2.000) işlem bandının (%50–%110) İÇİNDE kalsın;
                // 6.800'de 2.000 bant dışıydı ve Kaydet artık bant dışında kilitli (onay yalnız SAHİP/MÜDÜR + gerekçe).
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":2000,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/pos/cihazlar") => "[]",
                var v when v.Contains("/islemler/satis-fisi") => """
                    {"fisId":"f1","fisNo":"FIS-000124","iskontoToplam":0,
                     "altinBacagi":{"hareketId":"h-hurda","netTl":9160,"belgeSayisi":2,
                      "giderPusulasi":{"id":"gp1","pusulaNo":"GP-2026-000077","brutTutar":9160,
                        "stopajOran":0,"stopajTutar":0,"netOdenen":9160}}}
                    """,
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

    private void OturumAc(string mod) =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1", null, mod));

    // TEMA TEMİZLİĞİ 17.10: SATIŞ = Yıldız tezgâhı (BASİT de). Tek kapı "+ Başka ödeme" = tezgâhın hapı
    // (#yildiz-tezgah-baska-odeme → sayfanın OdemeyiBol'u, eski #islem-odeme-bol ile AYNI yöntem); Kaydet
    // #yildiz-tezgah-kaydet, kilit sebebi Fark bloğunun not satırında (#yildiz-tezgah-not) ve title'da.
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran()
    {
        OturumAc(ArayuzModuServisi.Basit);
        return Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
    }

    /// <summary>Basit satış + 10.000 TL'lik bir kalem (elle satırından).</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> KalemliFis()
    {
        var e = Ekran();
        TemaTemizligiGDYardimcisi.MusteriBekle(e, "Peşin Müşteri");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.Equal("10000", SeciciYardim.SeciliDeger(e, "islem-birimfiyat")));
        e.Find("#islem-kalem-ekle").Click();
        TemaTemizligiGDYardimcisi.KalemSayisiBekle(e, 1);
        return e;
    }

    private const string BaskaOdeme = "#yildiz-tezgah-baska-odeme";

    /// <summary>Kalemli fiş + "Eski altın" tek dokunuşu → panel açık.</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> AltinAcik()
    {
        var e = KalemliFis();
        EskiAltinSec(e);
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#altin-ayarlar-yok")));
        return e;
    }

    /// <summary>k2 (tasarım hakemi K19): TEK KAPI — "+ Başka ödeme ekle", açılan satırın ödeme türünden "Eski altın".</summary>
    private static void EskiAltinSec(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        TemaTemizligiGDYardimcisi.KarmaAc(e);
        SeciciYardim.Sec(e, "islem-bacak-1-arac", "ALTIN");
        e.WaitForElement("#altin-bacagi");
    }

    /// <summary>K22 · 5 g · 2.000 TL/has-g → has 4,58 · net 9.160 (nihai tüketici, kesinti 0).</summary>
    private static void AltinDoldur(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, bool saticiyla = true)
    {
        e.Find("#altin-ayar").Click();
        e.Find("#altin-ayar-liste li[data-deger='K22']").Click();
        e.Find("#altin-gram").Input("5");
        e.Find("#altin-fiyat").Input("2000");
        if (!saticiyla) return;
        e.Find("#altin-satici-ad").Input("Ali Veli");
        e.Find("#altin-satici-tckn").Input("10000000146");
        e.Find("#altin-satici-adres").Input("Ulus, Ankara");
    }

    private static bool KaydetKilitli(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) => TemaTemizligiGDYardimcisi.KaydetKilitli(e);

    /// <summary>Kaydet'in kilit cümlesi — Yıldız'da Fark bloğunun not satırı (#yildiz-tezgah-not; title ile AYNI).</summary>
    private static string KilitCumlesi(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        var not = e.Find("#yildiz-tezgah-not").TextContent.Trim();
        Assert.Equal(e.Find(TemaTemizligiGDYardimcisi.Kaydet).GetAttribute("title"), not);
        return not;
    }

    // ── E-1 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S24_Basit_Satista_Eski_Altin_Kalem_Olunca_Hep_Var_Kalemsizken_Cizilmez()
    {
        var e = Ekran();
        // Ödenecek bir şey yokken çizilmez (ilk bakış ≤ 12 düğme, S2-2); "Ödemeyi böl"
        // kilitli ve sebebini yazar — boşa tıklanan düğme doğmaz (DoD EK 1).
        Assert.Empty(e.FindAll("#islem-odeme-altin"));
        Assert.True(e.Find(BaskaOdeme).HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(e.Find(BaskaOdeme).GetAttribute("title")));   // kilit sebebini söyler
        // Aynı soru iki kontrolle sorulmaz (UX-S22): pro anahtarı basit modda yok.
        Assert.Empty(e.FindAll("#islem-karma"));

        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.Equal("10000", SeciciYardim.SeciliDeger(e, "islem-birimfiyat")));
        e.Find("#islem-kalem-ekle").Click();
        // k2 (tasarım hakemi K19, 29.09): ayrı "Eski altın" düğmesi YOK (iki kapı iki sonuç veriyordu) — tek kapı
        // "+ Başka ödeme"; eski altın açılan satırın ödeme türü listesinde, taslaktaki adıyla.
        e.WaitForAssertion(() => Assert.False(e.Find(BaskaOdeme).HasAttribute("disabled")));
        Assert.Empty(e.FindAll("#islem-odeme-altin"));
        Assert.Empty(e.FindAll("#islem-odeme-altin-not"));
        e.Find(BaskaOdeme).Click();
        e.WaitForElement("#islem-bacak-1-arac");
        Assert.Contains(SeciciYardim.Secenekler(e, "islem-bacak-1-arac"),
            o => o.Deger() == "ALTIN" && o.TextContent.Trim() == E.EskiAltin);
    }

    // ── E-2 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S24_Eski_Altin_Tek_Dokunusla_Boler_ve_Altin_Bacagi_Acar_Kasa_Yok()
    {
        var e = KalemliFis();
        EskiAltinSec(e);   // k2: tek kapı

        // Ödeme bölündü: tek ödeme İLK bacak (NAKIT, kasa otomatik), ALTIN ikinci.
        Assert.Equal("NAKIT", SeciciYardim.SeciliDeger(e, "islem-bacak-0-arac"));
        Assert.Equal("ALTIN", SeciciYardim.SeciliDeger(e, "islem-bacak-1-arac"));
        // Bacak adı taslaktaki "Eski altın" (UX-S31).
        Assert.Contains(SeciciYardim.Secenekler(e, "islem-bacak-1-arac"),
            o => o.Deger() == "ALTIN" && o.TextContent.Trim() == E.EskiAltin);
        // B4-5: kasa YOK; B4-31: TL giriş alanı YOK.
        Assert.Empty(e.FindAll("#islem-bacak-1-kasa"));
        Assert.Empty(e.FindAll("#islem-bacak-1-tutar"));
        // Oranlar sunucudan (ADR-0005).
        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "GET" && i.Yol.Contains("/islemler/altin-bacagi/ayarlar")));
        // B4-6: tek altın bacağı → düğme artık çizilmez.
        Assert.Empty(e.FindAll("#islem-odeme-altin"));
    }

    // ── E-3 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S24_Alan_Sirasi_Ayar_Gram_Fiyat_Has_TL_Has_ve_TL_Yazilamaz()
    {
        var e = AltinAcik();
        AltinDoldur(e, saticiyla: false);

        var sira = new[] { "altin-ayar", "altin-gram", "altin-fiyat", "altin-has", "altin-tl" };
        var markup = e.Find("#altin-bacagi").OuterHtml;
        var konum = sira.Select(id => markup.IndexOf($"id=\"{id}\"", StringComparison.Ordinal)).ToList();
        Assert.All(konum, k => Assert.True(k >= 0));
        Assert.Equal(konum.OrderBy(k => k).ToList(), konum);

        // Türetilen has ve TL SALT OKUNUR: içinde ne kendisi ne çocuğu yazılabilir alan.
        foreach (var id in new[] { "altin-has", "altin-tl" })
        {
            var el = e.Find($"#{id}");
            Assert.NotEqual("INPUT", el.TagName);
            Assert.NotEqual("TEXTAREA", el.TagName);
            Assert.Empty(el.QuerySelectorAll("input, textarea, select, [contenteditable]"));
        }
        Assert.Empty(e.FindAll("input#altin-has, textarea#altin-has, input#altin-tl"));
        // B4-34: değerler girdiden TÜRETİLİR (K22 · 5 g · 2.000).
        e.WaitForAssertion(() => Assert.Equal("9160", SeciciYardim.SeciliDeger(e, "altin-tl")));
        Assert.Equal("4.58", e.Find("#altin-has").GetAttribute("data-deger"));
    }

    // ── E-4 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S24_Satici_Kimligi_Hemen_Gorunur_Eksikse_Kaydet_Kilitli_Sebep_Altinin_Ilk_Eksigi()
    {
        var e = AltinAcik();

        // B4-32: bacak açılır açılmaz satıcı alanları görünür.
        Assert.NotNull(e.Find("#altin-satici-ad"));
        Assert.NotNull(e.Find("#altin-satici-tckn"));
        Assert.True(KaydetKilitli(e));
        var ilkEksik = e.Find("#altin-eksikler li").TextContent.Trim();
        Assert.Equal(string.Format(E.KaydetKilit, ilkEksik), KilitCumlesi(e));

        AltinDoldur(e);   // satıcı dahil
        // SATIS-3 (KAYNAK 2 md.5): elle yazılmayan nakit bacağı kalanı OTOMATİK üstlenir (840);
        // fark kilidini sınamak için tezgâhtar nakdi ELLE 10.000 yazar → FARK 9.160 fazla.
        e.WaitForAssertion(() => Assert.Equal("840", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")));
        e.Find("#islem-bacak-0-tutar").Input("10000");
        // SATIS-3 t10 (hakem KRİTİK "fark üç yerde"): fark kilidi eski altının eksiği diye DEĞİL, fişin tek fark cümlesiyle söylenir.
        e.WaitForAssertion(() => Assert.Equal(
            string.Format(Kuyumcu.Core.I18n.Satis3Sozlugu.Tr.FarkFazla, Kuyumcu.Web.Servisler.Bicim.Tl(9160m)), KilitCumlesi(e)));
        Assert.True(KaydetKilitli(e));
    }

    // ── E-5 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S24_Kayittan_Once_Iki_Belge_Kaydet_Yaninda_Mukellefte_Tek_Belge()
    {
        var e = KalemliFis();
        Assert.Empty(e.FindAll("#islem-kaydet-belgeler"));   // altın yokken söylenmez

        EskiAltinSec(e);   // k2: tek kapı
        e.WaitForAssertion(() => Assert.Equal(A.IkiBelge, e.Find("#islem-kaydet-belgeler").TextContent.Trim()));

        // k2: BASİT'te satıcı türü özet satırında ("Satıcı: Vatandaş · Değiştir") — Değiştir ile açılır.
        Assert.Empty(e.FindAll("#altin-satici-tip"));
        e.Find("#altin-satici-degistir").Click();
        SeciciYardim.Sec(e, "altin-satici-tip", AltinBacagiGirisi.Mukellef);
        e.WaitForAssertion(() =>
            Assert.Equal(A.TekBelgeMukellef, e.Find("#islem-kaydet-belgeler").TextContent.Trim()));
    }

    // ── E-6 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S24_Fark_Sifir_Olmadan_Kaydedilmez_Kalani_Ata_ile_Tek_POST_Iki_Bacak_Pusula_Bantta()
    {
        var e = AltinAcik();
        AltinDoldur(e);
        e.WaitForAssertion(() => Assert.Equal("9160", SeciciYardim.SeciliDeger(e, "altin-tl")));
        // SATIS-3 (KAYNAK 2 md.5): nakit kalanı otomatik üstlendi (840); elle 10.000 yazılınca FARK doğar.
        e.WaitForAssertion(() => Assert.Equal("840", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar")));
        e.Find("#islem-bacak-0-tutar").Input("10000");

        // B4-33: FARK ≠ 0 → kilitli, tıklama POST ATMAZ.
        Assert.True(KaydetKilitli(e));
        lock (istekler) istekler.Clear();
        e.Find(TemaTemizligiGDYardimcisi.Kaydet).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST");

        // "Kalanı ata" nakit bacağına 840 yazar (altına yazmaz) → FARK 0 → açık.
        e.Find("#islem-bacak-kalan-ata").Click();
        e.WaitForAssertion(() => Assert.False(KaydetKilitli(e)));
        Assert.Equal("9160", SeciciYardim.SeciliDeger(e, "altin-tl"));

        e.Find(TemaTemizligiGDYardimcisi.Kaydet).Click(); TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);
        e.WaitForAssertion(() =>
        {
            List<(string Metot, string Yol, string Govde)> postlar;
            lock (istekler) postlar = istekler.Where(i => i.Metot == "POST").ToList();
            var istek = Assert.Single(postlar);
            Assert.EndsWith("/islemler/satis-fisi", istek.Yol);
            var odemeler = JsonDocument.Parse(istek.Govde).RootElement.GetProperty("odemeler");
            Assert.Equal(2, odemeler.GetArrayLength());
            var nakit = odemeler[0];
            Assert.Equal("NAKIT", nakit.GetProperty("arac").GetString());
            Assert.Equal(840m, nakit.GetProperty("tutarTl").GetDecimal());
            Assert.Equal("ks1", nakit.GetProperty("kasaId").GetString());
            var altin = odemeler[1];
            Assert.Equal("ALTIN", altin.GetProperty("arac").GetString());
            Assert.Equal(9160m, altin.GetProperty("tutarTl").GetDecimal());
            Assert.Equal("K22", altin.GetProperty("ayarId").GetString());
            Assert.Equal(5m, altin.GetProperty("gramAgirlik").GetDecimal());
            Assert.Equal("Ali Veli", altin.GetProperty("satici").GetProperty("adSoyad").GetString());
            Assert.False(altin.TryGetProperty("kasaId", out var k) && k.ValueKind != JsonValueKind.Null);
        });
        // UX-S27: yanıt okunur — gider pusulası numarası banda yazılır.
        e.WaitForAssertion(() => Assert.Contains("GP-2026-000077", e.Markup));
    }

    // ── E-7 ───────────────────────────────────────────────────────────────
    [Fact]
    public void S24_Tek_Odeme_Ipucu_Basit_Modda_Eski_Altin_Dugmesini_Soyler()
    {
        OturumAc(ArayuzModuServisi.Basit);
        var basit = Render<FisOdemeKasa>(p => p
            .Add(x => x.Tur, "SATIS")
            .Add(x => x.OdemeKapisiAcik, true)   // yasal kapı: seçici basit modda da açık
            .Add(x => x.AnaKasalar, new List<KasaVeri>()));
        // k2: ipucu ayrı düğmeyi DEĞİL tek kapıyı söyler ("Başka ödeme ekle" → ödeme türü "Eski altın").
        Assert.Equal(string.Format(Kuyumcu.Core.I18n.Satis3Sozlugu.Tr.EskiAltinYolu,
                SatisSadeSozlugu.Tr.BaskaOdemeEkle.TrimStart('+', ' '), E.EskiAltin),
            basit.Find("#islem-odeme-altin-ipucu").TextContent.Trim());
    }

    [Fact]
    public void S24_Tek_Odeme_Ipucu_Pro_Modda_Karma_Anahtarini_Soyler()
    {
        OturumAc(ArayuzModuServisi.Profesyonel);
        var pro = Render<FisOdemeKasa>(p => p
            .Add(x => x.Tur, "SATIS")
            .Add(x => x.AnaKasalar, new List<KasaVeri>()));
        var ipucu = pro.Find("#islem-odeme-altin-ipucu").TextContent.Trim();
        Assert.Contains("Karma ödeme", ipucu);
        Assert.Contains(E.EskiAltin, ipucu);
    }
}

/// <summary>PAKET S2-4 — "Eski altın" düğmesinin ve Kaydet notlarının saf kuralları.</summary>
public sealed class S24EskiAltinSafTestleri
{
    [Fact]
    public void S24_Ekle_Tek_Altin_Bacagi_Kasasiz_Dogar_Ikinci_ve_Sinir_Reddedilir()
    {
        var b = new List<FisBacagi> { new() { Arac = "NAKIT", TutarTl = 10_000m, KasaId = "ks1" } };
        Assert.True(FisAltinBacagi.Ekle(b));
        Assert.Equal(2, b.Count);
        Assert.Equal(("ALTIN", "", 0m), (b[1].Arac, b[1].KasaId, b[1].TutarTl));
        Assert.NotNull(b[1].Altin);

        Assert.False(FisAltinBacagi.EklenebilirMi(b));
        Assert.False(FisAltinBacagi.Ekle(b));   // B4-6
        Assert.Equal(2, b.Count);

        var dolu = Enumerable.Range(0, FisAltinBacagi.EnFazlaBacak).Select(_ => new FisBacagi()).ToList();
        Assert.False(FisAltinBacagi.Ekle(dolu));
        Assert.Equal(FisAltinBacagi.EnFazlaBacak, dolu.Count);
    }

    [Fact]
    public void S24_KilitSebebi_Altinin_Ilk_Eksigi_Tamamsa_ya_da_Altin_Yoksa_Null()
    {
        var m = EskiAltinSozlugu.Tr;
        Assert.Null(FisAltinBacagi.KilitSebebi([new FisBacagi { Arac = "NAKIT" }], "tr"));

        var altin = new FisBacagi
        {
            Arac = "ALTIN", Altin = new AltinBacagiGirisi(),
            AltinDurum = new AltinBacagiDurumu(false, ["Gram girin", "Fiyat girin"], 0m, null, null),
        };
        Assert.Equal(string.Format(m.KaydetKilit, "Gram girin"), FisAltinBacagi.KilitSebebi([altin], "tr"));
        Assert.Equal(string.Format(EskiAltinSozlugu.En.KaydetKilit, "Gram girin"), FisAltinBacagi.KilitSebebi([altin], "en"));

        altin.AltinDurum = new AltinBacagiDurumu(true, [], 0m, null, null);
        Assert.Null(FisAltinBacagi.KilitSebebi([altin], "tr"));
    }

    [Fact]
    public void S24_BelgeNotu_Pusula_Gerekirse_Iki_Belge_Mukellefte_Tek_Altin_Yoksa_Null()
    {
        var ayar = new AltinBacagiAyarlari(
            [new(AltinBacagiGirisi.NihaiTuketici, 0m, true), new(AltinBacagiGirisi.Mukellef, 0m, false)],
            null, true);
        var a = AltinBacagiSozlugu.Tr;
        Assert.Null(FisAltinBacagi.BelgeNotu([new FisBacagi { Arac = "NAKIT" }], ayar, "tr"));

        var altin = new FisBacagi { Arac = "ALTIN", Altin = new AltinBacagiGirisi() };
        Assert.Equal(a.IkiBelge, FisAltinBacagi.BelgeNotu([altin], ayar, "tr"));
        altin.Altin.SaticiTipi = AltinBacagiGirisi.Mukellef;
        Assert.Equal(a.TekBelgeMukellef, FisAltinBacagi.BelgeNotu([altin], ayar, "tr"));
        // Ayarlar yüklenemediyse tutucu taraf: pusula GEREKİR sayılır (fail-closed).
        Assert.Equal(a.IkiBelge, FisAltinBacagi.BelgeNotu(
            [new FisBacagi { Arac = "ALTIN", Altin = new AltinBacagiGirisi { SaticiTipi = AltinBacagiGirisi.Mukellef } }],
            null, "tr"));
    }
}
