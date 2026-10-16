using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-23 — ÖDEME KALEMİ TABLOSU (R-17), FAZ 1: İLERİYE DÖNÜK YAZIM
//
// NE PROBLEMİ ÇÖZÜYOR
// ───────────────────
// Bugün bir fişin ödeme kırılımı ÜÇ AYRI YERE dağılmış durumda ve hiçbiri
// tek başına "bu fiş neyle ödendi" sorusunu cevaplamıyor:
//   • hareketler.odeme_turu   → karma fişte yalnız "KARMA" ÖZET etiketi
//     (SatisModulu.cs:1450 · SatisRaporlari.cs:158 bu yüzden o kolonu kullanmaz)
//   • hareketler.nakit_tutar / veresiye_tutar → yalnız İKİ araç; kart, havale
//     ve döviz payları buradan OKUNAMAZ
//   • kasa_hareketleri        → VERESIYE bacağının kasa satırı YOKTUR, dolayısıyla
//     kırılım kasa defterinden de tamamlanamaz
// Sonuç: "20.000 nakit + 20.000 kart" fişinin araç kırılımı ancak üç tabloyu
// birleştirip ÇIKARIM yaparak elde ediliyor. Çıkarım varsayımdır (Anayasa md.1).
// Bu tablo, kırılımı ÇIKARIM DEĞİL KAYIT hâline getirir.
//
// ⚠ FAZ 1 SINIRI — GERİ DOLDURMA YOK (plan § 11 BK-03)
// ────────────────────────────────────────────────────
// Plan, mevcut KARMA satırların geri doldurulmasını ÜÇ ŞARTA bağlamıştır:
// (i) salt okunur etki raporu, (ii) koşturulmuş Down() tatbikat çıktısı,
// (iii) öncesi/sonrası tam DB yedeği + geri dönüş provası kaydı. Bu faz
// yalnız BOŞ tabloyu doğurur ve BUGÜNDEN SONRAKİ fişleri yazar; tek bir
// geçmiş satıra dokunmaz. Geçmiş fişlerin kalemi YOKTUR ve bu "bilinmiyor"
// demektir — tahmin edilmiş bir kalem yazmaktansa hiç yazmamak tercih
// edilmiştir (md.1). Okuyucu taraf bunu <see cref="OdemeKalemi"/> satırı
// bulunmayan fişte ESKİ yoldan (nakit_tutar/veresiye_tutar + kasa_hareketleri)
// okuyarak karşılar; davranış birebir korunur.
//
// ⚠ ESKİ ALANLAR KORUNUR: nakit_tutar, veresiye_tutar ve odeme_turu
//   KALDIRILMADI. TevsikKapisi (459 s. VUK kümülatif nakit) ve MASAK kümülatifi
//   bu fazda ESKİ kaynaklarından okumaya DEVAM EDER; yeni tablo onların yanında
//   ikinci bir kayıt olarak durur. Kaynak değişimi ancak geri doldurma
//   koştuktan sonra yapılabilir — yoksa geçmişi olmayan tablo kümülatifi
//   sıfırlar ve had sessizce delinir.
//
// ⚠ SİLİNMEZ (finansal varlık): hard DELETE ucu YOKTUR. Storno, kalemi
//   <see cref="Aktif"/> = false yapar ve hükümsüz kılan hareketi
//   <see cref="StornoHareketId"/> içinde saklar (ADR-0004 append-only).
//
// ⚠ 1.1 TAKSİT PLANI (Y-24) İÇİN YER: <see cref="TaksitAdedi"/> ve
//   <see cref="PosTahsilatId"/> şimdiden buradadır; taksit planı tablosu
//   eklendiğinde bu satıra FK ile bağlanır, şema değişikliği gerekmez.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Ödeme kaleminin aracı — kapalı liste, sihirli dize yazılmaz.</summary>
public static class OdemeAraci
{
    public const string Nakit = "NAKIT";
    public const string Kart = "KART";
    public const string Havale = "HAVALE";
    public const string Veresiye = "VERESIYE";
    public const string Doviz = "DOVIZ";

