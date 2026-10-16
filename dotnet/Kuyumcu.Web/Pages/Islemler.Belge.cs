using System.Text.Json;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// M6 (müşteri 16.10: "fişe hiç gerek yok… sisteme girdiğim anda fatura kessin";
// "30 bin lira geçtiğinde isim konulmalı") — Tezgâh satış belgesi: Fiş | Fatura.
//   • Varsayılan işletme ayarından (GET /satis-belge); her satışta değiştirilebilir.
//   • Kuyumcu fatura haddini (VUK GT 514: genel haddin 3 katı) AŞAN satışta Fatura zorunlu.
//   • Fatura seçiliyken gerçek müşteri şart (Peşin Müşteri'ye fatura kesilmez — sunucu da 400).
//   • Satış TEK fiş ucuna gider; sunucu satış + faturayı aynı istekte yapar.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    private string belgeTuru = "FIS";
    private string belgeVarsayilan = "FIS";
    private decimal? kuyumcuFaturaHaddi;
    private string? sonFaturaNotu;

    /// <summary>Belge seçimi yalnız satışta (iade/alış/takas… değil).</summary>
    private bool BelgeSecimiGorunur => tur == "SATIS" && !IadeKipi;

    /// <summary>Kuyumcu fatura haddi aşıldı mı (satış toplamı)?</summary>
    private bool FaturaZorunlu => BelgeSecimiGorunur && kuyumcuFaturaHaddi is { } h && ToplamTl > h;

    private bool FaturaSecili => BelgeSecimiGorunur && (belgeTuru == "FATURA" || FaturaZorunlu);

    /// <summary>Kaydet kilidi: fatura seçiliyken gerçek müşteri yoksa sebep (UX-S30 — görünmez kural yok).</summary>
    private string? BelgeKilidi =>
        FaturaSecili && (string.IsNullOrEmpty(cariId) || cariId == pesinCariId) ? SatisBelgeSozlugu.MusteriSecin(Dil.Dil) : null;

    /// <summary>Satış gövdesine giden belge türü (fişte alan gönderilmez → yanıt ve davranış eskisi gibi).</summary>
    private string? BelgeGovde => FaturaSecili ? "FATURA" : null;

    private string? BelgeNotu => FaturaZorunlu && kuyumcuFaturaHaddi is { } h
        ? SatisBelgeSozlugu.Zorunlu(Bicim.Tl(h), Dil.Dil) : null;

    private async Task BelgeAyariYukle()
    {
        try
        {
            var y = await Api.GetAsync<JsonElement>("/satis-belge");
            belgeVarsayilan = y.TryGetProperty("varsayilan", out var v) && v.GetString() == "FATURA" ? "FATURA" : "FIS";
            kuyumcuFaturaHaddi = y.TryGetProperty("kuyumcuFaturaHaddi", out var h) && h.ValueKind == JsonValueKind.Number ? h.GetDecimal() : null;
            belgeTuru = belgeVarsayilan;
        }
        catch (Exception ex) when (ex is ApiHata or JsonException)
        {
            // Ayar okunamazsa bugünkü davranış (Fiş) sürer; satış engellenmez.
        }
    }

    private void BelgeSec(string t)
    {
        if (t is "FIS" or "FATURA") belgeTuru = t;
    }

    /// <summary>
    /// Faturalı satış yanıtı { satis, fatura, faturaHata } biçimindedir: satış kısmı fiş okuyucularına
    /// (FisYaniti) AYNEN verilir; fatura sonucu banda tek satır olarak yazılır. Fişli yanıt değişmeden döner.
    /// </summary>
    private JsonElement BelgeYaniti(JsonElement y)
    {
        if (y.ValueKind != JsonValueKind.Object || !y.TryGetProperty("satis", out var satis)) return y;
        if (y.TryGetProperty("faturaHata", out var fh) && fh.ValueKind == JsonValueKind.String)
            sonFaturaNotu = SatisBelgeSozlugu.FaturaOlusmadi(fh.GetString()!, Dil.Dil);
        else if (y.TryGetProperty("fatura", out var f) && f.ValueKind == JsonValueKind.Object)
            sonFaturaNotu = FaturaNo(f) is { Length: > 0 } no
                ? SatisBelgeSozlugu.FaturaKesildi(no, Dil.Dil)
                : SatisBelgeSozlugu.FaturaTaslak(Dil.Dil);
        return satis;
    }

    private static string? FaturaNo(JsonElement f)
    {
        foreach (var ad in new[] { "FaturaNo", "faturaNo" })
            if (f.TryGetProperty(ad, out var n) && n.ValueKind == JsonValueKind.String) return n.GetString();
        return null;
    }
}
