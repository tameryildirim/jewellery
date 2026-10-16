using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Kuyumcu.Api.Modules.Hurda;
using Kuyumcu.Api.Modules.Masak;

namespace Kuyumcu.Api.Modules.Satis;

// ═════════════════════════════════════════════════════════════════════════════
// Satış / İşlemler modülü — kaynak: apps/api/src/modules/satis/*
//   • Rota: api/v1/islemler (satis, alis, hurda-alis, transfer, takas, iscilik)
//   • Paralel defter: hareketler.tlTutari (TL bacağı) + hareketler.hasGrami (has bacağı)
//   • Her işlem tek EF transaction içinde: hareket + stok + cari bakiye + kasa
//
// 2026-09-10 Faz 0 düzeltmeleri (ekip oyu: Bertan mimari, Mehmet güvenlik):
//   K1 Her yabancı kimlik (cari/kasa/şube/kur) IslemDogrulayici'den geçer.
//   K2 Alışta kasa ödemesi yapıldıysa cari BORÇLANMAZ (çifte kayıt kalktı).
//   K3 Hurda alışta kasa ödemesi yapıldıysa cariye TL yazılmaz; has bacağı
//      hiçbir durumda cariye yazılmaz (üçlü kayıt kalktı).
//   K4 İşaret sözleşmesi: hareketler.has_grami = fiziki akış (giren +, çıkan −);
//      cari_bakiyeler.has_gram_bakiye = alacak perspektifi (+ = müşteri borçlu).
//      TL ile kapanan işlemin (satış/alış/hurda/takas) has bacağı CARİYE YAZILMAZ.
//   K6 Kasa hareketi kasanın para birimiyle kaydedilir; TL işlemi döviz
//      kasasına yazılamaz.
//   Ayrıca: hareketler.odeme_turu yazılır → bakiyeler defterden türetilebilir.
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class SatisOlusturDto
{

    /// <summary>
    /// Bu borcun vadesi — İŞLEM GÜNÜNDEN İTİBAREN GÜN sayısı. Boşsa carinin
    /// varsayılan vadesi, o da boşsa 0 (aynı gün muaccel) kullanılır.
    /// </summary>
    [Range(0, 3650)] public int? VadeGun { get; set; }
    /// <summary>Müşteri cari ID (opsiyonel, müşteriye bağlanır) — Nest: @IsOptional + @IsNotEmpty (boş string reddedilir)</summary>
    [MinLength(1)] public string? CariId { get; set; }

    /// <summary>Satılan ürün ID</summary>
    [Required] public string UrunId { get; set; } = null!;

    /// <summary>Satış adedi (1 tam ürün = 1)</summary>
    [Range(0.001, KolonTavani.UstAdet)] public decimal Adet { get; set; }

    /// <summary>Birim satış fiyatı TL</summary>
    [Range(0, KolonTavani.UstTutar)] public decimal BirimFiyatTl { get; set; }

    /// <summary>İşçilik TL (sıfır da olabilir)</summary>
    [Range(0, KolonTavani.UstTutar)] public decimal? IscilikTl { get; set; }

    /// <summary>
    /// Tek etiketli ödeme türü: NAKIT · KART · HAVALE · VERESIYE.
    /// <para>
    /// PAKET B-4 (G-1): <c>ALTIN</c> ÇIKARILDI. Tek etiketli ALTIN yolu fişin
    /// tamamını TL kasasına yazıyor, tevsik kapısını tetiklemiyor ve alınan
    /// altını hiçbir yerde has olarak göstermiyordu (KARAR-HAS-DEFTERI § 3.2).
    /// Altınla ödeme artık satış fişinin ALTIN bacağıdır. Geçmiş satırlar
    /// okunabilir kalır (<c>Sabitler.OdemeTurleri</c> "ALTIN"ı korur — G-2).
    /// </para>
    /// </summary>
    [Required, RegularExpression("NAKIT|KART|HAVALE|VERESIYE", ErrorMessage = OdemeSepeti.TekEtiketliAltinMesaji)]
    public string OdemeTuru { get; set; } = null!;

    /// <summary>Kasa ID — NAKIT/KART/HAVALE için zorunlu; VERESIYE ise boş bırakılabilir.</summary>
    public string? KasaId { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    /// <summary>İşlem anındaki kur snapshot ID</summary>
    [Required] public string KurSnapshotId { get; set; } = null!;

    /// <summary>MASAK bloğu — eşik altında boş. Kimlik eşiğinde (işlem gününün yasal_parametreler değeri; 07.10.2026'dan itibaren 370.000 TL) ve amaç beyanı kademesinde (200.000 TL, TEYİTSİZ) kapı zorunlu kılar.</summary>
    public MasakBilgiDto? Masak { get; set; }

    /// <summary>
    /// PAKET 25 — OPSİYONEL reyon: malın fiziksel olarak hangi reyondan
    /// çıkacağı/gireceği. Boş bırakılırsa çıkışta önce ATANMAMIŞ mal tükenir,
    /// yetmezse reyonlar ad sırasıyla düşülür ve yanıtta <c>yerlesimUyarisi</c>
    /// döner (sessizce yutulmaz). Girişte ürünün varsayılan reyonu (aynı şubede
    /// ve aktifse) kullanılır.
    /// </summary>
    [MaxLength(36)] public string? ReyonId { get; set; }

    /// <summary>
    /// PAKET 31 — YALNIZ KART ödemesinde: tahsilatın geçtiği POS cihazı.
    /// Boşsa şubede TEK aktif cihaz varsa o seçilir; birden çok varsa istek
    /// REDDEDİLİR (ilk cihaz SEÇİLMEZ — fail-closed).
    /// </summary>
    [MaxLength(36)] public string? PosCihazId { get; set; }

    /// <summary>PAKET 31 — kart taksit adedi (1 = tek çekim).</summary>
    [Range(1, 12)] public int? PosTaksitAdedi { get; set; }

    /// <summary>
    /// PAKET S2-5b — SATIŞI YAPAN personel (kullanıcı kimliği). Gönderilmezse OTURUM
    /// kullanıcısı yazılır. Başkası yalnız SAHIP/MUDUR tarafından seçilebilir; aynı
    /// kiracıda ve AKTİF olmalıdır (SatisServisi.SaticiBelirle). Yasal belge alanı değil.
    /// </summary>
    [MaxLength(36)] public string? PersonelId { get; set; }
}

public sealed class AlisOlusturDto
{

    /// <summary>
    /// Bu borcun vadesi — İŞLEM GÜNÜNDEN İTİBAREN GÜN sayısı. Boşsa carinin
    /// varsayılan vadesi, o da boşsa 0 (aynı gün muaccel) kullanılır.
    /// </summary>
    [Range(0, 3650)] public int? VadeGun { get; set; }
    /// <summary>Tedarikçi / satıcı cari ID</summary>
    public string? CariId { get; set; }

    /// <summary>Alınan ürün ID (yoksa önce ürün tanımlanmalı)</summary>
    [Required] public string UrunId { get; set; } = null!;

    /// <summary>Alış adedi</summary>
    [Range(0.001, KolonTavani.UstAdet)] public decimal Adet { get; set; }

    /// <summary>Birim alış fiyatı TL</summary>
    [Range(0, KolonTavani.UstTutar)] public decimal BirimFiyatTl { get; set; }

    /// <summary>Kasa ID — nakit/havale ödeme yapılıyorsa; cari borçlandırılacaksa boş.</summary>
    public string? KasaId { get; set; }

    /// <summary>
    /// Ödeme aracı: NAKIT · KART · HAVALE.
    ///
    /// <para>
    /// Bu alan 2026-09-14'te eklendi. Öncesinde alışta ödeme aracı HİÇ
    /// sorulmuyordu; (2026-09-14'te silinen) <c>OdemeEtiketi</c> peşin ödemeyi koşulsuz "NAKIT"
    /// yazıyordu. Sonuç: <b>30.000 TL üstü havaleli alış hiç kaydedilemiyordu</b> —
    /// tevsik kapısı doğru çalışıyor ama girdi yanlış olduğu için meşru
    /// banka ödemesini de reddediyordu. Kuyumcunun 68.000 TL'lik külçe alımı
    /// sisteme girmiyordu.
    /// </para>
    ///
    /// <para>
    /// ⚠ Varsayılan <b>NAKIT</b>'tir ve bu bilinçlidir (tahsilatla aynı gerekçe):
    /// alan gönderilmediğinde en sıkı kapı işler. Varsayılanı HAVALE yapmak,
    /// eski istemcilerin nakit ödemelerini sessizce tevsik denetiminden
    /// kaçırırdı — fail-closed.
    /// </para>
    /// </summary>
    [RegularExpression("NAKIT|KART|HAVALE")]
    public string OdemeTuru { get; set; } = "NAKIT";

    /// <summary>
    /// Kısmi ödeme: kasadan çıkan tutar. Boş bırakılırsa kasa verildiyse tamamı
    /// peşin, verilmediyse tamamı cari borcu sayılır. Verilirse kasaId zorunludur.
    /// </summary>
    [Range(0, KolonTavani.UstTutar)] public decimal? OdenenTl { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;

    /// <summary>MASAK bloğu — eşik altında boş. Kimlik eşiğinde (işlem gününün yasal_parametreler değeri; 07.10.2026'dan itibaren 370.000 TL) ve amaç beyanı kademesinde (200.000 TL, TEYİTSİZ) kapı zorunlu kılar.</summary>
    public MasakBilgiDto? Masak { get; set; }

    /// <summary>
    /// PAKET 25 — OPSİYONEL reyon: malın fiziksel olarak hangi reyondan
    /// çıkacağı/gireceği. Boş bırakılırsa çıkışta önce ATANMAMIŞ mal tükenir,
    /// yetmezse reyonlar ad sırasıyla düşülür ve yanıtta <c>yerlesimUyarisi</c>
    /// döner (sessizce yutulmaz). Girişte ürünün varsayılan reyonu (aynı şubede
    /// ve aktifse) kullanılır.
    /// </summary>
    [MaxLength(36)] public string? ReyonId { get; set; }

}

/// <summary>
/// PAKET L-1b — alış fişinin bir kalemi (tek kalemli <see cref="AlisOlusturDto"/>'nun
/// kalem alanlarıyla AYNI sözleşme).
/// </summary>
public sealed class AlisFisKalemDto
{
    /// <summary>Alınan ürün ID</summary>
    [Required, MaxLength(36)] public string UrunId { get; set; } = null!;

    /// <summary>Alış adedi</summary>
    [Range(0.001, KolonTavani.UstAdet)] public decimal Adet { get; set; }

    /// <summary>Birim alış fiyatı TL</summary>
    [Range(0, KolonTavani.UstTutar)] public decimal BirimFiyatTl { get; set; }
}

/// <summary>
/// PAKET L-1b — POST /islemler/alis-fisi gövdesi: çok kalemli alış TEK işlemdir.
/// Fiş düzeyindeki alanların anlamı <see cref="AlisOlusturDto"/> ile BİREBİR aynıdır
/// (cari · kasa · ödeme aracı · kısmi ödeme · vade · MASAK · reyon); yalnız kalemler
/// liste olur ve kapılar fişin BRÜT toplamını görür.
/// </summary>
public sealed class AlisFisiDto
{
    [Range(0, 3650)] public int? VadeGun { get; set; }
    public string? CariId { get; set; }

    // L-1b (canlı C14): liste için varsayılan ileti "en az 1 karakter" diyordu → alan altı Türkçe ileti düzeltildi.
    [Required(ErrorMessage = "Alış fişinde en az bir kalem olmalıdır"),
     MinLength(1, ErrorMessage = "Alış fişinde en az bir kalem olmalıdır"),
     MaxLength(SatisServisi.AlisFisiKalemTavani, ErrorMessage = "Bir alış fişinde en çok 100 kalem olabilir")]
    public List<AlisFisKalemDto> Kalemler { get; set; } = [];

    public string? KasaId { get; set; }

    /// <summary>NAKIT · KART · HAVALE — varsayılan NAKIT (fail-closed; tek kalemli uçla aynı gerekçe).</summary>
    [RegularExpression("NAKIT|KART|HAVALE")]
    public string OdemeTuru { get; set; } = "NAKIT";

    /// <summary>Kısmi ödeme: FİŞ için kasadan çıkan tutar (boş = kasa varsa tamamı peşin).</summary>
    [Range(0, KolonTavani.UstTutar)] public decimal? OdenenTl { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;

    /// <summary>MASAK bloğu — fiş TOPLAMI eşiğe ulaşıyorsa kapı zorunlu kılar.</summary>
    public MasakBilgiDto? Masak { get; set; }

    /// <summary>Opsiyonel giriş reyonu — tüm kalemler için (tek kalemli uçla aynı sözleşme).</summary>
    [MaxLength(36)] public string? ReyonId { get; set; }
}

public sealed class HurdaAlisDto
{

    /// <summary>
    /// Bu borcun vadesi — İŞLEM GÜNÜNDEN İTİBAREN GÜN sayısı. Boşsa carinin
    /// varsayılan vadesi, o da boşsa 0 (aynı gün muaccel) kullanılır.
    /// </summary>
    [Range(0, 3650)] public int? VadeGun { get; set; }
    /// <summary>Müşteri / satıcı cari</summary>
    public string? CariId { get; set; }

    /// <summary>Hurdanın ayarı</summary>
    [Required, Kuyumcu.Core.Domain.AyarId]
    public string AyarId { get; set; } = null!;

    /// <summary>Hurda fiziksel gram</summary>
    [Range(0.001, KolonTavani.UstAdet)] public decimal GramAgirlik { get; set; }

    /// <summary>Ödenen birim fiyat TL/has gram</summary>
    [Range(0, KolonTavani.UstTutar)] public decimal BirimFiyatTlHasGram { get; set; }
    /// <summary>PAKET ISLEM-BANT — bant dışı has fiyatı bilerek kaydet (yalnız SAHİP/MÜDÜR; IslemFiyatKapisi).</summary>
    public bool? BantOnay { get; set; }
    /// <summary>PAKET ISLEM-BANT — bant aşımı gerekçesi (K30 ≥ 20; servis içinde denetlenir).</summary>
    [StringLength(1000)] public string? BantGerekce { get; set; }

    /// <summary>Ödeme yapılan kasa</summary>
    public string? KasaId { get; set; }

    /// <summary>
    /// Ödeme aracı: NAKIT · KART · HAVALE. Varsayılan NAKIT (fail-closed).
    ///
    /// <para>
    /// ⚠ Hurda alımı, 459 s. VUK Genel Tebliği'nin en riskli vakasıdır:
    /// 572 s. Tebliğ (RG 18.10.2024/32696) ile <b>mükellef olmayanlar</b>
    /// (vatandaş) açıkça kapsama alındı. 30.000 TL üstü nakit hurda alımında
    /// ceza <b>hem kuyumcuya hem vatandaşa ayrı ayrı</b> kesilir. Bu yüzden
    /// ödeme aracının beyan edilmesi gerekir — her ödeme nakit sayılamaz.
    /// </para>
    /// </summary>
    [RegularExpression("NAKIT|KART|HAVALE")]
    public string OdemeTuru { get; set; } = "NAKIT";

    /// <summary>Kısmi ödeme: kasadan çıkan tutar (bkz. AlisOlusturDto.OdenenTl).</summary>
    [Range(0, KolonTavani.UstTutar)] public decimal? OdenenTl { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;

    /// <summary>MASAK bloğu — eşik altında boş. Kimlik eşiğinde (işlem gününün yasal_parametreler değeri; 07.10.2026'dan itibaren 370.000 TL) ve amaç beyanı kademesinde (200.000 TL, TEYİTSİZ) kapı zorunlu kılar.</summary>
    public MasakBilgiDto? Masak { get; set; }

    /// <summary>
    /// Satıcı kimliği — ZORUNLU (VUK md.234, PLAN Adım 6). MASAK'tan farklı olarak
    /// TUTAR EŞİĞİ YOK: her hurda alımında gerekir. Nullable bırakıldı ki eski
    /// çağrılar derlensin; servis null'ı REDDEDER (fail-closed).
    /// </summary>
    public HurdaSaticiDto? Satici { get; set; }

    /// <summary>KMTS etiketli mal alınıyorsa kodu — opsiyonel (Adım 8).</summary>
    [MaxLength(100)] public string? KmtsKodu { get; set; }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-20 (Y-10) — HURDA SATIŞI / RAMAT ÇIKIŞI
//
// Biriken hurdanın rafineriye (ramatçıya) çıkışı. HURDA_ALIS'in AYNASIDIR:
//   has bacağı EKSİLİR (−), TL bacağı ARTAR (+).
//
// ⚠ NEDEN GRAM DEĞİL HAS GRAM İSTENİR: hurda stoğu ayarıyla değil, HAS'ıyla
//   birikir (22K 10 g ile 14K 16 g aynı 9,16 has olabilir). Çıkışta "gram+ayar"
//   sorulsaydı, elde 9,16 has varken 10 g 24K (= 9,95 has) çıkışı ekranda
//   "10 g vardı, 10 g çıktı" gibi görünür ve defter EKSİYE düşerdi.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class HurdaSatisDto
{
    /// <summary>
    /// Hangi madenin hurdası çıkıyor — bakiye MADEN BAZINDA tutulur
    /// (altının hurdası gümüşün hurdasıyla toplanmaz, K20-1).
    /// </summary>
    [Required, MaxLength(20)] public string Maden { get; set; } = Kuyumcu.Core.Domain.Maden.Altin;

    /// <summary>
    /// BİZİM KAYDIMIZ — rafineriye çıkan HAS gram. Şubedeki hurda bakiyesini
    /// AŞAMAZ (fail-closed). Stoktan çıkan TOPLAM has budur.
    /// </summary>
    [Range(0.000001, KolonTavani.UstHas)] public decimal HasGrami { get; set; }

    /// <summary>
    /// PAKET P-20b (K-13/2) — ALICININ (rafineri/toptancı) KABUL ETTİĞİ HAS.
    ///
    /// <para>
    /// <c>null</c> ⇒ alıcı bizim kaydımızı aynen kabul etti; fark YOKTUR ve
    /// ayrı bacak yazılmaz (P-20'nin bugünkü davranışı birebir korunur).
    /// Doluysa TL bacağı BU hasla ödenir (<c>kabul × birim fiyat</c>) ve
    /// aradaki fark ayrı bir <c>TAHLIL_FARKI</c> bacağına yazılır.
    /// </para>
    /// <para>
    /// ⚠ Bu alan FİRE DEĞİLDİR (K-13/1: eritme firesi imalat kapsamındadır ve
    /// 1.0'a girmez). Tahlil farkı bir ÖLÇÜM mutabakatsızlığıdır: alıcının
    /// tahlil raporu ile bizim kaydımız tutmaz. Fark yazılmazsa has-cari denk
    /// gelmez.
    /// </para>
    /// </summary>
    [Range(0.000001, KolonTavani.UstHas)] public decimal? AliciKabulHasGrami { get; set; }

    /// <summary>
    /// PAKET P-20b — tahlil farkının GEREKÇESİ. Fark ≠ 0 ise ZORUNLUDUR ve en
    /// az <see cref="Kuyumcu.Api.Data.TahlilFarkiFabrikasi.GerekceAsgari"/>
    /// karakterdir; fark 0 ise gönderilse de bacak yazılmaz (yanıt bunu söyler).
    /// </summary>
    [MaxLength(1000)] public string? TahlilFarkiGerekcesi { get; set; }

    /// <summary>Ramatçının ödediği birim fiyat TL/has gram.</summary>
    [Range(0, KolonTavani.UstTutar)] public decimal BirimFiyatTlHasGram { get; set; }
    /// <summary>PAKET ISLEM-BANT — bant dışı has fiyatı bilerek kaydet (yalnız SAHİP/MÜDÜR; IslemFiyatKapisi).</summary>
    public bool? BantOnay { get; set; }
    /// <summary>PAKET ISLEM-BANT — bant aşımı gerekçesi (K30 ≥ 20; servis içinde denetlenir).</summary>
    [StringLength(1000)] public string? BantGerekce { get; set; }

    /// <summary>Ramatçı / rafineri carisi — tahsil edilmeyen kısım buraya ALACAK yazılır.</summary>
    public string? CariId { get; set; }

    /// <summary>Tahsilatın girdiği kasa.</summary>
    public string? KasaId { get; set; }

    /// <summary>Tahsil aracı: NAKIT · KART · HAVALE (varsayılan NAKIT).</summary>
    [RegularExpression("NAKIT|KART|HAVALE")]
    public string OdemeTuru { get; set; } = "NAKIT";

    /// <summary>Kısmi tahsilat: kasaya giren tutar. Boşsa tamamı tahsil edilmiş sayılır.</summary>
    [Range(0, KolonTavani.UstTutar)] public decimal? TahsilEdilenTl { get; set; }

    /// <summary>Kalan alacağın vadesi — işlem gününden itibaren GÜN.</summary>
    [Range(0, 3650)] public int? VadeGun { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;

    /// <summary>MASAK bloğu — eşik altında boş.</summary>
    public MasakBilgiDto? Masak { get; set; }

    /// <summary>
    /// FİRE / RAMAT FARKI — K-13 KURUCU KARARI BEKLİYOR.
    /// <para>
    /// Alan DTO'da DURUR ama kapı KAPALIDIR: yapılandırmada
    /// <c>Hurda:FireKaydiAcik</c> açılmadıkça dolu gönderilirse istek
    /// gerekçesiyle REDDEDİLİR (sessizce yutulmaz — Anayasa md.1).
    /// </para>
    /// </summary>
    [Range(0, KolonTavani.UstHas)] public decimal? FireGrami { get; set; }
}

public sealed class TransferDto
{
    [Required] public string KaynakSubeId { get; set; } = null!;
    [Required] public string HedefSubeId { get; set; } = null!;
    [Required] public string UrunId { get; set; } = null!;

    [Range(0.001, KolonTavani.UstAdet)] public decimal Adet { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;

    /// <summary>
    /// PAKET 25 — OPSİYONEL kaynak reyon (KAYNAK şubede). Boş bırakılırsa önce
    /// ATANMAMIŞ mal tükenir, yetmezse reyonlar ad sırasıyla düşülür.
    /// </summary>
    [MaxLength(36)] public string? ReyonId { get; set; }

    /// <summary>
    /// PAKET 25 — OPSİYONEL hedef reyon (HEDEF şubede). Boşsa mal hedef şubede
    /// ATANMAMIŞ olarak girer; başka şubenin reyonuna SESSİZCE yazılmaz.
    /// </summary>
    [MaxLength(36)] public string? HedefReyonId { get; set; }

}

public sealed class SatisFisKalemDto
{
    [Required] public string UrunId { get; set; } = null!;

    [Range(0.001, KolonTavani.UstAdet)] public decimal Adet { get; set; }

    [Range(0, KolonTavani.UstTutar)] public decimal BirimFiyatTl { get; set; }

    [Range(0, KolonTavani.UstTutar)] public decimal? IscilikTl { get; set; }

    /// <summary>
    /// Satışın yanında BEDELSİZ verilen mal. Sunucu birim fiyatı 0'a zorlar ve liste
    /// fiyatını indirim olarak yazar (KDV GUT: tutar iskontosu). Aynı fişte bedelli
    /// kalem yoksa REDDEDİLİR — tek başına hediye KDVK md.27 emsal bedel doğurur.
    /// </summary>
    public bool Hediye { get; set; }

    /// <summary>
    /// PAKET P-16 (Y-4) — SATIR İSKONTOSU, YÜZDE (0-100).
    /// <para>
    /// ⚠ İstemci TUTAR göndermez, YÜZDE gönderir; TL karşılığı sunucuda
    /// <see cref="IskontoHesabi"/> ile üretilir. Böylece ekranda görünen ile
    /// deftere yazılan ayrışamaz.
    /// </para>
    /// <para>Hediye kalemde anlamsızdır: fiyat zaten 0'a zorlanır.</para>
    /// </summary>
    [Common.Dogrulama.Yuzde]   // PAKET GOC-C: K15 (0–100, 2 ondalık) — ekran Yuzde ile aynı Core kuralı
    public decimal? SatirIskontoYuzde { get; set; }
}

/// <summary>
/// COK BACAKLI SATIS FISI - "30.000 TL'nin 15.000'i nakit, 10.000'i kart,
/// 5.000'i veresiye" gibi tek seferde birden fazla aracla kapatilan satis.
///
/// UYARI: Mevcut <see cref="SatisOlusturDto"/> ve <c>POST /islemler/satis</c>
/// ucu DEGISMEZ; <c>OdemeTuru</c> regex'ine "KARMA" EKLENMEZ. Karma odeme
/// yalniz bu uctan yapilir, boylece tek odemeli akisin davranisi hic kipirdamaz.
/// </summary>
public sealed class SatisFisiDto
{

    /// <summary>
    /// Bu borcun vadesi — İŞLEM GÜNÜNDEN İTİBAREN GÜN sayısı. Boşsa carinin
    /// varsayılan vadesi, o da boşsa 0 (aynı gün muaccel) kullanılır.
    /// </summary>
    [Range(0, 3650)] public int? VadeGun { get; set; }
    /// <summary>Musteri cari - veresiye bacagi varsa ZORUNLU.</summary>
    [MinLength(1)] public string? CariId { get; set; }

    [Required, MinLength(1)] public List<SatisFisKalemDto> Kalemler { get; set; } = [];

    /// <summary>PAKET ISLEM-BANT — fişteki eski altın bacağının bant dışı has fiyatı bilerek kaydet (yalnız SAHİP/MÜDÜR; IslemFiyatKapisi).</summary>
    public bool? BantOnay { get; set; }
    /// <summary>PAKET ISLEM-BANT — bant aşımı gerekçesi (K30 ≥ 20; servis içinde denetlenir).</summary>
    [StringLength(1000)] public string? BantGerekce { get; set; }

    [Required(ErrorMessage = "Fiş en az bir ödeme bacağı içermelidir"), MinLength(1, ErrorMessage = "Fiş en az bir ödeme bacağı içermelidir")] public List<OdemeBacagiDto> Odemeler { get; set; } = [];

    [MaxLength(500)] public string? Aciklama { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;

    /// <summary>MASAK blogu - esik FISIN TOPLAMINA uygulanir, bacak basina degil.</summary>
    public MasakBilgiDto? Masak { get; set; }

    /// <summary>
    /// PAKET P-16 (Y-4) — FİŞ (GENEL) İSKONTOSU, YÜZDE (0-100).
    /// <para>
    /// Satır iskontoları düşüldükten SONRA kalan toplama uygulanır ve
    /// satırlara oransal dağıtılır (<see cref="IskontoHesabi.Dagit"/>).
    /// </para>
    /// <para>
    /// ⚠ UX-S21: boş bırakılırsa SUNUCU carinin kartındaki
    /// <c>iskonto_yuzde</c> oranını uygular — sistem bildiği oranı kasiyerden
    /// tekrar istemez. Açıkça <c>0</c> gönderilirse iskonto YOKTUR (carinin
    /// oranı da uygulanmaz); "0" ile "boş" ayrı anlamlardır.
    /// </para>
    /// </summary>
    [Common.Dogrulama.Yuzde]   // PAKET GOC-C: K15 (0–100, 2 ondalık) — ekran Yuzde ile aynı Core kuralı
    public decimal? FisIskontoYuzde { get; set; }

    /// <summary>
    /// PAKET S2-5b — SATIŞI YAPAN personel (kullanıcı kimliği). Gönderilmezse OTURUM
    /// kullanıcısı yazılır. Başkası yalnız SAHIP/MUDUR tarafından seçilebilir; aynı
    /// kiracıda ve AKTİF olmalıdır (SatisServisi.SaticiBelirle). Yasal belge alanı değil.
    /// </summary>
    [MaxLength(36)] public string? PersonelId { get; set; }

    /// <summary>
    /// PAKET L-1 — OPSİYONEL reyon (tek kalemli <c>POST /islemler/satis</c> ile AYNI sözleşme):
    /// çok kalemli düz satış artık TEK fiş olarak bu uca gelir (tevsik/MASAK fiş toplamına
    /// bakar). Ekranın seçtiği reyon kaybolmasın diye kabul edilir; seçildiyse fişin TÜM
    /// kalemleri YALNIZ o reyondan düşer (fail-closed), boşsa sunucu sırası işler ve yanıtta
    /// <c>yerlesimUyarisi</c> döner. Gönderilmezse davranış eskisiyle AYNIDIR.
    /// </summary>
    [MaxLength(36)] public string? ReyonId { get; set; }
}

/// <summary>
/// İADE — satılan malın geri alınması veya alınan malın geri verilmesi.
/// İade bağımsız bir işlem DEĞİLDİR: hangi işlemi tersine çevirdiği zorunludur.
/// </summary>
public sealed class IadeDto
{
    /// <summary>İade edilen ORİJİNAL satış/alış hareketinin kimliği.</summary>
    [Required] public string KaynakHareketId { get; set; } = null!;

    /// <summary>İade edilen adet — orijinalin tamamı veya bir kısmı.</summary>
    [Range(0.001, KolonTavani.UstAdet)] public decimal Adet { get; set; }

    /// <summary>
    /// Geri ödenecek/tahsil edilecek TL. Boşsa orantılı tutar kullanılır;
    /// verilirse orantılı tutarı AŞAMAZ.
    /// </summary>
    [Range(0, KolonTavani.UstTutar)] public decimal? IadeTutariTl { get; set; }

    [Required, RegularExpression("NAKIT|KART|HAVALE|VERESIYE")]
    public string OdemeTuru { get; set; } = null!;

    /// <summary>NAKIT/KART/HAVALE için zorunlu; VERESIYE'de boş.</summary>
    public string? KasaId { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;

    public MasakBilgiDto? Masak { get; set; }
    /// <summary>
    /// PAKET 31 — YALNIZ KART ödemesinde: tahsilatın geçtiği POS cihazı.
    /// Boşsa şubede TEK aktif cihaz varsa o seçilir; birden çok varsa istek
    /// REDDEDİLİR (ilk cihaz SEÇİLMEZ — fail-closed).
    /// </summary>
    [MaxLength(36)] public string? PosCihazId { get; set; }

    /// <summary>PAKET 31 — kart taksit adedi (1 = tek çekim).</summary>
    [Range(1, 12)] public int? PosTaksitAdedi { get; set; }

}

public sealed class TakasDto
{

    /// <summary>
    /// Bu borcun vadesi — İŞLEM GÜNÜNDEN İTİBAREN GÜN sayısı. Boşsa carinin
    /// varsayılan vadesi, o da boşsa 0 (aynı gün muaccel) kullanılır.
    /// </summary>
    [Range(0, 3650)] public int? VadeGun { get; set; }
    /// <summary>Müşteri cari</summary>
    public string? CariId { get; set; }

    /// <summary>Müşteriden alınan eski ürün (takas edilecek)</summary>
    [Required] public string EskiUrunAyarId { get; set; } = null!;
    // PAKET PATCH-3: aralıksızdı (negatif/7·10²⁸ kabul) → çarpımda OverflowException 500'ü.
    // PAKET GOC-C (#7): Range(0,…) 0 gramı ve 0 TL'yi geçiriyordu → alt sınır 0'ın ÜSTÜ (servis yolunda TakasPozitif).
    [Range(0.0001, KolonTavani.UstAdet)] public decimal EskiUrunGram { get; set; }
    [Range(0.000001, KolonTavani.UstTutar)] public decimal EskiUrunBirimFiyatTlHasGram { get; set; }
    /// <summary>PAKET ISLEM-BANT — bant dışı has fiyatı bilerek kaydet (yalnız SAHİP/MÜDÜR; IslemFiyatKapisi).</summary>
    public bool? BantOnay { get; set; }
    /// <summary>PAKET ISLEM-BANT — bant aşımı gerekçesi (K30 ≥ 20; servis içinde denetlenir).</summary>
    [StringLength(1000)] public string? BantGerekce { get; set; }

    /// <summary>Müşteriye verilen yeni ürün</summary>
    [Required] public string YeniUrunId { get; set; } = null!;

    [Range(0.001, KolonTavani.UstAdet)] public decimal YeniUrunAdet { get; set; }

    [Range(0.000001, KolonTavani.UstTutar)] public decimal YeniUrunBirimFiyatTl { get; set; }   // PAKET GOC-C (#7): 0 TL verilen ürün yok

    /// <summary>Fark ödemesi — pozitif = müşteri öder, negatif = biz öderiz</summary>
    public string? KasaId { get; set; }

    /// <summary>
    /// Fark ödemesinin aracı: NAKIT · KART · HAVALE. Kasa verilmişse anlamlıdır.
    ///
    /// ⚠ Eskiden sorulmuyordu: kasa varsa hareket koşulsuz "NAKIT" damgalanıyor,
    /// bu da tevsik kümülatifini BESLİYORDU — ama takas kapıyı hiç ÇAĞIRMIYORDU.
    /// Yani başkasının haddini doldururken kendisi denetlenmiyordu. Varsayılan
    /// NAKIT'tir (fail-closed: alan gönderilmezse en sıkı kapı işler).
    /// </summary>
    [RegularExpression("NAKIT|KART|HAVALE")]
    public string OdemeTuru { get; set; } = "NAKIT";

    [MaxLength(500)] public string? Aciklama { get; set; }

    [Required] public string KurSnapshotId { get; set; } = null!;

    /// <summary>MASAK bloğu — eşik altında boş. Kimlik eşiğinde (işlem gününün yasal_parametreler değeri; 07.10.2026'dan itibaren 370.000 TL) ve amaç beyanı kademesinde (200.000 TL, TEYİTSİZ) kapı zorunlu kılar.</summary>
    public MasakBilgiDto? Masak { get; set; }

    /// <summary>
    /// PAKET PARA-DEMO (madde 3, yasal) — müşteriden ALINAN eski altının SATICI kimliği.
    /// <para>
    /// Takas (mal değişimi) iki satıştır: TBK md.283 "taraflardan her biri, vermeyi
    /// üstlendiği şey bakımından satıcı" sayılır; dükkân eski altını müşteriden SATIN
    /// ALIR. Belge düzenleme zorunluluğu olmayandan alınan mal için VUK md.234 gider
    /// pusulası (satanın adı, soyadı, adresi) düzenlenir. Hurda alışı ve satışta eski
    /// altınla ödeme bu kapıdan zaten geçiyordu; takas kimliksiz kaydediliyordu.
    /// Kapı AYNIDIR (GiderPusulasiKapisi.Dogrula) ve FAIL-CLOSED'dur: blok yoksa 400.
    /// </para>
    /// </summary>
    public Kuyumcu.Api.Modules.Hurda.HurdaSaticiDto? Satici { get; set; }

    /// <summary>
    /// PAKET 25 — OPSİYONEL reyon: malın fiziksel olarak hangi reyondan
    /// çıkacağı/gireceği. Boş bırakılırsa çıkışta önce ATANMAMIŞ mal tükenir,
    /// yetmezse reyonlar ad sırasıyla düşülür ve yanıtta <c>yerlesimUyarisi</c>
    /// döner (sessizce yutulmaz). Girişte ürünün varsayılan reyonu (aynı şubede
    /// ve aktifse) kullanılır.
    /// </summary>
    [MaxLength(36)] public string? ReyonId { get; set; }

    /// <summary>
    /// PAKET 31 — YALNIZ KART ödemesinde: tahsilatın geçtiği POS cihazı.
    /// Boşsa şubede TEK aktif cihaz varsa o seçilir; birden çok varsa istek
    /// REDDEDİLİR (ilk cihaz SEÇİLMEZ — fail-closed).
    /// </summary>
    [MaxLength(36)] public string? PosCihazId { get; set; }

    /// <summary>PAKET 31 — kart taksit adedi (1 = tek çekim).</summary>
    [Range(1, 12)] public int? PosTaksitAdedi { get; set; }

}

public sealed class IscilikDto
{

    /// <summary>
    /// Bu borcun vadesi — İŞLEM GÜNÜNDEN İTİBAREN GÜN sayısı. Boşsa carinin
    /// varsayılan vadesi, o da boşsa 0 (aynı gün muaccel) kullanılır.
    /// </summary>
    [Range(0, 3650)] public int? VadeGun { get; set; }
    /// <summary>İşçilik talep eden müşteri</summary>
    [Required] public string CariId { get; set; } = null!;

    /// <summary>İşçilik yapılan ürün (varsa)</summary>
    public string? UrunId { get; set; }

    /// <summary>İşçilik ücreti TL (Nest: @IsPositive — sıfırdan büyük)</summary>
    [Range(0d, KolonTavani.UstTutar, MinimumIsExclusive = true)] public decimal IscilikTl { get; set; }

    [MaxLength(500)] public string? Aciklama { get; set; }

    /// <summary>Peşin tahsilat yapılıyorsa kasa</summary>
    public string? KasaId { get; set; }

    /// <summary>
    /// Tahsilat aracı: NAKIT · KART · HAVALE. Kasa verilmişse anlamlıdır.
    /// Takasla aynı gerekçe: eskiden koşulsuz "NAKIT" damgalanıyor ve tevsik
    /// kapısı hiç çağrılmıyordu. Varsayılan NAKIT (fail-closed).
    /// </summary>
    [RegularExpression("NAKIT|KART|HAVALE")]
    public string OdemeTuru { get; set; } = "NAKIT";

    [Required] public string KurSnapshotId { get; set; } = null!;

    /// <summary>MASAK bloğu — eşik altında boş. Kimlik eşiğinde (işlem gününün yasal_parametreler değeri; 07.10.2026'dan itibaren 370.000 TL) ve amaç beyanı kademesinde (200.000 TL, TEYİTSİZ) kapı zorunlu kılar.</summary>
    public MasakBilgiDto? Masak { get; set; }
    /// <summary>
    /// PAKET 31 — YALNIZ KART ödemesinde: tahsilatın geçtiği POS cihazı.
    /// Boşsa şubede TEK aktif cihaz varsa o seçilir; birden çok varsa istek
    /// REDDEDİLİR (ilk cihaz SEÇİLMEZ — fail-closed).
    /// </summary>
    [MaxLength(36)] public string? PosCihazId { get; set; }

    /// <summary>PAKET 31 — kart taksit adedi (1 = tek çekim).</summary>
    [Range(1, 12)] public int? PosTaksitAdedi { get; set; }

}

// ── Servis ───────────────────────────────────────────────────────────────────

// ⚠ PAKET P-21: `partial` — Y-17/Y-19/Y-20/Y-23 metotları
// `SatisModulu.P21.cs` dosyasındadır. Sınıf bölündü ki bu 2500 satırlık dosyaya
// ikinci bir el değmesin (plan § 7 TEK EL zinciri); private yardımcılar
// (SubeBelirle · StokDegistir · CariBakiyeGuncelle · KasaGuncelle · Yuvarla*)
// KOPYALANMADAN paylaşılsın — kopyalanan aritmetik bir gün ayrışırdı.
public sealed partial class SatisServisi(
    KuyumcuDbContext db, ILogger<SatisServisi> logger, MasakKuralMotoru? masakMotoru = null,
    HurdaAyarlari? hurdaAyarlari = null, Tevsik.TevsikKurali? tevsikKurali = null,
    Pos.PosAyarlari? posAyarlari = null, YasalParametreServisi? yasal = null)
{
    // Fiyat listesi/kampanya teklifi — ince bir db sarmalayıcısı; DI'ye eklemek yerine
    // burada kurulur ki mevcut ctor imzası ve onu kuran testler değişmesin.
    private readonly Fiyat.FiyatServisi fiyat = new(db);

    /// <summary>
    /// Paket 25 — stok ÇIKIŞ/GİRİŞİNİN reyon bacağı. `fiyat` ile aynı gerekçeyle
    /// burada kurulur: mevcut ctor imzası ve onu kuran onlarca test değişmesin.
    /// Denetim kaydı BURADA yazılmaz — satış/alış zaten deftere ve denetime
    /// yazıyor; reyon kırılımı o işlemin bir parçasıdır, ayrı bir olay değildir.
    /// </summary>
    private readonly Yerlesim.YerlesimServisi yerlesim = new(db);

    /// <summary>
    /// 459 s. VUK Genel Tebliği tevsik kapısı (PLAN-2 Adım 2) — aynı gerekçe: null → kontrol
    /// yok YAZILMAZ, varsayılan eşiklerle kurulur.
    /// </summary>
    private readonly Tevsik.TevsikKapisi tevsik =
        tevsikKurali is null ? Tevsik.TevsikKapisi.Varsayilan() : new(tevsikKurali);

    // DI gerçek motoru verir; testlerdeki `new SatisServisi(db, logger)` VARSAYILAN eşiklerle
    // kurulu motoru alır. Null → kontrol yok YAZILMADI (SIRADAKI-ADIM-KARAR §2/A).
    // PAKET B-3 (D2 · K2): Peşin Müşteri sınırı yapılandırılmış tevsik haddini de görür.
    private readonly MasakKapisi masak = new(
        masakMotoru ?? new MasakKuralMotoru(new MasakEsikleri()), tevsikKurali?.Esikler);

    /// <summary>
    /// PAKET 31 — POS davranış anahtarları. null → VARSAYILANLAR (CihazZorunlu
    /// KAPALI) kullanılır; yani paket öncesi davranış aynen sürer ve mevcut
    /// yüzlerce `new SatisServisi(db, logger)` testi değişmeden kalır.
    /// </summary>
    private readonly Pos.PosAyarlari pos = posAyarlari ?? new Pos.PosAyarlari();

    /// <summary>Hurda alış kapısı (Adım 6) — aynı gerekçe: null → kontrol yok YAZILMAZ.</summary>
    private readonly GiderPusulasiKapisi hurda =
        hurdaAyarlari is null ? GiderPusulasiKapisi.Varsayilan() : new(hurdaAyarlari);

    /// <summary>
    /// Maliyet partisi motoru. Stok her değiştiğinde maliyet defteri de yazılır;
    /// ikisi ayrı düşerse kâr-zarar yalan söyler.
    /// </summary>
    private readonly Maliyet.MaliyetMotoru maliyet = new(db);

    // ─── Satış ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Paralel defter mantığı:
    ///   hareketler.tlTutari   = +toplamTl (satış alacağı / kasa girişi)
    ///   hareketler.hasGrami   = -toplamHas (stoktan çıkış)
    ///   stoklar               -= adet / toplamHasGram
    ///   cari_bakiyeler.tl     += toplamTl  (YALNIZ VERESIYE ise cari borçlanır)
    ///   kasalar.bakiye        += toplamTl  (nakit/kart/havale ise kasaya girer)
    /// Has bacağı cariye yazılmaz — satış TL ile kapanır (K4).
    /// </summary>
    public async Task<object> Satis(SatisOlusturDto dto, AktifKullanici islem)
    {
        // PAKET B-4 (G-1): DTO regex'i yalnız controller yolunda çalışır; servis
        // doğrudan çağrılsa da tek etiketli ALTIN yolu açılmaz (fail-closed).
        if (dto.OdemeTuru == OdemeSepeti.AltinArac)
            throw new BadRequestException(OdemeSepeti.TekEtiketliAltinMesaji);
        // P-23: DTO regex'i yalnız controller yolunda koşar — servis ödeme türsüz satış yazmaz.
        OdemeTutarKapisi.EtiketGecerli(dto.OdemeTuru, ["NAKIT", "KART", "HAVALE", "VERESIYE"]);

        var subeId = SubeBelirle(islem, "Satış için şube seçimi gerekli");

        // K1: yabancı kimlikler transaction açılmadan önce doğrulanır
        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);
        await IslemDogrulayici.CariDogrula(db, dto.CariId, islem.TenantId);
        // PAKET P-16 (B-31): SATIŞ carisi ayrıca AKTİF olmalı — ekran pasif
        // cariyi listeden gizler, sunucu gizlemeye GÜVENMEZ (fail-closed).
        await IslemDogrulayici.CariAktifDogrula(db, dto.CariId, islem.TenantId);

        // PAKET S2-5b: satışı YAPAN (yoksa oturum kullanıcısı) — tx öncesi doğrulanır.
        var satici = await SaticiBelirle(dto.PersonelId, islem);

        var urun = await UrunBul(dto.UrunId, islem.TenantId);
        Urun.UrunAdetKapisi.Dogrula(urun.Birim, dto.Adet, "adet");   // PAKET GOC-C (#6 · K14): ADET birimli üründe miktar tam sayı.

        // Kuyum Ticareti Yönetmeliği md.11/(e): kökeni bildirilmemiş taşlı ürün
        // satışa sunulamaz. Göç öncesi kayıtlar burada yakalanır.
        Urun.TasKapisi.SatisaUygunMu(urun);

        // Konsinye mal bize ait değildir; sahibi/bedeli eksikse satılamaz —
        // aksi hâlde mal sahibine borç doğmaz ve kâr yanlış hesaplanır.
        Urun.KonsinyeKapisi.SatisaUygunMu(urun);

        var stok = await StokBul(dto.UrunId, subeId, islem.TenantId);

        // ── PAKET P-21 (Y-23) — SATILABİLİR STOK ───────────────────────────
        // ⚠ `stok.Adet` ARTIK TEK BAŞINA YETMEZ: rezerve (bloke) mal fiziken
        // raftadır ama SÖZ VERİLMİŞTİR. Eski kontrol bıraksaydı aynı mal iki
        // müşteriye satılır, ikincisi teslim alamazdı ve hata ancak müşteri
        // kapıya geldiğinde görünürdü. Kapı: BlokeKapisi.Satilabilir.
        BlokeKapisi.SatilabilirDogrula(stok, dto.Adet, "Satış");

        var iscilikTl = dto.IscilikTl ?? 0m;
        var toplamTl  = dto.Adet * dto.BirimFiyatTl + iscilikTl;
        // P-23 ODEME_KALEMSIZ_YOL: 0 TL'lik etiketli satış kalem doğurmaz → 400.
        OdemeTutarKapisi.SifirOlamaz(toplamTl, "Satış",
            "Birim fiyatı girin; hediye verilecekse satış fişinde bedelli bir kalemin yanına 'Hediye' olarak ekleyin.",
            "birimFiyatTl");
        var toplamHas = dto.Adet * urun.HasGrami;
        var now       = DateTime.UtcNow;

        var masakGerek = await masak.DogrulaKumulatif(
            db, islem.TenantId, dto.CariId, toplamTl, dto.Masak, now, islem);   // MASAK Faz 1 — tx açılmadan

        if (dto.OdemeTuru != "VERESIYE" && string.IsNullOrEmpty(dto.KasaId))
            throw new BadRequestException("Nakit/Kart/Havale satış için kasa ID gerekli");

        // K6: TL tutarı yalnız TL kasasına yazılabilir
        Data.Kasa? kasa = dto.OdemeTuru != "VERESIYE"
            ? await IslemDogrulayici.KasaDogrula(db, dto.KasaId!, islem.TenantId, "TRY", now)
            : null;

        // 459 s. VUK Genel Tebliği — TEVSİK KAPISI. MASAK ile aynı desen: transaction'dan
        // ÖNCE, ki hadde takılan işlemde hiçbir yazma başlamasın.
        if (dto.OdemeTuru == "NAKIT")
            await tevsik.Dogrula(db, islem.TenantId, dto.CariId, toplamTl, now,
                islemToplamTutari: toplamTl);

        // VADE + KREDİ LİMİTİ — veresiye satış bu pakete kadar limiti HİÇ görmüyordu;
        // limit yalnız CariModulu.VeresiyeGir ekranından geçenler için işliyordu.
        var vade = await Vade.VadeKapisi.Hazirla(
            db, islem.TenantId, dto.CariId, dto.VadeGun,
            dto.OdemeTuru == "VERESIYE" ? toplamTl : 0m, alacakYonu: true, now, dto.KurSnapshotId);

        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, dto.CariId, now);   // PAKET MASAK-ESZAMAN: işlem + CARİ SATIRI KİLİDİ
        // PAKET MASAK-ESZAMAN: kümülatif kapılar kilit altında, güncel toplamla YİNELENİR (eşik/kural aynı).
        masakGerek = await masak.KilitAltindaYinele(db, islem.TenantId, dto.CariId, toplamTl, dto.Masak, now, masakGerek, islem);
        if (dto.OdemeTuru == "NAKIT")
            await tevsik.Dogrula(db, islem.TenantId, dto.CariId, toplamTl, now,
                islemToplamTutari: toplamTl);

        // Liste fiyatı / kampanya — FEY md.11 için satıra yazılır. Kasiyer fiyatı
        // elle değiştirmiş olabilir: indirim = (liste − uygulanan) × adet, ≥ 0.
        var hasKuru = await db.KurSnapshots.AsNoTracking()
            .Where(s => s.Id == dto.KurSnapshotId && s.TenantId == islem.TenantId)
            .Select(s => s.AltinHasFiyati).SingleAsync();
        var teklif = await fiyat.Teklif(urun, dto.CariId, hasKuru, DateOnly.FromDateTime(Zaman.UtcdenYerele(now)));
        decimal? listeFiyati = teklif?.ListeFiyati;
        decimal? indirim = teklif is null ? null
            : Yuvarla2(Math.Max(0m, (teklif.ListeFiyati - dto.BirimFiyatTl) * dto.Adet));
        var kampanyaId = teklif is { IndirimTutari: > 0m } && indirim > 0m ? teklif.KampanyaId : null;

        // 1. Ana hareket kaydı
        var hareketId = Kimlik.Ulid();
        Data.HareketFabrikasi.Ekle(db,
            dto.OdemeTuru == "VERESIYE"
                ? (string.IsNullOrEmpty(dto.CariId) ? Data.OdemeIzi.Yok : Data.OdemeIzi.Veresiyeden(toplamTl, vade))
                : Data.OdemeIzi.Etiketten(dto.OdemeTuru, toplamTl, vade: null),
            new Data.Hareket
        {
            Id            = hareketId,
            TenantId      = islem.TenantId,
            SubeId        = subeId,
            Tur           = "SATIS",
            Tarih         = now,
            Adet          = dto.Adet,   // P-08 / Y-14
            ListeFiyati   = listeFiyati,
            IndirimTutari = indirim,
            KampanyaId    = kampanyaId,
            Aciklama      = dto.Aciklama,
            TlTutari      = Yuvarla2(toplamTl),
            HasGrami      = Yuvarla6(-toplamHas),   // stoktan çıkış
            // K20-1: has bacağının madeni — ürünün ayarından. Altın dışı gram
            // ALTIN toplamına karışamaz.
            Maden         = Kuyumcu.Core.Domain.Ayarlar.MadenBul(urun.AyarId) ?? Kuyumcu.Core.Domain.Maden.Altin,
            OdemeTuru     = dto.OdemeTuru,
            KurSnapshotId = dto.KurSnapshotId,
            CariId        = dto.CariId,
            UrunId        = dto.UrunId,
            KullaniciId   = islem.Id,
            PersonelId    = satici,   // S2-5b: SATAN (kaydı giren değil)
            Olusturuldu   = now,
        });
        masak.Kaydet(db, masakGerek, hareketId, dto.CariId, toplamTl, dto.Masak, islem, now, MasakKaynakTuru.Hareket);   // MASAK Faz 2

        // 2. Stok azalt + satılan malın maliyetini partilerden düş
        // PAKET 25: reyon seçildiyse YALNIZ o reyondan düşer (fail-closed);
        // seçilmediyse önce ATANMAMIŞ tükenir ve gerekirse uyarı döner.
        var yerlesimUyarisi = await StokDegistir(
            dto.UrunId, subeId, islem.TenantId, -dto.Adet, urun.HasGrami, now, dto.ReyonId);
        await maliyet.PartiTuket(islem.TenantId, subeId, dto.UrunId, dto.Adet, now,
            await maliyet.Yontem(islem.TenantId), hareketId);

        // 2b. Konsinye mal satıldıysa bedeli SAHİBİNE borçlanılır.
        // Bu satırın olmaması hâlinde mal bedava satılmış gibi görünür ve
        // kâr, mal sahibinin payı kadar şişer.
        if (urun.Konsinye && urun.KonsinyeBirimBedel is > 0m && !string.IsNullOrEmpty(urun.KonsinyeCariId))
        {
            var konsinyeBorc = Yuvarla2(dto.Adet * urun.KonsinyeBirimBedel.Value);
            await CariBakiyeGuncelle(urun.KonsinyeCariId, islem.TenantId, -konsinyeBorc, 0m, now);
            logger.LogInformation(
                "[{TenantId}] KONSINYE: {UrunId} satıldı, {Borc} TL {CariId} cariye borçlanıldı",
                islem.TenantId, dto.UrunId, konsinyeBorc, urun.KonsinyeCariId);
        }

        // 3a. VERESİYE → cari bakiye artır (müşteri bize borçlanır)
        if (dto.OdemeTuru == "VERESIYE" && !string.IsNullOrEmpty(dto.CariId))
            await CariBakiyeGuncelle(dto.CariId, islem.TenantId, toplamTl, 0m, now);

        // 3b. Nakit/Kart/Havale → kasaya ekle + kasa hareketi kaydet
        if (kasa is not null)
        {
            var kasaHareketId = await KasaGuncelle(kasa, islem.TenantId, toplamTl, now, hareketId, dto.OdemeTuru);
            // PAKET 31 — KART bacağı POS alacak defterine de yazılır (yan defter).
            await PosDefterineYaz(kasa, islem, dto.OdemeTuru, dto.PosCihazId, dto.PosTaksitAdedi,
                toplamTl, hareketId, kasaHareketId, dto.KurSnapshotId, dto.CariId, now);
        }

        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] SATIS: ürün={UrunId} adet={Adet} toplam={ToplamTl} TL",
            islem.TenantId, dto.UrunId, dto.Adet, toplamTl);

        return await HareketGetir(dto.UrunId, islem.TenantId, yerlesimUyarisi);
    }

    // ─── Alış ─────────────────────────────────────────────────────────────────
    /// <summary>
    ///   hareketler.tlTutari   = -toplamTl  (kasadan çıkış / tedarikçi borcu)
    ///   hareketler.hasGrami   = +toplamHas (stoka giriş)
    ///   stoklar               += adet / toplamHasGram
    ///   kasalar.bakiye        -= ödenen tutar
    ///   cari_bakiyeler.tl     -= ÖDENMEYEN kısım (biz tedarikçiye borçlanırız)
    /// K2: ödenen kısım cariye YAZILMAZ — peşin alış borç bırakmaz.
    /// </summary>
    public async Task<object> Alis(AlisOlusturDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem, "Alış için şube seçimi gerekli");

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);
        await IslemDogrulayici.CariDogrula(db, dto.CariId, islem.TenantId);

        var urun      = await UrunBul(dto.UrunId, islem.TenantId);
        Urun.UrunAdetKapisi.Dogrula(urun.Birim, dto.Adet, "adet");   // PAKET GOC-C (#6 · K14): ADET birimli üründe miktar tam sayı.
        var toplamTl  = Yuvarla2(dto.Adet * dto.BirimFiyatTl);
        var now       = DateTime.UtcNow;

        // PAKET L-1b: kapılar (MASAK · kasa/gün kilidi · vade · tevsik) TEK yerde —
        // alış fişi ucu AYNI yardımcıyı fişin BRÜT toplamıyla çağırır.
        var kapi = await AlisKapilari(dto.CariId, dto.KasaId, dto.OdenenTl, dto.VadeGun,
            dto.OdemeTuru, toplamTl, dto.Masak, dto.KurSnapshotId, islem, now);
        await AlisTevsikKapisi(dto.CariId, dto.OdemeTuru, kapi.Odenen, toplamTl, islem, now);

        // URUN-ACILIS-ATOMIK: dış işlem (mal kabul) varsa ona KATILIR, commit sahibinde; tek başına davranış aynı.
        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, dto.CariId, now);   // PAKET MASAK-ESZAMAN: işlem + CARİ SATIRI KİLİDİ
        // PAKET MASAK-ESZAMAN: kümülatif kapılar kilit altında, güncel toplamla YİNELENİR (eşik/kural aynı).
        kapi = kapi with { MasakGerek = await masak.KilitAltindaYinele(db, islem.TenantId, dto.CariId, toplamTl, dto.Masak, now, kapi.MasakGerek, islem) };
        await AlisTevsikKapisi(dto.CariId, dto.OdemeTuru, kapi.Odenen, toplamTl, islem, now);

        var (hareketId, alisYerlesimUyarisi) = await AlisKalemiYaz(
            new AlisBaglami(subeId, dto.CariId, dto.KurSnapshotId, dto.Aciklama, dto.OdemeTuru,
                            dto.ReyonId, kapi.Kasa, kapi.Vade, FisId: null, now),
            urun, dto.Adet, dto.BirimFiyatTl, toplamTl, kapi.Odenen, islem);
        masak.Kaydet(db, kapi.MasakGerek, hareketId, dto.CariId, toplamTl, dto.Masak, islem, now, MasakKaynakTuru.Hareket);   // MASAK Faz 2

        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] ALIS: ürün={UrunId} adet={Adet} toplam={ToplamTl} TL (ödenen {Odenen})",
            islem.TenantId, dto.UrunId, dto.Adet, toplamTl, kapi.Odenen);

        return await HareketGetir(dto.UrunId, islem.TenantId, alisYerlesimUyarisi);
    }

    // ─── PAKET L-1b — ALIŞ FİŞİ (çok kalem, TEK işlem) ───────────────────────
    //
    // ⚠ NEDEN (KRİTİK yasal, L-1 doğrulayıcısının ORTA bulgusu): ekran çok kalemli
    //   ALIŞ'ı kalem başına ayrı POST /islemler/alis ile gönderiyordu. Müşteriden iki
    //   parça alınıp toplam haddi aşan nakit ödeme iki kayda bölünüyor, tevsik kapısı
    //   (459/575 s. VUK GT; had yapılandırmada) FİŞ toplamını hiç görmüyordu —
    //   Peşin Müşteri'de kümülatif toplam da yoktur. Bu uç fişi TEK işlem sayar:
    //   • tevsik + MASAK + kasa para birimi + gün kilidi fişin BRÜT toplamıyla BİR KEZ
    //     (AlisKapilari — tek kalemli uçla AYNI yardımcı; kural gevşemez/sıkılaşmaz);
    //   • kalemler aynı fis_id'yi taşır; tek transaction — ya hep ya hiç;
    //   • kalem başına yazım (hareket · stok · maliyet partisi · cari · kasa) tek
    //     kalemli alışla AYNI yardımcıdan geçer (AlisKalemiYaz) — kopya yok.
    public async Task<object> AlisFisi(AlisFisiDto dto, AktifKullanici islem)
    {
        // DTO DataAnnotations yalnız controller yolunda işler → servis içi elle kontrol.
        if (dto.Kalemler is null || dto.Kalemler.Count == 0)
            throw new BadRequestException("Alış fişinde en az bir kalem olmalıdır").AlanIle("kalemler");
        if (dto.Kalemler.Count > AlisFisiKalemTavani)
            throw new BadRequestException($"Bir alış fişinde en çok {AlisFisiKalemTavani} kalem olabilir").AlanIle("kalemler");
        if (dto.OdemeTuru is not ("NAKIT" or "KART" or "HAVALE"))
            throw new BadRequestException("Ödeme türü NAKIT, KART ya da HAVALE olmalıdır").AlanIle("odemeTuru");
        foreach (var k in dto.Kalemler)
        {
            if (k is null || string.IsNullOrWhiteSpace(k.UrunId))
                throw new BadRequestException("Her kalemde ürün seçilmelidir").AlanIle("kalemler");
            // PAKET PATCH-3 (PATCH-2 kalıbı): ÖNCE kolon hanesine (decimal(12,3)) yuvarla,
            // SONRA ≤ 0 — 0,0004 eskiden kontrolü geçip kolona 0,000 yazılıyordu.
            k.Adet = Yuvarla3(k.Adet);
            if (k.Adet <= 0m)
                throw new BadRequestException("Kalem adedi sıfırdan büyük olmalıdır").AlanIle("kalemler");
            KolonTavani.Denetle(k.Adet, 3, KolonTavani.D12_3, "kalemler", "Kalem adedi");
            KolonTavani.Denetle(k.BirimFiyatTl, 2, KolonTavani.D15_2, "kalemler", "Birim alış fiyatı");
            if (k.BirimFiyatTl < 0m)
                throw new BadRequestException("Birim alış fiyatı negatif olamaz").AlanIle("kalemler");
        }

        var subeId = SubeBelirle(islem, "Alış için şube seçimi gerekli");

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);
        await IslemDogrulayici.CariDogrula(db, dto.CariId, islem.TenantId);

        // Kalemler: ürün + tutar (kalem başına 2 haneye — tek kalemli uçla aynı yuvarlama).
        var kalemler = new List<(AlisFisKalemDto Dto, Data.Urun Urun, decimal Tutar)>(dto.Kalemler.Count);
        foreach (var k in dto.Kalemler)
        {
            var alisUrun = await UrunBul(k.UrunId, islem.TenantId);
            Urun.UrunAdetKapisi.Dogrula(alisUrun.Birim, k.Adet, "kalemler", alisUrun.Ad);   // PAKET GOC-C (#6 · K14): ADET birimli üründe miktar tam sayı.
            kalemler.Add((k, alisUrun, Yuvarla2(k.Adet * k.BirimFiyatTl)));
        }

        var fisToplam = Yuvarla2(kalemler.Sum(x => x.Tutar));
        var now = DateTime.UtcNow;

        // ⚠ TEK KEZ, FİŞ TOPLAMIYLA (MASAK · kasa/gün kilidi · vade) — kalem başına çağrılsaydı fiş hadde hiç takılmazdı.
        var kapi = await AlisKapilari(dto.CariId, dto.KasaId, dto.OdenenTl, dto.VadeGun,
            dto.OdemeTuru, fisToplam, dto.Masak, dto.KurSnapshotId, islem, now);

        // Ödenen / ödenmeyen kısım kalemlere sırayla dağıtılır (satış fişiyle aynı
        // dağıtıcı); Σ pay = fiş toplamı, aksi hâlde dağıtıcı patlar (sessiz geçmez).
        var paylar = OdemeSepeti.PaylariDagit(
            kalemler.Select(x => x.Tutar).ToList(), kapi.Odenen, kapi.Kalan);

        // ⚠ TEVSİK: fişin ÖDENEN nakdi ve BRÜT toplamıyla TEK KEZ (kalem başına değil).
        await AlisTevsikKapisi(dto.CariId, dto.OdemeTuru, kapi.Odenen, fisToplam, islem, now);

        var fisId = Kimlik.Ulid();
        var baglam = new AlisBaglami(subeId, dto.CariId, dto.KurSnapshotId, dto.Aciklama, dto.OdemeTuru,
                                     dto.ReyonId, kapi.Kasa, kapi.Vade, fisId, now);

        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, dto.CariId, now);   // PAKET MASAK-ESZAMAN: işlem + CARİ SATIRI KİLİDİ
        // PAKET MASAK-ESZAMAN: kümülatif kapılar kilit altında, güncel toplamla YİNELENİR (eşik/kural aynı).
        kapi = kapi with { MasakGerek = await masak.KilitAltindaYinele(db, islem.TenantId, dto.CariId, fisToplam, dto.Masak, now, kapi.MasakGerek, islem) };
        await AlisTevsikKapisi(dto.CariId, dto.OdemeTuru, kapi.Odenen, fisToplam, islem, now);

        var hareketIdleri = new List<string>(kalemler.Count);
        string? yerlesimUyarisi = null;
        for (var i = 0; i < kalemler.Count; i++)
        {
            var (k, urun, tutar) = kalemler[i];
            var (hareketId, kalemUyari) = await AlisKalemiYaz(
                baglam, urun, k.Adet, k.BirimFiyatTl, tutar, paylar[i].Nakit, islem);
            hareketIdleri.Add(hareketId);
            yerlesimUyarisi ??= kalemUyari;   // çağrı HER kalemde koşar; ??= yalnız atamayı korur
        }

        // MASAK kaydı fişin İLK hareketine, fiş TOPLAMIYLA (fiş tek işlemdir).
        masak.Kaydet(db, kapi.MasakGerek, hareketIdleri[0], dto.CariId, fisToplam, dto.Masak, islem, now, MasakKaynakTuru.Hareket);

        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] ALIS_FISI: fiş={FisId} kalem={Kalem} toplam={ToplamTl} TL (ödenen {Odenen})",
            islem.TenantId, fisId, kalemler.Count, fisToplam, kapi.Odenen);

        // UX-S27: sonuç okunur — fiş no, kalem sayısı, ödenen/kalan, ilk kalem (yazdırma: belge fişin tüm kalemlerini basar).
        return new
        {
            fisId,
            hareketId = hareketIdleri[0],
            hareketIdleri,
            kalemSayisi = kalemler.Count,
            toplamTl = fisToplam,
            odenenTl = kapi.Odenen,
            kalanTl = kapi.Kalan,
            odemeTuru = dto.OdemeTuru,
            yerlesimUyarisi,
        };
    }

    /// <summary>Bir alış fişinin kalem tavanı (tek istekte yazılabilecek en çok satır).</summary>
    public const int AlisFisiKalemTavani = 100;

    private sealed record AlisKapiSonucu(
        MasakGereksinim MasakGerek, Data.Kasa? Kasa, decimal Odenen, decimal Kalan, DateOnly? Vade);

    /// <summary>
    /// PAKET L-1b — alışın MASAK · kasa (para birimi + gün kilidi) · vade kapıları TEK yerde
    /// (tevsik: <see cref="AlisTevsikKapisi"/>). Tek kalemli alış ile alış fişi AYNI sırayla ve
    /// AYNI tutarla (fişte: BRÜT fiş toplamı) çağırır.
    /// </summary>
    private async Task<AlisKapiSonucu> AlisKapilari(
        string? cariId, string? kasaId, decimal? odenenTl, int? vadeGun, string odemeTuru,
        decimal toplamTl, MasakBilgiDto? masakBilgi, string kurSnapshotId, AktifKullanici islem, DateTime now)
    {
        var masakGerek = await masak.DogrulaKumulatif(
            db, islem.TenantId, cariId, toplamTl, masakBilgi, now, islem);   // MASAK Faz 1
        // Kasa: para birimi (TRY) + gün kilidi (an = now) IslemDogrulayici.KasaDogrula'da.
        var (kasa, odenen, kalan) = await OdemeCoz(kasaId, odenenTl, toplamTl, islem.TenantId, now);

        // Ödenmeyen kısım BİZİM tedarikçiye borcumuzdur → alacakYonu: false
        // (kendi borcumuza kredi limiti uygulanmaz; vade yine takip edilir).
        var vade = await Vade.VadeKapisi.Hazirla(
            db, islem.TenantId, cariId, vadeGun, kalan, alacakYonu: false, now, kurSnapshotId);

        return new AlisKapiSonucu(masakGerek, kasa, odenen, kalan, vade);
    }

    /// <summary>
    /// 459 s. VUK Genel Tebliği — tediye de hadde tabidir; kasadan ÇIKAN nakit denetlenir.
    /// Artık işletmenin BEYAN ETTİĞİ ödeme aracına bakılır; her peşin ödeme
    /// nakit sayılmaz. Havale/kart zaten tevsik edilmiştir.
    /// İŞLEM TUTARI = toplam bedel (ödenen kısım DEĞİL). §4.1.2: bedel haddi
    /// aşıyorsa kısmi nakit ödeme de ihlaldir — 68.000 TL'lik alışın
    /// 10.000 TL'sini nakit vermek kurtarmaz.
    /// PAKET L-1b: tek kalemli alış ve alış fişi AYNI kapıyı çağırır; fişte tutarlar FİŞİN.
    /// </summary>
    private async Task AlisTevsikKapisi(string? cariId, string odemeTuru, decimal odenen, decimal toplamTl,
                                        AktifKullanici islem, DateTime now)
    {
        if (odenen > 0m && odemeTuru == "NAKIT")
            await tevsik.Dogrula(db, islem.TenantId, cariId, odenen, now,
                islemToplamTutari: toplamTl);
    }

    private sealed record AlisBaglami(
        string SubeId, string? CariId, string KurSnapshotId, string? Aciklama, string OdemeTuru,
        string? ReyonId, Data.Kasa? Kasa, DateOnly? Vade, string? FisId, DateTime An);

    /// <summary>
    /// PAKET L-1b — BİR alış kaleminin yazımı (tek kalemli alış ile alış fişinin
    /// ORTAK yolu): hareket (HareketFabrikasi) · stok + yerleşim · maliyet partisi ·
    /// ödenmeyen kısım cariye · ödenen kısım kasadan. Transaction'ı ÇAĞIRAN açar.
    /// </summary>
    private async Task<(string HareketId, string? YerlesimUyarisi)> AlisKalemiYaz(
        AlisBaglami b, Data.Urun urun, decimal adet, decimal birimFiyatTl,
        decimal toplamTl, decimal odenen, AktifKullanici islem)
    {
        var kalan = Yuvarla2(toplamTl - odenen);
        var toplamHas = adet * urun.HasGrami;

        var hareketId = Kimlik.Ulid();
        Data.HareketFabrikasi.Ekle(db,
            new Data.OdemeIzi(
                Nakit:    b.OdemeTuru == "NAKIT" ? odenen : 0m,
                Veresiye: string.IsNullOrEmpty(b.CariId) ? 0m : kalan,
                Vade:     string.IsNullOrEmpty(b.CariId) || kalan <= 0m ? null : b.Vade),
            new Data.Hareket
        {
            Id            = hareketId,
            TenantId      = islem.TenantId,
            SubeId        = b.SubeId,
            Tur           = "ALIS",
            Tarih         = b.An,
            Adet          = adet,   // P-08 / Y-14
            Aciklama      = b.Aciklama,
            TlTutari      = Yuvarla2(-toplamTl),    // kasadan çıkış / borç
            HasGrami      = Yuvarla6(toplamHas),    // stoka giriş
            // K20-1: has bacağının madeni — ürünün ayarından. Altın dışı gram
            // ALTIN toplamına karışamaz.
            Maden         = Kuyumcu.Core.Domain.Ayarlar.MadenBul(urun.AyarId) ?? Kuyumcu.Core.Domain.Maden.Altin,
            // Kısmi ödemede defter hâlâ KARMA der; tam ödemede beyan edilen araç yazılır.
            // P-23: bedelsiz (0 TL) mal girişi para hareketi değildir → ödeme etiketi TAŞIMAZ.
            OdemeTuru     = OdemeTutarKapisi.Etiket(toplamTl, odenen <= 0m ? "VERESIYE"
                          : odenen >= toplamTl ? b.OdemeTuru : "KARMA"),
            FisId         = b.FisId,   // PAKET L-1b: fişin kalemleri ortak fis_id taşır (tek kalemli uçta null)
            KurSnapshotId = b.KurSnapshotId,
            CariId        = b.CariId,
            UrunId        = urun.Id,
            KullaniciId   = islem.Id,
            Olusturuldu   = b.An,
        });

        // Stok artır + maliyet partisi aç.
        // Kâr-zararın tek dayanağı burasıdır: mal kaça alındı, o gün has kaçtı.
        // PAKET 25: reyon seçilmediyse ürünün VARSAYILAN reyonu (aynı şubede ve
        // aktifse) kullanılır; değilse mal ATANMAMIŞ girer ve uyarı döner.
        var yerlesimUyarisi = await StokDegistir(
            urun.Id, b.SubeId, islem.TenantId, adet, urun.HasGrami, b.An,
            b.ReyonId, urun.ReyonId);
        maliyet.PartiAc(islem.TenantId, b.SubeId, urun.Id, Data.PartiKaynagi.Alis,
            adet, urun.HasGrami, b.An,
            birimMaliyetTl: birimFiyatTl,
            altinHasFiyati: await HasFiyatiOku(b.KurSnapshotId, islem.TenantId),
            girisHareketId: hareketId, kurSnapshotId: b.KurSnapshotId);

        // Ödenmeyen kısım tedarikçiye borç (has bacağı cariye yazılmaz — K3/K4)
        if (kalan > 0m && !string.IsNullOrEmpty(b.CariId))
            await CariBakiyeGuncelle(b.CariId, islem.TenantId, -kalan, 0m, b.An);

        // Ödenen kısım kasadan — kasa satırı KALEMİN hareketine bağlanır (storno kalem başına doğru tutarı geri alır).
        if (b.Kasa is not null && odenen > 0m)
            await KasaGuncelle(b.Kasa, islem.TenantId, -odenen, b.An, hareketId, b.OdemeTuru);

        return (hareketId, yerlesimUyarisi);
    }

    // ─── Hurda alış ───────────────────────────────────────────────────────────
    /// <summary>
    /// Hurdayı has gram üzerinden fiyatlandırır, stok girişi yapmaz
    /// (hurda genelde eritilir, ayrı bir ürün sayılmaz).
    /// K3: ödenen kısım cariye yazılmaz; has bacağı hiçbir durumda cariye
    /// yazılmaz — hurda satın alınmıştır, müşterinin altın alacağı doğmaz.
    /// </summary>
    public async Task<object> HurdaAlis(HurdaAlisDto dto, AktifKullanici islem)
    {
        // ZO-ISLEM-A (doğrulayıcı 27.09, UX-S22): hurda defterinin GİRİŞİ de /hurda bakiyesi ve
        // ramat çıkışıyla AYNI şube çözümünü kullanır — şubesiz tüm-şube rolü tek aktif
        // şubeli kiracıda o şubeye yazar; 2+ aktif şubede 400 kalır (fail-closed).
        islem = await Hurda.HurdaStokKapisi.OturumSubesiCoz(db, islem);
        var subeId = SubeBelirle(islem, "Hurda alış için şube seçimi gerekli");

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);
        await IslemDogrulayici.CariDogrula(db, dto.CariId, islem.TenantId);

        var ayar = Ayarlar.Bul(dto.AyarId)
            ?? throw new BadRequestException($"Geçersiz ayar: {dto.AyarId}");
        await IslemFiyatKapisi.Denetle(db, islem, islem.TenantId, Kuyumcu.Core.Domain.Girdi.IslemFiyatBaglami.HurdaAlis, ayar.Maden, dto.BirimFiyatTlHasGram, "birimFiyatTlHasGram", dto.BantOnay, dto.BantGerekce);   // PAKET ISLEM-BANT

        var hasGrami = HasHesap.ToHasGram(dto.GramAgirlik, ayar);
        var toplamTl = Yuvarla2(hasGrami * dto.BirimFiyatTlHasGram);
        OdemeTutarKapisi.SifirOlamaz(toplamTl, "Hurda alışı",
            "Has gram fiyatını girin (güncel kur yoksa önce kuru güncelleyin).", "birimFiyatTlHasGram");   // P-23
        var now      = DateTime.UtcNow;

        // MASAK Faz 1 — BRÜT bedel üzerinden (stopaj öncesi), bağlantılı işlem
        // kümülatifiyle. Mevzuat araştırması (2026-09-15) eşiğin ödenen nete
        // değil işlem tutarına baktığını doğruladı.
        var masakGerek = await masak.DogrulaKumulatif(
            db, islem.TenantId, dto.CariId, toplamTl, dto.Masak, now, islem);
        // Adım 6 Faz 1 — VUK md.234: satıcı kimliği TUTAR EŞİĞİ OLMADAN zorunlu.
        var saticiTipi = hurda.Dogrula(dto.Satici);
        // PAKET YASAL-4 (Y4-C): KMTS kodu transaction ÖNCESİ, ürünle AYNI yardımcıdan.
        var kmtsKodu = KmtsKodu.Dogrula(dto.KmtsKodu, yasal);
        // PAKET YASAL-3 (§ 4.4 nokta 3): satıcı TCKN'si yasal listede TAM eşleşirse işlem ERTELENİR (400).
        await Masak.YaptirimServisi.KimlikTara(db, islem.TenantId, islem, Data.YaptirimNoktasi.HurdaSatici,
            dto.Satici?.Tckn, dto.Satici?.AdSoyad, string.IsNullOrEmpty(dto.CariId) ? null : dto.CariId);

        // STOPAJ: esnaf muaflığından yararlanandan alımda %2 tevkifat yapılır
        // (GVK md.94). Satıcının eline geçen NET tutardır — kasadan çıkan da,
        // satıcıya borçlanılan da net olmalıdır. Brüt bedel ve kesilen stopaj
        // gider pusulasında belge verisi olarak durur.
        // ⚠ AÇIK: kesilen stopajın vergi dairesine BORÇ olarak muhasebeleştirilmesi
        // bu sürümde YOK — vergi yükümlülüğü hesabı henüz modellenmedi.
        var stopajOran = hurda.StopajOran(saticiTipi);
        var stopajTutar = Yuvarla2(toplamTl * stopajOran);
        var odenecek = Yuvarla2(toplamTl - stopajTutar);

        var (kasa, odenen, kalan) = await OdemeCoz(dto.KasaId, dto.OdenenTl, odenecek, islem.TenantId, now);

        // Ödenmeyen kısım BİZİM tedarikçiye borcumuzdur → alacakYonu: false
        // (kendi borcumuza kredi limiti uygulanmaz; vade yine takip edilir).
        var vade = await Vade.VadeKapisi.Hazirla(
            db, islem.TenantId, dto.CariId, dto.VadeGun, kalan, alacakYonu: false, now, dto.KurSnapshotId);

        // 459 s. VUK Genel Tebliği — hurda alımında satıcıya ödenen NAKİT de hadde tabidir.
        // Kümülatif toplam cari üzerinden yürür; hurda satıcısı carisi yoksa
        // yalnız bu işlemin kendi tutarı değerlendirilir.
        // Hurda alımı 459 s. Tebliğ'in en riskli vakası — ödeme aracı beyanı esastır.
        // İŞLEM TUTARI = BRÜT bedel. Mevzuat araştırması (2026-09-15, iki mercek
        // doğruladı): hem tevsik haddi hem MASAK eşiği ödenen nete değil işlem
        // tutarına bakar. MASAK zaten brüt alıyordu (yukarıda toplamTl); tevsik
        // de aynı ölçüye getirildi.
        if (odenen > 0m && dto.OdemeTuru == "NAKIT")
            await tevsik.Dogrula(db, islem.TenantId, dto.CariId, odenen, now,
                islemToplamTutari: toplamTl);

        // Pusula numarası transaction ÖNCESİ alınır (MAX+1 okuması kilitlenmesin).
        var pusulaNo = Data.SaticiTipi.PusulaGerekir(saticiTipi)
            ? await FaturaNoUretici.SiradakiPusula(db, islem.TenantId, Zaman.UtcdenYerele(now).Year)
            : null;

        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, dto.CariId, now);   // PAKET MASAK-ESZAMAN: işlem + CARİ SATIRI KİLİDİ
        // PAKET MASAK-ESZAMAN: kümülatif kapılar kilit altında, güncel toplamla YİNELENİR (eşik/kural aynı).
        masakGerek = await masak.KilitAltindaYinele(db, islem.TenantId, dto.CariId, toplamTl, dto.Masak, now, masakGerek, islem);
        if (odenen > 0m && dto.OdemeTuru == "NAKIT")
            await tevsik.Dogrula(db, islem.TenantId, dto.CariId, odenen, now,
                islemToplamTutari: toplamTl);

        var hareketId = Kimlik.Ulid();
        Data.HareketFabrikasi.Ekle(db,
            new Data.OdemeIzi(
                Nakit:    dto.OdemeTuru == "NAKIT" ? odenen : 0m,
                Veresiye: string.IsNullOrEmpty(dto.CariId) ? 0m : odenecek - odenen,
                Vade:     string.IsNullOrEmpty(dto.CariId) || odenecek - odenen <= 0m ? null : vade),
            new Data.Hareket
        {
            Id            = hareketId,
            TenantId      = islem.TenantId,
            SubeId        = subeId,
            KmtsKodu      = kmtsKodu,   // YASAL-4 Y4-C: tek doğrulama yeri (yukarıda)
            FaturaNo      = dto.Satici!.TedarikciFaturaNo?.Trim(),
            Tur           = "HURDA_ALIS",
            Tarih         = now,
            // P-08 / Y-14 — ADET BİLİNMİYOR ve UYDURULMAZ: hurda GRAMLA alınır
            // (HurdaAlisDto'da adet alanı YOKTUR). "1 parça" yazmak bir iddia
            // üretirdi; kolon null kalır, rapor "bilinmiyor" der (Anayasa md.1).
            Aciklama      = dto.Aciklama,
            TlTutari      = Yuvarla2(-odenecek),   // kasadan çıkış / borç (stopaj sonrası NET)
            MasakOlcuTl   = toplamTl,              // YASAL-3 § 4.1: MASAK ölçüsü BRÜT bedel (stopaj öncesi) — kümülatif de brüt okur
            HasGrami      = Yuvarla6(hasGrami),    // has gram girişi (fiziki akış)
            // K20-1: hurdanın madeni satıcının getirdiği ayardan gelir.
            Maden         = Kuyumcu.Core.Domain.Ayarlar.MadenBul(ayar.Id) ?? Kuyumcu.Core.Domain.Maden.Altin,
            OdemeTuru     = odenen <= 0m ? "VERESIYE"
                          : odenen >= odenecek ? dto.OdemeTuru : "KARMA",
            KurSnapshotId = dto.KurSnapshotId,
            CariId        = dto.CariId,
            KullaniciId   = islem.Id,
            Olusturuldu   = now,
        });
        masak.Kaydet(db, masakGerek, hareketId, dto.CariId, toplamTl, dto.Masak, islem, now, MasakKaynakTuru.Hareket);   // MASAK Faz 2
        // Adım 6 Faz 2 — pusula ile hareket AYNI commit'te; "alım var, belgesi yok" doğmaz.
        // Pusulaya BRÜT verilir; stopaj ve net orada hesaplanır (belge verisi).
        var pusula = pusulaNo is null ? null : hurda.Kaydet(
            db, pusulaNo, dto.Satici!, hareketId, subeId, dto.CariId,
            dto.AyarId, dto.GramAgirlik, Yuvarla6(hasGrami), toplamTl, islem, now);

        if (kalan > 0m && !string.IsNullOrEmpty(dto.CariId))
            await CariBakiyeGuncelle(dto.CariId, islem.TenantId, -kalan, 0m, now);

        if (kasa is not null && odenen > 0m)
            await KasaGuncelle(kasa, islem.TenantId, -odenen, now, hareketId, dto.OdemeTuru);

        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] HURDA_ALIS: {GramAgirlik}g {AyarId} = {HasGrami} has, {ToplamTl} TL · satıcı={SaticiTipi} pusula={PusulaNo}",
            islem.TenantId, dto.GramAgirlik, dto.AyarId, Yuvarla6(hasGrami), toplamTl, saticiTipi, pusulaNo ?? "-");

        return new
        {
            gramAgirlik = dto.GramAgirlik,
            ayarId      = dto.AyarId,
            hasGrami    = Yuvarla6(hasGrami),
            brutTutarTl = toplamTl,
            stopajTutarTl = stopajTutar,
            odenecekTl  = odenecek,      // brüt − stopaj
            odenenTl    = odenen,
            kalanTl     = kalan,
            saticiTipi,
            giderPusulasi = pusula is null ? null : new
            {
                pusula.PusulaNo, pusula.BrutTutar, pusula.StopajOran,
                pusula.StopajTutar, pusula.NetOdenen, pusula.DuzenlemeSonTarihi,
            },
        };
    }

    // ─── Hurda satışı / ramat çıkışı (PAKET P-20 · Y-10) ─────────────────────
    /// <summary>
    /// Biriken hurdanın rafineriye (ramatçıya) çıkışı: has bacağı EKSİLİR,
    /// TL bacağı ARTAR. HURDA_ALIS ile AYNI defter hattını paylaşır.
    ///
    /// <para>
    /// ⚠ NEDEN BURADA (HurdaModulu'nda değil): bu yazma kasa, cari, MASAK,
    /// tevsik ve kur bacaklarını BİRLİKTE yazar; bu bacakların tek yeri
    /// SatisServisi'dir. İkinci bir yazma motoru açılsaydı, tevsik kümülatifi
    /// ile MASAK eşiği iki ayrı yerden yürür ve biri diğerinden sapardı.
    /// </para>
    /// <para>
    /// ⚠ STOK BACAĞI YOKTUR ve bu bir eksiklik değildir: hurda `stoklar`
    /// tablosunda satır AÇMAZ (HurdaAlis da açmıyor). Hurdanın tek kaynağı
    /// defterdir; bakiye <see cref="Hurda.HurdaStokKapisi"/> ile TÜRETİLİR.
    /// </para>
    /// </summary>
    public async Task<Kuyumcu.Contracts.Sayfalar.Hurda.HurdaSatisSonucu> HurdaSatis(
        HurdaSatisDto dto, AktifKullanici islem)
    {
        // ZO-ISLEM-A: /hurda ekranının bakiyesiyle AYNI şube çözümü (tek kural).
        islem = await Hurda.HurdaStokKapisi.OturumSubesiCoz(db, islem);
        var subeId = SubeBelirle(islem, "Hurda satışı için şube seçimi gerekli");

        // ⚠ DataAnnotations YALNIZ controller yolunda çalışır; servis doğrudan
        //   çağrıldığında (testler, iç çağrılar) hiçbir şeyi doğrulamaz.
        var maden = (dto.Maden ?? string.Empty).Trim().ToUpperInvariant();
        if (maden.Length == 0 || Array.IndexOf(Kuyumcu.Core.Domain.Maden.Hepsi, maden) < 0)
            throw new BadRequestException(
                $"Geçersiz maden: '{dto.Maden}'. Hurda çıkışı maden bazında yazılır; "
                + $"kabul edilenler: {string.Join(", ", Kuyumcu.Core.Domain.Maden.Hepsi)}.");

        if (dto.HasGrami <= 0m)
            throw new BadRequestException("Hurda satışında has gram sıfırdan büyük olmalıdır.");

        if (dto.BirimFiyatTlHasGram < 0m)
            throw new BadRequestException("Hurda satışında birim fiyat negatif olamaz.");

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);
        await IslemDogrulayici.CariDogrula(db, dto.CariId, islem.TenantId);

        // ── K-13 KAPISI: fire / ramat farkı bu sürümde YAZILMAZ ─────────────
        // Yapılandırmadan okunur, varsayılanı KAPALI. Dolu gelirse SESSİZCE
        // YUTULMAZ, gerekçesiyle reddedilir (Anayasa md.1).
        Hurda.HurdaStokKapisi.DogrulaFire(hurda.Ayarlar, dto.FireGrami);
        await IslemFiyatKapisi.Denetle(db, islem, islem.TenantId, Kuyumcu.Core.Domain.Girdi.IslemFiyatBaglami.HurdaSatis, maden, dto.BirimFiyatTlHasGram, "birimFiyatTlHasGram", dto.BantOnay, dto.BantGerekce);   // PAKET ISLEM-BANT

        // ── ÇIKIŞ KAPISI (fail-closed): elde olmayan hurda satılamaz ────────
        await Hurda.HurdaStokKapisi.DogrulaCikis(db, islem, maden, dto.HasGrami);

        // ── P-20b (K-13/2): İKİ HAS AYRI DURUR ─────────────────────────────
        // bizimKaydimiz (dto.HasGrami) = stoktan çıkan TOPLAM has → çıkış kapısı
        // aliciKabul                   = TL'nin ödendiği has → fatura/mutabakat
        // tahlilFark = kabul − bizim   (negatif ALEYHTE, pozitif LEHTE)
        var hasGrami   = Yuvarla6(dto.HasGrami);
        var aliciKabul = Yuvarla6(dto.AliciKabulHasGrami ?? dto.HasGrami);
        var tahlilFark = Hurda.HurdaStokKapisi.FarkHesapla(hasGrami, aliciKabul);

        // ⚠ TL bacağı KABUL EDİLEN hasla ödenir: kabul edilmeyen has için para
        //   istenmez. Bizim kaydımızla çarpılsaydı cariye olmayan bir alacak
        //   yazılır, tahsil edilemeyen tutar defterde sonsuza dek asılı kalırdı.
        var toplamTl = Yuvarla2(aliciKabul * dto.BirimFiyatTlHasGram);
        OdemeTutarKapisi.SifirOlamaz(toplamTl, "Hurda satışı",
            "Has gram fiyatını girin (güncel kur yoksa önce kuru güncelleyin).", "birimFiyatTlHasGram");   // P-23
        var now      = DateTime.UtcNow;

        // MASAK Faz 1 — kıymetli maden ÇIKIŞI da işlemdir; eşik brüt bedele bakar.
        var masakGerek = await masak.DogrulaKumulatif(
            db, islem.TenantId, dto.CariId, toplamTl, dto.Masak, now, islem);

        var (kasa, tahsil, kalan) = await OdemeCoz(dto.KasaId, dto.TahsilEdilenTl, toplamTl, islem.TenantId, now);

        // ⚠ Tahsil edilmeyen tutarın gideceği bir yer OLMALIDIR: cari yoksa
        //   alacak hiçbir deftere düşmez ve para sessizce buharlaşır.
        if (kalan > 0m && string.IsNullOrEmpty(dto.CariId))
            throw new BadRequestException(
                $"Tahsil edilmeyen {kalan:0.##} TL için ramatçı carisi zorunludur: "
                + "cari seçilmezse bu alacak hiçbir deftere yazılmaz. "
                + "Ya tamamını tahsil edin ya da ramatçıyı cari olarak seçin.");

        // Kalan BİZİM ALACAĞIMIZDIR → kredi limiti alacak yönünde işler.
        var vade = await Vade.VadeKapisi.Hazirla(
            db, islem.TenantId, dto.CariId, dto.VadeGun, kalan, alacakYonu: true, now, dto.KurSnapshotId);

        // 459 s. VUK Genel Tebliği — NAKİT tahsilat da hadde tabidir.
        if (tahsil > 0m && dto.OdemeTuru == "NAKIT")
            await tevsik.Dogrula(db, islem.TenantId, dto.CariId, tahsil, now,
                islemToplamTutari: toplamTl);

        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, dto.CariId, now);   // PAKET MASAK-ESZAMAN: işlem + CARİ SATIRI KİLİDİ
        // PAKET MASAK-ESZAMAN: kümülatif kapılar kilit altında, güncel toplamla YİNELENİR (eşik/kural aynı).
        masakGerek = await masak.KilitAltindaYinele(db, islem.TenantId, dto.CariId, toplamTl, dto.Masak, now, masakGerek, islem);
        if (tahsil > 0m && dto.OdemeTuru == "NAKIT")
            await tevsik.Dogrula(db, islem.TenantId, dto.CariId, tahsil, now,
                islemToplamTutari: toplamTl);

        var hareketId = Kimlik.Ulid();
        var hurdaSatisSatiri = Data.HareketFabrikasi.Ekle(db,
            new Data.OdemeIzi(
                Nakit:    dto.OdemeTuru == "NAKIT" ? tahsil : 0m,
                Veresiye: kalan,
                Vade:     kalan <= 0m ? null : vade),
            new Data.Hareket
        {
            Id            = hareketId,
            TenantId      = islem.TenantId,
            SubeId        = subeId,
            Tur           = "HURDA_SATIS",
            Tarih         = now,
            // P-08 / Y-14 — ADET BİLİNMİYOR: hurda gramla çıkar, parça sayılmaz.
            Aciklama      = dto.Aciklama,
            TlTutari      = Yuvarla2(toplamTl),     // kasaya giriş / alacak
            // P-20b: bu bacak yalnız KABUL EDİLEN hası taşır; bizim kaydımızın
            // kalanı fark bacağındadır. İkisinin toplamı −(bizim kaydımız) eder.
            HasGrami      = Yuvarla6(-aliciKabul),  // HAS BACAĞI EKSİLİR
            Maden         = maden,                  // K20-1
            OdemeTuru     = tahsil <= 0m ? "VERESIYE"
                          : tahsil >= toplamTl ? dto.OdemeTuru : "KARMA",
            KurSnapshotId = dto.KurSnapshotId,
            CariId        = dto.CariId,
            KullaniciId   = islem.Id,
            Olusturuldu   = now,
        });
        masak.Kaydet(db, masakGerek, hareketId, dto.CariId, toplamTl, dto.Masak, islem, now, MasakKaynakTuru.Hareket);

        // ── P-20b: TAHLİL FARKI BACAĞI — AYNI İŞLEM (tek kayıt çağrısı) ────
        // ⚠ Fabrikayı çağıran metotta TEK bir kayıt çağrısı olur (denetim.sh
        //   TAHLIL_FARKI_ESSIZ_BACAK): iki bacak ya birlikte yazılır ya hiç.
        //   Fark 0 ise bacak YAZILMAZ — boş bir fark satırı deftere gürültüdür.
        Data.Hareket? farkBacagi = tahlilFark == 0m ? null
            : Data.TahlilFarkiFabrikasi.Ekle(
                db, hurdaSatisSatiri, tahlilFark, dto.TahlilFarkiGerekcesi, islem.Id, now);

        if (kalan > 0m && !string.IsNullOrEmpty(dto.CariId))
            await CariBakiyeGuncelle(dto.CariId, islem.TenantId, kalan, 0m, now);

        if (kasa is not null && tahsil > 0m)
            await KasaGuncelle(kasa, islem.TenantId, tahsil, now, hareketId, dto.OdemeTuru);

        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] HURDA_SATIS: kayıt {HasGrami} / kabul {Kabul} has {Maden} = {ToplamTl} TL "
            + "(tahsil {Tahsil}, tahlil farkı {Fark})",
            islem.TenantId, hasGrami, aliciKabul, maden, toplamTl, tahsil, tahlilFark);

        // UX-S27: sunucunun döndürdüğü SONUÇ banda yazılır — yeni hurda bakiyesi
        // burada okunur ki ekran ikinci bir türetme yapmasın (UX-S22).
        var kalanBakiye = await Hurda.HurdaStokKapisi.Bakiye(db, islem, maden);

        // ⚠ Yanıt tipi ANONİM DEĞİL (P-03): istemci tipi derleyiciyle kilitlenir.
        return new Kuyumcu.Contracts.Sayfalar.Hurda.HurdaSatisSonucu(
            hareketId,
            maden,
            Kuyumcu.Core.Domain.Maden.Ad(maden),
            hasGrami,
            dto.BirimFiyatTlHasGram,
            toplamTl,
            tahsil,
            kalan,
            kalanBakiye,
            kasa?.Bakiye,
            "TRY",
            now,
            // K-13: fire kaydının neden yazılmadığı SUNUCUDAN gelir, ekranda uydurulmaz.
            hurda.Ayarlar.FireKaydiAcik,
            hurda.Ayarlar.FireKaydiAcik ? null : Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(Hurda.HurdaStokKapisi.FireKapaliMesaji),
            hasGrami,
            aliciKabul,
            tahlilFark,
            farkBacagi?.Id);
    }

    // ─── Şubeler arası transfer ───────────────────────────────────────────────
    public async Task<object> Transfer(TransferDto dto, AktifKullanici islem)
    {
        if (dto.KaynakSubeId == dto.HedefSubeId)
            throw new BadRequestException("Kaynak ve hedef şube aynı olamaz");

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);
        // K1: her iki şube de bu kiracıya ait olmalı (hedef şube doğrulanmıyordu)
        await IslemDogrulayici.SubeDogrula(db, dto.KaynakSubeId, islem.TenantId);
        await IslemDogrulayici.SubeDogrula(db, dto.HedefSubeId, islem.TenantId);

        var urun       = await UrunBul(dto.UrunId, islem.TenantId);
        Urun.UrunAdetKapisi.Dogrula(urun.Birim, dto.Adet, "adet");   // PAKET GOC-C (#6 · K14): ADET birimli üründe miktar tam sayı.
        var kaynakStok = await StokBul(dto.UrunId, dto.KaynakSubeId, islem.TenantId);

        // P-21 (Y-23): rezerve mal başka şubeye TRANSFER de edilemez — söz
        // verilen müşteri malı bu şubede bekliyor.
        BlokeKapisi.SatilabilirDogrula(kaynakStok, dto.Adet, "Transfer (kaynak şube)");

        var hasGramiPerAdet = urun.HasGrami;
        var now = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();

        // Maliyet taşıma kaydı bu harekete bağlanacağı için kimlik değişkene alınır.
        var hareketId = Kimlik.Ulid();
        Data.HareketFabrikasi.Ekle(db,
            Data.OdemeIzi.Yok,
            new Data.Hareket
        {
            Id            = hareketId,
            TenantId      = islem.TenantId,
            SubeId        = dto.KaynakSubeId,
            Tur           = "TRANSFER",
            Tarih         = now,
            Adet          = dto.Adet,   // P-08 / Y-14
            Aciklama      = dto.Aciklama,
            TlTutari      = 0m,
            HasGrami      = Yuvarla6(-dto.Adet * hasGramiPerAdet),
            // K20-1: has bacağının madeni — ürünün ayarından. Altın dışı gram
            // ALTIN toplamına karışamaz.
            Maden         = Kuyumcu.Core.Domain.Ayarlar.MadenBul(urun.AyarId) ?? Kuyumcu.Core.Domain.Maden.Altin,
            KurSnapshotId = dto.KurSnapshotId,
            UrunId        = dto.UrunId,
            KarsiSubeId   = dto.HedefSubeId,
            KullaniciId   = islem.Id,
            Olusturuldu   = now,
        });

        // Kaynak şubeden düş, hedef şubeye ekle
        // PAKET 25: kaynak şubede dto.ReyonId'den düşer, hedef şubede
        // dto.HedefReyonId'ye girer. Hedef reyon verilmediyse mal hedef şubede
        // ATANMAMIŞ olur — ürünün varsayılan reyonu BAŞKA şubeye ait olabilir,
        // sessizce oraya yazmak yanlış şubede mal gösterirdi.
        var kaynakUyari = await StokDegistir(
            dto.UrunId, dto.KaynakSubeId, islem.TenantId, -dto.Adet, hasGramiPerAdet, now, dto.ReyonId);
        var hedefUyari = await StokDegistir(
            dto.UrunId, dto.HedefSubeId, islem.TenantId, dto.Adet, hasGramiPerAdet, now, dto.HedefReyonId);

        // Maliyet de taşınır. Taşınmazsa hedef şube malı BEDAVA almış görünür
        // ve oradaki satış sahte kâr yazar — transfer kâr doğurmaz.
        await maliyet.PartiTasi(islem.TenantId, dto.KaynakSubeId, dto.HedefSubeId, dto.UrunId,
            dto.Adet, hasGramiPerAdet, now, await maliyet.Yontem(islem.TenantId), hareketId);

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] TRANSFER: {UrunId} {Adet} adet {KaynakSubeId} → {HedefSubeId}",
            islem.TenantId, dto.UrunId, dto.Adet, dto.KaynakSubeId, dto.HedefSubeId);

        return new
        {
            mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Transfer tamamlandı"), urunId = dto.UrunId, adet = dto.Adet,
            // UX-S27/UX-S28: uyarı sessizce yutulmaz, yanıtta görünür.
            yerlesimUyarisi = kaynakUyari ?? hedefUyari,
        };
    }

    // ─── Çok bacaklı satış fişi ───────────────────────────────────────────────
    /// <summary>
    /// Tek fişte birden fazla ödeme aracıyla satış.
    ///
    /// KAPI PARİTESİ — <see cref="Satis"/>'in kapılarının HEPSİ buradadır:
    /// kur · cari · ürün · taş kökeni (md.11/e) · konsinye · stok yeterliliği ·
    /// MASAK (fiş toplamı) · kasa para birimi (K6, bacak başına) · tevsik
    /// (fişin NAKİT toplamı, TEK KEZ). Biri eksik kalırsa bu uç, mevcut satış
    /// ucunun atladığı bir kapıyı da atlayan ikinci bir yol olurdu.
    /// </summary>
    public async Task<object> SatisFisi(SatisFisiDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem, "Satış için şube seçimi gerekli");

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);
        await IslemDogrulayici.CariDogrula(db, dto.CariId, islem.TenantId);
        // PAKET P-16 (B-31): SATIŞ carisi ayrıca AKTİF olmalı — ekran pasif
        // cariyi listeden gizler, sunucu gizlemeye GÜVENMEZ (fail-closed).
        await IslemDogrulayici.CariAktifDogrula(db, dto.CariId, islem.TenantId);

        // PAKET S2-5b: satışı YAPAN (yoksa oturum kullanıcısı) — fişin tüm SATIŞ kalemlerine.
        var satici = await SaticiBelirle(dto.PersonelId, islem);

        // ── Kalemler: ürün, yasal kapılar, stok ──
        var kalemler = new List<(SatisFisKalemDto Dto, Data.Urun Urun, decimal Tutar, decimal Has)>();
        IskontoHesabi.YuzdeDogrula(dto.FisIskontoYuzde, "Fiş iskontosu");
        var satirIskontoToplam = 0m;
        foreach (var k in dto.Kalemler)
        {
            var urun = await UrunBul(k.UrunId, islem.TenantId);
            Urun.UrunAdetKapisi.Dogrula(urun.Birim, k.Adet, "kalemler", urun.Ad);   // PAKET GOC-C (#6 · K14): ADET birimli üründe miktar tam sayı.
            Urun.TasKapisi.SatisaUygunMu(urun);          // md.11/(e)
            Urun.KonsinyeKapisi.SatisaUygunMu(urun);     // sahibi/bedeli eksik konsinye satılamaz

            var stok = await StokBul(k.UrunId, subeId, islem.TenantId);
            // P-21 (Y-23): çok kalemli fişte de satılabilir stok geçerlidir.
            BlokeKapisi.SatilabilirDogrula(stok, k.Adet, $"Satış fişi ({urun.Ad})");

            // HEDİYE: fiyat sunucuda 0'a zorlanır — istemcinin gönderdiği fiyat
            // ne olursa olsun. Kalem yine stoktan ve maliyetten düşer.
            if (k.Hediye) { k.BirimFiyatTl = 0m; k.IscilikTl = 0m; }

            // ── PAKET P-16 (Y-4) — SATIR İSKONTOSU SUNUCUDA HESAPLANIR ──────
            // İstemci yalnız YÜZDE gönderir; TL karşılığı burada üretilir.
            IskontoHesabi.YuzdeDogrula(k.SatirIskontoYuzde, "Satır iskontosu", "satirIskontoYuzde");
            var brut = Yuvarla2(k.Adet * k.BirimFiyatTl + (k.IscilikTl ?? 0m));
            var satirIsk = IskontoHesabi.Tutar(brut, k.SatirIskontoYuzde);
            satirIskontoToplam += satirIsk;
            var tutar = brut - satirIsk;
            kalemler.Add((k, urun, tutar, k.Adet * urun.HasGrami));
        }

        // KDV GUT I/B-10.1: promosyon malı "satılan malın YANINDA" verilir. Yanında
        // bedelli kalem yoksa bu bedelsiz teslimdir (KDVK md.27 emsal bedel) ve
        // sistem onu hediye diye yazmaz.
        if (kalemler.Any(x => x.Dto.Hediye) && !kalemler.Any(x => !x.Dto.Hediye && x.Tutar > 0m))
            throw new BadRequestException(
                "Hediye kalemi tek başına verilemez: aynı fişte bedelli en az bir kalem olmalıdır " +
                "(bedelsiz teslim KDVK md.27 emsal bedel üzerinden KDV doğurur)");

        // ── PAKET P-16 (Y-4) — FİŞ (GENEL) İSKONTOSU ────────────────────────
        // UX-S21: yüzde GÖNDERİLMEDİYSE carinin kartındaki oran uygulanır —
        // sistem bildiği oranı kasiyerden tekrar istemez. Açıkça 0 gönderilirse
        // iskonto yoktur; "0" ile "boş" AYRI anlamlardır, biri diğerine çevrilmez.
        var fisIskontoYuzde = dto.FisIskontoYuzde;
        var fisIskontoCaridenMi = false;
        if (fisIskontoYuzde is null && !string.IsNullOrEmpty(dto.CariId))
        {
            var cariOran = await db.Cariler.AsNoTracking()
                .Where(c => c.Id == dto.CariId && c.TenantId == islem.TenantId)
                .Select(c => c.IskontoYuzde).FirstOrDefaultAsync();
            if (cariOran is > 0m) { fisIskontoYuzde = cariOran; fisIskontoCaridenMi = true; }
        }

        var satirToplam = Yuvarla2(kalemler.Sum(x => x.Tutar));
        var fisIskontoTutari = IskontoHesabi.Tutar(satirToplam, fisIskontoYuzde);
        if (fisIskontoTutari > 0m)
        {
            // Satırlara ORANSAL dağıtılır: Σ satır = fiş toplamı TAM eşit kalsın
            // (ödeme bacağı, KDV matrahı ve stok maliyeti satır tutarını okur).
            var iskontoPaylari = IskontoHesabi.Dagit([.. kalemler.Select(x => x.Tutar)], fisIskontoTutari);
            for (var i = 0; i < kalemler.Count; i++)
            {
                var (kd, ku, kt, kh) = kalemler[i];
                kalemler[i] = (kd, ku, kt - iskontoPaylari[i], kh);
            }
            // Dağıtım sonrası gerçekte düşen tutar (kelepçelenmiş olabilir).
            fisIskontoTutari = satirToplam - Yuvarla2(kalemler.Sum(x => x.Tutar));
        }

        var fisToplam = Yuvarla2(kalemler.Sum(x => x.Tutar));
        // P-23: iskonto sonrası 0 TL'lik fiş ödeme bacağı taşıyamaz (bacak > 0 zorunlu).
        OdemeTutarKapisi.SifirOlamaz(fisToplam, "Satış fişi",
            "Kalem fiyatlarını ya da iskontoyu düzeltin; tamamen bedelsiz teslim satış fişiyle yazılmaz.",
            "kalemler");
        var now = DateTime.UtcNow;
        var bugun =DateOnly.FromDateTime(Zaman.UtcdenYerele(now));
        var hasKuru = await db.KurSnapshots.AsNoTracking()
            .Where(s => s.Id == dto.KurSnapshotId && s.TenantId == islem.TenantId)
            .Select(s => s.AltinHasFiyati).SingleAsync();

        // Her kalem için SUNUCU liste fiyatı ve kampanya — FEY md.11 için satıra yazılır.
        // Kasiyer fiyatı elle değiştirmiş olabilir: indirim = liste − uygulanan (≥ 0).
        var teklifler = new List<Fiyat.FiyatTeklifi?>();
        foreach (var x in kalemler)
            teklifler.Add(await fiyat.Teklif(x.Urun, dto.CariId, hasKuru, bugun));

        // ── Ödeme bacakları: tek doğrulama merkezi ──
        // Döviz kurları İŞLEM ANINDAKİ kur kaydından okunur — kasiyerin ekranındaki
        // sayıdan değil. KurDogrula yalnız kaydın varlığını denetler; kurun kendisi
        // burada çekilir ki döviz bacağının TL'si sunucuda hesaplanabilsin.
        var kurKaydi = await db.KurSnapshots.AsNoTracking()
            .Where(s => s.Id == dto.KurSnapshotId && s.TenantId == islem.TenantId)
            .Select(s => new OdemeSepeti_Kur(s.UsdTry, s.EurTry, s.GbpTry, s.IptalEdildi))
            .SingleAsync();
        var sepet = OdemeSepeti.Coz(dto.Odemeler, fisToplam, dto.CariId,
            new DovizKurlari(kurKaydi.Usd, kurKaydi.Eur, kurKaydi.Gbp),
            // PAKET B-3 (D2 · K3): "Peşin Müşteri" bayrağı SUNUCUDA ölçülür.
            sistemCarisi: await Kuyumcu.Api.Modules.Cari.SistemCarisi.MiAsync(db, islem.TenantId, dto.CariId),
            // PAKET B-4: ALTIN bacağının stopaj oranı + satıcı kapısı (yapılandırmadan).
            hurda: hurda);

        // PAKET B-4 (K-KUR-2 · B4-23): altın bacağı İPTAL EDİLMİŞ kur kaydına
        // demirlenemez — satışın ve alışın hası aynı, GEÇERLİ ana bağlanır.
        if (sepet.Altin is not null && kurKaydi.IptalEdildi)
            throw new BadRequestException(
                "Bu kur kaydı iptal edilmiş; eski altınla ödeme iptal edilmiş kurla yazılamaz. Güncel kuru seçin.");
        if (sepet.Altin is { } eskiAltin) await IslemFiyatKapisi.Denetle(db, islem, islem.TenantId, Kuyumcu.Core.Domain.Girdi.IslemFiyatBaglami.EskiAltinOdeme, eskiAltin.Ayar.Maden, eskiAltin.Bacak.BirimFiyatTlHasGram, "birimFiyatTlHasGram", dto.BantOnay, dto.BantGerekce);   // PAKET ISLEM-BANT

        // MASAK: FİŞİN TOPLAMINA bakar. Nakit payına çevrilmesi mevzuata aykırı
        // bir gevşetme olurdu — 100.000 nakit + 2.400.000 havale ile kapanan fiş
        // eşiğin altına düşerdi.
        var masakGerek = await masak.DogrulaKumulatif(
            db, islem.TenantId, dto.CariId, fisToplam, dto.Masak, now, islem);

        // K6: her bacağın kasası ayrı doğrulanır. TL bacağı TRY kasasına, DOVIZ
        // bacağı KENDİ para biriminin kasasına — dolar avro kasasına giremez.
        // Aynı kasa iki farklı beklentiyle gelirse (biri TRY biri USD) ikinci
        // doğrulama patlar; bu doğru davranıştır.
        var kasalar = new Dictionary<string, Data.Kasa>();
        // PAKET B-4: ALTIN bacağı da kasadan GEÇMEZ (B4-5/B4-19) — tek ölçü
        // OdemeAraci.KasayaGirer'dir (VERESIYE + ALTIN hariç).
        foreach (var b in sepet.Bacaklar.Where(b => Data.OdemeAraci.KasayaGirer(b.Arac)))
        {
            var beklenen = b.Arac == "DOVIZ" ? b.ParaBirimi! : "TRY";
            if (kasalar.TryGetValue(b.KasaId!, out var mevcut) && mevcut.ParaBirimi != beklenen)
                throw new BadRequestException(
                    $"'{mevcut.Ad}' kasası bu fişte hem {mevcut.ParaBirimi} hem {beklenen} bacağına bağlanamaz");
            if (!kasalar.ContainsKey(b.KasaId!))
                kasalar[b.KasaId!] =
                    await IslemDogrulayici.KasaDogrula(db, b.KasaId!, islem.TenantId, beklenen, now);
        }

        // 459 s. VUK — fişin NAKİT toplamı için TEK KEZ. Bacak başına çağrılsaydı
        // her bacak ayrı ayrı haddin altında kalıp fiş hadde takılmazdı.
        // §4.1.2 KAPI 2 de burada işler: fiş toplamı haddi aşıyorsa nakit bacak
        // KABUL EDİLMEZ. Karma ödemenin varlık sebebi tam olarak budur —
        // "yarısını nakit verelim" talebi yasal olarak mümkün değildir.
        if (sepet.NakitToplam > 0m)
            await tevsik.Dogrula(db, islem.TenantId, dto.CariId, sepet.NakitToplam, now,
                islemToplamTutari: fisToplam);

        // Fişin veresiye bacağı tek bir borçtur; kalemlere dağıtılan paylar
        // aynı vadeyi taşır. Kalem başına vade, tek fişi yapay olarak birden
        // çok borca bölerdi.
        var vade = await Vade.VadeKapisi.Hazirla(
            db, islem.TenantId, dto.CariId, dto.VadeGun,
            sepet.VeresiyeToplam, alacakYonu: true, now, dto.KurSnapshotId);

        // PAKET B-4 (B4-20): gider pusulası numarası transaction ÖNCESİ alınır
        // (HurdaAlis ile aynı desen — MAX+1 okuması kilitlenmesin).
        // PAKET YASAL-3 (§ 4.4 nokta 3): eski altını getiren satıcı da taranır (hurda alışla aynı kural).
        if (sepet.Altin is { } yaptirimAltin)
            await Masak.YaptirimServisi.KimlikTara(db, islem.TenantId, islem, Data.YaptirimNoktasi.HurdaSatici,
                yaptirimAltin.Bacak.Satici?.Tckn, yaptirimAltin.Bacak.Satici?.AdSoyad, string.IsNullOrEmpty(dto.CariId) ? null : dto.CariId);
        var altinPusulaNo = await AltinPusulaNoAl(sepet.Altin, islem, now);

        // ── Yazma ──
        var fisId = Kimlik.Ulid();
        var paylar = OdemeSepeti.PaylariDagit(
            kalemler.Select(x => x.Tutar).ToList(), sepet.NakitToplam, sepet.VeresiyeToplam);

        // ── PAKET P-21 (Y-18) — ORTAM TRANSACTION'INA KATILMA ───────────────
        // ⚠ NEDEN: sipariş → satış dönüşümü "TEK TRANSACTION" olmak ZORUNDA
        // (plan DoD: "yarım dönüşüm imkânsız"). Dönüşüm bu metodu çağırır; bu
        // metot koşulsuz kendi transaction'ını açsaydı EF "zaten açık
        // transaction var" diye patlar, dönüşüm de mecburen satış mantığını
        // KOPYALARDI — kopyalanan kapılar (MASAK · tevsik · iskonto · stok)
        // bir gün ayrışırdı. Ambient transaction varsa ona KATILIR ve commit'i
        // AÇANA bırakır; yoksa davranış AYNEN eskisi gibidir.
        // ⚠ `await using`: istisna hâlinde geri alma (rollback) davranışı
        //   AYNEN korunur — null üzerinde `await using` güvenlidir.
        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, dto.CariId, now);   // PAKET MASAK-ESZAMAN: işlem + CARİ SATIRI KİLİDİ (dış işlem varsa katılır)
        // PAKET MASAK-ESZAMAN: kümülatif kapılar kilit altında, güncel toplamla YİNELENİR (eşik/kural aynı).
        masakGerek = await masak.KilitAltindaYinele(db, islem.TenantId, dto.CariId, fisToplam, dto.Masak, now, masakGerek, islem);
        if (sepet.NakitToplam > 0m)
            await tevsik.Dogrula(db, islem.TenantId, dto.CariId, sepet.NakitToplam, now,
                islemToplamTutari: fisToplam);

        string? ilkHareketId = null;
        string? yerlesimUyarisi = null;   // PAKET L-1: reyon düşümünün uyarısı (ilk uyarı) banda
        for (var i = 0; i < kalemler.Count; i++)
        {
            var (k, urun, tutar, has) = kalemler[i];
            var (nakit, veresiye) = paylar[i];
            var hareketId = Kimlik.Ulid();
            ilkHareketId ??= hareketId;

            // Liste fiyatı / indirim / kampanya — teklif yoksa (liste tanımlanmamış,
            // gümüş, kur yok) alanlar BOŞ kalır; uydurulmaz. Hediyede indirim = liste × adet.
            var teklif = teklifler[i];
            var uygulananBirim = k.Adet > 0m ? Yuvarla2(tutar / k.Adet) : 0m;
            decimal? listeFiyati = teklif?.ListeFiyati;
            decimal? indirim = teklif is null ? null
                : Yuvarla2(Math.Max(0m, (teklif.ListeFiyati - uygulananBirim) * k.Adet));
            // Hediye satırı kampanya damgası TAŞIMAZ: indirimi kampanyanın 250 TL'si değil, malın
            // tamamıdır; sebep alanı `hediye` bayrağıdır. Damgalansaydı kampanya raporu hediyeleri
            // kampanya indirimi diye toplardı.
            var kampanyaId = !k.Hediye && teklif is { IndirimTutari: > 0m } && indirim > 0m ? teklif.KampanyaId : null;

            Data.HareketFabrikasi.Ekle(db,
                // PAKET P-23c: ÇOK BACAKLI fişin kalemlerini sepet yazar (aşağıda);
                // fabrika tek bacak türetemez. Beyan AÇIKTIR — beyan etmeyen her
                // yol fabrika yolundan geçer ve kalemi kendiliğinden doğar.
                new Data.OdemeIzi(nakit, veresiye, veresiye > 0m ? vade : null,
                                  Data.KalemYazari.Cagiran),
                new Data.Hareket
                {
                    Id            = hareketId,
                    TenantId      = islem.TenantId,
                    SubeId        = subeId,
                    Tur           = "SATIS",
                    Tarih         = now,
                    Adet          = k.Adet,   // P-08 / Y-14
                    Aciklama      = dto.Aciklama,
                    TlTutari      = tutar,
                    HasGrami      = Yuvarla6(-has),        // stoktan çıkış
                    // K20-1: fiş kaleminin madeni o kalemin ürününden.
                    Maden         = Kuyumcu.Core.Domain.Ayarlar.MadenBul(urun.AyarId) ?? Kuyumcu.Core.Domain.Maden.Altin,
                    // PARA-DEMO-2 (madde 1, VERİ/YÜKSEK): "KARMA" YALNIZ ≥ 2 bacakta yazılır. Tek bacaklı fiş
                    // (BASİT satışta tek Kart/Havale satırı) o bacağın aracını taşır — eskiden ekstre
                    // "Karma ödeme", gün sonu "Karışık" yazıyordu. Kırılım yine paylardadır (odeme_kalemleri).
                    // Geri dönük veri düzeltmesi YOK: eski tek bacaklı KARMA satırlar olduğu gibi kalır.
                    OdemeTuru     = OdemeSepeti.FisEtiketi(sepet.Bacaklar),
                    FisId         = fisId,
                    ListeFiyati   = listeFiyati,
                    IndirimTutari = indirim,
                    KampanyaId    = kampanyaId,
                    Hediye        = k.Hediye,
                    KurSnapshotId = dto.KurSnapshotId,
                    CariId        = dto.CariId,
                    UrunId        = k.UrunId,
                    KullaniciId   = islem.Id,
                    PersonelId    = satici,   // S2-5b: SATAN
                    Olusturuldu   = now,
                });

            // PAKET L-1: stok HER kalemde düşer; ??= yalnız ATAMAYI korur (çağrıyı DEĞİL).
            var kalemUyari = await StokDegistir(k.UrunId, subeId, islem.TenantId, -k.Adet, urun.HasGrami, now, dto.ReyonId);
            yerlesimUyarisi ??= kalemUyari;
            await maliyet.PartiTuket(islem.TenantId, subeId, k.UrunId, k.Adet, now,
                await maliyet.Yontem(islem.TenantId), hareketId);

            // Konsinye mal satıldıysa bedeli SAHİBİNE borçlanılır.
            if (urun.Konsinye && urun.KonsinyeBirimBedel is > 0m
                && !string.IsNullOrEmpty(urun.KonsinyeCariId))
            {
                var konsinyeBorc = Yuvarla2(k.Adet * urun.KonsinyeBirimBedel.Value);
                await CariBakiyeGuncelle(urun.KonsinyeCariId, islem.TenantId, -konsinyeBorc, 0m, now);
            }
        }

        // MASAK kaydı fişin İLK hareketine bağlanır (fiş tek işlemdir).
        masak.Kaydet(db, masakGerek, ilkHareketId!, dto.CariId, fisToplam, dto.Masak, islem, now, MasakKaynakTuru.Hareket);

        // ── PAKET B-4 — ALTIN BACAĞI: AYRI HURDA_ALIS + GİDER PUSULASI ───────
        // AYNI transaction, AYNI kur kaydı (K-KUR-1). SATIS satırları yukarıda
        // BRÜT yazıldı (B4-16); altın mahsubu onları düşürmez.
        var altinYazim = sepet.Altin is null ? null
            : AltinBacaginiYaz(sepet.Altin, altinPusulaNo, dto.KurSnapshotId, dto.CariId,
                               fisId, subeId, islem, now);

        // Veresiye bacağı → cari borçlanır.
        if (sepet.VeresiyeToplam > 0m)
            await CariBakiyeGuncelle(dto.CariId!, islem.TenantId, sepet.VeresiyeToplam, 0m, now);

        // Her nakit/kart/havale bacağı KENDİ kasa hareketini yazar; özet satır YOK.
        // Böylece gün sonu raporunun ödeme aracına göre kırılımı (RaporModulu,
        // kasa hareketlerini OdemeTuru'na göre gruplar) hiç dokunmadan doğru kalır.
        // Kasaya KENDİ para biriminde yazılır: döviz kasasına efektif miktar (USD),
        // TL kasasına TL. TL karşılığı hareket satırında (TlTutari + KurSnapshotId)
        // durur; bakiye de kur da append-only defterden yeniden türetilebilir.
        // ── PAKET P-23 (R-17) — ÖDEME KALEMİ YAZIMI ────────────────────────
        // Döngü ARTIK TÜM bacakları geziyor (VERESIYE dâhil) çünkü ödeme kalemi
        // VERESIYE bacağını da KAYDEDER: fişin kalem toplamı fiş toplamına ancak
        // o zaman eşitlenir (denetim.sh: FIS_TOPLAM_ODEME_KALEMI_FARKI = 0).
        // Kasa/POS işleri ESKİSİ GİBİ yalnız kasaya giren bacaklar için koşar —
        // VERESIYE'nin kasa satırı YOKTUR ve bu davranış korunur.
        // ⚠ Kalem yazmak DEFTERE YAZMAK DEĞİLDİR: HareketFabrikasi tek yazma
        //   yolu olmaya devam eder, kalem yazılmış fişin yanındaki açıklayıcı
        //   kayıttır (KuyumcuDbContext.OdemeKalemleri gerekçesi).
        var yazilanKalemler = new List<Data.OdemeKalemi>(sepet.Bacaklar.Count);
        for (var bi = 0; bi < sepet.Bacaklar.Count; bi++)
        {
            var b = sepet.Bacaklar[bi];
            string? kasaHareketId = null;
            string? posTahsilatId = null;

            if (Data.OdemeAraci.KasayaGirer(b.Arac))
            {
                var bacakKasa = kasalar[b.KasaId!];
                kasaHareketId = await KasaGuncelle(bacakKasa, islem.TenantId,
                    b.Arac == "DOVIZ" ? b.DovizTutar!.Value : b.TutarTl,
                    now, ilkHareketId!, b.Arac);
                // PAKET 31 — YALNIZ kart bacağı POS defterine yazılır; nakit/havale/döviz yazılmaz.
                posTahsilatId = await PosDefterineYaz(bacakKasa, islem, b.Arac, b.PosCihazId,
                    b.PosTaksitAdedi, b.TutarTl, ilkHareketId!, kasaHareketId,
                    dto.KurSnapshotId, dto.CariId, now);
            }

            yazilanKalemler.Add(new Data.OdemeKalemi
            {
                Id            = Kimlik.Ulid(),
                TenantId      = islem.TenantId,
                FisId         = fisId,
                // PAKET B-4: ALTIN kalemi KENDİ hareketine (HURDA_ALIS) bağlanır —
                // kırılım "bu bacak hangi alışla kapandı" sorusunu kayıttan cevaplar.
                HareketId     = b.Arac == Data.OdemeAraci.Altin && altinYazim is not null
                                    ? altinYazim.HareketId : ilkHareketId!,
                Sira          = bi,
                Arac          = b.Arac,
                TutarTl       = b.TutarTl,
                // UX-S32: tutar para birimiyle birlikte taşınır. TL bacağı TRY;
                // döviz bacağı KENDİ para birimi (efektif miktar DovizTutar'da).
                ParaBirimi    = b.Arac == "DOVIZ" ? b.ParaBirimi! : "TRY",
                DovizTutar    = b.Arac == "DOVIZ" ? b.DovizTutar : null,
                KasaId        = b.KasaId,
                KasaHareketId = kasaHareketId,
                PosTahsilatId = posTahsilatId,
                TaksitAdedi   = b.Arac == "KART" ? (b.PosTaksitAdedi is > 1 ? b.PosTaksitAdedi : 1) : null,
                KurSnapshotId = dto.KurSnapshotId,
                Bilinmiyor    = false,   // canlı sepetten doğdu; tahmin YOK
                Aktif         = true,
                Olusturuldu   = now,
            });
        }

        // ── P-23 FAZ 2 · FIS_TOPLAM_ODEME_KALEMI_FARKI ÇALIŞMA ZAMANI KAPISI ──
        // Hasım doğrulayıcı bulgusu (2026-09-22 · ORTA): aynı adı taşıyan
        // denetim.sh sayacı hiçbir TOPLAM karşılaştırmıyordu; yalnız `Add`
        // satırının 40 satır öncesinde `sepet.Bacaklar` geçip geçmediğine
        // bakan bir metin taramasıydı. Bacak tutarı çalışma zamanında
        // yuvarlanıp saptığında sayaç yine 0 dönerdi; rapor okuyanı
        // "toplamlar denkleşiyor" sanısına düşürüyordu.
        // ARTIK FARK GERÇEKTEN ÖLÇÜLÜYOR: kalem toplamı fiş toplamına eşit
        // değilse fiş YAZILMAZ. Sayaç adının söylediği şey bu satırla DOĞRU
        // olur; denetim.sh yalnız bu kapının VARLIĞINI sayar.
        var kalemToplam = Yuvarla2(yazilanKalemler.Sum(k => k.TutarTl));
        if (kalemToplam != Yuvarla2(fisToplam))
            throw new ConflictException(
                $"Ödeme kalemlerinin toplamı fiş toplamına eşit değil: " +
                $"kalem {kalemToplam:0.00} TL, fiş {Yuvarla2(fisToplam):0.00} TL. " +
                "Fiş yazılmadı.");
        // PAKET P-23c: yazma TEK KAPIDAN geçer (Data/OdemeKalemFabrikasi.cs).
        Data.OdemeKalemFabrikasi.Yaz(db, yazilanKalemler);

        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] SATIS_FISI: fis={FisId} kalem={Kalem} toplam={Toplam} TL nakit={Nakit} veresiye={Veresiye}",
            islem.TenantId, fisId, kalemler.Count, fisToplam,
            sepet.NakitToplam, sepet.VeresiyeToplam);

        return new
        {
            fisId,
            toplamTl       = fisToplam,
            kalemSayisi    = kalemler.Count,
            nakitToplam    = sepet.NakitToplam,
            veresiyeToplam = sepet.VeresiyeToplam,
            dovizToplamTl = sepet.DovizToplamTl,
            // ── PAKET P-16 (Y-4 / UX-S27) — uygulanan iskonto BANDA YAZILIR ──
            // Kasiyer ne girdiğini değil, SUNUCUNUN NE UYGULADIĞINI görür.
            satirIskontoToplam,
            fisIskontoTutari,
            fisIskontoYuzde,
            // Oran caridenmi geldi — "ben girmedim ki" sorusunun cevabı ekranda.
            fisIskontoCaridenMi,
            iskontoToplam  = satirIskontoToplam + fisIskontoTutari,
            odemeler       = sepet.Bacaklar.Select(b => new { b.Arac, b.TutarTl, b.KasaId }),
            // PAKET B-4 (UX-S27): altın bacağının SUNUCU sonucu banda yazılır —
            // has, brüt, stopaj, net ve doğan İKİNCİ BELGE (gider pusulası).
            altinBacagi    = altinYazim?.Yanit(),
            yerlesimUyarisi,   // PAKET L-1 (UX-S27): reyon düşümü sessizce yutulmaz
        };
    }

    // ─── İade (gelen / çıkan) ─────────────────────────────────────────────────
    /// <summary>
    /// SATIŞ İADESİ — müşteri aldığı malı geri getirir.
    ///   hareketler.tlTutari = -iadeTutar   (kasadan çıkış / cari borcu azalır)
    ///   hareketler.hasGrami = +has         (mal stoğa geri girer)
    ///   stoklar            += adet
    ///   maliyet partisi     geri konur (ÇIKTIĞI maliyetle)
    ///
    /// ⚠ İade, satışın AYNASIDIR; bağımsız bir işlem değildir. Orijinal satış
    /// bulunmadan iade alınamaz — aksi hâlde hiç satılmamış mal "iade" diye
    /// stoğa sokulabilir ve kasadan para çıkarılabilirdi.
    /// </summary>
    public async Task<object> SatisIade(IadeDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem, "İade için şube seçimi gerekli");
        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);

        var (orijinal, iadeEdilebilir) = await IadeKaynagi(dto.KaynakHareketId, "SATIS", islem.TenantId);

        if (dto.Adet > iadeEdilebilir)
            throw new BadRequestException(
                $"İade adedi kalan iade hakkını aşıyor. Satılan {OrijinalAdet(orijinal):N3}, "
                + $"daha önce iade edilen {OrijinalAdet(orijinal) - iadeEdilebilir:N3}, "
                + $"kalan {iadeEdilebilir:N3} adet.");

        var urun = await UrunBul(orijinal.UrunId!, islem.TenantId);
        Urun.UrunAdetKapisi.Dogrula(urun.Birim, dto.Adet, "adet");   // PAKET GOC-C (#6 · K14): ADET birimli üründe miktar tam sayı.
        var now = DateTime.UtcNow;

        // İade tutarı ORANTILIDIR: yarısı iade edilen bir satışın yarısı geri
        // ödenir. Elle tutar verilirse orijinali AŞAMAZ — aşabilseydi iade,
        // kasadan para çıkarmanın denetimsiz bir yolu olurdu.
        var oranliTutar = Yuvarla2(Math.Abs(orijinal.TlTutari) * dto.Adet / OrijinalAdet(orijinal));
        var iadeTutar = dto.IadeTutariTl is { } elle ? Yuvarla2(elle) : oranliTutar;

        if (iadeTutar > oranliTutar)
            throw new BadRequestException(
                $"İade tutarı orantılı tutarı aşamaz (en fazla {oranliTutar:N2} TL).");

        var iadeHas = Yuvarla6(dto.Adet * urun.HasGrami);

        var masakGerek = await masak.DogrulaKumulatif(
            db, islem.TenantId, orijinal.CariId, iadeTutar, dto.Masak, now, islem);

        Data.Kasa? kasa = null;
        if (dto.OdemeTuru != "VERESIYE")
        {
            if (string.IsNullOrEmpty(dto.KasaId))
                throw new BadRequestException("Nakit/Kart/Havale iadesi için kasa ID gerekli");
            kasa = await IslemDogrulayici.KasaDogrula(db, dto.KasaId!, islem.TenantId, "TRY", now);
        }
        else if (string.IsNullOrEmpty(orijinal.CariId))
        {
            throw new BadRequestException(
                "Veresiye iadesi için orijinal satışın müşterisi olmalıdır");
        }

        // 459 s. VUK — iadede para KASADAN ÇIKAR; tediye de hadde tabidir.
        if (dto.OdemeTuru == "NAKIT")
            await tevsik.Dogrula(db, islem.TenantId, orijinal.CariId, iadeTutar, now,
                islemToplamTutari: iadeTutar);

        await using var tx = await db.Database.BaslatVeyaDevral();   // PAKET MASAK-ESZAMAN: READ COMMITTED
        // PAKET KAPORA-ESZAMAN (sınıf taraması): kalan iade hakkı yukarıda işlem DIŞINDA hesaplanıyordu →
        // iki eşzamanlı iade ikisi de geçip hakkın İKİ katını iade edebiliyordu (kasa iki kat). Kaynak
        // satır kilitlenir, hak kilitten SONRA yeniden hesaplanır.
        await db.Database.Kilitle(KilitTablosu.Hareketler, orijinal.Id, islem.TenantId);
        var kilitliKalan = (await IadeKaynagi(dto.KaynakHareketId, "SATIS", islem.TenantId)).Kalan;
        if (dto.Adet > kilitliKalan)
            throw new BadRequestException(
                $"İade adedi kalan iade hakkını aşıyor. Kalan {kilitliKalan:N3} adet.");
        // PAKET MASAK-ESZAMAN: kilit sırası kaynak hareket → CARİ; kümülatif kapılar kilit altında yinelenir.
        await YasalKilit.CariKilitle(db.Database, islem.TenantId, orijinal.CariId, now);
        masakGerek = await masak.KilitAltindaYinele(db, islem.TenantId, orijinal.CariId, iadeTutar, dto.Masak, now, masakGerek, islem);
        if (dto.OdemeTuru == "NAKIT")
            await tevsik.Dogrula(db, islem.TenantId, orijinal.CariId, iadeTutar, now,
                islemToplamTutari: iadeTutar);

        var hareketId = Kimlik.Ulid();
        Data.HareketFabrikasi.Ekle(db,
            new Data.OdemeIzi(
                Nakit:    dto.OdemeTuru == "NAKIT" ? iadeTutar : 0m,
                Veresiye: dto.OdemeTuru == "VERESIYE" ? iadeTutar : 0m,
                // İade borcu KAPATIR; kapanan borcun vadesi olmaz.
                Vade:     null),
            new Data.Hareket
            {
                Id                  = hareketId,
                TenantId            = islem.TenantId,
                SubeId              = subeId,
                Tur                 = "SATIS_IADE",
                Tarih               = now,
                Adet                = dto.Adet,   // P-08 / Y-14
                Aciklama            = dto.Aciklama,
                TlTutari            = Yuvarla2(-iadeTutar),   // para geri gider
                HasGrami            = iadeHas,                // mal geri gelir
                // K20-1: iade edilen mal ÇIKTIĞI madenle geri girer.
                Maden               = Kuyumcu.Core.Domain.Ayarlar.MadenBul(urun.AyarId) ?? Kuyumcu.Core.Domain.Maden.Altin,
                OdemeTuru           = OdemeTutarKapisi.Etiket(iadeTutar, dto.OdemeTuru),   // P-23: bedelsiz (hediye) iade etiket taşımaz
                KurSnapshotId       = dto.KurSnapshotId,
                CariId              = orijinal.CariId,
                UrunId              = orijinal.UrunId,
                IadeKaynakHareketId = orijinal.Id,
                KullaniciId         = islem.Id,
                Olusturuldu         = now,
            });
        masak.Kaydet(db, masakGerek, hareketId, orijinal.CariId, iadeTutar, dto.Masak, islem, now, MasakKaynakTuru.Hareket);

        await StokDegistir(orijinal.UrunId!, subeId, islem.TenantId, dto.Adet, urun.HasGrami, now);
        var maliyetGeri = await maliyet.PartiIade(
            islem.TenantId, orijinal.Id, dto.Adet, now, hareketId);

        if (kasa is not null && !OdemeTutarKapisi.ParaYok(iadeTutar))
        {
            var kasaHareketId = await KasaGuncelle(kasa, islem.TenantId, -iadeTutar, now, hareketId, dto.OdemeTuru);
            // PAKET 31 — kart iadesinde POS defterine TERS KAYIT (brüt NEGATİF).
            await PosDefterineYaz(kasa, islem, dto.OdemeTuru, dto.PosCihazId, dto.PosTaksitAdedi,
                -iadeTutar, hareketId, kasaHareketId, dto.KurSnapshotId, orijinal.CariId, now);
        }
        else if (!OdemeTutarKapisi.ParaYok(iadeTutar))
            // Veresiye satışın iadesi: müşterinin borcu azalır.
            await CariBakiyeGuncelle(orijinal.CariId!, islem.TenantId, -iadeTutar, 0m, now);

        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] SATIS_IADE: kaynak={Kaynak} adet={Adet} tutar={Tutar} TL",
            islem.TenantId, orijinal.Id, dto.Adet, iadeTutar);

        return new
        {
            hareketId, kaynakHareketId = orijinal.Id, adet = dto.Adet,
            iadeTutariTl = iadeTutar, iadeHasGrami = iadeHas,
            geriGelenMaliyetTl = maliyetGeri.ToplamMaliyetTl,
            kalanIadeHakki = Yuvarla3(iadeEdilebilir - dto.Adet),
        };
    }

    /// <summary>
    /// ALIŞ İADESİ — aldığımız malı tedarikçiye geri veririz.
    /// Satış iadesinin aynası: mal stoktan ÇIKAR, para kasaya GİRER
    /// (veya tedarikçiye olan borcumuz azalır).
    /// </summary>
    public async Task<object> AlisIade(IadeDto dto, AktifKullanici islem)
    {
        var subeId = SubeBelirle(islem, "İade için şube seçimi gerekli");
        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);

        var (orijinal, iadeEdilebilir) = await IadeKaynagi(dto.KaynakHareketId, "ALIS", islem.TenantId);

        if (dto.Adet > iadeEdilebilir)
            throw new BadRequestException(
                $"İade adedi kalan iade hakkını aşıyor. Kalan {iadeEdilebilir:N3} adet.");

        var urun = await UrunBul(orijinal.UrunId!, islem.TenantId);
        Urun.UrunAdetKapisi.Dogrula(urun.Birim, dto.Adet, "adet");   // PAKET GOC-C (#6 · K14): ADET birimli üründe miktar tam sayı.
        var now = DateTime.UtcNow;

        var oranliTutar = Yuvarla2(Math.Abs(orijinal.TlTutari) * dto.Adet / OrijinalAdet(orijinal));
        var iadeTutar = dto.IadeTutariTl is { } elle ? Yuvarla2(elle) : oranliTutar;
        if (iadeTutar > oranliTutar)
            throw new BadRequestException(
                $"İade tutarı orantılı tutarı aşamaz (en fazla {oranliTutar:N2} TL).");

        var iadeHas = Yuvarla6(dto.Adet * urun.HasGrami);

        // Stok yeterliliği: elimizde olmayan malı iade edemeyiz.
        var stok = await StokBul(orijinal.UrunId!, subeId, islem.TenantId);
        // P-21 (Y-23): rezerve mal tedarikçiye geri verilemez.
        BlokeKapisi.SatilabilirDogrula(stok, dto.Adet, "Alış iadesi");

        var masakGerek = await masak.DogrulaKumulatif(
            db, islem.TenantId, orijinal.CariId, iadeTutar, dto.Masak, now, islem);

        Data.Kasa? kasa = null;
        if (dto.OdemeTuru != "VERESIYE")
        {
            if (string.IsNullOrEmpty(dto.KasaId))
                throw new BadRequestException("Nakit/Kart/Havale iadesi için kasa ID gerekli");
            kasa = await IslemDogrulayici.KasaDogrula(db, dto.KasaId!, islem.TenantId, "TRY", now);
        }
        else if (string.IsNullOrEmpty(orijinal.CariId))
        {
            throw new BadRequestException(
                "Veresiye iadesi için orijinal alışın tedarikçisi olmalıdır");
        }

        // Tedarikçiden NAKİT tahsil ediyoruz; tahsilat da hadde tabidir.
        if (dto.OdemeTuru == "NAKIT")
            await tevsik.Dogrula(db, islem.TenantId, orijinal.CariId, iadeTutar, now,
                islemToplamTutari: iadeTutar);

        await using var tx = await db.Database.BaslatVeyaDevral();   // PAKET MASAK-ESZAMAN: READ COMMITTED
        // PAKET KAPORA-ESZAMAN (sınıf taraması): kalan iade hakkı yukarıda işlem DIŞINDA hesaplanıyordu →
        // iki eşzamanlı iade ikisi de geçip hakkın İKİ katını iade edebiliyordu (kasa iki kat). Kaynak
        // satır kilitlenir, hak kilitten SONRA yeniden hesaplanır.
        await db.Database.Kilitle(KilitTablosu.Hareketler, orijinal.Id, islem.TenantId);
        var kilitliKalan = (await IadeKaynagi(dto.KaynakHareketId, "ALIS", islem.TenantId)).Kalan;
        if (dto.Adet > kilitliKalan)
            throw new BadRequestException(
                $"İade adedi kalan iade hakkını aşıyor. Kalan {kilitliKalan:N3} adet.");
        // PAKET MASAK-ESZAMAN: kilit sırası kaynak hareket → CARİ; kümülatif kapılar kilit altında yinelenir.
        await YasalKilit.CariKilitle(db.Database, islem.TenantId, orijinal.CariId, now);
        masakGerek = await masak.KilitAltindaYinele(db, islem.TenantId, orijinal.CariId, iadeTutar, dto.Masak, now, masakGerek, islem);
        if (dto.OdemeTuru == "NAKIT")
            await tevsik.Dogrula(db, islem.TenantId, orijinal.CariId, iadeTutar, now,
                islemToplamTutari: iadeTutar);

        var hareketId = Kimlik.Ulid();
        Data.HareketFabrikasi.Ekle(db,
            new Data.OdemeIzi(
                Nakit:    dto.OdemeTuru == "NAKIT" ? iadeTutar : 0m,
                Veresiye: dto.OdemeTuru == "VERESIYE" ? iadeTutar : 0m,
                // İade borcu KAPATIR; kapanan borcun vadesi olmaz.
                Vade:     null),
            new Data.Hareket
            {
                Id                  = hareketId,
                TenantId            = islem.TenantId,
                SubeId              = subeId,
                Tur                 = "ALIS_IADE",
                Tarih               = now,
                Adet                = dto.Adet,   // P-08 / Y-14
                Aciklama            = dto.Aciklama,
                TlTutari            = Yuvarla2(iadeTutar),    // para geri gelir
                HasGrami            = Yuvarla6(-iadeHas),     // mal stoktan çıkar
                // K20-1: iade edilen mal GİRDİĞİ madenle çıkar.
                Maden               = Kuyumcu.Core.Domain.Ayarlar.MadenBul(urun.AyarId) ?? Kuyumcu.Core.Domain.Maden.Altin,
                OdemeTuru           = OdemeTutarKapisi.Etiket(iadeTutar, dto.OdemeTuru),   // P-23: bedelsiz (hediye) iade etiket taşımaz
                KurSnapshotId       = dto.KurSnapshotId,
                CariId              = orijinal.CariId,
                UrunId              = orijinal.UrunId,
                IadeKaynakHareketId = orijinal.Id,
                KullaniciId         = islem.Id,
                Olusturuldu         = now,
            });
        masak.Kaydet(db, masakGerek, hareketId, orijinal.CariId, iadeTutar, dto.Masak, islem, now, MasakKaynakTuru.Hareket);

        await StokDegistir(orijinal.UrunId!, subeId, islem.TenantId, -dto.Adet, urun.HasGrami, now);
        // Alış iadesinde mal stoktan çıkar: girişte açılan parti TÜKETİLİR.
        await maliyet.PartiTuket(islem.TenantId, subeId, orijinal.UrunId!, dto.Adet, now,
            await maliyet.Yontem(islem.TenantId), hareketId);

        if (kasa is not null && !OdemeTutarKapisi.ParaYok(iadeTutar))
        {
            var kasaHareketId = await KasaGuncelle(kasa, islem.TenantId, iadeTutar, now, hareketId, dto.OdemeTuru);
            // PAKET 31 — alış iadesinde para KASAYA GİRER; POS defterine de pozitif yazılır.
            await PosDefterineYaz(kasa, islem, dto.OdemeTuru, dto.PosCihazId, dto.PosTaksitAdedi,
                iadeTutar, hareketId, kasaHareketId, dto.KurSnapshotId, orijinal.CariId, now);
        }
        else if (!OdemeTutarKapisi.ParaYok(iadeTutar))
            // Veresiye alışın iadesi: tedarikçiye olan borcumuz azalır.
            await CariBakiyeGuncelle(orijinal.CariId!, islem.TenantId, iadeTutar, 0m, now);

        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] ALIS_IADE: kaynak={Kaynak} adet={Adet} tutar={Tutar} TL",
            islem.TenantId, orijinal.Id, dto.Adet, iadeTutar);

        return new
        {
            hareketId, kaynakHareketId = orijinal.Id, adet = dto.Adet,
            iadeTutariTl = iadeTutar, kalanIadeHakki = Yuvarla3(iadeEdilebilir - dto.Adet),
        };
    }

    /// <summary>
    /// İade edilebilecek işlemler — tamamı iade edilmiş olanlar listeye GİRMEZ.
    /// Ekran "hangi satışı iade ediyoruz" sorusunu ancak bu listeyle sorabilir.
    /// </summary>
    public async Task<object> IadeAdaylari(string tur, AktifKullanici islem, int limit)
    {
        if (tur is not ("SATIS" or "ALIS"))
            throw new BadRequestException("İade adayı yalnız SATIS veya ALIS için listelenir");

        var iadeTuru = tur + "_IADE";

        var adaylar = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tur == tur && h.UrunId != null)
            .OrderByDescending(h => h.Tarih)
            .Take(limit * 3)          // iadesi dolanlar elenecek; pay bırakılır
            .ToListAsync();

        var kaynakIdler = adaylar.Select(h => h.Id).ToList();

        var iadeler = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tur == iadeTuru
                     && h.IadeKaynakHareketId != null
                     && kaynakIdler.Contains(h.IadeKaynakHareketId))
            .GroupBy(h => h.IadeKaynakHareketId!)
            .Select(g => new { Kaynak = g.Key, Has = g.Sum(x => Math.Abs(x.HasGrami)) })
            .ToDictionaryAsync(x => x.Kaynak, x => x.Has);

        var urunIdler = adaylar.Select(h => h.UrunId!).Distinct().ToList();
        var urunler = await db.Urunler.AsNoTracking()
            .Where(u => u.TenantId == islem.TenantId && urunIdler.Contains(u.Id))
            .Select(u => new { u.Id, u.Ad, u.HasGrami })
            .ToDictionaryAsync(x => x.Id, x => x);

        var sonuc = new List<object>();
        foreach (var h in adaylar)
        {
            if (!urunler.TryGetValue(h.UrunId!, out var u) || u.HasGrami <= 0m) continue;

            var toplamAdet = Yuvarla3(Math.Abs(h.HasGrami) / u.HasGrami);
            var iadeEdilen = iadeler.TryGetValue(h.Id, out var v) ? Yuvarla3(v / u.HasGrami) : 0m;
            var kalan = Yuvarla3(toplamAdet - iadeEdilen);
            if (kalan <= 0m) continue;   // tamamı iade edilmiş

            sonuc.Add(new
            {
                id = h.Id, h.Tarih, h.TlTutari, urunId = h.UrunId, urunAdi = u.Ad,
                toplamAdet, iadeEdilenAdet = iadeEdilen, kalanAdet = kalan, h.CariId,
            });
            if (sonuc.Count >= limit) break;
        }
        return sonuc;
    }

    /// <summary>
    /// İade kaynağını doğrular ve KALAN iade hakkını hesaplar.
    /// Aynı satış iki kez tam iade edilemez.
    /// </summary>
    private async Task<(Data.Hareket Orijinal, decimal Kalan)> IadeKaynagi(
        string kaynakId, string beklenenTur, string tenantId)
    {
        var orijinal = await db.Hareketler.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == kaynakId && h.TenantId == tenantId)
            ?? throw new NotFoundException("İade edilecek işlem bulunamadı");

        if (orijinal.Tur != beklenenTur)
            throw new BadRequestException(
                $"Bu işlem {beklenenTur} değil ({orijinal.Tur}); iade edilemez");

        if (string.IsNullOrEmpty(orijinal.UrunId))
            throw new BadRequestException("Ürünü olmayan bir işlem iade edilemez");

        var iadeTuru = beklenenTur + "_IADE";
        var oncekiIade = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId
                     && h.IadeKaynakHareketId == kaynakId && h.Tur == iadeTuru)
            .SumAsync(h => (decimal?)Math.Abs(h.HasGrami)) ?? 0m;

        var urunHas = await db.Urunler.AsNoTracking()
            .Where(u => u.Id == orijinal.UrunId && u.TenantId == tenantId)
            .Select(u => (decimal?)u.HasGrami).FirstOrDefaultAsync() ?? 0m;

        // Adet has gramdan türetilir: hareket satırı adedi ayrıca tutmuyor.
        var oncekiAdet = urunHas > 0m ? Yuvarla3(oncekiIade / urunHas) : 0m;
        var toplamAdet = OrijinalAdet(orijinal, urunHas);
        var kalan = Yuvarla3(toplamAdet - oncekiAdet);

        if (kalan <= 0m)
            throw new BadRequestException("Bu işlemin tamamı zaten iade edilmiş");

        return (orijinal, kalan);
    }

    /// <summary>
    /// Orijinal işlemin adedi. Hareket satırı adedi tutmaz; has gramdan
    /// türetilir (<c>|hasGrami| / ürünün birim has gramı</c>).
    /// </summary>
    /// <summary>
    /// Orijinal satırın adedi.
    ///
    /// <para>
    /// P-08 / Y-14 — ÖNCE KOLON: <c>hareketler.adet</c> doluysa TÜRETME YAPILMAZ.
    /// ⚠ Aşağıdaki türetme (|has_grami| ÷ ürünün birim hası) bir VARSAYIMDIR ve
    /// ürünün ayarı satıştan SONRA değiştiyse yanlış sonuç verir; has bacağı
    /// olmayan üründe ise sessizce <c>1</c> döner. Kolon geldiğinden beri bu yol
    /// YALNIZ kolondan önce yazılmış eski satırlar içindir.
    /// </para>
    /// </summary>
    private decimal OrijinalAdet(Data.Hareket orijinal, decimal? urunHas = null)
    {
        if (orijinal.Adet is { } adet && adet > 0m) return adet;

        var birim = urunHas ?? db.Urunler.AsNoTracking()
            .Where(u => u.Id == orijinal.UrunId).Select(u => (decimal?)u.HasGrami).FirstOrDefault() ?? 0m;
        return birim > 0m ? Yuvarla3(Math.Abs(orijinal.HasGrami) / birim) : 1m;
    }

    // ─── Takas (eski → yeni) ──────────────────────────────────────────────────
    /// <summary>
    /// Eski ürünün değeri farkTl içinde paraya çevrilir; has bacağı deftere
    /// yazılır, cariye YAZILMAZ (K3/K4 — aynı malı iki kez alacaklandırmamak için).
    /// </summary>
    /// <summary>PAKET GOC-C (#7): değer &gt; 0 değilse 400 — ileti GirdiSozlugu'ndan (ekranın alan altı metniyle AYNI).</summary>
    private static void TakasPozitif(decimal d, string alan)
    {
        if (d < 0m) Common.Dogrulama.GirdiKapisi.Denetle(new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(Kuyumcu.Core.Domain.Girdi.GirdiKodu.SayiEksi), alan);
        if (d == 0m) Common.Dogrulama.GirdiKapisi.Denetle(new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(Kuyumcu.Core.Domain.Girdi.GirdiKodu.SayiSifir), alan);
    }

    public async Task<object> Takas(TakasDto dto, AktifKullanici islem)
    {
        // PAKET GOC-C (#7): 0 gram / 0 TL eski altın ve 0 TL verilen ürün deftere yazılamaz — servis
        // yolunda da (DTO özniteliği yalnız controller'da koşar). Kapsam envanterin #7'si: SIFIR/EKSİ reddi.
        // Ondalık hanesi bu uçta değiştirilmedi (gram 4 hane ve yarım kuruş ölçüsü mevcut sınır testlerinin
        // sözleşmesi — CIFTBACAK/YASAL3 — kolon kapısı KolonTavani'da kalır).
        TakasPozitif(dto.EskiUrunGram, "eskiUrunGram");
        TakasPozitif(dto.EskiUrunBirimFiyatTlHasGram, "eskiUrunBirimFiyatTlHasGram");
        TakasPozitif(dto.YeniUrunBirimFiyatTl, "yeniUrunBirimFiyatTl");
        var subeId = SubeBelirle(islem, "Takas için şube seçimi gerekli");

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);
        await IslemDogrulayici.CariDogrula(db, dto.CariId, islem.TenantId);

        var ayar = Ayarlar.Bul(dto.EskiUrunAyarId)
            ?? throw new BadRequestException($"Geçersiz ayar: {dto.EskiUrunAyarId}");
        await IslemFiyatKapisi.Denetle(db, islem, islem.TenantId, Kuyumcu.Core.Domain.Girdi.IslemFiyatBaglami.Takas, ayar.Maden, dto.EskiUrunBirimFiyatTlHasGram, "eskiUrunBirimFiyatTlHasGram", dto.BantOnay, dto.BantGerekce);   // PAKET ISLEM-BANT

        var yeniUrun = await UrunBul(dto.YeniUrunId, islem.TenantId);
        Urun.UrunAdetKapisi.Dogrula(yeniUrun.Birim, dto.YeniUrunAdet, "yeniUrunAdet");   // PAKET GOC-C (#6 · K14): ADET birimli üründe miktar tam sayı.

        // Takasta müşteriye VERİLEN ürün de satışa sunulmuş sayılır — md.11/(e).
        Urun.TasKapisi.SatisaUygunMu(yeniUrun);

        var yeniStok = await StokBul(dto.YeniUrunId, subeId, islem.TenantId);

        // P-21 (Y-23): takasta verilen mal da satılabilir stoktan çıkar.
        BlokeKapisi.SatilabilirDogrula(yeniStok, dto.YeniUrunAdet, "Takas (verilen ürün)");

        // ═════════════════════════════════════════════════════════════════════
        // K20-3 (paket 20) — TAKASTA MADEN KARIŞTIRMA YASAĞI.
        //
        // TAKAS satırı TEK bir net has bacağı yazar: (eski has − yeni has).
        // Müşteri 20 g gümüş verip 5 g altın alsaydı bu çıkarma iki FARKLI
        // madenin gramını tek sayıda birleştirir ve defterde "15 g altın girdi"
        // gibi görünürdü. Fail-closed: aynı madenin takası serbest, maden
        // değiştiren takas REDDEDİLİR (iki ayrı işlem olarak girilir).
        // ═════════════════════════════════════════════════════════════════════
        var eskiMaden = Kuyumcu.Core.Domain.Ayarlar.MadenBul(ayar.Id) ?? Kuyumcu.Core.Domain.Maden.Altin;
        var yeniMaden = Kuyumcu.Core.Domain.Ayarlar.MadenBul(yeniUrun.AyarId) ?? Kuyumcu.Core.Domain.Maden.Altin;
        if (eskiMaden != yeniMaden)
            throw new BadRequestException(
                $"Takasta maden değiştirilemez: alınan {Kuyumcu.Core.Domain.Maden.Ad(eskiMaden).ToLowerInvariant()}, " +
                $"verilen {Kuyumcu.Core.Domain.Maden.Ad(yeniMaden).ToLowerInvariant()}. " +
                "Farklı madenlerin gramları tek satırda toplanamaz — işlemi hurda alışı " +
                "ve satış olarak ayrı ayrı girin.");

        // ═════════════════════════════════════════════════════════════════════
        // PAKET PARA-DEMO (madde 3) — TAKASTA SATICI KİMLİĞİ (VUK md.234 · TBK md.283).
        // Hurda alışı / eski altınla ödemeyle AYNI kapı; tutar eşiği YOK, FAIL-CLOSED.
        // Stopajlı satıcı tipi (esnaf muaflığı) takasta REDDEDİLİR: stopaj ödenecek
        // tutarı düşürür, takas farkı ise stopajsız hesaplanır — iki sayı tutmazdı.
        // O işlem hurda alışı + satış olarak AYRI girilir (yeni hesap İCAT EDİLMEDİ).
        // ═════════════════════════════════════════════════════════════════════
        var saticiTipi = hurda.Dogrula(dto.Satici);
        if (Data.SaticiTipi.PusulaGerekir(saticiTipi) && hurda.StopajOran(saticiTipi) != 0m)
            throw new BadRequestException(
                "Takas: bu satıcı tipinde eski altın bedelinden stopaj kesilir; takas farkı stopajsız "
                + "hesaplandığı için iki tutar tutmaz. İşlemi Hurda Alış ve Satış olarak ayrı ayrı girin.");

        var eskiHas     = HasHesap.ToHasGram(dto.EskiUrunGram, ayar);
        var eskiDeger   = eskiHas * dto.EskiUrunBirimFiyatTlHasGram;
        var yeniDeger   = dto.YeniUrunAdet * dto.YeniUrunBirimFiyatTl;
        var farkTl      = Yuvarla2(yeniDeger - eskiDeger);   // pozitif = müşteri öder
        var yeniUrunHas = dto.YeniUrunAdet * yeniUrun.HasGrami;
        var now         = DateTime.UtcNow;

        // MASAK Faz 1 — el değiştiren DEĞER eşiğe verilir, fark değil (Bertan, mühür):
        // 900.000'lik eski + 910.000'lik yeni → fark 10.000 ama 1,8 M el değiştirdi.
        var masakTutar = Math.Max(eskiDeger, yeniDeger);
        var masakGerek = await masak.DogrulaKumulatif(
            db, islem.TenantId, dto.CariId, masakTutar, dto.Masak, now, islem);

        Data.Kasa? kasa = string.IsNullOrEmpty(dto.KasaId)
            ? null
            : await IslemDogrulayici.KasaDogrula(db, dto.KasaId, islem.TenantId, "TRY", now);

        // 459 s. VUK — TEVSİK KAPISI. Eskiden takas bu kapıyı HİÇ çağırmıyordu ama
        // hareketi "NAKIT" damgalayıp kümülatif toplamı besliyordu: başkasının
        // haddini doldururken kendisi denetlenmiyordu. MUTLAK DEĞER: fark negatifse
        // (biz öderiz, tediye) o da hadde tabidir. Transaction'dan ÖNCE.
        // ⚠ İŞLEM TUTARI KURALI (§4.1.2) TAKASA UYGULANMAZ ve bu bilinçlidir.
        // Trampa 459 s. Tebliğ'de HİÇ geçmiyor ("takas/trampa/ayni" kelimeleri
        // metinde yok). Mevzuat araştırması (2026-09-15) bu başlıkta DÜŞÜK güven
        // verdi ve doğrulama merceği aksi yönde bir özelge buldu. Doğrulanmamış
        // bir yorum yüzünden her altın takası bloke edilmez; yalnız fiilen el
        // değiştiren NAKİT FARK denetlenir. Mali müşavir teyidi beklemektedir.
        if (kasa is not null && dto.OdemeTuru == "NAKIT" && farkTl != 0m)
            await tevsik.Dogrula(db, islem.TenantId, dto.CariId, Math.Abs(farkTl), now);

        var vade = await Vade.VadeKapisi.Hazirla(
            db, islem.TenantId, dto.CariId, dto.VadeGun,
            kasa is null && farkTl > 0m ? farkTl : 0m, alacakYonu: true, now, dto.KurSnapshotId);

        // PARA-DEMO (madde 3): pusula numarası transaction ÖNCESİ alınır (HurdaAlis deseni).
        var takasPusulaNo = Data.SaticiTipi.PusulaGerekir(saticiTipi)
            ? await FaturaNoUretici.SiradakiPusula(db, islem.TenantId, Zaman.UtcdenYerele(now).Year)
            : null;
        // Mükellef satıcının belgesi KENDİ faturasıdır (pusula düzenlenmez); numarası
        // kaybolmasın diye takas satırının açıklamasına yazılır (yeni kolon açılmadı).
        var takasAciklama = saticiTipi == Data.SaticiTipi.Mukellef
            ? $"{(string.IsNullOrWhiteSpace(dto.Aciklama) ? "Takas" : dto.Aciklama.Trim())} · satıcı faturası {dto.Satici!.TedarikciFaturaNo!.Trim()}"
            : dto.Aciklama;

        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, dto.CariId, now);   // PAKET MASAK-ESZAMAN: işlem + CARİ SATIRI KİLİDİ
        // PAKET MASAK-ESZAMAN: kümülatif kapılar kilit altında, güncel toplamla YİNELENİR (eşik/kural aynı).
        masakGerek = await masak.KilitAltindaYinele(db, islem.TenantId, dto.CariId, masakTutar, dto.Masak, now, masakGerek, islem);
        if (kasa is not null && dto.OdemeTuru == "NAKIT" && farkTl != 0m)
            await tevsik.Dogrula(db, islem.TenantId, dto.CariId, Math.Abs(farkTl), now);

        // TAKAS hareketi: net TL farkı + net has gram hareketi
        var hareketId = Kimlik.Ulid();
        Data.HareketFabrikasi.Ekle(db,
            new Data.OdemeIzi(
                Nakit:    kasa is not null && dto.OdemeTuru == "NAKIT" ? Math.Abs(farkTl) : 0m,
                Veresiye: kasa is null && !string.IsNullOrEmpty(dto.CariId) ? Math.Abs(farkTl) : 0m,
                // Fark POZİTİFSE müşteri bize borçlanır → vadelidir. NEGATİFSE borç
                // bizimdir; onu müşterinin vade listesine yazmak alacağı borç gösterirdi.
                Vade:     kasa is null && !string.IsNullOrEmpty(dto.CariId) && farkTl > 0m ? vade : null),
            new Data.Hareket
        {
            Id            = hareketId,
            TenantId      = islem.TenantId,
            SubeId        = subeId,
            Tur           = "TAKAS",
            Tarih         = now,
            Aciklama      = takasAciklama,
            TlTutari      = farkTl,                              // müşteriden alınan fark
            MasakOlcuTl   = Yuvarla2(masakTutar),                // YASAL-3 § 4.1: el değiştiren değer (kümülatif ölçü) — Max(eski, yeni), ikinci formül yok
            HasGrami      = Yuvarla6(eskiHas - yeniUrunHas),     // net fiziki has hareketi
            // K20-3: NET has tek bir madene aittir — iki maden arası takas
            // yukarıda REDDEDİLDİ; buraya yalnız aynı madenin takası gelir.
            Maden         = Kuyumcu.Core.Domain.Ayarlar.MadenBul(ayar.Id) ?? Kuyumcu.Core.Domain.Maden.Altin,
            OdemeTuru     = OdemeTutarKapisi.Etiket(farkTl, kasa is not null ? dto.OdemeTuru : "VERESIYE"),   // P-23: fark 0 = para el değiştirmedi
            KurSnapshotId = dto.KurSnapshotId,
            CariId        = dto.CariId,
            UrunId        = dto.YeniUrunId,
            // PAKET CIFT-BACAK (HATA-3): verilen ürünün ADEDİ satırda durur (stok bilgisi).
            Adet          = dto.YeniUrunAdet,
            // Tur 2 (hasım KRİTİK): alınan eski has İŞLEM ANINDA donar; ürün kartına bağlanmaz.
            TakasEskiHasGrami = Yuvarla6(eskiHas),
            KullaniciId   = islem.Id,
            Olusturuldu   = now,
        });
        masak.Kaydet(db, masakGerek, hareketId, dto.CariId, masakTutar, dto.Masak, islem, now, MasakKaynakTuru.Hareket);   // MASAK Faz 2

        // PARA-DEMO (madde 3): gider pusulası takas satırıyla AYNI commit'te doğar; brüt =
        // eski altının bedeli (stopajlı tip yukarıda reddedildi → net = brüt).
        var takasPusula = takasPusulaNo is null ? null : hurda.Kaydet(
            db, takasPusulaNo, dto.Satici!, hareketId, subeId, dto.CariId, ayar.Id,
            dto.EskiUrunGram, Yuvarla6(eskiHas), Yuvarla2(eskiDeger), islem, now);

        // Yeni ürün stoktan çıkar — takasta da mal satılmıştır, maliyeti düşülür
        // PAKET 25: takasta verilen yeni mal da bir reyondan çıkar.
        await StokDegistir(dto.YeniUrunId, subeId, islem.TenantId, -dto.YeniUrunAdet,
                           yeniUrun.HasGrami, now, dto.ReyonId);
        await maliyet.PartiTuket(islem.TenantId, subeId, dto.YeniUrunId, dto.YeniUrunAdet, now,
            await maliyet.Yontem(islem.TenantId), hareketId);

        // Fark kasadan geçmediyse cariye yazılır; has bacağı cariye yazılmaz
        if (kasa is not null && farkTl != 0m)
        {
            var kasaHareketId = await KasaGuncelle(kasa, islem.TenantId, farkTl, now, hareketId, dto.OdemeTuru);
            // PAKET 31 — takas farkı kartla tahsil edildiyse POS defterine yazılır (fark İŞARETLİ).
            await PosDefterineYaz(kasa, islem, dto.OdemeTuru, dto.PosCihazId, dto.PosTaksitAdedi,
                farkTl, hareketId, kasaHareketId, dto.KurSnapshotId, dto.CariId, now);
        }
        else if (!string.IsNullOrEmpty(dto.CariId) && farkTl != 0m)
            await CariBakiyeGuncelle(dto.CariId, islem.TenantId, farkTl, 0m, now);

        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] TAKAS: eskiHas={EskiHas} yeniHas={YeniHas} fark={FarkTl} TL",
            islem.TenantId, eskiHas, yeniUrunHas, farkTl);

        return new
        {
            eskiHasGrami = Yuvarla6(eskiHas),
            yeniHasGrami = yeniUrunHas,
            eskiDegerTl  = eskiDeger,
            yeniDegerTl  = yeniDeger,
            farkTl,
            // PARA-DEMO (UX-S27): kimlik kapısının sonucu yanıtta — mükellefte pusula yok (null).
            saticiTipi,
            giderPusulasi = takasPusula is null ? null : new
            {
                id = takasPusula.Id, takasPusula.PusulaNo, takasPusula.BrutTutar, takasPusula.NetOdenen,
                takasPusula.DuzenlemeSonTarihi,
            },
        };
    }

    // ─── İşçilik ──────────────────────────────────────────────────────────────
    /// <summary>Peşin tahsil edilirse yalnız kasaya yazılır; edilmezse yalnız cari borçlanır.</summary>
    public async Task<object> Iscilik(IscilikDto dto, AktifKullanici islem)
    {
        // PAKET GOC-C (K9): işçilik tutarı > 0 ve en çok 2 ondalık — 0,001 TL eskiden deftere 0,00 düşebiliyordu.
        Common.Dogrulama.GirdiKapisi.Tutar(dto.IscilikTl, "iscilikTl");
        var subeId = SubeBelirle(islem, "İşçilik için şube seçimi gerekli");

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);
        await IslemDogrulayici.CariDogrula(db, dto.CariId, islem.TenantId);

        var now = DateTime.UtcNow;
        var tutar = Yuvarla2(dto.IscilikTl);
        var masakGerek = await masak.DogrulaKumulatif(
            db, islem.TenantId, dto.CariId, tutar, dto.Masak, now, islem);   // MASAK Faz 1

        Data.Kasa? kasa = string.IsNullOrEmpty(dto.KasaId)
            ? null
            : await IslemDogrulayici.KasaDogrula(db, dto.KasaId, islem.TenantId, "TRY", now);

        // 459 s. VUK — TEVSİK KAPISI (takasla aynı gerekçe: eskiden hiç çağrılmıyordu).
        if (kasa is not null && dto.OdemeTuru == "NAKIT")
            await tevsik.Dogrula(db, islem.TenantId, dto.CariId, tutar, now);

        var vade = await Vade.VadeKapisi.Hazirla(
            db, islem.TenantId, dto.CariId, dto.VadeGun,
            kasa is null ? tutar : 0m, alacakYonu: true, now, dto.KurSnapshotId);

        await using var tx = await db.Database.CariIcinBaslat(islem.TenantId, dto.CariId, now);   // PAKET MASAK-ESZAMAN: işlem + CARİ SATIRI KİLİDİ
        // PAKET MASAK-ESZAMAN: kümülatif kapılar kilit altında, güncel toplamla YİNELENİR (eşik/kural aynı).
        masakGerek = await masak.KilitAltindaYinele(db, islem.TenantId, dto.CariId, tutar, dto.Masak, now, masakGerek, islem);
        if (kasa is not null && dto.OdemeTuru == "NAKIT")
            await tevsik.Dogrula(db, islem.TenantId, dto.CariId, tutar, now);

        var hareketId = Kimlik.Ulid();
        Data.HareketFabrikasi.Ekle(db,
            new Data.OdemeIzi(
                Nakit:    kasa is not null && dto.OdemeTuru == "NAKIT" ? tutar : 0m,
                Veresiye: kasa is null ? tutar : 0m,
                Vade:     kasa is null && tutar > 0m ? vade : null),
            new Data.Hareket
        {
            Id            = hareketId,
            TenantId      = islem.TenantId,
            SubeId        = subeId,
            Tur           = "ISCILIK",
            Tarih         = now,
            Aciklama      = dto.Aciklama,
            TlTutari      = tutar,
            HasGrami      = 0m,
            OdemeTuru     = kasa is not null ? dto.OdemeTuru : "VERESIYE",
            KurSnapshotId = dto.KurSnapshotId,
            CariId        = dto.CariId,
            UrunId        = dto.UrunId,
            KullaniciId   = islem.Id,
            Olusturuldu   = now,
        });
        masak.Kaydet(db, masakGerek, hareketId, dto.CariId, tutar, dto.Masak, islem, now, MasakKaynakTuru.Hareket);   // MASAK Faz 2

        if (kasa is not null)
        {
            var kasaHareketId = await KasaGuncelle(kasa, islem.TenantId, tutar, now, hareketId, dto.OdemeTuru);
            // PAKET 31 — işçilik bedeli kartla tahsil edildiyse POS defterine yazılır.
            await PosDefterineYaz(kasa, islem, dto.OdemeTuru, dto.PosCihazId, dto.PosTaksitAdedi,
                tutar, hareketId, kasaHareketId, dto.KurSnapshotId, dto.CariId, now);
        }
        else
            await CariBakiyeGuncelle(dto.CariId, islem.TenantId, tutar, 0m, now);

        await db.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] ISCILIK: cari={CariId} {IscilikTl} TL",
            islem.TenantId, dto.CariId, tutar);

        // P-23c: hareket kimliği yanıta EKLENDİ (additif). Vezne fişi, tahsil
        // sonucunda doğan defter satırını bu kimlikle kendine bağlar; aksi hâlde
        // "vezne fişi tahsil edildi ama defterde karşılığı hangi satır" sorusu
        // zaman damgasından TAHMİN edilirdi (Anayasa md.1).
        return new { cariId = dto.CariId, iscilikTl = tutar, hareketId };
    }

    // ─── Dahili yardımcılar ───────────────────────────────────────────────────

    /// <summary>Nest ile birebir: token'da şube seçili değilse hata (fallback yok).</summary>
    private static string SubeBelirle(AktifKullanici islem, string hataMesaji)
    {
        if (string.IsNullOrEmpty(islem.SubeId))
            throw new BadRequestException(hataMesaji);
        return islem.SubeId!;
    }

    /// <summary>
    /// Ödeme kurgusunu çözer (K2/K3): kasa doğrulanır, ödenen ve kalan tutar
    /// hesaplanır. Kasa yoksa tamamı cari borcu; kasa varsa ve OdenenTl
    /// belirtilmediyse tamamı peşindir.
    /// </summary>
    private async Task<(Data.Kasa? Kasa, decimal Odenen, decimal Kalan)> OdemeCoz(
        string? kasaId, decimal? odenenTl, decimal toplamTl, string tenantId, DateTime an)
    {
        if (string.IsNullOrEmpty(kasaId))
        {
            if (odenenTl is > 0m)
                throw new BadRequestException("Ödenen tutar belirtildiyse kasa ID zorunludur");
            return (null, 0m, toplamTl);
        }

        var kasa = await IslemDogrulayici.KasaDogrula(db, kasaId, tenantId, "TRY", an);
        var odenen = Yuvarla2(odenenTl ?? toplamTl);

        if (odenen > toplamTl)
            throw new BadRequestException(
                $"Ödenen tutar işlem tutarını aşamaz. İşlem: {toplamTl}, ödenen: {odenen}");

        return (kasa, odenen, Yuvarla2(toplamTl - odenen));
    }

    /// <summary>
    /// Maliyetin has karşılığını hesaplamak için o anki has altın fiyatı.
    /// Bulunamazsa null döner — has maliyeti UYDURULMAZ, boş bırakılır.
    /// </summary>
    private async Task<decimal?> HasFiyatiOku(string kurSnapshotId, string tenantId) =>
        await db.KurSnapshots.AsNoTracking()
            .Where(k => k.Id == kurSnapshotId && k.TenantId == tenantId)
            .Select(k => (decimal?)k.AltinHasFiyati)
            .FirstOrDefaultAsync();

    private async Task<Data.Urun> UrunBul(string urunId, string tenantId) =>
        await db.Urunler.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == urunId && u.TenantId == tenantId && u.Aktif)
        ?? throw new NotFoundException("Ürün bulunamadı veya aktif değil");

    private async Task<Data.Stok> StokBul(string urunId, string subeId, string tenantId) =>
        await db.Stoklar.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UrunId == urunId && s.SubeId == subeId && s.TenantId == tenantId)
        ?? throw new BadRequestException("Bu şubede ürün stoğu bulunamadı");

    /// <summary>
    /// Şube stoğunu değiştirir ve PAKET 25 ile birlikte yerleşim (reyon)
    /// bacağını da yürütür. Dönüş değeri: kullanıcıya gösterilecek yerleşim
    /// UYARISI (reyon seçilmediği için mal reyonlardan düşüldüyse ya da giriş
    /// ATANMAMIŞ'a yazıldıysa); uyarı yoksa null.
    ///
    /// ⚠ Stok aritmetiği DEĞİŞMEDİ — yerleşim bacağı gövdenin SONUNA eklendi.
    /// </summary>
    private async Task<string?> StokDegistir(
        string urunId, string subeId, string tenantId,
        decimal adetDelta, decimal hasGramiPerAdet, DateTime now,
        string? reyonId = null, string? varsayilanReyonId = null)
    {
        // Aynı transaction içinde henüz kaydedilmemiş satırları da görebilmek için
        // önce EF change tracker'a (Local) bakılır. Kiracı filtresi her iki yolda da uygulanır.
        var mevcut = db.Stoklar.Local
                .FirstOrDefault(s => s.UrunId == urunId && s.SubeId == subeId && s.TenantId == tenantId)
            ?? await db.Stoklar
                .FirstOrDefaultAsync(s => s.UrunId == urunId && s.SubeId == subeId && s.TenantId == tenantId);

        var yeniAdet = mevcut is not null ? mevcut.Adet + adetDelta : adetDelta;
        var yeniHas  = mevcut is not null
            ? mevcut.ToplamHasGram + adetDelta * hasGramiPerAdet
            : adetDelta * hasGramiPerAdet;

        if (yeniAdet < 0m) throw new BadRequestException("Stok sıfırın altına düşemez");

        if (mevcut is not null)
        {
            mevcut.Adet          = Yuvarla3(yeniAdet);
            mevcut.ToplamHasGram = Yuvarla6(yeniHas);
            mevcut.Guncellendi   = now;
        }
        else
        {
            db.Stoklar.Add(new Data.Stok
            {
                Id            = Kimlik.Ulid(),
                TenantId      = tenantId,
                SubeId        = subeId,
                UrunId        = urunId,
                Adet          = Yuvarla3(yeniAdet),
                ToplamHasGram = Yuvarla6(yeniHas),
                Guncellendi   = now,
            });
        }

        // ── PAKET 25 — YERLEŞİM BACAĞI ──────────────────────────────────────
        // `stoklar` tek gerçek kaynaktır ve yukarıda güncellendi. Burada yalnız
        // o adedin ŞUBE İÇİ KIRILIMI yürütülür. Yeni stok adedi sorgudan DEĞİL
        // hesaptan verilir: aynı transaction içinde henüz kaydedilmemiştir.
        if (adetDelta < 0m)
            return await yerlesim.CikisDus(
                tenantId, subeId, urunId, -adetDelta, reyonId, Yuvarla3(yeniAdet), now);
        if (adetDelta > 0m)
            return await yerlesim.GirisEkle(
                tenantId, subeId, urunId, adetDelta, reyonId, varsayilanReyonId, now);
        return null;
    }

    private async Task CariBakiyeGuncelle(
        string cariId, string tenantId,
        decimal tlDelta, decimal hasGramDelta, DateTime now)
    {
        // PAKET CARI-BAKIYE: TEK YAZICI (Common/CariBakiyeServisi.cs) — Local'deki satırı bulma,
        // kiracı kilidi ve TL 2 / has 6 hane yuvarlama orada, tek yerde.
        await Common.CariBakiyeYazici.Uygula(db, tenantId, cariId, tlDelta, hasGramDelta, now);
    }

    /// <summary>
    /// Doğrulanmış (izlenen) kasanın bakiyesini günceller ve hareket kaydı düşer.
    /// K6: hareket, kasanın kendi para birimiyle kaydedilir.
    /// </summary>
    /// <returns>
    /// PAKET 31 — yazılan kasa hareketinin kimliği; pos_tahsilatlari.kasa_hareket_id
    /// bunu taşır. ⚠ BAKİYE ARİTMETİĞİ DEĞİŞMEDİ, yalnız imza genişledi.
    /// </returns>
    private async Task<string> KasaGuncelle(
        Data.Kasa kasa, string tenantId,
        decimal tutarDelta, DateTime now,
        string hareketId, string odemeTuru)
    {
        // PAKET P-44: alış, hurda alımı, iade ve para üstü bu tek yoldan geçer;
        // hepsi KAPISIZDI. Canlı ölçüm 2026-09-23: kasa −738.298,80 TL, dört
        // ALIS (−858.500,00) + bir HURDA_ALIS (−24.548,80) bakiyeyi aşmıştı.
        // PARA-DEMO-2 (madde 3): NAKİT çıkışı ayrıca kasanın NAKİT payına bakar (toplam kapısı içinde).
        await Modules.Kasa.KasaBakiyeKapisi.NakitDogrula(db, tenantId, kasa, tutarDelta, odemeTuru);

        await BakiyeKilidi.Kilitle(db, kasa);   // PAKET KAPORA-ESZAMAN tur 2: kilitli taze okuma (kayıp güncelleme)
        kasa.Bakiye      = Yuvarla2(kasa.Bakiye + tutarDelta);
        kasa.Guncellendi = now;

        var kasaHareketId = Kimlik.Ulid();
        db.KasaHareketleri.Add(new Data.KasaHareket
        {
            Id          = kasaHareketId,
            TenantId    = tenantId,
            KasaId      = kasa.Id,
            HareketId   = hareketId,
            // İŞARETLİ yazılır: + giriş, − çıkış. Math.Abs KULLANILMAZ.
            // Eskiden mutlak değer yazılıyordu; bu yüzden kasa bakiyesi append-only
            // defterden YENİDEN TÜRETİLEMİYORDU (Bertan kuralı ihlali) ve hurda
            // alımı gibi para ÇIKIŞLARI gün sonu raporunda TAHSİLAT gibi görünüyordu.
            // Canlı ölçüm (2026-09-12): bakiye −49.576 iken defter toplamı +199.576.
            Tutar       = Yuvarla2(tutarDelta),
            ParaBirimi  = kasa.ParaBirimi,
            OdemeTuru   = odemeTuru,
            Tarih       = now,
            Olusturuldu = now,
        });
        return kasaHareketId;
    }

    // ── PAKET 31 — POS ALACAK DEFTERİ BACAĞI ────────────────────────────────
    /// <summary>
    /// KART bacağı için POS alacak satırı yazar. ⚠ KASA BAKİYESİNE DOKUNMAZ:
    /// pos_tahsilatlari bir YAN DEFTERDİR (ADR-0016).
    /// <para>
    /// Çağrı KasaGuncelle'nin İÇİNE konmadı çünkü kasa hareketi Id'si gerekiyor
    /// ve her çağrı noktasının cihaz/taksit girdisi farklı. Bu yüzden "KART
    /// bacağı var ama PosTahsilat yok" durumunu yakalayan DB denetim testi
    /// (CihazZorunlu=true kiracıda 6 nokta) Faz A'nın kabul ölçütüdür.
    /// </para>
    /// </summary>
    /// <remarks>
    /// PAKET P-23: dönüş tipi <c>Task</c> → <c>Task&lt;string?&gt;</c> oldu; yazılan POS
    /// tahsilatının kimliği ödeme kalemine (<c>odeme_kalemleri.pos_tahsilat_id</c>)
    /// konabilsin diye. Yazılmadıysa (kart değil / cihaz yok) <c>null</c> döner.
    /// Mevcut altı çağrı noktası dönüşü kullanmaz; davranış DEĞİŞMEZ.
    /// </remarks>
    private async Task<string?> PosDefterineYaz(
        Data.Kasa? kasa, AktifKullanici islem, string odemeTuru, string? istenenCihazId,
        int? taksitAdedi, decimal brutIsaretli, string hareketId, string kasaHareketId,
        string kurSnapshotId, string? cariId, DateTime now)
    {
        if (kasa is null || odemeTuru != "KART" || brutIsaretli == 0m) return null;

        var cihaz = await Pos.PosServisi.CihazCoz(
            db, pos, islem.TenantId, kasa.SubeId, istenenCihazId, kasa.Id);
        if (cihaz is null) return null;   // cihaz yok + CihazZorunlu kapalı → bugünkü davranış

        var taksit = taksitAdedi is > 1 ? taksitAdedi.Value : 1;
        var (oran, valorGun) = await Pos.PosServisi.TaksitOraniCoz(
            db, islem.TenantId, cihaz.Id, taksit);

        return Pos.PosDefteri.Ekle(db, pos, cihaz, brutIsaretli, taksit, hareketId, kasaHareketId,
            kurSnapshotId, cariId, now, oran, valorGun).Id;
    }

    private async Task<object> HareketGetir(string urunId, string tenantId,
                                            string? yerlesimUyarisi = null)
    {
        var h = await db.Hareketler.AsNoTracking()
            .Where(x => x.UrunId == urunId && x.TenantId == tenantId)
            .OrderByDescending(x => x.Olusturuldu)
            .FirstOrDefaultAsync();

        var govde = (object?)h ?? new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("İşlem tamamlandı") };
        if (string.IsNullOrEmpty(yerlesimUyarisi)) return govde;

        // ── PAKET 25 · UX-S28 — UYARI YUTULMAZ ──────────────────────────────
        // Yanıt ŞEKLİ KORUNUR: mevcut alanlar (id, fisId, …) aynen kalır,
        // yalnız `yerlesimUyarisi` EKLENİR. Sunucunun kendi JSON ayarlarıyla
        // (web varsayılanı = camelCase) düğüme çevrilip alan eklenir; elle
        // alan listesi yazmak, ileride Hareket'e eklenen bir alanın sessizce
        // kaybolması demek olurdu.
        var dugum = System.Text.Json.JsonSerializer.SerializeToNode(
            govde, govde.GetType(),
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
            !.AsObject();
        dugum["yerlesimUyarisi"] = yerlesimUyarisi;
        return dugum;
    }

    // ─── Fiş okuma (paket 1 Faz A — TEK belge veri kaynağı) ───────────────────
    //
    // FİŞ İÇİN AYRI TABLO YOKTUR ve BİLİNÇLİDİR (Entities.cs FisId gerekçesi):
    // fiş = aynı fis_id'yi taşıyan hareket kümesidir. Bugüne kadar bu kümeyi
    // OKUYAN bir uç yoktu; kesilen fiş ekranda bir daha görüntülenemiyordu.
    //
    // ⚠ Ödeme bacakları fişin İLK hareketine bağlanır (SatisFisi'nde KasaGuncelle
    // ilkHareketId ile çağrılır); bu yüzden sorgu fişin TÜM hareket kimlikleri
    // üzerinden kurulur, tek bir kimlik varsayılmaz.
    //
    // Mehmet K1-5: yanıt cari ADI ve kimliğini taşır; telefon/adres/vergi no DÖNMEZ.
    public async Task<object> FisOku(string fisId, AktifKullanici islem)
    {
        if (string.IsNullOrWhiteSpace(fisId)) throw new NotFoundException("Fiş bulunamadı");

        var hareketler = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.FisId == fisId)
            .OrderBy(h => h.Olusturuldu).ThenBy(h => h.Id)
            .ToListAsync();

        // ── P-23 FAZ 2 · FİŞSİZ GEÇMİŞ HAREKET DE OKUNUR ────────────────────
        // ÖLÇÜLDÜ (kuyumcu_net, 2026-09-22): ödeme türü taşıyan 87 hareketin
        // yalnız 4'ünün fis_id'si var. Fiş kimliği, çok bacaklı satış fişi
        // ucundan ÖNCE yazılmış hiçbir harekette yoktur. Bu yüzden fiş = aynı
        // fis_id'yi taşıyan küme; fis_id YOKSA hareketin KENDİSİ tek satırlık
        // bir fiştir. Geri doldurma migration'ı kalemleri tam bu ölçütle
        // (COALESCE(fis_id, id)) yazdı; okuma ucu aynı ölçütü kullanmazsa
        // yazılan kırılım ekranda hiç görünmez.
        // Uç bu yüzden HAREKET kimliğini de kabul eder: hareketin fis_id'si
        // varsa o fişin tamamı, yoksa hareketin kendisi döner.
        if (hareketler.Count == 0)
        {
            var tek = await db.Hareketler.AsNoTracking()
                .FirstOrDefaultAsync(h => h.TenantId == islem.TenantId && h.Id == fisId);
            if (tek is not null)
            {
                hareketler = string.IsNullOrEmpty(tek.FisId)
                    ? [tek]
                    : await db.Hareketler.AsNoTracking()
                        .Where(h => h.TenantId == islem.TenantId && h.FisId == tek.FisId)
                        .OrderBy(h => h.Olusturuldu).ThenBy(h => h.Id)
                        .ToListAsync();
                fisId = tek.FisId ?? tek.Id;
            }
        }
        if (hareketler.Count == 0) throw new NotFoundException("Fiş bulunamadı");

        var urunIdler = hareketler.Where(h => !string.IsNullOrEmpty(h.UrunId))
            .Select(h => h.UrunId!).Distinct().ToList();
        var urunAd = urunIdler.Count == 0
            ? new Dictionary<string, string>()
            : await db.Urunler.AsNoTracking()
                .Where(u => u.TenantId == islem.TenantId && urunIdler.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Ad);

        var hareketIdler = hareketler.Select(h => h.Id).ToList();
        var bacaklar = await db.KasaHareketleri.AsNoTracking()
            .Where(kh => kh.TenantId == islem.TenantId
                         && kh.HareketId != null && hareketIdler.Contains(kh.HareketId))
            .OrderBy(kh => kh.Olusturuldu)
            .ToListAsync();

        // ── PAKET P-23 (R-17) — ÖDEME KALEMLERİ ─────────────────────────────
        // ⚠ ESKİ FİŞTE BOŞ DÖNER ve bu "kırılım BİLİNMİYOR" demektir, "ödeme
        //   yok" demek DEĞİLDİR (Anayasa md.1). `bacaklar` ve `veresiyeToplam`
        //   alanları AYNEN durduğu için okuyan taraf eski yolu kullanmaya
        //   devam edebilir; yanıt şekli yalnız GENİŞLEDİ.
        // ⚠ Pasif (stornolanmış) kalem de DÖNER — `aktif` bayrağıyla; fişin
        //   geçmişi gizlenmez.
        var odemeKalemleri = await db.OdemeKalemleri.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && k.FisId == fisId)
            .OrderBy(k => k.Sira)
            .ToListAsync();

        var cariId = hareketler.Select(h => h.CariId).FirstOrDefault(c => !string.IsNullOrEmpty(c));
        var cariAd = string.IsNullOrEmpty(cariId) ? null : await db.Cariler.AsNoTracking()
            .Where(c => c.Id == cariId && c.TenantId == islem.TenantId)
            .Select(c => c.Ad).FirstOrDefaultAsync();

        var kurId = hareketler.Select(h => h.KurSnapshotId).FirstOrDefault(k => !string.IsNullOrEmpty(k));
        var kur = string.IsNullOrEmpty(kurId) ? null : await db.KurSnapshots.AsNoTracking()
            .Where(k => k.Id == kurId && k.TenantId == islem.TenantId)
            .Select(k => new { k.Id, k.AltinHasFiyati, k.Kaynak, k.Olusturuldu })
            .FirstOrDefaultAsync();

        return new
        {
            fisId,
            tarih       = hareketler[0].Tarih,
            cariId,
            cariAd,
            faturaNo    = hareketler.Select(h => h.FaturaNo).FirstOrDefault(f => !string.IsNullOrEmpty(f)),
            kur,
            kalemSayisi = hareketler.Count,
            toplamTl    = Yuvarla2(hareketler.Sum(h => h.TlTutari)),
            toplamHas   = Yuvarla6(hareketler.Sum(h => h.HasGrami)),
            kalemler    = hareketler.Select(h => new
            {
                h.Id, h.Tur, h.Tarih, h.Aciklama, h.TlTutari, h.HasGrami, h.OdemeTuru,
                h.NakitTutar, h.VeresiyeTutar, h.ListeFiyati, h.IndirimTutari, h.Hediye,
                h.UrunId, h.FaturaNo,
                UrunAd = string.IsNullOrEmpty(h.UrunId) ? null : urunAd.GetValueOrDefault(h.UrunId),
            }).ToList(),
            bacaklar = bacaklar.Select(b => new
            {
                b.Id, b.KasaId, b.HareketId, b.Tutar, b.ParaBirimi, b.OdemeTuru, b.Tarih,
            }).ToList(),
            veresiyeToplam = Yuvarla2(hareketler.Sum(h => h.VeresiyeTutar ?? 0m)),
            // P-23: fişin KAYITLI ödeme kırılımı (çıkarım değil).
            odemeKalemleri = odemeKalemleri.Select(k => new
            {
                k.Id, k.Sira, k.Arac, k.TutarTl, k.ParaBirimi, k.DovizTutar,
                k.KasaId, k.KasaHareketId, k.PosTahsilatId, k.TaksitAdedi,
                k.Bilinmiyor, k.GeriDolduruldu, k.Aktif, k.StornoHareketId,
            }).ToList(),
            // Kalem toplamı YALNIZ aktif kalemlerden — stornolanmış para sayılmaz.
            odemeKalemToplam = Yuvarla2(odemeKalemleri.Where(k => k.Aktif).Sum(k => k.TutarTl)),
            // ── P-23 FAZ 2 · KAPSAM BAYRAĞI (hasım bulgusu 2026-09-22/ORTA) ──
            // "kalem yok" ile "ödeme yok" ayrımı artık KOD düzeyinde taşınır;
            // önceden yalnız kaynak yorumunda duruyordu ve okuyan taraf boş
            // diziyi "bu fişte ödeme yok" diye yorumlayabiliyordu (md.1 riski).
            kalemKapsami = Data.OdemeKalemKapsami.Belirle(
                odemeKalemleri.Count, odemeKalemleri.Count(k => k.Bilinmiyor)),
        };
    }

    // Nest'teki toFixed(2)/toFixed(3)/toFixed(6) karşılıkları
    private static decimal Yuvarla2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);
    // PAKET PATCH-3: tek yuvarlama kuralı KolonTavani'da (6 hane = fatura kuralı, AwayFromZero).
    private static decimal Yuvarla3(decimal d) => KolonTavani.Yuvarla3(d);
    private static decimal Yuvarla6(decimal d) => KolonTavani.Yuvarla6(d);
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/islemler")]
[Yetki.ModulYetkisi("islem")]
public sealed class SatisController(SatisServisi servis, StornoServisi storno) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/islemler/satis — Ürün satışı: stok düşer, kasa/cari güncellenir, deftere yazılır</summary>
    [HttpPost("satis")]
    public Task<object> Satis([FromBody] SatisOlusturDto dto) => servis.Satis(dto, Islem);

    /// <summary>POST /api/v1/islemler/alis — Ürün alışı: stok artar, kasa/cari güncellenir — MUHASEBE+</summary>
    [HttpPost("alis")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Alis([FromBody] AlisOlusturDto dto) => servis.Alis(dto, Islem);

    /// <summary>
    /// POST /api/v1/islemler/alis-fisi — PAKET L-1b: çok kalemli alış TEK fiş (tek
    /// transaction; tevsik/MASAK/kasa kapıları fiş BRÜT toplamıyla bir kez) — MUHASEBE+
    /// (tek kalemli /islemler/alis ile AYNI rol kümesi).
    /// </summary>
    [HttpPost("alis-fisi")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> AlisFisi([FromBody] AlisFisiDto dto) => servis.AlisFisi(dto, Islem);

    /// <summary>POST /api/v1/islemler/hurda-alis — Hurda altın alışı: has gram hesaplanır, fiyat ödenir — MUHASEBE+</summary>
    [HttpPost("hurda-alis")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> HurdaAlis([FromBody] HurdaAlisDto dto) => servis.HurdaAlis(dto, Islem);

    /// <summary>
    /// POST /api/v1/islemler/hurda-satis — PAKET P-20 (Y-10): biriken hurdanın
    /// rafineriye çıkışı. Has bacağı eksilir, TL bacağı yazılır — MUHASEBE+
    /// (hurda alışıyla AYNI yetki: aynı defter hattı, aynı sorumluluk).
    /// </summary>
    [HttpPost("hurda-satis")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<Kuyumcu.Contracts.Sayfalar.Hurda.HurdaSatisSonucu> HurdaSatis(
        [FromBody] HurdaSatisDto dto) => servis.HurdaSatis(dto, Islem);

    /// <summary>POST /api/v1/islemler/transfer — Şubeler arası stok transferi — MUDUR+</summary>
    [HttpPost("transfer")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Transfer([FromBody] TransferDto dto) => servis.Transfer(dto, Islem);

    /// <summary>
    /// POST /api/v1/islemler/satis-fisi — Çok bacaklı ödemeyle satış
    /// (nakit + kart + havale + veresiye tek fişte). Yetki: mevcut satışla aynı.
    /// </summary>
    [HttpPost("satis-fisi")]
    public Task<object> SatisFisi([FromBody] SatisFisiDto dto) => servis.SatisFisi(dto, Islem);

    /// <summary>
    /// GET /api/v1/islemler/fis/{fisId} — Fişin kalemleri, ödeme bacakları, kur kaydı.
    /// İşlem modülüne erişebilen tüm roller okuyabilir; kiracı dışı fis_id → 404.
    /// </summary>
    [HttpGet("fis/{fisId}")]
    public Task<object> FisOku(string fisId) => servis.FisOku(fisId, Islem);

    /// <summary>
    /// GET /api/v1/islemler/altin-bacagi/ayarlar — PAKET B-4: satış fişinin
    /// eski altın bacağı için ekranın bilmesi gereken YAPILANDIRMA (satıcı
    /// tipine göre stopaj oranları, alış iskontosu ön-dolgusu). Salt okunur;
    /// oranlar koda gömülü değildir (ADR-0005). Yetki: satış fişiyle aynı.
    /// </summary>
    [HttpGet("altin-bacagi/ayarlar")]
    public object AltinBacagiAyarlari() => servis.AltinBacagiAyarlari();

    /// <summary>GET /api/v1/islemler/iade-adaylari?tur=SATIS — İade edilebilecek işlemler</summary>
    [HttpGet("iade-adaylari")]
    public Task<object> IadeAdaylari(
        [FromQuery] string tur = "SATIS", [FromQuery, Range(1, 200)] int limit = 50)
        => servis.IadeAdaylari(tur, Islem, limit);

    /// <summary>POST /api/v1/islemler/satis-iade — Müşteri malı geri getirdi</summary>
    [HttpPost("satis-iade")]
    public Task<object> SatisIade([FromBody] IadeDto dto) => servis.SatisIade(dto, Islem);

    /// <summary>POST /api/v1/islemler/alis-iade — Malı tedarikçiye geri verdik — MUHASEBE+</summary>
    [HttpPost("alis-iade")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> AlisIade([FromBody] IadeDto dto) => servis.AlisIade(dto, Islem);

    /// <summary>POST /api/v1/islemler/takas — Eski ürün → yeni ürün takası</summary>
    [HttpPost("takas")]
    public Task<object> Takas([FromBody] TakasDto dto) => servis.Takas(dto, Islem);

    /// <summary>POST /api/v1/islemler/iscilik — İşçilik ücreti kaydet</summary>
    [HttpPost("iscilik")]
    public Task<object> Iscilik([FromBody] IscilikDto dto) => servis.Iscilik(dto, Islem);

    /// <summary>
    /// POST /api/v1/islemler/{hareketId}/storno — hatalı kaydı TERS KAYITLA
    /// hükümsüz kılar. Defter yerinde değişmez; kaynak satıra dokunulmaz.
    /// İş mantığı bu dosyaya YAZILMAZ (StornoServisi) — SAHIP/MUDUR.
    /// </summary>
    [HttpPost("{hareketId}/storno")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Storno(string hareketId, [FromBody] StornoDto dto)
        => storno.Storno(hareketId, dto, Islem);
}

/// <summary>Kur kaydından yalnız döviz kurlarını çeken projeksiyon.</summary>
internal sealed record OdemeSepeti_Kur(decimal? Usd, decimal? Eur, decimal? Gbp, bool IptalEdildi = false);
