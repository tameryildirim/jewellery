using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// FATURA SATIR KALEMİ — paket 1 Faz A (TB-17)
//
// NEDEN AYRI TABLO: Bu satır bir BELGE parçasıdır, defter değildir. Ürün
// kartına REFERANS DEĞİL KOPYA taşır: ayar, milyem, gram, has gram, taş ibaresi
// ve KDV matrahı FATURA ANINDAKİ hâliyle DONDURULUR. Ürün kartı sonradan
// düzeltilirse basılmış fatura DEĞİŞEMEZ (VUK md.253 saklama süresi boyunca
// belge içeriği sabit kalmalıdır).
//
// ⚠ DEFTER DEĞİLDİR: buraya yazmak HareketFabrikasi tekelini delmez;
// hareketler tablosuna tek yazma kapısı (denetim.sh "Hareketler.Add = 1")
// bu tablodan etkilenmez.
//
// DEĞİŞMEZ: satir_toplam = round2(miktar × birim_fiyat + iscilik_tl − indirim).
// KDV hesabı OzelMatrahMotoru'ndadır (tek motor kararı — Bertan vetosu,
// paket 38 aynı sınıfı kullanır).
// ═════════════════════════════════════════════════════════════════════════════

[Table("fatura_kalemleri")]
public class FaturaKalemi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("fatura_id"), MaxLength(36)] public string FaturaId { get; set; } = null!;
    /// <summary>1'den başlayan satır sırası — belge üzerindeki görünüm sırası.</summary>
    [Column("sira")] public int Sira { get; set; }

    /// <summary>Kalemin dayandığı defter satırı (varsa). "Bu defter satırı hangi faturada" sorusu.</summary>
    [Column("hareket_id"), MaxLength(36)] public string? HareketId { get; set; }
    /// <summary>Kalemin kopyalandığı ürün kartı — İZ amaçlıdır; alanlar buradan OKUNMAZ, kopyalanmıştır.</summary>
    [Column("urun_id"), MaxLength(36)] public string? UrunId { get; set; }

    [Column("aciklama"), MaxLength(300)] public string Aciklama { get; set; } = null!;
    [Column("miktar", TypeName = "decimal(12,3)")] public decimal Miktar { get; set; }
    /// <summary>ADET · GRAM — ürün kartından kopya (A15: gramlı üründe 1 adet = 1 gram).</summary>
    [Column("birim"), MaxLength(10)] public string Birim { get; set; } = "ADET";

    [Column("ayar_id"), MaxLength(10)] public string? AyarId { get; set; }
    [Column("milyem", TypeName = "decimal(7,2)")] public decimal? Milyem { get; set; }
    [Column("gram_agirlik", TypeName = "decimal(12,4)")] public decimal? GramAgirlik { get; set; }
    /// <summary>
    /// SATIRIN TOPLAM has gramı (TEK ANLAM, 30.09): fişten yolunda defterdeki hareketin
    /// has gramı; ürün yollarında fatura anındaki ürün kartının birim has'ı × miktar.
    /// Taslakta miktar düzeltilince <see cref="BirimHasGrami"/> × yeni miktar olarak yeniden hesaplanır
    /// (PAKET CARI-BAKIYE: orantılı yeniden hesap KALDIRILDI — 6 hanede birikiyordu).
    /// </summary>
    [Column("has_grami", TypeName = "decimal(15,6)")] public decimal? HasGrami { get; set; }

    /// <summary>
    /// PAKET CARI-BAKIYE (TOPLU kuyruk 30.09 08:50) — satırın BİRİM has gramı; ürün yollarında
    /// fatura anındaki ürün kartının birim has'ı. Taslakta miktar düzeltilince toplam
    /// = Yuvarla6(birim × miktar) — eski toplamdan orantıyla TÜRETİLMEZ, böylece 3 → 1,001 → 3
    /// gidiş-dönüşü has'ı kaydırmaz. <c>null</c>: fişten kesilen (defter bağlı) kalem — adedi
    /// belgede değişmez; ya da bilinmiyor (ilk düzeltmede eski toplam ÷ eski miktar bir kez donar).
    /// Migration geri doldurması YALNIZ TASLAK faturalarda yapıldı.
    /// </summary>
    [Column("birim_has_grami", TypeName = "decimal(15,6)")] public decimal? BirimHasGrami { get; set; }

    /// <summary>
    /// Kalemin MADENİ (ALTIN · GUMUS · PLATIN · PALADYUM · CELIK) — kalem oluşurken
    /// BİR KEZ yazılır: fişten kesilen kalemde defter satırının madeni (<c>Hareket.Maden</c>),
    /// ürün kartından eklenen kalemde ürünün madeni. Özel matrah (KDVK 23) kararı
    /// oluşturmada, taslak düzenlemede, başlık has gramında ve ekran notunda YALNIZ
    /// buradan okunur (ZO-BELGE, orkestratör 30.09 05:50 — "madenin tek kaynağı").
    /// <para>
    /// NULL = madeni BİLİNMİYOR → özel matrah UYGULANMAZ (fail-closed, KDV tam);
    /// ayar yok diye ALTIN VARSAYILMAZ (önceki <c>AyarId ?? ALTIN</c> kuralı ürünsüz
    /// gümüş hurda satırını altın sanıp düzenlemeyi 400'e düşürüyordu).
    /// </para>
    /// </summary>
    [Column("maden"), MaxLength(10)] public string? Maden { get; set; }

    /// <summary>
    /// Kuyum Ticareti Yönetmeliği md.11/(e) ibaresi — METİN olarak kopyalanır.
    /// Referans verilseydi ürün kartı düzeltilince geçmiş fatura da değişirdi.
    /// </summary>
    [Column("tas_ibaresi"), MaxLength(60)] public string? TasIbaresi { get; set; }

    [Column("birim_fiyat", TypeName = "decimal(15,2)")] public decimal BirimFiyat { get; set; }
    [Column("iscilik_tl", TypeName = "decimal(12,2)")] public decimal? IscilikTl { get; set; }
    [Column("indirim_tutari", TypeName = "decimal(15,2)")] public decimal? IndirimTutari { get; set; }

    /// <summary>STANDART · KUYUM — fatura başlığından kopyalanır (kalem bazında saklanır ki belge kendi kendini anlatsın).</summary>
    [Column("kdv_turu"), MaxLength(10)] public string KdvTuru { get; set; } = "STANDART";
    [Column("kdv_oran", TypeName = "decimal(5,4)")] public decimal KdvOran { get; set; }

    /// <summary>
    /// KDVK md.23/(e) uyarınca matrah DIŞINDA bırakılan has altın bedeli
    /// (miktar × has_grami × kur kaydındaki altin_has_fiyati). ⚠ Hükmün birincil
    /// metni DOGRULANMALI olarak işaretlidir; bu paket hesabın YERİNİ değiştirir
    /// (elle giriş → sunucuda türetme), kuralın kendisini değil. KUYUM bilinçli
    /// seçimdir, varsayılan STANDART'tır.
    /// </summary>
    [Column("has_deger", TypeName = "decimal(15,2)")] public decimal HasDeger { get; set; }

    /// <summary>
    /// Has bedelinin DAYANAĞI: kalem oluşurken özel matrah motorunun (<c>OzelMatrahMotoru</c>) kullandığı
    /// KUR KAYDINDAKİ altın has fiyatı (TL / has g) — fişten yolunda hareketin kur kaydı,
    /// ürün yollarında kiracının o anki son kur kaydı. Taslakta satır düzeltilince has
    /// bedeli = round2(yeni has gram × bu fiyat); yuvarlanmış bedelden GERİ ÇÖZÜLMEZ
    /// (ZO-BELGE, orkestratör 30.09 07:50). NULL = dayanak bilinmiyor → düzeltmede özel
    /// matrah UYGULANMAZ, KDV tam (fail-closed; sessiz varsayım yok).
    /// </summary>
    [Column("has_birim_fiyat", TypeName = "decimal(15,6)")] public decimal? HasBirimFiyat { get; set; }
    [Column("kdv_matrah", TypeName = "decimal(15,2)")] public decimal KdvMatrah { get; set; }
    [Column("kdv_tutar", TypeName = "decimal(15,2)")] public decimal KdvTutar { get; set; }
    /// <summary>KDV DAHİL satır toplamı.</summary>
    [Column("satir_toplam", TypeName = "decimal(15,2)")] public decimal SatirToplam { get; set; }

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}
