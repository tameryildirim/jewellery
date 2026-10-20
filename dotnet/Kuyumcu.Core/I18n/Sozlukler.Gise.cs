namespace Kuyumcu.Core.I18n;

// Y-03 (20.10, rakip Ariyen/CetaSoft) — gişe zimmeti ekranı (TR + EN). Jargon yok: "zimmet" tezgâhtarın bildiği sözcüktür.
public static class GiseSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string Baslik(string dil) => En(dil) ? "Till custody" : "Gişe zimmeti";
    public static string SayfaAciklama(string dil) => En(dil)
        ? "Morning hand-over of goods and opening cash to a clerk; evening count and transfer to the main register."
        : "Sabah tezgâhtara mal ve açılış nakdi teslimi; akşam sayım ve ana kasaya devir.";
    public static string YeniZimmet(string dil) => En(dil) ? "New custody" : "Yeni zimmet";
    public static string Kasa(string dil) => En(dil) ? "Till register" : "Gişe kasası";
    public static string Personel(string dil) => En(dil) ? "Clerk" : "Tezgâhtar";
    public static string KaynakKasa(string dil) => En(dil) ? "Main register (cash source)" : "Ana kasa (nakit kaynağı)";
    public static string AcilisNakdi(string dil) => En(dil) ? "Opening cash" : "Açılış nakdi";
    public static string Urun(string dil) => En(dil) ? "Product" : "Ürün";
    public static string Adet(string dil) => En(dil) ? "Qty" : "Adet";
    public static string UrunEkle(string dil) => En(dil) ? "Add to list" : "Listeye ekle";
    public static string Cikar(string dil) => En(dil) ? "Remove" : "Çıkar";
    public static string Olustur(string dil) => En(dil) ? "Hand over" : "Zimmetle";
    public static string Kabul(string dil) => En(dil) ? "Accept custody" : "Zimmeti kabul et";
    public static string Iptal(string dil) => En(dil) ? "Cancel custody" : "Zimmeti iptal et";
    public static string TakviyeIste(string dil) => En(dil) ? "Request top-up" : "Takviye iste";
    public static string TakviyeNakit(string dil) => En(dil) ? "Cash amount" : "Nakit tutarı";
    public static string TalepGonder(string dil) => En(dil) ? "Send request" : "Talep gönder";
    public static string Onayla(string dil) => En(dil) ? "Approve" : "Onayla";
    public static string Reddet(string dil) => En(dil) ? "Decline" : "Reddet";
    public static string Kapat(string dil) => En(dil) ? "Close the day and transfer" : "Günü kapat ve devret";
    public static string KapatAc(string dil) => En(dil) ? "Evening count" : "Akşam sayımı";
    public static string SayilanNakit(string dil) => En(dil) ? "Counted cash" : "Sayılan nakit";
    public static string Sayilan(string dil) => En(dil) ? "Counted" : "Sayılan";
    public static string Beklenen(string dil) => En(dil) ? "Expected" : "Beklenen";
    public static string Satilan(string dil) => En(dil) ? "Sold" : "Satılan";
    public static string Zimmet(string dil) => En(dil) ? "Handed" : "Zimmet";
    public static string Fark(string dil) => En(dil) ? "Difference" : "Fark";
    public static string Gerekce(string dil) => En(dil) ? "Reason for the difference" : "Farkın gerekçesi";
    public static string DevirKasasi(string dil) => En(dil) ? "Transfer to register" : "Devir kasası";
    public static string BeklenenNakit(string dil) => En(dil) ? "Expected cash" : "Beklenen nakit";
    public static string NakitGizli(string dil) => En(dil)
        ? "Blind count: the expected cash is not shown. Count the drawer and enter it."
        : "Kör sayım: beklenen nakit gösterilmez. Çekmeceyi sayıp yazın.";
    public static string Acik(string dil) => En(dil) ? "Open custodies" : "Açık zimmetler";
    public static string Gecmis(string dil) => En(dil) ? "Past custodies" : "Geçmiş zimmetler";
    public static string Bos(string dil) => En(dil) ? "No open custody." : "Açık zimmet yok.";
    public static string Takviyeler(string dil) => En(dil) ? "Top-up requests" : "Takviye talepleri";
    public static string Isteyen(string dil) => En(dil) ? "Requested by" : "İsteyen";
    public static string KendiTalep(string dil) => En(dil) ? "You cannot approve your own request." : "Kendi talebinizi onaylayamazsınız.";
    public static string Geri(string dil) => En(dil) ? "Back to list" : "Listeye dön";
    public static string Ac(string dil) => En(dil) ? "Open" : "Aç";
    public static string Tarih(string dil) => En(dil) ? "Date" : "Tarih";
    public static string Durum(string dil) => En(dil) ? "Status" : "Durum";
    public static string DurumAdi(string durum, string dil) => durum switch
    {
        "TEKLIF" => En(dil) ? "Awaiting clerk" : "Tezgâhtar onayı bekliyor",
        "KABUL" => En(dil) ? "At the till" : "Gişede",
        "KAPANDI" => En(dil) ? "Closed" : "Kapandı",
        "IPTAL" => En(dil) ? "Cancelled" : "İptal",
        "TALEP" => En(dil) ? "Pending" : "Bekliyor",
        "RED" => En(dil) ? "Declined" : "Reddedildi",
        _ => durum,
    };
    public static string Olusturuldu(string dil) => En(dil) ? "Custody created; waiting for the clerk to accept." : "Zimmet oluşturuldu; tezgâhtarın kabulü bekleniyor.";
    public static string KabulEdildi(string dil) => En(dil) ? "Custody accepted; opening cash moved to the till." : "Zimmet kabul edildi; açılış nakdi gişeye geçti.";
    public static string TalepGonderildi(string dil) => En(dil) ? "Top-up request sent to a manager." : "Takviye talebi yöneticiye gönderildi.";
    public static string KararVerildi(string dil) => En(dil) ? "Request decided." : "Talep karara bağlandı.";
    public static string Kapatildi(string devir, string? kasa, string dil) => kasa is null
        ? (En(dil) ? "Till closed." : "Gişe kapandı.")
        : (En(dil) ? $"Till closed; {devir} transferred to {kasa}." : $"Gişe kapandı; {devir} {kasa} kasasına devredildi.");
    public static string IptalEdildi(string dil) => En(dil) ? "Custody cancelled." : "Zimmet iptal edildi.";
    public static string VardiyaIpucu(string dil) => En(dil)
        ? "Shift hand-over: close this custody with a count, then create a new one for the next clerk."
        : "Vardiya devri: bu zimmeti sayımla kapatın, sonra sıradaki tezgâhtara yeni zimmet açın.";
    public static string NakitsizIpucu(string dil) => En(dil)
        ? "Leave opening cash empty to hand over goods only."
        : "Yalnız mal zimmetlemek için açılış nakdini boş bırakın.";
    public static string UrunListesiBos(string dil) => En(dil) ? "No product added yet." : "Henüz ürün eklenmedi.";
    public static string DegismezGerekce(string dil) => En(dil)
        ? "A custody is a responsibility record: it is accepted, topped up, counted and closed; it is not edited."
        : "Zimmet bir sorumluluk kaydıdır: kabul edilir, takviye alır, sayılır ve kapanır; düzenlenmez.";
}
