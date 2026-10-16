using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kuyumcu.Api.Common;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-21 — TİCARİ ÇEVRE HATLARI (Y-17 · Y-18 · Y-19 · Y-20 · Y-21 · Y-23)
//
// Bu dosya altı hattın ORTAK DEĞİŞMEZLERİNİ taşır. Hepsi `hareketler`
// defterine dokunduğu için tek pakette yazıldı (plan § 2, P-21 risk notu:
// "bölünürse aynı dosyaya iki el değer").
//
// ⚠ Entities.cs'e YALNIZ üç şey eklendi (Stok.BlokeAdet, Hareket.SiparisId,
//   beş yeni hareket türü); geri kalan her şey burada durur — Siparis.cs /
//   KiymetliEvrak.cs / GunKapanisi.cs deseniyle aynı (çakışma azaltma).
//
// ⚠ AYNA DEĞİL DEFTER: avans ve konsinye için AYRI BİR TABLO AÇILMADI ve bu
//   bilinçlidir. KiymetliEvrak.cs'teki K4-B gerekçesinin aynısı: iki kaynak
//   olduğu an ikisi ayrışır ve hangisinin doğru olduğu bir daha bilinemez.
//   Açık avans = Σ AVANS_ALINDI − Σ AVANS_MAHSUP − Σ AVANS_IADE (TL bacağı).
//   Açık konsinye = Σ KONSINYE_CIKIS.adet − Σ KONSINYE_IADE.adet.
//   Rezervasyon ise defterden TÜRETİLEMEZ (hiç hareket doğurmaz: mal hâlâ
//   bizimdir, para el değiştirmemiştir) — o yüzden TEK yeni tablo odur.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// REZERVASYON (Y-23) — "bu mal bu müşteriye ayrıldı".
///
/// <para>⚠ DEFTERE YAZMAZ: rezervasyon bir SÖZdür, bir işlem değil. Ne para ne
/// mülkiyet el değiştirir. Tek fiziksel etkisi <c>stoklar.bloke_adet</c>'tir.</para>
///
/// <para>SİLİNMEZ: hard DELETE ucu YOKTUR. Vazgeçilen rezervasyon
/// <see cref="RezervasyonDurumu.Iptal"/>'e geçirilir (gerekçe zorunlu).</para>
/// </summary>
[Table("rezervasyonlar")]
public class Rezervasyon : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;
    [Column("urun_id"), MaxLength(36)] public string UrunId { get; set; } = null!;

    /// <summary>Malın ayrıldığı müşteri.</summary>
    [Column("cari_id"), MaxLength(36)] public string CariId { get; set; } = null!;

    /// <summary>Rezervasyon bir siparişten doğduysa o siparişin kimliği.</summary>
    [Column("siparis_id"), MaxLength(36)] public string? SiparisId { get; set; }

    [Column("adet", TypeName = "decimal(12,3)")] public decimal Adet { get; set; }

    /// <summary><see cref="RezervasyonDurumu"/> — kapalı liste.</summary>
    [Column("durum"), MaxLength(20)] public string Durum { get; set; } = RezervasyonDurumu.Aktif;

    /// <summary>
    /// Sözün bittiği gün — yerel takvim günü (<c>KiymetliEvrak.VadeGunu</c> ile
    /// aynı K5 gerekçesi: UTC damga 00:00–03:00 arası bir gün erken düşer).
    /// Geçmişse rezervasyon "süresi doldu" sayılır ama KENDİLİĞİNDEN düşmez —
    /// bloke ancak bir kullanıcı eylemiyle kalkar (sessiz yan etki yasağı).
    /// </summary>
    [Column("bitis_gunu")] public DateOnly? BitisGunu { get; set; }

    [Column("aciklama"), MaxLength(500)] public string? Aciklama { get; set; }

    /// <summary>İptalde/kapanışta zorunlu gerekçe (en az <see cref="GerekceAsgari"/> karakter).</summary>
    [Column("kapanis_gerekcesi"), MaxLength(500)] public string? KapanisGerekcesi { get; set; }

    /// <summary>SATILDI durumuna geçiren satış hareketinin kimliği.</summary>
    [Column("satis_hareket_id"), MaxLength(36)] public string? SatisHareketId { get; set; }

    [Column("olusturan_kullanici_id"), MaxLength(36)] public string OlusturanKullaniciId { get; set; } = null!;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }

    public const int GerekceAsgari = 10;
    public const int AzamiLimit = 200;
    public const int VarsayilanLimit = 50;
}

