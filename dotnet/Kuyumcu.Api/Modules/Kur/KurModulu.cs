using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Api.Modules.Kur;

// ═════════════════════════════════════════════════════════════════════════════
// Kur modülü — NestJS portu.
// Kaynak: apps/api/src/modules/kur/*
//   • kur.controller.ts / kur.service.ts → KurController / KurServisi
//   • adaptorler/*                       → IKurAdaptoru / AltinApiAdaptoru
//   • kur.scheduler.ts (@Cron)           → KurZamanlayici (BackgroundService)
// Rota ve JSON alan adları NestJS API'siyle birebir aynı (Türkçe camelCase).
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class ManuelKurDto
{
    // PAKET GOC-A (envanter açık #8): kur alanları Core K16 kuralıyla (> 0, tavan) + kolon decimal(15,4)
    // ondalığı servis içinde (GirdiKapisi.Kur … 4). Eski [Range(1, …)] ekranla çelişiyordu (ekran > 0);
    // isteğe bağlı alanlarda 0 SESSİZCE "girilmedi" sayılıyordu — artık 400 (boş bırakılır, 0 yazılmaz).
    [Required(ErrorMessage = "Altın has fiyatı sayı olmalıdır")]
    [Kur]
    public decimal AltinHasFiyati { get; set; }

    /// <summary>Opsiyonel: piyasa gram altın (24 ayar) satış fiyatı — vitrin için.</summary>
    [Kur] public decimal? GramAltinFiyati { get; set; }

    [Kur] public decimal? UsdTry { get; set; }
    [Kur] public decimal? EurTry { get; set; }
    [Kur] public decimal? GbpTry { get; set; }

    // PAKET KUR-BANT (09.10): makul bant dışı fiyat (FiyatBandi) yalnız SAHİP/MÜDÜR'ün
    // "fiyat doğru" onayı + gerekçesiyle (≥ 20, K30) kaydedilir; onay denetim izine yazılır.
    /// <summary>Bant dışı fiyatın bilerek kaydedilmesi (yalnız SAHİP/MÜDÜR).</summary>
    public bool? BantOnay { get; set; }

    /// <summary>Bant aşımı gerekçesi (K30: en az 20 karakter; servis içinde denetlenir).</summary>
    [StringLength(AlanSinirlari.GerekceEnCok)]
    public string? BantGerekce { get; set; }
}

public sealed class KaynakSecDto
{
    /// <summary>Kur kaynağı kodu (ALTINKAYNAK · ANKARA · TRUNCGIL)</summary>
    [Required, MaxLength(20)]
    public string Kaynak { get; set; } = null!;
}

public sealed class KurFiltreDto
{
    public DateTime? Baslangic { get; set; }
    public DateTime? Bitis { get; set; }

    [RegularExpression("ALTINKAYNAK|ANKARA|TRUNCGIL|HAREM|KAPALICARSI|IAB|MANUAL", ErrorMessage = "Geçersiz kaynak")]
    public string? Kaynak { get; set; }

    [Range(1, 500)]
    public int? Limit { get; set; }
}

/// <summary>Ayar bazlı fiyat — 1 gram bu ayarda kaç TL (Nest: AyarFiyati).</summary>
public sealed record AyarFiyati(
    string AyarId,
    string Label,
    int Milyem,
    decimal GramFiyati,   // TL/gram (has fiyatı × milyem/1000)
    decimal HasFiyati);   // TL/has gram (referans)

/// <summary>Zenginleştirilmiş kur snapshot yanıtı — Nest snapshotZenginlestir çıktısıyla birebir.</summary>
public sealed record KurSnapshotCevap(
    string Id,
    string TenantId,
    decimal AltinHasFiyati,
    decimal? GramAltinFiyati,
    decimal? UsdTry,
    decimal? EurTry,
    decimal? GbpTry,
    string Kaynak,
    DateTime Olusturuldu,
    List<AyarFiyati> AyarFiyatlari,
    // PAKET S-3b: yanlis girilen MANUEL kur GECERSIZ kilinabilir; satir silinmez.
    bool IptalEdildi = false,
    string? IptalGerekce = null,
    DateTime? IptalTarihi = null);

// ── Pariteler (sarrafiye kalemleri) ─────────────────────────────────────────

/// <summary>
/// Kanonik parite kodları — KAYNAKTAN BAĞIMSIZ. Her kaynak kendi kodunu
/// (Altınkaynak "PC", Ankara "YENI-CEYREK", Truncgil "ceyrek-altin") bu ortak
/// koda eşler. Ürün kartı bu koda bağlanır; kaynak değişse de eşleşme bozulmaz.
/// </summary>
public static class PariteKodlari
{
    public const string Has = "HAS";
    public const string Gram = "GRAM";
    public const string Kulce = "KULCE";
    public const string CeyrekYeni = "CEYREK_YENI";
    public const string CeyrekEski = "CEYREK_ESKI";
    public const string YarimYeni = "YARIM_YENI";
    public const string YarimEski = "YARIM_ESKI";
    public const string TamYeni = "TAM_YENI";
    public const string TamEski = "TAM_ESKI";
    public const string AtaYeni = "ATA_YENI";
    public const string AtaEski = "ATA_ESKI";
    public const string Resat = "RESAT";
    public const string Hamit = "HAMIT";
    public const string GremseYeni = "GREMSE_YENI";
    public const string GremseEski = "GREMSE_ESKI";
    public const string Besli = "BESLI";
    public const string Bilezik22 = "BILEZIK_22";
    public const string Ayar14 = "AYAR_14";
    public const string Ayar18 = "AYAR_18";
    public const string Hurda22 = "HURDA_22";
    public const string Gumus = "GUMUS";
    public const string Ons = "ONS";

    /// <summary>Kod → görünen ad (arayüz etiketleri).</summary>
    public static readonly IReadOnlyDictionary<string, string> Adlar = new Dictionary<string, string>
    {
        [Has] = "Has Altın (gram)",
        [Gram] = "Gram Altın",
        [Kulce] = "Külçe Altın",
        [CeyrekYeni] = "Yeni Çeyrek",
        [CeyrekEski] = "Eski Çeyrek",
        [YarimYeni] = "Yeni Yarım",
        [YarimEski] = "Eski Yarım",
        [TamYeni] = "Yeni Tam (Teklik)",
        [TamEski] = "Eski Tam (Teklik)",
        [AtaYeni] = "Yeni Ata Lira",
        [AtaEski] = "Eski Ata Lira",
        [Resat] = "Reşat Altın",
        [Hamit] = "Hamit Altın",
        [GremseYeni] = "Yeni Gremse",
        [GremseEski] = "Eski Gremse",
        [Besli] = "Ata Beşli",
        [Bilezik22] = "22 Ayar Bilezik",
        [Ayar14] = "14 Ayar Altın",
        [Ayar18] = "18 Ayar Altın",
        [Hurda22] = "22 Ayar Hurda",
        [Gumus] = "Gümüş (gram)",
        [Ons] = "ONS Altın",
    };

    public static string Ad(string kod) => Adlar.TryGetValue(kod, out var a) ? a : kod;
    public static bool Gecerli(string? kod) => !string.IsNullOrEmpty(kod) && Adlar.ContainsKey(kod);

    // ── Fiyat birimi ─────────────────────────────────────────────────────────
    // Kaynaklar bazı kalemleri ADET, bazılarını GRAM başına kote eder.
    // Çeyrek/yarım/ata bir parçanın fiyatıdır; "22 Ayar Bilezik" ya da "14 Ayar"
    // ise gram fiyatıdır. Ürün fiyatı buna göre çarpılır — karıştırılırsa
    // 20 gramlık bilezik tek gramın fiyatına satılır.
    public const string BirimAdet = "ADET";       // fiyat = kotasyon
    public const string BirimGram = "GRAM";       // fiyat = kotasyon × net gram
    public const string BirimHasGram = "HAS_GRAM"; // fiyat = kotasyon × has gram

    private static readonly IReadOnlyDictionary<string, string> Birimler = new Dictionary<string, string>
    {
        [Has] = BirimHasGram,
        [Gram] = BirimGram,
        [Kulce] = BirimGram,
        [Bilezik22] = BirimGram,
        [Ayar14] = BirimGram,
        [Ayar18] = BirimGram,
        [Hurda22] = BirimGram,
        [Gumus] = BirimGram,
        [Ons] = BirimAdet,
    };

    /// <summary>Kotasyonun birimi; listede yoksa ADET (sarrafiye parçası).</summary>
    public static string Birim(string kod) => Birimler.TryGetValue(kod, out var b) ? b : BirimAdet;

    /// <summary>
    /// Otomatik eşleşmeye AÇIK kodlar. Yalnız kotasyonu doğrudan ürünün fiyatı
    /// olan kalemler (sarrafiye parçaları + gram/külçe) otomatik bağlanır.
    /// "22 Ayar Bilezik", "14/18 Ayar", "Hurda", "Gümüş" gram kotasyonları
    /// dükkânın kendi işçiliğini ezeceği için OTOMATİK bağlanmaz — kullanıcı
    /// isterse elle seçer.
    /// </summary>
    private static readonly HashSet<string> OtomatikEslesir =
    [
        Gram, Kulce, CeyrekYeni, CeyrekEski, YarimYeni, YarimEski,
        TamYeni, TamEski, AtaYeni, AtaEski, Resat, Hamit,
        GremseYeni, GremseEski, Besli,
    ];

