using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Kuyumcu.Contracts.Sayfalar.NakitSiniri;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Tevsik;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-2 (TB-16) — GEÇ FARK EDİLEN NAKİT SINIRI AŞIMI: bul · göster ·
// bildirim dilekçesi hazırla · kapanışı kaydet.
//
// DAYANAK (şartname § 3.1, birincil kaynak):
//   M-2  459 s. VUK GT §4.1.1 — aynı gün aynı kişi toplamı (GUNLUK_KUMULATIF).
//   M-1  459 s. VUK GT §4.1.2 — işlem toplamı / kısım kısım ödeme (ISLEM_TUTARI, TAKSIT).
//   M-6  VUK mük. md.355 — ödemeyi YAPAN, 5 iş günü içinde kendiliğinden idareye
//        bildirirse ödeme yapan adına ceza kesilmez → yön (TAHSILAT/ODEME).
// DOĞRULANAMAYAN (§ 3.2): bildirimin biçimi/kanalı (T-1) → dilekçe "TASLAK — mali
//   müşavirinizle kontrol edin" damgalı serbest metindir; arife (T-2) iş günü sayılır.
//
// ⚠ TEK MOTOR (ADR-0016 ruhu): cari × gün nakit toplamı TevsikKapisi.GunlukNakitToplam
//   ile hesaplanır; ikinci formül yazılmaz. Taksit ihlali VadeKapisi FIFO'sunun
//   yeniden oynatılması + TaksitKurali ile bulunur (kapıyla aynı kural).
// ⚠ KAPSAM: tarama kiracı GENELİDİR (IsZamanlayici SistemKapsami.Kiraci'da koşar;
//   şube süzgeci kapalı). ŞK-A § 2.6.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Taramanın çıktısı: yeni yazılan satırlar + süresi dolan sayısı.</summary>
public sealed record TevsikTaramaSonucu(IReadOnlyList<TevsikIhlali> Yeni, int SuresiDolan, int IncelenenGun);

/// <summary>"Bildirildi olarak işaretle" gövdesi.</summary>
public sealed class TevsikBildirildiDto
{
    /// <summary>Vergi dairesine bildirildiği yerel gün (zorunlu, gelecekte olamaz).</summary>
    public DateOnly? BildirimTarihi { get; set; }

    /// <summary>Dilekçe/evrak kayıt numarası (isteğe bağlı).</summary>
    [MaxLength(60)] public string? EvrakNo { get; set; }

    /// <summary>Gerekçe — en az 20 karakter.</summary>
    [MaxLength(500)] public string? Gerekce { get; set; }
}

/// <summary>"Geçersiz" gövdesi — yalnız SAHİP.</summary>
public sealed class TevsikGecersizDto
{
    /// <summary>Gerekçe — en az 20 karakter (ör. "havale yapılmış, kasaya yanlış NAKİT girilmiş").</summary>
    [MaxLength(500)] public string? Gerekce { get; set; }
}

public static class TevsikIhlalTarayici
{
    /// <summary>Geriye bakılan takvim günü (5 iş gününü her zaman kapsar).</summary>
    public const int TaramaGun = 10;

    /// <summary>Bildirim görünür rolleri (şartname § 4.2).</summary>
    public const string BildirimRolleri = "SAHIP,MUHASEBE";

    /// <summary>Kuyumcunun ÖDEDİĞİ satır türleri (md.355 muafiyeti bu yöndedir).</summary>
    public static bool OdemeYonu(string tur) => tur is "ALIS" or "HURDA_ALIS" or "SATIS_IADE" or "EMANET_CIKIS";

    private static decimal Nakit(decimal? nakitTutar, string? odemeTuru, decimal tlTutari) =>
        nakitTutar ?? (odemeTuru == "NAKIT" ? Math.Abs(tlTutari) : 0m);

