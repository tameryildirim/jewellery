using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MEmanet = Kuyumcu.Api.Modules.Emanet;
using MFatura = Kuyumcu.Api.Modules.Fatura;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET CARI-BAKIYE (30.09, ekip oyu) — cari bakiye tek tanım + tek yazıcı + TL aynası.
//   (1) TEK TANIM: liste, detay, bakiye ucu, ekstre, pasife alma özeti AYNI emanetsiz has'ı verir.
//   (2) TEK YAZICI: CariBakiyeYazici (Local satırı bulur, TL 2 / has 6 yuvarlar).
//   (3) TL AYNASI: her akıştan sonra kayıtlı TL = kural toplamı; bilinemeyen → null.
//   (4) Bant metni "düzeltme fişiyle giderin" demez (fiş bakiye ve aynayı birlikte oynatır).
//   (5) Fatura taslak PATCH: toplam has = birim × miktar (3 → 1,001 → 3 kayma 0).
// Mutasyon kodları (rapor § 5): MCBK-1 … MCBK-10.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class CariBakiyeTestleri(TestVeritabani vt)
{
    private const string Gerekce = "CARI-BAKIYE gerileme testi — yeterince uzun gerekçe";

    private static MEmanet.EmanetServisi Emanet(KuyumcuDbContext db) =>
        new(db, NullLogger<MEmanet.EmanetServisi>.Instance);

    private static MSatis.StornoServisi Storno(KuyumcuDbContext db) =>
        new(db, TestKurulum.Denetim(db), NullLogger<MSatis.StornoServisi>.Instance);

    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, TestKurulum.Denetim(db));

    private Kiraci YeniKiraci(string ad)
    {
        using var kurulum = vt.OlusturSistem();
        return TestKurulum.KiraciKur(kurulum, $"PAKETCARI-BAKIYE-TEST {ad}");
    }

    private static JsonElement J(object o) => JsonSerializer.SerializeToElement(o);

    private async Task<CariAyna> Ayna(Kiraci k)
    {
        using var db = vt.Olustur(k);
        return await CariBakiyeKurali.Hesapla(db, k.TenantId, k.CariId);
    }

    /// <summary>Her adımdan sonra: has VE TL aynası 0 (ölçülebilir olmalı).</summary>
    private async Task AynaSifir(Kiraci k, string adim)
    {
        var a = await Ayna(k);
        Assert.True(a.TlUyusmazligi is not null, $"{adim}: TL aynası ölçülemedi ({a.TlOlculemezSebep})");
        Assert.True(Math.Abs(a.HasUyusmazligi) <= CariBakiyeKurali.HasEsik, $"{adim}: has farkı {a.HasUyusmazligi}");
        Assert.True(Math.Abs(a.TlUyusmazligi!.Value) <= CariBakiyeKurali.TlEsik,
            $"{adim}: TL farkı {a.TlUyusmazligi} (kayıtlı {a.KayitliTl} · ayna {a.TlAyna})");
    }

    private async Task<string> SonHareket(Kiraci k, string tur)
    {
        using var oku = vt.OlusturSistem();
        return await oku.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == tur && h.StornoKaynakId == null)
            .OrderByDescending(h => h.Olusturuldu).ThenByDescending(h => h.Id)
            .Select(h => h.Id).FirstAsync();
    }

    private async Task Veresiye(Kiraci k, decimal tl, decimal? has = null)
    {
        using var db = vt.Olustur(k);
        await Cari(db).VeresiyeGir(k.CariId, new MCari.VeresiyeGirisDto
        {
            TlTutari = tl, HasGrami = has, KurSnapshotId = k.KurId, Aciklama = "CARI-BAKIYE veresiye", VadeGun = 30,
        }, k.Islem);
    }

    private async Task Tahsilat(Kiraci k, decimal? tl, decimal? has = null, string odeme = "NAKIT")
    {
        using var db = vt.Olustur(k);
        await Cari(db).TahsilatGir(k.CariId, new MCari.TahsilatGirisDto
        {
            TlTutari = tl, HasGrami = has, OdemeTuru = odeme,
            KasaId = tl is > 0m ? k.KasaId : null, KurSnapshotId = k.KurId, Aciklama = "CARI-BAKIYE tahsilat",
        }, k.Islem);
    }

    private async Task EmanetGiris(Kiraci k, string ayarId, decimal gram)
    {
        using var db = vt.Olustur(k);
        await Emanet(db).Giris(new MEmanet.EmanetGirisDto
        {
            CariId = k.CariId, AyarId = ayarId, GramAgirlik = gram, KurSnapshotId = k.KurId,
            Aciklama = "CARI-BAKIYE emanet",
        }, k.Islem);
    }

    private async Task StornoEt(Kiraci k, string hareketId, string? kasaId = null)
    {
        using var db = vt.Olustur(k);
        await Storno(db).Storno(hareketId, new MSatis.StornoDto
        {
            Gerekce = Gerekce, KurSnapshotId = k.KurId, KasaId = kasaId,
        }, k.Islem);
    }

    /// <summary>Liste, detay, bakiye ucu, ekstre ve pasife alma özeti — hepsinin has bakiyesi.</summary>
    private async Task<(decimal Liste, decimal ListeEmanette, decimal Detay, decimal DetayEmanette,
        decimal Uc, decimal Ekstre, decimal EkstreEmanetsiz, decimal EkstreEmanette, decimal Ozet, decimal Kayitli)>
        DortYuz(Kiraci k)
    {
        using var db = vt.Olustur(k);
        var s = Cari(db);
        var liste = J(await s.Listele(k.Islem, null, null, null, 50, 0))
            .EnumerateArray().Single(x => x.GetProperty("id").GetString() == k.CariId);
        var detay = J(await s.Getir(k.CariId, k.Islem)).GetProperty("bakiye");
        var uc = J(await s.BakiyeGetir(k.CariId, k.Islem));
        var ekstre = J(await s.Ekstre(k.CariId, k.Islem, null, null, 1000, 0));
        var ozet = J(await s.BagliKayitlariGetir(k.CariId, k.Islem));
        return (liste.GetProperty("hasGramBakiye").GetDecimal(), liste.GetProperty("emanetteHas").GetDecimal(),
            detay.GetProperty("hasGramBakiye").GetDecimal(), detay.GetProperty("emanetteHas").GetDecimal(),
            uc.GetProperty("hasGramBakiye").GetDecimal(),
            ekstre.GetProperty("bakiye").GetProperty("hasGramBakiye").GetDecimal(),
            ekstre.GetProperty("bakiyeEmanetsizHas").GetDecimal(),
            ekstre.GetProperty("emanetteHas").GetDecimal(),
            ozet.GetProperty("HasGramBakiye").GetDecimal(),
            detay.GetProperty("kayitliHasGramBakiye").GetDecimal());
    }

    // ═══════════ (1) TEK TANIM — liste = detay = ekstre (emanetli · emanetsiz · stornolu) ═══════

    /// <summary>
    /// KESIF-P2 açık kalan (a): aynı müşteri listede "−5,2278 has Biz borçluyuz", detayda "0,9322 has
    /// Bize borçlu" çıkıyordu. Emanetli müşteri: 2 has veresiye + 22 ayar 10 g emanet (9,16 has).
    /// Beş yüz de 2 has (emanetsiz) der, emanet ayrı 9,16. MCBK-1 (liste kayıtlı kolonu basarsa) KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Emanetli_musteri_liste_detay_uc_ekstre_ozet_AYNI_emanetsiz_has()
    {
        var k = YeniKiraci("emanetli");
        await Veresiye(k, 800m, 2m);
        await EmanetGiris(k, "K22", 10m);

        var y = await DortYuz(k);
        Assert.Equal(2m, y.Liste);
        Assert.Equal(2m, y.Detay);
        Assert.Equal(2m, y.Uc);
        Assert.Equal(2m, y.Ekstre);
        Assert.Equal(2m, y.EkstreEmanetsiz);
        Assert.Equal(2m, y.Ozet);
        Assert.Equal(9.16m, y.ListeEmanette);
        Assert.Equal(9.16m, y.DetayEmanette);
        Assert.Equal(9.16m, y.EkstreEmanette);
        Assert.Equal(2m - 9.16m, y.Kayitli);       // kolonun anlamı DEĞİŞMEDİ (ADR-CB-1)
        await AynaSifir(k, "emanetli");
    }

    /// <summary>Emanetsiz müşteri: emanetsiz = kayıtlı, "Emanette" 0.</summary>
    [Fact]
    public async Task Emanetsiz_musteri_liste_detay_ayni_ve_kayitliya_esit()
    {
        var k = YeniKiraci("emanetsiz");
        await Veresiye(k, 500m, 3.5m);

        var y = await DortYuz(k);
        Assert.Equal(3.5m, y.Liste);
        Assert.Equal(3.5m, y.Detay);
        Assert.Equal(3.5m, y.Ekstre);
        Assert.Equal(3.5m, y.Kayitli);
        Assert.Equal(0m, y.ListeEmanette);
        Assert.Equal(0m, y.EkstreEmanette);
    }

    /// <summary>
    /// Stornolu müşteri: emanet girişi stornolanır, veresiye stornolanır → her yüz 0, emanette 0.
    /// </summary>
    [Fact]
    public async Task Stornolu_musteri_liste_detay_ayni()
    {
        var k = YeniKiraci("stornolu");
        await Veresiye(k, 1_000m, 2m);
        var ver = await SonHareket(k, "VERESIYE");
        await EmanetGiris(k, "K22", 5m);
        var em = await SonHareket(k, "EMANET_GIRIS");
        await StornoEt(k, em);
        await StornoEt(k, ver);

        var y = await DortYuz(k);
        Assert.Equal((0m, 0m, 0m, 0m, 0m), (y.Liste, y.Detay, y.Ekstre, y.ListeEmanette, y.EkstreEmanette));
        await AynaSifir(k, "stornolu");
    }

    /// <summary>
    /// Raporlar → cari yaşlandırma da AYNI tanımı okur: emanetli müşterinin has'ı emanetsiz (2), yalnız
    /// emaneti olan müşteri "bakiyesi olan" sayılmaz. MCBK-14 (rapor kayıtlı kolonu okursa) KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Yaslandirma_raporu_emanetsiz_has_okur_yalniz_emanetli_cari_listede_yok()
    {
        var k = YeniKiraci("yaslandirma");
        await Veresiye(k, 800m, 2m);
        await EmanetGiris(k, "K22", 10m);
        string yalnizEmanet;
        using (var db = vt.Olustur(k))
        {
            yalnizEmanet = TestKurulum.Metin(await Cari(db).Olustur(new MCari.CariOlusturDto
            {
                Tur = "MUSTERI", Ad = "CARI-BAKIYE yalnız emanet",
            }, k.Islem), "id")!;
            await Emanet(db).Giris(new MEmanet.EmanetGirisDto
            {
                CariId = yalnizEmanet, AyarId = "K22", GramAgirlik = 5m, KurSnapshotId = k.KurId, Aciklama = "yalnız emanet",
            }, k.Islem);
        }
        using var db2 = vt.Olustur(k);
        var r = J(await TestKurulum.RaporServisi(db2).CariYaslandirma(k.Islem));
        var satirlar = r.GetProperty("yaslananCariler").EnumerateArray().ToList();
        var ana = satirlar.Single(x => x.GetProperty("cariId").GetString() == k.CariId);
        Assert.Equal(2m, ana.GetProperty("hasGramBakiye").GetDecimal());
        Assert.DoesNotContain(satirlar, x => x.GetProperty("cariId").GetString() == yalnizEmanet);
    }

    // ═══════════ (2) TEK YAZICI ═══════

    /// <summary>
    /// Aynı istekte satırı olmayan cariye iki yazım: ikinci yazım Local'deki (kaydedilmemiş) satırı
    /// bulur — iki ayrı satır eklenmez (PK çakışması yok) ve TL 2 / has 6 haneye yuvarlanır.
    /// MCBK-7 (Local araması kalkarsa) ve MCBK-8 (has deltası düşerse) KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Tek_yazici_Local_satiri_bulur_toplar_ve_yuvarlar()
    {
        var k = YeniKiraci("tek yazici");
        using (var s = vt.OlusturSistem())
        {
            await s.CariBakiyeler.Where(b => b.CariId == k.CariId).ExecuteDeleteAsync();
        }
        using (var db = vt.Olustur(k))
        {
            var now = DateTime.UtcNow;
            await CariBakiyeYazici.Uygula(db, k.TenantId, k.CariId, 100.004m, 1.0000004m, now);
            await CariBakiyeYazici.Uygula(db, k.TenantId, k.CariId, 0.006m, 2.0000006m, now);
            await db.SaveChangesAsync();
        }
        using var oku = vt.OlusturSistem();
        var satir = await oku.CariBakiyeler.AsNoTracking().SingleAsync(b => b.CariId == k.CariId);
        Assert.Equal(100.01m, satir.TlBakiye);          // 100 + 0,01
        Assert.Equal(3.000001m, satir.HasGramBakiye);    // 1 + 2,000001
        Assert.NotNull(satir.SonIslemTarihi);
    }

    // ═══════════ (3) TL AYNASI — akışlar ═══════

    /// <summary>
    /// Senaryonun para akışları tek müşteride: veresiye (TL + has), kısmi TL tahsilat, has tahsilatı,
    /// veresiye satış, tahsilat + stornosu, avans al + iade, veresiye işçilik, emanet. Her adımda
    /// has VE TL aynası 0. MCBK-2 (TAHSILAT yön kuralı), MCBK-3 (işaret × veresiye payı),
    /// MCBK-4 (avans işareti) KIRMIZI.
    /// </summary>
    [Fact]
    public async Task TL_aynasi_her_akista_sifir()
    {
        var k = YeniKiraci("tl aynasi");

        await Veresiye(k, 800m, 5m);                 await AynaSifir(k, "veresiye");
        await Tahsilat(k, 800m);                     await AynaSifir(k, "tahsilat TL");
        await Tahsilat(k, null, 5m);                 await AynaSifir(k, "tahsilat has");

        using (var db = vt.Olustur(k))
            await Satis(db).Satis(new MSatis.SatisOlusturDto
            {
                CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 5_000m,
                OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, VadeGun = 30,
            }, k.Islem);
        await AynaSifir(k, "veresiye satış");

        await Tahsilat(k, 2_000m);                   await AynaSifir(k, "tahsilat 2");
        var tah = await SonHareket(k, "TAHSILAT");
        await StornoEt(k, tah, k.KasaId);            await AynaSifir(k, "tahsilat stornosu");

        using (var db = vt.Olustur(k))
            await Satis(db).AvansAl(new MSatis.AvansAlDto
            {
                CariId = k.CariId, Tutar = 1_000m, OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);
        await AynaSifir(k, "avans alındı");

        using (var db = vt.Olustur(k))
            await Satis(db).AvansIade(new MSatis.AvansIadeDto
            {
                CariId = k.CariId, Tutar = 400m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
                Gerekce = "Müşteri avansının bir kısmını geri istedi",
            }, k.Islem);
        await AynaSifir(k, "avans iadesi");

        using (var db = vt.Olustur(k))
            await Satis(db).Iscilik(new MSatis.IscilikDto
            {
                CariId = k.CariId, IscilikTl = 750m, OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, VadeGun = 30,
                Aciklama = "CARI-BAKIYE işçilik veresiye",
            }, k.Islem);
        await AynaSifir(k, "işçilik veresiye");

        await EmanetGiris(k, "K22", 10m);            await AynaSifir(k, "emanet");

        var a = await Ayna(k);
        Assert.Equal(800m - 800m + 5_000m - 2_000m + 2_000m - 1_000m + 400m + 750m, a.KayitliTl);
    }

    /// <summary>
    /// Değişim, fark müşteri LEHİNE ve VERESİYE: cariye −fark yazılır ama iki bacak da ödeme izi
    /// taşımaz. Ayna farkı fişten çıkarır (DegisimVeresiyeFarki). MCBK-5 KIRMIZI.
    /// </summary>
    [Fact]
    public async Task TL_aynasi_degisim_musteri_lehine_veresiye_fark()
    {
        var k = YeniKiraci("degisim");
        using (var db = vt.Olustur(k))
            await Satis(db).Satis(new MSatis.SatisOlusturDto
            {
                CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 10_000m,
                OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);
        var satisId = await SonHareket(k, "SATIS");

        using (var db = vt.Olustur(k))
            await Satis(db).Degisim(new MSatis.DegisimDto
            {
                KaynakHareketId = satisId, GeriAlinanAdet = 1m,
                YeniUrunId = k.UrunId, YeniAdet = 1m, YeniBirimFiyatTl = 7_000m,
                FarkOdemeTuru = "VERESIYE", KurSnapshotId = k.KurId,
            }, k.Islem);

        var a = await Ayna(k);
        Assert.Equal(-3_000m, a.KayitliTl);
        await AynaSifir(k, "değişim −3.000 veresiye");
    }

    /// <summary>
    /// Konsinye malın sahibine borç defter satırı olmadan yazılır → TL aynası UYDURULMAZ: null +
    /// KONSINYE_SAHIBI. MCBK-6 (konsinye kapısı kalkarsa ayna 0 sanıp farkı "uyuşmazlık" der) KIRMIZI.
    /// </summary>
    [Fact]
    public async Task TL_aynasi_konsinye_sahibinde_olculemez_uydurulmaz()
    {
        var k = YeniKiraci("konsinye");
        using (var s = vt.OlusturSistem())
        {
            var u = await s.Urunler.SingleAsync(x => x.Id == k.UrunId);
            u.Konsinye = true; u.KonsinyeCariId = k.CariId; u.KonsinyeBirimBedel = 4_000m;
            await s.SaveChangesAsync();
        }
        using (var db = vt.Olustur(k))
            await Satis(db).Satis(new MSatis.SatisOlusturDto
            {
                CariId = null!, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 5_000m,
                OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);

        var a = await Ayna(k);
        Assert.Equal(-4_000m, a.KayitliTl);
        Assert.Null(a.TlAyna);
        Assert.Null(a.TlUyusmazligi);
        Assert.Equal(CariBakiyeKurali.SebepKonsinyeSahibi, a.TlOlculemezSebep);

        using var db2 = vt.Olustur(k);
        var e = J(await Cari(db2).Ekstre(k.CariId, k.Islem, null, null, 100, 0));
        Assert.Equal(JsonValueKind.Null, e.GetProperty("bakiyeUyusmazligiTl").ValueKind);
        Assert.Equal(CariBakiyeKurali.SebepKonsinyeSahibi, e.GetProperty("tlAynaOlculemez").GetString());
    }

    /// <summary>
    /// Kayıtlı TL, defterle tutmuyorsa (09.09 tohumunun TL tarafı gibi) ekstre farkı SÖYLER;
    /// has farkıyla birlikte, ayrı alanda. MCBK-9 (ekstre TL farkını döndürmezse) KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Ekstre_TL_uyusmazligini_ayri_soyler()
    {
        var k = YeniKiraci("tl fark");
        await Veresiye(k, 1_000m);
        using (var s = vt.OlusturSistem())
        {
            // Kayıtlı önbelleği defterden kopar (yalnız test: tohum hatası taklidi).
            await s.CariBakiyeler.Where(b => b.CariId == k.CariId)
                .ExecuteUpdateAsync(u => u.SetProperty(b => b.TlBakiye, 1_250m));
        }
        using var db = vt.Olustur(k);
        var e = J(await Cari(db).Ekstre(k.CariId, k.Islem, null, null, 100, 0));
        Assert.Equal(250m, e.GetProperty("bakiyeUyusmazligiTl").GetDecimal());
        Assert.Equal(0m, e.GetProperty("bakiyeUyusmazligiHas").GetDecimal());
        Assert.Equal(JsonValueKind.Null, e.GetProperty("tlAynaOlculemez").ValueKind);
        var satir = e.GetProperty("hareketler").EnumerateArray().Single();
        Assert.Equal(1_000m, satir.GetProperty("cariTlEtkisi").GetDecimal());
    }

    // ═══════════ (5) FATURA TASLAK PATCH — birim × miktar ═══════

    /// <summary>
    /// TOPLU 30.09 08:50: taslak kalem miktarı 3 → 1,001 → 3 yapılınca has, orantılı hesapta 6. hanede
    /// kayıyordu (3,703701 → 3,703702). Artık toplam = birim has × miktar: gidiş-dönüş kayması 0.
    /// MCBK-10 (orantılı hesaba dönülürse) KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Fatura_taslak_miktar_gidis_donus_has_kaymaz()
    {
        var k = YeniKiraci("fatura");
        using (var s = vt.OlusturSistem())
        {
            var u = await s.Urunler.SingleAsync(x => x.Id == k.UrunId);
            u.HasGrami = 1.234567m;
            await s.SaveChangesAsync();
        }
        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 60_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 3m, BirimFiyat = 20_000m }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kid = db.FaturaKalemleri.Single(x => x.FaturaId == fid).Id;

        decimal Has()
        {
            using var o = vt.OlusturSistem();
            return o.FaturaKalemleri.AsNoTracking().Single(x => x.Id == kid).HasGrami!.Value;
        }
        var ilk = Has();
        Assert.Equal(3.703701m, ilk);

        await servis.TaslakKalemGuncelle(fid, kid, JsonDocument.Parse("{\"miktar\":1.001}").RootElement, k.Islem);
        Assert.Equal(1.235802m, Has());   // Yuvarla6(1,234567 × 1,001)
        await servis.TaslakKalemGuncelle(fid, kid, JsonDocument.Parse("{\"miktar\":3}").RootElement, k.Islem);
        Assert.Equal(ilk, Has());         // gidiş-dönüş kayması 0

        using var o2 = vt.OlusturSistem();
        Assert.Equal(1.234567m, o2.FaturaKalemleri.AsNoTracking().Single(x => x.Id == kid).BirimHasGrami);
    }

    /// <summary>
    /// Birimi bilinmeyen (migration öncesi) taslak kalem: ilk düzeltmede eski toplam ÷ eski miktar BİR KEZ
    /// donar; sonraki düzeltmeler o birimden hesaplanır (yine kayma yok).
    /// </summary>
    [Fact]
    public async Task Fatura_taslak_birimsiz_eski_kalem_ilk_duzeltmede_birim_donar()
    {
        var k = YeniKiraci("fatura eski");
        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 200_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 2m, BirimFiyat = 100_000m }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kid = db.FaturaKalemleri.Single(x => x.FaturaId == fid).Id;
        using (var s = vt.OlusturSistem())
            await s.FaturaKalemleri.Where(x => x.Id == kid)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.BirimHasGrami, (decimal?)null)
                                          .SetProperty(x => x.HasGrami, 5m));

        // Yeni bağlam: izlenen (eski) kalem nesnesi değil, veritabanındaki birimsiz satır düzeltilir.
        using var db2 = vt.Olustur(k);
        var servis2 = new MFatura.FaturaServisi(db2);
        await servis2.TaslakKalemGuncelle(fid, kid, JsonDocument.Parse("{\"miktar\":3}").RootElement, k.Islem);
        await servis2.TaslakKalemGuncelle(fid, kid, JsonDocument.Parse("{\"miktar\":2}").RootElement, k.Islem);
        using var o = vt.OlusturSistem();
        var kalem = o.FaturaKalemleri.AsNoTracking().Single(x => x.Id == kid);
        Assert.Equal(2.5m, kalem.BirimHasGrami);
        Assert.Equal(5m, kalem.HasGrami);
    }
}

