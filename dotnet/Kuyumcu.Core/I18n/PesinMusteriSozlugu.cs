namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-3 (karar D2) — "PEŞİN MÜŞTERİ" SİSTEM CARİSİ: TR + EN METİNLER
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs 9200+ satırlık ORTAK HUB'dır ve aynı dalgada
//   başka şeritler de ona dokunuyor (orkestratör koşulu: "yeni metin için AYRI
//   dosya aç"). PozisyonSozlugu / GorunumSozlugu ile aynı desen.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL bir record'da
//   durur; Tr ve En aynı kurucudan geçer.
//
// ⚠ EŞİK SAYISI BURADA YOK (PLAN § 3 md.6, D2 K2): "kimlik gerektiren tutar"
//   sunucunun kararıdır; hangi sayıda olduğu sunucunun 400 mesajında yazar.
//   dotnet/denetim.sh B-3 bloğu (ESIK_ISTEMCIDE) istemciye sayı sızmasını sayar.
//
// ⚠ Jargon sıfır (PLAN § 5B): "sistem carisi" kelimesi ekrana çıkmaz;
//   tezgâhtar "Peşin Müşteri" görür.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Peşin Müşteri kartının bir dildeki tüm metinleri.</summary>
public sealed record PesinMusteriMetinleri(
    string Ad,
    string Rozet,
    string SeciliNot,
    string YuklemeHatasi,
    string DegismezGerekce,
    string DegismezYol);

public static class PesinMusteriSozlugu
{
    public static readonly PesinMusteriMetinleri Tr = new(
        Ad: "Peşin Müşteri",
        Rozet: "Sistem kartı",
        SeciliNot: "Peşin Müşteri seçili: adı bilinmeyen müşteriye peşin satış. "
                 + "Kimlik gerektiren bir tutarda program gerçek müşteri seçmenizi ister; "
                 + "bu karta borç (veresiye) yazılamaz.",
        YuklemeHatasi: "Peşin Müşteri kartı yüklenemedi — müşteriyi listeden seçin.",
        DegismezGerekce: "Peşin Müşteri programın kendi kartıdır: silinemez, pasife alınamaz, borç ya da alacak taşımaz.",
        DegismezYol: "Adını Düzenle'den değiştirebilirsiniz. Kimliği bilinen müşteri için yeni müşteri kartı açın.");

    public static readonly PesinMusteriMetinleri En = new(
        Ad: "Walk-in Customer",
        Rozet: "System account",
        SeciliNot: "Walk-in Customer selected: cash sale to an unnamed customer. "
                 + "For an amount that requires identification the program asks you to pick a real customer; "
                 + "no credit (on-account) balance can be written to this account.",
        YuklemeHatasi: "The Walk-in Customer account could not be loaded — pick the customer from the list.",
        DegismezGerekce: "Walk-in Customer is the program's own account: it cannot be deleted or deactivated and never carries a balance.",
        DegismezYol: "You can rename it via Edit. Open a new customer account for a customer whose identity is known.");

    /// <summary>Seçili dilin metinleri.</summary>
    public static PesinMusteriMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
