using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Evrak;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests.Dogrulayici;

// ═══════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET CARI-BAKIYE finansal sınır testleri.
// Beklenen değerler SABİT ve elle hesaplandı (üretim kodundan türetilmez).
// Yuvarlama kuralı: CariBakiyeYazici TL 2 / has 6 hane, MidpointRounding.AwayFromZero.
// ═══════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class CARIBAKIYESinirTestleri(TestVeritabani vt)
{
    private const string Gerekce = "CARI-BAKIYE dogrulayici sinir testi — yeterince uzun gerekce";

    private Kiraci YeniKiraci(string ad)
    {
        using var kurulum = vt.OlusturSistem();
        return TestKurulum.KiraciKur(kurulum, $"PAKETCARI-BAKIYE-TEST dg {ad}");
    }

    private async Task<(decimal Tl, decimal Has)> Uygula(Kiraci k, decimal tl, decimal has)
    {
        using (var db = vt.Olustur(k))
        {
            await CariBakiyeYazici.Uygula(db, k.TenantId, k.CariId, tl, has, DateTime.UtcNow);
            await db.SaveChangesAsync();
        }
        using var oku = vt.OlusturSistem();
        var b = await oku.CariBakiyeler.AsNoTracking()
            .SingleAsync(x => x.TenantId == k.TenantId && x.CariId == k.CariId);
        return (b.TlBakiye, b.HasGramBakiye);
    }

    [Fact]
    public async Task Sifir_delta_bakiyeyi_degistirmez()
    {
        var k = YeniKiraci("sifir");
        Assert.Equal((0m, 0m), await Uygula(k, 0m, 0m));
    }

    [Fact]
    public async Task Eksi_delta_isareti_korunur()
    {
        var k = YeniKiraci("eksi");
        Assert.Equal((-150.25m, -2.5m), await Uygula(k, -150.25m, -2.5m));
    }

    [Fact]
    public async Task Kurus_0005_AwayFromZero_ve_has_6_hane_0001_korunur()
    {
        var k = YeniKiraci("kurus");
        Assert.Equal((0.01m, 0.0001m), await Uygula(k, 0.005m, 0.0001m));
        // 0,01 − 0,015 = −0,005 → AwayFromZero −0,01 ; 0,0001 + 0,0000005 → 0,000101 (6 hane)
        Assert.Equal((-0.01m, 0.000101m), await Uygula(k, -0.015m, 0.0000005m));
    }

    [Fact]
    public async Task Cok_buyuk_tutar_kaybolmaz()
    {
        var k = YeniKiraci("buyuk");
        Assert.Equal((999_999_999.99m, 0m), await Uygula(k, 999_999_999.99m, 0m));
        Assert.Equal((1_000_000_000.00m, 0m), await Uygula(k, 0.01m, 0m));
    }

    [Fact]
    public void Bos_null_veresiye_payi_uydurulmaz_bos_grup_sifir()
    {
        Assert.Null(CariBakiyeKurali.TlEtkisi("SATIS", 100m, null, false));
        Assert.Equal(0m, CariBakiyeKurali.TlEtkisi("SATIS", 0m, null, false));
        Assert.Equal(0m, CariBakiyeKurali.TlAynasi([]));
        Assert.Equal(0m, CariBakiyeKurali.EmanetsizHas(0m, 0m));
        Assert.Equal(-5m + 5m, CariBakiyeKurali.EmanetsizHas(-5m, -5m));   // Fatma örneği: emanetsiz 0
    }

    [Theory]
    [InlineData("45.000", 45000.0)]
    [InlineData("1,5", 1.5)]
    [InlineData("0,0001", 0.0001)]
    public void Turkce_giris_cozulur(string ham, double beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.True(c.Tamam);
        Assert.Equal((decimal)beklenen, c.Deger);
    }

    [Fact]
    public void Turkce_giris_belirsiz_1500_reddedilir()
    {
        var c = FormAlani.SayiCoz("1.500");
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, c.Sonuc);
        Assert.Null(c.Deger);
    }

    /// <summary>
    /// Evrak tahsili (TAHSILAT +tutar) cari TL'ye YAZMAZ (EvrakModulu çift sayma yasağı) ve
    /// ayna bunu 0 sayar. Stornosu da cariye YAZMAMALI; yazarsa TL aynası uyuşmazlık gösterir.
    /// Beklenen: evrak girişi −10.000 TL, tahsil + storno sonrası yine −10.000 TL ve TL farkı 0.
    /// </summary>
    [Fact]
    public async Task Evrak_tahsili_stornosu_cari_TL_yi_ve_aynayi_bozmaz()
    {
        var k = YeniKiraci("evrak storno");
        static EvrakServisi Evrak(KuyumcuDbContext db) =>
            new(db, NullLogger<EvrakServisi>.Instance, TestKurulum.Denetim(db),
                Kuyumcu.Api.Modules.Tevsik.TevsikKapisi.Varsayilan());

        string evrakId;
        using (var db = vt.Olustur(k))
            evrakId = (await Evrak(db).Gir(new EvrakGirisDto
            {
                EvrakTuru = "CEK", Tutar = 10_000m, VadeGunu = Zaman.YerelBugun().AddDays(30),
                EvrakNo = "CEK-DG-CB-1", BankaAdi = "Ziraat",
                KaynakCariId = k.CariId, KurSnapshotId = k.KurId,
            }, k.Islem)).Evrak.Id;
        using (var db = vt.Olustur(k))
            await Evrak(db).TahsilEdildi(evrakId, new TahsilEdildiDto
            {
                Hedef = "KASA", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);

        string tahsil;
        using (var oku = vt.OlusturSistem())
            tahsil = await oku.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT")
                .Select(h => h.Id).SingleAsync();

        decimal tlOnce;
        using (var db = vt.Olustur(k))
        {
            var a = await CariBakiyeKurali.Hesapla(db, k.TenantId, k.CariId);
            tlOnce = a.KayitliTl;
            Assert.Equal(0m, a.TlUyusmazligi);
        }

        var stornoOldu = true;
        try
        {
            using var db = vt.Olustur(k);
            await new MSatis.StornoServisi(db, TestKurulum.Denetim(db), NullLogger<MSatis.StornoServisi>.Instance)
                .Storno(tahsil, new MSatis.StornoDto { Gerekce = Gerekce, KurSnapshotId = k.KurId, KasaId = k.KasaId }, k.Islem);
        }
        catch (Exception ex) when (ex is BadRequestException or ConflictException)
        {
            stornoOldu = false;   // storno reddediliyorsa bakiye bozulamaz — kabul
        }

        using (var db = vt.Olustur(k))
        {
            var a = await CariBakiyeKurali.Hesapla(db, k.TenantId, k.CariId);
            Assert.Equal(-10_000m, tlOnce);
            Assert.True(a.KayitliTl == -10_000m,
                $"storno={stornoOldu}: evrak tahsili stornosu cari TL'yi {a.KayitliTl} yaptı (beklenen −10.000)");
            Assert.Equal(0m, a.TlUyusmazligi);
        }
    }
}

