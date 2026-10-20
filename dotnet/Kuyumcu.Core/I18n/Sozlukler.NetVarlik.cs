namespace Kuyumcu.Core.I18n;

// PAKET-37 (20.10) — TL net varlık (servet) özeti: altın pozisyonu kartının ayrıntı bölümü (TR + EN).
// Jargon sıfır: "net varlık" yerine gündelik "bugünkü servet"; muhasebe öz sermayesi olmadığı açıkça yazılır.
public static class NetVarlikSozlugu
{
    private static bool En(string dil) => dil == "en";

    public static string Baslik(string dil) => En(dil) ? "Today's net worth" : "Bugünkü servet";
    public static string Nakit(string dil) => En(dil) ? "Cash (till + bank)" : "Nakit (kasa + banka)";
    public static string KasaKisa(string dil) => En(dil) ? "till" : "kasa";
    public static string BankaKisa(string dil) => En(dil) ? "bank" : "banka";
    public static string AltinDegeri(string dil) => En(dil) ? "Gold (net position × fine price)" : "Altın (net pozisyon × has fiyatı)";
    public static string CariBakiye(string dil) => En(dil) ? "Customer / supplier balances (TL)" : "Müşteri / tedarikçi bakiyeleri (TL)";
    public static string Toplam(string dil) => En(dil) ? "Total" : "Toplam";
    public static string HasFiyatiNotu(string fiyat, string dil) => En(dil) ? $"fine gold price used: {fiyat}" : $"kullanılan has fiyatı: {fiyat}";
    public static string Not(string dil) => En(dil)
        ? "Informational — gold at fine value (not cost); not an accounting equity figure."
        : "Bilgi amaçlıdır — altın has değeriyle (maliyet değil); muhasebe öz sermayesi değildir.";
    public static string SonKapanis(string gun, string dil) => En(dil) ? $"Change since last day close ({gun})" : $"Son gün kapanışına göre ({gun}) fark";
    public static string HasFarki(string dil) => En(dil) ? "fine gold" : "has";
    public static string HesaplanamadiBaslik(string dil) => En(dil) ? "Total could not be calculated:" : "Toplam hesaplanamadı:";
    public static string Eksik(string kod, string dil) => kod switch
    {
        "HAS_FIYATI" => En(dil) ? "no current fine gold price (enter it under Rates)" : "güncel has fiyatı yok (Kur ekranından girin)",
        var k when k.StartsWith("KUR_", StringComparison.Ordinal) =>
            En(dil) ? $"no {k[4..]} rate in the current rate record" : $"güncel kur kaydında {k[4..]} kuru yok",
        _ => kod,
    };
}
