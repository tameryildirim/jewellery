namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-65 — ONAY VE KİLİT STANDARDI (tur 8 triyajı: görsel E49 + UX-1 B48).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: yeni
//   metin AYRI dosyaya — Kuyumcu.Core/I18n/Sozlukler.<Alan>.cs).
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL record'da
//   durur; Tr ve En aynı kurucudan geçer.
//
// ⚠ DÜĞME ADI METNE GÖMÜLMEZ: "Sahipliği Devret", "İptal et", "Aç" gibi
//   düğme adları {0}/{1} yer tutucusuyla ekrandaki GERÇEK düğme etiketinden
//   gelir — düğmenin adı değişirse yol tarifi yalan söylemez.
//
// ⚠ ONAY / VAZGEÇ FARKLI FİİL (E49): onay düğmesi eylemin kendi fiilini
//   taşır ("Pasife al", "Varsayılana dön"); vazgeç düğmesi "Vazgeç"tir,
//   ASLA "İptal" değildir (/yedekler'de iptal onayında iki düğme de "iptal"
//   diyordu — kullanıcı hangisinin işlemi yaptığını bilemiyordu).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Onay kipi ve kilit gerekçesi metinleri (bir dilde).</summary>
public sealed record OnayKilitMetinleri(
    // ── OnayKipi bileşeni ──
    string Vazgec,
    string GeriDonulemezUyari,
    string YazarakOnayla,
    string YazarakOnaylaKilit,
    string OnayKilitSebebi,
    // ── Fiyatlandırma: pasife alma onayı ──
    string ListePasifBaslik,
    string ListePasifAciklama,
    string KampanyaPasifBaslik,
    string KampanyaPasifAciklama,
    string GeriDonusYolu,
    // ── Vitrin: varsayılana dönüş onayı ──
    string VitrinDonusBaslik,
    // ── Kullanıcılar ──
    string KullaniciPasifBaslik,
    string KendiRolYolu,
    // ── Kur ──
    string ManuelKurGerekce,
    string ManuelKurYolu,
    // ── Emanet ──
    string EmanetMusteriSecilmedi,
    string EmanetKurYok,
    string EmanetIadeEdilecekYok,
    // ── Evrak ──
    string EvrakEksikAlanlar,
    string EvrakTuruGerekce,
    // ── Bildirim ──
    string BildirimKurulumKapaliGerekce,
    string BildirimKurulumKapaliYol);

public static class OnayKilitSozlugu
{
    public static readonly OnayKilitMetinleri Tr = new(
        Vazgec: "Vazgeç",
        GeriDonulemezUyari: "Bu işlem geri alınamaz.",
        YazarakOnayla: "Onaylamak için kutuya {0} yazın.",
        YazarakOnaylaKilit: "Kutuya {0} yazılmadan düğme açılmaz.",
        OnayKilitSebebi: "Neden kapalı",
        ListePasifBaslik: "Fiyat listesini pasife al",
        ListePasifAciklama: "“{0}” listesi pasife alınır; yeniden aktifleştirilene kadar kullanılmaz.",
        KampanyaPasifBaslik: "Kampanyayı pasife al",
        KampanyaPasifAciklama: "“{0}” kampanyası pasife alınır; yeniden aktifleştirilene kadar uygulanmaz.",
        GeriDonusYolu: "Geri dönüş: aynı satırdaki “{0}” düğmesi.",
        VitrinDonusBaslik: "Varsayılana dön",
        KullaniciPasifBaslik: "Kullanıcının erişimini kapat",
        KendiRolYolu: "Rolünüzü başka bir SAHİP değiştirebilir. Sahipseniz ve rolünüzü bırakmak istiyorsanız: listede diğer kullanıcının satırındaki “{0}” düğmesiyle sahipliği devredin ve devirden sonraki rolünüzü orada seçin.",
        ManuelKurGerekce: "Elle girilen kur kaydı düzenlenemez; yalnız gerekçe yazılarak iptal edilir. İptal edilen kayıt silinmez, geçmişte “geçersiz” olarak kalır.",
        ManuelKurYolu: "Yanlış girildiyse: satırdaki “{0}” ile gerekçeli iptal edin, sonra “{1}” ile doğru kuru girin.",
        EmanetMusteriSecilmedi: "Önce müşteri seçin.",
        EmanetKurYok: "Güncel kur alınamadı; Kur ekranından kur çekin ya da elle girin.",
        EmanetIadeEdilecekYok: "Bu müşterinin elimizde emaneti yok; iade edilecek bir şey bulunmuyor.",
        EvrakEksikAlanlar: "Eksik alanlar: {0}",
        EvrakTuruGerekce: "Evrak türü (çek / senet) kayıtla birlikte deftere yazıldı; sonradan değiştirilemez.",
        BildirimKurulumKapaliGerekce: "Kurulum ayarında kapatılan hatırlatma bu ekrandan açılamaz: yasal ya da kurulum gereği kapalı tutulur.",
        BildirimKurulumKapaliYol: "Kurulumu yapan kişiden bu hatırlatmayı sunucu ayarında açmasını isteyin; açıldıktan sonra burada “{0}” kullanılabilir.");

    public static readonly OnayKilitMetinleri En = new(
        Vazgec: "Go back",
        GeriDonulemezUyari: "This action cannot be undone.",
        YazarakOnayla: "Type {0} in the box to confirm.",
        YazarakOnaylaKilit: "The button stays locked until {0} is typed in the box.",
        OnayKilitSebebi: "Why locked",
        ListePasifBaslik: "Deactivate price list",
        ListePasifAciklama: "The “{0}” list will be deactivated; it is not used until it is reactivated.",
        KampanyaPasifBaslik: "Deactivate campaign",
        KampanyaPasifAciklama: "The “{0}” campaign will be deactivated; it does not apply until it is reactivated.",
        GeriDonusYolu: "To undo: the “{0}” button on the same row.",
        VitrinDonusBaslik: "Reset to default",
        KullaniciPasifBaslik: "Close the user's access",
        KendiRolYolu: "Another OWNER can change your role. If you are the owner and want to give up your role: use the “{0}” button on the other user's row to transfer ownership and choose your role after the transfer there.",
        ManuelKurGerekce: "A manually entered rate cannot be edited; it can only be voided with a written reason. A voided record is not deleted, it stays in the history marked “invalid”.",
        ManuelKurYolu: "If it was entered wrongly: void it with a reason using “{0}” on its row, then enter the correct rate with “{1}”.",
        EmanetMusteriSecilmedi: "Select a customer first.",
        EmanetKurYok: "No current rate; fetch one or enter it manually on the Rates screen.",
        EmanetIadeEdilecekYok: "We hold no custody for this customer; there is nothing to return.",
        EvrakEksikAlanlar: "Missing fields: {0}",
        EvrakTuruGerekce: "The instrument type (cheque / note) was written to the ledger with the record; it cannot be changed later.",
        BildirimKurulumKapaliGerekce: "A reminder switched off in the installation settings cannot be turned on from this screen: it is kept off for legal or setup reasons.",
        BildirimKurulumKapaliYol: "Ask the person who installed the system to turn this reminder on in the server settings; after that “{0}” can be used here.");

    public static OnayKilitMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
