using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-08 — HAREKET TÜRÜ SÖZLEŞMESİ + ÇEKİRDEK KOLONLAR + FATURA BAĞI
// Kimlikler: Y-32 (tür kapısı) · Y-14 (adet) · Y-13 (personel/SATAN) ·
//            Y-25 (fatura ↔ hareket tek sorgu yolu, ADR-0018) · B-52 (sayaçlar)
//
// Her test BİR iş kuralını yeniden üretir; kural bozulunca ADIYLA kırmızı olur.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class P08HareketSozlesmeTestleri(TestVeritabani vt)
{
    private static Hareket Taslak(Kiraci k, string tur, decimal tl = 1_000m) => new()
    {
        Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = tur,
        Tarih = DateTime.UtcNow, TlTutari = tl, HasGrami = 0m,
        KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId, Olusturuldu = DateTime.UtcNow,
    };

    // ─── Y-32 — TÜR KAPISI ───────────────────────────────────────────────────

    [Fact]
    public void TurKapisi_ListedeOlmayanTur_400_doner()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 tür kapısı");
        using var db = vt.Olustur(k);

        var hata = Assert.Throws<BadRequestException>(() =>
            HareketFabrikasi.Ekle(db, OdemeIzi.Yok, Taslak(k, "UYDURMA_TUR")));

        Assert.Equal(400, hata.StatusCode);
        Assert.Contains("Tanımsız hareket türü", hata.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TurKapisi_BosTur_400_doner()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 boş tür");
        using var db = vt.Olustur(k);

        Assert.Throws<BadRequestException>(() =>
            HareketFabrikasi.Ekle(db, OdemeIzi.Yok, Taslak(k, "   ")));
    }

    [Fact]
    public void TurKapisi_ListedekiTum_turler_KABUL_edilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 tüm türler");
        using var db = vt.Olustur(k);

        // Kapı GEREĞİNDEN FAZLA sıkılaşırsa (ör. biri listeden düşerse) burada
        // ADIYLA kırmızı olur — "tür kapısı eski bir modülü kırabilir" riski
        // (PLAN § 2 P-08) gizlenmez, ölçülür.
        foreach (var tur in Sabitler.HareketTurleri)
        {
            var taslak = Taslak(k, tur);
            // Döviz türlerinde veresiye payı yasaktır (K27-E); iz Yok ile geçilir.
            var kayit = HareketFabrikasi.Ekle(db, OdemeIzi.Yok, taslak);
            Assert.Equal(tur, kayit.Tur);
        }
        db.SaveChanges();

        Assert.Equal(
            Sabitler.HareketTurleri.Length,
            db.Hareketler.AsNoTracking().Count(h => h.TenantId == k.TenantId));
    }

    // ─── Y-14 — ADET ─────────────────────────────────────────────────────────

    [Fact]
    public void AdetKapisi_NegatifAdet_400_doner()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 adet");
        using var db = vt.Olustur(k);

        var taslak = Taslak(k, "SATIS");
        taslak.Adet = -1m;

        var hata = Assert.Throws<BadRequestException>(() =>
            HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(1_000m), taslak));
        Assert.Equal(400, hata.StatusCode);
        Assert.Contains("negatif olamaz", hata.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Adet_NULL_kalir_ve_0_ile_DOLDURULMAZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 adet null");
        using var db = vt.Olustur(k);

        var taslak = Taslak(k, "SATIS");
        HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(1_000m), taslak);
        db.SaveChanges();

        var okunan = db.Hareketler.AsNoTracking().Single(h => h.Id == taslak.Id);
        // NULL = BİLİNMİYOR. Fabrika burayı 0 ile doldursaydı rapor "0 adet
        // satıldı" diye YANLIŞ bir iddia üretirdi (Anayasa md.1).
        Assert.Null(okunan.Adet);
    }

    [Fact]
    public void Adet_yazildiysa_uc_ondalikla_korunur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 adet gram");
        using var db = vt.Olustur(k);

        var taslak = Taslak(k, "SATIS");
        taslak.Adet = 2.125m;                       // gramlı üründe 1 adet = 1 gram (A15)
        HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(1_000m), taslak);
        db.SaveChanges();

        Assert.Equal(2.125m, db.Hareketler.AsNoTracking().Single(h => h.Id == taslak.Id).Adet);
    }

    [Fact]
    public async Task IslemDefteri_adeti_bilinmeyen_satirda_BILINMIYOR_der()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 defter adet");
        using var db = vt.Olustur(k);

        var taslak = Taslak(k, "SATIS");
        HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(1_000m), taslak);
        db.SaveChanges();

        var servis = new Kuyumcu.Api.Modules.Rapor.SatisRaporServisi(db, TestKurulum.Denetim(db));
        var cevap = await servis.IslemDefteri(k.Islem, null, null, new Kuyumcu.Api.Modules.Rapor.RaporFiltresi());

        var json = System.Text.Json.JsonSerializer.Serialize(cevap);
        Assert.Contains("\"adetBilinmiyor\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"adet\":null", json, StringComparison.Ordinal);
    }

    // ─── Y-13 — PERSONEL (SATAN) ─────────────────────────────────────────────

    [Fact]
    public void PersonelKapisi_BosDizge_400_doner()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 personel");
        using var db = vt.Olustur(k);

        var taslak = Taslak(k, "SATIS");
        taslak.PersonelId = "  ";

        var hata = Assert.Throws<BadRequestException>(() =>
            HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(1_000m), taslak));
        Assert.Equal(400, hata.StatusCode);
        Assert.Contains("boş dizge olamaz", hata.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PersonelId_SATAN_anlamiyla_kilitlidir_ve_KullaniciId_den_AYRIDIR()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 satan");
        using var db = vt.Olustur(k);

        var satanId = Kimlik.Ulid();
        var taslak = Taslak(k, "SATIS");
        taslak.PersonelId = satanId;
        HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(1_000m), taslak);
        db.SaveChanges();

        var okunan = db.Hareketler.AsNoTracking().Single(h => h.Id == taslak.Id);
        Assert.Equal(satanId, okunan.PersonelId);
        // Kaydeden ile satan AYRI kolondur; biri diğerine kopyalanmaz.
        Assert.NotEqual(okunan.KullaniciId, okunan.PersonelId);
        // Anlam kilidi kodda SABİTTİR (denetim.sh P-08 bu sabiti sayar).
        Assert.Equal("SATAN", Sabitler.HareketPersonelRolu);
    }

    // ─── Y-25 — FATURA ↔ HAREKET BAĞI (ADR-0018) ─────────────────────────────

    private static Fatura FaturaYaz(
        KuyumcuDbContext db, Kiraci k, string faturaNo, string durum = "KESILDI", decimal toplam = 1_000m)
    {
        var f = new Fatura
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, FaturaNo = faturaNo, CariId = k.CariId,
            Tur = "SATIS", Toplam = toplam, AraToplam = toplam, Durum = durum,
            Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        };
        db.Faturalar.Add(f);
        return f;
    }

    private static void KalemYaz(KuyumcuDbContext db, Kiraci k, Fatura f, string hareketId)
        => db.FaturaKalemleri.Add(new FaturaKalemi
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, FaturaId = f.Id, Sira = 1,
            HareketId = hareketId, Aciklama = "kalem", Miktar = 1m, BirimFiyat = f.Toplam,
            SatirToplam = f.Toplam, Olusturuldu = DateTime.UtcNow,
        });

    [Fact]
    public async Task Bag_OTORITE_fatura_kalemleri_hareket_id_dir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 bağ kalem");
        using var db = vt.Olustur(k);

        var h = Taslak(k, "SATIS");
        HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(1_000m), h);
        var f = FaturaYaz(db, k, "FTR-2026-000900");
        KalemYaz(db, k, f, h.Id);
        db.SaveChanges();

        var bag = await FaturaHareketBagi.Coz(db, k.TenantId, [h], [f]);

        Assert.True(bag.ContainsKey(h.Id));
        Assert.Equal(FaturaHareketBagi.KaynakKalem, bag[h.Id].Kaynak);
        Assert.Equal(f.Id, bag[h.Id].FaturaId);
    }

    [Fact]
    public async Task Bag_ESKI_SATIRDA_damga_yolu_calisir_ama_DAMGA_diye_etiketlenir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 bağ damga");
        using var db = vt.Olustur(k);

        var h = Taslak(k, "SATIS");
        HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(1_000m), h);
        h.FaturaNo = "FTR-2026-000901";             // kalemsiz, eski model damga
        var f = FaturaYaz(db, k, "FTR-2026-000901");
        db.SaveChanges();

        var bag = await FaturaHareketBagi.Coz(db, k.TenantId, [h], [f]);

        Assert.True(bag.ContainsKey(h.Id));
        Assert.Equal(FaturaHareketBagi.KaynakDamga, bag[h.Id].Kaynak);
    }

    [Fact]
    public async Task Bag_KALEM_damgayi_YENER_ve_hareket_TEK_KEZ_sayilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 çifte sayma");
        using var db = vt.Olustur(k);

        var h = Taslak(k, "SATIS");
        HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(1_000m), h);
        h.FaturaNo = "FTR-2026-000902";
        var f = FaturaYaz(db, k, "FTR-2026-000902");
        KalemYaz(db, k, f, h.Id);                   // AYNI hareket hem kalemli hem damgalı
        db.SaveChanges();

        var bag = await FaturaHareketBagi.Coz(db, k.TenantId, [h], [f]);

        // Sözlük hareket kimliğine göre tekildir → çifte sayma yapısal olarak yok.
        Assert.Single(bag);
        Assert.Equal(FaturaHareketBagi.KaynakKalem, bag[h.Id].Kaynak);
    }

    [Fact]
    public async Task IlkBag_IPTAL_edilmis_faturayi_BAG_SAYMAZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 iptal");
        using var db = vt.Olustur(k);

        var h = Taslak(k, "SATIS");
        HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(1_000m), h);
        var f = FaturaYaz(db, k, "FTR-2026-000903", durum: "IPTAL");
        KalemYaz(db, k, f, h.Id);
        db.SaveChanges();

        // İptal edilen belgenin kalemi DURUR; ama o satış yeniden faturalanabilmelidir.
        Assert.Null(await FaturaHareketBagi.IlkBag(db, k.TenantId, [h]));
    }

    [Fact]
    public async Task Bagsiz_kalemi_bagli_ama_damgasi_DUSMUS_hareketi_ADAY_SAYMAZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 aday");
        using var db = vt.Olustur(k);

        var bagli = Taslak(k, "SATIS");
        HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(1_000m), bagli);
        var serbest = Taslak(k, "SATIS", 2_000m);
        HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(2_000m), serbest);
        var f = FaturaYaz(db, k, "FTR-2026-000904");
        KalemYaz(db, k, f, bagli.Id);               // damga YOK, yalnız kalem bağı var
        db.SaveChanges();

        var adaylar = await FaturaHareketBagi
            .Bagsiz(db, k.TenantId, db.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId))
            .Select(h => h.Id)
            .ToListAsync();

        Assert.DoesNotContain(bagli.Id, adaylar);
        Assert.Contains(serbest.Id, adaylar);
    }

    [Fact]
    public async Task IlkBag_KARSILIKSIZ_damgayi_da_ENGELLER_fail_closed()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 karşılıksız damga");
        using var db = vt.Olustur(k);

        var h = Taslak(k, "SATIS");
        HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(1_000m), h);
        h.FaturaNo = "FTR-2026-000906";             // damga VAR, faturalar satırı YOK
        db.SaveChanges();

        var bag = await FaturaHareketBagi.IlkBag(db, k.TenantId, [h]);

        // Tutarsızlık sessiz geçilmez: "fatura kaydı yoksa serbest" demek,
        // silinmiş/iptal edilmiş bir belgenin satışını ikinci kez faturalatırdı.
        Assert.NotNull(bag);
        Assert.Equal(FaturaHareketBagi.FaturaBulunamadi, bag!.FaturaId);
        Assert.Equal("FTR-2026-000906", bag.FaturaNo);
    }

    [Fact]
    public async Task IlkBag_kalemi_bagli_ama_DAMGASIZ_hareketi_de_ENGELLER()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 damgasız kalem");
        using var db = vt.Olustur(k);

        var h = Taslak(k, "SATIS");
        HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(1_000m), h);
        var f = FaturaYaz(db, k, "FTR-2026-000907");
        KalemYaz(db, k, f, h.Id);                   // damga YOK
        db.SaveChanges();

        // ESKİ HÂLDE bu satış İKİNCİ KEZ faturalanabiliyordu (yalnız damgaya
        // bakılıyordu). Otorite artık fatura_kalemleri.hareket_id'dir.
        var bag = await FaturaHareketBagi.IlkBag(db, k.TenantId, [h]);
        Assert.NotNull(bag);
        Assert.Equal(FaturaHareketBagi.KaynakKalem, bag!.Kaynak);
    }

    [Fact]
    public async Task Mutabakat_yalniz_KALEMLE_bagli_satisi_FATURALI_sayar()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 mutabakat");
        using var db = vt.Olustur(k);

        var gun = DateTime.UtcNow;
        var h = Taslak(k, "SATIS");
        h.Tarih = gun;
        HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(1_000m), h);
        var f = FaturaYaz(db, k, "FTR-2026-000908");
        f.Tarih = gun;
        KalemYaz(db, k, f, h.Id);                   // damga YOK, yalnız kalem bağı
        db.SaveChanges();

        var rapor = TestKurulum.RaporServisi(db);
        var cevap = await rapor.PosFaturaMutabakati(
            Kuyumcu.Api.Common.Zaman.UtcdenYerele(gun).ToString("yyyy-MM-dd"), k.Islem);

        // ESKİ HÂLDE bu satış "faturasız" görünüyordu (yalnız fatura_no metnine
        // bakılıyordu) ve gün sonu FARK'ı sahte bir sapma gösteriyordu.
        Assert.Equal(1m, TestKurulum.Sayi(cevap, "satis", "faturaliSayisi"));
        Assert.Equal(0m, TestKurulum.Sayi(cevap, "satis", "faturasizSayisi"));
        Assert.Equal(1m, TestKurulum.Sayi(cevap, "faturaEslesme", "bagliSayisi"));
        Assert.Equal(0m, TestKurulum.Sayi(cevap, "faturaEslesme", "bagimsizSayisi"));
    }

    [Fact]
    public async Task Satis_hareketi_adedi_DEFTERE_yazar_ve_iade_onu_TURETMEZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 satış adedi");
        using var db = vt.Olustur(k);

        var satis = new Kuyumcu.Api.Modules.Satis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 3m, BirimFiyatTl = 1_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        };
        await new Kuyumcu.Api.Modules.Satis.SatisServisi(
            db, Microsoft.Extensions.Logging.Abstractions.NullLogger<
                    Kuyumcu.Api.Modules.Satis.SatisServisi>.Instance)
            .Satis(satis, k.Islem);

        var h = db.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "SATIS");
        // Adet artık DEFTERDE durur; |has| ÷ birim has türetmesine gerek yok.
        Assert.Equal(3m, h.Adet);
    }

    [Fact]
    public async Task Bag_kume_DISINDAKI_faturaya_BAGLANMAZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P08 küme sınırı");
        using var db = vt.Olustur(k);

        var h = Taslak(k, "SATIS");
        HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(1_000m), h);
        var f = FaturaYaz(db, k, "FTR-2026-000905");
        KalemYaz(db, k, f, h.Id);
        db.SaveChanges();

        // Gün sonu mutabakatı "o günün faturaları"nı sorar; küme BOŞ verilirse
        // bağ da boş olmalıdır — ertesi gün kesilen fatura bugünü değiştirmez.
        var bag = await FaturaHareketBagi.Coz(db, k.TenantId, [h], []);
        Assert.Empty(bag);
    }
}
