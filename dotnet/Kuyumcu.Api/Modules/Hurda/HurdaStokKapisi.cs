using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Sayfalar.Hurda;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Hurda;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-20 (Y-10) — HURDA STOK KAPISI (fail-closed)
//
// ⚠ NEDEN VAR: hurda AYRI BİR TABLODA İZLENMEZ. Ölçüldü (2026-09-21):
//   `HurdaAlis` stok satırı açmaz, maliyet partisi açmaz; `stoklar` tablosunda
//   hurda kolonu YOKTUR. Yani elde ne kadar hurda olduğunun TEK kaynağı
//   `hareketler` defteridir: HURDA_ALIS (+has) ve HURDA_SATIS (−has) satırları.
//
//   Bu yüzden "hurda bakiyesi" bir KOLON değil, bir TÜRETMEDİR ve türetmenin
//   TEK yeri burasıdır. İkinci bir yerde türetilseydi (ekran, rapor, satış
//   ucu) biri ayarı, biri şubeyi, biri stornoyu unutur ve iki ekran aynı
//   soruya iki cevap verirdi (UX-S22 ihlali).
//
// ⚠ NEDEN MİGRATION YOK: plan "hurda stok bakiyesi AYRI izleniyorsa kolon
//   gerekir (önce ölçülür)" diyordu. Ölçüm sonucu: ayrı izlenmiyor. Kolon
//   açmak, defterden zaten türetilebilen bir değeri İKİNCİ bir gerçek hâline
//   getirirdi (iki kaynak = sessiz sapma). Rapor § "Migration" bunu yazar.
//
// ⚠ K-13 (KURUCU KARARI BEKLİYOR): fire / ramat farkı — rafineriden dönen
//   has ile gönderilen has arasındaki eksilme — imalat (Katman 1) sınırındadır
//   ve kurucu kararı gelmeden YAZILMAZ. Kapı yapılandırmadan okunur
//   (HurdaAyarlari.FireKaydiAcik) ve VARSAYILANI KAPALIDIR (tutucu davranış,
//   Anayasa md.1: bilinmeyen tahmin edilmez). Ekran gerekçeyi görünür yazar.
// ═════════════════════════════════════════════════════════════════════════════

public static class HurdaStokKapisi
{
    /// <summary>
    /// Hurda has bakiyesini oluşturan hareket türleri — TEK kaynak.
    ///
    /// <para>
    /// ⚠ PAKET P-20b (K-13): <c>TAHLIL_FARKI</c> bu listeye AİTTİR ve dışarıda
    /// bırakılamaz. Hurda satışında <c>HURDA_SATIS</c> yalnız alıcının KABUL
    /// ETTİĞİ hası taşır; bizim kaydımızın kalanı fark bacağındadır. Fark
    /// listeye alınmasaydı, kabul edilmeyen has defterde HÂLÂ ELDEYMİŞ gibi
    /// görünür ve bir dahaki çıkışta "olmayan hurda" satılabilirdi:
    ///   HURDA_ALIS + HURDA_SATIS + TAHLIL_FARKI = elde kalan has.
    /// </para>
    /// </summary>
    public static readonly string[] HurdaTurleri =
        ["HURDA_ALIS", "HURDA_SATIS", Data.TahlilFarkiFabrikasi.Tur];

