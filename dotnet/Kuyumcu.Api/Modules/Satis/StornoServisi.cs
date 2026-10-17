using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Satis;

// ═════════════════════════════════════════════════════════════════════════════
// STORNO MOTORU — paket 48 Faz A (denetim izli düzeltme).
//
// SORU: yanlış girilmiş bir işçilik/tahsilat/emanet fişini defteri BOZMADAN
// nasıl geri alırım? Bugüne kadar tek yol iadeydi ve o da yalnız SATIS/ALIS
// içindi; kalan türlerde kullanıcı ya kaydı olduğu gibi bırakıyor ya da
// "ters iade" taklidi yapıyordu (ciro ve kâr raporunu şişirir — Eray kapısı).
//
// KURALLAR (hepsi fail-closed):
//   • DEFTER YERİNDE DEĞİŞMEZ: kaynak satıra DOKUNULMAZ (ADR-0004, K-UX-2).
//     Hükümsüzlük yalnız ters satırın VARLIĞINDAN okunur.
//   • TÜR BEYAZ LİSTESİ: liste dışı her tür 409. Yeni bir tür eklendiğinde
//     varsayılan REDDETMEKTİR.
//   • ÇİFT STORNO YASAK: bir hareket en fazla bir kez stornolanır; storno
//     satırının kendisi stornolanamaz.
//   • İŞARET SÖZLEŞMESİ: TlTutari = -kaynak.TlTutari, HasGrami = -kaynak.HasGrami.
//   • GEREKÇE ≥ 20 KARAKTER, servis içinde ELLE doğrulanır (DataAnnotations
//     yalnız controller yolunda çalışır).
//   • KUR UYDURULMAZ: storno bugünün hareketidir, yeni kurSnapshotId ister.
//   • GÜN KİLİDİ BUGÜNE BAKAR (KasaDogrula an=now). Kaynağın günü kapalı olsa
//     bile storno yazılabilir — kapalı güne satır EKLENMİYOR — ama o durumda
//     yalnız SAHİP yapabilir (Mehmet koşulu: kapanış tutanağından sonra
//     defterin net toplamı sessizce değişemez).
//   • HAREKETLER.ADD TEK KAPI: ters satır da HareketFabrikasi.Ekle'den geçer.
//
// ⚠ TEVSİK/MASAK KAPILARI TERS SATIRA UYGULANMAZ — gerekçe yazılıdır:
//   storno yeni bir müşteri işlemi değil, var olan işlemin HÜKÜMSÜZ
//   KILINMASIDIR; çiftin net etkisi sıfırdır. Kapı uygulansaydı, 25.000 TL
//   nakit bir hatanın stornosu "aynı gün 50.000 TL nakit" sayılır ve hata
//   DÜZELTİLEMEZ hâle gelirdi (spesifikasyonun kendi canlı doğrulama adımı da
//   stornonun BAŞARILI olmasını bekler). Çift sayma StornoSuzgeci ile
//   önlenir: çift, sonraki işlemlerin kümülatifinden dışlanır.
//   Bu sapma rapora ve mali müşavir notuna yazıldı (Anayasa md.1).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class StornoDto
{
    /// <summary>
    /// Neden geri alınıyor? En az 20 karakter — "yanlış" demek gerekçe değildir.
    /// Denetim izine bu metnin ÖZETİ düşer.
    /// </summary>
    [Required, MinLength(20), MaxLength(1000)] public string Gerekce { get; set; } = null!;

    /// <summary>Storno ANINın kur kaydı. Kaynağın kuru KOPYALANMAZ.</summary>
    [Required] public string KurSnapshotId { get; set; } = null!;

    /// <summary>
    /// Kaynak bir kasadan geçtiyse ZORUNLU ve o kasayla aynı olmalıdır.
    /// Kasa istemciden alınıp körlemesine kullanılmaz: sunucu kaynağın kasa
    /// bacağından kendi bulur ve gönderilenle karşılaştırır (yanlış kasa → 400).
    /// </summary>
    public string? KasaId { get; set; }
}

