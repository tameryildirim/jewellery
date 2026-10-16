using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Kur;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET KUR-BANT (09.10) — elle girilen has/altın/döviz fiyatında makul bant.
// Kaynak: GOC-A kapanış devri (YÜKSEK, VERİ): manuel kurda has 5 ₺ ve 99.999.999 ₺
// kaydediliyordu. Kural Core FiyatBandi (İŞ KURALI; tek kaynak): mutlak sınır
// (has 500–50.000, döviz 5–500) + son geçerli değerden ± %20. Aşım yalnız SAHİP/MÜDÜR
// onayı + gerekçe (≥ 20) ile 201 + denetim izi; TEZGAHTAR 403.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Saf: Core kuralı (sınırlar, tam sınır, önceki değer yok) ve sözlük.</summary>
public sealed class KurBantSafTestleri
{
    [Theory]
    [InlineData(6_827.20, 6_827.20)]      // aynı değer
    [InlineData(8_192.64, 6_827.20)]      // tam üst sınır (+%20) — kabul
    [InlineData(5_461.76, 6_827.20)]      // tam alt sınır (−%20) — kabul
    [InlineData(500, null)]               // önceki yok: mutlak alt sınır tam — kabul
    [InlineData(50_000, null)]            // önceki yok: mutlak üst sınır tam — kabul
    public void KB01_Has_bant_ici_ve_tam_sinir_kabul(double deger, double? onceki) =>
        Assert.Null(FiyatBandi.Denetle((decimal)deger, FiyatTuru.Has, (decimal?)onceki));

    [Fact]
    public void KB02_Has_bant_disi_onceki_degere_gore_red_ve_degerler()
    {
        var ust = FiyatBandi.Denetle(8_192.65m, FiyatTuru.Has, 6_827.20m);
        Assert.Equal(new GirdiHatasi(GirdiKodu.FiyatBantDisi, 6_827.20m, 20m), ust);
        var alt = FiyatBandi.Denetle(5_461.75m, FiyatTuru.Has, 6_827.20m);
        Assert.Equal(GirdiKodu.FiyatBantDisi, alt?.Kod);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(499.99)]
    [InlineData(50_000.01)]
    [InlineData(99_999_999)]
    public void KB03_Has_mutlak_sinir_disi_red_onceki_olsa_da(double deger)
    {
        // Mutlak sınır her zaman uygulanır (önceki değer verilse de) — fail-closed.
        Assert.Equal(new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, 500m, 50_000m),
            FiyatBandi.Denetle((decimal)deger, FiyatTuru.Has, null));
        Assert.Equal(GirdiKodu.FiyatMutlakDisi, FiyatBandi.Denetle((decimal)deger, FiyatTuru.GramAltin, 6_800m)?.Kod);
    }

    [Theory]
    [InlineData(4.99, false)]
    [InlineData(5, true)]
    [InlineData(500, true)]
    [InlineData(500.01, false)]
    public void KB04_Doviz_mutlak_sinir_onceki_yok(double deger, bool kabul)
    {
        var h = FiyatBandi.Denetle((decimal)deger, FiyatTuru.Doviz, null);
        if (kabul) Assert.Null(h); else Assert.Equal(new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, 5m, 500m), h);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    public void KB05_Bos_sifir_eksi_bu_kuralin_isi_degil(double? deger) =>
        Assert.Null(FiyatBandi.Denetle((decimal?)deger, FiyatTuru.Has, 6_800m));   // o mesaj K16'dan gelir

    [Fact]
    public void KB06_Sozluk_iki_dilde_ve_sayilar_metinde()
    {
        var tr = GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.FiyatBantDisi, 6_827.2m, 20m), "tr");
        Assert.Contains("6.827,20 TL", tr);   // tur 2: referans PARA biçimi (iki ondalık)
        Assert.Contains("(49,45 TL)", GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.FiyatBantDisi, 49.448m, 20m), "tr"));
        Assert.Contains("(6,827.20 TL)", GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.FiyatBantDisi, 6_827.2m, 20m), "en"));
        Assert.Contains("yüzde 20", tr);
        var mutlak = GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, 500m, 50_000m), "tr");
        Assert.Contains("500,00–50.000,00 TL", mutlak);   // ISLEM-BANT tur 2: mutlak sınır da para biçimi (işlem bandıyla tek biçim)
        Assert.NotEqual(tr, GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.FiyatBantDisi, 6_827.2m, 20m), "en"));
        Assert.DoesNotContain("TRY", GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, 5m, 500m), "en"));
    }

    [Theory]
    [InlineData("SAHIP", true)]
    [InlineData("MUDUR", true)]
    [InlineData("TEZGAHTAR", false)]
    [InlineData("MUHASEBE", false)]
    [InlineData("sahip", false)]          // TAM eşleşme
    [InlineData(null, false)]
    public void KB07_Bant_onayi_yalniz_SAHIP_MUDUR(string? rol, bool beklenen) =>
        Assert.Equal(beklenen, KurServisi.BantOnayYetkili(rol));

    [Fact]
    public void KB09_Yalniz_yuzde_sapma_onaylanabilir_mutlak_sinir_sert_kapi()
    {
        // KUR-BANT tur 2 (doğrulayıcı ORTA): mutlak sınır onayla AŞILAMAZ.
        Assert.True(FiyatBandi.OnaylanabilirMi(Dolu(FiyatBandi.Denetle(9_000m, FiyatTuru.Has, 6_827.2m))));
        Assert.False(FiyatBandi.OnaylanabilirMi(Dolu(FiyatBandi.Denetle(5m, FiyatTuru.Has, 6_827.2m))));
        Assert.False(FiyatBandi.OnaylanabilirMi(Dolu(FiyatBandi.Denetle(99_999_999m, FiyatTuru.Has, null))));
        Assert.False(FiyatBandi.OnaylanabilirMi(Dolu(FiyatBandi.Denetle(4m, FiyatTuru.Doviz, 41m))));
    }

    private static GirdiHatasi Dolu(GirdiHatasi? h)
    {
        Assert.NotNull(h);
        return h.GetValueOrDefault();
    }
}

