using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Evrak;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-21 (Y-21) — EVRAK CİRO + RÜCU  [gerçek MySQL]
//
// HER TEST BİR İŞ KURALINI YENİDEN ÜRETİR:
//   • Ciro BORÇ KAPATIR: ciro edilen cariye olan borcumuz azalır, kasa AÇILMAZ.
//   • Evrak, onu bize veren cariye geri ciro EDİLEMEZ.
//   • Rücu İKİ borç doğurur (bizimki ve keşidecininki) — tek satır yetmez.
//   • ⚠ PLANIN DoD MADDESİ: CİRO EDİLEN EVRAKTA RÜCU DURUMU TERMİNAL DEĞİLDİR:
//     rücu sonrası KARSILIKSIZ'dan TAHSIL_EDILDI'ye çıkış VARDIR.
//   • Rücu yalnız CİRO EDİLMİŞ evrakta olur (portföydeki evrakta 400).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class P21CiroRucuTestleri(TestVeritabani vt)
{
    private static EvrakServisi Servis(KuyumcuDbContext db) =>
        new(db, NullLogger<EvrakServisi>.Instance, TestKurulum.Denetim(db),
            Kuyumcu.Api.Modules.Tevsik.TevsikKapisi.Varsayilan());

    private static DateOnly Bugun => Zaman.YerelBugun();

    private static string CariEkle(KuyumcuDbContext sistem, Kiraci k, string ad)
    {
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();
        sistem.Cariler.Add(new Cari
        {
            Id = id, TenantId = k.TenantId, Tur = "TEDARIKCI", Ad = ad,
            Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        sistem.CariBakiyeler.Add(new CariBakiye
        {
            CariId = id, TenantId = k.TenantId, TlBakiye = 0m, HasGramBakiye = 0m, Guncellendi = now,
        });
        sistem.SaveChanges();
        return id;
    }

    /// <summary>Portföye 10.000 TL'lik bir çek koyar ve kimliğini döndürür.</summary>
    private static async Task<string> EvrakKoy(KuyumcuDbContext db, Kiraci k, decimal tutar = 10_000m)
    {
        var sonuc = await Servis(db).Gir(new EvrakGirisDto
        {
            EvrakTuru = "CEK", Tutar = tutar, VadeGunu = Bugun.AddDays(30),
            EvrakNo = "CEK-P21-001", BankaAdi = "Ziraat",
            KaynakCariId = k.CariId, KurSnapshotId = k.KurId,
        }, k.Islem);
        return sonuc.Evrak.Id;
    }

    // ═══════════ CİRO ════════════════════════════════════════════════════════

    [Fact]
    public async Task Ciro_borcumuzu_kapatir_kasa_ACILMAZ_durum_CIRO_EDILDI()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-ciro-1");
        var tedarikci = CariEkle(sistem, k, "Toptancı");
        using var db = vt.Olustur(k);

        var id = await EvrakKoy(db, k);
        var kasaOnce = TestKurulum.KasaBakiye(sistem, k.KasaId);

        using var db2 = vt.Olustur(k);
        await Servis(db2).Ciro(id, new CiroDto
        {
            CiroCariId = tedarikci, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var e = kontrol.KiymetliEvraklar.AsNoTracking().Single(x => x.Id == id);
        Assert.Equal(EvrakDurumu.CiroEdildi, e.Durum);
        Assert.Equal(tedarikci, e.CiroCariId);
        Assert.NotNull(e.CiroHareketId);

        var h = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "EVRAK_CIRO");
        Assert.Equal(10_000m, h.TlTutari);          // borcumuz kapandı (+)
        Assert.Equal(0m, h.NakitTutar);             // para EL DEĞİŞTİRMEZ
        Assert.Equal(10_000m, h.VeresiyeTutar);     // borç KAPATAN veresiye payı
        Assert.Null(h.VadeTarihi);

        Assert.Equal(kasaOnce, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Equal(10_000m, TestKurulum.CariBakiye(kontrol, tedarikci).Tl);
    }

    [Fact]
    public async Task Evrak_onu_veren_cariye_geri_CIRO_EDILEMEZ()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-ciro-2");
        using var db = vt.Olustur(k);
        var id = await EvrakKoy(db, k);

        using var db2 = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Servis(db2).Ciro(id,
            new CiroDto { CiroCariId = k.CariId, KurSnapshotId = k.KurId }, k.Islem));
        Assert.Contains("size veren cariye ciro edilemez", ex.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(EvrakDurumu.Portfoyde, kontrol.KiymetliEvraklar.Single(x => x.Id == id).Durum);
        Assert.Empty(kontrol.Hareketler.Where(x => x.TenantId == k.TenantId && x.Tur == "EVRAK_CIRO"));
    }

    [Fact]
    public async Task Tahsil_edilmis_evrak_CIRO_EDILEMEZ_durum_kapisi_calisir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-ciro-3");
        var tedarikci = CariEkle(sistem, k, "Toptancı");
        using var db = vt.Olustur(k);
        var id = await EvrakKoy(db, k);

        using var db2 = vt.Olustur(k);
        await Servis(db2).TahsilEdildi(id, new TahsilEdildiDto
        {
            Hedef = "KASA", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var db3 = vt.Olustur(k);
        await Assert.ThrowsAnyAsync<Exception>(() => Servis(db3).Ciro(id,
            new CiroDto { CiroCariId = tedarikci, KurSnapshotId = k.KurId }, k.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(EvrakDurumu.TahsilEdildi, kontrol.KiymetliEvraklar.Single(x => x.Id == id).Durum);
    }

    // ═══════════ RÜCU ════════════════════════════════════════════════════════

    [Fact]
    public async Task Rucu_IKI_borc_dogurur_ve_iki_ayri_cariye_yazar()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-rucu-1");
        var tedarikci = CariEkle(sistem, k, "Toptancı");
        using var db = vt.Olustur(k);
        var id = await EvrakKoy(db, k);

        using var db2 = vt.Olustur(k);
        await Servis(db2).Ciro(id, new CiroDto
        {
            CiroCariId = tedarikci, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var db3 = vt.Olustur(k);
        await Servis(db3).Rucu(id, new RucuDto
        {
            Aciklama = "Toptancı çeki karşılıksız diye geri getirdi",
            KurSnapshotId = k.KurId, YeniVadeGun = 15,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var e = kontrol.KiymetliEvraklar.AsNoTracking().Single(x => x.Id == id);

        Assert.Equal(EvrakDurumu.Karsiliksiz, e.Durum);
        Assert.NotNull(e.RucuHareketId);
        Assert.NotNull(e.RucuTarihi);
        Assert.Equal(10_000m, e.RucuTutar);
        Assert.NotNull(e.KarsiliksizHareketId);

        var rucu = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "EVRAK_RUCU");
        Assert.Equal(-10_000m, rucu.TlTutari);       // BİZİM borcumuz geri doğdu
        Assert.Equal(tedarikci, rucu.CariId);
        Assert.Equal(0m, rucu.VeresiyeTutar);        // alacak defterine SIZMAZ

        var karsiliksiz = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "EVRAK_KARSILIKSIZ");
        Assert.Equal(10_000m, karsiliksiz.TlTutari); // keşidecinin borcu geri doğdu
        Assert.Equal(k.CariId, karsiliksiz.CariId);
        Assert.NotNull(karsiliksiz.VadeTarihi);

        // Ciro ile kapanan borç geri açıldı: 10.000 − 10.000 = 0
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, tedarikci).Tl);
        // Keşidecinin borcu: evrak girişi −10.000, rücu +10.000 → 0
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
    }

    /// <summary>
    /// ⚠ PLANIN DoD MADDESİ: "Ciro edilen evrakta RÜCU DURUMU TERMİNAL DEĞİLDİR."
    /// Rücu sonrası evrak KARSILIKSIZ'dır ve oradan TAHSIL_EDILDI'ye geçebilir.
    /// </summary>
    [Fact]
    public async Task Rucu_sonrasi_durum_TERMINAL_DEGILDIR_tahsil_edilebilir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-rucu-2");
        var tedarikci = CariEkle(sistem, k, "Toptancı");
        using var db = vt.Olustur(k);
        var id = await EvrakKoy(db, k);

        using var db2 = vt.Olustur(k);
        await Servis(db2).Ciro(id, new CiroDto { CiroCariId = tedarikci, KurSnapshotId = k.KurId }, k.Islem);

        using var db3 = vt.Olustur(k);
        await Servis(db3).Rucu(id, new RucuDto
        {
            Aciklama = "Ciro edilen çek karşılıksız çıktı, bize rücu edildi",
            KurSnapshotId = k.KurId,
        }, k.Islem);

        // Kapı seviyesinde çıkış VAR (terminal olsaydı boş dönerdi).
        Assert.Contains(EvrakDurumu.TahsilEdildi,
            EvrakDurumKapisi.IzinliGecisler(EvrakDurumu.Karsiliksiz));

        // Ve gerçekten koşar: keşideci sonradan nakit öder.
        using var db4 = vt.Olustur(k);
        await Servis(db4).TahsilEdildi(id, new TahsilEdildiDto
        {
            Hedef = "KASA", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(EvrakDurumu.TahsilEdildi, kontrol.KiymetliEvraklar.Single(x => x.Id == id).Durum);
    }

    [Fact]
    public async Task Portfoydeki_evraga_RUCU_yazilamaz()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-rucu-3");
        using var db = vt.Olustur(k);
        var id = await EvrakKoy(db, k);

        using var db2 = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Servis(db2).Rucu(id,
            new RucuDto
            {
                Aciklama = "Portföydeki evraka rücu denemesi yapılıyor",
                KurSnapshotId = k.KurId,
            }, k.Islem));
        Assert.Contains("yalnız CİRO EDİLMİŞ evrakta", ex.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.Where(x => x.TenantId == k.TenantId && x.Tur == "EVRAK_RUCU"));
    }

    [Fact]
    public async Task Rucu_gerekcesi_servis_icinde_de_zorunludur()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P21-rucu-4");
        var tedarikci = CariEkle(sistem, k, "Toptancı");
        using var db = vt.Olustur(k);
        var id = await EvrakKoy(db, k);

        using var db2 = vt.Olustur(k);
        await Servis(db2).Ciro(id, new CiroDto { CiroCariId = tedarikci, KurSnapshotId = k.KurId }, k.Islem);

        using var db3 = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Servis(db3).Rucu(id,
            new RucuDto { Aciklama = "kısa", KurSnapshotId = k.KurId }, k.Islem));
        Assert.Contains("en az 20 karakter", ex.Message);
    }
}
