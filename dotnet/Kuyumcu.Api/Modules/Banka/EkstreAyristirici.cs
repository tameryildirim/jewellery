using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Kuyumcu.Api.Modules.Banka;

// ═════════════════════════════════════════════════════════════════════════════
// EKSTRE AYRIŞTIRICI — SAF SINIF (paket 32 Faz A).
//
// ⚠ DbContext GÖRMEZ. Sebep yalnız "test edilmesi kolay olsun" değil: ayrıştırma
//   KULLANICI DOSYASINI işler, yani bu sınıf projenin zehirleme yüzeyidir.
//   Veritabanına erişimi olmayan bir sınıf, en kötü ihtimalle yanlış satır
//   üretir — yazamaz.
//
// ⚠ FAIL-CLOSED: çözülemeyen satır ATLANMAZ. Hata mesajıyla birlikte geri
//   döner ve sahne tablosuna HATALI olarak yazılır. "Sessizce eksik içe
//   aktarım" bu projedeki en pahalı hata sınıfıdır: banka bakiyesi tutmaz ve
//   kimse nedenini bulamaz.
//
// ⚠ DoS TAVANLARI (Mehmet koşulu) burada SABİT DEĞİL, çağıran uçtan gelir
//   (EkstreAyarlari) — ADR-0005: eşik koda gömülmez.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Dosyadan çözülmüş TEK satır. Hata doluysa satır HATALI'dır.</summary>
public sealed record AyristirilmisSatir(
    int SatirNo,
    string HamSatir,
    DateTime? Tarih,
    string? Yon,
    decimal? Tutar,
    string? KarsiTarafAd,
    string? KarsiIban,
    string? BankaReferans,
    string? Aciklama,
    string? Hata);

/// <summary>Ayrıştırma sonucu: satırlar + dosya düzeyi bilgiler.</summary>
public sealed record AyristirmaSonucu(
    IReadOnlyList<AyristirilmisSatir> Satirlar,
    decimal? AcilisBakiye,
    decimal? KapanisBakiye);

/// <summary>Ayrıştırma tavanları — yapılandırmadan gelir (ADR-0005).</summary>
public sealed class EkstreAyarlari
{
    /// <summary>Ham dosya tavanı (bayt). Varsayılan 2 MB.</summary>
    public int MaksDosyaBayt { get; init; } = 2_000_000;

    /// <summary>Satır tavanı. Varsayılan 10.000.</summary>
    public int MaksSatir { get; init; } = 10_000;

    /// <summary>Tek satır uzunluk tavanı (karakter) — "CSV bomb" savunması.</summary>
    public int MaksSatirUzunluk { get; init; } = 2_000;

    /// <summary>Tek satırdaki alan sayısı tavanı.</summary>
    public int MaksAlanSayisi { get; init; } = 50;

    /// <summary>Tek onay isteğinde işlenebilecek en fazla satır.</summary>
    public int MaksOnaySatir { get; init; } = 500;

    /// <summary>Satır listesi sayfa tavanı (TB-19).</summary>
    public int SatirListesiTavan { get; init; } = 100;

    /// <summary>Ünvan benzerliği öneri eşiği — YALNIZ sıralama içindir (K4-OK-5).</summary>
    public decimal OneriEsigi { get; init; } = 0.60m;
}

public static class EkstreAyristirici
{
    private const string Bom = "\uFEFF";

    /// <summary>Beklenen CSV başlıkları — /sablon.csv bu diziden üretilir.</summary>
    public static readonly string[] SablonBasliklari =
        ["tarih", "aciklama", "borc", "alacak", "bakiye", "referans", "karsiIban", "karsiUnvan"];

    // ── Kodlama ──────────────────────────────────────────────────────────────

    /// <summary>
    /// UTF-8 dene; geçersiz bayt varsa ISO-8859-9 (Latin-5) ile yeniden dene.
    /// Bankaların text ekstresi genelde Latin-5'tir ve "ŞİRKET" UTF-8 olarak
    /// çözülmeye çalışılırsa sessizce "?İRKET" olurdu.
    /// </summary>
    public static string MetneCevir(byte[] ham)
    {
        try
        {
            return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(ham);
        }
        catch (DecoderFallbackException)
        {
            // Latin-5. .NET Core'da kod sayfaları ayrıca kaydedilir.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(28599).GetString(ham);
        }
    }

    /// <summary>Dosyanın SHA-256'sı (büyük harf hex) — mükerrer DOSYA tespiti.</summary>
    public static string DosyaOzeti(byte[] ham) => Convert.ToHexString(SHA256.HashData(ham));

