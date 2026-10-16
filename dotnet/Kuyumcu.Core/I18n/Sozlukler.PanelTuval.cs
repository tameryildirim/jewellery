namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-P1 — PANEL TUVALE (tuval/project/Panel.dc.html birebir): TR + EN
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs HUB'ına dokunulmaz (orkestratör koşulu);
//   S3-1'in PanelSadeMetinleri record'u da KONUMSAL — araya alan sokmak
//   Raporlar.razor'un kullandığı yardımcıları sarsmasın diye yeni metinler
//   bu dosyada, kendi record'unda durur. Eksik anahtar = derleme hatası.
//
// ⚠ JARGON SIFIR: etiketler tuvaldeki gündelik sözcüklerdir ("Nakit", "Banka",
//   "Kâr", "Vade geçti"). Ham tür kodu (VADE_HATIRLATMA, MASAK_SIB_SURESI)
//   ekrana basılmaz — Kaynak() ile okunur ada çevrilir (UX-S31).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Panel basit yüzünün tuval düzenine özgü metinleri (bir dilde).</summary>
public sealed record PanelTuvalMetinleri(
    string Nakit,
    string Banka,
    string KasaKirilimIpucu,
    string Kar,
    string KarIpucu,
    string KarEksikIpucu,
    string KarMaliyetEksik,
    string VadeGectiKisa,
    string VadeGectiIpucu,
    string Ayrinti,
    string AyrintiKapat,
    string KaynakVade,
    string KaynakUyum,
    string KaynakBelge,
    string KaynakSistem,
    string KaynakBildirim);

public static class PanelTuvalSozlugu
{
    public static readonly PanelTuvalMetinleri Tr = new(
        Nakit: "Nakit",
        Banka: "Banka",
        KasaKirilimIpucu: "Kasalar tek tek Kasalar ekranında",
        Kar: "Kâr",
        KarIpucu: "Bugünkü kâr — ayrıntısı Kâr-zarar ekranında",
        KarEksikIpucu: "{0} ürünün maliyeti bilinmiyor; kâr eksik olabilir.",
        KarMaliyetEksik: "Kâr: maliyet eksik",
        VadeGectiKisa: "Vade geçti",
        VadeGectiIpucu: "{0} gündür gecikmede",
        Ayrinti: "Ayrıntı",
        AyrintiKapat: "Kapat",
        KaynakVade: "Vade",
        KaynakUyum: "Uyum",
        KaynakBelge: "Belge",
        KaynakSistem: "Sistem",
        KaynakBildirim: "Bildirim");

    public static readonly PanelTuvalMetinleri En = new(
        Nakit: "Cash",
        Banka: "Bank",
        KasaKirilimIpucu: "Each till is on the Tills screen",
        Kar: "Profit",
        KarIpucu: "Today's profit — details on the Profit & loss screen",
        KarEksikIpucu: "Cost of {0} items is unknown; profit may be incomplete.",
        KarMaliyetEksik: "Profit: cost missing",
        VadeGectiKisa: "Overdue",
        VadeGectiIpucu: "Overdue for {0} days",
        Ayrinti: "Details",
        AyrintiKapat: "Close",
        KaynakVade: "Due dates",
        KaynakUyum: "Compliance",
        KaynakBelge: "Documents",
        KaynakSistem: "System",
        KaynakBildirim: "Notice");

    public static PanelTuvalMetinleri Al(string dil) => dil == "en" ? En : Tr;

    /// <summary>
    /// "Bekleyen iş" satırının sağındaki kaynak etiketi (tuval: Sipariş · Emanet ·
    /// Kasa · Sistem). Ham tür kodu ekrana basılmaz; bilinmeyen kod UYDURULMADAN
    /// genel "Bildirim" etiketine düşer.
    /// </summary>
    public static string Kaynak(string? tur, string dil)
    {
        var m = Al(dil);
        return tur switch
        {
            "VADE_HATIRLATMA" => m.KaynakVade,
            "MASAK_SIB_SURESI" => m.KaynakUyum,
            "SAKLAMA_ONIZLEME" => m.KaynakSistem,
            _ => m.KaynakBildirim,
        };
    }
}
