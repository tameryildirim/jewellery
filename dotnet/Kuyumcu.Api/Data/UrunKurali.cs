using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// M4 (müşteri 16.10: "Ayarlar bölümünde bileziği ben tanımlarım… içinde bilezik geçiyorsa binde bir…
/// bilezik ise şu kadar, kolyeyse bu kadar, tek tek yapma"; "kurallar var, bu olursa…") — ÜRÜN KURALI.
/// Koşul: kategori ve/veya "adında geçen" metin. Değer: alış/satış milyemi, KDV'ye giren pay (binde),
/// KDV oranı (%). Yeni üründe BOŞ alanlar uyan ilk kuraldan (sıraya göre) doldurulur; mevcut ürünlere
/// önizleme + uygula ile yayılır. Ürünün kendi (elle girilmiş) değeri her zaman kazanır.
/// </summary>
[Table("urun_kurallari")]
public class UrunKurali : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("ad"), MaxLength(100)] public string Ad { get; set; } = null!;
    /// <summary>Ürün adında geçen metin (büyük/küçük harf ve Türkçe harf duyarsız). Boş = ada bakılmaz.</summary>
    [Column("ad_icerir"), MaxLength(100)] public string? AdIcerir { get; set; }
    /// <summary>Ürün kategorisi. Boş = kategoriye bakılmaz. İkisi de doluysa İKİSİ birden uymalı.</summary>
    [Column("kategori_id"), MaxLength(36)] public string? KategoriId { get; set; }
    [Column("alis_milyemi", TypeName = "decimal(7,2)")] public decimal? AlisMilyemi { get; set; }
    [Column("satis_milyemi", TypeName = "decimal(7,2)")] public decimal? SatisMilyemi { get; set; }
    [Column("kdv_matrah_binde", TypeName = "decimal(7,3)")] public decimal? KdvMatrahBinde { get; set; }
    /// <summary>KDV oranı YÜZDE (ürün kartıyla aynı birim, ör. 20).</summary>
    [Column("kdv_orani", TypeName = "decimal(5,2)")] public decimal? KdvOrani { get; set; }
    [Column("sira")] public int Sira { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}
