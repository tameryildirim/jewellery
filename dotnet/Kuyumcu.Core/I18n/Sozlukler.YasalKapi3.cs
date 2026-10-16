namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-3 — YASAL-KAPI-2 kapanış devri (tasarım hakemi KRİTİK, 10.10).
// /cariler Tahsilat çekmecesinde 250.000 ₺ NAKİT: ekran önce kimlik formunu doldurtuyor, Kaydet'te sunucu
// nakit sınırı (459 s. VUK GT — had sunucudan, /tevsik/gunluk) ile reddediyordu; ödeme türü Kart'a
// çevrilince eski hata yerinde kalıyordu. Artık nakit sınırı ÖNCE söylenir (alan altında tek cümle),
// form doldurtulmaz; ödeme türü/tutar değişince cümle yeniden değerlendirilir.
// Metinler sade, tek cümle; had SAYISI istemciye gömülü DEĞİL (sunucunun kalan nakit hakkı {0}).
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (yeni metin paketin kendi dosyasına).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>YASAL-KAPI-3 metinleri (bir dilde).</summary>
public sealed record YasalKapi3Metinleri(
    // Bugün bu müşteriden nakit hakkı kalmadı (sunucu: kalan nakit hakkı 0) — tutar alanının altında
    string NakitBugunYok,
    // Ödeme türü seçicisinde "Nakit"in seçilemez gerekçesi (kalan hak 0)
    string NakitBugunPasif,
    // Döviz (efektif) tahsilatta TL karşılığı kalan nakit hakkını aşıyor: {0} = kalan hak (TL)
    string DovizNakitSinirAsildi);

/// <summary>YASAL-KAPI-3 sözlüğü (TR + EN).</summary>
public static class YasalKapi3Sozlugu
{
    public static readonly YasalKapi3Metinleri Tr = new(
        NakitBugunYok: "Bugün bu müşteriden nakit alınamaz; kartla ya da havaleyle alın.",
        NakitBugunPasif: "Bugünkü nakit sınırı doldu",
        DovizNakitSinirAsildi: "Nakit en çok {0} karşılığı olabilir; tutarı azaltın.");

    public static readonly YasalKapi3Metinleri En = new(
        NakitBugunYok: "No more cash can be taken from this customer today; take it by card or bank transfer.",
        NakitBugunPasif: "Today's cash limit is used up",
        DovizNakitSinirAsildi: "Cash can be at most the equivalent of {0}; lower the amount.");

    public static YasalKapi3Metinleri Al(string dil) => dil == "en" ? En : Tr;
}
