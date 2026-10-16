using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Isler;
using Kuyumcu.Api.Modules.Isler.Isler;
using Kuyumcu.Api.Modules.Tevsik;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-2 (TB-16) — TEVSIK_KURTARMA: geç fark edilen nakit sınırı aşımı.
//
// DAYANAK (§ 3.1 M-6, birincil): VUK mük. md.355 — ödemeyi YAPAN 5 iş günü içinde
// kendiliğinden bildirirse ödeme yapan adına ceza kesilmez → yön (ODEME/TAHSILAT).
// FİKSTÜR: kapıyı kaçan satırlar (kural kapalı kiracı, eşzamanlı fiş, eski kayıt)
// deftere doğrudan yazılır — üretimde bunlar kapı kapalıyken/yarışta doğar.
// İş, üretimdeki IsCalistirici ile koşar; kapsam üretimdeki servisleri taşır.
// ═════════════════════════════════════════════════════════════════════════════

internal static class IhlalFikstur
{
    public static YasalParametreServisi Yasal(KuyumcuDbContext db) =>
        YasalParametreServisi.Yukleyicili(() =>
            db.YasalParametreler.AsNoTracking().Where(p => p.Id.StartsWith("yasal1-tohum-"))
              .AsEnumerable().Select(YasalParametreSatiri.Kaynaktan).ToList());

    /// <summary>Üretim DI'sinin iş için gerekli parçası (kiracıya sabit db).</summary>
    public static IsCalistirici Calistirici(KuyumcuDbContext db, IConfiguration? y = null)
    {
        y ??= IsCatisiKurulum.Yapilandirma();
        var yasal = Yasal(db);
        var s = new ServiceCollection();
        s.AddSingleton(y);
        s.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        s.AddScoped(_ => db);
        s.AddScoped<DenetimServisi>();
        s.AddScoped<IsAyarServisi>();
        s.AddSingleton(yasal);
        s.AddSingleton(new TevsikKurali(new TevsikEsikleri { Etkin = false }, yasal));
        s.AddSingleton(new TakvimAyarlari());
        var kapsam = s.BuildServiceProvider();
        return new IsCalistirici(db, [new TevsikKurtarmaIsi()], y, kapsam, new UygulamaIciKanal(),
            kapsam.GetRequiredService<IsAyarServisi>(), NullLogger<IsCalistirici>.Instance);
    }

    /// <summary>Nakit satır (kapıyı kaçmış): satış/alış, istenen şube ve anda.</summary>
    public static string Nakit(KuyumcuDbContext db, Kiraci k, decimal nakit, DateTime an,
        string tur = "SATIS", string? cariId = null, string? subeId = null, decimal? toplam = null)
    {
        var id = Kimlik.Ulid();
        var isaret = tur is "ALIS" or "HURDA_ALIS" ? -1m : 1m;
        db.Hareketler.Add(new Hareket
        {
            Id = id, TenantId = k.TenantId, SubeId = subeId ?? k.SubeId, Tur = tur, Tarih = an,
            TlTutari = isaret * (toplam ?? nakit), HasGrami = 0m, ParaBirimi = "TRY", KurSnapshotId = k.KurId,
            CariId = cariId ?? k.CariId, KullaniciId = k.KullaniciId, FisId = Kimlik.Ulid(),
            OdemeTuru = toplam is null ? "NAKIT" : "KARMA", NakitTutar = nakit,
            Aciklama = "YASAL-2 fikstür", Olusturuldu = an,
        });
        db.SaveChanges();
        return id;
    }

    public static string Cari(KuyumcuDbContext db, Kiraci k, string ad, string? vergiNo = null)
    {
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Cariler.Add(new Cari { Id = id, TenantId = k.TenantId, Tur = "MUSTERI", Ad = ad, VergiNo = vergiNo, Aktif = true, Olusturuldu = now, Guncellendi = now });
        db.CariBakiyeler.Add(new CariBakiye { CariId = id, TenantId = k.TenantId, TlBakiye = 0m, HasGramBakiye = 0m, Guncellendi = now });
        db.SaveChanges();
        return id;
    }

    public static DateTime Ogle(DateOnly g) => Zaman.YerelGunAraligiUtc(g).Item1.AddHours(12);
}

public sealed class TevsikIhlalSafTestleri
{
    // ── K-11 — son gün: izleyen günden, hafta sonu + 29 Ekim atlanır, arife sayılır ──

