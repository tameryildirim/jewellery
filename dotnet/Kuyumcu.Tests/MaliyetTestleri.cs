using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Maliyet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// STOK MALİYET PARTİSİ VE KÂR-ZARAR TESTLERİ
//
// Bu katmandan önce kod tabanında HİÇBİR maliyet alanı yoktu; kâr-zarar
// hesaplanamıyordu. Burada sınanan üç şey:
//   1. Maliyet doğru düşülüyor mu (FIFO / LIFO / ORTALAMA)
//   2. HAS bacağı doğru mu — kuyumcunun asıl baktığı yer
//   3. Maliyeti bilinmeyen mal SIFIR maliyetli sayılmıyor mu
//
// (3) en kritiği: sıfır saymak kârı şişirir ve kuyumcuya kazanmadığı parayı
// kazandım dedirtir. Testler bunu ayrı ayrı tutuyor.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MaliyetPartisiTestleri(TestVeritabani vt)
{
    private static MaliyetMotoru Motor(KuyumcuDbContext db) => new(db);

    // ── FIFO ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FIFO_once_giren_partiyi_tuketir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "FIFO");
        using var db = vt.Olustur(k.TenantId);
        var m = Motor(db);
        var t0 = DateTime.UtcNow.AddDays(-2);

        m.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Alis, 10m, 1m, t0,
            birimMaliyetTl: 100m, altinHasFiyati: 1000m);
        m.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Alis, 10m, 1m, t0.AddDays(1),
            birimMaliyetTl: 200m, altinHasFiyati: 1000m);
        await db.SaveChangesAsync();

        // 15 adet satılırsa: 10 × 100 (eski parti) + 5 × 200 (yeni parti) = 2.000
        var s = await m.PartiTuket(k.TenantId, k.SubeId, k.UrunId, 15m, DateTime.UtcNow, MaliyetYontemi.Fifo);
        await db.SaveChangesAsync();

        Assert.Equal(2_000m, s.ToplamMaliyetTl);
        Assert.Equal(15m, s.MaliyetliAdet);
        Assert.Equal(0m, s.MaliyetsizAdet);
    }

    [Fact]
    public async Task LIFO_son_giren_partiyi_tuketir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "LIFO");
        using var db = vt.Olustur(k.TenantId);
        var m = Motor(db);
        var t0 = DateTime.UtcNow.AddDays(-2);

        m.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Alis, 10m, 1m, t0,
            birimMaliyetTl: 100m, altinHasFiyati: 1000m);
        m.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Alis, 10m, 1m, t0.AddDays(1),
            birimMaliyetTl: 200m, altinHasFiyati: 1000m);
        await db.SaveChangesAsync();

        // 15 adet: 10 × 200 (yeni) + 5 × 100 (eski) = 2.500
        var s = await m.PartiTuket(k.TenantId, k.SubeId, k.UrunId, 15m, DateTime.UtcNow, MaliyetYontemi.Lifo);
        await db.SaveChangesAsync();

        Assert.Equal(2_500m, s.ToplamMaliyetTl);
    }

    [Fact]
    public async Task ORTALAMA_agirlikli_ortalamayi_kullanir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Ortalama");
        using var db = vt.Olustur(k.TenantId);
        var m = Motor(db);
        var t0 = DateTime.UtcNow.AddDays(-2);

        // 10 × 100 + 30 × 200 = 7.000 / 40 adet = 175 birim maliyet
        m.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Alis, 10m, 1m, t0,
            birimMaliyetTl: 100m, altinHasFiyati: 1000m);
        m.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Alis, 30m, 1m, t0.AddDays(1),
            birimMaliyetTl: 200m, altinHasFiyati: 1000m);
        await db.SaveChangesAsync();

        var s = await m.PartiTuket(k.TenantId, k.SubeId, k.UrunId, 4m, DateTime.UtcNow, MaliyetYontemi.Ortalama);
        await db.SaveChangesAsync();

        Assert.Equal(700m, s.ToplamMaliyetTl);   // 4 × 175
    }

    // ── HAS bacağı — kuyumculuğun aslı ──────────────────────────────────────

    [Fact]
    public async Task HAS_maliyeti_ALIS_GUNUNUN_kuruyla_hesaplanir()
    {
        // 68.000 TL'lik mal, o gün has 6.800 TL → 10 gram has maliyeti.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Has maliyeti");
        using var db = vt.Olustur(k.TenantId);
        var m = Motor(db);

        var p = m.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Alis, 1m, 10m, DateTime.UtcNow,
            birimMaliyetTl: 68_000m, altinHasFiyati: 6_800m);
        await db.SaveChangesAsync();

        Assert.Equal(10m, p.BirimMaliyetHas);
    }

    [Fact]
    public async Task Kur_yoksa_HAS_maliyeti_UYDURULMAZ()
    {
        // Kur bilinmiyorsa has karşılığı hesaplanamaz. TL maliyeti yazılır,
        // has maliyeti NULL kalır — sıfır yazmak "bedava gram" demek olurdu.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Kursuz maliyet");
        using var db = vt.Olustur(k.TenantId);

        var p = Motor(db).PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Alis, 1m, 10m,
            DateTime.UtcNow, birimMaliyetTl: 68_000m, altinHasFiyati: null);
        await db.SaveChangesAsync();

        Assert.Equal(68_000m, p.BirimMaliyetTl);
        Assert.Null(p.BirimMaliyetHas);
    }

    // ── Maliyeti bilinmeyen — en kritik kural ───────────────────────────────

    [Fact]
    public async Task Maliyeti_BILINMEYEN_parti_SIFIR_maliyetli_sayilmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Bilinmeyen maliyet");
        using var db = vt.Olustur(k.TenantId);
        var m = Motor(db);

        // Devir partisi: maliyeti yok.
        m.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Devir, 10m, 1m, DateTime.UtcNow.AddDays(-1));
        await db.SaveChangesAsync();

        var s = await m.PartiTuket(k.TenantId, k.SubeId, k.UrunId, 10m, DateTime.UtcNow, MaliyetYontemi.Fifo);
        await db.SaveChangesAsync();

        Assert.Equal(0m, s.MaliyetliAdet);
        Assert.Equal(10m, s.MaliyetsizAdet);     // AYRI sayıldı
        Assert.Equal(0m, s.ToplamMaliyetTl);     // toplama katılmadı
        Assert.False(s.TamBilinir);
    }

    [Fact]
    public async Task Karisik_parti_maliyetliyi_ve_maliyetsizi_AYIRIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Karışık parti");
        using var db = vt.Olustur(k.TenantId);
        var m = Motor(db);
        var t0 = DateTime.UtcNow.AddDays(-2);

        m.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Devir, 5m, 1m, t0);   // maliyetsiz
        m.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Alis, 5m, 1m, t0.AddDays(1),
            birimMaliyetTl: 300m, altinHasFiyati: 1000m);
        await db.SaveChangesAsync();

        var s = await m.PartiTuket(k.TenantId, k.SubeId, k.UrunId, 8m, DateTime.UtcNow, MaliyetYontemi.Fifo);
        await db.SaveChangesAsync();

        Assert.Equal(5m, s.MaliyetsizAdet);      // devir partisi
        Assert.Equal(3m, s.MaliyetliAdet);       // alış partisinden 3
        Assert.Equal(900m, s.ToplamMaliyetTl);   // 3 × 300
    }

    [Fact]
    public async Task Parti_yetmezse_satis_DURMAZ_eksik_maliyetsiz_sayilir()
    {
        // Parti defteri stoktan geride kalabilir. Bir muhasebe eksiği yüzünden
        // tezgâhta satışı durdurmak yanlış olurdu.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Parti yetmez");
        using var db = vt.Olustur(k.TenantId);
        var m = Motor(db);

        m.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Alis, 2m, 1m, DateTime.UtcNow.AddDays(-1),
            birimMaliyetTl: 100m, altinHasFiyati: 1000m);
        await db.SaveChangesAsync();

        var s = await m.PartiTuket(k.TenantId, k.SubeId, k.UrunId, 10m, DateTime.UtcNow, MaliyetYontemi.Fifo);
        await db.SaveChangesAsync();

        Assert.Equal(2m, s.MaliyetliAdet);
        Assert.Equal(8m, s.MaliyetsizAdet);
        Assert.Equal(200m, s.ToplamMaliyetTl);
    }

    // ── Kiracı sınırı ───────────────────────────────────────────────────────

    [Fact]
    public async Task BASKA_kiracinin_partisi_tuketilemez()
    {
        var sistem = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(sistem, "Maliyet A");
        var b = TestKurulum.KiraciKur(sistem, "Maliyet B");

        using (var dbB = vt.Olustur(b.TenantId))
        {
            Motor(dbB).PartiAc(b.TenantId, b.SubeId, b.UrunId, PartiKaynagi.Alis, 100m, 1m,
                DateTime.UtcNow.AddDays(-1), birimMaliyetTl: 999m, altinHasFiyati: 1000m);
            await dbB.SaveChangesAsync();
        }

        using var dbA = vt.Olustur(a.TenantId);
        // A, B'nin ürün kimliğiyle tüketmeye çalışsa bile parti görünmez.
        var s = await Motor(dbA).PartiTuket(a.TenantId, a.SubeId, b.UrunId, 5m,
            DateTime.UtcNow, MaliyetYontemi.Fifo);

        Assert.Equal(0m, s.MaliyetliAdet);
        Assert.Equal(5m, s.MaliyetsizAdet);
        Assert.Equal(0m, s.ToplamMaliyetTl);
    }

    // ── Transfer: kâr doğurmamalı ───────────────────────────────────────────

    [Fact]
    public async Task Transfer_maliyeti_TASIR_yeni_maliyet_dogurmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Transfer maliyet");
        var sube2 = TestKurulum.SubeEkle(vt.OlusturSistem(), k.TenantId, "Şube-2");
        using var db = vt.Olustur(k.TenantId);
        var m = Motor(db);

        m.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Alis, 10m, 2m, DateTime.UtcNow.AddDays(-1),
            birimMaliyetTl: 500m, altinHasFiyati: 1000m);
        await db.SaveChangesAsync();

        await m.PartiTasi(k.TenantId, k.SubeId, sube2, k.UrunId, 6m, 2m,
            DateTime.UtcNow, MaliyetYontemi.Fifo);
        await db.SaveChangesAsync();

        var hedef = await db.Set<StokPartisi>().AsNoTracking()
            .SingleAsync(p => p.SubeId == sube2);

        Assert.Equal(PartiKaynagi.Transfer, hedef.Kaynak);
        Assert.Equal(6m, hedef.AdetKalan);
        Assert.Equal(500m, hedef.BirimMaliyetTl);   // maliyet AYNI — bedava görünmüyor
        Assert.Equal(0.5m, hedef.BirimMaliyetHas);  // 500 / 1000
    }

    [Fact]
    public async Task Transfer_maliyetsiz_mali_maliyetli_gostermez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Transfer karışık");
        var sube2 = TestKurulum.SubeEkle(vt.OlusturSistem(), k.TenantId, "Şube-2");
        using var db = vt.Olustur(k.TenantId);
        var m = Motor(db);
        var t0 = DateTime.UtcNow.AddDays(-2);

        m.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Devir, 4m, 1m, t0);
        m.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Alis, 4m, 1m, t0.AddDays(1),
            birimMaliyetTl: 100m, altinHasFiyati: 1000m);
        await db.SaveChangesAsync();

        await m.PartiTasi(k.TenantId, k.SubeId, sube2, k.UrunId, 8m, 1m,
            DateTime.UtcNow, MaliyetYontemi.Fifo);
        await db.SaveChangesAsync();

        var hedefPartiler = await db.Set<StokPartisi>().AsNoTracking()
            .Where(p => p.SubeId == sube2).ToListAsync();

        // İki AYRI parti: 4 maliyetli + 4 maliyetsiz. Karıştırılmadı.
        Assert.Equal(2, hedefPartiler.Count);
        Assert.Equal(4m, hedefPartiler.Single(p => p.BirimMaliyetTl is not null).AdetKalan);
        Assert.Equal(4m, hedefPartiler.Single(p => p.BirimMaliyetTl is null).AdetKalan);
    }

    // ── Yöntem seçimi ───────────────────────────────────────────────────────

    [Fact]
    public async Task Varsayilan_yontem_FIFO_dur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Varsayılan yöntem");
        using var db = vt.Olustur(k.TenantId);
        Assert.Equal(MaliyetYontemi.Fifo, await Motor(db).Yontem(k.TenantId));
    }

    [Fact]
    public async Task Gecersiz_yontem_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Geçersiz yöntem");
        using var db = vt.Olustur(k.TenantId);
        await Assert.ThrowsAsync<BadRequestException>(
            () => Motor(db).PartiTuket(k.TenantId, k.SubeId, k.UrunId, 1m, DateTime.UtcNow, "HEPSI"));
    }

    [Fact]
    public async Task Tuketim_kaydi_maliyeti_KOPYALAR_referans_tutmaz()
    {
        // Parti sonradan düzeltilse bile geçmiş satışın maliyeti değişmemeli.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Maliyet kopyası");
        using var db = vt.Olustur(k.TenantId);
        var m = Motor(db);

        var p = m.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Alis, 10m, 1m,
            DateTime.UtcNow.AddDays(-1), birimMaliyetTl: 100m, altinHasFiyati: 1000m);
        await db.SaveChangesAsync();

        await m.PartiTuket(k.TenantId, k.SubeId, k.UrunId, 5m, DateTime.UtcNow, MaliyetYontemi.Fifo);
        await db.SaveChangesAsync();

        p.BirimMaliyetTl = 9_999m;   // parti sonradan bozuldu
        await db.SaveChangesAsync();

        var tuketim = await db.Set<StokPartiTuketimi>().AsNoTracking().SingleAsync();
        Assert.Equal(100m, tuketim.BirimMaliyetTl);   // geçmiş DEĞİŞMEDİ
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// UÇTAN UCA: alış → satış → kâr-zarar
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class KarZararTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static KarZararServisi Rapor(KuyumcuDbContext db) => new(db);

    private static DateOnly Bugun => DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));

    private static decimal Al(object o, params string[] yol)
    {
        var e = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(o)).RootElement;
        foreach (var a in yol) e = e.GetProperty(a);
        return e.ValueKind == System.Text.Json.JsonValueKind.Null ? 0m : e.GetDecimal();
    }

    private static string Metin(object o, string alan)
    {
        var e = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(o)).RootElement;
        return e.GetProperty(alan).GetString() ?? "";
    }

    [Fact]
    public async Task Alis_satis_zinciri_TL_karini_dogru_hesaplar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Kâr TL");
        using var db = vt.Olustur(k.TenantId);
        var s = Satis(db);

        // 2 adet × 5.000 TL alış
        await s.Alis(new MSatis.AlisOlusturDto
        {
            UrunId = k.UrunId, Adet = 2m, BirimFiyatTl = 5_000m,
            KasaId = k.KasaId, OdenenTl = 10_000m,
            KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

        // 1 adet × 8.000 TL satış → kâr 3.000 TL
        await s.Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 8_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId,
            KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

        var r = await Rapor(db).Donem(k.Islem, Bugun, Bugun, null);

        Assert.Equal(8_000m, Al(r, "hasilatTl"));
        Assert.Equal(5_000m, Al(r, "maliyetTl"));
        Assert.Equal(3_000m, Al(r, "karTl"));
        Assert.Equal(1m, Al(r, "maliyetiBilinenAdet"));
        Assert.Equal(0m, Al(r, "maliyetiBilinmeyenAdet"));
    }

    [Fact]
    public async Task TL_kari_varken_HAS_kaybi_GORUNUR()
    {
        // ⚠ KUYUMCULUĞUN ÖZÜ: TL kazanırken gram kaybetmek.
        // Alış: has 6.800 TL iken 68.000 TL'ye alındı → 10,000 g has maliyeti
        // Satış: has 8.000 TL iken 75.000 TL'ye satıldı → 9,375 g has hasılatı
        // TL kârı  : +7.000 TL  ("kâr ettim")
        // HAS kârı : −0,625 g   (aynı malı yerine koyamaz)
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Has kaybı");
        using var db = vt.Olustur(k.TenantId);

        // Alış günü kuru
        var alisKur = new KurSnapshot
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId,
            AltinHasFiyati = 6_800m, Kaynak = "MANUAL", Olusturuldu = DateTime.UtcNow,
        };
        var satisKur = new KurSnapshot
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId,
            AltinHasFiyati = 8_000m, Kaynak = "MANUAL", Olusturuldu = DateTime.UtcNow,
        };
        db.KurSnapshots.AddRange(alisKur, satisKur);
        await db.SaveChangesAsync();

        var s = Satis(db);
        await s.Alis(new MSatis.AlisOlusturDto
        {
            UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 68_000m,
            KurSnapshotId = alisKur.Id, CariId = k.CariId,
        }, k.Islem);

        await s.Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 75_000m,
            OdemeTuru = "VERESIYE", KurSnapshotId = satisKur.Id, CariId = k.CariId,
        }, k.Islem);

        var r = await Rapor(db).Donem(k.Islem, Bugun, Bugun, null);

        Assert.Equal(7_000m, Al(r, "karTl"));                 // TL'de kâr
        Assert.Equal(10m, Al(r, "maliyetHas"));               // 68.000 / 6.800
        Assert.Equal(9.375m, Al(r, "hasilatHas"));            // 75.000 / 8.000
        Assert.Equal(-0.625m, Al(r, "karHas"));               // ⛔ GRAMDA ZARAR
        Assert.True(Al(r, "karTl") > 0m && Al(r, "karHas") < 0m,
            "Rapor TL kârı gösterirken has kaybını gizliyor — kuyumcuya eridiğini söylemiyor");
    }

    [Fact]
    public async Task Maliyeti_bilinmeyen_satis_kari_SISIRMEZ_ve_UYARI_verir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Devir satışı");
        using var db = vt.Olustur(k.TenantId);

        // TestKurulum stoğu doğrudan yazar, parti açmaz → devir durumu.
        // Devir partisini elle ekle (migration'ın yaptığı şeyin aynısı).
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId,
            PartiKaynagi.Devir, 100m, k.UrunHasGrami, DateTime.UtcNow.AddDays(-1));
        await db.SaveChangesAsync();

        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 8_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId,
            KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

        var r = await Rapor(db).Donem(k.Islem, Bugun, Bugun, null);

        Assert.Equal(8_000m, Al(r, "hasilatTl"));
        Assert.Equal(0m, Al(r, "maliyetTl"));                  // maliyet uydurulmadı
        Assert.Equal(1m, Al(r, "maliyetiBilinmeyenAdet"));     // ama SAKLANMADI
        Assert.Equal("DUSUK", Metin(r, "guvenilirlik"));
        Assert.Contains("maliyeti bilinmiyor", Metin(r, "uyari"));
    }

    [Fact]
    public async Task Transfer_kar_zarara_KARISMAZ()
    {
        // Transfer bir satış değildir; maliyet tüketimi olsa da SMM'ye girmemeli.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Transfer kâr");
        var sube2 = TestKurulum.SubeEkle(vt.OlusturSistem(), k.TenantId, "Şube-2");
        using var db = vt.Olustur(k.TenantId);
        var s = Satis(db);

        await s.Alis(new MSatis.AlisOlusturDto
        {
            UrunId = k.UrunId, Adet = 5m, BirimFiyatTl = 1_000m,
            KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

        await s.Transfer(new MSatis.TransferDto
        {
            UrunId = k.UrunId, Adet = 2m,
            KaynakSubeId = k.SubeId, HedefSubeId = sube2,
            KurSnapshotId = k.KurId,
        }, k.Islem);

        var r = await Rapor(db).Donem(k.Islem, Bugun, Bugun, null);

        Assert.Equal(0m, Al(r, "hasilatTl"));
        Assert.Equal(0m, Al(r, "maliyetTl"));   // transfer SMM değildir
        Assert.Equal(0m, Al(r, "satisAdedi"));
    }

    [Fact]
    public async Task TRANSFER_SONRASI_satis_sahte_kar_YAZMAZ()
    {
        // ⚠ Bu test bir MUTASYON BOŞLUĞUNU kapatmak için yazıldı.
        // `PartiTasi`yi doğrudan çağıran test, SatisServisi.Transfer içindeki
        // bağlantı kaldırıldığında YEŞİL kalıyordu — yani asıl akış korunmuyordu.
        // Burada zincirin tamamı koşuyor: alış → transfer → HEDEF ŞUBEDE satış.
        // Maliyet taşınmazsa hedef şube malı bedava almış olur ve kâr şişer.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Transfer sonrası satış");
        var sube2 = TestKurulum.SubeEkle(vt.OlusturSistem(), k.TenantId, "Şube-2");
        using var db = vt.Olustur(k.TenantId);
        var s = Satis(db);

        await s.Alis(new MSatis.AlisOlusturDto
        {
            UrunId = k.UrunId, Adet = 5m, BirimFiyatTl = 1_000m,
            KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

        await s.Transfer(new MSatis.TransferDto
        {
            UrunId = k.UrunId, Adet = 3m,
            KaynakSubeId = k.SubeId, HedefSubeId = sube2,
            KurSnapshotId = k.KurId,
        }, k.Islem);

        // Hedef şubede satış — kullanıcının şubesi sube2 olmalı
        var sube2Islem = new AktifKullanici(k.KullaniciId, k.TenantId, sube2, "SAHIP");
        await s.Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 2m, BirimFiyatTl = 1_500m,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId,
        }, sube2Islem);

        var r = await Rapor(db).Donem(k.Islem, Bugun, Bugun, null);

        Assert.Equal(3_000m, Al(r, "hasilatTl"));
        Assert.Equal(2_000m, Al(r, "maliyetTl"));   // 2 × 1.000 — maliyet TAŞINDI
        Assert.Equal(1_000m, Al(r, "karTl"));       // 3.000 olsaydı sahte kâr
        Assert.Equal(0m, Al(r, "maliyetiBilinmeyenAdet"));
    }

    [Fact]
    public async Task Stok_degerlemesi_elde_kalan_mali_maliyetiyle_verir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Stok değeri");
        using var db = vt.Olustur(k.TenantId);

        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            UrunId = k.UrunId, Adet = 4m, BirimFiyatTl = 2_500m,
            KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

        var r = await Rapor(db).StokDegerlemesi(k.Islem, null);

        Assert.Equal(4m, Al(r, "maliyetliAdet"));
        Assert.Equal(10_000m, Al(r, "stokMaliyetiTl"));
    }

    [Fact]
    public async Task Urun_bazli_karlilik_siralar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Ürün kârlılığı");
        using var db = vt.Olustur(k.TenantId);
        var s = Satis(db);

        await s.Alis(new MSatis.AlisOlusturDto
        {
            UrunId = k.UrunId, Adet = 3m, BirimFiyatTl = 1_000m,
            KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);
        await s.Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 2m, BirimFiyatTl = 1_800m,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

        var r = await Rapor(db).UrunBazli(k.Islem, Bugun, Bugun, null, 50);
        var json = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(r)).RootElement;
        var satir = json.GetProperty("satirlar")[0];

        Assert.Equal(3_600m, satir.GetProperty("hasilatTl").GetDecimal());
        Assert.Equal(2_000m, satir.GetProperty("maliyetTl").GetDecimal());
        Assert.Equal(1_600m, satir.GetProperty("karTl").GetDecimal());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// SAYIM DEFTERE YAZILIR — TB-18 (devralınan T3 borcu)
//
// Devir denetiminin bulgusu: `SAYIM_DUZELTME` hareket türü tanımlıydı ama hiç
// kullanılmıyordu; sayım stoğu sessizce üzerine yazıyordu. Kaybolan 3 gramı
// kimin ne zaman düştüğü hiçbir yerde görünmüyordu — append-only defter,
// stok gerçeğinin tek kaynağı olma özelliğini sayım anında kaybediyordu.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class SayimDefteriTestleri(TestVeritabani vt)
{
    private static Kuyumcu.Api.Modules.Urun.UrunServisi Urun(KuyumcuDbContext db) => new(db);

    [Fact]
    public async Task Sayim_EKSIGI_deftere_SAYIM_DUZELTME_olarak_yazilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Sayım eksiği");   // 100 adet stok
        using var db = vt.Olustur(k.TenantId);

        await Urun(db).StokSayimGuncelle(k.UrunId, k.SubeId, 97m, k.Islem);

        var h = await db.Hareketler.AsNoTracking()
            .SingleAsync(x => x.Tur == "SAYIM_DUZELTME");

        Assert.Equal(k.UrunId, h.UrunId);
        Assert.Equal(k.SubeId, h.SubeId);
        Assert.Equal(k.KullaniciId, h.KullaniciId);          // KİM düştü
        Assert.Equal(0m, h.TlTutari);                        // para hareketi değil
        Assert.Equal(-3m * k.UrunHasGrami, h.HasGrami);      // eksilen has, işaretiyle
        Assert.Contains("100", h.Aciklama);                  // nereden nereye
        Assert.Contains("97", h.Aciklama);
    }

    [Fact]
    public async Task Sayim_FAZLASI_da_deftere_yazilir_ve_maliyeti_BILINMEZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Sayım fazlası");
        using var db = vt.Olustur(k.TenantId);

        await Urun(db).StokSayimGuncelle(k.UrunId, k.SubeId, 105m, k.Islem);

        var h = await db.Hareketler.AsNoTracking().SingleAsync(x => x.Tur == "SAYIM_DUZELTME");
        Assert.Equal(5m * k.UrunHasGrami, h.HasGrami);

        // Fazla çıkan malın maliyeti bilinmez — sıfır yazmak kârı şişirirdi.
        var parti = await db.Set<StokPartisi>().AsNoTracking()
            .SingleAsync(p => p.Kaynak == PartiKaynagi.SayimFazla);
        Assert.Equal(5m, parti.AdetKalan);
        Assert.Null(parti.BirimMaliyetTl);
    }

    [Fact]
    public async Task Sayim_DEGISMEDIYSE_deftere_hareket_YAZILMAZ()
    {
        // Gürültü üretmemeli: aynı adet girilirse defter kirlenmez.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Sayım değişmedi");
        using var db = vt.Olustur(k.TenantId);

        await Urun(db).StokSayimGuncelle(k.UrunId, k.SubeId, 100m, k.Islem);

        Assert.False(await db.Hareketler.AnyAsync(x => x.Tur == "SAYIM_DUZELTME"));
    }

    [Fact]
    public async Task Sayim_eksigi_maliyet_partisini_de_tuketir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Sayım parti tüketimi");
        using var db = vt.Olustur(k.TenantId);

        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Alis,
            100m, k.UrunHasGrami, DateTime.UtcNow.AddDays(-1),
            birimMaliyetTl: 500m, altinHasFiyati: 1_000m);
        await db.SaveChangesAsync();

        await Urun(db).StokSayimGuncelle(k.UrunId, k.SubeId, 96m, k.Islem);

        var parti = await db.Set<StokPartisi>().AsNoTracking()
            .SingleAsync(p => p.Kaynak == PartiKaynagi.Alis);
        Assert.Equal(96m, parti.AdetKalan);   // 4 adet tüketildi

        var tuketim = await db.Set<StokPartiTuketimi>().AsNoTracking().SingleAsync();
        Assert.Equal(4m, tuketim.Adet);
        Assert.Equal(500m, tuketim.BirimMaliyetTl);
    }

    [Fact]
    public async Task Sayim_kaybi_kar_zarara_SMM_olarak_KARISMAZ()
    {
        // Sayım kaybı satılan malın maliyeti değildir; ayrı bir zarardır.
        // Kâr-zarar raporu yalnız SATIŞ hareketlerinin tüketimini sayar.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Sayım SMM değil");
        using var db = vt.Olustur(k.TenantId);

        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.Alis,
            100m, k.UrunHasGrami, DateTime.UtcNow.AddDays(-1),
            birimMaliyetTl: 500m, altinHasFiyati: 1_000m);
        await db.SaveChangesAsync();

        await Urun(db).StokSayimGuncelle(k.UrunId, k.SubeId, 90m, k.Islem);

        var bugun = DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));
        var r = await new KarZararServisi(db).Donem(k.Islem, bugun, bugun, null);
        var json = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(r)).RootElement;

        Assert.Equal(0m, json.GetProperty("maliyetTl").GetDecimal());
        Assert.Equal(0, json.GetProperty("satisAdedi").GetInt32());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// ALIŞTA ÖDEME ARACI — canlı doğrulamada yakalandı (2026-09-14)
