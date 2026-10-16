using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using MMasak = Kuyumcu.Api.Modules.Masak;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-3 tur 2 — hasım doğrulama bulguları:
//  [KRİTİK] yaptırım kilidi yalnız DogrulaKumulatif'teydi → kilitli cariden avans,
//           emanet, değişim, rezerv, konsinye, sipariş, vezne, evrak, banka ve cari
//           tahsilat/veresiye yazılabiliyordu. Kapı artık SaveChanges'ta (tek merkez).
//           Her yol: kilitli cari → 400 YAPTIRIM_ENGEL ve yeni kayıt 0.
//  [ORTA]   bildirilecek varlığı olmayan eşleşme "MASAK'a bildirildi" yazılıyordu →
//           VARLIK_YOK; BILDIRILDI yalnız tarih + evrak no ile.
//  [ORTA]   eşik bölme sinyali (PESIN_ART_ARDA_FARKLI_KISI) Uyum → Risk'te sayılır.
//  [DÜŞÜK]  saklama aday seçiminde açlık: atlama kuralları sorguda.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class YaptirimKilidiYollariTestleri(HttpFikstur f)
{
    private sealed record Sayim(int Hareket, int Siparis, int Rezerv, int Vezne, int Banka);

    private Sayim Say(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return new Sayim(
            db.Hareketler.Count(h => h.TenantId == k.TenantId),
            db.Siparisler.Count(s => s.TenantId == k.TenantId),
            db.Rezervasyonlar.Count(r => r.TenantId == k.TenantId),
            db.VezneFisleri.Count(v => v.TenantId == k.TenantId),
            db.BankaHareketleri.Count(b => b.TenantId == k.TenantId));
    }

    /// <summary>Cariye TCKN yazar ve o TCKN'yi yasal listeye yükler → cari KİLİTLİ.</summary>
    private async Task Kilitle(Kiraci k)
    {
        var tc = Yasal3Yardim.Tckn();
        using (var db = f.Vt.OlusturSistem())
            await db.Database.ExecuteSqlRawAsync("UPDATE cariler SET vergi_no = {0} WHERE id = {1}", tc, k.CariId);
        var r = await f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari).PostAsJsonAsync("/api/v1/sistem/yaptirim-listesi", new
        {
            kaynak = "Test listesi (6415 s. K. md.5)",
            kaynakUrl = "https://masak.hmb.gov.tr/bkk-ile-malvarliklari-dondurulanlar",
            listeTarihi = YasalParametreServisi.IslemGunu(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            ekle = new[] { new { adSoyad = "Kilit Kisi " + tc, kimlikNo = tc, rgTarihi = (string?)null } },
        });
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(r.IsSuccessStatusCode, j.ToString());
        Assert.Equal(1, j.GetProperty("kilitlenenCari").GetInt32());
    }

    private static async Task Engel(HttpResponseMessage r)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode}: {govde}");
        var j = JsonSerializer.Deserialize<JsonElement>(govde);
        Assert.Equal(MMasak.YaptirimServisi.EngelMesaji, j.GetProperty("message").GetString());
        Assert.Equal(MMasak.YaptirimServisi.EngelKodu, j.GetProperty("kod").GetString());
    }

    private static async Task Tamam(HttpResponseMessage r) =>
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode}: {await r.Content.ReadAsStringAsync()}");

    private Kiraci Yeni(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETYASAL3-TEST-KILIT-" + ad);
    }

    [Theory]
    [InlineData("NAKIT")]
    [InlineData("HAVALE")]
    public async Task Kilitli_cariden_avans_alinamaz(string odeme)
    {
        var k = Yeni("AVANS-" + odeme);
        await Kilitle(k);
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Engel(await c.PostJson("/api/v1/islemler/avans", new
        {
            cariId = k.CariId, tutar = odeme == "NAKIT" ? 1_000m : 5_000m, odemeTuru = odeme, kasaId = k.KasaId, kurSnapshotId = k.KurId,
        }));
        Assert.Equal(0, Say(k).Hareket);
    }

    [Fact]
    public async Task Kilitli_cariye_avans_iadesi_yapilamaz()
    {
        var k = Yeni("AVANS-IADE");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Tamam(await c.PostJson("/api/v1/islemler/avans", new
        {
            cariId = k.CariId, tutar = 5_000m, odemeTuru = "HAVALE", kasaId = k.KasaId, kurSnapshotId = k.KurId,
        }));
        await Kilitle(k);
        var once = Say(k);
        await Engel(await c.PostJson("/api/v1/islemler/avans/iade", new
        {
            cariId = k.CariId, tutar = 1_000m, kasaId = k.KasaId, kurSnapshotId = k.KurId, gerekce = "Müşteri vazgeçti, iade",
        }));
        Assert.Equal(once, Say(k));
    }

    [Fact]
    public async Task Kilitli_cariden_emanet_alinamaz_verilemez()
    {
        var k = Yeni("EMANET");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Tamam(await c.PostJson("/api/v1/emanet/giris", new { cariId = k.CariId, ayarId = "K22", gramAgirlik = 10m, kurSnapshotId = k.KurId }));
        await Kilitle(k);
        var once = Say(k);
        await Engel(await c.PostJson("/api/v1/emanet/giris", new { cariId = k.CariId, ayarId = "K22", gramAgirlik = 10m, kurSnapshotId = k.KurId }));
        await Engel(await c.PostJson($"/api/v1/emanet/{k.CariId}/cikis", new { hasGrami = 5m, maden = "ALTIN", kurSnapshotId = k.KurId }));
        Assert.Equal(once, Say(k));
    }

    [Fact]
    public async Task Kilitli_cariyle_degisim_yapilamaz()
    {
        var k = Yeni("DEGISIM");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Tamam(await c.PostJson("/api/v1/islemler/satis", new
        {
            cariId = k.CariId, urunId = k.UrunId, adet = 1m, birimFiyatTl = 1_000m, odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId,
        }));
        string satisId;
        using (var db = f.Vt.OlusturSistem())
            satisId = db.Hareketler.Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS").Select(h => h.Id).Single();
        await Kilitle(k);
        var once = Say(k);
        await Engel(await c.PostJson("/api/v1/islemler/degisim", new
        {
            kaynakHareketId = satisId, geriAlinanAdet = 1m, yeniUrunId = k.UrunId, yeniAdet = 1m, yeniBirimFiyatTl = 1_000m,
            farkOdemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId,
        }));
        Assert.Equal(once, Say(k));
    }

    [Fact]
    public async Task Kilitli_cariye_rezerv_ve_konsinye_yapilamaz()
    {
        var k = Yeni("REZERV");
        await Kilitle(k);
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Engel(await c.PostJson("/api/v1/islemler/rezervler", new { urunId = k.UrunId, cariId = k.CariId, adet = 1m }));
        await Engel(await c.PostJson("/api/v1/islemler/konsinye-cikis", new { cariId = k.CariId, urunId = k.UrunId, adet = 1m, kurSnapshotId = k.KurId }));
        Assert.Equal(new Sayim(0, 0, 0, 0, 0), Say(k));
    }

    [Fact]
    public async Task Kilitli_cariye_siparis_ve_vezne_fisi_acilamaz()
    {
        var k = Yeni("SIPARIS");
        await Kilitle(k);
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Engel(await c.PostJson("/api/v1/siparisler", new
        {
            tur = "MUSTERI_SIPARISI", cariId = k.CariId, sozVerilenGun = Zaman.YerelBugun().AddDays(3).ToString("yyyy-MM-dd"),
            aciklama = "kilit testi", kalemler = new[] { new { urunId = k.UrunId, adet = 1m } },
        }));
        await Engel(await c.PostJson("/api/v1/vezne/fisler", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId, aciklama = "kilit testi",
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 1_000m, iscilikTl = 0m } },
        }));
        Assert.Equal(new Sayim(0, 0, 0, 0, 0), Say(k));
    }

    [Fact]
    public async Task Kilitli_cariden_cek_senet_alinamaz()
    {
        var k = Yeni("EVRAK");
        await Kilitle(k);
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Engel(await c.PostJson("/api/v1/evraklar", new
        {
            evrakTuru = "CEK", tutar = 10_000m, vadeGunu = Zaman.YerelBugun().AddDays(20).ToString("yyyy-MM-dd"),
            evrakNo = "CEK-KILIT-1", bankaAdi = "Ziraat", kaynakCariId = k.CariId, kurSnapshotId = k.KurId,
        }));
        Assert.Equal(0, Say(k).Hareket);
    }

    [Fact]
    public async Task Kilitli_cariye_veresiye_tahsilat_ve_banka_hareketi_yazilamaz()
    {
        var k = Yeni("CARI");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Tamam(await c.PostJson($"/api/v1/cari/{k.CariId}/veresiye", new { tlTutari = 900m, aciklama = "Veresiye satış", kurSnapshotId = k.KurId }));
        var hesap = await c.PostJson("/api/v1/bankalar", new
        {
            bankaAdi = "Ziraat", hesapAdi = "Ticari hesap", paraBirimi = "TRY",
            iban = TestIban.Uret(),
        });
        await Tamam(hesap);
        var hesapId = (await hesap.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        await Kilitle(k);
        var once = Say(k);
        await Engel(await c.PostJson($"/api/v1/cari/{k.CariId}/veresiye", new { tlTutari = 100m, aciklama = "Veresiye satış", kurSnapshotId = k.KurId }));
        await Engel(await c.PostJson($"/api/v1/cari/{k.CariId}/tahsilat", new { tlTutari = 500m, odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId }));
        await Engel(await c.PostJson($"/api/v1/bankalar/{hesapId}/hareket", new { yon = "GELEN", tutar = 1_000m, karsiTarafAd = "Ali Veli", cariId = k.CariId }));
        Assert.Equal(once, Say(k));
    }
}

