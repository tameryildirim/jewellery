using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// FİYAT LİSTESİ — "perakende / toptan / özel müşteri" gibi fiyat katmanı.
///
/// Fiyat = has_grami × has kuru × (1 + <see cref="HasMarjYuzde"/>/100)
///       + ürün işçiliği × <see cref="IscilikCarpanYuzde"/>/100.
///
/// ⚠ NEDEN MARJ ÜRÜN KARTINDA DEĞİL LİSTEDE: Faz 0 §11 kotasyonun ham hâliyle
/// ürüne yansıtılmasını geri aldı ve "(a) kotasyona eklenecek marj alanı" koşulunu
/// koydu. Marj müşteri katmanına göre değişir (toptancıya %2, vitrine %8); ürün
/// başına tek marj bunu taşıyamazdı. Hesap SUNUCUDA yapılır (PANO-KARAR §88):
/// marj ticari sırdır, ekrana kırılım gider, formül gitmez.
/// </summary>
[Table("fiyat_listeleri")]
public class FiyatListesi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("ad"), MaxLength(100)] public string Ad { get; set; } = null!;
    /// <summary>PERAKENDE · TOPTAN · OZEL — yalnız etiket, hesabı değiştirmez.</summary>
    [Column("tur"), MaxLength(20)] public string Tur { get; set; } = "PERAKENDE";
    /// <summary>Has değerine eklenen yüzde (negatif olabilir — toptancıya maliyetine yakın).</summary>
    [Column("has_marj_yuzde", TypeName = "decimal(7,3)")] public decimal HasMarjYuzde { get; set; }
    /// <summary>Ürün işçiliğinin bu listede uygulanan yüzdesi; 100 = aynen.</summary>
    [Column("iscilik_carpan_yuzde", TypeName = "decimal(7,2)")] public decimal IscilikCarpanYuzde { get; set; } = 100m;
    /// <summary>Kiracı başına TEK varsayılan; cariye liste atanmamışsa bu kullanılır.</summary>
    [Column("varsayilan")] public bool Varsayilan { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

/// <summary>
/// KAMPANYA — süreli indirim.
///
/// ⚠ SÜRE ≤ 3 AY: Fiyat Etiketi Yönetmeliği md.11/(3) indirimli satış süresini üç
/// ayla sınırlar (tasfiyede altı ay). Daha uzun kampanya açılamaz.
///
/// İndirim türleri:
///  • ISCILIK_YUZDE — yalnız işçilik bileşenine yüzde (sektörün "indirim işçilikte
///    olur" pratiği; has bileşeni piyasa değeridir, indirim vaadi yanıltıcı sayılır).
///  • TUTAR_YUZDE  — liste fiyatının tamamına yüzde.
///  • TUTAR_TL     — liste fiyatından sabit TL.
/// Uygulanan indirim satış satırına yazılır (<see cref="Hareket.IndirimTutari"/>,
/// <see cref="Hareket.ListeFiyati"/>) — FEY md.11/(1)-(2) "indirimli ve indirim
/// öncesi fiyat birlikte" kuralı fiş ve etiketten bu alanlarla sağlanır.
/// </summary>
[Table("kampanyalar")]
public class Kampanya : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("ad"), MaxLength(150)] public string Ad { get; set; } = null!;
    /// <summary>Yerel takvim günü — dahil.</summary>
    [Column("baslangic")] public DateOnly Baslangic { get; set; }
    /// <summary>Yerel takvim günü — dahil.</summary>
    [Column("bitis")] public DateOnly Bitis { get; set; }
    /// <summary>TUMU · KATEGORI · URUN</summary>
    [Column("hedef_tur"), MaxLength(20)] public string HedefTur { get; set; } = "TUMU";
    /// <summary>KATEGORI → urun_kategorileri.id (alt kategoriler dahil) · URUN → urunler.id</summary>
    [Column("hedef_id"), MaxLength(36)] public string? HedefId { get; set; }
    /// <summary>ISCILIK_YUZDE · TUTAR_YUZDE · TUTAR_TL</summary>
    [Column("indirim_turu"), MaxLength(20)] public string IndirimTuru { get; set; } = "ISCILIK_YUZDE";
    [Column("deger", TypeName = "decimal(15,2)")] public decimal Deger { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }

    public static readonly string[] HedefTurleri = ["TUMU", "KATEGORI", "URUN"];
    public static readonly string[] IndirimTurleri = ["ISCILIK_YUZDE", "TUTAR_YUZDE", "TUTAR_TL"];
    /// <summary>FEY md.11/(3): indirimli satış en fazla üç ay.</summary>
    public const int EnFazlaGun = 92;
}

