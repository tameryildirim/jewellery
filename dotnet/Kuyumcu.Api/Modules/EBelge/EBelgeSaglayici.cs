using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Kuyumcu.Api.Modules.EBelge;

// ═════════════════════════════════════════════════════════════════════════════
// E-1 — entegratörden BAĞIMSIZ e-belge arayüzü. Fatura → EBelgeModeli (EBelgeKurucu) → entegratör.
// İlk bağlayıcı Nilvera (REST, belgesi herkese açık: developer.nilvera.com — uçlar ve alan adları
// 20.10'da OpenAPI tanımından birebir alındı). Yeni entegratör = bu arayüzün yeni uygulaması.
// ⚠ Entegratörün test ortamında henüz DENENMEDİ (test anahtarı kurucunun hesabından gelecek).
// ═════════════════════════════════════════════════════════════════════════════

public static class EBelgeDurumlari
{
    /// <summary>Gönderilecek (ilk kez ya da geçici hatadan sonra yeniden).</summary>
    public const string Kuyrukta = "KUYRUKTA";
    /// <summary>Entegratöre ulaştı; GİB sonucu bekleniyor.</summary>
    public const string Gonderildi = "GONDERILDI";
    public const string Kabul = "KABUL";
    /// <summary>Kalıcı hata (belge eksik/yanlış) — düzeltilip "yeniden dene" ile kuyruğa döner.</summary>
    public const string Hata = "HATA";
    /// <summary>e-Fatura alıcısı reddetti.</summary>
    public const string Ret = "RET";
    public const string Iptal = "IPTAL";
}

public static class EBelgeSenaryolari { public const string EArsiv = "EARSIV", EFatura = "EFATURA"; }

public sealed record EBelgeTaraf(string VknTckn, string Ad, string? VergiDairesi, string? Adres, string? Il, string? Ilce, string? Eposta, string? Telefon);

/// <param name="BirimFiyat">KDV HARİÇ birim fiyat.</param>
/// <param name="NetTutar">KDV hariç satır tutarı (miktar × birim fiyat, yuvarlanmış).</param>
/// <param name="OzelMatrahTutar">Özel matrahlı satırda KDV'nin hesaplandığı matrah; değilse null.</param>
/// <param name="Aciklama">Kuyum ayrıntısı (ayar · gram · has · işçilik) — görüntüde ürün adının altında.</param>
public sealed record EBelgeSatiri(string Ad, decimal Miktar, string Birim, decimal BirimFiyat, decimal NetTutar,
    decimal KdvYuzde, decimal KdvTutar, decimal? OzelMatrahTutar, string? Aciklama = null);

/// <param name="Tip">SATIS | OZELMATRAH.</param>
/// <param name="Sablon">Görüntü şablonu (XSLT metni); null = entegratörün varsayılanı.</param>
public sealed record EBelgeModeli(Guid Ettn, string Senaryo, string Tip, DateTime TarihYerel, string ParaBirimi, string? SeriOnEki,
    EBelgeTaraf Satici, EBelgeTaraf Alici, string? AliciEtiketi, IReadOnlyList<EBelgeSatiri> Satirlar, string? OzelMatrahKodu,
    IReadOnlyList<string> Notlar, string? Sablon = null)
{
    public decimal NetToplam => Satirlar.Sum(s => s.NetTutar);
    public decimal KdvToplam => Satirlar.Sum(s => s.KdvTutar);
    public decimal Odenecek => NetToplam + KdvToplam;
}

/// <summary>Alıcı e-Fatura'ya kayıtlı mı; kayıtlıysa posta kutusu etiketi (alias).</summary>
public sealed record MukellefSonucu(bool Kayitli, string? Etiket);
public sealed record EBelgeDurumSonucu(string Durum, string? Aciklama);

/// <summary>Sade dille kullanıcıya söylenecek e-belge hatası. <see cref="Gecici"/> = yeniden denenecek (ağ, 5xx, 429).</summary>
public sealed class EBelgeHatasi(string sade, bool gecici, Exception? ic = null) : Exception(sade, ic)
{
    public bool Gecici { get; } = gecici;
}

