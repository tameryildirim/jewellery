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
using System.Net.Http;
using MAuth = Kuyumcu.Api.Modules.Auth;
using MKullanici = Kuyumcu.Api.Modules.Kullanici;
using MSube = Kuyumcu.Api.Modules.Sube;
using MYedek = Kuyumcu.Api.Modules.Yedek;
using MRapor = Kuyumcu.Api.Modules.Rapor;
using MAdmin = Kuyumcu.Api.Modules.Admin;

namespace Kuyumcu.Tests;

// olumsuz-fikstür: dosya — bu dosya kuralların SINIRLARINI sınar; geçersiz değerler (3000 yılı, ters aralık,
// 10 yıllık dönem, beyaz liste dışı süzgeç, zayıf parola "aaaaaaaa", 9 haneli telefon, "a@b" e-posta …) bilinçlidir.
// ═════════════════════════════════════════════════════════════════════════════
// PAKET GOC-D — yönetim ve rapor ekranlarının ortak girdi + ortak ızgara göçü.
// Kaynak: dokumanlar/09-uygulama-kayitlari/GIRDI-KURALLARI-ENVANTER-2026-10-08.md (E) Faz 2 GOC-D:
// kayıt parolası, şube telefonu hane sayısı, rapor tarih aralıkları (400 + bas ≤ bit), süzgeç beyaz listeleri.
// Her açık için sunucu 400 (+ alan + satır YAZILMAZ) ve ileti ekranın alan altı metniyle AYNI sözlük anahtarından.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Saf: kural ve sözleşme (veritabanı yok).</summary>
public sealed class GocDSafTestleri
{
    private static readonly DateOnly Bugun = new(2026, 10, 9);

    private static string M(GirdiKodu kod, decimal? s1 = null, decimal? s2 = null, DateOnly? gun = null) =>
        GirdiSozlugu.Mesaj(new GirdiHatasi(kod, s1, s2, gun), "tr");

    private static T Oz<T>(Type t, string ozellik) where T : Attribute =>
        t.GetProperty(ozellik)!.GetCustomAttributes(true).OfType<T>().Single();

    private static bool Var<T>(Type t, string ozellik) where T : Attribute =>
        t.GetProperty(ozellik)!.GetCustomAttributes(true).OfType<T>().Any();

    [Fact]
    public void GDS01_Izgara_bilinen_ekranlar_GOC_D_ekranlarini_icerir()
    {
        foreach (var e in new[] { "ayarlar", "kullanicilar", "isletme", "yedekler", "raporlar", "kar-zarar", "satis-analizi",
                                  "denetim-izi", "islem-defteri", "yetkiler" })
            Assert.True(IzgaraTercihKurali.AnahtarGecerli($"grid:{e}:liste"), e);
        Assert.False(IzgaraTercihKurali.AnahtarGecerli("grid:raporlarx:liste"));
    }

    [Fact]
    public void GDS02_Rapor_donemi_pencere_ters_ve_400_gun_tek_kural_sozluk_metniyle()
    {
        // ters aralık → bitiş alanı, Kabuk.TarihAraligi (ortak TarihAralik bileşeniyle AYNI anahtar)
        var ters = RaporDonemKurali.Denetle(new DateOnly(2026, 9, 16), new DateOnly(2026, 9, 1), Bugun, "tr");
        Assert.Equal(("bitis", Sozlukler.Tr.Kabuk.TarihAraligi), ters);
        // 3000 yılı → pencere dışı (Rapor: gelecek yıl sonu), 1999 → 2000 öncesi
        Assert.Equal(("baslangic", M(GirdiKodu.TarihGec, gun: new DateOnly(2027, 12, 31))),
            RaporDonemKurali.Denetle(new DateOnly(3000, 1, 1), new DateOnly(3000, 1, 2), Bugun, "tr"));
        Assert.Equal(("baslangic", M(GirdiKodu.TarihErken, gun: new DateOnly(2000, 1, 1))),
            RaporDonemKurali.Denetle(new DateOnly(1999, 12, 31), Bugun, Bugun, "tr"));
        Assert.Equal(("bitis", M(GirdiKodu.TarihGec, gun: new DateOnly(2027, 12, 31))),
            RaporDonemKurali.Denetle(Bugun, new DateOnly(2028, 1, 1), Bugun, "tr"));
        // 400 gün tam sınır geçer, 401 gün red; 10 yıl red
        var bas = new DateOnly(2025, 1, 1);
        Assert.Null(RaporDonemKurali.Denetle(bas, bas.AddDays(RaporDonemKurali.EnCokGun - 1), Bugun, "tr"));
        Assert.Equal(("bitis", string.Format(GocDSozlugu.Tr.RaporDonemUzun, 400)),
            RaporDonemKurali.Denetle(bas, bas.AddDays(RaporDonemKurali.EnCokGun), Bugun, "tr"));
        Assert.NotNull(RaporDonemKurali.Denetle(new DateOnly(2016, 1, 1), new DateOnly(2026, 1, 1), Bugun, "tr"));
        // denetim izi (gün sınırı yok) → K8e 5 yıl
        Assert.Null(RaporDonemKurali.Denetle(new DateOnly(2022, 1, 1), new DateOnly(2026, 1, 1), Bugun, "tr", enCokGun: 0));
        Assert.Equal(("bitis", M(GirdiKodu.AralikUzun, 5)),
            RaporDonemKurali.Denetle(new DateOnly(2016, 1, 1), new DateOnly(2026, 1, 1), Bugun, "tr", enCokGun: 0));
        // tek gün (mutabakat / günlük özet)
        Assert.NotNull(RaporDonemKurali.GunDenetle(new DateOnly(3000, 1, 1), Bugun, "tr"));
        Assert.Null(RaporDonemKurali.GunDenetle(Bugun, Bugun, "tr"));
        // İngilizce aynı anahtar
        Assert.Equal(Sozlukler.En.Kabuk.TarihAraligi,
            RaporDonemKurali.Denetle(new DateOnly(2026, 9, 16), new DateOnly(2026, 9, 1), Bugun, "en")!.Value.Mesaj);
    }

