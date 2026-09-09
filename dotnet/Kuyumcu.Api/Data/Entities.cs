using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ─────────────────────────────────────────────────────────────────────────────
// Mevcut MySQL şemasına (Drizzle migration'ları) BİREBİR eşlenen varlıklar.
// Şemanın kaynağı packages/db/src/schema — yeni migration'lar oradan üretilir;
// EF Core burada yalnızca eşleme yapar (database-first davranış).
// Enum kolonları sözleşme sadakati için string taşınır; geçerli değerler
// Sabitler sınıfındadır.
// ─────────────────────────────────────────────────────────────────────────────

public static class Sabitler
{
    public static readonly string[] Planlar = ["BASLANGIC", "PRO", "KURUMSAL"];
    public static readonly string[] Roller = ["SAHIP", "MUDUR", "TEZGAHTAR", "MUHASEBE"];
    public static readonly string[] CariTurleri = ["MUSTERI", "TEDARIKCI", "HER_IKISI", "ATOLYE", "SARRAF"];
    public static readonly string[] UrunTipleri = ["ZIYNET", "KULCE", "HURDA", "SIKKE", "PIRLANTA", "SAAT", "GUMUS"];
    public static readonly string[] HareketTurleri =
        ["ALIS", "SATIS", "TAKAS", "HURDA_ALIS", "EMANET_GIRIS", "EMANET_CIKIS",
         "VERESIYE", "TAHSILAT", "TRANSFER", "SAYIM_DUZELTME", "ISCILIK"];
    public static readonly string[] ParaBirimleri = ["TRY", "USD", "EUR", "GBP"];
    public static readonly string[] OdemeTurleri = ["NAKIT", "KART", "HAVALE", "ALTIN", "DIGER"];
    public static readonly string[] KurKaynaklari = ["HAREM", "KAPALICARSI", "IAB", "MANUAL"];
    public static readonly string[] BankaYonleri = ["GELEN", "GIDEN"];
    public static readonly string[] BankaDurumlari = ["BEKLIYOR", "ESLESTI", "FATURALANDI"];
    public static readonly string[] FaturaTurleri = ["SATIS", "ALIS"];
    public static readonly string[] FaturaDurumlari = ["TASLAK", "KESILDI", "ODENDI", "IPTAL"];
}

[Table("tenants")]
public class Tenant
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("ad"), MaxLength(200)] public string Ad { get; set; } = null!;
    [Column("plan"), MaxLength(20)] public string Plan { get; set; } = "BASLANGIC";
    [Column("ai_etkin")] public bool AiEtkin { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("subeler")]
public class Sube
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("ad"), MaxLength(200)] public string Ad { get; set; } = null!;
    [Column("adres"), MaxLength(500)] public string? Adres { get; set; }
    [Column("telefon"), MaxLength(20)] public string? Telefon { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("kullanicilar")]
public class Kullanici
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string? SubeId { get; set; }
    [Column("ad"), MaxLength(100)] public string Ad { get; set; } = null!;
    [Column("soyad"), MaxLength(100)] public string Soyad { get; set; } = null!;
    [Column("email"), MaxLength(254)] public string Email { get; set; } = null!;
    [Column("sifre_hash"), MaxLength(255)] public string SifreHash { get; set; } = null!;
    [Column("rol"), MaxLength(20)] public string Rol { get; set; } = "TEZGAHTAR";
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("son_giris")] public DateTime? SonGiris { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("refresh_tokenler")]
public class RefreshToken
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("token_hash"), MaxLength(64)] public string TokenHash { get; set; } = null!;
    [Column("ip_adresi"), MaxLength(45)] public string? IpAdresi { get; set; }
    [Column("user_agent"), MaxLength(500)] public string? UserAgent { get; set; }
    [Column("gecerlilik")] public DateTime Gecerlilik { get; set; }
    [Column("iptal_edildi")] public bool IptalEdildi { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

[Table("kur_snapshots")]
public class KurSnapshot
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("altin_has_fiyati", TypeName = "decimal(15,4)")] public decimal AltinHasFiyati { get; set; }
    [Column("gram_altin_fiyati", TypeName = "decimal(15,4)")] public decimal? GramAltinFiyati { get; set; }
    [Column("usd_try", TypeName = "decimal(15,4)")] public decimal? UsdTry { get; set; }
    [Column("eur_try", TypeName = "decimal(15,4)")] public decimal? EurTry { get; set; }
    [Column("gbp_try", TypeName = "decimal(15,4)")] public decimal? GbpTry { get; set; }
    [Column("kaynak"), MaxLength(20)] public string Kaynak { get; set; } = "MANUAL";
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

