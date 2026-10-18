using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 19 — Vezne modeli (Faz A): tezgâhtar satar, vezne tahsil eder.
//
// Perakende kuyumcuda müşteriyle ilgilenen kişi ile parayı alan kişi AYNI
// olmak zorunda değildir. Bu üç tablo o ayrımı kayda geçirir:
//   • vezne_fisleri        — tezgâhtarın hazırladığı, HENÜZ DEFTERE GİRMEMİŞ fiş
//   • vezne_fis_kalemleri  — fişin ürün satırları
//   • kiraci_ayarlari      — kiracı düzeyinde anahtar/değer ayarı (vezne_zorunlu)
//
// ⚠ DEFTERE YAZMAZ: vezne modülü HareketFabrikasi.Ekle çağırmaz, hareketler /
//   kasa_hareketleri satırı üretmez. Para el değiştirdiği an SatisServisi.SatisFisi
//   ÇAĞRILIR; tevsik, MASAK, gün kilidi, K6, stok kapıları bugünkü TEK yoldan
//   çalışır (R10). denetim.sh § vezne kuralı bunu sayar.
//
// ⚠ SİLİNMEZ: hard DELETE ucu YOKTUR. Vazgeçilen fiş IPTAL durumuna geçirilir
//   (gerekçe ≥10 karakter) ve satır kalır — "neden iptal edildi" sonradan sorulur.
//
// ⚠ tenants tablosuna KOLON EKLENMEDİ (Bertan koşulu, mühür 2026-09-16): Tenant
//   IKiraciVarlik DEĞİLDİR; global kiracı filtresi, KiraciYazmaDenetcisi ve
//   denetim izi ona işlemez. Ayar bu yüzden IKiraciVarlik olan kiraci_ayarlari
//   tablosundadır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Vezne fişinin kapalı durum listesi — sihirli dize yazılmaz.</summary>
public static class VezneDurum
{
    /// <summary>Tezgâhtar hazırlıyor; kalemleri ve fiyatı hâlâ değişebilir.</summary>
    public const string Acik = "ACIK";

    /// <summary>Vezneye gönderildi; tutar DAMGALANDI, tezgâhtar artık değiştiremez.</summary>
    public const string Veznede = "VEZNEDE";

    /// <summary>Vezne tahsil etti; deftere yazıldı (hareket_fis_id dolu).</summary>
    public const string Tahsil = "TAHSIL";

    /// <summary>Vazgeçildi. Kayıt SİLİNMEZ.</summary>
    public const string Iptal = "IPTAL";

    public static readonly string[] Tumu = [Acik, Veznede, Tahsil, Iptal];

    /// <summary>Bu durumdaki fiş hiçbir yönde değişmez (DegismezIpucu gerekçesi).</summary>
    public static bool Terminal(string durum) => durum is Tahsil or Iptal;
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-23c (FAZ 3) — VEZNE FİŞİ BAŞLIĞININ GENELLEŞTİRİLMESİ
//
// Paket 19'da vezne fişi YALNIZ satış yolunu tanıyordu: başlık ürün kalemleri
// üzerine kuruluydu, tahsil yolu her hâlükârda SatisServisi.SatisFisi'ye
// gidiyordu. Oysa tezgâhtar ile parayı alan kişinin ayrılması SATIŞA ÖZGÜ BİR
// İHTİYAÇ DEĞİLDİR: müşteri borcunu öderken (TAHSILAT) ya da işçilik bedeli
// alınırken (ISCILIK) de aynı dört göz kuralı gerekir. Bu genelleştirme, fişe
// bir TÜR kazandırır ve tahsil yolunu türe göre dağıtır.
//
// ⚠ R10 KORUNUR: vezne HÂLÂ deftere YAZMAZ. Genelleşen şey BAŞLIKTIR; tahsil
//   anında yine mevcut servis çağrılır (SatisFisi · TahsilatGir · Iscilik) ve
//   tevsik/MASAK/gün kilidi/K6 kapıları o servislerin İÇİNDE çalışır.
//
// ⚠ TAKAS ve EMANET BİLEREK YOKTUR (kapsam kararı, gerekçesi burada durur ki
//   sonraki paket tahmin etmesin):
//   • EMANET — emanet giriş/çıkış PARA HAREKETİ DOĞURMAZ (EmanetModulu
//     OdemeIzi.Yok ile yazar). Tahsil edecek para olmayan bir işin veznesi de
//     olmaz; vezne fişi açmak kullanıcıya "burada para alınacak" der ve YANLIŞ
//     olurdu.
//   • TAKAS — takas satırı eski malın AYARINI, GRAMINI ve has birim fiyatını
//     ister (SatisModulu.Takas: EskiUrunGram · EskiUrunBirimFiyatTlHasGram ·
//     ayar). vezne_fis_kalemleri bugün yalnız (urun_id, adet, birim_fiyat,
//     işçilik) taşır; takası desteklemek kalem tablosuna dört kolon daha
//     eklemek demektir. Bu paket veri modelini o kadar genişletmedi; takas
//     ekranda DegismezIpucu ile "vezne fişi henüz takası taşımıyor, takas
//     doğrudan işlem ekranından girilir" der. UYDURMA BİR EŞLEME YAPILMADI.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Vezne fişinin türü — kapalı liste, sihirli dize yazılmaz.</summary>
public static class VezneFisTuru
{
    /// <summary>Ürün kalemli satış fişi (paket 19'un tek türü; VARSAYILAN).</summary>
    public const string Satis = "SATIS";