    /// <summary>
    /// Son <see cref="TaramaGun"/> takvim gününü tarar, yeni ihlalleri yazar, süresi
    /// geçen AÇIK kayıtları SURE_DOLDU yapar. Aynı (cari, gün, tür) ikinci kez yazılmaz
    /// (UNIQUE — veritabanı karar verir).
    /// </summary>
    /// <param name="isGunu">Günün TEVSIK_BILDIRIM_IS_GUNU değeri (YASAL-1 tablosu).</param>
    public static async Task<TevsikTaramaSonucu> Tara(
        KuyumcuDbContext db, string tenantId, DateTime anUtc, TevsikKurali kural,
        Func<DateOnly, int> isGunu, TakvimAyarlari? takvim, CancellationToken iptal = default)
    {
        // FAIL-CLOSED (tur 2, K-16): tarama KİRACI GENELİ bağlam ister. Şube süzgeci
        // açık bir bağlamda aday satırlar yalnız o şubeden gelir; başka şubedeki
        // nakit SESSİZCE kaçardı. Üretimde iş SistemKapsami.Kiraci'da (tüm şubeler)
        // koşar; elle tetikleme SAHİP/MÜDÜR'dedir. Başka bir yol açılırsa burada durur.
        if (db.SubeFiltresiAcik)
            throw new InvalidOperationException(
                "Nakit sınırı taraması kiracı geneli bağlam ister (şube süzgeci açık); eksik tarama yapılmaz.");

        var bugun = DateOnly.FromDateTime(Zaman.UtcdenYerele(anUtc));
        var adaylar = new Dictionary<(string Cari, DateOnly Gun, string Tur), TevsikIhlali>();

        for (var i = TaramaGun - 1; i >= 0; i--)
        {
            iptal.ThrowIfCancellationRequested();
            var gun = bugun.AddDays(-i);
            var esik = kural.EsiklerGunu(gun);
            var (bas, bit) = Zaman.YerelGunAraligiUtc(gun);

            var satirlar = await db.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == tenantId && h.CariId != null && h.Tarih >= bas && h.Tarih < bit)
                .StornoCiftiniDisla(db, tenantId)
                .Select(h => new { h.Id, h.CariId, h.Tur, h.FisId, h.NakitTutar, h.OdemeTuru, h.TlTutari })
                .ToListAsync(iptal);

            // ── GUNLUK_KUMULATIF (§4.1.1) — toplam TEK MOTORDAN ──
            foreach (var g in satirlar.GroupBy(h => h.CariId!))
            {
                var nakitli = g.Where(h => Nakit(h.NakitTutar, h.OdemeTuru, h.TlTutari) > 0m).ToList();
                if (nakitli.Count == 0) continue;
                var toplam = await GunToplami(db, tenantId, g.Key, bas);
                if (toplam <= esik.Had) continue;
                var odeme = nakitli.Where(h => OdemeYonu(h.Tur)).Sum(h => Nakit(h.NakitTutar, h.OdemeTuru, h.TlTutari));
                var tahsil = nakitli.Where(h => !OdemeYonu(h.Tur)).Sum(h => Nakit(h.NakitTutar, h.OdemeTuru, h.TlTutari));
                adaylar[(g.Key, gun, TevsikIhlalTuru.GunlukKumulatif)] = Yeni(tenantId, TevsikIhlalTuru.GunlukKumulatif,
                    odeme >= tahsil ? TevsikIhlalYonu.Odeme : TevsikIhlalYonu.Tahsilat,
                    g.Key, gun, toplam, esik.Had, nakitli.Select(h => h.Id), anUtc);
            }

            // ── ISLEM_TUTARI (§4.1.2) — fiş toplamı haddi aşarken nakit payı > 0 ──
            // Takas fişi kural dışıdır (TevsikKurali: trampa DOĞRULANAMADI). Aynı
            // cari × gün için günlük kümülatif zaten yazıldıysa ikinci satır açılmaz.
            if (esik.IslemTutariKuraliEtkin)
            {
                foreach (var f in satirlar.Where(h => h.FisId != null).GroupBy(h => h.FisId!))
                {
                    if (f.Any(h => h.Tur == "TAKAS")) continue;
                    var cari = f.First().CariId!;
                    if (adaylar.ContainsKey((cari, gun, TevsikIhlalTuru.GunlukKumulatif))) continue;
                    var fisToplam = f.Sum(h => Math.Abs(h.TlTutari));
                    var fisNakit = f.Sum(h => Nakit(h.NakitTutar, h.OdemeTuru, h.TlTutari));
                    if (fisToplam <= esik.Had || fisNakit <= 0m) continue;
                    var anahtar = (cari, gun, TevsikIhlalTuru.IslemTutari);
                    if (adaylar.TryGetValue(anahtar, out var var0))
                    {
                        var0.NakitToplamTl += fisNakit;
                        var0.HareketIdleri = JsonBirlestir(var0.HareketIdleri, f.Select(h => h.Id));
                        continue;
                    }
                    adaylar[anahtar] = Yeni(tenantId, TevsikIhlalTuru.IslemTutari,
                        OdemeYonu(f.First().Tur) ? TevsikIhlalYonu.Odeme : TevsikIhlalYonu.Tahsilat,
                        cari, gun, fisNakit, esik.Had, f.Select(h => h.Id), anUtc);
                }
            }
        }