    /// <summary>Türkçe harfleri sadeleştirir (Külçe → kulce).</summary>
    private static string Sadelestir(string? metin) =>
        (metin ?? "").ToLowerInvariant()
            .Replace("ı", "i").Replace("İ", "i").Replace("ş", "s").Replace("ç", "c")
            .Replace("ü", "u").Replace("ö", "o").Replace("ğ", "g").Replace("â", "a");

    /// <summary>
    /// Ürün adından parite tahmini — yeni ürün eklenirken otomatik eşleşme.
    /// Bulamazsa null döner (fiyat has × kur + işçilik ile hesaplanır).
    /// </summary>
    public static string? Tahmin(string ad, string? tip = null)
    {
        var m = Sadelestir(ad);
        bool V(params string[] k) => k.Any(x => m.Contains(x));
        var eski = V("eski");

        string? kod =
            V("resat") ? Resat
            : V("hamit") ? Hamit
            : V("besli", "5'li", "5 li", "beslik") ? Besli
            : V("gremse", "gremise") ? (eski ? GremseEski : GremseYeni)
            : V("ata") ? (eski ? AtaEski : AtaYeni)
            : V("ceyrek") ? (eski ? CeyrekEski : CeyrekYeni)
            : V("yarim") ? (eski ? YarimEski : YarimYeni)
            : V("teklik", "tam altin") ? (eski ? TamEski : TamYeni)
            : V("kulce") ? Kulce
            : V("gram altin") ? Gram
            : null;

        return kod is not null && OtomatikEslesir.Contains(kod) ? kod : null;
    }
}

/// <summary>
/// Bir sarrafiye kaleminin kaynaktaki alış/satış fiyatı.
/// <paramref name="Birim"/>: ADET · GRAM · HAS_GRAM — fiyat buna göre çarpılır.
/// </summary>
public sealed record Parite(string Kod, string Ad, decimal? Alis, decimal? Satis, string Birim);

// ── Dış kur kaynağı adaptörü ─────────────────────────────────────────────────

/// <summary>Dış kaynaktan gelen ham kur verisi (Nest: DisKurVerisi).</summary>
public sealed record DisKurVerisi(
    decimal AltinHasFiyati,
    decimal? GramAltinFiyati,
    decimal? UsdTry,
    decimal? EurTry,
    decimal? GbpTry,
    string Kaynak,
    IReadOnlyList<Parite>? Pariteler = null);

/// <summary>Dış kur kaynağı adaptörü — her kaynak bu arayüzü uygular.</summary>
public interface IKurAdaptoru
{
    /// <summary>Kaynak kodu (kur_snapshots.kaynak ve tenants.kur_kaynagi ile aynı değer).</summary>
    string Kod { get; }

    Task<DisKurVerisi> KurCek(CancellationToken iptal = default);
}

/// <summary>Adaptörlerin ortak yardımcıları: sayı okuma ve parite listesi kurma.</summary>
internal static class AdaptorYardim
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>"6.787,27" (tr-TR) ya da JSON sayı → decimal; geçersizse null.</summary>
    public static decimal? Sayi(JsonElement oge, string alan, bool trBicim)
    {
        if (!oge.TryGetProperty(alan, out var d)) return null;
        decimal? deger = d.ValueKind switch
        {
            JsonValueKind.Number when d.TryGetDecimal(out var n) => n,
            JsonValueKind.String => Metin(d.GetString(), trBicim),
            _ => null,
        };
        return deger is > 0 ? Math.Round(deger.Value, 4, MidpointRounding.AwayFromZero) : null;
    }

    public static decimal? Metin(string? ham, bool trBicim)
    {
        if (string.IsNullOrWhiteSpace(ham)) return null;
        var temiz = ham.Replace("$", "").Replace("₺", "").Replace("%", "").Trim();
        var kultur = trBicim ? Tr : CultureInfo.InvariantCulture;
        return decimal.TryParse(temiz, NumberStyles.Number, kultur, out var d) && d > 0
            ? Math.Round(d, 4, MidpointRounding.AwayFromZero)
            : null;
    }

    /// <summary>Kaynak kodu → kanonik kod eşlemesinden parite listesi kurar.</summary>
    public static List<Parite> PariteKur(
        Dictionary<string, (decimal? Alis, decimal? Satis)> ham,
        IEnumerable<(string KaynakKodu, string KanonikKod)> esleme)
    {
        var liste = new List<Parite>();
        foreach (var (kaynakKodu, kanonik) in esleme)
        {
            if (!ham.TryGetValue(kaynakKodu, out var f)) continue;
            if (f.Alis is null && f.Satis is null) continue;
            liste.Add(new Parite(kanonik, PariteKodlari.Ad(kanonik), f.Alis, f.Satis,
                PariteKodlari.Birim(kanonik)));
        }
        return liste;
    }
}

/// <summary>
/// truncgil finans adaptörü — ücretsiz, anahtarsız Türk altın + döviz verisi.
/// Kaynak: https://finans.truncgil.com/today.json
/// Yanıt: anahtar → { "Alış", "Tür", "Satış", "Değişim" }, değerler tr-TR.
/// Yeni/eski ziynet ayrımı YOKTUR — tek çeyrek/yarım verir, yeni olarak eşlenir.
/// </summary>
public sealed class TruncgilAdaptoru(IHttpClientFactory httpFactory, ILogger logger) : IKurAdaptoru
{
    public string Kod => "TRUNCGIL";

    private const string Url = "https://finans.truncgil.com/today.json";

    private static readonly (string, string)[] Esleme =
    [
        ("gram-has-altin", PariteKodlari.Has),
        ("gram-altin", PariteKodlari.Gram),
        ("ceyrek-altin", PariteKodlari.CeyrekYeni),
        ("yarim-altin", PariteKodlari.YarimYeni),
        ("tam-altin", PariteKodlari.TamYeni),
        ("ata-altin", PariteKodlari.AtaYeni),
        ("resat-altin", PariteKodlari.Resat),
        ("hamit-altin", PariteKodlari.Hamit),
        ("gremse-altin", PariteKodlari.GremseYeni),
        ("besli-altin", PariteKodlari.Besli),
        ("22-ayar-bilezik", PariteKodlari.Bilezik22),
        ("14-ayar-altin", PariteKodlari.Ayar14),
        ("18-ayar-altin", PariteKodlari.Ayar18),
        ("gumus", PariteKodlari.Gumus),
        ("ons", PariteKodlari.Ons),
    ];

    public async Task<DisKurVerisi> KurCek(CancellationToken iptal = default)
    {
        using var zamanAsimi = CancellationTokenSource.CreateLinkedTokenSource(iptal);
        zamanAsimi.CancelAfter(TimeSpan.FromSeconds(8));

        var http = httpFactory.CreateClient();
        using var istek = new HttpRequestMessage(HttpMethod.Get, Url);
        istek.Headers.Accept.ParseAdd("application/json");

        using var cevap = await http.SendAsync(istek, zamanAsimi.Token);
        if (!cevap.IsSuccessStatusCode)
            throw new HttpRequestException($"truncgil HTTP {(int)cevap.StatusCode}");

        var govde = await cevap.Content.ReadAsStringAsync(zamanAsimi.Token);
        using var doc = JsonDocument.Parse(govde);
        var kok = doc.RootElement;

        var ham = new Dictionary<string, (decimal? Alis, decimal? Satis)>(StringComparer.OrdinalIgnoreCase);
        foreach (var oge in kok.EnumerateObject())
        {
            if (oge.Value.ValueKind != JsonValueKind.Object) continue;
            ham[oge.Name] = (AdaptorYardim.Sayi(oge.Value, "Alış", true),
                             AdaptorYardim.Sayi(oge.Value, "Satış", true));
        }

        decimal? Satis(string k) => ham.TryGetValue(k, out var f) ? f.Satis : null;

        var has = Satis("gram-has-altin") ?? Satis("gram-altin")
            ?? throw new InvalidOperationException("truncgil: has altın fiyatı bulunamadı");

        var usd = Satis("USD");
        if (usd is null && Satis("EUR") is null && Satis("GBP") is null)
            logger.LogWarning("truncgil: döviz kurları okunamadı, null kaydedilecek");

        return new DisKurVerisi(has, Satis("gram-altin"), usd, Satis("EUR"), Satis("GBP"), Kod,
            AdaptorYardim.PariteKur(ham, Esleme));
    }
}

