using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Core.Domain;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MEvrak = Kuyumcu.Api.Modules.Evrak;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET GOC-B finansal sınır testleri (08.10.2026).
// GOC-B evrak tutarına (K9 servis kapısı), MASAK şüpheli işlem tutarına (K9) ve evrak
// düzenleme PATCH gövdesine dokundu. Beklenen değerler SABİTTİR (üretim kodundan hesaplanmaz).
// Proje kuralı (GOC-A/GOC-B): fazla ondalık SESSİZCE YUVARLANMAZ — 400; 0,005 reddedilir.
// olumsuz-fikstür: dosya — geçersiz değerler bilinçlidir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GOCBSinirSafTestleri
{
    [Fact]
    public void GOCB_S01_Tutar_sifir_eksi_kurus_alti_tavan_null()
    {
        Assert.Equal(GirdiKodu.SayiSifir, TutarKurali.Denetle(0m)!.Value.Kod);
        Assert.Equal(GirdiKodu.SayiEksi, TutarKurali.Denetle(-0.01m)!.Value.Kod);
        Assert.Equal(GirdiKodu.SayiOndalik, TutarKurali.Denetle(0.005m)!.Value.Kod);     // yuvarlanmaz, reddedilir
        Assert.Equal(GirdiKodu.SayiOndalik, TutarKurali.Denetle(10.005m)!.Value.Kod);
        Assert.Null(TutarKurali.Denetle(0.01m));
        Assert.Null(TutarKurali.Denetle(999_999_999.99m));
        Assert.Null(TutarKurali.Denetle(null));
        var ex = Assert.Throws<BadRequestException>(() => GirdiKapisi.Tutar(0m, "tutar"));
        Assert.Equal("Değer 0'dan büyük olmalı.", ex.Message);
    }

    [Fact]
    public void GOCB_S02_Has_dort_hane_kabul_yedi_hane_red()
    {
        Assert.Null(AgirlikKurali.Has(0.0001m));
        Assert.Equal(GirdiKodu.SayiOndalik, AgirlikKurali.Has(0.0000001m)!.Value.Kod);
    }

    [Fact]
    public void GOCB_S03_Turkce_giris_45_000_ve_1_5_cozulur_belirsiz_1_500_reddedilir()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        var c = FormAlani.SayiCoz("1.500");
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, c.Sonuc);
        Assert.Null(c.Deger);
        Assert.Equal(999_999_999.99m, FormAlani.SayiCoz("999.999.999,99").Deger);
    }

    [Fact]
    public void GOCB_S04_Keside_vade_ayni_gun_gecer_bir_gun_sonra_red()
    {
        var bugun = new DateOnly(2026, 10, 8);
        var vade = new DateOnly(2026, 11, 1);
        MEvrak.EvrakServisi.TarihleriDenetle(vade, vade, bugun);                 // keşide = vade geçer
        var ex = Assert.Throws<BadRequestException>(() => MEvrak.EvrakServisi.TarihleriDenetle(vade, vade.AddDays(1), bugun));
        Assert.Equal("kesideTarihi", AlanIsareti.Alan(ex));
    }
}

