using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Grid;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MBanka = Kuyumcu.Api.Modules.Banka;
using MFatura = Kuyumcu.Api.Modules.Fatura;
using MKasa = Kuyumcu.Api.Modules.Kasa;
using MKur = Kuyumcu.Api.Modules.Kur;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// olumsuz-fikstür: dosya — bu dosya kuralların SINIRLARINI sınar; geçersiz değerler (bozuk IBAN, eksi,
// kuruş altı, ileri tarih …) bilinçlidir (denetim.sh TEST_GECERSIZ_FIKSTUR bu dosyayı saymaz).
// ═════════════════════════════════════════════════════════════════════════════
// PAKET GOC-A — para ekranlarının ortak girdi + ortak ızgara göçü.
// Kaynak: dokumanlar/09-uygulama-kayitlari/GIRDI-KURALLARI-ENVANTER-2026-10-08.md (E) Faz 2 GOC-A.
// Her açık için: sunucu 400 (+ alan + SATIR YAZILMAZ) ve mesaj ekranın alan altı metniyle AYNI sözlük
// anahtarından (GirdiSozlugu). Hasım değerleri: harf, 3000 yılı / ileri tarih, eksi, kesirli adet,
// bozuk IBAN, kuruş altı, tavan üstü. Geçerli fikstürler algoritmadan geçer (TestIban).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Saf: sözleşme ve kural kapısı (veritabanı yok).</summary>
public sealed class GocASafTestleri
{
    private static string M(GirdiKodu kod, decimal? s1 = null, decimal? s2 = null) =>
        GirdiSozlugu.Mesaj(new GirdiHatasi(kod, s1, s2), "tr");

    [Fact]
    public void GA01_Izgara_bilinen_ekranlar_on_uc_para_ekranini_icerir_bilinmeyen_red()
    {
        foreach (var e in new[] { "banka", "banka-ekstre", "faturalar", "kasalar", "kasa-raporu", "vezne", "kur",
                                  "pos", "pos-tahsilatlar", "gider", "gider-pusulalari", "hurda", "emanet" })
            Assert.True(IzgaraTercihKurali.AnahtarGecerli($"grid:{e}:liste"), e);
        Assert.True(IzgaraTercihKurali.AnahtarGecerli("grid:cariler:cari-liste"));
        Assert.False(IzgaraTercihKurali.AnahtarGecerli("grid:vezneler:liste"));
        Assert.False(IzgaraTercihKurali.AnahtarGecerli("grid:Vezne:liste"));
    }

    [Fact]
    public void GA02_Yerel_siralama_sayi_tarih_turkce_metin_ve_bos_deger()
    {
        var kolonlar = new List<IzgaraKolonu<(string Ad, decimal? Tutar, DateTime Tarih)>>
        {
            new() { Anahtar = "ad", Baslik = "Ad", Deger = x => x.Ad },
            new() { Anahtar = "tutar", Baslik = "Tutar", Deger = x => x.Tutar },
            new() { Anahtar = "tarih", Baslik = "Tarih", Deger = x => x.Tarih },
        };
        var l = new List<(string Ad, decimal? Tutar, DateTime Tarih)>
        {
            ("Şule", 10m, new DateTime(2026, 1, 3)), ("Can", null, new DateTime(2026, 1, 1)),
            ("Ömer", 2m, new DateTime(2026, 1, 2)), ("İnci", 100m, new DateTime(2026, 1, 4)),
        };
        Assert.Equal(["Ömer", "Şule", "İnci", "Can"],
            IzgaraYerelSiralama.Uygula(l, new IzgaraSiralama("tutar", "tutar", false), kolonlar).Select(x => x.Ad));
        Assert.Equal(["Can", "İnci", "Şule", "Ömer"],
            IzgaraYerelSiralama.Uygula(l, new IzgaraSiralama("tutar", "tutar", true), kolonlar).Select(x => x.Ad));
        Assert.Equal(["Can", "Ömer", "Şule", "İnci"],
            IzgaraYerelSiralama.Uygula(l, new IzgaraSiralama("tarih", "tarih", false), kolonlar).Select(x => x.Ad));
        // tr-TR: C < İ < Ö < Ş
        Assert.Equal(["Can", "İnci", "Ömer", "Şule"],
            IzgaraYerelSiralama.Uygula(l, new IzgaraSiralama("ad", "ad", false), kolonlar).Select(x => x.Ad));
        Assert.Same(l, IzgaraYerelSiralama.Uygula(l, null, kolonlar));
    }

