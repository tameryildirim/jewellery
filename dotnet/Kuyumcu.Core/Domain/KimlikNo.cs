namespace Kuyumcu.Core.Domain;

/// <summary>
/// PAKET P-16 (Y-27) — TCKN / VKN AYRIMI VE HANE DOĞRULAMASI.
///
/// <para>
/// Cari kartında numara TEK kolonda (<c>vergi_no</c>) durur; hangi türde
/// olduğu <c>kimlik_turu</c> kolonunda yazar. Bu sınıf ikisinin TEK kuralıdır:
/// sunucu (DTO doğrulaması) ve ekran (alan altı mesaj) aynı kaynağı kullanır,
/// böylece "ekranda geçti, sunucuda patladı" ayrışması doğamaz.
/// </para>
///
/// <para>
/// ⚠ KAPSAM SINIRI (Anayasa md.1 — tahmin yok): burada YALNIZ <b>hane sayısı</b>
/// ve <b>rakamdan oluşma</b> doğrulanır. TCKN'nin resmî SAĞLAMA (checksum)
/// algoritması ve VKN'nin mod-11 hesabı bu pakette UYGULANMADI: PLAN P-16
/// DoD'u "TCKN 11 / VKN 10 hane doğrulaması" der, sağlama istemez; geçerli
/// ama sağlamadan geçmeyen eski kayıtları reddetmek veri girişini kilitlerdi.
/// Sağlama istenirse ayrı kalem olarak açılmalıdır.
/// </para>
/// <para>
/// ⚠ PAKET TCKN-DOGRULAMA (08.10) GÜNCELLEMESİ: kurucu emriyle TCKN doğrulama kuralı
/// artık UYGULANIR (tek kaynak <see cref="KimlikNoDogrulayici"/>); VKN yine yalnız hane.
/// </para>
/// </summary>
public static class KimlikNo
{
    /// <summary>Gerçek kişi — T.C. Kimlik Numarası, 11 hane.</summary>
    public const string Tckn = "TCKN";
    /// <summary>Tüzel kişi — Vergi Kimlik Numarası, 10 hane.</summary>
    public const string Vkn = "VKN";

    public static readonly string[] Turler = [Tckn, Vkn];

    public const string TurDesen = "TCKN|VKN";

    public static string TurMesaj =>
        $"Kimlik türü geçersiz — kabul edilen değerler: {string.Join(", ", Turler)}";

    /// <summary>Türün görünen adı — ekran ham kod basmaz (UX-S31).</summary>
    public static string TurAdi(string? tur) => tur switch
    {
        Tckn => "T.C. Kimlik No (11 hane)",
        Vkn => "Vergi Kimlik No (10 hane)",
        _ => "Belirtilmedi",
    };

    /// <summary>Bu tür kaç hane ister? Bilinmeyen/boş tür için <c>null</c>.</summary>
    public static int? BeklenenHane(string? tur) => tur switch
    {
        Tckn => 11,
        Vkn => 10,
        _ => null,
    };

    /// <summary>
    /// Numaranın uzunluğundan türü ÇIKARIR — tür beyan edilmemiş eski kayıtlar
    /// için. Uzunluk ikisine de uymuyorsa <c>null</c> döner (uydurmaz).
    /// </summary>
    public static string? TurTuret(string? no) => (no?.Trim().Length) switch
    {
        11 => Tckn,
        10 => Vkn,
        _ => null,
    };

    /// <summary>
    /// Numara + tür ikilisini doğrular. <c>null</c> = geçerli, aksi hâlde
    /// kullanıcıya gösterilecek TÜRKÇE mesaj döner.
    /// <para>
    /// Boş numara GEÇERLİDİR ("beyan yok" hâli); tür beyan edilmişse ve numara
    /// boşsa yine geçerlidir — tür tek başına kimseyi bağlamaz.
    /// </para>
    /// </summary>
    public static string? Dogrula(string? no, string? tur)
    {
        var n = no?.Trim();
        if (string.IsNullOrEmpty(n)) return null;

        if (!n.All(char.IsDigit))
            return "Kimlik/vergi numarası yalnız rakamlardan oluşmalıdır";

        // Tür beyan edilmemişse uzunluktan türetilir; ikisine de uymuyorsa hata.
        var etkinTur = string.IsNullOrEmpty(tur) ? TurTuret(n) : tur;
        var beklenen = BeklenenHane(etkinTur);

        if (beklenen is null)
            return "Numara 10 hane (Vergi Kimlik No) ya da 11 hane (T.C. Kimlik No) olmalıdır";

        if (n.Length != beklenen.Value)
            return etkinTur == Tckn
                ? "T.C. Kimlik No 11 hane olmalıdır"
                : "Vergi Kimlik No 10 hane olmalıdır";

        // PAKET TCKN-DOGRULAMA (08.10, kurucu emri): TCKN doğrulama kuralı TEK kaynaktan.
        // VKN için birincil kaynakta formül yok → yalnız hane (TEYİTSİZ).
        if (etkinTur == Tckn && !KimlikNoDogrulayici.TcknGecerli(n))
            return KimlikNoDogrulayici.TcknGecersizMesaj;

        return null;
    }

    /// <summary>Tür değeri kabul edilir mi? Boş = beyan yok, geçerli.</summary>
    public static bool TurGecerli(string? tur) =>
        string.IsNullOrEmpty(tur) || Turler.Contains(tur);
}

/// <summary>
/// PAKET P-16 (Y-27) — cari segmenti. Ham kod ekrana basılmaz (UX-S31);
/// liste TEK kaynaktan üretilir (UX-S26: oluşturma ve düzenleme formu aynı diziyi okur).
/// </summary>
public static class CariSegmentleri
{
    public const string Bireysel = "BIREYSEL";
    public const string Kurumsal = "KURUMSAL";
    public const string Toptan = "TOPTAN";
    public const string Vip = "VIP";

    public static readonly string[] Hepsi = [Bireysel, Kurumsal, Toptan, Vip];

    public const string Desen = "BIREYSEL|KURUMSAL|TOPTAN|VIP";

    public static string Mesaj =>
        $"Segment geçersiz — kabul edilen değerler: {string.Join(", ", Hepsi)}";

    public static bool Gecerli(string? s) => string.IsNullOrEmpty(s) || Hepsi.Contains(s);
}