[Table("cariler")]
public class Cari
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("tur"), MaxLength(20)] public string Tur { get; set; } = null!;
    [Column("ad"), MaxLength(200)] public string Ad { get; set; } = null!;
    [Column("telefon"), MaxLength(20)] public string? Telefon { get; set; }
    [Column("email"), MaxLength(254)] public string? Email { get; set; }
    [Column("vergi_no"), MaxLength(20)] public string? VergiNo { get; set; }
    [Column("adres"), MaxLength(500)] public string? Adres { get; set; }
    [Column("notlar"), MaxLength(1000)] public string? Notlar { get; set; }
    [Column("kredi_limiti_tl", TypeName = "decimal(15,2)")] public decimal? KrediLimitiTl { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("cari_bakiyeler")]
public class CariBakiye
{
    [Key, Column("cari_id"), MaxLength(36)] public string CariId { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("tl_bakiye", TypeName = "decimal(15,2)")] public decimal TlBakiye { get; set; }
    [Column("has_gram_bakiye", TypeName = "decimal(15,6)")] public decimal HasGramBakiye { get; set; }
    [Column("son_islem_tarihi")] public DateTime? SonIslemTarihi { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("urunler")]
public class Urun
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("tip"), MaxLength(20)] public string Tip { get; set; } = null!;
    [Column("kod"), MaxLength(100)] public string Kod { get; set; } = null!;
    [Column("ad"), MaxLength(300)] public string Ad { get; set; } = null!;
    [Column("aciklama"), MaxLength(1000)] public string? Aciklama { get; set; }
    [Column("ayar_id"), MaxLength(10)] public string AyarId { get; set; } = null!;
    [Column("milyem", TypeName = "decimal(7,2)")] public decimal Milyem { get; set; }
    [Column("gram_agirlik", TypeName = "decimal(12,4)")] public decimal GramAgirlik { get; set; }
    [Column("tas_agirlik", TypeName = "decimal(12,4)")] public decimal? TasAgirlik { get; set; }
    [Column("has_grami", TypeName = "decimal(15,6)")] public decimal HasGrami { get; set; }
    [Column("iscilik_tl", TypeName = "decimal(12,2)")] public decimal? IscilikTl { get; set; }
    /// <summary>Opsiyonel sabit satış fiyatı; boşsa satışta has×kur+işçilik hesaplanır.</summary>
    [Column("satis_fiyati", TypeName = "decimal(15,2)")] public decimal? SatisFiyati { get; set; }
    [Column("barkod"), MaxLength(50)] public string? Barkod { get; set; }
    [Column("rfid"), MaxLength(100)] public string? Rfid { get; set; }
    [Column("gorsel_url"), MaxLength(500)] public string? GorselUrl { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("stoklar")]
public class Stok
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;
    [Column("urun_id"), MaxLength(36)] public string UrunId { get; set; } = null!;
    [Column("adet", TypeName = "decimal(12,3)")] public decimal Adet { get; set; }
    [Column("toplam_has_gram", TypeName = "decimal(15,6)")] public decimal ToplamHasGram { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("hareketler")]
public class Hareket
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;
    [Column("tur"), MaxLength(20)] public string Tur { get; set; } = null!;
    [Column("tarih")] public DateTime Tarih { get; set; }
    [Column("aciklama", TypeName = "text")] public string? Aciklama { get; set; }
    /// <summary>TL bacağı — pozitif giriş, negatif çıkış.</summary>
    [Column("tl_tutari", TypeName = "decimal(15,2)")] public decimal TlTutari { get; set; }
    [Column("para_birimi"), MaxLength(5)] public string ParaBirimi { get; set; } = "TRY";
    /// <summary>Has bacağı — pozitif artış, negatif azalış.</summary>
    [Column("has_grami", TypeName = "decimal(15,6)")] public decimal HasGrami { get; set; }
    [Column("kur_snapshot_id"), MaxLength(36)] public string KurSnapshotId { get; set; } = null!;
    [Column("cari_id"), MaxLength(36)] public string? CariId { get; set; }
    [Column("urun_id"), MaxLength(36)] public string? UrunId { get; set; }
    [Column("karsi_sube_id"), MaxLength(36)] public string? KarsiSubeId { get; set; }
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;
    [Column("fatura_no"), MaxLength(50)] public string? FaturaNo { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

[Table("kasalar")]
public class Kasa
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;
    [Column("ad"), MaxLength(100)] public string Ad { get; set; } = null!;
    [Column("para_birimi"), MaxLength(5)] public string ParaBirimi { get; set; } = "TRY";
    [Column("bakiye", TypeName = "decimal(15,2)")] public decimal Bakiye { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("kasa_hareketleri")]
public class KasaHareket
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("kasa_id"), MaxLength(36)] public string KasaId { get; set; } = null!;
    [Column("hareket_id"), MaxLength(36)] public string? HareketId { get; set; }
    [Column("tutar", TypeName = "decimal(15,2)")] public decimal Tutar { get; set; }
    [Column("para_birimi"), MaxLength(5)] public string ParaBirimi { get; set; } = "TRY";
    [Column("odeme_turu"), MaxLength(10)] public string OdemeTuru { get; set; } = null!;
    [Column("komisyon_oran", TypeName = "decimal(5,4)")] public decimal? KomisyonOran { get; set; }
    [Column("blokaj_bitis")] public DateTime? BlokajBitis { get; set; }
    [Column("tarih")] public DateTime Tarih { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

[Table("banka_hesaplari")]
public class BankaHesap
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("banka_adi"), MaxLength(100)] public string BankaAdi { get; set; } = null!;
    [Column("hesap_adi"), MaxLength(100)] public string HesapAdi { get; set; } = null!;
    [Column("hesap_sahibi"), MaxLength(150)] public string? HesapSahibi { get; set; }
    [Column("iban"), MaxLength(34)] public string Iban { get; set; } = null!;
    [Column("para_birimi"), MaxLength(5)] public string ParaBirimi { get; set; } = "TRY";
    [Column("bakiye", TypeName = "decimal(15,2)")] public decimal Bakiye { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("banka_hareketleri")]
