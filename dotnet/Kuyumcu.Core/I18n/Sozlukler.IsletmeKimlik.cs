using Kuyumcu.Core.Domain;

namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET Q10-3 — İŞLETME KİMLİĞİ: dört alan her modda düzenlenebilir
// (QA turu 10 KRİTİK: basit modda çekmecede yalnız "İşletme adı" vardı).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: yeni
//   metin Sozlukler.<Alan>.cs). Burada YALNIZ bu paketin yeni metinleri durur.
// ⚠ Mevzuat hükmü YOK: yalnız BİÇİM (10/11 hane, rakam) söylenir; sağlama
//   (kontrol hanesi) birincil kaynakla doğrulanamadığı için uygulanmaz.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Q10-3 metinleri (bir dilde). Konumsal kayıt: eksik dil = derleme hatası.</summary>
public sealed record IsletmeKimlikMetinleri(
    string VergiNoRakamDisi,
    string VergiNoHane,
    string VergiNoIpucu,
    string BelgeNotu,
    string AdUzunluk);

/// <summary>Q10-3 sözlüğü.</summary>
public static class IsletmeKimlikSozlugu
{
    public static readonly IsletmeKimlikMetinleri Tr = new(
        VergiNoRakamDisi: "Yalnız rakam girin: harf, boşluk ya da işaret olamaz.",
        VergiNoHane: "Vergi no 10 hane, TC kimlik no 11 hane olmalı.",
        VergiNoIpucu: "Şirketse 10 haneli vergi no, şahıs işletmesiyse 11 haneli TC kimlik no.",
        BelgeNotu: "Bu bilgiler gider pusulasında ve yazdırılan satış fişi, tahsilat makbuzu, emanet teslim belgesi ve müşteri ekstresinde işletme bilgisi olarak basılır.",
        AdUzunluk: "İşletme adı en az 2, en çok 200 karakter olmalı.");

    public static readonly IsletmeKimlikMetinleri En = new(
        VergiNoRakamDisi: "Enter digits only: no letters, spaces or symbols.",
        VergiNoHane: "Tax number must be 10 digits, national ID number 11 digits.",
        VergiNoIpucu: "10-digit tax number for a company, 11-digit national ID number for a sole proprietor.",
        BelgeNotu: "These details are printed as the business details on expense receipts and on printed sales slips, payment receipts, consignment delivery notes and customer statements.",
        AdUzunluk: "Business name must be 2 to 200 characters.");

    public static IsletmeKimlikMetinleri Al(string dil) => dil == "en" ? En : Tr;

    /// <summary>Biçim hatasının ekran mesajı — null = geçerli. Kural Core VergiKimlik'tedir.</summary>
    public static string? VergiNoMesaji(string dil, string? deger) => VergiKimlik.Denetle(deger) switch
    {
        VergiKimlikHatasi.RakamDisi => Al(dil).VergiNoRakamDisi,
        VergiKimlikHatasi.HaneSayisi => Al(dil).VergiNoHane,
        VergiKimlikHatasi.TcknGecersiz => KimlikNoSozlugu.Al(dil).TcknGecersiz,   // PAKET TCKN-DOGRULAMA
        _ => null,
    };
}