public interface IEBelgeSaglayici
{
    string Kod { get; }
    Task<MukellefSonucu> MukellefSorgula(HttpClient http, EBelgeBaglami b, string vkn, CancellationToken iptal);
    /// <summary>Belgeyi gönderir; entegratörün verdiği belge numarası döner. Aynı ETTN ikinci kez gelirse çakışma = zaten gönderildi.</summary>
    Task<string?> Gonder(HttpClient http, EBelgeBaglami b, EBelgeModeli belge, CancellationToken iptal);
    Task<EBelgeDurumSonucu> Durum(HttpClient http, EBelgeBaglami b, string senaryo, Guid ettn, CancellationToken iptal);
    Task<byte[]> Pdf(HttpClient http, EBelgeBaglami b, string senaryo, Guid ettn, CancellationToken iptal);
    /// <summary>İmzalı UBL-TR XML (yasal asıl) — e-Fatura: /einvoice/Sale/{uuid}/xml · e-Arşiv: /earchive/Invoices/{uuid}/xml.</summary>
    Task<byte[]> Xml(HttpClient http, EBelgeBaglami b, string senaryo, Guid ettn, CancellationToken iptal);
    Task Iptal(HttpClient http, EBelgeBaglami b, string senaryo, Guid ettn, CancellationToken iptal);
}

/// <summary>Çağrı bağlamı: çözülmüş anahtar ve ortam (TEST | CANLI).</summary>
public sealed record EBelgeBaglami(string Anahtar, string Ortam);

public static class EBelgeSaglayicilari
{
    public const string Nilvera = "NILVERA";
    public static readonly IReadOnlyList<(string Kod, string Ad)> Tumu = [(Nilvera, "Nilvera")];
    public static IEBelgeSaglayici? Bul(string? kod) => kod == Nilvera ? new NilveraSaglayici() : null;
}

/// <summary>Nilvera — developer.nilvera.com (OpenAPI 3; uçlar: /earchive, /einvoice, /general).</summary>
public sealed class NilveraSaglayici : IEBelgeSaglayici
{
    public const string TestTaban = "https://apitest.nilvera.com", CanliTaban = "https://api.nilvera.com";
    private const string Ad = "Nilvera";
    public string Kod => EBelgeSaglayicilari.Nilvera;

    private static string Taban(EBelgeBaglami b) => b.Ortam == "CANLI" ? CanliTaban : TestTaban;

    private static HttpRequestMessage Istek(HttpMethod m, EBelgeBaglami b, string yol, HttpContent? govde = null)
    {
        var r = new HttpRequestMessage(m, Taban(b) + yol) { Content = govde };
        r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", b.Anahtar);
        return r;
    }

    private static async Task<(HttpStatusCode Durum, byte[] Govde)> Gonder(HttpClient http, HttpRequestMessage r, CancellationToken iptal)
    {
        try
        {
            using var y = await http.SendAsync(r, iptal);
            return (y.StatusCode, await y.Content.ReadAsByteArrayAsync(iptal));
        }
        catch (HttpRequestException ex) { throw new EBelgeHatasi($"{Ad} servisine ulaşılamadı.", gecici: true, ex); }
        catch (TaskCanceledException ex) when (!iptal.IsCancellationRequested) { throw new EBelgeHatasi($"{Ad} zamanında yanıt vermedi.", gecici: true, ex); }
    }

