using System.Net;
using Bunit;
using Kuyumcu.Api.Modules.Tercih;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-b — KATLANIR KENAR ÇUBUĞU (tuval Panel.dc.html <nav>)
//
// Kurucunun onayladığı taslakta menü 38 düz bağlantı değil, 6 KATLANIR gruptur.
// Katlamanın tek kabul ölçütü "göze az satır görünsün" DEĞİLDİR; tuval notu n2
// şunu söyler: «Menüde HİÇBİR ekran kaybolmadı». Bu dosya o cümlenin dört
// ölçülebilir karşılığını sınar:
//
//   (1) KAPALI GRUP KAYBETMEZ — kapalı grubun altındaki tek satır önizleme o
//       gruptaki EKRAN ADLARININ HEPSİNİ yazar; satırlar DOM'dan silinmez,
//       `hidden` ile katlanır (plan § 3 md.1 "özellik silinmez").
//   (2) VARSAYILAN DOĞRU     — kullanıcı hiç katlama yapmamışsa yalnız AKTİF
//       ROTANIN grubu açıktır; gerisi kapalıdır (plan § S-1 md. 1.1).
//   (3) MOD YETKİYİ GEVŞETMEZ — yetkisiz kullanıcının ekranı ne açık grupta,
//       ne kapalı grupta, NE DE ÖNİZLEME METNİNDE görünür (M-UX3-06).
//   (4) TERCİH KULLANICIYA ÖZEL — katlama sunucuya yazılır (/tercihler/benim),
//       yazma başarısızsa geri alınır ve bant çizilir (md.1).
//
// ⚠ DOM SÖZLEŞMESİ: mevcut id'ler DEĞİŞMEDİ (#kenar-grup-<slug>, #kenar-nav,
//   #kenar-satir-<slug>); yalnız #kenar-grup-dugme-<slug>, #kenar-onizleme-<slug>
//   ve #kenar-tum-ekranlar EKLENDİ.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class MenuKatlamaTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private string moduller = """["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","evrak","siparis","bildirim"]""";
    private string tercihler = "{}";

    private sealed class SahteApi(
        List<(string, string, string)> kayit,
        Func<string> moduller, Func<string> tercihler, Func<bool> patlat) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            if (patlat() && istek.Method != HttpMethod.Get)
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"hata":"tercih yazılamadı"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            // ⚠ bUnit sözleşmesi: "{}" varsayılanı bırakılmaz — her uca gerçekçi yanıt.
            var cevap = yol switch
            {
                var y when y.Contains("/tercihler/benim", StringComparison.Ordinal) =>
                    $$"""{"tercihler":{{tercihler()}},"mesaj":"Tercihler kaydedildi"}""",
                var y when y.Contains("/yetkiler/benim", StringComparison.Ordinal) =>
                    $$"""{"moduller":{{moduller()}}}""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    public MenuKatlamaTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(
            new SahteApi(istekler, () => moduller, () => tercihler, static () => false /* TEMA TEMİZLİĞİ 17.10: yazma hatası senaryosu (favori/katlama) kaldırıldı */))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<MenuTercihServisi>();
    }

    // TEMA TEMİZLİĞİ 17.10: kenar çubuğu (Layout/KenarCubugu.razor) kaldırıldı; kabuk artık
    // YildizUstMenu (5 kelime) + "Tüm ekranlar" sayfasıdır. Katlanır grup, önizleme satırı,
    // katlama tercihi yazımı ve kenar kullanıcı kartı Yıldız'da BİLEREK yoktur (tuval Bugun4/Tum4).
    // Tuval notu n2 ("Menüde HİÇBİR ekran kaybolmadı") ve M-UX3-06 (mod yetkiyi gevşetmez)
    // güvenceleri aşağıda "Tüm ekranlar" sayfası (Pages/Ekranlar → YildizEkranlar) üzerinden sınanır.

    private IRenderedComponent<Ekranlar> TumEkranlar(string rol = "SAHIP", string? mod = null)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", rol, "s1", null,
                                mod ?? ArayuzModuServisi.Profesyonel));
        return Render<Ekranlar>().KareleriBekle();
    }

    // TEMA TEMİZLİĞİ 17.10: Varsayilan_olarak_YALNIZ_aktif_rotanin_grubu_aciktir kaldırıldı — katlanır grup Yıldız'da yok; bulunulan ekranın grubu üst menüde seçili sözcükle gösterilir (YildizKatmanTestleri.YK_01/YK_03).
    // TEMA TEMİZLİĞİ 17.10: Aktif_rota_degisince_ACIK_grup_da_degisir kaldırıldı — katlanır grup yok; rota → seçili sözcük eşlemesi YildizKatmanTestleri.YK_01/YK_03'te.
    // TEMA TEMİZLİĞİ 17.10: Alt_rota_dogru_grubu_acar kaldırıldı — katlanır grup yok; alt rota eşlemesi Yildiz2Testleri.Y2_04 (önek kuralı) ve YildizKatmanTestleri.YK_01'de.
    // TEMA TEMİZLİĞİ 17.10: Yetkisiz_rotada_o_rotanin_grubu_ACILMAZ kaldırıldı — katlanır grup yok; görünmeyen sözcükten seçim türetilmemesi YildizKatmanTestleri.YK_02'de.

    // ── (1) HİÇBİR EKRAN KAYBOLMAZ (tuval n2) ───────────────────────────────

    [Fact]
    public void Tum_ekranlar_bolumu_o_gruptaki_TUM_ekran_adlarini_yazar()
    {
        // ESKİ: Kapali_grubun_onizlemesi_o_gruptaki_TUM_ekran_adlarini_yazar (kenar önizleme satırı).
        var e = TumEkranlar();
        var tr = Sozlukler.Getir("tr");

        var bolum = e.Find($"#ekranlar-grup-{MenuTanimi.GrupSlug(MenuGrubu.MusteriStok)}");
        var adlar = bolum.QuerySelectorAll("a.yz-kare .yz-kad").Select(k => k.TextContent.Trim()).ToList();
        var beklenen = MenuTanimi.Ogeler.Where(o => o.Grup == MenuGrubu.MusteriStok).ToList();
        Assert.Equal(beklenen.Count, adlar.Count);
        foreach (var oge in beklenen)
            Assert.Contains(YildizEkranSozlugu.KareAdi(oge.Yol, oge.Etiket(tr), "tr"), adlar);
    }

    // TEMA TEMİZLİĞİ 17.10: Kapali_grubun_baglantilari_SILINMEZ_yalnizca_hidden_olur kaldırıldı — katlama (hidden satır) yok; her ekranın karesi "Tüm ekranlar"da (S1dEkranlarTestleri.Karolar_MenuTanimi_ndan_URETILIR_sayi_ve_sira_birebir).
    // TEMA TEMİZLİĞİ 17.10: Acik_grupta_onizleme_satiri_CIZILMEZ kaldırıldı — önizleme satırı Yıldız'da yok.
    // TEMA TEMİZLİĞİ 17.10: Grup_basligina_basinca_grup_acilir_ve_TERCIH_sunucuya_yazilir kaldırıldı — grup başlığı düğmesi ve katlama tercihi yazımı Yıldız'da yok (sunucu ucu KATMAN2TercihHttpTestleri'nde).
    // TEMA TEMİZLİĞİ 17.10: Acik_gruba_basinca_KAPANIR_ve_diger_acik_gruplar_kapanmaz kaldırıldı — katlama etkileşimi yok.
    // TEMA TEMİZLİĞİ 17.10: Kayitli_tercih_OTEKI_gruplari_yonetir_AKTIF_grup_yine_ACIK kaldırıldı — kayıtlı katlama tercihini okuyan menü yok.
    // TEMA TEMİZLİĞİ 17.10: KIYAS2a_tercih_para_banka_iken_faturalarda_BELGE_UYUM_acik_para_banka_tercihle_acik kaldırıldı — katlama yok.
    // TEMA TEMİZLİĞİ 17.10: KIYAS2a_aktif_grup_bu_ziyarette_katlanir_TERCIHE_YAZILMAZ_rota_degisince_yine_acik kaldırıldı — katlama yok.
    // TEMA TEMİZLİĞİ 17.10: KIYAS2a_oteki_grubu_katlayinca_AKTIF_grup_TOHUMA_girmez kaldırıldı — katlama yok.
    // TEMA TEMİZLİĞİ 17.10: Yazma_basarisizsa_katlama_GERI_ALINIR_ve_bant_cizilir kaldırıldı — katlama yazımı ve #kenar-sira-hatasi bandı yok.
    // TEMA TEMİZLİĞİ 17.10: Katlama_SIFIRLA_dugmesi_yalniz_kayitli_tercih_varken_cikar_ve_DELETE_atar kaldırıldı — #kenar-katlama-sifirla Yıldız'da yok.

    // ── (3) MOD YETKİYİ GEVŞETMEZ (M-UX3-06) ────────────────────────────────

    [Fact]
    public void Yetkisiz_ekran_TUM_EKRANLAR_da_ve_bolum_basliginda_yoktur()
    {
        // ESKİ: Yetkisiz_ekran_KAPALI_grupta_da_onizlemede_de_yoktur (kenar çubuğu).
        moduller = "[]";
        var e = TumEkranlar(rol: "TEZGAHTAR");

        Assert.Contains("/", e.KareYollari());   // Panel yetki istemez: sayfa gerçekten çizildi
        Assert.DoesNotContain("/urunler", e.KareYollari());
        // Grubun hiçbir ekranı yetkili değil → bölümü (başlığıyla) hiç çizilmez.
        Assert.Empty(e.FindAll($"#ekranlar-grup-{MenuTanimi.GrupSlug(MenuGrubu.MusteriStok)}"));
        Assert.DoesNotContain(MenuAdlari.Tr.Urunler.Ad, e.FindAll("a.yz-kare .yz-kad").Select(k => k.TextContent.Trim()));
    }

    // TEMA TEMİZLİĞİ 17.10: Basit_modda_da_ALTI_grup_basligi_cizilir_ve_kapali_gruplar_katlanir kaldırıldı — katlanır grup yok; basit modda bölümler ve yetkili her ekran BasitModMenuTestleri ile S1dEkranlarTestleri.Basit_modda_da_YETKILI_her_ekranin_karosu_cizilir_mod_suzgec_degil'de.

    // ── "TÜM EKRANLAR" SAYISI ───────────────────────────────────────────────

    // TEMA TEMİZLİĞİ 17.10: Tum_ekranlar_dugmesi_GORUNUR_EKRAN_SAYISINI_yazar kaldırıldı — kenar #kenar-tum-ekranlar "(N)" girişi yok; üst menüde "Tüm ekranlar" sayısız sözcüktür (Yildiz2Testleri.Y2_10), sayı sayfanın üst satırındadır (aşağıdaki iki test).
    // TEMA TEMİZLİĞİ 17.10: Tum_ekranlar_girisi_ARTIK_KARO_SAYFASINA_giden_BAGLANTIDIR kaldırıldı — giriş artık üst menünün #yildiz-menu-tum-ekranlar bağlantısı (href /ekranlar: Yildiz2Testleri.Y2_10; her rolde var: Y2_03/Y2_11).

    [Fact]
    public void Tum_ekranlar_sayisi_BASIT_MODDA_da_YETKILI_ekran_sayisidir_mod_suzgeci_yok()
    {
        // "Tüm ekranlar" sayfası kare sayısını söyler; mod süzgeç DEĞİLDİR.
        var e = TumEkranlar(mod: ArayuzModuServisi.Basit);
        var beklenen = MenuTanimi.Yetkililer(_ => true, "SAHIP").Count;

        Assert.Equal(MenuTanimi.Ogeler.Count, beklenen);
        Assert.StartsWith($"{beklenen} ekran ", e.Find("#ekranlar-yildiz-ozet").TextContent.Trim(), StringComparison.Ordinal);
        // Sayı ile sayfa AYNI soruya aynı cevabı verir.
        Assert.Equal(beklenen, e.FindAll("a.yz-kare").Count);
    }

    [Fact]
    public void Tum_ekranlar_sayisi_YETKISIZ_ekrani_SAYMAZ()
    {
        // M-UX3-06: TEZGÂHTAR'ın rol kapılı ekranları (gider, MASAK, işletme…)
        // ve yetkisi olmayan modüller sayıya girmez.
        moduller = """["cari","islem"]""";
        var e = TumEkranlar(rol: "TEZGAHTAR");
        var beklenen = MenuTanimi.Yetkililer(m => m is "cari" or "islem", "TEZGAHTAR").Count;
        // Bağımsız sayım (süzgecin kendisine güvenmeden): modül VE rol kapısı.
        var bagimsiz = MenuTanimi.Ogeler.Count(o =>
            (o.Modul is null || o.Modul is "cari" or "islem")
            && (o.Roller.Length == 0 || Array.IndexOf(o.Roller, "TEZGAHTAR") >= 0));

        Assert.Equal(bagimsiz, beklenen);
        Assert.True(beklenen < MenuTanimi.Ogeler.Count);
        e.WaitForAssertion(() => Assert.Equal(beklenen, e.FindAll("a.yz-kare").Count));
        Assert.StartsWith($"{beklenen} ekran ", e.Find("#ekranlar-yildiz-ozet").TextContent.Trim(), StringComparison.Ordinal);
    }

    // TEMA TEMİZLİĞİ 17.10: Marka_satiri_TEK_KAYNAKTAN_gelir_eski_ad_yazilmaz kaldırıldı — #kenar-marka yok; marka tek kaynaktan (KabukTuvalSozlugu.MarkaYazimi) üst menü amblemine gelir: YildizAmblem2Testleri.YA2_01 (aria-label) + YA2_04 (harfler); Marka.Ad'dan türeme TK4EkranlarGirisTuvalTestleri.TK4_01.
    // TEMA TEMİZLİĞİ 17.10: Kullanici_kartinda_rol_SOZLUKTEN_cevrilir_ham_kod_basilmaz kaldırıldı — Yıldız kullanıcı düğmesi rol yazmaz (tuval "HAKAN T."); ad/baş harf kuralı YildizAmblem2Testleri.YA2_18'de.
    // TEMA TEMİZLİĞİ 17.10: Taninmayan_rol_TAHMIN_EDILMEZ_bos_kalir kaldırıldı — kullanıcı düğmesinde rol satırı yok.
    // TEMA TEMİZLİĞİ 17.10: Grup_basligi_ARIA_EXPANDED_tasir_ve_menude_paragraf_yoktur kaldırıldı — grup başlığı düğmesi yok; üst menünün aria-expanded/aria-controls sözleşmesi Yildiz2Testleri.Y2_12'de.

    // ── SUNUCU SÖZLEŞMESİ ───────────────────────────────────────────────────

    [Fact]
    public void Katlama_anahtari_sunucunun_BEYAZ_LISTESINDEDIR()
    {
        // İstemci anahtarı ile sunucunun beyaz listesi AYNI dizgedir; biri
        // değişirse PATCH 400 döner ve katlama sessizce çalışmaz olurdu.
        Assert.Equal(TercihAnahtarlari.MenuAcikGruplar, MenuTercihServisi.AnahtarAcikGruplar);
        Assert.Contains(TercihAnahtarlari.MenuAcikGruplar, TercihAnahtarlari.Tumu);
    }

    [Fact]
    public void Menu_sozlugu_IKI_DILDE_doludur()
    {
        foreach (var dil in new[] { "tr", "en" })
        {
            Assert.False(string.IsNullOrWhiteSpace(MenuSozlugu.TumEkranlar(dil, 38)));
            Assert.False(string.IsNullOrWhiteSpace(MenuSozlugu.TumEkranlarAciklama(dil)));
            Assert.False(string.IsNullOrWhiteSpace(MenuSozlugu.GrubuAc(dil, "X")));
            Assert.False(string.IsNullOrWhiteSpace(MenuSozlugu.GrubuKapat(dil, "X")));
            Assert.False(string.IsNullOrWhiteSpace(MenuSozlugu.KatlamaIpucu(dil)));
            Assert.False(string.IsNullOrWhiteSpace(MenuSozlugu.OnizlemeEtiketi(dil)));
        }
        Assert.NotEqual(MenuSozlugu.KatlamaIpucu("tr"), MenuSozlugu.KatlamaIpucu("en"));
    }
}
