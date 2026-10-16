using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Isler;
using Kuyumcu.Api.Modules.Isler.Isler;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MMasak = Kuyumcu.Api.Modules.Masak;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-3 — saklama otomatiği (§ 4.3: K-6…K-9) ve yaptırım taraması
// (§ 4.4: K-10, K-11, K-11b, K-13) + risk YÜKSEK'e geçiş bildirimi (§ 4.5: K-14).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class SaklamaOtomatikTestleri(TestVeritabani vt)
{
    private static readonly IConfiguration Islak = IsCatisiKurulum.Yapilandirma(("Isler:SaklamaUygula:Kuru", "false"));
    private static readonly IConfiguration Kuru = IsCatisiKurulum.Yapilandirma();
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static MasakKimlikTespiti Tespit(Kiraci k, DateTime bitis, string belge, string? cariId = null) => new()
    {
        Id = Kimlik.Ulid(), TenantId = k.TenantId, CariId = cariId, IslemTutariTl = 250_000m,
        BelgeTuru = KimlikBelgeTuru.TcKimlik, BelgeNo = belge, AdSoyad = "Eski Müşteri",
        Adres = "Ulus", Uyruk = "TUR", DogumTarihi = new DateOnly(1980, 1, 1),
        HareketId = Kimlik.Ulid(), KaynakTur = MasakKaynakTuru.Hareket,
        TespitEdenKullaniciId = k.KullaniciId, Tarih = bitis.AddYears(-8),
        SaklamaBitis = bitis, Olusturuldu = bitis.AddYears(-8),
    };

    private static async Task<IsKosumSonucu> Kostur(TestVeritabani vt, Kiraci k, IConfiguration y, DateTime an)
    {
        using var db = vt.Olustur(k.TenantId);
        var i = new SaklamaUygulaIsi();
        return await IsCatisiKurulum.Calistirici(db, y, i).Calistir(i, k.TenantId, an, null, default);
    }

    private MasakKimlikTespiti Oku(string id)
    {
        using var kontrol = vt.OlusturSistem();
        return kontrol.MasakKimlikTespitleri.AsNoTracking().Single(m => m.Id == id);
    }

    /// <summary>K-6: SaklamaBitis dün doldu, bekletme yok → iş koşar → maskelenir; tutar/tarih/kademe korunur.</summary>
    [Fact]
    public async Task K6_suresi_dun_dolan_kayit_is_ile_maskelenir_tutar_korunur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-K6");
        var t = Tespit(k, DateTime.UtcNow.AddDays(-1), Yasal3Yardim.Tckn());
        using (var db = vt.Olustur(k)) { db.MasakKimlikTespitleri.Add(t); await db.SaveChangesAsync(); }

        await Kostur(vt, k, Islak, DateTime.UtcNow);

        var m = Oku(t.Id);
        Assert.Equal(MMasak.SaklamaSuresiIsi.Maskelenmis, m.AdSoyad);
        Assert.Null(m.Adres);
        Assert.Equal(250_000m, m.IslemTutariTl);
        Assert.Equal(t.SaklamaBitis, m.SaklamaBitis, TimeSpan.FromSeconds(1));
        using var kontrol = vt.OlusturSistem();
        Assert.Contains(kontrol.DenetimKayitlari.AsNoTracking().Where(d => d.TenantId == k.TenantId).Select(d => d.Detay).ToList(),
            d => d != null && d.Contains("(otomatik)"));
    }

    /// <summary>Kuru koşum (varsayılan): iş maskelemez.</summary>
    [Fact]
    public async Task Kuru_kosumda_is_maskelemez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-KURU");
        var t = Tespit(k, DateTime.UtcNow.AddDays(-1), Yasal3Yardim.Tckn());
        using (var db = vt.Olustur(k)) { db.MasakKimlikTespitleri.Add(t); await db.SaveChangesAsync(); }
        await Kostur(vt, k, Kuru, DateTime.UtcNow);
        Assert.Equal("Eski Müşteri", Oku(t.Id).AdSoyad);
    }

    /// <summary>K-7: aynı belge hash'iyle 2 yıl önce yeni tespit var → eski kayıt MASKELENMEZ (son işlem).</summary>
    [Fact]
    public async Task K7_ayni_belgeyle_yeni_tespit_varsa_eski_maskelenmez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-K7");
        var belge = Yasal3Yardim.Tckn();
        var eski = Tespit(k, DateTime.UtcNow.AddDays(-1), belge);
        var yeni = Tespit(k, DateTime.UtcNow.AddYears(6), belge);   // 2 yıl önce yapılmış tespit
        using (var db = vt.Olustur(k)) { db.MasakKimlikTespitleri.AddRange(eski, yeni); await db.SaveChangesAsync(); }

        await Kostur(vt, k, Islak, DateTime.UtcNow);

        Assert.Equal("Eski Müşteri", Oku(eski.Id).AdSoyad);
        Assert.Equal("Eski Müşteri", Oku(yeni.Id).AdSoyad);
    }

    /// <summary>
    /// Tur 2 (hasım, AÇLIK): parti sınırı atlanan (son işlem / bekletme) kayıtlarla DOLMAZ.
    /// Parti 1; en eski iki aday kalıcı atlanır (biri aynı belgeyle yeni tespit, biri tek-kayıt
    /// bekletmede) → daha yeni UYGUN aday yine seçilir ve maskelenecek beyan listesi de ona bağlıdır.
    /// </summary>
    [Fact]
    public async Task Parti_sinirini_atlanan_adaylar_doldurmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-ACLIK");
        var belge = Yasal3Yardim.Tckn();
        var enEski = Tespit(k, DateTime.UtcNow.AddDays(-30), belge);
        var canli = Tespit(k, DateTime.UtcNow.AddYears(6), belge);
        var bekleyen = Tespit(k, DateTime.UtcNow.AddDays(-20), Yasal3Yardim.Tckn());
        var uygun = Tespit(k, DateTime.UtcNow.AddDays(-10), Yasal3Yardim.Tckn());
        using (var db = vt.Olustur(k))
        {
            db.MasakKimlikTespitleri.AddRange(enEski, canli, bekleyen, uygun);
            db.SaklamaBekletmeleri.Add(new SaklamaBekletme
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, Kapsam = SaklamaBekletmeKapsami.Tespit, KimlikTespitiId = bekleyen.Id,
                Baslangic = DateTime.UtcNow.AddDays(-1), Bitis = DateTime.UtcNow.AddDays(60),
                Gerekce = "Uyuşmazlık nedeniyle saklama bekletmesi (test)", OlusturanKullaniciId = k.KullaniciId,
                Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }
        using var oku = vt.Olustur(k);
        var aday = await new MMasak.SaklamaSuresiIsi(oku, TestKurulum.Denetim(oku)).AdaylariBul(k.TenantId, null, DateTime.UtcNow, 1);
        Assert.Equal([uygun.Id], aday.TespitIdleri);
        Assert.Equal(1, aday.SonIslemNedeniyleAtlanan);
        Assert.Equal(1, aday.BekletmeNedeniyleAtlanan);
    }

    /// <summary>Cari ilişkisi sürüyorsa (son 8 yılda hareket) aday atlanır.</summary>
    [Fact]
    public async Task Carisiyle_son_8_yilda_hareket_varsa_maskelenmez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-CARI");
        using (var db = vt.Olustur(k))
            await Yasal3Yardim.Satis(db).Satis(new MSatis.SatisOlusturDto
            {
                CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 1_000m,
                OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);
        var t = Tespit(k, DateTime.UtcNow.AddDays(-1), Yasal3Yardim.Tckn(), k.CariId);
        using (var db = vt.Olustur(k)) { db.MasakKimlikTespitleri.Add(t); await db.SaveChangesAsync(); }
        await Kostur(vt, k, Islak, DateTime.UtcNow);
        Assert.Equal("Eski Müşteri", Oku(t.Id).AdSoyad);
    }

    /// <summary>K-8: bekletme aktifken maskelenmez; bekletme bitince (kaldırılınca) maskelenir.</summary>
    [Fact]
    public async Task K8_bekletme_aktifken_maskelemez_bitince_maskeler()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-K8");
        var t = Tespit(k, DateTime.UtcNow.AddDays(-1), Yasal3Yardim.Tckn());
        using (var db = vt.Olustur(k)) { db.MasakKimlikTespitleri.Add(t); await db.SaveChangesAsync(); }

        object bekletme;
        using (var db = vt.Olustur(k))
            bekletme = await new MMasak.SaklamaBekletmeServisi(db, TestKurulum.Denetim(db), Kuru).Olustur(new MMasak.SaklamaBekletmeDto
            {
                Kapsam = "TESPIT", KimlikTespitiId = t.Id,
                Bitis = YasalParametreServisi.IslemGunu(DateTime.UtcNow).AddDays(30),
                Gerekce = "Mahkeme süreci devam ediyor, kayıt saklanmalı",
            }, k.Islem);

        await Kostur(vt, k, Islak, DateTime.UtcNow);
        Assert.Equal("Eski Müşteri", Oku(t.Id).AdSoyad);

        var id = JsonSerializer.SerializeToElement(bekletme).GetProperty("id").GetString()!;
        using (var db = vt.Olustur(k))
            await new MMasak.SaklamaBekletmeServisi(db, TestKurulum.Denetim(db), Kuru).Kaldir(id, k.Islem);
        await Kostur(vt, k, Islak, DateTime.UtcNow.AddDays(1));   // ertesi gün tur
        Assert.Equal(MMasak.SaklamaSuresiIsi.Maskelenmis, Oku(t.Id).AdSoyad);
    }

    /// <summary>K-9: 30 gün kala bildirim BİR kez üretilir (iki tur → 1 bildirim).</summary>
    [Fact]
    public async Task K9_otuz_gun_kala_iki_tur_tek_bildirim()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-K9");
        var t = Tespit(k, DateTime.UtcNow.AddDays(20), Yasal3Yardim.Tckn());
        using (var db = vt.Olustur(k)) { db.MasakKimlikTespitleri.Add(t); await db.SaveChangesAsync(); }

        await Kostur(vt, k, Kuru, DateTime.UtcNow);
        await Kostur(vt, k, Kuru, DateTime.UtcNow.AddDays(1));

        using var kontrol = vt.OlusturSistem();
        var b = Assert.Single(kontrol.Bildirimler.AsNoTracking().Where(x => x.TenantId == k.TenantId && x.Tur == SaklamaUygulaIsi.IsKodu).ToList());
        Assert.Contains("30 gün sonra kendiliğinden gizlenecek", b.Govde);
        Assert.StartsWith("1 kaydın", b.Govde);
        Assert.Equal("Eski Müşteri", Oku(t.Id).AdSoyad);   // süre dolmadı — maskelenmez
    }

    /// <summary>Bekletme doğrulaması: gerekçe ≥ 20, bitiş ≤ 1 yıl, PATCH gövdede yoksa korur.</summary>
    [Fact]
    public async Task Bekletme_dogrulama_ve_PATCH_sozlesmesi()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-BEK");
        using var db = vt.Olustur(k);
        var s = new MMasak.SaklamaBekletmeServisi(db, TestKurulum.Denetim(db), Kuru);
        var bugun = YasalParametreServisi.IslemGunu(DateTime.UtcNow);

        var kisa = await Assert.ThrowsAsync<BadRequestException>(() => s.Olustur(new() { Kapsam = "TUMU", Bitis = bugun.AddDays(10), Gerekce = "kısa" }, k.Islem));
        Assert.Equal("gerekce", AlanIsareti.Alan(kisa));
        var uzun = await Assert.ThrowsAsync<BadRequestException>(() => s.Olustur(new() { Kapsam = "TUMU", Bitis = bugun.AddYears(1).AddDays(1), Gerekce = "Vergi incelemesi sürüyor, saklanmalı" }, k.Islem));
        Assert.Equal("bitis", AlanIsareti.Alan(uzun));

        var b = JsonSerializer.SerializeToElement(await s.Olustur(new() { Kapsam = "TUMU", Bitis = bugun.AddDays(10), Gerekce = "Vergi incelemesi sürüyor, saklanmalı" }, k.Islem));
        var id = b.GetProperty("id").GetString()!;
        var patch = JsonSerializer.Deserialize<MMasak.SaklamaBekletmePatchDto>("{\"bitis\":\"" + bugun.AddDays(40).ToString("yyyy-MM-dd") + "\"}", Web)!;
        var g = JsonSerializer.SerializeToElement(await s.Guncelle(id, patch, k.Islem));
        Assert.Equal(bugun.AddDays(40).ToString("yyyy-MM-dd"), g.GetProperty("bitisGunu").GetString());
        Assert.Equal("Vergi incelemesi sürüyor, saklanmalı", g.GetProperty("gerekce").GetString());   // gövdede yok → korundu
        var nullGerekce = JsonSerializer.Deserialize<MMasak.SaklamaBekletmePatchDto>("{\"gerekce\":null}", Web)!;
        await Assert.ThrowsAsync<BadRequestException>(() => s.Guncelle(id, nullGerekce, k.Islem));   // null → zorunlu alan
        using var kontrol = vt.OlusturSistem();
        // Gerekçeli iz (YazmaKaydet): ekleme + güncelleme; otomatik yazma dinleyicisinin satırları ayrıca sayılmaz.
        Assert.Equal(2, kontrol.DenetimKayitlari.Count(d => d.TenantId == k.TenantId && d.VarlikTuru == nameof(SaklamaBekletme)
            && d.Detay != null && d.Detay.StartsWith("saklama bekletmesi")));
    }
}

