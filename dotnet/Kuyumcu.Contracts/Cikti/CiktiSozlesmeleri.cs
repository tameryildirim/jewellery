// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-17 (kimlik Y-6) — ORTAK A4 ÇIKTI SÖZLEŞMESİ.
//
// NEDEN BURADA (PLAN § 1 md.9): uç YENİDİR, sözleşme tipi önce
// Kuyumcu.Contracts'a yazılır; sunucu imzası ile istemcinin ayrıştırdığı tip
// AYNI tiptir → derleyici kilidi (denetim.sh SOZLESME_KILITSIZ_TIP ölçütü (a)).
//
// NEDEN TEK TİP: satış fişi, tahsilat makbuzu, emanet teslim belgesi ve cari
// ekstresi bugüne dek YA HİÇ ÇIKTI VERMİYOR ya da her biri kendi işaretlemesini
// taşıyordu. Dört ayrı razor sayfası dört ayrı kâğıt düzeni demekti; kâğıt
// düzeni bir SUNUM kararıdır ve bir kez yazılır. Sunucu yalnız VERİYİ bu ortak
// biçimde döndürür, kâğıdı `Bilesenler/Yazdirma/A4Belge.razor` çizer.
//
// ⚠ ADR-0007 KORUNUR: sunucuya PDF kütüphanesi EKLENMEDİ. Çıktı tarayıcının
//   yazdırma motorundan alınır ("PDF olarak kaydet").
// ⚠ R-11 (P-01'de 1.1'e ertelendi): şablon MOTORU yoktur — şablon SABİTTİR.
//   Kiracı başına şablon tasarımı 1.0 kapsamında DEĞİLDİR.
// ═════════════════════════════════════════════════════════════════════════════

namespace Kuyumcu.Contracts.Cikti;

/// <summary>Ortak A4 şablonunun ürettiği belge türleri — kapalı liste.</summary>
public static class BelgeTurleri
{
    public const string SatisFisi = "SATIS_FISI";
    public const string TahsilatMakbuzu = "TAHSILAT_MAKBUZU";
    public const string EmanetTeslim = "EMANET_TESLIM";
    public const string CariEkstre = "CARI_EKSTRE";
    /// <summary>
    /// PAKET EKSIK-B1-SATINALMA (B1-2) — irsaliye dökümü (sevk = GİDEN, mal kabul = GELEN).
    /// Fiyat taşımaz (VUK md.230/5: irsaliyede fiyat ve bedel bilgileri hariç). Kâğıtta
    /// "bilgi amaçlı döküm" uyarısı basılır: anlaşmalı matbaa seri-sıra no ya da e-İrsaliye değildir.
    /// </summary>
    public const string Irsaliye = "IRSALIYE";
    /// <summary>
    /// D1-01 (18.10, rakip eşitleme) — kesilmiş faturanın kâğıt dökümü (A4 ya da 80 mm fiş).
    /// e-Fatura/e-Arşiv entegratörü bağlanana dek "bilgi amaçlı döküm" uyarısı basılır.
    /// </summary>
    public const string Fatura = "FATURA";
    /// <summary>
    /// D1-10 (18.10, rakip eşitleme) — cari hesap mutabakat formu: bugünkü bakiyeler (TL · has · döviz),
    /// yönleri yazılı; müşteri "mutabıkız / değiliz" işaretleyip imzalar.
    /// </summary>
    public const string CariMutabakat = "CARI_MUTABAKAT";
    /// <summary>D1-19 (18.10) — KVKK aydınlatma formu: işletmenin metni + pazarlama izni kutusu + imza.</summary>
    public const string KvkkAydinlatma = "KVKK_AYDINLATMA";

    public static readonly string[] Tumu = [SatisFisi, TahsilatMakbuzu, EmanetTeslim, CariEkstre, Irsaliye, Fatura, CariMutabakat, KvkkAydinlatma];

    public static bool Gecerli(string? tur) =>
        tur is not null && Array.IndexOf(Tumu, tur) >= 0;
}

/// <summary>
/// Belgenin DÜZENLEYEN tarafı — işletme kimliği (tenants satırı).
/// Boş alanlar <c>null</c> gelir; ekran UYDURMAZ (Anayasa md.1), işletme
/// kartına götüren ipucunu gösterir.
/// </summary>
public sealed record CiktiKimlik(
    string? Unvan, string? Sube, string? Adres,
    string? VergiDairesi, string? VergiNo, string? Telefon);

