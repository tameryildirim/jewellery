using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-26 (Y-34 · Y-35) — TEDARİK ZİNCİRİ: satınalma siparişi + irsaliye.
//
// ⚠ BU DOSYA DEFTERE YAZMAZ. Satınalma siparişi bir SÖZDÜR (henüz mal yok,
//   para yok); irsaliye malın FİZİKSEL sevkidir. Deftere giren tek şey MAL
//   KABULÜDÜR ve o da kendi yolunu açmaz: mevcut `SatisServisi.Alis`
//   çağrılır (tek yazma yolu — HareketFabrikasi.Ekle oradadır). İkinci bir
//   stok/kasa/cari aritmetiği yazılsaydı aynı alışın iki farklı maliyeti
//   olurdu (ADR-0016 tek motor gerekçesinin tedarik tarafı).
//
// ⚠ HARD DELETE YOKTUR. Vazgeçilen sipariş/irsaliye IPTAL durumuna GEÇER
//   (gerekçe zorunlu); kayıt silinmez — "neden iptal edildi" sonradan sorulur.
//
// ⚠ KAPSAM SINIRI (Katman 1 = imalat/atölye KAPSAM DIŞI, K-13 mührü):
//   kalem tablosunda fire, usta payı, ayar düşürme YOKTUR.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Satınalma siparişi (PO) durumları — KAPALI liste.</summary>
public static class SatinalmaDurumu
{
    /// <summary>Henüz onaylanmamış; serbestçe düzenlenir.</summary>
    public const string Taslak = "TASLAK";

    /// <summary>Onaylanmış; mal kabul edilebilir. Kalem düzenleme kapanır.</summary>
    public const string Onaylandi = "ONAYLANDI";

    /// <summary>En az bir kalem kısmen kabul edildi.</summary>
    public const string KismiKabul = "KISMI_KABUL";

    /// <summary>Tüm kalemler tam kabul edildi.</summary>
    public const string Kapandi = "KAPANDI";

    /// <summary>Vazgeçildi (gerekçe zorunlu). Kayıt SİLİNMEZ.</summary>
    public const string Iptal = "IPTAL";

    public static readonly IReadOnlyList<string> Hepsi =
        [Taslak, Onaylandi, KismiKabul, Kapandi, Iptal];

    public static bool Gecerli(string? d) => d is not null && Hepsi.Contains(d);

    /// <summary>Mal kabul yalnız bu durumlarda yapılabilir.</summary>
    public static bool KabulEdilebilir(string d) => d is Onaylandi or KismiKabul;
}

/// <summary>İrsaliye yönü — KAPALI liste.</summary>
public static class IrsaliyeYonu
{
    /// <summary>Tedarikçiden BİZE gelen mal (alış irsaliyesi).</summary>
    public const string Gelen = "GELEN";

    /// <summary>Bizden müşteriye/atölyeye giden mal (sevk irsaliyesi).</summary>
    public const string Giden = "GIDEN";

    public static readonly IReadOnlyList<string> Hepsi = [Gelen, Giden];
    public static bool Gecerli(string? y) => y is not null && Hepsi.Contains(y);
}

/// <summary>İrsaliye durumu — KAPALI liste.</summary>
/// <summary>PAKET-42 — sevk durum makinesi TASLAK → SEVK_EDILDI → TESLIM_EDILDI (her durumdan IPTAL; IPTAL'den çıkış yok).</summary>
public static class IrsaliyeSevkDurumu
{
    public const string Taslak = "TASLAK";
    public const string SevkEdildi = "SEVK_EDILDI";
    public const string TeslimEdildi = "TESLIM_EDILDI";

    /// <summary>GELEN (mal kabul) irsaliyesinde sevk makinesi yoktur → null.</summary>
    public static string? Hesapla(Irsaliye i) => i.Yon != IrsaliyeYonu.Giden ? null
        : i.TeslimTarihi is not null ? TeslimEdildi
        : i.FiiliSevkTarihi is not null ? SevkEdildi
        : Taslak;
}

public static class IrsaliyeDurumu
{
    /// <summary>Faturası kesilmemiş. <b>Bağsız irsaliye raporunda görünür.</b></summary>
    public const string Acik = "ACIK";

    /// <summary>Bir faturaya bağlandı.</summary>
    public const string Faturalandi = "FATURALANDI";

