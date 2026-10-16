using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Vade;

/// <summary>Açık (kapanmamış) tek bir veresiye borcu.</summary>
/// <param name="HareketId">Borcu doğuran hareket.</param>
/// <param name="Tarih">Borcun doğduğu an (UTC).</param>
/// <param name="Vade">Vade günü — yerel takvim günü.</param>
/// <param name="AcikTutar">Bu borçtan HÂLÂ kapanmamış TL.</param>
/// <param name="GecikmeGun">Bugüne göre gecikme; 0 ya da negatif ise gecikmemiş.</param>
public readonly record struct AcikBorc(
    string HareketId, DateTime Tarih, DateOnly Vade, decimal AcikTutar, int GecikmeGun);

/// <summary>
/// PAKET YASAL-2 (TB-15): FIFO sırasındaki açık parti ve onu doğuran KAYNAK hareket.
/// </summary>
/// <param name="HareketId">Partiyi açan hareket.</param>
/// <param name="Tur">Kaynak hareketin türü (SATIS, ALIS, ACILIS, TAKAS …).</param>
/// <param name="Tarih">Kaynak hareketin anı (UTC).</param>
/// <param name="Kalan">Bu partiden hâlâ kapanmamış TL.</param>
/// <param name="FisId">Kaynak hareket bir fişe aitse fiş kimliği.</param>
public readonly record struct AcikParti(
    string HareketId, string Tur, DateTime Tarih, decimal Kalan, string? FisId);

/// <summary>
/// Cari borcunun VADESİ ile ilgili tek karar noktası: vade hesabı, kredi limiti
/// ve açık borçların vadeye dağıtımı.
///
/// ⚠ NEDEN TEK SINIF: kredi limiti bu paketten önce YALNIZ
/// <c>CariModulu.VeresiyeGir</c>'de kontrol ediliyordu. Veresiye satış
/// (<c>SatisModulu.Satis</c>), çok bacaklı fişin veresiye bacağı, takas/işçilik
/// farkının cariye yazılması ve alışta kalan borç limiti HİÇ görmüyordu —
/// yani limit, kasiyerin hangi ekranı kullandığına bağlı bir tavsiyeydi.
/// Limit tek kapıya alındı; her veresiye yolu buradan geçer.
/// </summary>
public static class VadeKapisi
{
    /// <summary>
    /// Borcun vade gününü hesaplar.
    /// Öncelik: işlemdeki <paramref name="vadeGun"/> → carinin varsayılanı → 0 gün.
    /// 0 gün = borç doğduğu gün muaccel (peşin beklenir) — vade YOK demek değildir;
    /// borç yine takip edilir, ertesi gün gecikmeye düşer.
    /// </summary>
    public static DateOnly VadeHesapla(Data.Cari cari, int? vadeGun, DateTime anUtc)
    {
        var gun = vadeGun ?? cari.VarsayilanVadeGun ?? 0;
        if (gun < 0)
            throw new BadRequestException("Vade gün sayısı negatif olamaz");
        if (gun > 3650)
            throw new BadRequestException("Vade gün sayısı en fazla 3650 olabilir");

        return DateOnly.FromDateTime(Zaman.UtcdenYerele(anUtc)).AddDays(gun);
    }

