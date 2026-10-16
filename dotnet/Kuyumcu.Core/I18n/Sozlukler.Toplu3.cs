namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET TOPLU-3 — demo videosu bulguları + RAPOR-YASLANDIRMA tasarım YÜKSEK'leri
// + TOPLU-2 YARIM kalanları (01.10). YALNIZ GÖRÜNÜM: metinler bugünkü davranışı
// sade dille anlatır; hiçbir yasal kural, eşik ya da kaynak seçimi DEĞİŞMEZ.
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör kuralı: yeni metin
//   paketin kendi dosyasına). Ekran bu dosyadaki karşılığı okur.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>TOPLU-3 metinleri (bir dilde).</summary>
public sealed record Toplu3Metinleri(
    // ── Panel yasal liste bandı (demo bulgusu): uyarı GİZLENMEZ (liste gerçekten yok =
    //    doğru yasal uyarı, fail-closed); dili sade ve eyleme dönük + ayrıntı bağlantısı. ──
    string PanelListeYok,
    string PanelListeEski,
    string PanelListeGit,
    // ── Kur ekranı kaynak rozeti (demo bulgusu): "Deneme" sözcüğü müşteriye ürünün
    //    denemelik olduğunu düşündürüyordu. Rozet yalnız sözleşmesiz kaynak seçiliyken çıkar
    //    (Kur:SozlesmesizKaynaklarAcik — üretimde KAPALI); davranış aynı, dil sade. ──
    string KurUcretsizRozet,
    string KurUcretsizIpucu,
    // ── Cari yaşlandırma (RAPOR-YASLANDIRMA tasarım hakemi YÜKSEK/ORTA) ──
    string YasDovizBizeBorclu,
    string YasDovizBizBorcluyuz,
    string YasDovizOzetKisa,
    string YasIpucuAc,
    // ── /kurulum (tasarım hakemi KRİTİK, TOPLU-2 YARIM): açılışta yalnız adım listesi; açılış
    //    bakiyesi ve mühür bölümleri KAPALI akordeon; "KURU PROVA" jargonu yerine "Kontrol et"
    //    (ZO-BELGE #16 ile aynı sade karşılık); "bu sürümde yok" yerine "Yakında" rozeti. ──
    string KurulumStokAdi,
    string KurulumYakinda,
    string KurulumAcilisOzet,
    string KurulumAcilisIpucu,
    string KurulumKontrolEt,
    string KurulumKontrolSonucu,
    string KurulumMuhurOzet);

public static class Toplu3Sozlugu
{
    public static readonly Toplu3Metinleri Tr = new(
        PanelListeYok: "Yasal liste henüz yüklenmedi. Yüklenmesi için destek hattını arayın.",
        PanelListeEski: "Yasal liste {0} gündür güncellenmedi. Güncellenmesi için destek hattını arayın.",
        PanelListeGit: "Ayrıntı",
        KurUcretsizRozet: "Ücretsiz kur kaynağı",
        KurUcretsizIpucu: "Kur bilgisi ücretsiz, herkese açık bir kaynaktan geliyor. Kaynağın kullanım şartını işletme sahibi görür.",
        YasDovizBizeBorclu: BorcDiliSozlugu.BizeBorcluKalip("tr"), // P-DIL-BORC-KASA
        YasDovizBizBorcluyuz: BorcDiliSozlugu.BizBorcluyuzKalip("tr"),
        YasDovizOzetKisa: "Döviz bakiyeleri",
        YasIpucuAc: "Açıklama",
        KurulumStokAdi: "Stok açılışı",
        KurulumYakinda: "Yakında",
        KurulumAcilisOzet: "Kasa, banka ve müşteri bakiyelerinizi girin.",
        KurulumAcilisIpucu: "Önce \"Kontrol et\" düğmesine basın: sistem satırları kontrol eder, hiçbir kayıt oluşturmaz. Sorun yoksa \"Açılışı uygula\" ile kaydedin.",
        KurulumKontrolEt: "Kontrol et",
        KurulumKontrolSonucu: "Kontrol sonucu",
        KurulumMuhurOzet: "Her şey tamamsa kurulumu bitirin. Bundan sonra açılış bakiyeleri değiştirilemez.");

    public static readonly Toplu3Metinleri En = new(
        PanelListeYok: "The legal list has not been loaded yet. Call support to have it loaded.",
        PanelListeEski: "The legal list has not been updated for {0} days. Call support to have it updated.",
        PanelListeGit: "Details",
        KurUcretsizRozet: "Free rate source",
        KurUcretsizIpucu: "Rates come from a free, public source. The shop owner can see the source's terms of use.",
        YasDovizBizeBorclu: BorcDiliSozlugu.BizeBorcluKalip("en"),
        YasDovizBizBorcluyuz: BorcDiliSozlugu.BizBorcluyuzKalip("en"),
        YasDovizOzetKisa: "Foreign currency balances",
        YasIpucuAc: "Explanation",
        KurulumStokAdi: "Stock opening",
        KurulumYakinda: "Coming soon",
        KurulumAcilisOzet: "Enter your cash, bank and customer balances.",
        KurulumAcilisIpucu: "First press \"Check\": the system checks the rows and creates nothing. If all is well, save with \"Apply opening\".",
        KurulumKontrolEt: "Check",
        KurulumKontrolSonucu: "Check result",
        KurulumMuhurOzet: "When everything is ready, finish the setup. After that, opening balances cannot be changed.");

    public static Toplu3Metinleri Al(string dil) => dil == "en" ? En : Tr;
}
