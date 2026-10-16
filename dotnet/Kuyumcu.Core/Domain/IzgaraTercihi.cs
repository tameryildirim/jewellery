using System.Text.Json;
using System.Text.RegularExpressions;

namespace Kuyumcu.Core.Domain;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET GRID-1 — ORTAK IZGARA (grid) KİŞİSEL DÜZEN TERCİHİ · TEK KURAL KAYNAĞI
//
// Kurucu 08.10: "kolon sırası (sürükle + klavye), göster/gizle, sıralama;
// kullanıcıya + ekrana özel kalıcı; diğerlerine varsayılan; varsayılana dön."
// Envanter GIRDI-KURALLARI-ENVANTER-2026-10-08 § G1.
//
// NEDEN Core'da: aynı kural İKİ yerde koşar — sunucu (TercihModulu: yazarken
// reddeder, 400) ve istemci (Izgara.razor: okurken bozuk/eski tercihi yok sayar,
// varsayılan düzeni çizer). İki kopya olursa biri gevşer ("bir yerde var
// diğerinde yok" — kurucunun şikâyeti). Tek sınıf, iki çağıran.
//
// Anahtar biçimi: grid:<ekran>:<grid-id>   (ör. grid:cariler:cari-liste)
// Değer biçimi : {"s":1,"k":["ad","telefon"],"g":["has"],"o":{"a":"ad","y":"azalan"}}
//   s = sürüm (ekranın kolon kümesi sürümü), k = kolon sırası, g = gizli kolonlar,
//   o = sıralama (a = kolon anahtarı, y = artan|azalan; null = varsayılan sıra).
//
// ⚠ Tercih İŞ VERİSİ DEĞİLDİR: hiçbir yetki/kapı ona bakmaz; yalnız çizimi
//   etkiler. Bu yüzden okurken FAIL-SAFE'tir (bozuksa varsayılan, hata yok),
//   yazarken FAIL-CLOSED'dır (biçim dışı değer depoya girmez).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Kayıtlı ızgara sıralaması: kolon anahtarı + yön ("artan" | "azalan").</summary>
public sealed record IzgaraSiralamaTercihi(string A, string Y);

/// <summary>Bir ızgaranın kişisel düzeni (sürüm · kolon sırası · gizliler · sıralama).</summary>
public sealed record IzgaraTercihi(int S, IReadOnlyList<string> K, IReadOnlyList<string> G, IzgaraSiralamaTercihi? O);

public static class IzgaraTercihKurali
{
    /// <summary>Tercih anahtarı öneki. Beyaz liste DESENDİR (aşağıda) + bilinen ekran listesi.</summary>
    public const string Onek = "grid:";

    /// <summary>
    /// Bilinen ekranlar — anahtarın &lt;ekran&gt; parçası bu listede olmalıdır.
    /// Bir ekran ortak ızgaraya geçtiğinde (göç paketleri) adı buraya eklenir;
    /// listede olmayan ekran 400 alır (beyaz liste fail-closed).
    /// </summary>
    public static readonly IReadOnlyList<string> BilinenEkranlar =
    [
        "cariler",
        // PAKET GOC-A (para ekranları) — 08.10
        "banka", "banka-ekstre", "faturalar", "kasalar", "kasa-raporu", "vezne", "kur",
        "pos", "pos-tahsilatlar", "gider", "gider-pusulalari", "hurda", "emanet",
        // PAKET GOC-B (kimlik ve uyum ekranları) — 08.10
        "uyum", "evraklar", "evrak-takvimi", "vade-takibi", "bildirimler",
        // PAKET GOC-C (ürün ve fiş ekranları) — 08.10
        "urunler", "urun-agaci", "urun-etiketleri", "vitrin", "fiyatlandirma", "sayim", "kurulum",
        "veri-ice-aktarim", "veri-disa-aktarim", "islemler", "siparisler", "siparis-detay",
        // PAKET GOC-D (yönetim ve rapor ekranları) — 09.10
        "ayarlar", "kullanicilar", "isletme", "yedekler", "raporlar", "kar-zarar", "satis-analizi",
        "denetim-izi", "islem-defteri", "yetkiler",
        // PAKET EKSIK-B1-SATINALMA — 09.10
        "satin-alma", "irsaliyeler",
    ];

