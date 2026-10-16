using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using MFatura = Kuyumcu.Api.Modules.Fatura;
using Maden = Kuyumcu.Core.Domain.Maden;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// YASAL-4 Y4-D (K-10b) — ÖZEL MATRAHTA MADEN SÜZGECİ [DB]
//
// KDVK md.23/e (birincil kaynak, mevzuat.gov.tr/mevzuatmetin/1.5.3065.pdf):
//   "Altından mamül veya altın ihtiva eden ziynet eşyaları ile sikke altınların
//    teslim ve ithalinde matrah, külçe altın bedeli düşüldükten sonra kalan miktardır."
// Hüküm YALNIZ ALTIN içindir. Gümüş için özel matrah 23/g yetkisiyle Tebliğ'dedir;
// fiyat kaynağı (gümüş kapanışı) sistemde yok → mali müşavir teyidine kadar
// UYGULANMAZ (tutucu: gümüş kalemin tam bedeli KDV matrahına girer).
//
// Hata (ağaçta doğrulandı, bu dosya önce kırmızı koştu): has bacakları madene
// bakılmadan ALTIN has fiyatıyla çarpılıp matrahtan düşülüyordu → gümüş kalemli
// kuyum faturasında KDV EKSİK hesaplanıyordu.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class OzelMatrahMadenTestleri(TestVeritabani vt)
{
    private const decimal KurHasFiyati = 6_827.20m;      // TestKurulum.KiraciKur
    private const decimal GumusHas = 9.25m;               // G925 × 10 g
    private const decimal AltinHas = 4.58m;               // 22 ayar (916) × 5 g

    private static decimal Y2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);

    private static Hareket Satir(Kiraci k, string fisId, decimal tl, decimal hasGram,
        string maden, string? urunId = null)
    {
        var now = DateTime.UtcNow;
        return new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = "SATIS", Tarih = now, TlTutari = tl, HasGrami = -hasGram, Maden = maden,
            OdemeTuru = "HAVALE", NakitTutar = 0m, VeresiyeTutar = 0m, Adet = 1m,
            FisId = fisId, KurSnapshotId = k.KurId, CariId = k.CariId,
            UrunId = urunId ?? k.UrunId, KullaniciId = k.KullaniciId, Olusturuldu = now,
        };
    }

    private static string GumusUrunEkle(KuyumcuDbContext db, Kiraci k)
    {
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Urunler.Add(new Urun
        {
            Id = id, TenantId = k.TenantId, Tip = "GUMUS", Kod = $"G-{id[..8]}", Ad = "925 Gümüş Kolye",
            AyarId = "G925", Milyem = 925m, GramAgirlik = 10m, TasAgirlik = 0m, HasGrami = GumusHas,
            Birim = "ADET", IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        return id;
    }

    /// <summary>Beklenen KDV: yalnız ALTIN has bedeli düşülür.</summary>
    private static (decimal HasDeger, decimal Kdv) Beklenen(decimal toplam, decimal altinHas)
    {
        var has = Y2(altinHas * KurHasFiyati);
        var kalan = toplam - has;
        var matrah = Y2(kalan / 1.20m);
        return (has, Y2(kalan - matrah));
    }

    // ── K-10b · HAREKET yolu (fatura oluştur + hareketId) ───────────────────

    [Fact]
    public async Task K10b_Altin_ve_gumus_fisinde_HAREKET_yolu_YALNIZ_altin_has_bedeli_dusulur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "YASAL4 K10b hareket");
        var fisId = Kimlik.Ulid();
        var altin = Satir(k, fisId, 90_000m, AltinHas, Maden.Altin);
        var gumus = Satir(k, fisId, 10_000m, GumusHas, Maden.Gumus);
        kurulum.Hareketler.AddRange(altin, gumus);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 100_000m, KdvOran = 0.20m,
            KdvTuru = "KUYUM", HareketId = altin.Id,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var f = kontrol.Faturalar.Single(x => x.TenantId == k.TenantId);
        var (has, kdv) = Beklenen(100_000m, AltinHas);
        // Gümüş 9,25 g has ALTIN fiyatıyla düşülseydi has bedeli 94.420,58 ve KDV 929,90 olurdu.
        Assert.Equal(AltinHas, f.HasGram);
        Assert.Equal(has, f.HasDeger);
        Assert.Equal(kdv, f.KdvTutar);
        // Damga kümesi değişmez: gümüş kalem de fişin parçası olarak faturalandı.
        Assert.All(kontrol.Hareketler.Where(x => x.TenantId == k.TenantId).ToList(),
            x => Assert.Equal(MFatura.FaturaSeriKapisi.Isaret(f.Id), x.FaturaNo));
    }

    [Fact]
    public async Task K10b_Yalniz_gumus_fisinde_has_bedeli_0_KDV_TAM_ve_ozel_matrah_kodu_yazilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "YASAL4 K10b gümüş");
        var gumus = Satir(k, Kimlik.Ulid(), 10_000m, GumusHas, Maden.Gumus);
        kurulum.Hareketler.Add(gumus);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db,
            new MFatura.KdvAyarlari { OzelMatrahKodu = MFatura.KdvAyarlari.OneriKod })
            .Olustur(new MFatura.FaturaOlusturDto
            {
                CariId = k.CariId, Toplam = 10_000m, KdvOran = 0.20m,
                KdvTuru = "KUYUM", HareketId = gumus.Id,
            }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var f = kontrol.Faturalar.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(0m, f.HasDeger);
        Assert.Equal(1_666.67m, f.KdvTutar);                 // 10.000 − 10.000/1,2
        // 805 = "altından mamul/altın içeren ziynet" kodu; altın düşülmeyen belgeye basılmaz.
        Assert.Null(f.OzelMatrahKodu);
    }

    [Fact]
    public async Task K10b_Yalniz_altin_fisinde_davranis_DEGISMEZ_ve_kod_yazilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "YASAL4 K10b altın");
        var altin = Satir(k, Kimlik.Ulid(), 75_000m, 10m, Maden.Altin);
        kurulum.Hareketler.Add(altin);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db,
            new MFatura.KdvAyarlari { OzelMatrahKodu = MFatura.KdvAyarlari.OneriKod })
            .Olustur(new MFatura.FaturaOlusturDto
            {
                CariId = k.CariId, Toplam = 75_000m, KdvOran = 0.20m,
                KdvTuru = "KUYUM", HareketId = altin.Id,
            }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var f = kontrol.Faturalar.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(Y2(10m * KurHasFiyati), f.HasDeger);
        Assert.Equal(1_121.33m, f.KdvTutar);
        Assert.Equal(MFatura.KdvAyarlari.OneriKod, f.OzelMatrahKodu);
    }

    // ── K-10b · FİŞTEN fatura yolu (ekranın kullandığı yol) ─────────────────

    [Fact]
    public async Task K10b_Fisten_faturada_gumus_kalemi_altin_fiyatiyla_DUSULMEZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "YASAL4 K10b fişten");
        var gumusUrun = GumusUrunEkle(kurulum, k);
        var fisId = Kimlik.Ulid();
        kurulum.Hareketler.AddRange(
            Satir(k, fisId, 90_000m, AltinHas, Maden.Altin),
            Satir(k, fisId, 10_000m, GumusHas, Maden.Gumus, gumusUrun));
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId, KdvTuru = "KUYUM", KdvOran = 0.20m }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var f = kontrol.Faturalar.Single(x => x.TenantId == k.TenantId);
        var kalemler = kontrol.FaturaKalemleri.Where(x => x.TenantId == k.TenantId)
            .OrderBy(x => x.Sira).ToList();
        var gumusKalem = kalemler.Single(x => x.UrunId == gumusUrun);
        var altinKalem = kalemler.Single(x => x.UrunId != gumusUrun);

        // Gümüş kalem: has bedeli 0, KDV tam (10.000 × 0,2/1,2).
        Assert.Equal(0m, gumusKalem.HasDeger);
        Assert.Equal(1_666.67m, gumusKalem.KdvTutar);
        var (has, kdv) = Beklenen(90_000m, AltinHas);
        Assert.Equal(has, altinKalem.HasDeger);
        Assert.Equal(kdv, altinKalem.KdvTutar);
        // Belge başlığı: has gram ve bedel YALNIZ altındır; özet kur altın kurudur.
        Assert.Equal(has, f.HasDeger);
        Assert.Equal(kdv + 1_666.67m, f.KdvTutar);
        Assert.Equal(AltinHas, f.HasGram);
        Assert.Equal(KurHasFiyati, Math.Round(f.HasBirimFiyat ?? 0m, 2));
    }

    [Fact]
    public async Task K10b_Kalemli_faturada_gumus_urun_kalemi_has_bedeli_0_KDV_tam()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "YASAL4 K10b kalemli");
        var gumusUrun = GumusUrunEkle(kurulum, k);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        await new MFatura.FaturaServisi(db).Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 10_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = gumusUrun, Miktar = 1m, BirimFiyat = 10_000m }],
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var f = kontrol.Faturalar.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(0m, f.HasDeger);
        Assert.Equal(1_666.67m, f.KdvTutar);
    }

    [Fact]
    public async Task K10b_Taslaga_eklenen_gumus_kalemi_has_bedeli_0_KDV_tam()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "YASAL4 K10b ekle");
        var gumusUrun = GumusUrunEkle(kurulum, k);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 90_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 1m, BirimFiyat = 90_000m }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        await servis.TaslakKalemEkle(fid,
            new MFatura.FaturaKalemDto { UrunId = gumusUrun, Miktar = 1m, BirimFiyat = 10_000m }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var eklenen = kontrol.FaturaKalemleri.Single(x => x.TenantId == k.TenantId && x.UrunId == gumusUrun);
        Assert.Equal(0m, eklenen.HasDeger);
        Assert.Equal(1_666.67m, eklenen.KdvTutar);
        var f = kontrol.Faturalar.Single(x => x.Id == fid);
        // Başlık has gramı gümüş gramını İÇERMEZ (yalnız altın kalemin has'ı).
        var altinKalem = kontrol.FaturaKalemleri.Single(x => x.TenantId == k.TenantId && x.UrunId == k.UrunId);
        Assert.Equal(Math.Abs(altinKalem.HasGrami ?? 0m), f.HasGram);
    }

    // ── K-10b · TASLAK KALEM DÜZENLEME yolu (5. yol; hasım doğrulayıcı bulgusu) ──

    [Fact]
    public async Task K10b_Taslakta_gumus_kalemi_DUZENLENIR_has_0_KDV_tam_altin_dayanak_korunur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "YASAL4 K10b düzenle");
        var gumusUrun = GumusUrunEkle(kurulum, k);
        var fisId = Kimlik.Ulid();
        kurulum.Hareketler.AddRange(
            Satir(k, fisId, 90_000m, AltinHas, Maden.Altin),
            Satir(k, fisId, 10_000m, GumusHas, Maden.Gumus, gumusUrun));
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId, KdvTuru = "KUYUM", KdvOran = 0.20m }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var gid = db.FaturaKalemleri.Single(x => x.FaturaId == fid && x.UrunId == gumusUrun).Id;
        var aid = db.FaturaKalemleri.Single(x => x.FaturaId == fid && x.UrunId != gumusUrun).Id;

        // Gümüş kalem: fiyat, işçilik ve indirim düzeltilir → 400 YOK, has 0, KDV tam.
        await servis.TaslakKalemGuncelle(fid, gid, System.Text.Json.JsonDocument.Parse(
            "{\"birimFiyat\":11000,\"iscilikTl\":500,\"indirimTutari\":300}").RootElement, k.Islem);
        // Altın kalem: fiyat düzeltilir → has bedeli DAYANAĞI (eski kur) korunur.
        await servis.TaslakKalemGuncelle(fid, aid, System.Text.Json.JsonDocument.Parse(
            "{\"birimFiyat\":95000}").RootElement, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var g = kontrol.FaturaKalemleri.Single(x => x.Id == gid);
        Assert.Equal(11_200m, g.SatirToplam);
        Assert.Equal(0m, g.HasDeger);
        Assert.Equal(Y2(11_200m - Y2(11_200m / 1.20m)), g.KdvTutar);
        var a = kontrol.FaturaKalemleri.Single(x => x.Id == aid);
        var (has, kdv) = Beklenen(95_000m, AltinHas);
        Assert.Equal(has, a.HasDeger);
        Assert.Equal(kdv, a.KdvTutar);
        var f = kontrol.Faturalar.Single(x => x.Id == fid);
        Assert.Equal(has, f.HasDeger);                 // başlık: yalnız altın has bedeli
        Assert.Equal(106_200m, f.Toplam);
    }

    [Fact]
    public async Task K10b_Yalniz_gumus_taslaga_ALTIN_satir_eklenince_805_kodu_belgeye_girer()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "YASAL4 K10b kod");
        var gumusUrun = GumusUrunEkle(kurulum, k);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db,
            new MFatura.KdvAyarlari { OzelMatrahKodu = MFatura.KdvAyarlari.OneriKod });
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 10_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = gumusUrun, Miktar = 1m, BirimFiyat = 10_000m }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        using (var ara = vt.OlusturSistem())
            Assert.Null(ara.Faturalar.Single(x => x.Id == fid).OzelMatrahKodu);   // yalnız gümüş: kod yok

        await servis.TaslakKalemEkle(fid,
            new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 1m, BirimFiyat = 90_000m }, k.Islem);
        using (var ara = vt.OlusturSistem())
            Assert.Equal(MFatura.KdvAyarlari.OneriKod, ara.Faturalar.Single(x => x.Id == fid).OzelMatrahKodu);

        // Gümüş satırı düzeltmek kodu SİLMEZ (altın bedeli hâlâ var).
        var gid = db.FaturaKalemleri.Single(x => x.FaturaId == fid && x.UrunId == gumusUrun).Id;
        await servis.TaslakKalemGuncelle(fid, gid,
            System.Text.Json.JsonDocument.Parse("{\"birimFiyat\":9000}").RootElement, k.Islem);
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(MFatura.KdvAyarlari.OneriKod, kontrol.Faturalar.Single(x => x.Id == fid).OzelMatrahKodu);
    }

    // ── Önizleme: aynı süzgeç ────────────────────────────────────────────────

    [Fact]
    public async Task K10b_Onizleme_yalniz_altin_has_gramini_ve_bedelini_gosterir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "YASAL4 K10b önizleme");
        var fisId = Kimlik.Ulid();
        var altin = Satir(k, fisId, 90_000m, AltinHas, Maden.Altin);
        kurulum.Hareketler.AddRange(altin, Satir(k, fisId, 10_000m, GumusHas, Maden.Gumus));
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var y = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(
            await new MFatura.FaturaServisi(db).OzelMatrahOnizleme(altin.Id, 0.20m, 100_000m, k.Islem))).RootElement;
        Assert.Equal(AltinHas, y.GetProperty("hasGram").GetDecimal());
        Assert.Equal(Beklenen(100_000m, AltinHas).HasDeger, y.GetProperty("hasDeger").GetDecimal());
    }

    // ── ZO-BELGE 30.09 · KALEM MADENİNİN TEK KAYNAĞI (kalem.maden) ───────────
    // Kök neden (orkestratör 05:50): madenin İKİ kaynağı vardı — oluşturmada
    // hareketin madeni (h.Maden), düzenlemede kalemin AYARI (ayar yoksa ALTIN
    // varsayımı). Ürünsüz/ayarsız gümüş hurda satırı oluşturmada "gümüş",
    // düzenlemede "altın" sayılıp 400'e düşüyordu. Artık madeni kalem oluşurken
    // bir kez yazılır; tüm yollar onu okur; bilinmiyorsa özel matrah YOK.

    private static Hareket UrunsuzHurda(Kiraci k, string fisId, decimal tl, decimal hasGram, string maden)
    {
        var h = Satir(k, fisId, -tl, -hasGram, maden);
        h.Tur = "HURDA_ALIS";
        h.UrunId = null;
        return h;
    }

    private static System.Text.Json.JsonElement Govde(string json) =>
        System.Text.Json.JsonDocument.Parse(json).RootElement;

    [Fact]
    public async Task ZOB_Urunsuz_gumus_hurda_fisten_kalem_madeni_DEFTERDEN_yazilir_ve_DUZENLENIR()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOB maden hurda");
        var fisId = Kimlik.Ulid();
        kurulum.Hareketler.AddRange(
            UrunsuzHurda(k, fisId, 10_000m, GumusHas, Maden.Gumus),
            UrunsuzHurda(k, fisId, 40_000m, AltinHas, Maden.Altin));
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId, KdvTuru = "KUYUM", KdvOran = 0.20m }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kalemler = db.FaturaKalemleri.Where(x => x.FaturaId == fid).ToList();
        var gid = kalemler.Single(x => x.SatirToplam == 10_000m).Id;
        var aid = kalemler.Single(x => x.SatirToplam == 40_000m).Id;

        using (var once = vt.OlusturSistem())
        {
            var g0 = once.FaturaKalemleri.Single(x => x.Id == gid);
            Assert.Null(g0.AyarId);                         // ürünsüz: ayar YOK
            Assert.Equal(Maden.Gumus, g0.Maden);            // maden defterden
            Assert.Equal(0m, g0.HasDeger);
            Assert.Equal(Maden.Altin, once.FaturaKalemleri.Single(x => x.Id == aid).Maden);
        }

        // Gümüş satır düzeltilir → 400 YOK, has 0, KDV tam.
        await servis.TaslakKalemGuncelle(fid, gid, Govde("{\"birimFiyat\":12000}"), k.Islem);
        // Ürünsüz ALTIN hurda satırı: davranış DEĞİŞMEZ — has dayanağı (eski kur) korunur.
        await servis.TaslakKalemGuncelle(fid, aid, Govde("{\"birimFiyat\":42000}"), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var g = kontrol.FaturaKalemleri.Single(x => x.Id == gid);
        Assert.Equal(0m, g.HasDeger);
        Assert.Equal(2_000.00m, g.KdvTutar);
        var a = kontrol.FaturaKalemleri.Single(x => x.Id == aid);
        var (has, kdv) = Beklenen(42_000m, AltinHas);
        Assert.Equal(has, a.HasDeger);
        Assert.Equal(kdv, a.KdvTutar);
        var f = kontrol.Faturalar.Single(x => x.Id == fid);
        Assert.Equal(has, f.HasDeger);
        Assert.Equal(AltinHas, f.HasGram);                   // başlık has gramına gümüş KATILMAZ
    }

    [Fact]
    public async Task ZOB_Madeni_BILINMEYEN_kuyum_kalemi_ozel_matrah_UYGULANMAZ_KDV_tam()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOB maden bilinmiyor");
        var fisId = Kimlik.Ulid();
        kurulum.Hareketler.Add(Satir(k, fisId, 90_000m, AltinHas, Maden.Altin));
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.FistenOlustur(
            new MFatura.FaturaFistenDto { FisId = fisId, KdvTuru = "KUYUM", KdvOran = 0.20m }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kid = db.FaturaKalemleri.Single(x => x.FaturaId == fid).Id;

        // Eski (geri doldurulamamış) satır benzetimi: madeni bilinmiyor. Ayar ALTIN
        // (K22) olsa bile ayardan TÜRETİLMEZ.
        using (var ara = vt.OlusturSistem())
        {
            var kk = ara.FaturaKalemleri.Single(x => x.Id == kid);
            kk.Maden = null;
            kk.AyarId = "K22";
            ara.SaveChanges();
        }
        db.ChangeTracker.Clear();

        await servis.TaslakKalemGuncelle(fid, kid, Govde("{\"birimFiyat\":96000}"), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kalem = kontrol.FaturaKalemleri.Single(x => x.Id == kid);
        Assert.Equal(0m, kalem.HasDeger);                    // fail-closed: bedel düşülmez
        Assert.Equal(16_000.00m, kalem.KdvTutar);            // 96.000 × 0,2/1,2
        var f = kontrol.Faturalar.Single(x => x.Id == fid);
        Assert.Equal(0m, f.HasDeger);
        Assert.Equal(0m, f.HasGram ?? 0m);
    }

    [Fact]
    public async Task ZOB_Kalem_madeni_URUN_yollarinda_urunun_madeninden_yazilir_ve_okuma_ucu_dondurur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOB maden ürün");
        var gumusUrun = GumusUrunEkle(kurulum, k);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        await servis.Olustur(new MFatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = 10_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new MFatura.FaturaKalemDto { UrunId = gumusUrun, Miktar = 1m, BirimFiyat = 10_000m }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        await servis.TaslakKalemEkle(fid,
            new MFatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = 1m, BirimFiyat = 90_000m }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(Maden.Gumus, kontrol.FaturaKalemleri.Single(x => x.FaturaId == fid && x.UrunId == gumusUrun).Maden);
        Assert.Equal(Maden.Altin, kontrol.FaturaKalemleri.Single(x => x.FaturaId == fid && x.UrunId == k.UrunId).Maden);

        var y = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(
            await servis.Kalemler(fid, k.Islem))).RootElement;
        var madenler = y.GetProperty("kalemler").EnumerateArray()
            .Select(e => e.GetProperty("Maden").GetString()).OrderBy(s => s).ToList();
        Assert.Equal([Maden.Altin, Maden.Gumus], madenler);
    }

    [Fact]
    public async Task ZOB_Onizleme_altin_disi_maden_bayragi_yalniz_gumus_bacak_varken_true()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "ZOB önizleme bayrak");
        var karisik = Kimlik.Ulid();
        var yalnizAltin = Kimlik.Ulid();
        var a1 = Satir(k, karisik, 90_000m, AltinHas, Maden.Altin);
        var a2 = Satir(k, yalnizAltin, 90_000m, AltinHas, Maden.Altin);
        kurulum.Hareketler.AddRange(a1, Satir(k, karisik, 10_000m, GumusHas, Maden.Gumus), a2);
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var servis = new MFatura.FaturaServisi(db);
        bool Bayrak(object o) => System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o))
            .RootElement.GetProperty("altinDisiMadenVar").GetBoolean();
        Assert.True(Bayrak(await servis.OzelMatrahOnizleme(a1.Id, 0.20m, null, k.Islem)));
        Assert.False(Bayrak(await servis.OzelMatrahOnizleme(a2.Id, 0.20m, null, k.Islem)));
    }
}
