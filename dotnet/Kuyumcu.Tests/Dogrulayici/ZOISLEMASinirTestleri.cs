using System.Net.Http.Json;
using Bunit;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MHurda = Kuyumcu.Api.Modules.Hurda;
using MPos = Kuyumcu.Api.Modules.Pos;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET ZO-ISLEM-A finansal sınır testleri.
// Paket şu para/gram yollarını ekrana açtı ya da değiştirdi:
//   • /pos/tahsilatlar ELLE eşleştirme (PosServisi.Eslestir, hareketKalan sınırı)
//   • /hurda ramat çıkışı (şubesiz MÜDÜR tek şubede çözülür → has gram çıkışı)
//   • Türkçe sayı girişi (FormAlani.SayiCoz — Gram/Para alanları)
// Beklenen değerler SABİT yazılır (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class ZOISLEMASinirPosTestleri(TestVeritabani vt)
{
    private static MPos.PosEslestirDto Dto(string hareket, params (string id, decimal tutar)[] s) => new()
    {
        BankaHareketId = hareket,
        Satirlar = s.Select(x => new MPos.PosEslestirSatirDto { PosTahsilatId = x.id, Tutar = x.tutar }).ToList(),
    };

    private decimal Eslesen(Kiraci k, string tahsilatId)
    {
        using var db = vt.OlusturSistem();
        return db.PosYatmaEslesmeleri.AsNoTracking()
            .Where(e => e.TenantId == k.TenantId && e.PosTahsilatId == tahsilatId && !e.GeriAlindi)
            .Sum(e => (decimal?)e.EslesenTutar) ?? 0m;
    }

    [Fact]
    public async Task ZOS_pos_sifir_tutar_reddedilir_kayit_yazilmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS pos sifir");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);
        var h = PosYardim.BankaHareketiYaz(vt, k, 5_000m);
        using var db = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() =>
            PosYardim.Servis(db).Eslestir(Dto(h, (t, 0m)), k.Islem));
        Assert.Equal(0m, Eslesen(k, t));
    }

    [Fact]
    public async Task ZOS_pos_eksi_tutar_tek_basina_ve_karisik_reddedilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS pos eksi");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t1 = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);
        var t2 = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);
        var h = PosYardim.BankaHareketiYaz(vt, k, 5_000m);
        using (var db = vt.Olustur(k))
            await Assert.ThrowsAsync<BadRequestException>(() =>
                PosYardim.Servis(db).Eslestir(Dto(h, (t1, -1m)), k.Islem));
        using (var db = vt.Olustur(k))
            await Assert.ThrowsAsync<BadRequestException>(() =>
                PosYardim.Servis(db).Eslestir(Dto(h, (t1, 100m), (t2, -50m)), k.Islem));
        Assert.Equal(0m, Eslesen(k, t1));
        Assert.Equal(0m, Eslesen(k, t2));
    }

    [Fact]
    public async Task ZOS_pos_kurus_yarimi_AwayFromZero_yuvarlanir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS pos kurus");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);   // net 9.815,00
        var h = PosYardim.BankaHareketiYaz(vt, k, 5_000m);
        using (var db = vt.Olustur(k))
            await PosYardim.Servis(db).Eslestir(Dto(h, (t, 1_250.255m)), k.Islem);
        Assert.Equal(1_250.26m, Eslesen(k, t));
        using (var db = vt.Olustur(k))
        {
            var o = await PosYardim.Servis(db)
                .OtoEslestirOnerisi(new MPos.PosOtoOneriDto { BankaHareketId = h }, k.Islem);
            var kalan = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o))
                .RootElement.GetProperty("hareketKalan").GetDecimal();
            Assert.Equal(3_749.74m, kalan);
        }
    }

    [Fact]
    public async Task ZOS_pos_hareket_kalani_sinirda_gecer_bir_kurus_fazlasi_reddedilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS pos sinir");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);   // net 9.815,00
        var h = PosYardim.BankaHareketiYaz(vt, k, 5_000m);
        using (var db = vt.Olustur(k))
            await Assert.ThrowsAsync<BadRequestException>(() =>
                PosYardim.Servis(db).Eslestir(Dto(h, (t, 5_000.01m)), k.Islem));
        Assert.Equal(0m, Eslesen(k, t));
        using (var db = vt.Olustur(k))
            await PosYardim.Servis(db).Eslestir(Dto(h, (t, 5_000m)), k.Islem);
        Assert.Equal(5_000m, Eslesen(k, t));
    }

    [Fact]
    public async Task ZOS_pos_cok_buyuk_tutar_net_tutari_asamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS pos buyuk");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);   // net 9.815,00
        var h = PosYardim.BankaHareketiYaz(vt, k, 999_999_999.99m);
        using (var db = vt.Olustur(k))
            await Assert.ThrowsAsync<BadRequestException>(() =>
                PosYardim.Servis(db).Eslestir(Dto(h, (t, 999_999_999.99m)), k.Islem));
        Assert.Equal(0m, Eslesen(k, t));
    }

    [Fact]
    public async Task ZOS_pos_bos_satir_listesi_reddedilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS pos bos");
        var h = PosYardim.BankaHareketiYaz(vt, k, 5_000m);
        using var db = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() =>
            PosYardim.Servis(db).Eslestir(new MPos.PosEslestirDto { BankaHareketId = h, Satirlar = [] }, k.Islem));
        await Assert.ThrowsAsync<BadRequestException>(() =>
            PosYardim.Servis(db).Eslestir(new MPos.PosEslestirDto { BankaHareketId = h, Satirlar = null! }, k.Islem));
    }

    // Aynı tahsilat tek istekte İKİ satırla gönderilirse toplam, tahsilatın net
    // tutarını aşmamalıdır (elle eşleştirme ucu istemci dışından da çağrılabilir).
    [Fact]
    public async Task ZOS_pos_ayni_tahsilat_iki_satirla_net_tutari_asamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS pos cift");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);   // net 9.815,00
        var h = PosYardim.BankaHareketiYaz(vt, k, 20_000m);
        using (var db = vt.Olustur(k))
        {
            // Doğrulayıcı 28.09 gözlemi: servis bunu 400 ile DEĞİL, tekil indeks
            // (tenant, tahsilat, hareket) DbUpdateException ile durdurur → HTTP 500.
            // Finansal değişmez (net aşılmaz) korunur; hata biçimi DÜŞÜK bulgu.
            var ex = await Record.ExceptionAsync(() =>
                PosYardim.Servis(db).Eslestir(Dto(h, (t, 9_815m), (t, 9_815m)), k.Islem));
            Assert.NotNull(ex);
        }
        Assert.True(Eslesen(k, t) <= 9_815m, $"net 9815 iken eşleşen {Eslesen(k, t)}");
    }
}

[Collection("veritabani")]
public sealed class ZOISLEMASinirHurdaTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance,
            hurdaAyarlari: new MHurda.HurdaAyarlari { FireKaydiAcik = false });

    private static MSatis.HurdaAlisDto Alis(Kiraci k) => new()
    {
        CariId = k.CariId, AyarId = "K22", GramAgirlik = 10m,   // 9,16 has
        BirimFiyatTlHasGram = 2_800m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        Satici = TestKurulum.HurdaSatici(),
    };

    private static MSatis.HurdaSatisDto Cikis(Kiraci k, decimal has, decimal? tahsil = null) => new()
    {
        Maden = "ALTIN", HasGrami = has, BirimFiyatTlHasGram = 2_900m,
        KasaId = k.KasaId, KurSnapshotId = k.KurId, TahsilEdilenTl = tahsil,
    };

    private static AktifKullanici SubesizMudur(Kiraci k) => new(k.KullaniciId, k.TenantId, null, "MUDUR");

    [Fact]
    public async Task ZOS_hurda_sifir_ve_eksi_has_reddedilir()
    {
        var k = IslemBantFikstur.ReferansKur(vt.OlusturSistem(), TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS hurda sifir"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(Alis(k), k.Islem);
        await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).HurdaSatis(Cikis(k, 0m), SubesizMudur(k)));
        await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).HurdaSatis(Cikis(k, -1m), SubesizMudur(k)));
    }

    [Fact]
    public async Task ZOS_hurda_has_4_hane_0_0001_cikar_kalan_dogru_bakiye_asimi_reddedilir()
    {
        var k = IslemBantFikstur.ReferansKur(vt.OlusturSistem(), TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS hurda 4hane"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(Alis(k), k.Islem);
        var s = await Satis(db).HurdaSatis(Cikis(k, 0.0001m), SubesizMudur(k));
        Assert.Equal(9.1599m, s.KalanHurdaHasGram);
        Assert.Equal(0.29m, s.ToplamTl);   // 0,0001 × 2.900 = 0,29
        await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).HurdaSatis(Cikis(k, 9.16m), SubesizMudur(k)));
        var son = await Satis(db).HurdaSatis(Cikis(k, 9.1599m), SubesizMudur(k));
        Assert.Equal(0m, son.KalanHurdaHasGram);
    }

    [Fact]
    public async Task ZOS_hurda_kurus_yuvarlamasi_AwayFromZero()
    {
        var k = IslemBantFikstur.ReferansKur(vt.OlusturSistem(), TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS hurda kurus"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(Alis(k), k.Islem);
        IslemBantFikstur.ReferansKur(vt.OlusturSistem(), k.TenantId, 1_000m);   // ISLEM-BANT: satış fiyatı 1.000,01 ± %20 bandında
        // 0,0005 × 2.900 = 1,45 ; 1,2345 has × 2.900 = 3.580,05 ; fiyat 1.000,01 × 0,5 = 500,005 → 500,01
        var d = Cikis(k, 0.5m);
        d.BirimFiyatTlHasGram = 1_000.01m;
        var s = await Satis(db).HurdaSatis(d, SubesizMudur(k));
        Assert.Equal(500.01m, s.ToplamTl);
    }

    [Fact]
    public async Task ZOS_hurda_tahsil_edilen_tutari_asamaz_cok_buyuk_reddedilir()
    {
        var k = IslemBantFikstur.ReferansKur(vt.OlusturSistem(), TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS hurda buyuk"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(Alis(k), k.Islem);
        await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(db).HurdaSatis(Cikis(k, 1m, tahsil: 999_999_999.99m), SubesizMudur(k)));
    }
}

public sealed class ZOISLEMASinirSayiTestleri
{
    [Fact]
    public void ZOS_turkce_giris_45_000_ve_1_5_belirsiz_1_500_reddi()
    {
        var a = FormAlani.SayiCoz("45.000");
        Assert.Equal(FormAlani.SayiSonucu.Tamam, a.Sonuc);
        Assert.Equal(45_000m, a.Deger);
        var b = FormAlani.SayiCoz("1,5");
        Assert.Equal(FormAlani.SayiSonucu.Tamam, b.Sonuc);
        Assert.Equal(1.5m, b.Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
    }

    [Fact]
    public void ZOS_bos_null_4_hane_ve_cok_buyuk()
    {
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("  ").Sonuc);
        Assert.Equal(0.0001m, FormAlani.SayiCoz("0,0001").Deger);
        Assert.Equal(999_999_999.99m, FormAlani.SayiCoz("999.999.999,99").Deger);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI — ZO-ISLEM-A 4. doğrulama (28.09 gece): hareketKalan +
// çift satır kapısı + sunucu uyruk denetimi sınırları. Beklenenler SABİT.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class ZOISLEMASinir4Testleri(TestVeritabani vt)
{
    private static decimal Alan(object o, string ad) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o))
            .RootElement.GetProperty(ad).GetDecimal();

    private static MPos.PosEslestirDto Dto(string hareket, params (string id, decimal tutar)[] s) => new()
    {
        BankaHareketId = hareket,
        Satirlar = s.Select(x => new MPos.PosEslestirSatirDto { PosTahsilatId = x.id, Tutar = x.tutar }).ToList(),
    };

    private async Task<decimal> Kalan(Kiraci k, string h)
    {
        using var db = vt.Olustur(k);
        return Alan(await PosYardim.Servis(db).OtoEslestirOnerisi(
            new MPos.PosOtoOneriDto { BankaHareketId = h }, k.Islem), "hareketKalan");
    }

    private decimal Eslesen(Kiraci k, string tahsilatId)
    {
        using var db = vt.OlusturSistem();
        return db.PosYatmaEslesmeleri.AsNoTracking()
            .Where(e => e.TenantId == k.TenantId && e.PosTahsilatId == tahsilatId && !e.GeriAlindi)
            .Sum(e => (decimal?)e.EslesenTutar) ?? 0m;
    }

