using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Telemetri;

// ═════════════════════════════════════════════════════════════════════════════
// TELEMETRİ VARLIKLARI — PAKET P-28 (F-4 kiracı etiketli telemetri · R-2).
//
// NEDEN VAR: F-8 birim ekonomi belgesinin İKİ satırı bugün "OLCULMEDI"dir
// (kiracı başı kaynak tüketimi · kiracı başı destek dakikası). O iki satır
// ölçülmeden hiçbir kanalda rakamlı fiyat ilan edilemez (FAIL-CLOSED 1).
// Bu tablolar o iki satırın ÖLÇÜM ALTYAPISIDIR; rakamı kendileri üretir,
// belgeye elle sayı yazılmaz.
//
// ⚠ MEHMET KOŞULU — TELEMETRİ KİŞİSEL VERİ TAŞIMAZ (veto konusu):
//   Toplanan alanlar YALNIZ sayaçtır: kaç istek, kaç hata, kaç milisaniye,
//   kaç bayt. Yol (path), sorgu dizesi, başlık, gövde, IP, kullanıcı adı,
//   kullanıcı kimliği, cari/ürün bilgisi HİÇBİR alanda TUTULMAZ ve
//   tutulamaz — `dotnet/denetim.sh` `TELEMETRIDE_KISISEL_VERI` sayacı hem
//   kolon adlarını hem toplama yolundaki istek erişimlerini ölçer.
//   Kiracı kimliği ETİKETTİR (hangi kiracının kaynağı), içerik değildir.
//
// ⚠ FK YOK (bilerek): depoda `HasOne`/`WithMany` sayısı 0'dır; FK borcu TEK
//   paketle (P-34) toplu kapatılır. P-27 ile aynı gerekçe, yeni desen yok.
//
// ⚠ FİYAT YOK (K-16 · MÜHÜR 2): burada saatlik maliyet / TL tutar kolonu
//   AÇILMADI. Bu tablolar MİKTAR ölçer (dakika, istek, bayt); TL'ye çevirme
//   kurucunun rakam kalemidir ve F-8 belgesinde yapılır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Kiracı etiketli SAATLİK kaynak sayacı. Bir kiracı + bir UTC saati = bir satır.
/// <para>
/// Neden saatlik toplam, neden istek başına satır değil: istek başına kayıt
/// (a) telemetriyi bir erişim günlüğüne çevirir ve kişisel veri riskini geri
/// getirir, (b) kendi ölçtüğü sistemi yükler. Saatlik toplamda hangi isteğin
/// ne yaptığı SORULAMAZ — kapasite paydası için de sorulması gerekmez.
/// </para>
/// </summary>
[Table("telemetri_olcumleri")]
public class TelemetriOlcumu : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary>Ölçüm penceresinin başı (UTC, saate yuvarlanmış).</summary>
    [Column("olcum_saati")] public DateTime OlcumSaati { get; set; }

    /// <summary>Pencerede kiracı adına tamamlanan istek sayısı.</summary>
    [Column("istek_sayisi")] public int IstekSayisi { get; set; }

    /// <summary>Bunların kaçı 5xx ile bitti (kapasite yorumunda gürültü ayıklar).</summary>
    [Column("hata_sayisi")] public int HataSayisi { get; set; }

    /// <summary>Toplam sunucu işlem süresi (ms) — CPU payının ölçülen vekilidir.</summary>
    [Column("islem_ms")] public long IslemMs { get; set; }

    /// <summary>Toplam yanıt gövdesi (bayt) — bant genişliği payı.</summary>
    [Column("yanit_bayt")] public long YanitBayt { get; set; }

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

