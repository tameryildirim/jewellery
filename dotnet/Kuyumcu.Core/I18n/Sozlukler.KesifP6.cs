namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P6 · SİPARİŞ-UYUM-KUR-YÖNETİM — yeni ekran metinleri.
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır ve yalnız P8'indir (orkestratör
//   kuralı: yeni metin AYRI dosyaya). HUB metinleri DEĞİŞMEDİ.
// ⚠ UX-S31 / ekip kararı #2 (#55/#75): görünür metinde ham kod 0 — parite birimi
//   (HAS_GRAM · GRAM · ADET) ve MASAK beyan kademesi (AMAC_BEYANI …) burada çevrilir.
//   Bilinmeyen kod UYDURULMAZ: kodun kendisi döner (Anayasa md.1).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>KESIF-P6 metinleri (bir dilde).</summary>
public sealed record KesifP6Metinleri(
    // Sipariş formu (#35)
    string MusteriBasit,
    string MusteriSecinBasit,
    string MusteriSecinPro,
    string AyarSecin,
    string UrunSecin,
    string KalemBasligi,
    string TeklifAlBasit,
    // Kur pariteleri birimi (#29)
    string BirimHasGram,
    string BirimGram,
    string BirimAdet,
    // Kur kaynağı salt okunur (yetki — 30.09 18:10 ekip oyu)
    string KaynakSahipteDegisir,
    string KaynakYalnizSahip,
    string KaynakIsletmeBaglanti,
    string KaynakEtiket,
    // MASAK beyan kademesi (#86)
    string KademeYok,
    string KademeYalnizKimlik,
    string KademeAmacBeyani,
    string KademeBeyanFormu,
    string KademeBeyanFormuVeBelge,
    // Tur 2 (hakem 01.10): sipariş formu "Daha fazla", teklif sebebi, detay eylem dili
    string SiparisDahaFazla,
    string TeklifUrunGerekli,
    string GecisOnayla,
    string GecisAtolyeyeGonder,
    string GecisHazir,
    string GecisTeslimEt,
    string GecisTaslagaAl);

public static class KesifP6Sozlugu
{
    public static readonly KesifP6Metinleri Tr = new(
        MusteriBasit: "Müşteri",
        MusteriSecinBasit: "Müşteri seçin",
        MusteriSecinPro: "Cari seçin",
        AyarSecin: "Ayar seçin",
        UrunSecin: "Ürün seçin",
        KalemBasligi: "{0}. kalem",
        TeklifAlBasit: "Fiyatı hesapla",
        BirimHasGram: "Has gram",
        BirimGram: "Gram",
        BirimAdet: "Adet",
        KaynakSahipteDegisir: "Kur kaynağı İşletme ayarlarından, onaylanarak değiştirilir.",
        KaynakYalnizSahip: "Kur kaynağını yalnız işletme sahibi değiştirebilir.",
        KaynakIsletmeBaglanti: "İşletme ayarlarına git",
        KaynakEtiket: "Kur kaynağı",
        KademeYok: "Beyan gerekmez",
        KademeYalnizKimlik: "Yalnız kimlik",
        KademeAmacBeyani: "İşlem amacı beyanı",
        KademeBeyanFormu: "Nakit işlem beyan formu",
        KademeBeyanFormuVeBelge: "Beyan formu ve ek belge",
        SiparisDahaFazla: "Daha fazla (söz verilen gün, açıklama, tahmini tutar)",
        TeklifUrunGerekli: "Fiyat için listeden ürün seçin.",
        GecisOnayla: "Onayla",
        GecisAtolyeyeGonder: "Atölyeye gönder",
        GecisHazir: "Hazır olarak işaretle",
        GecisTeslimEt: "Teslim et",
        GecisTaslagaAl: "Taslağa al");

