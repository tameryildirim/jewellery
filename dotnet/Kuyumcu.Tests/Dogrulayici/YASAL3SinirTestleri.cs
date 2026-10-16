using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using MMasak = Kuyumcu.Api.Modules.Masak;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-3 — HASIM DOĞRULAYICI (Aşkın + Mehmet) finansal sınır testleri.
// MASAK ölçüsü (hareketler.masak_olcu_tl) ve kümülatif (GunlukIslemToplam).
// Beklenen değerler ELLE yazılmıştır; üretim kodundan hesaplanmaz.
// K24 = 1000 milyem → has gram = gram (Kuyumcu.Core/Domain/Ayarlar.cs).
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class YASAL3SinirTestleri(TestVeritabani vt)
{
    private static MSatis.TakasDto Takas(Kiraci k, decimal eskiGram, decimal eskiFiyat, decimal yeniFiyat,
        MMasak.MasakBilgiDto? masak = null) => new()
    {
        CariId = k.CariId, EskiUrunAyarId = "K24", EskiUrunGram = eskiGram, EskiUrunBirimFiyatTlHasGram = eskiFiyat,
        YeniUrunId = k.UrunId, YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = yeniFiyat,
        KasaId = k.KasaId, OdemeTuru = "HAVALE", KurSnapshotId = k.KurId, Masak = masak,
        Satici = ParaDemoTestVerisi.TakasSaticisi,   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
    };

    private static MSatis.HurdaAlisDto Hurda(Kiraci k, decimal gram, decimal fiyat) => new()
    {
        CariId = k.CariId, AyarId = "K24", GramAgirlik = gram, BirimFiyatTlHasGram = fiyat,
        OdemeTuru = "VERESIYE", KasaId = null, KurSnapshotId = k.KurId,
        Satici = TestKurulum.HurdaSatici(SaticiTipi.EsnafMuaf),
    };

    private static MSatis.SatisOlusturDto Satis(Kiraci k, decimal tutar) => new()
    {
        CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = tutar,
        OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
    };

    private Hareket Satir(Kiraci k, string tur)
    {
        using var kontrol = vt.OlusturSistem();
        return kontrol.Hareketler.AsNoTracking().Single(h => h.TenantId == k.TenantId && h.Tur == tur);
    }

    // ── sıfır: fark 0 takasta defter 0, MASAK ölçüsü büyük bacak ──
    [Fact]
    public async Task Sifir_farkli_takas_defter_sifir_olcu_30000()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-DS0"));
        using var db = vt.Olustur(k);
        await Yasal3Yardim.Satis(db).Takas(Takas(k, 10m, 3_000m, 30_000m), k.Islem);
        var h = Satir(k, "TAKAS");
        Assert.Equal(0m, h.TlTutari);
        Assert.Equal(30_000m, h.MasakOlcuTl);
        Assert.Equal(30_000m, await MMasak.MasakKapisi.GunlukIslemToplam(db, k.TenantId, k.CariId, DateTime.UtcNow));
    }

    // ── eksi: biz öderiz (fark −10.000) → ölçü pozitif büyük bacak 60.000 ──
    [Fact]
    public async Task Eksi_farkli_takas_olcu_pozitif_buyuk_bacak()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-DSE"));
        using var db = vt.Olustur(k);
        await Yasal3Yardim.Satis(db).Takas(Takas(k, 20m, 3_000m, 50_000m), k.Islem);
        var h = Satir(k, "TAKAS");
        Assert.Equal(-10_000m, h.TlTutari);
        Assert.Equal(60_000m, h.MasakOlcuTl);
        Assert.Equal(60_000m, await MMasak.MasakKapisi.GunlukIslemToplam(db, k.TenantId, k.CariId, DateTime.UtcNow));
    }

    // ── eksi + NULL: ölçüsü silinmiş hurda satırı NET'in MUTLAK değeriyle sayılır ──
    [Fact]
    public async Task Null_olculu_eksi_hurda_satiri_mutlak_net_ile_sayilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-DSN"));
        using var db = vt.Olustur(k);
        Assert.Equal(0m, await MMasak.MasakKapisi.GunlukIslemToplam(db, k.TenantId, k.CariId, DateTime.UtcNow));   // boş gün
        await Yasal3Yardim.Satis(db).HurdaAlis(Hurda(k, 60m, 3_000m), k.Islem);
        using (var ham = vt.OlusturSistem())
            await ham.Database.ExecuteSqlRawAsync(
                "UPDATE hareketler SET masak_olcu_tl = NULL WHERE tenant_id = {0} AND tur = 'HURDA_ALIS'", k.TenantId);
        Assert.Equal(176_400m, await MMasak.MasakKapisi.GunlukIslemToplam(db, k.TenantId, k.CariId, DateTime.UtcNow));
    }

    // ── kuruş sınırı: brüt 198.999,99 + 1.000,01 = 200.000,00 → RED; 198.999,98 → GEÇER (MASAK-370) ──
    [Fact]
    public async Task Kurus_siniri_brut_200000_tam_esik_red()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-DSK1"), 50_000m);
        using var db = vt.Olustur(k);
        var s = Yasal3Yardim.Satis(db);
        // MASAK-370: bugünkü en düşük kimlik tetikleyicisi 200.000 (amaç beyanı, TEYİTSİZ); eski 185.000 dönemi.
        await s.HurdaAlis(Hurda(k, 4.011m, 49_613.56m)   /* ISLEM-BANT: has fiyatı ≤ 50.000 (mutlak); aynı brüt */, k.Islem);
        Assert.Equal(198_999.99m, Satir(k, "HURDA_ALIS").MasakOlcuTl);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => s.Satis(Satis(k, 1_000.01m), k.Islem));
        Assert.Contains("200.000,00", ex.Message);
    }

    [Fact]
    public async Task Kurus_siniri_brut_199999_99_gecer()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-DSK2"), 50_000m);
        using var db = vt.Olustur(k);
        var s = Yasal3Yardim.Satis(db);
        await s.HurdaAlis(Hurda(k, 3.985m, 49_937.26m)   /* ISLEM-BANT: has fiyatı ≤ 50.000 (mutlak); aynı brüt */, k.Islem);
        await s.Satis(Satis(k, 1_000.01m), k.Islem);   // kümülatif 199.999,99 → kimlik istenmez (MASAK-370)
        Assert.Equal(199_999.99m, await MMasak.MasakKapisi.GunlukIslemToplam(db, k.TenantId, k.CariId, DateTime.UtcNow));
    }

    // ── 0,005 yuvarlaması: 198.999,985 → AwayFromZero 198.999,99 (ToEven olsa ,98) → eşik 400 (MASAK-370: 200.000) ──
    [Fact]
    public async Task Yarim_kurus_AwayFromZero_esigi_tetikler()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-DSY"), 50_000m);
        using var db = vt.Olustur(k);
        var s = Yasal3Yardim.Satis(db);
        await s.HurdaAlis(Hurda(k, 5.62m, 35_409.25m)   /* ISLEM-BANT: has fiyatı ≤ 50.000 (mutlak); aynı brüt */, k.Islem);
        Assert.Equal(198_999.99m, Satir(k, "HURDA_ALIS").MasakOlcuTl);
        await Assert.ThrowsAsync<BadRequestException>(() => s.Satis(Satis(k, 1_000.01m), k.Islem));
    }

    // ── takasta yarım kuruş: yeni 1.000,005 → ölçü 1.000,01 ──
    [Fact]
    public async Task Takas_yarim_kurus_olcusu_AwayFromZero()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-DSTY"));
        using var db = vt.Olustur(k);
        await Yasal3Yardim.Satis(db).Takas(Takas(k, 0.001m, 3_000m, 1_000.005m), k.Islem);
        Assert.Equal(1_000.01m, Satir(k, "TAKAS").MasakOlcuTl);
    }

    // ── has/gram 4 hane: 12,3457 g × 3.000,03 = 37.037,470371 → 37.037,47 = pusula brütü ──
    [Fact]
    public async Task Dort_haneli_gram_olcu_pusula_brutune_esit()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-DSG"));
        using var db = vt.Olustur(k);
        await Yasal3Yardim.Satis(db).HurdaAlis(Hurda(k, 12.3457m, 3_000.03m), k.Islem);
        var h = Satir(k, "HURDA_ALIS");
        Assert.Equal(37_037.47m, h.MasakOlcuTl);
        using var kontrol = vt.OlusturSistem();
        var p = kontrol.GiderPusulalari.AsNoTracking().Single(g => g.TenantId == k.TenantId && g.HareketId == h.Id);
        Assert.Equal(37_037.47m, p.BrutTutar);
    }

    // ── çok büyük tutar: 999.999.999,99 takas (kimlikli) → ölçü birebir saklanır ──
    [Fact]
    public async Task Cok_buyuk_takas_olcusu_birebir_saklanir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-DSB"));
        using var db = vt.Olustur(k);
        await Yasal3Yardim.Satis(db).Takas(
            Takas(k, 1m, 3_000m, 999_999_999.99m, Yasal3Yardim.Kimlik(Yasal3Yardim.Tckn())), k.Islem);
        var h = Satir(k, "TAKAS");
        Assert.Equal(999_999_999.99m, h.MasakOlcuTl);
        Assert.Equal(999_996_999.99m, h.TlTutari);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-3 TUR 2 — HASIM DOĞRULAYICI (Aşkın + Mehmet, 2026-10-01) sınır testleri.
