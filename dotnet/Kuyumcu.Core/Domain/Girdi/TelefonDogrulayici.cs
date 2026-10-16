namespace Kuyumcu.Core.Domain.Girdi;

// ═══════════════════════════════════════════════════════════════════════════
// K2 — TELEFON. KAYNAK (BİRİNCİL): ITU-T E.164 ulusal numaralandırma bildirimi,
// Türkiye (+90), BTK 2.III.2018 — "en az ve en çok uzunluk (ülke kodu hariç)
// 10 hane" + NDC tablosu:
// https://itu.int/dms_pub/itu-t/oth/02/02/T02020000D60002MSWS.docx
// (envanter K2'de okunup alıntılandı).
//   • Ulusal anlamlı numara TAM 10 hane (baştaki 0 ya da +90 hariç).
//   • Coğrafi 2xx/3xx/4xx · mobil 5xx yalnız aşağıdaki listede · 800 · 850.
//   • Yabancı numara: "+" + ülke kodu, E.164 en çok 15 hane.
// ⚠ Mobil aralık listesi BTK kararıyla değişebilir: liste TEK yerde (bu sınıf)
//   durur; ekran ve sunucu aynı listeyi okur (yapılandırmaya taşınırsa istemci ile
//   sunucu ayrışır — envanterdeki "yapılandırmadan okunmalı" önerisinden sapma,
//   rapor § Sapmalar).
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Telefon numarası kuralının tek kaynağı.</summary>
public static class TelefonDogrulayici
{
    /// <summary>Ulusal anlamlı numara hane sayısı (ITU bildirimi).</summary>
    public const int UlusalHane = 10;

    /// <summary>Ulusal tek numara öneki (444 XXXX, alan kodsuz).</summary>
    public const string TekNumaraOnek = "444";
    /// <summary>Ulusal tek numara toplam hane (444 + 4).</summary>
    public const int TekNumaraHane = 7;

    /// <summary>E.164 en çok hane (ülke kodu dâhil).</summary>
    public const int E164EnCok = 15;

    /// <summary>
    /// Mobil NDC'ler (BTK → ITU bildirimi, envanter K2): 501, 505–507, 530–549,
    /// 551–555, 559; MVNO 510/516/524; 592.
    /// </summary>
    public static readonly IReadOnlySet<int> MobilOnekler = MobilListe();

    /// <summary>Coğrafi dışı 8xx hizmet numaraları (800 ücretsiz, 850 tek numara).</summary>
    public static readonly IReadOnlySet<int> HizmetOnekleri = new HashSet<int> { 800, 850 };

    private static HashSet<int> MobilListe()
    {
        var s = new HashSet<int> { 501, 505, 506, 507, 510, 516, 524, 551, 552, 553, 554, 555, 559, 592 };
        for (var i = 530; i <= 549; i++) s.Add(i);
        return s;
    }

    /// <summary>Yazımda izin verilen karakter mi? (rakam, boşluk, parantez, tire; '+' yalnız başta.)</summary>
    public static bool IzinliKarakter(char c, bool basta) =>
        c is >= '0' and <= '9' || c is ' ' or '(' or ')' or '-' || (c == '+' && basta);

    /// <summary>Yalnız ASCII rakamlar.</summary>
    public static string Rakamlar(string? s) => KimlikNoDogrulayici.Rakamlar(s);

    /// <summary>
    /// Telefon denetimi. Boş → null. Yerel yazım ("0532 411 22 33", "5324112233",
    /// "+90 532 …", "0090 …") ve yabancı ("+44 …") kabul edilir.
    /// </summary>
    public static GirdiHatasi? Denetle(string? ham)
    {
        if (string.IsNullOrWhiteSpace(ham)) return null;
        var t = ham.Trim();
        for (var i = 0; i < t.Length; i++)
            if (!IzinliKarakter(t[i], i == 0)) return new(GirdiKodu.TelefonKarakter);

        var rakam = Rakamlar(t);
        string ulusal;
        // "+" ya da "00" uluslararası önektir; ardından ülke kodu gelir (ülke kodu 0 ile başlamaz).
        var uluslararasiMi = t[0] == '+' || (rakam.Length > 2 && rakam.StartsWith("00", StringComparison.Ordinal) && rakam[2] != '0');
        if (uluslararasiMi)
        {
            var uluslararasi = t[0] == '+' ? rakam : rakam[2..];
            if (uluslararasi.Length == 0 || uluslararasi[0] == '0') return new(GirdiKodu.TelefonYabanci, E164EnCok);
            if (!uluslararasi.StartsWith("90", StringComparison.Ordinal))
            {
                // Yabancı: E.164 — ülke kodu dâhil 8..15 hane.
                return uluslararasi.Length is >= 8 and <= E164EnCok ? null
                    : new GirdiHatasi(GirdiKodu.TelefonYabanci, E164EnCok);
            }
            ulusal = uluslararasi[2..];
        }
        else
        {
            ulusal = rakam.StartsWith('0') ? rakam[1..] : rakam;
        }

        // 444 + 4 hane: alan kodsuz ulusal tek numara (envanter K2 "… 850, 800, 444"; ITU bildirimindeki
        // 10 hane kuralının istisnası). Doğrulayıcı tur 1 bulgusu: "444 1 234" TelefonHane ile reddediliyordu.
        if (ulusal.Length == TekNumaraHane && ulusal.StartsWith(TekNumaraOnek, StringComparison.Ordinal)) return null;
        if (ulusal.Length != UlusalHane) return new(GirdiKodu.TelefonHane, ulusal.Length, UlusalHane);
        var ndc = (ulusal[0] - '0') * 100 + (ulusal[1] - '0') * 10 + (ulusal[2] - '0');
        var gecerli = ulusal[0] switch
        {
            '2' or '3' or '4' => true,
            '5' => MobilOnekler.Contains(ndc),
            '8' => HizmetOnekleri.Contains(ndc),
            _ => false,
        };
        return gecerli ? null : new GirdiHatasi(GirdiKodu.TelefonAralik);
    }

    public static bool Gecerli(string? ham) => !string.IsNullOrWhiteSpace(ham) && Denetle(ham) is null;
}
