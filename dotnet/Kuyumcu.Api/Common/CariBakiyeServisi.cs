using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Common;

// ─────────────────────────────────────────────────────────────────────────────
// PAKET CARI-BAKIYE (30.09, ekip oyu Tamer Bey + Taşkın + Mustafa + Mehmet)
//
// (1) TEK YAZICI — cari_bakiyeler.tl_bakiye / has_gram_bakiye kolonlarına YALNIZ
//     CariBakiyeYazici yazar. Satış, alış, tahsilat, emanet, storno, düzeltme, hurda,
//     takas, avans, evrak… hepsi Uygula()'yı çağırır. Eskiden beş ayrı modülde beş
//     ayrı "BakiyeGuncelle" vardı; biri yuvarlıyor, biri izlenen (Local) satırı
//     görmüyor, biri denetimsiz yazıyordu. denetim.sh "CARI_BAKIYE_DOGRUDAN_YAZIM"
//     sayacı bu dosya dışındaki her atamayı ihlal sayar (beklenen 0).
//
// (2) TEK TANIM — "müşterinin has bakiyesi" her ekranda EMANETSİZ bakiyedir
//     (KESIF-KARARLAR karar 3: emanet işletmenin malı değil, bakiyeye girmez).
//     ADR-CB-1 (bu paket): kayıtlı kolonun ANLAMI DEĞİŞMEDİ — has_gram_bakiye emanet
//     dahil tutulmaya devam eder (K4 + K20-2 sözleşmesi, EmanetKurali). Emanetsiz değer
//     OKUMADA tek fonksiyonla üretilir: emanetsiz = kayıtlı − emanet etkisi.
//     Gerekçe (daha az riskli olan): kolonun anlamını değiştirmek (a) 86+ canlı satırın
//     geri doldurulmasını, (b) emanet giriş/çıkış/storno yazan üç yolun ve EmanetKurali
//     aynasının birlikte değişmesini, (c) Pozisyon modülünün (kolon ↔ defter ALTIN
//     mutabakatı) yeniden yazılmasını gerektirirdi. Okuma tarafı ise tek fonksiyondur
//     ve yanlışsa ayna (bakiyeUyusmazligiHas) değişmez — kayıtlı veri hiç oynamaz.
//
// (3) TL AYNASI — has aynası gibi TL için de kural aynası: her defter satırının cari TL
//     bakiyesine yazdığı delta KURALDAN türetilir (yazan kodun kopyası DEĞİL; yazan kodu
//     okuyup satırın kendi kolonlarından — tür, TL, veresiye payı, storno — çıkarılır).
//     Bilinemeyen durumlar UYDURULMAZ (Anayasa md.1): veresiye payı olmayan (Faz 0 öncesi)
//     satır ve defter satırı taşımayan konsinye borcu "ölçülemez" döner (null).
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Cari bakiye kolonlarının TEK yazıcısı (denetim.sh: CARI_BAKIYE_DOGRUDAN_YAZIM = 0).</summary>
public static class CariBakiyeYazici
{
    /// <summary>Yeni carinin sıfır bakiye satırı (oluşturma anı).</summary>
    public static CariBakiye BosSatir(string tenantId, string cariId, DateTime now) => new()
    {
        CariId = cariId,
        TenantId = tenantId,
        TlBakiye = 0m,
        HasGramBakiye = 0m,
        Guncellendi = now,
    };

    /// <summary>
    /// Bakiye satırına delta uygular — çağıranın transaction'ı içinde, SaveChanges ÇAĞIRMADAN.
    /// Aynı istekte daha önce eklenmiş (henüz kaydedilmemiş) satır <c>Local</c>'den bulunur;
    /// satır yoksa açılır. TL 2, has 6 haneye yuvarlanır (kolon ölçeği). Döviz deltası verilirse
    /// döviz kolonu TEK YERDEN (DovizBakiye.Ekle) yazılır.
    /// </summary>
    public static async Task<CariBakiye> Uygula(
        KuyumcuDbContext db, string tenantId, string cariId,
        decimal tlDelta, decimal hasDelta, DateTime now,
        string? dovizParaBirimi = null, decimal dovizDelta = 0m)
    {
        var b = db.CariBakiyeler.Local
                    .FirstOrDefault(x => x.CariId == cariId && x.TenantId == tenantId)
                ?? await db.CariBakiyeler
                    .FirstOrDefaultAsync(x => x.CariId == cariId && x.TenantId == tenantId);
        if (b is null)
        {
            b = BosSatir(tenantId, cariId, now);
            db.CariBakiyeler.Add(b);
        }
        else await BakiyeKilidi.Kilitle(db, b);   // PAKET KAPORA-ESZAMAN tur 2: kayıp güncelleme — kilitli taze okuma

        b.TlBakiye = Math.Round(b.TlBakiye + tlDelta, 2, MidpointRounding.AwayFromZero);
        b.HasGramBakiye = Math.Round(b.HasGramBakiye + hasDelta, 6, MidpointRounding.AwayFromZero);
        if (dovizParaBirimi is not null) Modules.Cari.DovizBakiye.Ekle(b, dovizParaBirimi, dovizDelta);
        b.SonIslemTarihi = now;
        b.Guncellendi = now;
        return b;
    }
}

