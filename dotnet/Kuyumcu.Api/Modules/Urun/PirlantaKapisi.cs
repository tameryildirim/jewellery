using Kuyumcu.Api.Common;
using Kuyumcu.Core.Domain;

namespace Kuyumcu.Api.Modules.Urun;

/// <summary>
/// Pırlanta 4C tutarlılığı (paket 22). FAIL-CLOSED: PIRLANTA tipinde karat zorunlu;
/// PIRLANTA dışı tipe 4C yazılamaz (yanlış tip sessizce kabul edilmez); sertifika no
/// varsa kurum zorunlu; listeler kapalı.
/// </summary>
public static class PirlantaKapisi
{
    public static void Dogrula(string tip, decimal? karat, string? renk, string? berraklik, string? kesim, string? kurum, string? sertifikaNo)
    {
        var pirlanta = tip == "PIRLANTA";
        var dortC = karat is not null || renk is not null || berraklik is not null || kesim is not null;

        if (pirlanta && karat is null or <= 0m)
            throw new BadRequestException("Pırlanta üründe ana taş karatı zorunludur (4C)");
        if (!pirlanta && dortC)
            throw new BadRequestException($"4C alanları yalnız PIRLANTA tipinde girilir (tip: {tip})");
        if (renk is not null && !Pirlanta.Renkler.Contains(renk))
            throw new BadRequestException($"Geçersiz renk: {renk} (D–Z)");
        if (berraklik is not null && !Pirlanta.Berrakliklar.Contains(berraklik))
            throw new BadRequestException($"Geçersiz berraklık: {berraklik}");
        if (kesim is not null && !Pirlanta.Kesimler.Contains(kesim))
            throw new BadRequestException($"Geçersiz kesim: {kesim}");
        if (!string.IsNullOrWhiteSpace(sertifikaNo) && string.IsNullOrWhiteSpace(kurum))
            throw new BadRequestException("Sertifika numarası için sertifika kurumu zorunludur");
        // Kurum: listeden biri ya da DIGER için serbest ad ("DIGER:Laboratuvar X")
        if (kurum is not null && !Pirlanta.SertifikaKurumlari.Contains(kurum) && !kurum.StartsWith("DIGER:", StringComparison.Ordinal))
            throw new BadRequestException($"Geçersiz sertifika kurumu: {kurum} (GIA/HRD/IGI/EGL ya da 'DIGER:ad')");
    }

    /// <summary>
    /// PDF magic byte + boyut. İçerik türü beyanına GÜVENİLMEZ, bayta bakılır.
    ///
    /// ⚠ Paket 4: gövde <see cref="BelgeKapisi.IcerikDogrula"/>'ya DELEGE EDER.
    /// İmza ve dışa bakan davranış (hata metinleri dâhil) değişmez; sertifika
    /// türünün "yalnız PDF" kuralı artık tek yerde durur.
    /// </summary>
    public static void PdfDogrula(byte[] icerik) =>
        BelgeKapisi.IcerikDogrula(icerik, Data.BelgeTuru.Sertifika, out _);
}
