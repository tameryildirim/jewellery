using System.Net;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MMasak = Kuyumcu.Api.Modules.Masak;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET MASAK-370 — MASAK kimlik eşiği 185.000 → 370.000 TL (07.10.2026'dan itibaren).
//
// DAYANAK (birincil kaynak): Tedbirler Yönetmeliği md.5/1-b, 11845 sayılı CK md.1,
// RG 07.10.2026/33393; yürürlük md.6 "yayımı tarihinde". Rapor:
// dokumanlar/09-uygulama-kayitlari/MEVZUAT-KUYUMCU-BELGE-2026-10-07.md.
//
// Beklenen değerler SABİT sayıdır (üretim kodundan hesaplanmaz). Eşikler, migration'ın
// kurduğu GERÇEK yasal_parametreler tablosundan okunur — migration yanlışsa bu dosya kırılır.
//
// ⚠ 200.000 TL "işlem amacı beyanı" kademesi (TEYİTSİZ, kurucu md.15 (a)) DEĞİŞMEDİ:
// 07.10.2026'dan itibaren 200.000–369.999,99 arasında kimlik, md.5/1-b eşiği yüzünden DEĞİL
// amaç beyanı kademesi yüzünden istenmeye devam eder. Bu dosya bunu açıkça sınar.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class Masak370Testleri(TestVeritabani vt)
{
    private static readonly DateOnly Eski = new(2026, 10, 6);
    private static readonly DateOnly Yeni = new(2026, 10, 7);

    private List<YasalParametreSatiri> TabloSatirlari()
    {
        using var db = vt.OlusturSistem();
        return db.YasalParametreler.AsNoTracking().AsEnumerable()
            .Select(YasalParametreSatiri.Kaynaktan).ToList();
    }

    private YasalParametreServisi Tablo(TimeProvider? saat = null) =>
        YasalParametreServisi.Yukleyicili(TabloSatirlari, saat);

    private MMasak.MasakKuralMotoru Motor(TimeProvider? saat = null) =>
        new(new MMasak.MasakEsikleri(), Tablo(saat));

    // ── Migration: geçmiş yeniden yazılmaz, yeni satır DOGRULANDI ────────────────

    [Fact]
    public void Migration_185000_satiri_06_10_sonunda_kapanir_370000_07_10_DOGRULANDI_acilir()
    {
        using var db = vt.OlusturSistem();
        var satirlar = db.YasalParametreler.AsNoTracking()
            .Where(p => p.Anahtar == YasalAnahtar.MasakKimlikEsigiTl)
            .OrderBy(p => p.GecerlilikBaslangic).ToList();

        var eski = satirlar.Single(p => p.Id == "yasal1-tohum-07");
        Assert.Equal(185_000m, eski.DegerSayi);                          // değer yeniden YAZILMADI
        Assert.Equal(new DateOnly(2023, 1, 14), eski.GecerlilikBaslangic);
        Assert.Equal(Yeni, eski.GecerlilikBitis);                          // bitiş HARİÇ → 06.10 son gün
        Assert.Equal(YasalTeyit.Dogrulandi, eski.Teyit);

        var yeni = satirlar.Single(p => p.Id == "masak370-01");
        Assert.Equal(370_000m, yeni.DegerSayi);
        Assert.Equal(Yeni, yeni.GecerlilikBaslangic);
        Assert.Null(yeni.GecerlilikBitis);
        Assert.Equal(YasalTeyit.Dogrulandi, yeni.Teyit);
        Assert.Equal("Tedbirler Yönetmeliği md.5/1-b (11845 sayılı CK, RG 07.10.2026/33393)", yeni.Dayanak);
        Assert.Equal("https://www.resmigazete.gov.tr/eskiler/2026/10/20261007-9.pdf", yeni.KaynakUrl);

        // Tek açık uçlu satır; 2023 öncesi dönemlere dokunulmadı (4 eski + 1 yeni).
        Assert.Single(satirlar, p => p.GecerlilikBitis is null);
        Assert.Equal(5, satirlar.Count);

        // 200.000 amaç beyanı kademesi DEĞİŞMEDİ (kurucu md.15 (a)).
        var amac = db.YasalParametreler.AsNoTracking().Single(p => p.Anahtar == YasalAnahtar.MasakAmacBeyaniEsigiTl);
        Assert.Equal(200_000m, amac.DegerSayi);
        Assert.Equal(YasalTeyit.Teyitsiz, amac.Teyit);
        Assert.Null(amac.GecerlilikBitis);
    }

    [Fact]
    public void Migration_denetim_izi_birakir_sistem_kiracisi()
    {
        using var db = vt.OlusturSistem();
        var iz = db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.VarlikId == "masak370-01" && d.VarlikTuru == "YasalParametre").ToList();
        var ekleme = Assert.Single(iz, d => d.Detay!.Contains("yasal parametre eklendi"));
        Assert.Equal("SISTEM", ekleme.TenantId);
        Assert.Contains("370000", ekleme.Detay);
        Assert.Contains("33393", ekleme.Detay);
    }

    // ── Dönem geçişi: işlem GÜNÜNÜN eşiği ────────────────────────────────────────

    [Fact]
    public void Donem_06_10_200000_kimlik_ZORUNLU_ve_190000_eski_esikle_YALNIZ_KIMLIK()
    {
        var m = Motor();
        Assert.Equal(185_000m, m.EsiklerGunu(Eski).KimlikEsigi);
        Assert.True(m.Gereksinim(200_000m, Eski).KimlikGerekli);
        // Eski eşiğin kendisi: 185.000–199.999,99 06.10'da YALNIZ KİMLİK.
        Assert.Equal(BeyanKademesi.YalnizKimlik, m.Gereksinim(185_000m, Eski).Kademe);
        Assert.Equal(BeyanKademesi.YalnizKimlik, m.Gereksinim(190_000m, Eski).Kademe);
        Assert.Equal(BeyanKademesi.Yok, m.Gereksinim(184_999.99m, Eski).Kademe);
    }

    [Fact]
    public void Donem_07_10_190000_artik_kimlik_ISTEMEZ_200000_amac_beyani_kademesi_ister()
    {
        var m = Motor();
        Assert.Equal(370_000m, m.EsiklerGunu(Yeni).KimlikEsigi);
        Assert.Equal(BeyanKademesi.Yok, m.Gereksinim(190_000m, Yeni).Kademe);
        Assert.Equal(BeyanKademesi.Yok, m.Gereksinim(199_999.99m, Yeni).Kademe);
        // TEYİTSİZ 200.000 kademesi aynen: kimlik + amaç.
        var g = m.Gereksinim(200_000m, Yeni);
        Assert.Equal(BeyanKademesi.AmacBeyani, g.Kademe);
        Assert.True(g.KimlikGerekli);
        Assert.True(g.AmacGerekli);
    }

    [Fact]
    public void Donem_07_10_369999_99_esik_kapisi_istemez_ama_200000_beyan_kademesi_kimlik_ISTER()
    {
        var m = Motor();
        var e = m.EsiklerGunu(Yeni);
        // md.5/1-b eşik kapısı: 369.999,99 < 370.000 → bu kapı kimlik istemez.
        Assert.True(369_999.99m < e.KimlikEsigi);
        // …ama 200.000 amaç beyanı kademesi (TEYİTSİZ, kurucu md.15 (a)) kimliği yine ister.
        var g = m.Gereksinim(369_999.99m, Yeni);
        Assert.Equal(BeyanKademesi.AmacBeyani, g.Kademe);
        Assert.True(g.KimlikGerekli);

        // Mesaj gerçek tetikleyiciyi söyler: 200.000 (370.000 DEĞİL — tutar onun altında).
        var d = m.Dogrula(new MMasak.MasakGirdi { IslemTutariTl = 369_999.99m }, Yeni);
        Assert.False(d.Uygun);
        var kimlikMesaji = Assert.Single(d.Eksikler, x => x.Contains("kimlik tespiti zorunludur"));
        Assert.Contains("200.000", kimlikMesaji);
        Assert.DoesNotContain("370.000", kimlikMesaji);
    }

    [Fact]
    public void Donem_07_10_370000_tam_esikte_kimlik_ZORUNLU_mesaj_370000_soyler()
    {
        var m = Motor();
        Assert.True(m.Gereksinim(370_000m, Yeni).KimlikGerekli);
        Assert.True(m.Gereksinim(-370_000m, Yeni).KimlikGerekli);   // alış da eşiğe tabi
        var d = m.Dogrula(new MMasak.MasakGirdi { IslemTutariTl = 370_000m }, Yeni);
        Assert.False(d.Uygun);
        Assert.Contains(d.Eksikler, x => x.Contains("370.000") && x.Contains("kimlik tespiti zorunludur"));
    }

    [Fact]
    public void Gun_siniri_yerel_saatle_06_10_23_59_eski_07_10_00_00_yeni_esik()
    {
        // Europe/Istanbul UTC+3: 06.10 23:59 yerel = 06.10 20:59 UTC; 07.10 00:00 yerel = 06.10 21:00 UTC.
        var saat = new SahteSaat(new DateTimeOffset(2026, 10, 6, 20, 59, 0, TimeSpan.Zero));
        var m = Motor(saat);
        Assert.Equal(185_000m, m.Esikler.KimlikEsigi);
        Assert.True(m.Gereksinim(190_000m).KimlikGerekli);

        saat.Simdi = new DateTimeOffset(2026, 10, 6, 21, 0, 0, TimeSpan.Zero);
        var m2 = Motor(saat);
        Assert.Equal(370_000m, m2.Esikler.KimlikEsigi);
        Assert.False(m2.Gereksinim(190_000m).KimlikGerekli);
    }

    [Fact]
    public void Kimlik_istenen_en_dusuk_tutar_eski_donemde_185000_yeni_donemde_200000()
    {
        var m = Motor();
        Assert.Equal(185_000m, m.EsiklerGunu(Eski).KimlikIstenenEnDusukTutar);
        Assert.Equal(200_000m, m.EsiklerGunu(Yeni).KimlikIstenenEnDusukTutar);
    }

    // ── Açılış eşitlik denetimi (Program.cs YASAL_PARAM_AYRISMA) ─────────────────

    [Fact]
    public void Acilis_karsilastirmasi_varsayilan_370000_ile_GECER_eski_185000_ile_AYRISIR()
    {
        var saat = new SahteSaat(new DateTimeOffset(2026, 10, 7, 9, 0, 0, TimeSpan.Zero));
        var s = Tablo(saat);
        var varsayilan = new MMasak.MasakEsikleri();
        Assert.Empty(s.AcilisFarklari(new Dictionary<string, decimal>
        {
            [YasalAnahtar.MasakKimlikEsigiTl] = varsayilan.KimlikEsigi,
            [YasalAnahtar.MasakAmacBeyaniEsigiTl] = varsayilan.AmacBeyaniEsigi,
        }));
        var fark = Assert.Single(s.AcilisFarklari(new Dictionary<string, decimal>
        {
            [YasalAnahtar.MasakKimlikEsigiTl] = 185_000m,
        }));
        Assert.Contains("MASAK_KIMLIK_ESIGI_TL", fark);
        Assert.Contains("370000", fark);
    }

    // ── Bağlantılı işlem: aynı gün 2 × 185.000 = 370.000 ─────────────────────────

    private MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance, Motor());

    private static MSatis.SatisOlusturDto Dto(Kiraci k, decimal tutar, MMasak.MasakBilgiDto? masak = null) => new()
    {
        CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = tutar,
        OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId, Masak = masak,
    };

    [Fact]
    public async Task Baglantili_ayni_gun_185000_kimliksiz_gecer_ikinci_185000_toplam_370000_kimlik_ZORUNLU()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETMASAK370-TEST baglantili", stokAdet: 20m);
        using var db = vt.Olustur(k);

        // 07.10.2026'dan itibaren 185.000 tek başına kimlik istemez (eski eşikte isterdi).
        await Satis(db).Satis(Dto(k, 185_000m), k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).Satis(Dto(k, 185_000m), k.Islem));
        Assert.StartsWith("MASAK", ex.Message);
        Assert.Contains("370.000 TL ve üstü işlemde kimlik tespiti zorunludur", ex.Message);
        Assert.Contains("370.000,00", ex.Message);   // kümülatif toplam

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, kontrol.Hareketler.Count(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
        Assert.Empty(kontrol.MasakKimlikTespitleri.Where(m => m.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Tek_islem_369999_99_kimliksiz_REDDEDILIR_cunku_200000_beyan_kademesi_aynen()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETMASAK370-TEST 369999", stokAdet: 20m);
        using var db = vt.Olustur(k);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).Satis(Dto(k, 369_999.99m), k.Islem));
        Assert.Contains("200.000 TL ve üstü işlemde işlem amacı beyanıyla birlikte kimlik tespiti zorunludur", ex.Message);
        Assert.Contains("eşik 200.000 TL", ex.Message);   // kümülatif notu gerçek tetikleyiciyi söyler

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
    }
}