    /// <summary>
    /// ⚠ BU BİR ARAÇ DEĞİL, DAMGADIR (P-23 FAZ 2 geri doldurma).
    /// Geçmiş bir payın aracı mevcut kayıtlardan OKUNAMADIĞINDA yazılır ve
    /// satır <see cref="OdemeKalemi.Bilinmiyor"/> = true olur. Tahmin edilmiş
    /// bir araç yazmaktansa "bilmiyorum" yazılır (Anayasa md.1).
    /// <see cref="Tumu"/> içinde YOKTUR: hiçbir canlı fiş bu araçla yazılamaz,
    /// <see cref="Gecerli"/> bunu reddeder.
    /// </summary>
    public const string Bilinmiyor = "BILINMIYOR";

    /// <summary>
    /// PAKET B-4 (B4-2) — eski altınla ödeme bacağı. Kalem tutarı bacağın NET
    /// TL'sidir (fişi kapatan değer); has ve brüt HURDA_ALIS hareketinde ve
    /// gider pusulasındadır. KASAYA GİRMEZ (<see cref="KasayaGirer"/>).
    /// </summary>
    public const string Altin = "ALTIN";

    /// <summary>
    /// <c>OdemeBacagiDto.Arac</c> ile AYNI liste (PAKET B-4: ALTIN ikisine
    /// BİRLİKTE eklendi — B4-1/B4-2). İki listenin ayrışması, sunucunun kabul
    /// ettiği bir bacağın kalem olarak yazılamaması demek olurdu; o fişte
    /// toplam ≠ kalem toplamı çıkardı.
    /// </summary>
    // ── R-18 MAHSUP KARARI (P-23 FAZ 2, 2026-09-22) ─────────────────────────
    // Plan § 2 P-23: "mahsup ya TÜM ödeme yollarını kapsar ya HİÇ girmez —
    // kısmi mahsup bakiyeyi saptırır, mimari VETO" (§ 11 satır 819 aynı).
    // KARAR: **HİÇ** dalı seçilmiştir ve bu bilinçli bir kapsam kararıdır.
    // GEREKÇE:
    //   • Avans mahsubu (P-21 · AVANS_MAHSUP) bir ödeme BACAĞI değil, satıştan
    //     SONRA yazılan ayrı bir defter hareketidir; fişin ödeme sepetine hiç
    //     girmez. Onu buraya bir araç olarak eklemek, satışın bacak toplamını
    //     fiş toplamının ÜSTÜNE çıkarır (fiş zaten tam ödenmiş sayılır) ve
    //     FIS_TOPLAM_ODEME_KALEMI_FARKI kapısını kırardı.
    //   • "Tümü" dalı, mahsubun tahsilat · vezne · takas · emanet · işçilik ve
    //     P-21 değişimi yollarının HEPSİNDE kalem üretmesini gerektirir; o
    //     yollar bugün hiç kalem yazmıyor (denetim.sh: yeni yazma yolu = 0).
    //     Yarısını yapmak plan gereği MİMARİ VETO sebebidir.
    // SONUÇ: MAHSUP bir OdemeAraci DEĞİLDİR ve bu liste onu İÇERMEZ.
    // Kural testle tutuluyor: `R18_Mahsup_Odeme_Araci_Olarak_Girmez`.
    public static readonly string[] Tumu = [Nakit, Kart, Havale, Veresiye, Doviz, Altin];

    public static bool Gecerli(string? arac) => arac is not null && Tumu.Contains(arac);

    /// <summary>
    /// Kasaya para giren araçlar — VERESIYE'nin kasa satırı yoktur. PAKET B-4:
    /// ALTIN'ın da yoktur; eski altın kasaya TL olarak GİRMEZ (B4-19).
    /// </summary>
    public static bool KasayaGirer(string arac) => arac != Veresiye && arac != Altin;
}

