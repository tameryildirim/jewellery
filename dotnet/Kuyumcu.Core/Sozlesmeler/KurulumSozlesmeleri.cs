namespace Kuyumcu.Core.Sozlesmeler;

// ═════════════════════════════════════════════════════════════════════════════
// KURULUŞ SİHİRBAZI + AÇILIŞ BAKİYELERİ — istemci sözleşmeleri (paket T-2 Faz A)
//
// ⚠ Pages altına elle yanıt modeli YAZILMAZ (S-2 / MÜHÜR 5 KOŞUL 3 · denetim.sh
//   sayacı). Yanıt şekli burada TEK kopya durur.
//
// Alan adları sunucunun camelCase JSON'una birebir karşılık gelir:
//   Modules/Kurulum/KurulumModulu.cs (Durum · Muhurle) ·
//   Modules/Kurulum/AcilisServisi.cs (Prova · Uygula · GeriAl).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>GET /kurulum/durum — tek adımın durumu.</summary>
public sealed record KurulumAdimBilgisi(string Anahtar, bool Bitti, bool Zorunlu, bool Atlandi);

/// <summary>GET /kurulum/durum — sayı rozetleri (UX-S23: her sayı bir kayıt kümesidir).</summary>
public sealed record KurulumSayilari(
    int Sube, int Kullanici, int Kur, int Kasa, int Banka,
    int AcilisCari, int AcilisKasa, int AcilisBanka);

/// <summary>GET /kurulum/durum zarfı — sihirbazın TEK gerçeği.</summary>
public sealed record KurulumDurumYaniti(
    bool Muhurlendi, DateTime? MuhurTarihi, string? MuhurleyenKullaniciId, string? MuhurGerekcesi,
    DateTime AcilisPenceresiBitis, bool AcilisPenceresiAcik, bool Muhurlenebilir,
    List<string>? EksikAdimlar, List<string>? AtlananAdimlar,
    List<KurulumAdimBilgisi>? Adimlar, KurulumSayilari? Sayilar,
    int MaksSatir, int GerekceAsgari, string? IsletmeAdi = null,
    List<string>? AcilisParaBirimleri = null)
{
    public List<string> EksikListe => EksikAdimlar ?? [];
    public List<string> AtlananListe => AtlananAdimlar ?? [];
    public List<KurulumAdimBilgisi> AdimListesi => Adimlar ?? [];
    public KurulumSayilari SayiKutusu => Sayilar ?? new(0, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>
    /// P-68: açılışın kabul ettiği para birimleri (sunucu: AcilisServisi.KabulEdilenParaBirimleri).
    /// <c>null</c> = sunucu bildirmedi → ekran süzmez (sabit liste UYDURMAZ); sunucu provada reddeder.
    /// </summary>
    public bool ParaBirimiKabul(string? paraBirimi) =>
        AcilisParaBirimleri is null
        || AcilisParaBirimleri.Contains(paraBirimi ?? "", StringComparer.Ordinal);
}

/// <summary>Satır bazlı hata/uyarı — kod ekranda sözlükten çevrilir (UX-S31).</summary>
public sealed record AcilisSatirSonucu(int SatirNo, string HataKodu, string HataMesaji)
{
    /// <summary>
    /// PAKET P-13 — kullanıcıya gösterilen HATA KİMLİĞİ.
    /// <para>
    /// Ham iç kod (<c>DOVIZ_HESAP_FAZ_B</c>, <c>NEGATIF_ACILIS</c>) ekrana
    /// BASILMAZ: kuyumcuya hiçbir şey anlatmaz, üstelik iç faz adlarını sızdırır.
    /// Kullanıcı Türkçe mesajı okur, destekle konuşurken bu KISA kimliği söyler.
    /// Kodun kendisi sunucu yanıtında ve denetim izinde durmaya devam eder.
    /// </para>
    /// </summary>
    public string Kimlik => AcilisHataKimligi.Kimlik(HataKodu);
}

/// <summary>
/// İç hata kodu → kullanıcıya gösterilen kısa kimlik. TEK KAYNAK.
/// <para>
/// ⚠ Haritanın <c>AcilisHataKodu</c> / <c>AcilisUyariKodu</c> sabitlerinin
/// TAMAMINI kapsadığı testle kilitlidir (yeni kod eklenip haritaya
/// yazılmazsa test kırmızıya döner, ekran "AC-00" göstermez).
/// </para>
/// </summary>
public static class AcilisHataKimligi
{
    /// <summary>Bilinmeyen kod — haritaya yazılmamış demektir.</summary>
    public const string Bilinmeyen = "AC-00";

    private static readonly Dictionary<string, string> Harita = new(StringComparer.Ordinal)
    {
        ["HEDEF_YOK"] = "AC-01",
        ["HEDEF_BULUNAMADI"] = "AC-02",
        ["SIFIR_SATIR"] = "AC-03",
        ["HAS_BACAGI_YOK"] = "AC-04",
        ["YINELENEN_SATIR"] = "AC-05",
        ["ZATEN_ACILIS"] = "AC-06",
        ["DOVIZ_HESAP_FAZ_B"] = "AC-07",
        ["TUTAR_ASIRI"] = "AC-08",
        ["PASIF_HEDEF"] = "AC-09",
        ["NEGATIF_ACILIS"] = "AC-10",
        ["KREDI_LIMITI_ASIMI"] = "AC-11",
    };

    public static string Kimlik(string? kod) =>
        kod is not null && Harita.TryGetValue(kod, out var k) ? k : Bilinmeyen;

    /// <summary>Testin kapsam denetimi için haritadaki kodlar.</summary>
    public static IReadOnlyCollection<string> Kodlar => Harita.Keys;
}

/// <summary>
/// POST /kurulum/acilis/prova yanıtı.
/// <para>
/// ⚠ <c>ProvaJetonu</c> YALNIZ temiz provada doludur: hata varken jeton
/// gelmez ve "Uygula" düğmesi sunucu tarafından da kilitlenmiş olur.
/// </para>
/// </summary>
public sealed record AcilisProvaYaniti(
    string Tur, bool Temiz, string? ProvaJetonu,
    int ToplamSatir, int HataliSatir, int UygulanacakSatir, int UyariliSatir,
    bool LimitAsimiOnayiGerekli, decimal TlToplam, decimal HasToplam, bool KisiselVeriIzi,
    List<AcilisSatirSonucu>? Hatalar, List<AcilisSatirSonucu>? Uyarilar)
{
    public List<AcilisSatirSonucu> HataListesi => Hatalar ?? [];
    public List<AcilisSatirSonucu> UyariListesi => Uyarilar ?? [];
}

/// <summary>POST /kurulum/acilis/uygula yanıtı — banda yazılan sonuç (UX-S27).</summary>
public sealed record AcilisUygulaYaniti(
    string OturumId, string Tur, int UygulananSatir,
    decimal TlToplam, decimal HasToplam, bool GeriAlinabilir);

/// <summary>POST /kurulum/acilis/geri-al yanıtı.</summary>
public sealed record AcilisGeriAlYaniti(string OturumId, int GeriAlinan);
