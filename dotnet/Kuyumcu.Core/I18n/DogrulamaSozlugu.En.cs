using System.Text;
using System.Text.RegularExpressions;

namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET DIL-SUNUCU — DOĞRULAMA İLETİLERİNİN İNGİLİZCE KOLU
//
// DogrulamaSozlugu.Cevir İngilizce çerçeve iletisini TÜRKÇEYE çeviriyordu; EN
// istekte (IstekDili = "en") sonuç artık İngilizce ve alan etiketi İngilizcedir.
// Türkçe yol DEĞİŞMEDİ. Elle yazılmış (Türkçe) ileti SunucuIletiSozlugu'ndan çevrilir.
// ═════════════════════════════════════════════════════════════════════════════

public static partial class DogrulamaSozlugu
{
    /// <summary>Alan adı → İngilizce etiket (TR sözlüğüyle AYNI anahtarlar; test eşitliği denetler).</summary>
    public static readonly IReadOnlyDictionary<string, string> AlanAdlariEn =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Ad"] = "Name", ["Soyad"] = "Surname", ["AdSoyad"] = "Full name", ["Email"] = "E-mail", ["Telefon"] = "Phone",
            ["VergiNo"] = "Tax number", ["Adres"] = "Address", ["Notlar"] = "Notes", ["KrediLimitiTl"] = "Credit limit (TRY)",
            ["VarsayilanVadeGun"] = "Default term (days)", ["VadeGun"] = "Term (days)", ["Tur"] = "Type", ["Sira"] = "Order",
            ["Yon"] = "Direction", ["Tutar"] = "Amount", ["TlTutari"] = "TRY amount", ["HasGrami"] = "Fine grams",
            ["GramAgirlik"] = "Gram weight", ["TasAgirlik"] = "Stone weight", ["GercekMilyem"] = "Actual fineness",
            ["AyarId"] = "Purity", ["KasaId"] = "Cash desk", ["SubeId"] = "Branch", ["CariId"] = "Customer", ["UrunId"] = "Product",
            ["Adet"] = "Quantity", ["Miktar"] = "Quantity", ["BirimFiyat"] = "Unit price", ["BirimFiyatTl"] = "Unit price (TRY)",
            ["IscilikTl"] = "Labour (TRY)", ["IadeTutariTl"] = "Refund amount (TRY)", ["EskiUrunGram"] = "Old product grams",
            ["EskiUrunBirimFiyatTlHasGram"] = "Old product fine-gram price (TRY)", ["YeniBirimFiyatTl"] = "New unit price (TRY)",
            ["YeniIscilikTl"] = "New labour (TRY)", ["YeniUrunBirimFiyatTl"] = "New product unit price (TRY)",
            ["YeniUrunAdet"] = "New product quantity", ["YeniAdet"] = "New quantity", ["GeriAlinanAdet"] = "Quantity taken back",
            ["OdenenTl"] = "Paid (TRY)", ["TahsilEdilenTl"] = "Collected (TRY)", ["FireGrami"] = "Wastage grams",
            ["AliciKabulHasGrami"] = "Fine grams accepted by the buyer", ["BirimFiyatTlHasGram"] = "Fine-gram unit price (TRY)",
            ["AvansMahsupTl"] = "Advance offset (TRY)", ["HasGramBakiye"] = "Fine gram balance", ["TlBakiye"] = "TRY balance",
            ["SatisFiyati"] = "Sale price", ["IndirimTutari"] = "Discount amount", ["KdvOran"] = "VAT rate", ["OdemeTuru"] = "Payment type",
            ["Aciklama"] = "Description", ["Gerekce"] = "Reason", ["BelgeTuru"] = "Identity document type", ["BelgeNo"] = "Document number",
            ["IslemAmaci"] = "Purpose of the transaction", ["FonKaynagi"] = "Source of funds", ["EkBelgeReferansi"] = "Supporting document reference",
            ["Kaynak"] = "Source", ["AltinHasFiyati"] = "Fine gold price", ["GramAltinFiyati"] = "Gram gold price", ["UsdTry"] = "USD/TRY",
            ["EurTry"] = "EUR/TRY", ["GbpTry"] = "GBP/TRY", ["Limit"] = "Limit", ["Baslangic"] = "Start", ["Bitis"] = "End",
            ["Sifre"] = "Password", ["Rol"] = "Role", ["Aktif"] = "Status", ["PirlantaKarat"] = "Diamond carat",
            ["KonsinyeBirimBedel"] = "Consignment unit price", ["SayilanAdet"] = "Counted quantity", ["SayilanNakit"] = "Counted cash",
            ["KomisyonOran"] = "Commission rate", ["IskontoYuzde"] = "Discount (%)", ["GonderimReferansi"] = "Submission reference",
            ["KurSnapshotId"] = "Rate record", ["FiyatListesiId"] = "Price list", ["AcilisStogu"] = "Opening stock",
        };

    /// <summary>İngilizce alan etiketi: bilinen ad, yoksa PascalCase kelimeleri (Id/Tl ayıklanmaz — sade).</summary>
    public static string AlanAdiEn(string? alan)
    {
        if (string.IsNullOrWhiteSpace(alan)) return "Field";
        var son = alan.Split('.').Last();
        var koseli = son.IndexOf('[');
        if (koseli >= 0) son = son[..koseli];
        if (string.IsNullOrWhiteSpace(son)) son = alan;
        if (AlanAdlariEn.TryGetValue(son, out var en)) return en;
        var kelimeler = Regex.Replace(son, "(?<!^)([A-Z])", " $1").Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        for (var i = 0; i < kelimeler.Length; i++)
        {
            var k = kelimeler[i];
            if (k.Equals("Id", StringComparison.OrdinalIgnoreCase)) continue;
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(i == 0 ? k : k.ToLowerInvariant());
        }
        return sb.Length == 0 ? son : sb.ToString();
    }

    private static readonly (Regex Desen, Func<string, Match, string> Ceviri)[] KalipEn =
    [
        (new Regex(@"^The (?<a>.+?) field is required\.$"), (a, _) => $"{a} is required"),
        (new Regex(@"^A non-empty request body is required\.$"), (_, _) => "The request body cannot be empty"),
        (new Regex(@"^The (?<a>.+?) field is not a valid e-?mail address\.$"),
            (a, _) => $"{a} must be a valid e-mail address (example: name@example.com)"),
        (new Regex(@"^The (?<a>.+?) field is not a valid phone number\.$"), (a, _) => $"{a} must be a valid phone number"),
        (new Regex(@"^The (?<a>.+?) field is not a valid fully-qualified .*$"), (a, _) => $"{a} must be a valid address (URL)"),
        (new Regex(@"^The field (?<a>.+?) must be between (?<min>\S+) and (?<max>\S+)\.$"),
            (a, m) => $"{a} must be between {m.Groups["min"].Value} and {m.Groups["max"].Value}"),
        (new Regex(@"^The field (?<a>.+?) must be between (?<min>\S+) exclusive and (?<max>\S+)( exclusive)?\.$"),
            (a, m) => $"{a} must be greater than {m.Groups["min"].Value} and at most {m.Groups["max"].Value}"),
        (new Regex(@"^The field (?<a>.+?) must be a string or array type with a maximum length of '(?<n>\d+)'\.$"),
            (a, m) => $"{a} can be at most {m.Groups["n"].Value} characters"),
        (new Regex(@"^The field (?<a>.+?) must be a string or array type with a minimum length of '(?<n>\d+)'\.$"),
            (a, m) => $"{a} must be at least {m.Groups["n"].Value} characters"),
        (new Regex(@"^The field (?<a>.+?) must be a string with a minimum length of (?<min>\d+) and a maximum length of (?<max>\d+)\.$"),
            (a, m) => $"{a} must be between {m.Groups["min"].Value} and {m.Groups["max"].Value} characters"),
        (new Regex(@"^The field (?<a>.+?) must be a string with a maximum length of (?<max>\d+)\.$"),
            (a, m) => $"{a} can be at most {m.Groups["max"].Value} characters"),
        (new Regex(@"^The field (?<a>.+?) must match the regular expression '.*'\.$"), (a, _) => $"{a} is not in the expected format"),
        (new Regex(@"^The value '(?<v>.*)' is not valid( for .*)?\.$"), (a, m) => $"{a}: '{m.Groups["v"].Value}' is not a valid value"),
        (new Regex(@"^The JSON value could not be converted to .*$"), (a, _) => $"{a} is not of the expected type"),
        (new Regex(@"^Could not convert string to .*$"), (a, _) => $"{a} is not of the expected type"),
    ];

    private static string CevirEn(string? alanAnahtari, string? mesaj)
    {
        var alan = AlanAdiEn(alanAnahtari);
        if (string.IsNullOrWhiteSpace(mesaj)) return $"{alan} is invalid";
        foreach (var (desen, ceviri) in KalipEn)
        {
            var m = desen.Match(mesaj);
            if (m.Success) return ceviri(alan, m);
        }
        return SunucuIletiSozlugu.Cevir(mesaj, "en");   // elle yazılmış (Türkçe) mesaj → sözlük; yoksa AYNEN
    }
}
