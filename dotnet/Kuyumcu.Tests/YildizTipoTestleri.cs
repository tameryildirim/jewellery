using System.Globalization;
using System.Text.RegularExpressions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-TIPO — Yıldız TAM TİPOGRAFİ/ÖĞE DENETİMİ
// Kurucu (09.10): "yazı tipleri özellikle dropdownlarda eski temalardan kalma boyutları bile
// farklı geliyor tam bir kontrol sağlansın" · "birebir tasarım neyse aynısını".
//
// ASIL kanıt başsız Chrome taramasıdır (betikler/tur/yildiz-tipo-tara.mjs: 42 ekran × BASİT/PRO ×
// koyu/açık × 1440/375, her açılır katman; rapor). Bu sınıf o taramanın dayandığı SÖZLEŞMEYİ kilitler:
//   • tuvalden çıkarılan izinli ölçek (boy/ağırlık/harf aralığı) = tarama aracının kümesi = CSS'in kümesi;
//   • tema-yildiz.css "/* YILDIZ-TIPO */" bloğu: gövde harf aralığı normal, büyük sayı −0,035em,
//     600/700 → 500 (h1 300), akışkan boyların tuval basamağına inmesi (sign()), yer tutucu rengi,
//     açılır liste (Seçici paneli) ve giriş kutularının tek yazı kalıbı;
//   • blok yalnız Yıldız kapsamlı, sabit renk yok, kâğıda dokunmaz.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class YildizTipoTestleri
{
    private static readonly int[] TuvalBoylari = { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 22, 28, 36, 40, 52, 56, 64, 72, 104, 112 };
    private static readonly string[] TuvalAgirliklari = { "200", "300", "400", "500" };

    private static string Css() => YildizTablosu.Css();

    /// <summary>"/* YILDIZ-TIPO */" … "/* YILDIZ-TIPO sonu */" bloğu.</summary>
    private static string Blok()
    {
        var c = Css();
        var i = c.IndexOf("/* YILDIZ-TIPO */", StringComparison.Ordinal);
        var j = c.IndexOf("/* YILDIZ-TIPO sonu */", StringComparison.Ordinal);
        Assert.True(i >= 0 && j > i, "tema-yildiz.css'te YILDIZ-TIPO bloğu yok");
        return c[i..j];
    }

    private static List<CssKaynak.Bildirim> Bil() => CssKaynak.Bildirimler(Blok());

    private static string Deger(string seciciSonu, string ozellik)
    {
        var d = Bil().Where(x => x.Ozellik == ozellik && x.Secici.TrimEnd().EndsWith(seciciSonu, StringComparison.Ordinal)).ToList();
        Assert.True(d.Count == 1, $"{seciciSonu} {{ {ozellik} }} bulunamadı ya da birden çok ({d.Count})");
        return d[0].Deger.Trim();
    }

    private static string KaynakDizini() => CssKaynak.Yol("..", "dokumanlar", "09-uygulama-kayitlari", "ham", "yildiz-tuval", "kaynak");

    // ── YT-01) Tuvalin ölçeği = aracın kümesi = CSS basamakları ─────────────
    [Fact]
    public void YT_01_tuvalden_cikan_olcek_aracin_ve_cssin_kumesiyle_ayni()
    {
        var boy = new SortedSet<int>(); var agirlik = new SortedSet<string>();
        foreach (var f in Directory.GetFiles(KaynakDizini(), "*.dc.html"))
        {
            var t = File.ReadAllText(f);
            foreach (Match m in Regex.Matches(t, @"font-size\s*:\s*(\d+)px")) boy.Add(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture));
            foreach (Match m in Regex.Matches(t, @"font-weight\s*:\s*(\d+)")) agirlik.Add(m.Groups[1].Value);
        }
        Assert.Equal(TuvalBoylari, boy.ToArray());
        Assert.Equal(TuvalAgirliklari, agirlik.ToArray());

        var arac = File.ReadAllText(CssKaynak.Yol("..", "dokumanlar", "09-uygulama-kayitlari", "betikler", "tur", "yildiz-tipo-tara.mjs"));
        Assert.Contains("const BOYLAR = [" + string.Join(",", TuvalBoylari) + "];", arac);
        Assert.Contains("const AGIRLIK = ['200','300','400','500'];", arac);
        Assert.Contains("const ARALIK = [0,-0.005,-0.01,-0.035,0.02,0.04,0.06,0.08,0.1,0.14,0.18,0.22,0.3];", arac);
        Assert.Contains("'--metin','--metin-soluk','--kenarlik','--ayrac','--yuzey-govde','--basari','--tehlike'", arac);

        // CSS basamak işlevinin basamakları da aynı küme (11 + artışlar).
        Assert.Equal(TuvalBoylari, Basamaklar().ToArray());
    }

    // ── YT-02) Harf aralığı: gövde normal (YILDIZ-1'in −0,011em'ini SONRA ezer), büyük sayı −0,035em
    [Fact]
    public void YT_02_govde_harf_araligi_normal_buyuk_sayi_tuval_ince()
    {
        Assert.Equal("normal", Deger("html:root[data-sema=\"yildiz\"] body", "letter-spacing"));
        var c = Css();
        Assert.True(c.IndexOf("/* YILDIZ-TIPO */", StringComparison.Ordinal) > c.IndexOf("letter-spacing: -0.011em;", StringComparison.Ordinal),
            "TIPO bloğu YILDIZ-1 gövde kuralından SONRA gelmeli (aynı seçici, sonra gelen kazanır)");
        Assert.Equal("normal", Deger(".tuval-karo-izgara > .ekran-karo", "letter-spacing"));
        Assert.Equal("-0.035em", Deger(".panel-soru-sayi, .karsilama-selam)", "letter-spacing"));
        Assert.Equal("-0.035em", Deger("#kasa-liste > .kart > .kart-govde > .stat-deger", "letter-spacing"));
    }

    // ── YT-03) Ağırlık: blokta yalnız tuval ağırlıkları; vurgu 500, h1 300 ──
    [Fact]
    public void YT_03_agirlik_yalniz_tuval_kumesi_vurgu_500_baslik_300()
    {
        foreach (var d in Bil().Where(x => x.Ozellik == "font-weight"))
            Assert.Contains(Regex.Match(d.Deger, @"\d+").Value, TuvalAgirliklari);
        Assert.Equal("300 !important", Deger("body h1:not(.yz *, [id^=\"yildiz-\"])", "font-weight"));
        Assert.Equal("500 !important", Deger(":is(h2, h3, h4, h5, h6, th, strong, b):not(.yz *, [id^=\"yildiz-\"]:not(#yildiz-kabuk), [id^=\"yildiz-\"]:not(#yildiz-kabuk) *)", "font-weight"));
        // Kabuk sarmalayıcısı (#yildiz-kabuk, AnaYerlesim) TÜM sayfayı sarar: dışlama "[id^=yildiz-] *" çıplak kalırsa
        // kural hiçbir ekrana değmez (YILDIZ-TIPO tur 2 canlı taramada 600/700 başlıklar böyle kaldı).
        foreach (var d in Bil().Where(x => x.Ozellik == "font-weight" && x.Secici.Contains(":not(")))
            Assert.DoesNotMatch(@"\[id\^=""yildiz-""\](?!:not\(#yildiz-kabuk\))\s*\*", d.Secici);
        Assert.Equal("400 !important", Deger("body .secici-ad", "font-weight"));
    }

    // ── YT-04) Boy: blokta yazılan her px boyu tuval kümesinde; akışkan boy basamağa iner ─────
    [Fact]
    public void YT_04_boylar_tuval_kumesinde_akiskan_boy_basamaga_iner()
    {
        foreach (var d in Bil().Where(x => x.Ozellik == "font-size" && !x.Deger.Contains("sign(", StringComparison.Ordinal)))
            Assert.Contains(int.Parse(Regex.Match(d.Deger, @"^(\d+)px").Groups[1].Value, CultureInfo.InvariantCulture), TuvalBoylari);
        Assert.Equal("28px", Deger("html:root[data-sema=\"yildiz\"] h2", "font-size"));
        Assert.Equal("20px", Deger("html:root[data-sema=\"yildiz\"] h3", "font-size"));
        Assert.Equal("11px", Deger("svg text[font-size=\"9\"]", "font-size"));

        // Taramada ölçülen tuval dışı akışkan değerler → beklenen tuval basamağı.
        foreach (var (girdi, beklenen) in new[] { (24.0, 22), (22.6125, 22), (30.87, 28), (38.4, 36), (45.0, 40), (96.0, 72), (104.0, 104), (41.503, 40), (9.0, 11), (64.0, 64), (112.5, 112) })
            Assert.Equal(beklenen, Basamakla(girdi));
        // Bağlantılar: her akışkan öğe --tipo-akis taşır.
        foreach (var s in new[] { ".stat-kart > .kart-govde > .stat-deger", "#sayfa-basi-kasalar + .stat-izgara .stat-deger", "#kur-has-onde > #kur-has-deger", ".yz .yz-h1" })
            Assert.Contains(Bil(), d => d.Ozellik == "--tipo-akis" && d.Secici.TrimEnd().EndsWith(s, StringComparison.Ordinal));
        Assert.Equal("min(104px, 9cqi)", Deger("#sayfa-basi-kasalar + .stat-izgara .stat-deger", "--tipo-akis"));
    }

    /// <summary>CSS basamak işlevinin (calc(11px + Npx * (sign(var(--tipo-akis) - Tpx) + 1) / 2 …)) C# karşılığı.</summary>
    private static (double Taban, List<(double Artis, double Esik)> Terimler) Islev()
    {
        var d = Bil().Single(x => x.Ozellik == "font-size" && x.Deger.Contains("sign(", StringComparison.Ordinal));
        var taban = double.Parse(Regex.Match(d.Deger, @"^calc\((\d+)px").Groups[1].Value, CultureInfo.InvariantCulture);
        var t = Regex.Matches(d.Deger, @"(\d+)px \* \(sign\(var\(--tipo-akis\) - ([\d.]+)px\) \+ 1\) / 2")
            .Select(m => (double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture))).ToList();
        Assert.True(t.Count == TuvalBoylari.Length - 1, $"basamak sayısı {t.Count}");
        Assert.EndsWith("!important", d.Deger);
        return (taban, t);
    }

    private static int Basamakla(double v)
    {
        var (taban, t) = Islev();
        return (int)Math.Round(taban + t.Sum(x => x.Artis * (Math.Sign(v - x.Esik) + 1) / 2.0));
    }

    private static IEnumerable<int> Basamaklar()
    {
        var (taban, t) = Islev();
        var s = taban; yield return (int)s;
        foreach (var (artis, esik) in t)
        {
            s += artis;
            Assert.True(Math.Abs(esik - (s - 0.01)) < 1e-9, $"eşik {esik} basamağın ({s}) 0,01 altında değil");
            yield return (int)s;
        }
    }

    // ── YT-05) Yer tutucu tuval rengi ───────────────────────────────────────
    [Fact]
    public void YT_05_yer_tutucu_tuval_rengi_tarayici_grisi_yok()
    {
        Assert.Equal("var(--metin-soluk)", Deger("body ::placeholder", "color"));
        Assert.Equal("1", Deger("body ::placeholder", "opacity"));
    }

    // ── YT-06) Açılır liste (Seçici paneli) tek kalıp: tuval .sat ──────────
    [Fact]
    public void YT_06_acilir_liste_tuval_satir_kalibi()
    {
        Assert.Equal("19px", Deger("body .secici-secenek", "font-size"));
        Assert.Equal("400", Deger("body .secici-secenek", "font-weight"));
        Assert.Equal("var(--metin)", Deger("body .secici-secenek", "color"));
        Assert.Equal("22px", Deger("body .secici-ara", "font-size"));   // tuval .alan input
        Assert.Equal("300", Deger("body .secici-ara", "font-weight"));
        const string ikincil = ".secici-isaret, .secici-bos, .secici-sinir, .secici-kilit-sebep)";
        Assert.Equal("14px", Deger(ikincil, "font-size"));
        Assert.Equal(".06em", Deger(ikincil, "letter-spacing"));
        Assert.Equal("var(--metin-soluk)", Deger(ikincil, "color"));
        Assert.Equal("none", Deger("body .secici-liste-kap", "box-shadow"));
        Assert.Equal("underline", Deger(".secici-secenek.etkin .secici-ad", "text-decoration"));   // klavye etkin satırı görünür
        // Tezgâh'ın kendi (.yz-duzen) Seçici kuralları dokunulmadan durur.
        Assert.Contains("html:root[data-sema=\"yildiz\"] .yz .yz-duzen .secici-secenek {", Css());
    }

    // ── YT-07) Giriş kutuları, native select ve tetik aynı yazıyı taşır ─────
    [Fact]
    public void YT_07_giris_kutusu_native_select_ve_tetik_tek_yazi()
    {
        const string s = ":is(.secici-tetik, .secici-tetik .secici-metin, input.girdi, textarea.girdi, select, .secim):not(.yz *)";
        Assert.Equal("inherit", Deger(s, "font-family"));
        Assert.Equal("22px", Deger(s, "font-size"));        // tuval .k .v / .alan input = 22 px (19 px tuvalde girdi değil, liste satırı)
        Assert.Equal("300", Deger(s, "font-weight"));
        Assert.Equal("-0.01em", Deger(s, "letter-spacing"));
        Assert.Equal("normal", Deger(":is(input.girdi-arama, input[type=\"search\"], .arama-kutu > input, .secici-ara):not(.yz *):not(#_)", "letter-spacing"));
        // Form etiketi = tuval .k label (Tezgâh .yz-duzen ile aynı): 12 px · 400 · .18em · büyük harf · soluk; kutucuk etiketi hariç.
        const string e = ":is(.alan-etiket, .alan:not(.alan-onay) > label, .secici-alan > label):not(.yz *, .onay-etiket):not(#_)";
        Assert.Equal("12px", Deger(e, "font-size"));
        Assert.Equal("400 !important", Deger(e, "font-weight"));
        Assert.Equal(".18em", Deger(e, "letter-spacing"));
        Assert.Equal("uppercase", Deger(e, "text-transform"));
        Assert.Equal("var(--metin-soluk)", Deger(e, "color"));
        Assert.Equal("var(--metin)", Deger(s, "color"));
        Assert.Equal("19px", Deger(":is(select, .secim) option", "font-size"));
        Assert.Equal("var(--yuzey-govde)", Deger(":is(select, .secim) option", "background"));
        // Inter, Yıldız'da yazı ailesi belirtecidir (inherit → Inter).
        Assert.StartsWith("\"Inter\"", YildizTablosu.Yukle().Aydinlik["--yazi-ailesi"]);
    }

    // ── YT-08) İzolasyon: her seçici Yıldız kapsamlı, sabit renk yok, kâğıt yok ───
    [Fact]
    public void YT_08_blok_yalniz_yildiz_kapsamli_sabit_renksiz_kagitsiz()
    {
        var b = Bil();
        Assert.True(b.Count > 40);
        foreach (var d in b)
        {
            var derin = 0; var sb = new System.Text.StringBuilder(); var parcalar = new List<string>();
            foreach (var ch in d.Secici)
            {
                if (ch is '(' or '[') derin++; else if (ch is ')' or ']') derin--;
                if (ch == ',' && derin == 0) { parcalar.Add(sb.ToString()); sb.Clear(); } else sb.Append(ch);
            }
            parcalar.Add(sb.ToString());
            foreach (var p in parcalar)
                Assert.StartsWith("html:root[data-sema=\"yildiz\"]", p.Trim());
            Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgba?\(|hsla?\(", d.Deger);
        }
        var y = CssKaynak.YorumsuzCss(Blok());
        foreach (var yasak in new[] { ".belge", ".urun-etiket", "@media print", "@container", "#yildiz-marka" })
            Assert.DoesNotContain(yasak, y);
    }

    // ── YT-09) Tur 2 canlı tarama kalıntıları: seçili sekme, sınıfsız düğme, öneri satırı, panel sayısı ──
    [Fact]
    public void YT_09_secili_sekme_sinifsiz_dugme_oneri_satiri_panel_sayisi()
    {
        // Seçili sekme: eski 600 (olcek.css/tuval-satis.css) → 500.
        Assert.Equal("500 !important", Deger("body :is(.sekme, .sekmeler-sekme).secili", "font-weight"));
        // Sınıfsız düğme: tarayıcı rengi (koyu buttontext #FFF) yerine metin belirteci + b3 hap; Yıldız bileşenleri hariç.
        const string d = "body button:not([class]):not(.yz *, .yildiz-ust *)";
        Assert.Equal("var(--metin)", Deger(d, "color"));
        Assert.Equal("transparent", Deger(d, "background"));
        Assert.Equal("1px solid var(--ayrac)", Deger(d, "border"));
        // Tezgâh barkod/ad öneri satırı açılır liste birincil satırıdır: 400 (600 kalmaz).
        Assert.Equal("400 !important", Deger("body .fis-arama-ad", "font-weight"));
        // Uyarı cümlesi sonundaki kısa "Ayrıntı →" bağlantısı bölünmez (tuval aralığıyla 375'te K3/K9 çıkıyordu).
        Assert.Equal("nowrap", Deger("body .uyari-metni > a[id$=\"-git\"]", "white-space"));
        // Panel soru sayısı akışkan boyu tuval basamağına iner (375'te 34,21 px kalmasın).
        Assert.Contains(Bil(), x => x.Ozellik == "--tipo-akis" && x.Secici.TrimEnd().EndsWith("body .panel-soru > .panel-soru-sayi", StringComparison.Ordinal));
        Assert.Contains(Bil(), x => x.Ozellik == "font-size" && x.Secici.Contains(".panel-soru > .panel-soru-sayi)") && x.Deger.Contains("sign(var(--tipo-akis)"));
    }

    // ── Tur 3 (doğrulayıcı KRİTİK, 10.10): koyu Yıldız'da seçili sekme eski temanın DOLU bloğu ─────────────
    // dugme.css `html[data-mod] .ana-icerik .sekmeler-serit > .sekmeler-sekme.secili { background: var(--secili-dolgu) }`
    // Yıldız'ın "background: transparent" kuralıyla AYNI özgüllükte (0,5,1) ve sonra yüklendiği için kazanıyordu
    // (/siparisler /bildirimler /pos/tahsilatlar /evraklar /kur /gider /gider-pusulalari /uyum /raporlar).
    // Tuval: sekme = altı çizili yazı (Musteri4 'HEPSİ'), dolgu yok.
    private const string SeciliSekme = "html:root[data-sema=\"yildiz\"] body .ana-icerik :is(.sekmeler-serit > .sekmeler-sekme, .tuval-pro-sekmeler > .sekme).secili:not(#_)";
    private const string DilSecici = "html:root[data-sema=\"yildiz\"] body .yildiz-ust .dil-secici";

    [Fact]
    public void YT_10_secili_sekme_dolgusuz_alti_cizili_ve_eski_dolgu_kuralini_yener()
    {
        Assert.Equal("transparent", Deger(SeciliSekme, "background"));
        Assert.Equal("var(--metin)", Deger(SeciliSekme, "color"));
        Assert.Equal("transparent", Deger(SeciliSekme, "border-color"));
        Assert.Equal("var(--metin)", Deger(SeciliSekme, "border-bottom-color"));

        // Özgüllük: seçili sekmeye zemin/renk/kenar yazan HER eski kural (dugme.css + tema-yildiz.css'in blok dışı)
        // Yıldız kuralından KESİN düşük olmalı — eşitlikte dosya sırası belirler ve dugme.css sonra yüklenir.
        var yildiz = Ozgulluk(SeciliSekme);
        var rakipler = new[] { "dugme.css", "tuval-satis.css", "olcek.css", "app.css" }
            .SelectMany(f => CssKaynak.Bildirimler(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", f)))
            .Where(x => x.Ozellik is "background" or "background-color" or "color" or "border-color")
            .SelectMany(x => UstVirgul(x.Secici))
            .Where(x => Regex.IsMatch(x, @"(\.sekmeler-sekme|\.tuval-pro-sekmeler\s*>\s*\.sekme)\.secili\s*$"))
            .Distinct().ToList();
        // rakip kural yoksa test boşa koşmasın: iki kök neden de listede olmalı
        Assert.Contains(rakipler, r => r.Contains(".sekmeler-serit > .sekmeler-sekme.secili"));
        Assert.Contains(rakipler, r => r.Contains(".islem-sayfa.tuval-pro .tuval-pro-sekmeler > .sekme.secili"));
        foreach (var r in rakipler)
            Assert.True(yildiz.CompareTo(Ozgulluk(r)) > 0, $"Yıldız seçili sekme {yildiz} ≤ {Ozgulluk(r)} : {r}");

        // Kullanıcı menüsü dil seçicisi: eski gri kapsül + dolu seçili düğme yerine Görünüm anahtarıyla aynı altı çizili yazı.
        Assert.Equal("transparent", Deger(DilSecici + ":not(#_)", "background"));
        Assert.Equal("transparent", Deger(DilSecici + " button:not(#_)", "background"));
        Assert.Equal("var(--metin-soluk)", Deger(DilSecici + " button:not(#_)", "color"));
        Assert.Equal("var(--metin)", Deger(DilSecici + " button.secili:not(#_)", "color"));
        Assert.Equal("var(--metin)", Deger(DilSecici + " button.secili:not(#_)", "border-bottom-color"));
        foreach (var r in new[] { "app.css" }.SelectMany(f => CssKaynak.Bildirimler(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", f)))
                     .Where(x => x.Ozellik is "background" or "color").SelectMany(x => UstVirgul(x.Secici)).Where(x => x.Contains(".dil-secici")).Distinct())
            Assert.True(Ozgulluk(DilSecici + " button.secili:not(#_)").CompareTo(Ozgulluk(r)) > 0, $"dil seçici ≤ {r}");

        // Tarama aracı ZEMİNİ de ölçer (renk #000 = --yuzey-govde kuralından geçen dolu blok yakalanır);
        // --metin dolgu yalnız b1 hapında, sekme/segment b1 sayılmaz.
        var arac = File.ReadAllText(CssKaynak.Yol("..", "dokumanlar", "09-uygulama-kayitlari", "betikler", "tur", "yildiz-tipo-tara.mjs"));
        Assert.Contains("hatalar.push(['zemin'", arac);
        Assert.Contains("tok[z.renk] !== '--yuzey-govde' && !(tok[z.renk] === '--metin' && z.sahip.matches(B1) && !z.sahip.matches(SEKME))", arac);
        Assert.Contains("const SEKME = '.sekme, .sekmeler-sekme, .musteri-segment-oge, [role=\"tab\"]'", arac);
    }

    [Fact]
    public void YT_11_metin_zemini_eski_gri_kutu_vurgulu_satir_secenek_tehlike_dolgusu_kalmaz()
    {
        const string y = "html:root[data-sema=\"yildiz\"] body ";
        // Bilgi/uyarı kutuları: dolgu yok (kenar korunur).
        Assert.Equal("transparent", Deger(":is(.degismez-ipucu, .sayfa-basi-bilgi > .sayfa-basi-aciklama, .uyari-kutu, .yasal-not, .emanet-bilgi-metin, .dosya-secici, .gorsel-galerisi):not(.yz *):not(#_)", "background"));
        // Örten/yapışkan yüzeyler: --yuzey-govde (saydam değil, içerik altından görünmesin).
        const string orten = ":is(.cekmece-alt, .menu-arama-satir, tr[id$=\"-baslik-satiri\"] > th, .veri-tablosu th):not(.yz *):not(#_)";
        Assert.Equal("var(--yuzey-govde)", Deger(orten, "background-color"));
        Assert.Equal("transparent", Deger("body tr.satir-vurgu:not(#_)", "background"));
        Assert.Equal("var(--yuzey-govde)", Deger("body tr.satir-vurgu > td:not(#_)", "background-color"));
        Assert.Equal("inset 2px 0 0 var(--metin)", Deger("body tr.satir-vurgu > td:first-child:not(#_)", "box-shadow"));
        Assert.Equal("transparent", Deger("body .secim-secenek.secili:not(#_)", "background"));
        Assert.Equal("var(--metin)", Deger("body .secim-secenek.secili:not(#_)", "color"));
        Assert.Equal("transparent", Deger("body .tuval-karo-izgara > .ekran-karo.ekran-karo-birincil:not(#_)", "background"));
        Assert.Equal("var(--metin)", Deger("body .tuval-karo-izgara > .ekran-karo.ekran-karo-birincil:not(#_)", "color"));
        Assert.Equal("transparent", Deger(":is(.btn-tehlike, .dugme-tehlike):not(.yz *):not(#_):hover", "background"));
        Assert.Equal("var(--tehlike)", Deger(":is(.btn-tehlike, .dugme-tehlike):not(.yz *):not(#_):hover", "color"));

        // Her eski kural (app.css/katlanir.css/emanet.css/yasal.css/dugme.css) Yıldız kuralından KESİN düşük özgüllükte.
        var bizim = new[]
        {
            (Sinif: ".degismez-ipucu", Secici: y + ":is(.degismez-ipucu, .sayfa-basi-bilgi > .sayfa-basi-aciklama, .uyari-kutu, .yasal-not, .emanet-bilgi-metin, .dosya-secici, .gorsel-galerisi):not(.yz *):not(#_)"),
            (Sinif: ".sayfa-basi-aciklama", Secici: y + ":is(.degismez-ipucu, .sayfa-basi-bilgi > .sayfa-basi-aciklama, .uyari-kutu, .yasal-not, .emanet-bilgi-metin, .dosya-secici, .gorsel-galerisi):not(.yz *):not(#_)"),
            (Sinif: ".uyari-kutu", Secici: y + ":is(.degismez-ipucu, .sayfa-basi-bilgi > .sayfa-basi-aciklama, .uyari-kutu, .yasal-not, .emanet-bilgi-metin, .dosya-secici, .gorsel-galerisi):not(.yz *):not(#_)"),
            (Sinif: ".emanet-bilgi-metin", Secici: y + ":is(.degismez-ipucu, .sayfa-basi-bilgi > .sayfa-basi-aciklama, .uyari-kutu, .yasal-not, .emanet-bilgi-metin, .dosya-secici, .gorsel-galerisi):not(.yz *):not(#_)"),
            (Sinif: ".dosya-secici", Secici: y + ":is(.degismez-ipucu, .sayfa-basi-bilgi > .sayfa-basi-aciklama, .uyari-kutu, .yasal-not, .emanet-bilgi-metin, .dosya-secici, .gorsel-galerisi):not(.yz *):not(#_)"),
            (Sinif: ".gorsel-galerisi", Secici: y + ":is(.degismez-ipucu, .sayfa-basi-bilgi > .sayfa-basi-aciklama, .uyari-kutu, .yasal-not, .emanet-bilgi-metin, .dosya-secici, .gorsel-galerisi):not(.yz *):not(#_)"),
            (Sinif: ".cekmece-alt", Secici: y + orten),
            (Sinif: "th", Secici: y + orten),
            (Sinif: "tr.satir-vurgu", Secici: y + "tr.satir-vurgu > td:not(#_)"),
            (Sinif: ".secim-secenek.secili", Secici: y + ".secim-secenek.secili:not(#_)"),
            (Sinif: ".ekran-karo-birincil", Secici: y + ".tuval-karo-izgara > .ekran-karo.ekran-karo-birincil:not(#_)"),
            (Sinif: ".btn-tehlike", Secici: y + ":is(.btn-tehlike, .dugme-tehlike):not(.yz *):not(#_):hover"),
        };
        var eski = new[] { "app.css", "katlanir.css", "emanet.css", "yasal.css", "dugme.css", "olcek.css", "tuval-satis.css" }
            .SelectMany(f => CssKaynak.Bildirimler(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", f)))
            .Where(x => x.Ozellik is "background" or "background-color")
            .SelectMany(x => UstVirgul(x.Secici)).Distinct().ToList();
        foreach (var (sinif, secici) in bizim)
        {
            var rakip = eski.Where(r => r.Contains(sinif, StringComparison.Ordinal) && !r.Contains("belge", StringComparison.Ordinal)).ToList();
            Assert.NotEmpty(rakip);
            foreach (var r in rakip)
                Assert.True(Ozgulluk(secici).CompareTo(Ozgulluk(r)) > 0, $"{sinif}: Yıldız {Ozgulluk(secici)} ≤ {Ozgulluk(r)} : {r}");
        }
    }

    /// <summary>Üst düzey virgülle ayırır (:is(...)/:not(...) içindeki virgüller bölünmez).</summary>
    private static IEnumerable<string> UstVirgul(string secici)
    {
        int d = 0, b = 0;
        for (var i = 0; i < secici.Length; i++)
        {
            var ch = secici[i];
            if (ch is '(' or '[') d++;
            else if (ch is ')' or ']') d--;
            else if (ch == ',' && d == 0) { yield return secici[b..i].Trim(); b = i + 1; }
        }
        yield return secici[b..].Trim();
    }

    /// <summary>CSS Seçici Düzey 4 özgüllüğü (a: kimlik, b: sınıf/öznitelik/sözde sınıf, c: tür/sözde öğe).
    /// :is/:not/:has en özgül argüman; :where 0.</summary>
    internal static (int A, int B, int C) Ozgulluk(string s)
    {
        int a = 0, b = 0, c = 0, i = 0;
        static string Parantez(string t, ref int j)
        {
            var bas = ++j; var d = 1;
            for (; j < t.Length && d > 0; j++) { if (t[j] == '(') d++; else if (t[j] == ')') d--; }
            return t[bas..(j - 1)];
        }
        while (i < s.Length)
        {
            var ch = s[i];
            if (ch == '#') { a++; i++; while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] is '-' or '_')) i++; }
            else if (ch == '.') { b++; i++; while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] is '-' or '_')) i++; }
            else if (ch == '[') { b++; var d = 0; for (; i < s.Length; i++) { if (s[i] == '[') d++; else if (s[i] == ']' && --d == 0) { i++; break; } } }
            else if (ch == ':')
            {
                var el = i + 1 < s.Length && s[i + 1] == ':'; i += el ? 2 : 1;
                var bas = i; while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '-')) i++;
                var ad = s[bas..i];
                if (el) { c++; if (i < s.Length && s[i] == '(') Parantez(s, ref i); continue; }
                if (i < s.Length && s[i] == '(')
                {
                    var ic = Parantez(s, ref i);
                    if (ad is "is" or "not" or "has" or "matches")
                    {
                        var m = UstVirgul(ic).Select(Ozgulluk).Max();
                        a += m.A; b += m.B; c += m.C;
                    }
                    else if (ad != "where") b++;
                }
                else b++;
            }
            else if (char.IsLetter(ch)) { c++; while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] is '-' or '_')) i++; }
            else i++;
        }
        return (a, b, c);
    }
}