    /// <summary>
    /// Kredi limitini doğrular. Limit yoksa serbest.
    /// <paramref name="eklenecekTl"/> ≤ 0 ise (borç azaltan yön) kontrol edilmez —
    /// borcu kapatan bir hareket limiti aşamaz.
    ///
    /// <para>
    /// ⚠ <b>DÖVİZ BORCU DA LİMİTE SAYILIR (K27-I, paket 27).</b> Cari
    /// 500 USD borçluyken limitin yalnız TL bakiyesine bakması, limiti
    /// dövizle sınırsızca aşılabilir hâle getirirdi.
    /// </para>
    /// <para>
    /// ⚠ <b>FAIL-CLOSED:</b> döviz bakiyesi 0'dan farklıysa ve o para biriminin
    /// kuru <paramref name="kurSnapshotId"/> kaydında yoksa istek REDDEDİLİR —
    /// "kur yoksa döviz borcunu 0 say" SESSİZ ATLAMA olurdu (Anayasa md.1).
    /// </para>
    /// </summary>
    /// <param name="kurSnapshotId">
    /// Döviz borcunun TL karşılığının hesaplanacağı kur fotoğrafı.
    /// <b>Varsayılanı YOKTUR ve bu bilinçlidir</b>: her çağıran, elindeki kur
    /// kaydını vermeyi ya da (döviz bakiyesi varsa) reddedilmeyi açıkça seçer.
    /// </param>
    public static async Task KrediLimitiDogrula(
        KuyumcuDbContext db, Data.Cari cari, decimal eklenecekTl, string? kurSnapshotId)
    {
        if (cari.KrediLimitiTl is not { } limit || eklenecekTl <= 0m) return;

        var bakiye = await db.CariBakiyeler.AsNoTracking()
            .Where(b => b.CariId == cari.Id && b.TenantId == cari.TenantId)
            .FirstOrDefaultAsync();

        var mevcutTl = bakiye?.TlBakiye ?? 0m;
        var dovizTl = bakiye is null ? 0m : await DovizinTlKarsiligi(db, bakiye, cari.TenantId, kurSnapshotId);

        if (mevcutTl + dovizTl + eklenecekTl > limit)
            throw new BadRequestException(dovizTl != 0m
                ? FormattableString.Invariant(
                    $"Kredi limiti aşılıyor. Mevcut bakiye: {mevcutTl:G29} TL, Döviz borcunun TL karşılığı: {dovizTl:G29} TL, Eklenecek: {eklenecekTl:G29} TL, Limit: {limit} TL")
                : FormattableString.Invariant(
                    $"Kredi limiti aşılıyor. Mevcut bakiye: {mevcutTl:G29} TL, Eklenecek: {eklenecekTl:G29} TL, Limit: {limit} TL"));
    }

    /// <summary>
    /// Üç döviz kolonunun TL karşılığı toplamı. Hepsi sıfırsa kur kaydı
    /// ARANMAZ (dövizi olmayan carinin limiti kur yokluğundan kilitlenmesin);
    /// sıfırdan farklı bir kolon varsa kur ZORUNLUDUR.
    /// </summary>
    private static async Task<decimal> DovizinTlKarsiligi(
        KuyumcuDbContext db, Data.CariBakiye bakiye, string tenantId, string? kurSnapshotId)
    {
        var dolu = Modules.Cari.DovizBakiye.ParaBirimleri
            .Where(pb => Modules.Cari.DovizBakiye.Oku(bakiye, pb) != 0m).ToList();
        if (dolu.Count == 0) return 0m;

        if (string.IsNullOrEmpty(kurSnapshotId))
            throw new BadRequestException(
                "Carinin döviz bakiyesi var; kredi limiti kur kaydı olmadan hesaplanamaz");

        var kur = await db.KurSnapshots.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == kurSnapshotId && s.TenantId == tenantId)
            ?? throw new BadRequestException(
                "Carinin döviz bakiyesi var; kredi limiti için kur kaydı bulunamadı");

