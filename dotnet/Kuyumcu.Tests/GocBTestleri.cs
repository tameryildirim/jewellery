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
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MCari = Kuyumcu.Api.Modules.Cari;
using MEvrak = Kuyumcu.Api.Modules.Evrak;
using MMasak = Kuyumcu.Api.Modules.Masak;

namespace Kuyumcu.Tests;

// olumsuz-fikstür: dosya — bu dosya kuralların SINIRLARINI sınar; geçersiz değerler (gelecek doğum, 3000 yılı,
// "1" ad soyad, ileri tarihli MASAK bildirimi, keşide > vade, harfli telefon, bozuk e-posta …) bilinçlidir.
// ═════════════════════════════════════════════════════════════════════════════
// PAKET GOC-B — kimlik ve uyum ekranlarının ortak girdi + ortak ızgara göçü.
// Kaynak: dokumanlar/09-uygulama-kayitlari/GIRDI-KURALLARI-ENVANTER-2026-10-08.md (E) Faz 2 GOC-B:
// doğum tarihi (#2), MASAK uzunlukları (#3), MASAK bildirim ileri tarihi, evrak borçlu kimlik PATCH kaybı,
// cari telefon/e-posta, keşide ≤ vade. Her açık için sunucu 400 (+ alan + SATIR YAZILMAZ) ve mesaj ekranın
// alan altı metniyle AYNI sözlük anahtarından (GirdiSozlugu).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Saf: sözleşme ve kural kapısı (veritabanı yok).</summary>
public sealed class GocBSafTestleri
{
    private static string M(GirdiKodu kod, decimal? s1 = null, decimal? s2 = null, DateOnly? gun = null) =>
        GirdiSozlugu.Mesaj(new GirdiHatasi(kod, s1, s2, gun), "tr");

    private static T Alan<T>(Type t, string ozellik) where T : Attribute =>
        t.GetProperty(ozellik)!.GetCustomAttributes(true).OfType<T>().Single();

    [Fact]
    public void GBS01_Izgara_bilinen_ekranlar_bes_uyum_ekranini_icerir()
    {
        foreach (var e in new[] { "uyum", "evraklar", "evrak-takvimi", "vade-takibi", "bildirimler" })
            Assert.True(IzgaraTercihKurali.AnahtarGecerli($"grid:{e}:liste"), e);
        Assert.False(IzgaraTercihKurali.AnahtarGecerli("grid:uyumx:liste"));
    }

    [Fact]
    public void GBS02_Ekran_siniri_DTO_siniriyla_ayni_sabit()
    {
        Assert.Equal(AlanSinirlari.CariAd, Alan<MaxLengthAttribute>(typeof(MCari.CariOlusturDto), "Ad").Length);
        Assert.Equal(AlanSinirlari.CariAd, Alan<MaxLengthAttribute>(typeof(MCari.CariGuncelleDto), "Ad").Length);
        Assert.Equal(AlanSinirlari.CariUnvan, Alan<MaxLengthAttribute>(typeof(MCari.CariOlusturDto), "Unvan").Length);
        Assert.Equal(AlanSinirlari.Eposta, Alan<MaxLengthAttribute>(typeof(MCari.CariOlusturDto), "Email").Length);
        Assert.Equal(AlanSinirlari.MasakBelgeNo, Alan<MaxLengthAttribute>(typeof(MMasak.MasakBilgiDto), "BelgeNo").Length);
        Assert.Equal(AlanSinirlari.MasakIslemAmaci, Alan<MaxLengthAttribute>(typeof(MMasak.MasakBilgiDto), "IslemAmaci").Length);
        Assert.Equal(AlanSinirlari.MasakAdSoyad, Alan<MaxLengthAttribute>(typeof(MMasak.MasakBilgiDto), "AdSoyad").Length);
        Assert.Equal(AlanSinirlari.SupheliKapatma, Alan<MaxLengthAttribute>(typeof(MMasak.KapatDto), "Gerekce").Length);
        Assert.Equal(AlanSinirlari.RiskElleGerekce, Alan<MaxLengthAttribute>(typeof(MMasak.RiskElleDto), "Gerekce").Length);
        Assert.Equal(AlanSinirlari.EvrakBorcluAd, Alan<StringLengthAttribute>(typeof(MEvrak.EvrakGirisDto), "BorcluAd").MaximumLength);
    }

    [Fact]
    public void GBS03_DTOlar_ortak_Core_ozniteliklerini_tasir()
    {
        Assert.Equal(TarihTuru.KisiDogum, Alan<TarihPenceresiAttribute>(typeof(MCari.CariOlusturDto), "DogumTarihi").Tur);
        Assert.Equal(TarihTuru.KisiDogum, Alan<TarihPenceresiAttribute>(typeof(MCari.CariGuncelleDto), "DogumTarihi").Tur);
        Assert.Equal(TarihTuru.KisiDogum, Alan<TarihPenceresiAttribute>(typeof(MMasak.MasakBilgiDto), "DogumTarihi").Tur);
        Assert.Equal(TarihTuru.Keside, Alan<TarihPenceresiAttribute>(typeof(MEvrak.EvrakGirisDto), "KesideTarihi").Tur);
        Assert.NotNull(Alan<EpostaAttribute>(typeof(MCari.CariOlusturDto), "Email"));
        Assert.NotNull(Alan<EpostaAttribute>(typeof(MCari.CariGuncelleDto), "Email"));
        Assert.Empty(typeof(MCari.CariOlusturDto).GetProperty("Email")!.GetCustomAttributes<EmailAddressAttribute>());
        Assert.NotNull(Alan<AdAttribute>(typeof(MCari.CariOlusturDto), "Ad"));
        Assert.NotNull(Alan<AdAttribute>(typeof(MMasak.MasakBilgiDto), "AdSoyad"));
        Assert.NotNull(Alan<TutarAttribute>(typeof(MMasak.SupheliBildirimDto), "IslemTutariTl"));
        Assert.NotNull(Alan<GerekceAttribute>(typeof(MMasak.KapatDto), "Gerekce"));
        // Tur 2: yabancı borçlu (pasaport) — rakamsa K4/K5, harfliyse K7; DTO uzunluğu kuralla eşit (MinimumLength YOK).
        Assert.NotNull(Alan<TcknVknVeyaPasaportAttribute>(typeof(MEvrak.EvrakGirisDto), "BorcluKimlikNo"));
        Assert.NotNull(Alan<TcknVknVeyaPasaportAttribute>(typeof(MEvrak.EvrakDuzeltDto), "BorcluKimlikNo"));
        Assert.Equal(0, Alan<StringLengthAttribute>(typeof(MEvrak.EvrakGirisDto), "BorcluKimlikNo").MinimumLength);
        // Tur 2 (doğrulayıcı KRİTİK): evrak tutarı eski Range(…, 999_999_999) yerine Core K9 (tek tavan, doğru ileti).
        Assert.NotNull(Alan<TutarAttribute>(typeof(MEvrak.EvrakGirisDto), "Tutar"));
        Assert.Empty(typeof(MEvrak.EvrakGirisDto).GetProperty("Tutar")!.GetCustomAttributes<RangeAttribute>());
    }

