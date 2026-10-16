namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-e — GİRİŞ EKRANI: İKİ PANEL · METİNLER (TR + EN)
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs 9200+ satırlık ORTAK HUB'dır ve aynı dalgada
//   başka şeritler de ona dokunuyor (görev: "DÜZENLEME; yeni metin için AYRI
//   dosya aç"). EkranlarSozlugu / MenuSozlugu / UstBarSozlugu ile aynı desen.
//
// ⚠ Kaynak: kurucunun onayladığı tasarım tuvali
//   dokumanlar/02-mimari/sadelestirme/tuval/project/Giris.dc.html.
//   Metinler tuvalden BİREBİR alındı; sapmalar (e-posta etiketi, sürüm satırı)
//   paket raporunun § 4'ündedir.
//
// ⚠ UX-S31 — aynı kavram tek ad: "Yeni dükkân kur" bağlantısı, kayıt formunun
//   başlığı ve gönder düğmesi AYNI metni (YeniDukkanKur) kullanır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Giriş ekranı (iki panel) metinleri — dil kodu "tr" | "en".</summary>
public static class GirisSozlugu
{
    private static bool En(string dil) => dil == "en";

    /// <summary>Sol kartın başlığı (tuval: "Mihenk’e hoş geldin"). Ad KabukTuvalSozlugu.MarkaYazimi’dan (Marka.Ad’dan türer — T-K4).</summary>
    public static string HosGeldin(string dil) =>
        En(dil) ? $"Welcome to {KabukTuvalSozlugu.MarkaYazimi}" : $"{KabukTuvalSozlugu.MarkaYazimi}’e hoş geldin";

    /// <summary>Başlığın altındaki tek cümle (tuval: "Dükkânın hesabıyla gir.").</summary>
    public static string HesabinlaGir(string dil) =>
        En(dil) ? "Sign in with your shop account." : "İşletmenin hesabıyla gir.";

    /// <summary>Parola kutusunun içindeki göster düğmesi.</summary>
    public static string Goster(string dil) => En(dil) ? "Show" : "Göster";

    /// <summary>Parola görünürken aynı düğmenin metni.</summary>
    public static string Gizle(string dil) => En(dil) ? "Hide" : "Gizle";

    /// <summary>Formun altındaki sol bağlantı.</summary>
    public static string ParolamiUnuttum(string dil) => En(dil) ? "Forgot my password" : "Parolamı unuttum";

    /// <summary>
    /// "Parolamı unuttum"un açtığı yardım metni. ⚠ Kendi kendine parola
    /// sıfırlama ucu YOKTUR (doğrulandı: Modules/Auth); yeni parolayı
    /// SAHIP ya da MUDUR verir (PATCH /kullanicilar/{id}/sifre,
    /// [RolGerekli("SAHIP","MUDUR")]). Metin bunu söyler, uydurma yol açmaz.
    /// </summary>
    public static string ParolaYardim(string dil) => En(dil)
        ? "Ask your shop owner or manager: they can set a new password for you under Users → Reset password."
        : "İşletme sahibine ya da müdürüne söyle: Kullanıcılar ekranında “Şifre Sıfırla” ile sana yeni parola verir.";

    /// <summary>Formun altındaki sağ bağlantı + kayıt formunun başlığı + gönder düğmesi.</summary>
    public static string YeniDukkanKur(string dil) => En(dil) ? "Set up a new shop" : "Yeni işletme kur";

    /// <summary>Kayıt formunun alt cümlesi.</summary>
    public static string YeniDukkanAlt(string dil) => En(dil)
        ? "You need the invitation code sent to you."
        : "Sana gönderilen davet koduyla işletmesini aç.";

    /// <summary>Kayıt formundan giriş formuna dönüş bağlantısı.</summary>
    public static string GiriseDon(string dil) => En(dil) ? "Back to sign in" : "Girişe dön";

    /// <summary>
    /// Keşif B006 güvenliği: bu sekmenin kullanıcısı değil, BAŞKA bir kullanıcı bu tarayıcıda
    /// oturum açtı; sekme onun jetonunu benimsemedi ve düştü.
    /// </summary>
    public static string BaskaKullaniciOturumActi(string dil) => En(dil)
        ? "Another user has signed in on this browser — please sign in again."
        : "Bu tarayıcıda başka bir kullanıcı oturum açtı — yeniden giriş yapın.";

    /// <summary>
    /// Sürüm satırı. Sürüm ELLE YAZILMAZ: istemci derlemesinin sürümünden
    /// türer (yayın betiği -p:InformationalVersion ile tek yerden verir).
    /// </summary>
    public static string SurumSatiri(string dil, string surum) => $"{KabukTuvalSozlugu.MarkaYazimi} {surum} · ByGatech";

    /// <summary>Sağ panelin büyük cümlesi (tuval birebir).</summary>
    public static string PanelBaslik(string dil) => En(dil)
        ? "One slip at the counter, everything in the books."
        : "Tezgâhta bir fiş, defterde her şey.";

    /// <summary>Sağ panelin alt cümlesi (tuval birebir).</summary>
    public static string PanelAlt(string dil) => En(dil)
        ? "Sales, purchases and collections on one screen. Fine-gold accounting, cash and legal rules run by themselves in the background."
        : "Satış, alış, tahsilat aynı ekranda. Has hesabı, kasa ve yasal kurallar arkada kendiliğinden işler.";

    /// <summary>Sağ paneldeki üç rakam (tuval birebir): değer + alt etiket.</summary>
    public static IReadOnlyList<(string Deger, string Etiket)> Rakamlar(string dil) => En(dil)
        ? [("3 clicks", "cash sale"), ("30 min", "to learn"), ("0", "deleted records")]
        : [("3 tık", "nakit satış"), ("30 dk", "öğrenme"), ("0", "silinen kayıt")];

    /// <summary>Katlanır kısıt beyanının özet satırı: "(n)" sayısı sunucu listesinden.</summary>
    public static string KisitOzet(string baslik, int? adet) =>
        adet is { } n ? $"{baslik} ({n})" : baslik;
}
