using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests.Dogrulayici;

// ════════════════════════════════════════════════════════════════════════
// PAKET RAPOR-GORUNUM — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT metin/sayıdır (üretim
// kodundan hesaplanmaz). Kapsam: paketin eklediği biçim yardımcıları
// (Bicim.HasIsaretli / Oran / GunSayisi / MadenMiktariTam) ve pos-fatura
// mutabakatının hurda has gramı (yalnız altın) — sıfır, eksi, yuvarlama sınırı,
// 4 hane has, çok büyük değer, boş/null, Türkçe giriş.
// ════════════════════════════════════════════════════════════════════════

public sealed class RAPORGORUNUMSinirSafTestleri
{
    // ── sıfır ──
    [Fact]
    public void RGS01_Sifir_has_isaretsiz() =>
        Assert.Equal("0,0000 has", Bicim.HasIsaretli(0m));

    // ── eksi: tipografik eksi, değer Bicim.Has ile aynı sayı dili ──
    [Fact]
    public void RGS02_Eksi_has_tipografik_eksi() =>
        Assert.Equal("−22,0000 has", Bicim.HasIsaretli(-22m));

    // ── eksi ama 4 haneye yuvarlanınca sıfır: "−0,0000 has" görünmemeli ──
    [Fact]
    public void RGS03_Yuvarlaninca_sifir_olan_eksi_has_isaret_tasimaz() =>
        Assert.Equal("0,0000 has", Bicim.HasIsaretli(-0.00004m));

    // ── has 4 hane: 0,0001 ve yarım-hane sınırı (0,00005 → 0,0001; 0,00004 → 0,0000) ──
    [Fact]
    public void RGS04_Has_dort_hane_sinirlari()
    {
        Assert.Equal("0,0001 has", Bicim.HasIsaretli(0.0001m));
        Assert.Equal("0,0001 has", Bicim.HasIsaretli(0.00005m));
        Assert.Equal("0,0000 has", Bicim.HasIsaretli(0.00004m));
        Assert.Equal("−0,0001 has", Bicim.HasIsaretli(-0.00005m));
    }

    // ── kuruş yuvarlaması (AwayFromZero, 0,005 sınırı) — oran 2 hane ──
    [Fact]
    public void RGS05_Oran_iki_hane_yarim_sinir()
    {
        Assert.Equal("0,01", Bicim.Oran(0.005m));
        Assert.Equal("0,00", Bicim.Oran(0.0049m));
        Assert.Equal("1,17", Bicim.Oran(1.165m));
        Assert.Equal("0,00", Bicim.Oran(0m));
    }

    // ── gün tam sayı: AwayFromZero (0,5 → 1; 2,5 → 3), binlik ──
    [Fact]
    public void RGS06_Gun_sayisi_tam_ve_binlik()
    {
        Assert.Equal("1", Bicim.GunSayisi(0.5m));
        Assert.Equal("3", Bicim.GunSayisi(2.5m));
        Assert.Equal("0", Bicim.GunSayisi(0m));
        Assert.Equal("1.204", Bicim.GunSayisi(1204.4m));
    }

    // ── çok büyük değer ──
    [Fact]
    public void RGS07_Cok_buyuk_degerler()
    {
        Assert.Equal("999.999.999,9900 has", Bicim.HasIsaretli(999_999_999.99m));
        Assert.Equal("999.999.999,99", Bicim.Oran(999_999_999.99m));
        Assert.Equal("999.999.999.999", Bicim.GunSayisi(999_999_999_999m));
    }

