using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET TOPLU-6 — kalan görünüm kuyruğu (Vitrin, Kur, Vade, Kurulum, Siparişler, liste
// sayacı, Uyum, Panel yasal liste bandı, koyu tema seçili sekme/segment, Gider / Para
// aktarımı, Çıktı araç şeridi, Döviz Veresiye kutusu). YALNIZ GÖRÜNÜM (hasımsız paket):
// her kabul maddesi burada bir testle sabitlenir ve mutasyonla kırmızıya döndüğü gösterilir.
//   saf/kaynak : koyu tema seçili belirteci (≥ 3:1 her karanlık tema × şema) · sade.css şerit
//                · üst çubuk anahtarı · index.html sırası · toplu6.css kuralları · Panel/Uyum/
//                Ürün etiketleri/Cariler kaynak kalıpları.
//   bUnit      : yasal liste bandı · Vitrin katlanır + Sıra + 375 etiketi · Kur başlık hizası +
//                ⓘ · Vade BASİT sütunlar + telefon · Kurulum "Şimdi gir" + soru etiketi ·
//                GerekceAlani sayacı · Siparişler bağlantı kalıbı / rozet / "+ Yeni" · Gider
//                kilit yeri + nakit cümlesi + kart payı · Para aktarımı BASİT dili + boş durumlar.
// ═════════════════════════════════════════════════════════════════════════════

internal static class Toplu6Ortak
{
    public static string Css(string ad) => CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", ad);
    public static List<CssKaynak.Bildirim> Bildirimler(string ad) => CssKaynak.Bildirimler(Css(ad));
    public static string Normal(string s) => Regex.Replace(s.Trim(), "\\s+", " ");
    public static string Sayfa(string ad) => CssKaynak.Metin("Kuyumcu.Web", "Pages", ad);
    public static string Bilesen(params string[] p) => CssKaynak.Metin(["Kuyumcu.Web", "Bilesenler", .. p]);

    /// <summary>Bir seçicinin bir özelliğinin TEK değeri (dosyada tam 1 kez).</summary>
    public static string Deger(string dosya, string secici, string ozellik)
    {
        var l = Bildirimler(dosya).Where(b => Normal(b.Secici) == secici && b.Ozellik == ozellik).ToList();
        Assert.True(l.Count == 1, $"{dosya}: '{secici}' {{ {ozellik} }} tam 1 kez olmalı (bulunan {l.Count})");
        return l[0].Deger;
    }
}

// ═══ (1) Koyu tema seçili sekme/segment — tek yerde (dugme.css) ═════════════════
public sealed class Toplu6KoyuSekmeTestleri
{
    private static Dictionary<string, string> Esleme(Func<CssKaynak.Bildirim, bool> blok) =>
        Toplu6Ortak.Bildirimler("dugme.css")
            .Where(x => x.Ozellik.StartsWith("--secili-dolgu", StringComparison.Ordinal) && blok(x))
            .ToDictionary(x => x.Ozellik, x => Regex.Match(x.Deger, @"^var\((--[a-z0-9-]+)\)$").Groups[1].Value);

    private static Dictionary<string, string> KaranlikMedya() =>
        Esleme(x => x.AtKural.Contains("prefers-color-scheme: dark", StringComparison.Ordinal));

    private static Dictionary<string, string> KaranlikTercih() =>
        Esleme(x => x.AtKural.Length == 0 && x.Secici.Contains("data-tema=\"karanlik\"", StringComparison.Ordinal));

    [Fact]
    public void T6_Koyu_secili_esleme_iki_karanlik_kurulumda_AYNI_ve_TERS_dolgu()
    {
        var m = KaranlikMedya();
        var t = KaranlikTercih();
        Assert.Equal(3, m.Count);
        Assert.Equal(3, t.Count);
        Assert.Equal(m.OrderBy(x => x.Key), t.OrderBy(x => x.Key));
        Assert.Equal("--kenar-metin-guclu", m["--secili-dolgu"]);
        Assert.Equal("--kenar-yuzey", m["--secili-dolgu-metin"]);
        Assert.Equal(m["--secili-dolgu"], m["--secili-dolgu-kenar"]);   // hap içinde hap yok
        // Medya bloğu aydınlık tercihini dışlar (aydınlığı zorlayan kullanıcıda lacivert kalır).
        Assert.Contains(Toplu6Ortak.Bildirimler("dugme.css"), b => b.AtKural.Contains("prefers-color-scheme: dark", StringComparison.Ordinal)
            && b.Ozellik == "--secili-dolgu" && Toplu6Ortak.Normal(b.Secici) == "html[data-mod]:not([data-tema=\"aydinlik\"])");
    }

    [Fact]
    public void T6_Koyu_temada_secili_dolgu_yuzeyden_ve_zeminden_en_az_3e1_yazi_4_5_birincil_mavi_degil()
    {
        var e = KaranlikMedya();
        var t = BelirtecTablosu.Yukle();
        var kirik = new List<string>();
        var koyu = 0;
        foreach (var (tema, tablo) in t.Temalar())
        {
            if (!tema.StartsWith("KARANLIK", StringComparison.Ordinal)) continue;
            koyu++;
            var dolgu = t.Coz(e["--secili-dolgu"], tablo);
            var yazi = t.Coz(e["--secili-dolgu-metin"], tablo);
            var yuzey = t.Coz("--yuzey", tablo);
            var zemin = t.Coz("--yuzey-govde", tablo);
            var birincil = t.Coz("--vurgu", tablo);
            if (dolgu is null || yazi is null || yuzey is null || zemin is null || birincil is null
                || !Kontrast.HexMi(dolgu) || !Kontrast.HexMi(yazi) || !Kontrast.HexMi(yuzey) || !Kontrast.HexMi(zemin))
            { kirik.Add($"{tema}: çözülemedi"); continue; }
            if (Kontrast.Oran(dolgu, yuzey) < 3.0) kirik.Add($"{tema}: dolgu {dolgu} / yüzey {yuzey} = {Kontrast.Oran(dolgu, yuzey):0.00} < 3");
            if (Kontrast.Oran(dolgu, zemin) < 3.0) kirik.Add($"{tema}: dolgu {dolgu} / zemin {zemin} = {Kontrast.Oran(dolgu, zemin):0.00} < 3");
            if (Kontrast.Oran(yazi, dolgu) < 4.5) kirik.Add($"{tema}: yazı {yazi} / dolgu {dolgu} = {Kontrast.Oran(yazi, dolgu):0.00} < 4,5");
            if (string.Equals(dolgu, birincil, StringComparison.OrdinalIgnoreCase)) kirik.Add($"{tema}: dolgu = ana eylem mavisi");
        }
        Assert.True(koyu >= 2, "karanlık tema tablosu bulunamadı");
        Assert.True(kirik.Count == 0, string.Join("\n  ", kirik));
    }

    [Fact]
    public void T6_Kur_ve_doviz_sekme_seridi_ortak_belirteci_kullanir_ayri_karanlik_eslemesi_YOK()
    {
        const string secili = "#yd-sekme-seridi .sekmeler-sekme.secili, #kur-sekme-seridi .sekmeler-sekme.secili";
        Assert.Equal("var(--secili-dolgu)", Toplu6Ortak.Deger("sade.css", secili, "background"));
        Assert.Equal("var(--secili-dolgu-metin)", Toplu6Ortak.Deger("sade.css", secili, "color"));
        Assert.DoesNotContain(Toplu6Ortak.Bildirimler("sade.css"), b =>
            (b.Secici.Contains("kur-sekme-seridi", StringComparison.Ordinal) || b.Secici.Contains("yd-sekme-seridi", StringComparison.Ordinal))
            && b.Secici.Contains(".secili", StringComparison.Ordinal) && b.Deger.Contains("vurgu-yuzey", StringComparison.Ordinal));
    }

    [Fact]
    public void T6_Ust_cubuk_anahtari_ayni_secili_belirteci_kullanir()
    {
        Assert.Equal("var(--secili-dolgu)", Toplu6Ortak.Deger("toplu6.css", ":root[data-mod] .ust-bar .mod-anahtar button.secili", "background"));
        Assert.Equal("var(--secili-dolgu-metin)", Toplu6Ortak.Deger("toplu6.css", ":root[data-mod] .ust-bar .mod-anahtar button.secili", "color"));
    }

    [Fact]
    public void T6_index_toplu6_css_degisim_gunsonu_ten_SONRA_olcek_ten_ONCE()
    {
        var html = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "index.html");
        var t6 = html.IndexOf("css/toplu6.css", StringComparison.Ordinal);
        Assert.True(t6 > 0, "toplu6.css bağlı değil");
        Assert.True(html.IndexOf("css/degisim-gunsonu.css", StringComparison.Ordinal) < t6);
        Assert.True(t6 < html.IndexOf("css/olcek.css", StringComparison.Ordinal));
    }

    [Fact]
    public void T6_toplu6_css_sabit_renk_YOK_yalniz_belirtec()
    {
        var css = Regex.Replace(Toplu6Ortak.Css("toplu6.css"), @"/\*.*?\*/", "", RegexOptions.Singleline);
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b", css);
        Assert.DoesNotMatch(@"\brgba?\(", css);
        Assert.DoesNotContain("!important", css, StringComparison.Ordinal);
    }
}

