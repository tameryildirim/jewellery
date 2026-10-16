using System.Text;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Iceaktarim;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MKurulum = Kuyumcu.Api.Modules.Kurulum;
using MSatis = Kuyumcu.Api.Modules.Satis;
using MSiparis = Kuyumcu.Api.Modules.Siparis;
using MUrun = Kuyumcu.Api.Modules.Urun;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET GOC-C — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİTTİR (üretim kodundan
// hesaplanmaz). Sınırlar: sıfır · eksi · 0,005 kuruş (AwayFromZero) · has/gram
// 4 hane · 999.999.999,99 · boş/null · tr-TR giriş (FormAlani.SayiCoz).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GOCCSinirTestleri
{
    private static void Kart(string birim = "ADET", decimal gram = 5m, decimal tas = 0m, decimal? satis = null,
                             decimal? alis = null, decimal? kons = null, decimal? isc = null, decimal? min = null,
                             string? barkod = null) =>
        MUrun.UrunServisi.GocCKapisi(birim, gram, tas, satis, alis, kons, isc, min, barkod);

    private static MKurulum.AcilisSatirDto S(decimal tl, decimal has) => new() { HedefId = "x", TlTutar = tl, HasGram = has };

    // ── Sıfır ──────────────────────────────────────────────────────────────
    [Fact]
    public void Sifir_adet_red_sifir_fiyat_serbest_sifir_iskonto_tutari_sifir()
    {
        Assert.Throws<BadRequestException>(() => MUrun.UrunAdetKapisi.Dogrula("ADET", 0m, "adet"));
        MUrun.UrunAdetKapisi.SifirOlabilirDogrula("ADET", 0m, "sayilanAdet");   // sayılan 0 geçerli
        Kart(satis: 0m, alis: 0m, isc: 0m, kons: 0m);                           // K9a
        Assert.Equal(0m, MSatis.IskontoHesabi.Tutar(0m, 10m));
        Assert.Equal(0m, MSatis.IskontoHesabi.Tutar(1000m, 0m));
        Assert.Throws<BadRequestException>(() => Kuyumcu.Api.Common.Dogrulama.GirdiKapisi.Gram(0m, "gramAgirlik"));
    }

    // ── Eksi ───────────────────────────────────────────────────────────────
    [Fact]
    public void Eksi_tutar_adet_yuzde_red_acilis_isaretli_kabul()
    {
        Assert.Throws<BadRequestException>(() => Kart(alis: -0.01m));
        Assert.Throws<BadRequestException>(() => Kart(satis: -0.01m));
        Assert.Throws<BadRequestException>(() => MUrun.UrunAdetKapisi.Dogrula("ADET", -1m, "adet"));
        Assert.Throws<BadRequestException>(() => MUrun.UrunAdetKapisi.SifirOlabilirDogrula("ADET", -1m, "sayilanAdet"));
        Assert.Throws<BadRequestException>(() => MSatis.IskontoHesabi.YuzdeDogrula(-0.01m, "Fiş iskontosu"));
        Assert.Throws<BadRequestException>(() => Kart(min: -1m));
        MKurulum.AcilisKaynaklari.OndalikKapisi([S(-250.75m, -1.2500m)]);    // K9b/K11 işaretli: geçer
    }

    // ── Kuruş 0,005 (AwayFromZero) ─────────────────────────────────────────
    [Fact]
    public void Kurus_yarim_AwayFromZero_ve_uc_ondalik_tutar_red()
    {
        Assert.Equal(0.01m, MSatis.IskontoHesabi.Tutar(0.05m, 10m));   // 0,005 → 0,01
        Assert.Equal(0.03m, MSatis.IskontoHesabi.Tutar(0.25m, 10m));   // 0,025 → 0,03 (ToEven 0,02 verirdi)
        var pay = MSatis.IskontoHesabi.Dagit([0.01m, 0.01m, 0.01m], 0.02m);
        Assert.Equal(0.02m, pay.Sum());                                 // Σ pay = fiş iskontosu TAM
        Assert.Throws<BadRequestException>(() => Kart(satis: 0.005m));   // 3 ondalık sessiz yuvarlanmaz
        Assert.Throws<BadRequestException>(() => MKurulum.AcilisKaynaklari.OndalikKapisi([S(0.005m, 0m)]));
        MKurulum.AcilisKaynaklari.OndalikKapisi([S(0.01m, 0m)]);
        Assert.Throws<BadRequestException>(() => MSatis.IskontoHesabi.YuzdeDogrula(10.005m, "Fiş iskontosu"));
    }

    // ── Has / gram 4 hane ──────────────────────────────────────────────────
    [Fact]
    public void Has_ve_gram_dort_hane_sinirlari()
    {
        Kart(gram: 5.0000m, tas: 4.9999m);                                         // 0,0001 altı geçer
        Assert.Throws<BadRequestException>(() => Kart(gram: 5.0000m, tas: 5.0000m)); // eşit: has 0
        Assert.Throws<BadRequestException>(() => Kart(gram: 4.9999m, tas: 5.0000m)); // 0,0001 fazla
        MKurulum.AcilisKaynaklari.OndalikKapisi([S(0m, 0.0001m)]);
        Assert.Throws<BadRequestException>(() => MKurulum.AcilisKaynaklari.OndalikKapisi([S(0m, 1.0000001m)]));
        // K10: gram en çok 3 ondalık → sipariş kalem gramı 0,0001 reddedilir, 0,001 geçer.
        Assert.Throws<BadRequestException>(() => Kuyumcu.Api.Common.Dogrulama.GirdiKapisi.Gram(0.0001m, "gramAgirlik"));
        Assert.Equal(0.001m, Kuyumcu.Api.Common.Dogrulama.GirdiKapisi.Gram(0.001m, "gramAgirlik"));
    }

    // ── Çok büyük tutar ────────────────────────────────────────────────────
    [Fact]
    public void Cok_buyuk_tutar_999_999_999_99()
    {
        Kart(satis: 999_999_999.99m, alis: 999_999_999.99m);
        Assert.Equal(999_999_999.99m, MSatis.IskontoHesabi.Tutar(999_999_999.99m, 100m));
        Assert.Equal(100_000_000.00m, MSatis.IskontoHesabi.Tutar(999_999_999.99m, 10m)); // 99.999.999,999 → 100.000.000,00
        var pay = MSatis.IskontoHesabi.Dagit([999_999_999.99m, 0.01m], 100m);
        Assert.Equal(100m, pay.Sum());
        MKurulum.AcilisKaynaklari.OndalikKapisi([S(-999_999_999.99m, 999_999_999.999999m)]);
        Assert.Throws<BadRequestException>(() => MKurulum.AcilisKaynaklari.OndalikKapisi([S(0m, 1_000_000_000m)]));
        Assert.Throws<BadRequestException>(() => MUrun.UrunAdetKapisi.Dogrula("ADET", 1_000_000_000m, "adet"));
    }

    // ── Boş / null ─────────────────────────────────────────────────────────
    [Fact]
    public void Bos_null_kapilari_sessizce_atlar_deger_uydurmaz()
    {
        Kart();
        Assert.Null(MSiparis.SiparisServisi.SozGunuDogrula(null));
        MSatis.IskontoHesabi.YuzdeDogrula(null, "Fiş iskontosu");
        MUrun.UrunAdetKapisi.SifirOlabilirDogrula("ADET", null, "minStok");
        Assert.Equal(0m, MSatis.IskontoHesabi.Tutar(100m, null));
        Kart(barkod: "   ");                                              // boş barkod = barkodsuz ürün
    }

    // ── Türkçe giriş (FormAlani.SayiCoz) → Core kuralı ─────────────────────
    [Fact]
    public void Turkce_giris_45_000_ve_1_5_ve_belirsiz_1_500()
    {
        var a = FormAlani.SayiCoz("45.000");
        Assert.Equal(FormAlani.SayiSonucu.Tamam, a.Sonuc);
        Assert.Equal(45000m, a.Deger);
        var b = FormAlani.SayiCoz("1,5");
        Assert.Equal(1.5m, b.Deger);
        Assert.Equal(GirdiKodu.SayiTam, AdetKurali.Denetle(b.Deger)!.Value.Kod);   // 1,5 adet yazılamaz
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Null(AdetKurali.Denetle(FormAlani.SayiCoz("45.000").Deger)); // 45000 tam sayı
    }
}

// ── Veri içe aktarım açılış stoğu: ADET birimli üründe kesirli adet (#6 · K14) ──
[Collection("veritabani")]
public sealed class GOCCSinirVtTestleri(TestVeritabani vt)
{
    private const string Gerekce = "GOC-C dogrulayici sinir testi — yeterince uzun gerekce";

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
        Kaynak = kaynak, DosyaAdi = $"{kaynak}-gocc.csv", Gerekce = Gerekce,
        IcerikBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csv)),
    };

    [Fact]   // GOC-C tur 2: hasım bulgusu kapandı (AcilisStokKaynagi birim okur + StokYaz son savunma) — Skip kaldırıldı.
    public async Task Ice_aktarim_acilis_stogu_ADET_birimli_urune_kesirli_adet_YAZILMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-C-TEST dg acilis");
        using var db = vt.Olustur(k.TenantId);
        var now = DateTime.UtcNow; var uid = Kimlik.Ulid();
        db.Urunler.Add(new Urun
        {
            Id = uid, TenantId = k.TenantId, Tip = "ZIYNET", Kod = "GOCC-K1", Ad = "GocC ürün", AyarId = "K22",
            Milyem = 0.916m, GramAgirlik = 2m, TasAgirlik = 0m, HasGrami = 1.832m, Birim = "ADET",
            IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        await db.SaveChangesAsync();

        var s = Servis(db);
        try
        {
            var p = await s.Prova(Dto("acilis-stok", "kod,adet,birimMaliyetTl\nGOCC-K1,1.5,100\n"), k.Islem);
            var oturum = JsonDocument.Parse(JsonSerializer.Serialize(p)).RootElement.GetProperty("oturumId").GetString()!;
            await s.Uygula(oturum, new IceAktarimUygulaDto { Onay = s.Ayarlar.UygulaOnayMetni }, k.Islem);
        }
        catch (ApiException) { /* prova ya da uygula reddi kabul — aşağıdaki ölçüm belirleyici */ }

        using var oku = vt.Olustur(k.TenantId);
        var stok = await oku.Stoklar.AsNoTracking().Where(x => x.UrunId == uid).Select(x => x.Adet).ToListAsync();
        Assert.DoesNotContain(stok, a => a != decimal.Truncate(a));   // ADET birimli üründe kesirli stok YOK
    }
}