    [Fact]
    public async Task ZOS4_hareket_kalani_sifirdan_baslar_tam_eslesmede_0_geri_almada_geri_doner()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS4 kalan tam");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);   // net 9.815,00
        var h = PosYardim.BankaHareketiYaz(vt, k, 5_000m);
        Assert.Equal(5_000m, await Kalan(k, h));
        using (var db = vt.Olustur(k))
            await PosYardim.Servis(db).Eslestir(Dto(h, (t, 5_000m)), k.Islem);
        Assert.Equal(0m, await Kalan(k, h));
        string eid;
        using (var db = vt.OlusturSistem())
            eid = db.PosYatmaEslesmeleri.AsNoTracking().Single(e => e.TenantId == k.TenantId && e.PosTahsilatId == t).Id;
        using (var db = vt.Olustur(k))
            await PosYardim.Servis(db).EslesmeGeriAl(eid,
                new MPos.PosEslesmeGeriAlDto { Gerekce = "Yanlış hareket seçildi, düzeltme yapılıyor" }, k.Islem);
        Assert.Equal(5_000m, await Kalan(k, h));
    }

    [Fact]
    public async Task ZOS4_hareket_kalani_kurus_yarimi_AwayFromZero()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS4 kalan kurus");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);
        var h = PosYardim.BankaHareketiYaz(vt, k, 5_000m);
        using (var db = vt.Olustur(k))
            await PosYardim.Servis(db).Eslestir(Dto(h, (t, 0.005m)), k.Islem);
        Assert.Equal(0.01m, Eslesen(k, t));
        Assert.Equal(4_999.99m, await Kalan(k, h));
    }

    // Aynı (tahsilat, hareket) çifti İKİNCİ bir istekle yeniden eşleştirilebilmeli
    // (geri almadan sonra ya da kısmi ek tutar) ya da anlaşılır 400 dönmeli; DB
    // tekil indeksinden DbUpdateException (HTTP 500) KABUL EDİLMEZ.
    [Fact]
    public async Task ZOS4_geri_alinan_cift_yeniden_eslestirilebilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS4 yeniden");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);
        var h = PosYardim.BankaHareketiYaz(vt, k, 5_000m);
        using (var db = vt.Olustur(k))
            await PosYardim.Servis(db).Eslestir(Dto(h, (t, 4_000m)), k.Islem);
        string eid;
        using (var db = vt.OlusturSistem())
            eid = db.PosYatmaEslesmeleri.AsNoTracking().Single(e => e.TenantId == k.TenantId && e.PosTahsilatId == t).Id;
        using (var db = vt.Olustur(k))
            await PosYardim.Servis(db).EslesmeGeriAl(eid,
                new MPos.PosEslesmeGeriAlDto { Gerekce = "Tutar yanlış girildi, yeniden eşleştirilecek" }, k.Islem);
        Exception? ex;
        using (var db = vt.Olustur(k))
            ex = await Record.ExceptionAsync(() => PosYardim.Servis(db).Eslestir(Dto(h, (t, 5_000m)), k.Islem));
        Assert.True(ex is null or BadRequestException, $"beklenen başarı ya da 400; gerçek: {ex?.GetType().Name}");
        Assert.True(Eslesen(k, t) <= 5_000m);
    }

    [Fact]
    public async Task ZOS4_ayni_cift_ikinci_kismi_istek_500_uretmez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS4 ikinci kismi");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);
        var h = PosYardim.BankaHareketiYaz(vt, k, 5_000m);
        using (var db = vt.Olustur(k))
            await PosYardim.Servis(db).Eslestir(Dto(h, (t, 1_000m)), k.Islem);
        Exception? ex;
        using (var db = vt.Olustur(k))
            ex = await Record.ExceptionAsync(() => PosYardim.Servis(db).Eslestir(Dto(h, (t, 500m)), k.Islem));
        Assert.True(ex is null or BadRequestException, $"beklenen başarı ya da 400; gerçek: {ex?.GetType().Name}");
    }

    [Fact]
    public async Task ZOS4_ayni_tahsilat_buyuk_kucuk_harf_farkli_kimlikle_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS4 harf");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);
        var h = PosYardim.BankaHareketiYaz(vt, k, 20_000m);
        Exception? ex;
        using (var db = vt.Olustur(k))
            ex = await Record.ExceptionAsync(() =>
                PosYardim.Servis(db).Eslestir(Dto(h, (t, 100m), (t.ToLowerInvariant(), 200m)), k.Islem));
        Assert.True(Eslesen(k, t) <= 9_815m);
        Assert.IsType<BadRequestException>(ex);
    }

    private static Kuyumcu.Api.Modules.Masak.MasakBilgiDto Kimlik(string? uyruk) => new()
    {
        BelgeTuru = KimlikBelgeTuru.Pasaport, BelgeNo = "U1234567", AdSoyad = "John Smith",
        DogumTarihi = new DateOnly(1985, 4, 12), Uyruk = uyruk,
        IslemAmaci = "Yatırım", Aciklama = "Birikim değerlendirmek için altın alımı yapılıyor",
        FonKaynagi = "Maaş birikimi", EkBelgeReferansi = "BELGE-2026-001",
    };

    [Theory]
    [InlineData(" fra ", true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("TURK", false)]
    [InlineData("TÜR", false)]
    public void ZOS4_uyruk_bosluk_bos_uzun_ve_turkce_harf(string uyruk, bool gecer)
    {
        var kapi = Kuyumcu.Api.Modules.Masak.MasakKapisi.Varsayilan();
        var ex = Record.Exception(() => kapi.Dogrula(2_000_000m, Kimlik(uyruk)));
        if (gecer) Assert.Null(ex);
        else Assert.IsType<BadRequestException>(ex);
    }

    [Fact]
    public void ZOS4_uyruk_esik_sinirinda_199999_99_denetlenmez_200000_denetlenir()
    {
        // MASAK-370: varsayılan kimlik eşiği 370.000 (RG 07.10.2026/33393); kademe (dolayısıyla uyruk
        // denetimi) bugün en düşük 200.000 amaç beyanı kademesinde başlar (TEYİTSİZ, kurucu md.15 (a)).
        var kapi = Kuyumcu.Api.Modules.Masak.MasakKapisi.Varsayilan();
        Assert.Null(Record.Exception(() => kapi.Dogrula(199_999.99m, Kimlik("ZZQ"))));
        Assert.IsType<BadRequestException>(Record.Exception(() => kapi.Dogrula(200_000m, Kimlik("ZZQ"))));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI tur 6 (28.09, cift_sira) — geri alınan / kısmi çift eşleşmesi
// artık izin veriliyor; sınırlar (net tutar, hareket tutarı, kuruş) ÇOK OLAYLI çiftte
// de tutmalı. Beklenenler SABİT.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class ZOISLEMASinir5Testleri(TestVeritabani vt)
{
    private static MPos.PosEslestirDto Dto(string hareket, string t, decimal tutar) => new()
    {
        BankaHareketId = hareket,
        Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = t, Tutar = tutar }],
    };

    private async Task Esle(Kiraci k, string h, string t, decimal tutar)
    {
        using var db = vt.Olustur(k);
        await PosYardim.Servis(db).Eslestir(Dto(h, t, tutar), k.Islem);
    }

    private async Task<Exception?> EsleHata(Kiraci k, string h, string t, decimal tutar)
    {
        using var db = vt.Olustur(k);
        return await Record.ExceptionAsync(() => PosYardim.Servis(db).Eslestir(Dto(h, t, tutar), k.Islem));
    }

    private async Task GeriAl(Kiraci k, string eid)
    {
        using var db = vt.Olustur(k);
        await PosYardim.Servis(db).EslesmeGeriAl(eid,
            new MPos.PosEslesmeGeriAlDto { Gerekce = "Doğrulayıcı sınır testi geri alma gerekçesi" }, k.Islem);
    }

    private List<PosYatmaEslesme> Satirlar(Kiraci k, string t)
    {
        using var db = vt.OlusturSistem();
        return db.PosYatmaEslesmeleri.AsNoTracking()
            .Where(e => e.TenantId == k.TenantId && e.PosTahsilatId == t)
            .OrderBy(e => e.CiftSira).ToList();
    }

    [Fact]
    public async Task ZOS5_ayni_cift_kismi_olaylar_hareket_tutarini_asamaz_sinir_dahil()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS5 hareket sinir");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);   // net 9.815,00
        var h = PosYardim.BankaHareketiYaz(vt, k, 5_000m);
        await Esle(k, h, t, 3_000m);
        await Esle(k, h, t, 1_999.99m);
        await Esle(k, h, t, 0.01m);                               // tam 5.000,00 → sınır dahil
        Assert.IsType<BadRequestException>(await EsleHata(k, h, t, 0.01m));   // bir kuruş fazlası
        var s = Satirlar(k, t);
        Assert.Equal(new[] { 1, 2, 3 }, s.Select(x => x.CiftSira).ToArray());
        Assert.Equal(5_000.00m, s.Sum(x => x.EslesenTutar));
    }

    [Fact]
    public async Task ZOS5_geri_alinan_tutar_sayilmaz_ama_net_tutar_asilamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS5 net sinir");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);   // net 9.815,00
        var h = PosYardim.BankaHareketiYaz(vt, k, 20_000m);
        await Esle(k, h, t, 9_815m);
        await GeriAl(k, Satirlar(k, t)[0].Id);
        await Esle(k, h, t, 9_815m);                              // geri alınan sayılmaz → geçer
        Assert.IsType<BadRequestException>(await EsleHata(k, h, t, 0.01m));   // net aşımı
        using var db = vt.OlusturSistem();
        Assert.Equal(9_815.00m, db.PosTahsilatlari.AsNoTracking().Single(x => x.Id == t).YatanTutar);
        Assert.Equal(new[] { 1, 2 }, Satirlar(k, t).Select(x => x.CiftSira).ToArray());
    }

    [Fact]
    public async Task ZOS5_sira_geri_alinanlari_da_sayar_ve_yuvarlama_AwayFromZero()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS5 sira yuvarlama");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);
        var h = PosYardim.BankaHareketiYaz(vt, k, 5_000m);
        await Esle(k, h, t, 100m);
        await GeriAl(k, Satirlar(k, t)[0].Id);
        await Esle(k, h, t, 100m);
        await GeriAl(k, Satirlar(k, t)[1].Id);
        await Esle(k, h, t, 0.005m);                              // 0,005 → 0,01
        var s = Satirlar(k, t);
        Assert.Equal(3, s[2].CiftSira);
        Assert.Equal(0.01m, s[2].EslesenTutar);
        Assert.False(s[2].GeriAlindi);
        using var db = vt.OlusturSistem();
        Assert.Equal(0.01m, db.PosTahsilatlari.AsNoTracking().Single(x => x.Id == t).YatanTutar);
    }

    [Fact]
    public async Task ZOS5_sifir_eksi_ve_cok_buyuk_yeni_sira_yazmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS5 sifir eksi buyuk");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);
        var h = PosYardim.BankaHareketiYaz(vt, k, 5_000m);
        await Esle(k, h, t, 1_000m);
        Assert.IsType<BadRequestException>(await EsleHata(k, h, t, 0m));
        Assert.IsType<BadRequestException>(await EsleHata(k, h, t, -1m));
        Assert.IsType<BadRequestException>(await EsleHata(k, h, t, 999_999_999.99m));
        var s = Assert.Single(Satirlar(k, t));
        Assert.Equal(1, s.CiftSira);
        Assert.Equal(1_000m, s.EslesenTutar);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI tur 11 (29.09, Aşkın + Mehmet) — 7. doğrulamanın açtığı
