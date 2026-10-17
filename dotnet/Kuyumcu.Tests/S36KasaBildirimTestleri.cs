using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S3-6 — KASALAR + BİLDİRİMLER (dalga S3B, sadeleştirme planı § 6 S-3)
//
// KASALAR (liste + çekmece şablonu, taslak n3):
//   • kartta ≤ 3 eylem + "⋯"; düzenle / pasife al "⋯" penceresinde (özellik silinmedi)
//   • çok alanlı her form Cekmece'de; ModalPencere yalnız yıkıcı onay
//   • para birimi KODU ekranda yok (UX-S31); yeni kasa = düzenleme alan kümesi (UX-S26)
//   • tek şubede seçici yok, çok şubede var (UX-S22)
//   • boş durum = 1 cümle + 1 düğme; hata "kasa yok"a çevrilmez (UX-S28)
// BİLDİRİMLER:
//   • kapalı hatırlatma basit yüzden AÇILIR (tur 8 bulgusu 28); kurulumda kapalı
//     olan kilitli ve sebebi yazılı (P-11 fail-closed)
//   • boş durum = 1 cümle + 1 düğme; sekme açıklaması değişir; Yenile sessiz değil
//   • ham kod (BASARILI, VADE_HATIRLATMA) ekranda yok
//
// ⚠ Sahte API "{}" varsayılanıyla BIRAKILMADI: dokunulan her uca gerçekçi yanıt.
// ⚠ Gövdeler METİN olarak değil JSON AYRIŞTIRILARAK sınanır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S36KasalarTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly SahteApi sahte;

    public S36KasalarTestleri()
    {
        sahte = new SahteApi(istekler);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(sahte) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        public string Subeler = """[{"id":"s1","ad":"Merkez Şube"}]""";
        public string Kasalar = """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"EUR","bakiye":150,"aktif":true,"subeId":"s1"}, {"id":"k2","ad":"Kapalı Kasa","paraBirimi":"TRY","bakiye":0,"aktif":false,"subeId":"s1"}, {"id":"k3","ad":"Boş Kasa","paraBirimi":"TRY","bakiye":0,"aktif":true,"subeId":"s1"}]""";
        /// <summary>İlk N kasa listesi okumasından SONRA liste ucu 500 döner.</summary>
        public int KasaListesiHataSonra = int.MaxValue;
        public bool KurHata;
        public bool K3GecmisHata;
        private int kasaListesiSayac;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            var get = istek.Method == HttpMethod.Get;

            if (yol.Contains("/kur/guncel") && KurHata) return Hata(503, "Kur servisine ulaşılamadı");
            if (get && yol.Contains("/kasalar/k3/hareketler") && K3GecmisHata) return Hata(500, "Geçmiş okunamadı");
            if (get && yol.EndsWith("/kasalar") && ++kasaListesiSayac > KasaListesiHataSonra)
                return Hata(500, "Kasa listesi okunamadı");

            var y = yol switch
            {
                var v when v.Contains("/kur/guncel") => """{"id":"kur1","altinHasFiyati":4100}""",
                var v when v.Contains("/subeler") => Subeler,
                var v when get && v.Contains("/kasalar/") && v.Contains("/hareketler") =>
                    """{"hareketler":[{"id":"kh1","tutar":150,"odemeTuru":"NAKIT","tarih":"2026-09-16T10:00:00Z","hareketId":"h1","aciklama":null,"stornoKaynakId":null}, {"id":"kh2","tutar":-50,"odemeTuru":"NAKIT","tarih":"2026-09-16T11:00:00Z","hareketId":null,"aciklama":"Kira","stornoKaynakId":null}]}""",
                var v when get && v.EndsWith("/kasalar") => Kasalar,
                var v when v.EndsWith("/hareket") => """{"id":"khy","bakiye":250}""",
                var v when v.EndsWith("/storno") => """{"stornoKasaHareketId":"khs1"}""",
                var v when v.EndsWith("/gun-kapanisi") => """{"id":"gk1"}""",
                // PARA-DEMO: Günü Kapat çekmecesi sayılacak nakdi bu uçtan okur (kart/havale yok → nakit = bakiye).
                var v when get && v.Contains("/gun-kapanisi/onizleme") =>
                    """{"kasaId":"k1","paraBirimi":"EUR","toplamBakiye":150,"kartToplam":0,"havaleToplam":0,"nakitDefter":150}""",
                var v when !get && v.EndsWith("/kasalar") => """{"id":"k9","ad":"Yeni","paraBirimi":"TRY","bakiye":0,"aktif":true,"subeId":"s1"}""",
                var v when v.Contains("/kasalar/") => """{"id":"k1","ad":"Ana Kasa","paraBirimi":"EUR","bakiye":150,"aktif":true,"subeId":"s1"}""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }

        private static HttpResponseMessage Hata(int kod, string mesaj) => new((HttpStatusCode)kod)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { message = mesaj }),
                System.Text.Encoding.UTF8, "application/json"),
        };
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Kasalar> Ciz(string mod = ArayuzModuServisi.Profesyonel)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Kuyumcu.Web.Pages.Kasalar>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll(".yukleniyor, [aria-busy='true']")));
        return e;
    }

    private static JsonElement Govde(string g) => JsonDocument.Parse(g).RootElement;

    private static string? Alan(string govde, string ad)
    {
        var e = Govde(govde);
        return e.TryGetProperty(ad, out var v) && v.ValueKind != JsonValueKind.Null ? v.ToString() : null;
    }

    // ── 1) Kartta ≤ 3 eylem + "⋯" ───────────────────────────────────────────

    [Fact]
    public void S36_01_aktif_kartta_UC_eylem_ve_menu_dugmesi_duzenle_ve_pasife_al_MENUDE()
    {
        var e = Ciz();
        e.WaitForElement("#kasa-eylem-k1");
        var eylem = e.Find("#kasa-eylem-k1");
        var dugmeler = eylem.QuerySelectorAll("button").ToList();

        Assert.Equal(4, dugmeler.Count);
        Assert.Equal(3, dugmeler.Count(b => !b.HasAttribute("popovertarget")));
        Assert.Equal(["kasa-hareket-k1", "kasa-gecmis-k1", "kasa-gun-kapat-k1", "kasa-menu-ac-k1"],
            dugmeler.Select(b => b.Id ?? "").ToArray());
        Assert.Equal("kasa-menu-k1", e.Find("#kasa-menu-ac-k1").GetAttribute("popovertarget"));

        // Düzenle ve Pasife al "⋯" penceresinde — ÖZELLİK SİLİNMEDİ.
        var menu = e.Find("#kasa-menu-k1");
        Assert.Equal("auto", menu.GetAttribute("popover"));
        Assert.NotNull(menu.QuerySelector("#kasa-duzenle-k1.kasa-duzenle-btn"));
        Assert.NotNull(menu.QuerySelector("#kasa-pasife-al-k1"));
        Assert.Null(eylem.QuerySelector("#kasa-duzenle-k1"));
        Assert.Null(eylem.QuerySelector("#kasa-pasife-al-k1"));
        // Menüdeki eylem pencereyi kapatır VE kendi işini yapar.
        Assert.Equal("hide", e.Find("#kasa-duzenle-k1").GetAttribute("popovertargetaction"));
    }

    [Fact]
    public void S36_02_pasif_kartta_ILK_eylem_aktife_al_hareket_menude_KILITLI_ve_sebepli()
    {
        var e = Ciz();
        e.WaitForElement("#kasa-eylem-k2");
        var ilk = e.Find("#kasa-eylem-k2").QuerySelectorAll("button").First();
        Assert.Equal("kasa-aktife-al-k2", ilk.Id);

        var hareket = e.Find("#kasa-hareket-k2");
        Assert.True(hareket.HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(hareket.GetAttribute("title")));
        Assert.NotNull(e.Find("#kasa-menu-k2").QuerySelector("#kasa-hareket-k2"));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#kasa-pasif-not-k2").TextContent));
        // Pasif kasada gün kapatma ve pasife alma YOK (anlamsız).
        Assert.Empty(e.FindAll("#kasa-gun-kapat-k2"));
        Assert.Empty(e.FindAll("#kasa-pasife-al-k2"));
    }

    [Fact]
    public void S36_03_bakiyeli_kasada_pasife_al_KILITLI_sebep_menude_YAZILI()
    {
        var e = Ciz();
        e.WaitForElement("#kasa-pasife-al-k1");
        Assert.True(e.Find("#kasa-pasife-al-k1").HasAttribute("disabled"));
        Assert.Contains("Bakiyesi sıfır olmayan kasa", e.Find("#kasa-bakiyeli-not-k1").TextContent);
        // Boş kasada açık, notu yok.
        Assert.False(e.Find("#kasa-pasife-al-k3").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#kasa-bakiyeli-not-k3"));
    }

    // ── 2) Ham kod yok (UX-S31) ─────────────────────────────────────────────

    [Fact]
    public void S36_04_para_birimi_KODU_kartta_ve_secicide_gorunmez_govdeye_KOD_gider()
    {
        var e = Ciz();
        e.WaitForElement("#kasa-kart-k1");
        var kart = e.Find("#kasa-kart-k1 .satir-arasi").TextContent;
        Assert.Contains("Euro", kart);
        Assert.DoesNotContain("EUR", kart);
        Assert.DoesNotContain("TRY", e.Find("#kasa-kart-k2 .satir-arasi").TextContent);
        Assert.Contains("Türk lirası", e.Find("#kasa-kart-k2 .satir-arasi").TextContent);

        e.Find("#kasa-yeni").Click();
        e.WaitForElement("#cekmece-kasa-yeni");
        var adlar = SeciciYardim.Secenekler(e, "kasa-doviz").Select(o => o.Ad()).ToArray();
        var degerler = SeciciYardim.Secenekler(e, "kasa-doviz").Select(o => o.Deger()).ToArray();
        // KAT-1 (K5): TRY seçeneği "₺ Türk lirası" okunur; değer yine "TRY".
        Assert.Equal(["\u20ba Türk lirası", "Dolar", "Euro", "Sterlin"], adlar);
        Assert.Equal(["TRY", "USD", "EUR", "GBP"], degerler);
    }

    // ── 3) İki yüz, tek gövde ───────────────────────────────────────────────

    [Fact]
    public void S36_05_basit_yuzde_tek_ozet_profesyonelde_uc_ozet_karti()
    {
        var basit = Ciz(ArayuzModuServisi.Basit);
        basit.WaitForElement("#kasa-liste");
        Assert.Single(basit.FindAll(".stat-kart"));
        Assert.Contains("TL", basit.Find(".stat-kart").TextContent);   // UX-S32: hangi para birimi
        // Özellik silinmedi: kasa kartları ve bütün eylemler basit yüzde de var.
        Assert.NotNull(basit.Find("#kasa-duzenle-k1"));
        Assert.NotNull(basit.Find("#kasa-gun-kapat-k1"));
    }

    [Fact]
    public void S36_06_profesyonel_yuzde_uc_ozet_karti()
    {
        var pro = Ciz();
        pro.WaitForElement("#kasa-liste");
        Assert.Equal(3, pro.FindAll(".stat-kart").Count);
    }

    // ── 4) Yeni kasa çekmecesi (UX-S22 / UX-S26) ────────────────────────────

    [Fact]
    public void S36_07_yeni_kasa_CEKMECEDE_tek_subede_secici_YOK_sube_gövdeye_OTOMATIK()
    {
        var e = Ciz();
        e.Find("#kasa-yeni").Click();
        e.WaitForElement("#cekmece-kasa-yeni");
        Assert.Empty(e.FindAll("#kasa-sube"));
        Assert.Contains("Merkez Şube", e.Find("#kasa-sube-tek").TextContent);

        e.Find("#kasa-ad").Input("Vitrin Kasası");
        SeciciYardim.Sec(e, "kasa-doviz", "USD");
        istekler.Clear();
        e.Find("#kasa-olustur").Click();

        e.WaitForAssertion(() =>
        {
            var post = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/kasalar"));
            Assert.Equal("Vitrin Kasası", Alan(post.Govde, "ad"));
            Assert.Equal("USD", Alan(post.Govde, "paraBirimi"));
            Assert.Equal("s1", Alan(post.Govde, "subeId"));
        });
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#cekmece-kasa-yeni")));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#basari-bandi").TextContent));
    }

    [Fact]
    public void S36_08_cok_subede_SUBE_SECICISI_gorunur_ve_secilen_gövdeye_gider()
    {
        sahte.Subeler = """[{"id":"s1","ad":"Merkez Şube"},{"id":"s2","ad":"Kapalıçarşı Şube"}]""";
        var e = Ciz();
        e.Find("#kasa-yeni").Click();
        e.WaitForElement("#cekmece-kasa-yeni");
        Assert.Empty(e.FindAll("#kasa-sube-tek"));
        Assert.Equal("s1", SeciciYardim.SeciliDeger(e, "kasa-sube"));   // varsayılan dolu
        SeciciYardim.Sec(e, "kasa-sube", "s2");
        e.Find("#kasa-ad").Input("Çarşı Kasası");
        istekler.Clear();
        e.Find("#kasa-olustur").Click();

        e.WaitForAssertion(() =>
        {
            var post = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/kasalar"));
            Assert.Equal("s2", Alan(post.Govde, "subeId"));
        });
    }

    [Fact]
    public void S36_09_bos_adla_kaydet_ALAN_ALTI_mesaj_verir_ISTEK_ATMAZ()
    {
        var e = Ciz();
        e.Find("#kasa-yeni").Click();
        e.WaitForElement("#kasa-olustur");
        istekler.Clear();
        e.Find("#kasa-olustur").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-kasa-ad").TextContent));
    }

    // ── 5) Boş durum ve hata (UX-S28) ───────────────────────────────────────

    [Fact]
    public void S36_10_bos_liste_BIR_cumle_BIR_dugme_dugme_cekmeceyi_acar()
    {
        sahte.Kasalar = "[]";
        var e = Ciz();
        e.WaitForElement("#kasa-bos");
        var bos = e.Find("#kasa-bos");
        Assert.Single(bos.QuerySelectorAll(".bos-durum p"));
        Assert.Equal(KasaBildirimSozlugu.Tr.BosDurum, bos.QuerySelector(".bos-durum p")!.TextContent.Trim());
        Assert.Single(bos.QuerySelectorAll("button"));

        e.Find("#kasa-bos-yeni").Click();
        e.WaitForElement("#cekmece-kasa-yeni");
    }

    [Fact]
    public void S36_11_liste_hatasi_BOS_DURUMA_cevrilmez_son_liste_SILINMEZ()
    {
        // İlk okuma başarılı, sonraki (hareket sonrası tazeleme) 500.
        sahte.KasaListesiHataSonra = 1;
        var e = Ciz();
        e.WaitForElement("#kasa-kart-k1");
        e.Find("#kasa-hareket-k1").Click();
        e.WaitForElement("#h-tutar");
        e.Find("#h-tutar").Input("100");
        e.Find("#h-kaydet").Click();

        e.WaitForAssertion(() => Assert.Contains("Kasa listesi okunamadı", e.Find("#hata-bandi").TextContent));
        Assert.NotNull(e.Find("#kasa-kart-k1"));       // son bilinen liste DURUYOR
        Assert.Empty(e.FindAll("#kasa-bos"));
    }

    [Fact]
    public void S36_12_ilk_okuma_hatasinda_bos_durum_CIZILMEZ_hata_bandi_cizilir()
    {
        sahte.KasaListesiHataSonra = 0;
        var e = Ciz();
        e.WaitForAssertion(() => Assert.Contains("Kasa listesi okunamadı", e.Find("#hata-bandi").TextContent));
        Assert.Empty(e.FindAll("#kasa-bos"));
        Assert.Empty(e.FindAll(".bos-durum"));
    }

    // ── 6) Hareket / gün sonu / geçmiş çekmecede ────────────────────────────

    [Fact]
    public void S36_13_hareket_CEKMECEDE_POST_govdesi_ve_sonuc_bandi()
    {
        var e = Ciz();
        e.Find("#kasa-hareket-k1").Click();
        e.WaitForElement("#cekmece-kasa-hareket");
        Assert.Contains("Ana Kasa", e.Find("#cekmece-kasa-hareket-baslik").TextContent);
        SeciciYardim.Sec(e, "h-tur", "CIKIS");
        e.Find("#h-tutar").Input("40");
        istekler.Clear();
        e.Find("#h-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var post = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/kasalar/k1/hareket"));
            Assert.Equal("CIKIS", Alan(post.Govde, "yon"));
            Assert.Equal("40", Alan(post.Govde, "tutar"));
        });
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#cekmece-kasa-hareket")));
        // UX-S27: sunucunun döndürdüğü YENİ BAKİYE banda yazılır.
        Assert.Contains("250", e.Find("#basari-bandi").TextContent);
    }

    [Fact]
    public void S36_14_gun_sonu_CEKMECEDE_fark_varken_kilitli_ve_SEBEBI_yazili()
    {
        var e = Ciz();
        e.Find("#kasa-gun-kapat-k1").Click();
        e.WaitForElement("#cekmece-kasa-gun");
        e.Find("#gun-sayilan").Input("100");   // defter 150 → fark −50

        Assert.True(e.Find("#gun-kapat-onayla").HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#gun-kapat-kilit").TextContent));

        e.Find("#gun-aciklama").Input("Akşam sayımında eksik çıktı");
        Assert.False(e.Find("#gun-kapat-onayla").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#gun-kapat-kilit"));
    }

    [Fact]
    public void S36_15_gecmis_CEKMECEDE_storno_onayi_acilinca_GIZLENIR_vazgecince_GERI_gelir()
    {
        var e = Ciz();
        e.Find("#kasa-gecmis-k1").Click();
        e.WaitForElement("#cekmece-kasa-gecmis");
        Assert.Contains("Ana Kasa", e.Find("#cekmece-kasa-gecmis-baslik").TextContent);

        e.Find("#kh-storno-kh2").Click();
        e.WaitForElement("#kh-storno-onay");
        Assert.Empty(e.FindAll("#cekmece-kasa-gecmis"));   // iki pencere üst üste binmez

        e.Find("#kh-storno-vazgec").Click();
        e.WaitForElement("#cekmece-kasa-gecmis");
        Assert.Empty(e.FindAll("#kh-storno-onay"));
        Assert.NotNull(e.Find("#kh-storno-kh2"));
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    [Fact]
    public void S36_16_storno_KAPALI_cekmecede_kasa_baglamini_korur_ve_POST_atar_sonra_gecmis_geri_gelir()
    {
        var e = Ciz();
        e.Find("#kasa-gecmis-k1").Click();
        e.WaitForElement("#kh-storno-kh2");
        e.Find("#kh-storno-kh2").Click();
        e.Find("#kh-storno-gerekce").Input("Yanlış tutar girildi, ters kayıtla düzeltiliyor");
        istekler.Clear();
        e.Find("#kh-storno-onayla").Click();

        e.WaitForAssertion(() =>
        {
            var post = Assert.Single(istekler, i => i.Metot == "POST");
            Assert.EndsWith("/kasalar/k1/hareket/kh2/storno", post.Yol);
        });
        e.WaitForElement("#cekmece-kasa-gecmis");
        Assert.Contains("khs1", e.Find("#basari-bandi").TextContent);
    }

    [Fact]
    public void S36_17_baska_kasanin_gecmisi_HATA_verirse_onceki_kasanin_satirlari_GOSTERILMEZ()
    {
        sahte.K3GecmisHata = true;
        var e = Ciz();
        e.Find("#kasa-gecmis-k1").Click();
        e.WaitForElement("#kh-storno-kh2");
        e.Find("#cekmece-kasa-gecmis-iptal").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#cekmece-kasa-gecmis")));

        e.Find("#kasa-gecmis-k3").Click();
        e.WaitForElement("#cekmece-kasa-gecmis");
        e.WaitForAssertion(() => Assert.Contains("Geçmiş okunamadı", e.Find("#cekmece-kasa-gecmis").TextContent));
        Assert.Empty(e.FindAll("#kh-storno-kh2"));        // Ana Kasa'nın satırı Boş Kasa başlığı altında YOK
        Assert.Empty(e.FindAll("#cekmece-kasa-gecmis .bos-durum"));   // hata "kayıt yok" değil
    }

    [Fact]
    public void S36_18_kur_okunamazsa_SEBEP_sessizce_yutulmaz_gecmiste_yazilir()
    {
        sahte.KurHata = true;
        var e = Ciz();
        e.Find("#kasa-gecmis-k1").Click();
        e.WaitForElement("#kh-storno-kh1");
        Assert.True(e.Find("#kh-storno-kh1").HasAttribute("disabled"));      // defter bacağı kur ister
        Assert.Contains("Kur servisine ulaşılamadı", e.Find("#kh-kur-hata").TextContent);
        Assert.False(e.Find("#kh-storno-kh2").HasAttribute("disabled"));     // elle hareket kur istemez
    }

    // ── 7) DoD EK md.1 — her düğme bir iş yapar ya da kilitli + sebepli ────

    [Fact]
    public void S36_19_kart_ve_sayfa_basindaki_HER_dugme_is_yapar_ya_da_kilitli_ve_sebeplidir()
    {
        var ilk = Ciz();
        var idler = ilk.FindAll("#kasa-liste button, #sayfa-basi-kasalar button")
            .Select(b => b.Id ?? "").Where(x => x.Length > 0).ToList();
        Assert.True(idler.Count >= 12, $"beklenenden az düğme: {idler.Count}");

        foreach (var id in idler)
        {
            var e = Render<Kuyumcu.Web.Pages.Kasalar>().AyrintiAc();
            e.WaitForElement("#kasa-liste");
            var d = e.Find($"#{id}");
            if (d.HasAttribute("disabled"))
            {
                // Kilitli düğmenin sebebi title'da ya da kartta/menüde YAZILI (UX-S30).
                var kart = d.Closest(".kart")!;
                Assert.True(!string.IsNullOrWhiteSpace(d.GetAttribute("title"))
                            || kart.QuerySelector("[id^='kasa-pasif-not-'], [id^='kasa-bakiyeli-not-']") is not null,
                    $"{id}: kilitli düğmenin sebebi yok");
                continue;
            }
            // Yerli popover düğmeleri (⋯ aç / kapat) tarayıcıda pencereyi açıp kapatır;
            // hedef pencere DOM'da VAR olmalı (boşa işaret eden düğme yok).
            if (d.HasAttribute("popovertarget") && !id.StartsWith("kasa-duzenle-") && !id.StartsWith("kasa-pasife-al-"))
            {
                Assert.NotNull(e.Find($"#{d.GetAttribute("popovertarget")}"));
                continue;
            }
            istekler.Clear();
            d.Click();
            e.WaitForAssertion(() => Assert.True(
                istekler.Count > 0 || e.FindAll(".cekmece, .modal-panel").Count > 0,
                $"{id}: tıklanınca hiçbir şey olmadı"));
        }
    }

    // ── 8) Sözlük ───────────────────────────────────────────────────────────

    [Fact]
    public void S36_20_sozluk_TR_ve_EN_eksiksiz_ve_ham_kod_cevirisi_tek_kaynaktan()
    {
        foreach (var p in typeof(KasaBildirimMetinleri).GetProperties().Where(p => p.PropertyType == typeof(string)))
        {
            Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(KasaBildirimSozlugu.Tr)), $"TR boş: {p.Name}");
            Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(KasaBildirimSozlugu.En)), $"EN boş: {p.Name}");
        }
        Assert.Same(KasaBildirimSozlugu.En, KasaBildirimSozlugu.Al("en"));
        Assert.Same(KasaBildirimSozlugu.Tr, KasaBildirimSozlugu.Al("tr"));
        Assert.Equal("Türk lirası", KasaBildirimSozlugu.ParaAdi(KasaBildirimSozlugu.Tr, "TRY"));
        Assert.Equal("XAU", KasaBildirimSozlugu.ParaAdi(KasaBildirimSozlugu.Tr, "XAU"));   // uydurma ad yok
        Assert.Equal("Başarılı", KasaBildirimSozlugu.DurumAdi(KasaBildirimSozlugu.Tr, "BASARILI"));
        Assert.Equal("Bilinmiyor", KasaBildirimSozlugu.DurumAdi(KasaBildirimSozlugu.Tr, "TUHAF"));
    }

    // ═══ ORTAK-1 · KEŞİF #4 (30.09, KRİTİK) — kasa "+ Hareket" / gün sonu: tuş tuş "1.500" ═══
    // Canlıda kasa Tutar kutusu "1.500" → "0"a dönüyordu (Kaydet "sıfırdan büyük" diye durdu ama kutu
    // yazılanı silmişti); gün sonunda sayılan nakit aynı yoldan 0 sayılıp fark yazılabilirdi.
    private Kuyumcu.Core.I18n.Sozluk T => Services.GetRequiredService<DilServisi>().T;
    private static readonly string[] OK1TusTus1500 = ["1", "1.", "1.5", "1.50", "1.500"];

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void OK1_K4_kasa_hareket_tutari_belirsiz_1_500_POST_YOK_sebep_yazar(string mod)
    {
        var e = Ciz(mod);
        e.Find("#kasa-hareket-k1").Click();
        e.WaitForElement("#h-tutar");
        foreach (var a in OK1TusTus1500) e.Find("#h-tutar").Input(a);
        e.Find("#h-tutar").Blur();
        Assert.Equal("1.500", e.Find("#h-tutar").GetAttribute("value"));
        Assert.Equal("true", e.Find("#h-tutar").GetAttribute("aria-invalid"));
        Assert.True(e.Find("#h-kaydet").HasAttribute("disabled"));
        Assert.Contains(T.Ortak.Tutar, e.Find("#h-sayi-kilit").TextContent);
        istekler.Clear();
        e.Find("#h-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.Equal("1.500", e.Find("#h-tutar").GetAttribute("value"));

        e.Find("#h-tutar").Input("1500");
        Assert.Empty(e.FindAll("#h-sayi-kilit"));
        e.Find("#h-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal("1500",
            Alan(Assert.Single(istekler, i => i.Metot == "POST").Govde, "tutar")));
    }

    [Fact]
    public void OK1_K4_gun_sonu_sayilan_belirsiz_1_500_POST_YOK()
    {
        var e = Ciz();
        e.Find("#kasa-gun-kapat-k1").Click();
        e.WaitForElement("#gun-sayilan");
        foreach (var a in OK1TusTus1500) e.Find("#gun-sayilan").Input(a);
        // PARA-DEMO-2 (madde 2): fark ve gerekçe YALNIZ anlaşılan bir sayım girilince çizilir — belirsiz "1.500"de yok.
        Assert.Empty(e.FindAll("#gun-aciklama"));
        Assert.Equal("1.500", e.Find("#gun-sayilan").GetAttribute("value"));
        Assert.True(e.Find("#gun-kapat-onayla").HasAttribute("disabled"));
        Assert.Contains(T.Kasa.SayilanNakit, e.Find("#gun-sayi-kilit").TextContent);
        istekler.Clear();
        e.Find("#gun-kapat-onayla").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// BİLDİRİMLER
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S36BildirimTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string bildirimYaniti = BosListe;
    private string isYaniti = IsListesi;

    private const string BosListe = """
        {"durum":"ACIK","limit":100,"azamiLimit":100,"kapaliGosterimGun":30,"dahaFazlasiVar":false,"bildirimler":[]}
        """;

    private const string IkiBildirim = """
        {"durum":"ACIK","limit":100,"azamiLimit":100,"kapaliGosterimGun":30,"dahaFazlasiVar":false,
         "bildirimler":[
           {"id":"b1","tur":"VADE_HATIRLATMA","onem":"KRITIK","baslik":"Vadesi geçmiş borç","govde":"Bir müşterinin borcu gecikmiş.",
            "hedefRota":"/raporlar/vade-takibi","ilgiliVarlikTuru":null,"ilgiliVarlikId":null,"olusturuldu":"2026-09-16T08:00:00Z",
            "kapatildi":null,"kapatanKullaniciId":null,"kapatmaNotu":null},
           {"id":"b2","tur":"SAKLAMA_ONIZLEME","onem":"UYARI","baslik":"Saklama","govde":"2 kayıt.","hedefRota":null,
            "ilgiliVarlikTuru":null,"ilgiliVarlikId":null,"olusturuldu":"2026-09-16T07:00:00Z",
            "kapatildi":null,"kapatanKullaniciId":null,"kapatmaNotu":null}]}
        """;

    private const string IsListesi = """
        {"azamiCalisma":50,
         "ayarlar":{"baslangicSaat":0,"vadeUfukGun":7,"masakUyariGun":3,"kapaliIsler":["MASAK_SIB_SURESI"],
           "saatEnAz":0,"saatEnCok":23,"vadeEnAz":1,"vadeEnCok":365,"masakEnAz":0,"masakEnCok":60,
           "varsayilanVadeUfukGun":7,"varsayilanMasakUyariGun":3},
         "isler":[
           {"kod":"VADE_HATIRLATMA","ad":"Vade hatırlatma","etkin":true,"yapilandirmaEtkin":true,"kiraciKapatti":false,
            "sonCalisma":"2026-09-16T08:00:00Z","sonDurum":"BASARILI"},
           {"kod":"MASAK_SIB_SURESI","ad":"MASAK bildirim süresi","etkin":false,"yapilandirmaEtkin":true,"kiraciKapatti":true,
            "sonCalisma":null,"sonDurum":null},
           {"kod":"TEVSIK_KURTARMA","ad":"Tevsik hatırlatması","etkin":false,"yapilandirmaEtkin":false,"kiraciKapatti":false,
            "sonCalisma":null,"sonDurum":null}],
         "calismalar":[
           {"id":"c1","isKodu":"VADE_HATIRLATMA","calismaAnahtari":"2026-09-16","baslangic":"2026-09-16T08:00:00Z",
            "bitis":"2026-09-16T08:00:01Z","durum":"BASARILI","uretilenBildirim":1,"incelenenKayit":4,
            "sureMs":42,"hataOzeti":null,"elle":false}]}
        """;

    private const string HepsiAcikIsler = """
        {"azamiCalisma":50,"ayarlar":null,
         "isler":[{"kod":"VADE_HATIRLATMA","ad":"Vade hatırlatma","etkin":true,"yapilandirmaEtkin":true,"kiraciKapatti":false,
            "sonCalisma":null,"sonDurum":null}],"calismalar":[]}
        """;

    public S36BildirimTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(S36BildirimTestleri sahip) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            sahip.istekler.Add((istek.Method.Method, yol, govde));
            var y = yol switch
            {
                var v when v.Contains("/isler/ayarlar") =>
                    """{"baslangicSaat":0,"vadeUfukGun":7,"masakUyariGun":3,"kapaliIsler":[],"mesaj":"Zamanlanmış iş ayarları kaydedildi"}""",
                var v when v.Contains("/isler/") && v.EndsWith("/calistir") =>
                    """{"isKodu":"VADE_HATIRLATMA","durum":"BASARILI","atlandi":false,"mesaj":"İş çalıştı: 1 yeni bildirim, 0 tekrar (atlandı)."}""",
                var v when v.EndsWith("/isler") => sahip.isYaniti,
                var v when v.Contains("/bildirimler/") && v.EndsWith("/kapat") => """{"id":"b1","mesaj":"Bildirim kapatıldı"}""",
                var v when v.Contains("/bildirimler") => sahip.bildirimYaniti,
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Bildirimler> Ciz(
        string mod = ArayuzModuServisi.Basit, string rol = "SAHIP")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1", mod, mod));
        return Render<Kuyumcu.Web.Pages.Bildirimler>().AyrintiAc();
    }

    // ── 1) Kapalı hatırlatma basit yüzden açılır ────────────────────────────

    [Fact]
    public void S36_21_basit_yuzde_KAPALI_hatirlatma_karti_var_is_paneli_yok()
    {
        var e = Ciz();
        e.WaitForElement("#bildirim-kapali-isler");
        Assert.Empty(e.FindAll("#bildirim-is-paneli"));
        Assert.NotNull(e.Find("#bildirim-kapali-is-MASAK_SIB_SURESI"));
        Assert.NotNull(e.Find("#bildirim-kapali-is-TEVSIK_KURTARMA"));
        Assert.Empty(e.FindAll("#bildirim-kapali-is-VADE_HATIRLATMA"));   // açık iş listede yok
        // UX-S31: kart metninde ham iş kodu yok.
        Assert.DoesNotContain("MASAK_SIB_SURESI", e.Find("#bildirim-kapali-isler").TextContent);
    }

    [Fact]
    public void S36_22_kiracinin_kapattigi_is_AC_ile_PATCH_gider_yalniz_kapaliIsler_ve_sonuc_bantta()
    {
        var e = Ciz();
        e.WaitForElement("#is-ac-MASAK_SIB_SURESI");
        Assert.False(e.Find("#is-ac-MASAK_SIB_SURESI").HasAttribute("disabled"));
        istekler.Clear();
        e.Find("#is-ac-MASAK_SIB_SURESI").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/isler/ayarlar")));
        var g = JsonDocument.Parse(istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
        // PATCH sözleşmesi: yalnız kapaliIsler gönderilir; diğer ayarlar gövdede YOK (korunur).
        Assert.Equal(["kapaliIsler"], g.EnumerateObject().Select(p => p.Name).ToArray());
        var kapalilar = g.GetProperty("kapaliIsler").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.DoesNotContain("MASAK_SIB_SURESI", kapalilar);
        // UX-S27: sunucu mesajı + hangi hatırlatmanın açıldığı banda yazılır.
        e.WaitForAssertion(() =>
        {
            var bant = e.Find(".bant-basari").TextContent;
            Assert.Contains("MASAK bildirim süresi", bant);
            Assert.Contains("yeniden açıldı", bant);
            Assert.Contains("Zamanlanmış iş ayarları kaydedildi", bant);
        });
    }

    [Fact]
    public void S36_23_kurulumda_kapali_is_KILITLI_sebebi_ve_yolu_yazili_istek_ATMAZ()
    {
        var e = Ciz();
        e.WaitForElement("#is-ac-TEVSIK_KURTARMA");
        var d = e.Find("#is-ac-TEVSIK_KURTARMA");
        Assert.True(d.HasAttribute("disabled"));
        Assert.Equal(KasaBildirimSozlugu.Tr.KurulumKapaliSebep, d.GetAttribute("title"));
        var kilit = e.Find("#is-ac-kilit-TEVSIK_KURTARMA").TextContent;
        Assert.Contains(KasaBildirimSozlugu.Tr.KurulumKapaliSebep, kilit);
        Assert.Contains(KasaBildirimSozlugu.Tr.KurulumKapaliYol, kilit);
        // Kilitli düğmenin olay işleyicisi YOKTUR — tıklama hiçbir yola açılmaz.
        istekler.Clear();
        Assert.Throws<MissingEventHandlerException>(() => d.Click());
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
    }

    [Fact]
    public void S36_24_tezgahtar_kapali_hatirlatma_kartini_GORMEZ()
    {
        var e = Ciz(rol: "TEZGAHTAR");
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/bildirimler")));
        Assert.Empty(e.FindAll("#bildirim-kapali-isler"));
    }

    [Fact]
    public void S36_25_kapali_is_yoksa_kart_CIZILMEZ()
    {
        isYaniti = HepsiAcikIsler;
        var e = Ciz();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/isler")));
        Assert.Empty(e.FindAll("#bildirim-kapali-isler"));
    }

    [Fact]
    public void S36_26_profesyonel_yuzde_TEK_cevap_kutu_var_kapali_karti_yok()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        e.WaitForElement("#bildirim-is-paneli");
        Assert.Empty(e.FindAll("#bildirim-kapali-isler"));
        Assert.NotNull(e.Find("#is-acik-MASAK_SIB_SURESI"));
    }

    // ── 2) Boş durum / sekme açıklaması / Yenile ────────────────────────────

    [Fact]
    public void S36_27_bos_durum_BIR_cumle_BIR_dugme_ve_dugme_GERCEK_istek_atar()
    {
        var e = Ciz();
        e.WaitForElement("#bildirim-bos");
        var bos = e.Find("#bildirim-bos");
        Assert.Single(bos.QuerySelectorAll(".bos-durum p"));
        Assert.Single(bos.QuerySelectorAll("button"));
        Assert.Equal(KasaBildirimSozlugu.Tr.BosAcikCta, e.Find("#bildirim-bos-cta").TextContent.Trim());

        istekler.Clear();
        e.Find("#bildirim-bos-cta").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("durum=KAPALI")));
        e.WaitForAssertion(() =>
            Assert.Equal(KasaBildirimSozlugu.Tr.BosKapaliCta, e.Find("#bildirim-bos-cta").TextContent.Trim()));
    }

    [Fact]
    public void S36_28_sekme_degisince_sayfa_ACIKLAMASI_degisir()
    {
        var e = Ciz();
        e.WaitForElement("#sayfa-basi-bildirimler-aciklama");
        var acik = e.Find("#sayfa-basi-bildirimler-aciklama").TextContent.Trim();

        e.Find("#bildirim-sekme-kapali").Click();
        e.WaitForAssertion(() => Assert.Equal(KasaBildirimSozlugu.Tr.AciklamaKapali,
            e.Find("#sayfa-basi-bildirimler-aciklama").TextContent.Trim()));
        e.Find("#bildirim-sekme-tumu").Click();
        e.WaitForAssertion(() => Assert.Equal(KasaBildirimSozlugu.Tr.AciklamaTumu,
            e.Find("#sayfa-basi-bildirimler-aciklama").TextContent.Trim()));
        Assert.NotEqual(acik, KasaBildirimSozlugu.Tr.AciklamaTumu);
    }

    [Fact]
    public void S36_29_Yenile_SESSIZ_degil_kac_bildirim_geldigini_banda_yazar()
    {
        bildirimYaniti = IkiBildirim;
        var e = Ciz();
        e.WaitForElement("#bildirim-b1");
        istekler.Clear();
        e.Find("#bildirim-yenile").Click();
        e.WaitForAssertion(() =>
        {
            Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("/bildirimler"));
            Assert.Equal(string.Format(KasaBildirimSozlugu.Tr.Yenilendi, 2), e.Find(".bant-basari").TextContent.Trim());
        });
    }

    // ── 3) Ham kod yok (profesyonel iş paneli) ──────────────────────────────

    [Fact]
    public void S36_30_is_panelinde_ham_durum_ve_is_KODU_gorunmez()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        e.WaitForElement("#bildirim-is-paneli");
        var metin = e.Find("#bildirim-is-paneli").TextContent;
        Assert.DoesNotContain("BASARILI", metin);
        Assert.DoesNotContain("VADE_HATIRLATMA", metin);
        Assert.Contains("Başarılı", metin);
        Assert.Contains("Vade hatırlatma", metin);
    }
}
