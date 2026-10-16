using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// GİDER PUSULASI — PLAN Adım 6. VUK md.234.
//
// Kuyumcu, vergi mükellefi OLMAYAN kişiden hurda altın aldığında fatura alamaz;
// gider pusulası düzenlemek ZORUNDADIR. Bu MASAK'tan bağımsız, ondan eski bir
// VERGİ yükümlülüğüdür ve TUTAR EŞİĞİ YOKTUR — 1.000 TL'lik hurda alımında da
// gerekir. (MASAK kimlik eşiği işlem gününün yasal_parametreler değeridir — 07.10.2026'dan itibaren 370.000 TL; ikisi karıştırılmamalı.)
//
// STOPAJ — satıcının vergi durumuna göre DEĞİŞİR (doğrulandı, §Kaynaklar):
//   • NIHAI_TUKETICI  (vergi mükellefi değil, zati eşyasını satıyor) → tevkifat YOK.
//     GVK md.94'te bu kişilerden hurda alımı için tevkifat hükmü yoktur.
//   • ESNAF_MUAF      (esnaf muaflığından yararlanan)               → %2 stopaj
//     (hurda mal alımlarında GVK md.94 kapsamında).
//   • MUKELLEF        (vergi mükellefi)  → gider pusulası DÜZENLENMEZ, FATURA alınır.
//
// SÜRE: gider pusulası, malın tesliminden itibaren EN GEÇ 7 GÜN içinde düzenlenir.
//
// APPEND-ONLY: düzeltme silmez, yeni pusula ekler (DuzeltilenId).
// Satıcı TCKN'si diske ŞİFRELİ iner (MASAK kimliğiyle aynı altyapı).
//
// Kaynaklar (PAKET YASAL-4: BİRİNCİL metinlerle değiştirildi, şartname § 3.1):
//   - VUK md.234 (süre azami 7 gün; süresinde düzenlenmeyen pusula hiç düzenlenmemiş
//     sayılır): https://www.mevzuat.gov.tr/mevzuatmetin/1.4.213.pdf
//   - GVK md.94/13 (esnaf muafına mal alımı ödemesinde tevkifat), md.9/1-7 (kapı kapı
//     hurda toplayan esnaf muafı): https://www.mevzuat.gov.tr/mevzuatmetin/1.4.193.pdf
//   - 2009/14592 BKK 13. bent "Hurda mal alımları için % 2" (ilk metin; sonraki
//     değişiklik DOĞRULANAMADI): https://www.resmigazete.gov.tr/eskiler/2009/02/20090203-3.htm
//   - Nihai tüketiciden alımda tevkifat getiren Cumhurbaşkanı Kararı BULUNAMADI → %0
//     (TEYİTSİZ, mali müşavir teyidi gerekir).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Hurda satıcısının vergi durumu — stopajı ve belge türünü bu belirler.</summary>
public static class SaticiTipi
{
    /// <summary>Vergi mükellefi değil, zati eşyasını satıyor. Gider pusulası + stopaj YOK.</summary>
    public const string NihaiTuketici = "NIHAI_TUKETICI";

    /// <summary>Esnaf muaflığından yararlanıyor. Gider pusulası + %2 stopaj.</summary>
    public const string EsnafMuaf = "ESNAF_MUAF";

    /// <summary>Vergi mükellefi. Gider pusulası YOK — satıcı fatura keser.</summary>
    public const string Mukellef = "MUKELLEF";

    public static readonly IReadOnlyList<string> Hepsi = [NihaiTuketici, EsnafMuaf, Mukellef];

    public static bool Gecerli(string? t) => t is not null && Hepsi.Contains(t);

    /// <summary>Bu satıcı tipi için gider pusulası düzenlenir mi?</summary>
    public static bool PusulaGerekir(string tip) => tip is NihaiTuketici or EsnafMuaf;
}