// "Eşleşmeyi geri al" yolu (PosTahsilatlar ⋯ menüsü → POST /pos/eslesmeler/{id}/geri-al):
// kuruş, gerekçe sınırı (19/20, boşluk, null), çok büyük tutar, iki eşleşmeli hareketin
// durumu ve kiracı yalıtımı. Beklenenler SABİT (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class ZOISLEMASinir7Testleri(TestVeritabani vt)
{
    private static MPos.PosEslestirDto Dto(string hareket, string t, decimal tutar) => new()
    {
        BankaHareketId = hareket,
        Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = t, Tutar = tutar }],
    };

    private async Task Esle(Kiraci k, string h, string t, decimal tutar)
    {
        using var db = vt.Olustur(k);
        await PosYardim.Servis(db).Eslestir(Dto(h, t, tutar), k.Islem);
    }

    private async Task<Exception?> GeriAlHata(Kiraci k, string eid, string? gerekce)
    {
        using var db = vt.Olustur(k);
        return await Record.ExceptionAsync(() => PosYardim.Servis(db).EslesmeGeriAl(eid,
            new MPos.PosEslesmeGeriAlDto { Gerekce = gerekce! }, k.Islem));
    }

    private List<PosYatmaEslesme> Satirlar(Kiraci k, string t)
    {
        using var db = vt.OlusturSistem();
        return db.PosYatmaEslesmeleri.AsNoTracking()
            .Where(e => e.TenantId == k.TenantId && e.PosTahsilatId == t)
            .OrderBy(e => e.CiftSira).ToList();
    }

    private (decimal Yatan, string Durum) Tahsilat(string t)
    {
        using var db = vt.OlusturSistem();
        var x = db.PosTahsilatlari.AsNoTracking().Single(p => p.Id == t);
        return (x.YatanTutar, x.Durum);
    }

    private string HareketDurumu(string h)
    {
        using var db = vt.OlusturSistem();
        return db.BankaHareketleri.AsNoTracking().Single(p => p.Id == h).Durum;
    }

    [Fact]
    public async Task ZOS7_gerekce_19_karakter_ve_bosluk_dolgusu_null_reddedilir_20_gecer()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS7 gerekce sinir");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);   // net 9.815,00
        var h = PosYardim.BankaHareketiYaz(vt, k, 5_000m);
        await Esle(k, h, t, 1_000m);
        var eid = Assert.Single(Satirlar(k, t)).Id;

        Assert.IsType<BadRequestException>(await GeriAlHata(k, eid, "1234567890123456789"));          // 19
        Assert.IsType<BadRequestException>(await GeriAlHata(k, eid, "   1234567890123456789      ")); // 19 + boşluk
        Assert.IsType<BadRequestException>(await GeriAlHata(k, eid, null));
        Assert.IsType<BadRequestException>(await GeriAlHata(k, eid, ""));
        Assert.False(Assert.Single(Satirlar(k, t)).GeriAlindi);                                    // hiçbiri yazmadı
        Assert.Equal((1_000.00m, "KISMI"), Tahsilat(t));

        Assert.Null(await GeriAlHata(k, eid, "12345678901234567890"));                              // tam 20 → geçer
        var s = Assert.Single(Satirlar(k, t));
        Assert.True(s.GeriAlindi);
        Assert.Equal("12345678901234567890", s.GeriAlmaGerekcesi);
        Assert.Equal((0.00m, "BEKLIYOR"), Tahsilat(t));
        Assert.Equal("BEKLIYOR", HareketDurumu(h));
    }

    [Fact]
    public async Task ZOS7_bir_kurusluk_eslesme_geri_alininca_yatan_sifir_ikinci_geri_alma_reddedilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS7 kurus");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);
        var h = PosYardim.BankaHareketiYaz(vt, k, 5_000m);
        await Esle(k, h, t, 0.005m);                               // 0,005 → 0,01 (AwayFromZero)
        var s = Assert.Single(Satirlar(k, t));
        Assert.Equal(0.01m, s.EslesenTutar);
        Assert.Equal((0.01m, "KISMI"), Tahsilat(t));
        Assert.Null(await GeriAlHata(k, s.Id, "Bir kuruşluk yanlış eşleşme geri alınıyor"));
        Assert.Equal((0.00m, "BEKLIYOR"), Tahsilat(t));
        Assert.IsType<BadRequestException>(await GeriAlHata(k, s.Id, "Aynı eşleşmeyi ikinci kez geri alma"));
        Assert.Equal((0.00m, "BEKLIYOR"), Tahsilat(t));
    }

    [Fact]
    public async Task ZOS7_cok_buyuk_tutar_tam_eslesir_geri_alinir_ve_yeniden_eslesir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS7 buyuk");
        var cihaz = PosYardim.CihazYaz(vt, k, oran: 0m);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 999_999_999.99m);
        var h = PosYardim.BankaHareketiYaz(vt, k, 999_999_999.99m);
        await Esle(k, h, t, 999_999_999.99m);
        Assert.Equal((999_999_999.99m, "YATTI"), Tahsilat(t));
        Assert.Equal("ESLESTI", HareketDurumu(h));
        Assert.Null(await GeriAlHata(k, Satirlar(k, t)[0].Id, "Çok büyük tutarlı eşleşme geri alınıyor"));
        Assert.Equal((0.00m, "BEKLIYOR"), Tahsilat(t));
        Assert.Equal("BEKLIYOR", HareketDurumu(h));
        await Esle(k, h, t, 999_999_999.99m);                      // geri dönüş yolu
        Assert.Equal((999_999_999.99m, "YATTI"), Tahsilat(t));
        Assert.Equal(2, Satirlar(k, t).Count);
    }

    [Fact]
    public async Task ZOS7_iki_tahsilatli_harekette_biri_geri_alininca_hareket_ESLESTI_kalir_ikincisiyle_BEKLIYOR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS7 iki eslesme");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t1 = PosYardim.TahsilatYaz(vt, k, cihaz, 1_000m);    // net 981,50
        var t2 = PosYardim.TahsilatYaz(vt, k, cihaz, 2_000m);    // net 1.963,00
        var h = PosYardim.BankaHareketiYaz(vt, k, 2_944.50m);
        await Esle(k, h, t1, 981.50m);
        await Esle(k, h, t2, 1_963.00m);
        Assert.Equal("ESLESTI", HareketDurumu(h));
        Assert.Null(await GeriAlHata(k, Satirlar(k, t1)[0].Id, "Birinci tahsilat yanlış hareketle eşleşti"));
        Assert.Equal((0.00m, "BEKLIYOR"), Tahsilat(t1));
        Assert.Equal((1_963.00m, "YATTI"), Tahsilat(t2));
        Assert.Equal("ESLESTI", HareketDurumu(h));
        Assert.Null(await GeriAlHata(k, Satirlar(k, t2)[0].Id, "İkinci tahsilat da yanlış hareketle eşleşti"));
        Assert.Equal("BEKLIYOR", HareketDurumu(h));
    }

    [Fact]
    public async Task ZOS7_baska_kiracinin_eslesmesi_geri_alinamaz_NotFound_ve_iz_degismez()
    {
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS7 kiraci A");
        var b = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS7 kiraci B");
        var cihaz = PosYardim.CihazYaz(vt, a);
        var t = PosYardim.TahsilatYaz(vt, a, cihaz, 10_000m);
        var h = PosYardim.BankaHareketiYaz(vt, a, 5_000m);
        await Esle(a, h, t, 2_500m);
        var eid = Assert.Single(Satirlar(a, t)).Id;
        Assert.IsType<NotFoundException>(await GeriAlHata(b, eid, "Başka kiracıdan geri alma denemesi yapılıyor"));
        Assert.False(Assert.Single(Satirlar(a, t)).GeriAlindi);
        Assert.Equal((2_500.00m, "KISMI"), Tahsilat(t));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI — ZO-ISLEM-A r11 doğrulaması (29.09): /hurda EKRANININ
// istemci tarafı para/gram yolu (Tutar = kabul has × fiyat, AwayFromZero 2 hane;
// tahsil edilen varsayılanı = tutar; Türkçe giriş). Gövde AYRIŞTIRILARAK sınanır;
// beklenen değerler SABİT yazılır (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class ZOISLEMASinir11EkranTestleri : Bunit.BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string kurFiyati = "6800";

    public ZOISLEMASinir11EkranTestleri()
    {
        JSInterop.Mode = Bunit.JSRuntimeMode.Loose;
        Services.AddScoped<Kuyumcu.Web.Servisler.DilServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.OturumServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<Kuyumcu.Web.Servisler.ApiIstemci>();
        Services.AddScoped<Kuyumcu.Web.Servisler.CanliKurServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.UyumServisi>();
    }

    private sealed class Sahte(ZOISLEMASinir11EkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage i, CancellationToken c)
        {
            var yol = i.RequestUri!.PathAndQuery;
            var govde = i.Content is null ? "" : await i.Content.ReadAsStringAsync(c);
            lock (t.istekler) t.istekler.Add((i.Method.Method, yol, govde));
            string? g =
                yol.Contains("/islemler/hurda-satis") ? """{"id":"h9","maden":"ALTIN","madenAdi":"Altın","hasGrami":1,"birimFiyatTlHasGram":1,"toplamTl":1,"tahsilEdilenTl":1,"kalanTl":0,"kalanHurdaHasGram":1,"kasaBakiyesi":1,"paraBirimi":"TRY","tarih":"2026-09-29T09:00:00Z","fireKaydiAcik":false,"fireNotu":null,"aliciKabulHasGrami":1,"tahlilFarkiHasGram":0}"""
                : yol.Contains("/hurda/bakiye") ? """{"subeId":"s1","satirlar":[{"maden":"ALTIN","madenAdi":"Altın","hasGram":5000,"girenHas":5000,"cikanHas":0,"tahlilFarkiHas":0}],"fireKaydiAcik":false,"fireNotu":null}"""
                : yol.Contains("/hurda/defter") ? """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}"""
                : yol.Contains("/kasalar") ? """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":250000,"aktif":true}]"""
                : yol.Contains("/kur/guncel") ? "{\"id\":\"kur-1\",\"altinHasFiyati\":" + t.kurFiyati + ",\"gramAltinFiyati\":6850,\"usdTry\":34.0,\"eurTry\":37.0,\"gbpTry\":43.5,\"kaynak\":\"ALTINKAYNAK\",\"olusturuldu\":\"2026-09-29T09:00:00Z\",\"ayarFiyatlari\":[]}"
                : yol.Contains("/tevsik/gunluk") ? """{"had":30000,"kuralEtkin":false,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":999999999999,"islemTutariKuraliDevrede":false}"""
                : yol.Contains("/arama") ? """{"sonuclar":[],"toplam":0}"""
                : null;
            return new HttpResponseMessage(g is null ? System.Net.HttpStatusCode.NotFound : System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g ?? """{"message":"tanımsız uç"}""", System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Hurda> Ekran()
    {
        Services.GetRequiredService<Kuyumcu.Web.Servisler.OturumServisi>().KullaniciAta(
            new Kuyumcu.Web.Servisler.OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.Hurda>();
        e.WaitForAssertion(() => Assert.False(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        return e;
    }

    private static void Gir(Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e, string secici, string deger) =>
        e.WaitForAssertion(() =>
        {
            if (e.Find(secici).GetAttribute("value") != deger) e.Find(secici).Input(deger);
            Assert.Equal(deger, e.Find(secici).GetAttribute("value"));
        });

    private List<string> Postlar()
    {
        lock (istekler) return istekler.Where(i => i.Metot == "POST" && i.Yol.Contains("/islemler/hurda-satis")).Select(i => i.Govde).ToList();
    }

    private System.Text.Json.JsonElement Gonder(Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e)
    {
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar()));
        return System.Text.Json.JsonDocument.Parse(Postlar()[0]).RootElement;
    }

    private void Reddedilir(Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e)
    {
        e.Find("#hrd-kaydet").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#alan-hata-hrd-has")));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-hrd-has").TextContent));
        Assert.Empty(e.FindAll("#hrd-onay-onayla"));
        Assert.Empty(Postlar());
    }

    [Fact]
    public void ZOS11_hurda_ekran_kurus_yarimi_AwayFromZero_500_005_500_01()
    {
        kurFiyati = "1000.01";
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");
        e.WaitForAssertion(() => Assert.Contains("500,01", e.Find("#hrd-tutar").TextContent));
        var g = Gonder(e);
        Assert.Equal(0.5m, g.GetProperty("hasGrami").GetDecimal());
        Assert.Equal(1000.01m, g.GetProperty("birimFiyatTlHasGram").GetDecimal());
        Assert.Equal(500.01m, g.GetProperty("tahsilEdilenTl").GetDecimal());
    }

    [Fact]
    public void ZOS11_hurda_ekran_has_4_hane_0_0001_gram_kaybolmaz()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,0001");
        e.WaitForAssertion(() => Assert.Contains("0,68", e.Find("#hrd-tutar").TextContent));
        var g = Gonder(e);
        Assert.Equal(0.0001m, g.GetProperty("hasGrami").GetDecimal());
        Assert.Equal(0.68m, g.GetProperty("tahsilEdilenTl").GetDecimal());
    }

    [Fact]
    public void ZOS11_hurda_ekran_sifir_has_reddedilir_POST_yok()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0");
        Reddedilir(e);
    }

    [Fact]
    public void ZOS11_hurda_ekran_eksi_has_reddedilir_POST_yok()
    {
        var e = Ekran();
        e.Find("#hrd-has").Input("-1");
        Reddedilir(e);
    }

    [Fact]
    public void ZOS11_hurda_ekran_bos_has_reddedilir_POST_yok()
    {
        var e = Ekran();
        Reddedilir(e);
    }

    [Fact]
    public void ZOS11_hurda_ekran_belirsiz_1_500_reddedilir_POST_yok()
    {
        var e = Ekran();
        e.Find("#hrd-has").Input("1.500");
        Reddedilir(e);
    }

    [Fact]
    public void ZOS11_hurda_ekran_turkce_45_000_fiyat_ve_1_5_has_gövdeye_dogru_girer()
    {
        var e = Ekran();
        e.Find("#hrd-detay-ac").Click();
        e.WaitForElement("#hrd-fiyat");
        Gir(e, "#hrd-fiyat", "45.000");
        Gir(e, "#hrd-has", "1,5");
        // ISLEM-BANT-2: fiyat ramat satış bandının (kur 6.800 → %80–%120) dışında; Kaydet artık bant dışında kilitli —
        // SAHİP/MÜDÜR "Fiyat doğru, yine de kaydet" + 20+ karakter gerekçe ile açar (sunucu kuralıyla aynı).
        e.WaitForElement("#hrd-bant-onay").Change(true);
        e.WaitForElement("#hrd-bant-gerekce").Input("Ramatçı ile sözleşmeli fiyat, teyit edildi.");
        e.WaitForAssertion(() => Assert.Contains("67.500", e.Find("#hrd-tutar").TextContent));
        var g = Gonder(e);
        Assert.Equal(45000m, g.GetProperty("birimFiyatTlHasGram").GetDecimal());
        Assert.Equal(1.5m, g.GetProperty("hasGrami").GetDecimal());
        Assert.Equal(67500m, g.GetProperty("tahsilEdilenTl").GetDecimal());
    }

    [Fact]
    public void ZOS11_hurda_ekran_cok_buyuk_tutar_999_999_999_99_kurus_kaybetmez()
    {
        var e = Ekran();
        e.Find("#hrd-detay-ac").Click();
        e.WaitForElement("#hrd-fiyat");
        Gir(e, "#hrd-fiyat", "999.999.999,99");
        Gir(e, "#hrd-has", "1");
        // ISLEM-BANT-2 (kasıtlı davranış değişikliği): 999.999.999,99 has fiyatı mutlak sınır (500–50.000) dışı — sunucu her
        // durumda 400 verir (ISLEM-BANT IB10). Tutar kuruşu kaybetmeden biçimlenir (asıl iddia AYNEN) ve Kaydet sınır cümlesiyle
        // kilitli (tarayıcıda tıklanamaz). Eski "gövde birebir gider" beklentisi sunucunun reddedeceği bir istekti.
        e.WaitForAssertion(() => Assert.Contains("999.999.999,99", e.Find("#hrd-tutar").TextContent));
        Assert.True(e.Find("#hrd-kaydet").HasAttribute("disabled"));
        Assert.Contains("500,00–50.000,00", e.Find("#hrd-kaydet-kilit").TextContent);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI — ZO-ISLEM-A 12. doğrulama (29.09): POS YATMASI KAPISI
// (cihazın hesabı = hareketin hesabı) + yatma adayları KALAN sınırı + öneri
// hareketKalan. Komisyonsuz cihaz (oran 0) → net = brüt; beklenenler SABİT.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class ZOISLEMASinir12Testleri(TestVeritabani vt)
{
    private static MPos.PosEslestirDto Dto(string hareket, string t, decimal tutar) => new()
    {
        BankaHareketId = hareket,
        Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = t, Tutar = tutar }],
    };

    private async Task<Exception?> Esle(Kiraci k, string h, string t, decimal tutar)
    {
        using var db = vt.Olustur(k);
        return await Record.ExceptionAsync(() => PosYardim.Servis(db).Eslestir(Dto(h, t, tutar), k.Islem));
    }

    private int Sayi(Kiraci k)
    {
        using var db = vt.OlusturSistem();
        return db.PosYatmaEslesmeleri.AsNoTracking().Count(e => e.TenantId == k.TenantId);
    }

    private string HareketYaz(Kiraci k, string hesap, decimal tutar, string para = "TRY", string yon = "GELEN")
    {
        using var db = vt.OlusturSistem();
        var id = Kimlik.Ulid();
        db.BankaHareketleri.Add(new BankaHareket
        {
            Id = id, TenantId = k.TenantId, BankaHesapId = hesap, Yon = yon, Tutar = tutar,
            ParaBirimi = para, Durum = "BEKLIYOR", Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow,
        });
        db.SaveChanges();
        return id;
    }

    [Fact]
    public async Task ZOS12_sifir_ve_eksi_tutar_POS_hesabinda_da_reddedilir_kayit_yok()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS12 sifir");
        var hesap = PosYardim.HesapYaz(vt, k, "Garanti", "POS12a");
        var c = PosYardim.CihazYaz(vt, k, "G12", oran: 0m, bankaHesapId: hesap);
        var t = PosYardim.TahsilatYaz(vt, k, c, 1_000m);
        var h = HareketYaz(k, hesap, 1_000m);
        Assert.IsType<BadRequestException>(await Esle(k, h, t, 0m));
        Assert.IsType<BadRequestException>(await Esle(k, h, t, -0.01m));
        Assert.Equal(0, Sayi(k));
    }

    [Fact]
    public async Task ZOS12_para_birimi_farkli_hareket_ayni_hesapta_olsa_da_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS12 doviz");
        var hesap = PosYardim.HesapYaz(vt, k, "Garanti", "POS12b");
        var c = PosYardim.CihazYaz(vt, k, "G12b", oran: 0m, bankaHesapId: hesap);
        var t = PosYardim.TahsilatYaz(vt, k, c, 1_000m);
        var h = HareketYaz(k, hesap, 1_000m, para: "USD");
        var ex = Assert.IsType<BadRequestException>(await Esle(k, h, t, 1_000m));
        Assert.Contains("para birimi", ex.Message);
        Assert.Equal(0, Sayi(k));
    }

    [Fact]
    public async Task ZOS12_GIDEN_hareket_POS_hesabinda_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS12 giden");
        var hesap = PosYardim.HesapYaz(vt, k, "Garanti", "POS12c");
        var c = PosYardim.CihazYaz(vt, k, "G12c", oran: 0m, bankaHesapId: hesap);
        var t = PosYardim.TahsilatYaz(vt, k, c, 1_000m);
        var h = HareketYaz(k, hesap, 1_000m, yon: "GIDEN");
        Assert.IsType<BadRequestException>(await Esle(k, h, t, 1_000m));
        Assert.Equal(0, Sayi(k));
    }

    [Fact]
    public async Task ZOS12_cok_buyuk_tutar_tam_eslesir_bir_kurus_fazlasi_reddedilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS12 buyuk");
        var hesap = PosYardim.HesapYaz(vt, k, "Garanti", "POS12d");
        var c = PosYardim.CihazYaz(vt, k, "G12d", oran: 0m, bankaHesapId: hesap);
        var t = PosYardim.TahsilatYaz(vt, k, c, 999_999_999.99m);
        var h = HareketYaz(k, hesap, 999_999_999.99m);
        Assert.IsType<BadRequestException>(await Esle(k, h, t, 1_000_000_000.00m));
        Assert.Null(await Esle(k, h, t, 999_999_999.99m));
        Assert.IsType<BadRequestException>(await Esle(k, h, t, 0.01m));
        using var db = vt.OlusturSistem();
        Assert.Equal(999_999_999.99m, db.PosTahsilatlari.AsNoTracking().Single(x => x.Id == t).YatanTutar);
    }

    [Fact]
    public async Task ZOS12_yatma_adaylari_kalan_0_01_listelenir_tam_eslesen_listelenmez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS12 aday");
        var hesap = PosYardim.HesapYaz(vt, k, "Garanti", "POS12e");
        var c = PosYardim.CihazYaz(vt, k, "G12e", oran: 0m, bankaHesapId: hesap);
        var t = PosYardim.TahsilatYaz(vt, k, c, 5_000m);
        var hTam = HareketYaz(k, hesap, 100.00m);
        var hKalan = HareketYaz(k, hesap, 200.00m);
        Assert.Null(await Esle(k, hTam, t, 100.00m));
        Assert.Null(await Esle(k, hKalan, t, 199.99m));
        using var db = vt.Olustur(k);
        var y = await PosYardim.Servis(db).YatmaAdaylari(null, 50, "tr", k.TenantId);
        var kimlikler = y.Sonuclar.Select(s => s.Kimlik).ToList();
        Assert.Contains(hKalan, kimlikler);
        Assert.DoesNotContain(hTam, kimlikler);
        Assert.Contains("kalan 0,01", y.Sonuclar.Single(s => s.Kimlik == hKalan).AltBilgi);
    }

    [Fact]
    public async Task ZOS12_oneri_hareketKalan_kismi_sonrasi_dogru_ve_kume_kalana_esitlenir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS12 oneri");
        var hesap = PosYardim.HesapYaz(vt, k, "Garanti", "POS12f");
        var c = PosYardim.CihazYaz(vt, k, "G12f", oran: 0m, bankaHesapId: hesap);
        var a = PosYardim.TahsilatYaz(vt, k, c, 700.00m);
        var b = PosYardim.TahsilatYaz(vt, k, c, 300.005m);   // brüt kuruş yarımı
        var h = HareketYaz(k, hesap, 1_000.01m);
        Assert.Null(await Esle(k, h, a, 700.00m));
        using var db = vt.Olustur(k);
        var o = System.Text.Json.JsonSerializer.SerializeToElement(
            await PosYardim.Servis(db).OtoEslestirOnerisi(new MPos.PosOtoOneriDto { BankaHareketId = h }, k.Islem));
        Assert.Equal(300.01m, o.GetProperty("hareketKalan").GetDecimal());
        Assert.True(o.GetProperty("bulundu").GetBoolean());
        Assert.Equal(b, o.GetProperty("satirlar")[0].GetProperty("posTahsilatId").GetString());
        Assert.Equal(300.01m, o.GetProperty("satirlar")[0].GetProperty("tutar").GetDecimal());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI — ZO-ISLEM-A 13. doğrulama (29.09, Aşkın + Mehmet): /pos/tahsilatlar