/// <summary>Kuralın saf (DB'siz) yüzü — TL etkisi tablosu ve bant metni.</summary>
public sealed class CariBakiyeSafTestleri
{
    /// <summary>MCBK-2/3/4: kural tablosunun her dalı.</summary>
    [Theory]
    [InlineData("VERESIYE", 800, null, false, 800)]
    [InlineData("TAHSILAT", -800, null, false, -800)]         // müşteri ödedi
    [InlineData("TAHSILAT", 800, null, true, 800)]            // tahsilat stornosu: borç geri doğar
    [InlineData("TAHSILAT", 2500, 0, false, 0)]               // evrak tahsili: cariye yazmaz
    [InlineData("TAHSILAT", -2500, 0, true, 0)]               // evrak tahsilinin stornosu: kural 0
    [InlineData("EVRAK_GIRIS", -2500, 2500, false, -2500)]
    [InlineData("EVRAK_RUCU", -1000, 0, false, -1000)]
    [InlineData("AVANS_ALINDI", 1000, 0, false, -1000)]
    [InlineData("AVANS_IADE", -400, 0, false, 400)]
    [InlineData("AVANS_MAHSUP", 300, 0, false, 0)]
    [InlineData("SATIS", 5000, 5000, false, 5000)]            // veresiye satış
    [InlineData("SATIS", 5000, 0, false, 0)]                  // peşin satış
    [InlineData("SATIS", 20000, 3000, false, 3000)]           // karma: yalnız veresiye payı
    [InlineData("ALIS", -4000, 1500, false, -1500)]           // tedarikçiye ödenmeyen kısım
    [InlineData("SATIS_IADE", -5000, 5000, false, -5000)]
    [InlineData("ISCILIK", -750, 750, true, -750)]            // veresiye işçiliğin stornosu
    [InlineData("DOVIZ_VERESIYE", 4000, 0, false, 0)]         // döviz satırı TL borcu değildir
    [InlineData("EMANET_GIRIS", 0, null, false, 0)]
    public void TL_etkisi_kural_tablosu(string tur, int tl, int? veresiye, bool storno, int beklenen) =>
        Assert.Equal((decimal)beklenen,
            CariBakiyeKurali.TlEtkisi(tur, (decimal)tl, veresiye is null ? null : (decimal)veresiye, storno));

