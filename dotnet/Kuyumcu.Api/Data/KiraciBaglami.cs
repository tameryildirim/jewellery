using Microsoft.AspNetCore.Http;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// Kiracı bağlamı — ADIM1-KARAR-2026-09-11.md §3/Ç1, §4.
//
// DbContext'in HANGİ kiracı adına çalıştığını taşır. Üç hâli vardır ve üçü de
// AÇIKTIR: ambient/AsyncLocal kullanılmaz, çünkü o yol Task sınırlarında sessizce
// kaybolur ya da önceki isteğin değeriyle sızar — kapatmaya çalıştığımız açığın
// aynısını üretirdi.
//
// Bağımlılık DbContext ctor'unda zorunludur: bağlamsız DbContext İNŞA EDİLEMEZ.
// Böylece elle `new KuyumcuDbContext(ayar)` yapan her yer DERLENMEZ ve derleyici
// bize tüm çağrı noktalarını gösterir.
//
// ⚠ KURAL: bağlamsız ikinci bir ctor overload'ı EKLENMEZ. Eklenirse kapı sessizce
// yeniden açılır. CI grep'i bunu denetler.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>DbContext'in hangi kiracı adına çalıştığı.</summary>
public interface IKiraciBaglami
{
    /// <summary>Aktif kiracı; sistem modunda null.</summary>
    string? KiraciId { get; }

    /// <summary>true → sorgu filtresi HİÇ kurulmaz. Yalnız <see cref="SistemKapsami"/> kullanır.</summary>
    bool SistemModu { get; }

    // ── PAKET P-04 — ŞUBE BAĞLAMI (kimlik Y-1) ───────────────────────────────
    //
    // ŞUBE NULL SÖZLEŞMESİ (yazılı, testle kilitli — fail-open YASAK):
    //
    //   1. <see cref="TumSubeler"/> = true  → şube süzgeci KURULMAZ. Kayıtların
    //      tamamı kiracı kapsamında görünür. Bu bir AYRICALIKTIR ve yalnız
    //      <see cref="SubeYetkisi.TumSubelerRolleri"/> listesindeki rollere
    //      verilir; rapor uçlarının parametreli şube süzgeci bu sayede çalışır.
    //
    //   2. TumSubeler = false ve <see cref="SubeId"/> DOLU → süzgeç kurulur;
    //      kiracı ve şube TEK ifadede AND ile birleşir.
    //
    //   3. TumSubeler = false ve SubeId BOŞ → **FAIL-CLOSED**: DbContext inşa
    //      edilmez, istek 500 ile düşer. "Şube yok → hepsini göster" (fail-open)
    //      ve "şube yok → sessizce 0 satır" (fail-blind) yollarının İKİSİ de
    //      kapalıdır; ikincisi yanlış veriyi doğru gösterirdi (Anayasa md.1).
    //
    // Bu üç madde <c>SubeIzolasyonTestleri</c> içinde adıyla sınanır.

    /// <summary>Oturumun bağlı olduğu şube; <see cref="TumSubeler"/> true ise anlamsızdır.</summary>
    string? SubeId { get; }

    /// <summary>true → şube süzgeci kurulmaz (tüm şubeler görülür).</summary>
    bool TumSubeler { get; }
}

/// <summary>
/// "TÜM şubeler" ayrıcalığının TEK kaynağı. İki yerde tanımlanırsa biri
/// güncellenmeden kalır ve süzgeç sessizce açılır/kapanır — bu yüzden tek dizi.
/// </summary>
public static class SubeYetkisi
{
    /// <summary>Şube süzgecinden muaf roller (kiracı geneli okur).</summary>
    public static readonly string[] TumSubelerRolleri = ["SAHIP", "MUDUR", "MUHASEBE"];

    /// <summary>Rol tüm şubeleri görebilir mi? Bilinmeyen/boş rol → HAYIR (fail-closed).</summary>
    public static bool TumSubeler(string? rol) =>
        rol is { Length: > 0 } && Array.IndexOf(TumSubelerRolleri, rol) >= 0;
}

