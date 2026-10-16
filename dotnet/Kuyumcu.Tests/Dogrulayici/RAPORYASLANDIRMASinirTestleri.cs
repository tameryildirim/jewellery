using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET RAPOR-YASLANDIRMA finansal sınır testleri.
// Cari yaşlandırma raporu (açılış → "Tarihsiz"/BELIRSIZ dilimi, döviz tek kaynak) için:
// sıfır · eksi · kuruş yuvarlaması · has 4 hane · çok büyük tutar · boş/null ·
// açılış geri alma ve karma döviz açılışı senaryoları. Beklenen değerler SABİT.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class RAPORYASLANDIRMASinirTestleri(TestVeritabani vt)
{
    private const string Gerekce = "Doğrulayıcı sınır testi açılış bakiyesi";

    private static MCari.CariServisi Servis(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));

    private Kiraci Kur(string ad)
    {
        using var db = vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private string CariEkle(Kiraci k, string ad)
    {
        using var db = vt.OlusturSistem();
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Cariler.Add(new Cari
        {
            Id = id, TenantId = k.TenantId, Tur = "MUSTERI", Ad = ad, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    private async Task Acilis(Kiraci k, string cariId, decimal tl, decimal has = 0m, string? pb = null, decimal? doviz = null)
    {
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).AcilisYaz(cariId, new MCari.CariDuzeltmeDto
        {
            TlDelta = tl, HasDelta = has, Gerekce = Gerekce, KurSnapshotId = k.KurId,
            DovizParaBirimi = pb, DovizDelta = doviz,
        }, k.Islem);
    }

    private async Task Veresiye(Kiraci k, string cariId, decimal tl)
    {
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).Duzeltme(cariId, new MCari.CariDuzeltmeDto
        {
            TlDelta = tl, HasDelta = 0m, Gerekce = "Doğrulayıcı veresiye borcu yazımı", KurSnapshotId = k.KurId,
        }, k.Islem, krediLimitiUygula: false);
    }

    private async Task<JsonElement> Rapor(Kiraci k)
    {
        using var db = vt.Olustur(k.TenantId);
        var r = await TestKurulum.RaporServisi(db).CariYaslandirma(k.Islem);
        return JsonDocument.Parse(JsonSerializer.Serialize(r)).RootElement;
    }

    private static JsonElement? Satir(JsonElement r, string cariId)
    {
        foreach (var s in r.GetProperty("yaslananCariler").EnumerateArray())
            if (s.GetProperty("cariId").GetString() == cariId) return s;
        return null;
    }

    /// <summary>SIFIR — açılış +1000 sonra ters açılış −1000: cari rapordan düşer, Tarihsiz 0.</summary>
    [Fact]
    public async Task Sifir_acilis_ve_tersi_cari_raporda_yok_belirsiz_0()
    {
        var k = Kur("RYD sifir");
        var c = CariEkle(k, "RYD sıfır");
        await Acilis(k, c, 1_000m);
        await Acilis(k, c, -1_000m);

        var r = await Rapor(k);
        Assert.Null(Satir(r, c));
        Assert.Equal(0, r.GetProperty("toplamSatir").GetInt32());
        Assert.Equal(0m, r.GetProperty("ozetTl").GetProperty("BELIRSIZ").GetDecimal());
        Assert.Equal(0m, r.GetProperty("ozetTl").GetProperty("0-30").GetDecimal());
    }

    /// <summary>EKSİ — biz borçluyuz (−250,50): işaret korunur, Tarihsiz dilimde, 0-30'a karışmaz.</summary>
    [Fact]
    public async Task Eksi_acilis_isareti_korunur_belirsiz_dilimde()
    {
        var k = Kur("RYD eksi");
        var c = CariEkle(k, "RYD eksi");
        await Acilis(k, c, -250.50m);

        var r = await Rapor(k);
        var s = Satir(r, c)!.Value;
        Assert.Equal("BELIRSIZ", s.GetProperty("dilim").GetString());
        Assert.Equal(-250.50m, s.GetProperty("tlBakiye").GetDecimal());
        Assert.Equal(-250.50m, r.GetProperty("ozetTl").GetProperty("BELIRSIZ").GetDecimal());
        Assert.Equal(0m, r.GetProperty("ozetTl").GetProperty("0-30").GetDecimal());
    }

    /// <summary>KURUŞ — 0,015 açılış AwayFromZero → 0,02 (yazıcı kuralı); Tarihsiz 0,02. 0,004 → 0,00 → listede yok.</summary>
    [Fact]
    public async Task Kurus_yuvarlamasi_0_015_iki_kurus_0_004_listede_yok()
    {
        var k = Kur("RYD kurus");
        var c = CariEkle(k, "RYD kuruş");
        var d = CariEkle(k, "RYD sıfıra yuvarlanan");
        await Acilis(k, c, 0.015m);
        await Acilis(k, d, 0.004m);

        var r = await Rapor(k);
        var s = Satir(r, c)!.Value;
        Assert.Equal(0.02m, s.GetProperty("tlBakiye").GetDecimal());
        Assert.Equal("BELIRSIZ", s.GetProperty("dilim").GetString());
        Assert.Equal(0.02m, r.GetProperty("ozetTl").GetProperty("BELIRSIZ").GetDecimal());
        Assert.Null(Satir(r, d));
    }

    /// <summary>HAS 4 HANE — yalnız 0,0001 g has açılışı: satır görünür, has kaybolmaz, TL özeti 0.</summary>
    [Fact]
    public async Task Has_0_0001_acilis_gorunur_belirsiz_tl_ozeti_0()
    {
        var k = Kur("RYD has");
        var c = CariEkle(k, "RYD has");
        await Acilis(k, c, 0m, has: 0.0001m);

        var r = await Rapor(k);
        var s = Satir(r, c)!.Value;
        Assert.Equal("BELIRSIZ", s.GetProperty("dilim").GetString());
        Assert.Equal(0.0001m, Math.Abs(s.GetProperty("hasGramBakiye").GetDecimal()));
        Assert.Equal(0m, r.GetProperty("ozetTl").GetProperty("BELIRSIZ").GetDecimal());
    }

    /// <summary>ÇOK BÜYÜK — 999.999.999,99 açılış Tarihsiz özetinde kuruşu kuruşuna.</summary>
    [Fact]
    public async Task Cok_buyuk_tutar_999999999_99_belirsiz_ozette_birebir()
    {
        var k = Kur("RYD buyuk");
        var c = CariEkle(k, "RYD büyük");
        var v = CariEkle(k, "RYD veresiye");
        await Acilis(k, c, 999_999_999.99m);
        await Veresiye(k, v, 0.01m + 0.01m);

        var r = await Rapor(k);
        Assert.Equal(999_999_999.99m, r.GetProperty("ozetTl").GetProperty("BELIRSIZ").GetDecimal());
        Assert.Equal(0.02m, r.GetProperty("ozetTl").GetProperty("0-30").GetDecimal());
    }

    /// <summary>BOŞ — carisiz kiracı: liste boş, tüm dilimler 0, ozetDoviz her kolon kodu 0 (USD/EUR/GBP).</summary>
    [Fact]
    public async Task Bos_kiraci_dilimler_0_ozetDoviz_kodlari_0()
    {
        var k = Kur("RYD bos");
        var r = await Rapor(k);
        Assert.Empty(r.GetProperty("yaslananCariler").EnumerateArray());
        foreach (var d in new[] { "0-30", "31-60", "61-90", "90+", "BELIRSIZ" })
            Assert.Equal(0m, r.GetProperty("ozetTl").GetProperty(d).GetDecimal());
        var oz = r.GetProperty("ozetDoviz");
        foreach (var pb in new[] { "USD", "EUR", "GBP" })
            Assert.Equal(0m, oz.GetProperty(pb).GetDecimal());
    }

    /// <summary>NULL — saf kural: tarih yok → gün null → BELIRSIZ; döviz metni null sözlükte boş.</summary>
    [Fact]
    public void Null_tarih_belirsiz_doviz_metni_bos()
    {
        Assert.Null(Kuyumcu.Api.Modules.Rapor.CariYaslandirmaKurali.GunSayisi(null, DateTime.UtcNow));
        Assert.Equal("BELIRSIZ", Kuyumcu.Api.Modules.Rapor.CariYaslandirmaKurali.Dilim(null));
        Assert.Equal("", Kuyumcu.Web.Pages.Raporlar.DovizMetni(null));
        Assert.False(DovizBirimleri.BakiyeVar(null));
    }

    /// <summary>
    /// SENARYO — açılış 1000, ardından 50 TL yeni veresiye, ardından açılış TERS kaydı (−1000).
    /// Kalan 50 TL bugünkü borçtur: 0-30 diliminde olmalı, "Açılış bakiyesi" denmemeli.
    /// </summary>
    [Fact]
    public async Task Acilis_geri_alininca_kalan_yeni_borc_0_30_da_kalir()
    {
        var k = Kur("RYD gerial");
        var c = CariEkle(k, "RYD geri alma");
        await Acilis(k, c, 1_000m);
        await Veresiye(k, c, 50m);
        await Acilis(k, c, -1_000m);

        var r = await Rapor(k);
        var s = Satir(r, c)!.Value;
        Assert.Equal(50m, s.GetProperty("tlBakiye").GetDecimal());
        Assert.Equal("0-30", s.GetProperty("dilim").GetString());
        Assert.Equal(50m, r.GetProperty("ozetTl").GetProperty("0-30").GetDecimal());
    }

    /// <summary>
    /// SENARYO — karma döviz açılışı (USD +100, EUR −100) sonra 50 TL veresiye: açılış hâlâ
    /// bakiyede → acilisDahil TRUE olmalı (farklı birimler toplanıp sıfırlanmamalı).
    /// </summary>
    [Fact]
    public async Task Karma_doviz_acilisi_birimler_toplanip_sifirlanmaz_acilisDahil()
    {
        var k = Kur("RYD karma");
        var c = CariEkle(k, "RYD karma döviz");
        await Acilis(k, c, 0m, pb: "USD", doviz: 100m);
        await Acilis(k, c, 0m, pb: "EUR", doviz: -100m);
        await Veresiye(k, c, 50m);

        var r = await Rapor(k);
        var s = Satir(r, c)!.Value;
        Assert.Equal("0-30", s.GetProperty("dilim").GetString());
        Assert.True(s.GetProperty("acilisDahil").GetBoolean());
    }

    // ─── Tur 2 hasım ekleri (2026-10-01) ─────────────────────────────────────

    /// <summary>Carinin TÜM hareketlerini ve son işlem tarihini gun gün + 1 saat geriye alır.</summary>
    private void GeriTarihle(Kiraci k, string cariId, int gun)
    {
        using var db = vt.OlusturSistem();
        var t = DateTime.UtcNow.AddDays(-gun).AddHours(-1);
        var b = db.CariBakiyeler.Single(x => x.TenantId == k.TenantId && x.CariId == cariId);
        b.SonIslemTarihi = t;
        foreach (var h in db.Hareketler.Where(x => x.TenantId == k.TenantId && x.CariId == cariId))
            h.Tarih = t;
        db.SaveChanges();
    }

    /// <summary>SIFIR (servis) — sıfır açılış 400; rapor satırı doğmaz.</summary>
    [Fact]
    public async Task T2_Sifir_acilis_reddedilir_raporda_satir_yok()
    {
        var k = Kur("RYD2 sifir");
        var c = CariEkle(k, "RYD2 sıfır");
        await Assert.ThrowsAsync<BadRequestException>(() => Acilis(k, c, 0m));
        var r = await Rapor(k);
        Assert.Null(Satir(r, c));
        Assert.Equal(0m, r.GetProperty("ozetTl").GetProperty("BELIRSIZ").GetDecimal());
    }

    /// <summary>NULL (DB) — işlem tarihi olmayan bakiye: BELIRSIZ / TARIH_YOK, açılış bayrağı yok.</summary>
    [Fact]
    public async Task T2_Tarih_null_bakiye_TARIH_YOK_acilis_bayragi_yok()
    {
        var k = Kur("RYD2 null");
        var c = CariEkle(k, "RYD2 tarihsiz");
        await Veresiye(k, c, 75m);
        using (var db = vt.OlusturSistem())
        {
            var b = db.CariBakiyeler.Single(x => x.TenantId == k.TenantId && x.CariId == c);
            b.SonIslemTarihi = null;
            db.SaveChanges();
        }
        var r = await Rapor(k);
        var s = Satir(r, c)!.Value;
        Assert.Equal("BELIRSIZ", s.GetProperty("dilim").GetString());
        Assert.Equal("TARIH_YOK", s.GetProperty("belirsizNedeni").GetString());
        Assert.False(s.GetProperty("acilisDahil").GetBoolean());
        Assert.Equal(75m, r.GetProperty("ozetTl").GetProperty("BELIRSIZ").GetDecimal());
    }

    /// <summary>GÜN SINIRI — açılışsız cari: 30→0-30, 31→31-60, 90→61-90, 91→90+.</summary>
    [Theory]
    [InlineData(30, "0-30")]
    [InlineData(31, "31-60")]
    [InlineData(90, "61-90")]
    [InlineData(91, "90+")]
    public async Task T2_Gun_siniri_acilissiz_cari(int gun, string beklenen)
    {
        var k = Kur($"RYD2 gun {gun}");
        var c = CariEkle(k, $"RYD2 {gun} gün");
        await Veresiye(k, c, 10m);
        GeriTarihle(k, c, gun);
        var r = await Rapor(k);
        var s = Satir(r, c)!.Value;
        Assert.Equal(beklenen, s.GetProperty("dilim").GetString());
        Assert.Equal(gun, s.GetProperty("gunSayisi").GetInt32());
        Assert.Equal(10m, r.GetProperty("ozetTl").GetProperty(beklenen).GetDecimal());
    }

    /// <summary>GBP tek başına açılış: süzgeçten geçer, BELIRSIZ, satır ve özet GBP 12,34.</summary>
    [Fact]
    public async Task T2_Yalniz_GBP_acilisi_BELIRSIZ_ve_ozet_GBP()
    {
        var k = Kur("RYD2 gbp");
        var c = CariEkle(k, "RYD2 gbp");
        await Acilis(k, c, 0m, pb: "GBP", doviz: 12.34m);
        var r = await Rapor(k);
        var s = Satir(r, c)!.Value;
        Assert.Equal("BELIRSIZ", s.GetProperty("dilim").GetString());
        Assert.Equal("ACILIS", s.GetProperty("belirsizNedeni").GetString());
        Assert.Equal(12.34m, s.GetProperty("dovizBakiyeleri").GetProperty("GBP").GetDecimal());
        Assert.Equal(12.34m, r.GetProperty("ozetDoviz").GetProperty("GBP").GetDecimal());
    }

    /// <summary>
    /// GÖZLEM — açılıştan ÖNCE 100 günlük veresiyesi olan cari: kural (cari başına tek dilim)
    /// tüm satırı Tarihsiz'e alır; bilinen 90+ borç 90+ özetinde görünmez. Bugünkü davranışı
    /// sabitler (bulgu DÜŞÜK — rapora yazıldı).
    /// </summary>
    [Fact]
    public async Task T2_Acilistan_once_yasli_borc_kural_geregi_Tarihsiz_e_karisir()
    {
        var k = Kur("RYD2 once");
        var c = CariEkle(k, "RYD2 önce borç");
        await Veresiye(k, c, 100m);
        GeriTarihle(k, c, 100);
        await Acilis(k, c, 1_000m);
        var r = await Rapor(k);
        var s = Satir(r, c)!.Value;
        Assert.Equal(1_100m, s.GetProperty("tlBakiye").GetDecimal());
        Assert.Equal("BELIRSIZ", s.GetProperty("dilim").GetString());
        Assert.Equal(0m, r.GetProperty("ozetTl").GetProperty("90+").GetDecimal());
        Assert.Equal(1_100m, r.GetProperty("ozetTl").GetProperty("BELIRSIZ").GetDecimal());
    }
}
