using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET ISLEM-BANT finansal sınır testleri.
// Paket işlem içi elle yazılan HAS GRAM FİYATINA (hurda alış/satış, takas, eski altınla
// ödeme) bant kapısı ekledi. Referans 6.827,20 (TestKurulum.KiraciKur kur kaydı).
// Beklenen değerler SABİT yazılır (üretim kodundan hesaplanmaz):
//   alış bandı %50–%110 → 3.413,60 – 7.509,92 · hurda satışı ± %20 → 5.461,76 – 8.192,64
//   mutlak sınır 500 – 50.000 (onayla aşılamaz).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class ISLEMBANTSinirCekirdekTestleri
{
    private const decimal R = 6_827.20m;
    private const string Altin = "ALTIN";

    [Fact]
    public void IBS_sifir_ve_null_bu_kuralin_degil()
    {
        Assert.Null(FiyatBandi.IslemDenetle(0m, IslemFiyatBaglami.HurdaAlis, R, Altin));
        Assert.Null(FiyatBandi.IslemDenetle(null, IslemFiyatBaglami.HurdaAlis, R, Altin));
        Assert.Null(FiyatBandi.IslemDenetle(0m, IslemFiyatBaglami.HurdaSatis, null, Altin));
    }

    [Fact]
    public void IBS_eksi_deger_bant_kuralina_girmez_baska_kapi_reddeder()
    {
        // Eksi işaret bant kuralının konusu değil (null döner); sunucuda OdemeTutarKapisi/Range reddeder
        // (IB10 HTTP -5 → 400 ile kanıtlı). Burada yalnız bantın eksiyi "geçerli" İŞARETLEMEDİĞİ:
        Assert.Null(FiyatBandi.IslemDenetle(-5m, IslemFiyatBaglami.HurdaAlis, R, Altin));
        Assert.Null(FiyatBandi.IslemDenetle(-0.0001m, IslemFiyatBaglami.Takas, R, Altin));
    }

    [Fact]
    public void IBS_kurus_siniri_alt_uc_3413_60_dahil_3413_59_disi_ve_yarim_kurus()
    {
        const IslemFiyatBaglami b = IslemFiyatBaglami.HurdaAlis;
        Assert.Null(FiyatBandi.IslemDenetle(3_413.60m, b, R, Altin));
        Assert.Equal(new GirdiHatasi(GirdiKodu.IslemFiyatBantDisi, 3_413.60m, 7_509.92m),
            FiyatBandi.IslemDenetle(3_413.595m, b, R, Altin));
        Assert.Null(FiyatBandi.IslemDenetle(3_413.605m, b, R, Altin));
        Assert.Null(FiyatBandi.IslemDenetle(7_509.92m, b, R, Altin));
        Assert.NotNull(FiyatBandi.IslemDenetle(7_509.925m, b, R, Altin));
        // Referansın yarım kuruşu: 6.827,21 × %50 = 3.413,605 → alt uç YUKARI 3.413,61; × %110 = 7.509,931 → üst AŞAĞI 7.509,93.
        Assert.Equal((3_413.61m, 7_509.93m), FiyatBandi.IslemBantUclari(b, 6_827.21m));
    }

    [Fact]
    public void IBS_dort_hane_has_fiyat_ve_referans()
    {
        const IslemFiyatBaglami b = IslemFiyatBaglami.HurdaSatis;
        Assert.Null(FiyatBandi.IslemDenetle(5_461.7600m, b, R, Altin));
        Assert.NotNull(FiyatBandi.IslemDenetle(5_461.7599m, b, R, Altin));
        Assert.Null(FiyatBandi.IslemDenetle(8_192.6400m, b, R, Altin));
        Assert.NotNull(FiyatBandi.IslemDenetle(8_192.6401m, b, R, Altin));
        // Referans 4 hane: 6.827,2345 × %80 = 5.461,7876 → yukarı 5.461,79; × %120 = 8.192,6814 → aşağı 8.192,68.
        Assert.Equal((5_461.79m, 8_192.68m), FiyatBandi.IslemBantUclari(b, 6_827.2345m));
    }

    [Fact]
    public void IBS_cok_buyuk_tutar_mutlak_disi_onaylanamaz_tasma_yok()
    {
        var h = FiyatBandi.IslemDenetle(999_999_999.99m, IslemFiyatBaglami.EskiAltinOdeme, R, Altin);
        Assert.Equal(new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, 500m, 50_000m), h);
        Assert.False(FiyatBandi.OnaylanabilirMi(h!.Value));
        // Referansın kendisi çok büyük olsa da (bozuk kur kaydı) hesap taşmaz; mutlak üst sınır kalır.
        Assert.Equal(GirdiKodu.FiyatMutlakDisi,
            FiyatBandi.IslemDenetle(50_000.01m, IslemFiyatBaglami.HurdaAlis, 999_999_999.99m, Altin)!.Value.Kod);
        Assert.Equal(GirdiKodu.FiyatMutlakDisi,
            FiyatBandi.IslemDenetle(499.99m, IslemFiyatBaglami.HurdaAlis, null, Altin)!.Value.Kod);
        Assert.Null(FiyatBandi.IslemDenetle(500m, IslemFiyatBaglami.HurdaAlis, null, Altin));
    }

    [Fact]
    public void IBS_turkce_giris_45_000_ve_1_5_belirsiz_1_500_reddi()
    {
        Assert.Equal(45_000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
        // Türkçe çözülen 45.000 TL/has g bandı aşar (alış üst 7.509,92) ama mutlak içindedir → onaylanabilir.
        var h = FiyatBandi.IslemDenetle(FormAlani.SayiCoz("45.000").Deger, IslemFiyatBaglami.HurdaAlis, R, Altin);
        Assert.Equal(GirdiKodu.IslemFiyatBantDisi, h!.Value.Kod);
        Assert.True(FiyatBandi.OnaylanabilirMi(h.Value));
    }

    [Fact]
    public void IBS_ileti_TR_EN_para_bicimi_TRY_yok()
    {
        var h = new GirdiHatasi(GirdiKodu.IslemFiyatBantDisi, 5_461.76m, 8_192.64m);
        Assert.Contains("(5.461,76–8.192,64 TL)", GirdiSozlugu.Mesaj(h, "tr"));
        Assert.Contains("(5,461.76–8,192.64 TL)", GirdiSozlugu.Mesaj(h, "en"));
        Assert.DoesNotContain("TRY", GirdiSozlugu.Mesaj(h, "en"));
    }
}