// istemci para yolu (elle seçimde kısmi tutar = hareketin boştaki kalanı; eşleştirme sonrası
// "kalan" bandı = hareketKalan − eslesenToplam) ve /banka dekont MASAK kimlik eşiği sınırı.
// Gövde AYRIŞTIRILARAK sınanır; beklenen değerler SABİT (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class ZOISLEMASinir13EkranTestleri : Bunit.BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly List<(string Parca, string Govde)> yollar = [];

    public ZOISLEMASinir13EkranTestleri()
    {
        JSInterop.Mode = Bunit.JSRuntimeMode.Loose;
        Services.AddScoped<Kuyumcu.Web.Servisler.DilServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.OturumServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<Kuyumcu.Web.Servisler.ApiIstemci>();
        Services.AddScoped<Kuyumcu.Web.Servisler.CanliKurServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.UyumServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.PosServisi>();
    }

    private sealed class Sahte(ZOISLEMASinir13EkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage i, CancellationToken c)
        {
            var yol = i.RequestUri!.PathAndQuery;
            var govde = i.Content is null ? "" : await i.Content.ReadAsStringAsync(c);
            lock (t.istekler) t.istekler.Add((i.Method.Method, yol, govde));
            foreach (var (p, g) in t.yollar)
                if (yol.Contains(p))
                    return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
            { Content = new StringContent("""{"message":"tanımsız uç"}""", System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private static string Satir(string id, decimal kalan) =>
        "{\"id\":\"" + id + "\",\"posCihazId\":\"p1\",\"cihazAd\":\"Garanti POS " + id + "\",\"bankaAdi\":\"Garanti\",\"brutTutar\":" +
        kalan.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"komisyonOran\":0,\"komisyonTutar\":0,\"netTutar\":" +
        kalan.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"yatanTutar\":0,\"kalanTutar\":" +
        kalan.ToString(System.Globalization.CultureInfo.InvariantCulture) +
        ",\"taksitAdedi\":1,\"paraBirimi\":\"TRY\",\"islemTarihi\":\"2026-09-24T10:00:00Z\",\"valorTarihi\":\"2026-09-25T00:00:00Z\",\"durum\":\"BEKLIYOR\",\"iptalGerekcesi\":null}";

    private Bunit.IRenderedComponent<Kuyumcu.Web.Pages.PosTahsilatlar> PosSecili(
        string satirlar, decimal hareketKalan, decimal eslesenToplam)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        Services.GetRequiredService<Kuyumcu.Web.Servisler.OturumServisi>().KullaniciAta(
            new Kuyumcu.Web.Servisler.OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        yollar.Add(("/pos/valor-takvimi", "[]"));
        yollar.Add(("/pos/yatma-adaylari", """{"sorgu":"","tur":"BANKA_HAREKET","limit":10,"ofset":0,"sayi":1,"dahaVar":false,"tavan":50,"sonuclar":[{"kimlik":"bh9","ad":"Garanti POS yatan","altBilgi":"x","tur":"BANKA_HAREKET","rota":null}],"gozatma":true}"""));
        yollar.Add(("/pos/oto-eslestir-onerisi", "{\"bulundu\":false,\"bankaHareketId\":\"bh9\",\"toplam\":0,\"satirlar\":[],\"mesaj\":\"x\",\"hareketKalan\":" + hareketKalan.ToString(inv) + "}"));
        yollar.Add(("/pos/eslestir", "{\"mesaj\":\"Eşleştirme tamamlandı\",\"bankaHareketId\":\"bh9\",\"eslesenToplam\":" + eslesenToplam.ToString(inv) + ",\"satirSayisi\":1}"));
        yollar.Add(("/pos/tahsilatlar", "{\"satirlar\":[" + satirlar + "],\"toplamSatir\":1,\"limit\":50,\"offset\":0,\"azamiSatir\":200,\"sinirDegdi\":false}"));
        yollar.Add(("/pos/cihazlar", """[{"id":"p1","ad":"Garanti POS","bankaAdi":"Garanti","subeId":"s1","kasaId":"k1","bankaHesapId":"b2","uyeIsyeriNo":null,"terminalNo":null,"pesinKomisyonOran":0,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0}]"""));
        yollar.Add(("/bankalar", """{"hesaplar":[]}"""));
        var e = Render<Kuyumcu.Web.Pages.PosTahsilatlar>();
        e.WaitForElement("#pos-eslestir-hareket-sonuc-0", TimeSpan.FromSeconds(3)).Click();
        e.WaitForElement("#pos-elle", TimeSpan.FromSeconds(3));
        return e;
    }

    private List<string> EslestirPostlari()
    {
        lock (istekler) return istekler.Where(i => i.Metot == "POST" && i.Yol.Contains("/pos/eslestir") && !i.Yol.Contains("onerisi")).Select(i => i.Govde).ToList();
    }

    // SINIR: kuruş — hareketin kalanı 0,01; tahsilatın kalanı 999.999.999,99 → gövde tam 0,01 taşır.
    [Fact]
    public void ZOS13_pos_kismi_hareket_kalani_0_01_cok_buyuk_tahsilatta_govde_0_01()
    {
        var e = PosSecili(Satir("t1", 999_999_999.99m), 0.01m, 0.01m);
        e.Find("#pos-elle-t1").Change(true);
        e.WaitForAssertion(() => Assert.False(e.Find("#pos-eslestir-uygula").HasAttribute("disabled")));
        e.Find("#pos-eslestir-uygula").Click();
        e.WaitForAssertion(() => Assert.Single(EslestirPostlari()));
        var s = System.Text.Json.JsonDocument.Parse(EslestirPostlari()[0]).RootElement.GetProperty("satirlar");
        Assert.Equal(1, s.GetArrayLength());
        Assert.Equal(0.01m, s[0].GetProperty("tutar").GetDecimal());
        // Sonuç bandı: 0,01 − 0,01 = 0 → "tamamı eşleşti"; ham "TRY" kalıbı yok.
        e.WaitForAssertion(() => Assert.Contains("Bu yatmanın tamamı eşleşti.", e.Find("#pos-t-basari").TextContent));
        Assert.DoesNotContain("TRY", e.Find("#pos-t-basari").TextContent);
    }

    // SINIR: çok büyük tutar — 999.999.999,99 tam eşleşir, kuruş kaybolmaz.
    [Fact]
    public void ZOS13_pos_cok_buyuk_tutar_tam_eslesir_kurus_kaybolmaz()
    {
        var e = PosSecili(Satir("t1", 999_999_999.99m), 999_999_999.99m, 999_999_999.99m);
        e.Find("#pos-elle-t1").Change(true);
        e.WaitForAssertion(() => Assert.False(e.Find("#pos-eslestir-uygula").HasAttribute("disabled")));
        Assert.Empty(e.FindAll("#pos-elle-kismi-t1"));
        e.Find("#pos-eslestir-uygula").Click();
        e.WaitForAssertion(() => Assert.Single(EslestirPostlari()));
        var s = System.Text.Json.JsonDocument.Parse(EslestirPostlari()[0]).RootElement.GetProperty("satirlar");
        Assert.Equal(999_999_999.99m, s[0].GetProperty("tutar").GetDecimal());
        e.WaitForAssertion(() => Assert.Contains("999.999.999,99", e.Find("#pos-t-basari").TextContent));
    }

    // SINIR: iki satır, hareket kalanı 1.000,01: t1 981,50 tam + t2 (50) yalnız boştaki 18,50 kadar → toplam tam 1.000,00.
    // Kalan bandı: 1.000,01 − 1.000,00 = 0,01 kaldı (kuruş, tr-TR).
    [Fact]
    public void ZOS13_pos_iki_satir_ikincisi_bosta_kalan_18_50_ile_kismi_kalan_0_01_banda()
    {
        var e = PosSecili(Satir("t1", 981.50m) + "," + Satir("t2", 50m), 1000.01m, 1000.00m);
        e.Find("#pos-elle-t1").Change(true);
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#pos-elle-t2")));
        e.Find("#pos-elle-t2").Change(true);
        e.WaitForAssertion(() => Assert.False(e.Find("#pos-eslestir-uygula").HasAttribute("disabled")));
        e.Find("#pos-eslestir-uygula").Click();
        e.WaitForAssertion(() => Assert.Single(EslestirPostlari()));
        var s = System.Text.Json.JsonDocument.Parse(EslestirPostlari()[0]).RootElement.GetProperty("satirlar");
        var tutarlar = Enumerable.Range(0, s.GetArrayLength())
            .ToDictionary(i => s[i].GetProperty("posTahsilatId").GetString()!, i => s[i].GetProperty("tutar").GetDecimal());
        Assert.Equal(981.50m, tutarlar["t1"]);
        Assert.Equal(18.51m, tutarlar["t2"]);
        e.WaitForAssertion(() => Assert.Contains("0,01", e.Find("#pos-t-basari").TextContent));
        Assert.Contains("eşleşmemiş", e.Find("#pos-t-basari").TextContent);
    }

    // SINIR: sıfır / eksi hareket kalanı → eşleştir kilitli, POST YOK.
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ZOS13_pos_hareket_kalani_sifir_ya_da_eksi_POST_yok(int kalan)
    {
        var e = PosSecili(Satir("t1", 100m), kalan, 0m);
        e.Find("#pos-elle-t1").Change(true);
        e.WaitForAssertion(() => Assert.True(e.Find("#pos-eslestir-uygula").HasAttribute("disabled")));
        e.Find("#pos-eslestir-uygula").Click();
        Assert.Empty(EslestirPostlari());
    }

    // SINIR: boş — seçim kaldırılınca eşleştir kilitli, POST YOK.
    [Fact]
    public void ZOS13_pos_secim_kaldirilinca_bos_POST_yok()
    {
        var e = PosSecili(Satir("t1", 100m), 100m, 100m);
        e.Find("#pos-elle-t1").Change(true);
        e.WaitForAssertion(() => Assert.False(e.Find("#pos-eslestir-uygula").HasAttribute("disabled")));
        e.Find("#pos-elle-t1").Change(false);
        e.WaitForAssertion(() => Assert.True(e.Find("#pos-eslestir-uygula").HasAttribute("disabled")));
        Assert.Empty(EslestirPostlari());
    }

    // ── /banka dekont: MASAK kimlik eşiği sınırı (eşik SAYISI sunucudan: 185000) ──
    private Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Banka> Banka()
    {
        Services.GetRequiredService<Kuyumcu.Web.Servisler.OturumServisi>().KullaniciAta(
            new Kuyumcu.Web.Servisler.OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        yollar.Add(("/masak/esikler", """{"kimlikEsigi":185000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000,"ekBelgeEsigi":20000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}"""));
        yollar.Add(("/masak/gereksinim", """{"kademe":"YALNIZ_KIMLIK","kimlikGerekli":true,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}"""));
        yollar.Add(("/hareketler", """{"hareketler":[]}"""));
        yollar.Add(("/hareket", """{"id":"bh2","yon":"GELEN","tutar":500,"durum":"BEKLIYOR"}"""));
        yollar.Add(("/bankalar", """{"hesaplar":[{"id":"b1","bankaAdi":"Ziraat","hesapAdi":"Ana Hesap","hesapSahibi":"X Ltd.","iban":"TR200000000000000000000001","paraBirimi":"TRY","bakiye":0,"aktif":true}]}"""));
        yollar.Add(("/cari", """[{"id":"c1","ad":"Ayşe Yılmaz"}]"""));
        var e = Render<Kuyumcu.Web.Pages.Banka>();
        e.WaitForElement("#bh-dekont-gir").Click();
        e.WaitForElement("#dekont-tutar");
        return e;
    }

    private List<string> DekontPostlari()
    {
        lock (istekler) return istekler.Where(i => i.Metot == "POST" && i.Yol.EndsWith("/hareket")).Select(i => i.Govde).ToList();
    }

    // Eşiğin 1 kuruş altı (Türkçe giriş "184.999,99"): MASAK kapısı AÇILMAZ, gövdede masak null, tutar tam.
    [Fact]
    public void ZOS13_banka_dekont_esik_1_kurus_alti_masak_yok_govde_tutar_tam()
    {
        var e = Banka();
        e.Find("#dekont-tutar").Input("184.999,99");
        e.WaitForAssertion(() => Assert.False(e.Find("#dekont-kaydet").HasAttribute("disabled")));
        Assert.Empty(e.FindAll("#degismez-ipucu-MasakKimlikTespiti"));
        e.Find("#dekont-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(DekontPostlari()));
        var g = System.Text.Json.JsonDocument.Parse(DekontPostlari()[0]).RootElement;
        Assert.Equal(184_999.99m, g.GetProperty("tutar").GetDecimal());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, g.GetProperty("masak").ValueKind);
    }

    // Tam eşik (Türkçe giriş "185.000"): MASAK kapısı AÇILIR, alanlar eksikken Kaydet KİLİTLİ, POST yok.
    [Fact]
    public void ZOS13_banka_dekont_tam_esik_masak_acilir_kaydet_kilitli()
    {
        var e = Banka();
        e.Find("#dekont-tutar").Input("185.000");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#degismez-ipucu-MasakKimlikTespiti")));
        e.WaitForAssertion(() => Assert.True(e.Find("#dekont-kaydet").HasAttribute("disabled")));
        // Not (doğrulayıcı DÜŞÜK): DekontKaydet işleyicisinde DekontKilitSebebi koruması yok — kilitli düğmeye
        // programatik tıklama POST atar (bUnit'te görüldü); tarayıcı disabled düğmeyi tetiklemez, sunucu MASAK kapısı fail-closed.
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#dekont-kaydet-sebep").TextContent));
        Assert.Empty(DekontPostlari());
    }

    // Sıfır / eksi / boş / belirsiz "1.500" dekont tutarı: POST YOK, alan altı Türkçe mesaj.
    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("")]
    [InlineData("1.500")]
    public void ZOS13_banka_dekont_sifir_eksi_bos_belirsiz_reddedilir(string girdi)
    {
        var e = Banka();
        e.Find("#dekont-tutar").Input(girdi);
        if (!e.Find("#dekont-kaydet").HasAttribute("disabled")) e.Find("#dekont-kaydet").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("[id^='alan-hata-dekont-tutar']")));
        Assert.Empty(DekontPostlari());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI 29.09 — Hurda ekranı (Hurda.razor) "+ Detay ekle" KAPALIYKEN
// istemcinin türettiği TUTAR ve varsayılan TAHSİL gövdesi. Kapalı bölümde tahsil
// edilen = Tutar (istemci hesabı) olarak gider; sunucu toplamTl'yi aynı kuralla
// (Yuvarla2 AwayFromZero) hesaplamazsa "fazla tahsil" ya da kuruş farkı doğar.
// Beklenen değerler SABİT yazılır.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class ZOISLEMASinirHurdaEkranTestleri : Bunit.BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly List<(string Parca, string Govde)> yollar = [];

    public ZOISLEMASinirHurdaEkranTestleri()
    {
        JSInterop.Mode = Bunit.JSRuntimeMode.Loose;
        Services.AddScoped<Kuyumcu.Web.Servisler.DilServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.OturumServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<Kuyumcu.Web.Servisler.ApiIstemci>();
        Services.AddScoped<Kuyumcu.Web.Servisler.CanliKurServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.UyumServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.PosServisi>();
    }

    private sealed class Sahte(ZOISLEMASinirHurdaEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage i, CancellationToken c)
        {
            var yol = i.RequestUri!.PathAndQuery;
            var govde = i.Content is null ? "" : await i.Content.ReadAsStringAsync(c);
            lock (t.istekler) t.istekler.Add((i.Method.Method, yol, govde));
            foreach (var (p, g) in t.yollar)
                if (yol.Contains(p))
                    return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
            { Content = new StringContent("""{"message":"tanımsız uç"}""", System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Hurda> Ekran(string fiyat, string bakiyeHas = "9.16", bool kaydetAcik = true)
    {
        Services.GetRequiredService<Kuyumcu.Web.Servisler.OturumServisi>().KullaniciAta(
            new Kuyumcu.Web.Servisler.OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        yollar.Add(("/islemler/hurda-satis", """{"id":"h9","maden":"ALTIN","madenAdi":"Altın","hasGrami":1,"birimFiyatTlHasGram":1,"toplamTl":1,"tahsilEdilenTl":1,"kalanTl":0,"kalanHurdaHasGram":1,"kasaBakiyesi":1,"paraBirimi":"TRY","tarih":"2026-09-25T09:00:00Z","fireKaydiAcik":false,"fireNotu":null,"aliciKabulHasGrami":1,"tahlilFarkiHasGram":0}"""));
        yollar.Add(("/hurda/bakiye", "{\"subeId\":\"s1\",\"satirlar\":[{\"maden\":\"ALTIN\",\"madenAdi\":\"Altın\",\"hasGram\":" + bakiyeHas + ",\"girenHas\":" + bakiyeHas + ",\"cikanHas\":0,\"tahlilFarkiHas\":0}],\"fireKaydiAcik\":false,\"fireNotu\":null}"));
        yollar.Add(("/hurda/defter", """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}"""));
        yollar.Add(("/kasalar", """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":250000,"aktif":true}]"""));
        yollar.Add(("/kur/guncel", "{\"id\":\"kur-1\",\"altinHasFiyati\":" + fiyat + ",\"gramAltinFiyati\":6850,\"usdTry\":34.0,\"eurTry\":37.0,\"gbpTry\":43.5,\"kaynak\":\"ALTINKAYNAK\",\"olusturuldu\":\"2026-09-25T09:00:00Z\",\"ayarFiyatlari\":[]}"));
        yollar.Add(("/arama", """{"sonuclar":[],"toplam":0}"""));
        var e = Render<Kuyumcu.Web.Pages.Hurda>();
        if (kaydetAcik) e.WaitForAssertion(() => Assert.False(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        else e.WaitForElement("#hrd-tutar");
        return e;
    }

    private static void Gir(Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e, string s, string d) =>
        e.WaitForAssertion(() => { if (e.Find(s).GetAttribute("value") != d) e.Find(s).Input(d); });

    private List<string> Postlar()
    {
        lock (istekler) return istekler.Where(i => i.Metot == "POST" && i.Yol.Contains("/islemler/hurda-satis")).Select(i => i.Govde).ToList();
    }

    private System.Text.Json.JsonElement Gonder(Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e)
    {
        e.Find("#hrd-kaydet").Click();
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar()));
        return System.Text.Json.JsonDocument.Parse(Postlar()[0]).RootElement;
    }

    [Fact]
    public void ZOSH_kurus_yarimi_AwayFromZero_tahsil_govdesi_680_01()
    {
        // 0,1 g × 6.800,05 = 680,005 → AwayFromZero 680,01 (ToEven olsaydı 680,00).
        var e = Ekran("6800.05");
        Gir(e, "#hrd-has", "0,1");
        e.WaitForAssertion(() => Assert.Contains("680,01", e.Find("#hrd-tutar").TextContent));
        var g = Gonder(e);
        Assert.Equal(0.1m, g.GetProperty("hasGrami").GetDecimal());
        Assert.Equal(680.01m, g.GetProperty("tahsilEdilenTl").GetDecimal());
        Assert.Equal("NAKIT", g.GetProperty("odemeTuru").GetString());
    }

    [Fact]
    public void ZOSH_has_4_hane_0_0001_tutar_0_68()
    {
        var e = Ekran("6800");
        Gir(e, "#hrd-has", "0,0001");
        var g = Gonder(e);
        Assert.Equal(0.0001m, g.GetProperty("hasGrami").GetDecimal());
        Assert.Equal(0.68m, g.GetProperty("tahsilEdilenTl").GetDecimal());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("")]
    public void ZOSH_sifir_eksi_bos_has_gonderilmez_alan_hatasi_gorunur(string has)
    {
        var e = Ekran("6800");
        if (has.Length > 0) Gir(e, "#hrd-has", has);
        e.Find("#hrd-kaydet").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("[id^='alan-hata-hrd-has']")));
        Assert.Empty(e.FindAll("#hrd-onay-onayla"));
        Assert.Empty(Postlar());
    }

    [Fact]
    public void ZOSH_turkce_giris_1_5_gecer_belirsiz_1_500_reddedilir()
    {
        var e = Ekran("6800");
        Gir(e, "#hrd-has", "1.500");
        e.Find("#hrd-kaydet").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#hrd-onay-onayla")));
        Assert.Empty(Postlar());

        Gir(e, "#hrd-has", "1,5");
        var g = Gonder(e);
        Assert.Equal(1.5m, g.GetProperty("hasGrami").GetDecimal());
        Assert.Equal(10200m, g.GetProperty("tahsilEdilenTl").GetDecimal());
    }

    [Fact]
    public void ZOSH_cok_buyuk_tutar_tr_TR_bicimde_TRY_kalibi_yok()
    {
        // ISLEM-BANT-2 (kasıtlı): 999.999.999,99 has fiyatı mutlak sınır (500–50.000) dışı → Kaydet KİLİTLİ ve sebebi
        // alanın cümlesi (sunucu da 400 verir). Biçim iddiası AYNEN: tutar tr-TR, "TRY" kalıbı yok.
        var e = Ekran("999999999.99", kaydetAcik: false);
        Gir(e, "#hrd-has", "1");
        e.WaitForAssertion(() => Assert.Contains("999.999.999,99", e.Find("#hrd-tutar").TextContent));
        Assert.DoesNotContain("TRY", e.Find("#hrd-tutar").TextContent);
        Assert.DoesNotContain("TRY", e.Find("#hrd-fiyat-ozet").TextContent);
        Assert.True(e.Find("#hrd-kaydet").HasAttribute("disabled"));
        Assert.Contains("500,00–50.000,00", e.Find("#hrd-kaydet-kilit").TextContent);
    }

    [Fact]
    public void ZOSH_istemci_tutari_sunucu_toplamiyla_AYNI_kural()
    {
        // Sunucu: toplamTl = Yuvarla2(Yuvarla6(kabul) × fiyat). İstemci 4 haneli has ile
        // aynı sonucu vermeli; aksi hâlde kapalı detayda tahsil > toplam olur.
        var e = Ekran("6666.67");
        Gir(e, "#hrd-has", "0,0015");
        var g = Gonder(e);
        // 0,0015 × 6.666,67 = 10,000005 → 10,00
        Assert.Equal(10.00m, g.GetProperty("tahsilEdilenTl").GetDecimal());
        Assert.Equal(Math.Round(Math.Round(0.0015m, 6, MidpointRounding.AwayFromZero) * 6666.67m, 2, MidpointRounding.AwayFromZero),
                     g.GetProperty("tahsilEdilenTl").GetDecimal());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI — ZO-ISLEM-A kapanış-2 doğrulaması (29.09 10:40, Aşkın + Mehmet).
// Bu turda değişen para yolu: /pos/yatma-adaylari sonucunun ADI artık TUTAR
// ("+1.470 ₺"), hesap · tarih · karşı taraf · "kalan …" ikincil satırda.
// Ayrıca /hurda şube çözümü (OturumSubesiCoz) sınırları. Beklenenler SABİT.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class ZOISLEMASinir14Testleri(TestVeritabani vt)
{
    private string HareketYaz(Kiraci k, string hesap, decimal tutar, string? karsi = null)
    {
        using var db = vt.OlusturSistem();
        var id = Kimlik.Ulid();
        db.BankaHareketleri.Add(new BankaHareket
        {
            Id = id, TenantId = k.TenantId, BankaHesapId = hesap, Yon = "GELEN", Tutar = tutar,
            ParaBirimi = "TRY", Durum = "BEKLIYOR", Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow,
            KarsiTarafAd = karsi,
        });
        db.SaveChanges();
        return id;
    }

    private async Task<Kuyumcu.Contracts.Arama.AramaYaniti> Adaylar(Kiraci k, string? q = null, string dil = "tr")
    {
        using var db = vt.Olustur(k);
        return await PosYardim.Servis(db).YatmaAdaylari(q, 50, dil, k.TenantId);
    }

    [Fact]
    public async Task ZOS14_ad_tutardir_tam_sayi_N0_kalan_eki_yok()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS14 ad");
        var hesap = PosYardim.HesapYaz(vt, k, "Garanti", "POS14a");
        PosYardim.CihazYaz(vt, k, "G14a", oran: 0m, bankaHesapId: hesap);
        var h = HareketYaz(k, hesap, 1_470.00m, "Garanti POS yatan");
        var s = (await Adaylar(k)).Sonuclar.Single(x => x.Kimlik == h);
        Assert.Equal("+1.470\u00A0₺", s.Ad);
        Assert.DoesNotContain("kalan", s.AltBilgi);
        Assert.StartsWith("Garanti", s.AltBilgi);
        Assert.EndsWith("Garanti POS yatan", s.AltBilgi);
    }

    [Fact]
    public async Task ZOS14_kismi_eslesmede_ad_brut_tutar_kalan_0_01_ikincil_satirda()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS14 kismi");
        var hesap = PosYardim.HesapYaz(vt, k, "Garanti", "POS14b");
        var c = PosYardim.CihazYaz(vt, k, "G14b", oran: 0m, bankaHesapId: hesap);
        var t = PosYardim.TahsilatYaz(vt, k, c, 5_000m);
        var h = HareketYaz(k, hesap, 100.01m);
        using (var db = vt.Olustur(k))
            await PosYardim.Servis(db).Eslestir(new MPos.PosEslestirDto
            {
                BankaHareketId = h,
                Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = t, Tutar = 100.00m }],
            }, k.Islem);
        var s = (await Adaylar(k)).Sonuclar.Single(x => x.Kimlik == h);
        Assert.Equal("+100,01\u00A0₺", s.Ad);
        Assert.EndsWith("kalan 0,01\u00A0₺", s.AltBilgi);
    }

    [Fact]
    public async Task ZOS14_cok_buyuk_tutar_tr_TR_TRY_kalibi_yok_ve_EN_kalan_metni()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS14 buyuk");
        var hesap = PosYardim.HesapYaz(vt, k, "Garanti", "POS14c");
        var c = PosYardim.CihazYaz(vt, k, "G14c", oran: 0m, bankaHesapId: hesap);
        var t = PosYardim.TahsilatYaz(vt, k, c, 999_999_999.99m);
        var h = HareketYaz(k, hesap, 999_999_999.99m);
        using (var db = vt.Olustur(k))
            await PosYardim.Servis(db).Eslestir(new MPos.PosEslestirDto
            {
                BankaHareketId = h,
                Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = t, Tutar = 0.99m }],
            }, k.Islem);
        var tr = (await Adaylar(k)).Sonuclar.Single(x => x.Kimlik == h);
        Assert.Equal("+999.999.999,99\u00A0₺", tr.Ad);
        Assert.EndsWith("kalan 999.999.999\u00A0₺", tr.AltBilgi);
        Assert.DoesNotContain("TRY", tr.Ad + tr.AltBilgi);
        var en = (await Adaylar(k, dil: "en")).Sonuclar.Single(x => x.Kimlik == h);
        Assert.EndsWith("remaining 999.999.999\u00A0₺", en.AltBilgi);
    }

    [Fact]
    public async Task ZOS14_POS_e_bagli_olmayan_hesaptaki_ayni_tutar_aday_degildir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS14 ilgisiz");
        var pos = PosYardim.HesapYaz(vt, k, "Garanti", "POS14d");
        var diger = PosYardim.HesapYaz(vt, k, "Ziraat", "Vadesiz14d");
        PosYardim.CihazYaz(vt, k, "G14d", oran: 0m, bankaHesapId: pos);
        var hPos = HareketYaz(k, pos, 1_470m);
        var hDiger = HareketYaz(k, diger, 1_470m, "Ayşe Yılmaz havale");
        var ids = (await Adaylar(k, "1470")).Sonuclar.Select(x => x.Kimlik).ToList();
        Assert.Contains(hPos, ids);
        Assert.DoesNotContain(hDiger, ids);
    }

    [Fact]
    public async Task ZOS14_hurda_subesiz_MUDUR_tek_aktif_subede_cozulur_iki_subede_bos_kalir_TEZGAHTAR_cozulmez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOS14 sube");
        var mudur = new AktifKullanici(k.KullaniciId, k.TenantId, null, "MUDUR");
        var tezgah = new AktifKullanici(k.KullaniciId, k.TenantId, null, "TEZGAHTAR");
        using (var db = vt.Olustur(k))
        {
            Assert.Equal(k.SubeId, (await MHurda.HurdaStokKapisi.OturumSubesiCoz(db, mudur)).SubeId);
            Assert.Null((await MHurda.HurdaStokKapisi.OturumSubesiCoz(db, tezgah)).SubeId);
        }
        // Pasif ikinci şube sayılmaz → hâlâ tek aktif şube.
        var pasif = Kimlik.Ulid();
        using (var db = vt.OlusturSistem())
        {
            db.Subeler.Add(new Sube { Id = pasif, TenantId = k.TenantId, Ad = "Kapalı", Aktif = false, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow });
            db.SaveChanges();
        }
        using (var db = vt.Olustur(k))
            Assert.Equal(k.SubeId, (await MHurda.HurdaStokKapisi.OturumSubesiCoz(db, mudur)).SubeId);
        // İkinci AKTİF şube → tahmin yok, şubesiz kalır (fail-closed).
        using (var db = vt.OlusturSistem())
        {
            db.Subeler.Add(new Sube { Id = Kimlik.Ulid(), TenantId = k.TenantId, Ad = "Çarşı", Aktif = true, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow });
            db.SaveChanges();
        }
        using (var db = vt.Olustur(k))
            Assert.Null((await MHurda.HurdaStokKapisi.OturumSubesiCoz(db, mudur)).SubeId);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI 29.09 (kapanış 3) — Hurda "Tahsil edilen" alanı: kur oynaması
