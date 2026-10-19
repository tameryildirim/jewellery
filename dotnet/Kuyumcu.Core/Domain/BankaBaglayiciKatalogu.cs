namespace Kuyumcu.Core.Domain;

// ═════════════════════════════════════════════════════════════════════════════
// D2-05b (19.10, kurucu: "bankaları önceden bağlayalım") — hazır banka bağlayıcıları kataloğu.
// Her banka, işletmeye şubeden/internet şubesinden "hesap hareketleri web servisi" kullanıcısı verir
// (Paraşüt/Logo modeli). İşletme bu ekranda yalnız bankanın VERDİĞİ servis bilgilerini girer;
// internet şubesi parolası ASLA istenmez. Alan adları bağlayıcının okuduğu anahtardır.
// Kaynaklar: dokumanlar/09-uygulama-kayitlari/ARASTIRMA-BANKA-*.md
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bağlantı formunda bir alan. <paramref name="Gizli"/> alan ekrana bir daha dönmez.</summary>
public sealed record BankaAlani(string Ad, string Etiket, bool Gizli, bool Zorunlu = true, string? Ipucu = null);

/// <summary>Bir bankanın hazır bağlayıcısı.</summary>
/// <param name="Kod">Hesabın sağlayıcı kodu (en çok 20 karakter).</param>
/// <param name="BasvuruYolu">İşletmenin bankadan servis kullanıcısını nasıl alacağı — sade dil, tek cümle.</param>
public sealed record BankaBaglayiciTanimi(
    string Kod, string BankaAdi, IReadOnlyList<BankaAlani> Alanlar, string BasvuruYolu, bool IpKisitli);

public static class BankaBaglayiciKatalogu
{
    public const int AlanEnFazla = 500;

    private static BankaAlani Kullanici => new("kullanici", "Servis kullanıcı adı", false);
    private static BankaAlani Sifre => new("sifre", "Servis şifresi", true);

    private static BankaAlani HesapNo(string? ipucu = null) => new("hesapNo", "Hesap numarası", false, Ipucu: ipucu);
    private static BankaAlani MusteriNo => new("musteriNo", "Müşteri numarası", false);
    private const string Sube = "Şubeden \"hesap hareketleri web servisi\" başvurusu yapın; banka servis kullanıcı adı ve şifresini e-postayla verir.";

    // Sıra: ekranda gösterim sırası. Kaynak § numaraları ARASTIRMA-BANKA-API-TEKNIK-2026-10-19.md.
    public static readonly IReadOnlyList<BankaBaglayiciTanimi> Tumu =
    [
        new("ZIRAAT", "Ziraat Bankası", [new("kullanici", "Kurum kodu", false), Sifre, MusteriNo, new("ekNo", "Hesap ek numarası", false)], Sube, false),
        new("ISBANK", "İş Bankası", [Kullanici, Sifre], Sube, false),
        new("GARANTI", "Garanti BBVA",
            [new("consentId", "Onay numarası", false),
             new("clientId", "Uygulama kimliği", false, Zorunlu: false, Ipucu: "Boş bırakılırsa Mihenk'in kaydı kullanılır"),
             new("clientSecret", "Uygulama şifresi", true, Zorunlu: false)],
            "Garanti internet şubesinde Başvur › Elektronik Hesap Özeti ekranında Mihenk'in uygulama kimliğini girip hesabı seçin; çıkan onay numarasını buraya yazın.", false),
        new("AKBANK", "Akbank", [Kullanici, Sifre, new("urf", "Firma kodu (URF)", false), HesapNo()],
            "Şubeye başvurun, şube firma kodunu (URF) verir. Akbank Direkt › Web Servis Şifre İşlemleri › Hesap Ekstresi'nde kullanıcı tanımlayıp sunucu adresini ekleyin.", true),
        new("VAKIF", "VakıfBank", [MusteriNo, Kullanici, Sifre, HesapNo()], Sube, false),
        new("QNB", "QNB", [Kullanici, Sifre, HesapNo()], "QNB tüzel internet bankacılığından Online Ekstre tanımlayın ya da şubeye başvurun; banka kullanıcı adı ve şifre verir.", false),
        new("KUVEYT", "Kuveyt Türk", [Kullanici, Sifre, HesapNo("Müşteri no-ek no, ör. 98886207-1")], Sube, false),
        new("ALBARAKA", "Albaraka Türk", [Kullanici, Sifre, MusteriNo, HesapNo("Ek no ya da müşteri no-ek no")], Sube, false),
    ];

    public static BankaBaglayiciTanimi? Bul(string? kod) =>
        kod is null ? null : Tumu.FirstOrDefault(t => t.Kod == kod);

    /// <summary>Girilen alanları denetler: zorunlular dolu, uzunluk sınırı. Hata yoksa null.</summary>
    public static (string Alan, string Hata)? Denetle(BankaBaglayiciTanimi tanim, IReadOnlyDictionary<string, string?> alanlar)
    {
        foreach (var a in tanim.Alanlar)
        {
            alanlar.TryGetValue(a.Ad, out var d);
            if (a.Zorunlu && string.IsNullOrWhiteSpace(d)) return (a.Ad, $"{a.Etiket} zorunludur");
            if (d is not null && d.Length > AlanEnFazla) return (a.Ad, $"{a.Etiket} en çok {AlanEnFazla} karakter olabilir");
        }
        foreach (var k in alanlar.Keys)
            if (tanim.Alanlar.All(a => a.Ad != k)) return (k, "Bu banka için tanımsız alan");
        return null;
    }

    /// <summary>Gizli olmayan alanların ekran özeti; değerin yalnız ilk 2 karakteri görünür.</summary>
    public static string Ozet(BankaBaglayiciTanimi tanim, IReadOnlyDictionary<string, string?> alanlar) =>
        string.Join(" · ", tanim.Alanlar.Where(a => !a.Gizli && alanlar.TryGetValue(a.Ad, out var d) && !string.IsNullOrWhiteSpace(d))
            .Select(a => $"{a.Etiket}: {Maskele(alanlar[a.Ad]!.Trim())}"));

    public static string Maskele(string d) => d.Length <= 2 ? new string('•', d.Length) : d[..2] + new string('•', Math.Min(6, d.Length - 2));
}