[Collection("veritabani")]
public sealed class YaptirimDbTestleri(TestVeritabani vt)
{
    private async Task<object> ListeYukle(params MMasak.YaptirimSatiriDto[] satirlar)
    {
        using var sdb = vt.OlusturSistem();
        return await MMasak.YaptirimServisi.ListeGuncelle(sdb, TestKurulum.Denetim(sdb), new MMasak.YaptirimListesiDto
        {
            Kaynak = "Test listesi (6415 s. K. md.5)", KaynakUrl = "https://masak.hmb.gov.tr/bkk-ile-malvarliklari-dondurulanlar",
            ListeTarihi = YasalParametreServisi.IslemGunu(DateTime.UtcNow), Ekle = [.. satirlar],
        }, "asikin-qa");
    }

    /// <summary>
    /// K-11b: liste güncellemesi mevcut cariyle TAM eşleşir, carinin 5.000 TL açık bakiyesi var →
    /// SAHİP'e KRİTİK görev, son gün = RG + 30. Bakiye 0 ve emanet yoksa görev YOK (kilit yine var).
    /// </summary>
    [Fact]
    public async Task K11b_liste_guncellemesi_bakiyeli_cariye_30_gunluk_gorev_acar()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-K11B");
        var tcA = Yasal3Yardim.Tckn(); var tcB = Yasal3Yardim.Tckn();
        var cariB = Kimlik.Ulid();
        kurulum.Cariler.Add(new Cari { Id = cariB, TenantId = k.TenantId, Tur = "MUSTERI", Ad = "Sıfır Bakiyeli", VergiNo = tcB, KimlikTuru = "TCKN", Aktif = true, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow });
        kurulum.CariBakiyeler.Add(new CariBakiye { CariId = cariB, TenantId = k.TenantId, Guncellendi = DateTime.UtcNow });
        kurulum.SaveChanges();
        await kurulum.Database.ExecuteSqlRawAsync("UPDATE cariler SET vergi_no = {0} WHERE id = {1}", tcA, k.CariId);
        await kurulum.Database.ExecuteSqlRawAsync("UPDATE cari_bakiyeler SET tl_bakiye = 5000 WHERE cari_id = {0}", k.CariId);