        var toplam = 0m;
        foreach (var pb in dolu)
        {
            // Kuru yoksa KurZorunlu 400 fırlatır — sessiz atlama yok (K27-I).
            var oran = Modules.Cari.DovizBakiye.KurZorunlu(kur, pb);
            toplam += Modules.Cari.DovizBakiye.TlKarsiligi(Modules.Cari.DovizBakiye.Oku(bakiye, pb), oran);
        }
        return toplam;
    }

    /// <summary>
    /// Bir veresiye borcu için vadeyi hesaplar ve — ALACAK yönünde ise —
    /// kredi limitini doğrular. Veresiye yazan her yol bu tek çağrıyı kullanır.
    /// </summary>
    /// <param name="cariId">Boş/yoksa tezgâh müşterisi — vade bugündür, limit yoktur.</param>
    /// <param name="borcTutari">Cariye yazılacak TL (işaretsiz). 0 ise limit bakılmaz.</param>
    /// <param name="alacakYonu">
    /// <c>true</c> = müşteri BİZE borçlanıyor (satış, veresiye) → kredi limiti işler.
    /// <c>false</c> = BİZ tedarikçiye borçlanıyoruz (alış, hurda) → limit işlemez.
    /// ⚠ Tedarikçi carisindeki <c>KrediLimitiTl</c>'nin "bize tanınan limit" mi
    /// "ona tanıdığımız limit" mi olduğu veride ayrışmıyor; varsayım yapılmaz,
    /// yalnız alacak yönünde uygulanır.
    /// </param>
    /// <param name="kurSnapshotId">
    /// Kredi limitine döviz borcunun da katılabilmesi için kur fotoğrafı
    /// (K27-I). Varsayılanı YOKTUR: her veresiye yolu elindeki kuru verir.
    /// </param>
    public static async Task<DateOnly> Hazirla(
        KuyumcuDbContext db, string tenantId, string? cariId,
        int? vadeGun, decimal borcTutari, bool alacakYonu, DateTime anUtc,
        string? kurSnapshotId)
    {
        if (string.IsNullOrEmpty(cariId))
        {
            if (vadeGun is > 0)
                throw new BadRequestException("Vade tanımlamak için müşteri (cari) seçilmelidir");
            return DateOnly.FromDateTime(Zaman.UtcdenYerele(anUtc));
        }

        var cari = await db.Cariler.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cariId && c.TenantId == tenantId)
            ?? throw new NotFoundException("Cari bulunamadı");

        if (alacakYonu) await KrediLimitiDogrula(db, cari, borcTutari, kurSnapshotId);
        return VadeHesapla(cari, vadeGun, anUtc);
    }

    /// <summary>
    /// Carinin AÇIK borçlarını vadeleriyle döndürür — deftere ek bir tablo
    /// tutmadan, append-only kayıtlardan TÜRETİLEREK.
    ///
    /// Yöntem: defter ESKİDEN YENİYE yürünür. Borç doğuran her satır bir PARTİ
    /// açar (tutarı + vadesi); borç azaltan her satır açık partileri EN ESKİDEN
    /// başlayarak tüketir (FIFO). Sonunda tüketilmemiş partiler açık borçtur.
    ///
    /// ⚠ NEDEN BAKİYEYİ PARTİLERE DAĞITMIYORUZ: ilk tasarım "bakiye otoritedir,
    /// vadeli satırlara yeniden eskiye dağıt" diyordu. Canlı doğrulamada
    /// (2026-09-15) kusuru çıktı: kapanmış bir borç satırı aday havuzunda
    /// sonsuza dek kalıyordu, dolayısıyla SONRADAN doğan vadesiz bir bakiye
    /// artışı, çoktan ödenmiş ESKİ bir borca yazılıyordu. Müşteri, hiç
    /// gecikmediği hâlde "12 gün gecikmiş" görünüyordu. Defteri yürümek bu
    /// kapanmayı hatırlar; bakiyeyi dağıtmak hatırlamaz.
    ///
    /// ⚠ Ödemeleri tek tek borçlara bağlayan bir MAHSUP TABLOSU yine yok ve
    /// bilinçli: öyle bir tablo, ona satır yazmayan her ödeme yolunda bakiyeden
    /// sapardı. Tek doğruluk kaynağı defterdir.
    ///
    /// Bakiyenin İŞARETİ yönü belirler: pozitif bakiye müşterinin bize borcu,
    /// negatif bakiye bizim tedarikçiye borcumuzdur. İki yön aynı listede
    /// karışsaydı, hem müşteri hem tedarikçi olan carinin alacağı borç görünürdü.
    ///
    /// Defterden türeyen açık toplam ile <c>cari_bakiyeler</c> tutmazsa (vade
    /// alanı eklenmeden önce yazılmış satırlar, elle düzeltmeler) FARK YUTULMAZ:
    /// ikinci dönüş değeri olarak "vadesi bilinmeyen bakiye" diye dışarı verilir.
    /// </summary>
    public static async Task<(List<AcikBorc> Borclar, decimal VadesizBakiye)> AcikBorclar(
        KuyumcuDbContext db, string tenantId, string cariId, DateTime anUtc)
    {
        var bakiye = await db.CariBakiyeler.AsNoTracking()
            .Where(b => b.CariId == cariId && b.TenantId == tenantId)
            .Select(b => (decimal?)b.TlBakiye)
            .FirstOrDefaultAsync() ?? 0m;

        if (bakiye == 0m) return ([], 0m);

        var alacakYonu = bakiye > 0m;
        var mutlakBakiye = Math.Abs(bakiye);

        // PAKET YASAL-2 (TB-15): defter yürüyüşü TEK yardımcıdadır — taksit kapısı
        // (AcikPartiler) AYNI FIFO'yu okur; ikinci bir yürüyüş yazılmaz.
        var partiler = await DefteriYuru(db, tenantId, cariId, alacakYonu);

        var bugun = DateOnly.FromDateTime(Zaman.UtcdenYerele(anUtc));
        var borclar = new List<AcikBorc>();
        decimal vadesiz = 0m, acikToplam = 0m;

        foreach (var p in partiler)
        {
            if (p.Kalan <= 0m) continue;
            acikToplam += p.Kalan;
            if (p.Vade is { } v)
                borclar.Add(new AcikBorc(p.Id, p.Tarih, v, p.Kalan, bugun.DayNumber - v.DayNumber));
            else
                vadesiz += p.Kalan;   // borç var, vadesi yok — bugüne YAZILMAZ
        }

        // Defterden türeyen toplam ile kayıtlı bakiye tutmalıdır. Tutmuyorsa
        // fark vadesi bilinmeyen borçtur; SESSİZ GEÇİLMEZ.
        var fark = mutlakBakiye - acikToplam;
        if (fark > 0m) vadesiz += fark;
        else if (fark < 0m)
        {
            // Bakiye, açık partilerin altında: fazlalık en YENİ partiden kısılır —
            // eski borcun kapandığı bilgisi, yeni borcunkinden daha güvenilirdir.
            var kisilacak = -fark;
            for (var i = borclar.Count - 1; i >= 0 && kisilacak > 0m; i--)
            {
                var kis = Math.Min(borclar[i].AcikTutar, kisilacak);
                borclar[i] = borclar[i] with { AcikTutar = borclar[i].AcikTutar - kis };
                kisilacak -= kis;
            }
            borclar.RemoveAll(b => b.AcikTutar <= 0m);
            vadesiz = Math.Max(0m, vadesiz - kisilacak);
        }

        return (borclar, vadesiz);
    }

    // ═══ PAKET YASAL-2 (TB-15) — açık partiler + KAYNAK işlem ══════════════════

    /// <summary>Defter yürüyüşünün bir açık partisi (FIFO sırasında).</summary>
    private readonly record struct YuruyenParti(
        string Id, string Tur, DateTime Tarih, DateOnly? Vade, decimal Kalan, string? FisId);

    /// <summary>
    /// Defteri ESKİDEN YENİYE yürür (FIFO) ve açık partileri döndürür — AcikBorclar
    /// ile taksit kapısının TEK kaynağı. Davranış paket öncesi AcikBorclar gövdesinin
    /// birebir aynısıdır (yalnız partiye tür ve fiş kimliği eklendi).
    /// </summary>
    /// <param name="duzeltmeDahil">
    /// true: ACILIS / CARI_DUZELTME satırları da (TL etkisiyle) parti açar/tüketir —
    /// taksit kapısı açılış bakiyesini KAYNAKSIZ parti olarak FIFO'da doğru yerde görür.
    /// AcikBorclar false geçer (vade davranışı paket öncesiyle AYNI).
    /// </param>
    /// <param name="tuketimler">Verilirse her azaltan satırın hangi partiden ne kadar tükettiği yazılır.</param>
    private static async Task<List<YuruyenParti>> DefteriYuru(
        KuyumcuDbContext db, string tenantId, string cariId, bool alacakYonu,
        bool duzeltmeDahil = false, Dictionary<string, List<(string PartiId, decimal Tutar)>>? tuketimler = null)
    {
        var hareketler = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.CariId == cariId)
            .OrderBy(h => h.Tarih).ThenBy(h => h.Id)
            .Select(h => new { h.Id, h.Tur, h.Tarih, h.TlTutari, h.VeresiyeTutar, h.VadeTarihi, h.FisId })
            .ToListAsync();

        var partiler = new List<YuruyenParti>();

        foreach (var h in hareketler)
        {
            // Bu satırın cari bakiyesine ETKİSİ — alacak yönüne çevrilmiş.
            // TAHSILAT'ın veresiye payı yoktur (havale/kartla ödenir); bakiyeyi
            // TL bacağının tamamı kadar azaltır. Diğer satırlarda bakiyeyi
            // değiştiren şey yalnız VERESİYE payıdır — peşin satış cariye
            // hiçbir şey yazmaz.
            var pay = h.VeresiyeTutar ?? 0m;
            var etki = h.Tur == "TAHSILAT" || (duzeltmeDahil && h.Tur is "ACILIS" or "CARI_DUZELTME")
                ? h.TlTutari
                : pay > 0m ? Math.Sign(h.TlTutari) * pay : 0m;
            if (!alacakYonu) etki = -etki;
            if (etki == 0m) continue;

            if (etki > 0m)
            {
                partiler.Add(new YuruyenParti(h.Id, h.Tur, h.Tarih, h.VadeTarihi, etki, h.FisId));
                continue;
            }

            // Borç azaldı: en eski açık partiden başlayarak tüket.
            var dusulecek = -etki;
            for (var i = 0; i < partiler.Count && dusulecek > 0m; i++)
            {
                var p = partiler[i];
                if (p.Kalan <= 0m) continue;
                var tuket = Math.Min(p.Kalan, dusulecek);
                partiler[i] = p with { Kalan = p.Kalan - tuket };
                dusulecek -= tuket;
                if (tuketimler is not null)
                {
                    if (!tuketimler.TryGetValue(h.Id, out var l)) tuketimler[h.Id] = l = [];
                    l.Add((p.Id, tuket));
                }
            }
        }
        return partiler;
    }

    /// <summary>
    /// PAKET YASAL-2 (TB-15) — carinin AÇIK partileri FIFO sırasıyla, her birinin
    /// KAYNAK hareketiyle. <paramref name="alacakYonu"/>: true = müşterinin bize
    /// borcu (tahsilat), false = bizim borcumuz (tediye). Bakiye bu yönde değilse
    /// boş döner. <c>Kaynaksiz</c>: defterden türeyen açık toplamın bakiyeye
    /// yetmediği kısım (açılış bakiyesi, eski satırlar) — kaynağı BİLİNMEZ ve FIFO'da
    /// EN ESKİ sayılır (AcikBorclar'daki "vadesiz" farkın aynısı).
    /// </summary>
    public static async Task<(List<AcikParti> Partiler, decimal Kaynaksiz)> AcikPartiler(
        KuyumcuDbContext db, string tenantId, string cariId, bool alacakYonu)
    {
        var bakiye = await db.CariBakiyeler.AsNoTracking()
            .Where(b => b.CariId == cariId && b.TenantId == tenantId)
            .Select(b => (decimal?)b.TlBakiye)
            .FirstOrDefaultAsync() ?? 0m;
        if (bakiye == 0m || (bakiye > 0m) != alacakYonu) return ([], 0m);

        var yuruyen = (await DefteriYuru(db, tenantId, cariId, alacakYonu, duzeltmeDahil: true))
            .Where(p => p.Kalan > 0m).ToList();
        var acik = yuruyen.Sum(p => p.Kalan);
        var fark = Math.Abs(bakiye) - acik;

        // Bakiye açık partilerin altındaysa fazlalık en YENİ partiden kısılır
        // (AcikBorclar ile aynı kural: eski borcun kapandığı bilgisi daha güvenilir).
        if (fark < 0m)
        {
            var kisilacak = -fark;
            for (var i = yuruyen.Count - 1; i >= 0 && kisilacak > 0m; i--)
            {
                var kis = Math.Min(yuruyen[i].Kalan, kisilacak);
                yuruyen[i] = yuruyen[i] with { Kalan = yuruyen[i].Kalan - kis };
                kisilacak -= kis;
            }
            yuruyen.RemoveAll(p => p.Kalan <= 0m);
        }

        return ([.. yuruyen.Select(p => new AcikParti(p.Id, p.Tur, p.Tarih, p.Kalan, p.FisId))],
                Math.Max(0m, fark));
    }

    /// <summary>
    /// PAKET YASAL-2 (TB-16) — geçmişi YENİDEN OYNATIR: her azaltan satırın (tahsilat)
    /// o anda FIFO ile hangi partilerden ne kadar tükettiğini ve partilerin kaynak
    /// bilgisini döndürür. TEVSIK_KURTARMA işi kapıyı kaçan nakit taksiti bununla bulur.
    /// Açılış/düzeltme satırları parti sayılır (taksit kapısıyla aynı yürüyüş).
    /// </summary>
    public static async Task<(Dictionary<string, List<(string PartiId, decimal Tutar)>> Tuketimler,
        Dictionary<string, AcikParti> Partiler)> TuketimleriOynat(
        KuyumcuDbContext db, string tenantId, string cariId, bool alacakYonu)
    {
        var tuketimler = new Dictionary<string, List<(string PartiId, decimal Tutar)>>();
        var partiler = await DefteriYuru(db, tenantId, cariId, alacakYonu, duzeltmeDahil: true, tuketimler);
        return (tuketimler, partiler.ToDictionary(p => p.Id, p => new AcikParti(p.Id, p.Tur, p.Tarih, p.Kalan, p.FisId)));
    }
}
