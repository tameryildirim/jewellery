using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;
using MSiparis = Kuyumcu.Api.Modules.Siparis;
using MUrun = Kuyumcu.Api.Modules.Urun;
using MSayim = Kuyumcu.Api.Modules.Sayim;
using MFiyat = Kuyumcu.Api.Modules.Fiyat;
using MKurulum = Kuyumcu.Api.Modules.Kurulum;
using MHurda = Kuyumcu.Api.Modules.Hurda;

namespace Kuyumcu.Tests;

// olumsuz-fikstür: dosya — bu dosya kuralların SINIRLARINI sınar; geçersiz değerler (taş > gram, kesirli adet,
// 0 gram takas, %101 iskonto, eksi alış maliyeti, 51 karakter barkod, geçmiş söz günü, 3000 yılı …) bilinçlidir.
// ═════════════════════════════════════════════════════════════════════════════
// PAKET GOC-C — ürün ve fiş ekranlarının ortak girdi + ortak ızgara göçü.
// Kaynak: dokumanlar/09-uygulama-kayitlari/GIRDI-KURALLARI-ENVANTER-2026-10-08.md (E) Faz 2 GOC-C:
// taş > gram (#5), adet tam sayı (#6), takas 0 (#7), iskonto, barkod 60/50, sipariş söz günü + RAPOR-4 devri
// (ürün servis yolunda eksi alış maliyeti). Her açık için sunucu 400 (+ alan + SATIR YAZILMAZ) ve ileti ekranın
// alan altı metniyle AYNI sözlük anahtarından (GirdiSozlugu).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Saf: sözleşme ve kural kapısı (veritabanı yok).</summary>
public sealed class GocCSafTestleri
{
    private static string M(GirdiKodu kod, decimal? s1 = null, decimal? s2 = null, DateOnly? gun = null) =>
        GirdiSozlugu.Mesaj(new GirdiHatasi(kod, s1, s2, gun), "tr");

    private static T Oz<T>(Type t, string ozellik) where T : Attribute =>
        t.GetProperty(ozellik)!.GetCustomAttributes(true).OfType<T>().Single();

    private static bool Var<T>(Type t, string ozellik) where T : Attribute =>
        t.GetProperty(ozellik)!.GetCustomAttributes(true).OfType<T>().Any();

    [Fact]
    public void GCS01_Izgara_bilinen_ekranlar_GOC_C_ekranlarini_icerir()
    {
        foreach (var e in new[] { "urunler", "urun-agaci", "urun-etiketleri", "vitrin", "fiyatlandirma", "sayim", "kurulum",
                                  "veri-ice-aktarim", "veri-disa-aktarim", "islemler", "siparisler", "siparis-detay" })
            Assert.True(IzgaraTercihKurali.AnahtarGecerli($"grid:{e}:liste"), e);
        Assert.False(IzgaraTercihKurali.AnahtarGecerli("grid:urunlerx:liste"));
    }

    [Fact]
    public void GCS02_Ekran_siniri_DTO_siniriyla_ayni_sabit()
    {
        Assert.Equal(AlanSinirlari.UrunKod, Oz<MaxLengthAttribute>(typeof(MUrun.UrunOlusturDto), "Kod").Length);
        Assert.Equal(AlanSinirlari.UrunAd, Oz<MaxLengthAttribute>(typeof(MUrun.UrunOlusturDto), "Ad").Length);
        Assert.Equal(AlanSinirlari.UrunKod, Oz<MaxLengthAttribute>(typeof(MUrun.UrunGuncelleDto), "Kod").Length);
        Assert.Equal(AlanSinirlari.UrunAd, Oz<MaxLengthAttribute>(typeof(MUrun.UrunGuncelleDto), "Ad").Length);
        // Barkod uzunluğu BarkodKurali'nda (AlanSinirlari.Barkod) — DTO'da [Barkod] (GCS03); ayrı MaxLength YOK (tek ileti).
        Assert.DoesNotContain(typeof(MUrun.UrunOlusturDto).GetProperty("Barkod")!.GetCustomAttributes(true), a => a is MaxLengthAttribute);
        Assert.Equal(AlanSinirlari.UrunAciklama, Oz<MaxLengthAttribute>(typeof(MUrun.UrunOlusturDto), "Aciklama").Length);
        Assert.Equal(AlanSinirlari.UrunSertifikaNo, Oz<MaxLengthAttribute>(typeof(MUrun.UrunOlusturDto), "SertifikaNo").Length);
        Assert.Equal(AlanSinirlari.UrunSeriNo, Oz<MaxLengthAttribute>(typeof(MUrun.UrunOlusturDto), "SeriNo").Length);
        Assert.Equal(AlanSinirlari.UrunOlcu, Oz<MaxLengthAttribute>(typeof(MUrun.UrunOlusturDto), "Olcu").Length);
        Assert.Equal(AlanSinirlari.UrunKmts, Oz<MaxLengthAttribute>(typeof(MUrun.UrunOlusturDto), "KmtsKodu").Length);
        Assert.Equal(AlanSinirlari.UrunRfid, Oz<MaxLengthAttribute>(typeof(MUrun.UrunOlusturDto), "Rfid").Length);
        Assert.Equal(AlanSinirlari.UrunGorselUrl, Oz<MaxLengthAttribute>(typeof(MUrun.UrunOlusturDto), "GorselUrl").Length);
        Assert.Equal(AlanSinirlari.UrunTasIbaresi, Oz<MaxLengthAttribute>(typeof(MUrun.UrunOlusturDto), "TasIbaresi").Length);
        Assert.Equal(AlanSinirlari.UrunKategoriAdi, Oz<MaxLengthAttribute>(typeof(MUrun.KategoriOlusturDto), "Ad").Length);
        Assert.Equal(AlanSinirlari.SayimAdi, Oz<MaxLengthAttribute>(typeof(MSayim.SayimAcDto), "Ad").Length);
        Assert.Equal(AlanSinirlari.FiyatListesiAdi, Oz<MaxLengthAttribute>(typeof(MFiyat.FiyatListesiDto), "Ad").Length);
        Assert.Equal(AlanSinirlari.KampanyaAdi, Oz<MaxLengthAttribute>(typeof(MFiyat.KampanyaDto), "Ad").Length);
        Assert.Equal(AlanSinirlari.TedarikciFaturaNo, Oz<MaxLengthAttribute>(typeof(MHurda.HurdaSaticiDto), "TedarikciFaturaNo").Length);
        Assert.Equal(AlanSinirlari.SaticiAd, Oz<MaxLengthAttribute>(typeof(MHurda.HurdaSaticiDto), "AdSoyad").Length);
        Assert.Equal(AlanSinirlari.SaticiAdres, Oz<MaxLengthAttribute>(typeof(MHurda.HurdaSaticiDto), "Adres").Length);
        Assert.Equal(AlanSinirlari.IslemAciklama, Oz<MaxLengthAttribute>(typeof(MSatis.SatisFisiDto), "Aciklama").Length);
        Assert.Equal(AlanSinirlari.AktarimGerekce, Oz<MaxLengthAttribute>(typeof(Kuyumcu.Api.Modules.Disaaktarim.DisaAktarimTalepDto), "Gerekce").Length);
    }