/// <summary>
/// Altınkaynak adaptörü — kuyumcuların fiilen ekranında tuttuğu toptancı.
/// Kaynak: https://static.altinkaynak.com/public/Gold · /public/Currency
/// Anahtar GEREKTİRMEZ. Yanıt: [{ "Alis","Satis","Kod","Aciklama","GuncellenmeZamani" }]
/// Değerler tr-TR biçiminde ("6.787,27").
/// </summary>
public sealed class AltinkaynakAdaptoru(IHttpClientFactory httpFactory, ILogger logger) : IKurAdaptoru
{
    public string Kod => "ALTINKAYNAK";

    private const string AltinUrl = "https://static.altinkaynak.com/public/Gold";
    private const string DovizUrl = "https://static.altinkaynak.com/public/Currency";

    private static readonly (string, string)[] Esleme =
    [
        ("HH_T", PariteKodlari.Has),
        ("GA", PariteKodlari.Gram),
        ("CH_T", PariteKodlari.Kulce),
        ("PC", PariteKodlari.CeyrekYeni),
        ("EC", PariteKodlari.CeyrekEski),
        ("PY", PariteKodlari.YarimYeni),
        ("EY", PariteKodlari.YarimEski),
        ("PT", PariteKodlari.TamYeni),
        ("ET", PariteKodlari.TamEski),
        ("PA", PariteKodlari.AtaYeni),
        ("EA", PariteKodlari.AtaEski),
        ("PR", PariteKodlari.Resat),
        ("PH", PariteKodlari.Hamit),
        ("PG", PariteKodlari.GremseYeni),
        ("EG", PariteKodlari.GremseEski),
        ("PA5", PariteKodlari.Besli),
        ("PB", PariteKodlari.Bilezik22),
        ("P14", PariteKodlari.Ayar14),
        ("P18", PariteKodlari.Ayar18),
        ("B_T", PariteKodlari.Hurda22),
        ("AG_T", PariteKodlari.Gumus),
        ("XAUUSD", PariteKodlari.Ons),
    ];

    public async Task<DisKurVerisi> KurCek(CancellationToken iptal = default)
    {
        using var zamanAsimi = CancellationTokenSource.CreateLinkedTokenSource(iptal);
        zamanAsimi.CancelAfter(TimeSpan.FromSeconds(8));

        var http = httpFactory.CreateClient();
        var altin = await Kalemler(http, AltinUrl, zamanAsimi.Token);
        var doviz = await Kalemler(http, DovizUrl, zamanAsimi.Token);

        decimal? S(Dictionary<string, (decimal? Alis, decimal? Satis)> d, string k) =>
            d.TryGetValue(k, out var f) ? f.Satis : null;

        var has = S(altin, "HH_T") ?? S(altin, "GA")
            ?? throw new InvalidOperationException("Altınkaynak: has altın fiyatı bulunamadı");

        var usd = S(doviz, "USD");
        if (usd is null && S(doviz, "EUR") is null && S(doviz, "GBP") is null)
            logger.LogWarning("Altınkaynak: döviz kurları okunamadı, null kaydedilecek");

        return new DisKurVerisi(has, S(altin, "GA"), usd, S(doviz, "EUR"), S(doviz, "GBP"), Kod,
            AdaptorYardim.PariteKur(altin, Esleme));
    }

    private static async Task<Dictionary<string, (decimal? Alis, decimal? Satis)>> Kalemler(
        HttpClient http, string url, CancellationToken iptal)
    {
        using var istek = new HttpRequestMessage(HttpMethod.Get, url);
        istek.Headers.Accept.ParseAdd("application/json");

        using var cevap = await http.SendAsync(istek, iptal);
        if (!cevap.IsSuccessStatusCode)
            throw new HttpRequestException($"Altınkaynak HTTP {(int)cevap.StatusCode}");

        var govde = await cevap.Content.ReadAsStringAsync(iptal);
        using var doc = JsonDocument.Parse(govde);

        var sonuc = new Dictionary<string, (decimal? Alis, decimal? Satis)>(StringComparer.OrdinalIgnoreCase);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) return sonuc;

        foreach (var oge in doc.RootElement.EnumerateArray())
        {
            if (oge.ValueKind != JsonValueKind.Object) continue;
            if (!oge.TryGetProperty("Kod", out var k) || k.GetString() is not { Length: > 0 } kod) continue;
            sonuc[kod.Trim()] = (AdaptorYardim.Sayi(oge, "Alis", true), AdaptorYardim.Sayi(oge, "Satis", true));
        }
        return sonuc;
    }
}

/// <summary>
/// Ankara Altın Borsası adaptörü — Ankara ve İç Anadolu kuyumcusunun referansı.
/// Kaynak: https://api.ankaraaltinborsasi.com/graphql/site (GraphQL, anahtarsız)
/// Sorgu: GetLatestGoldPrices → siteLatestGoldPrices { parity buyPrice sellPrice }
///
/// DİKKAT: Saf altın borsasıdır, USD/EUR/GBP **TL kuru yayınlamaz**. Döviz
/// TCMB'nin resmî günlük kurundan tamamlanır (ücretsiz, anahtarsız).
/// </summary>
public sealed class AnkaraAltinBorsasiAdaptoru(IHttpClientFactory httpFactory, ILogger logger) : IKurAdaptoru
{
    public string Kod => "ANKARA";

    private const string GraphqlUrl = "https://api.ankaraaltinborsasi.com/graphql/site";
    private const string Sorgu =
        "query GetLatestGoldPrices { siteLatestGoldPrices { parity buyPrice sellPrice currency } }";

    private static readonly (string, string)[] Esleme =
    [
        ("HAS-ALTIN", PariteKodlari.Has),
        ("GRAM-ALTIN", PariteKodlari.Gram),
        ("KULCE-ALTIN", PariteKodlari.Kulce),
        ("YENI-CEYREK", PariteKodlari.CeyrekYeni),
        ("ESKI-CEYREK", PariteKodlari.CeyrekEski),
        ("YENI-YARIM", PariteKodlari.YarimYeni),
        ("ESKI-YARIM", PariteKodlari.YarimEski),
        ("YENI-TEKLIK", PariteKodlari.TamYeni),
        ("ESKI-TEKLIK", PariteKodlari.TamEski),
        ("YENI-ATA", PariteKodlari.AtaYeni),
        ("ESKI-ATA", PariteKodlari.AtaEski),
        ("RESAT-ALTIN", PariteKodlari.Resat),
        ("HAMIT-ALTIN", PariteKodlari.Hamit),
        ("YENI-GREMISE", PariteKodlari.GremseYeni),
        ("ESKI-GREMISE", PariteKodlari.GremseEski),
        ("YENI-ATA-5LI", PariteKodlari.Besli),
        ("22-AYAR-BILEZIK", PariteKodlari.Bilezik22),
        ("14-AYAR", PariteKodlari.Ayar14),
        ("18-AYAR", PariteKodlari.Ayar18),
        ("HAS-GUMUS", PariteKodlari.Gumus),
        ("XAUUSD", PariteKodlari.Ons),
    ];

    public async Task<DisKurVerisi> KurCek(CancellationToken iptal = default)
    {
        using var zamanAsimi = CancellationTokenSource.CreateLinkedTokenSource(iptal);
        zamanAsimi.CancelAfter(TimeSpan.FromSeconds(10));

        var http = httpFactory.CreateClient();
        var fiyatlar = await AltinFiyatlari(http, zamanAsimi.Token);

        decimal? S(string k) => fiyatlar.TryGetValue(k, out var f) ? f.Satis : null;

        var has = S("HAS-ALTIN") ?? S("KULCE-ALTIN") ?? S("GRAM-ALTIN")
            ?? throw new InvalidOperationException("Ankara Altın Borsası: has altın fiyatı bulunamadı");

        // Döviz bu kaynakta yok → TCMB resmî kuru ile tamamlanır (başarısızsa null)
        var (usd, eur, gbp) = await TcmbDoviz(http, logger, zamanAsimi.Token);

        return new DisKurVerisi(has, S("GRAM-ALTIN"), usd, eur, gbp, Kod,
            AdaptorYardim.PariteKur(fiyatlar, Esleme));
    }