    // ── boş/null maden = altın; gümüş 0 stok aynı kalıp; gümüş fiziksel yok → saf kalıbı ──
    [Fact]
    public void RGS08_Maden_miktari_tam_bos_ve_sifir()
    {
        const string fs = "{0} g (saf {1} g)", s = "saf {0} g";
        Assert.Equal("1,2346 has", Bicim.MadenMiktariTam(null, 1.23456m, 9m, fs, s));
        Assert.Equal("1,2346 has", Bicim.MadenMiktariTam("", 1.23456m, null, fs, s));
        Assert.Equal("0,00 g (saf 0,00 g)", Bicim.MadenMiktariTam("GUMUS", 0m, 0m, fs, s));
        Assert.Equal("saf 11,56 g", Bicim.MadenMiktariTam("GUMUS", 11.5625m, null, fs, s));
        Assert.Equal("12,50 g (saf 11,56 g)", Bicim.MadenMiktariTam("GUMUS", 11.5625m, 12.5m, fs, s));
        Assert.Equal("999.999.999,99 g (saf 924.999.999,99 g)",
            Bicim.MadenMiktariTam("GUMUS", 924_999_999.99m, 999_999_999.99m, fs, s));
    }

    // ── Türkçe giriş (FormAlani.SayiCoz): '45.000' binlik, '1,5' ondalık, '1.500' belirsiz → red ──
    [Fact]
    public void RGS09_Turkce_giris()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
    }
}

[Collection("veritabani")]
public sealed class RAPORGORUNUMSinirDbTestleri(TestVeritabani vt)
{
    private static JsonElement J(object o) => JsonDocument.Parse(JsonSerializer.Serialize(o)).RootElement;

    private static Hareket Hurda(Kiraci k, decimal tl, decimal has, string? maden) => new()
    {
        Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "HURDA_ALIS",
        Tarih = DateTime.UtcNow, TlTutari = tl, HasGrami = has, Maden = maden ?? "",
        ParaBirimi = "TRY", KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId, Olusturuldu = DateTime.UtcNow,
    };

    private async Task<JsonElement> MutabakatHurda(Kiraci k)
    {
        using var db = vt.Olustur(k);
        var m = J(await TestKurulum.RaporServisi(db).PosFaturaMutabakati(
            Zaman.YerelBugun().ToString("yyyy-MM-dd"), k.Islem));
        return m.GetProperty("hurda");
    }

    // ── sıfır: hurda yok → has 0, dağılım boş ──
    [Fact]
    public async Task RGS20_Hurda_yokken_has_sifir_dagilim_bos()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-GORUNUM-TEST sinir20");
        await sistem.SaveChangesAsync();
        var h = await MutabakatHurda(k);
        Assert.Equal(0, h.GetProperty("sayisi").GetInt32());
        Assert.Equal(0m, h.GetProperty("hasGrami").GetDecimal());
        Assert.Equal(0m, h.GetProperty("altinHasGrami").GetDecimal());
        Assert.Empty(h.GetProperty("madenDagilimi").EnumerateArray());
    }

    // ── boş maden = altın; yalnız gümüş hurdada altın has 0 (gümüş gramı has'a SIZMAZ) ──
    [Fact]
    public async Task RGS21_Bos_maden_altin_sayilir_yalniz_gumuste_has_sifir()
    {
        var sistem = vt.OlusturSistem();
        var k1 = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-GORUNUM-TEST sinir21a");
        var k2 = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-GORUNUM-TEST sinir21b");
        sistem.Add(Hurda(k1, -1_000m, 0.0001m, ""));
        sistem.Add(Hurda(k1, -500m, 0.9160m, "ALTIN"));
        sistem.Add(Hurda(k2, -900m, 250m, "GUMUS"));
        await sistem.SaveChangesAsync();

        var h1 = await MutabakatHurda(k1);
        Assert.Equal(0.9161m, h1.GetProperty("hasGrami").GetDecimal());
        var d1 = h1.GetProperty("madenDagilimi").EnumerateArray().ToList();
        Assert.Single(d1);
        Assert.Equal(2, d1[0].GetProperty("satirSayisi").GetInt32());

        var h2 = await MutabakatHurda(k2);
        Assert.Equal(0m, h2.GetProperty("hasGrami").GetDecimal());
        Assert.Equal(250m, h2.GetProperty("madenDagilimi").EnumerateArray().Single().GetProperty("toplamHas").GetDecimal());
    }

    // ── çok büyük has ve 6 hane yuvarlama (AwayFromZero) ──
    [Fact]
    public async Task RGS22_Buyuk_has_toplami_ve_alti_hane()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-GORUNUM-TEST sinir22");
        sistem.Add(Hurda(k, -999_999_999.99m, 99_999.123456m, "ALTIN"));
        sistem.Add(Hurda(k, -1m, 0.000001m, "ALTIN"));
        sistem.Add(Hurda(k, -1m, 77_777.5m, "GUMUS"));
        await sistem.SaveChangesAsync();
        var h = await MutabakatHurda(k);
        Assert.Equal(99_999.123457m, h.GetProperty("hasGrami").GetDecimal());
        Assert.Equal(1_000_000_001.99m, h.GetProperty("odenenTl").GetDecimal());
    }
}

