using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Kuyumcu.Api.Modules.Banka.Baglayicilar;

/// <summary>
/// Garanti BBVA — EHÖ Account Transactions (REST/JSON). Kaynak: ARASTIRMA-BANKA-API-TEKNIK § 1
/// (bankanın OpenAPI tanımı + çalışan topluluk kodu). Token TEK KULLANIMLIK: her sayfadan önce yeni token.
/// Aralık en çok 30 gün, sayfa en çok 500. Banka benzersiz hareket no vermez → özet anahtar.
/// </summary>
public sealed class GarantiBaglayicisi : IBankaBaglayicisi
{
    public const string KodSabit = "GARANTI";
    public const string Taban = "https://apis.garantibbva.com.tr";
    private static readonly string Ad = Kuyumcu.Core.Domain.BankaBaglayiciKatalogu.Bul(KodSabit)!.BankaAdi;
    private const int SayfaBoyu = 500;
    public string Kod => KodSabit;

    public async Task<IReadOnlyList<SaglayiciHareketi>> Cek(HttpClient http, BankaCekimIstegi istek, CancellationToken iptal)
    {
        var k = istek.Kimlik;
        var sonuc = new List<SaglayiciHareketi>();
        foreach (var (bas, bit) in Tarih.Dilimle(Tarih.Yerel(istek.Baslangic), Tarih.Yerel(istek.Bitis), 30))
        {
            for (var sayfa = 1; sayfa <= 200; sayfa++)
            {
                var token = await Token(http, k, iptal);
                using var m = new HttpRequestMessage(HttpMethod.Post, Taban + "/balancesandmovements/accountinformation/transaction/v1/gettransactions")
                {
                    Content = new StringContent(IstekGovdesi(Giris.Zorunlu(k, "consentId"), istek.Iban, bas, bit, sayfa), Encoding.UTF8, "application/json"),
                };
                m.Headers.Authorization = new("Bearer", token);
                m.Headers.Accept.ParseAdd("application/json");
                var (hareketler, ham) = Ayristir(await Http.Gonder(http, m, Ad, iptal), istek.Iban);
                sonuc.AddRange(hareketler);
                if (ham < SayfaBoyu) break;   // toplam sayfa bilgisi yok: eksik sayfa = son sayfa
            }
        }
        return sonuc;
    }

    private static async Task<string> Token(HttpClient http, IReadOnlyDictionary<string, string?> k, CancellationToken iptal)
    {
        var alanlar = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials", ["scope"] = "oob",
            ["client_id"] = Giris.Zorunlu(k, "clientId"), ["client_secret"] = Giris.Zorunlu(k, "clientSecret"),
        };
        if (Giris.Istege(k, "geriDonusAdresi") is { } r) alanlar["redirect_uri"] = r;
        using var m = new HttpRequestMessage(HttpMethod.Post, Taban + "/auth/oauth/v2/token") { Content = new FormUrlEncodedContent(alanlar) };
        var metin = await Http.Gonder(http, m, Ad, iptal);
        try
        {
            using var d = JsonDocument.Parse(metin);
            return d.RootElement.TryGetProperty("access_token", out var t) && t.GetString() is { Length: > 0 } s ? s
                : throw new BankaBaglantiHatasi($"{Ad} oturum anahtarı vermedi.");
        }
        catch (JsonException ex) { throw new BankaBaglantiHatasi($"{Ad} oturum yanıtı okunamadı.", ex); }
    }

    public static string IstekGovdesi(string consentId, string iban, DateTime yerelBas, DateTime yerelBit, int sayfa) =>
        JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["consentId"] = consentId, ["IBAN"] = iban,
            ["startDate"] = yerelBas.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture),
            ["endDate"] = yerelBit.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture),
            ["transactionId"] = "", ["pageIndex"] = sayfa, ["pageSize"] = SayfaBoyu,
        });

    /// <summary>(hareketler, bankanın döndüğü ham satır sayısı — sayfalama için).</summary>
    public static (List<SaglayiciHareketi> Hareketler, int Ham) Ayristir(string json, string iban)
    {
        JsonDocument d;
        try { d = JsonDocument.Parse(json); }
        catch (JsonException ex) { throw new BankaBaglantiHatasi($"{Ad} yanıtı okunamadı.", ex); }
        using (d)
        {
            var kok = d.RootElement;
            if (kok.TryGetProperty("result", out var r) && r.TryGetProperty("returnCode", out var rc)
                && Metin(rc) is { } kodMetni && kodMetni != "200")
                throw new BankaBaglantiHatasi($"{Ad}: {Metin(r, "messageText") ?? "istek reddedildi"} (kod {Metin(r, "reasonCode")}).");
            var liste = new List<SaglayiciHareketi>();
            if (!kok.TryGetProperty("transactions", out var t) || t.ValueKind != JsonValueKind.Array) return (liste, 0);
            foreach (var h in t.EnumerateArray())
            {
                var yon = Metin(h, "txnCreditDebitIndicator");
                var tarih = Tarih.Oku(Metin(h, "transactionInstanceId"), "yyyy-MM-ddTHH:mm:ss.FFFFFF")
                            ?? Tarih.Oku(Metin(h, "activityDate"), "yyyy-MM-dd");
                var tutar = Sayi.Oku(Metin(h, "amount"));
                if (tarih is null || tutar is null || yon is not ("A" or "B")) continue;
                string? karsiAd = null, karsiIban = null, detay = null;
                if (h.TryGetProperty("enrichmentInformation", out var zen) && zen.ValueKind == JsonValueKind.Array)
                    foreach (var z in zen.EnumerateArray())
                    {
                        if (!z.TryGetProperty("enrichmentValue", out var v) || v.ValueKind != JsonValueKind.Object) continue;
                        switch (Metin(z, "enrichmentCode"))
                        {
                            case "MUS": karsiAd = Metin(v, "corrNameSurnameText"); break;
                            case "CORRACNT": karsiIban = Metin(v, "corrIBAN"); break;
                            case "DEK": detay = Metin(v, "detailText"); break;
                        }
                    }
                var aciklama = string.Join(" ", new[] { Metin(h, "explanation"), detay }.Where(x => !string.IsNullOrWhiteSpace(x) && x != "00000"));
                var temiz = HareketTemizleyici.Temizle(KodSabit, null, tarih.Value, yon == "A", tutar.Value,
                    karsiAd, karsiIban, Metin(h, "corrVKN") ?? Metin(h, "corrTCKN"), aciklama,
                    yedekAnahtar: $"{Metin(h, "IBAN") ?? iban}|{Metin(h, "transactionReferenceId")}|{Metin(h, "transactionInstanceId")}");
                if (temiz is not null) liste.Add(temiz);
            }
            return (liste, t.GetArrayLength());
        }
    }

    private static string? Metin(JsonElement e, string ad) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(ad, out var v) ? Metin(v) : null;

    private static string? Metin(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.String => string.IsNullOrWhiteSpace(v.GetString()) ? null : v.GetString()!.Trim(),
        JsonValueKind.Number => v.GetRawText(),
        _ => null,
    };
}
