using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Maliyet;
using Kuyumcu.Api.Modules.Rapor;
using Kuyumcu.Api.Modules.Satis;
using Kuyumcu.Api.Modules.Urun;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET RAPOR-4 — rapor kalanları + KAR-HIZ
//   RD01  aylık "alış-satış nakit farkı" = net satış − alış − hurda (Panel ile tek tanım)
//   RD02  ürün kartındaki alış maliyeti, parti maliyeti yoksa kâra girer (kaynak "kart maliyeti");
//         kart 0/boşsa fail-closed (HESAPLANAMADI); has bacağı kartla hesaplanmaz
//   RD-KH01..04  ürün bazlı kârın ay ön-toplamı (KarUrunOnToplam) ESKİ yolla AYNI sonucu verir
//         (rastgele + sınır veri; önbellek isabeti; geçersizleştirme; pencere dışı parti satırı)
// ESKİ YOL: paket öncesi KarZararServisi.UrunBazli gövdesi AYNEN aşağıda (EskiKarYolu) — yalnız
//   (a) bağlam parametre oldu, (b) özel yardımcılar açık adlarıyla çağrıldı, (c) RD02 kararı gereği
//   satır düzeyinde kart maliyeti dolgusu eklendi (kartMaliyeti=false iken birebir eski davranış).
// ═════════════════════════════════════════════════════════════════════════════

