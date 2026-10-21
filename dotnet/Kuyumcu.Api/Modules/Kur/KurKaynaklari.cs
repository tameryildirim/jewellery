using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Kuyumcu.Api.Modules.Kur;

// ═════════════════════════════════════════════════════════════════════════════
// KUR KAYNAKLARI PAKETİ (kurucu kararları 17.10 — KARAR-MERKEZI-MIMARI § 9 md.1–3, 6)
//
//  1) HAREM altın adaptörü — belgesiz Socket.IO v4 ucu, SÖZLEŞMESİZ kapının arkasında
//     (Kur:SozlesmesizKaynaklarAcik). Kurucu 17.10: "şu an ticari kullanımda değiliz,
//     satıştan sonra izin başvurusu yapılacak." Hukuki risk: KUR-KAYNAKLARI-ARASTIRMASI § 6.
//  2) DÖVİZ kaynağı altın kaynağından AYRI seçilir: TCMB EVDS (anahtarlı, kaynak
//     gösterilerek ticari kullanım serbest — EVDS Kullanım Şartları), TCMB today.xml,
//     ya da bir altın kaynağının kendi döviz kuru.
//  3) Has bedeli (KDVK 23/e) kapanışı: kaynak adaptörü günün kapanışını bildirir
//     (Altınkaynak "IAB_KAPANIS", Harem gün sonu son gözlem) → ozel_matrah_kapanislari.
//
// GERÇEK YÜK DOĞRULAMASI (17.10.2026 17:34, tek bağlantı, ilk mesajdan sonra kapatıldı):
//   0{"sid":…,"upgrades":[],"pingInterval":25000,"pingTimeout":20000}  →  "40" gönderildi
//   40{"sid":…}
//   42["price_changed",{"meta":{"time":1792233585986},"data":{"ALTIN":{"code":"ALTIN",
//      "alis":"6601.950","satis":"6624.010","tarih":"17-10-2026 13:39:44","dir":{…},
//      "dusuk":"6615.040","yuksek":"6624.010","kapanis":6542.58}, "CEYREK_YENI":{"code":
//      "CEYREK_YENI","alis":10761,"satis":10829,…,"kapanis":10696}, …}}]
//   ÖLÇÜLEN BİÇİM: alis/satis BAZI kodlarda DİZGE ("6601.950", nokta ondalık, binlik
//   ayracı YOK), bazılarında SAYI (10761); kapanis her zaman SAYI; tarih "dd-MM-yyyy
//   HH:mm:ss" (İstanbul yerel); yayın dışı kodlarda alis/satis 0 ve tarih "".
//   İlk price_changed TÜM kodları taşıdı (ölçüldü). Sonraki mesajların yalnız değişeni
//   taşıyıp taşımadığı D (doğrulanamadı) — adaptör yalnız İLK mesajı okur.
//   "kapanis" alanının HANGİ GÜNÜN kapanışı olduğu yükte yazmıyor (D): has bedeli için
//   resmî dayanak SAYILMAZ, yalnız dayanak metnine çapraz kontrol olarak yazılır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir kaynağın bildirdiği gün kapanışı (TL / has gram).</summary>
/// <param name="Gun">Kapanışın ait olduğu işletme günü.</param>
/// <param name="Fiyat">TL / has gram (1000/1000).</param>
/// <param name="Dayanak">Değerin nereden geldiği (belgeye/denetime yazılır).</param>
public sealed record KaynakKapanisi(DateOnly Gun, decimal Fiyat, string Dayanak);

/// <summary>Döviz kaynağından gelen kur (satış; değeri olmayan alan null).</summary>
public sealed record DovizVerisi(decimal? UsdTry, decimal? EurTry, decimal? GbpTry, string Kaynak, DateOnly? Gun = null,
    DovizAlislari? Alislar = null);   // 21.10: alış kurları (TCMB ForexBuying · EVDS alış serisi)

/// <summary>Altın kaynağından AYRI seçilen döviz kaynağı adaptörü.</summary>
public interface IDovizAdaptoru
{
    /// <summary>Kaynak kodu (tenants.doviz_kaynagi ve kur_snapshots.doviz_kaynak ile aynı değer).</summary>
    string Kod { get; }

    Task<DovizVerisi> DovizCek(CancellationToken iptal = default);
}

