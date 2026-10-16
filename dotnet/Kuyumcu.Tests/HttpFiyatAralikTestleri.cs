using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// FİYAT LİSTESİ — MAKUL ARALIK KAPISI (paket S-3c · kurul tur 3 engel 20)
//
// QA turu 3 kanıtı: "Has marjı −5, işçilik çarpanı 500 → POST 200". Yani eksi
// marj POS'ta maliyetin altında fiyat üretiyordu ve kimse uyarılmıyordu.
//
// SÖZLEŞME:
//   • SERT sınır (FiyatAraliklari) dışı değer → 400, onayla BİLE geçmez.
//   • RAHAT aralık dışı değer → riskOnayi=true YOKSA 400 (fail-closed).
//   • Var olan riskli değer DEĞİŞMİYORSA (yalnız ad/aktiflik güncelleniyorsa)
//     yeniden onay İSTENMEZ — aksi halde eski liste pasife bile alınamazdı.
//
// ⚠ NEDEN HTTP: DataAnnotations yalnız denetleyici yolunda çalışır; kapının
//   SERVİSTE de durduğunu görmek için gerçek uçtan geçilir.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class HttpFiyatAralikTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static object Govde(string ad, decimal marj, decimal carpan, bool? riskOnayi = null) =>
        new { ad, tur = "PERAKENDE", hasMarjYuzde = marj, iscilikCarpanYuzde = carpan, varsayilan = true, riskOnayi };

    private Task<HttpResponseMessage> Olustur(Kiraci k, string ad, decimal marj, decimal carpan, bool? onay = null)
        => f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/fiyat/listeler", Govde(ad, marj, carpan, onay));

    /// <summary>M20-1 — eksi marj ONAYSIZ kabul EDİLMEZ ve liste hiç doğmaz.</summary>
    [Fact]
    public async Task EKSI_marj_onaysiz_400_doner_ve_liste_YAZILMAZ()
    {
        var k = YeniKiraci("S3c eksi marj");
        var cevap = await Olustur(k, "Zararına liste", -5m, 100m);

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        var mesaj = (await cevap.Json()).GetProperty("message").GetString();
        Assert.Contains("maliyetin altına", mesaj);

        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.FiyatListeleri.AnyAsync(x => x.TenantId == k.TenantId && x.Ad == "Zararına liste"));
    }

    /// <summary>M20-2 — %500 işçilik çarpanı da onaysız geçmez (rahat üst sınır %200).</summary>
    [Fact]
    public async Task YUKSEK_iscilik_carpani_onaysiz_400_doner()
    {
        var k = YeniKiraci("S3c yüksek çarpan");
        var cevap = await Olustur(k, "Aşırı işçilik", 5m, 500m);

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Contains("İşçilik çarpanı", (await cevap.Json()).GetProperty("message").GetString());
    }

    /// <summary>M20-3 — bilinçli onayla eksi marj GEÇER ve veritabanına AYNEN yazılır.</summary>
    [Fact]
    public async Task EKSI_marj_riskOnayi_ile_KABUL_edilir()
    {
        var k = YeniKiraci("S3c onaylı eksi marj");
        var cevap = await Olustur(k, "İndirimli liste", -5m, 100m, onay: true);

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var id = (await cevap.Json()).GetProperty("id").GetString()!;

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(-5m, (await db.FiyatListeleri.AsNoTracking().SingleAsync(x => x.Id == id)).HasMarjYuzde);
    }

    /// <summary>M20-4 — SERT sınır dışı değer onayla BİLE geçmez.</summary>
    [Fact]
    public async Task SERT_sinir_disi_deger_ONAYLA_BILE_400_doner()
    {
        var k = YeniKiraci("S3c sert sınır");
        var cevap = await Olustur(k, "İmkânsız liste", -500m, 100m, onay: true);

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        // Sert sınır denetleyici yolunda [Range] ile, servis yolunda AraligiDogrula ile
        // durur; ikisinin gövde biçimi farklıdır (doğrulama listesi vs. tek mesaj).
        var metin = await cevap.Content.ReadAsStringAsync();
        Assert.Contains("marj", metin, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("500", metin);
    }

    /// <summary>M20-5 — PATCH ile riskli değere GEÇİŞ onaysız reddedilir; eski değer KORUNUR.</summary>
    [Fact]
    public async Task PATCH_riskli_degere_gecis_onaysiz_400_doner_ve_eski_deger_KORUNUR()
    {
        var k = YeniKiraci("S3c patch risk");
        var id = (await (await Olustur(k, "Perakende", 5m, 100m)).Json()).GetProperty("id").GetString()!;

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/fiyat/listeler/{id}", Govde("Perakende", 250m, 100m));

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(5m, (await db.FiyatListeleri.AsNoTracking().SingleAsync(x => x.Id == id)).HasMarjYuzde);
    }

    /// <summary>
    /// M20-6 — DEĞİŞMEYEN riskli değer yeniden onay istemez: onayla açılmış eksi
    /// marjlı liste, sonradan yalnız adı güncellenerek kaydedilebilir.
    /// (Kapı fazla kapanırsa var olan liste düzenlenemez hâle gelirdi.)
    /// </summary>
    [Fact]
    public async Task DEGISMEYEN_riskli_deger_yeniden_onay_ISTEMEZ()
    {
        var k = YeniKiraci("S3c değişmeyen risk");
        var id = (await (await Olustur(k, "İndirimli", -5m, 100m, onay: true)).Json()).GetProperty("id").GetString()!;

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/fiyat/listeler/{id}", Govde("İndirimli (yeni ad)", -5m, 100m));

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        var liste = await db.FiyatListeleri.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("İndirimli (yeni ad)", liste.Ad);
        Assert.Equal(-5m, liste.HasMarjYuzde);
    }

    /// <summary>M20-7 — makul değer eskisi gibi tek adımda geçer (kapı iş akışını kapatmıyor).</summary>
    [Fact]
    public async Task MAKUL_deger_onaysiz_GECER()
    {
        var k = YeniKiraci("S3c makul");
        var cevap = await Olustur(k, "Perakende", 12m, 120m);
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // İKİNCİ DİLİM (S-3c doğrulama artığı) — kapı YALNIZ liste uçlarındaydı.
    // Aynı iki alan üç yazma yolundan daha yazılıyordu ve hiçbiri kapıdan
    // geçmiyordu: ürün-özel POST/PATCH ve TOPLU UYGULAMA. Toplu uygulama tek
    // istekte yüzlerce ürünü etkilediği için sızıntı kapatılan yoldan zararlıydı.
    // ═════════════════════════════════════════════════════════════════════════

    private async Task<string> ListeAc(Kiraci k, string ad = "Perakende")
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/fiyat/listeler",
            new { ad, tur = "PERAKENDE", hasMarjYuzde = 5m, iscilikCarpanYuzde = 100m, varsayilan = true });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        return (await c.Json()).GetProperty("id").GetString()!;
    }

    private async Task<string> UrunAc(Kiraci k, string kod)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/urunler",
            new { tip = "ZIYNET", kod, ad = "Bilezik " + kod, ayarId = "K22", gramAgirlik = 10m });
        Assert.Equal(HttpStatusCode.Created, c.StatusCode);
        return (await c.Json()).GetProperty("id").GetString()!;
    }

    /// <summary>M20-8 — ürüne özel EKSİ marj onaysız 400; kayıt hiç doğmaz.</summary>
    [Fact]
    public async Task URUN_OZEL_eksi_marj_onaysiz_400_doner_ve_kayit_YAZILMAZ()
    {
        var k = YeniKiraci("S3c ürün özel eksi marj");
        var listeId = await ListeAc(k);
        var urunId = await UrunAc(k, "S3C-1");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/fiyat/listeler/{listeId}/urun-fiyatlari",
            new { urunId, hasMarjYuzde = -5m, iscilikCarpanYuzde = 100m });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Contains("maliyetin altına", (await cevap.Json()).GetProperty("message").GetString());

        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.FiyatListesiUrunFiyatlari.AnyAsync(x => x.TenantId == k.TenantId));
    }

    /// <summary>M20-9 — aynı gövde riskOnayi ile GEÇER ve değer AYNEN yazılır.</summary>
    [Fact]
    public async Task URUN_OZEL_eksi_marj_riskOnayi_ile_KABUL_edilir()
    {
        var k = YeniKiraci("S3c ürün özel onaylı");
        var listeId = await ListeAc(k);
        var urunId = await UrunAc(k, "S3C-2");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/fiyat/listeler/{listeId}/urun-fiyatlari",
            new { urunId, hasMarjYuzde = -5m, iscilikCarpanYuzde = 100m, riskOnayi = true });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(-5m, (await db.FiyatListesiUrunFiyatlari.AsNoTracking()
            .SingleAsync(x => x.TenantId == k.TenantId && x.UrunId == urunId)).HasMarjYuzde);
    }

    /// <summary>M20-10 — ürüne özel PATCH ile riskli değere geçiş onaysız 400; eski değer KORUNUR.</summary>
    [Fact]
    public async Task URUN_OZEL_PATCH_riskli_degere_gecis_onaysiz_400_doner()
    {
        var k = YeniKiraci("S3c ürün özel patch");
        var listeId = await ListeAc(k);
        var urunId = await UrunAc(k, "S3C-3");
        var ekle = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/fiyat/listeler/{listeId}/urun-fiyatlari",
            new { urunId, hasMarjYuzde = 10m, iscilikCarpanYuzde = 120m });
        Assert.Equal(HttpStatusCode.OK, ekle.StatusCode);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchAsJsonAsync(
            $"/api/v1/fiyat/listeler/{listeId}/urun-fiyatlari/{urunId}",
            new { iscilikCarpanYuzde = 480m });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Contains("İşçilik çarpanı", (await cevap.Json()).GetProperty("message").GetString());

        using var db = f.Vt.OlusturSistem();
        var kayit = await db.FiyatListesiUrunFiyatlari.AsNoTracking()
            .SingleAsync(x => x.TenantId == k.TenantId && x.UrunId == urunId);
        Assert.Equal(120m, kayit.IscilikCarpanYuzde);
    }

    /// <summary>
    /// M20-11 — ürüne özel PATCH'te DEĞİŞMEYEN riskli değer yeniden onay istemez:
    /// onayla açılmış eksi marjlı kayıt sonradan yalnız notu güncellenerek kaydedilir.
    /// </summary>
    [Fact]
    public async Task URUN_OZEL_degismeyen_riskli_deger_yeniden_onay_ISTEMEZ()
    {
        var k = YeniKiraci("S3c ürün özel değişmeyen");
        var listeId = await ListeAc(k);
        var urunId = await UrunAc(k, "S3C-4");
        Assert.Equal(HttpStatusCode.OK, (await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/fiyat/listeler/{listeId}/urun-fiyatlari",
            new { urunId, hasMarjYuzde = -5m, riskOnayi = true })).StatusCode);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchAsJsonAsync(
            $"/api/v1/fiyat/listeler/{listeId}/urun-fiyatlari/{urunId}", new { not = "toptancı" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        var kayit = await db.FiyatListesiUrunFiyatlari.AsNoTracking()
            .SingleAsync(x => x.TenantId == k.TenantId && x.UrunId == urunId);
        Assert.Equal(-5m, kayit.HasMarjYuzde);
        Assert.Equal("toptancı", kayit.Not);
    }

    /// <summary>
    /// M20-12 — TOPLU uygulama: riskli değer onaysız 400 ve HİÇBİR ürün fiyatı yazılmaz.
    /// Kapı METODUN İLK SATIRINDA durur: kur/hedef sorgusu bile çalışmaz.
    /// </summary>
    [Fact]
    public async Task TOPLU_UYGULA_riskli_deger_onaysiz_400_doner_ve_HICBIR_fiyat_yazilmaz()
    {
        var k = YeniKiraci("S3c toplu risk");
        var listeId = await ListeAc(k);
        var urunId = await UrunAc(k, "S3C-5");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/fiyat/listeler/{listeId}/toplu-uygula",
            new { hedefTur = "SECILI", urunIdler = new[] { urunId }, iscilikCarpanYuzde = 480m, kurSnapshotId = "yok" });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Contains("İşçilik çarpanı", (await cevap.Json()).GetProperty("message").GetString());

        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.FiyatListesiUrunFiyatlari.AnyAsync(x => x.TenantId == k.TenantId));
    }

    /// <summary>M20-13 — TOPLU uygulamada SERT sınır dışı değer onayla BİLE geçmez.</summary>
    [Fact]
    public async Task TOPLU_UYGULA_sert_sinir_disi_ONAYLA_BILE_400_doner()
    {
        var k = YeniKiraci("S3c toplu sert sınır");
        var listeId = await ListeAc(k);
        var urunId = await UrunAc(k, "S3C-6");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/fiyat/listeler/{listeId}/toplu-uygula",
            new
            {
                hedefTur = "SECILI", urunIdler = new[] { urunId }, iscilikCarpanYuzde = 900m,
                kurSnapshotId = "yok", riskOnayi = true,
            });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        var metin = await cevap.Content.ReadAsStringAsync();
        Assert.Contains("500", metin);

        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.FiyatListesiUrunFiyatlari.AnyAsync(x => x.TenantId == k.TenantId));
    }

    /// <summary>
    /// M20-14 — ürüne özel POST'ta SERT sınır dışı değer (DTO [Range] hizası):
    /// eski hâlde [Range(-100,1000)] idi ve çarpan 900 kapıdan DAHA GEVŞEKTİ.
    /// </summary>
    [Fact]
    public async Task URUN_OZEL_sert_sinir_disi_deger_400_doner()
    {
        var k = YeniKiraci("S3c ürün özel sert sınır");
        var listeId = await ListeAc(k);
        var urunId = await UrunAc(k, "S3C-7");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/fiyat/listeler/{listeId}/urun-fiyatlari",
            new { urunId, iscilikCarpanYuzde = 900m, riskOnayi = true });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.FiyatListesiUrunFiyatlari.AnyAsync(x => x.TenantId == k.TenantId));
    }

    /// <summary>
    /// M20-15 — mesaj HER riskli alanı adıyla söyler. Tek kod döndüren sözleşme
    /// "çarpanı 480 yaptım, bana marjdan bahsediyor" diyen yanlış alanı gösterirdi
    /// (canlı doğrulamada görüldü).
    /// </summary>
    [Fact]
    public async Task RISK_mesaji_HER_riskli_alani_adiyla_soyler()
    {
        var k = YeniKiraci("S3c çoklu risk mesajı");
        var listeId = await ListeAc(k);
        var urunId = await UrunAc(k, "S3C-8");
        Assert.Equal(HttpStatusCode.OK, (await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/fiyat/listeler/{listeId}/urun-fiyatlari",
            new { urunId, hasMarjYuzde = -5m, riskOnayi = true })).StatusCode);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchAsJsonAsync(
            $"/api/v1/fiyat/listeler/{listeId}/urun-fiyatlari/{urunId}", new { iscilikCarpanYuzde = 480m });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        var mesaj = (await cevap.Json()).GetProperty("message").GetString()!;
        Assert.Contains("maliyetin altına", mesaj);
        Assert.Contains("İşçilik çarpanı", mesaj);
    }
}