/// <summary>Rezervasyon durumu — KAPALI liste (fail-closed), tek kaynak (UX-S31).</summary>
public static class RezervasyonDurumu
{
    public const string Aktif = "AKTIF";
    public const string Satildi = "SATILDI";
    public const string Iptal = "IPTAL";

    public static readonly string[] Tumu = [Aktif, Satildi, Iptal];

    /// <summary>
    /// TERMİNAL durumlar — bloke KALKMIŞTIR ve hiçbir çıkış yoktur.
    /// <see cref="TerminalKapisi"/> bu diziyi okur.
    /// </summary>
    public static readonly string[] Terminal = [Satildi, Iptal];
}

/// <summary>
/// PAKET P-21 — <c>stoklar.bloke_adet</c> yazmanın TEK KAPISI (sayaç: BLOKE_TUTARSIZ).
///
/// <para>⚠ NEDEN KAPI, NEDEN ÂDET DEĞİL: bloke iki ayrı yerden değişir
/// (rezervasyon açılır/kapanır, rezerve mal satılır). "Her yer kendi
/// aritmetiğini yapsın" bırakılsaydı ikisinden biri eksik/fazla düşer ve
/// <c>bloke_adet &gt; adet</c> hâli doğardı: satılabilir stok NEGATİF görünür,
/// raftaki mal satılamazdı — hata ekranda "stok yok" diye çıkar, sebebi
/// görünmezdi. Kapı bu hâli imkânsız kılar ve denetim.sh kapı dışı her
/// <c>BlokeAdet =</c> atamasını sayar.</para>
/// </summary>
public static class BlokeKapisi
{
    /// <summary>SATILABİLİR adet — satış/çıkış yolları YALNIZ buna bakar.</summary>
    public static decimal Satilabilir(Stok? stok) =>
        stok is null ? 0m : Math.Round(stok.Adet - stok.BlokeAdet, 3, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Mal ÇIKARAN her yolun kapısı: satılabilir adet talebi karşılıyor mu?
    ///
    /// <para>⚠ Mesaj üç sayıyı da söyler (stok / rezerve / satılabilir) —
    /// yalnız "yetersiz stok" deseydi tezgâhtar raftaki malı görür ve sistemin
    /// yalan söylediğini sanırdı (UX-S30).</para>
    /// </summary>
    public static void SatilabilirDogrula(Stok stok, decimal talep, string baglam)
    {
        var satilabilir = Satilabilir(stok);
        if (satilabilir >= talep) return;
        throw new BadRequestException(
            $"{baglam}: yetersiz satılabilir stok. Stok {stok.Adet} adet, " +
            $"rezerve {stok.BlokeAdet} adet, satılabilir {satilabilir} adet; talep {talep} adet.");
    }

    /// <summary>
    /// Bloke adedi <paramref name="delta"/> kadar değiştirir ve değişmezi zorlar.
    /// </summary>
    /// <exception cref="BadRequestException">
    /// Sonuç <c>0 ≤ bloke ≤ adet</c> aralığının dışına çıkarsa.
    /// </exception>
    public static void Degistir(Stok stok, decimal delta, DateTime now)
    {
        var yeni = Math.Round(stok.BlokeAdet + delta, 3, MidpointRounding.AwayFromZero);

        if (yeni < 0m)
            throw new BadRequestException(
                $"Bloke adet sıfırın altına düşemez: mevcut {stok.BlokeAdet}, değişim {delta}.");

        if (yeni > stok.Adet)
            throw new BadRequestException(
                $"Bloke adet stok adedini aşamaz: stok {stok.Adet}, istenen bloke {yeni}. " +
                $"Satılabilir: {Satilabilir(stok)} adet.");

        stok.BlokeAdet  = yeni;
        stok.Guncellendi = now;
    }

    /// <summary>
    /// Stok ADEDİ azalırken blokenin de düşmesi gereken hâl (rezerve mal satıldı).
    /// Adet ve bloke TEK çağrıda düşer; ikisi ayrı yazılsaydı arada
    /// <c>bloke &gt; adet</c> penceresi doğardı.
    /// </summary>
    public static void RezerveSatildi(Stok stok, decimal adet, DateTime now)
    {
        if (adet <= 0m)
            throw new BadRequestException("Rezerve satış adedi sıfırdan büyük olmalıdır.");
        if (stok.BlokeAdet < adet)
            throw new BadRequestException(
                $"Rezerve adet yetersiz: bloke {stok.BlokeAdet}, istenen {adet}.");

        stok.BlokeAdet  = Math.Round(stok.BlokeAdet - adet, 3, MidpointRounding.AwayFromZero);
        stok.Guncellendi = now;
    }
}

/// <summary>
/// PAKET P-21 — TERMİNAL DURUM KAPISI (sayaç: TERMINAL_SONRASI_YAZAN_YOL).
///
/// <para>⚠ NEDEN TEK KAPI: bu pakette dört ayrı durum makinesi deftere YAZAN
/// yollar kazandı (sipariş → satış, rezervasyon → satış, avans → mahsup, evrak
/// → ciro/rücu). Her yol kendi <c>if (durum == ...)</c> kontrolünü yazsaydı
/// biri unutulurdu ve TESLİM EDİLMİŞ bir sipariş ikinci kez satışa çevrilir,
/// İPTAL bir rezervasyon ikinci kez blokeyi düşürürdü. İki kayıt da sessizce
/// doğar; kimse fark etmez. Kapı bunu 409'a çevirir.</para>
///
/// <para>⚠ 409 (Conflict), 400 DEĞİL: istek kendi içinde geçerlidir; kaydın
/// GÜNCEL HÂLİ ile çelişir. Ekran "bu kayıt artık kapalı" diyebilsin diye
/// ayrıdır (UX-S30).</para>
/// </summary>
public static class TerminalKapisi
{
    /// <summary>
    /// <paramref name="durum"/> terminal ise 409 fırlatır.
    /// </summary>
    /// <param name="terminaller">O durum makinesinin terminal durum dizisi.</param>
    /// <param name="kayitAdi">Kullanıcıya gösterilecek kayıt adı ("Sipariş", "Rezervasyon"...).</param>
    /// <param name="eylem">Yapılmak istenen eylem ("satışa çevirme", "iptal"...).</param>
    public static void Dogrula(string durum, IReadOnlyList<string> terminaller, string kayitAdi, string eylem)
    {
        for (var i = 0; i < terminaller.Count; i++)
        {
            if (!string.Equals(durum, terminaller[i], StringComparison.Ordinal)) continue;
            throw new ConflictException(
                $"{kayitAdi} '{durum}' durumunda kapanmıştır; {eylem} yapılamaz. " +
                "Kapanmış kaydın doğru düzeltme yolu ters hareket ya da storno'dur.");
        }
    }
}

/// <summary>
/// PAKET P-21 (Y-17) — AVANS DEĞİŞMEZLERİ.
///
/// <para>⚠ ÇİFTE SAYMA KAPISI (plan DoD): avans alındığında cari bakiye
/// <b>azalır</b> (müşteri alacaklı olur). Satış anında mahsup edilirken bakiye
/// İKİNCİ KEZ azaltılsaydı aynı para iki kez alacak yazılırdı ve müşteri
/// borcunu ödemiş göründüğü hâlde ödememiş olurdu. Bu yüzden
/// <c>AVANS_MAHSUP</c> satırı cari bakiyeye <b>DOKUNMAZ</b>: yalnız avans
/// havuzunu eritir (<see cref="MahsupEdilebilir"/>).</para>
/// </summary>
public static class AvansKapisi
{
    /// <summary>Avansın TL bacağını + yazan tür.</summary>
    public const string Alindi = "AVANS_ALINDI";

