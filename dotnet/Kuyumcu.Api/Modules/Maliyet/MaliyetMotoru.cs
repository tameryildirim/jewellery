using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Maliyet;

// ═════════════════════════════════════════════════════════════════════════════
// MALİYET MOTORU — parti (lot) açma ve tüketme
//
// TEK YAZMA NOKTASI: stok her değiştiğinde buradan geçer. `StokDegistir`
// nasıl stoğun tek kapısıysa, bu da maliyetin tek kapısıdır.
//
// ⚠ STOK OTORİTEDİR, PARTİ DEĞİL.
// Parti defteri stok sayısından geride kalabilir (bu özellik gelmeden önce
// girmiş mallar, elle düzeltmeler). Böyle bir durumda satış DURDURULMAZ —
// tüketilebilen kadar parti tüketilir, kalan adet "maliyeti bilinmeyen" olarak
// raporlanır. Bir muhasebe defteri eksiği yüzünden tezgâhta satışı durdurmak
// yanlış olurdu; sessizce sıfır maliyet yazmak ise kârı şişirirdi.
//
// ⚠ SIFIR MALİYET ≠ BİLİNMEYEN MALİYET. NULL taşınır, 0 yazılmaz.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir çıkışın maliyet sonucu.</summary>
public sealed record MaliyetSonucu(
    decimal MaliyetliAdet,
    decimal MaliyetsizAdet,
    decimal ToplamMaliyetTl,
    decimal ToplamMaliyetHas)
{
    public static readonly MaliyetSonucu Bos = new(0m, 0m, 0m, 0m);

    /// <summary>Çıkışın tamamının maliyeti biliniyor mu?</summary>
    public bool TamBilinir => MaliyetsizAdet == 0m && MaliyetliAdet > 0m;

    /// <summary>Hiç maliyet bilgisi var mı?</summary>
    public bool KismenBilinir => MaliyetliAdet > 0m;
}

public sealed class MaliyetMotoru(KuyumcuDbContext db)
{
    // ── Giriş ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Stoğa giren mal için parti açar.
    /// </summary>
    /// <param name="birimMaliyetTl">
    /// Birim alış maliyeti. <b>null</b> = maliyet bilinmiyor (sayım fazlası, devir).
    /// </param>
    /// <param name="altinHasFiyati">
    /// Giriş anındaki has altın fiyatı (TL/gram). Maliyetin has karşılığı bununla
    /// hesaplanır. null ya da 0 ise has maliyeti yazılmaz — <b>uydurulmaz</b>.
    /// </param>
    public StokPartisi PartiAc(
        string tenantId, string subeId, string urunId, string kaynak,
        decimal adet, decimal hasGramBirim, DateTime an,
        decimal? birimMaliyetTl = null, decimal? altinHasFiyati = null,
        string? girisHareketId = null, string? kurSnapshotId = null)
    {
        if (adet <= 0m)
            throw new BadRequestException("Parti adedi sıfırdan büyük olmalıdır");

        // Has karşılığı ancak geçerli bir kur varsa hesaplanır.
        decimal? birimMaliyetHas = null;
        if (birimMaliyetTl is > 0m && altinHasFiyati is > 0m)
            birimMaliyetHas = Math.Round(birimMaliyetTl.Value / altinHasFiyati.Value, 6);

        var parti = new StokPartisi
        {
            Id = Kimlik.Ulid(),
            TenantId = tenantId,
            SubeId = subeId,
            UrunId = urunId,
            GirisHareketId = girisHareketId,
            Kaynak = kaynak,
            AdetGiren = Math.Round(adet, 3),
            AdetKalan = Math.Round(adet, 3),
            HasGramBirim = Math.Round(hasGramBirim, 6),
            BirimMaliyetTl = birimMaliyetTl is > 0m ? Math.Round(birimMaliyetTl.Value, 4) : null,
            BirimMaliyetHas = birimMaliyetHas,
            KurSnapshotId = kurSnapshotId,
            GirisTarihi = an,
            Olusturuldu = an,
        };
        db.Set<StokPartisi>().Add(parti);
        return parti;
    }

