namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET Q10-2 — /hurda RAMAT ÇIKIŞI ONAYI + BAKİYE MESAJI (QA turu 10, 24.09)
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: yeni
//   metin Sozlukler.<Alan>.cs). Burada YALNIZ bu paketin yeni metinleri durur.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; Tr ve En aynı
//   kurucudan geçer.
// ⚠ DÜĞME ADI METNE GÖMÜLMEZ: düzeltme yolundaki düğme adı {0} yer tutucusuyla
//   ekrandaki GERÇEK düğme etiketinden gelir (Dil.T.Hurda.Duzelt).
// ⚠ Mevzuat hükmü YOKTUR: bu dosyada eşik/oran/kod yazılı değildir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Q10-2 metinleri (bir dilde).</summary>
public sealed record HurdaOnayMetinleri(
    // ── Onay penceresi ──
    string OnayBaslik,
    string OnayDugmesi,
    string OnayAciklama,
    string OnayDuzeltmeYolu,
    string OzetBizimKaydimiz,
    string OzetKabul,
    string OzetBirimFiyat,
    string OzetTutar,
    string OzetTahsil,
    string OzetKasa,
    // ── Bakiye mesajı (QA 10: "bakiye yok" yanlış sebep söylüyordu) ──
    string BakiyeAsildi,
    // ── Q10-2 düzeltme turu — BASİT mod jargonsuz metinler (K8: cari/mutabakat).
    //    Yalnız Dil.BasitMi iken kullanılır; PRO metni Sozlukler.cs HUB'ında KALIR.
    string TahlilFarkiBasit,
    string TahlilFarkiNotuBasit,
    string RamatciBasit,
    string DegismezNotuBasit,
    string DuzeltmeNotuBasit);

/// <summary>Q10-2 sözlüğü.</summary>
public static class HurdaOnaySozlugu
{
    public static readonly HurdaOnayMetinleri Tr = new(
        OnayBaslik: "Ramat çıkışı onayı",
        OnayDugmesi: "Kaydet",
        OnayAciklama: "Bu hurda satışı kaydedildikten sonra değiştirilemez ve silinemez. Bilgileri kontrol edin.",
        OnayDuzeltmeYolu: "Yanlış çıkarsa: has farkı defterdeki “{0}” düğmesiyle eklenir; para tarafı müşteri düzeltme fişiyle düzeltilir.",
        OzetBizimKaydimiz: "Bizim kaydımız",
        OzetKabul: "Alıcının kabul ettiği",
        OzetBirimFiyat: "Birim fiyat",
        OzetTutar: "Tutar",
        OzetTahsil: "Tahsil edilen",
        OzetKasa: "Kasa",
        BakiyeAsildi: "Kalan hurdayı aşıyorsunuz ({0} mevcut)",
        TahlilFarkiBasit: "Tahlil farkı",
        TahlilFarkiNotuBasit: "Fark ayrı bir defter satırına yazılır; para tutarı değişmez. Bu bir eritme firesi DEĞİLDİR — alıcının tahlil ölçümü ile bizim kaydımız arasındaki farktır.",
        RamatciBasit: "Ramatçı / rafineri (kayıtlı müşteri)",
        DegismezNotuBasit: "Hurda satışı kaydedildikten sonra değiştirilmez ve silinmez: hurdanın stok satırı yoktur. Yanlış çıkış, ramatçıyla anlaşılan has farkı eklenerek düzeltilir.",
        DuzeltmeNotuBasit: "Defter satırının üzerine yazılmaz: aynı hurda satışına ikinci bir fark satırı eklenir, toplam fark satırların toplamıdır. Tahsilat tutarı bu yolla değişmez; para tarafı için müşteri düzeltme fişini kullanın.");

    public static readonly HurdaOnayMetinleri En = new(
        OnayBaslik: "Confirm the scrap sale",
        OnayDugmesi: "Save",
        OnayAciklama: "Once saved, this scrap sale cannot be edited or deleted. Please check the details.",
        OnayDuzeltmeYolu: "If it turns out wrong: the gram difference is added with the “{0}” button in the ledger; the cash side is corrected with a customer-account correction voucher.",
        OzetBizimKaydimiz: "Our record",
        OzetKabul: "Accepted by buyer",
        OzetBirimFiyat: "Unit price",
        OzetTutar: "Amount",
        OzetTahsil: "Collected",
        OzetKasa: "Register",
        BakiyeAsildi: "More than the remaining scrap ({0} available)",
        TahlilFarkiBasit: "Assay difference",
        TahlilFarkiNotuBasit: "The difference is written on a separate ledger line; the cash amount does not change. This is NOT a melting loss — it is the gap between the buyer's assay and our record.",
        RamatciBasit: "Refinery (saved customer)",
        DegismezNotuBasit: "A scrap sale cannot be edited or deleted once saved: scrap has no stock line. A wrong exit is corrected by adding the gram difference agreed with the refinery.",
        DuzeltmeNotuBasit: "The ledger line is not overwritten: a second difference line is added to the same scrap sale; the total difference is the sum of the lines. The collected amount does not change this way; use a customer correction voucher for the cash side.");

    public static HurdaOnayMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