/// <summary>
/// FİYAT LİSTESİ × ÜRÜN GEÇERSİZ KILMASI — paket 49.
///
/// NEDEN VAR: fiyat listesi tek bir marj/işçilik çarpanı taşır; gerçek dükkânda
/// "bu listede şu ürün istisna" durumu vardır (toptancıya tek bir modelde farklı
/// marj, vitrindeki tek parçaya elle konmuş fiyat). Bu istisna ÜRÜN KARTINA
/// yazılamaz (A13: marj listede kalır, ürün kartına marj alanı EKLENMEZ) —
/// yeri liste × ürün KESİŞİMİDİR.
///
/// ÖNCELİK (FiyatMotoru.Hesapla ile birebir):
///   1. <see cref="SabitFiyat"/> doluysa liste fiyatı odur; marj/işçilik
///      UYGULANMAZ ve semantiği ürün kartındaki sabit fiyatla AYNIDIR
///      (ISCILIK_YUZDE kampanyası 0 indirim verir).
///   2. Yoksa ürün kartındaki <c>Urun.SatisFiyati</c>.
///   3. Yoksa <see cref="HasMarjYuzde"/> / <see cref="IscilikCarpanYuzde"/>
///      listenin ilgili yüzdesinin YERİNE geçer (TOPLANMAZ); boş bırakılan alan
///      listeden gelir.
///   4. Hiçbiri yoksa liste değerleri.
/// (1)'in (2)'den önce gelmesi kasıtlıdır: liste × ürün kesişimi, ürün kartındaki
/// genel sabit fiyattan daha özeldir.
///
/// ⚠ GÜMÜŞ (ayar G*): yalnız <see cref="SabitFiyat"/> dolu geçersiz kılma gümüş
/// ürünü fiyatlar (sabit fiyat has kuruna bağlı değildir — Q3 mührü, 2026-09-16).
/// Yalnız marj/işçilik taşıyan geçersiz kılma gümüşte fail-closed null kalır.
///
/// ⚠ SİLİNMEZ, PASİFE ALINIR (<see cref="Aktif"/> = false): "fiyatı kim ne zaman
/// değiştirdi" sorusunun izini silmemek için.
/// </summary>
[Table("fiyat_listesi_urun_fiyatlari")]
public class FiyatListesiUrunFiyati : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("liste_id"), MaxLength(36)] public string ListeId { get; set; } = null!;
    [Column("urun_id"), MaxLength(36)] public string UrunId { get; set; } = null!;
    /// <summary>Doluysa liste fiyatı BUDUR; marj ve işçilik uygulanmaz.</summary>
    [Column("sabit_fiyat", TypeName = "decimal(18,2)")] public decimal? SabitFiyat { get; set; }
    /// <summary>Listenin marjının YERİNE geçer (toplanmaz); boşsa listeden gelir.</summary>
    [Column("has_marj_yuzde", TypeName = "decimal(7,3)")] public decimal? HasMarjYuzde { get; set; }
    /// <summary>Listenin işçilik çarpanının YERİNE geçer; boşsa listeden gelir.</summary>
    [Column("iscilik_carpan_yuzde", TypeName = "decimal(7,2)")] public decimal? IscilikCarpanYuzde { get; set; }
    /// <summary>Gerekçe — "neden bu ürüne özel fiyat" sorusunun cevabı.</summary>
    [Column("not"), MaxLength(200)] public string? Not { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }

    /// <summary>Üç alandan en az biri dolu mu — boş geçersiz kılma anlamsızdır.</summary>
    public bool Doldurulmus => SabitFiyat is not null || HasMarjYuzde is not null || IscilikCarpanYuzde is not null;
}