    // ── CSV ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ayraç otomatik: ilk satırda ';' sayısı ',' sayısından fazlaysa ';'.
    /// Türk bankalarının çoğu ';' kullanır (ondalık ayracı virgül olduğu için).
    /// </summary>
    public static char AyracBul(string ilkSatir) =>
        ilkSatir.Count(c => c == ';') > ilkSatir.Count(c => c == ',') ? ';' : ',';

    /// <summary>
    /// CSV metnini satır satır çözer.
    /// <para>⚠ Tavan aşımlarında <see cref="Common.BadRequestException"/> atar ve
    /// KISMİ SONUÇ DÖNMEZ: yarım içe aktarım, hiç içe aktarmamaktan pahalıdır.</para>
    /// </summary>
    public static AyristirmaSonucu CsvAyristir(string metin, EkstreAyarlari ayar)
    {
        if (metin.StartsWith(Bom, StringComparison.Ordinal)) metin = metin[Bom.Length..];

        var hamSatirlar = metin.Split('\n')
            .Select(s => s.TrimEnd('\r'))
            .Where(s => s.Trim().Length > 0)
            .ToList();

        if (hamSatirlar.Count == 0)
            throw new Common.BadRequestException("Ekstre dosyası boş — başlık satırı bulunamadı");

        // Başlık satırı da sayılır; veri satırı tavanı ayrıca ölçülür.
        if (hamSatirlar.Count - 1 > ayar.MaksSatir)
            throw new Common.BadRequestException(
                $"Ekstre dosyası {hamSatirlar.Count - 1} satır içeriyor; tavan {ayar.MaksSatir}. " +
                "Dosyayı dönem dönem bölüp yeniden yükleyin. Kısmi içe aktarım YAPILMAZ.");

        var uzun = hamSatirlar.FindIndex(s => s.Length > ayar.MaksSatirUzunluk);
        if (uzun >= 0)
            throw new Common.BadRequestException(
                $"{uzun + 1}. satır {hamSatirlar[uzun].Length} karakter; tavan {ayar.MaksSatirUzunluk}. " +
                "Dosya ekstre biçiminde değil ya da bozuk.");

        var ayrac = AyracBul(hamSatirlar[0]);
        var basliklar = Alanlar(hamSatirlar[0], ayrac, ayar);
        var harita = BasliklariEsle(basliklar);

        if (!harita.ContainsKey("tarih"))
            throw new Common.BadRequestException(
                $"Ekstre başlığında tarih sütunu tanınmadı. Beklenen başlıklar: {string.Join(";", SablonBasliklari)} — örnek dosya için 'Şablon indir'.");
        if (!harita.ContainsKey("borc") && !harita.ContainsKey("alacak") && !harita.ContainsKey("tutar"))
            throw new Common.BadRequestException(
                "Ekstre başlığında borç/alacak (ya da tutar) sütunu tanınmadı. " +
                "Örnek dosya için 'Şablon indir'.");

        var sonuc = new List<AyristirilmisSatir>(hamSatirlar.Count - 1);
        for (var i = 1; i < hamSatirlar.Count; i++)
            sonuc.Add(SatirCoz(i, hamSatirlar[i], ayrac, harita, ayar));

        // CSV'de açılış/kapanış bakiyesi YOKTUR (MT940 :60F:/:62F: — Faz B).
        return new AyristirmaSonucu(sonuc, null, null);
    }