    // ── Çıkış ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Stoktan çıkan mal için partileri tüketir ve satılan malın maliyetini döndürür.
    /// Parti yetmezse çıkış yine yapılır; eksik kalan adet
    /// <see cref="MaliyetSonucu.MaliyetsizAdet"/> olarak raporlanır.
    /// </summary>
    public async Task<MaliyetSonucu> PartiTuket(
        string tenantId, string subeId, string urunId,
        decimal adet, DateTime an, string yontem,
        string? cikisHareketId = null)
    {
        if (adet <= 0m) return MaliyetSonucu.Bos;
        if (!MaliyetYontemi.Gecerli(yontem))
            throw new BadRequestException($"Geçersiz maliyet yöntemi: '{yontem}'");

        var acikPartiler = await AcikPartiler(tenantId, subeId, urunId, yontem);

        // ORTALAMA: tüketim sırası FIFO ama birim maliyet tüm açık partilerin
        // ağırlıklı ortalamasıdır. Miktar muhasebesi basit kalır, maliyet doğru olur.
        decimal? ortTl = null, ortHas = null;
        if (yontem == MaliyetYontemi.Ortalama)
            (ortTl, ortHas) = AgirlikliOrtalama(acikPartiler);

        decimal kalanIhtiyac = Math.Round(adet, 3);
        decimal maliyetliAdet = 0m, maliyetsizAdet = 0m;
        decimal toplamTl = 0m, toplamHas = 0m;

        foreach (var parti in acikPartiler)
        {
            if (kalanIhtiyac <= 0m) break;

            var alinan = Math.Min(parti.AdetKalan, kalanIhtiyac);
            if (alinan <= 0m) continue;

            var bTl  = yontem == MaliyetYontemi.Ortalama ? ortTl  : parti.BirimMaliyetTl;
            var bHas = yontem == MaliyetYontemi.Ortalama ? ortHas : parti.BirimMaliyetHas;

            parti.AdetKalan = Math.Round(parti.AdetKalan - alinan, 3);

            db.Set<StokPartiTuketimi>().Add(new StokPartiTuketimi
            {
                Id = Kimlik.Ulid(),
                TenantId = tenantId,
                PartiId = parti.Id,
                CikisHareketId = cikisHareketId,
                UrunId = urunId,
                SubeId = subeId,
                Adet = Math.Round(alinan, 3),
                BirimMaliyetTl = bTl,
                BirimMaliyetHas = bHas,
                Tarih = an,
            });

            if (bTl is not null)
            {
                maliyetliAdet += alinan;
                toplamTl  += bTl.Value * alinan;
                toplamHas += (bHas ?? 0m) * alinan;
            }
            else
            {
                // Parti var ama maliyeti bilinmiyor (DEVIR / SAYIM_FAZLA).
                maliyetsizAdet += alinan;
            }

            kalanIhtiyac = Math.Round(kalanIhtiyac - alinan, 3);
        }

        // Parti defteri stoktan geride kaldıysa kalan adet maliyetsiz sayılır.
        if (kalanIhtiyac > 0m) maliyetsizAdet += kalanIhtiyac;

        return new MaliyetSonucu(
            Math.Round(maliyetliAdet, 3),
            Math.Round(maliyetsizAdet, 3),
            Math.Round(toplamTl, 2),
            Math.Round(toplamHas, 6));
    }