    [Fact]
    public void GDS03_Parola_kurali_8_72_harf_ve_rakam_metni_sozlukten()
    {
        Assert.False(ParolaKurali.Gecerli("aaaaaaaa"));      // envanter: kayıt ucu bunu kabul ediyordu
        Assert.False(ParolaKurali.Gecerli("12345678"));
        Assert.False(ParolaKurali.Gecerli("abc1234"));       // 7
        Assert.True(ParolaKurali.Gecerli("abcd1234"));       // 8 — tam sınır
        Assert.True(ParolaKurali.Gecerli("a1" + new string('x', 70)));   // 72 — tam sınır
        Assert.False(ParolaKurali.Gecerli("a1" + new string('x', 71)));  // 73
        Assert.Equal(AuthSifirlaSozlugu.Tr.ParolaKurali, ParolaKurali.Mesaj("aaaaaaaa", "tr"));
        Assert.Equal(AuthSifirlaSozlugu.En.ParolaKurali, ParolaKurali.Mesaj("aaaaaaaa", "en"));
        Assert.Null(ParolaKurali.Mesaj("", "tr"));
        // sıfırlama ekranının istemci kuralı artık AYNI sınıf
        Assert.Equal(ParolaKurali.Gecerli("aaaaaaaa"), Kuyumcu.Web.Pages.SifreSifirla.ParolaKuralinaUyar("aaaaaaaa"));
        Assert.Equal(ParolaKurali.Gecerli("abcd1234"), Kuyumcu.Web.Pages.SifreSifirla.ParolaKuralinaUyar("abcd1234"));
    }

    [Fact]
    public void GDS04_Suzgec_beyaz_listeleri_defterdeki_degerleri_kapsar_bilinmeyeni_reddeder()
    {
        foreach (var a in Sabitler.OdemeTurleri) Assert.Contains(a, RaporSuzgecKurali.OdemeAraclari);
        foreach (var a in new[] { "VERESIYE", "KARMA", "DOVIZ" }) Assert.Contains(a, RaporSuzgecKurali.OdemeAraclari);
        var eylemler = typeof(DenetimEylemi).GetFields().Select(x => (string)x.GetValue(null)!).ToHashSet();
        Assert.True(eylemler.SetEquals(RaporSuzgecKurali.DenetimEylemleri));
        Assert.True(RaporSuzgecKurali.ListedeMi(null, RaporSuzgecKurali.OdemeAraclari));
        Assert.True(RaporSuzgecKurali.ListedeMi("", RaporSuzgecKurali.OdemeAraclari));
        Assert.False(RaporSuzgecKurali.ListedeMi("nakit", RaporSuzgecKurali.OdemeAraclari));   // TAM eşleşme
        Assert.False(RaporSuzgecKurali.ListedeMi("XYZ", RaporSuzgecKurali.OdemeAraclari));
        Assert.True(RaporSuzgecKurali.VarlikTuruGecerli("GiderPusulasi"));
        Assert.False(RaporSuzgecKurali.VarlikTuruGecerli("a b"));
        Assert.False(RaporSuzgecKurali.VarlikTuruGecerli("1Cari"));
        Assert.False(RaporSuzgecKurali.VarlikTuruGecerli(new string('A', 101)));
    }