// (a) Tevsik red metni (YASAL-2 devri): had sınırı kuruş hassasiyetinde, işaret,
//     sıfır, çok büyük tutar; görünür metinde mevzuat kodu yok, tr-TR biçim.
// (b) Liste güncellemesi (6415 md.12/3): açık bakiye ölçüsü HER para biriminde
//     (TL, has, USD, EUR, GBP) ve en küçük birimde (0,01 · 0,000001 has) görev açar;
//     son gün = RG + 30 (ay/yıl dönümü).
// Beklenen değerler ELLE yazılmıştır; üretim kodundan hesaplanmaz.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class YASAL3Tur2TevsikMetinSinirTestleri
{
    private static readonly Kuyumcu.Api.Modules.Tevsik.TevsikEsikleri E = new() { Had = 30_000m, Etkin = true, IslemTutariKuraliEtkin = true };

    [Fact]
    public void Sifir_nakit_tevsik_gerekmez()
    {
        var s = Kuyumcu.Api.Modules.Tevsik.TevsikKurali.Degerlendir(0m, 0m, 0m, E);
        Assert.False(s.Gerekli);
        Assert.Null(s.Mesaj);
    }

    [Fact]
    public void Tam_had_30000_serbest_bir_kurus_fazlasi_red()
    {
        var tam = Kuyumcu.Api.Modules.Tevsik.TevsikKurali.Degerlendir(0m, 30_000.00m, 30_000.00m, E);
        Assert.False(tam.Gerekli);
        var fazla = Kuyumcu.Api.Modules.Tevsik.TevsikKurali.Degerlendir(0m, 30_000.01m, 30_000.01m, E);
        Assert.True(fazla.Gerekli);
        Assert.Equal(0.01m, fazla.Asim);
    }

    [Fact]
    public void Kumulatif_kurus_siniri_onceki_29999_99_arti_0_01_serbest_0_02_red()
    {
        Assert.False(Kuyumcu.Api.Modules.Tevsik.TevsikKurali.Degerlendir(29_999.99m, 0.01m, 0.01m, E).Gerekli);
        var r = Kuyumcu.Api.Modules.Tevsik.TevsikKurali.Degerlendir(29_999.99m, 0.02m, 0.02m, E);
        Assert.True(r.Gerekli);
        Assert.Equal(0.01m, r.Asim);
        Assert.Equal("Bu kişiyle bugün nakit toplamı 30.000,01 TL oluyor (önceki 29.999,99 TL, bu işlem 0,02 TL) ve 30.000,00 TL nakit sınırını 0,01 TL aşıyor; kalanı kartla ya da havaleyle ödenmeli.", r.Mesaj);
    }

    [Fact]
    public void Eksi_tediye_isareti_onemsiz_red_ve_metin_pozitif()
    {
        var r = Kuyumcu.Api.Modules.Tevsik.TevsikKurali.Degerlendir(0m, -45_000m, null, E);
        Assert.True(r.Gerekli);
        Assert.Equal(15_000m, r.Asim);
        Assert.Equal("45.000,00 TL nakit, 30.000,00 TL nakit sınırını 15.000,00 TL aşıyor; bu ödeme kartla ya da havaleyle yapılmalı.", r.Mesaj);
    }

    [Fact]
    public void Cok_buyuk_tutar_islem_tutari_kurali_tr_bicim_kod_yok()
    {
        var r = Kuyumcu.Api.Modules.Tevsik.TevsikKurali.Degerlendir(0m, 1_000m, 999_999_999.99m, E);
        Assert.True(r.Gerekli);
        Assert.Equal(999_969_999.99m, r.Asim);
        Assert.Equal("İşlemin toplamı 999.999.999,99 TL, 30.000,00 TL nakit sınırını aşıyor; 1.000,00 TL nakit de alınamaz, tamamı kartla ya da havaleyle ödenmeli.", r.Mesaj);
        foreach (var yasak in new[] { "VUK", "md.", "Tebliğ", "459", "257", "§" })
            Assert.DoesNotContain(yasak, r.Mesaj!);
    }
}