    /// <summary>İptal (gerekçe zorunlu). Kayıt SİLİNMEZ.</summary>
    public const string Iptal = "IPTAL";

    public static readonly IReadOnlyList<string> Hepsi = [Acik, Faturalandi, Iptal];
    public static bool Gecerli(string? d) => d is not null && Hepsi.Contains(d);
}

/// <summary>
/// Satınalma siparişi başlığı (Y-34). Tedarikçiye verilen mal ısmarlama sözü.
/// <para>SİLİNMEZ — <see cref="SatinalmaDurumu.Iptal"/>'e geçirilir.</para>
/// </summary>
[Table("satinalma_siparisleri")]
public class SatinalmaSiparisi : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary>Malın gireceği şube. NULL OLAMAZ: mal kabul şubeye yazar.</summary>
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;

    /// <summary>Kiracı içinde benzersiz sipariş numarası (SAS-yyyy-nnnnn).</summary>
    [Column("no"), MaxLength(30)] public string No { get; set; } = null!;

    /// <summary>Tedarikçi cari. Mal kabulde alış bu cariye borçlandırılır.</summary>
    [Column("tedarikci_cari_id"), MaxLength(36)] public string TedarikciCariId { get; set; } = null!;

    [Column("durum"), MaxLength(20)] public string Durum { get; set; } = SatinalmaDurumu.Taslak;

    [Column("beklenen_tarih")] public DateOnly? BeklenenTarih { get; set; }

    [Column("aciklama"), MaxLength(500)] public string? Aciklama { get; set; }

    /// <summary>
    /// PAKET P-26 (Y-37) — muhasebe hesap kodu. <b>BOŞSA UYDURULMAZ</b>:
    /// muhasebe fişi o satırın hesap kodu sütununu BOŞ basar (DoD).
    /// </summary>
    [Column("hesap_kodu"), MaxLength(30)] public string? HesapKodu { get; set; }

    [Column("olusturan_kullanici_id"), MaxLength(36)] public string OlusturanKullaniciId { get; set; } = null!;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }

    /// <summary>İYİMSER KİLİT damgası — eskimiş damga gönderilirse 409.</summary>
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }

    [Column("iptal_gerekce"), MaxLength(500)] public string? IptalGerekce { get; set; }

    /// <summary>Liste ucunun azami satır sayısı (sınırsız ToListAsync yasak).</summary>
    public const int AzamiLimit = 200;
    public const int VarsayilanLimit = 50;

    /// <summary>İptal gerekçesinin asgari uzunluğu.</summary>
    public const int GerekceAsgari = 10;

    /// <summary>
    /// Bir siparişin azami kalem sayısı. SINIRSIZ DÖKÜM YASAK (B-8): kalem
    /// listesi sorgusu bu sınırla okunur, sınır girişte de doğrulanır — yoksa
    /// sorgu sessizce kırpar ve kullanıcı eksik kalem görürdü.
    /// </summary>
    public const int AzamiKalem = 200;
}

