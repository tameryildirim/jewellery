namespace Kuyumcu.Core.Domain.Girdi;

// ═══════════════════════════════════════════════════════════════════════════
// K1 — IBAN. KAYNAK (BİRİNCİL): TCMB "Uluslararası Banka Hesap Numarası Hakkında
// Tebliğ" (Sayı 2008/6), RG 10.10.2008/27020 —
// https://www.resmigazete.gov.tr/eskiler/2008/10/20081010-8.htm
//   md.3: ISO 13616 · md.4: TR IBAN 26 karakter = "TR" + 2 kontrol hanesi +
//   5 hane banka kodu + 1 hane rezerv alan + 16 karakter hesap numarası;
//   kontrol: ISO 7064 MOD 97-10.
// ⚠ Hesap no bölümü (son 16) tebliğe göre HARF İÇEREBİLİR — "TR + 24 rakam"
//   dar okumadır (envanter K1 notu). Banka kodu ve rezerv hane rakamdır.
// Yabancı IBAN: bugünkü sunucu davranışı (2 harf + 2 rakam + 10–30 harf/rakam)
// KORUNUR, üstüne aynı MOD 97-10 denetimi uygulanır.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>IBAN kuralının tek kaynağı (ekran + sunucu + test üreticisi).</summary>
public static class IbanDogrulayici
{
    /// <summary>TR IBAN toplam uzunluğu (tebliğ md.4).</summary>
    public const int TrUzunluk = 26;

    private static readonly System.Text.RegularExpressions.Regex TrDesen =
        new(@"^TR[0-9]{2}[0-9]{5}[0-9][0-9A-Z]{16}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static readonly System.Text.RegularExpressions.Regex YabanciDesen =
        new(@"^[A-Z]{2}[0-9]{2}[A-Z0-9]{10,30}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>Boşlukları siler, büyük harfe çevirir (kayıt biçimi).</summary>
    public static string Normallestir(string? ham)
    {
        if (string.IsNullOrEmpty(ham)) return "";
        var b = new System.Text.StringBuilder(ham.Length);
        foreach (var c in ham)
            if (!char.IsWhiteSpace(c)) b.Append(char.ToUpperInvariant(c));
        return b.ToString();
    }

    /// <summary>
    /// IBAN denetimi. Boş → null (zorunluluk çağıranındır). Boşluklu/küçük harfli yazım
    /// kabul edilir (önce normalleştirilir).
    /// </summary>
    public static GirdiHatasi? Denetle(string? ham)
    {
        if (string.IsNullOrWhiteSpace(ham)) return null;
        var n = Normallestir(ham);
        if (n.Length < 2 || n[0] is < 'A' or > 'Z' || n[1] is < 'A' or > 'Z')
            return new(GirdiKodu.IbanBicim);
        if (n.StartsWith("TR", StringComparison.Ordinal))
        {
            if (n.Length != TrUzunluk) return new(GirdiKodu.IbanUzunluk, n.Length, TrUzunluk);
            if (!TrDesen.IsMatch(n)) return new(GirdiKodu.IbanBicim);
        }
        else if (!YabanciDesen.IsMatch(n))
        {
            // Tur 3: yabancı numarada "IBAN TR ile başlar" yanıltıcıydı → ayrı kod (uzunluk söylenir).
            return new(GirdiKodu.IbanYabanciBicim, n.Length);
        }
        return Mod97(n) == 1 ? null : new GirdiHatasi(GirdiKodu.IbanKontrol);
    }

    /// <summary>Geçerli mi (boş = geçersiz)?</summary>
    public static bool Gecerli(string? ham) => !string.IsNullOrWhiteSpace(ham) && Denetle(ham) is null;

    /// <summary>
    /// ISO 7064 MOD 97-10: ilk 4 karakter sona taşınır, harfler 10–35'e çevrilir,
    /// sayının 97'ye bölümünden kalan. Geçerli IBAN'da 1'dir.
    /// </summary>
    public static int Mod97(string normal)
    {
        if (normal.Length < 4) return -1;
        var dizi = normal[4..] + normal[..4];
        var kalan = 0;
        foreach (var c in dizi)
        {
            if (c is >= '0' and <= '9') kalan = (kalan * 10 + (c - '0')) % 97;
            else if (c is >= 'A' and <= 'Z') kalan = (kalan * 100 + (c - 'A' + 10)) % 97;
            else return -1;
        }
        return kalan;
    }

    /// <summary>
    /// 22 karakterlik TR BBAN'a (5 banka + 1 rezerv + 16 hesap) doğru kontrol hanelerini
    /// ekleyip 26 karakterlik IBAN döndürür (test ve tohum verisi için).
    /// </summary>
    public static string TrTamamla(string bban22)
    {
        if (bban22 is null || bban22.Length != 22) throw new ArgumentException("BBAN 22 karakter olmalı", nameof(bban22));
        var kontrol = 98 - Mod97("TR00" + bban22.ToUpperInvariant());
        return "TR" + kontrol.ToString("00", System.Globalization.CultureInfo.InvariantCulture) + bban22.ToUpperInvariant();
    }
}