    private static BadRequestException Firlat(Action a) => Assert.Throws<BadRequestException>(a);

    [Fact]
    public void GBS04_MASAK_kimlik_bilgisi_dogum_ad_soyad_pasaport_uzunluk_kapisi()
    {
        var bugun = Zaman.YerelBugun();
        MMasak.MasakBilgiDto B(Action<MMasak.MasakBilgiDto>? d = null)
        {
            var b = new MMasak.MasakBilgiDto { BelgeTuru = "TC_KIMLIK", BelgeNo = "10000000146", AdSoyad = "Ali Veli", DogumTarihi = new DateOnly(1980, 5, 1) };
            d?.Invoke(b);
            return b;
        }
        MMasak.MasakKapisi.KimlikBilgisiDenetle(B());   // geçerli: atmaz

        var e = Firlat(() => MMasak.MasakKapisi.KimlikBilgisiDenetle(B(b => b.DogumTarihi = bugun.AddDays(1))));
        Assert.Equal(M(GirdiKodu.DogumGelecek), e.Message);
        // Tur 2 (doğrulayıcı ORTA): 18 yaş altı ENGEL DEĞİL (TMK md.11 yasaklamaz; TEYİTSİZ → paket öncesi davranış).
        MMasak.MasakKapisi.KimlikBilgisiDenetle(B(b => b.DogumTarihi = bugun.AddYears(-17)));
        e = Firlat(() => MMasak.MasakKapisi.KimlikBilgisiDenetle(B(b => b.DogumTarihi = new DateOnly(1899, 12, 31))));
        Assert.Equal(M(GirdiKodu.DogumCokEski, 120), e.Message);
        e = Firlat(() => MMasak.MasakKapisi.KimlikBilgisiDenetle(B(b => b.AdSoyad = "1")));
        Assert.Equal(M(GirdiKodu.AdKisa, 2), e.Message);
        e = Firlat(() => MMasak.MasakKapisi.KimlikBilgisiDenetle(B(b => b.AdSoyad = "12345")));
        Assert.Equal(M(GirdiKodu.AdHarf), e.Message);
        e = Firlat(() => MMasak.MasakKapisi.KimlikBilgisiDenetle(B(b => { b.BelgeTuru = "PASAPORT"; b.BelgeNo = "AB 12"; })));
        Assert.Equal(M(GirdiKodu.PasaportBicim, 5, AlanSinirlari.Pasaport), e.Message);
        MMasak.MasakKapisi.KimlikBilgisiDenetle(B(b => { b.BelgeTuru = "PASAPORT"; b.BelgeNo = "u12345678"; }));
        e = Firlat(() => MMasak.MasakKapisi.KimlikBilgisiDenetle(B(b => b.IslemAmaci = new string('a', AlanSinirlari.MasakIslemAmaci + 1))));
        Assert.Equal(M(GirdiKodu.AdUzun, AlanSinirlari.MasakIslemAmaci), e.Message);
        MMasak.MasakKapisi.KimlikBilgisiDenetle(B(b => b.IslemAmaci = new string('a', AlanSinirlari.MasakIslemAmaci)));
    }

    [Fact]
    public void GBS04b_MASAK_kapisi_servis_yolunda_da_bicimi_uygular_DTO_dogrulamasi_olmadan()
    {
        // Servis yolu (satış/takas/hurda/dekont servisleri MasakKapisi.Dogrula'yı doğrudan çağırır): DataAnnotations
        // KOŞMAZ — kural kapının içinde olmalı. Eksiksiz dolu gövde; yalnız biçim bozuk.
        MMasak.MasakBilgiDto B(string adSoyad, DateOnly? dogum) => new()
        {
            BelgeTuru = "TC_KIMLIK", BelgeNo = "10000000146", AdSoyad = adSoyad, DogumTarihi = dogum,
            IslemAmaci = "Takı alımı", Aciklama = new string('a', 400), FonKaynagi = "Maaş birikimi", EkBelgeReferansi = "Banka dekontu 123",
        };
        var kapi = MMasak.MasakKapisi.Varsayilan();
        kapi.Dogrula(2_000_000m, B("Ayşe Demir", new DateOnly(1980, 1, 1)));   // geçerli: atmaz
        Assert.Equal(M(GirdiKodu.AdHarf), Firlat(() => kapi.Dogrula(2_000_000m, B("12345", null))).Message);
        Assert.Equal(M(GirdiKodu.DogumGelecek), Firlat(() => kapi.Dogrula(2_000_000m, B("Ayşe Demir", new DateOnly(3000, 1, 1)))).Message);
    }

    [Fact]
    public void GBS05_Evrak_vade_on_yil_ve_keside_vadeden_sonra_olamaz()
    {
        var bugun = new DateOnly(2026, 10, 8);
        MEvrak.EvrakServisi.TarihleriDenetle(bugun.AddDays(30), bugun.AddDays(-5), bugun);
        MEvrak.EvrakServisi.TarihleriDenetle(bugun.AddDays(30), bugun.AddDays(30), bugun);        // keşide = vade geçerli
        MEvrak.EvrakServisi.TarihleriDenetle(bugun.AddYears(10), null, bugun);                     // tam sınır
        var e = Firlat(() => MEvrak.EvrakServisi.TarihleriDenetle(bugun.AddYears(10).AddDays(1), null, bugun));
        Assert.Equal(M(GirdiKodu.TarihGec, gun: bugun.AddYears(10)), e.Message);
        e = Firlat(() => MEvrak.EvrakServisi.TarihleriDenetle(bugun.AddDays(30), bugun.AddDays(31), bugun));
        Assert.Equal(M(GirdiKodu.TarihGec, gun: bugun.AddDays(30)), e.Message);
        e = Firlat(() => MEvrak.EvrakServisi.TarihleriDenetle(bugun.AddDays(30), new DateOnly(1, 1, 1), bugun));
        Assert.Equal(M(GirdiKodu.TarihErken, gun: new DateOnly(1900, 1, 1)), e.Message);
    }