    [Fact]
    public void GDS05_DTO_lar_Core_kuralini_ve_ekranla_AYNI_sabiti_tasir()
    {
        Assert.True(Var<MAuth.ParolaAttribute>(typeof(MAuth.KayitDto), "Sifre"));
        Assert.True(Var<EpostaAttribute>(typeof(MAuth.KayitDto), "Email"));
        Assert.True(Var<EpostaAttribute>(typeof(MKullanici.KullaniciOlusturDto), "Email"));
        Assert.True(Var<EpostaAttribute>(typeof(MKullanici.KullaniciGuncelleDto), "Email"));
        Assert.True(Var<TelefonAttribute>(typeof(MSube.SubeOlusturDto), "Telefon"));
        Assert.Equal(YonetimSinirlari.IsletmeAdi, Oz<MaxLengthAttribute>(typeof(MAuth.KayitDto), "IsletmeAdi").Length);
        Assert.Equal(AlanSinirlari.Ad, Oz<MaxLengthAttribute>(typeof(MAuth.KayitDto), "Ad").Length);
        Assert.Equal(YonetimSinirlari.DavetJetonu, Oz<MaxLengthAttribute>(typeof(MAuth.KayitDto), "DavetJetonu").Length);
        Assert.Equal(YonetimSinirlari.SubeAdi, Oz<MaxLengthAttribute>(typeof(MSube.SubeOlusturDto), "Ad").Length);
        Assert.Equal(AlanSinirlari.Adres, Oz<MaxLengthAttribute>(typeof(MSube.SubeOlusturDto), "Adres").Length);
        Assert.Equal(YonetimSinirlari.VergiDairesi, Oz<MaxLengthAttribute>(typeof(MAdmin.TenantKimlikGuncelleDto), "VergiDairesi").Length);
        Assert.Equal(YonetimSinirlari.YedekGerekce, Oz<MaxLengthAttribute>(typeof(MYedek.YedekTalepDto), "Gerekce").Length);
        Assert.Equal(YonetimSinirlari.YedekGerekce, Oz<MaxLengthAttribute>(typeof(MYedek.YedekTalepGuncelleDto), "IptalGerekce").Length);
        Assert.Equal(ParolaKurali.EnCok, MAuth.ParolaSifirlamaServisi.ParolaEnFazla);
        Assert.Equal(ParolaKurali.EnAz, MAuth.ParolaSifirlamaServisi.ParolaEnAz);
        Assert.Equal(RaporDonemKurali.EnCokGun, MRapor.RaporAraligi.EnFazlaGun);
        // MFA kod kutusu: kurtarma kodu (16 + ayraç) sığar; DTO 25'i aşmaz (orkestratör C4_62/63 değeri korundu)
        Assert.InRange(YonetimSinirlari.MfaKodGirisi, 16, 25);
    }

    [Fact]
    public void GDS06_RaporAraligi_servis_yolunda_3000_ters_ve_uzun_donemi_400_ile_reddeder()
    {
        var bugun = Zaman.YerelBugun();
        var h1 = Assert.Throws<BadRequestException>(() => MRapor.RaporAraligi.Coz(new DateOnly(3000, 1, 1), new DateOnly(3000, 1, 1)));
        Assert.Contains("31.12.", h1.Message);
        var h2 = Assert.Throws<BadRequestException>(() => MRapor.RaporAraligi.Coz(bugun, bugun.AddDays(-3)));
        Assert.Equal(Sozlukler.Tr.Kabuk.TarihAraligi, h2.Message);
        var h3 = Assert.Throws<BadRequestException>(() => MRapor.RaporAraligi.Coz(bugun.AddYears(-10), bugun));
        Assert.Equal(string.Format(GocDSozlugu.Tr.RaporDonemUzun, 400), h3.Message);
        var d = MRapor.RaporAraligi.Coz(bugun.AddDays(-399), bugun);   // tam sınır
        Assert.Equal(bugun, d.YerelBit);
    }
}

