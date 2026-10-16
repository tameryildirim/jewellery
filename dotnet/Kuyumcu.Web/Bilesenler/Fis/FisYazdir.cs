using System.Text.Json;
using Kuyumcu.Contracts.Cikti;

namespace Kuyumcu.Web.Bilesenler.Fis;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-5 — KAYDET → YAZDIR (PLAN § S-2 madde 2.7) + PARA ÜSTÜ (madde 2.8, D7)
//
// SAF KURALLAR (istek atmaz, sayfa yoktur): sunucu yanıtından yazdırılacak
// HAREKET kimliğini okur, /cikti/{Tur}/{Id} adresini kurar, ham ULID'i
// ekrandan ayıklar ve nakit para üstünü hesaplar.
//
// ⚠ NEDEN HAREKET KİMLİĞİ: /api/v1/cikti/SATIS_FISI/{id} bir HAREKET kimliği
//   bekler (CiktiModulu.HareketBelgesi: x.Id == id) ve o hareketin fis_id'si
//   varsa fişin BÜTÜN bacaklarını basar. Çok bacaklı fiş ucu (satis-fisi)
//   yalnız fisId döndürür → sayfa fişi /islemler/fis/{fisId} ile okuyup ilk
//   kalemin kimliğini alır (IlkKalem). Uydurulmaz: kimlik yoksa bağlantı yok.
// ⚠ BelgeliTurler, sunucunun SATIS_FISI için kabul ettiği hareket türleriyle
//   AYNI kümedir (CiktiModulu.BeklenenTurler). Hurda alış / takas / işçilik
//   bu belgeyi üretmez → bağlantı çizilmez (sunucu 400 verirdi).
// ═════════════════════════════════════════════════════════════════════════════

public static class FisYazdir
{
    /// <summary>SATIS_FISI belgesini üreten hareket türleri (sunucuyla aynı küme).</summary>
    public static readonly string[] BelgeliTurler = ["SATIS", "SATIS_IADE", "ALIS", "ALIS_IADE"];

    public static bool Belgeli(string? tur) => tur is not null && Array.IndexOf(BelgeliTurler, tur) >= 0;

    /// <summary>Yazdırma sayfasının adresi; kimlik yoksa null (bağlantı çizilmez).</summary>
    public static string? Adres(string? hareketId) =>
        string.IsNullOrWhiteSpace(hareketId) ? null
        : $"/cikti/{BelgeTurleri.SatisFisi}/{Uri.EscapeDataString(hareketId)}";

    /// <summary>PAKET S2-5b: Cikti.razor bu sorguyla açılınca sayfa çizildikten sonra BİR KEZ yazdırır.</summary>
    public const string OtomatikYazdirSorgusu = "yazdir=1";

    /// <summary>PAKET S2-5b: ekrandaki "Fiş yazdır" bağlantısı — okut → Kaydet → Yazdır = 3 dokunuş.</summary>
    public static string? YazdirAdresi(string? hareketId) =>
        Adres(hareketId) is { } a ? a + "?" + OtomatikYazdirSorgusu : null;

    /// <summary>
    /// T-S1 (doğrulayıcı bulgusu, 24.09): kayıt TEK yazdırılabilir belge mi doğurur?
    /// İade tek harekettir; fiş ucu tek fiştir; tek kalemli kayıt tek harekettir.
    /// Kalem başına POST edilen çok kalemli kayıt (bugün yalnız ALIS) N ayrı belgedir.
    /// PAKET L-1: çok kalemli SATIŞ artık fiş ucuna gider (<see cref="FisUcuMu"/>).
    /// </summary>
    public static bool TekBelge(bool iade, int kalemSayisi, bool fisUcu) => iade || fisUcu || kalemSayisi <= 1;

    /// <summary>
    /// PAKET L-1 (tevsik açığı, KRİTİK): SATIŞ hangi uca gider? Karma/hediye/iskonto
    /// (<paramref name="ozelFis"/>) ya da ≥2 kalem → TEK POST <c>/islemler/satis-fisi</c>.
    /// ⚠ NEDEN: kalem başına ayrı POST, fişi tevsik haddinin (459/575 s. VUK GT, eşik
    /// SUNUCUDA) altına bölüyordu; Peşin Müşteri'de kümülatif yoktur, kapı yalnız TEK
    /// isteğin toplamını görür. Eşik burada YOK — karar yalnız kalem sayısına bakar.
    /// </summary>
    public static bool FisUcuMu(int kalemSayisi, bool ozelFis) => ozelFis || kalemSayisi > 1;

    /// <summary>
    /// Tek kalemli uç (satis/alis) hareketin KENDİSİNİ döndürür (id + tur);
    /// iade uçları <c>hareketId</c> döndürür. Başka şekil → null.
    /// </summary>
    public static string? HareketId(JsonElement y)
    {
        if (y.ValueKind != JsonValueKind.Object) return null;
        if (y.TryGetProperty("hareketId", out var h) && h.ValueKind == JsonValueKind.String)
            return h.GetString();
        return y.TryGetProperty("id", out var i) && i.ValueKind == JsonValueKind.String
               && y.TryGetProperty("tur", out var t) && t.ValueKind == JsonValueKind.String
               && Belgeli(t.GetString())
            ? i.GetString() : null;
    }

    /// <summary>/islemler/fis/{fisId} yanıtında belge üreten İLK kalemin kimliği.</summary>
    public static string? IlkKalem(JsonElement fis)
    {
        if (fis.ValueKind != JsonValueKind.Object
            || !fis.TryGetProperty("kalemler", out var k) || k.ValueKind != JsonValueKind.Array) return null;
        foreach (var s in k.EnumerateArray())
            if (s.ValueKind == JsonValueKind.Object
                && s.TryGetProperty("id", out var i) && i.ValueKind == JsonValueKind.String
                && s.TryGetProperty("tur", out var t) && t.ValueKind == JsonValueKind.String && Belgeli(t.GetString()))
                return i.GetString();
        return null;
    }

    private const string Crockford = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    /// <summary>26 karakterlik Crockford base32 = ULID (sistem kimliği).</summary>
    public static bool UlidMi(string? s) =>
        s is { Length: 26 } && s.All(c => Crockford.Contains(char.ToUpperInvariant(c)));

    /// <summary>
    /// Banda yazılabilir fiş numarası: ham ULID GÖSTERİLMEZ (QA tur 7 bulgusu,
    /// PLAN 2.7). Sunucu okunur bir numara döndürürse o yazılır.
    /// </summary>
    public static string? OkunurNo(string? no) =>
        string.IsNullOrWhiteSpace(no) || UlidMi(no) ? null : no;

    /// <summary>
    /// Para üstü = alınan − nakit (kuruşa yuvarlanır). Alınan yazılmadıysa null.
    /// Negatif sonuç EKSİK demektir. İstemci hesabıdır; gövdeye KONMAZ (D7).
    /// </summary>
    public static decimal? ParaUstu(decimal? alinan, decimal nakit) =>
        alinan is { } a ? Math.Round(a - nakit, 2, MidpointRounding.AwayFromZero) : null;
}