/// <summary>Aynanın okuduğu satır özeti (tek cari; gruplanmış).</summary>
public readonly record struct CariAynaGrubu(
    string Tur, string? Maden, bool Storno, int Yon, bool VeresiyeBilinmiyor,
    decimal Tl, decimal Veresiye, decimal Has);

/// <summary>Bir carinin kayıtlı bakiyesi ile kural aynası (has + TL) — tek hesap.</summary>
public sealed record CariAyna(
    decimal KayitliTl, decimal KayitliHas,
    decimal HasAyna, decimal EmanetEtkisi,
    decimal? TlAyna, string? TlOlculemezSebep)
{
    /// <summary>Ekrandaki (emanetsiz) has bakiyesi — TEK TANIM.</summary>
    public decimal EmanetsizHas => CariBakiyeKurali.EmanetsizHas(KayitliHas, EmanetEtkisi);
    /// <summary>"Emanette": müşterinin bizde duran altını (artı).</summary>
    public decimal EmanetteHas => -EmanetEtkisi;
    public decimal HasUyusmazligi => KayitliHas - HasAyna;
    /// <summary>null = ölçülemez (bkz. <see cref="TlOlculemezSebep"/>).</summary>
    public decimal? TlUyusmazligi => TlAyna is { } a ? KayitliTl - a : null;
}

/// <summary>Cari bakiye kural aynası ve emanetsiz okuma — TEK KAYNAK.</summary>
public static class CariBakiyeKurali
{
    public const decimal HasEsik = 0.0001m;
    public const decimal TlEsik = 0.01m;

    public const string SebepVeresiyePayiYok = "VERESIYE_PAYI_YOK";
    public const string SebepKonsinyeSahibi = "KONSINYE_SAHIBI";

    public static bool EmanetTuru(string tur) => tur is "EMANET_GIRIS" or "EMANET_CIKIS";

    /// <summary>Satırın cari has kolonuna yazdığı delta (K4 + K20-2; emanette EmanetKurali).</summary>
    public static decimal HasEtkisi(string tur, decimal hasGrami, string? maden) => tur switch
    {
        "VERESIYE" or "TAHSILAT" or "CARI_DUZELTME" or "ACILIS" => -hasGrami,
        "EMANET_GIRIS" or "EMANET_CIKIS" => Modules.Emanet.EmanetKurali.CariHasEtkisi(maden, hasGrami),
        _ => 0m,
    };

    /// <summary>Ekrandaki has = kayıtlı − emanet etkisi (karar 3: emanet bakiyeye girmez).</summary>
    public static decimal EmanetsizHas(decimal kayitliHas, decimal emanetEtkisi) => kayitliHas - emanetEtkisi;

    /// <summary>
    /// Satırın cari TL bakiyesine yazdığı delta — yazan kodun KURALI (işaret: artı = müşteri bize borçlu).
    /// <list type="bullet">
    /// <item>VERESIYE · ACILIS · CARI_DUZELTME · EVRAK_RUCU → TL tutarının kendisi (satır = bakiye deltası).</item>
    /// <item>TAHSILAT → kaynak yönü EKSİ ise (müşteri ödedi) TL tutarı; kaynak yönü ARTI ise 0 — bu evrak
    ///   tahsilidir (EvrakModulu: çek/senet girişinde borç zaten kapandı, tahsil kasaya yazar, cariye yazmaz).
    ///   Storno satırında yön kaynağın yönüdür (storno TL'si kaynağın tersidir).</item>
    /// <item>AVANS_ALINDI · AVANS_IADE → −TL (avans alınınca müşteri alacaklı olur; AvansKapisi).</item>
    /// <item>Diğer her tür (satış, alış, iade, hurda, takas, işçilik, evrak girişi/cirosu/karşılıksız, döviz…)
    ///   → işaret(TL) × veresiye payı. Peşin kısım cariye yazılmaz; döviz satırının veresiye payı daima 0.</item>
    /// <item>Veresiye payı NULL (alan eklenmeden önce yazılmış satır) ve TL ≠ 0 → null: BİLİNMİYOR.</item>
    /// </list>
    /// </summary>
    public static decimal? TlEtkisi(string tur, decimal tl, decimal? veresiye, bool storno) =>
        GrupTlEtkisi(new CariAynaGrubu(tur, null, storno, Math.Sign(tl), veresiye is null, tl, veresiye ?? 0m, 0m));

