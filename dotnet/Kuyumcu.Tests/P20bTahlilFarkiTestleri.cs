using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MHurda = Kuyumcu.Api.Modules.Hurda;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-20b (Y-10 kalan dilim · K-13) — TAHLİL / MUTABAKAT FARKI [gerçek MySQL]
//
// HER TEST BİR İŞ KURALINI YENİDEN ÜRETİR:
//   • İki has AYRI durur: HURDA_SATIS yalnız KABUL EDİLEN hası taşır, kalanı
//     TAHLIL_FARKI bacağına gider; ikisinin toplamı −(bizim kaydımız) eder.
//     Bu tutmazsa HAS-CARİ DENK GELMEZ (K-13'ün gerekçesi).
//   • TL bacağı KABUL EDİLEN hasla ödenir; fark bacağının TL'si DAİMA 0'dır.
//   • Fark ≠ 0 ise GEREKÇE ZORUNLUDUR ve gerekçesiz istek HİÇBİR ŞEY yazmaz
//     (iki bacak tek işlemde — yarım yazma imkânsız).
//   • Fark 0 ise ayrı bacak YAZILMAZ (P-20 davranışı birebir korunur).
//   • Fark sonradan DÜZELTİLİR: satırın üzerine yazılmaz, ikinci bir bacak
//     eklenir; bakiyeyi eksiye düşüren düzeltme fail-closed reddedilir.
//   • K-13/1: "fire" adıyla bir kayıt yolu AÇILMAMIŞTIR.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class P20bTahlilFarkiTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance,
            hurdaAyarlari: new MHurda.HurdaAyarlari());

    private static MHurda.TahlilFarkiServisi Duzeltme(KuyumcuDbContext db) =>
        new(db, new DenetimServisi(db, new HttpContextAccessor()));

    private static MSatis.HurdaAlisDto AlisDto(Kiraci k) => new()
    {
        CariId = k.CariId, AyarId = "K22", GramAgirlik = 10m,
        BirimFiyatTlHasGram = 2_800m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        Satici = TestKurulum.HurdaSatici(),
    };

    private const string Gerekce = "Rafineri tahlil raporu 2026/144: kabul edilen has düşük çıktı.";

    private static MSatis.HurdaSatisDto SatisDto(
        Kiraci k, decimal bizim = 5m, decimal? kabul = null, string? gerekce = null,
        decimal fiyat = 2_900m, decimal? tahsil = null) => new()
    {
        Maden = "ALTIN", HasGrami = bizim, AliciKabulHasGrami = kabul,
        TahlilFarkiGerekcesi = gerekce, BirimFiyatTlHasGram = fiyat,
        KasaId = k.KasaId, KurSnapshotId = k.KurId, TahsilEdilenTl = tahsil,
    };

    private static async Task<decimal> Bakiye(KuyumcuDbContext db, Kiraci k) =>
        await MHurda.HurdaStokKapisi.Bakiye(db, k.Islem, "ALTIN");

    // ═══ 1 — İKİ BACAK, TEK OLAY: toplam −(bizim kaydımız) eder ══════════════

    [Fact]
    public async Task Tahlil_farki_AYRI_bacak_yazar_ve_iki_bacagin_toplami_bizim_kaydimizdir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20B-fark"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);          // +9,16 has
        var kasaAlisSonrasi = TestKurulum.KasaBakiye(kurulum, k.KasaId);

        // Biz 5 has gönderdik, rafineri 4,8 has kabul etti → 0,2 has ALEYHTE.
        var sonuc = await Satis(db).HurdaSatis(
            SatisDto(k, bizim: 5m, kabul: 4.8m, gerekce: Gerekce), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var satis = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "HURDA_SATIS");
        var fark = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "TAHLIL_FARKI");

        Assert.Equal(-4.8m, satis.HasGrami);            // yalnız KABUL EDİLEN has
        Assert.Equal(13_920m, satis.TlTutari);          // 4,8 × 2.900 — kabul edilenle ödenir
        Assert.Equal(-0.2m, fark.HasGrami);             // fark AYRI bacakta
        Assert.Equal(0m, fark.TlTutari);                // fark PARA değil HAS'tır
        Assert.Equal(satis.Id, fark.TahlilKaynakHareketId);   // karşı bacak bağı
        Assert.Equal(satis.SubeId, fark.SubeId);
        Assert.Equal("ALTIN", fark.Maden);
        Assert.Contains("tahlil raporu", fark.Aciklama!.ToLowerInvariant());

        // DEĞİŞMEZ: iki bacağın toplamı bizim kaydımızdır (has-cari denk gelir).
        Assert.Equal(-5m, satis.HasGrami + fark.HasGrami);

        // Bakiye 9,16 − 5 = 4,16 (fark bacağı türetmeye DÂHİLDİR)
        using var okuma = vt.Olustur(k);
        Assert.Equal(4.16m, await Bakiye(okuma, k));

        // Kasaya yalnız KABUL EDİLEN hasın parası girer.
        Assert.Equal(kasaAlisSonrasi + 13_920m, TestKurulum.KasaBakiye(kontrol, k.KasaId));

        // UX-S27: sunucu yanıtı farkı ve yönünü SÖYLER.
        Assert.Equal(5m, sonuc.BizimKaydimizHasGram);
        Assert.Equal(4.8m, sonuc.AliciKabulHasGram);
        Assert.Equal(-0.2m, sonuc.TahlilFarkiHasGram);
        Assert.Equal(fark.Id, sonuc.TahlilFarkiHareketId);
    }

    // ═══ 2 — GEREKÇE ZORUNLU; gerekçesizse HİÇBİR ŞEY yazılmaz ═══════════════

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("kısa")]
    public async Task Fark_varken_gerekcesiz_istek_reddedilir_ve_HICBIR_SEY_yazilmaz(string? gerekce)
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20B-gerekce"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);
        var kasaOnce = TestKurulum.KasaBakiye(kurulum, k.KasaId);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).HurdaSatis(SatisDto(k, bizim: 5m, kabul: 4.8m, gerekce: gerekce), k.Islem));
        Assert.Contains("gerekçe", ex.Message.ToLowerInvariant());

        // ⚠ TEK İŞLEM: satış bacağı da yazılmamış olmalı (yarım yazma yok).
        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.Where(
            x => x.TenantId == k.TenantId && (x.Tur == "HURDA_SATIS" || x.Tur == "TAHLIL_FARKI")));
        Assert.Equal(kasaOnce, TestKurulum.KasaBakiye(kontrol, k.KasaId));
    }

    // ═══ 3 — FARK YOKSA BACAK YAZILMAZ (P-20 davranışı korunur) ══════════════

    [Fact]
    public async Task Fark_sifirsa_ayri_bacak_YAZILMAZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20B-farksiz"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);
        var sonuc = await Satis(db).HurdaSatis(SatisDto(k, bizim: 5m, kabul: 5m), k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.Where(x => x.TenantId == k.TenantId && x.Tur == "TAHLIL_FARKI"));
        var satis = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "HURDA_SATIS");
        Assert.Equal(-5m, satis.HasGrami);
        Assert.Equal(14_500m, satis.TlTutari);
        Assert.Equal(0m, sonuc.TahlilFarkiHasGram);
        Assert.Null(sonuc.TahlilFarkiHareketId);
    }

    /// <summary>Kabul alanı hiç gönderilmezse P-20'nin davranışı BİREBİR aynıdır.</summary>
    [Fact]
    public async Task Kabul_alani_bos_birakilirsa_kaydimiz_aynen_kabul_edilmis_sayilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20B-varsayilan"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);
        var sonuc = await Satis(db).HurdaSatis(SatisDto(k, bizim: 5m, kabul: null), k.Islem);

        Assert.Equal(5m, sonuc.AliciKabulHasGram);
        Assert.Equal(0m, sonuc.TahlilFarkiHasGram);
        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.Where(x => x.TenantId == k.TenantId && x.Tur == "TAHLIL_FARKI"));
    }

    // ═══ 4 — FARK LEHTE DE OLABİLİR ══════════════════════════════════════════

    [Fact]
    public async Task Fark_LEHTE_olabilir_ve_bakiye_yine_bizim_kaydimiz_kadar_duser()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20B-lehte"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);                 // 9,16
        await Satis(db).HurdaSatis(
            SatisDto(k, bizim: 5m, kabul: 5.1m, gerekce: Gerekce), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var fark = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "TAHLIL_FARKI");
        Assert.Equal(0.1m, fark.HasGrami);                              // LEHTE (+)

        using var okuma = vt.Olustur(k);
        Assert.Equal(4.16m, await Bakiye(okuma, k));                    // 9,16 − 5
    }

    // ═══ 5 — ÇIKIŞ KAPISI BİZİM KAYDIMIZA BAKAR ══════════════════════════════

    [Fact]
    public async Task Cikis_kapisi_BIZIM_KAYDIMIZA_bakar_kabul_edilene_degil()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20B-kapi"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);                 // 9,16

        // Bizim kaydımız 20 has (elde yok), kabul 9 has. Kapı BİZİM kaydımıza
        // bakmasaydı, 9 kabul gördüğü için istek geçer ve defter eksiye düşerdi.
        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).HurdaSatis(
                SatisDto(k, bizim: 20m, kabul: 9m, gerekce: Gerekce), k.Islem));
        Assert.Contains("yeterli hurda yok", ex.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.Where(x => x.TenantId == k.TenantId && x.Tur == "TAHLIL_FARKI"));
    }

    [Fact]
    public async Task Kabul_edilen_has_sifir_olamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20B-sifirkabul"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).HurdaSatis(
                SatisDto(k, bizim: 5m, kabul: 0m, gerekce: Gerekce), k.Islem));
        Assert.Contains("sıfırdan büyük", ex.Message);
    }

    // ═══ 6 — DÜZELTME: ÜZERİNE YAZMAZ, İKİNCİ BACAK EKLER ════════════════════

    [Fact]
    public async Task Duzeltme_satirin_uzerine_yazmaz_IKINCI_bacak_ekler()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20B-duzeltme"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);
        var s = await Satis(db).HurdaSatis(
            SatisDto(k, bizim: 5m, kabul: 4.8m, gerekce: Gerekce), k.Islem);
        var ilkFarkId = s.TahlilFarkiHareketId!;

        using var db2 = vt.Olustur(k);
        var sonuc = await Duzeltme(db2).Duzelt(s.Id, new MHurda.TahlilDuzeltmeDto
        {
            EkFarkHasGrami = -0.05m,
            Gerekce = "İkinci tahlil raporu geldi: eksilme 0,25 has olarak kesinleşti.",
        }, k.Islem);

        Assert.Equal(-0.05m, sonuc.EkFarkHasGram);
        Assert.Equal(-0.25m, sonuc.ToplamTahlilFarkiHasGram);
        Assert.Equal(4.11m, sonuc.KalanHurdaHasGram);            // 9,16 − 5,05

        using var kontrol = vt.OlusturSistem();
        // İLK bacak DEĞİŞMEDİ (append-only defter).
        var ilk = kontrol.Hareketler.AsNoTracking().Single(x => x.Id == ilkFarkId);
        Assert.Equal(-0.2m, ilk.HasGrami);
        // İkinci bacak aynı satışa bağlı.
        var bacaklar = kontrol.Hareketler.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId && x.Tur == "TAHLIL_FARKI").ToList();
        Assert.Equal(2, bacaklar.Count);
        Assert.All(bacaklar, b => Assert.Equal(s.Id, b.TahlilKaynakHareketId));
        // Satış bacağı ve TL tarafı DOKUNULMADI.
        var satis = kontrol.Hareketler.AsNoTracking().Single(x => x.Id == s.Id);
        Assert.Equal(-4.8m, satis.HasGrami);
        Assert.Equal(13_920m, satis.TlTutari);
        // Denetim izi YAZILDI (DoD sözleşmesi).
        Assert.Contains(kontrol.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikTuru == MHurda.TahlilFarkiServisi.Varlik),
            d => d.VarlikId == sonuc.DuzeltmeHareketId);
    }

    [Fact]
    public async Task Duzeltmede_gerekce_zorunludur_ve_sifir_ek_fark_reddedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20B-dz-red"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);
        var s = await Satis(db).HurdaSatis(SatisDto(k, bizim: 5m), k.Islem);

        using var db2 = vt.Olustur(k);
        var kisa = await Assert.ThrowsAsync<BadRequestException>(
            () => Duzeltme(db2).Duzelt(s.Id, new MHurda.TahlilDuzeltmeDto
            { EkFarkHasGrami = -0.1m, Gerekce = "olsun" }, k.Islem));
        Assert.Contains("gerekçe", kisa.Message.ToLowerInvariant());

        using var db3 = vt.Olustur(k);
        var sifir = await Assert.ThrowsAsync<BadRequestException>(
            () => Duzeltme(db3).Duzelt(s.Id, new MHurda.TahlilDuzeltmeDto
            { EkFarkHasGrami = 0m, Gerekce = "Tahlil raporu yeniden okundu, fark yok." }, k.Islem));
        Assert.Contains("sıfır olamaz", sifir.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.Where(x => x.TenantId == k.TenantId && x.Tur == "TAHLIL_FARKI"));
    }

    [Fact]
    public async Task Duzeltme_hurda_bakiyesini_EKSIYE_dusuremez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20B-dz-eksi"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);                 // 9,16
        var s = await Satis(db).HurdaSatis(SatisDto(k, bizim: 5m), k.Islem);

        using var db2 = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Duzeltme(db2).Duzelt(s.Id, new MHurda.TahlilDuzeltmeDto
            {
                EkFarkHasGrami = -99m,
                Gerekce = "Hatalı bir düzeltme denemesi: elde olmayan has eksiltilemez.",
            }, k.Islem));
        Assert.Contains("yeterli hurda yok", ex.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.Where(x => x.TenantId == k.TenantId && x.Tur == "TAHLIL_FARKI"));
    }

    [Fact]
    public async Task Baska_kiracinin_hurda_satisina_fark_yazilamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var a = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20B-kiraciA"));
        var b = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20B-kiraciB"));

        using var dbA = vt.Olustur(a);
        await Satis(dbA).HurdaAlis(AlisDto(a), a.Islem);
        var s = await Satis(dbA).HurdaSatis(SatisDto(a, bizim: 5m), a.Islem);

        using var dbB = vt.Olustur(b);
        await Assert.ThrowsAsync<NotFoundException>(
            () => Duzeltme(dbB).Duzelt(s.Id, new MHurda.TahlilDuzeltmeDto
            {
                EkFarkHasGrami = -0.1m,
                Gerekce = "Başka kiracının belgesine fark yazma denemesi.",
            }, b.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.Where(x => x.Tur == "TAHLIL_FARKI" && x.TenantId == b.TenantId));
    }

    [Fact]
    public async Task Tahlil_farki_yalnizca_HURDA_SATIS_satirina_baglanir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20B-yanlis-kaynak"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);
        using var kontrol = vt.OlusturSistem();
        var alis = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "HURDA_ALIS");

        using var db2 = vt.Olustur(k);
        // Hurda ALIŞ satırına fark yazılmak istenirse uç 404 verir (tür süzgeci).
        await Assert.ThrowsAsync<NotFoundException>(
            () => Duzeltme(db2).Duzelt(alis.Id, new MHurda.TahlilDuzeltmeDto
            {
                EkFarkHasGrami = -0.1m,
                Gerekce = "Alış satırına fark yazma denemesi — kabul edilmemeli.",
            }, k.Islem));
    }

    // ═══ 7 — SÖZLEŞME VE K-13/1 (saf testler) ════════════════════════════════

    /// <summary>
    /// K-13/1'in makine kontrolü: 1.0'da "fire" adıyla bir HAREKET TÜRÜ — yani
    /// bir kayıt yolu — YOKTUR. Tahlil farkı AYRI kavramdır ve öyle adlanır.
    /// </summary>
    [Fact]
    public void Fire_adiyla_bir_hareket_turu_YOKTUR_fark_FIRE_diye_adlandirilmaz()
    {
        Assert.DoesNotContain(Sabitler.HareketTurleri,
            t => t.Contains("FIRE", StringComparison.Ordinal));
        Assert.Equal("TAHLIL_FARKI", TahlilFarkiFabrikasi.Tur);
        Assert.Contains("TAHLIL_FARKI", Sabitler.HareketTurleri);
        // Reddetme metni K-13 gerekçesini söyler (UX-S30).
        Assert.Contains("imalat kapsam dışıdır", MHurda.HurdaStokKapisi.FireKapaliMesaji);
    }

    /// <summary>
    /// UX-S25: bu kayıt türü için "sunucuda uç var, ekranda düğme yok"un TERSİ
    /// de olmaz — politika haritası GERÇEKTEN VAR OLAN düzeltme ucunu gösterir.
    /// </summary>
    [Fact]
    public void Tahlil_farkinin_duzeltme_yolu_politika_haritasinda_YAZILIDIR()
    {
        var p = Kuyumcu.Api.Modules.Meta.DuzeltmePolitikasi.HareketTurleri["TAHLIL_FARKI"];
        Assert.Equal("DEFTER", p.GerekceAnahtari);
        Assert.Equal("api/v1/islemler/hurda-satis/{hareketId}/tahlil-duzeltme", p.HedefUc);
    }

    /// <summary>
    /// UX-S21/S22 — bakiye kartının ARİTMETİĞİ TUTAR: giren − çıkan + fark =
    /// bakiye. Fark "çıkan"ın içine saklansaydı ekran "5 çıktı" derken defterde
    /// 4,8 satış + 0,2 fark olurdu; iki ekran aynı soruya iki cevap verirdi.
    /// </summary>
    [Fact]
    public async Task Bakiye_kartinda_giren_eksi_cikan_arti_fark_BAKIYEYI_verir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "P20B-kart"));
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(AlisDto(k), k.Islem);
        await Satis(db).HurdaSatis(
            SatisDto(k, bizim: 5m, kabul: 4.8m, gerekce: Gerekce), k.Islem);

        using var okuma = vt.Olustur(k);
        var satir = (await MHurda.HurdaStokKapisi.Bakiyeler(okuma, k.Islem)).Single();

        Assert.Equal(9.16m, satir.GirenHas);
        Assert.Equal(4.8m, satir.CikanHas);          // yalnız SATIŞ bacağı
        Assert.Equal(-0.2m, satir.TahlilFarkiHas);   // fark AYRI kalem
        Assert.Equal(4.16m, satir.HasGram);
        Assert.Equal(satir.HasGram, satir.GirenHas - satir.CikanHas + satir.TahlilFarkiHas);
    }

    /// <summary>Fark bacağı hurda bakiyesi türetmesinin İÇİNDEDİR (tek kaynak).</summary>
    [Fact]
    public void Tahlil_farki_hurda_turetmesine_DAHILDIR()
        => Assert.Contains("TAHLIL_FARKI", MHurda.HurdaStokKapisi.HurdaTurleri);
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-20b — UÇ, YETKİ VE SÖZLEŞME KAPISI [HTTP, gerçek MySQL]
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class P20bTahlilFarkiHttpTestleri(HttpFikstur f)
{
    private static object AlisGovde(Kiraci k) => new
    {
        cariId = k.CariId, ayarId = "K22", gramAgirlik = 10m,
        birimFiyatTlHasGram = 2_800m, kasaId = k.KasaId, kurSnapshotId = k.KurId,
        satici = new { saticiTipi = "NIHAI_TUKETICI", adSoyad = "Ayşe Yılmaz", tckn = "10000000146", adres = "Kadıköy" },
    };

    [Fact]
    public async Task Uc_farki_yazar_gerekcesizi_reddeder_ve_TEZGAHTARa_KAPALIDIR()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = IslemBantFikstur.ReferansKur(db, TestKurulum.KiraciKur(db, "P20B-http"));

        var alis = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/islemler/hurda-alis", AlisGovde(k));
        Assert.Equal(System.Net.HttpStatusCode.OK, alis.StatusCode);

        // Gerekçesiz fark: 400 (sessizce yutulmaz).
        var gerekcesiz = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/islemler/hurda-satis", new
        {
            maden = "ALTIN", hasGrami = 5m, aliciKabulHasGrami = 4.8m,
            birimFiyatTlHasGram = 2_900m, kasaId = k.KasaId, kurSnapshotId = k.KurId, odemeTuru = "HAVALE",
        });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, gerekcesiz.StatusCode);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/islemler/hurda-satis", new
        {
            maden = "ALTIN", hasGrami = 5m, aliciKabulHasGrami = 4.8m,
            tahlilFarkiGerekcesi = "Rafineri tahlil raporu 2026/144 ile mutabakat farkı.",
            birimFiyatTlHasGram = 2_900m, kasaId = k.KasaId, kurSnapshotId = k.KurId, odemeTuru = "HAVALE",
        });
        Assert.Equal(System.Net.HttpStatusCode.OK, cevap.StatusCode);
        var g = await cevap.Json();
        Assert.Equal(-0.2m, g.GetProperty("tahlilFarkiHasGram").GetDecimal());
        Assert.Equal(13_920m, g.GetProperty("toplamTl").GetDecimal());
        Assert.Equal(4.16m, g.GetProperty("kalanHurdaHasGram").GetDecimal());
        var satisId = g.GetProperty("id").GetString()!;

        // Defter ucu farkı SUNUCUDA toplar (ekran kendi türetmez).
        var defter = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/hurda/defter?limit=100");
        var d = await defter.Json();
        var satisSatiri = d.GetProperty("satirlar").EnumerateArray()
            .Single(x => x.GetProperty("id").GetString() == satisId);
        Assert.Equal(-0.2m, satisSatiri.GetProperty("tahlilFarkiHasGram").GetDecimal());

        // Düzeltme ucu TEZGÂHTAR'a KAPALI.
        var yasak = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson(
            $"/api/v1/islemler/hurda-satis/{satisId}/tahlil-duzeltme",
            new { ekFarkHasGrami = -0.05m, gerekce = "Tezgâhtar düzeltme denemesi — kapalı olmalı." });
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, yasak.StatusCode);

        // Düzeltme: ikinci bacak eklenir.
        var duzelt = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/islemler/hurda-satis/{satisId}/tahlil-duzeltme",
            new { ekFarkHasGrami = -0.05m, gerekce = "İkinci tahlil raporu: eksilme 0,25 has." });
        Assert.Equal(System.Net.HttpStatusCode.OK, duzelt.StatusCode);
        var dz = await duzelt.Json();
        Assert.Equal(-0.25m, dz.GetProperty("toplamTahlilFarkiHasGram").GetDecimal());
        Assert.Equal(4.11m, dz.GetProperty("kalanHurdaHasGram").GetDecimal());

        // Olmayan satış: 404.
        var yok = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            "/api/v1/islemler/hurda-satis/01BOYLEBIRKAYITYOK000000000/tahlil-duzeltme",
            new { ekFarkHasGrami = -0.05m, gerekce = "Olmayan belgeye düzeltme denemesi." });
        Assert.Equal(System.Net.HttpStatusCode.NotFound, yok.StatusCode);
    }
}
