using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

/// <summary>
/// D2-05b (19.10, kurucu: "bankaları önceden bağlayalım") — bir banka hesabının bankanın hesap hareketi
/// servisine bağlantısı. Bankanın İŞLETMEYE verdiği servis kimliği (kullanıcı/şifre, müşteri no, client
/// id/secret, consentId…) tek JSON olarak AES-GCM ile ŞİFRELİ saklanır; ekrana bir daha dönmez (yalnız
/// "kayıtlı" + gizli olmayan alanların özeti). İnternet şubesi parolası istenmez.
/// </summary>
[Table("banka_baglantilari")]
public class BankaBaglantisi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("banka_hesap_id"), MaxLength(36)] public string BankaHesapId { get; set; } = null!;
    /// <summary>Bağlayıcı kodu (<see cref="Kuyumcu.Core.Domain.BankaBaglayiciKatalogu"/>).</summary>
    [Column("baglayici_kodu"), MaxLength(20)] public string BaglayiciKodu { get; set; } = null!;
    /// <summary>Tüm alanlar (gizli + açık) JSON, <see cref="Common.IAlanSifreleyici"/> ile şifreli.</summary>
    [Column("kimlik_sifreli"), MaxLength(4000)] public string KimlikSifreli { get; set; } = null!;
    /// <summary>Gizli olmayan alanların ekranda gösterilen özeti (ör. "Kullanıcı: AB***").</summary>
    [Column("ozet"), MaxLength(300)] public string? Ozet { get; set; }
    [Column("son_deneme")] public DateTime? SonDeneme { get; set; }
    [Column("son_basari")] public DateTime? SonBasari { get; set; }
    /// <summary>Son denemenin sade dildeki hata sebebi (başarılıysa null).</summary>
    [Column("son_hata"), MaxLength(300)] public string? SonHata { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}