    [Fact]
    public void GBS06_Cari_dogum_kapisi_ve_tur_beyaz_listesi()
    {
        MCari.CariServisi.DogumDenetle(null);
        MCari.CariServisi.DogumDenetle(Zaman.YerelBugun().AddYears(-30));
        MCari.CariServisi.DogumDenetle(Zaman.YerelBugun().AddYears(-10));   // tur 2: 18 altı engel değil
        MCari.CariServisi.DogumDenetle(Zaman.YerelBugun());                 // bugün doğan: sınır geçer
        Assert.Equal(M(GirdiKodu.DogumGelecek), Firlat(() => MCari.CariServisi.DogumDenetle(Zaman.YerelBugun().AddDays(1))).Message);
        Assert.Equal(M(GirdiKodu.DogumGelecek), Firlat(() => MCari.CariServisi.DogumDenetle(new DateOnly(3000, 1, 1))).Message);
        Assert.Equal(M(GirdiKodu.DogumCokEski, 120), Firlat(() => MCari.CariServisi.DogumDenetle(new DateOnly(1899, 1, 1))).Message);
        Assert.Equal(["MUSTERI", "TEDARIKCI", "HER_IKISI", "ATOLYE", "SARRAF"], MCari.CariServisi.CariTurleri);
    }

    [Fact]
    public void GBS07_MasakFormu_kilidi_ortak_kuralin_aynasi()
    {
        var m = new Kuyumcu.Web.Bilesenler.MasakFormu.MasakBilgi { BelgeNo = "10000000146", AdSoyad = "Ali Veli" };
        Assert.Null(Kuyumcu.Web.Bilesenler.MasakFormu.KimlikKuralHatasi(m));
        m.AdSoyad = "1";
        Assert.Equal(GirdiKodu.AdKisa, Kuyumcu.Web.Bilesenler.MasakFormu.KimlikKuralHatasi(m)!.Value.Kod);
        m.AdSoyad = "Ali Veli";
        m.DogumTarihi = DateOnly.FromDateTime(DateTime.Today).AddYears(-10);
        Assert.Null(Kuyumcu.Web.Bilesenler.MasakFormu.KimlikKuralHatasi(m));   // tur 2: 18 altı kilit DEĞİL
        m.DogumTarihi = DateOnly.FromDateTime(DateTime.Today).AddYears(1);
        Assert.Equal(GirdiKodu.DogumGelecek, Kuyumcu.Web.Bilesenler.MasakFormu.KimlikKuralHatasi(m)!.Value.Kod);
        m.DogumTarihi = null;
        m.IslemAmaci = new string('x', AlanSinirlari.MasakIslemAmaci + 1);
        Assert.Equal(GirdiKodu.AdUzun, Kuyumcu.Web.Bilesenler.MasakFormu.KimlikKuralHatasi(m)!.Value.Kod);
    }
}

