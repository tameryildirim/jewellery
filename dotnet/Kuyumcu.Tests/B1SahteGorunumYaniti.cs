namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-1 — bUnit sahte API'leri için GERÇEKÇİ /kullanici/gorunum yanıtı.
//
// ⚠ Neden ayrı dosya: Ayarlar.razor B-1 ile KullaniciGorunumServisi'ni enjekte
//   eder. Sayfayı çizen ESKİ bUnit sınıflarının "{}" varsayılanı bu uca YETMEZ
//   (servis "yüklenmedi" sayar, seçiciler kilitli kalır → YANLIŞ YEŞİL).
//   Sözleşme tek yerde dursun diye JSON burada üretilir; her sınıf kendi
//   switch'inde bu iki üreteci çağırır.
//
// ⚠ Bu dosya YALNIZ testlerindir; hiçbir üretim kuralı burada tanımlanmaz —
//   değerler sunucunun döndürdüğü şeklin birebir aynasıdır.
// ═════════════════════════════════════════════════════════════════════════════

internal static class B1SahteGorunumYaniti
{
    /// <summary>Dükkân varsayılanını devralan (kullanıcı satırı boş) bir kullanıcı.</summary>
    public const string Govde =
        """
        {"temaModu":"sistem","sema":"platin","yaziBoyutu":18,
         "temaModuKaynagi":"sistem","semaKaynagi":"sistem","yaziBoyutuKaynagi":"sistem",
         "ozelTemaModu":null,"ozelSema":null,"ozelYaziBoyutu":null,
         "kiraciTemaModu":"sistem","kiraciSema":"platin","kiraciYaziBoyutu":18,
         "semalar":["platin","zumrut","grafit","bordo","okyanus","menekse","zeytin","fildisi"],
         "temaModlari":["sistem","aydinlik","karanlik"],"yaziBoyutlari":[16,18,20]}
        """;

    /// <summary>PATCH yanıtının sarmalı — sunucu <c>{ gorunum, mesaj }</c> döner.</summary>
    public const string Sarmal =
        $$"""{"gorunum":{{Govde}},"mesaj":"Görünüm kaydedildi"}""";

    /// <summary>Dükkân görünüm varsayılanı (GET /gorunum) — aynı üçlünün kiracı tarafı.</summary>
    public const string Kiraci =
        """{"sema":"platin","yaziOlcegi":"18","temaModu":"sistem"}""";
}