/// <summary>HTTP: evrak ve şüpheli işlem tutarının sunucu sınırları (gerçek MySQL).</summary>
[Collection("http")]
public sealed class GOCBSinirSunucuTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETGOC-B-TEST dg " + ad);
    }

    private static Dictionary<string, object?> Evrak(Kiraci k, string no, decimal tutar) => new()
    {
        ["evrakTuru"] = "CEK", ["tutar"] = tutar, ["vadeGunu"] = Zaman.YerelBugun().AddDays(20).ToString("yyyy-MM-dd"),
        ["evrakNo"] = no, ["bankaAdi"] = "Ziraat", ["kaynakCariId"] = k.CariId, ["kurSnapshotId"] = k.KurId,
    };

    [Fact]
    public async Task GOCB_H01_Evrak_tutar_sifir_eksi_ondalik_400_kurus_ve_buyuk_tutar_birebir_yazilir()
    {
        var k = YeniKiraci("evrak tutar");
        var c = f.Ortam.Istemci(k);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostJson("/api/v1/evraklar", Evrak(k, "S-0", 0m))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostJson("/api/v1/evraklar", Evrak(k, "S-1", -5m))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostJson("/api/v1/evraklar", Evrak(k, "S-2", 0.005m))).StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(0, db.KiymetliEvraklar.AsNoTracking().Count(e => e.TenantId == k.TenantId));

        var ok = await c.PostJson("/api/v1/evraklar", Evrak(k, "S-3", 0.01m));
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        var buyuk = await c.PostJson("/api/v1/evraklar", Evrak(k, "S-4", 999_999_999.99m));
        Assert.True(buyuk.IsSuccessStatusCode, "999.999.999,99 TL evrak: " + await buyuk.Content.ReadAsStringAsync());
        using (var db = f.Vt.OlusturSistem())
        {
            var t = db.KiymetliEvraklar.AsNoTracking().Where(e => e.TenantId == k.TenantId).Select(e => e.Tutar).ToList();
            Assert.Contains(0.01m, t);
            Assert.Contains(999_999_999.99m, t);
        }
    }

    [Fact]
    public async Task GOCB_H03_Evrak_ekranin_gercek_PATCH_govdesi_kayitli_borclu_kimlik_noyu_KORUMALI()
    {
        var k = YeniKiraci("evrak kimlik");
        var c = f.Ortam.Istemci(k);
        var g = Evrak(k, "K-1", 1000m);
        g["borcluKimlikNo"] = "10000000146";
        var ok = await c.PostJson("/api/v1/evraklar", g);
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        string id;
        using (var db = f.Vt.OlusturSistem())
        {
            var e = db.KiymetliEvraklar.AsNoTracking().Single(x => x.TenantId == k.TenantId);
            id = e.Id;
            Assert.Equal("10000000146", e.BorcluKimlikNo);
        }
        // Evraklar.razor düzenleme çekmecesinin YALNIZ NOT değiştirildiğinde gönderdiği gövdenin birebir kopyası
        // (bUnit GOCB_E01 ile yakalandı): borcluKimlikNo = null.
        var p = await c.PatchJson($"/api/v1/evraklar/{id}", new Dictionary<string, object?>
        {
            ["vadeGunu"] = Zaman.YerelBugun().AddDays(20).ToString("yyyy-MM-dd"), ["evrakNo"] = "K-1", ["bankaAdi"] = "Ziraat",
            ["bankaSube"] = null, ["kesideYeri"] = null, ["kesideTarihi"] = null, ["borcluAd"] = null,
            ["borcluKimlikNo"] = null, ["notlar"] = "yalnız not değişti",
        });
        Assert.True(p.IsSuccessStatusCode, await p.Content.ReadAsStringAsync());
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal("10000000146", db.KiymetliEvraklar.AsNoTracking().Single(x => x.Id == id).BorcluKimlikNo);
    }

    [Fact]
    public async Task GOCB_H02_Supheli_tutar_kurus_ve_buyuk_tutar_birebir()
    {
        var k = YeniKiraci("supheli tutar");
        var c = f.Ortam.Istemci(k);
        var g = new string('a', 320);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostJson("/api/v1/masak/supheli", new { islemTutariTl = 0.005m, gerekce = g })).StatusCode);
        var ok1 = await c.PostJson("/api/v1/masak/supheli", new { islemTutariTl = 0.01m, gerekce = g });
        Assert.True(ok1.IsSuccessStatusCode, await ok1.Content.ReadAsStringAsync());
        var ok2 = await c.PostJson("/api/v1/masak/supheli", new { islemTutariTl = 999_999_999.99m, gerekce = g });
        Assert.True(ok2.IsSuccessStatusCode, await ok2.Content.ReadAsStringAsync());
        using var db = f.Vt.OlusturSistem();
        var t = db.MasakSupheliBildirimler.AsNoTracking().Where(x => x.TenantId == k.TenantId).Select(x => x.IslemTutariTl).ToList();
        Assert.Contains(0.01m, t);
        Assert.Contains(999_999_999.99m, t);
    }
}

