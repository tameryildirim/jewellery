using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using MFatura = Kuyumcu.Api.Modules.Fatura;
using Maden = Kuyumcu.Core.Domain.Maden;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ZO-BELGE (30.09 · orkestratör 07:00) — kalem.HasGrami TEK ANLAM [DB]
//
// Kök neden: fatura kaleminin has gramı ürün yollarında BİRİM (ürün kartı), fişten
// yolunda SATIR TOPLAMI yazılıyordu; başlık has gramı (Σ kalem.HasGrami) ve taslak
// miktar düzeltmesi bu iki anlamı karıştırıyordu (2 adetlik satırda başlık 9,16).
// Karar: kalem.HasGrami = SATIRIN TOPLAM has gramı, HER yolda. Beklenenler ELLE:
// kur 6.827,20 TL/has g; test ürünü 9,16 has g/adet (22 ayar 10 g).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class ZOBelgeKalemHasGramiTestleri(TestVeritabani vt)
{
    private static System.Text.Json.JsonElement G(string j) => System.Text.Json.JsonDocument.Parse(j).RootElement;

    private static Hareket AltinSatis(Kiraci k, string fisId, decimal tl, decimal hasToplam, decimal adet)
    {
        var now = DateTime.UtcNow;
        return new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = "SATIS", Tarih = now, TlTutari = tl, HasGrami = -hasToplam, Maden = Maden.Altin,
            OdemeTuru = "HAVALE", NakitTutar = 0m, VeresiyeTutar = 0m, Adet = adet,
            FisId = fisId, KurSnapshotId = k.KurId, CariId = k.CariId,
            UrunId = k.UrunId, KullaniciId = k.KullaniciId, Olusturuldu = now,
        };
    }

    [Fact]
    public async Task ZOBHG01_kalemli_yolda_2_adet_kalem_has_grami_SATIR_TOPLAMI_18_32()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOBHG01");
        kurulum.SaveChanges();
        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 200_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 2m, BirimFiyat = 100_000m }],
        }, k.Islem);
        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(18.32m, kalem.HasGrami);              // 2 × 9,16
        Assert.Equal(125_074.30m, kalem.HasDeger);         // 18,32 × 6.827,20 (çift çarpım YOK)
        Assert.Equal(18.32m, kontrol.Faturalar.Single(x => x.TenantId == k.TenantId).HasGram);
    }

    [Fact]
    public async Task ZOBHG02_taslaga_2_adet_urun_eklenince_kalem_has_grami_toplam_baslik_toplanir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOBHG02");
        kurulum.SaveChanges();
        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 100_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 1m, BirimFiyat = 100_000m }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        await servis.TaslakKalemEkle(fid,
            new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 2m, BirimFiyat = 100_000m }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var eklenen = kontrol.FaturaKalemleri.Single(x => x.FaturaId == fid && x.Sira == 2);
        Assert.Equal(18.32m, eklenen.HasGrami);
        Assert.Equal(125_074.30m, eklenen.HasDeger);
        Assert.Equal(27.48m, kontrol.Faturalar.Single(x => x.Id == fid).HasGram);   // 9,16 + 18,32
    }

    [Fact]
    public async Task ZOBHG03_fisten_kalemde_MIKTAR_400_fiyat_serbest_urun_kaleminde_2den_3e_KUR_KAYDIYLA_187_611_46()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOBHG03");
        var fisId = Kimlik.Ulid();
        kurulum.Hareketler.Add(AltinSatis(k, fisId, 200_000m, 18.32m, 2m));
        kurulum.SaveChanges();
        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId, KdvTuru = "KUYUM", KdvOran = 0.20m }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kid = db.FaturaKalemleri.Single(x => x.FaturaId == fid).Id;
        using (var once = vt.OlusturSistem())
        {
            var k0 = once.FaturaKalemleri.Single(x => x.Id == kid);
            Assert.Equal(2m, k0.Miktar);
            Assert.Equal(18.32m, k0.HasGrami);              // defterden: satır TOPLAMI
            Assert.Equal(125_074.30m, k0.HasDeger);
            Assert.Equal(6_827.20m, k0.HasBirimFiyat);       // dayanak = hareketin KUR KAYDI
        }

        // Yalnız fiyat düzeltmesi: has dayanağı AYNEN (125.074,30), has gram AYNEN.
        await servis.TaslakKalemGuncelle(fid, kid, G("{\"birimFiyat\":101000}"), k.Islem);
        using (var ara = vt.OlusturSistem())
        {
            var k1 = ara.FaturaKalemleri.Single(x => x.Id == kid);
            Assert.Equal(18.32m, k1.HasGrami);
            Assert.Equal(125_074.30m, k1.HasDeger);
            Assert.Equal(202_000.00m, k1.SatirToplam);
            Assert.Equal(18.32m, ara.Faturalar.Single(x => x.Id == fid).HasGram);
        }

        // Defter bağlı satırda ADET belgeden değiştirilemez (fatura ≠ satış kaydı olmasın).
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            servis.TaslakKalemGuncelle(fid, kid, G("{\"miktar\":3}"), k.Islem));
        Assert.Equal("Bu satır satış fişinden geldi; adedi fişten değiştirin.", hata.Message);
        using (var red = vt.OlusturSistem())
        {
            var k2 = red.FaturaKalemleri.Single(x => x.Id == kid);
            Assert.Equal(2m, k2.Miktar);
            Assert.Equal(18.32m, k2.HasGrami);
            Assert.Equal(125_074.30m, k2.HasDeger);
        }
        // Aynı adedi yeniden göndermek değişiklik değildir → reddedilmez.
        await servis.TaslakKalemGuncelle(fid, kid, G("{\"miktar\":2}"), k.Islem);

        // Ürün kaleminde 2 → 3: has bedeli = round2(27,48 × KUR KAYDI 6.827,20) = 187.611,456 → 187.611,46.
        // (Eski kod dayanağı yuvarlanmış bedelden geri çözüyordu: 125.074,30 ÷ 18,32 = 6.827,199782 → 187.611,45.)
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 200_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 2m, BirimFiyat = 100_000m }],
        }, k.Islem);
        var fid2 = db.Faturalar.Single(x => x.TenantId == k.TenantId && x.Id != fid).Id;
        var kid2 = db.FaturaKalemleri.Single(x => x.FaturaId == fid2).Id;
        await servis.TaslakKalemGuncelle(fid2, kid2, G("{\"miktar\":3}"), k.Islem);
        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.Id == kid2);
        Assert.Equal(27.48m, kalem.HasGrami);
        Assert.Equal(187_611.46m, kalem.HasDeger);
        var f = kontrol.Faturalar.Single(x => x.Id == fid2);
        Assert.Equal(27.48m, f.HasGram);
        Assert.Equal(187_611.46m, f.HasDeger);
        Assert.Equal(6_827.20m, Math.Round(f.HasBirimFiyat ?? 0m, 2));   // özet kur değişmez
    }

    [Fact]
    public async Task ZOBHG04_STANDART_kalemde_de_miktar_duzeltmesi_has_gramini_yeniden_hesaplar()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOBHG04");
        kurulum.SaveChanges();
        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 1_200m, KdvOran = 0.20m, KdvTuru = "STANDART",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 1m, BirimFiyat = 1_200m }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kid = db.FaturaKalemleri.Single(x => x.FaturaId == fid).Id;
        await servis.TaslakKalemGuncelle(fid, kid, G("{\"miktar\":2}"), k.Islem);
        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.Id == kid);
        Assert.Equal(18.32m, kalem.HasGrami);
        Assert.Equal(0m, kalem.HasDeger);                    // STANDART: has düşülmez
        Assert.Equal(400.00m, kalem.KdvTutar);               // 2.400 × 0,2/1,2
    }

    [Fact]
    public void ZOBHG05_KalemToplamHas_birim_x_miktar_birim_yoksa_null()
    {
        Assert.Equal(18.32m, MFatura.FaturaServisi.KalemToplamHas(2m, 9.16m));
        Assert.Equal(0.000275m, MFatura.FaturaServisi.KalemToplamHas(0.5m, 0.00055m));
        Assert.Null(MFatura.FaturaServisi.KalemToplamHas(3m, null));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // ZO-BELGE (orkestratör 30.09 07:50) — has bedelinin DAYANAĞI KUR KAYDIDIR.
    // kalem.HasBirimFiyat oluşurken motorun kullandığı kur kaydındaki fiyatla yazılır;
    // taslak düzeltmede has bedeli = round2(yeni has gram × kalem.HasBirimFiyat).
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ZOBHG06_uc_yolda_kalem_HasBirimFiyat_KUR_KAYDINDAN_STANDART_ve_altin_disi_NULL()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOBHG06");
        var fisId = Kimlik.Ulid();
        kurulum.Hareketler.Add(AltinSatis(k, fisId, 100_000m, 9.16m, 1m));
        kurulum.SaveChanges();
        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        // 1) kalemli yol
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 100_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 1m, BirimFiyat = 100_000m }],
        }, k.Islem);
        var fKalemli = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        // 2) taslağa kalem ekle
        await servis.TaslakKalemEkle(fKalemli,
            new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 1m, BirimFiyat = 100_000m }, k.Islem);
        // 3) fişten
        await servis.FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId, KdvTuru = "KUYUM", KdvOran = 0.20m }, k.Islem);
        // 4) STANDART: has bedeli düşülmez → dayanak yok
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 1_200m, KdvOran = 0.20m, KdvTuru = "STANDART",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 1m, BirimFiyat = 1_200m }],
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kalemler = kontrol.FaturaKalemleri.Where(x => x.TenantId == k.TenantId).ToList();
        Assert.Equal(4, kalemler.Count);
        var kuyum = kalemler.Where(x => x.KdvTuru == "KUYUM").ToList();
        Assert.Equal(3, kuyum.Count);
        Assert.All(kuyum, x => Assert.Equal(6_827.20m, x.HasBirimFiyat));
        Assert.Single(kuyum, x => x.HareketId is not null);                       // fişten yolu dahil
        Assert.Null(kalemler.Single(x => x.KdvTuru == "STANDART").HasBirimFiyat);
    }

    [Fact]
    public async Task ZOBHG07_dayanak_NULL_ise_duzeltmede_ozel_matrah_UYGULANMAZ_KDV_tam_geri_cozulmez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOBHG07");
        kurulum.SaveChanges();
        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 100_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 1m, BirimFiyat = 100_000m }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kid = db.FaturaKalemleri.Single(x => x.FaturaId == fid).Id;
        using (var eski = vt.OlusturSistem())
        {
            // Dayanağı bilinmeyen (çözülemeyen) eski kalem: kolon NULL, has bedeli dolu.
            var kalem = eski.FaturaKalemleri.Single(x => x.Id == kid);
            Assert.Equal(62_537.15m, kalem.HasDeger);
            kalem.HasBirimFiyat = null;
            eski.SaveChanges();
        }
        db.ChangeTracker.Clear();   // servis bağlamı eski kalemi izlemesin (kolon NULL okunmalı)
        await servis.TaslakKalemGuncelle(fid, kid, G("{\"miktar\":2}"), k.Islem);
        using var kontrol = vt.OlusturSistem();
        var sonra = kontrol.FaturaKalemleri.Single(x => x.Id == kid);
        Assert.Equal(18.32m, sonra.HasGrami);
        Assert.Equal(0m, sonra.HasDeger);                          // bedelden geri ÇÖZÜLMEDİ
        Assert.Equal(200_000.00m, sonra.SatirToplam);
        Assert.Equal(33_333.33m, sonra.KdvTutar);                  // 200.000 × 0,2/1,2 — KDV TAM
        var f = kontrol.Faturalar.Single(x => x.Id == fid);
        Assert.Equal(0m, f.HasDeger);
        Assert.Null(f.OzelMatrahKodu);
    }

    [Fact]
    public async Task ZOBHG08_duzeltmede_GUNCEL_kura_gecilmez_kalemin_kur_kaydi_kullanilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOBHG08");
        kurulum.SaveChanges();
        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 100_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 1m, BirimFiyat = 100_000m }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kid = db.FaturaKalemleri.Single(x => x.FaturaId == fid).Id;
        using (var yeniKur = vt.OlusturSistem())
        {
            yeniKur.KurSnapshots.Add(new KurSnapshot
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, AltinHasFiyati = 7_000m, GramAltinFiyati = 7_050m,
                Kaynak = "MANUAL", Olusturuldu = DateTime.UtcNow.AddMinutes(1),
            });
            yeniKur.SaveChanges();
        }
        await servis.TaslakKalemGuncelle(fid, kid, G("{\"miktar\":2}"), k.Islem);
        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.Id == kid);
        Assert.Equal(6_827.20m, kalem.HasBirimFiyat);
        Assert.Equal(125_074.30m, kalem.HasDeger);   // 18,32 × 6.827,20 = 125.074,304 (7.000 ile 128.240,00 olurdu)
    }

    private static IEnumerable<string> GocSql(Microsoft.EntityFrameworkCore.Migrations.Migration m) =>
        m.UpOperations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.SqlOperation>().Select(o => o.Sql);

    /// <summary>Migration'ın KOŞTUĞU SQL'in TA KENDİSİ, geri alınan işlem içinde (başka testin verisine kalıcı dokunmaz).</summary>
    private static void IslemIcindeKos(Microsoft.EntityFrameworkCore.Migrations.Migration m, KuyumcuDbContext db, Action kontrol)
    {
        using var tx = db.Database.BeginTransaction();
        foreach (var sql in GocSql(m)) db.Database.ExecuteSqlRaw(sql);
        db.ChangeTracker.Clear();
        kontrol();
        tx.Rollback();
    }

    private async Task<(Kiraci k, string fid)> BirimHasliEskiFatura(KuyumcuDbContext db, Kiraci k, string durum, string? maden)
    {
        var onceki = db.Faturalar.Where(x => x.TenantId == k.TenantId).Select(x => x.Id).ToList();
        using (var kdb = vt.Olustur(k))
            await new MFatura.FaturaServisi(kdb).Olustur(new MFatura.FaturaOlusturDto
            {
                CariId = k.CariId, Toplam = 200_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
                Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 2m, BirimFiyat = 100_000m }],
            }, k.Islem);
        var f = db.Faturalar.Single(x => x.TenantId == k.TenantId && !onceki.Contains(x.Id));
        var kalem = db.FaturaKalemleri.Single(x => x.FaturaId == f.Id);
        kalem.HasGrami = 9.16m;          // ESKİ anlam: birim has (migration öncesi kayıt)
        kalem.Maden = maden;
        kalem.HasBirimFiyat = null;      // kolon öncesi kayıt
        f.Durum = durum;
        f.HasGram = 9.16m;
        db.SaveChanges();
        return (k, f.Id);
    }

    [Fact]
    public async Task ZOBHG09_HasGramiToplam_geri_doldurmasi_YALNIZ_TASLAK_kalem_KESILDI_dokunulmaz_maden_NULL_baslik_toplanir()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "ZOBHG09");
        db.SaveChanges();
        var (_, taslak) = await BirimHasliEskiFatura(db, k, "TASLAK", Maden.Altin);
        var (_, kesildi) = await BirimHasliEskiFatura(db, k, "KESILDI", Maden.Altin);
        var (_, madensiz) = await BirimHasliEskiFatura(db, k, "TASLAK", null);

        IslemIcindeKos(new Kuyumcu.Api.Data.Migrations.ZOBelgeKalemHasGramiToplam(), db, () =>
        {
            decimal? Kalem(string fid) => db.FaturaKalemleri.AsNoTracking().Single(x => x.FaturaId == fid).HasGrami;
            Fatura F(string fid) => db.Faturalar.AsNoTracking().Single(x => x.Id == fid);
            Assert.Equal(18.32m, Kalem(taslak));          // 9,16 × 2
            Assert.Equal(18.32m, F(taslak).HasGram);
            Assert.Equal(9.16m, Kalem(kesildi));          // KESİLMİŞ belge: satırına DOKUNULMAZ
            Assert.Equal(9.16m, F(kesildi).HasGram);
            Assert.Equal(18.32m, Kalem(madensiz));
            Assert.Equal(0m, F(madensiz).HasGram);        // altın kalemi yok → başlık 0, eski 9,16 kalmaz
            Assert.Null(F(madensiz).HasBirimFiyat);
        });
    }

    [Fact]
    public async Task ZOBHG10_HasBirimFiyat_geri_doldurmasi_fisten_KUR_KAYDI_digeri_bedel_bolu_gram_KESILDI_ve_STANDART_NULL()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "ZOBHG10");
        var fisId = Kimlik.Ulid();
        db.Hareketler.Add(AltinSatis(k, fisId, 100_000m, 9.16m, 1m));
        db.SaveChanges();
        using var kdb = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(kdb);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 100_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 1m, BirimFiyat = 100_000m }],
        }, k.Islem);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 100_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 1m, BirimFiyat = 100_000m }],
        }, k.Islem);
        await servis.FistenOlustur(new MFatura.FaturaFistenDto { FisId = fisId, KdvTuru = "KUYUM", KdvOran = 0.20m }, k.Islem);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 1_200m, KdvOran = 0.20m, KdvTuru = "STANDART",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 1m, BirimFiyat = 1_200m }],
        }, k.Islem);
        var kalemler = db.FaturaKalemleri.Where(x => x.TenantId == k.TenantId).ToList();
        foreach (var x in kalemler) x.HasBirimFiyat = null;       // kolon öncesi kayıtlar
        var urunKalemleri = kalemler.Where(x => x.HareketId is null && x.KdvTuru == "KUYUM").OrderBy(x => x.Id).ToList();
        var taslakUrun = urunKalemleri[0].Id;
        var kesilen = urunKalemleri[1].Id;
        db.Faturalar.Single(x => x.Id == urunKalemleri[1].FaturaId).Durum = "KESILDI";
        var fisten = kalemler.Single(x => x.HareketId is not null).Id;
        var standart = kalemler.Single(x => x.KdvTuru == "STANDART").Id;
        db.SaveChanges();

        IslemIcindeKos(new Kuyumcu.Api.Data.Migrations.ZOBelgeKalemHasBirimFiyat(), db, () =>
        {
            decimal? D(string kid) => db.FaturaKalemleri.AsNoTracking().Single(x => x.Id == kid).HasBirimFiyat;
            Assert.Equal(6_827.20m, D(fisten));           // hareketin KUR KAYDI
            Assert.Equal(6_827.199782m, D(taslakUrun));   // eski kayıt: 62.537,15 ÷ 9,16 (6 hane)
            Assert.Null(D(kesilen));                       // KESİLMİŞ belgeye dokunulmaz
            Assert.Null(D(standart));                      // STANDART: özel matrah yok
        });
    }
}