    /// <summary>Tek ızgara tercih değerinin en büyük uzunluğu (karakter).</summary>
    public const int EnCokDegerUzunlugu = 2000;

    /// <summary>Kullanıcı başına en çok ızgara tercih anahtarı.</summary>
    public const int KullaniciBasinaEnCokAnahtar = 200;

    /// <summary>Anahtar sütunu (kullanici_tercihleri.anahtar) VARCHAR(60) — taşan anahtar 400 alır, 500 değil.</summary>
    public const int EnCokAnahtarUzunlugu = 60;

    /// <summary>Kolon anahtarı ve sayısı sınırları.</summary>
    public const int EnCokKolon = 60;

    public const string YonArtan = "artan";
    public const string YonAzalan = "azalan";

    private static readonly Regex AnahtarDeseni = new("^grid:([a-z0-9-]+):([a-z0-9-]+)$", RegexOptions.CultureInvariant);
    private static readonly Regex KolonDeseni = new("^[a-z0-9-]{1,40}$", RegexOptions.CultureInvariant);

    /// <summary>Anahtar "grid:" ile mi başlıyor (ızgara tercihi mi)?</summary>
    public static bool IzgaraAnahtariMi(string? anahtar) =>
        anahtar is not null && anahtar.StartsWith(Onek, StringComparison.Ordinal);

    /// <summary>Ekran + ızgara kimliğinden anahtar üretir (biçim denetlenmez — <see cref="AnahtarGecerli"/>).</summary>
    public static string Anahtar(string ekran, string gridId) => $"{Onek}{ekran}:{gridId}";

    /// <summary>Anahtar desene uyuyor, uzunluğu sütuna sığıyor ve ekranı bilinen listede mi?</summary>
    public static bool AnahtarGecerli(string? anahtar)
    {
        if (anahtar is null || anahtar.Length > EnCokAnahtarUzunlugu) return false;
        var m = AnahtarDeseni.Match(anahtar);
        return m.Success && BilinenEkranlar.Contains(m.Groups[1].Value);
    }

    /// <summary>
    /// Değeri ayrıştırır ve DOĞRULAR. Biçim dışıysa <c>null</c> + Türkçe sebep döner.
    /// Bilinmeyen alan, yinelenen kolon, geçersiz yön, eksik sürüm → geçersiz.
    /// </summary>
    public static IzgaraTercihi? Coz(string? deger, out string? hata)
    {
        hata = null;
        if (string.IsNullOrWhiteSpace(deger)) { hata = "Izgara tercihi boş."; return null; }
        if (deger.Length > EnCokDegerUzunlugu)
        {
            hata = $"Izgara tercihi çok uzun: en çok {EnCokDegerUzunlugu} karakter.";
            return null;
        }
        try
        {
            using var belge = JsonDocument.Parse(deger);
            var kok = belge.RootElement;
            if (kok.ValueKind != JsonValueKind.Object) { hata = "Izgara tercihi bir nesne olmalı."; return null; }

            int? s = null;
            List<string>? k = null, g = null;
            IzgaraSiralamaTercihi? o = null;
            foreach (var p in kok.EnumerateObject())
            {
                switch (p.Name)
                {
                    case "s":
                        if (p.Value.ValueKind != JsonValueKind.Number || !p.Value.TryGetInt32(out var sv) || sv < 1 || sv > 1000)
                        { hata = "Izgara tercihinde sürüm (s) 1–1000 arası tam sayı olmalı."; return null; }
                        s = sv;
                        break;
                    case "k":
                        k = KolonListesi(p.Value, "k", out hata);
                        if (k is null) return null;
                        break;
                    case "g":
                        g = KolonListesi(p.Value, "g", out hata);
                        if (g is null) return null;
                        break;
                    case "o":
                        if (p.Value.ValueKind == JsonValueKind.Null) { o = null; break; }
                        o = SiralamaCoz(p.Value, out hata);
                        if (o is null) return null;
                        break;
                    default:
                        hata = $"Izgara tercihinde bilinmeyen alan: {p.Name}.";
                        return null;
                }
            }
            if (s is null) { hata = "Izgara tercihinde sürüm (s) yok."; return null; }
            return new IzgaraTercihi(s.Value, k ?? [], g ?? [], o);
        }
        catch (JsonException)
        {
            hata = "Izgara tercihi geçerli JSON değil.";
            return null;
        }
    }