    /// <summary>
    /// Oturum şubesindeki bir madenin hurda has bakiyesi (gram). Defterden
    /// TÜRETİLİR; storno satırları da (ters işaretli) doğal olarak sayılır.
    ///
    /// <para>
    /// ⚠ ŞUBE SÜZGECİ ELLE YAZILMAZ: tek mekanizma <c>SubeSuzgeci.OturumSubesinde</c>
    /// (P-04 / Y-1). Şube karşılaştırması elle yazılsaydı, bir sonraki hurda
    /// ucunda unutulur ve bir şubenin hurdası diğerinin bakiyesine karışırdı;
    /// denetim.sh GUVENLIK_ELLE_IZOLASYON bunu kırmızıya çevirir.
    /// </para>
    /// </summary>
    public static async Task<decimal> Bakiye(
        KuyumcuDbContext db, AktifKullanici islem, string maden)
    {
        var toplam = await Sorgu(db, islem)
            .Where(h => h.Maden == maden)
            .SumAsync(h => (decimal?)h.HasGrami) ?? 0m;

        // CIFT-BACAK (HATA-3): takasta alınan eski altın da elde duran hurdadır.
        var takas = await TakasSorgu(db, islem)
            .Where(t => t.Maden == maden)
            .SumAsync(t => (decimal?)t.Has) ?? 0m;

        return Math.Round(toplam + takas, 6, MidpointRounding.AwayFromZero);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET CIFT-BACAK (HATA-3, ekip oyu 30.09 19:55) — TAKASIN ESKİ ALTIN BACAĞI.
    //
    // ÖLÇÜLEN KUSUR (senaryo 7.3 / sonda Y1): takasta müşteriden alınan eski
    // altın dükkâna FİZİKEN girer ama hiçbir bakiyede görünmüyordu; TAKAS
    // satırı yalnız NET has (eski − yeni) taşır ve bu liste TAKAS'ı hiç
    // saymıyordu → 2,748 has elde dururken rafineriye satılamadı.
    //
    // KURAL: hurdaya NET has DEĞİL, ALINAN eski altının has'ı girer; verilen
    // yeni ürün zaten stoktan çıkmıştır (stoklar). Eski has, takas yazılırken
    // satırın `takas_eski_has_grami` kolonuna DONMUŞ olarak yazılır ve buradan
    // okunur. ÜRÜN KARTINA BAĞLANMAZ (tur 2, hasım KRİTİK): ürünün gramı/ayarı
    // sonradan düzeltilince geçmiş hurda bakiyesi kaymamalıdır.
    //
    // ⚠ KOLONU BOŞ TAKAS SATIRI SAYILMAZ: kolondan önce yazılmış TAKAS satırının
    //   eski has'ı bilinmez; uydurulmaz (Anayasa md.1) — o satırlar bugünkü
    //   davranışla (hurdaya 0) kalır. Rapor § "Geçmiş TAKAS satırları" sayar.
    // ⚠ Hurda DEFTERİ listesi (DefterSorgu) takas satırını da gösterir (tur 2,
    //   hasım ORTA): kartın "giren" kalemi ile defter satırlarının toplamı aynı
    //   kaynaktan gelir. Bakiye türetmesi (Sorgu) takası AYRICA ekler; Sorgu'ya
    //   katılmaz, yoksa net has da toplanırdı.
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Takas satırının tür dizgesi (SatisModulu.Takas yazar).</summary>
    public const string TakasTuru = "TAKAS";

    /// <summary>Türetilmiş hurda satırı: (tür, maden, has).</summary>
    public sealed class TakasEskiBacak
    {
        public string Maden { get; init; } = null!;
        public decimal Has { get; init; }
    }

    /// <summary>
    /// Verilen hareket sorgusundaki TAKAS satırlarının ESKİ ALTIN bacağı —
    /// tek türetme yeri. Çağıran sorguyu kiracı/şube ile sınırlamış olmalıdır.
    /// </summary>
    public static IQueryable<TakasEskiBacak> TakasEskiBacaklari(
        KuyumcuDbContext db, IQueryable<Hareket> hareketler) =>
        from h in hareketler
        where h.Tur == TakasTuru && h.TakasEskiHasGrami != null
        select new TakasEskiBacak { Maden = h.Maden, Has = h.TakasEskiHasGrami!.Value };

    /// <summary>
    /// Hurda DEFTERİ satırları (tur 2, hasım ORTA): hurda türleri + eski has'ı
    /// bilinen TAKAS satırları. Defter satırında TAKAS'ın has'ı NET değil, ESKİ
    /// has olarak gösterilir (<see cref="DefterHas"/>). Bakiye için KULLANILMAZ.
    /// </summary>
    public static IQueryable<Hareket> DefterSorgu(IQueryable<Hareket> hareketler) =>
        hareketler.Where(h => HurdaTurleri.Contains(h.Tur)
                           || (h.Tur == TakasTuru && h.TakasEskiHasGrami != null));

    /// <summary>Defter satırının hurda has'ı: takasta alınan eski has, diğerlerinde satırın has'ı.</summary>
    public static decimal DefterHas(string tur, decimal hasGrami, decimal? takasEskiHas) =>
        tur == TakasTuru ? takasEskiHas ?? 0m : hasGrami;

    /// <summary>Oturum şubesindeki takasların eski altın bacakları.</summary>
    public static IQueryable<TakasEskiBacak> TakasSorgu(KuyumcuDbContext db, AktifKullanici islem) =>
        TakasEskiBacaklari(db, db.Hareketler.AsNoTracking().OturumSubesinde(islem, SubeGerekli));

    /// <summary>
    /// PAKET B-2 — KİRACI GENELİ hurda kırılımı (tür × maden), altın pozisyonu
    /// kartı için. Şubeye KİLİTLENMEZ: pozisyon işletmenin tamamıdır ve stok
    /// tarafı da kiracı genelidir (şubeye kilitli rolde global ISubeVarlik
    /// süzgeci zaten daraltır). Tür listesi ve kırılım yardımcısı TEK kaynaktan
    /// (bu dosya + P-45 HareketToplami.Coz) — pozisyon servisi listeyi okumaz.
    /// </summary>
    /// <para>
    /// PAKET ŞUBE-KAPSAM-B tur 3 (doğrulayıcı KRİTİK): <paramref name="gorunumSubesi"/>
    /// doluysa kırılım YALNIZ o şubenin satırlarıdır (GorunumSubesi.Daralt — tek
    /// `SubeId ==` yeri); boşsa bugünkü kiracı geneli davranış AYNEN.
    /// </para>
    public static async Task<List<Rapor.HareketKirilimi>> KiraciKirilimi(
        KuyumcuDbContext db, string tenantId, string? gorunumSubesi = null)
    {
        var kirilim = await Rapor.HareketToplami.Coz(db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId && HurdaTurleri.Contains(h.Tur))
            .Daralt(gorunumSubesi));

        // CIFT-BACAK (HATA-3): takasın eski altın bacağı pozisyonda da hurdadır.
        var takas = await TakasEskiBacaklari(db, db.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == tenantId)
                .Daralt(gorunumSubesi))
            .GroupBy(t => t.Maden)
            .Select(g => new { Maden = g.Key, Adet = g.Count(), Has = g.Sum(x => x.Has), Mutlak = g.Sum(x => Math.Abs(x.Has)) })
            .ToListAsync();
        kirilim.AddRange(takas.Select(t => new Rapor.HareketKirilimi(
            TakasTuru, t.Maden, t.Adet, 0m, 0m, t.Has, t.Mutlak)));
        return kirilim;
    }

    /// <summary>Oturum şubesindeki TÜM madenlerin hurda has bakiyesi.</summary>
    public static async Task<List<HurdaBakiyeVeri>> Bakiyeler(
        KuyumcuDbContext db, AktifKullanici islem)
    {
        // ⚠ Tür dizgesi ELLE YAZILMAZ; tek kaynak fabrikanın sabitidir.
        var farkTuru = Data.TahlilFarkiFabrikasi.Tur;

        var satirlar = await Sorgu(db, islem)
            .GroupBy(h => h.Maden)
            .Select(g => new
            {
                Maden = g.Key,
                Has = g.Sum(x => (decimal?)x.HasGrami) ?? 0m,
                Giren = g.Where(x => x.Tur == "HURDA_ALIS").Sum(x => (decimal?)x.HasGrami) ?? 0m,
                Cikan = g.Where(x => x.Tur == "HURDA_SATIS").Sum(x => (decimal?)x.HasGrami) ?? 0m,
                // P-20b: fark AYRI kalemdir — "çıkan"ın içine saklanmaz, yoksa
                // kartın aritmetiği tutmaz (giren − çıkan ≠ bakiye).
                Fark = g.Where(x => x.Tur == farkTuru).Sum(x => (decimal?)x.HasGrami) ?? 0m,
            })
            .ToListAsync();

        // CIFT-BACAK (HATA-3): takasta alınan eski altın GİREN kalemidir; yalnız
        // takası olan madenin satırı da doğar (kart aritmetiği: giren − çıkan + fark).
        var takas = await TakasSorgu(db, islem)
            .GroupBy(t => t.Maden)
            .Select(g => new { Maden = g.Key, Has = g.Sum(x => (decimal?)x.Has) ?? 0m })
            .ToListAsync();
        foreach (var t in takas)
        {
            var i = satirlar.FindIndex(s => s.Maden == t.Maden);
            if (i < 0) satirlar.Add(new { Maden = t.Maden, Has = t.Has, Giren = t.Has, Cikan = 0m, Fark = 0m });
            else satirlar[i] = satirlar[i] with { Has = satirlar[i].Has + t.Has, Giren = satirlar[i].Giren + t.Has };
        }

        return satirlar
            .Select(s => new HurdaBakiyeVeri(
                s.Maden ?? Kuyumcu.Core.Domain.Maden.Altin,
                Kuyumcu.Core.Domain.Maden.Ad(s.Maden ?? Kuyumcu.Core.Domain.Maden.Altin),
                Math.Round(s.Has, 6, MidpointRounding.AwayFromZero),
                Math.Round(s.Giren, 6, MidpointRounding.AwayFromZero),
                Math.Round(-s.Cikan, 6, MidpointRounding.AwayFromZero),
                Math.Round(s.Fark, 6, MidpointRounding.AwayFromZero)))
            .OrderBy(s => s.Maden, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Hurda satırlarının TEK sorgusu — kiracı global filtreden, şube kapıdan.</summary>
    public static IQueryable<Hareket> Sorgu(KuyumcuDbContext db, AktifKullanici islem) =>
        db.Hareketler.AsNoTracking()
            .OturumSubesinde(islem, SubeGerekli)
            .Where(h => HurdaTurleri.Contains(h.Tur));

    public const string SubeGerekli = "Hurda işlemleri için şube bağlamı gerekli.";

    /// <summary>
    /// PAKET ZO-ISLEM-A — HURDA İŞLEM ŞUBESİNİN ÇÖZÜMÜ (UX-S21/S22).
    ///
    /// <para>
    /// ÖLÇÜLEN KUSUR (25.09, kuyumcu_net): şubesi atanmamış MÜDÜR, kiracıda TEK
    /// aktif şube varken kullanıcı kartında "Müdür · Merkez Şube" görüyor (tek
    /// seçenek otomatik yazılır — UX-S22), ama /hurda 400 "şube bağlamı gerekli"
    /// veriyordu: aynı ekranda aynı sorunun iki cevabı.
    /// </para>
    /// <para>
    /// KURAL: oturumun şubesi varsa O kullanılır (değişmez). Yoksa ve rol TÜM
    /// ŞUBELER ayrıcalıklıysa (<see cref="SubeYetkisi.TumSubelerRolleri"/> —
    /// SAHIP/MUDUR/MUHASEBE: veri kapsamı zaten kiracının tamamı) ve kiracıda
    /// TAM OLARAK BİR aktif şube varsa, işlem o şubede yürür — seçilecek ikinci
    /// bir şube yoktur, soru anlamsızdır. Aksi hâlde (birden çok şube, şube hiç
    /// yok, şubeye kilitli rol) oturum OLDUĞU GİBİ döner ve kapı 400 verir
    /// (fail-closed; tahmin yok).
    /// </para>
    /// <para>
    /// ⚠ ŞUBEYE KİLİTLİ ROL (TEZGAHTAR) ÇÖZÜLMEZ: global süzgeç onun için
    /// token şubesiyle kurulur; şubesiz tezgâhtara şube uydurulsaydı bakiye
    /// süzgeç yüzünden 0 görünür, hurda "yok" sanılırdı (sessiz boşluk).
    /// </para>
    /// </summary>
    public static async Task<AktifKullanici> OturumSubesiCoz(KuyumcuDbContext db, AktifKullanici islem)
    {
        if (!string.IsNullOrEmpty(islem.SubeId)) return islem;
        if (!SubeYetkisi.TumSubeler(islem.Rol)) return islem;

        var aktifSubeler = await db.Subeler.AsNoTracking()
            .Where(s => s.TenantId == islem.TenantId && s.Aktif)
            .Select(s => s.Id)
            .Take(2)
            .ToListAsync();

        return aktifSubeler.Count == 1 ? islem with { SubeId = aktifSubeler[0] } : islem;
    }

    /// <summary>
    /// ÇIKIŞ KAPISI (fail-closed): elde olmayan hurda satılamaz.
    ///
    /// <para>
    /// ⚠ NEDEN KAPI, NEDEN SADECE EKRAN UYARISI DEĞİL: hurdanın stok satırı
    /// yoktur; "yetersiz stok" diye 400 veren hiçbir mevcut kapı bu yoldan
    /// geçmez. Kapı olmasaydı 100 g alınan hurdadan 400 g satılabilir, has
    /// defteri EKSİYE düşer ve kâr-zarar bu eksiyi "kazanç" sayardı.
    /// </para>
    /// </summary>
    public static async Task DogrulaCikis(
        KuyumcuDbContext db, AktifKullanici islem, string maden, decimal cikisHas)
    {
        // Q10-2 (canlı ölçüm 24.09): eski karşılaştırma "bakiye + 0,000001" PAYI
        // taşıyordu; decimal kesindir, pay gerekmez. Bakiye 9,16458 iken 9,164581
        // çıkışı 200 alıyor ve has defteri −0,000001'e DÜŞÜYORDU (fail-closed
        // ihlali). İstenen, deftere YAZILACAĞI kesinlikte (6 hane — Yuvarla6)
        // karşılaştırılır; sınır DAHİLDİR (= bakiye geçer, bir hane fazlası geçmez).
        // Sıfır kontrolü de yuvarlanmış değerle: 0,0000004 deftere 0 yazılırdı.
        var istenen = Math.Round(cikisHas, 6, MidpointRounding.AwayFromZero);
        if (istenen <= 0m)
            throw new BadRequestException("Hurda satışında has gram sıfırdan büyük olmalıdır.");

        var bakiye = await Bakiye(db, islem, maden);
        if (istenen > bakiye)
            throw new BadRequestException(
                $"Bu şubede yeterli hurda yok: elde {bakiye:0.######} has gram "
                + $"{Kuyumcu.Core.Domain.Maden.Ad(maden).ToLowerInvariant()} var, "
                + $"{cikisHas:0.######} has gram çıkış isteniyor. "
                + "Önce hurda alışını girin ya da çıkış gramını düşürün.");
    }

    /// <summary>
    /// PAKET P-20b (K-13/2) — TAHLİL FARKI KAPISI: alıcının kabul ettiği has
    /// doğrulanır ve İŞARETLİ fark döner (<c>kabul − bizim kaydımız</c>).
    ///
    /// <para>
    /// ⚠ NEDEN AYRI KAPI: fark hesabı iki yerde (satış ucu ve düzeltme ucu)
    /// yapılır. İkisinde ayrı ayrı yazılsaydı biri yuvarlamayı, diğeri işareti
    /// başka türlü yorumlar ve aynı belge iki farklı bakiye üretirdi.
    /// </para>
    /// </summary>
    public static decimal FarkHesapla(decimal bizimKaydimizHas, decimal aliciKabulHas)
    {
        if (aliciKabulHas <= 0m)
            throw new BadRequestException(
                "Alıcının kabul ettiği has sıfırdan büyük olmalıdır: kabul edilen has sıfırsa "
                + "ortada bir satış değil, iade edilmiş bir sevkiyat vardır.");

        return Math.Round(aliciKabulHas - bizimKaydimizHas, 6, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// K-13 KAPISI: fire / ramat farkı bu sürümde YAZILMAZ. Yapılandırmadan
    /// okunur, varsayılanı KAPALI; açık değilken fire alanı gönderilirse istek
    /// sessizce yutulmaz, gerekçesiyle REDDEDİLİR (Anayasa md.1).
    /// </summary>
    public static void DogrulaFire(HurdaAyarlari ayarlar, decimal? fireGrami)
    {
        if (fireGrami is null or 0m) return;
        if (ayarlar.FireKaydiAcik) return;

        throw new BadRequestException(FireKapaliMesaji);
    }

    /// <summary>
    /// Ekranda ve yanıtta AYNI metin görünür (UX-S31: tek kavram, tek ad).
    /// Metin bir düzeltme yolu VAAT ETMEZ; bugün yapılamadığını söyler.
    /// </summary>
    public const string FireKapaliMesaji =
        "Bu sürümde eritme firesi kaydedilmez; imalat kapsam dışıdır (K-13 kararı). "
        + "Rafineride eritmeden doğan kayıp bu programda bir kayıt türü DEĞİLDİR. "
        + "Alıcının kabul ettiği has bizim kaydımızdan farklı çıktıysa bu bir fire "
        + "değil TAHLİL FARKIDIR ve hurda satışında ayrı bacak olarak "   // Q10-2: K8 — "(MUTABAKAT)" jargonu çıkarıldı (BASİT ekranda da görünür)
        + "gerekçesiyle yazılır.";
}

// ⚠ Bakiye satırının tipi Kuyumcu.Contracts.Sayfalar.Hurda.HurdaBakiyeVeri'dir:
//    TEK tanım hem sunucu imzasını hem istemciyi bağlar (P-03 sözleşme kilidi).
