namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P5 · SATIŞ-VEZNE + SATIS-4 — yeni ekran metinleri.
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır ve yalnız P8'indir (orkestratör
//   kuralı: yeni metin AYRI dosyaya). HUB metinleri DEĞİŞMEDİ; BASİT yüz için
//   sade karşılık burada durur (ör. "Kur tahsil anında sunucudan alınır" → BASİT'te
//   "sunucu" sözcüğü yok, KESIF #17).
// ⚠ Eşik SAYISI metne gömülmez (MEVZUAT KURALI): nakit sınırı cümlesi sayıyı
//   yer tutucuyla alır; sayı sunucunun bildirdiği haddir (/tevsik/gunluk).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>KESIF-P5 metinleri (bir dilde).</summary>
public sealed record KesifP5Metinleri(
    string KurTahsilAnindaBasit,
    string OdemeOtomatikKart,
    string OdemeOtomatikHavale,
    string KimlikAsagidaAlinir,
    string KimlikAdresGerekli,
    string SonKayit,
    string Stok,
    string StokYok,
    string StokBilinmiyor);

public static class KesifP5Sozlugu
{
    public static readonly KesifP5Metinleri Tr = new(
        KurTahsilAnindaBasit: "Fiyat, tahsil anındaki güncel kurdan hesaplanır.",
        // SATIS-4 (a): tutar nakit sınırını aşınca ödeme kendiliğinden nakit dışına döner (sayı sunucudan).
        OdemeOtomatikKart: "Tutar {0} nakit sınırını aştığı için ödeme Kart olarak seçildi. İsterseniz değiştirebilirsiniz.",
        OdemeOtomatikHavale: "Tutar {0} nakit sınırını aştığı için ödeme Havale olarak seçildi. İsterseniz değiştirebilirsiniz.",
        // SATIS-4 (b): tek kimlik formu — satıcının kimliği yasal kimlik kutusundan alınır.
        KimlikAsagidaAlinir: "Satıcının adı, kimlik numarası ve adresi aşağıdaki müşteri kimliği kutusundan alınır — ikinci kez yazılmaz.",
        KimlikAdresGerekli: "Satıcının adresi zorunludur — aşağıdaki müşteri kimliği kutusunda Adres alanını doldurun.",
        // SATIS-4 (d): kalıcı son kayıt satırı (bir sonraki kayda kadar durur).
        SonKayit: "{0} · {1} · {2}",
        // SATIS-4 (c): ürün arama satırı.
        Stok: "Stok {0}",
        StokYok: "Stokta yok",
        StokBilinmiyor: "Stok —");

    public static readonly KesifP5Metinleri En = new(
        KurTahsilAnindaBasit: "The price is calculated from the current rate at checkout.",
        OdemeOtomatikKart: "The amount is above the {0} cash limit, so payment was set to Card. You can change it.",
        OdemeOtomatikHavale: "The amount is above the {0} cash limit, so payment was set to Bank transfer. You can change it.",
        KimlikAsagidaAlinir: "The seller's name, ID number and address are taken from the customer ID box below — no need to type them twice.",
        KimlikAdresGerekli: "The seller's address is required — fill in the Address field in the customer ID box below.",
        SonKayit: "{0} · {1} · {2}",
        Stok: "Stock {0}",
        StokYok: "Out of stock",
        StokBilinmiyor: "Stock —");

    public static KesifP5Metinleri Al(string dil) => dil == "en" ? En : Tr;
}
