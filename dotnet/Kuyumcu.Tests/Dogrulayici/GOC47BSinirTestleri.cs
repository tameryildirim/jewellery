using System.Text;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Iceaktarim;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests.Dogrulayici;

// ═══════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET GOC-47B finansal sınır testleri.
// Beklenen değerler SABİT ve elle yazıldı (üretim kodundan türetilmez).
// Sözleşme (spesifikasyon + kaynak başlığı): CSV ondalık ayracı NOKTA; TL 2 hane,
// has 4 hane (karar 5: yuvarlanmaz, HATA); maliyet SIFIR yazılmaz; yuvarlama
// projede MidpointRounding.AwayFromZero.
// ═══════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class GOC47BSinirTestleri(TestVeritabani vt)
{
    private const string Gerekce = "GOC-47B dogrulayici sinir testi — yeterince uzun gerekce";

    private static readonly AcilisBakiyeKaynagi Bakiye = new();
    private static readonly AcilisStokKaynagi Stok = new();

    private static SatirSonucu B(string ad = "Ayşe", string tl = "", string has = "", string usd = "") =>
        Bakiye.Dogrula(new Dictionary<string, string>
        {
            ["ad"] = ad, ["tlBakiye"] = tl, ["hasGramBakiye"] = has, ["usdBakiye"] = usd,
        });

    private static SatirSonucu S(string adet, string maliyet = "") =>
        Stok.Dogrula(new Dictionary<string, string> { ["kod"] = "K1", ["adet"] = adet, ["birimMaliyetTl"] = maliyet });

    private static AcilisBakiyeSatiri Gecerli(SatirSonucu s) =>
        Assert.IsType<AcilisBakiyeSatiri>(s.Dto);

    // ── Sıfır ──────────────────────────────────────────────
    [Fact]
    public void Sifir_bakiye_satiri_SIFIR_SATIR_hatasi()
    {
        var s = B(tl: "0", has: "0.0000");
        Assert.Null(s.Dto);
        Assert.Equal("SIFIR_SATIR", s.HataKodu);
    }

    [Fact]
    public void Sifir_maliyet_MALIYET_SIFIR_hatasi_sifir_adet_ADET_GECERSIZ()
    {
        Assert.Equal("MALIYET_SIFIR", S("1", "0").HataKodu);
        Assert.Equal("ADET_GECERSIZ", S("0").HataKodu);
    }

    // ── Eksi ───────────────────────────────────────────────
    [Fact]
    public void Eksi_TL_bakiye_kabul_isaret_korunur_eksi_adet_ve_maliyet_red()
    {
        var d = Gecerli(B(tl: "-250.75", has: "-1.2500"));
        Assert.Equal(-250.75m, d.Tl);
        Assert.Equal(-1.25m, d.Has);
        Assert.Equal("ADET_GECERSIZ", S("-1").HataKodu);
        Assert.Equal("MALIYET_NEGATIF", S("1", "-5").HataKodu);
    }

    // ── Kuruş (0,005 sınırı) — yuvarlanmaz, reddedilir ─────
    [Fact]
    public void Kurus_alti_TL_yuvarlanmaz_TUTAR_ONDALIK()
    {
        Assert.Equal("TUTAR_ONDALIK", B(tl: "0.005").HataKodu);
        // GOC-47B tur 3 (orkestratör 14:20): '100.105' binlik ayracına benzer → SAYI_BELIRSIZ;
        // kuruş altı kuralı 3 haneli grup oluşturmayan yazımla sınanır.
        Assert.Equal("SAYI_BELIRSIZ", B(tl: "100.105").HataKodu);
        Assert.Equal("TUTAR_ONDALIK", B(tl: "1000.105").HataKodu);
        Assert.Equal("TUTAR_ONDALIK", B(tl: "100.1051").HataKodu);
        Assert.Equal(0.01m, Gecerli(B(tl: "0.01")).Tl);
    }

    [Fact]
    public void Maliyet_toplami_AwayFromZero_yuvarlanir_0_005_yukari()
    {
        var oz = Stok.Ozetle([new AcilisStokSatiri("K1", 1m, 0.0050m), new AcilisStokSatiri("K2", 1m, 0.0049m) { }]);
        // 0.0050 + 0.0049 = 0.0099 → 0.01
        Assert.Equal(0.01m, oz.ToplamMaliyetTl);
        var tek = Stok.Ozetle([new AcilisStokSatiri("K1", 1m, 0.0050m)]);
        Assert.Equal(0.01m, tek.ToplamMaliyetTl);   // AwayFromZero (ToEven olsaydı 0.00)
    }

    // ── Has/gram 4 hane ────────────────────────────────────
    [Fact]
    public void Has_0_0001_kabul_0_00005_HAS_ONDALIK()
    {
        Assert.Equal(0.0001m, Gecerli(B(has: "0.0001")).Has);
        Assert.Equal("HAS_ONDALIK", B(has: "0.00005").HataKodu);
    }

    // ── Çok büyük tutar ────────────────────────────────────
    [Fact]
    public void Cok_buyuk_TL_999999999_99_tam_kabul_tavan_ustu_red()
    {
        Assert.Equal(999_999_999.99m, Gecerli(B(tl: "999999999.99")).Tl);
        Assert.Equal("TUTAR_ASIRI", B(tl: "99999999999999").HataKodu);
    }

    // ── Boş / null ─────────────────────────────────────────
    [Fact]
    public void Bos_hucreler_null_sifir_sayilir_ad_bos_ALAN_ZORUNLU()
    {
        Assert.Equal("SIFIR_SATIR", B(tl: "", has: "  ").HataKodu);
        Assert.Equal("ALAN_ZORUNLU", B(ad: " ", tl: "5").HataKodu);
        Assert.Equal("ALAN_ZORUNLU",
            Stok.Dogrula(new Dictionary<string, string> { ["kod"] = "", ["adet"] = "1" }).HataKodu);
        var d = Assert.IsType<AcilisStokSatiri>(S("2", "").Dto);
        Assert.Null(d.BirimMaliyetTl);              // boş maliyet = NULL (sıfır değil)
    }

    // ── Türkçe giriş: sözleşme NOKTA ayracı ────────────────
    // '1,5' Türk kullanıcı için 1,5 gram demektir; NOKTA sözleşmesinde ya
    // reddedilmeli ya da 1.5 okunmalı — ASLA 15 olmamalı (10 kat sessiz hata).
    [Fact]
    public void Turkce_virgul_1_5_has_ON_KAT_okunmaz()
    {
        var s = B(has: "1,5");
        if (s.Dto is AcilisBakiyeSatiri d) Assert.Equal(1.5m, d.Has);
        else Assert.Equal("SAYI_GECERSIZ", s.HataKodu);
    }

    [Fact]
    public void Turkce_virgul_1500_50_TL_YUZ_KAT_okunmaz()
    {
        var s = B(tl: "1500,50");
        if (s.Dto is AcilisBakiyeSatiri d) Assert.Equal(1500.50m, d.Tl);
        else Assert.Equal("SAYI_GECERSIZ", s.HataKodu);
    }

    [Fact]
    public void Nokta_sozlesmesi_45_000_ve_1_500_BELIRSIZ_45_5_kirk_bes_bucuk_okunur()
    {
        // GOC-47B tur 3 (orkestratör 14:20 ilkesi: belirsiz sayı sessizce yorumlanmaz):
        // '45.000' / '1.500' artık 45 / 1,5 OKUNMAZ → SAYI_BELIRSIZ. Nokta ondalık sözleşmesi sürer.
        Assert.Equal("SAYI_BELIRSIZ", B(tl: "45.000").HataKodu);
        Assert.Equal("SAYI_BELIRSIZ", B(has: "1.500").HataKodu);
        Assert.Equal(45.5m, Gecerli(B(tl: "45.5")).Tl);
        Assert.Equal(1.5m, Gecerli(B(has: "1.5")).Has);
    }

    // ── Uçtan uca: büyük tutar + has 4 hane + maliyet kuruşu deftere birebir ──
    [Fact]
    public async Task Uctan_uca_999999999_99_TL_ve_0_0001_has_bakiyeye_birebir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST dg buyuk");
        using var db = vt.Olustur(k.TenantId);
        var cid = Kimlik.Ulid(); var now = DateTime.UtcNow;
        db.Cariler.Add(new Cari { Id = cid, TenantId = k.TenantId, Tur = "MUSTERI", Ad = "Sınır Müşteri",
            Telefon = "05329990011", Aktif = true, Olusturuldu = now, Guncellendi = now });
        db.CariBakiyeler.Add(new CariBakiye { CariId = cid, TenantId = k.TenantId, Guncellendi = now });
        await db.SaveChangesAsync();

        var denetim = TestKurulum.Denetim(db);
        var cari = new Kuyumcu.Api.Modules.Cari.CariServisi(db, NullLogger<Kuyumcu.Api.Modules.Cari.CariServisi>.Instance, denetim);
        var urun = new Kuyumcu.Api.Modules.Urun.UrunServisi(db, denetim);
        var kurulum = new Kuyumcu.Api.Modules.Kurulum.KurulumServisi(db, denetim,
            NullLogger<Kuyumcu.Api.Modules.Kurulum.KurulumServisi>.Instance, new ConfigurationBuilder().Build());
        var s = new IceAktarimServisi(db, denetim, cari, urun, null, kurulum);

        var csv = "ad,telefon,tlBakiye,hasGramBakiye\nSınır Müşteri,05329990011,999999999.99,0.0001\n";
        var p = await s.Prova(new IceAktarimProvaDto
        {
            Kaynak = "acilis-bakiye", DosyaAdi = "b.csv", Gerekce = Gerekce,
            IcerikBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csv)),
        }, k.Islem);
        var oturumId = JsonDocument.Parse(JsonSerializer.Serialize(p)).RootElement.GetProperty("oturumId").GetString()!;
        await s.Uygula(oturumId, new IceAktarimUygulaDto { Onay = s.Ayarlar.UygulaOnayMetni }, k.Islem);

        using var oku = vt.Olustur(k.TenantId);
        var b = await oku.CariBakiyeler.AsNoTracking().SingleAsync(x => x.CariId == cid);
        Assert.Equal(999_999_999.99m, b.TlBakiye);
        Assert.Equal(0.0001m, b.HasGramBakiye);
    }
}

