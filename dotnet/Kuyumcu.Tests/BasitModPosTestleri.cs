using System.Text.Json;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// BASİT MOD — POS AKIŞI (paket UX-3 Faz B)
//
// Sınanan:
//   • basit modda tür düğmeleri 5 (S2-2 ızgarası: SATIS·ALIS·HURDA_ALIS·TAKAS·SATIS_IADE), profesyonelde 8
//   • basit modda karma ödeme / bacak / vade alanları YOKTUR
//   • tek aktif TL kasası varken seçici GİZLİ, gövdede o kasa; İKİ kasa varken
//     seçici GÖRÜNÜR (varsayım yapılmaz — Anayasa md.1)
//   • ⚠ YASAL KAPI 1 (tevsik): sunucu önizlemesi nakdi kapatınca ödeme türü
//     seçicisi basit modda AÇILIR ve uyarı bandı çıkar
//   • ⚠ YASAL KAPI 2 (MASAK): eşik aşılınca kimlik formu basit modda ÇİZİLİR
//   • ⚠ YASAL KAPI 3 (taş kökeni, ADR-0013): istemcide ön-kontrol YOK; sunucunun
//     400'ü olduğu gibi gösterilir VE gizli alanlar açılır
//   • basit modda gizlenen alan POST gövdesine HİÇ KONMAZ (null da gitmez)
//
// ⚠ Eşik SAYISI hiçbir teste gömülmez: bayraklar sahte SUNUCU yanıtından gelir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class BasitModPosTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool nakitKapali;
    private int kasaSayisi = 1;
    private decimal masakEsigi = 185_000m;
    private string? postHatasi;

    public BasitModPosTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(
            new SahteApi(istekler, () => nakitKapali, () => kasaSayisi,
                         () => masakEsigi, () => postHatasi))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    /// <summary>Oturumu verilen ETKİN modda kurar (değeri sunucu çözmüş kabul edilir).</summary>
    private void ModuKur(string etkinMod) =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "TEZGAHTAR", "s1",
                                null, etkinMod));

    private sealed class SahteApi(
        List<(string, string, string)> kayit,
        Func<bool> kapaliMi,
        Func<int> kasaSayisi,
        Func<decimal> masakEsigi,
        Func<string?> postHatasi) : HttpMessageHandler
    {
        private static decimal GereksinimTutari(string yol)
        {
            var m = System.Text.RegularExpressions.Regex.Match(yol, @"tutar=([0-9.]+)");
            return m.Success ? decimal.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0m;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            // ⚠ YASAL KAPI 3 — taş kökeni: istemcide ÖN-KONTROL YOKTUR, kapı
            //   yalnız sunucunun 400'üyle bilinir (ADR-0013 · Aşkın K1).
            if (istek.Method == HttpMethod.Post && postHatasi() is { } mesaj)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                        $$"""{"message":"{{mesaj}}"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };
            }

            var kasalar = kasaSayisi() >= 2
                ? """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},{"id":"ks2","ad":"Vitrin Kasa","paraBirimi":"TRY","aktif":true}]"""
                : """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""";

            var y = yol switch
            {
                var v when v.Contains("/tevsik/gunluk") =>
                    $$"""
                    {"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,
                     "bugunkuNakitToplam":0,"kalanNakitHakki":30000,
                     "islemTutariKuraliDevrede":{{(kapaliMi() ? "true" : "false")}}}
                    """,
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """
                    [{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,
                      "iscilikTl":0,"satisFiyati":10000}]
                    """,
                var v when v.Contains("/kasalar") => kasalar,
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                // SATIS-3 t19: ekran artık gerçek müşteriyle eşik ALTINDA da sorar (bağlantılı işlem kümülatifi sunucuda);
                // gerçek sunucu gibi eşik altı tutara "YOK" döner — her tutara kimlik isteyen taklit gerçekçi değildi.
                var v when v.Contains("/masak/gereksinim") && GereksinimTutari(v) < masakEsigi() =>
                    """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/gereksinim") =>
                    """{"kademe":"YALNIZ_KIMLIK","kimlikGerekli":true,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => $$"""
                    {"kimlikEsigi":{{masakEsigi()}},"amacBeyaniEsigi":{{masakEsigi()}},
                     "beyanFormuEsigi":{{masakEsigi()}},"ekBelgeEsigi":{{masakEsigi()}},
                     "asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}
                    """,
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

    /// <summary>Ekranı verilen modda çizer.</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod)
    {
        ModuKur(mod);
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-tur-SATIS");
        return e;
    }

    /// <summary>Bir kalem ekler (10.000 TL); Yıldız tezgâhında kalem satırı çizilene dek bekler.</summary>
    private static void KalemEkle(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) => e.YildizKalemEkle();

    // TEMA TEMİZLİĞİ 17.10: SATIŞ fişi artık hep Yıldız tezgâhıdır. Müşteri "Değiştir" katmanından seçilir
    // (sahte sunucu sistem carisini vermez → Peşin ön seçimi yok); ödeme türü / kasa / vade alanları (FisOdemeKasa,
    // AYNI id'ler) tek ödeme satırına dokununca açılan ödeme düzenindedir; Kaydet #yildiz-tezgah-kaydet; yasal
    // kapının cümlesi Fark satırının notunda (#yildiz-tezgah-not), sunucu reddi #yildiz-tezgah-hata.
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Satisa(string mod, bool musteri = true)
    {
        var e = Ekran(mod).YildizHazir(null);
        if (musteri) e.YildizMusteriSec("c1");
        return e;
    }
    private static JsonElement Govde(string g) => JsonDocument.Parse(g).RootElement;

    /// <summary>POST eşzamansızdır: satış isteğinin GELMESİNİ bekler ve gövdesini verir.</summary>
    private string Satis(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        e.WaitForAssertion(() => Assert.Contains(
            istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")));
        return istekler.Last(i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis")).Govde;
    }

    // ── Sekmeler ────────────────────────────────────────────────────────────

    // PAKET S2-2 (PLAN-SADELESTIRME § S-2 madde 2.2): basit moddaki tür listesi
    // UX-3'ün 3 sekmesinden 5 türlük DÜĞME IZGARASINA genişledi (SATIS, ALIS,
    // HURDA_ALIS, TAKAS, SATIS_IADE). Eski iddia (3 sekme; TAKAS/HURDA yok)
    // spesifikasyonla değişti; ISCILIK/TRANSFER/ALIS_IADE hâlâ basitte YOK.
    [Fact]
    // PAKET T-S1 (tuval Tezgah.dc.html): ALIS'in karosu tuvalde YOK → PRO'ya taşındı
    // (özellik silinmedi; aşağıdaki PRO testi 8 sekmeyi aynen bulur). 5 → 4.
    public void Basit_modda_izgarada_dort_tur_ALIS_PROda()
    {
        var e = Ekran(ArayuzModuServisi.Basit);

        var sekmeler = e.FindAll("[id^=islem-tur-]");
        Assert.Equal(4, sekmeler.Count);
        Assert.NotNull(e.Find("#islem-tur-SATIS"));
        Assert.Empty(e.FindAll("#islem-tur-ALIS"));
        Assert.NotNull(e.Find("#islem-tur-SATIS_IADE"));
        Assert.NotNull(e.Find("#islem-tur-TAKAS"));
        Assert.NotNull(e.Find("#islem-tur-HURDA_ALIS"));
        Assert.Empty(e.FindAll("#islem-tur-ISCILIK"));
        Assert.Empty(e.FindAll("#islem-tur-TRANSFER"));
        Assert.Empty(e.FindAll("#islem-tur-ALIS_IADE"));
    }

    [Fact]
    public void Profesyonel_modda_sekiz_sekme_MEVCUT_DAVRANIS()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);

        Assert.Equal(8, e.FindAll("[id^=islem-tur-]").Count);
        Assert.NotNull(e.Find("#islem-tur-TAKAS"));
    }

    [Fact]
    // PAKET T-S1: tuvalde (Tezgah.dc.html) adım şeridi YOK → basit modda da çizilmez;
    // yasal kapı bandı (#islem-yasal-kapi) aşağıdaki testlerde AYNEN sınanır.
    public void Adim_seridi_tuvalde_yok_basitte_de_profesyonelde_de_CIZILMEZ()
    {
        Assert.Empty(Ekran(ArayuzModuServisi.Basit).FindAll("#islem-adim-serit"));

        using var baglam = new BasitModPosTestleri();
        Assert.Empty(baglam.Ekran(ArayuzModuServisi.Profesyonel).FindAll("#islem-adim-serit"));
    }

    // ── Gizlenen profesyonel alanlar ────────────────────────────────────────

    [Fact]
    public void Basit_modda_karma_odeme_ve_bacak_YOKTUR()
    {
        var e = Ekran(ArayuzModuServisi.Basit);

        Assert.Empty(e.FindAll("#islem-karma"));
        Assert.Empty(e.FindAll("#islem-bacak-ekle"));
    }

    [Fact]
    public void Basit_modda_vade_alani_YOKTUR()
    {
        var e = Satisa(ArayuzModuServisi.Basit);
        KalemEkle(e);
        e.YildizOdemeAc();   // ödeme alanlarının çizildiği yer — boş DOM'da yanlış yeşil olmasın

        Assert.NotEmpty(e.FindAll("#yildiz-tezgah-odeme-duzen #islem-odeme-sade, #yildiz-tezgah-odeme-duzen #islem-aldim-odeme"));
        Assert.Empty(e.FindAll("#islem-vade"));
    }

    [Fact]
    public void Basit_modda_odeme_turu_SORULMAZ_sade_satir_yazilir()
    {
        // PARA-DEMO-2: tezgâhın tek ödeme satırı Nakit/Kart/Havale seçtirir (#islem-aldim-odeme); tam ödeme türü
        // listesi (#islem-odeme — veresiye/döviz…) BASİT'te sorulmaz, satır seçilen türü yazar.
        var e = Satisa(ArayuzModuServisi.Basit, musteri: false);
        Assert.StartsWith("Nakit", e.Find("#yildiz-odeme-0 .yz-tezgah-arac").TextContent.Trim());
        e.YildizOdemeAc();

        Assert.Empty(e.FindAll("#islem-odeme"));
        Assert.Contains("Nakit", e.Find("#islem-odeme-sade").TextContent);
    }

    // ── Kasa: tek kasa koşuldur, tespit değil ───────────────────────────────

    [Fact]
    public void Tek_kasa_varken_secici_GIZLI_ve_govdeye_o_kasa_konur()
    {
        kasaSayisi = 1;
        var e = Satisa(ArayuzModuServisi.Basit);
        KalemEkle(e);
        e.YildizOdemeAc();
        istekler.Clear();

        Assert.Empty(e.FindAll("#islem-kasa"));
        Assert.NotNull(e.Find("#islem-kasa-tek"));
        Assert.Contains("Merkez Kasa", e.Find("#yildiz-odeme-0 .yz-tezgah-kasa").TextContent);
        e.YildizKaydet();

        var post = Satis(e);
        Assert.Equal("ks1", Govde(post).GetProperty("kasaId").GetString());
    }

    // SATIS-3 t3 (kurucu KAYNAK 5 "kasa otomatik … yalnız birden çok uygun kasa varsa ve PRO'da değiştirilebilir"; tasarım
    // hakemi t3 YÜKSEK): eski UX-3 kuralı ("iki kasada BASİT'te de seçici") bu kurucu kararıyla DEĞİŞTİ. İki kasada BASİT
    // ilk aktif TL kasasını otomatik yazar (salt okunur, adıyla) ve gövdeye koyar; PRO'da seçici iki seçenekle kalır.
    [Fact]
    public void Iki_kasa_varken_BASITte_ilk_kasa_OTOMATIK_PROda_secici_GORUNUR()
    {
        kasaSayisi = 2;
        var b = Satisa(ArayuzModuServisi.Basit);
        b.YildizOdemeAc();
        b.WaitForAssertion(() => Assert.Contains("Merkez Kasa", b.Find("#islem-kasa-tek").TextContent), TimeSpan.FromSeconds(5));
        Assert.Empty(b.FindAll("#islem-kasa"));
        KalemEkle(b);
        istekler.Clear();
        b.YildizKaydet();
        Assert.Equal("ks1", Govde(Satis(b)).GetProperty("kasaId").GetString());

        var e = Ekran(ArayuzModuServisi.Profesyonel).YildizHazir(null);
        e.YildizOdemeAc();
        Assert.NotNull(e.Find("#islem-kasa"));
        // ⚠ KARARSIZLIK ÖNLEMİ (P-36c/3): kasa listesi SUNUCUDAN asenkron gelir;
        //   seçiciyi veri gelmeden açmak yüklü makinede BOŞ liste okutuyordu
        //   (tam süitte aralıklı kırmızı). Sayım artık veri gelene kadar beklenir;
        //   bu bir ürün hatası değil, testin yarışıydı — gizlenmedi, düzeltildi.
        e.WaitForAssertion(
            () => Assert.Equal(2, SeciciYardim.Secenekler(e, "islem-kasa")
                .Count(o => o.Deger().StartsWith("ks"))),
            TimeSpan.FromSeconds(5));
    }

    // ── ⚠ YASAL KAPI 1 — TEVSİK ─────────────────────────────────────────────

    [Fact]
    public void YASAL_KAPI_tevsik_basit_modda_odeme_turunu_ACAR()
    {
        nakitKapali = true;
        var e = Satisa(ArayuzModuServisi.Basit);       // müşteri seçimi → TevsikOnizle
        e.YildizOdemeAc();
        Assert.Empty(e.FindAll("#islem-odeme"));   // kapı kapalıyken gizli (ödeme düzeni AÇIK — boş DOM değil)
        e.Find("#yildiz-tezgah-odeme-tamam").Click();

        KalemEkle(e);
        e.YildizOdemeAc();
        e.WaitForElement("#islem-odeme");
        Satis4Yardim.NakitElleSec(e);   // SATIS-4 (a): eşik üstünde varsayılan artık nakit dışı — bu test nakdi ELLE seçen kullanıcıyı sınar

        Assert.NotNull(e.Find("#islem-odeme"));            // seçici AÇILDI
        // bant çıktı: Yıldız'da özgün bandın (#islem-yasal-kapi) cümlesi Fark satırında, kötü durum renginde
        e.WaitForAssertion(() => Assert.Contains("yz-kotu", e.Find("#yildiz-tezgah-not").ClassList));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#yildiz-tezgah-not").TextContent));
    }

    // ── ⚠ YASAL KAPI 2 — MASAK ──────────────────────────────────────────────

    [Fact]
    public void YASAL_KAPI_masak_basit_modda_kimlik_formunu_CIZER()
    {
        masakEsigi = 5_000m;                       // eşik SUNUCUDAN gelir
        var e = Satisa(ArayuzModuServisi.Basit);
        Assert.Empty(e.FindAll("#masak-belge-no, #yildiz-tezgah-kimlik"));

        KalemEkle(e);
        // Yıldız: kimlik formu "+ Kimlik bilgisi" çekmecesinde (aynı MasakFormu, aynı kapı)
        e.WaitForElement("#yildiz-tezgah-kimlik", TimeSpan.FromSeconds(5)).Click();
        e.WaitForElement("#cekmece-yildiz-kimlik #masak-belge-no");

        Assert.NotNull(e.Find("#masak-belge-no"));
        Assert.True(e.YildizKaydetKilitli());
        // YASAL-KAPI-3 tur 2: form açıkken genel "gereken alanlar açıldı" cümlesi yazılmaz — kilit eksik alanı söyler (uyarı tek kez)
        Assert.NotEqual(Kuyumcu.Core.I18n.Sozlukler.Tr.Arayuz.YasalKapiAcildi, e.Find("#yildiz-tezgah-not").TextContent.Trim());
    }

    // ── ⚠ YASAL KAPI 3 — TAŞ KÖKENİ (yalnız sunucuda) ───────────────────────

    [Fact]
    public void YASAL_KAPI_tas_kokeni_400u_basit_modda_GOSTERILIR_ve_alanlari_ACAR()
    {
        var e = Satisa(ArayuzModuServisi.Basit);
        KalemEkle(e);

        postHatasi = "Taş ağırlığı girilmiş üründe taş kökeni zorunludur";
        e.YildizKaydet();

        // UX-S28: hata YUTULMAZ, olduğu gibi gösterilir
        e.WaitForAssertion(() => Assert.Contains("taş kökeni", e.Find("#yildiz-tezgah-hata").TextContent,
                        StringComparison.OrdinalIgnoreCase));
        // ProAlan.ZorunluGorunur: gizli alanlar AÇILDI (ödeme düzeninde tam ödeme türü seçicisi)
        e.YildizOdemeAc();
        e.WaitForElement("#islem-odeme");
        Assert.NotNull(e.Find("#islem-odeme"));
    }

    // ── Veri koruma: gizlenen alan gövdeye KONMAZ ───────────────────────────

    [Fact]
    public void Basit_modda_gizlenen_alan_POST_govdesine_KONMAZ()
    {
        var e = Satisa(ArayuzModuServisi.Basit);
        KalemEkle(e);
        istekler.Clear();

        e.YildizKaydet();

        var g = Govde(Satis(e));
        // vadeGun ANAHTARI HİÇ YOKTUR — null olarak da gitmez ("gövdede olmayan
        // alan korunur" sözleşmesi ancak böyle işler).
        Assert.False(g.TryGetProperty("vadeGun", out _));
        // Gizlenmeyen alanlar yerli yerinde: kapıyı atlatan eksik gövde ÜRETİLMEZ.
        Assert.Equal("NAKIT", g.GetProperty("odemeTuru").GetString());
        Assert.Equal("kur-1", g.GetProperty("kurSnapshotId").GetString());
    }

    [Fact]
    public void Profesyonel_modda_govde_ALAN_KAYBETMEZ()
    {
        var e = Satisa(ArayuzModuServisi.Profesyonel);
        KalemEkle(e);
        istekler.Clear();

        e.YildizKaydet();

        Assert.True(Govde(Satis(e)).TryGetProperty("vadeGun", out _));
    }

    // ── PAKET S1-i — /gider-pusulalari "Hurda alımı gir" derin bağlantısı ──────
    [Theory]
    [InlineData("BASIT")]
    [InlineData("PROFESYONEL")]
    public void S1i_Hurda_derin_baglantisi_HURDA_ALIS_turunu_acar_bilinmeyen_tur_tahmin_edilmez(string mod)
    {
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
            .NavigateTo("http://localhost/islemler?tur=HURDA_ALIS");
        var e = Ekran(mod);
        e.WaitForAssertion(() =>
            Assert.Equal("true", e.Find("#islem-tur-HURDA_ALIS").GetAttribute("aria-pressed")));
        Assert.Equal("false", e.Find("#islem-tur-SATIS").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void S1i_Bilinmeyen_tur_sorgusu_satisi_acik_birakir()
    {
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
            .NavigateTo("http://localhost/islemler?tur=UYDURMA");
        var e = Ekran("BASIT");
        Assert.Equal("true", e.Find("#islem-tur-SATIS").GetAttribute("aria-pressed"));
    }
}
