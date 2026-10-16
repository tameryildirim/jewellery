namespace Kuyumcu.Core.Domain;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET Q10-3 — İşletme VKN / TCKN BİÇİM kuralı (TEK KAYNAK: sunucu + ekran).
//
// ⚠ YALNIZ BİÇİM: 10 hane (VKN) ya da 11 hane (TCKN), yalnız ASCII rakam.
//   SAĞLAMA (kontrol hanesi) algoritması UYGULANMAZ — MEVZUAT KURALI:
//   NVİ'nin resmî sayfası algoritmanın varlığını söyler ama formülü yayımlamaz
//   ("son iki basamak ilk dokuz basamaktan bir algoritma ile hesaplanmaktadır",
//   https://www.nvi.gov.tr/tokat/tc-kimlik-no-islemleri); VKN için GİB'in
//   yayımladığı birincil bir formül bulunamadı. Doğrulanamayan hüküm koda
//   sabit yazılmaz → mali müşavir / orkestratör notu (PAKET-Q10-3 raporu).
//
// ⚠ '\d' KULLANILMAZ: .NET'te '\d' Unicode rakamlarını da (Arapça-Hint
//   '١٢٣…') eşler; eski kural bunları 200 ile kabul ediyordu. Kural [0-9].
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>VKN/TCKN biçim hatasının türü. Ekran bu türden kendi dilinde mesaj üretir.</summary>
public enum VergiKimlikHatasi
{
    /// <summary>Geçerli ya da boş (boş = alanı silmek).</summary>
    Yok = 0,
    /// <summary>Rakam dışı karakter var (harf, iç boşluk, tire, Unicode rakam…).</summary>
    RakamDisi = 1,
    /// <summary>Yalnız rakam ama hane sayısı 10 ya da 11 değil.</summary>
    HaneSayisi = 2,
    /// <summary>PAKET TCKN-DOGRULAMA: 11 hane ama TC kimlik no doğrulama kuralından geçmiyor.</summary>
    TcknGecersiz = 3,
}

/// <summary>İşletme vergi kimliği biçim kuralı (VKN 10 hane / TCKN 11 hane).</summary>
public static class VergiKimlik
{
    /// <summary>DataAnnotations deseni — yalnız ASCII rakam, 10 ya da 11 hane.</summary>
    public const string Desen = "^[0-9]{10,11}$";

    public const string RakamDisiMesaj =
        "Vergi no / TC kimlik no yalnız rakamlardan oluşmalıdır (harf, boşluk ya da işaret olamaz)";

    public const string HaneMesaj =
        "Vergi no 10 hane, TC kimlik no 11 hane olmalıdır";

    /// <summary>
    /// Biçim denetimi. Boş/boşluk = alan verilmedi ya da siliniyor → <see cref="VergiKimlikHatasi.Yok"/>.
    /// Baştaki/sondaki boşluk kırpılır (sunucu da kırparak yazar); İÇ boşluk hatadır.
    /// </summary>
    public static VergiKimlikHatasi Denetle(string? deger)
    {
        if (string.IsNullOrWhiteSpace(deger)) return VergiKimlikHatasi.Yok;
        var d = deger.Trim();
        foreach (var c in d)
            if (c is < '0' or > '9') return VergiKimlikHatasi.RakamDisi;
        if (d.Length is not (10 or 11)) return VergiKimlikHatasi.HaneSayisi;
        // PAKET TCKN-DOGRULAMA (kurucu emri 08.10): 11 hane = şahıs işletmesi TCKN → doğrulama kuralı
        // TEK kaynaktan. 10 hane VKN: birincil kaynakta formül yok → yalnız biçim (TEYİTSİZ).
        return d.Length == 11 && !KimlikNoDogrulayici.TcknGecerli(d)
            ? VergiKimlikHatasi.TcknGecersiz : VergiKimlikHatasi.Yok;
    }

    /// <summary>Sunucu mesajı (Türkçe) — null = geçerli.</summary>
    public static string? Mesaj(string? deger) => Denetle(deger) switch
    {
        VergiKimlikHatasi.RakamDisi => RakamDisiMesaj,
        VergiKimlikHatasi.HaneSayisi => HaneMesaj,
        VergiKimlikHatasi.TcknGecersiz => KimlikNoDogrulayici.TcknGecersizMesaj,
        _ => null,
    };
}

/// <summary>
/// Q10-3 (doğrulayıcı bulgusu) — işletme adı uzunluk kuralı TEK kaynaktan.
/// Uzunluk KIRPILMIŞ metinde ölçülür: " x " tek karakterli bir ad olarak
/// kaydedilemez. Sunucu (AdminServisi.KimlikGuncelle) ve ekran (Isletme.razor)
/// aynı yöntemi çağırır.
/// </summary>
public static class IsletmeAdi
{
    public const int EnAz = 2;
    public const int EnCok = 200;
    public const string Mesaj = "İşletme adı 2-200 karakter olmalıdır";

    /// <summary>Kırpılmış uzunluk [EnAz, EnCok] içindeyse true; boş/null false.</summary>
    public static bool Gecerli(string? ad)
    {
        var k = ad?.Trim() ?? "";
        return k.Length is >= EnAz and <= EnCok;
    }
}