// ═══════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI TUR 2 (Aşkın + Mehmet, 2026-10-01) — ek sınırlar.
// Beklenen değerler SABİT ve elle yazıldı. Görev koşulu: Türkçe girişte
// belirsiz '1.500' / '45.000' REDDEDİLİR. TL ve döviz en çok 2 ondalık taşır;
// noktadan sonra TAM 3 rakam (sondaki sıfırlar dahil) ancak Türkçe BİNLİK
// ayracı olabilir → sessizce 45 TL okunması 1000 kat para hatasıdır.
// ═══════════════════════════════════════════════════════════
public sealed class GOC47BSinirTestleriTur2
{
    private static readonly AcilisBakiyeKaynagi Bakiye = new();
    private static readonly AcilisStokKaynagi Stok = new();

    private static SatirSonucu B(string tl = "", string has = "", string usd = "") =>
        Bakiye.Dogrula(new Dictionary<string, string>
        {
            ["ad"] = "Ayşe", ["tlBakiye"] = tl, ["hasGramBakiye"] = has, ["usdBakiye"] = usd,
        });

    private static SatirSonucu S(string adet, string maliyet = "") =>
        Stok.Dogrula(new Dictionary<string, string> { ["kod"] = "K1", ["adet"] = adet, ["birimMaliyetTl"] = maliyet });