    [Fact]
    public void GCS03_DTO_lar_Core_ozniteliklerini_tasir()
    {
        Assert.True(Var<BarkodAttribute>(typeof(MUrun.UrunOlusturDto), "Barkod"));
        Assert.True(Var<BarkodAttribute>(typeof(MUrun.UrunGuncelleDto), "Barkod"));
        Assert.True(Var<YuzdeAttribute>(typeof(MSatis.SatisFisiDto), "FisIskontoYuzde"));
        Assert.True(Var<YuzdeAttribute>(typeof(MSatis.SatisFisKalemDto), "SatirIskontoYuzde"));
        Assert.True(Var<GramAttribute>(typeof(MSiparis.SiparisKalemDto), "GramAgirlik"));
        Assert.True(Var<AdetAttribute>(typeof(MSiparis.SiparisKalemDto), "Adet"));
        Assert.True(Var<TutarAttribute>(typeof(MSiparis.SiparisKaporaDto), "TlTutari"));
        Assert.True(Var<TarihPenceresiAttribute>(typeof(MFiyat.KampanyaDto), "Baslangic"));
        Assert.True(Var<TarihPenceresiAttribute>(typeof(MFiyat.KampanyaDto), "Bitis"));
        // #7: takas alanlarının alt sınırı 0'ın ÜSTÜ (eski Range(0,…) 0'ı geçiriyordu).
        Assert.True((double)Oz<RangeAttribute>(typeof(MSatis.TakasDto), "EskiUrunGram").Minimum > 0);
        Assert.True((double)Oz<RangeAttribute>(typeof(MSatis.TakasDto), "EskiUrunBirimFiyatTlHasGram").Minimum > 0);
        Assert.True((double)Oz<RangeAttribute>(typeof(MSatis.TakasDto), "YeniUrunBirimFiyatTl").Minimum > 0);
    }

    [Fact]
    public void GCS04_Adet_kapisi_ADET_birimde_tam_sayi_GRAM_birimde_kesir_serbest()
    {
        var e = Assert.Throws<BadRequestException>(() => MUrun.UrunAdetKapisi.Dogrula("ADET", 1.5m, "adet"));
        Assert.Equal(M(GirdiKodu.SayiTam), e.Message);
        Assert.Throws<BadRequestException>(() => MUrun.UrunAdetKapisi.Dogrula(null, 0.5m, "adet"));   // birimsiz kayıt ADET sayılır (fail-closed)
        Assert.Throws<BadRequestException>(() => MUrun.UrunAdetKapisi.Dogrula("ADET", 0m, "adet"));
        MUrun.UrunAdetKapisi.Dogrula("ADET", 2m, "adet");
        MUrun.UrunAdetKapisi.Dogrula("GRAM", 1.5m, "adet");
        MUrun.UrunAdetKapisi.Dogrula("GRAM", 0.001m, "adet");
        // Sıfır olabilen sayım/eşik: 0 geçer, eksi ve kesir (ADET) reddedilir.
        MUrun.UrunAdetKapisi.SifirOlabilirDogrula("ADET", 0m, "adet");
        Assert.Equal(M(GirdiKodu.SayiTam), Assert.Throws<BadRequestException>(() => MUrun.UrunAdetKapisi.SifirOlabilirDogrula("ADET", 2.5m, "adet")).Message);
        Assert.Equal(M(GirdiKodu.SayiEksi), Assert.Throws<BadRequestException>(() => MUrun.UrunAdetKapisi.SifirOlabilirDogrula("ADET", -1m, "adet")).Message);
        MUrun.UrunAdetKapisi.SifirOlabilirDogrula("GRAM", 2.5m, "adet");
    }

