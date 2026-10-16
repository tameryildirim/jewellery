using Kuyumcu.Api.Common;

namespace Kuyumcu.Api.Modules.Urun;

// ═════════════════════════════════════════════════════════════════════════════
// TAŞ KÖKENİ KAPISI — Kuyum Ticareti Yönetmeliği md.11/(e) · PLAN-2 Adım 3
//
// FAIL-CLOSED: taş ağırlığı olan bir ürün, kökeni belirtilmeden SATIŞA
// SUNULAMAZ. Yönetmelik "açıkça yer vermeksizin satışa sunamaz" der; bilgi
// eksikse ürün doğal SAYILMAZ — işlem durur.
//
// MASAK ve tevsik kapılarıyla aynı desen: saf doğrulama, I/O yok, istisna
// fırlatır ve çağıran transaction'ı hiç açmaz.
// ═════════════════════════════════════════════════════════════════════════════

public static class TasKapisi
{
    /// <summary>
    /// Ürün kaydedilirken/güncellenirken köken tutarlılığını doğrular.
    /// </summary>
    /// <param name="tasAgirlik">Ürünün taş ağırlığı (gram). 0 veya null → taşsız.</param>
    /// <param name="koken">Bildirilen köken.</param>
    /// <param name="ibare">İşletmenin seçtiği ibare (opsiyonel).</param>
    public static void Dogrula(decimal? tasAgirlik, string? koken, string? ibare)
    {
        var tasliMi = (tasAgirlik ?? 0m) > 0m;

        if (koken is not null && !TasKokeni.Gecerli(koken))
            throw new BadRequestException(
                $"Geçersiz taş kökeni: '{koken}'. Geçerli değerler: {string.Join(", ", TasKokeni.Hepsi)}");

        // Taşsız ürüne köken bildirilmişse tutarsızdır — sessizce düzeltmeyiz.
        if (!tasliMi && TasKokeni.IbareGerekir(koken))
            throw new BadRequestException(
                "Taş ağırlığı girilmemiş bir ürün için taş kökeni bildirilemez. " +
                "Taş ağırlığını girin veya kökeni 'TAS_YOK' yapın.");

        // ⛔ ASIL KURAL: taşlı ürünün kökeni belirtilmek ZORUNDA.
        if (tasliMi && (koken is null || koken == TasKokeni.TasYok))
            throw new BadRequestException(
                "Kıymetli taş içeren ürünün kökeni belirtilmelidir " +
                $"({string.Join(" / ", TasKokeni.Hepsi.Where(x => x != TasKokeni.TasYok))}). " +
                "Dayanak: Kuyum Ticareti Yönetmeliği md.11/(e) — RG 24.06.2026/33290.");

        // Özel ibare verilmişse yönetmeliğin saydığı ibarelerden biri olmalı.
        if (!string.IsNullOrWhiteSpace(ibare) && TasKokeni.IbareGerekir(koken))
        {
            // ⚠ Türkçe harf kapanı: OrdinalIgnoreCase 'İ' ile 'i'yi EŞLEŞTİRMEZ.
            // Kuyumcu etikete "SENTETİK SAFİR" yazdığında reddedilirdi. Karşılaştırma
            // Türkçe kültürle yapılır — yönetmeliğin dili Türkçedir.
            var kabul = new[] { "sentetik", "laboratuvar üretimi", "yapay üretim" };
            if (!kabul.Any(x => Kuyumcu.Core.Domain.TurkceMetin.Icerir(ibare.Trim(), x)))
                throw new BadRequestException(
                    "Taş ibaresi yönetmeliğin saydığı ifadelerden en az birini içermelidir: " +
                    $"{string.Join(", ", kabul)}. Dayanak: md.11/(e).");
        }
    }

    /// <summary>
    /// Ürün satışa sunulmadan önceki son kontrol. Göç öncesi kayıtlarda köken
    /// <c>null</c> olabilir; satış anında yakalanır.
    /// </summary>
    public static void SatisaUygunMu(Data.Urun urun)
    {
        if ((urun.TasAgirlik ?? 0m) <= 0m) return;

        if (urun.TasKokeni is null || urun.TasKokeni == TasKokeni.TasYok)
            throw new BadRequestException(
                $"'{urun.Ad}' ürünü kıymetli taş içeriyor ancak taş kökeni belirtilmemiş. " +
                "Satıştan önce ürün kartında köken bilgisi girilmelidir. " +
                "Dayanak: Kuyum Ticareti Yönetmeliği md.11/(e) — RG 24.06.2026/33290.");
    }
}