/// <summary>Döviz ve has bedeli kaynak kodları (tek yer — ekran ve sunucu aynı kodları okur).</summary>
public static class KurKaynakKodlari
{
    // Altın kaynakları (KurKaynakDefteri adaptörleri)
    public const string Altinkaynak = "ALTINKAYNAK";
    public const string Ankara = "ANKARA";
    public const string Truncgil = "TRUNCGIL";
    public const string Harem = "HAREM";

    // Döviz kaynakları
    /// <summary>Döviz, seçili altın kaynağının kendi döviz kurundan (bugünkü davranış).</summary>
    public const string DovizAltinKaynagi = "ALTIN_KAYNAGI";
    /// <summary>TCMB EVDS web servisi (anahtar: Kur:EvdsAnahtar).</summary>
    public const string Evds = "EVDS";
    /// <summary>TCMB günlük kur dosyası (kurlar/today.xml) — ticari kullanım yazılı izne tabi (araştırma § 1/5).</summary>
    public const string Tcmb = "TCMB";

    // Has bedeli (özel matrah) kaynakları
    public const string Bist = "BIST";
    public const string IslemKuru = "ISLEM_KURU";
    public const string Elle = "ELLE";
    /// <summary>Faturada has bedeli fiyatla değil müşterinin "binde" yöntemiyle hesaplandı (M2b).</summary>
    public const string Binde = "BINDE";

    /// <summary>Kurucu 17.10: has bedeli kaynağının varsayılanı Borsa İstanbul (YENİ işletme bununla doğar).</summary>
    public const string OzelMatrahVarsayilan = Bist;

    /// <summary>Seçilebilir has bedeli kaynakları (ekran sırası).</summary>
    public static readonly IReadOnlyList<string> OzelMatrahTumu = [Bist, Harem, Altinkaynak, IslemKuru, Elle];

    /// <summary>Kapanışı ELLE girilen has bedeli kaynakları (BIST bülteni ya da kuyumcunun kendi değeri).</summary>
    public static readonly IReadOnlyList<string> ElleKapanisli = [Bist, Elle];

    /// <summary>Kapanışı adaptörün kendiliğinden bildirdiği kaynaklar (sözleşmesiz kapının arkasında).</summary>
    public static readonly IReadOnlyList<string> OtomatikKapanisli = [Harem, Altinkaynak];

    /// <summary>
    /// Has bedeli için kapanış en çok kaç gün geriye aranır. Borsa İstanbul hafta sonu ve resmî
    /// tatilde kapalıdır; en uzun bayram + hafta sonu boşluğu 9 gündür → 10 gün. Daha eski
    /// kapanış "son işgünü" sayılmaz: fatura kesilmez (fail-closed).
    /// </summary>
    public const int KapanisGeriGun = 10;
}

/// <summary>Sayı okuma (Harem): "6601.950" dizge ya da 10761 sayı → decimal; ≤ 0 ya da okunamazsa null.</summary>
internal static class HaremSayi
{
    public static decimal? Oku(JsonElement oge, string alan) => AdaptorYardim.Sayi(oge, alan, trBicim: false);
}

/// <summary>
/// Harem yükünün SAF ayrıştırıcısı (ağ görmez — gerçek yükten alınmış örnekle test edilir).
/// Girdi: Engine.IO "42[…]" çerçevesinin JSON kısmı (["price_changed",{meta,data}]).
/// </summary>
public static class HaremAyristirici
{
    public const string Kod = KurKaynakKodlari.Harem;

    /// <summary>Harem kodu → kanonik parite kodu (PariteKodlari). Platin/paladyumun kanonik kodu yok.</summary>
    public static readonly (string KaynakKodu, string KanonikKod)[] Esleme =
    [
        ("ALTIN", PariteKodlari.Has),
        ("KULCEALTIN", PariteKodlari.Kulce),
        ("CEYREK_YENI", PariteKodlari.CeyrekYeni),
        ("CEYREK_ESKI", PariteKodlari.CeyrekEski),
        ("YARIM_YENI", PariteKodlari.YarimYeni),
        ("YARIM_ESKI", PariteKodlari.YarimEski),
        ("TEK_YENI", PariteKodlari.TamYeni),
        ("TEK_ESKI", PariteKodlari.TamEski),
        ("ATA_YENI", PariteKodlari.AtaYeni),
        ("ATA_ESKI", PariteKodlari.AtaEski),
        ("ATA5_YENI", PariteKodlari.Besli),
        ("GREMESE_YENI", PariteKodlari.GremseYeni),
        ("GREMESE_ESKI", PariteKodlari.GremseEski),
        // Harem "AYAR22" = 22 ayar gram fiyatı (müşteri görüşmesinde "Harem 22 ayar bilezik" olarak anılan değer).
        ("AYAR22", PariteKodlari.Bilezik22),
        ("AYAR14", PariteKodlari.Ayar14),
        ("GUMUSTRY", PariteKodlari.Gumus),
        ("ONS", PariteKodlari.Ons),
    ];

