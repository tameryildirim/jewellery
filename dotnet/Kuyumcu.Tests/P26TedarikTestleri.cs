using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MFatura = Kuyumcu.Api.Modules.Fatura;
using MIrsaliye = Kuyumcu.Api.Modules.Irsaliye;
using MSatinalma = Kuyumcu.Api.Modules.Satinalma;
using MSatis = Kuyumcu.Api.Modules.Satis;
using MSiparis = Kuyumcu.Api.Modules.Siparis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-26 — TEDARİK ZİNCİRİ + MUHASEBE KÖPRÜSÜ + ÖZEL MATRAH
//
// Sınanan çekirdek (her iş kuralı için hatayı yeniden üreten test):
//   Y-34 mal kabul stoğa HareketFabrikasi üzerinden yazar; ikinci aritmetik yok
//   Y-35 irsaliye faturaya bağlanır, bağsız irsaliye raporda GÖRÜNÜR
//   Y-37 muhasebe fişi bilinmeyen hesap kodunu BOŞ bırakır, UYDURMAZ
//   Y-30 tamir kaporası GERÇEK tahsilat hareketi üretir
//   Y-36 ustaya metal çıkışı YAZILMAZ (K-13 kapsam sınırı), kolon yalnız kimlik
//   Y-40 özel matrah kodu VARSAYILAN KAPALI (K-10 teyidi yok) + KAYNAK BEYANI zorunlu
// ═════════════════════════════════════════════════════════════════════════════

// ── Saf testler: veritabanı yok ──────────────────────────────────────────────
public sealed class P26SafTestleri
{
    [Fact]
    public void Y40_varsayilan_KAPALI_dogar_oneri_kod_yalnizca_GIB_araliginda()
    {
        // PLAN:578/778 — K-10 teyidi gelmeden anahtar AÇILMAZ.
        var a = new MFatura.KdvAyarlari();
        Assert.Null(a.KodYaDaNull);
        Assert.False(a.KodAralikta);
        Assert.Equal(0.20m, a.VarsayilanOran);

        // Öneri kod kodda DURUR ama hiçbir yere atanmaz; aralığı geçerlidir.
        Assert.Equal("805", MFatura.KdvAyarlari.OneriKod);
        Assert.True(new MFatura.KdvAyarlari
        {
            OzelMatrahKodu = MFatura.KdvAyarlari.OneriKod,
        }.KodAralikta);
    }

    [Fact]
    public void Y40_kod_beyansiz_basilamaz_kaynak_beyani_karari_ve_sinirini_soyler()
    {
        // KAPALI kurulum: "HARİÇ" beyanı + sebep (K-10) + açma yolu.
        var kapali = new MFatura.KdvAyarlari().KaynakBeyani;
        Assert.Contains("KAPALI", kapali);
        Assert.Contains("HARİÇ", kapali);
        Assert.Contains("23/e", kapali);
        Assert.Contains("K-10", kapali);
        Assert.DoesNotContain("BASILIR", kapali);

        // AÇIK kurulum: kod + kaynak + Anayasa md.1 sınırı aynı yerde.
        var acik = new MFatura.KdvAyarlari
        {
            OzelMatrahKodu = MFatura.KdvAyarlari.OneriKod,
        }.KaynakBeyani;
        Assert.Contains("805", acik);
        Assert.Contains("23/e", acik);
        Assert.Contains("mali müşavir imzası DEĞİLDİR", acik);
    }

    [Fact]
    public void Y40_kod_bosa_cekilirse_kapanir_ve_beyan_bunu_soyler()
    {
        var a = new MFatura.KdvAyarlari { OzelMatrahKodu = "   " };
        Assert.Null(a.KodYaDaNull);
        Assert.False(a.KodAralikta);
        Assert.Contains("KAPALI", a.KaynakBeyani);
    }

    [Fact]
    public void Y40_aralik_disi_kod_aralikta_sayilmaz()
    {
        Assert.False(new MFatura.KdvAyarlari { OzelMatrahKodu = "999" }.KodAralikta);
        Assert.False(new MFatura.KdvAyarlari { OzelMatrahKodu = "abc" }.KodAralikta);
    }

    [Fact]
    public void Tedarik_numarasi_onek_yil_ve_bes_hane_bicimindedir()
    {
        Assert.Equal("SAS-2026-00001", MSatinalma.TedarikNo.Bicimle(MSatinalma.TedarikNo.SatinalmaOnek, 2026, 1));
        Assert.Equal("IRS-2026-00042", MSatinalma.TedarikNo.Bicimle(MSatinalma.TedarikNo.IrsaliyeOnek, 2026, 42));
        Assert.Equal("SAS-2026-00002", MSatinalma.TedarikNo.Artir("SAS-2026-00001"));
    }

