using System.Text.Json;
using Kuyumcu.Api.Common;

namespace Kuyumcu.Api.Modules;

/// <summary>
/// PAKET PATCH-SOZLESME tur 3 — KOLON KAPASİTESİ KAPISI.
/// MySQL katı kipte decimal(p,s) kolonuna sığmayan değer "Out of range" hatası verir;
/// bu hata yakalanmazsa istemciye 500 döner (hasım bulgusu: kampanya PATCH
/// <c>{"deger":100000000000000}</c> → 500). Tavanlar İŞ EŞİĞİ DEĞİLDİR — kolon tanımından
/// türetilir (p−s tam hane, s ondalık); uydurma sınır konmaz.
/// Karşılaştırma kolonun ondalık hanesine YUVARLANMIŞ değerle yapılır: MySQL fazla haneyi
/// yarımda sıfırdan uzağa yuvarlar, 9.999.999.999.999,995 → 10.000.000.000.000,00 taşar.
/// </summary>
public static class KolonTavani
{
    /// <summary>decimal(12,2)</summary>
    public const decimal D12_2 = 9_999_999_999.99m;
    /// <summary>decimal(12,3)</summary>
    public const decimal D12_3 = 999_999_999.999m;
    /// <summary>decimal(12,4)</summary>
    public const decimal D12_4 = 99_999_999.9999m;
    /// <summary>decimal(14,2)</summary>
    public const decimal D14_2 = 999_999_999_999.99m;
    /// <summary>decimal(15,2)</summary>
    public const decimal D15_2 = 9_999_999_999_999.99m;
    /// <summary>decimal(15,6)</summary>
    public const decimal D15_6 = 999_999_999.999999m;

    // ── PAKET PATCH-3 — DTO Range üst sınırları (öznitelik sabiti double ister) ──
    // Range(…, double.MaxValue) decimal aralığının (±7,9·10²⁸) tamamını kabul ediyordu:
    // adet × fiyat çarpımı OverflowException → 500 verebiliyordu. Üst sınır kolon
    // kapasitesidir (iş eşiği DEĞİL); hesaplanan toplam kolona sığmazsa son savunma
    // hattı (Program.SayiTasmasiCevir: MySQL 1264 → 400) devreye girer.
    /// <summary>decimal(12,3) — adet/gram kolonları üst sınırı (Range için).</summary>
    public const double UstAdet = 999_999_999.999;
    /// <summary>decimal(15,2) — TL tutar kolonları üst sınırı (Range için).</summary>
    public const double UstTutar = 9_999_999_999_999.99;
    /// <summary>decimal(15,6) — has gram kolonları üst sınırı (Range için).</summary>
    public const double UstHas = 999_999_999.999999;

    /// <summary>
    /// PAKET PATCH-3 — TEK 6 HANE (has gram) YUVARLAMA KURALI: yarımda sıfırdan uzağa
    /// (AwayFromZero). Fatura <c>OzelMatrahMotoru.Yuvarla6</c> ile birebir aynı sonuç;
    /// yeni kod 6 haneye bununla yuvarlar (ToEven ile karışık kullanım PATCH-SOZLESME
    /// tur 3 bulgusuydu: 0,916667 × 1,5 → 1,375000 / 1,375001).
    /// </summary>
    public static decimal Yuvarla6(decimal d) => Math.Round(d, 6, MidpointRounding.AwayFromZero);

    /// <summary>3 hane (adet/miktar, decimal(12,3)) yuvarlama — "önce yuvarla, sonra ≤ 0" kalıbı.</summary>
    public static decimal Yuvarla3(decimal d) => Math.Round(d, 3, MidpointRounding.AwayFromZero);

    /// <summary>2 hane (TL/döviz tutar, decimal(15,2)) yuvarlama — yarımda sıfırdan uzağa.</summary>
    public static decimal Yuvarla2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// PAKET PATCH-5 — PARA TUTARI GİRİŞ KAPISI ("önce yuvarla, sonra ≤ 0" + kolon tavanı).
    /// <para>
    /// Kolon decimal(15,2). Yuvarlanmamış tutar deftere yazılırsa MySQL fazla haneyi
    /// SESSİZCE yuvarlar; 0,004 gibi bir tutar "sıfırdan büyük" kapısından geçip satıra
    /// 0,00 olarak düşer (sıfır tutarlı kayıt). Ayrıca bakiye ile hareket farklı
    /// yuvarlanabilir (eksi bakiyede yarım kuruş ters yöne). Bu yüzden tutar ÖNCE 2 haneye
    /// yuvarlanır, ≤ 0 kontrolü yuvarlanmış değere yapılır ve yazılan değer budur.
    /// </para>
    /// </summary>
    /// <returns>2 haneye yuvarlanmış, (0, D15_2] aralığındaki tutar.</returns>
    public static decimal PozitifTutar(decimal deger, string alan, string ad)
    {
        var y = Yuvarla2(Denetle(deger, 2, D15_2, alan, ad));
        if (y <= 0m)
            throw new BadRequestException(EnAzTutarMesaji(ad)).AlanIle(alan);
        return y;
    }