    /// <summary>
    /// "42[...]" çerçevesinin JSON gövdesi → kur verisi. Olay adı price_changed değilse ya da
    /// has (ALTIN satış) okunamazsa <see cref="InvalidOperationException"/> (sessiz sıfır yok).
    /// </summary>
    public static DisKurVerisi Ayristir(string olayJson)
    {
        using var doc = JsonDocument.Parse(olayJson);
        var kok = doc.RootElement;
        if (kok.ValueKind != JsonValueKind.Array || kok.GetArrayLength() < 2
            || kok[0].GetString() != "price_changed")
            throw new InvalidOperationException("Harem: beklenen price_changed olayı gelmedi");
        if (!kok[1].TryGetProperty("data", out var veri) || veri.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Harem: price_changed yükünde data alanı yok");

        var ham = new Dictionary<string, (decimal? Alis, decimal? Satis)>(StringComparer.OrdinalIgnoreCase);
        foreach (var kalem in veri.EnumerateObject())
        {
            if (kalem.Value.ValueKind != JsonValueKind.Object) continue;
            ham[kalem.Name] = (HaremSayi.Oku(kalem.Value, "alis"), HaremSayi.Oku(kalem.Value, "satis"));
        }

        decimal? S(string k) => ham.TryGetValue(k, out var f) ? f.Satis : null;
        var has = S("ALTIN") ?? throw new InvalidOperationException("Harem: has altın (ALTIN) satış fiyatı bulunamadı");

        decimal? A(string k) => ham.TryGetValue(k, out var f) ? f.Alis : null;
        return new DisKurVerisi(has, null, S("USDTRY"), S("EURTRY"), S("GBPTRY"), Kod,
            AdaptorYardim.PariteKur(ham, Esleme), Kapanis(veri, has),
            Alislar: new DovizAlislari(A("USDTRY"), A("EURTRY"), A("GBPTRY")));
    }

    /// <summary>
    /// Has bedeli için Harem gün kapanışı = Harem'in o gün (tarih alanının günü) yayımladığı
    /// SON has (ALTIN) satış fiyatı. Zamanlayıcı her çekimde günün satırını günceller; gün
    /// bitince satır o günün son gözlemi olarak kalır. Harem'in "kapanis" alanı yalnız dayanak
    /// metnine yazılır (hangi güne ait olduğu yükte yok — D).
    /// </summary>
    private static KaynakKapanisi? Kapanis(JsonElement veri, decimal has)
    {
        if (!veri.TryGetProperty("ALTIN", out var altin)) return null;
        var tarihHam = altin.TryGetProperty("tarih", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
        if (!DateTime.TryParseExact(tarihHam, "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var yerel))
            return null;
        var kapanisAlani = HaremSayi.Oku(altin, "kapanis");
        var dayanak = FormattableString.Invariant($"Harem ALTIN satış {has} · yayın {yerel:dd.MM.yyyy HH:mm}")
            + (kapanisAlani is { } k ? FormattableString.Invariant($" · Harem kapanış alanı {k}") : "");
        return new KaynakKapanisi(DateOnly.FromDateTime(yerel), has, dayanak);
    }
}

/// <summary>
/// Harem Altın adaptörü — belgesiz Socket.IO v4 akışı (yalnız websocket taşıması).
/// En küçük Engine.IO v4 el sıkışması: "0{…}" açılış → "40" gönder → "40{…}" → ilk
/// "42[\"price_changed\",{…}]" okunur, bağlantı kapatılır. "2" ping'ine "3" yanıtı.
/// Çekim modeli mevcut IKurAdaptoru ile aynı: tur başına tek kısa bağlantı, 8 sn zaman aşımı;
/// hata → KurServisi 503 + elle giriş yolu (diğer adaptörlerle aynı).
/// </summary>
public sealed class HaremAdaptoru(ILogger logger) : IKurAdaptoru
{
    public string Kod => KurKaynakKodlari.Harem;

    private static readonly Uri Uc = new("wss://hrmsocketonly.haremaltin.com/socket.io/?EIO=4&transport=websocket");

    public async Task<DisKurVerisi> KurCek(CancellationToken iptal = default)
    {
        using var zamanAsimi = CancellationTokenSource.CreateLinkedTokenSource(iptal);
        zamanAsimi.CancelAfter(TimeSpan.FromSeconds(8));
        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(Uc, zamanAsimi.Token);
        try
        {
            var tampon = new byte[64 * 1024];
            for (var i = 0; i < 20; i++)   // el sıkışma + birkaç ping'den sonra vazgeç (sonsuz döngü yok)
            {
                var cerceve = await Oku(ws, tampon, zamanAsimi.Token);
                if (cerceve is null) break;
                switch (HaremCerceve.Tur(cerceve))
                {
                    case HaremCerceveTuru.Acilis:
                        await Gonder(ws, "40", zamanAsimi.Token);
                        break;
                    case HaremCerceveTuru.Ping:
                        await Gonder(ws, "3", zamanAsimi.Token);
                        break;
                    case HaremCerceveTuru.Olay:
                        return HaremAyristirici.Ayristir(cerceve[2..]);
                }
            }
            throw new InvalidOperationException("Harem: fiyat olayı gelmeden bağlantı kapandı");
        }
        finally
        {
            try
            {
                if (ws.State == WebSocketState.Open)
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            }
            catch (Exception ex) { logger.LogDebug("Harem bağlantısı kapatılırken: {Mesaj}", ex.Message); }
        }
    }

    private static Task Gonder(ClientWebSocket ws, string metin, CancellationToken iptal) =>
        ws.SendAsync(Encoding.UTF8.GetBytes(metin), WebSocketMessageType.Text, true, iptal);

    private static async Task<string?> Oku(ClientWebSocket ws, byte[] tampon, CancellationToken iptal)
    {
        using var ms = new MemoryStream();
        while (true)
        {
            var s = await ws.ReceiveAsync(tampon, iptal);
            if (s.MessageType == WebSocketMessageType.Close) return null;
            ms.Write(tampon, 0, s.Count);
            if (ms.Length > 4 * 1024 * 1024) throw new InvalidOperationException("Harem: çerçeve çok büyük");
            if (s.EndOfMessage) return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}

public enum HaremCerceveTuru { Bilinmeyen, Acilis, Baglandi, Ping, Olay }

/// <summary>Engine.IO v4 / Socket.IO çerçeve türü (saf, test edilir).</summary>
public static class HaremCerceve
{
    public static HaremCerceveTuru Tur(string cerceve) =>
        cerceve.StartsWith("42", StringComparison.Ordinal) ? HaremCerceveTuru.Olay
        : cerceve.StartsWith("40", StringComparison.Ordinal) ? HaremCerceveTuru.Baglandi
        : cerceve == "2" ? HaremCerceveTuru.Ping
        : cerceve.StartsWith('0') ? HaremCerceveTuru.Acilis
        : HaremCerceveTuru.Bilinmeyen;
}

// ── Zamanlayıcının çekim planı (saf) ─────────────────────────────────────────

/// <summary>Bir kiracının ÇÖZÜLMÜŞ kaynakları (geçersiz/boş seçim varsayılana düşürülmüş).</summary>
public sealed record KiraciKurAyari(string Id, string Altin, string Doviz, string OzelMatrah);

/// <summary>
/// Tur başına KAYNAK BAŞINA TEK çekim planı: hangi altın adaptörleri ve hangi günlük döviz
/// kaynakları bu tur çekilecek (kiracı sayısından bağımsız). Saf — test edilir.
/// </summary>
public sealed record KurCekimPlani(IReadOnlyList<string> AltinKodlari, IReadOnlyList<string> GunlukDovizKodlari)
{
    public static KurCekimPlani Kur(IEnumerable<KiraciKurAyari> kiracilar, KurKaynakDefteri defter)
    {
        var altin = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var doviz = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in kiracilar)
        {
            if (defter.Gecerli(t.Altin)) altin.Add(t.Altin);
            if (t.Doviz == KurKaynakKodlari.DovizAltinKaynagi || t.Doviz == t.Altin) { }
            else if (defter.DovizAltinAdaptorunden(t.Doviz)) altin.Add(t.Doviz);
            else if (defter.DovizGecerli(t.Doviz)) doviz.Add(t.Doviz);
            if (defter.KapanisAdaptoruVar(t.OzelMatrah)) altin.Add(t.OzelMatrah);
        }
        return new KurCekimPlani([.. altin], [.. doviz]);
    }
}

// ── TCMB EVDS ────────────────────────────────────────────────────────────────

/// <summary>
/// TCMB EVDS döviz ayrıştırıcısı (saf). Seri adlarındaki nokta yanıtta alt çizgiye döner
/// (TP.DK.USD.S → TP_DK_USD_S), satırlar "items" dizisinde, gün "Tarih" (dd-MM-yyyy).
/// ⚠ DOĞRULAMA DURUMU (17.10.2026): uç ve anahtar başlığı CANLI doğrulandı (anahtarsız istek
/// → 403 "Required request header 'key' is not present"). Yanıt gövdesinin biçimi anahtar
/// olmadan görülemedi: "items" + alt çizgili seri adı EVDS2 belgesinden ve üçüncü taraf
/// istemci kodundan alındı (D). Değer dizge ya da sayı olabilir; tatil günü null gelir.
/// </summary>
public static class EvdsAyristirici
{
    public const string UsdSatis = "TP.DK.USD.S";
    public const string EurSatis = "TP.DK.EUR.S";
    public const string GbpSatis = "TP.DK.GBP.S";
    // 21.10: döviz alış serileri (TCMB döviz alış kuru) — satış serilerinin ARKASINA eklenir.
    public const string UsdAlis = "TP.DK.USD.A";
    public const string EurAlis = "TP.DK.EUR.A";
    public const string GbpAlis = "TP.DK.GBP.A";

    public static string Alan(string seri) => seri.Replace('.', '_');

    public static DovizVerisi Ayristir(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("EVDS: yanıtta items dizisi yok");

        decimal? usd = null, eur = null, gbp = null;
        DovizAlislari? alis = null;
        DateOnly? gun = null;
        // Satırlar tarih sırasıyla gelir; SONDAN geriye ilk dolu değer alınır (tatil günü boş).
        for (var i = items.GetArrayLength() - 1; i >= 0; i--)
        {
            var s = items[i];
            var u = AdaptorYardim.Sayi(s, Alan(UsdSatis), false);
            var e = AdaptorYardim.Sayi(s, Alan(EurSatis), false);
            var g = AdaptorYardim.Sayi(s, Alan(GbpSatis), false);
            if (u is null && e is null && g is null) continue;
            usd = u; eur = e; gbp = g;
            alis = new DovizAlislari(AdaptorYardim.Sayi(s, Alan(UsdAlis), false), AdaptorYardim.Sayi(s, Alan(EurAlis), false),
                                     AdaptorYardim.Sayi(s, Alan(GbpAlis), false));
            if (s.TryGetProperty("Tarih", out var t) && t.ValueKind == JsonValueKind.String
                && DateOnly.TryParseExact(t.GetString(), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                gun = d;
            break;
        }
        if (usd is null && eur is null && gbp is null)
            throw new InvalidOperationException("EVDS: döviz satış kuru bulunamadı");
        return new DovizVerisi(usd, eur, gbp, KurKaynakKodlari.Evds, gun, alis);
    }

    /// <summary>İstek adresi (anahtar adreste DEĞİL, "key" başlığındadır — adres loglanabilir).</summary>
    public static string Adres(DateOnly bugun) =>
        "https://evds3.tcmb.gov.tr/igmevdsms-dis/series=" + string.Join("-", UsdSatis, EurSatis, GbpSatis, UsdAlis, EurAlis, GbpAlis)
        + $"&startDate={bugun.AddDays(-10):dd-MM-yyyy}&endDate={bugun:dd-MM-yyyy}&type=json";
}

/// <summary>
/// TCMB EVDS döviz adaptörü. Anahtar yapılandırmadan (<c>Kur:EvdsAnahtar</c> / ortam
/// <c>Kur__EvdsAnahtar</c>) gelir ve YALNIZ "key" başlığında gönderilir; değer loglanmaz,
/// yanıta/ekrana basılmaz. EVDS günlük veridir: 15 dakikalık önbellek (kiracı sayısı
/// kadar istek atılmaz); geçici hatada 24 saatten taze önbellek kullanılır.
/// </summary>
public sealed class EvdsDovizAdaptoru(IHttpClientFactory httpFactory, string anahtar) : IDovizAdaptoru
{
    public string Kod => KurKaynakKodlari.Evds;

    public async Task<DovizVerisi> DovizCek(CancellationToken iptal = default)
    {
        using var zamanAsimi = CancellationTokenSource.CreateLinkedTokenSource(iptal);
        zamanAsimi.CancelAfter(TimeSpan.FromSeconds(10));
        var bugun = DateOnly.FromDateTime(Common.Zaman.UtcdenYerele(DateTime.UtcNow));
        using var istek = new HttpRequestMessage(HttpMethod.Get, EvdsAyristirici.Adres(bugun));
        istek.Headers.Accept.ParseAdd("application/json");
        istek.Headers.TryAddWithoutValidation("key", anahtar);
        using var cevap = await httpFactory.CreateClient().SendAsync(istek, zamanAsimi.Token);
        if (!cevap.IsSuccessStatusCode)
            throw new HttpRequestException($"EVDS HTTP {(int)cevap.StatusCode}");
        return EvdsAyristirici.Ayristir(await cevap.Content.ReadAsStringAsync(zamanAsimi.Token));
    }
}

/// <summary>TCMB kurlar/today.xml ayrıştırıcısı (saf): ForexSelling (satış) · ForexBuying (alış, 21.10).</summary>
public static class TcmbAyristirici
{
    public static DovizVerisi Ayristir(string xml)
    {
        var doc = System.Xml.Linq.XDocument.Parse(xml);
        decimal? Kur(string kod, string alan = "ForexSelling") => AdaptorYardim.Metin((string?)doc.Root?.Elements("Currency")
            .FirstOrDefault(e => (string?)e.Attribute("Kod") == kod)?.Element(alan), false);
        DateOnly? gun = DateOnly.TryParseExact((string?)doc.Root?.Attribute("Date"), "MM/dd/yyyy",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
        var v = new DovizVerisi(Kur("USD"), Kur("EUR"), Kur("GBP"), KurKaynakKodlari.Tcmb, gun,
            new DovizAlislari(Kur("USD", "ForexBuying"), Kur("EUR", "ForexBuying"), Kur("GBP", "ForexBuying")));
        if (v.UsdTry is null && v.EurTry is null && v.GbpTry is null)
            throw new InvalidOperationException("TCMB: döviz satış kuru bulunamadı");
        return v;
    }
}

/// <summary>TCMB today.xml döviz adaptörü (sözleşmesiz kapının arkasında — ticari kullanım yazılı izne tabi).</summary>
public sealed class TcmbDovizAdaptoru(IHttpClientFactory httpFactory) : IDovizAdaptoru
{
    public string Kod => KurKaynakKodlari.Tcmb;

    public async Task<DovizVerisi> DovizCek(CancellationToken iptal = default)
    {
        using var zamanAsimi = CancellationTokenSource.CreateLinkedTokenSource(iptal);
        zamanAsimi.CancelAfter(TimeSpan.FromSeconds(8));
        using var cevap = await httpFactory.CreateClient().GetAsync("https://www.tcmb.gov.tr/kurlar/today.xml", zamanAsimi.Token);
        if (!cevap.IsSuccessStatusCode)
            throw new HttpRequestException($"TCMB HTTP {(int)cevap.StatusCode}");
        return TcmbAyristirici.Ayristir(await cevap.Content.ReadAsStringAsync(zamanAsimi.Token));
    }
}

/// <summary>
/// Günlük döviz kaynağının önbelleği: 15 dk içinde tekrar çekilmez; çekim başarısızsa
/// 24 saatten taze son değer döner (kaynak günde bir yayımlar — dakikalık yoklama gereksiz).
/// </summary>
public sealed class OnbellekliDovizAdaptoru(IDovizAdaptoru ic, TimeProvider? saat = null) : IDovizAdaptoru
{
    private readonly TimeProvider zaman = saat ?? TimeProvider.System;
    private readonly SemaphoreSlim kilit = new(1, 1);
    private DovizVerisi? son;
    private DateTimeOffset sonBasari = DateTimeOffset.MinValue;

    public static readonly TimeSpan Tazelik = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan HataToleransi = TimeSpan.FromHours(24);

    public string Kod => ic.Kod;

    /// <summary>Son BAŞARILI gerçek çekimin anı (UTC); hiç yoksa null.</summary>
    public DateTime? SonBasari => son is null ? null : sonBasari.UtcDateTime;

    public async Task<DovizVerisi> DovizCek(CancellationToken iptal = default)
    {
        await kilit.WaitAsync(iptal);
        try
        {
            var simdi = zaman.GetUtcNow();
            if (son is not null && simdi - sonBasari < Tazelik) return son;
            try
            {
                son = await ic.DovizCek(iptal);
                sonBasari = simdi;
                return son;
            }
            catch when (son is not null && simdi - sonBasari < HataToleransi)
            {
                return son;
            }
        }
        finally { kilit.Release(); }
    }
}
