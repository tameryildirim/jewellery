using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Yerlesim;

// ═════════════════════════════════════════════════════════════════════════════
// YERLEŞİM SERVİSİ — paket 25 Faz A. Stok yerleşimine yazan TEK MERKEZ.
//
// Neden tek merkez: `stoklar` tablosuna iki yol yazıyor (SatisModulu.StokDegistir
// ve UrunModulu.StokSayimGuncelle). Yerleşim kırılımı üçüncü bir yol açarsa
// I1 değişmezi (Σ yerleşim ≤ stok) üç ayrı yerde korunmak zorunda kalırdı.
//
// DEĞİŞMEZLER
//   I1  Σ stok_yerlesimleri.adet ≤ stoklar.adet — ihlal 400.
//   I2  Yerleşim adedi negatif olamaz; 0'a düşen SATIR SİLİNİR (çöp bırakılmaz).
//       Taşıma izi denetim_kayitlari'nda kalır, satırda değil.
//   I3  Reyon şubesine kilitlidir: reyon.SubeId != subeId → 400.
//   I4  Pasif reyona yerleştirme/taşıma 400.
//   I5  Çıkışta reyon verildiyse ve o reyon yetersizse 400 — başka reyondan
//       SESSİZCE alınmaz (fail-closed).
//   I6  Çıkışta reyon verilmediyse: önce ATANMAMIŞ tükenir, sonra reyonlar
//       Ad artan sırada; uyarı metni döner ve ekranda gösterilir (yutulmaz).
//
// Reyon içi hareket deftere YAZILMAZ (ADR-0016): adet, has ve para değişmez.
// Tüm sorgularda ELLE TenantId şartı vardır (ADR-0010); IgnoreQueryFilters,
// Find/FindAsync ve ExecuteUpdate/ExecuteDelete KULLANILMAZ.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class YerlesimServisi(KuyumcuDbContext db, DenetimServisi? denetim = null)
{
    private static decimal Yuvarla3(decimal d) => Math.Round(d, 3, MidpointRounding.AwayFromZero);

    // ── Okuma ────────────────────────────────────────────────────────────────

    /// <summary>Şube+ürün için yerleşime dağıtılmış toplam adet.</summary>
    public async Task<decimal> YerlesikToplam(string tenantId, string subeId, string urunId)
    {
        var kayitli = await db.StokYerlesimleri.AsNoTracking()
            .Where(y => y.TenantId == tenantId && y.SubeId == subeId && y.UrunId == urunId)
            .SumAsync(y => (decimal?)y.Adet) ?? 0m;
        // Aynı transaction içinde henüz kaydedilmemiş satırlar (SatisModulu.cs
        // Local deseni) — AsNoTracking sorgusu onları göremez.
        var yerel = db.StokYerlesimleri.Local
            .Where(y => y.TenantId == tenantId && y.SubeId == subeId && y.UrunId == urunId)
            .ToList();
        if (yerel.Count == 0) return Yuvarla3(kayitli);

        var kayitliIdler = await db.StokYerlesimleri.AsNoTracking()
            .Where(y => y.TenantId == tenantId && y.SubeId == subeId && y.UrunId == urunId)
            .Select(y => y.Id).ToListAsync();
        var toplam = kayitli;
        foreach (var y in yerel)
        {
            var giris = db.Entry(y);
            if (giris.State == EntityState.Added) toplam += y.Adet;
            else if (kayitliIdler.Contains(y.Id)) toplam += y.Adet - giris.OriginalValues.GetValue<decimal>(nameof(StokYerlesimi.Adet));
        }
        return Yuvarla3(toplam);
    }

    /// <summary>Şubedeki stok adedi (`stoklar` — tek gerçek kaynak).</summary>
    public async Task<decimal> StokAdedi(string tenantId, string subeId, string urunId) =>
        (db.Stoklar.Local.FirstOrDefault(s => s.TenantId == tenantId && s.SubeId == subeId && s.UrunId == urunId)
         ?? await db.Stoklar.AsNoTracking()
             .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.SubeId == subeId && s.UrunId == urunId))
        ?.Adet ?? 0m;

    /// <summary>ATANMAMIŞ = stoklar.adet − Σ yerleşim. SAKLANMAZ, HESAPLANIR.</summary>
    public async Task<decimal> Atanmamis(string tenantId, string subeId, string urunId) =>
        Yuvarla3(await StokAdedi(tenantId, subeId, urunId)
                 - await YerlesikToplam(tenantId, subeId, urunId));

    // ── Doğrulayıcılar ───────────────────────────────────────────────────────

    /// <summary>Reyon kiracıya ait mi, istenen şubede mi, aktif mi.</summary>
    public async Task<Reyon> ReyonKapisi(string tenantId, string subeId, string reyonId)
    {
        var reyon = await db.Set<Reyon>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reyonId && r.TenantId == tenantId)
            // Yabancı kiracının reyonu: 404 — varlığı sızdırılmaz (Mehmet koşulu).
            ?? throw new NotFoundException("Reyon bulunamadı");

        if (reyon.SubeId != subeId)
            throw new BadRequestException(
                "Reyon başka şubeye ait — şubeler arası taşıma reyon taşıma ucuyla YAPILAMAZ, transfer ucuyla yapılır.");
        if (!reyon.Aktif)
            throw new BadRequestException("Reyon pasif — pasif reyona mal yerleştirilemez.");
        return reyon;
    }

    private static void AdetKapisi(decimal adet)
    {
        if (adet <= 0m) throw new BadRequestException("Adet sıfırdan büyük olmalıdır");
    }

    // ── Satır yardımcıları ───────────────────────────────────────────────────

    private async Task<StokYerlesimi?> Satir(string tenantId, string subeId, string urunId, string reyonId) =>
        db.StokYerlesimleri.Local.FirstOrDefault(y =>
            y.TenantId == tenantId && y.SubeId == subeId && y.UrunId == urunId && y.ReyonId == reyonId)
        ?? await db.StokYerlesimleri.FirstOrDefaultAsync(y =>
            y.TenantId == tenantId && y.SubeId == subeId && y.UrunId == urunId && y.ReyonId == reyonId);

    private async Task Ekle(string tenantId, string subeId, string urunId, string reyonId,
                            decimal adet, DateTime now)
    {
        var satir = await Satir(tenantId, subeId, urunId, reyonId);
        if (satir is null)
        {
            db.StokYerlesimleri.Add(new StokYerlesimi
            {
                Id = Kimlik.Ulid(), TenantId = tenantId, SubeId = subeId, UrunId = urunId,
                ReyonId = reyonId, Adet = Yuvarla3(adet), Guncellendi = now,
            });
        }
        else
        {
            satir.Adet = Yuvarla3(satir.Adet + adet);
            satir.Guncellendi = now;
        }
    }

    /// <summary>Reyondan düşer; 0'a inen satırı siler (I2). Yetersizse false döner.</summary>
    private async Task<bool> Dus(string tenantId, string subeId, string urunId, string reyonId,
                                 decimal adet, DateTime now)
    {
        var satir = await Satir(tenantId, subeId, urunId, reyonId);
        if (satir is null || satir.Adet < adet) return false;

        var kalan = Yuvarla3(satir.Adet - adet);
        if (kalan <= 0m) db.StokYerlesimleri.Remove(satir);
        else { satir.Adet = kalan; satir.Guncellendi = now; }
        return true;
    }

    // ── Yazma yolları ────────────────────────────────────────────────────────

    /// <summary>ATANMAMIŞ maldan reyona yerleştirir (I1 kapısı burada).</summary>
    public async Task Yerlestir(AktifKullanici islem, string subeId, string urunId, string reyonId,
                                decimal adet, DateTime now)
    {
        AdetKapisi(adet);
        await IslemDogrulayici.SubeDogrula(db, subeId, islem.TenantId);
        await UrunKapisi(islem.TenantId, urunId, adet);
        var reyon = await ReyonKapisi(islem.TenantId, subeId, reyonId);

        var atanmamis = await Atanmamis(islem.TenantId, subeId, urunId);
        if (adet > atanmamis)
            throw new BadRequestException(
                $"Yerleştirilecek adet atanmamış maldan fazla olamaz — atanmamış: {atanmamis:0.###}");

        await Ekle(islem.TenantId, subeId, urunId, reyonId, adet, now);
        denetim?.YazmaKaydet(islem, nameof(StokYerlesimi), urunId,
            $"reyon yerleştirme · şube {subeId} · reyon {reyon.Ad} · {adet:0.###} adet", hassas: false);
    }

    /// <summary>Aynı şube içinde reyon→reyon taşır. Toplam stok DEĞİŞMEZ.</summary>
    public async Task Tasi(AktifKullanici islem, string subeId, string urunId,
                           string kaynakReyonId, string hedefReyonId, decimal adet,
                           string? aciklama, DateTime now)
    {
        AdetKapisi(adet);
        if (kaynakReyonId == hedefReyonId)
            throw new BadRequestException("Kaynak ve hedef reyon aynı olamaz");

        await IslemDogrulayici.SubeDogrula(db, subeId, islem.TenantId);
        await UrunKapisi(islem.TenantId, urunId, adet);
        var kaynak = await ReyonKapisi(islem.TenantId, subeId, kaynakReyonId);
        var hedef = await ReyonKapisi(islem.TenantId, subeId, hedefReyonId);

        if (!await Dus(islem.TenantId, subeId, urunId, kaynakReyonId, adet, now))
            throw new BadRequestException(
                $"'{kaynak.Ad}' reyonunda yeterli mal yok — taşınamaz");

        await Ekle(islem.TenantId, subeId, urunId, hedefReyonId, adet, now);
        denetim?.YazmaKaydet(islem, nameof(StokYerlesimi), urunId,
            $"reyon taşıma · şube {subeId} · {kaynak.Ad} → {hedef.Ad} · {adet:0.###} adet"
            + (string.IsNullOrWhiteSpace(aciklama) ? "" : $" · {aciklama.Trim()}"), hassas: false);
    }

    /// <summary>Reyondan ATANMAMIŞ'a geri çeker (yanlış yerleştirmenin düzeltme yolu).</summary>
    public async Task GeriAl(AktifKullanici islem, string subeId, string urunId, string reyonId,
                             decimal adet, DateTime now)
    {
        AdetKapisi(adet);
        await IslemDogrulayici.SubeDogrula(db, subeId, islem.TenantId);
        await UrunKapisi(islem.TenantId, urunId, adet);
        // Geri alma PASİF reyondan da yapılabilmelidir: reyonu boşaltmanın yolu
        // budur. Bu yüzden ReyonKapisi'nın aktiflik şartı burada uygulanmaz,
        // kiracı ve şube şartı uygulanır.
        var reyon = await db.Set<Reyon>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reyonId && r.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Reyon bulunamadı");
        if (reyon.SubeId != subeId) throw new BadRequestException("Reyon başka şubeye ait");

        if (!await Dus(islem.TenantId, subeId, urunId, reyonId, adet, now))
            throw new BadRequestException($"'{reyon.Ad}' reyonunda yeterli mal yok — geri alınamaz");

        denetim?.YazmaKaydet(islem, nameof(StokYerlesimi), urunId,
            $"reyondan geri alma · şube {subeId} · reyon {reyon.Ad} · {adet:0.###} adet", hassas: false);
    }

    // ── Stok akışlarının yerleşim bacağı ─────────────────────────────────────

    /// <summary>
    /// Stok ÇIKIŞININ yerleşim bacağı. <paramref name="yeniStokAdedi"/> çıkış
    /// sonrası `stoklar.adet` değeridir (çağıran hesaplar; sorgudan okunmaz —
    /// aynı transaction içinde henüz kaydedilmemiş olabilir).
    ///
    /// reyonId verilmişse YALNIZ o reyondan düşer (I5). Verilmemişse önce
    /// ATANMAMIŞ tükenir (yani hiç yerleşim düşmez), yetmezse reyonlar Ad artan
    /// sırada tüketilir ve UYARI metni döner (I6).
    /// </summary>
    public async Task<string?> CikisDus(string tenantId, string subeId, string urunId,
                                        decimal adet, string? reyonId, decimal yeniStokAdedi,
                                        DateTime now)
    {
        if (adet <= 0m) return null;

        if (!string.IsNullOrEmpty(reyonId))
        {
            var reyon = await db.Set<Reyon>().AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == reyonId && r.TenantId == tenantId)
                ?? throw new NotFoundException("Reyon bulunamadı");
            if (reyon.SubeId != subeId)
                throw new BadRequestException("Reyon bu şubeye ait değil");

            if (!await Dus(tenantId, subeId, urunId, reyonId, adet, now))
                throw new BadRequestException(
                    $"'{reyon.Ad}' reyonunda yeterli mal yok — çıkış başka reyondan yapılmaz. "
                    + "Önce reyona mal yerleştirin ya da reyon seçmeden işlem yapın.");
            return null;
        }

        // Reyon seçilmedi: atanmamış mal çıkışı zaten karşılıyorsa yerleşim
        // satırlarına DOKUNULMAZ. Karşılamıyorsa fazlası reyonlardan tüketilir.
        var fazla = Yuvarla3(await YerlesikToplam(tenantId, subeId, urunId) - yeniStokAdedi);
        if (fazla <= 0m) return null;

        var reyonlar = await db.StokYerlesimleri
            .Where(y => y.TenantId == tenantId && y.SubeId == subeId && y.UrunId == urunId)
            .Join(db.Set<Reyon>().Where(r => r.TenantId == tenantId),
                  y => y.ReyonId, r => r.Id, (y, r) => new { Yerlesim = y, Reyon = r })
            .OrderBy(x => x.Reyon.Ad)
            .ToListAsync();

        var tuketilen = new List<string>();
        foreach (var x in reyonlar)
        {
            if (fazla <= 0m) break;
            var duser = Math.Min(fazla, x.Yerlesim.Adet);
            if (duser <= 0m) continue;
            await Dus(tenantId, subeId, urunId, x.Yerlesim.ReyonId, duser, now);
            tuketilen.Add($"{x.Reyon.Ad} ({duser:0.###})");
            fazla = Yuvarla3(fazla - duser);
        }

        return tuketilen.Count == 0 ? null
            : "Reyon seçilmediği için mal şu reyonlardan düşüldü: " + string.Join(", ", tuketilen)
              + ". Doğru reyondan düşmesi için işlemde reyon seçin.";
    }

    /// <summary>
    /// Stok GİRİŞİNİN yerleşim bacağı. reyonId verilmemişse ürünün VARSAYILAN
    /// reyonu (urunler.reyon_id) kullanılır — ancak o reyon BU şubeye ait ve
    /// aktifse. Değilse mal ATANMAMIŞ'a girer (sessizce yanlış şubeye yazılmaz).
    /// </summary>
    public async Task<string?> GirisEkle(string tenantId, string subeId, string urunId,
                                         decimal adet, string? reyonId, string? varsayilanReyonId,
                                         DateTime now)
    {
        if (adet <= 0m) return null;

        if (!string.IsNullOrEmpty(reyonId))
        {
            var reyon = await ReyonKapisi(tenantId, subeId, reyonId);
            await Ekle(tenantId, subeId, urunId, reyon.Id, adet, now);
            return null;
        }

        if (!string.IsNullOrEmpty(varsayilanReyonId))
        {
            var varsayilan = await db.Set<Reyon>().AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == varsayilanReyonId && r.TenantId == tenantId);
            if (varsayilan is { Aktif: true } && varsayilan.SubeId == subeId)
            {
                await Ekle(tenantId, subeId, urunId, varsayilan.Id, adet, now);
                return null;
            }
        }

        return "Mal ATANMAMIŞ olarak girdi — fiziksel yeri belirtilmedi. "
             + "Ürün kartından reyona yerleştirebilirsiniz.";
    }

    private async Task UrunKapisi(string tenantId, string urunId, decimal adet)
    {
        // PAKET GOC-C (#6 · K14): yerleştirme/taşıma/geri almada da ADET birimli üründe tam sayı.
        var birim = await db.Urunler.AsNoTracking()
            .Where(u => u.Id == urunId && u.TenantId == tenantId).Select(u => u.Birim).FirstOrDefaultAsync();
        if (birim is null) throw new NotFoundException("Ürün bulunamadı");
        Urun.UrunAdetKapisi.Dogrula(birim, adet, "adet");
    }

    // ── Rapor/okuma uçlarının gövdesi ────────────────────────────────────────

    /// <summary>Reyonun içeriği — sunucuda GroupBy, sayfalı (TB-19).</summary>
    public async Task<object> ReyonIcerigi(AktifKullanici islem, string reyonId, int limit, int ofset)
    {
        limit = Math.Clamp(limit, 1, 500);
        ofset = Math.Max(ofset, 0);

        var reyon = await db.Set<Reyon>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reyonId && r.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Reyon bulunamadı");

        var temel = db.StokYerlesimleri.AsNoTracking()
            .Where(y => y.TenantId == islem.TenantId && y.ReyonId == reyonId)
            .Join(db.Urunler.Where(u => u.TenantId == islem.TenantId),
                  y => y.UrunId, u => u.Id, (y, u) => new { y, u });

        var toplamSatir = await temel.CountAsync();
        var ham = await temel
            .OrderBy(x => x.u.Ad)
            .Skip(ofset).Take(limit)
            .Select(x => new
            {
                urunId = x.u.Id,
                kod = x.u.Kod,
                ad = x.u.Ad,
                birim = x.u.Birim,
                ayarId = x.u.AyarId,
                adet = x.y.Adet,
                hasGrami = x.u.HasGrami,
            })
            .ToListAsync();

        // Maden ayar kodundan TÜRETİLİR (RaporModulu.StokDegeri ile aynı kaynak);
        // ürün kartında ayrı bir maden kolonu YOKTUR.
        var satirlar = ham.Select(x => new
        {
            x.urunId, x.kod, x.ad, x.birim, x.ayarId,
            maden = Kuyumcu.Core.Domain.Ayarlar.MadenBul(x.ayarId) ?? Kuyumcu.Core.Domain.Maden.Altin,
            x.adet,
            toplamHasGram = Math.Round(x.adet * x.hasGrami, 6),
        }).ToList();

        return new
        {
            reyonId = reyon.Id,
            reyonAdi = reyon.Ad,
            tip = reyon.Tip,
            subeId = reyon.SubeId,
            aktif = reyon.Aktif,
            toplamSatir,
            limit,
            ofset,
            sinirdaMi = ofset + satirlar.Count < toplamSatir,
            satirlar,
        };
    }

    /// <summary>Ürünün şube × reyon dağılımı + her şube için ATANMAMIŞ.</summary>
    public async Task<object> UrunYerlesimi(AktifKullanici islem, string urunId)
    {
        var urun = await db.Urunler.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == urunId && u.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Ürün bulunamadı");

        var stoklar = await db.Stoklar.AsNoTracking()
            .Where(s => s.TenantId == islem.TenantId && s.UrunId == urunId)
            .Select(s => new { s.SubeId, s.Adet })
            .ToListAsync();

        var yerlesimler = await db.StokYerlesimleri.AsNoTracking()
            .Where(y => y.TenantId == islem.TenantId && y.UrunId == urunId)
            .Join(db.Set<Reyon>().Where(r => r.TenantId == islem.TenantId),
                  y => y.ReyonId, r => r.Id, (y, r) => new
                  {
                      y.SubeId, y.ReyonId, r.Ad, r.Tip, r.Aktif, y.Adet,
                  })
            .ToListAsync();

        var subeAdlari = await db.Subeler.AsNoTracking()
            .Where(s => s.TenantId == islem.TenantId)
            .ToDictionaryAsync(s => s.Id, s => s.Ad);

        var subeIdler = stoklar.Select(s => s.SubeId)
            .Union(yerlesimler.Select(y => y.SubeId)).Distinct().ToList();

        var subeler = subeIdler.Select(subeId =>
        {
            var stokAdet = stoklar.FirstOrDefault(s => s.SubeId == subeId)?.Adet ?? 0m;
            var satirlar = yerlesimler.Where(y => y.SubeId == subeId)
                .OrderBy(y => y.Ad)
                .Select(y => new
                {
                    reyonId = y.ReyonId, reyonAdi = y.Ad, tip = y.Tip, aktif = y.Aktif,
                    adet = y.Adet, toplamHasGram = Math.Round(y.Adet * urun.HasGrami, 6),
                }).ToList();
            var yerlesik = satirlar.Sum(s => s.adet);
            return new
            {
                subeId,
                subeAdi = subeAdlari.GetValueOrDefault(subeId, subeId),
                stokAdedi = stokAdet,
                yerlesikAdet = Yuvarla3(yerlesik),
                // ATANMAMIŞ hesaplanır, saklanmaz. Negatife düşemez (I1).
                atanmamis = Yuvarla3(stokAdet - yerlesik),
                reyonlar = satirlar,
            };
        }).OrderBy(s => s.subeAdi).ToList();

        return new
        {
            urunId = urun.Id, kod = urun.Kod, ad = urun.Ad, birim = urun.Birim,
            hasGrami = urun.HasGrami,
            toplamStok = stoklar.Sum(s => s.Adet),
            subeler,
        };
    }
}
