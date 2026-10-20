using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// Y-03 (20.10, rakip Ariyen "Gişe Kasam" 7 adım / CetaSoft tezgâhtar başına mal + nakit) — GİŞE ZİMMETİ.
/// Sabah yönetici bir tezgâhtara ürün/sarrafiye adedi ve açılış nakdi ZİMMETLER (TEKLIF); tezgâhtar kabul eder (KABUL,
/// açılış nakdi kaynak kasadan gişe kasasına virmanla geçer); gün içi takviye talebi başka bir yönetici tarafından
/// karara bağlanır (kendi talebini onaylayamaz); akşam ürün ve nakit SAYILIR, beklenenle kalem kalem karşılaştırılır,
/// sayılan nakit devir kasasına (ana kasa) virmanla aktarılır (KAPANDI). Vardiya devri = kapat + yeni zimmet.
/// Stok TAŞINMAZ: zimmet bir SORUMLULUK kaydıdır; mal şube stoğunda kalır, satış tezgâhtarın defter hareketinden okunur.
/// Zimmet silinmez; teklif aşamasında İPTAL edilir (iz).
/// </summary>
[Table("gise_zimmetleri")]
public class GiseZimmeti : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;
    /// <summary>Gişe kasası — tezgâhtarın gün içinde tahsilat yazdığı kasa (D1-11 çalışan kasası).</summary>
    [Column("kasa_id"), MaxLength(36)] public string KasaId { get; set; } = null!;
    [Column("personel_id"), MaxLength(36)] public string PersonelId { get; set; } = null!;
    /// <summary>Açılış nakdinin ve nakit takviyenin çıktığı, kapanışta devrin döndüğü ana kasa; nakitsiz zimmette boş olabilir.</summary>
    [Column("kaynak_kasa_id"), MaxLength(36)] public string? KaynakKasaId { get; set; }
    [Column("acilis_nakit", TypeName = "decimal(15,2)")] public decimal AcilisNakit { get; set; }
    /// <summary><see cref="GiseDurum"/>.</summary>
    [Column("durum"), MaxLength(10)] public string Durum { get; set; } = GiseDurum.Teklif;
    [Column("olusturan_id"), MaxLength(36)] public string OlusturanId { get; set; } = null!;
    [Column("kabul_zamani")] public DateTime? KabulZamani { get; set; }
    /// <summary>Kapanışta gişe kasasının NAKİT defteri (kart/havale sayıma girmez — KasaNakitAyrimi).</summary>
    [Column("beklenen_nakit", TypeName = "decimal(15,2)")] public decimal? BeklenenNakit { get; set; }
    [Column("sayilan_nakit", TypeName = "decimal(15,2)")] public decimal? SayilanNakit { get; set; }
    /// <summary>sayılan − beklenen; fark gişe kasasına sayım farkı hareketi olarak yazılır.</summary>
    [Column("nakit_fark", TypeName = "decimal(15,2)")] public decimal? NakitFark { get; set; }
    /// <summary>Nakit ya da ürün farkı varsa ZORUNLU gerekçe (K30: 20–1000).</summary>
    [Column("aciklama", TypeName = "text")] public string? Aciklama { get; set; }
    [Column("devir_kasa_id"), MaxLength(36)] public string? DevirKasaId { get; set; }
    [Column("kapatan_id"), MaxLength(36)] public string? KapatanId { get; set; }
    [Column("kapanis_zamani")] public DateTime? KapanisZamani { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

public static class GiseDurum
{
    public const string Teklif = "TEKLIF";
    public const string Kabul = "KABUL";
    public const string Kapandi = "KAPANDI";
    public const string Iptal = "IPTAL";
    public static readonly string[] Acik = [Teklif, Kabul];
}

/// <summary>
/// Zimmetin kalemi: ZIMMET (sabah verilen ürün adedi) · TAKVIYE (gün içi talep; ürün adedi YA DA nakit tutarı; TALEP →
/// KABUL/RED, karar veren isteyenden FARKLI olmak zorunda) · SAYIM (kapanışta ürün başına beklenen/sayılan/fark).
/// </summary>
[Table("gise_zimmet_kalemleri")]
public class GiseZimmetKalemi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("zimmet_id"), MaxLength(36)] public string ZimmetId { get; set; } = null!;
    /// <summary><see cref="GiseKalemTur"/>.</summary>
    [Column("tur"), MaxLength(10)] public string Tur { get; set; } = null!;
    [Column("urun_id"), MaxLength(36)] public string? UrunId { get; set; }
    /// <summary>ZIMMET/TAKVIYE: verilen adet · SAYIM: beklenen adet.</summary>
    [Column("adet", TypeName = "decimal(12,3)")] public decimal Adet { get; set; }
    /// <summary>Yalnız nakit takviyede dolar.</summary>
    [Column("nakit_tutar", TypeName = "decimal(15,2)")] public decimal? NakitTutar { get; set; }
    /// <summary><see cref="GiseKalemDurum"/>.</summary>
    [Column("durum"), MaxLength(10)] public string Durum { get; set; } = GiseKalemDurum.Kabul;
    [Column("isteyen_id"), MaxLength(36)] public string IsteyenId { get; set; } = null!;
    [Column("onaylayan_id"), MaxLength(36)] public string? OnaylayanId { get; set; }
    [Column("karar_zamani")] public DateTime? KararZamani { get; set; }
    [Column("sayilan_adet", TypeName = "decimal(12,3)")] public decimal? SayilanAdet { get; set; }
    [Column("satilan_adet", TypeName = "decimal(12,3)")] public decimal? SatilanAdet { get; set; }
    [Column("fark", TypeName = "decimal(12,3)")] public decimal? Fark { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

public static class GiseKalemTur
{
    public const string Zimmet = "ZIMMET";
    public const string Takviye = "TAKVIYE";
    public const string Sayim = "SAYIM";
}

public static class GiseKalemDurum
{
    public const string Kabul = "KABUL";
    public const string Talep = "TALEP";
    public const string Red = "RED";
}
