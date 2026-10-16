using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MKasa = Kuyumcu.Api.Modules.Kasa;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// GÜN SONU KAPANIŞI — sayım, fark ve gün kilidi
//
// Kasa bakiyesi sistemin İDDİASIDIR; çekmecedeki para GERÇEKTİR. Fark ancak
// sayılıp kaydedilirse görülür; kaydedilmeyen fark ertesi güne devreder ve
// hangi günden geldiği bir daha bulunamaz.
//
// Kapanıştan sonra o kasa-gününe yeni hareket YAZILAMAZ — yazılabilseydi
// imzalanmış bir sayım tutanağının altı sonradan doldurulmuş olurdu.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class GunKapanisiTestleri(TestVeritabani vt)
{
    private static MKasa.KasaServisi Kasa(KuyumcuDbContext db) =>
        new(db, NullLogger<MKasa.KasaServisi>.Instance);

    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MSatis.SatisOlusturDto SatisDto(Kiraci k, decimal tutar) => new()
    {
        CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = tutar,
        OdemeTuru = "KART", KasaId = k.KasaId, KurSnapshotId = k.KurId,
    };

    private static string Bugun() =>
        DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow)).ToString("yyyy-MM-dd");

    // ── Sayım ve fark ────────────────────────────────────────────────────────

    [Fact]
    public async Task Sayim_defterle_ayniysa_fark_SIFIR_ve_gerekce_gerekmez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Gün kapanış tam");
        using var db = vt.Olustur(k);

        await Kasa(db).GunKapat(k.KasaId,
            new MKasa.GunKapanisiDto { SayilanNakit = TestKurulum.KasaAcilis }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kapanis = kontrol.GunKapanislari.AsNoTracking()
            .Single(g => g.TenantId == k.TenantId && g.KasaId == k.KasaId);
        Assert.Equal(0m, kapanis.Fark);
        Assert.True(kapanis.Kilitli);
    }

    [Fact]
    public async Task Fark_varsa_GEREKCE_ZORUNLUDUR()
    {
        // Gerekçesiz fark, ertesi gün "neden eksik" sorusunu cevapsız bırakır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Gün kapanış gerekçesiz");
        using var db = vt.Olustur(k);

        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Kasa(db).GunKapat(k.KasaId,
                new MKasa.GunKapanisiDto { SayilanNakit = TestKurulum.KasaAcilis - 500m }, k.Islem));

        // SON-KALANLAR: ileti sade (büyük harf/"TL" yok, ₺ + farkın yönü) ve alan:aciklama — kural aynı.
        Assert.Contains("500,00 ₺ eksik", hata.Message);
        Assert.Contains("Farkın nedenini yazın", hata.Message);
        Assert.Equal("aciklama", AlanIsareti.Alan(hata));
        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.GunKapanislari.Where(g => g.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Gerekceli_fark_kaydedilir_ve_ISARETI_korunur()
    {
        // Eksik kasa NEGATİF fark verir; mutlak değere çevrilirse "fazla mı
        // eksik mi" bilgisi kaybolur.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Gün kapanış eksik");
        using var db = vt.Olustur(k);

        await Kasa(db).GunKapat(k.KasaId, new MKasa.GunKapanisiDto
        {
            SayilanNakit = TestKurulum.KasaAcilis - 500m,
            Aciklama = "Akşam sayımında 500 TL eksik çıktı, araştırılıyor",
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kapanis = kontrol.GunKapanislari.AsNoTracking().Single(g => g.TenantId == k.TenantId);
        Assert.Equal(-500m, kapanis.Fark);
        Assert.Equal(TestKurulum.KasaAcilis, kapanis.DefterBakiye);
    }

    // ── Gün kilidi ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Kapanmis_gune_SATIS_yazilamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Gün kilidi satış");
        using var db = vt.Olustur(k);
        await Kasa(db).GunKapat(k.KasaId,
            new MKasa.GunKapanisiDto { SayilanNakit = TestKurulum.KasaAcilis }, k.Islem);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).Satis(SatisDto(k, 1_000m), k.Islem));
        // SON-KALANLAR: kapanmış gün iletisi sade (büyük harf ve rol kodu yok) — kilit aynı.
        Assert.Contains("günü kapandı", hata.Message);
        Assert.DoesNotContain("SAHİP", hata.Message);
    }

    [Fact]
    public async Task Kapanmis_gune_ELLE_KASA_HAREKETI_de_yazilamaz()
    {
        // Elle hareket ucu ayrı bir yoldur; kilit orada da uygulanmazsa
        // kapanmış gün bu kapıdan sessizce delinir.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Gün kilidi elle hareket");
        using var db = vt.Olustur(k);
        await Kasa(db).GunKapat(k.KasaId,
            new MKasa.GunKapanisiDto { SayilanNakit = TestKurulum.KasaAcilis }, k.Islem);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            Kasa(db).HareketGir(k.KasaId,
                new MKasa.KasaHareketDto { Yon = "GIRIS", Tutar = 100m, OdemeTuru = "NAKIT" }, k.Islem));
    }

    [Fact]
    public async Task BASKA_kasa_kapanmis_gunden_ETKILENMEZ()
    {
        // Kilit kasa başınadır; bir kasanın kapanması dükkânı durdurmaz.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Gün kilidi başka kasa");
        using var db = vt.Olustur(k);

        var ikinciKasaId = Kimlik.Ulid();
        using (var kur = vt.OlusturSistem())
        {
            var now = DateTime.UtcNow;
            kur.Kasalar.Add(new Kasa
            {
                Id = ikinciKasaId, TenantId = k.TenantId, SubeId = k.SubeId,
                Ad = "İkinci Kasa", ParaBirimi = "TRY", Bakiye = 0m, Aktif = true,
                Guncellendi = now,
            });
            kur.SaveChanges();
        }

        await Kasa(db).GunKapat(k.KasaId,
            new MKasa.GunKapanisiDto { SayilanNakit = TestKurulum.KasaAcilis }, k.Islem);

        // İkinci kasa açık → hareket geçmeli
        await Kasa(db).HareketGir(ikinciKasaId,
            new MKasa.KasaHareketDto { Yon = "GIRIS", Tutar = 100m, OdemeTuru = "NAKIT" }, k.Islem);
    }

    [Fact]
    public async Task Ayni_gun_IKINCI_kez_kapatilamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Gün çift kapanış");
        using var db = vt.Olustur(k);
        await Kasa(db).GunKapat(k.KasaId,
            new MKasa.GunKapanisiDto { SayilanNakit = TestKurulum.KasaAcilis }, k.Islem);

        await Assert.ThrowsAsync<ConflictException>(() =>
            Kasa(db).GunKapat(k.KasaId,
                new MKasa.GunKapanisiDto { SayilanNakit = TestKurulum.KasaAcilis }, k.Islem));
    }

    // ── Yeniden açma ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Yeniden_acilan_gunde_islem_TEKRAR_yapilabilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Gün yeniden açma");
        using var db = vt.Olustur(k);
        await Kasa(db).GunKapat(k.KasaId,
            new MKasa.GunKapanisiDto { SayilanNakit = TestKurulum.KasaAcilis }, k.Islem);

        await Kasa(db).GunAc(k.KasaId, Bugun(),
            new MKasa.GunAcmaDto { Gerekce = "Kapanıştan sonra gelen müşteri satışı işlenecek" }, k.Islem);

        await Satis(db).Satis(SatisDto(k, 1_000m), k.Islem);   // artık geçer
    }

    [Fact]
    public async Task Yeniden_acma_kaydi_SILMEZ_kim_neden_actigini_saklar()
    {
        // Kapanış kaydının silinmesi tarihçeyi yok ederdi; denetimde
        // "gün kapanmış mıydı" sorusu cevapsız kalırdı.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Gün açma izi");
        using var db = vt.Olustur(k);
        await Kasa(db).GunKapat(k.KasaId, new MKasa.GunKapanisiDto
        {
            SayilanNakit = TestKurulum.KasaAcilis - 200m,
            Aciklama = "Iki yuz lira eksik cikti, yarin bakilacak",
        }, k.Islem);

        await Kasa(db).GunAc(k.KasaId, Bugun(),
            new MKasa.GunAcmaDto { Gerekce = "Eksik para bulundu, sayim tekrarlanacak" }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kapanis = kontrol.GunKapanislari.AsNoTracking().Single(g => g.TenantId == k.TenantId);
        Assert.False(kapanis.Kilitli);
        Assert.Equal(k.KullaniciId, kapanis.AcanKullaniciId);
        Assert.Contains("sayim tekrarlanacak", kapanis.AcilisGerekcesi);
        Assert.Equal(-200m, kapanis.Fark);          // ilk sayımın farkı DURUYOR
    }

    [Fact]
    public async Task Yeniden_acilan_gun_TEKRAR_kapatilabilir_ve_tek_satir_kalir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Gün tekrar kapanış");
        using var db = vt.Olustur(k);
        await Kasa(db).GunKapat(k.KasaId,
            new MKasa.GunKapanisiDto { SayilanNakit = TestKurulum.KasaAcilis }, k.Islem);
        await Kasa(db).GunAc(k.KasaId, Bugun(),
            new MKasa.GunAcmaDto { Gerekce = "Sayim hatali girildi, duzeltilecek" }, k.Islem);

        await Kasa(db).GunKapat(k.KasaId, new MKasa.GunKapanisiDto
        {
            SayilanNakit = TestKurulum.KasaAcilis - 50m, Aciklama = "Duzeltilmis sayim sonucu",
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kapanis = kontrol.GunKapanislari.AsNoTracking()
            .Single(g => g.TenantId == k.TenantId && g.KasaId == k.KasaId);
        Assert.True(kapanis.Kilitli);
        Assert.Equal(-50m, kapanis.Fark);
        Assert.Null(kapanis.AcilisZamani);
    }

    [Fact]
    public async Task Kapanis_kaydi_olmayan_gun_ACILAMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Gün açma yok");
        using var db = vt.Olustur(k);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Kasa(db).GunAc(k.KasaId, Bugun(),
                new MKasa.GunAcmaDto { Gerekce = "Boyle bir kapanis kaydi bulunmuyor" }, k.Islem));
    }
}
