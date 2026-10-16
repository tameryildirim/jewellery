using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MHurda = Kuyumcu.Api.Modules.Hurda;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-ISLEM-A — /hurda "şube bağlamı gerekli" kök nedeni [DB]
//
// ÖLÇÜLEN (kuyumcu_net, 25.09): şubesi atanmamış MÜDÜR kullanıcıları var; tek
// aktif şubeli kiracıda kullanıcı kartı "Müdür · Merkez Şube" yazarken /hurda
// 400 veriyordu. Kural (HurdaStokKapisi.OturumSubesiCoz):
//   • oturumun şubesi varsa O (değişmez);
//   • yoksa + TÜM-ŞUBE rolü (SAHIP/MUDUR/MUHASEBE) + TEK aktif şube → o şube;
//   • aksi hâlde (çok şube, şubeye kilitli rol) 400 KALIR (fail-closed).
// Rol × şubeli/şubesiz × tek/çok şube matrisi burada sınanır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class ZOISLEMASubeTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance,
            hurdaAyarlari: new MHurda.HurdaAyarlari { FireKaydiAcik = false });

    private static MHurda.HurdaStokServisi Stok(KuyumcuDbContext db) =>
        new(db, new MHurda.HurdaAyarlari { FireKaydiAcik = false });

    private static MSatis.HurdaAlisDto Alis(Kiraci k) => new()
    {
        CariId = k.CariId, AyarId = "K22", GramAgirlik = 10m,
        BirimFiyatTlHasGram = 2_800m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        Satici = TestKurulum.HurdaSatici(),
    };

    private static MSatis.HurdaSatisDto Cikis(Kiraci k, decimal has) => new()
    {
        Maden = "ALTIN", HasGrami = has, BirimFiyatTlHasGram = 2_900m,
        KasaId = k.KasaId, KurSnapshotId = k.KurId,
    };

    private static AktifKullanici Subesiz(Kiraci k, string rol) => new(k.KullaniciId, k.TenantId, null, rol);

    [Theory]
    [InlineData("SAHIP")]
    [InlineData("MUDUR")]
    [InlineData("MUHASEBE")]
    public async Task ZO_subesiz_tum_sube_rolu_TEK_subeli_kiracida_o_subeye_cozulur(string rol)
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, $"ZO-tek-{rol}");
        using var db = vt.Olustur(k);

        var cozulen = await MHurda.HurdaStokKapisi.OturumSubesiCoz(db, Subesiz(k, rol));
        Assert.Equal(k.SubeId, cozulen.SubeId);
        Assert.Equal(rol, cozulen.Rol);
        Assert.Equal(k.KullaniciId, cozulen.Id);
    }

    [Fact]
    public async Task ZO_subesiz_mudur_tek_subede_bakiye_okur_ve_ramat_cikisi_O_SUBEYE_yazilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "ZO-mudur-cikis"));
        using var db = vt.Olustur(k);
        await Satis(db).HurdaAlis(Alis(k), k.Islem);   // şubeli SAHİP hurdayı alır
        var mudur = Subesiz(k, "MUDUR");

        using (var o = vt.Olustur(k))
        {
            // PAKET ŞUBE-KAPSAM-A § 4.2.2: OKUMA yolu görünüm şubesiyle çözülür —
            // şubesiz tüm-şube rolü BİRLEŞİK görür (tek şubede = o şube), kırılım
            // o şubeyi adıyla taşır. YAZMA yolu (aşağıda) OturumSubesiCoz'da kalır.
            var b = await Stok(o).Bakiye(mudur);
            Assert.Equal("TUM_SUBELER", b.Kapsam);
            Assert.Equal(k.SubeId, b.Subeler!.Single().SubeId);
            Assert.Equal(9.16m, b.Satirlar.Single().HasGram);
            var d = await Stok(o).Defter(mudur);
            Assert.Equal(1, d.ToplamAdet);
        }

        var s = await Satis(db).HurdaSatis(Cikis(k, 1m), mudur);
        Assert.Equal(8.16m, s.KalanHurdaHasGram);
        using var kontrol = vt.OlusturSistem();
        var satir = kontrol.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == "HURDA_SATIS");
        Assert.Equal(k.SubeId, satir.SubeId);
    }

    [Fact]
    public async Task ZO_subesiz_mudur_IKI_aktif_subede_400_kalir_fail_closed()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZO-iki-sube");
        TestKurulum.SubeEkle(kurulum, k.TenantId);
        using var db = vt.Olustur(k);
        var mudur = Subesiz(k, "MUDUR");

        var cozulen = await MHurda.HurdaStokKapisi.OturumSubesiCoz(db, mudur);
        Assert.Null(cozulen.SubeId);
        // PAKET ŞUBE-KAPSAM-A (K-4): OKUMA artık 400 DEĞİL — birleşik bakiye döner.
        var b = await Stok(db).Bakiye(mudur);
        Assert.Equal("TUM_SUBELER", b.Kapsam);
        // YAZMA yolu fail-closed KALIR: hangi şubeden çıkılacağı bilinmiyor.
        await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).HurdaSatis(Cikis(k, 1m), mudur));
    }

    // Doğrulayıcı 27.09 (UX-S22 "aynı sorunun iki cevabı"): hurda defterinin GİRİŞİ
    // (hurda alış) da çıkışla AYNI şube çözümünü kullanır. Tek aktif şubede şubesiz
    // MÜDÜR'ün alışı o şubeye yazılır; iki aktif şubede 400 kalır (fail-closed).
    [Fact]
    public async Task ZO_subesiz_mudur_HURDA_ALISI_tek_subede_o_subeye_iki_subede_400()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "ZO-mudur-alis"));
        using (var db = vt.Olustur(k))
            await Satis(db).HurdaAlis(Alis(k), Subesiz(k, "MUDUR"));
        using (var kontrol = vt.OlusturSistem())
        {
            var satir = kontrol.Hareketler.AsNoTracking()
                .Single(h => h.TenantId == k.TenantId && h.Tur == "HURDA_ALIS");
            Assert.Equal(k.SubeId, satir.SubeId);
        }

        var k2 = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "ZO-mudur-alis-iki"));
        TestKurulum.SubeEkle(kurulum, k2.TenantId);
        using var db2 = vt.Olustur(k2);
        await Assert.ThrowsAsync<BadRequestException>(() => Satis(db2).HurdaAlis(Alis(k2), Subesiz(k2, "MUDUR")));
    }

    [Fact]
    public async Task ZO_pasif_sube_SAYILMAZ_tek_aktif_sube_secilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZO-pasif");
        var ikinci = TestKurulum.SubeEkle(kurulum, k.TenantId);
        kurulum.Subeler.Single(s => s.Id == ikinci).Aktif = false;
        kurulum.SaveChanges();
        using var db = vt.Olustur(k);

        var cozulen = await MHurda.HurdaStokKapisi.OturumSubesiCoz(db, Subesiz(k, "MUDUR"));
        Assert.Equal(k.SubeId, cozulen.SubeId);
    }

    [Fact]
    public async Task ZO_subesiz_TEZGAHTAR_tek_subede_bile_COZULMEZ_400()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZO-tezgahtar");
        using var db = vt.Olustur(k);
        var tezgahtar = Subesiz(k, "TEZGAHTAR");

        var cozulen = await MHurda.HurdaStokKapisi.OturumSubesiCoz(db, tezgahtar);
        Assert.Null(cozulen.SubeId);
        await Assert.ThrowsAsync<BadRequestException>(() => Stok(db).Bakiye(tezgahtar));
    }

    [Fact]
    public async Task ZO_subeli_oturum_DEGISMEZ_baska_sube_uydurulmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZO-subeli");
        var ikinci = TestKurulum.SubeEkle(kurulum, k.TenantId);
        using var db = vt.Olustur(k);
        var oturum = new AktifKullanici(k.KullaniciId, k.TenantId, ikinci, "MUDUR");

        var cozulen = await MHurda.HurdaStokKapisi.OturumSubesiCoz(db, oturum);
        Assert.Same(oturum, cozulen);
    }

    [Fact]
    public async Task ZO_baska_kiracinin_subesi_SAYILMAZ()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "ZO-kiraci-a");
        var b = TestKurulum.KiraciKur(kurulum, "ZO-kiraci-b");
        using var db = vt.Olustur(a);

        var cozulen = await MHurda.HurdaStokKapisi.OturumSubesiCoz(db, Subesiz(a, "MUDUR"));
        Assert.Equal(a.SubeId, cozulen.SubeId);
        Assert.NotEqual(b.SubeId, cozulen.SubeId);
    }
}
