using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Kuyumcu.Api.Modules.Iceaktarim;

// ═════════════════════════════════════════════════════════════════════════════
// CSV OKUYUCU — RFC 4180. CsvYazici'nin TERSİ.
//
// Neden AYRI klasörde (Disaaktarim/ altında değil): dışa aktarım modülü kapalı
// bir bütündür ve SIZDIRMA yüzeyidir; içe aktarım ZEHİRLEME yüzeyidir. İkisini
// aynı klasöre koymak, birinin denetim kuralını (ör. "Iceaktarim altında Add=0")
// diğerinin dosyalarına bulaştırırdı.
//
// ⚠ CSV ENJEKSİYONU İKİ YÖNLÜDÜR: CsvYazici çıkışta "=" ile başlayan hücrenin
//    başına tek tırnak koyar. Burada o tırnak GİRİŞTE SÖKÜLÜR — aksi hâlde
//    kendi dosyamızı geri okuduğumuzda müşteri adı "'=Ahmet" olurdu. Sökme
//    yalnız tehlikeli karakterle DEVAM EDEN tırnakta yapılır: "'Ahmet" gibi
//    meşru bir kesme işareti kaybolmaz.
//
// ⚠ KÜLTÜR: sayı ve tarih InvariantCulture ile okunur. tr-TR iş parçacığında
//    da ondalık ayracı NOKTA'dır (testle sabitlendi).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Ayrıştırılmış bir CSV dosyası: başlıklar + veri satırları.</summary>
public sealed record CsvTablo(string[] Basliklar, IReadOnlyList<string[]> Satirlar);

public static class CsvOkuyucu
{
    /// <summary>Excel'in yazdığı UTF-8 BOM'u.</summary>
    public const string Bom = "﻿";

    /// <summary>CsvYazici.Hucre'nin kaçırdığı karakterler — baştaki tırnak bunlar için konur.</summary>
    private const string Tehlikeli = "=+-@\t\r";

    /// <summary>
    /// Ham metni RFC 4180'e göre satır satır çözer. Tırnaklı alanda virgül ve
    /// yeni satır KORUNUR; iki tırnak bir tırnağa iner; CRLF ve LF ikisi de
    /// satır sonu sayılır.
    /// </summary>
    public static IEnumerable<string[]> HamSatirlar(string metin)
    {
        if (metin.StartsWith(Bom, StringComparison.Ordinal)) metin = metin[Bom.Length..];

        var alanlar = new List<string>();
        var kutu = new StringBuilder();
        var tirnakta = false;
        var satirdaVeriVar = false;

        for (var i = 0; i < metin.Length; i++)
        {
            var c = metin[i];

            if (tirnakta)
            {
                if (c == '"')
                {
                    if (i + 1 < metin.Length && metin[i + 1] == '"') { kutu.Append('"'); i++; }
                    else tirnakta = false;
                }
                else kutu.Append(c);
                satirdaVeriVar = true;
                continue;
            }

            switch (c)
            {
                case '"':
                    tirnakta = true;
                    satirdaVeriVar = true;
                    break;
                case ',':
                    alanlar.Add(Temizle(kutu.ToString()));
                    kutu.Clear();
                    satirdaVeriVar = true;
                    break;
                case '\r':
                    break;   // CRLF: \n tarafı satırı bitirir
                case '\n':
                    alanlar.Add(Temizle(kutu.ToString()));
                    kutu.Clear();
                    if (satirdaVeriVar) yield return alanlar.ToArray();
                    alanlar.Clear();
                    satirdaVeriVar = false;
                    break;
                default:
                    kutu.Append(c);
                    satirdaVeriVar = true;
                    break;
            }
        }

        if (satirdaVeriVar || kutu.Length > 0)
        {
            alanlar.Add(Temizle(kutu.ToString()));
            yield return alanlar.ToArray();
        }
    }

    /// <summary>
    /// Baştaki CSV-enjeksiyon kaçış tırnağını söker ve kırpar.
    /// <para>"'=1+1" → "=1+1" · "'Ahmet" → "'Ahmet" (meşru kesme korunur).</para>
    /// </summary>
    public static string Temizle(string ham)
    {
        var s = ham.Trim();
        if (s.Length >= 2 && s[0] == '\'' && Tehlikeli.Contains(s[1])) s = s[1..];
        return s;
    }

    /// <summary>
    /// Başlık satırını ve veri satırlarını ayırır. Boş dosya HATA'dır —
    /// "başlıksız CSV" tahmin edilmez (Anayasa md.1).
    /// </summary>
    public static CsvTablo Ayristir(string metin)
    {
        var satirlar = HamSatirlar(metin).ToList();
        if (satirlar.Count == 0)
            throw new Common.BadRequestException("CSV dosyası boş — başlık satırı bulunamadı");

        var basliklar = satirlar[0].Select(b => b.Trim()).ToArray();
        return new CsvTablo(basliklar, satirlar.Skip(1).ToList());
    }

