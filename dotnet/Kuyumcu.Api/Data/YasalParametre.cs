using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-1 — TARİHLİ YASAL PARAMETRELER (eşiklerin zaman boyutu)
//
// Devletin rakamları (tevsik haddi, MASAK kimlik eşiği, gider pusulası süresi…)
// BİR TARİHTEN İTİBAREN geçerlidir. Bu tablo her rakamı geçerlilik aralığıyla
// tutar: bugünkü işlem bugünkü rakamla, geçmiş işlem O GÜNÜN rakamıyla
// değerlendirilir; ileri tarihli satır gününde kendiliğinden devreye girer.
//
// ⚠ KİRACI-ÜSTÜ (ulusal rakam) — IKiraciVarlik DEĞİLDİR, HasQueryFilter YOKTUR —
//   KuyumcuDbContext kapsam denetiminde gerekçeli beyaz liste maddesi vardır
//   (AbonelikPlani kalıbı). Okuma YALNIZ YasalParametreServisi'nden, yazma
//   YALNIZ migration + sistem anahtarlı uçtan (SistemAnahtariKapisi).
// ⚠ APPEND-ONLY (ADR-0004 ruhu): bir satırın tek değişebilen alanı
//   gecerlilik_bitis'tir ve yalnız YENİ satır eklenirken bir kez doldurulur.
//   Bu tabloda ExecuteUpdate/ExecuteDelete/Remove = 0 (denetim.sh YASAL_PARAM_YAZMA).
// ⚠ ARALIK ÇAKIŞMASI: aynı anahtarda iki AÇIK UÇLU satır olamaz — veritabanı
//   düzeyinde işlevsel UNIQUE indeks (migration: ux_yasal_param_acik_uc).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Tek bir yasal rakamın bir geçerlilik aralığındaki değeri.</summary>
[Table("yasal_parametreler")]
public class YasalParametre
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;

    /// <summary>Beyaz listeden anahtar (<see cref="YasalAnahtar"/>).</summary>
    [Column("anahtar"), MaxLength(60)] public string Anahtar { get; set; } = null!;

    [Column("deger_sayi", TypeName = "decimal(20,6)")] public decimal? DegerSayi { get; set; }

    [Column("deger_metin"), MaxLength(200)] public string? DegerMetin { get; set; }

    /// <summary>TL · GUN · IS_GUNU · YIL · ORAN · KOD · TARIH.</summary>
    [Column("birim"), MaxLength(20)] public string Birim { get; set; } = null!;

    /// <summary>YEREL (Europe/Istanbul) gün, DAHİL.</summary>
    [Column("gecerlilik_baslangic")] public DateOnly GecerlilikBaslangic { get; set; }

    /// <summary>YEREL gün, HARİÇ. NULL = açık uçlu (bugün de geçerli).</summary>
    [Column("gecerlilik_bitis")] public DateOnly? GecerlilikBitis { get; set; }

    /// <summary>YALNIZ resmî atıf (ör. "Tedbirler Yönetmeliği md.5/1-b") — ekranda basılır.</summary>
    [Column("dayanak"), MaxLength(300)] public string Dayanak { get; set; } = null!;

    /// <summary>
    /// YASAL-1 tur 2 (tasarım hakemi): İÇ NOT — teyit durumu, şartname atfı, "kural
    /// kodlanmadı" gibi geliştirici/mali müşavir notları. Ekrana ve API'ye BASILMAZ.
    /// </summary>
    [Column("ic_not"), MaxLength(300)] public string? IcNot { get; set; }
    [Column("kaynak_url"), MaxLength(500)] public string KaynakUrl { get; set; } = null!;

    /// <summary>DOGRULANDI · TEYITSIZ · KAPALI (<see cref="YasalTeyit"/>).</summary>
    [Column("teyit"), MaxLength(12)] public string Teyit { get; set; } = null!;

    [Column("aciklama_sade"), MaxLength(300)] public string AciklamaSade { get; set; } = null!;

    /// <summary>
    /// PAKET YASAL-PARAM-EKRAN: değişikliğin yayımlandığı Resmî Gazete'nin tarihi.
    /// Sistem ekranından yazılan her yeni satırda ZORUNLU; eski tohum satırlarında
    /// birincil kaynakla doğrulanmadıkça NULL kalır (uydurulmaz).
    /// </summary>
    [Column("resmi_gazete_tarihi")] public DateOnly? ResmiGazeteTarihi { get; set; }

    /// <summary>PAKET YASAL-PARAM-EKRAN: Resmî Gazete sayısı (ör. "33393"). Yeni satırda ZORUNLU.</summary>
    [Column("resmi_gazete_sayisi"), MaxLength(20)] public string? ResmiGazeteSayisi { get; set; }

    /// <summary>
    /// PAKET YASAL-PARAM-2: BUGÜNDEN GEÇERLİ yazılan satırın yürürlüğe girdiği AN (UTC).
    /// NULL = başlangıç gününün yerel 00:00'ı (tohum ve ileri tarihli satırlar). Doluysa
    /// dönem sınırı gün değil ANDIR: aynı sabahki işlem önceki değerle değerlendirilir.
    /// Aynı gün düzeltme satırı da kendi anını taşır (yanlış değer [an1, an2) arasında kalır).
    /// </summary>
    [Column("gecerlilik_baslangic_ani")] public DateTime? GecerlilikBaslangicAni { get; set; }
    /// <summary>"migration:&lt;ad&gt;" ya da "sistem:&lt;operatör&gt;".</summary>
    [Column("olusturan"), MaxLength(60)] public string Olusturan { get; set; } = null!;

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

