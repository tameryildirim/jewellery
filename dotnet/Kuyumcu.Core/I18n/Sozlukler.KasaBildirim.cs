namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET S3-6 — KASALAR + BİLDİRİMLER (dalga S3B): TR + EN METİNLER
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır; aynı dalgada başka şeritler
//   de ona dokunuyor (orkestratör koşulu: "yeni metin için AYRI dosya aç —
//   Kuyumcu.Core/I18n/Sozlukler.<Alan>.cs"). KurVitrinEmanetSozlugu ile aynı desen.
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL bir record'da
//   durur; Tr ve En aynı kurucudan geçer, bir dilde alan unutulursa derleme
//   kırılır.
//
// ⚠ JARGON SIFIR (PLAN § 5B): "zamanlanmış iş" yerine "otomatik hatırlatma",
//   "yapılandırma" yerine "kurulum ayarı". Ham kod (TRY, BASARILI,
//   VADE_HATIRLATMA) ekrana basılmaz; buradaki adlarla çevrilir.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Kasalar ve Bildirimler ekranlarına bu paketle eklenen metinler (bir dilde).</summary>
public sealed record KasaBildirimMetinleri(
    // ── Kasalar: liste + çekmece ──
    string DigerIslemler,
    string MenuBaslik,
    string BosDurum,
    string ParaTRY,
    string ParaUSD,
    string ParaEUR,
    string ParaGBP,
    string SubeOtomatik,
    string GecmisBaslik,
    // ── Bildirimler ──
    string KapaliIslerBaslik,
    string KapaliIslerAciklama,
    string Ac,
    string AcildiSonuc,
    string KurulumKapali,
    string KurulumKapaliSebep,
    string KurulumKapaliYol,
    string AciklamaKapali,
    string AciklamaTumu,
    string BosAcikCta,
    string BosKapaliCta,
    string Yenilendi,
    string DurumBasladi,
    string DurumBasarili,
    string DurumHatali,
    string DurumBilinmiyor);

public static class KasaBildirimSozlugu
{
    public static readonly KasaBildirimMetinleri Tr = new(
        DigerIslemler: "Diğer işlemler",
        MenuBaslik: "{0} — işlemler",
        BosDurum: "Henüz kasa açılmamış.",
        ParaTRY: "Türk lirası",
        ParaUSD: "Dolar",
        ParaEUR: "Euro",
        ParaGBP: "Sterlin",
        SubeOtomatik: "Şube: {0}",
        GecmisBaslik: "{0} — hareketler",
        KapaliIslerBaslik: "Kapalı hatırlatmalar",
        KapaliIslerAciklama: "Kapattığınız hatırlatmayı buradan yeniden açın.",
        Ac: "Aç",
        AcildiSonuc: "{0} yeniden açıldı. {1}",
        KurulumKapali: "Kurulumda kapalı",
        KurulumKapaliSebep: "Bu hatırlatma kurulum ayarında kapalı.",
        KurulumKapaliYol: "Açılması için kurulumu yapan kişiye söyleyin.",
        AciklamaKapali: "Kapatılan bildirimler; kayıtta kalır.",
        AciklamaTumu: "Açık ve kapalı bütün bildirimler.",
        BosAcikCta: "Kapatılanlara bak",
        BosKapaliCta: "Açık bildirimlere dön",
        Yenilendi: "Liste yenilendi: {0} bildirim.",
        DurumBasladi: "Sürüyor",
        DurumBasarili: "Başarılı",
        DurumHatali: "Hatalı",
        DurumBilinmiyor: "Bilinmiyor");

    public static readonly KasaBildirimMetinleri En = new(
        DigerIslemler: "More actions",
        MenuBaslik: "{0} — actions",
        BosDurum: "No cash register yet.",
        ParaTRY: "Turkish lira",
        ParaUSD: "Dollar",
        ParaEUR: "Euro",
        ParaGBP: "Pound",
        SubeOtomatik: "Branch: {0}",
        GecmisBaslik: "{0} — movements",
        KapaliIslerBaslik: "Turned-off reminders",
        KapaliIslerAciklama: "Turn a reminder you switched off back on here.",
        Ac: "Turn on",
        AcildiSonuc: "{0} turned back on. {1}",
        KurulumKapali: "Off in setup",
        KurulumKapaliSebep: "This reminder is switched off in the installation settings.",
        KurulumKapaliYol: "Ask the person who installed the system to turn it on.",
        AciklamaKapali: "Closed notifications; they stay on record.",
        AciklamaTumu: "All open and closed notifications.",
        BosAcikCta: "See closed ones",
        BosKapaliCta: "Back to open ones",
        Yenilendi: "List refreshed: {0} notifications.",
        DurumBasladi: "Running",
        DurumBasarili: "Succeeded",
        DurumHatali: "Failed",
        DurumBilinmiyor: "Unknown");

    public static KasaBildirimMetinleri Al(string dil) => dil == "en" ? En : Tr;

    /// <summary>UX-S31 — para birimi KODU ekrana basılmaz; okunur ad (tek kaynak).</summary>
    public static string ParaAdi(KasaBildirimMetinleri m, string? kod) => kod switch
    {
        "TRY" => m.ParaTRY,
        "USD" => m.ParaUSD,
        "EUR" => m.ParaEUR,
        "GBP" => m.ParaGBP,
        null or "" => "—",
        // Bilinmeyen kod ad UYDURULMADAN kendi adıyla görünür (Anayasa md.1).
        _ => kod,
    };

    /// <summary>UX-S31 — iş koşumu durumu (BASLADI/BASARILI/HATALI) okunur adla.</summary>
    public static string DurumAdi(KasaBildirimMetinleri m, string? kod) => kod switch
    {
        "BASLADI" => m.DurumBasladi,
        "BASARILI" => m.DurumBasarili,
        "HATALI" => m.DurumHatali,
        null or "" => "—",
        _ => m.DurumBilinmiyor,
    };
}