    /// <summary>Veresiye payı bilinmeyen (Faz 0 öncesi) satır uydurulmaz: null.</summary>
    [Fact]
    public void TL_etkisi_veresiye_payi_bilinmiyorsa_null()
    {
        Assert.Null(CariBakiyeKurali.TlEtkisi("SATIS", 77_777m, null, false));
        Assert.Null(CariBakiyeKurali.TlAynasi(
        [
            new CariAynaGrubu("VERESIYE", null, false, 1, true, 100m, 0m, 0m),
            new CariAynaGrubu("SATIS", null, false, 1, true, 5m, 0m, 0m),
        ]));
        Assert.Equal(100m, CariBakiyeKurali.TlAynasi(
            [new CariAynaGrubu("VERESIYE", null, false, 1, true, 100m, 0m, 0m)]));
    }

    /// <summary>
    /// ⚠ Yeni bulgu (kök neden § 4): düzeltme fişi bakiyeyi VE aynayı aynı miktarda oynatır, farkı
    /// kapatamaz. Bant metni fişi önermez; doğru yol (yeniden hesap / destek) yazar. MCBK-11 KIRMIZI.
    /// </summary>
    [Fact]
    public void Uyusmazlik_bandi_duzeltme_fisi_onermez()
    {
        foreach (var m in new[] { KesifP2Sozlugu.Tr, KesifP2Sozlugu.En })
        {
            foreach (var metin in new[] { m.BakiyeUyusmazligi, m.BakiyeUyusmazligiTl })
            {
                Assert.DoesNotContain("fişiyle giderin", metin);
                Assert.DoesNotContain("correct it with an adjustment slip", metin);
                Assert.Contains("{0}", metin);
            }
        }
        Assert.Contains("yeniden hesaplanması", KesifP2Sozlugu.Tr.BakiyeUyusmazligi);
        Assert.Contains("Destekle iletişime geçin", KesifP2Sozlugu.Tr.BakiyeUyusmazligiTl);
        Assert.Contains("contact support", KesifP2Sozlugu.En.BakiyeUyusmazligi);
        // Liste artık emanetsiz: kural metni "emaneti de içeren net tutar" demez.
        Assert.DoesNotContain("emaneti de içeren", KesifP2Sozlugu.Tr.HasEtkisiKural);
    }

