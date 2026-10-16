namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-PARAM-2 — bugünden geçerli yanlış girişin AYNI GÜN düzeltilmesi
// (ByGatech sistem yöneticisi formu) metinleri (TR + EN).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır; YasalParamMetinleri'ne alan
//   eklemek paralel şeritlerle çakışma yüzeyini büyütür.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; Tr ve En aynı kurucudan.
// ⚠ Rakam METNE GÖMÜLMEZ: değerler ve saatler {0}/{1} yer tutucularına SUNUCUDAN yazılır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Aynı gün düzeltme metinleri (bir dilde).</summary>
public sealed record YasalDuzeltMetinleri(
    string Baslik,
    string Aciklama,
    string AlanSatir,
    string SatirTek,
    string AlanDogruDeger,
    string AlanGerekce,
    string Onizle,
    string Onizleniyor,
    string ZorunluSatir,
    string ZorunluDeger,
    string AyniDeger,
    string OnayBaslik,
    string OnayDeger,
    string OnayEtki,
    string OnaylaDuzelt,
    string Duzeltiliyor,
    string Basari,
    string HataOnizleme,
    string HataKayit);

public static class YasalDuzeltSozlugu
{
    public static readonly YasalDuzeltMetinleri Tr = new(
        Baslik: "Bugün girilen değeri düzelt",
        Aciklama: "Bugün girdiğiniz değer yanlışsa düzeltin; önceki işlemler değişmez.",
        AlanSatir: "Düzeltilecek değer",
        SatirTek: "{0}: {1} (bugün {2} itibarıyla geçerli)",
        AlanDogruDeger: "Doğru değer",
        AlanGerekce: "Düzeltme gerekçesi",
        Onizle: "Düzeltmeyi önizle",
        Onizleniyor: "Önizleniyor…",
        ZorunluSatir: "Düzeltilecek değeri seçin.",
        ZorunluDeger: "Doğru değeri yazın.",
        AyniDeger: "Doğru değer, düzeltilecek değerle aynı.",
        OnayBaslik: "Düzeltmeyi onayla",
        OnayDeger: "{0}: yanlış {1} → doğru {2}",
        OnayEtki: "Yanlış değer bugün {0} itibarıyla geçerliydi; o saatten bu ana kadarki işlemler değişmez. Doğru değer onayladığınız andan geçerli olur.",
        OnaylaDuzelt: "Onayla ve düzelt",
        Duzeltiliyor: "Düzeltiliyor…",
        Basari: "Düzeltildi: {0} {1} → {2}, bugün {3} itibarıyla geçerli. {4}–{3} arasındaki işlemler değişmedi.",
        HataOnizleme: "Önizlenemedi: {0}",
        HataKayit: "Düzeltilemedi: {0}");

    public static readonly YasalDuzeltMetinleri En = new(
        Baslik: "Correct the value entered today",
        Aciklama: "If the value you entered today is wrong, correct it; earlier transactions do not change.",
        AlanSatir: "Value to correct",
        SatirTek: "{0}: {1} (valid from {2} today)",
        AlanDogruDeger: "Correct value",
        AlanGerekce: "Reason for the correction",
        Onizle: "Preview the correction",
        Onizleniyor: "Previewing…",
        ZorunluSatir: "Choose the value to correct.",
        ZorunluDeger: "Enter the correct value.",
        AyniDeger: "The correct value is the same as the value to correct.",
        OnayBaslik: "Confirm the correction",
        OnayDeger: "{0}: wrong {1} → correct {2}",
        OnayEtki: "The wrong value was valid from {0} today; transactions from then until now do not change. The correct value is valid from the moment you confirm.",
        OnaylaDuzelt: "Confirm and correct",
        Duzeltiliyor: "Correcting…",
        Basari: "Corrected: {0} {1} → {2}, valid from {3} today. Transactions between {4} and {3} did not change.",
        HataOnizleme: "Could not preview: {0}",
        HataKayit: "Could not correct: {0}");

    public static YasalDuzeltMetinleri Al(string? dil) => dil == "en" ? En : Tr;
}
