namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-55 — GİDER PUSULASI: satırda Düzenle + belge ekranından dönüş +
// kilitli alanların gerekçesi (TR + EN).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır; aynı dalgada başka şeritler
//   de ona dokunuyor (orkestratör koşulu: yeni metin AYRI dosyaya —
//   Kuyumcu.Core/I18n/Sozlukler.<Alan>.cs). Sozlukler.Urunler.cs ile aynı desen.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL record'da durur;
//   Tr ve En aynı kurucudan geçer.
//
// ⚠ VAAT YASAĞI (UX1C3KilitDurustlukTestleri.VaatKaliplari): ekranda olmayan bir
//   düzeltme yolu yazılmaz. Anlatılan tek yol ekranda GERÇEKTEN var olan
//   "Pusulayı iptal et" düğmesi + yeni hurda alışıdır.
//
// ⚠ MEVZUAT (kurucu talimatı 23.09): bu metinler YENİ bir mevzuat hükmü
//   koymaz; sunucudaki mevcut kilidi (HurdaServisi.Guncelle — PATCH yalnız
//   satıcı adresi + mal açıklaması) ekranda söyler. "Süre geçince düzeltme
//   yasak mı?" sorusu birincil kaynakla DOĞRULANAMADI → kilit KONMADI, yalnız
//   dürüst uyarı basılır (SureGectiUyari).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Gider pusulası liste/çekmece/belge ekranlarının P-55 metinleri (bir dilde).</summary>
public sealed record GiderPusulasiDuzenMetinleri(
    string Duzenle,
    string DuzenleEtiket,
    string CekmeceBaslik,
    string DuzenlenebilirBaslik,
    string DuzenlenebilirAciklama,
    string KilitliSatici,
    string KilitliTckn,
    string KilitliTutar,
    string KilitliTarih,
    string DuzeltmeYolu,
    string SureGectiUyari,
    string PusulayaDon,
    string PusulayiDuzenle,
    string IptalYeri);

public static class GiderPusulasiSozlugu
{
    public static readonly GiderPusulasiDuzenMetinleri Tr = new(
        Duzenle: "Düzenle",
        DuzenleEtiket: "{0} pusulasını düzenle",
        CekmeceBaslik: "Gider pusulası — Detay / Düzenle",
        DuzenlenebilirBaslik: "Düzeltilebilen alanlar",
        DuzenlenebilirAciklama: "Bu belgede yalnız satıcı adresi ve mal açıklaması düzeltilebilir. Diğer alanlar kilitlidir; nedeni her alanın yanında yazar.",
        KilitliSatici: "Satıcı adı, belgenin kimin adına düzenlendiğini gösterir; basılmış belgede değiştirilmez.",
        KilitliTckn: "TCKN, satıcının kimlik bilgisidir; basılmış belgede değiştirilmez.",
        KilitliTutar: "Brüt, stopaj ve net tutar hurda alışı kaydından hesaplanır; belgede elle değiştirilmez.",
        KilitliTarih: "Teslim tarihi 7 günlük düzenleme süresini belirler; geriye alınırsa süre uzamış olur, bu yüzden değiştirilmez.",
        DuzeltmeYolu: "Pusulayı iptal edip yeni pusula düzenleyin: aşağıdaki \"{0}\" düğmesiyle belgeyi gerekçeli olarak hükümsüz kılın, sonra doğru bilgilerle yeni bir hurda alışı girin.",
        SureGectiUyari: "Düzenleme süresi geçmiş. Süre geçtikten sonra yapılan düzeltmenin uygun olup olmadığını mali müşavirinize sorun.",
        PusulayaDon: "Pusulaya dön",
        PusulayiDuzenle: "Pusulayı düzenle",
        IptalYeri: "Bu belgeyi iptal etmek için \"Pusulaya dön\" ile pusulayı açın ve \"{0}\" düğmesini kullanın.");

    public static readonly GiderPusulasiDuzenMetinleri En = new(
        Duzenle: "Edit",
        DuzenleEtiket: "Edit voucher {0}",
        CekmeceBaslik: "Expense voucher — Details / Edit",
        DuzenlenebilirBaslik: "Editable fields",
        DuzenlenebilirAciklama: "Only the seller address and the goods description can be corrected on this document. Other fields are locked; the reason is written next to each one.",
        KilitliSatici: "The seller name shows who the document was issued to; it is not changed on an issued document.",
        KilitliTckn: "The ID number is the seller's identity detail; it is not changed on an issued document.",
        KilitliTutar: "Gross, withholding and net amounts are calculated from the scrap purchase record; they are not edited by hand.",
        KilitliTarih: "The delivery date sets the 7-day issuing period; moving it back would extend the period, so it is not changed.",
        DuzeltmeYolu: "Void the voucher and issue a new one: void the document with a reason using the \"{0}\" button below, then enter a new scrap purchase with the correct details.",
        SureGectiUyari: "The issuing period has passed. Ask your accountant whether a correction after the period is acceptable.",
        PusulayaDon: "Back to voucher",
        PusulayiDuzenle: "Edit voucher",
        IptalYeri: "To void this document, open the voucher with \"Back to voucher\" and use the \"{0}\" button.");

    public static GiderPusulasiDuzenMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