/// <summary>HTTP: her açığın sunucu 400'ü (gerçek MySQL + SQL kanıtı).</summary>
[Collection("http")]
public sealed class GocBSunucuTestleri(HttpFikstur f)
{
    private static string M(GirdiKodu kod, decimal? s1 = null, decimal? s2 = null, DateOnly? gun = null) =>
        GirdiSozlugu.Mesaj(new GirdiHatasi(kod, s1, s2, gun), "tr");

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETGOC-B-TEST " + ad);
    }

    /// <summary>400 + alan işareti (DTO yolunda "masak.adSoyad" gibi önekli olabilir) + ilk ileti.</summary>
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

    private int CariSayisi(Kiraci k) { using var db = f.Vt.OlusturSistem(); return db.Cariler.AsNoTracking().Count(c => c.TenantId == k.TenantId); }

    // ── #2 Doğum tarihi + cari ad/telefon/e-posta (K8b, K19, K2, K3) ───────────────────
    [Fact]
    public async Task GBH01_Cari_POST_dogum_3000_1899_17yas_ad_telefon_eposta_400_satir_YOK_gecerli_kayit()
    {
        var k = YeniKiraci("cari post");
        var c = f.Ortam.Istemci(k);
        var once = CariSayisi(k);
        var bugun = Zaman.YerelBugun();
        Assert.Equal(M(GirdiKodu.DogumGelecek), await Red(await c.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "Ayşe Kaya", dogumTarihi = "3000-01-01" }), "dogumTarihi"));
        Assert.Equal(M(GirdiKodu.DogumCokEski, 120), await Red(await c.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "Ayşe Kaya", dogumTarihi = "1899-12-31" }), "dogumTarihi"));
        Assert.Equal(M(GirdiKodu.AdHarf), await Red(await c.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "12345" }), "ad"));
        Assert.Equal(M(GirdiKodu.AdKisa, 2), await Red(await c.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "A" }), "ad"));
        await Red(await c.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "Ayşe Kaya", telefon = "0532abc1234" }), "telefon");
        Assert.Equal(M(GirdiKodu.EpostaBicim), await Red(await c.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "Ayşe Kaya", email = "a@b" }), "email"));
        await Red(await c.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = new string('a', AlanSinirlari.CariAd + 1) }), "ad");
        Assert.Equal(once, CariSayisi(k));

        var ok = await c.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "Ayşe Kaya", telefon = "0532 411 22 33", email = "ayse@ornek.com", dogumTarihi = "1985-04-12" });
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        var id = (await ok.Json()).GetProperty("id").GetString()!;
        using var db = f.Vt.OlusturSistem();
        var cari = db.Cariler.AsNoTracking().Single(x => x.Id == id);
        Assert.Equal(new DateOnly(1985, 4, 12), cari.DogumTarihi);
        Assert.Equal(once + 1, CariSayisi(k));

        // Tur 2 (doğrulayıcı ORTA): 17 yaşındaki müşteri KAYDEDİLİR (18 altı engel değil, ekranda yalnız uyarı).
        var genc = bugun.AddYears(-17);
        var ok17 = await c.PostJson("/api/v1/cari", new { tur = "MUSTERI", ad = "Can Kaya", dogumTarihi = genc.ToString("yyyy-MM-dd") });
        Assert.True(ok17.IsSuccessStatusCode, await ok17.Content.ReadAsStringAsync());
        var id17 = (await ok17.Json()).GetProperty("id").GetString()!;
        Assert.Equal(genc, db.Cariler.AsNoTracking().Single(x => x.Id == id17).DogumTarihi);
    }

    [Fact]
    public async Task GBH02_Cari_PATCH_gelecek_dogum_400_kayit_DEGISMEZ_tur_suzgeci_bilinmeyen_400()
    {
        var k = YeniKiraci("cari patch");
        var c = f.Ortam.Istemci(k);
        Assert.Equal(M(GirdiKodu.DogumGelecek), await Red(await c.PatchJson($"/api/v1/cari/{k.CariId}", new { dogumTarihi = "3000-01-01" }), "dogumTarihi"));
        Assert.Equal(M(GirdiKodu.EpostaBicim), await Red(await c.PatchJson($"/api/v1/cari/{k.CariId}", new { email = "a@b" }), "email"));
        using (var db = f.Vt.OlusturSistem())
            Assert.Null(db.Cariler.AsNoTracking().Single(x => x.Id == k.CariId).DogumTarihi);
        var ok = await c.PatchJson($"/api/v1/cari/{k.CariId}", new { dogumTarihi = "1980-01-02" });
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(new DateOnly(1980, 1, 2), db.Cariler.AsNoTracking().Single(x => x.Id == k.CariId).DogumTarihi);
        await Red(await c.GetAsync("/api/v1/cari?tur=XYZ"), "tur");
        Assert.True((await c.GetAsync("/api/v1/cari?tur=MUSTERI")).IsSuccessStatusCode);
    }

    // ── Evrak: keşide ≤ vade · vade ≤ 10 yıl · borçlu kimlik PATCH · tutar ondalık ──────────
    private object EvrakGovde(Kiraci k, string no, Action<Dictionary<string, object?>>? d = null)
    {
        var g = new Dictionary<string, object?>
        {
            ["evrakTuru"] = "CEK", ["tutar"] = 10_000m, ["vadeGunu"] = Zaman.YerelBugun().AddDays(20).ToString("yyyy-MM-dd"),
            ["evrakNo"] = no, ["bankaAdi"] = "Ziraat", ["kaynakCariId"] = k.CariId, ["kurSnapshotId"] = k.KurId,
        };
        d?.Invoke(g);
        return g;
    }

    private int EvrakSayisi(Kiraci k) { using var db = f.Vt.OlusturSistem(); return db.KiymetliEvraklar.AsNoTracking().Count(e => e.TenantId == k.TenantId); }

    [Fact]
    public async Task GBH03_Evrak_keside_vadeden_sonra_vade_on_yil_ustu_tutar_ondalik_borclu_ad_400_satir_YOK()
    {
        var k = YeniKiraci("evrak post");
        var c = f.Ortam.Istemci(k);
        var vade = Zaman.YerelBugun().AddDays(20);
        Assert.Equal(M(GirdiKodu.TarihGec, gun: vade), await Red(await c.PostJson("/api/v1/evraklar",
            EvrakGovde(k, "GB-1", g => g["kesideTarihi"] = vade.AddDays(1).ToString("yyyy-MM-dd"))), "kesideTarihi"));
        await Red(await c.PostJson("/api/v1/evraklar",
            EvrakGovde(k, "GB-2", g => g["vadeGunu"] = Zaman.YerelBugun().AddYears(11).ToString("yyyy-MM-dd"))), "vadeGunu");
        Assert.Equal(M(GirdiKodu.SayiOndalik, 2), await Red(await c.PostJson("/api/v1/evraklar", EvrakGovde(k, "GB-3", g => g["tutar"] = 10.005m)), "tutar"));
        await Red(await c.PostJson("/api/v1/evraklar", EvrakGovde(k, "GB-4", g => g["borcluAd"] = "1")), "borcluAd");
        await Red(await c.PostJson("/api/v1/evraklar", EvrakGovde(k, "GB-5", g => g["borcluKimlikNo"] = "12345678901234")), "borcluKimlikNo");
        Assert.Equal(0, EvrakSayisi(k));
        var ok = await c.PostJson("/api/v1/evraklar", EvrakGovde(k, "  GB-6  ", g => g["kesideTarihi"] = vade.ToString("yyyy-MM-dd")));
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        using var db = f.Vt.OlusturSistem();
        Assert.Equal("GB-6", db.KiymetliEvraklar.AsNoTracking().Single(e => e.TenantId == k.TenantId).EvrakNo);   // kırpıldı
    }

    [Fact]
    public async Task GBH04_Evrak_PATCH_borclu_kimlik_KAYBOLMAZ_keside_vadeyi_gecemez_liste_suzgecleri_400()
    {
        var k = YeniKiraci("evrak patch");
        var c = f.Ortam.Istemci(k);
        var ok = await c.PostJson("/api/v1/evraklar", EvrakGovde(k, "GBP-1"));
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        string id;
        using (var db = f.Vt.OlusturSistem()) id = db.KiymetliEvraklar.AsNoTracking().Single(e => e.TenantId == k.TenantId).Id;

        var p = await c.PatchJson($"/api/v1/evraklar/{id}", new { borcluKimlikNo = "10000000146" });
        Assert.True(p.IsSuccessStatusCode, await p.Content.ReadAsStringAsync());
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal("10000000146", db.KiymetliEvraklar.AsNoTracking().Single(e => e.Id == id).BorcluKimlikNo);

        var vade = Zaman.YerelBugun().AddDays(20);
        Assert.Equal(M(GirdiKodu.TarihGec, gun: vade), await Red(await c.PatchJson($"/api/v1/evraklar/{id}",
            new { kesideTarihi = vade.AddDays(5).ToString("yyyy-MM-dd") }), "kesideTarihi"));
        // vade keşideden ÖNCEYE çekilirse de aynı kural (kayıtlı keşide + yeni vade birlikte sınanır)
        Assert.True((await c.PatchJson($"/api/v1/evraklar/{id}", new { kesideTarihi = vade.ToString("yyyy-MM-dd") })).IsSuccessStatusCode);
        await Red(await c.PatchJson($"/api/v1/evraklar/{id}", new { vadeGunu = vade.AddDays(-1).ToString("yyyy-MM-dd") }), "kesideTarihi");
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(vade, db.KiymetliEvraklar.AsNoTracking().Single(e => e.Id == id).VadeGunu);

        await Red(await c.GetAsync("/api/v1/evraklar?evrakTuru=XYZ"), "evrakTuru");
        Assert.Equal(M(GirdiKodu.AralikTers), await Red(await c.GetAsync("/api/v1/evraklar?vadeBas=2026-12-01&vadeBit=2026-11-01"), "vadeBit"));
        await Red(await c.GetAsync("/api/v1/evraklar/takvim?bas=1999-01-01&bit=1999-02-01"), "bas");
    }

    // ── #3 MASAK uzunlukları + kimlik (servis yolu, satış) ────────────────────────────────
    [Fact]
    public async Task GBH05_Satis_MASAK_ad_soyad_1_dogum_gelecek_amac_101_400_hareket_YOK()
    {
        var k = YeniKiraci("masak");
        var c = f.Ortam.Istemci(k);
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.Hareketler.AsNoTracking().Count(x => x.TenantId == k.TenantId); }
        var once = Sayi();
        object Govde(object masak) => new
        {
            odemeTuru = "HAVALE", kasaId = k.KasaId, kurSnapshotId = k.KurId, cariId = k.CariId,
            urunId = k.UrunId, adet = 1m, birimFiyatTl = 1_000_000m, masak,
        };
        await Red(await c.PostJson("/api/v1/islemler/satis", Govde(new { belgeTuru = "TC_KIMLIK", belgeNo = "10000000146", adSoyad = "1" })), "adSoyad");
        await Red(await c.PostJson("/api/v1/islemler/satis", Govde(new { belgeTuru = "TC_KIMLIK", belgeNo = "10000000146", adSoyad = "Ali Veli", dogumTarihi = "3000-01-01" })), "dogumTarihi");
        await Red(await c.PostJson("/api/v1/islemler/satis", Govde(new
        {
            belgeTuru = "TC_KIMLIK", belgeNo = "10000000146", adSoyad = "Ali Veli",
            islemAmaci = new string('a', AlanSinirlari.MasakIslemAmaci + 1),
        })), "islemAmaci");
        Assert.Equal(once, Sayi());
    }

    // ── MASAK bildirim ileri tarihi (yaptırım eşleşmesi) ──────────────────────────────────
    [Fact]
    public async Task GBH06_Yaptirim_eslesmesi_ileri_tarihli_MASAK_bildirimi_400_kayit_DEGISMEZ_bugun_gecerli()
    {
        var k = YeniKiraci("yaptirim");
        var id = Kimlik.Ulid();
        using (var db = f.Vt.OlusturSistem())
        {
            db.YaptirimEslesmeleri.Add(new YaptirimEslesmesi
            {
                Id = id, TenantId = k.TenantId, YaptirimKaydiId = Kimlik.Ulid(), Tur = YaptirimEslesmeTuru.AdBenzerligi,
                Nokta = "CARI", CariId = k.CariId, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
            });
            db.SaveChanges();
        }
        var c = f.Ortam.Istemci(k);
        var yarin = Zaman.YerelBugun().AddDays(1).ToString("yyyy-MM-dd");
        Assert.Equal(M(GirdiKodu.TarihIleri), await Red(await c.PatchJson($"/api/v1/masak/yaptirim-eslesmeleri/{id}",
            new { durum = "BILDIRILDI", masakBildirimTarihi = yarin, masakEvrakNo = "E-1" }), "masakBildirimTarihi"));
        Assert.Equal(M(GirdiKodu.TarihIleri), await Red(await c.PatchJson($"/api/v1/masak/yaptirim-eslesmeleri/{id}",
            new { masakBildirimTarihi = "3000-01-01" }), "masakBildirimTarihi"));
        using (var db = f.Vt.OlusturSistem())
        {
            var e = db.YaptirimEslesmeleri.AsNoTracking().Single(x => x.Id == id);
            Assert.Equal(YaptirimEslesmeDurumu.Acik, e.Durum);
            Assert.Null(e.MasakBildirimTarihi);
        }
        var ok = await c.PatchJson($"/api/v1/masak/yaptirim-eslesmeleri/{id}",
            new { durum = "BILDIRILDI", masakBildirimTarihi = Zaman.YerelBugun().ToString("yyyy-MM-dd"), masakEvrakNo = "E-1" });
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
    }

    // ── Şüpheli bildirim tutarı (#10) · kapatma gerekçesi kırpılır · elle risk gerekçesi kolon ──
    [Fact]
    public async Task GBH07_Supheli_tutar_sifir_eksi_ondalik_400_kapatma_bosluklu_gerekce_400_risk_gerekce_501_400()
    {
        var k = YeniKiraci("supheli");
        var c = f.Ortam.Istemci(k);
        var aciklama = new string('a', 320);
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.MasakSupheliBildirimler.AsNoTracking().Count(x => x.TenantId == k.TenantId); }
        Assert.Equal(M(GirdiKodu.SayiSifir), await Red(await c.PostJson("/api/v1/masak/supheli", new { islemTutariTl = 0m, gerekce = aciklama }), "islemTutariTl"));
        Assert.Equal(M(GirdiKodu.SayiEksi), await Red(await c.PostJson("/api/v1/masak/supheli", new { islemTutariTl = -5m, gerekce = aciklama }), "islemTutariTl"));
        Assert.Equal(M(GirdiKodu.SayiOndalik, 2), await Red(await c.PostJson("/api/v1/masak/supheli", new { islemTutariTl = 10.005m, gerekce = aciklama }), "islemTutariTl"));
        Assert.Equal(0, Sayi());
        var ok = await c.PostJson("/api/v1/masak/supheli", new { islemTutariTl = 1500.25m, gerekce = aciklama });
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        var sid = (await ok.Json()).GetProperty("id").GetString()!;

        Assert.Equal(M(GirdiKodu.GerekceKisa, 20, 4), await Red(await c.PatchJson($"/api/v1/masak/supheli/{sid}/kapat",
            new { gerekce = "     kisa                    " }), "gerekce"));
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal("ACIK", db.MasakSupheliBildirimler.AsNoTracking().Single(x => x.Id == sid).Durum);

        await Red(await c.PostJson($"/api/v1/masak/risk/{k.CariId}/elle",
            new { seviye = "YUKSEK", gerekce = new string('g', AlanSinirlari.RiskElleGerekce + 1) }), "gerekce");
    }

    // ── İş ayarı: uydurma iş kodu ─────────────────────────────────────────────────────────
    [Fact]
    public async Task GBH08_Is_ayari_uydurma_is_kodu_400_bilinen_kod_gecer()
    {
        var k = YeniKiraci("isler");
        var c = f.Ortam.Istemci(k);
        await Red(await c.PatchJson("/api/v1/isler/ayarlar", new { kapaliIsler = new[] { "UYDURMA_IS" } }), "kapaliIsler");
        var ok = await c.PatchJson("/api/v1/isler/ayarlar", new { kapaliIsler = new[] { "VADE_HATIRLATMA" } });
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
    }

    // ── Ortak ızgara tercihi: A'da kalıcı, B'de varsayılan ─────────────────────────────────
    [Fact]
    public async Task GBH09_Uyum_izgara_tercihi_A_da_kalici_B_de_yok_bilinmeyen_ekran_400()
    {
        var k = YeniKiraci("izgara");
        const string anahtar = "grid:uyum:uy-supheli";
        const string deger = """{"s":1,"k":["tutar","tespit","son-bildirim","durum","gerekce"],"g":["gerekce"],"o":{"a":"tutar","y":"azalan"}}""";
        var a = f.Ortam.Istemci(k);
        var yaz = await a.PatchAsJsonAsync("/api/v1/tercihler/benim", new { tercihler = new Dictionary<string, string?> { [anahtar] = deger } });
        Assert.Equal(HttpStatusCode.OK, yaz.StatusCode);
        var oku = await (await a.GetAsync("/api/v1/tercihler/benim")).Json();
        Assert.Equal(deger, oku.GetProperty("tercihler").GetProperty(anahtar).GetString());
        var b = f.Ortam.Istemci("gocb-b-kullanici", k.TenantId, k.SubeId, "MUDUR");
        var bOku = await (await b.GetAsync("/api/v1/tercihler/benim")).Json();
        Assert.False(bOku.GetProperty("tercihler").TryGetProperty(anahtar, out _));
        var kotu = await a.PatchAsJsonAsync("/api/v1/tercihler/benim", new { tercihler = new Dictionary<string, string?> { ["grid:uyumx:liste"] = deger } });
        Assert.Equal(HttpStatusCode.BadRequest, kotu.StatusCode);
    }

    // ── Tur 2: doğrulayıcı KRİTİK bulguları (kimlik veri kaybı, tutar tavanı) + DÜŞÜK (yabancı borçlu) ─────
    private (string Kimlik, string? Hash) KimlikOku(string id)
    {
        using var db = f.Vt.OlusturSistem();
        var e = db.KiymetliEvraklar.AsNoTracking().Single(x => x.Id == id);
        return (e.BorcluKimlikNo ?? "", e.BorcluKimlikNoHash);
    }

    [Fact]
    public async Task GBH10_Evrak_kimlik_YALNIZ_YAZILIR_null_ve_yok_KORUR_acik_sil_SILER_celiski_400_liste_yalniz_var_yok()
    {
        var k = YeniKiraci("evrak kimlik t2");
        var c = f.Ortam.Istemci(k);
        var ok = await c.PostJson("/api/v1/evraklar", EvrakGovde(k, "GK-1", g => g["borcluKimlikNo"] = "10000000146"));
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        string id;
        using (var db = f.Vt.OlusturSistem()) id = db.KiymetliEvraklar.AsNoTracking().Single(e => e.TenantId == k.TenantId).Id;
        var hash0 = KimlikOku(id).Hash;
        Assert.NotNull(hash0);

        // (a) alan YOK → korunur
        Assert.True((await c.PatchJson($"/api/v1/evraklar/{id}", new { notlar = "a" })).IsSuccessStatusCode);
        Assert.Equal(("10000000146", hash0), KimlikOku(id));
        // (b) alan null → YİNE korunur (yalnız-yazılır alan; eski ekran gövdesi)
        Assert.True((await c.PatchJson($"/api/v1/evraklar/{id}", new Dictionary<string, object?> { ["borcluKimlikNo"] = null, ["notlar"] = "b" })).IsSuccessStatusCode);
        Assert.Equal(("10000000146", hash0), KimlikOku(id));
        // (c) liste: değer DÖNMEZ, yalnız "kayıtlı" bayrağı
        var liste = await (await c.GetAsync("/api/v1/evraklar")).Json();
        var satir = liste.GetProperty("satirlar")[0];
        Assert.False(satir.TryGetProperty("borcluKimlikNo", out _));
        Assert.True(satir.GetProperty("borcluKimlikKayitli").GetBoolean());
        Assert.DoesNotContain("10000000146", liste.GetRawText());
        // (d) hem değer hem sil → 400, kayıt değişmez
        Assert.Equal(MEvrak.EvrakServisi.KimlikSilCelisikMesaj, await Red(await c.PatchJson($"/api/v1/evraklar/{id}",
            new { borcluKimlikNo = "10000000146", borcluKimlikSil = true }), "borcluKimlikNo"));
        Assert.Equal(("10000000146", hash0), KimlikOku(id));
        // (e) AÇIK sil → kimlik ve arama hash'i silinir
        Assert.True((await c.PatchJson($"/api/v1/evraklar/{id}", new { borcluKimlikSil = true })).IsSuccessStatusCode);
        Assert.Equal(("", (string?)null), KimlikOku(id));
        var liste2 = await (await c.GetAsync("/api/v1/evraklar")).Json();
        Assert.False(liste2.GetProperty("satirlar")[0].GetProperty("borcluKimlikKayitli").GetBoolean());
    }

    [Fact]
    public async Task GBH11_Evrak_tutar_K9_tek_tavan_buyuk_tutar_yazilir_tavan_ustu_dogru_ileti_yabanci_borclu_pasaport()
    {
        var k = YeniKiraci("evrak tutar t2");
        var c = f.Ortam.Istemci(k);
        var ok = await c.PostJson("/api/v1/evraklar", EvrakGovde(k, "GT-1", g => g["tutar"] = 999_999_999.99m));
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        Assert.Equal(M(GirdiKodu.SayiTavan, TutarKurali.Tavan), await Red(await c.PostJson("/api/v1/evraklar",
            EvrakGovde(k, "GT-2", g => g["tutar"] = 10_000_000_000_000m)), "tutar"));
        Assert.Equal(M(GirdiKodu.SayiSifir), await Red(await c.PostJson("/api/v1/evraklar", EvrakGovde(k, "GT-3", g => g["tutar"] = 0m)), "tutar"));
        await Red(await c.PostJson("/api/v1/evraklar", EvrakGovde(k, "GT-4", g => g["tutar"] = -5m)), "tutar");
        // yabancı borçlu: harfli = pasaport K7 (küçük harf büyütülür); bozuk pasaport ve 14 rakam 400
        var yp = await c.PostJson("/api/v1/evraklar", EvrakGovde(k, "GT-5", g => g["borcluKimlikNo"] = "u1234567x"));
        Assert.True(yp.IsSuccessStatusCode, await yp.Content.ReadAsStringAsync());
        Assert.Equal(M(GirdiKodu.PasaportBicim, 5, AlanSinirlari.Pasaport), await Red(await c.PostJson("/api/v1/evraklar",
            EvrakGovde(k, "GT-6", g => g["borcluKimlikNo"] = "AB-12")), "borcluKimlikNo"));
        await Red(await c.PostJson("/api/v1/evraklar", EvrakGovde(k, "GT-7", g => g["borcluKimlikNo"] = "11111111111")), "borcluKimlikNo");
        using var db = f.Vt.OlusturSistem();
        var t = db.KiymetliEvraklar.AsNoTracking().Where(e => e.TenantId == k.TenantId).ToList();
        Assert.Equal(2, t.Count);
        Assert.Contains(t, e => e.Tutar == 999_999_999.99m);
        Assert.Equal("U1234567X", t.Single(e => e.EvrakNo == "GT-5").BorcluKimlikNo);
    }
}