/// <summary>Teyit damgası — ekranda rozet olarak sözlükten çevrilir (ham kod basılmaz).</summary>
public static class YasalTeyit
{
    public const string Dogrulandi = "DOGRULANDI";
    public const string Teyitsiz = "TEYITSIZ";
    public const string Kapali = "KAPALI";
    public static readonly string[] Hepsi = [Dogrulandi, Teyitsiz, Kapali];
}

/// <summary>Birim kodları.</summary>
public static class YasalBirim
{
    public const string Tl = "TL";
    public const string Gun = "GUN";
    public const string IsGunu = "IS_GUNU";
    public const string Yil = "YIL";
    public const string Oran = "ORAN";
    public const string Kod = "KOD";
    public const string Tarih = "TARIH";
    /// <summary>Ayın günü (1–31) — süre DEĞİL: "izleyen ayın 26. günü".</summary>
    public const string AyinGunu = "AYIN_GUNU";
}

/// <summary>
/// Anahtar BEYAZ LİSTESİ (şartname § 4.2). Listede olmayan anahtar sistem
/// ucundan yazılamaz; birim de buradan gelir (istemci birim uyduramaz).
/// </summary>
public static class YasalAnahtar
{
    public const string TevsikHaddiTl = "TEVSIK_HADDI_TL";
    public const string TevsikBildirimIsGunu = "TEVSIK_BILDIRIM_IS_GUNU";
    public const string MasakKimlikEsigiTl = "MASAK_KIMLIK_ESIGI_TL";
    public const string MasakAmacBeyaniEsigiTl = "MASAK_AMAC_BEYANI_ESIGI_TL";
    public const string MasakBeyanFormuEsigiTl = "MASAK_BEYAN_FORMU_ESIGI_TL";
    public const string MasakEkBelgeEsigiTl = "MASAK_EK_BELGE_ESIGI_TL";
    public const string MasakSibIsGunu = "MASAK_SIB_IS_GUNU";
    public const string MasakSaklamaYil = "MASAK_SAKLAMA_YIL";
    public const string GiderPusulasiGun = "GIDER_PUSULASI_GUN";
    public const string StopajEsnafMuafHurdaOran = "STOPAJ_ESNAF_MUAF_HURDA_ORAN";
    public const string StopajNihaiTuketiciOran = "STOPAJ_NIHAI_TUKETICI_ORAN";
    public const string StopajBeyanGunu = "STOPAJ_BEYAN_GUNU";
    public const string KdvGenelOran = "KDV_GENEL_ORAN";
    public const string KdvOzelMatrahKodu = "KDV_OZEL_MATRAH_KODU";
    public const string ResmiTatil = "RESMI_TATIL";
    public const string FaturaHaddiTl = "FATURA_HADDI_TL";
    /// <summary>
    /// M6 (müşteri 16.10: "30 bin lira geçtiğinde isim konmalı… fişe gerek yok") — işlenmiş kıymetli maden
    /// satışında fatura düzenleme haddi = genel haddin 3 KATI (VUK GT 514 md.4, RG 04.01.2020/30998).
    /// Yıllık satır olarak tutulur (genel had her yıl değişir); 2026: 12.000 × 3 = 36.000 TL.
    /// </summary>
    public const string KuyumcuFaturaHaddiTl = "KUYUMCU_FATURA_HADDI_TL";
    public const string KmtsKodDeseni = "KMTS_KOD_DESENI";

