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

    // ── İşletme logosu (PAKET-04) ──
    public static string Logo(string dil) => En(dil) ? "Business logo" : "İşletme logosu";
    public static string LogoIpucu(string dil) => En(dil)
        ? "Printed on slips, invoice copies and statements. The newest image is used." : "Fiş, fatura dökümü ve ekstrelerde basılır. En yeni görsel geçerlidir.";

    // ── Zekât (pozisyon kartı) ──
    public static string ZekatBaslik(string dil) => En(dil) ? "Zakat (for information)" : "Zekât (bilgi amaçlı)";
    public static string ZekatNisapAlti(string nisap, string dil) => En(dil) ? $"Net gold is below the nisab ({nisap} g fine)." : $"Net altın nisabın altında ({nisap} g has).";
    public static string ZekatTutar(string has, string? tl, string dil) => En(dil)
        ? $"1/40 of net gold: {has} fine" + (tl is null ? "" : $" ≈ {tl}") : $"Net altının 1/40'ı: {has} has" + (tl is null ? "" : $" ≈ {tl}");
    public static string ZekatNot(string dil) => En(dil) ? "Confirm with your religious/financial adviser." : "Dinî/mali danışmanınıza teyit ettirin.";

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

    // ── Giden kutusu sayfası (/e-belgeler) ──
    public static string KutuBaslik(string dil) => En(dil) ? "e-Invoice outbox" : "e-Fatura giden kutusu";
    public static string KutuBos(string dil) => En(dil) ? "No invoice has been sent yet." : "Henüz gönderilen fatura yok.";
    public static string Alici(string dil) => En(dil) ? "Customer" : "Müşteri";
    public static string Durum(string dil) => En(dil) ? "Status" : "Durum";
    public static string BelgeNo(string dil) => En(dil) ? "Document no" : "Belge no";
    public static string Tumu(string dil) => En(dil) ? "All" : "Tümü";
    public static string OlayIzi(string dil) => En(dil) ? "History" : "Geçmiş";
    public static string DegismezGerekce(string dil) => En(dil)
        ? "Sent documents are a legal record and cannot be edited; retry a failed one, or cancel the invoice (within 8 days) / issue a return invoice."
        : "Gönderilen belge yasal izdir, düzenlenmez; gönderilemeyeni yeniden deneyin ya da faturayı iptal edin (8 gün) / iade faturası kesin.";
    public static string DahaFazla(int toplam, string dil) => En(dil) ? $"Showing the latest 200 of {toplam}." : $"{toplam} kayıttan en yeni 200'ü gösteriliyor.";
    public static string DurumAdi(string durum, string dil) => durum switch
    {
        "KUYRUKTA" => En(dil) ? "queued" : "sırada",
        "GONDERILDI" => En(dil) ? "sent" : "gönderildi",
        "KABUL" => En(dil) ? "accepted" : "onaylandı",
        "HATA" => En(dil) ? "not sent" : "gönderilemedi",
        "RET" => En(dil) ? "rejected" : "reddedildi",
        "IPTAL" => En(dil) ? "cancelled" : "iptal",
        _ => durum,
    };
    public static string Kaynak(string k, string dil) => k switch
    {
        "KUYRUK" => En(dil) ? "automatic" : "kendiliğinden",
        "ISTEK" => En(dil) ? "sending" : "gönderim",
        "SORGU" => En(dil) ? "GİB status" : "GİB sorgusu",
        "KULLANICI" => En(dil) ? "user" : "kullanıcı",
        _ => k,
    };
    public static string Pdf(string dil) => En(dil) ? "e-Invoice PDF" : "e-Fatura PDF";
}
