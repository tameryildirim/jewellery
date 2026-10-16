namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET TOPLU-4 — toplu tasarım kuyruğu (TOPLU-2 t2 · ŞUBE-KAPSAM-B t2 · TOPLU-2
// kapanışı · TOPLU-3 t2 kalanları). YALNIZ GÖRÜNÜM: metinler bugünkü davranışı
// sade dille anlatır; hiçbir yasal kural, eşik, kaynak seçimi ya da hesap DEĞİŞMEZ.
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör kuralı: yeni metin
//   paketin kendi dosyasına). Ekran bu dosyadaki karşılığı okur.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>TOPLU-4 metinleri (bir dilde).</summary>
public sealed record Toplu4Metinleri(
    // ── (12) /kurulum adım listesi: sayı tek başına basılmaz, neyin sayısı olduğu yazılır ──
    string KurulumSayiSube,
    string KurulumSayiKullanici,
    string KurulumSayiKur,
    string KurulumSayiKasaBanka,
    string KurulumSayiCariAcilis,
    // ── (3) Vitrin panosu: açıklamalar tek ⓘ içinde; "Sunucu sınırı:" teknik öneki yok ──
    string VitrinTavanNotu,
    string VitrinPanoBilgi,
    string VitrinAyarBilgi,
    // ── (5) Liste sınırı: ayrı 12 px paragraf yerine sayaç satırı ("12 kayıt · en çok 50") ──
    string ListeSayac,
    // ── (8) Kur / Vade / Uyum açıklama satırları ⓘ'ye ──
    string KurUcretsizBilgi,
    string VadeUfukBilgi,
    string UyumRiskKisa,
    string UyumRiskBilgi,
    // ── (11) Yaşlandırma has notu ⓘ'ye ──
    string YasHasNotKisa,
    // ── (14) Kullanıcı kartı: çok şubeli görünümde bakılan şube ──
    string KartGorunumIpucu,
    // ── (8, tur 2 — tasarım hakemi 02.10) Vade takibi: kart alt metni kısa, açıklama ⓘ; serbest gün "+ Başka gün" arkasında;
    //    değişmezlik notu tek satır + ⓘ (bugünkü davranış AYNI; yalnız metin/yerleşim) ──
    string VadeBaskaGun,
    string VadeVadesizKisa,
    string VadeVadesizBilgi,
    string VadeVadesizAciklama,
    string VadeDegismezKisa,
    string VadeDegismezBilgi);

/// <summary>TOPLU-4 sözlüğü — TR ve EN aynı kurucudan (eksik anahtar derleme hatası).</summary>
public static class Toplu4Sozlugu
{
    public static readonly Toplu4Metinleri Tr = new(
        KurulumSayiSube: "{0} şube",
        KurulumSayiKullanici: "{0} kullanıcı",
        KurulumSayiKur: "{0} kur kaydı",
        KurulumSayiKasaBanka: "{0} kasa ya da banka hesabı",
        KurulumSayiCariAcilis: "{0} müşteri açılışı",
        VitrinTavanNotu: "Panoda en çok {0} satır gösterilir.",
        VitrinPanoBilgi: "Pano hakkında",
        VitrinAyarBilgi: "Ayarlar hakkında",
        ListeSayac: "{0} kayıt gösteriliyor · en çok {1}",
        KurUcretsizBilgi: "Kur kaynağı hakkında",
        VadeUfukBilgi: "Kaç gün ilerisi",
        UyumRiskKisa: "Risk puanı bilgi içindir; satışı durdurmaz.",
        UyumRiskBilgi: "Risk puanı hakkında",
        YasHasNotKisa: "Has bakiyesi sütunu",
        KartGorunumIpucu: "Bakılan şube — menüden değiştirilir",
        VadeBaskaGun: "+ Başka gün",
        VadeVadesizKisa: "Vadesi girilmemiş",
        VadeVadesizBilgi: "Vadesi girilmemiş borçlar hakkında",
        VadeVadesizAciklama: "Bu borçlar için vade günü girilmemiş; bu yüzden gecikmiş ya da yaklaşan sayılmaz.",
        VadeDegismezKisa: "Rakamlar işlemlerden gelir; burada düzeltilmez.",
        VadeDegismezBilgi: "Nasıl düzeltilir?");

    public static readonly Toplu4Metinleri En = new(
        KurulumSayiSube: "{0} branch(es)",
        KurulumSayiKullanici: "{0} user(s)",
        KurulumSayiKur: "{0} rate(s)",
        KurulumSayiKasaBanka: "{0} account(s)",
        KurulumSayiCariAcilis: "{0} customer(s)",
        VitrinTavanNotu: "The board shows at most {0} rows.",
        VitrinPanoBilgi: "About the board",
        VitrinAyarBilgi: "About the settings",
        ListeSayac: "{0} records shown · at most {1}",
        KurUcretsizBilgi: "About the rate source",
        VadeUfukBilgi: "How many days ahead",
        UyumRiskKisa: "The risk score is informational; it never stops a sale.",
        UyumRiskBilgi: "About the risk score",
        YasHasNotKisa: "Fine gold balance column",
        KartGorunumIpucu: "Branch being viewed — change it from the menu",
        VadeBaskaGun: "+ Other days",
        VadeVadesizKisa: "No due date entered",
        VadeVadesizBilgi: "About debts with no due date",
        VadeVadesizAciklama: "No due date was entered for these debts, so they are not counted as overdue or due soon.",
        VadeDegismezKisa: "Figures come from transactions; they are not corrected here.",
        VadeDegismezBilgi: "How to correct?");

    public static Toplu4Metinleri Al(string dil) => dil == "en" ? En : Tr;
}
