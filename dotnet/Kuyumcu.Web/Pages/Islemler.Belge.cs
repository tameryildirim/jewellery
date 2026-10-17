using System.Text.Json;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// M6 (müşteri 16.10: "fişe hiç gerek yok… sisteme girdiğim anda fatura kessin";
// "30 bin lira geçtiğinde isim konulmalı") — Tezgâh satış belgesi.
// 17.10 kurucu: "fiş her zaman oluşacağından seçenek kalksın; fatura da her zaman oluşsun ama müşteriye sorsun
// fatura kesilsin mi diye; zorunlu durumlarda hayır işaretlenirse emin misiniz sınırı aştınız uyarısı verilsin,
// evet derse fatura da kesilir".
//   • Fiş | Fatura seçimi YOK. Satış fişi her satışta arka planda oluşur.
//   • Müşterili satışta Kaydet'e basınca "Fatura kesilsin mi?" (Evet | Hayır) sorulur; işletme ayarı
//     (GET /satis-belge "varsayilan") hangi cevabın birincil düğme olduğunu söyler.
//   • Hayır + kuyumcu fatura haddi (VUK GT 514) aşıldı → "sınırı aştınız, emin misiniz?" → Evet: fatura kesilir.
//   • Peşin Müşteri'de soru yok (fatura kesilemez — sunucu da 400); had üstünde "Fatura için müşteri seçin" kilidi.
//   • Havaleden satış faturalıdır, sorulmaz (kurucu: "direkt fatura kesilir").
//   • Satış TEK fiş ucuna gider; sunucu satış + faturayı aynı istekte yapar.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    /// <summary>Bu satışın fatura cevabı: null = henüz sorulmadı, "FATURA" | "FIS".</summary>
    private string? belgeTuru;
    private string belgeVarsayilan = "FIS";
    private bool belgeSorusuAcik, belgeSinirUyarisi;
    /// <summary>Soru "Açık hesap olarak kaydet"ten açıldı: cevaptan sonra o yol sürer (veresiye satırı cevaptan SONRA eklenir).</summary>
    private bool belgeSonraAcikHesap;
    private decimal? kuyumcuFaturaHaddi;
    private string? sonFaturaNotu;

    /// <summary>Belge seçimi yalnız satışta (iade/alış/takas… değil).</summary>
    private bool BelgeSecimiGorunur => tur == "SATIS" && !IadeKipi;

    /// <summary>Kuyumcu fatura haddi aşıldı mı (satış toplamı)?</summary>
    private bool FaturaZorunlu => BelgeSecimiGorunur && kuyumcuFaturaHaddi is { } h && ToplamTl > h;

    private bool FaturaSecili => BelgeSecimiGorunur && (belgeTuru == "FATURA" || FaturaZorunlu);

    /// <summary>Kaydet kilidi: fatura seçiliyken gerçek müşteri yoksa sebep (UX-S30 — görünmez kural yok).</summary>
    private string? BelgeKilidi =>
        FaturaSecili && (string.IsNullOrEmpty(cariId) || cariId == pesinCariId) ? SatisBelgeSozlugu.MusteriSecin(Dil.Dil)
        : HavaleKilidi;   // 17.10 havaleden satış: fiş toplamı gelen havaleye eşit olmalı

    /// <summary>Satış gövdesine giden belge türü (fişte alan gönderilmez → yanıt ve davranış eskisi gibi).</summary>
    private string? BelgeGovde => FaturaSecili ? "FATURA" : null;

    private string? BelgeNotu => HavaleNotu ?? (FaturaZorunlu && kuyumcuFaturaHaddi is { } h
        ? SatisBelgeSozlugu.Zorunlu(Bicim.Tl(h), Dil.Dil) : null);

    /// <summary>Fatura sorusu bu satışta sorulur mu: satış, havale değil, gerçek müşteri seçili.</summary>
    private bool BelgeSorulur => BelgeSecimiGorunur && !HavaleModu && !string.IsNullOrEmpty(cariId) && cariId != pesinCariId;

    /// <summary>Kaydet'in ilk adımı: cevap yoksa soruyu açar ve kaydı durdurur (klavye kısayolu dahil her yol).</summary>
    private bool BelgeSorusuAc()
    {
        if (!BelgeSorulur || belgeTuru is not null) return false;
        belgeSorusuAcik = true; belgeSinirUyarisi = false;
        return true;
    }

    /// <summary>Soru cevabı: "FATURA" (Evet) | "FIS" (Hayır) | "GERI" (sınır uyarısında geri dön).</summary>
    private async Task BelgeCevapla(string cevap)
    {
        if (cevap == "GERI") { belgeSorusuAcik = false; belgeSinirUyarisi = false; belgeSonraAcikHesap = false; return; }
        if (cevap == "FIS" && FaturaZorunlu) { belgeSinirUyarisi = true; return; }   // "emin misiniz, sınırı aştınız"
        belgeTuru = cevap == "FATURA" ? "FATURA" : "FIS";
        belgeSorusuAcik = false; belgeSinirUyarisi = false;
        if (belgeSonraAcikHesap) { belgeSonraAcikHesap = false; await AcikHesapKaydet(); }
        else await Kaydet();
    }

    private async Task BelgeAyariYukle()
    {
        try
        {
            var y = await Api.GetAsync<JsonElement>("/satis-belge");
            belgeVarsayilan = y.TryGetProperty("varsayilan", out var v) && v.GetString() == "FATURA" ? "FATURA" : "FIS";
            kuyumcuFaturaHaddi = y.TryGetProperty("kuyumcuFaturaHaddi", out var h) && h.ValueKind == JsonValueKind.Number ? h.GetDecimal() : null;
        }
        catch (Exception ex) when (ex is ApiHata or JsonException)
        {
            // Ayar okunamazsa bugünkü davranış (Fiş) sürer; satış engellenmez.
        }
    }

    /// <summary>
    /// Faturalı satış yanıtı { satis, fatura, faturaHata } biçimindedir: satış kısmı fiş okuyucularına
    /// (FisYaniti) AYNEN verilir; fatura sonucu banda tek satır olarak yazılır. Fişli yanıt değişmeden döner.
    /// </summary>
    private JsonElement BelgeYaniti(JsonElement y)
    {
        HavaleKapat();   // 17.10: başarılı satışla havale modu biter
        if (y.ValueKind != JsonValueKind.Object || !y.TryGetProperty("satis", out var satis))
        {
            if (belgeTuru == "FIS") sonFaturaNotu = SatisBelgeSozlugu.FaturaFissiz(Dil.Dil);   // müşteri "hayır" dedi
            return y;
        }
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