// eşitlemesi (TahsilCoz / gorulenTutarlar), ramatçı cümlesi ve aşım cümlesi SINIRLARI.
// Beklenen değerler SABİT yazılır; sahte sunucu sabit yanıt verir (gövde ayrıştırılır).
// ═══════════════════════════════════════════════════════════════════════════
public sealed class ZOISLEMASinirHurdaTahsilKurTestleri : Bunit.BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public ZOISLEMASinirHurdaTahsilKurTestleri()
    {
        JSInterop.Mode = Bunit.JSRuntimeMode.Loose;
        Services.AddScoped<Kuyumcu.Web.Servisler.DilServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.OturumServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<Kuyumcu.Web.Servisler.ApiIstemci>();
        Services.AddScoped<Kuyumcu.Web.Servisler.CanliKurServisi>();
    }

    private sealed class Sahte(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage i, CancellationToken c)
        {
            var yol = i.RequestUri!.AbsolutePath;
            var govde = i.Content is null ? "" : await i.Content.ReadAsStringAsync(c);
            lock (kayit) kayit.Add((i.Method.Method, i.RequestUri!.PathAndQuery, govde));
            var g = yol switch
            {
                var y when y.EndsWith("/hurda/bakiye") =>
                    """{"subeId":"s1","satirlar":[{"maden":"ALTIN","madenAdi":"Altın","hasGram":20,"girenHas":20,"cikanHas":0,"tahlilFarkiHas":0}],"fireKaydiAcik":false,"fireNotu":null}""",
                var y when y.EndsWith("/hurda/defter") =>
                    """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}""",
                var y when y.EndsWith("/islemler/hurda-satis") =>
                    """{"id":"h9","maden":"ALTIN","madenAdi":"Altın","hasGrami":1,"birimFiyatTlHasGram":1,"toplamTl":1,"tahsilEdilenTl":1,"kalanTl":0,"kalanHurdaHasGram":1,"kasaBakiyesi":1,"paraBirimi":"TRY","tarih":"2026-09-29T09:00:00Z","fireKaydiAcik":false,"fireNotu":null,"aliciKabulHasGrami":1,"tahlilFarkiHasGram":0}""",
                var y when y.EndsWith("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":250000,"aktif":true}]""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-29T09:00:00Z","ayarFiyatlari":[]}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Hurda> Ekran()
    {
        Services.GetRequiredService<Kuyumcu.Web.Servisler.OturumServisi>().KullaniciAta(
            new Kuyumcu.Web.Servisler.OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "MUDUR", "s1"));
        var e = Render<Kuyumcu.Web.Pages.Hurda>();
        e.WaitForAssertion(() => Assert.False(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        e.WaitForElement("#hrd-detay-ac").Click();
        e.WaitForElement("#hrd-tahsil");
        return e;
    }

    private static void Gir(Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e, string s, string d) =>
        e.WaitForAssertion(() => { if (e.Find(s).GetAttribute("value") != d) e.Find(s).Input(d); Assert.Equal(d, e.Find(s).GetAttribute("value")); });

    private void Kur(Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e, string id, decimal has) =>
        e.InvokeAsync(() => Services.GetRequiredService<Kuyumcu.Web.Servisler.CanliKurServisi>()
            .Uygula(new Kuyumcu.Web.Servisler.CanliKurServisi.KurAnlik(id, has, has + 50m, 34m, 37m, 43.5m)));

    private List<string> Postlar() { lock (istekler) return istekler.Where(i => i.Metot == "POST" && i.Yol.Contains("hurda-satis")).Select(i => i.Govde).ToList(); }

    private static string Ramatci(string eksik, string tutar) =>
        $"Tahsil edilmeyen {eksik} için ramatçıyı seçin ya da tahsil edilen tutarı işlem tutarına ({tutar}) eşitleyin.";

    [Fact]
    public void ZOSK_sifir_tahsil_tamami_eksik_ramatci_cumlesi_3400_POST_yok()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");                              // 0,5 × 6.800 = 3.400,00
        Gir(e, "#hrd-tahsil", "0");
        e.Find("#hrd-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(Ramatci(Kuyumcu.Web.Servisler.Bicim.Para(3400m, "TRY"), Kuyumcu.Web.Servisler.Bicim.Para(3400m, "TRY")), e.Find("#alan-hata-hrd-cari").TextContent.Trim()));
        Assert.Empty(e.FindAll("#hrd-onay-onayla"));
        Assert.Empty(Postlar());
    }

    [Fact]
    public void ZOSK_eksi_tahsil_reddedilir_POST_yok()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");
        Gir(e, "#hrd-tahsil", "-5");
        e.Find("#hrd-kaydet").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("[id^='alan-hata-hrd-']")));
        Assert.Empty(e.FindAll("#hrd-onay-onayla"));
        Assert.Empty(Postlar());
    }

    [Fact]
    public void ZOSK_gorulmemis_bir_kurus_eksik_ramatci_ister_0_01()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");
        Gir(e, "#hrd-tahsil", "3399,99");
        e.Find("#hrd-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(Ramatci(Kuyumcu.Web.Servisler.Bicim.Para(0.01m, "TRY"), Kuyumcu.Web.Servisler.Bicim.Para(3400m, "TRY")), e.Find("#alan-hata-hrd-cari").TextContent.Trim()));
        Assert.Empty(Postlar());
    }

    [Fact]
    public void ZOSK_kurus_yarimi_kur_oynamasi_esitlenir_AwayFromZero_3400_02_onay_govde_ayni()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");
        e.WaitForAssertion(() => Assert.Contains(Kuyumcu.Web.Servisler.Bicim.Para(3400m, "TRY"), e.Find("#hrd-tutar").TextContent));
        Kur(e, "kur-a", 6800.01m);                              // 0,5 × 6.800,01 = 3.400,005 → 3.400,01
        e.WaitForAssertion(() => Assert.Contains(Kuyumcu.Web.Servisler.Bicim.Para(3400.01m, "TRY"), e.Find("#hrd-tutar").TextContent));
        Gir(e, "#hrd-tahsil", "3400,01");
        Kur(e, "kur-b", 6800.03m);                              // 3.400,015 → 3.400,02
        e.WaitForAssertion(() => Assert.Contains(Kuyumcu.Web.Servisler.Bicim.Para(3400.02m, "TRY"), e.Find("#hrd-tutar").TextContent));
        e.Find("#hrd-kaydet").Click();
        var ozet = e.WaitForElement("#hrd-onay-hedef").TextContent;
        Assert.Contains("Tutar: " + Kuyumcu.Web.Servisler.Bicim.Para(3400.02m, "TRY"), ozet);
        Assert.Contains(Kuyumcu.Web.Servisler.Bicim.Para(3400.02m, "TRY") + " (", ozet);                  // Tahsil edilen: 3.400,02 ₺ (Nakit)
        Assert.Contains("Kur değiştiği için tahsil edilen tutar güncel tutara eşitlendi.", ozet);
        Kur(e, "kur-c", 6900m);                                 // pencere açıkken kur oynar
        e.Find("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar()));
        var g = System.Text.Json.JsonDocument.Parse(Postlar()[0]).RootElement;
        Assert.Equal(3400.02m, g.GetProperty("tahsilEdilenTl").GetDecimal());
        Assert.Equal(6800.03m, g.GetProperty("birimFiyatTlHasGram").GetDecimal());
        Assert.Equal("kur-b", g.GetProperty("kurSnapshotId").GetString());
        Assert.Equal(0.5m, g.GetProperty("hasGrami").GetDecimal());
    }

    [Fact]
    public void ZOSK_has_degisince_eski_gorulen_tutar_esitlenmez_ramatci_ister()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");
        Gir(e, "#hrd-tahsil", "3400");
        Kur(e, "kur-b", 6810m);                                 // (0,5; 3.400) görüldü
        Gir(e, "#hrd-has", "0,6");                              // 0,6 × 6.810 = 4.086,00
        e.WaitForAssertion(() => Assert.Contains(Kuyumcu.Web.Servisler.Bicim.Para(4086m, "TRY"), e.Find("#hrd-tutar").TextContent));
        e.Find("#hrd-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(Ramatci(Kuyumcu.Web.Servisler.Bicim.Para(686m, "TRY"), Kuyumcu.Web.Servisler.Bicim.Para(4086m, "TRY")), e.Find("#alan-hata-hrd-cari").TextContent.Trim()));
        Assert.Empty(Postlar());
    }

    [Fact]
    public void ZOSK_has_4_hane_0_0001_eksik_tahsil_0_cumlesi_0_68()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,0001");                           // 0,0001 × 6.800 = 0,68
        Gir(e, "#hrd-tahsil", "0,5");
        e.Find("#hrd-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(Ramatci(Kuyumcu.Web.Servisler.Bicim.Para(0.18m, "TRY"), Kuyumcu.Web.Servisler.Bicim.Para(0.68m, "TRY")), e.Find("#alan-hata-hrd-cari").TextContent.Trim()));
        Assert.Empty(Postlar());
    }

    [Fact]
    public void ZOSK_cok_buyuk_tahsil_999_999_999_99_asim_cumlesi_tr_TR()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");
        Gir(e, "#hrd-tahsil", "999999999,99");
        e.Find("#hrd-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(
            "Tahsil edilen tutar işlem tutarını (" + Kuyumcu.Web.Servisler.Bicim.Para(3400m, "TRY") + ") aşamaz; " + Kuyumcu.Web.Servisler.Bicim.Para(999996599.99m, "TRY") + " fazla yazıldı.",
            e.Find("#alan-hata-hrd-tahsil").TextContent.Trim()));
        Assert.DoesNotContain("TRY", e.Markup.Replace("\"TRY\"", ""));
        Assert.Empty(Postlar());
    }

    [Fact]
    public void ZOSK_belirsiz_1_500_tahsil_reddedilir_POST_yok()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,5");
        Gir(e, "#hrd-tahsil", "1.500");
        e.Find("#hrd-kaydet").Click();
        var tani = string.Join(" | ", e.FindAll("[id^='alan-hata-hrd-']").Select(x => x.Id + "=" + x.TextContent.Trim()))
                 + " || ONAY=" + string.Join("", e.FindAll("#hrd-onay-hedef").Select(x => x.TextContent));
        if (e.FindAll("#hrd-onay-onayla").Count > 0)
        {
            e.Find("#hrd-onay-onayla").Click();
            e.WaitForAssertion(() => Assert.Single(Postlar()), TimeSpan.FromSeconds(3));
            tani += " || POST=" + Postlar()[0];
        }
        Assert.True(Postlar().Count == 0, "belirsiz 1.500 onaya ve POST'a gitti: " + tani);
        Assert.Empty(Postlar());
    }

    [Fact]
    public void ZOSK_buyuk_kur_sicramasi_esitlemesi_onayda_GORUNUR_tutar_ve_not()
    {
        // Eşitlemenin üst sınırı yok: gözlem testi — en azından kullanıcı onayda eşitlenen tutarı görür.
        var e = Ekran();
        Gir(e, "#hrd-has", "1");
        Gir(e, "#hrd-tahsil", "6800");
        Kur(e, "kur-b", 7500m);                                 // %10 sıçrama
        e.WaitForAssertion(() => Assert.Contains(Kuyumcu.Web.Servisler.Bicim.Para(7500m, "TRY"), e.Find("#hrd-tutar").TextContent));
        e.Find("#hrd-kaydet").Click();
        var ozet = e.WaitForElement("#hrd-onay-hedef").TextContent;
        Assert.Contains("Tutar: " + Kuyumcu.Web.Servisler.Bicim.Para(7500m, "TRY"), ozet);
        Assert.Contains("Kur değiştiği için tahsil edilen tutar güncel tutara eşitlendi.", ozet);
        Assert.Empty(Postlar());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (kapanış 3, 29.09) — "ONAYDAKİ TUTAR = KAYDEDİLEN TUTAR" ve
// "isteğe bağlı ramatçı boşken gönderim hata vermez" sınırları. Sahte sunucu
// hurda-satis kuralını SatisServisi.HurdaSatis'teki gibi UYGULAR (Round6 has →
// Round2 AwayFromZero tutar; tahsil < tutar ve cari yoksa 400). Beklenen tutarlar
// SABİT yazılır.
// ═════════════════════════════════════════════════════════════════════════════
public sealed partial class ZOISLEMASinirHurdaOnayKayitTestleri : Bunit.BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly List<decimal> sunucuToplamlari = [];
    private readonly List<string> sunucuRetleri = [];

    public ZOISLEMASinirHurdaOnayKayitTestleri()
    {
        JSInterop.Mode = Bunit.JSRuntimeMode.Loose;
        Services.AddScoped<Kuyumcu.Web.Servisler.DilServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.OturumServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<Kuyumcu.Web.Servisler.ApiIstemci>();
        Services.AddScoped<Kuyumcu.Web.Servisler.CanliKurServisi>();
    }

    private sealed class Sahte(ZOISLEMASinirHurdaOnayKayitTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage i, CancellationToken c)
        {
            var yol = i.RequestUri!.AbsolutePath;
            var govde = i.Content is null ? "" : await i.Content.ReadAsStringAsync(c);
            lock (t.istekler) t.istekler.Add((i.Method.Method, i.RequestUri!.PathAndQuery, govde));
            var kod = System.Net.HttpStatusCode.OK;
            string g;
            if (yol.EndsWith("/islemler/hurda-satis"))
            {
                var b = System.Text.Json.JsonDocument.Parse(govde).RootElement;
                var has = Math.Round(b.GetProperty("hasGrami").GetDecimal(), 6, MidpointRounding.AwayFromZero);
                var kabul = b.TryGetProperty("aliciKabulHasGrami", out var k) && k.ValueKind == System.Text.Json.JsonValueKind.Number
                    ? Math.Round(k.GetDecimal(), 6, MidpointRounding.AwayFromZero) : has;
                var fiyat = b.GetProperty("birimFiyatTlHasGram").GetDecimal();
                var toplam = Math.Round(kabul * fiyat, 2, MidpointRounding.AwayFromZero);
                var tahsil = b.GetProperty("tahsilEdilenTl").GetDecimal();
                var cari = b.TryGetProperty("cariId", out var cr) && cr.ValueKind == System.Text.Json.JsonValueKind.String ? cr.GetString() : null;
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                if (tahsil > toplam || (toplam - tahsil > 0m && string.IsNullOrEmpty(cari)))
                {
                    var m = $"Tahsil edilmeyen {(toplam - tahsil).ToString(inv)} TL için ramatçı carisi zorunludur.";
                    lock (t.istekler) t.sunucuRetleri.Add(m);
                    kod = System.Net.HttpStatusCode.BadRequest;
                    g = "{\"message\":\"" + m + "\",\"error\":\"Bad Request\",\"statusCode\":400}";
                }
                else
                {
                    lock (t.istekler) t.sunucuToplamlari.Add(toplam);
                    g = "{\"id\":\"h9\",\"maden\":\"ALTIN\",\"madenAdi\":\"Altın\",\"hasGrami\":" + has.ToString(inv)
                      + ",\"birimFiyatTlHasGram\":" + fiyat.ToString(inv) + ",\"toplamTl\":" + toplam.ToString(inv)
                      + ",\"tahsilEdilenTl\":" + tahsil.ToString(inv) + ",\"kalanTl\":" + (toplam - tahsil).ToString(inv)
                      + ",\"kalanHurdaHasGram\":1,\"kasaBakiyesi\":1,\"paraBirimi\":\"TRY\",\"tarih\":\"2026-09-29T09:00:00Z\","
                      + "\"fireKaydiAcik\":false,\"fireNotu\":null,\"aliciKabulHasGrami\":" + kabul.ToString(inv)
                      + ",\"tahlilFarkiHasGram\":" + (kabul - has).ToString(inv) + "}";
                }
            }
            else g = yol switch
            {
                var y when y.EndsWith("/hurda/bakiye") =>
                    """{"subeId":"s1","satirlar":[{"maden":"ALTIN","madenAdi":"Altın","hasGram":200000,"girenHas":200000,"cikanHas":0,"tahlilFarkiHas":0}],"fireKaydiAcik":false,"fireNotu":null}""",
                var y when y.EndsWith("/hurda/defter") =>
                    """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}""",
                var y when y.EndsWith("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":250000,"aktif":true}]""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-29T09:00:00Z","ayarFiyatlari":[]}""",
                var y when y.EndsWith("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":false,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":999999999999,"islemTutariKuraliDevrede":false}""",
                _ => "{}",
            };
            return new HttpResponseMessage(kod) { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Hurda> Ekran()
    {
        Services.GetRequiredService<Kuyumcu.Web.Servisler.OturumServisi>().KullaniciAta(
            new Kuyumcu.Web.Servisler.OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "MUDUR", "s1"));
        var e = Render<Kuyumcu.Web.Pages.Hurda>();
        e.WaitForAssertion(() => Assert.False(e.Find("#hrd-kaydet").HasAttribute("disabled")));
        return e;
    }

    private static void Gir(Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e, string s, string d) =>
        e.WaitForAssertion(() => { if (e.Find(s).GetAttribute("value") != d) e.Find(s).Input(d); Assert.Equal(d, e.Find(s).GetAttribute("value")); });

    private List<string> Postlar() { lock (istekler) return istekler.Where(i => i.Metot == "POST" && i.Yol.Contains("hurda-satis")).Select(i => i.Govde).ToList(); }

    /// <summary>Kaydet → onay özeti → Onayla; sunucu KABUL etmeli ve kaydettiği tutar onaydakiyle AYNI olmalı.</summary>
    private void OnaydakiKaydedilir(Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Hurda> e, decimal beklenenTutar)
    {
        e.Find("#hrd-kaydet").Click();
        var ozet = e.WaitForElement("#hrd-onay-hedef").TextContent;
        Assert.Contains("Tutar: " + Kuyumcu.Web.Servisler.Bicim.Para(beklenenTutar, "TRY"), ozet);
        e.Find("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.Single(Postlar()));
        e.WaitForAssertion(() => Assert.True(sunucuToplamlari.Count + sunucuRetleri.Count == 1));
        Assert.Empty(sunucuRetleri);                                    // isteğe bağlı ramatçı boş: RET YOK
        Assert.Equal(beklenenTutar, sunucuToplamlari.Single());         // kaydedilen = onaydaki
        e.WaitForAssertion(() => Assert.Contains(Kuyumcu.Web.Servisler.Bicim.Para(beklenenTutar, "TRY"), e.Find("#hrd-basari").TextContent));
    }

    [Fact]
    public void ZOSO_kurus_yarimi_0_5_x_6800_01_onay_ve_kayit_3400_01_ramatcisiz()
    {
        var e = Ekran();
        e.WaitForElement("#hrd-detay-ac").Click();
        Gir(e, "#hrd-fiyat", "6800,01");                                // 3.400,005 → 3.400,01
        Gir(e, "#hrd-has", "0,5");
        OnaydakiKaydedilir(e, 3400.01m);
    }

    [Fact]
    public void ZOSO_has_4_hane_0_0001_onay_ve_kayit_0_68()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "0,0001");                                   // 0,0001 × 6.800 = 0,68
        OnaydakiKaydedilir(e, 0.68m);
    }

    [Fact]
    public void ZOSO_turkce_1_5_has_onay_ve_kayit_10200()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "1,5");
        OnaydakiKaydedilir(e, 10200m);
    }

    [Fact]
    public void ZOSO_cok_buyuk_tutar_999_999_999_80_onay_ve_kayit_ayni()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "147058,8235");                              // × 6.800 = 999.999.999,80
        OnaydakiKaydedilir(e, 999_999_999.80m);
    }

    [Fact]
    public void ZOSO_has_7_hane_onaydaki_tutar_sunucunun_kaydettigi_tutardir()
    {
        // Sunucu has'ı 6 haneye yuvarlar (Yuvarla6). Ekran ham değerle çarparsa onay 6.800,00
        // gösterir, sunucu 6.800,01 hesaplar → tahsil 0,01 eksik → ramatçı yok → 400.
        var e = Ekran();
        Gir(e, "#hrd-has", "1,0000005");
        e.Find("#hrd-kaydet").Click();
        // r18 doğrulayıcı: hata ya da onay AYNI render turunda gelmeyebilir — ikisinden biri beklenir (yarış giderildi).
        e.WaitForAssertion(() => Assert.True(e.FindAll("#alan-hata-hrd-has").Count + e.FindAll("#hrd-onay-onayla").Count > 0));
        var hataVar = e.FindAll("#alan-hata-hrd-has").Count > 0;
        if (hataVar) { Assert.Empty(e.FindAll("#hrd-onay-onayla")); Assert.Empty(Postlar()); return; }   // ekran reddetti: kabul edilebilir
        e.WaitForElement("#hrd-onay-onayla").Click();
        e.WaitForAssertion(() => Assert.True(sunucuToplamlari.Count + sunucuRetleri.Count == 1));
        Assert.Empty(sunucuRetleri);
        var ozet = e.Find("#hrd-onay-hedef").TextContent;
        Assert.Contains("Tutar: " + Kuyumcu.Web.Servisler.Bicim.Para(sunucuToplamlari.Single(), "TRY"), ozet);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("")]
    [InlineData("1.500")]
    public void ZOSO_sifir_eksi_bos_belirsiz_has_POST_yok(string has)
    {
        var e = Ekran();
        if (has.Length > 0) Gir(e, "#hrd-has", has);
        e.Find("#hrd-kaydet").Click();
        e.WaitForAssertion(() => Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-hrd-has").TextContent)));
        Assert.Empty(e.FindAll("#hrd-onay-onayla"));
        Assert.Empty(Postlar());
    }
}