    private static List<string>? KolonListesi(JsonElement e, string ad, out string? hata)
    {
        hata = null;
        if (e.ValueKind != JsonValueKind.Array) { hata = $"Izgara tercihinde '{ad}' bir dizi olmalı."; return null; }
        var liste = new List<string>();
        foreach (var x in e.EnumerateArray())
        {
            if (x.ValueKind != JsonValueKind.String || !KolonDeseni.IsMatch(x.GetString()!))
            { hata = $"Izgara tercihinde '{ad}' içinde geçersiz kolon anahtarı."; return null; }
            var v = x.GetString()!;
            if (liste.Contains(v)) { hata = $"Izgara tercihinde '{ad}' içinde yinelenen kolon: {v}."; return null; }
            liste.Add(v);
        }
        if (liste.Count > EnCokKolon) { hata = $"Izgara tercihinde '{ad}' en çok {EnCokKolon} kolon taşır."; return null; }
        return liste;
    }

    private static IzgaraSiralamaTercihi? SiralamaCoz(JsonElement e, out string? hata)
    {
        hata = null;
        if (e.ValueKind != JsonValueKind.Object) { hata = "Izgara tercihinde sıralama (o) bir nesne olmalı."; return null; }
        string? a = null, y = null;
        foreach (var p in e.EnumerateObject())
        {
            if (p.Value.ValueKind != JsonValueKind.String) { hata = "Izgara sıralamasında alanlar metin olmalı."; return null; }
            switch (p.Name)
            {
                case "a": a = p.Value.GetString(); break;
                case "y": y = p.Value.GetString(); break;
                default: hata = $"Izgara sıralamasında bilinmeyen alan: {p.Name}."; return null;
            }
        }
        if (a is null || !KolonDeseni.IsMatch(a)) { hata = "Izgara sıralamasında kolon (a) geçersiz."; return null; }
        if (y is not (YonArtan or YonAzalan)) { hata = "Izgara sıralamasında yön (y) 'artan' ya da 'azalan' olmalı."; return null; }
        return new IzgaraSiralamaTercihi(a, y);
    }

    /// <summary>Tercihi kısa JSON biçimine yazar (alan adları s/k/g/o).</summary>
    public static string Yaz(IzgaraTercihi t)
    {
        using var akim = new MemoryStream();
        using (var w = new Utf8JsonWriter(akim))
        {
            w.WriteStartObject();
            w.WriteNumber("s", t.S);
            w.WriteStartArray("k");
            foreach (var x in t.K) w.WriteStringValue(x);
            w.WriteEndArray();
            w.WriteStartArray("g");
            foreach (var x in t.G) w.WriteStringValue(x);
            w.WriteEndArray();
            if (t.O is null) w.WriteNull("o");
            else
            {
                w.WriteStartObject("o");
                w.WriteString("a", t.O.A);
                w.WriteString("y", t.O.Y);
                w.WriteEndObject();
            }
            w.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(akim.ToArray());
    }
}