        var rg = YasalParametreServisi.IslemGunu(DateTime.UtcNow).AddDays(-2);
        var sonuc = JsonSerializer.SerializeToElement(await ListeYukle(
            new() { AdSoyad = "Liste Kişi A " + tcA, KimlikNo = tcA, RgTarihi = rg },
            new() { AdSoyad = "Liste Kişi B " + tcB, KimlikNo = tcB, RgTarihi = rg }));
        Assert.Equal(2, sonuc.GetProperty("kilitlenenCari").GetInt32());
        Assert.Equal(1, sonuc.GetProperty("acilanGorev").GetInt32());

        using var kontrol = vt.OlusturSistem();
        var eA = kontrol.YaptirimEslesmeleri.Single(e => e.TenantId == k.TenantId && e.CariId == k.CariId);
        Assert.Equal(rg.AddDays(30), eA.BildirimSonGunu);
        Assert.Equal(YaptirimEslesmeDurumu.Acik, eA.Durum);
        var eB = kontrol.YaptirimEslesmeleri.Single(e => e.TenantId == k.TenantId && e.CariId == cariB);
        Assert.Equal(YaptirimEslesmeDurumu.VarlikYok, eB.Durum);   // tur 2: "bildirildi" DEĞİL
        var b = Assert.Single(kontrol.Bildirimler.Where(x => x.TenantId == k.TenantId && x.Tur == MMasak.YaptirimServisi.BildirimTuru).ToList());
        Assert.Equal(BildirimOnem.Kritik, b.Onem);
        Assert.Contains(rg.AddDays(30).ToString("dd.MM.yyyy"), b.Govde);

