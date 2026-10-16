using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MMasak = Kuyumcu.Api.Modules.Masak;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// MASAK — BAĞLANTILI İŞLEM KÜMÜLATİFİ (eşik bölme / structuring)
//
// TESPİT (mevzuat araştırması 2026-09-15, 8.5/10, iki hasım mercek de
// doğruladı): Tedbirler Yönetmeliği m.5/1-(b) eşiği "işlem tutarı YA DA
// birbiriyle bağlantılı birden fazla işlemin TOPLAM tutarı" üzerinden kurar.
//
// Kapı bugüne kadar çağrı BAŞINA bakıyordu: aynı müşteriye aynı gün 3 ayrı
// fişle 150.000'er TL satıldığında hiçbiri 185.000'e değmiyor, kimlik tespiti
// HİÇ tetiklenmiyordu. Eşik bölme tam olarak budur.
//
// ⚠ Eşik operatörü ">=" dir (185.000,00 TL'nin KENDİSİ tetikler); tevsik
// haddindeki ">" ile karıştırılmamalıdır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MasakBaglantiliIslemTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));

    private static MMasak.MasakBilgiDto TamKimlik() => new()
    {
        BelgeTuru = KimlikBelgeTuru.TcKimlik, BelgeNo = "10000000146", AdSoyad = "Ayşe Demir",
        DogumTarihi = new DateOnly(1985, 4, 12), Uyruk = "TUR",
        IslemAmaci = "Yatırım", Aciklama = "Birikim değerlendirmek için altın alımı",
        FonKaynagi = "Maaş birikimi", EkBelgeReferansi = "BELGE-2026-001",
    };

    /// <summary>Eşik altı, kimliksiz satış — HAVALE (bu büyüklük yasal olarak nakit olamaz).</summary>
    private static MSatis.SatisOlusturDto Dto(
        Kiraci k, decimal tutar, MMasak.MasakBilgiDto? masak = null) => new()
    {
        CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = tutar,
        OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId, Masak = masak,
    };

    [Fact]
    public async Task Ayni_gun_bolunen_satislar_TOPLANIR_ve_kimlik_ISTENIR()
    {
        // 3 × 100.000 = 300.000. Tek tek hiçbiri 185.000'e değmiyor.
        // Eskiden üçü de kimliksiz geçiyordu; artık ÜÇÜNCÜSÜ durur.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "MASAK eşik bölme", stokAdet: 20m);
        using var db = vt.Olustur(k);

        await Satis(db).Satis(Dto(k, 100_000m), k.Islem);   // toplam 100.000 → serbest
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).Satis(Dto(k, 100_000m), k.Islem));   // toplam 200.000 ⛔

        Assert.Contains("MASAK", hata.Message);
        // ⚠ UX-4d madde 4: kümülatif notu TEK BİÇİM oldu — ilk denemede de,
        // ikinci denemede de aynı cümle basılır (eskiden ilkinde hiç yoktu).
        Assert.Contains("Bu cariyle bugün kümülatif", hata.Message);
        Assert.Contains("200.000,00 TL", hata.Message);
        Assert.Contains("200.000,00", hata.Message);
    }

    [Fact]
    public async Task Bolunen_satis_KIMLIK_verilirse_gecer()
    {
        // Kural kimlik ister, satışı yasaklamaz.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "MASAK bölme kimlikli", stokAdet: 20m);
        using var db = vt.Olustur(k);

        await Satis(db).Satis(Dto(k, 100_000m), k.Islem);
        await Satis(db).Satis(Dto(k, 100_000m, TamKimlik()), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var tespit = kontrol.MasakKimlikTespitleri.Single(m => m.TenantId == k.TenantId);
        // Kayda geçen tutar İŞLEMİN KENDİ tutarıdır; eşik kümülatiften değerlendirilir.
        Assert.Equal(100_000m, tespit.IslemTutariTl);
    }

    [Fact]
    public async Task Esik_alti_kalan_toplam_kimlik_ISTEMEZ()
    {
        // 2 × 90.000 = 180.000 < kimlik isteyen en düşük tutar (07.10.2026'dan itibaren 200.000;
        // öncesi 185.000) → kural meşru satışı durdurmamalı.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "MASAK eşik altı toplam", stokAdet: 20m);
        using var db = vt.Olustur(k);

        await Satis(db).Satis(Dto(k, 90_000m), k.Islem);
        await Satis(db).Satis(Dto(k, 90_000m), k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(2, kontrol.Hareketler.Count(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
        Assert.Empty(kontrol.MasakKimlikTespitleri.Where(m => m.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Tam_esikte_kumulatif_TETIKLER()
    {
        // MASAK-370: 100.000 + 100.000 = 200.000 — 07.10.2026'dan itibaren kimliği fiilen tetikleyen
        // amaç beyanı eşiği (TEYİTSİZ, kurucu md.15 (a)). Operatör ">=": eşiğin KENDİSİ tetikler.
        // (md.5/1-b 370.000 sınırı ve 2 × 185.000 bağlantılı toplamı Masak370Testleri'nde.)
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "MASAK tam eşik", stokAdet: 20m);
        using var db = vt.Olustur(k);

        await Satis(db).Satis(Dto(k, 100_000m), k.Islem);
        await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).Satis(Dto(k, 100_000m), k.Islem));
    }

    [Fact]
    public async Task BASKA_musterinin_islemi_toplama_KATILMAZ()
    {
        // "Bağlantılı işlem" aynı kişiyle olandır; başkasının alışverişi sayılmaz.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "MASAK başka müşteri", stokAdet: 20m);
        using var db = vt.Olustur(k);
        await Satis(db).Satis(Dto(k, 100_000m), k.Islem);

        // İkinci cari doğrudan kurulur: testin konusu kümülatifin MÜŞTERİ
        // bazlı olması, cari oluşturma ucunun dönüş şekli değil.
        var ikinciId = Kimlik.Ulid();
        using (var kur = vt.OlusturSistem())
        {
            var now = DateTime.UtcNow;
            kur.Cariler.Add(new Cari
            {
                Id = ikinciId, TenantId = k.TenantId, Tur = "MUSTERI", Ad = "İkinci Müşteri",
                Aktif = true, Olusturuldu = now, Guncellendi = now,
            });
            kur.SaveChanges();
        }

        var dto = Dto(k, 100_000m);
        dto.CariId = ikinciId;
        await Satis(db).Satis(dto, k.Islem);   // kendi toplamı 100.000 → serbest

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(2, kontrol.Hareketler.Count(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
    }

    [Fact]
    public async Task Farkli_islem_turleri_de_AYNI_toplama_girer()
    {
        // Bölme yalnız satışla yapılmaz: 100.000 satış + 100.000 tahsilat = 200.000 (MASAK-370:
        // bugünkü en düşük kimlik tetikleyicisi; eski metin 90.000 → 190.000 idi, 185.000 dönemi).
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "MASAK karışık tür", stokAdet: 20m);
        using var db = vt.Olustur(k);

        await Satis(db).Satis(Dto(k, 100_000m), k.Islem);

        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Cari(db).TahsilatGir(k.CariId, new MCari.TahsilatGirisDto
            {
                TlTutari = 100_000m, OdemeTuru = "HAVALE", KurSnapshotId = k.KurId,
            }, k.Islem));
        Assert.Contains("MASAK", hata.Message);
    }

    [Fact]
    public async Task Carisi_olmayan_islemde_kumulatif_HESAPLANMAZ()
    {
        // Kişi bilinmiyorsa "bağlantılı işlem" kurulamaz; tek işlem kuralı işler.
        // Bu bir SINIRDIR ve sessiz değildir (MasakKapisi XML'inde yazılı).
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "MASAK carisiz", stokAdet: 20m);
        using var db = vt.Olustur(k);

        var dto = Dto(k, 100_000m); dto.CariId = null;
        await Satis(db).Satis(dto, k.Islem);
        var dto2 = Dto(k, 100_000m); dto2.CariId = null;
        await Satis(db).Satis(dto2, k.Islem);   // toplanmaz → geçer

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(2, kontrol.Hareketler.Count(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
    }
}