    /// <summary>
    /// PAKET SON-KALANLAR (hasım DÜŞÜK, B1440__07): "≤ 0" kuralının TEK iletisi. 0,004 yazan kullanıcıya
    /// "sıfırdan büyük olmalıdır" demek yazdığıyla çelişiyordu (yazdığı sayı sıfırdan büyük); en küçük
    /// kaydedilebilir değer söylenir. DTO <c>[Range(0.01, …)]</c> iletileri de bu cümleyi kullanır.
    /// </summary>
    public static string EnAzTutarMesaji(string ad) => $"{ad} en az 0,01 olmalıdır — daha küçük tutar kaydedilemez";

    /// <summary>DTO özniteliği için sabit (öznitelik derleme zamanı sabiti ister): <see cref="EnAzTutarMesaji"/>("Tutar").</summary>
    public const string EnAzTutar = "Tutar en az 0,01 olmalıdır — daha küçük tutar kaydedilemez";

    /// <summary>
    /// PAKET PATCH-5 — işaretli (±) tutar: 2 haneye yuvarlanır, kolon tavanı sınanır;
    /// sıfır kontrolünü çağıran yapar (yuvarlanmış değerle).
    /// </summary>
    public static decimal IsaretliTutar(decimal deger, string alan, string ad) =>
        Yuvarla2(Denetle(deger, 2, D15_2, alan, ad));

    /// <summary>
    /// PAKET PATCH-5 — işaretli (±) has gram: 6 haneye (AwayFromZero, tek 6 hane kuralı)
    /// yuvarlanır, decimal(15,6) tavanı sınanır.
    /// </summary>
    public static decimal IsaretliHas(decimal deger, string alan, string ad) =>
        Yuvarla6(Denetle(deger, 6, D15_6, alan, ad));

    /// <summary>
    /// PAKET PATCH-5 — işlem sonrası BAKİYE kolona (decimal(15,2)) sığmalı; sığmazsa 400
    /// alan:<paramref name="alan"/> (kullanıcının gönderdiği tutar alanı), satır yazılmaz.
    /// Son savunma hattı (MySQL 1264 ağı) alanı KOLONDAN türetir (bakiye); burada
    /// kullanıcının düzelteceği alan söylenir.
    /// </summary>
    public static void BakiyeSigar(decimal yeniBakiye, string alan, string ad)
    {
        if (Math.Abs(Yuvarla2(yeniBakiye)) > D15_2)
            throw new BadRequestException(
                $"{ad} ile bakiye kaydedilebilecek en büyük değeri aşıyor").AlanIle(alan);
    }

    /// <summary>
    /// PAKET PATCH-5 — oran/yüzde alanı: kolon ölçeğinden fazla ondalık SESSİZCE
    /// yuvarlanmaz, reddedilir (Pos <c>OranDogrula</c> ve PATCH-4 gerçek milyem kapısıyla
    /// aynı kural; Anayasa md.1).
    /// </summary>
    public static decimal? OndalikSiniri(decimal? deger, int olcek, string alan, string ad)
    {
        if (deger is { } d && Math.Round(d, olcek) != d)
            throw new BadRequestException(
                $"{ad} en çok {olcek} ondalık haneli olabilir").AlanIle(alan);
        return deger;
    }

    /// <summary>Kasa/banka hesabı para birimleri — tek liste (DTO deseni "TRY|USD|EUR|GBP" ile aynı).</summary>
    public static readonly string[] HesapParaBirimleri = ["TRY", "USD", "EUR", "GBP"];