        // Kilitli cari: her işlem 400.
        using var db = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Yasal3Yardim.Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = cariB, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 1_000m, OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem));
        Assert.Equal(MMasak.YaptirimServisi.EngelMesaji, ex.Message);
    }

    /// <summary>K-14: risk YÜKSEK'e geçiş → 1 bildirim; aynı seviyede kalış → yeni bildirim yok.</summary>
    [Fact]
    public async Task K14_risk_yuksege_gecis_tek_bildirim()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-K14");
        using var db = vt.Olustur(k);
        var risk = new MMasak.RiskServisi(db, new MMasak.MasakRiskAyarlari(), new MMasak.MasakEsikleri(), TestKurulum.Denetim(db));
        await risk.Hesapla(k.CariId, k.Islem);   // DÜŞÜK
        db.YaptirimEslesmeleri.Add(new YaptirimEslesmesi
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, YaptirimKaydiId = Kimlik.Ulid(), Tur = YaptirimEslesmeTuru.AdBenzerligi,
            Nokta = YaptirimNoktasi.Cari, CariId = k.CariId, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        await risk.Hesapla(k.CariId, k.Islem);   // YÜKSEK'e geçiş
        await risk.Hesapla(k.CariId, k.Islem);   // YÜKSEK'te kalış
        using var kontrol = vt.OlusturSistem();
        var b = Assert.Single(kontrol.Bildirimler.Where(x => x.TenantId == k.TenantId && x.Tur == MMasak.RiskServisi.RiskYuksekBildirimi).ToList());
        Assert.Contains("şüpheli işlem bildirimi", b.Govde);
    }
}

