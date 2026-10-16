using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Tevsik;

// ═════════════════════════════════════════════════════════════════════════════
// TEVSİK KAPISI — 459 s. VUK Genel Tebliği · PLAN-2 Adım 2
//
// Saf kural (TevsikKurali) karar verir; bu sınıf ona günlük kümülatif nakit
// toplamını okur ve kararı UYGULAR.
//
// FAIL-CLOSED (Mehmet, K8): had aşılırsa istisna atılır ve işlem KAPANMAZ.
// "Sessiz loglama kabul değil" — çünkü 459 s. VUK Genel Tebliği cezası alıcı ve satıcıya
// BİRDEN kesilir; kullanıcıyı uyarıp geçmek onu cezaya sokmaktır.
//
// MASAK kapısıyla AYNI DESENİ izler: doğrulama transaction'dan ÖNCE yapılır ki
// hadde takılan bir işlemde hiçbir yazma başlamasın.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TevsikKapisi(TevsikKurali kural)
{
    /// <summary>Yapılandırma verilmeden kullanılan testler için varsayılan eşiklerle.</summary>
    public static TevsikKapisi Varsayilan() => new(new TevsikKurali(new TevsikEsikleri()));

    /// <summary>
    /// PAKET YASAL-1: VERİLEN YEREL GÜNÜN eşikleri (tarihsiz <c>Esikler</c> kaldırıldı —
    /// had tarihli tablodan gelir; unutulan çağrı derleme hatası verir).
    /// </summary>
    public TevsikEsikleri EsiklerGunu(DateOnly gun) => kural.EsiklerGunu(gun);

    /// <summary>
    /// Nakit ödemeyi 459 s. VUK Genel Tebliği'ne karşı denetler. Had aşılırsa
    /// <see cref="BadRequestException"/> atar — işlem tamamlanmaz (fail-closed).
    /// </summary>
    /// <param name="cariId">
    /// Karşı taraf. <b>null ise kümülatif toplam hesaplanamaz</b> (kişi
    /// bilinmiyor); yalnız bu işlemin kendi tutarı değerlendirilir.
    /// </param>
    /// <param name="nakitTutar">Bu işlemde nakit hareket eden tutar (işaret önemsiz).</param>
    /// <param name="an">İşlem anı (UTC). Yerel güne çevrilerek gün sınırı bulunur.</param>
    /// <param name="islemToplamTutari">
    /// İşlemin TOPLAM tutarı (kart/havale/veresiye bacakları dâhil). Verilirse
    /// §4.1.2 işlem tutarı kuralı da uygulanır. Trampa/takas gibi mevzuatta
    /// karşılığı bulunmayan hâllerde VERİLMEZ.
    /// </param>
    public async Task<TevsikSonuc> Dogrula(
        KuyumcuDbContext db, string tenantId, string? cariId, decimal nakitTutar, DateTime an,
        decimal? islemToplamTutari = null)
    {
        // PAKET YASAL-1 (imza satırı): İŞLEM GÜNÜNÜN eşikleri — tablo tanımsızsa 503, işlem yazılmaz.
        var esik = kural.EsiklerGunu(Common.YasalParametreServisi.IslemGunu(an));

        // ── PAKET B-3 (D2 · K2) — PEŞİN MÜŞTERİ: EŞİK ALTI ŞERİT, FAIL-CLOSED ──
        // Nakit payı sıfır olsa bile (tamamı kartla) FİŞ TOPLAMI hadde ulaşan
        // işlem bu kartla yapılamaz. Kural kapatılmış olsa da (Etkin=false)
        // uygulanır: bu kart için kapı yalnız SIKILAŞIR.
        if (await Modules.Cari.SistemCarisi.MiAsync(db, tenantId, cariId))
        {
            var olcut = Math.Max(Math.Abs(nakitTutar), Math.Abs(islemToplamTutari ?? 0m));
            if (olcut > esik.Had)
                throw new BadRequestException(Modules.Cari.SistemCarisi.EsikMesaji(
                    "tevsik (459 s. VUK GT)", olcut, esik.Had)).AlanIle("cariId");   // PAKET S2-3: ekran bandı müşteri alanında
        }

        if (Math.Abs(nakitTutar) == 0m) return TevsikSonuc.Gerekmez;

        var onceki = cariId is null
            ? 0m
            : await GunlukNakitToplam(db, tenantId, cariId, an);

        var sonuc = TevsikKurali.Degerlendir(onceki, nakitTutar, islemToplamTutari, esik);

        // Kural kapatılmışsa karar yine hesaplanır ama işlem durdurulmaz.
        // Böylece rapor ve denetim tarafı doğru veriyi görmeye devam eder.
        if (sonuc.Gerekli && esik.Etkin)
            throw new BadRequestException(sonuc.Mesaj!).KodIle(TevsikKurali.RedKodu);   // YASAL-3: istemci reddi kodla tanır (metinde "tevsik" yok)

        return sonuc;
    }

    /// <summary>
    /// Aynı YEREL GÜNDE (Europe/Istanbul), aynı cari ile yapılmış NAKİT
    /// hareketlerin toplamı.
    /// </summary>
    /// <remarks>
    /// ⚠ Gün sınırı UTC değil yereldir (K5). UTC kullanılsaydı İstanbul'da
    /// 00:00–03:00 arası yapılan nakit bir önceki güne düşer ve kümülatif
    /// toplam eksik hesaplanırdı — yani had sessizce atlatılabilirdi.
    ///
    /// İKİ AYRIK KÜME toplanır ve çift sayma YAPISAL olarak imkânsızdır:
    /// <list type="bullet">
    ///   <item><b>Yeni satırlar</b> (<c>NakitTutar != null</c>): nakit payı
    ///   doğrudan okunur. KARMA/kısmi ödemede yalnız nakit bacağı sayılır —
    ///   kart ve havale tevsik edilmiş sayıldığı için toplama GİRMEZ.</item>
    ///   <item><b>Eski satırlar</b> (<c>NakitTutar == null</c>, alan eklenmeden
    ///   önce yazılmış): eski kural aynen — yalnız <c>OdemeTuru == "NAKIT"</c>
    ///   ise <c>|TlTutari|</c> sayılır. Eski KARMA satırların nakit payı
    ///   bilinmediği için bunlar hâlâ görülmez (aşağıdaki nota bakınız).</item>
    /// </list>
    /// Tek bir <c>COALESCE + CASE</c> ifadesi yerine iki basit toplam KULLANILIR:
    /// bileşik ifadenin MySQL'e çevrimi doğrulanmamıştı; iki sorgu bu
    /// belirsizliği ortadan kaldırır ve okunması kolaydır.
    /// </remarks>
    public static async Task<decimal> GunlukNakitToplam(
        KuyumcuDbContext db, string tenantId, string cariId, DateTime an)
    {
        // ⚠ PAKET ŞUBE-KAPSAM-A § 2.6 (K-10): kümülatif MÜKELLEF (kiracı) genelidir.
        // Şubeye kilitli rolde P-04 süzgeci db.Hareketler'i oturum şubesine
        // daraltıyordu → B şubesindeki nakit A'daki kapıya görünmüyordu.
        // Şube süzgeci açıksa sorgu kiracı-geneli SALT OKUMA kapsamında koşar.
        if (db.SubeFiltresiAcik)
            return await SistemKapsami.YasalKumulatifOku(db, tenantId,
                kdb => GunlukNakitToplam(kdb, tenantId, cariId, an));

        // ⚠ PAKET B-3 (D2 · K1): Peşin Müşteri kartının nakit satırları havuza
        // GİRMEZ — kart bir kişi değildir. Boşluk Dogrula'da fail-closed kapanır.
        if (await Modules.Cari.SistemCarisi.MiAsync(db, tenantId, cariId)) return 0m;

        var yerelGun = DateOnly.FromDateTime(Zaman.UtcdenYerele(an));
        var (bas, bit) = Zaman.YerelGunAraligiUtc(yerelGun);

        // ⚠ STORNO ÇİFTİ DIŞLANIR (paket 48 Faz A): hükümsüz kılınmış bir işlem
        // ve onu hükümsüz kılan ters satır birlikte NET SIFIRDIR. Süzgeç
        // olmasaydı, yanlış girilip düzeltilmiş bir nakit işlem müşterinin
        // 459 s. VUK haddini İKİNCİ KEZ doldururdu.
        var gun = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId
                     && h.CariId == cariId
                     && h.Tarih >= bas && h.Tarih < bit)
            .StornoCiftiniDisla(db, tenantId);

        // Küme 1 — nakit payı bilinen satırlar (alan eklendikten sonra yazılanlar).
        var yeni = await gun
            .Where(h => h.NakitTutar != null)
            .SumAsync(h => (decimal?)h.NakitTutar) ?? 0m;

        // Küme 2 — alan eklenmeden önce yazılmış satırlar; eski kural.
        var eski = await gun
            .Where(h => h.NakitTutar == null && h.OdemeTuru == "NAKIT")
            .SumAsync(h => (decimal?)Math.Abs(h.TlTutari)) ?? 0m;

        return yeni + eski;
    }

    // ═══ PAKET YASAL-2 (TB-15) — BÜYÜK İŞLEMİN TAKSİTİ ═══════════════════════════

    /// <summary>
    /// Carinin açık partilerini KAYNAK işlem bilgisiyle döndürür (FIFO sırası).
    /// Kaynak işlem: parti bir fişe aitse fişin TAMAMI (storno çifti dışlanmış
    /// satırların |TL| toplamı), değilse hareketin kendisi. Kaynak gün: yerel gün.
    /// ACILIS / CARI_DUZELTME kaynaklı parti ve defterle açıklanamayan bakiye
    /// KAYNAKSIZDIR (açılış/devir); TAKAS kaynaklı parti kural dışıdır (§ 3.2 T-4).
    /// </summary>
    public static async Task<(List<TaksitKaynagi> Partiler, decimal Kaynaksiz)> TaksitKaynaklari(
        KuyumcuDbContext db, string tenantId, string cariId, bool alacakYonu)
    {
        // Kümülatifle aynı kural (ŞK-A § 2.6): kaynak fiş başka şubede kesilmiş
        // olabilir — şube süzgeci açıksa kiracı geneli SALT OKUMA kapsamında okunur.
        if (db.SubeFiltresiAcik)
            return await SistemKapsami.YasalKumulatifOku(db, tenantId,
                kdb => TaksitKaynaklari(kdb, tenantId, cariId, alacakYonu));

        var (partiler, kaynaksiz) = await Vade.VadeKapisi.AcikPartiler(db, tenantId, cariId, alacakYonu);
        if (partiler.Count == 0) return ([], kaynaksiz);
        var bilgi = await KaynakBilgisi(db, tenantId, partiler);
        return ([.. partiler.Select(p => bilgi[p.HareketId] with { Kalan = p.Kalan })], kaynaksiz);
    }

    /// <summary>
    /// Partilerin KAYNAK işlem bilgisi (tür sınıfı, kaynak tutar, kaynak gün) — kapı
    /// ve TEVSIK_KURTARMA yeniden oynatması AYNI hesabı kullanır.
    /// </summary>
    public static async Task<Dictionary<string, TaksitKaynagi>> KaynakBilgisi(
        KuyumcuDbContext db, string tenantId, IReadOnlyCollection<Vade.AcikParti> partiler)
    {
        var fisler = partiler.Where(p => p.FisId != null).Select(p => p.FisId!).Distinct().ToList();
        var fisToplam = fisler.Count == 0
            ? new Dictionary<string, decimal>()
            : (await db.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == tenantId && h.FisId != null && fisler.Contains(h.FisId))
                .StornoCiftiniDisla(db, tenantId)
                .Select(h => new { h.FisId, h.TlTutari })
                .ToListAsync())
              .GroupBy(x => x.FisId!)
              .ToDictionary(g => g.Key, g => g.Sum(x => Math.Abs(x.TlTutari)));

        var tekIds = partiler.Where(p => p.FisId == null).Select(p => p.HareketId).ToList();
        var tekTutar = tekIds.Count == 0
            ? new Dictionary<string, decimal>()
            : await db.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == tenantId && tekIds.Contains(h.Id))
                .Take(tekIds.Count)   // kimlik başına en çok bir satır — sınır kayıpsızdır
                .ToDictionaryAsync(h => h.Id, h => Math.Abs(h.TlTutari));

        var sonuc = new Dictionary<string, TaksitKaynagi>(partiler.Count);
        foreach (var p in partiler)
        {
            var tur = p.Tur switch
            {
                "ACILIS" or "CARI_DUZELTME" => TaksitKaynak.Kaynaksiz,
                "TAKAS" => TaksitKaynak.Muaf,
                _ => TaksitKaynak.Islem,
            };
            var kaynakTutar = p.FisId is { } f
                ? fisToplam.GetValueOrDefault(f)
                : tekTutar.GetValueOrDefault(p.HareketId);
            sonuc[p.HareketId] = new TaksitKaynagi(p.HareketId, p.Kalan, tur, kaynakTutar,
                DateOnly.FromDateTime(Zaman.UtcdenYerele(p.Tarih)));
        }
        return sonuc;
    }

    /// <summary>
    /// Bu ödemenin taksit kuralı karşısındaki durumu (hesap — istisna ATMAZ).
    /// <paramref name="odeme"/> ödemenin TAMAMI (FIFO'yu tüketen), <paramref name="nakit"/>
    /// onun nakit payı. <paramref name="alacakYonu"/>: true = müşteriden tahsilat.
    /// </summary>
    public async Task<TaksitSonuc> TaksitHesapla(
        KuyumcuDbContext db, string tenantId, string cariId, decimal odeme, decimal nakit, bool alacakYonu)
    {
        var (partiler, kaynaksiz) = await TaksitKaynaklari(db, tenantId, cariId, alacakYonu);
        if (partiler.Count == 0 && kaynaksiz == 0m) return TaksitSonuc.Serbest;
        var dagilim = TaksitKurali.FifoDagit(odeme, kaynaksiz, partiler);
        return TaksitKurali.TaksitDegerlendir(nakit, dagilim, g => kural.EsiklerGunu(g).Had);
    }

    /// <summary>
    /// Önizleme: şu an bu cariden en çok ne kadar nakit alınabilir (null = sınır yok)
    /// ve sınırı doğuran büyük işlem.
    /// </summary>
    public async Task<(decimal? NakitAlinabilir, TaksitKaynagi? Buyuk, decimal? BuyukHad)> TaksitOnizle(
        KuyumcuDbContext db, string tenantId, string cariId, bool alacakYonu)
    {
        var (partiler, kaynaksiz) = await TaksitKaynaklari(db, tenantId, cariId, alacakYonu);
        decimal Had(DateOnly g) => kural.EsiklerGunu(g).Had;
        var alinabilir = TaksitKurali.NakitAlinabilir(kaynaksiz, partiler, Had);
        if (alinabilir is null) return (null, null, null);
        var buyuk = partiler.First(p => p.Kalan > 0m
            && TaksitKurali.Buyuk(new TaksitParti(p.PartiId, p.Kalan, p.Tur, p.KaynakTutar, p.KaynakGun), Had));
        return (alinabilir, buyuk, Had(buyuk.KaynakGun));
    }

    /// <summary>
    /// TB-15 KAPISI — transaction'dan ÖNCE çağrılır (mevcut tevsik deseni). Nakit
    /// payı, büyük (haddi aşan) bir işlemden kalan partiye düşüyorsa 400 atar.
    /// Kural kapalıysa (Tevsik:Etkin=false ya da IslemTutariKuraliEtkin=false) karar
    /// hesaplanır ama işlem durdurulmaz — TEVSIK_KURTARMA işi sonradan bulur.
    /// Dönen sonucun <see cref="TaksitSonuc.Dogrulanamayan"/> &gt; 0 ise çağıran
    /// <c>TEVSIK_TAKSIT_DOGRULANAMADI</c> denetim izini yazar.
    /// </summary>
    public async Task<TaksitSonuc> TaksitDogrula(
        KuyumcuDbContext db, string tenantId, string? cariId, decimal odeme, decimal nakit,
        DateTime an, bool alacakYonu = true, bool dovizMi = false)
    {
        if (cariId is null || Math.Abs(nakit) == 0m) return TaksitSonuc.Serbest;
        var sonuc = await TaksitHesapla(db, tenantId, cariId, odeme, nakit, alacakYonu);
        var esik = kural.EsiklerGunu(Common.YasalParametreServisi.IslemGunu(an));
        if (sonuc.Red && esik.Etkin && esik.IslemTutariKuraliEtkin)
            throw new BadRequestException(TaksitMesaji(sonuc, alacakYonu, dovizMi)).AlanIle("odemeTuru");
        return sonuc;
    }

    private static readonly System.Globalization.CultureInfo Tr = new("tr-TR");

    /// <summary>
    /// Sade red cümlesi — TEK cümle + ne yapmalı (orkestratör 30.09 22:20):
    /// • ham mevzuat kodu ("459", "VUK", "§") görünür metinde YOK — dayanak ekranda
    ///   yalnız title/katlanır ipucunda (Cariler tahsilat kutusu);
    /// • nakit hiç alınamıyorsa "en çok 0,00 TL" YAZILMAZ — "nakit alınamaz" denir;
    /// • döviz tahsilatında rakam TL KARŞILIĞIDIR ve bunu söyler (dovizMi).
    /// </summary>
    public static string TaksitMesaji(TaksitSonuc s, bool alacakYonu, bool dovizMi = false)
    {
        // Tur 2 (hakem [DÜŞÜK]): kaynak her zaman satış değildir (elle veresiye,
        // ALIŞ borcu) → nötr "işlemden"; tür uydurulmaz.
        var ne = alacakYonu ? "kartla ya da havaleyle alın" : "kartla ya da havaleyle ödeyin";
        if (s.IzinliNakit <= 0m)
        {
            var hayir = alacakYonu ? "nakit alınamaz" : "nakit ödenemez";
            return s.BuyukKaynakGun is { } g && s.BuyukHad is { } h
                ? string.Format(Tr, "Bu borç {0:dd.MM.yyyy} tarihli, {1:N2} TL'yi aşan bir işlemden kalıyor; {2}, {3}.",
                    g.ToDateTime(TimeOnly.MinValue), h, hayir, ne)
                : string.Format(Tr, "Bu borç nakit sınırını aşan bir işlemden kalıyor; {0}, {1}.", hayir, ne);
        }
        var birim = dovizMi ? "TL karşılığı" : "TL";
        var fiil = alacakYonu ? "alınabilir" : "ödenebilir";
        return string.Format(Tr, "Bu ödemede en çok {0:N2} {1} nakit {2}; kalanı {3}.", s.IzinliNakit, birim, fiil, ne);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// KAPSAM KAYDI — NAKİT PAYI (Anayasa md.1: kapsadığımız ve kapsamadığımız açıkça yazılır)
//
// KAPANDI (2026-09-14, `hareketler.nakit_tutar`):
//   Karma/kısmi ödemenin NAKİT payı artık kümülatif toplama giriyor.
//   20.000 nakit + 20.000 kart → toplam 20.000 nakit görür; kart payı sayılmaz.
//   Test: TevsikKapiTestleri.KARMA_hareketin_NAKIT_PAYI_kumulatife_KATILIR /
//         KARMA_hareketin_KART_PAYI_nakit_SAYILMAZ.
//
// HÂLÂ AÇIK — her biri bilinçli, hiçbiri "tasarım tercihi" değil:
//   1. ESKİ KARMA SATIRLAR: alan eklenmeden önce yazılmış, `odeme_turu = "KARMA"`
//      olan satırların nakit payı BİLİNMİYOR (`nakit_tutar IS NULL`). Bunlar
//      toplama girmez. Geri-doldurma yalnız kanıtı olan satırlar için yapılır
//      (kasa_hareketleri'nde eşleşen NAKİT satırı olanlar); kanıtı olmayan
//      NULL kalır ve fiilen 0 sayılır.
//   2. TEZGÂH MÜŞTERİSİ (cariId null): kümülatif toplam HİÇ hesaplanmaz
//      (yukarıda Dogrula, "cariId null ise"). Aynı gün aynı kişiye iki ayrı
//      25.000 TL nakit fiş yakalanmaz — kişi kayıt altında değil.
//   3. ALTIN İLE ÖDEME — PAKET B-4 İLE KAPANDI (KARAR-HAS-DEFTERI-2026-09-23,
//      B4-22): tek etiketli `OdemeTuru = "ALTIN"` artık REDDEDİLİR (G-1);
//      eski altın satış fişinin ALTIN bacağıdır. Altın payı nakde SAYILMAZ
//      ama fişte nakit bacak varsa kapı fişin BRÜT toplamıyla
//      (islemToplamTutari) çağrılır — "yarısını altın verelim" haddi DELEMEZ.
//      Trampanın 459 s. Tebliğ karşısındaki niteliği mali müşavir teyidi
//      bekler (§ 7.2 md. 7); kapı yalnız SIKILAŞTI, gevşemedi.
//   4. EŞZAMANLILIK: kapı transaction'dan ÖNCE ve kilitsiz okur. Aynı cariye
//      eşzamanlı iki fiş haddi birlikte delebilir (TOCTOU). Ayrı karar konusu.
//   5. PEŞİN MÜŞTERİ (PAKET B-3 · karar D2 K1/K5 — GEVŞEME AÇIKÇA YAZILIDIR):
//      kiracının sistem carisinin nakit satırları GunlukNakitToplam'a GİRMEZ
//      (kart bir kişi değildir; havuzlansaydı eşik yanlış müşteride patlardı).
//      Karşılığı FAIL-CLOSED: bu kartla nakit payı YA DA fiş toplamı hadde
//      ulaşan işlem Dogrula'da REDDEDİLİR, kural kapalıyken bile. Kalan sınır
//      2. madde ile aynıdır: aynı kişinin aynı gün eşik altı birden çok fişi
//      ölçülmez — kişi kayıt altında değildir.
// ─────────────────────────────────────────────────────────────────────────────
