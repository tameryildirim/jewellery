using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Isler;
using Kuyumcu.Api.Modules.Isler.Isler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// FAZ A İŞLERİNİN SENARYOLARI (paket 6)
//
// ⚠ FİKSTÜR YÖNTEMİ (Aşkın'ın koşulu): "vadesi 3 gün geçmiş borç" VadeKapisi
//   üzerinden ÜRETİLEMEZ — VadeHesapla vadeyi bugüne ya da ileriye koyar,
//   HareketFabrikasi taslakta VadeTarihi doldurulmasını reddeder. Bu yüzden
//   önce GERÇEK bir veresiye satış yapılır (defter, bakiye, vade hepsi
//   üretimdeki yoldan doğar), sonra YALNIZ o satırın vade_tarihi ham SQL ile
//   geçmişe alınır. Deftere test tarafından satır EKLENMEZ.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class IsSenaryoTestleri(TestVeritabani vt)
{
    private static readonly IConfiguration Bos = IsCatisiKurulum.Yapilandirma();

    private Kiraci YeniKiraci(string ad) =>
        TestKurulum.KiraciKur(vt.OlusturSistem(), ad, stokAdet: 50m);

    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    /// <summary>Gerçek veresiye satış + vadeyi geçmişe alma (yalnız fikstür).</summary>
    private async Task VeresiyeBorcYarat(Kiraci k, decimal tutar, int gecikmeGun)
    {
        using var db = vt.Olustur(k.TenantId);
        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = tutar,
            OdemeTuru = "VERESIYE", KasaId = null, KurSnapshotId = k.KurId, VadeGun = 0,
        }, k.Islem);

        if (gecikmeGun <= 0) return;
        var yeniVade = Zaman.YerelBugun().AddDays(-gecikmeGun).ToString("yyyy-MM-dd");
        using var ham = vt.OlusturSistem();
        await ham.Database.ExecuteSqlRawAsync(
            "UPDATE hareketler SET vade_tarihi = {0} WHERE tenant_id = {1} AND tur = 'SATIS'",
            yeniVade, k.TenantId);
    }

    private static async Task<IsKosumSonucu> Kostur(
        TestVeritabani vt, Kiraci k, IZamanlanmisIs i, IConfiguration y)
    {
        using var db = vt.Olustur(k.TenantId);
        return await IsCatisiKurulum.Calistirici(db, y, i).Calistir(i, k.TenantId, DateTime.UtcNow, null, default);
    }

    private async Task<List<Bildirim>> Bildirimler(Kiraci k)
    {
        using var kontrol = vt.OlusturSistem();
        return await kontrol.Bildirimler.AsNoTracking()
            .Where(b => b.TenantId == k.TenantId).ToListAsync();
    }

    // ── VADE_HATIRLATMA ─────────────────────────────────────────────────────

    [Fact]
    public async Task Vadesi_gecmis_veresiye_KRITIK_bildirim_uretir()
    {
        var k = YeniKiraci("PAKET6-SENARYO vade geçmiş");
        await VeresiyeBorcYarat(k, 12_000m, gecikmeGun: 3);

        var sonuc = await Kostur(vt, k, new VadeHatirlatmaIsi(), Bos);

        Assert.Equal(IsDurum.Basarili, sonuc.Durum);
        Assert.Equal(1, sonuc.UretilenBildirim);

        var b = Assert.Single(await Bildirimler(k));
        Assert.Equal(VadeHatirlatmaIsi.IsKodu, b.Tur);
        Assert.Equal(BildirimOnem.Kritik, b.Onem);
        Assert.Equal("/raporlar/vade-takibi", b.HedefRota);
        Assert.Equal(k.CariId, b.IlgiliVarlikId);
        Assert.Equal("", b.GorunurRoller);   // vade bildirimi kişisel veri taşımaz → herkese açık
        Assert.Contains("3 gündür", b.Govde, StringComparison.Ordinal);
    }

    /// <summary>
    /// M6-4 (DB katmanı): tekillik anahtarına SAAT eklenirse aynı gün içindeki
    /// ikinci koşum İKİNCİ bir bildirim üretir ve kullanıcı dırdırla karşılaşır.
    /// Saf anahtar testi biçimi ölçer; bu test DAVRANIŞI ölçer.
    /// </summary>
    [Fact]
    public async Task Ayni_gunde_iki_kosum_cari_basina_TEK_vade_bildirimi_birakir()
    {
        var k = YeniKiraci("PAKET6-SENARYO vade tekillik");
        await VeresiyeBorcYarat(k, 15_000m, gecikmeGun: 2);

        var i = new VadeHatirlatmaIsi();
        using var db = vt.Olustur(k.TenantId);
        var c = IsCatisiKurulum.Calistirici(db, Bos, i);
        var gunluk = await c.Calistir(i, k.TenantId, DateTime.UtcNow, null, default);
        var elle = await c.Calistir(i, k.TenantId, DateTime.UtcNow, k.KullaniciId, default);

        Assert.Equal(1, gunluk.UretilenBildirim);
        Assert.Equal(0, elle.UretilenBildirim);
        Assert.Equal(1, elle.AtlananBildirim);
        Assert.Single(await Bildirimler(k));
    }

    [Fact]
    public async Task Vadesi_ufkun_otesinde_olan_cari_icin_bildirim_URETILMEZ()
    {
        var k = YeniKiraci("PAKET6-SENARYO ufuk dışı");
        using (var db = vt.Olustur(k.TenantId))
            await Satis(db).Satis(new MSatis.SatisOlusturDto
            {
                CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 9_000m,
                OdemeTuru = "VERESIYE", KasaId = null, KurSnapshotId = k.KurId, VadeGun = 30,
            }, k.Islem);

        var sonuc = await Kostur(vt, k, new VadeHatirlatmaIsi(),
            IsCatisiKurulum.Yapilandirma(("Isler:Vade:UfukGun", "7")));

        Assert.Equal(0, sonuc.UretilenBildirim);
        Assert.Empty(await Bildirimler(k));
    }

    [Fact]
    public async Task Borc_tahsil_edilince_YENI_bildirim_URETILMEZ()
    {
        var k = YeniKiraci("PAKET6-SENARYO tahsil");
        await VeresiyeBorcYarat(k, 8_000m, gecikmeGun: 5);

        // Borç kapanır (gerçek tahsilat yolu).
        using (var db = vt.Olustur(k.TenantId))
        {
            var cari = new Kuyumcu.Api.Modules.Cari.CariServisi(
                db, NullLogger<Kuyumcu.Api.Modules.Cari.CariServisi>.Instance,
                new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor()));
            await cari.TahsilatGir(k.CariId, new Kuyumcu.Api.Modules.Cari.TahsilatGirisDto
            {
                TlTutari = 8_000m, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);
        }

        var sonuc = await Kostur(vt, k, new VadeHatirlatmaIsi(), Bos);

        Assert.Equal(0, sonuc.UretilenBildirim);
        Assert.Empty(await Bildirimler(k));
    }

    // ── MASAK_SIB_SURESI ────────────────────────────────────────────────────

    private async Task SibEkle(Kiraci k, int gunSonra)
    {
        using var db = vt.Olustur(k.TenantId);
        db.MasakSupheliBildirimler.Add(new MasakSupheliBildirim
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, CariId = k.CariId,
            Gerekce = new string('G', 320), TespitTarihi = DateTime.UtcNow.AddDays(-7),
            SonBildirimTarihi = DateTime.UtcNow.AddDays(gunSonra),
            Durum = SupheliDurum.Acik, BildirenKullaniciId = k.KullaniciId,
            SaklamaBitis = DateTime.UtcNow.AddYears(8), Olusturuldu = DateTime.UtcNow.AddDays(-7),
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Suresi_yaklasan_SIB_bildirim_uretir_ve_KISISEL_VERI_TASIMAZ()
    {
        var k = YeniKiraci("PAKET6-SENARYO SIB yaklaşan");
        await SibEkle(k, gunSonra: 2);

        var sonuc = await Kostur(vt, k, new MasakSibSuresiIsi(), Bos);

        Assert.Equal(1, sonuc.UretilenBildirim);
        var b = Assert.Single(await Bildirimler(k));
        Assert.Equal(MasakSibSuresiIsi.IsKodu, b.Tur);
        Assert.Equal(BildirimOnem.Uyari, b.Onem);

        // Görünürlük: TEZGÂHTAR bu bildirimi GÖRMEZ (süzme sunucuda).
        Assert.Equal("SAHIP,MUDUR,MUHASEBE", b.GorunurRoller);
        Assert.DoesNotContain("TEZGAHTAR", b.GorunurRoller, StringComparison.Ordinal);

        // Kişisel veri: cari adı, TCKN ve gerekçe metni gövdeye SIZMAZ.
        var metin = b.Baslik + " " + b.Govde;
        Assert.DoesNotContain("müşterisi", metin, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("10000000146", metin, StringComparison.Ordinal);
        Assert.DoesNotContain("GGG", metin, StringComparison.Ordinal);
        Assert.DoesNotContain(k.CariId, metin, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Suresi_gecmis_SIB_bildirimi_KRITIK_olur()
    {
        var k = YeniKiraci("PAKET6-SENARYO SIB geçmiş");
        await SibEkle(k, gunSonra: -1);

        await Kostur(vt, k, new MasakSibSuresiIsi(), Bos);

        var b = Assert.Single(await Bildirimler(k));
        Assert.Equal(BildirimOnem.Kritik, b.Onem);
        Assert.Contains("GEÇTİ", b.Baslik, StringComparison.Ordinal);
    }

    // ── SAKLAMA_ONIZLEME ────────────────────────────────────────────────────

    [Fact]
    public async Task Saklama_onizlemesi_bildirim_uretir_ama_MASKELEME_YAPMAZ()
    {
        var k = YeniKiraci("PAKET6-SENARYO saklama");
        const string ad = "Eski Müşteri Kaydı";
        using (var db = vt.Olustur(k.TenantId))
        {
            db.MasakKimlikTespitleri.Add(new MasakKimlikTespiti
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, CariId = k.CariId, IslemTutariTl = 250_000m,
                BelgeTuru = KimlikBelgeTuru.TcKimlik, BelgeNo = "10000000146", AdSoyad = ad,
                Adres = "Ulus", Uyruk = "TUR", DogumTarihi = new DateOnly(1980, 1, 1),
                TespitEdenKullaniciId = k.KullaniciId, Tarih = DateTime.UtcNow.AddYears(-9),
                SaklamaBitis = DateTime.UtcNow.AddDays(-1), Olusturuldu = DateTime.UtcNow.AddYears(-9),
            });
            await db.SaveChangesAsync();
        }

        var sonuc = await Kostur(vt, k, new SaklamaOnizlemeIsi(), Bos);

        Assert.Equal(1, sonuc.UretilenBildirim);
        var b = Assert.Single(await Bildirimler(k));
        Assert.Equal(SaklamaOnizlemeIsi.IsKodu, b.Tur);
        Assert.Equal("/uyum", b.HedefRota);
        Assert.StartsWith("SAKLAMA:", b.TekillikAnahtari, StringComparison.Ordinal);

        // ⚠ ADR-0008 KANITI: Uygula() ÇAĞRILMADI — ad_soyad DEĞİŞMEDİ.
        using var kontrol = vt.OlusturSistem();
        var tespit = await kontrol.MasakKimlikTespitleri.AsNoTracking()
            .SingleAsync(m => m.TenantId == k.TenantId);
        Assert.Equal(ad, tespit.AdSoyad);
        Assert.NotEqual(Kuyumcu.Api.Modules.Masak.SaklamaSuresiIsi.Maskelenmis, tespit.AdSoyad);
    }

    [Fact]
    public async Task Saklama_onizlemesi_ayda_BIR_bildirim_uretir()
    {
        var k = YeniKiraci("PAKET6-SENARYO saklama aylık");
        using (var db = vt.Olustur(k.TenantId))
        {
            db.MasakBeyanlari.Add(new MasakBeyani
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, KimlikTespitiId = Kimlik.Ulid(),
                IslemTutariTl = 250_000m, Kademe = BeyanKademesi.AmacBeyani,
                IslemAmaci = "Yatırım amaçlı alım yapıldı",
                Aciklama = "Müşteri yatırım amacıyla altın aldı, kaynak maaş birikimi.",
                AlanKullaniciId = k.KullaniciId, Tarih = DateTime.UtcNow.AddYears(-9),
                SaklamaBitis = DateTime.UtcNow.AddDays(-1), Olusturuldu = DateTime.UtcNow.AddYears(-9),
            });
            await db.SaveChangesAsync();
        }

        var i = new SaklamaOnizlemeIsi();
        using var db2 = vt.Olustur(k.TenantId);
        var c = IsCatisiKurulum.Calistirici(db2, Bos, i);
        var birinci = await c.Calistir(i, k.TenantId, DateTime.UtcNow, null, default);
        var ikinci = await c.Calistir(i, k.TenantId, DateTime.UtcNow, k.KullaniciId, default);

        Assert.Equal(1, birinci.UretilenBildirim);
        Assert.Equal(0, ikinci.UretilenBildirim);
        Assert.Equal(1, ikinci.AtlananBildirim);
        Assert.Single(await Bildirimler(k));
    }

    // ── PAKET YASAL-2 (TB-16): Faz B işi ARTIK VAR ve varsayılanı AÇIK ─────────

    /// <summary>
    /// M6-8'in tersi (PAKET YASAL-2 § 4.2): TEVSIK_KURTARMA kayıtlı ve yapılandırma
    /// varsayılanı AÇIK — dayanak (VUK mük. md.355) birincil metinden doğrulandı.
    /// </summary>
    [Fact]
    public void Tevsik_kurtarma_isi_KAYITLI_ve_varsayilani_ACIK()
    {
        var s = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Kuyumcu.Api.Modules.ModulKayit.AddKuyumcuModulleri(s);
        var kodlar = s.Where(d => d.ServiceType == typeof(IZamanlanmisIs))
            .Select(d => d.ImplementationType!.Name).ToArray();
        Assert.Contains("TevsikKurtarmaIsi", kodlar);

        var ayarlar = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Api", "appsettings.json"))
            .Build();
        Assert.True(ayarlar.GetValue("Isler:TevsikKurtarma:Etkin", false));
        Assert.True(new TevsikKurtarmaIsi().Etkin(ayarlar));
    }
}