    /// <summary>Havuzu eriten, cari bakiyeye DOKUNMAYAN tür.</summary>
    public const string Mahsup = "AVANS_MAHSUP";

    /// <summary>Havuzu eriten, cari bakiyeyi geri ARTIRAN tür (para iadesi).</summary>
    public const string Iade = "AVANS_IADE";

    /// <summary>Üç tür — rapor/süzgeç tek kaynaktan okur (UX-S31).</summary>
    public static readonly string[] Tumu = [Alindi, Mahsup, Iade];

    /// <summary>
    /// Bir satışa uygulanabilecek avans tutarı: havuz ile talep edilenin küçüğü —
    /// ama talep havuzu AŞIYORSA sessizce kırpılmaz, REDDEDİLİR (UX-S28/md.1).
    /// </summary>
    public static decimal MahsupEdilebilir(decimal havuz, decimal talep)
    {
        if (talep <= 0m)
            throw new BadRequestException("Mahsup tutarı sıfırdan büyük olmalıdır.");
        if (havuz <= 0m)
            throw new BadRequestException("Bu carinin açık avansı yok; mahsup yapılamaz.");
        if (talep > havuz + 0.005m)
            throw new BadRequestException(
                $"Mahsup tutarı açık avansı aşıyor: açık avans {havuz:0.00} TL, istenen {talep:0.00} TL.");
        return Math.Round(Math.Min(talep, havuz), 2, MidpointRounding.AwayFromZero);
    }
}

/// <summary>
/// PAKET P-21 (Y-20) — KONSİNYE ÇIKIŞ / İADE DEĞİŞMEZLERİ.
///
/// <para>⚠ <c>Urun.Konsinye</c> İLE KARIŞTIRILMAZ ve bu ayrım yazılıdır:
/// <c>Urun.Konsinye</c> = <b>bize gelen</b> emanet mal (bizim değil, satılınca
/// sahibine borçlanırız). Buradaki <c>KONSINYE_CIKIS</c> = <b>bizden giden</b>
/// mal (bizimdir, karşı taraf satarsa bize borçlanır). Aynı ada iki anlam
/// yüklenmesin diye ekranda "Konsinye çıkış (bizden giden)" yazar (UX-S31).</para>
/// </summary>
public static class KonsinyeKapisi21
{
    public const string Cikis = "KONSINYE_CIKIS";
    public const string Iade  = "KONSINYE_IADE";

    public static readonly string[] Tumu = [Cikis, Iade];

    /// <summary>
    /// İade edilebilecek azami adet: çıkan − daha önce iade edilen.
    /// Fazlası REDDEDİLİR — aksi hâlde hiç göndermediğimiz mal stoğa girer.
    /// </summary>
    public static decimal IadeEdilebilir(decimal cikan, decimal iadeEdilen, decimal talep)
    {
        var acik = Math.Round(cikan - iadeEdilen, 3, MidpointRounding.AwayFromZero);
        if (talep <= 0m)
            throw new BadRequestException("İade adedi sıfırdan büyük olmalıdır.");
        if (acik <= 0m)
            throw new BadRequestException("Bu konsinye çıkışında açık adet kalmamıştır.");
        if (talep > acik + 0.0005m)
            throw new BadRequestException(
                $"İade adedi açık konsinye adedini aşıyor: açık {acik}, istenen {talep}.");
        return talep;
    }
}