    [Fact]
    public void Dg2_TL_45_000_binlik_belirsiz_REDDEDILIR_45_TL_okunmaz()
    {
        var s = B(tl: "45.000");
        Assert.Null(s.Dto);
        Assert.Equal("SAYI_BELIRSIZ", s.HataKodu);   // GOC-47B tur 3: kod orkestratör kararıyla
    }

    [Fact]
    public void Dg2_TL_1_500_binlik_belirsiz_REDDEDILIR()
    {
        Assert.Null(B(tl: "1.500").Dto);
    }

    [Fact]
    public void Dg2_USD_2_500_binlik_belirsiz_REDDEDILIR()
    {
        Assert.Null(B(usd: "2.500").Dto);
    }

    [Fact]
    public void Dg2_Turk_bicimi_1_500_000_ve_1_500_50_REDDEDILIR()
    {
        Assert.Equal("SAYI_BELIRSIZ", B(tl: "1.500.000").HataKodu);   // GOC-47B tur 3
        Assert.Equal("SAYI_GECERSIZ", B(tl: "1.500,50").HataKodu);
        Assert.Equal("SAYI_GECERSIZ", B(tl: "45 000").HataKodu);
    }

    [Fact]
    public void Dg2_eksi_buyuk_TL_kabul_isaret_korunur()
    {
        var d = Assert.IsType<AcilisBakiyeSatiri>(B(tl: "-999999999.99").Dto);
        Assert.Equal(-999_999_999.99m, d.Tl);
    }