[Collection("veritabani")]
public sealed class YaptirimTur2DbTestleri(TestVeritabani vt)
{
    /// <summary>
    /// Bulgu 2: bildirilecek varlık yokken eşleşme "BILDIRILDI" DEĞİL, VARLIK_YOK yazılır;
    /// BILDIRILDI'ye yalnız tarih + evrak no ile geçilir; VARLIK_YOK kullanıcıya seçenek değildir.
    /// </summary>
    [Fact]
    public async Task Varliksiz_eslesme_bildirildi_sayilmaz_kanitsiz_kapatilamaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-VARLIKYOK");
        var tc = Yasal3Yardim.Tckn();
        await kurulum.Database.ExecuteSqlRawAsync("UPDATE cariler SET vergi_no = {0} WHERE id = {1}", tc, k.CariId);
        using (var sdb = vt.OlusturSistem())
            await MMasak.YaptirimServisi.ListeGuncelle(sdb, TestKurulum.Denetim(sdb), new MMasak.YaptirimListesiDto
            {
                Kaynak = "Test listesi", KaynakUrl = "https://masak.hmb.gov.tr/bkk-ile-malvarliklari-dondurulanlar",
                ListeTarihi = YasalParametreServisi.IslemGunu(DateTime.UtcNow),
                Ekle = [new() { AdSoyad = "Varlik Yok " + tc, KimlikNo = tc }],
            }, "asikin-qa");