/// <summary>bUnit: ortak bileşenler ekranda (alan altı metin + Kaydet kilidi + PATCH gövdesi).</summary>
public sealed class GocBEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public GocBEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
    }

    private const string Evrak = """
        {"id":"e1","evrakTuru":"CEK","yon":"ALINAN","durum":"PORTFOYDE","tutar":1000,"paraBirimi":"TRY","vadeGunu":"2027-01-15",
         "evrakNo":"C-1","bankaAdi":"Ziraat","bankaSube":null,"kesideYeri":null,"kesideTarihi":null,"borcluCariId":null,
         "borcluAd":"Ali Veli","kaynakCariId":"c1","kaynakCariAd":"Ayşe Yılmaz","notlar":null,"gecikmeGun":0,"gecikmis":false,
         "yakindaVadeli":false,"duzenlenebilir":true,"izinliGecisler":["TAHSILE_VERILDI"],"borcluKimlikKayitli":true}
        """;

    private sealed class Sahte(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, yol, govde));
            var g = yol switch
            {
                var y when y.EndsWith("/evraklar") && istek.Method == HttpMethod.Get =>
                    "{\"toplamAdet\":1,\"toplamTutar\":1000,\"limit\":200,\"azamiLimit\":200,\"offset\":0,\"dahaFazlasiVar\":false," +
                    "\"portfoydeTutar\":1000,\"tahsileVerilenTutar\":0,\"buHaftaVadeliTutar\":0,\"karsiliksizTutar\":0,\"satirlar\":[" + Evrak + "]}",
                var y when y.Contains("/evraklar/e1") => "{\"evrak\":" + Evrak + "}",
                var y when y.EndsWith("/cari") => """
                    [{"id":"c1","ad":"Ayşe Yılmaz","tur":"MUSTERI","telefon":null,"email":null,"tlBakiye":0,"hasGramBakiye":0,
                      "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"riskSeviye":null,"pepBeyani":false}]
                    """,
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"kaynak":"MANUAL","olusturuldu":"2026-10-08T08:00:00Z","ayarFiyatlari":[]}""",
                var y when y.Contains("/masak/supheli") => """
                    [{"id":"sb1","hareketId":null,"cariId":null,"kimlikTespitiId":null,"islemTutariTl":150000,"gerekce":"x",
                      "tespitTarihi":"2026-10-01T10:00:00Z","sonBildirimTarihi":"2026-10-11T10:00:00Z","kalanSaat":60,"durum":"ACIK",
                      "gonderimTarihi":null,"gonderimReferansi":null,"kapatmaGerekcesi":null,"bildirenKullaniciId":"u1",
                      "saklamaBitis":"2034-10-01T10:00:00Z","olusturuldu":"2026-10-01T10:00:00Z","guncellendi":"2026-10-01T10:00:00Z"}]
                    """,
                var y when y.Contains("/masak/kimlik-tespitleri") => "[]",
                var y when y.Contains("/masak/ozet") => "{}",
                var y when y.Contains("/tercihler/benim") => """{"tercihler":{}}""",
                var y when y.Contains("/auth/ben") =>
                    """{"id":"u1","ad":"Test","soyad":"Kullanici","email":"t@t.local","rol":"SAHIP","subeId":"sb1"}""",
                var y when y.Contains("/bankalar") || y.Contains("/kasalar") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private void Oturum() =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));

    [Fact]
    public void GBE01_Evrak_duzenle_PATCH_govdesi_borclu_kimlik_no_TASIR_ortak_izgara_basliklari_korunur()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Evraklar>();
        e.WaitForElement("#evrak-satir-e1");
        Assert.NotNull(e.Find("#evrak-th-tur"));
        Assert.NotNull(e.Find("#evrak-liste-sutunlar-ac"));                 // ortak ızgara: sütun paneli
        e.Find("#evrak-duzenle-e1").Click();
        e.WaitForElement("#evrak-form-borclu-kimlik");
        e.Find("#evrak-form-borclu-kimlik").Input("10000000146");
        e.Find("#evrak-form-borclu-kimlik").Change("10000000146");
        e.WaitForAssertion(() => Assert.False(e.Find("#cekmece-evrak-kaydet").HasAttribute("disabled")));
        e.Find("#cekmece-evrak-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            lock (istekler)
            {
                var p = istekler.Last(i => i.Metot == "PATCH");
                var j = JsonDocument.Parse(p.Govde).RootElement;
                Assert.Equal("10000000146", j.GetProperty("borcluKimlikNo").GetString());
            }
        });
    }

    [Fact]
    public void GBE02_Evrak_keside_vadeden_sonra_alan_alti_sozluk_metni_ve_Kaydet_kilitli()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Evraklar>();
        e.WaitForElement("#evrak-satir-e1");
        e.Find("#evrak-duzenle-e1").Click();
        e.WaitForElement("#evrak-form-keside-tarihi");
        e.Find("#evrak-form-keside-tarihi").Change("16.01.2027");
        e.WaitForAssertion(() =>
        {
            Assert.Equal(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.TarihGec, Gun: new DateOnly(2027, 1, 15)), "tr"),
                e.Find("#evrak-form-keside-tarihi-hata").TextContent.Trim());
            Assert.True(e.Find("#cekmece-evrak-kaydet").HasAttribute("disabled"));
        });
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
    }

    [Fact]
    public void GBE03_Cari_yeni_dogum_3000_yili_alan_alti_ve_ad_yalniz_rakam_alan_alti_POST_YOK()
    {
        Oturum();
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => false);
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("#cari-yeni-ac");
        e.Find("#cari-yeni-ac").Click();
        e.WaitForElement("#cari-ad");
        e.Find("#cari-ad").Change("12345");
        e.WaitForAssertion(() => Assert.Equal(GirdiSozlugu.Tr.AdHarf, e.Find("#alan-hata-cari-ad").TextContent.Trim()));
        e.Find("#cari-dogum").Change("01.01.3000");
        e.WaitForAssertion(() => Assert.False(string.IsNullOrWhiteSpace(e.Find("#cari-dogum-hata").TextContent)));
        Assert.True(e.Find("#cari-kaydet").HasAttribute("disabled"));
        lock (istekler) Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/cari"));
    }

    [Fact]
    public void GBE04_Uyum_supheli_listesi_ortak_izgara_ham_durum_kodu_yok()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Uyum>();
        e.WaitForElement("#uy-sekme-supheli");
        e.Find("#uy-sekme-supheli").Click();
        e.WaitForElement("#uy-supheli-tablo");
        var satir = e.Find("#uy-supheli-satir-sb1");
        Assert.DoesNotContain(">ACIK<", satir.InnerHtml);
        Assert.Contains(Services.GetRequiredService<DilServisi>().T.Uyum.DurumAcik, satir.TextContent);
        Assert.NotNull(e.Find("#uy-kapat-sb1"));
    }

    private JsonElement SonPatch()
    {
        lock (istekler) return JsonDocument.Parse(istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
    }

    [Fact]
    public void GBE05_Evrak_kimlige_dokunulmazsa_PATCH_govdesinde_alan_YOK_acik_kaldir_borcluKimlikSil_gonderir()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Evraklar>();
        e.WaitForElement("#evrak-satir-e1");
        e.Find("#evrak-duzenle-e1").Click();
        e.WaitForElement("#evrak-form-notlar");
        Assert.Contains(GocBSozlugu.Tr.KimlikKayitliGizli, e.Markup);         // kayıtlı ama gizli
        e.Find("#evrak-form-notlar").Input("yalnız not");
        e.WaitForAssertion(() => Assert.False(e.Find("#cekmece-evrak-kaydet").HasAttribute("disabled")));
        e.Find("#cekmece-evrak-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            var j = SonPatch();
            Assert.False(j.TryGetProperty("borcluKimlikNo", out _), j.GetRawText());
            Assert.False(j.TryGetProperty("borcluKimlikSil", out _), j.GetRawText());
            Assert.Equal("yalnız not", j.GetProperty("notlar").GetString());
        });

        // açık "Kimlik no'yu kaldır" → borcluKimlikSil:true, kimlik değeri YOK
        e.WaitForElement("#evrak-duzenle-e1");
        e.Find("#evrak-duzenle-e1").Click();
        e.WaitForElement("#evrak-form-borclu-kimlik-kaldir");
        e.Find("#evrak-form-borclu-kimlik-kaldir").Change(true);
        e.WaitForAssertion(() => Assert.Contains(GocBSozlugu.Tr.KimlikKaldirilacak, e.Markup));
        e.Find("#cekmece-evrak-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            var j = SonPatch();
            Assert.True(j.GetProperty("borcluKimlikSil").GetBoolean(), j.GetRawText());
            Assert.False(j.TryGetProperty("borcluKimlikNo", out _), j.GetRawText());
        });
    }

    [Fact]
    public void GBE06_Evrak_yabanci_borclu_pasaport_yazilir_bozuk_pasaport_alan_alti_ve_kilit()
    {
        Oturum();
        var e = Render<Kuyumcu.Web.Pages.Evraklar>();
        e.WaitForElement("#evrak-satir-e1");
        e.Find("#evrak-duzenle-e1").Click();
        e.WaitForElement("#evrak-form-borclu-kimlik-turu-secenek-1");
        e.Find("#evrak-form-borclu-kimlik-turu-secenek-1").Change("YABANCI");
        e.WaitForAssertion(() => Assert.Equal("pasaport", e.Find("#evrak-form-borclu-kimlik").GetAttribute("data-maske")));
        Assert.Contains(GocBSozlugu.Tr.BorcluPasaportNo, e.Markup);   // etiket türle uyumlu (ölçüm görüntüsü bulgusu)
        e.Find("#evrak-form-borclu-kimlik").Change("AB12");
        e.WaitForAssertion(() =>
        {
            Assert.Equal(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.PasaportBicim, 5, AlanSinirlari.Pasaport), "tr"),
                e.Find("#evrak-form-borclu-kimlik-hata").TextContent.Trim());
            Assert.True(e.Find("#cekmece-evrak-kaydet").HasAttribute("disabled"));
        });
        e.Find("#evrak-form-borclu-kimlik").Change("u1234567x");
        e.WaitForAssertion(() => Assert.False(e.Find("#cekmece-evrak-kaydet").HasAttribute("disabled")));
        e.Find("#cekmece-evrak-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal("U1234567X", SonPatch().GetProperty("borcluKimlikNo").GetString()));
    }

    [Fact]
    public void GBE07_Cari_17_yas_dogum_ENGEL_DEGIL_alan_altinda_uyari_hata_yok()
    {
        Oturum();
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => false);
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("#cari-yeni-ac");
        e.Find("#cari-yeni-ac").Click();
        e.WaitForElement("#cari-dogum");
        e.Find("#cari-ad").Change("Can Kaya");
        var genc = DateOnly.FromDateTime(Kuyumcu.Web.Servisler.Bicim.IsletmeBugun).AddYears(-17);
        e.Find("#cari-dogum").Change(genc.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture));
        e.WaitForAssertion(() => Assert.Contains(GocBSozlugu.Tr.DogumResitDegilUyari, e.Markup));
        Assert.DoesNotContain(e.FindAll("#cari-dogum-hata"), x => !string.IsNullOrWhiteSpace(x.TextContent));
    }
}