/// <summary>Servis yolu (kullanıcısız iç çağrı): bant aşımı onaylanamaz (fail-closed 403).</summary>
[Collection("veritabani")]
public sealed class KurBantServisTestleri(TestVeritabani vt)
{
    [Fact]
    public async Task KB08_Kullanicisiz_yolda_onayli_asim_403_ve_kayit_yazilmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETKUR-BANT-TEST servis");
        var db = vt.Olustur(k);
        var servis = new KurServisi(db, NullLogger<KurServisi>.Instance, null!, null!,
            new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor()));
        var once = await vt.OlusturSistem().KurSnapshots.CountAsync(s => s.TenantId == k.TenantId);

        var hata = await Assert.ThrowsAsync<ForbiddenException>(() => servis.ManuelKurKaydet(new ManuelKurDto
        {
            AltinHasFiyati = 9_000m, BantOnay = true, BantGerekce = "Bu fiyat doğru, teyit ettim gerçekten.",
        }, k.TenantId));
        Assert.Equal(KurBantSozlugu.Tr.YetkiYok, hata.Message);
        Assert.Equal(once, await vt.OlusturSistem().KurSnapshots.CountAsync(s => s.TenantId == k.TenantId));
    }

    /// <summary>
    /// KUR-BANT tur 2 (doğrulayıcı DÜŞÜK): servis İÇİNDEKİ rol kapısı uç filtresinden BAĞIMSIZ sınanır —
    /// TEZGAHTAR/MUHASEBE kullanıcısıyla servis doğrudan çağrılır ([RolGerekli] devrede değil) → 403, kayıt/iz yok.
    /// SAHİP aynı çağrıda 201 alır (kapının yalnız rolü ayırdığını kanıtlar).
    /// </summary>
    [Theory]
    [InlineData("TEZGAHTAR")]
    [InlineData("MUHASEBE")]
    public async Task KB09b_Servis_rol_kapisi_TEZGAHTAR_dogrudan_cagride_403(string rol)
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETKUR-BANT-TEST servis rol " + rol);
        var db = vt.Olustur(k);
        var servis = new KurServisi(db, NullLogger<KurServisi>.Instance, null!, null!,
            new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor()));
        var once = await vt.OlusturSistem().KurSnapshots.CountAsync(s => s.TenantId == k.TenantId);
        var dto = new ManuelKurDto
        {
            AltinHasFiyati = 9_000m, BantOnay = true, BantGerekce = "Bu fiyat doğru, teyit ettim gerçekten.",
        };
        var hata = await Assert.ThrowsAsync<ForbiddenException>(() =>
            servis.ManuelKurKaydet(dto, new AktifKullanici(k.KullaniciId, k.TenantId, null, rol)));
        Assert.Equal(KurBantSozlugu.Tr.YetkiYok, hata.Message);
        Assert.Equal(once, await vt.OlusturSistem().KurSnapshots.CountAsync(s => s.TenantId == k.TenantId));

        var ok = await servis.ManuelKurKaydet(dto, new AktifKullanici(k.KullaniciId, k.TenantId, null, "SAHIP"));
        Assert.Equal(9_000m, ok.AltinHasFiyati);
    }

    /// <summary>KUR-BANT tur 2: SAHİP onay + gerekçe ile de mutlak sınır dışı → 400 (sert kapı), kayıt ve iz yok.</summary>
    [Theory]
    [InlineData(5)]
    [InlineData(99_999_999)]
    public async Task KB09c_Servis_SAHIP_onayli_mutlak_disi_400(int deger)
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETKUR-BANT-TEST servis sert " + deger);
        var db = vt.Olustur(k);
        var servis = new KurServisi(db, NullLogger<KurServisi>.Instance, null!, null!,
            new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor()));
        var once = await vt.OlusturSistem().KurSnapshots.CountAsync(s => s.TenantId == k.TenantId);
        await Assert.ThrowsAsync<BadRequestException>(() => servis.ManuelKurKaydet(new ManuelKurDto
        {
            AltinHasFiyati = deger, BantOnay = true, BantGerekce = "Bu fiyat doğru, teyit ettim gerçekten.",
        }, new AktifKullanici(k.KullaniciId, k.TenantId, null, "SAHIP")));
        Assert.Equal(once, await vt.OlusturSistem().KurSnapshots.CountAsync(s => s.TenantId == k.TenantId));
        Assert.False(await vt.OlusturSistem().DenetimKayitlari.AnyAsync(d => d.TenantId == k.TenantId && d.VarlikTuru == "KurBantAsimi"));
    }
}