// ═══════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI TUR 2 (01.10) — tahsilat TL kuralı, emanetsiz has 4 hane,
// fatura taslak birim has gidiş-dönüş sınırları. Beklenenler elle hesaplandı.
// ═══════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class CARIBAKIYETur2SinirTestleri(TestVeritabani vt)
{
    [Theory]
    [InlineData("0", "0")]
    [InlineData("-0.01", "-0.01")]
    [InlineData("0.01", "0")]
    [InlineData("-999999999.99", "-999999999.99")]
    [InlineData("999999999.99", "0")]
    public void Tahsilat_cari_TL_sinirlari(string kaynak, string beklenen) =>
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture),
            CariBakiyeKurali.TahsilatCariTl(decimal.Parse(kaynak, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void Tahsilat_storno_grubu_ayna_isareti()
    {
        // Müşteri ödemesi (−500) stornosu: satır +500, storno → cariye +500 yazılır.
        Assert.Equal(500m, CariBakiyeKurali.GrupTlEtkisi(new CariAynaGrubu("TAHSILAT", null, true, 1, false, 500m, 0m, 0m)));
        // Evrak tahsili (+500) stornosu: satır −500, storno → cariye 0.
        Assert.Equal(0m, CariBakiyeKurali.GrupTlEtkisi(new CariAynaGrubu("TAHSILAT", null, true, -1, false, -500m, 0m, 0m)));
        // Sıfır TL tahsilat (yalnız has): TL etkisi 0.
        Assert.Equal(0m, CariBakiyeKurali.GrupTlEtkisi(new CariAynaGrubu("TAHSILAT", null, false, 0, false, 0m, 0m, 0m)));
    }

    [Fact]
    public void Emanetsiz_has_4_hane()
    {
        Assert.Equal(0m, CariBakiyeKurali.EmanetsizHas(-0.0001m, -0.0001m));
        Assert.Equal(1.2346m, CariBakiyeKurali.EmanetsizHas(1.2345m, -0.0001m));
        Assert.Equal(-0.0001m, CariBakiyeKurali.HasEtkisi("EMANET_GIRIS", 0.0001m, "ALTIN"));
        Assert.Equal(0m, CariBakiyeKurali.HasEtkisi("EMANET_GIRIS", 100m, "GUMUS"));
        Assert.Equal(0m, CariBakiyeKurali.HasEtkisi("EMANET_GIRIS", 100m, null));
    }

    private async Task<(string Fid, string Kid, Kiraci K)> Taslak(decimal birimHas, decimal miktar, string ad)
    {
        Kiraci k;
        using (var kurulum = vt.OlusturSistem())
            k = TestKurulum.KiraciKur(kurulum, $"PAKETCARI-BAKIYE-TEST dg2 {ad}");
        using (var s = vt.OlusturSistem())
        {
            var u = await s.Urunler.SingleAsync(x => x.Id == k.UrunId);
            u.HasGrami = birimHas;
            await s.SaveChangesAsync();
        }
        using var db = vt.Olustur(k);
        await new Kuyumcu.Api.Modules.Fatura.FaturaServisi(db).Olustur(new Kuyumcu.Api.Modules.Fatura.FaturaOlusturDto
        {
            CariId = k.CariId, Toplam = miktar * 20_000m, KdvOran = 0.20m, KdvTuru = "KUYUM",
            Kalemler = [new Kuyumcu.Api.Modules.Fatura.FaturaKalemDto { UrunId = k.UrunId, Miktar = miktar, BirimFiyat = 20_000m }],
        }, k.Islem);
        var fid = db.Faturalar.Single(x => x.TenantId == k.TenantId).Id;
        var kid = db.FaturaKalemleri.Single(x => x.FaturaId == fid).Id;
        return (fid, kid, k);
    }

    private decimal? KalemHas(string kid)
    {
        using var o = vt.OlusturSistem();
        return o.FaturaKalemleri.AsNoTracking().Single(x => x.Id == kid).HasGrami;
    }

    private async Task Patch(Kiraci k, string fid, string kid, string govde)
    {
        using var db = vt.Olustur(k);
        await new Kuyumcu.Api.Modules.Fatura.FaturaServisi(db)
            .TaslakKalemGuncelle(fid, kid, System.Text.Json.JsonDocument.Parse(govde).RootElement, k.Islem);
    }

    [Fact]
    public async Task Fatura_birim_has_0001_gidis_donus_kaymaz()
    {
        var (fid, kid, k) = await Taslak(0.0001m, 3m, "b0001");
        Assert.Equal(0.0003m, KalemHas(kid));
        await Patch(k, fid, kid, "{\"miktar\":1.001}");
        Assert.Equal(0.0001m, KalemHas(kid));   // 0,0001001 → 6 hane 0,000100
        await Patch(k, fid, kid, "{\"miktar\":3}");
        Assert.Equal(0.0003m, KalemHas(kid));
    }

    [Fact]
    public async Task Fatura_sifir_ve_eksi_miktar_reddedilir_has_degismez()
    {
        var (fid, kid, k) = await Taslak(1.5m, 2m, "sifir");
        await Assert.ThrowsAsync<BadRequestException>(() => Patch(k, fid, kid, "{\"miktar\":0}"));
        await Assert.ThrowsAsync<BadRequestException>(() => Patch(k, fid, kid, "{\"miktar\":-1}"));
        Assert.Equal(3m, KalemHas(kid));
    }

    [Fact]
    public async Task Fatura_buyuk_miktar_birimden_hesaplanir()
    {
        var (fid, kid, k) = await Taslak(1.234567m, 1m, "buyuk");
        await Patch(k, fid, kid, "{\"miktar\":999999.999}");
        Assert.Equal(1234566.998765m, KalemHas(kid));   // 999999,999 × 1,234567 = 1234567 − 0,001234567 = 1234566,998765433 → 6 hane
        await Patch(k, fid, kid, "{\"miktar\":1}");
        Assert.Equal(1.234567m, KalemHas(kid));
    }

    /// <summary>
    /// Birimsiz ESKİ kalem (toplam 1, miktar 3 → birim 0,333333 donar): 3 → 1,001 → 3 dönüşü
    /// 0,999999 verir (1e-6 kayma). Has 4 hane (0,0001) eşiğinin ALTINDA kalmalı.
    /// </summary>
    [Fact]
    public async Task Fatura_birimsiz_eski_kalem_ucte_bir_kaymasi_4_hane_esiginin_altinda()
    {
        var (fid, kid, k) = await Taslak(1m, 3m, "ucte");
        using (var s = vt.OlusturSistem())
            await s.FaturaKalemleri.Where(x => x.Id == kid)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.BirimHasGrami, (decimal?)null)
                                          .SetProperty(x => x.HasGrami, 1m));
        await Patch(k, fid, kid, "{\"miktar\":1.001}");
        await Patch(k, fid, kid, "{\"miktar\":3}");
        var son = KalemHas(kid)!.Value;
        Assert.Equal(0.999999m, son);                 // gerçek davranış (belgelendi)
        Assert.True(Math.Abs(son - 1m) < 0.0001m);
    }
}
