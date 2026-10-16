using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Arama;
using Kuyumcu.Api.Modules.Yetki;
using Kuyumcu.Contracts.Arama;
using Microsoft.Extensions.Caching.Memory;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET OLCEK-YUK finansal sınır testleri.
//
// Paket FisAra'nın toplama sorgusunu SQL'e taşıdı: fiş toplamı (TL) artık
// "sayfadaki fişlerin TÜM satırlarından" SQL SUM ile hesaplanıp EkranPara ile
// gösteriliyor. Bu dosya o para yolunu sınırlarda sınar (kalıcı gerileme testi):
// sıfır · eksi (işaret "−") · kuruş yuvarlaması (AwayFromZero, 0,005) ·
// çok büyük tutar · carisiz (null) fiş · kısmi cari eşleşmesinde TÜM satırların toplamı.
// Beklenen değerler SABİT yazılmıştır; üretim kodundan hesaplanmaz.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class OLCEKYUKSinirTestleri(TestVeritabani vt)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var kurulum = vt.OlusturSistem();
        return TestKurulum.KiraciKur(kurulum, $"PAKETOLCEK-YUK-TEST {ad}");
    }

    /// <summary>Verilen fiş kimliğiyle defter satırları yazar (tek yol: HareketFabrikasi.Ekle).</summary>
    private void FisYaz(Kiraci k, string fisId, params (decimal tl, string? cariId)[] satirlar)
    {
        using var db = vt.Olustur(k);
        var now = DateTime.UtcNow;
        foreach (var (tl, cariId) in satirlar)
        {
            HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS",
                Tarih = now, TlTutari = tl, HasGrami = 0m, Adet = 1m, FisId = fisId,
                KurSnapshotId = k.KurId, CariId = cariId, UrunId = k.UrunId,
                KullaniciId = k.KullaniciId, Olusturuldu = now,
            });
        }
        db.SaveChanges();
    }

    private async Task<AramaSonucu> TekFis(Kiraci k, string q)
    {
        using var db = vt.Olustur(k);
        var servis = new AramaServisi(db, new YetkiServisi(db, new MemoryCache(new MemoryCacheOptions())));
        var yanit = await servis.Ara(q, "FIS", 20, 0, null, k.Islem);
        return Assert.Single(yanit.Sonuclar);
    }

    private static string Fis(string onek) => onek + Kimlik.Ulid()[..10];

    [Fact]
    public async Task SNR01_Sifir_toplamli_fis_isaretsiz_sifir_gosterir()
    {
        var k = YeniKiraci("sifir");
        var f = Fis("SNRSIFIR");
        FisYaz(k, f, (0m, null));
        var s = await TekFis(k, f);
        Assert.Equal("1 kalem · 0 ₺", s.AltBilgi);
    }

    [Fact]
    public async Task SNR02_Eksi_toplamli_fis_isaret_dusmez()
    {
        var k = YeniKiraci("eksi");
        var f = Fis("SNREKSI");
        FisYaz(k, f, (-1500.50m, null), (-1500.50m, null));
        var s = await TekFis(k, f);
        Assert.Equal("2 kalem · −3.001 ₺", s.AltBilgi);
    }

    [Fact]
    public async Task SNR03_Kurus_toplami_SQL_de_kayipsiz()
    {
        var k = YeniKiraci("kurus");
        var f = Fis("SNRKURUS");
        FisYaz(k, f, (0.10m, null), (0.20m, null), (0.01m, null));
        var s = await TekFis(k, f);
        Assert.Equal("3 kalem · 0,31 ₺", s.AltBilgi);
    }

    [Theory]
    [InlineData("2.005", "2,01 ₺")]      // AwayFromZero (banker 2,00 verirdi)
    [InlineData("2.004", "2 ₺")]         // aşağı → tam sayı N0
    [InlineData("-2.005", "2,01 ₺")]     // mutlak değer; işaret çağıranda
    [InlineData("1234.565", "1.234,57 ₺")]
    [InlineData("0", "0 ₺")]
    public void SNR04_EkranPara_yarim_kurus_AwayFromZero(string girdi, string beklenen)
    {
        var n = decimal.Parse(girdi, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(beklenen, AramaServisi.EkranPara(n, "TRY"));
    }

    [Fact]
    public async Task SNR05_Cok_buyuk_tutar_tasmadan_toplanir()
    {
        var k = YeniKiraci("buyuk");
        var f = Fis("SNRBUYUK");
        FisYaz(k, f, (999_999_999.99m, null), (999_999_999.99m, null));
        var s = await TekFis(k, f);
        Assert.Equal("2 kalem · 1.999.999.999,98 ₺", s.AltBilgi);
    }

    [Fact]
    public async Task SNR06_Carisiz_fis_tire_adla_ve_bos_sorgu_bos_yanitla()
    {
        var k = YeniKiraci("null");
        var f = Fis("SNRNULL");
        FisYaz(k, f, (100m, null));
        var s = await TekFis(k, f);
        Assert.StartsWith("— · ", s.Ad);

        using var db = vt.Olustur(k);
        var servis = new AramaServisi(db, new YetkiServisi(db, new MemoryCache(new MemoryCacheOptions())));
        var bos = await servis.Ara(null, "GENEL", 20, 0, null, k.Islem);
        Assert.Empty(bos.Sonuclar);
    }

    [Fact]
    public async Task SNR07_Cari_adi_eslesmesinde_toplam_fisin_TUM_satirlarindan()
    {
        var k = YeniKiraci("kismi");
        var f = Fis("SNRKISMI");
        // Yalnız bir satır carili; ad eşleşmesi o satırdan gelir, toplam iki satırdan.
        FisYaz(k, f, (1000m, k.CariId), (250.25m, null));
        var s = await TekFis(k, "PAKETOLCEK-YUK-TEST kismi");
        Assert.Equal(f, s.Kimlik);
        Assert.Equal("2 kalem · 1.250,25 ₺", s.AltBilgi);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI t2 (Aşkın + Mehmet) — ürün bazlı kâr SAYFALAMA (ofset) para yolu.
// Paket t2'de UrunBazli'ye ofset + toplamUrunSayisi eklendi; sayfa sınırında para
// toplamları, kuruş/has yuvarlaması ve sayfa sınırı kayıpsız mı? Beklenenler SABİT.
// Kur fotoğrafı (TestKurulum): AltinHasFiyati = 6.827,20.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class OLCEKYUKKarSayfaSinirTestleri(TestVeritabani vt)
{
    private static System.Text.Json.JsonElement J(object o) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o)).RootElement;

    private static DateOnly Bugun => DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));

    private Kiraci YeniKiraci(string ad)
    {
        using var kurulum = vt.OlusturSistem();
        return TestKurulum.KiraciKur(kurulum, $"PAKETOLCEK-YUK-TEST {ad}");
    }

    private string Urun(Kiraci k, string ad)
    {
        using var db = vt.Olustur(k);
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();
        db.Urunler.Add(new Urun
        {
            Id = id, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"SNRK-{id[^10..]}", Ad = ad,
            AyarId = "K22", Milyem = 916m, GramAgirlik = 5m, TasAgirlik = 0m, HasGrami = 4.58m,
            IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    private void Yaz(Kiraci k, string urunId, string tur, params decimal[] tutarlar)
    {
        using var db = vt.Olustur(k);
        var now = DateTime.UtcNow;
        foreach (var tl in tutarlar)
            HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = tur,
                Tarih = now, TlTutari = tl, HasGrami = 0m, Adet = 1m, FisId = "SNRK" + Kimlik.Ulid()[..10],
                KurSnapshotId = k.KurId, CariId = k.CariId, UrunId = urunId,
                KullaniciId = k.KullaniciId, Olusturuldu = now,
            });
        db.SaveChanges();
    }

    private async Task<System.Text.Json.JsonElement> Sayfa(Kiraci k, int limit, int ofset)
    {
        using var db = vt.Olustur(k);
        return J(await new Kuyumcu.Api.Modules.Maliyet.KarZararServisi(db).UrunBazli(k.Islem, Bugun, Bugun, null, limit, ofset));
    }

    private static System.Text.Json.JsonElement Satir(System.Text.Json.JsonElement y, string urunId) =>
        y.GetProperty("satirlar").EnumerateArray().Single(s => s.GetProperty("urunId").GetString() == urunId);

    [Fact]
    public async Task SNRK01_Sifir_tutarli_satis_sifir_hasilat_ve_sayilir()
    {
        var k = YeniKiraci("snrk01");
        var u = Urun(k, "sifir");
        Yaz(k, u, "SATIS", 0m);
        var y = await Sayfa(k, 50, 0);
        Assert.Equal(1, y.GetProperty("toplamUrunSayisi").GetInt32());
        Assert.Equal(0m, Satir(y, u).GetProperty("hasilatTl").GetDecimal());
        Assert.Equal(0m, Satir(y, u).GetProperty("hasilatHas").GetDecimal());
    }

    [Fact]
    public async Task SNRK02_Iade_satistan_buyukse_hasilat_eksi_isaretli_ve_eksi_ofset_limit_reddedilir()
    {
        var k = YeniKiraci("snrk02");
        var u = Urun(k, "eksi");
        Yaz(k, u, "SATIS", 1_000m);
        Yaz(k, u, "SATIS_IADE", -1_500m);
        var s = Satir(await Sayfa(k, 50, 0), u);
        Assert.Equal(1_000m, s.GetProperty("brutHasilatTl").GetDecimal());
        Assert.Equal(1_500m, s.GetProperty("iadeTl").GetDecimal());
        Assert.Equal(-500m, s.GetProperty("hasilatTl").GetDecimal());
        await Assert.ThrowsAsync<BadRequestException>(() => Sayfa(k, 50, -1));
        await Assert.ThrowsAsync<BadRequestException>(() => Sayfa(k, 0, 0));
        await Assert.ThrowsAsync<BadRequestException>(() => Sayfa(k, 501, 0));
    }

    [Fact]
    public async Task SNRK03_Yarim_kurus_AwayFromZero_ve_kolon_olcegi()
    {
        var k = YeniKiraci("snrk03");
        var u = Urun(k, "kurus");
        // decimal(15,2) kolon: 1000,005 → 1000,01 (yarım yukarı); iki satır toplamı 2000,02.
        Yaz(k, u, "SATIS", 1_000.005m, 1_000.005m);
        var s = Satir(await Sayfa(k, 50, 0), u);
        Assert.Equal(2_000.02m, s.GetProperty("brutHasilatTl").GetDecimal());
        Assert.Equal(2_000.02m, s.GetProperty("hasilatTl").GetDecimal());
    }

    [Fact]
    public async Task SNRK04_Has_karsiligi_dort_hane()
    {
        var k = YeniKiraci("snrk04");
        var u = Urun(k, "has");
        // 1000 / 6827,20 = 0,146472... → 0,1465 ; 6827,20 / 6827,20 = 1,0000 → toplam 1,1465
        Yaz(k, u, "SATIS", 1_000m, 6_827.20m);
        var s = Satir(await Sayfa(k, 50, 0), u);
        Assert.Equal(1.1465m, s.GetProperty("hasilatHas").GetDecimal());
    }

    [Fact]
    public async Task SNRK05_Cok_buyuk_tutar_tasmaz()
    {
        var k = YeniKiraci("snrk05");
        var u = Urun(k, "buyuk");
        Yaz(k, u, "SATIS", 999_999_999.99m, 999_999_999.99m);
        var s = Satir(await Sayfa(k, 50, 0), u);
        Assert.Equal(1_999_999_999.98m, s.GetProperty("hasilatTl").GetDecimal());
    }

    [Fact]
    public async Task SNRK06_Sayfa_siniri_kayipsiz_ortusmesiz_ve_bos_sayfa()
    {
        var k = YeniKiraci("snrk06");
        // Maliyet yok → tümü "hesaplanamadı" (karTl null, eşit) → sıra yalnız kararlı anahtarla belirlenir.
        var idler = new[] { Urun(k, "a"), Urun(k, "b"), Urun(k, "c"), Urun(k, "d"), Urun(k, "e") };
        foreach (var id in idler) Yaz(k, id, "SATIS", 100m);
        var gelen = new List<string>();
        decimal toplam = 0m;
        for (var o = 0; o < 5; o += 2)
        {
            var y = await Sayfa(k, 2, o);
            Assert.Equal(5, y.GetProperty("toplamUrunSayisi").GetInt32());
            Assert.Equal(o + 2 < 5, y.GetProperty("dahaFazlasiVar").GetBoolean());
            foreach (var s in y.GetProperty("satirlar").EnumerateArray())
            {
                gelen.Add(s.GetProperty("urunId").GetString()!);
                toplam += s.GetProperty("hasilatTl").GetDecimal();
            }
        }
        Assert.Equal(5, gelen.Distinct().Count());
        Assert.Equal(idler.OrderBy(x => x, StringComparer.Ordinal), gelen.OrderBy(x => x, StringComparer.Ordinal));
        Assert.Equal(500m, toplam);   // sayfalar toplamı = dönem toplamı (kayıp/çift 0)
        var uzak = await Sayfa(k, 2, 100_000);
        Assert.Empty(uzak.GetProperty("satirlar").EnumerateArray());
        Assert.False(uzak.GetProperty("dahaFazlasiVar").GetBoolean());
        Assert.Equal(5, uzak.GetProperty("toplamUrunSayisi").GetInt32());
    }

    [Theory]
    [InlineData(65, "65")]
    [InlineData(1234, "1.234")]
    [InlineData(0, "0")]
    public void SNRK07_Sinir_bandi_sayilari_tr_N0(int n, string beklenen)
        => Assert.Equal(beklenen, Kuyumcu.Web.Servisler.Bicim.Girdi(n, "N0"));
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI TUR 3 (Aşkın + Mehmet, 07.10 23:2x) — kendi sınır testlerim.
// Önceki iki turun kapsamadığı sınırlar: sayfa sınırının TAM kenarı (ofset = toplam−1,
// toplam), eşit tarihli fişlerde kararlı ikincil sıra (fiş no artan) sayfalar arasında,
// pasife alınmış ürünün adı (sözlük daraltması pasif ürünü düşürmemeli) ve tr-TR sayı
// girişi (FormAlani.SayiCoz: '45.000', '1,5', belirsiz '1.500' reddi). Beklenenler SABİT.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class OLCEKYUKTur3SinirTestleri(TestVeritabani vt)
{
    private static System.Text.Json.JsonElement J(object o) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o)).RootElement;

    private static DateOnly Bugun => DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));

    private Kiraci YeniKiraci(string ad)
    {
        using var kurulum = vt.OlusturSistem();
        return TestKurulum.KiraciKur(kurulum, $"PAKETOLCEK-YUK-TEST t3 {ad}");
    }

    private string Urun(Kiraci k, string ad)
    {
        using var db = vt.Olustur(k);
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();
        db.Urunler.Add(new Urun
        {
            Id = id, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"SNRD-{id[^10..]}", Ad = ad,
            AyarId = "K22", Milyem = 916m, GramAgirlik = 5m, TasAgirlik = 0m, HasGrami = 4.58m,
            IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    private void Satir(Kiraci k, string urunId, decimal tl, string fisId, DateTime tarih)
    {
        using var db = vt.Olustur(k);
        HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS",
            Tarih = tarih, TlTutari = tl, HasGrami = 0m, Adet = 1m, FisId = fisId,
            KurSnapshotId = k.KurId, CariId = k.CariId, UrunId = urunId,
            KullaniciId = k.KullaniciId, Olusturuldu = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    private async Task<System.Text.Json.JsonElement> Sayfa(Kiraci k, int limit, int ofset)
    {
        using var db = vt.Olustur(k);
        return J(await new Kuyumcu.Api.Modules.Maliyet.KarZararServisi(db).UrunBazli(k.Islem, Bugun, Bugun, null, limit, ofset));
    }

    [Fact]
    public async Task SNRD01_Ofset_tam_kenar_toplam_eksi_bir_ve_toplam()
    {
        var k = YeniKiraci("snrd01");
        var simdi = DateTime.UtcNow;
        foreach (var ad in new[] { "a", "b", "c" }) Satir(k, Urun(k, "SNRD01 " + ad), 100m, "SNRD01" + Kimlik.Ulid()[..8], simdi);

        var son = await Sayfa(k, 2, 2);                 // ofset = toplam − 1 → 1 satır, daha yok
        Assert.Single(son.GetProperty("satirlar").EnumerateArray());
        Assert.False(son.GetProperty("dahaFazlasiVar").GetBoolean());
        Assert.Equal(3, son.GetProperty("toplamUrunSayisi").GetInt32());

        var tam = await Sayfa(k, 2, 3);                 // ofset = toplam → boş, daha yok
        Assert.Empty(tam.GetProperty("satirlar").EnumerateArray());
        Assert.False(tam.GetProperty("dahaFazlasiVar").GetBoolean());

        var kenar = await Sayfa(k, 2, 1);               // 1 + 2 = 3 → tam bitiyor, daha YOK
        Assert.Equal(2, kenar.GetProperty("urunSayisi").GetInt32());
        Assert.False(kenar.GetProperty("dahaFazlasiVar").GetBoolean());

        var bir = await Sayfa(k, 1, 0);                 // limit 1 → daha VAR
        Assert.True(bir.GetProperty("dahaFazlasiVar").GetBoolean());
    }

    [Fact]
    public async Task SNRD02_Pasife_alinmis_urunun_adi_rapor_satirinda_kalir()
    {
        var k = YeniKiraci("snrd02");
        var id = Urun(k, "SNRD02 pasif yüzük");
        Satir(k, id, 2_500m, "SNRD02" + Kimlik.Ulid()[..8], DateTime.UtcNow);
        using (var db = vt.Olustur(k))
        {
            var u = db.Urunler.Single(x => x.Id == id);
            u.Aktif = false;
            db.SaveChanges();
        }
        var s = Assert.Single((await Sayfa(k, 50, 0)).GetProperty("satirlar").EnumerateArray());
        Assert.Equal(id, s.GetProperty("urunId").GetString());
        Assert.Equal("SNRD02 pasif yüzük", s.GetProperty("ad").GetString());
        Assert.Equal(2_500m, s.GetProperty("hasilatTl").GetDecimal());
    }

    [Fact]
    public async Task SNRD03_Esit_tarihli_fislerde_ikincil_sira_fis_no_artan_ve_sayfalar_ortusmez()
    {
        var k = YeniKiraci("snrd03");
        var t = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 9, 0, 0, DateTimeKind.Utc);
        // Kesim sırası alfabetik sıranın TERSİ: ikincil sıra kesim sırasından gelirse test kırmızı olur.
        Satir(k, k.UrunId, 10m, "SNRD03-C", t);
        Satir(k, k.UrunId, 20m, "SNRD03-B", t);
        Satir(k, k.UrunId, 30m, "SNRD03-A", t);

        var gelen = new List<string>();
        for (var o = 0; o < 3; o++)
        {
            using var db = vt.Olustur(k);
            var servis = new AramaServisi(db, new YetkiServisi(db, new MemoryCache(new MemoryCacheOptions())));
            var y = await servis.Ara("SNRD03-", "FIS", 1, o, null, k.Islem);
            gelen.Add(Assert.Single(y.Sonuclar).Kimlik);
            Assert.Equal(o < 2, y.DahaVar);
        }
        Assert.Equal(new[] { "SNRD03-A", "SNRD03-B", "SNRD03-C" }, gelen);
    }

    [Theory]
    [InlineData("45.000", "Tamam", "45000")]
    [InlineData("1,5", "Tamam", "1.5")]
    [InlineData("1.500", "Belirsiz", null)]
    [InlineData("", "Bos", null)]
    [InlineData("-0,005", "Tamam", "-0.005")]
    [InlineData("999.999.999,99", "Tamam", "999999999.99")]
    [InlineData("0,0001", "Tamam", "0.0001")]
    [InlineData("1,500.25", "Gecersiz", null)]
    public void SNRD04_Turkce_sayi_girisi(string ham, string sonuc, string? deger)
    {
        var c = Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc.ToString());
        Assert.Equal(deger is null ? null : decimal.Parse(deger, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }
}