    /// <summary>
    /// Düzeltme fişinin kural aynasındaki etkisi bakiyedeki etkisine EŞİT (ikisi birlikte oynar):
    /// fiş uyuşmazlığı kapatamaz — metnin gerekçesi. has: yazılan +HasDelta, satır −HasDelta → ayna +HasDelta.
    /// </summary>
    [Fact]
    public void Duzeltme_fisi_bakiye_ve_aynayi_ayni_miktarda_oynatir()
    {
        const decimal hasDelta = 7.5m, tlDelta = 1_200m;
        Assert.Equal(hasDelta, CariBakiyeKurali.HasEtkisi("CARI_DUZELTME", -hasDelta, "ALTIN"));
        Assert.Equal(tlDelta, CariBakiyeKurali.TlEtkisi("CARI_DUZELTME", tlDelta, 0m, false));
    }
}

/// <summary>HTTP: gerçek JSON yanıtında tek tanım alanları (camelCase) ve TL aynası.</summary>
[Collection("http")]
public sealed class CariBakiyeHttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Liste_ve_ekstre_JSON_ayni_emanetsiz_has_ve_TL_aynasi_doner()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem())
            k = TestKurulum.KiraciKur(db, $"PAKETCARI-BAKIYE-TEST http {Guid.NewGuid():N}"[..40]);
        var c = f.Ortam.Istemci(k, "SAHIP");

        var v = await c.PostJson($"/api/v1/cari/{k.CariId}/veresiye",
            new { tlTutari = 600m, hasGrami = 1.5m, kurSnapshotId = k.KurId, vadeGun = 30 });
        Assert.True(v.IsSuccessStatusCode, await v.Content.ReadAsStringAsync());
        var e = await c.PostJson("/api/v1/emanet/giris",
            new { cariId = k.CariId, ayarId = "K22", gramAgirlik = 10m, tasAgirlik = 0m, kurSnapshotId = k.KurId });
        Assert.True(e.IsSuccessStatusCode, await e.Content.ReadAsStringAsync());

        var liste = JsonDocument.Parse(await c.GetStringAsync("/api/v1/cari")).RootElement
            .EnumerateArray().Single(x => x.GetProperty("id").GetString() == k.CariId);
        var ekstre = JsonDocument.Parse(await c.GetStringAsync($"/api/v1/cari/{k.CariId}/ekstre")).RootElement;

        Assert.Equal(1.5m, liste.GetProperty("hasGramBakiye").GetDecimal());
        Assert.Equal(9.16m, liste.GetProperty("emanetteHas").GetDecimal());
        Assert.Equal(1.5m, ekstre.GetProperty("bakiye").GetProperty("hasGramBakiye").GetDecimal());
        Assert.Equal(1.5m, ekstre.GetProperty("bakiyeEmanetsizHas").GetDecimal());
        Assert.Equal(9.16m, ekstre.GetProperty("emanetteHas").GetDecimal());
        Assert.Equal(0m, ekstre.GetProperty("bakiyeUyusmazligiHas").GetDecimal());
        Assert.Equal(0m, ekstre.GetProperty("bakiyeUyusmazligiTl").GetDecimal());

        // Kâğıt ekstre de aynı tanım: "Has gram bakiye" emanetsiz, emanet ayrı satırda. MCBK-15 KIRMIZI.
        var belge = JsonDocument.Parse(await c.GetStringAsync($"/api/v1/cikti/CARI_EKSTRE/{k.CariId}")).RootElement;
        var tutarlar = belge.GetProperty("tutarlar").EnumerateArray()
            .ToDictionary(t => t.GetProperty("etiket").GetString()!, t => t.GetProperty("deger").GetDecimal());
        Assert.Equal(1.5m, tutarlar["Has gram bakiye"]);
        Assert.Equal(9.16m, tutarlar["Emanette (bakiyeye girmez)"]);
    }
}