// ── GOC-C TUR 2 · hasım doğrulayıcı (09.10) — açılış stoğu birim kapısının sınırları ──
// Beklenen değerler SABİTTİR: ADET birimde 0/eksi/kesir/tavan+1 satır hatası; GRAM birimde 3 ondalık serbest,
// 4. ondalık hata; "3.000" belirsiz binlik yazımı SAYI_BELIRSIZ ile reddedilir. Birimi boş (null) ürün ADET gibi davranır.
public sealed class GOCCSinirTur2SafTestleri
{
    [Fact]
    public void Adet_kapisi_birim_sinirlari_null_birim_ADET_sayilir()
    {
        var ex = Assert.Throws<BadRequestException>(() => MUrun.UrunAdetKapisi.Dogrula(null, 1.5m, "adet"));
        Assert.Equal("Küsuratlı yazılamaz; tam sayı girin.", ex.Message);
        Assert.Throws<BadRequestException>(() => MUrun.UrunAdetKapisi.Dogrula("ADET", 0.999m, "adet"));
        Assert.Throws<BadRequestException>(() => MUrun.UrunAdetKapisi.Dogrula("ADET", 1_000_000_000m, "adet"));
        MUrun.UrunAdetKapisi.Dogrula("ADET", 999_999_999m, "adet");   // tavan tam sınır geçer
        MUrun.UrunAdetKapisi.Dogrula("ADET", 3.000m, "adet");         // tam sayı yazımı geçer
        MUrun.UrunAdetKapisi.Dogrula("GRAM", 1.5m, "adet");           // gram birimde kesir serbest
        MUrun.UrunAdetKapisi.Dogrula("GRAM", 0.001m, "adet");
    }
}

