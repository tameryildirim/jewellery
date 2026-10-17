using System.Reflection;
using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-6 — PRO MODDA "DİĞER ▾" (PLAN-SADELESTIRME § S-2 madde 2.9)
//
// KABUL: (a) P-21 uçlarının HER BİRİ ekrandan çağrılır (UX-S25 "uç var düğme
// yok" yasağı kapanır) — hem yapısal (uç listesi yansımayla alınır) hem de
// ekrandan tıklanarak (sahte API gelen isteği ve gövdeyi kaydeder);
// (b) basit modda Diğer menüsü DOM'da YOK.
// Gövdeler JSON olarak AYRIŞTIRILARAK sınanır (Türkçe metin ham dizgede aranmaz).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class S26DigerIslemlerTestleri : BunitContext
{
    private static readonly FisDigerMetinleri M = FisDigerSozlugu.Tr;
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly HashSet<string> hataliYollar = [];

    public S26DigerIslemlerTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, hataliYollar)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, HashSet<string> hatali) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            if (yol.StartsWith("/api/v1", StringComparison.Ordinal)) yol = yol[7..];   // ApiIstemci öneki
            var metot = istek.Method.Method;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((metot, yol, govde));

            if (hatali.Any(h => yol.Contains(h)))
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                { Content = new StringContent("""{"hata":"Sunucu hatası (test)"}""", System.Text.Encoding.UTF8, "application/json") };

            var y = (metot, yol) switch
            {
                (_, var v) when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","sistemCarisi":true}""",
                (_, var v) when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                (_, var v) when v.Contains("/fiyat/teklif") => """{"teklifVar":false,"sebep":"LISTE_YOK"}""",
                (_, var v) when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":9.16,"iscilikTl":0,"satisFiyati":10000},{"id":"u2","ad":"Tektaş Yüzük","ayarId":"K14","hasGrami":2,"iscilikTl":0,"satisFiyati":20000}]""",
                (_, var v) when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                (_, var v) when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                (_, var v) when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                (_, var v) when v.Contains("/pos/cihazlar") => "[]",
                (_, var v) when v.Contains("/iade-adaylari") =>
                    """[{"id":"h1","tarih":"2026-09-20T10:00:00Z","tlTutari":10000,"urunId":"u1","urunAdi":"Burma Bilezik","toplamAdet":2,"iadeEdilenAdet":0,"kalanAdet":2,"cariId":"c1"}]""",
                (_, var v) when v.Contains("/reyon") => "[]",
                (_, var v) when v.Contains("/islemler/personeller") => """{"satirlar":[],"toplam":0,"limit":50,"sinirAsildi":false}""",
                ("GET", var v) when v.StartsWith("/islemler/rezervler") =>
                    """{"satirlar":[{"id":"r1","urunId":"u1","cariId":"c1","siparisId":null,"adet":2,"durum":"AKTIF","bitisGunu":"2099-01-01","aciklama":"vitrin arkası","kapanisGerekcesi":null,"suresiDoldu":false,"duzenlenebilir":true}],"toplam":1,"limit":50,"sinirAsildi":false}""",
                ("POST", "/islemler/rezervler") => """{"id":"r2","adet":2,"stokAdet":5,"blokeAdet":2,"satilabilirAdet":3}""",
                ("PATCH", var v) when v.StartsWith("/islemler/rezervler/") => """{"id":"r1","adet":3,"satilabilirAdet":2}""",
                ("POST", var v) when v.EndsWith("/iptal") => """{"id":"r1","durum":"IPTAL","satilabilirAdet":5}""",
                ("GET", var v) when v.StartsWith("/islemler/avans/") => """{"cariId":"c1","acikAvans":1500}""",
                ("POST", "/islemler/avans") => """{"hareketId":"a1","cariId":"c1","tutar":1000,"acikAvans":2500}""",
                ("POST", "/islemler/avans/mahsup") => """{"hareketId":"a2","satisHareketId":"h1","cariId":"c1","mahsupTutar":500,"acikAvans":2000}""",
                ("POST", "/islemler/avans/iade") => """{"hareketId":"a3","cariId":"c1","tutar":300,"acikAvans":1700}""",
                ("GET", var v) when v.StartsWith("/islemler/konsinye-acik") =>
                    """{"satirlar":[{"id":"k1","cariId":"c1","urunId":"u1","tarih":"2026-09-20T10:00:00Z","aciklama":null,"cikanAdet":3,"iadeEdilenAdet":1,"acikAdet":2}],"toplam":1,"limit":50,"sinirAsildi":false}""",
                ("POST", "/islemler/konsinye-cikis") => """{"hareketId":"k2","adet":1,"satilabilirAdet":4}""",
                ("POST", "/islemler/konsinye-iade") => """{"hareketId":"k3","cikisHareketId":"k1","adet":1,"kalanAcikAdet":1}""",
                ("POST", "/islemler/degisim") => """{"fisId":"f9","farkTl":10000,"farkYonu":"MUSTERI_ODER","kalanIadeHakki":1}""",
                ("GET", var v) when v.StartsWith("/siparisler?") =>
                    """{"limit":50,"toplam":1,"dahaFazlasiVar":false,"siparisler":[{"id":"sp1","no":"SIP-000007","durum":"HAZIR","cariId":"c1","cariAd":"Ayşe Demir"}]}""",
                ("GET", "/siparisler/sp1") =>
                    """{"id":"sp1","no":"SIP-000007","durum":"HAZIR","cariId":"c1","kalemler":[{"id":"sk1","urunId":"u1","urunAd":"Burma Bilezik","tanim":null,"gramAgirlik":10,"adet":1}]}""",
                ("POST", var v) when v.EndsWith("/satisa-cevir") => """{"siparisId":"sp1","siparisNo":"SIP-000007","fisId":"f10","kalemSayisi":1,"avansMahsupTl":0,"acikAvans":0}""",
                ("GET", var v) when v.StartsWith("/evraklar?durum=PORTFOYDE") =>
                    """{"toplamAdet":1,"toplamTutar":5000,"limit":50,"azamiLimit":200,"offset":0,"dahaFazlasiVar":false,"portfoydeTutar":5000,"tahsileVerilenTutar":0,"buHaftaVadeliTutar":0,"karsiliksizTutar":0,"satirlar":[{"id":"e1","evrakTuru":"CEK","yon":"ALACAK","durum":"PORTFOYDE","tutar":5000,"paraBirimi":"TRY","vadeGunu":"2026-12-01","evrakNo":"CK-001","kaynakCariId":"c1","kaynakCariAd":"Ayşe Demir","gecikmeGun":0,"gecikmis":false,"yakindaVadeli":false,"duzenlenebilir":true,"izinliGecisler":["TAHSILE_VERILDI","CIRO_EDILDI"]}]}""",
                ("GET", var v) when v.StartsWith("/evraklar?durum=CIRO_EDILDI") =>
                    """{"toplamAdet":1,"toplamTutar":7000,"limit":50,"azamiLimit":200,"offset":0,"dahaFazlasiVar":false,"portfoydeTutar":0,"tahsileVerilenTutar":0,"buHaftaVadeliTutar":0,"karsiliksizTutar":0,"satirlar":[{"id":"e2","evrakTuru":"SENET","yon":"ALACAK","durum":"CIRO_EDILDI","tutar":7000,"paraBirimi":"TRY","vadeGunu":"2026-11-01","evrakNo":"SN-002","gecikmeGun":0,"gecikmis":false,"yakindaVadeli":false,"duzenlenebilir":false,"izinliGecisler":["KARSILIKSIZ"]}]}""",
                ("POST", var v) when v.StartsWith("/evraklar/") => "{}",
                (_, var v) when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Ayşe Demir"},{"id":"c2","ad":"Kuyumcu Ali"}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod, string rol = "SAHIP")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#islem-tur-SATIS");
        return e;
    }

    // TEMA TEMİZLİĞİ 17.10: İşlemler SATIŞ'ta Yıldız tezgâhıyla açılır (Kaydet #yildiz-tezgah-kaydet). "Diğer" menüsü
    // (FisDigerIslemler — #islem-diger-* AYNI) Yıldız'da tür sekmelerinin "Diğer"ine basınca görünür
    // (#yildiz-tezgah-tur → .yz-turler-acik); testler kullanıcının bu yolunu izler.
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Panel(string tur, string rol = "SAHIP")
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel, rol).YildizHazir();   // kur + listeler yüklendi
        e.YildizTurlerAc();
        e.Find($"#islem-diger-{tur.ToLowerInvariant()}").Click();
        e.WaitForElement("#islem-diger-panel");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-diger-kur-yok")));   // kur geldi → yazma düğmeleri açık
        return e;
    }
    private (string Metot, string Yol, JsonElement Govde) Son(string metot, string yol)
    {
        lock (istekler)
        {
            var s = istekler.LastOrDefault(i => i.Metot == metot && i.Yol == yol);
            Assert.False(s.Yol is null, $"{metot} {yol} isteği atılmadı. Atılanlar: {string.Join(" | ", istekler.Select(i => $"{i.Metot} {i.Yol}"))}");
            return (s.Metot, s.Yol, JsonDocument.Parse(string.IsNullOrEmpty(s.Govde) ? "{}" : s.Govde).RootElement.Clone());
        }
    }

    private int Say(string metot, string yol) { lock (istekler) return istekler.Count(i => i.Metot == metot && i.Yol == yol); }

    // ── (b) basit mod ────────────────────────────────────────────────────────

    [Fact]
    public void Basit_modda_Diger_menusu_ve_paneli_DOMda_yok()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        Assert.Empty(e.FindAll("#islem-diger-ac"));
        Assert.Empty(e.FindAll("#islem-diger-menu"));
        Assert.Empty(e.FindAll("#islem-diger-panel"));
    }

    [Fact]
    public void Pro_modda_Diger_menusu_alti_islemi_tasir_ve_panel_fisi_gizler()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        Assert.Contains(M.MenuAc, e.Find("#islem-diger-ac").TextContent);
        foreach (var tur in DigerTurleri.Tumu)
            Assert.False(e.Find($"#islem-diger-{tur.ToLowerInvariant()}").HasAttribute("disabled"), $"{tur} SAHIP için kilitli");
        e.YildizHazir();   // TEMA TEMİZLİĞİ 17.10: satış fişi Yıldız tezgâhı

        e.YildizTurlerAc();
        e.Find("#islem-diger-rezerv").Click();
        e.WaitForElement("#islem-diger-panel");
        Assert.Empty(e.FindAll("#islem-kaydet, #yildiz-tezgah, #yildiz-tezgah-kaydet"));   // tek ekran, tek birincil iş: fiş gizli

        // sekmeye dönüş paneli kapatır (Yıldız: "Diğer" listesinden ürün alışı — PTezgahTurKimlikTestleri.TK_14)
        if (!e.Find(".islem-sayfa").ClassList.Contains("yz-turler-acik")) e.Find("#yildiz-tur-diger").Click();
        e.WaitForElement("#islem-tur-ALIS").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-diger-panel")));
        Assert.NotNull(e.Find("#islem-kaydet"));
    }

    [Fact]
    public void Fise_don_dugmesi_paneli_kapatir()
    {
        var e = Panel(DigerTurleri.Konsinye);
        e.Find("#islem-diger-kapat").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-diger-panel")));
        e.WaitForElement("#yildiz-tezgah-kaydet");   // satış fişine (Yıldız tezgâhı) dönüldü
    }

    [Fact]
    public void Tezgahtar_icin_konsinye_ve_cek_devri_kilitli_ve_sebebini_yazar()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel, "TEZGAHTAR");
        Assert.True(e.Find("#islem-diger-konsinye").HasAttribute("disabled"));
        Assert.Equal(M.YetkiSahipMudur, e.Find("#islem-diger-konsinye-kilit").TextContent.Trim());
        Assert.True(e.Find("#islem-diger-evrak").HasAttribute("disabled"));
        Assert.Equal(M.YetkiSahipMudurMuhasebe, e.Find("#islem-diger-evrak-kilit").TextContent.Trim());
        Assert.False(e.Find("#islem-diger-rezerv").HasAttribute("disabled"));
        Assert.False(e.Find("#islem-diger-avans").HasAttribute("disabled"));
    }

    // ── Ürün ayır (rezerv) ───────────────────────────────────────────────────

    [Fact]
    public void Rezerv_bos_gonderimde_istek_atmaz_alan_altina_yazar()
    {
        var e = Panel(DigerTurleri.Rezerv);
        e.Find("#rezerv-kaydet").Click();
        Assert.Equal(0, Say("POST", "/islemler/rezervler"));
        Assert.Equal(M.ZorunluAlan, e.Find("#rezerv-urun-hata").TextContent.Trim());
        Assert.Equal(M.ZorunluAlan, e.Find("#rezerv-cari-hata").TextContent.Trim());
    }

    [Fact]
    public void Rezerv_acar_listeyi_yukler_ve_sunucu_sonucunu_banda_yazar()
    {
        var e = Panel(DigerTurleri.Rezerv);
        Assert.True(Say("GET", "/islemler/rezervler?durum=AKTIF&limit=50") >= 1);
        e.WaitForElement("#rezerv-satir-r1");

        SeciciYardim.Sec(e, "rezerv-urun", "u1");
        SeciciYardim.Sec(e, "rezerv-cari", "c1");
        e.Find("#rezerv-adet").Input("2");
        e.Find("#rezerv-kaydet").Click();

        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/islemler/rezervler")));
        var g = Son("POST", "/islemler/rezervler").Govde;
        Assert.Equal("u1", g.GetProperty("urunId").GetString());
        Assert.Equal("c1", g.GetProperty("cariId").GetString());
        Assert.Equal(2m, g.GetProperty("adet").GetDecimal());
        e.WaitForAssertion(() => Assert.Contains(string.Format(M.RezervBasari, "2", "3"), e.Find("#islem-diger-basari").TextContent));
    }

    [Fact]
    public void Rezerv_duzenle_yalniz_degisen_alani_PATCH_eder_bosaltilan_null_gider()
    {
        var e = Panel(DigerTurleri.Rezerv);
        e.WaitForElement("#rezerv-duzenle-r1").Click();
        e.Find("#rezerv-duz-adet").Input("3");
        e.Find("#cekmece-Rezervasyon-kaydet").Click();

        e.WaitForAssertion(() => Assert.Equal(1, Say("PATCH", "/islemler/rezervler/r1")));
        var g = Son("PATCH", "/islemler/rezervler/r1").Govde;
        Assert.Equal(3m, g.GetProperty("adet").GetDecimal());
        Assert.False(g.TryGetProperty("aciklama", out _), "değişmeyen açıklama gövdeye girdi — PATCH 'yoksa koru' sözleşmesi");
        Assert.False(g.TryGetProperty("bitisGunu", out _), "değişmeyen son gün gövdeye girdi");
        e.WaitForAssertion(() => Assert.Contains(string.Format(M.RezervGuncellendi, "2"), e.Find("#islem-diger-basari").TextContent));

        // Açıklama boşaltılırsa AÇIKÇA null gider (null = sil).
        e.WaitForElement("#rezerv-duzenle-r1").Click();
        e.Find("#rezerv-duz-aciklama").Input("");
        e.Find("#cekmece-Rezervasyon-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(2, Say("PATCH", "/islemler/rezervler/r1")));
        var g2 = Son("PATCH", "/islemler/rezervler/r1").Govde;
        Assert.Equal(JsonValueKind.Null, g2.GetProperty("aciklama").ValueKind);
        Assert.False(g2.TryGetProperty("adet", out _));
    }

    [Fact]
    public void Rezerv_kaldir_gerekce_ve_ortak_onay_ister()
    {
        var e = Panel(DigerTurleri.Rezerv);
        e.WaitForElement("#rezerv-kaldir-r1").Click();
        e.Find("#rezerv-kaldir-devam").Click();
        Assert.Contains(string.Format(M.GerekceKisa, 10), e.Find("#rezerv-kaldir-gerekce-hata").TextContent);
        Assert.Empty(e.FindAll("#islem-diger-onay-evet"));

        e.Find("#rezerv-kaldir-gerekce").Input("Müşteri vazgeçti, aradı");
        e.Find("#rezerv-kaldir-devam").Click();
        Assert.Equal(0, Say("POST", "/islemler/rezervler/r1/iptal"));   // onay gelmeden istek yok
        e.WaitForElement("#islem-diger-onay-evet").Click();

        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/islemler/rezervler/r1/iptal")));
        Assert.Equal("Müşteri vazgeçti, aradı", Son("POST", "/islemler/rezervler/r1/iptal").Govde.GetProperty("gerekce").GetString());
        e.WaitForAssertion(() => Assert.Contains(string.Format(M.RezervKaldirildi, "5"), e.Find("#islem-diger-basari").TextContent));
    }

    [Fact]
    public void Tezgahtar_rezervi_kaldiramaz_dugme_kilitli_sebebi_yazili()
    {
        var e = Panel(DigerTurleri.Rezerv, "TEZGAHTAR");
        Assert.True(e.WaitForElement("#rezerv-kaldir-r1").HasAttribute("disabled"));
        Assert.Contains(M.YetkiSahipMudur, e.Find("#rezerv-kaldir-kilit").TextContent);
        Assert.False(e.Find("#rezerv-duzenle-r1").HasAttribute("disabled"));   // düzenleme TEZGAHTAR'a açık (sunucu aynası)
    }

    [Fact]
    public void Liste_hatasi_bos_duruma_cevrilmez()
    {
        hataliYollar.Add("/islemler/rezervler?");
        var e = Panel(DigerTurleri.Rezerv);
        e.WaitForAssertion(() => Assert.Contains("Liste alınamadı", e.Find("#islem-diger-liste-hata").TextContent));
        Assert.DoesNotContain(M.RezervBos, e.Markup);
    }

    // ── Kapora (avans) ───────────────────────────────────────────────────────

    [Fact]
    public void Kapora_al_mahsup_ve_iade_uclari_ekrandan_cagrilir()
    {
        var e = Panel(DigerTurleri.Avans);
        SeciciYardim.Sec(e, "avans-cari", "c1");
        e.WaitForAssertion(() => Assert.Equal(1, Say("GET", "/islemler/avans/c1")));
        e.WaitForAssertion(() => Assert.Contains(string.Format(M.AcikAvans, Bicim.Para(1500m, "TRY")), e.Find("#avans-acik").TextContent));
        Assert.NotNull(e.Find("#avans-kasa-tek"));   // UX-S22: tek kasa sorulmaz

        e.Find("#avans-tutar").Input("1000");
        e.Find("#avans-al").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/islemler/avans")));
        var g = Son("POST", "/islemler/avans").Govde;
        Assert.Equal("c1", g.GetProperty("cariId").GetString());
        Assert.Equal(1000m, g.GetProperty("tutar").GetDecimal());
        Assert.Equal("NAKIT", g.GetProperty("odemeTuru").GetString());
        Assert.Equal("ks1", g.GetProperty("kasaId").GetString());
        Assert.Equal("kur-1", g.GetProperty("kurSnapshotId").GetString());
        e.WaitForAssertion(() => Assert.Contains(string.Format(M.AvansBasari, Bicim.Para(1000m, "TRY"), Bicim.Para(2500m, "TRY")), e.Find("#islem-diger-basari").TextContent));

        SeciciYardim.Sec(e, "avans-mahsup-satis", "h1");
        e.Find("#avans-mahsup-tutar").Input("500");
        e.Find("#avans-mahsup").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/islemler/avans/mahsup")));
        var gm = Son("POST", "/islemler/avans/mahsup").Govde;
        Assert.Equal("h1", gm.GetProperty("satisHareketId").GetString());
        Assert.Equal(500m, gm.GetProperty("tutar").GetDecimal());

        e.Find("#avans-iade-tutar").Input("300");
        e.Find("#avans-iade").Click();
        Assert.Equal(0, Say("POST", "/islemler/avans/iade"));   // gerekçe yok → istek yok
        Assert.Contains(string.Format(M.GerekceKisa, 10), e.Find("#avans-iade-gerekce-hata").TextContent);
        e.Find("#avans-iade-gerekce").Input("Düğün iptal oldu");
        e.Find("#avans-iade").Click();
        e.WaitForElement("#islem-diger-onay-evet").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/islemler/avans/iade")));
        var gi = Son("POST", "/islemler/avans/iade").Govde;
        Assert.Equal(300m, gi.GetProperty("tutar").GetDecimal());
        Assert.Equal("Düğün iptal oldu", gi.GetProperty("gerekce").GetString());
    }

    [Fact]
    public void Tezgahtar_kaporayi_geri_odeyemez_dugme_kilitli_sebebi_yazili()
    {
        var e = Panel(DigerTurleri.Avans, "TEZGAHTAR");
        Assert.True(e.Find("#avans-iade").HasAttribute("disabled"));
        Assert.Equal(M.YetkiSahipMudur, e.Find("#avans-iade-kilit").TextContent.Trim());
    }

    // ── Konsinye ─────────────────────────────────────────────────────────────

    [Fact]
    public void Konsinye_gonder_ve_geri_al_uclari_ekrandan_cagrilir()
    {
        var e = Panel(DigerTurleri.Konsinye);
        e.WaitForElement("#konsinye-satir-k1");
        SeciciYardim.Sec(e, "konsinye-cari", "c2");
        SeciciYardim.Sec(e, "konsinye-urun", "u2");
        e.Find("#konsinye-gonder").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/islemler/konsinye-cikis")));
        var g = Son("POST", "/islemler/konsinye-cikis").Govde;
        Assert.Equal("c2", g.GetProperty("cariId").GetString());
        Assert.Equal("u2", g.GetProperty("urunId").GetString());
        Assert.Equal(1m, g.GetProperty("adet").GetDecimal());

        // Dönmemişten fazlası geri alınamaz → istek yok, sebep alan altında.
        e.Find("#konsinye-geri-adet-k1").Input("5");
        e.Find("#konsinye-geri-al-k1").Click();
        Assert.Equal(0, Say("POST", "/islemler/konsinye-iade"));
        Assert.NotNull(e.Find("#konsinye-geri-adet-k1-hata"));

        e.Find("#konsinye-geri-adet-k1").Input("1");
        e.Find("#konsinye-geri-al-k1").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/islemler/konsinye-iade")));
        var gi = Son("POST", "/islemler/konsinye-iade").Govde;
        Assert.Equal("k1", gi.GetProperty("cikisHareketId").GetString());
        Assert.Equal(1m, gi.GetProperty("adet").GetDecimal());
        e.WaitForAssertion(() => Assert.Contains(string.Format(M.KonsinyeIadeBasari, "1", "1"), e.Find("#islem-diger-basari").TextContent));
    }

    // ── Değişim ──────────────────────────────────────────────────────────────

    [Fact]
    public void Degisim_farki_kartla_kapanmaz_ve_govde_dogru_gider()
    {
        var e = Panel(DigerTurleri.Degisim);
        var secenekler = SeciciYardim.Secenekler(e, "degisim-fark").Select(o => o.TextContent.Trim()).Where(t => t.Length > 0).ToList();
        Assert.DoesNotContain(M.Kart, secenekler);
        Assert.Equal(2, secenekler.Count);   // yalnız NAKIT + açık hesap — üçüncü bir yol (KART) ekrana sızmaz
        Assert.Contains(M.Nakit, secenekler);
        Assert.Contains(M.AcikHesap, secenekler);

        SeciciYardim.Sec(e, "degisim-kaynak", "h1");
        SeciciYardim.Sec(e, "degisim-yeni-urun", "u2");
        e.Find("#degisim-yeni-fiyat").Input("20000");
        e.Find("#degisim-kaydet").Click();

        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/islemler/degisim")));
        var g = Son("POST", "/islemler/degisim").Govde;
        Assert.Equal("h1", g.GetProperty("kaynakHareketId").GetString());
        Assert.Equal("u2", g.GetProperty("yeniUrunId").GetString());
        Assert.Equal(20000m, g.GetProperty("yeniBirimFiyatTl").GetDecimal());
        Assert.Equal("NAKIT", g.GetProperty("farkOdemeTuru").GetString());
        Assert.Equal("ks1", g.GetProperty("kasaId").GetString());
        e.WaitForAssertion(() => Assert.Contains(string.Format(M.DegisimBasari, Bicim.Para(10000m, "TRY"), M.FarkMusteriOder), e.Find("#islem-diger-basari").TextContent));
    }

    // ── Siparişten satış ─────────────────────────────────────────────────────

    [Fact]
    public void Siparisten_satis_kalem_fiyatli_govde_ve_onayla_POST_eder()
    {
        var e = Panel(DigerTurleri.Siparis);
        Assert.True(Say("GET", "/siparisler?durum=HAZIR&limit=50") >= 1);
        SeciciYardim.Sec(e, "siparis-sec", "sp1");
        e.WaitForAssertion(() => Assert.Equal(1, Say("GET", "/siparisler/sp1")));
        e.WaitForElement("#siparis-fiyat-sk1").Input("12500");
        e.Find("#siparis-cevir").Click();
        Assert.Equal(0, Say("POST", "/siparisler/sp1/satisa-cevir"));   // onay gelmeden istek yok
        e.WaitForElement("#islem-diger-onay-evet").Click();

        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/siparisler/sp1/satisa-cevir")));
        var g = Son("POST", "/siparisler/sp1/satisa-cevir").Govde;
        var kalem = g.GetProperty("kalemler")[0];
        Assert.Equal("sk1", kalem.GetProperty("siparisKalemId").GetString());
        Assert.Equal(12500m, kalem.GetProperty("birimFiyatTl").GetDecimal());
        var odeme = g.GetProperty("odemeler")[0];
        Assert.Equal("NAKIT", odeme.GetProperty("arac").GetString());
        Assert.Equal(12500m, odeme.GetProperty("tutarTl").GetDecimal());
        Assert.Equal("ks1", odeme.GetProperty("kasaId").GetString());
        Assert.Equal(JsonValueKind.Null, g.GetProperty("avansMahsupTl").ValueKind);   // sessiz mahsup yok
        e.WaitForAssertion(() => Assert.Contains("SIP-000007", e.Find("#islem-diger-basari").TextContent));
    }

    // ── Çek/senet devri (ciro + rücu) ────────────────────────────────────────

    [Fact]
    public void Cek_devri_ve_karsiliksiz_donen_uclari_onayla_cagrilir()
    {
        var e = Panel(DigerTurleri.Evrak);
        SeciciYardim.Sec(e, "evrak-ciro-evrak", "e1");
        SeciciYardim.Sec(e, "evrak-ciro-cari", "c2");
        e.Find("#evrak-ciro").Click();
        e.WaitForElement("#islem-diger-onay-evet").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/evraklar/e1/ciro")));
        var g = Son("POST", "/evraklar/e1/ciro").Govde;
        Assert.Equal("c2", g.GetProperty("ciroCariId").GetString());
        Assert.Equal("kur-1", g.GetProperty("kurSnapshotId").GetString());

        SeciciYardim.Sec(e, "evrak-rucu-evrak", "e2");
        e.Find("#evrak-rucu-aciklama").Input("kısa");
        e.Find("#evrak-rucu").Click();
        Assert.Equal(0, Say("POST", "/evraklar/e2/rucu"));
        Assert.Contains(string.Format(M.GerekceKisa, 20), e.Find("#evrak-rucu-aciklama-hata").TextContent);
        e.Find("#evrak-rucu-aciklama").Input("Banka karşılıksız damgası vurdu, iade");
        e.Find("#evrak-rucu").Click();
        e.WaitForElement("#islem-diger-onay-evet").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/evraklar/e2/rucu")));
        Assert.Equal("Banka karşılıksız damgası vurdu, iade", Son("POST", "/evraklar/e2/rucu").Govde.GetProperty("aciklama").GetString());
    }

    // ── (a) yapısal: P-21 uçlarının HER BİRİ ekran katmanında çağrılır ───────

    [Fact]
    public void P21_uclarinin_her_biri_ekran_istek_katmaninda_var()
    {
        var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Pages", "Islemler.Diger.cs");
        var asm = typeof(Kuyumcu.Api.Modules.Satis.TicaretController).Assembly;
        var uclar = new List<string>();
        foreach (var (tip, onek) in new[]
                 {
                     (typeof(Kuyumcu.Api.Modules.Satis.TicaretController), "/islemler/"),
                     (asm.GetType("Kuyumcu.Api.Modules.Evrak.EvrakController")!, "/evraklar/"),
                     (asm.GetType("Kuyumcu.Api.Modules.Siparis.SiparisController")!, "/siparisler/"),
                 })
        {
            foreach (var yontem in tip.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var ozet = yontem.GetCustomAttributes<HttpMethodAttribute>().FirstOrDefault();
                if (ozet?.Template is not { } sablon) continue;
                // Evrak/Sipariş denetleyicilerinde YALNIZ P-21 uçları bu paketin konusudur.
                if (tip != typeof(Kuyumcu.Api.Modules.Satis.TicaretController) && sablon is not ("{id}/ciro" or "{id}/rucu" or "{id}/satisa-cevir"))
                    continue;
                uclar.Add(ozet.HttpMethods.First() + " " + onek + sablon);
            }
        }
        Assert.Equal(15, uclar.Count);   // TicaretController 12 + ciro + rücu + satışa çevir
        foreach (var uc in uclar)
        {
            var yol = uc[(uc.IndexOf(' ') + 1)..];
            var ilk = yol.Split('{')[0].TrimEnd('/');
            var son = yol.Contains('}') ? yol[(yol.LastIndexOf('}') + 1)..] : "";
            Assert.True(kaynak.Contains(ilk, StringComparison.Ordinal) && (son.Length == 0 || kaynak.Contains(son + "\"", StringComparison.Ordinal) || kaynak.Contains(son + "?", StringComparison.Ordinal)),
                $"{uc} ekrandan çağrılmıyor (UX-S25 'uç var düğme yok')");
        }
    }

    [Fact]
    public void Fis_Diger_bilesenleri_istek_atmaz_istek_katmani_sayfada()
    {
        var klasor = CssKaynak.Yol("Kuyumcu.Web", "Bilesenler", "Fis");
        var dosyalar = Directory.GetFiles(klasor, "FisDiger*").ToList();
        Assert.True(dosyalar.Count >= 8, "FisDiger bileşenleri eksik");
        foreach (var d in dosyalar)
        {
            var kod = string.Join('\n', File.ReadAllLines(d).Where(s => !s.TrimStart().StartsWith("//")));
            kod = System.Text.RegularExpressions.Regex.Replace(kod, @"@\*.*?\*@", "", System.Text.RegularExpressions.RegexOptions.Singleline);
            foreach (var yasak in new[] { "ApiIstemci", "HttpClient", "PostAsync", "PatchAsync", "GetAsync", "Api." })
                Assert.False(kod.Contains(yasak, StringComparison.Ordinal), $"{Path.GetFileName(d)} '{yasak}' içeriyor");
        }
    }

    // ── saf ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Rezerv_duzenleme_govdesi_yalniz_degisen_alanlari_tasir()
    {
        var d = new RezervDuzenlemesi();
        d.Ac(new RezervSatiri("r1", "u1", "c1", null, 2m, "AKTIF", new DateOnly(2099, 1, 1), "not", null, false, true));
        Assert.Empty(d.Govde());
        d.BitisGunu = null;
        var g = d.Govde();
        Assert.Single(g);
        Assert.Null(g["bitisGunu"]);
    }

    [Fact]
    public void Degisim_fark_turlerinde_KART_yok()
    {
        Assert.Equal(["NAKIT", "VERESIYE"], DegisimGirisi.FarkTurleri);
    }

    [Fact]
    public void Sozluk_TR_ve_EN_tum_anahtarlari_dolu()
    {
        foreach (var m in new[] { FisDigerSozlugu.Tr, FisDigerSozlugu.En })
            foreach (var p in typeof(FisDigerMetinleri).GetProperties().Where(p => p.PropertyType == typeof(string)))
                Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(m)), $"{p.Name} boş");
    }

    // ── SATIS-3 t10 (tasarım hakemi KRİTİK): Diğer formları ana türlerin Kaydet kalıbında ─────────
    [Fact]
    public void SATIS3_T10_Rezerv_eksik_alanda_birincil_PASIF_sebep_eksik_alanlari_soyler_dolunca_ACILIR()
    {
        var e = Panel(DigerTurleri.Rezerv);
        var d = e.Find("#rezerv-kaydet");
        Assert.True(d.HasAttribute("disabled"));
        Assert.Contains("btn-buyuk", d.ClassName);
        var kilit = e.Find("#rezerv-kaydet-kilit").TextContent;
        Assert.Contains(M.Urun, kilit);
        Assert.Contains(M.Musteri, kilit);
        Assert.Equal(Satis3Sozlugu.Tr.UrunSecin, e.Find("#rezerv-urun").TextContent.Trim());   // "Henüz kayıt seçilmedi." değil
        SeciciYardim.Sec(e, "rezerv-urun", "u1");
        SeciciYardim.Sec(e, "rezerv-cari", "c1");
        e.WaitForAssertion(() => Assert.False(e.Find("#rezerv-kaydet").HasAttribute("disabled")));
        Assert.Empty(e.FindAll("#rezerv-kaydet-kilit"));
    }

    // ══ SATIS-3 t17 — "EKRANDA GÖRÜNEN ≠ GÖNDERİLEN" hata sınıfı, Diğer işlem formları: kutu tuş tuş "1.500"
    //    (ara değerler 1 → 1,5 → 1,50 → belirsiz) yazılınca formun birincil düğmesi PASİF, sebep kutunun etiketi,
    //    tıklama istek ATMAZ; gövdeye ara değer/eski değer gitmez. ══
    private static readonly string[] TusTus1500 = ["1", "1.", "1.5", "1.50", "1.500"];

    private static void TusTus(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string id)
    {
        foreach (var q in TusTus1500) e.Find($"#{id}").Input(q);
    }

    private static void DugmeKilitli(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string kutu, string dugme, string etiket)
    {
        e.WaitForAssertion(() => Assert.Equal("true", e.Find($"#{kutu}").GetAttribute("aria-invalid")));
        e.WaitForAssertion(() => Assert.True(e.Find($"#{dugme}").HasAttribute("disabled")));
        Assert.Equal(string.Format(Satis3Sozlugu.Tr.AlanDuzeltin, etiket), e.Find($"#{dugme}-kilit").TextContent.Trim());
        Assert.Equal("1.500", e.Find($"#{kutu}").GetAttribute("value"));
    }

    [Fact]
    public void T17_Kapora_tutari_tus_tus_1_500_Al_PASIF_POST_YOK()
    {
        var e = Panel(DigerTurleri.Avans);
        SeciciYardim.Sec(e, "avans-cari", "c1");
        e.WaitForAssertion(() => Assert.Equal(1, Say("GET", "/islemler/avans/c1")));
        TusTus(e, "avans-tutar");
        DugmeKilitli(e, "avans-tutar", "avans-al", M.Tutar);
        e.Find("#avans-al").Click();
        Assert.Equal(0, Say("POST", "/islemler/avans"));
        e.Find("#avans-tutar").Input("1,5");   // düzeltince görünen = giden
        e.WaitForAssertion(() => Assert.False(e.Find("#avans-al").HasAttribute("disabled")));
        e.Find("#avans-al").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/islemler/avans")));
        Assert.Equal(1.5m, Son("POST", "/islemler/avans").Govde.GetProperty("tutar").GetDecimal());
    }

    [Fact]
    public void T17_Siparis_kapora_tus_tus_1_500_Cevir_PASIF_onay_ACILMAZ_POST_YOK()
    {
        var e = Panel(DigerTurleri.Siparis);
        SeciciYardim.Sec(e, "siparis-sec", "sp1");
        e.WaitForElement("#siparis-fiyat-sk1").Input("12500");
        TusTus(e, "siparis-kapora");
        DugmeKilitli(e, "siparis-kapora", "siparis-cevir", M.DusulecekKapora);
        e.Find("#siparis-cevir").Click();
        Assert.Empty(e.FindAll("#islem-diger-onay-evet"));
        Assert.Equal(0, Say("POST", "/siparisler/sp1/satisa-cevir"));
    }

    [Fact]
    public void T17_Degisim_yeni_fiyat_tus_tus_1_500_Kaydet_PASIF_POST_YOK()
    {
        var e = Panel(DigerTurleri.Degisim);
        SeciciYardim.Sec(e, "degisim-kaynak", "h1");
        SeciciYardim.Sec(e, "degisim-yeni-urun", "u2");
        TusTus(e, "degisim-yeni-fiyat");
        DugmeKilitli(e, "degisim-yeni-fiyat", "degisim-kaydet", M.YeniFiyat);
        e.Find("#degisim-kaydet").Click();
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.Contains("degisim"));
    }
}
