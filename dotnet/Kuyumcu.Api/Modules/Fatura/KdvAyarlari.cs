namespace Kuyumcu.Api.Modules.Fatura;

// ═════════════════════════════════════════════════════════════════════════════
// KDV AYARLARI — appsettings "Kdv" bölümü (ADR-0005: mevzuat rakamı koda gömülmez).
//
// İKİ ÇAĞRI NOKTASI TEK KAYNAKTAN OKUR: FaturaServisi ve BankaServisi. Biri
// taşınıp diğeri kodda bırakılsaydı ADR-0005 ihlali yarım kapanırdı (Bertan).
//
// ⚠ DAVRANIŞ DEĞİŞMEZ: VarsayilanOran 0.20'dir — bu paket rakamı taşır,
//   değiştirmez.
//
// ── P-26 (Y-40) — ÖZEL MATRAH KODU VARSAYILAN OLARAK **KAPALIDIR** ──────────
// GEÇMİŞ: P-26'nın ilk uygulaması varsayılanı "805" yapmıştı (ekip kararı
// K-10, 21.09.2026). HASIM DOĞRULAYICI BUNU KRİTİK BULGU OLARAK GERİ ÇEVİRDİ
// ve haklıdır: PLAN-TEK-SEFER-2026-09-18.md:578 (P-26 DoD) "motor ve kolon
// HAZIR; K-10 TEYİDİ GELİRSE tek satır yapılandırmayla AÇILIR… teyit gelmezse
// ürün içinde 'HARİÇ' beyanı yazılır" der; aynı belgenin 778. satırı K-10'u
// KURUCU/MALİ MÜŞAVİR kalemi sayar ve "P-26'daki Y-40 anahtarı AÇILMAZ" der.
// Şirket kuralı gereği imza/mevzuat teyidi gerektiren kalem EKİP OYUYLA
// İKAME EDİLEMEZ. Bu yüzden anahtar KAPALI DOĞAR (paket 38 / Mehmet K38-1
// mührüne dönüş). Plan üstündür — çelişki uygulama raporunda yazılıdır.
//
// AÇMA YOLU (teyit geldiğinde): appsettings "Kdv:OzelMatrahKodu" alanına
// <see cref="OneriKod"/> değeri (805) yazılır — TEK SATIR, derleme gerekmez.
// Bu açılış ayrı bir kapı kaydına yazılır.
//
// ⚠ DÜRÜST SINIR (Anayasa md.1 — üründe ve raporda AYNEN yazılır):
//   805 ÖNERİSİ BİR MALİ MÜŞAVİR İMZASI DEĞİLDİR. Dayanak: KDVK md.23/e
//   yorumları + GİB e-belge özel matrah kod listesi (801-812). Yorum ekibe
//   aittir; ilk gerçek faturadan ÖNCE mali müşavire onaylatılması ŞARTTIR.
//
// ⚠ KOD HİÇBİR HESABA GİRMEZ: matrah formülü OzelMatrahMotoru'ndadır ve bu
//   paket onu DEĞİŞTİRMEZ. Kod yalnız e-belge ETİKETİDİR.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KdvAyarlari
{
    /// <summary>GİB e-belge özel matrah neden kodlarının geçerli aralığı.</summary>
    public const int KodAlt = 801;
    public const int KodUst = 812;

    /// <summary>
    /// K-10 teyidi geldiğinde yapılandırmaya ELLE yazılacak ÖNERİ kod
    /// (altın ziynet teslimi). <b>Varsayılan DEĞİLDİR</b>: hiçbir yerde
    /// <see cref="OzelMatrahKodu"/>'na atanmaz, yalnız ekranda/raporda
    /// "teyit gelirse bu yazılacak" diye gösterilir.
    /// </summary>
    public const string OneriKod = "805";

    /// <summary>KDV genel oranı. Bugünkü davranışın aynısı: 0.20.</summary>
    public decimal VarsayilanOran { get; init; } = 0.20m;

    /// <summary>
    /// Özel matrah neden kodu (KDVK md.23/e). <b>Varsayılan BOŞ = KAPALI</b>
    /// (K-10 mali müşavir teyidi beklenir). Boş kaldığı sürece
    /// <c>ozel_matrah_kodu</c> kolonu NULL kalır ve hiçbir belgeye basılmaz.
    /// </summary>
    public string? OzelMatrahKodu { get; init; }

    /// <summary>Boşluk yalnızca boşluktan ibaretse de "yok" sayılır.</summary>
    public string? KodYaDaNull =>
        string.IsNullOrWhiteSpace(OzelMatrahKodu) ? null : OzelMatrahKodu.Trim();

    /// <summary>Kod GİB aralığında mı (801-812)? Aralık dışı kod ekranda uyarılır.</summary>
    public bool KodAralikta =>
        KodYaDaNull is { } k && int.TryParse(k, out var n) && n >= KodAlt && n <= KodUst;

    /// <summary>
    /// Y-40 / UX-S21 · UX-S31 — <b>değerin kaynağı ekranda söylenir</b>.
    /// Kapalıyken planın istediği "HARİÇ" beyanı basılır; açıkken kodun
    /// nereden geldiği ve imzasız olduğu aynı yerde yazılır.
    /// </summary>
    /// <remarks>
    /// PAKET YASAL-4 / Y4-C: kod KAPALIYKEN bu metin EKRANA BASILMAZ (yapılandırma anahtarı
    /// ve kurucu kalemi numarası kullanıcı dili değildir); yalnız sistem ucu / günlük içindir.
    /// Ekranda kapalı durum tek satır rozettir (Sozlukler.Yasal4 FaturaKodRozet).
    /// Kod AÇIKKEN kaynak beyanı kodla birlikte gösterilir (P-26 kuralı aynen).
    /// </remarks>
    public string KaynakBeyani => KodYaDaNull is null
        ? "KDV 23/e özel matrah neden kodu bu kurulumda KAPALIDIR — faturaya kod BASILMAZ "
          + "ve muhasebe fişinde alan BOŞ bırakılır (uydurulmaz). Ürün kapsamı: özel matrah "
          + "neden kodu beyanı HARİÇ. Sebep: mali müşavir teyidi (kurucu kalemi K-10) "
          + $"beklenmektedir. Teyit gelince yapılandırmada Kdv:OzelMatrahKodu = {OneriKod} "
          + "yazılarak tek satırla açılır."
        : $"Özel matrah neden kodu {KodYaDaNull} — kaynak: KDVK md.23/e + GİB e-belge özel matrah "
          + "kod listesi (801-812). Bu bir mali müşavir imzası DEĞİLDİR; ilk gerçek faturadan "
          + "önce onaylatılması tavsiye edilir. Yorum farklıysa kod tek alandan değiştirilebilir.";
}