    // ── İade ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Bir çıkış hareketinin tükettiği partileri GERİ KOYAR.
    ///
    /// ⚠ İade edilen mal, ÇIKTIĞI maliyetle geri girer — bugünkü maliyetle
    /// değil. Yeni parti açılsaydı, altın yükselirken iade alınan mal daha
    /// pahalı bir maliyetle stoğa girer ve satılmamış bir malda sahte kâr/zarar
    /// doğardı. Bu yüzden orijinal tüketim satırları bulunur ve aynı partilere
    /// aynı birim maliyetle geri yazılır.
    ///
    /// ⚠ Tüketim satırı SİLİNMEZ; NEGATİF adetli bir ters kayıt eklenir.
    /// Silinseydi "bu parti ne zaman tüketildi, ne zaman geri geldi" tarihçesi
    /// yok olurdu (append-only defter ilkesi).
    /// </summary>
    /// <param name="cikisHareketId">İade edilen ORİJİNAL çıkış hareketi.</param>
    /// <param name="adet">Geri konacak adet — orijinalin tamamı veya bir kısmı.</param>
    /// <param name="iadeHareketId">Bu iadeyi temsil eden yeni hareket.</param>
    public async Task<MaliyetSonucu> PartiIade(
        string tenantId, string cikisHareketId, decimal adet,
        DateTime an, string iadeHareketId)
    {
        if (adet <= 0m) return MaliyetSonucu.Bos;

        // Orijinal tüketimler; daha önceki iadelerin ters kayıtları da burada
        // görünür ve net adede dâhil edilir.
        var tuketimler = await db.Set<StokPartiTuketimi>()
            .Where(t => t.TenantId == tenantId && t.CikisHareketId == cikisHareketId)
            .OrderBy(t => t.Tarih).ThenBy(t => t.Id)
            .ToListAsync();

        // Parti bazında NET tüketim (pozitif tüketim − önceki iadeler).
        var netler = tuketimler
            .GroupBy(t => t.PartiId)
            .Select(g => new
            {
                PartiId = g.Key,
                Net = g.Sum(x => x.Adet),
                BirimTl = g.Where(x => x.Adet > 0m).Select(x => x.BirimMaliyetTl).FirstOrDefault(),
                BirimHas = g.Where(x => x.Adet > 0m).Select(x => x.BirimMaliyetHas).FirstOrDefault(),
                UrunId = g.First().UrunId,
                SubeId = g.First().SubeId,
            })
            .Where(x => x.Net > 0m)
            .ToList();

        var kalan = Math.Round(adet, 3);
        decimal maliyetliAdet = 0m, maliyetsizAdet = 0m, toplamTl = 0m, toplamHas = 0m;

        foreach (var n in netler)
        {
            if (kalan <= 0m) break;

            var geri = Math.Min(n.Net, kalan);
            if (geri <= 0m) continue;

            var parti = await db.Set<StokPartisi>()
                .FirstOrDefaultAsync(pp => pp.Id == n.PartiId && pp.TenantId == tenantId);
            if (parti is not null)
                parti.AdetKalan = Math.Round(parti.AdetKalan + geri, 3);

            db.Set<StokPartiTuketimi>().Add(new StokPartiTuketimi
            {
                Id = Kimlik.Ulid(),
                TenantId = tenantId,
                PartiId = n.PartiId,
                CikisHareketId = iadeHareketId,   // ters kayıt İADE hareketine bağlanır
                UrunId = n.UrunId,
                SubeId = n.SubeId,
                Adet = Math.Round(-geri, 3),      // NEGATİF: tüketim geri alındı
                BirimMaliyetTl = n.BirimTl,
                BirimMaliyetHas = n.BirimHas,
                Tarih = an,
            });

            if (n.BirimTl is not null)
            {
                maliyetliAdet += geri;
                toplamTl  += n.BirimTl.Value * geri;
                toplamHas += (n.BirimHas ?? 0m) * geri;
            }
            else maliyetsizAdet += geri;

            kalan = Math.Round(kalan - geri, 3);
        }

        // Parti defteri orijinal satışta geride kaldıysa geri konacak parti de yoktur.
        if (kalan > 0m) maliyetsizAdet += kalan;

        return new MaliyetSonucu(
            Math.Round(maliyetliAdet, 3), Math.Round(maliyetsizAdet, 3),
            Math.Round(toplamTl, 2), Math.Round(toplamHas, 6));
    }

