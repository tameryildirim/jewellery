using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// VERİ İÇE AKTARIMI — paket 47 Faz A (kapılı, kuru provalı)
//
// ⚠ NEDEN İKİ TABLO VAR (dışa aktarımın AYNASI DEĞİL, TERSİ):
//   Dışa aktarımın riski SIZDIRMA'dır; içe aktarımın riski ZEHİRLEME'dir
//   (uydurma cari, sahte stok). Bu yüzden burada "talep + indir" değil,
//   "OTURUM + SATIR" modeli var: hangi dosyadan hangi satırın hangi karta
//   dönüştüğü satır satır kayıtlıdır ve kuru prova (durum=PROVA) hiçbir
//   hedef tabloya dokunmadan önce bu kaydı üretir.
//
// SAKLANMAYANLAR (bilinçli — ADR-0014 "Saklanmayanlar" simetrisi):
//   • Yüklenen ham CSV DOSYASI diske YAZILMAZ. Yalnız satırların ayrıştırılmış
//     hâli ham_json'da durur.
//   • ham_json'daki KİMLİK alanları (vergi no / TCKN / telefon) MASKELENEREK
//     yazılır (Mehmet'in koşulu, ADR-0003 çelişkisi): sahne tablosu düz metin
//     kimlik tutamaz. Ham değer yalnız istek ömründe bellekte kalır ve
//     doğrudan servis DTO'suna geçer.
//
// FAZ A KAPSAMI: kaynak ∈ {cari, urun}. Açılış bakiyesi / açılış stoku
// (Faz B · paket T-2) bu pakette YOKTUR — bu yüzden bu iki tablo deftere
// (hareketler / kasa_hareketleri) HİÇ dokunmaz.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>İçe aktarım oturumunun yaşam döngüsü.</summary>
public static class IceAktarimDurumu
{
    /// <summary>Kuru prova yapıldı; hedef tablolara HİÇBİR ŞEY yazılmadı.</summary>
    public const string Prova = "PROVA";

    /// <summary>Geçerli satırlar uygulandı.</summary>
    public const string Uygulandi = "UYGULANDI";

    /// <summary>Uygulanan kartlar pasife alındı (soft delete).</summary>
    public const string GeriAlindi = "GERI_ALINDI";

    /// <summary>Kullanıcı vazgeçti ya da uygulama sırasında tümü geri alındı.</summary>
    public const string Iptal = "IPTAL";

    public static readonly string[] Tumu = [Prova, Uygulandi, GeriAlindi, Iptal];
}

/// <summary>
/// Yinelenen kayıt politikası.
/// <para>
/// ⚠ GUNCELLE FAZ A'DA YOKTUR (Bertan + Mehmet ortak koşulu): mevcut kartı
/// PATCH etmek geri almayı imkânsızlaştırır ve ekranda anlaşılmazsa veri
/// kaybı hissi üretir. İhtiyaç kanıtlanırsa Faz B'de açılır.
/// </para>
/// </summary>
public static class YinelenenPolitikasi
{
    /// <summary>Fail-closed VARSAYILAN: eşleşen satır HATA'dır.</summary>
    public const string Hata = "HATA";

    /// <summary>Eşleşen satır sayılır ve geçilir (yazılmaz).</summary>
    public const string Atla = "ATLA";

    public static readonly string[] Tumu = [Hata, Atla];
}

/// <summary>Satır sonucu.</summary>
public static class IceAktarimSatirSonucu
{
    public const string Gecerli = "GECERLI";
    public const string Hata = "HATA";
    public const string Atlandi = "ATLANDI";
    public const string Uygulandi = "UYGULANDI";
}

[Table("ice_aktarim_oturumlari")]
public class IceAktarimOturumu : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;

    /// <summary>Faz A: 'cari' | 'urun'.</summary>
    [Column("kaynak_ad"), MaxLength(30)] public string KaynakAd { get; set; } = null!;

    [Column("dosya_adi"), MaxLength(255)] public string DosyaAdi { get; set; } = null!;

    /// <summary>Yüklenen içeriğin SHA-256'sı — aynı dosya iki kez mi geldi?</summary>
    [Column("dosya_sha256"), MaxLength(64)] public string DosyaSha256 { get; set; } = null!;

    /// <summary>Dışa aktarımla simetrik: en az 20 karakter, denetim izine yazılır.</summary>
    [Column("gerekce"), MaxLength(500)] public string Gerekce { get; set; } = null!;

    [Column("durum"), MaxLength(20)] public string Durum { get; set; } = IceAktarimDurumu.Prova;

    [Column("yinelenen_politikasi"), MaxLength(10)]
    public string YinelenenPolitikasi { get; set; } = Data.YinelenenPolitikasi.Hata;

    [Column("toplam_satir")] public int ToplamSatir { get; set; }
    [Column("gecerli_satir")] public int GecerliSatir { get; set; }
    [Column("hatali_satir")] public int HataliSatir { get; set; }
    [Column("atlanan_satir")] public int AtlananSatir { get; set; }
    [Column("uygulanan_satir")] public int UygulananSatir { get; set; }

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("uygulandi")] public DateTime? Uygulandi { get; set; }
    [Column("geri_alindi")] public DateTime? GeriAlindi { get; set; }
    [Column("ip_adresi"), MaxLength(45)] public string? IpAdresi { get; set; }
}

[Table("ice_aktarim_satirlari")]
public class IceAktarimSatiri : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("oturum_id"), MaxLength(36)] public string OturumId { get; set; } = null!;

    /// <summary>Dosyadaki satır numarası (başlık satırı 1'dir; veri 2'den başlar).</summary>
    [Column("satir_no")] public int SatirNo { get; set; }

    /// <summary>
    /// Dosyadaki alanların ayrıştırılmış hâli (JSON).
    /// <para>
    /// ⚠ DİSKTE AES-256-GCM İLE ŞİFRELİDİR (ADR-0003, Mehmet'in koşulu:
    /// "ya maskelenerek yazılır ya ham_json AES şifreli"). Şifreleme
    /// <c>KuyumcuDbContext.OnModelCreating</c>'deki değer dönüştürücüyle
    /// kurulur; bellekte düz metin, diskte şifreli görünür.
    /// </para>
    /// <para>
    /// ⚠ MASKELEME OKUMA ANINDA YAPILIR: satır listesi ucu kimlik alanlarını
    /// (telefon / vergi no / adres / e-posta) maskeleyerek döndürür. Düz değer
    /// yalnız UYGULAMA adımında, servis DTO'suna geçmek için çözülür.
    /// </para>
    /// </summary>
    [Column("ham_json", TypeName = "text")] public string HamJson { get; set; } = null!;

    [Column("sonuc"), MaxLength(10)] public string Sonuc { get; set; } = null!;
    [Column("hata_kodu"), MaxLength(40)] public string? HataKodu { get; set; }
    [Column("hata_mesaji"), MaxLength(500)] public string? HataMesaji { get; set; }
    [Column("hedef_varlik_turu"), MaxLength(20)] public string? HedefVarlikTuru { get; set; }
    [Column("hedef_varlik_id"), MaxLength(36)] public string? HedefVarlikId { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}
