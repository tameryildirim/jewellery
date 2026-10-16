using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Cari;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;
using MMasak = Kuyumcu.Api.Modules.Masak;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-3 — tur 3 (orkestratör 01.10 03:25): yaptırım listesi eşleşmesinde
// "açık varlık" ölçüsü sabit para birimi listesi (TL/USD/EUR) TUTMAZ. Bakiye
// satırının TÜM birim kolonları + maden başına emanet + bekleyen sipariş tek
// yerden (YaptirimServisi.CariVarlikVar) okunur.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Saf: bakiye kolonu kümesi her para birimini kapsar.</summary>
public sealed class Yasal3AcikBakiyeOlcusuTestleri
{
    [Fact]
    public void Her_doviz_birimi_tek_basina_acik_bakiye_sayilir()
    {
        Assert.NotEmpty(DovizBakiye.ParaBirimleri);
        foreach (var p in DovizBakiye.ParaBirimleri)
        {
            var b = new CariBakiye { CariId = "c", TenantId = "t" };
            DovizBakiye.Ekle(b, p, 1m);
            Assert.True(MMasak.YaptirimServisi.AcikBakiyeVar(b), $"{p} bakiyesi açık sayılmadı");
        }
    }

    [Fact]
    public void TL_ve_has_tek_basina_acik_sifir_satir_acik_degil()
    {
        Assert.True(MMasak.YaptirimServisi.AcikBakiyeVar(new CariBakiye { CariId = "c", TenantId = "t", TlBakiye = -0.01m }));
        Assert.True(MMasak.YaptirimServisi.AcikBakiyeVar(new CariBakiye { CariId = "c", TenantId = "t", HasGramBakiye = 0.001m }));
        Assert.False(MMasak.YaptirimServisi.AcikBakiyeVar(new CariBakiye { CariId = "c", TenantId = "t" }));
        Assert.False(MMasak.YaptirimServisi.AcikBakiyeVar(null));
    }

    [Fact]
    public void Bakiye_kolonlari_varligin_tum_bakiye_alanlarini_kapsar()
    {
        var adlar = MMasak.YaptirimServisi.BakiyeKolonlari.Select(p => p.Name).ToHashSet();
        Assert.Contains(nameof(CariBakiye.TlBakiye), adlar);
        Assert.Contains(nameof(CariBakiye.HasGramBakiye), adlar);
        Assert.Contains(nameof(CariBakiye.UsdBakiye), adlar);
        Assert.Contains(nameof(CariBakiye.EurBakiye), adlar);
        Assert.Contains(nameof(CariBakiye.GbpBakiye), adlar);
    }
}

[Collection("veritabani")]
public sealed class Yasal3AcikBakiyeDbTestleri(TestVeritabani vt)
{
    private static string CariEkle(KuyumcuDbContext db, Kiraci k, string ad, string tc)
    {
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Cariler.Add(new Cari { Id = id, TenantId = k.TenantId, Tur = "MUSTERI", Ad = ad, VergiNo = tc, KimlikTuru = "TCKN", Aktif = true, Olusturuldu = now, Guncellendi = now });
        db.CariBakiyeler.Add(new CariBakiye { CariId = id, TenantId = k.TenantId, Guncellendi = now });
        db.SaveChanges();
        return id;
    }