/// <summary>Satınalma siparişi kalemi (Y-34).</summary>
[Table("satinalma_kalemleri")]
public class SatinalmaKalemi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("satinalma_siparis_id"), MaxLength(36)] public string SatinalmaSiparisId { get; set; } = null!;

    /// <summary>Stokta tanımlı ürün. Mal kabul için ZORUNLUDUR (alış ürüne yazar).</summary>
    [Column("urun_id"), MaxLength(36)] public string? UrunId { get; set; }

    /// <summary>Ürün henüz tanımlı değilse serbest tanım.</summary>
    [Column("tanim"), MaxLength(200)] public string? Tanim { get; set; }

    [Column("adet", TypeName = "decimal(12,3)")] public decimal Adet { get; set; } = 1m;

    /// <summary>Anlaşılan birim fiyat (TL). Mal kabulde varsayılan olarak önerilir.</summary>
    [Column("birim_fiyat_tl", TypeName = "decimal(15,2)")] public decimal? BirimFiyatTl { get; set; }

    /// <summary>
    /// Bugüne dek kabul edilen adet. <b>Yalnız mal kabul ucu artırır</b>; elle
    /// yazılamaz. <see cref="Adet"/>'i aşamaz (fazla mal ayrı siparişle girer).
    /// </summary>
    [Column("kabul_edilen_adet", TypeName = "decimal(12,3)")] public decimal KabulEdilenAdet { get; set; }

    [Column("sira")] public int Sira { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

/// <summary>
/// İrsaliye (Y-35) — malın fiziksel sevk belgesi.
/// <para>Faturaya bağlanır; bağlanmayanlar "bağsız irsaliye" raporunda görünür.</para>
/// </summary>
[Table("irsaliyeler")]
public class Irsaliye : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;

    /// <summary>Kiracı içinde benzersiz irsaliye numarası (IRS-yyyy-nnnnn).</summary>
    [Column("no"), MaxLength(30)] public string No { get; set; } = null!;

    /// <summary><see cref="IrsaliyeYonu"/>.</summary>
    [Column("yon"), MaxLength(10)] public string Yon { get; set; } = null!;

    [Column("cari_id"), MaxLength(36)] public string CariId { get; set; } = null!;
    [Column("tarih")] public DateTime Tarih { get; set; }

    /// <summary>GELEN irsaliye bir satınalma siparişinin mal kabulünden doğduysa.</summary>
    [Column("satinalma_siparis_id"), MaxLength(36)] public string? SatinalmaSiparisId { get; set; }

    /// <summary>
    /// Bağlandığı fatura. NULL = <b>BAĞSIZ</b> — rapor bunu gösterir
    /// (DoD: "bağsız irsaliye raporda görünür").
    /// </summary>
    [Column("fatura_id"), MaxLength(36)] public string? FaturaId { get; set; }

    [Column("durum"), MaxLength(20)] public string Durum { get; set; } = IrsaliyeDurumu.Acik;

    [Column("aciklama"), MaxLength(500)] public string? Aciklama { get; set; }

    /// <summary>PAKET P-26 (Y-37) — muhasebe hesap kodu; boşsa fişte BOŞ basılır.</summary>
    [Column("hesap_kodu"), MaxLength(30)] public string? HesapKodu { get; set; }

    [Column("olusturan_kullanici_id"), MaxLength(36)] public string OlusturanKullaniciId { get; set; } = null!;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
    [Column("iptal_gerekce"), MaxLength(500)] public string? IptalGerekce { get; set; }

    // ── PAKET-42 (20.10) — VUK sevk alanları: taşıyan, plaka, fiili sevk zamanı, teslim alan ve teslim zamanı.
    //    Sevk durumu KOLON DEĞİL, bu alanlardan türetilir (IrsaliyeSevkDurumu.Hesapla): teslim_tarihi dolu →
    //    TESLIM_EDILDI, fiili_sevk_tarihi dolu → SEVK_EDILDI, ikisi de boş → TASLAK. Yalnız GIDEN irsaliyede anlamlıdır.
    [Column("tasiyan_ad"), MaxLength(100)] public string? TasiyanAd { get; set; }
    [Column("plaka"), MaxLength(20)] public string? Plaka { get; set; }
    [Column("fiili_sevk_tarihi")] public DateTime? FiiliSevkTarihi { get; set; }
    [Column("teslim_alan_ad"), MaxLength(150)] public string? TeslimAlanAd { get; set; }
    [Column("teslim_tarihi")] public DateTime? TeslimTarihi { get; set; }

    public const int AzamiLimit = 200;
    public const int VarsayilanLimit = 50;
    public const int GerekceAsgari = 10;

    /// <summary>Bir irsaliyenin azami kalem sayısı (B-8 — sınırsız döküm yasak).</summary>
    public const int AzamiKalem = 200;
}

/// <summary>İrsaliye kalemi (Y-35).</summary>
[Table("irsaliye_kalemleri")]
public class IrsaliyeKalemi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("irsaliye_id"), MaxLength(36)] public string IrsaliyeId { get; set; } = null!;

    [Column("urun_id"), MaxLength(36)] public string? UrunId { get; set; }
    [Column("tanim"), MaxLength(200)] public string? Tanim { get; set; }
    [Column("adet", TypeName = "decimal(12,3)")] public decimal Adet { get; set; } = 1m;

    /// <summary>
    /// Bu kalemin deftere düşen satırı (mal kabulde <c>SatisServisi.Alis</c>'ın
    /// ürettiği ALIS hareketi). NULL = irsaliye deftere DOKUNMADI (sevk).
    /// </summary>
    [Column("hareket_id"), MaxLength(36)] public string? HareketId { get; set; }

    [Column("sira")] public int Sira { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}