    private static async Task<Dictionary<string, (decimal? Alis, decimal? Satis)>> AltinFiyatlari(
        HttpClient http, CancellationToken iptal)
    {
        using var istek = new HttpRequestMessage(HttpMethod.Post, GraphqlUrl)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { operationName = "GetLatestGoldPrices", query = Sorgu }),
                System.Text.Encoding.UTF8, "application/json"),
        };
        istek.Headers.Accept.ParseAdd("application/json");
        istek.Headers.Referrer = new Uri("https://ankaraaltinborsasi.com/");

        using var cevap = await http.SendAsync(istek, iptal);
        if (!cevap.IsSuccessStatusCode)
            throw new HttpRequestException($"Ankara Altın Borsası HTTP {(int)cevap.StatusCode}");

        var govde = await cevap.Content.ReadAsStringAsync(iptal);
        using var doc = JsonDocument.Parse(govde);

        if (doc.RootElement.TryGetProperty("errors", out var hatalar))
            throw new InvalidOperationException($"Ankara Altın Borsası GraphQL hatası: {hatalar}");

        var sonuc = new Dictionary<string, (decimal? Alis, decimal? Satis)>(StringComparer.OrdinalIgnoreCase);
        if (!doc.RootElement.TryGetProperty("data", out var veri)
            || !veri.TryGetProperty("siteLatestGoldPrices", out var liste)
            || liste.ValueKind != JsonValueKind.Array)
            return sonuc;

        foreach (var oge in liste.EnumerateArray())
        {
            if (!oge.TryGetProperty("parity", out var p) || p.GetString() is not { Length: > 0 } parity) continue;
            sonuc[parity.Trim()] = (AdaptorYardim.Sayi(oge, "buyPrice", false),
                                    AdaptorYardim.Sayi(oge, "sellPrice", false));
        }
        return sonuc;
    }

    /// <summary>TCMB resmî günlük kuru (döviz satış). Erişilemezse üçü de null.</summary>
    internal static async Task<(decimal? Usd, decimal? Eur, decimal? Gbp)> TcmbDoviz(
        HttpClient http, ILogger logger, CancellationToken iptal)
    {
        try
        {
            using var cevap = await http.GetAsync("https://www.tcmb.gov.tr/kurlar/today.xml", iptal);
            if (!cevap.IsSuccessStatusCode) return (null, null, null);

            var xml = System.Xml.Linq.XDocument.Parse(await cevap.Content.ReadAsStringAsync(iptal));
            decimal? Kur(string kod)
            {
                var oge = xml.Root?.Elements("Currency")
                    .FirstOrDefault(e => (string?)e.Attribute("Kod") == kod);
                return AdaptorYardim.Metin((string?)oge?.Element("ForexSelling"), false);
            }
            return (Kur("USD"), Kur("EUR"), Kur("GBP"));
        }
        catch (Exception ex)
        {
            logger.LogWarning("TCMB döviz kuru alınamadı: {Mesaj}", ex.Message);
            return (null, null, null);
        }
    }
}

/// <summary>
/// Kur kaynağı kayıt defteri — hangi kodun hangi adaptöre karşılık geldiğini bilir.
/// Kiracı kendi kaynağını seçer (tenants.kur_kaynagi); seçmezse yapılandırmadaki
/// varsayılan (Kur:VarsayilanKaynak) kullanılır.
/// Ayrıca her kaynağın SON parite listesini bellekte tutar (zamanlayıcı besler).
/// </summary>
public sealed class KurKaynakDefteri
{
    private readonly Dictionary<string, IKurAdaptoru> adaptorler;
    private readonly Dictionary<string, IReadOnlyList<Parite>> sonPariteler =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> sonCekim =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly object kilit = new();

    public KurKaynakDefteri(IHttpClientFactory httpFactory, ILogger<KurKaynakDefteri> logger, IConfiguration yapilandirma)
    {
        // ═════════════════════════════════════════════════════════════════════
        // MÜHÜR 4 EKİ — UX-4d madde 24 (K13): SÖZLEŞMESİZ KAYNAK KAPISI.
        //
        // Truncgil · Altınkaynak · Ankara Altın Borsası adaptörleri ücretsiz ve
        // SÖZLEŞMESİZ uçlardan okur. Pilotta kabul edilebilir; ticari/ücretli
        // sürümde varsayılan olamaz (F-8:105). Bu yüzden TEK yapılandırma
        // anahtarıyla kapatılabilirler:
        //     Kur:SozlesmesizKaynaklarAcik  (bool)
        // Anahtar YOKSA varsayılan KAPALI'dır — tutucu taraf (Anayasa md.1:
        // bilinmeyen tahmin edilmez; sözleşmesiz kaynak kendiliğinden açılmaz).
        // appsettings.Development.json'da açık, appsettings.json'da kapalıdır.
        //
        // Bayrak kapalıyken adaptör hiç KURULMAZ: HTTP çağrısı yapacak nesne
        // ortada olmaz (ikinci bir okuma yolu açılmadı). Etkin kaynak MANUAL
        // (POST /api/v1/kur/manuel) + TCMB resmî döviz kalır.
        // ═════════════════════════════════════════════════════════════════════
        SozlesmesizKaynaklarAcik =
            yapilandirma.GetValue("Kur:SozlesmesizKaynaklarAcik", false);

        adaptorler = new Dictionary<string, IKurAdaptoru>(StringComparer.OrdinalIgnoreCase);
        if (SozlesmesizKaynaklarAcik)
            foreach (var a in new IKurAdaptoru[]
                     {
                         new AltinkaynakAdaptoru(httpFactory, logger),
                         new AnkaraAltinBorsasiAdaptoru(httpFactory, logger),
                         new TruncgilAdaptoru(httpFactory, logger),
                     })
                adaptorler[a.Kod] = a;

        var istenen = yapilandirma["Kur:VarsayilanKaynak"];
        VarsayilanKod = !string.IsNullOrEmpty(istenen) && adaptorler.TryGetValue(istenen, out var v)
            ? v.Kod
            : adaptorler.Count > 0 ? "ALTINKAYNAK" : ManuelKod;
    }

    /// <summary>MANUAL: adaptörü olmayan, elle girilen kaynak kodu.</summary>
    public const string ManuelKod = "MANUAL";

    /// <summary>
    /// Sözleşmesiz (ücretsiz/izinsiz) dış kur kaynakları etkin mi?
    /// Kapalıysa <see cref="Kodlar"/> boştur ve dış çekim denenmez.
    /// </summary>
    public bool SozlesmesizKaynaklarAcik { get; }

    /// <summary>Bu kod sözleşmesiz bir kaynağa mı ait? (ekran uyarı şeridi için)</summary>
    public static bool Sozlesmesiz(string? kod) =>
        kod is "ALTINKAYNAK" or "ANKARA" or "TRUNCGIL";

    /// <summary>Kiracı bir şey seçmemişse kullanılacak kaynak.</summary>
    public string VarsayilanKod { get; }

    /// <summary>Seçilebilir kaynak kodları.</summary>
    public IReadOnlyList<string> Kodlar => adaptorler.Values.Select(a => a.Kod).ToList();

    public bool Gecerli(string? kod) => !string.IsNullOrEmpty(kod) && adaptorler.ContainsKey(kod);

    /// <summary>
    /// Kod geçersiz/boşsa varsayılana düşer — kur akışı hiç durmaz.
    /// Sözleşmesiz kaynaklar KAPALIYSA hiç adaptör yoktur: sessizce bir şey
    /// uydurmak yerine açık bir hata fırlatılır (Anayasa md.1).
    /// </summary>
    public IKurAdaptoru Bul(string? kod)
    {
        if (adaptorler.Count == 0)
            throw new ApiException(503,
                "Dış kur kaynakları kapalı (yalnız pilot kaynaklar sözleşmesizdir). "
                + "Kuru elle girin: Kur ekranı → Manuel kur.",
                "Service Unavailable");
        return Gecerli(kod) ? adaptorler[kod!] : adaptorler[VarsayilanKod];
    }

    /// <summary>Son çekimdeki parite listesini saklar (her kaynak için ayrı).</summary>
    public void PariteleriKaydet(string kaynakKod, IReadOnlyList<Parite>? pariteler)
    {
        if (pariteler is null || pariteler.Count == 0) return;
        lock (kilit)
        {
            sonPariteler[kaynakKod] = pariteler;
            sonCekim[kaynakKod] = DateTime.UtcNow;
        }
    }

    /// <summary>Bellekteki son parite listesi; hiç çekilmemişse boş.</summary>
    public IReadOnlyList<Parite> Pariteler(string kaynakKod)
    {
        lock (kilit)
            return sonPariteler.TryGetValue(kaynakKod, out var p) ? p : [];
    }

