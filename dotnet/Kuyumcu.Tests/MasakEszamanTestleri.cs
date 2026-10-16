using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MBanka = Kuyumcu.Api.Modules.Banka;
using MCari = Kuyumcu.Api.Modules.Cari;
using MMasak = Kuyumcu.Api.Modules.Masak;
using MSatis = Kuyumcu.Api.Modules.Satis;
using MSiparis = Kuyumcu.Api.Modules.Siparis;
using MTevsik = Kuyumcu.Api.Modules.Tevsik;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET MASAK-ESZAMAN — kümülatif yasal kapılar (MASAK bağlantılı işlem · tevsik) eşzamanlı
// isteklerle BÖLÜNEMEZ. Her yol: aynı cariye N = 10 eşzamanlı istek (ayrı DbContext = ayrı MySQL
// bağlantısı, başlangıç kapısıyla aynı anda), her biri tek başına eşik ALTI, toplamı eşik ÜSTÜ,
// hiçbiri beyanlı değil. Beklenen: kabul edilenlerin günlük toplamı eşiğin ALTINDA ve bir istek
// daha eşiği aşardı (yani kabul sayısı en çok olabilecek kadar); geri kalanlar MASAK/tevsik reddi.
// Eşik sayısı teste GÖMÜLMEZ: servislerin kullandığı varsayılan motordan okunur (tek kaynak).
// Toplam, kapının kendi ölçüsüyle (MasakKapisi.GunlukIslemToplam / TevsikKapisi.GunlukNakitToplam) okunur.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MasakEszamanTestleri(TestVeritabani vt)
{
    private const int N = 10;
    private const decimal Tek = 40_000m;        // 10 × 40.000 = 400.000 (her biri tek başına eşik altı)
    private const decimal TekNakit = 5_000m;    // 10 × 5.000 = 50.000 nakit

    /// <summary>Servislerin varsayılan motorunun kimlik isteyen en düşük tutarı (tek kaynak).</summary>
    private static readonly decimal MasakEsigi =
        new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri()).Esikler.KimlikIstenenEnDusukTutar;

    /// <summary>Tevsik haddi — varsayılan kural (tek kaynak).</summary>
    private static readonly decimal TevsikHaddi = new MTevsik.TevsikEsikleri().Had;

    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));

    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MSiparis.SiparisServisi Siparis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSiparis.SiparisServisi>.Instance, Cari(db));

    private Kiraci Kur(string ad)
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETMASAK-ESZAMAN-TEST " + ad, stokAdet: 50m);
        sistem.SaveChanges();
        return k;
    }

    private async Task<List<Exception?>> Paralel(Func<KuyumcuDbContext, int, Task> is_)
    {
        var baglamlar = Enumerable.Range(0, N).Select(_ => vt.OlusturSistem()).ToList();
        try
        {
            var kapi = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var gorevler = baglamlar.Select((db, i) => Task.Run(async () =>
            {
                await kapi.Task;
                try { await is_(db, i); return (Exception?)null; }
                catch (Exception e) { return e; }
            })).ToList();
            kapi.SetResult();
            return [.. await Task.WhenAll(gorevler)];
        }
        finally
        {
            foreach (var b in baglamlar) b.Dispose();
        }
    }

    private static int KabulSayisi(List<Exception?> sonuc) => sonuc.Count(e => e is null);

    /// <summary>
    /// MASAK: reddedilenlerin HEPSİ MASAK reddi (başka hata yok), kabul edilenlerin toplamı eşik
    /// ALTINDA, bir istek daha eşiği aşardı → bölme yok ve kapı gereğinden sıkı da değil.
    /// </summary>
    private async Task MasakKorundu(List<Exception?> sonuc, Kiraci k, decimal tek, bool cakisma = false)
    {
        var beklenmeyen = sonuc.Where(e => e is not null
            && !(cakisma ? e is ConflictException c && c.Message.Contains("kimlik tespiti")
                         : e is BadRequestException b && b.Message.StartsWith("MASAK"))).ToList();
        Assert.True(beklenmeyen.Count == 0,
            "Beklenmeyen istisna: " + string.Join(" | ", beklenmeyen.Select(e => e!.GetType().Name + ": " + e.Message + " → " + e.InnerException?.Message)));

        using var kontrol = vt.OlusturSistem();
        var toplam = await MMasak.MasakKapisi.GunlukIslemToplam(kontrol, k.TenantId, k.CariId, DateTime.UtcNow);
        var kabul = KabulSayisi(sonuc);
        Assert.True(kabul >= 1, "hiç kabul yok");
        Assert.True(toplam < MasakEsigi, $"kabul edilenlerin toplamı {toplam} eşiğe ({MasakEsigi}) ulaştı — beyansız eşik bölündü");
        Assert.True(toplam + tek >= MasakEsigi, $"toplam {toplam} + {tek} < eşik — kapı gereğinden fazla reddetti");
        Assert.Equal(N - kabul, sonuc.Count(e => e is not null));
    }

    private async Task TevsikKorundu(List<Exception?> sonuc, Kiraci k, decimal tek)
    {
        var beklenmeyen = sonuc.Where(e => e is not null
            && !(e is BadRequestException b && AlanIsareti.Kod(b) == MTevsik.TevsikKurali.RedKodu)).ToList();
        Assert.True(beklenmeyen.Count == 0,
            "Beklenmeyen istisna: " + string.Join(" | ", beklenmeyen.Select(e => e!.GetType().Name + ": " + e.Message)));

        using var kontrol = vt.OlusturSistem();
        var toplam = await MTevsik.TevsikKapisi.GunlukNakitToplam(kontrol, k.TenantId, k.CariId, DateTime.UtcNow);
        Assert.True(KabulSayisi(sonuc) >= 1, "hiç kabul yok");
        Assert.True(toplam <= TevsikHaddi, $"kabul edilen nakit {toplam} haddi ({TevsikHaddi}) aştı — tevsik bölündü");
        Assert.True(toplam + tek > TevsikHaddi, $"nakit {toplam} + {tek} ≤ had — kapı gereğinden fazla reddetti");
    }

    // ── Cari tahsilat (HAVALE) — KAYNAK BULGU: 10 × 40.000 beyansız → 10 × 200 idi ──────────
    [Fact]
    public async Task ME01_on_paralel_havale_tahsilati_masak_esigi_bolunmez()
    {
        var k = Kur("ME01");
        var sonuc = await Paralel((db, _) => Cari(db).TahsilatGirHareket(k.CariId, new MCari.TahsilatGirisDto
        {
            TlTutari = Tek, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        await MasakKorundu(sonuc, k, Tek);
    }

    // ── Cari tahsilat (NAKİT) — tevsik haddi (459 s. VUK GT) eşzamanlı bölünmez ─────────────
    [Fact]
    public async Task ME02_on_paralel_nakit_tahsilat_tevsik_haddi_bolunmez()
    {
        var k = Kur("ME02");
        var sonuc = await Paralel((db, _) => Cari(db).TahsilatGirHareket(k.CariId, new MCari.TahsilatGirisDto
        {
            TlTutari = TekNakit, OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        await TevsikKorundu(sonuc, k, TekNakit);
    }

    [Fact]
    public async Task ME03_on_paralel_veresiye_masak_esigi_bolunmez()
    {
        var k = Kur("ME03");
        var sonuc = await Paralel((db, _) => Cari(db).VeresiyeGir(k.CariId, new MCari.VeresiyeGirisDto
        {
            TlTutari = Tek, KurSnapshotId = k.KurId,
        }, k.Islem));
        await MasakKorundu(sonuc, k, Tek);
    }

    [Fact]
    public async Task ME04_on_paralel_tek_kalem_satis_masak_esigi_bolunmez()
    {
        var k = Kur("ME04");
        var sonuc = await Paralel((db, _) => Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = Tek,
            OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        await MasakKorundu(sonuc, k, Tek);
    }

    [Fact]
    public async Task ME05_on_paralel_satis_fisi_masak_esigi_bolunmez()
    {
        var k = Kur("ME05");
        var sonuc = await Paralel((db, _) => Satis(db).SatisFisi(new MSatis.SatisFisiDto
        {
            CariId = k.CariId, KurSnapshotId = k.KurId,
            Kalemler = [new MSatis.SatisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = Tek }],
            Odemeler = [new MSatis.OdemeBacagiDto { Arac = "HAVALE", TutarTl = Tek, KasaId = k.KasaId }],
        }, k.Islem));
        await MasakKorundu(sonuc, k, Tek);
    }

    [Fact]
    public async Task ME06_on_paralel_alis_masak_esigi_bolunmez()
    {
        var k = Kur("ME06");
        var sonuc = await Paralel((db, _) => Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = Tek,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId,
        }, k.Islem));
        await MasakKorundu(sonuc, k, Tek);
    }

    [Fact]
    public async Task ME07_on_paralel_hurda_alis_masak_esigi_bolunmez()
    {
        var k = Kur("ME07");
        var sonuc = await Paralel((db, i) => Satis(db).HurdaAlis(new MSatis.HurdaAlisDto
        {
            CariId = k.CariId, AyarId = "K22", GramAgirlik = 10m, BirimFiyatTlHasGram = 4_400m,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId,
            // Mükellef satıcı: pusula numarası alınmaz (eşzamanlı pusula numarası çakışması bu paketin
            // konusu değil — rapor § açık kalanlar); MASAK ölçüsü aynı (brüt bedel).
            Satici = Mukellef(i),
        }, k.Islem));
        await MasakKorundu(sonuc, k, 40_304m);   // 9,16 has × 4.400 (brüt — MASAK ölçüsü)
    }

    private static Kuyumcu.Api.Modules.Hurda.HurdaSaticiDto Mukellef(int i)
    {
        var s = TestKurulum.HurdaSatici(Kuyumcu.Api.Data.SaticiTipi.Mukellef);
        s.TedarikciFaturaNo = "FTR-ME07-" + i;
        return s;
    }

    [Fact]
    public async Task ME08_on_paralel_iscilik_masak_esigi_bolunmez()
    {
        var k = Kur("ME08");
        var sonuc = await Paralel((db, _) => Satis(db).Iscilik(new MSatis.IscilikDto
        {
            CariId = k.CariId, IscilikTl = Tek, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        await MasakKorundu(sonuc, k, Tek);
    }

    // ── Sipariş kaporası: 10 FARKLI sipariş, aynı cari (sipariş kilidi serileştirmez — cari kilidi şart) ──
    [Fact]
    public async Task ME09_on_paralel_farkli_siparis_kaporasi_masak_esigi_bolunmez()
    {
        var k = Kur("ME09");
        var siparisler = new List<string>();
        using (var db0 = vt.OlusturSistem())
            for (var i = 0; i < N; i++)
            {
                var y = System.Text.Json.JsonSerializer.SerializeToElement(await Siparis(db0).Olustur(new MSiparis.SiparisOlusturDto
                {
                    Tur = SiparisTuru.MusteriSiparisi, CariId = k.CariId, SubeId = k.SubeId, Aciklama = "eşzamanlı",
                    Kalemler = [new MSiparis.SiparisKalemDto { Tanim = "bilezik", Adet = 1m }],
                }, k.Islem), new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
                siparisler.Add(y.GetProperty("id").GetString()!);
            }
        var sonuc = await Paralel((db, i) => Siparis(db).Kapora(siparisler[i], new MSiparis.SiparisKaporaDto
        {
            TlTutari = Tek, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        await MasakKorundu(sonuc, k, Tek);
    }

    private static string BankaHesabiAc(KuyumcuDbContext db, Kiraci k)
    {
        var id = Kimlik.Ulid();
        db.BankaHesaplari.Add(new BankaHesap
        {
            Id = id, TenantId = k.TenantId, BankaAdi = "Test Bankası", HesapAdi = "Vadesiz",
            Iban = "TR200000000000000000000001", ParaBirimi = "TRY", Bakiye = 0m, Aktif = true,
            Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        db.SaveChanges();
        return id;
    }

    [Fact]
    public async Task ME10_on_paralel_musterili_banka_dekontu_masak_esigi_bolunmez()
    {
        var k = Kur("ME10");
        string hesapId;
        using (var db0 = vt.OlusturSistem()) hesapId = BankaHesabiAc(db0, k);
        var sonuc = await Paralel((db, i) => new MBanka.BankaServisi(db).HareketEkleIc(hesapId, new MBanka.HareketEkleDto
        {
            Yon = HareketYonu.BankaGelen, Tutar = Tek, Aciklama = "eşzamanlı dekont " + i, CariId = k.CariId,
        }, k.Islem));
        await MasakKorundu(sonuc, k, Tek);
    }

    // ── Sonradan eşleştirme: 10 müşterisiz dekont aynı müşteriye AYNI ANDA bağlanır → 409 (yasal kimlik) ──
    [Fact]
    public async Task ME11_on_paralel_dekont_eslestirme_masak_esigi_bolunmez()
    {
        var k = Kur("ME11");
        string hesapId;
        List<string> dekontlar;
        using (var db0 = vt.OlusturSistem())
        {
            hesapId = BankaHesabiAc(db0, k);
            for (var i = 0; i < N; i++)
                await new MBanka.BankaServisi(db0).HareketEkleIc(hesapId, new MBanka.HareketEkleDto
                {
                    Yon = HareketYonu.BankaGelen, Tutar = Tek, Aciklama = "müşterisiz dekont " + i,
                }, k.Islem);
            dekontlar = db0.BankaHareketleri.AsNoTracking()
                .Where(h => h.TenantId == k.TenantId && h.BankaHesapId == hesapId).Select(h => h.Id).ToList();
        }
        Assert.Equal(N, dekontlar.Count);
        var sonuc = await Paralel((db, i) => new MBanka.BankaServisi(db).DekontEslestir(hesapId, dekontlar[i],
            new MBanka.DekontEslestirDto { CariId = k.CariId }, k.Islem));
        await MasakKorundu(sonuc, k, Tek, cakisma: true);
    }

    // ── Avans (NAKİT) — tevsik ────────────────────────────────────────────────
    [Fact]
    public async Task ME12_on_paralel_nakit_avans_tevsik_haddi_bolunmez()
    {
        var k = Kur("ME12");
        var sonuc = await Paralel((db, _) => Satis(db).AvansAl(new MSatis.AvansAlDto
        {
            CariId = k.CariId, Tutar = TekNakit, OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        await TevsikKorundu(sonuc, k, TekNakit);
    }

    // ── Satış (NAKİT) — tevsik; MASAK eşiğinin altında kalan tutarla ─────────────────────
    [Fact]
    public async Task ME13_on_paralel_nakit_satis_tevsik_haddi_bolunmez()
    {
        var k = Kur("ME13");
        var sonuc = await Paralel((db, _) => Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = TekNakit,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        await TevsikKorundu(sonuc, k, TekNakit);
    }

    // ── Yardımcı sözleşmesi ─────────────────────────────────────────────────────
    [Fact]
    public async Task ME14_cari_icin_baslat_read_committed_acar_ve_cari_satirini_kilitler()
    {
        var k = Kur("ME14");
        using var db1 = vt.OlusturSistem();
        await using var tx = await db1.Database.CariIcinBaslat(k.TenantId, k.CariId, DateTime.UtcNow);
        Assert.NotNull(tx);
        Assert.Equal(System.Data.IsolationLevel.ReadCommitted,
            Microsoft.EntityFrameworkCore.Storage.DbContextTransactionExtensions.GetDbTransaction(tx!).IsolationLevel);

        // İkinci bağlantı aynı cari satırını kilitlemeye çalışırsa BEKLER (NOWAIT → hemen hata).
        using var db2 = vt.OlusturSistem();
        await using var tx2 = await db2.Database.BeginTransactionAsync();
        await Assert.ThrowsAnyAsync<Exception>(() => db2.Database.ExecuteSqlRawAsync(
            "SELECT id FROM cariler WHERE id = {0} AND tenant_id = {1} FOR UPDATE NOWAIT", k.CariId, k.TenantId));
    }

    [Fact]
    public async Task ME15_kilit_alti_yineleme_islem_disinda_firlatir()
    {
        var k = Kur("ME15");
        using var db = vt.OlusturSistem();
        var masak = MMasak.MasakKapisi.Varsayilan();
        var faz1 = masak.Dogrula(Tek, null);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            masak.KilitAltindaYinele(db, k.TenantId, k.CariId, Tek, null, DateTime.UtcNow, faz1));
    }

    // ── Dış işlem REPEATABLE READ ve anlık görüntüsü kilitten ÖNCE kurulmuş: eşzamanlı commit
    //    edilmiş tahsilat görünmez → tazelik denetimi işlemi YAZMAZ (fail-closed) ──
    [Fact]
    public async Task ME16_eski_anlik_goruntulu_dis_islemde_yazmaz()
    {
        var k = Kur("ME16");
        using var db1 = vt.OlusturSistem();
        await using var dis = await db1.Database.BeginTransactionAsync();   // varsayılan (REPEATABLE READ)
        _ = await db1.Hareketler.AsNoTracking().CountAsync(h => h.TenantId == k.TenantId);   // anlık görüntü burada kurulur

        using (var db2 = vt.OlusturSistem())   // başka bağlantı aynı cariye tahsilat yazar ve commit eder
            await Cari(db2).TahsilatGirHareket(k.CariId, new MCari.TahsilatGirisDto
            {
                TlTutari = Tek, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);

        var hata = await Assert.ThrowsAsync<InvalidOperationException>(() => Cari(db1).TahsilatGirHareket(k.CariId,
            new MCari.TahsilatGirisDto { TlTutari = Tek, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId },
            k.Islem));
        Assert.Contains("MASAK-ESZAMAN", hata.Message);
    }

    // ── Dış işlem REPEATABLE READ ama arada başka commit YOK: tazelik denetimi geçer, işlem yazılır ──
    [Fact]
    public async Task ME17_taze_dis_islemde_tahsilat_yazilir()
    {
        var k = Kur("ME17");
        using var db1 = vt.OlusturSistem();
        await using var dis = await db1.Database.BeginTransactionAsync();
        _ = await db1.Hareketler.AsNoTracking().CountAsync(h => h.TenantId == k.TenantId);
        await Cari(db1).TahsilatGirHareket(k.CariId,
            new MCari.TahsilatGirisDto { TlTutari = Tek, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId },
            k.Islem);
        await dis.CommitAsync();
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT"));
    }

    // ── Eşik ALTI eşzamanlı istekler kilit yüzünden REDDEDİLMEZ (yalnız sıraya girer) ──
    [Fact]
    public async Task ME18_esik_alti_on_paralel_tahsilatin_hepsi_kabul()
    {
        var k = Kur("ME18");
        var sonuc = await Paralel((db, _) => Cari(db).TahsilatGirHareket(k.CariId, new MCari.TahsilatGirisDto
        {
            TlTutari = 1_000m, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        Assert.True(sonuc.All(e => e is null),
            "Beklenmeyen istisna: " + string.Join(" | ", sonuc.Where(e => e is not null).Select(e => e!.GetType().Name + ": " + e.Message)));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(10_000m, await MMasak.MasakKapisi.GunlukIslemToplam(kontrol, k.TenantId, k.CariId, DateTime.UtcNow));
    }
    // ── Kimlik taraması açık işlem içinde koşarsa (sipariş kaporası: tahsilat dış işleme katılır)
    //    TAM yasal liste eşleşmesinde eşleşme kaydı reddedilen işlemle birlikte KAYBOLMAZ ──
    [Fact]
    public async Task ME19_islem_icinde_tam_yaptirim_eslesmesi_kaydi_kalici_islem_yazilmaz()
    {
        var k = Kur("ME19");
        var tckn = Enumerable.Range(0, 100_000).Select(i => (23_456_700_000L + i).ToString())
            .First(Kuyumcu.Core.Domain.KimlikNoDogrulayici.TcknGecerli);
        string listeId = Kimlik.Ulid(), siparisId;
        using (var db0 = vt.OlusturSistem())
        {
            db0.YaptirimKayitlari.Add(new YaptirimKaydi
            {
                Id = listeId, Kaynak = "MASAK-ESZAMAN test", KaynakUrl = "https://masak.hmb.gov.tr/test",
                ListeTarihi = DateOnly.FromDateTime(DateTime.UtcNow), AdSoyad = "MEZAMAN TESTKISI " + listeId[..6],
                KimlikNo = AesGcmAlanSifreleyici.Normalize(tckn),
                KimlikNoHash = TestVeritabani.TestSifreleyici.Hashle(AesGcmAlanSifreleyici.Normalize(tckn)),
                Eklendi = DateTime.UtcNow,
                SifreSurumu = TestVeritabani.TestSifreleyici.Surum, Olusturan = "sistem:MASAK-ESZAMAN test",
            });
            db0.SaveChanges();
            var y = System.Text.Json.JsonSerializer.SerializeToElement(await Siparis(db0).Olustur(new MSiparis.SiparisOlusturDto
            {
                Tur = SiparisTuru.MusteriSiparisi, CariId = k.CariId, SubeId = k.SubeId, Aciklama = "yaptırım",
                Kalemler = [new MSiparis.SiparisKalemDto { Tanim = "bilezik", Adet = 1m }],
            }, k.Islem), new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
            siparisId = y.GetProperty("id").GetString()!;
        }
        try
        {
            using var db = vt.OlusturSistem();
            var hata = await Assert.ThrowsAsync<BadRequestException>(() => Siparis(db).Kapora(siparisId, new MSiparis.SiparisKaporaDto
            {
                TlTutari = 250_000m, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
                Masak = new MMasak.MasakBilgiDto
                {
                    BelgeTuru = KimlikBelgeTuru.TcKimlik, BelgeNo = tckn, AdSoyad = "Ayşe Demir",
                    DogumTarihi = new DateOnly(1985, 4, 12), Uyruk = "TUR",
                    IslemAmaci = "Yatırım", Aciklama = "Düğün için bilezik siparişi kaporası",
                    FonKaynagi = "Maaş birikimi", EkBelgeReferansi = "BELGE-2026-001",
                },
            }, k.Islem));
            Assert.Equal(MMasak.YaptirimServisi.EngelMesaji, hata.Message);

            using var kontrol = vt.OlusturSistem();
            Assert.Equal(1, await kontrol.YaptirimEslesmeleri.CountAsync(e => e.TenantId == k.TenantId && e.YaptirimKaydiId == listeId));
            Assert.Equal(0, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT"));
        }
        finally
        {
            using var temiz = vt.OlusturSistem();
            await temiz.Database.ExecuteSqlRawAsync("DELETE FROM yaptirim_eslesmeleri WHERE yaptirim_kaydi_id = {0}", listeId);
            await temiz.Database.ExecuteSqlRawAsync("DELETE FROM yaptirim_kayitlari WHERE id = {0}", listeId);
        }
    }
}