/// <summary>Ekran: evrak düzenlemede kimliğe DOKUNULMADAN kaydedilince borçlu kimlik no silinmemeli.</summary>
public sealed class GOCBSinirEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string EvrakJson = """
        {"id":"e1","evrakTuru":"CEK","yon":"ALINAN","durum":"PORTFOYDE","tutar":1000,"paraBirimi":"TRY","vadeGunu":"2027-01-15",
         "evrakNo":"C-1","bankaAdi":"Ziraat","bankaSube":null,"kesideYeri":null,"kesideTarihi":null,"borcluCariId":null,
         "borcluAd":"Ali Veli","kaynakCariId":"c1","kaynakCariAd":"Ayşe Yılmaz","notlar":null,"gecikmeGun":0,"gecikmis":false,
         "yakindaVadeli":false,"duzenlenebilir":true,"izinliGecisler":["TAHSILE_VERILDI"]}
        """;

    public GOCBSinirEkranTestleri()
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
                    "\"portfoydeTutar\":1000,\"tahsileVerilenTutar\":0,\"buHaftaVadeliTutar\":0,\"karsiliksizTutar\":0,\"satirlar\":[" + EvrakJson + "]}",
                var y when y.Contains("/evraklar/e1") => "{\"evrak\":" + EvrakJson + "}",
                var y when y.EndsWith("/cari") => """
                    [{"id":"c1","ad":"Ayşe Yılmaz","tur":"MUSTERI","telefon":null,"email":null,"tlBakiye":0,"hasGramBakiye":0,
                      "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"riskSeviye":null,"pepBeyani":false}]
                    """,
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"kaynak":"MANUAL","olusturuldu":"2026-10-08T08:00:00Z","ayarFiyatlari":[]}""",
                var y when y.Contains("/tercihler/benim") => """{"tercihler":{}}""",
                var y when y.Contains("/auth/ben") =>
                    """{"id":"u1","ad":"Test","soyad":"Kullanici","email":"t@t.local","rol":"SAHIP","subeId":"sb1"}""",
                var y when y.Contains("/bankalar") || y.Contains("/kasalar") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    [Fact]
    public void GOCB_E01_Evrak_yalniz_not_duzenlenince_PATCH_borclu_kimlik_noyu_null_ile_SILMEZ()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1"));
        var e = Render<Kuyumcu.Web.Pages.Evraklar>().AyrintiAc();
        e.WaitForElement("#evrak-satir-e1");
        e.Find("#evrak-duzenle-e1").Click();
        e.WaitForElement("#evrak-form-notlar");
        e.Find("#evrak-form-notlar").Input("yalnız not değişti");
        e.WaitForAssertion(() => Assert.False(e.Find("#cekmece-evrak-kaydet").HasAttribute("disabled")));
        e.Find("#cekmece-evrak-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            lock (istekler)
            {
                var p = istekler.Last(i => i.Metot == "PATCH");
                var j = JsonDocument.Parse(p.Govde).RootElement;
                // Sunucu sözleşmesi: alan gövdede null ise SİL. Liste ucu kimliği döndürmediği için form kimliği
                // bilmez; dokunulmamış alan gönderilirse kayıtlı borçlu kimlik no sessizce silinir.
                Assert.False(j.TryGetProperty("borcluKimlikNo", out var v) && v.ValueKind == JsonValueKind.Null,
                    "PATCH gövdesi borcluKimlikNo:null taşıyor → kayıtlı kimlik silinir: " + p.Govde);
            }
        });
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI TUR 2 (08.10.2026 ~23:00) — GOC-B tur 2 değişikliklerinin sınırları:
// K8f KisiDogum penceresi, Coz ayrıştırıcısı, TcknVknVeyaPasaport, K9 tavan sınırı ve
// evrak kimlik YALNIZ-YAZILIR sözleşmesinin boş/boşluk/sil=false kenarları. Beklenenler SABİT.
// olumsuz-fikstür: dosya — geçersiz değerler bilinçlidir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GOCBSinirTur2SafTestleri
{
    private static readonly DateOnly Bugun = new(2026, 10, 8);

    [Fact]
    public void GOCB_T2_S01_KisiDogum_bugun_gecer_yarin_gelecek_120_yil_tam_sinir_gecer_bir_gun_fazlasi_red()
    {
        Assert.Null(TarihPenceresi.Denetle(Bugun, TarihTuru.KisiDogum, Bugun));
        Assert.Equal(GirdiKodu.DogumGelecek, TarihPenceresi.Denetle(new DateOnly(2026, 10, 9), TarihTuru.KisiDogum, Bugun)!.Value.Kod);
        Assert.Null(TarihPenceresi.Denetle(new DateOnly(1906, 10, 8), TarihTuru.KisiDogum, Bugun));
        Assert.Equal(GirdiKodu.DogumCokEski, TarihPenceresi.Denetle(new DateOnly(1906, 10, 7), TarihTuru.KisiDogum, Bugun)!.Value.Kod);
        Assert.Equal(GirdiKodu.DogumCokEski, TarihPenceresi.Denetle(new DateOnly(1, 1, 1), TarihTuru.KisiDogum, Bugun)!.Value.Kod);
        Assert.Null(TarihPenceresi.Denetle(null, TarihTuru.KisiDogum, Bugun));
        // 18 yaş: ENGEL DEĞİL, yalnız uyarı yardımcısı — tam 18. doğum günü reşit, bir gün sonrası değil
        Assert.Null(TarihPenceresi.Denetle(new DateOnly(2009, 10, 9), TarihTuru.KisiDogum, Bugun));
        Assert.False(TarihPenceresi.ResitDegil(new DateOnly(2008, 10, 8), Bugun));
        Assert.True(TarihPenceresi.ResitDegil(new DateOnly(2008, 10, 9), Bugun));
        Assert.False(TarihPenceresi.ResitDegil(new DateOnly(2026, 10, 9), Bugun));   // gelecek → uyarı değil, hata yolu
        Assert.False(TarihPenceresi.ResitDegil(null, Bugun));
    }

    [Fact]
    public void GOCB_T2_S02_Coz_bitisik_noktali_makine_bicimi_subat_29_31_ve_bozuk_yazim()
    {
        Assert.Equal(new DateOnly(2026, 10, 8), TarihPenceresi.Coz("08102026").Deger);
        Assert.Equal(new DateOnly(2026, 10, 8), TarihPenceresi.Coz("08.10.2026").Deger);
        Assert.Equal(new DateOnly(2026, 10, 8), TarihPenceresi.Coz("2026-10-08").Deger);
        Assert.Equal(new DateOnly(2024, 2, 29), TarihPenceresi.Coz("29.02.2024").Deger);
        Assert.Equal(GirdiKodu.TarihGecersiz, TarihPenceresi.Coz("29.02.2025").Hata!.Value.Kod);
        Assert.Equal(GirdiKodu.TarihGecersiz, TarihPenceresi.Coz("31.02.2024").Hata!.Value.Kod);
        Assert.Equal(GirdiKodu.TarihGecersiz, TarihPenceresi.Coz("2026-13-01").Hata!.Value.Kod);
        Assert.Equal(GirdiKodu.TarihGecersiz, TarihPenceresi.Coz("0000-01-01").Hata!.Value.Kod);
        Assert.Equal(GirdiKodu.TarihBicim, TarihPenceresi.Coz("8.10.2026").Hata!.Value.Kod);
        Assert.Equal(GirdiKodu.TarihBicim, TarihPenceresi.Coz("08.10.20266").Hata!.Value.Kod);
        Assert.Equal(GirdiKodu.TarihBicim, TarihPenceresi.Coz("08.1O.2026").Hata!.Value.Kod);   // harf O
        Assert.Equal((null, null), TarihPenceresi.Coz("   "));
        // 3000 yılı çözülür ama pencere reddeder (K8 Serbest 1900..2100)
        Assert.Equal(GirdiKodu.TarihGec, TarihPenceresi.Denetle(TarihPenceresi.Coz("01.01.3000").Deger, TarihTuru.Serbest, Bugun)!.Value.Kod);
    }

    [Fact]
    public void GOCB_T2_S03_TcknVknVeyaPasaport_rakam_ve_harf_kollari()
    {
        Assert.Null(KimlikKurali.TcknVknVeyaPasaport("10000000146"));
        Assert.Null(KimlikKurali.TcknVknVeyaPasaport("1234567890"));        // VKN 10 hane (kontrol hanesi TEYİTSİZ)
        Assert.Null(KimlikKurali.TcknVknVeyaPasaport(" u1234567x "));
        Assert.Null(KimlikKurali.TcknVknVeyaPasaport("   "));
        Assert.Equal(GirdiKodu.TcknGecersiz, KimlikKurali.TcknVknVeyaPasaport("11111111111")!.Value.Kod);
        Assert.Equal(GirdiKodu.KimlikHaneIkili, KimlikKurali.TcknVknVeyaPasaport("123456789012")!.Value.Kod);
        Assert.Equal(GirdiKodu.KimlikHaneIkili, KimlikKurali.TcknVknVeyaPasaport("123456789")!.Value.Kod);
        Assert.Equal(GirdiKodu.PasaportBicim, KimlikKurali.TcknVknVeyaPasaport("AB 12345")!.Value.Kod);
        Assert.Equal(GirdiKodu.PasaportBicim, KimlikKurali.TcknVknVeyaPasaport("ÇA12345")!.Value.Kod);
        Assert.Null(KimlikKurali.TcknVknVeyaPasaport("A1234"));             // tam 5 karakter alt sınır geçer
        Assert.Null(KimlikKurali.TcknVknVeyaPasaport("A1234B"));            // 6 karakter ≥ 5
        Assert.Equal(GirdiKodu.PasaportBicim, KimlikKurali.TcknVknVeyaPasaport("A123")!.Value.Kod);   // 4 < 5
        Assert.Equal(GirdiKodu.PasaportBicim, KimlikKurali.TcknVknVeyaPasaport(new string('A', 21))!.Value.Kod);
    }

    [Fact]
    public void GOCB_T2_S04_K9_tavan_tam_sinir_gecer_bir_kurus_fazlasi_red()
    {
        Assert.Null(TutarKurali.Denetle(9_999_999_999_999.99m));
        Assert.Equal(GirdiKodu.SayiTavan, TutarKurali.Denetle(10_000_000_000_000.00m)!.Value.Kod);
        Assert.Equal(GirdiKodu.SayiOndalik, TutarKurali.Denetle(1.001m)!.Value.Kod);
        Assert.Null(TutarKurali.Denetle(1.10m));
    }
}

/// <summary>HTTP tur 2: evrak kimlik YALNIZ-YAZILIR kenarları + keşide yalnız PATCH'i (gerçek MySQL).</summary>
[Collection("http")]
public sealed class GOCBSinirTur2SunucuTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETGOC-B-TEST dg2 " + ad);
    }

    private (string? No, string? Hash, DateOnly? Keside) Oku(string id)
    {
        using var db = f.Vt.OlusturSistem();
        var e = db.KiymetliEvraklar.AsNoTracking().Single(x => x.Id == id);
        return (e.BorcluKimlikNo, e.BorcluKimlikNoHash, e.KesideTarihi);
    }

    [Fact]
    public async Task GOCB_T2_H01_Kimlik_bos_dize_bosluk_ve_sil_false_KORUR_keside_vadeyi_gecen_PATCH_400_kayit_degismez()
    {
        var k = YeniKiraci("kimlik kenar");
        var c = f.Ortam.Istemci(k);
        var vade = Zaman.YerelBugun().AddDays(20);
        var ok = await c.PostJson("/api/v1/evraklar", new Dictionary<string, object?>
        {
            ["evrakTuru"] = "CEK", ["tutar"] = 1000m, ["vadeGunu"] = vade.ToString("yyyy-MM-dd"), ["evrakNo"] = "T2-1",
            ["bankaAdi"] = "Ziraat", ["kaynakCariId"] = k.CariId, ["kurSnapshotId"] = k.KurId, ["borcluKimlikNo"] = "10000000146",
        });
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        string id;
        using (var db = f.Vt.OlusturSistem()) id = db.KiymetliEvraklar.AsNoTracking().Single(e => e.TenantId == k.TenantId).Id;
        var ilk = Oku(id);
        Assert.Equal("10000000146", ilk.No);
        Assert.NotNull(ilk.Hash);

        foreach (var deger in new object?[] { "", "   " })
        {
            var p = await c.PatchJson($"/api/v1/evraklar/{id}", new Dictionary<string, object?> { ["borcluKimlikNo"] = deger, ["notlar"] = "x" });
            Assert.True(p.IsSuccessStatusCode, await p.Content.ReadAsStringAsync());
            Assert.Equal(ilk, Oku(id));
        }
        var p2 = await c.PatchJson($"/api/v1/evraklar/{id}", new Dictionary<string, object?> { ["borcluKimlikSil"] = false });
        Assert.True(p2.IsSuccessStatusCode, await p2.Content.ReadAsStringAsync());
        Assert.Equal(ilk, Oku(id));

        // yalnız keşide gönderilir, kayıtlı vadeden 1 gün sonra → 400 kesideTarihi, kayıt değişmez
        var p3 = await c.PatchJson($"/api/v1/evraklar/{id}", new Dictionary<string, object?> { ["kesideTarihi"] = vade.AddDays(1).ToString("yyyy-MM-dd") });
        Assert.Equal(HttpStatusCode.BadRequest, p3.StatusCode);
        Assert.Contains("kesideTarihi", await p3.Content.ReadAsStringAsync());
        Assert.Equal(ilk, Oku(id));
        // keşide = vade → geçer
        var p4 = await c.PatchJson($"/api/v1/evraklar/{id}", new Dictionary<string, object?> { ["kesideTarihi"] = vade.ToString("yyyy-MM-dd") });
        Assert.True(p4.IsSuccessStatusCode, await p4.Content.ReadAsStringAsync());
        Assert.Equal(vade, Oku(id).Keside);
    }

    [Fact]
    public async Task GOCB_T2_H02_Cari_dogum_bugun_gecer_yarin_400_17_yas_gecer_120_yil_ustu_400()
    {
        var k = YeniKiraci("cari dogum");
        var c = f.Ortam.Istemci(k);
        var bugun = Zaman.YerelBugun();
        async Task<HttpResponseMessage> Cari(string ad, DateOnly d) => await c.PostJson("/api/v1/cari", new Dictionary<string, object?>
        { ["ad"] = ad, ["tur"] = "MUSTERI", ["dogumTarihi"] = d.ToString("yyyy-MM-dd") });
        var yarin = await Cari("Yarin Dogan", bugun.AddDays(1));
        Assert.Equal(HttpStatusCode.BadRequest, yarin.StatusCode);
        Assert.Contains("dogumTarihi", await yarin.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        var eski = await Cari("Cok Eski", bugun.AddYears(-120).AddDays(-1));
        Assert.Equal(HttpStatusCode.BadRequest, eski.StatusCode);
        var genc = await Cari("Genc Musteri", bugun.AddYears(-17));
        Assert.True(genc.IsSuccessStatusCode, await genc.Content.ReadAsStringAsync());
        using var db = f.Vt.OlusturSistem();
        Assert.False(db.Cariler.AsNoTracking().Any(x => x.TenantId == k.TenantId && (x.Ad == "Yarin Dogan" || x.Ad == "Cok Eski")));
    }
}