        // ── TAKSIT (§4.1.2 kısım kısım ödeme) — kapıyı kaçan nakit taksit ──
        if (kural.EsiklerGunu(bugun).IslemTutariKuraliEtkin)
            await TaksitIhlalleri(db, tenantId, bugun, kural, adaylar, anUtc, iptal);

        // ── Yaz: var olan (cari, gün, tür) atlanır; UNIQUE yarışta karar verir ──
        var mevcut = (await db.TevsikIhlalleri.AsNoTracking()
                .Where(t => t.TenantId == tenantId && t.IhlalGunu >= bugun.AddDays(-(TaramaGun - 1)))
                .Select(t => new { t.CariId, t.IhlalGunu, t.Tur })
                .ToListAsync(iptal))
            .Select(t => (t.CariId, t.IhlalGunu, t.Tur)).ToHashSet();

        var yeniler = new List<TevsikIhlali>();
        foreach (var (anahtar, ihlal) in adaylar.OrderBy(a => a.Key.Gun))
        {
            if (mevcut.Contains(anahtar)) continue;
            var tatiller = ResmiTatiller.Aralik(ihlal.IhlalGunu.Year, 2, takvim);
            ihlal.BildirimSonGunu = TaksitKurali.BildirimSonGunu(ihlal.IhlalGunu, isGunu(ihlal.IhlalGunu), tatiller);
            db.TevsikIhlalleri.Add(ihlal);
            try
            {
                await db.SaveChangesAsync(iptal);
                yeniler.Add(ihlal);
            }
            catch (DbUpdateException ex) when (Isler.IsCalistirici.BenzersizlikIhlali(ex))
            {
                db.Entry(ihlal).State = EntityState.Detached;   // eşzamanlı tur yazdı — tek satır kalır
            }
        }

        // ── Süresi dolan: ACIK → SURE_DOLDU (kayıt SİLİNMEZ) ──
        var dolanlar = await db.TevsikIhlalleri
            .Where(t => t.TenantId == tenantId && t.Durum == TevsikIhlalDurumu.Acik && t.BildirimSonGunu < bugun)
            .ToListAsync(iptal);
        foreach (var d in dolanlar) d.Durum = TevsikIhlalDurumu.SureDoldu;
        if (dolanlar.Count > 0) await db.SaveChangesAsync(iptal);