[Collection("http")]
public sealed class YaptirimHttpTestleri(HttpFikstur f)
{
    private async Task Yukle(string ad, string? tckn)
    {
        var c = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
        var r = await c.PostAsJsonAsync("/api/v1/sistem/yaptirim-listesi", new
        {
            kaynak = "Test listesi (6415 s. K. md.5)",
            kaynakUrl = "https://masak.hmb.gov.tr/bkk-ile-malvarliklari-dondurulanlar",
            listeTarihi = YasalParametreServisi.IslemGunu(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            ekle = new[] { new { adSoyad = ad, kimlikNo = tckn, rgTarihi = (string?)null } },
        });
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode}: {await r.Content.ReadAsStringAsync()}");
    }

    private static object Satis(Kiraci k, MMasak.MasakBilgiDto kimlik) => new
    {
        cariId = k.CariId, urunId = k.UrunId, adet = 1m, birimFiyatTl = 200_000m,
        odemeTuru = "HAVALE", kasaId = k.KasaId, kurSnapshotId = k.KurId, masak = kimlik,
    };

    /// <summary>
    /// K-10: listede TCKN X → X ile MASAK kimlikli satış 400; hareketler yeni satır 0;
    /// SAHİP bildirimi 1 (KRİTİK); denetim YAPTIRIM_ENGEL 1.
    /// </summary>
    [Fact]
    public async Task K10_listedeki_kimlikle_satis_400_hareket_yok_bildirim_ve_denetim_bir()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETYASAL3-TEST-K10");
        var tc = Yasal3Yardim.Tckn();
        await Yukle("Yaptirim Kisi " + tc, tc);

        var r = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/islemler/satis", Satis(k, Yasal3Yardim.Kimlik(tc, "Farklı Yazılmış Ad")));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(MMasak.YaptirimServisi.EngelMesaji, (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());