    private static AyristirilmisSatir SatirCoz(
        int satirNo, string ham, char ayrac, Dictionary<string, int> harita, EkstreAyarlari ayar)
    {
        var kirpik = ham.Length > 500 ? ham[..500] : ham;

        string[] alanlar;
        try
        {
            alanlar = Alanlar(ham, ayrac, ayar);
        }
        catch (Common.BadRequestException ex)
        {
            return Hatali(satirNo, kirpik, ex.Message);
        }

        string? Al(string ad) =>
            harita.TryGetValue(ad, out var idx) && idx < alanlar.Length
                ? (alanlar[idx].Length == 0 ? null : alanlar[idx])
                : null;

        if (!TarihCoz(Al("tarih"), out var tarih))
            return Hatali(satirNo, kirpik, $"Tarih çözülemedi: '{Al("tarih")}'");

        var borcHam = Al("borc");
        var alacakHam = Al("alacak");
        var tutarHam = Al("tutar");

        decimal? borc = null, alacak = null, tutarTek = null;
        if (borcHam is not null && !SayiCoz(borcHam, out borc))
            return Hatali(satirNo, kirpik, $"Borç tutarı çözülemedi: '{borcHam}'");
        if (alacakHam is not null && !SayiCoz(alacakHam, out alacak))
            return Hatali(satirNo, kirpik, $"Alacak tutarı çözülemedi: '{alacakHam}'");
        if (tutarHam is not null && !SayiCoz(tutarHam, out tutarTek))
            return Hatali(satirNo, kirpik, $"Tutar çözülemedi: '{tutarHam}'");

        // YÖN SÖZLEŞMESİ: borç dolu → GIDEN, alacak dolu → GELEN.
        // Tek "tutar" sütunlu dosyalarda işaret yönü belirler. Tutar HER ZAMAN
        // pozitif yazılır (BankaModulu yön katsayısı sözleşmesi korunur).
        string yon;
        decimal tutar;
        if (borc is > 0 && alacak is > 0)
            return Hatali(satirNo, kirpik, "Aynı satırda hem borç hem alacak dolu — yön belirsiz");
        if (borc is > 0) { yon = Core.Domain.HareketYonu.BankaGiden; tutar = borc.Value; }
        else if (alacak is > 0) { yon = Core.Domain.HareketYonu.BankaGelen; tutar = alacak.Value; }
        else if (tutarTek is not null && tutarTek.Value != 0)
        {
            yon = tutarTek.Value > 0
                ? Core.Domain.HareketYonu.BankaGelen
                : Core.Domain.HareketYonu.BankaGiden;
            tutar = Math.Abs(tutarTek.Value);
        }
        else return Hatali(satirNo, kirpik, "Satırda tutar yok (borç ve alacak boş)");

        return new AyristirilmisSatir(
            satirNo, kirpik, tarih, yon, decimal.Round(tutar, 2),
            Kirp(Al("karsiUnvan"), 150), IbanNormalle(Al("karsiIban")),
            Kirp(Al("referans"), 50), Kirp(Al("aciklama"), 1000), null);
    }

    private static AyristirilmisSatir Hatali(int satirNo, string ham, string hata) =>
        new(satirNo, ham, null, null, null, null, null, null, null, Kirp(hata, 200));

    private static string? Kirp(string? s, int n) =>
        s is null ? null : (s.Length > n ? s[..n] : s);

    /// <summary>Boşlukları söker, büyük harfe çevirir. Hash de aynı normalizasyonu yapar.</summary>
    public static string? IbanNormalle(string? ham) =>
        string.IsNullOrWhiteSpace(ham)
            ? null
            : Kirp(new string(ham.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant(), 34);

    // ── Alan ayırma (tırnak destekli) ────────────────────────────────────────

    private static string[] Alanlar(string satir, char ayrac, EkstreAyarlari ayar)
    {
        var liste = new List<string>();
        var kutu = new StringBuilder();
        var tirnakta = false;

        for (var i = 0; i < satir.Length; i++)
        {
            var c = satir[i];
            if (tirnakta)
            {
                if (c == '"')
                {
                    if (i + 1 < satir.Length && satir[i + 1] == '"') { kutu.Append('"'); i++; }
                    else tirnakta = false;
                }
                else kutu.Append(c);
                continue;
            }

            if (c == '"') { tirnakta = true; continue; }
            if (c == ayrac)
            {
                liste.Add(Temizle(kutu.ToString()));
                kutu.Clear();
                if (liste.Count > ayar.MaksAlanSayisi)
                    throw new Common.BadRequestException(
                        $"Satırdaki alan sayısı {ayar.MaksAlanSayisi} tavanını aştı");
                continue;
            }
            kutu.Append(c);
        }
        liste.Add(Temizle(kutu.ToString()));
        return [.. liste];
    }

    /// <summary>
    /// CSV ENJEKSİYONU: kendi dışa aktarımımız "=" ile başlayan hücrenin başına
    /// tek tırnak koyar (CsvYazici). Burada o tırnak GİRİŞTE SÖKÜLÜR — yoksa
    /// kendi dosyamızı geri okuduğumuzda ad "'=Ahmet" olurdu. Meşru kesme
    /// işareti ("'Ahmet") KORUNUR.
    /// </summary>
    public static string Temizle(string ham)
    {
        var s = ham.Trim();
        if (s.Length >= 2 && s[0] == '\'' && "=+-@\t\r".Contains(s[1])) s = s[1..];
        return s;
    }

    // ── Başlık tanıma ────────────────────────────────────────────────────────

    private static readonly Dictionary<string, string[]> BaslikSozlugu = new()
    {
        ["tarih"] = ["tarih", "islemtarihi", "valortarihi", "date", "transactiondate"],
        ["aciklama"] = ["aciklama", "açıklama", "description", "detay", "islemaciklamasi"],
        ["borc"] = ["borc", "borç", "debit", "cikis", "çıkış", "gidenborc"],
        ["alacak"] = ["alacak", "credit", "giris", "giriş", "gelenalacak"],
        ["tutar"] = ["tutar", "amount", "islemtutari"],
        ["bakiye"] = ["bakiye", "balance"],
        ["referans"] = ["referans", "reference", "dekontno", "islemreferansno", "fisno"],
        ["karsiIban"] = ["karsiiban", "karşıiban", "iban", "counterpartyiban", "gonderenıban"],
        ["karsiUnvan"] = ["karsiunvan", "karşıünvan", "unvan", "ünvan", "karsitaraf", "adsoyad", "gonderen"],
    };

    /// <summary>
    /// Tanınmayan başlıklı dosya için KOLON EŞLEME SİHİRBAZI YOKTUR (kapsam
    /// kararı): otomatik tanıma + yayımladığımız şablon yeter. Tanınmayan başlık
    /// sessizce yutulmaz — zorunlu kolon bulunamazsa dosya 400 ile reddedilir.
    /// </summary>
    public static Dictionary<string, int> BasliklariEsle(string[] basliklar)
    {
        var harita = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < basliklar.Length; i++)
        {
            var n = BaslikNormalle(basliklar[i]);
            if (n.Length == 0) continue;
            foreach (var (alan, esler) in BaslikSozlugu)
            {
                if (harita.ContainsKey(alan)) continue;
                if (esler.Any(e => BaslikNormalle(e) == n)) { harita[alan] = i; break; }
            }
        }
        return harita;
    }

