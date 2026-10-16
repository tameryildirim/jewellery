namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-53 — TASLAK FATURA DÜZENLEME ÇEKMECESİ: TR + EN METİNLER
// PAKET P-54 — TASLAK FATURA KALEMLERİ: silme yolu gerekçesi + hata özeti
// PAKET Q9-2 — TASLAK DÜZENLE ÇEKMECESİNDE SATIRLAR: ekleme formu + kalemsiz taslak gerekçesi
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır; bu şeride ona dokunmak
//   YASAKTIR (orkestratör dosya sınırı, SERİT A). Sozlukler.Musteriler.cs /
//   PesinMusteriSozlugu.cs ile aynı desen: yeni metin AYRI dosyada.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL bir record'da
//   durur; Tr ve En aynı kurucudan geçer.
//
// ⚠ KURAL SUNUCUDADIR: "kuyum (özel matrah) taslağının toplamı başlıktan
//   değişmez" — FaturaModulu.TaslakGuncelle. Buradaki metin yalnız o kuralın
//   ekrandaki GEREKÇESİ ve DÜZELTME YOLUdur (UX-S25, UX-S30).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Taslak fatura düzenleme çekmecesinin bir dildeki metinleri.</summary>
public sealed record FaturaTaslakMetinleri(
    string KuyumToplamGerekce,
    string KuyumToplamYol,
    string CariYuklenemedi,
    // P-54 — taslak kalemi için silme ucu YOK (ölçüldü); sebep ve yol ekranda.
    string KalemSilmeGerekce,
    string KalemSilmeYol,
    // P-54 — kalem düzeltmesi sunucu tarafından reddedilince bant özeti.
    string KalemHatali,
    // Q9-2 — Düzenle çekmecesindeki satır bölümü ve yeni satır formu.
    string SatirlarBaslik,
    string YeniSatirBaslik,
    string SatirEkleDugme,
    string SatirEklendi,          // {0} = sıra, {1} = yeni toplam
    string AciklamaYaDaUrun,
    string SatirToplamiSifir,
    string KuyumUrunZorunlu,
    string UrunAdindanGelir,
    string UrunSiniri,            // {0} = liste sınırı
    string UrunYuklenemedi,       // {0} = sunucu mesajı
    string SatirsizGerekce,
    string SatirsizYol,
    string SatirDuzeltIpucu,
    // ZO-BELGE Y4-D — gümüş (altın dışı) kuyum satırında has bedeli düşülmez; ekranda kısa gerekçe.
    string GumusOzelMatrahYok,
    // ZO-BELGE (30.09) — madeni bilinmeyen kuyum satırı: özel matrah uygulanmaz (fail-closed), sebep yazılır.
    string MadenBilinmiyorOzelMatrahYok);