    /// <summary>Cari borç tahsilatı — kalemi YOKTUR, tek TL tutarı taşır.</summary>
    public const string Tahsilat = "TAHSILAT";

    /// <summary>İşçilik bedeli — kalemi YOKTUR, tek TL tutarı taşır.</summary>
    public const string Iscilik = "ISCILIK";

    public static readonly string[] Tumu = [Satis, Tahsilat, Iscilik];

    public static bool Gecerli(string? tur) => tur is not null && Tumu.Contains(tur);

    /// <summary>
    /// Fiş ÜRÜN KALEMİ taşır mı. Kalemsiz türlerde tutar tezgâhtarın girdiği
    /// <see cref="VezneFisi.TalepTutarTl"/>'dir; kalemli türde kalemlerden
    /// hesaplanır ve vezneleme anında damgalanır (M-V10).
    /// </summary>
    public static bool Kalemli(string tur) => tur == Satis;

    /// <summary>Kalemsiz türlerde cari ZORUNLUDUR: tahsilat/işçilik cariye yazılır.</summary>
    public static bool CariZorunlu(string tur) => tur is Tahsilat or Iscilik;
}

/// <summary>
/// Vezne fişi — tezgâhtarın hazırladığı, veznenin tahsil ettiği işlem başlığı.
/// </summary>
[Table("vezne_fisleri")]
public class VezneFisi : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary>Fişi açan kullanıcının şubesi. Gövdeden ALINMAZ (R6).</summary>
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;

    /// <summary>Kapalı liste — <see cref="VezneDurum"/>. ACIK doğar.</summary>
    [Column("durum"), MaxLength(10)] public string Durum { get; set; } = VezneDurum.Acik;

    /// <summary>
    /// P-23c — fişin türü, kapalı liste <see cref="VezneFisTuru"/>.
    /// Mevcut satırların hepsi SATIS'tır (migration varsayılanı); bu bir
    /// TAHMİN DEĞİL, paket 19'da tek tür bulunmasının sonucudur.
    /// </summary>
    [Column("fis_turu"), MaxLength(10)] public string FisTuru { get; set; } = VezneFisTuru.Satis;

    /// <summary>
    /// P-23c — KALEMSİZ türlerde (TAHSILAT · ISCILIK) tezgâhtarın girdiği tutar.
    /// Kalemli (SATIS) fişte NULL'dur ve doldurulması REDDEDİLİR: aynı sorunun
    /// iki cevabı olamaz (UX-S22). Vezneleme anında <see cref="ToplamTl"/>'ye
    /// damgalanır.
    /// </summary>
    [Column("talep_tutar_tl", TypeName = "decimal(15,2)")] public decimal? TalepTutarTl { get; set; }

    [Column("hazirlayan_kullanici_id"), MaxLength(36)]
    public string HazirlayanKullaniciId { get; set; } = null!;

    /// <summary>R11: deftere yazılan hareketin sahibi — kasa farkı BUNA sorulur.</summary>
    [Column("tahsil_eden_kullanici_id"), MaxLength(36)]
    public string? TahsilEdenKullaniciId { get; set; }

    [Column("cari_id"), MaxLength(36)] public string? CariId { get; set; }

    [Column("aciklama", TypeName = "text")] public string? Aciklama { get; set; }