[Collection("veritabani")]
public sealed class ISLEMBANTSinirServisTestleri(TestVeritabani vt)
{
    private const string Gerekce20 = "12345678901234567890";   // tam 20 karakter

    private static AktifKullanici Kisi(Kiraci k, string rol) => new(k.KullaniciId, k.TenantId, k.SubeId, rol);

    [Fact]
    public async Task IBS_mutlak_disi_TEZGAHTAR_onayli_bile_400_403_degil_iz_yok()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETISLEM-BANT-TEST dg mutlak");
        var db = vt.Olustur(k);
        var r = await Assert.ThrowsAsync<BadRequestException>(() => IslemFiyatKapisi.Denetle(
            db, Kisi(k, "TEZGAHTAR"), k.TenantId, IslemFiyatBaglami.HurdaAlis, "ALTIN", 5m, "birimFiyatTlHasGram", true, Gerekce20));
        Assert.StartsWith("Bu fiyat olağan aralığın dışında (500,00–50.000,00 TL)", r.Message);   // ISLEM-BANT tur 2: mutlak sınır para biçimi
        await Assert.ThrowsAsync<BadRequestException>(() => IslemFiyatKapisi.Denetle(
            db, Kisi(k, "SAHIP"), k.TenantId, IslemFiyatBaglami.HurdaSatis, "ALTIN", 999_999_999.99m, "birimFiyatTlHasGram", true, Gerekce20));
        await Assert.ThrowsAsync<BadRequestException>(() => IslemFiyatKapisi.Denetle(
            db, null, k.TenantId, IslemFiyatBaglami.Takas, "ALTIN", 50_000.01m, "eskiUrunBirimFiyatTlHasGram", true, Gerekce20));
        Assert.Empty(db.ChangeTracker.Entries<DenetimKaydi>());
    }

    [Fact]
    public async Task IBS_bant_ici_onay_isaretli_olsa_da_iz_yok_rol_bakilmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETISLEM-BANT-TEST dg ici");
        var db = vt.Olustur(k);
        await IslemFiyatKapisi.Denetle(db, Kisi(k, "TEZGAHTAR"), k.TenantId, IslemFiyatBaglami.HurdaAlis, "ALTIN",
            3_413.60m, "birimFiyatTlHasGram", true, null);
        await IslemFiyatKapisi.Denetle(db, null, k.TenantId, IslemFiyatBaglami.HurdaSatis, "ALTIN",
            8_192.64m, "birimFiyatTlHasGram", null, null);
        // Sıfır / eksi: bu kapının konusu değil, sessizce geçer (başka kapı reddeder).
        await IslemFiyatKapisi.Denetle(db, null, k.TenantId, IslemFiyatBaglami.HurdaAlis, "ALTIN", 0m, "birimFiyatTlHasGram", null, null);
        await IslemFiyatKapisi.Denetle(db, null, k.TenantId, IslemFiyatBaglami.HurdaAlis, "ALTIN", -1m, "birimFiyatTlHasGram", null, null);
        Assert.Empty(db.ChangeTracker.Entries<DenetimKaydi>());
    }

    [Fact]
    public async Task IBS_gerekce_19_karakter_400_20_karakter_iz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETISLEM-BANT-TEST dg gerekce");
        var db = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() => IslemFiyatKapisi.Denetle(
            db, Kisi(k, "MUDUR"), k.TenantId, IslemFiyatBaglami.EskiAltinOdeme, "ALTIN", 3_413.59m, "birimFiyatTlHasGram", true, Gerekce20[..19]));
        await Assert.ThrowsAsync<BadRequestException>(() => IslemFiyatKapisi.Denetle(
            db, Kisi(k, "MUDUR"), k.TenantId, IslemFiyatBaglami.EskiAltinOdeme, "ALTIN", 3_413.59m, "birimFiyatTlHasGram", true, "   "));
        Assert.Empty(db.ChangeTracker.Entries<DenetimKaydi>());
        await IslemFiyatKapisi.Denetle(
            db, Kisi(k, "MUDUR"), k.TenantId, IslemFiyatBaglami.EskiAltinOdeme, "ALTIN", 3_413.59m, "birimFiyatTlHasGram", true, Gerekce20);
        var iz = Assert.Single(db.ChangeTracker.Entries<DenetimKaydi>()).Entity;
        Assert.Contains("birimFiyatTlHasGram=3413.59", iz.Detay);   // InvariantCulture
        Assert.True(iz.Hassas);
    }

    [Fact]
    public async Task IBS_TEZGAHTAR_onaysiz_bant_disi_400_onayli_403()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETISLEM-BANT-TEST dg tez");
        var db = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() => IslemFiyatKapisi.Denetle(
            db, Kisi(k, "TEZGAHTAR"), k.TenantId, IslemFiyatBaglami.Takas, "ALTIN", 7_509.93m, "eskiUrunBirimFiyatTlHasGram", false, null));
        await Assert.ThrowsAsync<ForbiddenException>(() => IslemFiyatKapisi.Denetle(
            db, Kisi(k, "TEZGAHTAR"), k.TenantId, IslemFiyatBaglami.Takas, "ALTIN", 7_509.93m, "eskiUrunBirimFiyatTlHasGram", true, Gerekce20));
        Assert.Empty(db.ChangeTracker.Entries<DenetimKaydi>());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI tur 3 (09.10, Aşkın + Mehmet) — ek sınırlar. Beklenen değerler
