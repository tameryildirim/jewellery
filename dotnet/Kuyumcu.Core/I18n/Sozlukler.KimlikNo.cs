using Kuyumcu.Core.Domain;

namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET TCKN-DOGRULAMA — kimlik numarası hata metni (TR + EN). Kural Core
// KimlikNoDogrulayici'dadır; burada yalnız ekranda basılan metin durur.
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (yeni metin Sozlukler.<Alan>.cs).
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>TCKN-DOGRULAMA metinleri (bir dilde). Konumsal kayıt: eksik dil = derleme hatası.</summary>
public sealed record KimlikNoMetinleri(
    string TcknGecersiz);

/// <summary>TCKN-DOGRULAMA sözlüğü.</summary>
public static class KimlikNoSozlugu
{
    public static readonly KimlikNoMetinleri Tr = new(
        TcknGecersiz: KimlikNoDogrulayici.TcknGecersizMesaj + ".");

    public static readonly KimlikNoMetinleri En = new(
        TcknGecersiz: "The national ID number is not valid.");

    public static KimlikNoMetinleri Al(string dil) => dil == "en" ? En : Tr;

    /// <summary>Elinde yalnız <see cref="SozlukIslem"/> olan model sınıfları için dil seçimi.</summary>
    public static KimlikNoMetinleri Icin(SozlukIslem t) => t.DilKodu == "en" ? En : Tr;
}
