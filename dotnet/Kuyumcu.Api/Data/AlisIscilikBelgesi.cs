using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// M9 (müşteri 16.10: "aradaki masrafı fatura etmen lazım, yoksa kâr görünür… denetimde ceza") —
/// bir ALIŞ hareketinin İŞÇİLİK BELGESİ. Toptancıdan 925 milyemle alınan 22 ayar bilezikte has bedeli
/// (916) ile ödenen arasındaki fark toptancının işçiliğidir; o işçiliğin faturası (ya da gider
/// pusulası) yoksa fark incelemede kâr sayılır. Kayıt alışı DEĞİŞTİRMEZ; alışın yanındaki belge
/// bilgisidir (düzenlenebilir). "Belgesiz" de bir beyandır: uyarı listesinden çıkmaz.
/// </summary>
[Table("alis_iscilik_belgeleri")]
public class AlisIscilikBelgesi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    /// <summary>ALIS türündeki defter satırı — kiracıda TEKİL (bir alışın tek işçilik belgesi kaydı).</summary>
    [Column("alis_hareket_id"), MaxLength(36)] public string AlisHareketId { get; set; } = null!;
    [Column("cari_id"), MaxLength(36)] public string? CariId { get; set; }
    /// <summary><see cref="IscilikBelgeTuru"/>.</summary>
    [Column("belge_turu"), MaxLength(15)] public string BelgeTuru { get; set; } = null!;
    [Column("belge_no"), MaxLength(50)] public string? BelgeNo { get; set; }
    [Column("belge_tarihi")] public DateOnly? BelgeTarihi { get; set; }
    /// <summary>Belgedeki işçilik tutarı (TL, KDV hariç beyan).</summary>
    [Column("iscilik_tl", TypeName = "decimal(15,2)")] public decimal IscilikTl { get; set; }
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

public static class IscilikBelgeTuru
{
    public const string Fatura = "FATURA";
    public const string GiderPusulasi = "GIDER_PUSULASI";
    /// <summary>Kuyumcunun "belge yok" beyanı — kabul edilir ama uyarı sürer (sessiz kapanmaz).</summary>
    public const string Belgesiz = "BELGESIZ";
    public static readonly string[] Tumu = [Fatura, GiderPusulasi, Belgesiz];
}
