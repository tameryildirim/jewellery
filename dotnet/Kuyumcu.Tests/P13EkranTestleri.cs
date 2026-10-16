using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-13 — EKRAN TESTLERİ [bUnit]
// Kimlikler: B-39 (cari) · B-40 (kategori) · B-47 (kur kaynağı / maliyet
//            yöntemi) · B-48 (/yedekler iptal gerekçesi) · B-12 (/uyum ek beyan)
//
// SÖZLEŞME: "sunucuda uç var, ekranda düğme yok" KABUL EDİLMEZ (UX-S25) ve
// "tıklanınca hiçbir şey olmayan düğme" paketi geçirmez (DoD EK md.1).
// Bu yüzden her test DÜĞMENİN VARLIĞINI DEĞİL, gittiği UCU ve GÖVDEYİ ölçer.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P13EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public P13EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<UyumServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    // ── Sahte API ────────────────────────────────────────────────────────────
    // ⚠ '{}' varsayılanı bırakılan uç, ekranı sessizce boş bırakır ve test
    //   "geçmiş gibi" görünür. Bu pakette DOKUNULAN her uca gerçekçi yanıt verilir.
    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            var g = yol switch
            {
                // ── cari
                var y when y.EndsWith("/bagli-kayitlar") && y.Contains("/cari/") =>
                    """{"cariId":"c1","aktif":true,"hareketSayisi":7,"tlBakiye":-1500.00,"hasGramBakiye":2.5,"dovizBakiyeVar":false}""",
                var y when y.EndsWith("/cari/c1") =>
                    """{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567","adres":"Şişli"}""",
                var y when y.EndsWith("/cari") =>
                    """[{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567","tlBakiye":-1500.00,"hasGramBakiye":2.5,"aktif":true}]""",
                var y when y.EndsWith("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var y when y.Contains("/fiyat/listeler") =>
                    """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","aktif":true}]""",

                // ── ürün ağacı
                var y when y.EndsWith("/bagli-kayitlar") && y.Contains("/urun-kategorileri/") =>
                    """{"kategoriId":"k1","aktif":true,"altKategoriSayisi":0,"urunSayisi":3}""",
                var y when y.EndsWith("/urun-kategorileri") =>
                    """[{"id":"k1","ad":"Ziynet","sira":0,"aktif":true,"urunSayisi":3,"altlar":[]}, {"id":"k2","ad":"Eski Grup","sira":1,"aktif":false,"urunSayisi":0,"altlar":[]}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",

                // ── işletme
                var y when y.EndsWith("/admin/tenant") =>
                    """{"id":"t1","ad":"Kuyumcu","plan":"PRO","aiEtkin":false,"aktif":true, "kurKaynagi":null,"maliyetYontemi":null,"varsayilanArayuzModu":"PRO"}""",
                var y when y.EndsWith("/admin/tenant/maliyet-yontemi") =>
                    """{"id":"t1","ad":"Kuyumcu","plan":"PRO","aiEtkin":false,"aktif":true, "kurKaynagi":null,"maliyetYontemi":"LIFO","varsayilanArayuzModu":"PRO"}""",
                var y when y.EndsWith("/admin/vezne") => """{"zorunlu":false}""",
                var y when y.EndsWith("/kur/kaynaklar") =>
                    """{"kaynaklar":["TRUNCGIL","ALTINKAYNAK","MANUEL"],"varsayilan":"TRUNCGIL","secili":null}""",

                // ── yedekler
                var y when y.EndsWith("/yedekler/ozet") =>
                    """{"sonYedek":null,"dogrulama":"YOK","dogrulamaTarih":null,"sonDogrulanmisYedek":null,"disKopya":"YOK","saklananKosu":0,"saklamaGun":30,"rpoDakikaOlculen":null,"rpoDakikaTaahhut":1440,"rtoDakikaTaahhut":240,"rtoDakikaOlculen":null,"sonTatbikat":null,"sonTatbikatSonuc":null,"azamiLimit":50,"sonBasariliTatbikat":null,"tatbikatYasGun":null,"tatbikatAzamiGun":30,"tatbikatBayat":true}""",
                var y when y.EndsWith("/yedekler/tatbikatlar") =>
                    """{"limit":12,"azamiLimit":50,"tatbikatlar":[]}""",
                var y when y.EndsWith("/yedekler/talepler") =>
                    """{"limit":20,"azamiLimit":50,"talepler":[ {"id":"t-1","gerekce":"Yıl sonu arşivi için tam yedek isteniyor","durum":"IPTAL", "olusturuldu":"2026-09-01T10:00:00Z","islendi":null,"yedekKaydiId":null, "iptalGerekce":"Talep yanlışlıkla iki kez açıldı, ikincisi iptal edildi"}]}""",
                var y when y.EndsWith("/yedekler") =>
                    """{"limit":20,"azamiLimit":50,"dahaFazlasiVar":false,"kayitlar":[]}""",

                // ── uyum / MASAK
                var y when y.EndsWith("/masak/ozet") =>
                    """{"acikBildirim":0,"suresiGecmis":0,"yaklasan":0,"son30GunTespit":0}""",
                var y when y.EndsWith("/masak/supheli") && istek.Method == HttpMethod.Get =>
                    """[{"id":"sb1","hareketId":null,"cariId":null,"kimlikTespitiId":null, "islemTutariTl":250000,"gerekce":"Uzun gerekçe metni", "tespitTarihi":"2026-09-01T09:00:00Z","sonBildirimTarihi":"2026-09-15T09:00:00Z", "kalanSaat":120,"durum":"GONDERILDI","gonderimTarihi":"2026-09-02T09:00:00Z", "gonderimReferansi":"MSK-2026-1","kapatmaGerekcesi":null, "bildirenKullaniciId":"u1","saklamaBitis":"2034-09-01T09:00:00Z", "olusturuldu":"2026-09-01T09:00:00Z","guncellendi":"2026-09-02T09:00:00Z", "duzeltilenBildirimId":null}]""",
                var y when y.EndsWith("/ek-beyan") =>
                    """{"id":"sb2","islemTutariTl":250000,"gerekce":"Ek beyan metni", "tespitTarihi":"2026-09-20T09:00:00Z","sonBildirimTarihi":"2026-10-04T09:00:00Z", "kalanSaat":300,"durum":"ACIK","bildirenKullaniciId":"u1", "saklamaBitis":"2034-09-20T09:00:00Z","olusturuldu":"2026-09-20T09:00:00Z", "guncellendi":"2026-09-20T09:00:00Z","duzeltilenBildirimId":"sb1"}""",

                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private bool Gitti(string metot, string yolParcasi) =>
        istekler.Any(i => i.Metot == metot && i.Yol.Contains(yolParcasi));

    // ═══ B-39 — cari pasife alma onayında BAĞLI KAYIT SAYISI ════════════════

    [Fact]
    public void Cari_PasifOnayi_BagliKayitSayisiniOkurVeGosterir()
    {
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("#cari-yeni-ac");
        istekler.Clear();

        // Düzenleme çekmecesi → pasife al → onay penceresi
        e.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();
        e.WaitForElement("#c-pasife-al").Click();

        e.WaitForAssertion(() => Assert.True(Gitti("GET", "/cari/c1/bagli-kayitlar")));
        var metin = e.WaitForElement("#c-pasif-bagli").TextContent;
        Assert.Contains("7", metin);            // hareket sayısı UYDURULMADI
    }

    // ═══ B-40 — kategori pasife alma / aktife alma ══════════════════════════

    [Fact]
    public void Kategori_PasifeAlDugmesi_BagliKaytlariOkurVeDeleteAtar()
    {
        var e = Render<Kuyumcu.Web.Pages.UrunAgaci>();
        e.WaitForElement("#k-pasife-k1");
        istekler.Clear();

        e.Find("#k-pasife-k1").Click();
        e.WaitForAssertion(() => Assert.True(Gitti("GET", "/urun-kategorileri/k1/bagli-kayitlar")));
        Assert.Contains("3", e.WaitForElement("#k-pasif-bagli").TextContent);

        e.Find("#k-pasif-onayla").Click();
        e.WaitForAssertion(() => Assert.True(Gitti("DELETE", "/urun-kategorileri/k1")));
    }

    [Fact]
    public void Kategori_PasifKayit_AktifeAlYoluGosterir()
    {
        var e = Render<Kuyumcu.Web.Pages.UrunAgaci>();
        e.WaitForElement("#k-aktife-al-k2");
        istekler.Clear();

        e.Find("#k-aktife-al-k2").Click();
        e.WaitForAssertion(() =>
        {
            var p = istekler.First(i => i.Metot == "PATCH" && i.Yol.Contains("/urun-kategorileri/k2"));
            Assert.Contains("\"aktif\":true", p.Govde.Replace(" ", ""));
        });
    }

    /// <summary>
    /// UX-S22: aynı soruya iki cevap olamaz — düzenleme çekmecesindeki
    /// "Aktif" işaret kutusu KALDIRILDI ve PATCH gövdesi artık aktifliğe
    /// dokunmuyor (PATCH sözleşmesi: gövdede yoksa koru).
    /// </summary>
    [Fact]
    public void Kategori_DuzenlemeKaydi_AktifligeDokunmaz()
    {
        var e = Render<Kuyumcu.Web.Pages.UrunAgaci>();
        e.WaitForElement("#k-pasife-k1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Düzenle").Click();
        e.WaitForElement("#k-ad");
        istekler.Clear();

        e.Find("#cekmece-kategori-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            var p = istekler.First(i => i.Metot == "PATCH" && i.Yol.Contains("/urun-kategorileri/k1"));
            Assert.DoesNotContain("aktif", p.Govde);
        });
    }

    // ═══ B-47 — kur kaynağı ve maliyet yöntemi BOŞKEN KİLİTLİ DEĞİL ═════════

    [Fact]
    public void Isletme_KurKaynagiVeMaliyetYontemi_BoskenSecilebilir()
    {
        var e = Render<Kuyumcu.Web.Pages.Isletme>();
        var kur = e.WaitForElement("#i-kur-kaynagi");
        var maliyet = e.Find("#i-maliyet-yontemi");

        // Kilit YOK ve seçenekler SUNUCUDAN geldi (ekrana gömülmedi).
        Assert.False(kur.HasAttribute("disabled"));
        Assert.False(maliyet.HasAttribute("disabled"));
        // KESIF-P1 (#71): seçenek ADI görünür (üst çubukla aynı çevirici), ham kod değil.
        Assert.Contains("Altınkaynak", SeciciYardim.ListeMetni(e, "i-kur-kaynagi"));
        Assert.DoesNotContain("ALTINKAYNAK", SeciciYardim.ListeMetni(e, "i-kur-kaynagi"));
        // Belirlenmemiş ayar SESSİZ kalmaz: hangi değerin kullanıldığı yazılı (UX-S21).
        Assert.NotNull(e.Find("#i-kur-kaynagi-varsayilan"));
        Assert.NotNull(e.Find("#i-maliyet-yontemi-varsayilan"));

        // KESIF-P1 (#12): seçim ANINDA yazmaz; "Değişikliği kaydet" + onay penceresi ister.
        istekler.Clear();
        SeciciYardim.Sec(e, "i-maliyet-yontemi", "LIFO");
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
        e.Find("#i-ayar-kaydet").Click();
        e.WaitForElement("#i-ayar-onay-onayla").Click();
        e.WaitForAssertion(() =>
        {
            var p = istekler.First(i => i.Yol.EndsWith("/admin/tenant/maliyet-yontemi"));
            Assert.Equal("PATCH", p.Metot);
            Assert.Contains("LIFO", p.Govde);
        });

        istekler.Clear();
        SeciciYardim.Sec(e, "i-kur-kaynagi", "ALTINKAYNAK");
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
        e.Find("#i-ayar-kaydet").Click();
        e.WaitForElement("#i-ayar-onay-onayla").Click();
        e.WaitForAssertion(() =>
        {
            // Yazma yolu MEVCUT uçtur; ikinci bir yazma yolu açılmadı.
            var p = istekler.First(i => i.Yol.EndsWith("/kur/kaynak"));
            Assert.Equal("PATCH", p.Metot);
            Assert.Contains("ALTINKAYNAK", p.Govde);
        });
    }

    // ═══ B-48 — /yedekler iptal gerekçesi EKRANDA OKUNUR ════════════════════

    [Fact]
    public void Yedekler_IptalGerekcesi_EkrandaGorunur()
    {
        var e = Render<Kuyumcu.Web.Pages.Yedekler>();
        var alan = e.WaitForElement("#yd-iptal-gerekce-t-1");
        Assert.Contains("iki kez açıldı", alan.TextContent);
    }

    // ═══ B-12 — /uyum GONDERILDI kaydının DÜZELTME YOLU ═════════════════════

    [Fact]
    public void Uyum_GonderilmisBildirim_EkBeyanYoluIleDuzeltilir()
    {
        var e = Render<Kuyumcu.Web.Pages.Uyum>();
        e.WaitForElement("#uy-sekme-risk");
        e.FindAll("[role=tab]").First(t => t.TextContent.Contains("Şüpheli")).Click();

        var dugme = e.WaitForElement("#uy-ek-beyan-sb1");
        dugme.Click();

        // Form AYNI formdur (UX-S26); gerekçe boş başlar, tutar aslından gelir.
        var gerekce = e.WaitForElement("#sb-gerekce");
        gerekce.Input(new string('ş', 320));
        istekler.Clear();
        e.Find("#sb-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var p = istekler.First(i => i.Yol.EndsWith("/masak/supheli/sb1/ek-beyan"));
            Assert.Equal("POST", p.Metot);
        });
    }
}