// ═════════════════════════════════════════════════════════════════════════════
// P-23 FAZ 2 — KAPSAM BAYRAĞI (hasım doğrulayıcı bulgusu, 2026-09-22 · ORTA)
//
// BULGU: "kalemi olmayan fiş = BİLİNMİYOR" kuralı yalnız kaynak YORUMUNDA
// duruyordu. Okuyan taraf (ekran, rapor, dış tüketici) boş `odemeKalemleri`
// dizisini pekâlâ "bu fişte ödeme yok" diye okuyabilirdi — bu, çıkarımı
// kayıt sanmaktır ve Anayasa md.1 ihlalidir.
// ÇÖZÜM: yanıt, kırılımın NE KADAR bilindiğini AÇIK bir alanla söyler.
// Yorum kural taşımaz; kod taşır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir fişin ödeme kırılımının ne kadar KAYITLI olduğunu söyleyen kapalı liste.</summary>
public static class OdemeKalemKapsami
{
    /// <summary>Kırılım tam kayıtlı — her bacak ölçülmüş bir kalem satırıdır.</summary>
    public const string Kayitli = "KAYITLI";

    /// <summary>
    /// Fişin HİÇ kalemi yok. Bu "ödeme yok" DEĞİL, "kırılım bilinmiyor" demektir:
    /// fiş, ödeme kalemi tablosu doğmadan önce yazılmıştır ya da geri doldurma
    /// o satıra dokunmamıştır. Okuyan taraf ESKİ yoldan (nakit_tutar /
    /// veresiye_tutar + kasa_hareketleri) okumalıdır.
    /// </summary>
    public const string Bilinmiyor = "BILINMIYOR";

    /// <summary>
    /// Kalem var ama en az biri geri doldurmada ÖLÇÜLEMEMİŞ olarak işaretli
    /// (<see cref="OdemeKalemi.Bilinmiyor"/> = true). Tutar tahmin değildir,
    /// "bu payın aracı bilinmiyor" damgasıdır.
    /// </summary>
    public const string KismenBilinmiyor = "KISMEN_BILINMIYOR";

    public static readonly string[] Tumu = [Kayitli, Bilinmiyor, KismenBilinmiyor];

    /// <summary>Kalem sayısı ve "bilinmiyor" damgalı kalem sayısından kapsamı türetir.</summary>
    public static string Belirle(int kalemSayisi, int bilinmiyorSayisi)
    {
        if (kalemSayisi <= 0) return Bilinmiyor;
        return bilinmiyorSayisi > 0 ? KismenBilinmiyor : Kayitli;
    }
}

