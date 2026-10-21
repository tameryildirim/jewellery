using System.Net;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET UX-1-C3 — "her veri girilen yer düzeltilebilir", sayfa grubu 3
//   /gider-pusulalari · /gider-pusulalari/{id}/belge · /uyum ·
//   /raporlar · /raporlar/kar-zarar · /raporlar/vade-takibi · /denetim-izi
//
// ÖLÇÜLEN ŞEY "metin var mı" DEĞİL:
//   • Sayfa başı GERÇEKTEN çiziliyor mu, başlığı dolu mu
//   • Düzenlenemeyen kayıtta düğme YERİNE gerekçe çiziliyor mu (UX-S25)
//     — negatif iddia (#Düzenle yok) pozitif eşiyle (#degismez-ipucu-…) çiftlenir
//   • Kilitli Kaydet SEBEBİNİ söylüyor mu (UX-S30)
//   • Ham veritabanı kodu (NIHAI_TUKETICI, TC_KIMLIK, KurSnapshot) basılıyor mu
//   • UX-4d'nin sunucuya yazdığı alanlar (karKaynagi, satisAlisFarkiTl,
//     netHasGramHareketi, faturaEslesme) EKRANA çiziliyor mu
//
// Bu dosya MEVCUT testlerin hiçbirini değiştirmez; yalnız EKLER (K4).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UX1C3EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    /// <summary>Aylık rapor yanıtı — UX-4d'nin (RaporModulu.Aylik) GERÇEK şekli.</summary>
    private string aylikYanit = """{"yil":2026,"ay":9, "satis":{"sayisi":4,"toplamTl":169000,"toplamHas":23.657192}, "alis":{"sayisi":1,"toplamTl":340500,"toplamHas":50}, "tahsilat":{"toplamTl":90850}, "veresiye":{"toplamTl":98300}, "karTl":169000,"karKaynagi":"KAR_ZARAR_SERVISI","satisAlisFarkiTl":-171500, "netHasGramHareketi":425.72,"hareketSayisi":6,"gunler":[]}""";

    /// <summary>Gün sonu mutabakatı — UX-4d md.11 faturaEslesme zarfıyla.</summary>
    private string mutabakatYaniti =
    """{"tarih":"2026-09-16", "tahsilat":{"nakit":42850,"kart":48000,"havale":0,"diger":0,"toplam":90850}, "satis":{"sayisi":4,"toplamTl":169000,"faturaliSayisi":0,"faturaliTl":0, "faturasizSayisi":4,"faturasizTl":169000}, "alis":{"sayisi":1,"toplamTl":340500,"faturaSayisi":0,"faturaToplamTl":0}, "hurda":{"sayisi":1,"odenenTl":74196,"hasGrami":50,"pusulaSayisi":1, "pusulaBrutTl":74196,"stopajToplamTl":0,"belgesizSayisi":0, "gecikmisPusulaSayisi":0}, "fatura":{"sayisi":1,"toplamTl":142500,"kdvToplam":0,"satisFaturasiTl":142500}, "faturaEslesme":{"bagliSayisi":0,"bagliTl":0,"bagimsizSayisi":1, "bagimsizTl":142500,"faturasizTl":169000, "farkTanimi":"FARK = satış toplamı − fişe bağlı faturalar − fişe bağlanmamış faturalar", "bagimsizFaturalar":[{"id":"f1","faturaNo":"SAT-2026-000007", "tarih":"2026-09-16T13:00:00Z","toplam":142500, "cariId":"c1","durum":"KESILDI"}]}, "fark":26500,"mutabik":false,"uyarilar":[],"faturasizIslemler":[]}""";

    /// <summary>Gider pusulası belgesi — işletme kimliği DOLU hâl.</summary>
    private string belgeYaniti = BelgeYaz(
        adres: "Kuyumcular Çarşısı No:5 Fatih/İstanbul",
        vergiDairesi: "Fatih", vergiNo: "1234567890");

    private static string BelgeYaz(string? adres, string? vergiDairesi, string? vergiNo) => $$"""{"belgeTuru":"GİDER PUSULASI","dayanak":"213 sayılı VUK md.234", "pusulaNo":"GP-2026-000001","duzenlemeTarihi":"2026-09-16T10:00:00Z", "teslimTarihi":"2026-09-16T10:00:00Z","duzenlemeSonTarihi":"2026-09-23T10:00:00Z", "gecikti":false, "duzenleyen":{"unvan":"Deneme Kuyumculuk","sube":"Merkez", "adres":{{Json(adres)}},"vergiDairesi":{{Json(vergiDairesi)}}, "vergiNo":{{Json(vergiNo)}},"telefon":null}, "satici":{"saticiTipi":"NIHAI_TUKETICI", "saticiTipiAciklama":"Vergi mükellefi olmayan (zati eşya satışı)", "adSoyad":"Ayşe Yılmaz","tckn":"10000000146","adres":"Kadıköy"}, "mal":{"cins":"Hurda altın (22K — 916/1000)","gramAgirlik":54.6, "hasGrami":50,"aciklama":null}, "tutarlar":{"brutTutar":74196,"stopajOran":0,"stopajOranYuzde":"%0", "stopajTutar":0,"netOdenen":74196, "yaziyla":"YETMİŞDÖRTBİNYÜZDOKSANALTI TL"}, "not":"Vergi mükellefi olmayan kişiden alımda gelir vergisi tevkifatı yapılmamıştır."}""";

    private static string Json(string? d) => d is null ? "null" : $"\"{d}\"";

    public UX1C3EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(
            new SahteApi(istekler, () => aylikYanit, () => mutabakatYaniti, () => belgeYaniti))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<RaporServisi>();   // paket 34 — Rapor Merkezi kataloğu
    }

    private int Sayim(string parca) => istekler.Count(i => i.Yol.Contains(parca));

    /// <summary>Sayfa başı sözleşmesi: bileşen var, başlığı DOLU.</summary>
    private static void SayfaBasiVar(IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> c, string anahtar)
    {
        Assert.NotNull(c.Find($"#sayfa-basi-{anahtar}"));
        Assert.False(string.IsNullOrWhiteSpace(
            c.Find($"#sayfa-basi-{anahtar}-baslik").TextContent.Trim()));
    }

    /// <summary>Gerekçe metni kullanıcıya iç paket/dalga kodu SIZDIRMAZ.</summary>
    private static void GerekceDurust(string metin)
    {
        Assert.False(string.IsNullOrWhiteSpace(metin));
        Assert.DoesNotContain("paket", metin, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dalga", metin, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADR-", metin, StringComparison.Ordinal);
    }

    // ── Sayfa başı: 7 sayfanın hepsinde ──────────────────────────────────────

    [Fact]
    public void C3_01_Gider_pusulalari_sayfa_basi_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>().AyrintiAc();
        c.WaitForAssertion(() => SayfaBasiVar(c, "gider-pusulalari"));
    }

    [Fact]
    public void C3_02_Uyum_sayfa_basi_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.Uyum>();
        c.WaitForAssertion(() => SayfaBasiVar(c, "uyum"));
    }

    [Fact]
    public void C3_03_Raporlar_sayfa_basi_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.Raporlar>();
        c.WaitForAssertion(() => SayfaBasiVar(c, "raporlar"));
    }

    [Fact]
    public void C3_04_Kar_zarar_sayfa_basi_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => SayfaBasiVar(c, "kar-zarar"));
    }

    [Fact]
    public void C3_05_Vade_takibi_sayfa_basi_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.VadeTakibi>();
        c.WaitForAssertion(() => SayfaBasiVar(c, "vade-takibi"));
    }

    [Fact]
    public void C3_06_Denetim_izi_sayfa_basi_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        c.WaitForAssertion(() => SayfaBasiVar(c, "denetim-izi"));
    }

    [Fact]
    public void C3_07_Pusula_belgesi_sayfa_basi_KAGIDA_CIKMAZ()
    {
        var c = Render<Kuyumcu.Web.Pages.PusulaBelgesi>(p => p.Add(x => x.Id, "gp1"));
        c.WaitForAssertion(() => SayfaBasiVar(c, "gider-pusulasi-belge"));
        // Sayfa başı yalnız ekranda: yazdırmada gizlenen kapsayıcının içinde.
        Assert.Contains("yazdirma-gizle", c.Find(".belge-arac").ClassName);
    }

    // ── Gider pusulası: düzenleme yolu yerine GEREKÇE ─────────────────────────

    [Fact]
    public void C3_10_Pusula_detayi_CEKMECEDE_acilir_ve_YANILTICI_Kaydet_YOKTUR()
    {
        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>().AyrintiAc();
        c.WaitForState(() => c.FindAll("table tbody tr").Count > 0);
        c.FindAll("button").First(b => b.TextContent.Contains("Detay")).Click();

        c.WaitForAssertion(() => Assert.NotNull(c.Find("#cekmece-gider-pusulasi")));
        var cek = c.Find("#cekmece-gider-pusulasi");
        Assert.Equal("dialog", cek.GetAttribute("role"));
        Assert.Equal("true", cek.GetAttribute("aria-modal"));
        Assert.Equal("cekmece-gider-pusulasi-baslik", cek.GetAttribute("aria-labelledby"));

        // ⚠ PAKET S-3d (QA turu 4, KRİTİK 3) — SÖZLEŞME DEĞİŞTİ.
        //   S-3b'de çekmecede düzenlenecek ALAN YOKTU, bu yüzden "Kaydet"
        //   çizilmiyordu. Kurucunun "her veri girişi düzenlenebilir" şartı
        //   gereği MALİ KİMLİK DIŞI iki alan (adres · mal açıklaması) artık
        //   yerinde düzeltiliyor → Kaydet GERÇEK bir istek atar, dolayısıyla
        //   çizilir ve YANILTICI DEĞİLDİR. Kilit metni yalnız İPTALLİ pusulada
        //   çıkar (aşağıdaki iddia onu da çiviler).
        Assert.NotEmpty(c.FindAll("#cekmece-gider-pusulasi-kaydet"));
        Assert.NotNull(c.Find("#gp-duz-adres"));
        Assert.NotNull(c.Find("#gp-duz-mal"));
        // ⚠ PAKET S-3f (QA turu 5, YÜKSEK) — SÖZLEŞME YİNE DEĞİŞTİ.
        //   Adres ve mal açıklaması VUK md.234'ün zorunlu unsurlarıdır:
        //   düzeltilebilir ama BOŞ BIRAKILAMAZ. Bu fikstürde malAciklamasi
        //   null geldiği için Kaydet KİLİTLİDİR ve sebebi ekranda YAZILIDIR —
        //   sessiz etkisizlik yerine adı konmuş bir kilit (UX-S30). Kilidin
        //   VARLIĞI değil, SEBEPSİZ olması kusur sayılır.
        var kilit = c.FindAll("#cekmece-gider-pusulasi-kilit");
        Assert.NotEmpty(kilit);
        GerekceDurust(kilit[0].TextContent);
        Assert.NotNull(c.Find("#gp-duz-mal-hata"));
        // Mali alanlar için gerçek düzeltme yolu (iptal) düğmesi VARDIR.
        Assert.NotEmpty(c.FindAll("[id^=gp-iptal-]"));
        // Ve mali alanların DEĞİŞMEZLİK gerekçesi hâlâ ekranda yazılıdır.
        GerekceDurust(c.Find("#degismez-ipucu-GiderPusulasi").TextContent);
    }

    [Fact]
    public void C3_11_Pusula_detayinda_DUZENLE_dugmesi_YOK_gerekce_VAR()
    {
        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>().AyrintiAc();
        c.WaitForState(() => c.FindAll("table tbody tr").Count > 0);
        c.FindAll("button").First(b => b.TextContent.Contains("Detay")).Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#degismez-ipucu-GiderPusulasi")));

        // Pozitif eş: gerekçe + doğru düzeltme yolu yazılı.
        var ipucu = c.Find("#degismez-ipucu-GiderPusulasi").TextContent;
        GerekceDurust(ipucu);
        Assert.Contains("VUK", ipucu, StringComparison.OrdinalIgnoreCase);

        // Negatif iddia: çekmece gövdesinde "Düzenle" düğmesi YOK.
        Assert.DoesNotContain(
            c.FindAll("#cekmece-gider-pusulasi-govde button"),
            b => b.TextContent.Contains("Düzenle"));
    }

    [Fact]
    public void C3_12_Pusula_detayinda_HAM_KOD_basilmaz()
    {
        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>().AyrintiAc();
        c.WaitForState(() => c.FindAll("table tbody tr").Count > 0);
        c.FindAll("button").First(b => b.TextContent.Contains("Detay")).Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#cekmece-gider-pusulasi-govde")));

        var govde = c.Find("#cekmece-gider-pusulasi-govde").TextContent;
        Assert.DoesNotContain("NIHAI_TUKETICI", govde, StringComparison.Ordinal);
        Assert.Contains("Vergi mükellefi olmayan", govde, StringComparison.Ordinal);
    }

    [Fact]
    public void C3_13_Stopaj_sifirken_listede_de_TUTAR_yazar()
    {
        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>().AyrintiAc();
        c.WaitForState(() => c.FindAll("table tbody tr").Count > 0);
        // KESIF-P3 (#11): TCKN ayrı sütun değil (satıcının alt satırında) → hücre konumla değil
        // kendi etiketiyle (data-etiket = Stopaj) bulunur.
        var hucre = c.FindAll("table tbody tr:first-child td")
            .First(td => td.GetAttribute("data-etiket") == Kuyumcu.Core.I18n.Sozlukler.Tr.Pusula.Stopaj).TextContent;
        Assert.DoesNotContain("—", hucre, StringComparison.Ordinal);
        Assert.Contains("0,00", hucre, StringComparison.Ordinal);
    }

    // ── Gider pusulası belgesi: işletme kimliği (VUK md.234) ─────────────────

    [Fact]
    public void C3_20_Belgede_ISLETME_ADRESI_ve_VERGI_NO_basilir()
    {
        var c = Render<Kuyumcu.Web.Pages.PusulaBelgesi>(p => p.Add(x => x.Id, "gp1"));
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#gpb-duzenleyen-adres")));

        Assert.Contains("Kuyumcular Çarşısı", c.Find("#gpb-duzenleyen-adres").TextContent);
        var vergi = c.Find("#gpb-duzenleyen-vergi").TextContent;
        Assert.Contains("Fatih", vergi, StringComparison.Ordinal);
        Assert.Contains("1234567890", vergi, StringComparison.Ordinal);
        // Bilgi tamken "doldurun" ipucu ÇIKMAZ.
        Assert.Empty(c.FindAll("#degismez-ipucu-IsletmeKimligi"));
    }

    [Fact]
    public void C3_21_Isletme_bilgisi_EKSIKSE_uydurulmaz_doldurma_yolu_gosterilir()
    {
        belgeYaniti = BelgeYaz(adres: null, vergiDairesi: null, vergiNo: null);

        var c = Render<Kuyumcu.Web.Pages.PusulaBelgesi>(p => p.Add(x => x.Id, "gp1"));
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#degismez-ipucu-IsletmeKimligi")));

        Assert.Empty(c.FindAll("#gpb-duzenleyen-adres"));
        Assert.Empty(c.FindAll("#gpb-duzenleyen-vergi"));
        GerekceDurust(c.Find("#degismez-ipucu-IsletmeKimligi").TextContent);
        Assert.NotNull(c.Find("#gpb-isletme-karti"));
    }

    // ── Uyum: MASAK kayıtlarının düzeltme yolu ───────────────────────────────

    [Fact]
    public void C3_30_Kimlik_tespiti_CEKMECEDE_acilir_gerekce_VAR_duzenle_YOK()
    {
        var c = Render<Kuyumcu.Web.Pages.Uyum>();
        c.WaitForState(() => c.FindAll("table tbody tr").Count > 0);
        c.FindAll("button").First(b => b.TextContent.Contains("Görüntüle")).Click();

        c.WaitForAssertion(() => Assert.NotNull(c.Find("#cekmece-kimlik-tespiti")));
        Assert.NotNull(c.Find("#cekmece-kimlik-tespiti-kaydet").GetAttribute("disabled"));
        GerekceDurust(c.Find("#degismez-ipucu-MasakKimlikTespiti").TextContent);
        Assert.DoesNotContain(
            c.FindAll("#cekmece-kimlik-tespiti-govde button"),
            b => b.TextContent.Contains("Düzenle"));
    }

    [Fact]
    public void C3_31_Kimlik_belgesi_turu_HAM_KOD_olarak_basilmaz()
    {
        var c = Render<Kuyumcu.Web.Pages.Uyum>();
        c.WaitForState(() => c.FindAll("table tbody tr").Count > 0);

        var tablo = c.Find("table").TextContent;
        Assert.DoesNotContain("TC_KIMLIK", tablo, StringComparison.Ordinal);
        Assert.Contains("kimlik kartı", tablo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void C3_32_Cekmece_ESCAPE_ile_kapanir()
    {
        var c = Render<Kuyumcu.Web.Pages.Uyum>();
        c.WaitForState(() => c.FindAll("table tbody tr").Count > 0);
        c.FindAll("button").First(b => b.TextContent.Contains("Görüntüle")).Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#cekmece-kimlik-tespiti")));

        c.Find("#cekmece-kimlik-tespiti").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        c.WaitForAssertion(() => Assert.Empty(c.FindAll("#cekmece-kimlik-tespiti")));
    }

    [Fact]
    public void C3_33_Supheli_bildiriminde_duzeltme_yolu_YAZILIDIR()
    {
        var c = Render<Kuyumcu.Web.Pages.Uyum>();
        c.WaitForState(() => c.FindAll("[role=tab]").Count > 0);
        c.FindAll("[role=tab]").First(t => t.TextContent.Contains("Şüpheli")).Click();

        c.WaitForAssertion(() => Assert.NotNull(c.Find("#degismez-ipucu-MasakSupheliBildirim-sb1")));
        var metin = c.Find("#degismez-ipucu-MasakSupheliBildirim-sb1").TextContent;
        GerekceDurust(metin);
        Assert.Contains("KAPATILIR", metin, StringComparison.Ordinal);
    }

    [Fact]
    public void C3_34_Yeni_bildirimde_GECERSIZ_tutar_alan_alti_mesaj_verir()
    {
        var c = Render<Kuyumcu.Web.Pages.Uyum>();
        c.WaitForState(() => c.FindAll("#uy-yeni-bildirim").Count > 0);
        c.Find("#uy-yeni-bildirim").Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#sb-tutar")));

        var oncesi = Sayim("/masak/supheli");
        c.Find("#sb-tutar").Input("-5");

        // Sessiz etkisizlik YASAK: mesaj var, istek YOK, Kaydet kilitli + sebebi yazılı.
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#sb-tutar-hata")));
        Assert.Equal(oncesi, Sayim("/masak/supheli"));
        Assert.NotNull(c.Find("#sb-kaydet").GetAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(c.Find("#sb-kaydet-kilit").TextContent));
    }

    // ── Raporlar: UX-4d alanları EKRANA çizilir ──────────────────────────────

    [Fact]
    public void C3_40_Kar_karti_KAYNAGINI_yazar_ve_satis_alis_farki_AYRI_karttir()
    {
        var c = Render<Kuyumcu.Web.Pages.Raporlar>();
        c.WaitForAssertion(() => Assert.Contains("Kâr kaynağı", c.Markup, StringComparison.Ordinal));

        Assert.Contains("Kâr-zarar hesabı", c.Markup, StringComparison.Ordinal);
        // Eski "KÂR (satış − alış)" rakamı artık KENDİ adıyla, ayrı kartta.
        Assert.Contains("Alış-satış nakit farkı", c.Markup, StringComparison.Ordinal);
        Assert.Contains("171.500", c.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void C3_41_Net_has_GUNLUK_ile_ayni_tanimdan_cizilir()
    {
        var c = Render<Kuyumcu.Web.Pages.Raporlar>();
        // PAKET RAPOR-3 (hasım ORTA): etiket kapsamı söyler — "Ayın tüm altın hareketi (açılış, sayım, emanet dâhil)".
        c.WaitForAssertion(() => Assert.Contains(Kuyumcu.Core.I18n.KarRaporSozlugu.Tr.AyinTumAltinHareketi, c.Markup, StringComparison.Ordinal));

        // netHasGramHareketi (425,72) çizilir; alış-satış has farkı (26,3428) DEĞİL.
        // P-57: Bicim.Has artık tr-TR ("425,7200 has") — ertelenen karışık sayı
        //   biçimi bulgusu P-57 ile kapandı; beklenen metin virgül ondalık.
        Assert.Contains("425,7200", c.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("26,3428", c.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void C3_42_Veresiye_ve_tahsilat_kartlari_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.Raporlar>();
        c.WaitForAssertion(() => Assert.Contains("Veresiye satış", c.Markup, StringComparison.Ordinal));
        Assert.Contains("98.300", c.Markup, StringComparison.Ordinal);
        Assert.Contains("90.850", c.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void C3_43_Kar_HESAPLANAMADIYSA_sifir_YAZILMAZ()
    {
        aylikYanit =
    """{"yil":2026,"ay":9,"satis":{"sayisi":0,"toplamTl":0,"toplamHas":0}, "alis":{"sayisi":0,"toplamTl":0,"toplamHas":0}, "tahsilat":{"toplamTl":0},"veresiye":{"toplamTl":0}, "karTl":null,"karKaynagi":"HESAPLANAMADI","satisAlisFarkiTl":0, "netHasGramHareketi":0,"hareketSayisi":0,"gunler":[]}""";

        var c = Render<Kuyumcu.Web.Pages.Raporlar>();
        c.WaitForAssertion(() => Assert.Contains("Hesaplanamadı", c.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void C3_44_Gun_sonu_FARK_TANIMI_ve_bagsiz_faturalar_gorunur()
    {
        var c = Render<Kuyumcu.Web.Pages.Raporlar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#mut-fark-tanimi")));

        Assert.Contains("FARK =", c.Find("#mut-fark-tanimi").TextContent, StringComparison.Ordinal);
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#mut-bagsiz-faturalar")));
        Assert.Contains("SAT-2026-000007",
            c.Find("#mut-bagsiz-faturalar").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void C3_45_Raporda_duzenleme_yoktur_gerekce_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.Raporlar>();
        // ZO-BELGE: gerekçe açılışta değil, KAPALI "rakam yanlışsa" katlanırında.
        c.WaitForElement("#rp-duzeltme-ac");
        Assert.Empty(c.FindAll("#degismez-ipucu-Rapor"));
        c.Find("#rp-duzeltme-ac").Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#degismez-ipucu-Rapor")));
        GerekceDurust(c.Find("#degismez-ipucu-Rapor").TextContent);
    }

    // ── Kâr-zarar ────────────────────────────────────────────────────────────

    [Fact]
    public void C3_50_Adet_para_gibi_bicimlenmez()
    {
        // PAKET RAPOR-3: satışsız dönem artık boş durum çizer — sahte özet 10 satış taşır (adet biçimi sınanabilsin).
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.Contains("Maliyeti bilinen", c.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain("5,000", c.Markup, StringComparison.Ordinal);
        Assert.Contains("<strong class=\"sayi\">5</strong>", c.Markup, StringComparison.Ordinal);
    }

    // ── Vade takibi ──────────────────────────────────────────────────────────

    [Fact]
    public void C3_60_Musteri_adi_cari_kartina_telefon_aramaya_gider()
    {
        var c = Render<Kuyumcu.Web.Pages.VadeTakibi>();
        c.WaitForState(() => c.FindAll("#vade-cari-c1").Count > 0);

        Assert.Contains("/cariler?cariId=c1", c.Find("#vade-cari-c1").GetAttribute("href")!);
        Assert.Equal("tel:05321112233", c.Find("#vade-tel-c1").GetAttribute("href"));
    }

    [Fact]
    public void C3_61_Ufuk_kisayolu_GERCEK_istek_atar()
    {
        var c = Render<Kuyumcu.Web.Pages.VadeTakibi>();
        c.WaitForState(() => c.FindAll("#vade-ufuk-30").Count > 0);

        c.Find("#vade-ufuk-30").Click();

        c.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("ufukGun=30")));
        Assert.False(string.IsNullOrWhiteSpace(c.Find("#vade-ufuk-aciklama").TextContent));
    }

    // ── Denetim izi ──────────────────────────────────────────────────────────

    [Fact]
    public void C3_70_Kayit_turu_TEKNIK_sinif_adi_olarak_basilmaz()
    {
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        c.Find("#dz-suzgec-ac").Click();   // ZO-BELGE: süzgeç KAPALI katlanırda
        c.WaitForState(() => SeciciYardim.Secenekler(c, "f-varlik").Count > 1);

        var secenekler = SeciciYardim.ListeMetni(c, "f-varlik");
        Assert.DoesNotContain("KurSnapshot", secenekler, StringComparison.Ordinal);
        Assert.Contains("Kur kaydı", secenekler, StringComparison.Ordinal);
    }

    [Fact]
    public void C3_71_Denetim_izinde_DUZENLE_YOK_gerekce_VAR()
    {
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        // ZO-BELGE: gerekçe açılışta değil, "Süzgeç" katlanırında (yasal dayanakla).
        c.Find("#dz-suzgec-ac").Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#degismez-ipucu-DenetimKaydi")));
        GerekceDurust(c.Find("#degismez-ipucu-DenetimKaydi").TextContent);
        Assert.DoesNotContain(c.FindAll("button"), b => b.TextContent.Contains("Düzenle"));
    }

    [Fact]
    public void C3_72_Yalniz_hassas_onay_kutusunun_ADI_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        c.Find("#dz-suzgec-ac").Click();   // ZO-BELGE: süzgeç KAPALI katlanırda
        var kutu = c.Find("#f-yalniz-hassas");
        Assert.False(string.IsNullOrWhiteSpace(kutu.GetAttribute("aria-label")));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Sahte API — bu paketin dokunduğu HER uca gerçek yanıt şekli verilir
    // ('{}' varsayılanı bırakılmaz; S-2'nin NRE'si tam da oradan doğmuştu).
    // ═════════════════════════════════════════════════════════════════════════
    private sealed class SahteApi(
        List<(string, string, string)> kayit,
        Func<string> aylik,
        Func<string> mutabakat,
        Func<string> belge) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var tam = istek.RequestUri!.PathAndQuery;
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, tam, govde));

            var g = yol switch
            {
                var y when y.EndsWith("/belge") => belge(),
                var y when y.Contains("/gider-pusulalari/sure-takibi") =>
                    """{"gecikmisSayisi":0,"yaklasanSayisi":0, "sure":{"gun":7,"dayanak":"VUK md.234"},"uyarilar":[]}""", var y when y.EndsWith("/gider-pusulalari") => """ [{"id":"gp1","pusulaNo":"GP-2026-000001","saticiAdSoyad":"Ayşe Yılmaz", "saticiTckn":"100*****146","ayarId":"22K","gramAgirlik":54.6, "brutTutar":74196,"stopajOran":0,"stopajTutar":0,"netOdenen":74196, "teslimTarihi":"2026-09-16T10:00:00Z", "duzenlemeSonTarihi":"2026-09-23T10:00:00Z","gecikti":false,"kalanGun":7}]""",
                var y when y.Contains("/gider-pusulalari/") =>
            """{"id":"gp1","pusulaNo":"GP-2026-000001","saticiAdSoyad":"Ayşe Yılmaz", "saticiTckn":"10000000146","saticiAdres":"Kadıköy", "saticiTipi":"NIHAI_TUKETICI","ayarId":"22K","gramAgirlik":54.6, "hasGrami":50,"brutTutar":74196,"stopajOran":0,"stopajTutar":0, "netOdenen":74196,"malAciklamasi":null, "teslimTarihi":"2026-09-16T10:00:00Z", "duzenlemeSonTarihi":"2026-09-23T10:00:00Z","gecikti":false}""",
                var y when y.Contains("kimlik-tespitleri/ara") =>
                    """{"adet":0,"toplamTutarTl":0,"ilkIslem":null,"sonIslem":null,"kayitlar":[]}""",
                var y when y.EndsWith("/masak/kimlik-tespitleri") =>
            """[{"id":"kt1","tarih":"2026-09-16T09:00:00Z","adSoyad":"Ayşe Yılmaz", "belgeTuru":"TC_KIMLIK","belgeNo":"100*****146","islemTutariTl":340500, "saklamaBitis":"2034-09-16T09:00:00Z"}]""",
                var y when y.Contains("/masak/kimlik-tespitleri/") =>
            """{"id":"kt1","tarih":"2026-09-16T09:00:00Z","adSoyad":"Ayşe Yılmaz", "belgeTuru":"TC_KIMLIK","belgeNo":"10000000146","dogumTarihi":null, "uyruk":"TR","adres":"Kadıköy","islemTutariTl":340500, "saklamaBitis":"2034-09-16T09:00:00Z", "beyan":{"kademe":"BASIT","islemAmaci":"Yatırım","fonKaynagi":"Birikim", "ekBelgeReferansi":null,"aciklama":"Hurda altın satışı"}}""",
                var y when y.EndsWith("/masak/supheli") =>
            """[{"id":"sb1","hareketId":null,"cariId":null,"kimlikTespitiId":null, "islemTutariTl":250000,"gerekce":"Gerekçe metni", "tespitTarihi":"2026-09-16T09:00:00Z","sonBildirimTarihi":"2026-09-30T09:00:00Z", "kalanSaat":200,"durum":"ACIK","gonderimTarihi":null,"gonderimReferansi":null, "kapatmaGerekcesi":null,"bildirenKullaniciId":"u1", "saklamaBitis":"2034-09-16T09:00:00Z","olusturuldu":"2026-09-16T09:00:00Z", "guncellendi":"2026-09-16T09:00:00Z"}]""",
                var y when y.EndsWith("/masak/ozet") =>
                    """{"acikBildirim":1,"suresiGecmis":0,"yaklasan":1,"son30GunTespit":1}""",
                var y when y.EndsWith("/masak/saklama-onizleme") =>
                    """{"aciklama":"Saklama süresi 10 yıldır.", "suresiDolmus":{"kimlikTespiti":0,"beyan":0},"enYakinSaklamaBitisi":null}""", var y when y.Contains("pos-fatura-mutabakati") => mutabakat(), var y when y.Contains("/raporlar/aylik") => aylik(), var y when y.Contains("cari-yaslandirma") => """{"yaslananCariler":[],"ozetTl":0}""", var y when y.Contains("/raporlar/stok-degeri") => """{"urunler":[],"toplamHas":0}""", var y when y.Contains("vade-takibi") => """ {"ufukGun":7,"gecikenToplam":0,"yaklasanToplam":0,"vadesizToplam":0, "ufukOtesiToplam":78300,"acikToplam":78300, "cariler":[{"cariId":"c1","cariAd":"Ayşe Yılmaz","cariTur":"MUSTERI", "telefon":"05321112233","gecikenTutar":0,"yaklasanTutar":0, "vadesizTutar":0,"enEskiGecikmeGun":null,"enYakinVade":"2026-10-16", "borclar":[{"hareketId":"h1","tarih":"2026-09-16T13:00:00Z", "vade":"2026-10-16","acikTutar":78300,"gecikmeGun":-29, "durum":"VADESINDE"}]}]}""",
                var y when y.Contains("kar-zarar/urun") => """{"satirlar":[]}""",
                var y when y.Contains("kar-zarar") =>
                    """{"hasilatTl":0,"maliyetTl":0,"karTl":0,"karMarjiYuzde":null, "hasilatHas":0,"maliyetHas":0,"karHas":0,"satisAdedi":10, "maliyetiBilinenAdet":5.000,"maliyetiBilinmeyenAdet":5.000, "guvenilirlik":"TAM","uyari":null}""", var y when y.Contains("stok-degerlemesi") =>"""{"toplamAdet":0,"toplamHasGram":0,"maliyetliAdet":0,"maliyetsizAdet":0, "stokMaliyetiTl":0,"stokMaliyetiHas":0,"konsinyeUrunSayisi":0,"uyari":null}""",
                var y when y.EndsWith("/denetim/ozet") =>
                    """{"toplam":3,"hassasAdet":1,"enEskiKayit":"2026-09-11T09:00:00Z", "eylemler":[{"eylem":"OKUMA","adet":3}], "turler":[{"varlikTuru":"KurSnapshot","adet":2}, {"varlikTuru":"GiderPusulasi","adet":1}], "subeler":[]}""",
                var y when y.Contains("/denetim") => """{"toplam":1,"limit":100,"offset":0, "kayitlar":[{"id":"d1","tarih":"2026-09-16T09:00:00Z","eylem":"OKUMA", "varlikTuru":"KurSnapshot","varlikId":"k1","hassas":false, "detay":"kur okundu","ipAdresi":null,"kullaniciId":"u1", "kullanici":"QA Test","rol":"SAHIP","subeId":null,"subeAd":null, "kasaId":null,"kasaAd":null}]}""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0, "eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK", "olusturuldu":"2026-09-16T09:00:00Z","ayarFiyatlari":[]}""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    // ═══ ORTAK-1 · KEŞİF #4 (30.09, KRİTİK) — şüpheli işlem bildirimi tutarı tuş tuş "1.500" ═══════
    [Fact]
    public void OK1_K4_bildirim_tutari_belirsiz_1_500_POST_YOK_duzeltince_acilir()
    {
        var c = Render<Kuyumcu.Web.Pages.Uyum>();
        c.WaitForState(() => c.FindAll("#uy-yeni-bildirim").Count > 0);
        c.Find("#uy-yeni-bildirim").Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#sb-tutar")));
        c.Find("#sb-gerekce").Input(new string('a', 320));
        foreach (var a in new[] { "1", "1.", "1.5", "1.50", "1.500" }) c.Find("#sb-tutar").Input(a);
        c.Find("#sb-tutar").Blur();
        Assert.Equal("1.500", c.Find("#sb-tutar").GetAttribute("value"));
        Assert.NotNull(c.Find("#sb-kaydet").GetAttribute("disabled"));
        Assert.Contains(c.Find("label[for='sb-tutar']").TextContent.Replace("*", "").Split("(isteğe")[0].Trim(),
                        c.Find("#sb-sayi-kilit").TextContent);
        var oncesi = Sayim("/masak/supheli");
        c.Find("#sb-kaydet").Click();
        Assert.Equal(oncesi, Sayim("/masak/supheli"));

        c.Find("#sb-tutar").Input("1500");   // aynı gerekçe, geçerli tutar → kilit kalkar (kilidin sebebi kapıydı)
        c.WaitForAssertion(() => Assert.Null(c.Find("#sb-kaydet").GetAttribute("disabled")));
        Assert.Empty(c.FindAll("#sb-sayi-kilit"));
    }
}