    /// <summary>
    /// Kuralın TEK gövdesi — bir grup (tür, storno, TL yönü, veresiye bilinirliği aynı satırlar) için.
    /// Yön grubu içinde sabit olduğundan Σ işaret(tl) × v = yön × Σv (doğrusal).
    /// </summary>
    public static decimal? GrupTlEtkisi(CariAynaGrubu g) => g.Tur switch
    {
        "VERESIYE" or "ACILIS" or "CARI_DUZELTME" or "EVRAK_RUCU" => g.Tl,
        "TAHSILAT" => TahsilatCariyeYazar(g.Storno ? -g.Yon : g.Yon) ? g.Tl : 0m,
        AvansKapisi.Alindi or AvansKapisi.Iade => -g.Tl,
        _ => g.Yon == 0 ? 0m : g.VeresiyeBilinmiyor ? null : g.Yon * g.Veresiye,
    };

    /// <summary>
    /// TAHSILAT satırının cari TL'ye YAZIP yazmadığı — kaynağın TL yönüne göre TEK kural
    /// (aynayla StornoServisi ortak; EmanetKurali deseni). Yön EKSİ = müşteri ödedi → cariye yazar;
    /// yön ARTI = evrak tahsili (EvrakModulu: borç çek/senet girişinde kapandı) → cariye YAZMAZ.
    /// </summary>
    public static bool TahsilatCariyeYazar(int kaynakTlYonu) => kaynakTlYonu < 0;

    /// <summary>
    /// Bir kaynak TAHSILAT hareketinin cari TL'ye yazdığı delta (storno bunu tersler).
    /// Evrak tahsili (TL &gt; 0) cariye hiç yazmamıştır → 0; yazılmamış etki geri alınmaz.
    /// </summary>
    public static decimal TahsilatCariTl(decimal kaynakTl) =>
        TahsilatCariyeYazar(Math.Sign(kaynakTl)) ? kaynakTl : 0m;

    /// <summary>Gruplanmış satırlardan TL aynası; tek bir grup bile bilinmiyorsa null (uydurulmaz).</summary>
    public static decimal? TlAynasi(IEnumerable<CariAynaGrubu> gruplar)
    {
        var toplam = 0m;
        foreach (var g in gruplar)
        {
            if (GrupTlEtkisi(g) is not { } e) return null;
            toplam += e;
        }
        return toplam;
    }

    /// <summary>
    /// DEĞİŞİM (satış iadesi + yeni satış, tek fiş) farkı müşteri LEHİNE ve VERESİYE kapandığında
    /// (SatisModulu.P21 Degisim: fark &lt; 0) cariye −|fark| yazılır ama iki bacak da ödeme izi
    /// taşımaz (veresiye payı işaretsizdir, eksi fark yazılamaz). Ayna bunu fişin kendisinden
    /// çıkarır: SATIS bacağı etiketi VERESIYE, veresiye payı 0, aynı fişte SATIS_IADE var →
    /// fark = iki bacağın TL toplamı; yalnız eksiyse cariye yazılmıştır.
    /// </summary>
    public static async Task<decimal> DegisimVeresiyeFarki(KuyumcuDbContext db, string tenantId, string cariId)
    {
        var fisler = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.CariId == cariId && h.Tur == "SATIS"
                     && h.StornoKaynakId == null && h.FisId != null
                     && h.OdemeTuru == "VERESIYE" && h.VeresiyeTutar == 0m && h.TlTutari > 0m
                     && db.Hareketler.Any(i => i.TenantId == tenantId && i.FisId == h.FisId
                                            && i.Tur == "SATIS_IADE" && i.CariId == cariId))
            .Select(h => h.FisId!)
            .Distinct()
            .ToListAsync();
        if (fisler.Count == 0) return 0m;