internal static class EskiKarYolu
{
    private static decimal R2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);
    private static decimal R4(decimal d) => Math.Round(d, 4, MidpointRounding.AwayFromZero);
    private static bool Altin(string? maden) => HareketToplami.MadenKodu(maden) == Maden.Altin;
    private static async Task<Dictionary<string, decimal>> Kurlar(KuyumcuDbContext db, string tenantId, IEnumerable<string> idler)
    {
        var kume = idler.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        if (kume.Count == 0) return [];
        return await db.KurSnapshots.AsNoTracking()
            .Where(k => k.TenantId == tenantId && kume.Contains(k.Id))
            .ToDictionaryAsync(k => k.Id, k => k.AltinHasFiyati);
    }

    public static async Task<object> EskiUrunBazli(KuyumcuDbContext db,
        AktifKullanici islem, DateOnly baslangic, DateOnly bitis, string? subeId, int limit, int ofset = 0, bool kartMaliyeti = true)
    {
        if (limit < 1 || limit > 500)
            throw new BadRequestException("limit 1 ile 500 arasında olmalı");
        // PAKET OLCEK-YUK t2 (tasarım hakemi KRİTİK, UX-S29): ekran "daha fazla göster" ile sonraki sayfayı ister.
        if (ofset < 0)
            throw new BadRequestException("ofset 0 ya da daha büyük olmalı");

        var (bas, _) = Zaman.YerelGunAraligiUtc(baslangic);
        var (_, sonBit) = Zaman.YerelGunAraligiUtc(bitis);

        var temel = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId
                     && h.Tarih >= bas && h.Tarih < sonBit
                     && (h.Tur == "SATIS" || h.Tur == "SATIS_IADE")
                     && h.UrunId != null);
        if (!string.IsNullOrEmpty(subeId)) temel = temel.Where(h => h.SubeId == subeId);

        var satislar = await temel.StornoCiftiniDisla(db, islem.TenantId)
            .Select(h => new { h.Id, h.Tur, h.UrunId, h.TlTutari, h.HasGrami, h.KurSnapshotId, h.Adet, h.Maden })
            .ToListAsync();

        var tuketimSorgu = db.Set<StokPartiTuketimi>().AsNoTracking()
            .Where(t => t.TenantId == islem.TenantId && t.Tarih >= bas && t.Tarih < sonBit);
        if (!string.IsNullOrEmpty(subeId)) tuketimSorgu = tuketimSorgu.Where(t => t.SubeId == subeId);
        var hamTuketimler = await tuketimSorgu
            .Select(t => new { t.CikisHareketId, t.UrunId, t.Adet, t.BirimMaliyetTl, t.BirimMaliyetHas })
            .ToListAsync();
        // RAPOR-4 başvuru: kart maliyeti satır düzeyinde (parti maliyeti yoksa ürün kartındaki alış maliyeti > 0).
        var kartlar = kartMaliyeti
            ? await db.Urunler.AsNoTracking().Where(u => u.TenantId == islem.TenantId && u.AlisMaliyeti != null && u.AlisMaliyeti > 0m)
                .ToDictionaryAsync(u => u.Id, u => u.AlisMaliyeti!.Value)
            : new Dictionary<string, decimal>();
        var tuketimler = hamTuketimler.Select(t => new
        {
            t.CikisHareketId, t.UrunId, t.Adet,
            BirimMaliyetTl = t.BirimMaliyetTl ?? (kartlar.TryGetValue(t.UrunId, out var km) ? km : (decimal?)null),
            BirimMaliyetHas = t.BirimMaliyetTl is null ? null : t.BirimMaliyetHas,
        }).ToList();

        var kimlikler = satislar.Select(s => s.Id).ToHashSet();
        // PAKET KAR-RAPOR: ürün satırında da kâr YALNIZ maliyeti tam bilinen satışlardan.
        var bilinenKimlikler = KarZararServisi.MaliyetiBilinenler(
            satislar.Select(h => (h.Id, h.Adet)),
            tuketimler.Where(t => t.CikisHareketId != null && kimlikler.Contains(t.CikisHareketId))
                      .Select(t => (t.CikisHareketId, t.Adet, t.BirimMaliyetTl)));
        // MADEN-2: ürün satırının has bacağı da dönemle AYNI has-bilinen kuralıyla (tek yardımcı).
        var hasBilinenKimlikler = KarZararServisi.HasMaliyetiBilinenler(
            satislar.Where(h => Altin(h.Maden)).Select(h => (h.Id, h.Adet)),
            tuketimler.Where(t => t.CikisHareketId != null && kimlikler.Contains(t.CikisHareketId))
                      .Select(t => (t.CikisHareketId, t.Adet, t.BirimMaliyetTl, t.BirimMaliyetHas)));
        var maliyetler = tuketimler
            .Where(t => t.CikisHareketId != null && kimlikler.Contains(t.CikisHareketId))
            .GroupBy(t => t.UrunId)
            .ToDictionary(g => g.Key, g => new
            {
                Tl  = g.Where(x => x.BirimMaliyetTl is not null && bilinenKimlikler.Contains(x.CikisHareketId!))
                       .Sum(x => x.BirimMaliyetTl!.Value * x.Adet),
                Has = g.Where(x => x.BirimMaliyetHas is not null && hasBilinenKimlikler.Contains(x.CikisHareketId!))
                       .Sum(x => x.BirimMaliyetHas!.Value * x.Adet),
                BilinenAdet    = g.Where(x => x.BirimMaliyetTl is not null).Sum(x => Math.Abs(x.Adet)),
                BilinmeyenAdet = g.Where(x => x.BirimMaliyetTl is null).Sum(x => Math.Abs(x.Adet)),
            });

        // PAKET OLCEK-YUK (TB-19): ürün adları artık kiracının TÜM kataloğundan okunmaz
        // (50.000 ürünlü kiracıda her istekte 50.000 satır belleğe alınıyordu, ölçüldü);
        // sıralama ve limit adlardan bağımsızdır, adlar yalnız SAYFADAKİ ürünler için
        // çekilir (aşağıda `with` ile doldurulur). Yanıt sözleşmesi ve alan sırası aynıdır.
        var kurlar = await Kurlar(db, islem.TenantId, satislar.Select(h => h.KurSnapshotId));
        var kursuzToplam = 0;

        var hamSatirlar = satislar
            .GroupBy(s => s.UrunId!)
            .Select(g =>
            {
                var brut = g.Where(x => x.Tur == "SATIS").Sum(x => x.TlTutari);
                var iade = g.Where(x => x.Tur == "SATIS_IADE").Sum(x => Math.Abs(x.TlTutari));
                var hasilat = brut - iade;

                var kursuz = 0;
                var hasilatHas =
                    KarZararServisi.HasCevir(g.Where(x => x.Tur == "SATIS").Select(x => (x.TlTutari, x.KurSnapshotId)), kurlar, ref kursuz)
                  - KarZararServisi.HasCevir(g.Where(x => x.Tur == "SATIS_IADE").Select(x => (Math.Abs(x.TlTutari), x.KurSnapshotId)), kurlar, ref kursuz);
                kursuzToplam += kursuz;

                var m = maliyetler.GetValueOrDefault(g.Key);
                var maliyet = m?.Tl ?? 0m;
                var maliyetHas = m?.Has ?? 0m;

                var bilinen = g.Where(x => bilinenKimlikler.Contains(x.Id)).ToList();
                var eksik   = g.Where(x => !bilinenKimlikler.Contains(x.Id)).ToList();
                var bilinenHasilat = bilinen.Where(x => x.Tur == "SATIS").Sum(x => x.TlTutari)
                                   - bilinen.Where(x => x.Tur == "SATIS_IADE").Sum(x => Math.Abs(x.TlTutari));
                // MADEN-2: has bacağı kendi bilinen kümesinden (has maliyeti eksik satış has kârına 0 maliyetle girmez).
                var hasBilinen = g.Where(x => hasBilinenKimlikler.Contains(x.Id)).ToList();
                var hasEksikAdet = g.Count(x => !hasBilinenKimlikler.Contains(x.Id));
                var yoksay = 0;
                var bilinenHasilatHas =
                    KarZararServisi.HasCevir(hasBilinen.Where(x => x.Tur == "SATIS").Select(x => (x.TlTutari, x.KurSnapshotId)), kurlar, ref yoksay)
                  - KarZararServisi.HasCevir(hasBilinen.Where(x => x.Tur == "SATIS_IADE").Select(x => (Math.Abs(x.TlTutari), x.KurSnapshotId)), kurlar, ref yoksay);
                var hasDurum = KarZararServisi.KarDurumuBelirle(hasBilinen.Count, hasEksikAdet);
                var durum = KarZararServisi.KarDurumuBelirle(bilinen.Count, eksik.Count);
                // KAR-RAPOR-2 kuruş kuralı (dönemle AYNI): kâr = yuvarlanmış bilinen hasılat − yuvarlanmış maliyet.
                var urunHesaplanamadi = durum == KarZararServisi.KarDurumlari.Hesaplanamadi;
                var hasilatR = R2(hasilat);
                var bilinenHasilatR = R2(bilinenHasilat);
                var maliyetR = R2(maliyet);
                decimal? kar = urunHesaplanamadi ? null : bilinenHasilatR - maliyetR;
                // MADEN-AYRIMI (tasarım hakemi t1 KRİTİK): has bacağı yalnız ALTIN ürünü taşır.
                // Gümüş/platin satırında hasilatHas/maliyetHas/karHas null — ekran "—" basar,
                // satır dönem "Altın kârı (has)" toplamına da katılmaz (Olc ile aynı küme).
                var urunMadeni = HareketToplami.MadenKodu(g.First().Maden);
                var altinUrun = Altin(urunMadeni);
                var hasHesaplanamadi = urunHesaplanamadi || !altinUrun || hasDurum == KarZararServisi.KarDurumlari.Hesaplanamadi;
                decimal? karHasU = hasHesaplanamadi ? null : R4(bilinenHasilatHas) - R4(maliyetHas);
                return new
                {
                    urunId = g.Key,
                    kod = (string?)null,
                    ad  = (string?)null,
                    satisAdedi = g.Count(x => x.Tur == "SATIS"),
                    iadeAdedi  = g.Count(x => x.Tur == "SATIS_IADE"),
                    brutHasilatTl = Math.Round(brut, 2, MidpointRounding.AwayFromZero),
                    iadeTl = Math.Round(iade, 2, MidpointRounding.AwayFromZero),
                    hasilatTl = hasilatR,
                    // KAR-RAPOR-2: maliyeti hiç bilinmeyen satırda maliyet "0" DEĞİL null (bilinmiyor);
                    // KISMI satırda YALNIZ bilinen satışların maliyeti — bilinenHasilatTl ile eşleşir.
                    maliyetTl = urunHesaplanamadi ? (decimal?)null : maliyetR,
                    bilinenHasilatTl = bilinenHasilatR,
                    karTl = kar,
                    karDurumu = durum,
                    maliyetiEksikSatisAdedi = eksik.Count(x => x.Tur == "SATIS"),
                    maliyetiEksikTutarTl = hasilatR - bilinenHasilatR,
                    // HAS BACAĞI ASLA GİZLENMEZ: her ALTIN satırı TL kârıyla birlikte HAS kârını taşır (MADEN-AYRIMI: diğer madenlerde null).
                    maden = urunMadeni,
                    hasilatHas = altinUrun ? Math.Round(hasilatHas, 4, MidpointRounding.AwayFromZero) : (decimal?)null,
                    maliyetHas = hasHesaplanamadi ? (decimal?)null : R4(maliyetHas),
                    karHas = karHasU,
                    // MADEN-2: has bacağının kendi durumu (altın dışı satırda null) ve has maliyeti eksik satış adedi.
                    hasKarDurumu = altinUrun ? hasDurum : null,
                    hasMaliyetiEksikSatisAdedi = altinUrun ? g.Count(x => x.Tur == "SATIS" && !hasBilinenKimlikler.Contains(x.Id)) : 0,
                    karMarjiYuzde = kar is { } kk && bilinenHasilatR > 0m ? Math.Round(kk / bilinenHasilatR * 100m, 2, MidpointRounding.AwayFromZero) : (decimal?)null,
                    maliyetiBilinmeyenAdet = Math.Round(m?.BilinmeyenAdet ?? 0m, 3, MidpointRounding.AwayFromZero),
                    guvenilirlik = RaporGuvenilirligi.Hesapla(
                        m?.BilinenAdet ?? 0m, m?.BilinmeyenAdet ?? 0m, kursuz),
                };
            })
            // Kârı hesaplanamayan ürün sona (null büyük değildir).
            .OrderByDescending(x => x.karTl.HasValue).ThenByDescending(x => x.karTl)
            // Kararlı sıra: aynı kârlı ürünler sayfalar arasında yer değiştirmesin (ofset ile örtüşme 0).
            .ThenBy(x => x.urunId, StringComparer.Ordinal)
            .Skip(ofset)
            .Take(limit)
            .ToList();

        var sayfaUrunIdleri = hamSatirlar.Select(x => x.urunId).ToList();
        var urunAdlari = new Dictionary<string, (string? Kod, string? Ad)>();
        if (sayfaUrunIdleri.Count > 0)
            foreach (var u in await db.Urunler.AsNoTracking()
                         .Where(u => u.TenantId == islem.TenantId && sayfaUrunIdleri.Contains(u.Id))
                         .Select(u => new { u.Id, u.Kod, u.Ad })
                         .Take(sayfaUrunIdleri.Count)
                         .ToListAsync())
                urunAdlari[u.Id] = (u.Kod, u.Ad);
        var satirlar = hamSatirlar
            .Select(x => urunAdlari.TryGetValue(x.urunId, out var ka)
                ? x with { kod = ka.Kod, ad = ka.Ad }
                : x)
            .ToList();

        var toplamUrunSayisi = satislar.Select(s => s.UrunId).Distinct().Count();
        var bilinen = maliyetler.Values.Sum(x => x.BilinenAdet);
        var bilinmeyen = maliyetler.Values.Sum(x => x.BilinmeyenAdet);

        return new
        {
            baslangic = baslangic.ToString("yyyy-MM-dd"),
            bitis = bitis.ToString("yyyy-MM-dd"),
            subeId,
            urunSayisi = satirlar.Count,
            limit,
            ofset,
            // UX-S29: dönemde satılan TOPLAM ürün sayısı sunucudan gelir (ekran türetmez);
            // sunucu sınırına değildiyse ekran "daha fazlası var" der ve sonraki sayfayı ister.
            toplamUrunSayisi,
            dahaFazlasiVar = toplamUrunSayisi > ofset + satirlar.Count,
            maliyetiBilinenAdet = Math.Round(bilinen, 3, MidpointRounding.AwayFromZero),
            maliyetiBilinmeyenAdet = Math.Round(bilinmeyen, 3, MidpointRounding.AwayFromZero),
            kuruBulunamayanSatis = kursuzToplam,
            guvenilirlik = RaporGuvenilirligi.Hesapla(bilinen, bilinmeyen, kursuzToplam),
            satirlar,
        };
    }

}