// ═══ (2) CSS kuralları: bant ritmi, Vitrin 375 kart düzeni, Kur başlık, Kim borçlu, Sipariş bağlantısı, Çıktı şeridi ═══
public sealed class Toplu6CssTestleri
{
    private static string? Blok(string secici, bool medya = false)
    {
        var l = Toplu6Ortak.Bildirimler("toplu6.css").Where(b => Toplu6Ortak.Normal(b.Secici) == secici
            && (medya ? b.AtKural.Contains("max-width: 600px", StringComparison.Ordinal) : b.AtKural.Length == 0)).ToList();
        return l.Count == 0 ? null : string.Join(";", l.Select(b => b.Ozellik + ":" + b.Deger));
    }

    [Fact]
    public void T6_Yasal_bant_kartlara_yapismaz_24px_ritim()
    {
        Assert.Equal("var(--bosluk-5)", Toplu6Ortak.Deger("toplu6.css", ":is(#uyb-hata, #uyb-eslesme, #uyb-liste, #uyb-saklama)", "margin-bottom"));
        Assert.Equal("max(14px, var(--yazi-sm))", Toplu6Ortak.Deger("toplu6.css", "#ylp-neden > summary", "font-size"));
    }

    [Fact]
    public void T6_Vitrin_375_tablo_kart_duzenine_doner_baslik_etiketten()
    {
        Assert.Contains("display:block", Blok("#vitrin-ayarlar, #vitrin-ayarlar tbody, #vitrin-ayarlar tr, #vitrin-ayarlar td", medya: true));
        Assert.Contains("display:none", Blok("#vitrin-ayarlar thead", medya: true));
        Assert.Contains("content:attr(data-etiket)", Blok("#vitrin-ayarlar tbody td[data-etiket]::before", medya: true));
        Assert.Equal("start", Toplu6Ortak.Deger("toplu6.css", "#vitrin-ayarlar th:first-child, #vitrin-pano th:first-child", "text-align"));
    }

    [Fact]
    public void T6_Kur_ayar_fiyat_basligi_ust_kutuya_yapismaz()
    {
        Assert.Equal("var(--bosluk-5)", Toplu6Ortak.Deger("toplu6.css", "#kur-ayar-fiyat-baslik", "margin-top"));
    }

    [Fact]
    public void T6_Kim_borclu_satir_eylemi_esit_genislik()
    {
        Assert.Equal("6.875rem", Toplu6Ortak.Deger("toplu6.css", "html[data-mod] .panel-sade #panel-soru-borclu .panel-liste-satir > .dugme", "min-width"));
    }

    [Fact]
    public void T6_Siparis_istege_bagli_tetik_metin_baglantisi_kalibi()
    {
        var b = Blok("#sip-f-detay-ac, #sip-f-detay-gizle")!;
        Assert.Contains("border:0", b);
        Assert.Contains("background:none", b);
        Assert.Contains("color:var(--vurgu)", b);
        Assert.Contains("min-height:44px", b);
        Assert.Contains("display:none", Blok("#sip-f-detay-ac .zo-detay-ok"));
    }

    [Fact]
    public void T6_Cikti_seridi_belge_genisliginde_Yazdir_sagda_375_te_ustte_tam_genislik()
    {
        Assert.Equal("100%", Toplu6Ortak.Deger("toplu6.css", "#sayfa-basi-cikti-belge", "width"));
        Assert.Contains("margin-left:auto", Blok("#sayfa-basi-cikti-belge-eylem")!);
        var dar = Blok("#sayfa-basi-cikti-belge-eylem", medya: true)!;
        Assert.Contains("order:-1", dar);
        Assert.Contains("width:100%", dar);
        var geri = Blok("html[data-mod] #cikti-geri", medya: true)!;
        Assert.Contains("background:none", geri);
        Assert.Contains("border:0", geri);
    }