    [Theory]
    [InlineData("0.005")]
    [InlineData("1000.004")]
    [InlineData("12.345")]
    public void GA03_GirdiKapisi_tutar_kurus_alti_YUVARLAMAZ_400_sozluk_metni(string ham)
    {
        var ex = Assert.Throws<BadRequestException>(() => GirdiKapisi.Tutar(decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture), "tutar"));
        Assert.Equal(M(GirdiKodu.SayiOndalik, 2), ex.Message);
        Assert.Equal("tutar", AlanIsareti.Alan(ex));
    }

    [Fact]
    public void GA04_GirdiKapisi_eksi_sifir_tavan_gerekce_isaretli_has()
    {
        Assert.Equal(M(GirdiKodu.SayiEksi), Assert.Throws<BadRequestException>(() => GirdiKapisi.Tutar(-1m, "t")).Message);
        Assert.Equal(M(GirdiKodu.SayiSifir), Assert.Throws<BadRequestException>(() => GirdiKapisi.Tutar(0m, "t")).Message);
        Assert.Equal(0m, GirdiKapisi.Tutar(0m, "t", TutarTuru.SifirOlabilir));
        Assert.Throws<BadRequestException>(() => GirdiKapisi.Tutar(TutarKurali.Tavan + 0.01m, "t"));
        Assert.Equal(M(GirdiKodu.GerekceKisa, 20, 4), Assert.Throws<BadRequestException>(() => GirdiKapisi.Gerekce(" kısa ", "g")).Message);   // kırpılmış uzunluk
        Assert.Equal(M(GirdiKodu.GerekceUzun, 500, 501),
            Assert.Throws<BadRequestException>(() => GirdiKapisi.Gerekce(new string('a', 501), "g", 500)).Message);
        Assert.Equal(-0.123456m, GirdiKapisi.IsaretliHas(-0.123456m, "h"));
        Assert.Equal(M(GirdiKodu.SayiOndalik, 6), Assert.Throws<BadRequestException>(() => GirdiKapisi.IsaretliHas(0.1234567m, "h")).Message);
        Assert.Equal(M(GirdiKodu.SayiTam), Assert.Throws<BadRequestException>(() => GirdiKapisi.Adet(1.5m, "a")).Message);
    }

    private static int Uzunluk<TDto>(string ozellik)
    {
        var p = typeof(TDto).GetProperty(ozellik)!;
        return p.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength
            ?? p.GetCustomAttribute<MaxLengthAttribute>()!.Length;
    }

    /// <summary>Envanter F2 "ekran maxlength = sunucu sınırı": ekran ve DTO AYNI sabiti okur.</summary>
    [Fact]
    public void GA05_Ekran_siniri_sunucu_DTO_siniriyla_ayni_sabit()
    {
        Assert.Equal(AlanSinirlari.HesapSahibi, Uzunluk<MBanka.HesapOlusturDto>(nameof(MBanka.HesapOlusturDto.HesapSahibi)));
        Assert.Equal(AlanSinirlari.HesapSahibi, Uzunluk<MBanka.HareketEkleDto>(nameof(MBanka.HareketEkleDto.KarsiTarafAd)));
        Assert.Equal(AlanSinirlari.DekontAciklama, Uzunluk<MBanka.HareketEkleDto>(nameof(MBanka.HareketEkleDto.Aciklama)));
        Assert.Equal(AlanSinirlari.DekontAciklamaDuzelt, Uzunluk<MBanka.HareketDuzeltDto>(nameof(MBanka.HareketDuzeltDto.Aciklama)));
        Assert.Equal(AlanSinirlari.BankaAdi, Uzunluk<MBanka.HesapOlusturDto>(nameof(MBanka.HesapOlusturDto.BankaAdi)));
        Assert.Equal(AlanSinirlari.FaturaKalemAciklama, Uzunluk<MFatura.FaturaKalemDto>(nameof(MFatura.FaturaKalemDto.Aciklama)));
        Assert.Equal(AlanSinirlari.FaturaAciklama, Uzunluk<MFatura.FaturaOlusturDto>(nameof(MFatura.FaturaOlusturDto.Aciklama)));
        Assert.Equal(AlanSinirlari.KasaAdi, Uzunluk<MKasa.KasaGuncelleDto>(nameof(MKasa.KasaGuncelleDto.Ad)));
        Assert.Equal(AlanSinirlari.HareketAciklama, Uzunluk<MKasa.KasaHareketDto>(nameof(MKasa.KasaHareketDto.Aciklama)));
        Assert.Equal(AlanSinirlari.GerekceEnCok, Uzunluk<MKasa.GunKapanisiDto>(nameof(MKasa.GunKapanisiDto.Aciklama)));
        // Kur: ekran KurAlani EnCokOndalik = sunucu kolon ondalığı (decimal(15,4)).
        var ekran = (int)typeof(Kuyumcu.Web.Pages.Kur).GetField("KurOndalik", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        Assert.Equal(MKur.KurServisi.KurKolonOndalik, ekran);
    }

    [Fact]
    public void GA06_Karsi_IBAN_DTO_Core_IBAN_ozniteligi_tasir()
    {
        Assert.NotNull(typeof(MBanka.HareketEkleDto).GetProperty(nameof(MBanka.HareketEkleDto.KarsiIban))!.GetCustomAttribute<IbanAttribute>());
        Assert.NotNull(typeof(MBanka.HareketDuzeltDto).GetProperty(nameof(MBanka.HareketDuzeltDto.KarsiIban))!.GetCustomAttribute<IbanAttribute>());
    }
}

