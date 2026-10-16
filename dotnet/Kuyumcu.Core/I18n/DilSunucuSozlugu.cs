namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET DIL-SUNUCU — küçük ekran örnek metinleri (TR + EN)
//
// DIL-EKRAN hakem tur 2 (YÜKSEK): Banka "Yeni hesap" formunun örnek (yer tutucu) metinleri
// "Ziraat Bankası" / "Ana Hesap" çıplak Türkçeydi; EN modda Türkçe görünüyordu. Yer tutucu
// VERİ DEĞİL, arayüz metnidir → sözlükten, dile göre.
// ═════════════════════════════════════════════════════════════════════════════

/// <param name="BankaAdiOrnek">Banka adı alanının örnek metni.</param>
/// <param name="HesapAdiOrnek">Hesap adı alanının örnek metni.</param>
public sealed record DilSunucuMetinleri(string BankaAdiOrnek, string HesapAdiOrnek);

/// <summary>DIL-SUNUCU paketinin ekran metinleri.</summary>
public static class DilSunucuSozlugu
{
    public static readonly DilSunucuMetinleri Tr = new(
        BankaAdiOrnek: "Ziraat Bankası",
        HesapAdiOrnek: "Ana Hesap");

    public static readonly DilSunucuMetinleri En = new(
        BankaAdiOrnek: "Example Bank",
        HesapAdiOrnek: "Main account");

    public static DilSunucuMetinleri Al(string? dil) => dil == "en" ? En : Tr;

    /// <summary>
    /// Yedek ekranının sunucudan gelen HAM durum/tür kodlarının (BEKLIYOR, BASARILI, TAM, ELLE …) ekran adı.
    /// Tanınmayan kod AYNEN döner (ad uydurulmaz); boş kod "—". Renk/mantık kodla çalışmaya devam eder, yalnız GÖRÜNEN ad çevrilir.
    /// </summary>
    public static string KodAdi(string? kod, string? dil)
    {
        var en = dil == "en";
        return kod switch
        {
            null or "" => "—",
            "BEKLIYOR" => en ? "Pending" : "Bekliyor",
            "ISLENDI" => en ? "Processed" : "İşlendi",
            "HATA" => en ? "Error" : "Hata",
            "IPTAL" => en ? "Cancelled" : "İptal",
            "BASARILI" => en ? "Successful" : "Başarılı",
            "BASARISIZ" => en ? "Failed" : "Başarısız",
            "SURUYOR" => en ? "Running" : "Sürüyor",
            "TAM" => en ? "Full" : "Tam",
            "ELLE" => en ? "Manual" : "Elle",
            "ZAMANLI" => en ? "Scheduled" : "Zamanlı",
            _ => kod,
        };
    }
}
