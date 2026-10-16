namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET P-69 — "toplamHas" ADLANDIRMASI: TR + EN METİNLER
//
// ⚠ SORUN (QA turu 8): /raporlar/aylik `satis.toplamHas` ölçüldüğünde YALNIZ
//   ALTINDIR (Paket 20) ama ekran etiketi "Toplam Has" diyordu. Gümüş de
//   satılan bir ayda kullanıcı 32,76 g'yi tüm madenlerin toplamı sanıp gümüşü
//   ya iki kez sayıyor ya da eksik görüyordu. Veri doğru, AD yanlış.
//   Bu dosyadaki etiketler "toplam" demez; hangi madeni saydığını SÖYLER.
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır; aynı dalgada başka şeritler
//   de ona dokunuyor (orkestratör koşulu). RaporGrafikSozlugu ile aynı desen.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL bir record'da
//   durur; Tr ve En aynı kurucudan geçer.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Rapor ekranındaki has gram etiketleri (bir dilde).</summary>
public sealed record RaporHasMetinleri(
    string HasAltin,
    string NetHasAltin,
    string MadenHasBaslik,
    string StokHasBaslik,
    string ReyonHasBaslik,
    string MadenUyarisi);

public static class RaporHasSozlugu
{
    public static readonly RaporHasMetinleri Tr = new(
        HasAltin: "Has (altın)",
        NetHasAltin: "Net has gram hareketi — Has (altın)",
        MadenHasBaslik: "Has gram (satırdaki maden)",
        StokHasBaslik: "Has gram (ürünün madeni)",
        ReyonHasBaslik: "Has gram (madene göre)",
        MadenUyarisi: "Has (altın) kartı yalnız ALTINI sayar; diğer madenler bu tabloda ayrı satırdadır — farklı madenlerin gramları toplanmaz.");

    public static readonly RaporHasMetinleri En = new(
        HasAltin: "Fine (gold)",
        NetHasAltin: "Net fine-gold movement — Fine (gold)",
        MadenHasBaslik: "Fine grams (metal of the row)",
        StokHasBaslik: "Fine grams (product's metal)",
        ReyonHasBaslik: "Fine grams (by metal)",
        MadenUyarisi: "The Fine (gold) card counts GOLD only; other metals are on separate rows in this table — grams of different metals are never added together.");

    public static RaporHasMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
