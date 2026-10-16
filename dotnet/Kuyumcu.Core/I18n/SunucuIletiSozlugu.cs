using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET DIL-SUNUCU — SUNUCU İLETİ SÖZLÜĞÜ (TR → EN)
//
// SUNUCUNUN EKRANA GİDEN HER CÜMLESİ (400/403/404/409 hata iletileri, gün sonu ve
// mutabakat uyarıları, künye) istek diline göre BURADAN seçilir. Anahtar = Türkçe
// cümle ŞABLONU; değişken yerler `{}` ile işaretlenir (kaynakta `{kasaId}` →
// `{}`). Çalışma anında gerçek (doldurulmuş) cümle şablona REGEX ile uydurulur ve
// yakalanan değerler sırayla İngilizce şablona konur.
//
// ⚠ KURALLAR
//   • TR modda cümle AYNEN döner (hiçbir şeye dokunulmaz).
//   • Eşleşme yoksa cümle AYNEN döner — çeviri UYDURULMAZ (Anayasa md.1); bu
//     durumda denetim.sh SUNUCU_CIPLAK_TURKCE_ILETI sayacı kaynak satırı sayar.
//   • Veri ÇEVRİLMEZ: yakalanan değer (müşteri/ürün/şube adı, tutar) olduğu gibi
//     kalır; yalnız bilinen KOD ve alan adları DegerCevir ile çevrilir.
//   • Şablonun SABİT METNİ en az bir harf içerir (`{}` tek başına şablon olamaz).
//   • TR ve EN şablonda `{}` sayısı AYNI olmalıdır (DilSunucuTestleri bunu doğrular).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Sunucu cümlelerinin istek diline göre seçimi.</summary>
public static partial class SunucuIletiSozlugu
{
    private sealed record Sablon(string Tr, string En, Regex Desen, int SabitUzunluk);

    private static readonly Lazy<(IReadOnlyDictionary<string, string> Tam, Sablon[] Sablonlar)> Model = new(Kur);

    /// <summary>Sözlükteki toplam kayıt sayısı (testler için).</summary>
    public static int KayitSayisi => Cift.Length;

    /// <summary>Tüm (Tr, En) çiftleri (testler için salt okunur).</summary>
    public static IReadOnlyList<(string Tr, string En)> Ciftler => Cift;

    private static (IReadOnlyDictionary<string, string>, Sablon[]) Kur()
    {
        var tam = new Dictionary<string, string>(StringComparer.Ordinal);
        var sablonlar = new List<Sablon>();
        foreach (var (tr, en) in Cift)
        {
            if (!tr.Contains("{}"))
            {
                tam[tr] = en;
                continue;
            }
            var parcalar = tr.Split("{}");
            var sb = new StringBuilder("^");
            for (var i = 0; i < parcalar.Length; i++)
            {
                if (i > 0) sb.Append("(.+?)");
                sb.Append(Regex.Escape(parcalar[i]));
            }
            sb.Append('$');
            sablonlar.Add(new Sablon(tr, en,
                new Regex(sb.ToString(), RegexOptions.Singleline | RegexOptions.CultureInvariant),
                parcalar.Sum(p => p.Length)));
        }
        // Daha ÖZGÜL (daha çok sabit karakter) şablon önce denenir.
        return (tam, sablonlar.OrderByDescending(s => s.SabitUzunluk).ToArray());
    }

    /// <summary>Geçerli isteğin diline göre (<see cref="IstekDili.Gecerli"/>).</summary>
    public static string Cevir(string? tr) => Cevir(tr, IstekDili.Gecerli);

    /// <summary>
    /// Doldurulmuş Türkçe cümleyi <paramref name="dil"/> diline çevirir. TR modda ya da
    /// eşleşme yoksa cümle AYNEN döner.
    /// </summary>
    public static string Cevir(string? tr, string? dil)
    {
        if (string.IsNullOrEmpty(tr)) return tr ?? "";
        if (IstekDili.Normalize(dil) != "en") return tr;
        var (tam, sablonlar) = Model.Value;
        if (tam.TryGetValue(tr, out var hazir)) return hazir;
        foreach (var s in sablonlar)
        {
            var m = s.Desen.Match(tr);
            if (!m.Success) continue;
            var i = 0;
            return Yer.Replace(s.En, _ => DegerCevir(m.Groups[++i].Value, "en"));
        }
        return tr;
    }

    private static readonly Regex Yer = new(@"\{\}", RegexOptions.CultureInvariant);

    /// <summary>Bu cümle sözlükte KARŞILIĞI olan bir şablona/cümleye uyuyor mu? (testler)</summary>
    public static bool Kapsiyor(string? tr)
    {
        if (string.IsNullOrEmpty(tr)) return false;
        var (tam, sablonlar) = Model.Value;
        return tam.ContainsKey(tr) || sablonlar.Any(s => s.Desen.IsMatch(tr));
    }

