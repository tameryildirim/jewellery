namespace Kuyumcu.Web.Servisler;

/// <summary>
/// UX-4d madde 5 — POS'ta nakit ödeme önerisi.
///
/// <para>
/// ⚠ NEDEN VAR: hurda/alış ekranında ödeme türü varsayılanı NAKİT'ti. 459 sıra
/// nolu VUK Genel Tebliği'nin tevsik haddi (30.000 TL) yüzünden gerçek
/// tutarların çoğunda sunucu bu işlemi REDDEDİYOR, kuyumcu nedenini ekranda
/// göremiyordu (QA turu 1 fikstür bulgusu).
/// </para>
/// <para>
/// ⚠ SUNUCU KURALI DEĞİŞMEDİ: bu yalnız bir ÖNERİDİR (UX-S30 — kilitli/riskli
/// yol sebebini söyler). Kapı <c>TevsikKapisi</c>'dır ve olduğu gibi durur.
/// Ekran tek başına karar vermez; haddi de sunucunun önizleme ucundan alır.
/// </para>
/// </summary>
public static class TevsikOneri
{
    /// <summary>Nakit tevsik haddini aşıyor mu — banka yolu önerilmeli mi?</summary>
    public static bool Gerekli(string? odemeTuru, string? islemTuru, decimal toplamTl, decimal had) =>
        odemeTuru == "NAKIT"
        && islemTuru is "ALIS" or "HURDA_ALIS" or "SATIS"
        && had > 0m
        && toplamTl > had;   // t12: sunucu (TevsikKurali/TevsikKapisi "olcut > Had") ve 459 VUK GT §4.1 "aşan" — tam hadde nakit serbest
}
