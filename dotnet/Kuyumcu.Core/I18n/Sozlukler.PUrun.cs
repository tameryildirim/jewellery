namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-URUN (ekran denetimi 10.10 · K6, Y19, O13) — yeni ürün formunda ayar.
//
// K6: yeni ürün formu AYAR'ı "18 Ayar" dolu açıyordu; "22 Ayar Halka Küpe" 18 ayar
// fiyatıyla kaydedildi. Artık ayarın VARSAYILANI YOK (fail-closed): kullanıcı
// seçmeden Kaydet kilitli ve sebebi yazılı; ad ile seçim çelişirse uyarı görünür
// (kaydı engellemez). Sunucu da ayarsız gövdeyi 400 + alan "ayarId" ile reddeder.
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (yeni metin Sozlukler.<Alan>.cs).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; Tr ve En aynı
//   kurucudan geçer.
// ⚠ Mevzuat hükmü YOKTUR.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Yeni ürün formu ayar metinleri (bir dilde).</summary>
public sealed record PUrunMetinleri(
    // Ayar seçicisinin boş seçeneği.
    string AyarSecin,
    // Kaydet kilidinin sebebi / alan altı mesaj: ayar seçilmedi.
    string AyarSecilmedi,
    // Sunucu reddi: gövdede ayar yok.
    string AyarZorunlu,
    // Ad ile seçilen ayar çelişiyor. {0} = addaki ayar adı, {1} = seçilen ayar adı.
    string AdAyarCelisiyor);

/// <summary>Yeni ürün formu ayar sözlüğü.</summary>
public static class PUrunSozlugu
{
    public static readonly PUrunMetinleri Tr = new(
        AyarSecin: "Ayar seçin",
        AyarSecilmedi: "Ayarı seçin.",
        AyarZorunlu: "Ayar seçilmedi — ürünün ayarını seçin.",
        AdAyarCelisiyor: "Adda {0} yazıyor, seçilen {1}. Doğru mu?");

    public static readonly PUrunMetinleri En = new(
        AyarSecin: "Choose purity",
        AyarSecilmedi: "Choose the purity.",
        AyarZorunlu: "No purity chosen — choose the product's purity.",
        AdAyarCelisiyor: "The name says {0}, but {1} is chosen. Is that right?");

    public static PUrunMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
