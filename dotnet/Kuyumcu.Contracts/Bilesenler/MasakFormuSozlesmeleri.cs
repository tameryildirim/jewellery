// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Bilesenler/MasakFormu.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.

namespace Kuyumcu.Contracts.Bilesenler.MasakFormu;
/// <summary>/masak/esikler yanıtı.</summary>
public sealed record Esikler(
    decimal KimlikEsigi, decimal AmacBeyaniEsigi, decimal BeyanFormuEsigi, decimal EkBelgeEsigi,
    int AsgariAciklamaUzunlugu, List<string> BelgeTurleri)
{
    /// <summary>MASAK-370 tur 3: sunucunun bildirdiği, kimliği FİİLEN isteyen en düşük tutar
    /// (/masak/esikler → kimlikIstenenEnDusukTutar = min(md.5/1-b kimlik eşiği, amaç beyanı kademesi)).
    /// Eski sunucu alanı göndermezse 0 gelir.</summary>
    public decimal KimlikIstenenEnDusukTutar { get; init; }

    /// <summary>Ekranın MASAK formunu önceden açtığı tutar. Sunucu değeri varsa o; yoksa güvenli (fail-closed)
    /// yön: iki eşiğin küçüğü — formu sunucunun reddedeceği tutardan daha geç açmaz. Sayı istemciye gömülmez.</summary>
    public decimal KimlikIstenenTutar() =>
        KimlikIstenenEnDusukTutar > 0m ? KimlikIstenenEnDusukTutar
        : AmacBeyaniEsigi > 0m && AmacBeyaniEsigi < KimlikEsigi ? AmacBeyaniEsigi : KimlikEsigi;
}