public static class FaturaTaslakSozlugu
{
    public static readonly FaturaTaslakMetinleri Tr = new(
        KuyumToplamGerekce: "Kuyum faturasında toplam buradan değişmez: has altın bedeli bu toplama göre hesaplandı.",
        KuyumToplamYol: "Taslağı iptal edip doğru toplamla yeniden kesin. Taslak, fatura numarası harcamaz.",
        CariYuklenemedi: "Müşteri listesi yüklenemedi. Sayfayı yenileyip yeniden deneyin.",
        KalemSilmeGerekce: "Taslak faturada tek bir satır silinemez: toplam, KDV ve has bedeli satırların tamamından hesaplanır.",
        KalemSilmeYol: "Satırı düzeltmek için üzerindeki değere tıklayın. Satır hiç olmamalıysa taslağı listeden İptal Et ile iptal edip doğru satırlarla yeniden kesin. Taslak, fatura numarası harcamaz.",
        KalemHatali: "Satır kaydedilemedi — işaretli alanın altındaki açıklamaya bakın.",
        SatirlarBaslik: "Satırlar",
        YeniSatirBaslik: "Yeni satır ekle",
        SatirEkleDugme: "Satırı ekle",
        SatirEklendi: "Satır eklendi: {0}. satır · yeni toplam {1}",
        AciklamaYaDaUrun: "Açıklama yazın ya da ürün seçin.",
        SatirToplamiSifir: "Satır toplamı sıfırdan büyük olmalı (miktar × birim fiyat + işçilik − indirim).",
        KuyumUrunZorunlu: "Kuyum faturasında satır için ürün seçin: has altın bedeli ürün kartından hesaplanır.",
        UrunAdindanGelir: "Açıklama boş kalırsa ürün adı yazılır.",
        UrunSiniri: "İlk {0} ürün listelendi. Aradığınız ürün yoksa açıklamayı elle yazın.",
        UrunYuklenemedi: "Ürün listesi yüklenemedi: {0}",
        SatirsizGerekce: "Bu taslak satırsız, yalnız toplamla kaydedildi; satır eklenemez (ilk satır yazdığınız toplamı değiştirirdi).",
        SatirsizYol: "Tutarı yukarıdaki Toplam alanından düzeltin. Satır satır fatura istiyorsanız taslağı listeden İptal Et ile iptal edip Yeni Fatura'da satır ekleyerek yeniden kesin. Taslak, fatura numarası harcamaz.",
        SatirDuzeltIpucu: "Düzeltmek için değerin üzerine tıklayın; toplam sunucuda yeniden hesaplanır.",
        GumusOzelMatrahYok: "Gümüş, platin ve paladyumda özel matrah uygulanmaz — KDV tam.",
        MadenBilinmiyorOzelMatrahYok: "Bu satırın madeni bilinmiyor — özel matrah uygulanmadı, KDV tam.");

    public static readonly FaturaTaslakMetinleri En = new(
        KuyumToplamGerekce: "On a jewellery invoice the total cannot be changed here: the pure gold value was calculated from it.",
        KuyumToplamYol: "Cancel the draft and issue it again with the correct total. A draft does not use up an invoice number.",
        CariYuklenemedi: "The customer list could not be loaded. Refresh the page and try again.",
        KalemSilmeGerekce: "A single line cannot be deleted from a draft invoice: the total, VAT and fine-gold value are calculated from all lines.",
        KalemSilmeYol: "To correct a line, click its value. If the line should not exist at all, cancel the draft from the list with Cancel and issue it again with the correct lines. A draft does not use up an invoice number.",
        KalemHatali: "The line could not be saved — see the message under the marked field.",
        SatirlarBaslik: "Lines",
        YeniSatirBaslik: "Add a new line",
        SatirEkleDugme: "Add line",
        SatirEklendi: "Line added: line {0} · new total {1}",
        AciklamaYaDaUrun: "Enter a description or choose a product.",
        SatirToplamiSifir: "The line total must be greater than zero (quantity × unit price + labour − discount).",
        KuyumUrunZorunlu: "On a jewellery invoice choose a product for the line: the fine-gold value is calculated from the product card.",
        UrunAdindanGelir: "If the description is left empty, the product name is used.",
        UrunSiniri: "The first {0} products are listed. If yours is missing, type the description yourself.",
        UrunYuklenemedi: "The product list could not be loaded: {0}",
        SatirsizGerekce: "This draft was saved without lines, with a total only; lines cannot be added (the first line would change the total you entered).",
        SatirsizYol: "Correct the amount in the Total field above. If you want an invoice line by line, cancel the draft from the list with Cancel and issue it again in New Invoice with lines. A draft does not use up an invoice number.",
        SatirDuzeltIpucu: "Click a value to correct it; the total is recalculated on the server.",
        GumusOzelMatrahYok: "No special tax base applies to silver, platinum or palladium — full VAT.",
        MadenBilinmiyorOzelMatrahYok: "The metal of this line is unknown — no special tax base applied, full VAT.");

    /// <summary>Seçili dilin metinleri.</summary>
    public static FaturaTaslakMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