    public static readonly KesifP6Metinleri En = new(
        MusteriBasit: "Customer",
        MusteriSecinBasit: "Select a customer",
        MusteriSecinPro: "Select an account",
        AyarSecin: "Select purity",
        UrunSecin: "Select a product",
        KalemBasligi: "Item {0}",
        TeklifAlBasit: "Calculate price",
        BirimHasGram: "Fine gram",
        BirimGram: "Gram",
        BirimAdet: "Piece",
        KaynakSahipteDegisir: "The rate source is changed, with confirmation, in Business settings.",
        KaynakYalnizSahip: "Only the shop owner can change the rate source.",
        KaynakIsletmeBaglanti: "Go to Business settings",
        KaynakEtiket: "Rate source",
        KademeYok: "No declaration needed",
        KademeYalnizKimlik: "ID only",
        KademeAmacBeyani: "Purpose declaration",
        KademeBeyanFormu: "Cash transaction declaration form",
        KademeBeyanFormuVeBelge: "Declaration form and supporting document",
        SiparisDahaFazla: "More (promised date, note, estimated amount)",
        TeklifUrunGerekli: "Select a product from the list to get a price.",
        GecisOnayla: "Confirm",
        GecisAtolyeyeGonder: "Send to workshop",
        GecisHazir: "Mark as ready",
        GecisTeslimEt: "Hand over",
        GecisTaslagaAl: "Back to draft");

    public static KesifP6Metinleri Al(string dil) => dil == "en" ? En : Tr;

    /// <summary>Parite birimi kodu → görünen ad (#29). Bilinmeyen kod olduğu gibi döner.</summary>
    public static string Birim(string? kod, string dil)
    {
        var m = Al(dil);
        return kod switch
        {
            "HAS_GRAM" => m.BirimHasGram,
            "GRAM" => m.BirimGram,
            "ADET" => m.BirimAdet,
            _ => kod ?? "",
        };
    }

    /// <summary>MASAK beyan kademesi kodu → görünen ad (#86). Bilinmeyen kod olduğu gibi döner.</summary>
    public static string Kademe(string? kod, string dil)
    {
        var m = Al(dil);
        return kod switch
        {
            "YOK" => m.KademeYok,
            "YALNIZ_KIMLIK" => m.KademeYalnizKimlik,
            "AMAC_BEYANI" => m.KademeAmacBeyani,
            "BEYAN_FORMU" => m.KademeBeyanFormu,
            "BEYAN_FORMU_VE_BELGE" => m.KademeBeyanFormuVeBelge,
            _ => kod ?? "",
        };
    }

    /// <summary>
    /// Sipariş durum geçişi düğmesi — EYLEM dili (hakem 01.10: "Onaylandı" değil "Onayla").
    /// Bilinmeyen hedef UYDURULMAZ: null döner, çağıran durum adını basar.
    /// </summary>
    public static string? GecisEylemi(string? hedef, string dil)
    {
        var m = Al(dil);
        return hedef switch
        {
            "ONAYLANDI" => m.GecisOnayla,
            "ATOLYEDE" => m.GecisAtolyeyeGonder,
            "HAZIR" => m.GecisHazir,
            "TESLIM_EDILDI" => m.GecisTeslimEt,
            "TASLAK" => m.GecisTaslagaAl,
            _ => null,
        };
    }

    /// <summary>
    /// Uyruk kodu (ISO 3166-1 alpha-3, ör. TUR) → ülke adı (hakem 01.10: "Uyruk TUR" ham kodu).
    /// Tek kaynak MASAK formunun ülke listesidir (<see cref="UlkeListesi"/>); listede olmayan
    /// kod UYDURULMAZ, olduğu gibi döner (Anayasa md.1).
    /// </summary>
    public static string Uyruk(string? kod, string dil)
    {
        if (string.IsNullOrWhiteSpace(kod)) return "";
        var u = UlkeListesi.Tumu.FirstOrDefault(x => string.Equals(x.Kod, kod, StringComparison.OrdinalIgnoreCase));
        return u is null ? kod : (dil == "en" ? u.En : u.Tr);
    }
}
