namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET P-67 — VİTRİN PANO KALEM KATALOĞU: GEREKÇENİN EKRANDA GÖSTERİLMESİ
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır; bu dalgada başka şeritler de
//   ona dokunuyor (orkestratör koşulu: yeni metin AYRI dosyada —
//   Kuyumcu.Core/I18n/Sozlukler.<Alan>.cs). KurVitrinEmanetSozlugu ile aynı desen.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL record'dadır;
//   Tr ve En aynı kurucudan geçer.
//
// ⚠ GERÇEK: pano kalem kümesi sunucuda PanoAyarServisi.Kalemler =
//   PariteKodlari.Adlar.Keys'ten türer (kur kaynağının parite kod sözlüğü).
//   Kalem eklemek bir VERİ GİRİŞİ değil, kur kaynağı değişikliğidir; metin bunu
//   söyler, "yeni kalem ekle" izlenimi vermez (UX-1 gerekçeli istisna).
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Vitrin ekranına P-67 ile eklenen metinler (bir dilde).</summary>
public sealed record VitrinKalemMetinleri(
    string KaynakAciklama,
    string DuzenlenebilirNot,
    string DuzenlenebilirRozet,
    string YeniKalemYolu,
    string KurKaynagiBaglanti,
    string AtlananYok,
    string Varsayilan,
    string SatirVarsayilanaDon,
    string OnaySoru,
    string OnayEvet,
    string OnayVazgec);

public static class VitrinKalemSozlugu
{
    public static readonly VitrinKalemMetinleri Tr = new(
        KaynakAciklama: "Bu listedeki kalemler kur kaynağının verdiği fiyat kodlarından gelir. Kalem elle eklenmez ya da silinmez; istemediğinizi \"Panoda göster\" ile gizleyin.",
        DuzenlenebilirNot: "Her kalemde değiştirebildikleriniz: ad, sıra, alış ve satış farkı, yuvarlama, panoda göster.",
        DuzenlenebilirRozet: "değiştirilebilir",
        YeniKalemYolu: "Listede olmayan bir kalem mi lazım? Önce kur kaynağına bakın; kalem ancak kaynak o fiyatı verirse eklenebilir.",
        KurKaynagiBaglanti: "Kur kaynağı ayarları →",
        AtlananYok: "Atlanan kalem yok — açık kalemlerin hepsi panoda.",
        Varsayilan: "Varsayılan",
        SatirVarsayilanaDon: "Varsayılana dön",
        OnaySoru: "{0} için yaptığınız ayar silinip varsayılana dönülsün mü?",
        OnayEvet: "Evet, dön",
        OnayVazgec: "Vazgeç");

    public static readonly VitrinKalemMetinleri En = new(
        KaynakAciklama: "Items in this list come from the price codes provided by the rate source. Items are not added or removed by hand; hide the ones you do not want with \"Show on the board\".",
        DuzenlenebilirNot: "What you can change for each item: name, order, buy and sell margin, rounding, show on the board.",
        DuzenlenebilirRozet: "editable",
        YeniKalemYolu: "Need an item that is not listed? Check the rate source first; an item can only be added if the source provides that price.",
        KurKaynagiBaglanti: "Rate source settings →",
        AtlananYok: "No skipped items — every enabled item is on the board.",
        Varsayilan: "Default",
        SatirVarsayilanaDon: "Reset to default",
        OnaySoru: "Delete your settings for {0} and return to default?",
        OnayEvet: "Yes, reset",
        OnayVazgec: "Cancel");

    public static VitrinKalemMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