    [Fact]
    public void Dg2_TL_tavani_9999999999999_kabul_bir_kurus_ustu_TUTAR_ASIRI()
    {
        Assert.Equal(9_999_999_999_999m, Assert.IsType<AcilisBakiyeSatiri>(B(tl: "9999999999999").Dto).Tl);
        Assert.Equal("TUTAR_ASIRI", B(tl: "9999999999999.01").HataKodu);
    }

    [Fact]
    public void Dg2_adet_0_001_kabul_0_0001_red_maliyet_0_0001_kabul_0_00001_red()
    {
        Assert.Equal(0.001m, Assert.IsType<AcilisStokSatiri>(S("0.001").Dto).Adet);
        Assert.Equal("ADET_GECERSIZ", S("0.0001").HataKodu);
        Assert.Equal(0.0001m, Assert.IsType<AcilisStokSatiri>(S("1", "0.0001").Dto).BirimMaliyetTl);
        Assert.Equal("TUTAR_ONDALIK", S("1", "0.00001").HataKodu);
    }

    [Fact]
    public void Dg2_has_0_00005_yuvarlanip_0_0001_olmaz_eksi_de_ayni()
    {
        Assert.Equal("HAS_ONDALIK", B(has: "-0.00005").HataKodu);
        Assert.Equal(-0.0001m, Assert.IsType<AcilisBakiyeSatiri>(B(has: "-0.0001").Dto).Has);
    }
}