/// <summary>bUnit: liste "Emanette" satırı ve TL uyuşmazlık bandı (gerçekçi sahte yanıt).</summary>
public sealed class CariBakiyeEkranTestleri : BunitContext
{
    // Sunucunun YENİ sözleşmesi: hasGramBakiye emanetsiz (2), emanetteHas 9,16 ayrı.
    private const string CariListesi = """
        [{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567",
          "email":"ayse@ornek.com","tlBakiye":800.00,"hasGramBakiye":2,"emanetteHas":9.16,
          "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"riskSeviye":null,"pepBeyani":false},
         {"id":"c2","ad":"Mehmet Kaya","tur":"MUSTERI","telefon":null,
          "email":null,"tlBakiye":0,"hasGramBakiye":0,"emanetteHas":0,
          "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"riskSeviye":null,"pepBeyani":false}]
        """;

    private const string EkstreTemel = """
        {"bakiye":{"tlBakiye":800,"hasGramBakiye":2,"emanetteHas":9.16,"kayitliHasGramBakiye":-7.16,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0},
         "hareketler":[
          {"id":"h1","tur":"VERESIYE","tlTutari":800,"hasGrami":-2,"aciklama":null,"olusturuldu":"2026-09-30T09:00:00Z","cariHasEtkisi":2,"bakiyeHasEtkisi":2,"emanetHas":0,"hasBakiyesineGirer":true,"emanet":false,"maden":"ALTIN","cariTlEtkisi":800},
          {"id":"h2","tur":"EMANET_GIRIS","tlTutari":0,"hasGrami":9.16,"aciklama":"bilezik","olusturuldu":"2026-09-30T08:00:00Z","cariHasEtkisi":-9.16,"bakiyeHasEtkisi":0,"emanetHas":9.16,"hasBakiyesineGirer":false,"emanet":true,"maden":"ALTIN","cariTlEtkisi":0}],
         "sayfaDisiHasEtkisi":0,"dahaFazlaVar":false,"limit":100,
         "bakiyeUyusmazligiHas":0,"bakiyeEmanetsizHas":2,"emanetteHas":9.16,
         "bakiyeUyusmazligiTl":__TL__,"tlAynaOlculemez":__SEBEP__}
        """;