/// <summary>
/// Kiracıya harcanan DESTEK DAKİKASI. F-8 "destek" satırının tek meşru
/// kaynağıdır (plan § 2 / P-32 ile aynı kayıt).
/// <para>
/// Bu kayıt ELLE girilir (operatör) ya da tanılama kanalından gelir; otomatik
/// telemetriden FARKLIDIR ve bu yüzden düzeltilebilir olmak ZORUNDADIR:
/// yanlış girilen 90 dakika birim ekonomiyi bozar. PATCH + pasife alma +
/// yeniden aktifleştirme yolu vardır; HARD DELETE YOKTUR (ölçüm geçmişi
/// silinmez, pasife alınır).
/// </para>
/// </summary>
[Table("destek_dakikalari")]
public class DestekDakikasi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    /// <summary>Desteğin verildiği gün (UTC, gün başına yuvarlanmış).</summary>
    [Column("tarih")] public DateTime Tarih { get; set; }

    /// <summary>Harcanan dakika. 1..600 — 10 saatten uzun tek kalem kabul edilmez.</summary>
    [Column("dakika")] public int Dakika { get; set; }

    /// <summary>TELEFON · UZAKTAN · YERINDE · YAZILI — <see cref="DestekKanallari"/>.</summary>
    [Column("kanal"), MaxLength(16)] public string Kanal { get; set; } = null!;

    /// <summary>
    /// KURULUM · EGITIM · HATA · VERI · DIGER — <see cref="DestekKonulari"/>.
    /// ⚠ KOD'dur, serbest metin DEĞİLDİR: konu alanına müşteri adı/telefonu
    /// yazılmasının önü şemadan kapalıdır.
    /// </summary>
    [Column("konu"), MaxLength(24)] public string Konu { get; set; } = null!;

    /// <summary>
    /// Operatörün kısa notu (isteğe bağlı). ⚠ Kişisel veri YAZILMAMALIDIR;
    /// bu alan OTOMATİK TELEMETRİ DEĞİL, bilinçli operatör kaydıdır ve
    /// denetim izine gerekçesiyle birlikte düşer.
    /// </summary>
    [Column("aciklama"), MaxLength(300)] public string? Aciklama { get; set; }

    /// <summary>MANUEL (operatör girdi) · TANILAMA (destek kanalından geldi).</summary>
    [Column("kaynak"), MaxLength(16)] public string Kaynak { get; set; } = DestekKaynaklari.Manuel;

    /// <summary>false → ölçüme SAYILMAZ ama satır DURUR (hard delete yok).</summary>
    [Column("aktif")] public bool Aktif { get; set; } = true;

    /// <summary>Kaydı giren operatörün kimliği (sistem başlığından).</summary>
    [Column("operator_adi"), MaxLength(120)] public string OperatorAdi { get; set; } = null!;

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

/// <summary>Destek kanalı beyaz listesi — bilinmeyen değer REDDEDİLİR.</summary>
public static class DestekKanallari
{
    public const string Telefon = "TELEFON";
    public const string Uzaktan = "UZAKTAN";
    public const string Yerinde = "YERINDE";
    public const string Yazili = "YAZILI";

    public static readonly string[] Hepsi = [Telefon, Uzaktan, Yerinde, Yazili];
    public static bool Gecerli(string? k) => k is { Length: > 0 } && Array.IndexOf(Hepsi, k) >= 0;
}

/// <summary>Destek konusu beyaz listesi — serbest metin yerine KOD.</summary>
public static class DestekKonulari
{
    public const string Kurulum = "KURULUM";
    public const string Egitim = "EGITIM";
    public const string Hata = "HATA";
    public const string Veri = "VERI";
    public const string Diger = "DIGER";

    public static readonly string[] Hepsi = [Kurulum, Egitim, Hata, Veri, Diger];
    public static bool Gecerli(string? k) => k is { Length: > 0 } && Array.IndexOf(Hepsi, k) >= 0;
}

/// <summary>Destek kaydının doğduğu yol.</summary>
public static class DestekKaynaklari
{
    public const string Manuel = "MANUEL";
    public const string Tanilama = "TANILAMA";

    public static readonly string[] Hepsi = [Manuel, Tanilama];
    public static bool Gecerli(string? k) => k is { Length: > 0 } && Array.IndexOf(Hepsi, k) >= 0;
}
