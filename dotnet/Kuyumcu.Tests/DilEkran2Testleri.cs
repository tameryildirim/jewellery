using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.Domain;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET DIL-EKRAN tur 2 — tasarım hakemi bulguları (BİRİM / AD SINIFI)
//
//   • EN modda "13,8760 has" · "3 adet" · "22 Ayar" · "Gümüş" Türkçe kalıyordu
//     (Bicim.Has/Miktar sabit birim, KurKaynakAdi.AyarAdi çekirdek Türkçe ad,
//     sunucunun gönderdiği MadenAdi ham basılıyordu).
//   • EN /ekranlar 375 px'te üst şerit ipucu 47 px sağa taşıyordu; 1440'ta uzun
//     İngilizce karo adları iki satıra bölünüyordu.
//
// Bicim.Dil STATİK'tir: bu sınıf testleri DisableParallelization koleksiyonunda
// koşar ve Dil'i HER testte geri alır (diğer testlerin "has/adet" beklentisi bozulmaz).
// ═════════════════════════════════════════════════════════════════════════════

[CollectionDefinition("DilEkranBicimDil", DisableParallelization = true)]
public sealed class DilEkranBicimDilKoleksiyonu { }

[Collection("DilEkranBicimDil")]
public sealed class DilEkran2Testleri : BunitContext
{
    private static readonly Regex TurkceHarf = new("[çğıİöşüÇĞÖŞÜ]", RegexOptions.Compiled);

    private static T Dilde<T>(string dil, Func<T> is_)
    {
        var onceki = Bicim.Dil;
        Bicim.Dil = dil;
        try { return is_(); } finally { Bicim.Dil = onceki; }
    }