/// <summary>HTTP: 400 alanlı + sözlük metni, SAHİP/MÜDÜR onaylı 201 + denetim izi, TEZGAHTAR 403 (gerçek MySQL).</summary>
[Collection("http")]
public sealed class KurBantHttpTestleri(HttpFikstur f)
{
    private const string Gerekce = "Piyasa bugün sert oynadı, fiyatı odadan teyit ettim.";

    private static string M(GirdiKodu kod, decimal? s1 = null, decimal? s2 = null) =>
        GirdiSozlugu.Mesaj(new GirdiHatasi(kod, s1, s2), "tr");

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETKUR-BANT-TEST " + ad);   // fikstür has 6.827,20 (MANUAL)
    }

    private int Sayi(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.KurSnapshots.AsNoTracking().Count(s => s.TenantId == k.TenantId);
    }

    private static async Task<string> RedMesaj(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), govde);
        var p = alanlar.EnumerateObject().FirstOrDefault(x => x.Name.Equals(alan, StringComparison.OrdinalIgnoreCase));
        Assert.True(p.Value.ValueKind == JsonValueKind.Array, $"'{alan}' işaretli değil: {govde}");
        return p.Value[0].GetString() ?? "";
    }

    private static StringContent Ham(string json) => new(json, Encoding.UTF8, "application/json");

    [Fact]
    public async Task KB10_Hasim_gecersiz_degerler_400_alanli_ve_kayit_yazilmaz()
    {
        var k = YeniKiraci("hasim");
        var c = f.Ortam.Istemci(k);
        var once = Sayi(k);

        Assert.Equal(M(GirdiKodu.FiyatMutlakDisi, 500m, 50_000m),
            await RedMesaj(await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 5m }), "altinHasFiyati"));
        Assert.Equal(M(GirdiKodu.FiyatMutlakDisi, 500m, 50_000m),
            await RedMesaj(await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 99_999_999m }), "altinHasFiyati"));
        Assert.Equal(M(GirdiKodu.SayiSifir),
            await RedMesaj(await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 0m }), "altinHasFiyati"));
        Assert.Equal(M(GirdiKodu.SayiEksi),
            await RedMesaj(await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = -6_800m }), "altinHasFiyati"));
        // 1e10: kolon tavanı (K16) önce yakalar — yine 400, yine alan.
        await RedMesaj(await c.PostAsync("/api/v1/kur/manuel", Ham("""{"altinHasFiyati":1e10}""")), "altinHasFiyati");
        // harf: JSON çözülemez → 400 (alan işaretli).
        var harf = await c.PostAsync("/api/v1/kur/manuel", Ham("""{"altinHasFiyati":"altıbin"}"""));
        Assert.Equal(HttpStatusCode.BadRequest, harf.StatusCode);
        // döviz: önceki değer yok → mutlak 5–500
        Assert.Equal(M(GirdiKodu.FiyatMutlakDisi, 5m, 500m),
            await RedMesaj(await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 6_800m, usdTry = 4.99m }), "usdTry"));
        Assert.Equal(M(GirdiKodu.FiyatMutlakDisi, 5m, 500m),
            await RedMesaj(await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 6_800m, gbpTry = 1_000m }), "gbpTry"));

        Assert.Equal(once, Sayi(k));
    }

    [Fact]
    public async Task KB11_Onceki_degere_gore_bant_tam_sinir_kabul_bir_kurus_fazlasi_red()
    {
        var k = YeniKiraci("tam sinir");
        var c = f.Ortam.Istemci(k);
        // 6.827,20 × 1,2 = 8.192,64
        Assert.Equal(M(GirdiKodu.FiyatBantDisi, 6_827.20m, 20m),
            await RedMesaj(await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 8_192.65m }), "altinHasFiyati"));
        var ok = await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 8_192.64m });
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        // Yeni referans 8.192,64 → 6.500 artık bandın altında (6.554,11 sınırı)
        Assert.Equal(M(GirdiKodu.FiyatBantDisi, 8_192.64m, 20m),
            await RedMesaj(await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 6_500m }), "altinHasFiyati"));
    }

    [Fact]
    public async Task KB12_Iptal_edilen_kayit_referans_olmaz_ve_alan_basina_son_dolu_deger()
    {
        var k = YeniKiraci("referans");
        var c = f.Ortam.Istemci(k);
        // Eski kayıtta USD 40, son kayıtta USD yok → USD referansı 40 (mutlak 5–500 DEĞİL)
        using (var db = f.Vt.OlusturSistem())
        {
            db.KurSnapshots.Add(new KurSnapshot
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, AltinHasFiyati = 6_800m, UsdTry = 40m,
                Kaynak = "MANUAL", Olusturuldu = DateTime.UtcNow.AddDays(-2),
            });
            db.SaveChanges();
        }
        Assert.Equal(M(GirdiKodu.FiyatBantDisi, 40m, 20m),
            await RedMesaj(await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 6_800m, usdTry = 60m }), "usdTry"));

        // 8.000 gir (bant içi), iptal et → referans yine 6.827,20: 6.000 kabul (8.000'e göre olsaydı red)
        var r = await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 8_000m });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var id = (await r.Json()).GetProperty("id").GetString()!;
        var ip = await c.PostJson($"/api/v1/kur/manuel/{id}/iptal", new { gerekce = "Yanlış girildi, rakam hatalı yazıldı." });
        Assert.True(ip.IsSuccessStatusCode, await ip.Content.ReadAsStringAsync());
        var ok = await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 6_000m });
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
    }

    [Theory]
    [InlineData("SAHIP")]
    [InlineData("MUDUR")]
    public async Task KB13_Yetkili_onayli_asim_201_ve_denetim_izi(string rol)
    {
        var k = YeniKiraci("onay " + rol);
        var c = f.Ortam.Istemci(k, rol);

        // Onay var, gerekçe kısa → 400 bantGerekce (K30)
        Assert.Equal(M(GirdiKodu.GerekceKisa, 20m, 5m),
            await RedMesaj(await c.PostJson("/api/v1/kur/manuel",
                new { altinHasFiyati = 9_000m, bantOnay = true, bantGerekce = "doğru" }), "bantGerekce"));

        var r = await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 9_000m, bantOnay = true, bantGerekce = Gerekce });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var id = (await r.Json()).GetProperty("id").GetString()!;

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(9_000m, db.KurSnapshots.AsNoTracking().Single(s => s.Id == id).AltinHasFiyati);
        var iz = db.DenetimKayitlari.AsNoTracking()
            .Single(d => d.TenantId == k.TenantId && d.VarlikTuru == "KurBantAsimi");
        Assert.Equal(id, iz.VarlikId);
        Assert.Equal(k.KullaniciId, iz.KullaniciId);
        Assert.True(iz.Hassas);
        Assert.Contains("altinHasFiyati=9000", iz.Detay);
        Assert.Contains("son geçerli 6827.2", iz.Detay);
        Assert.Contains(Gerekce, iz.Detay);
    }

    [Fact]
    public async Task KB14_Bant_ici_onaysiz_kayitta_denetim_izi_yazilmaz()
    {
        var k = YeniKiraci("bant ici");
        var c = f.Ortam.Istemci(k);
        var r = await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 7_000m, usdTry = 41.5m });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.False(db.DenetimKayitlari.AsNoTracking().Any(d => d.TenantId == k.TenantId && d.VarlikTuru == "KurBantAsimi"));
    }

    [Theory]
    [InlineData("TEZGAHTAR")]
    [InlineData("MUHASEBE")]
    public async Task KB15_Yetkisiz_rol_onayli_asim_403_kayit_yok(string rol)
    {
        var k = YeniKiraci("yetkisiz " + rol);
        var c = f.Ortam.Istemci(k, rol);
        var once = Sayi(k);
        var r = await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 9_000m, bantOnay = true, bantGerekce = Gerekce });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal(once, Sayi(k));
    }

    /// <summary>KUR-BANT tur 2 (doğrulayıcı ORTA): SAHİP/MÜDÜR onay + gerekçeyle de mutlak sınır AŞILAMAZ → 400 alanlı.</summary>
    [Theory]
    [InlineData("SAHIP", "altinHasFiyati", 5)]
    [InlineData("MUDUR", "altinHasFiyati", 99_999_999)]
    [InlineData("SAHIP", "usdTry", 4.99)]
    [InlineData("SAHIP", "gbpTry", 1_000)]
    public async Task KB17_Onayli_mutlak_sinir_disi_400_sert_kapi(string rol, string alan, double deger)
    {
        var k = YeniKiraci("sert " + rol + alan);
        var c = f.Ortam.Istemci(k, rol);
        var once = Sayi(k);
        var govde = alan == "altinHasFiyati"
            ? (object)new { altinHasFiyati = (decimal)deger, bantOnay = true, bantGerekce = Gerekce }
            : alan == "usdTry"
                ? new { altinHasFiyati = 6_800m, usdTry = (decimal)deger, bantOnay = true, bantGerekce = Gerekce }
                : new { altinHasFiyati = 6_800m, gbpTry = (decimal)deger, bantOnay = true, bantGerekce = Gerekce };
        var beklenen = alan == "altinHasFiyati" ? M(GirdiKodu.FiyatMutlakDisi, 500m, 50_000m) : M(GirdiKodu.FiyatMutlakDisi, 5m, 500m);
        Assert.Equal(beklenen, await RedMesaj(await c.PostJson("/api/v1/kur/manuel", govde), alan));
        Assert.Equal(once, Sayi(k));
        using var db = f.Vt.OlusturSistem();
        Assert.False(db.DenetimKayitlari.AsNoTracking().Any(d => d.TenantId == k.TenantId && d.VarlikTuru == "KurBantAsimi"));
    }

    [Fact]
    public async Task KB16_Onay_bant_ici_degerde_iz_birakmaz_ve_onaysiz_asim_her_alanda_400()
    {
        var k = YeniKiraci("alanlar");
        var c = f.Ortam.Istemci(k);
        // gram altın: önceki 6.861,51
        Assert.Equal(M(GirdiKodu.FiyatBantDisi, 6_861.51m, 20m),
            await RedMesaj(await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 6_800m, gramAltinFiyati = 9_000m }), "gramAltinFiyati"));
        Assert.Equal(M(GirdiKodu.FiyatMutlakDisi, 5m, 500m),
            await RedMesaj(await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 6_800m, eurTry = 0.5m }), "eurTry"));
        // Onay bayrağı bant içi değerde etkisiz: 201, iz yok.
        var r = await c.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 6_900m, bantOnay = true, bantGerekce = Gerekce });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.False(db.DenetimKayitlari.AsNoTracking().Any(d => d.TenantId == k.TenantId && d.VarlikTuru == "KurBantAsimi"));
    }
}

