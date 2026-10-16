namespace Kuyumcu.Web.Bilesenler.Fis;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-GERI (kurucu bulgusu 09.10: "toplam tutarı 200.000 üzerine çıkardım
// sonra ürünlerden kaldırdım, 200.000'in altına indirdim, algılamadı").
//
// KÖK NEDEN (gerçek Chrome'da yeniden üretildi): sunucunun bir reddiyle (400) açılan ekran
// durumları — MASAK alanlarını zorla açan bayrak, BASİT'te "yasal kapı açıldı" bayrağı, Peşin
// reddi, sunucu hata bandı — DÜZ bool idi ve yalnız Kaydet/müşteri değişimi/Vazgeç ile
// sıfırlanıyordu. Fiş tutarı düşürülünce (kalem sil, adet azalt, iskonto, takas/hurda tutarı)
// bayrak yerinde kalıyor; MASAK formu açık, Kaydet kilitli ve "MASAK eşiği aşıldı" yazıyordu —
// sunucu ise güncel toplamla karar verip kabul edecekti (ekran ≠ sunucu).
//
// KURAL (tek kaynak, tüm işlem ekranları): sunucu reddinin açtığı durum, reddin verildiği
// ÖLÇÜYE (fişin yasal kapıya giden tutarı) ve MÜŞTERİYE bağlı bir İZDİR. Tutar o ölçünün
// ALTINA inerse ya da müşteri değişirse iz geçersizdir: form kapanır, kilit kalkar, karar yine
// istemcinin sunucudan aldığı eşiklere (/masak/esikler, /masak/gereksinim, /tevsik/gunluk)
// döner. İz SİLİNMEZ: tutar yeniden ölçüye çıkarsa durum geri gelir. Girilen beyan verisi
// (masak modeli) hiç silinmez; gövdeye yalnız eşik/iz geçerliyken konur (sayfanın kuralı).
// ⚠ Eşik SAYISI burada yoktur — yalnız sunucunun reddettiği tutar hatırlanır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Sunucu reddinin izi: reddin verildiği ölçü (mutlak tutar) ve müşteri.</summary>
public sealed record YasalKapiIzi(decimal Olcu, string? CariId)
{
    /// <summary>Şu anki fişten iz alır (müşteri boşsa null — "müşterisiz" tek değer).</summary>
    public static YasalKapiIzi Al(decimal olcu, string? cariId) => new(Math.Abs(olcu), Bos(cariId));

    /// <summary>İz bu fiş için hâlâ geçerli mi: aynı müşteri ve tutar reddin ölçüsünden aşağı inmemiş.</summary>
    public bool Gecerli(decimal olcu, string? cariId) => Bos(cariId) == CariId && Math.Abs(olcu) >= Olcu;

    /// <summary>İz var ve geçerli mi (null iz = hiç ret yok).</summary>
    public static bool Gecerli(YasalKapiIzi? iz, decimal olcu, string? cariId) => iz is not null && iz.Gecerli(olcu, cariId);

    private static string? Bos(string? c) => string.IsNullOrEmpty(c) ? null : c;
}