    /// <summary>
    /// Fişin açıldığı andaki kur kaydı — REFERANSTIR. Deftere yazılan kur,
    /// TAHSİL gövdesinde yeniden gönderilen kurdur (R8): sabah açılan fiş
    /// akşam eski kurla kapanmasın.
    /// </summary>
    [Column("kur_snapshot_id"), MaxLength(36)] public string KurSnapshotId { get; set; } = null!;

    /// <summary>
    /// YALNIZ vezneleme anında damgalanır (R4/M-V10): kuyrukta görünen tutar ile
    /// tahsil edilen tutar ayrışamaz.
    /// </summary>
    [Column("toplam_tl", TypeName = "decimal(15,2)")] public decimal ToplamTl { get; set; }

    /// <summary>Tahsil sonucu doğan defter fişinin kimliği (hareketler.fis_id).</summary>
    [Column("hareket_fis_id"), MaxLength(36)] public string? HareketFisId { get; set; }

    [Column("iptal_gerekce"), MaxLength(500)] public string? IptalGerekce { get; set; }

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }

    /// <summary>
    /// İYİMSER KİLİT damgası (Mehmet koşulu). Tahsil/PATCH/vezneleme bu kolonu
    /// eşzamanlılık jetonu olarak kullanır: iki vezne aynı fişe aynı anda
    /// basarsa ikincisi 409 alır, ExecuteUpdate KULLANILMADAN.
    /// </summary>
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }

    [Column("veznelendi")] public DateTime? Veznelendi { get; set; }
    [Column("tahsil_edildi")] public DateTime? TahsilEdildi { get; set; }
    [Column("iptal_edildi")] public DateTime? IptalEdildi { get; set; }

    /// <summary>Kuyruk ucunun azami satır sayısı (TB-19 — sınırsız ToListAsync yasak).</summary>
    public const int AzamiLimit = 200;

    public const int VarsayilanLimit = 50;

    /// <summary>İptal gerekçesinin asgari uzunluğu (R5).</summary>
    public const int GerekceAsgari = 10;

    /// <summary>Bir fişteki azami kalem sayısı — gövde sınırı.</summary>
    public const int AzamiKalem = 100;
}