// ELLE hesaplanmış sabitlerdir (üretim kodundan türetilmez):
//   referans 6.666,6667 (4 hane): alış %50 = 3.333,333335 → alt uç YUKARI 3.333,34;
//   %110 = 7.333,33337 → üst uç AŞAĞI 7.333,33. Satış %80 = 5.333,33336 → 5.333,34;
//   %120 = 8.000,00004 → 8.000,00.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class ISLEMBANTSinirDogrulayiciTur3Testleri
{
    private const decimal R4 = 6_666.6667m;
    private const string Altin = "ALTIN";

    [Fact]
    public void IBS3_dort_haneli_referans_alis_uclari_kurusa_ice_dogru()
    {
        const IslemFiyatBaglami b = IslemFiyatBaglami.HurdaAlis;
        Assert.Equal((3_333.34m, 7_333.33m), FiyatBandi.IslemBantUclari(b, R4));
        Assert.Null(FiyatBandi.IslemDenetle(3_333.34m, b, R4, Altin));
        Assert.Equal(new GirdiHatasi(GirdiKodu.IslemFiyatBantDisi, 3_333.34m, 7_333.33m),
            FiyatBandi.IslemDenetle(3_333.3399m, b, R4, Altin));          // 0,0001 eksik → dışarı
        Assert.Null(FiyatBandi.IslemDenetle(7_333.33m, b, R4, Altin));
        Assert.NotNull(FiyatBandi.IslemDenetle(7_333.3301m, b, R4, Altin)); // 0,0001 fazla → dışarı
    }

    [Fact]
    public void IBS3_dort_haneli_referans_satis_uclari()
    {
        const IslemFiyatBaglami b = IslemFiyatBaglami.HurdaSatis;
        Assert.Equal((5_333.34m, 8_000.00m), FiyatBandi.IslemBantUclari(b, R4));
        Assert.NotNull(FiyatBandi.IslemDenetle(5_333.33m, b, R4, Altin));
        Assert.Null(FiyatBandi.IslemDenetle(8_000.00m, b, R4, Altin));
        Assert.NotNull(FiyatBandi.IslemDenetle(8_000.01m, b, R4, Altin));
    }

    [Fact]
    public void IBS3_referans_sifir_ya_da_eksi_ise_yalniz_mutlak_sinir()
    {
        Assert.Null(FiyatBandi.IslemDenetle(600m, IslemFiyatBaglami.HurdaAlis, 0m, Altin));
        Assert.Null(FiyatBandi.IslemDenetle(49_999.99m, IslemFiyatBaglami.Takas, -6_000m, Altin));
        Assert.Equal(GirdiKodu.FiyatMutlakDisi,
            FiyatBandi.IslemDenetle(499.9999m, IslemFiyatBaglami.EskiAltinOdeme, 0m, Altin)!.Value.Kod);
    }

    [Fact]
    public void IBS3_eksi_ve_sifir_ve_cok_buyuk_her_baglamda()
    {
        foreach (var b in new[] { IslemFiyatBaglami.HurdaAlis, IslemFiyatBaglami.HurdaSatis, IslemFiyatBaglami.Takas, IslemFiyatBaglami.EskiAltinOdeme })
        {
            Assert.Null(FiyatBandi.IslemDenetle(-0.01m, b, R4, Altin));   // işaret hatası başka kapının (alan kuralı)
            Assert.Null(FiyatBandi.IslemDenetle(0m, b, R4, Altin));
            var h = FiyatBandi.IslemDenetle(999_999_999.99m, b, R4, Altin)!.Value;
            Assert.Equal(new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, 500m, 50_000m), h);
            Assert.False(FiyatBandi.OnaylanabilirMi(h));
        }
    }
}