/// <summary>
/// Bir fişin TEK ödeme bacağının KALICI kaydı — "30.000'in 12.000'i nakit".
/// </summary>
[Table("odeme_kalemleri")]
public class OdemeKalemi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary>
    /// Fişin kimliği — <c>hareketler.fis_id</c> ile AYNI değer.
    /// ⚠ <c>fisler</c> diye ayrı bir tablo YOKTUR (Entities.cs FisId gerekçesi);
    /// fiş, aynı fis_id'yi taşıyan satır kümesidir. FK tanımlanmaz.
    /// </summary>
    [Column("fis_id"), MaxLength(36)] public string FisId { get; set; } = null!;

    /// <summary>
    /// Fişin İLK hareketi — MASAK kaydı ve POS defteri de bu hareketi
    /// referans alır (SatisModulu: <c>ilkHareketId</c>). Kalem, deftere
    /// yazan satırla bu bağ üzerinden buluşur.
    /// </summary>
    [Column("hareket_id"), MaxLength(36)] public string HareketId { get; set; } = null!;

    /// <summary>Bacağın sırası (0'dan) — kullanıcının girdiği sıra KORUNUR.</summary>
    [Column("sira")] public int Sira { get; set; }

    /// <summary>Kapalı liste — <see cref="OdemeAraci"/>.</summary>
    [Column("arac"), MaxLength(10)] public string Arac { get; set; } = null!;

    /// <summary>
    /// Bacağın TL karşılığı — İŞARETSİZ (≥ 0). Döviz bacağında bu değer
    /// SUNUCUNUN kur kaydından hesapladığı TL'dir, istemcinin gönderdiği değil.
    /// </summary>
    [Column("tutar_tl", TypeName = "decimal(15,2)")] public decimal TutarTl { get; set; }

    /// <summary>
    /// Bacağın para birimi. TL bacaklarında "TRY"; döviz bacağında USD/EUR/GBP.
    /// ⚠ UX-S32: tutar para birimiyle BİRLİKTE taşınır, "varsayılan TL" sayılmaz.
    /// </summary>
    [Column("para_birimi"), MaxLength(5)] public string ParaBirimi { get; set; } = "TRY";

    /// <summary>
    /// YALNIZ döviz bacağı: kasaya giren EFEKTİF miktar (döviz cinsinden).
    /// TL karşılığı <see cref="TutarTl"/>'dedir; ikisi birlikte kuru türetir.
    /// </summary>
    [Column("doviz_tutar", TypeName = "decimal(15,2)")] public decimal? DovizTutar { get; set; }

    /// <summary>Bacağın kasası. VERESIYE bacağında BOŞ (kasa satırı doğmaz).</summary>
    [Column("kasa_id"), MaxLength(36)] public string? KasaId { get; set; }

    /// <summary>Bacağın yazdığı kasa hareketi — kasa defterine geri iz.</summary>
    [Column("kasa_hareket_id"), MaxLength(36)] public string? KasaHareketId { get; set; }

    /// <summary>YALNIZ kart bacağı: POS defterindeki tahsilat satırı (paket 31).</summary>
    [Column("pos_tahsilat_id"), MaxLength(36)] public string? PosTahsilatId { get; set; }

    /// <summary>Kart taksit adedi (1 = tek çekim). Y-24 taksit planının tutamağı.</summary>
    [Column("taksit_adedi")] public int? TaksitAdedi { get; set; }

    /// <summary>
    /// Bacağın dayandığı evrak (çek/senet/dekont) — bu fazda HİÇ YAZILMAZ,
    /// kolon ileri paket için açıktır ve NULL kalır. Uydurma değer konmaz.
    /// </summary>
    [Column("evrak_id"), MaxLength(36)] public string? EvrakId { get; set; }

    /// <summary>Kur fotoğrafı — para hareketi doğuran her satır taşır (Bertan değişmezi).</summary>
    [Column("kur_snapshot_id"), MaxLength(36)] public string? KurSnapshotId { get; set; }

    /// <summary>
    /// Geri doldurulmuş satırda payın GERÇEKTEN ölçülemediğini söyler.
    /// Faz 1'de yazılan hiçbir satırda true OLMAZ (kalem canlı sepetten doğar);
    /// geri doldurma fazının "BİLİNMİYOR" işareti için ayrılmıştır (plan DoD).
    /// </summary>
    [Column("bilinmiyor")] public bool Bilinmiyor { get; set; }

    /// <summary>
    /// P-23 FAZ 2: satır CANLI sepetten değil, GEÇMİŞ kayıtlardan (kasa
    /// hareketi + veresiye_tutar + odeme_turu) TÜRETİLEREK yazıldı.
    /// ⚠ "Türetildi" tahmin DEĞİLDİR: araç ve tutar mevcut kayıtlarda YAZILI
    ///   olan değerlerdir; ölçülemeyen pay <see cref="Bilinmiyor"/> ile
    ///   damgalanır. Bu bayrak geri doldurma migration'ının <c>Down()</c>
    ///   adımının de tutamağıdır: geri alma YALNIZ bu satırları siler,
    ///   canlı yazılmış tek bir kaleme dokunmaz.
    /// </summary>
    [Column("geri_dolduruldu")] public bool GeriDolduruldu { get; set; }

    /// <summary>false = storno ile hükümsüz kılındı. Satır SİLİNMEZ.</summary>
    [Column("aktif")] public bool Aktif { get; set; } = true;

    /// <summary>Kalemi hükümsüz kılan storno hareketi (paket 48 motoru).</summary>
    [Column("storno_hareket_id"), MaxLength(36)] public string? StornoHareketId { get; set; }

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}