/// <summary>
/// Vezne fişinin ürün satırı.
///
/// ⚠ adet ölçeği stoklar.adet ile BİREBİR aynıdır (decimal(12,3), Bertan koşulu):
///   iki ölçek ayrışırsa tahsilde sessiz yuvarlama olurdu.
/// ⚠ Kalem kimliği DIŞARIDAN gelmez: PATCH kalemleri tümüyle değiştirir (sil + yaz).
/// </summary>
[Table("vezne_fis_kalemleri")]
public class VezneFisKalemi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("fis_id"), MaxLength(36)] public string FisId { get; set; } = null!;
    [Column("urun_id"), MaxLength(36)] public string UrunId { get; set; } = null!;
    [Column("adet", TypeName = "decimal(12,3)")] public decimal Adet { get; set; }
    [Column("birim_fiyat_tl", TypeName = "decimal(15,2)")] public decimal BirimFiyatTl { get; set; }
    [Column("iscilik_tl", TypeName = "decimal(15,2)")] public decimal IscilikTl { get; set; }
    [Column("sira")] public int Sira { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

/// <summary>Kiracı ayarı anahtarları — sihirli dize yazılmaz.</summary>
public static class KiraciAyarAnahtari
{
    /// <summary>Vezne ayrımı zorunlu mu (dört göz kuralı). Varsayılan KAPALI.</summary>
    public const string VezneZorunlu = "vezne_zorunlu";

    /// <summary>Kiracının renk şeması (paket P-36b · kurucu talimatı 16). Yoksa "altin".</summary>
    public const string GorunumSemasi = "gorunum_semasi";

    /// <summary>
    /// Kiracının yazı boyutu (paket P-36b; B-1'de 16|18|20'ye genişledi).
    /// Yoksa "16". Eski "yogun"/"okunakli" değerleri B-1 migration'ında göç etti.
    /// </summary>
    public const string GorunumYaziOlcegi = "gorunum_yazi_olcegi";

    /// <summary>
    /// PAKET B-1 — dükkânın aydınlık/karanlık varsayılanı:
    /// <c>otomatik</c> | <c>aydinlik</c> | <c>karanlik</c>. Yoksa "otomatik".
    /// </summary>
    public const string GorunumTemaModu = "gorunum_tema_modu";

    /// <summary>
    /// PAKET YASAL-4 / Y4-C — KMTS takip kodu alanı ürün formunda görünsün mü.
    /// Varsayılan KAPALI ("true" değilse kapalı): perakende kuyumcuya yükümlülük
    /// incelenen metinlerde yok (şartname § 3.1 M-14, T-7). Veri silinmez.
    /// </summary>
    public const string KmtsKullanimi = "kmts_kullanimi";

    /// <summary>
    /// M6 (müşteri 16.10: "fişe hiç gerek yok… sisteme girdiğim anda fatura kessin") — Tezgâh'ta satış
    /// belgesinin varsayılanı: <c>FIS</c> | <c>FATURA</c>. Yoksa "FIS" (bugünkü davranış). Kuyumcu her
    /// satışta değiştirebilir; kuyumcu fatura haddini aşan satışta ekran FATURA'ya geçer.
    /// </summary>
    public const string SatisBelgeVarsayilan = "satis_belge_varsayilan";

    /// <summary>
    /// D1-11 (18.10) — KÖR SAYIM: açıkken gün kapanışında beklenen nakit SAHİP dışındaki rollere gösterilmez
    /// (sunucu göndermez); fark kapanıştan sonra görünür. Değer "true"/"false"; yoksa KAPALI.
    /// </summary>
    public const string KasaKorSayim = "kasa_kor_sayim";

    /// <summary>D1-05 — bu yüzdeyi aşan indirim (liste fiyatına göre) patron onayı ister. Yoksa KAPALI.</summary>
    public const string OnayIskontoYuzde = "satis_onay_iskonto_yuzde";
    /// <summary>D1-05 — bu tutarı aşan satış fişi patron onayı ister (TL). Yoksa KAPALI.</summary>
    public const string OnayTutarTl = "satis_onay_tutar_tl";
    /// <summary>D1-05 — has değerinin altındaki (zararına) satış patron onayı ister. "true"/"false"; yoksa KAPALI.</summary>
    public const string OnayZararina = "satis_onay_zararina";
    /// <summary>D1-05 — mesai başlangıcı "SS:dd"; ikisi de varsa mesai dışı satış patrona bildirilir.</summary>
    public const string MesaiBaslangic = "mesai_baslangic";
    /// <summary>D1-02 — etiket şablonu ("50x30|AD,IBARE,…" — Kuyumcu.Core.Domain.EtiketSablonu). Yoksa varsayılan.</summary>
    public const string EtiketSablonu = "etiket_sablonu";
    /// <summary>D2-12 — sadakat: kaç TL net alışveriş 1 puan. Boş = kapalı.</summary>
    public const string SadakatTlBasina = "sadakat_tl_basina";
    /// <summary>D2-12 — sadakat başlangıç günü (yyyy-MM-dd); puan bu günden itibaren kazanılır.</summary>
    public const string SadakatBaslangic = "sadakat_baslangic";
    /// <summary>D1-05 — mesai bitişi "SS:dd".</summary>
    public const string MesaiBitis = "mesai_bitis";

    public static readonly string[] Tumu =
        [VezneZorunlu, GorunumSemasi, GorunumYaziOlcegi, GorunumTemaModu, KmtsKullanimi, SatisBelgeVarsayilan, KasaKorSayim,
         OnayIskontoYuzde, OnayTutarTl, OnayZararina, MesaiBaslangic, MesaiBitis, EtiketSablonu, SadakatTlBasina, SadakatBaslangic];
}

/// <summary>
/// Kiracı düzeyinde anahtar/değer ayarı.
///
/// ⚠ NEDEN AYRI TABLO: <see cref="Tenant"/> IKiraciVarlik DEĞİLDİR — ona konan
/// bir kolon global kiracı filtresinden, yazma denetçisinden ve denetim izinden
/// muaf kalırdı. Ayar burada olduğunda üç mekanizma da kendiliğinden çalışır.
/// </summary>
[Table("kiraci_ayarlari")]
public class KiraciAyari : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary>Kapalı liste — <see cref="KiraciAyarAnahtari"/>.</summary>
    [Column("anahtar"), MaxLength(60)] public string Anahtar { get; set; } = null!;

    /// <summary>Metin gösterimi ("true"/"false"). Sır/parola BURAYA YAZILMAZ.</summary>
    [Column("deger"), MaxLength(200)] public string Deger { get; set; } = null!;

    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}