/// <summary>Ekran: alan altı AYNI metin, onay yolu yalnız SAHİP/MÜDÜR, gövdede onay + gerekçe.</summary>
public sealed class KurBantEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public KurBantEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, yol, govde));
            var g = yol switch
            {
                var y when y.EndsWith("/kur/manuel") =>
                    """{"id":"kur-2","altinHasFiyati":9000,"kaynak":"MANUAL","olusturuldu":"2026-10-09T10:00:00Z","ayarFiyatlari":[]}""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"MANUAL","olusturuldu":"2026-10-09T09:00:00Z","ayarFiyatlari":[]}""",
                var y when y.EndsWith("/kur/gecmis") => "[]",
                var y when y.Contains("/kur/pariteler") => """{"pariteler":[]}""",
                var y when y.EndsWith("/kur/kaynaklar") => """{"kaynaklar":[],"varsayilan":"MANUAL","secili":"MANUAL","sozlesmesizKaynaklarAcik":false,"seciliSozlesmesiz":false,"manuelKod":"MANUAL"}""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(g, Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Kur> Ekran(string rol)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1"));
        var e = Render<Kuyumcu.Web.Pages.Kur>();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/kur/guncel")));
        e.Find("#kur-manuel-ac").Click();
        lock (istekler) istekler.Clear();
        return e;
    }

    private int ManuelPost() { lock (istekler) return istekler.Count(i => i.Metot == "POST" && i.Yol.EndsWith("/kur/manuel")); }

    [Fact]
    public void KB20_Bant_disi_has_POST_YOK_alan_alti_sunucuyla_AYNI_metin()
    {
        var e = Ekran("SAHIP");
        Assert.Empty(e.FindAll("#m-bant-onay"));          // aşım yokken onay yolu görünmez
        e.Find("#m-has").Input("99999999");
        e.Find("#m-kaydet").Click();
        Assert.Equal(0, ManuelPost());
        var beklenen = GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, 500m, 50_000m), "tr");
        Assert.Contains(beklenen, e.Find("#m-has-sar").TextContent);
        // KUR-BANT tur 2: mutlak sınır dışı SERT kapı — SAHİP'e de onay yolu ÇİZİLMEZ; bant "formu düzeltin" der.
        Assert.Empty(e.FindAll("#m-bant-onay"));
        Assert.DoesNotContain(KurBantSozlugu.Tr.OnayGerekli, e.Markup);
        Assert.Contains(Kuyumcu.Core.I18n.Sozlukler.Tr.Kabuk.FormEksik, e.Markup);
    }

    [Fact]
    public void KB21_Onceki_degere_gore_bant_metni_ve_onay_gerekce_kisa_POST_YOK()
    {
        var e = Ekran("MUDUR");
        e.Find("#m-has").Input("9000");                   // 6.800 × 1,2 = 8.160 üstü
        e.Find("#m-kaydet").Click();
        Assert.Contains(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.FiyatBantDisi, 6_800m, 20m), "tr"), e.Markup);
        e.Find("#m-bant-onay").Change(true);
        e.Find("#m-bant-gerekce").Input("kısa");
        e.Find("#m-kaydet").Click();
        Assert.Equal(0, ManuelPost());
        Assert.Contains(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.GerekceKisa, 20m, 4m), "tr"), e.Markup);
        // Onay işaretliyken de neden alanın altında kalır (kullanıcı neyi onayladığını görür).
        Assert.Contains(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.FiyatBantDisi, 6_800m, 20m), "tr"),
            e.Find("#m-has-sar").TextContent);
    }

    [Fact]
    public void KB22_Onay_ve_gerekce_ile_govdede_bantOnay_ve_gerekce_gider()
    {
        var e = Ekran("SAHIP");
        e.Find("#m-has").Input("9000");
        e.Find("#m-kaydet").Click();
        e.Find("#m-bant-onay").Change(true);
        e.Find("#m-bant-gerekce").Input("Piyasa bugün sert oynadı, teyit ettim.");
        e.Find("#m-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(1, ManuelPost()));
        string govde; lock (istekler) govde = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/kur/manuel")).Govde;
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.GetProperty("bantOnay").GetBoolean());
        Assert.Equal("Piyasa bugün sert oynadı, teyit ettim.", j.GetProperty("bantGerekce").GetString());
        e.WaitForAssertion(() => Assert.Contains(KurBantSozlugu.Tr.OnayliKaydedildi, e.Markup));
    }

    [Fact]
    public void KB23_Bant_ici_kayitta_onay_alanlari_govdede_bos()
    {
        var e = Ekran("SAHIP");
        e.Find("#m-has").Input("7000");
        e.Find("#m-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(1, ManuelPost()));
        string govde; lock (istekler) govde = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/kur/manuel")).Govde;
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.Equal(JsonValueKind.Null, j.GetProperty("bantOnay").ValueKind);
    }

    [Fact]
    public void KB24_Tezgahtar_onay_yolu_gormez_neden_yazar_POST_YOK()
    {
        var e = Ekran("TEZGAHTAR");
        e.Find("#m-has").Input("9000");                   // ± %20 dışı (onaylanabilir sınıf)
        e.Find("#m-kaydet").Click();
        Assert.Equal(0, ManuelPost());
        Assert.Empty(e.FindAll("#m-bant-onay"));
        var yetki = e.Find("#m-bant-yetki");
        Assert.Equal(KurBantSozlugu.Tr.YetkiYok, yetki.TextContent.Trim());
        Assert.Contains("metin-kucuk", yetki.ClassName);
        // KUR-BANT tur 2 (tasarım hakemi KRİTİK): yetki cümlesi ekranda TEK KEZ (hata bandında tekrar yok).
        var say = System.Text.RegularExpressions.Regex.Matches(e.Markup,
            System.Text.RegularExpressions.Regex.Escape(KurBantSozlugu.Tr.YetkiYok)).Count;
        Assert.Equal(1, say);
        Assert.Contains(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.FiyatBantDisi, 6_800m, 20m), "tr"), e.Find("#m-has-sar").TextContent);
    }

    [Fact]
    public void KB25_Tezgahtar_mutlak_disi_yetki_cumlesi_yok_yalniz_neden_ve_bant()
    {
        var e = Ekran("TEZGAHTAR");
        e.Find("#m-usd").Input("4");
        e.Find("#m-kaydet").Click();
        Assert.Equal(0, ManuelPost());
        Assert.Empty(e.FindAll("#m-bant-onay"));
        Assert.Empty(e.FindAll("#m-bant-yetki"));         // sert kapıyı kimse onaylayamaz → "sahibe söyle" yanıltıcı olurdu
        Assert.Contains(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, 5m, 500m), "tr"), e.Markup);
        Assert.Contains(Kuyumcu.Core.I18n.Sozlukler.Tr.Kabuk.FormEksik, e.Markup);
    }
}