[Collection("http")]
public sealed class Masak370HttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Esikler_ucu_bugun_370000_06_10_tarihiyle_185000_doner()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETMASAK370-TEST esikler");
        var c = f.Ortam.Istemci(k, "TEZGAHTAR");

        var bugun = await c.GetAsync("/api/v1/masak/esikler");
        Assert.Equal(HttpStatusCode.OK, bugun.StatusCode);
        var bj = await bugun.Json();
        Assert.Equal(370_000m, bj.GetProperty("kimlikEsigi").GetDecimal());
        // MASAK-370 tur 3: ekranın formu açtığı tutar — 200.000 amaç beyanı kademesi (TEYİTSİZ, aynen) kimliği ister.
        Assert.Equal(200_000m, bj.GetProperty("kimlikIstenenEnDusukTutar").GetDecimal());

        var eski = await (await c.GetAsync("/api/v1/masak/esikler?tarih=2026-10-06")).Json();
        Assert.Equal(185_000m, eski.GetProperty("kimlikEsigi").GetDecimal());
        Assert.Equal(200_000m, eski.GetProperty("amacBeyaniEsigi").GetDecimal());
        Assert.Equal(185_000m, eski.GetProperty("kimlikIstenenEnDusukTutar").GetDecimal());   // 06.10: davranış birebir

        var yeni = await (await c.GetAsync("/api/v1/masak/esikler?tarih=2026-10-07")).Json();
        Assert.Equal(370_000m, yeni.GetProperty("kimlikEsigi").GetDecimal());
        Assert.Equal(200_000m, yeni.GetProperty("amacBeyaniEsigi").GetDecimal());
        Assert.Equal(200_000m, yeni.GetProperty("kimlikIstenenEnDusukTutar").GetDecimal());
    }

    [Fact]
    public async Task Gereksinim_ucu_bugun_190000_YOK_200000_AMAC_BEYANI_370000_AMAC_BEYANI_kimlikli()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETMASAK370-TEST gereksinim");
        var c = f.Ortam.Istemci(k, "TEZGAHTAR");

        var g190 = await (await c.GetAsync("/api/v1/masak/gereksinim?tutar=190000")).Json();
        Assert.Equal(BeyanKademesi.Yok, g190.GetProperty("kademe").GetString());
        Assert.False(g190.GetProperty("kimlikGerekli").GetBoolean());

        var g369 = await (await c.GetAsync("/api/v1/masak/gereksinim?tutar=369999.99")).Json();
        Assert.Equal(BeyanKademesi.AmacBeyani, g369.GetProperty("kademe").GetString());
        Assert.True(g369.GetProperty("kimlikGerekli").GetBoolean());

        var g370 = await (await c.GetAsync("/api/v1/masak/gereksinim?tutar=370000")).Json();
        Assert.True(g370.GetProperty("kimlikGerekli").GetBoolean());
    }
}
