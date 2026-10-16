namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET SON-KALANLAR — PATCH-5 hasım/hakem kalanları (istemci metinleri)
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: yeni metin
//   Sozlukler.<Paket>.cs). EKSİK ANAHTAR = DERLEME HATASI: konumsal record; Tr ve En
//   aynı kurucudan geçer.
// ⚠ JARGON SIFIR, büyük harfle bağırma yok, rol kodu yok (hakem: "SAHİP yetkisiyle" yazmaz).
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>SON-KALANLAR ekran metinleri (bir dilde).</summary>
public sealed record SonKalanlarMetinleri(
    // ── Gün Kapat: farkın yönü ({0} = biçimli tutar) ──
    string FarkFazla,
    string FarkEksik,
    // ── Kasa kartı: bugünkü gün kapandı ──
    string GunKapandiRozet,
    string GunKapandiSebep,
    // ── Tur 2 (tasarım hakemi KRİTİK): banka hesabı para birimi kilidi (kaydetmeden ÖNCE yazar) ──
    string BankaDovizKilitBakiye,
    string BankaDovizKilitHareket,
    string BankaDovizKilitBilinmiyor,
    // ── Tur 2 (doğrulayıcı DÜŞÜK): tahsilat altlığındaki kilit sebebi — yer tutucu "Kasa seçin..."
    //    ile aynı cümleyi tekrar etmez ──
    string TahsilatKasaKilit);

/// <summary>SON-KALANLAR sözlüğü (TR + EN).</summary>
public static class SonKalanlarSozlugu
{
    public static readonly SonKalanlarMetinleri Tr = new(
        FarkFazla: "{0} fazla",
        FarkEksik: "{0} eksik",
        GunKapandiRozet: "Bugün kapandı",
        GunKapandiSebep: "Bu kasanın bugünkü günü kapandı. Yeniden açmak için işletme sahibine başvurun.",
        BankaDovizKilitBakiye: "Bakiyesi olan hesabın para birimi değişmez — yeni hesap açın.",
        BankaDovizKilitHareket: "Hareketi olan hesabın para birimi değişmez — yeni hesap açın.",
        BankaDovizKilitBilinmiyor: "Para birimi şu an değiştirilemiyor: hesabın geçmişi okunamadı. Sayfayı yenileyip tekrar deneyin.",
        TahsilatKasaKilit: "Kaydetmek için yukarıdan kasayı seçin");

    public static readonly SonKalanlarMetinleri En = new(
        FarkFazla: "{0} over",
        FarkEksik: "{0} short",
        GunKapandiRozet: "Closed today",
        GunKapandiSebep: "Today's day for this cash box is closed. Ask the business owner to reopen it.",
        BankaDovizKilitBakiye: "An account that holds a balance keeps its currency — open a new account.",
        BankaDovizKilitHareket: "An account with transactions keeps its currency — open a new account.",
        BankaDovizKilitBilinmiyor: "The currency cannot be changed right now: the account history could not be read. Refresh the page and try again.",
        TahsilatKasaKilit: "Choose the cash box above to save");

    public static SonKalanlarMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