        var farklar = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.CariId == cariId && h.FisId != null
                     && fisler.Contains(h.FisId) && (h.Tur == "SATIS" || h.Tur == "SATIS_IADE"))
            .GroupBy(h => h.FisId)
            .Select(g => g.Sum(h => h.TlTutari))
            .ToListAsync();
        return farklar.Where(f => f < 0m).Sum();
    }

    /// <summary>Tek carinin emanet etkisi (kayıtlı has'taki emanet payı; alınan emanet eksi).</summary>
    public static async Task<decimal> EmanetEtkisi(KuyumcuDbContext db, string tenantId, string cariId) =>
        -(await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.CariId == cariId
                     && (h.Tur == "EMANET_GIRIS" || h.Tur == "EMANET_CIKIS") && h.Maden == Maden.Altin)
            .SumAsync(h => (decimal?)h.HasGrami) ?? 0m);

    /// <summary>Birden çok carinin emanet etkisi — liste ekranları için tek sorgu (N+1 yok).</summary>
    public static async Task<Dictionary<string, decimal>> EmanetEtkileri(
        KuyumcuDbContext db, string tenantId, IReadOnlyCollection<string> cariIdler)
    {
        if (cariIdler.Count == 0) return [];
        var satirlar = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.CariId != null && cariIdler.Contains(h.CariId)
                     && (h.Tur == "EMANET_GIRIS" || h.Tur == "EMANET_CIKIS") && h.Maden == Maden.Altin)
            .GroupBy(h => h.CariId!)
            .Select(g => new { CariId = g.Key, Has = g.Sum(h => h.HasGrami) })
            .ToListAsync();
        return satirlar.ToDictionary(s => s.CariId, s => -s.Has);
    }

    /// <summary>
    /// Sorgu düzeyinde emanetsiz has (rapor süzgeçleri SQL'de çalışsın diye): kayıtlı + Σ altın emanet has'ı.
    /// <see cref="EmanetsizHas"/> ile AYNI tanım (emanet etkisi = −Σ has).
    /// </summary>
    public static IQueryable<CariBakiyeGorunumu> Gorunum(KuyumcuDbContext db, string tenantId) =>
        db.CariBakiyeler.AsNoTracking()
            .Where(b => b.TenantId == tenantId)
            .Select(b => new CariBakiyeGorunumu
            {
                Bakiye = b,
                EmanetsizHas = b.HasGramBakiye + (db.Hareketler
                    .Where(h => h.TenantId == tenantId && h.CariId == b.CariId
                             && (h.Tur == "EMANET_GIRIS" || h.Tur == "EMANET_CIKIS") && h.Maden == Maden.Altin)
                    .Sum(h => (decimal?)h.HasGrami) ?? 0m),
            });

    /// <summary>Tek carinin kayıtlı bakiyesi + has ve TL aynası (ekstre ve testler bunu okur).</summary>
    public static async Task<CariAyna> Hesapla(KuyumcuDbContext db, string tenantId, string cariId)
    {
        var bakiye = await db.CariBakiyeler.AsNoTracking()
            .FirstOrDefaultAsync(b => b.CariId == cariId && b.TenantId == tenantId);

        var gruplar = (await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.CariId == cariId)
            .GroupBy(h => new
            {
                h.Tur, h.Maden,
                Storno = h.StornoKaynakId != null,
                Arti = h.TlTutari > 0m,
                Eksi = h.TlTutari < 0m,
                VNull = h.VeresiyeTutar == null,
            })
            .Select(g => new
            {
                g.Key.Tur, g.Key.Maden, g.Key.Storno, g.Key.Arti, g.Key.Eksi, g.Key.VNull,
                Tl = g.Sum(h => h.TlTutari),
                V = g.Sum(h => h.VeresiyeTutar ?? 0m),
                Has = g.Sum(h => h.HasGrami),
            })
            .ToListAsync())
            .Select(g => new CariAynaGrubu(g.Tur, g.Maden, g.Storno, g.Arti ? 1 : g.Eksi ? -1 : 0, g.VNull, g.Tl, g.V, g.Has))
            .ToList();

        var hasAyna = gruplar.Sum(g => HasEtkisi(g.Tur, g.Has, g.Maden));
        var emanetEtkisi = gruplar.Where(g => EmanetTuru(g.Tur)).Sum(g => HasEtkisi(g.Tur, g.Has, g.Maden));

        decimal? tlAyna;
        string? sebep = null;
        // Konsinye malın bedeli SAHİBİNE borç yazılır ama sahibin defterinde satır DOĞMAZ
        // (SatisModulu: konsinye borcu). Kural buradan türetilemez → ölçülemez, uydurulmaz.
        var konsinyeSahibi = await db.Urunler.AsNoTracking()
            .AnyAsync(u => u.TenantId == tenantId && u.KonsinyeCariId == cariId);
        if (konsinyeSahibi)
        {
            tlAyna = null;
            sebep = SebepKonsinyeSahibi;
        }
        else
        {
            tlAyna = TlAynasi(gruplar);
            if (tlAyna is null) sebep = SebepVeresiyePayiYok;
            else tlAyna += await DegisimVeresiyeFarki(db, tenantId, cariId);
        }

        return new CariAyna(bakiye?.TlBakiye ?? 0m, bakiye?.HasGramBakiye ?? 0m,
            hasAyna, emanetEtkisi, tlAyna, sebep);
    }
}

/// <summary>Kayıtlı bakiye + sorguda üretilmiş emanetsiz has (rapor süzgeçleri için).</summary>
public sealed class CariBakiyeGorunumu
{
    public CariBakiye Bakiye { get; init; } = null!;
    public decimal EmanetsizHas { get; init; }
}