    /// <summary>
    /// Yalnız GBP bakiyeli cari → görev AÇILIR · yalnız has (gram) bakiyeli → AÇILIR ·
    /// yalnız gümüş emaneti olan → AÇILIR · her şeyi sıfır → AÇILMAZ (VARLIK_YOK, kilit yine var).
    /// </summary>
    [Fact]
    public async Task Yalniz_GBP_has_ya_da_gumus_emanetli_cariye_gorev_acilir_sifirda_acilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-T3BAKIYE");
        var tcG = Yasal3Yardim.Tckn(); var tcH = Yasal3Yardim.Tckn(); var tcS = Yasal3Yardim.Tckn(); var tcZ = Yasal3Yardim.Tckn();
        var cG = CariEkle(kurulum, k, "Yalnız Sterlin", tcG);
        var cH = CariEkle(kurulum, k, "Yalnız Has", tcH);
        var cS = CariEkle(kurulum, k, "Yalnız Gümüş Emanet", tcS);
        var cZ = CariEkle(kurulum, k, "Sıfır", tcZ);
        await kurulum.Database.ExecuteSqlRawAsync("UPDATE cari_bakiyeler SET gbp_bakiye = 250 WHERE cari_id = {0}", cG);
        await kurulum.Database.ExecuteSqlRawAsync("UPDATE cari_bakiyeler SET has_gram_bakiye = 3.5 WHERE cari_id = {0}", cH);
        using (var db = vt.Olustur(k))
        {
            var now = DateTime.UtcNow;
            HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
                Tur = "EMANET_GIRIS", Tarih = now, TlTutari = 0m, HasGrami = 50m, Maden = Maden.Gumus,
                ParaBirimi = "TRY", KurSnapshotId = k.KurId, CariId = cS,
                KullaniciId = k.KullaniciId, Olusturuldu = now,
            });
            await db.SaveChangesAsync();
        }
        using (var on = vt.OlusturSistem())
        {
            // Ön koşul: gümüş emaneti cari bakiye satırına girmiyor; tek iz hareket defteri.
            var bS = on.CariBakiyeler.AsNoTracking().Single(b => b.CariId == cS);
            Assert.False(MMasak.YaptirimServisi.AcikBakiyeVar(bS));
        }

        using var sdb = vt.OlusturSistem();
        var rg = YasalParametreServisi.IslemGunu(DateTime.UtcNow);
        var sonuc = JsonSerializer.SerializeToElement(await MMasak.YaptirimServisi.ListeGuncelle(sdb, TestKurulum.Denetim(sdb), new MMasak.YaptirimListesiDto
        {
            Kaynak = "Test listesi (6415 s. K. md.5)", KaynakUrl = "https://masak.hmb.gov.tr/bkk-ile-malvarliklari-dondurulanlar",
            ListeTarihi = rg,
            Ekle =
            [
                new() { AdSoyad = "Liste G " + tcG, KimlikNo = tcG, RgTarihi = rg },
                new() { AdSoyad = "Liste H " + tcH, KimlikNo = tcH, RgTarihi = rg },
                new() { AdSoyad = "Liste S " + tcS, KimlikNo = tcS, RgTarihi = rg },
                new() { AdSoyad = "Liste Z " + tcZ, KimlikNo = tcZ, RgTarihi = rg },
            ],
        }, "asikin-qa"));
        Assert.Equal(4, sonuc.GetProperty("kilitlenenCari").GetInt32());
        Assert.Equal(3, sonuc.GetProperty("acilanGorev").GetInt32());

        using var kontrol = vt.OlusturSistem();
        string Durum(string cari) => kontrol.YaptirimEslesmeleri.AsNoTracking().Single(e => e.TenantId == k.TenantId && e.CariId == cari).Durum;
        Assert.Equal(YaptirimEslesmeDurumu.Acik, Durum(cG));
        Assert.Equal(YaptirimEslesmeDurumu.Acik, Durum(cH));
        Assert.Equal(YaptirimEslesmeDurumu.Acik, Durum(cS));
        Assert.Equal(YaptirimEslesmeDurumu.VarlikYok, Durum(cZ));
        Assert.Equal(rg.AddDays(30), kontrol.YaptirimEslesmeleri.AsNoTracking().Single(e => e.TenantId == k.TenantId && e.CariId == cG).BildirimSonGunu);
        Assert.Equal(3, kontrol.Bildirimler.Count(x => x.TenantId == k.TenantId && x.Tur == MMasak.YaptirimServisi.BildirimTuru));
    }
}