/// <summary>Sunucu (gerçek MySQL + HTTP boru hattı): 400 + alan + satır yazılmaz; geçerli kayıt olur; yetkisiz 403.</summary>
[Collection("http")]
public sealed class GocDSunucuTestleri(HttpFikstur f)
{
    private static string M(GirdiKodu kod, decimal? s1 = null, decimal? s2 = null, DateOnly? gun = null) =>
        GirdiSozlugu.Mesaj(new GirdiHatasi(kod, s1, s2, gun), "tr");

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETGOC-D-TEST " + ad);
    }

    /// <summary>400 + alan işareti + ilk ileti.</summary>
    private static async Task<string> Red(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), govde);
        var p = alanlar.EnumerateObject().FirstOrDefault(x => x.Name.Equals(alan, StringComparison.OrdinalIgnoreCase));
        Assert.True(p.Value.ValueKind == JsonValueKind.Array, $"'{alan}' işaretli değil: {govde}");
        return p.Value[0].GetString() ?? "";
    }

    private static object KayitGovde(string eposta, string sifre, string? jeton, string isletme) => new
    {
        isletmeAdi = isletme, ad = "Test", soyad = "Kayit", email = eposta, sifre, davetJetonu = jeton,
    };

    // ── Kayıt parolası (K31) + e-posta (K3): 400 + alan, kiracı DOĞMAZ; geçerli kayıt 201 ──
    [Fact]
    public async Task GDH01_Kayit_zayif_parola_ve_bozuk_eposta_400_kiraci_DOGMAZ_gecerli_kayit_201()
    {
        string jeton;
        await using (var db = f.Vt.OlusturSistem()) (jeton, _) = TestKurulum.DavetOlustur(db);
        var isletme = $"PAKETGOC-D-TEST kayit {Kimlik.Ulid()}";
        var eposta = $"gocd-{Kimlik.Ulid()}@test.local";

        Assert.Equal(AuthSifirlaSozlugu.Tr.ParolaKurali, await Red(await f.Ortam.AnonimIstemci()
            .PostJson("/api/v1/auth/kayit", KayitGovde(eposta, "aaaaaaaa", jeton, isletme)), "sifre"));
        Assert.Equal(AuthSifirlaSozlugu.Tr.ParolaKurali, await Red(await f.Ortam.AnonimIstemci()
            .PostJson("/api/v1/auth/kayit", KayitGovde(eposta, "12345678", jeton, isletme)), "sifre"));   // harfsiz
        Assert.Equal(M(GirdiKodu.EpostaBicim), await Red(await f.Ortam.AnonimIstemci()
            .PostJson("/api/v1/auth/kayit", KayitGovde("a@b", "GucluParola123", jeton, isletme)), "email"));
        await using (var db = f.Vt.OlusturSistem())
            Assert.False(await db.Tenants.AnyAsync(t => t.Ad == isletme));

        var ok = await f.Ortam.AnonimIstemci().PostJson("/api/v1/auth/kayit", KayitGovde(eposta, "GucluParola123", jeton, isletme));
        Assert.True(ok.StatusCode == HttpStatusCode.Created, $"{(int)ok.StatusCode} {await ok.Content.ReadAsStringAsync()}");
        await using (var db = f.Vt.OlusturSistem())
            Assert.True(await db.Tenants.AnyAsync(t => t.Ad == isletme));
    }

    // ── Şube telefonu (K2): 9 hane POST/PATCH 400 (HTTP + SERVİS yolu), kayıt değişmez; 10 hane 201 ──
    [Fact]
    public async Task GDH02_Sube_telefonu_9_hane_400_HTTP_ve_servis_yolu_gecerli_10_hane_kaydolur()
    {
        var k = YeniKiraci("sube");
        var c = f.Ortam.Istemci(k);
        await Red(await c.PostJson("/api/v1/subeler", new { ad = "GocD Şube 9", telefon = "212555123" }), "telefon");
        await using (var db = f.Vt.OlusturSistem())
            Assert.False(await db.Subeler.AnyAsync(s => s.TenantId == k.TenantId && s.Ad == "GocD Şube 9"));

        // servis yolu (DataAnnotations koşmaz) — AYNI Core kuralı, AYNI metin
        await using (var db = f.Vt.Olustur(k))
        {
            var servis = new MSube.SubeServisi(db);
            var h = await Assert.ThrowsAsync<BadRequestException>(() =>
                servis.Olustur(new MSube.SubeOlusturDto { Ad = "GocD Servis", Telefon = "212555123" }, k.Islem));
            Assert.Equal(M(GirdiKodu.TelefonHane, 9, 10), h.Message);
            var h2 = await Assert.ThrowsAsync<BadRequestException>(() =>
                servis.Guncelle(k.SubeId, new MSube.SubeGuncelleDto { Telefon = "212555123" }, k.Islem));
            Assert.Equal(M(GirdiKodu.TelefonHane, 9, 10), h2.Message);
        }

        var ok = await c.PostJson("/api/v1/subeler", new { ad = "GocD Şube 10", telefon = "02125551234" });
        Assert.True(ok.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK, $"{(int)ok.StatusCode} {await ok.Content.ReadAsStringAsync()}");
        await using (var db = f.Vt.OlusturSistem())
            Assert.True(await db.Subeler.AnyAsync(s => s.TenantId == k.TenantId && s.Ad == "GocD Şube 10"));
        var p = await c.PatchAsJsonAsync($"/api/v1/subeler/{k.SubeId}", new { telefon = "212555123" });
        await Red(p, "telefon");
    }

    // ── Rapor tarih aralıkları: 3000 / ters / 10 yıl → 400 (islem-defteri, odeme-araci, kar-zarar, gunluk, mutabakat, aylik) ──
    [Fact]
    public async Task GDH03_Rapor_uclari_3000_yili_ters_ve_uzun_donem_400_gecerli_donem_200()
    {
        var k = YeniKiraci("rapor");
        var c = f.Ortam.Istemci(k);
        var bugun = Zaman.YerelBugun();
        string G(DateOnly d) => d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(Sozlukler.Tr.Kabuk.TarihAraligi, await Red(await c.GetAsync(
            $"/api/v1/raporlar/islem-defteri?baslangic={G(bugun)}&bitis={G(bugun.AddDays(-5))}"), "bitis"));
        Assert.Contains("31.12.", await Red(await c.GetAsync("/api/v1/raporlar/islem-defteri?baslangic=3000-01-01&bitis=3000-01-02"), "baslangic"));
        Assert.Equal(string.Format(GocDSozlugu.Tr.RaporDonemUzun, 400), await Red(await c.GetAsync(
            $"/api/v1/raporlar/odeme-araci?baslangic={G(bugun.AddYears(-10))}&bitis={G(bugun)}"), "bitis"));
        Assert.Equal(Sozlukler.Tr.Kabuk.TarihAraligi, await Red(await c.GetAsync(
            $"/api/v1/raporlar/kar-zarar?baslangic={G(bugun)}&bitis={G(bugun.AddDays(-1))}"), "bitis"));
        await Red(await c.GetAsync("/api/v1/raporlar/kar-zarar?baslangic=3000-01-01&bitis=3000-01-31"), "baslangic");
        await Red(await c.GetAsync($"/api/v1/raporlar/kar-zarar/urun?baslangic={G(bugun.AddYears(-10))}&bitis={G(bugun)}"), "bitis");
        await Red(await c.GetAsync("/api/v1/raporlar/gunluk?tarih=3000-01-01"), "tarih");
        Assert.Equal(M(GirdiKodu.TarihBicim), await Red(await c.GetAsync("/api/v1/raporlar/pos-fatura-mutabakati?tarih=bozuk"), "tarih"));
        await Red(await c.GetAsync("/api/v1/raporlar/aylik?yil=3000&ay=1"), "yil");

        foreach (var u in new[]
                 {
                     $"/api/v1/raporlar/islem-defteri?baslangic={G(bugun.AddDays(-30))}&bitis={G(bugun)}",
                     $"/api/v1/raporlar/kar-zarar?baslangic={G(bugun.AddDays(-30))}&bitis={G(bugun)}",
                     $"/api/v1/raporlar/gunluk?tarih={G(bugun)}",
                     $"/api/v1/raporlar/aylik?yil={bugun.Year}&ay={bugun.Month}",
                 })
        {
            var r = await c.GetAsync(u);
            Assert.True(r.StatusCode == HttpStatusCode.OK, $"{u} → {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        }
    }

    // ── Süzgeç beyaz listeleri (K32): bilinmeyen değer "kayıt yok" DEĞİL, 400 + alan ──
    [Fact]
    public async Task GDH04_Suzgec_beyaz_liste_disi_400_bilinen_deger_200()
    {
        var k = YeniKiraci("suzgec");
        var c = f.Ortam.Istemci(k);
        var m = GocDSozlugu.Tr.SuzgecBilinmiyor;
        Assert.Equal(m, await Red(await c.GetAsync("/api/v1/raporlar/islem-defteri?tur=XYZ"), "tur"));
        Assert.Equal(m, await Red(await c.GetAsync("/api/v1/raporlar/islem-defteri?odemeTuru=XYZ"), "odemeTuru"));
        Assert.Equal(m, await Red(await c.GetAsync("/api/v1/raporlar/reyon-stok?maden=XYZ"), "maden"));
        Assert.Equal(m, await Red(await c.GetAsync("/api/v1/denetim?eylem=XYZ"), "eylem"));
        Assert.Equal(m, await Red(await c.GetAsync("/api/v1/denetim/ozet?varlikTuru=a%20b"), "varlikTuru"));
        Assert.Equal(Sozlukler.Tr.Kabuk.TarihAraligi, await Red(await c.GetAsync(
            "/api/v1/denetim?baslangic=2026-09-16&bitis=2026-09-01"), "bitis"));
        await Red(await c.GetAsync("/api/v1/denetim?baslangic=3000-01-01"), "baslangic");
        foreach (var u in new[] { "/api/v1/raporlar/islem-defteri?tur=SATIS&odemeTuru=KARMA", "/api/v1/raporlar/reyon-stok?maden=ALTIN",
                                  "/api/v1/denetim?eylem=GUNCELLEME&varlikTuru=Cari", "/api/v1/denetim/ozet?eylem=OKUMA" })
        {
            var r = await c.GetAsync(u);
            Assert.True(r.StatusCode == HttpStatusCode.OK, $"{u} → {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        }
    }

    // ── Yetkisiz rol (TEZGAHTAR) yönetim uçlarında 403 — yetki davranışı DEĞİŞMEDİ ──
    [Fact]
    public async Task GDH05_Tezgahtar_yonetim_uclarinda_403()
    {
        var k = YeniKiraci("tezgahtar");
        var t = f.Ortam.Istemci("gocd-tezgahtar", k.TenantId, k.SubeId, "TEZGAHTAR");
        foreach (var (metot, yol) in new[]
                 {
                     ("GET", "/api/v1/kullanicilar"), ("GET", "/api/v1/yetkiler"), ("GET", "/api/v1/denetim"),
                     ("GET", "/api/v1/yedekler/ozet"), ("GET", "/api/v1/raporlar/islem-defteri"), ("POST", "/api/v1/subeler"),
                 })
        {
            var r = metot == "GET" ? await t.GetAsync(yol) : await t.PostJson(yol, new { ad = "X", telefon = "02125551234" });
            Assert.True(r.StatusCode == HttpStatusCode.Forbidden, $"{metot} {yol} → {(int)r.StatusCode}");
        }
    }

    // ── Kullanıcı oluştur e-posta (K3): "a@b" 400 + satır YOK; geçerli 201 ──
    [Fact]
    public async Task GDH06_Kullanici_olustur_bozuk_eposta_400_satir_YOK_gecerli_201()
    {
        var k = YeniKiraci("kullanici");
        var c = f.Ortam.Istemci(k);
        var govde = (string e) => new { ad = "Ayşe", soyad = "Kaya", email = e, sifre = "GucluParola123", rol = "TEZGAHTAR" };
        Assert.Equal(M(GirdiKodu.EpostaBicim), await Red(await c.PostJson("/api/v1/kullanicilar", govde("a@b")), "email"));
        await using (var db = f.Vt.OlusturSistem())
            Assert.False(await db.Kullanicilar.AnyAsync(u => u.TenantId == k.TenantId && u.Email == "a@b"));
        var e = $"gocd-{Kimlik.Ulid()}@test.local";
        var ok = await c.PostJson("/api/v1/kullanicilar", govde(e));
        Assert.True(ok.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK, $"{(int)ok.StatusCode} {await ok.Content.ReadAsStringAsync()}");
    }

    // ── Izgara tercihi: A kullanıcısında kalıcı, B'de varsayılan (kayıt yok) ──
    [Fact]
    public async Task GDH07_Kullanicilar_izgara_tercihi_A_da_kalici_B_de_yok()
    {
        var k = YeniKiraci("izgara");
        const string anahtar = "grid:kullanicilar:k-tablo";
        const string deger = """{"s":1,"k":["ad","rol","durum","sube","mfa","son-giris"],"g":["mfa"],"o":{"a":"ad","y":"azalan"}}""";
        var a = f.Ortam.Istemci(k);
        var yaz = await a.PatchAsJsonAsync("/api/v1/tercihler/benim", new { tercihler = new Dictionary<string, string?> { [anahtar] = deger } });
        Assert.Equal(HttpStatusCode.OK, yaz.StatusCode);
        var oku = await (await a.GetAsync("/api/v1/tercihler/benim")).Json();
        Assert.Equal(deger, oku.GetProperty("tercihler").GetProperty(anahtar).GetString());
        var b = f.Ortam.Istemci("gocd-b-kullanici", k.TenantId, k.SubeId, "MUDUR");
        var bOku = await (await b.GetAsync("/api/v1/tercihler/benim")).Json();
        Assert.False(bOku.GetProperty("tercihler").TryGetProperty(anahtar, out _));
    }
}

/// <summary>Ekran (bUnit): alan altı Türkçe ileti + kilit (aynı metin); hatalı dönemde istek GİTMEZ; ortak ızgara.</summary>
public sealed class GocDEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Sorgu)> istekler = [];
    private static readonly TimeSpan Uzun = TimeSpan.FromSeconds(5);

    public GocDEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
    }

    private sealed class Sahte(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            lock (kayit) kayit.Add((istek.Method.Method, yol, istek.RequestUri.Query));
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/odeme-araci") => """
                    {"baslangic":"2026-09-10","bitis":"2026-10-09","kaynak":"KASA_HAREKETLERI",
                     "araclar":[{"arac":"NAKIT","paraBirimleri":[{"paraBirimi":"TRY","girisTutar":1200.00,"cikisTutar":450.00,"adet":3}],
                                 "girisTlToplam":1200.00,"adet":3}],
                     "veresiye":{"doganTl":0,"kapananTl":0},"bilinmeyen":{"nakitPayiBilinmeyenSatir":0,"nakitPayiBilinenSatir":3},
                     "storno":{"adet":0,"tutarTl":0},"guvenilirlik":"TAM"}
                    """,
                var y when y.EndsWith("/auth/kisitlar") => "[]",
                var y when y.Contains("/tercihler/benim") => """{"tercihler":{}}""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private int Sayim(string sonEk) { lock (istekler) return istekler.Count(i => i.Yol.EndsWith(sonEk)); }

    // ── Kayıt: zayıf parola alan altında sözlük metni + Kaydet kilidi (aynı metin), POST YOK ──
    [Fact]
    public void GDE01_Kayit_zayif_parola_alan_alti_ve_kilit_ayni_metin_POST_YOK()
    {
        var e = Render<Kuyumcu.Web.Pages.Giris>();
        e.WaitForElement("#giris-yeni-dukkan", Uzun).Click();
        e.WaitForElement("#kayit-sifre", Uzun);
        Assert.Equal(ParolaKurali.EnCok.ToString(System.Globalization.CultureInfo.InvariantCulture), e.Find("#kayit-sifre").GetAttribute("maxlength"));
        e.Find("#kayit-sifre").Input("aaaaaaaa");
        e.WaitForAssertion(() => Assert.Contains(AuthSifirlaSozlugu.Tr.ParolaKurali, e.Find("#alan-hata-kayit-sifre").TextContent), Uzun);
        Assert.True(e.Find("#kayit-gonder").HasAttribute("disabled"));
        Assert.Contains(AuthSifirlaSozlugu.Tr.ParolaKurali, e.Find("#kayit-kilit").TextContent);
        e.Find("#kayit-form").Submit();
        Assert.Equal(0, Sayim("/auth/kayit"));
        e.Find("#kayit-sifre").Input("aaaa1234");
        e.WaitForAssertion(() => Assert.False(e.Find("#kayit-gonder").HasAttribute("disabled")), Uzun);
        Assert.Empty(e.FindAll("#kayit-kilit"));
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.SatisAnalizi> SatisAnalizi()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.SatisAnalizi>();
        e.WaitForElement("#sa-odeme-tablo tbody tr", Uzun);
        return e;
    }

    // ── Satış analizi: ters aralık / 401 gün → bitiş altında sunucuyla AYNI metin, istek ATILMAZ; 3000 yazılamaz ──
    [Fact]
    public void GDE02_Satis_analizi_ters_ve_uzun_donem_alan_alti_istek_ATILMAZ_3000_yili_reddedilir()
    {
        var e = SatisAnalizi();
        var once = Sayim("/raporlar/odeme-araci");
        e.Find("#sa-baslangic").Change("16.09.2026");
        var araSayim = Sayim("/raporlar/odeme-araci");
        e.Find("#sa-bitis").Change("01.09.2026");
        e.WaitForAssertion(() => Assert.Contains(Sozlukler.Tr.Kabuk.TarihAraligi, e.Find("#sa-donem-hata").TextContent), Uzun);
        Assert.Equal(araSayim, Sayim("/raporlar/odeme-araci"));
        e.Find("#sa-baslangic").Change("01.01.2025");
        e.Find("#sa-bitis").Change("06.02.2026");   // 402 gün
        e.WaitForAssertion(() => Assert.Contains(string.Format(GocDSozlugu.Tr.RaporDonemUzun, 400), e.Find("#sa-donem-hata").TextContent), Uzun);
        Assert.Equal(araSayim, Sayim("/raporlar/odeme-araci"));
        e.Find("#sa-bitis").Change("01.01.3000");
        // 3000 yılı Rapor penceresi dışında: bileşenin kendi iletisi (aynı alan altı kimliği) — istek yok
        e.WaitForAssertion(() => Assert.Contains(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.TarihGec,
            Gun: TarihPenceresi.Pencere(TarihTuru.Rapor, DateOnly.FromDateTime(Bicim.IsletmeBugun)).EnGec), "tr"),
            e.Find("#sa-donem-hata").TextContent), Uzun);
        Assert.Equal(araSayim, Sayim("/raporlar/odeme-araci"));
        Assert.True(once >= 1);
    }

    // ── Satış analizi: liste ORTAK IZGARADA (Sütunlar düğmesi, id AYNI), ham date/input YOK ──
    [Fact]
    public void GDE03_Satis_analizi_listesi_ortak_izgarada_ham_tarih_girdisi_yok()
    {
        var e = SatisAnalizi();
        Assert.Contains("izgara-tablo", e.Find("#sa-odeme-tablo").ClassName);
        Assert.NotNull(e.Find("#sa-odeme-sutunlar-ac"));
        Assert.Empty(e.FindAll("input[type=date]:not(.tarih-takvim-yerli)"));
        Assert.Equal("text", e.Find("#sa-baslangic").GetAttribute("type"));
        Assert.Equal("tarih", e.Find("#sa-bitis").GetAttribute("data-maske"));
    }
}