        using var kontrol = f.Vt.OlusturSistem();
        Assert.Equal(0, kontrol.Hareketler.Count(h => h.TenantId == k.TenantId));
        var b = Assert.Single(kontrol.Bildirimler.Where(x => x.TenantId == k.TenantId && x.Tur == MMasak.YaptirimServisi.BildirimTuru).ToList());
        Assert.Equal(BildirimOnem.Kritik, b.Onem);
        Assert.Equal(1, kontrol.DenetimKayitlari.Count(d => d.TenantId == k.TenantId && d.VarlikTuru == MMasak.YaptirimServisi.DenetimTuru));
        Assert.Equal(1, kontrol.YaptirimEslesmeleri.Count(e => e.TenantId == k.TenantId && e.Tur == YaptirimEslesmeTuru.Tam));
    }

    /// <summary>K-11: yalnız ad benzerliği (TCKN farklı) → satış 200 + "olası eşleşme" bildirimi.</summary>
    [Fact]
    public async Task K11_yalniz_ad_benzerligi_satis_gecer_olasi_eslesme_bildirimi()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETYASAL3-TEST-K11");
        var listedeki = Yasal3Yardim.Tckn();
        var ad = "Zeynep Testoğlu " + new string(listedeki[..6].Select(c => (char)('a' + (c - '0'))).ToArray());
        await Yukle(ad.ToUpperInvariant(), listedeki);

        var r = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/islemler/satis", Satis(k, Yasal3Yardim.Kimlik(Yasal3Yardim.Tckn(), ad)));
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode}: {await r.Content.ReadAsStringAsync()}");

        using var kontrol = f.Vt.OlusturSistem();
        var b = Assert.Single(kontrol.Bildirimler.Where(x => x.TenantId == k.TenantId && x.Tur == MMasak.YaptirimServisi.BildirimTuru).ToList());
        Assert.Equal(BildirimOnem.Uyari, b.Onem);
        Assert.Equal(1, kontrol.YaptirimEslesmeleri.Count(e => e.TenantId == k.TenantId && e.Tur == YaptirimEslesmeTuru.AdBenzerligi));
        Assert.Equal(1, kontrol.Hareketler.Count(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
    }

    /// <summary>K-13: JWT (SAHİP) ile sistem liste ucu → 401/403 (yalnız sistem anahtarı).</summary>
    [Fact]
    public async Task K13_JWT_ile_sistem_liste_ucu_kapali()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETYASAL3-TEST-K13");
        var r = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/sistem/yaptirim-listesi", new { kaynak = "x" });
        Assert.True(r.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden, $"{(int)r.StatusCode}");
        var anonim = await f.Ortam.AnonimIstemci().PostAsJsonAsync("/api/v1/sistem/yaptirim-listesi", new { kaynak = "x" });
        Assert.True(anonim.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden, $"{(int)anonim.StatusCode}");
    }

    /// <summary>Uyum uçları: tezgâhtar 403; SAHİP eşleşmeyi tarih+evrak no ile kapatır (PATCH).</summary>
    [Fact]
    public async Task Uyum_uclari_rol_ve_eslesme_kapatma()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETYASAL3-TEST-UYUM");
        var tez = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/masak/yaptirim");
        Assert.Equal(HttpStatusCode.Forbidden, tez.StatusCode);

        var tc = Yasal3Yardim.Tckn();
        await Yukle("Uyum Kisi " + tc, tc);
        var sahip = f.Ortam.Istemci(k, "SAHIP");
        await sahip.PostAsJsonAsync("/api/v1/islemler/satis", Satis(k, Yasal3Yardim.Kimlik(tc)));
        var ozet = await (await sahip.GetAsync("/api/v1/masak/yaptirim")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, ozet.GetProperty("acikTam").GetInt32());
        Assert.False(ozet.GetProperty("listeEski").GetBoolean());
        var id = ozet.GetProperty("eslesmeler")[0].GetProperty("id").GetString();

        var eksik = await sahip.PatchAsJsonAsync($"/api/v1/masak/yaptirim-eslesmeleri/{id}", new { durum = "BILDIRILDI" });
        Assert.Equal(HttpStatusCode.BadRequest, eksik.StatusCode);
        var tamam = await sahip.PatchAsJsonAsync($"/api/v1/masak/yaptirim-eslesmeleri/{id}", new
        {
            durum = "BILDIRILDI", masakBildirimTarihi = YasalParametreServisi.IslemGunu(DateTime.UtcNow).ToString("yyyy-MM-dd"), masakEvrakNo = "MASAK-2026-77",
        });
        Assert.Equal(HttpStatusCode.OK, tamam.StatusCode);
        var panel = await (await sahip.GetAsync("/api/v1/masak/uyum-ozet")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, panel.GetProperty("acikTam").GetInt32());
    }
}
