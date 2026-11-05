namespace Kuyumcu.Core.I18n;

// D2-05b (19.10) — banka hesabı çekmecesi › "Bankaya bağla" bölümü (TR + EN).
public static class BankaBaglantiSozlugu
{
    private static bool En(string dil) => dil == "en";

    public static string Baslik(string dil) => En(dil) ? "Bank connection" : "Banka bağlantısı";
    public static string BagliDegil(string dil) => En(dil)
        ? "Not connected. Payments are entered by hand or from a statement file."
        : "Bağlı değil. Ödemeler elle ya da ekstre dosyasından girilir.";
    public static string Bagla(string dil) => En(dil) ? "Connect to bank" : "Bankaya bağla";
    public static string Banka(string dil) => En(dil) ? "Bank" : "Banka";
    public static string BankaSecin(string dil) => En(dil) ? "Choose bank" : "Banka seçin";
    public static string BaglanVeDene(string dil) => En(dil) ? "Save and test" : "Kaydet ve dene";
    public static string Vazgec(string dil) => En(dil) ? "Cancel" : "Vazgeç";
    public static string SimdiDene(string dil) => En(dil) ? "Test now" : "Şimdi dene";
    public static string BilgileriDegistir(string dil) => En(dil) ? "Change details" : "Bilgileri değiştir";
    public static string Kaldir(string dil) => En(dil) ? "Remove connection" : "Bağlantıyı kaldır";
    public static string KaldirOnay(string dil) => En(dil) ? "Remove? Saved credentials are deleted." : "Kaldırılsın mı? Kayıtlı bilgiler silinir.";
    public static string EvetKaldir(string dil) => En(dil) ? "Yes, remove" : "Evet, kaldır";
    public static string Bagli(string banka, string dil) => En(dil) ? $"Connected to {banka}" : $"{banka} bağlı";
    public static string SonAlis(string zaman, string dil) => En(dil) ? $"last received {zaman}" : $"son alış {zaman}";
    public static string HenuzDenenmedi(string dil) => En(dil) ? "not tested yet" : "henüz denenmedi";
    /// <summary>Kurucu 05.11: başarısız deneme "henüz denenmedi" gibi görünmesin — son deneme zamanı yazılır.</summary>
    public static string SonDeneme(string zaman, string dil) => En(dil) ? $"last attempt {zaman}, failed" : $"son deneme {zaman}, başarısız";
    public static string DenemeBasarili(int adet, string dil) => En(dil)
        ? $"Connection works. {adet} transactions read." : $"Bağlantı çalışıyor. {adet} hareket okundu.";
    /// <summary>Kurucu 05.11: bağlıyken HER alan boş bırakılabilir (gizli ya da değil); boş = eski değer kalır.</summary>
    public static string GizliKayitli(string dil) => En(dil) ? "Saved — leave empty to keep" : "Kayıtlı — değiştirmeyecekseniz boş bırakın";
    public static string NasilAlinir(string dil) => En(dil) ? "How to get these details" : "Bu bilgiler nereden alınır";
    public static string SunucuAdresi(string ip, string dil) => En(dil)
        ? $"Tell the bank this server address: {ip}" : $"Bankaya bu sunucu adresini bildirin: {ip}";
    public static string SunucuAdresiYok(string dil) => En(dil)
        ? "The bank asks for a fixed server address; ask " + Marka.Ad + " support for it."
        : "Banka sabit sunucu adresi ister; adresi " + Marka.Ad + " destekten isteyin.";
    public static string SifreUyari(string dil) => En(dil)
        ? "Enter only the service details the bank gives for this program. Never your internet banking password."
        : "Yalnız bankanın bu program için verdiği servis bilgilerini girin. İnternet şubesi şifrenizi asla girmeyin.";
}
