namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ŞUBE-KAPSAM-B — SEÇİLİ ŞUBE BÜTÜN EKRANI YÖNETİR (istemci)
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır; aynı dalgada başka şeritler
//   de ona dokunuyor (orkestratör koşulu: yeni metin Sozlukler.<Paket>.cs).
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; Tr ve En aynı
//   kurucudan geçer.
// ⚠ JARGON SIFIR: "görünüm şubesi", "kiracı", "tercih" ekrana yazılmaz.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Görünüm şubesi rozeti, uyarı bantları ve şubeli ekran metinleri (bir dilde).</summary>
public sealed record SubeKapsamBMetinleri(
    // ── Rozet (başlık altı tek satır) ──
    string RozetSube,
    string RozetTumu,
    string RozetMusteriHesaplari,
    // ── Bantlar ──
    string GecersizTercihSubeye,
    string GecersizTercihTumune,
    string KayitHatasi,
    // ── Şubeli ekranlar ──
    string SubeSutunu,
    string HurdaTumSubelerBos,
    string HurdaFormKendiSubende,
    // ── Tur 2 (doğrulayıcı): birleşik hurda kırılımı + kiracı geneli kart notu ──
    string HurdaSubeKirilimi,
    string KartTumSubelerNotu,
    // ── Tur 3 (doğrulayıcı): menü etiketi, bant kapatma, hurda satış şubesi ──
    string HangiSube,
    string BandiKapat,
    string GecersizTemizlenemedi,
    string HurdaSatisKendiSubede,
    string HurdaKendiSubeyeGec);

public static class SubeKapsamBSozlugu
{
    public static readonly SubeKapsamBMetinleri Tr = new(
        RozetSube: "Gösterilen şube: {0}",
        RozetTumu: "Gösterilen: tüm şubeler (birlikte)",
        RozetMusteriHesaplari: "Müşteri hesapları şubeye bölünmez — tüm şubeler birlikte",
        GecersizTercihSubeye: "Seçtiğiniz şube kapalı; kendi şubenize dönüldü.",
        GecersizTercihTumune: "Seçtiğiniz şube kapalı; tüm şubeler gösteriliyor.",
        KayitHatasi: "Şube seçimi kaydedilemedi, önceki seçim geri geldi: {0}",
        SubeSutunu: "Şube",
        HurdaTumSubelerBos: "Hiçbir şubede hurda hareketi yok: henüz hurda alışı girilmemiş.",
        HurdaFormKendiSubende: "Satış formu kendi şubenizin ({0}) hurdasından satar; üstteki kartlar seçili şubeyi gösterir.",
        HurdaSubeKirilimi: "Şubelere göre",
        KartTumSubelerNotu: "Tüm şubeler birlikte",
        HangiSube: "Hangi şube?",
        BandiKapat: "Tamam",
        GecersizTemizlenemedi: "Seçim kaydedilemedi, bir sonraki girişte bu uyarı yeniden çıkabilir: {0}",
        HurdaSatisKendiSubede: "Hurda satışı yalnız kendi şubenizden ({0}) yapılır. Şu an başka şube gösteriliyor.",
        HurdaKendiSubeyeGec: "Kendi şubeme geç ({0})");

    public static readonly SubeKapsamBMetinleri En = new(
        RozetSube: "Showing branch: {0}",
        RozetTumu: "Showing: all branches (combined)",
        RozetMusteriHesaplari: "Customer accounts are not split by branch — all branches combined",
        GecersizTercihSubeye: "The branch you chose is closed; switched back to your own branch.",
        GecersizTercihTumune: "The branch you chose is closed; showing all branches.",
        KayitHatasi: "Branch choice could not be saved, the previous choice is back: {0}",
        SubeSutunu: "Branch",
        HurdaTumSubelerBos: "No scrap movement in any branch: no scrap purchase has been entered yet.",
        HurdaFormKendiSubende: "The sale form sells from your own branch's ({0}) scrap; the cards above show the selected branch.",
        HurdaSubeKirilimi: "By branch",
        KartTumSubelerNotu: "All branches combined",
        HangiSube: "Which branch?",
        BandiKapat: "OK",
        GecersizTemizlenemedi: "The choice could not be saved; this notice may appear again next time: {0}",
        HurdaSatisKendiSubede: "Scrap can only be sold from your own branch ({0}). Another branch is shown now.",
        HurdaKendiSubeyeGec: "Switch to my branch ({0})");

    public static SubeKapsamBMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