    /// <summary>
    /// Başlık kapısı: ZORUNLU kolon eksikse dosya reddedilir (HATA), tanınmayan
    /// kolon yalnız UYARI'dır (rakip programın fazladan kolonu göçü durdurmaz).
    /// <para>
    /// ⚠ Bu kontrol kaldırılırsa eksik zorunlu kolon sessizce boş değer olarak
    /// akar ve yarım kart üretir — FAIL-CLOSED 1'in ihlali.
    /// </para>
    /// </summary>
    public static (string[] Eksik, string[] Bilinmeyen) BasliklariDenetle(
        IReadOnlyList<string> basliklar, IReadOnlyList<IceAktarimKolonu> kolonlar)
    {
        var gelen = new HashSet<string>(basliklar, StringComparer.OrdinalIgnoreCase);
        var bilinen = new HashSet<string>(kolonlar.Select(k => k.Ad), StringComparer.OrdinalIgnoreCase);

        var eksik = kolonlar.Where(k => k.Zorunlu && !gelen.Contains(k.Ad)).Select(k => k.Ad).ToArray();
        var bilinmeyen = basliklar.Where(b => b.Length > 0 && !bilinen.Contains(b)).ToArray();
        return (eksik, bilinmeyen);
    }

    // ── Alan okuyucuları: kültürden BAĞIMSIZ ─────────────────────────────────

    /// <summary>
    /// Boşsa null döner. Sayı çözülemezse <c>false</c>. KART kaynakları (Faz A) da KATI okur:
    /// binlik ayracı yok, yalnız NOKTA ondalık — '12,5' eskiden NumberStyles.Number ile 125
    /// okunup ürün kartına (ve oradan Faz B açılış stokuyla deftere) 10–100 kat yazılıyordu
    /// (GOC-47B kapanış hasım bulgusu, KRİTİK). Tek kural: <see cref="KatiSayiOku"/>.
    /// </summary>
    public static bool SayiOku(string? ham, out decimal? deger) => KatiSayiOku(ham, out deger);

    /// <summary>
    /// DEFTERE YAZAN kaynaklar (açılış bakiyesi / açılış stoku) için KATI sayı okuyucu.
    /// Binlik ayracı KABUL EDİLMEZ (yalnız baştaki işaret + NOKTA ondalık): '1,5' ya da
    /// '1500,50' gibi virgül içeren her hücre <c>false</c> döner — <see cref="SayiOku"/>
    /// (NumberStyles.Number = AllowThousands) bunları 15 / 150050 okuyup deftere 10–100 kat
    /// tutar yazardı (GOC-47B hasım bulgusu, KRİTİK). Boşsa null döner.
    /// </summary>
    public static bool KatiSayiOku(string? ham, out decimal? deger)
    {
        deger = null;
        if (string.IsNullOrWhiteSpace(ham)) return true;
        if (!decimal.TryParse(ham.Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var d))
            return false;
        deger = d;
        return true;
    }

    /// <summary>
    /// BİNLİK AYRACINA BENZEYEN NOKTA kalıbı (GOC-47B tur 3, orkestratör ilkesi:
    /// belirsiz sayı ASLA sessizce yorumlanmaz — GecersizAlanKapisi ile aynı ilke).
    /// '45.000', '1.500', '2.500.000': Türkçe alışkanlıkla "kırk beş bin" de olabilir,
    /// Invariant okumayla "kırk beş" de; ikisinden birini seçmek 1000 kat hata riskidir.
    /// Kalıp: baştaki isteğe bağlı işaret + 1-9 ile başlayan 1–3 hane + en az bir
    /// (nokta + TAM 3 hane) grubu. '0.916' (0 ile başlar), '45.5', '1.25', '13.9232'
    /// (grup 3 hane değil) ve '45000' (nokta yok) kalıba UYMAZ → geçerli.
    /// </summary>
    private static readonly Regex BinlikBenzeriKalip =
        new(@"^[+-]?[1-9]\d{0,2}(\.\d{3})+$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    /// <summary>Hücre binlik ayracına benzeyen noktalı sayıysa <c>true</c> (SAYI_BELIRSIZ).</summary>
    public static bool BinlikBelirsizMi(string? ham) =>
        !string.IsNullOrWhiteSpace(ham) && BinlikBenzeriKalip.IsMatch(ham.Trim());

    /// <summary>Belirsiz sayının satır hata kodu ve kullanıcıya söylenen doğru yazım.</summary>
    public const string SayiBelirsizKodu = "SAYI_BELIRSIZ";

    public static string SayiBelirsizMesaji(string kolon, string ham) =>
        $"{kolon}: '{ham.Trim()}' belirsiz. Binlik ayracı kullanmayın; 45000 ya da 45.5 yazın.";

    /// <summary>Boşsa null döner. ISO-8601 (UTC) bekler.</summary>
    public static bool TarihOku(string? ham, out DateTime? deger)
    {
        deger = null;
        if (string.IsNullOrWhiteSpace(ham)) return true;
        if (!DateTime.TryParse(ham.Trim(), CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t))
            return false;
        deger = t;
        return true;
    }

    /// <summary>EVET/HAYIR · true/false · 1/0. Boşsa null.</summary>
    public static bool EvetOku(string? ham, out bool? deger)
    {
        deger = null;
        if (string.IsNullOrWhiteSpace(ham)) return true;
        switch (ham.Trim().ToUpperInvariant())
        {
            case "EVET" or "TRUE" or "1" or "E": deger = true; return true;
            case "HAYIR" or "FALSE" or "0" or "H": deger = false; return true;
            default: return false;
        }
    }
}
