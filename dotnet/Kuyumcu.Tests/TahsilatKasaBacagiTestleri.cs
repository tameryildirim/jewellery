using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MKasa = Kuyumcu.Api.Modules.Kasa;
using MUrun = Kuyumcu.Api.Modules.Urun;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET UX-4b — CARİ TAHSİLATINA KASA BACAĞI (madde 9) ve KONSİNYE KORUMASI (madde 6)
//
// TESPİT (2026-09-16): nakit tahsilat cari bakiyesini düşürüyor ama HİÇBİR
// kasaya girmiyordu (CariServisi.TahsilatGir'de KasaHareket yoktu). Sonuç:
//   • çekmecedeki para ile defter arasında yapısal fark,
//   • gün sonu kapanışı kilidi ve K6 (TL yalnız TL kasasına) kapılarının
//     tahsilat yolunda HİÇ çalışmaması.
// Karar: Q1 → B (Tamer Bey mührü 2026-09-16 09:30). Zorunluluk SUNUCUDA;
// kasa doğrulaması satıştaki TEK mekanizmadan geçer
// (IslemDogrulayici.KasaDogrula(db, kasaId, tenant, "TRY", now)).
//
// HAVALE de KASAYA yazılır — satış konvansiyonuyla aynı (SatisModulu: "Kasa ID —
// NAKIT/KART/HAVALE için zorunlu"). Banka bacağı bu pakette AÇILMAZ; tek modülde
// satıştan farklı davranmak Mehmet'in (06 Güvenlik) veto ettiği durumdur.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class TahsilatKasaBacagiTestleri(TestVeritabani vt)
{
    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance,
            new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor()));

    private static MKasa.KasaServisi Kasa(KuyumcuDbContext db) =>
        new(db, NullLogger<MKasa.KasaServisi>.Instance);

    private static MCari.TahsilatGirisDto Dto(
        Kiraci k, decimal tl, string odeme = "NAKIT", string? kasaId = "VARSAYILAN",
        decimal? has = null) => new()
    {
        TlTutari = tl,
        HasGrami = has,
        OdemeTuru = odeme,
        KasaId = kasaId == "VARSAYILAN" ? k.KasaId : kasaId,
        KurSnapshotId = k.KurId,
    };

    // ── Mehmet koşul 4 / test 1 ──────────────────────────────────────────────

    [Fact]
    public async Task KasaIdsiz_NAKIT_tahsilat_REDDEDILIR()
    {
        // Ekran kapısı güvenlik kapısı değildir: API'den kasasız gelen istek
        // gün kilidi ve para birimi kapılarını atlardı.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tahsilat kasasız");
        using var db = vt.Olustur(k);

        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Cari(db).TahsilatGir(k.CariId, Dto(k, 1_000m, kasaId: null), k.Islem));

        Assert.Contains("kasa", hata.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task KasaIdsiz_KART_ve_HAVALE_tahsilati_da_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tahsilat kasasız 2");
        using var db = vt.Olustur(k);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            Cari(db).TahsilatGir(k.CariId, Dto(k, 500m, "KART", kasaId: null), k.Islem));
        await Assert.ThrowsAsync<BadRequestException>(() =>
            Cari(db).TahsilatGir(k.CariId, Dto(k, 500m, "HAVALE", kasaId: null), k.Islem));
    }

    // ── Mehmet koşul 4 / test 2 — gün kilidi ─────────────────────────────────

    [Fact]
    public async Task Kapali_gune_tahsilat_GUN_KILIDINE_takilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tahsilat gün kilidi");
        using var db = vt.Olustur(k);

        await Kasa(db).GunKapat(k.KasaId,
            new MKasa.GunKapanisiDto { SayilanNakit = TestKurulum.KasaAcilis }, k.Islem);

        // Kilit kapısı KasaDogrula'nın İÇİNDEDİR; tahsilat için ikinci bir kilit
        // kodu yazılmadı — bu test o tek mekanizmadan geçildiğini kanıtlar.
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Cari(db).TahsilatGir(k.CariId, Dto(k, 750m), k.Islem));
        Assert.Contains("kapan", hata.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Mehmet koşul 4 / test 3 — K6 para birimi kapısı ──────────────────────

    [Fact]
    public async Task Doviz_kasasina_TL_tahsilat_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tahsilat USD kasa", kasaDoviz: "USD");
        using var db = vt.Olustur(k);

        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Cari(db).TahsilatGir(k.CariId, Dto(k, 1_000m), k.Islem));

        Assert.Contains("TRY", hata.Message);
    }

    // ── Mehmet koşul 4 / test 4 — mutlu yol, TEK transaction ─────────────────

    [Fact]
    public async Task Basarili_NAKIT_tahsilat_kasayi_ARTIRIR_cariyi_DUSURUR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tahsilat mutlu yol");
        using var db = vt.Olustur(k);

        await Cari(db).TahsilatGir(k.CariId, Dto(k, 2_500m), k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis + 2_500m, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Equal(-2_500m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);

        var hareket = kontrol.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT");
        var kasaHareket = kontrol.KasaHareketleri.AsNoTracking()
            .Single(kh => kh.TenantId == k.TenantId);

        // İŞARETLİ yazılır (+giriş). Math.Abs KULLANILMAZ: defterden bakiye
        // yeniden türetilebilir kalmalı.
        Assert.Equal(2_500m, kasaHareket.Tutar);
        Assert.Equal(k.KasaId, kasaHareket.KasaId);
        Assert.Equal("TRY", kasaHareket.ParaBirimi);
        Assert.Equal("NAKIT", kasaHareket.OdemeTuru);
        // Tek transaction: kasa hareketi cari hareketine BAĞLIDIR.
        Assert.Equal(hareket.Id, kasaHareket.HareketId);
    }

    [Fact]
    public async Task HAVALE_tahsilati_da_KASAYA_yazilir_satisla_ayni()
    {
        // Belgedeki "HAVALE banka hesabına yazar" cümlesi Mehmet koşul 2 ile
        // geçersizdir: satışta HAVALE kasaya yazılıyor, tahsilatta da yazılır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tahsilat havale kasa");
        using var db = vt.Olustur(k);

        await Cari(db).TahsilatGir(k.CariId, Dto(k, 1_200m, "HAVALE"), k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis + 1_200m, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Equal("HAVALE",
            kontrol.KasaHareketleri.AsNoTracking().Single(x => x.TenantId == k.TenantId).OdemeTuru);
        // Banka bacağı bu pakette AÇILMADI — banka hareketi YOK.
        Assert.Empty(kontrol.BankaHareketleri.AsNoTracking().Where(x => x.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Sadece_HAS_GRAM_tahsilatinda_kasa_ISTENMEZ()
    {
        // Para el değiştirmez, altın gelir: kasa bacağı doğmaz.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tahsilat has gram");
        using var db = vt.Olustur(k);

        await Cari(db).TahsilatGir(k.CariId, Dto(k, 0m, kasaId: null, has: 5m), k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Empty(kontrol.KasaHareketleri.AsNoTracking().Where(x => x.TenantId == k.TenantId));
        Assert.Equal(-5m, TestKurulum.CariBakiye(kontrol, k.CariId).Has);
    }

    [Fact]
    public async Task Basarisiz_tahsilat_HICBIR_bacagi_yazmaz()
    {
        // Gün kilidine takılan tahsilat cari bakiyesini de DEĞİŞTİRMEMELİ:
        // kapı transaction'dan ÖNCE çalışır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tahsilat atomik");
        using var db = vt.Olustur(k);
        await Kasa(db).GunKapat(k.KasaId,
            new MKasa.GunKapanisiDto { SayilanNakit = TestKurulum.KasaAcilis }, k.Islem);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            Cari(db).TahsilatGir(k.CariId, Dto(k, 900m), k.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
        Assert.Empty(kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT"));
    }

    // ── Mehmet koşul 3 — "kasasız tahsilat" raporu ───────────────────────────

    [Fact]
    public async Task Kasasiz_tahsilat_raporu_ESIKSIZ_uretilmez()
    {
        // Eşik TAHMİN EDİLMEZ (Anayasa md.1): yapılandırma da sorgu da boşsa 400.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Kasasız rapor eşiksiz");
        using var db = vt.Olustur(k);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            Cari(db).KasasizTahsilatlar(k.Islem, esik: null));
    }

    [Fact]
    public async Task Kasasiz_tahsilat_raporu_YALNIZ_esik_oncesini_listeler()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Kasasız rapor");
        using var db = vt.Olustur(k);

        // Eşikten SONRA yazılan (kasalı) tahsilat.
        await Cari(db).TahsilatGir(k.CariId, Dto(k, 400m), k.Islem);

        // Eşik ŞİMDİ: yukarıdaki kayıt eşikten sonra olduğu için rapora GİRMEZ.
        var once = await Cari(db).KasasizTahsilatlar(k.Islem, DateTime.UtcNow.AddMinutes(-5));
        Assert.Equal(0m, TestKurulum.Sayi(once, "adet"));

        // Eşik GELECEK: aynı kayıt artık eşiğin altında kalır ve listelenir.
        var sonra = await Cari(db).KasasizTahsilatlar(k.Islem, DateTime.UtcNow.AddMinutes(5));
        Assert.Equal(1m, TestKurulum.Sayi(sonra, "adet"));
        // Defterde negatif duran ödeme raporda POZİTİF okunur.
        var govde = System.Text.Json.JsonDocument
            .Parse(System.Text.Json.JsonSerializer.Serialize(sonra)).RootElement;
        Assert.Equal(400m, govde.GetProperty("satirlar")[0].GetProperty("tlTutari").GetDecimal());
    }

    // ── Madde 6 — konsinye alanlarının sessiz silinmesi ──────────────────────

    [Fact]
    public async Task Konsinye_alanlari_gonderilmeyen_PATCH_ile_KORUNUR()
    {
        // Eskiden UrunGuncelleDto.Konsinye bool idi: {"ad":"yeni"} PATCH'i
        // sunucuya false olarak ulaşıyor, sahibi ve bedeli SESSİZCE siliniyordu —
        // başkasının malı işletme stoğuna dönüşüyordu.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Konsinye koruma");
        using var db = vt.Olustur(k);
        var urun = new MUrun.UrunServisi(db);

        var olusan = await urun.Olustur(new MUrun.UrunOlusturDto
        {
            Tip = "ZIYNET", Kod = "KONS-1", Ad = "Konsinye kolye", AyarId = "K22",
            GramAgirlik = 5m, TasAgirlik = 0m,
            Konsinye = true, KonsinyeCariId = k.CariId, KonsinyeBirimBedel = 9_500m,
        }, k.Islem);
        var urunId = ((Kuyumcu.Api.Data.Urun)olusan).Id;

        await urun.Guncelle(urunId, new MUrun.UrunGuncelleDto { Ad = "Yeni ad" }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var son = kontrol.Urunler.AsNoTracking().Single(u => u.Id == urunId);
        Assert.Equal("Yeni ad", son.Ad);
        Assert.True(son.Konsinye);
        Assert.Equal(k.CariId, son.KonsinyeCariId);
        Assert.Equal(9_500m, son.KonsinyeBirimBedel);
    }

    [Fact]
    public async Task Konsinye_ACIKCA_false_gonderilirse_alanlar_TEMIZLENIR()
    {
        // "Gönderilmeyen alan = koru" kuralı, açıkça gönderilen kararı
        // uygulamayı ENGELLEMEZ: mal işletmeye geçtiğinde false gider.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Konsinye devir");
        using var db = vt.Olustur(k);
        var urun = new MUrun.UrunServisi(db);

        var olusan = await urun.Olustur(new MUrun.UrunOlusturDto
        {
            Tip = "ZIYNET", Kod = "KONS-2", Ad = "Konsinye yüzük", AyarId = "K22",
            GramAgirlik = 3m, TasAgirlik = 0m,
            Konsinye = true, KonsinyeCariId = k.CariId, KonsinyeBirimBedel = 4_000m,
        }, k.Islem);
        var urunId = ((Kuyumcu.Api.Data.Urun)olusan).Id;

        await urun.Guncelle(urunId, new MUrun.UrunGuncelleDto { Konsinye = false }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var son = kontrol.Urunler.AsNoTracking().Single(u => u.Id == urunId);
        Assert.False(son.Konsinye);
        Assert.Null(son.KonsinyeCariId);
        Assert.Null(son.KonsinyeBirimBedel);
    }
}
