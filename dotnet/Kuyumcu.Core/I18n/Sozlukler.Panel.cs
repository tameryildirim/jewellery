namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S3-1 — PANEL "SABAHIN BEŞ SORUSU" (basit mod yüzü): TR + EN METİNLER
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır ve aynı dalgada başka şeritler
//   de ona dokunuyor (orkestratör koşulu: "yeni metin için AYRI dosya aç —
//   Kuyumcu.Core/I18n/Sozlukler.<Alan>.cs"). PozisyonSozlugu ile aynı desen.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL bir record'da
//   durur; Tr ve En aynı kurucudan geçer, bir dilde alan unutulursa derleme
//   kırılır.
//
// ⚠ JARGON SIFIR (PLAN § 5B): hedef kitle ilkokul/lise mezunu tezgâhtar.
//   "Cari", "tahsilat", "bakiye" gibi terimler başlıkta YOK; taslaktaki
//   (tuval/project/Panel.dc.html) gündelik sorular birebir kullanılır.
//   Etiket ≤ 3 kelime, mesaj ≤ 2 cümle.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Panelin basit mod yüzündeki tüm metinler (bir dilde).</summary>
public sealed record PanelSadeMetinleri(
    string YeniSatis,
    string BugunNeSattim,
    string SatisAdet,
    string SatisYok,
    string SatisBaglantiIpucu,
    string KasadaNeVar,
    string KasaBaglantiIpucu,
    string DahaKasa,
    string BugunItibariyla,
    string KimBorclu,
    string Tumu,
    string TahsilEt,
    string Ode,
    string VadeGecti,
    string VadeBugun,
    string GunKaldi,
    string AcikHesap,
    string BizBorcluyuz,
    string BorcluYok,
    string BorcluSinir,
    string AcikHesapSayisi,
    string DovizDeVar,
    string BekleyenIs,
    string BekleyenYok,
    string BekleyenSinir,
    string YetkiYok,
    string HasBirim,
    string OnemKritik,
    string OnemUyari,
    string OnemBilgi,
    string TlKasalar,
    string DolarKasalar,
    string EuroKasalar,
    string SterlinKasalar,
    string DigerKasalar);

public static class PanelSadeSozlugu
{
    public static readonly PanelSadeMetinleri Tr = new(
        YeniSatis: "Yeni satış",
        BugunNeSattim: "Bugün ne sattım?",
        SatisAdet: "{0} satış",
        SatisYok: "Bugün henüz satış yok.",
        SatisBaglantiIpucu: "Bugünkü işlemleri aç",
        KasadaNeVar: "Kasada ne var?",
        KasaBaglantiIpucu: "Kasaları aç",
        DahaKasa: "+{0} kasa daha",
        BugunItibariyla: "Şu anki para",
        KimBorclu: "Kim borçlu?",
        Tumu: "Tümü",
        TahsilEt: "Tahsil et",
        Ode: "Öde",
        VadeGecti: "Vade geçti ({0} gün)",
        VadeBugun: "Vade bugün",
        GunKaldi: "{0} gün kaldı",
        AcikHesap: "Açık hesap",
        BizBorcluyuz: BorcDiliSozlugu.BizBorcluyuzEtiket("tr"), // P-DIL-BORC-KASA
        BorcluYok: "Açık hesap yok.",
        BorcluSinir: "İlk {0} hesaba bakıldı. Hepsi Müşteriler ekranında.",
        AcikHesapSayisi: "{0} açık hesap",
        DovizDeVar: "döviz de var",
        BekleyenIs: "Bekleyen iş",
        BekleyenYok: "Bekleyen iş yok.",
        BekleyenSinir: "İlk {0} iş gösterildi. Hepsi Bildirimler ekranında.",
        YetkiYok: "Bu bilgiyi yalnız yönetici görür.",
        HasBirim: "g has",
        OnemKritik: "Acil",
        OnemUyari: "Dikkat",
        OnemBilgi: "Bilgi",
        TlKasalar: "TL kasalar",
        DolarKasalar: "Dolar kasalar",
        EuroKasalar: "Euro kasalar",
        SterlinKasalar: "Sterlin kasalar",
        DigerKasalar: "Diğer kasalar");

    public static readonly PanelSadeMetinleri En = new(
        YeniSatis: "New sale",
        BugunNeSattim: "What did I sell today?",
        SatisAdet: "{0} sales",
        SatisYok: "No sales yet today.",
        SatisBaglantiIpucu: "Open today's transactions",
        KasadaNeVar: "What is in the till?",
        KasaBaglantiIpucu: "Open tills",
        DahaKasa: "+{0} more tills",
        BugunItibariyla: "Money right now",
        KimBorclu: "Who owes?",
        Tumu: "All",
        TahsilEt: "Collect",
        Ode: "Pay",
        VadeGecti: "Overdue ({0} days)",
        VadeBugun: "Due today",
        GunKaldi: "{0} days left",
        AcikHesap: "Open account",
        BizBorcluyuz: BorcDiliSozlugu.BizBorcluyuzEtiket("en"),
        BorcluYok: "No open accounts.",
        BorcluSinir: "First {0} accounts checked. All are on the Customers screen.",
        AcikHesapSayisi: "{0} open accounts",
        DovizDeVar: "foreign currency too",
        BekleyenIs: "Pending work",
        BekleyenYok: "No pending work.",
        BekleyenSinir: "First {0} items shown. All are on the Notifications screen.",
        YetkiYok: "Only managers can see this.",
        HasBirim: "g fine gold",
        OnemKritik: "Urgent",
        OnemUyari: "Attention",
        OnemBilgi: "Info",
        TlKasalar: "TL tills",
        DolarKasalar: "Dollar tills",
        EuroKasalar: "Euro tills",
        SterlinKasalar: "Pound tills",
        DigerKasalar: "Other tills");

    public static PanelSadeMetinleri Al(string dil) => dil == "en" ? En : Tr;

    /// <summary>
    /// Kasa grubunun adı — ham para kodu (TRY/USD) EKRANA BASILMAZ (UX-S31).
    /// Sözlükte olmayan kod genel "Diğer kasalar" metnine düşer.
    /// </summary>
    public static string KasaGrubu(string? paraBirimi, string dil)
    {
        var m = Al(dil);
        return paraBirimi switch
        {
            null or "" or "TRY" => m.TlKasalar,
            "USD" => m.DolarKasalar,
            "EUR" => m.EuroKasalar,
            "GBP" => m.SterlinKasalar,
            _ => m.DigerKasalar,
        };
    }

    /// <summary>Bildirim önem kodunun sade adı (ham KRITIK/UYARI/BILGI ekrana basılmaz).</summary>
    public static string Onem(string? kod, string dil)
    {
        var m = Al(dil);
        return kod switch
        {
            "KRITIK" => m.OnemKritik,
            "UYARI" => m.OnemUyari,
            _ => m.OnemBilgi,
        };
    }
}
