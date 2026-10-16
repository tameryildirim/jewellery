namespace Kuyumcu.Core.Kilavuz;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-33 (B-11) — KILAVUZ ÇIPASI: rota şablonu → kilavuz.html içindeki
// ekran çıpası.
//
// NEDEN BURADA (Kuyumcu.Core): aynı işlevi İKİ taraf kullanır —
//   (1) kılavuz üreticisi (docs/KULLANIM_KILAVUZU.md → wwwroot/kilavuz.html),
//       her `<!-- ekran: /rota -->` işaretinin yerine bu çıpayı basar;
//   (2) Pages/Kilavuz.razor, RotaBasliklari.Harita'daki HER rotaya bu çıpayla
//       bağlantı verir.
// İki tarafın ayrı ayrı "slug" üretmesi, bir gün birinin değişip ötekinin
// ölü çıpaya bakması demekti (UX-S31: tek kavram, tek kaynak).
//
// Kural: baştaki/sondaki '/' atılır, '/' → '-', parametre kesimi ({Id},
// {Tur}, {IthalatId}) → "kayit", kök "/" → "panel"; hepsinin önüne "e-".
//   "/"                          → "e-panel"
//   "/urunler/agac"              → "e-urunler-agac"
//   "/siparisler/{Id}"           → "e-siparisler-kayit"
//   "/gider-pusulalari/{Id}/belge" → "e-gider-pusulalari-kayit-belge"
// "e-" öneki, kılavuzun elle verilmiş bölüm çıpalarıyla (#panel, #cariler …)
// çakışmayı imkânsız kılar: o çıpalar üst bardaki Yardım bağlantısının ve
// eski yer imlerinin hedefidir, DEĞİŞMEZ.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Rota şablonundan kılavuz ekran çıpası üretir (tek kaynak).</summary>
public static class KilavuzCapasi
{
    /// <summary>Kılavuzun statik adresi (CSP 'self'; oturumsuz okunur).</summary>
    public const string KilavuzAdresi = "/kilavuz.html";

    /// <summary>Rota şablonu → "e-…" çıpası.</summary>
    public static string Ekran(string rotaSablonu)
    {
        var temiz = (rotaSablonu ?? "").Trim().Trim('/');
        if (temiz.Length == 0) return "e-panel";

        var kesimler = temiz.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.StartsWith('{') && k.EndsWith('}') ? "kayit" : k.ToLowerInvariant());
        return "e-" + string.Join('-', kesimler);
    }

    /// <summary>Rota şablonu → kılavuzdaki tam adres ("/kilavuz.html#e-…").</summary>
    public static string Adres(string rotaSablonu) => $"{KilavuzAdresi}#{Ekran(rotaSablonu)}";
}
