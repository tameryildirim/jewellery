using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MMasak = Kuyumcu.Api.Modules.Masak;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 40-A — RİSK SERVİSİ (DB).
//
// ⚠ AYAR IConfiguration'DAN DEĞİL CTOR'DAN verilir (Aşkın koşulu): testin
//   sınadığı değer, testin yazdığı değerdir.
// ⚠ APPEND-ONLY: "iki hesaplama → iki satır" ve "mevcut satırda UPDATE reddedilir"
//   bu paketin en sert iki kuralıdır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class RiskServisiTestleri(TestVeritabani vt)
{
    private static MMasak.RiskServisi Servis(
        KuyumcuDbContext db, MMasak.MasakRiskAyarlari? ayar = null, MMasak.MasakEsikleri? esik = null)
        => new(db, ayar ?? new MMasak.MasakRiskAyarlari(), esik ?? new MMasak.MasakEsikleri(),
               new DenetimServisi(db, new HttpContextAccessor()));

    private static string Json(object o) => System.Text.Json.JsonSerializer.Serialize(o);
    private static System.Text.Json.JsonElement Kok(object o)
        => System.Text.Json.JsonDocument.Parse(Json(o)).RootElement;

    private static void NakitHareket(KuyumcuDbContext db, Kiraci k, decimal tutar)
    {
        var now = DateTime.UtcNow;
        HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(tutar), new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = "SATIS", Tarih = now, TlTutari = tutar, HasGrami = 0m,
            ParaBirimi = "TRY", OdemeTuru = "NAKIT",
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            Olusturuldu = now,
        });
    }

    // ── M40-4: APPEND-ONLY ───────────────────────────────────────────────────

    [Fact]
    public async Task Iki_hesaplama_IKI_SATIR_yazar_eskisini_guncellemez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Risk-appendonly");
        using var db = vt.Olustur(k);
        var servis = Servis(db);

        await servis.Hesapla(k.CariId, k.Islem);
        await servis.Hesapla(k.CariId, k.Islem);

        var satirlar = await db.CariRiskProfilleri.AsNoTracking()
            .Where(p => p.CariId == k.CariId).ToListAsync();

        Assert.Equal(2, satirlar.Count);
        Assert.Equal(2, satirlar.Select(s => s.Id).Distinct().Count());
        Assert.All(satirlar, s => Assert.Equal(RiskKaynagi.Hesap, s.Kaynak));
    }

    [Fact]
    public async Task Mevcut_profil_satirinda_UPDATE_reddedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Risk-update-red");
        using var db = vt.Olustur(k);
        await Servis(db).Hesapla(k.CariId, k.Islem);

        // Servisi ATLAYAN doğrudan bir güncelleme denemesi: koruma SaveChanges
        // düzeyindedir, kim denerse denesin patlar.
        var satir = await db.CariRiskProfilleri.FirstAsync(p => p.CariId == k.CariId);
        satir.Seviye = RiskSeviyesi.Yuksek;

        var hata = await Assert.ThrowsAsync<RiskProfiliDegistirilemez>(() => db.SaveChangesAsync());
        Assert.Contains("append-only", hata.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Profil_satiri_SILINEMEZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Risk-delete-red");
        using var db = vt.Olustur(k);
        await Servis(db).Hesapla(k.CariId, k.Islem);

        db.CariRiskProfilleri.Remove(await db.CariRiskProfilleri.FirstAsync(p => p.CariId == k.CariId));

        await Assert.ThrowsAsync<RiskProfiliDegistirilemez>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Guncel_profil_EN_SON_olusturuldu_satiridir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Risk-guncel");
        using var db = vt.Olustur(k);
        var servis = Servis(db);

        await servis.Hesapla(k.CariId, k.Islem);
        await servis.Elle(k.CariId,
            new MMasak.RiskElleDto { Seviye = "YUKSEK", Gerekce = "Uyum görevlisi incelemesi sonucu seviye yükseltildi." },
            k.Islem);

        var kok = Kok(await servis.Getir(k.CariId, k.Islem));

        Assert.Equal("YUKSEK", kok.GetProperty("guncel").GetProperty("seviye").GetString());
        Assert.Equal("ELLE", kok.GetProperty("guncel").GetProperty("kaynak").GetString());
        // Tarihçe en yeniden eskiye sıralı.
        Assert.Equal("ELLE", kok.GetProperty("tarihce")[0].GetProperty("kaynak").GetString());
        Assert.Equal("HESAP", kok.GetProperty("tarihce")[1].GetProperty("kaynak").GetString());
    }

    // ── M40-5: elle ezme gerekçesi + denetim izi ─────────────────────────────

    [Fact]
    public async Task Elle_ezme_gerekcesiz_400_doner()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Risk-gerekcesiz");
        using var db = vt.Olustur(k);

        var hata = await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Elle(
            k.CariId, new MMasak.RiskElleDto { Seviye = "YUKSEK", Gerekce = "kısa" }, k.Islem));

        Assert.Contains("gerekçe", hata.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.CariRiskProfilleri.Where(p => p.CariId == k.CariId).ToListAsync());
    }

    [Fact]
    public async Task Elle_ezme_gerekceliyse_ELLE_satiri_ve_denetim_kaydi_yazilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Risk-elle-iz");
        using var db = vt.Olustur(k);

        var oncekiIz = await db.DenetimKayitlari.CountAsync(d => d.VarlikTuru == "CariRiskProfili");

        await Servis(db).Elle(k.CariId,
            new MMasak.RiskElleDto { Seviye = "ORTA", Gerekce = "Müşteri beyanı doğrulandı, seviye orta yapıldı." },
            k.Islem);

        var satir = await db.CariRiskProfilleri.AsNoTracking().SingleAsync(p => p.CariId == k.CariId);
        Assert.Equal(RiskKaynagi.Elle, satir.Kaynak);
        Assert.Equal(RiskSeviyesi.Orta, satir.Seviye);
        Assert.NotNull(satir.Gerekce);

        // ⚠ AYNI transaction'da İKİ iz doğar ve bu BEKLENEN davranıştır:
        //   (1) DenetimYazmaDinleyicisi'nin OTOMATİK satır izi (Detay BOŞ),
        //   (2) RiskServisi'nin GEREKÇELİ yazma izi (DenetimIzi.cs:135-141).
        //   Sınanan, ikincisinin varlığı ve gerekçeyi taşımasıdır.
        var sonrakiIz = await db.DenetimKayitlari
            .Where(d => d.VarlikTuru == "CariRiskProfili" && d.VarlikId == satir.Id)
            .ToListAsync();
        Assert.Equal(2, sonrakiIz.Count);
        var gerekceli = Assert.Single(sonrakiIz, d => d.Detay != null);
        Assert.Contains("gerekçe", gerekceli.Detay!, StringComparison.OrdinalIgnoreCase);
        Assert.True(gerekceli.Hassas);
        Assert.True(await db.DenetimKayitlari.CountAsync(d => d.VarlikTuru == "CariRiskProfili") > oncekiIz);
    }

    [Fact]
    public async Task Elle_ezmede_gerekce_asgarisi_YAPILANDIRMADAN_gelir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Risk-gerekce-ayar");
        using var db = vt.Olustur(k);

        // Varsayılan 20 → 5 karakterlik gerekçe reddedilir; ayar 5 olunca geçer.
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Elle(
            k.CariId, new MMasak.RiskElleDto { Seviye = "ORTA", Gerekce = "kısaa" }, k.Islem));

        await Servis(db, new MMasak.MasakRiskAyarlari { ElleGerekceAsgari = 5 }).Elle(
            k.CariId, new MMasak.RiskElleDto { Seviye = "ORTA", Gerekce = "kısaa" }, k.Islem);

        Assert.Single(await db.CariRiskProfilleri.Where(p => p.CariId == k.CariId).ToListAsync());
    }

    [Fact]
    public async Task Gecersiz_seviye_400_doner()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Risk-seviye-red");
        using var db = vt.Olustur(k);

        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Elle(
            k.CariId,
            new MMasak.RiskElleDto { Seviye = "COK_YUKSEK", Gerekce = "Yirmi karakterden uzun bir gerekçe metni." },
            k.Islem));
    }

    // ── Kiracı izolasyonu ────────────────────────────────────────────────────

    [Fact]
    public async Task Yabanci_kiracinin_carisi_icin_hesapla_404_doner_ve_satir_yazilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "Risk-kiraci-A");
        var b = TestKurulum.KiraciKur(kurulum, "Risk-kiraci-B");
        using var db = vt.Olustur(a);

        await Assert.ThrowsAsync<NotFoundException>(() => Servis(db).Hesapla(b.CariId, a.Islem));

        using var sistem = vt.OlusturSistem();
        Assert.Empty(await sistem.CariRiskProfilleri.Where(p => p.CariId == b.CariId).ToListAsync());
    }

    // ── Girdi toplama ────────────────────────────────────────────────────────

    [Fact]
    public async Task Pep_beyanli_caride_puan_ve_sebep_uretilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Risk-pep");
        using var db = vt.Olustur(k);

        var cari = await db.Cariler.FirstAsync(c => c.Id == k.CariId);
        cari.PepBeyani = true;
        cari.PepGorev = "Belediye meclis üyesi";
        cari.PepBeyanTarihi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var kok = Kok(await Servis(db).Hesapla(k.CariId, k.Islem)).GetProperty("profil");

        Assert.Equal(new MMasak.MasakRiskAyarlari().PepPuan, kok.GetProperty("puan").GetInt32());
        Assert.Contains("PEP_BEYANI",
            kok.GetProperty("sebepler").EnumerateArray().Select(x => x.GetString()!));
    }

    [Fact]
    public async Task Nakit_agirlikli_gecmis_NAKIT_AGIRLIK_sebebi_uretir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Risk-nakit");
        using var db = vt.Olustur(k);
        NakitHareket(db, k, 50_000m);
        NakitHareket(db, k, 40_000m);
        await db.SaveChangesAsync();

        var kok = Kok(await Servis(db).Hesapla(k.CariId, k.Islem));

        Assert.Contains("NAKIT_AGIRLIK",
            kok.GetProperty("profil").GetProperty("sebepler").EnumerateArray().Select(x => x.GetString()!));
        Assert.Equal(90_000m, kok.GetProperty("girdiOzeti").GetProperty("toplamTl").GetDecimal());
    }

    [Fact]
    public async Task Esige_yakin_tekrarlayan_islem_ESIGE_YAKIN_sebebi_uretir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Risk-structuring");
        using var db = vt.Olustur(k);
        // MASAK-370: işlem günü (bugün, 07.10.2026 ve sonrası) kimlik eşiği 370.000; band %80-100 → 296.000-370.000.
        NakitHareket(db, k, 300_000m);
        NakitHareket(db, k, 360_000m);
        await db.SaveChangesAsync();

        var kok = Kok(await Servis(db).Hesapla(k.CariId, k.Islem));

        Assert.Equal(2, kok.GetProperty("girdiOzeti").GetProperty("esigeYakinIslem").GetInt32());
        Assert.Contains("ESIGE_YAKIN",
            kok.GetProperty("profil").GetProperty("sebepler").EnumerateArray().Select(x => x.GetString()!));
    }

    // ── M40-3: KVKK — yanıtta kimlik verisi YOK ──────────────────────────────

    [Fact]
    public async Task Risk_yanitlarinda_belge_no_alani_YOKTUR()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Risk-kvkk");
        using var db = vt.Olustur(k);
        db.MasakKimlikTespitleri.Add(new MasakKimlikTespiti
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, CariId = k.CariId, IslemTutariTl = 250_000m,
            BelgeTuru = KimlikBelgeTuru.TcKimlik, BelgeNo = "10000000146", AdSoyad = "Test Kişi",
            Uyruk = "TUR", TespitEdenKullaniciId = k.KullaniciId, Tarih = DateTime.UtcNow,
            SaklamaBitis = DateTime.UtcNow.AddYears(8), Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var servis = Servis(db);
        await servis.Hesapla(k.CariId, k.Islem);

        foreach (var govde in new[] { Json(await servis.Getir(k.CariId, k.Islem)), Json(await servis.Ozet(k.Islem)) })
        {
            Assert.DoesNotContain("belgeNo", govde, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("10000000146", govde);
            Assert.DoesNotContain("TUR", govde, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Tekil_risk_ucu_OKUMA_IZI_duser()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Risk-okuma-izi");
        using var db = vt.Olustur(k);

        var once = await db.DenetimKayitlari
            .CountAsync(d => d.VarlikTuru == "CariRiskProfili" && d.Eylem == DenetimEylemi.Okuma);
        await Servis(db).Getir(k.CariId, k.Islem);
        var sonra = await db.DenetimKayitlari
            .CountAsync(d => d.VarlikTuru == "CariRiskProfili" && d.Eylem == DenetimEylemi.Okuma);

        Assert.Equal(once + 1, sonra);
    }

    // ── Özet ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Ozet_seviye_dagilimini_ve_PEP_sayisini_dondurur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Risk-ozet");
        using var db = vt.Olustur(k);

        var cari = await db.Cariler.FirstAsync(c => c.Id == k.CariId);
        cari.PepBeyani = true;
        cari.PepGorev = "Genel müdür";
        await db.SaveChangesAsync();

        var servis = Servis(db);
        await servis.Elle(k.CariId,
            new MMasak.RiskElleDto { Seviye = "YUKSEK", Gerekce = "Uyum görevlisi kararıyla yüksek riskli sayıldı." },
            k.Islem);

        var kok = Kok(await servis.Ozet(k.Islem));

        Assert.Equal(1, kok.GetProperty("yuksek").GetInt32());
        Assert.Equal(1, kok.GetProperty("pepSayisi").GetInt32());
        Assert.False(kok.GetProperty("kuralEtkin").GetBoolean());   // VARSAYILAN KAPALI
        Assert.Equal(1, kok.GetProperty("yuksekRiskliler").GetArrayLength());
        Assert.Equal(k.CariId, kok.GetProperty("yuksekRiskliler")[0].GetProperty("cariId").GetString());
    }

    [Fact]
    public async Task Ozet_guncel_satiri_sayar_tarihcedeki_eski_satiri_DEGIL()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Risk-ozet-guncel");
        using var db = vt.Olustur(k);
        var servis = Servis(db);

        await servis.Elle(k.CariId,
            new MMasak.RiskElleDto { Seviye = "YUKSEK", Gerekce = "Önce yüksek riskli sayıldı, gerekçe uzundur." },
            k.Islem);
        await servis.Elle(k.CariId,
            new MMasak.RiskElleDto { Seviye = "DUSUK", Gerekce = "İnceleme sonucu şüphe kalktı, seviye düşürüldü." },
            k.Islem);

        var kok = Kok(await servis.Ozet(k.Islem));

        Assert.Equal(0, kok.GetProperty("yuksek").GetInt32());
        Assert.Equal(1, kok.GetProperty("dusuk").GetInt32());
    }

    // ── M40-6: risk skoru HİÇBİR KAPIYA bağlanmaz ───────────────────────────

    [Fact]
    public async Task Etkin_FALSE_varsayilandir_ve_yuksek_riskte_bile_satis_akisi_ETKILENMEZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Risk-kapi-yok");
        using var db = vt.Olustur(k);

        Assert.False(new MMasak.MasakRiskAyarlari().Etkin);

        await Servis(db).Elle(k.CariId,
            new MMasak.RiskElleDto { Seviye = "YUKSEK", Gerekce = "Skor yüksek olsa bile satış durmamalıdır." },
            k.Islem);

        // Profil YUKSEK iken defter yazımı aynen çalışır: risk hiçbir kapı kurmaz.
        NakitHareket(db, k, 25_000m);
        await db.SaveChangesAsync();

        Assert.Equal(1, await db.Hareketler.CountAsync(h => h.CariId == k.CariId));
    }
}
