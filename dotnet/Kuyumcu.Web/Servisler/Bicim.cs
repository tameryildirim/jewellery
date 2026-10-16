using System.Globalization;

namespace Kuyumcu.Web.Servisler;

/// <summary>tlFormat / hasFormat / gramFormat karşılıkları.</summary>
public static class Bicim
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// PAKET DIL-EKRAN — etkin arayüz dili ("tr" | "en"): yalnız BİRİM adlarını belirler
    /// (has/fine, adet/pcs). Sayı biçimi (tr-TR) dile bağlı DEĞİL, sabit kuraldır.
    /// <see cref="Kuyumcu.Web.Servisler.DilServisi"/> tarayıcıda yükleme/değiştirmede yazar.
    /// ⚠ Statik durum: bUnit testleri paralel koşar, bu yüzden DilServisi bunu YALNIZ
    /// tarayıcıda (WASM tek kullanıcı) ya da <see cref="DilYayilimi"/> açıksa yazar; EN birim
    /// testleri DisableParallelization koleksiyonunda Dil'i kendisi kurar ve geri alır.
    /// </summary>
    public static string Dil { get; set; } = "tr";

    /// <summary>DilServisi'nin Dil'i <see cref="Bicim.Dil"/>'e yazıp yazmayacağı (varsayılan: yalnız tarayıcı).</summary>
    public static bool DilYayilimi { get; set; } = OperatingSystem.IsBrowser();

    /// <summary>
    /// PAKET T-K1 — EKRAN TL BİÇİMİ (tuval "Sade Yüz Taslağı", kurucu 24.09 17:20).
    /// <para>
    /// Simge SONDA, araya bölünmez boşluk (U+00A0 — "57.550" ile "₺" iki satıra
    /// kırılmasın): 57550m → "57.550 ₺". Kuruş SIFIR ise yazılmaz; sıfır değilse
    /// her zaman iki hane: 45000.5m → "45.000,50 ₺". Tutar kuruşa
    /// <see cref="MidpointRounding.AwayFromZero"/> ile yuvarlanır (eski "C2" ile
    /// aynı yuvarlama: 0,005 → 0,01).
    /// </para>
    /// <para>
    /// Eksi işareti U+2212 (−), tuvaldeki gibi: "−22.000 ₺". Gerekçe: tire (-)
    /// Türkçe ekran okuyucularda çoğu zaman "tire"/"kısa çizgi" okunur ya da
    /// atlanır, U+2212 "eksi" okunur; rakam genişliğinde olduğu için tablo
    /// hizası kaymaz. Kopyalama kaybı yoktur: "₺" soneki yüzünden bu metin
    /// zaten hesap tablosunda sayı olarak ayrıştırılmaz — makineye giden her
    /// sayı <see cref="Makine(decimal)"/> (tire, nokta) kullanır.
    /// Kuruşa yuvarlanınca sıfır olan eksi değer işaretsiz basılır ("0 ₺").
    /// </para>
    /// <para>
    /// ⚠ KÂĞIT BELGE (fiş, makbuz, ekstre, fatura, gider pusulası) BU METODU
    /// KULLANMAZ: resmî belge tutarı kuruş taşır → <see cref="TlKurus"/> /
    /// <see cref="BelgePara"/>. Girdi kutusu değeri → <see cref="Girdi"/>.
    /// </para>
    /// </summary>
    public static string Tl(decimal n)
    {
        var y = decimal.Round(n, 2, MidpointRounding.AwayFromZero);
        return TlYaz(y, y != decimal.Truncate(y) ? "N2" : "N0");
    }

    /// <summary>
    /// PAKET T-K1 — KURUŞ HER ZAMAN İKİ HANE: 45000m → "45.000,00 ₺",
    /// 41.95m → "41,95 ₺". Kâğıt belgeler (VUK: belge tutarı kuruş taşır) ve üst
    /// bar kur şeridindeki döviz kurları (USD/EUR) bunu kullanır. İşaret ve
    /// boşluk kuralı <see cref="Tl"/> ile aynıdır.
    /// </summary>
    public static string TlKurus(decimal n) =>
        TlYaz(decimal.Round(n, 2, MidpointRounding.AwayFromZero), "N2");

    /// <summary>
    /// PAKET T-K1 — TAM LİRA: üst bar kur şeridinde HAS ve GRAM 22 fiyatı
    /// liraya yuvarlanır (6876.60m → "6.877 ₺"). Tam değer ipucunda (title)
    /// <see cref="TlKurus"/> ile ayrıca gösterilir; hesapta KULLANILMAZ.
    /// </summary>
    public static string TlTam(decimal n) =>
        TlYaz(decimal.Round(n, 0, MidpointRounding.AwayFromZero), "N0");

    /// <summary>
    /// PAKET T-K1 — KÂĞIT BELGE PARASI (A4Belge / Cikti): TRY → <see cref="TlKurus"/>
    /// ("45.000,00 ₺"), döviz → <see cref="Para"/> (zaten iki hane: "1.250,00 $").
    /// Ekranın sıfır kuruşu atan <see cref="Tl"/> biçimi kâğıda DÜŞMEZ.
    /// </summary>
    public static string BelgePara(decimal n, string? paraBirimi) => paraBirimi switch
    {
        null or "" or "TRY" => TlKurus(n),
        _ => Para(n, paraBirimi),
    };

    private static string TlYaz(decimal yuvarli, string kalip)
    {
        var govde = Math.Abs(yuvarli).ToString(kalip, Tr);
        return yuvarli < 0 ? $"\u2212{govde}\u00a0\u20ba" : $"{govde}\u00a0\u20ba";
    }
    /// <summary>
    /// PAKET P-57 — HAS/GRAM tr-TR (QA tur 8: aynı ekranda "₺1.234,56" yanında
    /// "12.5000 has" basılıyordu — üç ayrı sayı dili). Ondalık ayırıcı artık
    /// VİRGÜL: 77.6014m → "77,6014 has", 56.991m → "56,991 g". (TARİHÇE: P-57'de has
    /// da gram gibi binliksiz "F" biçimiydi; MADEN-AYRIMI tur 2 has'ı N4 binlik grupluya
    /// çevirdi — aşağıya bkz. <see cref="Gram"/> hâlâ binliksiz F3'tür.)
    /// ⚠ PAKET MADEN-AYRIMI tur 2 (tasarım hakemi KRİTİK + ekip kararı 5 İLKE "has her yerde
    ///   binlik gruplu, 4 ondalık"): aynı ekranda "1447,3856 has" ile "9.237.000 ₺" ve kâr uyarısının
    ///   N4 gramı yan yana iki ayrı sayı dili kuruyordu. Has artık tr-TR N4: 1447.3856m →
    ///   "1.447,3856 has". Dört ondalık hane SABİT olduğundan nokta/virgül karışmaz (virgülden
    ///   sonra hep 4 hane). 1.000 altı değerler değişmez ("3,2060 has").
    /// ⚠ API'ye/SVG'ye/input değerine giden sayı için <see cref="Makine(decimal)"/>.
    /// </summary>
    public static string Has(decimal n) => $"{n.ToString("N4", Tr)} {Kuyumcu.Core.I18n.DilEkranSozlugu.HasBirimi(Dil)}";
    /// <summary>PAKET RAPOR-GORUNUM: işaretli has — eksi "−" (tipografik), değer <see cref="Has(decimal)"/> ile AYNI
    /// sayı dili. Panel ve Rapor merkezi "Bugün ne sattım" kartlarının TEK biçim kaynağı.</summary>
    public static string HasIsaretli(decimal n) => HasIsaretli(n, Kuyumcu.Core.I18n.DilEkranSozlugu.HasBirimi(Dil));
    /// <summary>RAPOR-GORUNUM tur 2 (hasım KRİTİK): ÖNCE 4 haneye yuvarlanır, işaret YUVARLANMIŞ değerden seçilir —
    /// −0,00004 "−0,0000 has" değil "0,0000 has" yazar. <paramref name="birim"/> sözlükten (TR "has", EN "fine").</summary>
    public static string HasIsaretli(decimal n, string birim)
    {
        var y = Math.Round(n, 4, MidpointRounding.AwayFromZero);
        return $"{(y < 0m ? "−" : "")}{Math.Abs(y).ToString("N4", Tr)} {birim}";
    }
    /// <summary>PAKET RAPOR-GORUNUM: rapor oranı (devir hızı vb.) — 2 hane, tr-TR ("0,29").</summary>
    public static string Oran(decimal n) => n.ToString("N2", Tr);
    /// <summary>PAKET RAPOR-GORUNUM: gün sayısı — tam sayı, binlik ("126", "1.204").</summary>
    public static string GunSayisi(decimal n) => Math.Round(n, 0, MidpointRounding.AwayFromZero).ToString("N0", Tr);
    /// <summary>PAKET RAPOR-GORUNUM: <see cref="MadenMiktari"/> ile aynı kural, ama altın dışı madende tartı gramı
    /// biliniyorsa HER satır "X g (saf Y g)" kalıbındadır (0 stoklu satır da) — aynı tabloda iki biçim olmaz.</summary>
    public static string MadenMiktariTam(string? maden, decimal safGram, decimal? fizikselGram,
                                         string fizikselSafKalibi, string safKalibi) =>
        !string.IsNullOrEmpty(maden) && maden != Kuyumcu.Core.Domain.Maden.Altin && fizikselGram is { } f
            ? string.Format(fizikselSafKalibi, f.ToString("N2", Tr), safGram.ToString("N2", Tr))
            : MadenMiktari(maden, safGram, fizikselGram, fizikselSafKalibi, safKalibi);
    /// <inheritdoc cref="Has(decimal)"/>
    public static string Gram(decimal n) => $"{n.ToString("F3", Tr)} g";

    /// <summary>
    /// PAKET GOC-C (devir URUN-ACILIS-ATOMIK "gram binlik gruplanmıyor"): LİSTE/ızgara hücresinde gram
    /// binlik gruplu ("1.234,500 g") — <see cref="Has(decimal)"/> ile aynı sayı dili. <see cref="Gram"/> (grupsuz,
    /// BicimTestleri sözleşmesi) belge/fiş satırlarında AYNEN kalır; en küçük geriye uyumlu ekleme.
    /// </summary>
    public static string GramGruplu(decimal n) => $"{n.ToString("N3", Tr)} g";

    /// <summary>
    /// PAKET MADEN-AYRIMI — RAPOR EKRANLARINDA MADEN MİKTARI (tek kural, ekip kararı 5).
    /// "Has" YALNIZ ALTIN kavramıdır: ALTIN (ya da maden bilinmiyor) → <see cref="Has"/> ("3,2060 has").
    /// Diğer madenler kendi biriminde, has'a KARIŞMADAN: tartı gramı biliniyorsa
    /// "150,00 g (saf 138,75 g)", bilinmiyorsa "138,75 g saf" (metin kalıbı sözlükten — TR+EN).
    /// Gram iki hane, binlik gruplu (tur 2: <see cref="Has"/> ile aynı sayı dili — "1.500,00 g").
    /// </summary>
    public static string MadenMiktari(string? maden, decimal safGram, decimal? fizikselGram,
                                      string fizikselSafKalibi, string safKalibi)
    {
        if (string.IsNullOrEmpty(maden) || maden == Kuyumcu.Core.Domain.Maden.Altin) return Has(safGram);
        var saf = safGram.ToString("N2", Tr);
        return fizikselGram is { } f && Math.Round(f, 2, MidpointRounding.AwayFromZero) != Math.Round(safGram, 2, MidpointRounding.AwayFromZero)
            ? string.Format(fizikselSafKalibi, f.ToString("N2", Tr), saf)
            : string.Format(safKalibi, saf);
    }

    /// <summary>
    /// ORTAK-1 hakem turu 30.09 — KÂR-ZARAR HAS GRAMI (yalnız GÖSTERİM, tr-TR SABİT; tarayıcı
    /// kültüründen bağımsız). Profesyonel: 4 hane binlikli ("1.234,5678"). Sade (BASİT): 2 hane
    /// ("0,66") — ANCAK 2 haneye yuvarlanınca sıfıra düşen sıfırdan farklı değer 4 haneye kadar
    /// yazılır ("0,0001", "-0,004"): sıfırdan farklı has sıfır gibi GÖRÜNMEZ. 4 hane altı (sunucu
    /// has'ı 4 hane taşır) sıfır sayılır. Birim eki çağıranda ("g" / "gram").
    /// MADEN-AYRIMI tur 2: Has() de artık N4 — iki biçim aynı sayı dilinde (binlik gruplu).
    /// </summary>
    public static string KarHasGram(decimal n, bool sade)
    {
        if (!sade) return n.ToString("N4", Tr);
        if (Math.Round(n, 4, MidpointRounding.AwayFromZero) == 0m) return 0m.ToString("N2", Tr);
        return n.ToString(Math.Round(n, 2, MidpointRounding.AwayFromZero) == 0m ? "#,##0.00##" : "N2", Tr);
    }

    /// <summary>
    /// PAKET P-57 — YÜZDE (tr-TR). Girdi YÜZDE değeridir (oran değil):
    /// 12.5m → "%12,5", 25m → "%25". Türkçede yüzde işareti sayıdan ÖNCE yazılır;
    /// en çok iki ondalık, sondaki sıfırlar atılır.
    /// </summary>
    public static string Yuzde(decimal n) => $"%{n.ToString("0.##", Tr)}";
    /// <summary>
    /// UX-4d madde 17 — MİKTAR + BİRİM TEK BİÇİMDE.
    ///
    /// Fatura kalemleri "1.000 g ADET" basıyordu: gram biçimlendiricisi
    /// ("1.000 g") ile ham birim kodu ("ADET") yan yana yazılıyordu. Hem iki
    /// birim aynı anda görünüyor hem de ham veritabanı kodu ekrana düşüyordu
    /// (UX-S31). Artık tek yerden biçimlenir:
    ///   GRAM → "1,000 g"   ·   ADET → "1 adet"   ·   bilinmeyen → sayı + kod
    /// PAKET P-57: kesirli adet de tr-TR basılır (4.25m → "4,25 adet"); yorumdaki
    /// "1,000 g" hedefi ancak şimdi gerçek oldu (önceden "1.000 g" basıyordu).
    /// </summary>
    public static string Miktar(decimal n, string? birim) => birim switch
    {
        "GRAM" => Gram(n),
        "ADET" => $"{n.ToString("#,##0.###", Tr)} {Kuyumcu.Core.I18n.DilEkranSozlugu.AdetBirimi(Dil)}",
        null or "" => n.ToString("0.###", Tr),
        _ => $"{n.ToString("0.###", Tr)} {birim}",
    };

    /// <summary>
    /// UX-S32 — PARA BİRİMİ TUTARLA BİRLİKTE TAŞINIR (paket UX-1-C2).
    ///
    /// <see cref="Tl"/> her tutarı ₺ ile basar. QA turu 2'de EUR kasasının
    /// geçmişi "₺150,00", USD kasasının gün kapanışı "₺0,00" gösteriyordu:
    /// ekran, kasanın para birimini biliyorken tutarı "varsayılan TL" sayıyordu.
    /// Bu metot para birimini SORAR; TRY'de davranış <see cref="Tl"/> ile
    /// birebir aynıdır (mevcut ekranlar ve testleri etkilenmez).
    /// </summary>
    public static string Para(decimal n, string? paraBirimi) => paraBirimi switch
    {
        null or "" or "TRY" => Tl(n),
        "USD" => $"{n.ToString("N2", Tr)} $",
        "EUR" => $"{n.ToString("N2", Tr)} €",
        "GBP" => $"{n.ToString("N2", Tr)} £",
        _ => $"{n.ToString("N2", Tr)} {paraBirimi}",
    };

    /// <summary>
    /// PAKET KAT-1 (K5) — PARA BİRİMİNİN EKRANDAKİ İŞARETİ. Kurucu (24.09):
    /// "TRY yerine Türk lirası işaretini (₺) koyalım". Tek merkez: ekranda para
    /// KODU gösterilen her yer (alan son eki, kasa adı yanındaki parantez, tablo
    /// hücresi) bu metottan geçer.
    ///   "TRY" / null / "" → "₺"   ·   USD/EUR/GBP ve bilinmeyen kod → KOD AYNEN
    /// (kurucu yalnız TRY dedi; dövizlerin kodu okunur kalır).
    /// ⚠ YALNIZ GÖRÜNÜM: API gövdesi, sorgu dizesi ve karşılaştırmalar "TRY"
    /// kodunu kullanmaya devam eder (sunucu sözleşmesi değişmez).
    /// </summary>
    public static string ParaIsareti(string? kod) => kod switch
    {
        null or "" or "TRY" => "₺",
        _ => kod,
    };

    /// <summary>
    /// PAKET S-3a — MAKİNE BİÇİMİ (kurul tur 3, engel 19: "kültürsüz ToString").
    ///
    /// <para>
    /// İnsan için değil, MAKİNE için biçimlendirir: SVG <c>path</c>/koordinat,
    /// HTML <c>&lt;input type="number"&gt;</c> değeri, sorgu dizesi parametresi ve
    /// JSON gövdesi. Bu dört yerde ayırıcı DAİMA nokta olmalıdır; tr-TR kültürü
    /// devredeyken <c>12.5m.ToString()</c> "12,5" üretir ve SVG yolu çizilmez,
    /// number girdisi boş görünür, sunucu 400 döner.
    /// </para>
    /// <para>
    /// ⚠ Kullanıcıya gösterilecek sayı için BU METOT KULLANILMAZ —
    /// <see cref="Tl"/>, <see cref="Para"/>, <see cref="Gram"/> kullanılır.
    /// </para>
    /// </summary>
    public static string Makine(decimal n) => n.ToString("0.##########", CultureInfo.InvariantCulture);

    /// <summary>
    /// PAKET P-78 — KULLANICININ DÜZENLEYECEĞİ değer (girdi kutusu / satır içi
    /// düzenleme). tr-TR, GRUPLAMASIZ: 45000m → "45000", <c>Girdi(45000m, "0.00")</c>
    /// → "45000,00", 1.5m → "1,5". Nokta HİÇ basılmaz; böylece kullanıcı değeri
    /// aynen geri kaydettiğinde FormAlani.SayiCoz onu BİREBİR aynı sayı okur
    /// (gidiş-dönüş). Eski <c>ToString("0.00", Invariant)</c> "45000.00" basıyor,
    /// kullanıcı noktalı yazıma alışıp "45.000" yazıyordu (= 45 okunuyordu).
    /// ⚠ API gövdesi / sorgu dizesi / SVG için DEĞİL — onlar <see cref="Makine(decimal)"/>.
    /// </summary>
    public static string Girdi(decimal n, string kalip = "0.##########") => n.ToString(kalip, Tr);

    // ═══ PAKET BICIM-2 — GİRDİ KUTUSU ODAK DIŞINDA BİÇİMLİ ══════════════════
    // Kurucu 25.09 (Satış karma ödeme): solda tablo "113.363,16 ₺", sağda ödeme
    // kutularında "50000" / "63363,16". Kutu ODAK DIŞINDAYKEN tablo ile aynı
    // dili konuşur; ODAKTA ham düzenlenebilir metne (<see cref="Girdi"/>) döner
    // (CUMA-M TelefonBicimli kalıbı). ⚠ Bu metinler FormAlani.SayiCoz'a HİÇ
    // GİTMEZ — değer bileşende decimal olarak tutulur; "50.000" gösterimi
    // P-78 belirsizlik kuralına (kullanıcı "1.500" yazarsa hata) dokunmaz.
    // Tablo/rapor biçimleri (T-K1: Tl/Para/Gram/Has) DEĞİŞMEDİ.

    /// <summary>
    /// PARA kutusu, odak dışı: <see cref="Tl"/> ile AYNI sayı kuralı, simgesiz
    /// (simge kutunun sağındaki son ekte yazar). 50000m → "50.000",
    /// 63363.16m → "63.363,16", 1500.5m → "1.500,50". Kuruş sıfırsa yazılmaz,
    /// değilse en az iki hane; ikiden fazla hane varsa YUVARLANMAZ (kutu
    /// kullanıcının değerini saklamaz, gösterir).
    /// </summary>
    public static string ParaGirdi(decimal n) =>
        n.ToString(n == decimal.Truncate(n) ? "#,##0" : "#,##0.00########", Tr);

    /// <summary>
    /// GRAM kutusu, odak dışı: <see cref="Gram"/> ile AYNI kural (P-57: üç hane,
    /// binlik YOK, virgül ondalık), birimsiz: 12.5m → "12,500". Üç haneden
    /// fazlası yuvarlanmaz.
    /// </summary>
    public static string GramGirdi(decimal n) => n.ToString("0.000#######", Tr);

    /// <summary>
    /// HAS değeri, odak dışı (GİRİŞ KUTUSU): dört hane, binlik YOK — BİLEREK <see cref="Has"/>'tan
    /// farklı. MADEN-2: gösterim <see cref="Has"/> N4 binlik gruplu ("1.447,3856 has"); giriş kutusunda
    /// binlik noktası kullanıcının yazdığı değeri yeniden ayrıştırırken ondalık sanılabileceği için
    /// burada gruplama YAPILMAZ ("1447,3856").
    /// </summary>
    public static string HasGirdi(decimal n) => n.ToString("0.0000######", Tr);

    /// <summary>
    /// SAYI kutusu (adet, oran, gün, kur), odak dışı: binlik gruplu, ondalık
    /// kullanıcının yazdığı kadar: 12000m → "12.000", 41.95m → "41,95".
    /// </summary>
    public static string SayiGirdi(decimal n) => n.ToString("#,##0.##########", Tr);

    /// <inheritdoc cref="Makine(decimal)"/>
    public static string Makine(double n) => n.ToString("0.##########", CultureInfo.InvariantCulture);

    /// <inheritdoc cref="Makine(decimal)"/>
    public static string Makine(int n) => n.ToString(CultureInfo.InvariantCulture);

    /// <summary>Makine biçiminde tarih (ISO 8601 · <c>input type="date"</c>, sorgu dizesi).</summary>
    public static string Makine(DateOnly g) => g.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// PAKET P-25 / B-8 — makine biçiminde TARİH+SAAT kökü (yalnız GÜN).
    /// Sorgu dizesi ve JSON gövdesi için; <see cref="DateTime"/> taşıyan
    /// süzgeçler de <c>ToString("yyyy-MM-dd")</c> yazmak zorunda kalmasın diye
    /// buradadır (kültürsüz biçim sayacı: tek kaynak).
    /// </summary>
    public static string Makine(DateTime t) => t.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// PAKET P-36b — KÂĞIT/BELGE SAYISI (gider pusulası, tevsik belgesi).
    ///
    /// <para>
    /// Belgelerde sembol SONDA durur ("1.234,56 ₺"); PAKET T-K1'den beri ekran da
    /// (<see cref="Tl"/>) sonda basar, yalnız sıfır kuruşu atar. İkisi de BURADADIR:
    /// P-36b'den önce dört ayrı razor sayfası kendi <c>ToString("N2", Kultur)</c>
    /// satırını taşıyordu ve biri değişse ötekiler sapıyordu.
    /// </para>
    /// <para>
    /// ⚠ Basamak sayısı ÇAĞIRANIN kararıdır (gram 3, tutar 2) ve çıktı
    /// göç öncesiyle BİREBİR aynıdır — bu bir yeniden biçimlendirme değil,
    /// tek kaynağa taşımadır.
    /// </para>
    /// </summary>
    public static string BelgeSayisi(decimal n, string locale, int basamak) =>
        n.ToString("N" + basamak.ToString(CultureInfo.InvariantCulture),
            CultureInfo.GetCultureInfo(locale));

    /// <summary>Belge tutarı — sembol sonda (<see cref="BelgeSayisi"/> + " ₺").</summary>
    public static string BelgeParasi(decimal n, string locale) =>
        BelgeSayisi(n, locale, 2) + " \u20ba";

    /// <summary>
    /// PAKET T-K1 — ÜST BAR SAATİ "HH:mm" (tuval: "09:02"). Kültürden BAĞIMSIZ:
    /// özel biçimdeki ":" kültürün saat ayırıcısıdır (fi-FI'de "09.02" basar);
    /// tuval her dilde iki nokta ister. Çağıran yerel saate çevirmiş olmalıdır.
    /// </summary>
    public static string Saat(DateTime t) => t.ToString("HH':'mm", CultureInfo.InvariantCulture);

    /// <summary>
    /// BELGE-KALAN tur 2 (doğrulayıcı DÜŞÜK): ekrandaki GÜN işletme günüdür (Europe/Istanbul) —
    /// sunucunun gün sınırıyla (Zaman.UtcdenYerele) aynı kural. Tarayıcı başka saat dilimindeyse de
    /// liste, çekmece ve sunucu aynı günü söyler. Kind Unspecified = UTC (API "…T22:30:00" döndürür).
    /// Saat dilimi verisi yoksa (WASM'de InvariantTimezone) tarayıcı yereline düşer.
    /// </summary>
    public static DateTime IsletmeZamani(DateTime t)
    {
        var utc = t.Kind switch
        {
            DateTimeKind.Utc => t,
            DateTimeKind.Local => t.ToUniversalTime(),
            _ => DateTime.SpecifyKind(t, DateTimeKind.Utc),
        };
        return IsletmeDilimi is { } dilim ? TimeZoneInfo.ConvertTimeFromUtc(utc, dilim) : utc.ToLocalTime();
    }

    private static readonly TimeZoneInfo? IsletmeDilimi = IsletmeDilimiBul();
    private static TimeZoneInfo? IsletmeDilimiBul()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
        catch (TimeZoneNotFoundException) { return null; }
        catch (InvalidTimeZoneException) { return null; }
    }

    /// <summary>TOPLU-1 tur 2 (tasarım hakemi KRİTİK): TEK KALIP "dd.MM.yyyy". Eskiden kültürün kısa tarihi
    /// ("d") basılıyordu → tr-TR "1.10.2026", aynı ekranın komşu satırı "30.09.2026" (iki biçim). Dil
    /// parametresi imza uyumu için kalır; kalıp dilden bağımsızdır (tek kaynak).</summary>
    public static string Tarih(DateTime t, string locale) => TarihTam(t);

    /// <summary>TOPLU-1 tur 2: kültürün kısa kalıpları ("d", "g", "G") TEK KALIBA çevrilir — "d" tr-TR'de
    /// günü sıfırsız basar ("1.10.2026"). Diğer kalıplar (ör. "d MMMM", "dddd") olduğu gibi kalır.</summary>
    private static string TekKalip(string kalip) => kalip switch
    {
        "d" => "dd'.'MM'.'yyyy",
        "g" => "dd'.'MM'.'yyyy HH':'mm",
        "G" => "dd'.'MM'.'yyyy HH':'mm':'ss",
        _ => kalip,
    };

    // ═══ PAKET TOPLU-1 — TARIH-TEK (BELGE-KALAN 01.10, YÜKSEK) ═══════════════════════════
    // Kural: ekrandaki HER tarih/saat bu dosyadan geçer. Sunucunun anı (UTC; Kind Unspecified da
    // UTC sayılır) İŞLETME saatine (Europe/Istanbul) çevrilerek basılır — tarayıcının saat dilimi
    // ya da ham UTC (00:00–03:00 TSİ işlemleri bir gün geri) ekrana çıkmaz. Takvim günü taşıyan
    // alanlar (DateOnly ya da gün anlamlı DateTime: vade, kampanya başlangıcı) ÇEVRİLMEZ —
    // onlar an değil gündür. denetim.sh TARIH_TEK sayacı: Razor/sayfa kodunda ham tarih biçimi
    // (.ToString("dd.MM…"), {x:dd.MM}, ToString("g"), ToLocalTime) = 0; beyaz liste yok.

    /// <summary>Sunucu anı → işletme saatiyle "dd.MM.yyyy HH:mm" (tuval biçimi, kültürden bağımsız).</summary>
    public static string TarihSaat(DateTime an) =>
        IsletmeZamani(an).ToString("dd'.'MM'.'yyyy HH':'mm", CultureInfo.InvariantCulture);

    /// <summary>Sunucu anı → işletme saatiyle "dd.MM.yyyy HH:mm:ss" (denetim izi).</summary>
    public static string TarihSaatSaniye(DateTime an) =>
        IsletmeZamani(an).ToString("dd'.'MM'.'yyyy HH':'mm':'ss", CultureInfo.InvariantCulture);

    /// <summary>Sunucu anı → işletme GÜNÜ "dd.MM.yyyy" (UTC 22:30 = ertesi gün 01:30 TSİ → ertesi gün).</summary>
    public static string TarihTam(DateTime an) =>
        IsletmeZamani(an).ToString("dd'.'MM'.'yyyy", CultureInfo.InvariantCulture);

    /// <summary>Sunucu anı → işletme saatiyle "HH:mm".</summary>
    public static string IsletmeSaati(DateTime an) => Saat(IsletmeZamani(an));

    /// <summary>Sunucu anı → işletme saatiyle serbest kalıp (ör. "dd.MM", "g"). Kalıp çağırandan gelir,
    /// ÇEVİRİ burada yapılır — çağıran ham saati biçimleyemez.</summary>
    public static string IsletmeBicim(DateTime an, string kalip, IFormatProvider? kultur = null) =>
        IsletmeZamani(an).ToString(TekKalip(kalip), kultur ?? CultureInfo.InvariantCulture);

    /// <summary>İşletmenin BUGÜNÜ (Europe/Istanbul). Tarayıcının <c>DateTime.Today</c>'i yerine.</summary>
    public static DateTime IsletmeBugun => IsletmeZamani(DateTime.UtcNow).Date;

    /// <summary>Takvim günü (çevrilmez) "dd.MM.yyyy".</summary>
    public static string Gun(DateOnly g) => g.ToString("dd'.'MM'.'yyyy", CultureInfo.InvariantCulture);

    /// <summary>Gün anlamlı DateTime (vade, kampanya günü) — saat dilimi ÇEVRİLMEZ, "dd.MM.yyyy".</summary>
    public static string Gun(DateTime takvimGunu) => takvimGunu.ToString("dd'.'MM'.'yyyy", CultureInfo.InvariantCulture);

    /// <summary>Takvim günü (çevrilmez) serbest kalıp (ör. "dd.MM", "dddd, d MMMM yyyy").</summary>
    public static string GunBicim(DateTime takvimGunu, string kalip, IFormatProvider? kultur = null) =>
        takvimGunu.ToString(TekKalip(kalip), kultur ?? CultureInfo.InvariantCulture);

    /// <summary>Takvim günü (çevrilmez) serbest kalıp — DateOnly.</summary>
    public static string GunBicim(DateOnly g, string kalip, IFormatProvider? kultur = null) =>
        g.ToString(TekKalip(kalip), kultur ?? CultureInfo.InvariantCulture);

    /// <summary>Makine biçimi "yyyy-MM-dd" (sorgu/gövde) — boşsa null.</summary>
    public static string? Makine(DateTime? t) => t is { } d ? Makine(d) : null;

    /// <summary>Makine biçimi "yyyy-MM-dd" (sorgu/gövde) — DateOnly boşsa null.</summary>
    public static string? Makine(DateOnly? g) => g is { } d ? Makine(d) : null;

    /// <summary>Sunucu anının İŞLETME takvim günü (DateOnly) — gün karşılaştırması için.</summary>
    public static DateOnly IsletmeGunu(DateTime an) => DateOnly.FromDateTime(IsletmeZamani(an));

    /// <summary>
    /// PAKET P-76 — İNSAN OKUNUR SÜRE (QA tur 8: RPO kartı "7985 dk" basıyordu).
    ///
    /// <para>
    /// Dakikayı en büyük iki birimle yazar: 7985 → "5 gün 13 saat",
    /// 95 → "1 saat 35 dk", 360 → "6 saat", 42 → "42 dk". Sıfır olan alt
    /// birim yazılmaz; negatif değer 0 sayılır. Birim adları ÇAĞIRANDAN gelir
    /// (sözlük, TR+EN) — bu sınıf çıplak metin taşımaz.
    /// </para>
    /// <para>
    /// ⚠ Ham dakika kaybolmaz: ekran onu ipucunda (title) ayrıca gösterir.
    /// Bu yeni bir metottur; mevcut imzaların hiçbiri değişmedi.
    /// </para>
    /// </summary>
    public static string Sure(int dakika, string gunBirim, string saatBirim, string dakikaBirim)
    {
        var d = Math.Max(0, dakika);
        var gun = d / 1440;
        var saat = d % 1440 / 60;
        var dk = d % 60;
        static string P(int n, string birim) => $"{n.ToString(CultureInfo.InvariantCulture)} {birim}";
        if (gun > 0) return saat > 0 ? $"{P(gun, gunBirim)} {P(saat, saatBirim)}" : P(gun, gunBirim);
        if (saat > 0) return dk > 0 ? $"{P(saat, saatBirim)} {P(dk, dakikaBirim)}" : P(saat, saatBirim);
        return P(dk, dakikaBirim);
    }
}
