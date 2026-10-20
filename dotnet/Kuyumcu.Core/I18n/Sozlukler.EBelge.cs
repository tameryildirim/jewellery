namespace Kuyumcu.Core.I18n;

// E-belge (20.10) — İşletme › e-Fatura bağlantısı çekmecesi ve faturalar listesindeki e-belge durumu (TR + EN).
public static class EBelgeSozlugu
{
    private static bool En(string dil) => dil == "en";

    public static string Baslik(string dil) => En(dil) ? "e-Invoice connection" : "e-Fatura bağlantısı";
    public static string BagliDegil(string dil) => En(dil) ? "Not connected — invoices are not sent" : "Bağlı değil — faturalar gönderilmiyor";
    public static string Bagli(string saglayici, string ortam, string dil) => En(dil)
        ? $"{saglayici} · {(ortam == "CANLI" ? "live" : "test")}" : $"{saglayici} · {(ortam == "CANLI" ? "canlı" : "deneme")}";
    public static string Kapali(string dil) => En(dil) ? "sending paused" : "gönderim durduruldu";
    public static string Bagla(string dil) => En(dil) ? "Connect" : "Bağla";
    public static string Duzenle(string dil) => En(dil) ? "Edit" : "Düzenle";
    public static string Entegrator(string dil) => En(dil) ? "Provider" : "Entegratör";
    public static string Ortam(string dil) => En(dil) ? "Environment" : "Ortam";
    public static string OrtamTest(string dil) => En(dil) ? "Test" : "Deneme";
    public static string OrtamCanli(string dil) => En(dil) ? "Live" : "Canlı";
    public static string Anahtar(string dil) => En(dil) ? "API key" : "API anahtarı";
    public static string AnahtarKayitli(string dil) => En(dil) ? "Saved — leave empty to keep" : "Kayıtlı — değiştirmeyecekseniz boş bırakın";
    public static string AnahtarIpucu(string dil) => En(dil)
        ? "Create it in the provider's panel and paste it here." : "Entegratörün panelinden üretip buraya yapıştırın.";
    public static string SeriOnEki(string dil) => En(dil) ? "Series prefix (3 letters)" : "Seri ön eki (3 harf)";
    public static string DigerAyarlar(string dil) => En(dil) ? "Other settings" : "Diğer ayarlar";
    public static string GonderimAcik(string dil) => En(dil) ? "Send invoices automatically" : "Faturaları kendiliğinden gönder";
    public static string KaydetDene(string dil) => En(dil) ? "Save and test" : "Kaydet ve dene";
    public static string Calisiyor(string dil) => En(dil) ? "Connection works." : "Bağlantı çalışıyor.";
    public static string Kaldir(string dil) => En(dil) ? "Remove connection" : "Bağlantıyı kaldır";
    public static string KaldirOnay(string dil) => En(dil) ? "Remove? The saved key is deleted." : "Kaldırılsın mı? Kayıtlı anahtar silinir.";
    public static string EvetKaldir(string dil) => En(dil) ? "Yes, remove" : "Evet, kaldır";
    public static string NasilBaglanir(string dil) => En(dil) ? "How to connect" : "Nasıl bağlanır";
    public static string NasilMetni(string dil) => En(dil)
        ? "Sign a contract with the provider and get an e-seal (company) or e-signature (sole proprietor). Create an API key in its panel and paste it here. From then on every sale invoice is sent in the background."
        : "Entegratörle sözleşme yapın, mali mühür (şirket) ya da e-imza (şahıs) alın. Panelinden API anahtarı üretip buraya yapıştırın. Bundan sonra her satış faturası arka planda gönderilir.";

    // ── Faturalar listesi ──
    public static string Durum(string durum, string? senaryo, string dil)
    {
        var tur = senaryo == "EFATURA" ? "e-Fatura" : senaryo == "EARSIV" ? (En(dil) ? "e-Archive" : "e-Arşiv") : "e-belge";
        return durum switch
        {
            "KUYRUKTA" => En(dil) ? $"{tur}: queued" : $"{tur}: sırada",
            "GONDERILDI" => En(dil) ? $"{tur}: sent" : $"{tur}: gönderildi",
            "KABUL" => En(dil) ? $"{tur}: accepted" : $"{tur}: GİB onayladı",
            "HATA" => En(dil) ? $"{tur}: not sent" : $"{tur}: gönderilemedi",
            "RET" => En(dil) ? $"{tur}: rejected" : $"{tur}: alıcı reddetti",
            "IPTAL" => En(dil) ? $"{tur}: cancelled" : $"{tur}: iptal",
            _ => durum,
        };
    }
    public static string YenidenDene(string dil) => En(dil) ? "Retry" : "Yeniden dene";
    public static string Pdf(string dil) => En(dil) ? "e-Invoice PDF" : "e-Fatura PDF";
}