/// <summary>Belgenin KARŞI tarafı (müşteri / tedarikçi). Belge türüne göre null olabilir.</summary>
public sealed record CiktiTaraf(
    string Baslik, string Ad, string? VergiNo, string? Adres, string? Telefon);

/// <summary>
/// PAKET Q9-4 — tablo sütununun BİÇİM türü. Sunucu sayıyı InvariantCulture ile
/// dizer ("45000.00"); kâğıda nasıl basılacağı istemcinin işidir. QA turu 9:
/// kalem tutarı ham "45000.00", has gram "-13.923" basılıyordu çünkü istemci
/// hangi hücrenin para, hangisinin has olduğunu BİLMİYORDU. Bu kapalı liste
/// o bilgiyi sözleşmeye koyar.
/// </summary>
public static class SutunBicimi
{
    /// <summary>Düz metin — olduğu gibi basılır.</summary>
    public const string Metin = "METIN";
    /// <summary>Has gram — tr-TR, virgül, 4 hane, "has" birimi (Bicim.Has).</summary>
    public const string Has = "HAS";
    /// <summary>Para — satırın para birimiyle tr-TR (Bicim.Para; TRY → ₺45.000,00).</summary>
    public const string Para = "PARA";
    /// <summary>M5 — ürünün tartı gramı (Bicim.Gram: tr-TR, 3 hane, "g"); has'tan AYRI büyüklük.</summary>
    public const string Gram = "GRAM";
}

/// <summary>
/// Ortak tablo satırı — sütun sayısı bölümün başlık dizisiyle eşleşir.
/// <para>
/// Q9-4: <paramref name="ParaBirimi"/> PARA sütunlarının birimidir (UX-S32:
/// para birimi tutarla birlikte taşınır). Boşsa istemci ham hücreyi TL SAYMAZ;
/// Bicim.Para(null) yalnız sözleşmenin TL tutarı (tl_tutari) için verilir.
/// </para>
/// </summary>
public sealed record CiktiSatir(string[] Hucreler, bool Vurgu = false, string? ParaBirimi = null);

/// <summary>
/// Belgenin bir tablo bölümü.
/// <para>
/// Q9-4: <paramref name="SutunBicimleri"/> her sütunun <see cref="SutunBicimi"/>
/// değeridir (başlık dizisiyle aynı uzunluk). null ise hücreler olduğu gibi
/// basılır (eski sözleşme — geriye uyum).
/// </para>
/// </summary>
public sealed record CiktiBolum(
    string Baslik, string[] SutunBasliklari, List<CiktiSatir> Satirlar, string? Not = null,
    string[]? SutunBicimleri = null);

/// <summary>
/// Tutar satırı. UX-S32: para birimi TUTARLA BİRLİKTE taşınır; hiçbir tutar
/// "varsayılan TL" sayılıp ₺ ile basılmaz.
/// </summary>
public sealed record CiktiTutar(string Etiket, decimal Deger, string ParaBirimi, bool Toplam = false);

/// <summary>
/// Tek A4 belgesi. Dört belge türü de BU tipe indirgenir; kâğıt düzeni
/// tek bileşende yaşar.
/// </summary>
public sealed record OrtakBelge(
    string Tur,
    string BelgeAdi,
    string BelgeNo,
    string? Dayanak,
    DateTime DuzenlemeTarihi,
    CiktiKimlik Duzenleyen,
    CiktiTaraf? Karsi,
    List<CiktiBolum> Bolumler,
    List<CiktiTutar> Tutarlar,
    string[] ImzaBasliklari,
    string? Not,
    /// <summary>Kâğıda da basılan uyarı (ör. STORNO EDİLMİŞ kayıt). Boşsa çizilmez.</summary>
    string? Uyari,
    /// <summary>
    /// PAKET Q9-4 — <see cref="BelgeNo"/> okunur bir sıra numarası DEĞİL, kaydın
    /// sistem kimliğidir (hareket/fiş/cari). Ekran bunu söyler; numara UYDURULMAZ.
    /// </summary>
    bool BelgeNoKayitKimligi = false);