// ═══ Doğrulayıcı r18 (29.09 14:00) — has 6 hane sınırının SINIR durumları ═══════════════
// Bu sınıf ZOISLEMASinirHurdaOnayKayitTestleri'nin sahte sunucusunu (Yuvarla6 + AwayFromZero
// Yuvarla2 + ramatçısız eksik tahsil 400) kullanır; beklenen tutarlar ELLE hesaplanmış sabitlerdir.
public sealed partial class ZOISLEMASinirHurdaOnayKayitTestleri
{

    [Fact]
    public void ZOSR18_has_6_hane_kurus_yarimi_0_000001_x_5000_onay_ve_kayit_0_01()
    {
        var e = Ekran();
        e.WaitForElement("#hrd-detay-ac").Click();
        Gir(e, "#hrd-fiyat", "5000");                                // 0,000001 × 5000 = 0,005 → 0,01
        Gir(e, "#hrd-has", "0,000001");
        // ISLEM-BANT-2: fiyat ramat satış bandının (kur 6.800 → %80–%120) dışında; Kaydet artık bant dışında kilitli —
        // SAHİP/MÜDÜR "Fiyat doğru, yine de kaydet" + 20+ karakter gerekçe ile açar (sunucu kuralıyla aynı).
        // Bant onayı/gerekçe kutusu fiyat girişinin render'ından SONRA çizilir — yük altında Find erken
        // koşup bulamıyordu (17.10 tam koşuda tek seferlik kırmızı); bekleyerek bulunur.
        e.WaitForElement("#hrd-bant-onay").Change(true);
        e.WaitForElement("#hrd-bant-gerekce").Input("Ramatçı ile sözleşmeli fiyat, teyit edildi.");
        OnaydakiKaydedilir(e, 0.01m);
    }