/// <summary>HTTP: her açığın sunucu 400'ü (gerçek MySQL + SQL kanıtı).</summary>
[Collection("http")]
public sealed class GocASunucuTestleri(HttpFikstur f)
{
    private static string M(GirdiKodu kod, decimal? s1 = null, decimal? s2 = null) =>
        GirdiSozlugu.Mesaj(new GirdiHatasi(kod, s1, s2), "tr");

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETGOC-A-TEST " + ad);
    }

    private static async Task<(string Alan, string Mesaj, string Govde)> Red(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), govde);
        var p = alanlar.EnumerateObject().FirstOrDefault(x => x.Name.Equals(alan, StringComparison.OrdinalIgnoreCase));
        Assert.True(p.Value.ValueKind == JsonValueKind.Array, $"'{alan}' işaretli değil: {govde}");
        return (p.Name, p.Value[0].GetString() ?? "", govde);
    }

    private async Task<string> BankaHesabi(Kiraci k, HttpClient c)
    {
        var r = await c.PostJson("/api/v1/bankalar", new { bankaAdi = "GOC-A Banka", hesapAdi = "Vadesiz", iban = TestIban.Uret(), paraBirimi = "TRY" });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        return (await r.Json()).GetProperty("id").GetString()!;
    }

    private int BankaHareketSayisi(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.BankaHareketleri.AsNoTracking().Count(h => h.TenantId == k.TenantId);
    }

    // ── IBAN (#1) · K1 — karşı IBAN artık Core MOD 97-10 ─────────────────────────
    [Fact]
    public async Task GB01_Banka_dekont_karsi_IBAN_mod97_bozuk_400_satir_YOK_gecerli_bosluklu_normallesir()
    {
        var k = YeniKiraci("karsi iban");
        var c = f.Ortam.Istemci(k);
        var hesap = await BankaHesabi(k, c);
        var (_, mesaj, _) = await Red(await c.PostJson($"/api/v1/bankalar/{hesap}/hareket",
            new { yon = "GELEN", tutar = 100m, karsiIban = "TR340006100519786457841326" }), "karsiIban");
        Assert.Equal(GirdiSozlugu.Tr.IbanKontrol, mesaj);
        Assert.Equal(0, BankaHareketSayisi(k));
        // Harfli hesap bölümü olmayan "TR + harf" (eski desenin kabul ettiği) da 400.
        await Red(await c.PostJson($"/api/v1/bankalar/{hesap}/hareket",
            new { yon = "GELEN", tutar = 100m, karsiIban = "TRAB0006100519786457841326" }), "karsiIban");
        var gecerli = TestIban.Uret();
        var bosluklu = string.Join(' ', Enumerable.Range(0, (gecerli.Length + 3) / 4).Select(i => gecerli.Substring(i * 4, Math.Min(4, gecerli.Length - i * 4)))).ToLowerInvariant();
        var ok = await c.PostJson($"/api/v1/bankalar/{hesap}/hareket", new { yon = "GELEN", tutar = 100m, karsiIban = bosluklu });
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(gecerli, db.BankaHareketleri.AsNoTracking().Single(h => h.TenantId == k.TenantId).KarsiIban);
    }

    [Fact]
    public async Task GB02_Banka_dekont_PATCH_karsi_IBAN_bozuk_400_kayit_DEGISMEZ()
    {
        var k = YeniKiraci("karsi iban patch");
        var c = f.Ortam.Istemci(k);
        var hesap = await BankaHesabi(k, c);
        var gecerli = TestIban.Uret();
        var y = await c.PostJson($"/api/v1/bankalar/{hesap}/hareket", new { yon = "GELEN", tutar = 50m, karsiIban = gecerli });
        Assert.True(y.IsSuccessStatusCode, await y.Content.ReadAsStringAsync());
        string hid;
        using (var db = f.Vt.OlusturSistem()) hid = db.BankaHareketleri.AsNoTracking().Single(h => h.TenantId == k.TenantId).Id;
        var (_, mesaj, _) = await Red(await c.PatchAsJsonAsync($"/api/v1/bankalar/{hesap}/hareketler/{hid}",
            new { karsiIban = "TR340006100519786457841326" }), "karsiIban");
        Assert.Equal(GirdiSozlugu.Tr.IbanKontrol, mesaj);
        using var db2 = f.Vt.OlusturSistem();
        Assert.Equal(gecerli, db2.BankaHareketleri.AsNoTracking().Single(h => h.Id == hid).KarsiIban);
    }

    // ── Dekont tarihi K8a (ileri gün yok, en çok 1 yıl geri) · karşı taraf K20 ────────
    [Fact]
    public async Task GB03_Banka_dekont_ileri_gun_ve_cok_eski_tarih_400_karsi_taraf_yalniz_rakam_400()
    {
        var k = YeniKiraci("dekont tarih");
        var c = f.Ortam.Istemci(k);
        var hesap = await BankaHesabi(k, c);
        var yarin = Zaman.YerelBugun().AddDays(2).ToString("yyyy-MM-dd");
        var (_, ileri, _) = await Red(await c.PostJson($"/api/v1/bankalar/{hesap}/hareket", new { yon = "GELEN", tutar = 10m, tarih = yarin }), "tarih");
        Assert.Equal(M(GirdiKodu.TarihIleri), ileri);
        await Red(await c.PostJson($"/api/v1/bankalar/{hesap}/hareket",
            new { yon = "GELEN", tutar = 10m, tarih = Zaman.YerelBugun().AddYears(-2).ToString("yyyy-MM-dd") }), "tarih");
        await Red(await c.PostJson($"/api/v1/bankalar/{hesap}/hareket", new { yon = "GELEN", tutar = 10m, tarih = "3000-01-01" }), "tarih");
        var (_, harf, _) = await Red(await c.PostJson($"/api/v1/bankalar/{hesap}/hareket", new { yon = "GELEN", tutar = 10m, karsiTarafAd = "12345" }), "karsiTarafAd");
        Assert.Equal(M(GirdiKodu.AdHarf), harf);
        Assert.Equal(0, BankaHareketSayisi(k));
    }

    // ── Kur (#8) · K16 + kolon decimal(15,4) ─────────────────────────────────────
    [Fact]
    public async Task GB04_Kur_manuel_sifir_eksi_bes_ondalik_400_gecerli_birebir_yazilir()
    {
        var k = YeniKiraci("kur manuel");
        var c = f.Ortam.Istemci(k);
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.KurSnapshots.AsNoTracking().Count(s => s.TenantId == k.TenantId); }
        var once = Sayi();
        var (_, sifir, _) = await Red(await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 6_800m, usdTry = 0m }), "usdTry");
        Assert.Equal(M(GirdiKodu.SayiSifir), sifir);
        var (_, eksi, _) = await Red(await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 6_800m, gramAltinFiyati = -5m }), "gramAltinFiyati");
        Assert.Equal(M(GirdiKodu.SayiEksi), eksi);
        var (_, ondalik, _) = await Red(await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 6_800.12345m }), "altinHasFiyati");
        Assert.Equal(M(GirdiKodu.SayiOndalik, 4), ondalik);
        await Red(await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 0.5m, usdTry = 1e12m }), "usdTry");
        Assert.Equal(once, Sayi());
        var ok = await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 6_800.1234m, usdTry = 41.5678m });
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        using var db = f.Vt.OlusturSistem();
        var s = db.KurSnapshots.AsNoTracking().Where(x => x.TenantId == k.TenantId).OrderByDescending(x => x.Olusturuldu).First();
        Assert.Equal(6_800.1234m, s.AltinHasFiyati);
        Assert.Equal(41.5678m, s.UsdTry);
    }

    [Fact]
    public async Task GB05_Kur_gecmis_ters_aralik_400_sessiz_bos_liste_YOK()
    {
        var k = YeniKiraci("kur aralik");
        var (_, mesaj, _) = await Red(await f.Ortam.Istemci(k).GetAsync("/api/v1/kur/gecmis?baslangic=2026-09-10&bitis=2026-09-01"), "bitis");
        Assert.Equal(M(GirdiKodu.AralikTers), mesaj);
    }

    // ── Vezne (#6): kesirli adet, kuruş altı tutar, iptal gerekçesi K30 + kolon ─────────
    [Fact]
    public async Task GB06_Vezne_ADET_urunde_kesirli_adet_400_kalemsiz_tutar_kurus_alti_400_fis_ACILMAZ()
    {
        var k = YeniKiraci("vezne adet");
        var c = f.Ortam.Istemci(k);
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.VezneFisleri.AsNoTracking().Count(x => x.TenantId == k.TenantId); }
        var r = await c.PostJson("/api/v1/vezne/fisler", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1.5m, birimFiyatTl = 1000m, iscilikTl = 0m } },
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var mesaj = (await r.Json()).GetProperty("message").GetString()!;
        Assert.Equal($"1. kalem · adet: {M(GirdiKodu.SayiTam)}", mesaj);
        await Red(await c.PostJson("/api/v1/vezne/fisler", new
        {
            fisTuru = "TAHSILAT", cariId = k.CariId, kurSnapshotId = k.KurId, tutarTl = 10.005m,
        }), "tutarTl");
        Assert.Equal(0, Sayi());
    }

    [Fact]
    public async Task GB07_Vezne_iptal_gerekcesi_20_alti_ve_500_ustu_400_SESSIZ_KESME_YOK()
    {
        var k = YeniKiraci("vezne iptal");
        var c = f.Ortam.Istemci(k);
        var y = await c.PostJson("/api/v1/vezne/fisler", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 1000m, iscilikTl = 0m } },
        });
        Assert.True(y.IsSuccessStatusCode, await y.Content.ReadAsStringAsync());
        var id = (await y.Json()).GetProperty("id").GetString()!;
        var (_, kisa, _) = await Red(await c.PostJson($"/api/v1/vezne/fisler/{id}/iptal", new { gerekce = "müşteri vazgeçti" }), "gerekce");
        Assert.Equal(M(GirdiKodu.GerekceKisa, 20, 16), kisa);
        var (_, uzun, _) = await Red(await c.PostJson($"/api/v1/vezne/fisler/{id}/iptal", new { gerekce = new string('ş', 501) }), "gerekce");
        Assert.Equal(M(GirdiKodu.GerekceUzun, 500, 501), uzun);
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(VezneDurum.Acik, db.VezneFisleri.AsNoTracking().Single(x => x.Id == id).Durum);
        var tam = new string('a', 500);
        var ok = await c.PostJson($"/api/v1/vezne/fisler/{id}/iptal", new { gerekce = tam });
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        using var db2 = f.Vt.OlusturSistem();
        Assert.Equal(tam, db2.VezneFisleri.AsNoTracking().Single(x => x.Id == id).IptalGerekce);
    }

    // ── Fatura (#9) · ileri tarih, sessiz kırpma, kuruş altı ────────────────────────
    [Fact]
    public async Task GB08_Fatura_ileri_tarih_POST_ve_taslak_PATCH_400_kalem_aciklama_301_KIRPILMAZ_400()
    {
        var k = YeniKiraci("fatura tarih");
        var c = f.Ortam.Istemci(k);
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.Faturalar.AsNoTracking().Count(x => x.TenantId == k.TenantId); }
        var ileri = Zaman.YerelBugun().AddDays(3).ToString("yyyy-MM-dd");
        var (_, mesaj, _) = await Red(await c.PostJson("/api/v1/faturalar", new
        {
            cariId = k.CariId, tur = "SATIS", toplam = 1200m, kdvOran = 0.20m, tarih = ileri,
        }), "tarih");
        Assert.Equal(M(GirdiKodu.TarihIleri), mesaj);
        await Red(await c.PostJson("/api/v1/faturalar", new { cariId = k.CariId, tur = "SATIS", toplam = 12.345m, kdvOran = 0.20m }), "toplam");
        Assert.Equal(0, Sayi());

        var y = await c.PostJson("/api/v1/faturalar", new
        {
            cariId = k.CariId, tur = "SATIS", toplam = 1200m, kdvOran = 0.20m,
            kalemler = new[] { new { aciklama = "Kalem", miktar = 1m, birimFiyat = 1200m } },
        });
        Assert.True(y.IsSuccessStatusCode, await y.Content.ReadAsStringAsync());
        var fid = (await y.Json()).GetProperty("id").GetString()!;
        DateTime once;
        string kid, aciklamaOnce;
        using (var db = f.Vt.OlusturSistem())
        {
            once = db.Faturalar.AsNoTracking().Single(x => x.Id == fid).Tarih;
            var kl = db.FaturaKalemleri.AsNoTracking().Single(x => x.FaturaId == fid);
            kid = kl.Id; aciklamaOnce = kl.Aciklama;
        }
        await Red(await c.PatchAsJsonAsync($"/api/v1/faturalar/{fid}", new { tarih = ileri }), "tarih");
        await Red(await c.PatchAsJsonAsync($"/api/v1/faturalar/{fid}", new { tarih = "3000-01-01" }), "tarih");
        var (_, uzun, _) = await Red(await c.PatchAsJsonAsync($"/api/v1/faturalar/{fid}/kalemler/{kid}", new { aciklama = new string('k', 301) }), "aciklama");
        Assert.Contains("300", uzun);
        using var db2 = f.Vt.OlusturSistem();
        Assert.Equal(once, db2.Faturalar.AsNoTracking().Single(x => x.Id == fid).Tarih);
        Assert.Equal(aciklamaOnce, db2.FaturaKalemleri.AsNoTracking().Single(x => x.Id == kid).Aciklama);
    }

    // ── Kasa · K30 gerekçe + oluşturmada en az 2 ─────────────────────────────────
    [Fact]
    public async Task GB09_Kasa_farkli_gun_kapat_kisa_gerekce_400_kasa_adi_tek_harf_400()
    {
        var k = YeniKiraci("kasa gerekce");
        var c = f.Ortam.Istemci(k);
        var (_, mesaj, _) = await Red(await c.PostJson($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            new { sayilanNakit = 100m, aciklama = "eksik" }), "aciklama");
        Assert.Equal(M(GirdiKodu.GerekceKisa, 20, 5), mesaj);
        using (var db = f.Vt.OlusturSistem())
            Assert.False(db.GunKapanislari.AsNoTracking().Any(g => g.TenantId == k.TenantId));
        await Red(await c.PostJson("/api/v1/kasalar", new { ad = "A", subeId = k.SubeId }), "ad");
    }

    // ── Emanet (#5) · K10 taş < brüt, 3 ondalık ────────────────────────────────────
    [Fact]
    public async Task GB10_Emanet_tas_brutten_buyuk_ve_dort_ondalik_gram_400_dogru_sebep_satir_YOK()
    {
        var k = YeniKiraci("emanet tas");
        var c = f.Ortam.Istemci(k);
        var (_, tas, _) = await Red(await c.PostJson("/api/v1/emanet/giris", new
        {
            cariId = k.CariId, ayarId = "K22", gramAgirlik = 10m, tasAgirlik = 10m, kurSnapshotId = k.KurId,
        }), "tasAgirlik");
        Assert.Equal(M(GirdiKodu.TasBrut), tas);
        await Red(await c.PostJson("/api/v1/emanet/giris", new
        {
            cariId = k.CariId, ayarId = "K22", gramAgirlik = 10.0005m, kurSnapshotId = k.KurId,
        }), "gramAgirlik");
        using var db = f.Vt.OlusturSistem();
        Assert.False(db.Hareketler.AsNoTracking().Any(h => h.TenantId == k.TenantId && h.Tur == "EMANET_GIRIS"));
    }

    // ── POS tahsilat listesi · K32 beyaz liste + K8e ─────────────────────────────
    [Theory]
    [InlineData("durum=XYZ", "durum")]
    [InlineData("bas=2026-09-10&bit=2026-09-01", "bit")]
    [InlineData("bas=abc", "bas")]
    public async Task GB11_POS_tahsilat_listesi_bilinmeyen_durum_ve_bozuk_aralik_400_sessiz_suzgecsiz_YOK(string sorgu, string alan)
    {
        var k = YeniKiraci("pos liste");
        await Red(await f.Ortam.Istemci(k).GetAsync($"/api/v1/pos/tahsilatlar?{sorgu}"), alan);
    }

    // ── Gider pusulası listesi · bitiş GÜNÜ dahil (§ A acil hata) ─────────────────────
    [Fact]
    public async Task GB12_Gider_pusulasi_listesi_bitis_gunundeki_pusulayi_GOSTERIR_ters_aralik_400()
    {
        var k = YeniKiraci("pusula bitis");
        using (var rdb = f.Vt.OlusturSistem()) IslemBantFikstur.ReferansKur(rdb, k);   // ISLEM-BANT: fikstür 2.800 TL/has g bant içinde
        using (var db = f.Vt.Olustur(k))
        {
            await new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance).HurdaAlis(new MSatis.HurdaAlisDto
            {
                CariId = k.CariId, AyarId = "K22", GramAgirlik = 1m,
                BirimFiyatTlHasGram = 2_800m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
                Satici = TestKurulum.HurdaSatici(SaticiTipi.EsnafMuaf),
            }, k.Islem);
        }
        DateTime teslim;
        using (var db = f.Vt.OlusturSistem()) teslim = db.GiderPusulalari.AsNoTracking().Single(g => g.TenantId == k.TenantId).TeslimTarihi;
        var gun = teslim.ToString("yyyy-MM-dd");
        var c = f.Ortam.Istemci(k);
        var r = await c.GetAsync($"/api/v1/gider-pusulalari?baslangic={gun}&bitis={gun}");
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.IsSuccessStatusCode, govde);
        Assert.Contains("\"pusulaNo\"", govde);
        await Red(await c.GetAsync($"/api/v1/gider-pusulalari?baslangic={gun}&bitis={teslim.AddDays(-3):yyyy-MM-dd}"), "bitis");
    }

    // ── Stopaj · K9 yuvarlamadan ÖNCE (0,004 → 0,00 TL'lik satır doğmaz) ───────────────
    [Fact]
    public async Task GB13_Stopaj_odemesi_kurus_alti_400_sozluk_metni_satir_YOK()
    {
        var k = YeniKiraci("stopaj");
        var r = await f.Ortam.Istemci(k).PostJson("/api/v1/stopaj/odemeler", new { donem = "2026-08", kasaId = k.KasaId, tutar = 0.004m });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var mj = (await r.Json()).GetProperty("message");
        var mesaj = mj.ValueKind == JsonValueKind.Array ? string.Join(" ", mj.EnumerateArray().Select(x => x.GetString())) : mj.GetString()!;
        Assert.Contains(M(GirdiKodu.SayiOndalik, 2), mesaj);   // controller yolunda StopajOdemeDto [Tutar] (ModelState dizisi)
        using var db = f.Vt.OlusturSistem();
        Assert.False(db.StopajOdemeleri.AsNoTracking().Any(o => o.TenantId == k.TenantId));
    }

    // ── Izgara tercihi: A kullanıcısında kalıcı, B'de varsayılan, bilinmeyen ekran 400 ──────
    [Fact]
    public async Task GB14_Para_ekrani_izgara_tercihi_A_da_kalici_B_de_yok_bilinmeyen_ekran_400()
    {
        var k = YeniKiraci("izgara tercih");
        const string anahtar = "grid:vezne:vz-liste-izgara";
        const string deger = """{"s":1,"k":["toplam","durum","tur","cari","hazirlayan","kalem"],"g":["hazirlayan"],"o":{"a":"toplam","y":"azalan"}}""";
        var a = f.Ortam.Istemci(k);
        var yaz = await a.PatchAsJsonAsync("/api/v1/tercihler/benim", new { tercihler = new Dictionary<string, string?> { [anahtar] = deger } });
        Assert.Equal(HttpStatusCode.OK, yaz.StatusCode);
        var oku = await (await a.GetAsync("/api/v1/tercihler/benim")).Json();
        Assert.Equal(deger, oku.GetProperty("tercihler").GetProperty(anahtar).GetString());
        var b = f.Ortam.Istemci("goca-b-kullanici", k.TenantId, k.SubeId, "TEZGAHTAR");
        var bOku = await (await b.GetAsync("/api/v1/tercihler/benim")).Json();
        Assert.False(bOku.GetProperty("tercihler").TryGetProperty(anahtar, out _));
        var kotu = await a.PatchAsJsonAsync("/api/v1/tercihler/benim", new { tercihler = new Dictionary<string, string?> { ["grid:veznex:liste"] = deger } });
        Assert.Equal(HttpStatusCode.BadRequest, kotu.StatusCode);
    }
}