    [Fact]
    public void GCS05_Urun_girdi_kapisi_tas_brutten_kucuk_eksi_tutar_barkod()
    {
        static void K(string birim, decimal gram, decimal tas, decimal? satis = null, decimal? alis = null,
                      decimal? kons = null, decimal? isc = null, decimal? min = null, string? barkod = null) =>
            MUrun.UrunServisi.GocCKapisi(birim, gram, tas, satis, alis, kons, isc, min, barkod);
        Assert.Equal(M(GirdiKodu.TasBrut), Assert.Throws<BadRequestException>(() => K("ADET", 5m, 10m)).Message);
        Assert.Equal(M(GirdiKodu.TasBrut), Assert.Throws<BadRequestException>(() => K("ADET", 5m, 5m)).Message);   // eşit = has 0
        K("ADET", 5m, 4.999m);                      // tam sınırın altı geçer
        K("GRAM", 1m, 0m);                          // GRAM birimde taş yok sayılır
        Assert.Equal(M(GirdiKodu.SayiEksi), Assert.Throws<BadRequestException>(() => K("ADET", 5m, 0m, alis: -1m)).Message);   // RAPOR-4 devri
        Assert.Throws<BadRequestException>(() => K("ADET", 5m, 0m, satis: -0.01m));
        Assert.Throws<BadRequestException>(() => K("ADET", 5m, 0m, isc: -5m));
        Assert.Throws<BadRequestException>(() => K("ADET", 5m, 0m, kons: -5m));
        K("ADET", 5m, 0m, alis: 0m, satis: 0m, isc: 0m);   // sıfır serbest (K9a)
        Assert.Equal(M(GirdiKodu.SayiTam), Assert.Throws<BadRequestException>(() => K("ADET", 5m, 0m, min: 1.5m)).Message);
        K("GRAM", 1m, 0m, min: 1.5m);
        Assert.Equal(M(GirdiKodu.BarkodKarakter), Assert.Throws<BadRequestException>(() => K("ADET", 5m, 0m, barkod: "ÇAĞ-1")).Message);
        Assert.Throws<BadRequestException>(() => K("ADET", 5m, 0m, barkod: new string('A', AlanSinirlari.Barkod + 1)));
        K("ADET", 5m, 0m, barkod: new string('A', AlanSinirlari.Barkod));
    }

    [Fact]
    public void GCS06_Siparis_soz_gunu_gecmis_ve_2_yil_ustu_red_bugun_gecer()
    {
        var bugun = Kuyumcu.Api.Common.Zaman.YerelBugun();
        Assert.Equal(M(GirdiKodu.TarihErken, gun: bugun),
            Assert.Throws<BadRequestException>(() => MSiparis.SiparisServisi.SozGunuDogrula(bugun.AddDays(-1))).Message);
        var sinir = bugun.AddYears(AlanSinirlari.SiparisSozEnCokYil);
        Assert.Equal(M(GirdiKodu.TarihGec, gun: sinir),
            Assert.Throws<BadRequestException>(() => MSiparis.SiparisServisi.SozGunuDogrula(sinir.AddDays(1))).Message);
        Assert.Equal(bugun, MSiparis.SiparisServisi.SozGunuDogrula(bugun));
        Assert.Equal(sinir, MSiparis.SiparisServisi.SozGunuDogrula(sinir));
        Assert.Null(MSiparis.SiparisServisi.SozGunuDogrula(null));
    }

    [Fact]
    public void GCS07_Iskonto_yuzde_0_100_iki_ondalik_eksi_ve_101_red()
    {
        Assert.Contains(M(GirdiKodu.YuzdeAralik, 0, 100), Assert.Throws<BadRequestException>(() => MSatis.IskontoHesabi.YuzdeDogrula(101m, "Fiş iskontosu")).Message);
        Assert.Throws<BadRequestException>(() => MSatis.IskontoHesabi.YuzdeDogrula(-5m, "Fiş iskontosu"));
        Assert.Contains(M(GirdiKodu.SayiOndalik, 2), Assert.Throws<BadRequestException>(() => MSatis.IskontoHesabi.YuzdeDogrula(10.555m, "Fiş iskontosu")).Message);
        MSatis.IskontoHesabi.YuzdeDogrula(100m, "Fiş iskontosu");
        MSatis.IskontoHesabi.YuzdeDogrula(0m, "Fiş iskontosu");
        MSatis.IskontoHesabi.YuzdeDogrula(12.5m, "Fiş iskontosu");
        MSatis.IskontoHesabi.YuzdeDogrula(null, "Fiş iskontosu");
    }