public sealed class StornoServisi(
    KuyumcuDbContext db, DenetimServisi denetim, ILogger<StornoServisi> logger)
{
    /// <summary>
    /// Ters kayıt üretilebilen hareket türleri. LİSTE DIŞI = REDDEDİLİR.
    /// Stok ve maliyet partisi bacağı olan türler (TAKAS, HURDA_ALIS, TRANSFER)
    /// bilinçli olarak DIŞARIDADIR — bkz. <see cref="ErtelenenTurler"/>.
    /// </summary>
    public static readonly HashSet<string> StornolanabilirTurler =
        ["ISCILIK", "VERESIYE", "TAHSILAT", "EMANET_GIRIS", "EMANET_CIKIS"];

    /// <summary>
    /// Stok + maliyet partisi geri alması gereken türler. Bu pakette 409 ile
    /// DURULUR: partisi sonradan tüketilmiş bir malı bugünkü maliyetle geri
    /// koymak kâr-zararı bozar (REDDEDİLMİŞ alternatif). Kısmi geri alma
    /// YAPILMAZ — yarım geri alınmış bir işlem, geri alınmamış işlemden kötüdür.
    /// </summary>
    public static readonly HashSet<string> ErtelenenTurler =
        ["TAKAS", "HURDA_ALIS", "TRANSFER"];

    /// <summary>
    /// PAKET CIFT-BACAK (HATA-2): iade edilmiş emanetin girişi stornolanamaz.
    /// Ekranda ve yanıtta AYNI metin (UX-S31); düzeltme yolu metnin içindedir.
    /// Tek kaynak sözlüktür (Sozlukler.CiftBacak.cs): ekrandaki önceden kilit
    /// (Emanet.razor) ile sunucunun 409 metni aynı cümleyi okur.
    /// </summary>
    public static string EmanetIadeEdildiMesaji =>
        Kuyumcu.Core.I18n.CiftBacakSozlugu.Al(Kuyumcu.Core.I18n.IstekDili.Gecerli).EmanetIadeEdildiKilit;   // PAKET DIL-SUNUCU: istek diline göre

    public async Task<object> Storno(string hareketId, StornoDto dto, AktifKullanici islem)
    {
        // Gerekçe: DTO doğrulaması yalnız controller yolundadır (servis doğrudan
        // çağrıldığında çalışmaz) — kapı burada da kapatılır.
        var gerekce = GerekceDogrula(dto.Gerekce);

        // Find/FindAsync YASAK (denetim.sh): açık kiracı koşuluyla okunur.
        var kaynak = await db.Hareketler
            .FirstOrDefaultAsync(h => h.Id == hareketId && h.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Hareket bulunamadı");

        if (kaynak.StornoKaynakId is not null)
            await Reddet(islem, hareketId, gerekce, "storno satırının kendisi stornolanamaz",
                "Bu satır zaten bir STORNO kaydıdır; storno satırı stornolanamaz. "
                + "Yanlışlık varsa yeni bir kayıtla düzeltilir.");

        var zatenStorno = await db.Hareketler
            .AnyAsync(h => h.TenantId == islem.TenantId && h.StornoKaynakId == hareketId);
        if (zatenStorno)
            await Reddet(islem, hareketId, gerekce, "zaten stornolanmış",
                "Bu hareket zaten stornolanmıştır; bir hareket en fazla bir kez stornolanır.");

        if (!StornolanabilirTurler.Contains(kaynak.Tur))
            await Reddet(islem, hareketId, gerekce, $"tür desteklenmiyor ({kaynak.Tur})",
                TurMesaji(kaynak.Tur));

        // PAKET CIFT-BACAK (HATA-2) — ELDE KALAN EMANET KAPISI. Emanet girişinin
        // stornosu emaneti defterden DÜŞER; o emanetin bir kısmı müşteriye iade
        // edildiyse düşülecek mal elimizde yoktur ve defter eksiye iner (senaryo
        // 5.3/9.3: "emanet gümüş −92,5"). İade kapısıyla AYNI toplam okunur
        // (EmanetKurali.EldeKalan); doğru yol önce iadenin stornosudur.
        if (kaynak.Tur == "EMANET_GIRIS" && kaynak.CariId is not null)
        {
            var eldeKalan = await Modules.Emanet.EmanetKurali.EldeKalan(
                db, kaynak.CariId, islem.TenantId, kaynak.Maden);
            if (eldeKalan < kaynak.HasGrami)
                await Reddet(islem, hareketId, gerekce,
                    $"emanet iade edilmiş (elde {eldeKalan:0.######} < giriş {kaynak.HasGrami:0.######})",
                    EmanetIadeEdildiMesaji);
        }

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);

        var now = DateTime.UtcNow;

        // ── Kasa bacağı: kaynağın kasa hareketlerinden TÜRETİLİR ─────────────
        var kasaBacaklari = await db.KasaHareketleri
            .Where(k => k.TenantId == islem.TenantId && k.HareketId == kaynak.Id)
            .ToListAsync();

        var kasaIdler = kasaBacaklari.Select(k => k.KasaId).Distinct().ToList();
        if (kasaIdler.Count > 1)
            await Reddet(islem, hareketId, gerekce, "çok kasalı fiş",
                "Bu hareket birden fazla kasaya dokunuyor; çok bacaklı fişin stornosu "
                + "bu sürümde desteklenmiyor. Fişin tamamı için düzeltme fişi kullanın.");

        Data.Kasa? kasa = null;
        if (kasaIdler.Count == 1)
        {
            if (string.IsNullOrWhiteSpace(dto.KasaId))
                throw new BadRequestException(
                    "Bu hareket kasadan geçmiştir; storno için kasa seçimi zorunludur.");
            if (dto.KasaId != kasaIdler[0])
                throw new BadRequestException(
                    "Seçilen kasa, hareketin geçtiği kasa değildir; storno aynı kasaya yazılır.");

            // K6 (para birimi) + GÜN KİLİDİ (an = now): kapanmış BUGÜNE yazılmaz.
            kasa = await IslemDogrulayici.KasaDogrula(
                db, kasaIdler[0], islem.TenantId, kasaBacaklari[0].ParaBirimi, now);

            // Mehmet koşulu: KAYNAĞIN günü kapalıysa storno yalnız SAHİP'e açıktır.
            if (await KaynakGunuKapali(islem.TenantId, kasa.Id, kaynak.Tarih) && islem.Rol != "SAHIP")
                await Reddet(islem, hareketId, gerekce, "kapalı gün stornosu · rol yetersiz",
                    "Kaynağın günü kapatılmıştır; kapalı gün stornosunu yalnız SAHİP yapabilir.");
        }

        var kapaliGun = kasa is not null
            && await KaynakGunuKapali(islem.TenantId, kasa.Id, kaynak.Tarih);

        // ── Cari etkisi: TÜRE GÖRE AÇIK switch (uydurma yok) ─────────────────
        var (cariTl, cariHas) = CariEtkisi(kaynak, kasaBacaklari.Count);

        Data.Cari? cari = null;
        if (kaynak.CariId is not null && (cariTl != 0m || cariHas != 0m))
        {
            cari = await db.Cariler
                .FirstOrDefaultAsync(c => c.Id == kaynak.CariId && c.TenantId == islem.TenantId)
                ?? throw new NotFoundException("Cari bulunamadı");

            // Borcu ARTIRAN yönde (ör. tahsilat stornosu) kredi limiti işler.
            if (-cariTl > 0m)
                await Modules.Vade.VadeKapisi.KrediLimitiDogrula(db, cari, -cariTl, kaynak.KurSnapshotId);
        }

        // ── Ters kayıt ───────────────────────────────────────────────────────
        await using var tx = await db.Database.BeginTransactionAsync();
        // PAKET KAPORA-ESZAMAN (sınıf taraması): "zaten stornolanmış" kontrolü yukarıda işlem
        // DIŞINDA okunuyordu → iki eşzamanlı storno ikisi de geçip İKİ ters kayıt yazıyordu
        // (kasa iki kat geri). Kaynak satır kilitlenir, kontrol kilitten SONRA yinelenir.
        await db.Database.Kilitle(KilitTablosu.Hareketler, kaynak.Id, islem.TenantId);
        if (await db.Hareketler.AnyAsync(h => h.TenantId == islem.TenantId && h.StornoKaynakId == hareketId))
        {
            await tx.RollbackAsync();
            await Reddet(islem, hareketId, gerekce, "zaten stornolanmış (eşzamanlı)",
                "Bu hareket zaten stornolanmıştır; bir hareket en fazla bir kez stornolanır.");
        }

        var nakit = kaynak.NakitTutar ?? 0m;
        var veresiye = kaynak.VeresiyeTutar ?? 0m;
        // Kaynak borç DOĞURDUYSA (vadesi var) ters satır borç KAPATIR; kaynak
        // borç azalttıysa ters satır borç doğurur ve vade ZORUNLUDUR.
        DateOnly? tersVade = veresiye > 0m && kaynak.VadeTarihi is null && cari is not null
            ? Modules.Vade.VadeKapisi.VadeHesapla(cari, null, now)
            : null;

        var stornoId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db,
            // PAKET P-23c: storno ters kayıt YENİ ödeme kalemi DOĞURMAZ ve bu
            // bilinçlidir. Geri dönüş aşağıda kaynağın kalemlerini PASİFLEŞTİREREK
            // yapılır (ADR-0004 append-only). Ters satıra da kalem yazılsaydı
            // fişin aktif kalem toplamı fiş toplamından sapardı
            // (denetim.sh FIS_TOPLAM_ODEME_KALEMI_FARKI) ve araç kırılımlı rapor
            // hükümsüz kılınmış parayı İKİ KEZ sayardı.
            new OdemeIzi(nakit, veresiye, tersVade, KalemYazari.Cagiran),
            new Hareket
            {
                Id             = stornoId,
                TenantId       = islem.TenantId,
                SubeId         = kaynak.SubeId,
                Tur            = kaynak.Tur,   // tür KORUNUR: yeni bir "STORNO" türü
                                               // eski raporlarda satırı görünmez kılardı
                Tarih          = now,
                Aciklama       = "STORNO: " + gerekce,
                TlTutari       = -kaynak.TlTutari,
                ParaBirimi     = kaynak.ParaBirimi,
                HasGrami       = -kaynak.HasGrami,
                // K20-1: storno KAYNAĞIN madenini birebir tersler; maden değişmez.
                Maden          = kaynak.Maden,
                // P-DIL-BORC-KASA (Y18): emanetin bırakıldığı gibi izi de ters işaretle döner (ayar aynı).
                EmanetGram     = -kaynak.EmanetGram,
                EmanetTas      = -kaynak.EmanetTas,
                EmanetAyarId   = kaynak.EmanetAyarId,
                OdemeTuru      = kaynak.OdemeTuru,
                KurSnapshotId  = dto.KurSnapshotId,
                CariId         = kaynak.CariId,
                UrunId         = kaynak.UrunId,
                KullaniciId    = islem.Id,
                StornoKaynakId = kaynak.Id,
                Olusturuldu    = now,
            });

        // Kasa: her bacak TERS işaretle geri alınır (Math.Abs KULLANILMAZ).
        foreach (var bacak in kasaBacaklari)
        {
            // PAKET P-44 gerekçeli istisna: ters kayıt bakiye kapısına tabi DEĞİL.
            Modules.Kasa.KasaBakiyeKapisi.StornoIcinAtlanir(-bacak.Tutar);
            await BakiyeKilidi.Kilitle(db, kasa!);   // PAKET KAPORA-ESZAMAN tur 2: kilitli taze okuma (kayıp güncelleme)
            kasa!.Bakiye = Math.Round(kasa.Bakiye - bacak.Tutar, 2, MidpointRounding.AwayFromZero);
            kasa.Guncellendi = now;
            db.KasaHareketleri.Add(new KasaHareket
            {
                Id          = Kimlik.Ulid(),
                TenantId    = islem.TenantId,
                KasaId      = kasa.Id,
                HareketId   = stornoId,
                Tutar       = Math.Round(-bacak.Tutar, 2, MidpointRounding.AwayFromZero),
                ParaBirimi  = kasa.ParaBirimi,
                OdemeTuru   = bacak.OdemeTuru,
                Tarih       = now,
                Olusturuldu = now,
            });
        }

        // ── PAKET P-23 (R-17) — ÖDEME KALEMLERİ DE GERİ DÖNER ───────────────
        // Plan DoD: "Storno motoru ödeme kalemlerini de döndürür; unutulursa
        // para tek yönlü kaybolur." Kasa bacağı ters işaretle geri alınıyor
        // (yukarıda) ama kalem tablosu dokunulmasaydı fişin ödeme kırılımı
        // stornodan SONRA da yürürlükte görünürdü: araç kırılımlı rapor
        // hükümsüz kılınmış parayı saymaya devam ederdi.
        // ⚠ SİLİNMEZ (finansal varlık): satır DURUR, yalnız pasifleşir ve
        //   hangi stornonun hükümsüz kıldığı satıra yazılır (ADR-0004).
        // ⚠ Geçmiş fişte kalem YOKTUR (geri doldurma koşmadı) — döngü hiç
        //   dönmez ve eski davranış birebir korunur.
        var stornoKalemleri = await db.OdemeKalemleri
            .Where(k => k.TenantId == islem.TenantId && k.HareketId == kaynak.Id && k.Aktif)
            .ToListAsync();
        foreach (var kalem in stornoKalemleri)
        {
            kalem.Aktif = false;
            kalem.StornoHareketId = stornoId;
        }

        // Cari bakiye: kaynağın etkisi TERS uygulanır.
        if (cari is not null)
        {
            // PAKET CARI-BAKIYE: TEK YAZICI (Common/CariBakiyeServisi.cs) — kaynağın etkisi TERS uygulanır.
            await Common.CariBakiyeYazici.Uygula(db, islem.TenantId, cari.Id, -cariTl, -cariHas, now);
        }

        // M7a (17.10): bu TAHSILAT bir banka ödemesinin "borcundan düş" kararından doğduysa karar
        // AYNI işlemde boşalır — dekont yeniden "karar bekliyor" listesine düşer (borç geri geldi).
        if (kaynak.Tur == "TAHSILAT")
        {
            var kararli = await db.BankaHareketleri
                .Where(b => b.TenantId == islem.TenantId && b.KararHareketId == kaynak.Id)
                .Take(1).ToListAsync();
            foreach (var b in kararli) { b.CariKarari = null; b.KararHareketId = null; }
        }

        // Gerekçeli denetim izi — AYNI transaction, SaveChanges'tan ÖNCE.
        denetim.YazmaKaydet(islem, "Storno", stornoId,
            $"{kaynak.Tur} stornosu (kaynak {kaynak.Id})"
            + (kapaliGun ? " · KAPALI GÜN STORNOSU" : "")
            + $" · gerekçe: {gerekce}",
            hassas: true, kasaId: kasa?.Id);

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] STORNO: {Tur} kaynak={KaynakId} storno={StornoId}",
            islem.TenantId, kaynak.Tur, kaynak.Id, stornoId);

        var yeniBakiye = cari is null
            ? (decimal?)null
            : await db.CariBakiyeler.AsNoTracking()
                .Where(b => b.CariId == cari.Id && b.TenantId == islem.TenantId)
                .Select(b => (decimal?)b.TlBakiye).FirstOrDefaultAsync();

        return new
        {
            stornoHareketId = stornoId,
            kaynakHareketId = kaynak.Id,
            tur             = kaynak.Tur,
            tlTutari        = -kaynak.TlTutari,
            hasGrami        = -kaynak.HasGrami,
            kasaBakiyesi    = kasa?.Bakiye,
            cariBakiyesi    = yeniBakiye,
            kapaliGunStornosu = kapaliGun,
        };
    }

    /// <summary>
    /// Kaynağın cari bakiyeye ETKİSİ — türe göre AÇIK eşleme. Bilinmeyen tür
    /// buraya hiç gelmez (beyaz liste önce kapıyı kapatır); yine de default
    /// dalı REDDEDER (savunma derinliği).
    /// </summary>
    private static (decimal Tl, decimal Has) CariEtkisi(Hareket h, int kasaBacakSayisi) => h.Tur switch
    {
        // Bakiye sözleşmesi (K4): tl += h.TlTutari, has += -h.HasGrami.
        "VERESIYE" => (h.TlTutari, -h.HasGrami),
        // PAKET CARI-BAKIYE (tur 2): evrak tahsili (TAHSILAT +tutar) cariye YAZMAZ; stornosu da
        // yazmaz. Kural aynayla ORTAK (CariBakiyeKurali.TahsilatCariTl) — yazılmamış etki geri alınmaz.
        "TAHSILAT" => (Common.CariBakiyeKurali.TahsilatCariTl(h.TlTutari), -h.HasGrami),
        // PAKET CIFT-BACAK (HATA-1): emanet hareketi cari has kolonuna yalnız ALTIN
        // madeninde yazar (K20-2). Storno, KAYNAĞIN YAZDIĞINI tersler — yazılmamış
        // bir etki geri alınmaz. Kural yazan modülle ve aynayla ORTAK (EmanetKurali).
        "EMANET_GIRIS" or "EMANET_CIKIS" => (h.TlTutari, Modules.Emanet.EmanetKurali.CariHasEtkisi(h.Maden, h.HasGrami)),
        // İşçilik kasadan tahsil edildiyse cariye HİÇ yazılmamıştır.
        "ISCILIK" => kasaBacakSayisi > 0 ? (0m, 0m) : (h.TlTutari, 0m),
        _ => throw new ConflictException($"Bu türün cari etkisi tanımlı değil: {h.Tur}"),
    };

    private static string TurMesaji(string tur) => tur switch
    {
        "SATIS" or "SATIS_IADE" =>
            "Satış stornolanmaz; düzeltme yolu SATIŞ İADESİDİR (POST /api/v1/islemler/satis-iade).",
        "ALIS" or "ALIS_IADE" =>
            "Alış stornolanmaz; düzeltme yolu ALIŞ İADESİDİR (POST /api/v1/islemler/alis-iade).",
        "CARI_DUZELTME" =>
            "Cari düzeltme fişi stornolanmaz; ters delta taşıyan yeni bir düzeltme fişi kesilir "
            + "(POST /api/v1/cari/{id}/duzeltme).",
        "SAYIM_DUZELTME" =>
            "Sayım düzeltmesi stornolanmaz; düzeltmesi yeni bir sayım oturumudur.",
        "TAKAS" or "HURDA_ALIS" or "TRANSFER" =>
            $"{tur} stornosu bu sürümde KAPALIDIR: bu türler stok ve maliyet partisi geri alması "
            + "gerektirir; partisi sonradan tüketilmiş malı bugünkü maliyetle geri koymak kâr-zararı "
            + "bozar. Kısmi geri alma yapılmaz (fail-closed).",
        _ => $"'{tur}' türü storno beyaz listesinde değildir; bu tür için ters kayıt üretilmez.",
    };

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET S-3b — ELLE GİRİLEN KASA HAREKETİNİN STORNOSU.
    //
    // KURUL TUR 3 / ENGEL 1 (KRİTİK): "elle girilen kasa hareketi hiçbir şekilde
    // düzeltilemiyor" — Geçmiş modalındaki tek eylem kalıcı olarak disabled'dı.
    // Yanlış tutar ancak ters yönde İKİNCİ BİR ELLE HAREKET ile dengelenebiliyor,
    // o da geçmişte birbirine BAĞLANMAMIŞ iki satır olarak kalıyordu.
    //
    // ⚠ İKİNCİ MOTOR YAZILMADI (Bertan mimari vetosu): ters kayıt üretme yetkisi
    // bu sınıfta, tek yerde kalır. Kasa hareketi defter hareketi DEĞİLDİR
    // (hareketler tablosunda satırı yoktur), bu yüzden HareketFabrikasi.Ekle
    // yolu KULLANILMAZ — o kapı yalnız deftere yazan satırlar içindir. Buradaki
    // ters satır kasa_hareketleri'ne yazılır ve kaynağına storno_kaynak_id ile
    // BAĞLANIR (QA turu 3 önerisi: "üretilen ters kayıt kaynak satıra bağlansın").
    //
    // KURALLAR (fail-closed, Storno ile aynı):
    //   • YALNIZ ELLE hareket: hareket_id DOLU olan satır bir defter fişinin
    //     bacağıdır; onun düzeltmesi fişin kendi stornosudur (409, yol yazılır).
    //   • ÇİFT STORNO YASAK; storno satırının kendisi stornolanamaz.
    //   • GEREKÇE ≥ 20 KARAKTER (servis içinde ELLE doğrulanır).
    //   • GÜN KİLİDİ: kapanmış BUGÜNE satır eklenmez; kaynağın günü kapalıysa
    //     yalnız SAHİP düzeltebilir (Mehmet koşulu, Storno ile aynı).
    //   • DEFTER YERİNDE DEĞİŞMEZ: kaynak satıra DOKUNULMAZ.
    // ═════════════════════════════════════════════════════════════════════════
    public async Task<object> KasaHareketiStorno(
        string kasaId, string kasaHareketId, string? gerekceHam, AktifKullanici islem)
    {
        // Kur bu yolda İSTENMEZ: elle kasa hareketinin has/kur bacağı yoktur
        // (deftere yazmaz). Bkz. Common/GerekceDto.cs — kur verisi olmayan bir
        // kiracıda bu düzeltme yolu kapanmamalıdır.
        var gerekce = GerekceDogrula(gerekceHam);

        var kasa = await db.Kasalar
            .FirstOrDefaultAsync(k => k.Id == kasaId && k.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Kasa bulunamadı");

        var kaynak = await db.KasaHareketleri
            .FirstOrDefaultAsync(h => h.Id == kasaHareketId
                                   && h.TenantId == islem.TenantId
                                   && h.KasaId == kasaId)
            ?? throw new NotFoundException("Kasa hareketi bulunamadı");

        if (kaynak.HareketId is not null)
            await KasaReddet(islem, kaynak.Id, gerekce, "defter fişine bağlı bacak",
                "Bu kasa hareketi bir defter işleminin bacağıdır; tek başına stornolanamaz. "
                + "Düzeltme yolu işlemin KENDİ stornosudur (POST /api/v1/islemler/{hareketId}/storno).");

        if (kaynak.StornoKaynakId is not null)
            await KasaReddet(islem, kaynak.Id, gerekce, "storno satırının kendisi stornolanamaz",
                "Bu satır zaten bir STORNO kaydıdır; storno satırı stornolanamaz. "
                + "Yanlışlık varsa yeni bir kasa hareketi girilir.");

        // PAKET P-19 (Y-11): VİRMAN BACAĞI TEK BAŞINA STORNOLANAMAZ.
        // ⚠ Bu kapı olmasaydı virmanın yalnız bir yarısı geri döner, karşı kasa
        //   şişmiş kalırdı — "virman bakiyeyi çift sayar" riskinin tam da o hâli.
        if (kaynak.KarsiBacakId is not null)
            await KasaReddet(islem, kaynak.Id, gerekce, "virman bacağı · tek bacak stornosu",
                "Bu kasa hareketi bir VİRMANIN bacağıdır; tek başına stornolanamaz, yoksa paranın "
                + "yalnız yarısı geri döner. Doğru yol virmanın kendi stornosudur "
                + $"(POST /api/v1/virman/{kaynak.Id}/storno) — iki bacak birlikte döner.");

        var zaten = await db.KasaHareketleri
            .AnyAsync(h => h.TenantId == islem.TenantId && h.StornoKaynakId == kaynak.Id);
        if (zaten)
            await KasaReddet(islem, kaynak.Id, gerekce, "zaten stornolanmış",
                "Bu kasa hareketi zaten stornolanmıştır; bir hareket en fazla bir kez stornolanır.");

        var now = DateTime.UtcNow;

        // BUGÜN kapalıysa satır eklenmez (Storno ile aynı kapı).
        await Modules.Kasa.GunKapanisiKapisi.Dogrula(db, islem.TenantId, kasa.Id, now);

        // Mehmet koşulu: KAYNAĞIN günü kapatılmışsa düzeltmeyi yalnız SAHİP yapar.
        var kapaliGun = await KaynakGunuKapali(islem.TenantId, kasa.Id, kaynak.Tarih);
        if (kapaliGun && islem.Rol != "SAHIP")
            await KasaReddet(islem, kaynak.Id, gerekce, "kapalı gün stornosu · rol yetersiz",
                "Kaynağın günü kapatılmıştır; kapalı gün düzeltmesini yalnız SAHİP yapabilir.");

        await using var tx = await db.Database.BeginTransactionAsync();
        // PAKET KAPORA-ESZAMAN (sınıf taraması): kilit + kontrolün yinelenmesi (Storno ile aynı).
        await db.Database.Kilitle(KilitTablosu.KasaHareketleri, kaynak.Id, islem.TenantId);
        if (await db.KasaHareketleri.AnyAsync(h => h.TenantId == islem.TenantId && h.StornoKaynakId == kaynak.Id))
        {
            await tx.RollbackAsync();
            await KasaReddet(islem, kaynak.Id, gerekce, "zaten stornolanmış (eşzamanlı)",
                "Bu kasa hareketi zaten stornolanmıştır; bir hareket en fazla bir kez stornolanır.");
        }

        var tersTutar = Math.Round(-kaynak.Tutar, 2, MidpointRounding.AwayFromZero);
        var stornoId = Kimlik.Ulid();

        db.KasaHareketleri.Add(new KasaHareket
        {
            Id             = stornoId,
            TenantId       = islem.TenantId,
            KasaId         = kasa.Id,
            HareketId      = null,
            Tutar          = tersTutar,
            ParaBirimi     = kaynak.ParaBirimi,   // K6: para birimi kaynaktan, kasadan DEĞİL
            OdemeTuru      = kaynak.OdemeTuru,
            KomisyonOran   = kaynak.KomisyonOran,
            // PAKET P-19 (Y-12): gider kategorisi TERS SATIRA DA yazılır. Aksi
            // hâlde stornolanan gider kâr-zararda asılı kalırdı: çıkış satırı
            // kategorili (gider sayılır), ters satırı kategorisiz (sayılmaz).
            // Kategori taşındığı için toplam kendiliğinden sıfırlanır — ikinci
            // bir "stornolanmışları dışla" süzgeci yazılmaz.
            GiderKategoriId = kaynak.GiderKategoriId,
            Aciklama       = "STORNO: " + gerekce,
            StornoKaynakId = kaynak.Id,
            Tarih          = now,
            Olusturuldu    = now,
        });

        // PAKET P-44 gerekçeli istisna: ters kayıt bakiye kapısına tabi DEĞİL.
        Modules.Kasa.KasaBakiyeKapisi.StornoIcinAtlanir(tersTutar);
        await BakiyeKilidi.Kilitle(db, kasa);   // PAKET KAPORA-ESZAMAN tur 2: kilitli taze okuma (kayıp güncelleme)
        kasa.Bakiye = Math.Round(kasa.Bakiye + tersTutar, 2, MidpointRounding.AwayFromZero);
        kasa.Guncellendi = now;

        denetim.YazmaKaydet(islem, "KasaHareketiStorno", stornoId,
            $"elle kasa hareketi stornosu (kaynak {kaynak.Id})"
            + (kapaliGun ? " · KAPALI GÜN STORNOSU" : "")
            + $" · gerekçe: {gerekce}",
            hassas: true, kasaId: kasa.Id);

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] KASA STORNO: kaynak={KaynakId} storno={StornoId}",
            islem.TenantId, kaynak.Id, stornoId);

        return new
        {
            stornoKasaHareketId = stornoId,
            kaynakKasaHareketId = kaynak.Id,
            tutar               = tersTutar,
            paraBirimi          = kaynak.ParaBirimi,
            kasaBakiyesi        = kasa.Bakiye,
            kapaliGunStornosu   = kapaliGun,
        };
    }

    // ═════════════════════════════════════════════════════════════════════════
    // BANKA DEKONTU STORNOSU — PAKET P-12 (B-42).
    //
    // NEDEN BURADA: tek storno motoru kuralı (PLAN § 1 md.8). Dekontun kendi
    // modülünde ikinci bir ters-kayıt mantığı açılsaydı, "çift storno yasak" ve
    // "storno satırı stornolanamaz" kuralları iki yerde ayrı ayrı yaşardı.
    //
    // KURALLAR (Kasa yoluyla AYNI):
    //   • DEFTER YERİNDE DEĞİŞMEZ: kaynak dekonta DOKUNULMAZ; hükümsüzlük ters
    //     satırın VARLIĞINDAN okunur (ADR-0004).
    //   • ÇİFT STORNO YASAK; storno satırının kendisi stornolanamaz.
    //   • GEREKÇE ≥ 20 KARAKTER (servis içinde ELLE doğrulanır).
    //   • FATURALANMIŞ DEKONT STORNOLANMAZ: dekont bir faturaya dönüştürülmüşse
    //     ters kayıt faturayı öksüz bırakır; önce fatura iptal edilir.
    //   • EKSTREDEN GELEN DEKONT STORNOLANMAZ: o satır bankanın kendi kaydının
    //     aynasıdır; bankada olmayan bir düzeltme uydurulamaz (ekstre yeniden
    //     yüklendiğinde mutabakat kırılırdı).
    //   • KUR İSTENMEZ: banka dekontunun has/kur bacağı yoktur (deftere yazmaz).
    //
    // ⚠ BAKİYE: hesap bakiyesi ters satırın tutarı kadar düzeltilir; tutar
    //   işaretli değil YÖNlüdür (GELEN/GIDEN), bu yüzden ters satır AYNI tutarı
    //   KARŞI yönle taşır — negatif tutar yazılsaydı `Range(0.01, …)` sözleşmesi
    //   ve tüm toplam sorguları bozulurdu.
    // ═════════════════════════════════════════════════════════════════════════
    public async Task<object> BankaHareketiStorno(
        string hesapId, string bankaHareketId, string? gerekceHam, AktifKullanici islem)
    {
        var gerekce = GerekceDogrula(gerekceHam);

        var hesap = await db.BankaHesaplari
            .FirstOrDefaultAsync(h => h.Id == hesapId && h.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Banka hesabı bulunamadı");

        var kaynak = await db.BankaHareketleri
            .FirstOrDefaultAsync(h => h.Id == bankaHareketId
                                   && h.TenantId == islem.TenantId
                                   && h.BankaHesapId == hesapId)
            ?? throw new NotFoundException("Banka hareketi bulunamadı");

        if (kaynak.StornoKaynakId is not null)
            await BankaReddet(islem, kaynak.Id, gerekce, "storno satırının kendisi stornolanamaz",
                "Bu satır zaten bir STORNO kaydıdır; storno satırı stornolanamaz. "
                + "Yanlışlık varsa yeni bir dekont girilir.");

        // PAKET P-19 (Y-11): VİRMAN BACAĞI TEK BAŞINA STORNOLANAMAZ (kasa yoluyla AYNI).
        if (kaynak.KarsiBacakId is not null)
            await BankaReddet(islem, kaynak.Id, gerekce, "virman bacağı · tek bacak stornosu",
                "Bu dekont bir VİRMANIN bacağıdır; tek başına stornolanamaz, yoksa paranın yalnız "
                + "yarısı geri döner. Doğru yol virmanın kendi stornosudur "
                + $"(POST /api/v1/virman/{kaynak.Id}/storno) — iki bacak birlikte döner.");

        var zaten = await db.BankaHareketleri
            .AnyAsync(h => h.TenantId == islem.TenantId && h.StornoKaynakId == kaynak.Id);
        if (zaten)
            await BankaReddet(islem, kaynak.Id, gerekce, "zaten stornolanmış",
                "Bu dekont zaten stornolanmıştır; bir dekont en fazla bir kez stornolanır.");

        if (kaynak.FaturaId is not null || kaynak.Durum == "FATURALANDI")
            await BankaReddet(islem, kaynak.Id, gerekce, "faturalanmış dekont",
                "Bu dekont bir faturaya dönüştürülmüştür; ters kayıt faturayı dayanaksız bırakır. "
                + "Önce faturayı iptal edin, sonra dekontu stornolayın.");

        // M7a (17.10): borcundan düşülmüş ödeme önce geri alınır; yoksa müşterinin borcu düşük kalır.
        if (kaynak.CariKarari == Modules.Banka.CariKarari.Borc)
            await BankaReddet(islem, kaynak.Id, gerekce, "borçtan düşülmüş ödeme",
                "Bu ödeme müşterinin borcundan düşüldü. Önce 'Borçtan düşmeyi geri al' deyin, sonra dekontu stornolayın.");

        if (kaynak.EkstreSatirId is not null)
            await BankaReddet(islem, kaynak.Id, gerekce, "ekstreden gelen dekont",
                "Bu dekont banka ekstresinden geldi; bankanın kendi kaydı ekrandan hükümsüz "
                + "kılınamaz. Bankada gerçekten bir düzeltme olduysa onu yeni bir dekont "
                + "olarak girin.");

        var now = DateTime.UtcNow;
        var tersYon = kaynak.Yon == Kuyumcu.Core.Domain.HareketYonu.BankaGelen
            ? Kuyumcu.Core.Domain.HareketYonu.BankaGiden
            : Kuyumcu.Core.Domain.HareketYonu.BankaGelen;

        await using var tx = await db.Database.BeginTransactionAsync();
        // PAKET KAPORA-ESZAMAN (sınıf taraması): kilit + kontrolün yinelenmesi (Storno ile aynı).
        await db.Database.Kilitle(KilitTablosu.BankaHareketleri, kaynak.Id, islem.TenantId);
        if (await db.BankaHareketleri.AnyAsync(h => h.TenantId == islem.TenantId && h.StornoKaynakId == kaynak.Id))
        {
            await tx.RollbackAsync();
            await BankaReddet(islem, kaynak.Id, gerekce, "zaten stornolanmış (eşzamanlı)",
                "Bu dekont zaten stornolanmıştır; bir dekont en fazla bir kez stornolanır.");
        }

        var stornoId = Kimlik.Ulid();
        db.BankaHareketleri.Add(new BankaHareket
        {
            Id             = stornoId,
            TenantId       = islem.TenantId,
            BankaHesapId   = hesap.Id,
            Yon            = tersYon,
            Tutar          = kaynak.Tutar,
            ParaBirimi     = kaynak.ParaBirimi,   // para birimi KAYNAKTAN, hesaptan değil
            KarsiTarafAd   = kaynak.KarsiTarafAd,
            KarsiIban      = kaynak.KarsiIban,
            Aciklama       = "STORNO: " + gerekce,
            CariId         = kaynak.CariId,
            FaturaId       = null,
            HareketId      = null,
            EkstreSatirId  = null,
            BankaReferans  = null,
            DedupeAnahtar  = null,   // ters kayıt mükerrer anahtarı TAŞIMAZ
            Durum          = "BEKLIYOR",
            StornoKaynakId = kaynak.Id,
            Tarih          = now,
            Olusturuldu    = now,
        });

        var etki = tersYon == Kuyumcu.Core.Domain.HareketYonu.BankaGelen ? kaynak.Tutar : -kaynak.Tutar;
        await BakiyeKilidi.Kilitle(db, hesap);   // PAKET KAPORA-ESZAMAN tur 2: kilitli taze okuma (kayıp güncelleme)
        hesap.Bakiye = Math.Round(hesap.Bakiye + etki, 2, MidpointRounding.AwayFromZero);
        hesap.Guncellendi = now;

        denetim.YazmaKaydet(islem, "BankaHareketiStorno", stornoId,
            $"banka dekontu stornosu (kaynak {kaynak.Id}) · gerekçe: {gerekce}");

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] BANKA STORNO: kaynak={KaynakId} storno={StornoId}",
            islem.TenantId, kaynak.Id, stornoId);

        return new
        {
            stornoBankaHareketId = stornoId,
            kaynakBankaHareketId = kaynak.Id,
            yon                  = tersYon,
            tutar                = kaynak.Tutar,
            paraBirimi           = kaynak.ParaBirimi,
            hesapBakiyesi        = hesap.Bakiye,
        };
    }

    // ═════════════════════════════════════════════════════════════════════════
    // VİRMAN STORNOSU — PAKET P-19 (Y-11).
    //
    // NEDEN BURADA: tek storno motoru kuralı (PLAN § 1 md.8). Virmanın kendi
    // modülünde ikinci bir ters-kayıt mantığı açılsaydı "çift storno yasak",
    // "storno satırı stornolanamaz" ve gerekçe kapısı iki yerde ayrı yaşardı.
    //
    // ⚠ İKİ BACAK BİRLİKTE DÖNER. Ters kayıtlar da bir ÇİFTTİR ve yine
    //   CiftBacakFabrikasi ile, TEK SaveChangesAsync içinde yazılır: storno
    //   yarım kalırsa virmanın kendisinden daha büyük bir hasar doğardı.
    // ═════════════════════════════════════════════════════════════════════════
    public async Task<Modules.Gider.VirmanStornoCiktisi> VirmanStorno(string bacakId, string? gerekceHam, AktifKullanici islem)
    {
        var gerekce = GerekceDogrula(gerekceHam);

        var (kaynak, karsi) = await VirmanCifti(bacakId, islem.TenantId);

        foreach (var b in new[] { kaynak, karsi })
        {
            if (StornoSatiriMi(b))
                throw new ConflictException(
                    "Bu satır zaten bir STORNO kaydıdır; storno satırı stornolanamaz. "
                    + "Yanlışlık varsa yeni bir virman girilir.");
        }

        var kaynakId = kaynak.Id;
        var karsiId = karsi.Id;
        var zatenKasa = await db.KasaHareketleri.AsNoTracking()
            .AnyAsync(h => h.TenantId == islem.TenantId
                        && h.StornoKaynakId != null
                        && (h.StornoKaynakId == kaynakId || h.StornoKaynakId == karsiId));
        var zatenBanka = await db.BankaHareketleri.AsNoTracking()
            .AnyAsync(h => h.TenantId == islem.TenantId
                        && h.StornoKaynakId != null
                        && (h.StornoKaynakId == kaynakId || h.StornoKaynakId == karsiId));
        if (zatenKasa || zatenBanka)
            throw new ConflictException(
                "Bu virman zaten stornolanmıştır; bir virman en fazla bir kez stornolanır.");

        var now = DateTime.UtcNow;

        // Her iki bacağın kasası için gün kilidi (banka bacağında gün kapanışı yoktur).
        foreach (var b in new[] { kaynak, karsi })
            if (b is KasaHareket kh)
                await Modules.Kasa.GunKapanisiKapisi.Dogrula(db, islem.TenantId, kh.KasaId, now);

        await using var tx = await db.Database.BeginTransactionAsync();
        // PAKET KAPORA-ESZAMAN (sınıf taraması): iki bacak SABİT sırayla (kimliğe göre) kilitlenir
        // — A bacağından ve B bacağından gelen iki eşzamanlı istek kilitlenmeye (deadlock) düşmez;
        // "zaten stornolanmış" kontrolü kilitten SONRA yinelenir.
        foreach (var b in new[] { kaynak, karsi }.OrderBy(x => x.Id, StringComparer.Ordinal))
            await db.Database.Kilitle(b is KasaHareket ? KilitTablosu.KasaHareketleri : KilitTablosu.BankaHareketleri,
                b.Id, islem.TenantId);
        if (await db.KasaHareketleri.AnyAsync(h => h.TenantId == islem.TenantId && h.StornoKaynakId != null
                                                  && (h.StornoKaynakId == kaynakId || h.StornoKaynakId == karsiId))
            || await db.BankaHareketleri.AnyAsync(h => h.TenantId == islem.TenantId && h.StornoKaynakId != null
                                                  && (h.StornoKaynakId == kaynakId || h.StornoKaynakId == karsiId)))
            throw new ConflictException(
                "Bu virman zaten stornolanmıştır; bir virman en fazla bir kez stornolanır.");


        var tersKaynak = TersBacak(kaynak, gerekce, now);
        var tersKarsi  = TersBacak(karsi, gerekce, now);

        CiftBacakFabrikasi.BacaklariEsle(db, tersKaynak, tersKarsi);

        await BacakBakiyesiIsle(tersKaynak, now);
        await BacakBakiyesiIsle(tersKarsi, now);

        denetim.YazmaKaydet(islem, "VirmanStorno", tersKaynak.Id,
            $"virman stornosu · kaynak bacaklar {kaynakId} / {karsiId} · "
            + $"ters bacaklar {tersKaynak.Id} / {tersKarsi.Id} · gerekçe: {gerekce}",
            hassas: true,
            kasaId: kaynak is KasaHareket k0 ? k0.KasaId : null);

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("[{TenantId}] VIRMAN STORNO: {A}/{B} → {TA}/{TB}",
            islem.TenantId, kaynakId, karsiId, tersKaynak.Id, tersKarsi.Id);

        return new Modules.Gider.VirmanStornoCiktisi(
            [kaynakId, karsiId], [tersKaynak.Id, tersKarsi.Id], 2, gerekce, now);
    }

    /// <summary>
    /// Bacağı ve KARŞI bacağını birlikte çözer. Karşı bacağı bulunamayan satır
    /// bir VERİ HATASIDIR (eşsiz bacak) ve GİZLENMEZ (Anayasa md.1): 409 döner.
    /// </summary>
    private async Task<(IBacakliHareket Kaynak, IBacakliHareket Karsi)> VirmanCifti(
        string bacakId, string tenantId)
    {
        IBacakliHareket? bacak =
            await db.KasaHareketleri.FirstOrDefaultAsync(h => h.Id == bacakId && h.TenantId == tenantId)
            ?? (IBacakliHareket?)await db.BankaHareketleri.FirstOrDefaultAsync(h => h.Id == bacakId && h.TenantId == tenantId);

        if (bacak is null) throw new NotFoundException("Virman bacağı bulunamadı");

        if (bacak.KarsiBacakId is null)
            throw new BadRequestException(
                "Bu satır bir virman bacağı değildir (karşı bacağı yok). Elle girilmiş kasa hareketi "
                + "ya da dekont ise kendi storno ucundan geri alınır.");

        var karsiId = bacak.KarsiBacakId;
        IBacakliHareket? karsi =
            await db.KasaHareketleri.FirstOrDefaultAsync(h => h.Id == karsiId && h.TenantId == tenantId)
            ?? (IBacakliHareket?)await db.BankaHareketleri.FirstOrDefaultAsync(h => h.Id == karsiId && h.TenantId == tenantId);

        if (karsi is null)
            throw new ConflictException(
                $"EŞSİZ BACAK: {bacakId} satırının karşı bacağı ({karsiId}) bulunamadı. Bu bir veri "
                + "hatasıdır; storno yarım bir virmanı düzeltmez. Kayıt destek ile incelenmelidir.");

        if (karsi.KarsiBacakId != bacak.Id)
            throw new ConflictException(
                $"BACAK EŞLEŞMESİ BOZUK: {bacakId} → {karsiId} ama {karsiId} → {karsi.KarsiBacakId ?? "—"}. "
                + "Bu bir veri hatasıdır; storno yapılmaz.");

        return (bacak, karsi);
    }

    private static bool StornoSatiriMi(IBacakliHareket b) => b switch
    {
        KasaHareket k  => k.StornoKaynakId is not null,
        BankaHareket d => d.StornoKaynakId is not null,
        _ => false,
    };

    /// <summary>Kaynağın AYNASI: ters işaret/yön, storno bağı, gerekçeli açıklama.</summary>
    private static IBacakliHareket TersBacak(IBacakliHareket kaynak, string gerekce, DateTime now) => kaynak switch
    {
        KasaHareket k => new KasaHareket
        {
            Id              = Kimlik.Ulid(),
            TenantId        = k.TenantId,
            KasaId          = k.KasaId,
            HareketId       = null,
            Tutar           = Math.Round(-k.Tutar, 2, MidpointRounding.AwayFromZero),
            ParaBirimi      = k.ParaBirimi,
            OdemeTuru       = k.OdemeTuru,
            KomisyonOran    = k.KomisyonOran,
            GiderKategoriId = k.GiderKategoriId,
            Aciklama        = "STORNO: " + gerekce,
            StornoKaynakId  = k.Id,
            Tarih           = now,
            Olusturuldu     = now,
        },
        BankaHareket d => new BankaHareket
        {
            Id             = Kimlik.Ulid(),
            TenantId       = d.TenantId,
            BankaHesapId   = d.BankaHesapId,
            Yon            = d.Yon == Kuyumcu.Core.Domain.HareketYonu.BankaGelen
                                ? Kuyumcu.Core.Domain.HareketYonu.BankaGiden
                                : Kuyumcu.Core.Domain.HareketYonu.BankaGelen,
            Tutar          = d.Tutar,
            ParaBirimi     = d.ParaBirimi,
            KarsiTarafAd   = d.KarsiTarafAd,
            KarsiIban      = d.KarsiIban,
            Aciklama       = "STORNO: " + gerekce,
            CariId         = null,
            FaturaId       = null,
            HareketId      = null,
            EkstreSatirId  = null,
            BankaReferans  = null,
            DedupeAnahtar  = null,
            Durum          = "ESLESTI",
            StornoKaynakId = d.Id,
            Tarih          = now,
            Olusturuldu    = now,
        },
        _ => throw new InvalidOperationException($"Bilinmeyen virman bacağı türü: {kaynak.GetType().Name}"),
    };

    /// <summary>Ters bacağın bakiye etkisini işler (kasa işaretli tutar, banka yönlü).</summary>
    private async Task BacakBakiyesiIsle(IBacakliHareket ters, DateTime now)
    {
        switch (ters)
        {
            case KasaHareket k:
            {
                var kasa = await db.Kasalar.FirstAsync(x => x.Id == k.KasaId);
                // PAKET P-44 gerekçeli istisna: ters kayıt bakiye kapısına tabi DEĞİL.
                Modules.Kasa.KasaBakiyeKapisi.StornoIcinAtlanir(k.Tutar);
                await BakiyeKilidi.Kilitle(db, kasa);   // PAKET KAPORA-ESZAMAN tur 2: kilitli taze okuma (kayıp güncelleme)
                kasa.Bakiye = Math.Round(kasa.Bakiye + k.Tutar, 2, MidpointRounding.AwayFromZero);
                kasa.Guncellendi = now;
                break;
            }
            case BankaHareket d:
            {
                var hesap = await db.BankaHesaplari.FirstAsync(x => x.Id == d.BankaHesapId);
                var etki = d.Yon == Kuyumcu.Core.Domain.HareketYonu.BankaGelen ? d.Tutar : -d.Tutar;
                await BakiyeKilidi.Kilitle(db, hesap);   // PAKET KAPORA-ESZAMAN tur 2: kilitli taze okuma (kayıp güncelleme)
                hesap.Bakiye = Math.Round(hesap.Bakiye + etki, 2, MidpointRounding.AwayFromZero);
                hesap.Guncellendi = now;
                break;
            }
        }
    }

    private async Task BankaReddet(
        AktifKullanici islem, string bankaHareketId, string gerekce, string sebep, string mesaj)
    {
        await denetim.RedKaydet(islem, "BankaHareketiStorno", bankaHareketId,
            $"banka dekontu stornosu REDDEDİLDİ ({sebep}) · gerekçe: {gerekce}");
        throw new ConflictException(mesaj);
    }

    /// <summary>
    /// Gerekçe kapısı — DataAnnotations yalnız controller yolunda çalışır,
    /// servis doğrudan çağrıldığında da kapanmalıdır. İki storno yolu da
    /// AYNI kuralı kullanır (tek kaynak).
    /// </summary>
    public static string GerekceDogrula(string? ham)
    {
        var gerekce = (ham ?? "").Trim();
        if (gerekce.Length < 20)
            throw new BadRequestException(
                $"Storno gerekçesi en az 20 karakter olmalıdır (girilen: {gerekce.Length}).");
        if (gerekce.Length > 1000)
            throw new BadRequestException("Storno gerekçesi en fazla 1000 karakter olabilir.");
        return gerekce;
    }

    private async Task KasaReddet(
        AktifKullanici islem, string kasaHareketId, string gerekce, string sebep, string mesaj)
    {
        await denetim.RedKaydet(islem, "KasaHareketiStorno", kasaHareketId,
            $"kasa hareketi stornosu REDDEDİLDİ ({sebep}) · gerekçe: {gerekce}");
        throw new ConflictException(mesaj);
    }

    private async Task<bool> KaynakGunuKapali(string tenantId, string kasaId, DateTime kaynakAn)
    {
        var yerelGun = DateOnly.FromDateTime(Zaman.UtcdenYerele(kaynakAn));
        var kapanis = await db.GunKapanislari.AsNoTracking()
            .FirstOrDefaultAsync(g => g.TenantId == tenantId && g.KasaId == kasaId && g.YerelGun == yerelGun);
        return kapanis is not null && kapanis.AcilisZamani is null;
    }

    /// <summary>
    /// REDDEDİLEN storno denemesi — storno transaction'ının DIŞINDA, kendi
    /// SaveChanges'i ile yazılır (Mehmet/ADR-0004 koşulu): aynı transaction'da
    /// yazılsaydı geri alınan denemenin izi de kaybolurdu.
    /// </summary>
    private async Task Reddet(
        AktifKullanici islem, string hareketId, string gerekce, string sebep, string mesaj)
    {
        await denetim.RedKaydet(islem, "Storno", hareketId,
            $"storno REDDEDİLDİ ({sebep}) · gerekçe: {gerekce}");
        throw new ConflictException(mesaj);
    }
}