    private string ekstreYaniti = EkstreTemel.Replace("__TL__", "0").Replace("__SEBEP__", "null");

    public CariBakiyeEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => ekstreYaniti)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(Func<string> ekstre) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"gbpTry":null}""",
                var y when y.Contains("/ekstre") => ekstre(),
                var y when y.EndsWith("/cari/c1") => """
                    {"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567","email":null,
                     "vergiNo":null,"adres":null,"krediLimitiTl":null,"varsayilanVadeGun":30,"fiyatListesiId":null,
                     "pepBeyani":false,"pepGorev":null,"pepBeyanTarihi":null,"uyruk":null}
                    """,
                var y when y.EndsWith("/cari") => CariListesi,
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","aktif":true}]""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Cariler> Ciz()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        return Render<Kuyumcu.Web.Pages.Cariler>();
    }

    private static string H(decimal n) => Kuyumcu.Web.Pages.Cariler.HasBicim(n);
    private static KesifP2Metinleri P2 => KesifP2Sozlugu.Tr;

    /// <summary>
    /// Listedeki has hücresi sunucunun emanetsiz değerini basar, emanet ayrı satırda "Emanette 9,16 has";
    /// emaneti olmayan satırda o satır YOK. Liste ile detay kutusu aynı sayıyı gösterir.
    /// </summary>
    [Fact]
    public void Liste_emanetsiz_has_ve_ayri_Emanette_satiri_detayla_ayni()
    {
        var e = Ciz();
        e.WaitForElement("#cari-liste-emanette-c1");
        Assert.Equal(string.Format(P2.ListeEmanette, H(9.16m)), e.Find("#cari-liste-emanette-c1").TextContent.Trim());
        Assert.Empty(e.FindAll("#cari-liste-emanette-c2"));
        var satir = e.Find("#cari-liste-emanette-c1").ParentElement!.TextContent;
        Assert.Contains(H(2m), satir);
        Assert.DoesNotContain(H(-7.16m), e.Markup);          // kayıtlı (emanet dahil) hiçbir yerde basılmaz

        e.Find("#c-detay-c1").Click();
        e.WaitForElement("#cari-detay-hareketler");
        var kutular = e.Find("#cari-detay-bakiyeler").TextContent;
        Assert.Contains(H(2m), kutular);
        Assert.Contains(H(9.16m), kutular);
    }

    /// <summary>TL aynası tutmuyorsa ayrı TL bandı (doğru yol metniyle); 0 ya da null ise bant yok.</summary>
    [Fact]
    public void TL_uyusmazlik_bandi_yalniz_fark_varken_ve_dogru_metinle()
    {
        var e = Ciz();
        e.Find("#c-detay-c1").Click();
        e.WaitForElement("#cari-detay-hareketler");
        Assert.Empty(e.FindAll("#cari-detay-uyusmazlik-tl"));

        ekstreYaniti = EkstreTemel.Replace("__TL__", "-71283.3").Replace("__SEBEP__", "null");
        var e2 = Ciz();
        e2.Find("#c-detay-c1").Click();
        e2.WaitForElement("#cari-detay-uyusmazlik-tl");
        var bant = e2.Find("#cari-detay-uyusmazlik-tl");
        // TOPLU-1: TL ve altın farkı TEK uyarıda (bakiyenin yanında); TL farkı kimlikli parça olarak.
        Assert.Equal(string.Format(Toplu1Sozlugu.Tr.UyusmazlikTlFark, Bicim.Tl(-71283.3m)), bant.TextContent.Trim());
        Assert.Contains("bant-uyari", e2.Find("#cari-detay-uyusmazlik").ClassName);
        Assert.DoesNotContain("düzeltme fişiyle", e2.Find("#cari-detay").TextContent);

        ekstreYaniti = EkstreTemel.Replace("__TL__", "null").Replace("__SEBEP__", "\"KONSINYE_SAHIBI\"");
        var e3 = Ciz();
        e3.Find("#c-detay-c1").Click();
        e3.WaitForElement("#cari-detay-hareketler");
        Assert.Empty(e3.FindAll("#cari-detay-uyusmazlik-tl"));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET CARI-BAKIYE — TUR 2 (01.10, hasım bulguları):
//   KRİTİK: evrak tahsilinin stornosu cariye −TL yazıyordu (StornoServisi TAHSILAT dalı koşulsuz).
//           Kural artık aynayla ORTAK: CariBakiyeKurali.TahsilatCariTl. MCBK-18 / MCBK-19 kırmızı.
//   DÜŞÜK:  dışa aktarım cari CSV'si "hasGramBakiye" = tek tanım (emanetsiz) + emanetteHas +
//           kayitliHasGramBakiye. MCBK-20 kırmızı.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class CariBakiyeTur2Testleri(TestVeritabani vt)
{
    private const string Gerekce = "CARI-BAKIYE tur 2 gerileme testi — yeterince uzun gerekçe";

    private Kiraci YeniKiraci(string ad)
    {
        using var kurulum = vt.OlusturSistem();
        return TestKurulum.KiraciKur(kurulum, $"PAKETCARI-BAKIYE-TEST t2 {ad}");
    }

    private static Kuyumcu.Api.Modules.Evrak.EvrakServisi Evrak(KuyumcuDbContext db) =>
        new(db, NullLogger<Kuyumcu.Api.Modules.Evrak.EvrakServisi>.Instance, TestKurulum.Denetim(db),
            Kuyumcu.Api.Modules.Tevsik.TevsikKapisi.Varsayilan());

    private static MSatis.StornoServisi Storno(KuyumcuDbContext db) =>
        new(db, TestKurulum.Denetim(db), NullLogger<MSatis.StornoServisi>.Instance);

    private async Task<CariAyna> Ayna(Kiraci k)
    {
        using var db = vt.Olustur(k);
        return await CariBakiyeKurali.Hesapla(db, k.TenantId, k.CariId);
    }

    private async Task<string> Tahsil(Kiraci k)
    {
        string evrakId;
        using (var db = vt.Olustur(k))
            evrakId = (await Evrak(db).Gir(new Kuyumcu.Api.Modules.Evrak.EvrakGirisDto
            {
                EvrakTuru = "CEK", Tutar = 10_000m, VadeGunu = Zaman.YerelBugun().AddDays(30),
                EvrakNo = "CEK-CB-T2-1", BankaAdi = "Ziraat",
                KaynakCariId = k.CariId, KurSnapshotId = k.KurId,
            }, k.Islem)).Evrak.Id;
        using (var db = vt.Olustur(k))
            await Evrak(db).TahsilEdildi(evrakId, new Kuyumcu.Api.Modules.Evrak.TahsilEdildiDto
            {
                Hedef = "KASA", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);
        using var oku = vt.OlusturSistem();
        return await oku.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT")
            .Select(h => h.Id).SingleAsync();
    }

    /// <summary>
    /// Evrak girişi −10.000 TL; tahsil cariye yazmaz; tahsilin STORNOSU da cariye yazmaz
    /// (yazılmamış etki geri alınmaz). Her adımda TL aynası 0. MCBK-18 KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Evrak_tahsili_stornosu_cariye_yazmaz_TL_aynasi_sifir()
    {
        var k = YeniKiraci("evrak storno");
        var tahsil = await Tahsil(k);

        var once = await Ayna(k);
        Assert.Equal(-10_000m, once.KayitliTl);
        Assert.Equal(0m, once.TlUyusmazligi);

        using (var db = vt.Olustur(k))
            await Storno(db).Storno(tahsil, new MSatis.StornoDto
            {
                Gerekce = Gerekce, KurSnapshotId = k.KurId, KasaId = k.KasaId,
            }, k.Islem);

        using (var oku = vt.OlusturSistem())
            Assert.True(await oku.Hareketler.AsNoTracking()
                .AnyAsync(h => h.TenantId == k.TenantId && h.StornoKaynakId == tahsil),
                "storno satırı yazılmadı — sınama boşa düşer");

        var sonra = await Ayna(k);
        Assert.Equal(-10_000m, sonra.KayitliTl);
        Assert.Equal(0m, sonra.TlUyusmazligi);
    }

    /// <summary>
    /// Storno ile ayna AYNI kuralı okur (EmanetKurali deseni): müşteri ödemesi (TL &lt; 0) yazılır,
    /// evrak tahsili (TL &gt; 0) yazılmaz. MCBK-19 KIRMIZI.
    /// </summary>
    [Theory]
    [InlineData(-800, -800)]
    [InlineData(2500, 0)]
    [InlineData(0, 0)]
    public void Tahsilat_cari_TL_kurali_storno_ve_aynada_ortak(int kaynakTl, int beklenen)
    {
        Assert.Equal((decimal)beklenen, CariBakiyeKurali.TahsilatCariTl(kaynakTl));
        // Aynanın storno satırı (yön = −kaynak) aynı kararı verir: yazdıysa −etki, yazmadıysa 0.
        Assert.Equal(-(decimal)beklenen,
            CariBakiyeKurali.TlEtkisi("TAHSILAT", -(decimal)kaynakTl, null, storno: true));
    }

    /// <summary>
    /// Dışa aktarım cari CSV'si: "hasGramBakiye" ekrandaki tek tanımdır (emanetsiz); emanet
    /// "emanetteHas" kolonunda, kayıtlı kolon "kayitliHasGramBakiye" adıyla ayrı. MCBK-20 KIRMIZI.
    /// </summary>
    [Fact]
    public async Task Disa_aktarim_cari_csv_has_tek_tanim_emanet_ayri()
    {
        var k = YeniKiraci("csv");
        using (var db = vt.Olustur(k))
            await new Kuyumcu.Api.Modules.Cari.CariServisi(db, NullLogger<Kuyumcu.Api.Modules.Cari.CariServisi>.Instance,
                TestKurulum.Denetim(db)).VeresiyeGir(k.CariId, new Kuyumcu.Api.Modules.Cari.VeresiyeGirisDto
            {
                TlTutari = 800m, HasGrami = 2m, KurSnapshotId = k.KurId, Aciklama = "CARI-BAKIYE csv", VadeGun = 30,
            }, k.Islem);
        using (var db = vt.Olustur(k))
            await new Kuyumcu.Api.Modules.Emanet.EmanetServisi(db, NullLogger<Kuyumcu.Api.Modules.Emanet.EmanetServisi>.Instance)
                .Giris(new Kuyumcu.Api.Modules.Emanet.EmanetGirisDto
                {
                    CariId = k.CariId, AyarId = "K22", GramAgirlik = 10m, KurSnapshotId = k.KurId,
                    Aciklama = "CARI-BAKIYE csv emanet",
                }, k.Islem);

        var kaynak = new Kuyumcu.Api.Modules.Disaaktarim.CariKaynagi();
        var bas = kaynak.Basliklar.ToList();
        string[]? satir = null;
        using (var db = vt.Olustur(k))
            await foreach (var s in kaynak.Satirlar(db, k.TenantId, maskeli: false))
                if (s[0] == k.CariId) satir = s;
        Assert.NotNull(satir);
        Assert.Equal(bas.Count, satir!.Length);

        decimal D(string kolon) => decimal.Parse(satir[bas.IndexOf(kolon)], System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(2m, D("hasGramBakiye"));            // ekrandaki has (emanetsiz)
        Assert.Equal(9.16m, D("emanetteHas"));           // 10 g × 22 ayar
        Assert.Equal(2m - 9.16m, D("kayitliHasGramBakiye"));
        Assert.Equal(800m, D("tlBakiye"));
    }
}
