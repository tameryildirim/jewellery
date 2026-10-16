using Kuyumcu.Core.I18n;

namespace Kuyumcu.Web.Bilesenler.Fis;

/// <summary>
/// PAKET S2-2 — işlem türlerinin listesi ve adları. Islemler.razor'dan BİREBİR
/// taşındı (TumTurler / TurEtiket): sayfa 1.400 satır sınırında
/// (S21 yapı testi) ve ızgara bileşeni aynı listeyi kullanır. Tek kaynak:
/// pro sekmesi ile basit ızgara düğmesi AYNI adı taşır (UX-S31).
/// </summary>
public static class FisTurleri
{
    /// <summary>Sayfanın 8 işlem türü, pro moddaki sekme sırasıyla.</summary>
    public static (string Deger, string Etiket)[] Tumu(SozlukIslem t) =>
    [
        ("SATIS", t.Satis),
        ("ALIS", t.Alis),
        ("HURDA_ALIS", t.HurdaAlis),
        ("ISCILIK", t.Iscilik),
        ("TAKAS", t.Takas),
        ("TRANSFER", t.Transfer),
        ("SATIS_IADE", t.SatisIade),
        ("ALIS_IADE", t.AlisIade),
    ];

    /// <summary>
    /// PAKET T-S2 — tuval Profesyonel.dc.html sekme şeridi, TUVAL SIRASIYLA:
    /// Satış · Alış · Hurda alış · Tahsilat · Ödeme · Takas · Satış iadesi · Alış iadesi.
    /// TAHSILAT / ODEME bu sayfanın türü DEĞİLDİR (BasitPos.Kisayollar: Tahsilat →
    /// Müşteriler, Ödeme kilitli + sebep). ISCILIK / TRANSFER tuvalde sekme değil →
    /// "Diğer ▾" menüsüne taşındı (<see cref="ProDigerTurleri"/>; özellik silinmedi,
    /// id'ler #islem-tur-ISCILIK / #islem-tur-TRANSFER AYNEN).
    /// </summary>
    /// SATIS-3 t11 (tasarım hakemi YÜKSEK "Ödeme her zaman pasif"): tedarikçiye ödeme ucu sunucuda YOK (Cari modülünde
    /// yalnız /cari/{id}/tahsilat var) → sekme hiçbir şey yapmıyordu. Hakemin seçeneğiyle KALDIRILDI (gerekçeli tuval farkı);
    /// ödeme ucu gelince (Müşteriler/Cari paketi) buraya geri eklenir. Tedarikçiye ödeme bugün Alış fişinin ödeme türüyle.
    public static readonly string[] ProSekmeler =
        ["SATIS", "ALIS", "HURDA_ALIS", "TAHSILAT", "TAKAS", "SATIS_IADE", "ALIS_IADE"];

    /// <summary>PAKET T-S2 — tuvalde sekmesi olmayan, "Diğer ▾" menüsünde duran sayfa türleri.</summary>
    public static readonly string[] ProDigerTurleri = ["ISCILIK", "TRANSFER"];

    /// <summary>SATIS-3 t6 — BASİT karo adı (tuval): ızgara, sayfa başlığı ve form başlığı AYNI adı taşır (UX-S31).</summary>
    public static string KaroEtiket(SatisTuvalMetinleri t, string kod, string etiket) => kod switch
    {
        "HURDA_ALIS" => t.KaroAlisEskiAltin,
        "SATIS_IADE" => t.KaroIade,
        "TAKAS" => t.KaroDegisim,
        _ => etiket,
    };

    /// <summary>Türün ekrandaki adı (bilinmeyen tür → İşçilik; eski davranış).</summary>
    public static string Etiket(SozlukIslem t, string tur) => tur switch
    {
        "SATIS" => t.Satis,
        "ALIS" => t.Alis,
        "HURDA_ALIS" => t.HurdaAlis,
        "TAKAS" => t.Takas,
        "TRANSFER" => t.Transfer,
        "SATIS_IADE" => t.SatisIade,
        "ALIS_IADE" => t.AlisIade,
        _ => t.Iscilik,
    };
}