// ═══════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI TUR 3 (Aşkın + Mehmet, 2026-10-01 kör inceleme) — ek sınırlar.
// Beklenen değerler SABİT, elle yazıldı; üretim kodundan TÜRETİLMEZ.
// Kapsam: döviz bacağında kuruş/belirsiz sayı, adet/has'ta belirsiz nokta,
// boşluk/işaret/ABD binlik ayracı, uçtan uca maliyet kuruşu (AwayFromZero),
// kesirli adet × has, ve büyük tutarın geri almada tam SIFIRA dönmesi.
// ═══════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class GOC47BSinirTestleriTur3(TestVeritabani vt)
{
    private const string Gerekce = "GOC-47B dogrulayici tur 3 sinir testi — yeterince uzun gerekce";
    private static readonly AcilisBakiyeKaynagi Bakiye = new();
    private static readonly AcilisStokKaynagi Stok = new();

    private static SatirSonucu B(string tl = "", string has = "", string usd = "", string eur = "") =>
        Bakiye.Dogrula(new Dictionary<string, string>
        {
            ["ad"] = "Ayşe", ["tlBakiye"] = tl, ["hasGramBakiye"] = has, ["usdBakiye"] = usd, ["eurBakiye"] = eur,
        });

    private static SatirSonucu S(string adet, string maliyet = "") =>
        Stok.Dogrula(new Dictionary<string, string> { ["kod"] = "K1", ["adet"] = adet, ["birimMaliyetTl"] = maliyet });

    [Fact]
    public void Dg3_doviz_kurus_alti_0_005_TUTAR_ONDALIK_0_01_kabul()
    {
        Assert.Equal("TUTAR_ONDALIK", B(usd: "0.005").HataKodu);
        Assert.Equal("TUTAR_ONDALIK", B(tl: "0.005").HataKodu);
        var d = Assert.IsType<AcilisBakiyeSatiri>(B(usd: "0.01").Dto);
        Assert.Equal(0.01m, Assert.Single(d.Dovizler).Tutar);
        Assert.Equal("USD", d.Dovizler[0].ParaBirimi);
    }

    [Fact]
    public void Dg3_belirsiz_nokta_eur_adet_eksi_TL_hepsi_SAYI_BELIRSIZ()
    {
        Assert.Equal("SAYI_BELIRSIZ", B(eur: "1.250").HataKodu);
        Assert.Equal("SAYI_BELIRSIZ", B(tl: "-1.500").HataKodu);
        Assert.Equal("SAYI_BELIRSIZ", S("1.000").HataKodu);
        Assert.Equal("SAYI_BELIRSIZ", S("1", "2.500").HataKodu);
    }

    [Fact]
    public void Dg3_bosluk_ve_arti_isaret_okunur_ABD_binlik_ve_unicode_eksi_red()
    {
        Assert.Equal(1500.50m, Assert.IsType<AcilisBakiyeSatiri>(B(tl: "  1500.50 ").Dto).Tl);
        Assert.Equal(12.5m, Assert.IsType<AcilisBakiyeSatiri>(B(has: "+12.5").Dto).Has);
        Assert.Equal("SAYI_GECERSIZ", B(tl: "1,000").HataKodu);        // ABD binlik → 1000 okunmaz
        Assert.Equal("SAYI_GECERSIZ", B(tl: "1,000.50").HataKodu);
        Assert.Equal("SAYI_GECERSIZ", B(tl: "−250").HataKodu);     // Unicode eksi
        Assert.Equal(0.5m, Assert.IsType<AcilisBakiyeSatiri>(B(tl: "0.500").Dto).Tl);  // 0 ile başlar → belirsiz değil
    }

    [Fact]
    public void Dg3_has_tavani_bir_gram_ustu_TUTAR_ASIRI_doviz_tavani()
    {
        Assert.Equal("TUTAR_ASIRI", B(has: "1000000000").HataKodu);
        Assert.Equal("TUTAR_ASIRI", B(usd: "10000000000000").HataKodu);
        Assert.Equal(-999_999_999.9999m, Assert.IsType<AcilisBakiyeSatiri>(B(has: "-999999999.9999").Dto).Has);
    }

    // Faz B açılış stoku has değerini ürün kartının HasGrami alanından alır (DefterKaynaklari:
    // HasGramBirim = u.HasGrami). Kartın gramı Faz A dosyasından gelir; '1,5' gram 15 okunursa
    // açılış stoku deftere (SAYIM_DUZELTME has_grami) 10 KAT has yazar. Görev koşulu: '1,5'
    // ya reddedilir ya 1,5 okunur — ASLA 15. Satış fiyatı '1500,50' → 150050 de aynı sınıf.
    [Fact]
    public void Dg3_FazA_urun_karti_gram_1_virgul_5_ON_KAT_okunmaz_fiyat_YUZ_KAT_okunmaz()
    {
        var kaynak = new UrunKaynagi();
        SatirSonucu U(string gram, string fiyat = "") => kaynak.Dogrula(new Dictionary<string, string>
        {
            ["tip"] = "ZIYNET", ["kod"] = "DG3-V1", ["ad"] = "Dg3 bilezik", ["ayarId"] = "K22",
            ["gramAgirlik"] = gram, ["satisFiyati"] = fiyat,
        });
        var g = U("1,5");
        if (g.Dto is Kuyumcu.Api.Modules.Urun.UrunOlusturDto d) Assert.Equal(1.5m, d.GramAgirlik);
        else Assert.Equal("SAYI_GECERSIZ", g.HataKodu);
        var f = U("12.5", "1500,50");
        if (f.Dto is Kuyumcu.Api.Modules.Urun.UrunOlusturDto d2) Assert.Equal(1500.50m, d2.SatisFiyati);
        else Assert.Equal("SAYI_GECERSIZ", f.HataKodu);
    }

    private static IceAktarimServisi Servis(KuyumcuDbContext db)
    {
        var denetim = TestKurulum.Denetim(db);
        var cari = new Kuyumcu.Api.Modules.Cari.CariServisi(db, NullLogger<Kuyumcu.Api.Modules.Cari.CariServisi>.Instance, denetim);
        var urun = new Kuyumcu.Api.Modules.Urun.UrunServisi(db, denetim);
        var kurulum = new Kuyumcu.Api.Modules.Kurulum.KurulumServisi(db, denetim,
            NullLogger<Kuyumcu.Api.Modules.Kurulum.KurulumServisi>.Instance, new ConfigurationBuilder().Build());
        return new IceAktarimServisi(db, denetim, cari, urun, null, kurulum);
    }

    private static IceAktarimProvaDto Dto(string kaynak, string csv) => new()
    {
        Kaynak = kaynak, DosyaAdi = $"{kaynak}-dg3.csv", Gerekce = Gerekce,
        IcerikBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csv)),
    };

    private static string OturumId(object p) =>
        JsonDocument.Parse(JsonSerializer.Serialize(p)).RootElement.GetProperty("oturumId").GetString()!;

    [Fact]
    public async Task Dg3_uctan_uca_maliyet_kurusu_AwayFromZero_kesirli_adet_has_6_hane()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST dg3 stok");
        using var db = vt.Olustur(k.TenantId);
        var now = DateTime.UtcNow; var uid = Kimlik.Ulid();
        db.Urunler.Add(new Urun
        {
            Id = uid, TenantId = k.TenantId, Tip = "ZIYNET", Kod = "DG3-K1", Ad = "Dg3 ürün", AyarId = "K22",
            Milyem = 0.916m, GramAgirlik = 1.3477m, TasAgirlik = 0m, HasGrami = 1.2345m,
            IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        await db.SaveChangesAsync();
        // GOC-C (#6 · K14): kesirli açılış adedi yalnız GRAM birimli üründe geçerlidir (ADET birimde
        // satır hatası) — kuruş/has ölçüsü aynen GRAM birimli üründe korunur.
        TestKurulum.UrunuGramBirimiYap(db, uid);

        var s = Servis(db);
        // 0.0020 × 2.5 = 0.005 → AwayFromZero 0.01 (ToEven 0.00 olurdu); adet 2.5 → has 2.5 × 1.2345 = 3.08625
        var p = await s.Prova(Dto("acilis-stok", "kod,adet,birimMaliyetTl\nDG3-K1,2.5,0.0020\n"), k.Islem);
        await s.Uygula(OturumId(p), new IceAktarimUygulaDto { Onay = s.Ayarlar.UygulaOnayMetni }, k.Islem);

        using var oku = vt.Olustur(k.TenantId);
        var kalem = await oku.AcilisKalemleri.AsNoTracking().SingleAsync(x => x.HedefId == uid);
        Assert.Equal(0.01m, kalem.TlTutar);          // 0.0020 × 2.5 = 0.005 → 0.01 (AwayFromZero)
        Assert.Equal(3.08625m, kalem.HasGram);
        var parti = await oku.Set<StokPartisi>().AsNoTracking().SingleAsync(x => x.UrunId == uid);
        Assert.Equal(0.0020m, parti.BirimMaliyetTl);
        Assert.Equal(2.5m, parti.AdetGiren);
        var h = await oku.Hareketler.AsNoTracking().SingleAsync(x => x.UrunId == uid);
        Assert.Equal(3.08625m, h.HasGrami);
        Assert.Equal(0m, h.TlTutari);
        var stok = await oku.Stoklar.AsNoTracking().SingleAsync(x => x.UrunId == uid && x.SubeId == k.SubeId);
        Assert.Equal(2.5m, stok.Adet);
    }

    [Fact]
    public async Task Dg3_buyuk_TL_has_ve_doviz_geri_alinca_TAM_SIFIR_iz_kalir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-47B-TEST dg3 geri");
        using var db = vt.Olustur(k.TenantId);
        var cid = Kimlik.Ulid(); var now = DateTime.UtcNow;
        db.Cariler.Add(new Cari { Id = cid, TenantId = k.TenantId, Tur = "MUSTERI", Ad = "Dg3 Müşteri",
            Telefon = "05329990033", Aktif = true, Olusturuldu = now, Guncellendi = now });
        db.CariBakiyeler.Add(new CariBakiye { CariId = cid, TenantId = k.TenantId, Guncellendi = now });
        await db.SaveChangesAsync();

        var s = Servis(db);
        var csv = "ad,telefon,tlBakiye,hasGramBakiye,usdBakiye\nDg3 Müşteri,05329990033,-999999999.99,0.0001,-0.01\n";
        var p = await s.Prova(Dto("acilis-bakiye", csv), k.Islem);
        var oid = OturumId(p);
        await s.Uygula(oid, new IceAktarimUygulaDto { Onay = s.Ayarlar.UygulaOnayMetni }, k.Islem);

        using (var ara = vt.Olustur(k.TenantId))
        {
            var b = await ara.CariBakiyeler.AsNoTracking().SingleAsync(x => x.CariId == cid);
            Assert.Equal(-999_999_999.99m, b.TlBakiye);
            Assert.Equal(0.0001m, b.HasGramBakiye);
            Assert.Equal(-0.01m, b.UsdBakiye);
        }

        await s.GeriAl(oid, k.Islem);

        using var oku = vt.Olustur(k.TenantId);
        var son = await oku.CariBakiyeler.AsNoTracking().SingleAsync(x => x.CariId == cid);
        Assert.Equal(0m, son.TlBakiye);
        Assert.Equal(0m, son.HasGramBakiye);
        Assert.Equal(0m, son.UsdBakiye);
        Assert.Equal(4, await oku.Hareketler.CountAsync(x => x.CariId == cid));   // 2 açılış + 2 ters — silme yok
        var kalem = await oku.AcilisKalemleri.AsNoTracking().SingleAsync(x => x.HedefId == cid);
        Assert.Equal(AcilisKalemDurumu.GeriAlindi, kalem.Durum);
    }
}