/// <summary>
/// HTTP isteği bağlamı — kiracıyı JWT claim'inden okur.
/// Claim adı <c>AktifKullanici</c> ile aynı olmak ZORUNDA; ayrışırsa filtre
/// sessizce boş kiracıya düşerdi.
/// İstek dışı bir kapsamda (arka plan işi) <see cref="KapsamBaglamAyari"/>'ye düşer.
/// </summary>
public sealed class IstekKiraciBaglami(IHttpContextAccessor erisim, KapsamBaglamAyari kapsamAyari) : IKiraciBaglami
{
    private IKiraciBaglami? Kapsam => kapsamAyari.Baglam;

    // AÇIKÇA KURULAN KAPSAM ÖNCELİKLİDİR. Sebep: SistemKapsami bir HTTP isteğinin
    // İÇİNDE de yeni kapsam açar (ör. AuthServisi.Giris). O çocuk kapsamda
    // IHttpContextAccessor hâlâ isteği görür; HttpContext'e öncelik verilseydi
    // sistem bağlamı ezilir ve giriş "kiracı çözülemedi" ile 500 verirdi.
    // (Bu tam olarak yaşandı ve canlı testte yakalandı.)
    public string? KiraciId =>
        Kapsam is { } kapsam
            ? kapsam.KiraciId
            : erisim.HttpContext?.User.FindFirst("tenantId")?.Value is { Length: > 0 } t ? t : null;

    public bool SistemModu => Kapsam?.SistemModu ?? false;

    // Şube ve rol AYNI jetondan okunur; ayrışırsa süzgeç yanlış kapsamda kurulur.
    public string? SubeId =>
        Kapsam is { } kapsam
            ? kapsam.SubeId
            : erisim.HttpContext?.User.FindFirst("subeId")?.Value is { Length: > 0 } s ? s : null;

    public bool TumSubeler =>
        Kapsam is { } kapsam
            ? kapsam.TumSubeler
            : SubeYetkisi.TumSubeler(erisim.HttpContext?.User.FindFirst("rol")?.Value);
}

/// <summary>
/// Açıkça verilen kiracı — arka plan işleri ve testler için.
/// <para>
/// ⚠ ŞUBE: varsayılan <c>tumSubeler = true</c>'dur ve bu BİLİNÇLİDİR. Arka plan
/// işinin (kur yazımı, zamanlanmış iş) bir şubesi yoktur; kiracı genelinde
/// çalışır. Şubeye kilitli bir kapsam isteniyorsa şube AÇIKÇA verilir —
/// "unutulunca kilitlenir" davranışı arka plan işlerini sessizce yarım bırakırdı.
/// Kullanıcı isteklerinde bu sınıf KULLANILMAZ; orada <see cref="IstekKiraciBaglami"/>
/// jetondan okur ve rol ayrıcalığı yoksa fail-closed davranır.
/// </para>
/// </summary>
public sealed class SabitKiraciBaglami(string kiraciId, string? subeId = null, bool tumSubeler = true)
    : IKiraciBaglami
{
    public string? KiraciId { get; } = kiraciId;
    public bool SistemModu => false;
    public string? SubeId { get; } = subeId;
    public bool TumSubeler { get; } = tumSubeler;
}

/// <summary>
/// Filtresiz erişim. Kullanımı <see cref="SistemKapsami"/>'ndaki dar listeyle
/// SINIRLIDIR: kur zamanlayıcısı ve kimlik-öncesi Auth yolları.
/// </summary>
public sealed class SistemKiraciBaglami : IKiraciBaglami
{
    public string? KiraciId => null;
    public bool SistemModu => true;
    /// <summary>Sistem modunda hiçbir süzgeç kurulmaz; şube kavramı da yoktur.</summary>
    public string? SubeId => null;
    public bool TumSubeler => true;
}

/// <summary>
/// Bir DI kapsamına (scope) yerleştirilen bağlam taşıyıcısı. HTTP isteği olmayan
/// kapsamlarda <see cref="IstekKiraciBaglami"/> bunu okur. Tek yazımlıktır —
/// bir kapsamın kiracısı sonradan değiştirilemez.
/// </summary>
public sealed class KapsamBaglamAyari
{
    public IKiraciBaglami? Baglam { get; private set; }

    public void Ayarla(IKiraciBaglami baglam)
    {
        if (Baglam is not null)
            throw new InvalidOperationException("Kapsamın kiracı bağlamı zaten ayarlı; değiştirilemez.");
        Baglam = baglam;
    }
}