        return new TevsikTaramaSonucu(yeniler, dolanlar.Count, TaramaGun);
    }

    /// <summary>Günün nakit toplamı — TevsikKapisi'nin TEK motoru (ikinci formül yok).</summary>
    private static Task<decimal> GunToplami(KuyumcuDbContext db, string tenantId, string cariId, DateTime gunBasi) =>
        TevsikKapisi.GunlukNakitToplam(db, tenantId, cariId, gunBasi);

    private static async Task TaksitIhlalleri(
        KuyumcuDbContext db, string tenantId, DateOnly bugun, TevsikKurali kural,
        Dictionary<(string, DateOnly, string), TevsikIhlali> adaylar, DateTime anUtc, CancellationToken iptal)
    {
        var (bas, _) = Zaman.YerelGunAraligiUtc(bugun.AddDays(-(TaramaGun - 1)));
        var tahsilatlar = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.CariId != null && h.Tur == "TAHSILAT"
                     && h.TlTutari < 0m && h.Tarih >= bas)
            .StornoCiftiniDisla(db, tenantId)
            .Select(h => new { h.Id, h.CariId, h.Tarih, h.NakitTutar, h.OdemeTuru, h.TlTutari })
            .ToListAsync(iptal);

        foreach (var c in tahsilatlar.Where(h => Nakit(h.NakitTutar, h.OdemeTuru, h.TlTutari) > 0m)
                                     .GroupBy(h => h.CariId!))
        {
            var (tuketimler, partiler) = await Vade.VadeKapisi.TuketimleriOynat(db, tenantId, c.Key, alacakYonu: true);
            var bilgi = await TevsikKapisi.KaynakBilgisi(db, tenantId, partiler.Values);
            foreach (var t in c)
            {
                if (!tuketimler.TryGetValue(t.Id, out var tuk)) continue;
                var dagilim = tuk.Select(x =>
                {
                    var k = bilgi[x.PartiId];
                    return new TaksitParti(x.PartiId, x.Tutar, k.Tur, k.KaynakTutar, k.KaynakGun);
                }).ToList();
                var nakit = Nakit(t.NakitTutar, t.OdemeTuru, t.TlTutari);
                var sonuc = TaksitKurali.TaksitDegerlendir(nakit, dagilim, g => kural.EsiklerGunu(g).Had);
                if (!sonuc.Red) continue;

                var gun = DateOnly.FromDateTime(Zaman.UtcdenYerele(t.Tarih));
                var buyukId = dagilim.First(p => TaksitKurali.Buyuk(p, g => kural.EsiklerGunu(g).Had)).PartiId!;
                // Tur 2 (hakem [DÜŞÜK]): aynı cari × gün için GÜNLÜK KÜMÜLATİF satırı
                // zaten açıldıysa AYNI NAKİT ikinci kez (TAKSIT olarak) raporlanmaz —
                // tek olay, tek bildirim; taksit kanıtı (tahsilat + büyük işlem) o
                // satırın hareketlerine eklenir (ISLEM_TUTARI ile aynı kural).
                if (adaylar.TryGetValue((c.Key, gun, TevsikIhlalTuru.GunlukKumulatif), out var gunluk))
                {
                    gunluk.HareketIdleri = JsonBirlestir(gunluk.HareketIdleri, [t.Id, buyukId]);
                    continue;
                }
                var anahtar = (c.Key, gun, TevsikIhlalTuru.Taksit);
                if (adaylar.TryGetValue(anahtar, out var var0))
                {
                    var0.NakitToplamTl += nakit;
                    var0.HareketIdleri = JsonBirlestir(var0.HareketIdleri, [t.Id, buyukId]);
                    continue;
                }
                adaylar[anahtar] = Yeni(tenantId, TevsikIhlalTuru.Taksit, TevsikIhlalYonu.Tahsilat,
                    c.Key, gun, nakit, sonuc.BuyukHad ?? kural.EsiklerGunu(gun).Had, [t.Id, buyukId], anUtc);
            }
        }
    }

    private static TevsikIhlali Yeni(
        string tenantId, string tur, string yon, string cariId, DateOnly gun, decimal nakit, decimal had,
        IEnumerable<string> hareketler, DateTime anUtc) => new()
    {
        Id = Kimlik.Ulid(),
        TenantId = tenantId,
        Tur = tur,
        Yon = yon,
        CariId = cariId,
        IhlalGunu = gun,
        NakitToplamTl = Math.Round(nakit, 2, MidpointRounding.AwayFromZero),
        HadTl = had,
        HareketIdleri = JsonSerializer.Serialize(hareketler.Distinct().ToArray()),
        Durum = TevsikIhlalDurumu.Acik,
        Olusturuldu = anUtc,
    };

    private static string JsonBirlestir(string json, IEnumerable<string> ek) =>
        JsonSerializer.Serialize((JsonSerializer.Deserialize<string[]>(json) ?? []).Concat(ek).Distinct().ToArray());

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// Bildirim başlığı — kişisel veri YOK (PAKET-06 Mehmet koşulu). "tevsik"
    /// kelimesi yok: "nakit sınırı" denir.
    /// </summary>
    public static string BildirimBasligi(TevsikIhlali t) => t.Yon == TevsikIhlalYonu.Odeme
        ? string.Format(Tr, "Nakit sınırı aşıldı — bildirim için son gün {0:dd.MM}", t.BildirimSonGunu.ToDateTime(TimeOnly.MinValue))
        : "Nakit sınırı aşıldı — mali müşavirinize danışın";

    /// <summary>Bildirim gövdesi — ad/TCKN/belge no YOK; cari yalnız bağlantıyla (ilgili varlık).</summary>
    public static string BildirimGovdesi(TevsikIhlali t)
    {
        // Tur 2: sınırın ALTINDAKİ nakit tutarı "sınır aşıldı" diye tek başına yazılmaz —
        // NEDEN aşım sayıldığı türe göre söylenir (ekrandaki satır gerekçesiyle aynı).
        var gun = t.IhlalGunu.ToDateTime(TimeOnly.MinValue);
        var ilk = t.Tur switch
        {
            TevsikIhlalTuru.IslemTutari => string.Format(Tr,
                "{0:dd.MM.yyyy} günü sınırı ({2:N2} TL) aşan bir işlemin {1:N2} TL'si nakit yapıldı.", gun, t.NakitToplamTl, t.HadTl),
            TevsikIhlalTuru.Taksit => string.Format(Tr,
                "{0:dd.MM.yyyy} günü sınırı ({2:N2} TL) aşan bir işlemin borcuna {1:N2} TL nakit alındı.", gun, t.NakitToplamTl, t.HadTl),
            _ => string.Format(Tr, "{0:dd.MM.yyyy} günü bir kişiyle toplam {1:N2} TL nakit işlem var (sınır {2:N2} TL).",
                gun, t.NakitToplamTl, t.HadTl),
        };
        return t.Yon == TevsikIhlalYonu.Odeme
            ? ilk + string.Format(Tr, " Son güne ({0:dd.MM.yyyy}) kadar idareye (vergi dairesi — bildirim yolunu mali müşavirinizle teyit edin) bildirirseniz ödeyen adına ceza kesilmez. Dilekçe taslağı Uyum ekranında hazır.",
                t.BildirimSonGunu.ToDateTime(TimeOnly.MinValue))
            : ilk + " Bu muafiyet ödeyen müşteriye aittir; mali müşavirinize bugün danışın.";
    }
}

