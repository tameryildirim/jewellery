namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-66 — ETİKET BASIM TASLAĞI (/urunler/etiketler): TR + EN
//
// QA tur 8 (ORTA): 200 ürünlük basım hazırlığı tek yanlış tıkla yok oluyordu,
// kayıp sessizdi. Taslak tarayıcıda (kiracı + kullanıcı anahtarıyla) saklanır;
// bu dosya o akışın ekran metinleridir.
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu); S3-3'ün
//   Sozlukler.Urunler.cs dosyası da başka paketin sahasıdır — ona dokunulmaz.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal tip, Tr ve En aynı kurucudan.
// ⚠ JARGON SIFIR: "taslak" kelimesi tezgâhtarın anladığı "kaldığınız yer" ile
//   birlikte kullanılır; teknik terim (depo, localStorage) ekrana basılmaz.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Etiket basım taslağı metinleri (bir dilde).</summary>
public sealed record EtiketTaslakMetinleri(
    string GeriYuklendi,
    string EksikKalem,
    string TaslagiTemizle,
    string Temizlendi,
    string GeriAl,
    string YazdirmaGonderildi,
    string SaklanamiyorNotu);

public static class EtiketTaslakSozlugu
{
    public static readonly EtiketTaslakMetinleri Tr = new(
        GeriYuklendi: "Kaldığınız yerden devam ediyorsunuz: {0} ürün seçili, {1} kopya.",
        EksikKalem: "{0} kalem artık listede yok; seçimden çıkarıldı.",
        TaslagiTemizle: "Taslağı temizle",
        Temizlendi: "Taslak temizlendi. Seçim, adet ve arama sıfırlandı.",
        GeriAl: "Geri al",
        YazdirmaGonderildi: "Yazdırma penceresi açıldı. Kaydedilen taslak temizlendi.",
        SaklanamiyorNotu: "Bu tarayıcı seçimi saklamıyor. Sayfadan çıkarsanız seçim kaybolur.");

    public static readonly EtiketTaslakMetinleri En = new(
        GeriYuklendi: "Continuing where you left off: {0} products selected, {1} copies.",
        EksikKalem: "{0} items are no longer in the list; removed from the selection.",
        TaslagiTemizle: "Clear draft",
        Temizlendi: "Draft cleared. Selection, copies and search were reset.",
        GeriAl: "Undo",
        YazdirmaGonderildi: "The print window opened. The saved draft was cleared.",
        SaklanamiyorNotu: "This browser does not keep the selection. It is lost if you leave the page.");

    public static EtiketTaslakMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