[Collection("veritabani")]
public sealed class YASAL3Tur2ListeBakiyeSinirTestleri(TestVeritabani vt)
{
    private async Task<(Kiraci k, string tc)> KurVeBakiye(string ad, string kolon, decimal deger)
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-DT2-" + ad);
        var tc = Yasal3Yardim.Tckn();
        await kurulum.Database.ExecuteSqlRawAsync("UPDATE cariler SET vergi_no = {0} WHERE id = {1}", tc, k.CariId);
        if (kolon.Length > 0)
            {
            var sql = kolon switch
            {
                "tl_bakiye" => "UPDATE cari_bakiyeler SET tl_bakiye = {0} WHERE cari_id = {1}",
                "has_gram_bakiye" => "UPDATE cari_bakiyeler SET has_gram_bakiye = {0} WHERE cari_id = {1}",
                "usd_bakiye" => "UPDATE cari_bakiyeler SET usd_bakiye = {0} WHERE cari_id = {1}",
                "eur_bakiye" => "UPDATE cari_bakiyeler SET eur_bakiye = {0} WHERE cari_id = {1}",
                "gbp_bakiye" => "UPDATE cari_bakiyeler SET gbp_bakiye = {0} WHERE cari_id = {1}",
                _ => throw new ArgumentException(kolon),
            };
            await kurulum.Database.ExecuteSqlRawAsync(sql, deger, k.CariId);
        }
        return (k, tc);
    }

    private async Task<YaptirimEslesmesi> Yukle(Kiraci k, string tc, DateOnly? rg)
    {
        using (var sdb = vt.OlusturSistem())
            await MMasak.YaptirimServisi.ListeGuncelle(sdb, TestKurulum.Denetim(sdb), new MMasak.YaptirimListesiDto
            {
                Kaynak = "Test listesi", KaynakUrl = "https://masak.hmb.gov.tr/bkk-ile-malvarliklari-dondurulanlar",
                ListeTarihi = YasalParametreServisi.IslemGunu(DateTime.UtcNow),
                Ekle = [new() { AdSoyad = "Sinir Kisi " + tc, KimlikNo = tc, RgTarihi = rg }],
            }, "askin-dogrulayici");
        using var kontrol = vt.OlusturSistem();
        return kontrol.YaptirimEslesmeleri.AsNoTracking().Single(e => e.TenantId == k.TenantId && e.CariId == k.CariId);
    }

    [Fact]
    public async Task Sifir_bakiye_gorev_yok_VARLIK_YOK()
    {
        var (k, tc) = await KurVeBakiye("S0", "", 0m);
        var e = await Yukle(k, tc, new DateOnly(2026, 9, 1));
        Assert.Equal(YaptirimEslesmeDurumu.VarlikYok, e.Durum);
        Assert.Null(e.BildirimSonGunu);
    }

    [Fact]
    public async Task Bir_kurus_TL_alacak_gorev_acar_son_gun_RG_arti_30_ay_donumu()
    {
        var (k, tc) = await KurVeBakiye("TL001", "tl_bakiye", 0.01m);
        var e = await Yukle(k, tc, new DateOnly(2026, 9, 1));
        Assert.Equal(YaptirimEslesmeDurumu.Acik, e.Durum);
        Assert.Equal(new DateOnly(2026, 10, 1), e.BildirimSonGunu);
    }

    [Fact]
    public async Task Eksi_TL_borcumuz_de_gorev_acar_yil_donumu()
    {
        var (k, tc) = await KurVeBakiye("TLEKSI", "tl_bakiye", -0.01m);
        var e = await Yukle(k, tc, new DateOnly(2026, 12, 15));
        Assert.Equal(YaptirimEslesmeDurumu.Acik, e.Durum);
        Assert.Equal(new DateOnly(2027, 1, 14), e.BildirimSonGunu);
    }

    [Fact]
    public async Task En_kucuk_has_birimi_0_000001_gram_gorev_acar()
    {
        var (k, tc) = await KurVeBakiye("HAS", "has_gram_bakiye", 0.000001m);
        var e = await Yukle(k, tc, new DateOnly(2026, 9, 1));
        Assert.Equal(YaptirimEslesmeDurumu.Acik, e.Durum);
    }

    [Theory]
    [InlineData("usd_bakiye")]
    [InlineData("eur_bakiye")]
    [InlineData("gbp_bakiye")]
    public async Task Doviz_bakiyesi_her_para_biriminde_gorev_acar(string kolon)
    {
        var (k, tc) = await KurVeBakiye("DVZ-" + kolon[..3].ToUpperInvariant(), kolon, 100m);
        var e = await Yukle(k, tc, new DateOnly(2026, 9, 1));
        Assert.Equal(YaptirimEslesmeDurumu.Acik, e.Durum);
        Assert.Equal(new DateOnly(2026, 10, 1), e.BildirimSonGunu);
    }

    [Fact]
    public async Task Cok_buyuk_bakiye_999999999_99_gorev_acar()
    {
        var (k, tc) = await KurVeBakiye("BUYUK", "tl_bakiye", 999_999_999.99m);
        var e = await Yukle(k, tc, null);
        Assert.Equal(YaptirimEslesmeDurumu.Acik, e.Durum);
        // RG tarihi yoksa liste tarihi (bugün) + 30 — tutucu yön.
        Assert.Equal(YasalParametreServisi.IslemGunu(DateTime.UtcNow).AddDays(30), e.BildirimSonGunu);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-3 tur 3 — HASIM DOĞRULAYICI sınırları: CariVarlikVar (açık varlık
// ölçüsü tek kaynak). Beklenenler ELLE; üretim kodundan hesaplanmaz.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class YASAL3Tur3VarlikSinirTestleri(TestVeritabani vt)
{
    private static Hareket Emanet(Kiraci k, string tur, decimal has, string maden) => new()
    {
        Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = tur, Tarih = DateTime.UtcNow,
        TlTutari = 0m, HasGrami = has, Maden = maden, ParaBirimi = "TRY", KurSnapshotId = k.KurId,
        CariId = k.CariId, KullaniciId = k.KullaniciId, Olusturuldu = DateTime.UtcNow,
    };

    [Fact]
    public void Cok_buyuk_TL_ve_bir_kurus_GBP_acik_sifir_acik_degil()
    {
        Assert.True(MMasak.YaptirimServisi.AcikBakiyeVar(new CariBakiye { CariId = "c", TenantId = "t", TlBakiye = 999_999_999.99m }));
        Assert.True(MMasak.YaptirimServisi.AcikBakiyeVar(new CariBakiye { CariId = "c", TenantId = "t", GbpBakiye = 0.01m }));
        Assert.True(MMasak.YaptirimServisi.AcikBakiyeVar(new CariBakiye { CariId = "c", TenantId = "t", UsdBakiye = -0.01m }));
        Assert.False(MMasak.YaptirimServisi.AcikBakiyeVar(new CariBakiye { CariId = "c", TenantId = "t", TlBakiye = 0.00m, GbpBakiye = -0.00m }));
    }

    [Fact]
    public async Task Tamamen_iade_edilmis_emanet_varlik_sayilmaz_kismi_iade_sayilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-DT3-EMANET");
        using (var db = vt.Olustur(k))
        {
            HareketFabrikasi.Ekle(db, OdemeIzi.Yok, Emanet(k, "EMANET_GIRIS", 5.0001m, Kuyumcu.Core.Domain.Maden.Altin));
            HareketFabrikasi.Ekle(db, OdemeIzi.Yok, Emanet(k, "EMANET_CIKIS", -5.0001m, Kuyumcu.Core.Domain.Maden.Altin));
            await db.SaveChangesAsync();
        }
        using (var s1 = vt.OlusturSistem())
            Assert.False(await MMasak.YaptirimServisi.CariVarlikVar(s1, k.TenantId, k.CariId));
        using (var db = vt.Olustur(k))
        {
            HareketFabrikasi.Ekle(db, OdemeIzi.Yok, Emanet(k, "EMANET_GIRIS", 0.0001m, Kuyumcu.Core.Domain.Maden.Altin));
            await db.SaveChangesAsync();
        }
        using (var s2 = vt.OlusturSistem())
            Assert.True(await MMasak.YaptirimServisi.CariVarlikVar(s2, k.TenantId, k.CariId));
    }

    [Fact]
    public async Task Bekleyen_siparis_varlik_sayilir_teslim_edilmis_sayilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-DT3-SIPARIS");
        var id = Kimlik.Ulid(); var now = DateTime.UtcNow;
        kurulum.Siparisler.Add(new Siparis
        {
            Id = id, TenantId = k.TenantId, No = $"S-{id[..8]}", Tur = "MUSTERI", Durum = SiparisDurumu.TeslimEdildi,
            CariId = k.CariId, SubeId = null, OlusturanKullaniciId = k.KullaniciId, Olusturuldu = now, Guncellendi = now,
        });
        await kurulum.SaveChangesAsync();
        using (var s1 = vt.OlusturSistem())
            Assert.False(await MMasak.YaptirimServisi.CariVarlikVar(s1, k.TenantId, k.CariId));
        await kurulum.Database.ExecuteSqlRawAsync("UPDATE siparisler SET durum = {0} WHERE id = {1}", SiparisDurumu.Onaylandi, id);
        using (var s2 = vt.OlusturSistem())
            Assert.True(await MMasak.YaptirimServisi.CariVarlikVar(s2, k.TenantId, k.CariId));
    }
}
