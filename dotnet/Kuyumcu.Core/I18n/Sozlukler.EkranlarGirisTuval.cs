namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-K4 — TÜM EKRANLAR + GİRİŞ TUVALE · METİNLER (TR + EN)
//
// Tek doğruluk kaynağı: dokumanlar/02-mimari/sadelestirme/tuval/project/
//   Ekranlar.dc.html · Giris.dc.html (kurucu talimatı 24.09 17:20: "birebir").
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler HUB'ına dokunulmaz (dalga kuralı). Hub'daki
//   ortak anahtarlar (Auth.Sifre = "Şifre", Auth.GirisYap = "Giriş Yap",
//   Pano.Baslik = "Vitrin Panosu") başka ekranlarda aynen kalır; bu iki
//   ekran tuvalin yazımını buradan okur.
// ⚠ Marka adı burada YAZILMAZ: "ByGatech" yazımı KabukTuvalSozlugu.MarkaYazimi'dan
//   (Marka.Ad'dan türetilir, P-D2 tek kaynak).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Tüm ekranlar ve giriş tuvallerinin yazımları — dil kodu "tr" | "en".</summary>
public static class EkranlarGirisTuvalSozlugu
{
    private static bool En(string dil) => dil == "en";

    /// <summary>Tüm ekranlar alt satırındaki küçük karo (tuval: "Vitrin panosu").</summary>
    public static string VitrinPanosu(string dil) => En(dil) ? "Showcase board" : "Vitrin panosu";

    /// <summary>Giriş formundaki parola kutusunun etiketi (tuval: "Parola" — "Parolamı unuttum" ile aynı kelime).</summary>
    public static string Parola(string dil) => En(dil) ? "Password" : "Parola";

    /// <summary>Giriş düğmesi (tuval: "Giriş yap").</summary>
    public static string GirisYap(string dil) => En(dil) ? "Sign in" : "Giriş yap";
}
