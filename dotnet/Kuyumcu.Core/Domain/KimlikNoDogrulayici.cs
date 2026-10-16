namespace Kuyumcu.Core.Domain;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET TCKN-DOGRULAMA (08.10, kurucu bulgusu: Değişim satıcı TCKN'ye 11111111111
// yazılabiliyordu) — KİMLİK NUMARASI DOĞRULAMASININ TEK KAYNAĞI.
//
// Sunucu (Cari, İşletme kimliği, Gider pusulası / hurda / değişim satıcısı,
// MASAK kimlik tespiti, Kıymetli evrak borçlusu) ve ekran (alan altı anında hata,
// Kaydet kilidi sebebi) AYNI yöntemi çağırır → "ekranda geçti, sunucuda patladı"
// ayrışması doğamaz.
//
// KAYNAK NOTU (MEVZUAT KURALI — rapor PAKET-TCKN-DOGRULAMA § Kaynaklar):
//  • TCKN: NVİ resmî sayfası — "T.C. Kimlik Numarası 11 basamaktan oluşan bir
//    numaradır. Son iki rakamı doğrulama sayısıdır." (nvi.gov.tr). NVİ formülü
//    yayımlamaz; uygulanan formül kamuya açık yerleşik formüldür (bu projede
//    GiderPusulasiKapisi.TcknGecerli'de 2026-09'dan beri kullanılan kural) ve
//    kurucu emriyle (08.10) tüm giriş noktalarına yayıldı:
//      10. hane = (7 × (1.+3.+5.+7.+9. haneler) − (2.+4.+6.+8. haneler)) mod 10
//      11. hane = (ilk 10 hanenin toplamı) mod 10 ; 1. hane 0 olamaz.
//    ⚠ Eski uygulamada C# '%' negatif kalan veriyordu (7×tek − çift < 0 iken
//    GEÇERLİ numara reddediliyordu, örn. 19090909018). Burada matematiksel mod.
//  • YKN (yabancı kimlik no, 99 ile başlar): aynı 11 hane yapısı; ayrı bir
//    formül birincil kaynakta bulunamadı → TCKN kuralı uygulanır (görev hükmü).
//  • VKN (10 hane): GİB'in yayımladığı birincil bir kontrol hanesi formülü
//    bulunamadı → yalnız 10 hane + ASCII rakam (TEYİTSİZ: sağlama uygulanmaz).
//
// ⚠ '\d' / char.IsDigit KULLANILMAZ: Unicode rakamlarını (Arapça-Hint vb.) da eşler.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Kimlik numarası denetiminin sonucu.</summary>
public enum KimlikNoHatasi
{
    /// <summary>Geçerli ya da boş (boş = beyan yok; zorunluluğu çağıran söyler).</summary>
    Yok = 0,
    /// <summary>Rakam dışı karakter var.</summary>
    RakamDisi = 1,
    /// <summary>Yalnız rakam ama hane sayısı beklenene uymuyor.</summary>
    HaneSayisi = 2,
    /// <summary>11 hane rakam ama TC kimlik no doğrulama kuralından geçmiyor (ilk hane 0 dahil).</summary>
    TcknGecersiz = 3,
}

/// <summary>TCKN / YKN / VKN doğrulamasının tek kaynağı (sunucu + ekran).</summary>
public static class KimlikNoDogrulayici
{
    /// <summary>Sunucu mesajı (sade Türkçe) — ekran aynı metni sözlükten (TR/EN) basar.</summary>
    public const string TcknGecersizMesaj = "TC kimlik numarası geçerli değil";

    /// <summary>Yalnız ASCII rakam mı (boş değil)?</summary>
    public static bool YalnizRakam(string s)
    {
        if (s.Length == 0) return false;
        foreach (var c in s)
            if (c is < '0' or > '9') return false;
        return true;
    }

    /// <summary>Metindeki ASCII rakamlar (boşluk/tire kullanıcıyı engellemesin diye ayıklayan ekranlar için).</summary>
    public static string Rakamlar(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var b = new System.Text.StringBuilder(s.Length);
        foreach (var c in s)
            if (c is >= '0' and <= '9') b.Append(c);
        return b.ToString();
    }

    /// <summary>
    /// TC kimlik no (ve aynı yapıdaki YKN) doğrulama kuralı: 11 ASCII rakam, ilk hane 0 değil,
    /// 10. ve 11. hane kontrol haneleri tutuyor. Boş/null → false.
    /// </summary>
    public static bool TcknGecerli(string? no)
    {
        if (no is null || no.Length != 11 || !YalnizRakam(no) || no[0] == '0') return false;
        Span<int> h = stackalloc int[11];
        for (var i = 0; i < 11; i++) h[i] = no[i] - '0';
        var tek = h[0] + h[2] + h[4] + h[6] + h[8];
        var cift = h[1] + h[3] + h[5] + h[7];
        // Matematiksel mod: C# '%' negatif sayıda negatif kalan verir.
        var onuncu = ((tek * 7 - cift) % 10 + 10) % 10;
        if (onuncu != h[9]) return false;
        var toplam = 0;
        for (var i = 0; i < 10; i++) toplam += h[i];
        return toplam % 10 == h[10];
    }

    /// <summary>Yabancı kimlik numarası mı (99 ile başlayan, TCKN kuralından geçen 11 hane)?</summary>
    public static bool YknMi(string? no) => TcknGecerli(no) && no!.StartsWith("99", StringComparison.Ordinal);

    /// <summary>VKN biçimi: 10 ASCII rakam. ⚠ TEYİTSİZ — kontrol hanesi uygulanmaz (birincil kaynak yok).</summary>
    public static bool VknBicimGecerli(string? no) => no is { Length: 10 } && YalnizRakam(no);

    /// <summary>
    /// TCKN alanı denetimi (yalnız 11 hane kabul edilen alanlar). Boş → <see cref="KimlikNoHatasi.Yok"/>.
    /// Baştaki/sondaki boşluk kırpılır.
    /// </summary>
    public static KimlikNoHatasi TcknDenetle(string? no)
    {
        if (string.IsNullOrWhiteSpace(no)) return KimlikNoHatasi.Yok;
        var n = no.Trim();
        if (!YalnizRakam(n)) return KimlikNoHatasi.RakamDisi;
        if (n.Length != 11) return KimlikNoHatasi.HaneSayisi;
        return TcknGecerli(n) ? KimlikNoHatasi.Yok : KimlikNoHatasi.TcknGecersiz;
    }

    /// <summary>
    /// TCKN ya da VKN kabul eden alan (türü uzunluktan anlaşılır): 10 hane = VKN (biçim),
    /// 11 hane = TCKN/YKN (kural). Boş → <see cref="KimlikNoHatasi.Yok"/>.
    /// </summary>
    public static KimlikNoHatasi TcknVeyaVknDenetle(string? no)
    {
        if (string.IsNullOrWhiteSpace(no)) return KimlikNoHatasi.Yok;
        var n = no.Trim();
        if (!YalnizRakam(n)) return KimlikNoHatasi.RakamDisi;
        return n.Length switch
        {
            10 => KimlikNoHatasi.Yok,
            11 => TcknGecerli(n) ? KimlikNoHatasi.Yok : KimlikNoHatasi.TcknGecersiz,
            _ => KimlikNoHatasi.HaneSayisi,
        };
    }
}