[Table("gider_pusulalari")]
public class GiderPusulasi : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary>GP-2026-000001 — kiracı + yıl içinde tekil.</summary>
    [Column("pusula_no"), MaxLength(30)] public string PusulaNo { get; set; } = null!;

    /// <summary>Belgeye konu hurda alış hareketi.</summary>
    [Column("hareket_id"), MaxLength(36)] public string HareketId { get; set; } = null!;

    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;
    [Column("cari_id"), MaxLength(36)] public string? CariId { get; set; }

    // ── Satıcı (VUK md.234: kimlik zorunlu) ─────────────────────────────────
    [Column("satici_tipi"), MaxLength(20)] public string SaticiTipi { get; set; } = null!;
    [Column("satici_ad_soyad"), MaxLength(200)] public string SaticiAdSoyad { get; set; } = null!;

    /// <summary>TCKN — diske AES-256-GCM ŞİFRELİ iner (MASAK belge no ile aynı altyapı).</summary>
    [Column("satici_tckn"), MaxLength(200)] public string SaticiTckn { get; set; } = null!;

    /// <summary>HMAC parmak izi — "bu kişiden daha önce ne aldık" sorusu için.</summary>
    [Column("satici_tckn_hash"), MaxLength(64)] public string SaticiTcknHash { get; set; } = null!;

    [Column("sifre_surumu")] public byte SifreSurumu { get; set; }

    [Column("satici_adres"), MaxLength(500)] public string? SaticiAdres { get; set; }

    // ── Mal ──────────────────────────────────────────────────────────────────
    [Column("ayar_id"), MaxLength(10)] public string AyarId { get; set; } = null!;
    [Column("gram_agirlik", TypeName = "decimal(12,3)")] public decimal GramAgirlik { get; set; }
    [Column("has_grami", TypeName = "decimal(15,6)")] public decimal HasGrami { get; set; }
    [Column("mal_aciklamasi"), MaxLength(500)] public string? MalAciklamasi { get; set; }

    // ── Tutarlar ─────────────────────────────────────────────────────────────
    /// <summary>Stopaj öncesi brüt bedel (satıcıya tahakkuk eden).</summary>
    [Column("brut_tutar", TypeName = "decimal(15,2)")] public decimal BrutTutar { get; set; }

    [Column("stopaj_oran", TypeName = "decimal(5,4)")] public decimal StopajOran { get; set; }
    [Column("stopaj_tutar", TypeName = "decimal(15,2)")] public decimal StopajTutar { get; set; }

    /// <summary>Satıcının eline geçen net tutar = brüt − stopaj.</summary>
    [Column("net_odenen", TypeName = "decimal(15,2)")] public decimal NetOdenen { get; set; }

    // ── Tarihler ─────────────────────────────────────────────────────────────
    /// <summary>Malın teslim alındığı an.</summary>
    [Column("teslim_tarihi")] public DateTime TeslimTarihi { get; set; }

    /// <summary>
    /// VUK md.234: teslimden itibaren en geç 7 gün. PAKET YASAL-4: pusula alışla aynı anda
    /// düzenlendiği için bu tarih YALNIZ iptal edilip yerine yenisi düzenlenmeyen belgede
    /// anlam taşır (GiderPusulasiSuresi). Yeniden düzenlenen belge ORİJİNAL tarihi taşır.
    /// </summary>
    [Column("duzenleme_son_tarihi")] public DateTime DuzenlemeSonTarihi { get; set; }

    /// <summary>PAKET YASAL-4: yeniden düzenlenen belgede yerine geçtiği İPTAL edilmiş pusula.</summary>
    [Column("duzeltilen_id"), MaxLength(36)] public string? DuzeltilenId { get; set; }

    /// <summary>
    /// PAKET S-3b — VUK md.234 düzeltme yolu: yanlış düzenlenen gider pusulası
    /// SİLİNMEZ, İPTAL edilir ve doğru bilgilerle YENİ pusula düzenlenir.
    /// İptal edilen belge "İPTAL" ibaresiyle saklanır (belge zinciri kopmaz).
    /// ⚠ İptal yalnız BELGEYİ hükümsüz kılar; hurda alış hareketinin stok ve
    /// maliyet partisi bacağına DOKUNMAZ (bkz. StornoServisi.ErtelenenTurler).
    /// </summary>
    [Column("iptal_edildi")] public bool IptalEdildi { get; set; }

    /// <summary>İptal gerekçesi — en az 20 karakter, denetim izine düşer.</summary>
    [Column("iptal_gerekce"), MaxLength(1000)] public string? IptalGerekce { get; set; }

    [Column("iptal_tarihi")] public DateTime? IptalTarihi { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }

    /// <summary>
    /// PAKET YASAL-4 tur 4 (hasım SINIR2): "bir alış için EN ÇOK BİR geçerli pusula"
    /// değişmezinin VERİTABANI kilidi. Saklanan üretilmiş kolon: iptal edilmemiş belgede
    /// hareket_id, iptal edilmişte NULL; UNIQUE (tenant_id, gecerli_hareket_id).
    /// Eşzamanlı iki "yeniden düzenle" isteğinin ikisi de "geçerli yok" okuyup yazabiliyordu
    /// (stopaj iki kez sayılırdı); ikinci yazma artık 1062 ile reddedilir (→ 409).
    /// Uygulama bu alanı YAZMAZ (MySQL hesaplar).
    /// </summary>
    [Column("gecerli_hareket_id"), MaxLength(36)]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public string? GecerliHareketId { get; private set; }
}
