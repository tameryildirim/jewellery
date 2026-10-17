using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// KUR KAYNAKLARI (kurucu 17.10) — HAS BEDELİ (KDVK 23/e) GÜN KAPANIŞI.
/// <para>
/// Mevzuat (KDV GUT § III/A-4.2.1; araştırma § 5): has bedeli = has gram × "Borsa İstanbul'da
/// işlem yapılan son resmî işgünü" külçe altın kapanışı. Kiracı has bedeli kaynağını seçer
/// (tenants.ozel_matrah_kaynagi); faturada seçili kaynağın SON İŞGÜNÜ kapanışı kullanılır ve
/// kaynak kodu + gün + fiyat faturaya yazılır.
/// </para>
/// <para>
/// Satır başına bir (kiracı, kaynak, gün). BIST ve ELLE kapanışını kuyumcu girer (BIST
/// verisinin kendiliğinden çekilip dağıtılması lisans ister — araştırma § 1/4); HAREM ve
/// ALTINKAYNAK kapanışını zamanlayıcı kaynaktan yazar. Bir faturada kullanılmış kapanış
/// DEĞİŞTİRİLEMEZ (belgenin dayanağıdır).
/// </para>
/// </summary>
[Table("ozel_matrah_kapanislari")]
public class OzelMatrahKapanisi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary>BIST · HAREM · ALTINKAYNAK · ELLE.</summary>
    [Column("kaynak"), MaxLength(20)] public string Kaynak { get; set; } = null!;

    /// <summary>Kapanışın ait olduğu işletme günü (Europe/Istanbul).</summary>
    [Column("gun")] public DateOnly Gun { get; set; }

    /// <summary>TL / has gram (1000/1000). TL/kg girilmişse ÷ 1000 çevrilmiş değer.</summary>
    [Column("fiyat", TypeName = "decimal(15,4)")] public decimal Fiyat { get; set; }

    /// <summary>Girilen ham değer (TL/kg ya da TL/g) — çevrimin kanıtı. Otomatik kapanışta fiyatın aynısı.</summary>
    [Column("girilen_deger", TypeName = "decimal(18,4)")] public decimal GirilenDeger { get; set; }

    /// <summary>TL_GRAM · TL_KG.</summary>
    [Column("girilen_birim"), MaxLength(8)] public string GirilenBirim { get; set; } = "TL_GRAM";

    /// <summary>Değerin dayanağı (örn. "BIST günlük bülten" ya da "Harem ALTIN satış · yayın 17.10 17:59").</summary>
    [Column("dayanak"), MaxLength(300)] public string Dayanak { get; set; } = null!;

    /// <summary>Elle girende kullanıcı; otomatik kapanışta NULL.</summary>
    [Column("kaydeden_kullanici_id"), MaxLength(36)] public string? KaydedenKullaniciId { get; set; }

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}