public class BankaHareket
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("banka_hesap_id"), MaxLength(36)] public string BankaHesapId { get; set; } = null!;
    [Column("yon"), MaxLength(10)] public string Yon { get; set; } = null!;
    [Column("tutar", TypeName = "decimal(15,2)")] public decimal Tutar { get; set; }
    [Column("para_birimi"), MaxLength(5)] public string ParaBirimi { get; set; } = "TRY";
    [Column("karsi_taraf_ad"), MaxLength(150)] public string? KarsiTarafAd { get; set; }
    [Column("karsi_iban"), MaxLength(34)] public string? KarsiIban { get; set; }
    [Column("aciklama", TypeName = "text")] public string? Aciklama { get; set; }
    [Column("cari_id"), MaxLength(36)] public string? CariId { get; set; }
    [Column("fatura_id"), MaxLength(36)] public string? FaturaId { get; set; }
    [Column("durum"), MaxLength(15)] public string Durum { get; set; } = "BEKLIYOR";
    [Column("tarih")] public DateTime Tarih { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

[Table("faturalar")]
public class Fatura
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("fatura_no"), MaxLength(30)] public string FaturaNo { get; set; } = null!;
    [Column("cari_id"), MaxLength(36)] public string CariId { get; set; } = null!;
    [Column("tur"), MaxLength(10)] public string Tur { get; set; } = "SATIS";
    /// <summary>STANDART: KDV tüm tutara. KUYUM: has altın düşülüp KDV yalnız işçilik+kâra.</summary>
    [Column("kdv_turu"), MaxLength(10)] public string KdvTuru { get; set; } = "STANDART";
    /// <summary>Kuyum KDV'sinde matrahtan düşülen has altın bedeli (KDV'siz kısım).</summary>
    [Column("has_deger", TypeName = "decimal(15,2)")] public decimal HasDeger { get; set; }
    [Column("ara_toplam", TypeName = "decimal(15,2)")] public decimal AraToplam { get; set; }
    [Column("kdv_oran", TypeName = "decimal(5,4)")] public decimal KdvOran { get; set; }
    [Column("kdv_tutar", TypeName = "decimal(15,2)")] public decimal KdvTutar { get; set; }
    [Column("toplam", TypeName = "decimal(15,2)")] public decimal Toplam { get; set; }
    [Column("para_birimi"), MaxLength(5)] public string ParaBirimi { get; set; } = "TRY";
    [Column("durum"), MaxLength(10)] public string Durum { get; set; } = "TASLAK";
    [Column("banka_hareket_id"), MaxLength(36)] public string? BankaHareketId { get; set; }
    [Column("aciklama", TypeName = "text")] public string? Aciklama { get; set; }
    [Column("tarih")] public DateTime Tarih { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

/// <summary>
/// Rol → modül yetki kaydı. Bir satırın varlığı = o rolün o modüle erişimi var.
/// Tenant bazında; SAHIP her zaman tam yetkilidir (bu tabloda tutulmaz).
/// </summary>
[Table("rol_yetkileri")]
public class RolYetki
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("rol"), MaxLength(20)] public string Rol { get; set; } = null!;
    [Column("modul"), MaxLength(30)] public string Modul { get; set; } = null!;
}
