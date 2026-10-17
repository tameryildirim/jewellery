using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-22 — EKRAN KAPILARI [bUnit]
//
// DoD EK md.1: "tıklanınca hiçbir şey olmayan düğme" paketi geçiremez. Bu
// dosya, P-22'nin açtığı YOLLARIN ekranda GERÇEKTEN İSTEK ATTIĞINI ölçer:
//   • /faturalar TASLAK satırında "Düzenle" → PATCH /faturalar/{id}
//   • /sayim oturum adı satır içi → PATCH /sayim/{id}
//   • /raporlar/kasa "Sayımı gir" → POST /kasalar/{id}/gun-kapanisi
//
// ⚠ Sahte API'nin "{}" varsayılanı JsonException doğurur; YENİ uçlara
//   GERÇEKÇİ yanıt verilir (bUnit sözleşmesi).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P22EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public P22EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Kuyumcu.Web.Bilesenler.KayitSecici.BeklemeMs = 0;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            var y = yol switch
            {
                // ── /sayim ──────────────────────────────────────────────────
                var v when v.StartsWith("/api/v1/sayim", StringComparison.Ordinal)
                           && istek.Method == HttpMethod.Get && !v.Contains("/sayim/")
                    => SayimListe,
                var v when v.StartsWith("/api/v1/sayim/", StringComparison.Ordinal)
                    => SayimDetay,

                // ── /kasalar (kasa defteri seçicisi + gün kapanışı) ─────────
                var v when v.StartsWith("/api/v1/kasalar", StringComparison.Ordinal)
                           && istek.Method == HttpMethod.Post
                    => GunKapanisiYaniti,
                // PARA-DEMO: gün sonu sayım çekmecesi nakit defterini bu uçtan okur (gerçekçi yanıt).
                var v when v.Contains("/gun-kapanisi/onizleme")
                    => """{"kasaId":"ks1","paraBirimi":"TRY","toplamBakiye":1200.00,"kartToplam":0,"havaleToplam":0,"nakitDefter":1200.00}""",
                var v when v.StartsWith("/api/v1/kasalar", StringComparison.Ordinal)
                    => KasaListe,

                // ── /raporlar/kasa-defteri ─────────────────────────────────
                var v when v.Contains("/raporlar/kasa-defteri") => KasaDefteri,

                // ── /faturalar ─────────────────────────────────────────────
                var v when v.Contains("/faturalar/") && v.Contains("/kalemler") => KalemYok,
                var v when v.StartsWith("/api/v1/faturalar", StringComparison.Ordinal)
                           && istek.Method.Method == "PATCH" => TaslakPatchYaniti,
                var v when v.StartsWith("/api/v1/faturalar", StringComparison.Ordinal)
                    => FaturaListe,

                var v when v.Contains("/subeler") => """[{"id":"sb1","ad":"Merkez"}]""",
                var v when v.Contains("/reyonlar") || v.Contains("/urun-kategorileri") => "[]",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik"}]""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }

        private const string SayimListe = """
            [{"id":"s1","ad":"Vitirn","subeId":"sb1","kapsamTur":"TUMU","kapsamId":null,
              "durum":"ACIK","acildi":"2026-09-21T06:00:00Z","kapandi":null,"notlar":null}]
            """;

        private const string SayimDetay = """
            {"id":"s1","ad":"Vitirn","subeId":"sb1","kapsamTur":"TUMU","kapsamId":null,
             "durum":"ACIK","acildi":"2026-09-21T06:00:00Z","kapandi":null,"notlar":null,
             "toplamSatir":1,"sayilan":0,"sayilmayan":1,"eksikAdet":0,"fazlaAdet":0,
             "netFarkHas":0,"satirlar":[
               {"id":"sr1","urunId":"u1","kod":"U-1","ad":"Burma Bilezik","barkod":"869",
                "birim":"ADET","sistemAdet":1,"sayilanAdet":null,"fark":null,
                "guncelAdet":1,"hareketli":false,"ayarId":"K22","hasGrami":9.16,
                "farkHas":0,"sayildi":null}]}
            """;

        private const string KasaListe = """
            [{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]
            """;

        private const string GunKapanisiYaniti = """
            {"kasaId":"ks1","yerelGun":"2026-09-21","sayilanNakit":1500.00,
             "defterBakiye":1200.00,"fark":300.00,"aciklama":"fazla","kilitli":true}
            """;

        private const string KalemYok = """
            {"faturaId":"f1","faturaNo":null,"kalemKaynagi":"BASLIK",
             "hasDegerKaynagi":"ELLE","toplam":1200.00,"kalemler":[]}
            """;

        private const string TaslakPatchYaniti = """
            {"id":"f1","faturaNo":null,"cariId":"c1","cariAd":"Ayşe Demir","tur":"SATIS",
             "araToplam":1000.00,"kdvOran":0.2,"kdvTutar":200.00,"toplam":1200.00,
             "durum":"TASLAK","bankaHareketId":null,"tarih":"2026-09-21T08:00:00Z",
             "kalemKaynagi":"BASLIK","hasDegerKaynagi":"ELLE","aciklama":"düzeltildi"}
            """;

        private const string FaturaListe = """
            {"faturalar":[{"id":"f1","faturaNo":null,"cariAd":"Ayşe Demir","tur":"SATIS",
              "araToplam":1000.00,"kdvOran":0.2,"kdvTutar":200.00,"toplam":1200.00,
              "durum":"TASLAK","bankaHareketId":null,"tarih":"2026-09-21T08:00:00Z",
              "kalemKaynagi":"BASLIK","hasDegerKaynagi":"ELLE","cariId":"c1",
              "aciklama":"ilk not"}]}
            """;

        private static string KasaDefteri => $$"""
            {"baslangic":"2026-09-08","bitis":"{{DateTime.Today:yyyy-MM-dd}}",
             "saatDilimi":"Europe/Istanbul","utcOfsetSaat":3,"yazSaatiRiski":false,
             "toplamSatir":1,"kasalar":[
               {"kasaId":"ks1","kasaAd":"Merkez Kasa","paraBirimi":"TRY","acilisDeviri":0,
                "kapanisBakiye":1200.00,"guncelBakiye":1200.00,"gunler":[
                  {"tarih":"{{DateTime.Today:yyyy-MM-dd}}","devir":0,"giris":1200.00,
                   "cikis":0,"hareketSayisi":1,"kapanis":1200.00,"durum":"ACIK",
                   "sayilanNakit":null,"kapanisDefterBakiye":null,"fark":null}]}]}
            """;
    }

    // ═══ B-3 · TASLAK fatura satırında "Düzenle" GERÇEK istek atar ═════════

    [Fact]
    public void B3_taslak_satirinda_Duzenle_dugmesi_PATCH_atar()
    {
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-duzenle-f1");

        // TASLAK belgede numara YOKTUR ve SEBEBİ yazılıdır (boş hücre değil).
        Assert.NotNull(e.Find("#fk-no-yok-f1"));

        e.Find("#fk-duzenle-f1").Click();
        e.WaitForElement("#fd-duzenle-aciklama");
        e.Find("#fd-duzenle-aciklama").Input("düzeltildi");   // PAKET GOC-A: ortak Metin (oninput)
        e.Find("#cekmece-fatura-kaydet").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "PATCH" && i.Yol == "/api/v1/faturalar/f1"));
        // Gövde SUNUCUNUN beklediği alanları taşır (tip/ad eşleşmesi).
        var istek = istekler.Last(i => i.Metot == "PATCH");
        Assert.Contains("\"cariId\"", istek.Govde, StringComparison.Ordinal);
        // BELGE-KALAN (VERİ/YASAL): tarih DEĞİŞMEDİYSE gövdeye girmez (sözleşme "gövdede yoksa
        // KORU"; eski davranış dokunulmamış tarihi UTC gününe, bir gün GERİ yazabiliyordu).
        // Değişen tarihin gönderildiği BelgeKalanEkranTestleri'nde sınanır.
        Assert.DoesNotContain("\"tarih\"", istek.Govde, StringComparison.Ordinal);
        Assert.Contains("\"aciklama\"", istek.Govde, StringComparison.Ordinal);
    }

    // ═══ B-44 · sayım oturumunun adı satır içi düzenlenir ══════════════════

    [Fact]
    public void B44_sayim_adi_satir_ici_duzenlenir_ve_PATCH_atar()
    {
        var e = Render<Kuyumcu.Web.Pages.Sayim>().AyrintiAc();
        e.WaitForElement("#sy-ac-s1");
        e.Find("#sy-ac-s1").Click();

        e.WaitForElement("#si-sayim-ad-s1-ac");
        e.Find("#si-sayim-ad-s1-ac").Click();
        e.Find("#si-sayim-ad-s1").Input("Vitrin sayımı");
        e.Find("#si-sayim-ad-s1-kaydet").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "PATCH" && i.Yol == "/api/v1/sayim/s1"));
    }

    // ═══ B-33 · /sayim DURUM sütunu ham kod ya da EYLEM METNİ yazmaz ═══════

    [Fact]
    public void B33_sayim_DURUM_sutunu_gercek_durumu_yazar()
    {
        var e = Render<Kuyumcu.Web.Pages.Sayim>().AyrintiAc();
        e.WaitForElement("#sy-durum-s1");
        var metin = e.Find("#sy-durum-s1").TextContent.Trim();
        // UX-S31: ham kod değil, sözlükten çözülmüş etiket.
        Assert.Equal("AÇIK", metin, StringComparer.OrdinalIgnoreCase);
        Assert.NotEqual("ACIK", metin, StringComparer.Ordinal);   // ham kod DEĞİL
        Assert.DoesNotContain("İptal Et", metin, StringComparison.Ordinal);
    }

    // ═══ B-45 · gün sonu kasa sayımı /raporlar/kasa'dan girilir ════════════

    [Fact]
    public void B45_kasa_raporundan_gun_sonu_sayimi_girilir()
    {
        var e = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        e.WaitForElement("#kr-sayim-ks1");
        e.Find("#kr-sayim-ks1").Click();

        e.WaitForElement("#kr-sayim-tutar");
        e.Find("#kr-sayim-tutar").Input("1500");   // ZO-BELGE: girdi ailesi (Para)
        // PAKET GOC-A: fark varken gerekçe K30 (en az 20 karakter) — sunucu ile AYNI kural.
        e.Find("#kr-sayim-aciklama").Input("kasada 1.500 TL fazla çıktı, sayıldı");   // ZO-BELGE: girdi ailesi (Metin)
        e.Find("#cekmece-gun-sayimi-kaydet").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "POST" && i.Yol == "/api/v1/kasalar/ks1/gun-kapanisi"));
        var istek = istekler.Last(i => i.Metot == "POST");
        Assert.Contains("1500", istek.Govde, StringComparison.Ordinal);
    }

    [Fact]
    public void B45_negatif_sayim_SESSIZ_gecmez_alan_alti_mesaj_verir()
    {
        var e = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        e.WaitForElement("#kr-sayim-ks1");
        e.Find("#kr-sayim-ks1").Click();
        e.WaitForElement("#kr-sayim-tutar");
        e.Find("#kr-sayim-tutar").Input("-5");
        e.Find("#cekmece-gun-sayimi-kaydet").Click();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#alan-hata-kr-sayim-tutar")));
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("gun-kapanisi"));
    }
}