    /// <summary>Anahtar → birim. Beyaz listenin TEK kaynağı.</summary>
    public static readonly IReadOnlyDictionary<string, string> Birimler =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [TevsikHaddiTl] = YasalBirim.Tl,
            [TevsikBildirimIsGunu] = YasalBirim.IsGunu,
            [MasakKimlikEsigiTl] = YasalBirim.Tl,
            [MasakAmacBeyaniEsigiTl] = YasalBirim.Tl,
            [MasakBeyanFormuEsigiTl] = YasalBirim.Tl,
            [MasakEkBelgeEsigiTl] = YasalBirim.Tl,
            [MasakSibIsGunu] = YasalBirim.IsGunu,
            [MasakSaklamaYil] = YasalBirim.Yil,
            [GiderPusulasiGun] = YasalBirim.Gun,
            [StopajEsnafMuafHurdaOran] = YasalBirim.Oran,
            [StopajNihaiTuketiciOran] = YasalBirim.Oran,
            [StopajBeyanGunu] = YasalBirim.AyinGunu,
            [KdvGenelOran] = YasalBirim.Oran,
            [KdvOzelMatrahKodu] = YasalBirim.Kod,
            [ResmiTatil] = YasalBirim.Tarih,
            [FaturaHaddiTl] = YasalBirim.Tl,
            [KuyumcuFaturaHaddiTl] = YasalBirim.Tl,
            [KmtsKodDeseni] = YasalBirim.Kod,
        };

    /// <summary>
    /// Aynı anda BİRDEN ÇOK açık satırı olabilen anahtar: her tatil günü ayrı
    /// satırdır (değer = tarih). Diğer anahtarlarda bir gün için TEK değer vardır.
    /// </summary>
    public static bool CokDegerli(string anahtar) => anahtar == ResmiTatil;

    /// <summary>
    /// YASAL-1 tur 2: program kuralını TABLODAN, İŞLEM GÜNÜNE GÖRE okuyan anahtarlar.
    /// Sistem ucu YALNIZ bunlara ileri tarihli değer yazar: diğer anahtarları kod hâlâ
    /// yapılandırmadan okur (gider pusulası/stopaj/KDV → YASAL-4, tatil → YASAL-3);
    /// onlara yazılan ileri tarihli satır gününde UYGULANMAZDI (hasım bulgusu ORTA-3).
    /// </summary>
    public static readonly IReadOnlySet<string> TablodanOkunan = new HashSet<string>(StringComparer.Ordinal)
    {
        TevsikHaddiTl, MasakKimlikEsigiTl, MasakAmacBeyaniEsigiTl, MasakBeyanFormuEsigiTl,
        MasakEkBelgeEsigiTl, MasakSibIsGunu, MasakSaklamaYil,
        // PAKET YASAL-4 (Y4-C): KMTS biçimi işlem günü tablodan okunur (Common/KmtsKodu).
        KmtsKodDeseni,
        // M6 (16.10): kuyumcu fatura haddi satış anında tablodan okunur (Tezgâh belge türü kuralı).
        KuyumcuFaturaHaddiTl,
    };

    /// <summary>
    /// Programın bugün bir kuralda KULLANDIĞI anahtarlar (tablodan ya da tabloyla
    /// eşitliği açılışta denetlenen yapılandırmadan). Listede olmayan satır ekranda
    /// "Henüz kullanılmayan sınırlar" grubuna düşer — kullanıcı programın onu
    /// denetlediğini sanmaz (hasım bulgusu DÜŞÜK-5: FATURA_HADDI_TL).
    /// </summary>
    public static readonly IReadOnlySet<string> ProgramdaKullanilan = new HashSet<string>(TablodanOkunan, StringComparer.Ordinal)
    {
        GiderPusulasiGun, StopajEsnafMuafHurdaOran, StopajNihaiTuketiciOran, KdvGenelOran,
    };

    /// <summary>
    /// YASAL-1 tur 3 (orkestratör bulgusu 2): programın <c>tutar &gt;= eşik</c> ile
    /// karşılaştırdığı eşikler (<c>MasakKuralMotoru</c>). Ekranda iç karşılaştırma
    /// değeri (ör. 2.000.000,01) KURUŞLA gösterilmez; kullanıcı dilinde yazılır.
    /// </summary>
    public static readonly IReadOnlySet<string> BuyukEsitKarsilastirilan = new HashSet<string>(StringComparer.Ordinal)
    {
        MasakKimlikEsigiTl, MasakAmacBeyaniEsigiTl, MasakBeyanFormuEsigiTl, MasakEkBelgeEsigiTl,
    };

    /// <summary>Eşik tutarından büyük olanlar ("2.000.000 ₺ üzeri").</summary>
    public const string EsikUstu = "USTU";

    /// <summary>Eşik tutarı dahil ("370.000 ₺ ve üzeri").</summary>
    public const string EsikVeUstu = "VE_USTU";

    /// <summary>
    /// Ekran eşik ifadesi: <c>t &gt;= X,01</c> kuruş duyarlığında <c>t &gt; X</c> demektir →
    /// ("USTU", X); tam sayı eşikte <c>t &gt;= X</c> → ("VE_USTU", X). Karşılaştırması
    /// <c>&gt;=</c> olmayan anahtarda ya da başka kesirde (null, null): değer olduğu gibi yazılır.
    /// </summary>
    public static (string? Tur, decimal? Tutar) EsikGosterimi(string anahtar, decimal? deger)
    {
        if (deger is not { } d || !BuyukEsitKarsilastirilan.Contains(anahtar)) return (null, null);
        var tam = decimal.Floor(d);
        if (d - tam == 0.01m) return (EsikUstu, tam);
        if (d == tam) return (EsikVeUstu, d);
        return (null, null);
    }
}