    [Fact]
    public void GCS08_Kurulum_acilis_fazla_ondalik_400_sessiz_yuvarlama_yok()
    {
        Assert.Throws<BadRequestException>(() => MKurulum.AcilisKaynaklari.OndalikKapisi(
            [new MKurulum.AcilisSatirDto { HedefId = "x", TlTutar = 10.005m }]));
        Assert.Throws<BadRequestException>(() => MKurulum.AcilisKaynaklari.OndalikKapisi(
            [new MKurulum.AcilisSatirDto { HedefId = "x", HasGram = 1.1234567m }]));
        MKurulum.AcilisKaynaklari.OndalikKapisi(
            [new MKurulum.AcilisSatirDto { HedefId = "x", TlTutar = -10.05m, HasGram = -1.123456m }]);   // işaretli (yön) geçer
    }
}

/// <summary>Sunucu (gerçek MySQL + HTTP): her açık 400 + alan + satır YOK; geçerli kayıt yazılır.</summary>
[Collection("http")]
public sealed class GocCSunucuTestleri(HttpFikstur f)
{
    private static string M(GirdiKodu kod, decimal? s1 = null, decimal? s2 = null, DateOnly? gun = null) =>
        GirdiSozlugu.Mesaj(new GirdiHatasi(kod, s1, s2, gun), "tr");

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return IslemBantFikstur.ReferansKur(db, TestKurulum.KiraciKur(db, "PAKETGOC-C-TEST " + ad));
    }

    /// <summary>400 + alan işareti (DTO yolunda "kalemler[0].adet" gibi önekli olabilir) + ilk ileti.</summary>
    private static async Task<string> Red(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), govde);
        var p = alanlar.EnumerateObject().FirstOrDefault(x =>
            x.Name.Equals(alan, StringComparison.OrdinalIgnoreCase) || x.Name.EndsWith("." + alan, StringComparison.OrdinalIgnoreCase));
        Assert.True(p.Value.ValueKind == JsonValueKind.Array, $"'{alan}' işaretli değil: {govde}");
        return p.Value[0].GetString() ?? "";
    }

    private int UrunSayisi(Kiraci k) { using var db = f.Vt.OlusturSistem(); return db.Urunler.AsNoTracking().Count(u => u.TenantId == k.TenantId); }
    private int HareketSayisi(Kiraci k) { using var db = f.Vt.OlusturSistem(); return db.Hareketler.AsNoTracking().Count(h => h.TenantId == k.TenantId); }

    private static object UrunGovde(string kod, Action<Dictionary<string, object?>>? degistir = null)
    {
        var g = new Dictionary<string, object?>
        {
            ["tip"] = "ZIYNET", ["kod"] = kod, ["ad"] = "Burma bilezik", ["ayarId"] = "K22", ["gramAgirlik"] = 10m, ["birim"] = "ADET",
        };
        degistir?.Invoke(g);
        return g;
    }

    // ── #5 taş > gram · RAPOR-4 eksi alış maliyeti · barkod 60/50 (POST) ──────────────────
    [Fact]
    public async Task GCH01_Urun_POST_tas_gram_eksi_maliyet_barkod_400_satir_YOK_gecerli_kayit()
    {
        var k = YeniKiraci("urun post");
        var c = f.Ortam.Istemci(k);
        var once = UrunSayisi(k);
        Assert.Equal(M(GirdiKodu.TasBrut), await Red(await c.PostJson("/api/v1/urunler",
            UrunGovde("GC-1", g => { g["gramAgirlik"] = 5m; g["tasAgirlik"] = 10m; g["tasKokeni"] = "DOGAL"; })), "tasAgirlik"));
        Assert.Equal(M(GirdiKodu.SayiEksi), await Red(await c.PostJson("/api/v1/urunler",
            UrunGovde("GC-2", g => g["alisMaliyeti"] = -100m)), "alisMaliyeti"));
        await Red(await c.PostJson("/api/v1/urunler", UrunGovde("GC-3", g => g["barkod"] = new string('A', AlanSinirlari.Barkod + 1))), "barkod");
        Assert.Equal(M(GirdiKodu.BarkodKarakter), await Red(await c.PostJson("/api/v1/urunler",
            UrunGovde("GC-4", g => g["barkod"] = "ÇAĞ-001")), "barkod"));
        Assert.Equal(M(GirdiKodu.SayiTam), await Red(await c.PostJson("/api/v1/urunler",
            UrunGovde("GC-5", g => g["acilisStogu"] = new { adet = 1.5m })), "acilisStogu.adet"));
        Assert.Equal(once, UrunSayisi(k));

        var ok = await c.PostJson("/api/v1/urunler", UrunGovde("GC-OK", g =>
        {
            g["gramAgirlik"] = 5m; g["tasAgirlik"] = 0.5m; g["tasKokeni"] = "DOGAL"; g["alisMaliyeti"] = 0m; g["barkod"] = "GC-OK-01";
        }));
        Assert.True(ok.StatusCode == HttpStatusCode.Created, $"{(int)ok.StatusCode} {await ok.Content.ReadAsStringAsync()}");
        using var db = f.Vt.OlusturSistem();
        var u = db.Urunler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Kod == "GC-OK");
        Assert.Equal(0.5m, u.TasAgirlik);
        Assert.True(u.HasGrami > 0m);
        Assert.Equal("GC-OK-01", u.Barkod);
    }

    // ── #5 PATCH: KAYITLI + GELEN birleşimi · eksi maliyet PATCH (RAPOR-4) ──────────────────
    [Fact]
    public async Task GCH02_Urun_PATCH_gram_tasin_altina_ve_eksi_maliyet_400_kayit_DEGISMEZ()
    {
        var k = YeniKiraci("urun patch");
        var c = f.Ortam.Istemci(k);
        var ok = await c.PostJson("/api/v1/urunler", UrunGovde("GCP-1", g => { g["gramAgirlik"] = 10m; g["tasAgirlik"] = 2m; g["tasKokeni"] = "DOGAL"; }));
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        var id = JsonDocument.Parse(await ok.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetString()!;

        Assert.Equal(M(GirdiKodu.TasBrut), await Red(await c.PatchAsJsonAsync($"/api/v1/urunler/{id}", new { gramAgirlik = 1.5m }), "tasAgirlik"));
        Assert.Equal(M(GirdiKodu.SayiEksi), await Red(await c.PatchAsJsonAsync($"/api/v1/urunler/{id}", new { alisMaliyeti = -1m }), "alisMaliyeti"));
        using (var db = f.Vt.OlusturSistem())
        {
            var u = db.Urunler.AsNoTracking().Single(x => x.Id == id);
            Assert.Equal(10m, u.GramAgirlik);
            Assert.Null(u.AlisMaliyeti);
        }
        var r = await c.PatchAsJsonAsync($"/api/v1/urunler/{id}", new { gramAgirlik = 2.5m, alisMaliyeti = 1500m });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        using var db2 = f.Vt.OlusturSistem();
        Assert.Equal(2.5m, db2.Urunler.AsNoTracking().Single(x => x.Id == id).GramAgirlik);
    }

    // ── #6 kesirli adet + iskonto (satış fişi) ─────────────────────────────────────────────
    [Fact]
    public async Task GCH03_Satis_fisi_kesirli_adet_ve_iskonto_101_eksi_400_hareket_YOK_gecerli_satis()
    {
        var k = YeniKiraci("satis");
        var c = f.Ortam.Istemci(k);
        var once = HareketSayisi(k);
        object Fis(decimal adet, decimal? iskonto) => new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId, fisIskontoYuzde = iskonto,
            kalemler = new[] { new { urunId = k.UrunId, adet, birimFiyatTl = 1000m } },
            odemeler = new[] { new { arac = "NAKIT", tutarTl = adet * 1000m * (1m - (iskonto ?? 0m) / 100m), kasaId = k.KasaId } },
        };
        Assert.Contains(M(GirdiKodu.SayiTam), await Red(await c.PostJson("/api/v1/islemler/satis-fisi", Fis(1.5m, null)), "kalemler"));
        await Red(await c.PostJson("/api/v1/islemler/satis-fisi", Fis(1m, 101m)), "fisIskontoYuzde");
        await Red(await c.PostJson("/api/v1/islemler/satis-fisi", Fis(1m, -5m)), "fisIskontoYuzde");
        Assert.Equal(once, HareketSayisi(k));

        var ok = await c.PostJson("/api/v1/islemler/satis-fisi", Fis(1m, 10m));
        Assert.True(ok.IsSuccessStatusCode, $"{(int)ok.StatusCode} {await ok.Content.ReadAsStringAsync()}");
        Assert.True(HareketSayisi(k) > once);
    }

    // ── #7 takas 0 (HTTP + servis yolu) ────────────────────────────────────────────────────
    [Fact]
    public async Task GCH04_Takas_sifir_gram_sifir_fiyat_kesirli_adet_400_hareket_YOK()
    {
        var k = YeniKiraci("takas");
        var c = f.Ortam.Istemci(k);
        var once = HareketSayisi(k);
        object Takas(decimal gram, decimal eskiFiyat, decimal yeniAdet, decimal yeniFiyat) => new
        {
            cariId = k.CariId, eskiUrunAyarId = "K22", eskiUrunGram = gram, eskiUrunBirimFiyatTlHasGram = eskiFiyat,
            yeniUrunId = k.UrunId, yeniUrunAdet = yeniAdet, yeniUrunBirimFiyatTl = yeniFiyat,
            odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId,
            satici = new { saticiTipi = "NIHAI_TUKETICI", adSoyad = "Ayşe Yılmaz", tckn = "10000000146", adres = "Kadıköy Moda Caddesi 1" },
        };
        await Red(await c.PostJson("/api/v1/islemler/takas", Takas(0m, 3000m, 1m, 20000m)), "eskiUrunGram");
        await Red(await c.PostJson("/api/v1/islemler/takas", Takas(3m, 0m, 1m, 20000m)), "eskiUrunBirimFiyatTlHasGram");
        await Red(await c.PostJson("/api/v1/islemler/takas", Takas(3m, 3000m, 1m, 0m)), "yeniUrunBirimFiyatTl");
        Assert.Contains(M(GirdiKodu.SayiTam), await Red(await c.PostJson("/api/v1/islemler/takas", Takas(3m, 3000m, 1.5m, 20000m)), "yeniUrunAdet"));
        Assert.Equal(once, HareketSayisi(k));

        // Servis yolu (DTO özniteliği koşmaz): aynı kural, sözlük iletisi.
        using var db = f.Vt.Olustur(k);
        var servis = new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance);
        var dto = new MSatis.TakasDto
        {
            CariId = k.CariId, EskiUrunAyarId = "K22", EskiUrunGram = 0m, EskiUrunBirimFiyatTlHasGram = 3000m,
            YeniUrunId = k.UrunId, YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = 20000m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        };
        Assert.Equal(M(GirdiKodu.SayiSifir), (await Assert.ThrowsAsync<BadRequestException>(() => servis.Takas(dto, k.Islem))).Message);
        Assert.Equal(once, HareketSayisi(k));
    }

    // ── Sipariş söz günü + kalem kuralları (POST + PATCH) ─────────────────────────────────
    [Fact]
    public async Task GCH05_Siparis_gecmis_soz_gunu_kesirli_adet_sifir_gram_400_gecerli_kayit_PATCH_kayitli_gun_korunur()
    {
        var k = YeniKiraci("siparis");
        var c = f.Ortam.Istemci(k);
        var bugun = Kuyumcu.Api.Common.Zaman.YerelBugun();
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.Siparisler.AsNoTracking().Count(s => s.TenantId == k.TenantId); }
        object Govde(DateOnly? gun, decimal adet = 1m, decimal? gram = 5m) => new
        {
            tur = "MUSTERI_SIPARISI", cariId = k.CariId, sozVerilenGun = gun?.ToString("yyyy-MM-dd"),
            kalemler = new[] { new { tanim = "Burgu bilezik", adet, gramAgirlik = gram } },
        };
        Assert.Equal(M(GirdiKodu.TarihErken, gun: bugun), await Red(await c.PostJson("/api/v1/siparisler", Govde(bugun.AddDays(-1))), "sozVerilenGun"));
        await Red(await c.PostJson("/api/v1/siparisler", Govde(bugun.AddYears(AlanSinirlari.SiparisSozEnCokYil).AddDays(1))), "sozVerilenGun");
        Assert.Contains(M(GirdiKodu.SayiTam), await Red(await c.PostJson("/api/v1/siparisler", Govde(bugun, adet: 1.5m)), "adet"));
        Assert.Contains(M(GirdiKodu.SayiSifir), await Red(await c.PostJson("/api/v1/siparisler", Govde(bugun, gram: 0m)), "gramAgirlik"));
        Assert.Equal(0, Sayi());

        var ok = await c.PostJson("/api/v1/siparisler", Govde(bugun.AddDays(7)));
        Assert.True(ok.IsSuccessStatusCode, $"{(int)ok.StatusCode} {await ok.Content.ReadAsStringAsync()}");
        var id = JsonDocument.Parse(await ok.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetString()!;
        Assert.Equal(1, Sayi());

        // PATCH: geçmişe çekilemez; KAYITLI gün (zamanla geçmişte kalmış) aynen gönderilirse düzenleme kilitlenmez.
        await Red(await c.PatchAsJsonAsync($"/api/v1/siparisler/{id}", new { sozVerilenGun = bugun.AddDays(-3).ToString("yyyy-MM-dd") }), "sozVerilenGun");
        using (var db = f.Vt.OlusturSistem())
        {
            var s = db.Siparisler.Single(x => x.Id == id);
            s.SozVerilenGun = bugun.AddDays(-2);
            db.SaveChanges();
        }
        var r = await c.PatchAsJsonAsync($"/api/v1/siparisler/{id}", new { sozVerilenGun = bugun.AddDays(-2).ToString("yyyy-MM-dd"), aciklama = "Ölçü 14" });
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        using var db2 = f.Vt.OlusturSistem();
        var son = db2.Siparisler.AsNoTracking().Single(x => x.Id == id);
        Assert.Equal(bugun.AddDays(-2), son.SozVerilenGun);
        Assert.Equal("Ölçü 14", son.Aciklama);
    }

    // ── #6 sayım + rezerv kesirli adet (ADET birim) ────────────────────────────────────────
    [Fact]
    public async Task GCH06_Sayim_ve_rezerv_kesirli_adet_400_GRAM_birimde_kesir_gecer()
    {
        var k = YeniKiraci("sayim");
        var c = f.Ortam.Istemci(k);
        var ac = await c.PostJson("/api/v1/sayim", new { ad = "Ekim vitrin sayımı", subeId = k.SubeId, kapsamTur = "TUMU" });
        Assert.True(ac.IsSuccessStatusCode, $"{(int)ac.StatusCode} {await ac.Content.ReadAsStringAsync()}");
        var oturum = JsonDocument.Parse(await ac.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetString()!;
        Assert.Equal(M(GirdiKodu.SayiTam), await Red(await c.PostJson($"/api/v1/sayim/{oturum}/say", new { urunId = k.UrunId, sayilanAdet = 1.5m }), "sayilanAdet"));
        await Red(await c.PostJson($"/api/v1/sayim/{oturum}/say", new { urunId = k.UrunId }), "sayilanAdet");   // gövdede yok = sessiz 0 DEĞİL
        var say = await c.PostJson($"/api/v1/sayim/{oturum}/say", new { urunId = k.UrunId, sayilanAdet = 3m });
        Assert.True(say.IsSuccessStatusCode, await say.Content.ReadAsStringAsync());

        Assert.Equal(M(GirdiKodu.SayiTam), await Red(await c.PostJson("/api/v1/islemler/rezervler", new { urunId = k.UrunId, cariId = k.CariId, adet = 1.5m }), "adet"));
        using (var db = f.Vt.OlusturSistem())
        {
            Assert.Equal(0, db.Rezervasyonlar.AsNoTracking().Count(r => r.TenantId == k.TenantId));
            TestKurulum.UrunuGramBirimiYap(db, k.UrunId);
        }
        var gramli = await c.PostJson("/api/v1/islemler/rezervler", new { urunId = k.UrunId, cariId = k.CariId, adet = 1.5m });
        Assert.Equal(HttpStatusCode.Created, gramli.StatusCode);
    }

    // ── Kampanya yıl penceresi (K8) ────────────────────────────────────────────────────────
    [Fact]
    public async Task GCH07_Kampanya_0001_ve_3000_yili_400()
    {
        var k = YeniKiraci("kampanya");
        var c = f.Ortam.Istemci(k);
        object Govde(string bas, string bit) => new { ad = "Ekim indirimi", baslangic = bas, bitis = bit, hedefTur = "TUMU", indirimTuru = "TUTAR_YUZDE", deger = 10m, aktif = true };
        await Red(await c.PostJson("/api/v1/fiyat/kampanyalar", Govde("0001-01-01", "2026-12-01")), "baslangic");
        await Red(await c.PostJson("/api/v1/fiyat/kampanyalar", Govde("2026-11-01", "3000-01-01")), "bitis");
    }

    // ── Ortak ızgara tercihi: A'da kalıcı, B'de yok ───────────────────────────────────────
    [Fact]
    public async Task GCH08_Urunler_izgara_tercihi_A_da_kalici_B_de_yok()
    {
        var k = YeniKiraci("izgara");
        const string anahtar = "grid:urunler:urun-liste";
        const string deger = """{"s":1,"k":["ad","gram","has","stok","ayar","gorsel"],"g":["gorsel"],"o":{"a":"gram","y":"azalan"}}""";
        var a = f.Ortam.Istemci(k);
        var yaz = await a.PatchAsJsonAsync("/api/v1/tercihler/benim", new { tercihler = new Dictionary<string, string?> { [anahtar] = deger } });
        Assert.Equal(HttpStatusCode.OK, yaz.StatusCode);
        var oku = await (await a.GetAsync("/api/v1/tercihler/benim")).Json();
        Assert.Equal(deger, oku.GetProperty("tercihler").GetProperty(anahtar).GetString());
        var b = f.Ortam.Istemci("gocc-b-kullanici", k.TenantId, k.SubeId, "MUDUR");
        var bOku = await (await b.GetAsync("/api/v1/tercihler/benim")).Json();
        Assert.False(bOku.GetProperty("tercihler").TryGetProperty(anahtar, out _));
    }
}