[Collection("veritabani")]
public sealed class GOCCSinirTur2VtTestleri(TestVeritabani vt)
{
    [Fact]
    public async Task Acilis_stogu_provasi_sifir_eksi_kesir_tavan_ve_gram_dorduncu_ondalik_satir_hatasi()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-C-TEST dg t2 sinir");
        using var db = vt.Olustur(k.TenantId);
        var now = DateTime.UtcNow;
        foreach (var (kod, birim) in new[] { ("S-A1", "ADET"), ("S-A2", "ADET"), ("S-A3", "ADET"), ("S-A4", "ADET"),
                                             ("S-A5", "ADET"), ("S-G1", "GRAM"), ("S-G2", "GRAM"), ("S-A6", "ADET") })
            db.Urunler.Add(new Urun
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, Tip = "ZIYNET", Kod = kod, Ad = "Sınır " + kod, AyarId = "K22",
                Milyem = 0.916m, GramAgirlik = 2m, TasAgirlik = 0m, HasGrami = 1.832m, Birim = birim,
                IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
            });
        await db.SaveChangesAsync();

        var denetim = TestKurulum.Denetim(db);
        var cari = new Kuyumcu.Api.Modules.Cari.CariServisi(db, NullLogger<Kuyumcu.Api.Modules.Cari.CariServisi>.Instance, denetim);
        var s = new IceAktarimServisi(db, denetim, cari, new MUrun.UrunServisi(db, denetim), null,
            new MKurulum.KurulumServisi(db, denetim, NullLogger<MKurulum.KurulumServisi>.Instance, new ConfigurationBuilder().Build()));
        const string csv = "kod,adet,birimMaliyetTl\nS-A1,0,100\nS-A2,-1,100\nS-A3,1000000,100\nS-A4,1000001,100\n"
                         + "S-A5,3.000,100\nS-G1,0.001,100\nS-G2,1.0005,100\nS-A6,0.5,100\n";
        var p = await s.Prova(new IceAktarimProvaDto
        {
            Kaynak = "acilis-stok", DosyaAdi = "dg-t2-sinir.csv", Gerekce = "GOC-C tur 2 dogrulayici sinir provasi — yeterince uzun",
            IcerikBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csv)),
        }, k.Islem);
        var j = JsonDocument.Parse(JsonSerializer.Serialize(p)).RootElement;
        Assert.Equal(2, j.GetProperty("gecerliSatir").GetInt32());   // S-A3 (1000000 tam tavan) + S-G1 (0,001 g)
        Assert.Equal(6, j.GetProperty("hataliSatir").GetInt32());
        var hatalar = j.GetProperty("ilkHatalar").EnumerateArray()
            .Select(h => (h.GetProperty("satirNo").GetInt32(), h.GetProperty("hataKodu").GetString())).OrderBy(x => x.Item1).ToList();
        Assert.Equal(new[] { 2, 3, 5, 6, 8, 9 }, hatalar.Select(x => x.Item1).ToArray());
        // "3.000" belirsiz binlik ayracı (FormAlani/SayiOku kuralı) — sessizce 3 ya da 3000 okunmaz.
        Assert.Equal("SAYI_BELIRSIZ", hatalar.Single(x => x.Item1 == 6).Item2);
        Assert.All(hatalar.Where(x => x.Item1 != 6), h => Assert.Equal("ADET_GECERSIZ", h.Item2));
    }
}