/// <summary>Saf: ekranın süzgeç seçenekleri sunucunun beyaz listesinin ALT kümesi (ekran 400 alacak değer gönderemez).</summary>
public sealed class GocDSuzgecEslesmeTestleri
{
    private static string[] Alan(Type t, string ad) =>
        (string[])t.GetField(ad, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.GetValue(null)!;

    [Fact]
    public void GDS07_Ekran_suzgec_secenekleri_sunucu_beyaz_listesinde()
    {
        foreach (var t in Alan(typeof(Kuyumcu.Web.Pages.IslemDefteri), "Turler")) Assert.Contains(t, Sabitler.HareketTurleri);
        foreach (var a in Alan(typeof(Kuyumcu.Web.Pages.IslemDefteri), "Araclar")) Assert.Contains(a, RaporSuzgecKurali.OdemeAraclari);
        foreach (var e in Alan(typeof(Kuyumcu.Web.Pages.DenetimIzi), "Eylemler")) Assert.Contains(e, RaporSuzgecKurali.DenetimEylemleri);
    }
}

/// <summary>Ekran (bUnit) — İşletme: şube telefonu 9 hane alan altı + Kaydet kilidi (sunucuyla AYNI metin); şube listesi ortak ızgarada.</summary>
public sealed class GocDIsletmeEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public GocDIsletmeEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<KullaniciGorunumServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "QA", "Test", "qa@ornek.com", "SAHIP", "s1"));
    }

    [Fact]
    public void GDE04_Isletme_sube_telefonu_9_hane_alan_alti_kilit_ayni_metin_POST_YOK_liste_ortak_izgarada()
    {
        var c = Render<Kuyumcu.Web.Pages.Isletme>();
        c.WaitForElement("#isletme-sube-liste-sar");
        Assert.NotNull(c.Find("#sube-liste-sutunlar-ac"));            // ortak ızgara: Sütunlar
        Assert.Equal(2, c.FindAll("[id^=s-pasife-]").Count);       // satır eylemleri AYNI kimlikle
        c.Find("#s-yeni-sube-ac").Click();
        c.WaitForElement("#cekmece-sube");
        Assert.Equal(YonetimSinirlari.SubeAdi.ToString(System.Globalization.CultureInfo.InvariantCulture), c.Find("#s-ad").GetAttribute("maxlength"));
        c.Find("#s-ad").Input("PAKETGOC-D-TEST Şube");
        c.Find("#s-telefon").Input("212555123");
        c.Find("#s-telefon").Change("212555123");
        var beklenen = GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.TelefonHane, 9, 10), "tr");
        c.WaitForAssertion(() => Assert.Equal(beklenen, c.Find("#s-telefon-hata").TextContent.Trim()));
        Assert.True(c.Find("#cekmece-sube-kaydet").HasAttribute("disabled"));
        Assert.EndsWith(beklenen, c.Find("#cekmece-sube-kilit").TextContent.Trim());
        c.Find("#cekmece-sube-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/subeler"));
    }

    [Fact]
    public void GDE05_Isletme_vergi_no_anlamsal_alan_harf_yazilamaz_11_haneyi_asamaz()
    {
        var c = Render<Kuyumcu.Web.Pages.Isletme>();
        c.WaitForElement("#i-kimlik-duzenle").Click();
        c.WaitForElement("#cekmece-isletme-kimlik");
        var v = c.Find("#ikimlik-vergino");
        Assert.Equal("rakam", v.GetAttribute("data-maske"));
        Assert.Equal("11", v.GetAttribute("maxlength"));
        v.Input("12a45");
        Assert.DoesNotContain("a", c.Find("#ikimlik-vergino").GetAttribute("value") ?? "");
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, istek.RequestUri!.PathAndQuery, govde));
            var g = yol switch
            {
                var y when y.EndsWith("/admin/tenant/kimlik") || y.EndsWith("/admin/tenant") =>
                    """{"id":"t1","ad":"QA-TEST Kuyumculuk","plan":"BASLANGIC","aiEtkin":false,"varsayilanArayuzModu":"PROFESYONEL","adres":null,"telefon":null,"vergiDairesi":null,"vergiNo":"1234567890","kurKaynagi":"ALTINKAYNAK","maliyetYontemi":"FIFO"}""",
                var y when y.EndsWith("/subeler") =>
                    """[{"id":"s1","ad":"Merkez","adres":"Çarşı","telefon":"05321112233","aktif":true},{"id":"s2","ad":"Şube 2","adres":null,"telefon":null,"aktif":true}]""",
                var y when y.EndsWith("/abonelik") =>
                    """{"planKodu":"BASLANGIC","planAdi":"Başlangıç","yazabilirMi":true,"planlar":[],"gecmis":[]}""",
                var y when y.EndsWith("/yetkiler/benim") => """{"moduller":["cari","urun"]}""",
                var y when y.EndsWith("/kullanici/gorunum", StringComparison.Ordinal) =>
                    istek.Method == HttpMethod.Patch ? B1SahteGorunumYaniti.Sarmal : B1SahteGorunumYaniti.Govde,
                var y when y.EndsWith("/gorunum", StringComparison.Ordinal) => B1SahteGorunumYaniti.Kiraci,
                var y when y.Contains("/tercihler/benim") => """{"tercihler":{}}""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
        }
    }
}