/// <summary>Ekran (bUnit): ortak bileşenler alan altı Türkçe ileti + Kaydet kilidi; istek GİTMEZ.</summary>
public sealed class GocCEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private static readonly TimeSpan Uzun = TimeSpan.FromSeconds(5);

    public GocCEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private static string M(GirdiKodu kod, decimal? s1 = null, decimal? s2 = null, DateOnly? gun = null) =>
        GirdiSozlugu.Mesaj(new GirdiHatasi(kod, s1, s2, gun), "tr");

    private sealed class Sahte(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, yol, govde));
            var g = yol switch
            {
                var y when y.EndsWith("/siparisler") && istek.Method == HttpMethod.Get =>
                    """{"limit":200,"azamiLimit":200,"toplam":0,"dahaFazlasiVar":false,"gecikenSayisi":0,"durumKirilimi":[],"siparisler":[]}""",
                var y when y.EndsWith("/siparisler") => """{"id":"s9","no":"S-2026-000009"}""",
                var y when y.EndsWith("/subeler") => """[{"id":"sb1","ad":"Merkez"}]""",
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz","tur":"MUSTERI"}]""",
                var y when y.EndsWith("/urunler/madenler") => """
                    {"madenler":[{"maden":"ALTIN","madenAdi":"Altın","rodyumKaplanabilir":true,
                      "ayarlar":[{"id":"K22","ad":"22 Ayar","milyem":916,"kiymetli":true,"hasHesaplanir":true}]}],
                     "etiket":{"rodyumIbaresiGoster":false,"rodyumIbaresi":"rodyum kaplama"}}
                    """,
                var y when y.Contains("/urunler/stok-adetleri") => """{"urunler":[]}""",
                var y when y.EndsWith("/urunler") && istek.Method == HttpMethod.Post => """{"id":"u9"}""",
                var y when y.Contains("/urunler") => "[]",
                var y when y.Contains("/urun-kategorileri") => "[]",
                var y when y.Contains("/reyonlar") => "[]",
                var y when y.Contains("/kur/pariteler") => """{"kaynak":"TEST","pariteler":[]}""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"kaynak":"MANUAL","olusturuldu":"2026-10-08T08:00:00Z","ayarFiyatlari":[]}""",
                // TEMA TEMİZLİĞİ 17.10: Yıldız açık sayımı kendiliğinden açar — ayrıntı yanıtı gerekir.
                var y when y.Contains("/sayim/o1") => """{"id":"o1","ad":"Ekim sayımı","subeId":"sb1","kapsamTur":"TUMU","durum":"ACIK","toplamSatir":0,"sayilan":0,"sayilmayan":0,"farkli":0,"eksikAdet":0,"fazlaAdet":0,"netFarkHas":0,"hareketli":0,"satirlar":[]}""",
                var y when y.EndsWith("/sayim") && istek.Method == HttpMethod.Get => """[{"id":"o1","ad":"Ekim sayımı","kapsamTur":"TUMU","durum":"ACIK"}]""",
                var y when y.Contains("/tercihler/benim") => """{"tercihler":{}}""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private void Oturum(string mod = "PROFESYONEL") =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanici", "t@ornek.com", "SAHIP", "sb1", mod, mod));

    private int PostSayisi(string sonEk) { lock (istekler) return istekler.Count(i => i.Metot == "POST" && i.Yol.EndsWith(sonEk)); }

    // ── Sipariş: geçmiş söz günü + kesirli adet → alan altı + kilit, POST YOK ─────────────
    [Fact]
    public void GCE01_Siparis_gecmis_soz_gunu_ve_kesirli_adet_alan_alti_Kaydet_kilitli_POST_YOK()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Siparisler>().AyrintiAc();
        e.WaitForElement("#sip-yeni-ac", Uzun).Click();
        e.WaitForElement("#sip-form", Uzun);
        SeciciYardim.Sec(e, "sip-f-cari", "c1");
        e.Find("#sip-f-kalem-tanim-0").Input("Burgu bilezik");
        e.Find("#sip-f-detay-ac").Click();
        var dun = DateTime.Today.AddDays(-1);
        e.Find("#sip-f-gun").Change(dun.ToString("ddMMyyyy"));
        e.WaitForAssertion(() => Assert.Contains(M(GirdiKodu.TarihErken, gun: DateOnly.FromDateTime(DateTime.Today)), e.Find("#sip-f-gun-hata").TextContent), Uzun);
        Assert.True(e.Find("#sip-f-kaydet").HasAttribute("disabled"));
        e.Find("#sip-f-gun").Change(DateTime.Today.AddDays(5).ToString("ddMMyyyy"));
        e.Find("#sip-f-kalem-adet-0").Input("1,5");
        e.WaitForAssertion(() => Assert.Contains(M(GirdiKodu.SayiTam), e.Find("#sip-f-kalem-adet-0-hata").TextContent), Uzun);
        Assert.True(e.Find("#sip-f-kaydet").HasAttribute("disabled"));
        Assert.Contains(M(GirdiKodu.SayiTam), e.Find("#sip-f-sayi-kilit").TextContent);   // kilit sebebi = alan altı metni
        e.Find("#sip-f-kaydet").Click();
        Assert.Equal(0, PostSayisi("/siparisler"));
        e.Find("#sip-f-kalem-adet-0").Input("2");
        e.WaitForAssertion(() => Assert.False(e.Find("#sip-f-kaydet").HasAttribute("disabled")), Uzun);
    }

    // ── Ürün: PRO'da da zorunlu önce; taş ≥ gram alan altı + kilit; barkod 50 sınırı ──────
    [Fact]
    public void GCE02_Urun_yeni_form_PRO_zorunlu_once_tas_gram_alan_alti_kilit_barkod_siniri()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Urunler>().AyrintiAc();
        e.WaitForElement("#urun-yeni-ac", Uzun).Click();
        // Devir: isteğe bağlı alanlar açılışta YOK (PRO dahil); "Daha fazla" ile açılır.
        Assert.Empty(e.FindAll("#u-tas"));
        Assert.Empty(e.FindAll("#u-alis-maliyeti"));
        Assert.Equal(AlanSinirlari.Barkod.ToString(System.Globalization.CultureInfo.InvariantCulture), e.Find("#u-barkod").GetAttribute("maxlength"));
        e.Find("#u-daha-fazla").Click();
        e.Find("#u-gram").Input("5");
        SeciciYardim.Sec(e, "u-ayar", "K18");   // P-URUN (K6): ayarın varsayılanı yok — kullanıcı seçer
        e.Find("#u-tas").Input("6");
        e.WaitForAssertion(() => Assert.Contains(M(GirdiKodu.TasBrut), e.Find("#alan-hata-u-tas").TextContent), Uzun);
        Assert.True(e.Find("#u-kaydet").HasAttribute("disabled"));
        Assert.Contains(M(GirdiKodu.TasBrut), e.Find("#u-kaydet-kilit").TextContent);
        e.Find("#u-barkod").Input("ÇAĞ");
        e.Find("#u-barkod").Change("ÇAĞ");
        Assert.DoesNotContain("Ç", e.Find("#u-barkod").GetAttribute("value") ?? "");   // Türkçe harf yazılamaz (K18)
    }

    // ── Sayım: ortak ızgara + Sütunlar; ham select/input YOK ──────────────────────────────
    [Fact]
    public void GCE03_Sayim_oturum_listesi_ortak_izgarada_ham_girdi_yok()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Sayim>().SayimOzgunListe();
        e.WaitForElement("#sy-tablo tbody tr", Uzun);
        Assert.Contains("izgara-tablo", e.Find("#sy-tablo").ClassName);
        Assert.NotNull(e.Find("#sy-tablo-sutunlar-ac"));
        Assert.Single(e.FindAll("#sy-durum-o1"));
        e.Find("#sy-yeni").Click();
        Assert.Empty(e.FindAll("select"));
        Assert.Equal("combobox", e.Find("#sy-kapsam").GetAttribute("role"));
    }
}