    private static string Kok()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("Kuyumcu.sln bulunamadı");
    }

    private static string Oku(string goreli) => File.ReadAllText(Path.Combine(Kok(), goreli));

    // ── (1) has / adet birimi dile bağlı; sayı biçimi tr-TR sabit ────────────

    [Fact]
    public void Has_ve_Miktar_birimi_EN_modda_fine_ve_pcs_TR_modda_has_ve_adet()
    {
        Assert.Equal("13,8760 has", Dilde("tr", () => Bicim.Has(13.876m)));
        Assert.Equal("13,8760 fine", Dilde("en", () => Bicim.Has(13.876m)));
        Assert.Equal("1.447,3856 fine", Dilde("en", () => Bicim.Has(1447.3856m)));   // sayı dili değişmez
        Assert.Equal("−1,6030 has", Dilde("tr", () => Bicim.HasIsaretli(-1.603m)));
        Assert.Equal("−1,6030 fine", Dilde("en", () => Bicim.HasIsaretli(-1.603m)));
        Assert.Equal("0,0000 fine", Dilde("en", () => Bicim.HasIsaretli(-0.00004m)));

        Assert.Equal("3 adet", Dilde("tr", () => Bicim.Miktar(3m, "ADET")));
        Assert.Equal("3 pcs", Dilde("en", () => Bicim.Miktar(3m, "ADET")));
        Assert.Equal("4,25 pcs", Dilde("en", () => Bicim.Miktar(4.25m, "ADET")));
        Assert.Equal("1,000 g", Dilde("en", () => Bicim.Miktar(1m, "GRAM")));          // gram birimi evrensel
        Assert.Equal("3", Dilde("en", () => Bicim.Miktar(3m, null)));
        Assert.DoesNotMatch(TurkceHarf, Dilde("en", () => Bicim.Has(5m) + Bicim.Miktar(2m, "ADET")));
    }

    [Fact]
    public async Task DilServisi_dil_degisince_Bicim_Dil_i_yayar_yalniz_yayilim_acikken()
    {
        var yayilim = Bicim.DilYayilimi; var onceki = Bicim.Dil;
        try
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            var dil = new DilServisi(JSInterop.JSRuntime);
            Bicim.DilYayilimi = false; Bicim.Dil = "tr";
            await dil.DegistirAsync("en");
            Assert.Equal("tr", Bicim.Dil);                       // testlerde sızıntı yok
            Bicim.DilYayilimi = true;
            await dil.DegistirAsync("en");
            Assert.Equal("en", Bicim.Dil);
            await dil.DegistirAsync("tr");
            Assert.Equal("tr", Bicim.Dil);
            await dil.DegistirAsync("xx");                         // bilinmeyen → tr
            Assert.Equal("tr", Bicim.Dil);
        }
        finally { Bicim.DilYayilimi = yayilim; Bicim.Dil = onceki; }
    }

    [Fact]
    public void Cari_ekraninin_kendi_has_bicimleyicisi_birimi_dilden_alir()
    {
        Assert.Equal("0 has", Dilde("tr", () => Kuyumcu.Web.Pages.Cariler.HasBicim(0m)));
        Assert.Equal("0 fine", Dilde("en", () => Kuyumcu.Web.Pages.Cariler.HasBicim(0m)));
        Assert.Equal("1.234,5000 fine", Dilde("en", () => Kuyumcu.Web.Pages.Cariler.HasBicim(1234.5m)));
        Assert.Equal("−9,1600 fine", Dilde("en", () => Kuyumcu.Web.Pages.Cariler.HasBicim(-9.16m)));
        Assert.Equal("−9,1600 has", Dilde("tr", () => Kuyumcu.Web.Pages.Cariler.HasBicim(-9.16m)));
    }

    [Fact]
    public void Sozluklerdeki_has_birimi_EN_de_fine_dir_ve_Turkce_has_kalmaz()
    {
        Assert.Equal("has", KesifP2Sozlugu.Al("tr").BirimHas);
        Assert.Equal("fine", KesifP2Sozlugu.Al("en").BirimHas);
        Assert.Equal("fine", DilEkranSozlugu.HasBirimi("en"));
        Assert.Equal("has", DilEkranSozlugu.HasBirimi("tr"));
        Assert.Equal("pcs", DilEkranSozlugu.AdetBirimi("en"));
        Assert.Equal("adet", DilEkranSozlugu.AdetBirimi("tr"));
    }

    // ── (2) Ayar adı: TR çekirdek ad AYNEN; EN "22k" ailesi ──────────────────

    [Fact]
    public void Ayar_adi_TR_cekirdek_adi_EN_Turkce_harf_ve_Ayar_icermez_HER_ayar_icin()
    {
        Assert.True(Ayarlar.Tumu.Count >= 15, "ayar tablosu küçüldüyse tarama körleşir");
        foreach (var a in Ayarlar.Tumu.Values)
        {
            Assert.Equal(a.Ad, Dilde("tr", () => KurKaynakAdi.AyarAdi(a.Id)));
            var en = Dilde("en", () => KurKaynakAdi.AyarAdi(a.Id));
            Assert.NotEqual(a.Ad, en);
            Assert.DoesNotMatch(TurkceHarf, en);
            Assert.DoesNotContain("Ayar", en);
            Assert.False(string.IsNullOrWhiteSpace(en));
        }
        Assert.Equal("22k", Dilde("en", () => KurKaynakAdi.AyarAdi("K22")));
        Assert.Equal("14k", Dilde("en", () => KurKaynakAdi.AyarAdi("K14")));
        Assert.Equal("995 Commercial fine", Dilde("en", () => KurKaynakAdi.AyarAdi("K995")));
        Assert.Equal("925 Silver", Dilde("en", () => KurKaynakAdi.AyarAdi("G925")));
        Assert.Equal("950 Platinum", Dilde("en", () => KurKaynakAdi.AyarAdi("PT950")));
        Assert.Equal("999 Palladium", Dilde("en", () => KurKaynakAdi.AyarAdi("PD999")));
        Assert.Equal("Stainless steel", Dilde("en", () => KurKaynakAdi.AyarAdi("CELIK")));
        Assert.Equal("YENI9", Dilde("en", () => KurKaynakAdi.AyarAdi("YENI9")));          // tanınmayan kod AYNEN
        Assert.Equal("", KurKaynakAdi.AyarAdi(null));
    }

    // ── (3) Maden adı: sunucunun gönderdiği Türkçe ad ham basılmaz ───────────

    [Fact]
    public void Maden_adi_EN_modda_Ingilizce_TR_modda_cekirdek_ad_tanınmayan_kod_AYNEN()
    {
        foreach (var m in Maden.Hepsi.Append(Maden.Rodyum))
        {
            Assert.Equal(Maden.Ad(m), Dilde("tr", () => KurKaynakAdi.MadenAdi(m)));
            var en = Dilde("en", () => KurKaynakAdi.MadenAdi(m));
            Assert.DoesNotMatch(TurkceHarf, en);
            Assert.NotEqual(Maden.Ad(m), en);
            // Sunucu adı Türkçe gelse de ekran etkin dili seçer.
            Assert.Equal(en, Dilde("en", () => KurKaynakAdi.MadenAdi(m, Maden.Ad(m))));
            Assert.Equal(Maden.Ad(m), Dilde("tr", () => KurKaynakAdi.MadenAdi(m, Maden.Ad(m))));
        }
        Assert.Equal("Gold", Dilde("en", () => KurKaynakAdi.MadenAdi("ALTIN", "Altın")));
        Assert.Equal("Silver", Dilde("en", () => KurKaynakAdi.MadenAdi("GUMUS", "Gümüş")));
        Assert.Equal("Yeni Maden", Dilde("en", () => KurKaynakAdi.MadenAdi("YENIMADEN", "Yeni Maden")));   // uydurma ad yok
        Assert.Equal("", KurKaynakAdi.MadenAdi(null, null));
        Assert.Equal("sunucu", KurKaynakAdi.MadenAdi(null, "sunucu"));
    }

    // ── (4) Denetim izi detayı (basit mod) EN gösterimde çevrilir; TR değişmez ─

    [Fact]
    public void Denetim_detayi_BASIT_EN_gosterimde_liste_basi_ve_sabit_gerekceler_Ingilizce()
    {
        Assert.Equal("list: 7 records · customer list", Q105Sozlugu.DenetimDetayi("liste: 7 kayıt · cari listesi", "en", true));
        Assert.Equal("list: 0 records · identity check list", Q105Sozlugu.DenetimDetayi("liste: 0 kayıt · kimlik tespitleri listesi", "en", true));
        Assert.Equal("list: 0 records · suspicious report list", Q105Sozlugu.DenetimDetayi("liste: 0 kayıt · şüpheli bildirim listesi", "en", true));
        Assert.Equal("list: 25 records · transaction ledger", Q105Sozlugu.DenetimDetayi("liste: 25 kayıt · islem-defteri", "en", true));
        Assert.Equal("list: 3 records", Q105Sozlugu.DenetimDetayi("liste: 3 kayıt", "en", true));
        Assert.Equal("list: 9 records · backup summary", Q105Sozlugu.DenetimDetayi("liste: 9 kayıt · yedek özeti", "en", true));
        Assert.Equal("SATIS document viewed", Q105Sozlugu.DenetimCumleleriEn("SATIS belgesi görüntülendi", "en"));
        Assert.Equal("withholding list downloaded (accountant) · period 2026-09 · 12 rows · full ID no.",
            Q105Sozlugu.DenetimCumleleriEn("stopaj listesi indirildi (mali müşavir) · dönem 2026-09 · 12 satır · tam TCKN", "en"));
        Assert.Equal("list: 4 kayıt", Q105Sozlugu.DenetimCumleleriEn("list: 4 kayıt", "tr"));   // TR'de dokunulmaz
        // TR gösterim ve PRO (denetçi ham kayıt) DEĞİŞMEZ
        Assert.Equal("liste: 7 kayıt · müşteri listesi", Q105Sozlugu.DenetimDetayi("liste: 7 kayıt · cari listesi", "tr", true));
        Assert.Equal("liste: 7 kayıt · cari listesi", Q105Sozlugu.DenetimDetayi("liste: 7 kayıt · cari listesi", "en", false));
        // Tanınmayan metin AYNEN (ad uydurulmaz)
        Assert.Equal("Bilinmeyen Gerekçe", Q105Sozlugu.DenetimDetayi("Bilinmeyen Gerekçe", "en", true));
    }

    // ── (5) Menü adları 8 sütunlu karoya sığar (163 px karo, 143 px yazı alanı) ─

    [Fact]
    public void EN_menu_adlari_gunluk_is_disindaki_karolarda_en_cok_18_karakter()
    {
        // Ölçüm (gerçek Chrome, 16 px): 18 karakter ≈ 141 px ≤ 143 px. Günlük iş satırı nowrap ve
        // yazıya göre genişler (tuval-kabuk.css), muaf.
        var uzun = MenuTanimi.Ogeler.Concat(MenuTanimi.AltBlok)
            .Where(o => o.Grup != MenuGrubu.Gunluk)
            .Select(o => (o.Yol, Ad: o.Etiket(Sozlukler.En)))
            .Where(x => x.Ad.Length > 18).ToList();
        Assert.True(uzun.Count == 0, "karoya sığmayan EN ad: " + string.Join(" | ", uzun.Select(x => $"{x.Yol}='{x.Ad}'")));
        // Kısaltılan adların eski/uzun karşılığı ARAMADA bulunur (Esanlam)
        Assert.Contains("Expenses and transfers", MenuAdlari.En.GiderVirman.Esanlam);
        Assert.Contains("Cheques and notes", MenuAdlari.En.CekSenet.Esanlam);
        Assert.Contains("Scrap and refinery", MenuAdlari.En.HurdaRamat.Esanlam);
        Assert.Contains("MASAK", MenuAdlari.En.Uyum.Esanlam);
    }

    // ── (6) CSS ve kaynak kilitleri (ekrana dokunan sınıf; ölçüm Chrome'da yapıldı) ─

    [Fact]
    public void Tuval_css_dar_ekranda_ipucunu_sarar_ve_8_sutunda_karo_dolgusunu_daraltir()
    {
        var css = Oku("Kuyumcu.Web/wwwroot/css/tuval-kabuk.css");
        Assert.Matches(new Regex(@"@media \(max-width: 1199px\)\s*\{\s*\.ekranlar-sayfa \.ekranlar-ipucu\s*\{[^}]*white-space:\s*normal", RegexOptions.Singleline), css);
        Assert.Matches(new Regex(@"@media \(min-width: 1400px\)\s*\{\s*\.ekranlar-sayfa \.ekran-karo\s*\{[^}]*padding-left:\s*10px[^}]*font-size:\s*16px", RegexOptions.Singleline), css);
    }

    [Fact]
    public void Razor_kaynaginda_ham_MadenAdi_ve_birim_literali_yok_Emanet_has_sayisi_birimi_dilden_keser()
    {
        var web = Path.Combine(Kok(), "Kuyumcu.Web");
        var ham = new List<string>();
        foreach (var f in Directory.EnumerateFiles(Path.Combine(web, "Pages"), "*.razor"))
        {
            var t = Regex.Replace(File.ReadAllText(f), @"@\*.*?\*@", "", RegexOptions.Singleline);
            foreach (Match m in Regex.Matches(t, @"(?<![\w.])\w+(?:\[\d+\])?(?:\??\.\w+)*\??\.MadenAdi\b(?!\()"))
            {
                var once = t[..m.Index];
                if (once.LastIndexOf("KurKaynakAdi.MadenAdi(", StringComparison.Ordinal) > once.LastIndexOf(')')) continue;
                ham.Add($"{Path.GetFileName(f)}: {m.Value}");
            }
            foreach (var satir in t.Split('\n').Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)))
                foreach (Match m in Regex.Matches(satir, "(?:\\\\u00a0|\\s)(?:has|adet)\""))
                    ham.Add($"{Path.GetFileName(f)}: birim literali {m.Value}");
        }
        Assert.True(ham.Count == 0, string.Join("\n", ham));
        var emanet = Oku("Kuyumcu.Web/Pages/Emanet.razor");
        Assert.Contains("DilEkranSozlugu.HasBirimi(Bicim.Dil)", emanet);
        Assert.DoesNotContain("EndsWith(\" has\"", emanet);
        var cariler = Oku("Kuyumcu.Web/Pages/Cariler.razor");
        Assert.Contains("DilEkranSozlugu.HasBirimi(Bicim.Dil)", cariler);
    }

    // ── (7) 375 px'te İngilizce sekme / başlık / satır metni taşmaz (gerçek Chrome ölçümü: K3 tasan/kirik 0) ─

    [Fact]
    public void Dar_ekranda_sekme_seridi_esnek_sarar_ve_gider_seridi_de_sarar()
    {
        var app = Oku("Kuyumcu.Web/wwwroot/css/app.css");
        Assert.Matches(new Regex(@"@media \(max-width: 600px\)\s*\{[^@]*?\.sekmeler-serit\s*\{[^}]*display:\s*flex;\s*flex-wrap:\s*wrap", RegexOptions.Singleline), app);
        Assert.DoesNotMatch(new Regex(@"\.sekmeler-serit\s*\{[^}]*grid-template-columns:\s*repeat\(2"), app);
        var t5 = Oku("Kuyumcu.Web/wwwroot/css/toplu5.css");
        Assert.Contains("#gd-sekme-seridi { display: flex; flex-wrap: wrap; }", t5);
        Assert.DoesNotContain("#gd-sekme-seridi { display: flex; flex-wrap: nowrap; }", t5);
    }

    [Fact]
    public void Bildirimler_calisma_anahtari_ISO_tarihi_tire_yerinden_bolunmez_elle_tetiklenen_satir_haric()
    {
        var b = Oku("Kuyumcu.Web/Pages/Bildirimler.razor");
        Assert.Matches(new Regex(@"Anahtar = ""anahtar"".*?HucreSinif = c => c\.Elle \? ""metin-kucuk"" : ""metin-kucuk hucre-tek-satir""", RegexOptions.Singleline), b);
        Assert.Contains(".hucre-tek-satir { white-space: nowrap; }", Oku("Kuyumcu.Web/wwwroot/css/app.css"));
    }

    [Fact]
    public void EN_kurulum_adim_metinleri_kisa_ve_ekranda_ISO_para_kodu_TRY_yok()
    {
        var t = Toplu4Sozlugu.Al("en");
        Assert.Equal("{0} rate(s)", t.KurulumSayiKur);
        Assert.Equal("{0} account(s)", t.KurulumSayiKasaBanka);
        Assert.Equal("{0} customer(s)", t.KurulumSayiCariAcilis);
        Assert.Equal("Go to step", Sozlukler.En.Kurulum.AdimaGit);
        Assert.Equal("Shop & Branches", Sozlukler.En.Isletme.Baslik);
        Assert.True(Sozlukler.En.Isletme.Baslik.Length <= 16, "375 px'te 36 px başlık tek satıra sığmalı");
        // TR kullanıcısı TL, EN kullanıcısı ₺ görür; ISO kodu "TRY" ekranda görünen etiketlerde YOK
        var g = RaporGrafikSozlugu.Al("en");
        Assert.DoesNotContain("TRY", g.GelirGiderKar);
        Assert.DoesNotContain("TRY", g.GirisCikisGrafik);
        Assert.DoesNotContain("TRY", g.GirisCikisNotu);
        Assert.Contains("₺", g.GirisCikisGrafik);
        Assert.DoesNotContain("TRY", Sozlukler.En.Kurulum.TlTutar);
        Assert.DoesNotContain("TRY", Sozlukler.En.Cari.TlKarsiligiBilgiAmacli);
    }

    [Fact]
    public void Web_kaynaginda_ham_ayar_adi_yok_secici_ve_etiketler_KurKaynakAdi_AyarAdi_kullanir()
    {
        var web = Path.Combine(Kok(), "Kuyumcu.Web");
        var ham = new List<string>();
        foreach (var f in Directory.EnumerateFiles(web, "*.*", SearchOption.AllDirectories)
                     .Where(f => (f.EndsWith(".razor") || f.EndsWith(".cs")) && !f.Contains("/obj/") && !f.Contains("/bin/")))
        {
            var t = Regex.Replace(File.ReadAllText(f), @"@\*.*?\*@", "", RegexOptions.Singleline);
            foreach (var satir in t.Split('\n').Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)))
                foreach (Match m in Regex.Matches(satir, @"Ayarlar\.Bul\([^)]*\)!?\??\.Ad\b"))
                    ham.Add($"{Path.GetRelativePath(web, f)}: {m.Value}");
        }
        Assert.True(ham.Count == 0, "ham ayar adı (EN modda '22 Ayar' kalır): " + string.Join(" | ", ham));
        // Fiş ürün etiketi: EN'de görünen ad "22k"; Türkçe ürün adı (veri) çekirdek adı söylüyorsa tekrar edilmez
        Assert.Equal("22 Ayar Burma Bilezik", Dilde("en", () => Kuyumcu.Web.Bilesenler.Fis.FisHesap.UrunEtiketi("22 Ayar Burma Bilezik", "K22")));
        Assert.Equal("Burma Bilezik · 22k", Dilde("en", () => Kuyumcu.Web.Bilesenler.Fis.FisHesap.UrunEtiketi("Burma Bilezik", "K22")));
        Assert.Equal("Burma Bilezik · 22 Ayar", Dilde("tr", () => Kuyumcu.Web.Bilesenler.Fis.FisHesap.UrunEtiketi("Burma Bilezik", "K22")));
        Assert.Equal("22k", Dilde("en", () => Kuyumcu.Web.Bilesenler.Fis.FisHesap.AyarSecenekleri(["K22"])[0].Ad));
    }

    [Fact]
    public void Denetim_izi_ekrani_detay_metnini_BASIT_ve_PRO_modda_EN_cumle_cevirisinden_gecirir()
    {
        // Gerçek Chrome ölçümü: PRO + EN denetim izinde "liste: 25 kayıt · transaction ledger" (yarı Türkçe) görünüyordu.
        var razor = Oku("Kuyumcu.Web/Pages/DenetimIzi.razor");
        Assert.Matches(new Regex(@"private string\? DetayMetni\(string\? ham\) => DetayMetniCevir\(Q105Sozlugu\.DenetimCumleleriEn\(ham, Dil\.Dil\)\);"), razor);
        // Çeviri iki modda da aynıdır (mod parametresi yok): TR'de dokunulmaz.
        Assert.Equal("list: 7 records · customer list", Q105Sozlugu.DenetimCumleleriEn("liste: 7 kayıt · cari listesi", "en") is { } e ? Q105Sozlugu.DenetimDetayi(e, "en", true) : null);
        Assert.Equal("liste: 7 kayıt · cari listesi", Q105Sozlugu.DenetimCumleleriEn("liste: 7 kayıt · cari listesi", "tr"));
    }
}