    /// <summary>
    /// Bu kaynaktan en son BAŞARIYLA veri çekilen an (UTC); hiç çekilmediyse null.
    /// Vitrin panosunun bayat tespiti buna bakar.
    /// DİKKAT: ölçüt <b>çekim</b> zamanıdır, <b>yazım</b> zamanı değil —
    /// <c>KurServisi.KaydetVeyaMevcut</c> fiyat değişmediyse veritabanına satır
    /// yazmaz; yazım zamanına bakılsaydı sakin piyasada pano kendini boşuna
    /// karartırdı. (PANO-KARAR-2026-09-11.md §4/2)
    /// </summary>
    public DateTime? SonCekim(string kaynakKod)
    {
        lock (kilit)
            return sonCekim.TryGetValue(kaynakKod, out var t) ? t : null;
    }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class KurServisi(
    KuyumcuDbContext db, ILogger<KurServisi> logger,
    IHubContext<KurHub> kurHub, KurKaynakDefteri kaynakDefteri,
    DenetimServisi denetim)
{
    /// <summary>Seçilebilir kaynak kodları + kiracının seçili kaynağı.</summary>
    public async Task<object> Kaynaklar(string tenantId) => new
    {
        kaynaklar = kaynakDefteri.Kodlar,
        varsayilan = kaynakDefteri.VarsayilanKod,
        secili = await TenantKaynagi(tenantId),
        // UX-4d madde 24: ekran "sözleşmesiz kaynak — yalnız pilot" şeridini
        // buna bakarak gösterir; kapalıyken yalnız manuel giriş yolu kalır.
        sozlesmesizKaynaklarAcik = kaynakDefteri.SozlesmesizKaynaklarAcik,
        seciliSozlesmesiz = KurKaynakDefteri.Sozlesmesiz(await TenantKaynagi(tenantId)),
        manuelKod = KurKaynakDefteri.ManuelKod,
    };

    /// <summary>
    /// Kiracının kur kaynağını değiştirir ve YENİ kaynaktan hemen kur çeker
    /// (bekleyen 10 sn'lik tura kalmaz; herkesin çubuğu anında güncellenir).
    /// Kiracının kendi ayarı olduğu için rol kısıtı yoktur.
    /// </summary>
    public async Task<object> KaynakAyarla(string kaynak, string tenantId)
    {
        if (!kaynakDefteri.Gecerli(kaynak))
            throw new BadRequestException(
                $"Geçersiz kur kaynağı: {kaynak}. Seçilebilir: {string.Join(", ", kaynakDefteri.Kodlar)}");

        var kod = kaynakDefteri.Bul(kaynak).Kod;
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId)
            ?? throw new NotFoundException("İşletme bulunamadı");

        tenant.KurKaynagi = kod;
        tenant.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Yeni kaynaktan anında çek; başarısız olursa ayar yine kaydedilmiş kalır.
        string? uyari = null;
        try { await KaydetVeyaMevcut(tenantId, await DisVeriCek(kod)); }
        catch (Exception ex)
        {
            uyari = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir($"Kaynak kaydedildi ama kur şu an çekilemedi: {ex.Message}");
            logger.LogWarning("[{TenantId}] {Kaynak} kaynağından ilk çekim başarısız: {Mesaj}",
                tenantId, kod, ex.Message);
        }

        return new { secili = kod, uyari };
    }

    /// <summary>
    /// Kiracının kaynağındaki sarrafiye fiyatları. Bellekte yoksa (uygulama yeni
    /// açıldı) bir kez canlı çekilir. Ürünler ekranı ve otomatik fiyat bunu kullanır.
    /// </summary>
    public async Task<object> Pariteler(string tenantId)
    {
        var kod = await TenantKaynagi(tenantId);
        var liste = kaynakDefteri.Pariteler(kod);

        if (liste.Count == 0)
        {
            try { liste = (await DisVeriCek(kod)).Pariteler ?? []; }
            catch (Exception ex)
            {
                logger.LogWarning("[{TenantId}] Parite listesi çekilemedi: {Mesaj}", tenantId, ex.Message);
            }
        }

        return new { kaynak = kod, pariteler = liste };
    }