    // ── Yer tutucuya giren BİLİNEN kodlar ve alan adları ─────────────────────
    // Yalnız TAM eşleşme çevrilir (kod/etiket); serbest metin (veri) AYNEN kalır.
    private static readonly IReadOnlyDictionary<string, string> Degerler =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ISLENDI"] = "PROCESSED", ["HATA"] = "ERROR", ["BASARILI"] = "SUCCESSFUL", ["BASARISIZ"] = "FAILED",
            ["SURUYOR"] = "RUNNING", ["TAM"] = "FULL", ["ELLE"] = "MANUAL", ["ZAMANLI"] = "SCHEDULED",
            ["ACIK"] = "OPEN", ["KAPALI"] = "CLOSED", ["BEKLIYOR"] = "PENDING", ["TASLAK"] = "DRAFT",
            ["ONAYLANDI"] = "APPROVED", ["IPTAL"] = "CANCELLED", ["HAZIR"] = "READY", ["TESLIM"] = "DELIVERED",
            ["KISMI_KABUL"] = "PARTIALLY RECEIVED", ["PORTFOYDE"] = "IN PORTFOLIO", ["CIRO_EDILDI"] = "ENDORSED",
            ["TAHSIL_EDILDI"] = "COLLECTED", ["KARSILIKSIZ"] = "BOUNCED", ["TAMAMLANDI"] = "COMPLETED",
            ["UYGULANDI"] = "APPLIED", ["PROVA"] = "DRY RUN", ["GERI_ALINDI"] = "ROLLED BACK",
            ["SATIS"] = "SALE", ["ALIS"] = "PURCHASE", ["GELEN"] = "INCOMING", ["GIDEN"] = "OUTGOING",
            ["MUSTERI"] = "CUSTOMER", ["TEDARIKCI"] = "SUPPLIER", ["TEZGAHTAR"] = "COUNTER STAFF",
            ["MUDUR"] = "MANAGER", ["MUHASEBE"] = "ACCOUNTING", ["SAHIP"] = "OWNER", ["SAHİP"] = "OWNER",
            ["NAKIT"] = "CASH", ["KART"] = "CARD", ["HAVALE"] = "TRANSFER", ["VERESIYE"] = "CREDIT",
            ["Açıklama"] = "Description", ["Ad"] = "Name", ["Tutar"] = "Amount", ["Not"] = "Note", ["Notlar"] = "Notes",
            ["Adres"] = "Address", ["Telefon"] = "Phone", ["Ad soyad"] = "Full name", ["E-posta"] = "E-mail",
            ["Gerekçe"] = "Reason", ["Barkod"] = "Barcode", ["Kod"] = "Code", ["Stok kodu"] = "Stock code",
            ["Ürün adı"] = "Product name", ["Kategori adı"] = "Category name", ["Reyon adı"] = "Shelf name",
            ["Cari adı"] = "Customer name", ["Şube adı"] = "Branch name", ["Kasa adı"] = "Cash desk name",
            ["Banka adı"] = "Bank name", ["Hesap adı"] = "Account name", ["Evrak no"] = "Instrument number",
            ["Vade günü"] = "Due date", ["Tarih"] = "Date", ["Miktar"] = "Quantity", ["Adet"] = "Quantity",
            ["Gram"] = "Grams", ["Ayar"] = "Purity", ["Fiyat"] = "Price", ["Birim fiyat"] = "Unit price",
            ["İşçilik"] = "Labour", ["İndirim"] = "Discount", ["Kur"] = "Rate", ["Kasa"] = "Cash desk",
            ["Şube"] = "Branch", ["Cari"] = "Customer", ["Ürün"] = "Product", ["Kalem"] = "Line",
        };

    /// <summary>
    /// Künye satırı (destek çağrısında okunan TEK satır) anahtar/kaynak etiketleri: TR "surum=1.2" ·
    /// EN "version=1.2". Anahtar TR'dir; bilinmeyen anahtar AYNEN döner. Değerler (sürüm, zaman) çevrilmez.
    /// </summary>
    public static string KunyeEtiketi(string trAnahtar, string? dil)
    {
        if (IstekDili.Normalize(dil) != "en") return trAnahtar;
        return trAnahtar switch
        {
            "surum" => "version", "derleme" => "build", "ortam" => "environment", "migration" => "migrations",
            "son" => "last", "bekleyen" => "pending", "damga" => "stamp", "dosya" => "file",
            _ => trAnahtar,
        };
    }

    /// <summary>Yakalanan değer bilinen bir kod/etikete TAM uyuyorsa çevirir; yoksa AYNEN.</summary>
    /// <remarks>
    /// İÇ İÇE CÜMLE: yakalanan değer kendisi sözlükte bir cümleyse (ör. "Hurda alış: {}" içindeki eksik listesi,
    /// "…({})" içindeki iç ileti) ya da " · " ile ayrılmış cümle listesiyse her parça AYRI çevrilir.
    /// Yakalanan değer daha kısa olduğundan özyineleme biter.
    /// </remarks>
    public static string DegerCevir(string deger, string? dil)
    {
        if (IstekDili.Normalize(dil) != "en") return deger;
        if (Degerler.TryGetValue(deger, out var en)) return en;
        var ic = Cevir(deger, "en");
        if (ic != deger) return ic;
        if (deger.Contains(" · ", StringComparison.Ordinal))
            return string.Join(" · ", deger.Split(" · ").Select(p => Cevir(p, "en")));
        return deger;
    }
}