/// <summary>bUnit: ekranın alan altı metni ve sessiz silmenin kapanması.</summary>
public sealed class GocAEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public GocAEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Sahte(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, yol, govde));
            var g = yol switch
            {
                var y when y.EndsWith("/vezne/kuyruk") => """
                    {"durum":"VEZNEDE","limit":200,"azamiLimit":200,"toplam":0,"dahaFazlasiVar":false,"vezneZorunlu":false,
                     "gerekceAsgari":20,"subeId":"sb1","fisler":[]}
                    """,
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez kasa","paraBirimi":"TRY"}]""",
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz"}]""",
                var y when y.EndsWith("/urunler") => """[{"id":"u-1","ad":"Bilezik","birim":"ADET"},{"id":"u-2","ad":"Has zincir","birim":"GRAM"}]""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":null,"usdTry":40,"eurTry":null,"gbpTry":null,"kaynak":"MANUAL","olusturuldu":"2026-10-08T08:00:00Z","ayarFiyatlari":[]}""",
                var y when y.Contains("/kur/gecmis") => "[]",
                var y when y.Contains("/kur/pariteler") => """{"pariteler":[]}""",
                var y when y.Contains("/tercihler/benim") => """{"tercihler":{}}""",
                var y when y.Contains("/auth/ben") =>
                    """{"id":"u1","ad":"Test","soyad":"Kullanici","email":"t@t.local","rol":"SAHIP","subeId":"sb1"}""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Vezne> Vezne()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));
        var e = Render<Kuyumcu.Web.Pages.Vezne>();
        e.WaitForElement("#vz-yeni-ac");
        e.Find("#vz-yeni-ac").Click();
        e.WaitForElement("#vz-f-urun-0");
        return e;
    }

    private static void Sec<T>(IRenderedComponent<T> e, string id, string deger) where T : Microsoft.AspNetCore.Components.IComponent
    {
        e.Find($"#{id}").Click();
        e.Find($"#{id}-liste li[data-deger='{deger}']").Click();
    }

    [Fact]
    public void GE01_Vezne_eksik_ikinci_kalem_SESSIZCE_ATILMAZ_satir_numarasiyla_soylenir_POST_YOK()
    {
        var e = Vezne();
        Sec(e, "vz-f-urun-0", "u-1");
        e.Find("#vz-f-fiyat-0").Input("1500");
        e.Find("#vz-f-kalem-ekle").Click();
        Sec(e, "vz-f-urun-1", "u-1");          // ürün seçildi, fiyat BOŞ
        e.Find("#vz-f-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(Q105Sozlugu.Tr.VezneKalemSayiHatasi, e.Markup));
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/vezne/fisler"));
    }

    [Fact]
    public void GE02_Vezne_ADET_urunde_adet_ortak_Adet_GRAM_urunde_ortak_Gram_kesirli_adet_alan_alti_hata()
    {
        var e = Vezne();
        Sec(e, "vz-f-urun-0", "u-1");
        Assert.Equal("1", e.Find("#vz-f-adet-0").GetAttribute("data-tam"));       // ADET → tam sayı maskesi
        e.Find("#vz-f-adet-0").Input("1,5");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrWhiteSpace(e.Find("#vz-f-adet-0-hata").TextContent)));
        Sec(e, "vz-f-urun-0", "u-2");
        e.WaitForAssertion(() => Assert.Null(e.Find("#vz-f-adet-0").GetAttribute("data-tam")));   // GRAM → ondalıklı miktar
    }

    [Fact]
    public void GE03_Vezne_kalemsiz_tutar_ortak_Para_eksi_yazilamaz_uc_ondalik_alan_alti_sozluk()
    {
        var e = Vezne();
        Sec(e, "vz-f-tur", "TAHSILAT");
        e.WaitForElement("#vz-f-tutar");
        Assert.Equal("sayi", e.Find("#vz-f-tutar").GetAttribute("data-maske"));
        Assert.Null(e.Find("#vz-f-tutar").GetAttribute("data-eksi"));
        e.Find("#vz-f-tutar").Input("10,005");
        e.WaitForAssertion(() => Assert.Equal(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.SayiOndalik, 2), "tr"),
            e.Find("#vz-f-tutar-hata").TextContent.Trim()));
    }

    [Fact]
    public void GE04_Kur_manuel_USD_sifir_alan_alti_hata_ve_Kaydet_kilitli()
    {
        var e = Render<Kuyumcu.Web.Pages.Kur>();
        e.WaitForElement("#kur-manuel-ac");
        e.Find("#kur-manuel-ac").Click();
        e.WaitForElement("#m-usd");
        e.Find("#m-usd").Input("0");
        e.WaitForAssertion(() =>
        {
            Assert.Equal(GirdiSozlugu.Tr.SayiSifir, e.Find("#alan-hata-m-usd").TextContent.Trim());
            Assert.True(e.Find("#m-kaydet").HasAttribute("disabled"));
        });
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/kur/manuel"));
    }
}