    private static string BaslikNormalle(string ham) =>
        new(ham.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    // ── Sayı ve tarih ────────────────────────────────────────────────────────

    /// <summary>
    /// TR ("1.234,56") ve EN ("1,234.56") ondalık biçimlerinin İKİSİNİ de çözer.
    /// Kural: SON ayraçtan sonra tam 2 (ya da 1) hane varsa o ayraç ondalıktır;
    /// yoksa hepsi binlik ayracıdır. Kültür ayarı KULLANILMAZ — makine kültürü
    /// testi yeşil/kırmızı yapmasın (Aşkın koşulu).
    /// </summary>
    public static bool SayiCoz(string? ham, out decimal? deger)
    {
        deger = null;
        if (string.IsNullOrWhiteSpace(ham)) return true;

        var s = ham.Trim().Replace(" ", "").Replace("\u00a0", "");
        // Para birimi simgesi / kodu temizliği.
        s = new string(s.Where(c => char.IsDigit(c) || c is '.' or ',' or '-' or '+').ToArray());
        if (s.Length == 0) return false;

        var negatif = s.StartsWith('-');
        s = s.TrimStart('+', '-');
        if (s.Length == 0) return false;

        var sonNokta = s.LastIndexOf('.');
        var sonVirgul = s.LastIndexOf(',');
        var son = Math.Max(sonNokta, sonVirgul);

        string tamsayi, ondalik = "";
        if (son >= 0)
        {
            var kuyruk = s[(son + 1)..];
            if (kuyruk.Length is 1 or 2 && kuyruk.All(char.IsDigit))
            {
                tamsayi = s[..son];
                ondalik = kuyruk;
            }
            else tamsayi = s;   // hepsi binlik ayracı
        }
        else tamsayi = s;

        tamsayi = new string(tamsayi.Where(char.IsDigit).ToArray());
        if (tamsayi.Length == 0 && ondalik.Length == 0) return false;
        if (tamsayi.Length == 0) tamsayi = "0";

        var metin = ondalik.Length > 0 ? $"{tamsayi}.{ondalik}" : tamsayi;
        if (!decimal.TryParse(metin, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
            return false;

        deger = negatif ? -d : d;
        return true;
    }

    private static readonly string[] TarihBicimleri =
    [
        "dd.MM.yyyy", "dd/MM/yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "yyyy/MM/dd",
        "dd.MM.yy", "dd/MM/yy", "yyyyMMdd",
        "dd.MM.yyyy HH:mm", "dd.MM.yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss",
    ];

    /// <summary>
    /// Dosyadaki tarih SAAT TAŞIMAZ; yerel gün başı UTC'ye çevrilir
    /// (Zaman.YerelGunBasiUtc) — aksi hâlde "30 Eylül" satırı UTC'de 29 Eylül'e
    /// düşer ve dönem raporları kayardı.
    /// </summary>
    public static bool TarihCoz(string? ham, out DateTime? deger)
    {
        deger = null;
        if (string.IsNullOrWhiteSpace(ham)) return false;
        var s = ham.Trim();
        if (!DateTime.TryParseExact(s, TarihBicimleri, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var t))
            return false;
        deger = Common.Zaman.YerelGunBasiUtc(DateOnly.FromDateTime(t));
        return true;
    }
}