    /// <summary>Kiracının seçtiği kaynak; seçmemişse varsayılan.</summary>
    public async Task<string> TenantKaynagi(string tenantId)
    {
        var kod = await db.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.KurKaynagi)
            .FirstOrDefaultAsync();
        return kaynakDefteri.Gecerli(kod) ? kod! : kaynakDefteri.VarsayilanKod;
    }

    /// <summary>TS @kuyum/core ALTIN_AYARLARI ile birebir aynı değerler.</summary>
    private static readonly (string Id, string Label, int Milyem)[] AltinAyarlari =
    [
        ("K8",  "8 Ayar Altın",  333),
        ("K14", "14 Ayar Altın", 585),
        ("K18", "18 Ayar Altın", 750),
        ("K21", "21 Ayar Altın", 875),
        ("K22", "22 Ayar Altın", 916),
        ("K24", "24 Ayar (Has)", 1000),
    ];

    // ─── En güncel kur ────────────────────────────────────────────────────────

    public async Task<KurSnapshotCevap> GuncelKur(string tenantId)
    {
        // PAKET S-3b: iptal edilmis (gecersiz kilinmis) kur GUNCEL sayilmaz.
        var snapshot = await db.KurSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenantId && !s.IptalEdildi)
            .OrderByDescending(s => s.Olusturuldu)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Henüz kur verisi yok. Lütfen kur güncelleyin.");
        return SnapshotZenginlestir(snapshot);
    }

    // ─── Kur geçmişi ─────────────────────────────────────────────────────────

    public async Task<List<KurSnapshotCevap>> KurGecmisi(string tenantId, KurFiltreDto filtre)
    {
        // PAKET GOC-A (envanter C · kur-gecmis-bas/bit): ters aralık SESSİZCE boş liste dönüyordu —
        // K8e (başlangıç ≤ bitiş, en çok 5 yıl) Core kuralı + sözlük metni, 400 alan:bitis.
        GirdiKapisi.Aralik(filtre.Baslangic is { } b0 ? DateOnly.FromDateTime(b0) : null,
                           filtre.Bitis is { } b1 ? DateOnly.FromDateTime(b1) : null);
        var q = db.KurSnapshots.AsNoTracking().Where(s => s.TenantId == tenantId);

        if (filtre.Baslangic is not null) q = q.Where(s => s.Olusturuldu >= filtre.Baslangic);

        // ⚠ UX-4d madde 9: "bitis" bir GÜN'dür, bir AN değil.
        // Ekran `bitis=2026-09-16` gönderiyor; bu 16 Eylül 00:00 olarak
        // ayrıştırılıyor ve `Olusturuldu <= 00:00` o günün TAMAMINI dışarıda
        // bırakıyordu. Sonuç: POST /kur/manuel 201 döndükten hemen sonra
        // GET /kur/gecmis BOŞ geliyordu (QA turu 1: kur kartı MANUAL 6.750
        // olmuşken geçmiş boştu) — kullanıcıya kaydın kaybolduğunu düşündürüyordu.
        // Saat bileşeni verilmemişse (00:00) bitiş günün SONUNA taşınır.
        if (filtre.Bitis is { } bitis)
        {
            var ustSinir = bitis.TimeOfDay == TimeSpan.Zero ? bitis.Date.AddDays(1) : bitis;
            q = q.Where(s => s.Olusturuldu < ustSinir);
        }
        if (!string.IsNullOrEmpty(filtre.Kaynak)) q = q.Where(s => s.Kaynak == filtre.Kaynak);

        var liste = await q.OrderByDescending(s => s.Olusturuldu)
            .Take(filtre.Limit ?? 100)
            .ToListAsync();

        return liste.Select(SnapshotZenginlestir).ToList();
    }

    // ─── Dış kaynaktan çek ve kaydet ─────────────────────────────────────────

    public async Task<KurSnapshotCevap> DisKaynakKurCek(string tenantId) =>
        (await KaydetVeyaMevcut(tenantId, await DisVeriCek(await TenantKaynagi(tenantId)))).Cevap;

    /// <summary>
    /// Dış kaynaktan yalnız veri çeker, kaydetmez. Zamanlayıcı bunu TEK KEZ
    /// çağırıp sonucu tüm kiracılar için kullanır — aksi halde kiracı sayısı
    /// kadar dış istek atılır ve ücretsiz kaynak bizi kısıtlar.
    /// </summary>
    public async Task<DisKurVerisi> DisVeriCek(string? kaynakKod = null)
    {
        var adaptor = kaynakDefteri.Bul(kaynakKod);
        try
        {
            var veri = await adaptor.KurCek();
            // Sarrafiye fiyatları belleğe alınır; ürün ekranı buradan besleniyor.
            kaynakDefteri.PariteleriKaydet(adaptor.Kod, veri.Pariteler);
            return veri;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Dış kaynak kur çekme hatası ({Kaynak})", adaptor.Kod);
            throw new ApiException(503,
                "Altın kuru şu an dış kaynaktan alınamıyor. Manuel kur girebilirsiniz.",
                "Service Unavailable");
        }
    }

    /// <summary>
    /// Fiyat son snapshot'la aynıysa YENİ SATIR YAZMAZ, mevcut olanı döner.
    /// 10 saniyelik sorgu aralığında her tur satır yazmak günde ~8.600 gereksiz
    /// kayıt üretirdi; yalnız gerçekten değişen fiyat deftere geçer ve push edilir.
    /// </summary>
    public async Task<(KurSnapshotCevap Cevap, bool Yazildi)> KaydetVeyaMevcut(string tenantId, DisKurVerisi veri)
    {
        var son = await db.KurSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenantId && !s.IptalEdildi)
            .OrderByDescending(s => s.Olusturuldu)
            .FirstOrDefaultAsync();

        if (son is not null && Ayni(son, veri))
            return (SnapshotZenginlestir(son), false);

        var cevap = await SnapshotKaydet(tenantId, veri.AltinHasFiyati, veri.GramAltinFiyati,
            veri.UsdTry, veri.EurTry, veri.GbpTry, veri.Kaynak);
        return (cevap, true);
    }

    private static bool Ayni(KurSnapshot son, DisKurVerisi v) =>
        son.Kaynak == v.Kaynak
        && son.AltinHasFiyati == Yuvarla4(v.AltinHasFiyati)
        && son.GramAltinFiyati == Yuvarla4(v.GramAltinFiyati)
        && son.UsdTry == Yuvarla4(v.UsdTry)
        && son.EurTry == Yuvarla4(v.EurTry)
        && son.GbpTry == Yuvarla4(v.GbpTry);

    private static decimal Yuvarla4(decimal d) => Math.Round(d, 4, MidpointRounding.AwayFromZero);
    private static decimal? Yuvarla4(decimal? d) => d is null ? null : Yuvarla4(d.Value);

    // ─── Manuel kur girişi ────────────────────────────────────────────────────

    /// <summary>İç/test yolu (kullanıcısız): bant aşımı ONAYLANAMAZ (fail-closed → 400 / 403).</summary>
    public Task<KurSnapshotCevap> ManuelKurKaydet(ManuelKurDto dto, string tenantId) =>
        ManuelKurKaydetIc(dto, tenantId, null);

    /// <summary>Uç yolu: bant aşımını yalnız SAHİP/MÜDÜR onaylayabilir; onay denetim izine yazılır.</summary>
    public Task<KurSnapshotCevap> ManuelKurKaydet(ManuelKurDto dto, AktifKullanici islem) =>
        ManuelKurKaydetIc(dto, islem.TenantId, islem);

    /// <summary>PAKET KUR-BANT — bant aşımını onaylayabilen roller (TAM eşleşme).</summary>
    public static bool BantOnayYetkili(string? rol) => rol is "SAHIP" or "MUDUR";

    private async Task<KurSnapshotCevap> ManuelKurKaydetIc(ManuelKurDto dto, string tenantId, AktifKullanici? islem)
    {
        // PAKET GOC-A — servis yolu da AYNI Core kuralı (> 0, tavan) + kolon decimal(15,4): 5. ondalık
        // SESSİZCE YUVARLANMAZ (Math.Round(…, 4) eskiden 6.800,12345'i 6.800,1235 yazıyordu).
        GirdiKapisi.Kur(dto.AltinHasFiyati, "altinHasFiyati", KurKolonOndalik);
        GirdiKapisi.Kur(dto.GramAltinFiyati, "gramAltinFiyati", KurKolonOndalik);
        GirdiKapisi.Kur(dto.UsdTry, "usdTry", KurKolonOndalik);
        GirdiKapisi.Kur(dto.EurTry, "eurTry", KurKolonOndalik);
        GirdiKapisi.Kur(dto.GbpTry, "gbpTry", KurKolonOndalik);

        // PAKET KUR-BANT — makul bant (Core FiyatBandi; ekran Kur.razor AYNI kural, AYNI cümle).
        // Referans: alan başına SON GEÇERLİ (iptal edilmemiş) kayıttaki değer; yoksa mutlak sınır.
        var asimlar = await BantAsimlari(dto, tenantId);
        var id = Kimlik.Ulid();
        // KUR-BANT tur 2 (doğrulayıcı ORTA): MUTLAK sınır dışı değer SERT kapıdır — onay/gerekçe/rol
        // ne olursa olsun 400 (SAHİP gerekçe yazınca has 5 TL kaydediliyordu). Onay yalnız ± %20'yi geçer.
        if (asimlar.FirstOrDefault(a => !FiyatBandi.OnaylanabilirMi(a.Hata)) is { } sert)
            GirdiKapisi.Denetle(sert.Hata, sert.Alan);
        if (asimlar.Count > 0)
        {
            if (dto.BantOnay != true)
                GirdiKapisi.Denetle(asimlar[0].Hata, asimlar[0].Alan);          // 400 + alan + sözlük metni
            if (islem is null || !BantOnayYetkili(islem.Rol))
                throw new ForbiddenException(KurBantSozlugu.Al(Kuyumcu.Core.I18n.IstekDili.Gecerli).YetkiYok);          // 403: onay yalnız SAHİP/MÜDÜR
            var gerekce = GirdiKapisi.Gerekce(dto.BantGerekce, "bantGerekce");    // K30: ≥ 20 karakter
            var ozet = string.Join(" · ", asimlar.Select(a =>
                string.Create(CultureInfo.InvariantCulture,
                    $"{a.Alan}={a.Deger} (son geçerli {(a.Onceki is { } o ? o.ToString(CultureInfo.InvariantCulture) : "yok")})")));
            denetim.YazmaKaydet(islem, "KurBantAsimi", id,
                $"elle kur MAKUL BANT DIŞI onayla kaydedildi · {ozet} · gerekçe: {gerekce}", hassas: true);
        }
        return await SnapshotKaydet(tenantId, dto.AltinHasFiyati, dto.GramAltinFiyati,
            dto.UsdTry, dto.EurTry, dto.GbpTry, "MANUAL", id);
    }

    /// <summary>Bant aşımı: alan, değer, referans ve hata (alan sırası ekrandaki sırayla aynı).</summary>
    public sealed record BantAsimi(string Alan, decimal Deger, decimal? Onceki, GirdiHatasi Hata);

    /// <summary>
    /// PAKET KUR-BANT — her elle girilen fiyatı son GEÇERLİ değerine göre bantlar. Referans alan başına
    /// ayrı bulunur (son kayıtta USD boşsa bir önceki dolu USD kullanılır); hiç yoksa mutlak sınır.
    /// </summary>
    public async Task<List<BantAsimi>> BantAsimlari(ManuelKurDto dto, string tenantId)
    {
        var sonlar = await db.KurSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenantId && !s.IptalEdildi)
            .OrderByDescending(s => s.Olusturuldu)
            .Take(50)
            .Select(s => new { s.AltinHasFiyati, s.GramAltinFiyati, s.UsdTry, s.EurTry, s.GbpTry })
            .ToListAsync();
        decimal? Ilk(Func<decimal?, decimal?, decimal?, decimal?, decimal?, decimal?> sec) =>
            sonlar.Select(s => sec(s.AltinHasFiyati, s.GramAltinFiyati, s.UsdTry, s.EurTry, s.GbpTry))
                  .FirstOrDefault(v => v is > 0m);

        var liste = new List<BantAsimi>();
        void Bak(string alan, decimal? deger, FiyatTuru tur, decimal? onceki)
        {
            if (deger is { } d && FiyatBandi.Denetle(d, tur, onceki) is { } h)
                liste.Add(new BantAsimi(alan, d, onceki, h));
        }
        Bak("altinHasFiyati", dto.AltinHasFiyati, FiyatTuru.Has, Ilk((h, _, _, _, _) => h));
        Bak("gramAltinFiyati", dto.GramAltinFiyati, FiyatTuru.GramAltin, Ilk((_, g, _, _, _) => g));
        Bak("usdTry", dto.UsdTry, FiyatTuru.Doviz, Ilk((_, _, u, _, _) => u));
        Bak("eurTry", dto.EurTry, FiyatTuru.Doviz, Ilk((_, _, _, e, _) => e));
        Bak("gbpTry", dto.GbpTry, FiyatTuru.Doviz, Ilk((_, _, _, _, g) => g));
        return liste;
    }

    /// <summary>PAKET GOC-A — kur_snapshots kolonları decimal(15,4): elle girişte en çok 4 ondalık (ekran KurAlani EnCokOndalik ile AYNI sayı).</summary>
    public const int KurKolonOndalik = 4;

    // ─── Manuel kur İPTALİ (PAKET S-3b) ──────────────────────────────────────
    //
    // KURUL TUR 3 / ENGEL 1 + QA turu 3 (/kur, YÜKSEK): "manuel kur kaydı
    // düzeltilemiyor/silinemiyor" — kur geçmişi satırlarında hiçbir eylem yoktu.
    // Yanlış girilen bir MANUAL kur, ona bağlanan işlemlerle birlikte kalıcıydı.
    //
    // DEFTER YERİNDE DEĞİŞMEZ: satır SİLİNMEZ ve fiyatı DEĞİŞTİRİLMEZ; yalnız
    // "geçersiz" damgası vurulur. Geçmişte rozetiyle durur, güncel kur
    // seçiminden ve yeni işlemlerden dışlanır.
    //
    // FAIL-CLOSED: kura bağlanmış EN AZ BİR hareket varsa iptal REDDEDİLİR.
    // Aksi hâlde geçmiş bir işlemin kuru, o işlem dururken "geçersiz" olur ve
    // has hesabı açıklanamaz hâle gelirdi. Düzeltme yolu önce o işlemlerin
    // stornosudur — mesaj bunu ve kaç işlem olduğunu SÖYLER (UX-S30).
    public async Task<KurSnapshotCevap> ManuelKurIptal(
        string id, string? gerekceHam, AktifKullanici islem)
    {
        var gerekce = Modules.Satis.StornoServisi.GerekceDogrula(gerekceHam);

        var snapshot = await db.KurSnapshots
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Kur kaydı bulunamadı");

        if (snapshot.Kaynak != KurKaynakDefteri.ManuelKod)
            throw new ConflictException(
                $"Yalnız ELLE girilen ({KurKaynakDefteri.ManuelKod}) kur kaydı iptal edilebilir; "
                + $"bu kayıt '{snapshot.Kaynak}' kaynağından gelmiştir. Dış kaynak kaydı düzeltilmez, "
                + "yenisi çekilir (POST /api/v1/kur/guncelle).");

        if (snapshot.IptalEdildi)
            throw new ConflictException("Bu kur kaydı zaten iptal edilmiştir.");

        var bagliIslem = await db.Hareketler
            .CountAsync(h => h.TenantId == islem.TenantId && h.KurSnapshotId == id);
        if (bagliIslem > 0)
        {
            await denetim.RedKaydet(islem, "KurIptali", id,
                $"manuel kur iptali REDDEDİLDİ (bağlı işlem: {bagliIslem}) · gerekçe: {gerekce}");
            throw new ConflictException(
                $"Bu kur kaydına bağlı {bagliIslem} işlem var; kur iptal edilemez. "
                + "Önce o işlemleri storno edin, sonra kuru iptal edin.");
        }

        snapshot.IptalEdildi = true;
        snapshot.IptalGerekce = gerekce;
        snapshot.IptalTarihi = DateTime.UtcNow;

        denetim.YazmaKaydet(islem, "KurIptali", id,
            $"manuel kur kaydı GEÇERSİZ kılındı · gerekçe: {gerekce}", hassas: true);

        await db.SaveChangesAsync();

        logger.LogInformation("[{TenantId}] MANUEL KUR İPTAL: {Id}", islem.TenantId, id);
        return SnapshotZenginlestir(snapshot);
    }

    // ─── Zamanlayıcı için iç metod ────────────────────────────────────────────

    /// <summary>Zamanlayıcının tek kez çektiği veriyi bu kiracıya işler.</summary>
    public async Task<KurSnapshotCevap?> OtomatikGuncelle(string tenantId, DisKurVerisi veri)
    {
        try
        {
            var (cevap, yazildi) = await KaydetVeyaMevcut(tenantId, veri);
            if (yazildi)
                logger.LogInformation("[{TenantId}] Kur güncellendi: {Fiyat} TL/has gram",
                    tenantId, cevap.AltinHasFiyati);
            else
                logger.LogDebug("[{TenantId}] Kur değişmedi, yeni kayıt yazılmadı", tenantId);
            return cevap;
        }
        catch (Exception ex)
        {
            logger.LogWarning("[{TenantId}] Otomatik kur güncellenemedi: {Mesaj}", tenantId, ex.Message);
            return null;
        }
    }

    // ─── Snapshot kayıt yardımcısı ────────────────────────────────────────────

    private async Task<KurSnapshotCevap> SnapshotKaydet(
        string tenantId, decimal altinHasFiyati, decimal? gramAltinFiyati,
        decimal? usdTry, decimal? eurTry, decimal? gbpTry, string kaynak, string? id = null)
    {
        var snapshot = new KurSnapshot
        {
            Id = id ?? Kimlik.Ulid(),
            TenantId = tenantId,
            AltinHasFiyati = Math.Round(altinHasFiyati, 4, MidpointRounding.AwayFromZero),
            GramAltinFiyati = gramAltinFiyati is null ? null : Math.Round(gramAltinFiyati.Value, 4, MidpointRounding.AwayFromZero),
            UsdTry = usdTry is null ? null : Math.Round(usdTry.Value, 4, MidpointRounding.AwayFromZero),
            EurTry = eurTry is null ? null : Math.Round(eurTry.Value, 4, MidpointRounding.AwayFromZero),
            GbpTry = gbpTry is null ? null : Math.Round(gbpTry.Value, 4, MidpointRounding.AwayFromZero),
            Kaynak = kaynak,
            Olusturuldu = DateTime.UtcNow,
        };
        db.KurSnapshots.Add(snapshot);
        await db.SaveChangesAsync();

        var cevap = SnapshotZenginlestir(snapshot);
        // Canlı push: yalnız bu tenant'ın bağlı istemcilerine (yoklama yerine)
        try { await kurHub.Clients.Group(tenantId).SendAsync("KurGuncellendi", cevap); }
        catch (Exception ex) { logger.LogWarning(ex, "Kur push gönderilemedi (bağlı istemci yok olabilir)"); }
        return cevap;
    }

    // ─── Snapshot → zengin yanıt (ayar fiyatları dahil) ──────────────────────

    private static KurSnapshotCevap SnapshotZenginlestir(KurSnapshot s)
    {
        var hasFiyati = s.AltinHasFiyati;

        var ayarFiyatlari = AltinAyarlari.Select(ayar => new AyarFiyati(
            ayar.Id,
            ayar.Label,
            ayar.Milyem,
            // Nest: toHasGram(1g, ayar) × hasFiyatı, 2 haneye yuvarlanır
            Math.Round(ayar.Milyem / 1000m * hasFiyati, 2, MidpointRounding.AwayFromZero),
            hasFiyati)).ToList();

        return new KurSnapshotCevap(
            s.Id, s.TenantId, hasFiyati, s.GramAltinFiyati, s.UsdTry, s.EurTry, s.GbpTry, s.Kaynak, s.Olusturuldu, ayarFiyatlari,
            s.IptalEdildi, s.IptalGerekce, s.IptalTarihi);
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/kur")]
// P-09 (Y-39): yetki matrisi RotaModulleri haritasından türetilir.
[Yetki.ModulYetkisi("kur")]
public sealed class KurController(KurServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/kur/guncel — En güncel kur + tüm ayarların gram fiyatları (POS ekranı için tüm rollere açık)</summary>
    [HttpGet("guncel")]
    public Task<KurSnapshotCevap> GuncelKur() => servis.GuncelKur(Islem.TenantId);

    /// <summary>GET /api/v1/kur/gecmis?baslangic=..&amp;bitis=..&amp;kaynak=..&amp;limit=.. — Kur geçmişi — MUHASEBE+</summary>
    [HttpGet("gecmis")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<List<KurSnapshotCevap>> KurGecmisi([FromQuery] KurFiltreDto filtre) =>
        servis.KurGecmisi(Islem.TenantId, filtre);

    /// <summary>POST /api/v1/kur/guncelle — Dış kaynaktan anında kur çek ve kaydet — MUDUR+ (200 OK)</summary>
    [HttpPost("guncelle")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<KurSnapshotCevap> DisKaynakKurCek() => servis.DisKaynakKurCek(Islem.TenantId);

    /// <summary>GET /api/v1/kur/kaynaklar — Seçilebilir kur kaynakları + kiracının seçimi</summary>
    [HttpGet("kaynaklar")]
    public Task<object> Kaynaklar() => servis.Kaynaklar(Islem.TenantId);

    /// <summary>GET /api/v1/kur/pariteler — Kaynaktaki sarrafiye alış/satış fiyatları</summary>
    [HttpGet("pariteler")]
    public Task<object> Pariteler() => servis.Pariteler(Islem.TenantId);

    /// <summary>PATCH /api/v1/kur/kaynak — Kur kaynağını değiştir — YALNIZ SAHİP</summary>
    /// <remarks>
    /// Mehmet K3 (UX-3): kaynak KİRACI GENELİ bir ayardır ve tüm fiyatlandırmayı
    /// etkiler. Ekranda gizlemek bir kapı değildir; uç rolle kapatılır.
    /// KESIF-P6 (ekip oyu Mehmet+Taşkın, 30.09 18:10): /kur ekranındaki ikinci seçici
    /// MÜDÜR'e kaynağı ONAYSIZ değiştirtiyordu. Kaynak yalnız SAHİP'in kararıdır ve
    /// tek yerde (İşletme, onay penceresi) değişir → uç [RolGerekli("SAHIP")].
    /// Manuel kur girişi MUDUR+ KALIR (günlük işletme işi; kaynak seçimi değil).
    /// </remarks>
    [HttpPatch("kaynak")]
    [RolGerekli("SAHIP")]
    public Task<object> KaynakAyarla([FromBody] KaynakSecDto dto) =>
        servis.KaynakAyarla(dto.Kaynak, Islem.TenantId);

    /// <summary>
    /// POST /api/v1/kur/manuel/{id}/iptal — PAKET S-3b.
    /// Yanlış girilen MANUEL kur kaydını gerekçeyle GEÇERSİZ kılar (silmez).
    /// </summary>
    [HttpPost("manuel/{id}/iptal")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<KurSnapshotCevap> ManuelKurIptal(string id, [FromBody] GerekceDto dto) =>
        servis.ManuelKurIptal(id, dto.Gerekce, Islem);

    /// <summary>POST /api/v1/kur/manuel — Manuel kur girişi (internet yoksa / özel fiyat) — MUDUR+ (201)</summary>
    [HttpPost("manuel")]
    [RolGerekli("SAHIP", "MUDUR")]
    public async Task<IActionResult> ManuelKurGir([FromBody] ManuelKurDto dto) =>
        StatusCode(StatusCodes.Status201Created, await servis.ManuelKurKaydet(dto, Islem));
}

// ── Zamanlayıcı ──────────────────────────────────────────────────────────────

/// <summary>
/// Kur güncelleme zamanlayıcısı.
///   • Varsayılan her 60 SANİYEDE bir dış kaynaktan kur çekilir
///     (Kur:GuncellemeSaniye ile ayarlanır; 15–3600 sn dışı değer YAPILANDIRMA
///     HATASIDIR — sessizce sıkıştırılmaz, uygulama açılışta anlaşılır hatayla durur).
///     PAKET P-73: 10 sn'de canlı DB'de kiracı başına saatte ~340 KurSnapshot
///     ölçüldü ("değişmedi → yazma" koruması turların çoğunda devreye girmiyor).
///   • Dış kaynağa tur başına TEK istek atılır; sonuç aktif tüm kiracılara
///     uygulanır — kiracı başına istek atmak ücretsiz kaynakta kısıtlanmaya yol açar.
///   • Fiyat değişmediyse yeni satır YAZILMAZ (bkz. KurServisi.KaydetVeyaMevcut).
///   • Saat penceresi Kur:BaslangicSaat / Kur:BitisSaat ile ayarlanır
///     (varsayılan 0–23: gün boyu; vitrin gece de güncel kalır).
///   • Her (İstanbul) günü bir kez 90 günden eski snapshot'lar temizlenir.
/// Dış kaynak/DB hatası uygulamayı düşürmez — loglanır ve sonraki tura geçilir.
/// </summary>
public sealed class KurZamanlayici(
    SistemKapsami kapsam, ILogger<KurZamanlayici> logger, IConfiguration yapilandirma)
    : BackgroundService
{
    private static readonly TimeZoneInfo Istanbul = IstanbulSaatDilimi();

    /// <summary>Kur:GuncellemeSaniye anahtarı yoksa kullanılan aralık (sn).</summary>
    public const int VarsayilanAralikSaniye = 60;
    /// <summary>Kabul edilen en küçük aralık (sn); altı yapılandırma hatasıdır.</summary>
    public const int AltSinirSaniye = 15;
    /// <summary>Kabul edilen en büyük aralık (sn); üstü yapılandırma hatasıdır.</summary>
    public const int UstSinirSaniye = 3600;
    public const string AralikAnahtari = "Kur:GuncellemeSaniye";

    /// <summary>
    /// PAKET P-73 — etkin güncelleme aralığını yapılandırmadan okur.
    /// Anahtar YOKSA <see cref="VarsayilanAralikSaniye"/>; VAR ama tamsayı
    /// değilse ya da [15, 3600] dışındaysa <see cref="InvalidOperationException"/>
    /// — sessiz sıkıştırma/varsayım YOK (Anayasa md.1). Kurucu içinde çağrıldığı
    /// için hatalı değer barındırılan hizmet başlarken uygulamayı durdurur.
    /// </summary>
    public static TimeSpan AraligiOku(IConfiguration yapilandirma)
    {
        var ham = yapilandirma[AralikAnahtari];
        if (ham is null)
            return TimeSpan.FromSeconds(VarsayilanAralikSaniye);

        if (!int.TryParse(ham.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var saniye))
            throw new InvalidOperationException(
                $"Yapılandırma hatası: '{AralikAnahtari}' tamsayı saniye olmalı; okunan değer '{ham}' "
                + $"ayrıştırılamadı. Geçerli aralık {AltSinirSaniye}–{UstSinirSaniye} sn "
                + $"(önerilen varsayılan {VarsayilanAralikSaniye}).");

        if (saniye < AltSinirSaniye || saniye > UstSinirSaniye)
            throw new InvalidOperationException(
                $"Yapılandırma hatası: '{AralikAnahtari}' = {saniye} sn kabul edilmez; "
                + $"geçerli aralık {AltSinirSaniye}–{UstSinirSaniye} sn "
                + $"(önerilen varsayılan {VarsayilanAralikSaniye}). Değer sessizce sıkıştırılmaz — "
                + "appsettings / ortam değişkenini düzeltip uygulamayı yeniden başlatın.");

        return TimeSpan.FromSeconds(saniye);
    }

    /// <summary>Etkin aralık (test ve açılış logu için okunabilir).</summary>
    public TimeSpan EtkinAralik => GuncellemeAraligi;

    private readonly TimeSpan GuncellemeAraligi = AraligiOku(yapilandirma);
    private readonly int baslangicSaat = Math.Clamp(yapilandirma.GetValue("Kur:BaslangicSaat", 0), 0, 23);
    private readonly int bitisSaat = Math.Clamp(yapilandirma.GetValue("Kur:BitisSaat", 23), 0, 23);

    private DateOnly sonTemizlikGunu = DateOnly.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Kur zamanlayıcı başladı (etkin aralık: {Saniye} sn, pencere: {Bas}-{Bit})",
            GuncellemeAraligi.TotalSeconds, baslangicSaat, bitisSaat);

        while (!stoppingToken.IsCancellationRequested)
        {
            var simdi = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Istanbul);
            try
            {
                // Günlük temizlik: İstanbul gününün ilk uyanışında bir kez çalışır
                if (DateOnly.FromDateTime(simdi) != sonTemizlikGunu)
                {
                    await EskiKurlariTemizle(stoppingToken);
                    sonTemizlikGunu = DateOnly.FromDateTime(simdi);
                }

                // Güncelleme penceresi (İstanbul saati). Varsayılan gün boyu.
                if (simdi.Hour >= baslangicSaat && simdi.Hour <= bitisSaat)
                {
                    await KurGuncelle(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kur zamanlayıcı tur hatası");
            }

            try { await Task.Delay(GuncellemeAraligi, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task KurGuncelle(CancellationToken iptal)
    {
        logger.LogDebug("Kur güncelleme görevi başladı");

        // ADIM1-KARAR §3/Ç1 madde 1: kiracı listesini okumak SİSTEM kapsamıdır.
        List<(string Id, string? Kaynak)> aktifTenantlar;
        KurKaynakDefteri defter;
        await using (var sistem = kapsam.Sistem())
        {
            var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
            defter = sistem.ServiceProvider.GetRequiredService<KurKaynakDefteri>();
            aktifTenantlar = (await db.Tenants.AsNoTracking()
                .Where(t => t.Aktif)
                .Select(t => new { t.Id, t.KurKaynagi })
                .ToListAsync(iptal))
                .Select(t => (t.Id, t.KurKaynagi))
                .ToList();
        }
        if (aktifTenantlar.Count == 0) return;

        // Kiracılar KAYNAĞA GÖRE gruplanır: her kaynağa tur başına TEK istek atılır.
        var gruplar = aktifTenantlar.GroupBy(t =>
            defter.Gecerli(t.Kaynak) ? t.Kaynak! : defter.VarsayilanKod);

        foreach (var grup in gruplar)
        {
            DisKurVerisi veri;
            try
            {
                await using var cekimKapsami = kapsam.Sistem();
                veri = await cekimKapsami.ServiceProvider
                    .GetRequiredService<KurServisi>().DisVeriCek(grup.Key);
            }
            catch (Exception ex)
            {
                logger.LogWarning("{Kaynak} kurları çekilemedi, bu tur atlanıyor: {Mesaj}",
                    grup.Key, ex.Message);
                continue;
            }

            // ADIM1-KARAR §3/Ç1 madde 2: kur YAZIMI sistem değil, KİRACI kapsamıdır.
            // Böylece yazma yapısal olarak doğru kiracıya çakılır — güvenlik kazancı.
            foreach (var t in grup)
            {
                await using var kiraciKapsami = kapsam.Kiraci(t.Id);
                await kiraciKapsami.ServiceProvider
                    .GetRequiredService<KurServisi>().OtomatikGuncelle(t.Id, veri);
            }
        }
    }

    private async Task EskiKurlariTemizle(CancellationToken iptal)
    {
        var sinir = DateTime.UtcNow.AddDays(-90);
        logger.LogInformation("90 günden eski kur kayıtları temizleniyor (öncesi: {Sinir:O})", sinir);

        // ADIM1-KARAR §3/Ç1 madde 3: kiracılar arası bakım → SİSTEM kapsamı.
        // ⚠ ExecuteDelete SaveChanges'ı atlar; yazma denetçisi bunu görmez.
        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
        // hareketler.kur_snapshot_id NOT NULL: bir harekete bağlı snapshot ASLA silinmez,
        // aksi halde 90 gün sonra her işlemin kur kanıtı yetim kalırdı.
        await db.KurSnapshots
            .Where(s => s.Olusturuldu < sinir
                     && !db.Hareketler.Any(h => h.KurSnapshotId == s.Id))
            .ExecuteDeleteAsync(iptal);

        logger.LogInformation("Eski kur kayıtları temizlendi");
    }

    private static TimeZoneInfo IstanbulSaatDilimi()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
        catch (Exception)
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
            catch (Exception)
            {
                return TimeZoneInfo.CreateCustomTimeZone(
                    "Europe/Istanbul", TimeSpan.FromHours(3), "Europe/Istanbul", "Europe/Istanbul");
            }
        }
    }
}