        string id;
        using (var kontrol = vt.OlusturSistem())
        {
            var e = kontrol.YaptirimEslesmeleri.Single(x => x.TenantId == k.TenantId && x.CariId == k.CariId);
            Assert.Equal(YaptirimEslesmeDurumu.VarlikYok, e.Durum);
            Assert.NotEqual(YaptirimEslesmeDurumu.Bildirildi, e.Durum);
            Assert.Null(e.MasakBildirimTarihi);
            id = e.Id;
        }
        Assert.DoesNotContain(YaptirimEslesmeDurumu.VarlikYok, YaptirimEslesmeDurumu.Hepsi);

        using var db = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => MMasak.YaptirimServisi.EslesmeGuncelle(
            db, id, new MMasak.YaptirimEslesmePatchDto { Durum = "BILDIRILDI" }, k.Islem));
        Assert.Contains("evrak", ex.Message);
        var ex2 = await Assert.ThrowsAsync<BadRequestException>(() => MMasak.YaptirimServisi.EslesmeGuncelle(
            db, id, new MMasak.YaptirimEslesmePatchDto { Durum = "VARLIK_YOK" }, k.Islem));
        Assert.Contains("geçersiz", ex2.Message);
    }

    /// <summary>Bulgu 3 (§ 4.2): Uyum → Risk özeti "başka biri" beyanlarını sayar (son 30 gün), eskisini saymaz.</summary>
    [Fact]
    public async Task Esik_bolme_sinyali_ozette_sayilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETYASAL3-TEST-SINYAL");
        using (var db = vt.Olustur(k))
        {
            var d = TestKurulum.Denetim(db);
            d.YazmaKaydet(k.Islem, MMasak.PesinPencereKapisi.DenetimTuru, k.CariId, "test 1");
            d.YazmaKaydet(k.Islem, MMasak.PesinPencereKapisi.DenetimTuru, k.CariId, "test 2");
            d.YazmaKaydet(k.Islem, "BASKA_TUR", k.CariId, "sayılmaz");
            await db.SaveChangesAsync();
        }
        await kurulum.Database.ExecuteSqlRawAsync(
            "UPDATE denetim_kayitlari SET tarih = {0} WHERE tenant_id = {1} AND detay = 'test 2'",
            DateTime.UtcNow.AddDays(-31), k.TenantId);

        using var oku = vt.Olustur(k);
        var ozet = JsonSerializer.SerializeToElement(await MMasak.YaptirimServisi.Ozet(oku, k.TenantId));
        var s = ozet.GetProperty("esikBolmeSinyali");
        Assert.Equal(30, s.GetProperty("gun").GetInt32());
        Assert.Equal(1, s.GetProperty("adet").GetInt32());
        Assert.Equal(1, s.GetProperty("son").GetArrayLength());
        Assert.False(string.IsNullOrEmpty(s.GetProperty("son")[0].GetProperty("kullanici").GetString()));
    }
}