    [Fact]
    public void Satinalma_durumu_kapali_listedir_ve_kabul_yalniz_iki_durumda()
    {
        Assert.True(SatinalmaDurumu.Gecerli("ONAYLANDI"));
        Assert.False(SatinalmaDurumu.Gecerli("YOLDA"));
        Assert.True(SatinalmaDurumu.KabulEdilebilir(SatinalmaDurumu.Onaylandi));
        Assert.True(SatinalmaDurumu.KabulEdilebilir(SatinalmaDurumu.KismiKabul));
        Assert.False(SatinalmaDurumu.KabulEdilebilir(SatinalmaDurumu.Taslak));
        Assert.False(SatinalmaDurumu.KabulEdilebilir(SatinalmaDurumu.Kapandi));
    }
}

// ── Veritabanı testleri ──────────────────────────────────────────────────────
[Collection("veritabani")]
public sealed class P26TedarikTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));

    private static MSatinalma.SatinalmaServisi Satinalma(KuyumcuDbContext db) =>
        new(db, TestKurulum.Denetim(db), Satis(db),
            NullLogger<MSatinalma.SatinalmaServisi>.Instance);

    private static MIrsaliye.IrsaliyeServisi Irsaliye(KuyumcuDbContext db) =>
        new(db, TestKurulum.Denetim(db), NullLogger<MIrsaliye.IrsaliyeServisi>.Instance);

    private static MSiparis.SiparisServisi Siparis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSiparis.SiparisServisi>.Instance, Cari(db));

    private static JsonElement J(string json) => JsonDocument.Parse(json).RootElement;

    private static string Kimlikten(object yanit, string alan)
    {
        var dugum = JsonSerializer.SerializeToNode(
            yanit, yanit.GetType(), new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        return dugum[alan]!.GetValue<string>();
    }

    private async Task<(Kiraci k, string siparisId)> SiparisKur(
        KuyumcuDbContext db, string ad, decimal adet = 5m, decimal fiyat = 1_000m)
    {
        Kiraci k;
        using (var kur = vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(kur, ad);
            await kur.SaveChangesAsync();
        }

        var sonuc = await Satinalma(db).Olustur(new MSatinalma.SatinalmaOlusturDto
        {
            TedarikciCariId = k.CariId,
            SubeId = k.SubeId,
            Aciklama = "22 ayar bilezik siparişi",
            Kalemler = [new MSatinalma.SatinalmaKalemDto
            {
                UrunId = k.UrunId, Adet = adet, BirimFiyatTl = fiyat,
            }],
        }, k.Islem);

        return (k, Kimlikten(sonuc, "id"));
    }

    // ═══ Y-34 — SATINALMA SİPARİŞİ ══════════════════════════════════════════

    [Fact]
    public async Task Y34_siparis_TASLAK_dogar_ve_numarasi_kiraci_icinde_tekildir()
    {
        using var db = vt.OlusturSistem();
        var (k, id) = await SiparisKur(db, "PAKETP26-TEST-Y34-A");

        var s = await db.SatinalmaSiparisleri.AsNoTracking().FirstAsync(x => x.Id == id);
        Assert.Equal(SatinalmaDurumu.Taslak, s.Durum);
        Assert.StartsWith("SAS-", s.No);
        Assert.Equal(k.SubeId, s.SubeId);
        Assert.Single(await db.SatinalmaKalemleri.Where(x => x.SatinalmaSiparisId == id).ToListAsync());
    }

    [Fact]
    public async Task Y34_PATCH_gonderilmeyen_alani_korur_null_olani_siler()
    {
        using var db = vt.OlusturSistem();
        var (k, id) = await SiparisKur(db, "PAKETP26-TEST-Y34-B");

        // hesapKodu yazılır, açıklama GÖNDERİLMEZ → korunmalı
        await Satinalma(db).Guncelle(id, J("""{"hesapKodu":"320.01.0001"}"""), k.Islem);
        var s1 = await db.SatinalmaSiparisleri.AsNoTracking().FirstAsync(x => x.Id == id);
        Assert.Equal("320.01.0001", s1.HesapKodu);
        Assert.Equal("22 ayar bilezik siparişi", s1.Aciklama);

        // açıklama null → SİLİNMELİ, hesapKodu gönderilmedi → korunmalı
        await Satinalma(db).Guncelle(id, J("""{"aciklama":null}"""), k.Islem);
        var s2 = await db.SatinalmaSiparisleri.AsNoTracking().FirstAsync(x => x.Id == id);
        Assert.Null(s2.Aciklama);
        Assert.Equal("320.01.0001", s2.HesapKodu);
    }

    [Fact]
    public async Task Y34_onaylanmis_siparis_duzenlenemez_ve_sebebi_yazilidir()
    {
        using var db = vt.OlusturSistem();
        var (k, id) = await SiparisKur(db, "PAKETP26-TEST-Y34-C");
        await Satinalma(db).Onayla(id, k.Islem);

        var hata = await Assert.ThrowsAsync<ConflictException>(
            () => Satinalma(db).Guncelle(id, J("""{"aciklama":"x"}"""), k.Islem));
        Assert.Contains("TASLAK", hata.Message);
    }

    [Fact]
    public async Task Y34_iptal_gerekcesi_kisa_olamaz_ve_kayit_SILINMEZ()
    {
        using var db = vt.OlusturSistem();
        var (k, id) = await SiparisKur(db, "PAKETP26-TEST-Y34-D");

        await Assert.ThrowsAsync<BadRequestException>(
            () => Satinalma(db).Iptal(id, "kisa", k.Islem));

        await Satinalma(db).Iptal(id, "tedarikçi malı gönderemedi", k.Islem);
        var s = await db.SatinalmaSiparisleri.AsNoTracking().FirstAsync(x => x.Id == id);
        Assert.Equal(SatinalmaDurumu.Iptal, s.Durum);          // hard DELETE yok
        Assert.Equal("tedarikçi malı gönderemedi", s.IptalGerekce);
    }

    [Fact]
    public async Task Y34_mal_kabul_STOGA_HareketFabrikasi_uzerinden_yazar_ve_irsaliye_hareketine_baglanir()
    {
        using var db = vt.OlusturSistem();
        var (k, id) = await SiparisKur(db, "PAKETP26-TEST-Y34-E", adet: 4m, fiyat: 2_500m);
        await Satinalma(db).Onayla(id, k.Islem);

        var oncekiStok = (await db.Stoklar.AsNoTracking()
            .FirstAsync(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId)).Adet;
        var kalemId = (await db.SatinalmaKalemleri.AsNoTracking()
            .FirstAsync(x => x.SatinalmaSiparisId == id)).Id;

        var sonuc = await Satinalma(db).MalKabul(id, new MSatinalma.MalKabulDto
        {
            KurSnapshotId = k.KurId,
            Kalemler = [new MSatinalma.MalKabulKalemDto { KalemId = kalemId, Adet = 3m }],
        }, k.Islem);
        var irsaliyeId = Kimlikten(sonuc, "irsaliyeId");

        // (1) STOK arttı — SatisServisi.Alis yolundan
        var yeniStok = (await db.Stoklar.AsNoTracking()
            .FirstAsync(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId)).Adet;
        Assert.Equal(oncekiStok + 3m, yeniStok);

        // (2) DEFTERE ALIS hareketi düştü (tek yazma yolu)
        var alis = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "ALIS")
            .OrderByDescending(h => h.Olusturuldu).FirstAsync();
        Assert.Equal(7_500m, Math.Abs(alis.TlTutari));

        // (3) İrsaliye kalemi O hareketa BAĞLANDI — bağsız kalmadı
        var irsKalem = await db.IrsaliyeKalemleri.AsNoTracking()
            .FirstAsync(x => x.IrsaliyeId == irsaliyeId);
        Assert.Equal(alis.Id, irsKalem.HareketId);

        // (4) Kısmi kabul → durum KISMI_KABUL, kabul adedi yazıldı
        var s = await db.SatinalmaSiparisleri.AsNoTracking().FirstAsync(x => x.Id == id);
        Assert.Equal(SatinalmaDurumu.KismiKabul, s.Durum);
        Assert.Equal(3m, (await db.SatinalmaKalemleri.AsNoTracking().FirstAsync(x => x.Id == kalemId)).KabulEdilenAdet);
    }

    [Fact]
    public async Task Y34_mal_kabul_siparis_adedini_ASAMAZ()
    {
        using var db = vt.OlusturSistem();
        var (k, id) = await SiparisKur(db, "PAKETP26-TEST-Y34-F", adet: 2m);
        await Satinalma(db).Onayla(id, k.Islem);
        var kalemId = (await db.SatinalmaKalemleri.AsNoTracking()
            .FirstAsync(x => x.SatinalmaSiparisId == id)).Id;

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satinalma(db).MalKabul(id, new MSatinalma.MalKabulDto
            {
                KurSnapshotId = k.KurId,
                Kalemler = [new MSatinalma.MalKabulKalemDto { KalemId = kalemId, Adet = 3m }],
            }, k.Islem));
        Assert.Contains("aşıyor", hata.Message);

        // FAIL-CLOSED: reddedilen kabulden ne stok ne irsaliye doğdu
        Assert.Empty(await db.Irsaliyeler.Where(i => i.TenantId == k.TenantId).ToListAsync());
    }

    [Fact]
    public async Task Y34_tam_kabulde_siparis_KAPANIR_ve_yeni_kabul_reddedilir()
    {
        using var db = vt.OlusturSistem();
        var (k, id) = await SiparisKur(db, "PAKETP26-TEST-Y34-G", adet: 2m);
        await Satinalma(db).Onayla(id, k.Islem);
        var kalemId = (await db.SatinalmaKalemleri.AsNoTracking()
            .FirstAsync(x => x.SatinalmaSiparisId == id)).Id;

        await Satinalma(db).MalKabul(id, new MSatinalma.MalKabulDto
        {
            KurSnapshotId = k.KurId,
            Kalemler = [new MSatinalma.MalKabulKalemDto { KalemId = kalemId, Adet = 2m }],
        }, k.Islem);

        Assert.Equal(SatinalmaDurumu.Kapandi,
            (await db.SatinalmaSiparisleri.AsNoTracking().FirstAsync(x => x.Id == id)).Durum);

        var hata = await Assert.ThrowsAsync<ConflictException>(
            () => Satinalma(db).MalKabul(id, new MSatinalma.MalKabulDto
            {
                KurSnapshotId = k.KurId,
                Kalemler = [new MSatinalma.MalKabulKalemDto { KalemId = kalemId, Adet = 1m }],
            }, k.Islem));
        Assert.Contains("ONAYLANDI", hata.Message);
    }

    [Fact]
    public async Task Y34_mal_kabulu_yapilmis_siparis_IPTAL_EDILEMEZ()
    {
        using var db = vt.OlusturSistem();
        var (k, id) = await SiparisKur(db, "PAKETP26-TEST-Y34-H", adet: 2m);
        await Satinalma(db).Onayla(id, k.Islem);
        var kalemId = (await db.SatinalmaKalemleri.AsNoTracking()
            .FirstAsync(x => x.SatinalmaSiparisId == id)).Id;
        await Satinalma(db).MalKabul(id, new MSatinalma.MalKabulDto
        {
            KurSnapshotId = k.KurId,
            Kalemler = [new MSatinalma.MalKabulKalemDto { KalemId = kalemId, Adet = 1m }],
        }, k.Islem);

        var hata = await Assert.ThrowsAsync<ConflictException>(
            () => Satinalma(db).Iptal(id, "vazgeçtim bu maldan", k.Islem));
        Assert.Contains("İADE", hata.Message.ToUpperInvariant());
    }

    [Fact]
    public async Task Y34_baska_kiracinin_siparisi_GORULMEZ()
    {
        Kiraci a, b;
        using (var kur = vt.OlusturSistem())
        {
            a = TestKurulum.KiraciKur(kur, "PAKETP26-TEST-IZOLASYON-A");
            b = TestKurulum.KiraciKur(kur, "PAKETP26-TEST-IZOLASYON-B");
            await kur.SaveChangesAsync();
        }

        string id;
        using (var dbA = vt.Olustur(a))
            id = Kimlikten(await Satinalma(dbA).Olustur(new MSatinalma.SatinalmaOlusturDto
            {
                TedarikciCariId = a.CariId, SubeId = a.SubeId,
                Kalemler = [new MSatinalma.SatinalmaKalemDto { UrunId = a.UrunId, Adet = 1m, BirimFiyatTl = 10m }],
            }, a.Islem), "id");

        using var dbB = vt.Olustur(b);
        await Assert.ThrowsAsync<NotFoundException>(() => Satinalma(dbB).Detay(id, b.Islem));
    }

    // ═══ Y-35 — İRSALİYE ════════════════════════════════════════════════════

    [Fact]
    public async Task Y35_GELEN_irsaliye_elle_ACILAMAZ_mal_kabulden_dogar()
    {
        Kiraci k;
        using (var kur = vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(kur, "PAKETP26-TEST-Y35-A");
            await kur.SaveChangesAsync();
        }
        using var db = vt.OlusturSistem();

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Irsaliye(db).Olustur(new MIrsaliye.IrsaliyeOlusturDto
            {
                Yon = IrsaliyeYonu.Gelen, CariId = k.CariId, SubeId = k.SubeId,
                Kalemler = [new MIrsaliye.IrsaliyeKalemDto { UrunId = k.UrunId, Adet = 1m }],
            }, k.Islem));
        Assert.Contains("mal-kabul", hata.Message);
    }

    [Fact]
    public async Task Y35_bagsiz_irsaliye_RAPORDA_GORUNUR_faturaya_baglaninca_cikar()
    {
        Kiraci k;
        using (var kur = vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(kur, "PAKETP26-TEST-Y35-B");
            await kur.SaveChangesAsync();
        }
        using var db = vt.Olustur(k);

        var irsaliye = await Irsaliye(db).Olustur(new MIrsaliye.IrsaliyeOlusturDto
        {
            Yon = IrsaliyeYonu.Giden, CariId = k.CariId, SubeId = k.SubeId,
            Kalemler = [new MIrsaliye.IrsaliyeKalemDto { UrunId = k.UrunId, Adet = 2m }],
        }, k.Islem);
        var irsaliyeId = Kimlikten(irsaliye, "id");

        var rapor = JsonSerializer.SerializeToNode(await Irsaliye(db).Bagsiz(null, k.Islem))!.AsObject();
        Assert.Equal(1, rapor["toplam"]!.GetValue<int>());

        // Fatura kes → bağla → rapordan ÇIKMALI
        var faturaId = Kimlik.Ulid();
        db.Faturalar.Add(new Fatura
        {
            Id = faturaId, TenantId = k.TenantId, CariId = k.CariId, FaturaNo = "F-1",
            Tur = "SATIS", KdvTuru = "STANDART", Durum = "KESILDI",
            AraToplam = 100m, KdvOran = 0.20m, KdvTutar = 20m, Toplam = 120m,
            Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await Irsaliye(db).FaturayaBagla(irsaliyeId, faturaId, k.Islem);
        var rapor2 = JsonSerializer.SerializeToNode(await Irsaliye(db).Bagsiz(null, k.Islem))!.AsObject();
        Assert.Equal(0, rapor2["toplam"]!.GetValue<int>());
        Assert.Equal(IrsaliyeDurumu.Faturalandi,
            (await db.Irsaliyeler.AsNoTracking().FirstAsync(i => i.Id == irsaliyeId)).Durum);

        // GERİ DÖNÜŞ YOLU (UX-S25): bağ kaldırılınca ACIK'a döner
        await Irsaliye(db).BagiKaldir(irsaliyeId, k.Islem);
        var rapor3 = JsonSerializer.SerializeToNode(await Irsaliye(db).Bagsiz(null, k.Islem))!.AsObject();
        Assert.Equal(1, rapor3["toplam"]!.GetValue<int>());
    }

    [Fact]
    public async Task Y35_baska_carinin_faturasina_baglanamaz()
    {
        Kiraci k;
        using (var kur = vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(kur, "PAKETP26-TEST-Y35-C");
            await kur.SaveChangesAsync();
        }
        using var db = vt.Olustur(k);

        var irsaliyeId = Kimlikten(await Irsaliye(db).Olustur(new MIrsaliye.IrsaliyeOlusturDto
        {
            Yon = IrsaliyeYonu.Giden, CariId = k.CariId, SubeId = k.SubeId,
            Kalemler = [new MIrsaliye.IrsaliyeKalemDto { Tanim = "sevk", Adet = 1m }],
        }, k.Islem), "id");

        var baskaCari = Kimlik.Ulid();
        db.Cariler.Add(new Cari
        {
            Id = baskaCari, TenantId = k.TenantId, Ad = "Başka", Tur = "MUSTERI", Aktif = true,
            Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        var faturaId = Kimlik.Ulid();
        db.Faturalar.Add(new Fatura
        {
            Id = faturaId, TenantId = k.TenantId, CariId = baskaCari, FaturaNo = "F-2",
            Tur = "SATIS", KdvTuru = "STANDART", Durum = "KESILDI",
            AraToplam = 10m, KdvOran = 0.20m, KdvTutar = 2m, Toplam = 12m,
            Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Irsaliye(db).FaturayaBagla(irsaliyeId, faturaId, k.Islem));
        Assert.Contains("başka bir cariye", hata.Message);
    }

    [Fact]
    public async Task Y35_deftere_bagli_irsaliye_IPTAL_EDILEMEZ_stok_geri_gelmez()
    {
        using var db = vt.OlusturSistem();
        var (k, id) = await SiparisKur(db, "PAKETP26-TEST-Y35-D", adet: 1m);
        await Satinalma(db).Onayla(id, k.Islem);
        var kalemId = (await db.SatinalmaKalemleri.AsNoTracking()
            .FirstAsync(x => x.SatinalmaSiparisId == id)).Id;
        var sonuc = await Satinalma(db).MalKabul(id, new MSatinalma.MalKabulDto
        {
            KurSnapshotId = k.KurId,
            Kalemler = [new MSatinalma.MalKabulKalemDto { KalemId = kalemId, Adet = 1m }],
        }, k.Islem);

        var hata = await Assert.ThrowsAsync<ConflictException>(
            () => Irsaliye(db).Iptal(Kimlikten(sonuc, "irsaliyeId"), "yanlış mal geldi", k.Islem));
        Assert.Contains("storno", hata.Message.ToLowerInvariant());
    }

    // ═══ Y-30 — TAMİR KAPORASI ══════════════════════════════════════════════

    [Fact]
    public async Task Y30_tamir_kaporasi_GERCEK_tahsilat_hareketi_uretir()
    {
        Kiraci k;
        using (var kur = vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(kur, "PAKETP26-TEST-Y30-A");
            await kur.SaveChangesAsync();
        }
        using var db = vt.OlusturSistem();

        var siparisId = Kimlikten(await Siparis(db).Olustur(new MSiparis.SiparisOlusturDto
        {
            Tur = SiparisTuru.Tamir, CariId = k.CariId, SubeId = k.SubeId,
            Aciklama = "kolye tamiri",
            Kalemler = [new MSiparis.SiparisKalemDto { Tanim = "kolye", Adet = 1m }],
        }, k.Islem), "id");

        var oncekiTahsilat = await db.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT");

        await Siparis(db).Kapora(siparisId, new MSiparis.SiparisKaporaDto
        {
            TlTutari = 500m, OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        var s = await db.Siparisler.AsNoTracking().FirstAsync(x => x.Id == siparisId);
        Assert.NotNull(s.KaporaHareketId);

        // Kapora sipariş tablosunda bir "tutar alanı" DEĞİL, DEFTER satırıdır.
        var h = await db.Hareketler.AsNoTracking().FirstAsync(x => x.Id == s.KaporaHareketId);
        Assert.Equal("TAHSILAT", h.Tur);
        Assert.Equal(500m, Math.Abs(h.TlTutari));
        Assert.Equal(oncekiTahsilat + 1,
            await db.Hareketler.CountAsync(x => x.TenantId == k.TenantId && x.Tur == "TAHSILAT"));
    }

    [Fact]
    public async Task Y30_ikinci_kapora_REDDEDILIR_ve_duzeltme_yolu_yazilidir()
    {
        Kiraci k;
        using (var kur = vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(kur, "PAKETP26-TEST-Y30-B");
            await kur.SaveChangesAsync();
        }
        using var db = vt.OlusturSistem();

        var siparisId = Kimlikten(await Siparis(db).Olustur(new MSiparis.SiparisOlusturDto
        {
            Tur = SiparisTuru.Tamir, CariId = k.CariId, SubeId = k.SubeId,
            Kalemler = [new MSiparis.SiparisKalemDto { Tanim = "yüzük", Adet = 1m }],
        }, k.Islem), "id");

        var kapora = new MSiparis.SiparisKaporaDto
        {
            TlTutari = 100m, OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        };
        await Siparis(db).Kapora(siparisId, kapora, k.Islem);

        var hata = await Assert.ThrowsAsync<ConflictException>(
            () => Siparis(db).Kapora(siparisId, kapora, k.Islem));
        Assert.Contains("geri ver", hata.Message.ToLowerInvariant());
    }

    // ═══ Y-36 — USTAYA METAL ÇIKIŞI (K-13 SINIRI) ═══════════════════════════

    [Fact]
    public async Task Y36_usta_carisi_kaydedilir_ama_METAL_CIKISI_YAZILMAZ()
    {
        Kiraci k;
        using (var kur = vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(kur, "PAKETP26-TEST-Y36-A");
            await kur.SaveChangesAsync();
        }
        using var db = vt.OlusturSistem();

        var oncekiHareket = await db.Hareketler.CountAsync(h => h.TenantId == k.TenantId);

        var siparisId = Kimlikten(await Siparis(db).Olustur(new MSiparis.SiparisOlusturDto
        {
            Tur = SiparisTuru.AtolyeSiparisi, CariId = k.CariId, SubeId = k.SubeId,
            UstaCariId = k.CariId,
            Kalemler = [new MSiparis.SiparisKalemDto { Tanim = "bilezik", Adet = 1m }],
        }, k.Islem), "id");

        var s = await db.Siparisler.AsNoTracking().FirstAsync(x => x.Id == siparisId);
        Assert.Equal(k.CariId, s.UstaCariId);

        // K-13: imalat kapsam DIŞI — hiçbir metal/defter satırı doğmadı.
        Assert.Equal(oncekiHareket, await db.Hareketler.CountAsync(h => h.TenantId == k.TenantId));

        // ve ekran bunun SEBEBİNİ söylüyor (beyansız kapsam sınırı olmaz).
        var detay = JsonSerializer.SerializeToNode(await Siparis(db).Detay(siparisId, k.Islem))!.AsObject();
        Assert.Contains("imalat", detay["ustaMetalCikisiBeyani"]!.GetValue<string>());
    }

    [Fact]
    public async Task Y36_usta_carisi_MUSTERI_siparisine_baglanamaz()
    {
        Kiraci k;
        using (var kur = vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(kur, "PAKETP26-TEST-Y36-B");
            await kur.SaveChangesAsync();
        }
        using var db = vt.OlusturSistem();

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Siparis(db).Olustur(new MSiparis.SiparisOlusturDto
            {
                Tur = SiparisTuru.MusteriSiparisi, CariId = k.CariId, SubeId = k.SubeId,
                UstaCariId = k.CariId,
                Kalemler = [new MSiparis.SiparisKalemDto { Tanim = "küpe", Adet = 1m }],
            }, k.Islem));
        Assert.Contains("ATOLYE_SIPARISI", hata.Message);
    }

    // ═══ Y-37 — MUHASEBE FİŞİ ═══════════════════════════════════════════════

    [Fact]
    public async Task Y37_muhasebe_fisi_bilinmeyen_hesap_kodunu_BOS_birakir_UYDURMAZ()
    {
        Kiraci k;
        using (var kur = vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(kur, "PAKETP26-TEST-Y37-A");
            await kur.SaveChangesAsync();
        }
        using var db = vt.OlusturSistem();

        db.Faturalar.Add(new Fatura
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, CariId = k.CariId, FaturaNo = "F-100",
            Tur = "SATIS", KdvTuru = "KUYUM", Durum = "KESILDI", OzelMatrahKodu = null,
            HasDeger = 1_000m, AraToplam = 200m, KdvOran = 0.20m, KdvTutar = 40m, Toplam = 1_240m,
            Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var kaynak = Kuyumcu.Api.Modules.Disaaktarim.Kaynaklar.Bul("muhasebe-fisi")!;
        var basliklar = kaynak.Basliklar;
        var hesapIdx = Array.IndexOf(basliklar, "hesapKodu");
        var eksikIdx = Array.IndexOf(basliklar, "eksikAlan");

        var satirlar = new List<string[]>();
        await foreach (var r in kaynak.Satirlar(db, k.TenantId, maskeli: false)) satirlar.Add(r);

        var satir = Assert.Single(satirlar);
        Assert.Equal("", satir[hesapIdx]);                       // ⚠ BOŞ — "120" uydurulmadı
        Assert.Contains("hesapKodu", satir[eksikIdx]);
        Assert.Contains("ozelMatrahKodu", satir[eksikIdx]);      // KUYUM ama kod yok
    }

    [Fact]
    public async Task Y37_hesap_kodu_tanimliysa_fise_YAZILIR_ve_eksik_listesinden_cikar()
    {
        Kiraci k;
        using (var kur = vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(kur, "PAKETP26-TEST-Y37-B");
            await kur.SaveChangesAsync();
        }
        using var db = vt.OlusturSistem();

        (await db.Cariler.FirstAsync(c => c.Id == k.CariId)).HesapKodu = "120.01.0007";
        db.Faturalar.Add(new Fatura
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, CariId = k.CariId, FaturaNo = "F-101",
            Tur = "SATIS", KdvTuru = "KUYUM", Durum = "KESILDI", OzelMatrahKodu = "805",
            HasDeger = 1_000m, AraToplam = 200m, KdvOran = 0.20m, KdvTutar = 40m, Toplam = 1_240m,
            Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var kaynak = Kuyumcu.Api.Modules.Disaaktarim.Kaynaklar.Bul("muhasebe-fisi")!;
        var hesapIdx = Array.IndexOf(kaynak.Basliklar, "hesapKodu");
        var kodIdx = Array.IndexOf(kaynak.Basliklar, "ozelMatrahKodu");
        var eksikIdx = Array.IndexOf(kaynak.Basliklar, "eksikAlan");

        var satirlar = new List<string[]>();
        await foreach (var r in kaynak.Satirlar(db, k.TenantId, maskeli: false)) satirlar.Add(r);

        var satir = Assert.Single(satirlar);
        Assert.Equal("120.01.0007", satir[hesapIdx]);
        Assert.Equal("805", satir[kodIdx]);
        Assert.Equal("", satir[eksikIdx]);
    }

    [Fact]
    public async Task Y37_cari_hesap_kodu_OLUSTURMADA_yazilir_PATCHte_guncellenir_ve_SILINEBILIR()
    {
        Kiraci k;
        using (var kur = vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(kur, "PAKETP26-TEST-Y37-D");
            await kur.SaveChangesAsync();
        }
        using var db = vt.Olustur(k);

        // UX-S26: alan OLUŞTURMA formunda da vardır — "sonra düzenle" ertelemesi yok.
        var olusan = JsonSerializer.SerializeToNode(await Cari(db).Olustur(new MCari.CariOlusturDto
        {
            Tur = "TEDARIKCI", Ad = "P26 Tedarikçi", HesapKodu = " 320.01.0026 ",
        }, k.Islem))!.AsObject();
        var cariId = olusan["id"]!.GetValue<string>();
        Assert.Equal("320.01.0026", olusan["hesapKodu"]!.GetValue<string>());   // kırpılarak

        // PATCH: gönderilen değer yazılır.
        await Cari(db).Guncelle(cariId, new MCari.CariGuncelleDto { HesapKodu = "320.02.0099" }, k.Islem);
        Assert.Equal("320.02.0099",
            (await db.Cariler.AsNoTracking().FirstAsync(c => c.Id == cariId)).HesapKodu);

        // PATCH: gönderilMEyen alan KORUNUR (üç durumlu sözleşme).
        await Cari(db).Guncelle(cariId, new MCari.CariGuncelleDto { Ad = "P26 Tedarikçi 2" }, k.Islem);
        Assert.Equal("320.02.0099",
            (await db.Cariler.AsNoTracking().FirstAsync(c => c.Id == cariId)).HesapKodu);

        // PATCH: boş dizge = SİL (mali müşavir kodu kaldırabilmeli).
        await Cari(db).Guncelle(cariId, new MCari.CariGuncelleDto { HesapKodu = "" }, k.Islem);
        Assert.Null((await db.Cariler.AsNoTracking().FirstAsync(c => c.Id == cariId)).HesapKodu);
    }

    [Fact]
    public async Task Y37_TASLAK_fatura_muhasebe_fisine_GIRMEZ()
    {
        Kiraci k;
        using (var kur = vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(kur, "PAKETP26-TEST-Y37-C");
            await kur.SaveChangesAsync();
        }
        using var db = vt.OlusturSistem();

        db.Faturalar.Add(new Fatura
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, CariId = k.CariId, FaturaNo = null,
            Tur = "SATIS", KdvTuru = "STANDART", Durum = "TASLAK",
            AraToplam = 10m, KdvOran = 0.20m, KdvTutar = 2m, Toplam = 12m,
            Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var kaynak = Kuyumcu.Api.Modules.Disaaktarim.Kaynaklar.Bul("muhasebe-fisi")!;
        var satirlar = new List<string[]>();
        await foreach (var r in kaynak.Satirlar(db, k.TenantId, maskeli: false)) satirlar.Add(r);
        Assert.Empty(satirlar);
    }

    [Fact]
    public void Y37_tedarik_ve_muhasebe_kaynaklari_disa_aktarim_katalogunda()
    {
        var adlar = Kuyumcu.Api.Modules.Disaaktarim.Kaynaklar.Adlar;
        Assert.Contains("satinalma", adlar);
        Assert.Contains("irsaliye", adlar);
        Assert.Contains("muhasebe-fisi", adlar);
    }

    // ═══ Y-40 — ÖZEL MATRAH BEYANI (sunucu ucu) ═════════════════════════════

    // PLAN-TEK-SEFER-2026-09-18.md:578 + :778 — K-10 teyidi GELMEDEN anahtar
    // AÇILMAZ. Varsayılan kurulum KAPALI doğar ve ürün "HARİÇ" beyanı yazar.
    [Fact]
    public void Y40_ayar_ucu_VARSAYILAN_KAPALI_dogar_ve_HARIC_beyani_yazar()
    {
        using var db = vt.OlusturSistem();
        var yanit = JsonSerializer.SerializeToNode(
            new MFatura.FaturaServisi(db).OzelMatrahAyari())!.AsObject();

        Assert.True(yanit["kod"] is null, "kapalı kurulumda kod NULL dönmeli");
        Assert.False(yanit["acik"]!.GetValue<bool>());
        Assert.False(yanit["maliMusavirTeyidi"]!.GetValue<bool>());
        var beyan = yanit["kaynakBeyani"]!.GetValue<string>();
        Assert.Contains("HARİÇ", beyan);
        Assert.Contains("K-10", beyan);
    }

    // Teyit geldiğinde: TEK SATIR yapılandırma (derleme yok) anahtarı açar.
    [Fact]
    public void Y40_teyit_gelince_TEK_SATIR_yapilandirmayla_acilir_ve_kaynagini_soyler()
    {
        using var db = vt.OlusturSistem();
        var ayar = new MFatura.KdvAyarlari { OzelMatrahKodu = MFatura.KdvAyarlari.OneriKod };
        var yanit = JsonSerializer.SerializeToNode(
            new MFatura.FaturaServisi(db, ayar).OzelMatrahAyari())!.AsObject();

        Assert.Equal("805", yanit["kod"]!.GetValue<string>());
        Assert.True(yanit["acik"]!.GetValue<bool>());
        Assert.True(yanit["aralikta"]!.GetValue<bool>());
        Assert.False(yanit["maliMusavirTeyidi"]!.GetValue<bool>());
        Assert.Contains("23/e", yanit["kaynakBeyani"]!.GetValue<string>());
    }

    // Kurulum dosyası da KAPALI sevk edilir: 805'i "sessizce" geri getiren bir
    // yapılandırma değişikliği bu testi KIRMIZI yapar (kapı kaydı zorunluluğu).
    [Fact]
    public void Y40_appsettings_ozel_matrah_kodu_BOS_sevk_edilir()
    {
        var kok = MutlakKok();
        var metin = File.ReadAllText(Path.Combine(kok, "Kuyumcu.Api", "appsettings.json"));
        using var belge = System.Text.Json.JsonDocument.Parse(metin);
        var kod = belge.RootElement.GetProperty("Kdv").GetProperty("OzelMatrahKodu").GetString();

        Assert.True(string.IsNullOrWhiteSpace(kod),
            "K-10 teyidi gelmeden appsettings'te özel matrah kodu dolu olamaz (PLAN:578/778).");
    }

    private static string MutlakKok()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }
}