    /// <summary>
    /// PAKET PATCH-5 — para birimi girdisi. <c>[RegularExpression]</c> boş dizeyi GEÇERLİ sayar
    /// (öznitelik null/"" için true döner); "" kasaya/hesaba para birimi olarak yazılıyordu.
    /// null → null (çağıran korur ya da varsayılanı koyar); "" ya da listede olmayan → 400.
    /// </summary>
    public static string? ParaBirimi(string? deger, string alan)
    {
        if (deger is null) return null;
        if (!HesapParaBirimleri.Contains(deger, StringComparer.Ordinal))
            throw new BadRequestException(
                "Geçersiz para birimi — kabul edilen değerler: TRY, USD, EUR, GBP").AlanIle(alan);
        return deger;
    }

    /// <summary>
    /// PAKET PATCH-5 — metin kolonu (varchar(n)) tavanı. MySQL 1406 (Data too long) genel
    /// ağda değildir → 500 olurdu; burada 400 alan.
    /// </summary>
    public static string? MetinSiniri(string? deger, int azami, string alan, string ad)
    {
        if (deger is not null && deger.Length > azami)
            throw new BadRequestException($"{ad} en fazla {azami} karakter olabilir").AlanIle(alan);
        return deger;
    }

    /// <summary>
    /// JSON metin alanı: dize ya da null olmalı; başka tür (sayı, nesne) 500 değil 400 alan.
    /// null → null döner (çağıran "null = sil/koru" kararını verir).
    /// </summary>
    public static string? JsonMetin(JsonElement oge, string alan, string ad)
    {
        if (oge.ValueKind == JsonValueKind.Null) return null;
        if (oge.ValueKind != JsonValueKind.String)
            throw new BadRequestException($"{ad} metin olmalı").AlanIle(alan);
        return oge.GetString();
    }

    /// <summary>
    /// Değer (kolonun <paramref name="olcek"/> hanesine yuvarlanmış hâliyle) mutlak olarak
    /// <paramref name="tavan"/>ı aşıyorsa 400 alan:<paramref name="alan"/>; aşmıyorsa değeri döndürür.
    /// </summary>
    public static decimal Denetle(decimal deger, int olcek, decimal tavan, string alan, string ad)
    {
        if (Math.Abs(Math.Round(deger, olcek, MidpointRounding.AwayFromZero)) > tavan)
            throw new BadRequestException(
                $"{ad} çok büyük — kaydedilebilecek en büyük değeri aşıyor").AlanIle(alan);
        return deger;
    }

    /// <summary>Boş geçilebilir alan için <see cref="Denetle(decimal,int,decimal,string,string)"/>.</summary>
    public static decimal? Denetle(decimal? deger, int olcek, decimal tavan, string alan, string ad) =>
        deger is { } d ? Denetle(d, olcek, tavan, alan, ad) : null;

    /// <summary>
    /// JSON sayısını decimal'e çevirir. decimal aralığını (±7,9·10²⁸) aşan sayıda
    /// <see cref="JsonElement.GetDecimal"/> FormatException atar → 500 olurdu; burada 400 alan.
    /// Sayı olmayan değer de 400'dür (sessiz yorum yok).
    /// </summary>
    public static decimal JsonSayi(JsonElement oge, string alan, string ad)
    {
        if (oge.ValueKind != JsonValueKind.Number)
            throw new BadRequestException($"{ad} sayı olmalı").AlanIle(alan);
        if (!oge.TryGetDecimal(out var d))
            throw new BadRequestException(
                $"{ad} çok büyük — kaydedilebilecek en büyük değeri aşıyor").AlanIle(alan);
        return d;
    }

    // PAKET PATCH-2: web varsayılanı AllowReadingFromString — kalem dizisinde "1.500" 1,5 okunurdu.
    // Strict: tırnaklı sayı JsonException → 400 alan (genel serileştirici ayarıyla aynı kural).
    private static readonly JsonSerializerOptions KalemAyari = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict,
    };

    /// <summary>
    /// JSON dizisini kalem DTO listesine çözer; tür/aralık hatası (ör. decimal'e sığmayan sayı,
    /// sayı yerine metin) 500 değil 400 alan:<paramref name="alan"/> olur.
    /// </summary>
    public static List<T> JsonListe<T>(JsonElement oge, string alan, string ad)
    {
        try
        {
            return oge.Deserialize<List<T>>(KalemAyari) ?? [];
        }
        catch (JsonException)
        {
            throw new BadRequestException(
                $"{ad} okunamadı: sayı alanları geçerli ve kaydedilebilir büyüklükte olmalı").AlanIle(alan);
        }
    }
}