    [Fact]
    public void K11_Cuma_23_10_2026_ihlalinin_son_gunu_02_11_2026()
    {
        var tatiller = ResmiTatiller.Aralik(2026, 2, new TakvimAyarlari());
        Assert.Equal(new DateOnly(2026, 11, 2),
            TaksitKurali.BildirimSonGunu(new DateOnly(2026, 10, 23), 5, tatiller));
    }

    [Fact]
    public void K11_tatilsiz_takvimde_ayni_ihlal_30_10_cikar_tatil_atlanmazsa_KISA_olur()
    {
        Assert.Equal(new DateOnly(2026, 10, 30),
            TaksitKurali.BildirimSonGunu(new DateOnly(2026, 10, 23), 5, new HashSet<DateOnly>()));
    }

    [Fact]
    public void Bildirim_metni_yone_gore_ODEME_son_gun_TAHSILAT_musavir()
    {
        var t = new TevsikIhlali
        {
            Yon = TevsikIhlalYonu.Odeme, IhlalGunu = new DateOnly(2026, 10, 1), BildirimSonGunu = new DateOnly(2026, 10, 7),
            NakitToplamTl = 35_000m, HadTl = 30_000m,
        };
        Assert.Equal("Nakit sınırı aşıldı — bildirim için son gün 07.10", TevsikIhlalTarayici.BildirimBasligi(t));
        Assert.Contains("ceza kesilmez", TevsikIhlalTarayici.BildirimGovdesi(t), StringComparison.Ordinal);
        t.Yon = TevsikIhlalYonu.Tahsilat;
        Assert.Contains("mali müşavirinize", TevsikIhlalTarayici.BildirimBasligi(t), StringComparison.Ordinal);
        Assert.DoesNotContain("ceza kesilmez", TevsikIhlalTarayici.BildirimGovdesi(t), StringComparison.Ordinal);
        Assert.DoesNotContain("tevsik", TevsikIhlalTarayici.BildirimGovdesi(t), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tur 2: sınırın ALTINDAKİ nakit payı (41.250,50'nin 11.250,50'si) bildirimde
    /// "bir kişiyle 11.250,50 TL nakit işlem var (sınır 30.000)" diye çelişkili yazılmaz;
    /// neden aşım sayıldığı türe göre söylenir.
    /// </summary>
    [Fact]
    public void Bildirim_govdesi_ISLEM_TUTARI_ve_TAKSIT_nedeni_soyler_GUNLUK_toplam_der()
    {
        var t = new TevsikIhlali
        {
            Tur = TevsikIhlalTuru.IslemTutari, Yon = TevsikIhlalYonu.Tahsilat, IhlalGunu = new DateOnly(2026, 9, 30),
            BildirimSonGunu = new DateOnly(2026, 10, 7), NakitToplamTl = 11_250.50m, HadTl = 30_000m,
        };
        var g = TevsikIhlalTarayici.BildirimGovdesi(t);
        Assert.Contains("sınırı (30.000,00 TL) aşan bir işlemin 11.250,50 TL'si nakit", g, StringComparison.Ordinal);
        Assert.DoesNotContain("nakit işlem var (sınır", g, StringComparison.Ordinal);
        t.Tur = TevsikIhlalTuru.Taksit;
        Assert.Contains("aşan bir işlemin borcuna 11.250,50 TL nakit alındı", TevsikIhlalTarayici.BildirimGovdesi(t), StringComparison.Ordinal);
        t.Tur = TevsikIhlalTuru.GunlukKumulatif; t.NakitToplamTl = 35_000m;
        Assert.Contains("toplam 35.000,00 TL nakit işlem var (sınır 30.000,00 TL)", TevsikIhlalTarayici.BildirimGovdesi(t), StringComparison.Ordinal);
    }
}

[Collection("veritabani")]
public sealed class TevsikIhlalDbTestleri(TestVeritabani vt)
{
    private Kiraci YeniKiraci(string ad) => TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETYASAL2-TEST-" + ad);

    private async Task<IsKosumSonucu> Kostur(Kiraci k, DateTime? an = null, string? elle = null)
    {
        using var db = vt.Olustur(k.TenantId);
        var c = IhlalFikstur.Calistirici(db);
        return await c.Calistir(new TevsikKurtarmaIsi(), k.TenantId, an ?? DateTime.UtcNow, elle, default);
    }

    private List<TevsikIhlali> Ihlaller(Kiraci k)
    {
        using var db = vt.OlusturSistem();
        return db.TevsikIhlalleri.AsNoTracking().Where(t => t.TenantId == k.TenantId).ToList();
    }

    private List<Bildirim> Bildirimler(Kiraci k)
    {
        using var db = vt.OlusturSistem();
        return db.Bildirimler.AsNoTracking().Where(b => b.TenantId == k.TenantId).ToList();
    }

    // ── K-9 ──

    [Fact]
    public async Task K9_kural_kapali_kiracida_ayni_gun_20000_ve_15000_nakit_is_1_satir_GUNLUK_KUMULATIF_35000()
    {
        var k = YeniKiraci("K9");
        using (var db = vt.OlusturSistem())
        {
            IhlalFikstur.Nakit(db, k, 20_000m, DateTime.UtcNow.AddMinutes(-30));
            IhlalFikstur.Nakit(db, k, 15_000m, DateTime.UtcNow.AddMinutes(-10));
        }
        var s = await Kostur(k);
        Assert.Equal(IsDurum.Basarili, s.Durum);

        var t = Assert.Single(Ihlaller(k));
        Assert.Equal(TevsikIhlalTuru.GunlukKumulatif, t.Tur);
        Assert.Equal(35_000m, t.NakitToplamTl);
        Assert.Equal(30_000m, t.HadTl);
        Assert.Equal(TevsikIhlalYonu.Tahsilat, t.Yon);
        Assert.Equal(TevsikIhlalDurumu.Acik, t.Durum);
        Assert.Equal(2, System.Text.Json.JsonSerializer.Deserialize<string[]>(t.HareketIdleri)!.Length);
        var b = Assert.Single(Bildirimler(k));
        Assert.Equal(BildirimOnem.Kritik, b.Onem);
        Assert.Equal("SAHIP,MUHASEBE", b.GorunurRoller);
        Assert.Equal(t.Id, b.IlgiliVarlikId);
    }

    [Fact]
    public async Task Esik_altinda_ayni_gun_toplam_tam_had_ihlal_YOK()
    {
        var k = YeniKiraci("esik alti");
        using (var db = vt.OlusturSistem())
        {
            IhlalFikstur.Nakit(db, k, 20_000m, DateTime.UtcNow.AddMinutes(-30));
            IhlalFikstur.Nakit(db, k, 10_000m, DateTime.UtcNow.AddMinutes(-10));
        }
        await Kostur(k);
        Assert.Empty(Ihlaller(k));
        Assert.Empty(Bildirimler(k));
    }

    // ── K-9b — yön ──

    [Fact]
    public async Task K9b_vatandastan_35000_nakit_hurda_alisi_ODEME_son_gun_dolu_musteriye_35000_nakit_satis_TAHSILAT()
    {
        var k = YeniKiraci("K9b");
        string vatandas;
        using (var db = vt.OlusturSistem())
        {
            vatandas = IhlalFikstur.Cari(db, k, "Vatandaş Hurda");
            IhlalFikstur.Nakit(db, k, 35_000m, DateTime.UtcNow.AddMinutes(-20), "HURDA_ALIS", vatandas);
            IhlalFikstur.Nakit(db, k, 35_000m, DateTime.UtcNow.AddMinutes(-15), "SATIS");
        }
        await Kostur(k);

        var l = Ihlaller(k);
        Assert.Equal(2, l.Count);
        var odeme = Assert.Single(l, t => t.CariId == vatandas);
        Assert.Equal(TevsikIhlalYonu.Odeme, odeme.Yon);
        Assert.True(odeme.BildirimSonGunu > odeme.IhlalGunu);
        var tahsil = Assert.Single(l, t => t.CariId == k.CariId);
        Assert.Equal(TevsikIhlalYonu.Tahsilat, tahsil.Yon);

        var b = Bildirimler(k);
        Assert.Contains(b, x => x.IlgiliVarlikId == odeme.Id && x.Baslik.Contains("son gün", StringComparison.Ordinal));
        Assert.Contains(b, x => x.IlgiliVarlikId == tahsil.Id && x.Baslik.Contains("mali müşavirinize danışın", StringComparison.Ordinal)
                              && !x.Govde.Contains("iş günü", StringComparison.Ordinal));
    }

    // ── K-10 — tekillik ──

    [Fact]
    public async Task K10_is_ayni_gun_iki_kez_kosar_1_satir_1_bildirim()
    {
        var k = YeniKiraci("K10");
        using (var db = vt.OlusturSistem()) IhlalFikstur.Nakit(db, k, 40_000m, DateTime.UtcNow.AddMinutes(-5));

        var s1 = await Kostur(k);
        var s2 = await Kostur(k, elle: k.KullaniciId);
        Assert.Equal(IsDurum.Basarili, s2.Durum);
        Assert.Equal(1, s1.UretilenBildirim);
        Assert.Equal(0, s2.UretilenBildirim);
        Assert.Single(Ihlaller(k));
        Assert.Single(Bildirimler(k));
    }

    [Fact]
    public async Task K10_ayni_cari_gun_tur_ikinci_satir_VERITABANINDA_reddedilir()
    {
        var k = YeniKiraci("K10 db");
        TevsikIhlali Satir() => new()
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, Tur = TevsikIhlalTuru.GunlukKumulatif, Yon = TevsikIhlalYonu.Tahsilat,
            CariId = k.CariId, IhlalGunu = new DateOnly(2026, 9, 1), NakitToplamTl = 35_000m, HadTl = 30_000m,
            BildirimSonGunu = new DateOnly(2026, 9, 8), Olusturuldu = DateTime.UtcNow,
        };
        using var db = vt.Olustur(k.TenantId);
        db.TevsikIhlalleri.Add(Satir());
        await db.SaveChangesAsync();
        db.TevsikIhlalleri.Add(Satir());
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Contains("Duplicate", ex.InnerException?.Message ?? "", StringComparison.OrdinalIgnoreCase);
    }

    // ── K-11 (iş üzerinden) ──

    [Fact]
    public async Task K11_is_Cuma_23_10_2026_ihlaline_son_gun_02_11_2026_yazar()
    {
        var k = YeniKiraci("K11");
        var gun = new DateOnly(2026, 10, 23);
        using (var db = vt.OlusturSistem()) IhlalFikstur.Nakit(db, k, 45_000m, IhlalFikstur.Ogle(gun), "HURDA_ALIS");
        await Kostur(k, IhlalFikstur.Ogle(gun).AddHours(6));
        var t = Assert.Single(Ihlaller(k));
        Assert.Equal(new DateOnly(2026, 11, 2), t.BildirimSonGunu);
        Assert.Equal(TevsikIhlalYonu.Odeme, t.Yon);
    }

    // ── K-12 — süre dolar, kayıt kalır ──

    [Fact]
    public async Task K12_son_gun_gecince_ACIK_SURE_DOLDU_olur_kayit_SILINMEZ()
    {
        var k = YeniKiraci("K12");
        var bugun = Zaman.YerelBugun();
        using (var db = vt.OlusturSistem()) IhlalFikstur.Nakit(db, k, 50_000m, IhlalFikstur.Ogle(bugun.AddDays(-9)));
        var s = await Kostur(k);
        Assert.Equal(IsDurum.Basarili, s.Durum);
        var t = Assert.Single(Ihlaller(k));
        Assert.True(t.BildirimSonGunu < bugun);
        Assert.Equal(TevsikIhlalDurumu.SureDoldu, t.Durum);

        await Kostur(k, elle: k.KullaniciId);
        Assert.Single(Ihlaller(k));
    }

    // ── K-13 — kişisel veri yok ──

    [Fact]
    public async Task K13_bildirim_basligi_ve_govdesinde_ad_TCKN_belge_no_GECMEZ()
    {
        var k = YeniKiraci("K13");
        string cari;
        using (var db = vt.OlusturSistem())
        {
            cari = IhlalFikstur.Cari(db, k, "Zeynep Kaya", "12345678950");
            IhlalFikstur.Nakit(db, k, 33_000m, DateTime.UtcNow.AddMinutes(-3), "HURDA_ALIS", cari);
        }
        await Kostur(k);
        var b = Assert.Single(Bildirimler(k));
        foreach (var metin in new[] { b.Baslik, b.Govde })
        {
            Assert.DoesNotContain("Zeynep", metin, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Kaya", metin, StringComparison.OrdinalIgnoreCase);
            Assert.False(Regex.IsMatch(metin, @"\d{10,11}"), metin);
        }
        Assert.Equal(TevsikKurtarmaIsi.Rota, b.HedefRota);
    }

    // ── K-16 — şubeye bölünmez ──

    [Fact]
    public async Task K16_A_subesinde_20000_B_subesinde_15000_iki_tezgahtar_is_ihlali_BULUR()
    {
        var k = YeniKiraci("K16");
        string subeB;
        using (var db = vt.OlusturSistem())
        {
            subeB = TestKurulum.SubeEkle(db, k.TenantId, "Şube B");
            IhlalFikstur.Nakit(db, k, 20_000m, DateTime.UtcNow.AddMinutes(-40));
            IhlalFikstur.Nakit(db, k, 15_000m, DateTime.UtcNow.AddMinutes(-20), subeId: subeB);
        }
        await Kostur(k);
        var t = Assert.Single(Ihlaller(k));
        Assert.Equal(35_000m, t.NakitToplamTl);
    }

    /// <summary>
    /// Tur 2 (hakem [DÜŞÜK], şartname mutasyon 6): tarama ŞUBE SÜZGEÇLİ bağlamda
    /// koşturulursa B şubesindeki nakdi sessizce kaçırıp "ihlal yok" demez —
    /// fail-closed durur, satır yazmaz.
    /// </summary>
    [Fact]
    public async Task K16_sube_suzgecli_baglamda_tarama_SESSIZCE_eksik_saymaz_DURUR()
    {
        var k = YeniKiraci("K16 subeli");
        string subeB;
        using (var db = vt.OlusturSistem())
        {
            subeB = TestKurulum.SubeEkle(db, k.TenantId, "Şube B");
            IhlalFikstur.Nakit(db, k, 20_000m, DateTime.UtcNow.AddMinutes(-40));
            IhlalFikstur.Nakit(db, k, 15_000m, DateTime.UtcNow.AddMinutes(-20), subeId: subeB);
        }
        using (var sdb = vt.OlusturSubeli(k.TenantId, k.SubeId))
        {
            var kural = new TevsikKurali(new TevsikEsikleri { Etkin = false }, IhlalFikstur.Yasal(sdb));
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                TevsikIhlalTarayici.Tara(sdb, k.TenantId, DateTime.UtcNow, kural, _ => 5, null));
            Assert.Contains("şube süzgeci açık", ex.Message, StringComparison.Ordinal);
        }
        Assert.Empty(Ihlaller(k));
        // Aynı fikstür kiracı geneli bağlamda (üretim yolu) 35.000 bulur.
        await Kostur(k);
        Assert.Equal(35_000m, Assert.Single(Ihlaller(k)).NakitToplamTl);
    }

    /// <summary>
    /// Tur 2 (hakem [DÜŞÜK]): aynı nakit hem günlük toplam hem taksit ihlali olarak
    /// İKİ KEZ raporlanmaz — tek satır (GÜNLÜK), tek bildirim; taksit kanıtı o satırda.
    /// </summary>
    [Fact]
    public async Task Ayni_cari_gun_GUNLUK_varsa_TAKSIT_ikinci_satir_ACMAZ_kanit_birlesir()
    {
        var k = YeniKiraci("tek olay");
        string tahsilatId, kaynakId;
        using (var db = vt.OlusturSistem())
        {
            kaynakId = TaksitFikstur.Borc(db, k, 62_000m, DateTime.UtcNow.AddDays(-4));
            tahsilatId = Kimlik.Ulid();
            var an = DateTime.UtcNow.AddMinutes(-30);
            db.Hareketler.Add(new Hareket
            {
                Id = tahsilatId, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "TAHSILAT", Tarih = an,
                TlTutari = -20_000m, HasGrami = 0m, ParaBirimi = "TRY", KurSnapshotId = k.KurId, CariId = k.CariId,
                KullaniciId = k.KullaniciId, OdemeTuru = "NAKIT", NakitTutar = 20_000m, Olusturuldu = an,
            });
            db.CariBakiyeler.First(b => b.CariId == k.CariId).TlBakiye -= 20_000m;
            db.SaveChanges();
            IhlalFikstur.Nakit(db, k, 15_000m, DateTime.UtcNow.AddMinutes(-10));   // aynı gün toplam 35.000
        }
        await Kostur(k);
        var t = Assert.Single(Ihlaller(k));
        Assert.Equal(TevsikIhlalTuru.GunlukKumulatif, t.Tur);
        Assert.Equal(35_000m, t.NakitToplamTl);
        var ids = System.Text.Json.JsonSerializer.Deserialize<string[]>(t.HareketIdleri)!;
        Assert.Contains(tahsilatId, ids);
        Assert.Contains(kaynakId, ids);                 // büyük işlem kanıtı kaybolmaz
        Assert.Single(Bildirimler(k));
    }

    // ── ISLEM_TUTARI ve TAKSIT ──

    [Fact]
    public async Task Islem_tutari_40000_fisin_10000_nakit_payi_ISLEM_TUTARI_ihlali()
    {
        var k = YeniKiraci("islem tutari");
        using (var db = vt.OlusturSistem()) IhlalFikstur.Nakit(db, k, 10_000m, DateTime.UtcNow.AddMinutes(-5), toplam: 40_000m);
        await Kostur(k);
        var t = Assert.Single(Ihlaller(k));
        Assert.Equal(TevsikIhlalTuru.IslemTutari, t.Tur);
        Assert.Equal(10_000m, t.NakitToplamTl);
    }

    [Fact]
    public async Task Taksit_kapiyi_kacan_nakit_taksit_TAKSIT_ihlali_olarak_bulunur()
    {
        var k = YeniKiraci("taksit");
        using (var db = vt.OlusturSistem())
        {
            var kaynak = TaksitFikstur.Borc(db, k, 62_000m, DateTime.UtcNow.AddDays(-4));
            var id = Kimlik.Ulid();
            var an = DateTime.UtcNow.AddMinutes(-5);
            db.Hareketler.Add(new Hareket
            {
                Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "TAHSILAT", Tarih = an,
                TlTutari = -20_000m, HasGrami = 0m, ParaBirimi = "TRY", KurSnapshotId = k.KurId, CariId = k.CariId,
                KullaniciId = k.KullaniciId, OdemeTuru = "NAKIT", NakitTutar = 20_000m, Olusturuldu = an,
            });
            db.CariBakiyeler.First(b => b.CariId == k.CariId).TlBakiye -= 20_000m;
            db.SaveChanges();
        }
        await Kostur(k);
        var t = Assert.Single(Ihlaller(k), x => x.Tur == TevsikIhlalTuru.Taksit);
        Assert.Equal(20_000m, t.NakitToplamTl);
        Assert.Equal(TevsikIhlalYonu.Tahsilat, t.Yon);
    }

    /// <summary>Fail-closed: bildirim süresi tablosu okunamazsa iş HATALI biter, sessizce geçmez.</summary>
    [Fact]
    public async Task Yasal_parametre_servisi_yoksa_is_HATALI_biter()
    {
        var k = YeniKiraci("failclosed");
        using var db = vt.Olustur(k.TenantId);
        var i = new TevsikKurtarmaIsi();
        var s = await IsCatisiKurulum.Calistirici(db, IsCatisiKurulum.Yapilandirma(), i)
            .Calistir(i, k.TenantId, DateTime.UtcNow, null, default);
        Assert.Equal(IsDurum.Hatali, s.Durum);
    }
}

[Collection("http")]
public sealed class TevsikIhlalHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETYASAL2-TEST-" + ad);
    }

    /// <summary>Aşım yaratır ve işi ÜRETİM DI'siyle elle koşturur; ihlal kimliğini döner.</summary>
    private async Task<string> IhlalYarat(Kiraci k, string tur = "HURDA_ALIS", DateTime? an = null)
    {
        using (var db = f.Vt.OlusturSistem()) IhlalFikstur.Nakit(db, k, 36_000m, an ?? DateTime.UtcNow.AddMinutes(-5), tur);
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson($"/api/v1/isler/{TevsikKurtarmaIsi.IsKodu}/calistir", new { });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        Assert.Equal("BASARILI", (await c.Json()).GetProperty("durum").GetString());
        using var kontrol = f.Vt.OlusturSistem();
        return kontrol.TevsikIhlalleri.AsNoTracking().Single(t => t.TenantId == k.TenantId).Id;
    }

    // ── Tur 2: satır gerekçesi ve kaynağı bilinmeyen nakit listesi ──

    [Fact]
    public async Task Liste_ISLEM_TUTARI_satirinda_fis_toplami_TAKSIT_satirinda_kaynak_gunu_doner()
    {
        var k = YeniKiraci("gerekce");
        var kaynakGun = DateTime.UtcNow.AddDays(-4);
        using (var db = f.Vt.OlusturSistem())
        {
            IhlalFikstur.Nakit(db, k, 11_250.50m, DateTime.UtcNow.AddMinutes(-5), toplam: 41_250.50m);
            var cari2 = IhlalFikstur.Cari(db, k, "Taksit Müşterisi");
            var kid = TaksitFikstur.Borc(db, k, 62_000m, kaynakGun);
            db.Hareketler.First(h => h.Id == kid).CariId = cari2;
            var an = DateTime.UtcNow.AddMinutes(-3);
            db.Hareketler.Add(new Hareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "TAHSILAT", Tarih = an,
                TlTutari = -20_000m, HasGrami = 0m, ParaBirimi = "TRY", KurSnapshotId = k.KurId, CariId = cari2,
                KullaniciId = k.KullaniciId, OdemeTuru = "NAKIT", NakitTutar = 20_000m, Olusturuldu = an,
            });
            db.SaveChanges();
        }
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson($"/api/v1/isler/{TevsikKurtarmaIsi.IsKodu}/calistir", new { });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);

        var l = await (await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/tevsik/ihlaller")).Json();
        var satirlar = l.GetProperty("ihlaller").EnumerateArray().ToList();
        var islem = Assert.Single(satirlar, x => x.GetProperty("tur").GetString() == "ISLEM_TUTARI");
        Assert.Equal(41_250.50m, islem.GetProperty("islemToplamTl").GetDecimal());
        Assert.Equal(11_250.50m, islem.GetProperty("nakitToplamTl").GetDecimal());
        var taksit = Assert.Single(satirlar, x => x.GetProperty("tur").GetString() == "TAKSIT");
        Assert.Equal(DateOnly.FromDateTime(Zaman.UtcdenYerele(kaynakGun)).ToString("yyyy-MM-dd"),
            taksit.GetProperty("kaynakGunu").GetString());
    }

    [Fact]
    public async Task Dogrulanamayanlar_ACILIS_bakiyesinden_nakit_satiri_listede_TEZGAHTAR_403_okuma_izi()
    {
        var k = YeniKiraci("dogrulanamayan");
        using (var db = f.Vt.OlusturSistem()) TaksitFikstur.Borc(db, k, 50_000m, DateTime.UtcNow.AddDays(-30), tur: "ACILIS");
        var th = await f.Ortam.Istemci(k, "SAHIP").PostJson($"/api/v1/cari/{k.CariId}/tahsilat",
            new { tlTutari = 20_000m, odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId });
        Assert.Equal(HttpStatusCode.OK, th.StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/tevsik/ihlaller/dogrulanamayanlar")).StatusCode);
        var y = await f.Ortam.Istemci(k, "MUHASEBE").GetAsync("/api/v1/tevsik/ihlaller/dogrulanamayanlar");
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var j = await y.Json();
        var s = Assert.Single(j.GetProperty("satirlar").EnumerateArray());
        Assert.Equal(20_000m, s.GetProperty("nakitTl").GetDecimal());
        Assert.Equal(20_000m, s.GetProperty("dogrulanamayanTl").GetDecimal());
        Assert.Equal("TAHSILAT", s.GetProperty("kaynak").GetString());
        Assert.Equal(k.CariId, s.GetProperty("cariId").GetString());
        Assert.False(string.IsNullOrEmpty(s.GetProperty("cariAd").GetString()));
        Assert.Equal(100, j.GetProperty("sinir").GetInt32());

        using var kontrol = f.Vt.OlusturSistem();
        Assert.True(await kontrol.DenetimKayitlari.AnyAsync(d => d.TenantId == k.TenantId
            && d.VarlikTuru == "TevsikDogrulanamayan" && d.Eylem == DenetimEylemi.Okuma));
        Assert.Equal(0, await kontrol.TevsikIhlalleri.CountAsync(t => t.TenantId == k.TenantId));   // ihlal DEĞİL
    }

    // ── K-14 ──

    [Fact]
    public async Task K14_TEZGAHTAR_liste_403_SAHIP_MUDUR_MUHASEBE_200()
    {
        var k = YeniKiraci("K14 rol");
        await IhlalYarat(k);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/tevsik/ihlaller")).StatusCode);
        foreach (var rol in new[] { "SAHIP", "MUDUR", "MUHASEBE" })
        {
            var c = await f.Ortam.Istemci(k, rol).GetAsync("/api/v1/tevsik/ihlaller?durum=ACIK");
            Assert.Equal(HttpStatusCode.OK, c.StatusCode);
            var j = await c.Json();
            Assert.Equal(1, j.GetProperty("ihlaller").GetArrayLength());
            Assert.Equal(1, j.GetProperty("kapanmamisSayisi").GetInt32());
        }
    }

    [Fact]
    public async Task K14_dilekce_ucu_tam_kimlik_doner_ve_OKUMA_izi_yazar_TEZGAHTAR_403()
    {
        var k = YeniKiraci("K14 dilekce");
        var id = await IhlalYarat(k);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync($"/api/v1/tevsik/ihlaller/{id}/dilekce")).StatusCode);

        var c = await f.Ortam.Istemci(k, "MUHASEBE").GetAsync($"/api/v1/tevsik/ihlaller/{id}/dilekce");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var j = await c.Json();
        Assert.True(j.GetProperty("taslak").GetBoolean());
        Assert.Contains("mali müşavir", j.GetProperty("damga").GetString(), StringComparison.Ordinal);
        Assert.Equal("ODEME", j.GetProperty("ihlal").GetProperty("yon").GetString());
        Assert.False(string.IsNullOrEmpty(j.GetProperty("karsiTaraf").GetProperty("ad").GetString()));
        Assert.Equal(1, j.GetProperty("hareketler").GetArrayLength());

        using var db = f.Vt.OlusturSistem();
        Assert.True(await db.DenetimKayitlari.AnyAsync(d => d.TenantId == k.TenantId && d.VarlikTuru == "TevsikIhlali"
            && d.VarlikId == id && d.Eylem == DenetimEylemi.Okuma));
    }

    // ── Kapatma sözleşmesi ──

    [Fact]
    public async Task Bildirildi_gerekce_kisa_400_gelecek_tarih_400_gecerli_200_ikinci_kapatma_409()
    {
        var k = YeniKiraci("bildirildi");
        var id = await IhlalYarat(k);
        var ist = f.Ortam.Istemci(k, "MUHASEBE");
        var bugun = Zaman.YerelBugun();

        var kisa = await ist.PostJson($"/api/v1/tevsik/ihlaller/{id}/bildirildi", new { bildirimTarihi = bugun, gerekce = "kısa" });
        Assert.Equal(HttpStatusCode.BadRequest, kisa.StatusCode);
        var gelecek = await ist.PostJson($"/api/v1/tevsik/ihlaller/{id}/bildirildi",
            new { bildirimTarihi = bugun.AddDays(3), gerekce = "Vergi dairesine dilekçe elden verildi" });
        Assert.Equal(HttpStatusCode.BadRequest, gelecek.StatusCode);

        var ok = await ist.PostJson($"/api/v1/tevsik/ihlaller/{id}/bildirildi",
            new { bildirimTarihi = bugun, evrakNo = "E-2026/45", gerekce = "Vergi dairesine dilekçe elden verildi" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var j = await ok.Json();
        Assert.Equal("BILDIRILDI", j.GetProperty("durum").GetString());
        Assert.False(j.GetProperty("gec").GetBoolean());

        var tekrar = await ist.PostJson($"/api/v1/tevsik/ihlaller/{id}/bildirildi",
            new { bildirimTarihi = bugun, gerekce = "Vergi dairesine dilekçe elden verildi" });
        Assert.Equal(HttpStatusCode.Conflict, tekrar.StatusCode);

        using var db = f.Vt.OlusturSistem();
        var t = db.TevsikIhlalleri.AsNoTracking().Single(x => x.Id == id);
        Assert.Equal("E-2026/45", t.BildirimEvrakNo);
        Assert.Equal(k.KullaniciId, t.KapatanKullaniciId);
        Assert.True(await db.DenetimKayitlari.AnyAsync(d => d.VarlikId == id && d.Detay!.StartsWith("BILDIRILDI")));
    }

    [Fact]
    public async Task Son_gunden_sonra_bildirim_kaydi_kapatir_ama_gec_TRUE_yalan_beyan_yok()
    {
        var k = YeniKiraci("gec");
        var id = await IhlalYarat(k, an: IhlalFikstur.Ogle(Zaman.YerelBugun().AddDays(-9)));
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson($"/api/v1/tevsik/ihlaller/{id}/bildirildi",
            new { bildirimTarihi = Zaman.YerelBugun(), gerekce = "Süre geçtikten sonra dilekçe verildi" });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var j = await c.Json();
        Assert.Equal("BILDIRILDI", j.GetProperty("durum").GetString());
        Assert.True(j.GetProperty("gec").GetBoolean());
    }

    [Fact]
    public async Task Gecersiz_yalniz_SAHIP_MUDUR_403_gerekce_20_karakter_zorunlu()
    {
        var k = YeniKiraci("gecersiz");
        var id = await IhlalYarat(k, "SATIS");
        const string Gerekce = "havale yapılmış, kasaya yanlış NAKİT girilmiş — düzeltme fişi 12";
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "MUDUR")
            .PostJson($"/api/v1/tevsik/ihlaller/{id}/gecersiz", new { gerekce = Gerekce })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/tevsik/ihlaller/{id}/gecersiz", new { gerekce = "yanlış" })).StatusCode);
        var ok = await f.Ortam.Istemci(k, "SAHIP").PostJson($"/api/v1/tevsik/ihlaller/{id}/gecersiz", new { gerekce = Gerekce });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("GECERSIZ", (await ok.Json()).GetProperty("durum").GetString());

        var liste = await (await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/tevsik/ihlaller?durum=KAPANMAMIS")).Json();
        Assert.Equal(0, liste.GetProperty("ihlaller").GetArrayLength());
        Assert.Equal(0, liste.GetProperty("kapanmamisSayisi").GetInt32());
    }

    [Fact]
    public async Task Gecersiz_durum_suzgeci_400()
    {
        var k = YeniKiraci("suzgec");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/tevsik/ihlaller?durum=UYDURMA")).StatusCode);
    }
}
