using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Isler;

/// <summary>
/// Bir işin ÜRETTİĞİ bildirim taslağı. İşler veritabanına YAZMAZ; taslak
/// döndürür ve yazma tek yerde (IsCalistirici) olur — böylece tekillik,
/// kiracı damgası ve sayaçlar tek koddan geçer.
/// </summary>
public readonly record struct BildirimTaslagi(
    string Tur,
    string Onem,
    string Baslik,
    string Govde,
    string? HedefRota,
    string? IlgiliVarlikTuru,
    string? IlgiliVarlikId,
    string TekillikAnahtari,
    string? GorunurRoller);

/// <summary>
/// İşin koştuğu bağlam. <see cref="Kapsam"/> kiracıya SABİTLENMİŞ DI kapsamıdır
/// (SistemKapsami.Kiraci) — iş kendi ihtiyacı olan scoped servisi oradan çözer.
///
/// ⚠ SAPMA (spesifikasyondan): arayüz imzasında yalnız <c>KuyumcuDbContext</c>
/// vardı. SAKLAMA_ONIZLEME işi <c>SaklamaSuresiIsi</c> (scoped, Program.cs:47)
/// servisini çağırmak zorunda; db'den o servis türetilemez. Alternatifi işin
/// içinde <c>new DenetimServisi(...)</c> kurmaktı — ikinci bir denetim yolu
/// doğururdu (ADR-0021 ihlali). Bu yüzden bağlam bir kayıt tipine alındı.
/// </summary>
public readonly record struct IsBaglami(
    IServiceProvider Kapsam,
    KuyumcuDbContext Db,
    string TenantId,
    DateTime AnUtc)
{
    /// <summary>İşletme (Europe/Istanbul) günü — anahtarların tek kaynağı.</summary>
    public DateOnly YerelGun => DateOnly.FromDateTime(Zaman.UtcdenYerele(AnUtc));
}

/// <summary>
/// Zamanlanmış iş sözleşmesi. Uygulamalar DURUMSUZDUR (singleton kaydedilir)
/// ve deftere YAZMAZ (denetim.sh: Isler/ altında HareketFabrikasi = 0).
/// </summary>
public interface IZamanlanmisIs
{
    /// <summary>Kararlı iş kodu — is_calismalari.is_kodu ve bildirim.tur budur.</summary>
    string Kod { get; }

    /// <summary>Ekranda görünen ad (sözlük anahtarı değil; ekran sözlükten çevirir).</summary>
    string Ad { get; }

    /// <summary>Yapılandırmadan okunan etkinlik. KAPALI iş hiç koşmaz, çalışma satırı bile açılmaz.</summary>
    bool Etkin(IConfiguration yapilandirma);

    /// <summary>Günlük koşum anahtarı — YEREL gün (UTC günü DEĞİL, K5 tuzağı).</summary>
    string CalismaAnahtari(DateOnly yerelGun);

    /// <summary>İşi koşturur ve bildirim taslakları döndürür. Yazma YAPMAZ.</summary>
    Task<IsCikti> Calistir(IsBaglami baglam, CancellationToken iptal);
}

/// <summary>İşin çıktısı: taslaklar + kaç kayıt incelendiği (dürüst sayaç).</summary>
public readonly record struct IsCikti(IReadOnlyList<BildirimTaslagi> Taslaklar, int IncelenenKayit)
{
    public static IsCikti Bos => new([], 0);
}

/// <summary>Anahtar üretimi — tek kaynak, saf ve testlenebilir.</summary>
public static class IsAnahtarlari
{
    /// <summary>Günlük çalışma anahtarı: yerel gün. Saat İÇERMEZ.</summary>
    public static string Gunluk(DateOnly yerelGun) =>
        yerelGun.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// Elle koşum anahtarı: günlük anahtara "-Ehhmm" eklenir. Günlük anahtarla
    /// ÇAKIŞMAZ (yeni çalışma satırı açılır), ama bildirim tekillik anahtarı
    /// aynı kaldığı için YENİ BİLDİRİM ÜRETİLMEZ.
    /// </summary>
    public static string Elle(DateTime anUtc)
    {
        var yerel = Zaman.UtcdenYerele(anUtc);
        return FormattableString.Invariant(
            $"{Gunluk(DateOnly.FromDateTime(yerel))}-E{yerel:HHmm}");
    }

    /// <summary>Bildirim tekillik anahtarı: önek + kimlik + YEREL GÜN. Saat İÇERMEZ.</summary>
    public static string Tekillik(string onek, string kimlik, DateOnly yerelGun) =>
        $"{onek}:{kimlik}:{Gunluk(yerelGun)}";
}
