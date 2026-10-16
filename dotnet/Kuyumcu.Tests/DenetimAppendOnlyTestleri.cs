using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// DENETİM İZİ APPEND-ONLY — PLAN-2 Adım 0e (Mehmet'in koşulu #3).
//
// TESPİT (2026-09-13): `DenetimIzi.cs` başlığı 2026-09-11'den beri
// "bu tablo güncellenmez, silinmez" diye YAZIYORDU — ama bunu uygulayan
// hiçbir kod YOKTU. Ölçüldü: DenetimKaydi için Modified/Deleted engeli sıfır.
//
// Yorumun iddia edip kodun tutmadığı bir kural, kural değildir. Burası o
// kuralı KANITA bağlar: korumayı kaldırırsanız bu testler kırmızıya döner.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class DenetimAppendOnlyTestleri(TestVeritabani vt)
{
    private DenetimKaydi IzYaz(string tenantId)
    {
        using var db = vt.Olustur(tenantId);
        var kayit = DenetimServisi.Olustur(
            tenantId, null, DenetimEylemi.Okuma, nameof(Cari), "test-varlik", "test izi", null);
        db.DenetimKayitlari.Add(kayit);
        db.SaveChanges();
        return kayit;
    }

    [Fact]
    public void Denetim_kaydi_EKLENEBILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "AppendOnly ekleme");

        var kayit = IzYaz(k.TenantId);

        using var db = vt.Olustur(k.TenantId);
        Assert.True(db.DenetimKayitlari.Any(d => d.Id == kayit.Id));
    }

    [Fact]
    public void Denetim_kaydi_GUNCELLENEMEZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "AppendOnly güncelleme");
        var kayit = IzYaz(k.TenantId);

        using var db = vt.Olustur(k.TenantId);
        var izlenen = db.DenetimKayitlari.Single(d => d.Id == kayit.Id);
        izlenen.Detay = "izi örtmeye çalışıyorum";

        var hata = Assert.Throws<DenetimIziDegistirilemez>(() => db.SaveChanges());
        Assert.Contains("GÜNCELLEME", hata.Message);
    }

    [Fact]
    public void Denetim_kaydi_SILINEMEZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "AppendOnly silme");
        var kayit = IzYaz(k.TenantId);

        using var db = vt.Olustur(k.TenantId);
        var izlenen = db.DenetimKayitlari.Single(d => d.Id == kayit.Id);
        db.DenetimKayitlari.Remove(izlenen);

        var hata = Assert.Throws<DenetimIziDegistirilemez>(() => db.SaveChanges());
        Assert.Contains("SİLME", hata.Message);
    }

    [Fact]
    public async Task Silme_denemesi_kaydi_gercekten_yerinde_birakir()
    {
        // Sadece istisna atmak yetmez — kayıt DURUYOR mu?
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "AppendOnly kalıcılık");
        var kayit = IzYaz(k.TenantId);

        using (var db = vt.Olustur(k.TenantId))
        {
            db.DenetimKayitlari.Remove(db.DenetimKayitlari.Single(d => d.Id == kayit.Id));
            Assert.Throws<DenetimIziDegistirilemez>(() => db.SaveChanges());
        }

        using var kontrol = vt.Olustur(k.TenantId);
        var hala = await kontrol.DenetimKayitlari.AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == kayit.Id);

        Assert.NotNull(hala);
        Assert.Equal("test izi", hala.Detay);
    }

    [Fact]
    public void Ayni_kaydetmede_baska_varlikla_birlikte_gelen_degisiklik_de_reddedilir()
    {
        // Denetim kaydını meşru bir yazmanın ARASINA saklamak da çalışmamalı:
        // istisna tüm SaveChanges'ı düşürür, cari değişikliği de yazılmaz.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "AppendOnly karışık");
        var kayit = IzYaz(k.TenantId);

        using (var db = vt.Olustur(k.TenantId))
        {
            db.Cariler.Single(c => c.Id == k.CariId).Ad = "Değişmiş Ad";
            db.DenetimKayitlari.Single(d => d.Id == kayit.Id).Detay = "örtbas";

            Assert.Throws<DenetimIziDegistirilemez>(() => db.SaveChanges());
        }

        using var kontrol = vt.Olustur(k.TenantId);
        Assert.NotEqual("Değişmiş Ad", kontrol.Cariler.AsNoTracking().Single(c => c.Id == k.CariId).Ad);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// DENETİM İZİ OKUMA UCU — PLAN-2 Adım 0d.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class DenetimOkumaTestleri(TestVeritabani vt)
{
    private Kuyumcu.Api.Modules.Denetim.DenetimOkumaServisi Servis(KuyumcuDbContext db) =>
        new(db, new DenetimServisi(db, new HttpContextAccessor()));

    [Fact]
    public async Task Liste_yalnizca_kendi_kiracisinin_izini_doner()
    {
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "İz kiracı A");
        var b = TestKurulum.KiraciKur(vt.OlusturSistem(), "İz kiracı B");

        // Her kiracı kurulumu zaten yazma izi üretti.
        using var db = vt.Olustur(a.TenantId);
        var cevap = await Servis(db).Listele(a.Islem, null, null, null, null, null, null, null, 500, 0);

        var json = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(cevap)).RootElement;

        Assert.True(json.GetProperty("toplam").GetInt32() > 0);
        // B'nin kiracı kimliği hiçbir kayıtta geçmemeli — filtre kiracıya bağlı.
        Assert.DoesNotContain(b.TenantId, json.ToString());
    }

    [Fact]
    public async Task Eylem_filtresi_calisir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İz eylem filtresi");

        using var db = vt.Olustur(k.TenantId);
        await new DenetimServisi(db, new HttpContextAccessor())
            .OkumaKaydet(k.Islem, nameof(Cari), k.CariId, "filtre testi");

        var cevap = await Servis(db).Listele(
            k.Islem, null, null, DenetimEylemi.Okuma, null, null, null, null, 500, 0);

        var json = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(cevap)).RootElement;

        Assert.All(json.GetProperty("kayitlar").EnumerateArray(),
            e => Assert.Equal(DenetimEylemi.Okuma, e.GetProperty("eylem").GetString()));
    }

    [Fact]
    public async Task Denetim_izini_okumak_da_iz_birakir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İz okuma izi");

        using var db = vt.Olustur(k.TenantId);
        await Servis(db).Listele(k.Islem, null, null, null, null, null, null, null, 50, 0);

        var iz = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikTuru == nameof(DenetimKaydi))
            .ToListAsync();

        Assert.NotEmpty(iz);
        Assert.Contains("denetim izi görüntülendi", iz[0].Detay);
    }

    [Fact]
    public async Task Ozet_eylem_ve_varlik_kirilimini_doner()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İz özet");

        using var db = vt.Olustur(k.TenantId);
        var cevap = await Servis(db).Ozet(k.Islem, null, null);

        var json = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(cevap)).RootElement;

        Assert.True(json.GetProperty("toplam").GetInt32() > 0);
        Assert.NotEmpty(json.GetProperty("eylemler").EnumerateArray());
        Assert.NotEmpty(json.GetProperty("turler").EnumerateArray());
    }
}