[Collection("veritabani")]
public sealed class ISLEMBANTSinirServisTur3Testleri(TestVeritabani vt)
{
    private const string Gerekce = "Ayar dusuk cikti, ustayla teyit edildi.";

    [Fact]
    public async Task IBS3_kullanicisiz_ic_yol_onayli_oran_disi_403_fail_closed_iz_yok()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETISLEM-BANT-TEST dg3 ic");
        var db = vt.Olustur(k);
        // Referans 6.827,20 → alış alt uç 3.413,60; 3.413,59 oran dışı, onaylı ama kullanıcı yok → 403.
        await Assert.ThrowsAsync<ForbiddenException>(() => IslemFiyatKapisi.Denetle(
            db, null, k.TenantId, IslemFiyatBaglami.HurdaAlis, "ALTIN", 3_413.59m, "birimFiyatTlHasGram", true, Gerekce));
        // MUHASEBE (küçük harf 'sahip' rol adı da) yetkili sayılmaz.
        await Assert.ThrowsAsync<ForbiddenException>(() => IslemFiyatKapisi.Denetle(
            db, new AktifKullanici(k.KullaniciId, k.TenantId, k.SubeId, "sahip"), k.TenantId, IslemFiyatBaglami.HurdaAlis, "ALTIN", 3_413.59m, "birimFiyatTlHasGram", true, Gerekce));
        Assert.Empty(db.ChangeTracker.Entries<DenetimKaydi>());
    }

    [Fact]
    public async Task IBS3_gumus_madende_kapi_devreye_girmez_altinda_girer()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETISLEM-BANT-TEST dg3 maden");
        var db = vt.Olustur(k);
        await IslemFiyatKapisi.Denetle(db, null, k.TenantId, IslemFiyatBaglami.HurdaSatis, "GUMUS", 5m, "birimFiyatTlHasGram", null, null);
        var r = await Assert.ThrowsAsync<BadRequestException>(() => IslemFiyatKapisi.Denetle(
            db, null, k.TenantId, IslemFiyatBaglami.HurdaSatis, "ALTIN", 5_461.75m, "birimFiyatTlHasGram", null, null));
        Assert.Contains("(5.461,76–8.192,64 TL)", r.Message);
    }
}
