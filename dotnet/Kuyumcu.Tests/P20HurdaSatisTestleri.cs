using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MHurda = Kuyumcu.Api.Modules.Hurda;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-20 (Y-10) — HURDA SATIŞI / RAMAT ÇIKIŞI  [gerçek MySQL]
//
// HER TEST BİR İŞ KURALINI YENİDEN ÜRETİR:
//   • HURDA_SATIS has bacağını EKSİLTİR ve TL karşı bacağını YAZAR.
//   • Elde olmayan hurda satılamaz (fail-closed çıkış kapısı) — bakiye
//     defterden TÜRETİLİR, ayrı bir kolon yoktur.
//   • Fire / ramat farkı K-13 gelmeden YAZILMAZ; dolu gelirse sessizce
//     yutulmaz, gerekçesiyle reddedilir.
//   • Madensiz / geçersiz madenli hurda yazılamaz (MadenKapisi · K20-1).
//   • Tahsil edilmeyen tutarın gideceği bir defter OLMALIDIR (cari zorunlu).
//   • Bakiye kiracıya ve şubeye kilitlidir: başka kiracının hurdası satılamaz.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class P20HurdaSatisTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db, bool fireAcik = false) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance,
            hurdaAyarlari: new MHurda.HurdaAyarlari { FireKaydiAcik = fireAcik });

    /// <summary>10 g K22 = 9,16 has girer; tutar 459 s. VUK haddinin altındadır.</summary>
    private static MSatis.HurdaAlisDto AlisDto(Kiraci k, decimal gram = 10m, decimal fiyat = 2_800m) => new()
    {
        CariId = k.CariId, AyarId = "K22", GramAgirlik = gram,
        BirimFiyatTlHasGram = fiyat, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        Satici = TestKurulum.HurdaSatici(),
    };

    private static MSatis.HurdaSatisDto SatisDto(
        Kiraci k, decimal has = 5m, decimal fiyat = 2_900m,
        string maden = "ALTIN", decimal? tahsil = null,
        string? cariId = null, decimal? fire = null) => new()
    {
        Maden = maden, HasGrami = has, BirimFiyatTlHasGram = fiyat,
        KasaId = k.KasaId, KurSnapshotId = k.KurId,
        TahsilEdilenTl = tahsil, CariId = cariId, FireGrami = fire,
    };

    private static async Task<decimal> Bakiye(KuyumcuDbContext db, Kiraci k) =>
        await MHurda.HurdaStokKapisi.Bakiye(db, k.Islem, "ALTIN");

    // ═══════════ 1 — HAS EKSİLİR, TL KARŞI BACAĞI YAZILIR ════════════════════

    [Fact]
    public async Task HurdaSatisi_has_bacagini_EKSILTIR_ve_TL_bacagini_yazar()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20-cikis"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);          // +9,16 has
        var kasaAlisSonrasi = TestKurulum.KasaBakiye(kurulum, k.KasaId);

        await Satis(db).HurdaSatis(SatisDto(k, has: 5m, fiyat: 2_900m), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var h = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "HURDA_SATIS");

        Assert.Equal(-5m, h.HasGrami);                  // HAS BACAĞI EKSİLİR
        Assert.Equal(14_500m, h.TlTutari);              // 5 × 2.900 = +14.500 TL
        Assert.Equal("ALTIN", h.Maden);                 // K20-1
        Assert.Equal(k.SubeId, h.SubeId);
        Assert.Equal(14_500m, h.NakitTutar);            // tamamı nakit tahsil
        Assert.Equal(0m, h.VeresiyeTutar);

        // Kasa TL bacağı gerçekten ARTTI (para defterde de göründü)
        Assert.Equal(kasaAlisSonrasi + 14_500m, TestKurulum.KasaBakiye(kontrol, k.KasaId));

        // Bakiye defterden türetilir: 9,16 − 5 = 4,16
        using var okuma = vt.Olustur(k);
        Assert.Equal(4.16m, await Bakiye(okuma, k));
    }

    // ═══════════ 2 — ELDE OLMAYAN HURDA SATILAMAZ (fail-closed) ══════════════

    [Fact]
    public async Task Elde_olmayan_hurda_satilamaz_ve_hicbir_sey_yazilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20-yetersiz"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);          // yalnız 9,16 has
        var kasaOnce = TestKurulum.KasaBakiye(kurulum, k.KasaId);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).HurdaSatis(SatisDto(k, has: 20m), k.Islem));
        Assert.Contains("yeterli hurda yok", ex.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.Where(x => x.TenantId == k.TenantId && x.Tur == "HURDA_SATIS"));
        Assert.Equal(kasaOnce, TestKurulum.KasaBakiye(kontrol, k.KasaId));
    }

    [Fact]
    public async Task Hic_hurda_alinmamis_subede_satis_reddedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20-bos"));
        using var db = vt.Olustur(k);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).HurdaSatis(SatisDto(k, has: 1m), k.Islem));
        Assert.Contains("yeterli hurda yok", ex.Message);
    }

    /// <summary>
    /// Bakiye KÜMÜLATİFTİR: ilk çıkış ikincinin tavanını düşürür. Aksi hâlde
    /// aynı hurda iki kez satılır ve defter eksiye düşerdi.
    /// </summary>
    [Fact]
    public async Task Ikinci_cikis_ilk_cikistan_kalani_asamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20-kumulatif"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);          // 9,16 has
        await Satis(db).HurdaSatis(SatisDto(k, has: 6m), k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).HurdaSatis(SatisDto(k, has: 4m), k.Islem));
        Assert.Contains("yeterli hurda yok", ex.Message);

        using var okuma = vt.Olustur(k);
        Assert.Equal(3.16m, await Bakiye(okuma, k));
    }

    // ═══════════ 3 — K-13: FİRE / RAMAT FARKI YAZILMAZ ═══════════════════════

    [Fact]
    public async Task Fire_grami_K13_kapaliyken_gerekcesiyle_reddedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20-fire"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).HurdaSatis(SatisDto(k, has: 5m, fire: 0.4m), k.Islem));
        Assert.Contains("fire", ex.Message.ToLowerInvariant());
        Assert.Contains("K-13", ex.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.Where(x => x.TenantId == k.TenantId && x.Tur == "HURDA_SATIS"));
    }

    /// <summary>
    /// Kural KODA GÖMÜLÜ DEĞİLDİR: kurucu kararı gelince yapılandırma anahtarı
    /// açılır ve istek reddedilmez. (Fire HESABI yine kurulmaz — bu sürüm onu
    /// yazmaz; kapı yalnız isteği geçirir.)
    /// </summary>
    [Fact]
    public async Task Fire_anahtari_acilinca_istek_reddedilmez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20-fire-acik"));
        using var db = vt.Olustur(k);

        await Satis(db, fireAcik: true).HurdaAlis(AlisDto(k), k.Islem);
        await Satis(db, fireAcik: true).HurdaSatis(SatisDto(k, has: 5m, fire: 0.4m), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var h = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "HURDA_SATIS");
        Assert.Equal(-5m, h.HasGrami);
    }

    // ═══════════ 4 — MADEN KAPISI (K20-1) ═══════════════════════════════════

    [Fact]
    public async Task Gecersiz_maden_ile_hurda_satisi_reddedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20-maden"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).HurdaSatis(SatisDto(k, has: 1m, maden: "TENEKE"), k.Islem));
        Assert.Contains("Geçersiz maden", ex.Message);

        var bos = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).HurdaSatis(SatisDto(k, has: 1m, maden: ""), k.Islem));
        Assert.Contains("Geçersiz maden", bos.Message);
    }

    /// <summary>
    /// Altın hurdası GÜMÜŞ olarak çıkarılamaz: farklı madenlerin has gramları
    /// toplanmaz, gümüş bakiyesi 0'dır.
    /// </summary>
    [Fact]
    public async Task Baska_madenin_bakiyesi_altin_hurdasini_karsilamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20-gumus"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);          // ALTIN 9,16 has

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).HurdaSatis(SatisDto(k, has: 1m, maden: "GUMUS"), k.Islem));
        Assert.Contains("yeterli hurda yok", ex.Message);
    }

    // ═══════════ 5 — TAHSİLAT VE CARİ ═══════════════════════════════════════

    [Fact]
    public async Task Tahsil_edilmeyen_tutar_icin_ramatci_carisi_zorunludur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20-carisiz"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).HurdaSatis(SatisDto(k, has: 5m, tahsil: 1_000m), k.Islem));
        Assert.Contains("cari", ex.Message.ToLowerInvariant());

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.Where(x => x.TenantId == k.TenantId && x.Tur == "HURDA_SATIS"));
    }

    [Fact]
    public async Task Kismi_tahsilatta_kalan_cariye_ALACAK_yazilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20-kismi"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);
        var kasaOnce = TestKurulum.KasaBakiye(kurulum, k.KasaId);
        var cariOnce = (await kurulum.CariBakiyeler.AsNoTracking()
            .FirstOrDefaultAsync(b => b.TenantId == k.TenantId && b.CariId == k.CariId))?.TlBakiye ?? 0m;

        // 5 has × 2.900 = 14.500 TL; yalnız 1.000 TL nakit tahsil edildi.
        // ⚠ Tutar bilinçli küçük: 459 s. VUK kümülatifi hurda ALIŞININ nakdini
        //   (25.648 TL) de sayar; daha büyük bir tahsilat 30.000 TL haddini aşar
        //   ve bu test asıl sınadığı şeyi (cari alacak bacağı) sınayamazdı.
        await Satis(db).HurdaSatis(
            SatisDto(k, has: 5m, tahsil: 1_000m, cariId: k.CariId), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var h = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "HURDA_SATIS");

        Assert.Equal(1_000m, h.NakitTutar);
        Assert.Equal(13_500m, h.VeresiyeTutar);        // kalan alacak
        Assert.Equal("KARMA", h.OdemeTuru);
        Assert.Equal(kasaOnce + 1_000m, TestKurulum.KasaBakiye(kontrol, k.KasaId));

        var cariSonra = (await kontrol.CariBakiyeler.AsNoTracking()
            .SingleAsync(b => b.TenantId == k.TenantId && b.CariId == k.CariId)).TlBakiye;
        Assert.Equal(cariOnce + 13_500m, cariSonra);   // ALACAK yönü (+)
    }

    // ═══════════ 6 — KİRACI / ŞUBE İZOLASYONU ═══════════════════════════════

    [Fact]
    public async Task Baska_kiracinin_hurdasi_bu_kiracida_satilamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var a = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20-izole-A"));
        var b = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20-izole-B"));

        using var dbA = vt.Olustur(a);
        await Satis(dbA).HurdaAlis(AlisDto(a), a.Islem);         // hurda YALNIZ A'da

        using var dbB = vt.Olustur(b);
        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(dbB).HurdaSatis(SatisDto(b, has: 1m), b.Islem));
        Assert.Contains("yeterli hurda yok", ex.Message);

        using var okuma = vt.Olustur(a);
        Assert.Equal(9.16m, await Bakiye(okuma, a));             // A'nınki duruyor
    }

    // ═══════════ 7 — SIFIR / NEGATİF GİRDİ ══════════════════════════════════

    [Fact]
    public async Task Sifir_has_gram_ile_cikis_reddedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20-sifir"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).HurdaSatis(SatisDto(k, has: 0m), k.Islem));
        Assert.Contains("sıfırdan büyük", ex.Message);
    }

    // ═══════════ 8 — DÜZELTME POLİTİKASI (UX-S25) ═══════════════════════════

    /// <summary>
    /// "Sunucuda uç var, ekranda düğme yok"un TERSİ: politika haritası
    /// HURDA_SATIS için OLMAYAN bir storno ucu VAAT ETMEZ.
    /// </summary>
    [Fact]
    public void HurdaSatis_duzeltme_politikasi_storno_vaat_etmez()
    {
        var s = Kuyumcu.Api.Modules.Meta.DuzeltmePolitikasi.HareketTurleri["HURDA_SATIS"];
        Assert.Equal("STORNO_YOK", s.GerekceAnahtari);
        Assert.DoesNotContain("HURDA_SATIS",
            Kuyumcu.Api.Modules.Satis.StornoServisi.StornolanabilirTurler);
    }

    /// <summary>
    /// MASAK paritesi: hurda ALIŞI kapıya tabiyse, hurda SATIŞI da tabidir —
    /// aksi hâlde eşik üstü çıkış kimlik tespitinden kaçış yolu olurdu.
    /// </summary>
    [Fact]
    public void HurdaSatis_MASAK_kapisina_tabidir()
    {
        var alan = typeof(Kuyumcu.Api.Modules.Masak.MasakKapisi)
            .GetField("KapiyaTabiTurler",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var turler = (string[])alan.GetValue(null)!;
        Assert.Contains("HURDA_SATIS", turler);
        Assert.Contains("HURDA_ALIS", turler);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-20 — UÇ VE YETKİ KAPISI [HTTP, gerçek MySQL]
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class P20HurdaHttpTestleri(HttpFikstur f)
{
    private static object AlisGovde(Kiraci k) => new
    {
        cariId = k.CariId, ayarId = "K22", gramAgirlik = 10m,
        birimFiyatTlHasGram = 2_800m, kasaId = k.KasaId, kurSnapshotId = k.KurId,
        satici = new { saticiTipi = "NIHAI_TUKETICI", adSoyad = "Ayşe Yılmaz", tckn = "10000000146", adres = "Kadıköy" },
    };

    private static object SatisGovde(Kiraci k, decimal has = 5m) => new
    {
        maden = "ALTIN", hasGrami = has, birimFiyatTlHasGram = 2_900m,
        kasaId = k.KasaId, kurSnapshotId = k.KurId, odemeTuru = "HAVALE",
    };

    [Fact]
    public async Task Uc_calisir_bakiye_dusurur_ve_TEZGAHTARa_KAPALIDIR()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = IslemBantFikstur.ReferansKur(db, TestKurulum.KiraciKur(db, "P20-http"));

        var alis = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/islemler/hurda-alis", AlisGovde(k));
        Assert.Equal(System.Net.HttpStatusCode.OK, alis.StatusCode);

        // TEZGÂHTAR hurda çıkışı YAZAMAZ (alışla aynı rol kapısı).
        var yasak = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson("/api/v1/islemler/hurda-satis", SatisGovde(k));
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, yasak.StatusCode);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/islemler/hurda-satis", SatisGovde(k));
        Assert.Equal(System.Net.HttpStatusCode.OK, cevap.StatusCode);
        var g = await cevap.Json();
        Assert.Equal(4.16m, g.GetProperty("kalanHurdaHasGram").GetDecimal());
        Assert.Equal(14_500m, g.GetProperty("toplamTl").GetDecimal());

        // Okuma ucu AYNI rakamı söyler (tek türetme yeri — UX-S22).
        var bakiye = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/hurda/bakiye");
        Assert.Equal(System.Net.HttpStatusCode.OK, bakiye.StatusCode);
        var b = await bakiye.Json();
        var satir = b.GetProperty("satirlar").EnumerateArray().Single();
        Assert.Equal(4.16m, satir.GetProperty("hasGram").GetDecimal());
        Assert.False(b.GetProperty("fireKaydiAcik").GetBoolean());
        Assert.Contains("K-13", b.GetProperty("fireNotu").GetString()!);

        // Elde olmayan hurda: 400 (fail-closed), defter bozulmaz.
        var fazla = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/islemler/hurda-satis", SatisGovde(k, 99m));
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, fazla.StatusCode);
    }
}
