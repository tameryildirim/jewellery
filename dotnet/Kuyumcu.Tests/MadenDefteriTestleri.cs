using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MEmanet = Kuyumcu.Api.Modules.Emanet;
using MRapor = Kuyumcu.Api.Modules.Rapor;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 20 Faz A — DEFTERDE MADEN AYRIMI (veritabanı testleri).
//
// Kanıtlanan değişmezler:
//   K20-1  has bacağı olan satır madensiz YAZILAMAZ (HareketFabrikasi kapısı)
//   K20-2  cari_bakiyeler.has_gram_bakiye ALTIN'dır; gümüş emaneti dokunamaz
//   K20-3  hiçbir toplam farklı madenleri tek sayıda birleştirmez
//   K20-9  gümüş yatıran müşteri ALTIN çekemez
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MadenDefteriTestleri(TestVeritabani vt)
{
    private static MEmanet.EmanetServisi Emanet(KuyumcuDbContext db) =>
        new(db, NullLogger<MEmanet.EmanetServisi>.Instance);

    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    /// <summary>Kiracıya ikinci bir ürün ekler (istenen ayarda) ve stok açar.</summary>
    private static string UrunEkle(
        KuyumcuDbContext db, Kiraci k, string ayarId, string tip, decimal gram = 5m,
        decimal? satisFiyati = null, decimal stok = 10m)
    {
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();
        var ayar = Ayarlar.Bul(ayarId)!;
        var has = HasHesap.ToHasGram(gram, ayar);
        db.Urunler.Add(new Urun
        {
            Id = id, TenantId = k.TenantId, Tip = tip, Kod = $"M-{id[..8]}", Ad = $"{ayar.Ad} ürün",
            AyarId = ayarId, Milyem = ayar.Milyem, GramAgirlik = gram, TasAgirlik = 0m,
            HasGrami = has, IscilikTl = 0m, SatisFiyati = satisFiyati, Aktif = true,
            Olusturuldu = now, Guncellendi = now,
        });
        db.Stoklar.Add(new Stok
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, UrunId = id,
            Adet = stok, ToplamHasGram = stok * has, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    // ── K20-1: fabrika kapısı ────────────────────────────────────────────────

    /// <summary>
    /// Mutasyon M20-3: HareketFabrikasi.Ekle içindeki MadenKapisi.HareketDogrula
    /// çağrısı silinirse bu test KIRMIZI olur.
    /// </summary>
    [Fact]
    public void MadensizHareketYazilamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET20-TEST madensiz");
        using var db = vt.Olustur(k);

        var x = Assert.Throws<InvalidOperationException>(() => HareketFabrikasi.Ekle(
            db, OdemeIzi.Yok, new Hareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS",
                Tarih = DateTime.UtcNow, TlTutari = 0m, HasGrami = -9.16m,
                Maden = "", KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId,
                Olusturuldu = DateTime.UtcNow,
            }));
        Assert.Contains("madensiz", x.Message);
    }

    // ── K20-1: satış satırı ürünün madenini taşır ───────────────────────────

    [Fact]
    public async Task PlatinSatisindaHareketMadeniPLATIN()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET20-TEST platin satış");
        var platinId = UrunEkle(kurulum, k, "PT950", "PLATIN", gram: 5m, satisFiyati: 42_000m);

        using var db = vt.Olustur(k);
        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = platinId, Adet = 1m, BirimFiyatTl = 42_000m,
            OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var h = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "SATIS");
        Assert.Equal(Maden.Platin, h.Maden);
        // 5 g × 950/1000 = 4,75 has — ama PLATIN hasıdır, altın toplamına girmez.
        Assert.Equal(-4.750000m, h.HasGrami);
    }

    // ── K20-2: cari bakiyesi ALTIN'dır ──────────────────────────────────────

    /// <summary>
    /// Mutasyon M20-4: EmanetModulu'nda BakiyeGuncelle'nin `maden == ALTIN`
    /// koşulu kaldırılırsa bu test KIRMIZI olur.
    /// </summary>
    [Fact]
    public async Task GumusEmanetiAltinBakiyesiniDegistirmez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET20-TEST gümüş emanet");
        using var db = vt.Olustur(k);

        // 100 g 925 gümüş = 92,5 "gümüş has" — ALTIN bakiyesine ASLA yazılmaz.
        await Emanet(db).Giris(new MEmanet.EmanetGirisDto
        {
            CariId = k.CariId, AyarId = "G925", GramAgirlik = 100m, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Has);
        var h = kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "EMANET_GIRIS");
        Assert.Equal(Maden.Gumus, h.Maden);
        Assert.Equal(92.500000m, h.HasGrami);
    }

    [Fact]
    public async Task AltinEmanetiBakiyeyiDegistirir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET20-TEST altın emanet");
        using var db = vt.Olustur(k);

        await Emanet(db).Giris(new MEmanet.EmanetGirisDto
        {
            CariId = k.CariId, AyarId = "K22", GramAgirlik = 10m, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(-9.160000m, TestKurulum.CariBakiye(kontrol, k.CariId).Has);
        Assert.Equal(Maden.Altin, kontrol.Hareketler.AsNoTracking()
            .Single(x => x.TenantId == k.TenantId && x.Tur == "EMANET_GIRIS").Maden);
    }

    // ── K20-9: maden bazlı emanet çıkışı ────────────────────────────────────

    /// <summary>
    /// Mutasyon M20-7: emanet çıkışındaki maden koşulu kaldırılırsa bu test
    /// KIRMIZI olur — gümüş yatıran müşteri altın çekerdi.
    /// </summary>
    [Fact]
    public async Task GumusYatiranAltinCekemez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET20-TEST gümüş→altın");
        using var db = vt.Olustur(k);

        await Emanet(db).Giris(new MEmanet.EmanetGirisDto
        {
            CariId = k.CariId, AyarId = "G925", GramAgirlik = 100m, KurSnapshotId = k.KurId,
        }, k.Islem);

        var x = await Assert.ThrowsAnyAsync<ApiException>(() =>
            Emanet(db).Cikis(k.CariId, new MEmanet.EmanetCikisDto
            {
                HasGrami = 10m, Maden = Maden.Altin, KurSnapshotId = k.KurId,
            }, k.Islem));
        Assert.Contains("altın", x.Message);

        // Aynı madenden çekmek serbest.
        await Emanet(db).Cikis(k.CariId, new MEmanet.EmanetCikisDto
        {
            HasGrami = 10m, Maden = Maden.Gumus, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(Maden.Gumus, kontrol.Hareketler.AsNoTracking()
            .Single(x2 => x2.TenantId == k.TenantId && x2.Tur == "EMANET_CIKIS").Maden);
        // Gümüş çıkışı da ALTIN bakiyesine dokunmadı.
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Has);
    }

    /// <summary>Maden belirtilmeyen çıkış ALTIN sayılır — eski istemci sessizce gümüş çekemez.</summary>
    [Fact]
    public async Task MadensizCikisAltinSayilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET20-TEST madensiz çıkış");
        using var db = vt.Olustur(k);

        await Emanet(db).Giris(new MEmanet.EmanetGirisDto
        {
            CariId = k.CariId, AyarId = "G925", GramAgirlik = 100m, KurSnapshotId = k.KurId,
        }, k.Islem);

        await Assert.ThrowsAnyAsync<ApiException>(() =>
            Emanet(db).Cikis(k.CariId, new MEmanet.EmanetCikisDto
            {
                HasGrami = 5m, KurSnapshotId = k.KurId,   // maden YOK → ALTIN
            }, k.Islem));
    }

    /// <summary>Emanet yanıtı maden bazlı bakiyeleri ve daralan alanın notunu taşır.</summary>
    [Fact]
    public async Task EmanetYanitiMadenBakiyeleriniTasir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET20-TEST emanet yanıt");
        using var db = vt.Olustur(k);

        var e = Emanet(db);
        await e.Giris(new MEmanet.EmanetGirisDto
        { CariId = k.CariId, AyarId = "K24", GramAgirlik = 10m, KurSnapshotId = k.KurId }, k.Islem);
        await e.Giris(new MEmanet.EmanetGirisDto
        { CariId = k.CariId, AyarId = "G999", GramAgirlik = 50m, KurSnapshotId = k.KurId }, k.Islem);

        var yanit = await e.CariEmanetListesi(k.CariId, k.Islem);
        var tur = yanit.GetType();
        Assert.Equal(10.000000m, (decimal)tur.GetProperty("mevcutHasGramBakiye")!.GetValue(yanit)!);
        Assert.Equal(Maden.Altin, (string)tur.GetProperty("mevcutHasGramBakiyeMadeni")!.GetValue(yanit)!);
        Assert.Contains("YALNIZ altın",
            (string)tur.GetProperty("mevcutHasGramBakiyeNotu")!.GetValue(yanit)!);

        var bakiyeler = (System.Collections.IEnumerable)tur.GetProperty("madenBakiyeleri")!.GetValue(yanit)!;
        var satirlar = bakiyeler.Cast<object>().ToList();
        Assert.Equal(2, satirlar.Count);
        // 50 g 999 gümüş = 49,95 — ALTIN'ın 10 g'ıyla TOPLANMAZ.
        Assert.Contains(satirlar, s =>
            (string)s.GetType().GetProperty("maden")!.GetValue(s)! == Maden.Gumus
            && (decimal)s.GetType().GetProperty("gram")!.GetValue(s)! == 49.950000m);
    }

    /// <summary>K20-5: çelik emaneti has bacağı üzerinden izlenemez → reddedilir.</summary>
    [Fact]
    public async Task CelikEmanetiReddedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET20-TEST çelik emanet");
        using var db = vt.Olustur(k);

        var x = await Assert.ThrowsAnyAsync<ApiException>(() =>
            Emanet(db).Giris(new MEmanet.EmanetGirisDto
            {
                CariId = k.CariId, AyarId = "CELIK", GramAgirlik = 40m, KurSnapshotId = k.KurId,
            }, k.Islem));
        Assert.Contains("has gramı", x.Message);
    }

    // ── K20-3: raporlar madenleri toplamaz ──────────────────────────────────

    /// <summary>
    /// Mutasyon M20-5: rapor gruplamasından maden kırılımı çıkarılıp tek toplam
    /// döndürülürse bu test KIRMIZI olur.
    /// </summary>
    [Fact]
    public async Task DonemRaporuMadenleriToplamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET20-TEST dönem raporu");
        // Altın ürün zaten var (K22, 10 g → 9,16 has). Gümüş ürün ekle:
        var gumusId = UrunEkle(kurulum, k, "G999", "GUMUS", gram: 100m, satisFiyati: 5_000m);

        using var db = vt.Olustur(k);
        var satis = Satis(db);
        await satis.Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 60_000m,
            OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
        await satis.Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = gumusId, Adet = 1m, BirimFiyatTl = 5_000m,
            OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var okuma = vt.Olustur(k);
        var bugun = Zaman.YerelBugun();
        var rapor = await TestKurulum.RaporServisi(okuma)
            .AylikOzet(bugun.Year, bugun.Month, k.Islem);

        var tur = rapor.GetType();
        // toplamHas YALNIZ ALTIN: 9,16 — 99,9 g gümüş buna EKLENMEZ.
        var satisOzet = tur.GetProperty("satis")!.GetValue(rapor)!;
        Assert.Equal(9.160000m,
            (decimal)satisOzet.GetType().GetProperty("toplamHas")!.GetValue(satisOzet)!);

        var dagilim = ((System.Collections.IEnumerable)satisOzet.GetType()
            .GetProperty("madenDagilimi")!.GetValue(satisOzet)!).Cast<object>().ToList();
        Assert.Equal(2, dagilim.Count);
        Assert.Contains(dagilim, d =>
            (string)d.GetType().GetProperty("maden")!.GetValue(d)! == Maden.Gumus
            && (decimal)d.GetType().GetProperty("toplamHas")!.GetValue(d)! == 99.900000m);

        // Dönem geneli: uyarı ekranda görünsün diye METİN olarak döner.
        Assert.Contains("yalnız ALTIN",
            (string)tur.GetProperty("madenUyarisi")!.GetValue(rapor)!);
    }

    [Fact]
    public async Task TekMadenVarsaUyariYOK()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET20-TEST tek maden");
        using var db = vt.Olustur(k);
        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 60_000m,
            OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var okuma = vt.Olustur(k);
        var bugun = Zaman.YerelBugun();
        var rapor = await TestKurulum.RaporServisi(okuma).AylikOzet(bugun.Year, bugun.Month, k.Islem);
        Assert.Null(rapor.GetType().GetProperty("madenUyarisi")!.GetValue(rapor));
    }

    // ── K20-3: takasta maden karıştırma yasağı ──────────────────────────────

    [Fact]
    public async Task MadenDegistirenTakasReddedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET20-TEST takas");
        using var db = vt.Olustur(k);

        // Müşteri GÜMÜŞ getiriyor, ALTIN ürün alıyor → net has iki madeni karıştırırdı.
        var x = await Assert.ThrowsAnyAsync<ApiException>(() => Satis(db).Takas(new MSatis.TakasDto
        {
            Satici = ParaDemoTestVerisi.TakasSaticisi,   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
            CariId = k.CariId, EskiUrunGram = 100m, EskiUrunAyarId = "G925",
            EskiUrunBirimFiyatTlHasGram = 50m,
            YeniUrunId = k.UrunId, YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = 60_000m,
            OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        Assert.Contains("maden değiştirilemez", x.Message);
    }

    // ── Geri doldurma migration'ının SQL'i (Aşkın koşulu) ───────────────────

    /// <summary>
    /// Geri doldurma kuyumcu_test'te ÖLÇÜLEMEZ (taze şema, eski satır yok).
    /// Aşkın'ın koşulu gereği migration SQL'i DİZGE düzeyinde doğrulanır:
    /// ürünü olan satır ürünün ayarından, kalanı 'ALTIN'.
    /// </summary>
    [Fact]
    public void GeriDoldurmaMigrationiUrunAyarindanTuretir()
    {
        var kok = MigrationKlasoru();
        var dosya = Directory.GetFiles(kok, "*_HareketMaden.cs").Single();
        var metin = File.ReadAllText(dosya);

        Assert.Contains("UPDATE hareketler h", metin);
        Assert.Contains("JOIN urunler u ON u.id = h.urun_id", metin);
        Assert.Contains("WHEN u.ayar_id LIKE 'G%'  THEN 'GUMUS'", metin);
        Assert.Contains("WHEN u.ayar_id LIKE 'PT%' THEN 'PLATIN'", metin);
        Assert.Contains("WHEN u.ayar_id LIKE 'PD%' THEN 'PALADYUM'", metin);
        Assert.Contains("WHEN u.ayar_id = 'CELIK'  THEN 'CELIK'", metin);
        Assert.Contains("ELSE 'ALTIN'", metin);
        // Varsayım ve sevk öncesi ölçüm migration YORUMUNDA yazılı olmalı (md.1).
        Assert.Contains("GERİ DOLDURMA BİR VARSAYIMDIR", metin);
        Assert.Contains("SEVK ÖNCESİ ÖLÇÜM", metin);
        Assert.Contains("defaultValue: \"ALTIN\"", metin);
    }

    private static string MigrationKlasoru()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "Kuyumcu.Api")))
            d = d.Parent;
        Assert.NotNull(d);
        return Path.Combine(d!.FullName, "Kuyumcu.Api", "Data", "Migrations");
    }
}