// ── Servis (uçlar) ───────────────────────────────────────────────────────────

public sealed class TevsikIhlalServisi(KuyumcuDbContext db, DenetimServisi denetim, TakvimAyarlari? takvim = null)
{
    public const int GerekceAsgari = 20;

    /// <summary>Kapanmamış (işlem bekleyen) durumlar.</summary>
    public static readonly string[] Kapanmamis = [TevsikIhlalDurumu.Acik, TevsikIhlalDurumu.SureDoldu];

    public async Task<TevsikIhlalListesi> Listele(string? durum, AktifKullanici islem)
    {
        var bugun = Zaman.YerelBugun();
        var q = db.TevsikIhlalleri.AsNoTracking().Where(t => t.TenantId == islem.TenantId);
        if (!string.IsNullOrWhiteSpace(durum))
        {
            if (durum == "KAPANMAMIS") q = q.Where(t => Kapanmamis.Contains(t.Durum));
            else if (durum is TevsikIhlalDurumu.Acik or TevsikIhlalDurumu.Bildirildi
                     or TevsikIhlalDurumu.SureDoldu or TevsikIhlalDurumu.Gecersiz)
                q = q.Where(t => t.Durum == durum);
            else throw new BadRequestException("Geçersiz durum süzgeci").AlanIle("durum");
        }

        const int Sinir = 200;
        var satirlar = await q.OrderByDescending(t => t.IhlalGunu).ThenBy(t => t.Id)
            .Take(Sinir + 1)
            .Join(db.Cariler.AsNoTracking(), t => t.CariId, c => c.Id, (t, c) => new { t, CariAd = c.Ad })
            .ToListAsync();
        var dahaVar = satirlar.Count > Sinir;
        if (dahaVar) satirlar = satirlar.Take(Sinir).ToList();

        await denetim.ListeOkumaKaydet(islem, "TevsikIhlali", satirlar.Count);

        // Tur 2 (hakem [KRİTİK] K14): satır gerekçesi iki modda da yazılır →
        // ISLEM_TUTARI için fiş(ler) toplamı, TAKSIT için büyük işlemin günü.
        // Kaynak: kaydın kendi hareket kimlikleri (defter; tahmin yok).
        var gerekceIdleri = satirlar
            .Where(x => x.t.Tur is TevsikIhlalTuru.IslemTutari or TevsikIhlalTuru.Taksit)
            .SelectMany(x => JsonSerializer.Deserialize<string[]>(x.t.HareketIdleri) ?? [])
            .Distinct().ToList();
        var gerekceHareket = gerekceIdleri.Count == 0 ? []
            : await db.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == islem.TenantId && gerekceIdleri.Contains(h.Id))
                .Take(gerekceIdleri.Count)
                .Select(h => new { h.Id, h.Tur, h.Tarih, h.TlTutari })
                .ToListAsync();
        var hIndeks = gerekceHareket.ToDictionary(h => h.Id);
        (decimal? toplam, DateOnly? kaynakGun) Gerekce(TevsikIhlali t)
        {
            var ids = JsonSerializer.Deserialize<string[]>(t.HareketIdleri) ?? [];
            var hs = ids.Where(hIndeks.ContainsKey).Select(i => hIndeks[i]).ToList();
            if (hs.Count == 0) return (null, null);
            if (t.Tur == TevsikIhlalTuru.IslemTutari) return (hs.Sum(h => Math.Abs(h.TlTutari)), null);
            if (t.Tur == TevsikIhlalTuru.Taksit)
            {
                var kaynak = hs.Where(h => h.Tur != "TAHSILAT").OrderBy(h => h.Tarih).FirstOrDefault();
                return kaynak is null ? (null, null) : (null, DateOnly.FromDateTime(Zaman.UtcdenYerele(kaynak.Tarih)));
            }
            return (null, null);
        }