    /// <summary>Başarısız yanıtı sade hataya çevirir. 400/422 gövdesindeki doğrulama iletisi (ör. "Alıcı VKN hatalı") aynen gösterilir.</summary>
    public static EBelgeHatasi Hata(HttpStatusCode d, byte[] govde) => d switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new($"{Ad} API anahtarını kabul etmedi (yetkisi yok ya da süresi dolmuş).", false),
        HttpStatusCode.TooManyRequests => new($"{Ad} çok sık istek aldı; biraz sonra yeniden denenecek.", true),
        _ when (int)d >= 500 => new($"{Ad} şu an yanıt vermiyor; biraz sonra yeniden denenecek.", true),
        _ => new($"{Ad}: {Ileti(govde) ?? $"istek reddedildi (kod {(int)d})"}", false),
    };

    /// <summary>Hata gövdesinden okunur ileti: dizi ise ilk öğeler, nesne ise Message/Description/title alanı. En çok 300 karakter.</summary>
    public static string? Ileti(byte[] govde)
    {
        var metin = Encoding.UTF8.GetString(govde).Trim();
        if (metin.Length == 0) return null;
        string? s = null;
        try
        {
            var n = JsonNode.Parse(metin);
            if (n is JsonArray a)
                s = string.Join(" · ", a.Select(x => x is JsonObject o ? (string?)(o["Description"] ?? o["Message"] ?? o["Code"]) : x?.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).Take(3));
            else if (n is JsonObject o)
                s = (string?)(o["Message"] ?? o["message"] ?? o["Description"] ?? o["detail"] ?? o["title"]);
            else s = n?.ToString();
        }
        catch (JsonException) { s = metin.StartsWith('<') ? null : metin; }
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        return s.Length > 300 ? s[..300] : s;
    }

    public async Task<MukellefSonucu> MukellefSorgula(HttpClient http, EBelgeBaglami b, string vkn, CancellationToken iptal)
    {
        var (d, g) = await Gonder(http, Istek(HttpMethod.Get, b, $"/general/GlobalCompany/Check/TaxNumber/{Uri.EscapeDataString(vkn)}?globalUserType=Invoice"), iptal);
        if (d == HttpStatusCode.NotFound) return new(false, null);
        if (!((int)d is >= 200 and < 300)) throw Hata(d, g);
        return MukellefAyristir(g);
    }

    /// <summary>
    /// Yanıt: GlobalCompanyInfoResponse[] (TaxNumber, Title, Name, Type, DocumentType…). Boş dizi = kayıtlı değil.
    /// Etiket (alias) olarak "urn:" ile başlayan Name, yoksa ilk Name (alan anlamı test ortamında doğrulanacak — TEYİTSİZ).
    /// </summary>
    public static MukellefSonucu MukellefAyristir(byte[] govde)
    {
        try
        {
            if (JsonNode.Parse(govde) is not JsonArray a || a.Count == 0) return new(false, null);
            var adlar = a.OfType<JsonObject>().Select(o => (string?)o["Name"]).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            return new(true, adlar.FirstOrDefault(x => x!.StartsWith("urn:", StringComparison.OrdinalIgnoreCase)) ?? adlar.FirstOrDefault());
        }
        catch (JsonException ex) { throw new EBelgeHatasi($"{Ad} mükellef yanıtı okunamadı.", false, ex); }
    }

    public async Task<string?> Gonder(HttpClient http, EBelgeBaglami b, EBelgeModeli belge, CancellationToken iptal)
    {
        var (yol, govde) = belge.Senaryo == EBelgeSenaryolari.EFatura
            ? ("/einvoice/Send/Model", EFaturaGovdesi(belge)) : ("/earchive/Send/Model", EArsivGovdesi(belge));
        var (d, g) = await Gonder(http, Istek(HttpMethod.Post, b, yol, new StringContent(govde, Encoding.UTF8, "application/json")), iptal);
        if (d == HttpStatusCode.Conflict) return null;   // aynı ETTN zaten var → gönderilmiş sayılır, numara durum/sorgudan gelir
        if (!((int)d is >= 200 and < 300)) throw Hata(d, g);
        try { return JsonNode.Parse(g) is JsonObject o ? (string?)o["InvoiceNumber"] : null; }
        catch (JsonException) { return null; }
    }

    public async Task<EBelgeDurumSonucu> Durum(HttpClient http, EBelgeBaglami b, string senaryo, Guid ettn, CancellationToken iptal)
    {
        var yol = senaryo == EBelgeSenaryolari.EFatura ? $"/einvoice/Sale/{ettn}/Status" : $"/earchive/Invoices/{ettn}/Status";
        var (d, g) = await Gonder(http, Istek(HttpMethod.Get, b, yol), iptal);
        if (!((int)d is >= 200 and < 300)) throw Hata(d, g);
        return DurumAyristir(senaryo, g);
    }

    /// <summary>
    /// e-Arşiv: StatusResponse{StatusCode(unknown|waiting|succeed|error), StatusDetail, ReportStatus, CancelStatus}.
    /// e-Fatura: StatusResponse{InvoiceStatus{Code, Description, DetailDescription}, Answer{AnswerCode(… approved|rejected …), AnswerNote}}.
    /// </summary>
    public static EBelgeDurumSonucu DurumAyristir(string senaryo, byte[] govde)
    {
        JsonObject o;
        try { o = JsonNode.Parse(govde) as JsonObject ?? throw new EBelgeHatasi($"{Ad} durum yanıtı okunamadı.", true); }
        catch (JsonException ex) { throw new EBelgeHatasi($"{Ad} durum yanıtı okunamadı.", true, ex); }
        if (senaryo == EBelgeSenaryolari.EArsiv)
        {
            if (o["CancelStatus"]?.GetValueKind() == JsonValueKind.True) return new(EBelgeDurumlari.Iptal, null);
            return Esle((string?)o["StatusCode"], (string?)o["StatusDetail"]);
        }
        var cevap = o["Answer"] as JsonObject;
        if ((string?)cevap?["AnswerCode"] == "rejected")
            return new(EBelgeDurumlari.Ret, (string?)cevap?["AnswerNote"] ?? (string?)cevap?["Description"]);
        var fd = o["InvoiceStatus"] as JsonObject;
        return Esle((string?)fd?["Code"], (string?)fd?["DetailDescription"] ?? (string?)fd?["Description"]);

        static EBelgeDurumSonucu Esle(string? kod, string? aciklama) => kod switch
        {
            "succeed" => new(EBelgeDurumlari.Kabul, null),
            "error" => new(EBelgeDurumlari.Hata, string.IsNullOrWhiteSpace(aciklama) ? "GİB belgeyi reddetti." : aciklama.Trim()),
            _ => new(EBelgeDurumlari.Gonderildi, null),
        };
    }

    public async Task<byte[]> Pdf(HttpClient http, EBelgeBaglami b, string senaryo, Guid ettn, CancellationToken iptal)
    {
        var yol = senaryo == EBelgeSenaryolari.EFatura ? $"/einvoice/Sale/{ettn}/pdf" : $"/earchive/Invoices/{ettn}/pdf";
        var (d, g) = await Gonder(http, Istek(HttpMethod.Get, b, yol), iptal);
        if (!((int)d is >= 200 and < 300)) throw Hata(d, g);
        // Gövde PDF baytı ya da base64 metni olabilir (OpenAPI içerik türü yazmıyor — TEYİTSİZ) → ikisi de kabul.
        if (g.Length > 4 && g[0] == '%' && g[1] == 'P' && g[2] == 'D' && g[3] == 'F') return g;
        var metin = Encoding.UTF8.GetString(g).Trim().Trim('"');
        try { return Convert.FromBase64String(metin); }
        catch (FormatException ex) { throw new EBelgeHatasi($"{Ad} PDF'i okunamadı.", false, ex); }
    }

    public async Task<byte[]> Xml(HttpClient http, EBelgeBaglami b, string senaryo, Guid ettn, CancellationToken iptal)
    {
        var yol = senaryo == EBelgeSenaryolari.EFatura ? $"/einvoice/Sale/{ettn}/xml" : $"/earchive/Invoices/{ettn}/xml";
        var (d, g) = await Gonder(http, Istek(HttpMethod.Get, b, yol), iptal);
        if (!((int)d is >= 200 and < 300)) throw Hata(d, g);
        // Gövde XML ya da base64 metin olabilir (içerik türü OpenAPI'de yazmıyor — TEYİTSİZ) → ikisi de kabul.
        var metin = Encoding.UTF8.GetString(g).TrimStart('\uFEFF', ' ', '\r', '\n', '\t');
        if (metin.StartsWith('<')) return g;
        try { return Convert.FromBase64String(metin.Trim().Trim('"')); }
        catch (FormatException ex) { throw new EBelgeHatasi($"{Ad} XML'i okunamadı.", false, ex); }
    }

    public async Task Iptal(HttpClient http, EBelgeBaglami b, string senaryo, Guid ettn, CancellationToken iptal)
    {
        if (senaryo != EBelgeSenaryolari.EArsiv)
            throw new EBelgeHatasi("e-Fatura buradan iptal edilemez: alıcının reddi ya da GİB iptal portalı gerekir; ya da iade faturası kesin.", false);
        var (d, g) = await Gonder(http, Istek(HttpMethod.Put, b, $"/earchive/Invoices/{ettn}/Cancel"), iptal);
        if (!((int)d is >= 200 and < 300)) throw Hata(d, g);
    }

    // ── Gövdeler (alan adları OpenAPI şemasından BİREBİR) ──────────────────

    private static string T(DateTime yerel) => yerel.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    private static double D(decimal x) => (double)x;

    private static JsonObject Taraf(EBelgeTaraf t) => new()
    {
        ["TaxNumber"] = t.VknTckn, ["Name"] = t.Ad, ["TaxOffice"] = t.VergiDairesi, ["Address"] = t.Adres,
        ["District"] = t.Ilce, ["City"] = t.Il, ["Country"] = "Türkiye", ["Mail"] = t.Eposta, ["Phone"] = t.Telefon,
    };

    private static JsonArray Satirlar(EBelgeModeli b) => new(b.Satirlar.Select((s, i) =>
    {
        var o = new JsonObject
        {
            ["Index"] = (i + 1).ToString(CultureInfo.InvariantCulture), ["Name"] = s.Ad, ["Quantity"] = D(s.Miktar),
            ["UnitType"] = s.Birim, ["Price"] = D(s.BirimFiyat), ["AllowanceTotal"] = 0d, ["Description"] = s.Aciklama,
            ["KDVPercent"] = D(s.KdvYuzde), ["KDVTotal"] = D(s.KdvTutar),
        };
        if (s.OzelMatrahTutar is { } om) { o["OzelMatrahReason"] = b.OzelMatrahKodu; o["OzelMatrahTotal"] = D(om); }
        return (JsonNode)o;
    }).ToArray());

    private static JsonObject BilgiOrtak(EBelgeModeli b)
    {
        var o = new JsonObject
        {
            ["UUID"] = b.Ettn.ToString(), ["InvoiceType"] = b.Tip, ["IssueDate"] = T(b.TarihYerel),
            ["CurrencyCode"] = b.ParaBirimi, ["LineExtensionAmount"] = D(b.NetToplam), ["GeneralAllowanceTotal"] = 0d,
            ["KdvTotal"] = D(b.KdvToplam), ["PayableAmount"] = D(b.Odenecek),
        };
        if (!string.IsNullOrWhiteSpace(b.SeriOnEki)) o["InvoiceSerieOrNumber"] = b.SeriOnEki;
        // ByGatech görüntü şablonu (XSLT, base64) — alan OpenAPI'de var; entegratörün yorumu test ortamında doğrulanacak.
        if (!string.IsNullOrWhiteSpace(b.Sablon)) o["TemplateBase64String"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(b.Sablon));
        // Genel KDV toplamları oran kovalarına (şemada 1/8/10/18/20 alanları var).
        foreach (var g in b.Satirlar.GroupBy(s => s.KdvYuzde))
        {
            var alan = g.Key switch { 1m => "GeneralKDV1Total", 8m => "GeneralKDV8Total", 10m => "GeneralKDV10Total", 18m => "GeneralKDV18Total", 20m => "GeneralKDV20Total", _ => null };
            if (alan is not null) o[alan] = D(g.Sum(s => s.KdvTutar));
        }
        return o;
    }

    public static string EArsivGovdesi(EBelgeModeli b)
    {
        var bilgi = BilgiOrtak(b);
        bilgi["SalesPlatform"] = "NORMAL";
        bilgi["SendType"] = string.IsNullOrWhiteSpace(b.Alici.Eposta) ? "KAGIT" : "ELEKTRONIK";
        return new JsonObject
        {
            ["ArchiveInvoice"] = new JsonObject
            {
                ["InvoiceInfo"] = bilgi, ["CompanyInfo"] = Taraf(b.Satici), ["CustomerInfo"] = Taraf(b.Alici),
                ["InvoiceLines"] = Satirlar(b), ["Notes"] = new JsonArray(b.Notlar.Select(n => (JsonNode)JsonValue.Create(n)!).ToArray()),
            },
        }.ToJsonString();
    }

    public static string EFaturaGovdesi(EBelgeModeli b)
    {
        var bilgi = BilgiOrtak(b);
        bilgi["InvoiceProfile"] = "TEMELFATURA";
        return new JsonObject
        {
            ["EInvoice"] = new JsonObject
            {
                ["InvoiceInfo"] = bilgi, ["CompanyInfo"] = Taraf(b.Satici), ["CustomerInfo"] = Taraf(b.Alici),
                ["InvoiceLines"] = Satirlar(b), ["Notes"] = new JsonArray(b.Notlar.Select(n => (JsonNode)JsonValue.Create(n)!).ToArray()),
            },
            ["CustomerAlias"] = b.AliciEtiketi,
        }.ToJsonString();
    }
}