    [Fact]
    public void T6_Alan_kilit_mesaji_en_az_14px()
    {
        Assert.Equal("max(14px, var(--yazi-sm))", Toplu6Ortak.Deger("toplu6.css", ".t6-alan-kilit", "font-size"));
    }
}

// ═══ (3) Kaynak kalıpları (Panel, Uyum, Ürün etiketleri, Cariler) ════════════════
public sealed class Toplu6KaynakTestleri
{
    [Fact]
    public void T6_Panel_bekleyen_is_yasal_seritte_SAYIYLA_tutarli_dil()
    {
        var p = Toplu6Ortak.Sayfa("Panel.razor");
        // PAKET RAPOR-3 (hasım YÜKSEK): "Bekleyen N iş üstteki uyarıda" yönlendirmesi kalktı — yasal iş kartın KENDİ
        // satırında (BekleyenUyumSatirlari, BASİT + PRO); sayı ile dil yine tutarlı (satır sayısı = bant işi).
        Assert.Empty(Regex.Matches(p, Regex.Escape("Toplu6Sozlugu.Al(Dil.Dil).PanelBekleyenSeritte")));
        Assert.Equal(3, Regex.Matches(p, Regex.Escape("<BekleyenUyumSatirlari ")).Count);   // BASİT kart + PRO kutucuğun iki dalı
        Assert.DoesNotContain("Yasal3Sozlugu.Al(Dil.Dil).PanelSeritte", p, StringComparison.Ordinal);
        Assert.DoesNotContain("Başka iş yok", Toplu6Sozlugu.Tr.PanelBekleyenSeritte, StringComparison.Ordinal);
    }

    [Fact]
    public void T6_Uyum_risk_sekmesinde_Yeni_bildirim_YOK_arama_notu_ipucunda()
    {
        var u = Toplu6Ortak.Sayfa("Uyum.razor");
        Assert.Contains("@if (sekme == Sekme.Nakit || sekme == Sekme.Risk || sekme == Sekme.Supheli)", u, StringComparison.Ordinal);   // sunum 15.10: Şüpheli sekmesinde de başlık eylemsiz (düğme sekmede)
        // Bileşen öznitelikleri içinde "=>" geçtiği için etiket sonu değil, DegerChanged'e kadar olan bölüm alınır.
        var bas = u.IndexOf("<Arama ElemanId=\"ara-belge\"", StringComparison.Ordinal);
        Assert.True(bas >= 0, "ara-belge araması yok");
        var arama = u[bas..u.IndexOf("DegerChanged=", bas, StringComparison.Ordinal)];
        Assert.Contains("YerTutucu=", arama, StringComparison.Ordinal);
        Assert.DoesNotContain("Ipucu=", arama, StringComparison.Ordinal);
        Assert.Matches(new Regex("<details class=\"zo-ipucu\" id=\"uy-arama-ipucu\">.*?<p id=\"ara-belge-ipucu\">@Dil.T.Uyum.AramaNotu</p>", RegexOptions.Singleline), u);
    }

    [Fact]
    public void T6_Urun_etiketleri_sayaci_sinirin_altinda_yalniz_sayi()
    {
        var u = Toplu6Ortak.Sayfa("UrunEtiketleri.razor");
        Assert.Contains("@if (UrunListesi.Count >= Sinir)", u, StringComparison.Ordinal);
        Assert.Contains("Toplu6Sozlugu.Al(Dil.Dil).ListeSayacSade, UrunListesi.Count", u, StringComparison.Ordinal);
    }

    [Fact]
    public void T6_Doviz_veresiye_kutusu_bos_gelir_gonderilen_deger_ayni()
    {
        var c = Toplu6Ortak.Sayfa("Cariler.razor");
        Assert.Contains("private decimal? DvKutuDegeri => dvDokunuldu || dvTutar > 0m ? dvTutar : null;", c, StringComparison.Ordinal);
        Assert.Contains("Deger=\"@DvKutuDegeri\"", c, StringComparison.Ordinal);
        Assert.Contains("tutar = dvTutar,", c, StringComparison.Ordinal);   // gövde AYNI
    }

    [Fact]
    public void T6_Sozluk_TR_EN_dolu()
    {
        foreach (var m in new[] { Toplu6Sozlugu.Tr, Toplu6Sozlugu.En })
            foreach (var p in typeof(Toplu6Metinleri).GetProperties().Where(p => p.PropertyType == typeof(string)))
                Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(m)), p.Name);
        Assert.DoesNotContain("destek", Toplu6Sozlugu.Tr.PanelListeYok, StringComparison.OrdinalIgnoreCase);
    }
}

// ═══ (4) Panel yasal liste bandı + Uyum liste paneli ═══════════════════════════
public sealed class Toplu6YasalBantTestleri : Toplu4Tabani
{
    public Toplu6YasalBantTestleri()
    {
        Yol("/masak/uyum-ozet", """{"saklamaYaklasan":0,"saklamaGun":30,"saklamaKuru":true,"listeTarihi":null,"listeEski":true,"listeYasiGun":null,"acikTam":0,"acikBenzerlik":0}""");
        Yol("/masak/yaptirim", """{"listeTarihi":null,"kaynak":null,"kaynakUrl":null,"listeYasiGun":null,"listeEski":true,"eskiListeGun":14,"yururlukteSatir":0,"acikTam":0,"acikBenzerlik":0,"eslesmeler":[]}""");
        Oturum(ArayuzModuServisi.Basit);
    }