//
// `AlisOlusturDto`'da ödeme aracı alanı YOKTU; `OdemeEtiketi` her peşin ödemeyi
// koşulsuz "NAKIT" yazıyordu. Sonuç: 30.000 TL üstü HAVALELİ alış hiç
// kaydedilemiyordu. Tevsik kapısı doğru çalışıyordu ama girdisi yanlış olduğu
// için meşru banka ödemesini de reddediyordu — kuyumcunun 68.000 TL'lik külçe
// alımı sisteme girmiyordu. Alış girmeyince maliyet partisi de doğmuyordu,
// yani kâr-zarar zinciri en başından kopuyordu.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class AlisOdemeAraciTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MSatis.AlisOlusturDto Dto(Kiraci k, decimal tutar, string odeme) => new()
    {
        UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = tutar,
        KasaId = k.KasaId, OdenenTl = tutar,
        OdemeTuru = odeme, KurSnapshotId = k.KurId, CariId = k.CariId,
    };

    [Fact]
    public async Task HAVALELI_buyuk_alis_KAYDEDILEBILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Havaleli alış");
        using var db = vt.Olustur(k.TenantId);

        // 68.000 TL havale — tevsik zaten sağlanmış, kapı takılmamalı.
        await Satis(db).Alis(Dto(k, 68_000m, "HAVALE"), k.Islem);

        var h = await db.Hareketler.AsNoTracking().SingleAsync(x => x.Tur == "ALIS");
        Assert.Equal("HAVALE", h.OdemeTuru);
    }

    [Fact]
    public async Task NAKIT_buyuk_alis_HALA_DURDURULUR()
    {
        // Regresyon: düzeltme tevsik kapısını gevşetmemeli.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Nakit büyük alış");
        using var db = vt.Olustur(k.TenantId);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).Alis(Dto(k, 68_000m, "NAKIT"), k.Islem));
        Assert.Contains("nakit sınırı", hata.Message);
    }

    [Fact]
    public async Task Odeme_araci_GONDERILMEZSE_NAKIT_sayilir()
    {
        // Fail-closed: alan yoksa en sıkı kapı işler. Varsayılanı HAVALE yapmak
        // eski istemcilerin nakit ödemesini sessizce denetimden kaçırırdı.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Varsayılan nakit");
        using var db = vt.Olustur(k.TenantId);

        var dto = Dto(k, 68_000m, "NAKIT");
        Assert.Equal("NAKIT", new MSatis.AlisOlusturDto().OdemeTuru);   // DTO varsayılanı
        await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).Alis(dto, k.Islem));
    }

    [Fact]
    public async Task Kismi_odemede_defter_KARMA_der()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Kısmi ödeme");
        using var db = vt.Olustur(k.TenantId);

        var dto = Dto(k, 20_000m, "HAVALE");
        dto.OdenenTl = 8_000m;   // yarısı ödendi
        await Satis(db).Alis(dto, k.Islem);

        var h = await db.Hareketler.AsNoTracking().SingleAsync(x => x.Tur == "ALIS");
        Assert.Equal("KARMA", h.OdemeTuru);
    }

    [Fact]
    public async Task Havaleli_alis_maliyet_partisi_ACAR()
    {
        // Asıl bedel buydu: alış girmeyince maliyet partisi doğmuyor,
        // kâr-zarar zinciri en başından kopuyordu.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Havale → parti");
        using var db = vt.Olustur(k.TenantId);

        await Satis(db).Alis(Dto(k, 68_000m, "HAVALE"), k.Islem);

        var parti = await db.Set<StokPartisi>().AsNoTracking()
            .SingleAsync(p => p.Kaynak == PartiKaynagi.Alis);
        Assert.Equal(68_000m, parti.BirimMaliyetTl);
        Assert.NotNull(parti.BirimMaliyetHas);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// MADEN AYRIMI — farklı madenlerin gramı toplanmaz
//
// Bulgu (2026-09-14): `StokDegeri` raporu bütün ürünlerin has gramını tek bir
// `toplamHas` altında topluyordu. 100 g altın + 500 g gümüş = "562,5 g has"
// çıkıyordu. Bu rakam hiçbir şey ifade etmez: "has" 24 ayar SAF ALTIN
// karşılığı demektir, gümüşün has gramı diye bir şey yoktur.
// Platin ve paladyum eklenince sorun daha da büyüyecekti.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class MadenAyrimiTestleri
{
    [Fact]
    public void Altin_ayarlari_ALTIN_madenindedir()
    {
        foreach (var id in new[] { "K8", "K14", "K18", "K21", "K22", "K24", "K995" })
            Assert.Equal(Kuyumcu.Core.Domain.Maden.Altin,
                Kuyumcu.Core.Domain.Ayarlar.MadenBul(id));
    }

    [Fact]
    public void Gumus_ayarlari_ALTIN_sayilmaz()
    {
        // Eski modelde her ayar örtük olarak altındı; gümüş de has grama
        // dönüşüp altın toplamına karışıyordu.
        foreach (var id in new[] { "G800", "G925", "G950", "G999" })
            Assert.Equal(Kuyumcu.Core.Domain.Maden.Gumus,
                Kuyumcu.Core.Domain.Ayarlar.MadenBul(id));
    }

    [Theory]
    [InlineData("PT950", "PLATIN", 950)]
    [InlineData("PT900", "PLATIN", 900)]
    [InlineData("PD999", "PALADYUM", 999)]
    [InlineData("K995", "ALTIN", 995)]
    [InlineData("G999", "GUMUS", 999)]
    public void Yeni_ayarlar_dogru_maden_ve_milyemle_tanimli(string id, string maden, int milyem)
    {
        var a = Kuyumcu.Core.Domain.Ayarlar.Bul(id);
        Assert.NotNull(a);
        Assert.Equal(maden, a!.Maden);
        Assert.Equal(milyem, a.Milyem);
    }

    [Fact]
    public void Bilinmeyen_ayarin_madeni_UYDURULMAZ()
        => Assert.Null(Kuyumcu.Core.Domain.Ayarlar.MadenBul("XX999"));

    [Fact]
    public void Madenin_ayarlari_milyeme_gore_siralanir()
    {
        var platin = Kuyumcu.Core.Domain.Ayarlar.MadeninAyarlari("PLATIN").ToList();
        Assert.Equal(4, platin.Count);
        Assert.Equal("PT850", platin[0].Id);
        Assert.Equal("PT999", platin[^1].Id);
    }

    [Fact]
    public void Dogrulama_deseni_tum_ayarlari_kapsar()
    {
        // Desen ile tablo birbirinden kopmamalı: tabloya ayar eklenip desene
        // eklenmezse o ayar API'de reddedilir ve kimse fark etmez.
        var desendekiler = Kuyumcu.Core.Domain.Ayarlar.Desen.Split('|').ToHashSet();
        foreach (var id in Kuyumcu.Core.Domain.Ayarlar.Tumu.Keys)
            Assert.Contains(id, desendekiler);
        Assert.Equal(Kuyumcu.Core.Domain.Ayarlar.Tumu.Count, desendekiler.Count);
    }

    [Fact]
    public void Has_hesabi_maden_farketmeksizin_milyemi_uygular()
    {
        // 10 gram 950 platin → 9,5 gram saf platin. Formül aynı, ama sonuç
        // "has altın" DEĞİLDİR — maden alanı bunu ayırt etmek için var.
        Assert.Equal(9.5m, Kuyumcu.Core.Domain.HasHesap.HasGramHesapla(10m, 0m, "PT950"));
    }
}

[Collection("veritabani")]
public sealed class StokMadenDagilimiTestleri(TestVeritabani vt)
{
    [Fact]
    public async Task Stok_raporu_altin_ve_gumusu_TOPLAMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Maden karışımı");   // K22 altın, 100 adet
        using var db = vt.Olustur(k.TenantId);

        // Gümüş ürün ekle
        var gumusId = Kimlik.Ulid();
        var gumusHas = Kuyumcu.Core.Domain.HasHesap.HasGramHesapla(10m, 0m, "G925");
        db.Urunler.Add(new Kuyumcu.Api.Data.Urun
        {
            Id = gumusId, TenantId = k.TenantId, Tip = "GUMUS", Kod = "GM-1", Ad = "Gümüş kolye",
            AyarId = "G925", Milyem = 925m, GramAgirlik = 10m, TasAgirlik = 0m,
            HasGrami = gumusHas, Aktif = true,
            Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        db.Stoklar.Add(new Stok
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, UrunId = gumusId,
            Adet = 10m, ToplamHasGram = 10m * gumusHas, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var r = await TestKurulum.RaporServisi(db).StokDegeri(k.Islem, null);
        var j = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(r)).RootElement;

        var altinHas = 100m * k.UrunHasGrami;
        var gumusGram = 10m * gumusHas;

        // toplamHas YALNIZ altını sayar — gümüş karışmaz
        Assert.Equal(altinHas, j.GetProperty("toplamHas").GetDecimal());
        Assert.NotEqual(altinHas + gumusGram, j.GetProperty("toplamHas").GetDecimal());

        var dagilim = j.GetProperty("madenDagilimi").EnumerateArray().ToList();
        Assert.Equal(2, dagilim.Count);
        Assert.Contains(dagilim, x => x.GetProperty("maden").GetString() == "GUMUS"
                                   && x.GetProperty("toplamGram").GetDecimal() == gumusGram);
        Assert.Contains("toplanmaz", j.GetProperty("uyari").GetString());
    }

    [Fact]
    public async Task Tek_maden_varsa_UYARI_verilmez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tek maden");
        using var db = vt.Olustur(k.TenantId);

        var r = await TestKurulum.RaporServisi(db).StokDegeri(k.Islem, null);
        var j = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(r)).RootElement;

        Assert.Equal(System.Text.Json.JsonValueKind.Null, j.GetProperty("uyari").ValueKind);
    }
}