    // ── Transfer ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Şubeler arası taşıma: kaynak şubede parti tüketilir, hedef şubede
    /// <b>aynı maliyetle</b> yeni parti açılır.
    ///
    /// ⚠ Transfer KÂR DOĞURMAZ. Maliyet taşınmazsa hedef şube malı bedava almış
    /// görünür ve oradaki satış sahte kâr yazar.
    /// </summary>
    public async Task<MaliyetSonucu> PartiTasi(
        string tenantId, string kaynakSubeId, string hedefSubeId, string urunId,
        decimal adet, decimal hasGramBirim, DateTime an, string yontem,
        string? hareketId = null)
    {
        var tuketim = await PartiTuket(tenantId, kaynakSubeId, urunId, adet, an, yontem, hareketId);

        // Taşınan malın birim maliyeti = tüketilen partilerin ortalaması.
        decimal? birimTl = tuketim.MaliyetliAdet > 0m
            ? Math.Round(tuketim.ToplamMaliyetTl / tuketim.MaliyetliAdet, 4) : null;
        decimal? birimHas = tuketim.MaliyetliAdet > 0m
            ? Math.Round(tuketim.ToplamMaliyetHas / tuketim.MaliyetliAdet, 6) : null;

        // Maliyeti bilinen kısım ve bilinmeyen kısım AYRI partiler olarak gider —
        // birini diğerine karıştırmak bilinmeyeni maliyetli göstermek olurdu.
        if (tuketim.MaliyetliAdet > 0m)
        {
            var p = PartiAc(tenantId, hedefSubeId, urunId, PartiKaynagi.Transfer,
                tuketim.MaliyetliAdet, hasGramBirim, an, birimTl, null, hareketId);
            p.BirimMaliyetHas = birimHas;   // kur yeniden uygulanmaz, maliyet TAŞINIR
        }

        if (tuketim.MaliyetsizAdet > 0m)
            PartiAc(tenantId, hedefSubeId, urunId, PartiKaynagi.Transfer,
                tuketim.MaliyetsizAdet, hasGramBirim, an, null, null, hareketId);

        return tuketim;
    }

    // ── Sorgular ─────────────────────────────────────────────────────────────

    /// <summary>Açık partiler, yönteme göre tüketim sırasında.</summary>
    private async Task<List<StokPartisi>> AcikPartiler(
        string tenantId, string subeId, string urunId, string yontem)
    {
        // ADR-0010: global kiracı filtresi VAR, elle şart da yazılır — tek katman bırakılmaz.
        var sorgu = db.Set<StokPartisi>()
            .Where(p => p.TenantId == tenantId && p.SubeId == subeId
                     && p.UrunId == urunId && p.AdetKalan > 0m);

        // LIFO'da son giren önce çıkar; FIFO ve ORTALAMA'da giriş sırası.
        sorgu = yontem == MaliyetYontemi.Lifo
            ? sorgu.OrderByDescending(p => p.GirisTarihi).ThenByDescending(p => p.Id)
            : sorgu.OrderBy(p => p.GirisTarihi).ThenBy(p => p.Id);

        var kayitli = await sorgu.ToListAsync();

        // Aynı transaction'da henüz kaydedilmemiş partiler (ör. alış + satış
        // aynı istekte) — `StokDegistir`in Local deseniyle aynı gerekçe.
        var yerel = db.Set<StokPartisi>().Local
            .Where(p => p.TenantId == tenantId && p.SubeId == subeId
                     && p.UrunId == urunId && p.AdetKalan > 0m
                     && kayitli.All(k => k.Id != p.Id))
            .ToList();

        if (yerel.Count == 0) return kayitli;

        var hepsi = kayitli.Concat(yerel);
        return (yontem == MaliyetYontemi.Lifo
                ? hepsi.OrderByDescending(p => p.GirisTarihi).ThenByDescending(p => p.Id)
                : hepsi.OrderBy(p => p.GirisTarihi).ThenBy(p => p.Id))
            .ToList();
    }

    /// <summary>Maliyeti bilinen açık partilerin ağırlıklı ortalaması.</summary>
    private static (decimal? Tl, decimal? Has) AgirlikliOrtalama(List<StokPartisi> partiler)
    {
        decimal adet = 0m, tl = 0m, has = 0m;
        foreach (var p in partiler.Where(p => p.BirimMaliyetTl is not null))
        {
            adet += p.AdetKalan;
            tl   += p.BirimMaliyetTl!.Value * p.AdetKalan;
            has  += (p.BirimMaliyetHas ?? 0m) * p.AdetKalan;
        }
        return adet <= 0m
            ? (null, null)
            : (Math.Round(tl / adet, 4), Math.Round(has / adet, 6));
    }

    /// <summary>Kiracının seçili maliyet yöntemi. Tanımsızsa FIFO.</summary>
    public async Task<string> Yontem(string tenantId)
    {
        var t = await db.Tenants.AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => x.MaliyetYontemi)
            .FirstOrDefaultAsync();
        return MaliyetYontemi.Gecerli(t) ? t! : MaliyetYontemi.Fifo;
    }
}
