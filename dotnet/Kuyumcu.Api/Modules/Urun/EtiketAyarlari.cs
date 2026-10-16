namespace Kuyumcu.Api.Modules.Urun;

/// <summary>
/// ETİKET İBARE AYARLARI (paket 20) — ADR-0005: doğrulanmamış mevzuat iddiası
/// koda GÖMÜLMEZ, yapılandırmadan okunur ve varsayılanı KAPALIDIR.
///
/// <para>
/// ⚠ <b>AÇIK SORU:</b> rodyum kaplamanın etikette/faturada gösterilmesinin
/// ZORUNLU olup olmadığı doğrulanmadı (Darphane ayar evi uygulamaları —
/// paket 20 mevzuat dayanağı "VARSAYIM" işaretli). Zorunlu olduğu varsayılıp
/// koda sabitlenseydi, sistem mevzuatta olmayan bir iddiayı kuyumcuya
/// dayatmış olurdu. Bu yüzden ibare <b>varsayılan KAPALI</b> gelir; kurucunun
/// ya da mali müşavirin teyidiyle açılır.
/// </para>
/// <para>
/// Taş kökeni ibaresi (md.11/(e)) bu ayardan ETKİLENMEZ — o hüküm DOĞRULANDI
/// ve fail-closed uygulanmaya devam eder.
/// </para>
/// </summary>
public sealed class EtiketAyarlari
{
    /// <summary>Rodyum kaplama ibaresi etikete basılsın mı? Varsayılan: hayır.</summary>
    public bool RodyumIbaresiGoster { get; init; }

    /// <summary>İbare açıkken etikete basılacak metin.</summary>
    public string RodyumIbaresi { get; init; } = "rodyum kaplama";
}