[Collection("veritabani")]
public sealed class RaporDortTestleri(TestVeritabani vt)
{
    private static decimal R2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);
    private static JsonElement J(object o) => JsonDocument.Parse(JsonSerializer.Serialize(o)).RootElement;
    private static DateOnly Bugun => Zaman.YerelBugun();

    private static DateTime Utc(DateOnly gun, int dakika) => Zaman.YerelGunAraligiUtc(gun).Bas.AddMinutes(dakika);

    private static string Hareket(KuyumcuDbContext db, Kiraci k, string tur, string urunId, string subeId, DateTime tarih,
        decimal tl, decimal? adet, string kurId, string maden = Maden.Altin, string? stornoKaynak = null)
    {
        var id = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
        {
            Id = id, TenantId = k.TenantId, SubeId = subeId, Tur = tur, Tarih = tarih, TlTutari = tl,
            HasGrami = 1.5m, Maden = maden, ParaBirimi = "TRY", KurSnapshotId = kurId, UrunId = urunId,
            KullaniciId = k.KullaniciId, Olusturuldu = DateTime.UtcNow, Adet = adet, StornoKaynakId = stornoKaynak,
        });
        return id;
    }

    private static void Tuketim(KuyumcuDbContext db, Kiraci k, string cikisId, string urunId, string subeId,
        decimal adet, decimal? birimTl, decimal? birimHas, DateTime tarih)
    {
        db.Set<StokPartiTuketimi>().Add(new StokPartiTuketimi
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, PartiId = "RD-PARTI", CikisHareketId = cikisId,
            UrunId = urunId, SubeId = subeId, Adet = adet, BirimMaliyetTl = birimTl, BirimMaliyetHas = birimHas, Tarih = tarih,
        });
    }

    private static List<string> Urunler(KuyumcuDbContext db, Kiraci k, int adet, Random r, string onEk)
    {
        var now = DateTime.UtcNow;
        var idler = new List<string>();
        for (var i = 0; i < adet; i++)
        {
            var id = Kimlik.Ulid();
            idler.Add(id);
            db.Urunler.Add(new Urun
            {
                Id = id, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"{onEk}-{i}-{id[^6..]}", Ad = $"{onEk} {i}",
                AyarId = "K22", Milyem = 916m, GramAgirlik = 5m, TasAgirlik = 0m, HasGrami = 4.58m, IscilikTl = 0m,
                Aktif = true, Olusturuldu = now, Guncellendi = now,
                // RD02 kararı: bir kısmının kartında alış maliyeti var, bir kısmında 0 (bilinmiyor sayılır), kalanı boş.
                AlisMaliyeti = (i % 3) switch { 0 => 2_500m + i, 1 => (i % 2 == 0 ? 0m : null), _ => null },
            });
        }
        db.SaveChanges();
        return idler;
    }

    /// <summary>
    /// Rastgele defter: 14 ay geriye yayılı satış/iade; K (maliyeti tam), N (boş maliyetli satır),
    /// U (satır yok / adet karşılanmamış), kursuz, gümüş, ikinci şube, pencere DIŞI tarihli parti satırı, storno.
    /// </summary>
    private static List<string> Tohumla(KuyumcuDbContext db, Kiraci k, int seed, int satisSayisi, out string ikinciSube)
    {
        var r = new Random(seed);
        var urunler = Urunler(db, k, 14, r, $"RD{seed}");
        ikinciSube = "RD-SUBE2-" + Kimlik.Ulid()[^8..];
        var satislar = new List<string>();
        for (var n = 0; n < satisSayisi; n++)
        {
            var ay = r.Next(0, 14);
            var ayBasi = new DateOnly(Bugun.Year, Bugun.Month, 1).AddMonths(-ay);
            var gunSayisi = ay == 0 ? Bugun.Day : DateTime.DaysInMonth(ayBasi.Year, ayBasi.Month);
            var gun = ayBasi.AddDays(r.Next(0, gunSayisi));
            var tarih = Utc(gun, r.Next(0, 24 * 60));
            if (tarih > DateTime.UtcNow) tarih = DateTime.UtcNow.AddMinutes(-5);
            var ui = r.Next(urunler.Count);
            var urun = urunler[ui];
            var iade = r.Next(0, 5) == 0;
            var tl = Math.Round((decimal)r.Next(100_000, 900_000) / 100m, 2);
            decimal? adet = r.Next(0, 10) == 0 ? null : r.Next(1, 3);   // defter adedi işaretsiz (yön TL'de)
            var kur = r.Next(0, 12) == 0 ? "RD-KUR-YOK" : k.KurId;
            var maden = ui % 5 == 4 ? Maden.Gumus : Maden.Altin;        // maden ürünün ayarından: ürün başına TEK
            var sube = r.Next(0, 6) == 0 ? ikinciSube : k.SubeId;
            var id = Hareket(db, k, iade ? "SATIS_IADE" : "SATIS", urun, sube, tarih, iade ? -tl : tl, adet, kur, maden);
            satislar.Add(id);
            var mutlakAdet = Math.Abs(adet ?? 1m);
            var isaret = iade ? -1m : 1m;
            var tTarih = r.Next(0, 25) == 0 ? tarih.AddDays(-40) : tarih;   // pencere/ay dışı parti satırı (sınır)
            switch (r.Next(0, 6))
            {
                case 0: break;                                                     // U: satır yok
                case 1:                                                            // U: adet karşılanmamış
                    Tuketim(db, k, id, urun, sube, isaret * mutlakAdet / 2m, 1_000m, 0.15m, tTarih); break;
                case 2:                                                            // N: bir satır boş maliyetli
                    Tuketim(db, k, id, urun, sube, isaret * mutlakAdet / 2m, 1_200m + n, 0.17m, tTarih);
                    Tuketim(db, k, id, urun, sube, isaret * mutlakAdet / 2m, null, null, tTarih); break;
                case 3:                                                            // N: tamamı boş
                    Tuketim(db, k, id, urun, sube, isaret * mutlakAdet, null, null, tTarih); break;
                default:                                                           // K: tam (has bazen boş)
                    Tuketim(db, k, id, urun, sube, isaret * mutlakAdet, 1_500m + n % 7, n % 4 == 0 ? null : 0.2m, tTarih); break;
            }
        }
        db.SaveChanges();
        // Storno: üç satış (biri kapanmış bir aydan) — çift dışlanır.
        foreach (var kaynak in satislar.Where((_, i) => i % 17 == 3).Take(3))
        {
            var h = db.Hareketler.AsNoTracking().Single(x => x.Id == kaynak);
            Hareket(db, k, h.Tur, h.UrunId!, h.SubeId, DateTime.UtcNow.AddMinutes(-1), -h.TlTutari, h.Adet, h.KurSnapshotId, h.Maden, kaynak);
        }
        db.SaveChanges();
        return urunler;
    }

    /// <summary>Eski yolun her alanı yeni yolda AYNI (yeni yolun ek alanları — kartMaliyetliAdet — serbest).</summary>
    private static void Ayni(JsonElement eski, JsonElement yeni, string baglam)
    {
        foreach (var p in eski.EnumerateObject())
        {
            Assert.True(yeni.TryGetProperty(p.Name, out var y), $"{baglam}: yeni yanıtta '{p.Name}' yok");
            if (p.Name == "satirlar")
            {
                var e = p.Value.EnumerateArray().ToList();
                var n = y.EnumerateArray().ToList();
                Assert.True(e.Count == n.Count, $"{baglam}: satır sayısı {e.Count} ≠ {n.Count}");
                for (var i = 0; i < e.Count; i++)
                    foreach (var a in e[i].EnumerateObject())
                        Assert.True(n[i].TryGetProperty(a.Name, out var b) && b.GetRawText() == a.Value.GetRawText(),
                            $"{baglam}: satır {i} '{a.Name}' eski={a.Value.GetRawText()} yeni={(n[i].TryGetProperty(a.Name, out var bb) ? bb.GetRawText() : "YOK")}");
            }
            else
                Assert.True(p.Value.GetRawText() == y.GetRawText(), $"{baglam}: '{p.Name}' eski={p.Value.GetRawText()} yeni={y.GetRawText()}");
        }
    }

    private async Task Karsilastir(Kiraci k, string? ikinciSube, string baglam)
    {
        var ayBasi = new DateOnly(Bugun.Year, Bugun.Month, 1);
        // Takvime bağlı kırmızıyı önler: "13 ay önceki ayın 5'i → bugün" ayın ~11'inden sonra rapor dönemi
        // sınırını (RaporDonemKurali.EnCokGun, iki uç dahil) aşıyordu. Başlangıç sınırın izin verdiği en erken
        // güne sıkıştırılır — kısmi baş + kapanmış tam aylar + içinde bulunulan ay niyeti her gün korunur.
        var enErken = Bugun.AddDays(-(Kuyumcu.Core.Domain.Girdi.RaporDonemKurali.EnCokGun - 1));
        var uzunBas = ayBasi.AddMonths(-13).AddDays(4) < enErken ? enErken : ayBasi.AddMonths(-13).AddDays(4);
        var araliklar = new (DateOnly Bas, DateOnly Bit)[]
        {
            (uzunBas, Bugun),                                          // kısmi baş + 12 tam ay + içinde bulunulan ay (≤ dönem sınırı)
            (ayBasi.AddMonths(-10), ayBasi.AddMonths(-2).AddDays(-1)), // yalnız kapanmış tam aylar
            (ayBasi.AddMonths(-6).AddDays(9), ayBasi.AddMonths(-1).AddDays(14)), // iki kısmi uç
            (Bugun.AddDays(-20), Bugun),                              // canlı (≤ 2 ay)
        };
        foreach (var (bas, bit) in araliklar)
            foreach (var sube in new[] { null, k.SubeId, ikinciSube })
                foreach (var (limit, ofset) in new[] { (500, 0), (3, 2) })
                {
                    using var db = vt.Olustur(k);
                    var eski = J(await EskiKarYolu.EskiUrunBazli(db, k.Islem, bas, bit, sube, limit, ofset));
                    var yeni = J(await new KarZararServisi(db).UrunBazli(k.Islem, bas, bit, sube, limit, ofset));
                    Ayni(eski, yeni, $"{baglam} {bas:yyyy-MM-dd}..{bit:yyyy-MM-dd} şube={sube ?? "tümü"} limit={limit} ofset={ofset}");
                }
    }

    // ── KAR-HIZ eşdeğerlik ─────────────────────────────────────────────────

    [Fact]
    public async Task RD_KH01_Ay_ontoplami_eski_yolla_ayni_sonucu_verir_rastgele_ve_sinir_veri_onbellek_isabeti_dahil()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "RD-KH01");
        KarUrunOnToplam.KiraciGuvenlikPayi[k.TenantId] = TimeSpan.Zero;   // isabet yolunu sına (tohum önbellekten ÖNCE)
        string ikinci;
        using (var db = vt.Olustur(k)) Tohumla(db, k, 4101, 260, out ikinci);
        await Karsilastir(k, ikinci, "ilk (önbellek kuruluyor)");
        Assert.True(KarUrunOnToplam.OnbellektekiAySayisi(k.TenantId) >= 8, "kapanmış aylar önbelleğe alınmalı");
        await Karsilastir(k, ikinci, "ikinci (önbellek isabeti)");
        KarUrunOnToplam.KiraciGuvenlikPayi.TryRemove(k.TenantId, out _);
    }

    [Fact]
    public async Task RD_KH02_Kapanmis_aya_dusen_yeni_satir_storno_ve_kart_degisimi_onbellegi_bayatlatmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "RD-KH02");
        KarUrunOnToplam.KiraciGuvenlikPayi[k.TenantId] = TimeSpan.Zero;
        string ikinci; List<string> urunler;
        using (var db = vt.Olustur(k)) urunler = Tohumla(db, k, 4202, 120, out ikinci);
        await Karsilastir(k, ikinci, "önce");
        Assert.True(KarUrunOnToplam.OnbellektekiAySayisi(k.TenantId) >= 8);

        var ayBasi = new DateOnly(Bugun.Year, Bugun.Month, 1);
        using (var db = vt.Olustur(k))
        {
            // (1) Kapanmış aya düşen YENİ satış (oluşturuldu = şimdi) — maliyeti tam, kârı çok büyük → sıralamanın başı.
            var tarih = Utc(ayBasi.AddMonths(-5).AddDays(10), 600);
            var id = Hareket(db, k, "SATIS", urunler[1], k.SubeId, tarih, 999_999m, 1m, k.KurId);
            Tuketim(db, k, id, urunler[1], k.SubeId, 1m, 100m, 0.01m, tarih);
            db.SaveChanges();
        }
        await Karsilastir(k, ikinci, "kapanmış aya yeni satış");
        using (var db = vt.Olustur(k))
        {
            // (2) Kapanmış aydaki bir satışın stornosu (storno satırı bugün).
            var (bas, _) = Zaman.YerelGunAraligiUtc(ayBasi.AddMonths(-3));
            var (bit, _) = Zaman.YerelGunAraligiUtc(ayBasi.AddMonths(-2));
            var h = db.Hareketler.AsNoTracking().Where(x => x.TenantId == k.TenantId && x.Tur == "SATIS" && x.StornoKaynakId == null
                    && x.Tarih >= bas && x.Tarih < bit).OrderBy(x => x.Id).First();
            Hareket(db, k, h.Tur, h.UrunId!, h.SubeId, DateTime.UtcNow, -h.TlTutari, h.Adet, h.KurSnapshotId, h.Maden, h.Id);
            db.SaveChanges();
        }
        await Karsilastir(k, ikinci, "kapanmış aydan storno");
        using (var db = vt.Olustur(k))
        {
            // (3) Kart maliyeti değişimi — önbellek kartı istek anında uygular.
            foreach (var u in db.Urunler.Where(x => x.TenantId == k.TenantId && urunler.Contains(x.Id)).ToList())
                u.AlisMaliyeti = u.AlisMaliyeti is null ? 1_111m : null;
            db.SaveChanges();
        }
        await Karsilastir(k, ikinci, "kart maliyeti değişti");
        using (var db = vt.Olustur(k))
        {
            // (4) KISMİ BAŞ parçasına (önbellekte ay olarak değil, aralık toplamında duran) düşen yeni satış.
            var tarih = Utc(ayBasi.AddMonths(-13).AddDays(10), 700);
            var id = Hareket(db, k, "SATIS", urunler[2], k.SubeId, tarih, 777_777m, 1m, k.KurId);
            Tuketim(db, k, id, urunler[2], k.SubeId, 1m, 10m, 0.01m, tarih);
            // (5) İçinde bulunulan aya yeni satış (her istekte canlı parça).
            var bugun = DateTime.UtcNow.AddMinutes(-1);
            var id2 = Hareket(db, k, "SATIS", urunler[3], k.SubeId, bugun, 555_555m, 1m, k.KurId);
            Tuketim(db, k, id2, urunler[3], k.SubeId, 1m, 10m, 0.01m, bugun);
            // (6) YALNIZ bu ay satılan yeni ürün (kapanmış kısımda hiç yok → yalnız canlı parçada).
            var yeni = Urunler(db, k, 1, new Random(1), "KH02-YENI")[0];
            var id3 = Hareket(db, k, "SATIS", yeni, k.SubeId, bugun, 333_333m, 1m, k.KurId);
            Tuketim(db, k, id3, yeni, k.SubeId, 1m, 10m, 0.01m, bugun);
            db.SaveChanges();
        }
        await Karsilastir(k, ikinci, "kısmi baş parçası + bu ay");
        KarUrunOnToplam.KiraciGuvenlikPayi.TryRemove(k.TenantId, out _);
    }

    [Fact]
    public async Task RD_KH03_Ontoplam_kart_maliyetsiz_eski_yolla_birebir_ve_kart_kapali_baslangic_ayni()
    {
        // Kart maliyeti hiç yokken yeni yol PAKET ÖNCESİ yolla (kartMaliyeti=false) BİREBİR aynıdır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "RD-KH03");
        KarUrunOnToplam.KiraciGuvenlikPayi[k.TenantId] = TimeSpan.Zero;
        using (var db = vt.Olustur(k))
        {
            Tohumla(db, k, 4303, 150, out _);
            foreach (var u in db.Urunler.Where(x => x.TenantId == k.TenantId).ToList()) u.AlisMaliyeti = null;
            db.SaveChanges();
        }
        var ayBasi = new DateOnly(Bugun.Year, Bugun.Month, 1);
        for (var tur = 0; tur < 2; tur++)
        {
            using var db = vt.Olustur(k);
            var eski = J(await EskiKarYolu.EskiUrunBazli(db, k.Islem, ayBasi.AddMonths(-12), Bugun, null, 500, 0, kartMaliyeti: false));
            var yeni = J(await new KarZararServisi(db).UrunBazli(k.Islem, ayBasi.AddMonths(-12), Bugun, null, 500, 0));
            Ayni(eski, yeni, $"kartsız tur {tur}");
        }
        KarUrunOnToplam.KiraciGuvenlikPayi.TryRemove(k.TenantId, out _);
    }

    private async Task TekKarsilastir(Kiraci k, DateOnly bas, DateOnly bit, string? sube, string baglam)
    {
        using var db = vt.Olustur(k);
        var eski = J(await EskiKarYolu.EskiUrunBazli(db, k.Islem, bas, bit, sube, 500, 0));
        var yeni = J(await new KarZararServisi(db).UrunBazli(k.Islem, bas, bit, sube, 500, 0));
        Ayni(eski, yeni, baglam);
    }

    [Fact]
    public async Task RD_KH05_Pencere_disi_ve_sube_uyumsuz_parti_satiri_olan_ay_canli_hesaplanir()
    {
        // Sınır verisi (deterministik): kapanmış TAM ayda bir satış; parti satırı (a) aralık BAŞLANGICINDAN önce
        // tarihli, (b) BAŞKA şubede. Eski yol pencere/şube süzgeciyle satırı görmez → satış "eksik"; ay kovası
        // kimlikle bağlar → "bilinen" derdi. Ön-toplam bu ayı CANLI hesaplamalı (fail-closed).
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "RD-KH05");
        KarUrunOnToplam.KiraciGuvenlikPayi[k.TenantId] = TimeSpan.Zero;
        var ayBasi = new DateOnly(Bugun.Year, Bugun.Month, 1);
        var bas = ayBasi.AddMonths(-6);                       // aralık tam ayla başlar
        using (var db = vt.Olustur(k))
        {
            var r = new Random(4505);
            var urunler = Urunler(db, k, 4, r, "KH05");
            foreach (var u in db.Urunler.Where(x => x.TenantId == k.TenantId)) u.AlisMaliyeti = null;
            db.SaveChanges();
            var t1 = Utc(bas.AddDays(3), 600);
            var a = Hareket(db, k, "SATIS", urunler[0], k.SubeId, t1, 5_000m, 1m, k.KurId);
            Tuketim(db, k, a, urunler[0], k.SubeId, 1m, 1_000m, 0.1m, Utc(bas.AddDays(-5), 600));      // pencere DIŞI
            var b2 = Hareket(db, k, "SATIS", urunler[1], k.SubeId, Utc(bas.AddMonths(2).AddDays(4), 600), 6_000m, 1m, k.KurId);
            Tuketim(db, k, b2, urunler[1], "RD-BASKA-SUBE", 1m, 1_000m, 0.1m, Utc(bas.AddMonths(2).AddDays(4), 600)); // şube uyumsuz
            var c = Hareket(db, k, "SATIS", urunler[2], k.SubeId, Utc(bas.AddMonths(3).AddDays(1), 600), 7_000m, 1m, k.KurId);
            Tuketim(db, k, c, urunler[2], k.SubeId, 1m, 2_000m, 0.2m, Utc(bas.AddMonths(3).AddDays(1), 600));     // normal
            db.SaveChanges();
        }
        for (var tur = 0; tur < 2; tur++)   // 1. tur önbelleği kurar, 2. tur isabet eder
        {
            await TekKarsilastir(k, bas, Bugun, null, $"pencere dışı tur {tur}");
            await TekKarsilastir(k, bas, Bugun, k.SubeId, $"şube süzgeçli tur {tur}");
        }
        Assert.True(KarUrunOnToplam.OnbellektekiAySayisi(k.TenantId) >= 5);
        KarUrunOnToplam.KiraciGuvenlikPayi.TryRemove(k.TenantId, out _);
    }

    [Fact]
    public async Task RD_KH06_Aralik_toplami_isabetinden_sonra_kismi_bas_parcasina_dusen_satir_gorulur()
    {
        // Kısmi baş parçası (ay ortasında başlayan aralığın ilk ayı) Aylar'da DEĞİL, aralık toplamında saklanır;
        // oraya düşen yeni satır aralık toplamını bayatlatmalıdır (Dogrula: her iz sürümü artırır).
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "RD-KH06");
        KarUrunOnToplam.KiraciGuvenlikPayi[k.TenantId] = TimeSpan.Zero;
        var ayBasi = new DateOnly(Bugun.Year, Bugun.Month, 1);
        var bas = ayBasi.AddMonths(-5).AddDays(9);
        List<string> urunler;
        using (var db = vt.Olustur(k)) urunler = Tohumla(db, k, 4606, 80, out _);
        await TekKarsilastir(k, bas, Bugun, null, "önce");
        await TekKarsilastir(k, bas, Bugun, null, "isabet");
        using (var db = vt.Olustur(k))
        {
            var t = Utc(bas.AddDays(3), 500);
            var id = Hareket(db, k, "SATIS", urunler[5], k.SubeId, t, 888_888m, 1m, k.KurId);
            Tuketim(db, k, id, urunler[5], k.SubeId, 1m, 10m, 0.01m, t);
            db.SaveChanges();
        }
        await TekKarsilastir(k, bas, Bugun, null, "kısmi başa yeni satış");
        KarUrunOnToplam.KiraciGuvenlikPayi.TryRemove(k.TenantId, out _);
    }

    [Fact]
    public void RD_KH04_Kova_kurali_MaliyetiBilinenler_ile_ayni_siniflar()
    {
        // Saf: K (tam) · N (boş satırlı, karşılanmış) · U (satırsız / karşılanmamış); tolerans sınırı 0,0005.
        var kur = new Dictionary<string, decimal> { ["KUR"] = 6_000m };
        var h = new List<OnToplamHareket>
        {
            new("h1", "SATIS", "u", "s", 1_000m, 1m, "KUR", 1m, Maden.Altin),
            new("h2", "SATIS", "u", "s", 2_000m, 1m, "KUR", 2m, Maden.Altin),
            new("h3", "SATIS_IADE", "u", "s", -500m, 1m, "YOK", -1m, Maden.Altin),
            new("h4", "SATIS", "u", "s", 700m, 1m, "KUR", 1m, Maden.Altin),
            new("h5", "SATIS", "u", "s", 300m, 1m, "KUR", 1m, Maden.Altin),
        };
        var t = new List<OnToplamTuketim>
        {
            new("t1", "h1", "u", "s", 1m, 400m, 0.1m, DateTime.UtcNow),
            new("t2", "h2", "u", "s", 1m, 500m, 0.1m, DateTime.UtcNow),
            new("t3", "h2", "u", "s", 1m, null, null, DateTime.UtcNow),
            new("t4", "h3", "u", "s", -1m, 400m, 0.1m, DateTime.UtcNow),
            new("t5", "h4", "u", "s", 0.9995m, 300m, 0.1m, DateTime.UtcNow),   // 0,9995 + 0,0005 ≥ 1 → karşılanmış
        };
        var k = Assert.Single(KarUrunOnToplam.Kovala(h, t, kur)).Value;
        Assert.Equal(3, k.KAdet);                // h1, h3, h4
        Assert.Equal(1, k.NAdet);                // h2
        Assert.Equal(1, k.UAdet);                // h5 (satırsız)
        Assert.Equal(1_000m - 500m + 700m, k.KTl);
        Assert.Equal(400m - 400m + 0.9995m * 300m, k.KMal);
        Assert.Equal(1m, k.NBosAdet);
        Assert.Equal(1, k.Kursuz);               // h3
        Assert.Null(new UrunKovasi { UAdet = 1 }.Kar(5m));                     // hiç bilinen yok → null
        Assert.Equal(R2(1_200m + 2_000m) - R2(299.85m + 500m + 100m), k.Kar(100m));   // kart: N bilinir
        Assert.Equal(R2(1_200m) - R2(299.85m), k.Kar(0m));   // kart 0 = bilinmiyor
    }

    // ── VERİ 1: ürün kartındaki alış maliyeti ───────────────────────────────

    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) => new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static async Task<string> KartliUrun(KuyumcuDbContext db, Kiraci k, string kod, decimal? alisMaliyeti)
    {
        await new UrunServisi(db).Olustur(new UrunOlusturDto
        {
            Tip = "ZIYNET", Kod = kod, Ad = "Kartlı " + kod, AyarId = "K22", GramAgirlik = 5m, AlisMaliyeti = alisMaliyeti,
            AcilisStogu = new AcilisStoguDto { Adet = 3m },
        }, k.Islem);
        return (await db.Urunler.AsNoTracking().SingleAsync(u => u.TenantId == k.TenantId && u.Kod == kod)).Id;
    }

    private static Task Sat(KuyumcuDbContext db, Kiraci k, string urunId, decimal tutar) =>
        Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = urunId, Adet = 1m, BirimFiyatTl = tutar, OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

    [Fact]
    public async Task RD02_Kart_alis_maliyeti_parti_maliyeti_yokken_kara_girer_kaynak_etiketiyle_has_failclosed()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "RD02-kart");
        using var db = vt.Olustur(k);
        var kartli = await KartliUrun(db, k, "RD02-K", 18_000m);
        await Sat(db, k, kartli, 25_000m);   // nakit sınırının altında

        // Kök neden kanıtı: açılış stoğu partisi maliyetsiz (SayimFazla) — satışın parti satırında maliyet YOK.
        var satirlar = await db.Set<StokPartiTuketimi>().AsNoTracking().Where(t => t.TenantId == k.TenantId && t.UrunId == kartli).ToListAsync();
        Assert.NotEmpty(satirlar);
        Assert.All(satirlar, t => Assert.Null(t.BirimMaliyetTl));

        var servis = new KarZararServisi(db);
        var d = J(await servis.Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("TAM", d.GetProperty("karDurumu").GetString());
        Assert.Equal(7_000m, d.GetProperty("karTl").GetDecimal());            // 25.000 − 18.000 (kart maliyeti)
        Assert.Equal(18_000m, d.GetProperty("maliyetTl").GetDecimal());
        Assert.Equal(1m, d.GetProperty("kartMaliyetliAdet").GetDecimal());
        Assert.Equal(JsonValueKind.Null, d.GetProperty("karHas").ValueKind);  // has maliyeti kartla BİLİNMEZ

        var u = J(await servis.UrunBazli(k.Islem, Bugun, Bugun, null, 50));
        var satir = Assert.Single(u.GetProperty("satirlar").EnumerateArray());
        Assert.Equal(7_000m, satir.GetProperty("karTl").GetDecimal());
        Assert.Equal("TAM", satir.GetProperty("karDurumu").GetString());
        Assert.Equal(1m, satir.GetProperty("kartMaliyetliAdet").GetDecimal());
        Assert.Equal(JsonValueKind.Null, satir.GetProperty("karHas").ValueKind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task RD02b_Kart_maliyeti_bos_ya_da_sifirsa_kar_HESAPLANAMADI_kalir(int? kart)
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), $"RD02b-{kart}");
        using var db = vt.Olustur(k);
        var urun = await KartliUrun(db, k, $"RD02b-{kart}", kart);
        await Sat(db, k, urun, 25_000m);
        var d = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("HESAPLANAMADI", d.GetProperty("karDurumu").GetString());
        Assert.Equal(JsonValueKind.Null, d.GetProperty("karTl").ValueKind);
        Assert.Equal(0m, d.GetProperty("kartMaliyetliAdet").GetDecimal());
    }

    // ── VERİ 2: alış-satış nakit farkı ─────────────────────────────────────

    [Fact]
    public async Task RD01_Aylik_nakit_farki_net_satis_eksi_alis_eksi_hurda()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "RD01-fark");
        using var db = vt.Olustur(k);
        var simdi = DateTime.UtcNow.AddMinutes(-2);
        Hareket(db, k, "SATIS", k.UrunId, k.SubeId, simdi, 50_000m, 1m, k.KurId);
        Hareket(db, k, "SATIS_IADE", k.UrunId, k.SubeId, simdi, -7_500m, 1m, k.KurId);
        Hareket(db, k, "ALIS", k.UrunId, k.SubeId, simdi, -20_000m, 1m, k.KurId);
        Hareket(db, k, "HURDA_ALIS", k.UrunId, k.SubeId, simdi, -3_000m, null, k.KurId);
        db.SaveChanges();
        var r = J(await TestKurulum.RaporServisi(db).AylikOzet(Bugun.Year, Bugun.Month, k.Islem));
        Assert.Equal(42_500m, r.GetProperty("netSatisTl").GetDecimal());
        // İade varken fark NET satıştan: 50.000 − 7.500 − 20.000 − 3.000 (eski: brüt 50.000 − 20.000 = 30.000).
        Assert.Equal(19_500m, r.GetProperty("satisAlisFarkiTl").GetDecimal());
        Assert.Equal(3_000m, r.GetProperty("hurdaAlisTl").GetDecimal());
    }

    // ── RAPOR-4 tur 2 (KAR-HIZ): kart maliyetlerinin (maliyet, id) sayfalı okuması — eşit maliyetler sayfa sınırında kaybolmaz ──
    [Fact]
    public async Task RD_KH08_Kart_maliyetleri_maliyet_id_sayfasiyla_eksiksiz_okunur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "RD-KH08");
        var now = DateTime.UtcNow;
        var beklenen = new Dictionary<string, decimal>();
        using (var db = vt.Olustur(k))
        {
            for (var i = 0; i < 23; i++)
            {
                var id = Kimlik.Ulid();
                // Eşit maliyetler (3 değer) sayfa sınırına (7) denk gelir; 0 ve boş kart okunmaz (fail-closed).
                decimal? kart = (i % 5) switch { 0 => 0m, 1 => null, 2 => 1_000m, 3 => 2_000m, _ => 3_000m };
                db.Urunler.Add(new Urun
                {
                    Id = id, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"KH08-{i}-{id[^6..]}", Ad = $"KH08 {i}", AyarId = "K22",
                    Milyem = 916m, GramAgirlik = 5m, TasAgirlik = 0m, HasGrami = 4.58m, IscilikTl = 0m, Aktif = true,
                    Olusturuldu = now, Guncellendi = now, AlisMaliyeti = kart,
                });
                if (kart is > 0m) beklenen[id] = kart.Value;
            }
            db.SaveChanges();
        }
        var adaylar = Enumerable.Range(0, 2_001).Select(i => $"aday-{i}").ToList();   // > 2.000 → sayfalı yol
        using (var db = vt.Olustur(k))
        {
            KarZararServisi.KartSayfasiTest.Value = 7;
            try
            {
                var okunan = await new KarZararServisi(db).KartMaliyetleriToplu(k.TenantId, adaylar);
                Assert.Equal(beklenen.Count, okunan.Count);
                foreach (var (id, m) in beklenen) Assert.Equal(m, okunan[id]);
            }
            finally { KarZararServisi.KartSayfasiTest.Value = null; }
        }
    }

    // ── RAPOR-4 tur 2 (hasım DÜŞÜK, VERİ 1): stok değerlemesi kâr hesabıyla AYNI kuralı kullanır ──
    [Fact]
    public async Task RD03_Stok_degerlemesi_kart_maliyetini_kar_ile_ayni_kuralla_kullanir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "RD03");
        var now = DateTime.UtcNow;
        string Urun(KuyumcuDbContext db, string kod, decimal? kart)
        {
            var id = Kimlik.Ulid();
            db.Urunler.Add(new Urun
            {
                Id = id, TenantId = k.TenantId, Tip = "ZIYNET", Kod = kod + id[^6..], Ad = kod, AyarId = "K22", Milyem = 916m,
                GramAgirlik = 5m, TasAgirlik = 0m, HasGrami = 4.58m, IscilikTl = 0m, Aktif = true,
                Olusturuldu = now, Guncellendi = now, AlisMaliyeti = kart,
            });
            return id;
        }
        void Parti(KuyumcuDbContext db, string urunId, decimal adet, decimal? birimTl, decimal? birimHas)
            => db.Set<StokPartisi>().Add(new StokPartisi
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, UrunId = urunId, Kaynak = "SAYIM_FAZLA",
                AdetGiren = adet, AdetKalan = adet, HasGramBirim = 4.58m, BirimMaliyetTl = birimTl, BirimMaliyetHas = birimHas,
                GirisTarihi = now, Olusturuldu = now,
            });
        using (var db = vt.Olustur(k))
        {
            Parti(db, Urun(db, "KARTLI", 18_000m), 8m, null, null);          // partide maliyet YOK, kart 18.000 → kartla bilinir
            Parti(db, Urun(db, "KARTSIZ", null), 2m, null, null);            // kart boş → bilinmez (fail-closed)
            Parti(db, Urun(db, "KARTSIFIR", 0m), 1m, null, null);            // kart 0 → bilinmez
            Parti(db, Urun(db, "PARTILI", 9_999m), 3m, 1_000m, 0.4m);       // parti maliyeti ÖNCELİKLİ (kart yok sayılır)
            db.SaveChanges();
        }
        using (var db = vt.Olustur(k))
        {
            var r = J(await new KarZararServisi(db).StokDegerlemesi(k.Islem, null));
            Assert.Equal(11m, r.GetProperty("maliyetliAdet").GetDecimal());
            Assert.Equal(3m, r.GetProperty("maliyetsizAdet").GetDecimal());
            Assert.Equal(8m, r.GetProperty("kartMaliyetliAdet").GetDecimal());
            Assert.Equal(144_000m, r.GetProperty("kartMaliyetliTl").GetDecimal());
            Assert.Equal(147_000m, r.GetProperty("stokMaliyetiTl").GetDecimal());   // 8 × 18.000 + 3 × 1.000
            Assert.True(r.GetProperty("stokMaliyetiKismi").GetBoolean());
            // Has bacağı kartla HESAPLANMAZ: kartlı 8 adet has maliyetsiz sayılır (kâr motoruyla aynı).
            Assert.Equal(3m, r.GetProperty("hasMaliyetliAdet").GetDecimal());
            Assert.Equal(11m, r.GetProperty("hasMaliyetsizAdet").GetDecimal());
        }
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// RAPOR-4 tur 2 (hasım DÜŞÜK): KarUrunOnToplam önbelleği kapanmış ayı "olusturuldu > damga" ile doğrular. Bu varsayım
// SATIS/SATIS_IADE yazan HER yolun Tarih ile Olusturuldu'yu AYNI "şimdi" değeriyle yazmasına dayanır. Mimari bekçi:
// kaynakta bu türü yazan her Hareket ilklendiricisi Tarih ve Olusturuldu'yu aynı değişkenle verir ve o değişken
// DateTime.UtcNow'dur. Geriye tarihli bir satış yolu eklenirse bu test kırmızı olur → önbellek doğrulaması gözden geçirilir.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class RaporDortMimariTestleri
{
    [Fact]
    public void RD_KH07_Satis_ve_iade_yazan_her_yol_Tarih_ve_Olusturuldu_ayni_simdi()
    {
        var kok = Path.Combine(CssKaynak.Kok(), "Kuyumcu.Api");
        var dosyalar = Directory.EnumerateFiles(kok, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"))
            .ToList();
        var turRx = new System.Text.RegularExpressions.Regex("\\bTur\\s*=\\s*\"SATIS(_IADE)?\"\\s*,");
        var bulunan = 0;
        var ihlaller = new List<string>();
        foreach (var f in dosyalar)
        {
            var metin = File.ReadAllText(f);
            foreach (System.Text.RegularExpressions.Match m in turRx.Matches(metin))
            {
                bulunan++;
                var yeniler = System.Text.RegularExpressions.Regex.Matches(metin[..m.Index], @"new\s+(?:[\w.]+\.)?Hareket\b");
                var bas = yeniler.Count == 0 ? -1 : yeniler[^1].Index;
                var bit = metin.IndexOf("}", m.Index, StringComparison.Ordinal);
                var yer = $"{Path.GetFileName(f)}:{metin[..m.Index].Count(c => c == '\n') + 1}";
                if (bas < 0 || bit < 0 || m.Index - bas > 1_500) { ihlaller.Add($"{yer}: ilklendirici bulunamadı"); continue; }
                var govde = metin[bas..bit];
                var tarih = System.Text.RegularExpressions.Regex.Match(govde, "\\bTarih\\s*=\\s*(\\w+)\\s*,");
                var olus = System.Text.RegularExpressions.Regex.Match(govde, "\\bOlusturuldu\\s*=\\s*(\\w+)\\s*,?");
                if (!tarih.Success || !olus.Success || tarih.Groups[1].Value != olus.Groups[1].Value)
                {
                    ihlaller.Add($"{yer}: Tarih='{tarih.Groups[1].Value}' Olusturuldu='{olus.Groups[1].Value}'");
                    continue;
                }
                var degisken = tarih.Groups[1].Value;
                var tanimlar = System.Text.RegularExpressions.Regex.Matches(metin[..m.Index], $"\\bvar\\s+{degisken}\\s*=\\s*([^;]+);");
                if (tanimlar.Count == 0 || tanimlar[^1].Groups[1].Value.Trim() != "DateTime.UtcNow")
                    ihlaller.Add($"{yer}: '{degisken}' DateTime.UtcNow değil");
            }
        }
        // Tarama kendini doğrular: bugünkü 5 yazma yolu (SatisModulu ×3, SatisModulu.P21 ×2) bulunmalı.
        Assert.True(bulunan >= 5, $"SATIS/SATIS_IADE ilklendiricisi bulunamadı ({bulunan}) — tarama bozuk");
        Assert.True(ihlaller.Count == 0, "Önbellek doğrulaması (olusturuldu damgası) bu yollar için güvenli değil:\n" + string.Join("\n", ihlaller));
    }
}