// ════════════════════════════════════════════════════════════════════════
// TUR 2 DOĞRULAYICI EKİ (03.10) — tur 1 sınırlarının görmediği açılar:
// bankacı/AwayFromZero ayrımı gerçekten ölçülür (x,xxxx5 / 0,025 / 11,25 gün),
// ASCII eksi sızmaz, eksi büyük değer, stok yaşı ortalaması 1 hane yuvarlama,
// tüketim yokken tahmin null (0 değil).
// ════════════════════════════════════════════════════════════════════════
public sealed class RAPORGORUNUMSinirTur2SafTestleri
{
    [Fact]
    public void RGS30_Yarim_hane_bankaci_degil_AwayFromZero()
    {
        Assert.Equal("1,2345 has", Bicim.HasIsaretli(1.23445m));     // bankacı 1,2344
        Assert.Equal("−1,2345 has", Bicim.HasIsaretli(-1.23445m));
        Assert.Equal("0,03", Bicim.Oran(0.025m));                     // bankacı 0,02
        Assert.Equal("−1", Bicim.GunSayisi(-0.5m).Replace("-", "−"));  // işaret korunur
    }

    [Fact]
    public void RGS31_Eksi_ascii_sizmaz_ve_buyuk_eksi()
    {
        Assert.DoesNotContain("-", Bicim.HasIsaretli(-2.5m), StringComparison.Ordinal);
        Assert.Equal("−999.999.999,9900 has", Bicim.HasIsaretli(-999_999_999.99m));
        Assert.Equal("−12,3000 fine", Bicim.HasIsaretli(-12.3m, "fine"));
        Assert.Equal("0,0000 fine", Bicim.HasIsaretli(0m, "fine"));
    }
}

[Collection("veritabani")]
public sealed class RAPORGORUNUMSinirTur2DbTestleri(TestVeritabani vt)
{
    private static JsonElement J(object o) => JsonDocument.Parse(JsonSerializer.Serialize(o)).RootElement;

    // ── stok yaşı ortalaması: (1×10 + 1×11 + 2×12) ÷ 4 = 11,25 → 1 hane AwayFromZero 11,3 (bankacı 11,2);
    //    tüketim yok → tahmini satış süresi null (0 değil) ──
    [Fact]
    public async Task RGS40_Ortalama_yas_bir_hane_AwayFromZero_tahmin_null()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-GORUNUM-TEST sinir40", stokAdet: 0m);
        var simdi = DateTime.UtcNow;
        StokPartisi P(int gun, decimal kalan) => new()
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, UrunId = k.UrunId,
            Kaynak = PartiKaynagi.Alis, AdetGiren = kalan, AdetKalan = kalan, HasGramBirim = 1m,
            BirimMaliyetTl = 1_000m, GirisTarihi = simdi.AddDays(-gun).AddHours(-1), Olusturuldu = simdi,
        };
        sistem.Add(P(10, 1m));
        sistem.Add(P(11, 1m));
        sistem.Add(P(12, 2m));
        await sistem.SaveChangesAsync();

        using var db = vt.Olustur(k);
        var o = J(await new Kuyumcu.Api.Modules.Rapor.StokYaslandirmaServisi(db, TestKurulum.Denetim(db))
            .Yaslandirma(k.Islem, null, devirPenceresiGun: 90));
        var d = o.GetProperty("devir");
        Assert.Equal(11.3m, d.GetProperty("ortalamaYasGun").GetDecimal());
        Assert.Equal(JsonValueKind.Null, d.GetProperty("tahminiSatisSuresiGun").ValueKind);
    }
}
