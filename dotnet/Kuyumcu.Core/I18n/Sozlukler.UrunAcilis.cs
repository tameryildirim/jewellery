namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET URUN-ACILIS-ATOMIK (tur 2, tasarım hakemi KRİTİK) — ürün kartında açılış
// stoğu reddinin TEZGÂHTAR DİLİ.
//
// Eski metinler sayım ekranına aitti ("Sayım kaydı için en az bir kur girişi
// gerekir — hareket kur fotoğrafına bağlanır", "Sayılan adet çok büyük"):
// kullanıcı sayım yapmıyor, ürün ekliyor. Ürün kartı yolu bağlama uygun metni
// okur; sunucu bu metni `alanlar["acilisStogu.adet"]` ile döndürür, ekran
// açılış stoğu kutusunun ALTINA yazar (Urunler.razor YeniEsleme).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (yeni metin Sozlukler.<Alan>.cs).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; Tr ve En aynı
//   kurucudan geçer.
// ⚠ Mevzuat hükmü YOKTUR: tavan kolon kapasitesidir (decimal(15,6)), eşik değil.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Ürün kartı açılış stoğu metinleri (bir dilde).</summary>
public sealed record UrunAcilisMetinleri(
    // Açılış stoğu × has gram kolonun taşıyabileceği değeri aşıyor.
    string AcilisCokBuyuk,
    // Kiracıda henüz hiç altın fiyatı yok (yeni işletmede ilk saniyeler).
    string AcilisKurYok,
    // URUN-FORM-2: adet 0 ya da eksi (tezgâhtar dili; aralık jargonu yok).
    string AdetSifirdanBuyuk,
    // URUN-FORM-2: adet üst sınırı aşıyor.
    string AdetCokBuyuk);

/// <summary>Ürün kartı açılış stoğu sözlüğü.</summary>
public static class UrunAcilisSozlugu
{
    public static readonly UrunAcilisMetinleri Tr = new(
        AcilisCokBuyuk: "Açılış stoğu çok büyük — toplam ağırlık kaydedilebilecek sınırı aşıyor. Adedi azaltın.",
        AcilisKurYok: "Altın fiyatları henüz gelmedi. Birkaç saniye sonra tekrar Kaydet'e basın ya da Kur ekranından fiyat girin.",
        AdetSifirdanBuyuk: "Adet sıfırdan büyük olmalı.",
        AdetCokBuyuk: "Adet çok büyük — en fazla 1.000.000 girilebilir.");

    public static readonly UrunAcilisMetinleri En = new(
        AcilisCokBuyuk: "Opening stock is too large — the total weight exceeds what can be recorded. Lower the quantity.",
        AcilisKurYok: "Gold prices have not arrived yet. Press Save again in a few seconds or enter a price on the Rates screen.",
        AdetSifirdanBuyuk: "Quantity must be greater than zero.",
        AdetCokBuyuk: "Quantity is too large — at most 1,000,000 can be entered.");

    public static UrunAcilisMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
