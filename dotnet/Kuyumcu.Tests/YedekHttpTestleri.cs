using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// /api/v1/yedekler — HTTP KAPILARI (paket T-3 Faz A)
//
// Sınanan sözleşme: SAHİP dışı 403 · gerekçe < 20 → 400 · ikinci bekleyen
// talep → 429 · İNDİRME UCU YOK (404) · çapraz kiracı talebine dokunulamaz.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class YedekHttpTestleri(HttpFikstur f)
{
    private const string Ozet = "/api/v1/yedekler/ozet";
    private const string Talep = "/api/v1/yedekler/talep";

    [Fact]
    public async Task Ozet_SahipOlmayan_403()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "T3-http-403");

        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync(Ozet);
        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    [Fact]
    public async Task Ozet_Sahip_200()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "T3-http-200");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").GetAsync(Ozet);
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        var govde = await cevap.Json();
        // Sözleşme camelCase ve ölçüm/taahhüt AYRI alanlar.
        Assert.True(govde.TryGetProperty("rpoDakikaTaahhut", out _));
        Assert.True(govde.TryGetProperty("rtoDakikaTaahhut", out _));
        Assert.True(govde.TryGetProperty("rpoDakikaOlculen", out _));
    }

    [Fact]
    public async Task TalepAc_GerekceKisa_400()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "T3-http-kisa");

        var cevap = await f.Ortam.Istemci(k).PostJson(Talep, new { gerekce = "kısa" });
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);

        // Reddin MODEL DOĞRULAMA katmanından geldiği de sınanır: DTO'daki
        // [MinLength(20)] kaldırılırsa istek servise düşer ve gövde biçimi
        // değişir (errors zarfı kaybolur) — mutasyon bu yüzden yakalanır.
        var govde = await cevap.Json();
        Assert.True(govde.TryGetProperty("alanlar", out var alanlar),
            "Kısa gerekçe controller katmanındaki model doğrulamasıyla reddedilmeli (alan→mesaj zarfı).");
        Assert.True(alanlar.EnumerateObject().Any(),
            "Alan→mesaj sözlüğü dolu olmalı; ekran mesajı alanın altına yazar.");
    }

    [Fact]
    public async Task TalepAc_Gecerli_201_DenetimKaydiOlusur()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "T3-http-201");

        var cevap = await f.Ortam.Istemci(k)
            .PostJson(Talep, new { gerekce = "aylık denetim öncesi elle yedek alınıyor" });
        Assert.Equal(HttpStatusCode.Created, cevap.StatusCode);

        var id = (await cevap.Json()).GetProperty("id").GetString();
        Assert.False(string.IsNullOrEmpty(id));

        using var kontrol = f.Vt.Olustur(k);
        // ⚠ Otomatik yazma dinleyicisi Added varlıklarda Detay'ı BOŞ bırakır;
        // "NEDEN" sorusunun cevabı ancak GEREKÇELİ izle (YazmaKaydet) gelir.
        // Bu yüzden izin VARLIĞI değil, GEREKÇEYİ taşıdığı sınanır — gerekçeli
        // satır silinirse test kırmızıya döner (M-T3-8).
        var izler = await kontrol.DenetimKayitlari
            .Where(d => d.VarlikTuru == "YedekTalebi" && d.VarlikId == id)
            .ToListAsync();
        Assert.NotEmpty(izler);
        Assert.Contains(izler, d => d.Detay != null && d.Detay.Contains("aylık denetim öncesi"));
    }

    [Fact]
    public async Task TalepAc_IkinciKez_429()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "T3-http-429");
        var istemci = f.Ortam.Istemci(k);

        var ilk = await istemci.PostJson(Talep, new { gerekce = "ilk talep, aylık yedek denetimi" });
        Assert.Equal(HttpStatusCode.Created, ilk.StatusCode);

        var ikinci = await istemci.PostJson(Talep, new { gerekce = "ikinci talep, aynı anda açılıyor" });
        Assert.Equal(HttpStatusCode.TooManyRequests, ikinci.StatusCode);
    }

    [Fact]
    public async Task YedekDosyasiIndirmeUcu_YOK_404()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "T3-http-indir");
        var istemci = f.Ortam.Istemci(k);

        // Dosya TÜM kiracıların verisini taşır: indirme yüzeyi HİÇ açılmadı.
        foreach (var yol in new[]
                 {
                     "/api/v1/yedekler/abc/indir",
                     "/api/v1/yedekler/indir",
                     "/api/v1/yedekler/dosya",
                 })
        {
            var cevap = await istemci.GetAsync(yol);
            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }
    }

    [Fact]
    public async Task BaskaKiracininTalebi_PatchEdilemez_404()
    {
        Kiraci a, b;
        using (var db = f.Vt.OlusturSistem())
        {
            a = TestKurulum.KiraciKur(db, "T3-capraz-A");
            b = TestKurulum.KiraciKur(db, "T3-capraz-B");
        }

        var acilan = await f.Ortam.Istemci(a)
            .PostJson(Talep, new { gerekce = "A kiracısının elle yedek talebi" });
        var id = (await acilan.Json()).GetProperty("id").GetString();

        var istek = new HttpRequestMessage(HttpMethod.Patch, $"{Talep}/{id}")
        {
            Content = JsonContent.Create(new { iptalGerekce = "B kiracısı iptal etmeye çalışıyor" }),
        };
        var cevap = await f.Ortam.Istemci(b).SendAsync(istek);
        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);

        // Kayıt A'da DOKUNULMADAN durur.
        using var kontrol = f.Vt.Olustur(a);
        var kayit = await kontrol.YedekTalepleri.AsNoTracking().SingleAsync(t => t.Id == id);
        Assert.Equal(YedekTalepDurumu.Bekliyor, kayit.Durum);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET P-56 — PATCH /yedekler/talep/{id}/gerekce
    // ═════════════════════════════════════════════════════════════════════════

    private static HttpRequestMessage GerekceIstegi(string id, object govde) =>
        new(HttpMethod.Patch, $"{Talep}/{id}/gerekce") { Content = JsonContent.Create(govde) };

    private async Task<string> BekleyenTalepAc(Kiraci k, string gerekce)
    {
        var cevap = await f.Ortam.Istemci(k).PostJson(Talep, new { gerekce });
        Assert.Equal(HttpStatusCode.Created, cevap.StatusCode);
        var id = (await cevap.Json()).GetProperty("id").GetString();
        Assert.False(string.IsNullOrEmpty(id));
        return id!;
    }

    [Fact]
    public async Task GerekceDegisimiDenetimIzineYazilir()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "P56-http-iz");

        const string Eski = "aylık denetim öncesi elle yedek";
        const string Yeni = "mali müşavir talebiyle ek yedek alınıyor";
        var id = await BekleyenTalepAc(k, Eski);

        var cevap = await f.Ortam.Istemci(k).SendAsync(GerekceIstegi(id, new { gerekce = Yeni }));
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        var govde = await cevap.Json();
        Assert.Equal(Yeni, govde.GetProperty("gerekce").GetString());
        // Talep İPTAL OLMADI: gerekçe düzeltmesi durumu değiştirmez.
        Assert.Equal(YedekTalepDurumu.Bekliyor, govde.GetProperty("durum").GetString());

        using var kontrol = f.Vt.Olustur(k);
        var kayit = await kontrol.YedekTalepleri.AsNoTracking().SingleAsync(t => t.Id == id);
        Assert.Equal(Yeni, kayit.Gerekce);
        Assert.Equal(YedekTalepDurumu.Bekliyor, kayit.Durum);

        // ⚠ İzin VARLIĞI değil, ESKİ ve YENİ değeri BİRLİKTE taşıdığı sınanır:
        // gerekçeli satır (YazmaKaydet) silinirse test kırmızıya döner (M56-3).
        var izler = await kontrol.DenetimKayitlari
            .Where(d => d.VarlikTuru == "YedekTalebi" && d.VarlikId == id)
            .ToListAsync();
        Assert.Contains(izler, d => d.Detay != null
            && d.Detay.Contains("gerekçesi güncellendi")
            && d.Detay.Contains(Eski) && d.Detay.Contains(Yeni));
    }

    [Fact]
    public async Task GerekceGuncelle_KisaGerekce_400()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "P56-http-kisa");
        var id = await BekleyenTalepAc(k, "aylık denetim öncesi elle yedek");

        var cevap = await f.Ortam.Istemci(k).SendAsync(GerekceIstegi(id, new { gerekce = "kısa" }));
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);

        using var kontrol = f.Vt.Olustur(k);
        var kayit = await kontrol.YedekTalepleri.AsNoTracking().SingleAsync(t => t.Id == id);
        Assert.Equal("aylık denetim öncesi elle yedek", kayit.Gerekce);
    }

    [Fact]
    public async Task GerekceGuncelle_IslenmisTalep_400()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "P56-http-islenmis");
        var id = await BekleyenTalepAc(k, "aylık denetim öncesi elle yedek");

        // Talebi İPTAL et → artık BEKLIYOR değil.
        var iptal = new HttpRequestMessage(HttpMethod.Patch, $"{Talep}/{id}")
        {
            Content = JsonContent.Create(new { iptalGerekce = "yanlışlıkla açıldı, iptal edildi" }),
        };
        Assert.Equal(HttpStatusCode.OK, (await f.Ortam.Istemci(k).SendAsync(iptal)).StatusCode);

        var cevap = await f.Ortam.Istemci(k)
            .SendAsync(GerekceIstegi(id, new { gerekce = "işlenmiş talebin gerekçesi değişmez" }));
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);

        using var kontrol = f.Vt.Olustur(k);
        var kayit = await kontrol.YedekTalepleri.AsNoTracking().SingleAsync(t => t.Id == id);
        Assert.Equal("aylık denetim öncesi elle yedek", kayit.Gerekce);
    }

    [Fact]
    public async Task GerekceGuncelle_AlanGonderilmedi_Korunur()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "P56-http-koru");
        var id = await BekleyenTalepAc(k, "aylık denetim öncesi elle yedek");

        // PATCH sözleşmesi: gövdede alan YOKSA korunur.
        var cevap = await f.Ortam.Istemci(k).SendAsync(GerekceIstegi(id, new { }));
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.False((await cevap.Json()).GetProperty("degisti").GetBoolean());

        using var kontrol = f.Vt.Olustur(k);
        var kayit = await kontrol.YedekTalepleri.AsNoTracking().SingleAsync(t => t.Id == id);
        Assert.Equal("aylık denetim öncesi elle yedek", kayit.Gerekce);
    }

    [Fact]
    public async Task GerekceGuncelle_SahipOlmayan_403()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "P56-http-403");
        var id = await BekleyenTalepAc(k, "aylık denetim öncesi elle yedek");

        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR")
            .SendAsync(GerekceIstegi(id, new { gerekce = "tezgâhtar değiştirmeye çalışıyor" }));
        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET P-77 — POST /yedekler/dogrulama-talep ("Şimdi doğrula")
    // ═════════════════════════════════════════════════════════════════════════

    private const string DogrulamaTalep = "/api/v1/yedekler/dogrulama-talep";

    [Fact]
    public async Task YedekApi_DogrulamaTalebi_KuyrugaYazilir()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "P77-http-kuyruk");
        var istemci = f.Ortam.Istemci(k);

        // Bekleyen bir YEDEK talebi doğrulama talebini ENGELLEMEZ (iki ayrı iş).
        await BekleyenTalepAc(k, "aylık denetim öncesi elle yedek");

        var cevap = await istemci.PostJson(DogrulamaTalep, new { });
        Assert.Equal(HttpStatusCode.Created, cevap.StatusCode);
        var govde = await cevap.Json();
        var id = govde.GetProperty("id").GetString();
        Assert.False(string.IsNullOrEmpty(id));
        Assert.Equal(YedekTalepTuru.Dogrulama, govde.GetProperty("tur").GetString());
        Assert.Equal(YedekTalepDurumu.Bekliyor, govde.GetProperty("durum").GetString());
        Assert.False(string.IsNullOrWhiteSpace(govde.GetProperty("mesaj").GetString()));

        using (var kontrol = f.Vt.Olustur(k))
        {
            var dogrulamalar = await kontrol.YedekTalepleri.AsNoTracking()
                .Where(t => t.Tur == YedekTalepTuru.Dogrulama).ToListAsync();
            var tek = Assert.Single(dogrulamalar);
            Assert.Equal(id, tek.Id);
            Assert.Equal(YedekTalepDurumu.Bekliyor, tek.Durum);
            Assert.Equal(k.TenantId, tek.TenantId);
            // Yedek talebi YEDEK türünde kaldı (tür karışmaz).
            Assert.Single(await kontrol.YedekTalepleri.AsNoTracking()
                .Where(t => t.Tur == YedekTalepTuru.Yedek).ToListAsync());
        }

        // GET özet bekleyen doğrulama talebini söyler (ekran kilidi buna bakar).
        var ozet = await (await istemci.GetAsync(Ozet)).Json();
        Assert.Equal(id, ozet.GetProperty("bekleyenDogrulamaTalebi").GetProperty("id").GetString());

        // Talepler listesinde tür görünür.
        var liste = await (await istemci.GetAsync("/api/v1/yedekler/talepler")).Json();
        Assert.Contains(liste.GetProperty("talepler").EnumerateArray(),
            t => t.GetProperty("id").GetString() == id
                 && t.GetProperty("tur").GetString() == YedekTalepTuru.Dogrulama);
    }

    [Fact]
    public async Task YedekApi_BekleyenTalepVarken_IkinciTalep409()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "P77-http-409");
        var istemci = f.Ortam.Istemci(k);

        // Bekleyen talep yokken özet null döner.
        var bos = await (await istemci.GetAsync(Ozet)).Json();
        Assert.Equal(System.Text.Json.JsonValueKind.Null,
            bos.GetProperty("bekleyenDogrulamaTalebi").ValueKind);

        var ilk = await istemci.PostJson(DogrulamaTalep, new { });
        Assert.Equal(HttpStatusCode.Created, ilk.StatusCode);
        var ilkId = (await ilk.Json()).GetProperty("id").GetString();

        var ikinci = await istemci.PostJson(DogrulamaTalep, new { });
        Assert.Equal(HttpStatusCode.Conflict, ikinci.StatusCode);

        using (var kontrol = f.Vt.Olustur(k))
        {
            // Çift kuyruk YOK: hâlâ tek doğrulama satırı.
            Assert.Equal(1, await kontrol.YedekTalepleri
                .CountAsync(t => t.Tur == YedekTalepTuru.Dogrulama));
        }

        // Geri dönüş yolu: bekleyen talep iptal edilince (pasife alma) yenisi açılabilir.
        var iptal = new HttpRequestMessage(HttpMethod.Patch, $"{Talep}/{ilkId}")
        {
            Content = JsonContent.Create(new { iptalGerekce = "yanlışlıkla istendi, geri alınıyor" }),
        };
        Assert.Equal(HttpStatusCode.OK, (await istemci.SendAsync(iptal)).StatusCode);
        var ucuncu = await istemci.PostJson(DogrulamaTalep, new { });
        Assert.Equal(HttpStatusCode.Created, ucuncu.StatusCode);
    }

    [Fact]
    public async Task YedekApi_DogrulamaTalebi_YetkisizRol403()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "P77-http-403");

        // Rol kapısı mevcut yedek uçlarıyla BİREBİR: yalnız SAHİP (ölçüldü —
        // YedekController sınıf düzeyi [RolGerekli("SAHIP")]).
        // ⚠ YALITIM: MÜDÜR'e "yedek" MODÜL yetkisi açıkça verilir; böylece 403'ün
        // ModulYetkisi'nden değil ROL kapısından geldiği sınanır (rol niteliği
        // kaldırılırsa MÜDÜR 201 alır ve test kırmızıya döner — M77-2).
        var put = new HttpRequestMessage(HttpMethod.Put, "/api/v1/yetkiler")
        {
            Content = JsonContent.Create(new { matris = new Dictionary<string, string[]>
            {
                ["MUDUR"] = ["yedek", "rapor"],
            } }),
        };
        Assert.Equal(HttpStatusCode.OK, (await f.Ortam.Istemci(k).SendAsync(put)).StatusCode);
        var benim = await (await f.Ortam.Istemci(k, "MUDUR").GetAsync("/api/v1/yetkiler/benim")).Json();
        Assert.Contains(benim.GetProperty("moduller").EnumerateArray(), m => m.GetString() == "yedek");

        foreach (var rol in new[] { "TEZGAHTAR", "MUDUR" })
        {
            var cevap = await f.Ortam.Istemci(k, rol).PostJson(DogrulamaTalep, new { });
            Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        }

        using var kontrol = f.Vt.Olustur(k);
        Assert.Equal(0, await kontrol.YedekTalepleri.CountAsync());
    }

    [Fact]
    public async Task YedekApi_DogrulamaTalebi_DenetimIzineYazilir()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "P77-http-iz");

        var cevap = await f.Ortam.Istemci(k).PostJson(DogrulamaTalep, new { });
        Assert.Equal(HttpStatusCode.Created, cevap.StatusCode);
        var id = (await cevap.Json()).GetProperty("id").GetString();

        using var kontrol = f.Vt.Olustur(k);
        // ⚠ Otomatik dinleyici Added varlıkta Detay'ı BOŞ bırakır; açık
        // YazmaKaydet satırı silinirse bu test kırmızıya döner (M77-3).
        var izler = await kontrol.DenetimKayitlari
            .Where(d => d.VarlikTuru == "YedekTalebi" && d.VarlikId == id)
            .ToListAsync();
        Assert.Contains(izler, d => d.Detay != null && d.Detay.Contains("doğrulama talebi açıldı"));
    }
}
