using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// ÜRÜN AĞACI VE YERLEŞİM — rakip taramasının stok başlığındaki üç kalem
//
// 1. ÇOK SEVİYELİ ÜRÜN AĞACI — rakiplerde "bölüm / stok cinsi / stok türü /
//    stok grubu" diye 4 seviyeye kadar çıkıyor. Bizde tek düz `Tip` alanı vardı
//    (ZIYNET, KULCE…), kırılımlı stok raporu alınamıyordu.
//
// 2. REYON / VİTRİN — hangi malın hangi vitrinde durduğu. Sayım ve sorumluluk
//    bunun üzerinden yürür.
//
// 3. KONSİNYE — başkasının malı bizim vitrinde. Kendi stoğumuzla karışırsa
//    hem stok değeri hem kâr yanlış çıkar: satılınca para mal sahibinin olur.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Çok seviyeli ürün kategorisi. <see cref="UstId"/> ile kendine referans verir;
/// seviye sayısı sınırlı değildir (pratikte 3–4).
/// </summary>
[Table("urun_kategorileri")]
public class UrunKategorisi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    [Column("ad"), MaxLength(150)] public string Ad { get; set; } = null!;

    /// <summary>Üst kategori. Kök kategorilerde null.</summary>
    [Column("ust_id"), MaxLength(36)] public string? UstId { get; set; }

    /// <summary>Aynı seviyedeki sıralama.</summary>
    [Column("sira")] public int Sira { get; set; }

    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

/// <summary>
/// Şube içindeki fiziksel yerleşim birimi: vitrin, kasa, depo, çanta.
///
/// <para>
/// ⚠ KAPSAM SINIRI — DÜRÜST KAYIT: reyon ÜRÜN KARTINA bağlanır
/// (<c>Urun.ReyonId</c>), stok satırına DEĞİL. Yani "bu ürün şu vitrinde durur"
/// denebilir; "aynı üründen 3 tanesi vitrinde, 2 tanesi kasada" DENEMEZ.
/// Stok anahtarını (ürün + şube) üçlüye çıkarmak satış, transfer ve sayımın
/// tamamını etkiler; bu turda yapılmadı. Reyon bazlı sayım ve sorumluluk
/// raporu bu sınır içinde çalışır.
/// </para>
/// </summary>
[Table("reyonlar")]
public class Reyon : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;

    [Column("ad"), MaxLength(100)] public string Ad { get; set; } = null!;

    /// <summary>VITRIN · KASA · DEPO · CANTA</summary>
    [Column("tip"), MaxLength(20)] public string Tip { get; set; } = ReyonTipi.Vitrin;

    /// <summary>Reyondan sorumlu kullanıcı — sayım farkı kime sorulacak.</summary>
    [Column("sorumlu_kullanici_id"), MaxLength(36)] public string? SorumluKullaniciId { get; set; }

    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

public static class ReyonTipi
{
    public const string Vitrin = "VITRIN";
    public const string Kasa = "KASA";
    public const string Depo = "DEPO";
    public const string Canta = "CANTA";

    public static readonly string[] Hepsi = [Vitrin, Kasa, Depo, Canta];
    public const string Desen = "VITRIN|KASA|DEPO|CANTA";
}