    [Fact]
    public void ZOSR18_has_7_hane_SONDAKI_SIFIR_ek_hassasiyet_degildir_kabul_6800()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "1,0000000");
        OnaydakiKaydedilir(e, 6800m);
    }

    [Fact]
    public void ZOSR18_has_6_hane_cok_buyuk_999_999_999_99_onay_ve_kayit_ayni()
    {
        var e = Ekran();
        Gir(e, "#hrd-has", "147058,823528");                         // × 6800 = 999.999.999,9904
        OnaydakiKaydedilir(e, 999_999_999.99m);
    }

    [Fact]
    public void ZOSR18_kabul_6_hane_farkli_gerekceli_onay_ve_kayit_6799_99_ramatcisiz()
    {
        var e = Ekran();
        e.WaitForElement("#hrd-detay-ac").Click();
        Gir(e, "#hrd-has", "1");
        Gir(e, "#hrd-kabul-has", "0,999999");                        // × 6800 = 6799,9932 → 6799,99
        e.WaitForElement("#hrd-tahlil-gerekce").Input("Rafineri tahlili bizimkinden dusuk cikti");
        OnaydakiKaydedilir(e, 6799.99m);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0,000001")]
    [InlineData("0,0000001")]
    public void ZOSR18_kabul_sifir_eksi_7_hane_alan_hatasi_POST_yok(string kabul)
    {
        var e = Ekran();
        e.WaitForElement("#hrd-detay-ac").Click();
        Gir(e, "#hrd-has", "1");
        Gir(e, "#hrd-kabul-has", kabul);
        e.Find("#hrd-kaydet").Click();
        e.WaitForAssertion(() => Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-hrd-kabul-has").TextContent)));
        Assert.Empty(e.FindAll("#hrd-onay-onayla"));
        Assert.Empty(Postlar());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI — 19. tur (29.09): müşteriyle kümülatif MASAK gereksinimi
// (MasakServisi.Gereksinim(tutar, cariId, tenant)) ve POS "birebir" önerisi.
// Beklenen değerler SABİT yazılır; eşikler 185.000 / 200.000 / 2.000.000,01 / 20.000.000,01.
// MASAK-370: kimlik eşiği 185.000 06.10.2026'ya dek yürürlükteki DÖNEM değeridir ve burada açıkça
// verilir (varsayılan 07.10.2026'dan itibaren 370.000 — YALNIZ_KIMLIK sınırı o dönemde oluşmaz).
// 370.000 dönemi ve geçiş Masak370Testleri'nde.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class ZOISLEMASinirR19Testleri(TestVeritabani vt)
{
    private static Kuyumcu.Api.Modules.Masak.MasakServisi Masak(KuyumcuDbContext db) =>
        new(db, new Kuyumcu.Api.Modules.Masak.MasakKuralMotoru(new Kuyumcu.Api.Modules.Masak.MasakEsikleri { KimlikEsigi = 185_000m }),
            TestKurulum.Denetim(db));

    private static System.Text.Json.JsonElement Json(object o) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o)).RootElement;

    [Fact]
    public async Task ZOSR19_gereksinim_kumulatif_TAM_ESIK_185000_kimlik_bir_kurus_alti_YOK()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOSR19 esik");
        PosYardim.BankaHareketiYaz(vt, k, 85_000m, cariId: k.CariId);
        using var db = vt.Olustur(k);
        var s = Masak(db);
        var tam = await s.Gereksinim(100_000m, k.CariId, k.TenantId);          // 185.000,00
        Assert.True(tam.KimlikGerekli);
        Assert.False(tam.AmacGerekli);
        var alt = await s.Gereksinim(99_999.99m, k.CariId, k.TenantId);        // 184.999,99
        Assert.False(alt.KimlikGerekli);
        var yarim = await s.Gereksinim(99_999.995m, k.CariId, k.TenantId);     // 184.999,995 < eşik
        Assert.False(yarim.KimlikGerekli);
        var amac = await s.Gereksinim(115_000m, k.CariId, k.TenantId);         // 200.000,00
        Assert.True(amac.AmacGerekli);
        Assert.False(amac.BeyanFormuGerekli);
    }

    [Fact]
    public async Task ZOSR19_gereksinim_sifir_ve_eksi_tutar_kumulatife_mutlak_eklenir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOSR19 eksi");
        PosYardim.BankaHareketiYaz(vt, k, 190_000m, cariId: k.CariId);
        using var db = vt.Olustur(k);
        var s = Masak(db);
        // Sıfır tutar: önceki 190.000 zaten kimlik eşiğinin üstünde → kimlik (işaret karıştırmaz).
        Assert.True((await s.Gereksinim(0m, k.CariId, k.TenantId)).KimlikGerekli);
        // Eksi tutar mutlak değerle eklenir: 190.000 + 10.000 = 200.000 → amaç beyanı (180.000'e DÜŞMEZ).
        var eksi = await s.Gereksinim(-10_000m, k.CariId, k.TenantId);
        Assert.True(eksi.AmacGerekli);
        // Müşterisiz eksi: tek işlem kuralı, mutlak 10.000 → hiçbir şey.
        Assert.False((await s.Gereksinim(-10_000m, null, k.TenantId)).KimlikGerekli);
    }

    [Fact]
    public async Task ZOSR19_gereksinim_cok_buyuk_ve_bos_cari_ve_bilinmeyen_cari()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOSR19 buyuk");
        using var db = vt.Olustur(k);
        var s = Masak(db);
        var buyuk = await s.Gereksinim(999_999_999.99m, k.CariId, k.TenantId);
        Assert.True(buyuk.EkBelgeGerekli);
        Assert.True(buyuk.BeyanFormuGerekli);
        // Boş/boşluk cari = müşterisiz tek işlem kuralı.
        Assert.False((await s.Gereksinim(150_000m, "   ", k.TenantId)).KimlikGerekli);
        Assert.False((await s.Gereksinim(150_000m, "", k.TenantId)).KimlikGerekli);
        // Var olmayan cari kimliği: toplam 0, tek işlem kuralı (hata fırlatmaz).
        Assert.False((await s.Gereksinim(150_000m, "01JXXXXXXXXXXXXXXXXXXXXXXX", k.TenantId)).KimlikGerekli);
        Assert.True((await s.Gereksinim(185_000m, "01JXXXXXXXXXXXXXXXXXXXXXXX", k.TenantId)).KimlikGerekli);
    }

    [Fact]
    public async Task ZOSR19_oneri_birebir_bir_kurus_fark_ONERMEZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOSR19 kurus");
        var cihaz = PosYardim.CihazYaz(vt, k);
        PosYardim.TahsilatYaz(vt, k, cihaz, 20_000m);     // net 19.630
        PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);     // net 9.815
        var h = PosYardim.BankaHareketiYaz(vt, k, 9_815.01m);
        using var db = vt.Olustur(k);
        var j = Json(await PosYardim.Servis(db)
            .OtoEslestirOnerisi(new MPos.PosOtoOneriDto { BankaHareketId = h }, k.Islem));
        Assert.False(j.GetProperty("bulundu").GetBoolean());
        Assert.Equal(9_815.01m, j.GetProperty("hareketKalan").GetDecimal());
    }

    [Fact]
    public async Task ZOSR19_oneri_TAMAMI_eslesmis_harekette_kalan_sifir_ONERMEZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOSR19 sifir kalan");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var ilk = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);   // net 9.815
        PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);             // aynı net — birebir adayı olurdu
        var h = PosYardim.BankaHareketiYaz(vt, k, 9_815m);
        using (var db0 = vt.Olustur(k))
            await PosYardim.Servis(db0).Eslestir(new MPos.PosEslestirDto
            {
                BankaHareketId = h,
                Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = ilk, Tutar = 9_815m }],
            }, k.Islem);
        using var db = vt.Olustur(k);
        var j = Json(await PosYardim.Servis(db)
            .OtoEslestirOnerisi(new MPos.PosOtoOneriDto { BankaHareketId = h }, k.Islem));
        Assert.False(j.GetProperty("bulundu").GetBoolean());
        Assert.Equal(0m, j.GetProperty("hareketKalan").GetDecimal());
        Assert.Empty(j.GetProperty("satirlar").EnumerateArray());
    }

    [Fact]
    public async Task ZOSR19_oneri_birebir_MUSTERI_havalesinde_kapi_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZOSR19 musteri");
        var cihaz = PosYardim.CihazYaz(vt, k);
        PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);     // net 9.815
        var h = PosYardim.BankaHareketiYaz(vt, k, 9_815m, cariId: k.CariId);
        using var db = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() => PosYardim.Servis(db)
            .OtoEslestirOnerisi(new MPos.PosOtoOneriDto { BankaHareketId = h }, k.Islem));
    }

    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("0,0001", "0.0001")]
    [InlineData("999.999.999,99", "999999999.99")]
    public void ZOSR19_turkce_giris_cozulur(string ham, string beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }

    [Theory]
    [InlineData("1.500")]
    [InlineData("")]
    [InlineData(null)]
    public void ZOSR19_turkce_giris_belirsiz_ve_bos_deger_uretmez(string? ham)
    {
        Assert.Null(FormAlani.SayiCoz(ham).Deger);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI — ZO-ISLEM-A 20. tur (P-23 ODEME_KALEMSIZ_YOL, OdemeTutarKapisi)
// Doğrulayıcının kendi sınır testleri: sıfır · eksi (servis yolu, DTO atlanır) ·
// kuruş sınırı 0,005 (AwayFromZero) · has 4 hane · çok büyük · boş/null · Türkçe giriş.
// Her paralı test ayrıca "etiketli ama kalemsiz hareket = 0" ölçütünü sınar.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class ZOISLEMASinirR20Testleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Servis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance,
            hurdaAyarlari: new MHurda.HurdaAyarlari { FireKaydiAcik = false });

    private int KalemsizEtiketli(Kiraci k)
    {
        using var kontrol = vt.OlusturSistem();
        var hepsi = kontrol.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId).ToList();
        var kalemli = kontrol.OdemeKalemleri.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId && x.Aktif).Select(x => x.HareketId).ToHashSet();
        return hepsi.Count(h => h.OdemeTuru is "NAKIT" or "KART" or "HAVALE" or "VERESIYE" or "DOVIZ"
                                && h.StornoKaynakId is null && !kalemli.Contains(h.Id));
    }

    private int SatisSayisi(Kiraci k)
    {
        using var kontrol = vt.OlusturSistem();
        return kontrol.Hareketler.AsNoTracking().Count(h => h.TenantId == k.TenantId && h.Tur == "SATIS");
    }

    private static MSatis.SatisOlusturDto Dto(Kiraci k, decimal fiyat, string? odeme = "KART", decimal adet = 1m) => new()
    {
        CariId = k.CariId, UrunId = k.UrunId, Adet = adet, BirimFiyatTl = fiyat,
        OdemeTuru = odeme!, KasaId = odeme == "VERESIYE" ? null : k.KasaId, KurSnapshotId = k.KurId,
    };

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.004)]
    public async Task ZOSR20_sifir_ve_kurus_alti_satis_400_satir_yok(double fiyat)
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETZO-ISLEM-A-TEST dogr sifir");
        using var db = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Satis(Dto(k, (decimal)fiyat), k.Islem));
        Assert.StartsWith("Satış tutarı 0 TL olamaz", ex.Message);
        Assert.Equal(0, SatisSayisi(k));
        Assert.Equal(0, KalemsizEtiketli(k));
    }

    [Fact]
    public async Task ZOSR20_kurus_yarimi_0_005_gecer_hareket_ve_kalem_0_01()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETZO-ISLEM-A-TEST dogr kurus");
        using var db = vt.Olustur(k);
        await Servis(db).Satis(Dto(k, 0.005m), k.Islem);
        using var kontrol = vt.OlusturSistem();
        var h = kontrol.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "SATIS");
        var kalem = Assert.Single(kontrol.OdemeKalemleri.AsNoTracking().Where(x => x.TenantId == k.TenantId).ToList());
        Assert.Equal(0.01m, h.TlTutari);
        Assert.Equal(0.01m, kalem.TutarTl);
        Assert.Equal(0, KalemsizEtiketli(k));
    }

    [Fact]
    public async Task ZOSR20_eksi_birim_fiyat_servis_yolu_DAVRANISI_olculur()
    {
        // BULGU (doğrulayıcı, ORTA): DTO [Range(0,…)] yalnız controller yolunda koşar; servis
        // eksi fiyatlı satışı REDDETMİYOR. HTTP yolu 400 verir (ZOSR20h testi). Bu test mevcut
        // davranışı SABİTLER ve en azından kalemsiz etiketli satır doğmadığını sınar.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETZO-ISLEM-A-TEST dogr eksi");
        using var db = vt.Olustur(k);
        await Servis(db).Satis(Dto(k, -100m), k.Islem);
        using var kontrol = vt.OlusturSistem();
        var h = kontrol.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "SATIS");
        Assert.Equal(-100m, h.TlTutari);
        Assert.Equal(0, KalemsizEtiketli(k));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("kart")]
    [InlineData("ALTIN")]
    [InlineData("KARMA")]
    public async Task ZOSR20_bos_null_ve_liste_disi_odeme_turu_400(string? odeme)
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETZO-ISLEM-A-TEST dogr odeme");
        using var db = vt.Olustur(k);
        var dto = Dto(k, 5_000m);
        dto.OdemeTuru = odeme!;
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Satis(dto, k.Islem));
        Assert.Equal(0, SatisSayisi(k));
    }

    [Fact]
    public async Task ZOSR20_hurda_has_4_hane_0_0001_gram_kurus_altinda_400()
    {
        // ISLEM-BANT: 50 TL/has g artık bant dışı (mutlak 500). Aynı sınır geçerli fiyatla:
        // 0,000001 g K22 → has 0,000001 g; × 3.000 TL = 0,003 TL → 2 hane 0,00 → 400.
        var k = IslemBantFikstur.ReferansKur(vt.OlusturSistem(), TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETZO-ISLEM-A-TEST dogr has"));
        using var db = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).HurdaAlis(new MSatis.HurdaAlisDto
        {
            CariId = k.CariId, AyarId = "K22", GramAgirlik = 0.000001m, BirimFiyatTlHasGram = 3_000m,
            KasaId = k.KasaId, KurSnapshotId = k.KurId, Satici = TestKurulum.HurdaSatici(),
        }, k.Islem));
        Assert.StartsWith("Hurda alışı tutarı 0 TL olamaz", ex.Message);
        Assert.Equal(0, KalemsizEtiketli(k));
    }

    [Theory]
    [InlineData("999999999.99", false)]
    [InlineData("-999999999.99", false)]
    [InlineData("0.0049", true)]
    [InlineData("-0.005", false)]
    [InlineData("0.0001", true)]
    public void ZOSR20_ParaYok_cok_buyuk_eksi_ve_has_hanesi(string tutar, bool beklenen)
        => Assert.Equal(beklenen, MSatis.OdemeTutarKapisi.ParaYok(
            decimal.Parse(tutar, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public async Task ZOSR20_cok_buyuk_satis_ya_reddedilir_ya_kalemiyle_yazilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETZO-ISLEM-A-TEST dogr buyuk");
        using var db = vt.Olustur(k);
        try { await Servis(db).Satis(Dto(k, 999_999_999.99m, "VERESIYE"), k.Islem); }
        catch (BadRequestException) { /* MASAK/tevsik kapısı reddedebilir — kabul */ }
        Assert.Equal(0, KalemsizEtiketli(k));
    }

    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    public void ZOSR20_turkce_giris(string ham, string beklenen)
        => Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), FormAlani.SayiCoz(ham).Deger);

    [Fact]
    public void ZOSR20_turkce_belirsiz_1_500_reddi() => Assert.Null(FormAlani.SayiCoz("1.500").Deger);
}

[Collection("http")]
public sealed class ZOISLEMASinirR20HttpTestleri(HttpFikstur f)
{
    [Theory]
    [InlineData(-100)]
    [InlineData(-0.01)]
    public async Task ZOSR20h_eksi_birim_fiyat_HTTP_yolunda_400_satir_yok(double fiyat)
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETZO-ISLEM-A-TEST-http dogr eksi");
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/islemler/satis", new
        {
            cariId = k.CariId, urunId = k.UrunId, adet = 1, birimFiyatTl = (decimal)fiyat, odemeTuru = "KART",
            kasaId = k.KasaId, kurSnapshotId = k.KurId,
        });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, c.StatusCode);
        using var kontrol = f.Vt.OlusturSistem();
        Assert.Equal(0, kontrol.Hareketler.AsNoTracking().Count(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
    }
}
