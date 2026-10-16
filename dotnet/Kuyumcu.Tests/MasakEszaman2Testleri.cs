using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MBanka = Kuyumcu.Api.Modules.Banka;
using MCari = Kuyumcu.Api.Modules.Cari;
using MEvrak = Kuyumcu.Api.Modules.Evrak;
using MMasak = Kuyumcu.Api.Modules.Masak;
using MSatinalma = Kuyumcu.Api.Modules.Satinalma;
using MSatis = Kuyumcu.Api.Modules.Satis;
using MSiparis = Kuyumcu.Api.Modules.Siparis;
using MTevsik = Kuyumcu.Api.Modules.Tevsik;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET MASAK-ESZAMAN-2 — MASAK-ESZAMAN tamamlama.
//
// (1) Kilit + kilit altı yineleme eklenmiş ama eşzamanlı testi OLMAYAN 10 yol + siparişi satışa
//     çevirmenin kendi MASAK testi (ME20–ME30). Kalıp MasakEszamanTestleri ile aynı: aynı cariye
//     N = 10 eşzamanlı istek (ayrı DbContext = ayrı MySQL bağlantısı, başlangıç kapısı), her biri
//     tek başına eşik ALTI, toplamı eşik ÜSTÜ, hiçbiri beyanlı değil → eşiği aşanlar beyansız RED,
//     kabul edilenlerin toplamı eşiğin ALTINDA, bir istek daha eşiği aşardı.
//     ⚠ Bazı yollarda tek işlem tutarı küçüktür (döviz tahsilatın tevsiki, hurda stoku): MASAK eşiği
//     için cari önce "eşiğe 2,5 işlem kalacak kadar" HAVALE tahsilatıyla DOLDURULUR (Doldur) — eşik
//     sayısı yine teste gömülmez, servislerin varsayılan motorundan okunur.
//     ⚠ İADELER: SATIS_IADE/ALIS_IADE MASAK kümülatifine GİRMEZ (MasakKapisi.KapiyaTabiTurler) →
//     iade, MASAK toplamını büyütmez; yarış yalnız TEVSİK (nakit iade = tediye/tahsil, nakit_tutar
//     dolu) tarafındadır. İade testleri bu yüzden tevsik haddiyle kurulur.
// (2) Peşin Müşteri art arda fiş pencere sorusu kilit altında yinelenir (ME31, ME32).
// (3) Dekont eşleştirmede DEKONT satırı kilitlenir: aynı dekont iki müşteriye → 1 başarı (ME33).
// (4) Satınalma mal kabulü dış işlemi READ COMMITTED: eşzamanlı aynı tedarikçi kümülatifinde
//     tazelik ihlali (500) yerine doğru karar (ME34 MASAK 400, ME35 kabul).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MasakEszaman2Testleri(TestVeritabani vt)
{
    private const int N = 10;
    private const decimal Tek = 40_000m;        // 10 × 40.000 = 400.000 (her biri tek başına eşik altı)
    private const decimal TekNakit = 5_000m;    // 10 × 5.000 = 50.000 nakit
    private const decimal UsdKur = 40m;
    /// <summary>Has fiyatı — fikstür kurunun (6.827,20) yakını: ISLEM-BANT fiyat bandının İÇİNDE.</summary>
    private const decimal HasFiyati = 6_800m;

    /// <summary>Servislerin varsayılan motorunun kimlik isteyen en düşük tutarı (tek kaynak).</summary>
    private static readonly decimal MasakEsigi =
        new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri()).Esikler.KimlikIstenenEnDusukTutar;

    /// <summary>Tevsik haddi — varsayılan kural (tek kaynak).</summary>
    private static readonly decimal TevsikHaddi = new MTevsik.TevsikEsikleri().Had;

    private static readonly System.Text.Json.JsonSerializerOptions Web =
        new(System.Text.Json.JsonSerializerDefaults.Web);

    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));

    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MSiparis.SiparisServisi Siparis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSiparis.SiparisServisi>.Instance, Cari(db));

    private static MEvrak.EvrakServisi Evrak(KuyumcuDbContext db) =>
        new(db, NullLogger<MEvrak.EvrakServisi>.Instance, TestKurulum.Denetim(db), MTevsik.TevsikKapisi.Varsayilan());

    private static MSatinalma.SatinalmaServisi Satinalma(KuyumcuDbContext db) =>
        new(db, TestKurulum.Denetim(db), Satis(db), NullLogger<MSatinalma.SatinalmaServisi>.Instance);

    private Kiraci Kur(string ad)
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETMASAK-ESZAMAN-2-TEST " + ad, stokAdet: 50m);
        sistem.SaveChanges();
        return k;
    }

    private static string CariEkle(KuyumcuDbContext db, Kiraci k, string ad)
    {
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Cariler.Add(new Kuyumcu.Api.Data.Cari
        {
            Id = id, TenantId = k.TenantId, Tur = "MUSTERI", Ad = ad, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.CariBakiyeler.Add(new CariBakiye { CariId = id, TenantId = k.TenantId, TlBakiye = 0m, HasGramBakiye = 0m, Guncellendi = now });
        db.SaveChanges();
        return id;
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

    private static string Ozet(IEnumerable<Exception?> hatalar) =>
        string.Join(" | ", hatalar.Select(e => e!.GetType().Name + ": " + e.Message + " → " + e.InnerException?.Message));

    /// <summary>
    /// Cariyi bugünkü MASAK toplamı "eşik − 2,5 × tek" olacak kadar HAVALE tahsilatıyla doldurur:
    /// 2 işlem daha eşik altında kalır, 3. işlem eşiği aşar. Eşik sayısı teste gömülmez.
    /// </summary>
    private async Task Doldur(Kiraci k, decimal tek)
    {
        using var db = vt.OlusturSistem();
        var simdi = await MMasak.MasakKapisi.GunlukIslemToplam(db, k.TenantId, k.CariId, DateTime.UtcNow);
        var dolgu = Math.Round(MasakEsigi - simdi - 2.5m * tek, 2);
        Assert.True(dolgu > 0m && simdi + dolgu < MasakEsigi, $"dolgu kurulamadı (şimdi {simdi}, eşik {MasakEsigi})");
        await Cari(db).TahsilatGirHareket(k.CariId, new MCari.TahsilatGirisDto
        {
            TlTutari = dolgu, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
    }

    /// <summary>
    /// MASAK: reddedilenlerin HEPSİ MASAK reddi (başka hata yok), kabul edilenlerin toplamı eşik
    /// ALTINDA, bir istek daha eşiği aşardı → bölme yok ve kapı gereğinden sıkı da değil.
    /// </summary>
    private async Task MasakKorundu(List<Exception?> sonuc, Kiraci k, decimal tek)
    {
        var beklenmeyen = sonuc.Where(e => e is not null
            && !(e is BadRequestException b && b.Message.StartsWith("MASAK"))).ToList();
        Assert.True(beklenmeyen.Count == 0, "Beklenmeyen istisna: " + Ozet(beklenmeyen));

        using var kontrol = vt.OlusturSistem();
        var toplam = await MMasak.MasakKapisi.GunlukIslemToplam(kontrol, k.TenantId, k.CariId, DateTime.UtcNow);
        Assert.True(KabulSayisi(sonuc) >= 1, "hiç kabul yok");
        Assert.True(toplam < MasakEsigi, $"kabul edilenlerin toplamı {toplam} eşiğe ({MasakEsigi}) ulaştı — beyansız eşik bölündü");
        Assert.True(toplam + tek >= MasakEsigi, $"toplam {toplam} + {tek} < eşik — kapı gereğinden fazla reddetti");
    }

    private async Task TevsikKorundu(List<Exception?> sonuc, Kiraci k, decimal tek)
    {
        var beklenmeyen = sonuc.Where(e => e is not null
            && !(e is BadRequestException b && AlanIsareti.Kod(b) == MTevsik.TevsikKurali.RedKodu)).ToList();
        Assert.True(beklenmeyen.Count == 0, "Beklenmeyen istisna: " + Ozet(beklenmeyen));

        using var kontrol = vt.OlusturSistem();
        var toplam = await MTevsik.TevsikKapisi.GunlukNakitToplam(kontrol, k.TenantId, k.CariId, DateTime.UtcNow);
        Assert.True(KabulSayisi(sonuc) >= 1, "hiç kabul yok");
        Assert.True(toplam <= TevsikHaddi, $"kabul edilen nakit {toplam} haddi ({TevsikHaddi}) aştı — tevsik bölündü");
        Assert.True(toplam + tek > TevsikHaddi, $"nakit {toplam} + {tek} ≤ had — kapı gereğinden fazla reddetti");
    }

    private static async Task<List<string>> HareketIdleri(KuyumcuDbContext db, Kiraci k, string tur) =>
        await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == tur).OrderBy(h => h.Id).Select(h => h.Id).ToListAsync();

    // ── ME20 Döviz veresiye (MASAK — TL karşılığı) ───────────────────────────────
    [Fact]
    public async Task ME20_on_paralel_doviz_veresiye_masak_esigi_bolunmez()
    {
        var k = Kur("ME20");
        using (var db0 = vt.OlusturSistem()) TestKurulum.KurDoviz(db0, k.KurId, usd: UsdKur);
        var sonuc = await Paralel((db, _) => Cari(db).DovizVeresiyeGir(k.CariId, new MCari.DovizVeresiyeDto
        {
            ParaBirimi = "USD", Tutar = Tek / UsdKur, KurSnapshotId = k.KurId,
        }, k.Islem));
        await MasakKorundu(sonuc, k, Tek);
    }

    // ── ME21 Döviz tahsilat (efektif = nakit; tevsik hadden önce MASAK eşiği dolgu ile) ─────
    [Fact]
    public async Task ME21_on_paralel_doviz_tahsilat_masak_esigi_bolunmez()
    {
        var k = Kur("ME21");
        string usdKasa;
        using (var db0 = vt.OlusturSistem())
        {
            TestKurulum.KurDoviz(db0, k.KurId, usd: UsdKur);
            usdKasa = TestKurulum.KasaEkle(db0, k.TenantId, k.SubeId, "USD");
        }
        const decimal tekTl = 4_000m;   // 100 USD × 40 — 3 işlem tevsik haddinin altında
        await Doldur(k, tekTl);
        var sonuc = await Paralel((db, _) => Cari(db).DovizTahsilatGir(k.CariId, new MCari.DovizTahsilatDto
        {
            ParaBirimi = "USD", Tutar = tekTl / UsdKur, KasaId = usdKasa, KurSnapshotId = k.KurId,
        }, k.Islem));
        await MasakKorundu(sonuc, k, tekTl);
    }

    // ── ME22 Alış fişi (MASAK) ─────────────────────────────────────────────────
    [Fact]
    public async Task ME22_on_paralel_alis_fisi_masak_esigi_bolunmez()
    {
        var k = Kur("ME22");
        var sonuc = await Paralel((db, _) => Satis(db).AlisFisi(new MSatis.AlisFisiDto
        {
            CariId = k.CariId, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            Kalemler = [new MSatis.AlisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = Tek }],
        }, k.Islem));
        await MasakKorundu(sonuc, k, Tek);
    }

    // ── ME23 Hurda satış (MASAK — kıymetli maden çıkışı) ───────────────────────────
    [Fact]
    public async Task ME23_on_paralel_hurda_satis_masak_esigi_bolunmez()
    {
        var k = Kur("ME23");
        using (var db0 = vt.OlusturSistem())
        {
            // Hurda stoku BAŞKA cariden (mükellef satıcı) girer — bu carinin kümülatifini kirletmez.
            var tedarikci = CariEkle(db0, k, "ME23 hurda tedarikçisi");
            var satici = TestKurulum.HurdaSatici(SaticiTipi.Mukellef);
            satici.TedarikciFaturaNo = "FTR-ME23";
            await Satis(db0).HurdaAlis(new MSatis.HurdaAlisDto
            {
                CariId = tedarikci, AyarId = "K22", GramAgirlik = 20m, BirimFiyatTlHasGram = HasFiyati,
                OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, Satici = satici,
            }, k.Islem);
        }
        const decimal tek = HasFiyati;   // 1 has × has fiyatı
        await Doldur(k, tek);
        var sonuc = await Paralel((db, _) => Satis(db).HurdaSatis(new MSatis.HurdaSatisDto
        {
            Maden = "ALTIN", HasGrami = 1m, BirimFiyatTlHasGram = tek, CariId = k.CariId,
            OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        await MasakKorundu(sonuc, k, tek);
    }

    // ── ME24 Satış iadesi — NAKİT iade (tediye) tevsik haddi; 10 FARKLI kaynak satış ─────
    [Fact]
    public async Task ME24_on_paralel_nakit_satis_iadesi_tevsik_haddi_bolunmez()
    {
        var k = Kur("ME24");
        List<string> kaynaklar;
        using (var db0 = vt.OlusturSistem())
        {
            for (var i = 0; i < N; i++)
                await Satis(db0).Satis(new MSatis.SatisOlusturDto
                {
                    CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = TekNakit,
                    OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
                }, k.Islem);
            kaynaklar = await HareketIdleri(db0, k, "SATIS");
        }
        Assert.Equal(N, kaynaklar.Count);
        var sonuc = await Paralel((db, i) => Satis(db).SatisIade(new MSatis.IadeDto
        {
            KaynakHareketId = kaynaklar[i], Adet = 1m, OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        await TevsikKorundu(sonuc, k, TekNakit);
    }

    // ── ME25 Alış iadesi — NAKİT iade (tahsil) tevsik haddi; 10 FARKLI kaynak alış ───────
    [Fact]
    public async Task ME25_on_paralel_nakit_alis_iadesi_tevsik_haddi_bolunmez()
    {
        var k = Kur("ME25");
        List<string> kaynaklar;
        using (var db0 = vt.OlusturSistem())
        {
            for (var i = 0; i < N; i++)
                await Satis(db0).Alis(new MSatis.AlisOlusturDto
                {
                    CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = TekNakit,
                    OdemeTuru = "HAVALE", OdenenTl = TekNakit, KasaId = k.KasaId, KurSnapshotId = k.KurId,
                }, k.Islem);
            kaynaklar = await HareketIdleri(db0, k, "ALIS");
        }
        Assert.Equal(N, kaynaklar.Count);
        var sonuc = await Paralel((db, i) => Satis(db).AlisIade(new MSatis.IadeDto
        {
            KaynakHareketId = kaynaklar[i], Adet = 1m, OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        await TevsikKorundu(sonuc, k, TekNakit);
    }

    // ── ME26 Takas (MASAK ölçüsü = max(eski, yeni)) — hasım MUT3 bu yolu yakalayamamıştı ─────
    [Fact]
    public async Task ME26_on_paralel_takas_masak_esigi_bolunmez()
    {
        var k = Kur("ME26");
        var sonuc = await Paralel((db, i) =>
        {
            var satici = TestKurulum.HurdaSatici(SaticiTipi.Mukellef);
            satici.TedarikciFaturaNo = "FTR-ME26-" + i;
            return Satis(db).Takas(new MSatis.TakasDto
            {
                Satici = satici, CariId = k.CariId, EskiUrunAyarId = "K22", EskiUrunGram = 1m,
                EskiUrunBirimFiyatTlHasGram = HasFiyati,
                YeniUrunId = k.UrunId, YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = Tek,
                OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);
        });
        await MasakKorundu(sonuc, k, Tek);
    }

    // ── ME27 Avans iadesi (NAKİT tediye) — tevsik ───────────────────────────────
    [Fact]
    public async Task ME27_on_paralel_avans_iadesi_tevsik_haddi_bolunmez()
    {
        var k = Kur("ME27");
        using (var db0 = vt.OlusturSistem())
            await Satis(db0).AvansAl(new MSatis.AvansAlDto
            {
                CariId = k.CariId, Tutar = N * TekNakit, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);
        var sonuc = await Paralel((db, _) => Satis(db).AvansIade(new MSatis.AvansIadeDto
        {
            CariId = k.CariId, Tutar = TekNakit, KasaId = k.KasaId, KurSnapshotId = k.KurId,
            Gerekce = "Eşzamanlı avans iadesi sınaması",
        }, k.Islem));
        await TevsikKorundu(sonuc, k, TekNakit);
    }

    // ── ME28 Değişim (nakit fark) — tevsik; 10 FARKLI kaynak satış ───────────────────
    [Fact]
    public async Task ME28_on_paralel_degisim_nakit_farki_tevsik_haddi_bolunmez()
    {
        var k = Kur("ME28");
        const decimal kaynakFiyat = 1_000m;
        List<string> kaynaklar;
        using (var db0 = vt.OlusturSistem())
        {
            for (var i = 0; i < N; i++)
                await Satis(db0).Satis(new MSatis.SatisOlusturDto
                {
                    CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = kaynakFiyat,
                    OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
                }, k.Islem);
            kaynaklar = await HareketIdleri(db0, k, "SATIS");
        }
        Assert.Equal(N, kaynaklar.Count);
        var sonuc = await Paralel((db, i) => Satis(db).Degisim(new MSatis.DegisimDto
        {
            KaynakHareketId = kaynaklar[i], GeriAlinanAdet = 1m, YeniUrunId = k.UrunId, YeniAdet = 1m,
            YeniBirimFiyatTl = kaynakFiyat + TekNakit, FarkOdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        await TevsikKorundu(sonuc, k, TekNakit);
    }

    // ── ME29 Evrak (senet) nakit tahsili — tevsik + taksit; 10 FARKLI evrak ─────────────
    [Fact]
    public async Task ME29_on_paralel_evrak_nakit_tahsili_tevsik_haddi_bolunmez()
    {
        var k = Kur("ME29");
        var evraklar = new List<string>();
        using (var db0 = vt.OlusturSistem())
            for (var i = 0; i < N; i++)
            {
                var e = await Evrak(db0).Gir(new MEvrak.EvrakGirisDto
                {
                    EvrakTuru = "SENET", Tutar = TekNakit, VadeGunu = Zaman.YerelBugun().AddDays(30),
                    EvrakNo = "ME29-" + i, KaynakCariId = k.CariId, KurSnapshotId = k.KurId,
                }, k.Islem);
                evraklar.Add(e.Evrak.Id);
            }
        var sonuc = await Paralel((db, i) => Evrak(db).TahsilEdildi(evraklar[i],
            new MEvrak.TahsilEdildiDto { Hedef = "KASA", KasaId = k.KasaId, KurSnapshotId = k.KurId }, k.Islem));
        await TevsikKorundu(sonuc, k, TekNakit);
    }

    // ── ME30 Siparişi satışa çevir — kendi eşzamanlı MASAK testi (10 FARKLI sipariş, aynı cari) ──
    [Fact]
    public async Task ME30_on_paralel_siparisi_satisa_cevir_masak_esigi_bolunmez()
    {
        var k = Kur("ME30");
        var kalemler = new List<(string Siparis, string Kalem)>();
        using (var db0 = vt.OlusturSistem())
            for (var i = 0; i < N; i++)
            {
                var y = System.Text.Json.JsonSerializer.SerializeToElement(await Siparis(db0).Olustur(new MSiparis.SiparisOlusturDto
                {
                    Tur = SiparisTuru.MusteriSiparisi, CariId = k.CariId, SubeId = k.SubeId, Aciklama = "eşzamanlı satışa çevir",
                    Kalemler = [new MSiparis.SiparisKalemDto { UrunId = k.UrunId, Adet = 1m }],
                }, k.Islem), Web);
                var id = y.GetProperty("id").GetString()!;
                await Siparis(db0).Gecis(id, new MSiparis.SiparisGecisDto { YeniDurum = SiparisDurumu.Onaylandi }, k.Islem);
                await Siparis(db0).Gecis(id, new MSiparis.SiparisGecisDto { YeniDurum = SiparisDurumu.Hazir }, k.Islem);
                var kalem = await db0.SiparisKalemleri.AsNoTracking().Where(x => x.SiparisId == id).Select(x => x.Id).FirstAsync();
                kalemler.Add((id, kalem));
            }
        var sonuc = await Paralel((db, i) => Satis(db).SiparisiSatisaCevir(kalemler[i].Siparis, new MSatis.SiparisSatisaCevirDto
        {
            KurSnapshotId = k.KurId, VadeGun = 0,
            Kalemler = [new MSatis.SiparisSatisaCevirKalemDto { SiparisKalemId = kalemler[i].Kalem, BirimFiyatTl = Tek }],
            Odemeler = [new MSatis.OdemeBacagiDto { Arac = "HAVALE", TutarTl = Tek, KasaId = k.KasaId }],
        }, k.Islem));
        await MasakKorundu(sonuc, k, Tek);
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(KabulSayisi(sonuc), await kontrol.Siparisler.CountAsync(s => s.TenantId == k.TenantId && s.Durum == SiparisDurumu.TeslimEdildi));
    }

    // ── ME31 Peşin Müşteri art arda fiş: 10 eşzamanlı Peşin NAKİT satış, beyansız → pencere sorusu
    //    kilit altında yinelenir: YALNIZ 1 fiş geçer, 9'u "aynı müşteri mi?" sorusuyla 400 ──
    [Fact]
    public async Task ME31_on_paralel_pesin_fis_pencere_sorusu_kilit_altinda_sorulur()
    {
        var k = Kur("ME31");
        string pesin;
        using (var db0 = vt.OlusturSistem())
            pesin = (await MCari.SistemCarisi.GetirVeyaOlustur(db0, TestKurulum.Denetim(db0), k.Islem)).Id;
        var tutar = Math.Round(TevsikHaddi * 2m / 3m, 2);   // tek başına had altı; iki fiş haddi aşar
        var sonuc = await Paralel((db, _) => Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = pesin, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = tutar,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        var beklenmeyen = sonuc.Where(e => e is not null
            && !(e is BadRequestException b && AlanIsareti.Kod(b) == MMasak.PesinPencereKapisi.SoruKodu)).ToList();
        Assert.True(beklenmeyen.Count == 0, "Beklenmeyen istisna: " + Ozet(beklenmeyen));
        Assert.Equal(1, KabulSayisi(sonuc));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.CariId == pesin && h.Tur == "SATIS"));
    }

    // ── ME32 Peşin beyan "başka müşteri" (false): önceki fişten sonra 10 eşzamanlı fiş — hepsi geçer,
    //    her biri TEK denetim izi taşır (Faz 1 izi kilit altında yenilenir, çift iz yok) ──
    [Fact]
    public async Task ME32_pesin_baska_kisi_beyani_eszamanli_her_fise_tek_iz()
    {
        var k = Kur("ME32");
        string pesin;
        var tutar = Math.Round(TevsikHaddi * 2m / 3m, 2);
        using (var db0 = vt.OlusturSistem())
        {
            pesin = (await MCari.SistemCarisi.GetirVeyaOlustur(db0, TestKurulum.Denetim(db0), k.Islem)).Id;
            await Satis(db0).Satis(new MSatis.SatisOlusturDto
            {
                CariId = pesin, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = tutar,
                OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);
        }
        var sonuc = await Paralel((db, _) => Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = pesin, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = tutar,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            Masak = new MMasak.MasakBilgiDto { AyniKisiBeyani = false },
        }, k.Islem));
        Assert.True(sonuc.All(e => e is null), "Beklenmeyen istisna: " + Ozet(sonuc.Where(e => e is not null)));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(N, await kontrol.DenetimKayitlari.CountAsync(d => d.TenantId == k.TenantId
            && d.VarlikTuru == MMasak.PesinPencereKapisi.DenetimTuru));
    }

    // ── ME33 Aynı dekont iki FARKLI müşteriye eşzamanlı eşleştirilir → tek başarı, diğerleri 409 ──
    [Fact]
    public async Task ME33_ayni_dekont_iki_cariye_eszamanli_tek_eslesme()
    {
        var k = Kur("ME33");
        string hesapId, dekontId, ikinci;
        using (var db0 = vt.OlusturSistem())
        {
            ikinci = CariEkle(db0, k, "ME33 ikinci müşteri");
            hesapId = Kimlik.Ulid();
            db0.BankaHesaplari.Add(new BankaHesap
            {
                Id = hesapId, TenantId = k.TenantId, BankaAdi = "Test Bankası", HesapAdi = "Vadesiz",
                Iban = "TR200000000000000000000001", ParaBirimi = "TRY", Bakiye = 0m, Aktif = true,
                Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
            });
            db0.SaveChanges();
            await new MBanka.BankaServisi(db0).HareketEkleIc(hesapId, new MBanka.HareketEkleDto
            {
                Yon = HareketYonu.BankaGelen, Tutar = 1_000m, Aciklama = "müşterisiz dekont",
            }, k.Islem);
            dekontId = await db0.BankaHareketleri.AsNoTracking()
                .Where(h => h.TenantId == k.TenantId && h.BankaHesapId == hesapId).Select(h => h.Id).SingleAsync();
        }
        var cariler = new[] { k.CariId, ikinci };
        var sonuc = await Paralel((db, i) => new MBanka.BankaServisi(db, TestKurulum.Denetim(db)).DekontEslestir(hesapId, dekontId,
            new MBanka.DekontEslestirDto { CariId = cariler[i % 2] }, k.Islem));
        var beklenmeyen = sonuc.Where(e => e is not null && e is not ConflictException).ToList();
        Assert.True(beklenmeyen.Count == 0, "Beklenmeyen istisna: " + Ozet(beklenmeyen));
        Assert.Equal(1, KabulSayisi(sonuc));

        var kazanan = cariler[sonuc.FindIndex(e => e is null) % 2];
        using var kontrol = vt.OlusturSistem();
        var d = await kontrol.BankaHareketleri.AsNoTracking().SingleAsync(h => h.Id == dekontId);
        Assert.Equal("ESLESTI", d.Durum);
        Assert.Equal(kazanan, d.CariId);
        Assert.Equal(1, await kontrol.DenetimKayitlari.CountAsync(x => x.TenantId == k.TenantId
            && x.VarlikTuru == "BankaHareketiEslestirme" && x.VarlikId == dekontId));
    }

    // ── Satınalma mal kabulü: dış işlem READ COMMITTED. Tedarikçi carisi başka bağlantıda kilitliyken
    //    mal kabul Faz 1'i okur ve kilitte bekler; kilit sahibi aynı tedarikçiye tahsilat commit eder.
    //    Mal kabul kilidi alınca GÜNCEL toplamı görür ve doğru karar verir (eskiden: tazelik ihlali →
    //    InvalidOperationException → 500). ──
    private async Task<(Task Gorev, IDbContextTransactionTutucu Tutucu)> MalKabulKilitteBekler(Kiraci k, decimal tutucuTutar)
    {
        string id, kalemId;
        using (var db0 = vt.OlusturSistem())
        {
            var s = await Satinalma(db0).Olustur(new MSatinalma.SatinalmaOlusturDto
            {
                TedarikciCariId = k.CariId, SubeId = k.SubeId, Aciklama = "eşzamanlı mal kabul",
                Kalemler = [new MSatinalma.SatinalmaKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = Tek }],
            }, k.Islem);
            id = System.Text.Json.JsonSerializer.SerializeToNode(s, s.GetType(), Web)!["id"]!.GetValue<string>();
            await Satinalma(db0).Onayla(id, k.Islem);
            kalemId = (await db0.SatinalmaKalemleri.AsNoTracking().FirstAsync(x => x.SatinalmaSiparisId == id)).Id;
        }
        var tutucuDb = vt.OlusturSistem();
        var tx = await tutucuDb.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
        await tutucuDb.Database.Kilitle(KilitTablosu.Cariler, k.CariId, k.TenantId);
        await Cari(tutucuDb).TahsilatGirHareket(k.CariId, new MCari.TahsilatGirisDto
        {
            TlTutari = tutucuTutar, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);   // devralınan işlemde yazılır, henüz commit YOK → mal kabulün Faz 1'i görmez

        var db1 = vt.OlusturSistem();
        var gorev = Task.Run(async () =>
        {
            try
            {
                await Satinalma(db1).MalKabul(id, new MSatinalma.MalKabulDto
                {
                    KurSnapshotId = k.KurId,
                    Kalemler = [new MSatinalma.MalKabulKalemDto { KalemId = kalemId, Adet = 1m }],
                }, k.Islem);
            }
            finally { db1.Dispose(); }
        });
        await Task.Delay(1_500);
        Assert.False(gorev.IsCompleted, "Mal kabul tedarikçi cari kilidini beklemeliydi: " + gorev.Exception?.InnerException?.Message);
        return (gorev, new IDbContextTransactionTutucu(tutucuDb, tx));
    }

    private sealed class IDbContextTransactionTutucu(KuyumcuDbContext db, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx)
    {
        public async Task CommitVeBirak()
        {
            await tx.CommitAsync();
            await tx.DisposeAsync();
            db.Dispose();
        }
    }

    [Fact]
    public async Task ME34_mal_kabul_kilit_sonrasi_esik_asilinca_masak_400_tazelik_500_yok()
    {
        var k = Kur("ME34");
        var (gorev, tutucu) = await MalKabulKilitteBekler(k, MasakEsigi - Tek / 2m);   // + 40.000 → eşik aşılır
        await tutucu.CommitVeBirak();
        var hata = await Assert.ThrowsAnyAsync<Exception>(() => gorev);
        Assert.True(hata is BadRequestException b && b.Message.StartsWith("MASAK"),
            "MASAK reddi bekleniyordu: " + hata.GetType().Name + ": " + hata.Message);
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "ALIS"));
        Assert.Equal(0, await kontrol.Irsaliyeler.CountAsync(i => i.TenantId == k.TenantId));
    }

    [Fact]
    public async Task ME35_mal_kabul_kilit_sonrasi_esik_altinda_yazilir()
    {
        var k = Kur("ME35");
        var (gorev, tutucu) = await MalKabulKilitteBekler(k, 1_000m);
        await tutucu.CommitVeBirak();
        await gorev;   // tazelik ihlali (InvalidOperationException) YOK
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "ALIS"));
        Assert.Equal(1, await kontrol.Irsaliyeler.CountAsync(i => i.TenantId == k.TenantId));
    }
}
