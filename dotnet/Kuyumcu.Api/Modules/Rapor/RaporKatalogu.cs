namespace Kuyumcu.Api.Modules.Rapor;

// ═════════════════════════════════════════════════════════════════════════════
// RAPOR KATALOĞU — paket 34 Faz A.
//
// "Kaç rapor var?" sorusunun TEK cevabı burasıdır. PARITE belgesi 19 rapor
// der ama LİSTEYİ HİÇBİR YERDE VERMEZ (paket 34 § açık sorular). Bu yüzden
// katalog bir İDDİA değil ÖLÇÜMDÜR: her satırın `Uc` alanı gerçekten kayıtlı
// bir [HttpGet] ucuna karşılık gelir ve test bunu yansımayla doğrular
// (Bertan/Aşkın koşulu: "== 19" testi tautolojidir ve sonraki paketleri kırar).
//
// Ekran menüsü (Rapor Merkezi) bu listeden kurulur; yeni rapor eklemenin tek
// yolu buraya satır eklemektir, aksi hâlde rapor "sunucuda var, ekranda yok"
// olur (UX-S25).
// ═════════════════════════════════════════════════════════════════════════════

/// <param name="Kod">Kalıcı rapor kodu (R01…). Ekran ve sözlük bununla eşleşir.</param>
/// <param name="Rota">Raporun görüldüğü EKRAN yolu.</param>
/// <param name="Uc">API yolu — <c>api/v1/</c> öneki OLMADAN.</param>
/// <param name="Roller">Sunucudaki [RolGerekli] ile BİREBİR aynı küme.</param>
/// <param name="Faz">"A" = sevk edildi · "B" = sonraki faz (katalogda YER ALMAZ).</param>
/// <param name="Grup">Rapor Merkezi kart ızgarasındaki bölüm.</param>
public sealed record RaporKarti(
    string Kod, string Rota, string Uc, string[] Roller, string Faz, string Grup);

public static class RaporKatalogu
{
    public const string GrupGunluk = "GUNLUK";
    public const string GrupSatis  = "SATIS";
    public const string GrupStok   = "STOK";
    public const string GrupMali   = "MALI";

    private static readonly string[] MuhasebeUstu = ["SAHIP", "MUDUR", "MUHASEBE"];

    /// <summary>
    /// Faz A'da SEVK EDİLMİŞ raporlar. Faz B raporları (kategori satış, stok
    /// devir, hurda özet, KDV özet, iade özet) BİLİNÇLİ OLARAK YOKTUR: uç
    /// yazılmadan katalog satırı açmak, ekranda tıklanınca 404 veren kart
    /// demektir (DoD: her düğme ya istek atar ya kilitlidir).
    /// </summary>
    public static readonly RaporKarti[] Hepsi =
    [
        new("R01", "/raporlar",            "raporlar/gunluk",                 MuhasebeUstu, "A", GrupGunluk),
        new("R02", "/raporlar",            "raporlar/aylik",                  MuhasebeUstu, "A", GrupGunluk),
        new("R05", "/raporlar",            "raporlar/pos-fatura-mutabakati",  MuhasebeUstu, "A", GrupGunluk),
        new("R11", "/raporlar/kasa",       "raporlar/kasa-defteri",           MuhasebeUstu, "A", GrupGunluk),

        new("R10", "/raporlar/islem-defteri",  "raporlar/islem-defteri",      MuhasebeUstu, "A", GrupSatis),
        new("R12", "/raporlar/satis-analizi",  "raporlar/odeme-araci",        MuhasebeUstu, "A", GrupSatis),
        new("R13", "/raporlar/satis-analizi",  "raporlar/personel",           MuhasebeUstu, "A", GrupSatis),
        new("R14", "/raporlar/satis-analizi",  "raporlar/sube-karsilastirma", MuhasebeUstu, "A", GrupSatis),

        new("R06", "/raporlar",            "raporlar/stok-degeri",            MuhasebeUstu, "A", GrupStok),
        new("R20", "/raporlar",            "raporlar/reyon-stok",             MuhasebeUstu, "A", GrupStok),
        new("R09", "/raporlar/kar-zarar",  "raporlar/stok-degerlemesi",       MuhasebeUstu, "A", GrupStok),

        // P-25 / Y-16 — 16. RAPOR. Rapor Merkezi sayısı BELGEDEN değil BU
        // DİZİDEN okunur (Y-31); yeni rapor eklemenin tek yolu bu satırdır.
        new("R15", "/raporlar",            "raporlar/stok-yaslandirma",       MuhasebeUstu, "A", GrupStok),

        new("R03", "/raporlar",              "raporlar/cari-yaslandirma",     MuhasebeUstu, "A", GrupMali),
        new("R04", "/raporlar/vade-takibi",  "raporlar/vade-takibi",          MuhasebeUstu, "A", GrupMali),
        new("R07", "/raporlar/kar-zarar",    "raporlar/kar-zarar",            MuhasebeUstu, "A", GrupMali),
        new("R08", "/raporlar/kar-zarar",    "raporlar/kar-zarar/urun",       MuhasebeUstu, "A", GrupMali),
    ];

    /// <summary>
    /// Faz A'da sevk edilen rapor sayısı. Test bu sayıyı AŞAĞI çekmeyi engeller
    /// (M116: katalogdan rapor silinirse kırmızı); yukarı büyümek serbesttir —
    /// paket 39/42 kendi raporlarını ekleyecek.
    /// </summary>
    public const int FazAAsgari = 16;
}
