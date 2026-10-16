namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET CUMA-S — SATIŞ BASİT: ALDIM (ödeme) SÜTUNU SADE DURUM SATIRI · TR + EN
//
// Kaynak: orkestratör görsel kontrolü 25.09 03:05 (a,b) — basit modda nakit
// toplam tevsik haddine ulaşınca ALDIM sütununda ödeme türü seçici + uzun
// kırmızı metin + alınan/para üstü + düğmeler üst üste yığılıyor, alt eylem
// satırı 1440×900'de ekranın altına taşıyordu.
// Çözüm: basit tezgâh fişinde tek satır kısa durum + katlanır "Neden?" ayrıntısı.
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record.
// ⚠ JARGON YOK (hedef kitle tezgâhtar): "tevsik", "459 s. VUK GT", "HAVALE/EFT"
//   basit yüzde yazılmaz; PRO metni (Sozlukler.cs TevsikNakitOnerisi) DEĞİŞMEDİ.
// ⚠ EŞİK SAYISI YOK (MEVZUAT KURALI / Mehmet K2): ayrıntıdaki tutar {0} yer
//   tutucusudur ve sayfanın SUNUCUDAN aldığı haddir (/tevsik/gunluk).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Basit tezgâh fişinin ALDIM sütunundaki sade yasal durum metinleri.</summary>
public sealed record SatisAldimMetinleri(
    string DurumKimlik,
    string DurumNakitYok,
    string Neden,
    string Ayrinti,
    string AyrintiPesin);

/// <summary>PAKET CUMA-S — ALDIM sütunu sade durum sözlüğü (TR + EN).</summary>
public static class SatisAldimSozlugu
{
    public static readonly SatisAldimMetinleri Tr = new(
        DurumKimlik: "Müşteri seçin",   // t3 hakemi: "Kimlik gerekli" MASAK kimlik eşiğiyle çelişiyordu; "Neden?" nakit sınırını söyler
        DurumNakitYok: "Nakit alınmaz",
        Neden: "Neden?",
        Ayrinti: "Bu tutar nakit sınırının ({0}) üstünde; nakit kabul edilmez. Ödemeyi kart ya da havale yapın.",
        AyrintiPesin: "Peşin Müşteri ile bu tutarda satış yapılmaz: müşteriyi seçin ya da ekleyin.");

    public static readonly SatisAldimMetinleri En = new(
        DurumKimlik: "Pick the customer",
        DurumNakitYok: "No cash",
        Neden: "Why?",
        Ayrinti: "This amount is above the cash limit ({0}); cash is not accepted. Take card or bank transfer.",
        AyrintiPesin: "A walk-in customer cannot buy this amount: pick or add the customer.");

    public static SatisAldimMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