        return new TevsikIhlalListesi(
            [.. satirlar.Select(x => new TevsikIhlalSatiri(
                x.t.Id, x.t.Tur, x.t.Yon, x.t.CariId, x.CariAd, x.t.IhlalGunu,
                x.t.NakitToplamTl, x.t.HadTl, x.t.BildirimSonGunu, x.t.Durum,
                IsGunuFarki(bugun, x.t.BildirimSonGunu),
                x.t.KapatmaTarihi, x.t.BildirimTarihi, x.t.BildirimEvrakNo, x.t.Gec,
                Gerekce(x.t).toplam, Gerekce(x.t).kaynakGun))],
            await db.TevsikIhlalleri.CountAsync(t =>
                t.TenantId == islem.TenantId && Kapanmamis.Contains(t.Durum)),
            Sinir,
            dahaVar,
            ResmiTatiller.DiniBayramEksik(bugun.Year, takvim));
    }

    /// <summary>Denetim izindeki varlık türü (CariModulu/EvrakModulu K-A izi).</summary>
    public const string DogrulanamadiIzi = "TEVSIK_TAKSIT_DOGRULANAMADI";

    private static readonly System.Text.RegularExpressions.Regex IzDetay = new(
        @"nakit(?: tahsili)? (?<n>[0-9]+(?:\.[0-9]+)?) TL; kaynağı bilinmeyen bakiyeden (?<d>[0-9]+(?:\.[0-9]+)?) TL",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>
    /// K-A (tur 2, ekip oyu): kaynağı bilinmeyen borçtan alınan nakit — kapı GEÇİRDİ,
    /// iz yazdı. Uyum → Nakit sınırı sekmesinde salt okunur, KATLANIR liste. İhlal
    /// değildir (son gün/bildirim yok). Tutarlar izin kendi değişmez (Invariant)
    /// biçiminden okunur; cari hareket/evraktan bulunur (ad ekranda, kimlik yok).
    /// </summary>
    public async Task<TevsikDogrulanamayanListesi> Dogrulanamayanlar(AktifKullanici islem)
    {
        const int Sinir = 100, Gun = 90;
        var bas = DateTime.UtcNow.AddDays(-Gun);
        var izler = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == islem.TenantId && d.VarlikTuru == DogrulanamadiIzi && d.Tarih >= bas)
            .OrderByDescending(d => d.Tarih).ThenBy(d => d.Id)
            .Take(Sinir + 1)
            .Select(d => new { d.Id, d.Tarih, d.VarlikId, d.Detay })
            .ToListAsync();
        var dahaVar = izler.Count > Sinir;
        if (dahaVar) izler = izler.Take(Sinir).ToList();

        var ids = izler.Where(i => i.VarlikId != null).Select(i => i.VarlikId!).Distinct().ToList();
        var hareketCari = ids.Count == 0 ? [] : await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && ids.Contains(h.Id))
            .Take(ids.Count).Select(h => new { h.Id, h.CariId }).ToListAsync();
        var evrakCari = ids.Count == 0 ? [] : await db.KiymetliEvraklar.AsNoTracking()
            .Where(e => e.TenantId == islem.TenantId && ids.Contains(e.Id))
            .Take(ids.Count).Select(e => new { e.Id, CariId = (string?)e.KaynakCariId }).ToListAsync();
        var cariMap = hareketCari.Select(h => (h.Id, h.CariId)).Concat(evrakCari.Select(e => (e.Id, e.CariId)))
            .GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First().CariId);
        var cariIdleri = cariMap.Values.Where(c => c != null).Select(c => c!).Distinct().ToList();
        var cariAd = cariIdleri.Count == 0 ? new Dictionary<string, string>() : await db.Cariler.AsNoTracking()
            .Where(c => c.TenantId == islem.TenantId && cariIdleri.Contains(c.Id))
            .Take(cariIdleri.Count).ToDictionaryAsync(c => c.Id, c => c.Ad);

        await denetim.ListeOkumaKaydet(islem, "TevsikDogrulanamayan", izler.Count);

        return new TevsikDogrulanamayanListesi(
            [.. izler.Select(i =>
            {
                var m = IzDetay.Match(i.Detay ?? "");
                decimal? Sayi(string g) => m.Success && decimal.TryParse(m.Groups[g].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : null;
                var cari = i.VarlikId is { } v0 && cariMap.TryGetValue(v0, out var c0) ? c0 : null;
                return new TevsikDogrulanamayanSatiri(
                    i.Id, i.Tarih,
                    (i.Detay ?? "").StartsWith("evrak", StringComparison.Ordinal) ? "EVRAK" : "TAHSILAT",
                    cari, cari is { } ci && cariAd.TryGetValue(ci, out var ad) ? ad : null,
                    Sayi("n"), Sayi("d"));
            })],
            Sinir, dahaVar, Gun);
    }

    /// <summary>Bugünden son güne kalan iş günü (son gün dahil); geçmişse negatif değil 0.</summary>
    private int IsGunuFarki(DateOnly bugun, DateOnly son)
    {
        if (son < bugun) return 0;
        var tatiller = ResmiTatiller.Aralik(bugun.Year, 2, takvim);
        var n = 0;
        for (var g = bugun; g <= son; g = g.AddDays(1))
            if (g.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !tatiller.Contains(g)) n++;
        return n;
    }

    private async Task<TevsikIhlali> Bul(string id, string tenantId) =>
        await db.TevsikIhlalleri.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId)
        ?? throw new NotFoundException("Kayıt bulunamadı");

    /// <summary>
    /// Bildirim dilekçesi verisi. Karşı tarafın TAM kimliği YALNIZ bu uçta döner ve
    /// OKUMA İZİ zorunludur (KVKK md.12, hassas). Hüküm şekil öngörmediği için (§ 3.2 T-1)
    /// metin "TASLAK — mali müşavirinizle kontrol edin" damgalı serbest dilekçedir.
    /// </summary>
    public async Task<TevsikDilekceVeri> Dilekce(string id, AktifKullanici islem)
    {
        var t = await Bul(id, islem.TenantId);
        var cari = await db.Cariler.AsNoTracking().FirstOrDefaultAsync(c => c.Id == t.CariId && c.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Karşı taraf bulunamadı");
        var isletme = await db.Tenants.AsNoTracking().FirstAsync(x => x.Id == islem.TenantId);

        var ids = JsonSerializer.Deserialize<string[]>(t.HareketIdleri) ?? [];
        var hareketler = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && ids.Contains(h.Id))
            .OrderBy(h => h.Tarih)
            .Take(ids.Length)   // kanıt kimlik başına tek satır — sınır kayıpsızdır
            .Select(h => new { h.Id, h.Tarih, h.Tur, h.TlTutari, h.NakitTutar, h.OdemeTuru })
            .ToListAsync();

        await denetim.OkumaKaydet(islem, "TevsikIhlali", t.Id, "nakit sınırı bildirim dilekçesi");

        return new TevsikDilekceVeri(
            Taslak: true,
            Damga: "TASLAK — mali müşavirinizle kontrol edin",
            Ihlal: new TevsikDilekceIhlal(t.Id, t.Tur, t.Yon, t.IhlalGunu, t.NakitToplamTl, t.HadTl, t.BildirimSonGunu, t.Durum),
            Isletme: new TevsikDilekceIsletme(isletme.Ad, isletme.VergiDairesi, isletme.VergiNo, isletme.Adres, isletme.Telefon),
            KarsiTaraf: new TevsikDilekceTaraf(cari.Ad, cari.VergiNo, cari.Adres, cari.Telefon),
            Hareketler: [.. hareketler.Select(h => new TevsikDilekceHareket(h.Id, h.Tarih, h.Tur,
                Math.Abs(h.TlTutari), h.NakitTutar ?? (h.OdemeTuru == "NAKIT" ? Math.Abs(h.TlTutari) : 0m)))],
            Dayanak: "VUK mükerrer md.355; 459 sıra no.lu VUK Genel Tebliği §4.1",
            MuafiyetOdeyene: true);
    }

    public async Task<TevsikKapatmaYaniti> Bildirildi(string id, TevsikBildirildiDto dto, AktifKullanici islem)
    {
        var t = await Bul(id, islem.TenantId);
        if (!Kapanmamis.Contains(t.Durum))
            throw new ConflictException("Bu kayıt zaten kapatılmış; kapatma bir kez yazılır.");
        if (dto.BildirimTarihi is not { } tarih)
            throw new BadRequestException("Bildirim tarihi zorunludur.").AlanIle("bildirimTarihi");
        if (tarih > Zaman.YerelBugun())
            throw new BadRequestException("Bildirim tarihi bugünden sonra olamaz.").AlanIle("bildirimTarihi");
        if (tarih < t.IhlalGunu)
            throw new BadRequestException("Bildirim tarihi aşım gününden önce olamaz.").AlanIle("bildirimTarihi");
        var gerekce = GerekceDogrula(dto.Gerekce);

        var now = DateTime.UtcNow;
        t.Durum = TevsikIhlalDurumu.Bildirildi;
        t.BildirimTarihi = tarih;
        t.Gec = tarih > t.BildirimSonGunu;   // geç bildirim yalan beyan üretmez
        t.BildirimEvrakNo = string.IsNullOrWhiteSpace(dto.EvrakNo) ? null : dto.EvrakNo.Trim();
        t.KapatmaTarihi = now;
        t.KapatanKullaniciId = islem.Id;
        t.KapatmaGerekcesi = gerekce;
        denetim.YazmaKaydet(islem, "TevsikIhlali", t.Id,
            FormattableString.Invariant($"BILDIRILDI · tarih {tarih:yyyy-MM-dd} · gec={t.Gec} · {gerekce}"));
        await db.SaveChangesAsync();
        return new TevsikKapatmaYaniti(t.Id, t.Durum, t.Gec, t.BildirimTarihi, t.BildirimSonGunu);
    }

    public async Task<TevsikKapatmaYaniti> Gecersiz(string id, TevsikGecersizDto dto, AktifKullanici islem)
    {
        var t = await Bul(id, islem.TenantId);
        if (!Kapanmamis.Contains(t.Durum))
            throw new ConflictException("Bu kayıt zaten kapatılmış; kapatma bir kez yazılır.");
        var gerekce = GerekceDogrula(dto.Gerekce);
        var now = DateTime.UtcNow;
        t.Durum = TevsikIhlalDurumu.Gecersiz;
        t.KapatmaTarihi = now;
        t.KapatanKullaniciId = islem.Id;
        t.KapatmaGerekcesi = gerekce;
        denetim.YazmaKaydet(islem, "TevsikIhlali", t.Id, $"GECERSIZ · {gerekce}");
        await db.SaveChangesAsync();
        return new TevsikKapatmaYaniti(t.Id, t.Durum, t.Gec, t.BildirimTarihi, t.BildirimSonGunu);
    }

    private static string GerekceDogrula(string? g)
    {
        var s = (g ?? "").Trim();
        if (s.Length < GerekceAsgari)
            throw new BadRequestException($"Gerekçe en az {GerekceAsgari} karakter olmalı ({s.Length}/{GerekceAsgari}).")
                .AlanIle("gerekce");
        return s;
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/tevsik/ihlaller")]
public sealed class TevsikIhlalController(TevsikIhlalServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/tevsik/ihlaller?durum=ACIK|KAPANMAMIS|BILDIRILDI|SURE_DOLDU|GECERSIZ</summary>
    [HttpGet]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<TevsikIhlalListesi> Listele([FromQuery] string? durum) => servis.Listele(durum, Islem);

    /// <summary>K-A: kaynağı bilinmeyen borçtan alınan nakit (salt okunur, son 90 gün).</summary>
    [HttpGet("dogrulanamayanlar")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<TevsikDogrulanamayanListesi> Dogrulanamayanlar() => servis.Dogrulanamayanlar(Islem);

    /// <summary>Bildirim dilekçesi — tam kimlik YALNIZ burada, okuma izi zorunlu.</summary>
    [HttpGet("{id}/dilekce")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<TevsikDilekceVeri> Dilekce(string id) => servis.Dilekce(id, Islem);

    [HttpPost("{id}/bildirildi")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<TevsikKapatmaYaniti> Bildirildi(string id, [FromBody] TevsikBildirildiDto dto) => servis.Bildirildi(id, dto, Islem);

    /// <summary>Yalnız SAHİP — gerekçe ≥ 20 karakter, denetim izine yazılır.</summary>
    [HttpPost("{id}/gecersiz")]
    [RolGerekli("SAHIP")]
    public Task<TevsikKapatmaYaniti> Gecersiz(string id, [FromBody] TevsikGecersizDto dto) => servis.Gecersiz(id, dto, Islem);
}