    [Fact]
    public void T6_Bant_cikmaz_sokak_degil_baglanti_ne_yaptigini_soyler()
    {
        var e = Render<Kuyumcu.Web.Bilesenler.Uyum.UyumYasalBantlari>();
        e.WaitForElement("#uyb-liste", TimeSpan.FromSeconds(10));
        Assert.Equal(Toplu6Sozlugu.Tr.PanelListeYok, e.Find("#uyb-liste > span").TextContent.Trim());
        Assert.Equal(Toplu6Sozlugu.Tr.PanelListeGit + " →", e.Find("#uyb-liste-git").TextContent.Trim());
        Assert.Equal("/uyum?sekme=risk", e.Find("#uyb-liste-git").GetAttribute("href"));
        Assert.DoesNotContain("destek", e.Find("#uyb-liste").TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void T6_Uyum_liste_paneli_kimin_yukledigini_soyler()
    {
        var e = Render<Kuyumcu.Web.Bilesenler.Uyum.YasalListePaneli>();
        e.WaitForElement("#ylp-yok", TimeSpan.FromSeconds(10));
        Assert.Equal(Toplu6Sozlugu.Tr.UyumListeYukleyen, e.Find("#ylp-yukleyen").TextContent.Trim());
    }
}

// ═══ (5) Vitrin: ayar tablosu katlanır, Sıra yalnız PRO, 375 etiketleri, boş atlanan kartı ═══
public sealed class Toplu6VitrinTestleri : Toplu4Tabani
{
    public Toplu6VitrinTestleri()
    {
        Yol("/pano/guncel", """
            {"kaynak":"ALTINKAYNAK","zaman":"2026-09-24T08:00:00Z",
             "satirlar":[{"kod":"GRAM","etiket":"Gram Altın","alis":6800.00,"satis":6900.00,"birim":"GRAM"}],
             "atlananlar":[],"tavanSatir":16,"okunabilirlikEsigi":12,"okunabilirlikUyarisi":false}
            """, son: false);
        Yol("/pano", """
            {"ayarlar":[
               {"kod":"GRAM","ad":"Gram Altın","etiket":null,"sira":0,"alisMarjTl":-10.00,"satisMarjTl":20.00,"yuvarlamaAdimi":10,"aktif":true,"ozel":true},
               {"kod":"CEYREK_YENI","ad":"Yeni Çeyrek","etiket":null,"sira":1,"alisMarjTl":0,"satisMarjTl":0,"yuvarlamaAdimi":10,"aktif":true,"ozel":false}],
             "yuvarlamaAdimlari":[0,1,5,10,50,100],"tavanSatir":16,"okunabilirlikEsigi":12,"aktifSayisi":2}
            """, son: false);
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Vitrin> Ciz(string mod)
    {
        Oturum(mod);
        var e = Render<Kuyumcu.Web.Pages.Vitrin>();
        e.WaitForElement("#vitrin-ayar-kat-ac", TimeSpan.FromSeconds(10));
        return e;
    }

    [Fact]
    public void T6_Vitrin_BASIT_ayar_tablosu_KAPALI_katlanirda_acilinca_gelir_Sira_yok()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        Assert.Equal("false", e.Find("#vitrin-ayar-kat-ac").GetAttribute("aria-expanded"));
        Assert.Empty(e.FindAll("#vitrin-ayarlar"));
        Assert.Equal("2", e.Find("#vitrin-ayar-kat-sayi").TextContent.Trim());
        e.Find("#vitrin-ayar-kat-ac").Click();
        e.WaitForElement("#vitrin-ayarlar");
        Assert.DoesNotContain(e.FindAll("#vitrin-ayarlar thead th"), th => th.TextContent.Trim() == Sozlukler.Tr.Pano.Sira);
        Assert.NotNull(e.Find("#vitrin-duzenle-GRAM"));   // düzenleme yolu açınca AYNEN
    }

    [Fact]
    public void T6_Vitrin_PRO_ayar_tablosu_ACIK_Sira_sutunu_var()
    {
        // GOC-C-2 (tasarım hakemi YÜKSEK "K4 /vitrin PRO 375 = 10 ekran"): PRO'da da KAPALI gelir (zorunlu önce) — açınca
        // Sıra sütunu AYNEN (PRO kuralı korunur). Ad korunur (geçmiş kayıtlarla bağ); sözleşme: kapalı → aç → Sıra var.
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        Assert.Equal("false", e.Find("#vitrin-ayar-kat-ac").GetAttribute("aria-expanded"));
        Assert.Empty(e.FindAll("#vitrin-ayarlar"));
        GocC2Ortak.AyarlariAc(e);
        Assert.Equal("true", e.Find("#vitrin-ayar-kat-ac").GetAttribute("aria-expanded"));
        Assert.Contains(e.FindAll("#vitrin-ayarlar thead th"), th => th.TextContent.Trim() == Sozlukler.Tr.Pano.Sira);
    }

    [Fact]
    public void T6_Vitrin_hucreler_375_kart_duzeni_icin_baslik_etiketi_tasir()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        GocC2Ortak.AyarlariAc(e);   // GOC-C-2: PRO'da da kapalı gelir; data-etiket artık ortak ızgaradan (aynı başlıklar).
        var td = e.FindAll("#vitrin-ayar-GRAM > td").ToList();
        Assert.Equal(Sozlukler.Tr.Pano.Kalem, td[0].GetAttribute("data-etiket"));
        Assert.Contains("t6-kalem", td[0].ClassName ?? "", StringComparison.Ordinal);
        foreach (var et in new[] { Sozlukler.Tr.Pano.AlisMarj, Sozlukler.Tr.Pano.SatisMarj, Sozlukler.Tr.Pano.Yuvarlama, Sozlukler.Tr.Pano.Durum })
            Assert.Contains(td, h => h.GetAttribute("data-etiket") == et);
    }

    [Fact]
    public void T6_Vitrin_atlanan_yok_karti_BASIT_te_cizilmez_PRO_da_var()
    {
        var b = Ciz(ArayuzModuServisi.Basit);
        b.WaitForElement("#vitrin-pano");
        Assert.Empty(b.FindAll("#vitrin-atlanan-yok"));
        var p = Ciz(ArayuzModuServisi.Profesyonel);
        p.WaitForElement("#vitrin-atlanan-yok");
    }
}

// ═══ (6) Kur: ayar bazlı gram fiyatları başlık hizası + BASİT alt not ⓘ ═══════════
public sealed class Toplu6KurTestleri : Toplu4Tabani
{
    public Toplu6KurTestleri()
    {
        Yol("/kur/kaynaklar", """{"kaynaklar":["ALTINKAYNAK"],"varsayilan":"ALTINKAYNAK","secili":"ALTINKAYNAK","sozlesmesizKaynaklarAcik":false,"seciliSozlesmesiz":false,"manuelKod":"MANUAL"}""");
        Yol("/kur/gecmis", "[]");
        Yol("/kur/pariteler", """{"kaynak":"ALTINKAYNAK","pariteler":[]}""");
        Yol("/kur/guncel", """
            {"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":7100,"usdTry":34.2,"eurTry":null,"gbpTry":null,
             "kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-30T09:00:00Z",
             "ayarFiyatlari":[{"ayarId":"K22","milyem":916,"gramFiyati":6228.8},{"ayarId":"K14","milyem":585,"gramFiyati":3978}]}
            """);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T6_Kur_sayisal_sutun_basliklari_degerlerle_ayni_saga_hizali(string mod)
    {
        Oturum(mod);
        var e = Render<Kuyumcu.Web.Pages.Kur>();
        e.WaitForElement("#kur-ayar-fiyat-tablo", TimeSpan.FromSeconds(10));
        var th = e.FindAll("#kur-ayar-fiyat-tablo thead th").ToList();
        Assert.Equal(3, th.Count);
        Assert.DoesNotContain("sayi", th[0].ClassName ?? "", StringComparison.Ordinal);
        Assert.Contains("sayi", th[1].ClassName ?? "", StringComparison.Ordinal);
        Assert.Contains("sayi", th[2].ClassName ?? "", StringComparison.Ordinal);
        Assert.NotNull(e.Find("#kur-ayar-fiyat-baslik"));
    }

    [Fact]
    public void T6_Kur_BASIT_alt_not_satir_degil_ipucunda()
    {
        Oturum(ArayuzModuServisi.Basit);
        var e = Render<Kuyumcu.Web.Pages.Kur>();
        e.WaitForElement("#kur-pro-notu-ipucu", TimeSpan.FromSeconds(10));
        Assert.NotNull(e.Find("#kur-pro-notu-ipucu > #kur-pro-notu > #kur-pro-notu-bag"));
        Assert.False(e.Find("#kur-pro-notu-ipucu").HasAttribute("open"));
    }
}

// ═══ (7) Vade takibi: BASİT sütun sayısı + telefon biçimi ═══════════════════════
public sealed class Toplu6VadeTestleri : Toplu4Tabani
{
    public Toplu6VadeTestleri()
    {
        Yol("/raporlar/vade-takibi", """
            {"ufukGun":7,"gecikenToplam":3000,"yaklasanToplam":0,"vadesizToplam":0,"ufukOtesiToplam":0,"acikToplam":3000,
             "cariler":[{"cariId":"c1","cariAd":"Geciken Musteri","cariTur":"MUSTERI","telefon":"05320000102",
                "gecikenTutar":3000,"yaklasanTutar":0,"vadesizTutar":0,"enEskiGecikmeGun":12,"enYakinVade":"2026-09-03",
                "borclar":[{"hareketId":"h1","tarih":"2026-08-20T08:00:00Z","vade":"2026-09-03","acikTutar":3000,"gecikmeGun":12,"durum":"GECIKTI"}]}]}
            """, son: false);
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.VadeTakibi> Ciz(string mod)
    {
        Oturum(mod);
        var e = Render<Kuyumcu.Web.Pages.VadeTakibi>();
        e.WaitForElement("#vade-tel-c1", TimeSpan.FromSeconds(10));
        return e;
    }

    [Fact]
    public void T6_Vade_BASIT_tablo_iki_tarih_sutunu_olmadan_sigar()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        var tablo = e.Find("#vade-tel-c1").Closest("table")!;
        Assert.Equal(5, tablo.QuerySelectorAll("thead th").Length);
        Assert.Equal(5, tablo.QuerySelectorAll("tbody tr:first-child td").Length);
        Assert.DoesNotContain(Sozlukler.Tr.Vade.EnYakinVade, tablo.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void T6_Vade_PRO_tablo_yedi_sutun_AYNEN()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        var tablo = e.Find("#vade-tel-c1").Closest("table")!;
        Assert.Equal(7, tablo.QuerySelectorAll("thead th").Length);
    }

    [Fact]
    public void T6_Vade_telefon_okunur_bicimde_baglanti_ham_numara_satir_ici_stil_yok()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        var a = e.Find("#vade-tel-c1");
        Assert.Equal("0532 000 01 02", a.TextContent.Trim());
        Assert.Equal("tel:05320000102", a.GetAttribute("href"));
        Assert.Empty(e.FindAll("th[style]"));
    }
}

// ═══ (8) Kurulum: eksik adıma "Şimdi gir" · soru etiketi · gerekçe sayacı ═══════════
public sealed class Toplu6KurulumTestleri : Toplu4Tabani
{
    public Toplu6KurulumTestleri()
    {
        Yol("/kurulum/durum", """
            {"muhurlendi":false,"muhurTarihi":null,"muhurleyenKullaniciId":null,"muhurGerekcesi":null,
             "acilisPenceresiBitis":"2026-10-17T00:00:00Z","acilisPenceresiAcik":true,
             "muhurlenebilir":true,"eksikAdimlar":[],"atlananAdimlar":[],
             "adimlar":[
               {"anahtar":"ISLETME","bitti":true,"zorunlu":true,"atlandi":false},
               {"anahtar":"KULLANICILAR","bitti":true,"zorunlu":true,"atlandi":false},
               {"anahtar":"KUR","bitti":true,"zorunlu":true,"atlandi":false},
               {"anahtar":"KASA_BANKA","bitti":true,"zorunlu":true,"atlandi":false},
               {"anahtar":"CARI_ACILIS","bitti":false,"zorunlu":false,"atlandi":false},
               {"anahtar":"STOK_ACILIS","bitti":false,"zorunlu":false,"atlandi":false}],
             "sayilar":{"sube":1,"kullanici":2,"kur":6,"kasa":1,"banka":1,"acilisCari":0,"acilisKasa":0,"acilisBanka":0},
             "maksSatir":500,"gerekceAsgari":20,"isletmeAdi":"Altın Işık Kuyumcu"}
            """);
        Yol("/kasalar", """[{"id":"k1","ad":"Merkez Kasa","aktif":true}]""");
        Yol("/bankalar", """{"hesaplar":[]}""");
        Yol("/kur/guncel", """{"id":"kur-1","altinHasFiyati":6827.2}""", son: false);
        Yol("/yetkiler/benim", """{"moduller":["kurulum"]}""", son: false);
        Yol("/cari", "[]", son: false);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T6_Kurulum_eksik_adim_Simdi_gir_baglantisi_acilis_bolumunu_acar(string mod)
    {
        Oturum(mod);
        var e = Render<Kuyumcu.Web.Pages.Kurulum>();
        e.WaitForElement("#kur-adim-CARI_ACILIS", TimeSpan.FromSeconds(10));
        var a = e.Find("#kur-adim-CARI_ACILIS-git");
        Assert.Equal(Toplu6Sozlugu.Tr.KurulumSimdiGir, a.TextContent.Trim());
        Assert.Equal("/kurulum#kur-acilis", a.GetAttribute("href"));
        Assert.Empty(e.FindAll("#kur-adim-STOK_ACILIS-git"));   // "Yakında" adımda eylem yok
        Assert.False(e.Find("#kur-acilis-bolum").HasAttribute("open"));
        a.Click();
        e.WaitForAssertion(() => Assert.True(e.Find("#kur-acilis-bolum").HasAttribute("open")));
    }

    [Fact]
    public void T6_Kurulum_alani_olmayan_Aciklama_etiketi_yerine_soru()
    {
        Oturum(ArayuzModuServisi.Basit);
        var e = Render<Kuyumcu.Web.Pages.Kurulum>();
        e.WaitForElement("#kur-acilis-ipucu", TimeSpan.FromSeconds(10));
        var etiketler = e.FindAll(".kur-bolum-ipucu-satir > span").Select(s => s.TextContent.Trim()).ToList();
        Assert.Contains(Toplu6Sozlugu.Tr.KurulumAcilisNasil, etiketler);
        Assert.Contains(Toplu6Sozlugu.Tr.KurulumMuhurNe, etiketler);
        Assert.DoesNotContain(Toplu3Sozlugu.Tr.YasIpucuAc, etiketler);
    }

    [Fact]
    public void T6_Gerekce_sayaci_bosken_NOTR_yazinca_esik_altinda_kirmizi()
    {
        Services.GetRequiredService<DilServisi>();
        var bos = Render<GerekceAlani>(p => p.Add(x => x.Id, "g6").Add(x => x.AsgariUzunluk, 20).Add(x => x.Deger, ""));
        Assert.DoesNotContain("metin-kirmizi", bos.Find("#g6-sayac").ClassName ?? "", StringComparison.Ordinal);
        var kisa = Render<GerekceAlani>(p => p.Add(x => x.Id, "g7").Add(x => x.AsgariUzunluk, 20).Add(x => x.Deger, "abc"));
        Assert.Contains("metin-kirmizi", kisa.Find("#g7-sayac").ClassName ?? "", StringComparison.Ordinal);
        var tam = Render<GerekceAlani>(p => p.Add(x => x.Id, "g8").Add(x => x.AsgariUzunluk, 20).Add(x => x.Deger, new string('a', 20)));
        Assert.DoesNotContain("metin-kirmizi", tam.Find("#g8-sayac").ClassName ?? "", StringComparison.Ordinal);
    }
}

// ═══ (9) Siparişler: rozet yok · "+ … ekle" kalıbı · Daha az göster · "+ Yeni sipariş" ═══
public sealed class Toplu6SiparisTestleri : Toplu4Tabani
{
    public Toplu6SiparisTestleri()
    {
        Yol("/siparisler", """
            {"istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":0,"dahaFazlasiVar":false,
             "gecikenSayisi":0,"durumKirilimi":[],"siparisler":[]}
            """);
        Yol("/subeler", """[{"id":"s1","ad":"Merkez"}]""");
        Yol("/cari", """[{"id":"c1","ad":"Ayşe Yılmaz","tur":"MUSTERI"}]""");
        Yol("/urunler", """[{"id":"u-1","ad":"Bilezik","ayarId":"K22","gramAgirlik":10.0}]""");
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Siparisler> FormAcik(string mod)
    {
        Oturum(mod);
        var e = Render<Kuyumcu.Web.Pages.Siparisler>();
        var ac = e.WaitForElement("#sip-yeni-ac", TimeSpan.FromSeconds(10));
        Assert.StartsWith("+ ", ac.TextContent.Trim(), StringComparison.Ordinal);
        ac.Click();
        e.WaitForElement("#sip-f-detay-ac");
        return e;
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T6_Siparis_sube_satirinda_rozet_YOK_istege_bagli_alanlar_ekle_baglantisi(string mod)
    {
        var e = FormAcik(mod);
        Assert.Empty(e.FindAll("#sip-f-sube-sabit .rozet"));
        Assert.Equal(Toplu6Sozlugu.Tr.SiparisDetayEkle, e.Find("#sip-f-detay-ac").TextContent.Trim());
        Assert.Equal("false", e.Find("#sip-f-detay-ac").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void T6_Siparis_Daha_az_goster_yalniz_bolum_bosken_kapatir()
    {
        var e = FormAcik(ArayuzModuServisi.Basit);
        e.Find("#sip-f-detay-ac").Click();
        Assert.Equal(Toplu6Sozlugu.Tr.SiparisDetayGizle, e.Find("#sip-f-detay-gizle").TextContent.Trim());
        e.Find("#sip-f-detay-gizle").Click();
        Assert.Empty(e.FindAll("#sip-f-detay"));
        e.Find("#sip-f-detay-ac").Click();
        e.Find("#sip-f-aciklama").Input("Yüzük ölçüsü 14");   // PAKET GOC-C: ortak Metin (yazarken input)
        Assert.Empty(e.FindAll("#sip-f-detay-gizle"));   // değer varken kapatılamaz (gizli değer gövdeye gitmez)
    }
}

// ═══ (10) Gider / Para aktarımı ═══════════════════════════════════════════════
public sealed class Toplu6GiderTestleri : Toplu4Tabani
{
    private string kasalar = """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":221726.43,"aktif":true}]""";
    private string kategoriler = """{"kategoriler":[{"id":"gk1","ad":"Kira","aciklama":null,"aktif":true,"giderAdedi":0,"giderToplamTl":0}],"toplam":1}""";
    private string bankalar = """{"hesaplar":[{"id":"bh1","bankaAdi":"Ziraat","hesapAdi":"Vadesiz","hesapSahibi":null,"iban":"TR11","paraBirimi":"TRY","bakiye":5000,"aktif":true}]}""";

    private IRenderedComponent<Kuyumcu.Web.Pages.Gider> Ciz(string mod, string? rota = null)
    {
        Yol("/kasalar/nakit-ayrimi", """[{"kasaId":"k1","paraBirimi":"TRY","toplam":221726.43,"nakit":193830.90,"kart":25895.53,"havale":0},{"kasaId":"k2","paraBirimi":"TRY","toplam":0,"nakit":0,"kart":0,"havale":0}]""");
        Yol("/kasalar", kasalar);
        Yol("/bankalar", bankalar);
        Yol("/gider-kategorileri", kategoriler);
        Yol("/giderler", """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":"x"}""");
        Yol("/virman", """{"satirlar":[],"toplam":0,"limit":50,"dahaFazlasiVar":false,"sinirNotu":"x"}""");
        Yol("/subeler", """[{"id":"s1","ad":"Merkez"}]""", son: false);
        Oturum(mod);
        var e = Render<Kuyumcu.Web.Pages.Gider>();
        e.WaitForElement("#gd-sekme-giderler", TimeSpan.FromSeconds(10));
        if (rota is not null) e.Find("#gd-sekme-virman").Click();
        GiderAkordeon.Ac(e); // SON-KALANLAR: geçmiş kayıtlar kapalı akordeon
        return e;
    }

    private static string Alan(AngleSharp.Dom.IElement kilit) => kilit.Closest(".alan")?.QuerySelector("[id^='gd-']")?.Id ?? "(alan yok)";

    [Fact]
    public void T6_Gider_kategori_yokken_kilit_KATEGORI_alaninin_altinda_14px_sinifi()
    {
        kategoriler = """{"kategoriler":[],"toplam":0}""";
        var e = Ciz(ArayuzModuServisi.Basit);
        var k = e.WaitForElement("#gd-kaydet-kilit");
        Assert.StartsWith("gd-kategori", Alan(k), StringComparison.Ordinal);
        Assert.Contains("t6-alan-kilit", k.ClassName ?? "", StringComparison.Ordinal);
        Assert.Null(e.Find("#gd-kaydet").ParentElement!.QuerySelector("#gd-kaydet-kilit"));   // düğmenin yanında değil
    }

    [Fact]
    public void T6_Gider_kasa_secilmeden_kilit_KASA_alaninin_altinda()
    {
        kasalar = """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":221726.43,"aktif":true},{"id":"k2","ad":"Vitrin","paraBirimi":"TRY","bakiye":0,"aktif":true}]""";
        var e = Ciz(ArayuzModuServisi.Basit);
        var k = e.WaitForElement("#gd-kaydet-kilit");
        Assert.StartsWith("gd-kasa", Alan(k), StringComparison.Ordinal);
        Assert.Equal(Sozlukler.Tr.Gider.KilitKasaSec, k.TextContent.Trim());
    }

    [Fact]
    public void T6_Gider_nakit_asimi_TEK_cumle_tutar_alaninin_altinda_kart_payi_yalniz_fark_yaratirsa()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.WaitForElement("#gd-kasa-nakit");
        e.Find("#gd-tutar").Input("200000");   // nakit 193.830,90 < 200.000 ≤ toplam 221.726,43 → kart payı fark yaratır
        var k = e.Find("#gd-kaydet-kilit");
        Assert.Equal(string.Format(Toplu6Sozlugu.Tr.GiderNakitYetmiyor, Bicim.Para(193_830.90m, "TRY"), Bicim.Para(200_000m, "TRY")), k.TextContent.Trim());
        Assert.Equal(Kuyumcu.Web.Pages.Gider.NakitBantMetni(Toplu5Sozlugu.Tr, 25_895.53m, 0m, "TRY"), e.Find("#gd-yetersiz-metin").TextContent.Trim());
        e.Find("#gd-tutar").Input("250000");   // toplamı da aşar → kart payı yardım etmez, yanıltıcı cümle YOK
        Assert.Equal(Toplu6Sozlugu.Tr.GiderCikisYolu, e.Find("#gd-yetersiz-metin").TextContent.Trim());
        Assert.Contains(Bicim.Para(250_000m, "TRY"), e.Find("#gd-kaydet-kilit").TextContent, StringComparison.Ordinal);
        Assert.True(e.Find("#gd-kaydet").HasAttribute("disabled"));   // kural AYNI
    }

    [Fact]
    public void T6_Para_aktarimi_BASIT_adi_her_yerde_aciklama_ipucunda_kilit_kaynak_altinda()
    {
        var e = Ciz(ArayuzModuServisi.Basit, "/gider?sekme=virman");
        e.WaitForElement("#gd-v-kaynak");
        Assert.Equal(Toplu6Sozlugu.Tr.VirmanSekmeBasit, e.Find("#gd-sekme-virman").TextContent.Trim());
        // SON-KALANLAR: liste başlığı artık kapalı akordeonun başlığı (aynı ad, kart h3'ü değil).
        Assert.Equal(Toplu6Sozlugu.Tr.VirmanListeBasit, e.Find("#gd-virman-gecmis .katlanir-ad").TextContent.Trim());
        Assert.Contains(Toplu6Sozlugu.Tr.VirmanYokBasit, e.Markup, StringComparison.Ordinal);
        Assert.NotNull(e.Find("#gd-virman-ipucu > #gd-virman-aciklama"));
        // Yük altında taraf listesi ve nakit ayrımı ikinci çizimde gelir — yerleşim son duruma göre sınanır.
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#gd-v-kaydet-kilit").Closest(".alan")), TimeSpan.FromSeconds(10));
        var alan = e.Find("#gd-v-kaydet-kilit").Closest(".alan");
        Assert.Equal(Sozlukler.Tr.Gider.KilitKaynakSec, e.Find("#gd-v-kaydet-kilit").TextContent.Trim());
        Assert.StartsWith("gd-v-kaynak", alan!.QuerySelector("[id^='gd-v-']")!.Id, StringComparison.Ordinal);
    }

    [Fact]
    public void T6_Para_aktarimi_PRO_adlari_AYNEN()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel, "/gider?sekme=virman");
        e.WaitForElement("#gd-v-kaynak");
        Assert.Equal(Sozlukler.Tr.Gider.SekmeVirman, e.Find("#gd-sekme-virman").TextContent.Trim());
        Assert.Equal(Sozlukler.Tr.Gider.VirmanYap, e.Find("#gd-v-kaydet").TextContent.Trim());   // PRO düğme adı AYNEN
        Assert.Equal(Sozlukler.Tr.Gider.SayfaAciklama, e.Find("#sayfa-basi-gider-aciklama").TextContent.Trim());
    }

    /// <summary>TOPLU-6 t2 — tasarım hakemi KRİTİK: BASİT'te birincil düğme de "Para aktar"; sekme gövdesinde
    /// (sayfa başlığı hariç — menü adı tuvalden, orkestratör kararı) "virman" kelimesi görünmez.</summary>
    [Fact]
    public void T6_Para_aktarimi_BASIT_kaydet_dugmesi_ve_aciklama_tek_ad()
    {
        var e = Ciz(ArayuzModuServisi.Basit, "/gider?sekme=virman");
        e.WaitForElement("#gd-v-kaynak");
        Assert.Equal(Toplu6Sozlugu.Tr.VirmanYapBasit, e.Find("#gd-v-kaydet").TextContent.Trim());
        Assert.Equal(Toplu6Sozlugu.Tr.SayfaAciklamaBasit, e.Find("#sayfa-basi-gider-aciklama").TextContent.Trim());
        var baslik = e.Find("#sayfa-basi-gider-baslik").TextContent;
        var govde = string.Concat(e.Nodes.Select(n => n.TextContent)).Replace(baslik, "", StringComparison.Ordinal);
        Assert.DoesNotContain("irman", govde, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T6_Para_aktarimi_basari_bandi_moda_gore_tek_ad(string mod)
    {
        var e = Ciz(mod, "/gider?sekme=virman");
        e.WaitForElement("#gd-v-kaynak");
        Yol("/virman", """{"kaynakAdi":"Ana Kasa","hedefAdi":"Ziraat","tutar":2000,"paraBirimi":"TRY","subelerArasi":false,"satirlar":[],"toplam":0,"limit":50,"dahaFazlasiVar":false}""");
        yollar.Insert(0, yollar[^1]);
        SeciciYardim.Sec(e, "gd-v-kaynak", "KASA:k1");
        SeciciYardim.Sec(e, "gd-v-hedef", "BANKA:bh1");
        e.Find("#gd-v-tutar").Input("2000");
        e.Find("#gd-v-kaydet").Click();
        var beklenen = mod == ArayuzModuServisi.Basit ? Toplu6Sozlugu.Tr.VirmanKaydedildiBasit : Sozlukler.Tr.Gider.VirmanKaydedildi;
        e.WaitForAssertion(() => Assert.StartsWith(beklenen + ":", e.Find("#gd-v-sonuc").TextContent.Trim(), StringComparison.Ordinal), TimeSpan.FromSeconds(10));
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T6_Para_aktarimi_geri_alma_onayi_ve_sonucu_moda_gore_tek_ad(string mod)
    {
        Yol("/storno", """{"kaynakBacakIdleri":["a1","b1"],"stornoBacakIdleri":["t1","t2"],"bacakSayisi":2,"gerekce":"test"}""");
        Yol("/virman", """{"satirlar":[{"cikisBacakId":"a1","girisBacakId":"b1","kaynakTur":"KASA","kaynakAdi":"Ana Kasa","hedefTur":"BANKA","hedefAdi":"Ziraat","tutar":2000,"paraBirimi":"TRY","aciklama":null,"tarih":"2026-09-20T06:00:00Z","stornoSatiri":false}],"toplam":1,"limit":50,"dahaFazlasiVar":false}""");
        var e = Ciz(mod, "/gider?sekme=virman");
        e.WaitForElement("#gd-v-storno-a1", TimeSpan.FromSeconds(10)).Click();
        var basit = mod == ArayuzModuServisi.Basit;
        Assert.Equal(basit ? Toplu6Sozlugu.Tr.VirmanStornoOnayBasit : Sozlukler.Tr.Gider.VirmanStornoOnay, e.Find("#gd-storno-onay").TextContent.Trim());
        e.Find("#gd-storno-gerekce").Input("Yanlış hesaba yapılan aktarım geri alınıyor.");
        e.Find("#gd-storno-onayla").Click();
        e.WaitForAssertion(() => Assert.StartsWith(basit ? Toplu6Sozlugu.Tr.VirmanStornoYapildiBasit : Sozlukler.Tr.Gider.VirmanStornoYapildi,
            e.Find("#gd-basari").TextContent.Trim(), StringComparison.Ordinal), TimeSpan.FromSeconds(10));
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void T6_Gider_eksi_kasa_uyarisi_moda_gore_tek_ad(string mod)
    {
        kasalar = """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":-150,"aktif":true}]""";
        var e = Ciz(mod);
        var u = e.WaitForElement("#gd-eksi-kasa-uyari", TimeSpan.FromSeconds(10));
        var sablon = mod == ArayuzModuServisi.Basit ? Toplu6Sozlugu.Tr.EksiKasaUyariBasit : Sozlukler.Tr.Gider.EksiKasaUyari;
        Assert.Contains(string.Format(sablon, Bicim.Para(-150m, "TRY")), u.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void T6_Para_aktarimi_BASIT_sozluk_metinleri_virman_demez()
    {
        foreach (var m in new[] { Toplu6Sozlugu.Tr.VirmanKaydedildiBasit, Toplu6Sozlugu.Tr.VirmanStornoOnayBasit,
                                  Toplu6Sozlugu.Tr.VirmanStornoYapildiBasit, Toplu6Sozlugu.Tr.EksiKasaUyariBasit,
                                  Toplu6Sozlugu.Tr.SayfaAciklamaBasit, Toplu6Sozlugu.Tr.VirmanYapBasit })
        {
            Assert.DoesNotContain("irman", m, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("storno", m, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("bacak", m, StringComparison.OrdinalIgnoreCase);
        }
        Assert.Contains("{0}", Toplu6Sozlugu.Tr.EksiKasaUyariBasit, StringComparison.Ordinal);
        Assert.Contains("{0}", Toplu6Sozlugu.En.EksiKasaUyariBasit, StringComparison.Ordinal);
    }

    [Fact]
    public void T6_Para_aktarimi_banka_yoksa_bant_hesap_yoksa_bos_durum()
    {
        bankalar = """{"hesaplar":[]}""";
        kasalar = """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":100,"aktif":true},{"id":"k2","ad":"Vitrin","paraBirimi":"TRY","bakiye":0,"aktif":true}]""";
        var e = Ciz(ArayuzModuServisi.Basit, "/gider?sekme=virman");
        e.WaitForElement("#gd-v-banka-yok");
        Assert.Equal("/banka", e.Find("#gd-v-banka-ekle").GetAttribute("href"));
        Assert.NotNull(e.Find("#gd-v-kaynak"));   // kasa ↔ kasa aktarımı AYNEN mümkün
    }

    [Fact]
    public void T6_Para_aktarimi_tek_hesapta_form_yerine_bos_durum_ve_ekleme_yolu()
    {
        bankalar = """{"hesaplar":[]}""";
        var e = Ciz(ArayuzModuServisi.Basit, "/gider?sekme=virman");
        e.WaitForElement("#gd-v-hesap-yok-ekle");
        Assert.Empty(e.FindAll("#gd-v-kaynak"));
        Assert.Contains(Toplu6Sozlugu.Tr.VirmanHesapYok, e.Markup, StringComparison.Ordinal);
    }
}
