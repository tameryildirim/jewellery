using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET UX-4d madde 8 — DTO doğrulama mesajlarının TÜRKÇELEŞTİRİLMESİ.
//
// ⚠ NEDEN BURADA (tek çıkış noktası), her özniteliğe tek tek ErrorMessage
// yazmak yerine:
//   • ASP.NET Core sunucu tarafında DataAnnotations yerelleştirmesini YALNIZ
//     ErrorMessage'ı ELLE verilmiş özniteliklerde uygular
//     (DataAnnotationsModelValidator: ErrorMessage boşsa yerelleştirici hiç
//     çağrılmaz). Yani "AddDataAnnotationsLocalization" tek başına varsayılan
//     İngilizce mesajları çevirmez — denendi, çalışmıyor.
//   • 80'den fazla özniteliğe elle mesaj yazmak aynı cümlenin 80 kopyasını
//     doğurur ve yeni bir DTO eklendiğinde yine İngilizce mesaj sızar.
// Bu yüzden çeviri, doğrulama çıktısının TEK geçtiği yerde (InvalidModelState
// fabrikası) yapılır: bugünkü ve gelecekteki tüm DTO'ları kapsar.
//
// QA turu 1 kanıtı: "The Email field is not a valid e-mail address." ve
// "KrediLimitiTl must be between 0 and 1,79E+308" kullanıcıya böyle görünüyordu.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>DTO alan adlarının Türkçe karşılıkları ve İngilizce doğrulama
/// mesajlarının Türkçeye çevrilmesi.</summary>
public static partial class DogrulamaSozlugu
{
    /// <summary>
    /// Alan adı → Türkçe etiket. Burada olmayan alanlar
    /// <see cref="AlanAdi"/> tarafından PascalCase'ten türetilir.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AlanAdlari =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Ad"] = "Ad",
            ["Soyad"] = "Soyad",
            ["AdSoyad"] = "Ad soyad",
            ["Email"] = "E-posta",
            ["Telefon"] = "Telefon",
            ["VergiNo"] = "Vergi numarası",
            ["Adres"] = "Adres",
            ["Notlar"] = "Notlar",
            ["KrediLimitiTl"] = "Kredi limiti (TL)",
            ["VarsayilanVadeGun"] = "Varsayılan vade (gün)",
            ["VadeGun"] = "Vade (gün)",
            ["Tur"] = "Tür",
            ["Sira"] = "Sıra",
            ["Yon"] = "Yön",
            ["Tutar"] = "Tutar",
            ["TlTutari"] = "TL tutarı",
            ["HasGrami"] = "Has gram",
            ["GramAgirlik"] = "Gram ağırlık",
            ["TasAgirlik"] = "Taş ağırlık",
            ["GercekMilyem"] = "Gerçek milyem",
            ["AyarId"] = "Ayar",
            ["KasaId"] = "Kasa",
            ["SubeId"] = "Şube",
            ["CariId"] = "Cari",
            ["UrunId"] = "Ürün",
            ["Adet"] = "Adet",
            ["Miktar"] = "Miktar",
            ["BirimFiyat"] = "Birim fiyat",
            ["BirimFiyatTl"] = "Birim fiyat (TL)",
            ["IscilikTl"] = "İşçilik (TL)",
            // PAKET PATCH-3: satış uçlarının kolon tavanı iletilerinde ham PascalCase ("Iade tutari TL") görünüyordu.
            ["IadeTutariTl"] = "İade tutarı (TL)",
            ["EskiUrunGram"] = "Eski ürün gramı",
            ["EskiUrunBirimFiyatTlHasGram"] = "Eski ürün has gram fiyatı (TL)",
            ["YeniBirimFiyatTl"] = "Yeni birim fiyat (TL)",
            ["YeniIscilikTl"] = "Yeni işçilik (TL)",
            ["YeniUrunBirimFiyatTl"] = "Yeni ürün birim fiyatı (TL)",
            ["YeniUrunAdet"] = "Yeni ürün adedi",
            ["YeniAdet"] = "Yeni adet",
            ["GeriAlinanAdet"] = "Geri alınan adet",
            ["OdenenTl"] = "Ödenen (TL)",
            ["TahsilEdilenTl"] = "Tahsil edilen (TL)",
            ["FireGrami"] = "Fire gramı",
            ["AliciKabulHasGrami"] = "Alıcının kabul ettiği has gram",
            ["BirimFiyatTlHasGram"] = "Has gram birim fiyatı (TL)",
            ["AvansMahsupTl"] = "Avans mahsubu (TL)",
            ["HasGramBakiye"] = "Has gram bakiyesi",
            ["TlBakiye"] = "TL bakiyesi",
            ["SatisFiyati"] = "Satış fiyatı",
            ["IndirimTutari"] = "İndirim tutarı",
            ["KdvOran"] = "KDV oranı",
            ["OdemeTuru"] = "Ödeme türü",
            ["Aciklama"] = "Açıklama",
            ["Gerekce"] = "Gerekçe",
            ["BelgeTuru"] = "Kimlik belgesi türü",
            ["BelgeNo"] = "Belge numarası",
            ["IslemAmaci"] = "İşlem amacı",
            ["FonKaynagi"] = "Fon kaynağı",
            ["EkBelgeReferansi"] = "Ek belge referansı",
            ["Kaynak"] = "Kaynak",
            ["AltinHasFiyati"] = "Altın has fiyatı",
            ["GramAltinFiyati"] = "Gram altın fiyatı",
            ["UsdTry"] = "USD/TRY",
            ["EurTry"] = "EUR/TRY",
            ["GbpTry"] = "GBP/TRY",
            ["Limit"] = "Limit",
            ["Baslangic"] = "Başlangıç",
            ["Bitis"] = "Bitiş",
            ["Sifre"] = "Şifre",
            ["Rol"] = "Rol",
            ["Aktif"] = "Durum",
            ["PirlantaKarat"] = "Pırlanta karat",
            ["KonsinyeBirimBedel"] = "Konsinye birim bedel",
            ["SayilanAdet"] = "Sayılan adet",
            // PAKET SON-KALANLAR (hasım DÜŞÜK): PascalCase çözümü "Sayilan nakit" (ı yerine i) basıyordu.
            ["SayilanNakit"] = "Sayılan nakit",
            ["KomisyonOran"] = "Komisyon oranı",
            ["IskontoYuzde"] = "İskonto (%)",
            ["GonderimReferansi"] = "Gönderim referansı",
            ["KurSnapshotId"] = "Kur kaydı",
            ["FiyatListesiId"] = "Fiyat listesi",
            ["AcilisStogu"] = "Açılış stoğu",
        };

    /// <summary>Alan adının Türkçe etiketi; sözlükte yoksa PascalCase çözülür.</summary>
    public static string AlanAdi(string? alan)
    {
        // PAKET DIL-SUNUCU: EN istekte alan etiketi İngilizce (DogrulamaSozlugu.En.cs).
        if (IstekDili.Gecerli == "en") return AlanAdiEn(alan);
        if (string.IsNullOrWhiteSpace(alan)) return "Alan";
        // "kalemler[0].Miktar" → "Miktar"
        var son = alan.Split('.').Last();
        var koseli = son.IndexOf('[');
        if (koseli >= 0) son = son[..koseli];
        if (string.IsNullOrWhiteSpace(son)) son = alan;

        if (AlanAdlari.TryGetValue(son, out var tr)) return tr;

        // PascalCase → "Kredi Limiti Tl" → "Kredi limiti"
        var parcali = Regex.Replace(son, "(?<!^)([A-Z])", " $1").Trim();
        var kelimeler = parcali.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        for (var i = 0; i < kelimeler.Length; i++)
        {
            var k = kelimeler[i];
            if (k.Equals("Tl", StringComparison.OrdinalIgnoreCase)) k = "TL";
            else if (k.Equals("Id", StringComparison.OrdinalIgnoreCase)) continue;
            else if (i > 0) k = k.ToLowerInvariant();
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(k);
        }
        return sb.Length == 0 ? son : sb.ToString();
    }

    /// <summary>camelCase anahtar — ekran alan altına yazabilsin diye.</summary>
    public static string Anahtar(string alan)
    {
        if (string.IsNullOrEmpty(alan)) return alan;
        var parcalar = alan.Split('.');
        for (var i = 0; i < parcalar.Length; i++)
        {
            var p = parcalar[i];
            if (p.Length > 0 && char.IsUpper(p[0]))
                parcalar[i] = char.ToLowerInvariant(p[0]) + p[1..];
        }
        return string.Join('.', parcalar);
    }

    // ── İngilizce varsayılan DataAnnotations mesajları ────────────────────────
    // Sıra ÖNEMLİ: daha özel desenler önce.
    private static readonly (Regex Desen, Func<string, Match, string> Ceviri)[] Kalip =
    [
        (new Regex(@"^The (?<a>.+?) field is required\.$"),
            (alan, _) => $"{alan} zorunludur"),

        (new Regex(@"^A non-empty request body is required\.$"),
            (_, _) => "İstek gövdesi boş olamaz"),

        (new Regex(@"^The (?<a>.+?) field is not a valid e-?mail address\.$"),
            (alan, _) => $"{alan} geçerli bir e-posta adresi olmalıdır (örnek: ad@ornek.com)"),

        (new Regex(@"^The (?<a>.+?) field is not a valid phone number\.$"),
            (alan, _) => $"{alan} geçerli bir telefon numarası olmalıdır"),

        (new Regex(@"^The (?<a>.+?) field is not a valid fully-qualified .*$"),
            (alan, _) => $"{alan} geçerli bir adres (URL) olmalıdır"),

        (new Regex(@"^The field (?<a>.+?) must be between (?<min>\S+) and (?<max>\S+)\.$"),
            (alan, m) => AralikMesaji(alan, m.Groups["min"].Value, m.Groups["max"].Value)),

        // PAKET PATCH-3: Range(MinimumIsExclusive = true) iletisi ("0 exclusive") çevrilmiyordu → İngilizce sızıyordu.
        (new Regex(@"^The field (?<a>.+?) must be between (?<min>\S+) exclusive and (?<max>\S+)( exclusive)?\.$"),
            (alan, m) => m.Groups["max"].Value.Contains('E') || m.Groups["max"].Value.Contains('e')
                ? $"{alan} {Okunur(m.Groups["min"].Value)} değerinden büyük olmalıdır"
                : $"{alan} {Okunur(m.Groups["min"].Value)} değerinden büyük, en çok {Okunur(m.Groups["max"].Value)} olmalıdır"),

        (new Regex(@"^The field (?<a>.+?) must be a string or array type with a maximum length of '(?<n>\d+)'\.$"),
            (alan, m) => $"{alan} en fazla {m.Groups["n"].Value} karakter olabilir"),

        (new Regex(@"^The field (?<a>.+?) must be a string or array type with a minimum length of '(?<n>\d+)'\.$"),
            (alan, m) => $"{alan} en az {m.Groups["n"].Value} karakter olmalıdır"),

        (new Regex(@"^The field (?<a>.+?) must be a string with a minimum length of (?<min>\d+) and a maximum length of (?<max>\d+)\.$"),
            (alan, m) => $"{alan} en az {m.Groups["min"].Value}, en fazla {m.Groups["max"].Value} karakter olmalıdır"),

        (new Regex(@"^The field (?<a>.+?) must be a string with a maximum length of (?<max>\d+)\.$"),
            (alan, m) => $"{alan} en fazla {m.Groups["max"].Value} karakter olabilir"),

        (new Regex(@"^The field (?<a>.+?) must match the regular expression '.*'\.$"),
            (alan, _) => $"{alan} beklenen biçimde değil"),

        (new Regex(@"^The value '(?<v>.*)' is not valid( for .*)?\.$"),
            (alan, m) => $"{alan}: '{m.Groups["v"].Value}' geçerli bir değer değil"),

        (new Regex(@"^The JSON value could not be converted to .*$"),
            (alan, _) => $"{alan} beklenen türde değil"),

        (new Regex(@"^Could not convert string to .*$"),
            (alan, _) => $"{alan} beklenen türde değil"),
    ];

    /// <summary>
    /// Range iletisi — TEK İLETİ (PAKET PATCH-3, PATCH-SOZLESME tur 3 açığı).
    /// <para>Range iletisi değerin alt mı üst mü sınırı aştığını söylemez; bu yüzden ileti
    /// İKİ YÖNDE DE doğru olmalıdır. ESKİ: üst sınır ≥ 10⁹ ise yalnız "X veya daha büyük"
    /// deniyordu — üst sınırı aşan değerde YANILTICIYDI (değer zaten büyüktü). YENİ: yalnız
    /// gerçekten sınırsız üst sınır (double.MaxValue → "E+308" gösterimi; decimal'e sığmayan
    /// sayı zaten JSON çözücüde reddedilir) gizlenir; sonlu her üst sınır yazılır, büyükse
    /// binlik ayırıcıyla okunur biçimde (9.999.999.999.999,99).</para>
    /// </summary>
    private static string AralikMesaji(string alan, string min, string max)
    {
        var ustSinirsiz = max.Contains('E') || max.Contains('e');
        return ustSinirsiz
            ? $"{alan} {Okunur(min)} veya daha büyük olmalıdır"
            : $"{alan} {Okunur(min)} ile {Okunur(max)} arasında olmalıdır";
    }

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// Büyük sınır (≥ 1.000.000) tr-TR binlik ayırıcıyla; küçükler binliksiz tr-TR ondalıkla
    /// (3650 → "3650", 0.01 → "0,01") — sunucu kültüründen BAĞIMSIZ.
    /// Range iletisi sınırı double "G" biçimiyle yazar (binlik ayırıcı YOK; ondalık ayırıcı
    /// sunucu kültürüne göre ',' ya da '.') → tek ayırıcı ondalıktır, kültürden bağımsız okunur.
    /// </summary>
    private static string Okunur(string sayi)
    {
        // Küçük alt sınır double "G" ile bilimsel yazılır (0,000001 → "1E-06"): ondalığa çevrilir.
        if ((sayi.Contains("E-") || sayi.Contains("e-"))
            && double.TryParse(sayi.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var kucuk))
            return ((decimal)kucuk).ToString("0.##########", Tr);
        if (!decimal.TryParse(sayi.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var d))
            return sayi;
        // PATCH-3 tur 2 (doğrulayıcı ORTA): küçük sınır da tr-TR ondalıkla yazılır — ham
        // bırakılınca kültürsüz (invariant) sunucuda "0.01 ile 9.999.999…,99" karışık çıkıyordu.
        return d.ToString(Math.Abs(d) >= 1_000_000m ? "#,##0.######" : "0.######", Tr);
    }

    /// <summary>
    /// Doğrulama mesajını Türkçeleştirir. Mesaj zaten Türkçeyse (ErrorMessage
    /// elle verilmişse) OLDUĞU GİBİ döner — elle yazılmış mesaj her zaman önceliklidir.
    /// </summary>
    public static string Cevir(string? alanAnahtari, string? mesaj)
    {
        // PAKET DIL-SUNUCU: EN istekte İngilizce desen çıktıları + elle yazılmış cümlenin sözlükten çevirisi.
        if (IstekDili.Gecerli == "en") return CevirEn(alanAnahtari, mesaj);
        var alan = AlanAdi(alanAnahtari);
        if (string.IsNullOrWhiteSpace(mesaj)) return $"{alan} geçersiz";

        foreach (var (desen, ceviri) in Kalip)
        {
            var m = desen.Match(mesaj);
            if (m.Success) return ceviri(alan, m);
        }
        return mesaj;   // elle yazılmış (Türkçe) mesaj
    }
}
